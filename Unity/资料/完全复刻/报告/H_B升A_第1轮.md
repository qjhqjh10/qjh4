# H · 菜单 / 外壳 · B 升 A 第 1 轮

> 执行代理 **H**。范围 = 上一轮 `菜单_外壳.tsv` 里 **343 条 B → 逐条核原版方法体**（按调度台给的顺序：窗口六件套 → WindowsManager → 滚动/命中 → 顶栏/页签 → 主菜单 → 其余），外加两件小事（`OneText` 结构注释+断言 · `claimedWarning` 未查到落账）与一件插单（**战役红点 `CampaignBadge`**）。
> ⛔ 未跑 Unity 批处理 · ⛔ 未动 git · ⛔ 未改 `CLAUDE.md` / `项目任务.md` · ⛔ 未跑 `gen_replica_ledger.py`。
> ✅ `TMPDIR=/tmp/wf_h bash 工具/typecheck.sh`：**运行时 0 / 编辑器 0**（末次；中途 `Core/FilterPanelModel.cs` 的一次报错是别的代理写到一半，复跑已清）。

---

## ① 结论（数字）

| 项 | 上轮 | 本轮 | 变化 |
|---|---:|---:|---:|
| `A 已对齐` | 204 | **332** | **+128** |
| `B 已实现未核` | 343 | **43** | **−300** |
| `C 该做没做` | 84 | **182** | +98（其中多数是「我们压根没这件」的诚实降级） |
| `D 不适用` | 242 | **316** | +74（大量是**实例其实在战场/特效/商店域**、或原版该分支为死代码/移动专用） |
| `E 未读` | 0 | **0** | — |
| 合计 | 873 | **873** | 每行都有 要点/落点/轮次；B 全部写了「差什么」 |

**43 条 B 的去向**（核不动，按调度台口径保留）：`ParentConstraint` 6（仅 4 处实例、未逐处核）· `MenuActions` 3（无动作队列）· `GameWindowWithTabs` 2（无 lazy SetupTabs / startTab）· `FlexibleGridLayout` 2 · `TitleDrawer/TitleIconDrawer` 4（称号页未建，画法未核）· `PromptPopup` 2 · `Referral` 2 · `DropTableContainer` 2（服务器掉落表）· `WindowHeaderWithBackButton` 2 · `EverguildButton/Toggle.SetUpButtonColors` 2（逐颗 `m_Colors` 未普查）· `UIGradientUtils` 2 · `SceneTransition/SceneTransitionList` 2 · 其余 10 条（`Draggable.OnDrag/OnRelease` 原版无产物、判据核不动 等）。

**关键降级（A/B → C/D，都带判据）**：
- `GameWindowWithTabs` / `WindowTabBase` / `TabButtons` 的 **页签生命周期**（`CloseTab`/`OnClose`/`OnTabChange`/`GetTab`/`ForceCloseTabs`/`CreateTabButton`/`ToggleInteractable`/`UpdateTab`）—— 我们只有「SetActive + OnOpen」⇒ **C**（P2）。
- `UIScaleToFit`（**183 处菜单实例**）· `UIImageMaterialColorChanger`（666）· `UIGroupTint`（306）—— 无通用组件 ⇒ **C**。
- `ScreenHighlightPosition`（28 处**全在战场**）/ `UICameraSetRenderScale`（26 处全在战场）/ `ShaderProperySetter`（4 处全在 battleprefabs）/ `UIAnchorAvoidSafeArea`（移动刘海）/ `GrabScreenFeature*` 旧式 `Execute` 路径 —— **D**（给了替代物）。
- `DragAndDropItem`（5 处实例全在 `Collection Deck` = 卡组编辑线）· `DragButtonInfoInt`（**全解包 0 实例**）→ C / D。

---

## ② 判据（本轮逐个方法读出来的关键几条）

- **窗口六件套（`GameWindow`）** —— 原版 `GameWindow.cs` 逐句：`Close` = 守卫 + 关窗音 + `manager.CloseWindow`；`Hide` = 守卫 + `state=Closed` + `SetActive(false)` + **`OnCloseWindow.Invoke`**；`TryOpen` 两条重载「带参先 `SetupData`、无参不调」；`ESCPressed` = 输入层 + `closeOnESC` 两道门槛 + `updateNavPanel`；`IsOpen` = `state==1`；`UnHide` = `ToBackground + SetActive(true)`；基类 `ToFocus` **空桩**。⇒ 我们逐条对上（见 `Shell/GameWindow.cs` 的 A217/A447/A49 doc），本轮**补上了原版唯一漏项 `OnCloseWindow`**（详见 §③）。
- **`WindowsManager.CloseWindowCO`** —— baseMenu 特例（只 `Hide`）、摘表、`Destroy` 实例化窗、顶窗则 `ShowPreviousWindow`、表空回 baseMenu —— 我们 `NotifyClosed` + `ShowPreviousWindow`（`WindowsManager.cs:476/522`）+ `GameWindow.Close` 的 A1714 分支逐条对位（实例化窗那半不适用：我们的窗代码建）。
- **`GetLastOpenMainWindow` / `TabChanged` / `RefreshCurrentWindow`** —— 全量反编译 **0 调用点** + 全解包 `m_PersistentCalls` **无该方法名** ⇒ **D（死方法）**。
- **页签红点（硬接线，逐份现读 `bundle_menus_assets_all`）**：
  | 页签 | 定义 |
  |---|---|
  | `CampaignRewardsButton` | **`CampaignBadge`（无过滤）+ `MissionsBadge`（useFilter=1 · `parentId="CampaignMission"`）** —— `MonoBehaviour_-287428095701543169.json`（`references.RefIds` 就是那两个预设） |
  | `MissionsRewardsButton` | `MissionsBadge`（MB_1604463168230790911） |
  | `Forge Button` | `ForgeBadge`（MB_8050322982303438591） |
  | `Friends Tab Button` | `FriendsBadge`（MB_-1609997114873618269） |
  | `Alliances Tab Button` | `AlliancesNotificationBadge`（MB_7673959444828905635） |
- **战役红点数据源**：`CampaignHandler__CheckNotification`（`Everguild.LiveOps/CampaignHandler.cs:151-230`）—— 取所选阵营战役（`GetCampaign(SelectedArmy)`，取不到再试 `army+10`）→ 有 `AvailableNodes`（可用 **且** 可领）⇒ **发【一条】** `new CampaignBadge(SelectedArmy)`（**一战役一条**，不是每节点一条）。
- **`EverguildButton`** —— `SoftDisable` 不改 `interactable`（A65② 已核）· `SwitchMaterial` 换 `Everguild/UI/Greyscale` · `OnPointerClick` 头一句 `!IsActive()||!IsInteractable()` · 五态 `GetStateColor` 取 `m_Colors` ⇒ 与 `Shell/WindowButton.cs` 逐条对上（22 行 A）。
- **`DraggableController`** —— `SetDraggable`（RVA 0x18153A0）· `OnBeginDrag`（0x1814690）· `OnDrag`（0x1814790）· `OnEndDrag`（0x1814E90，`hovered` 上逐个 `Drop`、循环内两次判空）⇒ 与 `Core/DraggableController.cs:148-230` 逐条对上（11 行 A）。
- **`ScrollViewFocusFunctions.FocusOnItem`** —— 原版调用点 = `ArmySelector.FocusOnArmy` / `DailyRewardSelector.AdjustView`；我们 = `MenuScroll.FocusOn`（`MenuScroll.cs:315`）+ 四个调用点（`CampaignTab:894` / `ForgeTab:835,846` / `DailyStreakPopup:593`）⇒ A；**补间版（×3）未做** ⇒ C·P2。

---

## ③ 改动清单

| # | 文件 | 改了什么 | `git diff --numstat` |
|---|---|---|---|
| 1 | `Shell/GameWindow.cs` | **补 `OnCloseWindow` 事件口**（原版 `+0x60`）：`Hide()` 末句非空就 Invoke（try/catch 出声）；`Close()` 不调（照原版，由 Close→manager→Hide 那一跳统一调） | `+26 / −1`（含注释） |
| 2 | `Shell/TopBar.cs` | **P1 修复：顶栏资源数字跟着 `Wallet` 刷** —— `Parts.CounterRef`（币种/标签/上次串）+ 建格时登记 + `RefreshCountersIfChanged`（变了才写） | `+51 / −4` |
| 3 | `Shell/MainMenuRuntime.cs` | `Update()` 里每帧调 `TopBar.RefreshCountersIfChanged` + public 自检口 `RefreshTopBarCountersIfChanged()`；另 4 处 UIGradient 措辞订正之一 | `+35 / −6` |
| 4 | `Shell/BadgeNotifications.cs` | **战役红点**：`CampaignNotificationProvider`（有可领节点 ⇒ 发 1 条 `CampaignBadge`）+ `DefinitionsForTab(instId)`（上表五类页签定义）+ 默认登记 | `+80 / −2` |
| 5 | `Shell/MainMenuSubmenuWindow.cs` | 页签按钮红点接 `BadgeWiring.Attach` + `DefinitionsForTab`（原来 `res.badge[]` 建出来 **alpha 恒 0、全仓零处刷新**）；UIGradient 措辞订正 | `+20 / −3` |
| 6 | `Shell/ParentEventWindow.cs` | UIGradient 措辞订正（**只改理由、188 不动**） | `+2 / −2` |
| 7 | `Shell/DailyRewardPopup.cs` | 同上（第四处；只改注释、不改行为 —— 暂停线允许） | `+2 / −1` |
| 8 | `Editor/MainMenuScene.cs` | **两条新断言**：`OneText` 结构（父级=导航钮 + 与红点同心）· 顶栏数字随 Wallet 刷（走生产唯一发奖口 + 当场还账）· 页签红点定义/Campaign provider 五条 | `+60 / −0` |

✅ 行尾未翻（每个文件的 numstat 都是「新增为主」；`SceneInitializer/BadgeNotifications` 是新文件）。

---

## ④ 断言（宿主 `Editor/MainMenuScene.cs`；⚠️ 自检暂停 ⇒ 只写不跑）

1. **`OneText` 结构**（调度台点名）：五颗导航的 `OneText` **父级必须是导航钮**（⛔ 不是 `Badge Highlight`）+ **世界中心与红点同心**（≤0.01 单位）。
   灭自证：照原版层级改回红点子件、坐标不同步 ⇒ 字整体偏一个红点中心 ⇒ 红；改矩形 ⇒ 红。
2. **顶栏资源数字随 `Wallet` 刷**（本轮 P1）：`Wallet.Grant(key,7)` → `RefreshTopBarCountersIfChanged()` → 那格串**变了**且 = `CounterText(Of(key), MaxOf(currency))`；随后**还账**并断言复原。
   灭自证：`RefreshCountersIfChanged` 改空 / 只改 `Shown` 不写 Label / 不还账 ⇒ 各自红。
3. **页签红点定义**（本轮插单）：`DefinitionsForTab("RewardsMenu_CampaignButton")` = 2 条（`CampaignBadge` 无过滤 + `MissionsBadge` useFilter=1 & `parentId="CampaignMission"`）；`MissionsButton`/`Forge Button` 各 1 条；**聚合器里必须有 `CampaignBadge` 那栏**（provider 已登记）；计数 = 「当前有可领节点 ? 1 : 0」（原版一战役一条）。
   灭自证：定义表清空 / 改 parentId / 注销 provider ⇒ 各自红。

---

## ⑤ 没查清（如实）

1. **顶栏数字刷新只重写文字、不重算药丸宽**：药丸宽是建窗时按 preferred 宽量的（`QtyWMin..QtyWMax` 夹取）。位数变多时走 `SetAutoFitBox` 缩字兜底而不是加宽药丸（原版 `ContentSizeFitter` 会加宽）——**我们挑的**，位数不变时逐位等价。
2. **战役红点计数**用的是「当前有可领节点 ⇒ 1」，与 `CampaignHandler.CheckNotification` 的**外显行为**一致；但原版 `GetCampaign(SelectedArmy)` 取不到还会试 **`army + 10`**（我按 `CampaignData.HasContent(Selected)` 判，不做 +10 回退——本地只有一个有内容的阵营 `Ultramarines`，该回退不可达）。
3. **`parentId="CampaignMission"` 的 `MissionsBadge`**：定义照挂，但**本地没有战役任务源** ⇒ 恒 0（⛔ 不登记假 provider）。
4. **战役进度不落盘**（重开回「只解锁根节点」）⇒ 本次会话领完红点会灭、重开又亮 —— 存档策略问题，已写进 `CampaignNotificationProvider` 的 doc。
5. **43 条 B 的具体缺口**逐行写在 `差异` 列（`ParentConstraint` 4 实例未逐处核 · 逐颗 `m_Colors`/`m_Transition` 未普查 · 称号页画法 · 服务器掉落表 · 无 lazy 页签缓存 …）。

---

## ⑥ 顺手发现

1. 🔴 **`UIGradient` 角度那条错措辞共 5 处**（本轮订正 4 处 + `DeckRuntime:1452` 由 C 改）：错在把「`RotationDir`」与「有效渐变轴」当成同一个方向、又把它说成「差 90°」。**实际**：`RotationDir` 返回 **`(cosθ,sinθ)`**（`UIGradientUtils__RotationDir.c:9-11`）· 有效轴 **`(sinθ,cosθ)`**（`UIGradient__ModifyMesh.c:148-149` + `LocalPositionMatrix.c:20-27`）· 换算 **φ = 90° − θ + 180°**。**数值 188 一直是对的** ⇒ 只改理由。
2. 🔴 **`UIScaleToFit` 在菜单 prefab 里有 183 处实例**（`fitInParent`+`targetRect`），我们一个都没有 —— 内容超框时（长文案弹窗、长卡名）观感会与原版不同。这是本轮新查出的**成规模缺口**（C·P2 已按行记）。
3. 🔴 **`EverguildTooltipTrigger` 241 处菜单实例**，原版在「点击/按下/选中/取消选中」都会收起 tooltip；我们只在指针离开时收 ⇒ 点一下带 tooltip 的钮后，tooltip 会**挂到指针离开**才消失（C·P2）。
4. **`UIImageMaterialColorChanger` 666 处 / `UIGroupTint` 306 处**：原版靠材质属性做**带时长的组染色**；我们只有顶点 tint 瞬切 ⇒ 渐变观感不同（C·P2）。
5. **页签红点原来全仓零消费者**（`res.badge[]` 只有建、没有刷新）—— 本轮接上后 **Campaign 才会亮**；其余四类定义照挂、无源恒 0（= 今天观感）。
6. `DragButtonInfoInt` **全解包 0 实例**（field 名 `clickedMessage`/`dragEnterMessage` 全库零命中）⇒ 死组件，D。

---

## ⑦ 跨账复核（⛔ 只读别人的账）

1. **E 报的「战役红点 `CampaignBadge` 0 消费者」** —— 本轮已补：provider + 页签定义 + 断言。**注意 E 的判据不完整**：原版那颗定义在 `CampaignRewardsButton` 上**是两条**（`CampaignBadge` + `MissionsBadge{parentId="CampaignMission"}`，`MonoBehaviour_-287428095701543169.json` 的 `references` 里写着），不是「只有 CampaignBadge」。
2. **C 报的 UIGradient 第四处（`DailyRewardPopup:266`）** —— 已按同一口径订正（只改注释、数值 188 不动）。
3. **G 账 `SceneInitializer` 16 条 + `ItemDrawer<T>` 8 条** —— 上轮已落地，建议改判 A（本轮未动它们）。
4. **`Shell/DeckRuntime.cs:1452`** 那处已由 C 改完（我未碰）。

---

**收工口径**：账 873/873 有状态（**A 332 / B 43 / C 182 / D 316 / E 0**）；B 全部写了「差什么」；三件必做（`OneText` 注释+断言 · `claimedWarning` 落账 · 战役红点）全做；四处 UIGradient 措辞订正；P1 修复 1 条（顶栏数字）；`typecheck` 0/0；无 Unity / 无 git / 无正本改动。
