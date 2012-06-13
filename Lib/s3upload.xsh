<xsharper switchPrefixes="-">
    <usage options="default ifNoArguments autoSuffix" />
    <versionInfo version="1.0.0.0">Program to upload files to Amazon S3.</versionInfo>
    <param name="directory" required="1">bucket/directory name</param>
    <param name="files" required="1" count="multiple" description="file [file]">Files to upload</param>
    <param />
    <param>Either -secretkey or -secretkeyfile must be specified.</param>
    <param switch="baseUrl" default="http://s3.amazonaws.com">Service URL</param>
    <param switch="accesskey" required="1">Access key</param>
    <param switch="secretkey">Secret key</param>
    <param switch="secretkeyfile">Text file, first line of which contains the key</param>

    <if isNotSet="secretKey">
        <if isNotSet="secretkeyfile">
            <throw>Either -secretkey or -secretkeyfile must be specified.</throw>
        </if>
        <set secretKey="${=.ReadLines($secretKeyFile)[0].Trim()}" />
    </if>


    <foreach file="${files}">
        <print outTo="^info" nl="false">Uploading ${file} ...</print>
        <code />
        <set md5="${=X.FileMD5($file)}" />
        <set url="${=X.SignUrl($baseUrl,$accesskey,$secretkey,$directory,$file,$md5)}" />
        <print outTo="^debug">${url}</print>

        <code>
            try 
            {
				System.Net.ServicePointManager.Expect100Continue = true;
                using (XS.WebClientEx w=new XS.WebClientEx())
                {
                    w.Headers.Add(HttpRequestHeader.ContentMd5, c.GetStr("md5"));           
                    w.Timeout=TimeSpan.FromSeconds(100000);
                    w.UploadFile(c.GetStr("url"),"PUT",c.GetStr("file"));
                }
            }
            catch (WebException e)
            {
                if (e.Response!=null)
                    try {   
                        XS.XmlDoc xs=new XS.XmlDoc(((HttpWebResponse)e.Response).GetResponseStream());
                        c.Error.WriteLine("Error message: {0}",xs.V("/Error/Message/text()"));
                    }
                    catch 
                    {
                    }
                throw;
            }
        </code>
        <print outTo="^info"> Done</print>
    </foreach>

    <?h using System.Security.Cryptography;
     using System.Net;

     public static class X
     {
        public static string FileMD5(string filename)
        {
            using (Stream fs=File.OpenRead(filename))
            using (MD5 md5=MD5.Create())
                return Convert.ToBase64String(md5.ComputeHash(fs),Base64FormattingOptions.None);
        }
        public static string SignUrl(string baseUrl, string accesskey, string secretkey, string directory, string filename,string md5)
        {                            
            directory=directory.TrimStart('/');
            int n=directory.IndexOf('/');
            
            string path=directory;
            if (!path.EndsWith("/"))
                path+='/';
            path+=Path.GetFileName(filename);
        
            string expiration=((long)((DateTime.UtcNow.AddDays(1)-new DateTime(1970, 1, 1)).TotalSeconds)).ToString();
            using (HMACSHA1 sign = new HMACSHA1(Encoding.UTF8.GetBytes(secretkey)))
            {
                string escaped=Uri.EscapeUriString(path);
                string stringToSign = string.Format("PUT\n{0}\napplication/octet-stream\n{1}\n/{2}",md5,expiration,escaped);
                string authorization = Uri.EscapeDataString(Convert.ToBase64String(sign.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)),Base64FormattingOptions.None).Trim());
                return string.Format( "{0}/{1}?AWSAccessKeyId={2}&Expires={3}&Signature={4}",baseUrl, escaped,accesskey,expiration,authorization);
            }
        }
      }
   ?>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>IDosnLNrY36ShzfALeWG28F2vss=</DigestValue></Reference></SignedInfo><SignatureValue>iaMvOVCfUARmrIzeqVgTGd+apLBTwGBhzr0GW2r0157JSpkanaPJu3HV2GNvizrf/kJdhRPQ1N5XTBKbHgAFs2PTvTUDBIhN9bNDniDs8gJPgvzFG45CYKYBiLdisoNj1VEaYFt6WCzEbe+SY6L+5PUc98pXUEZopy1QHl7nJ5A=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>