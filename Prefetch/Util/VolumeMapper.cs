using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;

namespace Prefetch
{
    // Maps volume identifiers
    internal static class VolumeResolver
    {
        // Lock for cache synchronization
        private static readonly object CacheLock = new object();
        // Cache of serial to drive mappings
        private static readonly Dictionary<string, string> _volumeCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Flag indicating cache built
        private static bool _cacheBuilt;

        // Build volume mapping cache safely
        public static void BuildCache()
        {
            lock (CacheLock)
            {
                if (_cacheBuilt)
                {
                    return;
                }

                _volumeCache.Clear();

                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT DeviceID, DriveLetter, SerialNumber FROM Win32_Volume WHERE DriveLetter IS NOT NULL"))
                    {
                        foreach (var volumeObject in searcher.Get())
                        {
                            var volume = volumeObject as ManagementObject;
                            if (volume == null)
                            {
                                continue;
                            }

                            var deviceId = volume["DeviceID"] as string;
                            var driveLetter = volume["DriveLetter"] as string;
                            var serialNumber = volume["SerialNumber"];

                            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(driveLetter) || serialNumber == null)
                            {
                                continue;
                            }

                            if (!TryConvertSerial(serialNumber, out var serial))
                            {
                                continue;
                            }

                            var serialKey = serial.ToString("X8");

                            if (!_volumeCache.ContainsKey(serialKey))
                            {
                                _volumeCache[serialKey] = driveLetter.TrimEnd('\\') + "\\";
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    _volumeCache.Clear();
                }
                finally
                {
                    _cacheBuilt = true;
                }
            }
        }

        // Resolve device path to drive letter
        public static string ResolvePath(string prefetchPath)
        {
            if (string.IsNullOrEmpty(prefetchPath))
            {
                return prefetchPath;
            }

            BuildCache();

            var trimmed = prefetchPath.Trim();
            var volumeEnd = trimmed.IndexOf('}');
            if (volumeEnd == -1)
            {
                return trimmed;
            }

            var filePart = trimmed.Substring(volumeEnd + 1);
            var serialKey = ExtractSerialKeyFromVolumePath(trimmed);

            if (!string.IsNullOrEmpty(serialKey) && _volumeCache.TryGetValue(serialKey, out var driveLetter))
            {
                return driveLetter + filePart.TrimStart('\\');
            }

            return trimmed;
        }

        // Register volume serial mapping
        public static void RegisterVolumeSerial(string devicePath, uint serialNumber)
        {
            if (string.IsNullOrEmpty(devicePath))
            {
                return;
            }

            BuildCache();

            var serialKey = serialNumber.ToString("X8");
            lock (CacheLock)
            {
                if (!_volumeCache.ContainsKey(serialKey))
                {
                    var drive = TryGetDriveLetter(devicePath);
                    _volumeCache[serialKey] = string.IsNullOrEmpty(drive) ? devicePath : drive;
                }
            }
        }

        // Extract drive letter from path
        private static string TryGetDriveLetter(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length < 2)
            {
                return null;
            }

            if (char.IsLetter(path[0]) && path[1] == ':')
            {
                return path.Substring(0, 2).TrimEnd(':') + "\\";
            }

            return null;
        }

        // Extract serial key from volume path
        private static string ExtractSerialKeyFromVolumePath(string volumePath)
        {
            if (string.IsNullOrEmpty(volumePath))
            {
                return null;
            }

            var start = volumePath.IndexOf('{');
            var end = volumePath.IndexOf('}');
            if (start == -1 || end == -1 || end <= start)
            {
                return null;
            }

            var inner = volumePath.Substring(start + 1, end - start - 1);
            var dashIndex = inner.LastIndexOf('-');
            var serialCandidate = dashIndex >= 0 ? inner.Substring(dashIndex + 1) : inner;

            if (string.IsNullOrWhiteSpace(serialCandidate))
            {
                return null;
            }

            serialCandidate = serialCandidate.Trim();
            if (serialCandidate.Length > 8)
            {
                serialCandidate = serialCandidate.Substring(serialCandidate.Length - 8);
            }

            return uint.TryParse(serialCandidate, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var serial)
                ? serial.ToString("X8")
                : null;
        }

        // Convert serial value to uint
        public static bool TryConvertSerial(object serialValue, out uint serial)
        {
            serial = 0;

            try
            {
                if (serialValue is uint u)
                {
                    serial = u;
                    return true;
                }

                if (serialValue is int i && i >= 0)
                {
                    serial = (uint)i;
                    return true;
                }

                var serialString = serialValue as string;
                if (!string.IsNullOrEmpty(serialString))
                {
                    uint parsed;
                    if (uint.TryParse(serialString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed))
                    {
                        serial = parsed;
                        return true;
                    }

                    if (uint.TryParse(serialString, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        serial = parsed;
                        return true;
                    }
                }

                serial = Convert.ToUInt32(serialValue, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
