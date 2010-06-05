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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Security.Cryptography;
using System.Data;


namespace XSharper.Core
{
    /// Special handler for script in xsharper section of the configuration file
    public class ScriptSectionHandler : IConfigurationSectionHandler
    {
        #region IConfigurationSectionHandler Members

        /// Standard callback
        public object Create(object parent, object configContext, XmlNode section)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter xw = XmlWriter.Create(ms))
                    section.WriteTo(xw);
                ms.Position = 0;
                return ms.ToArray();
            }
        }

        #endregion
    }

    public partial class Utils
    {
        /// Find a stream with the specified name in the given assembly and return it, or null if not found. Automatically decompresses if there is a stream with a matching name and .gz suffix
        ///<param name="resourceAssembly">Assembly where to search for streams</param>
        ///<param name="streamName">Stream to find (case insensitive)</param>
        ///<returns>MemoryStream with data or null if not found </returns>
        public static MemoryStream FindResourceMemoryStream(Assembly resourceAssembly, string streamName)
        {
            using (var str = FindResourceStream(resourceAssembly, streamName))
            {
                if (str == null)
                    return null;
                return new MemoryStream(ReadBytes(str));
            }
        }

        ///<summary>Open a stream with a given name in the specified assembly, and return the stream. Automatically decompresses if there is a stream with a matching name and .gz suffix
        ///</summary>
        ///<param name="resourceAssembly">Assembly where to search for streams</param>
        ///<param name="streamName">Stream to find (case insensitive)</param>
        ///<returns>Stream (caller must dispose of it) or null if not found </returns>
        public static Stream FindResourceStream(Assembly resourceAssembly, string streamName)
        {
            if (resourceAssembly == null)
                return null;
            if (string.IsNullOrEmpty(streamName))
                return null;
            foreach (string name in resourceAssembly.GetManifestResourceNames())
            {
                if (name.EndsWith(streamName, StringComparison.OrdinalIgnoreCase))
                {
                    // Try to get as is
                    return resourceAssembly.GetManifestResourceStream(name);
                }
                if (name.EndsWith(streamName + ".gz", StringComparison.OrdinalIgnoreCase))
                {
                    // M'kay, maybe it's compressed
                    Stream s = resourceAssembly.GetManifestResourceStream(name);
                    if (s != null)
                        return new GZipStream(s, CompressionMode.Decompress, false);
                }
            }
            // Better luck next time
            return null;
        }

        /// Return true if any of the parameters is true
        public static object Or(params bool[] bools)
        {
            foreach (var b in bools)
                if (b)
                    return true;
            return false;
        }

        /// Return true if all of the parameters are true
        public static object And(params bool[] bools)
        {
            bool ret = false;
            foreach (var b in bools)
            {
                ret = b;
                if (!ret)
                    break;
            }
            return ret;
        }

        /// Return one of the objects depeding on condition. Note that it does not short-circuit, e.g. IIf(true,func1(),func2()) will call both func1 and func2
        public static object IIf(bool condition, object ifTrue, object ifFalse)
        {
            return condition ? ifTrue : ifFalse;
        }

        /// Return true if objects are equal
        public static bool IsEqual(object o1, object o2)
        {
            if (o1 == null)
                return o2 == null;
            return o1.Equals(o2);
        }

        /// Return true if object is null
        public static bool IsNull(object oTest)
        {
            return oTest == null;
        }

        /// Return first not-null value from the arguments
        public static object Coalesce(params object[] par)
        {
            if (par == null)
                return null;
            foreach (var o in par)
                if (o != null)
                    return o;
            return null;
        }

        
        /// Generator
        public static IEnumerable<int> Range(int min, int max, int step)
        {
            for (int i = min; i <= max; i += step)
                yield return i;
        }

        /// Generator
        public static IEnumerable<int> Range(int min, int max)
        {
            return Range(min, max, 1);
        }

        /// Compare two strings using invariant culture
        public static int Compare(string str1, string str2)
        {
            return string.Compare(str1, str2, StringComparison.InvariantCulture);
        }

        /// Compare two strings using invariant culture and ignoring case
        public static int CompareIgnoreCase(string str1, string str2)
        {
            return string.Compare(str1, str2, StringComparison.InvariantCultureIgnoreCase);
        }

        // Internal class with a decent RNG, that is instantiated upon request
        class RngSingleton
        {
            internal static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
        }

        private static readonly Random _random = new Random();

        /// Get a lousy pseudorandom number between min and max
        public static int GenRandom(int min, int max)
        {
            lock (_random)
                return _random.Next(min, max);
        }

        /// Get a decent pseudorandom number using cryptographical grade RNG
        public static int GenGoodRandom(int min, int max)
        {
            byte[] b = new byte[8];
            lock (RngSingleton.rng)
                RngSingleton.rng.GetBytes(b);
            long rnd = new BinaryReader(new MemoryStream(b)).ReadInt64();
            return (int)(Math.Abs(rnd % (max - min)) + min);
        }

        /// Get count decent pseudorandom bytes using cryptographical grade RNG
        public static byte[] GenGoodRandomBytes(int count)
        {
            byte[] b = new byte[count];
            lock (RngSingleton.rng)
                RngSingleton.rng.GetBytes(b);
            return b;
        }

        /// Read until the end of stream (up to 4GB)
        public static byte[] ReadBytes(Stream stream)
        {
            return ReadBytes(stream, int.MaxValue, false);
        }

        /// <summary>
        /// Read bytes from stream
        /// </summary>
        /// <param name="stream">stream to read</param>
        /// <param name="count">read at least as many bytes</param>
        /// <param name="exactCount">true, if EndOfStreamException must be thrown if EOF is found earlier. Otherwise a shorter than count bytes array is returned</param>
        /// <returns>Data read</returns>
        public static byte[] ReadBytes(Stream stream, int count, bool exactCount)
        {
            var ms = new MemoryStream();
            int n;
            byte[] buf = new byte[16384];
            while ((n = stream.Read(buf, 0, Math.Min(count, buf.Length))) != 0)
            {
                ms.Write(buf, 0, n);
                count -= n;
            }
            if (exactCount && count != 0)
                throw new EndOfStreamException();
            return ms.ToArray();
        }


        /// Get values of all public object fields and properties 
        public static Vars PropsToVars(object o, IStringFilter filter)
        {
            Vars v = new Vars();
            if (o != null)
            {
                Type t = o.GetType();
                foreach (var p in t.GetProperties(BindingFlags.Instance|BindingFlags.Public))
                {
                    if (filter!=null && !filter.IsMatch(p.Name))
                        continue;
                    var ip=p.GetIndexParameters();
                    if (ip != null && ip.Length != 0)
                        continue;
                    if (!p.CanRead)
                        continue;
                    try
                    {
                        v[p.Name] = p.GetValue(o, null);
                    }
                     catch 
                    {
                    }
                }
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (filter != null && !filter.IsMatch(f.Name))
                        continue;
                    try
                    {
                    
                    v[f.Name] = f.GetValue(o);
                    }
                    catch
                    {
                    }
                }
            }
            return v;
        }

        /// Get values of all public object fields and properties 
        public static Vars PropsToVars(object o, string filter)
        {
            return PropsToVars(o, new StringFilter(filter));
        }

        /// Get values of all public object fields and properties 
        public static Vars ObjectToVars(object o)
        {
            return PropsToVars(o, (IStringFilter)null);
        }

        /// Get values of all public object fields and properties 
        public static Vars PropsToVars(object o)
        {
            return PropsToVars(o, (IStringFilter)null);
        }

        /// Get values of all public object fields and properties in the list
        public static IEnumerable<Vars> ListToVars(IEnumerable o)
        {
            if (o != null)
                foreach (var v in o)
                    yield return PropsToVars(v, (IStringFilter)null);
        }

        /// Get values of selected fields and properties in the list
        public static IEnumerable<Vars> ListToVars(IEnumerable o,string filter)
        {
            if (o != null)
                foreach (var v in o)
                    yield return PropsToVars(v, filter);
        }

        /// Get values of selected fields and properties in the list
        public static IEnumerable<Vars> ListToVars(IEnumerable o, IStringFilter filter)
        {
            if (o != null)
                foreach (var v in o)
                    yield return PropsToVars(v, filter);
        }

        /// Return at most count element, starting from the specified element, as array
        public static object[] ToArray(IEnumerable a, int from, int count)
        {
            if (a == null)
                return null;
            var r = new List<object>();
            foreach (var o in a)
            {
                if (from > 0)
                    from--;
                else
                    if (count-- > 0)
                        r.Add(o);
                    else
                        break;
            }
            return r.ToArray();
        }


        /// Convert enumerable to array
        public static object[] ToArray(IEnumerable a)
        {
            return ToArray(a, 0, int.MaxValue);
        }

        /// Skip from elements and return the rest as array
        public static object[] ToArray(IEnumerable a, int from)
        {
            return ToArray(a, from, int.MaxValue);
        }

        /// Create a set of name-value pairs out of list {name1,value1,name2,value2,...}
        public static Vars ToVars(IEnumerable o)
        {
            Vars set = new Vars();
            var en = o.GetEnumerator();
            while (en.MoveNext())
            {

                var name = To<string>(en.Current);
                if (en.MoveNext())
                {

                    set[name] = en.Current;
                }
                else
                    break;
            }
            return set;
        }

        /// Create a set of name-value pairs out of list {name1,value1,name2,value2,...}
        public static Vars ToVars(params object[] o)
        {
            return ToVars((IEnumerable)o);
        }

        /// Convert rowset to datatable with a given name
        public static DataTable ToDataTable(IEnumerable<Vars> data)
        {
            return ToDataTable(data, null);
        }
        /// Convert rowset to datatable with a given name
        public static DataTable ToDataTable(IEnumerable<Vars> data, string tableName)
        {
            DataTable t = new DataTable(tableName ?? "data");
            string emptyName = null;

            List<Vars> rows = new List<Vars>();
            List<Type> colTypes = new List<Type>();
            foreach (var row in data)
            {
                rows.Add(row);
                foreach (var var in row)
                {
                    string col = var.Name;
                    if (string.IsNullOrEmpty(col))
                        col = emptyName;
                    int colNo = -1;
                    if (string.IsNullOrEmpty(col))
                    {
                        emptyName = t.Columns.Add(col).ColumnName;
                        colTypes.Add(null);
                        colNo = t.Columns.Count - 1;
                    }
                    else
                    {
                        colNo = t.Columns.IndexOf(col);
                        if (colNo == -1)
                        {
                            t.Columns.Add(col);
                            colTypes.Add(null);
                            colNo = t.Columns.Count - 1;
                        }
                    }
                    object prev = colTypes[colNo];
                    if (var.Value == null)
                        continue;
                    Type ot = var.Value.GetType();
                    if (prev == null)
                    {
                        colTypes[colNo] = ot;
                        continue;
                    }

                    colTypes[colNo] = Utils.CommonBase(var.Value, colTypes[colNo]);
                }
            }
            for (int i = 0; i < colTypes.Count; ++i)
            {
                if (colTypes[i] == null)
                    colTypes[i] = typeof(object);
                t.Columns[i].DataType = colTypes[i];
            }
            foreach (var row in rows)
            {
                DataRow r = t.NewRow();
                foreach (var var in row)
                {
                    string col = var.Name;
                    if (string.IsNullOrEmpty(col))
                        col = emptyName;
                    int n = t.Columns.IndexOf(col);
                    r[n] = Utils.To(colTypes[n], var.Value) ?? DBNull.Value;
                }
                t.Rows.Add(r);
            }
            return t;
        }


    }


}