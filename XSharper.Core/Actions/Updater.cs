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
using System.Net.Cache;
using System.Security.Cryptography.Xml;
using System.Text.RegularExpressions;
using System.Xml;

namespace XSharper.Core
{
    class PackageInfo
    {
        public Version Version;
        public Uri DownloadUri;
        public byte[] Hash;
    }
    
    ///<summary>A set of components to be updated</summary>
    [XsType("updater", ScriptActionBase.XSharperNamespace)]
    [Description("A set of components to be updated")]
    public class Updater : ScriptActionBase
    {
        /// Block to run before all updates
        [Description("Block to run before all updates")]
        [XsElement("beforeUpdate", SkipIfEmpty = true)]
        public Block BeforeUpdate { get; set; }

        /// List of components to be updated
        [Description("List of components to be updated")]
        [XsElement("", CollectionItemElementName = "package", CollectionItemType = typeof(Package))]
        public List<Package> Items { get; set; }

        /// Block to run after all updates
        [Description("Block to run after all updates")]
        [XsElement("afterUpdate", SkipIfEmpty = true)]
        public Block AfterUpdate { get; set; }

        /// Validate signatures of the packages
        [Description("Validate signatures of the packages")]
        public bool ValidateSignature { get; set; }

        /// Which edition to download and install
        [Description("Which edition to download and install")]
        [XsRequired]
        public string Edition { get; set; }

        /// Repository address
        [Description("Repository address")]
        [XsRequired]
        public string Repository { get; set; }

        /// Working directory, where files are extracted temporarily
        [Description("Working directory, where files are extracted temporarily")]
        [XsRequired]
        public string WorkingDirectory { get; set; }

        /// Download directory, where files are downloaded from repository
        [Description("Download directory, where files are downloaded from repository")]
        public string DownloadDirectory { get; set; }

        /// Cache level, affecting download
        [Description("Cache level, affecting download")]
        [XsAttribute("cacheLevel")]
        [XsAttribute("cache",Deprecated = true)]
        public RequestCacheLevel CacheLevel { get; set; }

        /// True if extracted files should be deleted after execution
        [Description("True if extracted files should be deleted after execution")]
        public bool Cleanup { get; set; }

        /// Only components with ID that matches this filter will be updated
        [Description("Only components with ID that matches this filter will be updated")]
        public string Filter { get; set; }

        /// Syntax of the filter
        [Description("Syntax of the filter")]
        public FilterSyntax Syntax { get; set; }

        /// True if update must be executed even if component version is up to date
        [Description("True if update must be executed even if component version is up to date")]
        public bool ForceUpdate { get; set; }

        /// How to interpret time in downloaded ZIP archives
        [Description("How to interpret time in downloaded ZIP archives")]
        public ZipTime ZipTime { get; set; }

        /// Password for password protected ZIP files
        [Description("Password for password protected ZIP files")]
        public string Password { get; set; }

        /// Default constructor
        public Updater()
        {
            CacheLevel = RequestCacheLevel.Default;
            ZipTime = ZipTime.FileTime;
            Syntax = FilterSyntax.Auto;
            Filter = "*";
            Cleanup = true;
        }

        Dictionary<string, PackageInfo> knownPackages
        {
            get { return (Dictionary<string, PackageInfo>)Context.StateBag.Get(this, "known", null); }
            set { Context.StateBag.Set(this, "known", value); }
        }


        /// <summary>
        /// Execute a delegate for all child nodes of the current node
        /// </summary>
        public override bool ForAllChildren(Predicate<IScriptAction> func, bool isFind)
        {
            if (base.ForAllChildren(func,isFind) || func(BeforeUpdate) || func(AfterUpdate)) return true;
            foreach (var item in Items)
                if (item.ForAllChildItems(func,isFind))
                    return true;
            return false;
        }

        internal DirectoryInfo GetDownloadDirectory()
        {
            string s = Context.TransformStr(DownloadDirectory, Transform);
            if (string.IsNullOrEmpty(s))
                s = Path.Combine(Context.TransformStr(WorkingDirectory, Transform), ".download");
            return new DirectoryInfo(s);
        }
        internal DirectoryInfo GetWorkingDirectory(string id)
        {
            return new DirectoryInfo(Path.Combine(Context.TransformStr(WorkingDirectory, Transform),id));
        }

        internal PackageInfo GetPackage(string id)
        {
            PackageInfo r;
            if (knownPackages!=null && knownPackages.TryGetValue(id, out r))
                return r;
            return null;
        }

        void downloadPackagesInfo()
        {
            ScriptContext context = Context;
            var edition =Context.TransformStr(Edition, Transform);

            Uri piUri;
            if (Regex.IsMatch(edition, "^\\w+$"))
            {
                var ub = new UriBuilder(new Uri(Context.TransformStr(Repository, Transform)));
                ub.Path = ub.Path.TrimEnd('/') + "/" + edition + ".pinfo";
                piUri = ub.Uri;
            }
            else
                piUri=new Uri(edition);

            // Download the file
            Context.Info.WriteLine("Downloading available packages from " + Utils.SecureUri(piUri));
            
            byte[] pinfo = null;
            context.ExecuteWithVars(() =>
                {
                    Download d = new Download();
                    d.From = piUri.ToString();
                    d.OutTo = "var";
                    d.Binary = true;
                    d.CacheLevel = CacheLevel;
                    context.Execute(d);
                    pinfo = (byte[]) Context["var"];
                    return null;
                }, new Vars(), null);
            
            XmlDocument x = new XmlDocument();
            x.PreserveWhitespace = true;
            x.Load(new MemoryStream(pinfo));
            if (ValidateSignature)
            {
                SignedXml verifier = new SignedXml(x);
                var el = x.GetElementsByTagName("Signature");

                bool valid = false;
                if (el != null && el.Count != 0 && el[0] != null && el[0] is XmlElement)
                {
                    verifier.LoadXml((XmlElement) el[0]);
                    valid = Context.VerifyXmlSignature(verifier);
                }
                else
                    VerboseMessage("PI. Validity of information about available packages cannot be verified, signature not found in {0}.", Utils.SecureUri(piUri));
                if (!valid)
                    throw new ScriptRuntimeException("Information about available packages is corrupted in " + Utils.SecureUri(piUri));
            }

            // Parsing
            try
            {
                Dictionary<string, PackageInfo> pinfo1 = new Dictionary<string, PackageInfo>(StringComparer.OrdinalIgnoreCase);
                var n = x.SelectNodes("//package");
                if (n != null)
                    foreach (XmlNode node in n)
                    {
                        PackageInfo pi = new PackageInfo();

                        var id = node.Attributes["name"];
                        if (id == null)
                            id = node.Attributes["id"];
                        if (id == null)
                            throw new ScriptRuntimeException("name attribute is missing in package information");
                        var v = node.Attributes["version"];
                        if (v == null)
                            throw new ScriptRuntimeException("version attribute is missing in package information [" + id.Value + "]");

                        pi.Version = new Version(v.Value);

                        var hash = (node.Attributes["sha1"]);
                        if (hash != null)
                            pi.Hash = Utils.ToBytes(hash.Value);


                        var urlNode = node.Attributes["location"];
                        if (urlNode==null)
                            urlNode=node.Attributes["url"];
                        if (urlNode != null && !string.IsNullOrEmpty(urlNode.Value))
                        {
                            string url = urlNode.InnerText;
                            Uri outUri = null;
                            if (!Uri.TryCreate(url, UriKind.Absolute, out outUri))
                                Uri.TryCreate(piUri, url, out outUri);

                            if (outUri != null)
                                pi.DownloadUri = outUri;
                            else
                                throw new ScriptRuntimeException("Invalid download URL " + Utils.SecureUri(url) + " in package information [" + id.Value + "]");
                        }
                        pinfo1[id.Value] = pi;
                    }
                knownPackages = pinfo1;
            }
            catch (ScriptException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ScriptRuntimeException("An error occured while processing " + Utils.SecureUri(piUri) + " .", e);
            }

            VerboseMessage("PI. Information about {0} available packages is loaded.", knownPackages.Count);
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Context.Initialize(BeforeUpdate);
            Context.Initialize(AfterUpdate);
            foreach (var item in Items)
                item.Initialize();
        }

        /// Execute action
        public override object Execute()
        {
            downloadPackagesInfo();

            RowSet rs=new RowSet();
            Context.Initialize(rs);

            List<Package> toUpdate = new List<Package>();
            var filter = new StringFilter(Syntax, Context.TransformStr(Filter, Transform));
            foreach (var c in Items)
            {
                var rv=c.CheckVersion();
                Version currentVersion = null;
                string str = (rv==null?null:(ReturnValue.Unwrap(rv)??string.Empty).ToString());
                if (!string.IsNullOrEmpty(str))
                    currentVersion = new Version(str);

                var pinfo = GetPackage(c.Name);
                Version newVersion =(pinfo != null) ?pinfo.Version:null;

                rs.AddRow(new Vars(){
                    {"Package",c.FriendlyName},
                    {"Installed",(currentVersion==null)?"not installed":currentVersion.ToString()},
                    {"Available",(newVersion == null) ? "n/a" : newVersion.ToString()}}
                );

                if (currentVersion!=null && ((newVersion > currentVersion) || ForceUpdate) && filter.IsMatch(c.Name))
                    toUpdate.Add(c);
            }

            Context.Out.WriteLine(string.Empty);
            Context.Out.WriteLine(rs.ToTextTable(TableFormatOptions.Header));

            if (toUpdate.Count==0)
            {
                Context.Out.WriteLine("All packages are up to date!");
                return null;
            }

            try
            {
                VerboseMessage("--- Downloading components... --- ");
                foreach (var c in toUpdate)
                    c.Download(this);
                VerboseMessage("--- Download completed. --- ");
                VerboseMessage(string.Empty);

                VerboseMessage("--- Starting update... ---");
                Context.Execute(BeforeUpdate);
                try
                {
                    foreach (var c in toUpdate)
                        c.DoUpdate(this);
                }
                finally
                {
                    Context.Execute(AfterUpdate);
                }
                VerboseMessage("--- Update completed. ---");
                VerboseMessage(string.Empty);
                return null;
            }
            finally
            {
                if (Cleanup)
                {
                    VerboseMessage("--- Cleaning up... ---");
                    foreach (var c in toUpdate)
                    {
                        try
                        {
                            c.Cleanup(this);
                        }
                        catch
                        {
                            Context.WriteLine(OutputType.Error, string.Format("Cleanup failed for {0}...", c.Name));
                        }
                    }
                    VerboseMessage("--- Cleaning completed. ---");
                    VerboseMessage(string.Empty);

                }
            }
        }
    }
}