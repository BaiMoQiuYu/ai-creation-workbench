# 小白龙 · AI 创作工作台

一个 Windows 桌面入口，把本地图片、GPT Image 提示词、语音制作、音频、视频、桌面 AI 和素材目录放在同一个窗口。当前快照保留了新版的左侧导航、卡片和任务日志，并用本地配置接入各工具。

## 下载后运行

1. 解压整个仓库。需要 64 位 Windows 和系统自带的 .NET Framework 4.x C# 编译器。
2. 在解压目录打开 PowerShell，运行 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1`。
3. 双击生成的 `AI-Creation-Workbench.exe`。保留同目录下的脚本和说明。
4. 音视频处理需要 Python 3.8+、FFmpeg 和 FFprobe。将命令加入 PATH，或在本地配置中指定 Python 可执行文件。

复制和保存 GPT Image 提示词、查看工作台输出目录不需要安装模型。媒体页支持读取媒体信息、抽取音轨、转 MP3/WAV、调响度、裁切、压缩到 720p 内、去声和截帧。结果默认另存到当前用户的本地应用数据目录下的 `AICreationWorkbench\output`。

## 可选接入

复制 `workbench.example.json` 为 `workbench.local.json`，在副本里使用绝对路径。JSON 中的 Windows 路径也可用正斜杠，例如 `C:/Tools/python.exe`。空白项表示使用默认值或暂未接入。

| 功能 | 本地配置 |
| --- | --- |
| ComfyUI | 自行安装并启动 ComfyUI，默认使用本机的 `http://127.0.0.1:8188/`。`comfy_root` 指向包含 `ComfyUI` 子目录的安装根目录。已有启动脚本时，也可指定 `comfy_service` 和 `comfy_python`。 |
| MOSS Image、音乐模型分析 | `model_worker` 指向本机 Python 任务脚本。该脚本接收一个 UTF-8 JSON 文件路径，按 `kind` 处理 `moss` 或 `analysis`，通过 `output_dir` 另存结果，并用退出码反馈成功或失败。`moss` 字段含 `prompt`、`negative`、`width`、`height`、`steps`。`analysis` 字段含 `model`、`input`、`start`、`duration`。 |
| 语音制作 | `voice_url` 指向本机语音服务。页面链接对应 `/`、`/longform`、`/timeline`、`/guide`，需要服务提供这些路由。目录按钮可配置 `voice_project` 和 `voice_logs`。 |
| 桌面 AI | `desktop_agent` 指向支持 `list` 命令的本地桥接组件，`agent_client` 指向你要打开的客户端。 |
| 素材与项目 | `image_gallery`、`video_project`、`music_reports`、`output_dir` 对应各目录。`video_url` 是可选的短剧网站入口。 |

这个仓库提供桌面源码、构建脚本、基础媒体任务和配置模板。可选模型、语音服务和桌面桥需要分别安装。
