// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0009 // use this directive
#pragma warning disable CA1716

// Original source from
// https://raw.githubusercontent.com/dotnet/extensions/main/src/Shared/EmptyCollections/EmptyReadOnlyList.cs


[ExcludeFromCodeCoverage]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Static field, lifetime matches the process")]
internal sealed class EmptyReadOnlyList<T> : IReadOnlyList<T>, ICollection<T>
{
    public static readonly EmptyReadOnlyList<T> Instance = new();
    private readonly Enumerator _enumerator = new();


    public IEnumerator<T> GetEnumerator()
    {
        return _enumerator;
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return _enumerator;
    }


    public int Count => 0;

    public T this[int index] => throw new ArgumentOutOfRangeException(nameof(index));


    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        // nop
    }


    bool ICollection<T>.Contains(T item)
    {
        return false;
    }


    bool ICollection<T>.IsReadOnly => true;


    void ICollection<T>.Add(T item)
    {
        throw new NotSupportedException();
    }


    bool ICollection<T>.Remove(T item)
    {
        return false;
    }


    void ICollection<T>.Clear()
    {
        // nop
    }


    internal sealed class Enumerator : IEnumerator<T>
    {
        public void Dispose()
        {
            // nop
        }


        public void Reset()
        {
            // nop
        }


        public bool MoveNext()
        {
            return false;
        }


        public T Current => throw new InvalidOperationException();
        object IEnumerator.Current => throw new InvalidOperationException();
    }
}
