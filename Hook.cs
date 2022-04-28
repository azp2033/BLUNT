using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BLUNT
{
    internal class Natives
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("mono.dll", EntryPoint = "mono_compile_method", SetLastError = true)]
        public extern static IntPtr CompileMethod(RuntimeMethodHandle handle);

        public enum PageProtection : uint
        {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }
    }

    public class HookManager
    {
        private bool is64 = false;
        Dictionary<MethodInfo, byte[]> hooks;

        public HookManager()
        {
            PortableExecutableKinds kind;
            ImageFileMachine machine;
            Assembly.GetExecutingAssembly().ManifestModule.GetPEKind(out kind, out machine);
            if (machine == ImageFileMachine.AMD64 || machine == ImageFileMachine.I386)
            {
                is64 = (machine == ImageFileMachine.AMD64);
                hooks = new Dictionary<MethodInfo, byte[]>();
            }
            else
            {
                throw new NotImplementedException("Only Intel processors are supported.");
            }
        }

        public unsafe void Hook(MethodInfo original, MethodInfo replacement)
        {
            if ((object)original == null) throw new ArgumentNullException("original");
            if ((object)replacement == null) throw new ArgumentNullException("replacement");
            if ((object)original == (object)replacement) throw new ArgumentException("A function can't hook itself");
            if (original.IsGenericMethod) throw new ArgumentException("Original method cannot be generic");
            if (replacement.IsGenericMethod || !replacement.IsStatic) throw new ArgumentException("Hook method must be static and non-generic");
            if (hooks.ContainsKey(original)) throw new ArgumentException("Attempting to hook an already hooked method");
            byte[] originalOpcodes = PatchJMP(original, replacement);
            hooks.Add(original, originalOpcodes);
        }

        public unsafe void Unhook(MethodInfo original)
        {
            if (original == null) throw new ArgumentNullException("original");
            if (!hooks.ContainsKey(original)) throw new ArgumentException("Method was never hooked");
            byte[] originalOpcodes = hooks[original];
            UnhookJMP(original, originalOpcodes);
            hooks.Remove(original);
        }

        private unsafe byte[] PatchJMP(MethodInfo original, MethodInfo replacement)
        {
            IntPtr originalSite = Natives.CompileMethod(original.MethodHandle);
            IntPtr replacementSite = Natives.CompileMethod(replacement.MethodHandle);

            uint offset = (is64 ? 13u : 6u);
            byte[] originalOpcodes = new byte[offset];

            unsafe
            {
                uint oldProtecton = VirtualProtect(originalSite, (uint)originalOpcodes.Length, (uint)Natives.PageProtection.PAGE_EXECUTE_READWRITE);
                byte* originalSitePointer = (byte*)originalSite.ToPointer();
                for (int k = 0; k < offset; k++)
                {
                    originalOpcodes[k] = *(originalSitePointer + k);
                }
                if (is64)
                {
                    *originalSitePointer = 0x49;
                    *(originalSitePointer + 1) = 0xBB;
                    *((ulong*)(originalSitePointer + 2)) = (ulong)replacementSite.ToInt64();
                    *(originalSitePointer + 10) = 0x41;
                    *(originalSitePointer + 11) = 0xFF;
                    *(originalSitePointer + 12) = 0xE3;
                }
                else
                {
                    *originalSitePointer = 0x68;
                    *((uint*)(originalSitePointer + 1)) = (uint)replacementSite.ToInt32();
                    *(originalSitePointer + 5) = 0xC3;
                }
                FlushInstructionCache(originalSite, (uint)originalOpcodes.Length);
                VirtualProtect(originalSite, (uint)originalOpcodes.Length, oldProtecton);
            }
            return originalOpcodes;
        }

        private unsafe void UnhookJMP(MethodInfo original, byte[] originalOpcodes)
        {
            IntPtr originalSite = original.MethodHandle.GetFunctionPointer();
            unsafe
            {
                uint oldProtecton = VirtualProtect(originalSite, (uint)originalOpcodes.Length, (uint)Natives.PageProtection.PAGE_EXECUTE_READWRITE);
                byte* originalSitePointer = (byte*)originalSite.ToPointer();
                for (int k = 0; k < originalOpcodes.Length; k++)
                {
                    *(originalSitePointer + k) = originalOpcodes[k];
                }
                FlushInstructionCache(originalSite, (uint)originalOpcodes.Length);
                VirtualProtect(originalSite, (uint)originalOpcodes.Length, oldProtecton);
            }
        }

        private uint VirtualProtect(IntPtr address, uint size, uint protectionFlags)
        {
            uint oldProtection;
            if (!Natives.VirtualProtect(address, (UIntPtr)size, protectionFlags, out oldProtection))
            {
                throw new Win32Exception();
            }
            return oldProtection;
        }

        private void FlushInstructionCache(IntPtr address, uint size)
        {
            if (!Natives.FlushInstructionCache(Natives.GetCurrentProcess(), address, (UIntPtr)size))
            {
                throw new Win32Exception();
            }
        }
    }
}