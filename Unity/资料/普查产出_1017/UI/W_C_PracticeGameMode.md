# W_C · 练习窗 `Game mode` 开关（U4-1）落地报告

日期：2026-10-17 · 写手代理（界面线）· 唯一改动文件 = `Unity/MyGame/Assets/CardPresentation/Shell/PracticeModePopup.cs`
（同目录**没有** `PracticeModePopup.*.cs` 辅助文件 —— 现读 `ls Shell/ | grep -i practice` 只有 `PracticeModePopup.cs` + `.meta`）。

---

## 1. 结论 + 改动清单

**结论**：练习窗底部那条 `Game mode` 开关**四件几何全部按原版逐值建全**（旧实现只有一张拉错的底图 + 一行画在整框上的字），
点击按原版语义在 **Classic(0) ↔ Skirmish(13)** 之间切，切换后走 `ResetContent`（重列该模式的卡组），
`OpenDeckSelection` 不再写死 Classic、`Battle!` 一侧补了一致性出声。`typecheck` **0/0**。

| # | 改动 | 位置（`Shell/PracticeModePopup.cs`） |
|---|---|---|
| 1 | 新加 `Game mode` 开关**四件子件的几何常量**（`GmBg*` / `GmSk*` / `GmCl*` / `GmPlate*` / `GmLabel*`）+ 判据注释 | `:450-464` |
| 2 | `Build()` 第 5 步：删掉「89×40 底图按 keepAspect 拉进 293.51×57.84 + 文字画整框 + `NotBuilt` 命中区」，改调 `BuildGameModeToggle(root)` | `:876-880` |
| 3 | 新增 `SelectedGameMode` / `ToggleModeOf` / `GameModeKnobArt` / `BuildGameModeToggle` / `ToggleGameMode` / `SetGameMode` / `RefreshGameModeVisual` / `ResetContent` / `FirstDeckInMode` | `:895-1045` |
| 4 | `Open()`：`Game mode` 初值按当前选中卡组的模式初始化（**主动偏离 ctor 默认 13**，理由见 §4） | `:805` |
| 5 | 卡组列表按模式筛（`RebuildDeckRows`） | `:1323` |
| 6 | `FilteredDeckCount()` 与 #5 **同一套筛选**（模式 + 阵营，内容高/内容同源） | `:1515-1527` |
| 7 | `OpenDeckSelection()` 的模式实参 `(int)GameMode.Classic` → `(int)SelectedGameMode`；就地订正旧注释（铁律 5） | `:1563-1568` |
| 8 | `StartPracticeMatch()`：开窗后先把开关对齐**这一副自己的模式**（`SelectedGameMode` + `RefreshGameModeVisual`）再 `PickDeck` | `:1812-1814` |
| 9 | `StartBattle()`：选中卡组的模式 ≠ 开关模式 ⇒ **出声警告**（原版会取同一 entry 的另一套槽位；我们没有那套数据） | `:1918-1932` |

**节点结构**（照原版兄弟序，全部挂在新建的 `Toggle` 节点下）：

```
Toggle                        (813.24,906.26 → 1106.76,964.10)   ← 只作交互框（原版 EverguildToggle + NonDrawingGraphic）
 ├ Background                 (89×40)                 40_main_bt_toggle_off / _on（随 isOn 换图）
 ├ Skirmish toggle Image      (94.6439²)              40k_gamemode_icon_skirmish
 ├ Classic toggle Image       (94.6439²)              40k_gamemode_icon_classic
 ├ Label Background           (320.267×40)            40k_main_bt_nametag（Simple 拉伸）
 │   └ Toggle Label           (210.536×40 · fs36)     Loc.T("Event_Description/Generic/GameMode")
 └ 命中区 ×4（ToggleHit / ToggleSkirmishHit / ToggleClassicHit / ToggleLabelHit）→ 同一个动作
```

⚠️ 两处命名如实标注：
· 原版那颗文字节点名是 **`Label`**，我们用**既有名 `Toggle Label`**（`Editor/MainMenuScene.cs:2240` 按这个名字取件，改名会红）。
· `ToggleHit` 是既有名（`Editor/CollectionScene.cs:4043` 按它断节点类型），保留。

---

## 2. 四件几何逐值对照表

判据命令（本轮现读）：
`python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Practice Mode Menu" --depth 6 --md`
（输出另存 `/tmp/pm_dump2.md`；下表逐值照抄那一份）

| 件 | 原版矩形（设计 px，y 向上） | 原版尺寸 | sprite / 字段 | 我们实现值 | 出处 |
|---|---|---|---|---|---|
| `Toggle`（交互框） | 813.24,906.26 → 1106.76,964.10 | 293.51×57.84 | `EverguildToggle` `isOn=0`；`onSprite=-7583144681335034689` / `offSprite=-8582066306391164324` | `Node("Toggle")` 同矩形（**只当交互框**）+ `HitOn` 同矩形 | dump 第 106 行 |
| `Background` | 915.50,915.18 → 1004.50,955.18 | **89×40** | `40_main_bt_toggle_off`（89×40 · `m_PreserveAspect=0` · `m_RaycastTarget=1`） | `Img(..., "Background", QPrRow, keepAspect:false)` @ 同矩形 | dump 第 107 行；MB `6335940333449269052` |
| `Skirmish toggle Image` | 813.24,887.86 → 907.89,982.50 | **94.6439²** | `40k_gamemode_icon_skirmish`（256×256） | `Img(..., "Skirmish toggle Image", QPrRow, keepAspect:true)` @ 同矩形 | dump 第 108 行 |
| `Classic toggle Image` | 1012.11,887.86 → 1106.76,982.50 | **94.6439²** | `40k_gamemode_icon_classic`（256×256） | `Img(..., "Classic toggle Image", QPrRow, keepAspect:true)` @ 同矩形 | dump 第 109 行 |
| `Label Background` | 799.87,853.24 → 1120.13,893.24 | **320.267×40** | `40k_main_bt_nametag`（源图 109×41 · `m_PreserveAspect=0` ⇒ **拉伸**） | `Img(..., "Label Background", QPrItemHL, keepAspect:false)` @ 同矩形 | dump 第 110 行 |
| `Label` | 854.73,853.24 → 1065.27,893.24 | **210.536×40** | fs **36** · 基准 **36** · `Center/Capline` · 折行 **1** · 无色偏 | `Txt(Loc.T(...), L/R/T/B, 36f, Align.Center, wrapPx=210.536)` | dump 第 111 行 |

**旧实现错在哪（逐条对照）**：
1. 底图 `40_main_bt_toggle_off` 按 `keepAspect` 画进 **293.51×57.84** 整框（89×40 的图被缩成 128.7×57.84 居中）—— 原版是 89×40、不保比。
2. 文字画在整框（813.24..1106.76 × 906.26..964.10）上 —— 原版框是 854.73..1065.27 × 853.24..893.24（**低 13px、窄 82px**）。
3. 两颗 94.6439² 模式图标 **一个都没建**。
4. `Label Background`（名牌底）**没建**。
5. 点击只 `NotBuilt`（出声但什么都不做）。
6. 下游：`OpenDeckSelection` 写死 `(int)GameMode.Classic`。

**队列（分层）**：
`Background`/两颗图标 = `QPrRow(3101)`；`Label Background` = `QPrItemHL(3102)`（原版兄弟序里它最后 ⇒ 画在图标之上，
两者在 y 887.86..893.24 有 5.38px 重叠；`QPrItemHL` 与本窗 item 内景**屏上不相交**，复用同 A178「两族 item 共号」的口径）；
文字 = `QPrText(3107)`；命中区 = `QPrHit(3108)`。

**isOn ↔ 模式的对应（prefab 实读）**：`GameModeValueChange(false) → 13`、`(true) → 0` ⇒
`isOn = false → offSprite = 40_main_bt_toggle_off → Skirmish`；`isOn = true → onSprite = 40_main_bt_toggle_on → Classic`。
`MonoBehaviour -8743438662623342788`（= 本窗 Toggle，`spriteToChange` 指向 `Background` 那张图 MB `6335940333449269052`，
全 bundle 只有 1 张图用该 sprite ⇒ 唯一解）里 `colorTintOnValueChange = 0` ⇒ **只换图、不换色**（`onColor/offColor` 是死值）。

---

## 3. 行为链（逐跳）

**点击 → 切模式**
1. 命中区 4 块（`:966-975`）→ `ToggleGameMode()`（`:977`）
2. `SetGameMode(mode)`（`:982`）：`SelectedGameMode` 取反档 → `RefreshGameModeVisual()`（`:995`，`Background.SetTexture(on/off, keepAspect:true)`）→ `ResetContent()`（`:1014`）
3. `ResetContent` → `FirstDeckInMode(SelectedGameMode)`（`:1034`，按 **模式 + 阵营** 选第一副）→ `PickDeck(first)`；
   一套都没有 ⇒ `DeckIndex = -1` + 清空卡列表/文案 + `Debug.LogWarning`（**出声**，不假装选到）

**重列卡组**
4. `PickDeck` → `RebuildDeckRows(_deckHolder)` → 列表过滤 `ToggleModeOf(info.GameMode) != SelectedGameMode ⇒ continue`（`:1323`）；
   `FilteredDeckCount()`（`:1515`）用同一套筛选 ⇒ 内容高与列表同源

**选卡组窗按当前模式**
5. `Deck Info ▸ Change Deck` → `OpenDeckSelection()`（`:1550`）→ `DeckSelectionPopup.Create(..., modeFilter: (int)SelectedGameMode)`（`:1563`）
   → `DeckSelectionPopup.Shown()` 的 `ModeFilter` 分支只筛「我的卡组」页（预组页照原版不筛）

**Battle! 按当前模式**
6. `Battle!` 命中区 → `StartBattle()`（`:1904`）：先查出选中卡组，若其模式 ≠ 开关模式 ⇒ `Debug.LogWarning`（`:1918-1932`）；
   开战链本身不变（`PlayMode = OfflinePractice(6)`，原版 `BattleButtonOnClick` 的 `StartMatch(..., 6, ...)` 也是 6 ——
   `gameMode` 只决定**用哪套卡组**，不决定 `PlayModes` 号）

**开窗初值**
7. `Open()`（`:797`）→ `SelectedGameMode = ToggleModeOf(DeckAt(CurrentIndex).GameMode)`（`:805`）→ `Build()`
8. `StartPracticeMatch()`（`:1808`）：`OpenWindow` 后先把开关对齐 `ownDeckIndex` 那一副的模式，再 `PickDeck`

**原版判据**
- `PracticeModePopup__GameModeValueChange.c`：`param_2 == 0 → 0xd`，否则 `0`；写 `self+0x110`；`ResetContent`；再调 vtable+0x178。
- `PracticeModePopup__.ctor.c`：`*(int*)(self+0x110) = 0xd`（出厂 Skirmish）。
- `PracticeModePopup__Awake.c`：`Toggle+0x118`（`UnityEvent<bool>`）→ `Action<bool>` → `GameModeValueChange`。
- `PracticeModePopup__SetArmyButtons.c`：按 `gameMode` 在 `[+0x18] / [+0x20]` 两套槽位里二选一 ⇒ 我们的等价 = 按 `PlayerDeck.GameMode` 筛。
- `PracticeModePopup__BattleButtonOnClick.c:96-160`：`plVar1[3]`（gameMode=0）/ `plVar1[4]`（≠0）二选一 → `CardDeck..ctor` → `StartMatch(..., 6, ...)`。

---

## 4. 没做的 / 没查清的 / 主动偏离（逐条「为什么 + 搜过什么」）

1. **初值偏离原版 ctor 默认 13 → 按当前卡组模式**（`Open()`，`:805`）。
   为什么：我们这扇窗列的是**玩家自己的卡组**（原版列的是预组 `Practice Deck`，每条带 Classic/Skirmish 两套槽位），
   而玩家卡组默认 `GameMode = Classic(0)`（`RuleEngine/Data/DeckLibrary` 侧默认 0）⇒ 照 13 开局列表会空。
   **如实标注这是偏离**，不是「原版就是这样」。若调度台裁定要严格照 ctor，把 `:805` 改成 `GameMode.Skirmish` 即可（一行）。
2. **开关的显示条件**：原版 `Awake` 里 `GameModes.GetActiveEvent(...) != null` 才 `SetActive(true)`（LiveOps 活动）。
   我们没有服务器 ⇒ **常显**（同 `ChatPanel` 那条老账）。本轮不动。
3. **`plVar1[3]/[4]` 的槽位结构**：照 U4 §五·3 的已知结论，**没有照搬槽位号**；我们按「每副卡组自己的 `GameMode`」实现。
   没查清的部分：原版一个 entry 的两套槽位里，Classic/Skirmish 各自指向的 `CardDeck`/督军数据是不是**同一副牌的两个变体**（判据缺）。
4. **模式号不在 {0,13} 的卡组**：`ToggleModeOf` 一律按 Classic 处理（原版这颗 `EverguildToggle` 也只有两档）。
   ⚠️ 一处**已知的不一致**：`RebuildDeckRows` 用 `ToggleModeOf`（非 13 → Classic），而 `DeckSelectionPopup.Shown()` 用
   **精确相等**筛 `GameMode` ⇒ 若真有 mode=6 之类的卡组，练习窗列表会显示、选卡组窗「我的卡组」页不显示。
   本轮查过的生产侧来源（`LiveOpsEventWindow.DeckGameMode` 基类 0 / 遭遇窗 13）都只产生 0/13 ⇒ 今天不可达；**不改**（改动在别的文件）。
5. **`StartBattle` 的模式不一致只警告、不拦**：原版会取同一 entry 的另一套槽位；我们没有那套数据。
   拦截会改变玩家可见流程（且不在本任务验收范围）⇒ 只 `LogWarning`，如实记在这里。
6. **`NotBuilt` helper 现在没有调用点**（`:2074` 那一族唯一调用点被本轮替换）。保留（私有方法无未使用警告；同类先例是给未来缺口留的口）。
7. **没跑 Unity / 没跑自检**（铁律 12 现行口径：复刻阶段不验收）。只跑了秒级类型检查。
8. 原版 `Label` 的**垂直对齐 = Capline**：本文件 `Txt` helper 没有纵向对齐形参（全文件同形），本轮**没加**
   （加会改公共 helper 语义、影响其它 8 处调用点）—— 记作缺口，建议后续在 `Txt` 上加 `VAlign` 形参统一处理。
9. 开关的**悬停/按下态**：原版 `EverguildToggle` 的 `m_Transition = 1(ColorTint)`，`m_Colors` 默认那组
   （`m_HighlightedColor = 0.9607843`）⇒ 悬停时整组 graphic 乘 0.9608。我们这一层走的是 `WindowButton` 的通用悬停兜底
   （本文件的 `HitOn` 就是那颗），**没有**把四件图形绑成 targetGraphic —— 与全工程其它按钮同一档偏离（A15 那笔账），本轮不新增。

### 顺手发现（⛔ 未改，交调度台）
- `PracticeModePopup__Awake` 里那颗 `Toggle` 是**运行时按 LiveOps 活动开关**的（见 §4·2）；我们建了常显件。
  同类先例已记在文件头（`GameModeText` / `Character Image` 的 act 判断），这一处**没有**登记在「没建的」那一节 —— 建议收口时补一行。
- `U4_卡组编辑与战斗入口.md` §二 B 最后两行写的 `Background 89×40 @ (915.5, 915.2)` 是**左上角**；
  `Skirmish/Classic toggle Image @ (813.2, 887.9)` 也是左上角（本轮 dump 复核一致）。文档本身没错，记在这里只为消除歧义
  （`@` 在别处的表格里有时指中心，容易读错）。

---

## 5. typecheck

```
TMPDIR=/tmp/wf_c_practice bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⚠️ 第一次跑时运行时程序集报 `Assets\CardPresentation\Shell\MainMenuRuntime.cs(1004,24): error CS0103: … Get()` ——
那是**另一个写手正在写的半成品**（文件 mtime 与 typecheck 同一秒、行号在重跑前已经漂走）；
隔 1 分钟重跑即 **0/0**。本轮**没有**碰那个文件。
