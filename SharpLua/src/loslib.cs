/*
** $Id: loslib.c,v 1.19.1.3 2008/01/18 16:38:18 roberto Exp $
** Standard Operating System library
** See Copyright Notice in lua.h
*/

using System;
using System.IO;

namespace SharpLua
{
    using lua_Number = System.Double;

    public partial class Lua
    {
        private static int os_pushresult(lua_State L, int i, CharPtr filename)
        {
            int en = errno();  /* calls to Lua API may change this value */
            if (i != 0)
            {
                lua_pushboolean(L, 1);
                return 1;
            }
            else
            {
                lua_pushnil(L);
                lua_pushfstring(L, "%s: %s", filename, strerror(en));
                lua_pushinteger(L, en);
                return 3;
            }
        }

        private static int os_execute(lua_State L)
        {
#if XBOX || SILVERLIGHT
			luaL_error(L, "os_execute not supported on XBox360");
#else
            CharPtr strCmdLine = "/C regenresx " + luaL_optstring(L, 1, null);
            System.Diagnostics.Process proc = new()
            {
                EnableRaisingEvents = false
            };
            proc.StartInfo.FileName = "CMD.exe";
            proc.StartInfo.Arguments = strCmdLine.ToString();
            proc.Start();
            proc.WaitForExit();
            lua_pushinteger(L, proc.ExitCode);
#endif
            return 1;
        }

        private static int os_remove(lua_State L)
        {
            var filename = luaL_checkstring(L, 1);
            int result = 1;
            try { File.Delete(filename.ToString()); } catch { result = 0; }
            return os_pushresult(L, result, filename);
        }

        private static int os_rename(lua_State L)
        {
            var fromname = luaL_checkstring(L, 1);
            var toname = luaL_checkstring(L, 2);
            int result;
            try
            {
                File.Move(fromname.ToString(), toname.ToString());
                result = 0;
            }
            catch
            {
                result = 1; // todo: this should be a proper error code
            }
            return os_pushresult(L, result, fromname);
        }


        private static int os_tmpname(lua_State L)
        {
#if XBOX
		    luaL_error(L, "os_tmpname not supported on Xbox360");
#else
            lua_pushstring(L, Path.GetTempFileName());
#endif
            return 1;
        }

        private static int os_getenv(lua_State L)
        {
            lua_pushstring(L, getenv(luaL_checkstring(L, 1)));  /* if null push nil */
            return 1;
        }

        private static int os_clock(lua_State L)
        {
            long ticks = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            lua_pushnumber(L, ((lua_Number)ticks) / (lua_Number)1000);
            return 1;
        }


        /*
		** {======================================================
		** Time/Date operations
		** { year=%Y, month=%m, day=%d, hour=%H, min=%M, sec=%S,
		**   wday=%w+1, yday=%j, isdst=? }
		** =======================================================
		*/

        private static void setfield(lua_State L, CharPtr key, int value)
        {
            lua_pushinteger(L, value);
            lua_setfield(L, -2, key);
        }

        private static void setboolfield(lua_State L, CharPtr key, int value)
        {
            if (value < 0)  /* undefined? */
                return;  /* does not set field */
            lua_pushboolean(L, value);
            lua_setfield(L, -2, key);
        }

        private static int getboolfield(lua_State L, CharPtr key)
        {
            lua_getfield(L, -1, key);
            var res = lua_isnil(L, -1) ? -1 : lua_toboolean(L, -1);
            lua_pop(L, 1);
            return res;
        }

        private static int getfield(lua_State L, CharPtr key, int d)
        {
            int res;
            lua_getfield(L, -1, key);
            if (lua_isnumber(L, -1) != 0)
                res = (int)lua_tointeger(L, -1);
            else
            {
                if (d < 0)
                    return luaL_error(L, "field " + LUA_QS + " missing in date table", key);
                res = d;
            }
            lua_pop(L, 1);
            return res;
        }

        private static int os_date(lua_State L)
        {
            var s = new CharPtr(luaL_optstring(L, 1, "%c"));
            DateTime stm;
            if (s[0] == '!')
            {  /* UTC? */
                stm = DateTime.UtcNow;
                s.inc();  /* skip `!' */
            }
            else
                stm = DateTime.Now;
            if (strcmp(s, "*t") == 0)
            {
                lua_createtable(L, 0, 9);  /* 9 = number of fields */
                setfield(L, "sec", stm.Second);
                setfield(L, "min", stm.Minute);
                setfield(L, "hour", stm.Hour);
                setfield(L, "day", stm.Day);
                setfield(L, "month", stm.Month);
                setfield(L, "year", stm.Year);
                setfield(L, "wday", (int)stm.DayOfWeek);
                setfield(L, "yday", stm.DayOfYear);
                setboolfield(L, "isdst", stm.IsDaylightSavingTime() ? 1 : 0);
            }
            else
            {
                luaL_error(L, "strftime not implemented yet"); // todo: implement this - mjf
#if false
			CharPtr cc = new char[3];
			luaL_Buffer b;
			cc[0] = '%'; cc[2] = '\0';
			luaL_buffinit(L, b);
			for (; s[0] != 0; s.inc()) {
			  if (s[0] != '%' || s[1] == '\0')  /* no conversion specifier? */
				luaL_addchar(b, s[0]);
			  else {
				uint reslen;
				CharPtr buff = new char[200];  /* should be big enough for any conversion result */
				s.inc();
				cc[1] = s[0];
				reslen = strftime(buff, buff.Length, cc, stm);
				luaL_addlstring(b, buff, reslen);
			  }
			}
			luaL_pushresult(b);
#endif // #if 0
            }
            return 1;
        }


        private static int os_time(lua_State L)
        {
            DateTime t;
            if (lua_isnoneornil(L, 1))  /* called without args? */
                t = DateTime.Now;  /* get current time */
            else
            {
                luaL_checktype(L, 1, LUA_TTABLE);
                lua_settop(L, 1);  /* make sure table is at the top */
                int sec = getfield(L, "sec", 0);
                int min = getfield(L, "min", 0);
                int hour = getfield(L, "hour", 12);
                int day = getfield(L, "day", -1);
                int month = getfield(L, "month", -1) - 1;
                int year = getfield(L, "year", -1) - 1900;
                int isdst = getboolfield(L, "isdst");   // todo: implement this - mjf
                t = new DateTime(year, month, day, hour, min, sec);
            }
            lua_pushnumber(L, t.Ticks);
            return 1;
        }

        private static int os_difftime(lua_State L)
        {
            long ticks = (long)luaL_checknumber(L, 1) - (long)luaL_optnumber(L, 2, 0);
            lua_pushnumber(L, ticks / TimeSpan.TicksPerSecond);
            return 1;
        }

        /* }====================================================== */
        // locale not supported yet
        private static int os_setlocale(lua_State L)
        {
            /*
            static string[] cat = {LC_ALL, LC_COLLATE, LC_CTYPE, LC_MONETARY,
                                LC_NUMERIC, LC_TIME};
            static string[] catnames[] = {"all", "collate", "ctype", "monetary",
               "numeric", "time", null};
            CharPtr l = luaL_optstring(L, 1, null);
            int op = luaL_checkoption(L, 2, "all", catnames);
            lua_pushstring(L, setlocale(cat[op], l));
            */
            var l = luaL_optstring(L, 1, null);
            lua_pushstring(L, "C");
            return (l.ToString() == "C") ? 1 : 0;
        }

        private static int os_exit(lua_State L)
        {
#if XBOX
			luaL_error(L, "os_exit not supported on XBox360");
#else
#if SILVERLIGHT
            throw new SystemException();
#else
            Environment.Exit(EXIT_SUCCESS);
#endif
#endif
            return 0;
        }

        private readonly static luaL_Reg[] syslib = {
          new ("clock",     os_clock),
          new ("date",      os_date),
          new("difftime", os_difftime),
          new("execute", os_execute),
          new("exit", os_exit),
          new("getenv", os_getenv),
          new("remove", os_remove),
          new("rename", os_rename),
          new("setlocale", os_setlocale),
          new("time", os_time),
          new("tmpname", os_tmpname),
          new(null, null)
        };

        /* }====================================================== */
        public static int luaopen_os(lua_State L)
        {
            luaL_register(L, LUA_OSLIBNAME, syslib);
            return 1;
        }
    }
}
