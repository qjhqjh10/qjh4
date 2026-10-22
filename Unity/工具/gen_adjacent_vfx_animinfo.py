#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_adjacent_vfx_animinfo.py — 「相邻单位各播一份」那个特效模块的**播放真值**表。

账：`资料/普查产出_1022/W64_兜底审计_特效.md` **B18**
    （原文：「`AnimFXInstanceParticleAdjacent`：Doomweaver shader 缺 + 起终点未接」）

这个模块在全库**只有 1 个实例**（`BulletImpact_deathspinner_arc_alt`），但它要的东西
（一份 `CardAnim` 从 A 播到 B）我们运行时**一个都还没接**，数据侧也从来没导出过。
本脚本把「原版到底在哪播、播什么、播多久」全解出来落表 —— 运行时接线的判据就是它。

判据链（**逐方法体**，不是按名字猜）
------------------------------------
  ① `AnimFXInstanceParticleAdjacent__Initialize.c`
       · `+0x38` = `cardAnim`（`AssetReferenceTyped<CardAnim>`）：**引用无效就
         `Debug.LogError` 并整条 return**（那句 Roope 的报错）
       · `+0x44` = `playOnRetaliation` · `+0x30` = 基类的 `isRetaliation` · `+0x40` = `delay`
       · 放行条件 = `playOnRetaliation || !isRetaliation`；
         `delay > 0` ⇒ 起协程，否则**立刻** `ExecuteEffect()`
  ② `AnimFXInstanceParticleAdjacent._DelayActivation_d__4__MoveNext.c`
       ⇒ **`WaitForSeconds(delay)` 之后 `ExecuteEffect()`** ——
       这一条以前只有推断（见 `WFModuleInstanceParticleAdjacent.cs` 文件头「我们定的 ①」），
       **现在方法体读得到，推断坐实**
  ③ `AnimFXInstanceParticleAdjacent__ExecuteEffect.c`
       · 目标集合 = `BattleManager.GetAdjacentUnits(controller.targetCard)`（**目标卡**的相邻单位）
       · 逐单位：`cardAnim.Load()` → `CreateAnimFromAnimFxController(animInfo,
         actingCard.transform, unit.transform, unit.transform, actingCard, unit, null, false)`
  ④ `BattleManager__CreateAnimFromAnimFxController.c`
       ⇒ **`from`/`to` 那两个实参【没用】**：真正取的是
         `GetAnimTransform(animInfo.startPosOption)` 与 `GetAnimTransform(animInfo.endPosOption)`，
         实参表 = (actingCard, unit) —— 所以**起点/终点由 `animInfo` 的两个 option 决定**
  ⑤ `BattleManager__GetAnimTransform.c` —— option 枚举 → Transform（表见产物 `_judges`）
       · 本实例 `startPosOption = 1` / `endPosOption = 1` ⇒ **两者都是
         `unit.BattleCardUI.effectsAnchor`**（`BattleCardUI +0x1A0`，`dump.cs:26166` 那一段）
       ⇒ 🔴 **`WFModuleInstanceParticleAdjacent.cs` 文件头那句「起点 = 出手卡，终点 = 那个相邻单位」
         【是错的】**：本实例起终点**同一个**（都是那个相邻单位的 `effectsAnchor`），
         而且 `shouldMoveVFX = 0` / `timeMoving = 0` ⇒ 它**不飞**，只在那个锚点上停 0.5 s。
  ⑥ shader：该材质 = `Material_-357286667203166072.json`（`m_Name = Doomweaver effect`），
     `m_Shader` → `Shader_3612509111447358907.json` = **`Shader Graphs/Doomweaver effect`**；
     ✅ **我们已经有它**（`WarpforgeVFX/Shaders/WFDoomweaverEffect.shader`，按字节码逐条重写，
     并在 `WarpforgeShaderMap.UseOriginal` 白名单里 —— 判据 → 那个文件 `:89` 与 `:308`）

产物
----
`d:/4/Unity/数据/游戏数据/adjacent_vfx_animinfo.json`

用法
----
  python -I -X utf8 d:/4/Unity/工具/gen_adjacent_vfx_animinfo.py            # 重出产物
  python -I -X utf8 d:/4/Unity/工具/gen_adjacent_vfx_animinfo.py --check    # 只比不写（幂等自检）
"""

import io
import json
import os
import sys

ROOT4 = "d:/4/Unity"
ROOT2 = "d:/2"

COMPONENTS = ROOT4 + "/数据/游戏数据/animfx_components.json"
ADDR_MAP = ROOT4 + "/数据/索引/anim_address_map.json"
ASSETS_FULL = ROOT2 + "/新解包资源/assets_full"
VFX_LIB = ROOT4 + "/MyGame/Assets/WarpforgeVFX/Prefabs"
SHADER_DIR = ROOT4 + "/MyGame/Assets/WarpforgeVFX/Shaders"
EFFECT_LIB = ROOT4 + "/MyGame/Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset"

OUT = ROOT4 + "/数据/游戏数据/adjacent_vfx_animinfo.json"

MODULE_KIND = "AnimFXInstanceParticleAdjacent"

# ⑤ `GetAnimTransform` 的 option → 取哪个 Transform（逐行读 `BattleManager__GetAnimTransform.c`；
#   `BattleManager` 的字段名用 `d:/2/tools/il2cpp_out/dump.cs` 的偏移表核过）
ANIM_TRANSFORM_OPTIONS = {
    "0":  "actingCard（第 6 个实参）的 `BattleCardUI.effectsAnchor`（`CardScript+0x290` → `+0x1A0`）",
    "1":  "unit（第 7 个实参）的 `BattleCardUI.effectsAnchor`（同一跳）",
    "2":  "同 1（`unit` 的 `effectsAnchor`）",
    "3":  "`BattleManager.playerManager(0xC0)` → 那张卡 → `CardScript.GetEffectAnchor()`",
    "4":  "`BattleManager.enemyManager(0xC8)` → 同上",
    "5":  "`BattleManager.battleCenterAnchor(0x1D0)`",
    "18": "`actingCard.isPlayer ? playerHand(0xD0) : enemyHand(0xD8)` → 那张卡 → `GetEffectAnchor()`",
    "19": "`actingCard.isPlayer ? enemyHand(0xD8) : playerHand(0xD0)` 的 transform",
    "50": "`actingCard.isPlayer ? playerDeck(0xF0) : enemyDeck(0x108)` 的 transform",
    "55": "`actingCard.isPlayer ? enemyDeck(0x108) : playerDeck(0xF0)` 的 transform",
    "14": "鼠标点击处 → `MinionManager.GetNextSlotPos` 造一个临时 Transform",
}

# `animInfo` 里与「在哪播 / 播多久」有关的字段（其余原样整块带出）
ANIMINFO_KEYS = ("startPosOption", "endPosOption", "pointTowardsOption", "vfxDelayTime",
                 "startDelay", "timeAtStartPos", "timeMoving", "timeAtEndPos",
                 "shouldMoveVFX", "orientToVelocity", "applyMovementOffsets",
                 "waitAnimToApplyEffect")


def load(p):
    return json.load(io.open(p, encoding="utf-8"))


def find_cardanim_dump(bundle, name):
    """`assets_full/bundle_<bundle>/MonoBehaviour/<name>.json`。

    ⚠️ 三种写法都会出现（`anim_address_map.json` 里 `sourceBundle` 已经带了 `bundle_` 前缀）：
       `aeldarisaimhanncardanims_assets_all` / `…_assets_all.bundle` / `bundle_…_assets_all`
    ⇒ 统一剥成「裸束名」再拼一次。"""
    if not bundle or not name:
        return None
    b = bundle
    if b.endswith(".bundle"):
        b = b[:-7]
    if b.startswith("bundle_"):
        b = b[len("bundle_"):]
    p = os.path.join(ASSETS_FULL, "bundle_" + b, "MonoBehaviour", name + ".json")
    return p if os.path.isfile(p) else None


def shader_facts(amap, prefab_dump_rel):
    """本实例那条材质 → 原版 shader 名，以及我们这侧有没有对应的重写件。

    ⚠️ **只能按「材质文件名 = 材质名」找**：`Deathspinner Cut Effect` 这个 prefab 是个
    **纯模块控制器**（4 个 MonoBehaviour + 1 个 Transform，**没有渲染器**）⇒ 材质挂在
    它模块引用的子物体上，prefab→材质那一跳不在本表里做（如实标）。"""
    mat = os.path.join(ASSETS_FULL, "bundle_battleprefabs_vfxandmisc_assets_all/Material",
                       "Material_-357286667203166072.json")
    sh = os.path.join(ASSETS_FULL, "bundle_battleprefabs_vfxandmisc_assets_all/Shader",
                      "Shader_3612509111447358907.json")
    out = {"materialAsset": None, "materialName": None,
           "originalShader": "Shader Graphs/Doomweaver effect",
           "originalShaderAsset": None,
           "rewrittenShader": "WarpforgeVFX/FX/DoomweaverEffect",
           "rewrittenShaderFile": None,
           "inUseOriginalWhitelist": None,
           "prefabToMaterialHop": "不在本表：prefab 是纯模块控制器（无渲染器），材质挂在模块引用的子物体上"}
    if os.path.isfile(mat):
        m = load(mat)
        out["materialAsset"] = mat.replace(ROOT4 + "/", "")
        out["materialName"] = m.get("m_Name")
    if os.path.isfile(sh):
        out["originalShaderAsset"] = sh.replace(ROOT4 + "/", "")
    for cand in ("WFDoomweaverEffect.shader",):
        p = os.path.join(SHADER_DIR, cand)
        if os.path.isfile(p):
            out["rewrittenShaderFile"] = "MyGame/Assets/WarpforgeVFX/Shaders/" + cand
    sm = os.path.join(ROOT4, "MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs")
    if os.path.isfile(sm):
        txt = io.open(sm, encoding="utf-8", errors="replace").read()
        out["inUseOriginalWhitelist"] = ('"Shader Graphs/Doomweaver effect"' in txt)
    return out


def build():
    comp = load(COMPONENTS)
    amap = load(ADDR_MAP)
    by_guid = amap.get("cardanim_guid_to_name") or {}
    to_asset = amap.get("cardanim_to_asset") or {}

    rows = []
    for eff in sorted(comp.get("effects") or {}):
        for m in comp["effects"][eff]:
            if m.get("class") != MODULE_KIND:
                continue
            f = m.get("fields") or {}
            guid = str(((f.get("cardAnim") or {}).get("m_AssetGUID") or "")).strip().lower()
            ca_name = by_guid.get(guid) or ""
            target = to_asset.get(ca_name) or {}
            prefab = target.get("targetName") or ""
            src_bundle = target.get("sourceBundle") or ""
            dump = find_cardanim_dump(src_bundle, ca_name)
            anim_info = {}
            if dump:
                anim_info = (load(dump).get("animInfo") or {})

            pick = {k: anim_info.get(k) for k in ANIMINFO_KEYS if k in anim_info}
            start = anim_info.get("startPosOption")
            end = anim_info.get("endPosOption")
            positions = {
                "startPosOption": start,
                "endPosOption": end,
                "startMeans": ANIM_TRANSFORM_OPTIONS.get(str(start), "未在表里（见原版 `GetAnimTransform` 的 default 分支：LogError + 退回出手卡锚点）"),
                "endMeans": ANIM_TRANSFORM_OPTIONS.get(str(end), "未在表里（同上）"),
                "sameAnchor": (start == end),
                "note": ("`CreateAnimFromAnimFxController` 收的 `from`/`to` 两个 Transform **没用** —— "
                         "它只用 `animInfo.startPosOption/endPosOption` 去 `GetAnimTransform` 取锚点"
                         "（判据 → `BattleManager__CreateAnimFromAnimFxController.c`）。"),
            }
            rows.append({
                "effect": eff,
                "nodePath": m.get("nodePath") or "",
                "delay": f.get("delay"),
                "playOnRetaliation": bool(f.get("playOnRetaliation")),
                "cardAnimGuid": guid,
                "cardAnim": ca_name,
                "cardAnimDump": (dump.replace(ASSETS_FULL + "/", "") if dump else None),
                "prefab": prefab,
                "prefabBundle": target.get("targetBundle"),
                "prefabInProject": bool(prefab) and os.path.isfile(os.path.join(VFX_LIB, prefab + ".prefab")),
                "inEffectLibrary": bool(prefab) and os.path.isfile(EFFECT_LIB) and
                                   (eff in io.open(EFFECT_LIB, encoding="utf-8", errors="replace").read()),
                "animInfo": pick,
                "animInfoEaseCurve": anim_info.get("easeCurve"),
                "animDurationSeconds": round(
                    float(anim_info.get("vfxDelayTime") or 0.0)
                    + float(anim_info.get("timeAtStartPos") or 0.0)
                    + float(anim_info.get("timeMoving") or 0.0)
                    + float(anim_info.get("timeAtEndPos") or 0.0), 4),
                "positions": positions,
                "shader": shader_facts(amap, target.get("dump")),
            })

    headline = (
        "🔴 `W64 · B18`：`AnimFXInstanceParticleAdjacent`（全库 **%d 个实例**）的播放真值。"
        "原版语义（逐方法体）：目标集合 = `GetAdjacentUnits(controller.targetCard)`；"
        "每个相邻单位各 `CreateAnimFromAnimFxController(animInfo, actingCard.t, unit.t, unit.t, …)`；"
        "而该方法**只用** `animInfo.startPosOption/endPosOption` 取锚点 ⇒ "
        "本实例 **start/end 都是那个相邻单位的 `BattleCardUI.effectsAnchor`**、"
        "`shouldMoveVFX = 0` ⇒ **不飞**，只停 `timeAtStartPos` 秒。"
        "⛔ 所以 `WFModuleInstanceParticleAdjacent.cs` 文件头那句「起点 = 出手卡、终点 = 相邻单位」"
        "**要改**（那是没读 `CreateAnimFromAnimFxController` 时的推断）。"
        "shader `Shader Graphs/Doomweaver effect` **我们已有重写件**"
        "（`WarpforgeVFX/FX/DoomweaverEffect` + `UseOriginal` 白名单）⇒ B18 ① 已不欠。"
        % len(rows))

    conventions = {
        "whoPlaysIt": "原版：`AnimFXInstanceParticleAdjacent.ExecuteEffect` —— 目标集合是"
                      "**控制器 `targetCard` 的相邻单位**（不是出手卡的相邻单位；"
                      "判据 `__ExecuteEffect.c:45` 读 `controller+0x28 → +0x58`）",
        "wherePlays": "锚点由 `animInfo.startPosOption/endPosOption` 决定（表见 `_judges`）；"
                      "本实例两个都是 `unit.BattleCardUI.effectsAnchor`（`dump.cs:26166` → `effectsAnchor // 0x1A0`）",
        "delay": "`delay` 秒（`WaitForSeconds`，方法体已坐实）之后才 `ExecuteEffect`；"
                 "`playOnRetaliation == false` 时**反击不算**（`__Initialize.c` 的放行条件）",
        "next": "**运行时还欠的接线**（都不在本批文件所有权内，只报告）："
                "① 需要一个能把一份 CardAnim「贴到某个 Transform 上播」的载体（我们只有 "
                "`WarpforgeEffectPlayer.Play(name,parent,localPos)`）"
                "② 需要「相邻单位」那份棋盘信息（在 `BattleDriver`，VFX 层不认识局面）"
                "③ 需要上层「谁在什么时候播」（`VfxMap` 现在是「一个事件→一个特效」）",
    }

    return {
        "schema": "adjacent_vfx_animinfo/v1",
        "generatedBy": "d:/4/Unity/工具/gen_adjacent_vfx_animinfo.py",
        "_sources": {
            "animfxComponents": COMPONENTS,
            "animAddressMap": ADDR_MAP,
            "assetsFull": ASSETS_FULL,
            "decomp": ROOT2 + "/tools/decomp_full/",
            "offsets": ROOT2 + "/tools/il2cpp_out/dump.cs",
        },
        "_judges": {
            "initialize": "AnimFXInstanceParticleAdjacent__Initialize.c（+0x38 cardAnim · +0x44 playOnRetaliation"
                          " · +0x40 delay · +0x30 isRetaliation）",
            "delay": "AnimFXInstanceParticleAdjacent._DelayActivation_d__4__MoveNext.c（WaitForSeconds → ExecuteEffect）",
            "execute": "AnimFXInstanceParticleAdjacent__ExecuteEffect.c（GetAdjacentUnits(targetCard) → 逐单位）",
            "animFromTo": "BattleManager__CreateAnimFromAnimFxController.c（from/to 实参不用；只看两个 option）",
            "animTransformOptions": ANIM_TRANSFORM_OPTIONS,
            "shader": "Material_-357286667203166072.json → Shader_3612509111447358907.json"
                      "（`Shader Graphs/Doomweaver effect`）；我们的重写件 "
                      "`Assets/WarpforgeVFX/Shaders/WFDoomweaverEffect.shader`",
        },
        "_headline": headline,
        "_conventions": conventions,
        "instances": rows,
        "stats": {
            "instances": len(rows),
            "prefabInProject": len([r for r in rows if r["prefabInProject"]]),
            "inEffectLibrary": len([r for r in rows if r["inEffectLibrary"]]),
            "sameStartEndAnchor": len([r for r in rows if r["positions"]["sameAnchor"]]),
            "unresolvedCardAnim": len([r for r in rows if not r["cardAnim"]]),
        },
    }


def canon(o):
    return json.dumps(o, ensure_ascii=False, sort_keys=True, indent=1)


def main():
    check = "--check" in sys.argv
    doc = build()
    if check:
        if not os.path.exists(OUT):
            print("✗ 缺文件: " + OUT)
            return 1
        if canon(load(OUT)) == canon(doc):
            print("✅ 一致: " + OUT)
            print("=== --check 全过 ✅")
            return 0
        print("✗ 不一致: " + OUT)
        return 1
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(canon(doc))
    print("[写出] %s (%d 字节)" % (OUT, os.path.getsize(OUT)))
    s = doc["stats"]
    print("实例 %d 个 · prefab 在工程里 %d · 在效果库里 %d · 起终点同一个锚 %d · cardAnim 解不出 %d"
          % (s["instances"], s["prefabInProject"], s["inEffectLibrary"],
             s["sameStartEndAnchor"], s["unresolvedCardAnim"]))
    for r in doc["instances"]:
        print("   %s | delay=%s playOnRetaliation=%s" % (r["effect"], r["delay"], r["playOnRetaliation"]))
        print("      cardAnim=%s → prefab=%s（工程里:%s 库里:%s）"
              % (r["cardAnim"], r["prefab"], r["prefabInProject"], r["inEffectLibrary"]))
        print("      start/end option = %s/%s ⇒ %s"
              % (r["animInfo"].get("startPosOption"), r["animInfo"].get("endPosOption"),
                 r["positions"]["startMeans"]))
        print("      timeAtStartPos=%s timeMoving=%s timeAtEndPos=%s shouldMoveVFX=%s ⇒ 时长 %s s"
              % (r["animInfo"].get("timeAtStartPos"), r["animInfo"].get("timeMoving"),
                 r["animInfo"].get("timeAtEndPos"), r["animInfo"].get("shouldMoveVFX"),
                 r["animDurationSeconds"]))
        print("      shader=%s（重写件:%s · UseOriginal:%s）"
              % (r["shader"]["originalShader"], r["shader"]["rewrittenShaderFile"],
                 r["shader"]["inUseOriginalWhitelist"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
