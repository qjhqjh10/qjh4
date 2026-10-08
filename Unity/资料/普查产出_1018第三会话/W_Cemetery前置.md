# W_Cemetery前置.md — `A985⑥` 前置那一半：给引擎加 3 种事件（2026-10-18 · 第三会话）

> 执行写手代理。**只动白名单**：`RuleEngine/Core/{BattleEvent,RuleCore}.cs` · `RuleEngine/Editor/RuleEngineTest.cs`
> （`EffectResolver.cs` / `BattleContext.cs` 一个字没动）。⛔ 没跑 Unity · 没动 git · 没改正本 · ✅ 秒级类型检查末次 0/0（改完即跑，共 5 次）。
> ⚠️ **白名单读法**：枚举**只能在 `Core/BattleEvent.cs`** ⇒ 按「`RuleEngine/Core/` 这个范围」执行（只多碰这一个文件）；不合口径的话枚举那处整体回退即可。

---

## 1. 原版判据：3 种事件各自的时机（逐条读的方法体）

| 事件 | 类型 | **在什么时机发**（判据 = 方法体名 + 行） | 我们这一侧的对应点 |
|---|---|---|---|
| **抽牌** | `0x14` → `Action{You,Opponent}Draw` | `CemeteryManager.AddDrawTrapAction`（`CemeteryManager__AddDrawTrapAction.c:86`）**唯一**调用点 = `BattleManager.<ResolveDrawCard>d__432.MoveNext` case 4/5：`RemoveCardFromDeck`(:282) → `IsDuringMulligan` **假**时 → `BroadcastCardDrawn`(:285) → **[本行]** `AddDrawTrapAction`(:291) → `SetCardTurnDrawn`(:295)。⚠️ 包在 `if (card.armedTrap != 0)` 里（`EntityScript.armedTrap` = `dump.cs:21983` `// 0x54`） | `RuleCore.Draw`：`BroadcastWhen(WhenEventKind.Draw)` **之后**、`inst.DrawnThisTurn = true` **之前**（与原版同序） |
| **撤伏击** | `0x28` → `ActionExitAmbush` | `CemeteryManager.AddExitAmbushActionToCemetery`（记录第一个 int = `0x28`，`:37`）**唯一**调用点 = `CardScript__SetAmbush.c:105` —— **`SetAmbush(card, false)`**（撤掉那一条；方法头部 `param_2 != 0` 是**设**为伏击、不记日志）。`SetAmbush(card,0)` 三个调用点**全是伤害路**：`CardScript__ResolveUnitAttacked.c:111` · `CardScript__ResolveDamageDealt.c:173` · `BattleManager._ResolveAttack_d__438__MoveNext.c:305` | `RuleCore.ApplyDamage` 里 `if (u.FaceDown) { u.FaceDown = false; … }`（**盾挡下 / 无敌 / 减到 0 都到不了这里** ⇒ 与原版「真掉血才撤」同形） |
| **密令** | `0x19` → `ActionSecretOrder` | `CemeteryManager.AddExecuteSecretOrderAction`（`:75`）**唯一**调用点 = `BattleManager.<ResolveRevealOrders>d__549.MoveNext:76`：那一刻已 `DisplayRevealedCard` → `DestroyCard` → 必要时 `RemoveEnchantment(…,2)`，**最后**记这一行。上游 = `AbilityLogic.PlayAbility` 的 `spellId == 0x118`(280) 支 → `BattleManager.AddRevealOrders`（`AbilityLogic__PlayAbility.c:2495`），后者压一条 **`BattleAction` 类型 `0x31`**（`BattleManager__AddRevealOrders.c:32`） | **没有发出点** —— 见 §3 |

**顺手订正 `G5` 报告那张枚举表的一处**（铁律 5）：`0x14` 支里 `(char)*(param_2+6)=='\0'` 取 `DAT_184285c40`，**字符串表实读 = `ActionOpponentDraws`**（另一支 `DAT_184286420` = `ActionYouDraw`）；判据位 `EntityScript.isPlayer` **为 0 = 是敌方那张**（`dump.cs:21973` `// 0x40`）⇒ `G5` 表里那两项的**顺序与原版相反**（两态都在，结论不变）。另：`0xF` 的判别式 = **记录 +0x28 那个引用是否为 null**（`GetActionText.c:215`，方法体写作 `param_2[10]`，而 `param_2` 是 `int*` ⇒ 下标 10 = **字节 0x28**，别被变量名 `uStack_50` 带偏）—— 见 §4。

---

## 2. 加了什么（按符号）

| 文件 | 加了什么 |
|---|---|
| `Core/BattleEvent.cs` | `EvtKind` **末尾**追加 3 个：`Draw` · `AmbushExit` · `SecretOrder`（各带整段判据注释）。**只许追加、不许插队**（表头写着「少而稳定」）。**`BattleEvent` 的字段一个都没加、没改** |
| `Core/RuleCore.cs` | ① `EmitUnit(...)` 多一个**可选**参 `string effect = null`（老调用点零影响，转发给 `ctx.Emit(..., effect:)`）；② `Draw`：`if (!ctx.MulliganOpen) ctx.Emit(EvtKind.Draw, p, -1, card.Name);`；③ `ApplyDamage` 翻伏击那句后：`EmitUnit(ctx, EvtKind.AmbushExit, u, 0, effect: "damage")`；④ `RevealAmbush` 里 `u.FaceDown = false` 后：`… effect: "window"` |
| `Editor/RuleEngineTest.cs` | 新增 `Section/Step(TestCemeteryEvents)`（挂在 `TestAmbush` 之后）+ helper `HasLog(ctx, kind)`；**改动了既有断言 1 处**（`TestRally` 的 `ActionLog.Count == 3`，见 §6 末） |

**⛔ 没有自造中文模板、没加 `Loc` 键、没碰 `BattleDriver`** —— 那 12 条中文模板与 19 条词条值照旧挂着。**⚠️ 两处「照旧标着」的多发**（不是顺手做宽）：

- `AmbushExit` 的 `"window"` 档（我们 `RevealAmbush` = 「撑一轮才翻开」）：**原版没有这一档** —— 原版那条路走 `CardScript.TriggerAmbush`（`RawCardScript.OnTrigger(0x29e,…)`），**它不翻面**（`SetAmbush` 的调用点里没有它）。**仍然发**（事件语义 = 「不再面朝下」这个状态迁移），用 `Effect` 记下是哪一档 ⇒ **消费端要严格照原版就只印 `"damage"`**。🔴 **连带一条请分流**：我们 `RevealAmbush` 这条出口**本身与原版不同形**（判据就是这一句）。
- `Draw`：原版那一行**只在「抽到的是已布设的陷阱」时**记（`armedTrap`），我们**没有陷阱机制** ⇒ 本事件**每次抽牌都发**；要严格照原版就判不了（没有那个位）⇒ 如实挂着、**没加恒假字段**。会多记的入口：① 教程局起手（`TutorialRules.SetupInitialHand` → `RuleCore.Draw`，`MulliganOpen` 是**假**）；② 普通局起手**不会**（走 `DealOpeningHand`、不走 `Draw` ✓）。

---

## 3. `SecretOrder`：**今天没有发出点**（如实挂着，⛔ 没猜）

原版那边它 = **战将身上一件面朝下的「密令」附魔**：`EntityScript__HasSecretOrders.c` 数的是 `+0x110` 那串 `activeEnchantments` 里 `CardEffect + 0x90 == 0x3c`(60) 的（`AI__ValueOfEnchantments.c:65` 同一判据）。我们**没有这一层模型**：`grep Enchant` 全仓零模型（只有注释）；`KeywordTable` 里**没有 `secretOrder`**（原版那条是 `Card_Trait/secretOrder`，字符串表实读）—— ⚠️ **它 ≠ 我们的 `Secret` 兵种牌**（秘仪 / `spellType 150`）：原版打出秘仪走 `PlayerManager.AddSecretPlayed`（`_ResolvePlayCardFromHand_d__447:1274`）。⛔ **还有一条是半推的**：`ResolveRevealOrders` 在 26,282 个方法体里 **grep 不到调用点** ⇒「动作 `0x31` → 它」是**从动作类型推的**、不是直读。⇒ 要让它真发出来**先得有密令那套机制**（面朝下放置 + 揭示时机），那是另一件活；今天只是**把表补全**。

---

## 4. `Ability.Targeted` 那个「缺字段」：**判了 · 不用补字段，缺的是【填充】**

- **判别式（原版）** = 记录 +0x28 那个「目标引用」是否为 null ⇒ `ActionAbility` / `ActionTargetedAbility`。
- **我们有现成的位**：`BattleEvent` 早就有 `TargetPlayer`/`TargetSlot`/`TargetCardId`，而 `RuleCore.UseAbility` 那一处**目标就在手边**：`var chosen = EffectTargets.NeedsPick(spec.Target) ? ctx.Players[1-p].Board[targetSlot] : null;` ⇒ **`chosen != null` 就是那个判别式**（**不需要新字段**）。
- 🔴 **本笔没填**，理由（判据在别人的文件里，一动就是静默改行为）：`TargetPlayer`/`TargetSlot` 被表现层直读两处 —— `BattleDriver.cs:1813`（`targetIsPlayer = e.TargetPlayer == _me`）· `:1438`（`targetIsWarlord = _lastEvent.TargetSlot == WarlordSlot`），消费方 `WFModuleCollisions.cs:392` / `WFModuleTransformModifier.cs:384` ⇒ **填了会改技能特效行为**（尤其 AI 施法那侧：`TargetPlayer = 1-p` 会让 `targetIsPlayer` 翻真）。**改法（留给你裁，一行）**：`UseAbility` 那条 `ctx.Emit(EvtKind.Ability, …)` 上加 `targetPlayer: 1 - p, targetSlot: targetSlot, targetCardId: chosen != null ? chosen.Name : null`，并在表现层同步核 `targetIsPlayer` / `targetIsWarlord`。
- ⚠️ `Play` 那 6 条的细分（靶向 × 你是谁 / 伏击）**同理**，落点在**两个** emit 点（`RuleCore.cs:1537` 单位卡 / `EffectResolver.cs:1080` 战术卡，后者带着 `targetSlot`）；伏击那半 = `card.Has(KeywordTable.Ambush) && TriggerOps(Ambush) != null`（`RuleCore.cs:1610` 那个条件，**本笔改完之后的现读行号**），两点都算得出。**⛔ 本笔没动**（那是简报里「①」之外的 ②）。

---

## 5. 两个记账口 / 哈希影响：**判了**

- **两个记账口**：本笔加的是**引擎侧广播**，**不是「会改引擎状态」的动作路径** —— 它不改状态、只往事件流塞一条记录，发出点都在**已有的、走那两个记账口的动作内部** ⇒ **不新增记账点、也不需要**（录像录「起始条件 + 动作流」）。
- **哈希影响：零。** 新事件**不进** `NetProtocol.Fingerprint` —— 它读 `Turn/Active/Winner` + **`ctx.Events.Count`（字符串日志）** + `PlayerHash`，而 `Emit` 只写 `Signals`/`ActionLog`；⛔ **所以本笔全程没加一句 `ctx.Log(...)`**。`StateHash` 两者都不含 ⇒ **旧录像 / 旧联机局不受影响**；枚举**追加在末尾** ⇒ 不动任何按整数存的值。
- **表现层影响（唯一可见的一处）**：两条新事件会进 `ActionLog` ⇒ `RefreshBattleLog` 的 **`default:` 分支**把它们印成「我方「卡名」」（无动词）⇒ **战斗日志面板会多出抽牌 / 撤伏击行**，可能把刚打出的那一步挤出 8 行窗口（`BattleScene.cs:2818-2826` 那条断言扫前 8 行找「打出」）。**其余全是 no-op**：`PlaySignal` 的 `switch` 里新 kind 落到 `default: return;`，`SpeakFor`/`PlayRemnantSfx` 各自也是 `default: return`，`EventTiming` 是 `default: 0f`。**建议**：要今天不看见它，在 `RefreshBattleLog` 的 `switch` 里加 `case EvtKind.Draw: continue;`（表现层那一步的活）。

---

## 6. 断言：`TestCemeteryEvents`（`Editor/RuleEngineTest.cs`）

| # | 断什么（**两态**） | 🧨 改坏法 | 灭自证 |
|---|---|---|---|
| ① | 真·抽一张 ⇒ **恰好 1 条** `Draw`；`Player` = 抽牌方；`Slot` = -1；**`CardId` = 现读手牌里新出现那一份** | 删掉那句 `Emit` ⇒ 红 | 卡名**不是字面量**，是「按实例号 diff 出来的那一份」⇒ 把 Emit 挪到 `MoveDeckTopToHand` **之前**也红 |
| ①b | 换牌阶段补抽（`MulliganOpen` 真）⇒ **0 条**；`EndMulligan` 后再抽 ⇒ **1 条** | 去掉 `if (!ctx.MulliganOpen)` ⇒ 第一条红；**整句删 ⇒ 第二条红** | 两态成对：用「永远不发」蒙不过去 |
| ②a | 面朝下单位挨真伤害 ⇒ **恰好 1 条** `AmbushExit`、`Effect == "damage"`、`Player`/`CardId` 对、**格位现读棋盘 = 那个单位** | 删掉 Emit ⇒ 红 | 格位**不写字面量**（`Board(ctx,0,ev.Slot) == u`） |
| ②b | 同一格**已翻开**后再挨一次 ⇒ **0 条**（但确认真发了 `Hit`） | 把 Emit 挪出 `if (u.FaceDown)` ⇒ 红 | 与 ②a 成对（否则「每次 Hit 都发」也能过 ②a） |
| ②c | 面朝下 + **Shield** 被打 ⇒ **没翻开**、**0 条** | 把 Emit 提到护盾那一关之前 ⇒ 红 | 同时断「它仍 `FaceDown`」⇒ 不是靠「没人打它」蒙过去 |
| ②d | 回合窗口那条出口 ⇒ **1 条**、`Effect == "window"`、格位现读棋盘 | 删掉那条 Emit ⇒ 红 | **「两档标签不同」**：只留一条出口的实现无论怎么改都过不了（同一枚举上必须出现过两种标签） |
| ③ | `EvtKind` 现在 **15** 种、三个新名字都在表里 | 加第 16 种而不改这条 ⇒ 红（**「事件表」护栏**） | 表式断言 |
| ③b | 跑两个回合 ⇒ **没有任何一处发 `SecretOrder`** | —— | ⚠️ **记账、不是行为锁**（机制不存在）；注释写明「接上那套机制时改成『该发的时候发了』」 |

🔴 **既有断言改了 1 处（如实报）**：`TestRally` 的 `Check(ctx.ActionLog.Count, 3, …)` —— 它**依赖「推进回合不产生日志」**，而 `ToP1Turn` 里每次真抽牌现在都会记一条，`ActionLog` 又**不受 `ClearSignals` 影响** ⇒ 3 这个字面量不再成立。改成**断尾部三条**（Play → Trigger → Hit，原意图逐字保留）**并反着加钉一条**「日志里**一条 `Deploy` 都没有**」（🧨 删掉 `AppendLog` 的合并 ⇒ 红）。全仓只有这一处 `ActionLog.Count`、一处 `Signals.Count`（后者在 `ClearSignals` 之后 ⇒ 不受影响）。

---

## 7. 类型检查 · 行尾 · 没查清

**类型检查**（改完即跑，末次 0/0）：`--- 运行时程序集 --- 运行时错误数: 0` / `--- 编辑器程序集 --- 编辑器错误数: 0`。（中途出现过 1 条 `BattleDriver.cs(3327): AlliancePanelWindow.HitBody` —— **不是我负责的文件**，另一位写手的半成品，下一轮跑就自己没了，如铁律 13·3 所述。）

**行尾**（`b.count(b'\r\n')` vs `b.count(b'\n')`，**不是** `file` 命令；全程 Edit 工具、⛔ 无 `sed -i`）：

| 文件 | CRLF | lone LF | 判定 | 本笔净增/删 |
|---|---|---|---|---|
| `Core/BattleEvent.cs` | 0 | 252 | **纯 LF（改前就是）** ✅ | +101 / −0 |
| `Core/RuleCore.cs` | 0 | 5236 | **纯 LF** ✅ | +34 / −2 |
| `Editor/RuleEngineTest.cs` | 21714 | 0 | **纯 CRLF** ✅ | +221 / −2 |

**没查清 / 留给你的**：
1. **`RevealAmbush`（回合窗口那条）与原版不同形**（原版 `TriggerAmbush` 不翻面）—— 判据已记，**要你裁**（§2）。
2. **`Draw` 的 `armedTrap` 那一档判不了**（没有陷阱机制）⇒ 日志若严格照原版只印陷阱行，这个事件**今天没人用**。
3. **19 条的文案值照旧一条都没有**（远端 I2）—— 本笔没碰；今天**能多推 3 条**（`Action{You,Opponent}Draw` + `ActionExitAmbush`）。
4. **没跑 Unity**：本笔宿主 = `RuleEngineTest.cs` ⇒ **至少 `RuleEngineTest.Run` 必需**；`BattleScene.Run` 建议一起（**日志面板那 8 行窗口**会因 `Draw` 行变化，见 §5 末）。
