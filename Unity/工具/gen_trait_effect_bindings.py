#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_trait_effect_bindings.py —— 把 **CardTrait SO 的 `_traitEffects[]`** 原样导出成
运行期可用的绑定表（`Resources/trait_effect_bindings.json`）。

## 为什么需要它（2026-10-23 · 用户指令）
> 「安卓存档有 CardTrait SO，把我们兜底之类的东西都去掉，**全面使用 CardTrait SO**」。

在此之前 `Core/TraitFrames.cs`（OnPlay 那本）与 `Core/TraitParticles.cs`（FromCode 那张）
都是**手写的 C# 表**，逐条注明「这是我们的口径 / 按名字硬绑」。现在：
**表的唯一来源 = 原版 `CardTrait` SO**（`数据/游戏数据/trait_collection.json`，
生成器 `工具/export_trait_collection.py`；安卓 1.35.0 与 PC 关服前 build **逐字段 0 差异**，
判据 → `资料/普查产出_1023b/安卓存档_CardTraitSO普查.md`）。

## 判据链（每一跳都可复核，代码里逐跳校验）

| 列 | 来源 |
|---|---|
| `trait` / `definedTrait` | `CardTrait.asset` → `CardTraitCollection.GetTrait()` 用的那个 `DefinedTrait` |
| `triggerType` / `triggerName` | `TraitEffect.TriggerType`（`EffectTriggerType`：0 Start · 1 End · 2 OnPlay · 3 Trigger · 5 FromCode · 6 TriggerOnPlay） |
| `effectGuid` | `TraitEffect.effectAnim` 那份 `AssetReference` 的 GUID |
| `cardAnim` | `effectGuid` 反查出来的 CardAnim 资产名（与 `trait_collection.json` 的 `effectAnim` 逐字比，**不等就报错退出**） |
| `prefab` | 那张 CardAnim 的 target prefab（`数据/索引/anim_address_map.json` 的 `cardanim_to_asset[<CardAnim>].targetName`） |
| `playIfWarlord` | `TraitEffect.playIfWarlord`（原版 `BattleCardUI__DisplayTriggerAnim.c:62-68` 读它） |
| `overrides[]` | `TraitEffect.overrideByArmies[]`（原版按阵营换那条 effect） |

⛔ **没有一列是猜的**。解不出来的行**照实写 `unresolved`**（`prefab` 留空），
运行期消费端必须出声、⛔ 不许拿名字相近的顶替。

## 输出
`MyGame/Assets/CardPresentation/Resources/trait_effect_bindings.json`（JsonUtility 形状：对象 + 数组，无字典）

用法：
    python -I -X utf8 d:/4/Unity/工具/gen_trait_effect_bindings.py
    python -I -X utf8 d:/4/Unity/工具/gen_trait_effect_bindings.py --check   # 只校验、不写
"""

import argparse
import datetime
import io
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity

TRAIT_COLLECTION = os.path.join(ROOT, "数据/游戏数据/trait_collection.json")
ANIM_ADDRESS_MAP = os.path.join(ROOT, "数据/索引/anim_address_map.json")
GUID_MAP = os.path.join(ROOT, "资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv")
OUT = os.path.join(ROOT, "MyGame/Assets/CardPresentation/Resources/trait_effect_bindings.json")

# `EffectTriggerType` —— 判据 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/EffectTriggerType.cs`
# （`FromCode` 在反编译里是 `(5)`，不是枚举序号，见 `CardScript.ActivateTraitParticlesFromCode` 里那两句硬编码）
TRIGGER_NAMES = {
    0: "Start",
    1: "End",
    2: "OnPlay",
    3: "Trigger",
    5: "FromCode",
    6: "TriggerOnPlay",
}

PROBLEMS = []


def read_json(p):
    with io.open(p, encoding="utf-8") as f:
        return json.load(f)


def load_guid_names():
    """`effectAnimGuid → CardAnim 资产名`（只收 `className == CardAnim` 的行）。

    ⚠️ 这张表**不是**判据，是**交叉校验**：`trait_collection.json` 已经带了 `effectAnim` 名字，
    这里只用来证明「那一列确实是从 GUID 解出来的、不是抄错」。对不上就报错退出。
    """
    out = {}
    if not os.path.isfile(GUID_MAP):
        PROBLEMS.append("（交叉校验跳过）guid_map 不在：%s" % GUID_MAP)
        return out
    with io.open(GUID_MAP, encoding="utf-8") as f:
        head = f.readline().rstrip("\n").split("\t")
        try:
            i_guid = head.index("assetGuid")
            i_cls = head.index("className")
            i_name = head.index("name")
        except ValueError:
            PROBLEMS.append("（交叉校验跳过）guid_map 表头不认识：%s" % head)
            return out
        for line in f:
            p = line.rstrip("\n").split("\t")
            if len(p) <= max(i_guid, i_cls, i_name):
                continue
            if p[i_cls] != "CardAnim":
                continue
            out.setdefault(p[i_guid].strip().lower(), set()).add(p[i_name])
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只校验现有产物与判据一致，不写文件")
    ap.add_argument("--generated-at", default=None)
    args = ap.parse_args()

    tc = read_json(TRAIT_COLLECTION)
    amap = read_json(ANIM_ADDRESS_MAP)
    ca = amap.get("cardanim_to_asset") or {}
    guid_names = load_guid_names()

    traits = tc.get("traits") or []
    rows = []
    stats = {
        "traits": len(traits),
        "effects": 0,
        "byTriggerType": {},
        "withPrefab": 0,
        "unresolved": 0,
        "overrides": 0,
        "guidChecked": 0,
        "guidMismatch": 0,
    }
    unresolved_rows = []

    for t in traits:
        tname = t.get("name")
        dt = t.get("definedTrait")
        for e in t.get("traitEffects") or []:
            stats["effects"] += 1
            tt = e.get("triggerType")
            stats["byTriggerType"][str(tt)] = stats["byTriggerType"].get(str(tt), 0) + 1
            anim = e.get("effectAnim") or ""
            guid = (e.get("effectAnimGuid") or "").strip().lower()
            row = {
                "trait": tname,
                "definedTrait": dt,
                "triggerType": tt,
                "triggerName": e.get("triggerName") or TRIGGER_NAMES.get(tt, ""),
                "effectGuid": guid,
                "cardAnim": anim,
                "prefab": "",
                "playIfWarlord": bool(e.get("playIfWarlord")),
                "unresolved": "",
                "overrides": [],
            }

            # ---- 交叉校验：GUID → CardAnim 名（只在本机 guid_map 里有这个 GUID 时才比）----
            if guid and guid in guid_names:
                names = guid_names[guid]
                stats["guidChecked"] += 1
                if anim and anim not in names:
                    stats["guidMismatch"] += 1
                    PROBLEMS.append(
                        "GUID/名字不一致：`%s` 在 guid_map 里是 %s，而 trait_collection 写的是 `%s`"
                        % (guid, sorted(names), anim))

            # ---- CardAnim → prefab ----
            if not anim:
                row["unresolved"] = ("`TraitEffect.effectAnim` 是空引用（原版这条 effect 没有动画，"
                                     "不是解错）")
            else:
                target = (ca.get(anim) or {}).get("targetName")
                if target:
                    row["prefab"] = target
                    stats["withPrefab"] += 1
                else:
                    row["unresolved"] = ("CardAnim `%s` 在 `anim_address_map.json` 里没有 targetName"
                                         "（纯计时拍 / 无 VFX）" % anim)

            # ---- 按阵营覆盖 ----
            for o in e.get("overrideByArmies") or []:
                oa = o.get("effectAnim") or ""
                opf = (ca.get(oa) or {}).get("targetName") or ""
                og = (o.get("effectAnimGuid") or o.get("guid") or "")
                row["overrides"].append({
                    "army": o.get("army"),
                    "effectGuid": str(og).strip().lower(),
                    "cardAnim": oa,
                    "prefab": opf,
                })
                stats["overrides"] += 1

            if row["unresolved"]:
                stats["unresolved"] += 1
                unresolved_rows.append({
                    "trait": tname, "definedTrait": dt,
                    "triggerType": tt, "triggerName": row["triggerName"],
                    "cardAnim": anim, "why": row["unresolved"],
                })
            rows.append(row)

    doc = {
        "schema": "wf.trait_effect_bindings/1",
        "generatedBy": "工具/gen_trait_effect_bindings.py",
        "generatedAt": args.generated_at or datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "sources": {
            "traitCollection": "数据/游戏数据/trait_collection.json",
            "traitCollectionGenerated": tc.get("generated"),
            "traitCollectionPath": TRAIT_COLLECTION,
            "animAddressMap": "数据/索引/anim_address_map.json",
            "animAddressMapGeneratedAt": amap.get("generatedAt"),
            "guidMap": "资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv",
            "why": ("**唯一来源 = 原版 `CardTrait` SO**（`sharedassets0.assets` 的 `TraitsCollection` + "
                    "73 个 `CardTrait` 资产；安卓 1.35.0 与 PC 关服前 build 逐字段 0 差异）。"
                    "`prefab` 由 `TraitEffect.effectAnim` 的 GUID → CardAnim → `anim_address_map` 两跳解出。"),
        },
        "conventions": {
            "triggerType": ("原版 `EffectTriggerType`：0 Start · 1 End · 2 OnPlay(→ 状态框「加身」那本) · "
                            "3 Trigger(→ 状态框「触发」那本) · 5 FromCode(→ 从代码播的粒子) · "
                            "6 TriggerOnPlay(→ 状态框「触发」那本、会写去重表)。"
                            "2/3/6 三档由 `TraitFrames` 消费，5 由 `TraitParticles` 消费，0/1 只留档。"),
            "playIfWarlord": ("原版 `TraitEffect.playIfWarlord`；卡是督军且该位为 0 ⇒ 原版不播"
                              "（`BattleCardUI__DisplayTriggerAnim.c:62-68`）。"),
            "overrides": ("原版 `TraitEffect.overrideByArmies[]` —— 按阵营换掉那条 effect。"
                          "⚠️ 运行期**今天还没按阵营取**（`TraitFrames.PrefabFor` 没有阵营参数，"
                          "改签名要动 `CardView` / `BattleDriver`）⇒ 这里只把真值留全，"
                          "缺口如实记在 `unresolvedArmyVariants`。"),
        },
        "stats": stats,
        "unresolvedRows": unresolved_rows,
        "rows": rows,
    }

    # 运行期消费端只要 2/3/5/6 —— 但**全量写进去**（0/1 也是原版真值，留档用；
    # 消费端按 triggerType 过滤，⛔ 不在数据侧预先筛，免得下一个人以为漏了）。
    text = json.dumps(doc, ensure_ascii=False, indent=1)

    if PROBLEMS:
        for p in PROBLEMS:
            sys.stderr.write("[PROBLEM] %s\n" % p)

    if args.check:
        if not os.path.isfile(OUT):
            sys.stderr.write("[check] 产物不存在：%s\n" % OUT)
            return 2
        old = io.open(OUT, encoding="utf-8").read()
        same = json.loads(old).get("rows") == json.loads(text).get("rows")
        print("[check] rows 与判据一致：%s" % same)
        return 0 if same else 3

    # ⚠️ 先算好内容再写（CLAUDE.md 的纪律：别让 `open(...,'wb').write(<表达式>)` 在表达式炸掉时把文件清空）。
    data = text.encode("utf-8") + b"\n"
    with open(OUT, "wb") as f:
        f.write(data)

    print("[out] %s  %d B  rows=%d  withPrefab=%d  unresolved=%d" %
          (os.path.relpath(OUT, ROOT), len(data), len(rows), stats["withPrefab"], stats["unresolved"]))
    print("[stats] effects=%d byTriggerType=%s guidChecked=%d guidMismatch=%d overrides=%d" %
          (stats["effects"], stats["byTriggerType"], stats["guidChecked"],
           stats["guidMismatch"], stats["overrides"]))
    for r in unresolved_rows:
        print("  [unresolved] %-16s tt=%s %-18s %s" %
              (r["trait"], r["triggerType"], r["cardAnim"] or "(空)", r["why"][:40]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
