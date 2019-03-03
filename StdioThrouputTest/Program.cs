using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace StdioThrouputTest
{
    class Program
    {
        public const string STDIO_TEST = "stdio";
        public const string NAMED_PIPE_TEST = "named-pipe";
        public const string ANONYMOUS_PIPE_TEST = "anonymous-pipe";
        public const string TCP_SOCKET_TEST = "tcp";
        public const int NUM_OF_CHAIN = 1;
        public const int DATA_SIZE = 1 << 30; // Max value is 1 << 30 (1,073,741,824 bytes) 
        public static int BLOCK_SIZE;
        public static int BUFFER_SIZE => BLOCK_SIZE * 10;

        static string exePath;
        static void Main(string[] args)
        {
            exePath = Assembly.GetExecutingAssembly().Location;

            if (args.Length == 0)
            {
                for (var i = 0; i < 5; i++)
                {
                    BLOCK_SIZE = 1 << (12 + (i * 2));

                    new StdioTest().RunRoot();
                    Console.WriteLine();
                    Thread.Sleep(5000);
                }
                for (var i = 0; i < 5; i++)
                {
                    BLOCK_SIZE = 1 << (12 + (i * 2));
                    new NamedPipeTest().RunRoot();
                    Console.WriteLine();
                    Thread.Sleep(5000);
                }
                for (var i = 0; i < 5; i++)
                {
                    BLOCK_SIZE = 1 << (12 + (i * 2));
                    new AnonymousPipeTest(null, null).RunRoot();
                    Console.WriteLine();
                    Thread.Sleep(5000);
                }
                Console.ReadKey();
            }
            else
            {
                BLOCK_SIZE = int.Parse(args[2]);
                var test = default(TestBase);
                switch (args[0])
                {
                    case STDIO_TEST: test = new StdioTest(); break;
                    case NAMED_PIPE_TEST: test = new NamedPipeTest(); break;
                    case ANONYMOUS_PIPE_TEST: test = new AnonymousPipeTest(args[3], args[4]); break;
                }
                test.RunChild(int.Parse(args[1]));
            }
        }
    }
}
