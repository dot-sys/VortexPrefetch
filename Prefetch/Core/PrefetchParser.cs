using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Prefetch
{
    internal static class PrefetchFile
    {
        private const uint SccaSignature = 0x41434353;

        internal static IPrefetch Open(Stream stream, string filePath)
        {
            byte[] bytes;
            try
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    bytes = ms.ToArray();
                }
            }
            catch
            {
                return null;
            }
            return Parse(bytes, filePath);
        }

        internal static IPrefetch Parse(byte[] bytes, string filePath)
        {
            if (bytes == null || bytes.Length < 120)
                return null;

            var version = BitConverter.ToUInt32(bytes, 0);
            var signature = BitConverter.ToUInt32(bytes, 4);

            if (signature != SccaSignature)
                return null;

            if (version != 30)
                return null;

            var exeName = ReadNullTerminatedUtf16(bytes, 16, 60);

            var filenameOffset = BitConverter.ToUInt32(bytes, 100);
            var filenameSize   = BitConverter.ToUInt32(bytes, 104);
            var volumesOffset  = BitConverter.ToUInt32(bytes, 108);
            var volumesCount   = BitConverter.ToUInt32(bytes, 112);

            var runTimes = new List<DateTimeOffset>();
            int runCount = 0;

            for (int i = 0; i < 8; i++)
            {
                var pos = 128 + i * 8;
                if (pos + 8 > bytes.Length) break;
                var ft = BitConverter.ToInt64(bytes, pos);
                if (ft > 0)
                    runTimes.Add(DateTimeOffset.FromFileTime(ft));
            }
            if (bytes.Length >= 212)
                runCount = (int)BitConverter.ToUInt32(bytes, 208);

            var filenames = ParseFilenameStrings(bytes, filenameOffset, filenameSize);
            var volumes   = ParseVolumes(bytes, volumesOffset, (int)volumesCount, version);

            DateTimeOffset sourceCreated  = DateTimeOffset.MinValue;
            DateTimeOffset sourceModified = DateTimeOffset.MinValue;
            DateTimeOffset sourceAccessed = DateTimeOffset.MinValue;
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    var fi = new FileInfo(filePath);
                    sourceCreated  = new DateTimeOffset(fi.CreationTime);
                    sourceModified = new DateTimeOffset(fi.LastWriteTime);
                    sourceAccessed = new DateTimeOffset(fi.LastAccessTime);
                }
            }
            catch { }

            return new PrefetchImpl
            {
                Header           = new PrefetchHeaderImpl { ExecutableFilename = exeName },
                VolumeInformation = new ReadOnlyCollection<IPrefetchVolume>(volumes),
                LastRunTimes     = new ReadOnlyCollection<DateTimeOffset>(runTimes),
                RunCount         = runCount,
                SourceFilename   = filePath ?? string.Empty,
                SourceCreatedOn  = sourceCreated,
                SourceModifiedOn = sourceModified,
                SourceAccessedOn = sourceAccessed,
                FilenameStrings  = new ReadOnlyCollection<string>(filenames)
            };
        }

        private static List<string> ParseFilenameStrings(byte[] bytes, uint offset, uint size)
        {
            var result = new List<string>();
            if (offset == 0 || size == 0 || offset + size > bytes.Length)
                return result;

            var end = (int)(offset + size);
            var pos = (int)offset;
            while (pos + 1 < end)
            {
                var nullPos = pos;
                while (nullPos + 1 < end)
                {
                    if (bytes[nullPos] == 0 && bytes[nullPos + 1] == 0)
                        break;
                    nullPos += 2;
                }
                if (nullPos > pos)
                {
                    var s = Encoding.Unicode.GetString(bytes, pos, nullPos - pos);
                    if (!string.IsNullOrWhiteSpace(s))
                        result.Add(s);
                }
                pos = nullPos + 2;
            }
            return result;
        }

        private static List<IPrefetchVolume> ParseVolumes(byte[] bytes, uint volumesOffset, int count, uint version)
        {
            var result = new List<IPrefetchVolume>();
            if (volumesOffset == 0 || count <= 0 || volumesOffset >= bytes.Length)
                return result;

            int entrySize = 96;

            for (int i = 0; i < count; i++)
            {
                var entryBase = (int)volumesOffset + i * entrySize;
                if (entryBase + 36 > bytes.Length)
                    break;

                var devicePathOffset  = BitConverter.ToUInt32(bytes, entryBase + 0);
                var devicePathLength  = BitConverter.ToUInt32(bytes, entryBase + 4);
                var serialNumber      = BitConverter.ToUInt32(bytes, entryBase + 16);
                var dirStringsOffset  = BitConverter.ToUInt32(bytes, entryBase + 28);
                var dirStringsCount   = BitConverter.ToUInt32(bytes, entryBase + 32);

                var devicePath = string.Empty;
                if (devicePathLength > 0 && devicePathOffset > 0)
                {
                    var absOff  = (int)volumesOffset + (int)devicePathOffset;
                    var byteLen = (int)devicePathLength * 2;
                    if (absOff + byteLen <= bytes.Length)
                        devicePath = Encoding.Unicode.GetString(bytes, absOff, byteLen);
                }

                var dirs = new List<string>();
                if (dirStringsCount > 0 && dirStringsOffset > 0)
                {
                    var absOff = (int)volumesOffset + (int)dirStringsOffset;
                    for (int d = 0; d < (int)dirStringsCount && absOff + 2 <= bytes.Length; d++)
                    {
                        var charCount = BitConverter.ToUInt16(bytes, absOff);
                        absOff += 2;
                        if (charCount > 0 && absOff + charCount * 2 <= bytes.Length)
                        {
                            dirs.Add(Encoding.Unicode.GetString(bytes, absOff, charCount * 2));
                            absOff += charCount * 2;
                        }
                    }
                }

                result.Add(new PrefetchVolumeImpl
                {
                    DevicePath     = devicePath,
                    SerialNumber   = serialNumber,
                    DirectoryNames = new ReadOnlyCollection<string>(dirs)
                });
            }
            return result;
        }

        private static string ReadNullTerminatedUtf16(byte[] bytes, int offset, int maxBytes)
        {
            if (offset + maxBytes > bytes.Length)
                maxBytes = bytes.Length - offset;

            var end = offset;
            while (end + 1 < offset + maxBytes)
            {
                if (bytes[end] == 0 && bytes[end + 1] == 0)
                    break;
                end += 2;
            }
            return end <= offset ? string.Empty : Encoding.Unicode.GetString(bytes, offset, end - offset);
        }
    }

    internal sealed class PrefetchHeaderImpl : IPrefetchHeader
    {
        public string ExecutableFilename { get; set; }
    }

    internal sealed class PrefetchVolumeImpl : IPrefetchVolume
    {
        public string DevicePath { get; set; }
        public uint SerialNumber { get; set; }
        public IReadOnlyList<string> DirectoryNames { get; set; }
    }

    internal sealed class PrefetchImpl : IPrefetch
    {
        public IPrefetchHeader Header { get; set; }
        public IReadOnlyList<IPrefetchVolume> VolumeInformation { get; set; }
        public IReadOnlyList<DateTimeOffset> LastRunTimes { get; set; }
        public int RunCount { get; set; }
        public string SourceFilename { get; set; }
        public DateTimeOffset SourceCreatedOn { get; set; }
        public DateTimeOffset SourceModifiedOn { get; set; }
        public DateTimeOffset SourceAccessedOn { get; set; }
        public IReadOnlyList<string> FilenameStrings { get; set; }
    }
}
