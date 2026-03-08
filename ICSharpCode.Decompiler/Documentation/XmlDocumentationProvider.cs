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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.Documentation
{
	/// <summary>
	/// Provides XML documentation fragments for entities identified by type-system metadata.
	/// </summary>
	/// <remarks>
	/// Implementations are expected to return the inner XML payload of a documentation member, not a wrapped <c>&lt;member&gt;</c> element.
	/// Consumers such as ILSpy UI hosts inject that payload into their own parsing pipeline.
	/// </remarks>
	public interface IDocumentationProvider
	{
		/// <summary>
		/// Gets the XML documentation payload associated with an entity.
		/// </summary>
		/// <param name="entity">Entity to look up using its documentation ID.</param>
		/// <returns>
		/// Inner XML content from the matching <c>&lt;member&gt;</c> element, or <see langword="null"/> when the entity has no available documentation.
		/// </returns>
		/// <exception cref="ArgumentNullException"><paramref name="entity"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// The lookup key is the canonical member ID produced by <see cref="IdStringProvider.GetIdString(IEntity)"/>.
		/// </remarks>
		string GetDocumentation(IEntity entity);
	}

	/// <summary>
	/// Reads compiler-generated XML documentation files using a lightweight hash index and on-demand member loading.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The provider builds an index of <c>&lt;member name="..."&gt;</c> locations once, then reopens the XML file and seeks directly to matching entries.
	/// This keeps memory usage low compared to loading the full XML DOM.
	/// </para>
	/// <para>
	/// Lookups are cached in a small ring buffer for hot keys. If file contents change after index creation, the provider retries once with a rebuilt index.
	/// </para>
	/// <para>
	/// The provider is thread-compatible for concurrent readers: lookup and cache mutation paths synchronize on the cache instance.
	/// </para>
	/// </remarks>
	[Serializable]
	public class XmlDocumentationProvider : IDeserializationCallback, IDocumentationProvider
	{
		#region Cache
		/// <summary>
		/// Small fixed-size ring buffer for recent documentation lookups.
		/// </summary>
		sealed class XmlDocumentationCache
		{
			readonly KeyValuePair<string, string>[] entries;
			int pos;

			/// <summary>
			/// Initializes the lookup cache with a fixed slot count.
			/// </summary>
			/// <param name="size">Number of cache entries. Must be positive.</param>
			/// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is less than or equal to zero.</exception>
			public XmlDocumentationCache(int size = 50)
			{
				if (size <= 0)
					throw new ArgumentOutOfRangeException(nameof(size), size, "Value must be positive");
				this.entries = new KeyValuePair<string, string>[size];
			}

			/// <summary>
			/// Tries to retrieve a cached value for the specified documentation key.
			/// </summary>
			/// <param name="key">Documentation member ID used as lookup key.</param>
			/// <param name="value">Receives the cached XML fragment when the key is present.</param>
			/// <returns><see langword="true"/> if a cache entry exists for <paramref name="key"/>; otherwise <see langword="false"/>.</returns>
			internal bool TryGet(string key, out string value)
			{
				foreach (var pair in entries)
				{
					if (pair.Key == key)
					{
						value = pair.Value;
						return true;
					}
				}
				value = null;
				return false;
			}

			/// <summary>
			/// Inserts or overwrites the next ring-buffer slot with a lookup result.
			/// </summary>
			/// <param name="key">Documentation member ID used as cache key.</param>
			/// <param name="value">Cached XML fragment, or <see langword="null"/> for a negative lookup.</param>
			internal void Add(string key, string value)
			{
				entries[pos++] = new KeyValuePair<string, string>(key, value);
				if (pos == entries.Length)
					pos = 0;
			}
		}
		#endregion

		/// <summary>
		/// Hash-index entry that maps a documentation member hash to a file position candidate.
		/// </summary>
		[Serializable]
		struct IndexEntry : IComparable<IndexEntry>
		{
			/// <summary>
			/// Hash code of the documentation tag
			/// </summary>
			internal readonly int HashCode;

			/// <summary>
			/// Position in the .xml file where the documentation starts
			/// </summary>
			internal readonly int PositionInFile;

			/// <summary>
			/// Creates an index entry.
			/// </summary>
			/// <param name="hashCode">Stable hash of the member ID key.</param>
			/// <param name="positionInFile">Byte offset where the candidate <c>&lt;member&gt;</c> element starts.</param>
			internal IndexEntry(int hashCode, int positionInFile)
			{
				this.HashCode = hashCode;
				this.PositionInFile = positionInFile;
			}

			/// <inheritdoc/>
			public int CompareTo(IndexEntry other)
			{
				return this.HashCode.CompareTo(other.HashCode);
			}
		}

		[NonSerialized]
		XmlDocumentationCache cache = new XmlDocumentationCache();

		readonly string fileName;
		readonly Encoding encoding;
		volatile IndexEntry[] index; // SORTED array of index entries

		#region Constructor / Redirection support
		/// <summary>
		/// Initializes a provider for a documentation XML file, following optional root-level redirection metadata.
		/// </summary>
		/// <param name="fileName">Path to the XML documentation file to index.</param>
		/// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
		/// <exception cref="IOException">An I/O failure occurs while opening or reading the original or redirected XML file.</exception>
		/// <exception cref="XmlException">The XML content is malformed, or redirection points to a non-existing target.</exception>
		public XmlDocumentationProvider(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));

			using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
			{
				using (XmlTextReader xmlReader = new XmlTextReader(fs))
				{
					xmlReader.XmlResolver = null; // no DTD resolving
					xmlReader.MoveToContent();
					if (string.IsNullOrEmpty(xmlReader.GetAttribute("redirect")))
					{
						this.fileName = fileName;
						this.encoding = xmlReader.Encoding;
						ReadXmlDoc(xmlReader);
					}
					else
					{
						string redirectionTarget = GetRedirectionTarget(fileName, xmlReader.GetAttribute("redirect"));
						if (redirectionTarget != null)
						{
							Debug.WriteLine("XmlDoc " + fileName + " is redirecting to " + redirectionTarget);
							using (FileStream redirectedFs = new FileStream(redirectionTarget, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
							{
								using (XmlTextReader redirectedXmlReader = new XmlTextReader(redirectedFs))
								{
									redirectedXmlReader.XmlResolver = null; // no DTD resolving
									redirectedXmlReader.MoveToContent();
									this.fileName = redirectionTarget;
									this.encoding = redirectedXmlReader.Encoding;
									ReadXmlDoc(redirectedXmlReader);
								}
							}
						}
						else
						{
							throw new XmlException("XmlDoc " + fileName + " is redirecting to " + xmlReader.GetAttribute("redirect") + ", but that file was not found.");
						}
					}
				}
			}
		}

		static string GetRedirectionTarget(string xmlFileName, string target)
		{
			string programFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			programFilesDir = AppendDirectorySeparator(programFilesDir);

			string corSysDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
			corSysDir = AppendDirectorySeparator(corSysDir);

			var fileName = target.Replace("%PROGRAMFILESDIR%", programFilesDir)
				.Replace("%CORSYSDIR%", corSysDir);
			if (!Path.IsPathRooted(fileName))
				fileName = Path.Combine(Path.GetDirectoryName(xmlFileName), fileName);
			return XmlDocLoader.LookupLocalizedXmlDoc(fileName);
		}

		static string AppendDirectorySeparator(string dir)
		{
			if (dir.EndsWith("\\", StringComparison.Ordinal) || dir.EndsWith("/", StringComparison.Ordinal))
				return dir;
			else
				return dir + Path.DirectorySeparatorChar;
		}

		#endregion

		#region Load / Create Index
		/// <summary>
		/// Rebuilds the in-memory hash index from the documentation file.
		/// </summary>
		/// <param name="reader">Reader positioned at the XML document root.</param>
		void ReadXmlDoc(XmlTextReader reader)
		{
			//lastWriteDate = File.GetLastWriteTimeUtc(fileName);
			// Open up a second file stream for the line<->position mapping
			using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
			{
				LinePositionMapper linePosMapper = new LinePositionMapper(fs, encoding);
				List<IndexEntry> indexList = new List<IndexEntry>();
				while (reader.Read())
				{
					if (reader.IsStartElement())
					{
						switch (reader.LocalName)
						{
							case "members":
								ReadMembersSection(reader, linePosMapper, indexList);
								break;
						}
					}
				}
				indexList.Sort();
				this.index = indexList.ToArray(); // volatile write
			}
		}

		/// <summary>
		/// Maps XML line numbers to byte positions in the encoded file stream.
		/// </summary>
		/// <remarks>
		/// <see cref="XmlTextReader"/> reports line/column locations, but lookup requires byte offsets for stream seeking. This mapper bridges
		/// the two coordinate systems by replaying decoder state over the original byte stream.
		/// </remarks>
		sealed class LinePositionMapper
		{
			readonly FileStream fs;
			readonly Decoder decoder;
			int currentLine = 1;
			char prevChar = '\0';

			// buffers for use with Decoder:
			readonly byte[] input = new byte[1];
			readonly char[] output = new char[2];

			/// <summary>
			/// Initializes the line-to-position mapper.
			/// </summary>
			/// <param name="fs">Readable stream positioned at the start of the XML file.</param>
			/// <param name="encoding">Encoding used by the XML document.</param>
			public LinePositionMapper(FileStream fs, Encoding encoding)
			{
				this.decoder = encoding.GetDecoder();
				this.fs = fs;
			}

			/// <summary>
			/// Advances the stream to the requested line and returns the byte offset at that line start.
			/// </summary>
			/// <param name="line">1-based line number to map.</param>
			/// <returns>Byte position corresponding to the start of <paramref name="line"/>.</returns>
			/// <exception cref="EndOfStreamException">The underlying stream ends before reaching <paramref name="line"/>.</exception>
			public int GetPositionForLine(int line)
			{
				Debug.Assert(line >= currentLine);
				while (line > currentLine)
				{
					int b = fs.ReadByte();
					if (b < 0)
						throw new EndOfStreamException();
					input[0] = (byte)b;
					decoder.Convert(input, 0, 1, output, 0, output.Length, false, out int bytesUsed, out int charsUsed, out _);
					Debug.Assert(bytesUsed == 1);
					if (charsUsed == 1)
					{
						if ((prevChar != '\r' && output[0] == '\n') || output[0] == '\r')
							currentLine++;
						prevChar = output[0];
					}
				}
				return checked((int)fs.Position);
			}
		}

		/// <summary>
		/// Reads the <c>&lt;members&gt;</c> section and records candidate positions for each documented member ID.
		/// </summary>
		static void ReadMembersSection(XmlTextReader reader, LinePositionMapper linePosMapper, List<IndexEntry> indexList)
		{
			while (reader.Read())
			{
				switch (reader.NodeType)
				{
					case XmlNodeType.EndElement:
						if (reader.LocalName == "members")
						{
							return;
						}
						break;
					case XmlNodeType.Element:
						if (reader.LocalName == "member")
						{
							int pos = linePosMapper.GetPositionForLine(reader.LineNumber) + Math.Max(reader.LinePosition - 2, 0);
							string memberAttr = reader.GetAttribute("name");
							if (memberAttr != null)
								indexList.Add(new IndexEntry(GetHashCode(memberAttr), pos));
							reader.Skip();
						}
						break;
				}
			}
		}

		/// <summary>
		/// Hash algorithm used for the index.
		/// This is a custom implementation so that old index files work correctly
		/// even when the .NET string.GetHashCode implementation changes
		/// (e.g. due to .NET 4.5 hash randomization)
		/// </summary>
		static int GetHashCode(string key)
		{
			unchecked
			{
				int h = 0;
				foreach (char c in key)
				{
					h = (h << 5) - h + c;
				}
				return h;
			}
		}
		#endregion

		#region GetDocumentation
		/// <summary>
		/// Gets documentation by raw XML documentation member key.
		/// </summary>
		/// <param name="key">Member ID key (for example <c>M:Namespace.Type.Method(System.String)</c>).</param>
		/// <returns>The inner XML for the member element, or <see langword="null"/> when no matching member exists.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
		public string GetDocumentation(string key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			return GetDocumentation(key, true);
		}

		/// <summary>
		/// Gets documentation for a type-system entity by converting it to an XML documentation ID.
		/// </summary>
		/// <param name="entity">Entity whose documentation should be retrieved.</param>
		/// <returns>The member documentation payload, or <see langword="null"/> when no entry is available.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="entity"/> is <see langword="null"/>.</exception>
		public string GetDocumentation(IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException(nameof(entity));
			return GetDocumentation(entity.GetIdString());
		}

		string GetDocumentation(string key, bool allowReload)
		{
			int hashcode = GetHashCode(key);
			var index = this.index; // read volatile field
									// index is sorted, so we can use binary search
			int m = Array.BinarySearch(index, new IndexEntry(hashcode, 0));
			if (m < 0)
				return null;
			// correct hash code found.
			// possibly there are multiple items with the same hash, so go to the first.
			while (--m >= 0 && index[m].HashCode == hashcode)
				;
			// m is now 1 before the first item with the correct hash

			XmlDocumentationCache cache = this.cache;
			lock (cache)
			{
				if (!cache.TryGet(key, out string val))
				{
					try
					{
						// go through all items that have the correct hash
						while (++m < index.Length && index[m].HashCode == hashcode)
						{
							val = LoadDocumentation(key, index[m].PositionInFile);
							if (val != null)
								break;
						}
						// cache the result (even if it is null)
						cache.Add(key, val);
					}
					catch (IOException)
					{
						// may happen if the documentation file was deleted/is inaccessible/changed (EndOfStreamException)
						return allowReload ? ReloadAndGetDocumentation(key) : null;
					}
					catch (XmlException)
					{
						// may happen if the documentation file was changed so that the file position no longer starts on a valid XML element
						return allowReload ? ReloadAndGetDocumentation(key) : null;
					}
				}
				return val;
			}
		}

		/// <summary>
		/// Rebuilds the index once and retries a documentation lookup.
		/// </summary>
		/// <param name="key">Documentation member ID to resolve after index rebuild.</param>
		/// <returns>The matching XML fragment, or <see langword="null"/> if lookup still fails.</returns>
		string ReloadAndGetDocumentation(string key)
		{
			try
			{
				// Reload the index
				using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
				{
					using (XmlTextReader xmlReader = new XmlTextReader(fs))
					{
						xmlReader.XmlResolver = null; // no DTD resolving
						xmlReader.MoveToContent();
						ReadXmlDoc(xmlReader);
					}
				}
			}
			catch (IOException)
			{
				// Ignore errors on reload; IEntity.Documentation callers aren't prepared to handle exceptions
				this.index = Empty<IndexEntry>.Array; // clear index to avoid future load attempts
				return null;
			}
			catch (XmlException)
			{
				this.index = Empty<IndexEntry>.Array; // clear index to avoid future load attempts
				return null;
			}
			return GetDocumentation(key, allowReload: false); // prevent infinite reload loops
		}
		#endregion

		#region Load / Read XML
		/// <summary>
		/// Attempts to load a member entry by seeking to a previously indexed byte position.
		/// </summary>
		/// <param name="key">Documentation member ID expected at the indexed position.</param>
		/// <param name="positionInFile">Byte offset of a candidate <c>&lt;member&gt;</c> element.</param>
		/// <returns>Inner XML payload when the candidate entry matches <paramref name="key"/>; otherwise <see langword="null"/>.</returns>
		/// <remarks>
		/// Hash collisions are expected, so callers probe multiple index entries with the same hash until one yields a matching <c>name</c> attribute.
		/// </remarks>
		string LoadDocumentation(string key, int positionInFile)
		{
			using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
			{
				fs.Position = positionInFile;
				var context = new XmlParserContext(null, null, null, XmlSpace.None) { Encoding = encoding };
				using (XmlTextReader r = new XmlTextReader(fs, XmlNodeType.Element, context))
				{
					r.XmlResolver = null; // no DTD resolving
					while (r.Read())
					{
						if (r.NodeType == XmlNodeType.Element)
						{
							string memberAttr = r.GetAttribute("name");
							if (memberAttr == key)
							{
								return r.ReadInnerXml();
							}
							else
							{
								return null;
							}
						}
					}
					return null;
				}
			}
		}
		#endregion

		/// <summary>
		/// Reinitializes non-serialized runtime cache state after binary deserialization.
		/// </summary>
		/// <param name="sender">Serialization callback source (unused).</param>
		public virtual void OnDeserialization(object sender)
		{
			cache = new XmlDocumentationCache();
		}
	}
}
