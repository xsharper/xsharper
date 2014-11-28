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
using System.Reflection;
using System.Collections;


namespace XSharper.Core
{
    
    /// Utilities
    public partial class Utils
    {
        /// Try to convert object to a given type
        public static bool TryTo(Type pt, object obj, out object value)
        {
            // A rather naive implementation
            value = null;
            if (pt == null) throw new ArgumentNullException("pt");
            try
            {
                value = To(pt, obj);
                return true;
            }
            catch (InvalidCastException)    {return false;}
            catch (ArgumentOutOfRangeException) { return false; }
            catch (TargetInvocationException) { return false; }
        }

        /// Try to convert object to a given type
        public static bool TryTo<T>(object obj, out T value)
        {
            object t;
            if (TryTo(typeof(T), obj, out t))
            {
                value = (T) t;
                return true;
            }
            value = default(T);
            return false;
        }

        /// Convert object to a given type
        public static T To<T>(object obj)
        {
            return (T)To(typeof(T), obj);
        }

        /// Convert object to a given type
        public static object To(Type pt, object obj)
        {
            if (pt == null) throw new ArgumentNullException("pt");

            if (obj == null || pt == typeof(object))
            {
                if (pt.IsArray)
                {
                    Array na = (Array)Activator.CreateInstance(pt, 1);
                    na.SetValue(To(pt.GetElementType(), obj), 0);
                    return na;
                }
                
                if (obj == null && (pt.IsPrimitive || pt == typeof(decimal)))
                    return Convert.ChangeType(0, pt);

                return obj;
            }
            


            var ot = obj.GetType();

            if (pt == ot || ot.IsSubclassOf(pt) || pt.IsAssignableFrom(ot))
                return obj;
            
            if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return To(pt.GetGenericArguments()[0], obj);
            }
            if (pt == typeof(string))
            {
                if (obj is double)
                    return ((double)obj).ToString("R");
                if (obj is float)
                    return ((float)obj).ToString("R");
                return obj.ToString();
            }


            
            // From 0 or 1 element array
            if (!pt.IsArray && ot.IsArray)
            {
                Array arr = (Array)obj;
                if (arr.Rank == 1)
                {
                    if (arr.Length == 0)
                        return null;
                    if (arr.Length == 1)
                        return To(pt, arr.GetValue(0));
                }
            }

            // Arrays convert element by element
            if (pt.IsArray && ot.IsArray)
            {
                Array arr = (Array)obj;
                Array na = (Array)Activator.CreateInstance(pt, arr.Length);
                if (arr.Rank == 1 && na.Rank == 1)
                {
                    for (int i = 0; i < arr.Length; ++i)
                        na.SetValue(To(pt.GetElementType(), arr.GetValue(i)), i);
                    return na;
                }
            }

            // Convert smth enumerable to object
            if (pt.IsArray && obj is IEnumerable)
            {
                ArrayList al=new ArrayList();
                foreach (var o in (IEnumerable)obj)
                    al.Add(o);
                
                Array na = (Array)Activator.CreateInstance(pt, al.Count);
                for (int i = 0; i < al.Count; ++i)
                    na.SetValue(To(pt.GetElementType(), al[i]), i);
                return na;
            }

            // To single element array
            if (pt.IsArray && !ot.IsArray)
            {
                Array na = (Array)Activator.CreateInstance(pt, 1);
                na.SetValue(To(pt.GetElementType(), obj), 0);
                return na;
            }

            string objs = obj as string;
            if (objs != null)
                return convertString(objs, pt);
            if (pt.IsEnum)
                return Enum.ToObject(pt, obj);
            if (pt.IsInterface)
                return obj;
            if (pt == typeof(TimeSpan))
                return ToTimeSpan(obj.ToString());

            if (!ot.IsClass || ot is IConvertible)
                return Convert.ChangeType(obj, pt);

            // We have a class here, which probably can be converted
            if (pt.IsArray && !ot.IsArray && ot.GetMethod("ToArray", Type.EmptyTypes) != null)
            {
                obj = ot.InvokeMember("ToArray", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, obj, new object[0]);
                ot = obj.GetType();
            }
            if (pt == typeof(Guid?))
            {
                try
                {
                    var g = new Guid(ot.ToString());
                    return (Guid?)g;
                }
                catch { return (Guid?)null; }
            }
            if (pt == typeof(Guid))
                return new Guid(ot.ToString());
            if (pt == typeof(TimeSpan))
            {
                var v = Utils.ToTimeSpan(ot.ToString());
                if (v.HasValue)
                    return v.Value;
            }
            if (pt == typeof(TimeSpan?))
            {
                return Utils.ToTimeSpan(ot.ToString());
            }
            throw new InvalidCastException("Cannot convert " + ot + " to " + pt);
        }

		private static char[] s_enumDelimiters="+;,|\n\r\t ".ToCharArray();

        private static object convertString(string text, Type pt)
        {
            if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (String.IsNullOrEmpty(text))
                    return null;
                return convertString(text, pt.GetGenericArguments()[0]);
            }
            if (String.IsNullOrEmpty(text))
            {
                if (pt.IsPrimitive || pt.IsEnum || pt == typeof(decimal))
                    text = "0";

            }


            if (pt == typeof(bool))
            {
                bool t1;
                if (bool.TryParse(text, out t1))
                    return t1;
                return (long)convertString(text,typeof(long))!=0;
            }
            if (pt == typeof(byte) ||
                pt == typeof(sbyte) ||
                pt == typeof(short) ||
                pt == typeof(ushort) ||
                pt == typeof(int) ||
                pt == typeof(uint) ||
                pt == typeof(long) ||
                pt == typeof(ulong) ||
                pt == typeof(decimal) ||
                pt == typeof(float) ||
                pt == typeof(double))
            {
                object o = ParsingReader.TryParseNumber(text);
                if (o == null)
                    throw new InvalidCastException("Cannot cast '" + text + "' to " + pt.FullName);
                return Convert.ChangeType(o, pt);
            }
            if (pt == typeof(char?))
            {
                if (string.IsNullOrEmpty(text))
                    return null;
                return text[0];
            }
            if (pt == typeof(char))
            {
                return text[0];
            }
            if (pt == typeof(DateTime))
                return DateTime.Parse(text);
            if (pt == typeof(TimeSpan))
            {
                var v = Utils.ToTimeSpan(text);
                if (v.HasValue)
                    return v.Value;
            }
            if (pt == typeof(TimeSpan?))
                return Utils.ToTimeSpan(text);
            if (pt == typeof(Guid))
                return new Guid(text);
            if (pt == typeof(string))
                return text;
            if (pt.IsEnum)
            {
                bool hasFlags = CustomAttributeHelper.Has<FlagsAttribute>(pt);
                bool hasDigits = !string.IsNullOrEmpty(text) && text.IndexOfAny("0123456789".ToCharArray()) != -1;
                bool isempty = string.IsNullOrEmpty(text);
                if (isempty || hasFlags || hasDigits)
                {
                    long val = 0;
                    var names = Enum.GetNames(pt);
                    var dictionary = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                    var values = Enum.GetValues(pt);
                    for (int i = 0; i < names.Length; ++i)
                    {
                        long vv = (long)Convert.ChangeType(values.GetValue(i), typeof(long));
                        dictionary[names[i]] = vv;
                        if (isempty && vv == 0)
                            return Enum.ToObject(pt, vv);
                    }
                    if (isempty)
                        throw new InvalidCastException(String.Format("Unexpected empty enum value"));

                    int step = 0;
                    foreach (string str in text.Split(s_enumDelimiters))
                    {
                        if (String.IsNullOrEmpty(str))
                            continue;
                        step++;
                        if (!hasFlags && step > 1)
                            throw new InvalidCastException(String.Format("Unexpected enum value {0}", str));
                        long v;
                        if (char.IsDigit(str[0]))
                            val |= ParsingReader.ParseNumber<long>(str);
                        else if (dictionary.TryGetValue(str, out v))
                            val |= v;
                        else
                            throw new InvalidCastException(String.Format("Unexpected enum value {0}", str));
                    }
                    return Enum.ToObject(pt, val);
                }
                return Enum.Parse(pt, text, true);
            }

            throw new InvalidCastException(String.Format("'{0}' cannot be converted to {1}", text, pt.ToString()));
        }


        /// Convert string timespan (can be in milliseconds, or 00:00:00.33, or P120D) to a TimeSpan.
        static public TimeSpan? ToTimeSpan(string timeout)
        {
            TimeSpan ts;
            double t;
            if (String.IsNullOrEmpty(timeout))
                return null;
            if (timeout[0] == 'P' || (timeout[0] == '-' && timeout.Length > 2 && timeout[1] == 'P'))
                return System.Xml.XmlConvert.ToTimeSpan(timeout);
            if (Double.TryParse(timeout, out t))
                return TimeSpan.FromMilliseconds(t);
            if (TimeSpan.TryParse(timeout, out ts))
                return ts;
            throw new ParsingException(String.Format("Invalid timespan {0}", timeout));
        }
    }
} 
