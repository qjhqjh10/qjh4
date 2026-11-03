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
     `DraggableController` 那处也一样 ⇒ **A/B 两组全部走 `MixerType.FX`（=0）**，⛔ 别照 `EndBattleDoors` 的 Jingles 套。
     （🔴 **2026-11-03 订正**：原来这里写「`EndBattleDoors` 的胜/败/平三支是**唯一**走 `Jingles` 的、不在本表」——
     那三支**已经进本表了**（`group=scene`，D2 补），而且 `MixerType.Jingles` 的还不止它们：
     `OvertimeUi.enteringOvertimeSound` 与活动 SO 的 `OpenSound` 也走 Jingles ⇒ **C 组一律 Jingles**。）

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

🆕 **2026-11-03（`B121-AUD-03`）C 组 = 手写组（`group=scene` / `group=event`）**：
  这一组的引用者**不在** `SoundAssetCollection` 里（场景/预制体的序列化字段、活动 SO 的 `EventAssets[key=8]`）
  ⇒ 没有自动取材口。**只有「谁指向哪条 cue」写死在 `HAND_WRITTEN`**；clips / 音量 / 音高 / ttp **仍然
  当场从 shipped cue 束重解**。⇒ 重跑本脚本**不再丢**那 7 条（`B121-D2` 那 6 条 + `B121-AUD-05` 补的 `Draw`）。
  ⚠️ 这一组的 clip **不从解包目录拷**（扩展名可能不同，会出现同名两扩展名）⇒ `--check` 只按 **stem** 验在位。

用法（🔴 写盘脚本必须有显式动作档，见 CLAUDE.md §三 2026-10-26 那条）：
    PYTHONIOENCODING=utf-8 python 工具/import_battle_sfx.py --check   # 只报告 + 与产物比对，不写盘（rc 0/1）
    PYTHONIOENCODING=utf-8 python 工具/import_battle_sfx.py --dry     # 只算 + 打印读数，零写盘
    PYTHONIOENCODING=utf-8 python 工具/import_battle_sfx.py --write   # **唯一会写盘的档**：拷音频 + 补 meta + 写表
    ⛔ 一个动作档都不给（或不认识）= 只打印用法并退出 2，不计算、不写盘。
"""
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


USAGE = """用法：导入原版战斗「时机 → cue」表（默认什么都不做）
  PYTHONIOENCODING=utf-8 python 工具/import_battle_sfx.py --check   只报告 + 与产物比对，**零写盘**（一致 rc=0 / 缺失或过期 rc=1）
  PYTHONIOENCODING=utf-8 python 工具/import_battle_sfx.py --dry     只算 + 打印读数，**零写盘**
  PYTHONIOENCODING=utf-8 python 工具/import_battle_sfx.py --write   **唯一会写盘的档**：拷音频 + 补 meta + 写表
  -h, --help  只打印本用法并退出（退出码 0）
⛔ 一个动作档都不给（或不认识）= 只打印本用法并退出 2，不计算、不写盘。"""

ACTIONS = ("--check", "--dry", "--write")


def main():
    argv = sys.argv[1:]
    if any(a in ("-h", "--help") for a in argv):
        print(USAGE)
        return 0
    picked = [a for a in argv if a in ACTIONS]
    if len(picked) != 1 or len(picked) != len(argv):
        print(USAGE)
        return 2
    mode = picked[0]

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

    have = set(os.path.splitext(f)[0].strip() for f in os.listdir(DST_AUDIO)) if os.path.isdir(DST_AUDIO) else set()

    # ---- C 组（**手写组**）：`group=scene` / `group=event` --------------------------------------
    # 🔴 **为什么这一组只能手写**：它的引用者**不在** `SoundAssetCollection` 的 28 个字段里 ——
    #   ③ `scene` = **场景 / 预制体的序列化字段**：13 场 arena 的 `EndBattleDoors`
    #      （`victoryCue` +0xB0 · `defeatAudioCue` +0xB8 · `drawAudioCue` +0xC0；`m_Script` 82936325632428873，
    #       例 `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_5238.json`）
    #      ＋ 同 13 场的 `OvertimeUi.enteringOvertimeSound`
    #      ＋ `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_9031176634359415475.json`
    #        的 `sounds[0].sound`（`m_Script` 8184164905518033017 → 类 `ShakeAndSoundObject`，环境条件 Saim Hann Blackout）；
    #   ④ `event` = **活动 SO 的 `EventElements.EventAssets[key=8]`**（`EventAssetReferenceType.OpenSound`）：
    #      `bundle_cosmeticsso_assets_all` 的 `{Expansion Orks1,Release Space Wolves} Parent Event`，
    #      消费点 = `BaseEventPopup.Open`（`AudioCue.Play2D(MixerType.Jingles)`，cpp2il `BaseEventPopup.cs:56-60`）。
    # 🔴 **只有「谁指向哪条 cue」是手写的**；clips / 音量 / 音高 / timeToPlayAgain 一律**当场从 shipped cue 束
    #   重解**（`read_cues()` 的 `cues[名字]`）⇒ 原版改值或我们换束时，这一组不会与真值脱节。
    # ⚠️ 这两组的 `name` 是 **cue 资产名**（不是字段名）—— 与 ①② 的唯一形状差异（`srcCue` 同值）。
    # 判据全文 → `资料/完全复刻/报告/B121_D2_音效cue覆盖.md` §三·A / §四（持有者逐条 + 搜过的三种目录）。
    # 🆕 `Draw` = `B121-AUD-05`（2026-11-03）补解：`B121-D2` 那份漏了**同一颗 MB 里**的 `drawAudioCue`
    #   （pid `-2870999988157826586` → cue `Draw` → clip `MatchDraw`，参数与胜/败同：vol 0.7 · ttp 0.1）。
    HAND_WRITTEN = [
        # (表里的 name = **cue 资产名**, group, 判据出处)
        ("Victory", "scene",
         "`EndBattleDoors.victoryCue`(+0xB0) × 13 场 arena；消费点 decomp_full/EndBattleDoors__SetupDoor.c:113 "
         "= SoundManager.Play2D(cue, Jingles)"),
        ("Defeat", "scene",
         "`EndBattleDoors.defeatAudioCue`(+0xB8) × 13 场 arena；同一消费点（同上）"),
        ("Draw", "scene",
         "`EndBattleDoors.drawAudioCue`(+0xC0) × 13 场 arena；同一消费点（同上）"),
        ("OvertimeStart", "scene",
         "`OvertimeUi.enteringOvertimeSound` × 13 场 arena（例 battlearena1/MonoBehaviour/MonoBehaviour_4883.json）"),
        ("Blackout", "scene",
         "`ShakeAndSoundObject.sounds[0].sound`（bundle_battleprefabs_vfxandmisc_assets_all/"
         "MonoBehaviour_9031176634359415475.json，环境条件 Saim Hann Blackout）"),
        ("Ork Expansion 1 jingle", "event",
         "`Expansion Orks1 Parent Event` 的 `EventAssets[key=8 OpenSound]`（容器键 11c215f6…）；"
         "消费点 = BaseEventPopup.Open → AudioCue.Play2D(Jingles)"),
        ("Space Wolves Relase Jingle", "event",
         "`Release Space Wolves Parent Event` 的 `EventAssets[key=8 OpenSound]`（容器键 baa1a292…）；同一消费点"),
    ]
    # ⚠️ **这一组的 clip 不参与「从解包目录拷音频」**：它们早就导进 `Resources/Art/audio/sfx/` 了，而扩展名
    #   可能不同（`OvertimeStart` 解包目录是 `.vorbis`、我们的 Resources 里是 `.wav`）—— 照 A/B 组那套拷进来
    #   会出现**同名两扩展名**，`Resources.Load` 挑哪一个不可控 ⇒ 这里只**按 stem 查 Resources 有没有**。
    hand_clips = set()
    for name, group, why in HAND_WRITTEN:
        c = cues.get(name)
        if c is None:
            gaps.append({"field": name, "group": group,
                         "why": f"手写组钉的 cue 资产 `{name}` 不在 soundcollection 束里（换束 / 名字写错）—— {why}"})
            continue
        clips = sorted({(x or "").strip() for x in c["clips"] if (x or "").strip()})
        hand_clips.update(clips)
        got = [cl for cl in clips if cl in files or cl in have]
        if not got:
            gaps.append({"field": name, "group": group, "clips": clips,
                         "why": f"cue 在、但它的 clip 在解包目录和 Resources 里都对不上 —— {why}"})
            continue
        table.append({
            "name": name, "group": group, "clips": got, "srcCue": name,
            "minPitch": round(c["minPitch"], 4), "maxPitch": round(c["maxPitch"], 4),
            "minVolume": round(c["minVolume"], 4), "maxVolume": round(c["maxVolume"], 4),
            "timeToPlayAgain": round(c["timeToPlayAgain"], 4),
        })

    n_coll = sum(1 for t in table if t["group"] == "collection")
    n_card = sum(1 for t in table if t["group"] == "card")
    n_scene = sum(1 for t in table if t["group"] == "scene")
    n_event = sum(1 for t in table if t["group"] == "event")
    print(f"\n接得上 = {len(table)} 条（A 组 collection {n_coll}/{len(fields)} · "
          f"B 组 card {n_card}/{len(CARD_CUE_FIELDS) + len(CARD_TOP_FIELDS)} · "
          f"C 组 scene {n_scene} / event {n_event}（手写 + 参数重解））；"
          f"原版这格里就没有声音的 = {len(gaps)}")
    for g in gaps:
        print(f"  ⊘ {g['field']:<24} {g['why'][:60]}")

    need_files = sorted({cl for t in table for cl in t["clips"]} - hand_clips)
    print(f"\n要用到 {len(need_files)} 个音频文件；其中本地已有 {sum(1 for c in need_files if c in have)} 个"
          f" ⇒ 要拷 {sum(1 for c in need_files if c not in have)} 个")

    copied = skipped = n_meta = 0
    if mode == "--write":
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
        "note": "原版**战斗**的「时机 → cue」表。**四组**：① `group=collection` = "
                "`BattleManager.soundCollection`(`+0x80`) → `SoundAssetCollection` 的 28 个字段；"
                "② `group=card` = **卡 prefab 上 `CardScript`** 那一块（9 条 + `changeTo*` 2 条；"
                "🔴 **不是逐卡序列化** —— 全资产里只有一份，小/中/大三档还指向同一条音）；"
                "③ `group=scene` = **战场场景/预制体上直接序列化的 `AudioCue`**"
                "（`EndBattleDoors.victoryCue/defeatAudioCue/drawAudioCue` × 13 场 · `OvertimeUi.enteringOvertimeSound` × 13 场 · "
                "`ShakeAndSoundObject.sounds[0].sound` = 环境条件 Blackout）；"
                "④ `group=event` = **活动弹窗开窗音**（`EventElements.EventAssets[key=8 OpenSound]`，"
                "消费点 `BaseEventPopup.Open` 的 `AudioCue.Play2D(MixerType.Jingles)`）。"
                "⚠️ `name` 那一列：**①② 是原版字段名**（C# 按字段名索引，如 `startTurn` / `invokeCUE`）、"
                "**③④ 是 cue 资产名**（`srcCue` 同值）—— 这是四组之间唯一的形状差异。"
                "clips 里随机挑一条；音量 = minVolume..maxVolume、音高 = minPitch..maxPitch；"
                "文件名在 Resources/Art/audio/sfx/<clip 名>。"
                "①② 全部走 MixerType.FX（反编译调用点第 4 实参恒为 0）；③④ 走 `MixerType.Jingles`（=3，调用点见各组出处）。"
                "`gaps[]` = 原版这一格里就没有声音的那些字段（空引用 / 悬挂 GUID / 空 PPtr），⛔ 不是我们没做。"
                "🔴 **③④ 的「谁指向哪条 cue」写死在生成器的 `HAND_WRITTEN`**（引用者在场景/预制体/活动 SO 上，"
                "没有自动取材口），**但参数是当场从 shipped cue 束重解的** ⇒ 重跑生成器**不会丢**这 7 条"
                "（`Editor/B121_AUD_Assert.cs` / `B121_D2_Assert.cs` 逐名钉住）。"
                "⚠️ `Draw` 在 `animfx_sounds.json` 里**还有一条同名的旧手写副本**（现 `import_original_sfx.py` 的取材面"
                "产不出它）⇒ `WFSoundBank` 撞名时保留**先装的**那条并警告一次；两条的参数逐值相同（clip `MatchDraw` · "
                "vol 0.7 · ttp 0.1），行为无差别。下次重生成 `animfx_sounds.json` 会自然掉那条旧副本。"
                "判据全文 → `资料/完全复刻/报告/B121_D2_音效cue覆盖.md`（③④ 的持有者与搜过的三种目录）。",
        "source": {
            "so": "SoundAssetCollection（bundle_duplicateassetisolationso_assets_all，"
                  "挂在 BattleManager.soundCollection +0x80）",
            "card_block": "CardScript.battleCardSounds（bundle_battleprefabs_vfxandmisc_assets_all，"
                          "**全资产唯一一份**；字段是普通 PPtr ⇒ 按束 externals 解到 soundcollection 的 cue）",
            "cue_bundle": "soundcollection_assets_all.bundle",
            "guid_resolver": "工具/_guid_resolve_probe.py（catalog_main.json；**只 A 组用**）",
            "script": "工具/import_battle_sfx.py（四组都产出；③④ 见脚本里的 `HAND_WRITTEN`）",
            "scene_group": "③ 战场场景序列化字段：`EndBattleDoors.victoryCue/defeatAudioCue/drawAudioCue`（13 场 arena 的同一组 pid）+ `OvertimeUi.enteringOvertimeSound`（同 13 场）+ `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour_9031176634359415475.json` 的 `sounds[0].sound`（`m_Script` pathID 8184164905518033017 → 类 `ShakeAndSoundObject`，= 环境条件 Saim Hann Blackout）",
            "event_group": "④ 活动开窗音：`EventElements.EventAssets` 里 `key=8 (EventAssetReferenceType.OpenSound)`，资产 = bundle_cosmeticsso_assets_all 的 `Expansion Orks1 Parent Event` / `Release Space Wolves Parent Event`；消费点 = `BaseEventPopup.Open`（cpp2il `BaseEventPopup.cs:56-60`，`AudioCue.Play2D(MixerType.Jingles)`）"
        },
        "mixer": "FX",
        "cues": table,
        "gaps": gaps,
    }
    if mode == "--write":
        io.open(DST_JSON, "w", encoding="utf-8", newline="\n").write(
            json.dumps(out, ensure_ascii=False, indent=1))
        print(f"已拷 {copied} 条 · 已存在跳过 {skipped} 条 · 补 meta {n_meta} 条 → {DST_AUDIO}")
        print(f"表已写：{DST_JSON}")
        return 0
    if mode == "--dry":
        print(f"干跑（未写盘）：会拷 {sum(1 for c in need_files if c not in have)} 条音频 → {DST_AUDIO}")
        print(f"                 会写表 {DST_JSON}（{len(table)} 条 cue）")
        return 0
    else:
        # 🔴 2026-10-27（执行代理 B9_W35CHECK）：`--check` 必须**连产物一起查** ——
        #   原来这里只打「（--check：没有写盘）」；`have` 那行虽算了「要拷 N 个」却不点名、不报红。
        #   判据同 工具/import_original_art.py `--only-altart`（`:1714-1721`）。
        lack_json = None
        if not os.path.exists(DST_JSON):
            lack_json = f"{DST_JSON} 不存在"
        else:
            old = io.open(DST_JSON, encoding="utf-8", newline="").read()
            if old != json.dumps(out, ensure_ascii=False, indent=1):
                lack_json = f"{DST_JSON} 已过期（与现生成不一致）"
        lack_files = []
        for cl in need_files:
            src = files[cl]
            ext = os.path.splitext(src)[1].lower()
            dst = os.path.join(DST_AUDIO, cl + ext)
            if not os.path.exists(dst):
                lack_files.append(cl + ext)
            elif os.path.getsize(dst) != os.path.getsize(src):
                lack_files.append(cl + ext + "（尺寸不符）")
        if lack_json:
            print("🔴 产物缺失/过期：" + lack_json)
        if lack_files:
            print(f"🔴 音频产物缺失 {len(lack_files)}/{len(need_files)} 个（{DST_AUDIO}）—— "
                  f"重跑不带 --check 落盘：")
            for b in lack_files[:50]:
                print("   缺:", b)
            if len(lack_files) > 50:
                print(f"   …共 {len(lack_files)} 个")
        # 🆕 2026-11-03（`B121-AUD-03`）：C 组（手写组）的 clip **不走拷贝那条路**（原因见 `hand_clips`），
        #   但照样要验 —— 按 **stem** 查 `Resources/Art/audio/sfx/` 里有没有（扩展名不比，原版解包是
        #   `.vorbis`、我们导入的是 `.wav` 的那几条就靠这一条）。
        lack_hand = sorted(cl for cl in hand_clips if cl not in have)
        if lack_hand:
            print(f"🔴 C 组（scene/event）手写 cue 的 clip 在 {DST_AUDIO} 里缺 {len(lack_hand)}/{len(hand_clips)} 个：")
            for cl in lack_hand[:50]:
                print("   缺:", cl)
        if not lack_json and not lack_files and not lack_hand:
            print(f"✅ --check：产物齐全（表 {DST_JSON} · 音频 {len(need_files)} 个 · "
                  f"C 组手写 clip {len(hand_clips)} 个按 stem 在位）")
        print("（--check：没有写盘）")
        return 0 if (not lack_json and not lack_files and not lack_hand) else 1


if __name__ == "__main__":
    sys.exit(main())
