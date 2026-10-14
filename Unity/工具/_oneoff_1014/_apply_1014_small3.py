# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 三处小判据的落账：
 ① A1989 前置（扫全池 = 1 张）+ 同族没扫的提醒
 ② A1690 前置（原版判据 = Self(100)，⛔ 别新造锚点）
 ③ A1969 改判为「人造负例 ⇒ 可核销」
 ④ 新账 A1998（负例断的是「失败」不是「为什么失败」）
 ⑤ 交接 §三 补一条坑：`_tmp_view` 有两份
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_small3.py
"""
import io

MAIN = r'D:\4\项目任务.md'
TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
HANDOFF = r'D:\4\Unity\资料\交接_第三十会话.md'

A1989_SEG = (
    " ✅ **2026-10-14 前置扫全池已完成 ⇒ 同类句 = 【1】张（就是 `GSC29`），M = 1**（拿不准的 0 张）："
    "扫法 = 按 `EffectText.Split` 的边界（`Core/EffectText.cs:1014` 的 `.`/`\\n`/`\\r`）切段 → 剥触发头（`Rally:`/`Duty:`/`Mob:`/`Oath N:`…）→ "
    "取「**段首 = `choose `** 且**同段后文**出现 `lower (their|its|the|a) cost by N` / `costs N less` / `reduce … cost`」的段。"
    "**静默丢确认**：`D:/4/_tmp_view/w15_scan.txt:411` 那一行只有一条 op（`choosecard/pool/2-cost genestealer cults troop/copies/…`），"
    "`{SRC:}` = **整句**，**`lowercost` 既不进 op、也不进 `unparsed`/`partial`**。"
    "**根因复核（静态读码）**：`TryChooseCard` 在 `EffectText.cs:2617` **早于** `TryLowerCost`（`:2752`），"
    "且命中后 `:5110-5121` **直接 `r.Kind=Ok; return true`、不切 ` and ` 尾句**。"
    "🔴 **卡面旁证（铁律 7）**：`Genestealer Cult/3部队/Warpforge_31_Metamorph-Iconbearer.png` **亲读** —— "
    "卡面就是 `… in your hand Lower their cost by 1`（**没有 `and`、没有句号**）⇒ **`desc` 忠实、修在解析层（⛔ 不要改数据）**。"
    "**对照组（同形状但尾巴剥得出来的）**：`DA_Convoke_the_Circle` · `AM_Astropath` · `TAU48` · `UM93` · `GSC55` —— "
    "它们**都不是 `choose` 开头** ⇒ 走 `Finish` 递归 ⇒ scan 里都带 `{TAIL:…}` + 独立的 `lowercost` op。"
    "🔴 **同族【没扫】**：这一族里**别的未进 `TryChooseCard` `act` 表**的写法"
    "（`act` 表只认 `return it to your deck/hand` · `draw it` · `decktop` · `enemyhand` · `todeck` · `increase its cost by` · copies/deploy/hand 那几种）"
    "**只扫了「降费」一种** ⇒ **要补扫**（⛔ 别以为修完 `GSC29` 这一族就干净了）。"
)

A1690_SEG = (
    " ✅ **2026-10-14 判据已给，而且【既不是 ① `Unset`、也不是 ② 新造锚点】**：**原版判据指向 `Self`**"
    "（= 原版 `adjacentToSelfIfMeetsCriteria = 100`）；「**事件宾语**」那一档**就是现成的 `TargetIfMeetsCriteria`（= 110）** "
    "—— ⛔ **原版【没有】「事件参数」这个独立枚举值，别新造**。\n"
    "**判据（第一权威 ＋ 真值导出两侧都取到）**："
    "① `RawCardScript__TriggerAbilities.c:250`（**已现读确认文件与名相符**）= `AbilityLogic__GetTargets(board, logic, param_4, param_4, param_6, 1)` "
    "⇒ **`param_3`(self) = `param_4` = 拥有这条 ability 的那张牌**、**`param_5`(target) = `param_6` = 触发事件的**对象**；"
    "② `AbilityLogic__GetTargets.c:1001` `targetsAffected == 100` → `GetAdjacentUnits(board, param_3)`；"
    "`:1046` `== 110` → `if (param_5 != 0) GetAdjacentUnits(board, param_5)` —— **`param_5 == 0` 时一个都不加（空集），⛔ 不退回「自己」**；"
    "③ **真值导出全扫**：带相邻锚点共 **38 处** = `Self(100)` **17** · `TargetIfMeets(110)` **17** · `FriendWarlord(113)` **4**，"
    "而 **17 处 `Self` 全是「卡自己拥有的触发式/常驻式 ability」**（TurnEnd 40 / Codex 145 / WhileInPlayVariable 240 / Mob 300 / Strike 580 / Cruelty 720）"
    "⇒ **原版「事件触发的裸 `adjacent`」用的就是 100（卡自己）**；110 只在**有宾语**时可用。\n"
    "**⇒ 修法**：把「**这段是 `When` 正文**」**显式传参**（与现成的 `eventHasTarget` **同一形状、两条闸共用一份判据**）："
    "事件**带宾语** ⇒ `TargetIfMeetsCriteria`（**现状已正确**）；事件**不带宾语** ⇒ 走 ④ `Self`（原版 100），"
    "**并从这条路上摘掉 ⑤ 的 `AdjacentFailed`**。\n"
    "🔴 **本笔的实质 = 【同一个句子、两个入口判定不一致】**：⑤ 的闸是**文本**判据 `low.StartsWith(\"when \")`（`EffectText.cs:1479`），"
    "而事件层 `CardDef.FinishWhenSentence:2549` 传进来的 `body` **已经剥掉了 `When …,` 头** ⇒ ⑤ 永不触发、落 `:1488` 的 `Self`；"
    "**但同一模型走 `EffectText` 自己那条入口（`TryWhenPrefixStub` → ⑤·三 `:1515`）时会置 `adjFailed`**。\n"
    "⚠️ **两档代价（已量化）**：落 `Self` —— 与真值 **17/17** 观测一致，风险 = 若事件解析器**漏判** `evHasTarget` 会**静默锚错** "
    "⇒ **必须配一条断言**（`CardDef.cs:2546-2548` 算 `evHasTarget` 那句要与 `WhenEvents.ParseAll` 的宾语口径**同步**）；"
    "落 `Unset` —— 零错打，代价 = 这类卡面打 `*`、**当前池 0 张**吃到。\n"
    "⚠️ **未查清**：`SW25`/`SW43` 的原版 ability 数据**取不到**（`cards_1195_2022时代.json` 里**根本没有 Space Wolves** "
    "⇒ 与「真值缺 266 张新卡、SW 68 居多」一致）⇒ 那两张「锚 100 还是 110」**只有同族推断、没有逐卡实证**；"
    "且严格说**当前池能踩到那个静默 `Self` 的 = 0 张**。"
)

A1969_SEG = (
    " ✅ **2026-10-14 判据已查实 ⇒ 本笔【可核销】—— 那条 LogError 是【人造分叉负例】打的，故意的，不是缺陷**：\n"
    "· 现读 `D:/4/Unity/_tmp_view/arena3/run_battle_1014.log`：`:207456` = "
    "`BT ★ 反面用例：下面这几行**红字是预期的**（靶场局放不出来 ⇒ 必须出声）`（栈 = `BattleScene.cs:15721`）；"
    "`:209079` = `[Replay] **分叉点 = 第 0 条** …`（栈 = `BattleDriver.PlayReplay(BattleDriver.cs:738)` ← `BattleScene.cs:15722`）；"
    "`:209163` = `BT ✓ ★★ **靶场手改过局面的那局放不出来，而且【报出来了】**（不是静默演成另一局）` —— **绿**。\n"
    "· 代码：`:15719 if (newest != playable)` → `:15722 bool bad = driver.PlayReplay(rec);` → `:15723 Check(!bad, \"★★ …放不出来，而且【报出来了】…\")` "
    "⇒ **期望值就是「必须报分叉」**。\n"
    "· **为什么整跑仍 2896/0**：失败只由 `Check()` 计（`BT ✗` **0 条** / `BT ✓` **2896 条**）；"
    "`PlayReplay` 内部走的是 `Debug.LogError`、**不进 Check 计数器**。\n"
    "⚠️ 顺带更正：**我转述给查证代理的日志行号 `:204145` 是错的**（那里是 `CardView.cs:711` 的材质告警）—— 又一次「⛔ 别信转述的行号」。"
    "⚠️ 同批新账 → `A1998`。"
)

A1998 = (
    "| 🆕 **`A1998`** | ⚠️ **那条回放负例，断言断的是「失败」、不是「为什么失败」—— 而它失败的【原因】已经与注释写的不一样了**"
    "（2026-10-14 `A1969` 查证顺手查出）：① **实际分叉点在第 0 条**（日志 `:209079`，P2 手牌 5 vs 4），"
    "而 `CardPresentation/Editor/BattleScene.cs:15667-15674` 的注释写的是「**分叉点稳稳在第 6 条**」、根因 `UseAlternative` → `ErrNotUnit`；"
    "② 断言只要求「**放不出来**」（`Check(!bad, …)`）、**没断分叉点** ⇒ 所以仍绿；"
    "③ 🔴 **后果**：将来它可能因为**别的原因**红/绿（例：录像目录 `_tmp_view/replays_selftest` **跨轮次留存旧文件**），**而没人看得出来**。"
    "⇒ **要做**：把两处对齐，**或**把「分叉点在第几条」也断上（**`A1972` 那一族**「断言写成同义反复 / 断错对象」的又一例）。"
    "判据原文 → `资料/普查产出_第三十会话/`（本会话报告）。 |"
)

HANDOFF_PIT = (
    "9. 🔴 **`_tmp_view` 有【两份】**：**`D:/4/_tmp_view/`** 与 **`D:/4/Unity/_tmp_view/`** —— "
    "同一个名字、产物住哪一份**取决于哪个工具写的**（例：`w15_scan.txt` / `our_scan.txt` 在 **`D:/4/_tmp_view/`**，"
    "而 `BattleScene.Run` 的日志在 **`D:/4/Unity/_tmp_view/arena3/`**）⇒ **找产物时两边都必须看**，"
    "只查一边会**假阴性**（本会话因它浪费过一轮，`A1969` 那次给的日志行号就是这么错的）。"
)


def append_seg(path, head_prefix, seg):
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, path
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith(head_prefix)]
    assert len(hits) == 1, (path, head_prefix, len(hits))
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
    append_seg(MAIN, '| 🆕 **`A1989`**', A1989_SEG)   # ⚠️ A1986~A1998 全在 MAIN（§29·b）
    append_seg(TASKS, '| 🆕 **`A1690`**', A1690_SEG)
    append_seg(MAIN, '| 🆕 **`A1969`**', A1969_SEG)
    # A1998 紧随 A1997
    b = io.open(MAIN, 'rb').read()
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1997`**')]
    assert len(hits) == 1
    j = hits[0]
    while not lines[j].rstrip().endswith(' |'):
        j += 1
    lines[j + 1:j + 1] = [A1998]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A1998')
    # 交接 §三 坑
    hb = io.open(HANDOFF, 'rb').read()
    ht = hb.decode('utf-8')
    assert ht.count('8. ⚠️ **一个代理包办多件事时') == 1
    ht = ht.replace('8. ⚠️ **一个代理包办多件事时', HANDOFF_PIT + '\n8. ⚠️ **一个代理包办多件事时')
    io.open(HANDOFF, 'wb').write(ht.encode('utf-8'))
    print('  +交接 §三 坑')


if __name__ == '__main__':
    main()
