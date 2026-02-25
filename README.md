# RankingVegas_Template

[English](./README.md) | [中文](./README.zh.md) | [日本語](./README.ja.md)

A template project for creating a VEGAS Pro extension that provides ranked leaderboards, time tracking and AFK detection, supports online/offline modes and offers two UI styles (WebView2-based and WinForms fallback).

## Key features

- Leaderboard system (online-only). In offline mode only time tracking is available.
- Time tracking and automatic activity/AFK detection
- Group management for leaderboards (manage separate leaderboard groups)
- Multilingual support: English, Chinese (Simplified), Japanese
- Two UI styles: modern WebView2-based view and WinForms fallback
- Configurable app profile for connecting to a remote ranking API
- Packaged single-file build using ILRepack (optional deployment to VEGAS Pro extensions folder)

## Build and development

- Recommended IDE: Visual Studio 2022 / 2026
- Package management: NuGet
- Target framework: .NET Framework 4.8

### VEGAS Pro SDK references

The project depends on the VEGAS Pro scripting API. Add one of the following pairs of references to the project:

- For MAGIX VEGAS Pro 14 and later:
  - `ScriptPortal.Vegas.dll`
  - `ScriptPortal.MediaSoftware.Skins.dll`

- For Sony VEGAS Pro 13:
  - `Sony.Vegas.dll`
  - `Sony.MediaSoftware.Skins.dll`

If you target the Sony variant, add the conditional compilation symbol `Sony` in the project settings (Build → Conditional compilation symbols).

### App profile configuration

Copy `RankingAppProfile.Local.template.cs` to `RankingAppProfile.Local.cs` and fill in the API keys and endpoint fields. This project expects a server-side implementation that provides account management and online leaderboard support. A sample API design (in Chinese) that can be used as reference is:

https://otm.ink/article/api-ranking

If your API differs from the reference, you may need to adapt some client-side logic in `RankingApiClient`.

### Assembly information

Edit `Properties/AssemblyInfo.cs` to set product, company and version metadata for the generated assembly.

### Post-build packaging

The project includes a post-build event that uses ILRepack to merge required dependencies into a single DLL. The configured command is:

```
"$(SolutionDir)packages\ILRepack.2.0.44\tools\ILRepack.exe" /out:"$(TargetDir)Merged\$(TargetFileName)" "$(TargetPath)" "$(TargetDir)Newtonsoft.Json.dll" "$(TargetDir)Microsoft.Web.WebView2.Core.dll" "$(TargetDir)Microsoft.Web.WebView2.WinForms.dll" "$(TargetDir)System.Buffers.dll" "$(TargetDir)System.Memory.dll" "$(TargetDir)System.Numerics.Vectors.dll" "$(TargetDir)System.Runtime.CompilerServices.Unsafe.dll" "$(TargetDir)System.Text.Encoding.CodePages.dll" "$(TargetDir)SixLabors.ImageSharp.dll"
:: xcopy /R /Y "$(TargetDir)Merged\$(TargetFileName)" "C:\ProgramData\VEGAS Pro\Application Extensions\"
```

This produces a merged assembly under `$(TargetDir)Merged`. The commented `xcopy` line can be enabled to copy the resulting DLL directly into the VEGAS Pro extensions directory.

Additional note — common VEGAS Pro extension directories

You can install copies of the generated DLL into one of the common VEGAS Pro extensions folders. Examples (replace `22.0` with the target VEGAS version number):

```
C:\ProgramData\VEGAS Pro\Application Extensions\
C:\ProgramData\VEGAS Pro\22.0\Application Extensions\
%userprofile%\Documents\Vegas Application Extensions\
%appdata%\VEGAS Pro\Application Extensions\
%appdata%\VEGAS Pro\22.0\Application Extensions\
%localappdata%\VEGAS Pro\Application Extensions\
%localappdata%\VEGAS Pro\22.0\Application Extensions\
```

For Sony VEGAS (VP13 and earlier) the installation paths may be under a parent `Sony` folder. For example:

```
C:\ProgramData\Sony\VEGAS Pro\Application Extensions\
```

Make sure to choose the folder that matches the installed VEGAS Pro version on the target machine.

### Security / obfuscation

Because .NET assemblies can be decompiled, avoid embedding sensitive secrets (API keys, secrets) in the distributed binaries. Consider using obfuscation or a secure server-side token exchange to protect credentials.

## License

This project is released under the MIT License. See `LICENSE` for details.

## Third-party dependencies and licenses

- `Newtonsoft.Json` — MIT License (Json.NET)
- `Microsoft.Web.WebView2` (Core & WinForms) — MIT License (Microsoft)
- `SixLabors.ImageSharp` — Apache-2.0 License
- `ILRepack` — MIT License
- VEGAS SDK DLLs (`ScriptPortal.Vegas.dll`, `ScriptPortal.MediaSoftware.Skins.dll`, `Sony.Vegas.dll`, `Sony.MediaSoftware.Skins.dll`) — proprietary, provided by MAGIX / Sony as part of VEGAS Pro; follow VEGAS Pro SDK licensing and distribution rules

If you include other NuGet packages, check their individual licenses before redistribution.

## Notes

- The template assumes a compatible server implementation for online features; offline-only usage is supported but some features (leaderboards, online account sync) will be unavailable.
- The WebView2 UI requires the WebView2 runtime. When WebView2 is not available the project falls back to a WinForms UI.

---

Happy hacking — adapt this template to build your own VEGAS Pro extension quickly.