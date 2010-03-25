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
using System.Diagnostics;
using System.Reflection;

namespace XSharper.Core
{
    class TypeManager
    {
        private readonly List<Assembly> _xsharperAssemblies = new List<Assembly>();
        private readonly List<Assembly> _loadTypesAssemblies = new List<Assembly>();
        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Type> _nonameTypes = new List<Type>();
        

        public TypeManager()
        {
            
            AddAssembly(Assembly.GetExecutingAssembly(), true);
        }

        public IEnumerable<Assembly> AllAssemblies
        {
            get { return _xsharperAssemblies; }
        }

        public bool IsCompiled(string id)
        {
            // If this header has been already compiled into any assembly - no need to compile again
            foreach (Assembly assembly in _xsharperAssemblies)
            {
                foreach (XsHeadersIdentifierAttribute h in assembly.GetCustomAttributes(typeof(XsHeadersIdentifierAttribute), false))
                {
                    if (h.HeadersId == id)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Resolve type on top of the XML reader to a type with <see cref="XsTypeAttribute"/> attribute.
        /// </summary>
        /// <param name="reader">XML reader</param>
        /// <returns>Found type or null if not found</returns>
        public Type ResolveType(System.Xml.XmlReader reader)
        {
            loadAllTypes();
            Type t;
            if (!string.IsNullOrEmpty(reader.NamespaceURI))
            {
                if (_types.TryGetValue(reader.NamespaceURI + "#" + reader.LocalName, out t))
                    return t;
            }
            if (_types.TryGetValue(reader.LocalName, out t))
                return t;
            return null;
        }

        /// Return all known types with XsTypeAttribute
        public Type[] GetKnownTypes()
        {

            loadAllTypes();
            if (_types == null)
                return new Type[0];
            Dictionary<Type, bool> unique = new Dictionary<Type, bool>();
            foreach (Type t in _nonameTypes)
                unique[t] = true;
            foreach (Type t in _types.Values)
                unique[t] = true;

            Type[] ta = new Type[unique.Count];
            unique.Keys.CopyTo(ta, 0);
            return ta;

        }

        private void loadAllTypes()
        {
            foreach (Assembly assembly in _loadTypesAssemblies)
                foreach (Type type in assembly.GetExportedTypes())
                {
                    var arr = type.GetCustomAttributes(typeof(XsTypeAttribute), false);
                    foreach (XsTypeAttribute pair in arr)
                    {
                        if (pair.Name == null)
                        {
                            _nonameTypes.Add(type);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(pair.Namespace))
                            _types[pair.Namespace + "#" + pair.Name] = type;

                        _types[pair.Name] = type;
                    }
                }
            _loadTypesAssemblies.Clear();
        }

        /// Make assembly resolveable in XSharper expressions
        public void AddAssembly(Assembly assembly, bool withTypes)
        {
            if (assembly == null)
                return;
            if (withTypes && !_loadTypesAssemblies.Contains(assembly))
                _loadTypesAssemblies.Add(assembly);

            if (assembly != Assembly.GetExecutingAssembly() && !_xsharperAssemblies.Contains(assembly))
            {
                _xsharperAssemblies.Add(assembly);
            }

        }


    }
}