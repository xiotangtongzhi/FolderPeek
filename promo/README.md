# Folder Peek 宣传片 · 第二版

2026-10-16 修订。GitHub 中仅包含最终第二版的制作源码。

## 两种配音版本

- 中文：`zh-CN/out/FolderPeek_Promo_Rev2_zh-CN_1080p.mp4`，约 2 分 41 秒。
- 英文：`en-US/out/FolderPeek_Promo_Rev2_en-US_1080p.mp4`，约 2 分 27 秒。

均为 1920×1080、30fps、H.264/AAC，包含中英文画内字幕。每个目录下另有 `out/FolderPeek_Bilingual.srt` 和 `audio/voice-only.wav`。

## 修正

1. 移除右上角页码、章节/用途标签，以及分页演示中右上位置的计数标注。其余功能动画保留。
2. 不再使用上一版 25 句随机独立生成的 Qwen 配音。
3. 中文采用固定晓晓神经女声（`zh-CN-XiaoxiaoNeural`）；英文采用固定 Jenny 神经女声（`en-US-JennyNeural`）。每种语言整篇连续合成，语速分别为 -5% 和 -3%，音高固定为 +0Hz。
4. 去掉每句人为加入的 1.5 秒空白；仅在整篇开头、结尾留白。不对句子单独变速或拼接。
5. 按各语言语音服务返回的逐词时间标记建立独立时间轴。英文 Ctrl 改用 Control 完整读法，1.9 使用 one point nine，SimSun 使用 Sim Sun。
6. 全篇统一响度；背景音乐保持较低音量，以旁白为主。

配音使用 Microsoft Edge neural TTS（edge-tts），属于在线语音服务；仅提交宣传文稿。没有上传软件目录数据，没有使用声音克隆。字幕与文稿对应检查使用语音服务返回的词时间标记，不等同于独立语音识别或人工逐字听审。

## 修改与重新渲染

两个语言目录分别保留完整画面代码、时间轴和配音。前端仍为原来的 25 段功能与原理动画，两个版本内容对应，节奏适配各自语言。

在语言目录运行 `npm install`、`npm run preview` 可预览；`npm run render -- --out out/自定义名称.mp4` 可导出。需要 Chrome/Chromium 和 FFmpeg；通过 `CHROME`、`FFMPEG` 环境变量指定路径。中文预览端口 5195，英文 5197。

根目录的 `scripts/speech.py` 重建连续旁白，`scripts/assemble.py` 生成时间轴、字幕和混音，`scripts/verify.py` 检查两个成片。

不包含模型或 node_modules。已经附带 audio/continuous.mp3 和逐词时间标记；安装 NumPy、Pillow 并设置 FFMPEG 后，可先运行根目录的 scripts/assemble.py 还原 WAV 混音，再进入语言目录预览或渲染。仅在重新生成配音时需要安装 edge-tts（pip install edge-tts）；也可通过 EDGE_TTS_PATH 指向已有依赖目录。CHROME 和 FFMPEG 应设置为本机程序路径。
