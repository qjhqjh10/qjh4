# -*- coding: utf-8 -*-
"""把「普通关键词被 OCR 误抽」那一批（2026-09-21 六路子代理逐张开 PnP 卡图核过）写进
`数据/游戏数据/cardface_fixes.json` 的 `_manual_keywords` 列。

**为什么不直接改 `keywords` 列**：那一列是 `gen_cardface_fixes.py` **算出来的**
（源 = `_合并总表.md`），直接改会被下一次重跑**静默抹掉** —— 规矩见
`cardface_fixes.json` 的 `_manual_keywords` 注释。

**判据**：六份逐张核对的结论（每张卡都开了 PnP 成品图，卡面原文写进了报告）。
本脚本**只做减法**：把确认误抽的那个词从该卡的整份关键词表里去掉，其余原样。

用法：
  PYTHONIOENCODING=utf-8 python 工具/_apply_kw_misextract.py            # 干跑，只打印差异
  PYTHONIOENCODING=utf-8 python 工具/_apply_kw_misextract.py --write    # 落盘
"""
import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))      # d:/4/Unity
POOL = os.path.join(ROOT, "MyGame/Assets/RuleEngine/Resources/cards_engine.json")
FIXES = os.path.join(ROOT, "数据/游戏数据/cardface_fixes.json")
WRITE = "--write" in sys.argv

# (卡 id, 被删的词) —— 逐条来自六份开图核对报告。词按**归一化**匹配（去数字、小写）。
# ⚠️ 条目之间用 **`;`** 分隔，**不能用空格** —— 关键词本身带空格（`blood thirst` / `hunt mark` / `long range`）。
MISEXTRACT = """
GOF1|tide;GOF10|vanguard;GOF100|stomp;GOF15|fast;GOF20|flank;GOF46|flank;GOF46|vanguard;
GOF48|blood thirst;GOF52|concussive;GOF88|flank;GOF99|flank;
GOF_Dead_Choppy|blood thirst;GOF_Krump_da_Gitz|flank;GOF_Wreckin_Ball|concussive;
SW10|hunt mark;SW20|hunt mark;SW22|hunt mark;SW29|hunt mark;SW29|invulnerable;SW33|pack;
SW37|hunt mark;SW40|hunt mark;SW41|hunt mark;SW43|hunt mark;SW48|hunt mark;
SW56|blind;SW58|blood thirst;SW64|flank;
SOR29|flank;SOR45|flank;SOR45|blast;
UM10|blind;UM100|sentry;UM44|vanguard;UM49|camouflage;UM77|flank;UM78|vanguard;
UM84|oath;UM88|invulnerable;UM93|flank;UM_Angels_of_Death|flank;UM_Antaro_Chronus|flank;
UM_Light_Cover|camouflage;UM_Master_of_Arms|blast;
TAU24|vanguard;TAU27|flank;TAU31|flank;TAU48|long range;TAU50|vanguard;TAU62|markerlight;
TAU75|flank;TAU76|flank;TAU_Experimental_Drone|shield;
TL20|stealth;TL22|swarm;TL44|camouflage;TL59|blood thirst;TL60|swarm;TL75|flying;TL83|synapse;
SAU13|vanguard;SAU16|remnant;SAU49|vanguard;SAU5|remnant;
ASH16|flank;ASH28|flank;ASH42|blast;ASH42|camouflage;ASH44|invulnerable;ASH46|stealth;
ASH47|vanguard;ASH58|shuriken;ASH78|fast;ASH_Bright_Lance_Vyper|sniper;ASH_Empyric_Ambush|stealth;
ASH_Farseer|shield;ASH_Fate_Inescapable|stealth;ASH_Fate_Inescapable|vulnerable;
GSC11|sabotage;GSC12|flank;GSC27|vanguard;GSC69|sabotage;GSC70|flank;GSC70|blast;
GSC70|concussive;GSC74|stealth;GSC_Telephatic_Domination|fast;
GSC15|sabotage;GSC_Cult_Propaganda|sabotage;GSC_Improvised_Barricade|sabotage;
DA23|flank;DA24|slay;DA24|teleport;DA30|flank;DA30|stealth;DA36|fast;DA82|invulnerable;
EC14|dark pact;EC16|flank;EC18|bloodthirst;EC21|shield;EC42|dark pact;EC50|dark pact of excess;EC8|flank;
AM11|flank;AM51|duty;AM54|armour;AM54|blast;AM73|flank;AM74|flank;
BL31|vulnerable;BL4|blast;BL42|dark pact;BL8|dark pact
"""

NOTE_KEY = "_2026-09-21_普通关键词误抽"
NOTE = (
    "OCR 把**效果句里被授予/被引用的普通关键词**也记成了本卡自己的关键词（前几批清的是"
    "「带正文关键词」那一族 —— `_2026-09-18_Stun误抽` / `_2026-09-19_Ferocity误抽` / "
    "`_2026-09-19_句中引用误抽_13张`，本批是那一族**没做的另一半**，见 `资料/关键词图标_现状与总表.md`）。"
    "**114 条 / 104 张卡**，由 **6 路子代理逐张开 PnP 成品卡图核对**（卡面原文逐条抄进报告，"
    "主对话另行复核了仲裁项：`Grukk Face-Rippa` 本人开图、`Jackal Outrider` 本人开图）。"
    "⚠️ **2026-09-21 晚更正：原写「111 条 / 96 张卡」，实际那批是 111 条 / 101 张卡**"
    "（96 是错的，与本脚本实跑打印的 `命中 N 张卡` 不符），本次再 +3 条 ⇒ **114 / 104**。"
    "判据（唯一）：卡面上该词**独立成项**（图标 + 词 + `:`/`.`，或与别的关键词并列）⇒ 保留；"
    "**只出现在句子里**（被 `Give`/`Gain`/`give it`/`has`/`with`/`an enemy with` 领着，或夹在 and/逗号之间）⇒ 误抽。"
    "🔴 **影响玩法**：引擎真的读这一列（`RuleCore` 的 `UnitState.Has(...)` —— `flank`/`fast` 决定打出的当回合能不能攻击"
    "（`UnitState.cs:226`）、`stealth` 不可被选中、`vanguard` 强制先打、`invulnerable` 免伤…）"
    "⇒ 不删 = **静默给卡加一个它没有的能力**。"
    "⚠️ **不动的 5 条（核过是真关键词）**：`SOR72 Adelaide the Serene` flank/shield（`Gain ⌾Flank and ⌾Shield.` 自获且带句号独立成项）·"
    "`UM_Honour_Guard` vanguard（卡面单独一行 `⌾Vanguard`）· `TAU36 Enforcer Battlesuit` armour（盾徽值，卡面效果区没印字）·"
    "`SAU70 Lokhust Destroyer` 的 `Remnant.`/`Destroyer.`（独立成项）。"
    "🔴 **2026-09-21 晚更正（本条原来把它当成「不动」的第 5 项，是错的）**："
    "`GSC15 Poisoned Supplies` / `GSC_Cult_Propaganda` / `GSC_Improvised_Barricade` 的 `Sabotage` **已改成删** ——"
    "**错因 = 把「卡面下方那行橙字」当成了关键词的证据**，而**橙字那一行就是兵种行（`subtype`）**，"
    "不是关键词（卡面版式见 `CLAUDE.md` §七：卡名白 / 阵营橙 / 效果米白 / **兵种橙**）。"
    "一张一张开图 + 逐行像素扫描复核：三张卡**全卡面 `Sabotage` 只出现一次**，就是效果区下方那行橙字，"
    "效果区原文里没有这个词（也没有带图标的独立关键词项）。"
    "**独立佐证**：原版 `BattleCardUI.ChangeToHighlightColor`（`d:/2/tools/decomp_full/`）判破坏高亮走的是"
    "**卡类** `spellType == 0xe6(=230)`（`SpellType.Sabotage = 230`），**不是** trait/keyword ⇒ 破坏在数据上本来就该只落在 `subtype`。"
    "⚠️ **不动的 4 条「带正文关键词」**：`GOF24 Killa Kan` 的 `Mob: …` · `ASH32` 的 `Rally: …` · `ASH79` 的 `Strike: …` · `DA28` 的 `Teleport: …`。"
    "⚠️ 顺带核过：库里本来就有的**畸形值**（如 `Armour 2.` / `Remnant.` / `Destroyer.` 带尾点、`Sentry` 丢数字）本批**没动**。"
)


def nz(k):
    return re.sub(r"\s*\d+$", "", (k or "")).strip().lower()


def main():
    pairs = set()
    for t in MISEXTRACT.replace("\n", "").split(";"):
        t = t.strip()
        if not t or "|" not in t:
            continue
        cid, kw = t.split("|", 1)
        pairs.add((cid, nz(kw)))

    cards = json.load(io.open(POOL, encoding="utf-8"))["cards"]
    by_id = {c["id"]: c for c in cards}
    # 同名卡（跨阵营）必须用 `"<阵营>/<卡名>"` 键 —— 见 `load_cardface_fixes` 的注释
    name_count = {}
    for c in cards:
        name_count[c["name"]] = name_count.get(c["name"], 0) + 1

    # 🔴 **先按卡汇总要删的词，再一次性减** —— 一条一条删、每条各写一遍 `touched` 的话，
    #    同一张卡的第二条会把第一条的结果**覆盖掉**（实测：`AM54` 的 armour 与 blast 相撞）。
    by_card = {}
    for cid, kw in sorted(pairs):
        by_card.setdefault(cid, set()).add(kw)

    touched = {}
    missing = []
    for cid, kws_del in sorted(by_card.items()):
        c = by_id.get(cid)
        if c is None:
            missing.append((cid, sorted(kws_del), "池里没这张卡"))
            continue
        kws = list(c.get("keywords") or [])
        for kw in sorted(kws_del):
            idx = [i for i, k in enumerate(kws) if nz(k) == kw]
            if not idx:
                missing.append((cid, [kw], "该卡 keywords 里没有这个词（可能已被别批清掉）"))
                continue
            for i in reversed(idx):
                kws.pop(i)
        key = c["name"] if name_count[c["name"]] == 1 else (c["faction"] + "/" + c["name"])
        if key in touched:
            print("🔴 键冲突：%s（%s 与上一次）" % (key, cid))
            return 1
        touched[key] = kws
        print("  %-26s %-26s 删=%-24s 剩余=%s" % (cid, c["name"], ",".join(sorted(kws_del)), kws))

    print()
    print("命中 %d 张卡 / 请求 %d 条" % (len(touched), len(pairs)))
    if missing:
        print("⚠️ 没落上的 %d 条：" % len(missing))
        for x in missing:
            print("   ", x)
    if not WRITE:
        print("（干跑，没写文件）")
        return 0

    raw = io.open(FIXES, "rb").read()
    crlf = raw.count(b"\r\n") > 0
    doc = json.loads(raw.decode("utf-8"))
    man = doc.setdefault("_manual_keywords", {})
    for k, v in touched.items():
        man[k] = v
    man[NOTE_KEY] = NOTE
    txt = json.dumps(doc, ensure_ascii=False, indent=1)
    if crlf:
        txt = txt.replace("\n", "\r\n")
    io.open(FIXES, "wb").write(txt.encode("utf-8"))
    print("落盘 %s（%d 张 + 1 条说明；行尾 %s）" % (FIXES, len(touched), "CRLF" if crlf else "LF"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
