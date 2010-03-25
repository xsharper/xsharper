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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace XSharper.Core
{
    /// <summary>
    /// How to execute shell command
    /// </summary>
    public enum ShellMode
    {
        /// Autodetect
        [Description("Autodetect")]
        Auto,

        /// Default (Autodetect)
        [Description("Default (Autodetect)")]
        Default=Auto,

        /// Using CMD.EXE
        [Description("Using CMD.EXE")]
        Comspec,

        /// Using CMD.EXE
        [Description("Write a batch file and run CMD.EXE on it")]
        Batch,

        /// Run the executable directly with CreateProcess
        [Description("Run the executable directly with CreateProcess")]
        Direct,

        /// Using ShellExecute
        [Description("Using ShellExecute")]
        ShellExecute
    }

    /// <summary>
    /// Execute shell command
    /// </summary>
    [XsType("shell", ScriptActionBase.XSharperNamespace)]
    [Description("Execute shell command")]
    public class Shell : ValueBase
    {
        /// do not throw exception if exit code is not zero
        [Description("do not throw exception if exit code is not zero")]
        public bool IgnoreExitCode { get; set;}

        /// Where to save exit code 
        [Description("Where to save exit code ")]
        public string ExitCodeTo { get; set; }

        /// Input stream redirection
        [Description("Input stream redirection")]
        public string Input { get; set; }

        /// Output stream redirection
        [Description("Output stream redirection")]
        public string OutTo { get; set; }

        /// Whether the program outputs binary data instead of strings
        [Description("Whether the program outputs binary data instead of strings")]
        public bool BinaryOutput { get; set; }

        /// Error stream redirection
        [Description("Error stream redirection")]
        public string ErrorTo { get; set; }

        /// Execution timeout
        [Description("Execution timeout")]
        public string Timeout { get; set; }

        /// Verb, for shellExecute
        [Description("Verb, for shellExecute")]
        public string Verb { get; set; }

        /// true, if no window should be created. Default: false
        [Description("true, if no window should be created")]
        public bool CreateNoWindow { get; set; }

        /// Window style of the process. Default: normal
        [Description("Window style of the process. Default: normal")]
        public ProcessWindowStyle WindowStyle { get; set; }

        /// true (default) if must wait until the program terminates
        [Description("true (default) if must wait until the program terminates")]
        public bool Wait { get; set; }

        /// Execution method
        [Description("Execution mode")]
        [XsAttribute("mode")]
        [XsAttribute("shellMode")]
        public ShellMode Mode { get; set; }

        /// List of arguments
        [Description("List of arguments")]
        [XsElement("", SkipIfEmpty = true, CollectionItemElementName = "param", CollectionItemType = typeof(ShellArg))]
        public List<ShellArg> Args { get; set; }

        /// Output encoding
        [Description("Output encoding")]
        public string Encoding { get; set; }

        /// Default directory
        [Description("Default directory")]
        public string Directory { get; set; }

        /// Constructor
        public Shell()
        {
            Args = new List<ShellArg>();
            WindowStyle = ProcessWindowStyle.Normal;
            Wait = true;
            Mode = ShellMode.Auto;
        }

        /// Constructor
        public Shell(string cmd) : this()
        {
            Value = cmd;
        }

        class Redir : IDisposable
        {
            private readonly ScriptContext _context;
            private string _outTo;

            private StringBuilder _sbTmp = new StringBuilder();
            private MemoryStream _ms=new MemoryStream();
            private AutoResetEvent _dataReady = new AutoResetEvent(false);
            private AsyncReader _reader;
            private bool _binary;

            public Redir(ScriptContext context, string outp)
            {
                _context = context;
                _outTo = outp;
                if (!string.IsNullOrEmpty(_outTo))
                    context.OutTo(_outTo, string.Empty);
            }
            public void StartRedirect(Stream stream, Encoding encoding)
            {
                _binary = false;
                _reader = new AsyncReader(stream, encoding, dataReceiver);
            }
            public void StartRedirect(Stream stream)
            {
                _binary = true;
                _reader = new AsyncReader(stream, binaryReceiver);
            }


            ~Redir()
            {
                Dispose(false);
            }

            protected virtual void Dispose(bool dispose)
            {
                if (dispose && _dataReady!=null)
                {
                    if (_reader != null)
                        _reader.Dispose();
                    _dataReady.Close();
                }
                _dataReady = null;
                _reader = null;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }


            public WaitHandle Event
            {
                get { return _dataReady; }
            }

            private void dataReceiver(string d)
            {
                if (d != null)
                {
                    lock (_sbTmp)
                    {
                        _sbTmp.Append(d);
                        _dataReady.Set();
                    }
                }
            }
            private void binaryReceiver(byte[] dataBinary, int bytes)
            {
                lock (_ms)
                {
                    _ms.Write(dataBinary,0,bytes);
                }
            }
            public void Flush(bool final)
            {
                if (final && _reader!=null)
                    _reader.WaitEof(5000);
                // Dump the rest of the output to variables
                if (!string.IsNullOrEmpty(_outTo))
                {

                    if (_binary)
                    {
                        if (final)
                            lock (_ms)
                            {
                                _context.OutTo(_outTo, _ms.ToArray());
                            }
                    }
                    else
                        lock (_sbTmp)
                        {
                            string s = _sbTmp.ToString();
                            _context.OutTo(_outTo, s, true);
                            _sbTmp.Length = 0;
                        }
                }

            }
        }

        class ExitedWithContext
        {
            private EventWaitHandle  _terminated;
            public ExitedWithContext(EventWaitHandle terminated)
            {
                _terminated = terminated;
            }
            public void onExited(object sender, EventArgs e)
            {
                _terminated.Set();
            }
        }

        /// Execute action
        public override object Execute()
        {
            string args = (GetTransformedValueStr()??string.Empty).Trim();

            ShellMode t = this.Mode;
            if (t == ShellMode.Auto)
                t = string.IsNullOrEmpty(Verb) ? ShellMode.Comspec : ShellMode.ShellExecute;

            string outp = Context.TransformStr(OutTo, Transform);
            string errorp = Context.TransformStr(ErrorTo, Transform);
            string enc = Context.TransformStr(Encoding, Transform);
            object input = Context.Transform(Input, Transform);

            bool redirectError = !string.IsNullOrEmpty(errorp);
            bool redirectOutput = !string.IsNullOrEmpty(outp);

            ProcessStartInfo pi = new ProcessStartInfo();
            if (!string.IsNullOrEmpty(enc))
                pi.StandardOutputEncoding = pi.StandardErrorEncoding = Utils.GetEncoding(enc);
            pi.CreateNoWindow = CreateNoWindow;
            pi.WindowStyle = WindowStyle;
            pi.ErrorDialog = false;

            pi.WorkingDirectory = Context.TransformStr(Directory, Transform);
            if (string.IsNullOrEmpty(pi.WorkingDirectory))
                pi.WorkingDirectory = Environment.CurrentDirectory;

            string fileToDelete = null;
            
            
            // do a bit of clean up occasionally by deleting older xsh-batch-*.cmd files
            if (Utils.GenRandom(0,5)==3)
            {
                try
                {
                    VerboseMessage("Cleaning up older batch files in temp directory");
                    var dt = DateTime.UtcNow.AddDays(-1);
                    foreach (var file in new DirectoryInfo(Path.GetTempPath()).GetFileSystemInfos("xsh-batch-*.cmd"))
                    {
                        if (file.LastWriteTimeUtc<dt)
                            file.Delete();
                    }
                }
                catch
                {
                    
                }
            }
            try
            {
                switch (t)
                {
                    case ShellMode.Batch:
                        fileToDelete = Path.Combine(Path.GetTempPath(), "xsh-batch-" +Utils.GenRandom(0, int.MaxValue) + "-" + Utils.GenRandom(0, int.MaxValue) + ".cmd");
                        File.WriteAllText(fileToDelete, args, System.Text.Encoding.Default);
                        args = Utils.QuoteArg(fileToDelete);
                        if (!Wait)
                            fileToDelete = null; // don't delete the batch if we're not waiting for it to complete
                        goto default;
                    default:
                        string cmd = Utils.QuoteArg(Environment.ExpandEnvironmentVariables("%COMSPEC%")) + " "+args;
                        fillFilenameAndArguments(pi, cmd);
                        pi.Arguments = " /C \"" + pi.Arguments + "\"";
                        pi.UseShellExecute = false;
                        pi.RedirectStandardError = redirectError && Wait;
                        pi.RedirectStandardOutput = redirectOutput && Wait;
                        pi.RedirectStandardInput = (input != null) && Wait;
                        break;
                    case ShellMode.Direct:
                        fillFilenameAndArguments(pi, args);
                        pi.UseShellExecute = false;
                        pi.RedirectStandardError = redirectError && Wait;
                        pi.RedirectStandardOutput = redirectOutput && Wait;
                        pi.RedirectStandardInput = (input != null) && Wait;
                        break;
                    case ShellMode.ShellExecute:
                        pi.UseShellExecute = true;
                        fillFilenameAndArguments(pi, args);
                        if (!string.IsNullOrEmpty(Verb))
                            pi.Verb = Verb;
                        break;
                }


                TimeSpan? ts = null;
                if (!string.IsNullOrEmpty(Timeout))
                    ts = Utils.ToTimeSpan(Context.TransformStr(Timeout, Transform));
                VerboseMessage("Executing " + Utils.QuoteArg(pi.FileName) + " " + pi.Arguments);

                using (ManualResetEvent terminated = new ManualResetEvent(false))
                using (WaitableTimer timeout = new WaitableTimer(ts))
                {
                    ExitedWithContext ect = new ExitedWithContext(terminated);
                    using (Process p = Process.Start(pi))
                    {
                        if (Wait && p != null)
                        {
                            Redir[] r = new Redir[2] {new Redir(Context, outp), new Redir(Context, errorp)};

                            p.Exited += ect.onExited;
                            p.EnableRaisingEvents = true;

                            try
                            {
                                AsyncWriter asyncWriter = null;
                                if (pi.RedirectStandardInput)
                                {
                                    byte[] data;
                                    if (input is byte[])
                                        data = ((byte[]) input);
                                    else
                                    {
                                        Encoding en = System.Text.Encoding.Default;
                                        if (string.IsNullOrEmpty(enc))
                                            en = Utils.GetEncoding(enc);
                                        if (en == null)
                                            en = System.Text.Encoding.Default;
                                        data = en.GetBytes(Utils.To<string>(input));
                                    }
                                    asyncWriter = new AsyncWriter(p.StandardInput, data);

                                }

                                if (pi.RedirectStandardOutput)
                                    if (BinaryOutput)
                                        r[0].StartRedirect(p.StandardOutput.BaseStream);
                                    else
                                        r[0].StartRedirect(p.StandardOutput.BaseStream, p.StandardOutput.CurrentEncoding);
                                if (pi.RedirectStandardError)
                                    r[1].StartRedirect(p.StandardError.BaseStream, p.StandardError.CurrentEncoding);


                                var wh = new WaitHandle[] {r[0].Event, r[1].Event, terminated, timeout.WaitHandle};
                                do
                                {
                                    Context.OnProgress(1);
                                    int n = WaitHandle.WaitAny(wh, 500,true);

                                    switch (n)
                                    {
                                        case 0:
                                        case 1:
                                            r[n].Flush(false);
                                            break;
                                        case 2:
                                            break; // Exit
                                        case 3:
                                            throw new TimeoutException("Command execution timed out");
                                    }
                                } while (!p.HasExited);

                                // must wait to ensure that all output is flushed
                                p.WaitForExit();

                            }
                            finally
                            {

                                try
                                {
                                    if (!p.HasExited && !p.WaitForExit(1000))
                                    {
                                        VerboseMessage("Process didn't terminate as expected. TASKKILL will be used.");
                                        Shell sh = new Shell(Utils.BackslashAdd(Environment.GetFolderPath(Environment.SpecialFolder.System)) + "TASKKILL.EXE /T /F /pid " + p.Id);
                                        sh.CreateNoWindow = true;
                                        sh.Mode = ShellMode.Comspec;
                                        sh.IgnoreExitCode = true;
                                        Context.InitializeAndExecute(sh);
                                        VerboseMessage("TASKKILL completed");
                                    }

                                    VerboseMessage("Waiting for process to terminate completely.");
                                    p.WaitForExit();
                                }
                                catch
                                {
                                    VerboseMessage("Failed to wait until the process is terminated");
                                }

                                try
                                {
                                    if (!p.HasExited)
                                        p.Kill();
                                }
                                catch
                                {
                                    VerboseMessage("Kill failed");
                                }
                                r[0].Flush(true);
                                r[1].Flush(true);
                                r[0].Dispose();
                                r[1].Dispose();

                                terminated.WaitOne(1000, false);

                                // Restore
                                p.Exited -= ect.onExited;
                            }


                            int exitCode = p.ExitCode;
                            if (!string.IsNullOrEmpty(ExitCodeTo))
                                Context.OutTo(Context.TransformStr(ExitCodeTo, Transform), exitCode.ToString(CultureInfo.InvariantCulture));

                            VerboseMessage("Execution completed with exit code={0}", exitCode);
                            if (exitCode != 0 && !IgnoreExitCode)
                                throw new ScriptRuntimeException(string.Format("Command [{0}] failed with exit code = {1}", Utils.QuoteArg(pi.FileName) + " " + pi.Arguments, p.ExitCode));
                        }
                    }
                }
            }
            finally
            {
                if (fileToDelete!=null)
                {
                    try
                    {
                        File.Delete(fileToDelete);
                    }
                    catch
                    {
                        
                    }
                }
            }
            return null;
        }


        private void fillFilenameAndArguments(ProcessStartInfo pi, string args)
        {
            string cmdLine = string.Concat(args,args.Length>0?" ":string.Empty,ShellArg.GetCommandLine(Context, Args)).TrimStart();
            if (cmdLine.StartsWith("\"",StringComparison.Ordinal))
            {
                int n = cmdLine.IndexOf("\"", 1,StringComparison.Ordinal);
                pi.FileName = cmdLine.Substring(1, n - 1);
                pi.Arguments = cmdLine.Substring(n + 1);
            }
            else
            {
                int n = cmdLine.IndexOf(" ", StringComparison.Ordinal);
                if (n == -1)
                {
                    pi.FileName = cmdLine;
                    pi.Arguments = string.Empty;
                }
                else
                {
                    pi.FileName = cmdLine.Substring(0, n);
                    pi.Arguments = cmdLine.Substring(n + 1);
                }
            }
        }


        class AsyncWriter
        {
            private readonly StreamWriter _sw;

            public AsyncWriter(StreamWriter sw, byte[] data)
            {
                _sw = sw;
                sw.BaseStream.BeginWrite(data, 0, (int)data.Length, write, null);
            }
            void write(IAsyncResult ar)
            {
                try
                {
                    _sw.BaseStream.EndWrite(ar);
                    _sw.BaseStream.Flush();
                    _sw.Close();
                }
                catch
                {

                }
            }
        }

        class AsyncReader : IDisposable
        {
            public delegate void OnStringAvailable(string data);
            public delegate void OnBinaryAvailable(byte[] dataBinary, int bytes);

            private readonly byte[] _byteBuffer;
            private readonly char[] _charBuffer;
            private readonly OnStringAvailable _callbackText;
            private readonly OnBinaryAvailable _callbackBinary;
            private readonly Decoder _decoder;
            private readonly Stream _stream;
            private ManualResetEvent _eofEvent;

            public AsyncReader(Stream stream, OnBinaryAvailable binaryCallback)
            {
                _stream = stream;
                _callbackBinary = binaryCallback;
                _byteBuffer = new byte[4096];
                _eofEvent = new ManualResetEvent(false);
                _stream.BeginRead(_byteBuffer, 0, _byteBuffer.Length, read, null);
            }

            public AsyncReader(Stream stream, Encoding encoding, OnStringAvailable callback)
            {
                _stream = stream;
                _callbackText = callback;
                _decoder = encoding.GetDecoder();
                _byteBuffer = new byte[1024];
                _charBuffer = new char[encoding.GetMaxCharCount(_byteBuffer.Length)];
                _eofEvent = new ManualResetEvent(false);
                _stream.BeginRead(_byteBuffer, 0, _byteBuffer.Length, read, null);
            }

            ~AsyncReader()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_eofEvent != null)
                    {
                        // Give it a chance to close
                        _eofEvent.WaitOne(5000,false);
                        _eofEvent.Close();
                        _eofEvent = null;
                    }
                }
            }

            private void read(IAsyncResult ar)
            {
                int readBytes = 0;
                try
                {
                    if (_stream!=null)
                        readBytes = _stream.EndRead(ar);
                }
                catch
                {
                }

                try
                {
                    if (readBytes != 0)
                    {
                        if (_callbackBinary!=null)
                            _callbackBinary(_byteBuffer,readBytes);
                        else
                        {
                            int charCount = _decoder.GetChars(_byteBuffer, 0, readBytes, _charBuffer, 0);
                            _callbackText(new string(_charBuffer, 0, charCount));
                        }
                        if (_stream != null)
                            _stream.BeginRead(_byteBuffer, 0, _byteBuffer.Length, read, null);
                    }
                    else
                        _eofEvent.Set();
                }
                catch
                {

                }
            }

            public void WaitEof(int ms)
            {
                _eofEvent.WaitOne(ms,false);
            }
        }
    }
}