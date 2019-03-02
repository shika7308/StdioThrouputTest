using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StdioThrouputTest
{
    class StreamIO : IDisposable
    {
        public int Count;
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

    abstract class TestBase
    {
        static string exePath = Assembly.GetExecutingAssembly().Location;

        static void echo(object str) => Console.WriteLine(str);

        static byte[] GenerateTestData()
        {
            var ret = new byte[Program.BLOCK_SIZE];
            new Random().NextBytes(ret);
            return ret;
        }

        protected readonly StreamIO io = new StreamIO();

        public void RunRoot()
        {
            echo($"Test target   : {TestType}");
            echo($"No. of chain  : {Program.NUM_OF_CHAIN}");
            echo($"Data size     : {Program.DATA_SIZE.ToString("#,0")}");
            echo($"Block size    : {Program.BLOCK_SIZE.ToString("#,0")}");
            echo($"Buffer size   : {Program.BUFFER_SIZE.ToString("#,0")}");

            var data = GenerateTestData();
            var sw = new Stopwatch();

            var writeSum = 0L;
            var readSum = 0L;

            //echo("Starting child process...");
            BeforeChildStart();
            var child = CreateChild(Program.NUM_OF_CHAIN - 1);
            child.Start();
            SetChildIO(child);

            //echo("Waiting child initialize...");
            WaitForChild();

            //echo("Start process!");
            using (io)
            using (child)
            {
                var writeTask = Task.Run(() =>
                {
                    sw.Start();
                    while (writeSum < Program.DATA_SIZE)
                    {
                        io.ChildInput.Write(data, 0, Program.BLOCK_SIZE);
                        writeSum += Program.BLOCK_SIZE;
                    }
                });
                var block = new byte[Program.BLOCK_SIZE];
                while (readSum < Program.DATA_SIZE)
                {
                    var len = io.ChildOutput.Read(block, 0, Program.BLOCK_SIZE);
                    readSum += len;
                }
                sw.Stop();
                if (writeTask.IsFaulted)
                    throw writeTask.Exception;
            }

            //echo("");
            echo($"Time             : {sw.Elapsed}");
            echo($"Throughput       : {((long)(Program.DATA_SIZE / sw.Elapsed.TotalSeconds * Program.NUM_OF_CHAIN * 2)).ToString("#,0")} bytes/s");
            echo($"Total Throughput : {((long)(Program.DATA_SIZE / sw.Elapsed.TotalSeconds)).ToString("#,0")} bytes/s");
        }

        public void RunChild(int count)
        {
            io.Count = count;
            if (count == 0)
            {
                SetIO();
                WaitForParent();
                using (io)
                    DoSelfIO(io);
            }
            else
            {
                SetIO();
                WaitForParent();
                BeforeChildStart();
                var child = CreateChild(count);
                child.Start();
                SetChildIO(child);
                WaitForChild();
                using (io)
                using (child)
                    DoChildIO(io);
            }
        }

        protected virtual Process CreateChild(int count)
        {
            if (count < 0)
                return null;
            var info = new ProcessStartInfo(exePath);
            info.Arguments = $"{TestType} {count}";
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;

            var ret = new Process();
            ret.StartInfo = info;
            return ret;
        }

        protected virtual void WaitForChild() { }
        protected virtual void WaitForParent() { }
        protected abstract string TestType { get; }
        protected virtual void BeforeChildStart()
        {
            io.Count = Program.NUM_OF_CHAIN;
        }
        protected abstract void SetChildIO(Process child);
        protected abstract void SetIO(); 

        static void DoChildIO(StreamIO io)
        {
            Task.Run(() =>
            {
                var writeBlock = new byte[Program.BLOCK_SIZE];
                var writeLen = 0;
                while ((writeLen = io.ChildOutput.Read(writeBlock, 0, Program.BLOCK_SIZE)) != 0)
                {
                    io.Output.Write(writeBlock, 0, writeLen);
                    io.Output.Flush();
                }
            });
            var readBlock = new byte[Program.BLOCK_SIZE];
            var readLen = 0;
            while ((readLen = io.Input.Read(readBlock, 0, Program.BLOCK_SIZE)) != 0)
            {
                io.ChildInput.Write(readBlock, 0, readLen);
                io.ChildInput.Flush();
            }
        }

        static void DoSelfIO(StreamIO io)
        {
            var block = new byte[Program.BLOCK_SIZE];
            var len = 0;
            while ((len = io.Input.Read(block, 0, Program.BLOCK_SIZE)) != 0)
            {
                io.Output.Write(block, 0, len);
                io.Output.Flush();
            }
        }
    }
}
