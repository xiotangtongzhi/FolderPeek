# Folder Peek

**Keep your folder structure beside your work.** A lightweight Windows folder-tree viewer that follows the frontmost File Explorer folder window.

[中文说明](README.md) · [Download v1.5](https://github.com/xiotangtongzhi/FolderPeek/releases/tag/v1.5.0) · [User guide](docs/UserGuide-en-US.txt) · [Build instructions](docs/BUILD.md)

[![Watch the Folder Peek introduction](docs/media/hero.png)](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek_Promo_Rev2_en-US_1080p.mp4)

*Click the promotional frame to watch the English narration. The film uses animated interaction diagrams.*

## Download

- [Chinese installer](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek-v1.5-Setup-zh-CN.exe)
- [English installer](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek-v1.5-Setup-en-US.exe)
- [All release files, source, videos and checksums](https://github.com/xiotangtongzhi/FolderPeek/releases/tag/v1.5.0)

Each installer is approximately 1.9 MiB. Requires **Windows 10 / 11** and **.NET Framework 4.8+**. Installs for the current user without administrator rights. Desktop shortcuts are optional; uninstall through Windows Settings. The two language packages install the same application and replace the UI language when installed over each other.

The runtime is checked but not bundled. This release is unsigned, so Windows may display an unknown publisher prompt.

## Videos

| Narration | Duration | Video | Bilingual subtitles |
| --- | --- | --- | --- |
| Chinese female voice | About 2:41 | [Watch / download](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek_Promo_Rev2_zh-CN_1080p.mp4) | [SRT](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek-Promo-zh-CN.srt) |
| English female voice | About 2:27 | [Watch / download](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek_Promo_Rev2_en-US_1080p.mp4) | [SRT](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek-Promo-en-US.srt) |

Both films are 1080p / 30fps with Chinese and English on-screen subtitles. They cover the purpose, features and implementation. Editable animation sources and reusable continuous narration are in [promo/](promo/).

## Features

- Follow the frontmost visible File Explorer folder, including navigation and window changes.
- Display folders and files as a branch tree, with one entry per row and distinct folder-depth colors.
- Count direct children and initially show five entries per folder. Show more adds five; the context menu offers page sizes of 5, 10 or 20.
- Double-click a folder to focus it; Ctrl-click to open a folder or locate a file in File Explorer.
- Middle-drag to pan, wheel to scroll, Shift+wheel to move horizontally, Ctrl+wheel to zoom, and Home to reset.
- Choose a folder, go up, refresh, toggle files and following, pin the window, and set expansion depth.
- Switch day/night mode with the sun/moon button.
- Hide the entire header using the triangle below the path while keeping the tree visible.
- Times New Roman for English/numbers, SimSun for Chinese, and square controls.

## Quick start

1. Install and launch Folder Peek, then open a folder in File Explorer.
2. Leave Auto follow enabled to update the tree when the Explorer folder changes.
3. Choose a depth and use Show more to reveal additional entries.
4. Middle-drag to move around; double-click a folder to focus it.

F5 refreshes, Ctrl+O chooses a folder, Ctrl+L focuses the path, and Alt+F4 closes the window. When the header is hidden, drag the blank area beside the triangle to move the window.

Preferences are stored in `%LOCALAPPDATA%\FolderPeek\settings-v1.5.xml` and retained on uninstall. The application does not configure automatic startup, upload directory data, or need to open file contents to browse the tree.

## How it works

About every 650ms, a background STA worker examines window order and selects a visible, non-minimized File Explorer window. Windows Shell / COM interfaces provide the active folder path. Directory reads run in the background, count direct children and retain the visible prefix.

Only visible folder branches are expanded to the chosen depth. Cancellation and generation checks discard stale results during rapid navigation. The canvas converts hierarchy into coordinates and branch lines, then applies pan and zoom transforms. See [architecture](docs/ARCHITECTURE.md).

## Build

C# / Windows Forms / .NET Framework 4.8. Development requires the .NET 8 SDK; installers additionally require Inno Setup 6.5+.

```powershell
./build.ps1 -Language zh-CN
./build.ps1 -Language en-US
dotnet run --project tests/CoreTests.csproj -c Release
```

See [build and validation instructions](docs/BUILD.md).

## Author

**TTang** · [xiotangtongzhi](https://github.com/xiotangtongzhi)

- Email: [lim822190@gamil.com](mailto:lim822190@gamil.com)
- Work email: [tzw822190@nwafu.edu.cn](mailto:tzw822190@nwafu.edu.cn)

Email spelling is preserved as supplied by the author. Use repository Issues for feedback.

## License

The source is publicly available; an open-source license has not yet been selected. See [third-party notices](THIRD-PARTY.md).
