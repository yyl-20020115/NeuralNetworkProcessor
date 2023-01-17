/*
** $Id: linit.c,v 1.14.1.1 2007/12/27 13:02:25 roberto Exp $
** Initialization of libraries for lua.c
** See Copyright Notice in lua.h
*/

namespace SharpLua
{
    public partial class Lua
    {
        private readonly static luaL_Reg[] lualibs = {
          new ("", luaopen_base),
          new (LUA_LOADLIBNAME, luaopen_package),
          new (LUA_TABLIBNAME, luaopen_table),
          new (LUA_IOLIBNAME, luaopen_io),
          new (LUA_OSLIBNAME, luaopen_os),
          new (LUA_STRLIBNAME, luaopen_string),
          new (LUA_MATHLIBNAME, luaopen_math),
          new (LUA_DBLIBNAME, luaopen_debug),
          new (null,null)
        };

        public static void luaL_openlibs(lua_State L)
        {
            foreach(var lib in lualibs)
            {
                if (lib.name != null)
                {
                    lua_pushcfunction(L, lib.func);
                    lua_pushstring(L, lib.name);
                    lua_call(L, 1, 0);
                }
            }
        }
    }
}
