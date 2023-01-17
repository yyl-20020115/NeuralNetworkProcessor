using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetworkProcessor.Utils
{
    public class Lookups<TKey, TValue> : IEnumerable<KeyValuePair<TKey, List<TValue>>>
    {
        public bool HasChanged(Lookups<TKey, TValue> other)
        {
            if (other == null) return true;

            if (this.Count != other.Count) return true;
            foreach (var key in this.Keys)
            {
                if (!other.ContainsKey(key)) return true;
                var myval = this[key];
                var otval = other[key];
                if (myval.Count != otval.Count) return true;
                for (int i = 0; i < myval.Count; i++)
                {
                    if (!object.ReferenceEquals(myval[i], otval[i])) return true;
                }
            }
            return false;
        }
        public Dictionary<TKey, List<TValue>> Data { get; protected set; } = new();

        public int Count => this.Data.Count;
        public int TotalValuesCount => this.Values.Sum(v => v.Count);
        public Lookups() { }
        public Lookups(IEnumerable<KeyValuePair<TKey,List<TValue>>> collection)
        {
            this.Data = new Dictionary<TKey, List<TValue>>(collection);
        }
        public Lookups<TKey, TValue> Clone() 
            => new() { Data = new Dictionary<TKey, List<TValue>>(Data) };
        public Dictionary<TKey,List<TValue>>.KeyCollection Keys => this.Data.Keys;
        public Dictionary<TKey, List<TValue>>.ValueCollection Values => this.Data.Values;

        public bool ContainsKey(TKey key) => this.Data.ContainsKey(key);
        public bool ContainsList(List<TValue> list) => this.Data.ContainsValue(list);
        public bool ContainsValue(TValue value) => this.Data.Values.SelectMany(s=>s).Contains(value);

        public void Add(TKey key, TValue value) {
            if (this.Data.TryGetValue(key, out var list))
            {
                list.Add(value);
            }
            else
            {
                this.Data.Add(key, new List<TValue> { value });
            }
        }
        public List<TValue> this[TKey key]
        {
            get
            {
                if(!this.Data.TryGetValue(key, out var list))
                    this.Data.Add(key, list = new List<TValue>());
                
                return list;
            }
        }
        public bool Remove(TKey key) => this.Data.Remove(key);
        public void Clear() => this.Data.Clear();
        IEnumerator IEnumerable.GetEnumerator() => this.Data.GetEnumerator();

        public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, List<TValue>>>)Data).GetEnumerator();
    }
}
