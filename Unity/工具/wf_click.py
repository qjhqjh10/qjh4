#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""wf_click.py —— 真机鼠标 / 键盘 / 截图工具（给「真 Play」用；纯 ctypes + Pillow，无第三方输入库）

为什么有它：本工程的复刻品**只能在批处理里验证断言**，而「鼠标点得动吗 / 手感对不对 / 版面像不像」
这些**批处理验不了**（`资料/真Play待验清单.md` §一）。有了它，主对话可以：
**先截图 → 看图 → 再点**（而不是盲点）。

⚠️ **它动的是【真鼠标】** —— 光标会真的移过去、真的按下。用法纪律：
  ① **先 `shot` 再 `click`**：每次都先看一眼图，别按记忆点；
  ② 点之前先 `win` 把游戏窗口**置前**（否则点到别的窗口上）；
  ③ 坐标一律是**屏幕像素**（已按 DPI 感知取，与截图 1:1）。

用法（每条命令独立跑一次；`-I` 避免加载同目录的 `json.py` 之类）：
  python -I d:/4/Unity/工具/wf_click.py info
  python -I d:/4/Unity/工具/wf_click.py win Warpforge            # 找窗口（子串，不分大小写）并置前，打印 rect
  python -I d:/4/Unity/工具/wf_click.py shot d:/4/_tmp_view/x.png [窗口子串]
  python -I d:/4/Unity/工具/wf_click.py move 960 540
  python -I d:/4/Unity/工具/wf_click.py click 960 540 [left|right] [次数]
  python -I d:/4/Unity/工具/wf_click.py drag 100 200 800 600
  python -I d:/4/Unity/工具/wf_click.py key esc | key space | key f5 | key ctrl+s | key w:200  (w 按住 200ms)
  python -I d:/4/Unity/工具/wf_click.py cursor          # 只读当前光标位置
输出一律 `ok ...` / `FAIL ...`（便于调用方判读）。
"""
import ctypes
import ctypes.wintypes as wt
import json
import sys
import time

user32 = ctypes.windll.user32
try:                                   # 让坐标与真实像素 1:1（缩放 125% 的机器必须有这一步）
    user32.SetProcessDpiAwarenessContext(ctypes.c_void_p(-4))   # PER_MONITOR_AWARE_V2
except Exception:
    try:
        ctypes.windll.shcore.SetProcessDpiAwareness(2)
    except Exception:
        user32.SetProcessDPIAware()

# ---------------------------------------------------------------- 窗口
def enum_windows():
    out = []

    @ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)
    def cb(hwnd, _):
        if not user32.IsWindowVisible(hwnd):
            return True
        n = user32.GetWindowTextLengthW(hwnd)
        if n <= 0:
            return True
        buf = ctypes.create_unicode_buffer(n + 1)
        user32.GetWindowTextW(hwnd, buf, n + 1)
        r = wt.RECT()
        user32.GetWindowRect(hwnd, ctypes.byref(r))
        out.append(dict(hwnd=hwnd, title=buf.value,
                        x=r.left, y=r.top, w=r.right - r.left, h=r.bottom - r.top))
        return True

    user32.EnumWindows(cb, 0)
    return out


def find_window(sub):
    sub = (sub or "").lower()
    for w in enum_windows():
        if sub in w["title"].lower():
            return w
    return None


def focus(sub):
    w = find_window(sub)
    if not w:
        return None
    if user32.IsIconic(w["hwnd"]):
        user32.ShowWindow(w["hwnd"], 9)          # SW_RESTORE
    user32.SetForegroundWindow(w["hwnd"])
    time.sleep(0.35)
    w2 = find_window(sub) or w
    return w2


# ---------------------------------------------------------------- 输入
PUL = ctypes.POINTER(ctypes.c_ulong)


class MOUSEINPUT(ctypes.Structure):
    _fields_ = [("dx", ctypes.c_long), ("dy", ctypes.c_long), ("mouseData", ctypes.c_ulong),
                ("dwFlags", ctypes.c_ulong), ("time", ctypes.c_ulong),
                ("dwExtraInfo", PUL)]


class KEYBDINPUT(ctypes.Structure):
    _fields_ = [("wVk", ctypes.c_ushort), ("wScan", ctypes.c_ushort),
                ("dwFlags", ctypes.c_ulong), ("time", ctypes.c_ulong),
                ("dwExtraInfo", PUL)]


class _INPUTunion(ctypes.Union):
    _fields_ = [("mi", MOUSEINPUT), ("ki", KEYBDINPUT)]


class INPUT(ctypes.Structure):
    _fields_ = [("type", ctypes.c_ulong), ("u", _INPUTunion)]


MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP = 0x0002, 0x0004
MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP = 0x0008, 0x0010
MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP = 0x0020, 0x0040
KEYEVENTF_KEYUP = 0x0002
KEYEVENTF_UNICODE = 0x0004


def _send(inp):
    n = user32.SendInput(1, ctypes.byref(inp), ctypes.sizeof(INPUT))
    return n == 1


def mouse_event(flags):
    inp = INPUT(type=0)                    # INPUT_MOUSE
    inp.u.mi = MOUSEINPUT(0, 0, 0, flags, 0, None)
    return _send(inp)


def move_to(x, y):
    ok = bool(user32.SetCursorPos(int(x), int(y)))
    time.sleep(0.05)
    return ok


def click(x, y, button="left", times=1):
    d = {"left": (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP),
         "right": (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP),
         "middle": (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP)}[button]
    move_to(x, y)
    for _ in range(int(times)):
        mouse_event(d[0]); time.sleep(0.04); mouse_event(d[1]); time.sleep(0.09)
    return True


def drag(x1, y1, x2, y2, steps=24):
    move_to(x1, y1)
    mouse_event(MOUSEEVENTF_LEFTDOWN)
    for i in range(1, steps + 1):
        move_to(x1 + (x2 - x1) * i / steps, y1 + (y2 - y1) * i / steps)
        time.sleep(0.012)
    time.sleep(0.08)
    mouse_event(MOUSEEVENTF_LEFTUP)
    return True


VK = {"esc": 0x1B, "escape": 0x1B, "space": 0x20, "enter": 0x0D, "return": 0x0D,
      "tab": 0x09, "shift": 0x10, "ctrl": 0x11, "control": 0x11, "alt": 0x12,
      "up": 0x26, "down": 0x28, "left": 0x25, "right": 0x27, "back": 0x08,
      "spacebar": 0x20, "f1": 0x70, "f2": 0x71, "f3": 0x72, "f4": 0x73, "f5": 0x74,
      "f6": 0x75, "f7": 0x76, "f8": 0x77, "f9": 0x78, "f10": 0x79, "f11": 0x7A, "f12": 0x7B}


def key(spec):
    """key esc · key ctrl+s · key w:200（按住 200ms）"""
    hold = 0.05
    if ":" in spec:
        spec, ms = spec.split(":", 1)
        hold = max(0.02, int(ms) / 1000.0)
    parts = [p.strip().lower() for p in spec.split("+") if p.strip()]
    vks = []
    for p in parts:
        if p in VK:
            vks.append(VK[p])
        elif len(p) == 1:
            vk = user32.VkKeyScanW(ctypes.c_wchar(p))
            vks.append(vk & 0xFF)
        else:
            return False
    for vk in vks:
        inp = INPUT(type=1); inp.u.ki = KEYBDINPUT(vk, 0, 0, 0, None); _send(inp)
    time.sleep(hold)
    for vk in reversed(vks):
        inp = INPUT(type=1); inp.u.ki = KEYBDINPUT(vk, 0, KEYEVENTF_KEYUP, 0, None); _send(inp)
    return True


# ---------------------------------------------------------------- 截图
def shot(path, win_sub=None):
    from PIL import ImageGrab
    box = None
    if win_sub:
        w = find_window(win_sub)
        if not w:
            print("FAIL 找不到窗口:", win_sub); return False
        box = (w["x"], w["y"], w["x"] + w["w"], w["y"] + w["h"])
    im = ImageGrab.grab(bbox=box, all_screens=True)
    im.save(path)
    print("ok shot %s %dx%d box=%s" % (path, im.size[0], im.size[1], box))
    return True


def cursor():
    p = wt.POINT(); user32.GetCursorPos(ctypes.byref(p))
    print("ok cursor %d %d" % (p.x, p.y))
    return True


def main(argv):
    if len(argv) < 2:
        print(__doc__); return 2
    cmd = argv[1].lower()
    try:
        if cmd == "info":
            print(json.dumps(enum_windows(), ensure_ascii=False, indent=1)[:4000]); return 0
        if cmd == "win":
            w = focus(argv[2])
            if not w:
                print("FAIL 找不到窗口:", argv[2]); return 1
            print("ok win %s rect=(%d,%d %dx%d)" % (w["title"], w["x"], w["y"], w["w"], w["h"]))
            return 0
        if cmd == "shot":
            return 0 if shot(argv[2], argv[3] if len(argv) > 3 else None) else 1
        if cmd == "move":
            move_to(argv[2], argv[3]); print("ok move", argv[2], argv[3]); return 0
        if cmd == "click":
            btn = argv[4] if len(argv) > 4 else "left"
            n = argv[5] if len(argv) > 5 else 1
            click(argv[2], argv[3], btn, int(n)); print("ok click", argv[2], argv[3], btn, n); return 0
        if cmd == "drag":
            drag(int(argv[2]), int(argv[3]), int(argv[4]), int(argv[5])); print("ok drag"); return 0
        if cmd == "key":
            if not key(argv[2]):
                print("FAIL 认不出这个键:", argv[2]); return 1
            print("ok key", argv[2]); return 0
        if cmd == "cursor":
            return 0 if cursor() else 1
    except Exception as e:
        print("FAIL %s: %s" % (type(e).__name__, e)); return 1
    print("FAIL 不认识的子命令:", cmd); return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
