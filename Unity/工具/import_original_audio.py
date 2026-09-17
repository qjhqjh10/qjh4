#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""import_original_audio.py — 把**原版单位语音**导进 Unity 工程 + 生成运行时映射

产出两块：

  ① **音频**：`MyGame/Assets/CardPresentation/Resources/Art/audio/vo/<原文件名>.ogg|wav`
     —— `Resources/Art/` 整个被 `.gitignore` 排除（原版资产不进仓库，和美术同一条规矩）。
     这一份是**运行时真正加载的那份**（`Resources.Load<AudioClip>("Art/audio/vo/<名>")`）。
  ② **映射**：`MyGame/Assets/CardPresentation/Resources/voice_lines.json`
     —— **我们的分析产物，进仓库**。键 = 我们卡表的 `id`，值是每张卡的台词表
     （事件 + 文件名 + 台词原文）。运行时 `VoiceLines.cs` 读它。

数据源（**唯一来源**，2026-09-12 起「以 `assets_full` 为准」那条规矩）：
  · 音频  `d:/2/新解包资源/assets_full/bundle_*cardassets_assets_all/AudioClip/`
          （13 个包、**1857 条 / 70.59 MiB**；`d:/2/解包整理/01_卡牌/*/AudioClip/` 是同内容第二份）
  · 归属  `d:/4/Unity/数据/索引/card_index.json` 的 `cards[].voice`
          （602 张卡 / 1778 条引用；实测 **1778/1778 在音频源里都找得到**）
  · 卡名  `MyGame/Assets/RuleEngine/Resources/cards_engine.json`
          （拿 `(阵营, 卡名)` 把 index 的卡并到**我们的 id** 上 —— index 里没有 id 字段）

⚠️ **四条坑**（`资料/普查产出_0913/语音索引.md` §三 + 2026-09-17 实测）：
  1. **别只 copy `*.ogg`** —— DarkAngels 31 条 + Ultramarines 84 条是 `.wav`，
     `Guilliman` / `Valius Paxor` 两个督军**全套都是 wav**，按 ogg 过滤会整个漏掉。
  2. **死灵族有 44 条不带 `VO_` 前缀**（`Sautekh_Orikan_attack.ogg`），其中 **21 条是被引用的**。
  3. **扩展名/大小写/写法都不统一**：`_attack` 与 `_Attack` · `_cant`（死灵）与 `_cantDo` ·
     `_backup` 与 `_Backup1..N` ⇒ 事件名一律**小写规范化**后再比。
  4. **卡名本身含 `_`**（`Blissbringer_High-pitch_Screech`）⇒ **不能按最后一个 `_` 切分**，
     归属一律用 `card_index.json`，脚本只对**孤儿**（索引没收录的）才按名字猜，并标出来。

用法：
    PYTHONIOENCODING=utf-8 python 工具/import_original_audio.py --check   # 只报告，不写盘
    PYTHONIOENCODING=utf-8 python 工具/import_original_audio.py           # 拷音频 + 写映射
"""
import argparse
import glob
import io
import json
import os
import re
import shutil
import sys

sys.stdout.reconfigure(encoding="utf-8")

UNPACK = "d:/2/新解包资源/assets_full"
CARD_INDEX = "d:/4/Unity/数据/索引/card_index.json"
CARDS_ENGINE = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
DST_AUDIO = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio/vo"
DST_JSON = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/voice_lines.json"

# 认得出的事件后缀（**小写**比较）。⚠️ `wp` 的语义未证实（只在文件名里出现），
# 但**照原样收进来**不解释 —— 不认识的照样进表，事件名就叫它自己。
KNOWN_EVENTS = {
    "greet", "attack", "death", "concede", "hurry", "intro", "mirror", "threat",
    "wp", "tbc", "cantdo", "cant", "backup",
}
_EVENT_RE = re.compile(r"^(gen[1-9]|backup\d+|vs.*)$")


def norm_event(suffix):
    """文件名尾段 → 规范事件名（认不出返回 None）。"""
    s = suffix.lower()
    if s in KNOWN_EVENTS:
        return "cantdo" if s == "cant" else ("backup" if s == "backup" else s)
    if _EVENT_RE.match(s):
        return s
    return None


def parse_name(base):
    """文件名 → (事件, 台词原文)。`<...> - <台词> (配音演员)` 这种是**出场台词**（普通单位就这一条）。"""
    stem = os.path.splitext(base)[0]
    if " - " in stem:
        text = stem.split(" - ", 1)[1].strip()
        # 去掉结尾的配音演员名（`(Vivienne)` 这种）
        text = re.sub(r"\s*\([^)]*\)\s*$", "", text).strip()
        return "line", text
    # 没有 " - " 的：看尾段是不是事件
    tail = stem.rsplit("_", 1)[-1] if "_" in stem else ""
    ev = norm_event(tail)
    return (ev, "") if ev else ("line", "")


def load_audio_index():
    """{文件名(basename): 完整路径} —— 13 个 bundle 的 AudioClip 全平铺。"""
    idx = {}
    for d in sorted(glob.glob(os.path.join(UNPACK, "bundle_*cardassets*_assets_all", "AudioClip"))):
        for f in os.listdir(d):
            idx.setdefault(f, os.path.join(d, f))
    return idx


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只报告，不写盘")
    args = ap.parse_args()

    audio = load_audio_index()
    print(f"音频源：{len(audio)} 个文件（{len(set(os.path.basename(p) for p in audio.values()))} 个不同文件名）")

    index_cards = json.load(io.open(CARD_INDEX, encoding="utf-8"))["cards"]
    eng_cards = json.load(io.open(CARDS_ENGINE, encoding="utf-8"))["cards"]
    ekey = {(c["faction"].lower(), c["name"]): c for c in eng_cards}

    # ---- ① 按 card_index 的归属建表（键 = 我们的 id）----
    lines = {}          # id -> [ {"ev","file","text"} ]
    card_meta = {}      # id -> (name, faction)
    used_files = set()
    missing = []
    no_join = []

    for c in index_cards:
        if not c.get("voice"):
            continue
        eng = ekey.get((c["faction"].lower(), c["name"]))
        if eng is None:
            no_join.append((c["faction"], c["name"]))
            continue
        cid = eng["id"]
        card_meta[cid] = (eng["name"], eng["faction"])
        for v in c["voice"]:
            base = os.path.basename(v.replace("\\", "/"))
            if base not in audio:
                missing.append(base)
                continue
            ev, text = parse_name(base)
            lines.setdefault(cid, []).append({"ev": ev, "file": base, "text": text})
            used_files.add(base)

    # ---- ② 孤儿：索引没收录、但文件名能对上我们某张卡的（**只有这一步是猜的，标出来**）----
    orphans = []
    eng_by_name = {}
    for c in eng_cards:
        eng_by_name.setdefault(c["name"].lower(), []).append(c)
    for base, path in sorted(audio.items()):
        if base in used_files:
            continue
        stem = os.path.splitext(base)[0]
        ev, text = parse_name(base)
        hit = None
        for name_l, cards in eng_by_name.items():
            # 文件名里出现 `_<卡名>_` 才算（卡名含空格/撇号，原样比）
            if ("_" + cards[0]["name"] + "_") in stem or stem.endswith("_" + cards[0]["name"]):
                if len(cards) == 1:
                    hit = cards[0]
                    break
        if hit is None:
            orphans.append(base)
            continue
        cid = hit["id"]
        card_meta.setdefault(cid, (hit["name"], hit["faction"]))
        lines.setdefault(cid, []).append({"ev": ev, "file": base, "text": text, "src": "orphan"})
        used_files.add(base)

    # ---- ③ 拷贝音频 ----
    copied = skipped = 0
    if not args.check:
        os.makedirs(DST_AUDIO, exist_ok=True)
        for base in sorted(used_files):
            src, dst = audio[base], os.path.join(DST_AUDIO, base)
            if os.path.exists(dst) and os.path.getsize(dst) == os.path.getsize(src):
                skipped += 1
                continue
            shutil.copy2(src, dst)
            copied += 1

    # ---- ④ 写映射 ----
    # ⚠️ `cards` 是**数组不是字典** —— Unity 的 `JsonUtility` 反序列化不了字典
    #    （`VoiceLines.cs` 直接吃这个形状；要用字典就得再引一个 JSON 库，不值当）。
    out = {
        "version": 1,
        "note": "原版单位语音表。id = 我们卡表的 id（cards_engine.json）；"
                "file = Resources/Art/audio/vo/<名>（无扩展名时按文件名去掉扩展名）；"
                "ev 见 语音索引.md §四（`wp` 语义未证实）。",
        "source": {
            "audio": "d:/2/新解包资源/assets_full/bundle_*cardassets*_assets_all/AudioClip/",
            "owner": "d:/4/Unity/数据/索引/card_index.json 的 cards[].voice",
            "script": "工具/import_original_audio.py",
        },
        "cards": [
            {"id": cid, "name": card_meta[cid][0], "faction": card_meta[cid][1], "lines": rows}
            for cid, rows in sorted(lines.items())
        ],
    }
    if not args.check:
        io.open(DST_JSON, "w", encoding="utf-8", newline="\n").write(
            json.dumps(out, ensure_ascii=False, indent=1))

    total_lines = sum(len(v) for v in lines.values())
    evs = {}
    for rows in lines.values():
        for r in rows:
            evs[r["ev"]] = evs.get(r["ev"], 0) + 1
    print(f"卡片：有语音 {len(lines)} 张（其中并到我们卡表 {len(lines) - 0} 张）· 台词 {total_lines} 条")
    print(f"对不上卡表的 index 卡：{len(no_join)} 张 {no_join[:6]}")
    print(f"音频源里找不到的引用：{len(missing)} 条 {missing[:3]}")
    print(f"孤儿（按文件名并进我们卡表的）：{len(used_files) - total_lines + len(lines) * 0} 条"
          f"（未并上的 {len(orphans)} 条，例 {orphans[:3]}）")
    print("事件分布：", dict(sorted(evs.items(), key=lambda kv: -kv[1])))
    if args.check:
        print("（--check：没有写盘）")
    else:
        print(f"已拷 {copied} 条 · 已存在跳过 {skipped} 条 → {DST_AUDIO}")
        print(f"映射已写：{DST_JSON}")


if __name__ == "__main__":
    main()
