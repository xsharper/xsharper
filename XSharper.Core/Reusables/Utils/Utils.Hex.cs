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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;


namespace XSharper.Core
{
    /// Utilities
    public partial class Utils
    {
        private static readonly string s_hex = "0123456789abcdef";

        /// Convert binary data to a hex string
        public static string ToHex(byte[] data)
        {
            if (data==null)
                return null;
            char[] ch=new char[data.Length*2];
            int ptr = 0;
            foreach (var b in data)
            {
                ch[ptr++] = s_hex[b >> 4 & 0x0f];
                ch[ptr++] = s_hex[b & 0x0f];
            }
            return new string(ch);
        }

        /// Convert hex string to binary data. <see cref="ToHex"/> for reverse.
        public static byte[] ToBytes(string str)
        {
            return ToBytes(new StringReader(str));
        }

        /// Convert hex string to binary data. <see cref="ToHex"/> for reverse.
        public static byte[] ToBytes(TextReader tr)
        {
            if (tr==null)
                return null;
            MemoryStream ms=new MemoryStream();
            int n;
            int next = -1;
            while ((n=tr.Read())!=-1)
            {
                char c = (char) n;
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
                    continue;

                int cur;
                if (char.IsDigit(c))
                    cur = c - '0';
                else
                {
                    cur = (char.ToLowerInvariant(c)) - 'a';
                    if (cur<0 || cur>5)
                        throw new ParsingException("Unexpected character '"+c+"' found");
                    cur += 10;
                }
                if (next==-1)
                    next = cur<<4;
                else
                {
                    ms.WriteByte((byte)(next|cur));
                    next = -1;
                }
            }
            if (next!=-1)
                throw new EndOfStreamException("Data truncated");
            return ms.ToArray();
        }


        /// Convert binary data to a hex dump, adding a new line after every 16 bytes
        public static string ToHexDump(byte[] data)
        {
            return ToHexDump(data, 16, null, false);
        }

        /// Convert binary data to a hex dump, adding a new line after every bytesPerRow bytes
        public static string ToHexDump(byte[] data, int bytesPerRow)
        {
            return ToHexDump(data, bytesPerRow, null, false);
        }

        /// <summary>
        /// Convert binary data to a hex dump, adding a new line after every bytesPerRow bytes
        /// </summary>
        /// <param name="data">Binary data</param>
        /// <param name="bytesPerRow">Number of bytes per line</param>
        /// <param name="offset">Offset of the first byte, or null in order not to display any offset</param>
        /// <param name="withChars">true, if characters must be displayed</param>
        /// <returns>Formatted string</returns>
        public static string ToHexDump(byte[] data, int bytesPerRow, long? offset, bool withChars)
        {
            StringBuilder sb = new StringBuilder();

            for (int off = 0; off < data.Length; off += bytesPerRow)
            {
                int bytesLeft = Math.Min(data.Length - off, bytesPerRow);
                if (bytesLeft == 0)
                    break;
                if (offset.HasValue)
                    sb.AppendFormat("{0:X8}  ", off+offset.Value);
                for (int i = 0; i < bytesPerRow; ++i)
                    if (i < bytesLeft)
                        sb.AppendFormat("{0:X2} ", data[off + i]);
                    else
                        sb.Append("   ");
                if (withChars)
                {
                    sb.Append(" ");
                    for (int i = 0; i < bytesPerRow; ++i)
                        if (i < bytesLeft)
                            sb.Append((data[off + i] < 32) ? '.' : (char)data[off + i]);
                        else
                            sb.Append(' ');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}