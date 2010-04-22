using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using XSharper.Core;
using System.Diagnostics;

namespace DumpDemo
{
    class Program
    {
        class TreeElement
        {
            private readonly string _name;

            public TreeElement(string name)
            {
                _name = name;
            }
            public TreeElement(string name, TreeElement left, TreeElement right)
            {
                _name = name;
                Left = left;
                Right = right;
            }
            public TreeElement Left {  get;set;}
            public TreeElement Right { get; set; }

            public TreeElement BadProperty
            {
                get { return new TreeElement(_name); }
            }
            public bool SideEffectProperty
            {
                get
                {
                    Console.WriteLine("**! Side Effect of {0}!**",ToString());
                    return false;
                }
            }
            public override string ToString()
            {
                return "Test('"+_name+"')";
            }
            public void InvalidApi(int x, string y, TreeElement z)
            {
                try
                {
                    if (x<5)
                        throw new ArgumentOutOfRangeException("x",x,"Parameter should be larger than 5");
                }   
                catch (Exception e)
                {
                    Console.WriteLine("---------------\nException {0}\nWhen calling {1}\nWith parameters:\n{2}\n---------------\n ",
                        e.Message,
                        new StackTrace(true).GetFrame(0).ToString(),
                        new Dump(new {x,y,z}));
                }
            }
        }

        
        static void Main(string[] args)
        {
            // Dump information about the current executable file
            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);

            // Dumping file info
            Console.WriteLine("--- FileInfo for the current assembly ---");
            Console.WriteLine(Dump.ToDump(fi,"FileInfo"));

            // Dumping a temp object
            Console.WriteLine();
            Console.WriteLine("--- Dumping a temp object ---");
            Console.WriteLine(Dump.ToDump(new 
                {
                    A="Hello, world",
                    B=21.45,
                    C=new object[]  
                        {
                          new { X=20},
                          new { Y=20},
                        }
                }, "Dump for temp class"));

            // Array
            Console.WriteLine();
            Console.WriteLine("--- Array ---");
            Console.WriteLine(Dump.ToDump(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.System))));

            // Byte array
            Console.WriteLine();
            Console.WriteLine("--- Byte Array ---");
            Console.WriteLine(Dump.ToDump(Encoding.UTF8.GetBytes("Hello, world!")));
             
            // Add hidden property
            Dump.AddHiddenProperty(typeof(TreeElement), "SideEffectProperty");

            // Dumping a bad object (long tree)

            Console.WriteLine();
            Console.WriteLine("--- Object with broken property ---");
            Console.WriteLine(Dump.ToDump(new TreeElement("Before")));

            // Fixing the bad property
            Dump.AddBloatProperty(typeof(TreeElement),"BadProperty");
            Console.WriteLine();
            Console.WriteLine("--- Object with opaque property ---");
            Console.WriteLine(Dump.ToDump(new TreeElement("Before")));

            // Tree with loops
            TreeElement root=new TreeElement("0-Root");
            TreeElement left = new TreeElement("1-Left",root,null);
            TreeElement right = new TreeElement("1-Right", new TreeElement("2-Right-Left"), new TreeElement("2-Right-Right"));
            root.Left = left;
            root.Right = right;
            Console.WriteLine();
            Console.WriteLine("--- Object tree with loops ---");
            Console.WriteLine(Dump.ToDump(root));

            // Function call
            Console.WriteLine();
            Console.WriteLine("--- Function call throwing exception ---");
            root.InvalidApi(3,"Hello",left);

            Console.WriteLine("=== Press Enter to close ===");
            Console.ReadLine();
        }
    }
}
