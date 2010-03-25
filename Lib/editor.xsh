	<?xml version="1.0" encoding="utf-8"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0" ID="Editor" unknownSwitches="true">
	<versionInfo title="XSharper Editor" value="Edit any XSharper script in interactive mode." Version="0.1.0.0" Copyright="(C) 2009 DeltaX Inc." />
	<usage options="ifNoArguments default" />
	<param name="filename" required="true" value="Script file to edit" />
	<param name="args" required="false" value="Command line arguments for the edited script" count="multiple" last="true" />

<?_ Application.EnableVisualStyles();
	Application.SetCompatibleTextRenderingDefault(false);
	
	EditorForm f1=new EditorForm();
	f1.Filename=c.GetString("filename");
	
	StringBuilder sb=new StringBuilder();
	if (c.IsSet("args"))
		f1.Args=XS.Utils.QuoteArgs(c.GetStringArray("args"));
	Application.Run(f1);

?>

<reference name="System.Windows.Forms" />
<reference name="System.Drawing" />
<?header using System.Windows.Forms;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data;
	using System.Diagnostics;
	using System.Drawing;
	using System.IO;
	using System.Text;
	using System.Windows.Forms;
	using XSharper.Core;
	using System.Xml;
	using System.Runtime.InteropServices;
	using System.Text.RegularExpressions;	
	using System.Threading;
	
	public  class EditorForm : Form
	{
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
	        this.Icon=Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.edCode = new MyTextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.reOut = new OutputRichTextBox();
            this.edArgs = new MyTextBox();
            this.btnRun = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.edDir = new MyTextBox();
            this.chkDebug = new System.Windows.Forms.CheckBox();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.edCode);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tableLayoutPanel1);
            this.splitContainer1.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.splitContainer1.Size = new System.Drawing.Size(979, 501);
            this.splitContainer1.SplitterDistance = 326;
            this.splitContainer1.TabIndex = 0;
            // 
            // edCode
            // 
            this.edCode.AcceptsReturn = true;
            this.edCode.AcceptsTab = true;
            this.edCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.edCode.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.edCode.Location = new System.Drawing.Point(0, 0);
            this.edCode.MaxLength = 999999999;
            this.edCode.Multiline = true;
            this.edCode.Name = "edCode";
            this.edCode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.edCode.Size = new System.Drawing.Size(979, 326);
            this.edCode.TabIndex = 0;
            this.edCode.WordWrap = false;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.edDir, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.reOut, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.edArgs, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.btnRun, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.chkDebug, 2, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 27F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(979, 171);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // reOut
            // 
            this.reOut.BackColor = System.Drawing.Color.Black;
            this.tableLayoutPanel1.SetColumnSpan(this.reOut, 3);
            this.reOut.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reOut.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.reOut.ForeColor = System.Drawing.Color.White;
            this.reOut.Location = new System.Drawing.Point(3, 56);
            this.reOut.Name = "reOut";
            this.reOut.ReadOnly = true;
            this.reOut.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.reOut.Size = new System.Drawing.Size(973, 112);
            this.reOut.TabIndex = 7;
            this.reOut.Text = "";
            this.reOut.ShortcutsEnabled = true;
            // 
            // edArgs
            // 
            this.edArgs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.edArgs.Location = new System.Drawing.Point(109, 3);
            this.edArgs.Name = "edArgs";
            this.edArgs.Size = new System.Drawing.Size(786, 20);
            this.edArgs.TabIndex = 2;
            // 
            // btnRun
            // 
            this.btnRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRun.Location = new System.Drawing.Point(901, 29);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(75, 21);
            this.btnRun.TabIndex = 6;
            this.btnRun.Text = "&Run";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 26);
            this.label1.TabIndex = 1;
            this.label1.Text = "&Arguments:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(3, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 23);
            this.label2.TabIndex = 4;
            this.label2.Text = "&Starting directory:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // edDir
            // 
            this.edDir.Dock = System.Windows.Forms.DockStyle.Fill;
            this.edDir.Location = new System.Drawing.Point(109, 29);
            this.edDir.Name = "edDir";
            this.edDir.Size = new System.Drawing.Size(786, 20);
            this.edDir.TabIndex = 5;
            // 
            // chkDebug
            // 
            this.chkDebug.AutoSize = true;
            this.chkDebug.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkDebug.Location = new System.Drawing.Point(901, 3);
            this.chkDebug.Name = "chkDebug";
            this.chkDebug.Size = new System.Drawing.Size(75, 20);
            this.chkDebug.TabIndex = 3;
            this.chkDebug.Text = "&Debug";
            this.chkDebug.UseVisualStyleBackColor = true;
            // 
            // EditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(979, 501);
            this.Controls.Add(this.splitContainer1);
            this.Name = "EditorForm";
            this.Text = "";
            this.Load += new System.EventHandler(this.EditorForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EditorForm_FormClosing);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TextBox edCode;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private OutputRichTextBox reOut;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Label label1;
        private MyTextBox edArgs;
        private MyTextBox edDir;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkDebug;
        
        private const int EM_SETTABSTOPS = 0x00CB;
        
        public string Filename;
        public string Args;

        
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr h,int msg,int wParam,int[] lParam);
        private ManualResetEvent _running = new ManualResetEvent(false);
        private ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private bool _closeOnStop = false;

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr h, int msg, int wParam, int lParam);


        
        public EditorForm()
        {
            InitializeComponent();
        }

        
		private void EditorForm_Load(object sender, EventArgs e)
        {
            // Change font & tab stops
            reOut.LoadFonts();
            edCode.Font = reOut.Font;
            SendMessage(edCode.Handle, EM_SETTABSTOPS, 1, new int[] {  2*4 });

            this.Text=Path.GetFullPath(Filename);
            if (File.Exists(Filename))
	   		    this.Icon=Icon.ExtractAssociatedIcon(Filename);

           
            ScriptContext ctx=new ScriptContext(null);
            if (File.Exists(Filename))
            {
           		edCode.Text=ctx.ReadText(Filename);
           	}
           	else
           	{
           		this.Text+=" (new)";
           		edCode.Text=@"<?xml version=""1.0"" encoding=""utf-8""?"+">"+@"
<xsharper xmlns=""http://www.xsharper.com/schemas/1.0"">
</xsharper>";
           	}	
            edCode.SelectionStart = 0;
            edCode.SelectionLength = 0;
            
            edArgs.Text = Args;
            edDir.Text = Directory.GetCurrentDirectory();
        }
        private bool Save(bool close)
        {
        	try {
				if (edCode.Modified)
				{
					using (StreamWriter sw = new StreamWriter(Filename, false))
					{
						sw.Write(edCode.Text);
					}
					this.Text=Path.GetFullPath(Filename);
					edCode.Modified=false;
				}
			}
			catch (Exception ex)
			{
				
				MessageBoxButtons btn=MessageBoxButtons.OK;
				string message=ex.Message;
				if (close)
				{
					message+=Environment.NewLine+Environment.NewLine+"Close without saving?";
					btn=MessageBoxButtons.YesNo;
					
				}
				DialogResult res=MessageBox.Show(message, "Error when saving script",  btn, MessageBoxIcon.Error, close?MessageBoxDefaultButton.Button2:MessageBoxDefaultButton.Button1);
				if (close && res!=DialogResult.Yes)
					return false;
				return true;
			}
			return true;
        }

        ScriptContext ctx = new ScriptContext(System.Reflection.Assembly.GetEntryAssembly());
		private void btnRun_Click(object sender, EventArgs e)
        {
			Save(false);        	
            if (_running.WaitOne(0,false))
            {
                _stopEvent.Set();
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();
            object ret = "";
            string oldText = btnRun.Text;
            string oldDir = Directory.GetCurrentDirectory();
            try
            {
                _stopEvent.Reset();
                _running.Set();
                btnRun.Text = "&Cancel";
                btnRun.Update();
                reOut.Clear();

                Directory.SetCurrentDirectory(edDir.Text);
                ctx.MinOutputType = chkDebug.Checked ? OutputType.Debug : OutputType.Info;
                ctx.Output= OnOutput;

                Cursor = Cursors.WaitCursor;
                
                ctx.Progress = OnProgress;

                // Run it!
                XmlReaderSettings rs=new XmlReaderSettings();
                rs.IgnoreWhitespace = false;
                rs.ConformanceLevel = ConformanceLevel.Fragment;
                
                string text=edCode.SelectedText;
                if (text.Length==0)
                {
                   	text=edCode.Text;
                   	ctx.Clear();
                }
                Script s = ctx.LoadScript(XmlReader.Create(new StringReader(text),rs), Filename);
                ctx.Initialize(s);
                using (XS.ConsoleRedirector r=new XS.ConsoleRedirector(ctx)) 
                {
                   	ctx.In=TextReader.Null;
	                ret=ctx.ExecuteScript(s, XS.Utils.SplitArgs(edArgs.Text), CallIsolation.None);
	            }
            }
            catch(Exception ex)
            {
                ctx.WriteException(ex);
                ret = "-1";
            }
            
            Directory.SetCurrentDirectory(oldDir);
            _running.Reset();
            btnRun.Text = oldText;
            ctx.Info.WriteLine("--- Completed in "+sw.Elapsed+" with return value="+ret+" ---");
            reOut.ScrollToBottom();
            Cursor = Cursors.Arrow;
            if (_closeOnStop)
                Close();

        }
        
        private void OnProgress(object sender1, OperationProgressEventArgs e1)
        {
            Application.DoEvents();
            if (_stopEvent.WaitOne(0,false))
            {
                e1.Cancel = true;
                _stopEvent.Reset();
            }
        }
        private void OnOutput(object sender1, OutputEventArgs e1)
        {
    	    reOut.Output(e1.OutputType,e1.Text);
      	}
        
        

        private void EditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
        	if (!Save(true))
        	{
        		e.Cancel=true;
				return;
        	}
            if (_running.WaitOne(0,false))
            {
                Text = "Cancelling script...";
                _closeOnStop = true;
                e.Cancel = true;
                
                return;
            }
        }
        
        public class MyTextBox : System.Windows.Forms.TextBox
		{
			protected override void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
			{
				if (e.Control && (e.KeyCode == System.Windows.Forms.Keys.A))
				{
					this.SelectAll();
					e.SuppressKeyPress = true;
					e.Handled = true;
				}
				else
					base.OnKeyDown(e);
			}
		}

	}
	
	// Extended richedit box, that supports different fonts and backspace character
	public  class OutputRichTextBox : RichTextBox
	{
        private Font _font,_fontBold;

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr h, int msg, int wParam, int lParam);
        
		private Font createFont(bool bold)
		{
			Font f=new Font("Consolas", 9, bold?FontStyle.Bold:FontStyle.Regular);
            if (f.Name != "Consolas")
                f = new Font("Courier New", 9, bold?FontStyle.Bold:FontStyle.Regular);

            return f;
		}
		
		public void LoadFonts() 
		{
			_font=createFont(false);
            _fontBold=createFont(true);
            this.Font = _font;
		}
		public void ScrollToBottom()
		{
			SendMessage(this.Handle, 0x115, 7, 0);
		}
        
        public void Output(OutputType otype, string text)
        {        
    	    this.Select(this.TextLength,0);
            StringBuilder sb=new StringBuilder();
            foreach (char ch in text)
            {
            	if (ch==(char)8)
            	{
            		if (sb.Length>0)
	            		this.AppendText(sb.ToString());
	            	while (this.TextLength>0)
	            	{
						this.Select(this.TextLength-1,1);	            	
						string s=this.SelectedText;
						if (s=="\n" || s=="\r" || s=="")
						{
							this.Select(this.TextLength,0);
							break;
						}
						this.Select(this.TextLength-1,1);	            	
						this.ReadOnly=false;
						this.SelectedText=string.Empty;
						this.ReadOnly=true;
					}
            		sb.Length=0;
            	}
            	else
            	{
            		if (sb.Length==0)
            		{
						switch (otype)
						{
							case OutputType.Debug:	this.SelectionColor = Color.Cyan;	break;
							case OutputType.Error:  this.SelectionColor = Color.Yellow; break;
							case OutputType.Info:	this.SelectionColor = Color.LightGreen; break;
							case OutputType.Bold:   this.SelectionColor = Color.White; break;
							default:				this.SelectionColor = Color.LightGray; break;
						}
						if (otype==OutputType.Bold)
						{
							if (this.SelectionFont.Bold!=true)
								this.SelectionFont=_fontBold;
						}
						else if (this.SelectionFont.Bold!=false)
							this.SelectionFont=_font;
					}
            		sb.Append(ch);
            		if (sb.Length>5000)
            		{
      					this.AppendText(sb.ToString());
      					sb.Length=0;
      				}
      					
            	}
			}
			this.AppendText(sb.ToString());

			ScrollToBottom();
            Application.DoEvents();
        }
	}
?>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>ncSKFmqLL76jDeDDWq//pva2HVk=</DigestValue></Reference></SignedInfo><SignatureValue>mg3grTGyBA2bETD8qGNjPr0XDz2K78wsPgrldZpGaKkiuVNDFhOToKkFs95HkJWNsFs2sHcJE8oYzZdXr89hDOQebiJHZkyDSGVo7I4l2Lko3U3TgFcuqIMhJP6D6ngLISkd2fn5qspd20mOV49NKUhcKRFSGUFMltSlaz1s0+A=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>
