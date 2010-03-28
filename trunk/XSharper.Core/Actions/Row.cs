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
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace XSharper.Core
{
    /// Attribute to indicate to code generator that the class must be serialized as XML instead of C# code
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CodegenAsXmlAttribute : Attribute
    {

    }

    /// Data row
    [XsType("row", AnyAttribute = true)]
    [CodegenAsXmlAttribute]
    [Description("Data row")]
    public class Row : XsElement, IEnumerable<Var>
    {
        /// Row variables
        private Vars _variables;

        /// Delete all variables
        public void Clear()
        {
            _variables.Clear();
        }

        /// Returns number of values
        public int Count { get { return _variables.Count;  } }
        
        /// Access variables by name
        public object this[string var]
        {
            get { return _variables[var]; }
            set { _variables[var]=value; }
        }

        /// <summary>
        /// Read child element of the current node
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">XML reader</param>
        /// <param name="setToProperty">Property to which the object must be assigned, or null for automatic resolution</param>
        protected override void ReadChildElement(IXsContext context, XmlReader reader, PropertyInfo setToProperty)
        {
            string name = reader.LocalName;
            var v = reader.GetAttribute("_xml");
            string value;
            if (v != null && Utils.To<bool>(v))
                value = reader.ReadInnerXml();
            else
                value = reader.ReadElementContentAsString();

            _variables.Add(new Var(name,value));
        }
        
        
        /// "" value
        [XsAttribute("")]
        public string Text 
        { 
            get {   
                return _variables.GetString(string.Empty,null);
            }
            set
            {
                _variables[string.Empty] = value;
            }
        }

        
        /// Constructor
        public Row()
        {
            _variables = new Vars();
        }
        /// Constructor
        public Row(params Var[] vars)
        {
            _variables = new Vars(vars);
        }
        /// Constructor from XML
        public Row(string xml) : this()
        {
            using (XmlTextReader tr = new XmlTextReader(new StringReader(xml)))
                base.ReadXml(null, tr);
        }
        
        /// Constructor copying another set of variables
        public Row(Vars vs) 
        {
            _variables=new Vars(vs);
        }


        /// <summary>
        /// Called when XML Reader reads an attribute or a text field
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="attribute">Attribute name, or an empty string for element text</param>
        /// <param name="value">Attribute value</param>
        /// <param name="previouslyProcessed">List of previously processed attributes, to detect duplicate attributes. May be null if duplicate attributes are allowed.</param>
        /// <returns>true, if the attribute if correctly processed and false otherwise</returns>
        protected override bool ProcessAttribute(IXsContext context, string attribute, string value, IDictionary<string, bool> previouslyProcessed)
        {
            if (previouslyProcessed != null && !string.IsNullOrEmpty(attribute))
                previouslyProcessed.Add(attribute, true);
            _variables[attribute ?? string.Empty] = value;
            return true;
        }

        /// <summary>
        /// Write attributes to the XmlWriter
        /// </summary>
        /// <param name="writer">XmlWriter where the attributes must be written</param>
        protected override void WriteAttributes(XmlWriter writer)
        {
            base.WriteAttributes(writer);
            foreach (Var pair in _variables)
            {
                if (!string.IsNullOrEmpty(pair.Name))
                    writer.WriteAttributeString(pair.Name, Utils.To<string>(pair.Value));
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<Var> GetEnumerator()
        {
            return _variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}