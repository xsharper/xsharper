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
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Security.AccessControl;

namespace XSharper.Core
{
    /// Download progress status
    public struct DownloadProgress
    {
        /// Number of bytes received
        public long BytesReceived;

        /// Total number of bytes receive. -1 = unknown
        public long TotalBytesToReceive;

        /// Percentage completed. -1 = unknown
        public int ProgressPercentage;

        /// Constructor
        public DownloadProgress(int init)
        {
            BytesReceived = 0;
            TotalBytesToReceive = init;
            ProgressPercentage = init;
        }
        public DownloadProgress(long received, long? len)
        {
            BytesReceived = received;
            TotalBytesToReceive = len??-1;
            ProgressPercentage = 0;
            if (len!=null)
                ProgressPercentage = (int)(received*100.0/len.Value);
        }
        


        /// Returns the fully qualified type name of this instance.
        public override string ToString()
        {
            if (TotalBytesToReceive != -1)
                return string.Format("{0:00}%",ProgressPercentage);
            return BytesReceived+" bytes";
        }
    } ;

    /// Download a file
    [XsType("download", ScriptActionBase.XSharperNamespace)]
    [Description("Download a file")]
    public class Download : Block
    {
        /// URL from where to download
        [Description("URL from where to download")]
        [XsRequired]
        public string From { get; set; }

        /// Filename where to save the downloaded data. May be null if data is not saved to a file.
        [Description("Filename where to save the downloaded data. May be null if data is not saved to a file.")]
        public string To { get; set; }

        /// Location where to save the downloaded data. 
        [Description("Location where to save the downloaded data. ")]
        public string OutTo { get; set; }

        /// Encoding of the downloaded data
        [Description("Encoding of the downloaded data")]
        public string Encoding { get; set; }

        /// true if downloaded file is not a string, but binary data, and should be stored into OutTo as byte[]
        [Description("true if downloaded file is not a string, but binary data, FTP should be in binary mode, and result should be stored into OutTo as byte[]")]
        [XsAttribute("binary")]
        [XsAttribute("binaryFtp",Deprecated = true)]
        public bool Binary { get; set; }

        /// Username for HTTP basic authentication
        [Description("Username for HTTP basic authentication")]
        [XsAttribute("user"),XsAttribute("login",Deprecated=true)]
        public string User { get; set; }

        /// Password for HTTP basic authentication
        [Description("Password for HTTP basic authentication")]
        public string Password { get; set; }

        /// Data to be send with POST.
        [Description("Data to be send with POST.")]
        public string Post { get; set; }

        /// Content type of the POST data.
        [Description("Content type of the POST data.")]
        public string PostContentType { get; set; }

        /// User agent
        [Description("User agent")]
        public string UserAgent { get; set; }

        /// true if passive FTP should be used, in case of FTP protocol (default). 
        [Description("true if passive FTP should be used, in case of FTP protocol (default). ")]
        public bool PassiveFtp { get; set; }

        /// true if download goes directly to the output file. false=download to temp file and then move
        [Description("true if download goes directly to the output file. false=download to temp file and then move")]
        public bool Direct { get; set; }

        /// Cache level of the download request. Default: RequestCacheLevel.Default
        [Description("Cache level of the download request. ")]
        public RequestCacheLevel CacheLevel { get; set; }

        /// Variable prefix
        [Description("Variable prefix")]
        public string Name { get; set; }

        /// Timeout. Default 60 seconds
        [Description("Timeout")]
        public string Timeout { get; set; }

        /// Constructor
        public Download()
        {
            Binary=PassiveFtp = true;
            CacheLevel = RequestCacheLevel.Default;
            Timeout = "00:01:00";
        }
        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            if (string.IsNullOrEmpty(OutTo) == string.IsNullOrEmpty(To))
                throw new ParsingException("Either To or OutTo must be specified");
        }

        
        class DownloadState : IDisposable
        {
            private readonly IWriteVerbose _writeVerbose;
            private object _dpargs_lock = new object();
            public Exception _exception = null;
            private EventWaitHandle _completed = new ManualResetEvent(false);
            private EventWaitHandle _progressAvailable = new AutoResetEvent(false);
            private DownloadProgress _progress=new DownloadProgress(-1);
            private string _resultStr;
            private object _result;

            public DownloadState(IWriteVerbose writeVerbose)
            {
                _writeVerbose = writeVerbose;
            }

            ~DownloadState()
            {
                Dispose(false);
            }
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            protected void Dispose(bool dispose)
            {
                if (_completed != null)
                {
                    _completed.Close();
                    _completed = null;
                }
                if (_progressAvailable != null)
                {
                    _progressAvailable.Close();
                    _progressAvailable = null;
                }

            }
            public Exception Error
            {
                get { return _exception; }
            }

            public DownloadProgress Progress
            {
                get
                {
                    lock (_dpargs_lock)
                        return _progress;
                }
            }
            public object Result
            {
                get
                {
                    lock (_dpargs_lock)
                        return _result;
                }
            }
            public string ResultStr
            {
                get
                {
                    lock (_dpargs_lock)
                        return _resultStr;
                }
            }
            public WaitHandle Completed
            {
                get { return _completed; }
            }

            public WaitHandle ProgressAvailable
            {
                get { return _progressAvailable;  }
            }
            public void SetProgress(DownloadProgress p)
            {
                lock (_dpargs_lock)
                {
                    _progress = p;
                }
            }
            
            public void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
            {
                lock (_dpargs_lock)
                {
                    _progress.BytesReceived = e.BytesReceived;
                    _progress.TotalBytesToReceive = e.TotalBytesToReceive;
                    _progress.ProgressPercentage = e.ProgressPercentage;
                    if (_progressAvailable!=null)
                        _progressAvailable.Set();
                }
            }

            public void FileCompleted(object sender, AsyncCompletedEventArgs e)
            {
                lock (_dpargs_lock)
                {
                    try
                    {
                        _exception = e.Error;
                    }
                    catch (TargetInvocationException te)
                    {
                        _exception = te.InnerException;
                    }
                    catch (Exception err)
                    {
                        _exception = err;
                    }
                    if (_completed != null)
                        _completed.Set();
                }

            }
            public void StringCompleted(object sender, DownloadStringCompletedEventArgs e)
            {
                _writeVerbose.WriteVerbose("String download completed!");
                lock (_dpargs_lock)
                {
                    try
                    {
                        _exception = e.Error;
                        _resultStr = e.Result;
                    }
                    catch(TargetInvocationException te)
                    {
                        _exception = te.InnerException;
                    }
                    catch (Exception err)
                    {
                        _exception = err;
                    }
                    if (_completed != null)
                        _completed.Set();
                }
                

            }
            public void DataCompleted(object sender, DownloadDataCompletedEventArgs e)
            {
                lock (_dpargs_lock)
                {
                    try
                    {
                        _exception = e.Error;
                        _result = e.Result;
                    }
                    catch (TargetInvocationException te)
                    {
                        _exception = te.InnerException;
                    }
                    catch (Exception err)
                    {
                        _exception = err;
                    }
                    if (_completed != null)
                        _completed.Set();
                }
                

            }

            public void SetCompleted()
            {
                lock (_dpargs_lock)
                {
                    if (_completed != null)
                        _completed.Set();
                }
            }
        }

        /// Execute action
        public override object Execute()
        {
            string fromExpanded = Context.TransformStr(From, Transform);
            string toExpanded = Context.TransformStr(To, Transform);
            string outToExpanded = Context.TransformStr(OutTo, Transform);
            if (string.IsNullOrEmpty(outToExpanded))
            {
                if (!string.IsNullOrEmpty(toExpanded))
                    toExpanded = UrlToLocalFileName(fromExpanded, toExpanded);
            }
            var enc = Utils.GetEncoding(Context.TransformStr(Encoding, Transform));
            Uri uri = new Uri(fromExpanded);

            VerboseMessage("Downloading {0} => {1}...", Utils.SecureUri(fromExpanded), toExpanded);

            bool passive = PassiveFtp;
            bool ftpssl = false;
            bool ftp = false;
            var scheme = uri.Scheme;
            if (scheme == "ftpa" || scheme == "ftps" || scheme == "ftpas" || scheme == "ftpsa")
            {
                ftp = true;
                UriBuilder ub = new UriBuilder(uri);
                ub.Scheme = "ftp";
                uri = ub.Uri;
                passive = !(scheme == "ftpa" || scheme == "ftpas" || scheme == "ftpsa");
                ftpssl = (scheme == "ftps" || scheme == "ftpas" || scheme == "ftpsa");
            }
            var timeout = Utils.ToTimeSpan(Context.TransformStr(Timeout, Transform));



            if (uri.IsFile || uri.Scheme == "embed")
            {
                VerboseMessage("Local filename '{0}' detected. Copying instead", uri.LocalPath);
                try
                {
                    if (Binary)
                    {
                        if (File.Exists(toExpanded))
                            File.Delete(toExpanded);
                        using (var toStr = Context.CreateStream(toExpanded))
                            copyFile(uri.LocalPath, toStr, toExpanded, true);
                    }
                    else
                    {
                        using (var ms = new MemoryStream())
                        {
                            copyFile(uri.LocalPath, ms, "memory:///", true);
                            Context.OutTo(outToExpanded, (enc == null ? new StreamReader(ms) : new StreamReader(ms, enc)).ReadToEnd());
                        }
                    }
                }
                catch
                {
                    File.Delete(toExpanded);
                    throw;
                }
                return null;
            }
            using (DownloadState state = new DownloadState(Context))
            {
                using (WebClientEx webClient = new WebClientEx(passive, Binary))
                {
                    webClient.KeepAlive = (ftp && !passive);
                    webClient.FtpSsl = ftpssl;
                    webClient.CachePolicy = new RequestCachePolicy(CacheLevel);

                    string user = Context.TransformStr(User, Transform);
                    string password = Context.TransformStr(Password, Transform);
                    uri = webClient.SetCredentials(uri, user, password);

                    if (!string.IsNullOrEmpty(Post))
                    {
                        webClient.HttpPost = Context.Transform(Post, Transform);
                        if (!string.IsNullOrEmpty(PostContentType))
                            webClient.HttpPostContentType = Context.TransformStr(PostContentType, Transform);
                    }
                    webClient.HttpUserAgent = Context.TransformStr(UserAgent, Transform);
                    webClient.Timeout = timeout;


                    int oldPercentage = -1;
                    long bytesReceived = -1;


                    // We must ensure that all script components are executed in a single thread
                    webClient.DownloadProgressChanged += state.ProgressChanged;
                    webClient.DownloadFileCompleted += state.FileCompleted;
                    webClient.DownloadStringCompleted += state.StringCompleted;
                    webClient.DownloadDataCompleted += state.DataCompleted;

                    if (enc != null)
                        webClient.Encoding = enc;

                    string tmp = null;
                    if (string.IsNullOrEmpty(outToExpanded))
                        tmp = Direct ? toExpanded : Path.GetTempFileName();


                    var lastUpdate = System.Diagnostics.Stopwatch.StartNew();
                    WaitHandle[] wh = new WaitHandle[] { state.Completed, state.ProgressAvailable };
                    try
                    {
                        if (tmp == null)
                        {
                            if (Binary)
                                webClient.DownloadDataAsync(uri);
                            else
                                webClient.DownloadStringAsync(uri);
                        }
                        else
                            webClient.DownloadFileAsync(uri, tmp);

                        string pref = Context.TransformStr(Name, Transform);
                        while (true)
                        {
                            int n = WaitHandle.WaitAny(wh, 300, true);

                            if (n == 0 || n == 1)
                            {
                                lastUpdate = System.Diagnostics.Stopwatch.StartNew();
                                DownloadProgress ps = state.Progress;
                                if (n == 0)
                                {
                                    ps = state.Progress;
                                    if (Binary && state.Result != null)
                                        ps.BytesReceived = ((byte[])state.Result).LongLength;
                                    else if (tmp != null)
                                        ps.BytesReceived = new FileInfo(tmp).Length;
                                }

                                if (ps.BytesReceived > 0 && ps.BytesReceived > bytesReceived)
                                {
                                    VerboseMessage("Received: {0}", ps);
                                    Context.OnProgress(ps.ProgressPercentage, uri.ToString());
                                    oldPercentage = ps.ProgressPercentage;

                                    if (base.Items.Count != 0)
                                    {
                                        Vars sv = new Vars();
                                        sv.Set("", ps);
                                        Context.ExecuteWithVars(baseExecute, sv, pref);
                                    }
                                    bytesReceived = ps.BytesReceived;
                                }
                            }
                            else
                            {
                                // Sometimes FTP hangs, seen with FileZilla 0.9.31 + VMWare a few times
                                if (timeout.HasValue && lastUpdate.Elapsed > timeout.Value)
                                    throw new TimeoutException();
                            }
                            if (n == 0)
                            {
                                break;
                            }


                            Context.OnProgress(Math.Max(oldPercentage, 0), uri.ToString());
                        }
                        if (state.Error != null)
                        {
                            if (state.Error is TargetInvocationException)
                                Utils.Rethrow(state.Error.InnerException);
                            else
                                Utils.Rethrow(state.Error);
                        }

                        if (tmp != null && toExpanded != tmp)
                        {
                            if (File.Exists(toExpanded))
                                File.Delete(toExpanded);
                            using (var toStr = Context.CreateStream(toExpanded))
                                copyFile(tmp, toStr, toExpanded, false);
                            VerboseMessage("Copying completed. Deleting '{0}'", tmp);
                            File.Delete(tmp);
                        }
                    }
                    catch (Exception e)
                    {
                        VerboseMessage("Caught exception: {0}", e.Message);
                        webClient.CancelAsync();
                        state.SetCompleted();
                        throw;
                    }
                    finally
                    {
                        VerboseMessage("Waiting for download completion");

                        state.Completed.WaitOne(timeout ?? TimeSpan.FromSeconds(30), false);

                        VerboseMessage("Waiting completed");

                        webClient.DownloadProgressChanged -= state.ProgressChanged;
                        webClient.DownloadFileCompleted -= state.FileCompleted;
                        webClient.DownloadStringCompleted -= state.StringCompleted;
                        webClient.DownloadDataCompleted -= state.DataCompleted;


                        try
                        {
                            if (webClient.IsBusy)
                                webClient.CancelAsync();
                        }
                        catch
                        {
                        }

                        if (tmp == null)
                            Context.OutTo(outToExpanded, Binary ? state.Result : state.ResultStr);
                        else if (tmp != toExpanded)
                        {
                            try
                            {
                                File.Delete(tmp);
                            }
                            catch (IOException)
                            {
                                Thread.Sleep(500);
                                File.Delete(tmp);
                            }
                        }
                    }
                    VerboseMessage("Download completed.");
                }

            }

            return null;
        }

        private void copyFile(string from,Stream toStr, string to, bool withProgress)
        {
            // Copy manually, as normal File.Move is likely to copy ACL from text directory as well
            VerboseMessage("Copying file '{0}' to '{1}'", from, to);
            long? len = null;
            try
            {
                len = new FileInfo(from).Length;
            }
            catch { }
            byte[] buf = new byte[65536];
            long copied = 0;
            string pref = Context.TransformStr(Name, Transform);
            try
            {
                using (var fromStr = Context.OpenStream(from))
                {
                    if (withProgress)
                    {
                        var ps=new DownloadProgress(0, len);
                        Context.OnProgress(0, from);
                        if (base.Items.Count != 0)
                        {
                            Vars sv = new Vars();
                            sv.Set("", ps);
                            Context.ExecuteWithVars(baseExecute, sv, pref);
                            
                        }
                    }

                    int n;
                    while ((n = fromStr.Read(buf, 0, buf.Length)) != 0)
                    {
                        Context.CheckAbort();
                        toStr.Write(buf, 0, n);
                        copied += n;
                        if (withProgress)
                        {
                            var ps=new DownloadProgress(copied, len);
                            Context.OnProgress(ps.ProgressPercentage, from);
                            
                            if (base.Items.Count != 0)
                            {
                                Vars sv = new Vars();
                                sv.Set("", ps);
                                Context.ExecuteWithVars(baseExecute, sv, pref);
                            }
                        }
                    }
                    if (withProgress)
                    {
                        var ps = new DownloadProgress(copied, copied);
                        Context.OnProgress(100, from);
                        if (base.Items.Count != 0)
                        {
                            Vars sv = new Vars();
                            sv.Set("", ps);
                            Context.ExecuteWithVars(baseExecute, sv, pref);
                        }
                    }
                }

             
            }
            catch
            {
                VerboseMessage("Copying failed. Deleting '{0}'", to);
                File.Delete(to);
            }
        }


        object baseExecute()
        {
            return base.Execute();
        }
        
        /// <summary>
        /// Convert URL to local filename. For example http://www.google.com/x/y.data => c:\out\y.data
        /// </summary>
        /// <param name="url">url to download</param>
        /// <param name="destination">destination directory</param>
        /// <returns>full path of the suggested file</returns>
        public static string UrlToLocalFileName(string url, string destination)
        {
            Uri uri = new Uri(url);
            if (Directory.Exists(destination))
            {
                string s = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);  
                int n = s.LastIndexOfAny(new char[] {'/', '\\'});
                if (n != -1)
                    s = s.Substring(n + 1);
                if (s.Length == 0)
                    if (uri.Scheme == "http" || uri.Scheme == "https")
                        s = "default.html";
                    else
                        s = "default.dat";

                string extra = uri.GetComponents(UriComponents.Query | UriComponents.Fragment, UriFormat.Unescaped);
                if (!string.IsNullOrEmpty(extra))
                    s += "?" + extra;

                StringBuilder sb = new StringBuilder();
                sb.Append(Utils.BackslashAdd(Path.GetFullPath(destination)));
                foreach (char c in s)
                    if ("%^&()*<>?'\"`;:".IndexOf(c) != -1)
                        sb.Append("_");
                    else
                        sb.Append(c);

                return sb.ToString();
            }
            return Path.GetFullPath( destination);
        }
    }
}