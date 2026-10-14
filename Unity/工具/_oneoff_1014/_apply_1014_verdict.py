# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · ① 修 §29·d 里漏改的那一行（Card_Description/*）② A1981 裁决落盘
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_verdict.py
"""
import io

MAIN = r'D:\4\项目任务.md'

ROW_HEAD = "| 🆕 **卡面整句词条表 `Card_Description/*`** |"
ROW_ADD = (
    " ✅ **2026-10-14（第三十会话）就地订正（铁律 5）—— 本行【不再是「等远端」】**：`Card_Description/*` **就在本地的官方本地化表里**"
    "（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json`，`terms` 9,441 条；其中 **`Card_Description/*` 共 1098 键** = **951 非 hex ＋ 147 个 32 位 hex 键**）。"
    "🔴 **但本行的【后半句仍然成立、而且更要紧】**：拿到的是**模板层**（`[[waystone]]. ((Spirit_x,1)) Give ((Melee_attack_icon_x,+1)) …`），"
    "**不是渲染后的整句** ⇒ 「**卡面文本的判据只有两条：PnP 成品卡图 ＋ 数据自己的主流写法**」这条口径**不变**；"
    "「⛔ 别再试从客户端里读卡面整句」也**仍要遵守**。⇒ **落点 = `A1764` / `A1763`**（`A1763` 的口径已裁：**只换 `descZh`、`desc` 不动** —— 见 `资料/任务_卡组卡牌战斗.md` 的 `A1763` 那一行）。"
)

A1981_VERDICT = (
    " ✅ **2026-10-14 【主对话已裁：照真值落 —— 57 张 = `2`、`DA1`/`DA2`/`DA3` = `0`】**（⏳ 另派一路**对抗性验证**中，回来若有反证则改判）：\n"
    "🔴 **决定性证据 = 构造函数里【没有】`manaCost`**：`d:/2/tools/decomp_full/RawCardScript__.ctor.c`（**全文 29 行**）只写了 **5 个字段** —— "
    "`+0x48 = 5` · `+0x58 = 1` · `+0xac = 4` · **`+300(0x12C) = 0x14`（= `initiative` 默认 20）** · `+0x241 = 1`；"
    "**`+0x78`（`manaCost`）不在其中**；且签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:27` = `public int manaCost;`（**无字段初始化器**）"
    "⇒ **那个 `2` 既不是构造函数默认、也不是字段初始化器 ⇒ 它是真值（逐卡序列化数据）**。\n"
    "🔴 **对照（同一次查到，顺带把 `A1996` 钉得更死）**：`initiative` 的 `20` **恰恰是构造函数写死的**（`+0x12C = 0x14`）⇒ 所以那一列 1190/1192 同值；"
    "**两列的差别正好解释了「一列同值、一列逐卡不同」**。\n"
    "② 本 schema 的「无费用」哨兵是 **`-1`**（`UICardInfoItem__SetCost.c:24-27` = `SetActive(manaCost >= 0)`；真值里 `-1` 99 张全是环境/进攻/enchantment/token）⇒ `2` **不是哨兵**。\n"
    "③ 真值自己在 3 张上给了 `0`（`DA1/2/3`）⇒ **不是整列默认**。\n"
    "④ **有真实消费面**（不是纯装饰）：`EntityScript__get_CurrentCost.c:72-75` 是费用读点；我们侧 `RuleEngine/Core/CreatePool.cs:133/134/301` 按 `Cost` 筛卡 ⇒ "
    "**「找一张 N 费卡」那一族可能挑中督军**。\n"
    "⚠️ **反面证据（记着，别忘）**：已亲读 3 张督军 PnP 成品图、**右上没有费用六边形** —— 但按**铁律 10 第 3 条**「**版式与显隐规则不当尺子**」"
    "（先例：阵营行照 PnP 补、实际数字版哪都不印）⇒ **它既不能当正证、也不能当反证**（拿它推翻本裁决 = 用错尺子）。"
)

A1692_VERDICT = (
    " ✅ **2026-10-14 措辞已裁**：代码注释**写「原版行为不可比」**（⛔ 别写成「照原版」）—— 因为原版**没有「目标格被占」这一档**"
    "（`MinionManager__GetNextSlotWithoutDisplacing.c:14-30` 全文只比两个 `List.Count`、两侧是无洞 `List`），"
    "**加闸是补我们表示法的洞**。判据见本行前面的查证段。"
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
    for head, seg in (('| 🆕 **`A1981`**', A1981_VERDICT), ('| 🆕 **`A1692`**', A1692_VERDICT)):
        hits = [i for i, l in enumerate(lines) if l.startswith(head)]
        assert len(hits) == 1, (head, len(hits))
        assert seg not in '\n'.join(lines), '%s 已追加过' % head
        j = block_end(lines, hits[0])
        lines[j] = lines[j].rstrip()[:-1].rstrip() + seg + ' |'
        print('  +%s' % head)
    hits = [i for i, l in enumerate(lines) if l.startswith(ROW_HEAD)]
    assert len(hits) == 1, ('Card_Description 行', len(hits))
    if '不再是「等远端」' not in lines[hits[0]]:
        j = block_end(lines, hits[0])
        lines[j] = lines[j].rstrip()[:-1].rstrip() + ROW_ADD + ' |'
        print('  +§29·d 的 Card_Description/* 行')
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('OK 总行数 %d' % len(lines))


if __name__ == '__main__':
    main()
