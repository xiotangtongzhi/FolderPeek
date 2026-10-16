# Folder Peek · 目录浮窗

**让文件夹结构留在工作旁边。** 一款轻量 Windows 目录浮窗：自动跟随最前面的资源管理器文件夹，用分支树展示子目录与文件。

[English](README.en.md) · [下载 v1.5](https://github.com/xiotangtongzhi/FolderPeek/releases/tag/v1.5.0) · [中文使用说明](docs/UserGuide-zh-CN.txt) · [构建说明](docs/BUILD.md)

[![Folder Peek 宣传视频预览](docs/media/hero.png)](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek_Promo_Rev2_zh-CN_1080p.mp4)

*上图为宣传片画面；点击观看中文配音版。软件交互在宣传片中以动画示意呈现。*

## 下载安装

| 文件 | 用途 |
| --- | --- |
| [中文版安装包](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek-v1.5-Setup-zh-CN.exe) | 中文软件界面，约 1.9 MiB |
| [英文版安装包](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek-v1.5-Setup-en-US.exe) | 英文软件界面，约 1.9 MiB |
| [完整发行文件](https://github.com/xiotangtongzhi/FolderPeek/releases/tag/v1.5.0) | 安装包、源码、视频、字幕和校验值 |

适用于 **Windows 10 / 11**，需要 **.NET Framework 4.8 或更高版本**。安装包会检查运行环境，不捆绑大型运行库。按当前用户安装，无需管理员权限；可选桌面快捷方式，可在 Windows 设置中卸载。

中英文安装包对应同一个应用，安装另一语言版本会替换界面语言。当前发布包尚未数字签名，Windows 可能提示未知发布者。

## 宣传视频

| 版本 | 时长 | 视频 | 字幕 |
| --- | --- | --- | --- |
| 中文女声 | 约 2 分 41 秒 | [观看 / 下载 MP4](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek_Promo_Rev2_zh-CN_1080p.mp4) | [双语 SRT](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek-Promo-zh-CN.srt) |
| 英文女声 | 约 2 分 27 秒 | [观看 / 下载 MP4](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek_Promo_Rev2_en-US_1080p.mp4) | [双语 SRT](https://github.com/xiotangtongzhi/FolderPeek/releases/download/v1.5.0/FolderPeek-Promo-en-US.srt) |

两版均为 1080p / 30fps，配中英文画内字幕。按「用途 → 功能 → 实现原理」介绍软件；前端动画代码与可复用的连续旁白位于 [promo/](promo/)。

## 主要功能

- **自动跟随文件夹**：识别最前面的可见资源管理器文件夹，切换目录或窗口时更新；可以暂停跟随，也可以手动选择或输入完整路径。
- **分支树展示**：每项一行，逐层呈现根目录、子文件夹与文件；文件夹名称用层级颜色区分。
- **按需展开**：统计当前目录的文件夹与文件数量，默认先显示 5 项；「展示更多」每次追加 5 项，也可通过右键改为 10 或 20 项。
- **聚焦与定位**：双击文件夹聚焦分支；Ctrl+单击在资源管理器中打开目录或定位文件。
- **自由浏览画布**：中键拖拽、滚轮纵向移动、Shift+滚轮横向移动、Ctrl+滚轮缩放；Home 重置视图。
- **简洁的窗口控制**：选择文件夹、上一级、刷新、显示文件、自动跟随、置顶和展开深度。
- **明暗模式**：太阳 / 月亮切换日间和夜间模式；夜间使用黑底，普通文字使用白色，文件夹保留层级颜色。
- **隐藏顶部**：路径下方的三角按钮隐藏标题、工具栏与路径，目录树继续显示；再次点击恢复。
- **混合字体**：英文与数字使用 Times New Roman，中文使用宋体；按钮为方角。

## 快速开始

1. 安装并启动软件，在资源管理器中打开一个文件夹。
2. 保持「自动跟随」开启，目录浮窗会跟随资源管理器中的活动文件夹。
3. 用「展开深度」调整层数，用「展示更多」逐步查看条目。
4. 按住鼠标中键移动视野，双击文件夹聚焦，点击太阳 / 月亮切换主题。

F5 刷新；Ctrl+O 选择文件夹；Ctrl+L 选中路径；Alt+F4 关闭窗口。顶部收起时，可拖动三角按钮旁的空白区域移动窗口。

设置保存在 `%LOCALAPPDATA%\FolderPeek\settings-v1.5.xml`，卸载时保留。软件不设置开机自启，不上传目录数据；浏览目录不需要打开文件内容。

## 实现原理

程序约每 650ms 检查一次窗口顺序，筛选可见、未最小化的资源管理器窗口。通过 Windows Shell / COM 接口取得活动目录路径，在后台枚举目录条目并统计数量。

目录模型保留需要显示的前缀，按展开深度继续读取可见分支，避免递归遍历所有隐藏分支。快速切换目录时取消旧读取并丢弃过期结果。画布将层级转为坐标与分支线，统一应用平移和缩放。

更多说明见 [架构说明](docs/ARCHITECTURE.md)。

## 源码与构建

应用使用 C# / Windows Forms / .NET Framework 4.8，安装程序由 Inno Setup 构建。

```powershell
./build.ps1 -Language zh-CN
./build.ps1 -Language en-US
dotnet run --project tests/CoreTests.csproj -c Release
```

开发环境需要 .NET 8 SDK；生成安装包另需 Inno Setup 6.5+。完整命令、源码目录和验证方法见 [构建说明](docs/BUILD.md)。

## 作者

**TTang**

- 邮箱：[lim822190@gamil.com](mailto:lim822190@gamil.com)
- 工作邮箱：[tzw822190@nwafu.edu.cn](mailto:tzw822190@nwafu.edu.cn)
- GitHub：[xiotangtongzhi](https://github.com/xiotangtongzhi)

邮箱按作者提供的拼写保留。问题与建议可通过本仓库的 Issues 提交。

## 许可与第三方组件

本仓库公开提供源码，目前尚未指定开源许可证。第三方组件与字体说明见 [THIRD-PARTY.md](THIRD-PARTY.md)。
