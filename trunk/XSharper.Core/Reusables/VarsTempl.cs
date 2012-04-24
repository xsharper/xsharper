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
using System.Globalization;

namespace XSharper.Core
{
    public partial class Vars
    {
        /// <summary>
        /// Get value of the given variable and convert it to <typeparamref name="T"/>. If variable is not set, return <paramref name="def"/>
        /// </summary>
        /// <remarks>
        /// While it would be nice to expose these without T suffix, they don't work well with remoting because of name clashes with non-templated versions
        /// </remarks>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="variableName">variable name</param>
        /// <param name="def">default value</param>
        /// <returns>Value of the given variable, or <paramref name="def"/>if not set</returns>
        public T GetT<T>(string variableName, T def)
        { 
            object ret;
            if (!TryGetValue(variableName, out ret))
                return def;
            return Utils.To<T>(ret);
        }

        
        /// <summary>
        /// Get value of the given variable, converted to the provided type. If variable is not set, throws KeyNotFound exception 
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="variableName">Variable name</param>
        /// <returns>Value of the variable</returns>
        public T GetT<T>(string variableName)
        {
            return Utils.To<T>(Get(variableName));
        }

        
        /// <summary>
        /// Convert the value of the variable as array. Single values are converted to an 1 element arrays. If variable is not set, or is null, 0 element array is returned
        /// </summary>
        /// <typeparam name="T">Type of the array element</typeparam>
        /// <param name="variableName">Variable name</param>
        /// <returns>Variable value, converted to array of <typeparamref name="T"/></returns>
        public T[] GetArrayT<T>(string variableName)
        {
            variableName = variableName ?? string.Empty;
          
            object o;
            if (!TryGetValue(variableName,out o) || o==null)
                return new T[0];
            Array oa = o as Array;
            if (oa!=null)
            {
                var ret= new T[oa.Length];
                for (int i=0;i<oa.Length;++i)
                    ret[i] = Utils.To<T>(oa.GetValue(i));
                return ret;
            }
            return new T[] { Utils.To<T>(o) };
        }

        // Code is generated from template using templateExpander.xsh script
    #if TEMPLATE
        /// Get variable and convert its value to ${TypeName}. Throw if variable is not set.
        public ${TypeName} Get${TypeSuffix}(string variableName)          { return GetT<${TypeName}>(variableName); }

        /// Get variable and convert its value to ${TypeName}. Returns <paramref name="defaultValue"/> if the variable is not set
        public ${TypeName} Get${TypeSuffix}(string variableName, ${TypeName} defaultValue) { return GetT<${TypeName}>(variableName,defaultValue); }

        /// Get variable and convert its value to array of ${TypeName}. Returns an empty array if the variable is not set. 
        public ${TypeName}[] Get${TypeSuffix}Array(string variableName)    { return GetArrayT<${TypeName}>(variableName); }
#endif // TEMPLATE

        #region

        /// Get variable and convert its value to bool. Throw if variable is not set.
        public bool GetBool(string variableName)          { return GetT<bool>(variableName); }

        /// Get variable and convert its value to bool. Returns <paramref name="defaultValue"/> if the variable is not set
        public bool GetBool(string variableName, bool defaultValue) { return GetT<bool>(variableName,defaultValue); }

        /// Get variable and convert its value to array of bool. Returns an empty array if the variable is not set. 
        public bool[] GetBoolArray(string variableName) { return GetArrayT<bool>(variableName); }
    
        /// Get variable and convert its value to string. Throw if variable is not set.
        public string GetString(string variableName)          { return GetT<string>(variableName); }

        /// Get variable and convert its value to string. Returns <paramref name="defaultValue"/> if the variable is not set
        public string GetString(string variableName, string defaultValue) { return GetT<string>(variableName,defaultValue); }

        /// Get variable and convert its value to array of string. Returns an empty array if the variable is not set. 
        public string[] GetStringArray(string variableName) { return GetArrayT<string>(variableName); }
    
        /// Get variable and convert its value to string. Throw if variable is not set.
        public string GetStr(string variableName)          { return GetT<string>(variableName); }

        /// Get variable and convert its value to string. Returns <paramref name="defaultValue"/> if the variable is not set
        public string GetStr(string variableName, string defaultValue) { return GetT<string>(variableName,defaultValue); }

        /// Get variable and convert its value to array of string. Returns an empty array if the variable is not set. 
        public string[] GetStrArray(string variableName) { return GetArrayT<string>(variableName); }
    
        /// Get variable and convert its value to int. Throw if variable is not set.
        public int GetInt(string variableName)          { return GetT<int>(variableName); }

        /// Get variable and convert its value to int. Returns <paramref name="defaultValue"/> if the variable is not set
        public int GetInt(string variableName, int defaultValue) { return GetT<int>(variableName,defaultValue); }

        /// Get variable and convert its value to array of int. Returns an empty array if the variable is not set. 
        public int[] GetIntArray(string variableName) { return GetArrayT<int>(variableName); }
    
        /// Get variable and convert its value to long. Throw if variable is not set.
        public long GetLong(string variableName)          { return GetT<long>(variableName); }

        /// Get variable and convert its value to long. Returns <paramref name="defaultValue"/> if the variable is not set
        public long GetLong(string variableName, long defaultValue) { return GetT<long>(variableName,defaultValue); }

        /// Get variable and convert its value to array of long. Returns an empty array if the variable is not set. 
        public long[] GetLongArray(string variableName) { return GetArrayT<long>(variableName); }
    
        /// Get variable and convert its value to float. Throw if variable is not set.
        public float GetFloat(string variableName)          { return GetT<float>(variableName); }

        /// Get variable and convert its value to float. Returns <paramref name="defaultValue"/> if the variable is not set
        public float GetFloat(string variableName, float defaultValue) { return GetT<float>(variableName,defaultValue); }

        /// Get variable and convert its value to array of float. Returns an empty array if the variable is not set. 
        public float[] GetFloatArray(string variableName) { return GetArrayT<float>(variableName); }
    
        /// Get variable and convert its value to double. Throw if variable is not set.
        public double GetDouble(string variableName)          { return GetT<double>(variableName); }

        /// Get variable and convert its value to double. Returns <paramref name="defaultValue"/> if the variable is not set
        public double GetDouble(string variableName, double defaultValue) { return GetT<double>(variableName,defaultValue); }

        /// Get variable and convert its value to array of double. Returns an empty array if the variable is not set. 
        public double[] GetDoubleArray(string variableName) { return GetArrayT<double>(variableName); }
    
        /// Get variable and convert its value to decimal. Throw if variable is not set.
        public decimal GetDecimal(string variableName)          { return GetT<decimal>(variableName); }

        /// Get variable and convert its value to decimal. Returns <paramref name="defaultValue"/> if the variable is not set
        public decimal GetDecimal(string variableName, decimal defaultValue) { return GetT<decimal>(variableName,defaultValue); }

        /// Get variable and convert its value to array of decimal. Returns an empty array if the variable is not set. 
        public decimal[] GetDecimalArray(string variableName) { return GetArrayT<decimal>(variableName); }
    
#endregion
    }
}