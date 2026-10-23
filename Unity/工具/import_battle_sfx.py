#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""import_battle_sfx.py — 把**原版战斗的「时机 → cue」表**（`SoundAssetCollection`）导进 Unity 工程。

为什么单开一个脚本（不并进 `import_original_sfx.py`）：
  那个脚本的取材面是 `animfx_modules.json` 里 `sounds[*].sound` / `exitSounds[*].sound` /
  `collisionEvent` 的 `PlaySound` —— **只覆盖 AnimFX**。
  战斗那 28 条挂在**另一个 SO** 上：`BattleManager.soundCollection`（`+0x80`）→ `SoundAssetCollection`
  （`d:/2/tools/decomp_full/BattleManager__Initialize.c` 那一路；字段名来自 `dump.cs`）。
  实测（2026-10-15）：两族的 **cue 名零交集**（`animfx_sounds.json` 里那 16 个可解的名字一个都没有）。

判据（全部亲读，不是按名字猜）：

  1. **字段 → 资产**：`SoundAssetCollection` 的每个字段是一个 Addressables `AssetReference`
     （`m_AssetGUID`）。解析走 `工具/_guid_resolve_probe.py`（catalog_main.json → 束内容器键，
     **不是按名字猜**）。实测 28 个字段里 **16 个解得出**、**6 个是空引用**、**6 个是悬挂 GUID**。
  2. **资产 → clip**：cue 的 `clipList` 存 PathID，**必须读束**才拿得到名字（解包目录里没有映射）。
     ⚠️ **cue 名 ≠ clip 名**（`startTurn` → cue `Start Trun` → clip `StartTurn`；
     `minionInitiatingAttack` → clip `SelectUnit`）⇒ 表里两列都留。
  3. **MixerType**：反编译调用点第 4 实参**恒为 0**（`SoundManager__Play2D(&buf, inst, cue, 0, 0)`）
     —— 逐条核过 `EndTurnClick:50,171`（End Turn / End Turn Inactive）· `_NextTurn_d__395:450`（Start Turn）·
     `_MulliganCountdown_d__347:33`（Mulligan）· `EndCountdown:34`（TimerExpired）·
     `SetNoActionsUi:53`（No Actions）· `DrawCard:50`（Draw Card）· `_Heal_d__468:148,154`（UnitHeal）·
     `DraggableController` 那处也一样 ⇒ **全部走 `MixerType.FX`（=0）**，⛔ 别照 `EndBattleDoors` 的 Jingles 套。
     （`EndBattleDoors` 的胜/败/平三支是**唯一**走 `Jingles` 的，那三支另有归属，不在本表。）

产物两块：

  ① **音频**：`MyGame/Assets/CardPresentation/Resources/Art/audio/sfx/<clip 名>.ogg|wav`
     （`Resources/Art/` 整个被 `.gitignore` 排除 —— 与美术/语音同一条规矩）
     🆕 **2026-10-24（G4）**：拷完**顺手补 `<clip 名>.<ext>.meta`** —— 共用实现 =
     `工具/_sfx_meta.py`（模板 = 同目录同扩展名的众数，只换 guid；**已存在的 meta 一个字节都不碰**）。
     没有这步的话新补进来的音频会被 Unity 用内置默认导入（`loadType: 0`），与同目录既有 510 条不一致。
  ② **表**：`MyGame/Assets/CardPresentation/Resources/battle_sounds.json`
     —— 进仓库；形状与 `animfx_sounds.json` 逐字段相同（`WFSoundBank` 用同一个解析器读它），
     但 **`name` 那一列是「字段名」（如 `startTurn`），不是 cue 资产名** —— 见下面「键用哪个」。
     表尾另有 `gaps[]`：**原版这一格里就没有声音的那些**（⛔ 不许静默地少几条）。

🆕 **2026-10-16（B 组 = 卡 prefab 那一块）** —— 同一张表、同一套 cue 解析，多收一族的**字段**：
  · 挂在 `CardScript.battleCardSounds`（9 条）+ `CardScript` 自己那 3 条 `changeTo*CUE`。
  · 🔴 **它不是「逐卡序列化」**（`T06` 原来那句是错的，已就地订正）：全 `assets_full` 里带
    `invokeCUELegendary` 的 MonoBehaviour **只有 1 份** =
    `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_1744609728290659264.json`；
    且 `meleeAttackHitSmall/Medium/Big` **三条指向同一个 pathID** ⇒ 原版这三档本来就是同一条音。
  · 🔴 **这一族存的是普通 PPtr（`m_FileID` + `m_PathID`），没有 `m_AssetGUID`**
    ⇒ ⛔ 走不了 A 组那条 catalog 链，只能按**束的 externals** 解（`m_FileID = 9` →
    `CAB-62d1945b7005c115fb946f0d264a205b`，其 pathID 在 `soundcollection_assets_all` 束里解成 cue）。
  · 表里靠 `group` 列区分两组（`collection` / `card`）；`WFSoundBank` 只认 `name`/`clips`/音量/音高 ⇒ **多一列无副作用**。

🔴 **键用哪个（本表与 AnimFX 表唯一的形状差异）**：
   C# 那边要能一句 `soundCollection.startTurn` 那样**按原版字段名**索引 ⇒ 本表 `name` = **字段名**。
   另留一列 `srcCue` = **cue 资产名**（原版那个 SO 里真正指到的资产，供人核对；
   ⚠️ 原版自己的拼写错误也照抄：`startTurn` 指的资产叫 **`Start Trun`**）。
   ⚠️ 字段名与 `animfx_sounds.json` 的 cue 名**理论上可能撞车** ⇒ `WFSoundBank` 装载时**要报**（不许静默覆盖）。

用法：
    PYTHONIOENCODING=utf-8 python 工具/import_battle_sfx.py --check   # 只报告，不写盘
    PYTHONIOENCODING=utf-8 python 工具/import_battle_sfx.py           # 拷音频 + 写表
"""
import argparse
import importlib.util
import io
import json
import os
import shutil
import sys

sys.stdout.reconfigure(encoding="utf-8")

# 🔴 **2026-10-24（G4）：补 `.meta` 的共用实现**（`工具/_sfx_meta.py`，三个导入脚本共用一份）。
#   `python -I` 不会把脚本目录放进 `sys.path` ⇒ 这里显式插一下。
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import _sfx_meta                                  # noqa: E402

import UnityPy

BUNDLE_DIR = "d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
SAC_BUNDLE = os.path.join(BUNDLE_DIR, "duplicateassetisolationso_assets_all.bundle")
CUE_BUNDLE = os.path.join(BUNDLE_DIR, "soundcollection_assets_all.bundle")
# 🆕 2026-10-16（B 组）：卡 prefab 那一族 cue 挂在这束的 `CardScript` 上（**唯一一份**，见文件头「B 组」）。
PREFAB_BUNDLE = os.path.join(BUNDLE_DIR, "battleprefabs_vfxandmisc_assets_all.bundle")
UNPACK = "d:/2/新解包资源/assets_full"
SAC_JSON = os.path.join(UNPACK,
                        "bundle_duplicateassetisolationso_assets_all/MonoBehaviour/SoundAssetCollection.json")
CLIP_SRC = os.path.join(UNPACK, "bundle_soundcollection_assets_all/AudioClip")
PROBE = "d:/4/Unity/工具/_guid_resolve_probe.py"
DST_AUDIO = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio/sfx"
DST_JSON = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/battle_sounds.json"

# ---- B 组：卡 prefab（`CardScript`）上的 cue 字段。⛔ 字段名逐字照抄原版（含原版自己的拼写）----
CARD_CUE_FIELDS = ["invokeCUE", "invokeCUELegendary", "minionDeathCUE",
                   "meleeAttackHitSmall", "meleeAttackHitMedium", "meleeAttackHitBig",
                   "backToHand", "landOnGround", "createCardInHand"]
CARD_TOP_FIELDS = ["changeToMeleeAttackCUE", "changeToRangeAttackCUE", "changeToActiveAbilityCUE"]


def load_probe():
    """复用 `_guid_resolve_probe.py` 的 GUID 解析（**判据只留一处** —— ⛔ 别再抄一份 catalog 解析）。"""
    spec = importlib.util.spec_from_file_location("wf_guid_probe", PROBE)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def read_sac():
    """→ [(字段名, guid)]。**读 shipped bundle**（权威），并与解包 JSON 对一遍（不一致就报）。"""
    env = UnityPy.load(SAC_BUNDLE)
    from_bundle = None
    for obj in env.objects:
        d = obj.read_typetree()
        if d.get("m_Name") == "SoundAssetCollection":
            from_bundle = [(k, v.get("m_AssetGUID") or "")
                           for k, v in d.items() if isinstance(v, dict) and "m_AssetGUID" in v]
            break
    if from_bundle is None:
        raise SystemExit(f"❌ {SAC_BUNDLE} 里没有 SoundAssetCollection")

    from_unpack = None
    if os.path.exists(SAC_JSON):
        d = json.load(io.open(SAC_JSON, encoding="utf-8"))
        from_unpack = [(k, v.get("m_AssetGUID") or "")
                       for k, v in d.items() if isinstance(v, dict) and "m_AssetGUID" in v]
        if dict(from_unpack) != dict(from_bundle):
            print("⚠️ 解包 JSON 与 shipped bundle **不一致**（以 bundle 为准）：")
            for k in dict(from_bundle):
                a, b = dict(from_unpack).get(k), dict(from_bundle).get(k)
                if a != b:
                    print(f"    {k}: 解包={a!r} bundle={b!r}")
        else:
            print("✅ 解包 JSON 与 shipped bundle 逐字段一致（28 个字段）")
    return from_bundle


def read_cues(path):
    """→ (cues, id_to_name)：
      · `cues` = {cue 资产名: {clips, minPitch, maxPitch, minVolume, maxVolume, timeToPlayAgain}}
      · `id_to_name` = {path_id: cue 资产名}（**只收 cue 包装**，即带 `clipList` 的那些）
        —— B 组那边是 **PPtr（fileID/pathID）**，要靠这张表把 pathID 换回名字。
    """
    env = UnityPy.load(path)
    name_of = {}
    for obj in env.objects:
        try:
            nm = obj.read_typetree().get("m_Name")
        except Exception:                                       # noqa: BLE001
            nm = None
        name_of[obj.path_id] = (obj.type.name, nm)

    cues, id_to_name = {}, {}
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        d = obj.read_typetree()
        if "clipList" not in d:
            continue
        nm = d.get("m_Name")
        id_to_name[obj.path_id] = nm
        cues.setdefault(nm, {
            "clips": [name_of.get(c.get("m_PathID"), (None, None))[1] for c in d["clipList"]],
            "minPitch": d.get("minPitch", 1.0),
            "maxPitch": d.get("maxPitch", 1.0),
            "minVolume": d.get("minVolume", 1.0),
            "maxVolume": d.get("maxVolume", 1.0),
            "timeToPlayAgain": d.get("timeToPlayAgain", 0.0),
        })
    return cues, id_to_name


def _ext_name(sf, file_id):
    """PPtr 的 `m_FileID`（1 基）→ 所在束的 CAB 名。**`FileIdentifier` 是对象不是 dict** ⇒ 两种形状都认。"""
    if not file_id or file_id < 1 or file_id > len(sf.externals):
        return ""
    e = sf.externals[file_id - 1]
    if isinstance(e, dict):
        return e.get("name") or ""
    return getattr(e, "name", "") or ""


def read_card_cues():
    """B 组：**卡 prefab 上 `CardScript` 的那一块 cue**。→ [(字段名, pathID, 所在 CAB 名)]

    判据（2026-10-16 实核）：
      · 全 `assets_full` 里带 `invokeCUELegendary` 的 MonoBehaviour **只有 1 份** ——
        `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_1744609728290659264.json`
        ⇒ 🔴 **这一族【不是逐卡序列化】**（`CardScript` 只有一个字段，
        `mine` 出来的小/中/大三档还指向**同一个 pathID**）。
      · 这块存的是**普通 PPtr**（`m_FileID` + `m_PathID`），**没有 `m_AssetGUID`**
        ⇒ ⛔ **走不了** A 组那条 `_guid_resolve_probe` 的链，只能按**束的 externals** 解。
      · 实测 11 条 PPtr（第 12 条 `changeToActiveAbilityCUE` 是空引用）**全部** `m_FileID = 9`
        → `CAB-62d1945b7005c115fb946f0d264a205b`，其 pathID 在 `soundcollection_assets_all.bundle`
        里逐条解成「语义对得上字段名的 cue」（`invokeCUE` → `Invoke CUE`、`minionDeathCUE` → `Minion Death CUE` …）。
    """
    env = UnityPy.load(PREFAB_BUNDLE)
    hit = None
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        try:
            d = obj.read_typetree()
        except Exception:                                       # noqa: BLE001
            continue
        if "battleCardSounds" in d:
            hit = (obj, d)
            break
    if hit is None:
        raise SystemExit(f"❌ {PREFAB_BUNDLE} 里没有带 `battleCardSounds` 的 MonoBehaviour")
    obj, d = hit
    sf = obj.assets_file
    print(f"B 组：卡 prefab cue 块在 path_id={obj.path_id}（束 {sf.name}）")

    out = []
    block = d.get("battleCardSounds") or {}
    for k in CARD_CUE_FIELDS:
        v = block.get(k) or {}
        out.append((k, v.get("m_PathID") or 0, _ext_name(sf, v.get("m_FileID") or 0)))
    for k in CARD_TOP_FIELDS:
        v = d.get(k) or {}
        out.append((k, v.get("m_PathID") or 0, _ext_name(sf, v.get("m_FileID") or 0)))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只报告，不写盘")
    args = ap.parse_args()

    gp = load_probe()
    cat = gp.load_catalog()

    fields = read_sac()
    print(f"`SoundAssetCollection` 字段 = {len(fields)} 个")

    cues, cue_ids = read_cues(CUE_BUNDLE)
    print(f"`soundcollection_assets_all` 束内 cue 包装 = {len(cues)} 个")

    files = {}
    if os.path.isdir(CLIP_SRC):
        for f in sorted(os.listdir(CLIP_SRC)):
            stem, _ = os.path.splitext(f)
            files.setdefault(stem.strip(), os.path.join(CLIP_SRC, f))
    print(f"可用的音频文件（解包目录）= {len(files)} 个")

    table, gaps = [], []
    for field, guid in fields:
        if not guid:
            gaps.append({"field": field, "why": "原版这一格的 AssetReference 是空的（亲读 shipped bundle）"})
            continue
        hits = gp.lookup(cat, guid)
        names = sorted({h.get("name") for h in hits if h.get("name")})
        if not names:
            gaps.append({"field": field, "guid": guid,
                         "why": "GUID 在本地任何 catalog / 任何束的容器键里都解析不出来"
                                "（catalog_main.json 的 key_index=None；catalog.bin 裸搜 0；"
                                "aa 全目录 1.6 GB 裸搜 0）⇒ 判为**原版这一格是悬挂引用**，本 build 也不响"})
            continue
        if len(names) > 1:
            print(f"⚠️ {field}: 一个 GUID 解出多个名字 {names} —— 取第一个（其余请人来核）")
        cue = names[0]
        c = cues.get(cue)
        if c is None:
            gaps.append({"field": field, "guid": guid, "srcCue": cue,
                         "why": f"解出的 cue 资产 `{cue}` 不在 soundcollection 束的 cue 包装里"})
            continue
        clips = sorted({(x or "").strip() for x in c["clips"] if (x or "").strip()})
        got = [cl for cl in clips if cl in files]
        if not got:
            gaps.append({"field": field, "guid": guid, "srcCue": cue, "clips": clips,
                         "why": "cue 在、但它的 clip 在解包目录里一个都对不上"})
            continue
        table.append({
            "name": field, "group": "collection", "clips": got, "srcCue": cue,
            "minPitch": round(c["minPitch"], 4), "maxPitch": round(c["maxPitch"], 4),
            "minVolume": round(c["minVolume"], 4), "maxVolume": round(c["maxVolume"], 4),
            "timeToPlayAgain": round(c["timeToPlayAgain"], 4),
        })

    # ---- B 组（卡 prefab `CardScript` 那一块）：PPtr → cue 名（⛔ 不走 GUID 那条链）----
    print()
    for field, pid, cab in read_card_cues():
        if not pid:
            gaps.append({"field": field, "group": "card",
                         "why": "原版这一格的 PPtr 是空的（`m_PathID = 0`）⇒ 原版这格本来就没声音"})
            continue
        cue = cue_ids.get(pid)
        if cue is None:
            gaps.append({"field": field, "group": "card", "pathID": pid, "cab": cab,
                         "why": f"pathID={pid}（束 {cab}）在 `soundcollection_assets_all` 的 cue 包装里找不到"})
            continue
        c = cues.get(cue)
        clips = sorted({(x or "").strip() for x in c["clips"] if (x or "").strip()})
        got = [cl for cl in clips if cl in files]
        if not got:
            gaps.append({"field": field, "group": "card", "pathID": pid, "cab": cab, "srcCue": cue,
                         "clips": clips, "why": "cue 在、但它的 clip 在解包目录里一个都对不上"})
            continue
        table.append({
            "name": field, "group": "card", "clips": got, "srcCue": cue,
            "minPitch": round(c["minPitch"], 4), "maxPitch": round(c["maxPitch"], 4),
            "minVolume": round(c["minVolume"], 4), "maxVolume": round(c["maxVolume"], 4),
            "timeToPlayAgain": round(c["timeToPlayAgain"], 4),
        })

    n_coll = sum(1 for t in table if t["group"] == "collection")
    n_card = sum(1 for t in table if t["group"] == "card")
    print(f"\n接得上 = {len(table)} 条（A 组 collection {n_coll}/{len(fields)} · "
          f"B 组 card {n_card}/{len(CARD_CUE_FIELDS) + len(CARD_TOP_FIELDS)}）；"
          f"原版这格里就没有声音的 = {len(gaps)}")
    for g in gaps:
        print(f"  ⊘ {g['field']:<24} {g['why'][:60]}")

    need_files = sorted({cl for t in table for cl in t["clips"]})
    have = set(os.path.splitext(f)[0].strip() for f in os.listdir(DST_AUDIO)) if os.path.isdir(DST_AUDIO) else set()
    print(f"\n要用到 {len(need_files)} 个音频文件；其中本地已有 {sum(1 for c in need_files if c in have)} 个"
          f" ⇒ 要拷 {sum(1 for c in need_files if c not in have)} 个")

    copied = skipped = n_meta = 0
    if not args.check:
        os.makedirs(DST_AUDIO, exist_ok=True)
        guids = _sfx_meta.known_guids()      # 2026-10-24（G4）：全工程扫一次，别每条都扫
        for cl in need_files:
            src = files[cl]
            ext = os.path.splitext(src)[1].lower()
            dst = os.path.join(DST_AUDIO, cl + ext)
            if os.path.exists(dst) and os.path.getsize(dst) == os.path.getsize(src):
                skipped += 1
            else:
                shutil.copy2(src, dst)
                copied += 1
            # 🔴 已存在的 meta 一个字节都不碰 ⇒ 重跑幂等；上次漏了 meta 的这次补上。
            if _sfx_meta.ensure_meta(dst, guids=guids) == "created":
                n_meta += 1

    out = {
        "version": 1,
        "note": "原版**战斗**的「时机 → cue」表。**两组**：① `group=collection` = "
                "`BattleManager.soundCollection`(`+0x80`) → `SoundAssetCollection` 的 28 个字段；"
                "② `group=card` = **卡 prefab 上 `CardScript`** 那一块（9 条 + `changeTo*` 2 条；"
                "🔴 **不是逐卡序列化** —— 全资产里只有一份，小/中/大三档还指向同一条音）。"
                "⚠️ `name` 那一列**两组都是原版字段名**（C# 按字段名索引，如 `startTurn` / `invokeCUE`），"
                "`srcCue` 才是 cue 资产名。clips 里随机挑一条；"
                "音量 = minVolume..maxVolume、音高 = minPitch..maxPitch；"
                "文件名在 Resources/Art/audio/sfx/<clip 名>。"
                "全部走 MixerType.FX（反编译调用点第 4 实参恒为 0）。"
                "`gaps[]` = 原版这一格里就没有声音的那些字段（空引用 / 悬挂 GUID / 空 PPtr），⛔ 不是我们没做。",
        "source": {
            "so": "SoundAssetCollection（bundle_duplicateassetisolationso_assets_all，"
                  "挂在 BattleManager.soundCollection +0x80）",
            "card_block": "CardScript.battleCardSounds（bundle_battleprefabs_vfxandmisc_assets_all，"
                          "**全资产唯一一份**；字段是普通 PPtr ⇒ 按束 externals 解到 soundcollection 的 cue）",
            "cue_bundle": "soundcollection_assets_all.bundle",
            "guid_resolver": "工具/_guid_resolve_probe.py（catalog_main.json；**只 A 组用**）",
            "script": "工具/import_battle_sfx.py",
        },
        "mixer": "FX",
        "cues": table,
        "gaps": gaps,
    }
    if not args.check:
        io.open(DST_JSON, "w", encoding="utf-8", newline="\n").write(
            json.dumps(out, ensure_ascii=False, indent=1))
        print(f"已拷 {copied} 条 · 已存在跳过 {skipped} 条 · 补 meta {n_meta} 条 → {DST_AUDIO}")
        print(f"表已写：{DST_JSON}")
    else:
        print("（--check：没有写盘）")


if __name__ == "__main__":
    main()
