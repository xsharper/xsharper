<xsharper>
    <versionInfo title="Sign-SNK" value="Sign XSH script with a provided SNK key file" version="0.0.0.1" />
    <usage options="ifNoArguments default" />
    <param name="key" value="SNK key" required="true" />
    <param name="data" value="Script file" required="true" />
    <reference name="System.Security" />
    <?h using System;
        using System.Text;
        using System.Security.Cryptography;
        using System.Security.Cryptography.Xml;
    ?>
    <?_ RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        rsa.ImportCspBlob(System.IO.File.ReadAllBytes((string)c["key"]));

        string filename=(string)c["data"];
        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = true;
        doc.Load(filename);
        c.Info.WriteLine("Loaded "+filename);
                
        SignedXml signer=  new SignedXml(doc); 
            
        var el=doc.GetElementsByTagName("Signature");
        if (el!=null && el.Count!=0 && el[0]!=null && el[0] is XmlElement)
            el[0].ParentNode.RemoveChild(el[0]);

        
        signer.KeyInfo = new KeyInfo();
        signer.KeyInfo.AddClause(new RSAKeyValue(rsa));
        signer.SigningKey = rsa; 
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
    

        
    ?>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>fRHwe+Lq0WFUtE3A3itoTbx4sEA=</DigestValue></Reference></SignedInfo><SignatureValue>ZRa3Tr8sJBfHrNpXZorHjqkeaAiSmO7VFfhDuE2uD6taOqc1VNoFARzVrZndba7IGKM2Iy9CLlsKpk09wiBxuMAN/TNuyqfxsJQ2gyFjkiGQ6SRQJYYYAt/DCfKaK3Dm+4m5ukW3WeeqLnOYvXExIPikiSDFlH5zy7wkfJZep8c=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>