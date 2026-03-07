// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace LightJson
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;

	/// <summary>
	/// Represents a JSON object as an ordered-by-insertion key/value map.
	/// </summary>
	/// <remarks>
	/// Missing keys are intentionally read as <see cref="JsonValue.Null"/> instead of raising exceptions,
	/// which simplifies tolerant traversal of optional metadata fields.
	/// </remarks>
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(JsonObjectDebugView))]
	internal sealed class JsonObject : IEnumerable<KeyValuePair<string, JsonValue>>, IEnumerable<JsonValue>
	{
		private IDictionary<string, JsonValue> properties;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonObject"/> class.
		/// </summary>
		public JsonObject()
		{
			this.properties = new Dictionary<string, JsonValue>();
		}

		/// <summary>
		/// Gets the number of properties in this JsonObject.
		/// </summary>
		/// <value>The number of properties in this JsonObject.</value>
		public int Count {
			get {
				return this.properties.Count;
			}
		}

		/// <summary>
		/// Gets or sets the value associated with <paramref name="key"/>.
		/// </summary>
		/// <param name="key">The property name.</param>
		/// <value>
		/// The stored value for <paramref name="key"/>. The getter returns <see cref="JsonValue.Null"/> when the key is absent.
		/// </value>
		public JsonValue this[string key] {
			get {
				JsonValue value;

				if (this.properties.TryGetValue(key, out value))
				{
					return value;
				}
				else
				{
					return JsonValue.Null;
				}
			}

			set {
				this.properties[key] = value;
			}
		}

		/// <summary>
		/// Adds a key with a null value to this collection.
		/// </summary>
		/// <param name="key">The key of the property to be added.</param>
		/// <remarks>Returns this JsonObject.</remarks>
		/// <returns>The <see cref="JsonObject"/> that was added.</returns>
		public JsonObject Add(string key)
		{
			return this.Add(key, JsonValue.Null);
		}

		/// <summary>
		/// Adds a new property.
		/// </summary>
		/// <param name="key">The property name to add.</param>
		/// <param name="value">The value to assign to <paramref name="key"/>.</param>
		/// <returns>The current object, allowing fluent construction.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> already exists.</exception>
		public JsonObject Add(string key, JsonValue value)
		{
			this.properties.Add(key, value);
			return this;
		}

		/// <summary>
		/// Removes a property with the given key.
		/// </summary>
		/// <param name="key">The key of the property to be removed.</param>
		/// <returns>
		/// Returns true if the given key is found and removed; otherwise, false.
		/// </returns>
		public bool Remove(string key)
		{
			return this.properties.Remove(key);
		}

		/// <summary>
		/// Clears the contents of this collection.
		/// </summary>
		/// <returns>Returns this JsonObject.</returns>
		public JsonObject Clear()
		{
			this.properties.Clear();
			return this;
		}

		/// <summary>
		/// Changes the key of one of the items in the collection.
		/// </summary>
		/// <remarks>
		/// This method has no effects if the <i>oldKey</i> does not exists.
		/// If the <i>newKey</i> already exists, the value will be overwritten.
		/// </remarks>
		/// <param name="oldKey">The name of the key to be changed.</param>
		/// <param name="newKey">The new name of the key.</param>
		/// <returns>Returns this JsonObject.</returns>
		public JsonObject Rename(string oldKey, string newKey)
		{
			if (oldKey == newKey)
			{
				// Renaming to the same name just does nothing
				return this;
			}

			JsonValue value;

			if (this.properties.TryGetValue(oldKey, out value))
			{
				this[newKey] = value;
				this.Remove(oldKey);
			}

			return this;
		}

		/// <summary>
		/// Determines whether this collection contains an item assosiated with the given key.
		/// </summary>
		/// <param name="key">The key to locate in this collection.</param>
		/// <returns>Returns true if the key is found; otherwise, false.</returns>
		public bool ContainsKey(string key)
		{
			return this.properties.ContainsKey(key);
		}

		/// <summary>
		/// Determines whether this collection contains the given JsonValue.
		/// </summary>
		/// <param name="value">The value to locate in this collection.</param>
		/// <returns>Returns true if the value is found; otherwise, false.</returns>
		public bool Contains(JsonValue value)
		{
			return this.properties.Values.Contains(value);
		}

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		/// <returns>The enumerator that iterates through this collection.</returns>
		public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator()
		{
			return this.properties.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		/// <returns>The enumerator that iterates through this collection.</returns>
		IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator()
		{
			return this.properties.Values.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through this collection.
		/// </summary>
		/// <returns>The enumerator that iterates through this collection.</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		[ExcludeFromCodeCoverage]
		private class JsonObjectDebugView
		{
			private JsonObject jsonObject;

			public JsonObjectDebugView(JsonObject jsonObject)
			{
				this.jsonObject = jsonObject;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public KeyValuePair[] Keys {
				get {
					var keys = new KeyValuePair[this.jsonObject.Count];

					var i = 0;
					foreach (var property in this.jsonObject)
					{
						keys[i] = new KeyValuePair(property.Key, property.Value);
						i += 1;
					}

					return keys;
				}
			}

			[DebuggerDisplay("{value.ToString(),nq}", Name = "{key}", Type = "JsonValue({Type})")]
			public class KeyValuePair
			{
				[DebuggerBrowsable(DebuggerBrowsableState.Never)]
				private string key;

				[DebuggerBrowsable(DebuggerBrowsableState.Never)]
				private JsonValue value;

				public KeyValuePair(string key, JsonValue value)
				{
					this.key = key;
					this.value = value;
				}

				[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
				public object View {
					get {
						if (this.value.IsJsonObject)
						{
							return (JsonObject)this.value;
						}
						else if (this.value.IsJsonArray)
						{
							return (JsonArray)this.value;
						}
						else
						{
							return this.value;
						}
					}
				}

				[DebuggerBrowsable(DebuggerBrowsableState.Never)]
				private JsonValueType Type {
					get {
						return this.value.Type;
					}
				}
			}
		}
	}
}
