using System;
using System.Linq.Expressions;

public static class CastBetweenTypeParameters<TTo>
{
	private static class Cache<T>
	{
		internal static readonly Func<T, TTo> Caster = Get();

		private static Func<T, TTo> Get()
		{
			return ((Expression<Func<T, TTo>>)((T value) => (TTo)(object)value)).Compile();
		}
	}

	public static TTo From<TFrom>(TFrom s)
	{
		return Cache<TFrom>.Caster(s);
	}
}
