using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StdioThrouputTest
{
    class StdioTest : TestBase
    {
        protected override string TestType => Program.STDIO_TEST;

        protected override void SetChildIO(Process child)
        {
            io.ChildInput = child.StandardInput.BaseStream;
            io.ChildOutput = child.StandardOutput.BaseStream;
        }

        protected override void SetIO()
        {
            io.Input = new BufferedStream(Console.OpenStandardInput(), Program.BUFFER_SIZE);
            io.Output = new BufferedStream(Console.OpenStandardOutput(), Program.BUFFER_SIZE);
        }
    }
}
