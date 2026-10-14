# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · D2 判定落账：新账 A1986（仪器缺桥）· A1987（5 张天赋行丢失）· A1988（talent_cards 指向旧名）· A1989（解析层顺序静默吞句）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_d2.py
"""
import io

MAIN = r'D:\4\项目任务.md'

A1986 = (
    "| 🆕 **`A1986`** | 🔴 **【仪器缺陷】对账脚本的家族表缺一座桥：`choosecard` ↔ `create`/`recall`/… ⇒ 那一族【结构性恒报「我们缺」】**"
    "（2026-10-14 `D2` 判定现核 + 主对话现读）：原版那 27 条的 `chosenAbility` 虽叫 `createCardInHand(100)` / `recallToDeck(300)` / "
    "`setCardDeckToTop(420)` / `createCardInDeck(270)` / `summonUnit(70)`，但它们的 `targetCriteria` **一律带 `selectFromChooseResolution=1`** "
    "⇒ 原版语义 = 「**从候选里挑一张**，再把它造 / 召回 / 置顶」= **我们的 `choosecard` op**"
    "（`RuleEngine/Core/EffectResolver.cs:879 → DoChooseCard :3099`；10 种动作 `hand/draw/deploy/decktop/todeck/return/shuffle/enemyhand/copies/inccost` **全都实现了**，"
    "面板口 `ctx.ChooseCardIds` + `TakePickCard :2923` 已接）。"
    "而仪器侧 `工具/_a1759_reconcile.py:138` = `'create': {'create'}`、`:174` = `'choosecard': 'choose'` ⇒ **两条路之间没建桥** "
    "⇒ D2 那 33 条里 **27 条是假红**（⚠️ 现读行号 = **`:138`/`:174`**，⛔ 不是转述里的 `87/112`）。"
    "🔴 **旁证 = 作者知道这个形状**：同文件 `:145` 的 `'autoplay'` 家族**就写着** `{'summon','create','choose','recall'}` ⇒ **只是 `create`/`recall` 这几族漏了**。"
    "⇒ **要做**：给仪器补桥，**并跨全表复核还有哪些族同样中招**。判据 = `资料/普查产出_第三十会话/` 的 D2 判定（本会话产）。 |"
)

A1987 = (
    "| 🆕 **`A1987`** | 🔴 **5 张卡的 `desc` 丢了 `Talent: X` 那一行 —— 其中 2 张连 `descZh` 也缺 ⇒ 卡面【少印一整行】（玩家看不见）**"
    "（2026-10-14 `D2` 判定 + **亲读 PnP 成品卡图** · 铁律 7）：\n"
    "· `SOR9 Preacher` —— 卡面（`Sorotitas/3部队/Warpforge_12_Preacher.png`）印 `〔天赋徽〕Talent: Hymn of Battle` + `5 ☀: Gain Shield`；我们 `desc` 只有后一句（`descZh` 有）。\n"
    "· `TAU2 Aun'Va` —— 卡面（`Tau/1督军/Warpforge_1_AunVa.png`）印 `Talent: Ethereal Supreme`；我们 `desc` = `Ethereal Supreme`（**前缀被吃掉**）、`keywords=['Talent']`（**名字丢了**）。\n"
    "· `TL_Psychophage`（真值 uid `TL68`）—— 卡面（`Tyranid/3部队/Warpforge_7_Psychophage.png`）印 `Talent: Feeding Frenzy.` + `Rally: …`；我们 `desc` 只有 Rally 那句。\n"
    "· `GOF18 PainBoy Sniklaw` —— 卡面（`Orks/3部队/Warpforge_26_Painboy-Sniklaw.png`）印 `Rally: Heal 3 to a friendly unit.` **+ `Talent: Sawbonez`**；我们 **`desc` 与 `descZh` 都缺**、`keywords` 连 `Talent` 都没有。\n"
    "· `BL34 Abaddon the Despoiler` —— 卡面（`Chaos/1督军/Warpforge_1_Abaddon-the-Despoiler.png`）印 `Talent: Chosen of the Four`；我们 **两处都缺**。\n"
    "⇒ **要做**（**数据侧** → `数据/游戏数据/cardface_fixes.json` 的 `desc` / `descZh` 两列 + `_manual_*_note` 留痕；键用**池内唯一名**）。"
    "⚠️ **必须补 `desc` 而不是只补表现层** —— `desc` 是**解析层的输入**（`Talent:` 一族现在会解析）。判据 = 上列**亲读卡图路径**。 |"
)

A1988 = (
    "| 🆕 **`A1988`** | ⚠️ **`talent_cards.json` 里 Abaddon 那一项指向 2022 旧名 `Warmaster`，池里根本没有这张卡**"
    "（2026-10-14 `D2` 判定顺手查出，与 `A1987` 那张卡同族）：⇒ `Core/TalentCardData.Resolve` **返回空、且每回合打一条 `Debug.LogWarning`**；"
    "现在**只靠 `CardDef.CollectTalent` 的「keywords 项 == 整条 `desc`」兜底**才造得出 `BL_Chosen_of_the_Four`（池里**有**这张，现行 PnP 也是这个名字）。"
    "⇒ **要做**：修 `数据/游戏数据/talent_cards.json` 那一项，并**顺手把那条 warning 的措辞改准**。"
    "判据 = `talent_cards.json` 的 `why` 原文「真值里「Warmaster」落不进我们卡池」+ `Core/TalentCardData.Resolve`。 |"
)

A1989 = (
    "| 🆕 **`A1989`** | 🔴 **【解析层顺序缺陷】`TryChooseCard` 排在 `lowercost` 之前 ⇒ 后一句被【整句吞掉】、而且【静默】**"
    "（2026-10-14 `D2` 判定 + 亲读卡面）：`GSC29 Metamorph Iconbearer`（L1 行）现行卡面"
    "（`Genestealer Cult/3部队/Warpforge_31_Metamorph-Iconbearer.png`，**卡面本身就没有句号**）是 "
    "`Rally: Choose a 2-cost Genestealer Cults troop and create 2 copies in your hand Lower their cost by 1` —— "
    "我们只产出 1 条 `choosecard/…/copies`，**`Lower their cost by 1` 既不是 op、也不进 `unparsed`**"
    "（`_tmp_view/w15_scan.txt` 里 `GSC29` 那行第 3/4 字段皆空）⇒ **静默丢失**。"
    "**根因** = `RuleEngine/Core/EffectText.cs:2617` 那一处：`TryChooseCard` 在 `ParseSegment` 里**排在 `lowercost` handler 之前**，"
    "而该句**没有 ` and ` 可切** ⇒ **整句被 `choosecard` 领走**。⇒ **要做**（铁律 11，⛔ **不许静默丢**）；"
    "⚠️ **动手前先扫全池**：同类「`lower cost by N` 挂在别的动词后面、且不缺 ` and `」的句子**还有几张**（**可能不止 `GSC29` 一张**）。"
    "判据 = 卡面亲读 + `_tmp_view/w15_scan.txt` 现读。 |"
)


def main():
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1985`**')]
    assert len(hits) == 1, hits
    lines[hits[0] + 1:hits[0] + 1] = ['\n'.join([A1986, A1987, A1988, A1989])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('OK bytes %d -> %d' % (len(b), len(out.encode('utf-8'))))


if __name__ == '__main__':
    main()
