using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

	public class BaseContext : DbContext {
#if EF_CORE
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
			base.OnConfiguring(optionsBuilder);
			//var dbName = typeof(Context).FullName;
			//optionsBuilder.UseSqlServer($"Data Source=(localdb)\\mssqllocaldb;Initial Catalog={dbName};Integrated Security=True;;");
			optionsBuilder.UseInMemoryDatabase();
		}
#endif
	}

	public class Context : BaseContext {
		public virtual DbSet<Person> People { get; set; }
		public virtual DbSet<Thing> Things { get; set; }
		public virtual DbSet<Creature> Creatures { get; set; }
	}

	[ComplexType]
	public class Widget {
		public virtual String Text { get; set; }
	}

	public class Person {
		[Key]
		public virtual Int64 Id                  { get; private set;            }
		
		public virtual Int64 Number              { get; set;                    }
		public virtual Int64 Number2             { get; internal set;           }
		public virtual Int64 Number3             { get; protected internal set; }
		//public         Int64 Number4             { get; private set;            }
		//public         Int64 Number5             { get; internal set;           }
		public         Int64 Number6             { get; protected set;          }

		public         DateTime? Birth           { get; set;                    }
		public         String FirstName          { get; set;                    }

		public virtual String LastName           { get; set;                    }
		public virtual DateTime? Death           { get; set;                    }

#if !EF_CORE
		public virtual Widget Widget             { get; set;                    } = new Widget();
#endif
		public virtual ICollection<Thing> Things { get; private set;            } = new Collection<Thing>();
	}

	public class Thing {
		[Key]
		public virtual Int64 Id { get; private set; }
		public String Name { get; set; }
		[Required]
		public virtual Person Person { get; set; }
	}

	public class Creature {
		[Key]
		public virtual Int64 Id { get; private set; }

		[Required]
		public virtual String Name { get; set; }

		[NotMapped]
		public virtual DateTime When { get; set; }

		[NotMapped]
		public virtual List<Int32> Numbers { get; } = new List<Int32>();
	}

	public class Tests {
		// TODO:
		// Virtual properties are lazily evaluated via overridden getter
		// Async versions
		// Setter visibility


		// Simple properties
		public static void SimpleProperty<TDbContext, TEntity, TProperty>(Func<TDbContext, DbSet<TEntity>> dbSet, Func<TEntity, TProperty> property, Action<TEntity> setOriginalValue, Action<TEntity> setNewValue, Func<TEntity> factory, IEqualityComparer<TProperty> equalityComparer = null) where TEntity : class where TDbContext : DbContext, new() {
			using (var context = new TDbContext()) {
				var entity = factory();
				setOriginalValue(entity);
				dbSet(context).Add(entity);
				context.SaveChanges();
				try {
					var originalProperty = property(entity);
					setNewValue(entity);
					var original = context.GetOriginal(entity);
					if (equalityComparer == null)
						Assert.Equal(originalProperty, property(original));
					else
						Assert.Equal(originalProperty, property(original), equalityComparer);
				}
				finally {
					dbSet(context).Remove(entity);
					context.SaveChanges();
				}
			}
		}

		private static void SimpleProperty<TEntity, TProperty>(Func<Context, DbSet<TEntity>> dbSet,
		                                                       Func<TEntity, TProperty> property,
		                                                       Action<TEntity> setOriginalValue,
		                                                       Action<TEntity> setNewValue,
		                                                       IEqualityComparer<TProperty> equalityComparer = null)
		where TEntity : class, new() {
			SimpleProperty(dbSet, property, setOriginalValue, setNewValue, () => new TEntity(), equalityComparer);
		}

		[Fact] public void SimplePropertyString() => SimpleProperty(x => x.People, x => x.FirstName, x => x.FirstName = "John", x => x.FirstName = "James");
		[Fact] public void SimplePropertyInt64() => SimpleProperty(x => x.People, x => x.Number2, x => x.Number2 = 42, x => x.Number2 = 1337);
		[Fact] public void SimplePropertyDateTime() => SimpleProperty(x => x.People, x => x.Birth, x => x.Birth = new DateTime(1986, 1, 1), x => x.Birth = new DateTime(2001, 12, 24));
		[Fact] public void SimplePropertyVirtualString() => SimpleProperty(x => x.People, x => x.LastName, x => x.LastName = "Smith", x => x.LastName = "Simpson");
		[Fact] public void SimplePropertyVirtualInt64() => SimpleProperty(x => x.People, x => x.Number2, x => x.Number2 = 123456, x => x.Number2 = 654321);
		[Fact] public void SimplePropertyVirtualDateTime() => SimpleProperty(x => x.People, x => x.Birth, x => x.Birth = new DateTime(2020, 4, 20), x => x.Birth = new DateTime(1969, 6, 25));

#if !NET_4_0
		// Async versions
		private async Task SimplePropertyAsync<TEntity, TProperty>(Func<Context, DbSet<TEntity>> dbSet, Func<TEntity, TProperty> property, Action<TEntity> setOriginalValue, Action<TEntity> setNewValue, IEqualityComparer<TProperty> equalityComparer = null) where TEntity : class, new() {
			using (var context = new Context()) {
				var entity = new TEntity();
				setOriginalValue(entity);
				dbSet(context).Add(entity);
				await context.SaveChangesAsync();
				try {
					var originalProperty = property(entity);
					setNewValue(entity);
					var original = context.GetOriginal(entity);
					if (equalityComparer == null)
						Assert.Equal(originalProperty, property(original));
					else
						Assert.Equal(originalProperty, property(original), equalityComparer);
				}
				finally {
					dbSet(context).Remove(entity);
					await context.SaveChangesAsync();
				}
			}
		}
		
		[Fact] public Task SimplePropertyStringAsync() => SimplePropertyAsync(x => x.People, x => x.FirstName, x => x.FirstName = "John", x => x.FirstName = "James");
		[Fact] public Task SimplePropertyInt64Async() => SimplePropertyAsync(x => x.People, x => x.Number2, x => x.Number2 = 42, x => x.Number2 = 1337);
		[Fact] public Task SimplePropertyDateTimeAsync() => SimplePropertyAsync(x => x.People, x => x.Birth, x => x.Birth = new DateTime(1986, 1, 1), x => x.Birth = new DateTime(2001, 12, 24));
		[Fact] public Task SimplePropertyVirtualStringAsync() => SimplePropertyAsync(x => x.People, x => x.LastName, x => x.LastName = "Smith", x => x.LastName = "Simpson");
		[Fact] public Task SimplePropertyVirtualInt64Async() => SimplePropertyAsync(x => x.People, x => x.Number2, x => x.Number2 = 123456, x => x.Number2 = 654321);
		[Fact] public Task SimplePropertyVirtualDateTimeAsync() => SimplePropertyAsync(x => x.People, x => x.Birth, x => x.Birth = new DateTime(2020, 4, 20), x => x.Birth = new DateTime(1969, 6, 25));
#endif


#if !EF_CORE
		// Complex properties [ComplexType]
		// TODO: Watch for the addition of ComplexType to EFCore
		private void ComplexProperty<TEntity, TProperty>(Func<Context, DbSet<TEntity>> dbSet, Func<TEntity, TProperty> property, Action<TEntity> setOriginalValue, Action<TEntity> setNewValue, IEqualityComparer<TProperty> comparer)
		where TEntity : class, new() {
			SimpleProperty(dbSet, property, setOriginalValue, setNewValue, comparer);
		}

		class ComplexTypeComparer<TComplexTypeEntity> : IEqualityComparer<TComplexTypeEntity> {
			private readonly Func<TComplexTypeEntity, TComplexTypeEntity, Boolean> compare;

			public ComplexTypeComparer(Func<TComplexTypeEntity, TComplexTypeEntity, Boolean> compare) {
				this.compare = compare;
			}

			public Boolean Equals(TComplexTypeEntity x, TComplexTypeEntity y) => compare(x, y);
			public Int32 GetHashCode(TComplexTypeEntity obj) => obj.GetHashCode();
		}

		static readonly IEqualityComparer<Widget> Comparer = new ComplexTypeComparer<Widget>((x, y) => x.Text.Equals(y.Text));

		[Fact] public void ComplexPropertyWidget() => ComplexProperty(x => x.People, x => x.Widget, x => x.Widget = new Widget { Text = "Orig" }, x => x.Widget = new Widget { Text = "New" }, Comparer);
		[Fact] public void ComplexPropertyWidgetProperty() => SimpleProperty(x => x.People, x => x.Widget.Text, x => x.Widget.Text = "Orig", x => x.Widget.Text = "New");

#if !NET_4_0
		// Async versions
		private Task ComplexPropertyAsync<TEntity, TProperty>(Func<Context, DbSet<TEntity>> dbSet, Func<TEntity, TProperty> property, Action<TEntity> setOriginalValue, Action<TEntity> setNewValue, IEqualityComparer<TProperty> comparer)
		where TEntity : class, new() {
			return SimplePropertyAsync(dbSet, property, setOriginalValue, setNewValue, comparer);
		}

		[Fact] public Task ComplexPropertyWidgetAsync() => ComplexPropertyAsync(x => x.People, x => x.Widget, x => x.Widget = new Widget { Text = "Orig" }, x => x.Widget = new Widget { Text = "New" }, Comparer);
		[Fact] public Task ComplexPropertyWidgetPropertyAsync() => SimplePropertyAsync(x => x.People, x => x.Widget.Text, x => x.Widget.Text = "Orig", x => x.Widget.Text = "New");
#endif
#endif


		// Proxy setters throw
		[Fact]
		public void ProxySetterThrowsOnVirtualProperty() {
			using (var context = new Context()) {
				var person = new Person { LastName = "Smith" };
				context.People.Add(person);
				context.SaveChanges();
				var orig = context.GetOriginal(person);
				Assert.Throws<InvalidOperationException>(() => orig.LastName = "Simpson");
			}
		}


		// Non-virtual properties are set by constructor
		[Fact]
		public void NonVirtualPropertyDoesNotThrow() {
			// Non-virtual properties can't be overridden, so there's nothing we can do to control that. This test just makes sure that is true.
			using (var context = new Context()) {
				var person = new Person { FirstName = "John" };
				context.People.Add(person);
				context.SaveChanges();
				var orig = context.GetOriginal(person);
				orig.FirstName = "James";
			}
		}


		// Collection properties fail
		[Fact]
		public void CollectionPropertyThrows() {
			using (var context = new Context()) {
				var person = new Person();
				context.People.Add(person);
				context.SaveChanges();
				var orig = context.GetOriginal(person);
				Assert.Throws<InvalidOperationException>(() => orig.Things.FirstOrDefault());
			}
		}


		// Navigation properties fail
		[Fact]
		public void NavigationPropertyThrows() {
			using (var context = new Context()) {
				var person = new Person();
				context.People.Add(person);
				var thing = new Thing();
				thing.Person = person;
				context.Things.Add(thing);
				context.SaveChanges();
				var orig = context.GetOriginal(thing);
#if !EF_CORE
				Assert.Throws<ArgumentException>(() => orig.Person);
#else
				Assert.Throws<InvalidOperationException>(() => orig.Person);
#endif
			}
		}

		[Fact]
		public void PropertyWithNotMappedAttribute() {
			using (var context = new Context()) {
				var creature = new Creature { Name = "Dog", When = DateTime.UtcNow };
				context.Creatures.Add(creature);
				context.SaveChanges();
				var orig = context.GetOriginal(creature);
			}
		}

		[Fact]
		public void PropertyWithoutSetter() {
			using (var context = new Context()) {
				var creature = new Creature { Name = "Dog", When = DateTime.UtcNow };
				context.Creatures.Add(creature);
				context.SaveChanges();
				var orig = context.GetOriginal(creature);
			}
		}
	}
}
