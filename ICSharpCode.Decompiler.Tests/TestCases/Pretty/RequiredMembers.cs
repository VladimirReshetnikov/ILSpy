using System;
using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class RequiredMembers
	{
		private interface IMarker
		{
		}

		private interface ILeft
		{
		}

		private interface IRight
		{
		}

		private struct MarkerStruct : IMarker
		{
			public int Value { get; set; }
		}

		private sealed class MarkerClass : IMarker
		{
			public int Value { get; set; }
		}

		private struct DualStruct : ILeft, IRight
		{
			public int Value;
		}

		private sealed class DualClass : ILeft, IRight
		{
		}

		private sealed class ConditionalData
		{
			public required IMarker Item { get; init; }
		}

		private sealed class ConditionalListData
		{
			public required IReadOnlyList<IMarker> Items { get; init; }
		}

		private sealed class MutableData
		{
			public IMarker Item { get; set; }
		}

		internal class Data
		{
			public required string Y;

			public required int X { get; set; }
		}

		internal class Prompt
		{
			public required uint DefaultIndex { get; init; }

			public required string IconName { get; init; }
		}

		private static int? Icon => 1;

		private static Prompt Build(int defaultIndex)
		{
			return new Prompt {
				DefaultIndex = (uint)defaultIndex,
				IconName = Icon?.ToString()
			};
		}

		private static void Use(Data d)
		{
		}

		private static int Select(ILeft item)
		{
			return 1;
		}

		private static int Select(IRight item)
		{
			return 2;
		}

		private static int SelectConditional(bool condition, DualStruct item)
		{
			return Select(condition ? ((ILeft)item) : new DualClass());
		}

		private static int SelectBoxed(DualStruct item)
		{
			return Select((ILeft)item);
		}

		private static List<TResult> Map<TSource, TResult>(List<TSource> items, Func<TSource, TResult> selector)
		{
			return items.ConvertAll<TResult>(selector.Invoke);
		}

		private static List<IMarker> ConsumeMarkers(List<IMarker> items)
		{
			return items;
		}

		private static TResult Project<TSource, TResult>(TSource item, Func<TSource, TResult> selector)
		{
			return selector(item);
		}

		private static List<IMarker> PreserveSelectedGenericResult<T>(List<T> items) where T : IMarker
		{
			return ConsumeMarkers(Map<T, IMarker>(items, (T item) => item));
		}

		private static IMarker PreserveAnonymousSource(MarkerClass item)
		{
			return Project(new {
				Item = item
			}, value => (IMarker)value.Item);
		}

		private static ConditionalData CreateConditional(bool condition, MarkerStruct marker, int value)
		{
			return new ConditionalData {
				Item = (condition ? marker : new MarkerClass {
					Value = value
				})
			};
		}

		private static ConditionalData CreateConditionalOrThrow(bool condition, IMarker item)
		{
			return new ConditionalData {
				Item = (condition ? item : throw new ArgumentNullException("item"))
			};
		}

		private static ConditionalListData CreateConditionalList(bool condition, IMarker item)
		{
			return new ConditionalListData {
				Items = (condition ? new IMarker[1] { item } : new List<IMarker>())
			};
		}

		private static void SetNullGuardedValue(MutableData data, IMarker item)
		{
			data.Item = item ?? throw new ArgumentNullException("item");
		}

		private static void SetThroughNullGuardedReceiver(MutableData data, IMarker item)
		{
			(data ?? throw new ArgumentNullException("data")).Item = item;
		}

		private static Data CreateThenReassign(int x, string y)
		{
#if OPT
			Data obj = new Data {
				X = x,
				Y = y
			};
			obj.X = x;
			return obj;
#else
			Data data = new Data {
				X = x,
				Y = y
			};
			data.X = x;
			return data;
#endif
		}

		private static Data Create()
		{
#if OPT
			Data obj = new Data {
				X = 5,
				Y = "hi"
			};
			Use(obj);
			return obj;
#else
			Data data = new Data {
				X = 5,
				Y = "hi"
			};
			Use(data);
			return data;
#endif
		}
	}
}
