#if EF_CORE

using System;
using Microsoft.EntityFrameworkCore;
namespace EntityFrameworkCore.TypedOriginalValues {

#else

using System.Data.Entity;
namespace EntityFramework.TypedOriginalValues {

#endif

	public static class Extensions {
		[Obsolete("This extension is changing to `GetOriginal`. It will be removed in a future release.")]
		public static TEntity GetOriginalValues<TEntity>(this DbContext context, TEntity entity) where TEntity : class => context.GetOriginal(entity);

		public static TEntity GetOriginal<TEntity>(this DbContext context, TEntity entity) where TEntity : class {
			return OriginalValuesWrapper<TEntity>.Create(context.Entry(entity));
		}
	}
}
