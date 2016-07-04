using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

#if EF_CORE
using Microsoft.EntityFrameworkCore;
using EntityFrameworkCore.TypedOriginalValues;
namespace TestsCore {
#else
using EntityFramework.TypedOriginalValues;
using System.Data.Entity;
namespace Tests {
#endif

	public class ParameterlessConstructorVisibility {
		// Parameterless constructor visibilty
		[Fact] public void Public()            => Tests.SimpleProperty<Context, PublicConstructor, String>           (x => x.PublicConstructors           , x => x.Text, x => x.Text = "Orig", x => x.Text = "New", PublicConstructor.New);
		[Fact] public void Protected()         => Tests.SimpleProperty<Context, ProtectedConstructor, String>        (x => x.ProtectedConstructors        , x => x.Text, x => x.Text = "Orig", x => x.Text = "New", ProtectedConstructor.New);
		[Fact] public void Private()           => Tests.SimpleProperty<Context, PrivateConstructor, String>          (x => x.PrivateConstructors          , x => x.Text, x => x.Text = "Orig", x => x.Text = "New", PrivateConstructor.New);
		[Fact] public void Internal()          => Tests.SimpleProperty<Context, InternalConstructor, String>         (x => x.InternalConstructors         , x => x.Text, x => x.Text = "Orig", x => x.Text = "New", InternalConstructor.New);
		[Fact] public void InternalProtected() => Tests.SimpleProperty<Context, InternalProtectedConstructor, String>(x => x.InternalProtectedConstructors, x => x.Text, x => x.Text = "Orig", x => x.Text = "New", InternalProtectedConstructor.New);

		class Context : BaseContext {
			public virtual DbSet<PublicConstructor           > PublicConstructors            { get; set; }
			public virtual DbSet<ProtectedConstructor        > ProtectedConstructors         { get; set; }
			public virtual DbSet<PrivateConstructor          > PrivateConstructors           { get; set; }
			public virtual DbSet<InternalConstructor         > InternalConstructors          { get; set; }
			public virtual DbSet<InternalProtectedConstructor> InternalProtectedConstructors { get; set; }
		}

		public class Base {
			public virtual Int64 Id { get; set; }
			public virtual String Text { get; set; }
		}

		public class PublicConstructor            : Base { public PublicConstructor                       () { } public static PublicConstructor            New() => new PublicConstructor           (); }
		public class ProtectedConstructor         : Base { protected ProtectedConstructor                 () { } public static ProtectedConstructor         New() => new ProtectedConstructor        (); }
		public class PrivateConstructor           : Base { private PrivateConstructor                     () { } public static PrivateConstructor           New() => new PrivateConstructor          (); }
		public class InternalConstructor          : Base { internal InternalConstructor                   () { } public static InternalConstructor          New() => new InternalConstructor         (); }
		public class InternalProtectedConstructor : Base { internal protected InternalProtectedConstructor() { } public static InternalProtectedConstructor New() => new InternalProtectedConstructor(); }
	}
}
