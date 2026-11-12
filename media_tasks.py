"""Process an explicitly selected local media file via FFmpeg and FFprobe."""

import json
import subprocess
import sys
import uuid
from pathlib import Path


def run(job):
    source = Path(job["input"]).resolve(strict=True)
    if not source.is_file():
        raise ValueError("请选择媒体文件。")
    op = job["operation"]
    if op == "probe":
        subprocess.run(
            ["ffprobe", "-v", "error", "-show_format", "-show_streams", "-of", "json", str(source)],
            check=True,
        )
        return

    start = str(max(0.0, float(job.get("start", 0))))
    duration = str(max(0.1, float(job.get("duration", 15))))
    video = ["-c:v", "libx264", "-pix_fmt", "yuv420p", "-c:a", "aac"]
    operations = {
        "mp3": ("mp3", ["-vn", "-c:a", "libmp3lame", "-q:a", "2"]),
        "extract": ("mp3", ["-vn", "-c:a", "libmp3lame", "-q:a", "2"]),
        "wav": ("wav", ["-vn", "-c:a", "pcm_s16le"]),
        "normalize": ("wav", ["-vn", "-af", "loudnorm=I=-16:TP=-1.5:LRA=11"]),
        "clip": ("mp4", ["-ss", start, "-t", duration, *video]),
        "compress": ("mp4", ["-vf", "scale=-2:min(720\\,trunc(ih/2)*2)", "-crf", "24", *video]),
        "mute": ("mp4", ["-an", "-c:v", "libx264", "-pix_fmt", "yuv420p"]),
        "frame": ("png", ["-ss", start, "-frames:v", "1"]),
    }
    extension, flags = operations[op]
    output = Path(job["output_dir"]).resolve()
    output.mkdir(parents=True, exist_ok=True)
    target = output / f"{source.stem[:80]}-{op}-{uuid.uuid4().hex[:12]}.{extension}"
    subprocess.run(["ffmpeg", "-hide_banner", "-nostdin", "-n", "-i", str(source), *flags, str(target)], check=True)
    if not target.is_file() or not target.stat().st_size:
        raise RuntimeError("FFmpeg 没有生成输出，请检查输入和时间范围。")
    print(f"输出：{target}")


if __name__ == "__main__":
    try:
        with Path(sys.argv[1]).open(encoding="utf-8-sig") as stream:
            run(json.load(stream))
    except (OSError, ValueError, KeyError, RuntimeError, subprocess.CalledProcessError) as exc:
        print(f"媒体任务失败：{exc}", file=sys.stderr)
        raise SystemExit(1) from None
