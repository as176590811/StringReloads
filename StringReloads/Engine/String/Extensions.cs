﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using StringReloads.Engine;
using StringReloads.Engine.String;

namespace StringReloads
{
    public static class Extensions
    {
        public static bool ToBoolean(this string Value)
        {
            if (Value == null)
                return false;

            Value = Value.ToLowerInvariant();
            switch (Value)
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                case "enable":
                case "enabled":
                    return true;
            }
            return false;
        }

        internal static Log.LogLevel ToLogLevel(this string ValueStr)
        {
            switch (ValueStr.ToLowerInvariant())
            {
                case "t":
                case "tra":
                case "trc":
                case "trace":
                    return Log.LogLevel.Trace;
                case "d":
                case "deb":
                case "dbg":
                case "debug":
                    return Log.LogLevel.Debug;
                case "i":
                case "inf":
                case "info":
                case "information":
                    return Log.LogLevel.Information;
                case "w":
                case "war":
                case "warn":
                case "warning":
                    return Log.LogLevel.Warning;
                case "e":
                case "err":
                case "erro":
                case "error":
                    return Log.LogLevel.Error;
                case "c":
                case "cri":
                case "crit":
                case "critical":
                    return Log.LogLevel.Critical;
            }

            return (Log.LogLevel)ValueStr.ToInt32();
        }

        public static uint ToUInt32(this string Value)
        {
            if (Value == null)
                return 0;

            if (Value.StartsWith("0x") && uint.TryParse(Value.Substring(2), NumberStyles.HexNumber, null, out uint Val))
                return Val;

            if (uint.TryParse(Value, out Val))
                return Val;

            return 0;
        }

        public static int ToInt32(this string Value)
        {
            if (Value == null)
                return 0;

            if (Value.StartsWith("0x") && int.TryParse(Value.Substring(2), NumberStyles.HexNumber, null, out int Val))
                return Val;

            if (int.TryParse(Value, out Val))
                return Val;

            return 0;
        }

        public static ulong ToUInt64(this string Value)
        {
            if (Value == null)
                return 0;

            if (Value.StartsWith("0x") && ulong.TryParse(Value.Substring(2), NumberStyles.HexNumber, null, out ulong Val))
                return Val;

            if (ulong.TryParse(Value, out Val))
                return Val;

            return 0;
        }

        public static long ToInt64(this string Value)
        {
            if (Value == null)
                return 0;

            if (Value.StartsWith("0x") && long.TryParse(Value.Substring(2), NumberStyles.HexNumber, null, out long Val))
                return Val;

            if (long.TryParse(Value, out Val))
                return Val;

            return 0;
        }

        public static byte[] GetTermination(this Encoding Encoding)
        {
            if (EntryPoint.SRL != null)
            {
                var Customs = (from x in EntryPoint.SRL.Encodings where x.Encoding.EncodingName == Encoding.EncodingName select x);
                if (Customs.Any())
                {
                    var Termination = Customs.First().Termination;
                    if (Termination != null && Termination.Length != 0)
                        return Termination;
                }
            }

            return Encoding.GetBytes("\x0");
        }

        public static Encoding ToEncoding(this string Value)
        {
            if (EntryPoint.SRL != null)
            {
                var Customs = (from x in EntryPoint.SRL.Encodings where x.Name.ToLowerInvariant() == Value.ToLowerInvariant() select x);
                if (Customs.Any())
                    return Customs.First().Encoding;
            }

            if (int.TryParse(Value, out int CP))
                return Encoding.GetEncoding(CP);

            return Value.ToLowerInvariant() switch
            {
                "sjis" => Encoding.GetEncoding(932),
                "shiftjis" => Encoding.GetEncoding(932),
                "shift-jis" => Encoding.GetEncoding(932),
                "unicode" => Encoding.Unicode,
                "utf16" => Encoding.Unicode,
                "utf16be" => Encoding.BigEndianUnicode,
                "utf16wb" => new UnicodeEncoding(false, true),
                "utf16wbbe" => new UnicodeEncoding(true, true),
                "utf16bewb" => new UnicodeEncoding(true, true),
                "utf8" => Encoding.UTF8,
                "utf8wb" => new UTF8Encoding(true),
                "utf7" => Encoding.UTF7,
                _ => Encoding.GetEncoding(Value)
            };
        }

        public static string GetFilename(this string Path) => System.IO.Path.GetFileName(Path);
        public static string GetFilenameNoExt(this string Path) => System.IO.Path.GetFileNameWithoutExtension(Path);

        public static string GetStartTrimmed(this string Str)
        {
            int Len = Str.Length - Str.TrimStart().Length;
            return Str.Substring(0, Len);
        }
        public static string GetEndTrimmed(this string Str)
        {
            int Len = Str.TrimEnd().Length;
            return Str.Substring(Len);
        }


        public static dynamic Evalaute(this string Expression) => Expression.Evalaute(null);
        public static dynamic Evalaute(this string Expression, string Key, object Value) => Expression.Evalaute(new[] { Key }, new[] { Value });
        public static dynamic Evalaute(this string Expression, IEnumerable<string> Keys, IEnumerable<object> Values)
        {
            if (Keys.Count() != Values.Count())
                throw new InvalidOperationException("The Keys and Values needs to have the same amount of items");
            Dictionary<string, object> Items = new Dictionary<string, object>();
            for (int i = 0; i < Keys.Count(); i++)
                Items.Add(Keys.ElementAt(i), Values.ElementAt(i));
                return Expression.Evalaute(Items);
        }
        public static dynamic Evalaute(this string Expression, Dictionary<string, object> Paramters)
        {
            var Exp = new NCalc.Expression(Expression);
            if (Paramters != null)
                Exp.Parameters = Paramters;
            try
            {
                return Exp.Evaluate();
            }
            catch
            {
                Log.Error("Expression Failed: " + Expression + "\nParameters: " + string.Join("; ", Paramters.Keys));
                throw;
            }
        }

        public static string[] DenyList = Config.Default.Filter.DenyList.Unescape().Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
        public static string[] IgnoreList = Config.Default.Filter.IgnoreList.Unescape().Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
        public static Quote[] Quotes = Config.Default.Filter.QuoteList.Unescape().Split('\n')
                    .Where(x => x.Length == 2)
                    .Select(x => new Quote() { Start = x[0], End = x[1] }).ToArray();
        public static bool IsDialogue(this string String, int? Caution = null, bool UseAcceptableRange = true, bool? UseDB = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(String))
                    return false;

                if ((UseDB ?? Config.Default.Filter.UseDB) && EntryPoint.SRL.HasMatch(String))
                    return true;


                 string Str = String.Trim();
                Str = Str.Replace(Config.Default.BreakLine, "\n");

                foreach (string Ignore in IgnoreList)
                    Str = Str.Replace(Ignore, "");

                foreach (string Deny in DenyList)
                {
                    if (Str.ToLower().Contains(Deny.ToLower()))
                        return false;
                }

                if (string.IsNullOrWhiteSpace(Str))
                    return false;

                if (UseAcceptableRange && CharacterRanges.TotalMissmatch(Str, Config.Default.Filter.AcceptableRange) > 0)
                    return false;

                string[] ScriptPatterns = new string[] { "!=", "<=", ">=", "==", "+=", "-=", "->", "//", ");", "*-", "null", "&&", "||" };

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


                bool IsCaps = Str.ToUpper() == Str;
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
                foreach (Quote Quote in Quotes)
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
                    char Last = (LineQuotes == null ? Str.Last() : Str.TrimEnd(LineQuotes?.End ?? ' ').Last());
                    if (IsJap && PontuationJapList.Contains(Last))
                        Points -= 3;

                    if (!IsJap && (PontuationList).Contains(Last))
                        Points -= 3;

                }
                catch { }
                try
                {
                    char First = (LineQuotes == null ? Str.First() : Str.TrimEnd(LineQuotes?.Start ?? ' ').First());
                    if (IsJap && PontuationJapList.Contains(First))
                        Points -= 3;

                    if (!IsJap && (PontuationList).Contains(First))
                        Points -= 3;

                }
                catch { }

                if (!IsJap)
                {
                    int NumberOnly = 0;
                    int LetterOnly = 0;
                    foreach (string Word in Words)
                    {
                        int WNumbers = Word.Where(c => char.IsNumber(c)).Count();
                        int WLetters = Word.Where(c => char.IsLetter(c)).Count();
                        int WNumSpecials = Word.Where(c => c == ',' || c == '.').Count();

                        WLetters -= WNumSpecials;

                        if (WLetters > 0 && WNumbers > 0)
                        {
                            Points += 2;
                        }

                        if (WLetters <= 0 && WNumbers > 0)
                            NumberOnly++;
                        if (WNumbers <= 0 && WLetters > 0)
                            LetterOnly++;

                        if (Word.Trim(PontuationList).Where(c => PontuationList.Contains(c)).Count() != 0)
                        {
                            Points += 2;
                        }
                    }

                    if (NumberOnly > LetterOnly)
                        Points += NumberOnly > LetterOnly * 2 ? 2 : 1;
                }

                if (string.IsNullOrWhiteSpace(Str.Trim().Trim(Str.First())))
                    Points = 3;//Discard pontuation checks

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

                foreach (var Pattern in ScriptPatterns)
                {
                    if (Str.ToLowerInvariant().Replace(" ", "").Contains(Pattern))
                        Points += 2;
                }

                //Detect dots followed of a non space character
                if (!IsJap && Str.Trim().TrimEnd('.').Contains("."))
                {
                    var Count = Str.Trim().TrimEnd('.').Split('.').Skip(1).Count(x => !string.IsNullOrEmpty(x) && !char.IsWhiteSpace(x.FirstOrDefault()));
                    if (Count > 0)
                        Points++;
                }

                if (!IsJap && WordCount == 1 && char.IsUpper(Str.First()) && !char.IsPunctuation(Str.TrimEnd().Last()))
                    Points++;

                if (!IsJap && WordCount == 1 && !char.IsUpper(Str.First()))
                    Points++;

                if (Words.Where(x => x.Skip(1).Where(y => char.IsUpper(y)).Count() > 1
                                  && x.Where(y => char.IsLower(y)).Count() > 1).Any())
                    Points++;

                if (!IsJap && char.IsUpper(Str.TrimStart().First()) && char.IsPunctuation(Str.TrimEnd().Last()))
                    Points--;

                if (!char.IsPunctuation(Str.TrimEnd().Last()))
                    Points++;

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


                if (IsJap != Config.Default.Filter.FromAsian)
                    return false;

                bool Result = Points < (Caution ?? Config.Default.Filter.Sensitivity);
                return Result;
            }
            catch (Exception ex)
            {
#if DEBUG
                throw ex;
#else
                return false;
#endif
            }
        }

        internal static string Escape(this string String) => StringModifier.Escape.Default.Apply(String, null);
        internal static string Unescape(this string String) => StringModifier.Escape.Default.Restore(String);

        internal static double PercentOf(this string String, int Value)
        {
            var Result = Value / (double)String.Length;
            return Result * 100;
        }
    }
}
