using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Prefetch.Models;

namespace Prefetch
{
    // Handles metadata operations
    public static partial class PrefetchAnalyzer
    {
        // Sanitize date value from offset
        private static DateTime? SanitizeDate(DateTimeOffset date)
        {
            if (date == DateTimeOffset.MinValue)
            {
                return null;
            }

            return date.LocalDateTime;
        }

        // Set metadata when executable missing
        private static void SetMissingExecutableMetadata(PrefetchData data, string status)
        {
            data.Status = status;
            data.SourceCreatedOn = null;
            data.SourceModifiedOn = null;
            data.SourceAccessedOn = null;
            data.SignatureStatus = string.Empty;
            data.Md5Hash = string.Empty;
        }

        // Populate metadata from executable file
        private static void PopulateExecutableMetadata(PrefetchData data)
        {
            if (data == null)
            {
                return;
            }

            var executablePath = NormalizeExecutablePath(data.ExecutableFullPath);

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                SetMissingExecutableMetadata(data, "Unknown");
                return;
            }

            if (IsSysfile(executablePath))
            {
                SetMissingExecutableMetadata(data, "Sysfile");
                return;
            }

            if (!File.Exists(executablePath))
            {
                SetMissingExecutableMetadata(data, "Deleted");
                return;
            }

            data.Status = "Present";

            try
            {
                var fileInfo = new FileInfo(executablePath);
                data.SourceCreatedOn = fileInfo.CreationTime;
                data.SourceModifiedOn = fileInfo.LastWriteTime;
                data.SourceAccessedOn = fileInfo.LastAccessTime;
            }
            catch
            {
                data.SourceCreatedOn = null;
                data.SourceModifiedOn = null;
                data.SourceAccessedOn = null;
            }

            data.SignatureStatus = GetSignatureStatus(executablePath);
            data.Md5Hash = ComputeMd5Hash(executablePath);
        }

        // Normalize executable path for lookup
        private static string NormalizeExecutablePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            var normalizedInput = path.Replace('/', '\\').Trim();

            normalizedInput = Environment.ExpandEnvironmentVariables(normalizedInput);

            var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? string.Empty;
            if (!string.IsNullOrEmpty(systemRoot))
            {
                if (normalizedInput.StartsWith("\\\\SystemRoot\\", StringComparison.OrdinalIgnoreCase) ||
                    normalizedInput.StartsWith("\\SystemRoot\\", StringComparison.OrdinalIgnoreCase))
                {
                    normalizedInput = Path.Combine(systemRoot, normalizedInput.Substring(normalizedInput.IndexOf("SystemRoot", StringComparison.OrdinalIgnoreCase) + "SystemRoot".Length).TrimStart('\\'));
                }
            }

            if (normalizedInput.StartsWith("\\\\?\\", StringComparison.OrdinalIgnoreCase) || normalizedInput.StartsWith("\\??\\", StringComparison.OrdinalIgnoreCase))
            {
                normalizedInput = normalizedInput.Substring(4);
            }

            if (normalizedInput.IndexOf('\\') == -1)
            {
                return normalizedInput;
            }

            return NormalizeVolumePath(normalizedInput);
        }

        // Determine if path is system file
        private static bool IsSysfile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var normalized = NormalizeForCompare(path);
            var fileName = Path.GetFileName(normalized);

            if (!string.IsNullOrEmpty(fileName) && SysFileNames.Contains(fileName))
            {
                if (normalized.IndexOf(@"\\windows\\system32\\", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            foreach (var sysSuffix in SysFileSuffixes)
            {
                if (normalized.EndsWith(sysSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (fileName.Equals("securityhealthhost.exe", StringComparison.OrdinalIgnoreCase) &&
                normalized.IndexOf(@"\\windows\\system32\\securityhealth\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        // Normalize path for comparisons
        private static string NormalizeForCompare(string path)
        {
            try
            {
                var expanded = Environment.ExpandEnvironmentVariables(path ?? string.Empty);
                expanded = expanded.Replace('/', '\\');

                if (expanded.StartsWith("\\\\?\\", StringComparison.OrdinalIgnoreCase) || expanded.StartsWith("\\??\\", StringComparison.OrdinalIgnoreCase))
                {
                    expanded = expanded.Substring(4);
                }

                if (expanded.StartsWith("\\\\Device\\HarddiskVolume", StringComparison.OrdinalIgnoreCase))
                {
                    var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C\\\\Windows";
                    var systemDrive = Path.GetPathRoot(systemRoot);
                    if (!string.IsNullOrEmpty(systemDrive))
                    {
                        var suffixIndex = expanded.IndexOf("\\Windows\\", StringComparison.OrdinalIgnoreCase);
                        if (suffixIndex >= 0)
                        {
                            expanded = systemDrive.TrimEnd('\\') + expanded.Substring(suffixIndex);
                        }
                    }
                }

                return Path.GetFullPath(expanded).TrimEnd('\\').ToLowerInvariant();
            }
            catch
            {
                return (path ?? string.Empty).Replace('/', '\\').TrimEnd('\\').ToLowerInvariant();
            }
        }

        // Known system file suffixes
        private static readonly string[] SysFileSuffixes = new[]
        {
            @"\\windows\\system32\\taskhostw.exe",
            @"\\windows\\system32\\upfc.exe",
            @"\\windows\\system32\\vssvc.exe",
            @"\\windows\\system32\\sppsvc.exe",
            @"\\windows\\system32\\srtasks.exe",
            @"\\windows\\system32\\sihclient.exe",
            @"\\windows\\system32\\runtimebroker.exe",
            @"\\windows\\system32\\provtool.exe",
            @"\\windows\\system32\\pnputil.exe",
            @"\\windows\\system32\\logonui.exe",
            @"\\windows\\system32\\smartscreen.exe",
            @"\\windows\\system32\\drvinst.exe",
            @"\\windows\\system32\\defrag.exe",
            @"\\windows\\system32\\dataexchangehost.exe",
            @"\\windows\\system32\\consent.exe",
            @"\\windows\\system32\\conhost.exe",
            @"\\windows\\system32\\comppkgsrv.exe",
            @"\\windows\\system32\\compattelrunner.exe",
            @"\\windows\\system32\\clinfo.exe",
            @"\\windows\\system32\\applicationframehost.exe",
        };

        // Known system file names
        private static readonly HashSet<string> SysFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "taskhostw.exe",
            "upfc.exe",
            "vssvc.exe",
            "sppsvc.exe",
            "srtasks.exe",
            "sihclient.exe",
            "runtimebroker.exe",
            "provtool.exe",
            "pnputil.exe",
            "loginui.exe",
            "drvinst.exe",
            "defrag.exe",
            "dataexchangehost.exe",
            "consent.exe",
            "conhost.exe",
            "comppkgsrv.exe",
            "compattelrunner.exe",
            "clinfo.exe",
            "applicationframehost.exe",
            "securityhealthhost.exe",
        };

        // Determine digital signature status
        private static string GetSignatureStatus(string executablePath)
        {
            try
            {
                using (var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(executablePath)))
                using (var chain = new X509Chain())
                {
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    var isValid = chain.Build(cert);
                    return isValid ? "Signed" : "Unsigned";
                }
            }
            catch
            {
                return "Unsigned";
            }
        }

        // Compute MD5 hash for file
        private static string ComputeMd5Hash(string executablePath)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(executablePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
