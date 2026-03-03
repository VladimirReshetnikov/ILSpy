// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Concurrent;
using System.IO;

namespace ICSharpCode.ILSpyX.Search
{
	using ICSharpCode.Decompiler.TypeSystem;
	using ICSharpCode.ILSpyX.Abstractions;

	/// <summary>
	/// Base class for search strategies that operate on decompiler entities (<see cref="IEntity"/>).
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class centralizes two filters that are shared by member-, literal-, and token-based searches:
	/// API-level visibility filtering (based on <see cref="ApiVisibility"/>) and optional scope narrowing through
	/// <c>inassembly:</c>/<c>innamespace:</c> query prefixes.
	/// </para>
	/// <para>
	/// Concrete strategies are responsible for traversing candidate entities, then calling
	/// <see cref="CheckVisibility"/>, <see cref="IsInNamespaceOrAssembly"/>, and <see cref="OnFoundResult(IEntity)"/>
	/// in that order before emitting matches.
	/// </para>
	/// </remarks>
	public abstract class AbstractEntitySearchStrategy : AbstractSearchStrategy
	{
		protected readonly ILanguage language;
		protected readonly ApiVisibility apiVisibility;

		/// <summary>
		/// Initializes a strategy that searches entities using language-aware visibility checks.
		/// </summary>
		/// <param name="language">
		/// Active language service used for <see cref="ApiVisibility.PublicAndInternal"/> filtering via
		/// <see cref="ILanguage.ShowMember(IEntity)"/>.
		/// </param>
		/// <param name="apiVisibility">Visibility policy configured by the host UI.</param>
		/// <param name="searchRequest">Parsed query options and collaborator services.</param>
		/// <param name="resultQueue">Concurrent queue receiving search results.</param>
		protected AbstractEntitySearchStrategy(ILanguage language, ApiVisibility apiVisibility,
			SearchRequest searchRequest, IProducerConsumerCollection<SearchResult> resultQueue)
			: base(searchRequest, resultQueue)
		{
			this.language = language;
			this.apiVisibility = apiVisibility;
		}

		/// <summary>
		/// Determines whether an entity and all of its declaring types satisfy the configured visibility filter.
		/// </summary>
		/// <param name="entity">Entity to validate. May be <see langword="null"/>.</param>
		/// <returns>
		/// <see langword="true"/> when <paramref name="entity"/> is visible under the current
		/// <see cref="ApiVisibility"/> policy; otherwise <see langword="false"/>.
		/// </returns>
		protected bool CheckVisibility(IEntity? entity)
		{
			if (apiVisibility == ApiVisibility.All)
				return true;

			while (entity != null)
			{
				if (apiVisibility == ApiVisibility.PublicOnly)
				{
					if (!(entity.Accessibility == Accessibility.Public ||
						entity.Accessibility == Accessibility.Protected ||
						entity.Accessibility == Accessibility.ProtectedOrInternal))
						return false;
				}
				else if (apiVisibility == ApiVisibility.PublicAndInternal)
				{
					if (!language.ShowMember(entity))
						return false;
				}
				entity = entity.DeclaringTypeDefinition;
			}

			return true;
		}

		/// <summary>
		/// Applies optional <c>inassembly:</c> and <c>innamespace:</c> query constraints to a candidate entity.
		/// </summary>
		/// <param name="entity">Entity being evaluated for scope restrictions.</param>
		/// <returns>
		/// <see langword="true"/> when the entity is inside the requested namespace and assembly scope (if any);
		/// otherwise <see langword="false"/>.
		/// </returns>
		protected bool IsInNamespaceOrAssembly(IEntity entity)
		{
			if (searchRequest.InAssembly != null)
			{
				if (entity.ParentModule?.MetadataFile == null ||
					!(Path.GetFileName(entity.ParentModule.MetadataFile.FileName).Contains(searchRequest.InAssembly, StringComparison.OrdinalIgnoreCase)
					|| entity.ParentModule.FullAssemblyName.Contains(searchRequest.InAssembly, StringComparison.OrdinalIgnoreCase)))
				{
					return false;
				}
			}

			if (searchRequest.InNamespace != null)
			{
				if (searchRequest.InNamespace.Length == 0)
				{
					return entity.Namespace.Length == 0;
				}
				else if (!entity.Namespace.Contains(searchRequest.InNamespace, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Converts a matched entity into a <see cref="SearchResult"/> and enqueues it.
		/// </summary>
		/// <param name="entity">Matched entity to publish.</param>
		protected void OnFoundResult(IEntity entity)
		{
			OnFoundResult(searchRequest.SearchResultFactory.Create(entity));
		}
	}
}
