using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using static jit_winform.Program;
using static jit_winform.Runtime;

namespace jit_winform
{
    public static class Runtime
    {
        [DllImport("clrjit.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr getJit();

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate uint CompileMethodDelegate(IntPtr thisPtr, IntPtr comp, IntPtr info, uint flags, IntPtr nativeEntry, ref int nativeSizeOfCode);

        [DllImport("kernel32.dll", BestFitMapping = true, CallingConvention = CallingConvention.Winapi, SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out UInt32 lpflOldProtect);

        static CompileMethodDelegate originalCompiler;
        static Dictionary<IntPtr, byte[]> encryptedHandles;
        static bool Runned;
        public static void Initialize(string resName) {
            var jit = getJit();
            var vTable = Marshal.ReadIntPtr(jit);
            var compiledPtr = Marshal.ReadIntPtr(vTable, 0);

            originalCompiler = (CompileMethodDelegate)Marshal.GetDelegateForFunctionPointer(compiledPtr, typeof(CompileMethodDelegate));
            encryptedHandles = new Dictionary<IntPtr, byte[]>();
            Runned = false;

            var customCompiler = new CompileMethodDelegate(CompiledMethodDelegate);
            var customPtr = Marshal.GetFunctionPointerForDelegate(customCompiler);
            //this is interesting
            RuntimeHelpers.PrepareDelegate(originalCompiler);
            RuntimeHelpers.PrepareMethod(originalCompiler.Method.MethodHandle);
            RuntimeHelpers.PrepareDelegate(customCompiler);
            RuntimeHelpers.PrepareMethod(customCompiler.Method.MethodHandle);

            var module = typeof(Runtime).Module;
            var stream = module.Assembly.GetManifestResourceStream(resName);
            var reader = new BinaryReader(stream);

            var count = reader.ReadInt32();

            for (int i = 0; i < count; i++) { 
                var token = reader.ReadInt32();
                var str = EncryptOrDecrypt(reader.ReadString(), "PROT44");

                var methodBase = module.ResolveMethod(token);

                encryptedHandles.Add(methodBase.MethodHandle.Value, Convert.FromBase64String(str));
            }

            VirtualProtect(vTable, (uint)IntPtr.Size, 64U, out uint oldProtection);
            Marshal.WriteIntPtr(vTable, customPtr);
            VirtualProtect(vTable, (uint)IntPtr.Size, oldProtection, out _);
        }

        static uint CompiledMethodDelegate(IntPtr thisPtr, IntPtr comp, IntPtr info, uint flags, 
            IntPtr nativeEntry, ref int nativeSizeOfCode)
        {
            var methodHandle = Marshal.ReadIntPtr(info, 0);

            if (!encryptedHandles.ContainsKey(methodHandle))
                return originalCompiler(thisPtr, comp, info, flags, nativeEntry, ref nativeSizeOfCode);

            //var ilCodeSize = Marshal.ReadInt32(info, IntPtr.Size * 3);
            //var ilCodePtr = Marshal.ReadIntPtr(info, IntPtr.Size * 2);

            //var ilCode = new byte[ilCodeSize];
            //Marshal.Copy(ilCodePtr, ilCode, 0, ilCodeSize);

            //for (int i = 0; i < ilCode.Length; i++)
            //    ilCode[i] = (byte)(ilCode[i] ^ 15);
            var ilBytes = encryptedHandles[methodHandle];

            IntPtr customIlCodePtr = Marshal.AllocCoTaskMem(ilBytes.Length);
            //I can definitely make this better
            Marshal.Copy(ilBytes, 0, customIlCodePtr, ilBytes.Length);
            Marshal.WriteIntPtr(info, IntPtr.Size * 2, customIlCodePtr);
            Marshal.WriteInt32(info, IntPtr.Size * 3, ilBytes.Length);

            uint ret = 0U;

            if (flags == 216669565U && !Runned)
                Runned = true;
            else
                return originalCompiler(thisPtr, comp, info, flags, nativeEntry, ref nativeSizeOfCode);
            return ret;
        }

        static string EncryptOrDecrypt(string text, string key)
        {
            var result = new StringBuilder();

            for (int c = 0; c < text.Length; c++)
                result.Append((char)((uint)text[c] ^ (uint)key[c % key.Length]));

            return result.ToString();
        }
    }
}
