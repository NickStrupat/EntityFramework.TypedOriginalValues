using System.Data.Entity;

namespace EntityFramework.TypedOriginalValues {
	public static class Extensions {
		public static TEntity GetOriginalValues<TEntity>(this DbContext context, TEntity entity) where TEntity : class {
			return DbPropertyValuesWrapper<TEntity>.Create(context.Entry(entity).OriginalValues);
		}
	}
}
