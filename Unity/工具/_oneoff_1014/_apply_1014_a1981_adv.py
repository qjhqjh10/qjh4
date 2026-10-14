# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · A1981 追加对抗性验证结论 + 新账 A2006（督军费用显示两边都不对）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_a1981_adv.py
"""
import io

MAIN = r'D:\4\项目任务.md'

A1981_SEG = (
    " ✅✅ **2026-10-14 对抗性验证回来了 ⇒ 【裁决维持：`2` 是真值】—— 反证找到一条、但推不翻；"
    "而且它带来一条【必须连带】的修正（见新账 `A2006`）**：\n"
    "**① 正证被加强**：补核了**整条继承链** —— `RawCardScript__ctor → ObtainableItem__ctor → AddressablesBuildNumber__ctor`，"
    "**四个环节没有一个写 `+0x78`**；另扫了**全部 `*.ctor.c` 里写 `(param_1 + 0x78)` 的 18 个**，**全是无关类**"
    "（`PlayerManager` / `PhotonView` / `ShopOfferDataV2` / `UI_InfiniteScroll` …）⇒ **`2` 不可能是构造器默认**。\n"
    "**② 🔴 反证找到了一条 —— 而它正好解释了「PnP 上没有费用六边形」**：`BasicCardUI__SetRawCardData.c:51,81,84` 是**唯一一处按督军身份动费用的代码** —— "
    "`GameStaticData__IsWarlordEquivalent(card)`（判据 = `RawCardScript + 0x94 == 10`，即 `cardType == Hero`）为真时，"
    "`:84 CardTextCountersController__Toggle(costCtrl, false, …)` = **费用容器整块隐藏**；"
    "⚠️ **但 `:77-78` 照样 `SetText(costCtrl, card.manaCost)`** ⇒ **值写进去了、只是不显示** ⇒ **这正是「给了但界面不显示」的形状**，"
    "与「数字版不印 PnP 那颗六边形」完全自洽。另：`BattleCardUI__SetRawCardData`（战斗侧）**只碰生命/近战/远程/护甲、没有费用位** ⇒ 场上也不显示。\n"
    "**③ 但它推不翻裁决** —— 因为**没有一处把 `manaCost` 读成 0 / 忽略 / 清零**；`CardScript__UpdateCostText.c:19-23` 对**任何**卡（含督军）都无条件 "
    "`SetText(cost) = CurrentCost`。\n"
    "**④ 🔴 连带修正（必须做）**：**我们侧【没有】「督军不印费用」这条** —— `Core/CardView.cs:897 Show(_costBg, !board)` 是**唯一**的门"
    "（按类型分档的机制 `FaceShowsArmy` 只用在阵营行）⇒ **卡组池 / 收藏 / 详情里督军卡会印出费用六边形**（今天印 `0`，`A1773` 改完**印 `2`**），"
    "而**原版把它整块藏了** ⇒ **两边都不对**，只改数字 = **把一个错数换成另一个错数** ⇒ **新账 `A2006`**。\n"
    "**⑤ ⚠️ 但「督军费用一律不显示」也【不成立】** —— 反例：`UICardInfoItem__Initialize.c:76` ＋ `__SetCost.c:26`（调用者 `DeckListDrawer__SpawnCardInfo.c:76`）"
    "**没有督军分支**，判据只有 `manaCost >= 0` ⇒ **原版【卡组列表行】会印出 `2`**；⚠️ 而**我们反而在那里藏了**"
    "（`Deck/DeckRuntime.cs:4153 SetOn(_deckRowCost[i], on && !heroRow)`）⇒ **可能反向偏离**"
    "（要读预制体才能定：`CardListItemScript` 有 `isHero(+0x90)` 字段、`DeckListDrawer` 又给督军行换过 sprite ⇒ **不能排除它用的是另一套不带费用文本的 prefab**）。\n"
    "**⑥ 我们侧的消费面**：`RuleEngine/Core/CardDef.cs:2709 IsUnit => Type==\"unit\"||\"hero\"` ⇒ 督军算单位（同原版）；"
    "`Core/CreatePool.cs:159/175` 的 `troops` 支**会把督军挡掉**（实测 `Sautekh` 2 费部队 = **6 张**，与 `RuleEngineTest.cs:11050` 的期望一致 "
    "⇒ **改成 `2` 不破那条断言**）；🔴 **但** `CreatePool.cs:196-213`（只剩阵营词那一支、**没有 type 门**）⇒ 若某张卡是「阵营词 ＋ `costMin>=2`」，"
    "改完**督军可能新进候选池**（⚠️ **没穷举 1126 张的语料 ⇒ 是「可能」不是实测**）。\n"
    "**⑦ 关于那 3 张 `0`**：全是 `DarkAngels_war_*`（`cardArmy=110`、`cardShortId` 855/856/857 **连号**、且 `cardType` 五个字段全空串）"
    "⇒ 更像**漏填**；**反证**：之后加的 `EmperorsChildren` 督军（`912/913/914`）**仍是 `2`** ⇒ **不支持「`2` 是旧批次残留」**。\n"
    "**⑧ ⛔ 没查清**：督军卡面 prefab 上 `Cost Container` 的 `ActivationRules` 位在 asset 里怎么设（但**改不了** `option=false` 的无条件隐藏）；"
    "**原版卡组列表行到底印不印**（要读预制体）；现池里到底有没有「阵营词 ＋ `costMin>=2`」的效果。"
    "⚠️ 同批第 **6** 例「文件名与内容对不上」：`PlayerItem__.ctor.c` 的内容是 `ObtainableItem___ctor`（⇒ 并入 `A1962`）。"
)

A2006 = (
    "| 🆕 **`A2006`** | 🔴 **督军费用显示【两边都不对】—— 改 `cost` 之前必须先把这个补上**（2026-10-14 `A1981` 对抗性验证查出）：\n"
    "① **原版**：`BasicCardUI__SetRawCardData.c:84` 对督军 `CardTextCountersController.Toggle(costCtrl, false)` = **费用六边形【整块不显示】**"
    "（⚠️ 但 `:77-78` **照样把值 `SetText` 进去** ⇒ 「给了、只是不显示」）；`BattleCardUI__SetRawCardData`（战斗侧）也没有费用位。\n"
    "② **我们**：`Core/CardView.cs:897 Show(_costBg, !board)` 是**唯一**的门 ⇒ **卡组池 / 收藏 / 详情里督军卡会印出费用六边形**"
    "（今天印 `0`，`A1773` 改完**印 `2`**）⇒ **只改数字 = 把一个错数换成另一个错数**。\n"
    "⇒ **要做**：给 `CardView` 补「`Type == \"hero\"` ⇒ 不显示费用底板」的口径"
    "（⛔ **判据 = 原版 `GameStaticData__IsWarlordEquivalent`（即 `cardType==Hero`）**，⛔ 不是我们自己想的）。\n"
    "🔴 **但反向那条要【单独核】**：**原版卡组列表行【会】印**（`UICardInfoItem` 无督军分支、只看 `manaCost >= 0`），"
    "而**我们反而藏了**（`Deck/DeckRuntime.cs:4153`）⇒ **可能反向偏离** ⇒ 要读 `CardListItemScript` / `DeckListDrawer` 用的那套 prefab 才能定。"
    "⚠️ 与 `A1773`、`A1981` **同批做**（⛔ 别只改数字）。判据原文 → 本会话报告。 |"
)


def block_end(lines, i):
    j = i
    while j < len(lines) and not lines[j].rstrip().endswith(' |'):
        j += 1
    assert j < len(lines)
    return j


def main():
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1981`**')]
    assert len(hits) == 1, hits
    j = block_end(lines, hits[0])
    assert '对抗性验证回来了' not in lines[j], 'A1981 已追加过'
    lines[j] = lines[j].rstrip()[:-1].rstrip() + A1981_SEG + ' |'
    # A2006 紧随 A2005
    h = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A2005`**')]
    assert len(h) == 1, h
    if not [l for l in lines if l.startswith('| 🆕 **`A2006`**')]:
        j2 = block_end(lines, h[0])
        lines[j2 + 1:j2 + 1] = [A2006]
        print('  +A2006')
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('OK A1981 追加 + A2006；总行数 %d' % len(lines))


if __name__ == '__main__':
    main()
