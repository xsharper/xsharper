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
using System.Net;
using System.Text;

namespace XSharper.Core
{
    /// <summary>
    /// WebClient that can do active and text mode FTP
    /// </summary>
    public class WebClientEx : WebClient
    {
        private bool _passive;
        private bool _binary;
        private bool _ftpssl;
        
        /// HttpPost data. Can be either string or byte[], and in either case it should be properly encoded
        public object HttpPost { get; set; }

        /// Content type for POST. Default is application/x-www-form-urlencoded
        public string HttpPostContentType { get; set;}

        /// User agent
        public string HttpUserAgent { get; set; }

        /// Timeout
        public TimeSpan? Timeout { get; set; }

        /// In FTP mode, use SSL connection
        public bool FtpSsl
        {
            get { return _ftpssl; }
            set { _ftpssl = value; }
        }
        /// In FTP mode, use passive connection (default)
        public bool FtpPassive
        {
            get { return _passive; }
            set { _passive = value; }
        }

        /// In FTP mode, use binary connection (default)
        public bool FtpBinary
        {
            get { return _binary; }
            set { _binary = value; }
        }

        /// Keep alive, for connections that do support it. null=keep default.
        public bool? KeepAlive { get; set;}

        /// Default constructor
        public WebClientEx()
        {
            _passive= true;
            _binary = true;
            _ftpssl = false;
        }

        
        /// Constructor for FTP mode, where passive and binary mode may be specified
        public WebClientEx(bool passive, bool binary)
        {
            FtpPassive = passive;
            FtpBinary = binary;
            Proxy = WebRequest.GetSystemWebProxy();
            Proxy.Credentials = CredentialCache.DefaultCredentials;
        }
        
        /// Returns a <see cref="T:System.Net.WebRequest"/> object for the specified resource.
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest req = base.GetWebRequest(address);
            if (Timeout != null)
                req.Timeout = (int)Timeout.Value.TotalMilliseconds;

            FtpWebRequest freq = req as FtpWebRequest;
            HttpWebRequest httpreq = req as HttpWebRequest;
            
            
            if (freq!=null)
            {
                var f = freq;
                req = f;
                f.UsePassive = _passive;
                f.UseBinary = _binary;
                if (KeepAlive.HasValue)
                    f.KeepAlive = KeepAlive.Value;
                f.EnableSsl = _ftpssl;
            }
            else if (httpreq!=null)
            {
                if (KeepAlive.HasValue)
                    httpreq.KeepAlive = KeepAlive.Value;
                if (HttpPost != null)
                {
                    byte[] data= HttpPost as byte[];
                    if (data==null)
                        data = Encoding.UTF8.GetBytes(HttpPost.ToString());

                    httpreq.Method = "POST";
                    httpreq.ContentType = HttpPostContentType ?? "application/x-www-form-urlencoded";
                    httpreq.ContentLength = data.Length;
                    using (var str = httpreq.GetRequestStream())
                        str.Write(data,0,data.Length);
                }
                if (!string.IsNullOrEmpty(HttpUserAgent))
                    httpreq.UserAgent = HttpUserAgent;
            }
            
            return req;
        }

        
        /// <summary>
        /// Get credentials if specified in the URI (like ftp://user:password@server... ), set them to webClient, and return new credentials w/o the username
        /// </summary>
        /// <param name="uri">URI to parse</param>
        /// <param name="user">username to override the name specified in the URI, or null if no override</param>
        /// <param name="password">password to override the password specified in the URI, or null if no override</param>
        /// <returns>A new URI without embedded credentials</returns>
        public Uri SetCredentials(Uri uri, string user, string password)
        {
            Uri ret = uri;

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                UriBuilder ub = new UriBuilder(uri);
                
                if (user==null)
                    user = ub.UserName;
                if (password==null)
                    password = ub.Password;
                ub.UserName = null;
                ub.Password = null;
                ret = ub.Uri;
            }

            if (!string.IsNullOrEmpty(user) || !string.IsNullOrEmpty(password))
                Credentials = new NetworkCredential(user ?? "", password ?? "");
            return ret;
        }
    }
}