using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace FxSsh
{
    public static class NetworkStreamExtensions
    {
        public static async Task<int> ReadByteAsync(this NetworkStream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[1];
            var bytesRead = await stream.ReadAsync(buffer, 0, 1, cancellationToken);
            return bytesRead == 0 ? -1 : buffer[0];
        }
    }
}
