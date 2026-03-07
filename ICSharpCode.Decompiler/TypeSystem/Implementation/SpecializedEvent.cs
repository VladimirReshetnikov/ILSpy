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

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	/// <summary>
	/// Represents an <see cref="IEvent"/> wrapper that applies substitutions to event type and accessor signatures.
	/// </summary>
	/// <remarks>
	/// Accessor wrappers are created lazily to avoid unnecessary allocations when only event metadata flags are queried.
	/// </remarks>
	public class SpecializedEvent : SpecializedMember, IEvent
	{
		/// <summary>
		/// Creates a specialized event wrapper when substitution affects the declaring generic context.
		/// </summary>
		/// <param name="ev">The unspecialized event definition.</param>
		/// <param name="substitution">Substitution to apply.</param>
		/// <returns>
		/// <paramref name="ev"/> when no specialization is required; otherwise a specialized wrapper.
		/// </returns>
		public static IEvent Create(IEvent ev, TypeParameterSubstitution substitution)
		{
			if (TypeParameterSubstitution.Identity.Equals(substitution)
				|| ev.DeclaringType.TypeParameterCount == 0)
			{
				return ev;
			}
			if (substitution.MethodTypeArguments != null && substitution.MethodTypeArguments.Count > 0)
				substitution = new TypeParameterSubstitution(substitution.ClassTypeArguments, EmptyList<IType>.Instance);
			return new SpecializedEvent(ev, substitution);
		}

		readonly IEvent eventDefinition;

		/// <summary>
		/// Initializes a specialized event wrapper.
		/// </summary>
		/// <param name="eventDefinition">The wrapped event definition.</param>
		/// <param name="substitution">Substitution to apply to event type and accessors.</param>
		public SpecializedEvent(IEvent eventDefinition, TypeParameterSubstitution substitution)
			: base(eventDefinition)
		{
			this.eventDefinition = eventDefinition;
			AddSubstitution(substitution);
		}

		public bool CanAdd {
			get { return eventDefinition.CanAdd; }
		}

		public bool CanRemove {
			get { return eventDefinition.CanRemove; }
		}

		public bool CanInvoke {
			get { return eventDefinition.CanInvoke; }
		}

		IMethod addAccessor, removeAccessor, invokeAccessor;

		public IMethod AddAccessor {
			get { return WrapAccessor(ref this.addAccessor, eventDefinition.AddAccessor); }
		}

		public IMethod RemoveAccessor {
			get { return WrapAccessor(ref this.removeAccessor, eventDefinition.RemoveAccessor); }
		}

		public IMethod InvokeAccessor {
			get { return WrapAccessor(ref this.invokeAccessor, eventDefinition.InvokeAccessor); }
		}
	}
}
