// Copyright (c) 2019 Daniel Grunwald
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
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ICSharpCode.Decompiler.DebugInfo
{
	/// <summary>
	/// Represents async stepping metadata emitted as Portable PDB custom debug information.
	/// </summary>
	public readonly struct AsyncDebugInfo
	{
		/// <summary>
		/// IL offset of the synthesized catch handler used by the async state machine.
		/// </summary>
		public readonly int CatchHandlerOffset;

		/// <summary>
		/// Await suspension/resume pairs used by debugger stepper logic.
		/// </summary>
		public readonly ImmutableArray<Await> Awaits;

		/// <summary>
		/// Initializes async stepping metadata.
		/// </summary>
		/// <param name="catchHandlerOffset">IL offset of the async catch handler.</param>
		/// <param name="awaits">Await yield/resume mappings in state-machine order.</param>
		public AsyncDebugInfo(int catchHandlerOffset, ImmutableArray<Await> awaits)
		{
			this.CatchHandlerOffset = catchHandlerOffset;
			this.Awaits = awaits;
		}

		/// <summary>
		/// Represents a single await suspension point and its resume target.
		/// </summary>
		public readonly struct Await
		{
			/// <summary>
			/// IL offset where execution yields to the awaited operation.
			/// </summary>
			public readonly int YieldOffset;

			/// <summary>
			/// IL offset where execution resumes after the await completes.
			/// </summary>
			public readonly int ResumeOffset;

			/// <summary>
			/// Initializes a yield/resume mapping for one await.
			/// </summary>
			/// <param name="yieldOffset">IL offset of the suspension point.</param>
			/// <param name="resumeOffset">IL offset of the resume target.</param>
			public Await(int yieldOffset, int resumeOffset)
			{
				this.YieldOffset = yieldOffset;
				this.ResumeOffset = resumeOffset;
			}
		}

		/// <summary>
		/// Serializes this instance into the blob format consumed by the MethodSteppingInformation custom debug record.
		/// </summary>
		/// <param name="moveNext">Method definition handle for the state machine's <c>MoveNext</c> method.</param>
		/// <returns>A metadata blob containing catch-handler and await-step records.</returns>
		public BlobBuilder BuildBlob(MethodDefinitionHandle moveNext)
		{
			BlobBuilder blob = new BlobBuilder();
			blob.WriteUInt32((uint)CatchHandlerOffset);
			foreach (var await in Awaits)
			{
				blob.WriteUInt32((uint)await.YieldOffset);
				blob.WriteUInt32((uint)await.ResumeOffset);
				blob.WriteCompressedInteger(MetadataTokens.GetRowNumber(moveNext));
			}
			return blob;
		}
	}
}
