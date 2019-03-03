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
                Console.WriteLine("Preparing write buffer...\n");
                var wData = GenerateTestData();

                var bitShift = new[] { 10, 12, 14, 16, 18 };

                foreach (var shift in bitShift)
                {
                    BLOCK_SIZE = 1 << shift;
                    new StdioTest().RunRoot(wData);
                    Console.WriteLine();
                    Thread.Sleep(1000);

                    BLOCK_SIZE = 1 << shift;
                    new NamedPipeTest().RunRoot(wData);
                    Console.WriteLine();
                    Thread.Sleep(1000);

                    BLOCK_SIZE = 1 << shift;
                    new AnonymousPipeTest(null, null).RunRoot(wData);
                    Console.WriteLine();
                    Thread.Sleep(1000);
                }

                Console.WriteLine("Press any key to exit.");
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

        static byte[] GenerateTestData()
        {
            var ret = new byte[DATA_SIZE];
            new Random().NextBytes(ret);
            return ret;
        }
    }
}
