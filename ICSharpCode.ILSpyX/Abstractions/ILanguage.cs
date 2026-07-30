// Copyright (c) 2022 Siegfried Pammer
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

using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Output;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.ILSpyX.Abstractions
{
	/// <summary>
	/// Defines the language-specific services used by ILSpyX search and analyzer features.
	/// </summary>
	public interface ILanguage
	{
		/// <summary>
		/// Determines whether a member should be considered visible by this language implementation.
		/// </summary>
		/// <param name="member">The member to evaluate.</param>
		/// <returns><see langword="true"/> if the member should be shown; otherwise, <see langword="false"/>.</returns>
		bool ShowMember(IEntity member);

		/// <summary>
		/// Gets source-to-IL mapping information for a metadata member.
		/// </summary>
		/// <param name="module">The module that contains <paramref name="member"/>.</param>
		/// <param name="member">The metadata handle of the target member.</param>
		/// <returns>Mapping information used to navigate between generated code and metadata.</returns>
		CodeMappingInfo GetCodeMappingInfo(MetadataFile module, EntityHandle member);

		/// <summary>
		/// Formats a metadata entity name according to language rules.
		/// </summary>
		/// <param name="module">The module that provides metadata for <paramref name="handle"/>.</param>
		/// <param name="handle">The handle to a metadata entity.</param>
		/// <param name="fullName">If set, returns a fully qualified name when possible.</param>
		/// <param name="omitGenerics">If set, omits generic arity and type arguments.</param>
		/// <returns>
		/// A language-formatted name for the entity, or <c>null</c> for handle kinds the
		/// language does not recognize. Callers such as the member search strategy tolerate
		/// a <c>null</c> result by skipping the language-specific name match.
		/// </returns>
		string GetEntityName(MetadataFile module, System.Reflection.Metadata.EntityHandle handle, bool fullName, bool omitGenerics);

		/// <summary>
		/// Produces tooltip text for an entity as displayed in search and analysis UI.
		/// </summary>
		/// <param name="entity">The entity to describe.</param>
		/// <returns>Tooltip text for <paramref name="entity"/>.</returns>
		string GetTooltip(IEntity entity);

		/// <summary>
		/// Converts a type into language-specific display text.
		/// </summary>
		/// <param name="type">The type to format.</param>
		/// <param name="conversionFlags">Formatting options that control qualification and syntax details.</param>
		/// <returns>A language-formatted type name.</returns>
		string TypeToString(IType type, ConversionFlags conversionFlags = ConversionFlags.UseFullyQualifiedEntityNames | ConversionFlags.UseFullyQualifiedTypeNames);

		/// <summary>
		/// Converts an entity into language-specific display text.
		/// </summary>
		/// <param name="entity">The entity to format.</param>
		/// <param name="conversionFlags">Formatting options that control qualification and syntax details.</param>
		/// <returns>A language-formatted entity signature.</returns>
		string EntityToString(IEntity entity, ConversionFlags conversionFlags);
	}
}
