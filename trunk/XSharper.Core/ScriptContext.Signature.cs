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
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Security.Cryptography.X509Certificates;

namespace XSharper.Core
{
    public partial class ScriptContext
    {
        /// <summary>
        /// Validate script XML signature
        /// </summary>
        /// <param name="signedXml">Signed XML</param>
        /// <returns>true if signature is valid</returns>
        public virtual bool VerifyXmlSignature(SignedXml signedXml)
        {
            bool valid = false;
            foreach (var k in signedXml.KeyInfo)
            {
                var kv = k as RSAKeyValue;
                if (kv != null)
                {
                    var rsa = new RSACryptoServiceProvider();
                    var key = Assembly.GetExecutingAssembly().GetName().GetPublicKey();
                    if (key == null || key.Length < 12)
                        return false;

                    // here is a trick: the public key in assembly file has a 12-byte header at the beginning. 
                    // We strip it - and the remaining bytes can be now imported by RSACryptoServiceProvider class
                    var tmpKey = new byte[key.Length - 12];
                    Array.Copy(key, 12, tmpKey, 0, tmpKey.Length);
                    rsa.ImportCspBlob(tmpKey);
                    valid = signedXml.CheckSignature(rsa);
                    if (!valid)
                        break;
                    continue;
                }

                var ki = k as KeyInfoX509Data;
                if (ki != null)
                {
                    if (ki.Certificates == null)
                        valid = false;
                    else
                        foreach (X509Certificate2 cert in ki.Certificates)
                        {
                            // Verify that certificate is trusted for code signing
                            X509ChainPolicy pol = new X509ChainPolicy();
                            pol.RevocationMode = X509RevocationMode.NoCheck;
                            pol.VerificationFlags = X509VerificationFlags.IgnoreEndRevocationUnknown | X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown | X509VerificationFlags.IgnoreRootRevocationUnknown;
                            pol.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.3"));

                            X509Chain chain = new X509Chain(true);
                            chain.ChainPolicy = pol;
                            if (chain.Build(cert))
                                valid = signedXml.CheckSignature(cert, true);
                            if (!valid)
                                break;
                        }
                    if (!valid)
                        break;
                    continue;
                }
            }
            return valid;
        }

        /// <summary>
        /// Validate script XML Signature
        /// </summary>
        /// <param name="stream">Script XML stream</param>
        /// <returns>Signature status</returns>
        public virtual ScriptSignatureStatus VerifyScriptSignature(Stream stream)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.Load(stream);

                SignedXml verifier = new SignedXml(doc);
                var el = doc.GetElementsByTagName("Signature");
                if (el != null && el.Count != 0 && el[0] != null && el[0] is XmlElement)
                {
                    verifier.LoadXml((XmlElement)el[0]);
                    if (VerifyXmlSignature(verifier))
                        return ScriptSignatureStatus.Valid;
                }
                return ScriptSignatureStatus.Invalid;
            }
            catch
            {
                return ScriptSignatureStatus.Error;
            }

        }

    }

}