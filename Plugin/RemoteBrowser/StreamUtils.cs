using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLRBrowserSourcePlugin.RemoteBrowser
{
    static class StreamUtils
    {
        public static int CopyStream(Stream input, Stream output, int bytesToRead)
        {
            byte[] buffer = new byte[32768];
            int remaining = bytesToRead;
            int read = 0;
            while (remaining > 0 &&
                   (read = input.Read(buffer, 0, Math.Min(buffer.Length, remaining))) > 0)
            {
                output.Write(buffer, 0, read);
                remaining -= read;
            }

            return bytesToRead - remaining;
        }
    }
}
