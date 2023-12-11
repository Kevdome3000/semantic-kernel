// Copyright (c) Microsoft. All rights reserved.

<<<<<<< HEAD

namespace Microsoft.SemanticKernel.ChatCompletion;

====== =
>>>>>>> upstream/main
using System;
using System.Collections;
using System.Collections.Generic;
<<<<<<< HEAD
#pragma warning disable CA1033 // Interface methods should be callable by child types
    ====== =

namespace Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable CA1033 // Interface methods should be callable by child types
>>>>>>> upstream / main


/// <summary>
/// Contains collection of chat message content items of type <see cref="ContentBase"/>.
/// </summary>
public class ChatMessageContentItemCollection : IList<ContentBase>, IReadOnlyList<ContentBase>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageContentItemCollection"/> class.
    /// </summary>
    public ChatMessageContentItemCollection()
    {
        this._items = new();
    }
    <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main

    /// <summary>
    /// Gets or sets the content item at the specified index in the collection.
    /// </summary>
    /// <param name="index">The index of the content item to get or set.</param>
    /// <returns>The content item at the specified index.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> was not valid for this collection.</exception>
    public ContentBase this[int index]
    {
        get => this._items[index];
        set
        {
            Verify.NotNull(value);
            this._items[index] = value;
        }
    }

    /// <summary>
    /// Gets the number of content items in the collection.
    /// </summary>
    public int Count => this._items.Count;

        <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main


    /// <summary>
    /// Adds a content item to the collection.
    /// </summary>
    /// <param name="item">The content item to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
    public void Add(ContentBase item)
    {
        Verify.NotNull(item);
        this._items.Add(item);
    }
    <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main


    /// <summary>
    /// Removes all content items from the collection.
    /// </summary>
    public void Clear() => this._items.Clear();
        <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main


    /// <summary>
    /// Determines whether a content item is in the collection.
    /// </summary>
    /// <param name="item">The content item to locate.</param>
    /// <returns>True if the content item is found in the collection; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
    public bool Contains(ContentBase item)
    {
        Verify.NotNull(item);
        return this._items.Contains(item);
    }
    <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main


    /// <summary>
    /// Copies all of the content items in the collection to an array, starting at the specified destination array index.
    /// </summary>
    /// <param name="array">The destination array into which the content items should be copied.</param>
    /// <param name="arrayIndex">The zero-based index into <paramref name="array"/> at which copying should begin.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
    /// <exception cref="ArgumentException">The number of content items in the collection is greater than the available space from <paramref name="arrayIndex"/> to the end of <paramref name="array"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
    public void CopyTo(ContentBase[] array, int arrayIndex) => this._items.CopyTo(array, arrayIndex);
        <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main


    /// <summary>
    /// Searches for the specified content item and returns the index of the first occurrence.
    /// </summary>
    /// <param name="item">The content item to locate.</param>
    /// <returns>The index of the first found occurrence of the specified content item; -1 if the content item could not be found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
    public int IndexOf(ContentBase item)
    {
        Verify.NotNull(item);
        return this._items.IndexOf(item);
    }
    <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main


    /// <summary>
    /// Inserts a content item into the collection at the specified index.
    /// </summary>
    /// <param name="index">The index at which the content item should be inserted.</param>
    /// <param name="item">The content item to insert.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
    public void Insert(int index, ContentBase item)
    {
        Verify.NotNull(item);
        this._items.Insert(index, item);
    }
    <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main


    /// <summary>
    /// Removes the first occurrence of the specified content item from the collection.
    /// </summary>
    /// <param name="item">The content item to remove from the collection.</param>
    /// <returns>True if the item was successfully removed; false if it wasn't located in the collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
    public bool Remove(ContentBase item)
    {
        Verify.NotNull(item);
        return this._items.Remove(item);
    }
    <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main


    /// <summary>
    /// Removes the content item at the specified index from the collection.
    /// </summary>
    /// <param name="index">The index of the content item to remove.</param>
    public void RemoveAt(int index) => this._items.RemoveAt(index);
        <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main
    bool ICollection<ContentBase>.IsReadOnly => false;

    IEnumerator IEnumerable.GetEnumerator() => this._items.GetEnumerator();


    IEnumerator<ContentBase> IEnumerable<ContentBase>.GetEnumerator() => this._items.GetEnumerator();
        <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main


    #region private

    private readonly List<ContentBase> _items;

        #endregion


        <<<<<<< HEAD

    ====== =
    >>>>>>> upstream/main
}
