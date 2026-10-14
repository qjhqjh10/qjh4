# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 按铁律 5 订正三条账文 + 立一条新账（A1966）。
判据来源：两个只读判据代理 + 主对话现核（读数为 2026-10-14 现跑）。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_corrections.py
"""
import io, sys

P = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'

A1964 = (
    "| 🆕 **`A1964`** | ✅ **判据已查清（2026-10-14 只读代理 + 主对话现核）—— 可以放心加**："
    "`Editor/BattleScene.cs` 的「选牌面板」那一节加一条「`GSC65` 摆 4 张」（`driver.ChooseOptionCount == 4`）。"
    "🔴 **2026-10-14 更正（铁律 5）**：原文写「防御卡走**战前发放**那条路 ⇒ 拿『走面板打出去』去钉可能根本不触发面板」——"
    "**实测相反**：① **战前发放那条路默认根本不走**（`defensiveCardPanelEnabled` 出厂 `false`，`BattleDriver.cs:4685`；"
    "A1664：原版 `ShouldUseEnviromentalEffects()` 恒 false），且**它的候选域 = 本阵营 3 张防御卡、与 Sabotage 无关**（`:4772-4798`）；"
    "② **真正能钉「4 张 Sabotage」的是「把这张防御卡打出去」那条路**，逐跳（每跳现核）：`OnCardDeployed`(`:7639`) → `BeginPlay`(`:6026`) → "
    "`PlayAsks`(`:5912`) → `RuleCore.PlayerChooseOps`(`RuleEngine/Core/EffectResolver.cs:3451`) → `EffectText.TryChooseCard`(`EffectText.cs:5012`，"
    "要求 `low.StartsWith(\"choose \")`) → 产 `Verb=\"choosecard\"`(`:5109-5119`) → `ShowAsk()` 的 `if (op.Verb == \"choosecard\")`(`BattleDriver.cs:6122`) → "
    "`RuleCore.ChooseCardCandidates`(`:6125`) → `ChooseShowLimit`(`:6137`) → `_choosePanel.Open`(`:6236`)。"
    "**防御卡打得出去**：引擎明确放行 —— `CanPlayTactic` 那句「✅ 防御卡走同一条路」（`EffectResolver.cs:1373-1379`），"
    "`RuleCore.PlayCard:2333-2335` 对非单位卡一律分流 `PlayTactic` ⇒ 原担心的「因路径不对而红的假账」**不成立**。"
    "⚠️ **错因**：把「防御卡在卡组线里走战前发放」当成了「防御卡只能走战前发放」。**已核实**（现核）："
    "`GSC65 = Pilfered Supplies` · `type=defence` · **`subtype=\"Defence\"`（⛔ 不是 `Sabotage` —— 账文原来把它当 Sabotage 了）** · "
    "`cost=0` · `desc=\"Choose a Sabotage card and add it to your opponent's hand\"`（**以 `Choose ` 开头 ⇒ 有 ask 点**）· "
    "原版 `choicesDefinition.maxTargets = 4` · `filterSpecificCards` 恰好 4 条；候选域 = `subtype == \"Sabotage\"` 的卡，"
    "**我们池里正好 4 张**（`GSC_Cult_Propaganda` / `GSC_Improvised_Barricade` / `GSC_Jammed_Communications` / `GSC_Poisoned_Supplies`）✅。"
    "**加在哪**：`BattleScene.cs` 第 20 节 `{ }` 闭合（`:14652`）之后、`// ---- 21.`（`:14654`）之前，新起 `20-b` 节。"
    "🔴 **必须配一条【灭自证】配对**：`GSC50 Clandestine Operation`（`tactic`/`Spell`/费 2，同一句 select 子句、原版 `maxTargets = 3`；"
    "⚠️ 卡面**不是**一字不差 —— 它多一句 `Draw a card`）⇒ 断它只摆 **3** 张；两条一起红/绿才咬得住「上限是逐卡数据」。"
    "⚠️ **同族结构性差异（今天恰好等价，留着看）**：原版候选域 = `filterSpecificCards` 那 4 个固定 `m_PathID`，"
    "我们 = 按 `subtype` 全池匹配（`CreatePool.cs:281/1281`）⇒ 将来池子多一张 `Sabotage`，我们会变、原版不会。"
    "判据 → `资料/普查产出_第二十九会话/A1949_ChooseShowMax判据.md` + 2026-10-14 只读查证 |"
)

A1943 = (
    "| 🆕 **`A1943`** | 🔴 **`play check` 现在 rc=1 —— 但【不是「基线过期 ⇒ 重钉一下就好」】**（本会话主对话**现跑现核**；"
    "早上那条代理诊断**不完整，已就地订正**）：**哨兵 ②「基线文件的内容 vs 钉住常量」是 ✓**（现读 `d1fe0d96…` 与钉住一致，"
    "主对话另用独立脚本复算过）⇒ **红的不是「基线被改过」，是 ①-a / ①-b 的内容对拍**。"
    "🔴 **2026-10-14 更正（铁律 5）**：原文写「①-b 事件电池另有 **175 张**（154 + 15 + 6）」，"
    "**那 175 是 ①-c（净差 = 0 的候选）的数，不是 ①-b 的** —— 现读 **①-a = 773 变 / 增 193 / 消 193**（= 账 `A1758` 换 id 的必然结果）· "
    "**①-b = 345 变 / 增 179 / 消 179** · **①-c 分母 382 / 净差>0 = 207 / 净差=0 = 175（154 电池 + 15 灵魂石 + 6 信仰）**。"
    "**错因**：把两个不同含义的数顶替了。⚠️ **同一次更正（更要紧）**：「夹具起始资源 = 0」**只解释那 21 张（15 + 6），不是 175 张** —— "
    "另 **154 张的根因是「电池抬不到那个事件」**，那是 `A1965` 的**真实剩余工作面**，**改夹具治不了**。"
    "**夹具真身 = `工具/ruleprobe/PlayProbe.cs` 的 `BuildFixture`（`:232-304`）**：它**只设能量**（`SetEnergy :219-223` 只写 `Energy`/`MaxEnergy`），"
    "`Faith`/`SpiritStones`/`QuestPoints` **从头到尾没人写过** ⇒ 恒为 `int` 默认 **0**。"
    "**原版值（反编译方法体）**：`BattleManager__SetupInitialMana.c:101` → `PlayerManager__ResetMana` 只写 `+0x9c`（spiritStoneMana，值来自督军 `RawCardScript +0x128`）；"
    "`PlayerManager__.ctor.c` 十个 `ObscuredInt` 全 `op_Implicit(0)` ⇒ **原版开局 Faith = 0 · QuestPoints = 0**，"
    "**三格里只有灵魂石是真缺口**（另见新账 `A1966`）⇒ **「修夹具起始资源」本质是一次【量具干预】，⛔ 不是「补回原版值」** —— "
    "按铁律 3 必须在代码注释里标成「**这是我们挑的、不是原版的做法**」。"
    "**取值依据（全池实测）**：灵魂石**当代价**最大 N = **5**（`ASH53 Reclaim the Stars`）· 信仰**阈值**最大 N = **12**（`SOR50` 的 `12 ☀: Draw 3 cards`）· "
    "**任务点必须保持 0**（全池无一张卡把它当代价；引擎里它只被 `EffectResolver.cs:7269`「每满 3 点加 1 张隐秘」消费 ⇒ 给了会**凭空触发里程碑**）。"
    "🔴 **顺序已定**：**先修夹具起始资源 → 再整体重生成 `play` 基线（`play all pf <绝对路径>`）+ 重钉 `PinnedPlayMd5`**（`PlayProbe.cs:1775`）；"
    "⛔ **不许现在直接重钉** —— 那会把那 154 张**未确认的漂移一起盖成绿**，正是哨兵 ⑦ 要抓的事。"
    "**验收预期**：`play check` **rc=0**；**①-c 净差=0 由 175 降到 154**（21 张转成 `net>0`）；另两段与两条判据自检不得变。"
    "判据 = 2026-10-14 现跑 `bash d:/4/Unity/工具/ruleprobe.sh play check` 的 ①-a/①-b/①-c/② 四段输出。 |"
)

A1965 = (
    "| 🆕 **`A1965`** | 🔴 **全池结算对拍还差【一半覆盖面】——「档 B」那一族的事件电池那一轮**"
    "（从 ✅ 行 `A1431` 里拆出来的，**不是新发现**）：「**档 B**」= `Strike:` / `Slay:` / `Duty:` / `When …` / `Teleport:` / `Ambush:` / `Survivor:` … 那一族，"
    "**本夹具产生不了那个事件**（`other = 0` 是预期的）⇒ 那是**夹具盲区**，要**另起一轮**：在「打出一张卡」之后**再走一段事件电池**"
    "（让 A 攻击一发 / 让某张卡死一次 / 进下一回合 / 抽一张）。⚠️ **而那会改动全池所有读数** ⇒ 基线要重生成、候选要重新解读 ⇒ **得单独一轮**。"
    "🔴 **2026-10-14 更正（铁律 5）**：原文的 **「274 张」与「411 张候选」都已过期** —— 那是 2026-10-11 的旧数（判据 = `资料/普查产出_第十九会话/E8_ruleprobe逐卡真打.md` §2·2）；"
    "**现读**：`play all` 主档 md5 `48c4daf5…` · 电池 md5 `08a1d44b…` · 合起来 `888869e6…` · **候选 424 = A 42 · C 74 · B 308**"
    "（⚠️ 这个 424 是**改夹具之前**的读数，改完要重读）。"
    "🔴 **与 `A1943` 是同一块地、⛔ 但【工作面不是同一件事】**：`A1943` 的 ①-c 175 张里**只有 21 张（15 灵魂石 + 6 信仰）是夹具资源造成的**，"
    "**另 154 张「电池抬不到那个事件」正是本账要治的那一半**（原文把 154 也归成了「夹具起始资源 = 0」= **过度归因，已订正**）。"
    "⇒ **两笔合做的正确顺序 = 先修夹具起始资源（`A1943`）→ 再加电池步（本账）→ 最后统一重生成基线 + 重钉**。"
    "判据 → `资料/普查产出_第十九会话/E8_ruleprobe逐卡真打.md` + `资料/普查产出_第十四会话/逐卡效果检查_方法与边界.md` |"
)

A1966 = (
    "| 🆕 **`A1966`** | 🔴 **【新账 · 复刻缺口】我们引擎没实现「督军给开局灵魂石」**（2026-10-14 查 `A1943` 时顺手查出，铁律 14 插队立账）："
    "原版 `BattleManager__SetupInitialMana.c:101` → `PlayerManager__ResetMana(..., initialSpiritStone, ...)`，该值取**督军的** `RawCardScript +0x128`；"
    "字段桩在 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:83-87`（`initialHandModifier` / `initialManaModifier` / "
    "`initialPsychicModifier` / `initialPlasmaModifier` / `initialSpiritStoneModifier`）。"
    "**我们**：`RuleCore.NewBattle` 让 `SpiritStones` 恒从 0 起；全仓 `grep -rn \"initialSpiritStone\" MyGame/Assets` **只命中 `PlayerState.cs:86` 的一句注释**。"
    "⚠️ **同一次查证确认另两格【原版就是 0】**（`PlayerManager__.ctor.c` 十个 `ObscuredInt` 全 `op_Implicit(0)`；`ResetMana` 不写 `+0xb0` faith / `+0xc4` questPoints）"
    "⇒ **三格里只有灵魂石是缺口**，⛔ 别顺手去「补」信仰/任务点。"
    "**分两半**：① **「引擎实现该字段并接线」= 要做**，判据已齐（上面那条反编译方法体 + 桩字段名）；"
    "② **「逐督军的值」= 判据本地取不到 ⇒ 挂起** —— `d:/2` 全盘（含 `assets_full` 4.4 GB）二进制 grep `initialSpiritStoneModifier` **0 命中**、"
    "`d:/4` 数据表无此字段。🔴 ⛔ **但别把「字符串 grep 0 命中」当成「本地没有」**（本仓铁律：没有 type tree 的文件里字段名根本不出现）=⇒ "
    "**复核口 = 先按 bundle 名找出 `RawCardScript` 这个 MonoBehaviour 落在哪个包，再按 `peek_name()` + 原始字节手工解**；"
    "真要跑一次「按 `m_Script` pathID 全库扫」也便宜。⚠️ 这条**与 `A1943` 的夹具干预不是一回事**（那条是量具、这条是产品），⛔ 别合并。 |"
)

def main():
    b = io.open(P, 'rb').read()
    assert b.count(b'\r\n') == 0, 'file must stay LF'
    text = b.decode('utf-8')
    lines = text.split('\n')

    def idx_of(marker):
        hits = [i for i, l in enumerate(lines) if marker in l]
        assert len(hits) == 1, 'marker %r hits=%d' % (marker, len(hits))
        return hits[0]

    i1964 = idx_of('**`A1964`**')
    i1943 = idx_of('**`A1943`**')
    i1965 = idx_of('**`A1965`**')
    # 三条都必须还是【表格行】（以 | 开头、以 | 收尾），否则说明锚点找错了
    for i in (i1964, i1943, i1965):
        assert lines[i].startswith('| ') and lines[i].rstrip().endswith(' |'), (i, lines[i][:80])
    assert i1943 < i1964 < i1965

    lines[i1964] = A1964
    lines[i1943] = A1943
    lines[i1965] = A1965 + '\n' + A1966   # 新账紧随其后（紧挨 A1965，同族）

    out = '\n'.join(lines)
    assert out.count('\x00') == 0
    # 行尾不能翻：写回后仍应纯 LF、且行数 +1
    assert '\r\n' not in out
    io.open(P, 'wb').write(out.encode('utf-8'))
    print('OK  lines %d -> %d   bytes %d -> %d' % (len(lines), len(out.split('\n')), len(b), len(out.encode('utf-8'))))

if __name__ == '__main__':
    main()
