using System;

namespace FxSsh.Algorithms
{
    public abstract class CompressionAlgorithm
    {
        public abstract byte[] Compress(byte[] input);

        public abstract ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> input);
    }
}
