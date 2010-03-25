<xsharper>
	<versionInfo title="Sign-Cert" value="Sign XSH script with a given certificate" version="0.0.0.1" />
	<usage options="ifNoArguments default" />
	<param name="data" value="Script file" description="script.xsh" required="true" />
	<param name="certSubject" value="Certificate subject" required="true" />
	<param />
	<param value="Parameters:" />
	<param />
	<param switch="machine" count="none" value="Use machine certificate store" default="0" unspecified="1" />
	<reference name="System.Security" />
	<?h using System;
		using System.Text;
		using System.Security.Cryptography;
		using System.Security.Cryptography.Xml;
		using System.Security.Cryptography.X509Certificates;
		
		
	?>
	<code><![CDATA[
		X509Store store = new X509Store("My", c.GetBool("machine")?StoreLocation.LocalMachine:StoreLocation.CurrentUser);
        try
        {
	        store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
	    	X509Certificate2Collection coll=store.Certificates.Find(X509FindType.FindBySubjectName, c.GetStr("certSubject"), false);
		    if (coll.Count==0)
		    {
		    	c.Error.WriteLine("Certificate not found");
		    	return -2;
		    }
		    if (coll.Count!=1)
		    {
		    	c.Error.WriteLine("Too many certificates found");
		    	return -2;
		    }
		    
		    X509Certificate2 cert=coll[0];
		    if (cert.PrivateKey==null)
		    {
			    c.Error.WriteLine("Certificate does not have a private key");
		    	return -2;
		    }
		    
		    PrintInfo(cert);
		    
		    X509ChainPolicy pol = new X509ChainPolicy();
            pol.RevocationMode = X509RevocationMode.NoCheck;
            pol.VerificationFlags = X509VerificationFlags.IgnoreEndRevocationUnknown | X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown | X509VerificationFlags.IgnoreRootRevocationUnknown;
            pol.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.3"));
                
            X509Chain chain = new X509Chain(true);
            chain.ChainPolicy = pol;
            if (!chain.Build(cert))
            	c.Error.WriteLine("Warning: Certificate is not trusted for CodeSigning");
	    

            string filename=(string)c["data"];
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true;
			doc.Load(filename);
				
			SignedXml signer=  new SignedXml(doc); 
		
			var el=doc.GetElementsByTagName("Signature");
			if (el!=null && el.Count!=0 && el[0]!=null && el[0] is XmlElement)
				el[0].ParentNode.RemoveChild(el[0]);

			
			signer.KeyInfo = new KeyInfo();
			signer.KeyInfo.AddClause(new KeyInfoX509Data(cert,X509IncludeOption.ExcludeRoot));
			signer.SigningKey = cert.PrivateKey; 
			signer.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NWithCommentsTransformUrl;

			XmlDsigExcC14NWithCommentsTransform canMethod = (XmlDsigExcC14NWithCommentsTransform)signer.SignedInfo.CanonicalizationMethodObject;
			canMethod.InclusiveNamespacesPrefixList = "Sign";


			Reference orderRef = new Reference("");
			orderRef.AddTransform(new XmlDsigEnvelopedSignatureTransform());
			signer.AddReference(orderRef); 
			
			signer.ComputeSignature();
			doc.DocumentElement.AppendChild(signer.GetXml());
			doc.Save(filename); 
			c.Print("Script '${=Path.GetFullPath($data)}' has been signed & saved successfully");
			return 0;
		}
		finally {
			store.Close();
		}

    ]]>
    <sub id="PrintInfo">
    	<param name="cert" required="true" />
    	<print tr="trim multiline expand" outTo="^info">
    		==== Certificate info ===
			Subject:      ${=$cert.Subject}    	
			Expiration:   ${=$cert.GetExpirationDateString()}
			Issuer:       ${=$cert.Subject}    	
			Serial:       ${=$cert.SerialNumber}
			Thumbprint:   ${=$cert.Thumbprint}
			
    	</print>
    	
    </sub>
    </code>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>GtGeNJAyqi3JL2YzzX7aiNz7fpg=</DigestValue></Reference></SignedInfo><SignatureValue>hUvaH4BKLbx0HnJjtigSyr6HoppWxgJC0S4O1pu636Dh0BH0UV2Mq06fhVvu3zQwpCO+NDPNMUcMwzoT2V9vr3JHz1Nl/fjdSob1RIvxEVHJ4R4EY8LQDvRIpfrLxnaLBP7JRnaLA8xlj6IGRRwTXHRUeUyfYk14Bt6bsCd2Lf0=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>