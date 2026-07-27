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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Represents an exception that occurred while decompiling a metadata entity.
	/// </summary>
	/// <remarks>
	/// This type preserves decompilation context (module, file, and target entity) and
	/// formats the stack trace in a stable, language-independent form for diagnostics.
	/// </remarks>
	public class DecompilerException : Exception, ISerializable
	{
		/// <summary>
		/// Gets the simple assembly name associated with the decompilation attempt.
		/// </summary>
		public string AssemblyName => File.Name;

		/// <summary>
		/// Gets the input file path that was being decompiled.
		/// </summary>
		public string FileName => File.FileName;

		/// <summary>
		/// Gets the entity that triggered this exception.
		/// </summary>
		public IEntity DecompiledEntity { get; }

		/// <summary>
		/// Gets the metadata module used for the failed decompilation operation.
		/// </summary>
		public IModule Module { get; }

		/// <summary>
		/// Gets the metadata file that contains the failing entity.
		/// </summary>
		public MetadataFile File { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DecompilerException"/> class for an exception
		/// that occurred while decompiling a specific entity from a metadata module.
		/// </summary>
		/// <param name="module">The metadata module that was being decompiled.</param>
		/// <param name="decompiledEntity">The entity that failed to decompile.</param>
		/// <param name="innerException">The underlying exception thrown during decompilation.</param>
		/// <param name="message">
		/// An optional message. If <see langword="null"/>, a default message is generated from
		/// <paramref name="decompiledEntity"/>.
		/// </param>
		public DecompilerException(MetadataModule module, IEntity decompiledEntity,
			Exception innerException, string message = null)
			: base(message ?? GetDefaultMessage(decompiledEntity), innerException)
		{
			this.File = module.MetadataFile;
			this.Module = module;
			this.DecompiledEntity = decompiledEntity;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DecompilerException"/> class for a failure
		/// associated with a specific metadata file.
		/// </summary>
		/// <param name="file">The metadata file involved in the failure.</param>
		/// <param name="message">The error message that describes the failure.</param>
		/// <param name="innerException">The underlying exception that caused the failure.</param>
		public DecompilerException(MetadataFile file, string message, Exception innerException)
			: base(message, innerException)
		{
			this.File = file;
		}

		static string GetDefaultMessage(IEntity entity)
		{
			if (entity == null)
				return "Error decompiling";
			return $"Error decompiling @{MetadataTokens.GetToken(entity.MetadataToken):X8} {entity.FullName}";
		}

		// This constructor is needed for serialization.
		/// <summary>
		/// Initializes a new instance of the <see cref="DecompilerException"/> class from serialized data.
		/// </summary>
		/// <param name="info">The serialization information used to deserialize the exception.</param>
		/// <param name="context">The streaming context that describes the source and destination.</param>
		protected DecompilerException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		/// <summary>
		/// Gets a normalized stack trace string for this exception. Frames from inner exceptions are not
		/// included; use <see cref="ToString()"/> for the full chain.
		/// </summary>
		public override string StackTrace => GetStackTrace(this);

		/// <summary>
		/// Returns a diagnostic string that includes decompilation context and normalized stack traces.
		/// </summary>
		/// <returns>A formatted error string suitable for logging and diagnostics.</returns>
		public override string ToString() => ToString(this);

		string ToString(Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));
			string exceptionType = GetTypeName(exception);
			string stacktrace = GetStackTrace(exception);
			while (exception.InnerException != null)
			{
				exception = exception.InnerException;

				stacktrace = GetStackTrace(exception) + Environment.NewLine
					+ "-- continuing with outer exception (" + exceptionType + ") --" + Environment.NewLine
					+ stacktrace;
				exceptionType = GetTypeName(exception);
			}
			return this.Message + Environment.NewLine
				+ $"in assembly \"{this.FileName}\"" + Environment.NewLine
				+ " ---> " + exceptionType + ": " + exception.Message + Environment.NewLine
				+ stacktrace;
		}

		static string GetTypeName(Exception exception)
		{
			string type = exception.GetType().FullName;
			if (exception is ExternalException || exception is IOException)
				return type + " (" + Marshal.GetHRForException(exception).ToString("x8") + ")";
			else
				return type;
		}

		static string GetStackTrace(Exception exception)
		{
			// Output stacktrace in custom format (very similar to Exception.StackTrace
			// property on English systems).
			// Include filenames where available, but no paths.
			StackTrace stackTrace = new StackTrace(exception, true);
			StringBuilder b = new StringBuilder();
			for (int i = 0; i < stackTrace.FrameCount; i++)
			{
				StackFrame frame = stackTrace.GetFrame(i);
				MethodBase method = frame.GetMethod();
				if (method == null)
					continue;

				if (b.Length > 0)
					b.AppendLine();

				b.Append("   at ");
				Type declaringType = method.DeclaringType;
				if (declaringType != null)
				{
					b.Append(declaringType.FullName.Replace('+', '.'));
					b.Append('.');
				}
				b.Append(method.Name);
				// output type parameters, if any
				if ((method is MethodInfo) && ((MethodInfo)method).IsGenericMethod)
				{
					Type[] genericArguments = ((MethodInfo)method).GetGenericArguments();
					b.Append('[');
					for (int j = 0; j < genericArguments.Length; j++)
					{
						if (j > 0)
							b.Append(',');
						b.Append(genericArguments[j].Name);
					}
					b.Append(']');
				}

				// output parameters, if any
				b.Append('(');
				ParameterInfo[] parameters = method.GetParameters();
				for (int j = 0; j < parameters.Length; j++)
				{
					if (j > 0)
						b.Append(", ");
					if (parameters[j].ParameterType != null)
					{
						b.Append(parameters[j].ParameterType.Name);
					}
					else
					{
						b.Append('?');
					}
					if (!string.IsNullOrEmpty(parameters[j].Name))
					{
						b.Append(' ');
						b.Append(parameters[j].Name);
					}
				}
				b.Append(')');

				// source location
				if (frame.GetILOffset() >= 0)
				{
					string filename = null;
					try
					{
						string fullpath = frame.GetFileName();
						if (fullpath != null)
							filename = Path.GetFileName(fullpath);
					}
					catch (SecurityException)
					{
						// StackFrame.GetFileName requires PathDiscovery permission
					}
					catch (ArgumentException)
					{
						// Path.GetFileName might throw on paths with invalid chars
					}
					b.Append(" in ");
					if (filename != null)
					{
						b.Append(filename);
						b.Append(":line ");
						b.Append(frame.GetFileLineNumber());
					}
					else
					{
						b.Append("offset ");
						b.Append(frame.GetILOffset());
					}
				}
			}

			return b.ToString();
		}
	}
}
