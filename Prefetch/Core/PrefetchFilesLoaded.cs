using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Prefetch
{
    // Handles dependency extraction
    public static partial class PrefetchAnalyzer
    {
        // Extract dependency paths from prefetch data
        private static List<string> ExtractDependencies(IPrefetch prefetch)
        {
            var results = new List<string>();

            foreach (var propertyName in new[] { "Filenames", "Files", "LoadedModulePaths", "FilenameStrings" })
            {
                if (TryGetStringEnumerable(prefetch, propertyName, out var strings))
                {
                    results.AddRange(strings);
                }
            }

            if (TryGetEnumerable(prefetch, "FileMetrics", out var metrics))
            {
                foreach (var metric in metrics)
                {
                    var path = TryGetString(metric, "Path")
                               ?? TryGetString(metric, "Filename")
                               ?? TryGetString(metric, "Name");

                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    results.Add(path);
                }
            }

            if (prefetch.VolumeInformation != null)
            {
                foreach (var volume in prefetch.VolumeInformation)
                {
                    if (volume.DirectoryNames == null)
                    {
                        continue;
                    }

                    foreach (var directory in volume.DirectoryNames)
                    {
                        if (!string.IsNullOrWhiteSpace(directory) && LooksLikeFile(directory))
                        {
                            results.Add(directory);
                        }
                    }
                }
            }

            return NormalizeVolumePaths(results)
                .Where(LooksLikeFile)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Normalize multiple volume paths
        private static List<string> NormalizeVolumePaths(IEnumerable<string> paths)
        {
            if (paths == null)
            {
                return new List<string>();
            }

            return paths.Select(NormalizeVolumePath).ToList();
        }

        // Normalize a single volume path
        private static string NormalizeVolumePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            var normalizedInput = path.Replace('/', '\\').Trim();

            if (normalizedInput.IndexOf("VOLUME{", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return normalizedInput;
            }

            var resolved = VolumeResolver.ResolvePath(normalizedInput);
            return string.IsNullOrWhiteSpace(resolved) ? normalizedInput : resolved;
        }

        // Derive executable path from dependencies
        private static string ExtractExecutablePath(IPrefetch prefetch, List<string> dependencies)
        {
            if (prefetch?.Header == null)
            {
                return string.Empty;
            }

            var executableName = prefetch.Header.ExecutableFilename ?? string.Empty;

            if (dependencies != null && dependencies.Count > 0)
            {
                var match = dependencies.FirstOrDefault(d =>
                    d.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Path.GetFileName(d), executableName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(match))
                {
                    return match;
                }
            }

            return executableName;
        }

        // Clean source filename for display
        private static string SanitizeSourceFilename(string sourceFilename, string originalPath)
        {
            var candidate = string.IsNullOrWhiteSpace(sourceFilename)
                ? Path.GetFileName(originalPath)
                : sourceFilename;

            if (string.IsNullOrWhiteSpace(candidate))
            {
                return string.Empty;
            }

            var normalized = candidate.Replace('/', '\\');
            if (normalized.StartsWith(PrefetchDirectoryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(PrefetchDirectoryPrefix.Length);
            }

            return normalized;
        }

        // Check if path resembles a file
        private static bool LooksLikeFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var trimmed = path.TrimEnd('\\', '/');
            var ext = Path.GetExtension(trimmed);
            return !string.IsNullOrEmpty(ext);
        }
    }
}
