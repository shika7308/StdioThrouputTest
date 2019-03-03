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
        
        protected readonly StreamIO io = new StreamIO();

        public void RunRoot(byte[] wData)
        {
            echo($"Test target      : {TestType}");
            echo($"No. of chain     : {Program.NUM_OF_CHAIN}");
            echo($"Data size        : {Program.DATA_SIZE.ToString("#,0")}");
            echo($"Block size       : {Program.BLOCK_SIZE.ToString("#,0")}");
            echo($"Buffer size      : {Program.BUFFER_SIZE.ToString("#,0")}");

            //echo("Preparing read buffer...");
            var rData = new byte[wData.Length];
            
            //echo("Starting child process...");
            BeforeChildStart();
            var child = CreateChild(Program.NUM_OF_CHAIN - 1);
            child.Start();
            SetChildIO(child);

            //echo("Waiting child initialize...");
            WaitForChild();

            //echo("Start process!");
            var time = 0.0;
            using (io)
            using (child)
            {
                var t1 = Stopwatch.GetTimestamp(); 

                var writeTask = Task.Run(() =>
                {
                    var blockNum = (Program.DATA_SIZE - 1) / Program.BLOCK_SIZE + 1;
                    for (var bi = 0; bi < blockNum; ++bi)
                    {
                        var offset = Program.BLOCK_SIZE * bi;
                        var count = Math.Min(Program.BLOCK_SIZE, Program.DATA_SIZE - offset);
                        io.ChildInput.Write(wData, offset, count);
                        io.ChildInput.Flush();
                    } 
                });

                var readSum = 0;
                while (readSum < Program.DATA_SIZE)
                {
                    var offset = readSum; 
                    var count = Math.Min(Program.BLOCK_SIZE, Program.DATA_SIZE - offset); 
                    readSum += io.ChildOutput.Read(rData, offset, count);
                }

                var t2 = Stopwatch.GetTimestamp(); 
                time = (double)(t2 - t1) / Stopwatch.Frequency;

                if (writeTask.IsFaulted)
                    throw writeTask.Exception;
            }

            var missNum = 0;
            for (var i = 0; i < wData.Length; ++i)
            {
                if (wData[i] != rData[i])
                    missNum++;
            }

            //echo("");
            echo($"Time             : {time}");
            echo($"Throughput       : {((long)(Program.DATA_SIZE / time * Program.NUM_OF_CHAIN * 2)).ToString("#,0")} bytes/s");
            echo($"Total Throughput : {((long)(Program.DATA_SIZE / time)).ToString("#,0")} bytes/s");
            echo($"Miss rate        : {(float)missNum * 100 / Program.DATA_SIZE:F2} %");
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
                var child = CreateChild(count - 1);
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
            info.Arguments = $"{TestType} {count} {Program.BLOCK_SIZE}";
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
