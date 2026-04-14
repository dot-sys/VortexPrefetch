using System;
using System.Collections.Generic;

namespace Prefetch
{
    internal interface IPrefetchHeader
    {
        string ExecutableFilename { get; }
    }

    internal interface IPrefetchVolume
    {
        string DevicePath { get; }
        uint SerialNumber { get; }
        IReadOnlyList<string> DirectoryNames { get; }
    }

    internal interface IPrefetch
    {
        IPrefetchHeader Header { get; }
        IReadOnlyList<IPrefetchVolume> VolumeInformation { get; }
        IReadOnlyList<DateTimeOffset> LastRunTimes { get; }
        int RunCount { get; }
        string SourceFilename { get; }
        DateTimeOffset SourceCreatedOn { get; }
        DateTimeOffset SourceModifiedOn { get; }
        DateTimeOffset SourceAccessedOn { get; }
        IReadOnlyList<string> FilenameStrings { get; }
    }
}
