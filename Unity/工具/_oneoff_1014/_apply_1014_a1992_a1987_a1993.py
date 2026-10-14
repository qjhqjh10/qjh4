# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · `A1992` 前置 + `A1987` + `A1993` 三笔一起落盘。

三件事（顺序不能反，见 `资料/普查产出_第三十会话/D2_造卡族判定.md` 一·A）：
  ① `A1992`（前置）：`工具/gen_cardface_fixes.py` 加 `_manual_desc` / `_manual_descZh`
     「手工覆盖列优先」；**并**把 `资料/卡牌数据表/卡牌完整信息库_0824.md` 自己漏掉的那一行补对。
  ② `A1987`：5 张卡的 `desc` 补回 `Talent: X`（其中 `GOF18`/`BL34` 连 `descZh` 也缺）。
  ③ `A1993`：`SOR29 Ephrael Stern` 的 `desc` 数字 `3` → `2-3`。

判据 = **PnP 成品卡图逐张亲读**（铁律 7）＋ `Unity/资料/卡表核对_卡图提取/_合并总表.md`（另一路
逐张转录，两处同值）。本脚本只写 `cardface_fixes.json`；`.py` 与 `.md` 那两处由主对话直接改。

用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_a1992_a1987_a1993.py
"""
import io
import json
import os
import sys

try:
    sys.stdout.reconfigure(encoding='utf-8')
except Exception:
    pass

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
FIXES = os.path.join(ROOT, '数据', '游戏数据', 'cardface_fixes.json')

# ---- `desc`：6 条（`A1987` 五条 + `A1993` 一条）----------------------------------
# 逐条判据（卡图路径都在 `d:/2/Warpforge部队卡片/` 下，2026-10-14 本笔逐张亲读）：
DESC = {
    # `SOR9` 卡面（`Sorotitas/3部队/Warpforge_12_Preacher.png`）：
    #   〔书〕Talent: Hymn of Battle ／ 5 ☀ : Gain 〔眼〕Shield
    # 旧值只剩第二行（前缀被 OCR 吃掉）。分隔符照池内同族写法（`AM2 Ursula Creed` 那种 `. `）。
    "Preacher": "Talent: Hymn of Battle. 5 ☀: Gain Shield",
    # `TAU2` 卡面（`Tau/1督军/Warpforge_1_AunVa.png`）：〔书〕Talent: Ethereal Supreme
    # 旧值 = `Ethereal Supreme`（**前缀被吃掉**，连 `Talent:` 都没有）。
    "Aun'Va": "Talent: Ethereal Supreme",
    # `TL_Psychophage` 卡面（`Tyranid/3部队/Warpforge_7_Psychophage.png`）：
    #   〔书〕Talent: Feeding Frenzy. ／ 〔闪电〕Rally: Gain +2〔拳〕, +2〔枪〕 and +1 Health …
    # **天赋行印在 Rally 之上** ⇒ 补在最前。
    "Psychophage": "Talent: Feeding Frenzy. Rally: Gain +2 [Attack], +2 [Ranged] "
                   "and +1 Health for each damaged enemy",
    # `GOF18` 卡面（`Orks/3部队/Warpforge_26_Painboy-Sniklaw.png`）：
    #   〔闪电〕Rally: Heal 3 to a friendly unit. ／ 〔书〕Talent: Sawbonez
    # 🔴 **次序 = Rally 在前、Talent 在后**（卡图亲读 + `_合并总表.md:654` 两处同值）——
    #    `资料/普查产出_第三十会话/D2_造卡族判定.md` 把它写成了 Talent 在前，**那是错的**（见报告）。
    # ⚠️ 键必须是**卡池里的名字** `PainBoy Sniklaw`（大写 B），⛔ 不是 PnP 上的 `Painboy`。
    "PainBoy Sniklaw": "Rally: Heal 3 to a friendly unit. Talent: Sawbonez",
    # `BL34` 卡面（`Chaos/1督军/Warpforge_1_Abaddon-the-Despoiler.png`）：〔书〕Talent: Chosen of the Four
    # 旧值 = `Chosen of the Four`（前缀被吃掉，连 `Talent` 这个词都没有）。
    "Abaddon the Despoiler": "Talent: Chosen of the Four",
    # `SOR29`（`A1993`）卡面（`Sorotitas/3部队/Warpforge_33_Ephrael-Stern.png`）：
    #   〔尖盾〕Camouflage. ／ 〔人形〕Pray: Deal 2-3 damage. If target dies, gain 1〔太阳〕
    # 旧值 = `Deal 3 damage` ⇒ **数字错**，`2-3` 才对（与快照目标块 `enemyUnits=1` 一致，`to an enemy troop` 那半句不动）。
    "Ephrael Stern": "Pray: Deal 2-3 damage to an enemy troop. If target dies, gain 1 ☀",
}

# ---- `descZh`：2 条（`A1987` 里连中文也缺的那两张）--------------------------------
# 名字取**天赋卡自己的中文名**（判据 = `数据/本地化/i18n/zh_CN.csv` 的 `Talent: <名>` 那一行，
# 与 `数据/卡牌翻译/zh_cards.json` 的 `n` 同值）：
#   · `Talent: Sawbonez,~,天赋：锯骨大夫`（`zh_CN.csv:3436`；`:2209` 同名键也是「锯骨大夫」）
#     ⚠️ `D2_造卡族判定.md` 写的「锯骨」**不是**这个名字 —— 两处表都写的是「锯骨大夫」。
#   · `Chosen of the Four,~,四神宠儿`（`zh_CN.csv:830`；`zh_cards.json` 里这条**只有 desc 没有 name**
#     ⇒ `gen_cards_engine.py` 的 `ZH_NAME_OVERRIDE` 就是拿这一行兜的）
DESC_ZH = {
    # 次序与英文 `desc`、与卡面一致（Rally 在前）—— 池内 91 条 `天赋：` 条目全部与英文同序
    # （例 `GSC21 Clamavus` = `Rally: … Talent: …` / `集结：… 天赋：…。`）。
    "PainBoy Sniklaw": "集结：治疗一个友方单位 3 点生命值。天赋：锯骨大夫。",
    "Abaddon the Despoiler": "天赋：四神宠儿",
}

# ---- `_manual_keywords`：3 条（**配套检查的产物**，见报告）-------------------------
# 🔴 **为什么必须动 `keywords`**（⛔ 不是可选项）：`CardDef` 构造器对 `keywords` 每一项调
#    `KeywordTable.Normalize`，**认不出的会 `NoteDropped` 留痕**（`CardDef.cs:254-256`），
#    而撤下这条留痕的**唯一**地方是 `CollectTalent` 的「裸名写法」那一支
#    （`CardDef.cs:1648` 的 `ClearDropped`）。`desc` 一旦有了 `Talent:` 头，
#    `CollectTalent` 在 `:1607-1608` 就**提前 return** ⇒ 那一支**再也不执行**
#    ⇒ 旧的关键词项会**永远留在 `Dropped` 里**，而 `RuleEngineTest.cs:2034` 断的正是
#    `KeywordTable.Dropped.Count == 0` ⇒ **自检当场变红**（本笔不能吃这个回归）。
# ⇒ 把裸名改写成**能归一化**的 `Talent: <名>` 形（`Normalize` 切冒号取头 = `talent`），
#    池内先例：`ASH19 Witchblade Warlock`（`["Waystone","Talent: Witchfire"]`）·
#    `DA31 Ezekiel`（`[...,"Talent: Master Interromancer"]`）· `BL35 Haarken`（`["Talent: Helspear Assault"]`）。
# ⚠️ 这一形对 `_keywords` 的**值没有影响**：`Normalize` 只取冒号前那半段 ⇒ 得到的还是 `{"talent": 1}`，
#    与改前的 `["Talent"]` **逐字同值**；而 `KeywordSegment` 那边 `talent` 在正文里已声明 ⇒ 跳过
#    （`CardText.cs:248` 的闸）⇒ **卡面不会多印一行**。
MANUAL_KEYWORDS = {
    "Preacher": ["Talent: Hymn of Battle"],       # 旧 = ["Talent", "Hymn of Battle"]（裸名，见上）
    "Aun'Va": ["Talent: Ethereal Supreme"],       # 旧 = ["Talent"]（**名字整个不在任何一处**，见报告）
    # 旧 = ["Chosen of the Four"]（裸名；它的 `ClearDropped` 靠「与整条 desc 一字不差」那道闸，
    #   而 `desc` 一加 `Talent:` 头，那道闸也不成立了 ⇒ 同 `Preacher`）
    "Abaddon the Despoiler": ["Talent: Chosen of the Four"],
}

# ---- 留痕（铁律 5）----------------------------------------------------------------
NOTE_DESC_KEY = "_2026-10-14_A1992_A1987_段首天赋行补回"
NOTE_DESC = (
    "2026-10-14（第三十会话 · `A1987` ＋ 前置 `A1992`）。**卡面段首那一行 `Talent: <天赋名>` 被 "
    "OCR/数据管线从 `desc` 里吃掉了**，逐张开 PnP 成品卡图亲读（铁律 7），五张："
    "① `SOR9 Preacher` —— 卡面 `〔书〕Talent: Hymn of Battle` ＋ `5 ☀ : Gain 〔眼〕Shield`"
    "（`d:/2/Warpforge部队卡片/Sorotitas/3部队/Warpforge_12_Preacher.png`），旧值只剩第二行"
    "`5 ☀: Gain Shield` ⇒ 天赋行整行没有；"
    "② `TAU2 Aun'Va` —— 卡面 `〔书〕Talent: Ethereal Supreme`"
    "（`Tau/1督军/Warpforge_1_AunVa.png`），旧值 = `Ethereal Supreme`（**连 `Talent:` 前缀都没有**）；"
    "③ `TL_Psychophage` —— 卡面 `〔书〕Talent: Feeding Frenzy.` ＋ `〔闪电〕Rally: Gain +2〔拳〕, +2〔枪〕 …`"
    "（`Tyranid/3部队/Warpforge_7_Psychophage.png`），旧值从 `Rally:` 起 ⇒ 天赋行整行没有；"
    "④ `GOF18 PainBoy Sniklaw` —— 卡面 `〔闪电〕Rally: Heal 3 to a friendly unit.` ＋ `〔书〕Talent: Sawbonez`"
    "（`Orks/3部队/Warpforge_26_Painboy-Sniklaw.png`）—— 🔴 **`desc` 与 `descZh` 都缺**，卡面少印一整行；"
    "⑤ `BL34 Abaddon the Despoiler` —— 卡面只有一行 `〔书〕Talent: Chosen of the Four`"
    "（`Chaos/1督军/Warpforge_1_Abaddon-the-Despoiler.png`），旧值 = `Chosen of the Four`（前缀被吃掉）"
    "—— 同样 `desc` 与 `descZh` 都缺。"
    "旁证（第二份、非独立，但**同值**）：`资料/卡表核对_卡图提取/_合并总表.md:716`（Preacher）"
    "`:860`（Aun'Va）`:995`（Psychophage）`:654`（Painboy Sniklaw）`:187`（Abaddon）"
    "—— 那是另一路逐张看卡图抄的，五张逐字对上（含**段次序**）。"
    "🔴 **为什么必须补 `desc`、不能只补表现层**：`Talent: <名>` 现在是**解析层的输入** —— "
    "`EffectText.TryTalent`（`EffectText.cs:7321`）认头后发一条标记 op，机制侧 `CardDef.TalentName` "
    "（`CardDef.cs:1605` 的 `CollectTalent`）读它、`RuleCore.SpawnTalents` 每回合按它造那张天赋卡"
    "到手上（`A1873` 查实的语义）。旧数据里五张卡的 `TalentName` 只能靠**兜底启发式**认："
    "`SOR9` / `BL34` 走「名字只在 `keywords` 数组里」，`TAU2` 走「正文里只有名字」，"
    "`TL_Psychophage` / `GOF18` **两条路都认不出 ⇒ `TalentName` 是 null、天赋整条不生成**"
    "（`CollectTalent` 那段注释里点名的例子正是 `Psychophage` 的 `[\"Rally\",\"Talent\"]`）。"
    "⚠️ **`GOF18` 的段次序照卡面 = Rally 在前、Talent 在后**（⛔ 不是 Talent 在前）——"
    "`资料/普查产出_第三十会话/D2_造卡族判定.md` §三·A 把它写反了，本笔以卡图为准（铁律 7），"
    "同批 `descZh` 也跟着按同一序写。"
    "⚠️ **配套（下游）**：这次 `desc` / `descZh` 新出现 `Talent:` / `天赋：` 两种写法 ⇒ 卡面那枚天赋徽记"
    "改由按资源词的那一支画（`CardIcons.Rewrite` + `CardPresentation/Resources/card_icon_plan.json`）"
    "⇒ **要重跑 `工具/gen_icon_plan.py`**（本笔的白名单里没有它，见本表 `_note` 的口径）。"
    "🔴 **同时作废 `_2026-09-15_A7天赋名`（判据 = 铁律 11，2026-09-29）**：那条明写这五张的英文 `desc` "
    "「**同样缺 `Talent: X`** …… **属玩法改动，另记在文档里**」⇒ **挂着不做**。"
    "按铁律 11「与原版不符 / 复刻有缺漏 —— 先记录，之后【完全复刻】；不因困难或复杂而回避」，"
    "「属玩法改动」**不再是**可以挂起的理由 ⇒ **原 `A7` 那个决定作废**（它当时只补了 `descZh`、把 `desc` 留着）。"
    "⚠️ 另一条配套事实：`desc` 一有 `Talent:` 头，`CollectTalent` 的「裸名写法」就提前 return ⇒ "
    "`keywords` 里那两个**裸**天赋名（`SOR9` 的 `Hymn of Battle` · `BL34` 的 `Chosen of the Four`）"
    "再也等不到 `ClearDropped`、会留在 `KeywordTable.Dropped` 里 ⇒ 已同批改写成可归一化的 "
    "`Talent: <名>` 形（走 `_manual_keywords`，见那一列）。"
)

NOTE_DESCZH_KEY = "_2026-10-14_A1992_A1987_中文天赋行补回"
NOTE_DESCZH = (
    "2026-10-14（第三十会话 · `A1987` 的中文那半边）。**只有 `GOF18` / `BL34` 两张的 `descZh` 也缺**"
    "（另三张的中文早就有天赋那半句，所以只补英文）。名字取**天赋卡自己的中文名**，判据是 "
    "`数据/本地化/i18n/zh_CN.csv` 里 `Talent: <名>` 那一行（另与 `数据/卡牌翻译/zh_cards.json` 的 `n` 同值）："
    "`Talent: Sawbonez,~,天赋：锯骨大夫`（`zh_CN.csv:3436`）· `Chosen of the Four,~,四神宠儿`"
    "（`zh_CN.csv:830`）。逐条：`PainBoy Sniklaw` `集结：治疗一个友方单位 3 点生命值。` → "
    "`集结：治疗一个友方单位 3 点生命值。天赋：锯骨大夫。`（**Talent 挂在 Rally 之后** —— 与卡面、"
    "与英文 `desc` 同序，池内 91 条 `天赋：` 条目全部与英文同序，例 `GSC21 Clamavus`）；"
    "`Abaddon the Despoiler` `四神宠儿` → `天赋：四神宠儿`。"
    "⚠️ **`D2_造卡族判定.md` §三·A 那两句与判据不符，本笔没照它写**：① 名字写成「锯骨」——"
    "两处表（`zh_CN.csv:2209` / `:3436` 与 `zh_cards.json`）写的都是「**锯骨大夫**」；"
    "② 它把 `GOF18` 的序写成「天赋」在前 —— 卡面与 `_合并总表.md:654` 都是 **Rally 在前**。"
    "🔴 **为什么必须跟英文一起改**：`Editor/IconSetup.cs` 的 ④a 判据是「`desc` 的 sprite 集合 ⊆ "
    "`descZh` 的」—— 只补英文会让它把这 2 条当成**新缺口**（`A1444` / `A1403` 那两批实测过同一件事）。"
    "⚠️ 中文那半**不是原版判据**（原版官方中文表是另一份，见 `gen_cards_engine.py` 的 `OFFICIAL_LOC_SRC`）——"
    "这里推的是「英文卡面那个位置有什么记号、中文就在同一位置写那个词的规范中文名」。"
)


def main():
    b = io.open(FIXES, 'rb').read()
    crlf = b.count(b'\r\n')
    assert crlf == b.count(b'\n'), '行尾不是纯 CRLF —— 停下来看（本仓翻过好几次）'
    d = json.loads(b.decode('utf-8'))

    # ---- 改前先断言「盘上确实是我以为的样子」（锚点错 = 静默改错）----
    # `desc` 列现读只有 `Preacher` / `Psychophage` / `Ephrael Stern` 三条（其余三条是**新加**的）。
    OLD_DESC = {
        "Preacher": "5 ☀: Gain Shield",
        "Psychophage": "Rally: Gain +2 [Attack], +2 [Ranged] and +1 Health for each damaged enemy",
        "Ephrael Stern": "Pray: Deal 3 damage to an enemy troop. If target dies, gain 1 ☀",
        "Aun'Va": None, "PainBoy Sniklaw": None, "Abaddon the Despoiler": None,
    }
    for k, ov in OLD_DESC.items():
        got = d['desc'].get(k)
        assert got == ov, 'desc 的旧值跟我以为的不一样：%r 盘上=%r 我以为=%r' % (k, got, ov)
    # `descZh` 列现读**两条都没有**（`GOF18` / `BL34` 是**新加**的，它们的旧值来自 `zh_cards.json`）。
    assert d['descZh'].get('PainBoy Sniklaw') is None, 'GOF18 本来就有 descZh 条目？先看清再改'
    assert d['descZh'].get('Abaddon the Despoiler') is None, 'BL34 本来就有 descZh 条目？先看清再改'
    assert d['_manual_keywords'].get('Preacher') == ['Talent', 'Hymn of Battle'], 'SOR9 keywords 旧值变了'
    assert d['_manual_keywords'].get('Abaddon the Despoiler') == ['Chosen of the Four'], 'BL34 keywords 旧值变了'
    assert 'Aun\'Va' not in d['_manual_keywords'], 'TAU2 本来没有 _manual_keywords 条目'

    # ---- 写（新键一律追加在末尾，把 diff 压到最小）----
    n = 0
    for k, v in DESC.items():
        if d['desc'].get(k) != v:
            n += 1
        d['desc'][k] = v
    print('desc      改 %d 条' % n)
    n = 0
    for k, v in DESC_ZH.items():
        if d['descZh'].get(k) != v:
            n += 1
        d['descZh'][k] = v
    print('descZh    改 %d 条' % n)
    n = 0
    for k, v in MANUAL_KEYWORDS.items():
        if d['_manual_keywords'].get(k) != v:
            n += 1
        d['_manual_keywords'][k] = v
    print('_manual_keywords 改 %d 条' % n)
    assert NOTE_DESC_KEY not in d['_manual_desc_note'], '这条留痕已经在了'
    assert NOTE_DESCZH_KEY not in d['_manual_descZh_note'], '这条留痕已经在了'
    d['_manual_desc_note'][NOTE_DESC_KEY] = NOTE_DESC
    d['_manual_descZh_note'][NOTE_DESCZH_KEY] = NOTE_DESCZH
    print('留痕      _manual_desc_note / _manual_descZh_note 各 +1 条')

    out = json.dumps(d, ensure_ascii=False, indent=1, sort_keys=False)
    io.open(FIXES, 'wb').write(out.replace('\n', '\r\n').encode('utf-8'))
    b2 = io.open(FIXES, 'rb').read()
    assert b2.count(b'\r\n') == b2.count(b'\n'), '写出去之后行尾坏了'
    print('写出 %s（%d 字节 · CRLF）' % (FIXES, len(b2)))
    return 0


if __name__ == '__main__':
    sys.exit(main())
