using System.Collections.Generic;

using ICSharpCode.Decompiler.CSharp.Syntax.PatternMatching;

namespace ICSharpCode.Decompiler.CSharp.Syntax
{
	/// <summary>
	/// Represents a C# interpolated string expression (<c>$"..."</c>).
	/// </summary>
	/// <remarks>
	/// <para>
	/// The expression stores its payload as an ordered <see cref="Content"/> collection containing either literal
	/// segments (<see cref="InterpolatedStringText"/>) or interpolation holes (<see cref="Interpolation"/>).
	/// </para>
	/// <para>
	/// This node models classic interpolated strings. Raw-string interpolation token forms are represented by different
	/// token roles in newer syntax nodes.
	/// </para>
	/// </remarks>
	public class InterpolatedStringExpression : Expression
	{
		/// <summary>
		/// Role for the opening interpolated-string token (<c>$"</c>).
		/// </summary>
		public static readonly TokenRole OpenQuote = new TokenRole("$\"");
		public static readonly TokenRole CloseQuote = new TokenRole("\"");

		/// <summary>
		/// Gets the ordered sequence of literal and interpolation nodes that compose the string body.
		/// </summary>
		public AstNodeCollection<InterpolatedStringContent> Content {
			get { return GetChildrenByRole(InterpolatedStringContent.Role); }
		}

		public InterpolatedStringExpression()
		{

		}

		/// <summary>
		/// Initializes an interpolated string from existing content nodes.
		/// </summary>
		/// <param name="content">Content nodes to append in lexical order.</param>
		public InterpolatedStringExpression(IList<InterpolatedStringContent> content)
		{
			Content.AddRange(content);
		}

		public override void AcceptVisitor(IAstVisitor visitor)
		{
			visitor.VisitInterpolatedStringExpression(this);
		}

		public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
		{
			return visitor.VisitInterpolatedStringExpression(this);
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitInterpolatedStringExpression(this, data);
		}

		protected internal override bool DoMatch(AstNode other, Match match)
		{
			InterpolatedStringExpression o = other as InterpolatedStringExpression;
			return o != null && !o.IsNull && this.Content.DoMatch(o.Content, match);
		}
	}

	/// <summary>
	/// Base type for nodes that can appear inside an <see cref="InterpolatedStringExpression"/>.
	/// </summary>
	public abstract class InterpolatedStringContent : AstNode
	{
		#region Null
		public new static readonly InterpolatedStringContent Null = new NullInterpolatedStringContent();

		sealed class NullInterpolatedStringContent : InterpolatedStringContent
		{
			public override bool IsNull {
				get {
					return true;
				}
			}

			public override void AcceptVisitor(IAstVisitor visitor)
			{
				visitor.VisitNullNode(this);
			}

			public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
			{
				return visitor.VisitNullNode(this);
			}

			public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitNullNode(this, data);
			}

			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion

		/// <summary>
		/// Role used by <see cref="InterpolatedStringExpression"/> to store content children.
		/// </summary>
		public new static readonly Role<InterpolatedStringContent> Role = new Role<InterpolatedStringContent>("InterpolatedStringContent", Syntax.InterpolatedStringContent.Null);

		public override NodeType NodeType => NodeType.Unknown;
	}

	/// <summary>
	/// Represents one interpolation hole inside an interpolated string body.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="Alignment"/> and <see cref="Suffix"/> are metadata values carried from IL/string-format patterns.
	/// They are intentionally excluded from pattern matching in <see cref="DoMatch(AstNode, Match)"/>, which only
	/// compares the embedded <see cref="Expression"/> subtree.
	/// </para>
	/// </remarks>
	public class Interpolation : InterpolatedStringContent
	{
		public static readonly TokenRole LBrace = new TokenRole("{");
		public static readonly TokenRole RBrace = new TokenRole("}");

		public CSharpTokenNode LBraceToken {
			get { return GetChildByRole(LBrace); }
		}

		public Expression Expression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}

		/// <summary>
		/// Gets the alignment component that follows the expression in <c>{expr,alignment}</c> forms.
		/// </summary>
		public int Alignment { get; }

		/// <summary>
		/// Gets the format-suffix text that follows a colon in <c>{expr:format}</c> forms.
		/// </summary>
		public string Suffix { get; }

		public CSharpTokenNode RBraceToken {
			get { return GetChildByRole(RBrace); }
		}

		public Interpolation()
		{

		}

		/// <summary>
		/// Initializes an interpolation hole.
		/// </summary>
		/// <param name="expression">Expression to evaluate and format.</param>
		/// <param name="alignment">Optional alignment width supplied in source or reconstructed during transforms.</param>
		/// <param name="suffix">Optional format suffix (without the leading colon).</param>
		public Interpolation(Expression expression, int alignment = 0, string suffix = null)
		{
			Expression = expression;
			Alignment = alignment;
			Suffix = suffix;
		}

		public override void AcceptVisitor(IAstVisitor visitor)
		{
			visitor.VisitInterpolation(this);
		}

		public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
		{
			return visitor.VisitInterpolation(this);
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitInterpolation(this, data);
		}

		protected internal override bool DoMatch(AstNode other, Match match)
		{
			Interpolation o = other as Interpolation;
			return o != null && this.Expression.DoMatch(o.Expression, match);
		}
	}

	/// <summary>
	/// Represents a literal text segment within an <see cref="InterpolatedStringExpression"/>.
	/// </summary>
	public class InterpolatedStringText : InterpolatedStringContent
	{
		/// <summary>
		/// Gets or sets the literal text payload for this segment.
		/// </summary>
		public string Text { get; set; }

		public InterpolatedStringText()
		{

		}

		/// <summary>
		/// Initializes a literal interpolated-string text segment.
		/// </summary>
		/// <param name="text">Literal content for this segment.</param>
		public InterpolatedStringText(string text)
		{
			Text = text;
		}

		public override void AcceptVisitor(IAstVisitor visitor)
		{
			visitor.VisitInterpolatedStringText(this);
		}

		public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
		{
			return visitor.VisitInterpolatedStringText(this);
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitInterpolatedStringText(this, data);
		}

		protected internal override bool DoMatch(AstNode other, Match match)
		{
			InterpolatedStringText o = other as InterpolatedStringText;
			return o != null && o.Text == this.Text;
		}
	}
}
