<?xml version="1.0" encoding="utf-8"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0">
  <usage options="ifHelp ifNoArguments" />
  <param name="args" count="multiple" required="true" last="true" />
  <param description="Usage: timer &lt;command with parameters&gt;" />
  <timer outTo="x">
    <shell ignoreExitCode="true">${=.QuoteArgs($args)}</shell>
  </timer>
  <print outTo="^bold">Elapsed time: ${x}</print>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>b8+EE3nsCZvvdTPlTJ2skBY/vzo=</DigestValue></Reference></SignedInfo><SignatureValue>Qfv/CtQyNDU+Y75DknfUJoknlVRJMoVe/MhWh5KX6ZgBXjdtiDGue+gPPxQQhPmq1uiT0wTKdMglkPx4R9Jvd6bxC73shzr67Pnl5Jgt+mKDLz+5vq7+xIl0hVhBfEOw+DXTSEkDRgqhDR/rP1rq58vGL24cD8f2Ui2NCs2dRvM=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>