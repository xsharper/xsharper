<?xml version="1.0" encoding="utf-8"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0" unknownSwitches="true">
	<versionInfo title="gui-console" value="Run XSharper script in a normal Window." Version="0.1.0.0" Copyright="(C) 2009 DeltaX Inc." />
	<usage options="ifNoArguments default" />

	<param switch="run" required="false" value="Run immediately after loading" count="none" />
	<param name="filename" required="true" value="Script to execute" description="filename.xsh" />
	<param name="args" required="false" value="Command line arguments for the script" count="multiple" description="arguments" last="true" />

	<include id="myScript" from="${filename}" dynamic="true" />
	<call subId="run-in-gui-console">
		<param>${=$~myScript.IncludedScript}</param>
		<param>${=.QuoteArgs(${args|=null})}</param>
	</call>

<sub id="run-in-gui-console">
	<param name="script" required="true" />
	<param name="scriptArgs" required="true" />

	<?_ Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		ExecutorForm f1=new ExecutorForm();
		f1.Context=c;
		if (c["script"] is XS.Script)
			f1.Script=(XS.Script)c["script"];
		else
			f1.Script=c.Find<Script>(c.GetString("script"),true);

		f1.Args=c.GetString("scriptArgs");
		f1.Autorun=c.GetBool("run",false);
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

	
	public  class ExecutorForm : Form
	{
        private ManualResetEvent _running = new ManualResetEvent(false);
        private ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private bool _closeOnStop = false;

        public ScriptContext Context;
        public Script Script;
        public string Args;
        public bool Autorun;
        

        public ExecutorForm()
        {
            InitializeComponent();
        }

		
        private void Form1_Load(object sender, EventArgs e)
        {
		    this.Icon=Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
	    
            // Change font & tab stops
			reOut.LoadFonts();            
            
            
            lblDescription.Text = Script.VersionInfo.GenerateInfo(Context,true);

            string title=Context.TransformStr(Script.VersionInfo.Title,Script.VersionInfo.Transform);
            if (string.IsNullOrEmpty(title))
            	title=Script.Id;
           	if (string.IsNullOrEmpty(title))
            	title=Script.Location;
            if (string.IsNullOrEmpty(title))
            	Text = "XSharper";
            else
	            Text = title+" - XSharper";
            edArgs.Text=Args ?? "";
            edArgs.SelectionStart=0;
            edArgs.SelectionLength=0;
	        if (Autorun)
				BeginInvoke((MethodInvoker)delegate() { click(); });
        }

        void btnRun_Click(object sender, EventArgs e)
        {
        	click();
        }
        
        void click()
        {
            if (_running.WaitOne(0,false))
            {
                _stopEvent.Set();
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();
            object ret = null;
            string oldText = btnRun.Text;
            string oldDir = Directory.GetCurrentDirectory();
            EventHandler<OutputEventArgs> oldOut=Context.Output;
            EventHandler<OperationProgressEventArgs> oldProgress=Context.Progress;
            
            try
            {
                _stopEvent.Reset();
                _running.Set();
                btnRun.Text = "&Cancel";
                btnRun.Update();
                reOut.Clear();
                
                Context.MinOutputType = chkDebug.Checked ? OutputType.Debug : OutputType.Info;
                Context.Output= OnOutput;
                Cursor = Cursors.WaitCursor;

                Context.Progress= delegate(object sender1, OperationProgressEventArgs e1)
                {
                    Application.DoEvents();
                    if (_stopEvent.WaitOne(0,false))
                    {
                        e1.Cancel = true;
						_stopEvent.Reset();                      
					}
                };
                
                using (XS.ConsoleRedirector r=new XS.ConsoleRedirector(Context)) 
                {
                	Context.In=TextReader.Null;
	                ret = Context.ExecuteScript(Script, XS.Utils.SplitArgs(edArgs.Text), CallIsolation.High);
				}
            }
            catch(Exception ex)
            {
                Context.WriteException(ex);
                ret = -1;
            }
            
                 
            Directory.SetCurrentDirectory(oldDir);
            _running.Reset();
            btnRun.Text = oldText;
            Context.Info.WriteLine("--- Completed in "+sw.Elapsed+" with return value="+ret+" ---");
			reOut.ScrollToBottom();
		    Context.Progress=oldProgress;
           	Context.Output=oldOut;
            Cursor = Cursors.Arrow;
            Context.Progress = null;
            Context.Output = null;
            if (_closeOnStop)
                Close();
        }

        

        private void OnOutput(object sender1, OutputEventArgs e1)
        {
        	reOut.Output(e1.OutputType,e1.Text);
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_running.WaitOne(0,false))
            {
                Text = "Cancelling script...";
                _stopEvent.Set();
                _closeOnStop = true;
                e.Cancel = true;
                
                return;
            }
        }
        

        
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.reOut = new OutputRichTextBox();
            this.edArgs = new System.Windows.Forms.TextBox();
            this.btnRun = new System.Windows.Forms.Button();
            this.chkDebug = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.reOut, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.edArgs, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.btnRun, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.chkDebug, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblDescription, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(3);
            this.tableLayoutPanel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(979, 501);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(6, 3);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 23);
            this.label2.TabIndex = 0;
            this.label2.Text = "Script description:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDescription
            // 
            this.lblDescription.AccessibleRole = System.Windows.Forms.AccessibleRole.Document;
            this.lblDescription.AutoSize = true;
            this.lblDescription.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDescription.Location = new System.Drawing.Point(112, 3);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(780, 23);
            this.lblDescription.TabIndex = 1;
            this.lblDescription.Text = "...";
            this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(6, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 26);
            this.label1.TabIndex = 2;
            this.label1.Text = "&Arguments:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            // 
            // edArgs
            // 
            this.edArgs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.edArgs.Location = new System.Drawing.Point(112, 29);
            this.edArgs.Name = "edArgs";
            this.edArgs.Size = new System.Drawing.Size(780, 20);
            this.edArgs.TabIndex = 3;
            // 
            // btnRun
            // 
            this.btnRun.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRun.Location = new System.Drawing.Point(898, 29);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(75, 21);
            this.btnRun.TabIndex = 4;
            this.btnRun.Text = "&Run";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            
            // 
            // chkDebug
            // 
            this.chkDebug.AutoSize = true;
            this.chkDebug.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.chkDebug.Location = new System.Drawing.Point(898, 6);
            this.chkDebug.Name = "chkDebug";
            this.chkDebug.Size = new System.Drawing.Size(75, 17);
            this.chkDebug.TabIndex = 5;
            this.chkDebug.Text = "&Debug";
            this.chkDebug.UseVisualStyleBackColor = true;
            
            // 
            // reOut
            // 
            this.reOut.BackColor = System.Drawing.Color.Black;
            this.tableLayoutPanel1.SetColumnSpan(this.reOut, 3);
            this.reOut.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reOut.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.reOut.ForeColor = System.Drawing.Color.White;
            this.reOut.Location = new System.Drawing.Point(6, 56);
            this.reOut.Name = "reOut";
            this.reOut.ReadOnly = true;
            this.reOut.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.reOut.Size = new System.Drawing.Size(967, 439);
            this.reOut.TabIndex = 6;
            this.reOut.Text = "";
            this.reOut.ShortcutsEnabled = true;            
            
            
            // 
            // ExecutorForm
            // 
            this.AcceptButton = this.btnRun;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ExecutorForm";
            this.Text = " Sample Script Executor";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private OutputRichTextBox reOut;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.CheckBox chkDebug;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox edArgs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblDescription;

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
</sub>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>enAFsEe71/Qap9h73//wo5YBHME=</DigestValue></Reference></SignedInfo><SignatureValue>gz6nFmqvc/qR0ZZmUWM2oBpgtvdYw007+q5M9Uv6xK33Yd3fvY1TXtStyeMTCGidPVTgZG8bVWwtCFVqBgvd2DjII9T0TuUXlQJJFKOgAUNv2JoNXtrzFgMUl616loepdUpY1aQVPdsaxdRkkQc9yB3HwLg1aRa/d7+bBftpXnI=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>