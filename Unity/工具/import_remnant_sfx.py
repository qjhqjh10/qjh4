#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""import_remnant_sfx.py — 把**残骸体那六条 AudioCue** 指的原版音频导进 Unity 工程。

为什么单开一个脚本（不并进 `import_original_sfx.py`）：
那个脚本的输入是 `animfx_modules.json` 里 `sounds[*].sound` 引到的 cue —— **只覆盖 AnimFX**。
残骸这六条是 `RemnantBody` / `RemnantAeldari` / `RemnantNecrons` 上**序列化的 AudioCue 字段**，
不在那份清单里（`BattleDoors` 那三支 jingle 当初也是手工拷的）。

判据（2026-09-29 从 `dump.cs` 字段名 + 反编译用法逐条核实，**不是按 cue 名字猜**）：

  | 阵营 | 出现（`RemnantBody.toRemnantSound` +0x38） | 收集 | 被打掉（`RemnantBody.deathSound` +0x50） |
  |---|---|---|---|
  | 灵族 | `Aeldari To Waystone` | `Aeldari Waystone Collect`（+0x70） | `Aeldari Waystone Destruction` |
  | 死灵 | `CardShatter` | `NecronsCardReanimate`（+0x90） | `RemnantsDestroyed` |

⚠️ **六条 cue → 五条真 clip**：`Aeldari To Waystone` 与 `Aeldari Waystone Collect` 共用
`Aeldari To Waystone Death`；`NecronsCardReanimate` 指向的是 **`CardReanimate`**（**cue 名 ≠ clip 名**）。

产物：`MyGame/Assets/CardPresentation/Resources/Art/audio/sfx/<clip 名>.wav`
（`Resources/Art/` 整个被 `.gitignore` 排除 —— 原版资产不进仓库，与美术/语音同一条规矩。）

用法：
    PYTHONIOENCODING=utf-8 python 工具/import_remnant_sfx.py --check   # 只报告
    PYTHONIOENCODING=utf-8 python 工具/import_remnant_sfx.py           # 拷贝
"""
import argparse
import io
import os
import shutil
import sys

sys.stdout.reconfigure(encoding="utf-8")

import UnityPy

BUNDLE = ("d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/"
          "StandaloneWindows64/soundcollection_assets_all.bundle")
UNPACK = "d:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/AudioClip"
DEST = "MyGame/Assets/CardPresentation/Resources/Art/audio/sfx"

# 三时机 × 两阵营 —— 键是「时机 + 阵营」，值是原版 cue 名
CUES = [
    ("Aeldari", "toRemnant",  "Aeldari To Waystone"),
    ("Aeldari", "collect",    "Aeldari Waystone Collect"),
    ("Aeldari", "death",      "Aeldari Waystone Destruction"),
    ("Necrons", "toRemnant",  "CardShatter"),
    ("Necrons", "collect",    "NecronsCardReanimate"),
    ("Necrons", "death",      "RemnantsDestroyed"),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只报告，不写盘")
    a = ap.parse_args()

    if not os.path.exists(BUNDLE):
        print(f"🔴 bundle 不在：{BUNDLE}"); return 1

    env = UnityPy.load(BUNDLE)
    name_of = {}
    for o in env.objects:
        nm = None
        try:
            nm = o.read_typetree().get("m_Name")
        except Exception:                                    # noqa: BLE001
            pass
        name_of[o.path_id] = (o.type.name, nm)

    cue2clips = {}
    for o in env.objects:
        if o.type.name != "MonoBehaviour":
            continue
        d = o.read_typetree()
        if "clipList" not in d:
            continue
        nm = d.get("m_Name")
        if nm is None:
            continue
        cue2clips[nm] = [name_of.get(c.get("m_PathID"), (None, None))
                         for c in d["clipList"]]

    have = {}
    if os.path.isdir(UNPACK):
        for f in sorted(os.listdir(UNPACK)):
            stem, _ext = os.path.splitext(f)
            have.setdefault(stem.strip(), os.path.join(UNPACK, f))

    print("=== 六条 cue 的解析 ===")
    plan = {}
    bad = 0
    for army, when, cue in CUES:
        got = cue2clips.get(cue)
        if got is None:
            print(f"🔴 包里没有 cue `{cue}`"); bad += 1; continue
        clips = [n for (t, n) in got if t == "AudioClip" and n]
        if not clips:
            print(f"🔴 `{cue}` 的 clipList 没解出 AudioClip"); bad += 1; continue
        clip = clips[0]
        src = have.get(clip)
        flag = "✅" if src else "🔴 文件不在解包目录"
        if not src:
            bad += 1
        else:
            plan[(army, when)] = (clip, src)
        print(f"  {flag} {army:8s} {when:10s} cue `{cue}` → clip `{clip}`"
              + (f"  ({src})" if src else ""))

    print(f"\n=== 五条真 clip（去重后）===")
    uniq = {}
    for (army, when), (clip, src) in plan.items():
        uniq.setdefault(clip, src)
    for clip in sorted(uniq):
        print(f"  {clip}")

    if bad:
        print(f"\n🔴 有 {bad} 条没齐 —— 不写盘（宁可什么都不做，也别只拷一半）")
        return 1

    if a.check:
        print("\n（--check：只报告，没写盘）")
        return 0

    os.makedirs(DEST, exist_ok=True)
    n = 0
    for clip, src in sorted(uniq.items()):
        dst = os.path.join(DEST, clip + os.path.splitext(src)[1])
        if os.path.exists(dst) and os.path.getsize(dst) == os.path.getsize(src):
            print(f"  = 已有且同尺寸，跳过：{os.path.basename(dst)}")
            continue
        shutil.copy2(src, dst)
        print(f"  + {os.path.basename(dst)}  ({os.path.getsize(dst)} 字节)")
        n += 1
    print(f"\n✅ 拷了 {n} 个（跳过 {len(uniq) - n} 个已存在的）→ {DEST}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
