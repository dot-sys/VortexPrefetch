<p align="center">
  <img src="https://dot-sys.github.io/VortexCSRSSTool/Assets/VortexLogo.svg" alt="Vortex Logo" width="100" height="100">
</p>

<h2 align="center">Vortex Prefetch Analyzer</h2>

<p align="center">
  Standalone C# library for deep Windows Prefetch (.pf) parsing. Handles Win10 and 11, loaded files, volume mapping, sigs & run history!<br><br>
  ⭐ Star this project if you found it useful.
</p>

---
<p align="center">
  <img src="https://i.imgur.com/t0Tr0q2.png" alt="Vortex Logo" height="600">
</p>

### Overview

**Vortex Prefetch Analyzer** is a robust .NET library for forensic analysis of Windows Prefetch files in Win10 and Win11. Decompresses, patches Win11 version bugs, extracts loaded files/modules/directories via reflection, resolves VOLUME paths and pulls extra exe metadata (Dates, MD5, signatures).

#### Core Parsing

- Decompress MAM-compressed files using ntdll!RtlDecompressBufferEx
- Auto-detect/patch Win11 (v31→v30) for compatibility
- Extract dependencies: Filenames, Modules, FileMetrics, VolumeDirectories

#### Metadata Enrichment

- Resolve \VOLUME{Serial}\ paths
- Exe file checks: Timestamps, MD5, Authenticode sig validation
- Status flags: Present/Deleted/Sysfile/Unknown

---

### Features

- **Dependency Resolution**: Files/modules/dirs with volume normalization
- **Exe Metadata**: MD5, sig status, timestamps
- **Reflection Helpers**: Dynamic prop access for parser objects
- **No External Dumps**: Live file analysis + temp decompression

### Requirements

- .NET Framework 4.6.2
- Windows 10 or Windows 11
- Administrator privileges (for file access)
