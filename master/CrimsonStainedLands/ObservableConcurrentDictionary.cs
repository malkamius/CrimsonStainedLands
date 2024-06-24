using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CrimsonStainedLands
{
    public class ObservableConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new ConcurrentDictionary<TKey, TValue>();

        public TValue this[TKey key] 
        { get => _dictionary[key]; 
          set {
                _dictionary.TryGetValue(key, out var oldvalue);
                _dictionary[key] = value;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, oldvalue)));
            } 
        }

        public bool TryUpdate(TKey key, TValue value, TValue comparisonvalue)
        {
            var result = _dictionary.TryUpdate(key, value, comparisonvalue);
            if(result)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, comparisonvalue)));
            return result;
        }

        public ICollection<TKey> Keys => _dictionary.Keys;

        public ICollection<TValue> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Add(TKey key, TValue value)
        {

            if (!_dictionary.TryAdd(key, value))
            {
                throw new ArgumentException("Failed to add item.");
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (!_dictionary.TryAdd(item.Key, item.Value))
            {
                throw new ArgumentException("Failed to add item.");
            }
        }

        public void Clear()
        {
            _dictionary.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>) _dictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return this.TryRemove(key, out _);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (((IDictionary<TKey, TValue>)_dictionary).Remove(item))
            {
                OnItemRemoved(item.Key, item.Value);
                return true;
            }
            return false;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            bool added = _dictionary.TryAdd(key, value);
            if (added)
            {
                OnItemAdded(key, value);
            }
            return added;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            bool removed = _dictionary.TryRemove(key, out value);
            if (removed)
            {
                OnItemRemoved(key, value);
            }
            return removed;
        }

        // Implement other methods as needed, wrapping the internal _dictionary

        protected virtual void OnItemAdded(TKey key, TValue value)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
        }

        protected virtual void OnItemRemoved(TKey key, TValue value)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value)));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
