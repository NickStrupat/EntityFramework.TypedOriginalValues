using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace EntityFramework.TypedOriginalValues {
	internal static class DbPropertyValuesWrapper<TEntity> where TEntity : class
	{
		public static TEntity Create(DbPropertyValues originalValues) => Factory(originalValues);

		private static readonly Func<DbPropertyValues, TEntity> Factory = GetFactory();

		private static Func<DbPropertyValues, TEntity> GetFactory()
		{
			var generatedName = typeof (TEntity).Name + "__DbPropertyValuesWrapper";
			var assemblyName = new AssemblyName(generatedName + "Assembly");
			var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule(generatedName + "Module");
			var typeBuilder = moduleBuilder.DefineType(generatedName, TypeAttributes.Public | TypeAttributes.Class, typeof(TEntity));

			var fieldBuilder = typeBuilder.DefineField("dbPropertyValues", typeof (DbPropertyValues), FieldAttributes.Private | FieldAttributes.InitOnly);

			var constructorParameterTypes = new[] {typeof (DbPropertyValues)};
			var baseConstructor = typeof(TEntity).GetConstructor(Type.EmptyTypes);
			var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, constructorParameterTypes);
			var ilGenerator = constructorBuilder.GetILGenerator();
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Call, baseConstructor);
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldarg_1);
			ilGenerator.Emit(OpCodes.Stfld, fieldBuilder);

			var properties = typeof (TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			var virtualProperties = properties.Where(IsOverridable).ToArray();
			foreach (var property in properties.Except(virtualProperties).Where(x => x.GetSetMethod(nonPublic:true) != null)) {
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Ldarg_1);
				ilGenerator.Emit(OpCodes.Ldstr, property.Name);
				ilGenerator.Emit(OpCodes.Call, typeof(DbPropertyValues).GetMethod(nameof(DbPropertyValues.GetValue)).MakeGenericMethod(property.PropertyType));
				var setter = property.GetSetMethod(nonPublic:true);
				if (setter.IsPrivate || setter.IsAssembly)
					throw new Exception("Non-virtual properties must have a public or protected setter. Alternatively, make the property virtual.");
				ilGenerator.Emit(OpCodes.Call, setter);
			}

			ilGenerator.Emit(OpCodes.Ret);

			foreach (var property in virtualProperties)
				EmitPropertyOverride(typeBuilder, property, fieldBuilder);

			var type = typeBuilder.CreateType();
			var constructor = type.GetConstructor(constructorParameterTypes);
			var parameter = Expression.Parameter(typeof (DbPropertyValues));
			return Expression.Lambda<Func<DbPropertyValues, TEntity>>(Expression.New(constructor, parameter), parameter).Compile();
		}

		private static void EmitPropertyOverride(TypeBuilder typeBuilder, PropertyInfo property, FieldInfo dbPropertyValuesFieldInfo) {
			var getter = property.GetGetMethod();
			var getAndSetAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
			var getterBuilder = typeBuilder.DefineMethod(getter.Name, getAndSetAttributes, property.PropertyType, null);
			var ilGenerator = getterBuilder.GetILGenerator();
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldfld, dbPropertyValuesFieldInfo);
			ilGenerator.Emit(OpCodes.Ldstr, property.Name);
			ilGenerator.Emit(OpCodes.Call, typeof(DbPropertyValues).GetMethod(nameof(DbPropertyValues.GetValue)).MakeGenericMethod(property.PropertyType));
			ilGenerator.Emit(OpCodes.Ret);

			var setter = property.GetSetMethod(nonPublic:true);
			if (setter.IsAssembly)
				return;
			var setterBuilder = typeBuilder.DefineMethod(setter.Name, getAndSetAttributes, null, new[] {property.PropertyType});
			ilGenerator = setterBuilder.GetILGenerator();
			ilGenerator.Emit(OpCodes.Ldstr, "Properties cannot be set on a proxy object of `OriginalValues`");
			ilGenerator.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(new [] {typeof(String)}));
			ilGenerator.Emit(OpCodes.Throw);
		}

		private static Boolean IsOverridable(PropertyInfo propertyInfo)
		{
			var getter = propertyInfo.GetGetMethod();
			return getter.IsVirtual || getter.IsAbstract;
		}
	}
}