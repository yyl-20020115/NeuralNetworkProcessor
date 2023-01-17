using System;
using System.Diagnostics;

namespace SharpLua
{
    public partial class Lua
    {
        public class CharPtr
        {
            public char[] chars;
            public int index;

            public char this[int offset]
            {
                get => chars[index + offset];
                set => chars[index + offset] = value;
            }
            public char this[uint offset]
            {
                get => chars[index + offset];
                set => chars[index + offset] = value;
            }
            public char this[long offset]
            {
                get => chars[index + (int)offset];
                set => chars[index + (int)offset] = value;
            }

            public static implicit operator CharPtr(string str) => new(str);
            public static implicit operator CharPtr(char[] chars) => new(chars);
            public CharPtr()
            {
                this.chars = null;
                this.index = 0;
            }
            public CharPtr(string str)
            {
                this.chars = ((str ?? "") + '\0').ToCharArray();
                this.index = 0;
            }
            public CharPtr(CharPtr ptr, int index = 0)
            {
                this.chars = ptr.chars;
                this.index = index;
            }
            public CharPtr(char[] chars, int index = 0)
            {
                this.chars = chars;
                this.index = index;
            }
            public CharPtr(IntPtr ptr)
            {
                this.chars = new char[0];
                this.index = 0;
            }
            public static CharPtr operator +(CharPtr ptr, int offset)
                => new(ptr.chars, ptr.index + offset);
            public static CharPtr operator -(CharPtr ptr, int offset)
                => new(ptr.chars, ptr.index - offset);
            public static CharPtr operator +(CharPtr ptr, uint offset)
                => new(ptr.chars, ptr.index + (int)offset);
            public static CharPtr operator -(CharPtr ptr, uint offset)
                => new(ptr.chars, ptr.index - (int)offset);
            public void inc() => this.index++;
            public void dec() => this.index--;
            public CharPtr next() => new(this.chars, this.index + 1);
            public CharPtr prev() => new(this.chars, this.index - 1);
            public CharPtr add(int ofs) => new(this.chars, this.index + ofs);
            public CharPtr sub(int ofs) => new(this.chars, this.index - ofs);
            public static bool operator ==(CharPtr ptr, char ch) => ptr[0] == ch;
            public static bool operator ==(char ch, CharPtr ptr) => ptr[0] == ch;
            public static bool operator !=(CharPtr ptr, char ch) => ptr[0] != ch;
            public static bool operator !=(char ch, CharPtr ptr) => ptr[0] != ch;

            public static CharPtr operator +(CharPtr ptr1, CharPtr ptr2)
            {
                var result = "";
                for (int i = 0; ptr1[i] != '\0'; i++)
                    result += ptr1[i];
                for (int i = 0; ptr2[i] != '\0'; i++)
                    result += ptr2[i];
                return new CharPtr(result);
            }
            public static int operator -(CharPtr ptr1, CharPtr ptr2)
            {
                Debug.Assert(ptr1.chars == ptr2.chars);
                return ptr1.index - ptr2.index;
            }
            public static bool operator <(CharPtr ptr1, CharPtr ptr2)
            {
                Debug.Assert(ptr1.chars == ptr2.chars);
                return ptr1.index < ptr2.index;
            }
            public static bool operator <=(CharPtr ptr1, CharPtr ptr2)
            {
                Debug.Assert(ptr1.chars == ptr2.chars);
                return ptr1.index <= ptr2.index;
            }
            public static bool operator >(CharPtr ptr1, CharPtr ptr2)
            {
                Debug.Assert(ptr1.chars == ptr2.chars);
                return ptr1.index > ptr2.index;
            }
            public static bool operator >=(CharPtr ptr1, CharPtr ptr2)
            {
                Debug.Assert(ptr1.chars == ptr2.chars);
                return ptr1.index >= ptr2.index;
            }
            public static bool operator ==(CharPtr o1, CharPtr o2)
            {
                if ((o1 == null) && (o2 == null)) return true;
                if (o1 == null) return false;
                if (o2 == null) return false;
                return (o1.chars == o2.chars) && (o1.index == o2.index);
            }
            public static bool operator !=(CharPtr ptr1, CharPtr ptr2) 
                => !(ptr1 == ptr2);
            public override bool Equals(object o) 
                => this == (o as CharPtr);
            public override int GetHashCode() => 0;
            public override string ToString()
            {
                var result = "";
                for (int i = index; (i < chars.Length) && (chars[i] != '\0'); i++)
                    result += chars[i];
                return result;
            }
        }
    }
}
