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
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// <see cref="TokenWriter"/> implementation that emits C# tokens to an <see cref="ITextOutput"/> while preserving symbol references.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This writer is used by decompilation frontends that want navigable output (for example symbol hyperlinks, local-definition linkage,
	/// and fold regions) instead of plain text only. It inspects AST annotations and roles to decide whether a token should be written with
	/// one of the <see cref="ITextOutput.WriteReference"/> overloads or as ordinary text.
	/// </para>
	/// <para>
	/// Instance members are not thread-safe. The writer maintains mutable traversal state in <c>nodeStack</c> and assumes balanced
	/// <see cref="StartNode"/>/<see cref="EndNode"/> calls from a single traversal.
	/// </para>
	/// </remarks>
	public class TextTokenWriter : TokenWriter
	{
		readonly ITextOutput output;
		readonly DecompilerSettings settings;
		readonly IDecompilerTypeSystem typeSystem;
		readonly Stack<AstNode> nodeStack = new Stack<AstNode>();
		int braceLevelWithinType = -1;
		bool inDocumentationComment = false;
		bool firstUsingDeclaration;
		bool lastUsingDeclaration;

		/// <summary>
		/// Initializes a token writer that forwards formatted output to <paramref name="output"/>.
		/// </summary>
		/// <param name="output">The destination that receives text, symbol links, and fold markers.</param>
		/// <param name="settings">Decompiler settings that control folding behavior and formatting choices.</param>
		/// <param name="typeSystem">Type-system context used to resolve and classify symbols attached to AST nodes.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="output"/>, <paramref name="settings"/>, or <paramref name="typeSystem"/> is <see langword="null"/>.
		/// </exception>
		public TextTokenWriter(ITextOutput output, DecompilerSettings settings, IDecompilerTypeSystem typeSystem)
		{
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			if (typeSystem == null)
				throw new ArgumentNullException(nameof(typeSystem));
			this.output = output;
			this.settings = settings;
			this.typeSystem = typeSystem;
		}

		/// <summary>
		/// Writes an identifier token and attaches the most specific available reference metadata.
		/// </summary>
		/// <param name="identifier">The identifier token to emit.</param>
		/// <remarks>
		/// Resolution order is definition symbol, member reference symbol, local definition, then local reference.
		/// If no reference information is available, the escaped identifier text is written as plain text.
		/// </remarks>
		public override void WriteIdentifier(Identifier identifier)
		{
			if (identifier.IsVerbatim || CSharpOutputVisitor.IsKeyword(identifier.Name, identifier))
			{
				output.Write('@');
			}

			var definition = GetCurrentDefinition();
			string name = TextWriterTokenWriter.EscapeIdentifier(identifier.Name);
			switch (definition)
			{
				case IType t:
					output.WriteReference(t, name, true);
					return;
				case IMember m:
					output.WriteReference(m, name, true);
					return;
			}

			var member = GetCurrentMemberReference();
			switch (member)
			{
				case IType t:
					output.WriteReference(t, name, false);
					return;
				case IMember m:
					output.WriteReference(m, name, false);
					return;
			}

			var localDefinition = GetCurrentLocalDefinition(identifier);
			if (localDefinition != null)
			{
				output.WriteLocalReference(name, localDefinition, isDefinition: true);
				return;
			}

			var localRef = GetCurrentLocalReference();
			if (localRef != null)
			{
				output.WriteLocalReference(name, localRef);
				return;
			}

			if (firstUsingDeclaration && !lastUsingDeclaration)
			{
				output.MarkFoldStart(defaultCollapsed: !settings.ExpandUsingDeclarations);
				firstUsingDeclaration = false;
			}

			output.Write(name);
		}

		/// <summary>
		/// Gets the symbol referenced by the current node when it represents a member usage position.
		/// </summary>
		/// <returns>
		/// The referenced symbol, or <see langword="null"/> when the current node does not represent a navigable member reference.
		/// </returns>
		ISymbol GetCurrentMemberReference()
		{
			AstNode node = nodeStack.Peek();
			var symbol = node.GetSymbol();
			if (symbol == null && node.Role == Roles.TargetExpression && node.Parent is InvocationExpression)
			{
				symbol = node.Parent.GetSymbol();
			}
			if (symbol != null && node.Role == Roles.Type && node.Parent is ObjectCreateExpression)
			{
				var ctorSymbol = node.Parent.GetSymbol();
				if (ctorSymbol != null)
					symbol = ctorSymbol;
			}

			if (node is IdentifierExpression && node.Role == Roles.TargetExpression && node.Parent is InvocationExpression && symbol is IMember member)
			{
				var declaringType = member.DeclaringType;
				if (declaringType != null && declaringType.Kind == TypeKind.Delegate)
					return null;
			}
			return FilterMember(symbol);
		}

		/// <summary>
		/// Removes symbols that should not be emitted as navigable member links.
		/// </summary>
		/// <param name="symbol">The symbol candidate.</param>
		/// <returns>
		/// <paramref name="symbol"/> when it should be linked; otherwise <see langword="null"/>.
		/// </returns>
		ISymbol FilterMember(ISymbol symbol)
		{
			if (symbol == null)
				return null;

			if (symbol is LocalFunctionMethod)
				return null;

			return symbol;
		}

		/// <summary>
		/// Gets the local-reference identity associated with the current node.
		/// </summary>
		/// <returns>
		/// The local identity object used with <see cref="ITextOutput.WriteLocalReference"/>, or <see langword="null"/> when none applies.
		/// </returns>
		object GetCurrentLocalReference()
		{
			AstNode node = nodeStack.Peek();
			ILVariable variable = node.Annotation<ILVariableResolveResult>()?.Variable;
			if (variable != null)
				return variable;

			var letClauseVariable = node.Annotation<CSharp.Transforms.LetIdentifierAnnotation>();
			if (letClauseVariable != null)
				return letClauseVariable;

			if (node is GotoStatement gotoStatement)
			{
				var method = nodeStack.Select(nd => nd.GetSymbol() as IMethod).FirstOrDefault(mr => mr != null);
				if (method != null)
					return method + gotoStatement.Label;
			}

			if (node.Role == Roles.TargetExpression && node.Parent is InvocationExpression)
			{
				var symbol = node.Parent.GetSymbol();
				if (symbol is LocalFunctionMethod)
					return symbol;
			}

			return null;
		}

		/// <summary>
		/// Gets the local-definition identity introduced by the current identifier.
		/// </summary>
		/// <param name="id">The identifier currently being emitted.</param>
		/// <returns>
		/// An identity object that should be marked as a local definition, or <see langword="null"/> when the identifier is not a definition site.
		/// </returns>
		object GetCurrentLocalDefinition(Identifier id)
		{
			AstNode node = nodeStack.Peek();
			if (node is Identifier && node.Parent != null)
				node = node.Parent;

			if (node is ParameterDeclaration || node is VariableInitializer || node is CatchClause || node is VariableDesignation)
			{
				var variable = node.Annotation<ILVariableResolveResult>()?.Variable;
				if (variable != null)
					return variable;
			}

			if (id.Role == QueryJoinClause.IntoIdentifierRole || id.Role == QueryJoinClause.JoinIdentifierRole)
			{
				var variable = id.Annotation<ILVariableResolveResult>()?.Variable;
				if (variable != null)
					return variable;
			}

			if (node is QueryLetClause)
			{
				var variable = node.Annotation<CSharp.Transforms.LetIdentifierAnnotation>();
				if (variable != null)
					return variable;
			}

			if (node is LabelStatement label)
			{
				var method = nodeStack.Select(nd => nd.GetSymbol() as IMethod).FirstOrDefault(mr => mr != null);
				if (method != null)
					return method + label.Label;
			}

			if (node is MethodDeclaration && node.Parent is LocalFunctionDeclarationStatement)
			{
				var localFunction = node.Parent.GetResolveResult() as MemberResolveResult;
				if (localFunction != null)
					return localFunction.Member;
			}

			return null;
		}

		/// <summary>
		/// Gets the definition symbol for the current node, if the node denotes a declaration site.
		/// </summary>
		/// <returns>The declared symbol, or <see langword="null"/> when the current node is not a symbol definition.</returns>
		ISymbol GetCurrentDefinition()
		{
			if (nodeStack == null || nodeStack.Count == 0)
				return null;

			var node = nodeStack.Peek();
			if (node is Identifier)
				node = node.Parent;
			if (IsDefinition(ref node))
				return node.GetSymbol();

			return null;
		}

		/// <summary>
		/// Writes a keyword token and attaches constructor references for <c>this</c>/<c>base</c> initializers when available.
		/// </summary>
		/// <param name="role">The syntactic role for the keyword.</param>
		/// <param name="keyword">The keyword text.</param>
		public override void WriteKeyword(Role role, string keyword)
		{
			//To make reference for 'this' and 'base' keywords in the ClassName():this() expression
			if (role == ConstructorInitializer.ThisKeywordRole || role == ConstructorInitializer.BaseKeywordRole)
			{
				if (nodeStack.Peek() is ConstructorInitializer initializer && initializer.GetSymbol() is IMember member)
				{
					output.WriteReference(member, keyword);
					return;
				}
			}
			output.Write(keyword);
		}

		/// <summary>
		/// Writes punctuation and structural tokens, adding fold markers and reference metadata where applicable.
		/// </summary>
		/// <param name="role">The syntactic role represented by <paramref name="token"/>.</param>
		/// <param name="token">The token text.</param>
		public override void WriteToken(Role role, string token)
		{
			switch (token)
			{
				case "{":
					if (role != Roles.LBrace)
					{
						output.Write("{");
						break;
					}
					if (braceLevelWithinType >= 0 || nodeStack.Peek() is TypeDeclaration)
						braceLevelWithinType++;
					if (nodeStack.PeekOrDefault() is TypeDeclaration or ExtensionDeclaration or BlockStatement { Parent: EntityDeclaration or LocalFunctionDeclarationStatement or AnonymousMethodExpression or LambdaExpression } || settings.FoldBraces)
					{
						output.MarkFoldStart(defaultCollapsed: !settings.ExpandMemberDefinitions && braceLevelWithinType == 1, isDefinition: braceLevelWithinType == 1);
					}
					output.Write("{");
					break;
				case "}":
					output.Write('}');
					if (role != Roles.RBrace)
						break;
					if (nodeStack.PeekOrDefault() is TypeDeclaration or ExtensionDeclaration or BlockStatement { Parent: EntityDeclaration or LocalFunctionDeclarationStatement or AnonymousMethodExpression or LambdaExpression } || settings.FoldBraces)
						output.MarkFoldEnd();
					if (braceLevelWithinType >= 0)
						braceLevelWithinType--;
					break;
				default:
					// Attach member reference to token only if there's no identifier in the current node.
					var member = GetCurrentMemberReference();
					var node = nodeStack.Peek();
					if (member != null && node.GetChildByRole(Roles.Identifier).IsNull)
					{
						switch (member)
						{
							case IType t:
								output.WriteReference(t, token, false);
								return;
							case IMember m:
								output.WriteReference(m, token, false);
								return;
						}
					}
					else
						output.Write(token);
					break;
			}
		}

		/// <summary>
		/// Writes a single space character.
		/// </summary>
		public override void Space()
		{
			output.Write(' ');
		}

		/// <summary>
		/// Increases indentation depth in the underlying output.
		/// </summary>
		public override void Indent()
		{
			output.Indent();
		}

		/// <summary>
		/// Decreases indentation depth in the underlying output.
		/// </summary>
		public override void Unindent()
		{
			output.Unindent();
		}

		/// <summary>
		/// Writes a line break and closes the using-declaration fold region when the group ends.
		/// </summary>
		public override void NewLine()
		{
			if (!firstUsingDeclaration && lastUsingDeclaration)
			{
				output.MarkFoldEnd();
				lastUsingDeclaration = false;
			}
			output.WriteLine();
		}

		/// <summary>
		/// Writes a comment token and manages documentation-comment folding for consecutive lines.
		/// </summary>
		/// <param name="commentType">The comment syntax kind.</param>
		/// <param name="content">The comment text excluding start/end markers.</param>
		public override void WriteComment(CommentType commentType, string content)
		{
			switch (commentType)
			{
				case CommentType.SingleLine:
					output.Write("//");
					output.WriteLine(content);
					break;
				case CommentType.MultiLine:
					output.Write("/*");
					output.Write(content);
					output.Write("*/");
					break;
				case CommentType.Documentation:
					bool isLastLine = !(nodeStack.Peek().NextSibling is Comment);
					if (!inDocumentationComment && !isLastLine)
					{
						inDocumentationComment = true;
						output.MarkFoldStart("///" + content, true);
					}
					output.Write("///");
					output.Write(content);
					if (inDocumentationComment && isLastLine)
					{
						inDocumentationComment = false;
						output.MarkFoldEnd();
					}
					output.WriteLine();
					break;
				default:
					output.Write(content);
					break;
			}
		}

		/// <summary>
		/// Writes a preprocessor directive token sequence.
		/// </summary>
		/// <param name="type">The directive kind.</param>
		/// <param name="argument">The directive argument text, or empty when no argument exists.</param>
		public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
		{
			// pre-processor directive must start on its own line
			output.Write('#');
			output.Write(type.ToString().ToLowerInvariant());
			if (!string.IsNullOrEmpty(argument))
			{
				output.Write(' ');
				output.Write(argument);
			}
			output.WriteLine();
		}

		/// <summary>
		/// Formats and writes a literal value by delegating to <see cref="TextWriterTokenWriter"/>.
		/// </summary>
		/// <param name="value">The literal value to format.</param>
		/// <param name="format">Additional literal-formatting hints.</param>
		public override void WritePrimitiveValue(object value, LiteralFormat format = LiteralFormat.None)
		{
			new TextWriterTokenWriter(new TextOutputWriter(output)).WritePrimitiveValue(value, format);
		}

		/// <summary>
		/// Writes the text portion of an interpolated string after applying C# escaping rules.
		/// </summary>
		/// <param name="text">The unescaped interpolation text segment.</param>
		public override void WriteInterpolatedText(string text)
		{
			output.Write(TextWriterTokenWriter.ConvertString(text));
		}

		/// <summary>
		/// Writes a primitive type token and associates it with symbol metadata when resolvable.
		/// </summary>
		/// <param name="type">The primitive type token text.</param>
		public override void WritePrimitiveType(string type)
		{
			switch (type)
			{
				case "new":
					output.Write(type);
					output.Write("()");
					break;
				case "bool":
				case "byte":
				case "sbyte":
				case "short":
				case "ushort":
				case "int":
				case "uint":
				case "long":
				case "ulong":
				case "float":
				case "double":
				case "decimal":
				case "char":
				case "string":
				case "object":
					var node = nodeStack.Peek();
					ISymbol symbol;
					if (node.Role == Roles.Type && node.Parent is ObjectCreateExpression)
					{
						symbol = node.Parent.GetSymbol();
					}
					else
					{
						symbol = nodeStack.Peek().GetSymbol();
					}
					if (symbol == null)
						goto default;
					switch (symbol)
					{
						case IType t:
							output.WriteReference(t, type, false);
							return;
						case IMember m:
							output.WriteReference(m, type, false);
							return;
					}
					break;
				default:
					output.Write(type);
					break;
			}
		}

		/// <summary>
		/// Pushes <paramref name="node"/> onto the traversal stack and initializes using-fold tracking at the root level.
		/// </summary>
		/// <param name="node">The AST node being entered.</param>
		public override void StartNode(AstNode node)
		{
			if (nodeStack.Count == 0)
			{
				if (IsUsingDeclaration(node))
				{
					firstUsingDeclaration = !IsUsingDeclaration(node.PrevSibling);
					lastUsingDeclaration = !IsUsingDeclaration(node.NextSibling);
				}
				else
				{
					firstUsingDeclaration = false;
					lastUsingDeclaration = false;
				}
			}
			nodeStack.Push(node);
		}

		/// <summary>
		/// Determines whether <paramref name="node"/> is a top-level using declaration node.
		/// </summary>
		/// <param name="node">The node to examine.</param>
		/// <returns><see langword="true"/> when <paramref name="node"/> is a <see cref="UsingDeclaration"/> or <see cref="UsingAliasDeclaration"/>.</returns>
		private bool IsUsingDeclaration(AstNode node)
		{
			return node is UsingDeclaration || node is UsingAliasDeclaration;
		}

		/// <summary>
		/// Pops <paramref name="node"/> from the traversal stack.
		/// </summary>
		/// <param name="node">The AST node being left.</param>
		/// <exception cref="InvalidOperationException">
		/// Thrown when node boundaries are unbalanced and <paramref name="node"/> is not the current stack top.
		/// </exception>
		public override void EndNode(AstNode node)
		{
			if (nodeStack.Pop() != node)
				throw new InvalidOperationException();
		}

		/// <summary>
		/// Determines whether <paramref name="node"/> denotes a symbol definition site and normalizes field/event variable initializers.
		/// </summary>
		/// <param name="node">The candidate node. Updated to the owning declaration for field/event initializer definitions.</param>
		/// <returns><see langword="true"/> when the node should be treated as a definition; otherwise <see langword="false"/>.</returns>
		public static bool IsDefinition(ref AstNode node)
		{
			if (node is EntityDeclaration && !(node.Parent is LocalFunctionDeclarationStatement))
				return true;
			if (node is VariableInitializer && node.Parent is FieldDeclaration or EventDeclaration)
			{
				node = node.Parent;
				return true;
			}
			if (node is FixedVariableInitializer && node.Parent is FixedFieldDeclaration)
			{
				node = node.Parent;
				return true;
			}
			return false;
		}
	}
}
