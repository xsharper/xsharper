using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using XS=XSharper.Core;

// This attribute is required to prevent unnecessary compilations
[assembly: XSharper.Core.XsHeadersIdentifierAttribute("665332B36FC8FA1ED11210CDEE83B639B451E592")]





namespace Generated {

    // Generated XSharper script class
    public class $safeprojectname$
    {
        XS.Script _script;
        public XS.Script Script { get { return _script; } }

        public $safeprojectname$()
        {
             _script=new XS.Script((System.Diagnostics.Process.GetCurrentProcess().MainModule!=null)?System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName:null   ){
				Parameters = new List<XS.CommandLineParameter>() ,
				Items = new List<XS.IScriptAction> {
					new XS.Print(){
						Value = @"Hello, world!"
					}
				} ,
				Id = @"myscript"
			};
		}
		#region -- Code snippets --

		#endregion -- Code snippets --
	}

}
