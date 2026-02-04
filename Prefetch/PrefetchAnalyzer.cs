using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Prefetch.Models;

namespace Prefetch
{
    // PrefetchAnalyzer orchestrator
    public static partial class PrefetchAnalyzer
    {
        // Run time display format string
        private const string RunTimeFormat = "yyyy-MM-dd HH:mm:ss";
        // Prefetch directory prefix constant
        private const string PrefetchDirectoryPrefix = @"C:\Windows\Prefetch\";

        // Analyze a prefetch file and return data
        public static PrefetchData AnalyzePrefetchFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A prefetch file path is required.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Prefetch file not found.", filePath);
            }

            if (!TryOpenPrefetch(filePath, out var prefetch))
            {
                return null;
            }

            if (prefetch == null || prefetch.Header == null)
            {
                return null;
            }

            if (prefetch.VolumeInformation != null)
            {
                foreach (var volume in prefetch.VolumeInformation)
                {
                    TryRegisterVolume(volume);
                }
            }

            VolumeResolver.BuildCache();

            var dependencies = ExtractDependencies(prefetch);

            var executablePath = ExtractExecutablePath(prefetch, dependencies);

            var data = new PrefetchData
            {
                SourceFilename = SanitizeSourceFilename(prefetch.SourceFilename, filePath),
                PFCreatedOn = SanitizeDate(prefetch.SourceCreatedOn),
                PFModifiedOn = SanitizeDate(prefetch.SourceModifiedOn),
                PFAccesedOn = SanitizeDate(prefetch.SourceAccessedOn),
                ExecutableName = prefetch.Header.ExecutableFilename ?? string.Empty,
                Directories = dependencies
            };

            data.SetExecutableFullPath(executablePath);

            if (string.Equals(data.ExecutableName, data.ExecutableFullPath, StringComparison.OrdinalIgnoreCase))
            {
                data.SetExecutableFullPath(string.Empty);
            }

            PopulateExecutableMetadata(data);

            var orderedRunTimes = (prefetch.LastRunTimes ?? new List<DateTimeOffset>())
                .Where(rt => rt != DateTimeOffset.MinValue)
                .OrderByDescending(rt => rt)
                .Select(rt => rt.LocalDateTime)
                .ToList();

            data.SetRunTimes(orderedRunTimes, prefetch.RunCount, RunTimeFormat);

            return data;
        }
    }
}
