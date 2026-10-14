# -*- coding: utf-8 -*-
"""W4 只读助手：读 Loc.cs 那张表 / 官方 en+zh-CN 表。⛔ 本文件不改任何生产文件。"""
import io
import json
import os
import re

ROOT = "d:/4/Unity"
LOC = ROOT + "/MyGame/Assets/CardPresentation/Core/Loc.cs"
OFFICIAL = ROOT + "/数据/游戏数据/官方本地化_en+zh-CN.json"

PAT = re.compile(r'\{\s*"([^"]+)"\s*,\s*new Entry\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\)', re.S)
# ⚠️ 末尾**不要求** `\s*\}` —— `MainMenu/PurchasePremium/Description` 那条是多行写法、收尾带注释，
#    要求 `}` 会**静默漏掉一条**（2026-10-14 实测：605 vs 606 那个差 1 就是它）。


def loc_table(path=None):
    s = io.open(path or LOC, encoding="utf-8").read()
    return {m.group(1): (m.group(2), m.group(3)) for m in PAT.finditer(s)}


def official(family=None):
    t = json.load(io.open(OFFICIAL, encoding="utf-8"))["terms"]
    if family is None:
        return t
    return {k: v for k, v in t.items() if k == family or k.startswith(family + "/")}


def official_pairs(family=None):
    """同 `official()`，但值统一成 `(en, zh)` 两元组（原表里 en == zh 的压成了单个字符串）。"""
    out = {}
    for k, v in official(family).items():
        if isinstance(v, list):
            out[k] = (v[0] if len(v) > 0 else "", v[1] if len(v) > 1 else "")
        else:
            out[k] = (v or "", v or "")
    return out


def fam(key):
    return key.split("/", 1)[0]


def unesc(s):
    return s.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")
