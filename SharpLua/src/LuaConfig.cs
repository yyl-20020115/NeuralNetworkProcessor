/*
** $Id: luaconf.h,v 1.82.1.7 2008/02/11 16:25:08 roberto Exp $
** Configuration file for Lua
** See Copyright Notice in lua.h
*/

using System;
using System.IO;
using System.Diagnostics;

namespace SharpLua
{
    using LUA_INTEGER = System.Int32;
    using LUA_NUMBER = System.Double;
    using LUAI_UACNUMBER = System.Double;
    using LUA_INTFRM_T = System.Int64;
    using TValue = Lua.lua_TValue;
    using lua_Number = System.Double;
    using System.Globalization;
    using System.Collections;
    public partial class Lua
    {
        /*
		** ==================================================================
		** Search for "@@" to find all configurable definitions.
		** ===================================================================
		*/

        /*
		@@ LUA_ANSI controls the use of non-ansi features.
		** CHANGE it (define it) if you want Lua to avoid the use of any
		** non-ansi feature or library.
		*/
        //#if defined(__STRICT_ANSI__)
        //#define LUA_ANSI
        //#endif

        //#if !defined(LUA_ANSI) && _WIN32
        //#define LUA_WIN
        //#endif

        //#if defined(LUA_USE_LINUX)
        //#define LUA_USE_POSIX
        //#define LUA_USE_DLOPEN		/* needs an extra library: -ldl */
        //#define LUA_USE_READLINE	/* needs some extra libraries */
        //#endif

        //#if defined(LUA_USE_MACOSX)
        //#define LUA_USE_POSIX
        //#define LUA_DL_DYLD		/* does not need extra library */
        //#endif

        /*
		@@ LUA_USE_POSIX includes all functionallity listed as X/Open System
		@* Interfaces Extension (XSI).
		** CHANGE it (define it) if your system is XSI compatible.
		*/
        //#if defined(LUA_USE_POSIX)
        //#define LUA_USE_MKSTEMP
        //#define LUA_USE_ISATTY
        //#define LUA_USE_POPEN
        //#define LUA_USE_ULONGJMP
        //#endif

        /*
		@@ LUA_PATH and LUA_CPATH are the names of the environment variables that
		@* Lua check to set its paths.
		@@ LUA_INIT is the name of the environment variable that Lua
		@* checks for initialization code.
		** CHANGE them if you want different names.
		*/
        public const string LUA_PATH = "LUA_PATH";
        public const string LUA_CPATH = "LUA_CPATH";
        public const string LUA_INIT = "LUA_INIT";

        /*
		@@ LUA_PATH_DEFAULT is the default path that Lua uses to look for
		@* Lua libraries.
		@@ LUA_CPATH_DEFAULT is the default path that Lua uses to look for
		@* C libraries.
		** CHANGE them if your machine has a non-conventional directory
		** hierarchy or if you want to install your libraries in
		** non-conventional directories.
		*/
#if _WIN32
        /*
		** In Windows, any exclamation mark ('!') in the path is replaced by the
		** path of the directory of the executable file of the current process.
		*/
        public const string LUA_LDIR = "!\\lua\\";
        public const string LUA_CDIR = "!\\";
        public const string LUA_PATH_DEFAULT =
                ".\\?.lua;" + LUA_LDIR + "?.lua;" + LUA_LDIR + "?\\init.lua;"
                             + LUA_CDIR + "?.lua;" + LUA_CDIR + "?\\init.lua";
        public const string LUA_CPATH_DEFAULT =
            ".\\?.dll;" + LUA_CDIR + "?.dll;" + LUA_CDIR + "loadall.dll";

#else
		public const string LUA_ROOT	= "/usr/local/";
		public const string LUA_LDIR	= LUA_ROOT + "share/lua/5.1/";
		public const string LUA_CDIR	= LUA_ROOT + "lib/lua/5.1/";
		public const string LUA_PATH_DEFAULT  =
				"./?.lua;"  + LUA_LDIR + "?.lua;"  + LUA_LDIR + "?/init.lua;" +
							LUA_CDIR + "?.lua;"  + LUA_CDIR + "?/init.lua";
		public const string LUA_CPATH_DEFAULT =
			"./?.so;"  + LUA_CDIR + "?.so;" + LUA_CDIR + "loadall.so";
#endif


        /*
		@@ LUA_DIRSEP is the directory separator (for submodules).
		** CHANGE it if your machine does not use "/" as the directory separator
		** and is not Windows. (On Windows Lua automatically uses "\".)
		*/
        public static string LUA_DIRSEP = Path.DirectorySeparatorChar.ToString();// "\\";

        /*
		@@ LUA_PATHSEP is the character that separates templates in a path.
		@@ LUA_PATH_MARK is the string that marks the substitution points in a
		@* template.
		@@ LUA_EXECDIR in a Windows path is replaced by the executable's
		@* directory.
		@@ LUA_IGMARK is a mark to ignore all before it when bulding the
		@* luaopen_ function name.
		** CHANGE them if for some reason your system cannot use those
		** characters. (E.g., if one of those characters is a common character
		** in file/directory names.) Probably you do not need to change them.
		*/
        public const string LUA_PATHSEP = ";";
        public const string LUA_PATH_MARK = "?";
        public const string LUA_EXECDIR = "!";
        public const string LUA_IGMARK = "-";

        /*
		@@ LUA_INTEGER is the integral type used by lua_pushinteger/lua_tointeger.
		** CHANGE that if ptrdiff_t is not adequate on your machine. (On most
		** machines, ptrdiff_t gives a good choice between int or long.)
		*/
        //#define LUA_INTEGER	ptrdiff_t

        /*
		@@ LUA_API is a mark for all core API functions.
		@@ LUALIB_API is a mark for all standard library functions.
		** CHANGE them if you need to define those functions in some special way.
		** For instance, if you want to create one Windows DLL with the core and
		** the libraries, you may want to use the following definition (define
		** LUA_BUILD_AS_DLL to get it).
		*/
        //#if LUA_BUILD_AS_DLL

        //#if defined(LUA_CORE) || defined(LUA_LIB)
        //#define LUA_API __declspec(dllexport)
        //#else
        //#define LUA_API __declspec(dllimport)
        //#endif

        //#else

        //#define LUA_API		extern

        //#endif

        /* more often than not the libs go together with the core */
        //#define LUALIB_API	LUA_API

        /*
		@@ LUAI_FUNC is a mark for all extern functions that are not to be
		@* exported to outside modules.
		@@ LUAI_DATA is a mark for all extern (const) variables that are not to
		@* be exported to outside modules.
		** CHANGE them if you need to mark them in some special way. Elf/gcc
		** (versions 3.2 and later) mark them as "hidden" to optimize access
		** when Lua is compiled as a shared library.
		*/
        //#if defined(luaall_c)
        //#define LUAI_FUNC	static
        //#define LUAI_DATA	/* empty */

        //#elif defined(__GNUC__) && ((__GNUC__*100 + __GNUC_MINOR__) >= 302) && \
        //      defined(__ELF__)
        //#define LUAI_FUNC	__attribute__((visibility("hidden"))) extern
        //#define LUAI_DATA	LUAI_FUNC

        //#else
        //#define LUAI_FUNC	extern
        //#define LUAI_DATA	extern
        //#endif

        /*
		@@ LUA_QL describes how error messages quote program elements.
		** CHANGE it if you want a different appearance.
		*/
        public static CharPtr LUA_QL(string x) => "'" + x + "'";
        public static CharPtr LUA_QS => LUA_QL("%s");

        /*
		@@ LUA_IDSIZE gives the maximum size for the description of the source
		@* of a function in debug information.
		** CHANGE it if you want a different size.
		*/
        public const int LUA_IDSIZE = 256;

        /*
		** {==================================================================
		** Stand-alone configuration
		** ===================================================================
		*/

        //#if lua_c || luaall_c

        /*
		@@ lua_stdin_is_tty detects whether the standard input is a 'tty' (that
		@* is, whether we're running lua interactively).
		** CHANGE it if you have a better definition for non-POSIX/non-Windows
		** systems.
		*/
        public static int lua_stdin_is_tty() => 1;   /* assume stdin is a tty */

        /*
		@@ LUA_PROMPT is the default prompt used by stand-alone Lua.
		@@ LUA_PROMPT2 is the default continuation prompt used by stand-alone Lua.
		** CHANGE them if you want different prompts. (You can also change the
		** prompts dynamically, assigning to globals _PROMPT/_PROMPT2.)
		*/
        public const string LUA_PROMPT = "> ";
        public const string LUA_PROMPT2 = ">> ";

        /*
		@@ LUA_PROGNAME is the default name for the stand-alone Lua program.
		** CHANGE it if your stand-alone interpreter has a different name and
		** your system is not able to detect that name automatically.
		*/
        public const string LUA_PROGNAME = "lua";

        /*
		@@ LUA_MAXINPUT is the maximum length for an input line in the
		@* stand-alone interpreter.
		** CHANGE it if you need longer lines.
		*/
        public const int LUA_MAXINPUT = 4096;

        /*
		@@ lua_readline defines how to show a prompt and then read a line from
		@* the standard input.
		@@ lua_saveline defines how to "save" a read line in a "history".
		@@ lua_freeline defines how to free a line read by lua_readline.
		** CHANGE them if you want to improve this functionality (e.g., by using
		** GNU readline and history facilities).
		*/
        public static bool lua_readline(lua_State L, CharPtr b, CharPtr p)
        {
            fputs(p, stdout);
            fflush(stdout);     /* show prompt */
            return fgets(b, stdin) != null;  /* get line */
        }
        public static void lua_saveline(lua_State L, int idx) { }
        public static void lua_freeline(lua_State L, CharPtr b) { }

        //#endif

        /* }================================================================== */
        /*
		@@ LUAI_GCPAUSE defines the default pause between garbage-collector cycles
		@* as a percentage.
		** CHANGE it if you want the GC to run faster or slower (higher values
		** mean larger pauses which mean slower collection.) You can also change
		** this value dynamically.
		*/
        public const int LUAI_GCPAUSE = 200;  /* 200% (wait memory to double before next GC) */

        /*
		@@ LUAI_GCMUL defines the default speed of garbage collection relative to
		@* memory allocation as a percentage.
		** CHANGE it if you want to change the granularity of the garbage
		** collection. (Higher values mean coarser collections. 0 represents
		** infinity, where each step performs a full collection.) You can also
		** change this value dynamically.
		*/
        public const int LUAI_GCMUL = 200; /* GC runs 'twice the speed' of memory allocation */

        /*
		@@ LUA_COMPAT_GETN controls compatibility with old getn behavior.
		** CHANGE it (define it) if you want exact compatibility with the
		** behavior of setn/getn in Lua 5.0.
		*/
        //#undef LUA_COMPAT_GETN /* dotnet port doesn't define in the first place */

        /*
		@@ LUA_COMPAT_LOADLIB controls compatibility about global loadlib.
		** CHANGE it to undefined as soon as you do not need a global 'loadlib'
		** function (the function is still available as 'package.loadlib').
		*/
        //#undef LUA_COMPAT_LOADLIB /* dotnet port doesn't define in the first place */

        /*
		@@ LUA_COMPAT_VARARG controls compatibility with old vararg feature.
		** CHANGE it to undefined as soon as your programs use only '...' to
		** access vararg parameters (instead of the old 'arg' table).
		*/
        //#define LUA_COMPAT_VARARG /* defined higher up */

        /*
		@@ LUA_COMPAT_MOD controls compatibility with old math.mod function.
		** CHANGE it to undefined as soon as your programs use 'math.fmod' or
		** the new '%' operator instead of 'math.mod'.
		*/
        //#define LUA_COMPAT_MOD /* defined higher up */

        /*
		@@ LUA_COMPAT_LSTR controls compatibility with old long string nesting
		@* facility.
		** CHANGE it to 2 if you want the old behaviour, or undefine it to turn
		** off the advisory error when nesting [[...]].
		*/
        //#define LUA_COMPAT_LSTR		1
        //#define LUA_COMPAT_LSTR /* defined higher up */

        /*
		@@ LUA_COMPAT_GFIND controls compatibility with old 'string.gfind' name.
		** CHANGE it to undefined as soon as you rename 'string.gfind' to
		** 'string.gmatch'.
		*/
        //#define LUA_COMPAT_GFIND /* defined higher up */

        /*
		@@ LUA_COMPAT_OPENLIB controls compatibility with old 'luaL_openlib'
		@* behavior.
		** CHANGE it to undefined as soon as you replace to 'luaL_register'
		** your uses of 'luaL_openlib'
		*/
        //#define LUA_COMPAT_OPENLIB /* defined higher up */

        /*
		@@ luai_apicheck is the assert macro used by the Lua-C API.
		** CHANGE luai_apicheck if you want Lua to perform some checks in the
		** parameters it gets from API calls. This may slow down the interpreter
		** a bit, but may be quite useful when debugging C code that interfaces
		** with Lua. A useful redefinition is to use assert.h.
		*/
#if LUA_USE_APICHECK
			public static void luai_apicheck(lua_State L, bool o)	{Debug.Assert(o);}
			public static void luai_apicheck(lua_State L, int o) {Debug.Assert(o != 0);}
#else
        public static void luai_apicheck(lua_State L, bool o) { }
        public static void luai_apicheck(lua_State L, int o) { }
#endif

        /*
		@@ LUAI_BITSINT defines the number of bits in an int.
		** CHANGE here if Lua cannot automatically detect the number of bits of
		** your machine. Probably you do not need to change this.
		*/
        /* avoid overflows in comparison */
        //#if INT_MAX-20 < 32760
        //public const int LUAI_BITSINT	= 16
        //#elif INT_MAX > 2147483640L
        /* int has at least 32 bits */
        public const int LUAI_BITSINT = 32;
        //#else
        //#error "you must define LUA_BITSINT with number of bits in an integer"
        //#endif

        /*
		@@ LUAI_UINT32 is an unsigned integer with at least 32 bits.
		@@ LUAI_INT32 is an signed integer with at least 32 bits.
		@@ LUAI_UMEM is an unsigned integer big enough to count the total
		@* memory used by Lua.
		@@ LUAI_MEM is a signed integer big enough to count the total memory
		@* used by Lua.
		** CHANGE here if for some weird reason the default definitions are not
		** good enough for your machine. (The definitions in the 'else'
		** part always works, but may waste space on machines with 64-bit
		** longs.) Probably you do not need to change this.
		*/
        //#if LUAI_BITSINT >= 32
        //#define LUAI_UINT32	unsigned int
        //#define LUAI_INT32	int
        //#define LUAI_MAXINT32	INT_MAX
        //#define LUAI_UMEM	uint
        //#define LUAI_MEM	ptrdiff_t
        //#else
        ///* 16-bit ints */
        //#define LUAI_UINT32	unsigned long
        //#define LUAI_INT32	long
        //#define LUAI_MAXINT32	LONG_MAX
        //#define LUAI_UMEM	unsigned long
        //#define LUAI_MEM	long
        //#endif

        /*
		@@ LUAI_MAXCALLS limits the number of nested calls.
		** CHANGE it if you need really deep recursive calls. This limit is
		** arbitrary; its only purpose is to stop infinite recursion before
		** exhausting memory.
		*/
        public const int LUAI_MAXCALLS = 20000;

        /*
		@@ LUAI_MAXCSTACK limits the number of Lua stack slots that a C function
		@* can use.
		** CHANGE it if you need lots of (Lua) stack space for your C
		** functions. This limit is arbitrary; its only purpose is to stop C
		** functions to consume unlimited stack space. (must be smaller than
		** -LUA_REGISTRYINDEX)
		*/
        public const int LUAI_MAXCSTACK = 8000;

        /*
		** {==================================================================
		** CHANGE (to smaller values) the following definitions if your system
		** has a small C stack. (Or you may want to change them to larger
		** values if your system has a large C stack and these limits are
		** too rigid for you.) Some of these constants control the size of
		** stack-allocated arrays used by the compiler or the interpreter, while
		** others limit the maximum number of recursive calls that the compiler
		** or the interpreter can perform. Values too large may cause a C stack
		** overflow for some forms of deep constructs.
		** ===================================================================
		*/

        /*
		@@ LUAI_MAXCCALLS is the maximum depth for nested C calls (short) and
		@* syntactical nested non-terminals in a program.
		*/
        public const int LUAI_MAXCCALLS = 200;

        /*
		@@ LUAI_MAXVARS is the maximum number of local variables per function
		@* (must be smaller than 250).
		*/
        public const int LUAI_MAXVARS = 200;

        /*
		@@ LUAI_MAXUPVALUES is the maximum number of upvalues per function
		@* (must be smaller than 250).
		*/
        public const int LUAI_MAXUPVALUES = 60;

        /*
		@@ LUAL_BUFFERSIZE is the buffer size used by the lauxlib buffer system.
		*/
        public const int LUAL_BUFFERSIZE = 1024; // BUFSIZ; todo: check this - mjf

        /* }================================================================== */

        /*
		** {==================================================================
		@@ LUA_NUMBER is the type of numbers in Lua.
		** CHANGE the following definitions only if you want to build Lua
		** with a number type different from double. You may also need to
		** change lua_number2int & lua_number2integer.
		** ===================================================================
		*/

        //#define LUA_NUMBER_DOUBLE
        //#define LUA_NUMBER	double	/* declared in dotnet build with using statement */

        /*
		@@ LUAI_UACNUMBER is the result of an 'usual argument conversion'
		@* over a number.
		*/
        //#define LUAI_UACNUMBER	double /* declared in dotnet build with using statement */

        /*
		@@ LUA_NUMBER_SCAN is the format for reading numbers.
		@@ LUA_NUMBER_FMT is the format for writing numbers.
		@@ lua_number2str converts a number to a string.
		@@ LUAI_MAXNUMBER2STR is maximum size of previous conversion.
		@@ lua_str2number converts a string to a number.
		*/
        public const string LUA_NUMBER_SCAN = "%lf";
        public const string LUA_NUMBER_FMT = "%.14g";
        public static CharPtr lua_number2str(double n) => string.Format(CultureInfo.InvariantCulture, "{0}", n);
        public const int LUAI_MAXNUMBER2STR = 32; /* 16 digits, sign, point, and \0 */

        private const string number_chars = "0123456789+-eE.";
        public static double lua_str2number(CharPtr s, out CharPtr end)
        {
            end = new (s.chars, s.index);
            var str = "";
            while (end[0] == ' ')
                end = end.next();
            while (number_chars.IndexOf(end[0]) >= 0)
            {
                str += end[0];
                end = end.next();
            }

            try
            {
                return Convert.ToDouble(str.ToString(), Culture("en-US"));
            }
            catch (OverflowException)
            {
                // this is a hack, fix it - mjf
                if (str[0] == '-')
                    return LUA_NUMBER.NegativeInfinity;
                else
                    return LUA_NUMBER.PositiveInfinity;
            }
            catch
            {
                end = new CharPtr(s.chars, s.index);
                return 0;
            }
        }

        private static IFormatProvider Culture(string p)
        {
#if SILVERLIGHT
            return new CultureInfo(p);
#else
            return CultureInfo.GetCultureInfo(p);
#endif
        }

        /*
		@@ The luai_num* macros define the primitive operations over numbers.
		*/
#if LUA_CORE
        //#include <math.h>
        public delegate lua_Number op_delegate(lua_Number a, lua_Number b);
        public static lua_Number luai_numadd(lua_Number a, lua_Number b) => ((a) + (b));
        public static lua_Number luai_numsub(lua_Number a, lua_Number b) => ((a) - (b));
        public static lua_Number luai_nummul(lua_Number a, lua_Number b) => ((a) * (b));
        public static lua_Number luai_numdiv(lua_Number a, lua_Number b) => ((a) / (b));
        public static lua_Number luai_nummod(lua_Number a, lua_Number b) => ((a) - Math.Floor((a) / (b)) * (b));
        public static lua_Number luai_numpow(lua_Number a, lua_Number b) => (Math.Pow(a, b));
        public static lua_Number luai_numunm(lua_Number a) => (-(a));
        public static bool luai_numeq(lua_Number a, lua_Number b) => ((a) == (b));
        public static bool luai_numlt(lua_Number a, lua_Number b) => ((a) < (b));
        public static bool luai_numle(lua_Number a, lua_Number b) => ((a) <= (b));
        public static bool luai_numisnan(lua_Number a) => lua_Number.IsNaN(a);
#endif


        /*
		@@ lua_number2int is a macro to convert lua_Number to int.
		@@ lua_number2integer is a macro to convert lua_Number to lua_Integer.
		** CHANGE them if you know a faster way to convert a lua_Number to
		** int (with any rounding method and without throwing errors) in your
		** system. In Pentium machines, a naive typecast from double to int
		** in C is extremely slow, so any alternative is worth trying.
		*/

        /* On a Pentium, resort to a trick */
        //#if defined(LUA_NUMBER_DOUBLE) && !defined(LUA_ANSI) && !defined(__SSE2__) && \
        //	(defined(__i386) || defined (_M_IX86) || defined(__i386__))

        /* On a Microsoft compiler, use assembler */
        //#if defined(_MSC_VER)

        //#define lua_number2int(i,d)   __asm fld d   __asm fistp i
        //#define lua_number2integer(i,n)		lua_number2int(i, n)

        /* the next trick should work on any Pentium, but sometimes clashes
		   with a DirectX idiosyncrasy */
        //#else

        //union luai_Cast { double l_d; long l_l; };
        //#define lua_number2int(i,d) \
        //  { volatile union luai_Cast u; u.l_d = (d) + 6755399441055744.0; (i) = u.l_l; }
        //#define lua_number2integer(i,n)		lua_number2int(i, n)

        //#endif


        /* this option always works, but may be slow */
        //#else
        //#define lua_number2int(i,d)	((i)=(int)(d))
        //#define lua_number2integer(i,d)	((i)=(lua_Integer)(d))

        //#endif

        private static void lua_number2int(out int i, lua_Number d) => i = (int)d;
        private static void lua_number2integer(out int i, lua_Number n) => i = (int)n;

        /* }================================================================== */


        /*
		@@ LUAI_USER_ALIGNMENT_T is a type that requires maximum alignment.
		** CHANGE it if your system requires alignments larger than double. (For
		** instance, if your system supports long doubles and they must be
		** aligned in 16-byte boundaries, then you should add long double in the
		** union.) Probably you do not need to change this.
		*/
        //#define LUAI_USER_ALIGNMENT_T	union { double u; void *s; long l; }

        public class LuaException : Exception
        {
            public lua_State L;
            public lua_longjmp c;

            public LuaException(lua_State L, lua_longjmp c) { this.L = L; this.c = c; }
        }

        /*
		@@ LUAI_THROW/LUAI_TRY define how Lua does exception handling.
		** CHANGE them if you prefer to use longjmp/setjmp even with C++
		** or if want/don't to use _longjmp/_setjmp instead of regular
		** longjmp/setjmp. By default, Lua handles errors with exceptions when
		** compiling as C++ code, with _longjmp/_setjmp when asked to use them,
		** and with longjmp/setjmp otherwise.
		*/
        //#if defined(__cplusplus)
        ///* C++ exceptions */
        public static void LUAI_THROW(lua_State L, lua_longjmp c) { throw new LuaException(L, c); }
        //#define LUAI_TRY(L,c,a)	try { a } catch(...) \
        //    { if ((c).status == 0) (c).status = -1; }
        public static void LUAI_TRY(lua_State L, lua_longjmp c, object a)
        {
            if (c.status == 0) c.status = -1;
        }
        //#define luai_jmpbuf	int  /* dummy variable */

        //#elif defined(LUA_USE_ULONGJMP)
        ///* in Unix, try _longjmp/_setjmp (more efficient) */
        //#define LUAI_THROW(L,c)	_longjmp((c).b, 1)
        //#define LUAI_TRY(L,c,a)	if (_setjmp((c).b) == 0) { a }
        //#define luai_jmpbuf	jmp_buf

        //#else
        ///* default handling with long jumps */
        //public static void LUAI_THROW(lua_State L, lua_longjmp c) { c.b(1); }
        //#define LUAI_TRY(L,c,a)	if (setjmp((c).b) == 0) { a }
        //#define luai_jmpbuf	jmp_buf

        //#endif


        /*
		@@ LUA_MAXCAPTURES is the maximum number of captures that a pattern
		@* can do during pattern-matching.
		** CHANGE it if you need more captures. This limit is arbitrary.
		*/
        public const int LUA_MAXCAPTURES = 32;


        /*
		@@ lua_tmpnam is the function that the OS library uses to create a
		@* temporary name.
		@@ LUA_TMPNAMBUFSIZE is the maximum size of a name created by lua_tmpnam.
		** CHANGE them if you have an alternative to tmpnam (which is considered
		** insecure) or if you want the original tmpnam anyway.  By default, Lua
		** uses tmpnam except when POSIX is available, where it uses mkstemp.
		*/


        /*
		@@ lua_popen spawns a new process connected to the current one through
		@* the file streams.
		** CHANGE it if you have a way to implement it in your system.
		*/
        //#if LUA_USE_POPEN

        //#define lua_popen(L,c,m)	((void)L, fflush(null), popen(c,m))
        //#define lua_pclose(L,file)	((void)L, (pclose(file) != -1))

        //#elif LUA_WIN

        //#define lua_popen(L,c,m)	((void)L, _popen(c,m))
        //#define lua_pclose(L,file)	((void)L, (_pclose(file) != -1))

        //#else

        public static Stream lua_popen(lua_State L, CharPtr c, CharPtr m) 
        { luaL_error(L, LUA_QL("popen") + " not supported"); return null; }
        public static int lua_pclose(lua_State L, Stream file) => 0;

        //#endif

        /*
		@@ LUA_DL_* define which dynamic-library system Lua should use.
		** CHANGE here if Lua has problems choosing the appropriate
		** dynamic-library system for your platform (either Windows' DLL, Mac's
		** dyld, or Unix's dlopen). If your system is some kind of Unix, there
		** is a good chance that it has dlopen, so LUA_DL_DLOPEN will work for
		** it.  To use dlopen you also need to adapt the src/Makefile (probably
		** adding -ldl to the linker options), so Lua does not select it
		** automatically.  (When you change the makefile to add -ldl, you must
		** also add -DLUA_USE_DLOPEN.)
		** If you do not want any kind of dynamic library, undefine all these
		** options.
		** By default, _WIN32 gets LUA_DL_DLL and MAC OS X gets LUA_DL_DYLD.
		*/
        //#if LUA_USE_DLOPEN
        //#define LUA_DL_DLOPEN
        //#endif

        //#if LUA_WIN
        //#define LUA_DL_DLL
        //#endif


        /*
		@@ LUAI_EXTRASPACE allows you to add user-specific data in a lua_State
		@* (the data goes just *before* the lua_State pointer).
		** CHANGE (define) this if you really need that. This value must be
		** a multiple of the maximum alignment required for your machine.
		*/
        public const int LUAI_EXTRASPACE = 0;


        /*
		@@ luai_userstate* allow user-specific actions on threads.
		** CHANGE them if you defined LUAI_EXTRASPACE and need to do something
		** extra when a thread is created/deleted/resumed/yielded.
		*/
        public static void luai_userstateopen(lua_State L) { }
        public static void luai_userstateclose(lua_State L) { }
        public static void luai_userstatethread(lua_State L, lua_State L1) { }
        public static void luai_userstatefree(lua_State L) { }
        public static void luai_userstateresume(lua_State L, int n) { }
        public static void luai_userstateyield(lua_State L, int n) { }


        /*
		@@ LUA_INTFRMLEN is the length modifier for integer conversions
		@* in 'string.format'.
		@@ LUA_INTFRM_T is the integer type correspoding to the previous length
		@* modifier.
		** CHANGE them if your system supports long long or does not support long.
		*/

#if LUA_USELONGLONG

		public const string LUA_INTFRMLEN		= "ll";
		//#define LUA_INTFRM_T		long long

#else

        public const string LUA_INTFRMLEN = "l";
        //#define LUA_INTFRM_T		long			/* declared in dotnet build with using statement */

#endif



        /* =================================================================== */

        /*
		** Local configuration. You can use this space to add your redefinitions
		** without modifying the main part of the file.
		*/

        // misc stuff needed for the compile

        public static bool isalpha(char c) => char.IsLetter(c);
        public static bool iscntrl(char c) => char.IsControl(c);
        public static bool isdigit(char c) => char.IsDigit(c);
        public static bool islower(char c) => char.IsLower(c);
        public static bool ispunct(char c) => char.IsPunctuation(c);
        public static bool isspace(char c) => (c == ' ') || (c >= (char)0x09 && c <= (char)0x0D);
        public static bool isupper(char c) => char.IsUpper(c);
        public static bool isalnum(char c) => char.IsLetterOrDigit(c);
        public static bool isxdigit(char c) => "0123456789ABCDEFabcdef".IndexOf(c) >= 0;

        public static bool isalpha(int c) => char.IsLetter((char)c);
        public static bool iscntrl(int c) => char.IsControl((char)c);
        public static bool isdigit(int c) => char.IsDigit((char)c);
        public static bool islower(int c) => char.IsLower((char)c);
        public static bool ispunct(int c) => ((char)c != ' ') && !isalnum((char)c); // *not* the same as Char.IsPunctuation
        public static bool isspace(int c) => ((char)c == ' ') || ((char)c >= (char)0x09 && (char)c <= (char)0x0D);
        public static bool isupper(int c) => char.IsUpper((char)c);
        public static bool isalnum(int c) => char.IsLetterOrDigit((char)c);

        public static char tolower(char c) => char.ToLower(c);
        public static char toupper(char c) => char.ToUpper(c);
        public static char tolower(int c) => char.ToLower((char)c);
        public static char toupper(int c) => char.ToUpper((char)c);

        public static ulong strtoul(CharPtr s, out CharPtr end, int base_)
        {
            try
            {
                end = new (s.chars, s.index);

                // skip over any leading whitespace
                while (end[0] == ' ')
                    end = end.next();

                // ignore any leading 0x
                if ((end[0] == '0') && (end[1] == 'x'))
                    end = end.next().next();
                else if ((end[0] == '0') && (end[1] == 'X'))
                    end = end.next().next();

                // do we have a leading + or - sign?
                bool negate = false;
                if (end[0] == '+')
                    end = end.next();
                else if (end[0] == '-')
                {
                    negate = true;
                    end = end.next();
                }

                // loop through all chars
                bool invalid = false;
                bool had_digits = false;
                ulong result = 0;
                while (true)
                {
                    // get this char
                    char ch = end[0];

                    // which digit is this?
                    int this_digit = 0;
                    if (isdigit(ch))
                        this_digit = ch - '0';
                    else if (isalpha(ch))
                        this_digit = tolower(ch) - 'a' + 10;
                    else
                        break;

                    // is this digit valid?
                    if (this_digit >= base_)
                        invalid = true;
                    else
                    {
                        had_digits = true;
                        result = result * (ulong)base_ + (ulong)this_digit;
                    }

                    end = end.next();
                }

                // were any of the digits invalid?
                if (invalid || (!had_digits))
                {
                    end = s;
                    return System.UInt64.MaxValue;
                }

                // if the value was a negative then negate it here
                if (negate)
                    result = (ulong)-(long)result;

                // ok, we're done
                return (ulong)result;
            }
            catch
            {
                end = s;
                return 0;
            }
        }

        public static void putchar(char ch, TextWriter Writer = null)
            => (Writer ?? Console.Out).Write(ch);

        public static void putchar(int ch, TextWriter Writer = null)
            => putchar((char)ch, Writer);

        public static bool isprint(byte c) 
            => (c >= (byte)' ') && (c <= (byte)127);

        public static int parse_scanf(string str, CharPtr fmt, params object[] argp)
        {
            var parm_index = 0;
            var index = 0;
            while (fmt[index] != 0)
            {
                if (fmt[index++] == '%')
                    switch (fmt[index++])
                    {
                        case 's':
                            {
                                argp[parm_index++] = str;
                                break;
                            }
                        case 'c':
                            {
                                argp[parm_index++] = Convert.ToChar(str, Culture("en-US"));
                                break;
                            }
                        case 'd':
                            {
                                argp[parm_index++] = Convert.ToInt32(str, Culture("en-US"));
                                break;
                            }
                        case 'l':
                            {
                                argp[parm_index++] = Convert.ToDouble(str, Culture("en-US"));
                                break;
                            }
                        case 'f':
                            {
                                argp[parm_index++] = Convert.ToDouble(str, Culture("en-US"));
                                break;
                            }
                            //case 'p':
                            //    {
                            //        result += "(pointer)";
                            //        break;
                            //    }
                    }
            }
            return parm_index;
        }

        public static void printf(CharPtr str, params object[] argv) 
            => Utils.printf(str.ToString(), argv);

        public static void sprintf(CharPtr buffer, CharPtr str, params object[] argv) => strcpy(buffer, Utils.sprintf(str.ToString(), argv));

        public static int fprintf(Stream stream, CharPtr str, params object[] argv)
        {
            var result = Utils.sprintf(str.ToString(), argv);
            var chars = result.ToCharArray();
            var bytes = new byte[chars.Length];
            for (int i = 0; i < chars.Length; i++)
                bytes[i] = (byte)chars[i];
            stream.Write(bytes, 0, bytes.Length);
            return 1;
        }

        public const int EXIT_SUCCESS = 0;
        public const int EXIT_FAILURE = 1;
        public static int errno() => -1;  // todo: fix this - mjf

        public static CharPtr strerror(int error) => string.Format("error #{0}", error);

        public static CharPtr getenv(CharPtr envname)
        {
            // todo: fix this - mjf
            //if (envname == "LUA_PATH)
            //return "MyPath";
            var vs = Environment.GetEnvironmentVariables();
            return vs.Contains(envname) 
                ? new (vs[envname] as string) 
                : new ();
        }


        public static int memcmp(CharPtr ptr1, CharPtr ptr2, uint size) 
            => memcmp(ptr1, ptr2, (int)size);
        public static int memcmp(CharPtr ptr1, CharPtr ptr2, int size)
        {
            for (int i = 0; i < size; i++)
                if (ptr1[i] != ptr2[i])
                    return ptr1[i] < ptr2[i] ? -1 : +1;
            return 0;
        }

        public static CharPtr memchr(CharPtr ptr, char c, uint count)
        {
            for (uint i = 0; i < count; i++)
                if (ptr[i] == c)
                    return new CharPtr(ptr.chars, (int)(ptr.index + i));
            return null;
        }

        public static CharPtr strpbrk(CharPtr str, CharPtr charset)
        {
            for (int i = 0; str[i] != '\0'; i++)
                for (int j = 0; charset[j] != '\0'; j++)
                    if (str[i] == charset[j])
                        return new CharPtr(str.chars, str.index + i);
            return null;
        }

        // find c in str
        public static CharPtr strchr(CharPtr str, char c)
        {
            for (int index = str.index; str.chars[index] != 0; index++)
                if (str.chars[index] == c)
                    return new CharPtr(str.chars, index);
            return null;
        }

        public static CharPtr strcpy(CharPtr dst, CharPtr src)
        {
            int i = 0;
            for (; src[i] != '\0'; i++)
                dst[i] = src[i];
            dst[i] = '\0';
            return dst;
        }

        public static CharPtr strcat(CharPtr dst, CharPtr src)
        {
            int dst_index = 0;
            while (dst[dst_index] != '\0')
                dst_index++;
            int src_index = 0;
            while (src[src_index] != '\0')
                dst[dst_index++] = src[src_index++];
            dst[dst_index++] = '\0';
            return dst;
        }

        public static CharPtr strncat(CharPtr dst, CharPtr src, int count)
        {
            int dst_index = 0;
            while (dst[dst_index] != '\0')
                dst_index++;
            int src_index = 0;
            while ((src[src_index] != '\0') && (count-- > 0))
                dst[dst_index++] = src[src_index++];
            return dst;
        }

        public static uint strcspn(CharPtr str, CharPtr charset)
        {
            int index = str.ToString().IndexOfAny(charset.ToString().ToCharArray());
            if (index < 0)
                index = str.ToString().Length;
            return (uint)index;
        }

        public static CharPtr strncpy(CharPtr dst, CharPtr src, int length)
        {
            int index = 0;
            while ((src[index] != '\0') && (index < length))
            {
                dst[index] = src[index];
                index++;
            }
            while (index < length)
                dst[index++] = '\0';
            return dst;
        }

        public static int strlen(CharPtr str)
        {
            int index = 0;
            while (str[index] != '\0')
                index++;
            return index;
        }

        public static lua_Number fmod(lua_Number a, lua_Number b)
        {
            float quotient = (int)Math.Floor(a / b);
            return a - quotient * b;
        }

        public static lua_Number modf(lua_Number a, out lua_Number b)
        {
            b = Math.Floor(a);
            return a - Math.Floor(a);
        }

        public static long lmod(lua_Number a, lua_Number b) 
            => (long)a % (long)b;

        public static int getc(Stream f) => f.ReadByte();

        public static void ungetc(int c, Stream f)
        {
            if (f.Position > 0)
                f.Seek(-1, SeekOrigin.Current);
        }

#if XBOX || SILVERLIGHT
		public static Stream stdout;
		public static Stream stdin;
		public static Stream stderr;
#else
        public static Stream stdout = Console.OpenStandardOutput();
        public static Stream stdin = Console.OpenStandardInput();
        public static Stream stderr = Console.OpenStandardError();
#endif
        public static int EOF = -1;
        public static void fputs(CharPtr str, Stream stream)
        {
            using var writer = new StreamWriter(stream);
            writer.Write(str.ToString());
        }
        public static int feof(Stream s)
            => (s.Position >= s.Length) ? 1 : 0;
        public static int fread(CharPtr ptr, int size, int num, Stream stream)
        {
            int num_bytes = num * size;
            var bytes = new byte[num_bytes];
            try
            {
                int result = stream.Read(bytes, 0, num_bytes);
                for (int i = 0; i < result; i++)
                    ptr[i] = (char)bytes[i];
                return result / size;
            }
            catch
            {
                return 0;
            }
        }
        public static int fwrite(CharPtr ptr, int size, int num, Stream stream)
        {
            int num_bytes = num * size;
            var bytes = new byte[num_bytes];
            for (int i = 0; i < num_bytes; i++)
                bytes[i] = (byte)ptr[i];
            try
            {
                stream.Write(bytes, 0, num_bytes);
            }
            catch
            {
                return 0;
            }
            return num;
        }
        public static int strcmp(CharPtr s1, CharPtr s2)
        {
            if (s1 == s2)
                return 0;
            else if (s1 == null)
                return -1;
            else if (s2 == null)
                return 1;

            for (int i = 0; ; i++)
            {
                if (s1[i] != s2[i])
                    return s1[i] < s2[i] ? -1 : 1;
                
                if (s1[i] == '\0')
                    return 0;
            }
        }

        public static CharPtr fgets(CharPtr str, Stream stream)
        {
            int index = 0;
            try
            {
                while (true)
                {
                    str[index] = (char)stream.ReadByte();
                    if (str[index] == '\n')
                        break;
                    if (index >= str.chars.Length)
                        break;
                    index++;
                }
            }
            catch
            {
            }
            return str;
        }

        public static double frexp(double x, out int expptr)
        {
#if XBOX
			expptr = (int)(Math.Log(x) / Math.Log(2)) + 1;
#else
            expptr = (int)Math.Log(x, 2) + 1;
#endif
            return (double)( x / Math.Pow(2, expptr));
        }

        public static double ldexp(double x, int expptr) 
            => x * Math.Pow(2, expptr);
        public static CharPtr strstr(CharPtr str, CharPtr substr)
        {
            int index = str.ToString().IndexOf(substr.ToString());
            return index < 0 ? null : new CharPtr(str + index);
        }
        public static CharPtr strrchr(CharPtr str, char ch)
        {
            int index = str.ToString().LastIndexOf(ch);
            return index < 0 ? null : str + index;
        }

        public static Stream fopen(CharPtr filename, CharPtr mode)
        {
            var str = filename.ToString();
            var filemode = FileMode.Open;
            var fileaccess = (FileAccess)0;
            for (int i = 0; mode[i] != '\0'; i++)
                switch (mode[i])
                {
                    case 'r':
                        fileaccess |= FileAccess.Read;
                        if (!File.Exists(str))
                            return null;
                        break;
                    case 'w':
                        filemode = FileMode.Create;
                        fileaccess |= FileAccess.Write;
                        break;
                }
            try
            {
                return new FileStream(str, filemode, fileaccess);
            }
            catch
            {
                return null;
            }
        }
        public static Stream freopen(CharPtr filename, CharPtr mode, Stream stream)
        {
            try
            {
                stream.Flush();
                stream.Close();
            }
            catch 
            { 
            }
            return fopen(filename, mode);
        }
        public static void fflush(Stream stream)
            => stream.Flush();
        public static int ferror(Stream stream) 
            => 0;   // todo: fix this - mjf
        public static int fclose(Stream stream)
        {
            stream.Close();
            return 0;
        }

#if !XBOX
        public static Stream tmpfile() 
            => new FileStream(
                Path.GetTempFileName(),
                FileMode.Create, FileAccess.ReadWrite);
#endif
        public static int fscanf(Stream f, CharPtr format, params object[] argp)
        {
            using var s = new StreamReader(f);
            return parse_scanf(s.ReadLine(), format, argp);
        }
        public static int fseek(Stream f, long offset, int origin)
        {
            try
            {
                f.Seek(offset, (SeekOrigin)origin);
                return 0;
            }
            catch
            {
                return 1;
            }
        }
        public static long ftell(Stream f) 
            => f.Position;
        public static int clearerr(Stream f) 
            => 0;
            //Debug.Assert(false, "clearerr not implemented yet - mjf");
        public static int setvbuf(Stream stream, CharPtr buffer, int mode, uint size)
        {
            Debug.Assert(false, "setvbuf not implemented yet - mjf");
            return 0;
        }

        public static void memcpy<T>(T[] dst, T[] src, int length)
            => Array.Copy(src, dst, length);

        public static void memcpy<T>(T[] dst, int offset, T[] src, int length) 
            => Array.Copy(src, 0, dst, offset, length);

        public static void memcpy<T>(T[] dst, T[] src, int srcofs, int length)
            => Array.Copy(src, srcofs, dst, 0, length);

        public static void memcpy(CharPtr ptr1, CharPtr ptr2, uint size) 
            => memcpy(ptr1, ptr2, (int)size);

        public static void memcpy(CharPtr ptr1, CharPtr ptr2, int size)
        {
            //for (int i = 0; i < size; i++)
            //    ptr1[i] = ptr2[i];
            ptr2.chars = new char[size];
            ptr2.index = 0;
            
            Array.Copy(ptr2.chars, ptr1.chars, size);
        }

        public static object VOID(object f) => f;

        public const double HUGE_VAL = double.MaxValue;
        public const uint SHRT_MAX = ushort.MaxValue;

        public const int _IONBF = 0;
        public const int _IOFBF = 1;
        public const int _IOLBF = 2;

        public const int SEEK_SET = 0;
        public const int SEEK_CUR = 1;
        public const int SEEK_END = 2;

        // one of the primary objectives of this port is to match the C version of Lua as closely as
        // possible. a key part of this is also matching the behaviour of the garbage collector, as
        // that affects the operation of things such as weak tables. in order for this to occur the
        // size of structures that are allocated must be reported as identical to their C++ equivelents.
        // that this means that variables such as global_State.totalbytes no longer indicate the true
        // amount of memory allocated.
        public static int GetUnmanagedSize(Type t)
        {
            if (t == typeof(global_State))
                return 228;
            else if (t == typeof(LG))
                return 376;
            else if (t == typeof(CallInfo))
                return 24;
            else if (t == typeof(lua_TValue))
                return 16;
            else if (t == typeof(Table))
                return 32;
            else if (t == typeof(Node))
                return 32;
            else if (t == typeof(GCObject))
                return 120;
            else if (t == typeof(GCObjectRef))
                return 4;
            else if (t == typeof(ArrayRef))
                return 4;
            else if (t == typeof(Closure))
                return 0;   // handle this one manually in the code
            else if (t == typeof(Proto))
                return 76;
            else if (t == typeof(luaL_Reg))
                return 8;
            else if (t == typeof(luaL_Buffer))
                return 524;
            else if (t == typeof(lua_State))
                return 120;
            else if (t == typeof(lua_Debug))
                return 100;
            else if (t == typeof(CallS))
                return 8;
            else if (t == typeof(LoadF))
                return 520;
            else if (t == typeof(LoadS))
                return 8;
            else if (t == typeof(lua_longjmp))
                return 72;
            else if (t == typeof(SParser))
                return 20;
            else if (t == typeof(Token))
                return 16;
            else if (t == typeof(LexState))
                return 52;
            else if (t == typeof(FuncState))
                return 572;
            else if (t == typeof(GCheader))
                return 8;
            else if (t == typeof(lua_TValue))
                return 16;
            else if (t == typeof(TString))
                return 16;
            else if (t == typeof(LocVar))
                return 12;
            else if (t == typeof(UpVal))
                return 32;
            else if (t == typeof(CClosure))
                return 40;
            else if (t == typeof(LClosure))
                return 24;
            else if (t == typeof(TKey))
                return 16;
            else if (t == typeof(ConsControl))
                return 40;
            else if (t == typeof(LHS_assign))
                return 32;
            else if (t == typeof(expdesc))
                return 24;
            else if (t == typeof(upvaldesc))
                return 2;
            else if (t == typeof(BlockCnt))
                return 12;
            else if (t == typeof(Zio))
                return 20;
            else if (t == typeof(Mbuffer))
                return 12;
            else if (t == typeof(LoadState))
                return 16;
            else if (t == typeof(MatchState))
                return 272;
            else if (t == typeof(stringtable))
                return 12;
            else if (t == typeof(FilePtr))
                return 4;
            else if (t == typeof(Udata))
                return 24;
            else if (t == typeof(byte))
                return sizeof(byte);
            else if (t == typeof(char))
                return sizeof(char);
            else if (t == typeof(ushort))
                return sizeof(ushort);
            else if (t == typeof(short))
                return sizeof(short);
            else if (t == typeof(uint))
                return sizeof(uint);
            else if (t == typeof(int))
                return sizeof(int);
            else if (t == typeof(float))
                return sizeof(float);
            else if (t == typeof(long))
                return sizeof(long);
            else if (t == typeof(ulong))
                return sizeof(ulong);
            else if (t == typeof(double))
                return sizeof(double);
            else
                Debug.Assert(false, 
                    "Trying to get unknown sized of unmanaged type :" + t.ToString());
            return 0;
        }
    }
}
