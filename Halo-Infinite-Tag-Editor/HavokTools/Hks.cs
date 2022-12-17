using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace HavokScriptToolsCommon
{
    public class Hks
    {

        IntPtr LS;
        public Hks()
        {
            LS = HksLib.NewState();
            HksLib.RegisterErrorCallback(LuaErrorCallback);
            HksLib.OpenLibs(LS);
        }

        static private void LuaErrorCallback(IntPtr LS, string message)
        {
            Console.WriteLine("LuaError: " + message);
        }

        static private int LuaDumpCallback(IntPtr LS, IntPtr pData, ulong size, object userData)
        {
            byte[] data = new byte[size];
            unsafe
            {
                Marshal.Copy(pData, data, 0, (int)size);
            }
            BinaryWriter bw = (BinaryWriter)userData;
            bw.Write(data);
            return 0;
        }

        public int Dofile(string filename)
        {
            int err = HksLib.Dofile(LS, filename);
            if (err != 0)
            {
                HksLib.ReportError(LS);
            }
            return err;
        }

        public int Dostring(string code)
        {
            int err = HksLib.Dostring(LS, code);
            if (err != 0)
            {
                HksLib.ReportError(LS);
            }
            return err;
        }

        public int Loadfile(string filename)
        {
            int err = HksLib.Loadfile(LS, filename);
            if (err != 0)
            {
                HksLib.ReportError(LS);
            }
            return err;
        }

        public int Dump(string filename)
        {
            int err = 0;
            using (BinaryWriter bw = new BinaryWriter(File.Open(filename, FileMode.Create)))
            {
                err = HksLib.Dump(LS, LuaDumpCallback, bw);
            }
            if (err != 0)
            {
                HksLib.ReportError(LS);
            }
            return err;
        }
    }
}
