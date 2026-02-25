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

#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using ICSharpCode.ILSpyX.Extensions;
using ICSharpCode.ILSpyX.FileLoaders;

namespace ICSharpCode.ILSpyX
{
	/// <summary>
	/// A list of assemblies.
	/// </summary>
	public sealed class AssemblyList
	{
		readonly Thread ownerThread;
		readonly SynchronizationContext? synchronizationContext;
		readonly AssemblyListManager manager;
		readonly string listName;

		/// <summary>Dirty flag, used to mark modifications so that the list is saved later</summary>
		bool dirty;
		readonly object lockObj = new object();

		/// <summary>
		/// The assemblies in this list.
		/// Needs locking for multi-threaded access!
		/// Write accesses are allowed on the GUI thread only (but still need locking!)
		/// </summary>
		/// <remarks>
		/// Technically read accesses need locking when done on non-GUI threads... but whenever possible, use the
		/// thread-safe <see cref="GetAssemblies()"/> method.
		/// </remarks>
		readonly ObservableCollection<LoadedAssembly> assemblies = new ObservableCollection<LoadedAssembly>();

		/// <summary>
		/// Assembly lookup by filename.
		/// Usually byFilename.Values == assemblies; but when an assembly is loaded by a background thread,
		/// that assembly is added to byFilename immediately, and to assemblies only later on the main thread.
		/// </summary>
		readonly Dictionary<string, LoadedAssembly> byFilename = new Dictionary<string, LoadedAssembly>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Exists for testing only.
		/// </summary>
		internal AssemblyList()
		{
			ownerThread = Thread.CurrentThread;
			manager = null!;
			listName = "Testing Only";
		}

		internal AssemblyList(AssemblyListManager manager, string listName)
		{
			this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
			this.listName = listName;
			this.ApplyWinRTProjections = manager.ApplyWinRTProjections;
			this.UseDebugSymbols = manager.UseDebugSymbols;
			ownerThread = Thread.CurrentThread;
			synchronizationContext = SynchronizationContext.Current;
			assemblies.CollectionChanged += Assemblies_CollectionChanged;
		}

		/// <summary>
		/// Loads an assembly list from XML.
		/// </summary>
		/// <param name="manager">Owning manager that supplies persistence and load-policy settings for this list.</param>
		/// <param name="listElement">XML element containing the list name and persisted <c>Assembly</c> entries to reopen.</param>
		internal AssemblyList(AssemblyListManager manager, XElement listElement)
			: this(manager, (string?)listElement.Attribute("name") ?? AssemblyListManager.DefaultListName)
		{
			foreach (var asm in listElement.Elements("Assembly"))
			{
				OpenAssembly((string)asm);
			}
			this.dirty = false; // OpenAssembly() sets dirty, so reset it afterwards
		}

		/// <summary>
		/// Creates a shallow copy of another assembly list, reusing existing <see cref="LoadedAssembly"/> instances.
		/// </summary>
		/// <param name="list">Source list whose current assembly entries are copied.</param>
		/// <param name="newName">Name assigned to the cloned list.</param>
		public AssemblyList(AssemblyList list, string newName)
			: this(list.manager, newName)
		{
			lock (lockObj)
			{
				lock (list.lockObj)
				{
					this.assemblies.AddRange(list.assemblies);
				}
			}
			this.dirty = false;
		}

		/// <summary>
		/// Raised when the list's top-level assembly collection changes.
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged {
			add {
				VerifyAccess();
				this.assemblies.CollectionChanged += value;
			}
			remove {
				VerifyAccess();
				this.assemblies.CollectionChanged -= value;
			}
		}

		/// <summary>
		/// Gets or sets whether metadata readers created for this list apply Windows Runtime projections.
		/// </summary>
		public bool ApplyWinRTProjections { get; set; }
		/// <summary>
		/// Gets or sets whether assemblies in this list should load and use debug symbol information when available.
		/// </summary>
		public bool UseDebugSymbols { get; set; }
		/// <summary>
		/// Gets the shared file-loader registry used to open files added to this list.
		/// </summary>
		public FileLoaderRegistry LoaderRegistry => this.manager.LoaderRegistry;

		/// <summary>
		/// Gets the loaded assemblies. This method is thread-safe.
		/// </summary>
		public LoadedAssembly[] GetAssemblies()
		{
			lock (lockObj)
			{
				return assemblies.ToArray();
			}
		}

		internal AssemblyListSnapshot GetSnapshot()
		{
			lock (lockObj)
			{
				return new AssemblyListSnapshot(assemblies.ToImmutableArray());
			}
		}

		/// <summary>
		/// Gets all loaded assemblies recursively, including assemblies found in bundles or packages.
		/// </summary>
		public Task<IList<LoadedAssembly>> GetAllAssemblies()
		{
			return GetSnapshot().GetAllAssembliesAsync();
		}

		/// <summary>
		/// Gets the current number of top-level assemblies in the list.
		/// </summary>
		public int Count {
			get {
				lock (lockObj)
				{
					return assemblies.Count;
				}
			}
		}

		/// <summary>
		/// Saves this assembly list to XML.
		/// </summary>
		internal XElement SaveAsXml()
		{
			return new XElement(
				"List",
				new XAttribute("name", this.ListName),
				assemblies.Where(asm => !asm.IsAutoLoaded).Select(asm => new XElement("Assembly", asm.FileName))
			);
		}

		/// <summary>
		/// Gets the name of this list.
		/// </summary>
		public string ListName {
			get { return listName; }
		}

		/// <summary>
		/// Reorders one or more assemblies within the list.
		/// </summary>
		/// <param name="assembliesToMove">Assemblies to move as a block.</param>
		/// <param name="index">Target insertion index in the remaining list after removal.</param>
		public void Move(LoadedAssembly[] assembliesToMove, int index)
		{
			VerifyAccess();
			lock (lockObj)
			{
				foreach (LoadedAssembly asm in assembliesToMove)
				{
					int nodeIndex = assemblies.IndexOf(asm);
					Debug.Assert(nodeIndex >= 0);
					if (nodeIndex < index)
						index--;
					assemblies.RemoveAt(nodeIndex);
				}
				Array.Reverse(assembliesToMove);
				foreach (LoadedAssembly asm in assembliesToMove)
				{
					assemblies.Insert(index, asm);
				}
			}
		}

		void Assemblies_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			Debug.Assert(Monitor.IsEntered(lockObj));
			if (CollectionChangeHasEffectOnSave(e))
			{
				RefreshSave();
			}
		}

		static bool CollectionChangeHasEffectOnSave(NotifyCollectionChangedEventArgs e)
		{
			// Auto-loading dependent assemblies shouldn't trigger saving the assembly list
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					return e.NewItems.EmptyIfNull().Cast<LoadedAssembly>().Any(asm => !asm.IsAutoLoaded);
				case NotifyCollectionChangedAction.Remove:
					return e.OldItems.EmptyIfNull().Cast<LoadedAssembly>().Any(asm => !asm.IsAutoLoaded);
				default:
					return true;
			}
		}

		/// <summary>
		/// Marks the list as dirty and schedules a save through the owning manager.
		/// </summary>
		public void RefreshSave()
		{
			// Whenever the assembly list is modified, mark it as dirty
			// and enqueue a task that saves it once the UI has finished modifying the assembly list.
			if (!dirty)
			{
				dirty = true;
				BeginInvoke(
					delegate {
						if (dirty)
						{
							dirty = false;
							this.manager.SaveList(this);
						}
					}
				);
			}
		}

		/// <summary>
		/// Finds an already opened assembly by file path.
		/// </summary>
		/// <param name="file">Assembly path to look up. Relative paths are normalized to full paths.</param>
		/// <returns>The loaded assembly entry, or <see langword="null"/> when no matching file is open.</returns>
		public LoadedAssembly? FindAssembly(string file)
		{
			file = Path.GetFullPath(file);
			lock (lockObj)
			{
				if (byFilename.TryGetValue(file, out var asm))
					return asm;
			}
			return null;
		}

		/// <summary>
		/// Opens an assembly using the public URI/file entry point.
		/// </summary>
		/// <param name="assemblyUri">Path or URI accepted by the active file-loader pipeline.</param>
		/// <param name="isAutoLoaded">Marks the resulting entry as dependency-loaded instead of user-loaded.</param>
		/// <returns>The existing or newly created <see cref="LoadedAssembly"/> for <paramref name="assemblyUri"/>.</returns>
		public LoadedAssembly Open(string assemblyUri, bool isAutoLoaded = false)
		{
			return OpenAssembly(assemblyUri, isAutoLoaded);
		}

		/// <summary>
		/// Opens an assembly from disk.
		/// Returns the existing assembly node if it is already loaded.
		/// </summary>
		/// <param name="file">Assembly file path.</param>
		/// <param name="isAutoLoaded"><see langword="true"/> when the assembly is being added by dependency resolution.</param>
		/// <returns>The existing or newly loaded assembly entry.</returns>
		/// <remarks>
		/// If called on the UI thread, the newly opened assembly is added to the list synchronously.
		/// If called on another thread, the newly opened assembly won't be returned by GetAssemblies()
		/// until the UI thread gets around to adding the assembly.
		/// </remarks>
		public LoadedAssembly OpenAssembly(string file, bool isAutoLoaded = false)
		{
			file = Path.GetFullPath(file);
			return OpenAssembly(file, () => {
				var newAsm = new LoadedAssembly(this, file, fileLoaders: manager?.LoaderRegistry, applyWinRTProjections: ApplyWinRTProjections, useDebugSymbols: UseDebugSymbols) {
					IsAutoLoaded = isAutoLoaded
				};
				return newAsm;
			});
		}

		/// <summary>
		/// Opens an assembly from a stream.
		/// </summary>
		/// <param name="file">Logical assembly file name used as the identity key within the list.</param>
		/// <param name="stream">Stream providing assembly bytes. May be <see langword="null"/> to defer loading.</param>
		/// <param name="isAutoLoaded"><see langword="true"/> when this entry originates from automatic dependency loading.</param>
		/// <returns>The existing or newly loaded assembly entry.</returns>
		public LoadedAssembly OpenAssembly(string file, Stream? stream, bool isAutoLoaded = false)
		{
			file = Path.GetFullPath(file);
			return OpenAssembly(file, () => {
				var newAsm = new LoadedAssembly(this, file, stream: Task.FromResult(stream),
					fileLoaders: manager?.LoaderRegistry,
					applyWinRTProjections: ApplyWinRTProjections, useDebugSymbols: UseDebugSymbols);
				newAsm.IsAutoLoaded = isAutoLoaded;
				return newAsm;
			});
		}

		LoadedAssembly OpenAssembly(string file, Func<LoadedAssembly> load)
		{
			bool isUIThread = ownerThread == Thread.CurrentThread;
			LoadedAssembly? asm;
			lock (lockObj)
			{
				if (byFilename.TryGetValue(file, out asm))
					return asm;
				asm = load();
				Debug.Assert(asm.FileName == file);
				byFilename.Add(asm.FileName, asm);

				if (isUIThread)
				{
					assemblies.Add(asm);
				}
			}
			if (!isUIThread)
			{
				BeginInvoke(delegate () {
					lock (lockObj)
					{
						assemblies.Add(asm);
					}
				});
			}
			return asm;
		}

		/// <summary>
		/// Replaces an already loaded assembly with a new object model from an in-memory stream.
		/// </summary>
		/// <param name="file">Path of the assembly entry to replace.</param>
		/// <param name="stream">Stream containing the replacement assembly image.</param>
		/// <returns>The replacement assembly entry, or <see langword="null"/> when the target is not currently loaded.</returns>
		public LoadedAssembly? HotReplaceAssembly(string file, Stream stream)
		{
			VerifyAccess();
			file = Path.GetFullPath(file);
			lock (lockObj)
			{
				if (!byFilename.TryGetValue(file, out LoadedAssembly? target))
					return null;
				int index = this.assemblies.IndexOf(target);
				if (index < 0)
					return null;

				var newAsm = new LoadedAssembly(this, file, stream: Task.FromResult<Stream?>(stream),
					fileLoaders: manager?.LoaderRegistry,
					applyWinRTProjections: ApplyWinRTProjections, useDebugSymbols: UseDebugSymbols);
				newAsm.IsAutoLoaded = target.IsAutoLoaded;

				Debug.Assert(newAsm.FileName == file);
				byFilename[file] = newAsm;
				this.assemblies[index] = newAsm;
				return newAsm;
			}
		}

		/// <summary>
		/// Reloads a loaded assembly from disk by file name while preserving its position in the list.
		/// </summary>
		/// <param name="file">Assembly path to reload.</param>
		/// <returns>The reloaded entry, or <see langword="null"/> if the assembly is not present in the list.</returns>
		public LoadedAssembly? ReloadAssembly(string file)
		{
			VerifyAccess();
			file = Path.GetFullPath(file);

			var target = this.assemblies.FirstOrDefault(asm => file.Equals(asm.FileName, StringComparison.OrdinalIgnoreCase));
			if (target == null)
				return null;

			return ReloadAssembly(target);
		}

		/// <summary>
		/// Reloads a loaded assembly entry from its original file and debug-symbol path.
		/// </summary>
		/// <param name="target">Existing assembly entry to replace.</param>
		/// <returns>The replacement entry, or <see langword="null"/> when <paramref name="target"/> is no longer in this list.</returns>
		public LoadedAssembly? ReloadAssembly(LoadedAssembly target)
		{
			VerifyAccess();
			var index = this.assemblies.IndexOf(target);
			if (index < 0)
				return null;
			var newAsm = new LoadedAssembly(this, target.FileName, pdbFileName: target.PdbFileName,
				fileLoaders: manager?.LoaderRegistry,
				applyWinRTProjections: ApplyWinRTProjections, useDebugSymbols: UseDebugSymbols);
			newAsm.IsAutoLoaded = target.IsAutoLoaded;
			lock (lockObj)
			{
				this.assemblies.Remove(target);
				this.assemblies.Insert(index, newAsm);
			}
			return newAsm;
		}

		/// <summary>
		/// Removes an assembly from the list and filename index.
		/// </summary>
		/// <param name="assembly">Assembly entry to remove.</param>
		public void Unload(LoadedAssembly assembly)
		{
			VerifyAccess();
			lock (lockObj)
			{
				assemblies.Remove(assembly);
				byFilename.Remove(assembly.FileName);
			}
		}

		/// <summary>
		/// Removes all top-level assemblies from this list.
		/// </summary>
		public void Clear()
		{
			VerifyAccess();
			lock (lockObj)
			{
				assemblies.Clear();
				byFilename.Clear();
			}
		}
		/// <summary>
		/// Sorts the full assembly list using the specified comparer.
		/// </summary>
		/// <param name="comparer">Comparer that defines the target order.</param>
		public void Sort(IComparer<LoadedAssembly> comparer)
		{
			Sort(0, int.MaxValue, comparer);
		}

		/// <summary>
		/// Sorts a range within the assembly list.
		/// </summary>
		/// <param name="index">Zero-based start index of the range to sort.</param>
		/// <param name="count">Maximum number of items to sort from <paramref name="index"/>.</param>
		/// <param name="comparer">Comparer that defines the target order.</param>
		public void Sort(int index, int count, IComparer<LoadedAssembly> comparer)
		{
			VerifyAccess();
			lock (lockObj)
			{
				List<LoadedAssembly> list = new List<LoadedAssembly>(assemblies);
				list.Sort(index, Math.Min(count, list.Count - index), comparer);
				assemblies.Clear();
				assemblies.AddRange(list);
			}
		}

		private void BeginInvoke(Action action)
		{
			if (synchronizationContext == null)
			{
				action();
			}
			else
			{
				synchronizationContext.Post(new SendOrPostCallback(_ => action()), null);
			}
		}

		private void VerifyAccess()
		{
			if (this.ownerThread != Thread.CurrentThread)
				throw new InvalidOperationException("This method must always be called on the thread that owns the assembly list: " + ownerThread.ManagedThreadId + " " + ownerThread.Name);
		}
	}
}
