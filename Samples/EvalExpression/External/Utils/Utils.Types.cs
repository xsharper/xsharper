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
using System.Reflection;

namespace XSharper.Core
{
    /// Utilities
    public partial class Utils
    {
        /// Find type given a type name, and return the found type or null if not found
        public static Type FindType(string ts)
        {
            if (ts.EndsWith("?", StringComparison.Ordinal))
            {
                var f = FindType(ts.Substring(0, ts.Length - 1));
                if (f==null)
                    return f;
                return typeof (Nullable<>).MakeGenericType(f);
            }

            switch (ts)
            {
                case "int":
                    return typeof(int);
                case "uint":
                    return typeof(uint);
                case "long":
                    return typeof(long);
                case "ulong":
                    return typeof(ulong);
                case "sbyte":
                    return typeof(sbyte);
                case "byte":
                    return typeof(byte);
                case "short":
                    return typeof(short);
                case "ushort":
                    return typeof(ushort);
                case "float":
                    return typeof(float);
                case "double":
                    return typeof(double);
                case "decimal":
                    return typeof(decimal);
                case "bool":
                    return typeof(bool);
                case "char":
                    return typeof(char);
                case "string":
                    return typeof(string);
            }
            
            var t=Type.GetType(ts, false);
            if (t!=null)
                return t;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly != Assembly.GetExecutingAssembly())
                {
                    t = assembly.GetType(ts, false);
                    if (t != null)
                        return t;
                }

            }
            return null;
        }


        /// <summary>
        /// Find a base type between p and the given type
        /// </summary>
        /// <param name="p">object to test</param>
        /// <param name="common">type. can be null</param>
        /// <returns>Common base type of p and common</returns>
        public static Type CommonBase(object p, Type common)
        {
            if (ReferenceEquals(p, null))
            {
                if (common == null || common.IsClass || common == typeof(Nullable<>))
                    return common;
                return typeof(object);
            }
            Type pt = p.GetType();
            if (common == null || pt == common)
                return pt;

            TypeCode commonTc = Type.GetTypeCode(common);
            TypeCode ptTc = Type.GetTypeCode(pt);
            if ((ptTc >= TypeCode.Boolean && ptTc <= TypeCode.Decimal) &&
                    (commonTc >= TypeCode.Boolean && commonTc <= TypeCode.Decimal))
            {
                if (commonTc > ptTc)
                {
                    return common;
                }
                return pt;
            }

            while (common.BaseType != null)
            {
                if (pt == common || pt.IsSubclassOf(common))
                    break;


                common = common.BaseType;
                if (common == typeof(ValueType))
                    common = typeof(object);

            }
            return common;
        }
    }
}