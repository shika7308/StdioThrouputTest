using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace StdioThrouputTest
{
    class Program
    {
        public const string STDIO_TEST = "stdio";
        public const string NAMED_PIPE_TEST = "named-pipe";
        public const string ANONYMOUS_PIPE_TEST = "anonymous-pipe";
        public const string TCP_SOCKET_TEST = "tcp";
        public const int NUM_OF_CHAIN = 1;
        //public const long DATA_SIZE = 1L << 33; // 8,589,934,592 bytes
        public const long DATA_SIZE = 1L << 30; // 1,073,741,824 bytes
        public static int BLOCK_SIZE = 1 << 12; // 4,096 bytes
        //public const int BLOCK_SIZE = 1 << 14; // 16,384 bytes
        //public const int BLOCK_SIZE = 1 << 16; // 65,536 bytes
        public static int BUFFER_SIZE = BLOCK_SIZE * 10;

        static string exePath;
        static void Main(string[] args)
        {
            exePath = Assembly.GetExecutingAssembly().Location;

            if (args.Length == 0)
            {
                for (var i = 0; i < 5; i++)
                {
                    BLOCK_SIZE = 1 << (12 + (i * 2));
                    BUFFER_SIZE = BLOCK_SIZE * 10;
                    new StdioTest().RunRoot();
                    Console.WriteLine();
                    new NamedPipeTest().RunRoot();
                    Console.WriteLine();
                    new AnonymousPipeTest(null, null).RunRoot();
                    Console.WriteLine();
                }
                Console.ReadKey();
            }
            else
            {
                var test = default(TestBase);
                switch (args[0])
                {
                    case STDIO_TEST: test = new StdioTest(); break;
                    case NAMED_PIPE_TEST: test = new NamedPipeTest(); break;
                    case ANONYMOUS_PIPE_TEST: test = new AnonymousPipeTest(args[2], args[3]); break;
                }
                test.RunChild(int.Parse(args[1]));
            }
        }
    }
}
