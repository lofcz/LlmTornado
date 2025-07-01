#if !MODERN
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;

namespace System.Collections.Frozen
{
    public sealed class FrozenDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dictionary;

        internal FrozenDictionary(Dictionary<TKey, TValue> dictionary)
        {
            _dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        public TValue this[TKey key] => _dictionary[key];

        public IEnumerable<TKey> Keys => _dictionary.Keys;

        public IEnumerable<TValue> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

        public TValue GetValueOrDefault(TKey key) => _dictionary.GetValueOrDefault(key);

        public TValue GetValueOrDefault(TKey key, TValue defaultValue) => _dictionary.GetValueOrDefault(key, defaultValue);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class FrozenDictionary
    {
        public static FrozenDictionary<TKey, TValue> ToFrozenDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            Dictionary<TKey, TValue> dictionary = source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return new FrozenDictionary<TKey, TValue>(dictionary);
        }

        public static FrozenDictionary<TKey, TValue> ToFrozenDictionary<TSource, TKey, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector)
        {
            Dictionary<TKey, TValue> dictionary = source.ToDictionary(keySelector, valueSelector);
            return new FrozenDictionary<TKey, TValue>(dictionary);
        }
    }
}
#endif
