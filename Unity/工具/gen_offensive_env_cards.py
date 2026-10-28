#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_offensive_env_cards.py —— **进攻卡（环境效果卡）第二张表**的生成器。

产物：`MyGame/Assets/RuleEngine/Resources/offensive_env_cards.json`
      → 由 `RuleEngine/Data/CardDatabase.cs` 的 `EnsureOffensiveEnvFrom` / `OffensiveEnvCards` 读。

==================================================================================================
★ 为什么单开第二张表（而不是并进 `cards_engine.json`）—— 调度台 2026-10-28 裁定「方案乙」
==================================================================================================
原版判据说这 39 张**不是组牌池成员**：
  · `inventoryOptions = 5`（`CardInventoryOptions.CantAddToDeck`）39/39
  · `showInCollectionOptions = 0`（`ShowInCollectionOptions.Hide`）39/39
    （`DeckUtility__ValidateDeckCards.c:68-77`：`inv != 0` ⇒ 非法；`show != 5` ⇒ 非法）
⇒ 把它们塞进「组牌池」`cards_engine.json` 的语义本身就是错位，而且代价是
  `count` 1129→1168、`PinnedPoolMd5` / `PinnedPlayMd5` 必变、`PinnedParseMd5` /
  `PinnedBaselineMd5` 看 `desc` 落不落（`Scan.cs:196` 的 `if (string.IsNullOrEmpty(desc)) continue;`）、
  外加 `RuleEngineTest_EngineE1.cs:80` 那条硬编码张数断言要改。
**同一天的明文先例**（`gen_cards_engine.py:4536-4546`）：附魔模板也**不放宽入池闸**，
做法 = 单开 `enchantment_templates.json` + `CardDatabase.LoadEnchantmentTemplates()`。
本表照抄那条形状。

==================================================================================================
★ 收录范围 = **39 张**（13 阵营 × 3），⛔ 不是 52、也不是 91
==================================================================================================
`cards_truth.json` 里 `spellType == 200` 共 **91** 行，其中
  · **39 行 = 短 id 卡**（`AM63` / `ASH59` 这种，`cardType 20` ∧ `rarity 5`）← **本表收录这一批**
  · **52 行 = 环境效果模板**（32 位 hex id，`m_Name` 尾 `_enchantment`）← **不是卡，⛔ 不收**
      （它们才是「环境生效期间」的真正效果体；已在 `enchantment_templates.json` 里，85 张那份）
  · ⛔ **另 13 张 `Normal Conditions (X)` 空卡**（`OffensiveCards.json` 的 `emptyOffensiveCard`）**也不收**：
      名字带阵营括号 ⇒ 按名查必不中；而且空卡槽（`idx = -1`）本来就不触发任何 ability。

==================================================================================================
★ 三张表怎么 join（**这是本脚本存在的意义** —— 名字对不上 6 处，按名 join 必错）
==================================================================================================
① **槽位 ↔ 卡 id**（决定性判据，**不用卡名**）：
     `数据/游戏数据/offensive_cards.json` 的 `armies[<阵营>].cards[].cardRef.m_PathID`
     → `资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `path_id` 表
     → 该卡的 `uniqueId`（= 我们的 `CardDef.Id`）
   现读 **39/39 命中、0 歧义**（每个 pathID 只解出一个 uniqueId）。
   ⚠️ 这比 `B93_Q7` 施工单里「按名对」强：按名对时 `EC62` / `EC63`（原版 SO 名是占位名
   `1 Green` / `2 Fumes`）与 `SOR63`（SO 名少一个 `t`）**对不上**，只能标「未核」。

② **能力树**：`cards_truth.json` 的 `abilities`（原样带 —— 逐叶字段一个不丢，见 `_ability_rows`）。

③ **`buff.enchantment.m_PathID` → 附魔模板 id**：同一张 `path_id` 表；
   现读 **41/41 命中（39 张卡共 41 条 logic 行 → 37 个不同模板）**，且 **37/37 都在
   `enchantment_templates.json`（v1 · 85 张）里** —— 也就是说「环境卡挂哪张附魔」这条链
   **数据侧已经闭合**，缺的只是「谁来跑 `chosenAbility`」（见报告 §4）。

④ **官方显示名 / 正文**：`数据/游戏数据/官方本地化_en+zh-CN.json` 的 `Card_Name/<id>` /
   `Card_Description/<id>`（有 `<id>_new` 时新代赢，与 `gen_cards_engine.py:load_official_desc` 同口径）。
   现读 **39/39 全命中（en + zh 都有）**。

==================================================================================================
★ 缺值口径（⛔ 别把「没有这个键」读成 0）
==================================================================================================
· `targetsAffected` / `friendlyUnits` / `enemyUnits` / `includeHero` / `includeMinions` /
  `includeSelf` / `manaBuff.*` —— **缺键 = `-1`**（0 是合法取值：`targetsAffected 0` = `any`）。
  现读缺 `targetsAffected` 的 4 行：`AM65` 第二条 logic · `ASH59` · `SOR62` · `SW63`。
· `playersAffected` 缺 5 行（`SOR62` / `SW63` 以及 GOF50/51/54 之外那几条 `chosenAbility 40/60`）。
  同样是 `-1`。
· `buffType` 现读 **12（38 行）/ 15（2 行，都在 `SW62 First Light`）**。

用法（⛔ **不带参数 = 打印用法并退出，一个字节都不写** —— 本仓有过「脚本不带参就真合并」的事故）：
    python -I -X utf8 d:/4/Unity/工具/gen_offensive_env_cards.py --check   # 只读：报告覆盖率 / 与盘上产物比对
    python -I -X utf8 d:/4/Unity/工具/gen_offensive_env_cards.py --write   # 写盘（幂等）
"""
import io
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
MYGAME = os.path.join(ROOT, "MyGame")
OUT_REL = "MyGame/Assets/RuleEngine/Resources/offensive_env_cards.json"
OUT = os.path.join(ROOT, OUT_REL)

SRC_TRUTH = os.path.join(ROOT, "数据", "游戏数据", "cards_truth.json")
SRC_RAW = os.path.join(ROOT, "资料", "普查产出_1018", "allcards_1350", "cards_raw.json")
SRC_OFFENSIVE_RAW = os.path.join(ROOT, "数据", "游戏数据", "offensive_cards.json")
SRC_OFFENSIVE_RES = os.path.join(MYGAME, "Assets", "CardPresentation", "Resources", "OffensiveCards.json")
SRC_LOC = os.path.join(ROOT, "数据", "游戏数据", "官方本地化_en+zh-CN.json")
SRC_ENCH_TEMPLATES = os.path.join(MYGAME, "Assets", "RuleEngine", "Resources", "enchantment_templates.json")

VERSION = 1
GUID_RE = re.compile(r"^[0-9a-f]{32}$")

USAGE = """gen_offensive_env_cards.py —— 进攻卡（环境效果卡）第二张表生成器

用法（**必须显式给动作参数**；不带参数只打印这段，不写盘）：
  python -I -X utf8 d:/4/Unity/工具/gen_offensive_env_cards.py --check
        只读：跑完整条流水线、打印覆盖率与不一致，并把结果与盘上产物逐字节比对（不相同 ⇒ rc=1）
  python -I -X utf8 d:/4/Unity/工具/gen_offensive_env_cards.py --write
        生成 / 覆盖 %s

判据 / 口径 一律看本脚本头部注释（⛔ 别在别处抄第二份）。""" % OUT_REL


def _die(msg):
    sys.stderr.write("[FAIL] %s\n" % msg)
    raise SystemExit(1)


def load_json(path, what):
    if not os.path.exists(path):
        _die("找不到 %s：%s" % (what, path))
    with io.open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def norm_space(s):
    return " ".join((s or "").split())


def official_name(terms):
    """`Card_Name/<id>` → {id: (en, zh)}（官方表里 en==zh 的压成了裸字符串 ⇒ 原样两列都给英文）。"""
    out = {}
    for k, v in terms.items():
        if not k.startswith("Card_Name/"):
            continue
        _id = k[len("Card_Name/"):]
        if isinstance(v, list):
            en = (v[0] if len(v) > 0 else "") or ""
            zh = (v[1] if len(v) > 1 else "") or ""
        else:
            en = zh = v or ""
        en, zh = en.strip(), zh.strip()
        if not en:
            continue
        out[_id] = (en, zh or en)
    return out


def official_desc(terms):
    """`Card_Description/<id>` → {id: (en, zh)}；`_new` 新代覆盖旧代（与 load_official_desc 同口径）。

    ⚠️ 与 `gen_cards_engine.py:load_official_desc` 的**唯一区别**：那个只留中文（它的用途是 `descZh`），
    本表**英文也要**（`desc` 列 —— 环境卡的正文不在 `card_stats.json` 的 OCR 里，只能取官方表）。
    """
    out = {}
    for k, v in terms.items():
        if not k.startswith("Card_Description/"):
            continue
        _id = k[len("Card_Description/"):]
        is_new = _id.endswith("_new")
        if is_new:
            _id = _id[:-4]
        if isinstance(v, str):
            # en == zh 的单值（原表里被压成单串）⇒ 英文有、中文没有：英文照收，中文留空（⛔ 不拿英文顶中文）
            out[_id] = (v.strip(), "")
            continue
        en = (v[0] if len(v) > 0 else "") or ""
        zh = (v[1] if len(v) > 1 else "") or ""
        if is_new or _id not in out:
            out[_id] = (en.strip(), zh.strip())
    return out


def _ability_rows(abilities, warn):
    """把 `cards_truth.json` 的 `abilities` 树**逐叶**摊平成 JsonUtility 友好的行（⛔ 一个字段都不丢）。

    只在**出现时才**写哨兵 `-1`：本函数先把整行填 `-1`，再把真值覆盖上去 ⇒
    「键缺」与「值 0」在产物里分得开（`0` 是合法值，例 `targetsAffected 0 = any`）。
    """
    KNOWN_TOP = {"trigger", "logic"}
    KNOWN_LOGIC = {"chosenAbility", "playersAffected", "targetCriteria", "buff", "manaBuff"}
    KNOWN_TC = {"targetsAffected", "friendlyUnits", "enemyUnits", "includeHero",
                "includeMinions", "includeSelf"}
    KNOWN_BUFF = {"buffType", "enchantment"}
    KNOWN_MANA = {"manaAmount", "friendlyPlayer", "enemyPlayer", "createMana"}
    rows = []
    for ab in abilities or []:
        extra = set(ab.keys()) - KNOWN_TOP
        if extra:
            warn("abilities[] 出现未收录的键 %s ⇒ **会丢数据**，先扩 DTO/Doc 再跑" % sorted(extra))
        r = {"trigger": ab.get("trigger", -1), "logic": []}
        for lg in ab.get("logic") or []:
            extra = set(lg.keys()) - KNOWN_LOGIC
            if extra:
                warn("abilityLogic 出现未收录的键 %s ⇒ **会丢数据**" % sorted(extra))
            row = {
                "chosenAbility": lg.get("chosenAbility", -1),
                "playersAffected": lg.get("playersAffected", -1),
                "targetsAffected": -1, "friendlyUnits": -1, "enemyUnits": -1,
                "includeHero": -1, "includeMinions": -1, "includeSelf": -1,
                "buffType": -1, "enchantmentId": "", "enchantmentPathId": 0,
                "manaAmount": -1, "manaFriendlyPlayer": -1,
                "manaEnemyPlayer": -1, "manaCreateMana": -1,
            }
            tc = lg.get("targetCriteria") or {}
            extra = set(tc.keys()) - KNOWN_TC
            if extra:
                warn("targetCriteria 出现未收录的键 %s ⇒ **会丢数据**" % sorted(extra))
            for k in KNOWN_TC:
                if k in tc:
                    row[k] = tc[k]
            buff = lg.get("buff") or {}
            extra = set(buff.keys()) - KNOWN_BUFF
            if extra:
                warn("buff 出现未收录的键 %s ⇒ **会丢数据**" % sorted(extra))
            if "buffType" in buff:
                row["buffType"] = buff["buffType"]
            ench = buff.get("enchantment") or {}
            if "m_PathID" in ench:
                row["enchantmentPathId"] = int(ench["m_PathID"])
            mana = lg.get("manaBuff") or {}
            extra = set(mana.keys()) - KNOWN_MANA
            if extra:
                warn("manaBuff 出现未收录的键 %s ⇒ **会丢数据**" % sorted(extra))
            MANA_COL = {"manaAmount": "manaAmount", "friendlyPlayer": "manaFriendlyPlayer",
                        "enemyPlayer": "manaEnemyPlayer", "createMana": "manaCreateMana"}
            for k in KNOWN_MANA:
                if k in mana:
                    row[MANA_COL[k]] = mana[k]
            r["logic"].append(row)
        rows.append(r)
    return rows


def build(verbose=True):
    """跑完整条流水线 → (产物 dict, 统计 dict)。任何前提不成立就 `_die`（⛔ 不静默降级）。"""
    problems = []

    def warn(msg):
        problems.append(msg)
        if verbose:
            sys.stderr.write("[WARN] %s\n" % msg)

    truth = load_json(SRC_TRUTH, "cards_truth.json")
    cards = truth.get("cards") if isinstance(truth, dict) else None
    if not cards:
        _die("cards_truth.json 顶层是对象、卡在 `cards` 键里 —— 直接当 list 迭代会**静默扫到 0 张**")

    raw = load_json(SRC_RAW, "cards_raw.json")
    by_pathid = {}
    for c in raw["cards"]:
        pid = c.get("path_id")
        if pid is None:
            continue
        uid = c.get("uniqueId")
        if pid in by_pathid and by_pathid[pid] != uid:
            _die("`cards_raw.json` 的 path_id %s 解出两个 uniqueId（%s / %s）—— 该 join 不再无歧义"
                 % (pid, by_pathid[pid], uid))
        by_pathid[pid] = uid
    by_uid_raw = {c.get("uniqueId"): c for c in raw["cards"]}

    terms = load_json(SRC_LOC, "官方本地化_en+zh-CN.json")["terms"]
    off_name = official_name(terms)
    off_desc = official_desc(terms)

    ench_tpl_ids = set()
    if os.path.exists(SRC_ENCH_TEMPLATES):
        ench_tpl_ids = set(t["id"] for t in
                           load_json(SRC_ENCH_TEMPLATES, "enchantment_templates.json")["templates"])

    # ---- ① 39 张进攻卡（短 id）----
    env = [c for c in cards
           if c.get("spellType") == 200 and not GUID_RE.match(str(c.get("id", "")))]
    env = sorted(env, key=lambda c: c["id"])
    if len(env) != 39:
        _die("短 id 的 `spellType == 200` 行数 = %d，判据是 **39**（13 阵营 × 3）—— 数据换了就重核判据"
             % len(env))
    for c in env:
        if c.get("cardType") != 20:
            _die("%s：`cardType` = %r，判据是 20（Tactic）" % (c["id"], c.get("cardType")))
        if c.get("rarity") != 5:
            _die("%s：`cardRarity` = %r，判据是 5" % (c["id"], c.get("rarity")))
    env_by_id = {c["id"]: c for c in env}

    # ---- ② 槽位（offensive_cards.json 的 cardRef.m_PathID）----
    off_raw = load_json(SRC_OFFENSIVE_RAW, "offensive_cards.json")["armies"]
    slot_rows = {}          # (army_name, slot) -> dict
    id_to_slot = {}         # id -> (army_name, slot)
    for army_name, a in off_raw.items():
        for c in a.get("cards") or []:
            slot = c.get("slot")
            pid = ((c.get("cardRef") or {}).get("m_PathID"))
            uid = by_pathid.get(pid)
            if uid is None:
                _die("%s slot %s：`cardRef.m_PathID` = %r 在 cards_raw.json 的 path_id 表里**解不出来**"
                     % (army_name, slot, pid))
            if uid not in env_by_id:
                _die("%s slot %s 解出的 id `%s` **不在**那 39 张里（spellType/cardType 对不上）"
                     % (army_name, slot, uid))
            if uid in id_to_slot:
                _die("id `%s` 被两个槽引用（%r / %r）—— 该 join 不再一对一"
                     % (uid, id_to_slot[uid], (army_name, slot)))
            id_to_slot[uid] = (army_name, slot)
            slot_rows[(army_name, slot)] = c
    if len(id_to_slot) != 39:
        _die("槽位 join 覆盖 %d 张，判据是 **39**" % len(id_to_slot))

    # ---- ③ 运行时那份 Resources/OffensiveCards.json（`idx = slot - 1`；卡名以它为准）----
    off_res = load_json(SRC_OFFENSIVE_RES, "Resources/OffensiveCards.json")["armies"]
    res_name_by = {}
    for a in off_res:
        for c in a.get("cards") or []:
            res_name_by[(a["army"], c["idx"])] = c.get("name")
    if len(res_name_by) != 39:
        _die("Resources/OffensiveCards.json 的卡槽数 = %d，判据是 **39**（13×3）" % len(res_name_by))

    rows = []
    name_mismatch = []
    for uid in sorted(id_to_slot):
        army_name, slot = id_to_slot[uid]
        truth_card = env_by_id[uid]
        raw_card = by_uid_raw.get(uid) or {}
        srow = slot_rows[(army_name, slot)]
        res_name = res_name_by.get((army_name, slot - 1))
        if res_name is None:
            _die("%s：Resources/OffensiveCards.json 里 (%s, idx=%d) **没有槽**"
                 % (uid, army_name, slot - 1))
        cand = srow.get("cardName_candidate")
        if norm_space(res_name) != norm_space(cand):
            _die("%s：运行时卡名 %r ≠ offensive_cards.json 的 cardName_candidate %r"
                 "（两张表漂了，先查 gen_offensive_cards_flat.py）" % (uid, res_name, cand))

        if uid not in off_name or uid not in off_desc:
            _die("%s：官方本地化 `Card_Name/%s` 或 `Card_Description/%s` **没命中**" % (uid, uid, uid))
        oen, ozh = off_name[uid]
        den, dzh = off_desc[uid]
        if not den:
            _die("%s：官方 `Card_Description/%s` 的**英文列为空**" % (uid, uid))
        if not dzh:
            _die("%s：官方 `Card_Description/%s` 的**中文列为空**（单值 en==zh 那一档）" % (uid, uid))

        abilities = _ability_rows(truth_card.get("abilities"), warn)
        if len(abilities) != 1:
            _die("%s：`cardAbilities` = %d 条，判据是 **1**" % (uid, len(abilities)))
        for lg in abilities[0]["logic"]:
            tid = by_pathid.get(lg["enchantmentPathId"])
            if tid is None:
                _die("%s：附魔 PPtr %s 解不出模板 id" % (uid, lg["enchantmentPathId"]))
            lg["enchantmentId"] = tid
            if ench_tpl_ids and tid not in ench_tpl_ids:
                _die("%s：解出的模板 `%s` **不在** enchantment_templates.json（85 张）里" % (uid, tid))
        if raw_card.get("inventoryOptions") != 5:
            _die("%s：`inventoryOptions` = %r，判据是 5（CantAddToDeck）"
                 % (uid, raw_card.get("inventoryOptions")))
        if raw_card.get("showInCollectionOptions") != 0:
            _die("%s：`showInCollectionOptions` = %r，判据是 0（Hide）"
                 % (uid, raw_card.get("showInCollectionOptions")))

        match = 0 if norm_space(res_name) != norm_space(oen) else 1
        if not match:
            name_mismatch.append((uid, res_name, oen))
        rows.append({
            "id": uid,
            "name": oen,
            "nameZh": ozh,
            "nameTruth": truth_card.get("name"),
            "offensiveCardName": res_name,
            "nameMatchOfficial": match,
            "faction": army_name,
            "army": truth_card.get("army"),
            "slot": slot,
            "idx": slot - 1,
            "forgeLevel": srow.get("forgeLevel"),
            "envSO": srow.get("envSO_name"),
            "desc": den,
            "descZh": dzh,
            "cardType": truth_card.get("cardType"),
            "spellType": truth_card.get("spellType"),
            "rarity": truth_card.get("rarity"),
            "inventoryOptions": raw_card.get("inventoryOptions"),
            "showInCollectionOptions": raw_card.get("showInCollectionOptions"),
            "cantBeRandomlyCreated": raw_card.get("cantBeRandomlyCreated", 1),
            "abilityTriggers": sorted(set(int(a["trigger"]) for a in abilities)),
            "abilities": abilities,
        })

    if len(name_mismatch) != 6:
        _die("「运行时卡名 ≠ 官方显示名」的条数 = %d，判据是 **6**（判据 → "
             "判据/B93_Q7_数据侧三件施工单.md §顺手发现①）%r" % (len(name_mismatch), name_mismatch))

    rows.sort(key=lambda r: (r["army"], r["slot"]))

    note = (
        "进攻卡（环境效果卡）第二张表 · v%d —— ⛔ **不进** `cards_engine.json` 的 `cards[]`、"
        "不改它的 `count`（那 39 张原版判据是 `inventoryOptions 5 (CantAddToDeck)` ∧ "
        "`showInCollectionOptions 0 (Hide)` ⇒ **不是组牌池成员**）。"
        "收录 = `cards_truth.json` 里 `spellType == 200` 且**短 id** 的 39 行（13 阵营 × 3）；"
        "⛔ 不含 52 条环境效果模板（32 位 hex id，已在 `enchantment_templates.json`）、"
        "⛔ 不含 13 张 `Normal Conditions` 空卡。"
        "join 判据 = `数据/游戏数据/offensive_cards.json` 的 `cardRef.m_PathID` → "
        "`普查产出_1018/allcards_1350/cards_raw.json` 的 `path_id` → `uniqueId`（39/39、0 歧义，**不用卡名**）；"
        "`buff.enchantment.m_PathID` 走同一张表（41/41 → 37 个模板，全部已在 `enchantment_templates.json`）。"
        "卡名 / 正文 = 官方本地化 `Card_Name/<id>` / `Card_Description/<id>`（en+zh，39/39）。"
        "⚠️ `offensiveCardName` 是**运行时**（`Resources/OffensiveCards.json`）看到的卡名 —— 有 **6 处**与官方显示名"
        "对不上（`nameMatchOfficial 0`）：那是 `gen_offensive_cards_flat.py` 从**贴图名 / SO 占位名**反推的，"
        "⛔ **别拿卡名去 join 这两张表**，用 `faction + idx` 或 `id`。"
        "缺值口径：`targetsAffected` / `friendlyUnits` / `enemyUnits` / `includeHero` / `includeMinions` / "
        "`includeSelf` / `playersAffected` / `mana*` 的 **`-1` = 原真值里没有这个键**（`0` 是合法值，"
        "例 `targetsAffected 0 = any`）；`enchantmentPathId` 是原版 PPtr 原值、`enchantmentId` 是解出的模板 id。"
        "⚠️ **本表只落数据，今天没有任何消费者跑它** —— 要让 39 张真正生效，"
        "`BattleDriver.offensiveCardEnabled` / `ChosenOffensiveCardDef` / `chosenAbility` 分派口三处一起改"
        "（清单 → `资料/完全复刻/报告/B93_E16.md` §4）。"
    ) % VERSION

    out = {
        "version": VERSION,
        "count": len(rows),
        "source": "cards_truth.json（spellType==200 短 id）+ offensive_cards.json（cardRef.m_PathID → "
                  "cards_raw.json 的 path_id → uniqueId）+ Resources/OffensiveCards.json（运行时卡名 / idx）"
                  "+ 官方本地化_en+zh-CN.json（Card_Name/* · Card_Description/*）",
        "note": note,
        "cards": rows,
    }

    stats = {
        "count": len(rows),
        "name_mismatch": name_mismatch,
        "logic_rows": sum(len(a["logic"]) for r in rows for a in r["abilities"]),
        "distinct_templates": len(set(lg["enchantmentId"] for r in rows for a in r["abilities"] for lg in a["logic"])),
        "buffTypes": sorted(set(lg["buffType"] for r in rows for a in r["abilities"] for lg in a["logic"])),
        "factions": len(set(r["faction"] for r in rows)),
        "problems": problems,
    }
    return out, stats


def render(out):
    # 与同目录的 `cards_engine.json` / `enchantment_templates.json` 同形：**紧凑单行 + 裸 UTF-8 + 无行尾换行**
    return json.dumps(out, ensure_ascii=False, separators=(",", ":"))


def main(argv):
    if len(argv) != 2 or argv[1] not in ("--check", "--write"):
        print(USAGE)
        return 0 if len(argv) == 1 else 2
    action = argv[1]

    out, st = build()
    text = render(out)
    data = text.encode("utf-8")

    print("收录 %d 张 / %d 阵营 / %d 条 ability-logic 行 / %d 个不同附魔模板 / buffType=%r"
          % (st["count"], st["factions"], st["logic_rows"], st["distinct_templates"], st["buffTypes"]))
    print("运行时卡名 ≠ 官方显示名 %d 处：" % len(st["name_mismatch"]))
    for uid, legacy, official in st["name_mismatch"]:
        print("   · %-6s 运行时「%s」 → 官方「%s」" % (uid, legacy, official))
    if st["problems"]:
        print("⚠️ 有 %d 条 WARN（见上）" % len(st["problems"]))

    if action == "--check":
        if not os.path.exists(OUT):
            print("[CHECK] 盘上没有 %s ⇒ 需要 --write" % OUT_REL)
            return 1
        with io.open(OUT, "rb") as f:
            on_disk = f.read()
        if on_disk == data:
            print("[CHECK] OK —— 盘上产物与本次生成**逐字节相同**（%d 字节）" % len(data))
            return 0
        print("[CHECK] ⛔ 盘上产物与本次生成**不同**（盘上 %d 字节 / 本次 %d 字节）⇒ 跑 --write"
              % (len(on_disk), len(data)))
        return 1

    # --write
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "wb") as f:
        f.write(data)
    print("[WRITE] %s（%d 字节）" % (OUT_REL, len(data)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
