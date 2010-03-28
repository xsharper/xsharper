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
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace XSharper.Core
{
    /// <summary>
    /// XML serialization context.
    /// </summary>
    public interface IXsContext
    {
        /// <summary>
        /// Resolve the current element of the XML Reader to a C# type.
        /// </summary>
        /// <param name="reader">XML reader</param>
        /// <returns>type, or null if not found</returns>
        Type ResolveType(XmlReader reader);
    }

    
    /// <summary>
    /// XML reader/writer interface. Every class that is XML serializable implements it
    /// </summary>
    public interface IXsElement 
    {
        /// <summary>
        /// Reads current object from the XML reader
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="reader">XmlReader to read the current element from</param>
        void ReadXml(IXsContext context, XmlReader reader);

        /// <summary>
        /// High-level method, that writes current object to the XML writer
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="writer">XmlWriter where to write</param>
        /// <param name="nameOverride">na</param>
        void WriteXml(IXsContext context, XmlWriter writer, string nameOverride);
    }

    
    ///<summary>
    /// Attribute to mark a property to be XML-serialized as XML attribute.
    ///</summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class XsAttributeAttribute : Attribute
    {
        /// Constructor
        public XsAttributeAttribute(string name)
        {
            Name = name;
        }

        /// Comma-separated list of attribute names, that goes to XML
        public string Name { get; private set; }

        /// Deprecated attribute
        public bool Deprecated { get; set; }

        /// Return names of XML attributes associated with this property, or null if no attributes are found
        public static string[] GetNames(PropertyInfo pi, bool includingDeprecated)
        {
            var v = CustomAttributeHelper.All<XsAttributeAttribute>(pi);
            if (v!=null && v.Length>0)
            {
                int cnt = 0;
                for (int i = 0; i < v.Length; ++i)
                    if (includingDeprecated || v[i].Deprecated==false)
                        cnt++;

                if (cnt==0)
                    return null;

                var ret = new string[cnt];
                var n = 0;
                for (int i = 0; i < v.Length; ++i)
                    if (includingDeprecated || v[i].Deprecated == false)
                        ret[n++] = v[i].Name;
                return ret;
            }
            if (CustomAttributeHelper.Has<XmlIgnoreAttribute>(pi) || !pi.CanRead || !pi.CanWrite || pi.GetIndexParameters().Length!=0 || pi.GetSetMethod()==null)
                return null;
            if (CustomAttributeHelper.Has<XsElementAttribute>(pi))
                return null;
            return new string[] { (pi.Name.Substring(0, 1).ToLowerInvariant() + pi.Name.Substring(1)) };
        }
    }

    ///<summary>Mark XML attribute as required</summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class XsRequiredAttribute : Attribute
    {
        /// Constructor
        public XsRequiredAttribute()
        {

        }
        /// Constructor with a name of a required attribute (needed if attribute has synonyms)
        public XsRequiredAttribute(string attName)
        {
            Name = attName;
        }   

        /// Name of the required attribute, or null if any name matches
        public string Name { get; private set; }
    }


    ///<summary>Attribute to mark types that will be automatically serialized to XML</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class XsTypeAttribute : Attribute
    {
        /// <summary> Constructor </summary>
        /// <param name="name">XML element name</param>
        public XsTypeAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Constructor with name and namespace
        /// </summary>
        /// <param name="name">XML element name</param>
        /// <param name="namespace">XML namespace</param>
        public XsTypeAttribute(string name, string @namespace) : this(name)
        {
            Namespace = @namespace;
        }

        /// XML Element name
        public string Name { get; private set; }

        /// XML Element namespace (null=default)
        public string Namespace { get; private set; }
        
        /// True if this XML element handles any attribute, not only attributes for which it has properties
        public bool AnyAttribute { get; set; }
    }


    ///<summary>XML Element</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class XsElementAttribute : Attribute
    {
        /// Constructor
        public XsElementAttribute(string name)
        {
            Name = name;
            AtLeastOneChildForNotEmpty = true;
        }

        /// XML Element name
        public string Name { get; private set; }

        /// true if element must be ignored if it only contains default values
        public bool SkipIfEmpty { get; set; }

        /// true if element has 0 children, it is considered empty. if false, it's only empty if null
        public bool AtLeastOneChildForNotEmpty { get; set; }

        /// If this element is a collection of other elements, and these other elements are of the same type, this type may be specified here
        public Type CollectionItemType { get; set; }

        /// If this element is a collection of other elements, and these other elements use the same element name, this name may be specified here.
        public string CollectionItemElementName { get; set; }

        /// Order of this XML element related to other elements of the same object
        public int Ordering { get; set; }

        /// Determine if the specified object is empty and should not be saved if <see cref="SkipIfEmpty"/> is set
        public bool IsEmpty(object value)
        {
            if (value == null)
                return true;
            IEnumerable e = (value as IEnumerable);
            if (e != null)
            {
                IEnumerator en = e.GetEnumerator();
                if (en != null)
                {
                    if (!AtLeastOneChildForNotEmpty || en.MoveNext())
                        return false;
                }
            }

            // element is not empty if it has a not empty sub-element
            var props = value.GetType().GetProperties();
            foreach (PropertyInfo c in props)
            {
                var ee = CustomAttributeHelper.First<XsElementAttribute>(c);
                if (ee!=null && !ee.IsEmpty(c.GetValue(value, null)))
                    return false;
            }

            // Or any non-empty attributes
            object defValue = null;
            foreach (PropertyInfo c in props)
            {
                var n = XsAttributeAttribute.GetNames(c, false);
                if (n == null)
                    continue;

                object v = c.GetValue(value, null);
                if (v != null)
                {
                    if (defValue == null)
                        defValue = Utils.CreateInstance(value.GetType());
                    object vdef = c.GetValue(defValue, null);
                    if (!v.Equals(vdef))
                        return false;
                }
            }
            return true;
        }
    }

    
    ///<summary>XML exception</summary>
    [Serializable]
    public class XsException : Exception
    {
        /// Constructor
        public XsException()
        {
        }

        /// Constructor that tries to retrieve current line info from the XML Reader
        public XsException(XmlReader reader, string message) : base(message+getAtSuffix(reader))
        {
        }

        /// Constructor that tries to retrieve current line info from the XML Reader
        public XsException(XmlReader reader, string message, Exception inner)
            : base(message + getAtSuffix(reader), inner)
        {
        }

        /// Serialization constructor
        protected XsException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        private static string getAtSuffix(XmlReader reader)
        {
            if (reader == null)
                return string.Empty;
            IXmlLineInfo lineInfo = reader as IXmlLineInfo;
            string line = "";
            if (lineInfo != null && lineInfo.HasLineInfo())
                line = string.Format(" at ({0},{1})", lineInfo.LineNumber, lineInfo.LinePosition);
            return line;
        }
    }   


    
}