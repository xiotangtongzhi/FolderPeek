# 构建与验证 / Build and validation

## 环境

- Windows 10 / 11。
- .NET 8 SDK，用于构建应用及运行 net8.0 核心测试。
- .NET Framework 4.8，用于运行 Windows Forms 应用。
- Inno Setup 6.5+，用于打包安装程序。

## 应用与安装包

在仓库根目录运行：

```powershell
./build.ps1 -Language zh-CN
./build.ps1 -Language en-US
./build-release.ps1 -InnoCompiler 'C:/Program Files (x86)/Inno Setup 6/ISCC.exe'
```

输出目录为 `build/zh-CN`、`build/en-US` 和 `dist/`。exe 运行时需保留同目录的 `FolderPeek.exe.config`。

没有 SDK 时，可使用 Microsoft.Net.Compilers.Toolset 4.8.0 中的 .NET Framework 编译器：

```powershell
./build-release.ps1 -CompilerPath 'C:/tools/compiler/tasks/net472/csc.exe' -InnoCompiler 'C:/tools/Inno Setup 6/ISCC.exe'
```

安装包构建使用临时目录，避免输出目录中 exe 被占用。构建工具和大型运行库没有打包进仓库。

## 验证

```powershell
dotnet run --project tests/CoreTests.csproj -c Release
./test-windows.ps1 -CompilerPath 'C:/tools/compiler/tasks/net472/csc.exe'
```

安装与卸载验证应在没有现有 Folder Peek 安装的测试账户执行：

```powershell
./test-windows.ps1 -CompilerPath 'C:/tools/compiler/tasks/net472/csc.exe' -Installers
```

核心测试验证分页、排序、路径策略、错误、设置、深度和取消。GUI 与 Explorer 测试需要交互式 Windows 桌面，安装测试会临时安装再卸载测试副本。

## 源码目录

| 路径 | 内容 |
| --- | --- |
| `src/` | 应用与中英文界面 |
| `assets/` | 软件图标 |
| `installer/` | Inno Setup 安装脚本及中文翻译 |
| `tests/` | 核心、窗口、Explorer 与安装验证 |
| `tools/` | 图标生成工具 |
| `promo/` | 最终中英文宣传片前端与旁白素材 |
| `docs/` | 使用说明、发布记录与文档 |

`ENGLISH` 编译符号启用英文界面，默认中文。用户文件名和目录路径不会被翻译。更早免安装版保留各自版本的设置文件。
