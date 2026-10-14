# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · A1981 / A1983 查证的落账 + 两条新账（A1996 initiative 整列哨兵 · A1997 manaCost=-1）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_cost_state.py
"""
import io

MAIN = r'D:\4\项目任务.md'

A1981_SEG = (
    " 🔴 **2026-10-14 只读查证（比上一轮深得多）—— 仍【判不出】，但哨兵问题解决了、且【找到了消费点】**：\n"
    "① 🔴 **上一轮「找不到造价消费点」是【搜法】问题** —— 原版**直接读字段偏移**：`EntityScript__get_CurrentCost.c:72-75` 现读 = "
    "`费用 = RawCardScript.manaCost(+0x78) ＋ 各 buff 的 costChange 增量；负值夹到 0`；"
    "`CardScript__UpdateCostText.c:11-14`（21 行）= **卡面那颗数字是谁画的**（`get_CurrentCost()` ×2 ＋ 裸值 `rawCard+0x78`）。字段名 → `il2cpp_out/dump.cs:23256`。\n"
    "② 🔴🔴 **本 schema 的「无费用」哨兵是 `-1`、不是 `2`**（本轮最硬的一条）：`UICardInfoItem__SetCost.c:24-27` = `SetActive(manaCost >> 31 ^ 1)` "
    "⇒ **`manaCost >= 0` 才显示费用**；真值里 **`manaCost == -1` 共 99 张**，**全是「不占手牌费用」的卡**"
    "（`spellType=200` 的环境/进攻卡 `Normal Conditions (*)`/`Solar Storm`/`Warp Storm`/`Night Attack`/`Dawn Attack`… ＋ `spellType=190` 的 enchantment ＋ 少数 token）。"
    "`cardType=20` 分布 = `{-1:99, 0:51, 1..10}`；**`cardType=0`（部队 540）一个 `0` 都没有** ⇒ **`-1` 是显式的「无费用」标记、`0` 是另一回事**（另见新账 `A1997`）。\n"
    "③ **督军的 `2`**：`cardType=10` 60 张 = **`2`:57 ＋ `0`:3**（`DA1/2/3`），**没有任何别的取值**；短 id 与 hex-guid 两份重复督军读出来**同为 2**。\n"
    "④ 🔴 **「同一份导出里有整列没填」有实证**：**`initiative` 全表 1190/1192 = `20`**（另 2 张教学督军 = 30）—— `initiative` 是逐卡不同的属性 "
    "⇒ 这份导出**确实存在整列恒定的未填列**（⇒ 新账 **`A1996`**）。⚠️ **但这条判据是【双刃】**：同一次查证也证明这份导出**有**真值同值的列"
    "（督军 `armour` 60/60 全 0 是**真值** —— PnP 无护甲徽记）⇒ **「同类型同值」本身不能判哨兵**。\n"
    "⑤ **PnP 亲读 3 张（铁律 7 的尺子）**：`Azrael`（真值 0）· `Jain Zar`（2）· `Haarken`（2）—— **三张右上都没有费用六边形**；"
    "对照组 `Deathwing Terminator`（真值 4）**有**蓝边六边形印「4」（位置 = 阵营徽记正下方右上区）⇒ 与 2026-09-26 的「56/56 无费用六边形」一致。"
    "⚠️ **但这只能当旁证** —— **铁律 10 第 3 条**明写 PnP 是印刷品、「**版式与显隐规则不当尺子**」（先例：阵营行照 PnP 补、实际**数字版哪都不印**）"
    "⇒ ⛔ **不能单独用它定值**。\n"
    "⑥ **⇒ 不够裁**（两向旁证互相抵消）。**还差什么（三选一即可定案）**：① 一份**更晚的同一字段导出** —— 本地**没有**"
    "（`find d:/2 -name \"*.json\" -size +2M | grep card` 只 1 份）；② **数字版实况**（进原版看督军卡有没有费用六边形）—— "
    "⚠️ **大概率跑不通**（原版收藏/卡组编辑要服务器数据、实例化即黑）；③ 反编译里 **`manaCost(+0x78)` 的写入点** —— **没找到**"
    "（搜过 `grep \"0x78) =\"` / `grep -i mana` 文件名 / `dump.cs` 全库 `manaCost` 仅 **9** 处）。⇒ **维持「要裁」**；**`A1773` 继续不许整列照搬**。\n"
    "⚠️ 顺带订正：简报里写的 `Nemor Zahndrekh` **查不到** —— 真值拼写是 **`Nemesor Zahndrekh`**。"
)

A1983_SEG = (
    " 🔴 **2026-10-14 就地订正（铁律 5）—— 本笔【不是「缺判据」，是「判据早就有、只缺落地」】**：\n"
    "① **枚举真名 = `CardStateOptions`**（⛔ 不是我们 doc 里常写的 `CardState`），`dump.cs:45465-45487` **全表 18 档**；"
    "**`3 = inPlayAttacking`**（攻击动画播放中）· **`0x11(17) = inPlayAminingAttack`**（原版拼写，应为 \"Aiming\" = 已声明攻击/瞄准中）；字段落点 `cardState(+0x228)`。\n"
    "② **置位点只有 5 处、回落点 4 处**（已闭环）：`SetupAttack.c:12` / `TryDraggingFromBoard.c:59` → `0x11`；"
    "`_AttackMeleeAnim…:117` / `_ResolveAttackRangedAnim…:70` / `SetupActiveAbility.c:5` → `3`；"
    "回落 `FinishAfterAttack.c:81` / `_PrepareAttack…:30` / `SetAsHero.c:12` / `SetAsUnitInPlay.c:5` → `2`。\n"
    "③ 一句话语义：**「这张牌此刻正处在『已声明攻击／正在播攻击动画』这一段窗口里，才有东西可取消」**，"
    "否则只打一条 `LogWarning` 然后 `return`（**什么也不做，不是抛错**）。旁证：`IsInPlay.c:7-14` 把 `{2, 0xf, 3, 0x11}` 四档判成「在场上」"
    "⇒ **这道闸比 `IsInPlay` 更紧**（只要 `3`/`17` 两档）。\n"
    "④ 🔴 **我们【早就记下了】这句判据**：`RuleEngine/Core/RuleCore.cs:3285-3286` **逐字引了** `CancelAttack.c:35`，"
    "**全表 18 档的文档在 `:3262-3299`**（并已把 `{2,0xf,3,0x11}` 与 `CardScript.IsInPlay()` 对齐）⇒ **缺的是落地，不是判据**。\n"
    "⑤ ⚠️ **但它不是引擎状态、是【表现层状态字】** —— 我们**没有 `cardState` 这一类字段**；对应的**概念** = 「本次攻击**已声明、尚未结算**」那一刻，"
    "即 `RuleCore.cs:4267` 的 `ctx.Emit(EvtKind.Attack, …)` **之前**；而 `TryCancelAttackByOwnAboutToAttack`（`:4142`）**正好落在 `Emit` 之前**"
    "（`:4220-4223` 注释明写）⇒ **落地方式要按「我们有没有等价的状态窗口」重新想**，⛔ **别照抄原版那个字段**。\n"
    "⚠️ 别搞混：`AboutToAttack` 在我们这边是**关键词/触发**（`AbilityTrigger.AboutToAttack = 280`），**不是状态字**。\n"
    "⚠️ 同批第 4 例「文件名与内容对不上」：`CardData__get_cost.c` 的内容其实是 `PlayerItem__get_Expansion` ⇒ 并入 `A1962`。"
)

A1996 = (
    "| 🆕 **`A1996`** | 🔴🔴 **`initiative` 在真值里【整列恒定 20】⇒ 强烈疑似「未填列」；若我们的 `card_initiative.json` 是从它生成的，"
    "那一整列可能都是哨兵**（2026-10-14 `A1981` 查证顺手查出 · **高优先级**）：真值 `cards_1195_2022时代.json` 里 **`initiative` = 20 的有 1190/1192 张**"
    "（另 2 张是教学督军 = 30）。🔴 而 `initiative`（先手权）在原版里**是逐卡不同的属性** ⇒ **整列同值 = 这份导出确实存在整列没填的列**。\n"
    "⚠️ **旁证是双刃的**：同一次查证也证明这份导出**有**真值同值的列 —— 督军 `armour` 60/60 全 0 是**真值**（PnP 无护甲徽记）"
    "⇒ **「同类型同值」本身不能判哨兵**，要看「这个属性在原版里到底该不该逐卡不同」。\n"
    "⇒ **要做**：① 现读 `数据/游戏数据/card_initiative.json` 与 `cards_engine.json` 的 `initiative` 列**分布**（是不是也几乎恒定）；"
    "② 若恒定 ⇒ **去找它真正的判据**（反编译 / PnP 卡图 / 别的导出）；③ ⛔ **别拿 `20` 当值用**。"
    "⚠️ 顺带（纯报、未查）：`card_stats_raw.json`（OCR 表）里督军读出 `Jain Zar=5` / `Haarken=2` / `Azrael=null` —— 与「OCR 把阵营徽记读成数字」那笔结案自洽。"
    "判据原文 → `资料/普查产出_第三十会话/`（本会话报告）。 |"
)

A1997 = (
    "| 🆕 **`A1997`** | ⚠️ **原版给「不占手牌费用」的卡标的是 `manaCost = -1`、不是 `0`**（2026-10-14 `A1981` 查证顺手查出）："
    "真值里 **`-1` 共 99 张**，**全是环境/进攻卡**（`spellType=200`：`Normal Conditions (*)`/`Solar Storm`/`Warp Storm`/`Night Attack`/`Dawn Attack`…）"
    "＋ enchantment（`spellType=190`）＋ 少数 token；而**我们给这类卡标的是 `0`**（⛔ **未查我方现值，纯报**）。"
    "⇒ **要做**：先核我们表里对应那一批现在是什么值；若为 `0` ⇒ **是偏离** —— 原版用 `-1` 表达「无费用」，"
    "`UICardInfoItem__SetCost.c:24-27` 的 `manaCost >= 0 才显示` 就是那条 UI 分支（**`-1` 会让费用六边形整颗不画**）。\n"
    "⚠️ **同批还有一条独立的「数据源可疑」（纯报、未查）**：`数据/游戏数据/card_stats_raw.json`（OCR 表）里**存在另一套目录结构**的键"
    "（`Chaos\\Core\\…` / `Aeldari\\Storm of Silence\\…`，与现在的 `1督军/` 并存）⇒ **可能有两份 PnP 树**；若是，OCR 的**源头**要重核。"
    "判据原文 → 同上。 |"
)


def append_seg(head_prefix, seg):
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
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
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +%s (尾行 L%d)' % (head_prefix, j + 1))


def main():
    for cid, seg in (('A1981', A1981_SEG), ('A1983', A1983_SEG)):
        append_seg('| 🆕 **`%s`**' % cid, seg)
    b = io.open(MAIN, 'rb').read()
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1995`**')]
    assert len(hits) == 1
    j = hits[0]
    while not lines[j].rstrip().endswith(' |'):
        j += 1
    lines[j + 1:j + 1] = ['\n'.join([A1996, A1997])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A1996/A1997')


if __name__ == '__main__':
    main()
