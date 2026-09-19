#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""import_original_sfx.py — 把 **AnimFX `sounds` / `exitSounds` 指的那批原版音效**导进 Unity 工程

背景：每个特效的 `AnimFXController` 上有一组「特效开始后第 T 秒播一条声音」的定时条目
（`sounds[i].{time,sound,is2d,repeat,loops,timeInterval}`，另有 `exitSounds[]`）。
`sound` 指向的**不是 clip，是一个随机化 cue 包装**（`bundle_soundcollection_assets_all`
里同名的 MonoBehaviour），里面的 `clipList` 才是真 clip。

产出两块：

  ① **音频**：`MyGame/Assets/CardPresentation/Resources/Art/audio/sfx/<clip 名>.wav|ogg`
     —— `Resources/Art/` 整个被 `.gitignore` 排除（原版资产不进仓库，和美术/语音同一条规矩）。
  ② **表**：`MyGame/Assets/CardPresentation/Resources/animfx_sounds.json`
     —— **我们的分析产物，进仓库**（和 `voice_lines.json` 同一位置、同一形状）。
     键 = cue 名；值是 `clipList` + 音高/音量随机区间 + `timeToPlayAgain`。

数据源（唯一来源）：

  · bundle 本体 `d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/soundcollection_assets_all.bundle`
    （**必须读 bundle**：cue 里的 `clipList` 存的是 **PathID**，解包目录的文件名里没有 PathID ——
      实测过，`assets_full/.../MonoBehaviour/*.json` 拿不到 `pathid → 资产名` 的映射）
  · 谁在用   `d:/4/Unity/数据/游戏数据/animfx_modules.json` 的 `sounds[*].sound` / `exitSounds[*].sound`

用法：
    PYTHONIOENCODING=utf-8 python 工具/import_original_sfx.py --check   # 只报告，不写盘
    PYTHONIOENCODING=utf-8 python 工具/import_original_sfx.py           # 拷音频 + 写表
"""
import argparse
import glob
import io
import json
import os
import shutil
import sys

sys.stdout.reconfigure(encoding="utf-8")

import UnityPy

BUNDLE_DIR = "d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
UNPACK = "d:/2/新解包资源/assets_full"
# 兜底：cue 名字在这些包里找（先按名字找包，找不到才退回这几个）
BUNDLE_FALLBACK = ["soundcollection_assets_all", "battleprefabs_vfxandmisc_assets_all"]
MODULES = "d:/4/Unity/数据/游戏数据/animfx_modules.json"
DST_AUDIO = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio/sfx"
DST_JSON = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/animfx_sounds.json"

REF_PREFIX = "@asset:MonoBehaviour:"


def bundles_for(cue_names):
    """按名字定位 cue 在哪个包里（**返回裸名**，不带 `bundle_` 前缀；顺序 = 先两个音效主包，再其余）。

    🔴 **别再写死一个包**：第一次只读了 `soundcollection_assets_all`，结果 4 个 cue 报「bundle 里没有」——
       它们其实在 `battleprefabs_vfxandmisc_assets_all` 里（**又一个「搜错目录」**）。
    ⚠️ 顺序有意义：同名 MonoBehaviour 可能出现在别的包（卡动画包也有一堆同名资产），
       而**只有带 `clipList` 的那个才是 cue**。所以两个音效主包**先读**，其余按名字兜底。"""
    hits = set()
    for n in cue_names:
        for p in glob.glob(os.path.join(UNPACK, "bundle_*", "MonoBehaviour", n + ".json")):
            b = os.path.basename(os.path.dirname(os.path.dirname(p)))
            hits.add(b[len("bundle_"):] if b.startswith("bundle_") else b)
    rest = sorted(hits - set(BUNDLE_FALLBACK))
    return list(BUNDLE_FALLBACK) + rest


def load_bundles(names):
    """→ (cues, clip 名 → 源文件路径)。多个包的结果**合并**（cue 表按名字并，clip 按名去重）。"""
    cues, files = {}, {}
    for b in names:
        path = os.path.join(BUNDLE_DIR, b + ".bundle")
        if not os.path.exists(path):
            print(f"⚠️ bundle 不在（跳过）：{b}")
            continue
        env = UnityPy.load(path)
        name_of = {}          # pathid -> (type, name)
        n_clip = 0
        for obj in env.objects:
            nm = None
            try:
                nm = obj.read_typetree().get("m_Name")
            except Exception:                                    # noqa: BLE001
                pass
            name_of[obj.path_id] = (obj.type.name, nm)
            if obj.type.name == "AudioClip":
                n_clip += 1

        added = 0
        for obj in env.objects:
            if obj.type.name != "MonoBehaviour":
                continue
            d = obj.read_typetree()
            if "clipList" not in d:
                continue
            clips = []
            for c in d["clipList"]:
                t, n = name_of.get(c.get("m_PathID"), (None, None))
                clips.append(n if t == "AudioClip" else None)
            key = d.get("m_Name")
            if key in cues:
                continue
            cues[key] = {
                "clips": clips,
                "minPitch": d.get("minPitch", 1.0),
                "maxPitch": d.get("maxPitch", 1.0),
                "minVolume": d.get("minVolume", 1.0),
                "maxVolume": d.get("maxVolume", 1.0),
                "timeToPlayAgain": d.get("timeToPlayAgain", 0.0),
            }
            added += 1

        # 同包的音频文件（扩展名可能不同：wav / ogg / vorbis）
        cdir = os.path.join(UNPACK, "bundle_" + b, "AudioClip")
        got = 0
        if os.path.isdir(cdir):
            for f in sorted(os.listdir(cdir)):
                stem, ext = os.path.splitext(f)
                files.setdefault(stem.strip(), os.path.join(cdir, f))
                got += 1
        print(f"  · {b}：AudioClip {n_clip} 个 · 文件 {got} 个 · 新收 cue {added} 个")
    return cues, files


def used_cues():
    """→ (cue 名集合, 条目数, exitSounds 条目数)。从未含 `sound` 的槽位不算。"""
    d = json.load(io.open(MODULES, encoding="utf-8"))
    names, n, nex = set(), 0, 0
    for e in d["effects"]:
        for m in e["modules"]:
            if m["kind"] != "AnimFXController":
                continue
            kw = dict(zip(m["keys"], m["values"]))
            for k, v in kw.items():
                if not (k.startswith("sounds[") or k.startswith("exitSounds[")) \
                        or not k.endswith(".sound"):
                    continue
                if not v.startswith(REF_PREFIX):
                    print(f"⚠️ 认不出的引用形状：{v}（{e['name']}）")
                    continue
                names.add(v[len(REF_PREFIX):])
                if k.startswith("exitSounds"):
                    nex += 1
                else:
                    n += 1
    return names, n, nex


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只报告，不写盘")
    args = ap.parse_args()

    want, n_snd, n_exit = used_cues()
    print(f"特效在用：`sounds` 有值的 {n_snd} 条 + `exitSounds` {n_exit} 条 ⇒ **{n_snd + n_exit} 条**，"
          f"涉及 {len(want)} 个不同 cue")

    need_bundles = bundles_for(want)
    print(f"cue 分散在 {len(need_bundles)} 个包里，逐个读：")
    cues, files = load_bundles(need_bundles)
    print(f"合起来：cue 包装 {len(cues)} 个 · 能对上的音频文件 {len(files)} 个")

    # ---- ① 解析 cue → clip ----
    table, missing_cue, missing_clip, no_clip = [], [], set(), []
    for name in sorted(want):
        c = cues.get(name)
        if c is None:
            missing_cue.append(name)
            continue
        clips = [x for x in c["clips"] if x]
        # ⚠️ 资产名里有**尾随空格**的（`Hero of the Empire ` / `Tau Summon `）—— Windows 文件名存不下
        #    尾随空格，解包目录里是剥过的 ⇒ **两侧都要 strip 再比**，否则那两条静默落空。
        clips = [x.strip() for x in clips]
        if not clips:
            no_clip.append(name)
            continue
        got = []
        for cl in clips:
            if cl not in files:
                missing_clip.add(cl)
                continue
            got.append(cl)
        if not got:
            no_clip.append(name)
            continue
        table.append({
            "name": name, "clips": sorted(set(got)),
            "minPitch": round(c["minPitch"], 4), "maxPitch": round(c["maxPitch"], 4),
            "minVolume": round(c["minVolume"], 4), "maxVolume": round(c["maxVolume"], 4),
            "timeToPlayAgain": round(c["timeToPlayAgain"], 4),
        })

    need_files = sorted({cl for t in table for cl in t["clips"]})
    print(f"解析成功 {len(table)}/{len(want)} 个 cue；要用到 {len(need_files)} 个音频文件")
    if missing_cue:
        print(f"⚠️ bundle 里没有这个 cue（{len(missing_cue)}）：{missing_cue}")
    if no_clip:
        print(f"⚠️ cue 在、但 clipList 解不出名字（{len(no_clip)}）：{no_clip}")
    if missing_clip:
        print(f"⚠️ clip 名对不上解包文件（{len(missing_clip)}）：{sorted(missing_clip)}")

    # ---- ② 拷音频 ----
    copied = skipped = 0
    exts = {}
    if not args.check:
        os.makedirs(DST_AUDIO, exist_ok=True)
        for cl in need_files:
            src, dst = files[cl], os.path.join(DST_AUDIO, cl)
            ext = os.path.splitext(src)[1].lower()
            exts[ext] = exts.get(ext, 0) + 1
            if os.path.exists(dst + ext) and os.path.getsize(dst + ext) == os.path.getsize(src):
                skipped += 1
                continue
            shutil.copy2(src, dst + ext)
            copied += 1

    # ---- ③ 写表 ----
    out = {
        "version": 1,
        "note": "AnimFX `sounds`/`exitSounds` 的随机化 cue 表。clips 里随机挑一条；"
                "播放音量 = minVolume..maxVolume、音高 = minPitch..maxPitch；"
                "文件名在 Resources/Art/audio/sfx/<clip 名>。",
        "source": {
            "bundle": "soundcollection_assets_all.bundle",
            "who": "数据/游戏数据/animfx_modules.json 的 sounds[*].sound",
            "script": "工具/import_original_sfx.py",
        },
        "cues": table,
    }
    if not args.check:
        io.open(DST_JSON, "w", encoding="utf-8", newline="\n").write(
            json.dumps(out, ensure_ascii=False, indent=1))

    if args.check:
        print("（--check：没有写盘）")
    else:
        print(f"已拷 {copied} 条 · 已存在跳过 {skipped} 条 → {DST_AUDIO}（扩展名分布 {exts}）")
        print(f"表已写：{DST_JSON}")


if __name__ == "__main__":
    main()
