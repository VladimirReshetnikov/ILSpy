// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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

using System.Collections.Generic;
using System.Text;

using ICSharpCode.Decompiler.TypeSystem.Implementation;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Rewrites type-parameter references to concrete type arguments.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The substitution is split into two independent maps: one for type-definition parameters (<c>`0</c>, <c>`1</c>, ...)
	/// and one for method parameters (<c>``0</c>, <c>``1</c>, ...). A <see langword="null"/> map represents identity for
	/// that category, while an empty list means the category is explicitly mapped but has no available arguments.
	/// </para>
	/// <para>
	/// This type derives from <see cref="TypeVisitor"/>, so callers typically apply it via
	/// <see cref="IType.AcceptVisitor(TypeVisitor)"/>.
	/// </para>
	/// </remarks>
	public class TypeParameterSubstitution : TypeVisitor
	{
		/// <summary>
		/// Identity substitution that leaves all type parameters unchanged.
		/// </summary>
		public static readonly TypeParameterSubstitution Identity = new TypeParameterSubstitution(null, null);

		readonly IReadOnlyList<IType> classTypeArguments;
		readonly IReadOnlyList<IType> methodTypeArguments;

		/// <summary>
		/// Creates a new type parameter substitution.
		/// </summary>
		/// <param name="classTypeArguments">
		/// The type arguments to substitute for class type parameters.
		/// Pass <c>null</c> to keep class type parameters unmodified.
		/// </param>
		/// <param name="methodTypeArguments">
		/// The type arguments to substitute for method type parameters.
		/// Pass <c>null</c> to keep method type parameters unmodified.
		/// </param>
		public TypeParameterSubstitution(IReadOnlyList<IType> classTypeArguments, IReadOnlyList<IType> methodTypeArguments)
		{
			this.classTypeArguments = classTypeArguments;
			this.methodTypeArguments = methodTypeArguments;
		}

		/// <summary>
		/// Gets replacement type arguments for type-definition parameters.
		/// </summary>
		/// <value>
		/// <see langword="null"/> when class/type parameters should remain unchanged; otherwise an index-aligned list where
		/// position <c>i</c> replaces parameter <c>`i</c>.
		/// </value>
		public IReadOnlyList<IType> ClassTypeArguments {
			get { return classTypeArguments; }
		}

		/// <summary>
		/// Gets replacement type arguments for method type parameters.
		/// </summary>
		/// <value>
		/// <see langword="null"/> when method parameters should remain unchanged; otherwise an index-aligned list where
		/// position <c>i</c> replaces parameter <c>``i</c>.
		/// </value>
		public IReadOnlyList<IType> MethodTypeArguments {
			get { return methodTypeArguments; }
		}

		#region Compose
		/// <summary>
		/// Composes two substitutions into one pass.
		/// </summary>
		/// <param name="g">Second substitution applied after <paramref name="f"/>.</param>
		/// <param name="f">First substitution to apply.</param>
		/// <returns>
		/// A substitution equivalent to applying <paramref name="f"/> and then <paramref name="g"/> to every type.
		/// If either argument is effectively identity, the other is returned directly.
		/// </returns>
		/// <remarks>
		/// Functionally, this satisfies <c>t.AcceptVisitor(Compose(g, f)) == t.AcceptVisitor(f).AcceptVisitor(g)</c>
		/// for all <c>t</c>.
		/// </remarks>
		public static TypeParameterSubstitution Compose(TypeParameterSubstitution g, TypeParameterSubstitution f)
		{
			if (g == null)
				return f;
			if (f == null || (f.classTypeArguments == null && f.methodTypeArguments == null))
				return g;
			// The composition is a copy of 'f', with 'g' applied on the array elements.
			// If 'f' has a null list (keeps type parameters unmodified), we have to treat it as
			// the identity function, and thus use the list from 'g'.
			var classTypeArguments = f.classTypeArguments != null ? GetComposedTypeArguments(f.classTypeArguments, g) : g.classTypeArguments;
			var methodTypeArguments = f.methodTypeArguments != null ? GetComposedTypeArguments(f.methodTypeArguments, g) : g.methodTypeArguments;
			return new TypeParameterSubstitution(classTypeArguments, methodTypeArguments);
		}

		static IReadOnlyList<IType> GetComposedTypeArguments(IReadOnlyList<IType> input, TypeParameterSubstitution substitution)
		{
			IType[] result = new IType[input.Count];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = input[i].AcceptVisitor(substitution);
			}
			return result;
		}
		#endregion

		#region Equals and GetHashCode implementation
		/// <summary>
		/// Compares two substitutions after normalizing both argument lists with <paramref name="normalization"/>.
		/// </summary>
		/// <param name="other">The substitution to compare with the current instance.</param>
		/// <param name="normalization">Visitor used to normalize each mapped type before equality checks.</param>
		/// <returns><see langword="true"/> when both maps are equal after normalization; otherwise <see langword="false"/>.</returns>
		public bool Equals(TypeParameterSubstitution other, TypeVisitor normalization)
		{
			if (other == null)
				return false;
			return TypeListEquals(classTypeArguments, other.classTypeArguments, normalization)
				&& TypeListEquals(methodTypeArguments, other.methodTypeArguments, normalization);
		}

		public override bool Equals(object obj)
		{
			TypeParameterSubstitution other = obj as TypeParameterSubstitution;
			if (other == null)
				return false;
			return TypeListEquals(classTypeArguments, other.classTypeArguments)
				&& TypeListEquals(methodTypeArguments, other.methodTypeArguments);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return 1124131 * TypeListHashCode(classTypeArguments) + 1821779 * TypeListHashCode(methodTypeArguments);
			}
		}

		static bool TypeListEquals(IReadOnlyList<IType> a, IReadOnlyList<IType> b)
		{
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++)
			{
				if (!a[i].Equals(b[i]))
					return false;
			}
			return true;
		}

		static bool TypeListEquals(IReadOnlyList<IType> a, IReadOnlyList<IType> b, TypeVisitor normalization)
		{
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++)
			{
				var an = a[i].AcceptVisitor(normalization);
				var bn = b[i].AcceptVisitor(normalization);
				if (!an.Equals(bn))
					return false;
			}
			return true;
		}

		static int TypeListHashCode(IReadOnlyList<IType> obj)
		{
			if (obj == null)
				return 0;
			unchecked
			{
				int hashCode = 1;
				foreach (var element in obj)
				{
					hashCode *= 27;
					hashCode += element.GetHashCode();
				}
				return hashCode;
			}
		}
		#endregion

		/// <summary>
		/// Replaces a type parameter with its mapped argument when available.
		/// </summary>
		/// <param name="type">Type parameter node to rewrite.</param>
		/// <returns>
		/// The mapped argument for the parameter owner/category, <see cref="SpecialType.UnknownType"/> for out-of-range
		/// indexes in an active map, or the original visitor behavior when no map applies.
		/// </returns>
		public override IType VisitTypeParameter(ITypeParameter type)
		{
			int index = type.Index;
			if (classTypeArguments != null && type.OwnerType == SymbolKind.TypeDefinition)
			{
				if (index >= 0 && index < classTypeArguments.Count)
					return classTypeArguments[index];
				else
					return SpecialType.UnknownType;
			}
			else if (methodTypeArguments != null && type.OwnerType == SymbolKind.Method)
			{
				if (index >= 0 && index < methodTypeArguments.Count)
					return methodTypeArguments[index];
				else
					return SpecialType.UnknownType;
			}
			else
			{
				return base.VisitTypeParameter(type);
			}
		}

		/// <summary>
		/// Applies substitution while preserving nullability semantics for annotated type parameters.
		/// </summary>
		/// <param name="type">Annotated type to process.</param>
		/// <returns>
		/// A substituted type where nullable annotations on type parameters are re-applied to the substituted result.
		/// </returns>
		public override IType VisitNullabilityAnnotatedType(NullabilityAnnotatedType type)
		{
			if (type is NullabilityAnnotatedTypeParameter tp)
			{
				if (tp.Nullability == Nullability.Nullable)
				{
					return VisitTypeParameter(tp).ChangeNullability(Nullability.Nullable);
				}
				else
				{
					// T! substituted with T=oblivious string should result in oblivious string
					return VisitTypeParameter(tp);
				}
			}
			else
			{
				return base.VisitNullabilityAnnotatedType(type);
			}
		}

		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append('[');
			bool first = true;
			if (classTypeArguments != null)
			{
				for (int i = 0; i < classTypeArguments.Count; i++)
				{
					if (first)
						first = false;
					else
						b.Append(", ");
					b.Append('`');
					b.Append(i);
					b.Append(" -> ");
					b.Append(classTypeArguments[i].ReflectionName);
				}
				if (classTypeArguments.Count == 0)
				{
					if (first)
						first = false;
					else
						b.Append(", ");
					b.Append("[]");
				}
			}
			if (methodTypeArguments != null)
			{
				for (int i = 0; i < methodTypeArguments.Count; i++)
				{
					if (first)
						first = false;
					else
						b.Append(", ");
					b.Append("``");
					b.Append(i);
					b.Append(" -> ");
					b.Append(methodTypeArguments[i].ReflectionName);
				}
				if (methodTypeArguments.Count == 0)
				{
					if (first)
						first = false;
					else
						b.Append(", ");
					b.Append("[]");
				}
			}
			b.Append(']');
			return b.ToString();
		}
	}
}
