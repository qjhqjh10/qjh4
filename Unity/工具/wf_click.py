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
  python -I d:/4/Unity/工具/wf_click.py drag 100 200 800 600 [按住ms] [每步ms]   # 🆕 2026-10-25：加按住/步长，模拟真人拖拽
  python -I d:/4/Unity/工具/wf_click.py wheel 960 540 [格数]     # 🆕 正=向上滚、负=向下（一格 = 120）
  python -I d:/4/Unity/工具/wf_click.py key esc | key space | key f5 | key ctrl+s | key w:200  (w 按住 200ms)
  python -I d:/4/Unity/工具/wf_click.py cursor          # 只读当前光标位置
输出一律 `ok ...` / `FAIL ...`（便于调用方判读）。
"""
import ctypes
import ctypes.wintypes as wt
import json
import sys
import time

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法（每条命令独立跑一次；`-I` 避免加载同目录的 `json.py` 之类）：\n  python -I d:/4/Unity/工具/wf_click.py info\n  python -I d:/4/Unity/工具/wf_click.py win Warpforge            # 找窗口（子串，不分大小写）并置前，打印 rect\n  python -I d:/4/Unity/工具/wf_click.py shot d:/4/_tmp_view/x.png [窗口子串]\n  python -I d:/4/Unity/工具/wf_click.py move 960 540\n  python -I d:/4/Unity/工具/wf_click.py click 960 540 [left|right] [次数]\n  python -I d:/4/Unity/工具/wf_click.py drag 100 200 800 600\n  python -I d:/4/Unity/工具/wf_click.py wheel 960 540 [格数]     # 🆕 正=向上滚、负=向下（一格 = 120）\n  python -I d:/4/Unity/工具/wf_click.py key esc | key space | key f5 | key ctrl+s | key w:200  (w 按住 200ms)\n  python -I d:/4/Unity/工具/wf_click.py cursor          # 只读当前光标位置\n输出一律 `ok ...` / `FAIL ...`（便于调用方判读）。\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

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
MOUSEEVENTF_WHEEL = 0x0800
WHEEL_DELTA = 120                      # Windows 一格滚轮 = 120（正数 = 向远离用户方向 = 向上滚）
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


def drag(x1, y1, x2, y2, steps=24, hold_ms=0, step_ms=12):
    """按下 → (可选)按住 hold_ms → 分 steps 步移动 → 停 80ms → 松开。

    🔴 2026-10-25 补：原来没有 `hold_ms`/`step_ms` ⇒ 「按下就动」这一种节奏。
    真机验「选目标态」时发现游戏**不认**这种节奏（`click_log` 里只剩一个释放点的空点），
    加这两个参数后可以模拟「按住 — 缓慢拖 — 松手」。命令行用法：
        drag x1 y1 x2 y2 [hold_ms] [step_ms]
    """
    move_to(x1, y1)
    time.sleep(0.05)
    mouse_event(MOUSEEVENTF_LEFTDOWN)
    if hold_ms:
        time.sleep(hold_ms / 1000.0)
    for i in range(1, steps + 1):
        move_to(x1 + (x2 - x1) * i / steps, y1 + (y2 - y1) * i / steps)
        time.sleep(step_ms / 1000.0)
    time.sleep(0.08)
    mouse_event(MOUSEEVENTF_LEFTUP)
    return True


def button_down(button="left"):
    """按下不松（配合 `up` 用）。🔴 2026-10-25 加：真机要「按住 → 移 → 截图 → 松开」，
    而 `drag` 是一次性同步的，中间态看不到。"""
    m = {"left": MOUSEEVENTF_LEFTDOWN, "right": MOUSEEVENTF_RIGHTDOWN,
         "middle": MOUSEEVENTF_MIDDLEDOWN}[button]
    mouse_event(m); time.sleep(0.05); return True


def button_up(button="left"):
    m = {"left": MOUSEEVENTF_LEFTUP, "right": MOUSEEVENTF_RIGHTUP,
         "middle": MOUSEEVENTF_MIDDLEUP}[button]
    mouse_event(m); time.sleep(0.05); return True


def wheel(x, y, notches=1):
    """在 (x,y) 上滚轮。notches > 0 = 向上滚（远离用户），< 0 = 向下。

    🔴 2026-10-12 补（新账 `A1712`）：原来**没有滚轮口** ⇒ `A1701`（卡组编辑「要先滚动一下
    才能点」）那条探针的「滚一格再点」这一步用工具做不到，而「滚动一下就恢复」恰恰是它
    三个候选里只有候选 3（指针状态陈旧）能解释的那一半。
    """
    move_to(x, y)
    ok = True
    for _ in range(max(1, abs(int(notches)))):
        inp = INPUT(type=0)            # INPUT_MOUSE
        inp.u.mi = MOUSEINPUT(0, 0, WHEEL_DELTA if notches > 0 else -WHEEL_DELTA,
                              MOUSEEVENTF_WHEEL, 0, None)
        ok = _send(inp) and ok
        time.sleep(0.06)
    return ok


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
        if cmd == "down":
            button_down(argv[2] if len(argv) > 2 else "left"); print("ok down"); return 0
        if cmd == "up":
            button_up(argv[2] if len(argv) > 2 else "left"); print("ok up"); return 0
        if cmd == "drag":
            hold = int(argv[6]) if len(argv) > 6 else 0
            step = int(argv[7]) if len(argv) > 7 else 12
            drag(int(argv[2]), int(argv[3]), int(argv[4]), int(argv[5]), hold_ms=hold, step_ms=step)
            print(f"ok drag hold={hold}ms step={step}ms"); return 0
        if cmd == "wheel":
            n = argv[4] if len(argv) > 4 else 1
            if not wheel(int(argv[2]), int(argv[3]), int(n)):
                print("FAIL wheel 发送失败"); return 1
            print("ok wheel", argv[2], argv[3], n); return 0
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
