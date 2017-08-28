#if NETSTANDARD2_0

using Microsoft.EntityFrameworkCore;
namespace EntityFrameworkCore.TypedOriginalValues {

#else

using System.Data.Entity;
namespace EntityFramework.TypedOriginalValues {

#endif

	public static class Extensions {
		public static TEntity GetOriginal<TEntity>(this DbContext context, TEntity entity) where TEntity : class {
			return OriginalValuesWrapper<TEntity>.Create(context.Entry(entity));
		}
	}
}
