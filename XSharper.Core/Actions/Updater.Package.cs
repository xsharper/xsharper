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
using System.Security.Cryptography;

namespace XSharper.Core
{
    /// <summary>
    /// Updater component action 
    /// </summary>
    [XsType(null)]
    [Description("Updater package")]
    public class Package : XsTransformableElement
    {
        /// Package name
        [Description("Package Name")]
        [XsNotTransformed]
        [XsAttribute("name")]
        [XsAttribute("id",Deprecated = true)]
        [XsRequired]
        public string Name { get; set; }

        /// Package description
        [Description("Package description")]
        public string Description { get; set; }

        /// Block that returns installed Package version as "1.2.3.4", or an empty string if not installed
        [Description("Block that returns installed Package version as 1.2.3.4, or null/empty string if not installed")]
        [XsElement("getVersion",SkipIfEmpty = true)]
        public Block GetVersion { get;set;}

        /// Block that is called to do an update
        [Description("Block that is called to do an update")]
        [XsElement("update", SkipIfEmpty = true)]
        public Block Update { get; set; }

        private string _packageDownloadName
        {
            get
            {
                var context = ScriptContextScope.Current;
                return (string)context.StateBag.Get(this, "pdn", null);
            }
            set
            {
                var context = ScriptContextScope.Current;
                context.StateBag.Set(this,"pdn",value);
            }
        }

        /// Get a friendly name of the Package , as a combination of ID and Description
        public string FriendlyName
        {
            get
            {
                if (string.IsNullOrEmpty(Description))
                    return "[" + Name + "]";
                return Description + " [" + Name + "]";
                
            }
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public void Initialize()
        {
            var context = ScriptContextScope.Current;
            context.Initialize(GetVersion);
            context.Initialize(Update);
        }

        /// <summary>
        /// Execute a delegate for all child nodes of the current node
        /// </summary>
        public bool ForAllChildItems(Predicate<IScriptAction> func, bool isFind)
        {
            if (func(GetVersion)) return true;
            if (func(Update)) return true;
            return false;
        }

        /// Return version
        public object CheckVersion()
        {
            var context = ScriptContextScope.Current;
            context.WriteVerbose("Checking version of [" + Name + "]");
            return context.Execute(GetVersion);
        }

        private static bool isValidHash(string filename,byte[] sha1)
        {
            var context = ScriptContextScope.Current;
            context.WriteVerbose("IsValidHash: '" + filename + "' ");
            if (filename==null || sha1==null || !File.Exists(filename))
                return false;

            var real = context.SHA1File(filename);
            context.WriteVerbose("Hash of '" + filename + "' is " + Utils.ToHex(real));
            context.WriteVerbose("Correct hash is is " + Utils.ToHex(sha1));
            if (real.Length!=sha1.Length)
                return false;
            for (int i=0;i<real.Length;++i)
                if (real[i]!=sha1[i])
                    return false;
            return true;
        }

        /// Download package
        public void Download(Updater updater)
        {
            var context = ScriptContextScope.Current;

            DirectoryInfo download = updater.GetDownloadDirectory();
            download.Create();

            DirectoryInfo dsum = updater.GetWorkingDirectory(Name);
            if (updater.Cleanup)
                Cleanup(updater);
            dsum.Create();

            PackageInfo pi = updater.GetPackage(Name);
            if (pi.DownloadUri == null)
            {
                context.Info.WriteLine("[{0}] No download location provided...", Name);
                return;
            }

            var edition = context.TransformStr(updater.Edition, Transform);
            string fileName = edition + "." +Name + "." +  pi.Version + ".zip";

            _packageDownloadName = Path.Combine(download.FullName, fileName);
            // If there is hash, we don't necessarily have to download
            if (pi.Hash==null || !isValidHash(_packageDownloadName, pi.Hash))
            {
                context.Info.WriteLine("[{0}] Downloading package from {1}...", Name, Utils.SecureUri(pi.DownloadUri));

                Download dn = new Download
                    {
                        From = pi.DownloadUri.ToString(),
                        To = _packageDownloadName,
                        CacheLevel = updater.CacheLevel,
                        Transform = TransformRules.None

                    };
                context.InitializeAndExecute(dn);

                // Validate hash
                if (pi.Hash!=null && !isValidHash(_packageDownloadName, pi.Hash))
                    throw new ScriptRuntimeException("Hash of data downloaded from " + Utils.SecureUri(pi.DownloadUri) + " is invalid");
            }

            // Extract the file
            context.Info.WriteLine("[{0}] Extracting {1}...", Name, _packageDownloadName);
            Unzip ex = new Unzip
                                {
                                    To = dsum.FullName,
                                    From = _packageDownloadName,
                                    Clean = false,
                                    ZipTime = updater.ZipTime,
                                    Password = updater.Password,
                                    Transform = TransformRules.None
                                };
            context.InitializeAndExecute(ex);
        }

        /// Execute update
        public void DoUpdate(Updater updater )
        {
            var context = ScriptContextScope.Current;

            // Create a directory in download catalog
            DirectoryInfo dsum = updater.GetWorkingDirectory(Name);
            
            string dir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(dsum.FullName);
            string oldPath = context.ScriptPath;
            try
            {
                context.ScriptPath = dsum.FullName + ";" + oldPath;
                context.Info.WriteLine("[{0}] Updating...", Name);
                context.Execute(Update);
                var cv=ReturnValue.Unwrap(CheckVersion());
                context.Out.WriteLine("[{0}] Update completed. New version is {1}", Name, cv);
            }
            finally
            {
                context.ScriptPath = oldPath;
                Directory.SetCurrentDirectory(dir);
            }
            
        }


        /// Cleanup
        public void Cleanup(Updater updater)
        {
            var context = ScriptContextScope.Current;
            Delete d = new Delete
                {
                    DeleteReadOnly = true,
                    DeleteRoot = true,
                    Catch = new Block(),
                    Recursive = true,
                    From = updater.GetWorkingDirectory(Name).FullName,
                    Transform = TransformRules.None
                };
            context.InitializeAndExecute(d);
        }


    }
}