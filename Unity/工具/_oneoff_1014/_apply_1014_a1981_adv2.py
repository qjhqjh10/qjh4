# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · A1981/A2006 追加「淘汰性补正」：反证主判据换成 CheckIfRawCardMeetsCriteria
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_a1981_adv2.py
"""
import io

MAIN = r'D:\4\项目任务.md'

A1981_SEG = (
    " 🔴 **2026-10-14 追加（同族函数漏读的补正 —— 反证第 4 条的【主判据换成更强的一条】）**："
    "补读了 `FilterMethods__CheckIfRawCardMeetsCriteria.c` —— 它**才是「从手牌 / 牌库 / 卡池里按费用挑卡」的正路**"
    "（查的是 **raw 卡**、不是场上实体），调用者 = `AbilityLogic__GetCardToSummon`（取召唤/部署哪张）· `BoardAnalysis__DeckMeetsCriteria`（牌库筛选）· "
    "`AbilityLogic__GetTargets`。它**把这个问题问死了、两面都有**：\n"
    "· `:29-35` = 与实体版**同一套「每 `cardType` 一个开关位」**：`iVar10 = *(int*)(param_1 + 0x94)` 就是 `cardType`；"
    "**`cardType == 10(Hero)` 要 `criteria + 0x16` 这一位为真才放行，否则整条 `return 0`**（督军**连被比较的资格都没有**）；\n"
    "· `:55` = **放行之后确实比较费用**：`MeetsStatCriteria(*(param_1 + 0x78), 0, *(param_2 + 0x58), *(param_2 + 0x5c), …)` "
    "⇒ `param_1 + 0x78` = `RawCardScript.manaCost`、`param_2 + 0x58 / 0x5c` = 该 criteria 的**费用下界 / 上界**。\n"
    "⇒ **正面回答「有没有消费面」：有（就是它），它读的正是督军的 `+0x78`**；但**被一道显式的「包不包含督军」bool 挡住** —— "
    "该 bool **未在 asset 里设过就是 `false`** ⇒ **默认不包**。\n"
    "⇒ 与原版对督军费用的一贯态度**完全一致**：**「有此字段、默认不呈现（`BasicCardUI` 那条）、默认不参与挑选（本条）」**。\n"
    "🔴 **对本行的修改**：上面 ⑥ 里那条**要升级** —— `RuleEngine/Core/CreatePool.cs:196-213`（只剩阵营词那一支、**没有 type 门**）"
    "**不是「可能」，是【真偏离】**（原版这道门在 `CheckIfRawCardMeetsCriteria:29-35`、**默认不包督军**）⇒ ⛔ **别只按「可能」记**。\n"
    "⚠️ 原先那条 `FilterMethods__CheckIfMeetsCriteria.c:281,285`（实体版）**保留但降为佐证**，**主判据换成这一条**。"
    "其余不变 —— **仍然找不到任何一处把督军 `manaCost` 读成 0 / 清零 / 忽略**；`2` 依旧是**可被写、可被比、"
    "且在卡组列表行（`UICardInfoItem`）会被印出来**的真值。"
)

A2006_SEG = (
    " 🔴 **2026-10-14 追加（同批补读 —— 还有【第三处】要连带）**：`RuleEngine/Core/CreatePool.cs:196-213`（「只剩阵营词」那一支）"
    "**没有 type 门**，而**原版有** —— `FilterMethods__CheckIfRawCardMeetsCriteria.c:29-35`：`cardType == 10(Hero)` 要 `criteria + 0x16` "
    "那一位为真才放行，否则**整条 `return 0`** ⇒ **改完 `cost` 之后，「阵营词 ＋ 费用区间」类效果会把督军放进候选池、而原版不会** "
    "⇒ **这是真偏离**（⛔ **不是「可能」**）⇒ **要照原版补那道 type 门**（判据 = 上面那个函数，⛔ 不是我们自己设计的规则）。"
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
    for head, seg, tag in (('| 🆕 **`A1981`**', A1981_SEG, 'CheckIfRawCardMeetsCriteria'),
                           ('| 🆕 **`A2006`**', A2006_SEG, '第三处')):
        h = [i for i, l in enumerate(lines) if l.startswith(head)]
        assert len(h) == 1, (head, len(h))
        j = block_end(lines, h[0])
        if tag in lines[j]:
            print('  = %s 已追加过' % head); continue
        lines[j] = lines[j].rstrip()[:-1].rstrip() + seg + ' |'
        print('  +%s' % head)
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('OK 总行数 %d' % len(lines))


if __name__ == '__main__':
    main()
