# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 三处判据缺口的落账：
 ① A1763 追加「口径已裁：只换 descZh」+「[[melee]]/[[ranged]]/[[reanimate]] 不用接」
 ② A1982 追加「② 已查实并裁掉」
 ③ A1975 / A1990 追加「判据齐：原版起始单位不吃召唤病」
 ④ 新账 A1995（官方 EN 对比出的 8 处卡面文案差异）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_gap3.py
"""
import io

TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
MAIN = r'D:\4\项目任务.md'

A1763_SEG = (
    " 🔴 **2026-10-14 口径已裁（只读查证 + 主对话裁断）—— 只换 `descZh`，`desc` 不动**：\n"
    "① **原以为「只换 zh 更便宜」，现核那个前提【不成立】**：**官方 zh-CN 列【同样是模板】**"
    "（现数：`[[…]]` **533** 条 · `((…))` **308** 条 · `$$…$$` **120** 条 · **但 132 张官方 zh 为空**）"
    "—— 与 EN 列是**同一层渲染**，两列都要先有渲染器。\n"
    "② **真正的分野 = 「动不动解析器」**：`desc` 是**唯一解析输入**（现读 **9 处** `EffectText.Parse` 调用点："
    "`EffectResolver.cs:1391,1551,3455,3549` + `EffectText.cs:1879,1976,2000,4294,4339`；另有 `CardDef.cs` 7 处 · "
    "`DeckBuilder.cs:385` · `CardDatabase.cs:146` · **`工具/ruleprobe/PlayProbe.cs:435,447,1055,1095`**）"
    "⇒ **任一字符变化都会移动 `ruleprobe` 的 `faceOps`/`changes`/`changesFull` 列 与 `PlayProbe.PinnedPlayMd5` 哨兵**，"
    "并牵动 `RuleEngineTest.Run` 的全池那几条；而 **`descZh` 全仓【无一处】进解析器**（现扫复核成立 ⇒ `EffectText.cs:7326` 那句口径可放心用）。\n"
    "③ **换 `desc` 也【没有信息增益】**：官方 EN 与我们 `desc` 逐字相同只有 **74/832 = 8.9%**，但那个「不同」**正是「模板 vs 渲染后的成品句」**"
    "（例 `ASH83` 官方 `[[waystone]]. ((Spirit_x,1)) Give ((Melee_attack_icon_x,+1)) …` ↔ 我们 `1 [Spirit Stone]: Give +1 [Melee] …`）"
    "⇒ **我们那一列已经是同一模板的渲染结果**。\n"
    "④ ⚠️ **官方 zh 为空的那 132 张要【保留我们旧译】**（⛔ 别清空）。\n"
    "⑤ 🔴 **同一趟查实：`[[melee]]` / `[[ranged]]` / `[[reanimate]]` 【不用接进卡面渲染层】** —— 这三条双括号形态"
    "**只出现在 `Missions/*` 任务文案**里（`melee` **4** · `ranged` **4** · `reanimate` **2**），"
    "`Card_Description/*` / `Modular_Description/*` / `Card_Name/*` / `Card_Abilities/*` 里 **0 处**；"
    "我们池里的 `[Melee]`/`[Ranged]` 是**我们自己的记号**、语义来源是官方卡图上的**图标**（`Atlas_trait_icon_Melee`/`_Ranged`）——"
    "亲核 `TL20 Carnifex` 的 PnP 卡面**逐位置对应** ⇒ **接了三也永远没有输入**（⛔ 别为它们写代码）。"
)

A1982_SEG = (
    " ✅ **2026-10-14 判据已给 + 已裁**：① 的 **`Modular_Description/*` 缺口仍在**（**340 条还没进工程**，"
    "它是反编译 `SupportMethods__ModifyLocalization.c:75-186` 那一支 `((Key,arg))` 的**必需半边**）；"
    "② 的「只换 `descZh` 还是连 `desc` 一起换」**已查实并裁掉** —— 结论与依据（含 `desc` 的 16 处读者清单、"
    "`descZh` 无一处进解析器、官方 EN 逐字相同仅 8.9% 且那 8.9% 之外全是「模板 vs 渲染句」）"
    "**写在 `资料/任务_卡组卡牌战斗.md` 的 `A1763` 那一行**（本路查证的主产出）。"
)

A1975_SEG = (
    " ✅ **2026-10-14 判据齐（只读查证 · 反编译逐句）⇒ 原版起始单位【不吃】召唤病**："
    "`CardScript__CardSetup.c`（**350 行全文逐句读过**）**一次都没写 `0x58`**"
    "（`grep -c \"0x58|0x230|0x23c|0x2e0\"` = **0**）；连它调的 `ActivateTraitsOnSummonOrEnchantment`"
    "（`CreateStartingTroop.c:47`）也**只写 `canAct`/`canAttack`**、**不写 `summonSickness`**（trait 只对 `fast(0x28)`/`flank(0x1cc)`/`ferocity(0x4f1)`/`oath(0x4fb)` 生效）。"
    "🔑 **不是「有人清了它」，是「整条创建路一次都没置它」** —— 置 `0x58 = 1` 的**全部两处**"
    "（`CardScript._MinionPlayedIntoField_d__314__MoveNext.c:70` 出牌/召唤/自动召唤/变形 · `CardScript__ResetSummonSickness.c:8` 残骸/交换/偷取）"
    "**起始兵两条都不走**。⇒ **补钩子时可以放心，⛔ 不会顺手写错 `summonSickness`**；但 **trait 激活仍要补**（原版 `:47` 调了、我们没调）。"
    "字段名判据 → `dump.cs`（`summonSickness 0x58` · `canAct 0x230` · `canAttack 0x23c` · `cardState 0x228`）。"
)

A1990_SEG = (
    " ✅ **2026-10-14 同批查实**（判据见 `A1975` 那一行）：**起始单位不吃召唤病** ⇒ "
    "**补钩子时 ⛔ 别顺手写 `summonSickness`/`Exhausted`** —— 那两半正在 `ApplyDeployTurnState` 里，**直接搬它 = 另一档行为**。"
)

A1995 = (
    "| 🆕 **`A1995`** | ⚠️ **拿官方 EN 与我们的 `desc` 逐张比，盘出 8 处卡面文案差异**（2026-10-14 只读查证顺手 + 主对话采用）："
    "① **`ASH83 Autarch` 疑似漏了一整句** —— 官方 EN 起头是 `[[waystone]]. ((Spirit_x,1)) …`，而我们 `desc` **整条没有 \"Waystone\"**，"
    "而**同一批的 `ASH77` 我们写了 `Waystone.`** ⇒ **同族两种做法**（⚠️ **还差一步**：去亲读 "
    "`d:/2/Warpforge部队卡片/Aeldari/…Warpforge_83_Autarch.png` 核卡面到底印没印）；"
    "② **6 张只差句末句点**（官方无 `.`、我们有）—— 报告里点了 5 个名：`AM18` · `AM52` · `EC37` · `GSC76` · `SAU58`；"
    "③ **`ASH48 Cosmic Serpent` 我们【多一整句】** `2 [Spirit Stone]: Repeat this effect`（官方 EN 没有）⇒ **疑似我方多写**。"
    "⇒ **要做**（铁律 11 + **铁律 7：以 PnP 成品卡图为尺**逐张定，⛔ **别只按官方 EN 判** —— 官方 EN 是**模板层**）；"
    "⚠️ 改 `desc` = 动解析器输入 ⇒ **与 `A1763` 那批一起排**（⛔ 别单独改，否则要把 `PlayProbe.PinnedPlayMd5` 再动一次）。"
    "判据原文 → `资料/普查产出_第三十会话/`（本会话报告 ③ 的顺手发现）。 |"
)


def append_seg(path, head_prefix, seg):
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, path
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith(head_prefix)]
    assert len(hits) == 1, (head_prefix, len(hits))
    j = hits[0]
    while j < len(lines) and not lines[j].rstrip().endswith(' |'):
        j += 1
    assert j < len(lines)
    lines[j] = lines[j].rstrip()[:-1].rstrip() + seg + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(path, 'wb').write(out.encode('utf-8'))
    print('  +%s (尾行 L%d)' % (head_prefix, j + 1))


def main():
    append_seg(TASKS, '| 🆕 **`A1763`**', A1763_SEG)
    for cid, seg in (('A1982', A1982_SEG), ('A1975', A1975_SEG), ('A1990', A1990_SEG)):
        append_seg(MAIN, '| 🆕 **`%s`**' % cid, seg)
    b = io.open(MAIN, 'rb').read()
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1994`**')]
    assert len(hits) == 1
    j = hits[0]
    while not lines[j].rstrip().endswith(' |'):
        j += 1
    lines[j + 1:j + 1] = [A1995]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A1995')


if __name__ == '__main__':
    main()
