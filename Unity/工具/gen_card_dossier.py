#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_card_dossier.py —— **卡牌总索引**（用户 2026-11-02 点名；用途 = 以后做 **mod 卡牌** 时查
「某张卡对应什么代码 / 数值 / 特效 / 动画 / 语音」）。

跑法（🔴 写盘脚本必须有显式动作档，见 CLAUDE.md §三 2026-10-26 那条）：
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --card ASH83          # 单卡全貌（人读，打印）
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --all --dry           # 全池读数，**零写盘**
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --all --check         # 与盘上 TSV 逐字节比（一致 rc=0 / 不一致 rc=1）
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --all --write         # 写 判据/卡牌总索引.tsv（**唯一会写盘的档**）
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --json --out <路径>    # 机器可读导出
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --json                # 打印到 stdout（--out 缺省）
    ⛔ 一个档都不给（或不认识）= **只打印用法并退出 2**，不计算、不写盘。

产出的 TSV 以 `#` 开头的行是**元信息**（生成方式 / 数据源版本 / 复跑命令），消费方跳过 `#` 行后
第一行是列名行。列名**稳定**（mod 脚本按列名取），改列名要在本文件头写清并同步报告。

⚠️ 本脚本**只读**：不写任何数据源、不重算别人已经算过的判定。
   特效/动画/语音的「在工程 / 在消费表」**直接复用** `判据/全池事件覆盖_静态.tsv`（`B118-W13` 的产物），
   ⛔ 这里**不另算一套规则**。

数据源（逐列出处见 `--all` 产出的文件头与报告 §2）：
    MyGame/Assets/RuleEngine/Resources/cards_engine.json            身份 + trigger/trait 面 + animTimes（v47 · count 1129）
    MyGame/Assets/RuleEngine/Resources/cards_truth.json             效果树 abilities/activeAbility/alternativeAbility（1,351 张卡 SO）
    MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json  逐卡事件 → prefab/CardAnim（运行时那份）
    MyGame/Assets/CardPresentation/Resources/cardanim_clips.json    逐 CardAnim 的动画参数
    MyGame/Assets/CardPresentation/Resources/voice_lines.json       语音 cue 归属
    资料/完全复刻/判据/全池事件覆盖_静态.tsv                        「在工程 / 在消费表 / 参数判定」（只读复用）
    资料/完全复刻/判据/全卡核验_判定.tsv                            四维核验判定（引擎/特效/动画/语音）
    资料/完全复刻/判据/**                                          该卡有专项判据文档的，列文件名
    MyGame/Assets/RuleEngine/{Core,Data}/*.cs                       代码落点（入口现读行号）+ 逐卡专项特判

🔴 两种「口径」写死在这里，⛔ 别在别处再开一套：
  ① **统计口径**：`cards_raw.json` 每卡有两份同构技能树（解析视图 + `full_raw`）⇒ 递归扫会**翻倍**，
     本工具**不读**它。
  ② `cards_engine.json` 现读 **1129** 张，`全卡核验总表.tsv` 骨架 **1128** 张 —— 差 1 张
     `Reconnaissance Mission`（`e8cf1eb55ca41534abe38efc90b302e9`，老口径没进骨架）。
     本工具**以 cards_engine 的 1129 为准**，⛔ 不扩表、不给骨架补行。
"""

import io
import json
import os
import re
import sys
from collections import Counter, defaultdict

TOOL_DIR = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.dirname(TOOL_DIR)              # D:/4/Unity
ROOT = os.path.dirname(BASE)                  # D:/4

P_ENGINE = os.path.join(BASE, "MyGame", "Assets", "RuleEngine", "Resources", "cards_engine.json")
P_TRUTH = os.path.join(BASE, "MyGame", "Assets", "RuleEngine", "Resources", "cards_truth.json")
P_VFX = os.path.join(BASE, "MyGame", "Assets", "CardPresentation", "Resources", "card_vfx_by_card.json")
P_CLIPS = os.path.join(BASE, "MyGame", "Assets", "CardPresentation", "Resources", "cardanim_clips.json")
P_VOICE = os.path.join(BASE, "MyGame", "Assets", "CardPresentation", "Resources", "voice_lines.json")
VO_DIR = os.path.join(BASE, "MyGame", "Assets", "CardPresentation", "Resources", "Art", "audio", "vo")

CRIT_DIR = os.path.join(BASE, "资料", "完全复刻", "判据")
P_EVT = os.path.join(CRIT_DIR, "全池事件覆盖_静态.tsv")
P_VERIFY = os.path.join(CRIT_DIR, "全卡核验_判定.tsv")
P_OUT = os.path.join(CRIT_DIR, "卡牌总索引.tsv")

SRC_DIRS = [os.path.join(BASE, "MyGame", "Assets", "RuleEngine", "Core"),
            os.path.join(BASE, "MyGame", "Assets", "RuleEngine", "Data")]

# 原版枚举（⛔ 不抄表，每次都从签名桩现读；读不到就退回裸数字并在输出里写「查不到」）
ENUM_SRC = os.path.join("D:/2", "Warpforge_code", "Scripts", "Assembly-CSharp")

# 代码落点：符号锚（行号**现读**，⛔ 不写死 —— 源码会动，写死就过期）
CODE_ENTRIES = [
    ("CardDef",                       "MyGame/Assets/RuleEngine/Core/CardDef.cs",
     r"public\s+partial\s+class\s+CardDef\b"),
    ("CardDef.AddTriggerOp",          "MyGame/Assets/RuleEngine/Core/CardDef.cs",
     r"\bvoid\s+AddTriggerOp\s*\("),
    ("EffectText",                    "MyGame/Assets/RuleEngine/Core/EffectText.cs",
     r"public\s+static\s+class\s+EffectText\b"),
    ("EffectText.Parse",              "MyGame/Assets/RuleEngine/Core/EffectText.cs",
     r"public\s+static\s+List<EffectOp>\s+Parse\s*\("),
    ("EffectResolver.EffectDispatch", "MyGame/Assets/RuleEngine/Core/EffectResolver.cs",
     r"Dictionary<string,\s*EffectHandler>\s+EffectDispatch\b"),
    ("VfxMap.ByCard",                 "MyGame/Assets/CardPresentation/Core/VfxMap.cs",
     r"Dictionary<string,\s*Dictionary<string,\s*List<CardPick>>>\s+ByCard\b"),
    ("CardAnimClips",                 "MyGame/Assets/CardPresentation/Battle/CardAnimClips.cs",
     r"public\s+static\s+class\s+CardAnimClips\b"),
    ("UnitTweenTable",                "MyGame/Assets/CardPresentation/Core/UnitTweenTable.cs",
     r"public\s+static\s+class\s+UnitTweenTable\b"),
    ("VoiceLines",                    "MyGame/Assets/CardPresentation/Core/VoiceLines.cs",
     r"public\s+static\s+class\s+VoiceLines\b"),
]

# 「批量数据表」阈值：一个文件里出现 ≥N 个**不同的池内卡 id** ⇒ 那是数据表（id 别名表之类），
# 不是「逐卡特判」⇒ 整表排掉并**明说排掉了谁**（铁律 2：别把「排掉了」写成「没有」）。
BULK_TABLE_MIN = 100

USAGE = """用法：python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py <档>
  --card <卡id|英文卡名>   单卡全貌（人读；分区 + 每段带 文件:行号 / 数据源）
  --all --dry              全池读数（多少张、每维覆盖多少、三态分布）· 零写盘
  --all --check            与盘上 `判据/卡牌总索引.tsv` 逐字节比（一致 rc=0 / 不一致 rc=1）· 零写盘
  --all --write            写 `判据/卡牌总索引.tsv`（**唯一会写盘的档**）
  --json [--out <路径>]    机器可读导出（缺 --out = 打印到 stdout）
  ⛔ 裸跑 = 打印本用法并退出 2，不计算、不写盘。"""


# ---------------------------------------------------------------- 基础
def read_json(path):
    return json.load(io.open(path, "r", encoding="utf-8"))


def read_tsv(path):
    """TSV：UTF-8，按 '\\n' 切（⛔ 不用 csv 模块，见 CLAUDE.md），去行尾 '\\r'。"""
    raw = io.open(path, "r", encoding="utf-8", newline="").read()
    lines = raw.split("\n")
    if lines and lines[-1] == "":
        lines.pop()
    return [ln.rstrip("\r").split("\t") for ln in lines]


def rel(p):
    """绝对路径 → 相对 D:/4/Unity 的正斜杠路径（输出里统一这么写）。"""
    return os.path.relpath(p, BASE).replace("\\", "/")


def one_line(s, cap=400):
    if s is None:
        return ""
    s = str(s).replace("\t", " ").replace("\r", " ").replace("\n", " ")
    s = re.sub(r"\s+", " ", s).strip()
    return s if len(s) <= cap else s[:cap] + "…"


CELL_CAP = 20000      # 结构化列的单元格上限：只防病态，不做业务截断（⛔ 400 那种会静默吃掉半张卡的能力）


def one_line_c(s):
    """结构化单元格：只去 tab/换行，其余原样（供 mod 脚本按分隔符自己切）。"""
    return one_line(s, CELL_CAP)


def tri(state, text):
    """三态前缀：有 / 无 / 查不到（⛔ 不许用空串糊过去）。"""
    return "%s:%s" % (state, text)


# ---------------------------------------------------------------- 原版枚举（现读）
def load_enum(name):
    """从 `D:/2/Warpforge_code/Scripts/Assembly-CSharp/<name>.cs` 读 `name = 值` 对。
    读不到 ⇒ 返回空表（调用方会把裸数字打出来 + 标「查不到枚举名」）。"""
    p = os.path.join(ENUM_SRC, name + ".cs")
    if not os.path.exists(p):
        return {}
    txt = io.open(p, "r", encoding="utf-8", errors="replace").read()
    out = {}
    for m in re.finditer(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*,?\s*$", txt, re.M):
        out[int(m.group(2))] = m.group(1)
    return out


# ---------------------------------------------------------------- 载入
class Data(object):
    def __init__(self):
        self.engine_raw = read_json(P_ENGINE)
        self.engine = self.engine_raw["cards"]
        self.eidx = {c["id"]: c for c in self.engine}
        self.engine_pos = {c["id"]: i for i, c in enumerate(self.engine)}

        t = read_json(P_TRUTH)
        self.truth_meta = {k: t.get(k) for k in ("count", "generatedAt", "source")}
        self.truth = {c["id"]: c for c in t["cards"]}

        v = read_json(P_VFX)
        self.vfx_meta = {k: v.get(k) for k in ("generatedBy", "generatedAt", "cardCount", "entryCount")}
        self.vfx = {c["id"]: c for c in v["cards"]}
        self.vfx_notplayed = defaultdict(list)
        for r in v.get("notPlayed") or []:
            self.vfx_notplayed[r.get("id")].append(r)

        c = read_json(P_CLIPS)
        self.clips = {r["a"]: r for r in c["rows"]}

        vo = read_json(P_VOICE)
        self.voice = {c0["id"]: c0 for c0 in vo["cards"]}

        # 「在工程 / 在消费表」判定：只读复用（⛔ 不另算一套）
        self.evt = defaultdict(list)          # (id, 维) -> rows
        ev_rows = read_tsv(P_EVT)
        self.evt_header = ev_rows[0]
        for r in ev_rows[1:]:
            if len(r) >= 9:
                self.evt[(r[0], r[1])].append(r)

        self.verify = {}
        for r in read_tsv(P_VERIFY)[1:]:
            if len(r) >= 12:
                self.verify[r[0]] = r

        # trait id → 名（全池 cardTraits 汇总）
        self.trait_name = {}
        for c0 in self.truth.values():
            for tr in c0.get("cardTraits") or []:
                tid = tr.get("_trait")
                if tid is not None and tid not in self.trait_name:
                    self.trait_name[tid] = tr.get("name") or "?"

        self.vo_files = set(os.listdir(VO_DIR)) if os.path.isdir(VO_DIR) else set()

        self.trig = load_enum("AbilityTrigger")
        self.ta = load_enum("TargetsAffected")
        self.pa = load_enum("PlayersAffected")
        self.buff = load_enum("BuffType")
        self.eff = load_enum("AbilityEffect")
        self.enum_missing = [n for n, tbl in (("AbilityTrigger", self.trig), ("TargetsAffected", self.ta),
                                              ("PlayersAffected", self.pa), ("BuffType", self.buff),
                                              ("AbilityEffect", self.eff)) if not tbl]

        self.bulk_tables = []       # 被整表排除的代码数据表（懒加载时填）
        self._entry = None          # 代码落点行号（懒加载）
        self._special = None        # 逐卡特判（懒加载）
        self._crit = None           # 判据文档索引（懒加载）

    # ---------------------------------------------------------- 枚举渲染
    def e(self, tbl, val, fallback="?"):
        if val is None:
            return "—"
        name = tbl.get(val)
        return "%s(%d)" % (name, val) if name else "%d(枚举名查不到)" % val

    # ---------------------------------------------------------- 代码落点
    def code_entries(self):
        if self._entry is not None:
            return self._entry
        out = []
        for label, relp, pat in CODE_ENTRIES:
            p = os.path.join(BASE, relp.replace("/", os.sep))
            line = None
            if os.path.exists(p):
                try:
                    txt = io.open(p, "r", encoding="utf-8", errors="replace").read()
                    m = re.search(pat, txt)
                    if m:
                        line = txt.count("\n", 0, m.start()) + 1
                except Exception:
                    line = None
            out.append((label, relp, line))
        self._entry = out
        return out

    # ---------------------------------------------------------- 逐卡特判
    def special_cases(self):
        """扫 RuleEngine/{Core,Data} 的 .cs，找「引用了这个卡 id」的地方。
        返回 (idx, bulk_files)：idx[cardId] = [(文件, 行号, 行文本)]；bulk_files = 被整表排掉的数据表。"""
        if self._special is not None:
            return self._special
        tokre = re.compile(r'"([^"\\\n]{1,64})"')
        per_file_ids = {}
        for d in SRC_DIRS:
            for dp, _, fs in os.walk(d):
                for fn in fs:
                    if not fn.endswith(".cs"):
                        continue
                    p = os.path.join(dp, fn)
                    try:
                        txt = io.open(p, "r", encoding="utf-8", errors="replace").read()
                    except Exception:
                        continue
                    ids = set(t for t in tokre.findall(txt) if t in self.eidx)
                    if ids:
                        per_file_ids[p] = ids
        bulk = set(p for p, ids in per_file_ids.items() if len(ids) >= BULK_TABLE_MIN)
        idx = defaultdict(list)
        for p, ids in per_file_ids.items():
            if p in bulk:
                continue
            txt = io.open(p, "r", encoding="utf-8", errors="replace").read()
            for i, line in enumerate(txt.split("\n"), 1):
                if '"' not in line:
                    continue
                hit = [t for t in tokre.findall(line) if t in ids]
                if not hit:
                    continue
                for cid in hit:
                    idx[cid].append((rel(p), i, one_line(line.strip(), 150)))
        self.bulk_tables = [rel(p) for p in sorted(bulk)]
        self._special = (idx, self.bulk_tables)
        return self._special

    # ---------------------------------------------------------- 判据文档
    def criteria_docs(self):
        """一遍扫 `判据/` 全部文本，按「文件里出现了这个卡的 id 原文」建反查表。
        ⛔ 不做模糊匹配 —— 只认 id 字面量（大小写敏感、前后不能接字母数字）。"""
        if self._crit is not None:
            return self._crit
        tokre = re.compile(r"(?<![0-9A-Za-z_])([A-Z]{2,4}[0-9]{1,3}|[0-9a-f]{32})(?![0-9A-Za-z_])")
        per_file = {}
        for dp, _, fs in os.walk(CRIT_DIR):
            for fn in fs:
                if not (fn.endswith(".md") or fn.endswith(".tsv") or fn.endswith(".txt")):
                    continue
                p = os.path.join(dp, fn)
                if os.path.abspath(p) == os.path.abspath(P_OUT):
                    continue          # ⛔ 自己的产物不进判据索引（否则第一次写盘后自指）
                try:
                    txt = io.open(p, "r", encoding="utf-8", errors="replace").read()
                except Exception:
                    continue
                ids = set(t for t in tokre.findall(txt) if t in self.eidx)
                if ids:
                    per_file[p] = ids
        # 「全池表」= 一个文件里出现 ≥BULK_TABLE_MIN 个不同卡 id（全池对拍/覆盖/核验那类）
        # ⇒ 它对每张卡都成立、不是「这张卡的专项文档」⇒ 单列一份名单，⛔ 不糊进每张卡。
        self._crit_bulk = sorted(rel(p) for p, ids in per_file.items() if len(ids) >= BULK_TABLE_MIN)
        idx = defaultdict(list)
        for p, ids in per_file.items():
            if len(ids) >= BULK_TABLE_MIN:
                continue
            for t in ids:
                idx[t].append(rel(p))
        self._crit = {k: sorted(set(v)) for k, v in idx.items()}
        return self._crit

    def criteria_bulk(self):
        self.criteria_docs()
        return self._crit_bulk

    # ---------------------------------------------------------- ops（能力 → op 行）
    def trait_of(self, tid):
        if not tid:
            return "—"
        return "%s(%s)" % (self.trait_name.get(tid, "?"), tid)

    def buff_desc(self, b):
        parts = []
        bt = b.get("buffType")
        parts.append("buffType " + (self.e(self.buff, bt) if bt is not None else "缺键（原版这行没写 buffType）"))
        for k, lab in (("meleeAttackChange", "melee"), ("rangeAttackChange", "ranged"),
                       ("healthChange", "health"), ("armourChange", "armour"),
                       ("costChange", "cost")):
            if b.get(k):
                parts.append("%s%+d" % (lab, b[k]))
        if b.get("trait") or b.get("traitValue"):
            parts.append("trait %s v%s" % (self.trait_of(b.get("trait")), b.get("traitValue", 0)))
        dur = [k for k in ("untilEndOfTurn", "untilPlayerTurnStart", "untilEnemyTurnStart",
                           "untilNextAttack", "untilFollowingPlayerTurnStart", "permanentBuff")
               if b.get(k)]
        if dur:
            parts.append("时长=" + "/".join(dur))
        if b.get("enchantment"):
            parts.append("enchantment PPtr(附魔模板)")
        if b.get("effectName"):
            parts.append("effectName=%s" % one_line(b.get("effectName"), 40))
        if b.get("doNotStack"):
            parts.append("doNotStack")
        return " ".join(parts)

    def op_line(self, lg):
        """一条 `abilityLogic[j]` → 一行「这行数据被解析成什么 op」。"""
        parts = []
        if "chosenAbility" in lg:
            parts.append("chosenAbility " + self.e(self.eff, lg.get("chosenAbility")))
        if lg.get("playersAffected") is not None:
            parts.append("playersAffected " + self.e(self.pa, lg.get("playersAffected")))
        tc = lg.get("targetCriteria")
        if tc:
            ta = tc.get("targetsAffected")
            extra = ["%s=%s" % (k, v) for k, v in tc.items() if k != "targetsAffected" and v not in (0, [], None)]
            parts.append("ta " + self.e(self.ta, ta) + ((" [" + ",".join(extra) + "]") if extra else ""))
        if "damage" in lg:
            d = lg["damage"] or {}
            parts.append("damage %s-%s type=%s opt=%s" % (d.get("minDamage"), d.get("maxDamage"),
                                                          d.get("damageType"), d.get("valueOptions")))
        if "heal" in lg:
            h = lg["heal"] or {}
            parts.append("heal %s-%s" % (h.get("minValue"), h.get("maxValue")))
        if "buff" in lg:
            parts.append("buff " + self.buff_desc(lg["buff"] or {}))
        if "handBuff" in lg:
            hb = lg["handBuff"] or {}
            ce = hb.get("cardEffect") or {}
            parts.append("handBuff " + self.buff_desc(ce) +
                         (" uses=%s/%s" % (hb.get("numberOfUses"), hb.get("limitedUses")) if hb.get("limitedUses") else ""))
        if "manaBuff" in lg:
            mb = lg["manaBuff"] or {}
            flags = [k for k in ("friendlyPlayer", "enemyPlayer", "createMana", "refillMana",
                                 "spiritStoneMana", "questPoints", "faith") if mb.get(k)]
            parts.append("manaBuff +%s %s" % (mb.get("manaAmount"), "/".join(flags)))
        if "summon" in lg:
            s = lg["summon"] or {}
            us = s.get("unitsToSummon") or []
            names = [("%s=%s" % (u, self.truth.get(u, {}).get("name") or self.eidx.get(u, {}).get("name") or "?"))
                     for u in us]
            parts.append("summon " + ",".join(names))
        if lg.get("multiplier") is not None:
            parts.append("multiplier=%s" % lg.get("multiplier"))
        if lg.get("furtherMultiplier") is not None:
            parts.append("furtherMultiplier=%s" % lg.get("furtherMultiplier"))
        if lg.get("chanceOfHappening") is not None:
            parts.append("chanceOfHappening=%s%%" % lg.get("chanceOfHappening"))
        return " · ".join(parts) if parts else "（空行：原版这行没有 payload）"

    def ability_block(self, ab, label):
        """一个 ability 对象 → [(标题, [op 行])]。"""
        trig = ab.get("trigger")
        head = "%s trigger %s" % (label, self.e(self.trig, trig) if trig is not None else "(该 ability 无 trigger 键)")
        if ab.get("triggerValue") is not None:
            head += " triggerValue=%s" % ab.get("triggerValue")
        if ab.get("abilityContext") is not None:
            head += " abilityContext=%s" % ab.get("abilityContext")
        for k in ("manaCost", "plasmaCost", "psychicCost"):
            if ab.get(k):
                head += " %s=%s" % (k, ab[k])
        if ab.get("targeted"):
            head += " targeted=1"
        cond = []
        for k in ("actingCardCriteria", "boardCriteria", "multiCountCriteria"):
            v = ab.get(k)
            if isinstance(v, dict):
                nz = ["%s=%s" % (a, b) for a, b in v.items() if b not in (0, [], None)]
                if nz:
                    cond.append(k + "[" + ",".join(nz) + "]")
        if cond:
            head += " 条件:" + " ".join(cond)
        return head, [self.op_line(lg) for lg in (ab.get("logic") or [])]


# ---------------------------------------------------------------- 一张卡的档案
def build(D, cid):
    eng = D.eidx.get(cid) or {}
    tru = D.truth.get(cid) or {}
    rec = {"id": cid, "gaps": []}

    # ---- 1 身份
    rec["name"] = eng.get("name") or tru.get("name") or "?"
    rec["nameZh"] = eng.get("nameZh") or ""
    if not rec["nameZh"]:
        rec["gaps"].append("中文名：cards_engine.json 该卡无 nameZh（全池仅 2 张如此）")
    rec["faction"] = eng.get("faction") or "?"
    rec["subtype"] = eng.get("subtype") or tri("无", "cards_engine.subtype 空串（现池 13 张：tactic ×12 + enchantment ×1）")
    rec["cardType"] = eng.get("type") or "?"
    rec["rarity"] = eng.get("rarity") or ""
    rec["cost"] = eng.get("cost")
    rec["melee"] = eng.get("attack")
    rec["ranged"] = eng.get("ranged")
    rec["armour"] = eng.get("armour")
    rec["health"] = eng.get("health")
    kws = list(eng.get("keywords") or [])
    if eng.get("cardTraits"):
        for t in eng["cardTraits"]:
            if t not in kws:
                kws.append(t)
    rec["keywords"] = kws
    traits = []
    for tr in (tru.get("cardTraits") or []):
        traits.append("%s(%s)" % (tr.get("name"), tr.get("_trait")))
    rec["traits"] = traits
    _nod = tri("无", "cards_engine.desc 空串 = 原版这张卡没有效果文字（现池 8 张，全是白板单位/附魔）")
    rec["desc"] = eng.get("desc") or _nod
    rec["descZh"] = eng.get("descZh") or eng.get("desc") or _nod

    # ---- 2 能力
    abilities = []
    if not tru:
        rec["gaps"].append("效果树：cards_truth.json 无该 id（已查 cards_truth 的 1,351 张）")
        rec["abilities"] = []
        rec["abilities_state"] = "查不到"
        rec["abilities_note"] = "cards_truth.json 无该 id（该表覆盖 1,351 张卡 SO）"
    else:
        for label, key in (("abilities", "abilities"), ("activeAbility", "activeAbility"),
                           ("alternativeAbility", "alternativeAbility")):
            v = tru.get(key)
            if not v:
                continue
            if isinstance(v, dict):
                v = [v]
            for k, ab in enumerate(v):
                if isinstance(ab, dict):
                    abilities.append(D.ability_block(ab, "%s[%d]" % (label, k)))
        rec["abilities"] = abilities
        if not abilities:
            rec["abilities_state"] = "无"
            rec["abilities_note"] = "cards_truth.json 该卡 abilities/activeAbility/alternativeAbility 全为空（原版数据如此）"
            rec["gaps"].append("能力：cards_truth.json 该卡三处能力全为空 ⇒ 无能力（原版数据如此）")

    # ---- 3 特效
    vfx_rows = D.evt.get((cid, "特效"), [])
    vfx_decl = []
    ve = D.vfx.get(cid)
    if ve:
        for evd in ve.get("events") or []:
            ev = evd.get("ev")
            for es in evd.get("es") or []:
                vfx_decl.append((ev, es.get("n"), es.get("c"), es.get("a")))
    if vfx_decl:
        rec["vfx_state"] = "有"
        rec["vfx"] = vfx_decl
        rec["vfx_rows"] = vfx_rows
    elif ve:
        rec["vfx_state"] = "无"
        rec["vfx_note"] = "card_vfx_by_card.json 有这张卡但 events 为空（原版没声明逐卡特效）"
        rec["vfx"] = []
        rec["vfx_rows"] = []
    else:
        rec["vfx_state"] = "查不到"
        rec["vfx_note"] = ("card_vfx_by_card.json 未收录该 id（已查该表 %d 张卡的 events 键）；"
                           "已查 全池事件覆盖_静态.tsv 的 维=特效 行（该表覆盖 %d 张卡）"
                           % (len(D.vfx), len(set(k[0] for k in D.evt))))
        rec["vfx"] = []
        rec["vfx_rows"] = vfx_rows
    rec["vfx_notplayed"] = D.vfx_notplayed.get(cid, [])

    # ---- 4 动画
    anim = eng.get("animTimes") or []
    rec["anim"] = anim
    if anim:
        rec["anim_state"] = "有"
    else:
        rec["anim_state"] = "无"
        rec["anim_note"] = "cards_engine.json 的 animTimes 为空数组（原版该卡没声明动画）"
    rec["anim_rows"] = D.evt.get((cid, "动画"), [])

    # ---- 5 语音
    vl = D.voice.get(cid)
    lines = (vl or {}).get("lines") or []
    rec["voice"] = lines
    rec["voice_rows"] = D.evt.get((cid, "语音"), [])
    if lines:
        rec["voice_state"] = "有"
    else:
        vaud = (ve or {}).get("audio") or []
        if vaud:
            rec["voice_state"] = "有"
            rec["voice"] = [{"ev": a.get("ev"), "file": a.get("n"), "src": "card_vfx_by_card.audio"} for a in vaud]
        else:
            rec["voice_state"] = "无"
            rec["voice_note"] = ("voice_lines.json 无该 id 的登记（该表 %d 张卡有行）；"
                                 "card_vfx_by_card.json 该卡的 audio 也为空" % len(D.voice))

    # ---- 6 代码落点 / 特判
    sidx, bulk = D.special_cases()
    rec["special"] = sidx.get(cid, [])
    rec["bulk_tables"] = bulk

    # ---- 7 判据文档
    rec["crit_docs"] = D.criteria_docs().get(cid, [])

    # ---- 8 核验判定
    vr = D.verify.get(cid)
    rec["verify"] = {"引擎": vr[4], "特效": vr[6], "动画": vr[8], "语音": vr[10]} if vr else None
    if vr is None:
        rec["gaps"].append("四维核验：全卡核验_判定.tsv 无该 id")

    return rec


def verify_tsv_cell(rec):
    if not rec.get("verify"):
        return tri("查不到", "全卡核验_判定.tsv 无该行")
    v = rec["verify"]
    return "引擎%s/特效%s/动画%s/语音%s" % (v["引擎"], v["特效"], v["动画"], v["语音"])


def vfx_cell(rec):
    if rec["vfx_state"] != "有":
        return tri(rec["vfx_state"], rec.get("vfx_note", ""))
    rows = {}
    for r in rec["vfx_rows"]:
        rows[(r[2], r[3])] = r
    out = []
    for ev, n, conf, anim in rec["vfx"]:
        r = rows.get((ev, n))
        st = ("[工程%s/消费表%s/参数%s]" % (r[4], r[5], r[7])) if r else "[该表无此事件行]"
        out.append("%s→%s(%s)%s" % (ev, n, conf, st))
    if not rec["vfx_rows"]:
        out.append("⚠ 全池事件覆盖_静态.tsv 无该卡的特效行（该表覆盖 1,100 张卡）")
    return "有(" + str(len(rec["vfx"])) + "条): " + " ; ".join(out)


def anim_cell(rec):
    """覆盖表的「动画」行**不是按 CardAnim 名连的** —— 列序是
    `卡id | 维=动画 | 事件=<trigger 名 = animTimes.tn> | 资源名=<prefab = animTimes.n> | …`
    （⛔ 别拿 `animTimes.a` 去和它 join，那会一条都配不上）。"""
    if rec["anim_state"] != "有":
        return tri("无", rec.get("anim_note", ""))
    clips = rec.get("_clips") or {}
    rows = {}
    for r in rec["anim_rows"]:
        rows[(r[2], r[3])] = r
    out = []
    miss = 0
    for a in rec["anim"]:
        clip = a.get("a") or ""
        prefab = a.get("n") or ""
        tg = a.get("tg")
        # 覆盖表「动画」行的资源名列 = prefab 名；`Wait_*` 那种纯延时占位没有 prefab，
        # 那张表把 CardAnim 名写进同一列 ⇒ 连接键取 `n or a`（⛔ 别只拿 n）。
        r = rows.get((a.get("tn") or "", prefab or clip))
        if r is None:
            miss += 1
            cov = "覆盖表无行"
        else:
            cov = "工程%s/消费表%s/参数%s" % (r[4], r[5], r[7])
        if clip.startswith("Wait_"):
            mark = "纯延时占位（原版 animInfo.animAdressable 的 GUID 为空、无 prefab）"
        else:
            mark = ("✓" if clip in clips else "✗不在 cardanim_clips") + "CardAnim " + clip
        out.append("%s(tg%s:%s)→%s prefab[%s]{%s}" %
                   (clip, tg, a.get("tn") or "?", mark, prefab or "—", cov))
    return "有(" + str(len(rec["anim"])) + "条): " + " ; ".join(out) + \
        ("" if miss == 0 else " ; ⚠%d 条在覆盖表里配不上" % miss)


def voice_cell(rec):
    if rec["voice_state"] != "有":
        return tri("无", rec.get("voice_note", ""))
    out = []
    for l in rec["voice"]:
        f = l.get("file") or ""
        out.append("%s→%s[src=%s,so=%s]" % (l.get("ev"), f, l.get("src", ""), l.get("so", "—")))
    return "有(" + str(len(rec["voice"])) + "条): " + " ; ".join(out)


COLS = ["id", "name", "nameZh", "faction", "subtype", "cardType", "rarity",
        "cost", "melee", "ranged", "armour", "health", "keywords", "traits",
        "desc", "descZh", "abilities", "vfx", "anim", "voice",
        "code_entry", "code_special", "criteria_docs", "verify", "gaps"]


def abilities_cell(rec):
    if not rec.get("abilities"):
        return tri(rec.get("abilities_state", "无"), rec.get("abilities_note", ""))
    out = []
    for head, ops in rec["abilities"]:
        out.append(head + " ⇒ " + " | ".join(ops))
    return "有(%d 个 ability): " % len(rec["abilities"]) + " ;; ".join(out)


def code_entry_cell(D):
    out = []
    for label, relp, line in D.code_entries():
        out.append("%s=%s:%s" % (label, relp, line if line else "❓符号没找到"))
    return " ; ".join(out)


def row_of(D, rec, clips, bulk_crit):
    rec["_clips"] = clips
    return {
        "id": rec["id"],
        "name": rec["name"],
        "nameZh": rec["nameZh"] or tri("查不到", "cards_engine.nameZh 空"),
        "faction": rec["faction"],
        "subtype": rec["subtype"],
        "cardType": rec["cardType"],
        "rarity": rec["rarity"],
        "cost": rec["cost"],
        "melee": rec["melee"],
        "ranged": rec["ranged"],
        "armour": rec["armour"],
        "health": rec["health"],
        "keywords": ",".join(rec["keywords"]) or "无",
        "traits": ",".join(rec["traits"]) or "无",
        "desc": one_line(rec["desc"], CELL_CAP),
        "descZh": one_line(rec["descZh"], CELL_CAP),
        "abilities": one_line_c(abilities_cell(rec)),
        "vfx": one_line_c(vfx_cell(rec)),
        "anim": one_line_c(anim_cell(rec)),
        "voice": one_line_c(voice_cell(rec)),
        "code_entry": one_line_c(code_entry_cell(D)),
        "code_special": one_line_c(" ; ".join("%s:%d %s" % (f, l, t) for f, l, t in rec["special"])
                                 or "无:RuleEngine/{Core,Data} 的 116 个 .cs 里没有以字面量引用该 id 的行"),
        "criteria_docs": one_line_c(",".join(rec["crit_docs"]) or
                                    ("无:判据/ 除了全池表（%d 份）之外没有出现该 id 字面量的专项文档" % len(bulk_crit))),
        "verify": one_line_c(verify_tsv_cell(rec)),
        "gaps": one_line_c(" ; ".join(rec["gaps"]) or "无"),
    }


# ---------------------------------------------------------------- 单卡人读输出
def print_card(D, cid):
    rec = build(D, cid)
    clips = D.clips
    line = "═" * 78
    print(line)
    print("%s · %s · %s" % (rec["id"], rec["name"], rec["nameZh"] or "（中文名查不到）"))
    print(line)

    print("\n【1 身份】 数据源 = MyGame/Assets/RuleEngine/Resources/cards_engine.json:cards[%s]"
          % D.engine_pos.get(cid, "?"))
    print("  阵营=%s  兵种=%s  卡类=%s  稀有度=%s" % (rec["faction"], rec["subtype"], rec["cardType"], rec["rarity"]))
    print("  费用=%s  近战=%s  远程=%s  护甲=%s  生命=%s" % (rec["cost"], rec["melee"], rec["ranged"], rec["armour"], rec["health"]))
    print("  关键词=%s" % (",".join(rec["keywords"]) or "无"))
    print("  traits=%s" % (",".join(rec["traits"]) or "无"))
    print("  desc  = %s" % one_line(rec["desc"], 300))
    print("  descZh= %s" % one_line(rec["descZh"], 300))

    print("\n【2 能力】 数据源 = cards_truth.json:cards[\"%s\"] 的 abilities/activeAbility/alternativeAbility" % cid)
    if not rec["abilities"]:
        print("  " + (rec["gaps"][-1] if rec["gaps"] else "无"))
    for head, ops in rec["abilities"]:
        print("  ▸ " + head)
        for op in ops:
            print("      - " + op)

    print("\n【3 特效】 数据源 = CardPresentation/Resources/card_vfx_by_card.json:cards[id=%s].events" % cid)
    print("  状态：%s" % rec["vfx_state"])
    if rec["vfx_state"] == "有":
        rows = {(r[2], r[3]): r for r in rec["vfx_rows"]}
        for ev, n, conf, anim in rec["vfx"]:
            r = rows.get((ev, n))
            st = ("在工程=%s 在消费表=%s 参数=%s 出处=%s" % (r[4], r[5], r[7], r[8])) if r else "（全池事件覆盖_静态.tsv 无此事件行）"
            print("  %-14s → prefab %-34s [%s]" % (ev, n, conf))
            print("      %s" % st)
        if not rec["vfx_rows"]:
            print("  ⚠ 全池事件覆盖_静态.tsv 无该卡的特效行")
    else:
        print("  " + rec.get("vfx_note", ""))
    for np in rec["vfx_notplayed"]:
        print("  ⛔不播 %s→%s（outcome=%s）: %s" % (np.get("ev"), np.get("n"), np.get("outcome"), one_line(np.get("why"), 120)))

    print("\n【4 动画】 数据源 = cards_engine.json:cards[id=%s].animTimes + cardanim_clips.json" % cid)
    if not rec["anim"]:
        print("  " + rec.get("anim_note", ""))
    rows = {(r[2], r[3]): r for r in rec["anim_rows"]}   # 键 = (trigger 名 tn, prefab n)
    for a in rec["anim"]:
        a_name = a.get("a") or ""
        if a_name.startswith("Wait_"):
            clip = "纯延时占位（原版无 prefab/无 clip）"
        else:
            clip = "✓在 cardanim_clips.json" if a_name in clips else "✗不在 cardanim_clips.json"
        r = rows.get((a.get("tn") or "", (a.get("n") or "") or a_name))
        cov = ("覆盖表: 在工程=%s 在消费表=%s 参数=%s" % (r[4], r[5], r[7])) if r else "覆盖表: ⚠无此动画行"
        print("  tg%s(%s) → CardAnim %s [%s] prefab %s" % (a.get("tg"), a.get("tn"), a_name, clip, a.get("n")))
        print("      d=%s sd=%s s=%s m=%s e=%s u=%s t=%s · %s" %
              (a.get("d"), a.get("sd"), a.get("s"), a.get("m"), a.get("e"), a.get("u"), a.get("t"), cov))

    print("\n【5 语音】 数据源 = CardPresentation/Resources/voice_lines.json + 覆盖表(维=语音)")
    if rec["voice_state"] != "有":
        print("  " + rec.get("voice_note", ""))
    for l in rec["voice"]:
        f = l.get("file") or ""
        exists = "✓文件在" if f in D.vo_files else "✗文件不在 Resources/Art/audio/vo"
        print("  %-10s → %s [%s] src=%s so=%s" % (l.get("ev"), f, exists, l.get("src", ""), l.get("so", "—")))

    print("\n【6 代码落点】 行号现读（符号锚，见本工具 CODE_ENTRIES）")
    for label, relp, ln in D.code_entries():
        print("  %-30s %s:%s" % (label, relp, ln if ln else "❓符号没找到"))
    print("  专项特判：")
    if rec["special"]:
        for f, l, t in rec["special"]:
            print("    %s:%d  %s" % (f, l, t))
    else:
        print("    无（扫过 RuleEngine/Core + RuleEngine/Data 的 .cs，按「引号字面量恰好等于该 id」匹配）")
    if rec["bulk_tables"]:
        print("    （整表排除的批量数据表：%s）" % ", ".join(rec["bulk_tables"]))

    print("\n【7 判据文档】 判据/ 里出现该 id 字面量的**专项**文件（全池表已单列）")
    if rec["crit_docs"]:
        for f in rec["crit_docs"][:40]:
            print("    " + f)
        if len(rec["crit_docs"]) > 40:
            print("    …共 %d 份" % len(rec["crit_docs"]))
    else:
        print("    无（扫过 判据/ 764 个 .md/.tsv/.txt，按 id 字面量精确匹配）")
    bulkc = D.criteria_bulk()
    if bulkc:
        print("    全池表（提到全池、不算这张卡的专项）：%s" % ", ".join(bulkc))

    print("\n【8 四维核验】 数据源 = 判据/全卡核验_判定.tsv")
    print("  " + verify_tsv_cell(rec))

    print("\n【9 三态体检】")
    print("  特效=%s  动画=%s  语音=%s" % (rec["vfx_state"], rec["anim_state"], rec["voice_state"]))
    if rec["gaps"]:
        for g in rec["gaps"]:
            print("  ⚠ " + g)
    print("")


# ---------------------------------------------------------------- 全池
def header_lines(D):
    ev_mtime = None
    try:
        import datetime
        ev_mtime = datetime.datetime.fromtimestamp(os.path.getmtime(P_EVT)).strftime("%Y-%m-%d %H:%M")
    except Exception:
        ev_mtime = "?"
    return [
        "# 卡牌总索引 —— 由 工具/gen_card_dossier.py 生成，⛔ 别手改（重跑会覆盖）",
        "# 复跑：python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --all --write",
        "# 数据源版本：cards_engine.json v%s count=%s | cards_truth.json count=%s generatedAt=%s"
        % (D.engine_raw.get("version"), D.engine_raw.get("count"),
           D.truth_meta.get("count"), one_line(D.truth_meta.get("generatedAt"), 40)),
        "#            card_vfx_by_card.json cardCount=%s entryCount=%s generatedAt=%s"
        % (D.vfx_meta.get("cardCount"), D.vfx_meta.get("entryCount"), one_line(D.vfx_meta.get("generatedAt"), 40)),
        "#            全池事件覆盖_静态.tsv mtime=%s（「在工程/在消费表/参数判定」列原样复用，本工具不重算）"
        % ev_mtime,
        "# 口径：本表以 cards_engine.json 的 1129 张为准（全卡核验总表.tsv 骨架 1128，差 Reconnaissance Mission）；"
        "⛔ 不用 cards_raw.json（每卡两份同构技能树，递归扫会翻倍）。",
        "# 消费：跳过 '#' 开头的行后，第一行是列名行；按列名取值（列名稳定，改动会同步报告）。",
        "# 判据全池表（≥%d 个不同卡 id 的文件，不算任何一张卡的专项文档，故 criteria_docs 列里不出现）：%s"
        % (BULK_TABLE_MIN, ", ".join(D.criteria_bulk()) or "无"),
        "# 代码批量数据表（同规则，code_special 列里不出现）：%s" % (", ".join(D.bulk_tables) or "无"),
    ]


def gen_all(D):
    clips = D.clips
    bulk_crit = D.criteria_bulk()
    rows = [row_of(D, build(D, c["id"]), clips, bulk_crit) for c in D.engine]
    return rows


def tsv_text(D, rows):
    out = list(header_lines(D))
    out.append("\t".join(COLS))
    for r in rows:
        out.append("\t".join(str(r.get(c, "")) for c in COLS))
    return "\n".join(out) + "\n"


def _state(cell):
    """从 `有(...):` / `无:...` / `查不到:...` 里取三态档。"""
    for s in ("查不到", "有", "无"):
        if cell.startswith(s):
            return s
    return "?"


def coverage_stats(D, rows):
    st = Counter()
    for r in rows:
        st["vfx_" + _state(r["vfx"])] += 1
        st["anim_" + _state(r["anim"])] += 1
        st["voice_" + _state(r["voice"])] += 1
        st["abilities_" + _state(r["abilities"])] += 1
    return st


def gen_json(D, rows):
    cards = []
    for r in rows:
        cards.append({k: r[k] for k in COLS})
    return {
        "generatedBy": "d:/4/Unity/工具/gen_card_dossier.py",
        "sources": {
            "engine": rel(P_ENGINE), "engineVersion": D.engine_raw.get("version"),
            "engineCount": D.engine_raw.get("count"),
            "truth": rel(P_TRUTH), "truthCount": D.truth_meta.get("count"),
            "vfx": rel(P_VFX), "clips": rel(P_CLIPS), "voice": rel(P_VOICE),
            "eventCoverage": rel(P_EVT), "verify": rel(P_VERIFY),
        },
        "columns": COLS,
        "count": len(cards),
        "cards": cards,
    }


# ---------------------------------------------------------------- main
def print_usage(code):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(USAGE)
    sys.exit(code)


def main(argv):
    if not argv:
        print_usage(2)
    if argv[0] in ("-h", "--help"):
        print_usage(0)

    card = None
    all_ = False
    mode = None
    out_path = None
    do_json = False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--card":
            i += 1
            if i >= len(argv):
                print_usage(2)
            card = argv[i]
        elif a == "--all":
            all_ = True
        elif a in ("--dry", "--check", "--write"):
            if mode is not None:
                print("⛔ 只能给一个动作档（%s / %s）" % (mode, a))
                print_usage(2)
            mode = a
        elif a == "--json":
            do_json = True
        elif a == "--out":
            i += 1
            if i >= len(argv):
                print_usage(2)
            out_path = argv[i]
        else:
            print("⛔ 不认识的参数：%s" % a)
            print_usage(2)
        i += 1

    D = Data()

    # 单卡
    if card:
        if card in D.eidx:
            cid = card
        else:   # 名字兜底（精确、忽略大小写）
            hit = [c["id"] for c in D.engine if (c.get("name") or "").lower() == card.lower()]
            if not hit:
                print("⛔ 卡池里没有 id 或英文名 = %r 的卡（现池 %d 张；名字匹配为精确、忽略大小写）" % (card, len(D.engine)))
                sys.exit(1)
            cid = hit[0]
        print_card(D, cid)
        return 0

    if not (all_ or do_json):
        print_usage(2)

    rows = gen_all(D)

    if do_json:
        payload = gen_json(D, rows)
        text = json.dumps(payload, ensure_ascii=False, indent=1)
        if out_path:
            d = os.path.dirname(os.path.abspath(out_path))
            if d and not os.path.isdir(d):
                os.makedirs(d)
            io.open(out_path, "w", encoding="utf-8", newline="\n").write(text + "\n")
            print("✅ 已写 %s（%d 张卡 · %d 列）" % (out_path, len(rows), len(COLS)))
        else:
            print(text)
        if mode is None:
            return 0

    if mode is None:
        print_usage(2)

    text = tsv_text(D, rows)

    if mode == "--dry":
        stats = coverage_stats(D, rows)
        print("卡数 = %d（cards_engine.json v%s）" % (len(rows), D.engine_raw.get("version")))
        for dim in ("vfx", "anim", "voice"):
            print("  %-5s 有=%-5d 无=%-5d 查不到=%d" % (dim, stats[dim + "_有"], stats[dim + "_无"], stats[dim + "_查不到"]))
        print("  能力   有=%d 无=%d" % (stats["abilities_有"], stats["abilities_无"]))
        print("  枚举名缺失: %s" % (",".join(D.enum_missing) or "无（5 个原版枚举全部现读成功）"))
        print("  TSV 行数 = %d（含 %d 行 '#' 元信息 + 1 行列名）" % (text.count("\n"), len(header_lines(D))))
        print("  零写盘（--dry）")
        return 0

    if mode == "--check":
        if not os.path.exists(P_OUT):
            print("❌ 盘上没有 %s —— 先跑 --all --write" % rel(P_OUT))
            return 1
        cur = io.open(P_OUT, "r", encoding="utf-8", newline="").read()
        if cur == text:
            print("✅ 逐字节相同（%s）" % rel(P_OUT))
            return 0
        import difflib
        d = list(difflib.unified_diff(cur.split("\n"), text.split("\n"),
                                      fromfile="盘上", tofile="本次生成", lineterm="", n=0))
        print("❌ 不同（盘上 vs 本次生成）：共 %d 行差异，前 40 行：" % len(d))
        for ln in d[:40]:
            print("   " + one_line(ln, 240))
        return 1

    # --write
    io.open(P_OUT, "w", encoding="utf-8", newline="\n").write(text)
    print("✅ 已写 %s（%d 张卡 · %d 列 · %d 行）" % (rel(P_OUT), len(rows), len(COLS), text.count("\n")))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
