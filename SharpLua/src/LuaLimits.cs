//#define lua_assert

/*
** $Id: llimits.h,v 1.69.1.1 2007/12/27 13:02:25 roberto Exp $
** Limits, basic types, and some other `installation-dependent' definitions
** See Copyright Notice in lua.h
*/

using System;
using System.Diagnostics;

namespace SharpLua
{
    using lu_mem = System.UInt32;
    using lu_byte = System.Byte;
    using lua_Number = System.Double;

    public partial class Lua
    {

        //typedef LUAI_UINT32 lu_int32;
        //typedef LUAI_UMEM lu_mem;
        //typedef LUAI_MEM l_mem;

        /* chars used as small naturals (so that `char' is reserved for characters) */
        //typedef unsigned char lu_byte;


        public const uint MAX_SIZET = uint.MaxValue - 2;

        public const lu_mem MAX_LUMEM = lu_mem.MaxValue - 2;


        public const int MAX_INT = (Int32.MaxValue - 2);  /* maximum value of an int (-2 for safety) */

        /*
		** conversion of pointer to integer
		** this is for hashing only; there is no problem if the integer
		** cannot hold the whole pointer value
		*/
        //#define IntPoint(p)  ((uint)(lu_mem)(p))

        /* type to ensure maximum alignment */
        //typedef LUAI_USER_ALIGNMENT_T L_Umaxalign;

        /* result of a `usual argument conversion' over lua_Number */
        //typedef LUAI_UACNUMBER l_uacNumber;


        /* internal assertions for in-house debugging */

#if lua_assert

		[Conditional("DEBUG")]
		internal static void lua_assert(bool c) {Debug.Assert(c);}

		[Conditional("DEBUG")]
		internal static void lua_assert(int c) { Debug.Assert(c != 0); }

		internal static object check_exp(bool c, object e)		{lua_assert(c); return e;}
		internal static object check_exp(int c, object e) { lua_assert(c != 0); return e; }

#else

        [Conditional("DEBUG")]
        internal static void lua_assert(bool c) { }

        [Conditional("DEBUG")]
        internal static void lua_assert(int c) { }

        internal static object check_exp(bool c, object e) => e;
        internal static object check_exp(int c, object e) => e;

#endif

        [Conditional("DEBUG")]
        internal static void api_check(object o, bool e) => lua_assert(e);
        internal static void api_check(object o, int e) => lua_assert(e != 0);

        //#define UNUSED(x)	((void)(x))	/* to avoid warnings */

        internal static lu_byte cast_byte(int i) => (lu_byte)i;
        internal static lu_byte cast_byte(long i) => (lu_byte)(int)i;
        internal static lu_byte cast_byte(bool i) => i ? (lu_byte)1 : (lu_byte)0;
        internal static lu_byte cast_byte(lua_Number i) => (lu_byte)i;
        internal static lu_byte cast_byte(object i) => (lu_byte)(int)(i);
        internal static int cast_int(int i) => i;
        internal static int cast_int(uint i) => (int)i;
        internal static int cast_int(long i) => (int)i;
        internal static int cast_int(ulong i) => (int)i;
        internal static int cast_int(bool i) => i ? 1 : 0;
        internal static int cast_int(lua_Number i) => (int)i;
        internal static int cast_int(object i) { Debug.Assert(false, "Can't convert int."); return Convert.ToInt32(i); }

        internal static lua_Number cast_num(int i) => (lua_Number)i;
        internal static lua_Number cast_num(uint i) => (lua_Number)i;
        internal static lua_Number cast_num(long i) => (lua_Number)i;
        internal static lua_Number cast_num(ulong i) => (lua_Number)i;
        internal static lua_Number cast_num(bool i) => i ? (lua_Number)1 : (lua_Number)0;
        internal static lua_Number cast_num(object i) { 
            Debug.Assert(false, "Can't convert number."); 
            return Convert.ToSingle(i); 
        }

        /*
		** type for virtual-machine instructions
		** must be an unsigned with (at least) 4 bytes (see details in lopcodes.h)
		*/
        //typedef lu_int32 Instruction;

        /* maximum stack for a Lua function */
        public const int MAXSTACK = 250;

        /* minimum size for the string table (must be power of 2) */
        public const int MINSTRTABSIZE = 32;

        /* minimum size for string buffer */
        public const int LUA_MINBUFFER = 32;


#if !lua_lock
        public static void lua_lock(lua_State L) { }
        public static void lua_unlock(lua_State L) { }
#endif

#if !luai_threadyield
        public static void luai_threadyield(lua_State L) 
        { 
            lua_unlock(L); 
            lua_lock(L); 
        }
#endif
        /*
		** macro to control inclusion of some hard tests on stack reallocation
		*/
        //#ifndef HARDSTACKTESTS
        //#define condhardstacktests(x)	((void)0)
        //#else
        //#define condhardstacktests(x)	x
        //#endif

    }
}
