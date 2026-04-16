using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace IVSoftware.Portable.Disposable
{
    partial class AuthorityEpochProvider : IDictionary<string, object>
    {
        public object this[string key] 
        {
            get => ((IDictionary<string, object>)DHost)[key];
            set => ((IDictionary<string, object>)DHost)[key] = value;
        }

        public ICollection<string> Keys => ((IDictionary<string, object>)DHost).Keys;

        public ICollection<object> Values => ((IDictionary<string, object>)DHost).Values;

        public int Count => ((ICollection<KeyValuePair<string, object>>)DHost).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, object>>)DHost).IsReadOnly;

        public void Add(string key, object value)
        {
            ((IDictionary<string, object>)DHost).Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            ((ICollection<KeyValuePair<string, object>>)DHost).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, object>>)DHost).Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)DHost).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, object>)DHost).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)DHost).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object>>)DHost).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, object>)DHost).Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)DHost).Remove(item);
        }

        public bool TryGetValue(string key, out object value)
        {
            return ((IDictionary<string, object>)DHost).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)DHost).GetEnumerator();
        }
    }
}
