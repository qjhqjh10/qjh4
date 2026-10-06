# WB4 · 输入触发沿（A462 实现 + A658）

> 本件 = 写手代理 WB4。**没跑 Unity 自检**（简报口径：A 表清完再跑）· ⛔ 没动 git · ⛔ 没改两张正本。
> 类型检查（`TMPDIR=/tmp/wf_wb4 bash 工具/typecheck.sh`，共 **6 遍**）**运行时报 0 / 编辑器报 0**（最后一遍落地后，见 §九）。
> ⚠️ 落地时刻全库有**别的代理在跑**（同一分支上一堆 M 文件；`BattleDriver.cs` 里本来就有别人未提交的改动
> —— `HookUnitTweenResolvers` / `kindNet` / `HudExtraWorldPos` 等，**都不是本件的**）。
> ⚠️ 若本段交付后 `BattleDriver.cs` 又被别人改过，**先跑 `git diff --numstat`** 看行尾有没有翻、
> 再看 `_swallowNextRelease` / `PollInputEdges` 那几处还在不在。

**白名单**（只碰了这 4 个）：
`Battle/BattleDriver.cs` · `Battle/AttackSelector.cs` · `Battle/ChatPopupPanel.cs` · `Editor/BattleScene.cs`

---

## 一、结论

| 件 | 结论 |
|---|---|
| **A462 的 12 处「改抬起」** | ✅ **全改了**（逐处现读定位）。机制 = 新加**两条沿**（`PollInputEdges` + `_downEdge`/`_upEdge`），**没删** `ClickedThisFrame()` —— 它的语义一个字没改，只是「谁还在用它」变了。 |
| **12 处之外的第 13 处** | 🔴 **`HandleChatPopup` 里那颗 `ChatButton`（现读 `:2646`）也改了** —— 它是 A462 那 **15 处代码调用**之一，但**不在简报表的 12 行里**（表的第 7 行 `SettingsClickAt` 没有调用点，两边错开了一格）。判据同为 `EverguildButton` ⇒ 抬起。**如实报出来，请调度台裁**。 |
| **1 行保持按下沿（`#11` 日志面板背板）** | ✅ 保持，并就地写清判据（`EventTrigger eventID 2 = PointerDown`）。 |
| **半行保持按下沿（`#10` 条外关闭）** | ✅ 保持。 |
| **1 行改悬停（`#3` 攻击选择器）** | ✅ 改了，含 **0.1s 安全窗** + 「一次进入只发一次」（原版 `sendInput(+0x90)` / `isInputOverSent(+0x91)` 两格）。⛔ 拖拽流程没碰。 |
| **1 行拆两半（`#10`）** | ✅ 拆了：条外关闭走**按下**、选台词走**松手**。`ChatPopupPanel.SetPointer(world, down)` **拆成 `PointerDownAt` / `PointerUpAt`**。 |
| **A658** | ✅ 做了：入场动画改成**累计视口拖拽位移**驱动（`TouchInputManager.TouchDragDeltaViewport`），并把两条会过期**老断言**就地订正。 |
| 🔴 **简报之外我必须加的一件** | **`_swallowNextRelease`（「同一次按住的松手沿要吞掉」）** —— 不加就是**我这一改引入的新 bug**（点日志钮 / 点聊天钮时面板会「闪一下又开回来」）。理由见 §三·4，**处置请调度台复核**。 |
| 断言 | **59 条 `Check`**（含十几条「（前提）」）落在 `Editor/BattleScene.cs` 的 `★ A462 / A658` 段（排在 A463 段**之后**，现读 `:11247-11725`）。每条带 🧨 改坏法。 |

---

## 二、逐处改动表（分类结论 · 上表行号 vs 现读行号 · 改前 · 改后）

> 「上表行号」= 简报里那张表给的号（也就是 `WA462_输入入口分类.md` 的现读值 + 本批新增的偏移）。
> 「现读」= 我这一轮**grep 现读**的号。⚠️ 简报自己就写着「整体滞后」—— 实测**又滞后了**（A463/W-B1/W-B2 那几段插在上面）。

| # | 上表 | **现读** | 改前 | 改后 | 判据（组件/入口） |
|---|---|---|---|---|---|
| 1 | `Update:4993` 多卡窗 | **`Update:5057` → `TickMultiCards():5125`** | `bool tapped = ClickedThisFrame();` | `ReleasedThisFrame()`（抽成 `TickMultiCards()`，**判据只有一份**） | 原版 `BackgroundCloseButton`(`IPointerClickHandler`) + `Close` 钮 |
| 2 | `Update:5008` 放大窗 | **`Update:5065` → `TickCardDisplayClick():5142`** | `… && ClickedThisFrame() && HandleDisplayWindowClick(…)` | `… && ReleasedThisFrame() && …`（抽成 `TickCardDisplayClick()`） | `BackgroundCloseButton` / `EverguildButton` / `UIGenericEventCatcher` |
| 3 | `DrivePlayerTurn:5145` 选择器 | **`:5241`** | `if (ClickedThisFrame()) { … CommitCommand(hot) … }` | **悬停即发**：`if (hot != None && selector.HoverPickReady) { selector.MarkHoverPicked(); CommitCommand(hot); }`；槽外取消那半**保持按下沿** | `CardDisplayAttackTypeButton` 只实现 `IPointerEnter/ExitHandler`；`__Update.c:20-30` 三格 |
| 4 | `DrivePlayerTurn:5175` | **`:5289`（③④）+ `:5316`（⑤）** | 一道 `if (!ClickedThisFrame()) return;` 管 ③′/③/④/⑤ | **拆成两条沿**：`if (ReleasedThisFrame()) { ③′/③/④ }` + `if (!ClickedThisFrame()) return;`（⑤）；**并且 ⑤ 加了「正在等选目标时不许记」的闸**（理由见 §三·5） | ③ = `EverguildButton`(`TurnBtn`)；④ = `CardCollider.IPointerClickHandler`；⑤ = 我们自己的中间态 |
| 5 | `HandleSettings:2325` | **`:2330`** | `if (ClickedThisFrame() && !captured) SettingsClickAt(…)` | `if (captured) _pressCaptured = true;` + `if (ReleasedThisFrame()) SettingsClickAt(…)` | Resign/Difficulty/Close = `EverguildButton`/`Button`；Auto Zoom = `EverguildToggle` |
| 6 | `HandleSettings:2330` 设置钮 | **`:2337`** | `if (ClickedThisFrame()) _settingsPanel.Show();` | `if (ReleasedThisFrame()) …` | `BattleHud.settingsButton`(`EverguildButton`)，`BattleHud__Awake.c:48-55` 绑 `m_OnClick` |
| 7 | `SettingsClickAt:2338` | **`:2353`**（方法体） | 无调用点 | **一个字没改**（它由 #5 那条闸控制；A445 已经把 Auto Zoom 挪进这条链） | —— |
| 8 | `HandleOffensiveButton:2369` | **`:2378`** | `if (!ClickedThisFrame()) return false;` | `if (!ReleasedThisFrame()) return false;` | `BattleHud.offensiveCardButton`，`BattleHud__Initialize.c:50-57` 绑 `m_OnClick` |
| 9 | `HandleCameraResetButton:2546` | **`:2557`** | 同上 | 同上 | `BattleHud.resetCameraZoomButton`，`__Initialize.c:62-66` |
| 10 | `HandleChatPopup:2612` **（拆两半）** | **`:2634-2642`** | `if (ClickedThisFrame()) ChatClickAt(WorldPointer());` | 按下 → `_chatPopup.PointerDownAt(wp)`（**只做条外关闭**）/ 松手 → `SpeakChatAt(_chatPopup.PointerUpAt(wp))` | 条外 = `CloseChatPopup` 的 `EventTrigger`(**2**)；6 颗 = `ChatPopupButton : EverguildButton` |
| 10b | （表里没列）`HandleChatPopup:2616` | **`:2646`** | `if (!ClickedThisFrame()) return false;` | `if (!ReleasedThisFrame()) return false;` | 那颗 `ChatButton` 也是 `EverguildButton`（见 §一 第 2 行） |
| 11 | `HandleBattleLog:2917` | **`:2969`** | `if (ClickedThisFrame()) { _logPanel.Hide(); … }` | **保持按下沿**；只加了 `_swallowNextRelease = true;`（见 §三·4） | `shade` = `EventTrigger` `eventID **2 = PointerDown**` |
| 12 | `HandleBattleLog:2922` | **`:2978`** | `if (ClickedThisFrame()) ShowBattleLog();` | `if (ReleasedThisFrame()) …` | `ShowCemeteryBtn` = `EverguildButton`，`m_OnClick → ShowCemeteryLogBtn` |
| 13 | `HandleReplayBar:2832` | **`:2873`** | `… !ClickedThisFrame() …` | `… !ReleasedThisFrame() …` | 四颗全是 `EverguildButton` |
| 14 | `HandleMulligan:3050` | **`:3109`** | `if (ClickedThisFrame()) _mulligan.HandleClick(…)` | `if (ReleasedThisFrame()) …` | `EverguildButton` / `UnityEngine.UI.Button` 两族 |
| 15 | `HandleChoose:3808` | **`:3871`** | `if (ClickedThisFrame()) _choosePanel.HandleClick(…)` | `if (ReleasedThisFrame()) …` | `Button`(`ContinueButton`/`HideChooseButton`) + `EverguildButton`(`selectButton`) |

**机制落点**（`BattleDriver.cs`）：
`_downEdge`/`_upEdge`/`_held`/`_pressCaptured`/`_swallowNextRelease`（`:9200-9230`）·
`PollInputEdges()`（`:9238`，`Update` 的**第一句**，在 `Ctx == null` 那道闸**之前**）·
`ClickedThisFrame()`（`:9248`）/ `ReleasedThisFrame()`（`:9258`）/ `LogClickBeat()`（`:9266`）。

### 2·1 ⚠️ 与简报「12 行」的字面差异（请裁）
1. **多改了 1 行**（`HandleChatPopup` 的 `ChatButton`，现读 `:2646`）—— 见 §一。
2. **`#4` 那一行改成了「两条沿并存」**（不是整行换沿）—— 简报的纪律 3 要求 ⑤ 留在按下帧，而 ⑤ 原来是在同一道闸**下面**；不拆开就没法既改 ③④ 又保住 ⑤。
3. **`#4` 的 ⑤ 多了一道闸**（`_selectedSlot < 0 || _command == None`）—— 不加的话 ④（改到松手之后）**永远轮不到**（见 §三·5）。
4. **`#3` 的「槽外取消」那半仍然是按下沿**，并加了 `_swallowNextRelease = true`。

---

## 三、三条落地纪律各自怎么遵守的（尤其 `_clickLatch` 两支都记没记）

### 1. ✅ `_clickLatch` 没删，而且**两条沿都记**
- 原来那个 `_clickLatch` **换成** `_held`（同一个意思：上一帧按着没有），两条沿都由它算。
  `ClickedThisFrame()` 的**对外语义一个字没改**（按住状态的上升沿、一次按住只给一次）。
- `ClickLog.Begin/Hit` 那段**抽成 `LogClickBeat()`**，**按下沿和松手沿各调一次**
  （`ClickedThisFrame:9252` / `ReleasedThisFrame:9262`）。
  ⇒ 「真实点击记录」（用户 2026-09-24 要的）**不会**因为改了沿而缺一半。
- **有断言**（§六「一·b」，2 条）：用 `ClickLog.OverridePath` 指向 `_tmp_view/battle/_wb4_click_probe.txt`
  的**临时探针文件**（⛔ 不碰玩家那份真日志），两条沿各走一次、各 `End()` 一次，
  比 `ClickLog.LastBlock` 里有没有 `来源 BattleDriver`。
  做法同 `Editor/ShellScene.cs:3274-3288`（那边就是「工具本身也要先验证」的先例）。

### 2. ✅ `PointerDown()`（现读 `:5310`）一个字没动
- 它是「按住」，`BoardPress(world, PointerDown())`（`:5298`）与技能卡面板 `SetPointer(…, PointerDown())`（`:5301`）
  要的就是它。
- 新的松手沿是**另写的一个函数**（`ReleasedThisFrame()`），**没有**去改 `PointerDown()` 的语义。
- ⚠️ **顺手发现（没动）**：`PointerHeld()`（`:9290`）与 `PointerDown()`（`:5310`）**函数体仍然逐字相同**
  （WA462 §六·4 就建议合并）。本件只给 `PointerHeld()` 加了一个自检钉死口，**没合并**。

### 3. ✅ `#4` 的 ⑤ 支（`_pressSlot`）留在按下那一帧
- 拆沿之后，⑤ 仍在 `if (!ClickedThisFrame()) return;` 之后（现读 `:5316-5330`），**松手那一帧走不到它**。
- `BoardPress`（`:5303-5322`）的「`held == false` 那一帧」分流**没动**。

### 4. 🔴 **纪律之外我必须加的一条：`_swallowNextRelease`（「同一次按住的松手沿要吞掉」）**
**不加就是新 bug**，所以加了，并把理由写进代码注释。它**只**在这三处置位：
1. `HandleBattleLog` 面板开着那一支（按下就把面板关了）→ 现读 `:2969`
2. `HandleChatPopup` 的条外关闭那一半（按下就把面板关了）→ `:2637`
3. 攻击选择器的槽外取消那一半（按下就把选择取消掉了）→ `:5253`

**为什么必须有**：原版那三处的「按下」入口都是**全屏最上层**的节点（`shade` / `CloseChatPopup` / 选择器底板），
底下那颗 HUD 钮**根本收不到那次按下** —— `StandaloneInputModule` 只把 click 发给 `pointerPress`
（按下那一刻命中的那一件，`.../StandaloneInputModule.cs:208-217`）。
不吞的话：**按住日志钮 = 关日志，同一次按住的松手沿又把它打开**（画面闪一下 = 看着像「点了没反应」）。
`Editor/BattleScene.cs` 里有**两条断言专门钉它**（日志那条 / 聊天那条）。

### 5. 🔴 `#4` 的 ⑤ 加的那道闸（不加 ⇒ ④ 永远轮不到）
把 ④ 从按下沿改到松手沿之后，出现一条**新的依赖**：
- 按下那一帧若仍然记 `_pressSlot`，松手那一帧**最先**跑的 `BoardPress(world, PointerDown())`（`:5298`）
  就会 `_pressSlot >= 0` ⇒ `ToggleUnitCard` + **`return`** ⇒ `Update` 提前返回 ⇒ **④ 一辈子轮不到**、
  「点目标发动攻击」直接坏掉（而且是**静默**的：只会看到「点了没反应」）。
- 所以 ⑤ 加了 `if (_selectedSlot < 0 || _command == AttackKind.None)` —— 判据与 ④ 那一支**共用同一个条件**。
- 断言：§六 的「八」那一组（按下不结算 / 松手才结算）。

---

## 四、A658 的改法（速度驱动）

**判据**（亲读 `d:/2/tools/decomp_full/`）：
```
AttackTypesButtonsController__MoveButtons.c:
    accumulatedDrag(+0x50/+0x54) += TouchInputManager.TouchDragDeltaViewport(+0x18/+0x1c)
    t = clamp01( −accumulatedDrag.y ÷ accumulatedDragForMinDistance(+0x30) )
AttackTypesButtonsController__OnEnable.c:  把 +0x50/+0x54 写回静态零向量（= 清零）
```

**改了什么**（`Battle/AttackSelector.cs`）：
- **删掉** `const float EnterAnimSeconds = 0.085f`（按时间驱动的那一半），原地写清「就地更正 + 判据」。
- 新增 `Vector2 _accumulatedDrag`（原版 `+0x50`）+ `AccumulatedDrag`（自检读）。
- `Tick(float dt)` 改成：`dt` **只喂安全窗**，动画走 `PushDrag(TouchInputManager.TouchDragDeltaViewport)`。
- 新增 `bool PushDrag(Vector2 dragDeltaViewport)` —— **原版那两句的落点（判据只此一处）**：
  零位移直接返回（**停住就冻住**）；否则 `t = Clamp01(−acc.y / DragThreshold01)`，变了才重排。
- 新增 `TickForTest(float dt, Vector2 dragDeltaViewport)`（批处理里没有输入层，`TouchInputManager.Update` 不跑）。
- `Show()` 里 `_accumulatedDrag = Vector2.zero`（照原版 `OnEnable`）。

**顺带订正的两条老断言**（`Editor/BattleScene.cs` 5b 段，现读 `:3203-3220`）：
原来写 `driver.Selector.Tick(0.04f)` / `Tick(1f)`（「按时间推进 0.04s = 半程」）—— **驱动量错了**，
已改成 `TickForTest(0.04f, new Vector2(0f, -0.0425f))` / `TickForTest(1f, new Vector2(0f, -1f))`，
并把注释就地更正（保留更正痕迹）。

**⚠️ 如实标注的遗留**：`DragThreshold01`（0.085）**仍然兼着我们那条「拖多远弹出选择器」的阈值** ——
原版那两件事**不是同一个数**（0.085 是**动画分母**，真阈值没定死），这条偏离**2026-09-29 就记着**，
本件**没动**（不属于 A658）。

---

## 五、`BattleDriver.cs` 那 3 处注释订正

| 原注释 | 现在 |
|---|---|
| `:2312`「⚠️ 只能在这里调 `ClickedThisFrame()` —— 它是 latch…」 | 改成「⚠️ 边沿只能在这里**各耗一次**（`ReleasedThisFrame()` / `ClickedThisFrame()` 都是给一次就没了）…」 |
| `:2541`「…**先看命中区、再耗 `ClickedThisFrame()` 那个 latch**」 | 改成「…**先看命中区、再耗那个抬起沿**」（并补 `EverguildButton` 判据） |
| `:2607`「（`ClickedThisFrame()` 是 latch，只能在这里耗一次…）」 | 改成「（两条沿各自只能在这里耗一次…）」，并**加了一整段**「这一处是两种不同的触发沿」的说明 |
| （额外）`DrivePlayerTurn` 里「放在 `ClickedThisFrame` 之前」 | 改成「放在下面的两条沿之前」 |
| （额外）`PointerHeld()` 的 doc | 补了「它和 `PointerDown()` 是同一份实现的两个名字 + 有个自检钉死口」 |

---

## 六、断言清单（断什么 · 怎么分辨两态 · 改坏法 · 落点）

**落点**：`Editor/BattleScene.cs` 的 `★ A462 / A658` 段，**紧排在 A463 段之后**（现读 `:11247-11725`），
`Debug.Log(P + "--- A463 段结束（下面还有别的段）---")` 那一行的**下面**。**共 59 条 `Check`**（含十几条「（前提）」）。

**两态是怎么造出来的**（这是本段的核心手法）：批处理里没有鼠标 ⇒ `PointerHeld()` 恒 false，
**两条沿一条都验不了**。所以驱动层加了三个**自检口**（`BattleDriver`，⛔ 生产路径一个都不调）：
`PointerHeldForTest`（钉住「按住/松手」）、`PointerWorldForTest`（钉住指针世界坐标）、
`PollInputEdgesForTest()`（= 走一帧的边沿）。三个都只是**把输入源钉死**，判据本身照旧。

| 组 | 条数 | 断什么（两态怎么分） | 🧨 改坏法 |
|---|---|---|---|
| 一 · 两条沿本身 | 8 | 按下那一帧**不给**松手沿 / 松手那一帧**给**；两条沿各自「一次按住只给一次」；按住不放的第 2 帧**都不给**；松手之后**不补发** | 把 `_upEdge` 换成「按住」/ 去掉 `_downEdge=false` / 去掉 `_upEdge=false` |
| 一·b · `ClickLog` 两条沿都记 | 2 | 按下沿那一下进记录 / **松手沿那一下也进**（`ClickLog.LastBlock` 里有 `来源 BattleDriver`） | 只在按下那一支调 `ClickLog` |
| 二 · 滑块接住 | 2 | 被滑块接住的那次按住：**松手不算点击**（前提：`PressCapturedForTest`） | 删 `_pressCaptured` ⇒ 拖完音量条会顺带点掉别的钮 |
| 三 · 设置钮 + Auto Zoom | 4 | 设置钮**按下不弹 / 松手才弹**；面板里 Auto Zoom **按下不翻 / 松手才翻** | 换回 `ClickedThisFrame()` |
| 四 · 日志面板一对（#11 vs #12） | 4 | 日志钮**按下不开 / 松手才开**；面板开着时**按下就收**（#11 保持）；**同一次按住的松手沿不会又打开它** | ①换回按下沿 ②把 #11 也改成抬起 ③删 `_swallowNextRelease` |
| 五 · ChatPopup 拆两半（#10） | 4 | 条外**按下就关** + 松手不重开；按在台词钮上**按下不说 / 松手才说**（`LastClicked`） | 删 `_swallowNextRelease` / 两半合并 |
| 六 · 多卡窗（#1） | 2 | **按下不关 / 松手才关** | 换回 `ClickedThisFrame()` |
| 七 · 选择器悬停（#3） | 5 | 刚弹出（安全窗内）**压着也不发**；**刚要进入那一格 ⇒ 安全窗重开**；窗过了才发（`CommitCommand` 真的发生） | ①去掉安全窗 ②改回 `if (ClickedThisFrame())` |
| 八 · 选目标（#4 的 ④） | 2 | 按下那一帧**不结算** / 松手那一帧**结算**（`Resolve` 会 `ClearSelection`） | 把 ④ 挪回按下沿 |
| 九 · A658 | 4 | 弹出即清零；**停住不拖 ⇒ 冻住**（喂 0.5s 时间 + 零位移）；**往下拖不推进**；往上拖半个阈值 ⇒ ≈0.5；拖够 ⇒ 夹 1 | 换回 `_enterT += dt / 0.085f` / 把符号反成 `+acc.y` |
| 十 · 源级（结构） | 1 | `BattleDriver.cs` 里 `ClickedThisFrame()` 的**代码调用点只剩 4 处**（逐行打出是哪 4 行） | 多一处 = 有该改的没改；少一处 = 把该按下的改掉了 |

**⚠️ 弱断言自查**（派活必查行三条）：
- ① **不自证**：每条的期望值都写成**原版那个数/那条规则**（0.1s / `PointerDown=2` / 只看「两态不同」），
  **没有**拿 `BattleDriver` 自己的常量当期望。
- ② **能分辨两态**：每条都是「同一件事在**按下帧**与**松手帧**给出**相反**结果」；没有一条只断「发生了」。
- ③ **没有 `!RectOfUnion` / 「一个 quad 都没有」式断言**。
- ④ 每条标了能分辨两种状态（按下触发 vs 抬起触发）—— 源级那条**不是**行为断言，已在 §七 说明它顶不了谁。

---

## 七、没查清 / 没做的（⛔ 不猜）

1. 🔴 **`#2`（放大窗）/ `#13`（回放条）/ `#14`（换牌）/ `#15`（选牌）没做「行为」两态断言**，只被 §六「十」那条**源级断言**覆盖。
   原因：那四个面板在本段能拿到的状态下**开不起来 / 开起来也造不出一次成功的点击**
   （放大窗那条还卡在 `SameFrame(_cardWinOpenedFrame)` 上：批处理里 `Time.frameCount` 不推进，
   背景点击会**恒**被那道「同帧防打架」吃掉 ⇒ 假红风险）。**如实记，没替它们发明口径。**
   补充：`#13` 那条 `HandleReplayBar` 还有一层 —— `ReplayClickAt` 要求 `HolderVisible`，
   而普通对局里回放条**本来就不显示**（原版 `ReplayHud.Setup()`），造一次成功点击要先开一局回放。
2. ~~🔴 **ClickLog「两支都记」没有断言**~~ → ✅ **已补**（§六「一·b」2 条，见 §三·1）。
3. 🔴 **`#3` 在【拖拽语境】下的完整时序仍然没验**（WA462 §五·2 就标了）。
   本件只改触发沿：**拖拽流程一个字没动**。⚠️ 由此带来一条**未验的交互**：悬停即选中之后
   `CommitCommand` 会**把选择器收起**（原版那一下只是把 `attackType` 发给控制器）⇒
   「选中了之后还能不能改」我们与原版**可能不同**。**没查清，已如实记 —— 建议进真 Play 待验清单。**
4. 🟡 **`AttackTypesButtonsController.Initialize(param_2)` 注入的那个委托到底做到哪一步**没读
   （它决定「悬停发出去之后原版接着做什么」）。本件只照 `Update` 那三格做。
5. 🟡 **`EnemyInfoTouch`（原版唯一读 `TouchPressed` 那件：松手收小窗）我们没有对应件**（WA462 §六·2）。
6. 🟡 **「按下 vs 抬起」在**触摸**上的表现没验**：`PointerHeld()` 读的是 `Touchscreen…primaryTouch.press.isPressed`，
   语义与鼠标一致；但真机上「按下-抬起」之间的抖动我们没量过。
7. ⚠️ **本段的收尾面**：我摆过两个 `Ballista` 探针并清掉（同 5b 段做法）、Hide 了各个面板、
   把 `AutoZoom` 的内存态还原、把 `PointerHeldForTest`/`PointerWorldForTest` 置回 null（`finally` 里）。
   设置面板的显隐按进场时的状态还原。

---

## 八、顺手发现（⛔ 别自己顺手改 —— 一条都没动）

1. 🔴 **两份文档把 `EventTriggerType` 写错了一档**（WA462 §六·1 已记，**本件不在白名单，没改**）：
   - `资料/语音线_原版规格与ASR管道.md:329`：「`eventID 2 (**PointerClick**)」
   - `资料/战斗规格/战斗重建_0827/子代理读报_front交互层_0827.md:288`：同上
   **`2 = PointerDown`**（本机 `com.unity.ugui/.../EventTriggerType.cs:24`；`PointerClick` 是 **4**）。
   ⚠️ 这条错**正好落在 A462 的命门上**：照它读会把 `#10 / #11` **方向读反**。
2. 🟡 **`PointerDown()` / `PointerHeld()` 仍是同体的两个名字**（`:5310` vs `:9290`），建议谁动那一族谁合并。
3. 🟡 **`ClickedThisFrame()` 这个名字现在有点误导**：它现在**只剩 4 处**在用（都不是「点击」语义 ——
   两处是原版的 PointerDown、一处是槽外取消、一处是我们自己的中间态）。改名（如 `PressedThisFrame`）
   会牵动 4 处 + 2 个自检口，**本件没改**（怕撞别的在飞代理）。
4. 🟡 **`AttackSelector.Show()` 现在会把安全窗打满 0.1s** —— 这也会让「拖拽中弹出」的那一瞬间
   不会立刻误选（原版就是这个行为）。若真 Play 觉得迟滞，**先回来看这条**，别先改数。
5. 🟡 **`_swallowNextRelease` 是个新的全局状态**（本件加的）。它只被三处置位、只在一次按住的
   松手帧起作用，`PollInputEdges` 在「新的按下」与「松手帧」两处都清它。若将来有人把某处的
   「按下就把面板关掉」搬走，**要顺手把对应的 `_swallowNextRelease` 一起搬**（否则会吞掉一次无关的松手）。
6. 🟡 **`BattleDriver.E` 的 `SettingsPanel.PointerFrame` 那条路**（三根滑块）**没有被改沿波及**，
   本件的滑块断言正好把它当**对照组**用了（`_pressCaptured` 就是它置的）。

---

## 九、类型检查结果

| 轮次 | 结果 |
|---|---|
| 第 1 遍（驱动层两条沿 + 15 处换沿 + 悬停） | 运行时 **0** / 编辑器 **0** ✅ |
| 第 2 遍（`ChatPopupPanel` 拆两个入口 + `AttackSelector` A658） | **0 / 0** ✅ |
| 第 3 遍（`BattleScene` 老断言订正 + 新段落地） | **0 / 0** ✅ |
| 第 4 遍（新段重构：探针单位 / 悬停安全窗顺序 / 源级断言） | **0 / 0** ✅ |
| 第 5 遍（收尾：`MySideForTest` / `PollInputEdges` 提到 `Ctx==null` 之前 / 前提断言） | **0 / 0** ✅ |
| 第 6 遍（补 `ClickLog` 两条沿那组断言 + 订正 doc 注释，**落地后最后一遍**） | **0 / 0** ✅ |

⚠️ 本批有**别的代理同时在写**别的文件；上面 5 遍都是各自落地后立刻跑，**没有**出现过「错误集中在别人文件上」的情况。
⚠️ `git diff --numstat` 复查：`BattleDriver.cs 499/111` · `AttackSelector.cs 117/13` · `ChatPopupPanel.cs 31/12` ·
`Editor/BattleScene.cs 1171/6`（含本批别的代理的段）—— **没有整篇重写的迹象（行尾没被翻）**。
