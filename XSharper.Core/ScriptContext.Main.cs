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
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace XSharper.Core
{
    /// Output type
    public enum OutputType
    {
        /// Null stream
        [Description("Null stream")]
        Nul,

        /// Null stream
        [Description("Null stream")]
        Null = Nul,

        /// Debug output
        [Description("Debug output")]
        Debug,

        /// Information output
        [Description("Information output")]
        Info,

        /// Normal output
        [Description("Normal output")]
        Out,

        /// Normal output in bold
        [Description("Normal output in bold")]
        Bold,

        /// Error output
        [Description("Error output")]
        Error,
    }

    /// Script signature verification status
    public enum ScriptSignatureStatus
    {
        /// An error occured when validating signature
        Error,

        /// Signature is valid
        Valid,

        /// Signature is invalid
        Invalid,
    }


    /// Parameters to script progress event
    [Serializable]
    public class OperationProgressEventArgs : EventArgs
    {
        /// Additional information
        public string ExtraData { get; set; }

        /// Number of percent completed (0..100)
        public int PercentCompleted { get; set; }

        /// true if script execution should be aborted
        public bool Cancel { get; set; }
    }

    /// Parameters to script output event
    [Serializable]
    public class OutputEventArgs : EventArgs
    {
        /// Output type
        public OutputType OutputType { get; set; }

        /// Text to output
        public string Text { get; set; }
    }

    
    public partial class ScriptContext : Vars, IXsContext, IWriteVerbose
    {
        private readonly CallStack _callStack = new CallStack();
        private readonly StateBag _stateBag=new StateBag();
        private readonly TypeManager _typeManager=new TypeManager();
        private readonly Dictionary<string, Assembly> _loadedCode = new Dictionary<string, Assembly>();
        private OutputType _minOutputType = OutputType.Info;
        private readonly Dictionary<OutputType, string> _redirects = new Dictionary<OutputType, string>();
        private volatile bool _abort, _abortStarted;
        private readonly Dictionary<string, EmbeddedFileInfo> _embed = new Dictionary<string, EmbeddedFileInfo>();
        private readonly PrecompiledCache _exprCache=new PrecompiledCache(100);

        #region -- Callbacks and delegates -- 

        /// Delegate that returns a value (there is no Func in .NET 2.0)
        public delegate object ScriptExecuteMethod();

        /// Handler of script progress events
        public EventHandler<OperationProgressEventArgs> Progress { get; set; }
        
        /// Handler of script output
        public EventHandler<OutputEventArgs> Output { get; set; }
        #endregion

        #region -- Properties ---
        /// Default output stream (goes to stdout usually)
        public ContextWriter Out { get; private set; }

        /// Default output stream with some visual distinction from the normal output
        public ContextWriter Bold { get; private set; }

        /// Error stream (goes to stderr usually)
        public ContextWriter Error { get; private set; }

        /// Nul stream aka /dev/nul
        public ContextWriter Nul { get; private set; }

        /// Debug info stream
        public ContextWriter Debug { get; private set; }

        /// Non critical output stream. This usually goes to stdout, but goes to nul if in quiet mode
        public ContextWriter Info { get; private set; }

        /// Input stream (usually stdin)
        public TextReader    In { get ; set;}

        /// True (default) if code precompilation is enabled, i.e. script C# code is compiled during script load phase. If false, script is compiled when first code block is found
        public bool EnableCodePrecompilation { get; set; }

        /// Current C# compiler settings
        public CSharpCompiler Compiler { get; private set; }

        /// Resource assembly, from where some embedded resources may be borrowed
        public Assembly ResourceAssembly { get; set; }

        /// ; separated list of directories to search for loaded scripts
        public string ScriptPath { get; set; }

        /// Where to save generated C# files. If default (null) - temp directory is used and files are deleted
        public string CodeOutputDirectory { get; set; }

        /// Turn on Verbose output mode, where internal script command implementation details are written to debug log
        public bool   Verbose { get; set; }
        
        /// Current exception, when in catch block
        public Exception CurrentException { get; set; }

        /// Current exception, when in catch block
        public StateBag StateBag { get { return _stateBag; } }

        /// Script call stack
        public CallStack CallStack
        {
            [DebuggerStepThrough]
            get { return _callStack; }
        }

        /// Return XSharper assembly version
        public Version CoreVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        #endregion

        #region -- Constructors --

        /// Constructor
        public ScriptContext() : this(Assembly.GetExecutingAssembly())
        {
        }

        /// Constructor
        public ScriptContext(Assembly resourceAssembly)
        {
            ResourceAssembly = resourceAssembly;
            EnableCodePrecompilation = true;

            // Check already loaded assemblies for XSharper generated code, so we don't have to compile the same thing again
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetCustomAttributes(typeof(XsHeadersIdentifierAttribute), false).Length != 0)
                {
                    _typeManager.AddAssembly(assembly,true);
                    foreach (Type t in assembly.GetExportedTypes())
                        _loadedCode[t.Name] = assembly;
                }

            resetReferences();
            Out = new ContextWriter(this, OutputType.Out);
            Info = new ContextWriter(this, OutputType.Info);
            Error = new ContextWriter(this, OutputType.Error);
            Debug = new ContextWriter(this, OutputType.Debug);
            Bold = new ContextWriter(this, OutputType.Bold);
            Nul = new ContextWriter(this, OutputType.Null);
            In = Console.In;
        }

        // Set default references
        private void resetReferences()
        {
            Compiler = new CSharpCompiler(this);
            Compiler.AddHeaders("using System;using System.Collections.Generic;using System.Collections;using System.Text;using System.IO;using System.Text.RegularExpressions;using System.Xml;using System.Globalization;");
            Compiler.AddHeaders("using XS=" + typeof(CSharpCompiler).Namespace + ";");
            foreach (string type in "System.dll;System.Xml.dll;System.Data.dll".Split(';'))
                Compiler.AddReference(type,null,false,false,null);
            Compiler.AddReference("System.Configuration.dll", null, false, false, "System.Configuration.");
            Compiler.AddReference("System.ServiceProcess.dll", null, false, false, "System.ServiceProcess.");
            Compiler.AddReference(null,typeof(ScriptContext).Assembly.FullName, true,false,null);
        }

        /// Clear all variables, exceptions, callstack and prepare context for a new script execution. Requirements and references stay as is
        public override void Clear()
        {
            base.Clear();
            _callStack.Clear();
            CurrentException = null;
            _abort = false;
           _abortStarted = false;
            resetReferences();
            _stateBag.Clear();
        }

        


        #endregion

       #region -- Script loading --

        /// <summary>
        /// Create a new script object, and load script into it from XML reader
        /// </summary>
        /// <param name="reader">XML reader</param>
        /// <param name="location">Script location</param>
        /// <returns>Loaded script</returns>
       public virtual Script LoadScript(XmlReader reader, string location)
       {
           // Read screen to RAM completely
           location = Path.GetFullPath(location);
           Script s = CreateNewScript(location);
           RunOnStack(ScriptOperation.Loading, s, delegate
           {
               s.Load(reader);
               return null;
           });
           return s;
       }

       /// <summary>
       /// Create a new script object, and load script into it from the specified location
       /// </summary>
       /// <param name="location">Script location</param>
       /// <param name="validateSignature">true if script signature must be validated</param>
       /// <returns>Loaded script</returns>
       public virtual Script LoadScript(string location, bool validateSignature)
       {
           string origLocation = location;
           if (location.StartsWith("#/", StringComparison.Ordinal))
           {
               location = "http://www.xsharper.com/lib/" + location.Substring(2);
               if (!location.EndsWith(".xsh",StringComparison.OrdinalIgnoreCase))
                   location += ".xsh";
               origLocation = location;
               validateSignature = true;
           }

           string sLoc = SearchPath(location, ".;"+ScriptPath);
           if (sLoc == null)
               sLoc = location;
           using (Stream str = OpenReadStream(sLoc))
           {
               FileStream fs = str as FileStream;
               if (fs != null)
                   return LoadScript(fs, fs.Name, validateSignature);
               Uri url;
               if (Uri.TryCreate(location, UriKind.Absolute, out url))
               {
                   string path = url.GetComponents(UriComponents.Path, UriFormat.Unescaped);
                   return loadScript(str, Path.Combine(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), Path.GetFileName(path)), origLocation, validateSignature);
               }

               return loadScript(str, sLoc, origLocation, false);
           }
       }

        
       private Script loadScript(Stream stream, string location, string origLocation, bool validateSignature)
       {
           // Read screen to RAM completely
           MemoryStream ms = new MemoryStream(Utils.ReadBytes(stream));

           location = Path.GetFullPath(location);
           Script s = CreateNewScript(location);
           
           RunOnStack(ScriptOperation.Loading, s, delegate
           {
               if (validateSignature)
               {
                   var status = VerifyScriptSignature(ms);
                   if (status == ScriptSignatureStatus.Invalid || status == ScriptSignatureStatus.Error)
                       throw new ParsingException("Script at " + origLocation + " has invalid signature");
                   ms.Position = 0;
               }

               s.Load(ms);
               return null;
           });
           Compiler.AddRequireAdmin(s.RequireAdmin);
           return s;
       }

        /// <summary>
        /// Create a new script, and initialize it from stream
        /// </summary>
        /// <param name="stream">stream from where to load XML</param>
        /// <param name="location">location</param>
        /// <param name="validateSignature">true, if signature must be validated</param>
        /// <returns>new script</returns>
       public virtual Script LoadScript(Stream stream, string location, bool validateSignature)
       {
           return loadScript(stream, location, location, validateSignature);
       }

        /// <summary>
        /// Create new script 
        /// </summary>
        /// <param name="location">location</param>
        /// <returns>new script</returns>
       public virtual Script CreateNewScript(string location)
       {
           return new Script(location);
       }
       #endregion

        #region -- Write helpers --

        /// <summary>
        /// Output with type below this is ignored
        /// </summary>
        public OutputType MinOutputType
        {
            get { return _minOutputType; }
            set { _minOutputType = value; }
        }


        /// <summary>
        /// Output value to the specified output
        /// </summary>
        /// <param name="outTo">output destination</param>
        /// <param name="value">value</param>
        public void OutTo(string outTo, object value)
        {
            outToInternal(outTo, value, true, false);
        }

        /// <summary>
        /// Output value to the specified output
        /// </summary>
        /// <param name="outTo">output destination</param>
        /// <param name="value">value</param>
        /// <param name="forceAppend">If value already exists, always append instead of overwriting</param>
        public void OutTo(string outTo, object value, bool forceAppend)
        {
            outToInternal(outTo, value, true, forceAppend);
        }

        private void outToInternal(string outTo, object value, bool allowRedirect,bool forceAppend)
        {
            if (string.IsNullOrEmpty(outTo))
                return;

            if (outTo.StartsWith("+", StringComparison.Ordinal))
            {
                outTo = outTo.Substring(1);
                forceAppend = true;
            }
            if (outTo.StartsWith("^", StringComparison.Ordinal))
            {
                outTo = outTo.Substring(1);
                if (outTo.StartsWith("#", StringComparison.Ordinal))
                    WriteText(outTo.Substring(1), value, (Encoding)null, forceAppend);
                else
                    writeInternal(Utils.To<OutputType>(outTo), (value ?? string.Empty).ToString(), allowRedirect);
            }
            else
                Set(outTo, value, forceAppend);
        }

        /// 
        public void Write(OutputType type)
        {
            return;
        }
        
        /// 
        public void Write(OutputType type, string obj)
        {
            writeInternal(type, obj, true);
        }

        private void writeInternal(OutputType type, string obj, bool allowRedirect)
        {
            if (Output != null)
            {
                string forward;
                if (_redirects.TryGetValue(type, out forward) && allowRedirect)
                {
                    outToInternal(forward, obj ?? string.Empty, false, false);
                    return;
                }

                OutputType minConsole = MinOutputType;
                if (type >= minConsole)
                    Output(this, new OutputEventArgs { OutputType = type, Text = obj ?? string.Empty });
            }
        }

        /// 
        public void Write(OutputType type, string text, params object[] p)
        {
            Write(type, string.Format(text, p));
        }

        /// 
        public void WriteException(Exception e)
        {
            bool db = (MinOutputType<=OutputType.Debug);
            string message = db ? e.ToString() : e.Message;
            WriteLine(OutputType.Error, "Error: " + message);
        }
        /// 
        public void WriteLine(OutputType type)
        {
            Write(type, Environment.NewLine);
        }
        /// 
        public void WriteLine(OutputType type, object obj)
        {
            if (obj != null)
                Write(type, obj + Environment.NewLine);
            else
                Write(type, Environment.NewLine);
        }
        /// 
        public void WriteLine(OutputType type, string text)
        {
            if (text != null)
                Write(type, text + Environment.NewLine);
            else
                Write(type, Environment.NewLine);
        }
        /// 
        public void WriteLine(OutputType type, string text, params object[] p)
        {
            Write(type, string.Format(text, p) + Environment.NewLine);
        }
        /// 
        public void Print(object obj)
        {
            Out.Print(obj);
        }
#endregion

        #region -- Write forwarders (just forwards c.Write to c.Out.Write ) --

        /// Write object to output stream
        public void Write(object obj)
        {
            Out.Write(obj);
        }

        /// Write object to output stream, followed by new line
        public void WriteLine(object obj)
        {
            Out.WriteLine(obj);
        }

        /// Write string to output stream
        public void Write(string str)
        {
            Out.Write(str);
        }

        /// Write string to output stream, followed by new line
        public void WriteLine(string str)
        {
            Out.WriteLine(str);
        }

        /// Write character to the output stream
        public void Write(char c)
        {
            Out.Write(c);
        }


        /// Write a new line to the output stream
        public void WriteLine()
        {
            Out.WriteLine();
        }


        /// Write a formatted string to the output stream, followed by new line
        public void WriteLine(string text, params object[] p)
        {
            Out.WriteLine(text, p);
        }

        /// Write a formatted string to the output stream
        public void Write(string text, params object[] p)
        {
            Out.Write(text, p);
        }

        /// Dump object to the output stream
        public void Dump(object o)
        {
            Out.Dump(o);
        }

        /// Dump object to the output stream, preceded by name=
        public void Dump(object o, string name)
        {
            Out.Dump(o, name);
        }

        /// Dump object to the output stream, treating the object as the object of the specified type
        public void Dump(object o, Type type)
        {
            Out.Dump(o, type, string.Empty);
        }

        /// Dump object to the output stream, treating the object as the object of the specified type, preceded by name=
        public void Dump(object o, Type type, string name)
        {
            Out.Dump(o, type, name);
        }

        /// Write a string to the Debug stream, if Verbose output is set
        public void WriteVerbose(string text)
        {
            if (Verbose)
                Debug.WriteLine(text);
        }
        
        
        /// Read a character from the input stream, or return -1 for EOF
        public int Read() { return In.Read(); }

        /// Peek a characted from the input stream, or return -1 for EOF
        public int Peek() { return In.Peek(); }

        /// Read a line from the input stream
        public string ReadLine() { return In.ReadLine(); }
        
        #endregion

        #region -- Progress --

        /// Execute progress handler
        public void OnProgress(int percentCompleted)
        {
            OnProgress(percentCompleted, string.Empty);
        }

        /// Execute progress handler
        public void OnProgress(int percentCompleted, string extra)
        {
            OnProgressInternal(Math.Min(percentCompleted, 100), extra);
        }
        
        /// Check for abort condition, and throw exception if the program should be cancelled now.
        public void CheckAbort()
        {
            if (_abort && !_abortStarted)
            {
                _abortStarted = true;
                throw new ScriptTerminateException("Script execution is aborted", -1, new OperationCanceledException());
            }
        }

        /// Execute progress handler
        protected void OnProgressInternal(int percentCompleted, string extra)
        {
            CheckAbort();
            if (Progress != null)
            {
                OperationProgressEventArgs args = new OperationProgressEventArgs { PercentCompleted = percentCompleted, ExtraData = extra };
                Progress(this, args);
                if (args.Cancel && !_abortStarted)
                {
                    Abort();
                    CheckAbort();
                }
            }
        }    

            
        
        #endregion 

        #region -- Execution and initialization --
        /// Execute the specified action
        [DebuggerHidden]
        public virtual object Execute(IScriptAction action)
        {
            return action != null ? RunOnStack(ScriptOperation.Executing, action, ()=>action.Execute()) : null;
        }

        /// Initialize the specified action
        [DebuggerHidden]
        public virtual void Initialize(IScriptAction action)
        {
            if (action != null)
            {
                RunOnStack(ScriptOperation.Initializing, action, delegate
                                                                     {
                                                                         action.Initialize();
                                                                         return null;
                                                                     });
                Script s = action as Script;
                if (CallStack.Count == 0 && s != null)
                {
                    if (EnableCodePrecompilation)
                        RunOnStack(ScriptOperation.Compiling, s, delegate
                                                                 {
                                                                     compile(s, false);
                                                                     return null;
                                                                 });
                }
            }
        }

        /// Initialize and then execute the specified action
        [DebuggerHidden]
        public object InitializeAndExecute(IScriptAction action)
        {
            Initialize(action);
            return Execute(action);
        }

        /// Same as <see cref="InitializeAndExecute"/>
        [DebuggerHidden]
        public object Run(IScriptAction action)
        {
            return InitializeAndExecute(action);
        }

        void compile(IScriptAction action, bool runtime)
        {
            Dictionary<string, Code> newCode = new Dictionary<string, Code>();

            walkBreadthFirst(action, obj =>
            {
                Code c = obj as Code;
                if (c != null && (runtime || !c.Dynamic) && (c as CompiledCode) == null)
                {
                    string classname = c.GetClassName();
                    if (!_loadedCode.ContainsKey(classname) && !newCode.ContainsKey(classname))
                        newCode[classname] = c;
                }
            }, false);


            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Compiler.GenerateFileHeader());
            sb.AppendLine();
            foreach (Code s in newCode.Values)
                sb.AppendLine(s.GenerateSourceCode(this, false, true));
            sb.AppendLine();

            string currentId = Compiler.GetFileHeaderCodeId();

            string code = sb.ToString();

            bool shouldCompile = (newCode.Count > 0);
            if (!shouldCompile && !Compiler.IsTrivialFileHeader())
                shouldCompile = !_typeManager.IsCompiled(currentId);

            if (shouldCompile)
            {
                WriteVerbose("Context> Starting script compilation");
                Assembly a = Compiler.Compile(CompiledOutputType.InMemoryAssembly, code, "code_" + action.Id, new CompileOptions
                    {
                        CodeOutputDirectory = CodeOutputDirectory,
                        StreamProvider = FindResourceMemoryStream
                    });
            ;
                foreach (string d in newCode.Keys)
                    _loadedCode[d] = a;
                _typeManager.AddAssembly(a,false);
            }

        }

        private static void walkBreadthFirst(IScriptAction start, Action<IScriptAction> action, bool isFind)
        {
            Queue<IScriptAction> queue = new Queue<IScriptAction>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                IScriptAction obj = queue.Dequeue();
                if (obj == null)
                    continue;

                action(obj);
                obj.ForAllChildren(delegate(IScriptAction s) { queue.Enqueue(s); return false;}, isFind);
            }
        }

        /// Execute the given script action on call stack
        protected object RunOnStack(ScriptOperation operation, IScriptAction action, ScriptExecuteMethod method)
        {
            if (action == null)
                return null;

            using (var scope = new ScriptContextScope(this))
            {
                _callStack.Push(operation, action);
                
                OnProgress(0);
                try
                {
                    // one can put a breakpoint here to be called on every method call
                    object ret = method();
                    OnProgressInternal(100, null);
                    return ret;
                }
                catch (ScriptTerminateException)
                {
                    throw;
                }
                catch (ScriptExceptionWithStackTrace)
                {
                    throw;
                }
                catch (ThreadAbortException)
                {
                    _abortStarted = true;
                    throw;
                }
                catch (Exception e)
                {
                    throw new ScriptExceptionWithStackTrace(this, e);
                }
                finally
                {
                    _callStack.Pop();
                }
            }
        }

        #endregion

        #region -- Type management --
        internal Type GetClassInstanceType(Code code)
        {
            string className = code.GetClassName();
            if (!_loadedCode.ContainsKey(className))
                RunOnStack(ScriptOperation.Compiling, code, delegate { compile(code, true);return null; });

            var type=_loadedCode[className].GetType(className);
            return type;

        }


        /// Return all known types with XsTypeAttribute
        public Type[] GetKnownTypes()
        {
            return _typeManager.GetKnownTypes();
        }

        /// Make assembly resolveable in XSharper expressions
        public void AddAssembly(Assembly assembly, bool withTypes)
        {
            _typeManager.AddAssembly(assembly,withTypes);
        }

        #endregion

        #region -- Action search by type and ID --

        /// Return action given action ID, or return null if not found
        public IScriptAction Find(string id)
        {
            return Find<IScriptAction>(id);
        }

        /// <summary>
        /// Find action by ID
        /// </summary>
        /// <param name="id">Action ID</param>
        /// <param name="throwIfNotFound">true if ScriptRuntimeException is thrown if the ID is not found</param>
        /// <returns>Found action or null, if not found</returns>
        public IScriptAction Find(string id, bool throwIfNotFound)
        {
            return Find<IScriptAction>(id,throwIfNotFound);
        }

        /// Return action given action ID. If not found returns null
        public T Find<T>(string id) where T:class,IScriptAction
        {
            return Find<T>(id, false);
        }

        /// <summary>
        /// Find action by ID
        /// </summary>
        /// <typeparam name="T">Type of action to search</typeparam>
        /// <param name="id">Action ID</param>
        /// <param name="throwIfNotFound">true if ScriptRuntimeException is thrown if the ID is not found</param>
        /// <returns>Found action or null, if not found</returns>
        public T Find<T>(string id, bool throwIfNotFound) where T : class, IScriptAction
        {
            T t=CallStack.FindTree<T>(id);
            if (throwIfNotFound && t == null)
            {
                var v=CustomAttributeHelper.First<XsTypeAttribute>(typeof(T));
                if (v==null || v.Name==null)
                    throw new ScriptRuntimeException("Action with id='" + id + "' was not found");
                throw new ScriptRuntimeException(v.Name + " with id='" + id + "' was not found");
            }
            return t;
        }

        /// <summary>
        /// Search action in the tree
        /// </summary>
        /// <param name="start">Node where to start</param>
        /// <param name="func">Predicate to execute</param>
        /// <returns>Found action or null</returns>
        static public IScriptAction FindTree(IScriptAction start, Predicate<IScriptAction> func)
        {
            Queue<IScriptAction> queue = new Queue<IScriptAction>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                IScriptAction obj = queue.Dequeue();
                if (obj != null)
                {
                    if (func(obj))
                        return obj;

                    obj.ForAllChildren(delegate(IScriptAction s)
                        {
                            queue.Enqueue(s);
                            return false;
                        }, true);
                }
            }
            return null;
        }
        #endregion
        
        #region -- XsXmlContext Members --

        /// <summary>
        /// Resolve type on top of the XML reader to a type with <see cref="XsTypeAttribute"/> attribute.
        /// </summary>
        /// <param name="reader">XML reader</param>
        /// <returns>Found type or null if not found</returns>
        public Type ResolveType(System.Xml.XmlReader reader)
        {
            return _typeManager.ResolveType(reader);
        }

        #endregion

        #region -- Dealing with embedded resources --

        /// Find a resource with the specified name, read it to memory stream and return the stream to the caller.
        public MemoryStream FindResourceMemoryStream(string streamName)
        {
            using (var str=FindResourceStream(streamName))
            {
                if (str==null)
                    return null;
                return new MemoryStream(Utils.ReadBytes(str));
            }
        }
        
        /// Find a resource with the specified name in the resource assembly or embedded resources, read it to memory stream and return the stream to the caller.
        /// Caller is responsible for closing the stream
        public Stream FindResourceStream(string streamName)
        {
            // Check embedded files
            EmbeddedFileInfo ei;
            if (_embed.TryGetValue(streamName,out ei) && ei!=null)
                return OpenReadStream(ei.Location);

            Stream s = Utils.FindResourceStream(ResourceAssembly, streamName);
            if (s != null)
                return s;

            return null;
        }

        /// <summary>
        /// Add a file to embed into the internal table
        /// </summary>
        /// <param name="streamName">Name of the embedded stream (null if file name should be used)</param>
        /// <param name="fromFile">Filename where data is located (mandatory)</param>
        /// <param name="isAssembly">true, if the stream is actually an assembly file that will be loaded</param>
        /// <returns>Name of the added stream (matches streamName)</returns>
        public string AddEmbeddedFile(string streamName, string fromFile, bool isAssembly)
        {
            if (string.IsNullOrEmpty(streamName))
            {
                if (string.IsNullOrEmpty(fromFile))   
                    throw new ArgumentNullException(fromFile,"File location cannot be null");
                FileInfo fi=new FileInfo(fromFile);
                if (!fi.Exists)
                    throw new FileNotFoundException("File not found",fromFile);
                streamName = fi.Name;
            }
            _embed.Add(streamName,new EmbeddedFileInfo { IsAssembly = isAssembly, Location = fromFile, StreamName = streamName});
            return streamName;
        }

        /// Get array of embedded files
        public EmbeddedFileInfo[] GetFilesToEmbed()
        {
            return new List<EmbeddedFileInfo>(_embed.Values).ToArray();
        }

        /// Get list of embedded streams URIs. For example, for embedded stream "AAA.BBB" the URI will be "embed:///AAA.BBB"
        public string[] GetEmbeddedResources()
        {
            Dictionary<string, bool> s = new Dictionary<string, bool>();
            foreach (var name in ResourceAssembly.GetManifestResourceNames())
                s[name] = true;
            foreach (var n in _embed)
                s[n.Key] = true;
            List<string> ret = new List<string>();
            foreach (var pair in s)
                ret.Add("embed:///" + pair.Key);
            return ret.ToArray();
        }
        #endregion

        #region -- Script execution --

        /// <summary>
        /// Execute script with default isolation
        /// </summary>
        /// <param name="script">script to execute</param>
        /// <param name="args">script parameters</param>
        /// <returns>script return value</returns>
        public object ExecuteScript(Script script, IEnumerable<string> args)
        {
            return ExecuteScript(script, args, CallIsolation.Default);
        }

        /// <summary>
        /// Execute script
        /// </summary>
        /// <param name="script">script to execute</param>
        /// <param name="args">script parameters</param>
        /// <param name="isolation">isolation</param>
        /// <returns>script return value</returns>
        public object ExecuteScript(Script script, IEnumerable<string> args, CallIsolation isolation)
        {
            string location = script.Location;
            if (!string.IsNullOrEmpty(script.Id))
                location += "#" + script.Id;
            WriteVerbose(string.Format("Context> Executing script {0} with isolation {1} ", location, isolation));

            Vars snapshot = null;
            if (isolation != CallIsolation.None)
                snapshot = new Vars(this);
            try
            {
                // Clear vars
                if (isolation == CallIsolation.High)
                    base.Clear();

                
                // Print usage
                object ret = RunOnStack(ScriptOperation.ParsingArguments, script,
                                        delegate
                                            {
                                                script.ParseArguments(args);
                                                if (script.Usage.ShouldDisplayUsage(args, this))
                                                {
                                                    Write(OutputType.Out, GetAutoUsage(script, -1));
                                                    return script.Usage.ExitCode;
                                                }
                                                return null;
                                            });

                if (ret == null)
                    ret = RunOnStack(ScriptOperation.Executing, script, script.ExecuteScript);
                return ReturnValue.Unwrap(ret);
            }
            finally
            {
                if (snapshot != null)
                {
                    base.Clear();
                    AddRange(snapshot);
                }
            }
        }

        ///<summary>Set variables specified in the sv parameter, execute a certain action, then restore the set variables back.</summary>
        ///<param name="method">Delegate to execute</param>
        ///<param name="sv">Variables to set/restore before/after execution</param>
        ///<param name="prefix">Add a given prefix to the variable names.</param>
        ///<returns></returns>
        public object ExecuteWithVars(ScriptExecuteMethod method, Vars sv, string prefix)
        {
            var save = new Dictionary<string, object>();
            var delete = new List<string>();
            try
            {
                foreach (var v in sv)
                {
                    string vn = prefix+v.Name;
                    object o = GetOrDefault(vn, null);
                    if (o == null && !IsSet(vn))
                        delete.Add(v.Name);
                    else
                        save[v.Name] = o;

                    this[vn] = v.Value;
                }
                return method();
            }
            finally
            {
                foreach (var savePair in save)
                {
                    string vn = prefix + savePair.Key;
                    if (IsSet(vn))
                        sv[savePair.Key] = Get(vn);
                    else
                        sv.Remove(savePair.Key);
                    this[vn] = savePair.Value;
                }
                foreach (var d in delete)
                {
                    string vn = prefix + d;
                    if (IsSet(vn))
                        sv[d] = Get(vn);
                    else
                        sv.Remove(d);
                    Remove(vn);
                }
            }
        }


        /// <summary>
        /// Execute script action
        /// </summary>
        /// <param name="action">action to execute</param>
        /// <param name="args">list of parameters, in case the action is Call</param>
        /// <param name="isolation">isolation</param>
        /// <returns>action result</returns>
        public object ExecuteAction(IScriptAction action, IEnumerable<CallParam> args, CallIsolation isolation)
        {
            Vars snapshot = null;
            if (isolation!=CallIsolation.None)
                snapshot = new Vars(this);
            try
            {
                // Clear vars
                if (isolation == CallIsolation.High)
                    base.Clear();


                return RunOnStack(ScriptOperation.Executing, action, delegate
                                                                  {
                                                                      Sub sub = action as Sub;
                                                                      if (sub != null)
                                                                          return sub.ExecuteSub(args);
                                                                      return action.Execute();
                                                                  });
            }
            finally
            {
                if (snapshot != null)
                {
                    base.Clear();
                    AddRange(snapshot);
                }
            }
        }

        /// <summary>
        /// Abort script execution. As soon as scripting engine gets execution (new action is executed or progress of the currently going action is updated
        /// the script execution will stop and <see cref="ScriptTerminateException"/> will be thrown
        /// </summary>
        public void Abort()
        {
            _abort = true;
        }

        /// Reset internal abort flag. 
        public void ResetAbort()
        {
            _abort =_abortStarted= false;
        }

        #endregion

        #region -- Output redirection --

        /// Save current script redirection information to an opaque object
        public object SaveRedirect()
        {
            return new Dictionary<OutputType, string>(_redirects);
        }

        /// Restore current script redirection information from an opaque object, previously created by <see cref="SaveRedirect"/>
        public void RestoreRedirect(object redirect)
        {
            _redirects.Clear();
            foreach (var pair in (Dictionary<OutputType,string>)redirect)
                _redirects.Add(pair.Key,pair.Value);
        }

        /// Redirect output type to the specified destination
        public void AddRedirect(OutputType outputType, string redirectTo)
        {
            if (redirectTo == null)
                _redirects.Remove(outputType);
            else
            {
                if (redirectTo.StartsWith("^", StringComparison.Ordinal))
                {
                    string s = redirectTo.Substring(1);

                    // If stream name starts with # treat as file
                    if (s.StartsWith("#", StringComparison.Ordinal))
                    {
                        // ^#data.txt => Overwrite data.txt. ^#+data.txt => Append to data.txt
                        if (!s.StartsWith("#+", StringComparison.Ordinal))
                        {
                            WriteText(s.Substring(1), null);
                            redirectTo = "^#+" + s.Substring(1);
                        }
                    }
                    else 
                    {
                        var v = Utils.To<OutputType>(s);
                        if (v==outputType)
                            return;
                        string s1;
                        if (_redirects.TryGetValue(v, out s1))
                            redirectTo = s1;
                        
                    }
                }
                _redirects[outputType] = redirectTo;
            }
                
            writeInternal(outputType, string.Empty,true);
        }
        #endregion 

        #region -- Variable expansion --
        ///<summary>Transform variable</summary>
        ///<param name="source">Original variable value</param>
        ///<param name="rules">Transformation rules (<see cref="TransformRules"/></param>
        ///<returns>Transformed object</returns>
        public object Transform(object source, TransformRules rules)
        {
            object s = source;
            if (source != null && rules != TransformRules.None)
            {
                Type t = source.GetType();
                if (t == typeof (Nullable<>))
                    t = Nullable.GetUnderlyingType(t);
                if (t.IsPrimitive || t == typeof (Guid) || t==typeof(decimal))
                    rules &= ~(TransformRules.ExpandMask | TransformRules.TrimMask);

                if ((rules & TransformRules.ExpandAfterTrim) == TransformRules.ExpandAfterTrim)
                {
                    s = Utils.TransformStr(Utils.To<string>(s), rules & TransformRules.TrimMask);
                    rules = (rules & ~TransformRules.ExpandAfterTrim & ~TransformRules.TrimMask) | TransformRules.Expand;
                }
                if ((rules & TransformRules.Expand) != 0)
                    s = expandVars(rules, Utils.To<string>(s));
                if ((rules & TransformRules.ExpandReplaceOnly) == TransformRules.ExpandReplaceOnly)
                    rules &= ~TransformRules.ReplaceMask;
                if ((rules & TransformRules.ExpandTrimOnly) == TransformRules.ExpandTrimOnly)
                    rules &= ~TransformRules.TrimMask;
                if ((rules & ~TransformRules.ExpandMask) != TransformRules.None)
                    s = Utils.TransformStr(Utils.To<string>(s), rules & ~TransformRules.Expand);
            }
            return s;
        }
        ///<summary>Transform string to another string</summary>
        ///<param name="source">Original variable value</param>
        ///<param name="rules">Transformation rules (<see cref="TransformRules"/></param>
        ///<returns>Transformed string</returns>
        public string TransformStr(string source, TransformRules rules)
        {
            return Utils.To<string>(Transform(source,rules));
        }

        /// Expand ${} variables 
        public object Expand(object arguments)
        {
            return Transform(arguments, TransformRules.Expand);
        }

        /// Expand ${} variables 
        public string ExpandStr(object arguments)
        {
            return Utils.To<string>(Expand(arguments));
        }

        /// <summary>
        /// Verify that transformation is correct
        /// </summary>
        /// <param name="text">Text to verify</param>
        /// <param name="expand">Rules</param>
        public void AssertGoodTransform(string text, TransformRules expand)
        {
            if (String.IsNullOrEmpty(text))
                return;

            if ((expand & TransformRules.Expand) == TransformRules.Expand && text.Contains("${"))
            {
                if (!text.Contains("}"))
                    throw new ParsingException("Closing } not found in '" + text + "'");
            }
            if ((expand & TransformRules.ExpandDual) == TransformRules.ExpandDual && text.Contains("${{"))
            {
                if (!text.Contains("}}"))
                    throw new ParsingException("Closing }} not found in '" + text + "'");
            }
            if ((expand & TransformRules.ExpandDualSquare) == TransformRules.ExpandDualSquare && text.Contains("[["))
            {
                if (!text.Contains("]]"))
                    throw new ParsingException("Closing ]] not found in '" + text + "'");
            }
            if ((expand & TransformRules.ExpandSquare) == TransformRules.ExpandDualSquare && text.Contains("["))
            {
                if (!text.Contains("]"))
                    throw new ParsingException("Closing ] not found in '" + text + "'");
            }
        }

        /// <summary>
        /// Check if text is expression that needs to be calculated if transformed according to the expansion rules
        /// </summary>
        /// <param name="text">Text to verify</param>
        /// <param name="expand">Rules</param>
        /// <returns>true if text contains expressions</returns>
        public bool ContainsExpressions(string text, TransformRules expand)
        {
            if (String.IsNullOrEmpty(text))
                return false;
            if ((expand & TransformRules.Expand) == TransformRules.Expand)
                return text.Contains("${");
            if ((expand & TransformRules.ExpandDual) == TransformRules.ExpandDual)
                return text.Contains("${{");
            if ((expand & TransformRules.ExpandDualSquare) == TransformRules.ExpandDualSquare)
                return text.Contains("[[");
            if ((expand & TransformRules.ExpandSquare) == TransformRules.ExpandSquare)
                return text.Contains("[");
            return false;
        }
        private object expandVars(TransformRules rules, string s)
        {
            string begin;
            string end;
            if ((rules & TransformRules.ExpandDual) == TransformRules.ExpandDual)
            {
                begin = "${{";
                end = "}}";
            }
            else if ((rules & TransformRules.ExpandDualSquare) == TransformRules.ExpandDualSquare)
            {
                begin = "[[";
                end = "]]";
            }
            else if ((rules & TransformRules.ExpandSquare) == TransformRules.ExpandSquare)
            {
                begin = "[";
                end = "]";
            }
            else if ((rules & TransformRules.Expand) == TransformRules.Expand)
            {
                begin = "${";
                end = "}";
            }
            else
                return s;

            if (s.IndexOf(begin,StringComparison.Ordinal) != -1) 
            {
                StringBuilder sbNew = new StringBuilder();
                using (var sr = new ParsingReader(new StringReader(s)))
                {
                    int ptr = 0;
                    bool first=true;
                    while (!sr.IsEOF)
                    {
                        char ch = (char)sr.Read();
                        if (ch!=begin[ptr])    
                        {
                            sbNew.Append(begin,0,ptr);
                            sbNew.Append(ch);
                            ptr = 0;
                            first = false;
                            continue;
                        }
                        ptr++;
                        if (ptr < begin.Length)
                            continue;
                        if (sr.Peek()=='{' || sr.Peek()=='[')
                        {
                            sbNew.Append(begin);
                            ptr = 0;
                            first = false;
                            continue;
                        }
                        // 
                        object sv = EvalMulti(sr);
                        sv = ((rules & TransformRules.ExpandTrimOnly) == TransformRules.ExpandTrimOnly)
                                 ? Utils.TransformStr(Utils.To<string>(sv), rules & TransformRules.TrimMask)
                                 : sv;
                        sv = ((rules & TransformRules.ExpandReplaceOnly) == TransformRules.ExpandReplaceOnly)
                                 ? Utils.TransformStr(Utils.To<string>(sv), rules & TransformRules.ReplaceMask)
                                 : sv;

                        // Now read the trailing stuff
                        sr.SkipWhiteSpace();
                        for (ptr = 0; ptr < end.Length; ++ptr)
                            sr.ReadAndThrowIfNot(end[ptr]);
                        if (sr.IsEOF && first)
                            return sv;
                        ptr = 0;
                        first = false;
                        sbNew.Append(Utils.To<string>(sv));
                    }
                    for (int i = 0; i < ptr; ++i)
                        sbNew.Append(begin[i]);
                }
                
                
                return sbNew.ToString();
            }
            return s;
        }
        #endregion

    }

}