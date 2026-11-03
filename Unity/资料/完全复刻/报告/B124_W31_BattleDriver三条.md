# B124_W31b · `BattleDriver.cs` 两条（2026-11-03）

**唯一改动文件**：`CardPresentation/Battle/BattleDriver.cs`（42072 → **42139** 行，**+67 全在类尾**；纯 CRLF、裸 CR 0）
**typecheck**（`TMPDIR=/tmp/wf_b124w31b`）：运行时 **0** / 编辑器 **0** · **账闸**：无一条 BattleDriver 漂移/失败（现读 63 漂移 / 8 失败**全在别人的文件**：`CollectionWindow.cs` / `DeckInfoPopup.cs` / `CardFeel.cs`）
**未跑 Unity**（离线写手红线）；断言未加（`Editor/` 不在白名单）⇒ §4 给了可粘贴版。

## 1 · `B121-E04B-01` 回牌库的表现 = **两段**（已做）

**关键订正（改变了修法）**：原计划「把 `ReturnToHand` 换成 `UndissolveToDeck + MoveToDeck`」**不完整** —— 亲读 `decomp_full/BattleManager._ResolveRecallToDeck_d__543__MoveNext.c` 全 365 行：state 0 `:126` 起 `CardScript.TransformUnitInPlayToCard`（**它内部 `:52` 就是** `BattleCardUI.PlayBackToHandAnimation`）⇒ 倒放**是原版第一段**；state 1 `:182/:319` `UndissolveToDeck`；state 2 `:352` `MoveToDeck(card,1,0)`（返回值 0.1+0.3 = **0.4** = `EventTiming.MoveToDeckTotal`）⇒ **第二段**。`TransformUnitInPlayToCard` 被三条协程调（`_ResolveRecallToHand:273` / `_ResolveRecallToDeck:126` / `_ResolvePlayCardFromHand:958`）。

**改动** ①`:25692` 同一物理行改（零行数变化）：`onDone` 从「直接 Kill」改为 `() => ReturnFeelArrived(v, e)`；
②**类尾新增**（`:41989` 之后、`BattleDriver` 类内）：`DeckTargetScaleOf(float pileWorldH)`（终点缩放算式**收口一处**）、`static bool _warnedRecallToDeckNoPile`、`void ReturnFeelArrived(CardView, BattleEvent)`；
③`PlayReshuffleToDeckFeel` 的 `:41842-41843` 就地改成调用 `DeckTargetScaleOf`（2 行 → 2 行，去掉会变成未用的 `cardWorldH` 局部）。

**判据 / 复用**：目的地判别式**不新造** —— 用现成的 `EventTiming.ReturnDestOf`（`HookEventTimingCardSource` 按 `InstanceId` 在 `Hand`/`Deck` 反查，引擎**先搬实例再发事件**；`1` = 牌库/牌库顶）。第二段**复用** `CardFeel.ReturnToDeck`（三条同长补间 + `SetInDeckPosition` + 关卡）= `PlayReshuffleToDeckFeel` 用的**同一条链**；后者本身不能直接复用（它按**墓地**位姿**新建**视图、判别式要求 `Slot<0`）。回手那支逐字未动。

**两处如实记**：① 时长取 `EventTiming.MoveToDeckTotal`(0.4)：我们的等价物是三条**并行**同长补间、没有原版那 0.3 的 `SetDelay` ⇒ 在整段 0.4 内走完、收尾时刻对齐原版返回值；② 起第二段前显式 `v.SetFace(CardFace.Hand)` —— 否则 `CardFeel.ReturnToHand` 的 `OnKill`（`Face == Board` ⇒ `SetFaceGroupVisible(false)`）会在第二段起播**之后**把 2D 面关掉，飞回去的是空壳。

## 2 · `B124-BDRIVER-CMT` 注释订正（已做）

`:25669`/`:25688` 两处「原版两个调用者」**就地改写**（零行数变化、保留订正痕迹）：`PlayBackToHandAnimation` 全树**唯一调用者** = `CardScript._TransformUnitInPlayToCard_d__256__MoveNext.c:52`；共用的是 `TransformUnitInPlayToCard`（3 处调用点见上）；并写明回牌库的第二段与 `ReturnFeelArrived`。

## 3 · `B124-HOVERSND` —— ⛔ **判据本身错，未改（附反证）**

W29 的前提「原版悬停路径不发声」**不成立**：选中音**不在点击回调里**，在 `CardDisplayAttackTypeButton__DoPointerOverButtonActions.c`：

```text
if (sendInput(+0x90)==0 || isInputOverSent(+0x91)!=0) return;   // 悬停安全窗三格
Invoke(delegate +0x70)      // ← 点击消费者（AttackButtonClick），返回值**被丢弃**
*(+0x91) = 1;               // ← 标「本次进入已发」
AssetReferenceTyped.Load(+0x58) → AudioCue.Play2D   // ← 选中音，**无条件**
```

`UnitOnBoardAttackTypeSelector__AttackButtonClick.c:28` 的「同档 `return`」只让**指令**不重下单、**不会**拦掉上面的声音；`sendInput` 由 `Toggle(true)` 置 1（`CardDisplayAttackTypeButton__Toggle.c:14`），而选中后**没有** `Toggle(false)`（全库仅 4 个调用点）⇒ 悬停**已选**的那颗技能钮，原版就是**响一次**（每次进入一次）。我们现状（`MarkHoverPicked` 每次进入一次 + `_hoverSent` 拦住重复）**与之一致**；按工单收窄到「`CommitCommand` 真落地后」反而**少响**，且在「演出闸挡住 / 教程拒绝」两档会**每帧重试**（`_hoverSent` 不再置位 ⇒ `NotifyCantDoAction` 每帧刷一次）。⇒ 建议 `活状态表` 该行改 `⛔判据本身错`，工单撤回。

## 4 · 待调度台接（不在本代理白名单）

1. **断言（`Editor/`）**：回牌库那条 `Return`（棋盘 `Slot` + 该 `InstanceId` 放进 `ps.Deck`）经 `PlayFeelForTest` ⇒ `ReturningCount==1`，`Step(0.92)` 后**仍为 1**、`Step(0.5)` 后为 0 且销毁；反证 ① 同形状但实例在 `ps.Hand` ⇒ 0.92 s 后即为 0；② `InstanceId=0`（旧事件）⇒ 走回手支。
2. **账**：`战斗逻辑驱动.tsv:940`（→`:25671`）差异列补「回牌库档第二段 = `ReturnFeelArrived` 接 `CardFeel.ReturnToDeck`（`:319/:352`）」；`战斗界面_HUD.tsv:141/149` 现只提洗回那一趟，棋盘回牌库也走同一链，建议点一句。

## 5 · 顺手发现（未改）

- `EventTiming.ReturnHoldOf` 给「回牌库」排 **0.95 s**，而第一段（`CardFeel.ReturnToHand`）实演 **0.9167 s** ⇒ 第二段与下一条事件重叠（视图靠 `_returning` 活到补间完，画面不断）；原版第一段只等 0.55，这半秒差是既有的，不在本笔范围。
- `BattleScene.cs:10738` 那条自检合成的 `Return` 没有 `InstanceId` ⇒ 永远走回手支，本次改动对它零影响（已核）。
