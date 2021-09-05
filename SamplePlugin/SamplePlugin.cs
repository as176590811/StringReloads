﻿using System;
using StringReloads;
using StringReloads.Engine;
using StringReloads.Engine.Interface;
using StringReloads.Hook.Base;
using static StringReloads.Engine.User;

namespace SamplePlugin
{
    public class SamplePlugin : IPlugin
    {
        SRL Engine;
        public string Name => "Sample";

        public IAutoInstall[] GetAutoInstallers() => null;

        public IEncoding[] GetEncodings() => null;

        public Hook[] GetHooks() => null;

        public IMatch[] GetMatchs() => null;

        public IStringModifier[] GetModifiers() => new[] { new SampleModifier() };

        public IMod[] GetMods() => null;

        public IReloader[] GetReloaders() => null;

        public void Initialize(SRL Engine)
        {          
            this.Engine = Engine; 
            if (!Config.Default.GetValue("SampleModifier", "Modifiers").ToBoolean())
                ShowMessageBox("The Sample Plugin is loaded, but the SampleModifier isn't enabled in the SRL.ini", "Sample Warning", MBButtons.Ok, MBIcon.Warning);
        }
    }

    public class SampleModifier : IStringModifier
    {       
        public string Name => "SampleModifier";

        public bool CanRestore => true;

        public string Apply(string String, string Original)
        {
            Log.Debug("Sample Modifier Plugin Called");
            return String.Replace("a", "@").Replace("i", "1").Replace("s", "$").Replace("e", "&");
        }

        public string Restore(string String)
        {
            return String.Replace("@", "a").Replace("1", "i").Replace("$", "s").Replace("&", "e");
        }
    }
}
