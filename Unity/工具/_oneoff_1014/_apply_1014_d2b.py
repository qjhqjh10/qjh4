# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · D2 补发的落账：
 5 行追加（A1975/A1986/A1987/A1988/A1989）+ 新账 A1990/A1991/A1992。判据原文 → 资料/普查产出_第三十会话/D2_造卡族判定.md
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_d2b.py
"""
import io

TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
MAIN = r'D:\4\项目任务.md'
DOC = '`资料/普查产出_第三十会话/D2_造卡族判定.md`'

SEG = {}

SEG['A1986'] = (
    " 🔴 **2026-10-14 影响面现读（跨全表）**：全表 `我们缺` = **152 条**，其中**原版该步 `targetCriteria.selectFromChooseResolution == 1` 的 = 32 条** "
    "⇒ **桥缺口 ≈ 32/152（≈21%）** —— **不是 `D2` 独有、但也没蔓延到全表**（`D2` 占 27，**`D2` 之外还有 5 条**：`drawCard` 3 + `buffOthers` 2；上界口径 = **35**）。"
    "分布：`createCardInHand` **16/22** · `recallToDeck`/`setCardDeckToTop`/`summonUnit`/`drawCard` 各 3/3 · `createCardInDeck`/`summonBuffedUnit` 各 1/1 · `buffOthers` 2/58。"
    "⚠️ **剩下的 ~117 条不是这个桥**（`buffOthers 56` · `doDamage 16` · `buffSelf 13` · `triggerEffectInCard 8` …）⇒ 要么是真缺口、要么是**另一类承载错位**"
    "（工具里已有 `承载不同`/`CARRY_VERB` 那一支）⇒ **要逐族单独诊断**。⚠️ **判数要现读**：`triggerPray 我们缺` 已从 26 掉到 2（`Talent:` 那一笔）"
    "—— 这类「结构性恒报」会**随解析层补齐自己塌掉**。判据原文 → " + DOC + " §一。"
)

SEG['A1987'] = (
    " 🔴 **2026-10-14 本笔要覆盖一条旧决定（铁律 11）**：`cardface_fixes.json` 的 `_manual_descZh_note` 里那条 **`_2026-09-15_A7天赋名`** 记录**明写**"
    "「这 5 张的英文 `desc` 同样缺 `Talent: X` …… **没动** …… **属玩法改动，另记在文档里**」—— 而按 🔴 **铁律 11（2026-09-29）**，"
    "「**属玩法改动 / 另记在文档里**」**不再是可以挂起的理由** ⇒ **本笔就是把它做掉**，并在记录里写「**原 `A7` 决定作废，理由 = 铁律 11**」。"
    "⚠️ 本笔覆盖 `A7` 那 5 张里的 **3 张**（`SOR9`/`TAU2`/`TL_Psychophage`）；另 **2 张（`GOF18`/`BL34`）是 `A7` 没发现的** —— 它们连 `descZh` 都缺（**卡面少一整行**），更要紧。"
    "🔴 **配套检查（非可选）**：补完要**现读这 5 张的 `keywords`**（现读值 = `SOR9=['Talent','Hymn of Battle']` · `TAU2=['Talent']` · "
    "`TL_Psychophage=['Rally','Talent']` · `GOF18=['Rally']` · `BL34=['Chosen of the Four']`）—— 天赋名**同时**出现在 `desc` 与 `keywords` 时，"
    "卡面可能**把同一行画两遍**（判据 = `CardText.KeywordSegment` + `KeywordTable.Normalize`）⇒ 变多了就按 `_manual_keywords` 那一列收。"
    "⚠️ **键用裸卡名**（这 5 张在池内**都唯一**）。**改法全文 → " + DOC + " §三·A**。"
)

SEG['A1989'] = (
    " 🔴 **2026-10-14 改法已给 —— 【两处必须一起改，缺一处就是静默错】**（全文 → " + DOC + " §三·B）："
    "① `RuleEngine/Core/EffectText.cs` 的 `TryChooseCard`（`:5099` 那个 `else` 分支里 `ReChooseCopies` 命中处）：判出 `act=\"copies\"` 之后"
    "**再看一眼 `low` 尾部**有没有 `lower their cost by (\\d+)` / `lower its cost by (\\d+)`，命中就写进 `EffectOp`"
    "（**复用 `ChooseCostDelta`、取负值**表降价 —— ⛔ **别新开字段名造两套语义**）；"
    "② `RuleEngine/Core/EffectResolver.cs` 的 `DoChooseCard` 的 `case \"copies\"`（`:3317` 起）：造完每张 `copy` 后照 `case \"inccost\"`（`:3182`）**同款配方**挂 "
    "`ctx.CostMods.Add(new CostMod { Player=owner, HandOf=owner, Key=\"*\", HandInstanceId=copy.Id, Delta=-N, ExpireTurn=-1 })`"
    "（`HandInstanceId` 配方注释 @`BattleContext.cs:246`；**`Delta` 负数 = 降价** @`:178`）。"
    "**怎么验**：① 断言 `EffectText.ParseSegment(那一整句)` 的 op **带负 `ChooseCostDelta`**；② 结算后两张 copy **各有一条** `CostMod{HandInstanceId==copy.Id, Delta==-1}`；"
    "③ 补一条**灭自证**断：**盘上那张原卡的 `CostOf` 不变**（结构上不可能与 ② 同时满足）。"
    "⛔ **备选（把 `TryChooseCard` 挪到 `SplitAndTail` 之后）不建议** —— `GSC29` 那句**卡面本来就没有 `and`/句号**，切开它等于**替原版断句**、会连带动到别的 `choose` 句。"
)

SEG['A1988'] = (
    " 🔴 **2026-10-14 改法已给**（全文 → " + DOC + " §三·C）：改 `工具/gen_talent_cards.py`，加一张 **2022 → 现行印刷名**改名表"
    "（`Warmaster → Chosen of the Four`，池内 id `BL_Chosen_of_the_Four`）后重跑 ⇒ `Resources/talent_cards.json` 的 `BL34` 项从 `[\"Warmaster\"]` "
    "变成 `[\"BL_Chosen_of_the_Four\"]`，`数据/游戏数据/talent_cards.json` 里那行 `why` 同时消失。"
    "**判据**：现行 PnP 整套**没有**叫 `Warmaster` 的卡（`find . -iname \"*armaster*\"` = **0 命中**），而有 `Chaos/2天赋/Warpforge_2_Chosen-of-the-Four.png`。"
    "**怎么验**：`TalentCardData.Resolve(pool,\"BL34\").Count == 1` 且名字 = `Chosen of the Four`；并断言 `Core/TalentCardData.cs:90` 那条 `LogWarning` **不再出现**。"
)

SEG['A1975'] = (
    " 🔴 **2026-10-14 判据更完整 —— 差集【不止一项】，另有两个新事实**（判据原文 → " + DOC + " §四）："
    "**原版 `CreateStartingTroop.c`（104 行）调用序** = `CreateCard` → `SetAsUnitInPlay(1)` → `SetUnitInPlayLayer` → `CardSetup` → "
    "**`:47 ActivateTraitsOnSummonOrEnchantment`** → `SetUnitOrderInPlay` → `GetUnitSizeInPlay`/`set_DefaultScale` → **`MoveMinionIntoSlot`（真正落位）** → 列表 `Add`。⇒ "
    "① **trait 激活排在【落位之前】**（第 5 步 vs 第 8 步，且 `SetAsUnitInPlay` 已先把它标成在场）；"
    "② **这三个起始单位函数里 `ActivateMinion` 一次都没调**（`grep -c` = 0/0/0）⇒ **原版起始单位很可能不吃召唤病**、`canAct` 不由 `ActivateMinion` 置位 "
    "⇒ **⛔ 别直接搬 `ApplyDeployTurnState`**（它 = `ActivateMinion` + trait 两半，会把 `summonSickness`/`Exhausted` 一起写上去 = **另一档行为**）。"
    "🔴 **我方差集（不止 trait 这一项）**：`TutorialScript.PlaceTroops`（`Core/TutorialScript.cs:1273-1300`）**没调** `Auras.Recompose(ctx)`（另 3 个入口都调）· "
    "**没调** `ResolveDeploy` · **没调** `ResolveFaithAbility` · **没调** `RecomputeCurrentAttackType`/`ForceAttackTypeOnDeploy` ⇒ **要和本笔一起收**（见新账 `A1990`）。"
    "⚠️ **未核**：教程起始单位里有没有带光环 / 信仰能力 / 部署触发的卡（⇒ 可能今天不可观测，但按「新增写入点必须补钩子」与「不许静默失败」仍要做）。"
)

A1990 = (
    "| 🆕 **`A1990`** | 🔴 **【口径本身错了 + 铁律命中】「新单位落地入口 = 三个」这句话是错的**（2026-10-14 `D2` 顺手查出）："
    "`RuleEngine/Core/RuleCore.cs:2077` 与 `Core/UnitState.cs:81` **都写着**「新单位落地入口 = `PlayCard` / `DeployFree` / `DoReanimate` **三个**，"
    "`F9` 已**全树 grep 确认没有第四个**」—— 而 **`TutorialScript.PlaceTroops`（`Core/TutorialScript.cs:1273-1300`）就是【第四个】**（同在 `Core/` 下）。\n"
    "🔴 **它正是本仓那条铁律的靶子**：「**棋盘是裸数组，光环靠手挂的钩子重算 —— 新增写入点必须补钩子**」"
    "（清单在 `Core/Aura.cs` 的 `Recompose` 注释里，现读 9 个写入点 + `BeginTurn`）。\n"
    "⇒ **要做**：① **重核那句 grep 的范围/判据**；② 把 `PlaceTroops` 的四处缺口（`Auras.Recompose` / `ResolveDeploy` / `ResolveFaithAbility` / "
    "`RecomputeCurrentAttackType`+`ForceAttackTypeOnDeploy`）补上；③ **给「入口清单」本身配一条断言**（⛔ 别再靠「我 grep 过」—— 这次就是 grep 漏了）。"
    "⚠️ 与 `A1975` 一起收。判据原文 → " + DOC + " §四。 |"
)

A1991 = (
    "| 🆕 **`A1991`** | ⚠️ **对账仪器的 `卡id` 列印的是【真值 `uniqueId`】、不是匹配到的我们的 id**（2026-10-14 `D2` 顺手查出）："
    "`工具/_a1759_reconcile.py:671` 写的是 `row = [cid, …]`，而 `cid` = 真值 `uniqueId`；**匹配结果在 `ocid`（`:650`）里、没进这一列** "
    "⇒ 那 33 条里 **11 条印的是 32 位 GUID**（`fd67b475…` = Brood Prophet 等）。**只影响人读对照、不影响判定**"
    "（`ocard = our.get(ocid)` 用的是 `ocid`，故这些行是「我们缺」而不是「卡不在池」—— 反证匹配成功）。"
    "⇒ **要做**（仪器出来的表要人能读）；⚠️ 与 `A1977`（`SCAN` 写死过期路径）同属「**仪器可信度**」那一族，**建议一并交给同一个写手**。"
    "判据原文 → " + DOC + " §五·1。 |"
)

A1992 = (
    "| 🆕 **`A1992`** | 🔴 **`A1987` 的修复有一条【会被静默抹掉】的通路 —— ⛔ 顺序反了就是白做**（2026-10-14 `D2` 顺手查出）："
    "`资料/卡牌数据表/卡牌完整信息库_0824.md:613` **自己就漏了 `Painboy Sniklaw` 的 Talent 行**"
    "（效果列 = `Rally: Heal 3 to a friendly unit.`，关键词列 = `Rally`）⇒ 若 `工具/gen_cardface_fixes.py` **从这份表再生成**，"
    "**会把补进 `cardface_fixes.json` 的 `desc` 静默盖回**。"
    "⇒ **要做**：**先**给 `gen_cardface_fixes.py` 加「`desc` / `_manual_*` 覆盖列优先」的口径（或把那份 md 一起补对），**再**做 `A1987`。"
    "判据原文 → " + DOC + " §五·2。 |"
)


def append_seg(path, head_prefix, seg):
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, path
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith(head_prefix)]
    assert len(hits) == 1, (head_prefix, len(hits))
    i = hits[0]
    assert lines[i].rstrip().endswith(' |'), lines[i][:70]
    lines[i] = lines[i].rstrip()[:-1].rstrip() + seg + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(path, 'wb').write(out.encode('utf-8'))
    print('  +%s' % head_prefix)


def main():
    # ⚠️ 这 5 条全在 MAIN（§29·b）里，⛔ 不在 TASKS
    for cid in ('A1975', 'A1986', 'A1987', 'A1988', 'A1989'):
        append_seg(MAIN, '| 🆕 **`%s`**' % cid, SEG[cid])
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1989`**')]
    assert len(hits) == 1
    lines[hits[0] + 1:hits[0] + 1] = ['\n'.join([A1990, A1991, A1992])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A1990/A1991/A1992')


if __name__ == '__main__':
    main()
