# B9 · 执行代理 `E6ENG` —— 广播四件 + 光环残骸（2026-10-27）

> 文件白名单（本波独占）：`RuleEngine/Core/EffectResolver.cs` · `Core/Aura.cs` · `Core/ActionPump.cs`
> ⛔ 未跑 Unity · 未动 git · 未改正本与账主表（只写 `账/_parts/B9_E6ENG.tsv`）
> 判据来源：`判据/T7_引擎三条_判据.md` ① · `判据/B9_剩余C_派活包.md` + `B9_剩余C_施工单.tsv`（627/631/669/690）

---

## ① 结论（一句话一条）

| # | 账 | 结论 |
|---|---|---|
| 1 | T7-① 光环给残骸 | ✅ **已修**：`ApplyAura` 通用载荷委托首行加 `if (t.IsRemnant) return;`（残骸**整条不施加**，候选 ②「也写 `_remnantTraits`」按判据**否掉**）。改前残骸 `Armor` **1** / 改后 **0**，正对照仍 1（UM14 · AM28 两组都量了）。 |
| 2 | 669 `BroadcastWhileInPlay` | ✅ **复核：接线已在位**（`ActionPump.cs:143`，`E7P06` 2026-10-26 落的），本轮**零改动**。残余只有「手牌那半」（池内无样本 ⇒ 今天不可达）与「基线待重钉」（调度台的事）。 |
| 3 | 690 `AlreadyContainsEffect` | 🟡 **改到能给的最深处**：去重通道从**「来源+载荷」字符串**换成 `IsSameHandEffect`（原版 `CardEffect.IsSameEffect` 的逐字段比）。判据要求的「补两维」**做不了**（`enchantmentCardId` / `extrinsicVariable` 在我们的 DTO 上没有格子，而 `CardInstance.cs` / `Enchantment.cs` 都不在本代理白名单）—— 但**这两维经查都是恒等维**（论证见 ③），⇒ **可观察差异 = 0**，剩下的只是「结构上补格子」。 |
| 4 | 627 `BroadcastCardDrawn` + 631 `BroadcastCollectFaith` | ✅ **已修**：627 三跳（预调 / 当前回合方手牌两遍 / 棋盘去待死闸），**含原版那个漏 `!` 的 bug、照抄不修**（注释写死在 `DrawHandHopTakes` 的 doc）。631 两处发信仰的口改成「**先预调主体、再 `preCalled` 排除**」（= 原版 预调 + 循环 `op_Inequality`），棋盘跳同样去待死闸。 |

**验证**：`typecheck` 我三个文件 **0 错**（仅 `CardPresentation/Shell/CardDisplayEffects.cs` 有别人的错）；`ruleprobe check`（解析层）**rc=0 / 0 差异**；`ruleprobe play check` **rc=1（预期）**，指纹 `b671acdb…` → `517be27d…`，**隔离实测证明 100% 归因于本笔**（见 ④）。

---

## ② 证据（判据 + 实读）

### 627 `BroadcastCardDrawn`（🥇 ILSpy `out_cpp2il_dev_ilspy/BattleManagerSupport.cs:704-744`，Ghidra `.c` 交叉）

```
:709  drawnCard.CardDrawn(drawnCard);                       // ① 预调：被抽那张先给自己响一次
:710-717  foreach unitsInPlay: card.CardDrawn(drawnCard)     // ② 棋盘：无 IsInPlay 闸
:718-730  GetCardsInHandRef(IsPlayerTurn()) 逐个，!= drawnCard 才响   // ③ 当前手牌，排除被抽那张
:731-743  又 GetCardsInHandRef(IsPlayerTurn()) 逐个，!= drawnCard 才响 // ④ 仍是当前手牌（漏一个 !）
```
⇒ 计数：**被抽那张 1 次** · **当前手牌其余各 2 次** · **对手手牌 0 次**。
🔴 `:731` 那句**本意应是 `!IsPlayerTurn()`**（其余广播器都是「当前 → 另一方」）—— **原版 bug，照抄不修**。

顺带核实「被抽那张在广播时是否已在手牌里」：`_ResolveDrawCard.MoveNext` case 5 = `RemoveCardFromDeck`（**只从牌库列表移除**，`BattleManager__RemoveCardFromDeck.c` 全文只有 `List.Remove`）→ 广播；而 `PlayerHand._AddDrawnCardToHand_d__37__MoveNext` 的第二阶段（`iVar2 == 1`）已经 `List.Add` + `SetupCardInHand`。⇒ **广播时它已在手牌列表里** ⇒ ③④ 的 `!= drawnCard` 排除是**有效**的（也正是「被抽那张只响 1 次」的来源）。

### 631 `BroadcastCollectFaith`（`decomp_full/BattleManagerSupport__BroadcastCollectFaith.c` 全文）
```
:22  CardScript__ReactToCollectFaith(unitCollected, unitCollected)   // 预调自己
:29-38 循环：op_Inequality(u, unitCollected) 才 ReactToCollectFaith(u, unitCollected)
全文 0 处 GetCardsInHandRef；循环体**没有** IsInPlay。
```
`CardScript__ReactToCollectFaith.c` → `RawCardScript__OnTrigger(0x299 = 665, …)`（actingCard = targetCard = 收集者）。
`BattleManager__BroadcastCollectedFaith.c` 全文 2 行 = 纯转调（已有账 205 覆盖）。
兄弟 `BroadcastCollectQuestPoints.c:29-38` **逐句同形**（同样：预调 + 循环只判 `op_Inequality`、**无 IsInPlay**）。

### 690 `AlreadyContainsEffect` / `IsSameEffect`
- `CardScript__AlreadyContainsEffect.c:16-40`：`effect.source == null ⇒ false`；否则遍历 `+0x108`（`activeEffects`）逐个 `CardEffect.IsSameEffect`；命中 true / 遍历完 false。
- `CardEffect__IsSameEffect.c` 比的 19 组字段（`dump.cs` 的 `CardEffect` 字段表逐格对位）：`buffType(+0x38)` · `enchantingCard(+0x18)`（+ `heroCheck` 那支看 `cardType==10`）· **`enchantmentCardId(+0xa4)`** · `extrinsic(+0x30)` · **`extrinsicVariable(+0x31)`** · `untilEndOfTurn/+0x32` · `untilPlayerTurnStart/+0x33` · `untilEnemyTurnStart/+0x34` · `untilFollowingPlayerTurnStart/+0x35` · `untilNextAttack/+0x36` · `meleeAttackChange/+0x48` · `meleeAttackValueOptions/+0x4c` · `rangeAttackChange/+0x50` · `rangeAttackValueOptions/+0x54` · `healthChange/+0x60` · `healthValueOptions/+0x64` · `costChange/+0x68` · `costValueOptions/+0x6c` · `randomValueChange/+0x70`。
  ⚠️ **`armourChange(+0x58)` / `armourValueOptions(+0x5c)` / `trait(+0x3c)` / `traitValue(+0x40)` / `newRace(+0x44)` 原版【不】比** —— 照抄，别「补全」。
- 调用点（`grep AlreadyContainsEffect decomp_full/*.c`）：`AbilityLogic__PlayAbility.c:1229` / `:1364`（`extrinsic` 那一支才查）· `PlayerHand__CheckEffectsOnNewCard.c:37`（`heroCheck = 1`）· `PlayerHand__SetupCardInHand.c:65`。**`CardScript.AddEffect` 自己【不】查** ⇒ 去重只在调用方。
- 我们的落点：`AttachHandEffect` 的 `dedupSamePayload`（唯一调用方 = `AttachEffectToHandInstances`，即 `give … in your hand` 那条路）。

### T7-① 光环 / 残骸
判据全文见 `判据/T7_引擎三条_判据.md` ①（两层闸：目标池 `includeRemnants==0` 52/53 行 + 投递层 `CardScript__AddEffect.c:159-170` 的残骸门，且**那道门只在 `if (extrinsic)` 块内**，`.c:91-197`）。本轮只落「通用载荷分支整条跳过残骸」这一行；`RemnantStay` 支（`SAU36`）**一字未动**。

---

## ③ 没查清 / 做不到（如实）

1. **669 的手牌那半（未实现）**：原版 `RawCardScript__UpdateWhileInPlay.c` = `TriggerAbilities(0x14)` + `(0xf0)`，**无 `IsInPlay` 闸**，广播器对双方手牌也逐张调它。我们这一侧的等价物是**全场重算**（`Auras.RefreshInPlayNumericAndKeywords`），不是「逐手牌卡触发它自己的 20/240 ability」—— 后者是一条**新通道**，且池内没有「手牌卡自己的 WhileInPlay ability」样本（施工单同口径）⇒ **不发明**，留账。
2. **690 的两维补不了（但确认是恒等维）**：
   - `enchantmentCardId`：原版写点 `AbilityLogic__PlayAbility.c:1222` `*(undefined4 *)(uVar23 + 0xa4) = *(undefined4 *)(param_3 + 0x4c)`（源 = `AbilityData.enchantmentCardId`）。我们的附魔族**根本不写 `HandEffects`** —— `Enchantment.cs:46-49` 白纸黑字「附魔的 `CardEffect` **绝不放进** `HandEffects`」（手牌那族「打出即摘」、附魔「跟着牌在场不摘」，语义相反）⇒ 两条手牌效果**不可能**带不同的 `enchantmentCardId`。
   - `extrinsicVariable`：我们从未产出（`HandEffect.Extrinsic` 是唯一那个位、且**今天零生产者**）。
   ⇒ 要真补格子，得改 `CardInstance.HandEffect`（`CardInstance.cs`）+ `Enchantment.Attach`（`Enchantment.cs`）—— **两份都不在本代理白名单**，交调度台分流。
3. **`OnTrapDrawn`（trigger 250）未实现**：`CardScript__CardDrawn.c:23` 在「自己 == 被抽那张 ∧ 是陷阱卡 ∧ `+0x54`」时走 `OnTrapDrawn` → `OnTrigger(0xfa = 250)`，**不是** 260。我们只有 260 那条 ⇒ 被抽的陷阱卡在原版拿到的是 **250**、我们给的是 260（或什么都不给）。建议**单列一条新账**（`WhenEventKind` 里现在没有任何 trap-drawn 词形）。
4. **631 的「改前对手手牌会误响」未复现**：施工单 627 行写「改前手牌两跳 = 当前→对手 ⇒ 对手手牌会误响」，我的夹具实测**改前对手手牌也是 0 条**（`_tmp_view/E6ENG_probe`，BL6 放在 P1 手牌、P0 抽牌）。**原因未查清**（`WhenEvents.Matches` 对 `draw card` 没有极性闸，理论上该响）。⚠️ 这条**不影响**本次修改的正确性（改后两边读数都与原版一致）。
5. **631 的次序验证缺夹具**：原版「主体先响」是**次序**性质的，`play all dump` 的列看不见次序，`preset hand=self,event=gainfaith` 量的是手牌监听 ⇒ **没有断言**。按判据「新夹具：一张 `When you gain Faith` 监听卡留手牌 + 再 +1 信仰 ⇒ 原版 0 响应」那条**已由 `boardAndPersistentOnly` 覆盖**（`B8_AE` 已做），但「收集者自己**先于**棋盘其它单位」这一句仍**无夹具**。
6. **`play check` ①-b 那 13 张的下游涟漪未逐张追到底**：每一张的差异**都始于** `FIX_WAY`（= 会留残骸的 Waystone 夹具，`PlayProbe.FixtureWaystoneTroop`）的 `armour1` 消失，之后 `☠ 伤害累计` / `steps` 分支跟着变（伤害多 1 ⇒ 存活/死亡分支可能翻）。**机制链没逐张读完**，只确认了起点。

---

## ④ 改动清单（精确到行，供 `--fix` 对账）

### `Core/Aura.cs`（**行数中性**：1019 → 1019 行，纯 LF）
| 位置 | 改动 |
|---|---|
| `:883` | `if (!a.Filter.Matches(t)) return;` → `if (t.IsRemnant) return; if (!a.Filter.Matches(t)) return;` + 同行长注释（判据 T7-①） |

### `Core/EffectResolver.cs`（16420 → 16489 行，**全 CRLF 未翻**；⚠️ 插入点在下面 5 处 + 类尾）
| 插入点（新行号） | 增量 | 内容 |
|---|---|---|
| `:5499-5505` | **+1** | 去重通道换成 `IsSameHandEffect`（候选 `HandEffect` + `HandExpireOf(duration)`） |
| `:10671-10677` | **+1** | `BroadcastHandWhen` 的 `drawMode` 三趟（多一行注释） |
| `:12815-12820` | **+3** | 信仰支 1：`PreCallBoardListener` + `preCalled: wl`（+2 行注释） |
| `:12884-12887` | **+2** | 信仰支 2：`PreCallBoardListener` + `preCalled: unit` |
| `:16209-16213` | **+2** | `BoardJumpIncludesPendingDeath` 加 `Draw` / `GainFaith` / `GainQuest` |
| `:16448-16489` | **+60** | **类尾**新增 `DrawHandHopTakes` / `DrawnNow` / `HandExpireOf`（类尾 ⇒ **不造成任何落点漂移**） |

⇒ **落点漂移**（只对「指向这 5 处之后」的 A 行生效）：`:5499` 之后 **+1** · `:10671` 之后 **+2** · `:12815` 之后 **+5** · `:12884` 之后 **+7** · `:16209` 之后 **+9**。
行数中性不了的只有这 9 行（判据文档要求的「逐句同形 + 判据注释」两者不能兼得时优先可读性）。⛔ **我没有改任何账主表 / 基线常量**。

### `Core/ActionPump.cs`
**零改动**（669 的接线 `E7P06` 已落，`:143` 在位）。

### 新增（非仓库产物）
`d:/4/_tmp_view/E6ENG_probe/`（临时读数口：`e6probe.csproj` + `Program.cs` + `Stub.cs` + `Core/` 副本）——**一次性用**，可删。

---

## ⑤ 指纹归因（`ruleprobe play check`）

| 态 | 指纹 | 差异 |
|---|---|---|
| 基线（钉住） | `b671acdb0b60d86b17ffe3a9f9e69c25` | —— |
| **把我这三处改动临时回退**（其余并发改动全开） | `b671acdb0b60d86b17ffe3a9f9e69c25` | **逐字节相同 · 差分 0 行** |
| **带我的改动** | `517be27dd33773e07861d5762f118241` | ①-a **1 张** · ①-b **13 张** |

**⇒ 结论：`play check` 的全部差异 100% 来自本代理的这两笔行为改动**（隔离实验在 2026-10-27 07:0x 跑，前后各一次；期间别人的改动全开且当时全部行为中性）。

逐条归因：

- **①-a 1 张 = `BL6 Rubric Marine`**（19 列对拍）：`darkpact1→darkpact2` · `camouflage1→camouflage2` · `markofchaos1→markofchaos2` · 血 `4→6` · 手牌 `BL6*1→BL6*2`。
  ⇒ **627 的直接后果**：`When you draw a card, gain a Dark Pact of Fate` 现在按原版响 **2 遍**（原版那两个手牌跳都调它）⇒ 两个 Dark Pact。**这是照抄原版 bug 的必然结果**，⛔ 别当成回归去「修」。
- **①-b 13 张** = `AM38 · BL6 · GOF2 · GOF11 · GSC4 · SAU30 · SAU67 · SW25 · TAU19 · TAU28 · UM104 · UM107 · UM18`：**每一张的差异起点都是夹具残骸 `FIX_WAY` 上的 `… {armour1, waystone1} → {waystone1}`**（Armour 1 消失）+ 其后的 `☠ 伤害累计` / `steps` 分支变化。
  ⇒ **T7-① 的直接后果**：电池把每张卡摆到残骸旁边，原来光环的 `Armour 1` 落到残骸身上（残骸被算成更耐打），现在不落了；伤害累计随之变（残骸实伤本来就不减甲，但**预览/AI/存活判定会变**），于是 `survived/died` 分支与后续步骤跟着变。
  ⚠️ `steps` 里 `softAtk=0@6/survived → 0@3/survived` 这种「步号变了」是**分支翻**的下游表现，不是新的结算路径。

`ruleprobe check`（解析层）：**rc=0 · `解析差异 0 行` · `哨兵合计：失败 0 条`** ⇒ 我的改动**不碰解析**。

🔴 **收尾复测（2026-10-27 07:11）—— 指纹已不是 `517be27d…`，而是 `fc8ae029…`，且 ①-a「1128 张卡变了」/ ①-b「586 张卡变了」（全池）**：
**那不是本笔**。判据有两条，都很硬：
① 我的两处改动**结构上只能影响 14 张**（①-a 1 张 = `BL6`；①-b 13 张 = 被残骸光环波及的那一队），**不可能让 1128 张同时变**；
② 上表的**隔离实验**（把我的改动回退、其余全开）当时读数 = 基线**逐字节相同**、0 差异。
⇒ 全池漂移来自**另一个并发代理**在 07:06～07:11 之间落进 `Core/` 的行为改动（本波多写手并行）。
⇒ **给调度台的两句话**：a) 重钉基线前先重跑一次隔离实验，别把全池差异算到本笔头上；
b) 本笔的可归因证据是**隔离差分**（14 张）与**自建夹具的两态读数**（⑤⑥），**不是**当前那条 `fc8ae029…`。

---

## ⑥ 改前 / 改后读数（光环残骸 + 抽牌广播）

**读数口**：`d:/4/_tmp_view/E6ENG_probe`（自建，`Core/` 是**副本**，两态各编一次；⛔ 没有为取数改仓库代码）。

### 光环 / 残骸（`Auras.Recompose` 之后读 `UnitState.Armor`；slot 3 = 光环源，slot 2 = 合成残骸，slot 4 = 非残骸正对照）

| 夹具 | 单位 | 改前 | 改后 |
|---|---|---|---|
| `UM14 Honour Guard`（`Adjacent units have Armour 1`）+ `BL1 Chaos Spawn` | **残骸** `Armor` | **1** | **0** |
| 同上 | 正对照（非残骸）`Armor` | 1 | **1**（不变 ✔） |
| `AM28 Chimera`（`Friendly Infantry troops have Armour 1`）+ `UM1 Scout` | **残骸** `Armor` | **1** | **0** |
| 同上 | 正对照（非残骸）`Armor` | 1 | **1**（不变 ✔） |

⇒ 与判据完全吻合：**只有残骸变了、非残骸一字未动**。（正对照不变这一点是关键：它排除「我把整条光环关掉了」。）

### 抽牌广播（`RuleCore.Draw(ctx, 0)` 之后数 `EvtKind.Trigger` 且 `Effect == "listener:draw"` 的事件条数）

| 夹具 | 改前 | 改后 | 原版 |
|---|---|---|---|
| `BL6` 在**当前回合方（P0）手牌** | **1 条** | **2 条** | **2 条** ✔ |
| `BL6` 在**对手（P1）手牌** | 0 条 | **0 条** | **0 条** ✔ |

（改前对手手牌为什么也是 0 —— 见 ③·4，未查清；两态一致 ⇒ 无回归。）

---

## ⑦ 顺手发现（交给调度台分流，⛔ 本代理未改）

1. **`RuleCore.cs`（`E5ENG` 的文件）还有第三处 `GainFaith` 广播是**老形状**：`:1649` 祈祷附魔到期那一支 —— `BroadcastWhen(ctx, GainFaith, p, u.Card, u, boardAndPersistentOnly: true)`，**没有预调、没有 `preCalled`**。631 的三处口要同形才叫补齐（判据 = `BroadcastCollectFaith.c:22` + `:29-38` 对**任何**收集者都成立）。
2. **`GainQuest` 的同类缺口（同一份 `.c` 形状）**：`BroadcastCollectQuestPoints.c:22` 也有预调、`:29-38` 也有 `op_Inequality(u, subject)` 排除；我们的调用点（`EffectResolver` 的任务点支）**只做了 `boardAndPersistentOnly`**，**没有预调/排除**。本轮我只把 `GainQuest` 并进待死闸那一族（同一份证据），**预调那半没做**（不在本代理派活清单里）。
3. **`OnTrapDrawn`（trigger 250）/ `OnTrapResolved`** —— 见 ③·3，我们一条都没有；`WhenEventKind` 里也没有对应词形。
4. **`BroadcastCardDrawn` 的第二遍还有一个「取错表」的次生 bug**：ILSpy `:739` 用的还是**第一份** `cardsInHandRef.get_Item(num5)`（第二份 fetch 只用来量长度/判 `!=`）。两份内容相同时**不可观察**，所以我**没有**在代码里造这个形状（我们是同一次遍历跑两遍）—— 如实记着，别以后拿它当「我们做错了」。
5. **`AttachHandEffect` 的 `dedupSamePayload` 原本是「来源+载荷」字符串比** —— 这正是 `W4` 已判死的那条通道，却在 2026-10-25 的 `IsSameHandEffect` 整改里**漏掉了**（那次只改了 `AttachHandEffectCopy`）。本轮一并换上 ⇒ 「同来源同载荷、不同施放者 / 不同到期档」不再被压成一条（原版是 19 维逐字段比）。**风险提示**：这条会让「本来该叠两层却没叠」的情况**变多**（更接近原版），`play check` 目前无新增差异 ⇒ 池内暂无触发者。
6. **`Enchantment.cs:46-49` 是一句很有用的硬边界**（「附魔的 `CardEffect` 绝不放进 `HandEffects`」）—— 它把「`enchantmentCardId` 在我们这边恒等」这句话从「我们没做」升级成「**结构上不可能不同**」。以后有人想往 `HandEffects` 塞附魔类效果，先读那段。

---

# 第二批（2026-10-27 晚 · 调度台追加指派）

> 本波白名单扩到 `RuleEngine/Core/RuleCore.cs`（`EffectResolver.cs` / `Aura.cs` / `ActionPump.cs` 照旧）。
> 交付：**两件接线补齐（改代码）+ 一件新账判据（⛔ 不写代码，等调度台立账）**。
> 账补丁：`账/_parts/B9_E6ENG2.tsv`（11 列 × 2 行 = 631 / 632）。

## ⑧ 631 的第三处 `GainFaith` 口（`RuleCore.cs`）

**位置**：`RuleCore.cs` 的「祈祷附魔到期 +1 信仰」那一支（原报的 `:1649`，现读数 `:1647-1650`）。
**改法**（**行数中性** 3 → 3 行）：

```csharp
if (u.Card != null) PreCallBoardListener(ctx, u, WhenEventKind.GainFaith, p, u.Card, u, null, null, -1, allowPendingDeath: true);
if (u.Card != null) BroadcastWhen(ctx, WhenEventKind.GainFaith, p, u.Card, u, preCalled: u, boardAndPersistentOnly: true);
// ↑ 账 631 的第三处口：预调 + preCalled 必须成对 —— 判据 BroadcastCollectFaith.c:22 / :29-38
```

**判据（与另两处同一份）**：`BattleManagerSupport__BroadcastCollectFaith.c:22` 先 `ReactToCollectFaith(unitCollected, unitCollected)`、`:29-38` 循环用 `op_Inequality(u, unitCollected)` 把它排除。**任何**收集者都走这一份 `.c` ⇒ 三处口必须同形，不能只改两处。
**为什么合成一行 `if` 而没有加大括号**：`RuleCore.cs` 有 ~40 条账按「文件:行号」落点在本行**之后**，多一行就是 40 条漂移；`EffectResolver.cs`（307 条）同理。**本批两个文件都做到了行数中性**（`RuleCore.cs` 15012 → 15012 · `EffectResolver.cs` 16489 → 16489）。

## ⑨ 632 `GainQuest` 的预调那半（`EffectResolver.cs`）

**位置**：`DoFactionResource` 的任务点支（`for (int k = 0; k < n; k++)` 那个循环）。
**改法**（行数中性 3 → 3 行）：循环体改成 **`PreCallBoardListener` + `BroadcastWhen(…, preCalled: subj, boardAndPersistentOnly: true)` 逐点配对**。
**判据**：`BattleManagerSupport__BroadcastCollectQuestPoints.c:22` 预调自己、`:29-38` 循环 `op_Inequality(u, subject)` 排除 —— **与 faith 逐句同形**；调用方 `BattleManager__ProcessManaBuff.c:53-73` 是「**每 1 点任务点**走一遍广播」的循环 ⇒ **逐点配对**（而不是「先全部预调、再全部广播」）不是我挑的，是原版形状。
**顺带**：`BoardJumpIncludesPendingDeath` 上一批就已经把 `GainQuest` 并进去了（同一份 `.c` 循环体只判非 null），本批只是把预调那半补齐。

## ⑩ 验证（第二批）

| 项 | 结果 |
|---|---|
| `typecheck`（`TMPDIR=/tmp/wf_E6ENG`） | **`RuleEngine/` 下 0 错**（`CardPresentation/Battle/BattleDriver.cs` 报 6 条，是别的写手正在改的文件） |
| `ruleprobe check`（解析层） | **rc=0** |
| `ruleprobe play check` 两态 | **带本批两笔 = `79057e33cd3984c36055a11a60e0a83f`**；**临时回退本批两笔 = `79057e33cd3984c36055a11a60e0a83f`** ⇒ **逐字节相同** |
| 归因 | 本批**零可观察变化**（**预期**：预调 + 排除保住「主体只响 1 次」的**计数**，只动**次序**；`play all dump` 的列不记次序）⇒ 不是「没生效」，是**这条改动本来就不可由 dump 观察** |
| 全池 1128 + 586 张都变了 | ⚠️ 与本批**无关**（上一批就已存在，调度台已定位嫌疑 = `E7ENG` 的 `AchievementTally.AddTrigger` 20/240/630 过滤；本批两态对照下它**恒定**，所以不影响本批归因） |

**行尾**：`RuleCore.cs` 纯 LF 15012 行（未翻）· `EffectResolver.cs` 全 CRLF 16489 行（未翻、无裸 CR）。

---

## ⑪ 🆕 新账判据：`ThisCardDrawnAsTrap`（trigger **250**）—— ⛔ 本轮**没有写代码**

> 调度台指令：先查判据，写成「新账 + 判据 + 施工建议」，立账后再派。
> 建议账名：**`BroadcastCardDrawn` 的「被抽的武装陷阱」分支（trigger 250 `ThisCardDrawnAsTrap`）**；建议等级 **P1**（机制缺失、两头数据为 0）。

### (1) 原版做法（逐句，第一权威 = Ghidra `CardScript__CardDrawn.c` + ILSpy `CardScript.cs`）

**接收端 = `CardScript.CardDrawn(this, drawnCard)`，两条路互斥：**

```c
// CardScript__CardDrawn.c:16-31（Ghidra 逐行）
cVar2 = op_Equality(param_2 /*drawnCard*/, param_1 /*this*/);
if (cVar2 == 0) { lVar1 = raw; }                       // ① 不是自己 ⇒ 落到 OnCardDrawn
else {
    cVar2 = EntityScript__get_isTrapCard(this);
    if (cVar2 != 0 && *(char *)(this + 0x54) != 0) {   // ② 是自己 ∧ 是陷阱卡 ∧ armedTrap
        RawCardScript__OnTrapDrawn(raw, owner, this);  //    → trigger 250 后【直接 return】
        return;
    }
    lVar1 = raw; param_2 = this;
}
RawCardScript__OnCardDrawn(raw, owner, this, param_2); // → trigger 260
```

- `EntityScript__get_isTrapCard.c` 全文：`raw.cardType(+0x94) == 0x14 (20 Tactic)` **且** `raw.spellType(+0x90) == 0x46 (70 = SpellType.Trap)`。
- `this + 0x54` = **`EntityScript.armedTrap`**（`dump.cs:21963+` 的字段表：`0x50 usedActiveAbilityTimesPerTurn` / **`0x54 armedTrap`** / `0x55 stunnedAtStartOfTurn` / `0x65 isRemnant` …）。
- `RawCardScript__OnTrapDrawn.c` 全文 = `OnTrigger(0xfa = **250** = AbilityTrigger.ThisCardDrawnAsTrap, owner, this, actingCard = default, targetCard = default)`
  （ILSpy `RawCardScript.cs:1919-1924` 同形：`actingCard` / `targetCard` **都是 `default`**）。
- 对照：`RawCardScript__OnCardDrawn.c` 全文 = `OnTrigger(0x104 = **260** = OtherCardDrawn, owner, this, actingCard = **drawnCard**, targetCard = 0)`。

**上游（谁会走到这一跳）**：`BattleManagerSupport__BroadcastCardDrawn.c` 的**预调那一跳** —— `:25` `CardScript__CardDrawn(drawnCard, drawnCard)`（**自己给自己**）。棋盘跳与两趟手牌跳都传给 `!= drawnCard` 的接收者 ⇒ **永远不会走 250**。ILSpy `BattleManagerSupport.cs:709` 同一句。
⇒ **一次抽牌里，250 最多发 1 次，且只发给「被抽到的、已武装的陷阱卡自己」，并且这一发【取代】它的 260。**

**`armedTrap` 是谁置的**（这是本条最容易做错的一格）：
- **唯一写点** = `CardScript.SetAsActiveTrap()`（ILSpy `CardScript.cs:9023-9031`，体内一句 `base.armedTrap__BackingField = true`；它对 `CustomDebug.Log("SetAsActiveTrap " + cardName)` 也留着）。
- **唯一调用点** = `BattleCardManager.CreateCard(raw, parent, isPlayer, army, **armedTrap**)` 里那句 —— Ghidra `BattleCardManager__CreateCard.c:91-95`：`if (param_6 /*armedTrap*/ == 0) SetEnemySecret(); else SetAsActiveTrap();`
  🔴 **ILSpy 这里把常量丢了**（`object obj = default; if (obj == null) SetEnemySecret() else SetAsActiveTrap()`）—— 正是本仓记过的「ILSpy 把实参常量反编译成 `default`」那类坑，**必须看 `.c`**。
- **武装陷阱进手牌的唯一路** = `PlayerHand.AddActiveTrapToHand(card)`（`_ResolveDrawCard.MoveNext` case 0：`if (drawnCard.+0x54 != 0) AddActiveTrapToHand`，**否则** `AddDrawnCardToHand`；协程体 `PlayerHand._AddActiveTrapToHand_d__38__MoveNext.c:24-31` = 播抽牌动画后 `List.Add(hand)` + `SetupCardInHand`）。
- 同一次抽牌还**并行**发另一条（**与 250 不是一件事**）：`_ResolveDrawCard.MoveNext` case 4/5 里 `if (drawnCard.+0x54 != 0)` ⇒ `BattleManager__BroadcastTrapResolved` + `CemeteryManager__AddDrawTrapAction`（= 账 **217** / 941 / 1273 那一族，**都还是 D**）。
- ⚠️ **未查清**：`armedTrap = true` 的 **C# 调用点找不到**（`grep "armedTrap = true"` / `"armedTrap: true"` 在 ILSpy 全树 = 0；所有 `CreateCard(...)` 调用点在 ILSpy 里都渲染成 `armedTrap = default`）。**大概率同样是常量丢失**，要按 Ghidra 逐个调用点看第 6 实参（`BattleManager__AddNewCardToDeck.c:71` / `AddNewCardToHand.c:65` / `AddInitialCardToDeck.c:32` / `AddCardInPlayToDeck` / `CreatePlayerDeck`）。⇒ **这是本条唯一还缺的证据**。

### (2) 可达性（现读，两次扫描 + 对照值）

| 扫的什么 | 集合 | 结果 |
|---|---|---|
| `cards_truth.json`（1,351 张 SO）`abilities[].trigger == 250` | 监听方 | **0 行 / 0 张** |
| 同上 `spellType == 70`（= `SpellType.Trap`） | 主体方（`isTrapCard` 的必要条件） | **0 张**（`cardType==20` 的也 0 张） |
| `cards_engine.json`（池 1,128）`abilityTriggers` 含 250 | 监听方 | **0 张** |
| 同上 `spellType == 70` | 主体方 | **0 张** |

⚠️ **扫描纪律（本轮的假阳性自查）**：第一次我按 `cardAbilities[].trigger` 扫，得出「250 / 260 / 665 / 695 全是 0」——**那是假阴性**（truth 里那个键叫 **`abilities[].trigger`**，不是 `cardAbilities`）。**对照值重扫**：`260 = 2 行` · `665 = 1 行` · `695 = 3 行` ⇒ 扫描有效，`250 = 0` 才是真结论。

⇒ **今天两端都是 0**（既没有陷阱卡，也没有 250 的 ability）。
🔴 **但这不等于「可以不做」**（铁律 11）：**原版机制确实存在、判据也齐**，只是本地数据里没有样本 —— 属于「机制要做、数据为 0」，⛔ **不能按「原版本身没有」处理**。

### (3) 施工建议（按顺序，缺一不可）

1. **数据/载体那一层（前置）**：`CardInstance` 上没有 `armedTrap`（原版 `EntityScript+0x54`）。要照 `SetAsActiveTrap` 建一个 `ArmedTrap` 布尔 + 一个「创建即武装」的口（原版唯一写点）。⚠️ 「谁在什么条件下传 `armedTrap = true`」**还没查清**（见 (1) 末尾）⇒ **先查这个再动手**，否则会做出一个永远为 false 的字段（= 静默死机制，本工程红线）。
2. **`isTrapCard` 判据**：`Type == tactic(20) && SpellType == 70` —— 两个字段我们**都有**（`CardDef.SpellType`，`-1` = 未知、⛔ 不是 Normal），可以直接写，不需要新数据列。
3. **事件那一跳（可以独立做，且零风险）**：
   - 在 `WhenEventKind` 加一个 kind（建议 `ThisCardDrawnAsTrap = "thiscarddrawnastrap"` —— ⚠️ **它不是卡面词形**，是**结构化 trigger**，所以 `WhenEvent.Parse` 那条路**不该**加分支）；
   - `TriggerNameOfWhenKind` 加 `case` → `"ThisCardDrawnAsTrap"`（名字表 `CardDef.AbilityTriggerNumericNames:1928` **已经有** `{"ThisCardDrawnAsTrap", 250}`，`HasTriggerSource` 的 presence 闸**开箱可用**）；
   - 发点在 `BroadcastWhen` 的 627 `hop 0`（= 被抽那张自己的预调）里分叉：`isTrapCard && ArmedTrap` ⇒ 发 250、**且这一张不发 260**（照抄 `CardScript__CardDrawn.c:16-31` 的「互斥 + 提前 return」）。
   - **效果层（`chosenAbility` 派发）没有判据**：池内 / 全 SO 里 250 的 ability **一条都没有** ⇒ 不知道它挂的是什么 buff / 目标。⇒ 按本仓红线：**发事件 + `LogWarning` 出声「该 ability 的原版形状无数据，未执行」**，⛔ 不许静默、也不许自己发明效果。
4. **验收**：合成夹具（`RuleEngineTest`）—— 造一张 `cardType=20 / spellType=70 / abilities[0].trigger=250` 的卡，摆成 `ArmedTrap = true`，`RuleCore.Draw` 抽到它 ⇒ 断言 ① 收到 **250** 一条 ② **260 零条** ③ 正对照（`ArmedTrap = false` 的同型卡）⇒ 收 **260** 一条、250 零条。`play all dump` 今天**不会**有差异（两端 0 样本）。

### (4) 与本批已做两条的关系

- **627 的 `hop 0`（被抽那张的预调）就是 250 的发点** —— 本批已经把这一跳建起来了（`DrawHandHopTakes` 的 `hop == 0`），所以 250 那条只需在**同一个 hop 里分叉**，不用再改广播结构。
- **账 217 `BroadcastTrapResolved`（D / P0）** 与 250 **不是一件事**：250 = 这张陷阱卡**自己**「被抽到了」的能力；`BroadcastTrapResolved` = 「陷阱触发」的**广播**（外加 `CemeteryManager.AddDrawTrapAction`）。⛔ 别合并。

---

# 第三批（2026-10-27 夜 · 账 703 / 1272：`trap + armed ⇒ 250` 发射分叉）

> 白名单不变（`RuleCore.cs` / `EffectResolver.cs` / `Aura.cs` / `ActionPump.cs`）。
> 数据位（`CardDef.IsTrapCard` / `CardInstance.ArmedTrap`）已由 `E7ENG` 落地，我按「位已存在」接的分叉 **一次编过（typecheck 0 错）**。
> 账补丁：`账/_parts/B9_E6ENG3.tsv`（11 列 × 2 行 = **703 / 1272**；⛔ 两个 `状态` 字段我都没动）。

## ⑫ 落了什么（三处，`EffectResolver.cs`）

| 位置 | 改动 | 行数 |
|---|---|---|
| `BroadcastHandWhen` 的**廉价值预扫** | `any` 条件加 `‖ (drawMode && h[i].ArmedTrap == 1)` | **0**（同行加长；现读数 `:10661`） |
| `BroadcastHandWhen` 的循环体（`var c = …` 之后、`CanListenForEvents` 闸之前） | `if (drawMode && hop == 0 && drawnCard != null && ReferenceEquals(inst, drawnCard) && EmitTrapDrawnInsteadOfCardDrawn(ctx, inst, p)) continue;` | **+1**（现读数 **`:10685`** ⇒ 指向该行之后的 A 行**整体漂 +1**；本批唯一一处） |
| 类尾（`HandExpireOf` 之后） | 新方法 `EmitTrapDrawnInsteadOfCardDrawn`（`doc :16496-16519` · 方法 `:16520`） | **+~45**（类尾 ⇒ 不造成落点漂移） |

**判据**（全文已写在 §⑪，这里只复述落点）：`CardScript__CardDrawn.c:16-31` 两条路**互斥** —— 不是自己 ⇒ 260；**是自己 ∧ `isTrapCard` ∧ `armedTrap(+0x54)`** ⇒ `OnTrapDrawn` → trigger **250 `ThisCardDrawnAsTrap`**（`actingCard`/`targetCard` 都是 `default`）并**提前 return**（这一发**取代**它的 260）。
**⛔ 只在预调那一跳开岔**：`hop 0` 且 `inst == drawnCard`（= 原版 `BroadcastCardDrawn.c:25` 那句 `CardDrawn(drawnCard, drawnCard)`）—— 棋盘跳与两趟手牌跳在判据里就只传 `!= drawnCard`，本批**没有**在别处开岔。
**三态**：`ArmedTrap == -1` ⇒ `Debug.LogWarning` **出声** + 落回 260 保守路（⛔ 未知不当 0）；`ArmedTrap == 1` 但 `IsTrapCard` 不成 `true` ⇒ 也出声（两处判据打架 = 数据坏）。
**⛔ 效果层不做**：池内/全 SO 里 `abilities[].trigger == 250` = **0 行** ⇒ 只发事件 + 出声，**不发明** `chosenAbility` 的效果。⛔ **没有**新建 `WhenEventKind` 词形（250 是结构化 trigger，不是卡面写法；直接走 `EmitCardTrigger`，闸靠已有的 `AbilityTriggerNumericNames`）。

### 🔴 顺带补掉的一个真洞（不补的话本批等于没接）

`BroadcastHandWhen` 开头有一句廉价值预扫：「两边手牌里一张会听的**单位卡**都没有就一个字节都不动」。
而**陷阱卡是 tactic（非单位）**——`CardDef.CanListenForEvents == false` ⇒ 只要场上**没有一家手牌里躺着听 `draw` 的单位卡**，这个函数会**在 pre-scan 就早退**，被抽的武装陷阱**永远走不到 `hop 0`**。
⇒ 预扫条件加了 `‖ (drawMode && h[i].ArmedTrap == 1)`（**行数中性**）。**这一条是本批必须的，不是我顺手加的**。

## ⑬ 验证

| 项 | 结果 |
|---|---|
| `typecheck`（`TMPDIR=/tmp/wf_E6ENG`） | **`RuleEngine/` 0 错 · 运行时错误数 0** |
| `ruleprobe check`（解析层） | **rc=0** |
| `ruleprobe play check` | 指纹 **`79057e33cd3984c36055a11a60e0a83f`** —— **与第二批收尾时同一个值** ⇒ 第三批**零可观察**（结构原因：全池 `spellType == 70` = 0 张 ⇒ 分叉与预扫那两条**都不可能有命中**） |
| 行尾 | `EffectResolver.cs` 全 **CRLF**（未翻、无裸 CR）；`RuleCore.cs` 全 LF（本批未改） |

### 人造夹具读数（`_tmp_view/E6ENG_probe`，两态各编一次）

夹具 = 真卡池 + 一张合成卡 `new CardDef("E6TRAP","E6TRAP","tactic", "When you draw a card, gain 1 Spirit Stone.", "common","Test", 0,0,0,0,null, spellType: <70|0>, abilityTriggers: new[]{250,260}, abilityTriggersVersioned: true)`，再 `AttachCardType(20)`；把它放进 P0 手牌、把 `ctx.DrawnThisResolve` 种成它，然后**走生产路径** `RuleCore.BroadcastWhen(ctx, "draw", 0, trap, null)`。

| 夹具 | 前提硬断言 | `trapDrawn`(250) **改前 → 改后** | `listener:draw`(260 那条路) |
|---|---|---|---|
| **武装陷阱**（cardType 20 + spellType 70） | `ArmedTrap = 1` · `IsTrapCard = True` | **0 条 → 1 条** ✔ | 两态都 **0** |
| 非陷阱对照（同型卡、spellType 0） | `ArmedTrap = 0` · `IsTrapCard = False` | 0 → 0 ✔ | 0 |
| 陷阱卡但 `cardType` 没挂（三态） | `ArmedTrap = -1` · `IsTrapCard = 判不了(卡表 < v34)` | 0 → 0 ✔（保守落回 + 出声） | 0 |

**「改前」是怎么造的**：把探针里那份 `Core/` **副本**的 `EmitTrapDrawnInsteadOfCardDrawn` 首行改成 `return false;`（= 分叉未接），重编重跑；⛔ **没有**为取数改仓库代码。

### 如实标着的三条边界

1. **「260 被取代」在数据上不可单独观察**：陷阱是 tactic ⇒ `CanListenForEvents == false`（本来就不进单位手牌那跳），而 `BroadcastHandTrapWhen` 的 drawMode 又**排除被抽那张** ⇒ 两态 `listener:draw` 都是 0。这一格由**结构**保证（`continue` 那一下 + 两处排除），夹具只能证「250 出现/不出现」。
2. **断言没进 `RuleEngineTest`**：`RuleEngineTest.cs` 是 `E5ENG` 的文件。夹具配方已写进账 703 的 `差异`（真卡造不出陷阱，必须 `new CardDef(...)` + `AttachCardType(20)`；断言顺序 = **先硬断言 `ArmedTrap`/`IsTrapCard`，再断言 250 条数**）。
3. **效果层是空的**（不是我没做，是**判据不存在**）：`chosenAbility` 是什么、打谁 —— 全 SO 里 250 的 ability 0 条 ⇒ 无从照抄。本批按红线「发事件 + 出声、不发明」，等数据出现再按 `AbilityLogic.PlayAbility` 那条链补。

## ⑭ 顺手发现 / 建议

1. **账 1272（`RawCardScript.OnTrapDrawn`）原判 D 的理由已全部过期**（「armedTrap 位没建 / 抽牌路径没接 / 没建 Kind」）—— ① ② 现在都建了，③ 按本批口径**本来就不该建**（结构化 trigger）。**建议调度台改判 A/B**；我按纪律**没动 `状态` 字段**。
2. **`CardInstance.ArmedTrap` 的语义比名字窄**：它今天等价于 `IsTrapCard ? 1 : 0`（`TrapArmedOf` 就是这么算的），原版那两格在数据上其实**同源**。⇒ 将来若原版出现「造出来但未布设」的陷阱（`SetAsActiveTrap` 之外还有别的路），这两格才会分叉；今天判据里那两个条件的冗余是**无害且更贴原版**的（原版也是两个条件并列），⛔ 别因为「冗余」把它删掉。
3. **`BroadcastHandWhen` 的 pre-scan 早退是个系统性风险点**（本批撞到的那个洞）：以后任何「非单位手牌卡的自己那一跳」（如 `CardCreatedInHand(210)`）都会踩同一个坑。建议在 `EmitWhileInPlayPass` 那种「只为表现、无行不发」之外，凡是**新增一条不依赖 `CanListenForEvents` 的手牌跳**，都要同步扩这个 pre-scan（判据：`CanListenForEvents => IsUnit` 就是那个前提）。

---

# 第四批（2026-10-27 深夜 · `BroadcastHandWhen` 早退洞普查 · **只读，未改任何 `.cs`**）

> 产出正本 → **`判据/BroadcastHandWhen早退洞_普查.md`**（清单/可达性/判据都在那份，⛔ 本节不抄第二份 —— 铁律 6）。
> 白名单：只读 `RuleEngine/Core/**` + `d:/2/tools/decomp_full` + 判据/报告。⛔ 未跑 Unity · 未动 git · 未改账主表 · **未改一行代码**。

## ⑮ 结论摘要（六行）

1. **`BroadcastWhen` 的五个手牌侧/非棋盘侧跳里，只有一个按卡型早退** = `BroadcastHandWhen`（`CanListenForEvents => IsUnit`）；
   `BroadcastCostWhen` / `BroadcastPersistentWhen` / `BroadcastHandTrapWhen` 都**不按卡型**。
2. **原版的手牌跳一律不按卡型**（逐句：`BroadcastCardDrawn.c:36-76` · `BroadcastCardPlayed.c:35/44-46/49-58` 循环体只有 `!=`；接收端 `CardDrawn` / `ResolveCardPlayed` 第一道闸只有「有 `rawScript`」，无区域/卡型闸）。
3. **今天被这条早退挡住、原版会响的 = 2 族**：F1 武装陷阱 `250`（**第三批已补**，池内 0）· F2 `ArmedTrap == -1` 的出声路径（池内 0，但预扫只认 `== 1` ⇒ warning 不可达）。
4. **相邻但根因不同的 = 1 族**：F3 非单位卡的**多段** `When …` —— 根因在**注册端** `EffectText.SplitHandTrapWhen` 要求 `segs.Count == 1`（单位卡那条路允许多段）；全池候选 **1 张**（`DA42 Reconnaissance Mission`，而它其实是「in-hand 挂载 + when-played 钩子」，**另一条机制**，⛔ 别拿放宽 `SplitHandTrapWhen` 当它的实现）。
5. **调度台点名的两族查到底了**：`weapon` / `whisper` **在我们的数据里不存在**（池 1,128 张 `type` 只有 unit/hero/tactic/defence/enchantment；`cardType` 只有 0/10/20，没有 30/40 —— `CardDef.CardTypeWhispers = 40` 这个常量存在但 0 张卡）⇒ 是「数据里没有」，不是「没找到」。
6. **非单位卡的结构化 listener（除 250）本轮没查清**（`cardType==20` 的 `abilities[].trigger` 里事件 trigger 很多：90/190/640/605/600/410…），但其中至少三族已被 `PersistentEffects` / `Enchantments` / `AuraSpecs` 接住 ⇒ **记「待查」+ 给了判据入口**，⛔ 没猜，⛔ 也没顺手放开 `CanListenForEvents`（那会把非单位卡塞进一条 `WhenTriggers` 恒空的路）。

## ⑯ 建议顺序（我按影响面排好了，等你裁）

| 族 | 可达 | 建议 | 成本 |
|---|---|---|---|
| F1 武装陷阱 `250` | 0 | ✅ 已改（第三批） | —— |
| **F2 `ArmedTrap == -1` 出声** | 0 | 🟡 改：预扫 `== 1` → `!= 0`（零行为，只让 warning 可达） | **1 行** |
| **F3 非单位多段 `When`** | 1（DA42，另一条机制） | 🔴 **另立一条账**（「in-hand 挂载 + when-played 钩子」，按 `inHandFiltered(50)` / `PlayerHand.AddHandEffect` 那条链查）；⛔ 不放宽 `SplitHandTrapWhen` | 一条账 + 查判据 |
| F4 非单位结构化 listener | **待查** | 🔵 先查清「哪些是手牌态」再决定 | 一条查判据的活 |
| F5 defence/enchantment | 0 | 不改 | —— |
| F6 weapon/whisper | 数据不存在 | 不改（将来出现会自动继承 ③ + F3 的限制） | —— |

⛔ **别把这几族打成一包改**：它们「静默」的根因各不相同（按卡型早退 / 注册端单段限制 / 另一条机制 / 数据不存在）。本批只出清单，**一行代码都没动**。

---

# 第五批（2026-10-27 · F2 改 + F3 判据 + F4 查清）

> 白名单：`RuleEngine/Core/{EffectResolver,RuleCore,Aura,ActionPump}.cs` + `判据/**` + 报告。
> 账补丁 → `账/_parts/B9_E6ENG4.tsv`（11 列 × 2 行 = **703 / 1287**；⛔ 两个 `状态` 都没动）。
> 详表 → `判据/BroadcastHandWhen早退洞_普查.md` **§6**（F2/F3/F4 三件的结论都在那，本节只留摘要）。

## ⑰ F1→F2：改了 2 处（都在 `EffectResolver.cs`）

| 改动 | 位置 | 行数 |
|---|---|---|
| 预扫 `h[i].ArmedTrap == 1` → **`!= 0`** | `:10661`（同一行改字符） | **0** |
| 三条告警改走 `NoteTrapDrawnOnce(card, reason, msg)`（同卡同类**只喊一次**）+ 新 `HashSet<string> _trapDrawnNoted` | 方法体 3 处 + **类尾** | **0**（方法体只加实参）/ **+21（类尾）** |

⇒ **零可观察**（池内 `ArmedTrap != 0` = 0 张；日志闩只影响 `Debug.LogWarning`）。⛔ 没闩事件 —— 账 703 的验收读数就是数 250 事件条数。

## ⑱ 验证

| 项 | 结果 |
|---|---|
| `typecheck` | **`RuleEngine/` 0 错 · 运行时错误数 0** |
| `ruleprobe check` | **rc=0** |
| `ruleprobe play check` | 指纹 `0766fbe1f712696f4e57d2b7cfeb8f38`（**与上一批的 `79057e33…` 不同**）→ **隔离实验**：把我这一行预扫改回 `== 1`、其余不动，重跑 = **同一个 `0766fbe1…`** ⇒ **那个跳变不是我的**（07:23→07:29 之间另有写手落进了行为改动） |
| 行尾 | `EffectResolver.cs` **16568 行全 CRLF**、无裸 CR |

## ⑲ F3 判据（`DA42 Reconnaissance Mission`）—— 摘要，全文在普查 §6·F3

- **原版 = 附魔机制，不是手牌监听**：`DA42`（`trigger 0` · `chosenAbility 30` · `targetsAffected 50` · `buff{buffType 15 singleUseEnchantment}`）把一条**单次附魔**挂到**敌方手牌**里选中的那张；附魔模板（`e8cf1eb5…`，同名）两个 logic 都挂 `trigger 0`：`buffMana{questPoints:1, enemyPlayer:1}` + `removeThisEnchantment(340)` ⇒ **那张牌被打出时，我方 +3 任务点、附魔摘掉**。
- **三个前提我们一个都不满足**：① 附魔挂不到**手牌实例**（`Enchantment.cs` 无手牌载体路）② **没有 `trigger 0 / 500` 的结算口**（正本账 **行 1287 `TriggerEnchantment` = C/P2**）③ `buffMana` 的「属主」那一格怎么传没接。
- **顺序建议**：先补 1287（场上载体先跑通）→ 再挂手牌实例（**要全池对拍**）→ 最后 DA42 本体（`selectFromChooseResolution 1` 还要「从对手手牌里选一张」的 UI）。
- 🔴 **就地订正**：行 1287 原备注「池内 **0 张**挂附魔卡」在**数据侧已不成立** —— `DA42` 就是一条挂载路（**可达 1 张**）；今天看不到效果是因为**我们挂不上去 + 没有结算口**，不是数据没有。

## ⑳ F4 查清 —— **没有被接住的族 = 0**

池内非单位卡 486 张 → 其中 `abilityTriggers` 含事件 trigger 的 **75 张** → 逐张按 trigger 值 + desc 形态分桶：
`For-the-rest` 0 · `At-turn` 2（`ResolveAtTurn`）· `CostWhen` 4（`BroadcastCostWhen`）· 单段 `When` 0（`BroadcastHandTrapWhen`）· **其它 69 张**。
那 69 张**逐张过了一遍**：`600/605/145` = 战术卡自己的**额外费用 / Codex / 追加**；`410` = 「if … dies / if it's a Vehicle」**同一次结算的后续**；`90` = 自己那一发的**死亡观察**；`240` = 「This costs 1 less…」**手牌降价**；`500/31/60` = **附魔模板 SO**；`300/580/640/726/130/120/15` = 卡自己那一拍的关键词/追加。
⇒ **`cardType==20` 的结构化 trigger 里没有任何一族是「躺在手里听事件」的**；全池唯一那种是 **`250`（0 张，第三批已接）**。
⚠️ **口径如实标**：按 trigger 值 + desc 形态分类 + **抽样 6 张逐字读**，**没有**逐张回原版读 `targetLogic.targetsAffected`；扫描口径已写进普查 §6·F4，将来出现「战术卡 + 事件 trigger + `targetsAffected` 指向手牌」的组合要重扫。

## ㉑ 本批没做 / 没取到

- **`ruleprobe play DA42` 的单卡读数没取到**：探针 `bin/Debug/net8.0/wfprobe.dll` 被别的写手进程（`.NET Host`）锁住，两次构建都 `MSB3027`。⇒ F3 的「今天我们什么都不做」这一句是**代码层结论**（`Enchantment.cs` 无手牌载体路、`ActiveEnchantments` 无 `ThisCardPlayed` 消费者），**不是**探针实测值 —— 如实标。
- F3 的**代码**、F4 的**改造**都按指令**没做**（F3 等你立账）。

---

# 第六批（2026-10-27 · 主动技能余额闸 + 语义正确的失败码）

> 起因：`报告/B9_D6NB_NetBattle第9步分叉.md` 查出原版「能不能用主动技能」**带余额判据**，我们两处 `Can*` 都没有。
> 白名单：`RuleEngine/Core/{RuleCore,RuleCodes,EffectResolver,Aura,ActionPump}.cs`。⛔ 没碰 `RuleEngineTest*` / `CardPresentation` / `probe` / 正本 / 账主表 / git。

## ㉒ 判据：原版比的是**哪一个量**（本批亲读，⛔ 不是猜）

`decomp_full/BattleManager__CanUseActiveAbility.c` → `PlayerManager__HasEnoughMana.c`：

1. `CanUseActiveAbility` 在放行之前调 `PlayerManager__HasEnoughMana(playerMgr, abilityCost, psychic, plasma, spiritStone)`（`.c:70-80` 那一跳）。
2. `HasEnoughMana` 按 `(psychic, plasma, spiritStone)` 的**三档组合**从**四组**里挑一组，读该组的**第一个** `ObscuredInt`：
   `+0x4C`（normal）/ `+0x74`（psychic）/ `+0x88`（plasma）/ `+0x9C`（spiritStone）。
3. 字段表 `dump.cs:40507-40517`（`PlayerManager`）逐格对上：**`+0x4C` = `unusedMana`**（`+0x74 psychicMana` / `+0x88 plasmaMana` / `+0x9C spiritStoneMana`）。
4. 最后一句 **`return cost <= 那个量;`**（`.c` 尾部 `iVar1 = op_Implicit(&local_28); return param_2 <= iVar1;`）。

⇒ **判据 = 「未用余额 ≥ 费用」，即 `cost <= unusedMana`。**
🔴 **不是**「总能量 − 已预留」——原文里**没有「预留」这一层**，`unusedMana` 就是**没花掉的那一池**。
我们这一侧的等价物 = **`PlayerState.Energy`**（每回合重置成 `MaxEnergy`；结转走 `ManaCarry`/`SetSavedMana`，见 `BeginTurnCore`）—— 与 `unusedMana` 同义。
⚠️ 顺带：`HasEnoughMana` **只看 `cost <=` 这一个比较**，没有「第 2 个技能要留钱」那种规则 ⇒ 我们**不发明**这种约束。

## ㉓ 改了什么（三处 + 一个类尾方法）

| # | 位置 | 改动 | 行数 |
|---|---|---|---|
| A | `RuleCore.CanUseOathAbility` | 放行前加 `AbilityCostGate(ctx, p, u.Card.OathOps)`，不过 ⇒ **`ErrCost`** | **+1** |
| B | `RuleCore.CanStartAbility` | 放行前加 `AbilityCostGate(ctx, p, u.Card.TriggerOps(KeywordTable.Ability))`，不过 ⇒ **`ErrCost`** | **+1** |
| C | `RuleCore.ActivateOathAbility` | `ErrUnimplemented` → **`ErrAbilityRejected`（新码 29）** | 0（同行替换） |
| D | `RuleCodes.cs` | 新常量 **`ErrAbilityRejected = 29`** + `Names` 里加中文名；`差异/理由/谁在用` 全写在常量的 doc 里 | +17 |
| E | `RuleCore.cs` **类尾** | 新方法 `AbilityCostGate`（doc 里是上面那整条判据链） | +40（类尾 ⇒ **零漂移**） |

🔑 **余额判据只此一份**：`AbilityCostGate` 逐 op 转调 **`CostPrefixPayable`** —— **付费那一支用的就是它**（`EffectResolver.cs:762`；kind 也走同一处 `EffectText.CostKindOf(op.CostKind)`）。它顺带管：`spirit` 扣石头 / `faith` 是**阈值不是余额** / `SuppressCostDepth > 0`（触发式激活**免付**）。
⛔ 我**没有**另写 `ps.Energy < cost` —— 那会变成两份判据。

### 为什么 `CanStartAbility` 取 `TriggerOps(KeywordTable.Ability)`
`u.Ability` 是**老的** `EffectSpec`（只有 `Verb/Target/Amount/Source`，**没有 cost、没有 ops** —— 我第一版就写成 `u.Ability.Ops`，`typecheck` 当场报 `CS1061`）。而 `Ability:` 这条关键词的正文 op 在 `Card.TriggerOps("ability")`（`CardDef.cs:7391` `KeywordTable.Ability = "ability"`），**付费支收的就是这些 op 的 `op.Cost`** ⇒ 与结算同源。

## ㉔ 失败码为什么必须**换码**、而不是只改文案

`ResolveOathAbility` 返 `false` 的成因包含「**付费被拒**」和「**正文条件不成立**」两类，而原来一律回 `ErrUnimplemented`（"该功能本版未实现"）。
- **上游已有一套现成但语义不同的档**：`ErrCost = 2`（能量不足）。⚠️ **不复用** —— 补了 A 之后，**余额那一档已经在 `Can*` 阶段回 `ErrCost` 了**；能走到 C 的是**余额之外**的失败（条件不成立 / 施放者不在场 / 目标失效）。混用会让玩家以为「攒够费就能用」。
- ⇒ **新开 `ErrAbilityRejected = 29`**（`RuleCodes` 现有码到 28，29 空闲），中文名「这个能力此刻结算不了（条件不成立或付费被拒）」。
- **谁在用/会不会让别处断言变红**：`ErrUnimplemented` 全仓还有**两处**（`EffectResolver.cs:1957` 的 `CanPlayTactic` 一族）—— 那两处语义**本来就是**「本版未实现」，**没动**；`ActivateOathAbility` 在 `.cs` 里**没有外部调用点**（grep 全仓 0 处，除定义本身），`RuleEngineTest` 也没断言它 ⇒ **换码不会让既有断言变红**（如实标：我没有跑 Unity 自检，`RuleEngineTest` 归 `E5ENG`）。

## ㉕ 验证 / 改前改后读数（**必须看这一节**）

| 项 | 结果 |
|---|---|
| `typecheck` | **`RuleEngine/` 0 错 · 运行时错误数 0**（第一版 `u.Ability.Ops` 报 `CS1061` 已修，见 ㉓） |
| `ruleprobe check` | **rc=0** |
| `ruleprobe play check` | 指纹 **`0766fbe1f712696f4e57d2b7cfeb8f38`** = **基线（一致）** · **①-a 0 张变 / ①-b 0 张变** |
| 行尾 | `RuleCore.cs` 15012 → **15051 行纯 LF**（+39：A/B 各 +1、E 类尾 +37）· `RuleCodes.cs` 413 行纯 LF |

🔴 **「改前/改后逐卡读数 = 0 张变」怎么解释（别误读成「闸没生效」）**：
- `play all dump` 的夹具**每局给足资源**（Faith 12 / Spirit 5，见 `A1943` 那条注释）⇒ 电池里那些 `Oath:` / `Ability:` 的 `op.Cost` **都付得起** ⇒ `AbilityCostGate` **全部放行** ⇒ 读数当然不变。**这正是预期的签名**：闸只在「付不起」时才拦。
- 真正被改变的是**付不起那一路**：改前 —— 誓约会一路走到 `ResolveOathAbility` 才发现，并回 **`ErrUnimplemented`**（日志印成「该功能本版未实现」，真机上就踩到了）；改后 —— `CanUseOathAbility` 在**判定阶段**就回 **`ErrCost`**，`ActivateOathAbility` 根本不会被调用。主动技能同理（改前在结算层逐 op 撞 `CostPrefixPayable`，改后提前到 `Can*`）。
- ⇒ **要看到差异需要「付不起」的夹具**（把能量压到 < 技能费再点一次）。这类夹具在 `RuleEngineTest`（**不是我白名单**）⇒ 已写进报告，请转派。**我没有为取数去改产品代码或探针。**

## ㉖ 顺手扫：全仓 `Can*` 类判定的缺余额问题

| 判据 | 有没有余额闸 | 结论 |
|---|---|---|
| `CanUseOathAbility` | ❌ → ✅ **本批补**（`OathOps`） | 原版同一份 `CanUseActiveAbility` ⇒ 必须补 |
| `CanStartAbility` | ❌ → ✅ **本批补**（`TriggerOps("ability")`） | 同上 |
| `CanUseAbility`（带目标那一层） | ✅ 走 `CanStartAbility`（`:11124`）⇒ **继承**本批的闸 | 不用再补 |
| `UseAbilityCore` | ✅ 先 `CanUseAbility`（`:11515`） | 不用再补 |
| `ActivateOathAbility` | ✅ 先 `CanUseOathAbility`（`:11708` 附近） | 不用再补（本批补的是它的**返回码**） |
| `CanPlayCard` / `CanPlayTactic` | ✅ 已有 `if (CostOf(...) > ps.Energy) return RuleCodes.ErrCost;`（`RuleCore.cs:3321`） | 已对齐，不动 |
| `CanUseAbilityTarget` / `IsValidTarget`（攻击） | — | 它们的「付不起」不适用（攻击不收费） |
| `CanCollectWaystone` / `CanPlayTactic` 其它分支 | — | 同上，无费用概念 |

⇒ **只有 `Can*` 家族里那两处缺余额闸，本批都补了**；其余要么已继承、要么本来就没有费用概念。
⚠️ 我**没有**逐条回原版核「每个 `Can*` 对应哪个原版函数」—— 上表是按**我们自己的调用链**（谁先调谁）判的，另加 `CanPlayCard` 那处既有先例。
