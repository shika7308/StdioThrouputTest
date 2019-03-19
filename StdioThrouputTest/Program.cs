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
        public static int BLOCK_SIZE; // Max value is 1 << 27 (134,217,728 byte)
        public static int BUFFER_SIZE => BLOCK_SIZE << 3;

        static string exePath;
        static void Main(string[] args)
        {
            exePath = Assembly.GetExecutingAssembly().Location;

            if (args.Length == 0)
            {
                Console.WriteLine("Preparing write buffer...\n");

                var wData = GenerateTestData();
                var testSet = new[] { STDIO_TEST, NAMED_PIPE_TEST, ANONYMOUS_PIPE_TEST, TCP_SOCKET_TEST };
                var bitShift = new[] { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };

                foreach (var shift in bitShift)
                {
                    BLOCK_SIZE = 1 << shift;

                    foreach (var type in testSet)
                    {
                        var test = default(TestBase);
                        switch (type)
                        {
                            case STDIO_TEST: test = new StdioTest(); break;
                            case NAMED_PIPE_TEST: test = new NamedPipeTest(); break;
                            case ANONYMOUS_PIPE_TEST: test = new AnonymousPipeTest(null, null); break;
                            case TCP_SOCKET_TEST: test = new TcpSocketTest(2000); break;
                        }
                        test.RunRoot(wData);
                        test.Dispose();
                        Console.WriteLine();
                        Thread.Sleep(500);
                    }
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
                    case TCP_SOCKET_TEST: test = new TcpSocketTest(int.Parse(args[3])); break;
                }
                test.RunChild(int.Parse(args[1]));
                test.Dispose();
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
