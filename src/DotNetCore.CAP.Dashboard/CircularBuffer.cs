// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.CAP.Dashboard
{
    /// <summary>
    /// A generic circular buffer.
    /// </summary>
    /// <typeparam name="T">The type of items that are buffered.</typeparam>
    internal class CircularBuffer<T> : ICollection<T>
    {
        // Ring of items
        private readonly T[] _items;

        // Current length, as opposed to the total capacity
        // Current start of the list. Starts at 0, but may
        // move forwards or wrap around back to 0 due to
        // rotation.
        private int _firstIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.
        /// </summary>
        /// <param name="capacity">The maximum capacity of the buffer.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity" /> is negative.</exception>
        public CircularBuffer(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _items = new T[capacity];
            Clear();
        }

        /// <summary>
        /// Gets the maximum capacity of the buffer. If more items
        /// are added than the buffer has capacity for, then
        /// older items will be removed from the buffer with
        /// a first-in, first-out policy.
        /// </summary>
        public int Capacity => _items.Length;

        /// <summary>
        /// Whether or not the buffer is at capacity.
        /// </summary>
        public bool IsFull => Count == Capacity;

        /// <summary>
        /// Convert from a 0-based index to a buffer index which
        /// has been properly offset and wrapped.
        /// </summary>
        /// <param name="zeroBasedIndex">The index to wrap.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="zeroBasedIndex" /> is out of range.</exception>
        /// <returns>
        /// The actual index that <param ref="zeroBasedIndex" />
        /// maps to.
        /// </returns>
        private int WrapIndex(int zeroBasedIndex)
        {
            if (Capacity == 0 || zeroBasedIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(zeroBasedIndex));
            }

            return (zeroBasedIndex + _firstIndex) % Capacity;
        }

        #region IEnumerable<T> implementation.
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return _items[WrapIndex(i)];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }
        #endregion

        #region ICollection<T> implementation
        public int Count { get; private set; }

        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an item to the buffer. If the buffer is already
        /// full, the oldest item in the list will be removed,
        /// and the new item added at the logical end of the list.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            if (Capacity == 0)
            {
                return;
            }

            int itemIndex;

            if (IsFull)
            {
                itemIndex = _firstIndex;
                _firstIndex = (_firstIndex + 1) % Capacity;
            }
            else
            {
                itemIndex = _firstIndex + Count;
                Count++;
            }

            _items[itemIndex] = item;
        }

        public void Clear()
        {
            _firstIndex = 0;
            Count = 0;
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            if (Count > (array.Length - arrayIndex))
            {
                throw new ArgumentException("arrayIndex");
            }

            // Iterate through the buffer in correct order.
            foreach (T item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }
        #endregion

        /// <summary>
        /// Create an array of the items in the buffer. Items
        /// will be in the same order they were added.
        /// </summary>
        /// <returns>The new array.</returns>
        public T[] ToArray()
        {
            T[] result = new T[Count];
            CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// Access an item in the buffer. Indexing is based off
        /// of the order items were added, rather than any
        /// internal ordering the buffer may be maintaining.
        /// </summary>
        /// <param name="index">The index of the item to access.</param>
        /// <returns>The buffered item at index <paramref name="index"/>.</returns>
        public T this[int index]
        {
            get
            {
                if (!(index >= 0 && index < Count))
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _items[WrapIndex(index)];
            }
        }
    }

}
