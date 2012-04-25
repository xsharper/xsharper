<xsharper>
    <versionInfo title="gui-progress" value="Run XSharper script in a window with a progress bar." Version="0.1.0.0" Copyright="(C) 2009 DeltaX Inc." />

	<param name="filename" required="1" />
	<param name="arguments" default="${=null}" />
	<param name="title" default="XSharper script" />

    <include id="myScript" from="${filename}" dynamic="true" />
	<return>${=
		gui_Progress('myScript', .SplitArgs($arguments), $title);
	}</return>

	<sub id="gui_Progress">
		<param name="scriptid" required="1" />
		<param name="arguments" default="${=new string[0]}" />
		<param name="title" default="XSharper script" />
	<?_ Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
		ProgressForm p=new ProgressForm();
		p.Script=c.Find<XS.Script>(c.GetString("scriptid"),true);
		p.Arguments=c.GetStringArray("arguments");
		p.Context=c;
		p.Text=c.GetStr("title");
        Application.Run(p);
		return p.ExitCode;
	?>
	</sub>

	<reference name="System.Windows.Forms" addUsing="true" />
	<reference name="System.Drawing" addUsing="true" />
	<?h class ProgressForm : Form
    {
		public XS.ScriptContext Context { get;set; }
		public XS.Script Script { get; set; }
		public string[] Arguments { get;set;}
		public int ExitCode { get; private set;}
        EventHandler<XS.OutputEventArgs> _oldOut;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.Label info;

        public ProgressForm()
        {
            this.SuspendLayout();
            this.progress = new System.Windows.Forms.ProgressBar();
           	this.progress.Location = new System.Drawing.Point(13, 13);
			this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(342, 23);

            this.info = new System.Windows.Forms.Label();
			this.info.AutoEllipsis = true;
			this.info.Location = new System.Drawing.Point(13, 43);
            this.info.Name = "info";
            this.info.Size = new System.Drawing.Size(342, 23);

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(367, 71);
            this.ControlBox = false;
            this.Controls.Add(this.info);
            this.Controls.Add(this.progress);
            this.Name = "ProgressForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Load += new System.EventHandler(this.ProgressForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Progress_FormClosing);
            this.ResumeLayout(false);
        }
        private void OnOutput(object sender1, XS.OutputEventArgs e1)
		{
			if (e1.OutputType==XS.OutputType.Out || e1.OutputType==XS.OutputType.Bold)
				info.Text=e1.Text;
			progress.Value=Context.GetInt("progress",0);
			if (_oldOut!=null)
				_oldOut.Invoke(sender1,e1);
		}

        private void Progress_FormClosing(object sender, FormClosingEventArgs e)
        {
			if (_running.WaitOne(0,false))
				e.Cancel=true;
		}

        private System.Threading.ManualResetEvent _running = new System.Threading.ManualResetEvent(false);

		private void ProgressForm_Load(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker)delegate() { run(); });
		}

		void run()
		{
            _oldOut=Context.Output;
            EventHandler<XS.OperationProgressEventArgs> oldProgress=Context.Progress;
			ExitCode=-1;
            try
            {
                _running.Set();
                Context.Output= OnOutput;
                Cursor = Cursors.WaitCursor;
                Context.Progress= delegate(object sender1, XS.OperationProgressEventArgs e1)  {  Application.DoEvents();  };
                using (XS.ConsoleRedirector r=new XS.ConsoleRedirector(Context)) 
                {
                    Context.In=TextReader.Null;
                    ExitCode = XS.Utils.To< int? >( Context.ExecuteScript(Script, Arguments, XS.CallIsolation.High) )??0;
                }
            }
			catch(XS.ScriptTerminateException te)
            {
                Context.WriteException(te);
				progress.Value=100;
				ExitCode = te.ExitCode;
                Context.ResetAbort();
				MessageBox.Show(te.Message,this.Text, MessageBoxButtons.OK,		MessageBoxIcon.Hand);
            }    
            catch(Exception ex)
            {
                Context.WriteException(ex);
				progress.Value=100;
				MessageBox.Show(ex.Message,this.Text, MessageBoxButtons.OK,		MessageBoxIcon.Hand);
                ExitCode = -1;
            }
			_running.Reset();            
            Context.Output=_oldOut;
            Context.Progress=oldProgress;
			Close();
        }
    }
?>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>bycF8nN4mwRivPKPi8cLgrb/d08=</DigestValue></Reference></SignedInfo><SignatureValue>ZJ/GpQZ6dCb44r649XNL/9+jKelwZWH7qXQuFpK5tT38Xg1Nff7VqDJBxtXt1Ce6VcFpJYAtAuEm/QKLssMCHyck74O4Rh4iA1GSuMMXvA9SGFb19UEdUvHQrTmMG1LSfX8gHf+LnbcYm2oeeFFLXbtsdVn9owCcD3N8oXvd98g=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>