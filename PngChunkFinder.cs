using System;
using System.IO;

namespace IllCardFilter
{
    public class PngChunkFinder
    {
        private static readonly byte[] PngEndChunk = { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };

        private static int IndexOf(byte[] buffer, byte[] pattern)
        {
            for (int i = 0; i <= buffer.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

        public static long SearchPngEnd(Stream stream)
        {
            if (stream == null || !stream.CanRead)
            {
                throw new ArgumentException("Stream is null or cannot be read.");
            }

            stream.Seek(0, SeekOrigin.Begin);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            var index = IndexOf(buffer, PngEndChunk);
            return index == -1 ? -1 : index + PngEndChunk.Length;
        }

        public static bool IsInvalidPngEnd(long pngEnd, long streamLength)
        {
            return pngEnd == -1 || pngEnd > streamLength;
        }
    }
}