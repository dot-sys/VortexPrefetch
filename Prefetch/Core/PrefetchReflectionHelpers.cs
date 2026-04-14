namespace Prefetch
{
    public static partial class PrefetchAnalyzer
    {
        private static bool TryRegisterVolume(IPrefetchVolume volume)
        {
            if (volume == null || string.IsNullOrEmpty(volume.DevicePath))
                return false;

            VolumeResolver.RegisterVolumeSerial(volume.DevicePath, volume.SerialNumber);
            return true;
        }
    }
}
