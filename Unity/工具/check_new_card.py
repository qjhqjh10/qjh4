#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
check_new_card.py —— 新卡校验器（K10）

输入「一条新卡」（JSON 对象 / JSON 数组 / JSONL / `{"cards":[…]}`），逐条检查：
  id 形态与唯一性 · 枚举取值合法 · 必填数值 · 本地化键 · 立绘/语音/动画 GUID · animInfo 可解析
输出分 **错误 / 警告 / 通过** 三档，每条判据带出处。

用法（路径全部写死以 `d:/4/Unity` 为根，不依赖当前工作目录）：
    python -I -X utf8 d:/4/Unity/工具/check_new_card.py --card <卡片.json>
    python -I -X utf8 d:/4/Unity/工具/check_new_card.py --card <卡片.jsonl> --out <报告.json>
    python -I -X utf8 d:/4/Unity/工具/check_new_card.py --list-checks

输入形态（三种都吃，按字段别名识别）：
  1. 原版 SO 形态     : uniqueId / cardName / cardArmy / cardType / spellType / cardRarity / keyword
                        / manaCost / meleeAttack / rangeAttack / maxHealth / armour / cardAbilities[…]
  2. 我们卡池形态     : id / name / faction / type / cost / attack / ranged / health / armour / rarity
                        / subtype / desc / keywords[…]      （= `cards_engine.json` 的一行）
  3. card_stats 形态  : id / name / faction / type / cost / attack / health / ranged_attack / armor
                        / rarity / subtype / desc            （= `数据/游戏数据/card_stats.json` 的一行）
  额外可选字段（做 GUID / animInfo 两项检查时用）：
    cardImageReference / fullHeroSpriteReference / summonAudio / attackAudio / deathAudio /
    customRangedAttackParticles / customDeathParticles / remnantBodyPrefab / summonAnimDelayed
      —— 值可以是 `{"m_AssetGUID": "…"}`，也可以是裸 GUID 字符串
    cardAnimations: ["<guid>", …]（也可以写成 `{"m_AssetGUID": …}`，或把 animInfo 直接内联）
    animInfo: {vfxDelayTime, startDelay, timeAtStartPos, timeMoving, timeAtEndPos, …}
    refNameId / cardDescriptionTextReferenceId —— 本地化键引用（空 = 用 `…/<id>`）

判据出处（每条检查在输出里都会打印 `出处:`）：
  * id 形态 / 前缀→阵营 / 取号规则 : `资料/卡牌制作_正本.md` §1·2 / §1·3（实测 1,040 前缀号 + 311 hex）
  * 枚举取值 : `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{CardTypeOptions,SpellType,CardRarity,
               Keyword,CardArmy,AbilityTrigger,AbilityEffect,AbilityContext,ActionPriority}.cs`
               —— **运行时直接从枚举桩解析**（⛔ 不在工具里抄第二份，避免两处不一致）
  * 必填数值 : 正本 §2·2 B 组
  * 本地化键 : 正本 §5·1 / §5·2 + `数据/游戏数据/官方本地化_en+zh-CN.json`
  * GUID→资产 : `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv`
  * animInfo  : `数据/游戏数据/card_anim_order.json`（`cardAnims[].animInfo`）+
                `AnimInfo.GetAnimDuration() = vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`
                （出处 `数据/游戏数据/ability_anim_durations.json` 的 `_conventions.formula`）

退出码（⛔ 不许静默失败）：
    0 = 没有「错误」（可以有「警告」/「跳过」）
    1 = 至少一「错误」
    2 = 工具自身跑不起来（输入读不了 / 枚举桩缺失 / 输出路径非法）——此时**不写产物**
`--out` 只在**没有错误**时写盘（失败不写产物）。
"""
import argparse
import json
import os
import re
import sys

ROOT = "d:/4/Unity"                                   # 🔴 工程根（不依赖 cwd）
ENUM_DIR = "d:/2/Warpforge_code/Scripts/Assembly-CSharp"   # 🔴 只读
LOC_FILE = os.path.join(ROOT, "数据/游戏数据/官方本地化_en+zh-CN.json")
CARD_IDS_FILE = os.path.join(ROOT, "数据/游戏数据/card_ids.json")
POOL_FILE = os.path.join(ROOT, "MyGame/Assets/RuleEngine/Resources/cards_engine.json")
GUID_MAP = os.path.join(ROOT, "资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv")
ANIM_ORDER = os.path.join(ROOT, "数据/游戏数据/card_anim_order.json")
BUNDLE_CARDS_TSV = os.path.join(ROOT, "资料/普查产出_1018/allcards_1350/cards.tsv")
BUNDLE_CARDS_JSON = os.path.join(ROOT, "资料/普查产出_1018/allcards_1350/cards_raw.json")
EXPORTED_CARDS_TSV = "d:/4/_tmp_view/allcards_export/cards.tsv"
FORBIDDEN_WRITE_ROOT = "d:/2"

SCHEMA = "check_new_card/v1"

# ---------------------------------------------------------------- 输入字段别名
ALIASES = {
    "id": ["id", "uniqueId"],
    "name": ["name", "cardName"],
    "cost": ["cost", "manaCost"],
    "attack": ["attack", "meleeAttack"],
    "ranged": ["ranged", "ranged_attack", "rangeAttack"],
    "health": ["health", "maxHealth"],
    "armour": ["armour", "armor"],
    "faction": ["faction"],
    "cardArmy": ["cardArmy"],
    "type": ["type"],
    "cardType": ["cardType"],
    "spellType": ["spellType"],
    "rarity": ["rarity"],
    "cardRarity": ["cardRarity"],
    "subtype": ["subtype"],
    "keyword": ["keyword"],
    "desc": ["desc"],
    "keywords": ["keywords"],
    "cardTraits": ["cardTraits", "CardTraits"],
    "cardAbilities": ["cardAbilities"],
    "activeAbility": ["activeAbility"],
    "refNameId": ["refNameId"],
    "cardDescriptionTextReferenceId": ["cardDescriptionTextReferenceId"],
    "cardAnimations": ["cardAnimations"],
    "animInfo": ["animInfo"],
    "cardImageReference": ["cardImageReference"],
    "fullHeroSpriteReference": ["fullHeroSpriteReference"],
    "summonAudio": ["summonAudio"],
    "attackAudio": ["attackAudio"],
    "deathAudio": ["deathAudio"],
    "customRangedAttackParticles": ["customRangedAttackParticles"],
    "customDeathParticles": ["customDeathParticles"],
    "remnantBodyPrefab": ["remnantBodyPrefab"],
    "summonAnimDelayed": ["summonAnimDelayed"],
}

# 我们的 `type` 串 → `CardTypeOptions`（正本 §2·3；`defence` 由 `spellType=210` 推出，见 gen_cards_engine.py）
ENGINE_TYPE_TO_CARDTYPE = {"unit": 0, "hero": 10, "tactic": 20, "defence": 20}
ENGINE_RARITY = {"none": 0, "common": 1, "rare": 2, "epic": 3, "legendary": 4, "special": 5}

# 前缀 → CardArmy 名（正本 §1·2 的实测表；这是**我们定的惯例**，不是原版写下来的规则）
PREFIX_TO_ARMY = {
    "UM": "Ultramarines", "GOF": "Goff", "ASH": "SaimHann", "SAU": "Sautekh",
    "BL": "BlackLegion", "TL": "Leviathan", "TAU": "TauEmpire", "SOR": "Sororitas",
    "GSC": "Genestealers", "AM": "AstraMilitarum", "DA": "DarkAngels",
    "EC": "EmperorsChildren", "SW": "SpaceWolves",
}

RE_ID_PREFIX = re.compile(r"^([A-Z]{2,4})(\d+)$")
RE_ID_HEX = re.compile(r"^[0-9a-f]{32}$")

# GUID 字段 → 允许的资产类型（判据 = guid_map_bundles.tsv 的 `type` 列实测取值）
GUID_FIELD_TYPES = {
    "cardImageReference": {"Sprite", "Texture2D"},
    "fullHeroSpriteReference": {"Sprite", "Texture2D"},
    "summonAudio": {"AudioClip"},
    "attackAudio": {"AudioClip"},
    "deathAudio": {"AudioClip"},
    "customRangedAttackParticles": {"MonoBehaviour"},
    "customDeathParticles": {"GameObject", "MonoBehaviour"},
    "remnantBodyPrefab": {"GameObject", "MonoBehaviour"},
    "summonAnimDelayed": {"GameObject"},
    "cardAnimations": {"MonoBehaviour"},
}

REQ_ANIM_KEYS = ["vfxDelayTime", "startDelay", "timeAtStartPos", "timeMoving", "timeAtEndPos"]
ANIM_DURATION_KEYS = ["vfxDelayTime", "timeAtStartPos", "timeMoving", "timeAtEndPos"]
REQ_NUMERIC = ["cost", "attack", "ranged", "health", "armour"]

STATUS_ORDER = ["错误", "警告", "通过"]


# ---------------------------------------------------------------- 基础设施
def die(msg, code=2):
    sys.stderr.write("ERROR: %s\n" % msg)
    sys.exit(code)


def is_under(path, root):
    p = os.path.normcase(os.path.abspath(path)).replace("\\", "/").rstrip("/")
    r = os.path.normcase(os.path.abspath(root)).replace("\\", "/").rstrip("/")
    return p == r or p.startswith(r + "/")


def parse_enum(path):
    """从枚举桩（.cs）解析 name->value。⛔ 不在工具里抄第二份枚举表。"""
    if not os.path.isfile(path):
        die("判据缺失：枚举桩不存在 %s" % path)
    vals = {}
    with open(path, encoding="utf-8") as f:
        for line in f:
            m = re.match(r"^\s*([A-Za-z_]\w*)\s*=\s*(-?\d+)\s*,?\s*$", line)
            if m:
                vals[m.group(1)] = int(m.group(2))
    if not vals:
        die("判据缺失：%s 里没解析出任何枚举值" % path)
    return vals


class Enums(object):
    def __init__(self):
        self.card_type = parse_enum(os.path.join(ENUM_DIR, "CardTypeOptions.cs"))
        self.spell_type = parse_enum(os.path.join(ENUM_DIR, "SpellType.cs"))
        self.rarity = parse_enum(os.path.join(ENUM_DIR, "CardRarity.cs"))
        self.keyword = parse_enum(os.path.join(ENUM_DIR, "Keyword.cs"))
        self.army = parse_enum(os.path.join(ENUM_DIR, "CardArmy.cs"))
        self.trigger = parse_enum(os.path.join(ENUM_DIR, "AbilityTrigger.cs"))
        self.effect = parse_enum(os.path.join(ENUM_DIR, "AbilityEffect.cs"))
        self.context = parse_enum(os.path.join(ENUM_DIR, "AbilityContext.cs"))
        self.priority = parse_enum(os.path.join(ENUM_DIR, "ActionPriority.cs"))

    def describe(self):
        return {
            "CardTypeOptions": len(self.card_type), "SpellType": len(self.spell_type),
            "CardRarity": len(self.rarity), "Keyword": len(self.keyword),
            "CardArmy": len(self.army), "AbilityTrigger": len(self.trigger),
            "AbilityEffect": len(self.effect), "AbilityContext": len(self.context),
            "ActionPriority": len(self.priority),
        }


def load_json(path, what):
    if not os.path.isfile(path):
        die("判据缺失：%s 不存在（%s）" % (path, what))
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def load_existing_ids():
    """id 唯一性要比对的三张表：卡池 / card_ids.mapping / 原版 bundle（cards.tsv 优先）。"""
    out = {}
    pool = load_json(POOL_FILE, "我们的卡池")
    out["卡池 cards_engine.json"] = {c.get("id") for c in pool.get("cards", []) if c.get("id")}
    ids = load_json(CARD_IDS_FILE, "card_ids.json")
    out["card_ids.json.mapping"] = set(ids.get("mapping", {}).keys())
    bundle_ids, src = set(), None
    for p in (BUNDLE_CARDS_TSV, EXPORTED_CARDS_TSV):
        if os.path.isfile(p):
            with open(p, encoding="utf-8") as f:
                hdr = f.readline().rstrip("\n").split("\t")
                if "uniqueId" in hdr:
                    i = hdr.index("uniqueId")
                    for line in f:
                        cols = line.rstrip("\n").split("\t")
                        if len(cols) > i and cols[i]:
                            bundle_ids.add(cols[i])
            src = p
            break
    if not bundle_ids and os.path.isfile(BUNDLE_CARDS_JSON):
        d = load_json(BUNDLE_CARDS_JSON, "原版 allcards")
        bundle_ids = {c.get("uniqueId") for c in d.get("cards", []) if c.get("uniqueId")}
        src = BUNDLE_CARDS_JSON
    if src:
        out["原版 allcards（%s）" % os.path.basename(src)] = bundle_ids
    return out


def load_loc():
    d = load_json(LOC_FILE, "官方本地化 en+zh-CN")
    return d.get("terms") or {}


def load_guid_map():
    if not os.path.isfile(GUID_MAP):
        die("判据缺失：GUID 表不存在 %s" % GUID_MAP)
    rows = {}
    with open(GUID_MAP, encoding="utf-8") as f:
        hdr = f.readline().rstrip("\n").split("\t")
        for line in f:
            cols = line.rstrip("\n").split("\t")
            if len(cols) != len(hdr):
                continue
            r = dict(zip(hdr, cols))
            g = r.get("assetGuid", "").strip()
            if g:
                rows.setdefault(g, []).append(r)
    return rows


def load_anim_anims():
    if not os.path.isfile(ANIM_ORDER):
        die("判据缺失：动画索引不存在 %s" % ANIM_ORDER)
    d = load_json(ANIM_ORDER, "card_anim_order")
    out = {}
    for a in d.get("cardAnims", []):
        g = a.get("guid")
        if g:
            out[g] = a
    return out


# ---------------------------------------------------------------- 报告
class Report(object):
    def __init__(self, card_label):
        self.card = card_label
        self.items = []

    def add(self, status, check, detail, source, skip=False):
        assert status in STATUS_ORDER
        self.items.append({"status": status, "check": check, "detail": detail,
                           "source": source, "skip": skip})

    def error(self, check, detail, source):
        self.add("错误", check, detail, source)

    def warn(self, check, detail, source):
        self.add("警告", check, detail, source)

    def skip(self, check, detail, source):
        # 「跳过」也出声：归在警告档，但带 [跳过] 前缀
        self.add("警告", check, detail, source, skip=True)

    def ok(self, check, detail, source):
        self.add("通过", check, detail, source)

    def counts(self):
        c = {s: 0 for s in STATUS_ORDER}
        for it in self.items:
            c[it["status"]] += 1
        return c

    def failed(self):
        return any(it["status"] == "错误" for it in self.items)

    def to_dict(self):
        return {"card": self.card, "counts": self.counts(), "items": self.items}


def fmt_num(v):
    return ("%g" % v) if isinstance(v, float) else str(v)


# ---------------------------------------------------------------- 归一化
def normalize(raw):
    """把三种输入形态归一；`found` 记录每个规范名是从哪个键取到的。"""
    found = {}
    for canon, keys in ALIASES.items():
        for k in keys:
            if k in raw and raw[k] is not None:
                found[canon] = (k, raw[k])
                break
    return found


def get(found, k, default=None):
    return found[k][1] if k in found else default


def used_key(found, k):
    return found[k][0] if k in found else None


def is_int(v):
    return isinstance(v, int) and not isinstance(v, bool)


def is_num(v):
    return isinstance(v, (int, float)) and not isinstance(v, bool)


def as_guid(v):
    if isinstance(v, dict):
        g = v.get("m_AssetGUID")
        return g if isinstance(g, str) and g else None
    if isinstance(v, str):
        return v.strip() or None
    return None


def iter_ability_logics(card_abilities, active_ability):
    """产出 (位置标签, AbilityLogic dict)。"""
    if isinstance(card_abilities, list):
        for i, ab in enumerate(card_abilities):
            if not isinstance(ab, dict):
                continue
            for j, lg in enumerate(ab.get("abilityLogic") or []):
                if isinstance(lg, dict):
                    yield ("cardAbilities[%d].abilityLogic[%d]" % (i, j), lg)
    if isinstance(active_ability, dict):
        for j, lg in enumerate(active_ability.get("activeAbilityLogic") or []):
            if isinstance(lg, dict):
                yield ("activeAbility.activeAbilityLogic[%d]" % j, lg)


# ---------------------------------------------------------------- 检查项
def check_id(r, found, enums, existing_ids):
    src_fmt = "资料/卡牌制作_正本.md §1·2（实测 1,040 前缀号 + 311 hex）"
    if "id" not in found:
        r.error("ID_FORMAT", "缺少 id / uniqueId", src_fmt)
        return None
    key, cid = found["id"]
    if not isinstance(cid, str) or not cid.strip():
        r.error("ID_FORMAT", "%s=%r 不是非空字符串" % (key, cid), src_fmt)
        return None
    cid = cid.strip()
    m = RE_ID_PREFIX.match(cid)
    if m:
        prefix, num = m.group(1), m.group(2)
        r.ok("ID_FORMAT", "%s=%s —— 前缀号形态（前缀 %s，号 %s；号段按实际占用现查）"
             % (key, cid, prefix, num), src_fmt)
        return {"id": cid, "prefix": prefix, "kind": "prefix"}
    if RE_ID_HEX.match(cid):
        r.ok("ID_FORMAT", "%s=%s —— 32 位 hex 形态（原版历史/特例形态，新卡不建议用；正本 §1·3 第 4 条）"
             % (key, cid), src_fmt)
        return {"id": cid, "prefix": None, "kind": "hex"}
    r.error("ID_FORMAT", "%s=%r 既不是 `^[A-Z]{2,4}\\d+$` 也不是 `^[0-9a-f]{32}$`（正本 §1·2 的两种实测形态）"
            % (key, cid), src_fmt)
    return {"id": cid, "prefix": None, "kind": "bad"}


def check_id_prefix_army(r, id_info, found, enums):
    src = "资料/卡牌制作_正本.md §1·2（前缀→阵营 1:1）"
    if not id_info or not id_info.get("prefix"):
        return
    prefix = id_info["prefix"]
    want = PREFIX_TO_ARMY.get(prefix)
    if want is None:
        r.error("ID_PREFIX_ARMY", "前缀 %s 不在 13 个已知阵营前缀里（%s）"
                % (prefix, " / ".join(sorted(PREFIX_TO_ARMY))), src)
        return
    army_num = get(found, "cardArmy")
    faction = get(found, "faction")
    if is_int(army_num):
        actual = [k for k, v in enums.army.items() if v == army_num]
        actual = actual[0] if actual else str(army_num)
        if actual == want:
            r.ok("ID_PREFIX_ARMY", "前缀 %s → %s，与 cardArmy=%d(%s) 一致"
                 % (prefix, want, army_num, actual), src)
        else:
            r.error("ID_PREFIX_ARMY", "前缀 %s 应属 %s，但 cardArmy=%d(%s)"
                    % (prefix, want, army_num, actual), src)
    elif isinstance(faction, str) and faction:
        if faction == want:
            r.ok("ID_PREFIX_ARMY", "前缀 %s → %s，与 faction 一致" % (prefix, want), src)
        else:
            r.error("ID_PREFIX_ARMY", "前缀 %s 应属 %s，但 faction=%r" % (prefix, want, faction), src)
    else:
        r.skip("ID_PREFIX_ARMY", "没提供 cardArmy/faction ⇒ 无法核对前缀归属（前缀 %s 含义 = %s）"
               % (prefix, want), src)


def check_id_unique(r, id_info, existing_ids):
    src = "资料/卡牌制作_正本.md §1·3（id 全局唯一；撞车是上游生成器的责任）"
    if not id_info or not id_info.get("id"):
        return
    cid = id_info["id"]
    hits = [name for name, ids in existing_ids.items() if cid in ids]
    if hits:
        r.error("ID_UNIQUE", "id=%s 已被占用：%s" % (cid, " · ".join(hits)), src)
    else:
        r.ok("ID_UNIQUE", "id=%s 在 %d 张表里均未占用（%s）"
             % (cid, len(existing_ids), " · ".join(existing_ids)), src)


def check_enums(r, found, enums):
    # --- cardType / type
    src_ct = "d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTypeOptions.cs"
    ct = get(found, "cardType")
    ty = get(found, "type")
    ct_from_str = None
    if isinstance(ty, str) and ty:
        if ty in ENGINE_TYPE_TO_CARDTYPE:
            ct_from_str = ENGINE_TYPE_TO_CARDTYPE[ty]
            r.ok("ENUM_CARDTYPE", "%s=%r → CardTypeOptions.%s(%d)"
                 % (used_key(found, "type"), ty,
                    [k for k, v in enums.card_type.items() if v == ct_from_str][0], ct_from_str),
                 src_ct + " + 正本 §2·3（我们的 type 串映射）")
        else:
            r.error("ENUM_CARDTYPE", "type=%r 不在 {unit,hero,tactic,defence} 里" % ty,
                    src_ct + " + gen_cards_engine.py `TYPES`")
    if ct is not None:
        if is_int(ct) and ct in enums.card_type.values():
            r.ok("ENUM_CARDTYPE", "%s=%d = CardTypeOptions.%s"
                 % (used_key(found, "cardType"), ct,
                    [k for k, v in enums.card_type.items() if v == ct][0]), src_ct)
            if ct_from_str is not None and ct != ct_from_str:
                r.error("ENUM_CARDTYPE", "cardType=%d 与 type=%r（→%d）不一致"
                        % (ct, ty, ct_from_str), src_ct)
        else:
            r.error("ENUM_CARDTYPE", "%s=%r 不是 CardTypeOptions 的取值（%s）"
                    % (used_key(found, "cardType"), ct,
                       " / ".join("%s=%d" % kv for kv in sorted(enums.card_type.items(), key=lambda x: x[1]))),
                    src_ct)
    if ct is None and ty is None:
        r.skip("ENUM_CARDTYPE", "没提供 cardType/type ⇒ 未检查", src_ct)

    # --- spellType
    src_st = "d:/2/Warpforge_code/Scripts/Assembly-CSharp/SpellType.cs"
    st = get(found, "spellType")
    if st is None:
        if ty == "defence":
            r.ok("ENUM_SPELLTYPE", "type=defence ⇒ 原版取值应为 DefensiveCard(210)（正本 §2·3 / gen_cards_engine 判别器）",
                 src_st)
        else:
            r.skip("ENUM_SPELLTYPE", "没提供 spellType ⇒ 未检查", src_st)
    elif isinstance(st, str):
        hit = [v for k, v in enums.spell_type.items() if k.lower() == st.lower()]
        if hit:
            r.ok("ENUM_SPELLTYPE", "spellType=%r → %d" % (st, hit[0]), src_st)
        else:
            r.error("ENUM_SPELLTYPE", "spellType=%r 不是 SpellType 的名字" % st, src_st)
    elif is_int(st) and st in enums.spell_type.values():
        r.ok("ENUM_SPELLTYPE", "spellType=%d = %s"
             % (st, [k for k, v in enums.spell_type.items() if v == st][0]), src_st)
    else:
        r.error("ENUM_SPELLTYPE", "spellType=%r 不是 SpellType 的取值（共 %d 个）"
                % (st, len(enums.spell_type)), src_st)

    # --- cardRarity / rarity
    src_rar = "d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardRarity.cs"
    cr = get(found, "cardRarity")
    ra = get(found, "rarity")
    if cr is not None:
        if is_int(cr) and cr in enums.rarity.values():
            r.ok("ENUM_RARITY", "%s=%d = %s"
                 % (used_key(found, "cardRarity"), cr,
                    [k for k, v in enums.rarity.items() if v == cr][0]), src_rar)
        else:
            r.error("ENUM_RARITY", "%s=%r 不是 CardRarity 的取值（0–5）"
                    % (used_key(found, "cardRarity"), cr), src_rar)
    if isinstance(ra, str) and ra:
        low = ra.lower()
        if low in ENGINE_RARITY:
            r.ok("ENUM_RARITY", "rarity=%r → CardRarity.%s(%d)"
                 % (ra, [k for k, v in enums.rarity.items() if v == ENGINE_RARITY[low]][0], ENGINE_RARITY[low]),
                 src_rar + " + 卡池实测取值")
        else:
            r.error("ENUM_RARITY", "rarity=%r 不在 {common,rare,epic,legendary,special,none} 里" % ra,
                    src_rar)
    if cr is None and ra is None:
        r.skip("ENUM_RARITY", "没提供 cardRarity/rarity ⇒ 未检查", src_rar)

    # --- keyword / subtype
    src_kw = "d:/2/Warpforge_code/Scripts/Assembly-CSharp/Keyword.cs"
    kw = get(found, "keyword")
    sub = get(found, "subtype")
    if kw is not None:
        if is_int(kw) and kw in enums.keyword.values():
            r.ok("ENUM_KEYWORD", "%s=%d = %s"
                 % (used_key(found, "keyword"), kw,
                    [k for k, v in enums.keyword.items() if v == kw][0]), src_kw)
        else:
            r.error("ENUM_KEYWORD", "%s=%r 不是 Keyword 兵种枚举（0/5/10/15/20/25/30/35/40）"
                    % (used_key(found, "keyword"), kw), src_kw)
    if isinstance(sub, str) and sub:
        hit = [v for k, v in enums.keyword.items() if k.lower() == sub.lower()]
        if hit:
            r.ok("ENUM_KEYWORD", "subtype=%r = Keyword 兵种枚举值 %d" % (sub, hit[0]), src_kw)
        else:
            r.warn("ENUM_KEYWORD",
                   "subtype=%r 不是 Keyword 兵种枚举名 —— 我们的卡池里确实有大量非兵种 subtype"
                   "（Spell/Tactic/Warlord/Defence…），所以只报警告、不报错；"
                   "若它是兵种，请改用 Infantry/Vehicle/… 之一" % sub,
                   src_kw + " + cards_engine.json 实测 subtype 分布")
    if kw is None and sub is None:
        r.skip("ENUM_KEYWORD", "没提供 keyword/subtype ⇒ 未检查", src_kw)

    # --- cardArmy / faction
    src_army = "d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs"
    army = get(found, "cardArmy")
    fac = get(found, "faction")
    if army is not None:
        if is_int(army) and army in enums.army.values():
            r.ok("ENUM_ARMY", "%s=%d = %s"
                 % (used_key(found, "cardArmy"), army,
                    [k for k, v in enums.army.items() if v == army][0]), src_army)
        else:
            r.error("ENUM_ARMY", "%s=%r 不是 CardArmy 的取值" % (used_key(found, "cardArmy"), army), src_army)
    elif isinstance(fac, str) and fac:
        if fac in enums.army:
            r.ok("ENUM_ARMY", "faction=%r = CardArmy.%s(%d)" % (fac, fac, enums.army[fac]), src_army)
        else:
            r.error("ENUM_ARMY", "faction=%r 不是 CardArmy 的名字（%s）"
                    % (fac, " / ".join(enums.army)), src_army)
    else:
        r.skip("ENUM_ARMY", "没提供 cardArmy/faction ⇒ 未检查", src_army)

    # --- cardAbilities[].trigger / abilityLogic[].chosenAbility
    src_trg = "d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs"
    src_eff = "d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs"
    abs_ = get(found, "cardAbilities")
    active = get(found, "activeAbility")
    if abs_ is None and active is None:
        r.skip("ENUM_TRIGGER", "没提供 cardAbilities ⇒ 未检查（白板卡可以没有）", src_trg)
        r.skip("ENUM_EFFECT", "没提供 cardAbilities/activeAbility ⇒ 未检查（白板卡可以没有）", src_eff)
        return
    n_trig = n_eff = 0
    bad_trig = []
    bad_eff = []
    bad_ctx = []
    bad_pri = []
    if abs_ is not None and not isinstance(abs_, list):
        r.error("ABILITY_SHAPE", "cardAbilities 不是数组（是 %s）" % type(abs_).__name__,
                "资料/卡牌制作_正本.md §2·4（List<CardAbility>）")
        abs_ = []
    for i, ab in enumerate(abs_ or []):
        if not isinstance(ab, dict):
            r.error("ABILITY_SHAPE", "cardAbilities[%d] 不是对象" % i, "正本 §2·4")
            continue
        t = ab.get("trigger")
        if not is_int(t) or t not in enums.trigger.values():
            bad_trig.append("cardAbilities[%d].trigger=%r" % (i, t))
        else:
            n_trig += 1
        c = ab.get("abilityContext")
        if c is not None and (not is_int(c) or c not in enums.context.values()):
            bad_ctx.append("cardAbilities[%d].abilityContext=%r" % (i, c))
        p = ab.get("priority")
        if p is not None and (not is_int(p) or p not in enums.priority.values()):
            bad_pri.append("cardAbilities[%d].priority=%r" % (i, p))
        if "abilityLogic" in ab and not isinstance(ab.get("abilityLogic"), list):
            r.error("ABILITY_SHAPE", "cardAbilities[%d].abilityLogic 不是数组" % i, "正本 §2·4")
    for where, lg in iter_ability_logics(abs_, active):
        e = lg.get("chosenAbility")
        if not is_int(e) or e not in enums.effect.values():
            bad_eff.append("%s.chosenAbility=%r" % (where, e))
        else:
            n_eff += 1
    if bad_trig:
        r.error("ENUM_TRIGGER", "非法 trigger：%s（合法值共 %d 个）"
                % ("；".join(bad_trig[:6]), len(enums.trigger)), src_trg)
    elif n_trig:
        r.ok("ENUM_TRIGGER", "%d 个 trigger 全部合法" % n_trig, src_trg)
    else:
        r.skip("ENUM_TRIGGER", "cardAbilities 里没有 trigger 可查（白板卡可以没有）", src_trg)
    if bad_eff:
        r.error("ENUM_EFFECT", "非法 chosenAbility：%s（合法值共 %d 个）"
                % ("；".join(bad_eff[:6]), len(enums.effect)), src_eff)
    elif n_eff:
        r.ok("ENUM_EFFECT", "%d 个 chosenAbility 全部合法" % n_eff, src_eff)
    else:
        r.skip("ENUM_EFFECT", "没有 abilityLogic/activeAbilityLogic 可查（白板卡可以没有）", src_eff)
    if bad_ctx:
        r.warn("ENUM_CONTEXT", "非法 abilityContext：%s（合法值 %s）"
               % ("；".join(bad_ctx[:6]),
                  " / ".join("%s=%d" % kv for kv in sorted(enums.context.items(), key=lambda x: x[1]))),
               "d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityContext.cs")
    if bad_pri:
        r.warn("ENUM_PRIORITY", "非法 priority：%s（合法值 %s）"
               % ("；".join(bad_pri[:6]),
                  " / ".join("%s=%d" % kv for kv in sorted(enums.priority.items(), key=lambda x: x[1]))),
               "d:/2/Warpforge_code/Scripts/Assembly-CSharp/ActionPriority.cs")


def check_required_numbers(r, found):
    src = "资料/卡牌制作_正本.md §2·2 B 组（数值字段）"
    missing, bad = [], []
    for canon in REQ_NUMERIC:
        if canon not in found:
            missing.append(canon)
            continue
        k, v = found[canon]
        if not is_int(v):
            bad.append("%s(%s)=%r 不是整数" % (canon, k, v))
        elif v < 0:
            bad.append("%s(%s)=%d 为负" % (canon, k, v))
    if missing:
        r.error("REQ_NUMERIC", "缺少必填数值：%s" % ", ".join(missing), src)
    if bad:
        r.error("REQ_NUMERIC", "数值不合法：%s" % "；".join(bad), src)
    if not missing and not bad:
        r.ok("REQ_NUMERIC", "五个必填数值齐全且为整数：%s"
             % ", ".join("%s=%s" % (c, get(found, c)) for c in REQ_NUMERIC), src)


def check_localization(r, found, terms):
    src = "资料/卡牌制作_正本.md §5·1/§5·2 + 数据/游戏数据/官方本地化_en+zh-CN.json"
    id_info = get(found, "id")
    if not isinstance(id_info, str) or not id_info.strip():
        r.skip("LOC_NAME", "没有可用的 id ⇒ 无法算本地化键", src)
        r.skip("LOC_DESC", "没有可用的 id ⇒ 无法算本地化键", src)
        return
    id_info = id_info.strip()
    ref_name = get(found, "refNameId") or ""
    ref_desc = get(found, "cardDescriptionTextReferenceId") or ""
    if ref_name and not isinstance(ref_name, str):
        r.error("LOC_NAME", "refNameId=%r 不是字符串" % (ref_name,), src)
        ref_name = ""
    if ref_desc and not isinstance(ref_desc, str):
        r.error("LOC_DESC", "cardDescriptionTextReferenceId=%r 不是字符串" % (ref_desc,), src)
        ref_desc = ""
    k_name = "Card_Name/" + (ref_name or id_info)
    k_desc = "Card_Description/" + (ref_desc or id_info)
    for check, key, what in (("LOC_NAME", k_name, "卡名"), ("LOC_DESC", k_desc, "卡面效果")):
        v = terms.get(key)
        if not isinstance(v, list) or not v:
            r.error(check, "本地化键 %s 不存在（%s；%s）"
                    % (key, what,
                       ("refNameId 指向它" if check == "LOC_NAME" and ref_name
                        else "cardDescriptionTextReferenceId 指向它" if check == "LOC_DESC" and ref_desc
                        else "空引用 ⇒ 默认用 Card_…/<id>")), src)
            continue
        en = v[0] if len(v) > 0 else ""
        zh = v[1] if len(v) > 1 else ""
        if not (isinstance(en, str) and en.strip()):
            r.error(check, "本地化键 %s 存在但 en 为空" % key, src)
        elif not (isinstance(zh, str) and zh.strip()):
            r.warn(check, "本地化键 %s 有 en=%r，但缺 zh-CN（玩家中文界面会回退）" % (key, en), src)
        else:
            r.ok(check, "%s = %r / %r" % (key, en, zh), src)


def _guid_refs(found):
    """产出 (字段名, 位置标签, guid 或 None, 原始值)。None = 字段没提供。"""
    for field in GUID_FIELD_TYPES:
        if field == "cardAnimations":
            continue
        if field in found:
            k, v = found[field]
            yield (field, k, as_guid(v), v)
        else:
            yield (field, None, None, None)
    # cardAnimations：顶层 + cardAbilities/activeAbility 里的 cardAnimations
    top = get(found, "cardAnimations")
    if isinstance(top, list):
        for i, v in enumerate(top):
            yield ("cardAnimations", "cardAnimations[%d]" % i, as_guid(v), v)
    elif top is not None:
        yield ("cardAnimations", "cardAnimations", as_guid(top), top)
    for where, lg in iter_ability_logics(get(found, "cardAbilities"), get(found, "activeAbility")):
        for i, v in enumerate(lg.get("cardAnimations") or []):
            yield ("cardAnimations", "%s.cardAnimations[%d]" % (where, i), as_guid(v), v)


def check_guids(r, found, guid_map):
    src = "资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv（6,140 个 GUID → 资产）"
    checked = 0
    problems = 0
    missing_fields = []
    for field, where, guid, raw in _guid_refs(found):
        if where is None:
            missing_fields.append(field)
            continue
        if guid is None:
            # 字段存在但引用为空：AssetReferenceTyped 的空引用是合法的（原版 23 张卡没有立绘）
            r.warn("GUID_" + field, "%s 存在但 m_AssetGUID 为空 —— 合法，但新卡通常应有值" % where, src)
            continue
        checked += 1
        rows = guid_map.get(guid)
        if not rows:
            r.error("GUID_" + field, "%s 的 GUID %s 在 guid_map_bundles.tsv 里解不出" % (where, guid), src)
            problems += 1
            continue
        allowed = GUID_FIELD_TYPES[field]
        types = sorted({row.get("type") for row in rows})
        if not (set(types) & allowed):
            r.error("GUID_" + field, "%s 的 GUID %s 解出来是 %s，该字段期望 %s"
                    % (where, guid, "/".join(types), "/".join(sorted(allowed))), src)
            problems += 1
            continue
        row = next(row for row in rows if row.get("type") in allowed)
        exact = row.get("exact")
        if exact != "True":
            r.warn("GUID_" + field, "%s 的 GUID %s 解出 %s（%s），但 exact=%s"
                   % (where, guid, row.get("type"), row.get("name"), exact), src)
        else:
            r.ok("GUID_" + field, "%s → %s %r（bundle=%s）"
                 % (where, row.get("type"), row.get("name"), row.get("bundle")), src)
    if missing_fields:
        r.skip("GUID_未提供", "以下表现层字段未提供、未检查：%s" % ", ".join(missing_fields), src)
    if checked == 0 and problems == 0 and not missing_fields:
        r.skip("GUID_ANY", "这张卡没有提供任何表现层 GUID ⇒ 立绘/语音/动画都没有检查", src)


def _check_one_animinfo(r, ai, label, src):
    if not isinstance(ai, dict):
        r.error("ANIMINFO", "%s 不是对象（是 %s）" % (label, type(ai).__name__), src)
        return None
    bad = []
    vals = {}
    for k in REQ_ANIM_KEYS:
        if k not in ai:
            bad.append("缺 %s" % k)
        elif not is_num(ai[k]):
            bad.append("%s=%r 不是数字" % (k, ai[k]))
        else:
            vals[k] = float(ai[k])
    if bad:
        r.error("ANIMINFO", "%s 不可解析：%s" % (label, "；".join(bad)), src)
        return None
    dur = sum(vals[k] for k in ANIM_DURATION_KEYS)
    addr = ai.get("animAdressable")
    guid = as_guid(addr)
    if addr is not None and not guid:
        r.warn("ANIMINFO", "%s 的 animAdressable.m_AssetGUID 为空（特效本体没有引用）" % label, src)
    r.ok("ANIMINFO", "%s 可解析：vfxDelayTime=%s + timeAtStartPos=%s + timeMoving=%s + timeAtEndPos=%s "
         "⇒ GetAnimDuration=%.3fs%s"
         % (label, fmt_num(vals["vfxDelayTime"]), fmt_num(vals["timeAtStartPos"]),
            fmt_num(vals["timeMoving"]), fmt_num(vals["timeAtEndPos"]), dur,
            ("；vfx GUID=%s" % guid) if guid else ""), src)
    return dur


def check_animinfo(r, found, anim_index):
    src = ("数据/游戏数据/card_anim_order.json · AnimInfo.GetAnimDuration() = "
           "vfxDelayTime+timeAtStartPos+timeMoving+timeAtEndPos（ability_anim_durations.json `_conventions.formula`）")
    did = False
    inline = get(found, "animInfo")
    if inline is not None:
        did = True
        _check_one_animinfo(r, inline, "animInfo（内联）", src)
    n_guid = 0
    for field, where, guid, raw in _guid_refs(found):
        if field != "cardAnimations" or guid is None:
            continue
        n_guid += 1
        entry = anim_index.get(guid)
        if not entry:
            r.warn("ANIMINFO", "%s 的 CardAnim %s 不在 card_anim_order.json 的 %d 条索引里 ⇒ animInfo 取不到"
                   % (where, guid, len(anim_index)), src)
            continue
        ai = entry.get("animInfo")
        if ai is None:
            r.warn("ANIMINFO", "%s 的 CardAnim %r 没有 animInfo 字段" % (where, entry.get("name")), src)
            continue
        dur = _check_one_animinfo(r, ai, "%s（CardAnim %r）" % (where, entry.get("name")), src)
        params = entry.get("params")
        if isinstance(params, dict) and dur is not None:
            pdur = sum(float(params[k]) for k in ANIM_DURATION_KEYS if is_num(params.get(k)))
            if abs(pdur - dur) > 1e-6:
                r.warn("ANIMINFO", "%s 的 params 与 animInfo 算出的时长不一致（%s vs %s）"
                       % (where, fmt_num(pdur), fmt_num(dur)), src)
        did = True
    if not did and n_guid == 0:
        r.skip("ANIMINFO", "既没有内联 animInfo，也没有可解析的 cardAnimations GUID ⇒ 未检查", src)


def validate_one(raw, ctx):
    found = normalize(raw)
    label = get(found, "id") or get(found, "name") or "<无 id>"
    r = Report(str(label))
    id_info = check_id(r, found, ctx["enums"], ctx["existing_ids"])
    check_id_prefix_army(r, id_info, found, ctx["enums"])
    check_id_unique(r, id_info, ctx["existing_ids"])
    check_enums(r, found, ctx["enums"])
    check_required_numbers(r, found)
    check_localization(r, found, ctx["terms"])
    check_guids(r, found, ctx["guid_map"])
    check_animinfo(r, found, ctx["anim_index"])
    return r


# ---------------------------------------------------------------- 输入
def load_cards(path):
    if path == "-":
        text = sys.stdin.read()
    else:
        if not os.path.isfile(path):
            die("输入文件不存在：%s" % path)
        with open(path, encoding="utf-8") as f:
            text = f.read()
    text = text.lstrip("\ufeff")
    try:
        d = json.loads(text)
    except Exception:
        d = None
    if d is not None:
        if isinstance(d, list):
            return d
        if isinstance(d, dict) and isinstance(d.get("cards"), list):
            return d["cards"]
        if isinstance(d, dict):
            return [d]
        die("输入 JSON 既不是对象、数组，也不是 {cards:[…]}")
    # JSONL 兜底
    out = []
    for i, line in enumerate(text.splitlines(), 1):
        line = line.strip()
        if not line:
            continue
        try:
            out.append(json.loads(line))
        except Exception as e:
            die("第 %d 行不是合法 JSON：%s" % (i, e))
    if not out:
        die("输入里没有任何卡片")
    return out


# ---------------------------------------------------------------- 主流程
def print_report(r):
    c = r.counts()
    print("─" * 78)
    print("卡：%s   结论：%s（错误 %d / 警告 %d / 通过 %d）"
          % (r.card, "失败 ❌" if r.failed() else "通过 ✅", c["错误"], c["警告"], c["通过"]))
    for status in STATUS_ORDER:
        items = [it for it in r.items if it["status"] == status]
        if not items:
            continue
        print("  【%s】%d 条" % (status, len(items)))
        for it in items:
            tag = "[跳过] " if it["skip"] else ""
            print("    %s[%s] %s" % (tag, it["check"], it["detail"]))
            print("           出处: %s" % it["source"])


CHECK_CATALOG = [
    ("ID_FORMAT", "id 形态（前缀号 / 32 位 hex）", "资料/卡牌制作_正本.md §1·2"),
    ("ID_PREFIX_ARMY", "id 前缀 → 阵营 1:1", "资料/卡牌制作_正本.md §1·2"),
    ("ID_UNIQUE", "id 全局唯一（卡池 + card_ids.mapping + 原版 allcards）", "资料/卡牌制作_正本.md §1·3"),
    ("ENUM_CARDTYPE", "cardType（CardTypeOptions）", "d:/2/…/CardTypeOptions.cs"),
    ("ENUM_SPELLTYPE", "spellType（SpellType）", "d:/2/…/SpellType.cs"),
    ("ENUM_RARITY", "cardRarity / rarity（CardRarity）", "d:/2/…/CardRarity.cs"),
    ("ENUM_KEYWORD", "keyword / subtype（Keyword 兵种）", "d:/2/…/Keyword.cs"),
    ("ENUM_ARMY", "cardArmy / faction（CardArmy）", "d:/2/…/CardArmy.cs"),
    ("ENUM_TRIGGER", "cardAbilities[].trigger（AbilityTrigger，97 值）", "d:/2/…/AbilityTrigger.cs"),
    ("ENUM_EFFECT", "abilityLogic[].chosenAbility（AbilityEffect，83 值）", "d:/2/…/AbilityEffect.cs"),
    ("ENUM_CONTEXT/PRIORITY", "abilityContext / priority（警告级）", "d:/2/…/AbilityContext.cs · ActionPriority.cs"),
    ("ABILITY_SHAPE", "cardAbilities / abilityLogic 的容器形状", "资料/卡牌制作_正本.md §2·4"),
    ("REQ_NUMERIC", "必填数值 cost/attack/ranged/health/armour", "资料/卡牌制作_正本.md §2·2 B 组"),
    ("LOC_NAME", "Card_Name/<id>（或 refNameId 指向的键）", "正本 §5·1/§5·2 + 官方本地化表"),
    ("LOC_DESC", "Card_Description/<id>（或 cardDescriptionTextReferenceId）", "正本 §5·1/§5·2 + 官方本地化表"),
    ("GUID_*", "立绘/语音/动画 GUID 能在 guid_map_bundles.tsv 解出且类型匹配", "资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv"),
    ("ANIMINFO", "animInfo 可解析（五个数值字段 + GetAnimDuration）", "数据/游戏数据/card_anim_order.json"),
]


def main(argv=None):
    ap = argparse.ArgumentParser(description="新卡校验器（错误 / 警告 / 通过，每条带判据出处）")
    ap.add_argument("--card", default=None, help="卡片 JSON / JSONL / {cards:[…]}；`-` = stdin")
    ap.add_argument("--out", default=None, help="机读报告输出（JSON）；**只在没有错误时写**")
    ap.add_argument("--list-checks", action="store_true", help="打印检查项目录与判据出处")
    args = ap.parse_args(argv)

    if args.list_checks:
        print("检查项（%d 组）:" % len(CHECK_CATALOG))
        for name, what, src in CHECK_CATALOG:
            print("  %-22s %s\n      出处: %s" % (name, what, src))
        return 0
    if not args.card:
        die("必须给 --card <路径>（或 --list-checks）")
    if args.out and is_under(args.out, FORBIDDEN_WRITE_ROOT):
        die("⛔ 拒绝写 d:/2（--out=%s）" % args.out)

    enums = Enums()
    ctx = {
        "enums": enums,
        "existing_ids": load_existing_ids(),
        "terms": load_loc(),
        "guid_map": load_guid_map(),
        "anim_index": load_anim_anims(),
    }
    cards = load_cards(args.card)

    print("判据装载：枚举 %s；本地化 %d 条；GUID %d 个；CardAnim %d 条；id 表 %d 张"
          % (enums.describe(), len(ctx["terms"]), len(ctx["guid_map"]),
             len(ctx["anim_index"]), len(ctx["existing_ids"])))

    reports = [validate_one(c, ctx) for c in cards]
    for r in reports:
        print_report(r)

    n_err = sum(r.counts()["错误"] for r in reports)
    n_warn = sum(r.counts()["警告"] for r in reports)
    n_ok = sum(r.counts()["通过"] for r in reports)
    failed = [r for r in reports if r.failed()]
    print("=" * 78)
    print("合计：%d 张卡 · 错误 %d · 警告 %d · 通过 %d ⇒ %s"
          % (len(reports), n_err, n_warn, n_ok,
             ("失败 ❌（%d 张有错误）" % len(failed)) if failed else "全部通过 ✅"))

    if args.out:
        if failed:
            print("⛔ 有错误 ⇒ **不写产物**（%s）" % args.out)
        else:
            payload = {"schema": SCHEMA, "count": len(reports),
                       "counts": {"错误": n_err, "警告": n_warn, "通过": n_ok},
                       "cards": [r.to_dict() for r in reports]}
            tmp = args.out + ".tmp"
            with open(tmp, "w", encoding="utf-8", newline="\n") as f:
                json.dump(payload, f, ensure_ascii=False, indent=1)
            os.replace(tmp, args.out)
            print("产物：%s" % args.out)
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
