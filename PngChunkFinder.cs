using MessagePack;

namespace IllCardFilter
{
    public class PngChunkFinder {
        // PngChunks are defined here, https://www.w3.org/TR/png-3/#3PNGsignature
        // We don't need to find PngStartChunk for this card filter, but I want to leave a reference.
        // private static readonly byte[] PngStartChunk = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        // 0x89, 0x50, 0x4E, 0x47 is IEND.
        // 0xAE 0x42 0x60 0x82 is the CRC check.
        private static readonly byte[] PngEndChunk = { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };

        // Find position of EndChunk "in the buffer", it should not greater then the buffer size.
        private static int SearchEndChunkIndex(byte[] buffer, int readLength)
        {
            var firstByteOfEndChunk = PngEndChunk[0];
            for (var bufferIndex = 0; bufferIndex < readLength; bufferIndex++)
            {
                if (buffer[bufferIndex] != firstByteOfEndChunk) continue;

                var isMatch = false;
                for (var chunkIndex = 1; chunkIndex < PngEndChunk.Length && bufferIndex + chunkIndex < readLength; chunkIndex++)
                {
                    var byteIsNotInEndChunk = buffer[bufferIndex + chunkIndex] != PngEndChunk[chunkIndex];
                    if (byteIsNotInEndChunk) break;
                    if (chunkIndex == PngEndChunk.Length - 1)
                    {
                        isMatch = true;
                    }
                }

                var notOverFlow = bufferIndex + PngEndChunk.Length <= readLength;
                if (isMatch && notOverFlow)
                {
                    return bufferIndex;
                }
            }
            return -1;
        }

        // Attempting to read the stream in chunks of bufferSize each time until the position of the endChunk is found,
        // and then return the position of last byte in endChunk.
        private static long SearchEndChunkLastBytePosition(Stream stream)
        {
            const int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
            var origPos = stream.Position;
            int readLength;
            while ((readLength = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                var endChunkIndex = SearchEndChunkIndex(buffer, readLength);
                if (endChunkIndex < 0) continue;
                var endChunkFirstBytePosition = (stream.Position - readLength) + endChunkIndex;
                var endChunkLastBytePosition = endChunkFirstBytePosition + PngEndChunk.Length;
                return endChunkLastBytePosition;
            }
            stream.Position = origPos;
            return -1;
        }

        public static long SearchPngEnd(Stream stream) => SearchEndChunkLastBytePosition(stream);

        public static bool IsInvalidPngEnd(long pngEnd, long streamLength) => pngEnd == -1 || pngEnd >= streamLength;
    }
}