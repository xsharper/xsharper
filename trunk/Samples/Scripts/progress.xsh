<?xml version="1.0" encoding="utf-8"?>
<!-- This script demonstrates a custom user action -->

<xsharper xmlns="http://www.xsharper.com/schemas/1.0">
    <?ht // -- Custom action --

	using XSharper.Core;
		
	[XsType("progress","uri://mysite.com/testc1")]
	public class Progress : ValueBase
	{
		// width of console
		static int s_width;
    	static Progress()
		{
			if (Utils.HasRealConsole)
               s_width = Console.BufferWidth-1;
            else
               s_width = 80;
		}
		public override object Execute()
		{
			object o= base.Execute();
			if (o==null)
			{	
				string v=(Utils.FitWidth(Context.ExpandStr(Value),s_width,FitWidthOption.EllipsisEnd)+new string(' ',s_width)).Substring(0,s_width);
				Context.Info.Write(new string('\x08',s_width));
				Context.Info.Write(v);
				Context.Info.Write(new string('\x08',s_width));
			}
			return o;
		}
	}?>


	<set i="0" />
	<try>
	  <dir from='c:\' recursive='true'>
		  <set i="${ = (int)$i+1 }" />
		  <progress>${i}: ${}</progress>
		  <if condition='${ =$i==50000 }'>
			<break />
		   </if>			
		</dir>
	</try>
	<finally>
		<progress>${=null}</progress>
	</finally>
	

</xsharper>