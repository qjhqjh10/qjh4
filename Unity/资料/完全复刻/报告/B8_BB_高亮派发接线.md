# B8_BB · 场上卡高亮的**派发接线**（`BattleDriver` 三处 + 探针 + 两处口径订正）

> 执行代理 `BB` · 2026-10-26 · 批次 8
> 白名单（实际只动这一个 `.cs`）：`CardPresentation/Battle/BattleDriver.cs`
> 账补丁：`资料/完全复刻/账/_parts/B8_BB.tsv`（**3 行，全部 B → A**）
> 上游：`报告/B8_AW_场上卡高亮状态机.md`（状态机本体 + §七① 的可抄片段）· 口径：`判据/战斗逻辑驱动_B2A_口径.md`
> 判据源（🥇 本轮亲读 4 份）：`decomp_full/CardScript__OnTouchDown.c`（97 行）· `CardScript__OnTouchEnter.c` ·
> `CardScript__OnTouchExit.c` · `CardHighlight__ToggleCombatPreviewHighlight.c`（178 行）· 另读
> `EntityScript__EnemyReturnsAttack.c` / `EntityScript__GetCombatDamageWithType.c`（反击支的算式）·
> `BattleManager__StartAttackFrom.c` / `__StartSpellFrom.c` / `__StartMovingCardInHand.c`（`cardTracked` 的写入点）

---

## 一、结论

| 项 | 结果 |
|---|---|
| **三处接线** | ① **悬停每帧** = `Update`/`DrivePlayerTurn` 里新加的一句 `TickBoardHover(world);`（**在原 ② 段 `BoardPress` 之前**，`BattleDriver.cs:14344`）② **按下** = ⑤ 段记 `_pressSlot` 的**同一句**里调 `BoardCardTouchDown`（`:14423` / `:14427`）③ **伤害预览探针** = `BoardHighlightOf(view)` 惰性注入 `PreviewProbe`（`:32942-32951`） |
| **账** | `CardScript.OnTouchDown` / `OnTouchEnter` / `OnTouchExit` **3 条 B → A**。`OnTouchUpAsButton` **不动**（B8_AW 已证它与这台机器无关） |
| **改了多少** | **+194 行，全在类尾**（`:32880-33074`）；中间只有两处**就地改写且 net 0**（comment 压缩 / `CardHoldPeek` 体）。**零删改既有逻辑** |
| **漂移** | **0**。全账指向 `BattleDriver.cs` 的锚点共 **317 条**、**最大锚点行 = 32866** < 新增起点 32880 ⇒ 一条都不动（机检复核见 §三） |
| **机检** | `typecheck` **0 / 0**（运行时 + 编辑器两个程序集都干净；`CardDef.cs` 那条 `CS0708` 已被它的写手修掉）· `check_ledger_points` 对我的补丁 **A=3 通过=3 漂移=0 告警=0 失败=0** · `verify_annot` 规则自查 **同名 3/3**（首落点声明名 = 原版方法名）· `check_a_vs_diff` 词表自查 **0 命中** · 补丁 11 字段 ×（1 表头 + 3 行）· **LF、裸 CR 0** |
| 🔴 **一处重要订正（铁律 5）** | B8_AW 报告 **§七(e) 的探针草稿把 `willDie` 的主语写成了 `other`**（`WouldKillByEntries(other…)`）。反编译 `ToggleCombatPreviewHighlight.c:98-102` 的判死第一实参 = `param_1+0x50`（**这台高亮自己所属的 CardScript**）⇒ **`willDie` 说的是「我这一张会不会死」**，条目来自 `other`。已按反编译实现，见 `:33016-33053` |
| 🔴 **第二处订正** | `TrackedCard` **不能取 `_reticleTarget`**：`cardTracked(+0x330)` 的写入点是 `StartAttackFrom.c:14`（**攻方单位**）/ `StartSpellFrom.c:13`（法术）/ `StartMovingCardInHand.c:5`（手牌）⇒ 它是**玩家正在行动的那张卡**，棋盘侧的等价物 = **`_selectedSlot`**。取成「准星压着的目标」会把两张棋盘卡当成「行动卡 vs 目标」，算出一条原版不存在的预览 |

---

## 二、逐条证据

### 2.1 悬停（`OnTouchEnter` / `OnTouchExit` 的派发）

原版：`CardCollider` 是 uGUI 射线靶 ⇒ `IPointerEnterHandler/ExitHandler` → `CardScript.OnTouchEnter/OnTouchExit`
（`CardScript__OnTouchEnter.c:34-37` ⇒ `TryCardHighlight`；`CardScript__OnTouchExit.c:40-50` ⇒ `2⇒0` / `4⇒3`，**通篇无闸**）。

我们：**本仓棋盘命中共用一个口 `HitSlot`**（轻点 / 拖拽 / 教程探针都用它），没有 uGUI collider 那一层
⇒ 悬停只能**每帧轮询**，并且只在「压着的卡变了」时**成对**派发 exit → enter（次序照 uGUI）：

```
TickBoardHover(world)                                   BattleDriver.cs:32904-32928
  HitSlot(_myUnits) → HitSlot(_foeUnits)（两侧各一次，取先命中的那侧）
  hv == _hoverBoardCard ⇒ return
  旧卡：BoardHighlightOf(old).OnTouchExit(BoardTouchInput(old, _hoverBoardSide))
        + ApplyOwnAttackType(旧卡)   ← 见 §六·1
  新卡：BoardHighlightOf(hv).OnTouchEnter(BoardTouchInput(hv, hSide))
```

`BoardTouchInput`（`:32961-32992`）= 把原版那几个读数映射成 `CardHighlight.TouchInput`，**不另立判据**：

| 字段 | 我们的取值 | 原版出处 |
|---|---|---|
| `CardState` | `CardStateId.inPlay` | 棋盘卡恒 `inPlay(2)` |
| `IsPlayer` | `side == _me` | `EntityScript+0x40` |
| `HoverAllowed` | `Ctx != null && !Ctx.IsOver && !InMulligan && Ctx.Turn > 0` | `TryCardHighlight.c:19-24` 的封锁集 `{0,7,8,11,12}`（**⛔ 不是 `BattleUiIsNormalBattle`**，那条会多挡 5 档 —— 见 §六·3） |
| `ChoosingResolutionCard` | `_choosePanel.Visible` | `UIstate==14` |
| `RightButtonOrTwoFingers` | `interaction.IsRightButtonOrTwoFingerTouch()` | `OnTouchEnter.c:12-14` |
| `ChoosingUnitOrAbilityTarget` | `_selectedSlot >= 0 \|\| _command != None` | `UIstate ∈ {9,10}` |
| `MulliganElementsVisible` | `_mulligan.Visible` | `+0x1E8` |
| `TrackedCard` | `_selectedSlot >= 0 ? ViewAt(_me, _selectedSlot) : null` | `cardTracked(+0x330)`（见 §一 第二处订正） |
| `PressSeconds` | `interaction.PressElapsedForTest`（那条私有读数的公开 getter，实时值） | `+0x320`（只被**未接线**的 `OnTouchUpAsButton` 读） |

### 2.2 按下（`OnTouchDown` 的派发）

原版 `CardScript__OnTouchDown.c:61-70`：`cardState == 2` ⇒ `cardHighlight.state == 0` 时 `ChangeState(2)`，
然后**无条件** `(+0x24A) = 0`。我们把调用点钉在**与 `CardCollider.IPointerDownHandler` 同一刻**的地方
—— `Update` ⑤ 段「记 `_pressSlot`」那两句（`:14423` / `:14427`），一行里顺带调一次，**不新增行**。

### 2.3 伤害预览探针（`PreviewProbe`）

原版 `CardHighlight__ToggleCombatPreviewHighlight.c` 全体（本轮亲读）⇒ 我们的分支（`:33016-33053`）：

| 原版 | 我们的等价 |
|---|---|
| `:85-92` `formUnityAbility != 0` ⇒ `GetActiveAbilityDamage(other)` | `RuleCore.ActiveAbilityDamageEntries(Ctx, other → self)` |
| `:53-62` `isAttacker != 0` ⇒ `[GetCombatDamageWithType(other, changeAttackType ? other.attackType : self.attackType)]` **一条** | `[otherU.IsRemnant ? 0 : RuleCore.FieldAttack(Ctx, otherSide, otherU, ranged)]` |
| `:53-81` 其余 ⇒ `EnemyReturnsAttack` + 主伤害 + **other 的**星镖 + **本卡的**标记光 | `RuleCore.AttackDamageEntries(Ctx, other → self, ranged)`（引擎侧的 `[星镖, 主伤害, 标记光]`，与准星同一条算式） |
| `:98-102` 判死第一实参 = **自己** | `RuleCore.WouldKillByEntries(selfU, entries)` |
| `:141` 预览支点亮 **other 的** `attackType` | `info.AttackTypeToHighlight = (int)other.OwnAttackType` |
| `:132-134` other 是计策卡 ⇒ 不点亮 | `info.OtherIsTactic = !other.Data.isUnit`（与 `CardInteraction.IsTactic` 同一句，⛔ 没另造映射表） |

⛔ **不重写伤害算式**：两个聚合口 + `FieldAttack` + `WouldKillByEntries` 全是引擎侧既有口（两处写同一条规则 = 迟早不一致）。
⛔ **算不出就 `Valid=false`**（机器会收掉预览并出声一次，不假装「不会死」）。

### 2.4 顺带改准的一处驱动层口径（B8_AW §六·2）

`CardHoldPeek`（`:16422-16443`）原来是「按着手牌 **或** 按着棋盘格」，**两档都不是原版**。
判据 `CardScript__OnTouchDown.c:34-59` 只有两档会 `ShowDeckSize`：`case 0`（牌库里的卡，两侧）·
`case 1` 且 `+0x40 == 0`（**非我方**）。
⇒ 现在 = 「按住 且 指针压在**我方牌堆**上」（`HitMyDeckPile`）。两处**结构上够不到**、如实记在 doc 里：
敌方牌堆没有命中口（`HitMyDeckPile` 是我方几何口）、敌方手牌没有「被按下」这条输入（`CardInteraction` 只管我方）。

---

## 三、改了哪些文件哪些行

`CardPresentation/Battle/BattleDriver.cs`：**32880 → 33074 行（+194）**

| 位置 | 内容 | 行数 |
|---|---|---|
| `:14341-14344` | **就地压缩 4 行注释为 3 行** + 插入 `TickBoardHover(world);` | **net 0** |
| `:14423` / `:14427` | ⑤ 段两句各追加 `BoardCardTouchDown(...)`（原地加实参调用） | **net 0** |
| `:16405-16421` | `CardHoldPeek` 的 doc 重写成原版口径（17 行 → 17 行） | **net 0** |
| `:16436-16437` / `:16440-16441` | 注释与体就地改写（旧近似 → `HitMyDeckPile`） | **net 0** |
| `:32880-32928` | 段头注释 + `_hoverBoardCard` / `_hoverBoardSide` / `HoverBoardCardView` + `TickBoardHover` | +49 |
| `:32930-32951` | `BoardCardTouchDown` + `BoardHighlightOf`（惰性注入探针） | +22 |
| `:32953-32992` | `BoardTouchInput`（原版读数映射 + 机器上那几个自有读数） | +40 |
| `:32994-33053` | `BattleBoardPreview`（探针本体 + 判据注释） | +60 |
| `:33055-33064` | `TryLocateBoardView` | +10 |
| `:33066-33072` | 两个自检转发口 `TickBoardHoverForTest` / `SimulateBoardCardTouchDown` | +7 |

**漂移影响**：新增全部落在类尾（`>= 32880`），而全账指向本文件的最大锚点 = **32866**
（`战斗逻辑驱动.tsv` 的 `TryUsingBattleWaystone`）⇒ **0 条漂移**。就地改写的四处都 `net 0`。
机检复核：`check_ledger_points` 全账里 `BattleDriver` 的漂移仍是**原有那 2 条**
（`OnApplicationPause` 账 32765 / 实际 32802、`ToggleManaAccumulation` 账 32696 / 实际 32725 —— 都 < 32866，
是**我这轮之前别的写手**在文件中段插入造成的，与本次无关）。
`CRLF 33074 / LF 33074 / 裸 CR 0`（二进制复核）。

---

## 四、断言草稿（请转交 `Editor/BattleScene.cs` 的写手）

```csharp
// 前提（三条都要）：玩家回合 + !InMulligan + Ctx.Turn > 0 + 没选中单位(_selectedSlot<0) + 选牌窗关着。
// 注：3D 档喂坐标必须先过 `b.PointerAtWorldForTest(card.transform.position)`（同 W61 那条）。

// A1 · 悬停 0→2，离开回 0（判据 CardScript__TryCardHighlight.c:27-28 / OnTouchExit.c:42-49）
//   card.SetHighlight(CardHighlightState.Normal);                       // 摆回 regular
//   b.TickBoardHoverForTest(b.PointerAtWorldForTest(card.transform.position));
//   Check(b.HoverBoardCardView == card && card.BoardHighlightStateId == 2);
//   b.TickBoardHoverForTest(屏幕外/空处);
//   Check(b.HoverBoardCardView == null && card.BoardHighlightStateId == 0);
//   灭自证：把 `if (hv == _hoverBoardCard) return;` 改成恒 return ⇒ 第一条必红。

// A2 · 按下 regular ⇒ 2（判据 CardScript__OnTouchDown.c:61-66）
//   card.SetHighlight(CardHighlightState.Normal);
//   b.SimulateBoardCardTouchDown(b.MyIndex, slot);
//   Check(card.BoardHighlightStateId == 2);
//   同态早退：再按一次 ⇒ Describe() 里 rimRGB 不变（原版 ChangeState.c:14-16 一个像素都不改）。

// A3 · 离开 4 ⇒ 3（**⛔ 不是 0**）（判据 OnTouchExit.c:40-50）
//   card.SetHighlight(CardHighlightState.SelectedTargetInBoard);        // 4
//   b.TickBoardHoverForTest(空处);
//   Check(card.BoardHighlightStateId == 3);                             // 回绿 potentialTargetInBoard
//   判别式：把 MapToView(selectedTargetInBoard) 改成 Normal ⇒ 本条与 A1 一起红。

// A4 · 闸（判据 TryCardHighlight.c:19-24）—— 选牌窗开着时悬停**不改态**
//   b.Choose.Open(...); card.SetHighlight(CardHighlightState.Normal);
//   b.TickBoardHoverForTest(卡位置) ⇒ BoardHighlightStateId 仍 0（ChoosingResolutionCard 早退）；
//   关窗后再喂一次 ⇒ 2。
```

---

## 五、没查清的部分（⛔ 没拿猜测顶）

| 项 | 状态 | 卡在哪 |
|---|---|---|
| `EnemyReturnsAttack` 的五个 trait 闸（`0x14` = 20 · `0x208` = 520 · `0x3de` = 990 · `0x488` = 1160 · `0x460` = 1120） | **没锚定名字** | 都是数据侧 trait id（同族 `0x4b0` = 1200，B8_AW 也只记了裸 id）。⇒ 反击支**恒按「有反击」**（可能多报「会死」），已在代码注释与账里写明 |
| `GetCombatDamageWithType` 的 trait `0xe6`(230) 支路（读 `+0xf0` 那个值）· 残骸的 `+0x66` | **没查清** | `+0xf0` / `+0x66` 的语义没读；现在只按 `IsRemnant` 判 0 |
| **悬停只在玩家回合更新** | **已知偏离** | 调用点在 `DrivePlayerTurn` 内（原版 uGUI 事件不分回合）。挪到 `Update` 要动 `:1385x` 那段、漂移面很大 ⇒ 建议单开一批 |
| **手牌拖拽时的 `cardTracked`** | **没接** | 原版 `StartMovingCardInHand` 那条（= 拖伤害牌悬停敌方单位看「会不会死」）的输入源在 `CardInteraction`（白名单外） |
| `SyncBoard` 整盘 `SetHighlight(Normal)` 会抹掉悬停态 | **已知** | 要等指针换卡才恢复；原版靠 `UpdateFigures` 每帧重算那几层，我们还没有那一层 |
| `mulliganFinished(12)` 这一档 | **我们没有** | `InMulligan` 只覆盖 11（`Ctx.MulliganOpen`）；12 是转瞬态 |
| `IsAnimatingCombat(+0x248)` · `waitingToResolveUseWaystoneFlag(+0x419)` | **无逐卡 / 无等价物** | 前者恒 false（= 原版常态）；后者恒 false ⇒ `TryCardHighlight.c:80-82` 那道 early-return 不会触发（只在灵魂石结算那一瞬有意义）。两处都在代码里打了 `🔶 自己设计` |

---

## 六、顺手发现（都没越界改，按铁律 13·4⑧ 报上来）

1. 🔴 **`OnTouchExit` 的「收预览」支会清掉敌方卡常显的打法底光**：`OnTouchExit.c:161-168` 给**非我方**卡写
   `HighlightAttackTypes(None)`；而驱动层是「每张棋盘卡**常显**自己的打法底光」（`ApplyOwnAttackType` 在
   `HighlightTargets` / `ClearTargetHighlights` / `SyncBoard` 里刷）⇒ 悬停一次敌方卡，它的底光会灭到下一次整盘刷新。
   我**在派发点就地补回**（`TickBoardHover` 里 exit 之后调 `ApplyOwnAttackType(oldSide, oldSlot, old)`，`:32918-32923`），
   但**根因是两条口径的接口**（我们还没有 `UpdateFigures` 那一层）⇒ 建议记进 `战斗界面_HUD` 域另账。
2. ⚠️ **`CardBoardHighlight.ToggleCombatPreviewHighlight` 的 warning 措辞与实际不符**（Core 侧，不是我的文件）：
   探针**接了但拒答**（`Valid=false`）时，它仍然打「探针**没接线** ⇒ 原版这一段**没有执行**」。
   两种情形应该分开（`PreviewProbe == null` vs 探针拒绝）。建议转给 `Core/CardHighlight.cs` 的写手。
3. ⚠️ **`CardData` 是 struct** ⇒ 任何 `other.Data != null` 恒真、写出来**编译期**就 `CS0019`（我当场撞到一次）。
   与记忆里 `JsonUtility` 把 null 物化成整棵子树那条同族：**判「有没有数据」不能靠 `!= null`**。
   （判据 `<see cref="CardView.cs:29"/>` `public struct CardData`）
4. ⚠️ **`check_ledger_points.py --help` 不解析参数、直接跑全账**（只读，所以没损失）。与 2026-10-26 那条
   「写盘脚本不带显式动作必须什么都不做」同族 —— 别人想「看用法」时会以为它只打印帮助。
5. ✅ `BattleManager__ShowDeckSize` 的 `isPlayerY` 死参（B8_AW §六·1）本轮**复核成立**：方法体一次都没读它、
   两侧一起点亮。`CardHoldPeek` 的改动没有碰这个（两侧一起亮那条不受影响）。

---

## 七、给调度台的请求

1. 🔴 **同步点请跑 `BattleScene.Run` 一条读数**（§四 A1 / A2 / A3 任两条即可）。
   我这三行 A 的「我们侧」是**静态推导**（读数口 + 调用链），**没有实测值** ——
   本轮红线是子代理不跑 Unity、项目自检按用户 2026-10-25 口径暂停。**若实测与推导不等，请把这三行退回 B。**
2. **合并补丁**：先 `python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py`，再
   `python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_BB_`，合并后把 `B8_BB.tsv` 移进 `_parts/_merged_B8_BB/`。
3. **另派（判据缺口）**：把 `0x14 / 0x208 / 0x3de / 0x488 / 0x460 / 0xe6 / 0x4b0` 这几个 trait id 锚定成关键词名
   （补进 `判据/CardTrait词条权威表.md`）。锚定之后「反击支」与「`0x4b0`」这两处才能真正判死。
4. **另派（`Hand/CardInteraction.cs`）**：把「正在拖的那张手牌」接成 `TrackedCard`（原版
   `StartMovingCardInHand.c:5`）—— 那才是 `OnTouchEnter` 预览支真正被用到的场景（拖伤害牌悬停敌方单位看会不会死）。
5. **两条转派 Core**（`Core/CardHighlight.cs` 的写手）：§六·1 的底光冲突口径 · §六·2 的 warning 措辞。
6. §五 第 3 条（悬停只在玩家回合）要不要做：建议**单独一批**，因为要动 `Update` 的 `:1385x` 那段（漂移面大）。
