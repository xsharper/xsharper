<xsharper xmlns="http://www.xsharper.com/schemas/1.0">
	<set exe="${=System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}" />
	<set dir="${=.BackslashRemove(Path.GetDirectoryName(c.GetString('exe')))}" />
      
  <!-- This will copy the files to my working machine -->
	<block>
		<print>Files copied</print>

		<if directoryExists="c:\util">
			<print>Copying "${exe}" to c:\util</print>
			<copy from="${exe}" to="c:\util\" />
			
			<print>Generating schema in c:\util</print>
			<shell mode="direct">"${exe}" //quiet //genxsd c:\util\xsharper.xsd</shell>
			
			<print>Files copied</print>
		</if>

		<if directoryExists="c:\my\utils">
			<print>Copying "${exe}" to c:\my\utils</print>
			<copy from="${exe}" to="c:\my\utils" />
			
			<print>Generating schema in c:\my\utils</print>
			<shell mode="direct">"${exe}" //quiet //genxsd c:\my\utils\xsharper.xsd</shell>
			
			<print>Files copied</print>
		</if>

    <if directoryExists="c:\my\util">
      <print>Copying "${exe}" to c:\my\util</print>
      <copy from="${exe}" to="c:\my\util" />

      <print>Generating schema in c:\my\util</print>
      <shell mode="direct">"${exe}" //quiet //genxsd c:\my\util\xsharper.xsd</shell>

      <print>Files copied</print>
    </if>

  </block>
	<print>Completed</print>
</xsharper>
