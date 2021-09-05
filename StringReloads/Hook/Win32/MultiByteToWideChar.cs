﻿using StringReloads.Engine;
using StringReloads.Engine.String;
using StringReloads.StringModifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StringReloads.Hook
{
    public unsafe class MultiByteToWideChar : Base.Hook<MultiByteToWideCharDelegate>
    {
        List<Base.Hook> Parents = new List<Base.Hook>();
        void* Target = null;
        
        public MultiByteToWideChar()
        {
            if (Target == null && Config.Default.ImportHookEx)
            {
                foreach (var Module in Config.GameModules)
                    Parents.Add(new MultiByteToWideChar(Module.BaseAddress.ToPointer()));
            }
        }
        private MultiByteToWideChar(void* Target) {
            this.Target = Target;
        }
        public override string Library => "Kernel32.dll";

        public override string Export => "MultiByteToWideChar";

        public new void Install() {
            base.Install();

            foreach (var Parent in Parents)
                Parent.Install();
        }

        public new void Uninstall() {
            base.Uninstall();

            foreach (var Parent in Parents)
                Parent.Install();
        }
        public override void Initialize()
        {
            if (Config.Default.ImportHook || Config.Default.ImportHookEx) {
                HookDelegate = PersistentMultiByteToWideChar;
                Compile(true, Target == null ? null : new IntPtr(Target));
            }
            else { 
                HookDelegate = hMultiByteToWideChar;
                Compile();
            }
        }

        private int hMultiByteToWideChar(uint CodePage, uint dwFlags, byte* lpMultiByteStr, int cbMultiByte, char* lpWideCharStr, int cchWideChar)
        {
            Uninstall();
            int Rst = PersistentMultiByteToWideChar(CodePage, dwFlags, lpMultiByteStr, cbMultiByte, lpWideCharStr, cchWideChar);
            Install();
            return Rst;
        }

        private int PersistentMultiByteToWideChar(uint CodePage, uint dwFlags, byte* lpMultiByteStr, int cbMultiByte, char* lpWideCharStr, int cchWideChar)
        {
            if (Config.Default.MultiByteToWideCharCodePage >= 0)
                CodePage = (uint)Config.Default.MultiByteToWideCharCodePage;

            Encoding ReadEncoding = null;
            if (CodePage != 0 && Config.Default.MultiByteToWideCharAutoEncoding)
            {
                ReadEncoding = Config.Default.ReadEncoding;
                Config.Default.ReadEncoding = Encoding.GetEncoding((int)CodePage);
            }

            if (cbMultiByte > 0)
            {
                byte[] Buffer = new byte[cbMultiByte];
                for (int i = 0; i < cbMultiByte; i++)
                {
                    Buffer[i] = *(lpMultiByteStr + i);
                }
                fixed (void* pBuffer = &Buffer[0])
                {
                    var NewStr = (byte*)EntryPoint.Process((CString)pBuffer);
                    if (NewStr != lpMultiByteStr)
                    {
                        lpMultiByteStr = NewStr;
                        cchWideChar = ((CString)lpMultiByteStr).Count();
                        CodePage = (uint)Config.Default.WriteEncoding.CodePage;
                        if (Config.Default.MultiByteToWideCharAutoEncoding)
                            Config.Default.ReadEncoding = Encoding.GetEncoding((int)CodePage);
                    }
                }

            }
            else 
            {
                var NewStr = (byte*)EntryPoint.Process((CString)lpMultiByteStr);
                if (NewStr != lpMultiByteStr)
                {
                    lpMultiByteStr = NewStr;
                    CodePage = (uint)Config.Default.WriteEncoding.CodePage;
                    if (Config.Default.MultiByteToWideCharAutoEncoding)
                        Config.Default.ReadEncoding = Encoding.GetEncoding((int)CodePage);
                }
            }

            if (Config.Default.MultiByteToWideCharUndoRemap)
                lpMultiByteStr = (CString)Remaper.Default.Restore((CString)lpMultiByteStr);

            if (Config.Default.MultiByteToWideCharRemapAlt)
                lpMultiByteStr = (CString)RemaperAlt.Default.Apply((CString)lpMultiByteStr, null);

            if (ReadEncoding != null)
                Config.Default.ReadEncoding = ReadEncoding;

            return Bypass(CodePage, dwFlags, lpMultiByteStr, cbMultiByte, lpWideCharStr, cchWideChar);
        }
    }
}
