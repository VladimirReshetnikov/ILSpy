// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	public struct Maybe<T>
	{
		public T Value;
		public bool HasValue;
	}

	public static class MaybeExtensions
	{
		public static Maybe<TResult> Select<T, TResult>(this Maybe<T> a, Func<T, TResult> fn)
		{
			return default(Maybe<TResult>);
		}

		public static Maybe<T> Where<T>(this Maybe<T> a, Func<T, bool> predicate)
		{
			return default(Maybe<T>);
		}
	}

	public class QueryExpressions
	{
		public interface QueryNode : IEnumerable<QueryNode>, IEnumerable
		{
			QueryNode this[string name] { get; }

			string Text { get; }
		}

		public class HbmParam
		{
			public string Name { get; set; }
			public string[] Text { get; set; }
		}

		public class Customer
		{
			public int CustomerID;
			public IEnumerable<Order> Orders;
			public string Name;
			public string Country;
			public string City;
		}

		public class Order
		{
			public int OrderID;
			public DateTime OrderDate;
			public Customer Customer;
			public int CustomerID;
			public decimal Total;
			public IEnumerable<OrderDetail> Details;
		}

		public class OrderDetail
		{
			public decimal UnitPrice;
			public int Quantity;
		}

		public IEnumerable<Customer> customers;
		public IEnumerable<Order> orders;

		public object MultipleWhere()
		{
			return from c in customers
				   where c.Orders.Count() > 10
				   where c.Country == "DE"
				   select c;
		}

		public object SelectManyFollowedBySelect()
		{
			return from c in customers
				   from o in c.Orders
				   select new { c.Name, o.OrderID, o.Total };
		}

		public object SelectManyFollowedByOrderBy()
		{
			return from c in customers
				   from o in c.Orders
				   orderby o.Total descending
				   select new { c.Name, o.OrderID, o.Total };
		}

		public object MultipleSelectManyFollowedBySelect()
		{
			return from c in customers
				   from o in c.Orders
				   from d in o.Details
				   select new { c.Name, o.OrderID, d.Quantity };
		}

		public object MultipleSelectManyFollowedByLet()
		{
			return from c in customers
				   from o in c.Orders
				   from d in o.Details
				   let x = (decimal)d.Quantity * d.UnitPrice
				   select new { c.Name, o.OrderID, x };
		}

		public object FromLetWhereSelect()
		{
			return from o in orders
				   let t = o.Details.Sum((OrderDetail d) => d.UnitPrice * (decimal)d.Quantity)
				   where t >= 1000m
				   select new {
					   OrderID = o.OrderID,
					   Total = t
				   };
		}

		public IEnumerable<string> LetRangeVariableCollideWithInferredLocal(QueryNode root)
		{
#if EXPECTED_OUTPUT
			QueryNode queryNode = root["items"];
			if (queryNode == null)
			{
				return Enumerable.Empty<string>();
			}
			return from QueryNode queryNode2 in queryNode
				   let text = queryNode2.Text
#else
			QueryNode items = root["items"];
			if (items == null)
			{
				return Enumerable.Empty<string>();
			}
			return from QueryNode queryNode in items
				   let text = queryNode.Text
#endif
				   where text != null
				   select text;
		}

		public object MultipleLet()
		{
			return from a in customers
				   let b = a.Country
				   let c = a.Name
				   select b + c;
		}

		public object HibernateApplyGeneratorQuery()
		{
			return (from pi in customers.GetType().GetProperties()
					let pname = pi.Name
					let pvalue = pi.GetValue(customers, null)
					select new HbmParam {
						Name = pname,
						Text = new string[1] { (pvalue == null) ? "null" : pvalue.ToString() }
					}).ToArray();
		}

		public object Join()
		{
			return from c in customers
				   join o in orders on c.CustomerID equals o.CustomerID
				   select new { c.Name, o.OrderDate, o.Total };
		}

		public object JoinInto()
		{
			return from c in customers
				   join o in orders on c.CustomerID equals o.CustomerID into co
				   let n = co.Count()
				   where n >= 10
				   select new {
					   Name = c.Name,
					   OrderCount = n
				   };
		}

		public object OrderBy()
		{
			return from o in orders
				   orderby o.Customer.Name, o.Total descending
				   select o;
		}

		public object GroupBy()
		{
			return from c in customers
				   group c.Name by c.Country;
		}

		public object ExplicitType()
		{
			return from Customer c in customers
				   where c.City == "London"
				   select c;
		}

		public object QueryContinuation()
		{
			return from c in customers
				   group c by c.Country into g
				   select new {
					   Country = g.Key,
					   CustCount = g.Count()
				   };
		}

		public object Issue437(bool[] bools)
		{
			return from x in bools
				   where x
				   select (x);
		}

		public object SelectIntoContinuation()
		{
			return from c in customers
				   select c.Name into n
				   where n.Length > 3
				   select n.ToUpper();
		}

		private async Task<int> GetOrderCountAsync(Customer c)
		{
			await Task.Delay(1);
			return c.Orders.Count();
		}

		public IEnumerable<Task<int>> AsyncLambdaSelectStaysMethodCall()
		{
			return customers.Where((Customer c) => c.Country == "DE").Select(async (Customer c) => await GetOrderCountAsync(c));
		}

#if CS60
		private List<string> Issue2545(List<string> arglist)
		{
			return arglist?.OrderByDescending((string f) => f.Length).ThenBy((string f) => f.ToLower()).ToList();
		}
#endif

		public static IEnumerable<char> Issue1310a(bool test)
		{
#if ROSLYN && OPT
			IEnumerable<char> obj = (test ? (from c in Enumerable.Range(0, 255)
											 where char.IsLetter((char)c)
											 select (char)c) : (from c in Enumerable.Range(0, 255)
																where char.IsDigit((char)c)
																select (char)c));
			return obj.Concat(obj);
#else
			IEnumerable<char> enumerable = (test ? (from c in Enumerable.Range(0, 255)
													where char.IsLetter((char)c)
													select (char)c) : (from c in Enumerable.Range(0, 255)
																	   where char.IsDigit((char)c)
																	   select (char)c));
			return enumerable.Concat(enumerable);
#endif
		}

		public static IEnumerable<object> MembersMatching(IEnumerable<IEnumerable<object>> source, object other)
		{
			return from t in source
				   from m in t
				   where m is string || m is int
				   where !m.Equals(other)
				   select m;
		}

		public static Maybe<TB> Cast<TA, TB>(Maybe<TA> a) where TB : class
		{
			return from m in a
				   let t = m as TB
				   where t != null
				   select t;
		}
	}
}
