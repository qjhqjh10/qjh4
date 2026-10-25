#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""wf_postkey.py — 把按键【直接投递】到指定窗口（wf_click.py key 的备选路）

为什么要它（2026-10-25 真机实测）：
  · `wf_click.py key` 用的是 SendInput（注入到前台窗口），但 Warpforge 用的是
    **新输入系统（activeInputHandler = 1）**，实测 ESC / 方向键**送不进去**
    （收藏窗 ESC 也不关、方向键无反应，日志里一条 `[Win] ESC` 都没有）。
  · 本工具改用 `PostMessage(WM_KEYDOWN/WM_KEYUP)` 直接投给窗口消息队列，
    不改焦点、不依赖前台。**能不能被新输入系统吃到要实测** —— 本工具只是那条备选路。

用法：
  python -I wf_postkey.py <窗口子串> <键> [按住ms]
  键 = esc / enter / space / up / down / left / right / 单字符
  例：python -I wf_postkey.py Warpforge esc
输出：ok posted ... / FAIL ...
"""
import ctypes
import ctypes.wintypes as wt
import sys
import time

user32 = ctypes.windll.user32

VK = {"esc": 0x1B, "escape": 0x1B, "space": 0x20, "enter": 0x0D, "return": 0x0D,
      "tab": 0x09, "up": 0x26, "down": 0x28, "left": 0x25, "right": 0x27,
      "back": 0x08, "f1": 0x70, "f2": 0x71, "f3": 0x72, "f4": 0x73, "f5": 0x74}
WM_KEYDOWN, WM_KEYUP, WM_CHAR = 0x0100, 0x0101, 0x0102


def find_window(sub):
    sub = sub.lower()
    hits = []

    @ctypes.WINFUNCTYPE(wt.BOOL, wt.HWND, wt.LPARAM)
    def cb(hwnd, _):
        if not user32.IsWindowVisible(hwnd):
            return True
        n = user32.GetWindowTextLengthW(hwnd)
        if n <= 0:
            return True
        buf = ctypes.create_unicode_buffer(n + 1)
        user32.GetWindowTextW(hwnd, buf, n + 1)
        if sub in buf.value.lower():
            hits.append((hwnd, buf.value))
        return True

    user32.EnumWindows(cb, 0)
    return hits[0] if hits else None


def main():
    if len(sys.argv) < 3:
        print("FAIL 用法: wf_postkey.py <窗口子串> <键> [按住ms] [--scan]")
        return 2
    w = find_window(sys.argv[1])
    if not w:
        print("FAIL 找不到窗口:", sys.argv[1])
        return 1
    hwnd, title = w
    spec = sys.argv[2].lower()
    hold = int(sys.argv[3]) / 1000.0 if len(sys.argv) > 3 and sys.argv[3].isdigit() else 0.06
    scan_mode = "--scan" in sys.argv
    if spec in VK:
        vk = VK[spec]
    elif len(spec) == 1:
        vk = user32.VkKeyScanW(ctypes.c_wchar(spec)) & 0xFF
    else:
        print("FAIL 不认识的键:", spec)
        return 2

    if scan_mode:
        # 打 scancode 版本：新输入系统走 raw input 时只认这一种注入
        scan = user32.MapVirtualKeyW(vk, 0)  # MAPVK_VK_TO_VSC
        user32.keybd_event(vk, scan, 0x0008, 0)          # KEYEVENTF_SCANCODE
        time.sleep(hold)
        user32.keybd_event(vk, scan, 0x0008 | 0x0002, 0)  # +KEYEVENTF_KEYUP
        print("ok keybd_event(scancode) %s(0x%02X) scan=0x%02X -> %r" % (spec, vk, scan, title))
        return 0

    # lParam: repeat=1, scancode 交给系统（0 也能被多数 WndProc 接受）
    user32.PostMessageW(hwnd, WM_KEYDOWN, vk, 0x00000001)
    time.sleep(hold)
    user32.PostMessageW(hwnd, WM_KEYUP, vk, 0xC0000001)
    print("ok posted %s(0x%02X) -> hwnd=%s %r" % (spec, vk, hwnd, title))
    return 0


if __name__ == "__main__":
    sys.exit(main())
