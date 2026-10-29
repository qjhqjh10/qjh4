# B97_MINI2 报告（第九十六会话 · 2026-10-29）

> 两条活，都是调度台已裁的：`账/未分类_其他.tsv` 的 **`:312`**（走「形状派」）与 **`:430`**（「要做」）。
> 判据一律**现读**（`d:/2/新解包资源/assets_full/bundle_menus_assets_all` + `工具/menu_dump.py`），下面每个数都可复核。

| 账行 | 原版 | 本轮做了什么 | 状态 |
|---|---|---|---|
| `:312` | `GameObjectUniqueIdentifier.FindComponent` 那行缺的第 12 个注册点 | 在收藏窗**真建**载体节点 `Reference Card Pointer` 并登记唯一 id ⇒ 注册点 **12/12** | B → **A** |
| `:430` | `MilestoneScoreBar.Initialize` | 两扇宿主（Skirmish / Energy）**接真组件**（`MilestoneScoreBar` + 5 × `GenericScoreBarLine` + `Slider`），数据用本地模拟表，**画面零变化** | B → **A** |

---

## 一、`未分类_其他:312` —— `CardCollectionReferenceCardPointer`（**形状派**）

### 1·1 判据（一手现读，全部可复核）

`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "CardsTab" --depth 6 --no-layout`：

```
2      Reference Card Pointer    402.3  236.4  596.1  281.4  193.84  45.09   GameObjectUniqueIdentifier   字段: id
```

逐字段（`assets_full/bundle_menus_assets_all`）：

| 项 | 值 | 出处 |
|---|---|---|
| 父件 | `Collection Display`（与 `Scroll View` **同级**） | 上面的 dump 树（depth 2） |
| `m_IsActive` | **true** | `GameObject/Reference Card Pointer.json` |
| `m_Layer` | 5 | 同上 |
| 组件 | 只有 `GameObjectUniqueIdentifier`（`id = "CardCollectionReferenceCardPointer"`），**没有 Image、没有子件** | `MonoBehaviour/MonoBehaviour_2244189844683285205.json`（`m_Script` pathID `5370249714765181407`） |
| `RectTransform` | anchorMin/Max = pivot = (0.5,0.5) · anchoredPosition (−544.3845, 316.5719) · sizeDelta 193.839 × 45.0918 | `RectTransform/RectTransform_-1444448670045642027.json` |
| 绝对矩形 | **402.3,236.4 → 596.1,281.4** | 上面两条相加；与 `CollectionWindow.WcRefL/T/R/B` 逐位同值 |

🔴 **铁律 5 就地订正（这条是旧文档的错，已改在代码注释里）**：本仓两处注释写这颗节点「`m_IsActive = 0`」——
**那是卡组编辑窗那颗同名节点**：`GameObject/Reference Card Pointer_4160045545866129188.json`（`m_IsActive: false`）+
`RectTransform_6601596930717576996.json`（417.69,260.95 → 611.53,306.05，**一个组件都没有**）⇒ 就是
`Deck/DeckRuntime.PoolRef*` 那一组数。**本窗这一颗 active = 1**。
它在屏上照样看不见（没有 Image），所以 active 与否不影响画面；我们按原版取 **active = 1**。

### 1·2 改动（`Shell/CollectionWindow.cs`）

| 位置 | 内容 |
|---|---|
| `:1122-1132` | 新增 11 行注释（判据 + 上面那条订正），**插在 `var holder = Node(page, "Scroll View", CardsViewport);` 之后** |
| `:1133` | `var refPt = Node(page, "Reference Card Pointer", new PxRect(WcRefL, WcRefT, WcRefR, WcRefB));` |
| `:1134` | `if (refPt != null) MenuTutorialFlow.RegisterUnique("CardCollectionReferenceCardPointer", refPt);` |
| `:1101` | 就地订正旧注释里的 `m_IsActive = 0`（**行数不变**） |
| `:5114` | 同上（`WcRefL` 那条 doc）——**行数不变** |

- **矩形只有一份尺子**：建节点与 `CheckFocusedArmy()`（`:5128-5129` 的严格 AABB）共用 `WcRefL/T/R/B`，⛔ 没有第二份。
- **pivot/锚点**：`MenuDraw.Node` 恒写 (0.5,0.5)（这是本工程「无 uGUI 父矩形」下的既定口径，见 `MenuDraw.SetPxSize` 的 doc）——
  与原版这一颗**恰好一致**（原版也是 anchorMin=anchorMax=pivot=(0.5,0.5)）。
- **登记走 `MenuTutorialFlow.RegisterUnique`**（= 原版 `GameObjectUniqueIdentifier.Awake` 那一跳的本仓等价物）；
  它会先把同 id 的旧登记摘掉（本轮之后收藏窗会反复建/拆，避掉 `Duplicate Id` 的 LogError）。

### 1·3 影响面（给同步点）

- 本文件 **+13 行**（另 2 行是就地改写，行数守恒）⇒ `Shell/CollectionWindow.cs` 里**指向插入点之后的账落点会整体漂移**。
  现读该文件在账里被引用 **230 处** ⇒ 同步点必须跑 `check_ledger_points --all-A --fix`（由调度台跑）。
- `Editor/B94B18_Assert.cs:74/134-139` 的「**已知缺 / ⛔ 本仓没有这颗节点**」注释与那条 `LogWarning` 现在**过期了**
  （节点已建、`MenuUniqueIds.Find` 会命中）。⚠️ **该文件不在本代理白名单** ⇒ 只报告、没改；
  它那段是「不硬造的出声」，节点在 ⇒ 那条 warning 自然不再打印，**断言不会因此变红**（C 段判的是另外四条）。

---

## 二、`未分类_其他:430` —— `MilestoneScoreBar.Initialize`（两扇宿主接组件）

### 2·1 判据（prefab 现读）

`menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow" --depth 13 --relative --no-sprite`（Energy 同命令）：

```
2  Scoring Bar Event Score Info  <rect>  MilestoneScoreBar      字段: progressBar,scoringLines
3    Progress Bar                 <rect>  Slider                （字段里就有 m_FillRect/m_MaxValue/…）
4    Score Bar Line Level 1..5    <rect>  GenericScoreBarLine    字段: chestOpen,chestReward,readyToCollect,resourceIcon,score
5      Highlight Crate / Chest / Skull / (Skull ▸) Score
```

- `Score` 五颗的 TMP **出厂原文都是 `'1256'`**（`Skirmish` 与 `Energy` 两窗逐颗同值）。
- `Highlight Crate` 出厂 `act = 1`（金色 `Simple (0.98,0.801,0.0588,1)`）。
- 箱子贴图 `40k_Crate_Tier1..5_*.png` 全部 **512×512**（现读 PNG 头）。

### 2·2 改动

**新增文件 `Shell/MilestoneLocalSimulation.cs`（103 行）** —— 本地模拟表：
`CreateTable()`（5 档 · `score` 1256 · `state` Unlocked · 空 `rewards`）、`FromScore(...)`（原版 `RefreshScore` 那三条状态机的投影）、`LogOnce(...)`。
整段按铁律 18 打标：`// 🔶 自己设计（非原版）：… 服务器已关服，判据在远端 … 调度台 2026-10-29 裁定`。

**`Shell/SkirmishEventWindow.cs`（+57 / −3）**

| 位置 | 内容 |
|---|---|
| `:511-517` | 在 `Progress Bar` 节点上 `AddComponent<UnityEngine.UI.Slider>()`（原版这一颗就是 `Slider`） |
| `:518-519` | `BuildMilestones(bar); WireScoreBar(bar, barSlider);` |
| `:532` 起 `BuildMilestones` | 三颗 quad 改成 `var … = MenuDraw.Rect(...)` 取出句柄；每行末尾 `AddComponent<GenericScoreBarLine>()` + `Bind(score, chest, chestOpen, hiGO, skull)` → `_scoreLines` |
| `:1008` `WireScoreBar` | `AddComponent<MilestoneScoreBar>()` → `Bind(slider, lines)` → `Initialize(0, CreateTable(), null)` |

**`Shell/EnergySinglePlayerOnlyEventWindow.cs`（+104 / −38）**

| 位置 | 内容 |
|---|---|
| `:433-440` | 同上：`Progress Bar` 上建 `Slider` |
| `:491` | `WireScoreBar(bar, barSlider);` |
| `:554` 起 `BuildMilestones` | 同上：取 `hiQ/chestQ/skullQ` + 每行 `GenericScoreBarLine` |
| `:1203` `SetScore(...)` | **改走组件**：原版那条状态机投影成 `GenericScoringMilestone[]`（`FromScore`）→ `_scoreBar.Initialize(...)`；进度值改成**从 `Slider.normalizedValue` 读回**（⛔ 不再在本窗重算一遍 `InterpolateMilestones`）；`HasMilestoneToCollect` 直接用 `RewardProgressBarPanel.HasMilestoneToCollect(ms)`（原版那一条） |
| `:1701` `WireScoreBar` | 建 `MilestoneScoreBar` + `Bind` + `Initialize(0, CreateTable(), null)` + `PinChestAspect()` |
| `:1723` `PinChestAspect` | 见 §2·3 ③ |

### 2·3 **「画面必须与原来视觉等价」是靠这三条落地的**（⛔ 缺一条就会有静默的画变）

① **一个节点都没重建**。组件是 `AddComponent` 到**已经按 prefab 逐值建好的**那五件上，`Bind` 只存引用
（`score`/`chestReward`/`chestOpen`/`readyToCollect`/`resourceIcon`）；几何、父子链、渲染队列、字号全部没动。

② **模拟表的取值 = 镜像 prefab 出厂态**。`Initialize` 会把 `milestone.score` / `state` 写下去，所以表必须取
`score = 1256`（= 出厂 TMP 原文）、`state = Unlocked`（= 出厂 `Highlight Crate` act=1；`Unlocked` 支是**闭箱图 + 高亮开**，
与两窗今天画的东西逐件相同）、`currentScore = 0`（= 出厂 `Fill` 0 宽 ⇒ `InterpolateMilestones(0, [0,1256…]) = 0`）。
⛔ 换成一套「看起来更真」的阈值（100/300/…）= 屏上数字立刻变，那就不是视觉等价了。

③ **箱子的 `_aspect` 必须钉回框的比例**（这一条是本轮**唯一**发现会静默改画面的坑）：
`GenericScoreBarLine.SetChest` → `ImageQuad.SetTexture(tex)`（单参 = 原版 `image.sprite = x` 那一跳）会把
`_aspect` 拉成**贴图自己**的比例；而 Energy 的箱子框是按 prefab 矩形建的（`ChestW 104.49 × ChestH 93.20` ⇒ **1.1211**），
贴图是 **512² ⇒ 1.0** ⇒ `WorldW = WorldH × _aspect` 会让箱子**窄 10.8%**，**零报错**。
⇒ 新增 `PinChestAspect()`，在 `WireScoreBar` 与 `SetScore` 每次 `Initialize` 之后各钉一次。
（Skirmish 那扇不受影响：它的箱子是 `keepAspect: true` 建的，`_aspect` 本来就 = 贴图比例。）

### 2·4 残差（如实记）

- 原版第三扇宿主 **`Two Sides Event Window` 本仓还没建那扇窗**（`grep` 全仓无该文件）⇒ 不属于本批可接的范围。
- `Collect` 钮的 `interactable`：Skirmish 仍按 `ForceMilestoneStatesForTest`（生产恒 null ⇒ 不可点）、
  Energy 仍由 `SetScore` 的 `anyClaimable` 决定 —— **改前就是这样**，本轮**没动**（动了会改按钮外观）。

---

## 三、行数变化（现读 `git diff --numstat`）

| 文件 | +/− | 说明 |
|---|---|---|
| `Shell/CollectionWindow.cs` | **+15 / −2** | 净 +13 行（含两处就地改写的注释） |
| `Shell/SkirmishEventWindow.cs` | **+57 / −3** | 净 +54 行 |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs` | **+104 / −38** | 净 +66 行（`SetScore` 的等价族被组件调用替换掉一部分） |
| `Shell/MilestoneLocalSimulation.cs` | **新文件 103 行** | |
| `Editor/B97_MINI2_Assert.cs` | **新文件 247 行** | |
| 行尾 | 五份全部 **LF、0 个裸 CR**（`io.open(p,'rb')` 现数：`crlf=0` · `bareCR=0`） | |

---

## 四、typecheck

```
TMPDIR=/tmp/wf_mini2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

（中途有一次跑到别人的文件报 4 条 `RankedDivisionParticles.cs` / `RankedDivisionChangeWindow.cs` 的错 —— 那是当时在写的代理，
按纪律没去动；稍后重跑 **0/0**。）

---

## 五、账补丁

`账/_parts/B97_MINI2.tsv` —— **11 列 + 表头**，两行，键 = 主表那两行原键（`方法` 列带完整签名）：
`GameObjectUniqueIdentifier.FindComponent(string id)` · `MilestoneScoreBar.Initialize(int currentScore, GenericScoringMilestone[] scoringMilestones, Sprite resourceIcon)`。
两行都是「拿主表原文 + 追加/改列」生成，写盘后做过**逐列往返校验**（读回 11 列、逐行相等）。

- `python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py` → **✅ 没有跨补丁重复键（4 份补丁）**。
- ⚠️ 已归档的 `账/_parts/_merged_B97/B97_MISC2.tsv` 里也有这两行的**旧版本**（状态 B）——
  它已合并/归档、不在 `_parts/` 根下，所以整目录合并不会把它带回来；**合并本补丁 = 两个键 → A**。

---

## 六、待接线（断言宿主）

新建 `CardPresentation/Editor/B97_MINI2_Assert.cs`（`B97MINI2Assert`）。**本代理不改宿主**，请在
**`CardPresentation/Editor/ShellScene.cs` 的 `Run()` 那一行（现读 `:11023`）**里：
紧挨着 `B97MISC2Assert.Run(); _sink.Pass += B97MISC2Assert.Pass; _sink.Fail += B97MISC2Assert.Fail;` **之后追加**：

```csharp
B97MINI2Assert.Run(); _sink.Pass += B97MINI2Assert.Pass; _sink.Fail += B97MINI2Assert.Fail;
```

（**同一行内追加 ⇒ 行数零变化**；⛔ 不要接在 `Editor/RuleEngineTest.cs`。）

断言分三段：
- **A**（`:312`）：尺子常量 402.3/236.4/596.1/281.4 · 建一棵收藏窗 → Cards 页里节点存在 · `sizeDelta×108 ≈ 193.84×45.09`
  · 世界位置 = 矩形中心 · `MenuUniqueIds.Find("CardCollectionReferenceCardPointer")` 往返。
  建窗/建页抛异常会记一条**红**（⛔ 不静默跳过）。它建的窗在 `finally` 里 `DestroySafe`。
- **B**（`:430`）：模拟表口径（1256 / Unlocked / 非空 rewards）· 用同形 5 行树喂组件后
  `Score` 文本 = `1256` · 箱子仍是出厂闭箱图 · 高亮仍开着 · `slider.maxValue = 1256` · `normalizedValue = 0`
  · `FromScore` 三档状态机（Collected / Unlocked / Locked）。
- **C**：两扇宿主的 `_scoreBar` 字段 / `WireScoreBar` 方法 / `ScoreBarForTest` 读口 / `PinChestAspect` 的存在（结构钉）。

---

## 七、没查清 / 顺手发现

1. 🔴 **`m_IsActive` 那条旧结论是错的**（见 §1·1）：本窗这颗是 `true`，`0` 的是卡组编辑窗那颗同名节点。
   本文件两处注释已就地订正；**别处若还有「`Reference Card Pointer` 的 `m_IsActive=0`」的说法，同样要订正**
   （我 `grep` 过：本批落点文件内只有这两处）。
2. **Energy 那扇窗的 `Highlight Crate` 出厂是「亮着」的（不透明金）**，而 `SkirmishEventWindow` 那扇是 alpha 0（那句注释自己写着「这一条是我们挑的」）——
   **两扇窗今天不一致**。本轮按「视觉等价」原样保留（`state = Unlocked` 对两扇都是零变化），
   ⛔ 没有借这次机会统一；要不要统一由调度台定（那会改画面，不属于本条账）。
3. **`GenericScoreBarLine` 换箱子图时不保比例**（`SetTexture(tex)` 单参）—— 这对「非 1:1 的箱子框」是个静默坑，
   本轮用 `PinChestAspect()` 在**调用点**兜住；若将来别处也接这个组件，同样要钉比例（或给组件加一个 `keepAspect` 的绑定参数）。
4. **`Editor/B94B18_Assert.cs` 的那段「已知缺」说明过期了**（§1·3）。没改（文件不在白名单）。
5. **没查清的**：原版 `Two Sides Event Window` 那扇窗我们还没建 ⇒ 那份 `MilestoneScoreBar` 的宿主接线**无从落**；
   以及 `GenericScoringMilestone[]` 的**真值**（远端 LiveOps，关服后本地永远拿不到）。
