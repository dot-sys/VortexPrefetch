using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Prefetch
{
    // Handles prefetch reading
    public static partial class PrefetchAnalyzer
    {
        // Compression format Xpress identifier
        private const ushort CompressionFormatXpress = 0x0003;
        // Compression format Xpress Huffman identifier
        private const ushort CompressionFormatXpressHuffman = 0x0004;
        // Compression engine maximum flag
        private const ushort CompressionEngineMaximum = 0x0100;

        // Try opening prefetch from file path
        private static bool TryOpenPrefetch(string filePath, out IPrefetch prefetch)
        {
            prefetch = null;

            if (TryReadCompressedPrefetch(filePath, out var decompressed) &&
                TryOpenFromBytes(decompressed, filePath, out prefetch))
            {
                return true;
            }

            return TryReadAllBytes(filePath, out var bytes) &&
                   TryOpenFromBytes(bytes, filePath, out prefetch);
        }

        // Try opening prefetch from byte array
        private static bool TryOpenFromBytes(byte[] bytes, string filePath, out IPrefetch prefetch)
        {
            prefetch = null;

            if (TryOpenPatchedWin11(bytes, filePath, out prefetch))
            {
                return true;
            }

            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    prefetch = PrefetchFile.Open(ms, filePath);
                }

                if (prefetch != null)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (IsUnknownVersion(ex))
                {
                    return TryOpenPatchedWin11(bytes, filePath, out prefetch);
                }
            }

            return TryOpenFromTempFile(bytes, out prefetch);
        }

        // Try opening patched Windows 11 prefetch
        private static bool TryOpenPatchedWin11(byte[] bytes, string filePath, out IPrefetch prefetch)
        {
            prefetch = null;

            if (bytes == null || bytes.Length < 4)
            {
                return false;
            }

            var version = BitConverter.ToUInt32(bytes, 0);
            if (version != 0x1F)
            {
                return false;
            }

            var patched = (byte[])bytes.Clone();
            patched[0] = 0x1E;
            patched[1] = 0x00;
            patched[2] = 0x00;
            patched[3] = 0x00;

            try
            {
                using (var ms = new MemoryStream(patched))
                {
                    prefetch = PrefetchFile.Open(ms, filePath);
                }

                return prefetch != null;
            }
            catch
            {
                prefetch = null;
                return false;
            }
        }

        // Try opening prefetch from temporary file
        private static bool TryOpenFromTempFile(byte[] bytes, out IPrefetch prefetch)
        {
            prefetch = null;

            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pf");

            try
            {
                File.WriteAllBytes(tempPath, bytes);
                prefetch = PrefetchFile.Open(tempPath);
                return prefetch != null;
            }
            catch
            {
                prefetch = null;
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                }
            }
        }

        // Try reading compressed prefetch file
        private static bool TryReadCompressedPrefetch(string filePath, out byte[] decompressed)
        {
            decompressed = null;

            if (!TryReadAllBytes(filePath, out var bytes))
            {
                return false;
            }

            if (!IsCompressed(bytes))
            {
                return false;
            }

            var uncompressedSize = BitConverter.ToInt32(bytes, 4);
            if (uncompressedSize <= 0)
            {
                return false;
            }

            var compressedData = new byte[bytes.Length - 8];
            Buffer.BlockCopy(bytes, 8, compressedData, 0, compressedData.Length);

            if (TryDecompress(compressedData, uncompressedSize, (ushort)(CompressionFormatXpressHuffman | CompressionEngineMaximum), out decompressed))
            {
                return true;
            }

            return TryDecompress(compressedData, uncompressedSize, (ushort)(CompressionFormatXpress | CompressionEngineMaximum), out decompressed);
        }

        // Try reading all bytes from file
        private static bool TryReadAllBytes(string filePath, out byte[] bytes)
        {
            try
            {
                bytes = File.ReadAllBytes(filePath);
                return true;
            }
            catch
            {
                bytes = null;
                return false;
            }
        }

        // Try decompressing buffer with specified format
        private static bool TryDecompress(byte[] compressedBuffer, int expectedSize, ushort format, out byte[] decompressed)
        {
            decompressed = null;

            if (compressedBuffer == null || compressedBuffer.Length == 0 || expectedSize <= 0)
            {
                return false;
            }

            var status = RtlGetCompressionWorkSpaceSize(format, out var workspaceSize, out var fragmentSize);
            if (status != 0)
            {
                return false;
            }

            var workspaceLength = Math.Max(workspaceSize, fragmentSize);
            var buffer = new byte[expectedSize];
            IntPtr workspace = IntPtr.Zero;

            try
            {
                workspace = Marshal.AllocHGlobal((IntPtr)workspaceLength);

                status = RtlDecompressBufferEx(format, buffer, (uint)buffer.Length, compressedBuffer, (uint)compressedBuffer.Length, out var finalSize, workspace);
                if (status != 0 || finalSize != buffer.Length)
                {
                    return false;
                }
            }
            finally
            {
                if (workspace != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(workspace);
                }
            }

            decompressed = buffer;
            return true;
        }

        // Check if buffer is compressed
        private static bool IsCompressed(byte[] bytes)
        {
            return bytes != null && bytes.Length > 8 && bytes[0] == 'M' && bytes[1] == 'A' && bytes[2] == 'M';
        }

        // Check if exception is unknown version
        private static bool IsUnknownVersion(Exception ex)
        {
            return ex != null && ex.Message != null && ex.Message.IndexOf("Unknown version", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Get workspace size for compression
        [DllImport("ntdll.dll", SetLastError = false, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
        private static extern int RtlGetCompressionWorkSpaceSize(ushort compressionFormatAndEngine, out uint compressBufferWorkSpaceSize, out uint compressFragmentWorkSpaceSize);

        // Decompress buffer using native API
        [DllImport("ntdll.dll", SetLastError = false, CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
        private static extern int RtlDecompressBufferEx(ushort compressionFormatAndEngine, byte[] uncompressedBuffer, uint uncompressedBufferSize, byte[] compressedBuffer, uint compressedBufferSize, out uint finalUncompressedSize, IntPtr workSpace);
    }
}
