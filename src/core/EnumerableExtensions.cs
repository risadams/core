// perticula - core - EnumerableExtensions.cs
// 
// Copyright © 2015-2023  Ris Adams - All Rights Reserved
// 
// You may use, distribute and modify this code under the terms of the MIT license
// You should have received a copy of the MIT license with this file. If not, please write to: perticula@risadams.com, or visit : https://github.com/perticula

namespace core;

public static class EnumerableExtensions
{
	/// <summary>
	///   Break the list into multiple groups
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="list">The list.</param>
	/// <param name="num">The number.</param>
	/// <param name="parts">The parts.</param>
	/// <returns>IEnumerable&lt;T&gt;.</returns>
	public static IEnumerable<T> Divide<T>(this IEnumerable<T> list, int num, int parts)
	{
		if (num   < 0) throw new ArgumentOutOfRangeException(nameof(num),   "num must be greater than 0");
		if (parts < 1) throw new ArgumentOutOfRangeException(nameof(parts), "parts must be greater than 1");
		var enumerable = list as IList<T> ?? list.ToList();
		var count      = enumerable.Count;
		var cut        = (int) Math.Ceiling((decimal) count / parts);
		return enumerable.Skip((num - 1) * cut).Take(cut);
	}

	/// <summary>
	///   groups a collection of items into smaller lists of a particular count
	/// </summary>
	/// <typeparam name="TType">The type of the t type.</typeparam>
	/// <param name="value">The value.</param>
	/// <param name="count">The count.</param>
	/// <returns>List&lt;IGrouping&lt;System.Int32, TType&gt;&gt;.</returns>
	public static IEnumerable<IGrouping<int, TType>> GroupByCount<TType>(this IEnumerable<TType> value, int count)
	{
		if (value == null) throw new ArgumentNullException(nameof(value));

		var source = value as TType[] ?? value.ToArray();
		return source.GroupBy(src => source.ToList().IndexOf(src) / count);
	}

	/// <summary>
	///   Returns the enumerable values UNTIL the condition is met. Does NOT return the element where the condition was false.
	/// </summary>
	/// <typeparam name="TSource">The type of the source list.</typeparam>
	/// <param name="source">The source list.</param>
	/// <param name="predicate">The predicate to determine when to stop processing items.</param>
	/// <returns>IEnumerable&lt;TSource&gt;.</returns>
	/// <exception cref="ArgumentNullException">source or predicate </exception>
	public static IEnumerable<TSource> TakeUntil<TSource>(this IEnumerable<TSource>? source, Func<TSource, bool>? predicate)
	{
		if (source    == null) throw new ArgumentNullException(nameof(source));
		if (predicate == null) throw new ArgumentNullException(nameof(predicate));

		return _();

		IEnumerable<TSource> _()
		{
			foreach (var item in source)
			{
				if (predicate(item)) yield break;
				yield return item;
			}
		}
	}

	/// <summary>
	///   Determines whether the enumerable is null or empty.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="value">The value.</param>
	/// <returns><c>true</c> if the value is null or empty; otherwise, <c>false</c>.</returns>
	public static bool IsNullOrEmpty<T>(this IEnumerable<T>? value) => value == null || !value.Any();

	/// <summary>
	///   Takes any singular object and wraps it into a new array containing only that object.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="item">The item.</param>
	/// <returns>IEnumerable&lt;T&gt;.</returns>
	public static IEnumerable<T> ToEnumerable<T>(this T item) => new[] {item};

	/// <summary>
	///   Generates a sequence of typed tuples by combining the items in each list positionally
	///   will return the shortest intersection of the two enumerables as a typed tuple
	/// </summary>
	/// <typeparam name="T1">The type of the first list.</typeparam>
	/// <typeparam name="T2">The type of the second list.</typeparam>
	/// <param name="first">The first list.</param>
	/// <param name="second">The second list.</param>
	/// <returns>IEnumerable&lt;System.ValueTuple&lt;T1, T2&gt;&gt;.</returns>
	public static IEnumerable<(T1 first, T2 second)> Zip<T1, T2>(this IEnumerable<T1>? first, IEnumerable<T2>? second) => first?.Zip(second ?? Array.Empty<T2>(), (x, y) => (x, y)) ?? new List<(T1 first, T2 second)>(0);
}
