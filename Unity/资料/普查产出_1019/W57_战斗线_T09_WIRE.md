# W57 · 战斗线 `T09` 步骤 WIRE —— 接线收口（Settings / 日志 / 聊天）

> **日期** 2026-10-20（第五十七会话）· **代理** `T09-WIRE`（执行）
> **判据入口** `W57_战斗线_T09_A3b2_SettingsPanel.md` §6/§7 · `W57_战斗线_T09_A3c_BattleLogPanel.md` §五·1 · `W57_战斗线_T09_A3e1.md` §六·1 · `W57_战斗线_T09_A3e3.md` §三·2 · `W57_战斗线_T09_A3e4.md` §三·2/§四（均为协调者中途追加）
> **白名单（只改了这两个）**
> `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（相对本波开始 **+169 / −37**）
> `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（相对本波开始 **+466 / −132**）
> **本波口径 = 只做不验**：未跑 Unity / 未起 player / 未跑任何 `*.Run` / 未跑 `_run_8_checks.sh`；只跑了秒级 typecheck。
> 未动 git、未改正本、未删文件、**未改** `SettingsPanel` / `BattleLogPanel` / `ChatPopupPanel` / `WfSlider` / `SkillPanel` / `CardDisplayWindow` / `AttackSelector` / `ChoosePanel` / `HudCanvas`。

---

## 一、八件逐项结论（落点 `文件:行号`，2026-10-20 现读）

### A. 接 `OnSkipTutorial`

**落点** `Battle/BattleDriver.cs:18888`

```csharp
_settingsPanel = SettingsPanel.Create(root, Forfeit, CycleAiDifficulty);
if (_settingsPanel != null) _settingsPanel.OnSkipTutorial = SkipTutorialFromSettings;
```

- `SkipTutorialFromSettings` 是 `private void ()`（`BattleDriver.cs:12142`），方法组直接赋给 `public Action`。
- 没有扩 `SettingsPanel.Create` 的签名（那个文件不在本件白名单；A3b2 §六·1 允许二选一）。
- ⛔ 没有用 `ApplyTutorialSkipForTest()` 顶（它跳过 `TutorialOverlay.TrySkip` 那道闸，语义不同）。
- **顺带把「接线」变成可测的**：
  - `Editor/BattleScene.cs:12066` 新增 `Check(sp.OnSkipTutorial != null)`（`null` = 没接 ⇒ 点了只 `LogWarning`）。
  - `Editor/BattleScene.cs:23867 / :23880`（§⑩-b 那条「真实链路」两态用例）由 `drv.SettingsClickAt(...)` 改成
    `spChain.ClickForTest("SkipTutorial Button")` —— 走的是 `OnSkipTutorialClicked`（先 `Hide()`）→ **回调口**。
    **回调没接时第二档（判死）必红** ⇒ D 半从此有断言兜底。

### B. 同步 Editor 断言（A3b2 §7 列的那些，行号现核）

| 段 | 旧断言（已删的手搓路径） | 新断言（uGUI 单一路径） | 为什么仍能红 |
|---|---|---|---|
| A445 形状（`BattleScene.cs:12238-12251`） | `!sp.PointerFrame(AutoZoomBoxWorldPos, true)` | 喂 `PointerFrame` 按下 + 松开 ⇒ **不翻值、uGUI 计数仍 0**；随后 `sp.ClickForTest("Auto Zoom Toggle")` ⇒ 计数 +1 且 `AutoZoom.Enabled` 翻 | 把动作搬回 `PointerFrame` ⇒ 第一条红；`BattleHudButton` 没挂 / 动作接错 ⇒ 第二条红 |
| 语言框（`:12418-12431`） | `PointerFrame(按下)==false` / `PointerFrame(松开)==true` | `UguiFeed` 只喂**原生按下/松开** ⇒ 语言不换、列表不开、计数不动；再 `ClickForTest("Language Selector")` ⇒ 计数 0→1、列表开、语言不变 | 按「按下就触发」写 ⇒ 计数会非 0 / 列表会开；框没挂钮 ⇒ `lfClicks0 == 0` 那条红 |
| 语言框命中区有界（`:12448-12456`） | 按下在框内、抬到框外（`PointerFrame` 两帧） | `HitLanguage(框心)` 真 且 `HitLanguage(面板空白)` 假 | 命中区写成整个面板 ⇒ 第二条红 |
| 开列表（`:12605-12611`） | `PointerFrame` 一对边沿 | `ClickForTest("Language Selector")` + 计数 | 同语言框那一行 |
| 点行（`:12682-12699`） | `PointerFrame(row, 按下/松开)` | `HitLangRow` 有界两条 + `LangRowClickForTest(0)`（计数 0→1、列表收起、语言 = `Loc.Languages[0]`） | 行没挂钮 / `ChooseLanguage` 不 `HideLangList` ⇒ 红 |
| 按下行抬到别处（`:12713-12722`） | 同上 | 把**原生按下/松开**喂给行那颗钮 ⇒ 不选、计数不动 | 「按下就选」写法 ⇒ 红 |
| 点空白（`:12720-12729`） | `PointerFrame(blank)` | `ClickForTest("Blocker")` + 计数 + `!LangListOpen && Visible` | Blocker 没建 / 没挂钮 / 动作接错 ⇒ 红 |
| 滑块「接住了」（`:13066-13081`） | `PointerFrame(HandleWorldPos, true)` | ① `PointerInputForTest != null` + 三张图射线靶；② `UguiFeed` 按下 ⇒ `DraggingForTest`；③ Drag 改值；④ 抬起 ⇒ 退出拖动 | 删 `SetupUguiInput` / `raycastTarget=true` / `EventTrigger` 条目 ⇒ 红 |
| 拖滑块改值（`:13074-13080`、`:13105-13110`、`:13147-13154`） | `PointerFrame(左端/右端/1/4/右沿)` | `UguiFeed` 的 **PointerDown/Drag/PointerUp**（与 `EventSystem` 派给 `WfSlider` 的同一条链） | 算式（`SetFromLocal`）一个字没改 ⇒ 期望值照旧；拖动状态位单独断 |
| 「松手之后不再拖动」（`:13094-13099`） | `!PointerFrame(面板外, true)` | ① `!DraggingForTest`；② **没按下**时喂 `Drag` ⇒ 值不变 | 删 `if (!_dragging) return;` ⇒ 第二条红 |
| ESC（`:12732`，未改） | `PointerFrame(f5, false)` 走每帧入口读 ESC | **原样保留**（`EscDown()` 未动，本件只把它当帧级入口） | —— |
| A462 滑块模态（`:21930-21938`） | 「滑块接住了这一按」 | 措辞订正为「面板模态接管」；`PressCapturedForTest` + 松手沿不算点击照旧 | `_pressCaptured` 删掉 ⇒ 松手那条红 |
| A462 Auto Zoom（`:21979-22001`） | 松手帧 `TickSettingsInputForTest()` ⇒ **翻值** | ① 按下帧不翻；② **手搓松手帧也不翻**（`SettingsClickAt` 产线不可达）；③ `sp4.ClickForTest("Auto Zoom Toggle")` ⇒ 翻 + 计数 +1 | 把动作接回手搓链 ⇒ ②红；没挂 uGUI ⇒ ③红 |

**额外顺带改的**（不在 A3b2 §7 清单里，但同一批语义已变 / 文档已失真）：
- `:12326-12343`「静音对手」两下点击：`SettingsClickAt` → `ClickForTest("ChatToggle")` + 计数（同一个 `ToggleMuteChatFromPanel`，动作等价）。
- `:12244` / `:12294` / `:12148-12163` 等注释里的「走 `SettingsClickAt` 真路」就地订正（铁律 5）。

### C. 日志行点击后关面板（真偏离）

**落点** `Battle/BattleDriver.cs:8100`（`TryClickLogRow` 内，`ShowLogCard` 之后）

```csharp
ShowLogCard(key, row);
_logPanel.Hide();          // = 原版 ClickCardLink 末尾的 HideCemeteryLogBtn（真关那一支）
return true;
```

- 判据 = `CemeteryManager__ClickCardLink.c` 末尾：`BattleManager__DisplayCard(...)` 之后紧跟
  `CemeteryManager__HideCemeteryLogBtn(param_1, 0)`；那一刻 `showingSliders(+0x90)==1` ⇒
  `HideCemeteryLogBtn.c` 走 `DOAnchorPosX(…, initialX−1200, 0.3s)` + `shade.SetActive(false)` + 悬停卡 `SetActive(false)`。
- 我们的 `_logCard` 挂在**面板壳** `_logPanel.transform` 下（不是 `_root`）⇒ `Hide()` 只滑走面板本体，卡**留在原地继续显示**
  —— 即「点行 ⇒ 面板关 + 卡弹窗开」。
- ⛔ 没有在这里调 `HideLogCard()`（那会把刚弹的卡一起收掉；原版收的是**悬停**卡）。
- 「吞掉同一按的松手沿」放在调用方 `HandleBattleLog`（`BattleDriver.cs:5010-5012`），自检直调 `TryClickLogRow`
  不会留下挂起的 `_swallowNextRelease`。
- 空行 / 无卡行 / `hideCemetery` 闸三个分支**不关面板**（原版 `GetCardFromUniqueId` 取不到就 `return`，不调 `HideCemeteryLogBtn`）。
- **断言**（`Editor/BattleScene.cs:4631-4642`）：`!log.Visible` + `LogSelectedRow == linkRow` + `LogHoverCardKey == wantKey`。
  🧨 删掉 `_logPanel.Hide()` ⇒ 第二条红；把 `ShowLogCard` 排到 `Hide()` 之后 ⇒ 第三条红（卡被收掉）。

### D.（协调者追加）聊天行点击后关气泡（真偏离）

**落点** `Battle/BattleDriver.cs:4561`（`SpeakChatAt` 开头）

```csharp
if (hit < 0) return false;
_chatPopup.Hide();                 // 原版 ClickChat：先 Hide，再 OnVoiceLinePressed
if (SpeakChat(hit)) _chatCooldown = ChatPopupPanel.Cooldown;
```

- 判据 = `VoiceLinesPopupSelector__ClickChat.c` 两句**顺序**：`VoiceLinesPopupSelector__Hide(param_1, 0)` →
  `OnVoiceLinePressed(index)`（`Hide` 一开头 `CanvasGroup.interactable = 0`）。
- 两个入口（`HandleChatPopup` 的松手半 / `ChatClickAt`）共用 `SpeakChatAt` ⇒ 一处改、两条路同语义。
- **断言**（`Editor/BattleScene.cs:14424-14431`）：点第 1 句 ⇒ `LastClicked == 0` + `ChatCooldownLeft > 3.9` +
  **`!pop.Visible`**。同一段的 6 连点循环改成**每颗点前 `Show()`**（上一颗点完气泡已关），
  「点面板外 ⇒ 关」那条也先 `Show()` 再点（保持判别力，`Editor/BattleScene.cs:14458-14467`）。

### E.（协调者追加 2）`CloseAll964` 收掉 `ChoosePanel`

**落点** `Editor/BattleScene.cs` 的 `CloseAll964()`（`drv964.Choose != null && drv964.Choose.Visible ⇒ Hide()`）。

- 解的是 A2 报告 §三·5「`ChoosePanel` 收不掉」；A3-e3 已给它补公开 `Hide()`（= 基类 `Close()` 的别名，
  `ChoosePanel.cs:227`）。`BattleDriver.Choose` 属性本来就有（`BattleDriver.cs:6341`）。
- 用 `Hide()` 而不是 `Close()`：与同族（`Settings.Hide` / `ChatPopup.Hide` / `BattleLog.Hide`）**同一套命名**。

### F.（协调者追加 2）`SkillPanel` 的 uGUI 事件接回驱动

**落点** `Battle/BattleDriver.cs`

| 事件（A3-e3 新增，原无订阅者） | 驱动侧落点 | 原版判据 |
|---|---|---|
| `AbilityClicked` | `SkillPanelConfirmRelease()` → `skillPanel.SetActing()`（**不结算**） | `ActiveSkillDisplay.ClickUp → BattleManager.TryUsingActiveAbility` 的**目标型**分支：`ShowActingLight()` + `StartChoosingAbilityTargetByClick()`。我们的面板出现时**已经在选目标** ⇒ 只剩「亮 acting 层」这一步 |
| `AbilityDragged` | `SkillPanelDragToTarget()` → `HighlightTargets()` + `ShowReticleNow()`（幂等重进；**不结算**） | `ActiveSkillDisplay.OnTouchDrag → BattleManager.StartTargetedActiveAbility`（进选目标 / 亮准星 / 合法目标高亮）。uGUI `Drag` **不带目标** ⇒ 「拖到目标上释放」需要另开一件（A3-e3 §三·2-D 已记） |
| `CloseRequested` | `OnSkillPanelCloseRequested()` → `ClearSelection()` | `HideAbilityButton.PointerDown → StopDisplayingSkillFromManager` 的**宿主那半**（`CardScript.StopDisplayingActiveAbility`）；面板那半（`FadeOut`）它自己已做 |

- 订阅点：`BattleDriver.BuildHud()` 的 `if (skillPanel != null)` 块（`BattleDriver.cs:18850-18852`；`_hudBuilt` 闩 ⇒ 只订阅一次、不会叠委托）。
  三个处理函数：`SkillPanelConfirmRelease` `:10185` · `SkillPanelDragToTarget` `:10201` ·
  `OnSkillPanelCloseRequested` `:10214`；世界侧让路那一句在 `DrivePlayerTurn` ④ 开头 `:9833`。
- **「释放只发生一次」**：三个口都**不调 `Resolve`** ⇒ 结算仍只有「点目标」那一下（`DrivePlayerTurn` ④）。
  另外把世界侧那条「点空白 = 取消」在**指针压在技能面板上**时让路（`DrivePlayerTurn` ④ 第一句，
  原版点面板 = `TryUsingActiveAbility`、**不是**取消 —— 这就是 A3-e3 §三·3·1 记的那条真偏离）。
- **断言**（`Editor/BattleScene.cs`，插在 06c 截图之后、结算之前）：
  ① 面板 `PointerUp` ⇒ 计数 +1、灯变 `Acting`、**选中槽/打法不变**（没订阅 ⇒ 灯仍 `Available` ⇒ 红）；
  ② 面板 `Drag` ⇒ 计数 +1 且状态不变（钉「订阅者没把它做成结算/取消」）；
  ③ 关闭捕获区 `PointerDown` ⇒ 计数 +1、面板收、`SelectedSlot < 0 && Command == None`
  （没订阅 ⇒ 面板会自己收，但选中槽仍 ≥0 ⇒ 红）；
  ④ **灭自证**：三跳前后**敌人血量不变** ⇒ 「释放」不可能在这里发生（结构上排除两次释放）。

### G.（协调者追加 3）A3-e4：`AttackSelector` 悬停改 uGUI + `CardDisplayWindow` D 收口

**落点 1 —— 三处旧断言（`UpdatePointer` 的几何悬停已退场）**

`AttackSelector.UpdatePointer` 在画布档**只推进画/回读 `Hovered`**（A3-e4 §2·2 第 8 条），
所以三处「喂世界点 ⇒ 期望 `Hovered`」必红。全部改成 **`HighlightTriggerForTest(i).OnPointerEnter/Exit` 直调**
（= `EventTrigger` 的处理器，与 `EventSystem` 送达的同一跳；`i` = `Order` 下标 0 Melee / 1 Ability / 2 Ranged）：

| 位置 | 旧 | 新 | 判别力 |
|---|---|---|---|
| 5b 选择器（`:5984` 起） | `SimulatePointerAt(rp); HoveredCommand == Ranged` | `HighlightTriggerForTest(2).OnPointerEnter(...)`；**另补一条 `PointerExit ⇒ Hovered == None`** | 只进不出 / 恒命中 ⇒ 第二条红 |
| A462 §七（`:22154` 起） | `sel4.UpdatePointer(bw4)` | 同上（`OnPointerEnter`）；安全窗 / `HoverPickReady` 那几条不动 | 安全窗那几条仍按原判据 |
| A964 模型③（`:25206` 起） | `UpdatePointer(中心)` 中 / `UpdatePointer(+10)` 不中 | 逐格 `OnPointerEnter`（必须判成它）+ `OnPointerExit`（必须清掉） | 「进入全中·离开全不中」；恒命中 / 不清 ⇒ 红 |

`BattleDriver.SimulatePointerAt` 保留（它仍推进入场动画 / 准星 / 面板），doc 里写明「画布档不再改 `Hovered`」。

**落点 2 —— `HandleDisplayWindowClick` 三支的 D 收口**

| 支 | 处理 | 理由 |
|---|---|---|
| ① 语音钮 | **删掉**（改由 `CardDisplayWindow` 的 `BattleHudButton` → `ClickVoiceButton` → `PlayVoice`） | A3-e4 已给 uGUI 路；两条路并存 = 一次点击播两遍（那边只加了同帧去重兜底） |
| ② 卡格 `HitSlot → SwapToFront` | **保留** | 卡体仍是世界空间（`T09` 第 5/6 步未迁）；A3-e4 §三·4 明说 **⛔ 卡格迁移前不能删** |
| ③ 遮罩空白 | **删掉**（改由全屏透明件 `cdw_background_close` 的 `BattleHudButton` → `Hide()`） | 原版 `BackgroundCloseButton.OnPointerClick`；驱动不再关窗 |

方法现在（`BattleDriver.cs:8836`）= 「卡格换位 + **一律 `return true`**（窗开着 = 模态，吞掉松手沿，别落到棋盘/手牌）」。
**连带更新**了三条旧断言（原「`HandleDisplayWindowClick(遮罩空白)` ⇒ 窗关」）：
改成「驱动**不关窗**（负向）+ uGUI 那颗全屏关闭件 `ClickForTest()` ⇒ 窗关（正向）」，
三处（多格分支 / 单格分支 / 相关卡那一段）都改了。

### H.（协调者追加 4）双 605：`#i` 后缀在**显示前**拆掉

**落点** `Battle/BattleDriver.cs`

| 位置 | 改了什么 |
|---|---|
| 新 `KeywordForDisplay(string)`（`PlayTraitPulse` 上方） | **唯一**解口：`BattleEvent.SplitKeywordAbility(keyword, out _)`（同一处判据，⛔ 不另写解析） |
| 战斗日志 `case EvtKind.Trigger`（`:7813-7821`） | 印 `KeywordForDisplay(e.Keyword)`，只印 `UseFaith`（不是 `UseFaith#1`） |
| `PlayTraitPulse`（`:16032` 起） | 先 `kw = KeywordForDisplay(e.Keyword)`；`TraitFrames.DisplayTrigger(kw)` / `CardView.PulseBadgeByKeyword(kw)` / **miss 日志**全用 `kw` |

- ⛔ **没有改事件本体**：`VfxMap` 仍拿原始 `Keyword`（含 `#i`）筛行（`BattleDriver.cs:15315` 那句 `triggerKeyword` 原样）。
- 🔴 **顺手修掉一个真缺陷（同一处判据）**：`TraitFrames.DisplayTrigger` / `PulseBadgeByKeyword` 都是**按裸 trigger 名**
  查表的（`Norm` 只 trim/lower，**不认 `#`**）⇒ 不拆的话双能力卡的状态框与徽标**都查不到**，而且 miss 日志会
  印一句「这个词条本来就没有图标」= **误导**。本波把这两处也换成拆过的 `kw` —— 这正是 `KeywordWithAbility`
  落地之前的**原有行为**（不是新设计）。
- **断言**（`Editor/BattleScene.cs` 战斗日志段，`ShowBattleLog()` 之前）：
  临时塞一条合成 `EvtKind.Trigger`（`Keyword = KeywordWithAbility("UseFaith", 1)`）→ 刷日志 → 读第 0 行 →
  **立刻移除**并重刷（不留副作用）。断言：那一行 **含 `UseFaith` 且不含 `#`**。
  🧨 直接印 `e.Keyword` ⇒ 出现 `UseFaith#1` ⇒ 红。
- ⚠️ **没改的一处（记下来）**：`PlayTraitParticles` 的 `EvtKind.Trigger` 那一支仍用 `e.Keyword.ToLowerInvariant()`
  当 trait id 查 `TraitParticles`（`:16088`）—— 同样不认 `#i` ⇒ 双能力卡的「触发类词条粒子」会查不到。
  它不在协调者点名的「两个日志」里、也不是显示层，**本波未动**（建议入账）。

---

## 二、断言改动清单（完整）

全部在 `Editor/BattleScene.cs`；行号为 2026-10-20 现读。

1. `:12025-12060` 新增**测试辅助** `void UguiFeed(Transform, Vector3, EventTriggerType)`：
   只走 `ExecuteEvents`（`IPointerDown/Up/Drag`），屏幕点 = `WorldToScreenPoint(HudCanvas.WorldCamera, world)`；
   `pressEventCamera` 是**只读属性**（uGUI 从 `pointerPressRaycast.module.eventCamera` 现算）⇒
   把 `pointerPressRaycast.module` 填成**根画布**的 `GraphicRaycaster`（`eventCamera == HudCanvas.WorldCamera`）。
   **不经过** `EventSystem` 的射线/click 派发 ⇒ 不构成第二条点击判据。
2. `:12066-12069` 新增 `Check(sp.OnSkipTutorial != null, …)`（A 的接线断言）。
3. `:12238-12251` A445 形状负向断言换成行为判别式（见 B 表）。
4. `:12263` 第二下 `drv.SettingsClickAt` → `sp.ClickForTest` + 计数（两态翻得动 + 灭自证）。
5. `:12326-12343` ChatToggle 两下 → `ClickForTest` + 计数（见 B 表末）。
6. `:12418-12431` 语言框点击链 → 原生 down/up 不触发 + uGUI click 开列表（计数 0→1）。
7. `:12448-12456` 语言框命中区有界 → `HitLanguage` 两态。
8. `:12605-12611` 开列表 → `ClickForTest` + 计数。
9. `:12682-12699` 点行 → `HitLangRow` 两态 + `LangRowClickForTest(0)` + 计数；另加 `LangBlocker` 射线靶断言。
10. `:12713-12722` 按下行、抬到别处 → 原生 down/up 喂给行钮 ⇒ 不选 + 计数不动。
11. `:12720-12729` 点空白 → `ClickForTest("Blocker")` + 计数 + 列表关 + 窗不关。
12. `:13066-13110` 滑块 ①输入接线 ②按下进拖动态 ③Drag 改值 ④抬起退出 ⑤没按下时 Drag 不改值 ⑥收尾无残留拖动。
13. `:13147-13154` 1/4 处 / 滑区右沿 → uGUI down/drag/up（值 0.25 / 1 的期望值未动）。
14. `:21930-21938` A462 滑块模态断言措辞订正（行为断言未动）。
15. `:21979-22001` A462 Auto Zoom：松手帧手搓不翻 + uGUI click 才翻（拆成两条）。
16. `:23867 / :23880` 跳过教程两态用例 → 走 uGUI 回调口（顺带验 A 的接线）。
17. `:4631-4642` + `:4659-4666` 日志点行：面板关 + 卡弹窗开 + （重开面板后）第二行仍能点。
18. `:14380-14467` 聊天：每颗点前 `Show()`、第 1 句断言「生效 + 气泡关掉」、点外面那条先 `Show()`。
19. `Editor` 的 `CloseAll964()` 新增 `drv964.Choose.Hide()`（E）。
20. SkillPanel 接线断言块（F，插在 `06c_技能卡面板` 之后、结算之前）：`PointerUp` ⇒ Acting 灯 + 不结算；
    `Drag` ⇒ 幂等；关闭捕获区 ⇒ 面板收 + `ClearSelection`；三跳前后敌人血量不变（灭自证）。
21. 5b 选择器悬停（`:5984` 起）→ `HighlightTriggerForTest(2).OnPointerEnter` + **新增** `PointerExit ⇒ None`。
22. A462 选择器悬停（`:22154` 起）→ 同上（`OnPointerEnter`），安全窗那条判据不动。
23. A964 模型③（`:25206` 起）→ 逐格 `OnPointerEnter` 必中 + `OnPointerExit` 必清；`hits964` 那一行同步改口径。
24. 展示窗「遮罩空白 ⇒ 关窗」三处（多格 / 单格 / 相关卡段）→ 「驱动**不关窗**（负向）+ uGUI 全屏关闭件
    `ClickForTest()` ⇒ 关窗（正向）」。
25. 新增类级辅助 `NewPointerEventData()`（`EventTrigger.OnPointer…` 直调用；不走射线）。
26. 战斗日志段新增 `#i` 后缀断言（H）：合成 `Trigger(UseFaith#1)` → 读第 0 行 → 移除重刷；
    断言含 `UseFaith` 且不含 `#`。

**没有删任何一条断言**；被替换的每条都保留了「期望值 / 判别式 / 改坏法」三件，且多数补了 uGUI 计数当灭自证。

---

## 三、没查清的部分（⛔ 不猜）

1. **编辑模式下 `EventSystem.RaycastAll` 能否真打中设置面板子画布上的件**：本波不跑 Unity ⇒ 没实测
   （与 A3b2 / A3c / A3e1 同族风险）。本波的 `UguiFeed` **不走射线**（直接 `ExecuteEvents`），
   所以新增断言不依赖它；**生产点击**仍依赖它（第一次 `BattleScene.Run` 若几何断言假红，先补
   `BattleEventSystem.SyncRaycasters()` + `Canvas.ForceUpdateCanvases()`）。
2. **`WorldToScreenPoint` → `ScreenPointToLocalPointInRectangle` 的往返精度**：同一台正交相机、同一平面，
   理论上是精确逆；**未实跑**。若滑块/1-4 那两条出现 ~1e-3 量级的边界红，先查这里（不是算式错）。
3. **`ChatPopupPanel.Show()` 在淡出中途重开**是否完全复位：本波测试每颗点前 `Show()`（只读源码判断幂等），未实跑。
4. 🔴 **点行弹的「卡」与原版不是同一件（既有偏离，本波未改）**：原版 `ClickCardLink → BattleManager.DisplayCard`
   开的是**大卡展示窗**（`CardDisplayWindow__ShowBattleCard`）；我们的 `TryClickLogRow` 弹的是**小卡** `_logCard`
   （`ShowLogCard`，2026-10-18 `G9` 的既有口径）。本波按派工单只补「关面板」，**卡的种类/位置没动** —— 建议主对话记账。
5. 🔴 **`BattleLogPanel.RowClicked` / `CloseRequested` 仍未接线**（A3c §三·2-2 的合并配方归 W3/D）。
   本波只改了手搓行点击链；生产上若 `EventSystem` 的 `shade PointerDown` 先于驱动关掉面板，
   `HideLogCard()` 不会执行（A3c §五·3 的时序缺口）⇒ 悬停卡可能残留。**未在本波范围内修**。
6. **跳过教程的 `minTimeBeforeSkip` 闸**（A991 已如实记的真偏离）本波未动（要改得两边一起改）。
7. 聊天 6 颗钮的**悬停/按下视觉反馈**仍缺（A3e1 §六·2 已记），本波未补。
8. 🔴 **技能面板的全屏捕获区（`HideAbilityButton`，A3-e3）会压住 3D 棋盘单位**：它是**全屏 uGUI 射线靶**，
   而棋盘单位是 `MeshRenderer`（不是 uGUI）⇒ 生产里 `EventSystem` 只看得见那块捕获区。
   后果：技能面板开着时，**点合法目标**的 `PointerDown` 也会打到捕获区 ⇒ 面板 `Hide()` + `CloseRequested`
   ⇒ `ClearSelection()`；松手那一帧 `DrivePlayerTurn` ④ 已无选中的打法 ⇒ **技能可能放不出去**。
   原版那张卡自己的 `UI Collider` 会先吃掉点击（而我们没有）。本波按 A3-e3 配方只接事件、**没有改捕获区几何**
   （`SkillPanel.cs` 不在白名单）⇒ **第一次真 Play 必须先验这一条**；根治 = `T09` 第 5/6 步把卡格迁 uGUI，
   或在捕获区 `CloseRequested` 里按 `LegalTargetCode(WorldPointer())` 放行（需改 `SkillPanel` 语义，另开一件）。
9. 🔴 **展示窗的全屏 backdrop（A3-e4）同理压住 3D 卡格**：卡格仍是世界空间 ⇒ 生产里点卡格会命中
   `cdw_background_close` ⇒ 窗被关掉、`SwapToFront` 也走（驱动那条还在）——「点相关卡」这一下可能变成「关窗」。
   A3-e4 §三·4 已记「卡格未迁」，本波按指示**保留 `HitSlot` 手搓路**、不越界。
10. **`HandleDisplayWindowClick` 现在恒 `return true`**（窗开着就是模态）：随「同帧守卫」一并删掉的
    `SameFrame(_cardWinOpenedFrame)` 分支原本在「刚开窗那一帧」返回 false 让松手沿继续往下走；现在被吞掉。
    理论上更安全（模态），但**未实跑**；若真机出现「轻点手牌开窗后同帧的其它点击被误吞」，查这里。
11. **`SkillPanel` 的 `AbilityPressed` 没有订阅**：按压灯本来就由面板自己写（A3-e3 §2·2 第 7 条），
    而原版 `ClickDown` 的另一个副作用（`AbilityBoxClick` cue）本地没有那个音频资产（A3-e3 §三·1·3）⇒
    没有可接的动作。如实记：这一口**空着是判据侧的决定**，不是漏接。

---

## 四、typecheck 结果

```text
$ cd d:/4/Unity/MyGame
$ TMPDIR=/tmp/wf_t09wire bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 中途出现过**两次**别家代理的半成品报错（与简报预警一致）：
  - `ChatPopupPanel.cs(536-539): CS0103 AddTrigger`（另一代理正在补该方法）—— 等 60 s 重跑即消失；
  - `RuleEngine/Editor/RuleEngineTest.cs(394): CS0103 TestW57Double605` —— 等 75 s 重跑即消失。
  两次都**不在本件白名单文件里**、随后自愈 ⇒ 不是本件的错。
- **最终一次**（全部七件落完之后）复跑：**运行时 0 / 编辑器 0**。
- 行尾核对（二进制计数，本波结束时）：

| 文件 | 字节 | CRLF | LF | lone LF |
|---|---:|---:|---:|---:|
| `BattleDriver.cs` | 1,758,565 | 22,351 | 22,351 | **0** |
| `Editor/BattleScene.cs` | 2,720,446 | 31,494 | 31,494 | **0** |

⇒ 仍是**纯 CRLF**，没有整篇翻行尾（`git diff --numstat` = `1020/226` 与 `1494/230`；
本波开始时的基线是 `851/189` 与 `1028/98` ⇒ 本波净增 **+169 / −37** 与 **+466 / −132**）。

---

## 五、一句话给主对话

**八件都落地：① `OnSkipTutorial` 接到 `SkipTutorialFromSettings`（§⑩-b 改走 uGUI 回调口，没接就红）；
② 日志行点击按原版 `ClickCardLink→HideCemeteryLogBtn` **关面板**（卡继续弹着）；
③ 聊天行点击按原版 `ClickChat` **先关气泡再说话**；
④ `CloseAll964` 收 `ChoosePanel`；
⑤ `SkillPanel` 三个 uGUI 事件接回驱动（`AbilityClicked→SetActing`、`AbilityDragged→重进选目标`、
`CloseRequested→ClearSelection`），**都不结算** ⇒ 「释放只发生一次」由结构保证，另有血量不变的灭自证；
⑥ A3b2 §7 的 11 组旧断言换成 uGUI 单一路径，另多修 §7 没列的 4 处；
⑦ A3-e4：三处 `UpdatePointer` 悬停断言改喂 `Highlight` 的 `EventTrigger`，
`HandleDisplayWindowClick` 收口成「只做卡格 + 模态吞沿」（语音/遮罩交给 uGUI），三处「遮罩关窗」断言同步改；
⑧ 双 605 的 `#i` 后缀在**显示前**拆掉（战斗日志 + `PlayTraitPulse` 三处；顺带修好「双能力卡徽标/状态框查不到」），
另补一条「日志里不许出现 `#1`」的端到端断言。
typecheck 运行时 0 / 编辑器 0，两文件仍纯 CRLF。**
**⚠️ 三条留给下一个会话的验前必读**：技能面板的全屏捕获区 / 展示窗的全屏 backdrop 会压住**仍是 3D 的**卡格
（A3-e3/E4 的过渡期代价，本波按指示不越界）⇒ 真 Play 先验「技能能不能点目标放出去」「点相关卡会不会变关窗」；
`PlayTraitParticles` 的 `EvtKind.Trigger` 那一支仍不认 `#i`（本波未动，建议入账）。

