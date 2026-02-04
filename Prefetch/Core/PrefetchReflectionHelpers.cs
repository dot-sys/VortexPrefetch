using System;
using System.Collections;
using System.Collections.Generic;

namespace Prefetch
{
    // Reflection helpers
    public static partial class PrefetchAnalyzer
    {
        // Register volume information via reflection
        private static bool TryRegisterVolume(object volume)
        {
            if (volume == null)
            {
                return false;
            }

            try
            {
                var type = volume.GetType();
                var deviceProp = type.GetProperty("DevicePath")
                                ?? type.GetProperty("DeviceName")
                                ?? type.GetProperty("VolumeName");
                var serialProp = type.GetProperty("SerialNumber")
                                ?? type.GetProperty("VolumeSerialNumber");

                var devicePath = deviceProp?.GetValue(volume) as string;
                var serialValue = serialProp?.GetValue(volume);

                if (string.IsNullOrEmpty(devicePath) || serialValue == null)
                {
                    return false;
                }

                if (VolumeResolver.TryConvertSerial(serialValue, out var serial))
                {
                    VolumeResolver.RegisterVolumeSerial(devicePath, serial);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        // Extract enumerable of strings from property
        private static bool TryGetStringEnumerable(object source, string propertyName, out IEnumerable<string> values)
        {
            values = null;
            if (source == null)
            {
                return false;
            }

            try
            {
                var prop = source.GetType().GetProperty(propertyName);
                if (prop == null)
                {
                    return false;
                }

                if (prop.GetValue(source) is IEnumerable enumerable)
                {
                    var list = new List<string>();
                    foreach (var item in enumerable)
                    {
                        if (item is string s && !string.IsNullOrWhiteSpace(s))
                        {
                            list.Add(s);
                        }
                    }

                    values = list;
                    return list.Count > 0;
                }
            }
            catch
            {
            }

            return false;
        }

        // Extract enumerable of objects from property
        private static bool TryGetEnumerable(object source, string propertyName, out IEnumerable<object> values)
        {
            values = null;
            if (source == null)
            {
                return false;
            }

            try
            {
                var prop = source.GetType().GetProperty(propertyName);
                if (prop == null)
                {
                    return false;
                }

                if (prop.GetValue(source) is IEnumerable enumerable)
                {
                    var list = new List<object>();
                    foreach (var item in enumerable)
                    {
                        if (item != null)
                        {
                            list.Add(item);
                        }
                    }

                    values = list;
                    return list.Count > 0;
                }
            }
            catch
            {
            }

            return false;
        }

        // Retrieve string property safely
        private static string TryGetString(object source, string propertyName)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                var prop = source.GetType().GetProperty(propertyName);
                if (prop == null)
                {
                    return null;
                }

                return prop.GetValue(source) as string;
            }
            catch
            {
                return null;
            }
        }
    }
}
