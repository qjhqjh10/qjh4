# F4 · `BattleScene.Run` 剩下 7 条红（5273 / 5280）

> 执行代理 `F4` · 2026-10-25 · 现场日志 `d:/4/_tmp_view/20261025/battlescene6.log`
> 白名单：`Assets/CardPresentation/Editor/BattleScene.cs`（**唯一写手**；`BattleDriver.cs` 一个字节都没动）
> ⛔ 没跑 Unity（改完自跑不了）· 没动 git · 没写正本
> ✅ 已跑 `TMPDIR=/tmp/wf_f4 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
> ✅ 行尾：`git diff --numstat` = `2422 / 153`（我这一轮 +145 / −8；改前那份 = 2277/145），
>    `BattleScene.cs` 仍是纯 **CRLF（46213 / 46213）**，没被翻。

---

## 一 · A 组（R20 的 5 条「前提扫不到样本」）—— 判 **δ（夹具前提不成立）**

### 1 · 现象
5 条 `（前提）R20①②②b③④：扫得到 … 的样本` 全红（`BattleScene.cs:2216/2244/2264/2273/2283` 改前行号），
而**同组的读数断言**（`R20②` AM69 那两条）是绿的 —— 说明不是判据坏了，是**样本集空了**。

### 2 · 根因（现读，两条独立事实）
| # | 结论 | 出处 |
|---|---|---|
| ① | **全池「`CardDef.Ability != null` 的卡 = 0 张」现在仍成立**。`Ability` 只在 `!FromOriginalPool` 的卡上由 `EffectSpec.Parse` 赋值；池内 1127 张全是原版卡（`FromOriginalPool == true`）⇒ 恒 `null`。另核 `cards_engine.json`（v28，`count = 1127`）：`keywords` 里 `': '` 前那段归一成 `ability` 的条目 = **0**、`desc` 含 `Ability:` = **0**。 | `RuleEngine/Core/CardDef.cs:1288-1300`（`if (!FromOriginalPool)` 那道闸）· `EffectTargets.FriendlyUnit` 的 doc 同一读数 |
| ② | **E8 今天改的东西没有把这一档变可达**：E8 改的是 `CanUseAbilityTarget` / `ResolveAbilityTargetOwner`（候选**所有者**），**不是** `Ability` 字段的来路 ⇒ 判据数据多了，可达**卡**仍是 0 张。 | `RuleEngine/Core/RuleCore.cs:10340-10365` · `RuleEngineTest_EngineE8.E8bAbilityCard`（它自己就是拿**合成卡**验的，注释写明「池内 `Ability:` 0 张」） |

⇒ 这 5 条**不是断言写错**，是**「真卡这一档结构上不存在」**。判 **δ**。

### 3 · 改了什么（`BattleScene.cs` R20 段）
1. `Pick20(...)`（按真卡扫样本）**删掉**，换成：
   - 一条 **（如实记）** 断言：`现池带 Ability: 的卡 = 0` —— 把「为什么只能拿夹具」变成会说话的断言
     （卡池/解析口径一变就红，提醒回来重扫真卡；⛔ 防「夹具验过 = 真卡已覆盖」的误读）。
   - 合成夹具 `Make20(id, enemy, friendly, hasAlt)`：`new CardDef(..., new[]{ "Ability: Damage 1 EnemyUnit" },
     hasAlternativeAbility: hasAlt, activeAbilityPickCriteria: [...], activeAbilityPickCriteriaVersioned: true)`
     —— 照 `RuleEngineTest_EngineE8.E8bAbilityCard` 那套（`keywords` 里那条 `Ability: …` 是 `Ability` 字段**唯一**来路）。
     ⚠️ 36 格**逐格显式填**：`CountSetSpec` 的默认是 `Unknown(-1)`，不填会让四档**静默塌成 Unknown**（典型的断言自证）。
2. 四档改用夹具 `cEnemy20 (1,0)` / `cFriend20 (0,1)` / `cAny20 (1,1)` / `cZero20 (0,0)` / `cUnknown20 (1,0)+GateUnknown`。
3. `probe20` 从 4 格扩到 6 格：`{opened, pickSide, cmdSide, warnDelta(整趟), warnPerAsk(再问两次), sideStable}`。
   - `warnPerAsk` 钉死「**每问一次 `CommandTargetSide` 就 +1** ⇒ 再问两次 = **恰好 +2**」
     （`NoteAbilitySideGap` 先把计数 +1、再查 `_abilitySideGapNoted` 那道只压 `LogWarning` 的闩）。
     这一条**顶替**原来那条 `r2[3] >= 1`「再问一次仍然 +1」—— 那条对「每问都 +1」和「只 +1 一次」都成立，太松。
   - `sideStable`：三次问出来的侧必须同一个（防「问着问着换了一支」）。

### 4 · 断言强度（对照原文，**没降**）
| 原 | 现 |
|---|---|
| 5 条（前提）扫样本 —— **永远扫不到 = 没验** | 换成 5 条**真跑**：`(1,0)⇒1/静默` · `(0,1)⇒2/退回敌方/出声` · `(1,1)⇒3` · `(0,0)⇒0` · `判不出支⇒0` |
| 每条只断 `pickSide`（后来补了 `cmdSide/warn`） | 每条断 `opened && pickSide && cmdSide==1-MySeat && 出声`（+ 三次读数一致） |
| `r[3] == 1`（**从未被跑到过**） | 出声：非敌方档 `>= 1`（`CommitCommand` 内部会问多次）**＋** 计数口径 `再问两次 == 2`（确定式） |
| AM69（真卡）② 保留 | 保留、继续绿（它断的是「`Ability == null` ⇒ `CommandTargetSide` 提前回 -1」这条**如实记**，与合成夹具互补） |

⚠️ **顺手发现（值得记一笔）**：`AbilityPickSideForTest` 只要求 `_selectedSlot >= 0`（**不**看 `Ability`），
而生产口 `CommandTargetSide()` 有一道 `u.Ability == null || !NeedsPick(...) ⇒ -1`。
⇒ **同一组断言的两半前提不同**：读数那一半真卡可达（AM69），「退回 + 出声」那一半必须 `Ability != null`。
原夹具把两半的前提写成一个（`Ability != null`），所以四档一起空。以后再加这类「读数口 / 生产口」对照断言，别用一个前提盖两半。

---

## 二 · B 组（D3① 两条，`DA22` 相关）—— 判 **α（期望值的前提错）＋ 一次 γ 的暴露**；**实现侧无缺陷**

### 1 · 现象
```
✗ ★ D3①：DA22 在别人（我方载具）攻击时播自己那条 unit_attack_anim（2 → 2）
✗ ★ D3①：监听者族用的是它自己那条 trigger 事件（…）      ← 同一次读数，`LastUnitSkillUsedAttackEventForTest` 停在上一段（true）
```

### 2 · 根因（日志里那条 `[ActionQueue]` 警告就是答案）
`battlescene6.log:1337xx`（`BT` 段上下文）：
```
[ActionQueue] **不触发**「Techmarine·listen:attack」—— ability 级 actingCardCriteria **不满足**
（卡「Techmarine」· trigger=UnitAttack）。 这一档是「**ability 定位不唯一**」：
这张卡有 2 条 trigger = 50 (UnitAttack) 的 ability ⇒ ⛔ 不猜是第几条
```
两条独立事实叠在一起：
| # | 事实 | 出处 |
|---|---|---|
| ① | `DA22` 的 `cardAbilities` 有 **2 条 `trigger = 50`**（`cards_engine.json` v28 的 `abilityTargets` **两行**：`ta=20` / `ta=80`；`abilityTriggers` 是去重集合所以看着只有一行 —— **别拿那一列判歧义**）⇒ `UniqueAbilityIndexForTrigger("UnitAttack")` 判「不唯一」⇒ `EnqueueAbility` 按现口径 **`return false`（不触发）**。 | `Core/CardDef.cs:2555-2590` · `Core/ActionPump.cs:319-333`（`ambiguous ⇒ false`）· `资料/普查产出_1025/J5_判据_48行与DA22.md` §二 F4 / §七 |
| ② | `E8-2`（同日）把 `EmitListenerTrigger` 挪到那两道能力级闸**之后**、只在 `queued == true` 时发 ⇒ 被闸拒时**连 `EvtKind.Trigger` 一起不发** ⇒ `PlaySignal` 的 `canonical == UnitAttack` 那一支**根本没被走到** ⇒ 计数恒不涨、`Last*` 保持上一段的值。 | `Core/EffectResolver.cs:9480-9495`（`if (queued) EmitListenerTrigger(…)`）· `Core/ActionPump.cs` 两处闸 |

**②本身是对的**（第一权威 `RawCardScript__TriggerAbilities.c:108 / :207`：先 `CanTriggerAbility`（两道 criteria 闸）
⇒ 过了才 `TriggerAbility` → `:60 PlayTriggerTraitAnim`）⇒ 错的不是次序，是**「拿一张今天结构上不触发的卡去验『触发之后怎么演』」这层夹具前提**。
① 的引擎偏差**今天修不了**（`RuleEngine/**` 在 F4 黑名单，且它的口径裁定挂在 `J5`：`歧义 ⇒ 不触发` vs `OR 放行`）。

**第二条（`abilityTarget` 退回 self）已按反编译核过**：`BattleManager__PlayTriggerTraitAnim.c:29-32` 把
`BattleAction___ctor(&local, 0x44, isUnit, param_2 /*监听者自己*/, …, 0)` 建在一个 `FUN_180482360(&local,0,0x100)`
**清零过**的结构上 ⇒ **事件不带任何目标** ⇒ 原版这一拍的目标就是「它自己」。
⇒ 我们「没有目标 ⇒ `abilityTarget` 退回 self 并出声」**与原版一致**，这条断言的方向没错，只是**载体换了卡**。

### 3 · 改了什么（`BattleScene.cs` D3 段）
1. **正例换卡**：`DA22 Techmarine` → **`UM83 Valtus`**（「When a friendly unit attacks, deal 3 damage to the
   target of the attack」）。选它的三条硬理由（都现读过）：
   - `abilityTargets` 里 `trigger = 50` **只有 1 行** ⇒ 不歧义 ⇒ E3 那道 acting 闸**会真的过**
     （acting criteria = `friendlyUnits 1 / enemyUnits 0 / includeHero 1 / includeMinions 1` ⇒ 我方载具满足）；
   - 逐卡表有 **authoritative 的 `attack_melee`** ⇒ `EnsureLoaded` 把它搬进 `UnitAttack` 桶（`VfxMap.cs:1183-1194`）
     ⇒ `CardAnimClips.Resolve` 给 **1 拍**；
   - 代码里本来就把 `UM83` 当「别的卡在监听」的样板（`BattleDriver.cs:21352-21360` 的注释点名了它）。
2. **`DA22` 仍留在棋盘上**（③ 要用它那 2 条 authoritative `attack_melee` 当负例），并**明写**它这一下**一拍没起**：
   新加一条 **（如实记）** 断言 `AnimOfForTest(DA22 具) == null`，文案里写清原因与「`J5` 裁定落地后这条会红、
   届时应把它的 2 拍并进 ① 的期望（⛔ 别悄悄删）」。
3. **监听者「一拍照都没起」的读法换到具上**：新加 `AnimOfForTest(UM83 具) != null` ——
   `CardAnimOf` 全仓**只有一个调用点**（`PlayOneUnitAnim`）⇒ 有控制器 = **那一具**真起过拍。
   只看 `UnitSkillAnimsScheduled` 总量分不清「监听者播了」还是「攻方/被打者又排了一拍」（`hit` 桶进同一个计数）。
4. **`UM83` 必须撤走再进 ③**：它监听的是「**任意**友方单位攻击」⇒ `DA22` 自己攻击也算，留着会让 ③ 的「不涨」变成「涨 1」。
   撤走用 `Board[slot] = null; RefreshAll()`（照 R19 那处既有写法；`FreeSlot` 摆的是**最外一格**，撤掉后棋盘仍连续 ⇒ 不会触发 `[BoardSlots]` 告警）。
5. 新增 4 条**（前提）**：`UM83` 有 `Attack` 监听器且**有正文 op** · `trigger = 50` **不歧义**（`UniqueAbilityIndexForTrigger`）·
   有 `unit_attack_anim` 行（`CardAnimClips.Resolve` 给 ≥1 拍，**生产播放就是这一个调用**）· 视图建出来了。
6. ③ 的注释补一条**如实记**：它今天**同时**被两个理由撑着 —— (a) 真正要验的 acting criteria（它自己是 Infantry）；
   (b) 上面的歧义 ⇒ 根本不触发。**防「把已删的无条件给攻方播加回来」这一层鉴别力仍然成立**（那条路与 ability 闸无关，
   `DA22` 有 2 条 authoritative `attack_melee` ⇒ 加回来必涨 2），但 ⛔ 别读成「acting criteria 已验过」。

### 4 · 断言强度（对照原文，**没降**）
| 原 | 现 |
|---|---|
| ① 计数涨（1 条） | 计数涨 **＋ 钉到 `UM83` 那一具上**（新，更强） |
| ① 载体 = 自己那条 trigger 事件（1 条） | 不变 |
| ③ 自己攻击不播（1 条） | 不变（+ 如实记注释说明它今天被两个理由撑着） |
| — | **新加** （如实记）`DA22` 今天不触发 ⇒ 不播（带「裁定落地会红」的说明） |

---

## 三 · 逐条结论表

| # | 断言 | 判类 | 根因（文件:行号） | 改了什么 | 需不需要复跑 |
|---|---|---|---|---|---|
| 1 | （前提）R20① 扫「只敌方」样本 | **δ** | `CardDef.cs:1288-1300`（`Ability` 只对非原版卡赋值）⇒ 全池 0 张 | 换合成夹具；新增「现池 `Ability` = 0」如实记 | ✅ 需 `BattleScene.Run` |
| 2 | （前提）R20②b 扫「友方档且有 Ability」 | **δ** | 同上（`AM69` 的 `Ability` 是 null） | 同上 | ✅ |
| 3 | （前提）R20③ `(1,1)` 样本 | **δ** | 同上 | 同上 | ✅ |
| 4 | （前提）R20③ `(0,0)` 样本 | **δ** | 同上 | 同上 | ✅ |
| 5 | （前提）R20④「判不出支」样本 | **δ** | 同上 | 同上 | ✅ |
| 6 | ★ D3① `DA22` 播自己的 `unit_attack_anim`（2→2） | **α（前提错）+ γ 暴露** | `ActionPump.cs:319-333`（歧义⇒不触发；`DA22` 有 2 条 trigger 50）＋ `EffectResolver.cs:9480-9495`（E8-2：闸后不发 trigger 事件） | 正例换 `UM83`；`DA22` 保留当 ③ 负例并明写它不播 | ✅ |
| 7 | ★ D3①「用它自己那条 trigger 事件」 | **α**（读数被 #6 连带污染） | 同 #6；载体判据本身与反编译一致（`PlayTriggerTraitAnim.c:29-32` 目标清零） | 随 #6 一起（`UM83` 走同一支） | ✅ |

**实现侧（β）一条都没有** —— 7 条里 5 条是夹具前提（δ）、2 条是期望值的前提（α，其中一条由 γ 的 E8-2 变更暴露）。
`BattleDriver.cs` **没动**，`RuleEngine/**` 没动。

---

## 四 · 顺手发现 / 建议记账（⛔ 我一个字都没改这些）

1. 🔴 **`DA22` 的歧义不只是一个引擎小缺口 —— 它今天还在吃「表现层覆盖率」**：
   因为 `E8-2` 把 trigger 事件压在能力闸之后（**正确**），凡是「ability 定位不唯一」的卡，它的
   `unit_attack_anim` / 触发动画**整体不播**。今天只有 `DA22` 一张（`J5` §七），但它是**唯一一张有 2 条 trigger 50 的卡**，
   而它恰好是监听者族的样板 ⇒ 建议主对话尽快在 `J5` §二 F4 的甲/乙里裁定（甲 = OR 放行，能一次解掉「不触发 + 不播」两件）。
2. **`abilityTriggers` 是去重集合，不能用来数「有几条 ability」** —— `DA22` 那一列是 `[50]` 而真身有 2 条。
   判歧义只能看 `abilityTargets`（下标即 ability）。这条今天让原来的 R20 夹具与 D3 正例都踩了同一个坑。
3. **`AbilityPickSideForTest`（读数口）不要求 `Ability != null`，`CommandTargetSide()`（生产口）要求** ——
   同一族断言的两半前提不同；以后写这类对照断言要**分开给前提**（见 §一·4）。
4. **`VfxMap` 的 `unit_attack_anim` 桶是【运行时搬来的】**：`card_vfx_by_card.json` 里没有这个 evt，
   是 `EnsureLoaded` 把 `authoritative` 的 `attack_melee` **搬**进去的（`VfxMap.cs:1183-1194`）。
   查「某张卡有没有这一拍」时，**要么读 `CardAnimTime`（`tn == "UnitAttack"`），要么调 `CardAnimClips.Resolve`**；
   直接 grep json 的 `unit_attack_anim` 会**一张都搜不到**（这次差点因此把 `UM83` 判成「没拍」）。

---

## 五 · 交付状态 / 还欠什么

- ✅ `BattleScene.cs` 已改完；**唯一改动文件**。
- ✅ `typecheck` 绿（0 / 0）· 行尾 CRLF 未翻 · `git diff --numstat` = `2422 / 153`（本轮 +145 / −8）。
- ⛔ **没跑 Unity**（红线①）⇒ **需主对话复跑 `BattleScene.Run`** 核这 7 条 + 看有没有把别处带红。
  ⚠️ 复跑时重点看三处**我无法自验**的：
  ① 合成夹具能不能真开出技能选择器（`Place20` → `SimulateOpenCommand`）—— 若 `r[0] == 0` 会红在「（前提）」那条上；
  ② `UM83` 那条 `(前提) 不歧义 / 有拍` 两条 —— 若红，说明我对 `abilityTargets` / 逐卡表的口径判错了（不是实现问题）；
  ③ 新增的 **（如实记）`DA22` 一拍没起** —— 若红，说明歧义口径已经改了（好事），按它的文案把 2 拍并进 ①。
- 📌 若 `r[4] == 2`（「再问两次恰好 +2」）红了：说明 `CommitCommand`/`HighlightTargets` 里某处会在两次问之间**不**调
  `CommandTargetSide` 或调了别的口 —— 那时把这一格换成「问 N 次 = +N」的直接量法，**别退回 `>= 1`**。
