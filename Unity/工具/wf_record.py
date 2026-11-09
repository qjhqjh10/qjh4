#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""wf_record.py —— 真机录屏（只录目标窗口 / 帧序列 / MP4 / GIF / 定时动作）。

为什么有它：本机 **没有系统级 ffmpeg**，而 `wf_click.py shot` 每次都要重起 Python，做不到连续帧。
这里在同一个进程里按目标 fps 抓窗口，顺带可以在指定时刻做一次点击/拖拽；录完可用随包的 ffmpeg 直接出 MP4。

🔴 **只录目标窗口**：默认用 `PrintWindow(PW_RENDERFULLCONTENT)` 抓窗口自己的像素，
   别的窗口盖在上面 / 盖在旁边**都不会被录进去**；抓不到（部分硬件加速窗口会返回全黑）才回退到「按窗口矩形截屏」，
   并在 `record.txt` 里标 `capture=screen-fallback`（判据要看这一行）。

⚠️ 和 `wf_click.py` 一样：它抓的是**真屏幕**、动作是**真鼠标** ⇒ 先 `wf_click.py win <子串>` 置前，再开录。

用法（`-I` 避免加载同目录的 json.py 之类；⚠️ 必须 `-X utf8`，否则中文输出会炸 GBK）：
  python -I -X utf8 wf_record.py <输出目录> <秒数> [--fps 12] [--win Warpforge]
                                 [--at 1.5 --click X Y | --at 1.5 --drag X1 Y1 X2 Y2]
                                 [--mp4] [--gif] [--capture auto|window|screen] [--prefix cap]

产物：
  <目录>/<prefix>_0001.jpg …    帧序列
  <目录>/<prefix>.mp4           --mp4（随包 ffmpeg，H.264 / yuv420p / faststart）
  <目录>/<prefix>.gif           --gif
  <目录>/record.txt             参数 + 实测帧率 + 抓取方式 + 黑帧计数（判据留痕）

退出码：0 正常；2 参数错误；1 一帧都没抓到。
"""
import argparse
import ctypes
import ctypes.wintypes as wt
import os
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
VENDOR = os.path.join(HERE, "_vendor")
sys.path.insert(0, HERE)
sys.path.insert(0, VENDOR)
# ⚠️ 不能在模块顶层 import wf_click：它在**导入时**就检查 sys.argv 里有没有 -h/--help 并直接退出
#    ⇒ 那样 `wf_record.py --help` 会打出 wf_click 的用法。改成 main() 里 parse 之后再导入。

user32 = ctypes.WinDLL("user32", use_last_error=True)
gdi32 = ctypes.WinDLL("gdi32", use_last_error=True)


class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [("biSize", ctypes.c_uint32), ("biWidth", ctypes.c_int32), ("biHeight", ctypes.c_int32),
                ("biPlanes", ctypes.c_uint16), ("biBitCount", ctypes.c_uint16),
                ("biCompression", ctypes.c_uint32), ("biSizeImage", ctypes.c_uint32),
                ("biXPelsPerMeter", ctypes.c_int32), ("biYPelsPerMeter", ctypes.c_int32),
                ("biClrUsed", ctypes.c_uint32), ("biClrImportant", ctypes.c_uint32)]


class BITMAPINFO(ctypes.Structure):
    _fields_ = [("bmiHeader", BITMAPINFOHEADER), ("bmiColors", ctypes.c_uint32 * 3)]


def grab_window(hwnd):
    """用 PrintWindow 抓窗口自身像素（不受遮挡影响）。失败返回 None。"""
    r = wt.RECT()
    if not user32.GetWindowRect(hwnd, ctypes.byref(r)):
        return None
    w, h = r.right - r.left, r.bottom - r.top
    if w <= 0 or h <= 0:
        return None
    hdc_win = user32.GetWindowDC(hwnd)
    if not hdc_win:
        return None
    hdc_mem = gdi32.CreateCompatibleDC(hdc_win)
    hbmp = gdi32.CreateCompatibleBitmap(hdc_win, w, h)
    old = gdi32.SelectObject(hdc_mem, hbmp)
    ok = user32.PrintWindow(hwnd, hdc_mem, 2)          # PW_RENDERFULLCONTENT
    img = None
    if ok:
        bi = BITMAPINFO()
        bi.bmiHeader.biSize = ctypes.sizeof(BITMAPINFOHEADER)
        bi.bmiHeader.biWidth = w
        bi.bmiHeader.biHeight = -h                     # 负 = 自上而下
        bi.bmiHeader.biPlanes = 1
        bi.bmiHeader.biBitCount = 32
        bi.bmiHeader.biCompression = 0
        buf = ctypes.create_string_buffer(w * h * 4)
        got = gdi32.GetDIBits(hdc_mem, hbmp, 0, h, buf, ctypes.byref(bi), 0)
        if got:
            from PIL import Image
            img = Image.frombuffer("RGBA", (w, h), buf, "raw", "BGRA", 0, 1).convert("RGB")
    gdi32.SelectObject(hdc_mem, old)
    gdi32.DeleteObject(hbmp)
    gdi32.DeleteDC(hdc_mem)
    user32.ReleaseDC(hwnd, hdc_win)
    return img


def is_black(img, tol=8):
    """粗判全黑（硬件加速窗口 PrintWindow 常见结果）。"""
    try:
        ext = img.resize((16, 16)).convert("L")
        return max(ext.tobytes()) <= tol
    except Exception:
        return False


def find_ffmpeg():
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except Exception:
        import shutil
        return shutil.which("ffmpeg")


def grab_box(win_sub):
    import wf_click
    w = wf_click.find_window(win_sub)
    if not w:
        return None
    return (w["x"], w["y"], w["x"] + w["w"], w["y"] + w["h"]), w


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("outdir")
    ap.add_argument("seconds", type=float)
    ap.add_argument("--fps", type=float, default=12.0)
    ap.add_argument("--win", default="Warpforge")
    ap.add_argument("--capture", choices=["auto", "window", "screen"], default="auto",
                    help="auto=优先只录窗口，失败回退截屏")
    ap.add_argument("--prefix", default="cap")
    ap.add_argument("--at", type=float, default=None, help="第几秒触发动作")
    ap.add_argument("--click", nargs=2, type=int, metavar=("X", "Y"))
    ap.add_argument("--drag", nargs=4, type=int, metavar=("X1", "Y1", "X2", "Y2"))
    ap.add_argument("--mp4", action="store_true")
    ap.add_argument("--mp4-crf", type=int, default=23)
    ap.add_argument("--gif", action="store_true")
    ap.add_argument("--gif-scale", type=float, default=0.5)
    ap.add_argument("--jpg-quality", type=int, default=85)
    a = ap.parse_args(argv)

    import wf_click                       # 见文件头：必须等 argparse 处理完 -h 再导入
    from PIL import ImageGrab

    if a.seconds <= 0 or a.fps <= 0:
        print("FAIL 秒数与 fps 必须 > 0")
        return 2
    if a.at is not None and not (a.click or a.drag):
        print("FAIL --at 要配合 --click 或 --drag")
        return 2
    if (a.click or a.drag) and a.at is None:
        a.at = 0.6

    os.makedirs(a.outdir, exist_ok=True)
    w = wf_click.find_window(a.win)
    box = None
    capture = "screen"
    if w:
        box = (w["x"], w["y"], w["x"] + w["w"], w["y"] + w["h"])
        print("ok win %s rect=(%d,%d %dx%d) hwnd=%d"
              % (w["title"], w["x"], w["y"], w["w"], w["h"], w["hwnd"]))
    else:
        print("WARN 找不到窗口 %r ⇒ 整屏捕获" % a.win)

    def one_frame():
        """返回 (PIL.Image, 实际用的抓取方式)。"""
        if w and a.capture in ("auto", "window"):
            im = grab_window(w["hwnd"])
            if im is not None and not is_black(im):
                return im, "window"
            if a.capture == "window":
                return im, "window-black"
        return ImageGrab.grab(bbox=box, all_screens=True), ("screen" if box else "screen-full")

    interval = 1.0 / a.fps
    t0 = time.perf_counter()
    fired = False
    frames = 0
    black = 0
    modes = {}
    gaps = []
    last = t0
    i = 0
    while True:
        now = time.perf_counter() - t0
        if now >= a.seconds:
            break
        if a.at is not None and not fired and now >= a.at:
            fired = True
            if a.drag:
                x1, y1, x2, y2 = a.drag
                print("ACT drag %d %d -> %d %d @ %.2fs" % (x1, y1, x2, y2, now))
                wf_click.drag(x1, y1, x2, y2, steps=24, step_ms=10)
            else:
                x, y = a.click
                print("ACT click %d %d @ %.2fs" % (x, y, now))
                wf_click.click(x, y, "left", 1)
            last = time.perf_counter()
        path = os.path.join(a.outdir, "%s_%04d.jpg" % (a.prefix, i + 1))
        try:
            im, mode = one_frame()
            modes[mode] = modes.get(mode, 0) + 1
            if mode.endswith("black"):
                black += 1
            im.save(path, quality=a.jpg_quality, subsampling=0)
            frames += 1
        except Exception as e:
            print("FRAME_FAIL %d %s" % (i + 1, e))
        i += 1
        t = time.perf_counter()
        gaps.append((t - last) * 1000.0)
        last = t
        dt = t0 + i * interval - time.perf_counter()
        if dt > 0:
            time.sleep(dt)

    elapsed = time.perf_counter() - t0
    real_fps = frames / elapsed if elapsed > 0 else 0.0
    gap_max = max(gaps) if gaps else 0.0
    print("CAPTURE_DONE frames=%d elapsed=%.2fs fps_target=%.1f fps_real=%.2f gap_max=%.0fms modes=%s"
          % (frames, elapsed, a.fps, real_fps, gap_max, modes))

    mp4_path = ""
    if a.mp4 and frames:
        ff = find_ffmpeg()
        if not ff:
            print("WARN 找不到 ffmpeg（没装 imageio-ffmpeg？）⇒ 跳过 --mp4")
        else:
            mp4_path = os.path.join(a.outdir, a.prefix + ".mp4")
            cmd = [ff, "-y", "-loglevel", "error", "-framerate", "%g" % a.fps,
                   "-i", os.path.join(a.outdir, a.prefix + "_%04d.jpg"),
                   "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", str(a.mp4_crf),
                   "-movflags", "+faststart", mp4_path]
            r = subprocess.run(cmd, capture_output=True, text=True)
            if r.returncode == 0 and os.path.exists(mp4_path):
                print("ok mp4 %s (%.1f MB)" % (mp4_path, os.path.getsize(mp4_path) / 1e6))
            else:
                print("FAIL mp4 编码失败: %s" % (r.stderr or r.stdout)[:400])
                mp4_path = ""

    gif_path = ""
    if a.gif and frames:
        try:
            from PIL import Image
            files = sorted(f for f in os.listdir(a.outdir)
                           if f.startswith(a.prefix + "_") and f.endswith(".jpg"))
            imgs = []
            for fn in files:
                im = Image.open(os.path.join(a.outdir, fn)).convert("RGB")
                if a.gif_scale != 1.0:
                    im = im.resize((max(1, int(im.width * a.gif_scale)),
                                    max(1, int(im.height * a.gif_scale))))
                imgs.append(im)
            gif_path = os.path.join(a.outdir, a.prefix + ".gif")
            imgs[0].save(gif_path, save_all=True, append_images=imgs[1:],
                         duration=int(1000.0 / a.fps), loop=0)
            print("ok gif %s (%d 帧)" % (gif_path, len(imgs)))
        except Exception as e:
            print("WARN gif 合成失败: %s" % e)

    manifest = os.path.join(a.outdir, "record.txt")
    main_mode = max(modes, key=modes.get) if modes else capture
    with open(manifest, "w", encoding="utf-8") as f:
        f.write("window=%r\n" % a.win)
        f.write("box=%r\n" % (box,))
        f.write("capture=%s\n" % main_mode)
        f.write("capture_modes=%r\n" % (modes,))
        f.write("black_frames=%d\n" % black)
        f.write("seconds_requested=%.2f\n" % a.seconds)
        f.write("fps_target=%.2f\n" % a.fps)
        f.write("frames=%d\n" % frames)
        f.write("fps_real=%.2f\n" % real_fps)
        f.write("gap_max_ms=%.0f\n" % gap_max)
        f.write("mp4=%r\n" % (mp4_path,))
        f.write("gif=%r\n" % (gif_path,))
        f.write("action=%s\n" % (
            ("drag %s" % (a.drag,)) if a.drag else (("click %s" % (a.click,)) if a.click else "none")))

    if frames == 0:
        print("FAIL 一帧都没抓到（窗口被遮挡？先用 wf_click.py win 置前）")
        return 1
    print("RECORD_OK %s" % manifest)
    return 0


if __name__ == "__main__":
    sys.exit(main())
