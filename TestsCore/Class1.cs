using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

	public class Context : DbContext {
		public virtual DbSet<Person> People { get; set; }
		public virtual DbSet<Thing> Things { get; set; }

		//#if EF_CORE
		//			protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		//				optionsBuilder.UseInMemoryDatabase("Test");
		//			}
		//#endif
	}

	[ComplexType]
	public class Widget {
		public virtual String Text { get; set; }
	}

	public class Person {
		[Key]
		public virtual Int64 Id { get; private set; }
		public virtual Int64 Id2 { get; internal set; }
		public virtual Int64 Id3 { get; protected internal set; }
		//public Int64 Id4                       { get; private set;            }
		//public Int64 Id5                       { get; internal set;           }
		public Int64 Id6 { get; protected set; }
		public String FirstName { get; set; }
		public virtual String LastName { get; set; }
		public virtual DateTime BirthDate { get; set; } = DateTime.UtcNow;
		public virtual Widget Widget { get; set; } = new Widget();
		public virtual ICollection<Thing> Things { get; private set; } = new Collection<Thing>();
	}

	public class Thing {
		[Key]
		public Int64 Id { get; private set; }
		public String Name { get; set; }
		[Required]
		public virtual Person Person { get; set; }
	}

	public class Tests {
		// Simple properties
		// Complex properties [ComplexType]
		// Proxy setters throw
		// Non-virtual properties are set by constructor
		// Virtual properties are lazily evaluated via overridden getter
		// Navigation properties fail
		// Collection properties fail
		// Async versions
		// Setter visibility
		// Parameterless constructor visibilty

		private void SimpleProperty<TEntity, TProperty>(Func<Context, DbSet<TEntity>> dbSet, Func<TEntity, TProperty> property, Action<TEntity> originalValue, Action<TEntity> newValue) where TEntity : class, new() {
			using (var context = new Context()) {
				var entity = new TEntity();
				dbSet(context).Add(entity);
				context.SaveChanges();
				try {
					var originalProperty = property(entity);
					newValue(entity);
					var original = context.GetOriginal(entity);
					Assert.Equal(originalProperty, property(original));
				}
				finally {
					dbSet(context).Remove(entity);
				}
			}
		}

		[Fact] public void SimplePropertyString() => SimpleProperty(x => x.People, x => x.FirstName, x => x.FirstName = "John", x => x.FirstName = "James");
		[Fact] public void SimplePropertyInt64() => SimpleProperty(x => x.People, x => x.Id2, x => x.Id2 = 42, x => x.Id2 = 1337);
		[Fact] public void SimplePropertyDateTime() => SimpleProperty(x => x.People, x => x.BirthDate, x => x.BirthDate = new DateTime(1986, 1, 1), x => x.BirthDate = new DateTime(2001, 12, 24));
	}
}
