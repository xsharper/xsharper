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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace XSharper.Core
{
    /// <summary>
    /// Embedded XML data island
    /// </summary>
    [XsType("xmldoc", ScriptActionBase.XSharperNamespace)]
    [Description("Embedded XML data island")]
    public class XmlDoc : DynamicValueFromFileBase
    {
        /// Dummy field, to keep serializer happy
        [XsElement("", SkipIfEmpty = true, CollectionItemElementName = null)]
        internal string xml_string { get { return null; } set { ;} }

        /// Rowset to be converted into XML document
        [Description("Rowset to be converted into XML document")]
        public string RowsetId { get; set; }

        /// Root element name of the XML document. Default: "data"
        [Description("Root element name of the XML document")]
        public string RsRootName { get; set; }

        /// Recordset row element name. Default: "row"
        [Description("Recordset row element name.")]
        public string RsRowName { get; set; }

        /// Value to be stored instead of null
        [Description("Value to be stored instead of null")]
        public object RsNull { get; set; }

        /// True if rowset values should be stored as attributes (default). False = store as elements
        [Description("True if rowset values should be stored as attributes (default). False = store as elements")]
        public bool RsUseAttributes { get; set; }

        /// Namespace of the generated xml document
        [Description("Namespace of the generated xml document")]
        public string RsNamespace { get; set; }

        private XmlDocument _xmlDocument;

        /// Constructor
        public XmlDoc()
        {
            Verbatim = true;
            RsRootName = "data";
            RsRowName = "row";
            RsUseAttributes = true;
        }

        /// <summary>
        /// Write element text
        /// </summary>
        /// <param name="writer">XML writer</param>
        protected override void WriteText(XmlWriter writer)
        {
            if (Value != null)
                writer.WriteValue(Value);
        }

        /// <summary>
        /// Read child element of the current node
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">XML reader</param>
        /// <param name="property">Property to which the object must be assigned, or null for automatic resolution</param>
        protected override void ReadChildElement(IXsContext context, XmlReader reader, PropertyInfo property)
        {
            if (Value!=null)
                throw new ParsingException("Multiple root elements in xmldoc value are not allowed");
            Value= reader.ReadOuterXml();
        }

        /// Get a single value at given XPath
        public string V(string xpath, string defaultValue)
        {
            return XValue(xpath, defaultValue);
        }

        /// Get a single value at given XPath
        public string XValue(string xpath, string defaultValue)
        {
            var s = XValues(xpath);
            if (s.Length == 0)
                return defaultValue;
            if (s.Length == 1)
                return s[0];
            throw new ScriptRuntimeException(s.Length+" values were found for '"+xpath+"', but only 1 was expected");
        }

        /// Get a single value at given XPath. Value must exist.
        public string V(string xpath)
        {
            return XValue(xpath);
        }

        /// Get a single value at given XPath. Value must exist.
        public string XValue(string xpath)
        {
            var s = XValues(xpath);
            if (s.Length == 0)
                throw new ScriptRuntimeException("Value not found for '" + xpath + "'");
            if (s.Length == 1)
                return s[0];
            throw new ScriptRuntimeException(s.Length + " values were found for '" + xpath + "', but only 1 was expected");
        }

        /// Get a single XML Node matching the given XPath. If it does not exist null is returned. 
        /// If it does exist, it must be a single node, or <see cref="ScriptRuntimeException"/> is thrown.
        public XmlNode this[string xpath]
        {
            get { return Node(xpath); }
        }

        /// Get a single XML Node matching the given XPath. If it does not exist null is returned. 
        /// If it does exist, it must be a single node, or <see cref="ScriptRuntimeException"/> is thrown.
        public XmlNode Node(string xpath)
        {
            var c = Node(xpath,null);
            if (c == null)
                throw new ScriptRuntimeException("Node '"+xpath+"' was not found");
            return c;
        }

        /// Get a single XML Node matching the given XPath. If it does not exist, def argument is returned. 
        /// If it does exist, it must be a single node, or <see cref="ScriptRuntimeException"/> is thrown.
        public XmlNode Node(string xpath, XmlNode def)
        {
            var c = Nodes(xpath);
            if (c == null || c.Count == 0)
                return def;
            if (c.Count == 1)
                return c[0];
            throw new ScriptRuntimeException(c.Count + " nodes were found for '" + xpath + "', but only 1 was expected");
        }

        /// Return reference to the XMLDocument represented by this action
        public XmlDocument XmlDocument
        {
            get
            {
                if (_xmlDocument == null)
                    load();
                if (_xmlDocument == null)
                    throw new ScriptRuntimeException("xmldoc is referenced before it is initialized");
                return _xmlDocument;
            }
        }

        /// Find multiple nodes matching the given xpath. Null is returned if nothing is found.
        public XmlNodeList Nodes(string xpath)
        {
            
            var x = XmlDocument;
            XmlNamespaceManager ns = new XmlNamespaceManager(x.NameTable);
            if (x.FirstChild != null)
                ns.AddNamespace("_", x.FirstChild.NamespaceURI);
            return x.SelectNodes(xpath, ns);
        }
        
        
        /// Find multiple values matching the given xpath. Empty array is returned if nothing is found.
        public string[] Vs(string xpath)
        {
            return XValues(xpath);
        }

        /// Find multiple values matching the given xpath. Empty array is returned if nothing is found.
        public string[] XValues(string xpath)
        {
            
            XmlNodeList x = Nodes(xpath);
            List<string> s = new List<string>();
            if (x != null)
                foreach (XmlNode o in x)
                    s.Add(o.Value);
            return s.ToArray();
        }


        /// true, if there is at least one node matching the given xpath
        public bool Any(string xpath)
        {
            return Nodes(xpath) != null;
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            if (!string.IsNullOrEmpty(From) && Value!=null)
                throw new ParsingException("Xml document can either be loaded from file, or declared locally. Remove 'from' attribute or value.");
        }

        /// Execute action
        public override object Execute()
        {
            var r=base.Execute();
            if (r != null)
                return r;
            load();
            return null;
        }

        private void load()
        {
            string rs = Context.TransformStr(RowsetId, Transform);
            if (!string.IsNullOrEmpty(rs))
            {
                RowSet rw = Context.Find<RowSet>(rs);
                using (var dt = rw.ToDataTable(RsRootName))
                {
                    dt.Namespace = RsNamespace;
                    ReplaceDocument(Utils.ToXml(dt, Context.TransformStr(RsRootName, Transform),
                                                    Context.TransformStr(RsNamespace, Transform),
                                                    Context.TransformStr(RsRowName, Transform),
                                                    (RsNull != null) ? Utils.To<string>(Context.Transform(RsNull, Transform)) : null,
                                                    RsUseAttributes));
                }
            }
            else 
            {
                _xmlDocument = new XmlDocument();
                _xmlDocument.PreserveWhitespace = true;
                string fromts = Context.TransformStr(From, Transform);
                if (!string.IsNullOrEmpty(fromts) && (Verbatim || Transform == TransformRules.None) && string.IsNullOrEmpty(Context.TransformStr(Encoding, Transform)))
                {
                    using (var str = Context.OpenReadStream(fromts))
                        _xmlDocument.Load(str);
                }
                else 
                    if (!string.IsNullOrEmpty(GetTransformedValueStr()))
                        using (StringReader sr = new StringReader(GetTransformedValueStr()))
                            _xmlDocument.Load(sr);
            }
        }

        /// OuterXml of the XML document
        public string OuterXml
        {
            get { return XmlDocument.OuterXml; }
        }

        

        /// Save XML document to string using UTF-8 encoding
        public string Encode()
        {
            return Encode(System.Text.Encoding.UTF8);
        }

        /// Save XML document to string using the given encoding
        public string Encode(string encoding)
        {
            return Encode(Utils.GetEncoding(encoding));
        }

        /// Save XML document to string using the given encoding
        public string Encode(Encoding encoding)
        {
            XmlDocument x = XmlDocument;
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlTextWriter tw = new XmlTextWriter(ms, encoding))
                {
                    tw.Formatting = Formatting.Indented;
                    tw.Indentation = 2;
                    tw.IndentChar = ' ';
                    x.Save(tw);
                    tw.Flush();
                    ms.Position = 0;
                    StreamReader sr = new StreamReader(ms);
                    return sr.ReadToEnd();
                }
            }
        }

        /// Save document to file 
        public void Save(string filename)
        {
            Save(filename, (Encoding)null);
        }

        /// Save document to file with specified encoding
        public void Save(string fileName, string encoding)
        {
            Save(fileName,(encoding==null)?(Encoding)null:Utils.GetEncoding(encoding));
        }
        /// Save document to file with specified encoding
        public void Save(string fileName, Encoding encoding)
        {
            if (encoding == null)
            {
                string[] comp = fileName.Split('|');
                fileName = comp[0];
                if (comp.Length >= 2)
                    encoding = Utils.GetEncoding(comp[1]);
            }
            if (encoding == null)
                encoding = Utils.GetEncoding("utf8/nobom");

            XmlDocument x = XmlDocument;
            using (var f=Context.OpenFileStream(fileName,FileMode.Create,false))
            {
                using (XmlTextWriter tw = new XmlTextWriter(f, encoding))
                {
                    tw.Formatting = Formatting.Indented;
                    tw.Indentation = 2;
                    tw.IndentChar = ' ';
                    x.WriteTo(tw);
                }
            }
        }

        /// Replace internal XML document with another document
        public void ReplaceDocument(XmlDocument doc)
        {
            _xmlDocument=doc;
            From = null;
            Value = null;
            RowsetId = null;
        }
    }
}