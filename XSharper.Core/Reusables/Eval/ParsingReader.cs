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
using System.IO;
using System.Text;

namespace XSharper.Core
{
    /// <summary>
    /// Special simplified text reader, that can read string char by char similarly to StringReader, 
    /// but also can put a char to the queue to be read by the next Read()
    /// </summary>
    public class ParsingReader : TextReader
    {
        private char? _top;
        private TextReader _r;
        private bool _owns;
        private readonly char[] _history = new char[40];
        private int _ptr = 0;
        private int _line = 1;
        
        /// Current line number
        public int LineNumber
        {
            get { return _line; }
        }
        /// Stream history
        public string History
        {
            get { 
                string s=new string(_history).Trim('\x0'); 
                if (s.Length!=_history.Length)
                    return s;
                return "... "+s.Substring(_ptr) + s.Substring(0, _ptr);
            }
        }

        /// Constructor
        public ParsingReader(TextReader s) : this(s,true)
        {
        }

        /// Constructor
        public ParsingReader(string s)
            : this(new StringReader(s), true)
        {
        }

        /// Constructor
        public ParsingReader(TextReader s, bool ownsStream) 
        { 
            _r = s;
            _owns = ownsStream;
        }

        /// <summary> Releases the unmanaged resources used by the <see cref="T:System.IO.TextReader"/> and optionally releases the managed resources. </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _owns)
            {
                _r.Dispose();
                _r = null;
                _owns = false;
            }
           base.Dispose(disposing);
        }
            
        
        /// <summary>
        /// Returns true if end of stream is reached
        /// </summary>
        public bool IsEOF
        {
            get
            {
                return !_top.HasValue && (_r == null || _r.Peek() == -1);
            }
        }

        /// Returns true if character can be poked
        public bool CanPoke
        {
            get { return !_top.HasValue; }
        }

        /// <summary>
        /// Peek the next char
        /// </summary>
        /// <returns>next character or -1 if EOF</returns>
        public override int Peek()
        {
            if (_r==null)
                throw new ObjectDisposedException(GetType().FullName);
            if (_top.HasValue)
                return _top.Value;
            return _r.Peek();
        }
        /// <summary>
        /// Put the character on top of the stream, to be read with a subsequent 'Peek' or 'Read'.
        /// Only one character may be poked to the stream at any moment of time.
        /// </summary>
        /// <param name="ch">Character to poke</param>
        public void Poke(char ch)
        {
            if (_top.HasValue)
                throw new InvalidOperationException("Cannot poke");
            _top = ch;
        }

        /// <summary>
        /// Read the next char
        /// </summary>
        /// <returns>next character or -1 if EOF</returns>
        public override int Read()
        {
            if (_r == null)
                throw new ObjectDisposedException(GetType().FullName);
            int ret;
            if (_top.HasValue)
            {
                ret = _top.Value;
                _top = null;
            }
            else
            {
                ret = _r.Read();
                if (ret != -1)
                {
                    if (ret == '\n')
                        _line++;
                    _history[_ptr] = (char) ret;
                    _ptr = (_ptr + 1)%_history.Length;
                }
            }
            return ret;
        }

        /// <summary>
        /// Reads a line of characters from the current stream and returns the data as a string.
        /// </summary>
        /// <returns>
        /// The next line from the input stream, or null if all characters have been read.
        /// </returns>
        public override string ReadLine()
        {
            if (_r==null)
                throw new ObjectDisposedException(GetType().FullName);
            if (_top == null)
                return _r.ReadLine();
            string s=_top.Value + _r.ReadLine();
            _top = null;
            return s;
        }

        /// <summary>
        /// Reads all characters from the current position to the end of the TextReader and returns them as one string.
        /// </summary>
        /// <returns>
        /// A string containing all characters from the current position to the end of the TextReader.
        /// </returns>
        public override string ReadToEnd()
        {
            if (_r == null)
                throw new ObjectDisposedException(GetType().FullName);
            if (_top == null)
                return _r.ReadToEnd();
            string s = _top.Value + _r.ReadToEnd();
            _top = null;
            return s;
        }
        ///<summary>Skip until first non-whitespace character in the stream</summary>
        public void SkipWhiteSpace()
        {
            while (!IsEOF && char.IsWhiteSpace((char) Peek()))
                Read();
        }

        /// Read a number value from the stream. Value may be hex (with 0x prefix) or decimal, and may have suffixes such as 'm' or 'd'
        public ValueType ReadNumber()
        {
            StringBuilder sb = new StringBuilder();
            int n;
            bool firstDigit = false;
            bool prevDigit = false;
            bool dot = false;
            bool hex = false;
            SkipWhiteSpace();
            Type convType = null;
            while ((n = Peek()) != -1)
            {
                char ch = char.ToLowerInvariant(((char)n));
                if (!firstDigit)
                {
                    if (!char.IsDigit(ch) && ch != '-' && ch != '+' && ch != '.')
                        break;
                    prevDigit = firstDigit = char.IsDigit(ch);
                    sb.Append(ch);
                    Read();
                    continue;
                }
                if (ch == 'x')
                {
                    if (sb.ToString() != "0")
                        break;
                    hex = true;
                }
                if (ch == 'e' && !prevDigit && !hex)
                    break;
				if (ch == '.')
                {
                    if (dot || hex)
                        break;
                    dot = true;
                }

                prevDigit = char.IsDigit(ch);
                
                if (prevDigit || ch == '.' || ch == 'x' || ch == 'e' || (hex && ch >= 'a' && ch <= 'f'))
                {
                    Read();
                    sb.Append(ch);
                    if (ch=='e' && (Peek()=='-' || Peek()=='+'))
                    {
                        sb.Append((char)Read());
                    }

                    continue;
                }
                
                switch (ch)
                {
                    case 'm':
                        if (hex) break; //m suffix not allowed for hex
                        Read();
                        return decimal.Parse(sb.ToString());
                    case 'u':
                        Read();
                        if (((char)Peek()) == 'l')
                        {
                            Read();
                            convType = typeof(ulong);
                            if (hex) break;
                            return ulong.Parse(sb.ToString());
                        }
                        convType = typeof(uint);
                        if (hex) break;
                        return uint.Parse(sb.ToString());
                    case 'l':
                        Read();
                        convType = typeof(long);
                        if (hex) break;
                        return long.Parse(sb.ToString());
                    case 'f':
                        Read(); //f suffix not allowed for hex, but f is a legit hex character
                        return float.Parse(sb.ToString());
                    case 'd':
                        Read(); //f suffix not allowed for hex, but d is a legit hex character
                        return double.Parse(sb.ToString());
                }
                break;
            }
            if (sb.Length > 0 && sb[sb.Length - 1] == '.')
            {
                sb.Length--;
                Poke('.');
            }
            string s = sb.ToString().ToLowerInvariant();

            if (string.IsNullOrEmpty(s))
                return null;
            
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                ulong u = Convert.ToUInt64(s, 16);
                if (convType != null)
                    return (ValueType)Convert.ChangeType(u, convType);
                if (u <= int.MaxValue)
                    return (int)u;
                if (u <= uint.MaxValue)
                    return (uint)u;
            }
            long x;
            if (long.TryParse(s, out x))
            {
                if (x <= int.MaxValue && x >= int.MinValue)
                    return (int)x;
                return x;
            }
            double d;
            if (double.TryParse(s, out d))
                return d;
            return null;
        }

        /// Read first character from stream, and then read until the same character is read again.
        public string ReadQuote()
        {
            if (IsEOF)
                return null;

            char end = (char)Read();
            var sb = new StringBuilder();
            while (!IsEOF)
            {
                var ch = (char)Read();
                if (ch == end)
                {
                    if (Peek() == end)
                        Read();
                    else
                        return sb.ToString();
                }
                sb.Append(ch);
            }
            ThrowParsingException("Closing quote '" + end + "' not found");
            return null;
        }

        /// <summary>
        /// Read while a condition is false
        /// </summary>
        /// <param name="predicate">Condition</param>
        /// <returns>Characters read</returns>
        public string ReadUntil(Predicate<char> predicate)
        {
            StringBuilder sb = new StringBuilder();
            int n;
            while ((n = Peek()) != -1 && !predicate((char)n))
            {
                Read();
                sb.Append((char)n);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Read while a condition is true
        /// </summary>
        /// <param name="predicate">Condition</param>
        /// <returns>Characters read</returns>
        public string ReadWhile(Predicate<char> predicate)
        {
            StringBuilder sb = new StringBuilder();
            int n;
            while ((n = Peek()) != -1 && predicate((char)n))
            {
                Read();
                sb.Append((char)n);
            }
            return sb.ToString();
        }


        /// <summary>
        /// Read until character (not reading the actual character)
        /// </summary>
        /// <param name="c">Stop character</param>
        /// <returns>Characters read</returns>
        public string ReadUntil(char c)
        {
            StringBuilder sb=new StringBuilder();
            int n;
            while ((n = Peek()) != -1 && n != c)
            {
                Read();
                sb.Append((char) n);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Throw parsing exception with a given message
        /// </summary>
        public void ThrowParsingException(string message)
        {
            if (string.IsNullOrEmpty(History))
                throw new ParsingException(message);
            throw new ParsingException(message + " in expression '" + History + "'");
        }

        /// <summary>
        /// Read the next character, and if it does not match the given character, throw parsing exception
        /// </summary>
        public int ReadAndThrowIfNot(int expected)
        {
            int found = Read();
            if (found != expected)
            {
                var sb =new StringBuilder("Invalid expression. Character " + formatChar(found) + " found instead of " +
                                      formatChar(expected));
                ThrowParsingException(sb.ToString());
            }
            return found;
        }

        /// True if str is a number
        public static bool IsNumber(string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            using (var sr = new ParsingReader(str))
            {
                if (sr.ReadNumber() == null)
                    return false;
                sr.SkipWhiteSpace();
                if (sr.Peek() != -1)
                    return false;
            }
            return true;
        }

        /// Parse string to a number. 10 and hex numbers (like 0x222) are allowed. Suffixes like 20.3f are also allowed.
        public static ValueType ParseNumber(string str) 
        {
            if (str == null) throw new ArgumentNullException("str");
            using (var sr = new ParsingReader(str))
            {
                sr.SkipWhiteSpace();
                ValueType o = sr.ReadNumber();
                if (o == null)
                    throw new ParsingException("Invalid numeric expression at " + sr.ReadLine());
                sr.SkipWhiteSpace();
                if (sr.Peek() != -1)
                    throw new ParsingException("Invalid numeric expression, unexpected characters at " + sr.ReadLine());
                return o;
            }
        }

        /// Parse string to a number. 10 and hex numbers (like 0x222) are allowed. Suffixes like 20.3f are also allowed.
        public static T ParseNumber<T>(string str) where T : struct
        {
            return (T)Convert.ChangeType(ParseNumber(str), typeof (T));
        }

        /// Returns null if str is not a number, or its value otherwise
        public static ValueType TryParseNumber(string stringToParse)
        {
            if (stringToParse == null) 
                return null;
            using (var sr = new ParsingReader(stringToParse))
            {
                sr.SkipWhiteSpace();
                ValueType o = sr.ReadNumber();
                if (o == null)
                    return null;
                sr.SkipWhiteSpace();
                if (sr.Peek() != -1)
                    return null;
                return o;
            }
        }
        
        /// Parse string to a number. 10 and hex numbers (like 0x222) are allowed. Suffixes like 20.3f are also allowed.
        public static T? TryParseNumber<T>(string str) where T:struct
        {
            var z = TryParseNumber(str);
            if (z==null)
                return null;
            return (T)Convert.ChangeType(z, typeof (T));
        }

        private static string formatChar(int n)
        {
            return n == -1 ? "'EOF'" : "'" + (char)n + "'";
        }


    }
}       