<xsharper>
	<include id="sign" from="sign-snk.xsh" />
	<include id="ftp" from="ftpUpload.xsh" />

	<path operation="combine" path="${=Environment.GetFolderPath('MyDocuments')}" param="Sites\xsharper.site" outTo="siteFile" />
	<readtext from="${siteFile}" outTo="site" encoding="ascii" />  

	<set toUpload="${=new ArrayList()}" />
	<print outTo="^bold">Signing files</print>
	<dir from="." filter="^[a-z].*\.xsh">
		<set>${=$toUpload.Add($.fullName)}</set>
		<print>${=$.fullName}</print>
		<redirect outTo="^info" infoTo="^nul">	
			<exec includeId="sign">
				<param>S:\myDocs\Sites\DeltaX.snk</param>
				<param>${}</param>
			</exec>
		</redirect>
			
		<catch>
			<print>CATCH: ${=c.CurrentException}</print>
			<throw/>
		</catch>
	</dir>

	<print outTo="^bold">Uploading files</print>
	<exec includeId="ftp" isolation="high">
		<param value="ftp://${site}/lib/" />
		<param value="${toUpload}" />
	</exec>

		
</xsharper>
