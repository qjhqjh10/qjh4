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
  · 谁在用   `d:/4/Unity/数据/游戏数据/animfx_modules.json` —— **三条路径**（`used_cues()`）：
      ① `sounds[*].sound`   ② `exitSounds[*].sound`   ③ **`collisionEvent` 的 `PlaySound` 订阅**
      （③ 是 🆕 2026-10-16 加的，见下面 `COLLISION_CALL_RE` 那段注释）
    这**三条都推不出来**的（工程按名字播、原版那格是组件上的序列化 `AudioCue`）⇒ 人手声明在
      `EXTRA_CUES` 里（🆕 2026-10-17 收 `CardStartDrag`，A910）；每次跑还会**反向查漏**一遍
      （`code_only_cue_report`：原版 cue 名 ∩ 工程 `.cs` 的音效代码行 − 已收），**只报告**。

用法：
    PYTHONIOENCODING=utf-8 python 工具/import_original_sfx.py --check   # 只报告，不写盘
    PYTHONIOENCODING=utf-8 python 工具/import_original_sfx.py           # 拷音频 + 写表
    （两种都会打印「查漏（反向）」那一节；常态应为 0 个候选）
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

import UnityPy

BUNDLE_DIR = "d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
UNPACK = "d:/2/新解包资源/assets_full"
# 兜底：cue 名字在这些包里找（先按名字找包，找不到才退回这几个）
BUNDLE_FALLBACK = ["soundcollection_assets_all", "battleprefabs_vfxandmisc_assets_all"]
MODULES = "d:/4/Unity/数据/游戏数据/animfx_modules.json"
DST_AUDIO = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio/sfx"
DST_JSON = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/animfx_sounds.json"

REF_PREFIX = "@asset:MonoBehaviour:"

# 🆕 **2026-10-15（A425④）**：**窗口级 cue** —— 它**不在** `animfx_modules.json` 的 `sounds[]` 里
#   （引它的是 `RewardWindow` 的 `soundOnAppear` 序列化字段，不是 AnimFX 模块）⇒ `used_cues()` 收不到。
#   出处 = `Shell/RewardWindow.cs:409-417`（cue `Reward open item by item` → `clipList` 唯一一条 clip
#   `Add card to deck`）；判据 → `资料/普查产出_1013/A表现核_块6.md` §A425 第 ④ 步。
#   ⚠️ **只把这一份并进 `used_cues()` 的结果**，⛔ 别去动 `used_cues()` 的解析口径。
#   ⚠️ 它在 `bundle_soundcollection_assets_all` 里（同 `soundcollection_assets_all` 包）。
#
# 🔴 **2026-10-17（B21·A910）—— 这一格的口径说清楚：它不是「窗口级」，是【工程按名字播的 cue】。**
#   前三口（`sounds[]` / `exitSounds[]` / `collisionEvent`）都从 `animfx_modules.json` **推**得出来；
#   而**我们自己代码**里按名字播的 cue **推不出来**（原版是组件上的序列化 `AudioCue` 字段，
#   我们这边落成 C# 常量）⇒ **只能一条条声明在这里**。
#   判据（本轮亲核）：把 `bundle_*` 里那 **612** 个 cue 包装名 ∩ 工程 `MyGame/Assets/**/*.cs`
#   （只在**音效上下文的代码行**上算命中）− `used_cues()` ⇒ 剩 8 个名字，逐条都能指到出处。
#   `CardStartDrag`（**A910**）：原版 `DraggableController.dragSound`（两个实例共用，
#     PathID `−4308815958917459268`，`bundle_soundcollection_assets_all/MonoBehaviour/CardStartDrag.json`）
#     → 我们 `Deck/DeckRuntime.cs` 的 `const string DragCue = "CardStartDrag"`。
#     判据 → `资料/普查产出_1017/W_G3_拖拽件.md` §①/§② + `Core/DraggableController.cs:130`。
#   ⚠️ 另 7 个名字**另有产出方，别并到这里来**（理由逐条在 `CODE_ONLY_KNOWN` 里）：
#     残骸六条 = `工具/import_remnant_sfx.py` · `OvertimeStart` = `工具/rebuild_overtime_start_ogg.py`
#     （⛔ 并进来会把那条**重制过的 ogg 覆盖**掉）· `Add card to deck` 是 clip 名不是 cue 名。
EXTRA_CUES = ["Reward open item by item", "CardStartDrag"]

# 🔴 **2026-10-16（A828）第三来源 —— `collisionEvent` 上的 `PlaySound`。**
#   `AnimFXModuleCollisions` 的 `collisionAndParticles[i].particleSystemsDefinition[j].collisionEvent`
#   是一棵 **UnityEvent**（原版在 prefab 的 Inspector 里连的订阅），`PlaySound` 那几条要播的 cue
#   存在 **`m_Arguments.m_ObjectArgument`**（`@asset:MonoBehaviour:<cue 名>`，类型名 `AudioCue, Assembly-CSharp`）。
#   ⚠️ 它**不在 `sounds[]` / `exitSounds[]` 里**（那两条是 `AnimFXController` 的字段，这是另一个模块）
#      ⇒ `used_cues()` 原来**一条都收不到**。
#   实测（本脚本亲扫 `animfx_modules.json`）：`collisionEvent` 共 **563** 条订阅，其中
#      `PlaySound 283` / `TriggerCameraShake 179` / `PlayAnim 92` / `AnimEventDoShake 5` / 空方法名 4；
#      **非空 `m_ObjectArgument` 恰好 283 条、全在 `PlaySound` 上、类型名全是 `AudioCue`**（无一例外）。
#      283 条里 **211 条（22 个 cue 名）不在旧表** ⇒ 它们在运行期走
#      `WFModuleCollisions.MissingSoundCues` **静默不响**（2026-10-16 之前）。
#   判据 = `资料/普查产出_1016/W2_A828碰撞屏震.md` §④ 第 3 条；
#   生产侧**同一套读法** = `MyGame/Assets/WarpforgeVFX/Runtime/WFModuleCollisions.cs` 的 `ReadCalls`
#      （它同样按 `m_ObjectArgument` 取名字 ⇒ 两边的「认不认得这个 cue」必须一致）。
COLLISION_CALL_RE = re.compile(
    r"^(?P<prefix>collisionAndParticles\[\d+\]\.particleSystemsDefinition\[\d+\]"
    r"\.collisionEvent\.m_PersistentCalls\.m_Calls\[\d+\]\.)(?P<field>[A-Za-z_][A-Za-z0-9_.]*)$")
PLAYSOUND_METHOD = "PlaySound"


def collision_play_cues(kw, effect_name):
    """→ [cue 名]：本模块 `collisionEvent` 上**所有 `PlaySound` 订阅**要播的 cue（一条一元素，不去重）。

    一条订阅 = 键名上的一组 `…m_Calls[i].<字段>`。**只认 `PlaySound`**：另三种方法
    （`TriggerCameraShake` / `AnimEventDoShake` / `PlayAnim`）里那条名字不是 cue
    —— 实测它们的 `m_ObjectArgument` **全是空**，所以这里不是「筛掉了一批」（见文件头那段注释）。"""
    seen, out = set(), []
    for k in kw:
        m = COLLISION_CALL_RE.match(k)
        if not m:
            continue
        pre = m.group("prefix")
        if pre in seen:
            continue
        seen.add(pre)
        if kw.get(pre + "m_MethodName") != PLAYSOUND_METHOD:
            continue
        v = kw.get(pre + "m_Arguments.m_ObjectArgument", "")
        if not v:
            continue
        if not v.startswith(REF_PREFIX):
            print(f"⚠️ 认不出的引用形状：{v}（{effect_name}）")
            continue
        out.append(v[len(REF_PREFIX):])
    return out


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
    """→ (cue 名集合, sounds 条目数, exitSounds 条目数, collisionEvent 条目数)。从未含 `sound` 的槽位不算。"""
    d = json.load(io.open(MODULES, encoding="utf-8"))
    names, n, nex, ncol = set(), 0, 0, 0
    for e in d["effects"]:
        for m in e["modules"]:
            kw = dict(zip(m["keys"], m["values"]))
            if m["kind"] == "AnimFXController":
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
            # 🆕 第三来源：`collisionEvent`（在 `AnimFXModuleCollisions` 上，**与上面不是同一批模块**）
            for nm in collision_play_cues(kw, e["name"]):
                names.add(nm)
                ncol += 1
    return names | set(EXTRA_CUES), n, nex, ncol


# ============================================================================================
#  🔴 **2026-10-17（B21·A828 尾巴③）查漏（反向）—— 「取材面还漏了哪条 cue」**
# ============================================================================================
#  为什么要有它：前三口 + `EXTRA_CUES` 都是「**我声明什么就收什么**」，声明漏了**没人知道**
#  —— `A910`（起拖音 `CardStartDrag` 首次起拖打一条解不出的告警）就是这么来的：
#  它在 `animfx_modules.json` 里**一次都不出现**（原版那格是组件上的序列化 `AudioCue`），
#  而我们代码里是个 C# 常量。**没有第二个人会去比这张表**。
#
#  口径（三句话）：
#    ① **起点是权威集合**：拿**原版 cue 包装名**（`load_bundles()` 读到的那些 `m_Name`）去比，
#       ⛔ 不是拿 `.cs` 里随便一个字面量 —— 所以**不会凭空发明**一条原版没有的 cue。
#    ② **降噪两层**：命中的行必须在**音效上下文**里（`SCAN_CTX_RE`），且**注释行不算**（`_is_comment`）。
#       实测：不加这两层剩 28 个名字（`Victory`/`Mulligan`/`Asteroid Zone`… 全是重名噪声），
#       加了剩 8 个，**每个都指得到出处**。
#    ③ **只报告、不自动收**：剩下那 8 个里仍有「同名 clip」这类误报（`Add card to deck`），
#       自动并进表会把表弄脏。已解释过的列在 `CODE_ONLY_KNOWN` 里（**带理由**）⇒ 常态输出安静，
#       一旦出现**新**名字就 ⚠️ 打出来。
SCAN_ROOT = "d:/4/Unity/MyGame/Assets"
SCAN_CTX_RE = re.compile(r"(?i)sound|audio|cue|clip|\.Play\(")
CODE_ONLY_KNOWN = {
    "OvertimeStart": "**clip** 名（`BattleDriver` 直取 `WFSoundBank.Clip`）· 产出方 = "
                     "`工具/rebuild_overtime_start_ogg.py` —— ⛔ **别并进本表**：本表会把原版那条 "
                     "`.ogg/.wav` 原样拷过去，**覆盖掉那条重制过的**",
    "Add card to deck": "**clip** 名（cue `Reward open item by item` 的 clip，已在表里）· "
                        "与某个 cue 包装**同名** ⇒ 是误报",
    "Aeldari To Waystone": "残骸 cue（预制体上的序列化 `AudioCue` 字段，不在 `animfx_modules.json` 里）"
                           "· 产出方 = `工具/import_remnant_sfx.py`",
    "Aeldari Waystone Destruction": "同上（残骸线）",
    "CardShatter": "同上（残骸线）",
    "NecronsCardReanimate": "同上（残骸线）",
    "RemnantsDestroyed": "同上（残骸线）",
}


def _is_comment(line):
    s = line.lstrip()
    return s.startswith("//") or s.startswith("*") or s.startswith("/*")


def code_only_cue_report(all_cue_names, want):
    """→ 新候选 `[(name, [file:line, …]), …]`；**顺手把结果打印出来**（常态 = 0 个）。

    ⚠️ 调用点在 `load_bundles()` **之后**（`all_cue_names` 就是它读到的 cue 名，见 `main()`）。"""
    if not os.path.isdir(SCAN_ROOT):
        # 不许静默：扫不动就等于**这一次根本没查漏**，而输出看起来和「查过、没漏」一模一样。
        print(f"⚠️ 查漏（反向）：`{SCAN_ROOT}` 不在 ⇒ 这一趟**没有查漏**（不是「查了没漏」）")
        return []
    todo = sorted(set(all_cue_names) - set(want) - set(CODE_ONLY_KNOWN))
    if not todo:
        print("查漏（反向）：原版 cue 名里没有「取材面没收、代码又在提」的新名字 ✅")
        return []
    # 一条正则匹所有候选名（长的排前面，免得 `Aeldari To Waystone` 被 `Aeldari To Waystone Death` 截断）
    big = re.compile("|".join(re.escape(c) for c in sorted(todo, key=len, reverse=True)))
    hits, n_lines = {}, 0
    for root, _dirs, files in os.walk(SCAN_ROOT):
        for f in files:
            if not f.endswith(".cs"):
                continue
            p = os.path.join(root, f)
            rel = os.path.relpath(p, "d:/4/Unity").replace("\\", "/")
            for i, line in enumerate(io.open(p, encoding="utf-8", errors="replace"), 1):
                n_lines += 1
                if not SCAN_CTX_RE.search(line) or _is_comment(line):
                    continue
                for m in big.finditer(line):
                    hits.setdefault(m.group(0), []).append(f"{rel}:{i}")
    out = sorted(hits.items(), key=lambda kv: -len(kv[1]))
    print(f"查漏（反向）：扫 {SCAN_ROOT} 的 .cs 共 {n_lines} 行（只算音效上下文的**代码行**）⇒ "
          f"**{len(out)} 个候选**")
    for name, where in out:
        print(f"  ⚠️ `{name}`（{len(where)} 处，例如 {where[0]}）—— 表里没有它 ⇒ "
              f"要么补进 `EXTRA_CUES`（连判据一起），要么写进 `CODE_ONLY_KNOWN`（连理由一起）")
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只报告，不写盘")
    args = ap.parse_args()

    want, n_snd, n_exit, n_col = used_cues()
    print(f"特效在用：`sounds` 有值的 {n_snd} 条 + `exitSounds` {n_exit} 条 + "
          f"`collisionEvent` 的 `PlaySound` {n_col} 条 ⇒ **{n_snd + n_exit + n_col} 条**，"
          f"涉及 {len(want)} 个不同 cue (其中 `EXTRA_CUES` 硬编 {len(EXTRA_CUES)} 个)")

    need_bundles = bundles_for(want)
    print(f"cue 分散在 {len(need_bundles)} 个包里，逐个读：")
    cues, files = load_bundles(need_bundles)
    print(f"合起来：cue 包装 {len(cues)} 个 · 能对上的音频文件 {len(files)} 个")

    # ---- 🆕 查漏（反向）：取材面漏没收的 cue（只报告，见 `code_only_cue_report`）----
    code_only_cue_report(cues.keys(), want)

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
        "note": "AnimFX 的随机化 cue 表（来源三条路径：`sounds[*].sound` · `exitSounds[*].sound` · "
                "`collisionEvent` 的 `PlaySound` 订阅；另加 `EXTRA_CUES` —— 工程按名字播、"
                "AnimFX 数据里推不出来的 cue）。clips 里随机挑一条；"
                "播放音量 = minVolume..maxVolume、音高 = minPitch..maxPitch；"
                "文件名在 Resources/Art/audio/sfx/<clip 名>。",
        "source": {
            "bundle": "soundcollection_assets_all.bundle",
            "who": "数据/游戏数据/animfx_modules.json 的 sounds[*].sound / exitSounds[*].sound /"
                   " collisionEvent 的 PlaySound（m_Arguments.m_ObjectArgument）"
                   " + 脚本里的 EXTRA_CUES（按名字播的那几条）",
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
