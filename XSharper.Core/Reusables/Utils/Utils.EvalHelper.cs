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
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace XSharper.Core
{
    /// Helper class for reflection
    public partial class Utils
    {
        /// <summary>
        /// Create object of a given type
        /// </summary>
        /// <param name="objType">Object type</param>
        /// <param name="args">Arguments</param>
        /// <param name="accessPrivate">true, if private constructor may be accessed</param>
        /// <returns>true if object was created</returns>
        public static object CreateInstance(Type objType, Array args, bool accessPrivate)
        {
            if (!accessPrivate && (args == null || args.Length == 0))
                return CreateInstance(objType);
            object retVal;
            if (runAgainstObject(null, objType, null, args, RunFlags.New | (accessPrivate ? RunFlags.AccessPrivate : RunFlags.None), out retVal))
                return retVal;
            return Activator.CreateInstance(objType, args);
        }

        /// Create instance of a given type faster than Activator
        public static object CreateInstance(Type t)
        {
            if (_useFastCreator)
            {
                try
                {
                    var creator = findInstanceCreator(t);
                    return creator();
                }
                catch (NotSupportedException)
                {
                    // In .NET 2.0 this does not fly
                    _useFastCreator = false;
                }
                catch (MissingMethodException)
                {
                    // In .NET 2.0 this does not fly
                    _useFastCreator = false;
                }
                catch (Exception e)
                {
                    Utils.Rethrow(e);
                }
            }
            return Activator.CreateInstance(t);
        }

        /// <summary>
        /// Rethrow an exception while preserving stack trace, to be used in scenarios like below
        /// </summary>
        /// <param name="exception">Exception to rethrow</param>
        /// <example>
        /// try {
        /// ...
        /// }
        /// catch (Exception e)
        /// {
        ///     Utils.Rethrow(e);
        ///
        ///     // The line below will change the stacktrace, making it rather difficult to find where the problem is
        ///     // throw
        /// }
        /// </example>
        public static void Rethrow(Exception exception)
        {
            MethodInfo preserveStackTrace = typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);
            if (preserveStackTrace != null)
                preserveStackTrace.Invoke(exception, null);
            throw exception;
        }
        

        /// Create a COM object given progID or CLSID formatted as string
        public static object CreateComObject(string progId)
        {
            var t = Type.GetTypeFromProgID(progId);
            if (t==null)
            {
                try
                {
                    Guid objId = new Guid(progId);
                    return CreateComObject(objId);
                }
                catch (FormatException)
                {
                }
                throw new TypeLoadException("Failed to create COM object " + progId);
            }
            return Activator.CreateInstance(t);
        }

        /// Create a COM object given CLSID
        public static object CreateComObject(Guid clsid)
        {
            var t = Type.GetTypeFromCLSID(clsid);
            if (t == null)
                throw new TypeLoadException(("Failed to create COM object " + clsid));
            var o = Activator.CreateInstance(t);
            return o;
        }

        
        /// <summary>
        /// Create object of a given type
        /// </summary>
        /// <param name="objType">Object type</param>
        /// <param name="args">Arguments</param>
        /// <param name="accessPrivate">true, if private constructor may be accessed</param>
        /// <param name="retVal">Created object, with any luck</param>
        /// <returns>true if object was created</returns>
        public static bool TryCreateInstance(Type objType, Array args, bool accessPrivate,out object retVal)
        {
            return runAgainstObject(null, objType, null, args, RunFlags.New |(accessPrivate ? RunFlags.AccessPrivate : RunFlags.None), out retVal);
        }

        /// <summary>
        /// Get property of object by name
        /// </summary>
        /// <param name="obj">Object, property of which is retrieved (null for static objects)</param>
        /// <param name="objType">Object type</param>
        /// <param name="propertyName">Name of the property (case insensitive)</param>
        /// <param name="args">Property argument</param>
        /// <param name="accessPrivate">true, if private property may be accessed</param>
        /// <param name="retVal">Retrieved property value</param>
        /// <returns>true if property value was retrieved</returns>
        public static bool TryGetProperty(object obj, Type objType, string propertyName, Array args, bool accessPrivate, out object retVal)
        {
            return runAgainstObject(obj, objType, propertyName, args, RunFlags.Property | (accessPrivate ? RunFlags.AccessPrivate : RunFlags.None), out retVal);
        }


        /// <summary>
        /// Get property of object by name
        /// </summary>
        /// <param name="obj">Object, property of which is retrieved (null for static objects)</param>
        /// <param name="propertyName">Name of the property (case insensitive)</param>
        /// <returns>property value </returns>
        public static object GetProperty(object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            object retVal;
            if (runAgainstObject(obj, obj.GetType(), propertyName, null, RunFlags.Property, out retVal))
                return retVal;
            return GetPropertySimple(obj,propertyName);
        }

        /// <summary>
        /// Get property of object by name
        /// </summary>
        /// <param name="obj">Object, property of which is retrieved (null for static objects)</param>
        /// <param name="propertyName">Name of the property (case insensitive)</param>
        /// <returns>property value </returns>
        public static object GetPropertySimple(object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            object retVal;
            if (runAgainstObject(obj, obj.GetType(), propertyName, null, RunFlags.Property, out retVal))
                return retVal;
            return obj.GetType().InvokeMember(propertyName, BindingFlags.GetProperty | BindingFlags.Instance, null, obj, null);
        }

        /// <summary>
        /// Get property of object by name
        /// </summary>
        /// <param name="obj">Object, property of which is retrieved (null for static objects)</param>
        /// <param name="propertyName">Name of the property (case insensitive)</param>
        /// <returns>property value </returns>
        public static T GetProperty<T>(object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            object retVal;
            if (runAgainstObject(obj, obj.GetType(), propertyName, null, RunFlags.Property, out retVal))
                return Utils.To<T>(retVal);
            return GetPropertySimple<T>(obj, propertyName);
        }


        /// <summary>
        /// Get property of object by name
        /// </summary>
        /// <param name="obj">Object, property of which is retrieved (null for static objects)</param>
        /// <param name="propertyName">Name of the property (case insensitive)</param>
        /// <returns>property value </returns>
        public static T GetPropertySimple<T>(object obj, string propertyName)
        {
            object retVal=obj.GetType().InvokeMember(propertyName, BindingFlags.GetProperty | BindingFlags.Instance, null, obj, null);
            return Utils.To<T>(retVal);
        }


        /// <summary>
        /// Set property of object by name
        /// </summary>
        /// <param name="obj">Object, property of which is set (null for static objects)</param>
        /// <param name="propertyName">Name of the property (case insensitive)</param>
        /// <param name="value">Property value</param>
        public static void SetPropertySimple(object obj, string propertyName, object value)
        {
            obj.GetType().InvokeMember(propertyName, BindingFlags.SetProperty|BindingFlags.Instance, null, obj, new [] { value});
        }

        
        /// <summary>
        /// Call object method 
        /// </summary>
        /// <param name="obj">Object, method of which is retrieved (null for static objects)</param>
        /// <param name="objType">Object type</param>
        /// <param name="methodName">Name of the method to evaluate (case insensitive)</param>
        /// <param name="args">Arguments</param>
        /// <param name="accessPrivate">true, if private methods may be called</param>
        /// <param name="retVal">Return value, if any</param>
        /// <returns>true if method was called successfully</returns>
        public static bool TryCallMethod(object obj, Type objType, string methodName, Array args, bool accessPrivate, out object retVal)
        {
            return runAgainstObject(obj, objType, methodName, args, (accessPrivate ? RunFlags.AccessPrivate: RunFlags.None), out retVal);
        }

        /// <summary>
        /// Call object method 
        /// </summary>
        /// <param name="obj">Object, method of which is retrieved (null for static objects)</param>
        /// <param name="methodName">Name of the method to evaluate (case insensitive)</param>
        /// <param name="args">Arguments</param>
        /// <returns>true if method was called successfully</returns>
        public static object CallMethod(object obj, string methodName, params object[]args)
        {
            object retVal;
            if (runAgainstObject(obj, obj.GetType(), methodName, args, RunFlags.None, out retVal))
                return retVal;
            return CallMethodSimple(obj, methodName, args);
        }

        /// <summary>
        /// Call object method 
        /// </summary>
        /// <param name="obj">Object, method of which is retrieved (null for static objects)</param>
        /// <param name="methodName">Name of the method to evaluate (case insensitive)</param>
        /// <param name="args">Arguments</param>
        /// <returns>true if method was called successfully</returns>
        public static object CallMethodSimple(object obj, string methodName, params object[] args)
        {
            return obj.GetType().InvokeMember(methodName, BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance, null, obj, (object[])args);
        }


        /// <summary>
        /// Call object method 
        /// </summary>
        /// <param name="obj">Object, method of which is retrieved (null for static objects)</param>
        /// <param name="methodName">Name of the method to evaluate (case insensitive)</param>
        /// <param name="args">Arguments</param>
        /// <returns>true if method was called successfully</returns>
        public static T CallMethod<T>(object obj, string methodName, params object[] args)
        {
            object retVal;
            if (runAgainstObject(obj, obj.GetType(), methodName, args, RunFlags.None, out retVal))
                return Utils.To<T>(retVal);
            return CallMethodSimple<T>(obj,methodName,args);
        }


        /// <summary>
        /// Call object method 
        /// </summary>
        /// <param name="obj">Object, method of which is retrieved (null for static objects)</param>
        /// <param name="methodName">Name of the method to evaluate (case insensitive)</param>
        /// <param name="args">Arguments</param>
        /// <returns>true if method was called successfully</returns>
        public static T CallMethodSimple<T>(object obj, string methodName, params object[] args)
        {
            return Utils.To<T>(obj.GetType().InvokeMember(methodName, BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance, null, obj, (object[])args));
        }

        #region -- Gory details --

        private static readonly Dictionary<Type, FastCreateInstance> _fastCreator = new Dictionary<Type, FastCreateInstance>();
        private static bool _useFastCreator = false;
        private delegate object FastCreateInstance();
        private static FastCreateInstance findInstanceCreator(Type type)
        {
            FastCreateInstance oc;
            lock (_fastCreator)
            {
                if (!_fastCreator.TryGetValue(type, out oc))
                {
                    var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                    if (!type.IsPublic || ctor == null)
                        return delegate() { return Activator.CreateInstance(type); };

                    DynamicMethod method = new DynamicMethod(string.Empty, typeof(object), Type.EmptyTypes);
                    ILGenerator gen = method.GetILGenerator();
                    gen.Emit(OpCodes.Newobj, ctor); // new Created
                    gen.Emit(OpCodes.Ret);
                    _fastCreator[type] = oc = (FastCreateInstance)method.CreateDelegate(typeof(FastCreateInstance));
                }
            }
            return oc;
        }

 
        [Flags]
        enum RunFlags
        {
            None = 0,
            New = 1,
            Property = 2,
            AccessPrivate = 4
        }

        // Cache ParamArrayAttribute queries, as they take forever
        [ThreadStatic]
        private static Dictionary<ParameterInfo, bool> _isParam;
        private static bool isParams(ParameterInfo param)
        {
            if (_isParam == null)
                _isParam = new Dictionary<ParameterInfo, bool>();

            bool ret;
            if (!_isParam.TryGetValue(param, out ret))
            {
                ret = param.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0;
                _isParam[param] = ret;
            }
            return ret;
        }

        private static bool runAgainstObject(object obj, Type objType, string func, Array a, RunFlags flags, out object retVal)
        {
            try
            {
                BindingFlags bf = BindingFlags.Public | BindingFlags.Default | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy;
                if ((flags & RunFlags.AccessPrivate) != 0)
                    bf |= BindingFlags.NonPublic;

                bool newFlag = (flags & RunFlags.New) != 0;
                bool propFlag = (flags & RunFlags.Property) != 0;

                if (newFlag)
                {
                    if (objType == null)
                        throw new TypeLoadException("Unknown class '" + func + "'");
                    if (propFlag && a != null)
                    {
                        objType = objType.MakeArrayType(a.Length);
                        propFlag = false;
                    }
                    func = objType.FullName;
                    bf |= BindingFlags.CreateInstance;
                }
                else
                    bf |= BindingFlags.InvokeMethod;
                if (obj == null && !newFlag)
                    bf |= BindingFlags.Static;
                else
                    bf |= BindingFlags.Instance;

                if (propFlag)
                    return evalProperty(obj, objType, bf, func, a, out retVal);

                if (newFlag)
                    return evalConstructor(objType, bf, a, out retVal);
                return evalMethod(obj, objType, bf, func, a, out retVal);
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException != null)
                    throw e.InnerException;
                throw;
            }
        }

        static private bool evalProperty(object obj, Type objType, BindingFlags bindingFlags, string propertyName, Array args, out object retVal)
        {
            if (args == null || args.Length == 0)
                return evalSimpleProperty(obj, objType, bindingFlags, propertyName, out retVal);

            if (obj != null && obj.GetType().IsArray)
            {
                Array arr = (Array)obj;
                int[] indices = new int[arr.Rank];
                if (args.Length == arr.Rank)
                {
                    for (int i = 0; i < args.Length; ++i)
                        indices[i] = Utils.To<int>(args.GetValue(i));
                    retVal = arr.GetValue(indices);
                    return true;
                }
            }
            else if (obj != null && obj.GetType() == typeof(string))
            {
                String str = (String)obj;
                if (args.Length == 1)
                {
                    retVal = str[Utils.To<int>(args.GetValue(0))];
                    return true;
                }
            }
            else
            {
                // "proper" way is to use a custom Binder, but not only we need to invoke, but also to determine whether it can be invoked, so do it manually
                int best = -1;
                PropertyInfo bestPropInfo = null;
                foreach (var prop in objType.GetProperties(bindingFlags))
                {
                    var pp = prop.GetIndexParameters();
                    if (string.Compare(prop.Name, string.IsNullOrEmpty(propertyName) ? "Item" : propertyName, StringComparison.OrdinalIgnoreCase) == 0 &&
                        prop.CanRead)
                    {
                        if (pp.Length == 0)
                        {
                            // We found the property, but it is not an indexed property. Get its value and do [] on it
                            object o = prop.GetValue(obj, null);
                            return evalProperty(o, o.GetType(), bindingFlags & ~BindingFlags.Static | BindingFlags.Instance, string.Empty, args, out retVal);
                        }
                        if (isBestMethod(pp, args, ref best))
                            bestPropInfo = prop;
                    }


                }
                if (bestPropInfo != null)
                {
                    retVal = bestPropInfo.GetValue(obj, cvt(convertParams(bestPropInfo.GetIndexParameters(), args)));
                    return true;
                }
            }
            if (args.Length==1)
            {
                IEnumerable z = obj as IEnumerable;
                if (z!=null)
                {
                    int n = Utils.To<int>(args.GetValue(0));
                    if (n<0)
                        throw new IndexOutOfRangeException();
                    foreach (var o in z)
                        if (n-- == 0)
                        {
                            retVal = o;
                            return true;
                        }
                    throw new IndexOutOfRangeException();
                }
            }
            retVal = null;
            return false;
        }

        static private bool evalMethod(object obj, Type objType, BindingFlags bindingFlags, string methodName, Array args, out object retVal)
        {
            int bestScore = -1;
            MethodInfo bestMethodInfo = null;
            foreach (MethodInfo method in objType.GetMember(methodName, bindingFlags))
            {
                MethodInfo m = method;
                if (method.ContainsGenericParameters)
                {
                    var generics = method.GetGenericArguments();
                    var par = m.GetParameters();
                    if (par.Length != args.Length)
                        continue;
                    Type[] t = new Type[generics.Length];
                    int ptr = 0;
                    for (int i = 0; i < par.Length; ++i)
                    {
                        if (par[i].ParameterType.IsGenericParameter)
                            if (args.GetValue(i) == null)
                                t[ptr++] = typeof(object);
                            else
                                t[ptr++] = args.GetValue(i).GetType();
                    }
                    if (ptr == t.Length)
                        m = method.MakeGenericMethod(t);
                    else
                        m = null;
                }
                if (m!=null && isBestMethod(m.GetParameters(), args, ref bestScore))
                    bestMethodInfo = m;
            }

            if (bestMethodInfo != null)
            {
                retVal = bestMethodInfo.Invoke(obj, cvt(convertParams(bestMethodInfo.GetParameters(), args)));
                return true;
            }
            retVal = null;
            return false;
        }

        static private bool evalConstructor(Type objType, BindingFlags bindingFlags, Array args, out object retVal)
        {
            // For structs it's all really simple w/o arguments  
            if (objType.IsValueType && (args==null || args.Length==0))
            {
                retVal = Utils.CreateInstance(objType);
                return true;
            }
            int bestScore = -1;
            ConstructorInfo bestConstructor = null;
            foreach (ConstructorInfo constr in objType.GetConstructors(bindingFlags))
            {
                if (isBestMethod(constr.GetParameters(), args, ref bestScore))
                    bestConstructor = constr;
            }
            if (bestConstructor != null)
            {
                retVal = bestConstructor.Invoke(cvt(convertParams(bestConstructor.GetParameters(), args)));
                return true;
            }
            retVal = null;
            return false;
        }

        static private bool evalSimpleProperty(object obj, Type objType, BindingFlags bindingFlags, string propertyName, out object retVal)
        {
            if (objType.IsEnum)
            {
                retVal = Utils.To(objType, propertyName);
                return true;
            }

            PropertyInfo propertyInfo = objType.GetProperty(propertyName, bindingFlags);
            if (propertyInfo != null)
            {
                retVal = propertyInfo.GetValue(obj, null);
                return true;
            }

            FieldInfo fieldInfo = objType.GetField(propertyName, bindingFlags);
            if (fieldInfo != null)
            {
                retVal = fieldInfo.GetValue(obj);
                return true;
            }
            retVal = null;
            return false;
        }

        static private object[] cvt(Array a)
        {
            if (a == null)
                return null;
            object[] r = new object[a.Length];
            a.CopyTo(r, 0);
            return r;
        }

        static private Array convertParams(ParameterInfo[] pi, Array a)
        {
            var par = new object[pi.Length];
            int pptr = 0;
            bool f = false; 
            if (a != null)
                for (int i = 0; i < a.Length; ++i)
                {
                    object arg = a.GetValue(i);
                    ParameterInfo p = pi[pptr];
                    Type pt = p.ParameterType;
                    if (isParams(p))
                    {
                        if (!f && arg != null && (pt == arg.GetType() || pt.IsAssignableFrom(arg.GetType())))
                            par[pptr++] = Utils.To(pt, arg);
                        else if (!f && arg != null && arg.GetType().IsArray)
                        {
                            par[pptr++] = Utils.To(pt, arg);
                        }
                        else
                        {
                            if (par[pptr] == null)
                                par[pptr] = Array.CreateInstance(pt.GetElementType(), a.Length - i);
                            var converted = Utils.To(pt.GetElementType(), arg);
                            ((Array)par[pptr]).SetValue(converted, i - pptr);
                            f = true;
                        }
                    }
                    else if (pt.ContainsGenericParameters)
                        par[pptr++] = arg;
                    else
                        par[pptr++] = Utils.To(pt, arg);
                }
            return par;
        }
        static private bool isBestMethod(ParameterInfo[] pi, Array a, ref int bestScore)
        {
            int score = calcScore(pi, a);
            if (score > 0 && score > bestScore)
            {
                bestScore = score;
                return true;
            }
            return false;
        }

        private static readonly Dictionary<Type, int> s_sizeof = sizeofsPrimitiveTypes();
            
        static Dictionary<Type, int> sizeofsPrimitiveTypes()
        {
            Dictionary<Type, int> r = new Dictionary<Type, int>();
            var t = new[] { typeof(bool),typeof(char), typeof(sbyte), typeof(byte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) };
            foreach (var z in t)
                r[z] = Marshal.SizeOf(z);

            // All integer types convert implicitly
            r[typeof(double)] = Marshal.SizeOf(typeof(double))+10;
            r[typeof(float)] = Marshal.SizeOf(typeof(float)) + 10;
            return r;
        }

        // Calculate score over this variant
        static private int calcScore(ParameterInfo[] pi, Array a)
        {
            int score = 0;
            int pptr = 0;
            bool paramsFound = false;
            if (a != null)
                foreach (object arg in a)
                {
                    if (pptr >= pi.Length)
                    {
                        score = -2;
                        break;
                    }

                    Type pt = pi[pptr].ParameterType;
                    if (pt.IsPointer)
                        return -10;
                    if (isParams(pi[pptr]))
                    {
                        score += 3;
                        paramsFound = true;
                    }
                    else if (arg != null)
                    {
                        Type at = arg.GetType();
                        if (pt == at)
                            score += 20;
                        else if (pt.IsAssignableFrom(at))
                            score += 10;
                        else if (pt.IsPrimitive && at.IsPrimitive)
                        {
                            score += (s_sizeof[pt] >= s_sizeof[at]) ? 10 : 8;
                        }
                        else if ((pt==typeof(decimal) || pt==typeof(decimal?)) && at.IsPrimitive)
                            score+=8;
                        else if ((pt == typeof(char) || pt == typeof(char?)) && at == typeof(string))
                            score += 8;
                        pptr++;
                    }
                    else
                    {
                        if (pt.IsPrimitive || pt == typeof (decimal))
                        {
                            score = -2;
                            break;
                        }
                        score += 5;
                        pptr++;
                    }
                }
            if (pptr == pi.Length - 1 && isParams(pi[pptr]))
            {
                score += 3;
                paramsFound = true;
            }
            else if (pi.Length == (a == null ? 0 : a.Length))
                score += 5;
            if (paramsFound)
            {
                if (pptr != pi.Length - 1)
                    return -10;
            }
            else if (pptr != pi.Length)
                return -10;
            return score;
        }

        #endregion

    }
}