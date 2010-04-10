<xsharper >
	<versionInfo title='SDX Builder'
				 value='XSharper SFX builder'
				 company='DeltaX Inc.'
				 version='1.0.0.0' />
	<usage options='default|ifNoArguments' />
	<param name="source" required="true">Source directory containing the program to be installed</param>
	<param name="destination.exe" required="true">Output executable file</param>
	<param switch="command" default="setup.exe" required="false">Command to execute</param>
	<param switch="params" default="" required="false" count='forceSingle'>Parameters to the command</param>
	<param switch="description" default='Click Run to execute the program' required="false">Program description</param>

	<set tmpDir="${%TEMP%}\tmp+${=guid.NewGuid()}" />
	<path path="${tmpDir}" existence="createDirectory" />
	<block>
		<try>

			<!-- Compress to tmp.zip -->
			<zip from="${source}" to="${tmpDir}\data.zip" recursive="1">
				<print nl="false" outTo="^info">Compressing ${}...</print>
				<finally>
					<print outTo="^info" >Done</print>
				</finally>
			</zip>

			<!-- Save setup to file -->
			<xmldoc id="set" xmlns="">
				<config />
			</xmldoc>
			<eval>
				$~set['//config'].SetAttribute('command',c['command']);
				$~set['//config'].SetAttribute('params',c['params']);
			</eval>
			<print>${=$~set.Save(c.Expand('${tmpDir}\config.xml'))}</print>
	
			<!-- Save script to file -->
			<eval>
				$~setup.Add(new XS.Embed('data.zip'));
				$~setup.Add(new XS.Embed('config.xml'));

				c.Find('Software Installation').VersionInfo.set_Value(${description|''});
				$~setup.Save(c.Expand('${tmpDir}\gen.xsh'));
			</eval>

			<shell outTo='^out' errorTo='^error'>xsharper.exe
				<param>${tmpDir}\gen.xsh</param>
				<param>//debug</param>
				<param>//genexe</param>
				<param>${destination.exe}</param>
			</shell>

			<print>${destination.exe} compiled successfully.</print>

		</try>
		<finally>
			<delete from="${tmpDir}" recursive='true' />
		</finally>
	</block>

	<!-- Setup script -->
	<script id="setup" requireAdmin='true'>
		<include id="eform" from="#/gui-console.xsh" />
		<versionInfo  />

		<xmldoc id='config' from='embed:///config.xml' />

		<call subid="run-in-gui-console">
		  <param>Software installation</param>
	      <param>${=$~config.V('/config/@params')}</param>
		</call>

		<!-- Nested script, invoked by GUI -->
		<script id='Software installation' switchPrefixes=''>
			<param name='args' last='1' default='' count='multiple' />
			<xmldoc id='config' from='embed:///config.xml' />
			<set command="${=$~config.V('/config/@command')} ${=.QuoteArgs($args)}" />
			<set tmpDir="${%TEMP%}\tmp+${=guid.NewGuid()}" />
			<path path="${tmpDir}" existence="createDirectory" />
	    	<block>
				<try>
					<unzip from='embed:///data.zip' to='${tmpDir}'>
						<print nl="false" outTo="^info">${=new string(8,200)}Extracting ${}...</print>
					</unzip>
					<print />
					<print outTo="^info">${=new string(8,200)}Extraction completed</print>
					<print>Executing '${command}'</print>
					<shell directory='${tmpDir}'>${command}</shell>
    			</try>
				<finally>
					<delete from="${tmpDir}" recursive='true' />
				</finally>
			</block>
		</script>
	</script>

		
</xsharper>

	