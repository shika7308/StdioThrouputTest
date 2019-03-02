using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StdioThrouputTest
{
    class AnonymousPipeTest : TestBase
    {
        protected override string TestType => Program.ANONYMOUS_PIPE_TEST;

        AnonymousPipeServerStream childInputPipe;
        AnonymousPipeServerStream childOutputPipe;
        string childInputHandle;
        string childOutputHandle;
        AnonymousPipeClientStream parentInputPipe;
        AnonymousPipeClientStream parentOutputPipe;
        string parentInputHandle;
        string parentOutputHandle;

        public AnonymousPipeTest(string input, string output)
        {
            parentInputHandle = output;
            parentOutputHandle = input;
        }

        protected override void BeforeChildStart()
        {
            base.BeforeChildStart();
            childInputPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable, Program.BUFFER_SIZE);
            childOutputPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable, Program.BUFFER_SIZE);
            childInputHandle = childInputPipe.GetClientHandleAsString();
            childOutputHandle = childOutputPipe.GetClientHandleAsString();
        }

        protected override Process CreateChild(int count)
        {
            var ret = base.CreateChild(count);
            ret.StartInfo.Arguments += $" {childInputHandle} {childOutputHandle}";
            
            ret.StartInfo.RedirectStandardInput = false;
            ret.StartInfo.RedirectStandardOutput = false;
            ret.StartInfo.RedirectStandardError = false;
            return ret;
        }

        protected override void SetChildIO(Process child)
        {
            io.ChildInput = childInputPipe;
            io.ChildOutput = childOutputPipe;
        }

        protected override void SetIO()
        {
            parentOutputPipe = new AnonymousPipeClientStream(PipeDirection.In, parentOutputHandle);
            parentInputPipe = new AnonymousPipeClientStream(PipeDirection.Out, parentInputHandle);
            io.Input = parentOutputPipe;
            io.Output = parentInputPipe;
        }

        protected override void WaitForChild()
        {
            childInputPipe.DisposeLocalCopyOfClientHandle();
            childOutputPipe.DisposeLocalCopyOfClientHandle();
        }
    }
}
