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
using System.Security.Permissions;
using System.Xml;

namespace XSharper.Core
{
    /// <summary>
    /// XML element
    /// </summary>
    public class XsElement : MarshalByRefObject, IXsElement 
    {
        private bool _textFound;
        /// <summary>
        /// Constructor
        /// </summary>
        protected XsElement()
        {
        }
        
        /// <summary>
        /// Returns null, to make the XsElement to hang out forever in remoting scenarios.
        /// 
        /// See <see cref="MarshalByRefObject.InitializeLifetimeService"/> documentation for more info.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null; // Forever
        }

        /// <summary>
        /// Get properties of the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static PropertyInfo[] GetOrderedElementProperties(Type type)
        {
            var sequenceElements = new List<KeyValuePair<PropertyInfo,int>>();
            PropertyInfo[] prop=type.GetProperties();
            for (int i=0;i<prop.Length;++i)
            {
                PropertyInfo pi=prop[i];
                var atn = (CustomAttributeHelper.First<XsElementAttribute>(pi));
                if (atn == null || atn.Name.StartsWith("_",StringComparison.Ordinal))
                    continue;

                int order=(atn.Ordering)*10000+i;
                sequenceElements.Add(new KeyValuePair<PropertyInfo,int>(pi,order));
            }
            sequenceElements.Sort((x1, x2) => (x1.Value - x2.Value));
            PropertyInfo[] ret = new PropertyInfo[sequenceElements.Count];
            for (int i=0;i<sequenceElements.Count;++i)
                ret[i]=sequenceElements[i].Key;
            return ret;
        }

        /// <summary>
        /// Called when XML Reader reads an attribute or a text field
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="attribute">Attribute name, or an empty string for element text</param>
        /// <param name="value">Attribute value</param>
        /// <param name="previouslyProcessed">List of previously processed attributes, to detect duplicate attributes. May be null if duplicate attributes are allowed.</param>
        /// <returns>true, if the attribute if correctly processed and false otherwise</returns>
        protected virtual bool ProcessAttribute(IXsContext context, string attribute, string value, IDictionary<string,bool> previouslyProcessed)
        {
            if (string.IsNullOrEmpty(attribute))
                return processText(value);

            bool proc = false;
            foreach (PropertyInfo c in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty))
            {
                var n = XsAttributeAttribute.GetNames(c,true);
                if (n==null)
                    continue;
                foreach (var s in n)
                {
                    if (string.Compare(s, attribute, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        SetValue(c, value);
                        proc=true;
                    }

                    // Check that we aren't setting the same value twice
                    if (previouslyProcessed != null && proc && !string.IsNullOrEmpty(attribute))
                        previouslyProcessed.Add(s, true);
                }
                if (proc)
                    break;
            }
            return proc;
        }

        /// <summary>
        /// Write attributes to the XmlWriter
        /// </summary>
        /// <param name="writer">XmlWriter where the attributes must be written</param>
        protected virtual void WriteAttributes(XmlWriter writer)
        {
            object defValue = null;
            foreach (PropertyInfo c in GetType().GetProperties())
            {
                var n = XsAttributeAttribute.GetNames(c,false);
                if (hasText(c) || n==null)
                    continue;

                if (defValue == null)
                    defValue = Utils.CreateInstance(GetType());
                
                object v = c.GetValue(this, null);
                if (v != null)
                {
                    object vdef = c.GetValue(defValue, null);
                    if (!v.Equals(vdef))
                    {
                        writer.WriteStartAttribute(n[0]);
                        if (v.GetType().IsEnum)
                        {
                            string[] se = v.ToString().Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                            writer.WriteValue(string.Join(" ", Array.ConvertAll<string, string>(se, Utils.LowercaseFirstLetter)));
                        }
                        else
                            writer.WriteValue(v);
                        writer.WriteEndAttribute();
                    }
                }
            }
        }

        /// <summary>
        /// Write element to the output stream
        /// </summary>
        /// <param name="writer">Where to write</param>
        /// <param name="nameOverride">Local name to be used, or null if name should be retirevent from <see cref="XsTypeAttribute"/> of the type.</param>
        public virtual void WriteXml(XmlWriter writer, string nameOverride)
        {
            string namesp = null;
            if (nameOverride==null)
            {
                foreach (XsTypeAttribute a in CustomAttributeHelper.All<XsTypeAttribute>(GetType()))
                {
                    nameOverride = a.Name;
                    namesp = a.Namespace;
                    break;
                }
                if (nameOverride==null)
                    return;
            }
            writer.WriteStartElement(nameOverride, namesp);

            WriteAttributes(writer);
            WriteText(writer);

            foreach (PropertyInfo c in GetOrderedElementProperties(GetType()))
            {
                object v = c.GetValue(this, null);
                IXsElement o = v as IXsElement;
                XsElementAttribute ab = CustomAttributeHelper.First<XsElementAttribute>(c);
                if (ab==null)
                    continue;
                if (ab.Name.Length==0)
                {
                    IEnumerable e = (v as IEnumerable);
                    if (e != null)
                        foreach (IXsElement action in e)
                            if (action != null)
                                action.WriteXml(writer, ab.CollectionItemElementName);
                }
                else if (o != null && !(ab.SkipIfEmpty && ab.IsEmpty(v)))
                    o.WriteXml(writer, ab.Name);
            }

            writer.WriteEndElement();
            
        }

        /// <summary>
        /// Read element from the XML reader
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">Reader</param>
        public virtual void ReadXml(IXsContext context, XmlReader reader)
        {
            reader.MoveToContent();

            int startDepth = reader.Depth;
            if (reader.HasAttributes)
            {
                Dictionary<string, bool> attr = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                try
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (!string.IsNullOrEmpty(reader.NamespaceURI))
                            continue;

                        if (!ProcessAttribute(context, reader.LocalName, reader.Value, attr))
                            throw new XsException(reader, string.Format("Unknown attribute '{0}'", reader.Name));
                    }
                }
                catch (Exception e)
                {
                    if (!(e is XsException))
                        throw new XsException(reader, string.Format("Failed to process attribute '{0}'", reader.Name), e);
                    throw;
                }

                reader.MoveToElement();
            }

            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }
            reader.ReadStartElement();
            while (reader.Depth > startDepth && reader.NodeType != XmlNodeType.EndElement)
            {
                ProcessInnerNode(context, reader);
            }
            reader.ReadEndElement();
        }

        /// <summary>
        /// Read child element of the current node
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">XML reader</param>
        /// <param name="setToProperty">Property to which the object must be assigned, or null for automatic resolution</param>
        protected virtual void ReadChildElement(IXsContext context, XmlReader reader, PropertyInfo setToProperty)
        {
            Type t;
            PropertyInfo collProperty = null;
            if (setToProperty != null)
                t = setToProperty.PropertyType;
            else
                ResolveCollectionAndTypeForNode(reader, context, out collProperty, out t);

            object newObject = Utils.CreateInstance(t);
            IXsElement xse = newObject as IXsElement;
            if (xse != null)
                xse.ReadXml(context, reader);
            else
                reader.Skip();

            SetChildObject(reader, newObject, setToProperty, collProperty);
        }

        /// <summary>
        /// Write element text
        /// </summary>
        /// <param name="writer">XML writer</param>
        protected virtual void WriteText(XmlWriter writer)
        {
            // Write text
            string text = string.Empty;
            foreach (PropertyInfo c in GetType().GetProperties())
            {
                foreach (XsAttributeAttribute ab in CustomAttributeHelper.All<XsAttributeAttribute>(c))
                    if (ab.Name == string.Empty)
                    {
                        object v = c.GetValue(this, null);
                        text += v;
                        break;
                    }
            }
            if (text.Length > 0)
            {
                if (text.IndexOfAny("><&".ToCharArray()) != -1)
                    writer.WriteCData(text);
                else
                    writer.WriteValue(text);
            }
        }


        /// <summary>
        /// Set property of the current object to newObject
        /// </summary>
        /// <param name="reader">XML reader</param>
        /// <param name="newObject">New property value</param>
        /// <param name="setToProperty">If not null, <paramref name="newObject"/> must be assigned to this property</param>
        /// <param name="collProperty">If not null, <paramref name="newObject"/> must be added to this IList-derived collection property</param>
        protected virtual void SetChildObject(XmlReader reader, object newObject, PropertyInfo setToProperty, PropertyInfo collProperty)
        {
            if (setToProperty != null)
            {
                setToProperty.SetValue(this, newObject, null);
                return;
            }
            if (collProperty != null)
            {
                object prop = collProperty.GetValue(this, null);
                if (prop == null)
                {
                    prop = Utils.CreateInstance(collProperty.PropertyType);
                    collProperty.SetValue(this, prop, null);
                }
                IList c = prop as IList;
                if (c != null)
                {
                    c.Add(newObject);
                    return;
                }
            }

            throw new XsException(reader, string.Format("Failed to add child element {0}", newObject.GetType().FullName));
        }

        /// <summary>
        /// Set property of the current object to value
        /// </summary>
        /// <param name="propertyInfo">Property to set</param>
        /// <param name="value">Value. It will be converted to the appropriate property type.</param>
        protected virtual void SetValue(PropertyInfo propertyInfo, string value)
        {
            Type pt = propertyInfo.PropertyType;
            propertyInfo.SetValue(this, Utils.To(pt, value), null);
        }

        /// <summary>
        /// Process inner node 
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">Reader</param>
        protected virtual void ProcessInnerNode(IXsContext context, XmlReader reader)
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    ReadChildElement(context, reader, FindRelatedProperty(reader));
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    if (!ProcessAttribute(context, string.Empty, reader.Value,null))
                        throw new XsException(reader, string.Format("Element '{0}' cannot have text", ToString()));
                    reader.Read();
                    break;
                default:
                    reader.Read();
                    break;
            }
        }

        /// Get collection property associated with the current XML node, or null
        protected PropertyInfo FindCollectionForNode(XmlReader reader)
        {
            foreach (PropertyInfo c in GetType().GetProperties())
                foreach (XsElementAttribute e in CustomAttributeHelper.All<XsElementAttribute>(c))
                    if (e.Name.Length==0 && (string.IsNullOrEmpty(e.CollectionItemElementName) || string.Compare(e.CollectionItemElementName,reader.LocalName,StringComparison.OrdinalIgnoreCase)==0))
                        return c;
            return null;
        }

        /// Resolve collection property and node for the current XML node. Exception is thrown if nothing is found.
        protected void ResolveCollectionAndTypeForNode(XmlReader reader, IXsContext context, out PropertyInfo collection, out Type type)
        {
            foreach (PropertyInfo c in GetType().GetProperties())
                foreach (XsElementAttribute e in CustomAttributeHelper.All<XsElementAttribute>(c))
                    if (e.Name.Length==0 && (string.IsNullOrEmpty(e.CollectionItemElementName) || string.Compare(e.CollectionItemElementName,reader.LocalName,StringComparison.OrdinalIgnoreCase)==0))
                    {
                        collection = c;
                        type = e.CollectionItemType;
                        if (context!=null && (type == null || type.IsInterface || type.IsAbstract))
                            type = context.ResolveType(reader);
                        if (type == null || type.IsInterface || type.IsAbstract)
                            continue;
                        return;
                    }

            collection = null;
            type = null;
            throw new XsException(reader, string.Format("Unknown xml element '{0}'", reader.Name));
        }

        /// Try to find property matching the current XmlReader node or return null if not found.
        protected PropertyInfo FindRelatedProperty(XmlReader reader)
        {
            foreach (PropertyInfo c in GetType().GetProperties())
            {
                foreach (XsElementAttribute elements in CustomAttributeHelper.All<XsElementAttribute>(c))
                {
                    if (string.Compare(elements.Name, reader.LocalName, StringComparison.OrdinalIgnoreCase) == 0)
                        return c;
                }
            }
            return null;
        }

        private bool processText(string text)
        {
            foreach (PropertyInfo c in GetType().GetProperties())
                foreach (XsAttributeAttribute a in CustomAttributeHelper.All<XsAttributeAttribute>(c))
                    if (a.Name.Length == 0)
                    {
                        string s = (string)c.GetValue(this, null) ?? string.Empty;
                        if (_textFound)
                            s = s.TrimEnd() + Environment.NewLine + text.TrimStart();
                        else
                            s = text;
                        _textFound=true;
                        c.SetValue(this, s, null);
                        return true;
                    }
            return false;
        }
        private static bool hasText(PropertyInfo p)
        {
            foreach (XsAttributeAttribute a in CustomAttributeHelper.All<XsAttributeAttribute>(p))
                if (a.Name == string.Empty) // Text
                    return true;
            return false;
        }
    }
}