// Copyright (c) 2026 Siegfried Pammer
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

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Rewrites the VB compiler's copy-constructor closures into the single-store, empty-initialized
	/// shape that the C# closure pipeline expects, so that VB closures dissolve like C# display classes.
	///
	/// This runs before <see cref="DelegateConstruction"/> so the lambdas that capture the closure can
	/// be inlined (DelegateConstruction requires the closure variable to be single-definition, which the
	/// VB per-iteration copy pattern only becomes after normalization). The actual rewrite lives in
	/// <see cref="TransformDisplayClassUsage.NormalizeVisualBasicClosures"/>.
	/// </summary>
	public class NormalizeVisualBasicClosures : IILTransform
	{
		public void Run(ILFunction function, ILTransformContext context)
		{
			if (!context.Settings.AnonymousMethods)
				return;
			TransformDisplayClassUsage.NormalizeVisualBasicClosures(function, context);
		}
	}
}
