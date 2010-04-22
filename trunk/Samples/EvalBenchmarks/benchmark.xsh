<!-- This script requires XSharper ( http://xsharper.com/xsharper.exe ) -->
<script>
<?h using XSharper.Core ?>
<?_
	var context=new BasicEvaluationContext();
	context.Variables["v_t"]="T";

	IOperation tree=null;
	var timer=System.Diagnostics.Stopwatch.StartNew();
	int loops=200000;
	for (int i=0;i<loops;++i)
	{
		tree=new Parser().Parse(new ParsingReader(@"
                 ( ( $v_t == 'B' ) ? 'bus'.Length : 
                   ( $v_t == 'A' ) ? 'airplane'.Length+10 : 
	               ( $v_t == 'T' ) ? 'train'.Length+100 : 
	               ( $v_t == 'C' ) ? 'car'.Length +1000: 
	               ( $v_t == 'H' ) ? 'horse'.Length +10000: 
                    'feet'.Length+100000 );"));	
	}
	Console.WriteLine("Parsing took {0}, or {1} parsings per second",timer.Elapsed, (long)(loops/timer.Elapsed.TotalSeconds) );

	timer=System.Diagnostics.Stopwatch.StartNew();
	string res=null;
	var stack=new Stack<object>();
	for (int i=0;i<loops;++i)	
	{
		tree.Eval(context, stack);
		res=Utils.To<string>(stack.Pop());
	}	
	Console.WriteLine("Evaluation took {0}, or {1} evaluations per second",timer.Elapsed, (long)(loops/timer.Elapsed.TotalSeconds) );
	Console.WriteLine("Result="+res);	
?>
</script>

