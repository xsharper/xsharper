<!-- Setup script -->
<script id="setup" requireAdmin='true'>
	<include id="eform" from="#/gui-menu.xsh" />

	<call subid="gui-menu">
	  <param>XSharper template installation</param>
	  <param>Installs XSharper project template into Visual Studio 2008</param>
	  <param>script</param>
	</call>

	<!-- Nested script, invoked by GUI -->
	<script id='script'>
		<embed from='template.zip' />
		<embed from='..\XshCodeGenerator\bin\Release\XshCodeGenerator.dll' />
		<print>Reading registry...</print>

		<versionInfo  title='Install' tr='trim'>
			Installs XSharper code generation template into Visual Studio 2008
		</versionInfo>
		
		<set path="${=.RegistryGet('HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\9.0\UserProjectTemplatesLocation',null)}" />
		<if isNull='${path}'>
			<throw>Cannot find user project templates registry setting</throw>
		</if>

		<path path='${path}' param='Visual C#' operation='combine' existence='createDirectory' backslash='remove' outTo='path' />
		<download from='embed:///template.zip' to='${path}\xsharper.zip' />
		<download from='embed:///XshCodeGenerator.dll' to='${path}\XshCodeGenerator.dll' />
		<set qp="${=.QuoteArg($path+'\XshCodeGenerator.dll')}" />

		
		<print>Registering ${qp}...</print>
		<shell mode='batch' outTo='^out' errorTo='^error'>
			%WINDIR%\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe ${qp} 
			gacutil /u XshCodeGenerator 2>nul
			gacutil /i ${qp}
		</shell>

		<print>Template installed to ${path}\xsharper.zip . Please restart Visual Studio and create a new C# project.</print>

	</script>
</script>
