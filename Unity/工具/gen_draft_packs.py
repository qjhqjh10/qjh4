#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_draft_packs.py — 原版「轮抽（Draft）」的卡包表 → 一份工程数据

输入（解包产物，**只读**，⛔ 本脚本一个字都不改它）：
    d:/2/新解包资源/assets_full/bundle_draftpacks_assets_all/MonoBehaviour/*.json
    882 个 `PrebuiltPack` SO（`PrebuiltPack : IdentifiableSO`，`d:/2/tools/il2cpp_out/dump.cs:23093`）

输出（覆盖写，可重跑）：
    d:/4/Unity/数据/游戏数据/draft_packs.json

用法：
    python -I -X utf8 d:/4/Unity/工具/gen_draft_packs.py               干跑（打读数、零写盘）
    python -I -X utf8 d:/4/Unity/工具/gen_draft_packs.py --write       落盘
    python -I -X utf8 d:/4/Unity/工具/gen_draft_packs.py --check       只对账（与盘上比），不写盘
    python -I -X utf8 d:/4/Unity/工具/gen_draft_packs.py --write --emit-resources
                                                                       顺带落一份 Runtime 副本
                                                                       （`Assets/CardPresentation/Resources/draft_packs.json`）
    ... -h / --help                                                    只打用法、exit 0（不干活）

⚠️ 不带 `--write` = **干跑**（`A1648` 同族口径）：读数照打，**一个字节都不写**。

------------------------------------------------------------------
 原版字段（`PrebuiltPack`，7 个）→ 本产物
------------------------------------------------------------------
    packName    → packName
    packId      → packId          ⚠️ **不是唯一键**（见下「重复」）
    nameRefId   → （不落：882/882 全空，没有信息量）
    packArmy    → army（`CardArmy` 数值：10…130，`dump.cs:45634-45652`）
    packTier    → tier（`PackTier`：0 Common / 1 Epic / 2 Legendary，`dump.cs:23065-23072`）
    packEnabled → enabled（原文 0/1 → bool）
    cardIds     → cardIds（**恒 3 条**，882/882 实测）

🔴 **重复 packId**（2026-10-24 实测，27 组 / 54 个文件）：导出时同名 SO 被写成
   `<名>.json` + `<名>_<pathID>.json` 两份。其中 **26 组两份内容逐字相同**（纯导出重复）；
   **1 组不是** —— `TAU_EMYamoto` 同时是 `Darkstrider`（enabled=1 · TAU6/TAU12/TAU58）与
   `E’Myamoto`（enabled=**0** · TAU12/TAU21/TAU58）⇒ 原版自己就撞了键。
   本脚本**一条都不丢**（882 条全落），另给每条一个**唯一 `key`**：首次出现 = `packId`，
   之后 = `packId#2` / `packId#3` ……（顺序见下「排序」，**enabled 的排前面** ⇒ 正常轮抽
   （只要 enabled）拿到的是 `Darkstrider` 那条，禁用的那条只在 `enabledOnly=false` 时出现）。

排序（**字节稳定靠它**：两次 `--write` 必须逐字节相同）：
    (army, packId, 未启用在前? 即 enabled 优先, srcFile) —— 全部按 ordinal 比。

自检闸（任何一条不过 ⇒ 打红、**一个字节都不写**、exit 1）：
    · 每个 `packId` 非空、`packArmy` ∈ 那 13 个值、`packTier` ∈ {0,1,2}、`packEnabled` ∈ {0,1}
    · 每个包 `cardIds` **恰好 3 条**（不足/超出**如实报出来**，⛔ 不补齐、不截断）
    · 每个 `cardIds` 里的 id 都能在 `cards_engine.json` 里找到（缺的逐条报）

⛔ **自重包原样保留**（原版数据，不是脏数据）：
    `GOFF_RaininStormboyz [GOF6,GOF6,GOF20]` · `LEV_SynapticRelay [TL65,TL65,TL69]` ·
    `SAU_UnendingScourge [SAU69,SAU69,SAU53]` —— ⛔ 别在这里去重。
"""
import argparse
import json
import os
import sys

SRC = r"d:/2/新解包资源/assets_full/bundle_draftpacks_assets_all/MonoBehaviour"
SRC_BUNDLE = "draftpacks_assets_all.bundle"
DST = r"d:/4/Unity/数据/游戏数据/draft_packs.json"
# 只在 `--emit-resources` 时写：运行时 `DraftPacks.Load()` 走 `Resources.Load<TextAsset>("draft_packs")`
RES = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/draft_packs.json"
# 卡池（校验 cardIds 全命中用；⛔ 只读）
CARDS = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"

VERSION = 1

# `CardArmy` 枚举（`d:/2/tools/il2cpp_out/dump.cs:45634-45652`）。
# 🔴 这里只是**写进数据供人读 + 供 C# 侧交叉校验**；运行期唯一权威是
#    `CardPresentation/ArmyIcons.cs` 的 `ArmyIcons.Faction(army)`。
#    `DraftAssertD1` 有一条断言逐包比这两个，**任一处改了而另一处没改 ⇒ 红**。
ARMIES = [
    (0, "Neutral"),            # 数据里不该出现（882 个包全是 10…130）
    (10, "Ultramarines"),
    (20, "Goff"),
    (30, "SaimHann"),
    (40, "Sautekh"),
    (50, "BlackLegion"),
    (60, "Leviathan"),
    (70, "TauEmpire"),
    (80, "Sororitas"),
    (90, "Genestealers"),
    (100, "AstraMilitarum"),
    (110, "DarkAngels"),
    (120, "EmperorsChildren"),
    (130, "SpaceWolves"),
]
ARMY_NAME = dict(ARMIES)
PLAYABLE_ARMIES = [a for a, _ in ARMIES if a != 0]

TIERS = [(0, "Common"), (1, "Epic"), (2, "Legendary")]
TIER_NAME = dict(TIERS)

CARDS_PER_PACK = 3


def fail(msg):
    print("🔴 " + msg)
    return False


def load_src():
    """读 882 个 PrebuiltPack SO。返回 (rows, bad) —— bad = [(文件名, 原因)]。"""
    if not os.path.isdir(SRC):
        return [], [("<目录>", "源目录不存在：" + SRC)]
    names = sorted(n for n in os.listdir(SRC) if n.lower().endswith(".json"))
    rows, bad = [], []
    for n in names:
        p = os.path.join(SRC, n)
        try:
            with open(p, encoding="utf-8") as f:
                j = json.load(f)
        except Exception as e:
            bad.append((n, "JSON 读不了：" + str(e)))
            continue
        if "packId" not in j or "cardIds" not in j:
            bad.append((n, "不是 PrebuiltPack（没有 packId / cardIds）"))
            continue
        rows.append({
            "srcFile": n,
            "packId": j.get("packId") or "",
            "packName": j.get("packName") or "",
            "army": j.get("packArmy"),
            "tier": j.get("packTier"),
            "enabled": j.get("packEnabled"),
            "cardIds": j.get("cardIds") or [],
            "nameRefId": j.get("nameRefId") or "",
        })
    return rows, bad


def validate(rows):
    """逐包校验。返回 bad = [说明]。⛔ 不修数据、只报。"""
    bad = []
    for r in rows:
        n = r["srcFile"]
        if not r["packId"]:
            bad.append(f"{n}：packId 为空")
        if not r["packName"]:
            bad.append(f"{n}：packName 为空")
        if r["army"] not in PLAYABLE_ARMIES:
            bad.append(f"{n}：packArmy={r['army']!r} 不在 10…130 那 13 个阵营里")
        if r["tier"] not in TIER_NAME:
            bad.append(f"{n}：packTier={r['tier']!r} 不在 0/1/2")
        if r["enabled"] not in (0, 1):
            bad.append(f"{n}：packEnabled={r['enabled']!r} 不是 0/1")
        k = len(r["cardIds"])
        if k != CARDS_PER_PACK:
            bad.append(f"{n}：cardIds 有 {k} 条（原版恒 {CARDS_PER_PACK} 条）"
                       f" —— {r['cardIds']!r} ⇒ 如实报出、⛔ 不补齐不截断")
        for cid in r["cardIds"]:
            if not isinstance(cid, str) or not cid:
                bad.append(f"{n}：cardIds 里有一条不是非空字符串（{cid!r}）")
        if r["nameRefId"]:
            # 882/882 实测全空；哪天不空了 = 导出换了，要重新看语义
            bad.append(f"{n}：nameRefId 非空（{r['nameRefId']!r}）—— 本脚本按「恒空」写的，先核语义")
    return bad


def load_card_pool():
    with open(CARDS, encoding="utf-8") as f:
        d = json.load(f)
    return {c["id"] for c in d["cards"] if c.get("id")}, d.get("version"), d.get("count")


def build(rows):
    """→ (doc, report)。排序 + 发唯一 key + 汇总。"""
    # 排序：army ↑ → packId ↑ → enabled 优先 → srcFile ↑（全部 ordinal，字节稳定靠它）
    ordered = sorted(rows, key=lambda r: (r["army"], r["packId"], 0 if r["enabled"] == 1 else 1,
                                          r["srcFile"]))

    seen = {}
    packs = []
    for r in ordered:
        pid = r["packId"]
        seen[pid] = seen.get(pid, 0) + 1
        key = pid if seen[pid] == 1 else "%s#%d" % (pid, seen[pid])
        e = {
            "key": key,
            "packId": pid,
            "packName": r["packName"],
            "army": r["army"],
            "armyName": ARMY_NAME.get(r["army"], ""),
            "tier": r["tier"],
            "tierName": TIER_NAME.get(r["tier"], ""),
            "enabled": r["enabled"] == 1,
            "cardIds": list(r["cardIds"]),
            "srcFile": r["srcFile"],
        }
        if seen[pid] > 1:
            # 明确标出来：这是「同一个 packId 的第二份」。26 组内容相同、1 组不同（见文件头）。
            e["duplicate"] = True
        packs.append(e)

    army_counts = []
    for a in PLAYABLE_ARMIES:
        sub = [p for p in packs if p["army"] == a]
        army_counts.append({
            "army": a,
            "armyName": ARMY_NAME[a],
            "total": len(sub),
            "enabled": sum(1 for p in sub if p["enabled"]),
        })

    tier_counts = []
    for t, name in TIERS:
        sub = [p for p in packs if p["tier"] == t]
        tier_counts.append({"tier": t, "tierName": name,
                            "total": len(sub), "enabled": sum(1 for p in sub if p["enabled"])})

    unique_cards = set()
    for p in packs:
        unique_cards.update(p["cardIds"])

    doc = {
        "version": VERSION,
        "count": len(packs),
        "source": SRC_BUNDLE,
        "sourceDir": SRC,
        "note": ("原版 `PrebuiltPack` 逐包转写（`dump.cs:23093`）。每个恒定 3 张卡；"
                 "`enabled=false` 的包原版 `GetRandomPack` 不会抽到；"
                 "`duplicate=true` = 同一个 packId 的第二份（导出重复，26 组内容相同 / 1 组不同）。"
                 "⛔ 自重包（GOFF_RaininStormboyz / LEV_SynapticRelay / SAU_UnendingScourge）原样保留。"
                 "⚠️ `draftRounds` / `packsPerDraft` 不在本文件里 —— 原版由服务端下发、本地无真值。"),
        "armyCounts": army_counts,
        "tierCounts": tier_counts,
        "uniqueCardCount": len(unique_cards),
        "packs": packs,
    }
    report = {"uniqueCards": unique_cards, "armyCounts": army_counts, "tierCounts": tier_counts}
    return doc, report


def dump_doc(doc, path):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)
        f.write("\n")


def main():
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("-h", "--help", action="store_true", dest="help")
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    ap.add_argument("--write", action="store_true", help="显式落盘")
    ap.add_argument("--emit-resources", action="store_true", dest="emit_res",
                    help="顺带落一份 Assets/CardPresentation/Resources/draft_packs.json（运行期要读它）")
    args = ap.parse_args()
    if args.help:
        print(__doc__)
        return 0
    if args.check and args.write:
        ap.error("--check 与 --write 不能同时给（一个是只对账、一个是显式落盘）")

    print("读源目录 " + SRC)
    rows, bad = load_src()
    print("  PrebuiltPack 文件 %d 个，读坏的 %d 个" % (len(rows), len(bad)))
    for n, why in bad:
        print("    🔴 %s：%s" % (n, why))

    bad += validate(rows)

    pool, cv, cc = load_card_pool()
    missing = set()
    for r in rows:
        for cid in r["cardIds"]:
            if isinstance(cid, str) and cid and cid not in pool:
                missing.add(cid)
    print("卡池 `cards_engine.json` version=%s count=%s（%d 张）" % (cv, cc, len(pool)))
    if missing:
        for cid in sorted(missing):
            print("  🔴 包里的卡 id 在卡池里查不到：%s" % cid)
        bad.append("cardIds 有 %d 个 id 在 cards_engine.json 里查不到" % len(missing))

    if not rows:
        fail("一个包都没读到 —— 不落盘")
        return 1

    doc, rep = build(rows)

    # ---------------- 读数 ----------------
    n_all = doc["count"]
    n_en = sum(1 for p in doc["packs"] if p["enabled"])
    dups = {}
    for p in doc["packs"]:
        dups.setdefault(p["packId"], []).append(p)
    dup_groups = {k: v for k, v in dups.items() if len(v) > 1}
    identical = sum(1 for v in dup_groups.values()
                    if len({(tuple(p["cardIds"]), p["army"], p["tier"]) for p in v}) == 1)

    print("\n包总数 %d · enabled %d / disabled %d · 阵营 %d 个 · 唯一卡 id %d"
          % (n_all, n_en, n_all - n_en, len(rep["armyCounts"]), doc["uniqueCardCount"]))
    print("档位 " + " · ".join("%s(%d) %d" % (t["tierName"], t["tier"], t["total"])
                              for t in rep["tierCounts"]))
    print("阵营 " + " · ".join("%s %d/%d" % (a["armyName"], a["enabled"], a["total"])
                              for a in rep["armyCounts"]))
    print("每包卡数 " + " · ".join("%d 张 ×%d" % (k, sum(1 for p in doc["packs"] if len(p["cardIds"]) == k))
                                  for k in sorted({len(p["cardIds"]) for p in doc["packs"]})))
    print("重复 packId %d 组（内容全同 %d 组）" % (len(dup_groups), identical))
    for k, v in sorted(dup_groups.items()):
        if len({(tuple(p["cardIds"]), p["army"], p["tier"]) for p in v}) > 1:
            print("  🔴 内容不同的一组：%s" % k)
            for p in v:
                print("     %-28s enabled=%d %s" % (p["srcFile"], 1 if p["enabled"] else 0, p["cardIds"]))
    dis = [p for p in doc["packs"] if not p["enabled"]]
    print("禁用包 %d 个：%s" % (len(dis), ", ".join(p["packId"] for p in dis)))

    if bad:
        for b in bad:
            print("  🔴 " + b)
        fail("上面 %d 条没解决 —— 一个字节都不写" % len(bad))
        return 1

    # ---------------- --check ----------------
    if args.check:
        if os.path.exists(DST):
            with open(DST, encoding="utf-8") as f:
                old = json.load(f)
            same = (old.get("count") == doc["count"]
                    and old.get("version") == doc.get("version")
                    and old.get("packs") == doc["packs"]
                    and old.get("armyCounts") == doc.get("armyCounts")
                    and old.get("tierCounts") == doc.get("tierCounts")
                    and old.get("uniqueCardCount") == doc.get("uniqueCardCount"))
            print("\n--check：现有 %s %s" % (DST, "一致 ✅" if same else "**已过期** ❌（重跑时加 `--write` 落盘）"))
            return 0 if same else 1
        print("\n--check：%s 不存在" % DST)
        return 1

    if not args.write:
        print("\n干跑（未写盘）：要落盘请显式加 `--write` —— %s" % DST)
        return 0

    os.makedirs(os.path.dirname(DST), exist_ok=True)
    dump_doc(doc, DST)
    print("\n写出 %s  (%.0f KB)" % (DST, os.path.getsize(DST) / 1024))

    if args.emit_res:
        os.makedirs(os.path.dirname(RES), exist_ok=True)
        dump_doc(doc, RES)
        print("写出 %s  (%.0f KB)  ← 运行期 `DraftPacks.Load()` 读的就是这份"
              % (RES, os.path.getsize(RES) / 1024))
    else:
        print("ℹ️ 未写 Resources 副本（加 `--emit-resources` 才写）—— 运行期 `DraftPacks.Load()` 需要它")
    return 0


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main())
