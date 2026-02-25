using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler
{
	public static partial class SRMExtensions
	{
		internal const GenericParameterAttributes AllowByRefLike = (GenericParameterAttributes)0x0020;

		/// <summary>
		/// Gets all <see cref="MethodImplementationHandle"/> rows in the declaring type that use
		/// <paramref name="handle"/> as the method body.
		/// </summary>
		/// <param name="handle">The method whose matching <c>.override</c> records should be returned.</param>
		/// <param name="reader">Metadata reader used to inspect the owning type and method implementation table.</param>
		/// <returns>
		/// An immutable array containing every method implementation that points to
		/// <paramref name="handle"/> as <c>MethodBody</c>.
		/// </returns>
		public static ImmutableArray<MethodImplementationHandle> GetMethodImplementations(
			this MethodDefinitionHandle handle, MetadataReader reader)
		{
			var resultBuilder = ImmutableArray.CreateBuilder<MethodImplementationHandle>();
			var typeDefinition = reader.GetTypeDefinition(reader.GetMethodDefinition(handle)
				.GetDeclaringType());

			foreach (var methodImplementationHandle in typeDefinition.GetMethodImplementations())
			{
				var methodImplementation = reader.GetMethodImplementation(methodImplementationHandle);
				if (methodImplementation.MethodBody == handle)
				{
					resultBuilder.Add(methodImplementationHandle);
				}
			}

			return resultBuilder.ToImmutable();
		}
	}
}
