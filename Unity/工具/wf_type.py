#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""wf_type.py —— 往当前焦点控件里「打一串文字」。

🔴 2026-11-27 现场实测（Warpforge PC）：
  · `SendInput`（无论只带 SCANCODE / 只带 wVk / 两者都带 / UNICODE）**都进不去** ——
    光标在闪、`ESC` 能被游戏收到，但**不产生字符**（原版账户页邮箱框同样如此
    ⇒ 不是我们自建输入框的问题）；
  · 直接 `PostMessage(WM_CHAR)` 到游戏窗口 **立即可见**（实测 `xyz` 进了原版邮箱框）。
⇒ 默认走 `--mode post`。

用法：
  python -I -X utf8 E:/warpforge/_tmp_view/tools/wf_type.py "192.168.2.102:8764"
  python -I -X utf8 E:/warpforge/_tmp_view/tools/wf_type.py --clear 24 ""     # 先退格 24 次
  python -I -X utf8 E:/warpforge/_tmp_view/tools/wf_type.py --hwnd 0x9069A "abc"
说明：调用前先点进输入框；窗口默认取「前台窗口」。
"""
import ctypes
import sys
import time

user32 = ctypes.WinDLL("user32", use_last_error=False)
user32.VkKeyScanW.argtypes = (ctypes.c_wchar,)
user32.VkKeyScanW.restype = ctypes.c_short
user32.MapVirtualKeyW.argtypes = (ctypes.c_uint, ctypes.c_uint)
user32.MapVirtualKeyW.restype = ctypes.c_uint

KEYEVENTF_KEYUP = 0x0002
KEYEVENTF_UNICODE = 0x0004
KEYEVENTF_SCANCODE = 0x0008

WM_KEYDOWN, WM_KEYUP, WM_CHAR = 0x0100, 0x0101, 0x0102


class KEYBDINPUT(ctypes.Structure):
    _fields_ = [("wVk", ctypes.c_ushort), ("wScan", ctypes.c_ushort),
                ("dwFlags", ctypes.c_ulong), ("time", ctypes.c_ulong),
                ("dwExtraInfo", ctypes.c_void_p)]


class _U(ctypes.Union):
    _fields_ = [("ki", KEYBDINPUT)]


class INPUT(ctypes.Structure):
    _fields_ = [("type", ctypes.c_ulong), ("u", _U)]


MODE = "post"
HWND = 0


def send(vk, scan, flags):
    user32.SendInput.argtypes = (ctypes.c_uint, ctypes.c_void_p, ctypes.c_int)
    inp = INPUT(type=1)
    inp.u.ki = KEYBDINPUT(vk, scan, flags, 0, None)
    user32.SendInput(1, ctypes.byref(inp), ctypes.sizeof(INPUT))


def post_char(ch):
    user32.PostMessageW(HWND, WM_CHAR, ord(ch), 1)
    time.sleep(0.03)


def post_key(vk):
    user32.PostMessageW(HWND, WM_KEYDOWN, vk, 1)
    time.sleep(0.02)
    user32.PostMessageW(HWND, WM_KEYUP, vk, 1)
    time.sleep(0.04)


def tap_vk(vk, shift=False):
    scan = user32.MapVirtualKeyW(vk, 0)

    def ev(up=False):
        if MODE == "both":
            send(vk, scan, KEYEVENTF_KEYUP if up else 0)
        elif MODE == "vk":
            send(vk, 0, KEYEVENTF_KEYUP if up else 0)
        elif MODE == "unicode":
            pass
        else:
            send(vk, scan, KEYEVENTF_SCANCODE | (KEYEVENTF_KEYUP if up else 0))

    if MODE == "post":
        post_key(vk)
        return
    if shift:
        send(0x10, user32.MapVirtualKeyW(0x10, 0), 0)
        time.sleep(0.01)
    ev(False)
    time.sleep(0.03)
    ev(True)
    if shift:
        time.sleep(0.01)
        send(0x10, user32.MapVirtualKeyW(0x10, 0), KEYEVENTF_KEYUP)
    time.sleep(0.05)


def tap_unicode(ch):
    for up in (False, True):
        inp = INPUT(type=1)
        inp.u.ki = KEYBDINPUT(0, ord(ch), KEYEVENTF_UNICODE | (KEYEVENTF_KEYUP if up else 0), 0, None)
        user32.SendInput(1, ctypes.byref(inp), ctypes.sizeof(INPUT))
    time.sleep(0.05)


def type_text(text):
    ok = 0
    for ch in text:
        if MODE == "post":
            post_char(ch)
        elif MODE == "unicode":
            tap_unicode(ch)
        else:
            r = user32.VkKeyScanW(ch)
            if r == -1:
                print("WARN 无键位:", repr(ch)); continue
            tap_vk(r & 0xFF, bool((r >> 8) & 1))
        ok += 1
    return ok


def main():
    global MODE, HWND
    args = sys.argv[1:]
    clear = 0
    while args and args[0].startswith("--"):
        if args[0] == "--mode":
            MODE = args[1]; args = args[2:]
        elif args[0] == "--clear":
            clear = int(args[1]); args = args[2:]
        elif args[0] == "--hwnd":
            HWND = int(args[1], 0); args = args[2:]
        else:
            print("WARN 未知参数", args[0]); args = args[1:]
    if HWND == 0:
        HWND = user32.GetForegroundWindow()
    text = args[0] if args else ""
    for _ in range(clear):
        tap_vk(0x08)          # backspace
    n = type_text(text)
    print("ok typed %d chars (mode=%s hwnd=0x%X): %r" % (n, MODE, HWND, text))


if __name__ == "__main__":
    main()
