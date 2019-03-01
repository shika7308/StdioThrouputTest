using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StdioThrouputTest
{
    class Program
    {
        const string STDIO_TEST = "stdio";
        const string NAMEDPIPE_TEST = "named";
        const string TCPSOCKET_TEST = "tcp";
        const int NUM_OF_CHANE = 1;
        //const long DATA_SIZE = 1L << 33; // 8,589,934,592 bytes
        const long DATA_SIZE = 1L << 30; // 1,073,741,824 bytes
        const int BLOCK_SIZE = 1 << 12; // 4,096 bytes
        const int BUFFER_SIZE = BLOCK_SIZE * 10;

        static string exePath;
        static void Main(string[] args)
        {
            exePath = Assembly.GetExecutingAssembly().Location;

            if (args.Length == 0)
            {
                var data = GenerateTestData();
                var sw = new Stopwatch();

                var io = new StreamIO();
                var writeSum = 0L;
                var readSum = 0L;
                var child = StartStdioChild(NUM_OF_CHANE);
                SetChildStdIO(child, ref io);
                using (child)
                using (io)
                {
                    Task.Run(() =>
                    {
                        while (writeSum < DATA_SIZE)
                        {
                            io.ChildInput.Write(data, 0, BLOCK_SIZE);
                            writeSum += BLOCK_SIZE;
                        }
                    });
                    var block = new byte[BLOCK_SIZE];
                    sw.Start();
                    while (readSum < DATA_SIZE)
                    {
                        var len = io.ChildOutput.Read(block, 0, BLOCK_SIZE);
                        readSum += len;
                    }
                    sw.Stop();
                }

                Console.WriteLine(sw.Elapsed);
                Console.WriteLine("Press any key...");
                Console.ReadKey();
            }
            else if (args[0] == STDIO_TEST)
                StartStdioChildTest(int.Parse(args[1]));
        }

        static byte[] GenerateTestData()
        {
            var ret = new byte[BLOCK_SIZE];
            new Random().NextBytes(ret);
            return ret;
        }

        static void StartStdioChildTest(int count)
        {
            var io = new StreamIO();
            if (count == 0)
            {
                SetStdIO(ref io);
                using (io)
                    DoSelfIO(io);
            }
            else
            {
                var child = StartStdioChild(count);
                SetChildStdIO(child, ref io);
                SetStdIO(ref io);
                using (child)
                using (io)
                    DoChildIO(io);
            }
        }

        static Process StartStdioChild(int count)
        {
            if (count <= 0)
                return null;
            var info = new ProcessStartInfo(exePath);
            info.Arguments = $"{STDIO_TEST} {count - 1}";
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;

            return Process.Start(info);
        }

        static void SetChildStdIO(Process child, ref StreamIO io)
        {
            io.ChildInput = child.StandardInput.BaseStream;
            io.ChildOutput = child.StandardOutput.BaseStream;
        }

        static void SetStdIO(ref StreamIO io)
        {
            io.Input = new BufferedStream(Console.OpenStandardInput(), BUFFER_SIZE);
            io.Output = new BufferedStream(Console.OpenStandardOutput(), BUFFER_SIZE);
        }

        static void DoChildIO(StreamIO io)
        {
            Task.Run(() =>
            {
                var readBlock = new byte[BLOCK_SIZE];
                var readLen = 0;
                while ((readLen = io.Input.Read(readBlock, 0, BLOCK_SIZE)) != 0)
                {
                    io.ChildInput.Write(readBlock, 0, readLen);
                    io.ChildInput.Flush();
                }
            });
            var writeBlock = new byte[BLOCK_SIZE];
            var writeLen = 0;
            while ((writeLen = io.ChildOutput.Read(writeBlock, 0, BLOCK_SIZE)) != 0)
            {
                io.Output.Write(writeBlock, 0, writeLen);
                io.Output.Flush();
            }
        }

        static void DoSelfIO(StreamIO io)
        {
            var block = new byte[BLOCK_SIZE];
            var len = 0;
            while ((len = io.Input.Read(block, 0, BLOCK_SIZE)) != 0)
            {
                io.Output.Write(block, 0, len);
                io.Output.Flush();
            }
        }
    }

    struct StreamIO : IDisposable
    {
        public Stream ChildInput;
        public Stream ChildOutput;
        public Stream Input;
        public Stream Output;

        public void Dispose()
        {
            ChildInput?.Dispose();
            ChildOutput?.Dispose();
            Input?.Dispose();
            Output?.Dispose();
        }
    }
}
