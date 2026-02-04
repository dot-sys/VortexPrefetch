using System;
using System.Collections.Generic;
using System.Linq;

namespace Prefetch.Models
{
    // PrefetchData holds prefetch details
    public class PrefetchData
    {
        // Default runtime format string
        private const string DefaultRunTimeFormat = "yyyy-MM-dd HH:mm:ss";
        // Run time history list
        private readonly List<DateTime> _runTimes = new List<DateTime>();

        // Prefetch source file name
        public string SourceFilename { get; set; }
        // Prefetch creation timestamp
        public DateTime? PFCreatedOn { get; set; }
        // Prefetch modified timestamp
        public DateTime? PFModifiedOn { get; set; }
        // Prefetch accessed timestamp
        public DateTime? PFAccesedOn { get; set; }
        // Source file creation time
        public DateTime? SourceCreatedOn { get; set; }
        // Source file modification time
        public DateTime? SourceModifiedOn { get; set; }
        // Source file access time
        public DateTime? SourceAccessedOn { get; set; }
        // Prefetch status text
        public string Status { get; set; }
        // Digital signature status
        public string SignatureStatus { get; set; }
        // MD5 hash string
        public string Md5Hash { get; set; }
        // Executable name
        public string ExecutableName { get; set; }
        // Executable full path
        public string ExecutableFullPath { get; private set; }
        // Run count from file
        public int RunCount { get; private set; }
        // Most recent run time
        public DateTime? LastRun { get; private set; }
        // Previous run timestamp zero
        public DateTime? PreviousRun0 { get; private set; }
        // Previous run timestamp one
        public DateTime? PreviousRun1 { get; private set; }
        // Previous run timestamp two
        public DateTime? PreviousRun2 { get; private set; }
        // Previous run timestamp three
        public DateTime? PreviousRun3 { get; private set; }
        // Previous run timestamp four
        public DateTime? PreviousRun4 { get; private set; }
        // Previous run timestamp five
        public DateTime? PreviousRun5 { get; private set; }
        // Previous run timestamp six
        public DateTime? PreviousRun6 { get; private set; }
        // Referenced directories list
        public List<string> Directories { get; set; } = new List<string>();
        // Readonly run times list
        public IReadOnlyList<DateTime> LastRunTimes => _runTimes.AsReadOnly();

        // Set executable full path
        public void SetExecutableFullPath(string executableFullPath)
        {
            ExecutableFullPath = executableFullPath;
        }

        // Display string for run times
        public string LastRunTimesDisplay
        {
            get
            {
                if (_runTimes.Count == 0)
                {
                    return "N/A";
                }

                return string.Join(" | ", _runTimes.Select(rt => rt.ToString(_runTimeFormat)));
            }
        }

        // Current runtime format string
        private string _runTimeFormat = DefaultRunTimeFormat;

        // Set run times and metadata
        public void SetRunTimes(IEnumerable<DateTime> runTimes, int runCountFromFile, string runTimeFormat)
        {
            _runTimes.Clear();
            _runTimeFormat = string.IsNullOrWhiteSpace(runTimeFormat) ? _runTimeFormat : runTimeFormat;

            if (runTimes != null)
            {
                _runTimes.AddRange(runTimes);
            }

            RunCount = runCountFromFile > 0 ? runCountFromFile : _runTimes.Count;

            LastRun = GetRunTimeAtIndex(0);
            PreviousRun0 = GetRunTimeAtIndex(1);
            PreviousRun1 = GetRunTimeAtIndex(2);
            PreviousRun2 = GetRunTimeAtIndex(3);
            PreviousRun3 = GetRunTimeAtIndex(4);
            PreviousRun4 = GetRunTimeAtIndex(5);
            PreviousRun5 = GetRunTimeAtIndex(6);
            PreviousRun6 = GetRunTimeAtIndex(7);
        }

        // Get run time by index
        private DateTime? GetRunTimeAtIndex(int index)
        {
            if (index < 0 || index >= _runTimes.Count)
            {
                return null;
            }

            return _runTimes[index];
        }
    }
}
