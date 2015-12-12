using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using EntityFramework.TypedOriginalValues;

namespace Testing {
	public class Program {
		public class Context : DbContext {
			public virtual DbSet<Person> People { get; set; }
			public virtual DbSet<Thing> Things { get; set; }
		}

		public class Person {
			public virtual Int64 Id { get; private set; }
			public String FirstName { get; set; }
			public virtual String LastName { get; set; }
			public virtual ICollection<Thing> Things { get; private set; } = new Collection<Thing>();
		}

		public class Thing {
			public Int64 Id { get; private set; }
			public String Name { get; set; }
			[Required]
			public virtual Person Person { get; set; }
		}

		//public class PersonProxy : Person {
		//	private readonly DbPropertyValues originalValues;
		//	public PersonProxy(DbPropertyValues originalValues) {
		//		this.originalValues = originalValues;
		//		FirstName = originalValues.GetValue<String>(nameof(FirstName));
		//	}

		//	public override String LastName {
		//		get { return originalValues.GetValue<String>(nameof(LastName)); }
		//		set { throw new NotImplementedException(); }
		//	}
		//}

		static void Main(String[] args) {
			using (var context = new Context()) {
				if (context.Database.Delete())
					context.Database.Create();
				context.People.Add(new Person { FirstName = "Nick", LastName = "Strupat", Things = { new Thing { Name = "Computer" } } });
				context.SaveChanges();
			}
			using (var context = new Context()) {
				var nick = context.People.First();
				nick.FirstName = "Ned";
				nick.LastName = "Sputnik";
				var og = context.GetOriginalValues(nick);
				var what = context.Entry(nick).OriginalValues;
			}
		}
	}
}
