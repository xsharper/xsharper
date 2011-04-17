<?xml version="1.0" encoding="utf-8"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0">

	<reference from="${%programfiles%}\Reference Assemblies\Microsoft\WindowsPowerShell\v1.0\System.Management.Automation.dll" WithTypes="true" />
	<?ht using System.Management.Automation;
		using System.Management.Automation.Host;
		using System.Management.Automation.Runspaces;
		using System.Collections.ObjectModel;

	    [XS.XsType("powershell")]
	    public class PowerShell : XSharper.Core.StaticValueFromFileBase
	    {
	        /// Where to output the subroutine value. 
	        public string OutTo { get; set; }

	        /// Input collection 
	        public object Input { get; set; }

	        /// Call isolation
	        public XS.CallIsolation Isolation { get; set; }
            	       
	        public PowerShell()
	        {
	            Isolation = XS.CallIsolation.High;
	        }

	        public override object Execute()
	        {
	            object x=base.Execute();
	            if (x != null)
	                return x;

	            var s = Context.Transform(Input, Transform);
	                if (s != null && !(s is System.Collections.IEnumerable))
	                    s = new[] {s};
	                
	            object ret = execInternal(GetTransformedValueStr(), s, (Isolation == XS.CallIsolation.High)?null:Context);
	            Context.OutTo(OutTo, ret);
	            return null;
	        }
	        static public object Exec(string script)
	        {
	            return execInternal(script, null, null);
	        }
	        static public object Exec(string script, object input)
	        {
	            return execInternal(script, input, null);
	        }

	        static private object execInternal(string script, object input, IEnumerable<XS.Var> vars)
	        {
	        
	            MyHost host = new MyHost(XS.ScriptContextScope.Current);
	            using (var runspace = RunspaceFactory.CreateRunspace(host))
	            {
	                runspace.Open();
	                if (vars!=null)
	                    foreach (XS.Var v in vars)
	                        runspace.SessionStateProxy.SetVariable(v.Name, v.Value);

	                var ie = input as System.Collections.IEnumerable;
	                using (Pipeline pipeline = runspace.CreatePipeline())
	                {
	                    pipeline.Commands.AddScript(script);
	                    return (ie == null) ? pipeline.Invoke() : pipeline.Invoke(ie);
	                }
	            }
	        }

	        #region -- internal --
	        class MyHost : PSHost
	        {
	            public MyHost(XS.ScriptContext c) { _c = c; _ui = new UserInterface(c); }

	            public override void SetShouldExit(int exitCode) { throw new XS.ScriptTerminateException(exitCode); }
	            public override void EnterNestedPrompt() { throw new NotImplementedException(); }
	            public override void ExitNestedPrompt() { throw new NotImplementedException(); }
	            public override void NotifyBeginApplication() { }
	            public override void NotifyEndApplication() { }
	            public override string Name { get { return (_c.Script == null || string.IsNullOrEmpty(_c.Script.Id)) ? "XSharper" : _c.Script.Id; } }
	            public override Version Version
	            {
	                get
	                {
	                    if (_c.Script != null && _c.Script.VersionInfo != null)
	                    {
	                        var s = _c.TransformStr(_c.Script.VersionInfo.Version, _c.Script.VersionInfo.Transform);
	                        if (!string.IsNullOrEmpty(s))
	                            return new Version(s);
	                    }
	                    return new Version(0, 0, 0, 0);

	                }
	            }
	            public override Guid InstanceId { get { return _myId; } }
	            public override PSHostUserInterface UI { get { return _ui; } }
	            public override CultureInfo CurrentCulture { get { return _originalCultureInfo; } }
	            public override CultureInfo CurrentUICulture { get { return _originalUICultureInfo; } }

	            private readonly XS.ScriptContext _c;
	            private readonly CultureInfo _originalCultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
	            private readonly CultureInfo _originalUICultureInfo = System.Threading.Thread.CurrentThread.CurrentUICulture;
	            private readonly Guid _myId = Guid.NewGuid();
	            private readonly PSHostUserInterface _ui;
	        }

	        class RawUi : PSHostRawUserInterface
	        {
	            private XS.ScriptContext _c;
	            public RawUi(XS.ScriptContext scriptContext) { _c = scriptContext; }
	            public override KeyInfo ReadKey(ReadKeyOptions options)
	            {
	                if (!Console.KeyAvailable)
	                {
	                    _c.CheckAbort();
	                    System.Threading.Thread.Sleep(10);
	                }
	                ConsoleKeyInfo k = Console.ReadKey((options & ReadKeyOptions.NoEcho) == ReadKeyOptions.NoEcho);
	                ControlKeyStates ks = 0;

	                if ((k.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt)
	                    ks |= ControlKeyStates.LeftAltPressed;
	                if ((k.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control)
	                    ks |= ControlKeyStates.LeftAltPressed;
	                if ((k.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift)
	                    ks |= ControlKeyStates.ShiftPressed;
	                if (Console.CapsLock)
	                    ks |= ControlKeyStates.CapsLockOn;
	                return new KeyInfo((int)k.Key, k.KeyChar, ks, false);
	            }

	            public override void FlushInputBuffer() { }
	            public override void SetBufferContents(Coordinates origin, BufferCell[,] contents) { throw new NotImplementedException(); }
	            public override void SetBufferContents(Rectangle rectangle, BufferCell fill) { throw new NotImplementedException(); }
	            public override BufferCell[,] GetBufferContents(Rectangle rectangle) { throw new NotImplementedException(); }
	            public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill) { throw new NotImplementedException(); }
	            public override ConsoleColor ForegroundColor { get { return Console.ForegroundColor; } set { } }
	            public override ConsoleColor BackgroundColor { get { return Console.BackgroundColor; } set { } }
	            public override Coordinates CursorPosition { get { return new Coordinates(Console.CursorLeft, Console.CursorTop); } set { } }
	            public override Coordinates WindowPosition { get { return new Coordinates(Console.WindowLeft, Console.WindowTop); } set { } }
	            public override int CursorSize { get { return Console.CursorSize; } set { Console.CursorSize = value; } }
	            public override Size BufferSize
	            {
	                get
	                {
	                    return new Size(XS.Utils.HasRealConsole ? Console.BufferWidth : 120, XS.Utils.HasRealConsole ? Console.BufferHeight : 60);
	                }
	                set
	                {
	                    Console.BufferHeight = value.Height;
	                    Console.BufferWidth = value.Width;
	                }
	            }
	            public override Size WindowSize
	            {
	                get { return new Size(Console.WindowWidth, Console.WindowHeight); }
	                set
	                {
	                    Console.WindowHeight = value.Height;
	                    Console.WindowWidth = value.Width;
	                }
	            }
	            public override Size MaxWindowSize { get { return new Size(Console.LargestWindowWidth, Console.LargestWindowHeight); } }
	            public override Size MaxPhysicalWindowSize { get { return MaxWindowSize; } }
	            public override bool KeyAvailable { get { return Console.KeyAvailable; } }
	            public override string WindowTitle { get { return Console.Title; } set { Console.Title = WindowTitle; } }
	        }
	        class UserInterface : PSHostUserInterface
	        {
	            private PSHostRawUserInterface _rawUi;
	            private XS.ScriptContext _c;

	            public UserInterface(XS.ScriptContext scriptContext)
	            {
	                _c = scriptContext;
	                _rawUi = new RawUi(scriptContext);
	            }

	            public override string ReadLine()
	            {
	                return XS.Utils.HasRealConsole ? Console.ReadLine() : null;
	            }

	            public override System.Security.SecureString ReadLineAsSecureString()
	            {
	                if (!XS.Utils.HasRealConsole)
	                    return null;
	                System.Security.SecureString r = new System.Security.SecureString();

	                while (true)
	                {
	                    while (!Console.KeyAvailable)
	                    {
	                        _c.CheckAbort();
	                        System.Threading.Thread.Sleep(10);
	                    }
	                    ConsoleKeyInfo info = Console.ReadKey(true);
	                    if (info.Key == ConsoleKey.Enter)
	                        break;
	                    if (info.Key != ConsoleKey.Backspace)
	                    {
	                        r.AppendChar(info.KeyChar);
	                        _c.Write('*');
	                    }
	                    else
	                    {
	                        if (r.Length > 0)
	                        {
	                            r.RemoveAt(r.Length - 1);
	                            _c.Write("\b \b");
	                        }
	                    }
	                }
	                _c.WriteLine();
	                return r;

	            }

	            public override void Write(string value) { _c.Write(value); }
	            public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value) { _c.Write(value); }
	            public override void WriteLine(string value) { _c.WriteLine(value); }
	            public override void WriteErrorLine(string value) { _c.Error.WriteLine(value); }
	            public override void WriteDebugLine(string message) { _c.Debug.WriteLine(message); }
	            public override void WriteProgress(long sourceId, ProgressRecord record) { _c.OnProgress(record.PercentComplete, record.CurrentOperation); }
	            public override void WriteVerboseLine(string message) { _c.Info.WriteLine(message); }
	            public override void WriteWarningLine(string message) { _c.Info.WriteLine(message); }
	            public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions) { throw new NotImplementedException(); }
	            public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName) { throw new NotImplementedException(); }
	            public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options) { throw new NotImplementedException(); }
	            public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice) { throw new NotImplementedException(); }
	            public override PSHostRawUserInterface RawUI { get { return _rawUi; } }
	        }
	 
	        #endregion
	    }
	?>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>y2gi6OHlv0v4RSBzjnaAtaGQayw=</DigestValue></Reference></SignedInfo><SignatureValue>MzxaVYPWLLVQxeiWblexUM9H06PuagMbx/8GgUW969A9da0KbjZeC6bvyP4jNtHsHFFcDUWTNL4D51mRmkgZZyaqyhglbnfb7qsKvXDcbgUCtytRBWYDY3ANH61/l2gSSy8nzrB7lMCJH5ZcHWxr+YVsSvsebs3p2u0uJ3ZtwaI=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>    