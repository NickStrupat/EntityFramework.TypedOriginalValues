using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

#if EF_CORE

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityFrameworkCore.TypedOriginalValues {

#else

using System.Data.Entity.Infrastructure;

namespace EntityFramework.TypedOriginalValues {

#endif

	internal static class OriginalValuesWrapper<TEntity> where TEntity : class {
#if EF_CORE
		public static TEntity Create(EntityEntry originalValues) => Factory(originalValues);

		private static readonly Func<EntityEntry, TEntity> Factory = GetFactory();

		private static Func<EntityEntry, TEntity> GetFactory() {
			var valuesType = typeof(EntityEntry);
#else
		public static TEntity Create(DbEntityEntry<TEntity> entityEntry) => Factory(entityEntry.OriginalValues);

		private static readonly Func<DbPropertyValues, TEntity> Factory = GetFactory();

		private static Func<DbPropertyValues, TEntity> GetFactory() {
			var valuesType = typeof(DbPropertyValues);
#endif

			var generatedName = typeof(TEntity).Name + "__OriginalValuesWrapper";
			var assemblyName = new AssemblyName(generatedName + "Assembly");
#if DEBUG && !NET_CORE
			var dynamicAssemblyFileName = generatedName + ".Emitted.dll";
			var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule(generatedName + "Module", dynamicAssemblyFileName);
#else
			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule(generatedName + "Module");
#endif

			var typeBuilder = moduleBuilder.DefineType(generatedName, TypeAttributes.Public | TypeAttributes.Class, typeof(TEntity));

			var fieldBuilder = typeBuilder.DefineField("values", valuesType, FieldAttributes.Private | FieldAttributes.InitOnly);
			var constructorParameterTypes = new[] { valuesType };
			var baseConstructor = typeof(TEntity).GetConstructor(Type.EmptyTypes);
			var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, constructorParameterTypes);
			var ilGenerator = constructorBuilder.GetILGenerator();
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Call, baseConstructor);
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldarg_1);
			ilGenerator.Emit(OpCodes.Stfld, fieldBuilder);

			var properties = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			var virtualProperties = properties.Where(IsOverridable).ToArray();
			foreach (var property in properties.Except(virtualProperties).Where(x => x.GetSetMethod(nonPublic: true) != null)) {
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Ldarg_1);
				ilGenerator.Emit(OpCodes.Ldstr, property.Name);
				EmitGetValueInstructions(ilGenerator, property);

				var setter = property.GetSetMethod(nonPublic: true);
				if (setter.IsPrivate || setter.IsAssembly)
					throw new Exception($"Invalid setter for property `{property.DeclaringType.Name}.{property.Name}`. Non-virtual properties must have a public or protected setter. Alternatively, make the property virtual.");
				ilGenerator.Emit(OpCodes.Call, setter);
			}

			ilGenerator.Emit(OpCodes.Ret);

			foreach (var property in virtualProperties)
				EmitPropertyOverride(typeBuilder, property, fieldBuilder);
#if NET_4_0
			var type = typeBuilder.CreateType();
#else
			var type = typeBuilder.CreateTypeInfo();
#endif

#if DEBUG && !NET_CORE
			assemblyBuilder.Save(dynamicAssemblyFileName);
#endif
			var constructor = type.GetConstructor(constructorParameterTypes);

#if EF_CORE
			var parameter = Expression.Parameter(typeof(EntityEntry));
			return Expression.Lambda<Func<EntityEntry, TEntity>>(Expression.New(constructor, parameter), parameter).Compile();
#else
			var parameter = Expression.Parameter(typeof(DbPropertyValues));
			return Expression.Lambda<Func<DbPropertyValues, TEntity>>(Expression.New(constructor, parameter), parameter).Compile();
#endif
		}

		private static void EmitPropertyOverride(TypeBuilder typeBuilder, PropertyInfo property, FieldInfo dbPropertyValuesFieldInfo) {
			var getter = property.GetGetMethod();

			var newProperty = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, Type.EmptyTypes);

			var getAndSetAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
			var getterBuilder = typeBuilder.DefineMethod(getter.Name, getAndSetAttributes, property.PropertyType, null);
			var ilGenerator = getterBuilder.GetILGenerator();
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldfld, dbPropertyValuesFieldInfo);
			ilGenerator.Emit(OpCodes.Ldstr, property.Name);
			EmitGetValueInstructions(ilGenerator, property);
			ilGenerator.Emit(OpCodes.Ret);

			var setter = property.GetSetMethod(nonPublic: true);
			if (setter.IsAssembly)
				return;
			var setterBuilder = typeBuilder.DefineMethod(setter.Name, getAndSetAttributes, null, new[] { property.PropertyType });
			ilGenerator = setterBuilder.GetILGenerator();
			ilGenerator.Emit(OpCodes.Ldstr, "Properties cannot be set on a proxy object of `OriginalValues`");
			ilGenerator.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new[] { typeof(String) }));
			ilGenerator.Emit(OpCodes.Throw);

			newProperty.SetGetMethod(getterBuilder);
			newProperty.SetSetMethod(setterBuilder);
		}

		private static void EmitGetValueInstructions(ILGenerator ilGenerator, PropertyInfo property) {
#if EF_CORE
			ilGenerator.Emit(OpCodes.Callvirt, typeof(EntityEntry).GetMethod(nameof(EntityEntry.Property)));
			ilGenerator.Emit(OpCodes.Callvirt, typeof(PropertyEntry).GetProperty(nameof(PropertyEntry.OriginalValue)).GetGetMethod());
			ilGenerator.Emit(OpCodes.Castclass, property.PropertyType);
#else
			ilGenerator.Emit(OpCodes.Call, typeof(DbPropertyValues).GetMethod(nameof(DbPropertyValues.GetValue)).MakeGenericMethod(property.PropertyType));
#endif
		}

		private static Boolean IsOverridable(PropertyInfo propertyInfo) {
			var getter = propertyInfo.GetGetMethod();
			return getter.IsVirtual || getter.IsAbstract;
		}
	}
}