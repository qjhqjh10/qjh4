# WL2 · A251-L2+L4（四扇小窗）

> 现核时刻：**2026-10-13** 会话 · 写手 **WL2**（批次「清空 A 表」第四轮）
> 代码根 = `d:/4/Unity/MyGame/Assets/CardPresentation/` · 资料根 = `d:/4/Unity/资料/`
> 本件**没跑 Unity**、**没动 git**、**没改 `d:/2/`**、**没碰 `工具/*`**（判据一律现读）
> ⚠️ **判据一律现读**：四份 prefab 逐个跑 `menu_dump --relative` + 逐颗 MB 原文实读 + 反编译逐句 ——
> 本文里**没有一格是推的**（唯一两处「我们算的」逐字标出，见 §二·补 与 §九）

---

## 一、结论

- **四扇全建起来了**（`Shell/` 下四个新文件，**纯新增、没有翻行尾**）：
  `GenericOptionsPanel.cs`(552 行) · `AllianceMemberOptionsPopup.cs`(518) ·
  `PurchasePremiumWindow.cs`(826) · `RankedRewardEventWindow.cs`(605)
- **逐节点照原版**：**8 / 22 / 34 / 17** 个节点，名字 / 层级 / 矩形 / 字号 / `m_fontSizeBase` / 对齐 /
  颜色 / 九宫 / 出厂显隐**逐条**照 `menu_dump` 现读的字段。
- **注册进 `Shell/WindowsManager.cs`**：4 条 `PrefabRef*` 常量 + 4 个 `Open*` 方法（**只加了我这四扇**，
  ⛔ 没动 L1 的 `BaseOfferPopup` 那 39 行）。
- **断言**：`Editor/ShopScene.cs` 新加一节（**2 个 hunk：+21 行（一个 `CheckHasKids` 帮手）+ 247 行**），
  **104 处 `Check*` 调用点**（`ShopScene.cs:3895-4141`）。宿主 = `ShopScene.Run`（同族既成宿主，见 §七·0）。
- **可复现对账**：`_tmp_view/wl2/check_table.py`（纯 python）把四张 `Recon` 表与四份现读**逐格比**
  ⇒ **80 项对账 · 不符 0**（其中 `Member Options Panel` 22 项、`Purchase Premium Window` 33 项、
  `Generic Options Panel` 8 项、`Ranked Boost Reward Event Window` 17 项）。
- **类型检查**：`TMPDIR=/tmp/wf_wl2 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（见 §十一）。

---

## 二、四扇各自：建了什么（文件 · 节点数 · 逐节点判据出处）

**统一判据命令**（每条都逐份跑过；`--relative` 那一档是本文件里所有矩形常量的来源）：

```bash
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<prefab 根名>" --depth 12 --relative --md
```

| # | 窗口类（文件） | prefab 根名 | 节点 | 关键判据（除上面那条命令外） |
|---|---|---|---|---|
| L2-① | `GenericOptionsPanel`（`Shell/GenericOptionsPanel.cs`） | `Generic Options Panel` | **8** | `GenericOptionsPanel__{Start,Open,SetTitle,SetButtons,SetPosition}.c` · 窗口 MB `MonoBehaviour_2699463439353286802` |
| L2-② | `AllianceMemberOptionsPopup`（`Shell/AllianceMemberOptionsPopup.cs`） | `Member Options Panel` | **22** | `AllianceMemberOptionsPopup__{Awake,Open,Close,ShowConfirmation}.c` · `dump.cs` 字段偏移表（`TypeDefIndex 2543`） |
| L2-③ | `PurchasePremiumWindow`（`Shell/PurchasePremiumWindow.cs`） | `Purchase Premium Window` | **34** | `PurchasePremiumWindow__{TryOpen,OnEnable,Initialize,FetchData,ClearCurrentArmyContainers,FocusOnArmy,SelectArmy}.c` · `PuchasePremiumArmyInfo__Initialize.c` · MB `MonoBehaviour_6814103817760397369` |
| L4 | `RankedRewardEventWindow`（`Shell/RankedRewardEventWindow.cs`） | `Ranked Boost Reward Event Window` | **17** | `RankedRewardEventWindow__{Awake,Open,CloseButtonClick}.c` · `SimpleArmyImage__Initialize.c` · MB `MonoBehaviour_3493192490119290576` |

**每扇的树（兄弟序照 prefab 的 `m_Children`，逐条现读）**：

- **`Generic Options Panel`**：`[Menu Dark Background, bg shadow, bg, Name, Template, Buttons]`
  （`Template/Button Text` 是 `Template` 的子件）。
- **`Member Options Panel`**：`[Menu Dark Background, bg shadow, bg, Name, Buttons]`
  ＋ `Buttons[i]`（8 颗）+ 每颗的 `Button Text`。⚠️ **字段偏移顺序 ≠ 兄弟序**：
  `Challenge` 是**第一颗**子件、却挂在字段 `+0xA8` 上（⛔ 建树按 `m_Children`、映射按字段表，两件事分开读）。
- **`Purchase Premium Window`**：`[Menu Dark Background, Generic Window Red Background Big, Title, SubTitle,
  Premium image, Army Info, Scroll View, Generic Close Button Orange]`（8 个直系子件）。
  🔴 **`Scrollbar Collection` 是 `Scroll View` 的子件**（不是根的直系子件）—— 我原来建错了，
  **是对账脚本抓出来的**（见 §十·②）。
- **`Ranked Boost Reward Event Window`**：`[Menu Dark Background, window]`；
  `window = [Generic Window Red Background Big, Generic Close Button Orange, Title, Description, Timer,
  Bonus points, Scroll View]`。

### 二·补 —— **全文件唯一两处「我们算的，不是照抄」**（铁律 3）

1. **`PurchasePremiumWindow` 的价签 `Price Display/text` 框宽**：dump 量出来是 **0.00**（它带
   `ContentSizeFitter(h:PreferredSize)`，而文字宽要 Unity 的字体度量）。**能反推的那一半**（逐位可验）：
   `icon.x1 = PriceBox.x1 + (293.21 − 总首选宽)/2 = 947.36 + 116.6` ⇒ **总首选宽 = 60.01 = icon 的宽**
   ⇒ 证明 dump 那一刻文字宽**确实是 0**。**推不出来的那一半** = 它真正的宽 ⇒ 取
   **`[icon 右沿, PriceBox 右沿]` = 116.59** 并**标明是「我们摆的」**（旁证：同族 `BaseOfferPopup`
   同一格的 `Price Display/text` 是 124.50 宽，同一数量级）。⛔ **改版式时从这一格看起**。
2. **`RankedRewardEventWindow` 的倒计时格式串**：原版由 `TimerDisplay.Initialize(endTime)` 填，
   **`TimerDisplay` 本仓根本没有**、格式串判据也读不到 ⇒ 我按 prefab 出厂原文 `23h 34m` 的形状写了
   `FormatRemaining()`（`{h}h {m}m`）。**那一段是我们写的**，已在代码注释与 §九① 标明。

---

## 三、状态 → 参数 表（每扇：哪些元素在什么条件下显示/隐藏 · no-data 分支照的是哪个 `Set*` 方法体）

### ① `GenericOptionsPanel`

| 件 | prefab 出厂（现读） | **运行期**（`Start()` 之后） | no-data（我们的出厂态） | 判据 |
|---|---|---|---|---|
| 根高 | **126.60**（含 `Template`） | **61.60**（`Template` 关掉 ⇒ 不进布局） | 61.60 | 见下「🔴」那一段 |
| `Template` | `act = T` | **`act = F`** | 关着 | `GenericOptionsPanel__Start.c` 第一句 |
| `Buttons` | 0 子件（预制里就是空的） | 0 子件 | 0 子件 | `SetButtons(空表)` |
| `Menu Dark Background` | `act = T`、**`m_Color = (0,0,0,0)`（a = 0）** | 同 | 同（只吃点击） | MB 实读 |
| 按钮 | 无 | 由 `SetButtons` 逐颗 `Instantiate(buttonTemplate)` | 无 | `SetButtons.c` |

🔴 **运行期几何 = 不含 `Template`**（本件最重要的一条「我们算的」）：判据两条 ——
① uGUI `LayoutGroup.CalculateLayoutInputHorizontal`（本地源码
`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/LayoutGroup.cs:53-62`）
**只收 `activeInHierarchy` 的子件**；② 布局重建跑在 `Canvas.willRenderCanvases`（**`Start` 之后**）。
⇒ 根高 = `10 + 36.6 + 5 + max(0, 65n−5) + 10`（n = 实例化出来的按钮数；n=0 ⇒ **61.6**）。
⛔ **prefab 那个 126.6 只在 `Recon` 表里留档、不参与运行期**（断言**两档都钉**，见 §七）。

### ② `AllianceMemberOptionsPopup`

`Open()` 的显隐模型（**逐句读出来的**，设 `me` = 本机玩家的 `GroupMember`、`m` = 被选那一员）：

| 按钮 | 条件 | 字段（`dump.cs`） |
|---|---|---|
| `Challenge` | `!isSelf` | `+0xA8` |
| `Add as a friend` | `!isSelf && !FriendsData.IsFriend(m.id)` | `+0x78` |
| `Profile` | `!isSelf` | `+0x80` |
| `Promote` | `outrank && role < 3 (Leader)` | `+0x88` |
| `Demote` | `outrank && role > 0 (Member)` | `+0x90` |
| `Kick` | `outrank` | `+0x98` |
| `Quit` | `isSelf` | `+0xA0` |
| `Debug Add Skulls` | **永远关**（`Awake()` 无条件） | `+0xB0` |

其中 `isSelf = (myPlayfabId == m.Id)`、`sameGroup = (me.GroupId == m.GroupId)`、
`outrank = sameGroup && m.Role < me.Role`（`GroupRole`：`Member 0 / Moderator 1 / Admin 2 / Leader 3`）。
⚠️ **`m` 上那些偏移**：`+0x10 Id` · `+0x20 GroupId` · `+0x48 Role`（`GroupMember` 字段表实读）。

| 件 | prefab 出厂 | **no-data（我们的出厂态）** | 判据 |
|---|---|---|---|
| 八颗按钮 | 全 `act = T` | **全开，只有 `Debug Add Skulls` 关** | `Awake()` 那一句是**无条件**的（不依赖数据）；其余七颗的显隐**全要服务器** ⇒ 照 prefab 出厂态 |
| `Name` | `'Pepito el de siempre'` | **空串**（原版是 `Member.Name`，本地没有） | `Open()` 的 `playerName.SetText` |
| `Promote/Button Text` | `'Promote'` | `'Promote'`（prefab 原文） | 原版运行期按 role 换成 I2 词条 **`SocialMenu/Alliances/TransferLeadership`**（role==Admin）或 **`…/Promote`** —— 两个 term key 从二进制字面量表实读（`stringliteral.json` 的 `0x184253cb0` / `0x184253ab0`），**词条表在远端 CCD ⇒ 本地没有** |
| 整扇窗 | —— | **开不出来**（原版 `Open()` 在数据为空时直接抛） | `AllianceMemberOptionsPopup__Open.c` |

### ③ `PurchasePremiumWindow`

| 件 | prefab 出厂 | **no-data（我们的出厂态）** | 判据 |
|---|---|---|---|
| `SubTitle` | **`act = F`** | 关 | prefab 实读 |
| 价签 `Button Text` | **`act = F`** | 关 | prefab 实读 |
| `Scrollbar Collection` | **`act = F`** | 关（⇒ `Sliding Area`/`Handle` 整棵不画，与 dump 的 `ANC✗` 一致） | prefab 实读 |
| `Army Container`（模板） | `act = T` | **关**（`Initialize()` 尾段 `SetActive(false)`） | `Initialize.c` |
| `Content` 下的容器 | 1 个（模板实例） | **0 个** | `FetchData()` 读远端 LiveOps ⇒ 本地空表 |
| `Army Info` 五栏 | 见 §二 | **prefab 出厂原文**（`BUY ONCE, PROFIT ENDLESSLY` / 三条 bullet / `300,00` / `Purchased!` / `Ultramarines`） | 原版这几格分别是 I2 词条 + 逐军数据 |
| 价签 vs `Purchased!` | 两栏都开 | 两栏都开 | `PuchasePremiumArmyInfo__Initialize.c`：`!Purchased ⇒ 价签开 + Purchased 关`，反之亦然 |
| 开场动画 | —— | **alpha 0→1 + scale 0.8→1（0.3s，Ease.OutQuad）** | `OnEnable.c`；`0.3` 从二进制读（RVA `0x34B2DC8`），`0.8` 是立即数 `0x3f4ccccd` |

### ④ `RankedRewardEventWindow`

`Open()` 那五条**自带兜底**（= 原版自己的「没数据」分支）：

| 件 | 原版那一支 | **no-data（我们照它做）** | 判据 |
|---|---|---|---|
| `Title` | `GetLabel(data,0)`；**空 ⇒ `TMP.enabled = false`** | **关** | `RankedRewardEventWindow__Open.c` |
| `Description` | `GetLabel(data,1)`；空 ⇒ `enabled = false` | **关** | 同上 |
| `Bonus points text` | `string.Format(GetLabel(data,2), PointsBonus)` | **空串**（`string.Format("",x)` = `""`） | 同上 |
| `Timer` | **`data.DurationHours == 0` ⇒ 整件 `SetActive(false)`** | **关** | 同上 |
| `Content` 下的阵营卡 | `data.AffectedArmies` 逐条 `Instantiate(armyImagePrefab)` | **0 张** | 同上 |
| 压暗层命中区 | `backgroundCloseButton.onClick` 与关窗钮**同一条回调** | 两条都关窗 | `Awake.c` |

⚠️ **我们这一侧「关」用 `SetActive(false)`**，原版是 `TMP.enabled = false`（节点仍 active）——
对**没有子件的叶子文字**来说可观测结果相同（都不画）。本仓没有 `enabled` 那一层，如实记。

---

## 四、`PurchasePremiumWindow` 的参数族判据（照的是哪张表）

**照的是 `Shell/WindowsManager.cs` 那句注释指的「全库 7 个带参数的窗口」那张表**（`GameWindow.TryOpen`
那一段的注释，判据 = `GameWindow__TryOpen` 的两条重载反汇编）：
`DeckEditingWindow` / `DeckSelectionPopup` / **`PurchasePremiumWindow`** / `RankedEventWindow{,V2}` /
`SinglePlayerOnlyEnergyWindow` / `SkirmishEventWindow`。
⛔ **没有另立一套参数表**，同形照抄：

```csharp
// Shell/PurchasePremiumWindow.cs
public PurchasePremiumWindow OpenEx(int? army = null, ArmyOffer[] offers = null)   // = TryOpen(data, options) 的等价物
{ if (army.HasValue) FocusArmy = army.Value;
  if (offers != null) _offers = offers;
  Initialize();                      // = 原版「先调基类、再补自己的刷新」里那个「自己的刷新」
  return this; }
```

- `data` = 原版那个 **`CardArmy?`**、**为空取 `10` = `CardArmy.Ultramarines`**（枚举实读；
  `RankedRewardEventWindow__TryOpen.c` 里那个 `uVar7 = 10`）。
- `options`（`GameWindowOptions`）**本仓未建模** ⇒ 只保留 `army` 这一半，如实标。
- `WindowsManager.OpenPurchasePremiumWindow(army, offers)` 走 `OpenByRef` + 随后 `OpenEx(...)`
  （复用那一支也要喂新参数 —— `TryOpen` 在 `State == Open` 时**早退**，照原版 A217②）。

---

## 五、注册改动（`WindowsManager.cs:行`）

`git diff --numstat` ⇒ **+129 / −3**（⚠️ **其中 3 删 / 7 加不是我的**，见 §十·①）。

| 位置 | 内容 |
|---|---|
| `Shell/WindowsManager.cs:1119-1146`（同一 hunk 内，紧挨 L1 那条之后） | 4 条 `PrefabRef*`：`PrefabRefGenericOptionsPanel` / `PrefabRefAllianceMemberOptions` / `PrefabRefPurchasePremium` / `PrefabRefRankedRewardEvent` |
| `Shell/WindowsManager.cs:1240-1280`（新 hunk） | 4 个 `Open*`：`OpenGenericOptionsPanel(ctx, anchor)` / `OpenAllianceMemberOptions( view)` / `OpenPurchasePremiumWindow(army, offers)` / `OpenRankedRewardEvent(view)` |

🔴 **只注册了我这四扇** —— L1 那两条（`PrefabRefBaseOfferPopup` + `OpenBaseOfferPopup`）**一行没动**。
⚠️ 四个 `Open*` 都走 `OpenByRef`（= 原版 `automaticallyLoadedWindows` 命中复用）；
新建那一支会**多建一遍**（`Create` 的 `Open()` 建一次、`Show` 再建一次）—— 四扇树都很小（8/22/34/17），
且它们**没有** `BaseOfferPopup` 那种「内容没变 ⇒ 不重建」的守卫（参数是调用方每次现给的，判「没变」要逐格比数组）
⇒ **如实记这一处多余的重建**，⛔ 没假装它不存在（注释写在方法组头部）。

---

## 六、可复现对账（命令 + 对账项数 / 不符数）

```bash
# ① 四张 Recon 表 ↔ 现读，逐格对账（纯 python，不跑 Unity）
PYTHONIOENCODING=utf-8 python d:/4/_tmp_view/wl2/check_table.py
#   ⇒
#   === Generic Options Panel                 表里  8 条 · 现读  8 条 · 对账项  8 · **不符 0**
#   === Member Options Panel                  表里 22 条 · 现读 22 条 · 对账项 22 · **不符 0**
#   === Purchase Premium Window               表里 33 条 · 现读 33 条 · 对账项 33 · **不符 0**
#   === Ranked Boost Reward Event Window      表里 17 条 · 现读 17 条 · 对账项 17 · **不符 0**
#   合计：对账项 80 · **不符 0**

# ② 任意一扇的整棵树 / 窗口字段
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Member Options Panel" --depth 12 --relative --md
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Purchase Premium Window"  --depth 12 --relative --md
```

**两条如实标注**（对账表里逐条写了，这里再点一次）：

1. `Purchase Premium Window` 的 `…/Price Display/text`：**表里照抄 dump 的 0 宽**（真读数），
   而我们**建出来**的那一格是 **116.59** 宽（§二·补①）⇒ **这一行的框与建出来的不一致、是故意的**。
2. `…/Scrollbar Collection/Sliding Area/Handle` **不在表里** —— 原版那一格的 `m_AnchoredPosition.y`
   是 **NaN**（dump 印 `nan`），**没有可对账的值**（⛔ 没有编号码顶替）⇒ 表里 **33 条**、真实节点 **34 个**。

---

## 七、断言清单（断什么 · 期望值来源 · 改坏法 · 两态 · 落点）

### 七·0 **宿主怎么定的**（现读，不是猜）

`Editor/ShopScene.cs` —— 与 L1 **同一个宿主**（同族既成宿主：`BoosterInfoPopup` 26 处 / `OfferContainer` 63 处）。
🔴 **我加在 L1 那一节之后**（`// ---------------- 收尾 ----------------` 之前），
**L1 的 66 处断言一行没动**（`git diff` 只有 2 个 hunk：一个是新帮手 `CheckHasKids`（`ShopScene.cs:480`），
一个是从 `:3647` 起的那 498 行 —— 里面同时含 L1 的和我的，git 把两段相邻新增并成了一个 hunk）。

### 七·1 断言分组（调用点约 90 处）

| 组 | 断什么 | 期望值来源（**都不是自证**） | 改坏法 |
|---|---|---|---|
| **窗口参数**（每扇 4~6 条） | `type` / `placement` / `closeOnEsc` / `extraScaleSmallScreen` | **各扇 MB 原文** | `closeOnEsc` 写成 1 ⇒ GOP 那条红（**同族另两扇是 1，逐扇实读**） |
| **注册键 + 根名**（每扇 2 条） | 键 = prefab 名、根名 = prefab 名 | 现读的名字 | 改 `Create` 里那句 `new GameObject(...)` ⇒ 红 |
| **层级**（`KidNames` / `CheckHasKids` / `ButtonNames`） | 根/各级的**直接子件**名与**兄弟序** | prefab `m_Children` 现读 | `Scrollbar Collection` 挂回根 ⇒ PPW 那两条红（**真抓到过一次**） |
| **出厂显隐**（每扇 2~4 条 + 反例） | `Template` / `Debug Add Skulls` / `SubTitle` / 价签 `Button Text` / `Scrollbar Collection` 关着 | prefab `act` + `Start()`/`Awake()` 明文 | 把「关着」写成「不建」⇒ 红；**每条都配一条「反例开着」** ⇒ 「整棵树没建」蒙不过去 |
| **几何**（`CheckAt`，每扇 3~5 条） | 绝对框（冻结 px 字面量） | `menu_dump` 现读的 px | 改任一常量 ⇒ 红（**断言不读** `RootH` / `Recon` / `Abs` / `Geo` 那些表） |
| **no-data 行为** | GOP 0 按钮 / PPW 0 容器+模板关 / RRew 三栏关+空串+0 卡 | 反编译 `Set*` / `Initialize` 明文 | 改成「建出来开着」⇒ 红 |
| **两态**（每扇各一组） | 喂数据前后**各断一次**（AMOP `Quit` ↔ `Challenge` 互斥 · PPW 容器 0→2 · RRew 三栏关→开、卡 0→2） | 反编译那棵树的条件 | 只断一态 ⇒ 弱 |
| **复用/关过再开** | 同键再开 ⇒ 同一实例；关过再开 ⇒ 新建 | 原版 `automaticallyLoadedWindows` + `CloseWindowCO` | 删复用缓存 ⇒ 红 |
| **缺图出声** | `MissingArt` 张数 + 名字 | 工程里那张图**确实没有**（现查） | 悄悄拿别的图顶上 ⇒ 红 |

### 七·2 判别力最强的三条（**灭自证**）

1. **`GenericOptionsPanel.closeOnEsc == false`** —— 同族另两扇是 1 ⇒ 拿 1 去断**必红**。
2. **GOP 的 `Buttons` 落在 `688.30` 而不是 `753.30`** —— 这一条就是「`Template` 关掉之后布局会不会重算」
   那个**两档歧义**的判别式（见 §三① 🔴）；两档的数都在注释里。
3. **PPW 的 `Scrollbar Collection` 必须是 `Scroll View` 的子件** —— 对账脚本抓出的真错（§十·②）。

### 七·3 **派活必查行三条**（逐条自查结果）

| 检查 | 结果 |
|---|---|
| ① **断言自证 / 同义反复** | ✅ 期望值全是**冻结字面量**（`menu_dump` 现读的 px / MB 原文 / 反编译分支），⛔ 一处都不读 `RootH`/`Abs`/`Recon`/`Cards` 表 |
| ② **弱断言分不出两种状态** | ✅ 凡有另一态的地方**两态都断**（见上表「两态」那一行） |
| ③ **`!RectOfUnion` / 「一个 quad 都没有」式断言** | ✅ **一条都没有** —— 「关着」一律走 `activeSelf` 且**逐条配了反例**（同一棵树上必有一个开着的件） |
| 分层用**渲染队列**不是 z | ✅ 四扇各成一档、**不与其他窗重叠**（GOP 3330–3337 · AMOP 3340–3347 · RRew 3460–3478 · PPW 3521–3547）；压暗层命中区走公共件 `MenuDraw.ShadeHit`（它自带「`qShade < qContentMin`」现场核） |
| 「新加一层就配一条该藏的时候藏住了吗」 | ✅ 四扇的「出厂关着」件**逐条断**（GOP `Template` · AMOP `Debug Add Skulls` · PPW `SubTitle`/价签文本/`Scrollbar Collection`/模板 · RRew `Title`/`Description`/`Timer`） |
| 断言**落在正确的 `*Scene.Run`** | ✅ `ShopScene.Run`（§七·0） |

⚠️ **本节本次【没有跑】**（铁律 12：A 表清零前中途不跑 Unity 自检）—— **如实记**，⛔ 不当「已验过」。
**建议怎么跑**（同步点）：`ShopScene.Run` 一条即可（**只动了一个宿主**）：耗时 ≈ **45 秒**。
⚠️ 判绿红看「`=== 合计：N 通过 / M 失败 ===`」那一行（本宿主不用 `✗` 标记失败）。

---

## 八、缺的素材（逐张：原版 sprite 名 · 源在哪 · 我们缺不缺）

| # | 原版 sprite 名 | 用在哪 | 源（解包） | 我们 | 现在的处置 |
|---|---|---|---|---|---|
| 1 | **`OctagonUI Filled SDF`**（128×128 · 九宫 52,52,52,52 · Sliced） | GOP / AMOP 的 **`bg shadow`** | `bundle_duplicateassetisolation_assets_all/Sprite/OctagonUI Filled SDF.json` | ⛔ **工程里一张都没有**（`Resources/Art/{ui_menu,ui_deck,ui}/` 三处都没有；只有 `…_Border_SDF` / `…_Filled_Fade_SDF`） | 节点照建、**这一格不画** + `LogWarning`；断言钉 `MissingArt == 1` |
| 2 | **`UI_HIghlight Internal`**（69×63 · 九宫 31,28,31,28 · Sliced） | PPW 的 **`Army Container/Hightlight`** | `bundle_atlasindividual_assets_0_mainmenu/Sprite/UI_HIghlight Internal.json` | ⚠️ **只在 `Assets/CardPresentation/Art/原版/0_mainmenu/UI_HIghlight_Internal.png` 躺着，没进 `Resources/`** | 同上（`MissingArt == 1`） |
| 3 | **`40k_UI_Banner BW`**（624×190 · Simple） | RRew 的 **`Bonus points`** 红横幅 | `bundle_atlasindividual_assets_0_mainmenu/Sprite/40k_UI_Banner BW.json` | ⚠️ 同上，只在 `Art/原版/0_mainmenu/40k_UI_Banner_BW.png` | 同上（`MissingArt == 1`） |
| 4 | **`40K_shop_offer_bg_Sororitas_0`**（450×512 · Simple · **大写 `K`**） | RRew 的阵营卡 `Background` | `bundle_boosterpacks_assets_all/Sprite/` | ⛔ **工程里没有**（`Resources/` 下 `*shop_offer_bg*` 零命中） | 只在**有数据**那一支才取（出厂 0 张卡 ⇒ 不请求、不进 `MissingArt`） |
| 5 | `UI_Army_Selection_Featured`（172×172） | RRew 阵营卡的 `Feature Badge` | —— | ✅ **有**（`Resources/Art/ui_menu/`） | 正常画 |
| 6 | `40k_general_bt_yellow_hover` / `_pressed` | 四扇关窗钮的换图 | —— | ✅ 有 | 正常 |

> ⇒ **还欠主对话一次 `工具/import_original_art.py` 的腿**（把 #1 #2 #3 三张导进
> `Resources/Art/ui_menu/`；#4 要不要导由你裁 —— 它只在「有数据」那一支出现）。
> ⛔ 我**没有**动 `工具/*`、也**没有**往 `Resources/` 里塞文件（都在白名单外）。

---

## 九、没查清 / 没做的（⛔ 不许猜）

1. **`PurchasePremiumWindow` 的 `DOScale` 目标值**：那个 `Vector3` 来自一个静态字段链
   （`DAT_1842da2a8` → `+0xb8` → `+0xc`），**我没有把它读成具体值** ⇒ 按「根 `m_LocalScale = (1,1,1)`」
   取 **`Vector3.one`**。**如实标**：这一步是**推的**（起点 `0.8` 与时长 `0.3` 都是从二进制读的硬值）。
2. **`TimerDisplay` 的格式串**：本仓没有那个类，原版那份 `Initialize(endTime)` 的格式（含
   `useLastMinutesText` 那一支的词条）**判据不齐** ⇒ 我按 prefab 出厂原文的形状写了 `h`/`m`（§二·补②）。
3. **`EverguildButton.interactable` 那一层**：我们**没有 uGUI 的 `interactable`** ⇒ `OptButton.Interactable=false`
   时**把钮建成灰的**（原版 `m_DisabledColor = (0.784,0.784,0.784,0.502)`）并出声。这是**我们的近似**，如实标。
4. **`MenuDraw.Text`/`TextBox` 没有 `clip` 形参** ⇒ 视口里的**文字**不吃裁切（PPW 的 `Army Name` /
   `Premium Text`、RRew 的 `Army Text`）。**全壳同形**，⛔ 没为这两扇另立一套。**要做**的话是一处公共件扩参。
5. **`MenuScroll` 用的是自己的滚轮系数**（`NotchK = 0.4`），而原版这两扇的 `m_ScrollSensitivity` 是
   **30（PPW）/ 100（RRew）** ⇒ **手感不是同一个数**。如实记，⛔ 不为这两扇再立第二份滚动实现。
6. **四扇都没有本地入口**（全量反编译里各自只命中自己的 `.c`；`GenericOptionsPanel` 那扇的
   **跨 CAB UnityEvent 开启点**也没排除）⇒ 我们只提供「怎么开」，**⛔ 不编入口**。
7. **`AllianceMemberOptionsPopup` 那三条确认弹窗的字符串**（`challengeConfirmationPopupString` /
   `confirmationPopupButtonString` / `cancelPopupButtonString`）—— 它们**不是序列化字段**（运行时填），
   来源是 I2 词条 ⇒ **没查清**（不属本件的判据链；本件只建那 22 个节点）。
8. **`RankedRewardEventWindow` 的 `SimpleArmyImage` 卡高**：我按 **`ctrlH = 1` + `childForceExpandHeight = 1`**
   推出「卡高 = 视口高 293.22（**不是 prefab 那个 300**）」⇒ 子件框用锚点式算式算（`CardChild`），
   ⛔ **没有**用死矩形。**没有实跑核过**（那一支只在有数据时才建）。
9. **`Member Options Panel` 的 `Handle`** 那一格 `m_AnchoredPosition.y` 是 **NaN** ⇒ 框无可对账的值（§六②）。

---

## 十、顺手发现（⛔ 别自己顺手改）

1. 🔴 **`Shell/WindowsManager.cs` 上有一处不是我的改动**：`git diff` 在 **`:313` 那一带**有一处
   **−3 / +7**（内容是关于 `align` / `m_HorizontalAlignment` 的注释）。⚠️ 我的简报说这个文件是
   **我这个写手的独占口**（L1 的 +39 已记账）⇒ **另有一个写手动过它**（或者 L1 后来又动了一次）。
   我**没有碰那一段**，只在自己的两个 hunk 里加东西。**请调度台核一下那个文件的最终归属**。
2. 🔴 **`Scrollbar Collection` 的父**：我第一版按「根的直系子件」建，**对账脚本抓出来**它其实是
   `Scroll View` 的子件（`menu_dump --relative` 的缩进 `··2`）。已改。**这条值得记进坑表**：
   34 个节点里，靠肉眼数缩进会漏 —— **A251 那类「逐节点照原版」的活，脚本对账是必需的不是可选的**。
3. ⚠️ **`Member Options Panel` 八颗按钮的 `Image.m_Color` 是原版设计者按语义打的色**（逐颗实读）：
   `Promote` = `(0.00739, 1, 0, 1)`（绿）· `Demote`/`Kick`/`Quit` = `(0.8, 0.0275, 0.0275, 1)`（红）·
   `Debug Add Skulls` = `(0.923, 0, 1, 1)`（品红）。⚠️ 我核过 `EverguildButtonMaterialModifier` 是
   `IMaterialModifier`、**不写 `graphic.color`** ⇒ **这些 tint 真的会画出来**，我**逐颗照抄**了。
   （看着像「开发用的色标」，但**资产里就是这么存的** —— 照字段抄。）
4. ⚠️ **两个小面板的 `Menu Dark Background` 的 `m_Color` 是 `(0,0,0,0)`（a = 0）**，同族别的窗是
   `(0,0,0,0.773)`。它**只吃点击**（uGUI 的 `Graphic.Raycast` 不看 alpha）。⛔ 别顺手改成 0.773。
   ⚠️ 两扇的 `BackgroundOverDrawController` 一个禁用、一个启用（`Member Options Panel` 那颗是开的）——
   那是**全局栈**（同一时刻只画最上面那一个整屏底），**它不写 `m_Color`** ⇒ 透明仍然是透明；我们没实现那条栈。
5. ⚠️ **`Generic Options Panel` 的 `closeOnESC = 0`**（同族另两扇是 1）—— 逐扇实读，别互推。
6. ⚠️ **`AB` 号提示**：`Shell/RankedEventWindow.cs`（= `RankedEventWindowV2`）与我这扇
   `RankedRewardEventWindow`（= `Ranked Boost Reward Event Window`）**是两扇窗**，
   名字只差一个词、类名也只差一个词 ⇒ **将来 grep 时容易串**（我在两个文件头都写了交叉提醒，
   但**没动** `RankedEventWindow.cs` —— 它在白名单外）。

---

## 十一、类型检查结果

```text
$ TMPDIR=/tmp/wf_wl2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

**跑了 8 次**，其中**两次拿到的是别人的半成品**（如实记，按铁律 13·3 那条办：**没去改别人的文件**）：

| 第几次 | 结果 |
|---|---|
| 1 | 运行时 **2** 个（**我的** `GenericOptionsPanel.cs`：`MenuDraw.Nine` 返回 `GameObject` 不是 `ImageQuad`）⇒ 当场改掉 |
| 2 | 0 / 0 |
| 3 | 运行时 **7** 个，**全部在 `Battle/AttackSelector.cs` + `Battle/BattleDriver.cs`**（**不是我的文件**，另一写手正在写）⇒ 按纪律**不碰**、继续 |
| 4 | 0 / 0（那 7 个已由对方改完） |
| 5 | 编辑器 **2** 个，**在 `Editor/BattleScene.cs`**（`BattleDriver.MySideForTest` 未定义 —— 不是我的文件）⇒ 不碰 |
| 6 | 编辑器 **2** 个，**在 `Editor/ShopScene.cs`（我的 hunk）**：`"…Format("", x)…"` 里那个**未转义的 `"`** ⇒ 当场改成不含引号的写法 |
| 7 | 编辑器 **1** 个（我的）：`AllianceMemberOptionsPopup` 缺 `ButtonNames()` ⇒ 当场补 |
| 8 | **0 / 0** ✅ |

⚠️ **四扇窗的断言本次【一条都没跑】**（铁律 12）—— 见 §七·3 末段的「建议怎么跑」。
