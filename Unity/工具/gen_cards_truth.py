# -*- coding: utf-8 -*-
"""gen_cards_truth.py —— **SO 真值旁路**（任务线 T12，引擎线 P0）

## 这是什么

把**当前 build**（2026-10-18）的 `RawCardScript` 全部序列化真值导成「引擎可对拍」的 JSON。
**只读**：不改运行期解析器（`EffectText` / `RuleCore` 一行都不动），也不做任何语义推导。

- 输入：`d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`
  —— 1,351 个 `RawCardScript`（`union full_raw` 116 个序列化字段；外层 31 个是消费过的子集）。
- 产物（同一次运行产出，**不会分叉**；两份内容逐字节等价、只有排版不同）：
  1. `d:/4/Unity/数据/游戏数据/cards_truth.json` —— 人读版（`indent=1`）
  2. `d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_truth.json` —— 运行期可读（紧凑、无空格）
  ⚠️ 形状是 `{"cards": [ … ]}` **数组**（`JsonUtility` 不支持字典 —— 见 `gen_related_cards.py` 的先例）。

## 只落真值（⛔ 不做语义推导）

- 所有枚举**保留 SO 原值整数**（`trigger` 60 种、`chosenAbility` 55 种、`abilityContext` 5 种 …），
  **不映射成名字** —— 名字表属于语义层，弄错了会静默错卡。
- 判据里**默认值不落**（规则见下 `DEFAULTS` + 每个 helper 的注释）；**没落 = SO 默认值**。
  - 例外：`damage` / `heal` 四个/三个子键**整组照抄**（哪怕其中一项是 0），因为「0 伤害但带 damageType」
    这种情况不能靠省略区分。
- 字段名以 SO 为准；`cardTraits` 的条目照抄 `{name,_trait,_traitValue[, _hide]}`。

## 字段来源（⛔ 别改成猜）

| 输出字段 | SO 里的位置 |
|---|---|
| `id` / `name` / `army` / `manaCost` / 四围 / `cardType` / `spellType` / `rarity` / `keyword` / `initiative` | 外层（`uniqueId` / `cardName` / `cardArmy` / … / `cardRarity` / `keyword`） |
| `useOverDraw` / `targeted` / `attackScoringCriteria` / `cardTargetScoringCriteria` / `nonTargetScoringCriteria` | **只在 `full_raw` 里**（外层那 31 个键没有它们） |
| `cardTraits` | `CardTraits[]`（大写 C） |
| `abilities[].logic[]` | `cardAbilities[].abilityLogic[]` |
| `activeAbility` | `activeAbility`（`hasActiveAbility` 是**另一个**字段，两者不总一致 —— 见 `stats.flagMismatch`） |

**卡级 `maxTargets`** = `max(abilityLogic[].targetCriteria.maxTargets, cardAbilities[].targetCriteria.maxTargets)`。
⚠️ `cardAbilities[].targetCriteria.maxTargets` 在 1,496 条里只有 2 条非 0；真正的值在 **`abilityLogic[].targetCriteria`**
（池内 >0 的卡有 256 张）—— 和 `R_卡牌线…评估.md` §2 那行「317 张 >0」是同一处（那行的口径可能是含池外 224 张：全量 316~317）。

## 不变量（`invariant` 字段里也写了）

1. `cards[].id` 唯一；
2. `cards[].id` 集合 == `数据/游戏数据/card_ids.json` 的 `mapping` 键集合（差值落 `idDiff.vsCardIds`，**不静默**）；
3. 所有 `chosenAbility` / `trigger` 都是**整数原值**，没有名字字符串；
4. 两份产物 `json.dumps` 后再 `json.loads` 完全相等（只有缩进不同）。

## 用法

```bash
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_cards_truth.py
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_cards_truth.py --check   # 只比不写
```
"""

import argparse
import hashlib
import io
import json
import os
import sys
from collections import Counter

SRC = r"d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
CARD_IDS = r"d:/4/Unity/数据/游戏数据/card_ids.json"
POOL = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"  # 只读，只取 id 做差集
OUT_HUMAN = r"d:/4/Unity/数据/游戏数据/cards_truth.json"
RULE_ENGINE_RES = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_truth.json"

# SO 结构体里「默认值」的判据：0 / 0.0 / "" / False / 空 list / null。
# ⚠️ 有少数字段的默认不是 0（multiplyByFriend=1.0、multiplyByEnemy=-1.0、priority=1），
#    由调用点显式传入，别在这里一刀切。
DEFAULTS = (0, 0.0, "", False, None)

# 只会出现在 full_raw 里的字段（外层 31 键没有）—— 落报告用
FULL_RAW_ONLY = [
    "useOverDraw", "targeted", "attackScoringCriteria",
    "cardTargetScoringCriteria", "nonTargetScoringCriteria",
]


def is_set(v):
    """非默认值？dict/list 单独判空，别把 `{}`/`[]` 当有值。"""
    if isinstance(v, (dict, list)):
        return bool(v)
    return v not in DEFAULTS


def load_json(path):
    with io.open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def resolve_path_id(pid, by_path_id):
    """裸 PPtr 的 m_PathID → 卡 uniqueId；解不出就返回 None（⛔ 不猜）。"""
    return by_path_id.get(pid)


def resolve_pptr_list(lst, by_path_id):
    """PPtr 数组 → `{"ids": […], "unresolved": [pathId…]}`（两个桶都出声）。

    ⚠️ **保序、不 dedupe**：`unitsToSummon` 里同一张卡出现两次 = 召两只，去重会静默改语义。
    """
    ids, unresolved = [], []
    for p in lst or []:
        if not isinstance(p, dict):
            unresolved.append(p)
            continue
        pid = p.get("m_PathID", 0)
        if not pid:
            continue
        uid = resolve_path_id(pid, by_path_id)
        if uid is None:
            unresolved.append(pid)
        else:
            ids.append(uid)
    return ids, unresolved


def compact_criteria(c, by_path_id):
    """判据树 → 只留非默认键（键名照抄 SO）。`countCriteria` 递归压。

    `filterSpecificCards` / `excludeEnchantments` 是 PPtr 数组 ⇒ 解析成卡 id（解不出的落 `unresolved*`）。
    """
    if not c:
        return {}
    out = {}
    for k, v in c.items():
        if k in ("filterSpecificCards", "excludeEnchantments"):
            ids, unresolved = resolve_pptr_list(v, by_path_id)
            if ids:
                out[k] = ids
            if unresolved:
                out[k + "_unresolved"] = unresolved
            continue
        if isinstance(v, dict):
            sub = compact_criteria(v, by_path_id)  # 目前只有 boardCriteria.countCriteria 走这里
            if sub:
                out[k] = sub
            continue
        if isinstance(v, list):
            if v:
                out[k] = v
            continue
        if is_set(v):
            out[k] = v
    return out


def compact_buff(b):
    """`buffEffect` / `handBuff.cardEffect` 的压缩（键名照抄 SO）。

    ⚠️ `enchantingCard` / `enchantment` 是 PPtr，**两个键恒存在**（全 0）⇒ 只有 m_PathID 非 0 才落。
    """
    if not b:
        return None
    out = {}
    for k, v in b.items():
        if k in ("enchantingCard", "enchantment"):
            if isinstance(v, dict) and v.get("m_PathID"):
                out[k] = {"m_PathID": v["m_PathID"]}
            continue
        if is_set(v):
            out[k] = v
    return out or None


def compact_hand_buff(h, by_path_id):
    """`handBuff` / scoring 的 `handEffect`：`cardEffect` + 用量 + 目标判据。"""
    if not h:
        return None
    out = {}
    ce = compact_buff(h.get("cardEffect"))
    if ce:
        out["cardEffect"] = ce
    for k in ("playersAffected", "limitedUses", "numberOfUses"):
        if is_set(h.get(k)):
            out[k] = h[k]
    tc = compact_criteria(h.get("targetCriteria"), by_path_id)
    if tc:
        out["targetCriteria"] = tc
    return out or None


def compact_damage(d):
    """damageCriteria 整组照抄（全 0 就不落）。"""
    if not d:
        return None
    if not (d.get("minDamage") or d.get("maxDamage") or d.get("damageType") or d.get("valueOptions")):
        return None
    return {
        "minDamage": d.get("minDamage", 0),
        "maxDamage": d.get("maxDamage", 0),
        "damageType": d.get("damageType", 0),
        "valueOptions": d.get("valueOptions", 0),
    }


def compact_heal(d):
    if not d:
        return None
    if not (d.get("minValue") or d.get("maxValue") or d.get("valueOptions")):
        return None
    return {
        "minValue": d.get("minValue", 0),
        "maxValue": d.get("maxValue", 0),
        "valueOptions": d.get("valueOptions", 0),
    }


def compact_summon(s, by_path_id):
    """summonCriteria：把 `unitsToSummon` 的 PPtr 解成卡 id，其余非默认标志照抄。

    ⚠️ `m_PathID == 0` 是**空引用**（SO 里真有这种槽，当前 2 条）——单列 `unitsToSummon_null` 计数，
    ⛔ 不与「解不出」混为一谈（后者是真引用但不在本包对象表里）。
    """
    if not s:
        return None
    out = {}
    raw_units = s.get("unitsToSummon") or []
    nulls = sum(1 for p in raw_units if not (isinstance(p, dict) and p.get("m_PathID")))
    ids, unresolved = resolve_pptr_list(raw_units, by_path_id)
    if ids:
        out["unitsToSummon"] = ids
    if nulls:
        out["unitsToSummon_null"] = nulls
    if unresolved:
        out["unitsToSummon_unresolved"] = unresolved
    for k, v in s.items():
        if k == "unitsToSummon":
            continue
        if isinstance(v, list):
            if v:
                out[k] = v
        elif is_set(v):
            out[k] = v
    return out or None


def compact_mana_buff(m):
    if not m:
        return None
    out = {k: v for k, v in m.items() if is_set(v)}
    return out or None


def compact_logic(l, by_path_id):
    """`abilityLogic[]` 一条 → 精简真值。

    必落 `chosenAbility`（0 也落）；其余只落非默认。
    """
    e = {"chosenAbility": l.get("chosenAbility", 0)}
    for k in ("playersAffected", "furtherMultiplier", "chanceOfHappening", "checkIfCasterInPlay"):
        if is_set(l.get(k)):
            e[k] = l[k]
    for k in ("targetCriteria", "multiplier"):
        c = compact_criteria(l.get(k), by_path_id)
        if c:
            e[k] = c
    d = compact_damage(l.get("damageCriteria"))
    if d:
        e["damage"] = d
    h = compact_heal(l.get("healCriteria"))
    if h:
        e["heal"] = h
    b = compact_buff(l.get("buffEffect"))
    if b:
        e["buff"] = b
    s = compact_summon(l.get("summonCriteria"), by_path_id)
    if s:
        e["summon"] = s
    hb = compact_hand_buff(l.get("handBuff"), by_path_id)
    if hb:
        e["handBuff"] = hb
    mb = compact_mana_buff(l.get("manaBuff"))
    if mb:
        e["manaBuff"] = mb
    return e


def compact_ability(a, by_path_id):
    """`cardAbilities[]` 一条。"""
    out = {"trigger": a.get("trigger", 0)}
    if is_set(a.get("triggerValue")):
        out["triggerValue"] = a["triggerValue"]
    if is_set(a.get("abilityContext")):
        out["abilityContext"] = a["abilityContext"]
    if a.get("priority", 1) != 1:
        out["priority"] = a["priority"]
    if is_set(a.get("randomAbilityChoice")):
        out["randomAbilityChoice"] = a["randomAbilityChoice"]
    for k in ("actingCardCriteria", "targetCriteria", "boardCriteria", "multiCountCriteria"):
        c = compact_criteria(a.get(k), by_path_id)
        if c:
            out[k] = c
    out["logic"] = [compact_logic(x, by_path_id) for x in (a.get("abilityLogic") or [])]
    return out


def compact_active(a, by_path_id, logic_key):
    """`activeAbility` / `alternativeAbility` 的概要（只在「有内容」时落，判据见 `has_active_payload`）。"""
    out = {
        "manaCost": a.get("manaCost", 0),
        "plasmaCost": a.get("plasmaCost", 0),
        "psychicCost": a.get("psychicCost", 0),
    }
    for k in ("minHeroLevel", "targeted", "resolutionChoosingCount", "needsLoading"):
        if is_set(a.get(k)):
            out[k] = a[k]
    for k in ("targetCriteria", "targetsAvailableCounter"):
        c = compact_criteria(a.get(k), by_path_id)
        if c:
            out[k] = c
    lg = [compact_logic(x, by_path_id) for x in (a.get(logic_key) or [])]
    if lg:
        out["logic"] = lg
    return out


def has_active_payload(a, logic_key):
    if not a:
        return False
    if a.get(logic_key):
        return True
    if a.get("manaCost") or a.get("plasmaCost") or a.get("psychicCost"):
        return True
    if a.get("targeted"):
        return True
    return False


def compact_scoring(s, by_path_id):
    """评分判据一条（`attackScoringCriteria` / `cardTarget*` / `nonTarget*`）。

    五个标量照抄原值、**一律只在 != 0 时落**（0.0 视为默认）。⚠️ 别把 `multiplyByFriend=1.0` /
    `multiplyByEnemy=-1.0` 当默认：实测 1,351 张卡的 `attackScoringCriteria` 标量全是
    `0/0/1.0/-1.0/scoringOption=0` 同一份模板，列表条目里也有 1.0/-1.0 的真值
    ⇒ 那两种「默认」在本语料里分不开，一律按「非 0 即落」处理（宁可多落，不静默丢真值）。
    """
    if not s:
        return None
    out = {}
    for k in ("baseValueFriend", "baseValueEnemy", "multiplyByFriend", "multiplyByEnemy", "scoringOption"):
        v = s.get(k, 0)
        if v not in (0, 0.0):
            out[k] = v
    for k in ("targetCriteria", "multiplierCriteria"):
        c = compact_criteria(s.get(k), by_path_id)
        if c:
            out[k] = c
    d = compact_damage(s.get("damageCriteria"))
    if d:
        out["damage"] = d
    h = compact_heal(s.get("healCriteria"))
    if h:
        out["heal"] = h
    b = compact_buff(s.get("buffEffect"))
    if b:
        out["buff"] = b
    he = compact_hand_buff(s.get("handEffect"), by_path_id)
    if he:
        out["handEffect"] = he
    for k in ("codexEffect", "comboCardFirst"):
        if is_set(s.get(k)):
            out[k] = s[k]
    cc = s.get("comboCard")
    if isinstance(cc, dict) and cc.get("m_PathID"):
        out["comboCard"] = {"m_PathID": cc["m_PathID"]}
    return out or None


def build():
    raw = load_json(SRC)
    cards = raw["cards"]
    card_ids = load_json(CARD_IDS)
    mapping = card_ids.get("mapping") or {}
    # ⚠️ 池子 id 要从 cards_engine.json 取 —— `card_ids.poolTrueIds` 是**历史迁移表**
    #    （键 = 旧池 id、值 = 真 id；K06 之后池内 id 已全换成真 id，拿它的键当池子会算错）
    pool_ids = {x.get("id") for x in (load_json(POOL).get("cards") or [])}

    by_path_id = {c["path_id"]: c["uniqueId"] for c in cards}
    # path_id 反查表必须无重复（否则 PPtr 解析会静默拿错 —— 见「解包数据七个判据」）
    dup = [p for p, n in Counter(c["path_id"] for c in cards).items() if n > 1]
    if dup:
        print("[自检] ⚠️ path_id 重复 %d 个：%s" % (len(dup), dup[:5]))

    out_cards = []
    missing = set()
    stats = Counter()

    for c in cards:
        fr = c.get("full_raw") or {}

        def get(key):
            """外层优先、再 full_raw；两处都没有 ⇒ 记 missing。"""
            if key in c:
                return c[key]
            if key in fr:
                return fr[key]
            missing.add(key)
            return None

        # —— 卡级 maxTargets：abilityLogic / cardAbilities 的 targetCriteria ——
        mt = 0
        for a in c.get("cardAbilities") or []:
            mt = max(mt, (a.get("targetCriteria") or {}).get("maxTargets", 0) or 0)
            for l in a.get("abilityLogic") or []:
                mt = max(mt, (l.get("targetCriteria") or {}).get("maxTargets", 0) or 0)

        entry = {
            "id": c.get("uniqueId"),
            "name": c.get("cardName"),
            "army": c.get("cardArmy"),
            "manaCost": c.get("manaCost"),
            "melee": c.get("meleeAttack"),
            "range": c.get("rangeAttack"),
            "health": c.get("maxHealth"),
            "armour": c.get("armour"),
            "cardType": get("cardType"),
            "spellType": get("spellType"),
            "rarity": get("cardRarity"),
            "keyword": get("keyword"),
            "initiative": get("initiative"),
            "useOverDraw": get("useOverDraw"),
            "targeted": get("targeted"),
            "hasActiveAbility": c.get("hasActiveAbility", 0),
            "hasAlternativeAbility": c.get("hasAlternativeAbility", 0),
            "maxTargets": mt,
        }

        traits = []
        for t in c.get("CardTraits") or []:
            tr = {"name": t.get("name"), "_trait": t.get("_trait"), "_traitValue": t.get("_traitValue")}
            if is_set(t.get("_hide")):
                tr["_hide"] = t["_hide"]
            traits.append(tr)
        if traits:
            entry["cardTraits"] = traits

        abilities = [compact_ability(a, by_path_id) for a in (c.get("cardAbilities") or [])]
        if abilities:
            entry["abilities"] = abilities

        aa = c.get("activeAbility") or {}
        if has_active_payload(aa, "activeAbilityLogic"):
            entry["activeAbility"] = compact_active(aa, by_path_id, "activeAbilityLogic")
        alt = c.get("alternativeAbility") or {}
        if has_active_payload(alt, "activeAbilityLogic"):
            entry["alternativeAbility"] = compact_active(alt, by_path_id, "activeAbilityLogic")

        asc = compact_scoring(get("attackScoringCriteria"), by_path_id)
        if asc:
            entry["attackScoringCriteria"] = asc
        cts = get("cardTargetScoringCriteria") or []
        nts = get("nonTargetScoringCriteria") or []
        if cts:
            entry["cardTargetScoringCriteria"] = [
                x for x in (compact_scoring(s, by_path_id) for s in cts) if x
            ]
        if nts:
            entry["nonTargetScoringCriteria"] = [
                x for x in (compact_scoring(s, by_path_id) for s in nts) if x
            ]

        out_cards.append(entry)

        # —— 统计 ——
        stats["cards"] += 1
        if abilities:
            stats["withAbilities"] += 1
        stats["abilities"] += len(abilities)
        stats["logic"] += sum(len(a["logic"]) for a in abilities)
        if traits:
            stats["withTraits"] += 1
        if mt:
            stats["maxTargetsGt0"] += 1
        if entry["hasActiveAbility"]:
            stats["hasActiveAbility1"] += 1
        if entry["hasAlternativeAbility"]:
            stats["hasAlternativeAbility1"] += 1
        if "activeAbility" in entry:
            stats["activeAbilityEmitted"] += 1
        if aa.get("activeAbilityLogic"):
            stats["activeAbilityLogicNonEmpty"] += 1
            if not entry["hasActiveAbility"]:
                stats["flagMismatch_activeAbility_logicButFlag0"] += 1
        if entry.get("useOverDraw"):
            stats["useOverDraw1"] += 1
        if entry.get("targeted"):
            stats["targeted1"] += 1
        if "attackScoringCriteria" in entry:
            stats["attackScoringNonDefault"] += 1
        if cts:
            stats["cardTargetScoringCards"] += 1
            stats["cardTargetScoringEntries"] += len(cts)
        if nts:
            stats["nonTargetScoringCards"] += 1
            stats["nonTargetScoringEntries"] += len(nts)
        for a in abilities:
            for l in a["logic"]:
                for k in ("damage", "heal", "buff", "summon", "handBuff", "manaBuff"):
                    if k in l:
                        stats["logicWith_" + k] += 1

    out_cards.sort(key=lambda x: x["id"] or "")

    # 召唤引用的解析口径单列（报告要能看出「是真引用解不出」还是「SO 里就是空槽」）
    for c in out_cards:
        for a in c.get("abilities") or []:
            for l in a["logic"]:
                s = l.get("summon")
                if not s:
                    continue
                stats["summonLogics"] += 1
                stats["summonUnitsResolved"] += len(s.get("unitsToSummon") or [])
                stats["summonUnitsNull"] += s.get("unitsToSummon_null", 0)
                stats["summonUnitsUnresolved"] += len(s.get("unitsToSummon_unresolved") or [])

    truth_ids = [x["id"] for x in out_cards]
    mapping_keys = set(mapping.keys())
    truth_set = set(truth_ids)

    def diff(a, b):
        return {"truthOnly": sorted(a - b), "otherOnly": sorted(b - a)}

    # —— 源文件指纹（能证明「这份产物是哪一版语料出来的」；不碰 git）——
    h = hashlib.sha256()
    with io.open(SRC, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    src_mtime = os.path.getmtime(SRC)

    doc = {
        "note": "**SO 真值旁路**（T12）：当前 build 的 RawCardScript 全量序列化真值 → 引擎可对拍。"
                "⛔ 本文件只落 SO 原值（枚举一律整数、不映射名字），**不做任何语义推导**；"
                "判据里「没落的键 = SO 默认值」（0/0.0/空串/空列表），唯一例外是 damage/heal 子结构整组照抄。"
                "人读版 `数据/游戏数据/cards_truth.json` 与本文件**内容逐字节等价**，只有排版不同（本份紧凑）。"
                "由 工具/gen_cards_truth.py 生成，⛔ 不手改。",
        "source": "d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
                  "（当前 build allcards，RawCardScript × %d）" % len(cards),
        "sourceSha256": h.hexdigest(),
        "sourceMtime": src_mtime,
        "sourceBundle": raw.get("bundle"),
        "generatedAt": "2026-10-18",
        "provenance": "卡片 SO（RawCardScript）逐字段照抄：外层 31 键 + `full_raw` 116 键；"
                      "`useOverDraw`/`targeted`/三个评分判据**只在 full_raw 里**（外层没有）。"
                      "卡级 `maxTargets` = max(abilityLogic[].targetCriteria.maxTargets, "
                      "cardAbilities[].targetCriteria.maxTargets)。"
                      "`summon.unitsToSummon` 与 criteria 里的 filterSpecificCards/excludeEnchantments "
                      "按同包 m_PathID 反查成卡 uniqueId（`m_PathID==0` 的空槽记 `unitsToSummon_null` 计数；"
                      "真引用但不在本包对象表里的落 `*_unresolved`，⛔ 不猜）。"
                      "`activeAbility` 只在「hasActiveAbility==1 或 activeAbilityLogic 非空或三项费用/targeted 非 0」时落。",
        "defaults": {
            "omitted": "0 / 0.0 / \"\" / false / [] / null（键不出现即为该值）",
            "exceptions": {
                "damage": "整组照抄 {minDamage,maxDamage,damageType,valueOptions}（唯一整组保留的子结构）",
                "heal": "整组照抄 {minValue,maxValue,valueOptions}",
                "priority": "默认 1（不是 0），只有 !=1 才落",
                "multiplyByFriend/multiplyByEnemy": "照抄原值；SO 里模板是 1.0/-1.0，列表条目多为 0.0",
            },
        },
        "invariant": "① cards[].id 唯一；② cards[].id 集合与 card_ids.json 的 mapping 键集合的差集见 idDiff.vsCardIds"
                     "（当前必须两边都为空）；③ cards_engine.json 的池内 id 必须全部命中真值集"
                     "（idDiff.vsPool.otherOnly 为空；truthOnly 应 = 池外 224 张）；"
                     "④ 枚举一律整数原值；⑤ 两份产物 json.loads 后完全相等。",
        "count": len(out_cards),
        "missing": sorted(missing),
        "stats": dict(sorted(stats.items())),
        "idDiff": {
            "vsCardIds": diff(truth_set, mapping_keys),
            "vsPool": diff(truth_set, pool_ids),
            "counts": {
                "truth": len(truth_set),
                "cardIdsMapping": len(mapping_keys),
                "poolCards": len(pool_ids),
            },
        },
        "cards": out_cards,
    }
    return doc


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只和盘上那份比，不写")
    args = ap.parse_args()

    doc = build()
    human = json.dumps(doc, ensure_ascii=False, indent=1) + "\n"
    runtime = json.dumps(doc, ensure_ascii=False, separators=(",", ":")) + "\n"

    # 自检：两份内容必须等价（只差排版）
    if json.loads(human) != json.loads(runtime):
        print("[自检] ❌ 两份产物内容不一致（排版以外有差异）")
        return 2
    ids = [c["id"] for c in doc["cards"]]
    if len(ids) != len(set(ids)):
        print("[自检] ❌ id 有重复")
        return 2
    d = doc["idDiff"]["vsCardIds"]
    if d["truthOnly"] or d["otherOnly"]:
        print("[自检] ⚠️ 与 card_ids.json.mapping 的 id 集合不一致：truthOnly=%d otherOnly=%d"
              % (len(d["truthOnly"]), len(d["otherOnly"])))
    else:
        print("[自检] ✅ id 集合 == card_ids.json.mapping（%d）" % doc["count"])

    print("\n=== 覆盖率（非空/非默认）===")
    for k, v in doc["stats"].items():
        print("  %-40s %s" % (k, v))
    print("  missing keys:", doc["missing"] or "无")
    print("  idDiff.vsPool: truthOnly=%d poolOnly=%d"
          % (len(doc["idDiff"]["vsPool"]["truthOnly"]), len(doc["idDiff"]["vsPool"]["otherOnly"])))

    if args.check:
        ok = True
        for p, t in ((OUT_HUMAN, human), (RULE_ENGINE_RES, runtime)):
            old = io.open(p, encoding="utf-8").read() if os.path.exists(p) else ""
            same = old == t
            ok = ok and same
            print("[--check] %s 与盘上那份%s" % (p, "一致 ✅" if same else "**不同** ❌"))
        return 0 if ok else 1

    # ⚠️ 先把内容算进变量、确认没错再写（铁律：write(<表达式>) 会先截断再求值）
    for p, t in ((OUT_HUMAN, human), (RULE_ENGINE_RES, runtime)):
        io.open(p, "w", encoding="utf-8", newline="\n").write(t)
        print("[写盘] %s  %d 字节" % (p, len(t.encode("utf-8"))))
    return 0


if __name__ == "__main__":
    sys.exit(main())
