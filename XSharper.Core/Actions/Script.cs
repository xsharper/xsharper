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
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Reflection;

namespace XSharper.Core
{
    /// XSharper script
    [XsType("xsharper", ScriptActionBase.XSharperNamespace)]
    [XsType("script", ScriptActionBase.XSharperNamespace)]
    [Description("XSharper script")]
    public class Script : Block
    {
        private string _switchPrefixes = "/;-";
        UsageGenerator _usage = new UsageGenerator();
        List<CommandLineParameter> _parameters = new List<CommandLineParameter>();
        VersionInfo _version = new VersionInfo();

        /// Engine version required to run this script
        [Description("Engine version required to run this script")]
        [XsNotTransformed]
        public string EngineVersion { get; set; }

        /// .NET version required to run this script
        [Description(".NET version required to run this script")]
        public string NetVersion { get; set; }

        /// true if administrative privileges must be requested before running the script
        [Description("true if administrative privileges must be requested before running the script")]
        public bool RequireAdmin { get; set; }

        /// true if Ctrl+C should be ignored during script execution, and treated as input instead
        [Description("true if Ctrl+C should be ignored during script execution, and treated as input instead")]
        public bool IgnoreCtrlC { get; set; }

        /// Delay between Ctrl+C event, and forceful script abort
        [Description("Delay between Ctrl+C event, and forceful script abort")]
        [XsNotTransformed]
        public string AbortDelay { get; set; }

        /// Delay between script thread abort and forceful program termination
        [Description("Delay between script thread abort and forceful program termination")]
        [XsNotTransformed]
        public string ExitDelay { get; set; }


        /// true if script must be compiled before execution (default: true). If set to false, script is compiled only when first C# snippet is executed.
        [Description("true if script must be compiled before execution (default: true). If set to false, script is compiled only when first C# snippet is executed.")]
        public bool Precompile { get; set; }

        /// Semi-colon separated command line switch prefixes. Default "/;-", making /hello and -HELLO equivalent
        [Description("Semi-colon separated command line switch prefixes. Default '/;-', making /hello and -HELLO equivalent")]
        [XsAttribute("switchPrefixes")]
        [XsNotTransformed]
        public string SwitchPrefixes
        {
            get { return _switchPrefixes; }
            set { _switchPrefixes = value; }
        }

        /// true if unknown command line switches must be added to the list of script parameters are normal values. false (default) = exception is thrown.
        [Description("true if unknown command line switches must be added to the list of script parameters are normal values. false (default) = exception is thrown.")]
        public bool UnknownSwitches
        {
            get; set;
        }
        
        /// Script version information
        [XsElement("versionInfo", SkipIfEmpty = true)]
        public VersionInfo VersionInfo
        {
            get { return _version; }
            set { _version = value; }
        }

        /// Script usage generator
        [XsElement("usage", SkipIfEmpty = true)]
        public UsageGenerator Usage
        {
            get { return _usage; }
            set { _usage = value;}
        }

        /// Script XML signature
        public string XmlSignature
        {
            get;
            private set;
        }

        /// Script command line parsing instructions
        [XsElement("", CollectionItemElementName = "param", CollectionItemType = typeof(CommandLineParameter))]
        public List<CommandLineParameter> Parameters
        {
            get { return _parameters; } 
            set { _parameters = value; }
        }

        
        /// Script location, usually a filename
        public string Location { get; private set; }

        /// Script full filename with path
        public string FullName { get { return Location==null?null:Path.GetFullPath(Location); } }

        /// Directory of <see cref="FullName"/>
        public string DirectoryName { get { return Location==null?null:Utils.BackslashAdd(new FileInfo(FullName).DirectoryName); } }
        

        /// Constructor
        public Script()
        {
            EngineVersion = "0.9.0.1050";
            Precompile = true;
            SwitchPrefixes = "/;-";
            ExitDelay = "5000";
            AbortDelay = "5000";
        }

        /// Constructor
        public Script(string location)  : this()
        {
            Location = location;
        }



        /// <summary>
        /// Read child element of the current node
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">XML reader</param>
        /// <param name="setToProperty">Property to which the object must be assigned, or null for automatic resolution</param>
        protected override void ReadChildElement(IXsContext context, XmlReader reader, System.Reflection.PropertyInfo setToProperty)
        {
            if (reader.LocalName == "Signature")
                XmlSignature=reader.ReadOuterXml();
            else
                base.ReadChildElement(context, reader, setToProperty);
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            if (Context.CoreVersion < new Version(EngineVersion))
                throw new ParsingException(string.Format("XSharper engine version {0} is required to run this script", EngineVersion));

            Context.Compiler.AddVersion(string.IsNullOrEmpty(NetVersion) ? Context.Compiler.DefaultNETVersion : new Version(NetVersion));
            Context.Compiler.AddRequireAdmin(RequireAdmin);
            base.Initialize();
        }
        
        /// Execute action
        public override object Execute()
        {
            // Script does nothing when execute in the normal code.
            // It MUST be called through context.ExecuteScript
            return null;
        }


        /// <summary>
        /// Execute script against the current Context.
        /// 
        /// This method is not intended to be called directly.
        /// </summary>
        /// <returns>Script return value</returns>
        public virtual object ExecuteScript()
        {
            // 
            CommandLineParameters c = new CommandLineParameters(Parameters, SwitchPrefixes, UnknownSwitches);
            c.ApplyDefaultValues(Context);
            c.CheckRequiredValues(Context);
            return ReturnValue.Unwrap(base.Execute());
        }

        /// Parse command line arguments
        public void ParseArguments(IEnumerable<string> args)
        {
            CommandLineParameters c = new CommandLineParameters(Parameters, SwitchPrefixes, UnknownSwitches);
            c.Parse(Context, args, (Usage.Options & UsageOptions.IfHelp) != 0);
        }


        /// Returns a <see cref="System.String"/> that represents this instance.
        public override string ToString()
        {
            return string.Format("xsharper(location=\"{0}\")", Utils.FitWidth((Location??string.Empty).ToString(), 30, FitWidthOption.EllipsisStart));
        }
        
        /// <summary>
        /// Save script as XML to a file
        /// </summary>
        /// <param name="fileName">file where to save it</param>
        public void Save(string fileName)
        {
            using (Stream tr = Context.CreateStream(fileName))
                Save(tr);
        }

        /// <summary>
        /// Save script to XML writer
        /// </summary>
        /// <param name="writer">writer where to save it</param>
        public void Save(XmlWriter writer)
        {
            WriteXml(writer, null);
        }

        /// <summary>
        /// Save script to text stream
        /// </summary>
        /// <param name="writer">writer where to save it</param>
        public void Save(TextWriter writer)
        {

            using (XmlWriter w = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, Encoding = writer.Encoding, NewLineOnAttributes = false }))
                WriteXml(w, null);
        }

        /// <summary>
        /// Save script to stream in UTF8 encoding
        /// </summary>
        /// <param name="stream">stream where to save it</param>
        public void Save(Stream stream)
        {
            using (XmlWriter w = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Encoding = new System.Text.UTF8Encoding(false), NewLineOnAttributes = false }))
                WriteXml(w,null);
        }
        
        /// <summary>
        /// Save script to string
        /// </summary>
        public string SaveToString()
        {
            using (StringWriter sw=new StringWriter())
            {
                Save(sw);
                return sw.ToString();
            }
        }

        /// <summary>
        /// Load script from string
        /// </summary>
        /// <param name="scriptXml">script as XML</param>
        public void Load(string scriptXml)
        {
            Items.Clear();
            bool close = true;
            var r = new ParsingReader(new StringReader(scriptXml));
            try
            {
                r.SkipWhiteSpace();
                if (r.Peek() == '<')
                {
                    // Get a script file
                    XmlReaderSettings rs = new XmlReaderSettings();
                    rs.IgnoreWhitespace = false;
                    rs.ValidationType = ValidationType.None;

                    using (XmlReader xr = XmlReader.Create(r, new XmlReaderSettings() {ConformanceLevel = ConformanceLevel.Fragment}))
                    {
                        close = false;
                        Load(xr);
                        return;
                    }
                }
                
                
                if (r.Peek() == '=')
                {
                    r.Read();
                    Items.Add(new Eval { Value=r.ReadToEnd()});
                    return;
                }
                Items.Add(new Code { Value=r.ReadToEnd() });
                return;
            }
            finally
            {
                if (close)
                    r.Close();
            }
        }

        /// <summary>
        /// Load script from XML reader
        /// </summary>
        /// <param name="reader">reader from where to load it</param>
        public void Load(XmlReader reader)
        {
            var context = Context;
            Items.Clear();
            do
            {
                if (reader.NodeType == XmlNodeType.None || reader.NodeType == XmlNodeType.XmlDeclaration || reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.Comment)
                    reader.Read();
                else if (reader.NodeType == XmlNodeType.ProcessingInstruction)
                    ProcessInnerNode(context, reader);
                else
                    break;
            } while (!reader.EOF);

            Type t=context.ResolveType(reader);
            if (t != null && (t == GetType() || t.IsSubclassOf(GetType())))
                ReadXml(context, reader);
            else
            {
                // Load fragment
                while (!reader.EOF)
                    ProcessInnerNode(context, reader);
            }
            context.Compiler.AddRequireAdmin(RequireAdmin);
            if (!Precompile)
                context.EnableCodePrecompilation = false;
        }

        /// <summary>
        /// Load script from stream
        /// </summary>
        /// <param name="stream">reader from where to load it</param>
        public void Load(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            // Skip to the first symbol
            MemoryStream ms=new MemoryStream();

            do
            {
                int b = stream.ReadByte();
                if (b == -1)
                    break;
                ms.WriteByte((byte) b);

                if (b<0x80 && b!=0 && !char.IsWhiteSpace((char)b))
                {
                    if (b == '<')
                        break;

                    ms.Position = 0;
                    using (var r=new StreamReader(new ConcatStream(ms,new KeepOpenStream(stream))))
                    {
                        Load(r.ReadToEnd());
                        return;
                    }
                }

            } while (true);

            // Get a script file
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.IgnoreWhitespace = false;
            rs.ValidationType = ValidationType.None;

            ms.Position = 0;
            using (XmlReader r = XmlReader.Create(new StreamReader(new ConcatStream(ms, new KeepOpenStream(stream))), new XmlReaderSettings() { ConformanceLevel = ConformanceLevel.Fragment }))
            {
                Load(r);
            }
        }
    }

    
}