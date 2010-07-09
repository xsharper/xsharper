#region -- Copyrights --
// ***********************************************************************
//  This file is a part of XSharper (http://xsharper.com)
// 
//  Copyright (C) 2006 - 2010, Alexei Shamov, DeltaX Inc.
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// ************************************************************************
#endregion
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;


namespace XSharper.Core
{
    /// Transformation rules
    [Flags]
    public enum TransformRules
    {
        /// No expansion
        [Description("No expansion")]
        None = 0,
        
        /// Expand variables
        [Description("Expand ${variables}")]
        Expand = 1,
        
        /// Expand variables formatted as ${{var}}
        [Description("Expand variables formatted as ${{var}}")]
        ExpandDual = 2 | Expand,

        /// Expand variables formatted as [[var]]
        [Description("Expand variables formatted as [var]")]
        ExpandSquare = 4 | Expand,

        /// Expand variables formatted as [[var]]
        [Description("Expand variables formatted as [[var]]")]
        ExpandDualSquare = 8 | Expand,

        /// Do replacements only in expanded text
        [Description("Do replacements only in expanded text")]
        ExpandReplaceOnly = 16 | Expand,

        /// Trim expanded text only
        [Description("Trim expanded text only")]
        ExpandTrimOnly = 32 | Expand,

        /// Trim first, expand later
        [Description("Trim first, expand later")]
        ExpandAfterTrim=0x80 | Expand,

        /// Trim first, expand later
        [Description("Trim first, expand later")]
        TrimBeforeExpand=0x80 | Expand,

        /// Expand flags mask
        [Description("Expand flags mask")]
        ExpandMask=0xff,

        //////////////////////////////////////////

        /// Trim whitespace at the beginning of the string
        [Description("Trim whitespace at the beginning of the string")]
        TrimStart =         0x0000100,

        /// Trim whitespace at the end of the string
        [Description("Trim whitespace at the end of the string")]
        TrimEnd =           0x0000200,

        /// Trim whitespace at the beginning and at the end of the string
        [Description("Trim whitespace at the beginning and at the end of the string")]
        Trim =              TrimStart | TrimEnd,

        /// Trim internal whitespace
        [Description("Trim internal whitespace")]
        TrimInternal      = 0x0000400,

        /// Treat the string as multiline, and do processing of each line
        [Description("Treat the string as multiline, and do processing of each line")]
        Multiline          =0x0000800,

        /// All trim flags mask
        [Description("All trim flags mask")]
        TrimMask          = 0x0000f00,

        /// Replace ~ with space
        [Description("Replace ~ with space")]
        TildaToSpace =      0x0010000,

        /// Replace ` with "
        [Description("Replace ` with \"")]
        BackqToDouble =     0x0020000,

        /// Replace { with &lt; , and } with &gt;
        [Description("Replace { with < , and } with >")]
        CurvedToAngle =     0x0040000,
        
        /// Replace [ with &lt; , and ] with &gt;
        [Description("Replace [ with < , and ] with >")]
        SquareToAngle   =   0x0080000,

        /// Replace new lines with a single \n
        [Description("Replace new lines with a single LF")]
        NewLineToLF =       0x0100000,

        /// Replace new line with \r\n
        [Description("Replace new line with CR LF")]
        NewLineToCRLF     = 0x0200000,

        /// Convert ' to '' (useful for SQL)
        [Description("Convert ' to '' (useful for SQL)")]
        DoubleSingleQuotes= 0x0400000,

        /// Convert " to "" (useful for C#)
        [Description("Convert \" to \"\" (useful for C#)")]
        DoubleDoubleQuotes =0x0800000,

        /// Escape all XML special characters
        [Description("Escape all XML special characters")]
        EscapeXml =         0x1000000,

        /// If the string contains spaces, wrap it in double quotes. Internal double quotes are replaced with \"
        [Description("If the string contains spaces, wrap it in double quotes. Internal double quotes are replaced with \"")]
        QuoteArg          = 0x2000000,

        /// Escape all special regex characters
        [Description("Escape all special regex characters")]
        EscapeRegex       = 0x4000000,

        /// Replace all non-ASCII characters with . (dot)
        [Description("Replace all non-ASCII characters with . (dot)")]
        RemoveControl     = 0x8000000,

        /// All replace flags
        [Description("All replace flags")]
        ReplaceMask        =0xfff0000,

        /// Default transformation (same as Expand)
        [Description("Default transformation")]
        Default          = Expand
    }


    /// How to deal with backslash
    public enum BackslashOption
    {
        /// Leave as is
        [Description("Leave as is")]
        AsIs,

        /// Add backslash if not present
        [Description("Add backslash if not present")]
        Add,

        /// Remove backslash if not present
        [Description("Remove backslash if not present")]
        Remove
    }

    /// Where to put ellipsis
    public enum FitWidthOption
    {
        /// At the beginning of the string, like "automobile"=>"...bile"
        [Description("cut at beginning of the string 'automobile'=>'bile'")]
        CutStart,

        /// "cut at end of the string 'automobile'=>'auto'"
        [Description("cut at end of the string 'automobile'=>'auto'")]
        CutEnd,

        /// At the beginning of the string, like "automobile"=>"...bile"
        [Description("Ellipsis at the beginning of the string, like 'automobile'=>'...bile'")]
        EllipsisStart,

        /// At the end of the string, like "automobile"=>"auto..."
        [Description("At the end of the string, like 'automobile'=>'auto...'")]
        EllipsisEnd
    }

    public partial class Utils
    {
        /// Escape invalid XML chars
        public static string EscapeXml(string text)
        {
            return SecurityElement.Escape(text);
        }

        /// Lowercase the first letter of a string. "MonkeyBanana"=>"monkeyBanana"
        public static string LowercaseFirstLetter(string input)
        {
            input = input.Trim();
            if (input.Length > 0 && char.IsUpper(input[0]))
                return string.Concat(input.Substring(0, 1).ToLowerInvariant(), input.Substring(1));
            return input;
        }

        /// <summary>
        /// Convert an encoding type to encoding.
        ///
        /// For UTF7/8/16/32 a suffix may be specified as follows:
        ///  /nobom = "No Byte order marks"
        ///  /bom = "Add byte order marks"
        ///  /be-bom = "Big endian, add byte order marks" (UTF16 and UTF32 only)
        ///  /be-bom = "Big endian, no byte order order marks" (UTF16 and UTF32 only)
        ///
        /// For example, "utf-8/nobom"
        /// </summary>
        /// <param name="encoding">Encoding name</param>
        /// <returns>Encoding</returns>
        public static Encoding GetEncoding(string encoding)
        {
            if (encoding == null)
                return null;
            encoding = encoding.ToLower(System.Globalization.CultureInfo.InvariantCulture).Replace("utf8", "utf-8").Replace("utf16", "utf-16").Replace("utf32", "utf-32").Replace("utf7", "utf-7");
            switch (encoding.ToLowerInvariant())
            {
                case "utf-8/nobom": return new UTF8Encoding(false);
                case "utf-8/bom": return new UTF8Encoding(true);
                case "utf-7/nobom": return new UTF7Encoding(false);
                case "utf-7/bom": return new UTF7Encoding(true);
                case "utf-32/bom": return new UTF32Encoding(false, true);
                case "utf-32/nobom": return new UTF32Encoding(false, false);
                case "utf-32/be-bom": return new UTF32Encoding(true, true);
                case "utf-32/be-nobom": return new UnicodeEncoding(true, false);
                case "utf-16/bom": return new UnicodeEncoding(false, true);
                case "utf-16/nobom": return new UnicodeEncoding(false, false);
                case "utf-16/be-bom": return new UnicodeEncoding(true, true);
                case "utf-16/be-nobom": return new UnicodeEncoding(true, false);
                default: return Encoding.GetEncoding(encoding);
            }
        }


        /// Prefix each line of text with a given prefix
        public static string PrefixEachLine(string prefix, string text)
        {
            StringWriter sw = new StringWriter();
            bool w = true;
            foreach (char c in text)
            {
                if (c == '\r') continue;
                if (w)
                {
                    sw.Write(prefix);
                    w = false;
                }
                sw.Write(c);
                if (c == '\n')
                    w = true;
            }
            return sw.ToString();
        }

        /// If string ends with backslash, remove it
        public static string BackslashRemove(string ret)
        {
            return Backslash(ret, BackslashOption.Remove);
        }

        /// If string does not end with backslash, add it
        public static string BackslashAdd(string ret)
        {
            return Backslash(ret, BackslashOption.Add);
        }

        
        /// Get all values of a given enum, and lowercase their first letter (ala camelCase)
        public static string AllEnumValuesToString(Type t, string separator)
        {
            return string.Join(separator, Array.ConvertAll<string, string>(Enum.GetNames(t), LowercaseFirstLetter));
        }

        /// If string does or does not end with backslash, add or remove it
        public static string Backslash(string ret, BackslashOption backSlash)
        {
            ret = ret ?? string.Empty;
            switch (backSlash)
            {
                case BackslashOption.AsIs:
                    break;
                case BackslashOption.Add:
                    if (!ret.EndsWith("\\", StringComparison.Ordinal) && !ret.EndsWith("/", StringComparison.Ordinal))
                        ret += "\\";
                    break;
                case BackslashOption.Remove:
                    while (ret.EndsWith("\\", StringComparison.Ordinal) || ret.EndsWith("/", StringComparison.Ordinal))
                        ret = ret.Substring(0, ret.Length - 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("backSlash");
            }
            return ret;
        }

        /// Make sure that string length does not exceed a given number of characters, and if it does trim it and add ellipsis at the beginning or end
        public static string FitWidth(string s, int maxLen, FitWidthOption options)
        {
            if (s == null || s.Length < maxLen)
                return s;
            
            if (options == FitWidthOption.CutStart)
                return s.Substring(0, maxLen);
            if (options == FitWidthOption.CutEnd)
                return s.Substring(s.Length-maxLen);
            
            int len = Math.Max(maxLen - 3, 0);
            len = Math.Min(s.Length, len);

            if (options == FitWidthOption.EllipsisStart)
                return "..." + s.Substring(s.Length - len);
            return s.Substring(0, len) + "...";
        }

        /// Convert a wildcard using * and ? characters to a regular expression pattern
        public static string WildcardToPattern(string wildcard)
        {
            StringBuilder s = new StringBuilder(wildcard.Length);
            s.Append('^');
            foreach (char c in wildcard)
            {
                switch (c)
                {
                    case '*':
                        s.Append(".*?");
                        break;
                    case '?':
                        s.Append('.');
                        break;
                    default:
                        s.Append(Regex.Escape(c.ToString()));
                        break;
                }
            }
            s.Append('$');
            return s.ToString();
        }

        /// Convert a wildcard using * and ? characters to a regular expression
        public static Regex WildcardToRegex(string wildcard)
        {
            return new Regex(WildcardToPattern(wildcard));
        }

        /// Convert a wildcard using * and ? characters to a regular expression
        public static Regex WildcardToRegex(string wildcard, RegexOptions options)
        {
            return new Regex(WildcardToPattern(wildcard), options);
        }


        /// Transform string according to the specified rules
        public static string TransformStr(string arguments, TransformRules trim)
        {
            if (arguments == null)
                return String.Empty;
            if ((trim & TransformRules.Expand) != 0)
                throw new ArgumentOutOfRangeException("trim", "Expand flag cannot be specified");
            string ret;
            if ((trim & TransformRules.Multiline) == TransformRules.Multiline)
            {
                string str = TransformStr(arguments, trim & TransformRules.Trim);
                StringReader reader = new StringReader(str);
                List<string> list = new List<string>();
                while ((str = reader.ReadLine()) != null)
                {
                    list.Add(TransformStr(str, trim & ~TransformRules.Multiline));
                }
                ret = String.Join(Environment.NewLine, list.ToArray());

                return ret;
            }

            TransformRules tr = trim & TransformRules.Trim;
            switch (tr)
            {
                default:
                    ret = arguments;
                    break;

                case TransformRules.Trim:
                    ret = arguments.Trim();
                    break;

                case TransformRules.TrimEnd:
                    ret = arguments.TrimEnd();
                    break;

                case TransformRules.TrimStart:
                    ret = arguments.TrimStart();
                    break;
            }

            if ((trim & TransformRules.TrimInternal) != 0)
                ret = Regex.Replace(ret, @"\s{2,}", " ");

            if ((trim & TransformRules.TildaToSpace) == TransformRules.TildaToSpace)
                ret = ret.Replace("~", " ");
            if ((trim & TransformRules.BackqToDouble) == TransformRules.BackqToDouble)
                ret = ret.Replace("`", "\"");
            if ((trim & TransformRules.SquareToAngle) == TransformRules.SquareToAngle)
                ret = ret.Replace("[", "<").Replace("]", ">");
            if ((trim & TransformRules.CurvedToAngle) == TransformRules.CurvedToAngle)
                ret = ret.Replace("{", "<").Replace("}", ">");
            if ((trim & TransformRules.NewLineToLF) == TransformRules.NewLineToLF)
                ret = ret.Replace("\r\n", "\n").Replace("\r", "\n");
            if ((trim & TransformRules.NewLineToCRLF) == TransformRules.NewLineToCRLF)
                ret = ret.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
            if ((trim & TransformRules.DoubleSingleQuotes) == TransformRules.DoubleSingleQuotes)
                ret = ret.Replace("'", "''");
            if ((trim & TransformRules.DoubleDoubleQuotes) == TransformRules.DoubleDoubleQuotes)
                ret = ret.Replace("\"", "\"\"");
            if ((trim & TransformRules.QuoteArg) == TransformRules.QuoteArg)
                ret = QuoteArg(ret);
            if ((trim & TransformRules.EscapeXml) == TransformRules.EscapeXml)
                ret = SecurityElement.Escape(ret);
            if ((trim & TransformRules.EscapeRegex) == TransformRules.EscapeRegex)
                ret = Regex.Escape(ret);

            if ((trim & TransformRules.RemoveControl) == TransformRules.RemoveControl)
            {
                char[] ch = ret.ToCharArray();
                for (int i = 0; i < ch.Length; ++i)
                    if (char.IsControl(ch[i]))
                        ch[i] = (char)127;
                ret = new string(ch);
            }

            return ret;

        }
        private static readonly char[] _charsToEscape = " \t\r\n\"><|&()[]{}^=;!'+,`~%".ToCharArray();

        /// If string contains spaces or double quotes, wrap the string in quotes and replace quotes with \"
        public static string QuoteArg(string arg)
        {
            arg = arg ?? String.Empty;
            if (arg.Length == 0 || arg.IndexOfAny(_charsToEscape) != -1)
                return "\"" + arg.Replace("\"", "\\\"") + "\"";
            return arg;
        }

        /// Build MS-DOS like command line, wrapping arguments in quotes where needed and separating them with space
        public static string QuoteArgs(string[] args)
        {
            StringBuilder sb = new StringBuilder();
            if (args != null)
                foreach (var s in args)
                {
                    if (sb.Length != 0)
                        sb.Append(" ");
                    sb.Append(TransformStr(s, TransformRules.QuoteArg));
                }
            return sb.ToString();
        }

        /// Parse command line into the list of arguments. Mimics .NET command line parsing logic where possible
        public static string[] SplitArgs(string commandLine)
        {
            if (commandLine==null)
                return null;
            bool quoted = false;
            List<string> args = new List<string>();
            StringBuilder sb = new StringBuilder();
            using (var reader = new ParsingReader(new StringReader(commandLine)))
            {
                int n;
                do
                {
                    n = reader.Read();
                    if (n == '"')
                    {
                        quoted = true;
                        while (!reader.IsEOF)
                        {
                            var ch = (char)reader.Read();
                            if (ch == '"')
                                break;
                            sb.Append(ch);
                        }
                    }
                    else if (n == -1 || char.IsWhiteSpace((char)n))
                    {
                        if (sb.Length > 0 || quoted)
                            args.Add(sb.ToString());
                        sb.Length = 0;
                        quoted = false;
                    }
                    else
                        sb.Append((char)n);
                } while (n != -1);
            }
            return args.ToArray();
        }

        /// Display text in two columns, wrapping the second column so the total width does not exceed totalWidth
        public static string WrapTwoColumns(IEnumerable<Var> rows, int col1MaxWidth, int totalWidth)
        {
            using (var sw = new StringWriter())
            {
                WrapTwoColumns(sw,rows, col1MaxWidth, totalWidth);
                return sw.ToString();
            }
        }

        /// Display text in two columns, wrapping the second column so the total width does not exceed totalWidth
        public static void WrapTwoColumns(TextWriter sw, IEnumerable<Var> rows, int col1MaxWidth, int totalWidth)
        {
            int w = 0;
            foreach (Var v in rows)
                w = Math.Max(w, v.Name.Length);
            w += 4;

            foreach (Var n in rows)
            {
                string ind = new string(' ', w);
                string vs = Utils.To<string>(n.Value);
                if (n.Name.Length == 0)
                    Wrap(sw, vs, totalWidth, string.Empty);
                else
                {
                    int scolw = (totalWidth - w);
                    if (scolw >= col1MaxWidth)
                    {
                        sw.Write((n.Name + ind).Substring(0, w));
                        Wrap(sw, vs, scolw, ind);
                    }
                    else
                    {
                        Wrap(sw, n.Name, totalWidth, "  ");
                        Wrap(sw, "   " + n.Value, totalWidth - 4, "    ");
                    }
                }
            }
        }

        /// Wrapping text so the total width does not exceed totalWidth. Add indent specified to second and subsequent lines
        public static string Wrap(string text, int width, string indent)
        {
            using (var s = new StringWriter())
            {
                Wrap(s, text, width, indent);
                return s.ToString();
            }
        }

        /// Secure URI by removing password if any
        public static Uri SecureUri(Uri uri)
        {
            UriBuilder ub = new UriBuilder(uri);
            if (ub.Password!=null)
                ub.Password = null;
            return ub.Uri;
        }
        /// Secure URI by removing password if any
        public static Uri SecureUri(string uri)
        {
            UriBuilder ub = new UriBuilder(uri);
            if (ub.Password != null)
                ub.Password = null;
            return ub.Uri;
        }
        /// Fix filename, by replacing all invalid characters with _
        public static string FixFilename(string filename)
        {
            filename = filename ?? string.Empty;
            char[] basenamech = filename.ToCharArray();
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < basenamech.Length; ++i)
                for (int j = 0; j < invalid.Length; ++j)
                    if (basenamech[i] == invalid[j] || basenamech[i] == ' ')
                        basenamech[i] = '_';
            if (basenamech.Length == 0)
                return "_";
            return new string(basenamech);
        }
        /// Wrapping text so the total width does not exceed totalWidth. Add indent specified to second and subsequent lines
        public static void Wrap(TextWriter sw, string text, int width, string indent)
        {
            text = text ?? string.Empty;
            string[] lines = text.Replace("\r", "").Split('\n');
            bool first = true;
            foreach (string l in lines)
            {
                if (l.Length == 0)
                {
                    sw.WriteLine();
                    continue;
                }
                string[] words = l.Split(' ');
                StringBuilder cl = new StringBuilder();
                bool nonSpaceWordFound = false;
                foreach (string w in words)
                {
                    if (cl.Length + 4 + w.Length >= width)
                    {
                        if (!first)
                            sw.Write(indent);
                        first = false;
                        sw.WriteLine(cl);
                        cl.Length = 0;
                    }
                    if (w.Length > 0)
                        nonSpaceWordFound = true;
                    if (cl.Length > 0 || !nonSpaceWordFound)
                        cl.Append(" ");

                    cl.Append(w);


                }

                if (cl.Length > 0)
                {
                    if (!first)
                        sw.Write(indent);
                    first = false;
                    sw.WriteLine(cl);
                }
            }
        }

    }
}