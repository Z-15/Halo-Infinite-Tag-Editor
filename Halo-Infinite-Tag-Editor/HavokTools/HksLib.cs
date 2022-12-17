using System;
using System.Runtime.InteropServices;

namespace HavokScriptToolsCommon
{
    public class HksLib
    {
        const String DllName = "HavokScript_FinalRelease.dll";

        [DllImport(DllName, EntryPoint = "?luaL_newstate@@YAPEAUlua_State@@XZ")]
        public static extern IntPtr NewState();

        [DllImport(DllName, EntryPoint = "?luaL_loadfile@@YAHPEAUlua_State@@PEBD@Z")]
        public static extern int Loadfile(IntPtr LS, [MarshalAs(UnmanagedType.LPStr)] string filename);

        [DllImport(DllName, EntryPoint = "?luaL_openlibs@@YAXPEAUlua_State@@@Z")]
        public static extern void OpenLibs(IntPtr LS);

        [DllImport(DllName, EntryPoint = "?lua_dump@@YAHPEAUlua_State@@P6AH0PEBX_KPEAX@Z3@Z")]
        public static extern int Dump(IntPtr LS, LuaWriterDelegate writer, object userData);

        [DllImport(DllName, EntryPoint = "?DoFile@LuaState@LuaPlus@@QEAAHPEBD@Z")]
        public static extern int Dofile(IntPtr LS, [MarshalAs(UnmanagedType.LPStr)] string filename);

        [DllImport(DllName, EntryPoint = "?DoString@LuaState@LuaPlus@@QEAAHPEBD@Z")]
        public static extern int Dostring(IntPtr LS, [MarshalAs(UnmanagedType.LPStr)] string code);

        [DllImport(DllName, EntryPoint = "?ReportError@LuaPlus@@YAXPEAUlua_State@@@Z")]
        public static extern void ReportError(IntPtr LS);

        [DllImport(DllName, EntryPoint = "?RegisterErrorCallback@LuaPlus@@YAXP6AXPEAUlua_State@@PEBD@Z@Z")]
        public static extern void RegisterErrorCallback(LuaErrorCallbackDelegate callback);

        public delegate int LuaWriterDelegate(IntPtr LS, IntPtr pData, ulong size, object userData);
        public delegate void LuaErrorCallbackDelegate(IntPtr LS, string message);
    }
}
