﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SRL
{
    partial class StringReloader
    {

        /// <summary>
        /// Check if the string is a Mask
        /// </summary>
        /// <param name="String">The String to Verify</param>
        /// <returns>If is a mask, return true.</returns>
        internal static bool IsMask(string String)
        {
            return SplitMask(String).Length > 1;
        }

        /// <summary>
        /// Check if the Mask match with a string
        /// </summary>
        /// <param name="Mask">The Mask to check</param>
        /// <param name="String">The String to try match with the mask</param>
        /// <returns>If String is usable with this mask, return true</returns>
        internal static bool MaskMatch(string Mask, string String)
        {
            string[] Source = SplitMask(Mask);
            for (int i = 0, x = 0; i < Source.LongLength; i++)
            {
                string M = Source[i];
                if (M == string.Empty)
                    continue;

                if (i == 0 && !String.StartsWith(M))
                    return false;

                while (!String.Substring(x, String.Length - x).StartsWith(M))
                {
                    if (++x >= String.Length)
                        return false;
                }

                if (i + 1 >= Source.LongLength && String.Substring(x, String.Length - x) != M)
                    return false;

                x += M.Length;
            }
            return true;
        }

        /// <summary>
        /// Replace the content of the source string to the target mask using the Mask as base
        /// </summary>
        /// <param name="Mask">The Mask to use as base</param>
        /// <param name="String">The Original String</param>
        /// <param name="Target">The Target String Mask</param>
        /// <returns>The Modified Target String</returns>
        internal static string MaskReplace(string Mask, string String, string Target)
        {
            List<object> Format = new List<object>();
            string[] Source = SplitMask(Mask);
            for (int i = 0, x = 0; i < Source.LongLength; i++)
            {
                string M = Source[i];
                if (string.IsNullOrEmpty(M))
                {
                    if (i + 1 >= Source.LongLength)
                    {
                        Format.Add(String.Substring(x, String.Length - x));
                    }
                    continue;
                }

                if (i == 0 && !String.StartsWith(M))
                {
                    Warning("Invalid Mask Replace Request\nM: {0}\nS: {1}", Mask, String);
                    return String;
                }

                int Skiped = x;
                while (!String.Substring(x, String.Length - x).StartsWith(M))
                {
                    if (++x >= String.Length)
                    {
                        Warning("Invalid Mask Replace Request\nM: {0}\nS: {1}", Mask, String);
                        return String;
                    }
                }
                if (i + 1 >= Source.LongLength && String.Substring(x, String.Length - x) != M)
                {
                    Warning("Invalid Mask Replace Request\nM: {0}\nS: {1}", Mask, String);
                    return String;
                }
                if (x > Skiped)
                {
                    string Content = String.Substring(Skiped, x - Skiped);

                    #region FuckYouAppVeyor
                    int Passed1;
                    uint Passed2;
                    long Passed3;
                    ulong Passed4;
                    float Passed5;
                    double Passed6;
                    #endregion

                    //Brute-Force Data Type :P
                    if (int.TryParse(Content, out Passed1))
                        Format.Add(Passed1);
                    else if (uint.TryParse(Content, out Passed2))
                        Format.Add(Passed2);
                    else if (long.TryParse(Content, out Passed3))
                        Format.Add(Passed3);
                    else if (ulong.TryParse(Content, out Passed4))
                        Format.Add(Passed4);
                    else if (float.TryParse(Content, out Passed5))
                        Format.Add(Passed5);
                    else if (double.TryParse(Content, out Passed6))
                        Format.Add(Passed6);
                    else
                        Format.Add(Content);
                }
                x += M.Length;
            }

            int[] Sort = MaskSort(Mask);
            object[] F = Format.ToArray();
            if (Sort.Length != F.Length)
            {
                //Not log as error because sometimes is just a bad input from the game.
                Warning("Invalid Mask Replace Request\nM: {0}\nS: {1}", true, Mask, String);
                return String;
            }
            Array.Sort(Sort, F);


            //::MAXWIDTH[?]::
            uint BakWidth = MaxWidth;
            if (Target.StartsWith("::MAXWIDTH["))
            {
                string Width = Target.Split('[')[1].Split(']')[0];
                int TagLen = 14 + Width.Length;
                if (Target.Substring(TagLen - 3, 3) == "]::")
                {
                    Target = Target.Substring(TagLen, Target.Length - TagLen);
                    try
                    {
                        MaxWidth = uint.Parse(Width);
                    }
                    catch { }
                }
            }

            if (ReloadMaskParameters && Initialized)
            {
                for (long i = 0; i < F.LongLength; i++)
                {
                    if (F[i] is string)
                        F[i] = StrMap((string)F[i], IntPtr.Zero, false);

                }
            }

            MaxWidth = BakWidth;

            return string.Format(Target, F);
        }

        /// <summary>
        /// Split the Mask in Parts to Process
        /// </summary>
        /// <param name="String">The Mask</param>
        /// <returns>The Splited Mask</returns>
        internal static string[] SplitMask(string String)
        {
            List<string> Strings = new List<string>();
            string Content = string.Empty;
            for (int i = 0; i < String.Length; i++)
            {
                char c = String[i];
                if (c == '{' && !Content.EndsWith("\\"))
                {
                    int o = i;
                    while (i < String.Length && String[i] != '}')
                        i++;
                    if (i >= String.Length || String[i] != '}')
                    {
                        Warning("Bad String Format: \"{0}\"\nAt: {0}", String, o);
                    }

                    Strings.Add(Content);
                    Content = string.Empty;
                    continue;
                }
                if (Content.EndsWith("\\") && c == '{')
                {
                    Content = Content.Substring(0, Content.Length - 1);
                }
                Content += c;
            }
            Strings.Add(Content);
            return Strings.ToArray();
        }

        /// <summary>
        /// Get order of Mask Entries
        /// </summary>
        /// <param name="String">The Mask</param>
        /// <returns>The Index Array</returns>
        internal static int[] MaskSort(string String)
        {
            List<int> IDS = new List<int>();
            string Content = string.Empty;
            for (int i = 0; i < String.Length; i++)
            {
                char c = String[i];
                if (c == '{' && !Content.EndsWith("\\"))
                {
                    int o = i;
                    while (i < String.Length && String[i] != '}')
                        i++;
                    if (i >= String.Length || String[i] != '}')
                    {
                        Warning("Bad String Format: \"{0}\"\nAt: {0}", String, o);
                    }
                    o++;
                    string Str = String.Substring(o, i - o).Split(':')[0];
                    int ID = 0;
                    if (int.TryParse(Str, out ID))
                        IDS.Add(ID);
                    else
                        Warning("Bad Mask Format, Invalid ID: {0}", Str);
                    continue;
                }
            }

            return IDS.ToArray();
        }

        internal static string PrefixWorker(string Str)
        {
            string Reloaded = Str;
            if (Reloaded.Contains(AntiPrefixFlag) || string.IsNullOrEmpty(RldPrefix))
                Reloaded = Reloaded.Replace(AntiPrefixFlag, string.Empty);
            else
                Reloaded = RldPrefix + Reloaded;

            if (Reloaded.Contains(AntiSufixFlag) || string.IsNullOrEmpty(RldSufix))
                Reloaded = Reloaded.Replace(AntiSufixFlag, string.Empty);
            else
                Reloaded = Reloaded + RldSufix;

            return Reloaded;
        }

        /// <summary>
        /// Wordwrap a string
        /// </summary>
        /// <param name="Input">The string to wordwrap</param>
        /// <returns>The Result String</returns>        
        internal static string WordWrap(string Input)
        {
            if (Input.StartsWith(AntiWordWrapFlag))
                return Input.Substring(AntiWordWrapFlag.Length, Input.Length - AntiWordWrapFlag.Length);

            //::MAXWIDTH[?]::
            uint BakWidth = MaxWidth;
            if (Input.StartsWith("::MAXWIDTH["))
            {
                string Width = Input.Split('[')[1].Split(']')[0];
                int TagLen = 14 + Width.Length;
                if (Input.Substring(TagLen - 3, 3) == "]::")
                {
                    Input = Input.Substring(TagLen, Input.Length - TagLen);
                    try
                    {
                        MaxWidth = uint.Parse(Width);
                    }
                    catch { }
                }
            }

            if (FakeBreakLine)
            {
                while (Input.Contains(@"  "))
                    Input = Input.Replace(@"  ", @" ");
            }

            if (Monospaced)
            {
                string Rst = MonospacedWordWrap(MergeLines(Input));
                MaxWidth = BakWidth;
                return Rst;
            }
            else
            {
#if DEBUG
                if (Debugging) {
                    if (LastDPI != DPI) {
                        if (LastDPI == 0)
                            Log("Wordwrap DPI: {0}", true, DPI);
                        else
                            Warning("Wordwrap DPI: {0}", false, DPI);

                        LastDPI = DPI;
                    }
                }
#endif
                string Rst = ReplaceChars(MergeLines(Input), true);
                Rst = MultispacedWordWrap(Rst);
                MaxWidth = BakWidth;
                return ReplaceChars(Rst);
            }
        }

        /// <summary>
        /// Remove Break Lines
        /// </summary>
        /// <param name="String">The string to remove the breakline</param>
        /// <returns>The Result</returns>
        internal static string MergeLines(string String)
        {
            if (NoTrim)
                return String;

            string Rst = String.Replace(" " + GameLineBreaker + " ", "  ");
            Rst = Rst.Replace(GameLineBreaker + " ", " ");
            Rst = Rst.Replace(" " + GameLineBreaker, " ");
            Rst = Rst.Replace(GameLineBreaker, " ");
            return Rst;
        }

        #region WordWrap
        private static string MultispacedWordWrap(string String)
        {
            StringBuilder sb = new StringBuilder();
            if (MaxWidth == 0)
                return String;
            string[] Words = String.Split(' ');
            string Line = string.Empty;
            foreach (string Word in Words)
            {
                if (GetTextWidth(Font, Line + Word) > MaxWidth)
                {
                    if (Line == string.Empty)
                    {
                        string Overload = string.Empty;
                        int Cnt = 0;
                        while (GetTextWidth(Font, Word.Substring(0, Word.Length - Cnt)) > MaxWidth)
                            Cnt++;
                        sb.AppendLine(Word.Substring(0, Word.Length - Cnt));
                        Line = Word.Substring(Word.Length - Cnt, Cnt);
                    }
                    else
                    {
                        sb.AppendLine(Line);
                        Line = Word;
                    }
                }
                else
                    Line += (Line == string.Empty) ? Word : " " + Word;
            }
            if (Line != string.Empty)
                sb.AppendLine(Line);

            string rst = sb.ToString().Replace("\r\n", "\n");

            rst = rst.Replace("\n", GameLineBreaker);

            if (rst.EndsWith(GameLineBreaker))
                rst = rst.Substring(0, rst.Length - GameLineBreaker.Length);

            if (FakeBreakLine)
            {
                float SpaceLen = GetTextWidth(Font, "_ _") - (GetTextWidth(Font, "_") * 2);
                string[] Splited = rst.Replace(GameLineBreaker, "\n").Split('\n');
                string NewRst = string.Empty;
                for (int i = 0; i < Splited.Length; i++)
                {
                    string tmp = Splited[i];

                    int Spaces = 0;
                    bool Last = i + 1 >= Splited.Length;
                    if (!Last)
                        while (GetTextWidth(Font, tmp) + (SpaceLen * Spaces) < MaxWidth)
                            Spaces++;

                    tmp += new string(' ', Spaces);

                    NewRst += tmp;
                }
                rst = NewRst;
            }

            return rst;
        }

        internal static float GetTextWidth(Font Font, string Text)
        {
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                    return g.MeasureString(Text, Font).Width;
            }
            catch
            {
                return System.Windows.Forms.TextRenderer.MeasureText(Text, Font).Width;
            }
        }

        internal static string MonospacedWordWrap(string String)
        {
            int pos, next;
            StringBuilder sb = new StringBuilder();
            if (MaxWidth < 1)
                return String;
            for (pos = 0; pos < String.Length; pos = next)
            {
                int eol = String.IndexOf(GameLineBreaker, pos);
                if (eol == -1)
                    next = eol = String.Length;
                else
                    next = eol + GameLineBreaker.Length;
                if (eol > pos)
                {
                    do
                    {
                        int len = eol - pos;
                        if (len > MaxWidth)
                            len = BreakLine(String, pos, (int)MaxWidth);
                        sb.Append(String, pos, len);
                        sb.Append(GameLineBreaker);
                        pos += len;
                        while (pos < eol && char.IsWhiteSpace(String[pos]))
                            pos++;
                    } while (eol > pos);
                }
                else sb.Append(GameLineBreaker);
            }
            string rst = sb.ToString();


            if (rst.EndsWith(GameLineBreaker))
                rst = rst.Substring(0, rst.Length - GameLineBreaker.Length);

            if (FakeBreakLine)
            {
                string[] Splited = rst.Replace(GameLineBreaker, "\n").Split('\n');
                string NewRst = string.Empty;
                for (int i = 0; i < Splited.Length; i++)
                {
                    string tmp = Splited[i];
                    bool Last = i + 1 >= Splited.Length;
                    if (!Last)
                        while (tmp.Length < MaxWidth)
                            tmp += ' ';
                    NewRst += tmp;
                }
                rst = NewRst;
            }

            return rst;
        }
        private static int BreakLine(string text, int pos, int max)
        {
            int i = max - 1;
            while (i >= 0 && !char.IsWhiteSpace(text[pos + i]))
                i--;
            if (i < 0)
                return max;
            while (i >= 0 && char.IsWhiteSpace(text[pos + i]))
                i--;
            return i + 1;
        }
        #endregion

        /// <summary>
        /// Detect the correct string reader method, and load it
        /// </summary>
        /// <param name="Pointer">The pointer to the string</param>
        /// <param name="Decode">if False, return the hex of the string</param>
        /// <returns>The String</returns>
        internal static string GetString(IntPtr Pointer, bool Decode = true, int? Len = null, int? CP = null)
        {
            if (EncodingModifier != null)
                return EncodingModifier.Call("Modifier", "GetString", Pointer, Decode);

            if (Unicode && CP == null && Len == null)
                return GetStringW(Pointer, Decode);
            else
                return GetStringA(Pointer, Decode, CP, Len);
        }

        /// <summary>
        /// Gen a Null Terminated String
        /// </summary>
        /// <param name="String">The string</param>
        /// <returns>The Pointer to the new String</returns>
        internal static IntPtr GenString(string String, IntPtr OriPointer, bool ForceUnicode = false, IntPtr? ForcePointer = null)
        {
            byte[] buffer;
            if (ForceUnicode)
            {
                buffer = Encoding.Unicode.GetBytes(String + "\x0");
            }
            else
            {
                if (EncodingModifier != null)
                {
                    if (ModifierRewriteMode == null)
                    {
                        var Method = EncodingModifier.SearchMethods("Modifier", "GenString").Single();
                        ModifierRewriteMode = Method.GetParameters().Length == 2;

                        if (ModifierRewriteMode.Value)
                            Log("Encoding Modifier Rewrite Mode Enabled", true);
                    }

                    if (ModifierRewriteMode.Value)
                    {
                        buffer = EncodingModifier.Call("Modifier", "GenString", String, OriPointer);

                        if (buffer == null)
                        {
                            Log("Rewrite Output: {0}", true, String);
                            return OriPointer;
                        }
                    }
                    else
                    {
                        buffer = EncodingModifier.Call("Modifier", "GenString", String);
                    }
                }
                else
                {
                    int len = WriteEncoding.GetByteCount(String + "\x0");
                    buffer = new byte[len];
                    WriteEncoding.GetBytes(String, 0, String.Length, buffer, 0);

#if LEAKING //Less Memory Leak, but works only with some games
                IntPtr Pointer = LastGenerated;
                if (LastGenerated == IntPtr.Zero) {
                    Pointer = Marshal.AllocHGlobal(buffer.Length);
                } else {
                    if (AllocLen < buffer.Length) {
                        while (AllocLen < buffer.Length)
                            AllocLen++;
                        Pointer = Marshal.ReAllocHGlobal(Pointer, new IntPtr(AllocLen));
                    }
                }
                LastGenerated = Pointer;
#endif
                }
            }
            IntPtr Pointer = ForcePointer ?? Marshal.AllocHGlobal(buffer.Length);

            Marshal.Copy(buffer, 0, Pointer, buffer.Length);

            if (LogAll)
            {
                string New = GetString(Pointer);
                Log("Old: {0}\nNew: {1}\nHex: {2}", false, String, New, ParseBytes(buffer));
            }

            return Pointer;
        }

        /// <summary>
        /// Get a null terminated String
        /// </summary>
        /// <param name="Pointer">Pointer to the string</param>
        /// <param name="Decode">if False, return the hex of the string</param>
        /// <returns></returns>
        internal static string GetStringA(IntPtr Pointer, bool Decode = true, int? CP = null, int? Len = null)
        {
            if (Pointer == IntPtr.Zero)
                return null;

            int len = 0;
            if (Len == null)
            {
                while (Marshal.ReadByte(Pointer, len) != 0)
                    ++len;
            }
            else
                len = Len.Value;

            byte[] buffer = new byte[len];
            Marshal.Copy(Pointer, buffer, 0, buffer.Length);

            if ((LogInput || LogAll) && !DumpStrOnly)
            {
                Log("Input: {0}", true, ParseBytes(buffer));
            }

            if (Unicode && CP == null)
            {
                return Encoding.Default.GetString(buffer);
            }
            else
            {
                if (CP != null)
                {
                    Encoding Enco;
                    switch (CP)
                    {
                        case 0:
                        case 3:
                        case 2:
                        case 1:
                            Enco = Encoding.Default;
                            break;

                        default:
                            Enco = (from x in Encoding.GetEncodings() where x.CodePage == CP select x.GetEncoding()).FirstOrDefault() ?? WriteEncoding;
                            break;
                    }
                    return Enco.GetString(buffer);
                }
                else if (Decode)
                    return ReadEncoding.GetString(buffer);
                else
                    return ParseBytes(buffer);
            }
        }

        /// <summary>
        /// Get WideByte null terminated String
        /// </summary>
        /// <param name="Pointer">Pointer to the string</param>
        /// <param name="Decode">if False, return the hex of the string</param>
        /// <returns></returns>
        internal static string GetStringW(IntPtr Pointer, bool Decode = true, bool ForceUnicode = false)
        {
            if (Pointer == IntPtr.Zero)
                return null;

            int len = 0;
            while (Marshal.ReadInt16(Pointer, len) != 0)
                len += 2;
            byte[] buffer = new byte[len];
            Marshal.Copy(Pointer, buffer, 0, buffer.Length);

            if ((LogInput || LogAll) && !DumpStrOnly)
            {
                Log("Input: {0}", true, ParseBytes(buffer));
            }

            if (Decode)
                return ForceUnicode ? Encoding.Unicode.GetString(buffer) : ReadEncoding.GetString(buffer);
            else
                return ParseBytes(buffer);
        }

        /// <summary>
        /// Check if a Char is in the User Acceptable Range
        /// </summary>
        /// <param name="Char">The Char to Check</param>
        /// <returns>If the char is in the acceptable range, return true</returns>
        internal static bool InRange(char Char)
        {
            foreach (Range Range in Ranges)
                if (Char >= Range.Min && Char <= Range.Max)
                    return true;
            return false;
        }

        /// <summary>
        /// Restore the content removed by the Trim
        /// </summary>
        /// <param name="String">String to Restore</param>
        /// <param name="Original">Original Template</param>
        internal static void TrimWorker(ref string String, string Original)
        {
            if (NoTrim)
                return;

            if (LogAll)
            {
                Log("Trim Request:\nOri: {0}\nStr: {1}", true, Original, String);
            }

            if (IgnoreTag || TagCleaner)
                Original = RemoveTags(Original);

            String = TrimString(String);

            string Test = TrimStart(Original);
            int Diff = Original.Length - Test.Length;

            String = Original.Substring(0, Diff) + String;
            Test = TrimEnd(Original);

            Diff = Original.Length - Test.Length;
            String += Original.Substring(Original.Length - Diff, Diff);

            if (LogAll)
            {
                Log("Trim Result: {0}", true, String);
            }
        }

        /// <summary>
        /// Add a Reply to the Cache
        /// </summary>
        /// <param name="Str">The Reply String</param>
        internal static void CacheReply(string Str)
        {
            string Reply = SimplfyMatch(Str);

            if (Replys.Contains(Reply))
                return;

            if (ReplyPtr > 200)
                ReplyPtr = 0;

            Replys.Insert(ReplyPtr++, Reply);

#if DEBUG
            if (Debugging)
                Log("\"{0}\" Added to the cache at {1}", false, Reply, ReplyPtr - 1);
#endif
        }

        /// <summary>
        /// Check if a String is in the cache
        /// </summary>
        /// <param name="Str">The Reply String</param>
        internal static bool InCache(string Str)
        {
            string Reply = SimplfyMatch(Str);

            if (Replys.Contains(Reply))
                return true;

            int Last = ReplyPtr - 1;
            if (Last < 0)
                Last = CacheLength;

            if (Replys.Count > Last && (Replys[Last].EndsWith(Reply) || Replys[Last].StartsWith(Reply)) && Replys[Last].Length >= 3)
                return true;

            return false;
        }



        /// <summary>
        /// Cache a Pointer and your response
        /// </summary>
        /// <param name="Input">Original Pointer</param>
        /// <param name="Output">Response Pointer</param>
        internal static void CachePtr(IntPtr Input, IntPtr Output)
        {
            if (PtrCacheIn.Contains(Input))
                return;

            if (CacheArrPtr > CacheLength)
                CacheArrPtr = 0;

            PtrCacheIn.Insert(CacheArrPtr, Input);
            PtrCacheOut.Insert(CacheArrPtr++, Output);

#if DEBUG
            if (Debugging)
                Log("\"{0:D16}\" Added to the cache at {1}", false, Output, CacheArrPtr - 1);
#endif
        }


        /// <summary>
        /// Minify a String at the max.
        /// </summary>
        /// <param name="Str">The string to Minify</param>
        /// <returns>The Minified String</returns>
        internal static string SimplfyMatch(string Str)
        {
            if (LiteMode)
                return Str;

            if (IgnoreTag || TagCleaner)
                Str = RemoveTags(Str);

            string Output = TrimString(MergeLines(Str));
            for (int i = 0; i < MatchDel.Length; i++)
                Output = Output.Replace(MatchDel[i], "");

            return CaseSensitive ? Output : Output.ToLower();
        }

        /// <summary>
        /// Replace all user matchs in the string
        /// </summary>
        /// <param name="Input">The String to Replace</param>
        /// <returns>The Result String</returns>
        internal static string ReplaceChars(string Input, bool Restore = false)
        {
            string Output = Input;
            for (int i = 0; i < Replaces.Length; i += 2)
                Output = Output.Replace(Restore ? Replaces[i + 1] : Replaces[i], Restore ? Replaces[i] : Replaces[i + 1]);
            return Output;
        }


        /// <summary>
        /// Trim a String
        /// </summary>
        /// <param name="Txt">The String to Trim</param>
        /// <returns>The Result</returns>
        internal static string TrimString(string Input, bool Force = false)
        {
            if ((NoTrim || LiteMode) && !Force)
                return Input;

            string Result = Input;
            Result = TrimStart(Result, Force);
            Result = TrimEnd(Result, Force);
#if DEBUG
            if (LogAll) {
                Log("Trim: {0} to {1}", true, Input, Result);
            }
#endif
            return Result;
        }

        /// <summary>
        /// Trim the Begin of the String
        /// </summary>
        /// <param name="Txt">The String to Trim</param>
        /// <returns>The Result</returns>
        internal static string TrimStart(string Txt, bool Force = false)
        {
            if ((NoTrim || LiteMode) && !Force)
                return Txt;

            string rst = Txt;
            foreach (string str in TrimChars)
            {
                if (string.IsNullOrEmpty(str))
                    continue;
                while (rst.StartsWith(str))
                {
                    rst = rst.Substring(str.Length, rst.Length - str.Length);
                }
            }

            if (TrimRangeMismatch && Ranges != null)
            {
                int Len = rst.Length - 1;
                while (Len != rst.Length)
                {
                    Len = rst.Length;
                    while (!string.IsNullOrEmpty(rst) && !InRange(rst[0]))
                    {
                        rst = rst.TrimStart(rst[0]);
                    }
                    if (!string.IsNullOrEmpty(rst) && rst.Length > 2 && !InRange(rst[1]) && !InRange(rst[2]))
                    {
                        rst = rst.TrimStart(rst[0]);
                        continue;
                    }
                    break;
                }
            }

            if (rst != Txt)
                rst = TrimStart(rst);

            return rst;
        }

        /// <summary>
        /// Trim the End of the String
        /// </summary>
        /// <param name="Txt">The String to Trim</param>
        /// <returns>The Result</returns>
        internal static string TrimEnd(string Txt, bool Force = false)
        {
            if ((NoTrim || LiteMode) && !Force)
                return Txt;

            string rst = Txt;
            foreach (string str in TrimChars)
            {
                if (string.IsNullOrEmpty(str))
                    continue;
                while (rst.EndsWith(str))
                {
                    rst = rst.Substring(0, rst.Length - str.Length);
                }
            }

            if (TrimRangeMismatch && Ranges != null)
            {
                int Len = rst.Length - 1;
                while (Len != rst.Length)
                {
                    Len = rst.Length;
                    while (!string.IsNullOrEmpty(rst) && !InRange(rst[rst.Length - 1]))
                    {
                        rst = rst.TrimEnd(rst[rst.Length - 1]);
                    }
                    if (!string.IsNullOrEmpty(rst) && rst.Length > 2 && !InRange(rst[rst.Length - 2]) && !InRange(rst[rst.Length - 3]))
                    {
                        rst = rst.TrimStart(rst[rst.Length - 1]);
                        continue;
                    }
                    break;
                }
            }

            if (rst != Txt)
                rst = TrimEnd(rst);

            return rst;
        }

        /// <summary>
        /// Remove All Tags from the given Line
        /// </summary>
        /// <param name="Line">The Line with tags</param>
        /// <returns>The line without tags</returns>
        internal static string RemoveTags(string Line)
        {
            if (TagChars.Length < 2)
                return Line;

            char Open = TagChars[0];
            char Close = TagChars[1];

            if (!Line.Contains(Open) || !Line.Contains(Close))
                return Line;

            string Buff = Line;
            Line = string.Empty;
            bool InTag = false;
            string Tag = string.Empty;

            while (!string.IsNullOrEmpty(Buff))
            {
                char c = Buff[0];
                Buff = Buff.Substring(1, Buff.Length - 1);
                if (c == Open)
                    InTag = true;
                if (c == Close && InTag)
                {
                    InTag = false;
                    bool Bypass = false;
                    Tag += c;

                    //foreach (string Allow in Allowed)
                    //    Bypass |= Tag.Contains(Allow);

                    if (Bypass)
                        Line += Tag;

                    Tag = string.Empty;
                    continue;
                }

                if (InTag)
                {
                    Tag += c;
                    continue;
                }
                Line += c;
            }

            return Line;
        }

        /// <summary>
        /// Convert a Byte Array to Hex
        /// </summary>
        /// <param name="Arr">Byte Array to Convert</param>
        /// <returns>The Result Hex</returns>
        internal static string ParseBytes(byte[] Arr)
        {
            string Result = "0x";
            foreach (byte b in Arr)
                Result += string.Format("{0:X2}", b);
            return Result;
        }

        internal static byte[] ParseHex(string Hex)
        {
            if (Hex.StartsWith("0x"))
                Hex = Hex.Substring(2, Hex.Length - 2);
            Hex = Hex.Replace(@" ", "");

            byte[] Buffer = new byte[Hex.Length / 2];
            for (int i = 0; i < Hex.Length / 2; i++)
            {
                Buffer[i] = Convert.ToByte(Hex.Substring(i, 2), 16);
            }

            return Buffer;
        }

        /// <summary>
        /// Check if the string looks a dialog line
        /// </summary>
        /// <param name="Str">The String</param>
        /// <param name="Trim">Internal Parameter, don't change it.</param>
        /// <returns>If looks a dialog, return true, else return false.</returns>
        public static bool IsDialog(this string String)
        {
            if (string.IsNullOrWhiteSpace(String))
                return false;

            if (UseDatabase && ContainsKey(String))
                return true;

            if (!DialogCheck)
                return true;

            string Str = String.Trim();

            if (ForceTrim)
                Str = TrimString(Str, true);

            foreach (string Ignore in IgnoreList)
                if (!string.IsNullOrEmpty(Ignore))
                    Str = Str.Replace(Ignore, "");

            foreach (string Deny in DenyList)
                if (!string.IsNullOrEmpty(Deny) && Str.ToLower().Contains(Deny.ToLower()))
                    return false;


            Str = Str.Replace(GameLineBreaker, "\n");

            if (string.IsNullOrWhiteSpace(Str))
                return false;

            string[] Words = Str.Split(' ');

            char[] PontuationJapList = new char[] { '。', '？', '！', '…', '、', '―' };
            char[] SpecialList = new char[] { '_', '=', '+', '#', ':', '$', '@' };
            char[] PontuationList = new char[] { '.', '?', '!', '…', ',' };
            int Spaces = Str.Where(x => x == ' ' || x == '\t').Count();
            int Pontuations = Str.Where(x => PontuationList.Contains(x)).Count();
            int WordCount = Words.Where(x => x.Length >= 2 && !string.IsNullOrWhiteSpace(x)).Count();
            int Specials = Str.Where(x => char.IsSymbol(x)).Count();
            Specials += Str.Where(x => char.IsPunctuation(x)).Count() - Pontuations;
            int SpecialsStranges = Str.Where(x => SpecialList.Contains(x)).Count();

            int Uppers = Str.Where(x => char.IsUpper(x)).Count();
            int Latim = Str.Where(x => x >= 'A' && x <= 'z').Count();
            int Numbers = Str.Where(x => x >= '0' && x <= '9').Count();
            int NumbersJap = Str.Where(x => x >= '０' && x <= '９').Count();
            int JapChars = Str.Where(x => (x >= '、' && x <= 'ヿ') || (x >= '｡' && x <= 'ﾝ')).Count();
            int Kanjis = Str.Where(x => x >= '一' && x <= '龯').Count();


            bool IsCaps = GetLineCase(Str) == Case.Upper;
            bool IsJap = JapChars + Kanjis > Latim / 2;


            //More Points = Don't Looks a Dialogue
            //Less Points = Looks a Dialogue
            int Points = 0;

            if (Str.Length > 4)
            {
                string ext = Str.Substring(Str.Length - 4, 4);
                try
                {
                    if (System.IO.Path.GetExtension(ext).Trim('.').Length == 3)
                        Points += 2;
                }
                catch { }
            }

            bool BeginQuote = false;
            Quote? LineQuotes = null;
            foreach (Quote Quote in QuoteList)
            {
                BeginQuote |= Str.StartsWith(Quote.Start.ToString());

                if (Str.StartsWith(Quote.Start.ToString()) && Str.EndsWith(Quote.End.ToString()))
                {
                    Points -= 3;
                    LineQuotes = Quote;
                    break;
                }
                else if (Str.StartsWith(Quote.Start.ToString()) || Str.EndsWith(Quote.End.ToString()))
                {
                    Points--;
                    LineQuotes = Quote;
                    break;
                }
            }
            try
            {
                char Last = (LineQuotes == null ? Str.Last() : Str.TrimEnd(LineQuotes.Value.End).Last());
                if (IsJap && PontuationJapList.Contains(Last))
                    Points -= 3;

                if (!IsJap && (PontuationList).Contains(Last))
                    Points -= 3;

            }
            catch { }
            try
            {
                char First = (LineQuotes == null ? Str.First() : Str.TrimEnd(LineQuotes.Value.Start).First());
                if (IsJap && PontuationJapList.Contains(First))
                    Points -= 3;

                if (!IsJap && (PontuationList).Contains(First))
                    Points -= 3;

            }
            catch { }

            if (!IsJap)
            {
                foreach (string Word in Words)
                {
                    int WNumbers = Word.Where(c => char.IsNumber(c)).Count();
                    int WLetters = Word.Where(c => char.IsLetter(c)).Count();
                    if (WLetters > 0 && WNumbers > 0)
                    {
                        Points += 2;
                    }
                    if (Word.Trim(PontuationList).Where(c => PontuationList.Contains(c)).Count() != 0)
                    {
                        Points += 2;
                    }
                }
            }

            if (!BeginQuote && !char.IsLetter(Str.First()))
                Points += 2;

            if (Specials > WordCount)
                Points++;

            if (Specials > Latim + JapChars)
                Points += 2;

            if (SpecialsStranges > 0)
                Points += 2;

            if (SpecialsStranges > 3)
                Points++;

            if ((Pontuations == 0) && (WordCount <= 2) && !IsJap)
                Points++;

            if (Uppers > Pontuations + 2 && !IsCaps)
                Points++;

            if (Spaces > WordCount * 2)
                Points++;

            if (IsJap && Spaces == 0)
                Points--;

            if (!IsJap && Spaces == 0)
                Points += 2;

            if (WordCount <= 2 && Numbers != 0)
                Points += (int)(Str.PercentOf(Numbers) / 10);

            if (Str.Length <= 3 && !IsJap)
                Points++;

            if (Numbers >= Str.Length)
                Points += 3;

            if (IsJap && Kanjis / 2 > JapChars)
                Points--;

            if (IsJap && JapChars > Kanjis)
                Points--;

            if (IsJap && Latim != 0)
                Points += (int)(Str.PercentOf(Latim) / 10) + 2;

            if (IsJap && NumbersJap != 0)
                Points += (int)(Str.PercentOf(NumbersJap) / 10) + 2;

            if (IsJap && Numbers != 0)
                Points += (int)(Str.PercentOf(Numbers) / 10) + 3;

            if (IsJap && Pontuations != 0)
                Points += (int)(Str.PercentOf(Pontuations) / 10) + 2;

            if (Str.Trim() == string.Empty)
                return false;

            if (Str.Trim().Trim(Str.Trim().First()) == string.Empty)
                Points += 2;

            if (IsJap != AsianInput)
                return false;

            bool Result = Points < Sensitivity;
            return Result;
        }
        internal static double PercentOf(this string String, int Value)
        {
            var Result = Value / (double)String.Length;
            return Result * 100;
        }
        public enum Case
        {
            Lower, Upper, Normal, Title
        }
        public static Case GetLineCase(string String)
        {
            string[] Words = String.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            Case[] WordsCase = new Case[Words.Length];
            for (int x = 0; x < Words.Length; x++)
            {
                string Word = Words[x];
                for (int i = 0; i < Word.Length; i++)
                {
                    char Char = Word[i];
                    if (Char > 0x8000)
                        return Case.Normal;

                    if (i == 0)
                    {
                        if (char.IsLetter(Char) && char.IsUpper(Char))
                        {
                            WordsCase[x] = (x == 0 || (char.IsPunctuation(Words[x - 1].Last()))) ? Case.Normal : Case.Title;
                        }
                        else
                        {
                            WordsCase[x] = Case.Lower;
                        }
                    }
                    else
                    {
                        if (char.IsUpper(Char))
                            WordsCase[x] = Case.Upper;
                        break;
                    }
                }
            }

            int Titles = (from x in WordsCase where x == Case.Title select x).Count();
            int Upper = (from x in WordsCase where x == Case.Upper select x).Count();
            int Normal = (from x in WordsCase where x == Case.Normal select x).Count();
            int Lower = (from x in WordsCase where x == Case.Lower select x).Count();

            if (Titles > Normal && Titles > Upper && Titles > Lower)
                return Case.Title;
            if (Upper > Titles && Upper > Normal && Upper > Lower)
                return Case.Upper;
            if (Lower > Titles && Lower > Upper && Lower > Normal)
                return Case.Normal;

            return Case.Normal;
        }


        static int CountChars(string Str)
        {
            List<char> Chars = new List<char>();
            foreach (var chr in Str)
            {
                if (Chars.Contains(chr))
                    continue;
                Chars.Add(chr);
            }
            return Chars.Count;
        }

        /// <summary>
        /// Try get the encoding by name/id
        /// </summary>
        /// <param name="Name">Name/Id of the encoding</param>
        /// <returns>Result Encoding, Thrown if fails.</returns>
        static Encoding ParseEncodingName(string Name)
        {
            switch (Name.ToLower().Trim().Replace("-", ""))
            {
                case "default":
                    return Encoding.Default;

                case "japanese":
                case "shiftjis":
                case "sjis":
                    return Encoding.GetEncoding(932);

                case "utf8":
                    return Encoding.UTF8;

                case "unicode":
                case "utf16le":
                case "utf16":
                case "leutf16":
                    return Encoding.Unicode;

                case "beutf16":
                case "utf16be":
                    return Encoding.BigEndianUnicode;

                default:
                    int ID = 0;
                    if (int.TryParse(Name, out ID))
                    {
                        return Encoding.GetEncoding(ID);
                    }
                    return Encoding.GetEncoding(Name);
            }
        }

        /// <summary>
        /// Massive EndsWith Operation
        /// </summary>
        /// <param name="text">The String</param>
        /// <param name="v">The list of strings to match</param>
        /// <returns></returns>
        static bool EndsWithOr(string text, string v)
        {
            string[] letters = v.Split(',');
            foreach (string letter in letters)
                if (text.EndsWith(letter))
                    return true;
            return false;
        }

        /// <summary>
        /// Return true if the string contains a certain too many numbers
        /// </summary>
        /// <param name="text">The String</param>
        /// <param name="val">The Number Limit</param>
        /// <returns></returns>
        static bool NumberLimiter(string text, int val)
        {
            int min = '0', max = '9', total = 0;
            int asmin = '０', asmax = '９';
            foreach (char chr in text)
                if (chr >= min && chr <= max)
                    total++;
                else if (chr >= asmin && chr <= asmax)
                    total++;
            return total < val;
        }

        /// <summary>
        /// Massive Contains Operation
        /// </summary>
        /// <param name="text">The Text</param>
        /// <param name="MASK">List of substrings to match, in this format: Str1,Str2,Str3</param>
        /// <returns></returns>
        static bool ContainsOR(string text, string MASK)
        {
            string[] entries = MASK.Split(',');
            foreach (string entry in entries)
                if (text.Contains(entry))
                    return true;
            return false;
        }
    }
}