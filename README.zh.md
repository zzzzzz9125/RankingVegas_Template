# RankingVegas_Template

[English](./README.md) | [中文](./README.zh.md) | [日本語](./README.ja.md)

这是一个用于构建 VEGAS Pro 扩展的模板项目，提供排行榜、时间统计与挂机检测，支持在线/离线模式，并提供两套 UI 风格（基于 WebView2 的现代界面与 WinForms 回退）。

## 项目特点

- 排行榜系统（仅在线）。离线模式下仅可使用计时功能。
- 计时与自动活动/挂机检测
- 查看排行榜时支持分组管理（可管理独立排行榜分组）
- 多语言支持：英语、简体中文、日语
- 两套 UI 风格：WebView2 现代界面与 WinForms 回退
- 可配置的应用配置文件，用于连接远程排行榜 API
- 使用 ILRepack 可选打包为单一 DLL，便于部署到 VEGAS Pro 扩展文件夹

## 构建与开发

- 推荐 IDE：Visual Studio 2022 / 2026
- 包管理：NuGet
- 目标框架：.NET Framework 4.8

### VEGAS Pro SDK 引用

项目依赖 VEGAS Pro 的脚本 API。请在项目中添加以下任一对引用：

- MAGIX VEGAS Pro 14 及以上：
  - `ScriptPortal.Vegas.dll`
  - `ScriptPortal.MediaSoftware.Skins.dll`

- Sony VEGAS Pro 13：
  - `Sony.Vegas.dll`
  - `Sony.MediaSoftware.Skins.dll`

如果使用 Sony 版本，请在项目设置的“条件编译和符号”中添加 `Sony` 符号。

### 配置 App Profile

将 `RankingAppProfile.Local.template.cs` 复制并重命名为 `RankingAppProfile.Local.cs`，填写 API Key、AppId、AppSecret 和接口地址等信息。本项目需要服务端实现账号与在线排行榜功能。可以参考的 API 设计：

https://otm.ink/article/api-ranking

若你的服务器 API 与参考不同，可能需要修改 `RankingApiClient` 中的部分逻辑以匹配实际接口。

### 快速生成离线演示版

若想快速编译一个离线演示版，请在项目设置中（生成 → 条件编译符号）添加 `DEMO` 符号。这会启用模板的 AppProfile，并在无法连接到服务器时忽略连接问题，改用内置的演示数据来填充排行榜。

### Assembly 信息

编辑 `Properties/AssemblyInfo.cs`，填写产品名、公司名与版本等程序集元数据。

### 生成后事件打包

项目包含一个生成后事件，使用 ILRepack 将依赖合并到单一 DLL。如下所示：

```
"$(SolutionDir)packages\ILRepack.2.0.44\tools\ILRepack.exe" /out:"$(TargetDir)Merged\$(TargetFileName)" "$(TargetPath)" "$(TargetDir)Newtonsoft.Json.dll" "$(TargetDir)Microsoft.Web.WebView2.Core.dll" "$(TargetDir)Microsoft.Web.WebView2.WinForms.dll" "$(TargetDir)System.Buffers.dll" "$(TargetDir)System.Memory.dll" "$(TargetDir)System.Numerics.Vectors.dll" "$(TargetDir)System.Runtime.CompilerServices.Unsafe.dll" "$(TargetDir)System.Text.Encoding.CodePages.dll" "$(TargetDir)SixLabors.ImageSharp.dll"
:: xcopy /R /Y "$(TargetDir)Merged\$(TargetFileName)" "C:\ProgramData\VEGAS Pro\Application Extensions\"
```

此命令会在 `$(TargetDir)Merged` 下生成合并后的程序集。注释掉的 `xcopy` 行可启用以将生成的 DLL 直接复制到 VEGAS Pro 的扩展目录。

额外说明 — 常见的 VEGAS Pro 扩展目录

你可以将生成的 DLL 放到下列常见的 VEGAS Pro 扩展文件夹之一（将 `22.0` 替换为目标 VEGAS 版本号）：

```
C:\ProgramData\VEGAS Pro\Application Extensions\
C:\ProgramData\VEGAS Pro\22.0\Application Extensions\
%userprofile%\Documents\Vegas Application Extensions\
%appdata%\VEGAS Pro\Application Extensions\
%appdata%\VEGAS Pro\22.0\Application Extensions\
%localappdata%\VEGAS Pro\Application Extensions\
%localappdata%\VEGAS Pro\22.0\Application Extensions\
```

对于 Sony 版（VP13 及以下），路径可能位于上层 `Sony` 文件夹下，例如：

```
C:\ProgramData\Sony\VEGAS Pro\Application Extensions\
```

请选择与目标机器上已安装的 VEGAS Pro 版本和发行版相匹配的文件夹进行安装。

安装完成后，可在 VEGAS Pro 菜单栏中通过：`视图` → `扩展` → `RankingVegas` 打开本扩展。

### 安全性建议

.NET 程序容易被反编译，建议不要在可分发的二进制中明文存储敏感信息（例如 API Key、App Secret）。可考虑使用混淆工具或在服务端实现令牌交换来保护密钥。

## 许可证

本项目采用 MIT 许可证，详情请参见 `LICENSE`。

## 第三方依赖与许可证

- `Newtonsoft.Json` — MIT（Json.NET）
- `Microsoft.Web.WebView2`（Core & WinForms） — MIT（Microsoft）
- `SixLabors.ImageSharp` — Apache-2.0
- `ILRepack` — MIT
- VEGAS SDK DLLs（`ScriptPortal.Vegas.dll`、`ScriptPortal.MediaSoftware.Skins.dll`、`Sony.Vegas.dll`、`Sony.MediaSoftware.Skins.dll`）— 专有组件，由 MAGIX / Sony 随 VEGAS Pro 提供；请遵循 VEGAS Pro SDK 的许可与分发规则

如果你引入其他 NuGet 包，请在分发前检查各自许可证。

## 备注

- 模板假定存在兼容的服务端实现以支持在线功能；若仅离线使用，排行榜与在线账户同步等功能将不可用。
- WebView2 UI 依赖 WebView2 运行时；当 WebView2 不可用时，项目会回退到 WinForms UI。

### Microsoft Edge WebView2 运行时

基于 WebView2 的现代界面需要在目标机器上安装 Microsoft Edge WebView2 运行时（注意这与项目中的 `Microsoft.Web.WebView2` NuGet 包不同）。请从 Microsoft 官方页面选择适合的安装包：

https://developer.microsoft.com/microsoft-edge/webview2/

提示：较新的 Windows 10 版本和 Windows 11 在系统中已原生包含 Edge WebView2 运行时，因此在这些系统上通常不需要手动安装。

如果目标机器未安装 WebView2 运行时，本扩展会自动回退到 WinForms 界面。项目在没有 WebView2 的情况下仍然可用。

---

祝你开发顺利 — 用此模板快速创建自己的 VEGAS Pro 扩展。