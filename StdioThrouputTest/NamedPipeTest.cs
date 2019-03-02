using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StdioThrouputTest
{
    class NamedPipeTest : TestBase
    {
        protected override string TestType => Program.NAMED_PIPE_TEST;

        NamedPipeClientStream childInputPipe;
        NamedPipeServerStream childOutputPipe;
        NamedPipeClientStream parentInputPipe;
        NamedPipeServerStream parentOutputPipe;

        protected override Process CreateChild(int count)
        {
            var ret = base.CreateChild(count);
            ret.StartInfo.RedirectStandardInput = false;
            ret.StartInfo.RedirectStandardOutput = false;
            ret.StartInfo.RedirectStandardError = false;
            return ret;
        }

        protected override void SetChildIO(Process child)
        {
            var pipeName = $"Named-Pipe-Test-{io.Count}";
            childInputPipe = new NamedPipeClientStream(".", $"{pipeName}-parent-child", PipeDirection.Out, PipeOptions.None, System.Security.Principal.TokenImpersonationLevel.Impersonation);
            childOutputPipe = new NamedPipeServerStream($"{pipeName}-child-parent", PipeDirection.In, 1);
            io.ChildInput = childInputPipe;
            io.ChildOutput = childOutputPipe;
        }

        protected override void SetIO()
        {
            var pipeName = $"Named-Pipe-Test-{io.Count + 1}";
            parentOutputPipe = new NamedPipeServerStream($"{pipeName}-parent-child", PipeDirection.In, 1);
            parentInputPipe = new NamedPipeClientStream(".", $"{pipeName}-child-parent", PipeDirection.Out, PipeOptions.None, System.Security.Principal.TokenImpersonationLevel.Impersonation);
            io.Input = new BufferedStream(parentOutputPipe, Program.BUFFER_SIZE);
            io.Output = new BufferedStream(parentInputPipe, Program.BUFFER_SIZE);
        }

        protected override void WaitForChild()
        {
            childOutputPipe.WaitForConnection();
            childInputPipe.Connect();
        }

        protected override void WaitForParent()
        {
            parentInputPipe.Connect();
            parentOutputPipe.WaitForConnection();
        }
    }
}
