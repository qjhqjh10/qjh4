# -*- coding: utf-8 -*-
"""gen_related_cards.py —— **原版「相关卡」(`relatedCard1..4`) 真值 → 运行时数据件**（账 `A1796`）

## 这是什么

原版每张卡 SO（`RawCardScript`）上带着四个字段 `relatedCard1..4`（偏移 `+0xE8/+0xF0/+0xF8/+0x100`），
卡牌详情窗 `CardDisplayWindow.InitializeRelatedCards` 把它们（最多 4 张、按 1→4 顺序、
**遇到第一个空就停**）摆成主卡右边那几格；引擎侧 `CardScript.ProcessCompanion`
（`Companion N: <部队名>` 那条链）用的也是**同一个 `relatedCard1`**。

- 方法体判据：`d:/2/tools/decomp_full/RawCardScript__GetRelatedCards.c`（四个引用原样塞进 List）
  · `CardDisplayWindow__InitializeRelatedCards.c`（逐张 `iVar4=1..` 摆，**第一个 null 就 break**）
  · `CardScript__ProcessCompanion.c:28`（`:28` 取 `rawCardData +0xE8`）
  · `CardScript__ShouldTriggerCompanion.c`（`HasCurrentTrait(0x49C)` **且** `relatedCard1 != null`，或 counter > 0）
- 签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:66-72`

## 真值从哪来

`d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json` —— **当前 build（2026-10-18）的
allcards 卡 SO 语料**：1,351 张 `RawCardScript`（纯 JSON，只读；116 个序列化字段在 `full_raw` 里）。
四槽是**同包内的裸 PPtr**（`m_FileID = 0`、`m_PathID` 非零才是真引用）
⇒ 在同一包的对象表里按 `m_PathID` 反查那一张卡，取它的 `uniqueId`（`path_id` 就是那张卡的键）。

🔴 **换源记录**：2026-10-18 之前这里读的是
`d:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/54bdb7b1…/…/__data`（UnityPy，**Unity 2022.3.38f1 /
缓存时间 2025-07-30 的旧快照**，1,195 张）⇒ 只出 196 张 / 240 条边。
当前包 `relatedCard1` 非空 = **212 张**、四槽边合计 **266 条**。⛔ **别改回旧快照**
（判据 → `资料/普查产出_1018/R_卡牌线_完整ID与SO后的调整评估.md` §4）。

⚠️ 当前包里仍有 **224 张卡不在我们池子**（计策 195 / 单位 19 / 英雄 10），其中含
`_EASY` / `_tutorial` / `_enchantment*` 那些**另形态** ⇒ 它们的边/源**照旧不猜、丢掉并打印**。

## 怎么把真值 id 落到我们池子

三级，**每一级都出声**（照 `gen_cards_engine.py::resolve_warlord_defence` 的先例）：

1. **原版 `uniqueId` 直接命中我们池子的 `id`** —— 主力那一档；
2. **归一化卡名 + 同阵营**唯一命中 —— 原版那批**非可收集卡**（`m_Name` 里带 `Z-NC` / `_enchantment` 前后缀、
   `cardShortId == 0`）在各阵营自己身上有唯一 id，而我们池子给它们发的是**自造 id**
   （`SAU_Lord_of_the_Storm` 这种），两端对不上 `uniqueId` ⇒ 只能按名字过桥。
   🔴 **两条硬约束**（少了任一条都会静默张冠李戴）：
   · **必须同阵营** —— 全池有同名不同阵营的组（`Terminator` EC22/UM82 等）；
   · **候选只许是「我们自造 id」的卡**（`id ∉ 真值的 uniqueId 全集`）—— 满池都算候选的话，
     原版那些 `..._enchantment2` **派生形态**（`cardName` 就是底盘卡的名字）会搭到**可收集底盘卡**上，
     等于把「另一张卡」画进相关卡格子里。加了这条以后，派生形态一律**认不出**（红线：宁可认不出）。
   🔴 **2026-10-18 现状**：K06 已把池内 id 全换成真 `uniqueId`（池 1127/1127 ∈ 当前真值集）
   ⇒ **候选集为空、这一级实际命中 0**。保留它只作防御（万一将来又出现自造 id）。
   阵营映射 = `数据/游戏数据/factions.json` 的 `id→name`，与 `cardArmy` **1,127/1,127 逐张吻合**（本脚本自检，见下）。
3. 三条都落空 ⇒ 那条边**丢掉**、记进 `skipped`（⛔ 不猜、不填占位）。

🔴 **没有「模糊匹配」那一档**（与 `resolve_warlord_defence` 不同，别照抄）。实测把它打开会**造出假边**：
`DA6 Sergeant Taaman` 匹到 `DA_Sergeant_Naaman`（0.93）、`DA2 Belial, Master of the Deathwing`
匹到 `DA_Master_of_the_Deathwing`（0.87）—— **都不是同一张卡**。那条先例的候选池是
「同阵营的防御卡」（每阵营 1~2 张），这里的候选池是**整个阵营**，比值高不代表是同一张。
⇒ 宁可认不出（红线），那两条近名的（`SAU38 Extermination Protocol(s)` · `GOF12 Spanner`/`Ork Spanner`）
列在报告里等裁定。

## 输出（**两份，同一次运行产出 ⇒ 不会分叉**；照 `gen_card_vfx_by_card.py` 的先例）

1. `d:/4/Unity/数据/游戏数据/related_cards.json` —— **全量真值（给工具/人看）**：
   原版 id / 原版卡名 / 落成我们哪个 id / 用的哪一级 / **落不下的那些也在里面**（`id: null` + 原因）。
   每条边一条不落地全在这份里（当前 build：266 条）⇒ 想核真值不必再解包。
2. `MyGame/Assets/CardPresentation/Resources/related_cards.json` —— **运行时件**（游戏读的就是它）：
   只收**两个端点都落进我们卡池**的边，键 = **我们池子的 id**。形状用**数组**
   —— `JsonUtility` 不支持字典。

```bash
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_related_cards.py
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_related_cards.py --check   # 只比不写
```
"""

import argparse
import difflib
import io
import json
import os
import re
import sys
from collections import defaultdict

# ---- 路径 ----------------------------------------------------------------
# 🔴 **真值源 = 当前 build 的 allcards 卡 SO 语料**（2026-10-18 换）。
#   ⛔ 旧路径 `d:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/54bdb7b1…/…/__data` 是
#   **2025-07-30 的旧快照**（UnityPy，1,195 张）—— **别改回去**。
#   判据 → `资料/普查产出_1018/R_卡牌线_完整ID与SO后的调整评估.md` §4。
CARDS_RAW = r"d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
FACTIONS = r"d:/4/Unity/数据/游戏数据/factions.json"
POOL = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
DST = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/related_cards.json"
TRUTH_DST = r"d:/4/Unity/数据/游戏数据/related_cards.json"

FUZZY_MIN = 0.80
SLOTS = (1, 2, 3, 4)


def norm(s):
    """归一化卡名 —— 只用于**过桥那一跳**，⛔ 不是判据本身。

    做三件事：① `’/‘` → `'`；② **砍掉尾部的括号注**（我们池子给非可收集卡起的名字带
    `(Zodgrod Wortsnagga Talent)` 这种后缀，原版不带）；③ 只留字母数字、小写。
    """
    s = str(s or "")
    s = s.replace("\u2019", "'").replace("\u2018", "'")
    s = re.sub(r"\s*\([^)]*\)\s*$", "", s)
    s = re.sub(r"[^0-9a-zA-Z]+", "", s).lower()
    return s


class _Ref:
    """假装成 UnityPy 的 PPtr —— 下游只用到 `.m_PathID`。"""
    __slots__ = ("m_PathID",)

    def __init__(self, pid):
        self.m_PathID = pid or 0


class _CardSo:
    """把 `cards_raw.json` 的一条卡 SO 适配成下游 `getattr` 期望的形状。

    ⚠️ 当前 build 的语料把 116 个原始序列化字段放在 **`full_raw`** 下（`relatedCard1..4` 在那一层）；
       这里两种形状都认，免得换语料时静默取空。
    """

    __slots__ = ("uniqueId", "cardName", "cardArmy", "m_Name",
                 "relatedCard1", "relatedCard2", "relatedCard3", "relatedCard4")

    def __init__(self, raw):
        fr = raw.get("full_raw") or {}
        self.uniqueId = raw.get("uniqueId") or ""
        self.cardName = raw.get("cardName") or ""
        self.cardArmy = raw.get("cardArmy")
        self.m_Name = raw.get("m_Name") or ""
        for i in SLOTS:
            p = fr.get("relatedCard%d" % i)
            if p is None:
                p = raw.get("relatedCard%d" % i)
            setattr(self, "relatedCard%d" % i, _Ref((p or {}).get("m_PathID")))


def load_truth():
    """→ `{path_id: _CardSo}`。同包内按 `m_PathID` 反查四槽目标就靠这张表。"""
    doc = json.load(io.open(CARDS_RAW, encoding="utf-8"))
    raw = doc["cards"] if isinstance(doc, dict) else doc
    by_path = {}
    for c in raw:
        by_path[c["path_id"]] = _CardSo(c)
    return by_path


def read_edges(by_path):
    """→ `[(源卡原文, 槽1..槽4 的【真值 uniqueId 或 None】)]`，槽序保持 1..4。

    原版 `InitializeRelatedCards` **遇到第一个 null 就 break**；实测 1,351 张里
    **没有一张是「空槽后面还有非空槽」**（本脚本自检，见 `holes`）⇒ 丢掉 null 之后的列表
    与原版显示的那几格逐格一致。
    """
    out = []
    holes = []
    for pid, d in sorted(by_path.items(), key=lambda kv: str(getattr(kv[1], "uniqueId", ""))):
        uid = getattr(d, "uniqueId", "") or ""
        vals, seen_null = [], False
        for i in SLOTS:
            rc = getattr(d, "relatedCard%d" % i, None)
            t = by_path.get(rc.m_PathID) if (rc is not None and rc.m_PathID != 0) else None
            if t is None:
                seen_null = True
                vals.append(None)
            else:
                if seen_null:
                    holes.append((uid, [v is not None for v in vals] + [True]))
                vals.append(getattr(t, "uniqueId", "") or "")
        if any(vals):
            out.append((d, vals))
    return out, holes


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只比不写")
    args = ap.parse_args()

    pool = json.load(io.open(POOL, encoding="utf-8"))
    cards = pool["cards"]
    by_id = {c["id"]: c for c in cards}

    fac = json.load(io.open(FACTIONS, encoding="utf-8"))
    army_name = {f["id"]: f["name"] for f in fac["factions"]}

    by_path = load_truth()
    truth = {}
    for d in by_path.values():
        u = getattr(d, "uniqueId", "") or ""
        if u:
            truth[u] = d

    # ---- 阵营映射自检（`cardArmy` ↔ 我们池子 `faction`）；不成立就别往下走 ----
    agree = dis = 0
    for u, d in truth.items():
        c = by_id.get(u)
        if c is None:
            continue
        if army_name.get(getattr(d, "cardArmy", None)) == c["faction"]:
            agree += 1
        else:
            dis += 1
    print(f"[阵营映射] cardArmy→factions.json 与卡池 faction：吻合 {agree} · 不符 {dis}")
    if dis:
        raise SystemExit("❌ 阵营映射不成立 ⇒ 名字过桥那一跳会张冠李戴，停手")

    # 归一化名 + 阵营 → 我们池子的 id。**候选只收「自造 id」的卡**（见文件头第 2 条）——
    # 满池当候选的话，原版的 `..._enchantment2` 派生形态会搭到可收集底盘卡上（那是另一张卡）。
    name_idx = defaultdict(list)
    for c in cards:
        if c["id"] in truth:
            continue
        name_idx[(norm(c["name"]), c["faction"])].append(c["id"])
    fac_pool = defaultdict(list)
    for c in cards:
        fac_pool[c["faction"]].append(c)

    stats = defaultdict(int)
    missed = []
    near = []

    def resolve(u):
        """真值 uniqueId → `(我们的 id | None, 用了哪一级)`。"""
        if u in by_id:
            return u, "id"
        d = truth.get(u)
        if d is None:
            return None, "真值里没有这条 uniqueId"
        cn = getattr(d, "cardName", "") or ""
        f = army_name.get(getattr(d, "cardArmy", None))
        hit = name_idx.get((norm(cn), f), [])
        if len(hit) == 1:
            return hit[0], "名字+阵营"
        if len(hit) > 1:
            return None, "名字+阵营撞了 %d 个" % len(hit)
        # 认不出 —— 顺手记一条「同阵营里最近的那个」**只给人看**（⛔ 不进产物）
        best = max(((difflib.SequenceMatcher(None, norm(cn), norm(c["name"])).ratio(), c)
                    for c in fac_pool.get(f, [])), key=lambda x: x[0], default=(0.0, None))
        near.append((u, cn, best[1]["id"] if best[1] else "?", best[1]["name"] if best[1] else "?", best[0]))
        return None, "查不到「%s」（阵营 %s）" % (cn, f)

    entries, skipped, holes_all = [], [], []
    edges_total = 0
    truth_edges, holes_all = read_edges(by_path)

    def form_rank(d):
        """真值里同一条卡在我们池子里落成同一个 id 时，**谁当代表**。
        `_EASY` / `_tutorial` / `_enchantment` 那几种形态**排在底盘后面** ——
        实测它们的四槽与底盘**逐条相同或为其子集**（`_EASY` = 简单 AI 的督军版、
        `_tutorial*` = 教程版、`_enchantment*` = 附魔形态），所以取底盘那条不会丢内容。"""
        nm = getattr(d, "m_Name", "") or ""
        return (1 if re.search(r"_(EASY|tutorial\d?|Tutorial|enchantment)", nm) else 0, nm)

    # ---- 第 1 趟：**每一条真值卡都解析一次**（源 + 四槽），两份产物共用这一趟的结果 ----
    #   ⛔ 别解析两遍：`resolve` 会往 `stats` / `near` 里记账，跑两遍数就重了。
    rows, groups = [], defaultdict(list)   # groups：我们的源 id → [row]
    for d, vals in truth_edges:
        src_raw = getattr(d, "uniqueId", "") or ""
        sid, show = resolve(src_raw)
        stats["源:" + show] += 1
        if sid is None:
            missed.append(("源", src_raw, getattr(d, "cardName", ""), getattr(d, "m_Name", ""), show))
        row = {"d": d, "uid": src_raw, "srcId": sid, "via": show, "targets": []}
        for u in vals:
            if u is None:
                continue
            tid, thow = resolve(u)
            stats["目标:" + thow.split(" ")[0]] += 1
            row["targets"].append((u, tid, thow))
        rows.append(row)
        if sid is not None:
            groups[sid].append(row)

    collided = [(sid, [getattr(r["d"], "m_Name", "") for r in sorted(grp, key=lambda r: form_rank(r["d"]))])
                for sid, grp in groups.items() if len(grp) > 1]

    # ---- 第 2 趟：按我们池子的源 id 归并 → **运行时件** ----
    entries, skipped = [], []
    for sid, grp in sorted(groups.items()):
        row = sorted(grp, key=lambda r: form_rank(r["d"]))[0]
        stats["源:同 id 多形态（只取代表）"] += len(grp) - 1
        got = []
        for u, tid, thow in row["targets"]:
            if tid is None:
                td = truth.get(u)
                skipped.append((sid, u, getattr(td, "cardName", "") if td else "?", thow))
                continue
            got.append(tid)
        if got:
            entries.append({"id": sid, "related": got})
        elif row["targets"]:
            skipped.append((sid, "-", "（四个槽一条都没落地）", "全部落空"))

    # ---- 全量真值那份（**每条边一条不落**，含落不下的；给工具/人看）----
    #   每个**代表行**一行（同 id 的多形态归并掉，与运行时件同一套归并规则）；
    #   `related` 逐槽保留：原版 id + 原版卡名 + 落成我们哪个 id + 用的哪一级。
    full = []
    for sid, grp in sorted(groups.items()):
        rep = sorted(grp, key=lambda r: form_rank(r["d"]))[0]
        d = rep["d"]
        full.append({
            "srcUid": rep["uid"], "srcName": getattr(d, "cardName", ""),
            "srcMName": getattr(d, "m_Name", ""), "srcId": sid,
            "related": [{"uid": u, "name": getattr(truth.get(u), "cardName", "?") if truth.get(u) else "?",
                         "id": tid, "via": thow} for u, tid, thow in rep["targets"]],
        })
    for m in missed:      # 源认不出的那些也进全量那份（`srcId: null`）—— 每条边一条不落
        row = next(r for r in rows if r["uid"] == m[1])
        full.append({
            "srcUid": row["uid"], "srcName": m[2], "srcMName": m[3], "srcId": None, "srcVia": m[4],
            "related": [{"uid": u, "name": getattr(truth.get(u), "cardName", "?") if truth.get(u) else "?",
                         "id": tid, "via": thow} for u, tid, thow in row["targets"]],
        })
    full.sort(key=lambda r: str(r["srcUid"]))
    seen_src = set()
    for e in entries:
        if e["id"] in seen_src:
            raise SystemExit("❌ 源 id 撞了：%s —— 归并逻辑有洞" % e["id"])
        seen_src.add(e["id"])

    entries.sort(key=lambda e: e["id"])
    n_cards = len(entries)
    n_edges = sum(len(e["related"]) for e in entries)

    slot = [0, 0, 0, 0]
    for _, vals in truth_edges:
        for i, v in enumerate(vals):
            if v is not None:
                slot[i] += 1
    print(f"[真值现读] 有四槽非空的卡（真值侧）: {len(truth_edges)} · 边 {sum(slot)} · "
          f"逐槽 {slot[0]}/{slot[1]}/{slot[2]}/{slot[3]}")
    print(f"[落盘·运行时件] 卡 {n_cards} · 边 {n_edges} · 丢掉 {sum(slot) - n_edges} 条"
          f"（源认不出 {sum(len(r['targets']) for r in rows if r['srcId'] is None)} 条 + "
          f"目标认不出 {sum(1 for s in skipped if s[1] != '-')} 条）")
    print(f"[落盘·全量真值] 卡 {len(full)}（含源认不出的）· 边 {sum(len(r['related']) for r in full)}")
    for k in sorted(stats):
        print("   ", k, stats[k])
    print("[模糊那一档] 已关掉（见文件头）——认不出的那些，同阵营里最近的名字（**只给人看，⛔ 没进产物**）：")
    for u, cn, cid, cname, r in sorted(set(near), key=lambda x: -x[4]):
        print(f"    {u} 「{cn}」 ~ {cid}「{cname}」 比值 {r:.2f}")
    print("[丢掉] %d 条边：" % len(skipped))
    for s in skipped:
        print("    ", " · ".join(map(str, s)))
    print("[丢掉] %d 张【源卡】认不出（它们的边一条都没落地）：" % len(missed))
    for m in missed:
        nm = m[3]
        tag = ("  ⚠️形态卡（`_EASY`/`_tutorial`/`_enchantment`）"
               if re.search(r"_(EASY|tutorial\d?|Tutorial|enchantment)", nm) else "")
        print("    ", " · ".join(map(str, m[:4])), "|", nm, tag)
    print("[自检] 真值里「空槽后面还有非空槽」的卡: %d （必须 0 —— 否则原版会提前 break，我们的列表会多出格子）"
          % len(holes_all))
    print("[同池重复形态] %d 组（同一条真值底盘卡在我们池子里落成同一个 id ⇒ 只取一条）：" % len(collided))
    for raw, ms in collided:
        print("    ", raw, "→", " / ".join(ms))

    doc = {
        "note": "原版卡 SO 的 relatedCard1..4（卡牌详情窗那几格 + Companion 词条要造的那张）—— "
                "键 = 我们卡池的 id，值 = 相关卡 id（按槽 1..4 顺序，最多 4；缺键 = 这张卡没有相关卡）。"
                "由 工具/gen_related_cards.py 生成，⛔ 不手改。",
        "source": "d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json（当前 build allcards，"
                  "RawCardScript × 1351）",
        "provenance": "同包内按 m_PathID 反查裸 PPtr（m_FileID 全 0）；语料 = 当前 build（2026-10-18 换，"
                      "旧的是 2025-07-30 的 1,195 张快照）。池外仍有约 224 张原版卡"
                      "（含 `_EASY`/`_tutorial`/`_enchantment` 那些另形态）⇒ 那些边**没猜、丢在 skipped 里**。"
                      "落 id 两级：① 原版 uniqueId 直命中卡池 id（当前池 1127/1127 都是真 uniqueId，命中率见 stats）；"
                      "② 归一化卡名（砍尾部括号）+ 同阵营、且候选只限「池内 id 不在真值集里」的卡、唯一命中"
                      "（当前 build 下池内 id 全在真值集 ⇒ 这一级实际命中 0，仅作防御）。"
                      "全量真值（含落不下的每一条）→ 数据/游戏数据/related_cards.json，同一次运行产出。",
        "invariant": "① entries 里每个 related 至多 4 条、顺序 = relatedCard1..4；② 同一卡内无重复；"
                     "③ 源/目标 id 一律是 cards_engine.json 里现用的 id；④ 真值里没有「空槽后还有非空槽」的卡。",
        "count": n_cards,
        "edges": n_edges,
        "skipped": len(skipped),
        "entries": entries,
    }
    full_doc = {
        "note": "**全量真值**（给工具/人看）：原版每张卡的 relatedCard1..4 逐槽一条不落，"
                "含「落不进我们卡池」的那些（`id: null` + `via` 写原因）。游戏读的是"
                " CardPresentation/Resources/related_cards.json（同一次运行产出，不会分叉）。",
        "source": doc["source"],
        "provenance": doc["provenance"],
        "invariant": "① 每个 related 的 `uid` 是原版 `uniqueId`；② `id` 非 null 时它一定是"
                     " cards_engine.json 里现用的 id；③ 槽序 = relatedCard1..4。",
        "truthCards": len(truth_edges),
        "truthEdges": sum(slot),
        "perSlot": slot,
        "mappedCards": n_cards,
        "mappedEdges": n_edges,
        "cards": full,
    }
    text = json.dumps(doc, ensure_ascii=False, indent=1)
    full_text = json.dumps(full_doc, ensure_ascii=False, indent=1)
    if args.check:
        ok = True
        for p, t in ((DST, text), (TRUTH_DST, full_text)):
            old = io.open(p, encoding="utf-8").read() if os.path.exists(p) else ""
            same = old == t
            ok = ok and same
            print("[--check] %s 与盘上那份%s" % (p, "一致 ✅" if same else "**不同** ❌"))
        return 0 if ok else 1
    # ⚠️ 先算进变量、确认没错再写（铁律：`io.open(p,'wb').write(<表达式>)` 会先截断再求值）
    for p, t in ((TRUTH_DST, full_text), (DST, text)):
        io.open(p, "w", encoding="utf-8", newline="\n").write(t)
        print("[写盘]", p, len(t.encode("utf-8")), "字节")
    return 0


if __name__ == "__main__":
    sys.exit(main())
