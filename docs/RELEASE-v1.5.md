# v1.5 · 2026-10-15

首次提供中文、英文正式安装包；保留 v1.4 的目录树、字体、主题、顶部收起和 02 号图标。

## Distribution

- Per-user install to `%LOCALAPPDATA%/Programs/FolderPeek`; no elevation or automatic startup.
- Start Menu shortcut, optional desktop shortcut, Windows uninstall registration.
- Both language packages share an application identity and can replace each other.
- .NET Framework 4.8 prerequisite detection; runtime is not bundled.
- Version: 1.5.0.0; AnyCPU application. Windows 10/11 target.
- Unsigned release. No certificate or publisher identity has been fabricated.

## Validation on this Windows 11 computer

- 33 core checks passed: filesystem paths, errors, settings, pagination, depth and cancellation.
- Chinese and English GUI checks passed: typography, themes, tree pagination, middle-drag, window controls, hidden-header behavior and settings restoration.
- Installed Chinese and English binaries verified against build hashes; locale and main-window initialization checked from installed files.
- Chinese-to-English in-place language replacement passed.
- Uninstall removed installed executable and uninstall registration.
- Explorer follow checked with real Explorer windows, repeated navigation, manual branch focus, minimize/restore and hidden header.

No clean Windows 10, ARM64, non-Chinese OS or separate physical computer was available for validation. Native folder-picker labels and installed font availability follow Windows. This release does not include code signing.
