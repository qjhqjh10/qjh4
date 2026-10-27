# B9_W36SHINEA · 扫光（Shiny）断言落地 ＋ B/D 宿主定位 ＋ Deck 万能卡计数条那颗的参数定案

> 执行代理 `B9_W36SHINEA` · 2026-10-27 · 有写权限
> **全程未跑 Unity（含不编译 shader）· 未动 git · 未改正本/账主表/别人的报告/基线**
> 每改一次 `.cs` 都跑过 `TMPDIR=/tmp/wf_w36 bash 工具/typecheck.sh` ⇒ **最终 0/0**（中途一次红：我在
> `$"…{army = …}…"` 里漏了转义花括号，`CS0103 ×2`，当场改掉）
> 白名单外**一个字没动**（`Deck/**`、`Shell/LeaderboardWindow.cs`、`Shell/DailyRewardPopup.cs`、`Shaders/` 全没碰）

---

## 一、结论（三件，逐条）

| # | 任务 | 结果 |
|---|---|---|
| 1 | **落 4 条断言**（报告 §三 口径） | ✅ **三个宿主共 77 条 `Check*` 语句**（`CollectionScene` 36 · `DeckScene` 26 · `RewardsScene` 15；循环体里那几条按格子数各执行一次）。**条条先写前提硬检查**（`Shader.Find` / 层建没建 / 材质挂没挂 / 数据侧期望集合非空），⛔ **没有一处 `if (前提) Check(结论)`** |
| 2 | **定位 B/D 两处宿主** | **D 查到**（`Army Item Button` = **排行榜三扇 + 每日奖励弹窗** 里的 `Army Selector` 军种格）· **B 查不到**（`Army Item Button No Arrow Variant` 在**本机全库零引用** ⇒ 原版死资源）。两处**都在白名单外** ⇒ 给改动请求，⛔ 没动代码 |
| 3 | **Deck 编辑窗 `WIldcard Counter ▸ Background`** | ✅ **参数查清了，而且报告原来的那组是错的**：这颗与**收藏窗那颗逐字段相同**（120 / 0.25 / 0.5 / (1,1,1,1) / filter 6 / **0.75 s** / **Once**）；`139 / 0.213 / 0.533 / (2,2,2,1) / 1.0 s` 那套**属于 `Message Container ▸ Content`**（一个新的、任何清单里都没有的实例）。⇒ 挂法 = `Params.WildcardDisplay` **一行**。落点在 `Deck/**`（W31/W34 正在写）⇒ 改动请求 |

**三句话**：

1. **断言里最有价值的是「前提硬检查」这一半**：本波最爱踩的坑是「shader 取不到 ⇒ 层是 null ⇒ 断言静默跳过」。
   三处都把 `Shader.Find(...) != null`、`Rate >= 0f`（`-1` = 层没建）、`doneMat != null` 写成**独立断言**。
   ⇒ 同步点真机上 shader 若没进包，**红的是前提那几条**，不是「全绿但画面没扫光」。
2. **B 是原版死资源**（本机证据链三条 grep 全空），**D 有真宿主**。D 的语义是「**该格被选中才扫**」
   （`ArmyItemContainer__Click.c`：`+0x38` 的 toggler 跟着 `SetActive(highlight, on)` 同一分支走）。
3. **Deck 那颗的参数结论顺手推翻了账里 W64 表**：那套 139 的值**不是 Deck 的**。详见 §五·1。

---

## 二、证据（文件:行号 / 资产路径）

### 2·1 断言落点（本件新加）

| 宿主 | 行 | 内容 |
|---|---|---|
| `Editor/CollectionScene.cs` | `:12629-12773` | `B9 收藏:348` 扫光：shader 前提 / 层前提 / 9 块同一份材质 / shader 名 / 原版参数 / `_ShineFrame`+`_ShineRot`（独立算世界并集）/ 0→0.6667→1 的进度 / Once 自停 / **Neutral 闸 + 反证**（`RebuildCardsPageWithArmyForTest`） |
| `Editor/DeckScene.cs` | `:7015-7094` | `B9 实例 F`：前提三道 / 合法⇒开 / 从 0 起 / 材质身份 / 颜色 **3.4822** / 带宽 0.25 / 软边 0.5 / 角度 120° / **Loop 走 1.10 s 绕回 0.10** / 不合法⇒关（rate=1）/ 关掉时材质仍在原位 / 补回⇒又开 |
| `Editor/RewardsScene.cs` | `:12343-12451` | `B9 收藏:15`：**锻造页 13 格的过滤**（期望集合从 `ForgeData.HasToCollect` 独立算）/ **战役页只有选中那颗亮**（期望 = `CampaignData.Selected` + `Claimable` 前提）/ 两个**不带过滤的页签红点**作对照组 |
| `Shell/CollectionWindow.cs` | `:6462` / `:6469` | 两个自检读口：`WildcardShinePlayingForTest`（分「没播」与「播完了」）、`RebuildCardsPageWithArmyForTest(army)`（把 `_wcArmy` 置成 Neutral 再重建页） |

### 2·2 D（`Army Item Button`）的宿主链（AssetRipper 导出的 YAML，`armyItemButton` 字段）

`grep -rn "armyItemButton:" d:/2/tools/assetripper_out/ExportedProject/Assets --include=*.prefab` 的结果**正好把三种军种格分开**：

| `armyItemButton` 指向 | = 哪个 prefab | 被谁用 |
|---|---|---|
| `da958ae8466b90a458a216398e270d0f` | **`Army Item Button`（D）** | `Daily Reward Popup.prefab:28397` · `DraftLeaderboardPopup.prefab:2825` · `RankedSkirmishLeaderboardPopup.prefab:3252` · `RankedClassicLeaderboardPopup Variant.prefab:2611` · `Army Selector.prefab:153` |
| `d76040de984503d4aad8ac9846879290` | `Campaign Army Item Button`（C） | `Campaign Tab.prefab:1367` · `Campaign Army Selector.prefab:172` · `Rewards Base Submenu Variant.prefab:71683` |
| `75d3a885c42b2164abb9e8c581670394` | `Forge Army Item Button`（G） | `Forge Tab.prefab:15739` · `Forge Army Selector.prefab:154` · `Rewards Base Submenu Variant.prefab:68300` |

**D 的语义**（`d:/2/tools/decomp_full/ArmyItemContainer__Click.c`，逐句）：
`SetActive(*(this+0x30) /*highlightState*/, on)`；`on == 0` ⇒ `(**(code**)(*plVar1+0x178))(plVar1, 0, 0, …)`（= toggler 的 `Toggle` 虚调，**关**）；
`on != 0` ⇒ 先回调 `+0x50`（选中事件），再 `(**(code**)(*plVar1+0x178))(plVar1, 1, …, …)`（**开**）
⇒ **扫光 ⟺ 这一格被选中**（不是常亮、不是悬停）。

**我们工程里对应的两颗宿主（白名单外 ⇒ 改动请求）**：

| 窗口 | 落点 | 现在怎么建 |
|---|---|---|
| 排行榜三扇（`RankedSkirmish` / `RankedClassic Variant` / `Draft`） | `Shell/LeaderboardWindow.cs:518` `RebuildArmyButtons()` → `:555` 那颗 `"Icon"` | `bool on = army == SelectedArmy;`（`:535`）时建 `HighlightBG`/`Arrow`；**Icon 恒建** ⇒ 就在 `:555` 之后按 `on` 挂/播 |
| 每日奖励弹窗 | `Shell/DailyRewardPopup.cs:752` `BuildArmyCell(...)` → `:765` `Offset(AI_Icon, r)` | `bool on = DailyData.HeaderArmyValue == army;`（`:754`）⇒ 同上 |

### 2·3 B（`Army Item Button No Arrow Variant`）零引用（三条独立 grep 全空）

1. **同族字段**：`armyItemButton:` 的**七处**全部指向 C/G/D 三者之一（上表），**一条指向 B 都没有**。
2. **GUID**：`grep -rl 36ba919855e657c418a4a0588b25bcf0`（B 的 prefab GUID，取自它的 `.prefab.meta`）在
   `ExportedProject/Assets` 只命中它自己的 `.meta`；在 `d:/2/新解包资源/assets_full`（24.7 万文件）**0 命中**。
3. **组件 pathID**：B 根 GameObject（`6111991147133688144`）的三个组件
   （`2683003296789649744` / `2055156237352524112` / `3030960862266546512`）在 `assets_full` 里
   除了 B 自己的 JSON 之外**没有任何文件引用**。
4. 连带：**`Army Selector.prefab`（`6325de36127c97d408df41b656bb25e4`）自己也是零引用** —— 但它的 `armyItemButton`
   指向 D、且三扇榜各自内联了同名 `Army Selector` 节点（`LeaderboardWindow` 文档里那三条 `m_Father` 实证），
   所以 D 的宿主不受影响。

### 2·4 Deck `WIldcard Counter ▸ Background` 的**真值**（现读，全部一手）

| 对象 | pathID | 值 |
|---|---|---|
| `Deck Editing Menu` 的 `WildcardDisplay` MB | `6030196207654661924` | 它的 `shinyEffect` → `2688218479952654116`（tweener） |
| 父链 MB（**证它真是 Deck 编辑窗**） | `5918047927837519652` | 字段 = `editingPanel` / `cardDisplay` / `cosmeticDisplay` / `cardDrag` / `cosmeticDrag` / `stateController` / `ownedToggle` ⇒ **`DeckEditingWindow`** |
| 那颗 Background 的 `UIEffect` MB | `3594646202839789348` | rotation **120** · width **0.25** · softness **0.5** · color **(1,1,1,1)** · filter **6** · glow 0 · rate 1.0 · reverse 0 · keepAspect 1 · autoPlay 0 · texScale (1,1) / offset (0,0) / speed (0,0) |
| 同 GO 的 `UIEffectTweener` MB | `2688218479952654116` | duration **0.75** · **wrap `0` = Once** · delay 0 · playOnEnable 0 · resetTimeOnEnable 1 |
| 对照：收藏窗那颗 | UIEffect `-3693768431965379883` · tweener `1791554316153643733` | **逐字段完全相同**（120 / 0.25 / 0.5 / 白 / 0.75 / Once） |

⇒ 挂法 = `WildcardShineLayer.Params.WildcardDisplay`（**已存在**，一字不用加）。

### 2·5 `139 / 0.213 / 0.533 / (2,2,2,1) / 1.0 s` 那套的真实归属

| 事实 | 判据 |
|---|---|
| 全库只有 **1 个** `m_TransitionRotation == 139` 的 `UIEffect` | `assets_full/*/MonoBehaviour/*.json` 全扫：`MonoBehaviour_-7763153431244756047`（GO `-2571570060004938831`，width 0.213 · softness 0.533 · color (2,2,2,1)） |
| 同 GO 的 tweener = **1.0 s / Once** | `MonoBehaviour_-3271889046122976335`：duration 1.0 · wrap 0 · delay 0 · playOnEnable 0 · reset 1 |
| 它的父链 = **`Message Container ▸ Content`** | `R0_资源覆盖账.tsv:114481`：`Content <- RectTransform#8749932977937198001 <- RectTransform#955468390103264177 <- Message Container`；同链的组件 `MonoBehaviour#-3271889046122976335 <- MonoBehaviour#-9188051066216764495 <- Message Container`，而 `-9188051066216764495` 的脚本是 **`MessageContainer`**（`R0:8095`） |

⇒ 那套值**不属于 Deck 编辑窗**，属于消息窗（`MessageContainer`）的 `Content` 节点。

---

## 三、逐条断言（断什么 / 改坏哪处会红 / 灭自证吗）

### 3·1 `Editor/CollectionScene.cs`（`收藏:348`，36 条）

| # | 断什么 | 改坏哪处会红 | 灭自证 |
|---|---|---|---|
| 1 | `Shader.Find("CardPresentation/WildcardShine") != null` | 把 shader 挪出 `Resources/`（或删 `.meta` 后未导入）⇒ 红 | 前提 |
| 2 | `win.WildcardShineRate >= 0`（`-1` = 层没建） | `AttachNine` 返 null ⇒ 红（**不许**把它当「没播」） | 前提 |
| 3 | `Background` 下 **9 块** | 改成 `MenuDraw.Rect` 拉伸 / 单块 ⇒ 红 | — |
| 4 | 9 块 `sharedMaterial` **同一对象**（`ReferenceEquals`） | 退回「每块各算一份仿射/各自一份材质」⇒ 红 | **是**（结构不可兼得：9 块同一份 ∧ 每块一份不可能同时成立） |
| 5 | 9 块材质 shader 名全是 `CardPresentation/WildcardShine` | 没挂上 / 退回 `Sprites/Default` ⇒ 红 | **是**（与 #4 并排：共享一份错的材质也照红） |
| 6 | `_TransitionWidth 0.25` · `_TransitionSoftness 0.5` · `_TransitionColorFilter 6` · `_TransitionColor = 白` | 抄成 Deck Done 那颗（3.4822）/ 套错实例 ⇒ 红 | 期望值 = **原版 prefab 现读** |
| 7 | `_ShineFrame.xy` = 9 块**世界中心**、`.zw` = 世界宽高 | `Reframe()` 没调 ⇒ 读到默认 `(0,0,1,1)` ⇒ 红 | **是**（并集在本段**独立算**，不回读 `FrameBounds`） |
| 8 | `_ShineRot.x/y = cos/sin(120°) = (−0.5, 0.8660)`、`.z = 1/max(W,H)` | 角度参数抄错 / 归一换一档 ⇒ 红 | 一半原版参数、一半独立算 |
| 9 | 建页那一拍 `rate == 0` ∧ `Playing == true` | 删掉 `Play(true)` ⇒ rate 停 1 ⇒ 红；把 `Rate` 单独当结论 ⇒ 会被「播完」蒙混（所以两条并排） | **是**（「没播」与「播完了」用两条拆开） |
| 10 | `Step(0.5) ⇒ 0.6667`、`Step(0.25) ⇒ 1` ∧ `Playing == false` | 时长套成 1.0 s（Deck 那颗）⇒ 0.5 ⇒ 红；接成 Loop ⇒ `Playing` 仍 true ⇒ 红 | **是**（Once 那半） |
| 11 | **Neutral ⇒ 不播**（`Playing == false` ∧ `rate == 1` ∧ 再推进 0.2 s 仍 1） | 删掉 `BuildWildcardShine` 里那道 `IsNeutral` 闸 ⇒ 红 | **是**（「推进也不动」把「播完了」排除掉：播过 0.2 s 会是 0.267） |
| 12 | **非 Neutral ⇒ 又播了**（收尾）且 `rate` 回 0 | 把闸写成恒关 ⇒ 红 | **是**（#11 与 #12 结构上不可能同时被「恒开」或「恒关」满足） |

### 3·2 `Editor/DeckScene.cs`（实例 F，26 条）

| # | 断什么 | 改坏哪处会红 | 灭自证 |
|---|---|---|---|
| 1 | shader / `DoneShineRate >= 0` / `foot_done` 上有 `ImageQuad` 三道前提 | 取不到 ⇒ 红 | 前提 |
| 2 | 合法 ⇒ `on == true` ∧ `rate == 0` | 删掉 `FootShine(err == DeckError.None)` ⇒ 红；删 `Play(true)` ⇒ rate 停 1 ⇒ 红 | — |
| 3 | `foot_done` 的材质 shader 名 = `WildcardShine` | 被 `WindowButton.RefreshGray` 换回 `Sprites/Default` 且没重挂 ⇒ 红 | — |
| 4 | 颜色 **r/g/b = 3.4822** · 带宽 0.25 · 软边 0.5 · 角度 120° | 套成收藏窗那颗（白 0.75 s）⇒ 红 | 期望值 = 原版 prefab 现读 |
| 5 | `StepAll(0.25) ⇒ 0.25`；`StepAll(0.85)`（累计 1.10 s）⇒ **绕回 0.10** ∧ 仍 `on` | `loop` 改 false ⇒ 停 1.00 ⇒ 红；没帧泵也没 `StepAll` ⇒ 停 0.25 ⇒ 红 | **是**（三个可能终值各不相同，一次读数能分开） |
| 6 | 不合法 ⇒ `on == false` ∧ `rate == 1` | `FootShine` 写成恒 `true` ⇒ 红 | **是**（与 #2 并排：恒开/恒关都不行） |
| 7 | 关掉时**材质仍在原位**（`ReferenceEquals(sharedMaterial, doneMat)`） | 改成「换图/隐藏」实现「关」⇒ 红 | 语义断言（原版是 `Behaviour.enabled=false`） |
| 8 | 补回一张 ⇒ 又开、又从 0 起 | 闸写成恒关 ⇒ 红 | **是** |

### 3·3 `Editor/RewardsScene.cs`（`收藏:15`，15 条）

| # | 断什么 | 改坏哪处会红 | 灭自证 |
|---|---|---|---|
| 1 | 前提：`Army Content` 取得到 · 核到 ≥8 颗 · 期望集合非空 · **期望集合 ≠ 全部** | 夹具变成「全亮」时**前提先红**（否则「13 颗全亮」也能蒙过） | 前提 |
| 2 | 锻造页：**每颗格子的 `CurrentCount>0` 与 `CurrentAlpha≈1` == 该阵营自己有没有可领**（期望从 `ForgeData.HasToCollect` 独立算） | `ForgeBadge.Validate` 丢掉 `army` ⇒ 每颗都数到全体总数 ⇒ **10 颗假亮** ⇒ 红；把它写成恒 false ⇒ 该亮那颗也灭 ⇒ 红 | **是**（两个方向都红） |
| 3 | 战役页：**只有 `CampaignData.Selected` 那颗亮**，且亮的颗数 **恰好 = 1** | `CampaignBadge.Validate` 丢掉 `army` ⇒ 12 颗假亮 ⇒ 红 | **是**（同上；且期望来自 `Claimable` 而不是红点系统） |
| 4 | 对照：**不带过滤**的页签红点 = 全阵营可领总数（锻造 3 / 战役 ≥1） | 把过滤实现成「关掉 provider」⇒ 对照组归 0 ⇒ 红 | **是**（这条专门挡「把 provider 关掉也能让格子全灭」那种假绿） |

### 3·4 为断言加的两个读口（`Shell/CollectionWindow.cs:6462/6469`）
- `WildcardShinePlayingForTest` —— 只读（`_wcShine.Playing`）。
- `RebuildCardsPageWithArmyForTest(int army)` —— **自检专用、会在文档里写死「⛔ 生产别调」**；绕过 `SetWildcardArmy`
  是因为那条对 `Neutral` 会在「认不出阵营 ⇒ 保留当前那张」处 return（`ArmyIcons.IconName(0) = null`）。

---

## 四、没查清的部分（「查不到 + 还差什么」）

1. **B 会不会是「Addressables 按地址取」的？** 本机判不了。
   已核：本机 `catalog.bin`（`d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/catalog.bin`，1.3 MB）
   按**名字**与**GUID 的两种字节序**搜 B，全 0 命中；`armyItemButton` 字段也全指向 C/G/D。
   **还差什么**：远端 Addressables catalog（菜单族包是远端下发的，本机 `com.unity.addressables` 缓存是空的 ——
   `MEMORY: warpforge-live-game-verification` 已记「一个字节都没下到」）。没有它，**「原版死资源」只能是强证据、不是铁证**。
   ⛔ 但**按 铁律 11 例外① 记「原版本身没有使用点」是站得住的**（三条独立证据都空，且同族字段把三种格分得干干净净）。
2. **场景实例上的 prefab 覆盖还有没有第二套值？** 只核到 prefab 默认值。
   `bundle_scenes_scenes_mainmenuwarpforge` 的导出**命名混用**（一部分 `m_Name.json`、一部分 `GameObject_<pid>.json`），
   `WIldcard Counter` 的三个孩子里 GO `342` **根本没导出** ⇒ 那条链走不下去。
   **还差什么**：用 UnityPy 直接从原 bundle 取 `Content` 节点上的 `UIEffect`（按 GO pathID），
   或真机 `MenuDump` 一次。⚠️ 但**Deck 编辑窗那颗是 prefab 内的实例**（父链 MB `5918047927837519652` = `DeckEditingWindow`），
   而 `DeckEditingWindow` 在菜单包里是**窗口 prefab**、不是场景实例 ⇒ 本件结论（120/0.25/0.5/白/0.75/Once）**够用**。
3. **shader 一次都没编译过**（红线：本波不许跑 Unity）。断言只保证「取得到 + 参数写进去了 + 名字对」；
   **编不过 ⇒ `HasProperty` 那两条前提红**（我特意用 `HasProperty` 兜这一档，而不是直接 `GetFloat` 吃默认 0）。
4. **`Resources/Shaders/WildcardShine.shader` 没有 `.meta`**（调度台刚把它从 `Shaders/` 移过来）。
   本机 `Resources/Shaders/` 另外两个 shader 都有 `.meta`。⇒ **同步点第一次跑 Unity 时会先生成 `.meta` 并编译**；
   若那次编译失败，断言会红在前提与 `HasProperty` 上（不会静默）。⚠️ `.meta` 该由 `.cs` 写手之外的人签入，
   **本件没建**（不在白名单，也不该我造 GUID）。

---

## 五、改动清单

| 文件 | 动作 | 说明 |
|---|---|---|
| `Assets/CardPresentation/Editor/CollectionScene.cs` | +146 / −0 | `:12629-12773` 新增 `Section("B9 收藏:348 …")` 整块 |
| `Assets/CardPresentation/Editor/DeckScene.cs` | +220 / −0 | `:7015-7094` 新增 `Section("B9 实例 F …")`（插在 A330 那一节之后 = 卡组已知合法处，末尾复原） |
| `Assets/CardPresentation/Editor/RewardsScene.cs` | +110 / −0 | `:12343-12451` 新增 `Section("B9 收藏:15 …")`（`RunCore` 末尾、`win.Dump()` 之前） |
| `Assets/CardPresentation/Shell/CollectionWindow.cs` | +70 / −1 | 类尾两个自检读口（见 §3·4） |
| `Assets/CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs` | 1 行**就地改写**（零行数变化） | `:155` 那条「Deck 那颗不是同一套（139/0.213/…）」**是错的** ⇒ 改成「逐字段相同 + 139 那套属 `Message Container ▸ Content`」并指到本报告（铁律 5） |

**行尾**：改完逐个二进制核过 —— `CollectionWindow/CollectionScene/RewardsScene/UIEffectTogglerCoffeeEffects` 全 LF、`DeckScene` 全 CRLF（HEAD 也是 CRLF，10074 → 10294 = 正好我这 220 行），**裸 CR = 0**。

**没动**：`*.unity` · `ProjectSettings/` · `Shaders/` / `Resources/Shaders/WildcardShine.shader` · `Deck/**` · `Shell/LeaderboardWindow.cs` ·
`Shell/DailyRewardPopup.cs` · `CLAUDE.md` / `项目任务.md` / 任务总表 · 账主表 · 别人的报告 · 基线文件 · git。

### 5·1 改动请求（三件，落点都在白名单外）

**① D（`Army Item Button`）—— 排行榜三扇**
- 文件 `Shell/LeaderboardWindow.cs`（`:518` `RebuildArmyButtons()`）：在 `:555` 建完 `iconQ` 之后加
  `if (on) { _armyShine ??= ... }` —— 具体：加一个 `Dictionary<string, WildcardShineLayer> _armyShine`（同 `ForgeTab._armyShine` 的形状），
  建格时 `AttachQuad(iconQ, WildcardShineLayer.Params.Icon)`（**C/G/D 三处参数同族，见 `B9_W5COF` §2·6**），**只在 `on` 时 `Play(true)`**、`on == false` 时 `SetEnabled(false)`。
- 判据：`decomp_full/ArmyItemContainer__Click.c`（选中 ⇒ toggler `Toggle(1)`；取消 ⇒ `Toggle(0)`）+ 参数表 `B9_W5COF` §2·6。
- ⚠️ 重建是**整批销毁再建**（`MenuDraw.ClearChildren`）⇒ 字典每次 `Clear()`（照 `ForgeTab.BuildArmyItems` 的写法）。

**② D —— 每日奖励弹窗**
- 文件 `Shell/DailyRewardPopup.cs`（`:752` `BuildArmyCell`）：在 `:765` 建完 `Icon` 之后，`if (on)` 挂 + `Play`（`on` = `:754` 那一句，= `DailyData.HeaderArmyValue == army`）。
- 参数同上（`Params.Icon`）。

**③ Deck 编辑窗 `WIldcard Counter ▸ Background`（= 本件把参数定案的那颗）**
- 文件 `Deck/DeckRuntime.cs`：`:1693` 的 `MenuDraw.Nine(...)` 拿到 `wcBg` 之后（`:1697` 那句 `_cardsInfoOnly.Add(wcBg)` 旁边）加
  一份 `_wcCounterShine = WildcardShineLayer.AttachNine(wcBg.transform, WildcardShineLayer.Params.WildcardDisplay);`
- **播放时机**（原版 `WildcardDisplay.OnEnable` 那一拍）：`SetTab(int t)`（`:7059`）里 —— `_tab != 2`（Cards / Deck info 两页显示这条，Cosmetics 页整条关着）
  且 `!ArmyIcons.IsNeutral(State.Army)` 时 `Play(true)`。
- **参数已定案**：与收藏窗那颗**逐字段相同** ⇒ **直接用 `Params.WildcardDisplay`，⛔ 不要新建 139 那套预设**（那套属消息窗）。
- ⚠️ 与 `DoneShine`（`FootShine`）同一个坑：`hdr_wcbg` 若被 `WindowButton.RefreshGray` 换走材质 ⇒ 该亮时 `StillAttached` 为假要重挂。

---

## 六、顺手发现（请调度台分流）

1. 🔴 **账/W64 表那一套「139 / 0.213 / 0.533 / (2,2,2,1) / 1.0 s = `Deck Editing Menu ▸ WIldcard Counter`」是错的**
   （`报告/B9_W7COLL_收藏与卡面剩余C.md` §K-13 第 2 张表的右列；`报告/B9_W16SHINE_扫光统一.md` §四·5 / §六 旁注继承了它）。
   现读证据（§2·4 / §2·5）：Deck 那颗 = **120 / 0.25 / 0.5 / 白 / 0.75 s / Once**（与收藏窗逐字段相同），
   而那套 139 的值属 **`Message Container ▸ Content`**。
   - ✅ 我改了**我自己那份文件**里的同一句（`Effects/UIEffectTogglerCoffeeEffects.cs:155`，铁律 5）。
   - ⛔ **没改别人的报告**（红线）⇒ 请调度台就地订正那两处。
   - ⚠️ 连带：**`Message Container ▸ Content` 这颗扫光在任何清单里都没有**（W16SHINE 的「7+1」表里也没有）⇒
     它可能是一个**第 9 个实例**。我们工程里有没有 `MessageContainer` 窗、要不要接，**另立一条账**。
2. **`Army Selector.prefab` 与 `Army Item Button No Arrow Variant.prefab` 都是零引用资源**（§2·3）。
   前者被三扇榜「内联复制」了（同名节点），后者**没有替代物** ⇒ 按 铁律 11 例外① 记「原版无使用点」，
   ⛔ 别把它当「我们没复刻」。
3. 🆕 **`Resources/Shaders/WildcardShine.shader` 缺 `.meta`**（同目录另两个 shader 都有）。同步点第一次跑 Unity 会生成；
   若那份 `.meta` 要签入，请调度台在**没有 `.cs` 写手**时做（本件不造 GUID）。
4. **`WildcardShineLayer` 的三处宿主形状不一样**，改动请求①③别抄错：
   - `CampaignTab`/`ForgeTab`/`LeaderboardWindow` 是**整批重建**（`ClearChildren` ⇒ 字典要 `Clear()`）；
   - `DeckRuntime.FootShine` 是**惰性单件 + `StillAttached` 重挂**；
   - `CollectionWindow` 是**建页那一拍**（`BuildWildcardShine`，每次建页换一层，旧层靠 `StepAll` 的 `PruneDead` 回收）。
5. **断言口径提醒（给下一个接手的人）**：`Rate == 1` 有**两种**含义（没播 / 播完了）；本波所有「有没有在播」的判定
   一律 `Rate` + `Playing` **两条并排**（`CollectionWindow.WildcardShinePlayingForTest` 就是为此加的）。
   加新的扫光断言时 ⛔ **别只断 `Rate`**。

---

## 七、交接给下一个代理（本件无半成品）

- 三件都做完了（B 的宿主定位除外，见 §四·1）；`typecheck` **0/0**；未跑 Unity。
- 同步点必做（本件做不了）：① 让 Unity 编一次 `WildcardShine.shader`（**从未编译过**）；
  ② 三个宿主按铁律 12 跑 `CollectionScene.Run` + `DeckScene.Run` + `RewardsScene.Run`；
  ③ `check_ledger_points --all-A --fix`（本波在 3 个 Editor 宿主里**加了行**：`CollectionScene` +146 · `DeckScene` +220 ·
  `RewardsScene` +110 ⇒ 那三个文件的行号引用会整体下移；`Effects/UIEffectTogglerCoffeeEffects.cs` **零行数变化**）。
- 首次跑若红，**先看红在哪一半**：前提那几条（shader/层/材质）红 = shader 没编过/没进包；
  「9 块材质」「过滤」那些红 = 实现退化。⛔ 别先改断言。
