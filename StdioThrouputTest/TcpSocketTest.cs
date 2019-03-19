using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace StdioThrouputTest
{
    class TcpSocketTest : TestBase
    {
        protected override string TestType => Program.TCP_SOCKET_TEST;

        const string ipAddr = "127.0.0.1";

        int childPort;
        TcpListener childListener;
        int parentPort;
        TcpClient parentClient;

        public TcpSocketTest(int port)
        {
            parentPort = port;
            childPort = port + 1;

            childListener = new TcpListener(IPAddress.Parse(ipAddr), childPort);
            childListener.Start();
        }

        protected override Process CreateChild(int count)
        {
            var ret = base.CreateChild(count);
            ret.StartInfo.Arguments += $" {childPort}";

            ret.StartInfo.RedirectStandardInput = false;
            ret.StartInfo.RedirectStandardOutput = false;
            ret.StartInfo.RedirectStandardError = false;
            return ret;
        }

        protected override void SetChildIO(Process child)
        {
            // I/O is available after waiting for child            
        }

        protected override void SetIO()
        {
            parentClient = new TcpClient(ipAddr, parentPort);
            parentClient.SendBufferSize = Program.BUFFER_SIZE;
            parentClient.ReceiveBufferSize = Program.BUFFER_SIZE;
            var cStream = parentClient.GetStream();
            io.Input = cStream;
            io.Output = cStream;
        }

        protected override void WaitForChild()
        {
            var server = childListener.AcceptTcpClient();
            server.SendBufferSize = Program.BUFFER_SIZE;
            server.ReceiveBufferSize = Program.BUFFER_SIZE;
            var sStream = server.GetStream();
            io.ChildInput = sStream;
            io.ChildOutput = sStream;
        }

        public override void Dispose()
        {
            base.Dispose();
            
            childListener?.Stop();
            parentClient?.Close();
            parentClient?.Dispose();
        }
    }
}
