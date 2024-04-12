using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utilities;

public sealed record BitSet(int Count = 64) : ISet<int>
{
    public const int BitsPerByte = 8;
    public const int BitsPerLong = sizeof(long) * BitsPerByte;
    public static int GetLastAlignedInnerIndex(int Count) => (Count - 1) % BitsPerLong;
    public static int GetLastAlignedStoreIndex(int Count) => (Count - 1) / BitsPerLong;
    public static int TrimLast(long[] Bits, int Count)
    {
        var ls = GetLastAlignedStoreIndex(Count) * BitsPerLong;
        Count = Count <= ls ? Count : ls;
        if (Bits.Length > 0)
        {
            var LastStore = Bits[^1];
            for (int i = GetLastAlignedInnerIndex(Count);
                i >= 0;
                i--)
                LastStore &= ~(1L << i);

            Bits[^1] = LastStore;
        }
        return Count;
    }
    public readonly long[] BitsBuffer
        = new long[(Count + BitsPerLong - 1) / BitsPerLong];
    public bool this[int index]
    {
        get => index >= 0 && index < Count
            ? 0 != (this.BitsBuffer[index / BitsPerLong] & (1 << index % BitsPerLong))
            : throw new IndexOutOfRangeException(nameof(index))
            ;
        set
        {
            if (index >= 0 && index < Count)
                if (value)
                    this.BitsBuffer[index / BitsPerLong] |= (1L << index % BitsPerLong);
                else
                    this.BitsBuffer[index / BitsPerLong] &= ~(1L << index % BitsPerLong);
            else
                throw new IndexOutOfRangeException(nameof(index));
        }
    }
    public int Count { get; private set; } = Count;
    public bool IsReadOnly => false;
    public BitSet(IEnumerable<int> e)
        : this((e is ICollection c) ? c.Count : (e.Max(e => e) + 1))
    {
        if (e is BitSet s)
            this.Count = TrimLast(
                this.BitsBuffer = (long[])s.BitsBuffer.Clone(), s.Count);
        else
            foreach (var i in e)
                if (i >= 0 && i < Count)
                    this[i] = true;
                else throw new ArgumentOutOfRangeException(
                    $"elements in e should be within [0..{Count}]");
    }
    public bool Add(int item) => item < 0 || item >= this.Count
            ? throw new ArgumentOutOfRangeException(nameof(item))
            : this[item] ? false : (this[item] = true);
    void ICollection<int>.Add(int item) => this.Add(item);
    public bool Remove(int item)
        => item < 0 || item >= Count
        ? throw new ArgumentOutOfRangeException(nameof(item))
        : !(this[item] = false);
    public void Clear()
        => Array.Clear(this.BitsBuffer, 0, this.BitsBuffer.Length);
    public bool Contains(int item)
        => item >= 0 && item < this.Count && this[item];
    public void CopyTo(int[] array, int arrayIndex)
    {
        for (int i = 0; i < Count; i++)
            if (this[i])
            {
                array[arrayIndex++] = i;
                if (arrayIndex >= array.Length)
                    throw new ArgumentOutOfRangeException(
                        nameof(array),
                        "This BinarySet contains more elements " +
                        "than the array parameter can hold " +
                        "starting from arrayIndex");
            }
    }
    public IEnumerator<int> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            while (!this[i]) i++;
            yield return i;
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable<int>)this).GetEnumerator();
    public void ExceptWith(IEnumerable<int> other)
    {
        foreach (var i in other) if (i >= 0 && i < this.Count) this[i] = false;
    }
    public void UnionWith(IEnumerable<int> other)
    {
        foreach (var i in other) if (i >= 0 && i < this.Count) this[i] = true;
    }
    public bool SetEquals(IEnumerable<int> other)
    {
        var otherSet = new BitSet(other);
        if (this.Count != otherSet.Count) return false;
        for (int i = 0; i < this.BitsBuffer.Length; i++)
            if (this.BitsBuffer[i] != otherSet.BitsBuffer[i]) return false;
        return true;
    }
    public bool Overlaps(IEnumerable<int> other)
    {
        foreach (var i in other) if (i >= 0 && i < this.Count && this[i]) return true;
        return false;
    }
    public bool IsSupersetOf(IEnumerable<int> other)
    {
        foreach (var i in other) if (!this[i]) return false;
        return true;
    }
    public bool IsSubsetOf(IEnumerable<int> other)
        => new BitSet(other).IsSupersetOf(this);
    public void IntersectWith(IEnumerable<int> other)
    {
        if (other is BitSet that)
            for (int i = 0, count = Math.Min(this.BitsBuffer.Length, that.BitsBuffer.Length); i < count; i++)
                this.BitsBuffer[i] &= that.BitsBuffer[i];
        else
            foreach (var i in other)
                if (i >= 0 && i < this.Count) this[i] &= true;
    }
    public bool IsProperSubsetOf(IEnumerable<int> other)
      => this.IsSubsetOf(other) &&
           !new BitSet(other).IsSubsetOf(this);
    public bool IsProperSupersetOf(IEnumerable<int> other)
        => this.IsSupersetOf(other) &&
           !new BitSet(other).IsSupersetOf(this);
    public void SymmetricExceptWith(IEnumerable<int> other)
    {
        var copy = new BitSet(this);
        copy.IntersectWith(other);
        this.UnionWith(other);
        this.ExceptWith(copy);
    }
}
