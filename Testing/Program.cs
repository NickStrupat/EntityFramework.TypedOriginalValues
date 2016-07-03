using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using EntityFrameworkCore.TypedOriginalValues;
#else
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using EntityFramework.TypedOriginalValues;
#endif

namespace Testing {
	public class Program {
		public class Context : DbContext {
			public virtual DbSet<Person> People { get; set; }
			public virtual DbSet<Thing> Things { get; set; }

#if EF_CORE
			protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
				optionsBuilder.UseSqlServer(
					"Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Testing.Program+Context;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
			}
#endif
		}

#if !EF_CORE
		[ComplexType]
		public class Widget {
			public virtual String Text { get; set; }
		}
#endif

		public class Person {
			public virtual Int64 Id { get; private set; }
			public virtual Int64 Id2 { get; internal set; }
			public virtual Int64 Id3 { get; protected internal set; }
			//public Int64 Id4 { get; private set; }
			//public Int64 Id5 { get; internal set; }
			public Int64 Id6 { get; protected set; }
			public String FirstName { get; set; }
			public virtual String LastName { get; set; }
#if !EF_CORE
			public virtual Widget Widget { get; private set; } = new Widget();
#endif
			public virtual ICollection<Thing> Things { get; private set; } = new Collection<Thing>();
		}

		public class Thing {
			public Int64 Id { get; private set; }
			public String Name { get; set; }
			[Required]
			public virtual Person Person { get; set; }
		}

		[NotMapped]
#if EF_CORE
		public class PersonProxy : Person {
			private readonly EntityEntry entityEntry;
			public PersonProxy(EntityEntry entityEntry) {
				this.entityEntry = entityEntry;
				Id6 = (Int64) entityEntry.Property(nameof(Id6)).OriginalValue;
				FirstName = (String) entityEntry.Property(nameof(FirstName)).OriginalValue;
			}

			public override String LastName {
				get { return (String) entityEntry.Property(nameof(LastName)).OriginalValue; }
				set { throw new NotImplementedException(); }
			}
		}
#else
		public class PersonProxy : Person {
			private readonly DbPropertyValues originalValues;
			public PersonProxy(DbPropertyValues originalValues) {
				this.originalValues = originalValues;
				FirstName = originalValues.GetValue<String>(nameof(FirstName));
			}

			public override String LastName {
				get { return originalValues.GetValue<String>(nameof(LastName)); }
				set { throw new NotImplementedException(); }
			}
		}
#endif

		static void Main(String[] args) {
			using (var context = new Context()) {
#if EF_CORE
				//if (context.Database.EnsureDeleted())
				//	context.Database.EnsureCreated();
#else
				if (context.Database.Delete())
					context.Database.Create();
#endif
				context.People.Add(new Person { FirstName = "Nick", LastName = "Strupat", Id2 = 42, Id3 = 1337, Things = { new Thing { Name = "Computer" } } });
				context.SaveChanges();
			}
			using (var context = new Context()) {
				var nick = context.People.First();
				nick.FirstName = "Ned";
				nick.LastName = "Sputnik";
				nick.Id2 = 32;
				nick.Id3 = 4321;
				//var og = context.GetOriginal(nick);

				var thing = new Thing();
				thing.Person = nick;
				context.Things.Add(thing);

				var fdsa = "aaaaaaaaaaaaaaaaaaaaa";
#if EF_CORE
				//var wsf = context.Entry(nick).Property(nameof(Person.Things)).OriginalValue;
				//var what = (string)context.Entry(nick).Property(nameof(Person.Things)).OriginalValue;
				//var wid = context.Entry(nick).Property(x => x.Widget).OriginalValue;
#else
				//var asdfa = context.Entry(nick).OriginalValues;
				//var what = context.Entry(nick).OriginalValues.GetValue<String>(nameof(Person.FirstName));
				DbEntityEntry dbee = context.Entry(nick);
				DbEntityEntry dbeet = context.Entry(thing);
				var wid = (Widget) context.Entry(nick).Property(nameof(Person.Widget)).OriginalValue;
				//var things = context.Entry(nick).Collection	()
#endif
			}
		}
	}
}
