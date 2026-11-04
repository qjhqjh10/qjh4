# B141-W28 · §4 的 4-17「队列带重叠：档案页 Avatar 与开包窗同号 3170」

> 执行写手 `B141-W28` · 2026-11-04 · 批次 `B141` · 会话 第一百二十八。
> 施工判据 = `判据/B141_W129_对账_UI动画.md` 的 **W15 包**（条目 4-17 = 队列表三 #15）；触发审计 = `报告/B138_审计汇总_问题清单.md` §4 的 4-17。
> 口径：**以现读代码 + 现读原版场景 YAML 为准**；报告里的行号是 **2026-11-04 改后现读**。
> ⛔ 未跑 Unity、未动 git、未改 `d:/2`、未改账主表／活状态表／所有权表。

---

## 一、结论（一句话）

**开包窗整带 3170–3197 → 3122–3149**（= `PlayerProfileWindow.QShade − 28`），使它**整条严格低于档案窗（3150–3299）**；
并在本窗显式声明 `BlocksLeftNav = true`（把改档前「压暗层档更高 ⇒ 吃掉左栏点击」那半边行为**逐字保住**）。
⇒ 档案页 Avatar（`Q` = 3170 · 格子带 3180–3299）与开包窗**不再同号、也不再区间相交**，先后按原版 = **档案窗在上**。

---

## 二、原版判据（⚠️ 就地订正审计那句话）

审计（`B131` 表三 #15 / `B138` 4-17）写的是「**原版两者都在同一 PopUp 画布按兄弟序**」。
**2026-11-04 现读 Export 工程后，这句不成立** —— 两扇**不同画布**，而且排序层那一道机制单独就能定先后：

| 事实 | 判据（可复查坐标） |
|---|---|
| 开包窗 `windowsPlacement = 5 (Canvas)` | `assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_9012570135841684515.json`（本仓文件头已录）；锚点 = 场景里 **`2 - Canvas Holder Above upper bar`**（GameObject `31`，MonoBehaviour `1967`：`placement: 5`）—— `Data/Scenes/MainMenu Warpforge.unity` |
| 档案窗 `windowsPlacement = 15 (Popup)` | `MonoBehaviour_-4653119414410905038.json`；锚点 = **`3 - PopUp Holder`**（GameObject `45`，MonoBehaviour `1980`：`placement: 15`） |
| 该 holder **自带 Canvas**，且是 `overrideSorting` + `PopUps` 层 | `Canvas_1075`：`m_OverrideSorting: 1` · `m_SortingLayerID: 1054366423` · `m_SortingOrder: 0`（同上场景 :38607-38629） |
| `2 - Canvas Holder Above upper bar` **自己没有 Canvas** | 它的组件只有 RectTransform `1109` + MonoBehaviour `1967` ⇒ 合成进根画布 |
| 根画布 = `Default` | `Canvas_1077`（`Game UI`）：`m_OverrideSorting: 0` · `m_SortingLayerID: 0` · `order 0` |
| `PopUps` 索引 **7** > `Default` 索引 **0** | `ProjectSettings/TagManager.asset` 的 `m_SortingLayers`（Default 第 1 条 … PopUps 第 8 条）⇒ **排序层索引压过 order** |
| 兄弟序**同向** | `Safe area Only Horizontal`（RectTransform `1540`）的 `m_Children`：`Navigation Panel`(1103) 第 1 · `Upper bar`(1092) 第 4 · **`2 - Canvas Holder Above upper bar`(1109) 第 6** · **`3 - PopUp Holder`(1121) 第 7** |

⇒ **原版：档案窗（PopUp 层）整棵永远画在开包窗（Canvas 层）之上**；两扇在运行时还**不可能同屏**
（开包窗 `type = 0 Fullscreen` ⇒ `OpenWindow` 走「全藏其余窗」那一支；档案窗 `type = 1` ⇒ 只把上一扇 `ToBackground()`）
⇒ 这条先后是**潜在不变量**，不是观感差异 —— 但我们的梯子是固定号，必须把它写对。

---

## 三、改动（两个文件 + 一个新断言文件）

### 3.1 `Shell/BoosterPackOpenWindow.cs`

| 项 | 改前 | 改后 |
|---|---|---|
| `QBase` | `3170`（字面量） | **`PlayerProfileWindow.QShade - BandSize`** = **3122**（写成表达式：档案带一动，本带自动跟着走） |
| `BandSize` | 无 | **新增** `1 + 5 * CardStride + 2` = **28** |
| `QShade` / `QCard` | 3170 / 3171 | **3122 / 3123** |
| `QHint` / `QHintHit` | 3196 / 3197（`const`） | **3148 / 3149**，**改 `public const`**（新断言要读带顶；按「有外部引用才 public」的既有口径） |
| `BlocksLeftNav` | 无覆写（基类判据 `0/5` ⇒ **false**） | **新增覆写 `=> true`**（原因见下） |

**行数变化**：改前 **1814** 行 → 改后 **1830** 行（+16），**新增的 16 行全部落在文件末尾（`:1813-1828`）**，
而账里对本文件的 60 条 `文件:行号` 落点最深只到 **`:1698`** ⇒ **`check_ledger_points` 不会漂**。
队列常量那一段（原 `:128-153`）**重写成 26 行 = 原行数**（就地改写，零行数变化）。

**为什么必须同时改 `BlocksLeftNav`**（这一步不是顺手加的功能，是**保住原行为**）：
- 改档前：`QShade = 3170` **>** `MainMenuRuntime.QNavHit`(3156) ⇒ 左栏那一片的点击本来就落在本窗「点哪儿都关」的整屏块上
  （`Shell/GameWindow.cs:77` 那段注释本来就写着这件事）。
- 只把整带降下去（3122）之后：`QNavHit`(3156) 会反过来盖住它 ⇒ **左栏会变成能点**，点的是一个被整屏压暗层挡住、看不见的按钮。
- 修法 = 显式声明模态（基类那条 `type==Popup || placement==Popup` 在本窗 `0/5` 上两条都不成立）。
  **先例 = `Shell/SearchingOpponentWindow.cs:43`**：同一形状（`QSr = 3120 < QNavHit`，整屏背板盖住左栏）也是覆写成 `true`。
- 判据 = 原版兄弟序：`Navigation Panel`(1103) 是 `Safe area Only Horizontal` 的**第 1 个**兄弟、
  `2 - Canvas Holder Above upper bar`(1109) 是**第 6 个** ⇒ 本窗压暗层画在左栏**之上** ⇒ 左栏点击落到压暗层上（= 关窗）⇒ 与原版一致。

### 3.2 `Shell/AvatarTab.cs`

**零行数变化**（`:922` 那一行就地改写）：把「本页 `Q` = 3170」这条注释扩成**双向不变量**，
并打上 `✅ 已复刻（2026-11-04 · B141 · …）`。数值一个没动 —— 本页 `Q`(3170) 与格子带(3180–3299)本来就对，
错的是开包窗那一侧压了进来。

### 3.3 新建 `Editor/B141_W28_Assert.cs`

---

## 四、改后队列（现读）

| 带 | 号段 | 出处 |
|---|---|---|
| 顶栏 | 2986–2998 | `MainMenuRuntime.QBarPanel…QBarOverlay` |
| 模态时的左栏命中 | 3001 | `MainMenuRuntime.QNavHitBlocked` |
| 非模态时的左栏命中 | 3156 | `MainMenuRuntime.QNavHit` |
| **开包窗（本批改后）** | **3122–3149** | `QShade` 3122 · `QCard` 3123（5 张 × 5 档 = 3123–3147）· `QHint` 3148 · `QHintHit` 3149 |
| 档案窗窗框 | 3150–3155 | `PlayerProfileWindow.QShade…QHit` |
| 档案窗页带 | 3160–3210 | `QPageBase + PageIndex*10`（0..5） |
| **Avatar 页 `Q`** | **3170–3179** | `AvatarTab.PageIndex = 1` |
| **Avatar 格子带** | **3180–3299** | `AvatarTab.QCellOrderBase`（8 行 × 5 列 × 3 层 = 120） |

⇒ 后四行**全部 > 3149**：档案窗整棵（含 Avatar 页与格子）在开包窗之上，与原版一致。

⚠️ **如实记（本批接受的代价）**：3122–3149 这一段与 `DeckInfoPopup`(3120+)/`DeckSelectionPopup`(3122–3127)/
`BaseEventPopup`(3124–3129)/`RewardWindow`(3130–3143)/`SettingsWindow`(3130–3150)/`PromptPopup`(3140–3144)/
`SearchingMatchPopup`(3130–3135)/`ImportDeckPopup`(3130–3133) **共用号段**。
**它们与本窗不可能同屏**（本窗 `type = 0 Fullscreen` ⇒ `OpenWindow` 走「全藏其余窗」那一支），
与工程既有「互斥的窗共用号段」同一条前提（先例 = `AvatarTab.QCellOrderBase` 把 3180–3299 借给 8 行档，
同时与 `TitleTab`/`AchievementsMenu`/`AllianceMemberTab`/`SocialWindow` 的号段重叠）。
⚠️ 与 `TopBar.QBarOverlayBase`(3120–3132) 也重叠 —— 那一档只在**卡片详情窗开着**时生效（`CardDetailPopup` 调用 `ToggleOverlay`），
与本窗不会同屏；如实记在这里。

---

## 五、断言（`Editor/B141_W28_Assert.cs`，共 **16** 条）

| 组 | 条数 | 断什么 |
|---|---|---|
| ① 分带 | 3 | 两扇不再同号 · 整带与 `QAvatarPage` 不相交 · 整带 < 3180（不碰格子带） |
| ② 原版先后 | 2 | 整带**严格低于** `PlayerProfileWindow.QShade`(3150) · Avatar 页 `Q` 在整带之上 |
| ④ 带内不变量 | 4 | `QShade<QCard` · `QHint<QHintHit` · 带长 28 · 5 张卡 × 5 档全落在 `QCard…QHint−1` |
| ⑤ 顶栏 + 左栏 | 4 | 带 > `QBarOverlay`(2998) · 带 < `QNavHit`(3156)（⇒ 必须模态） · 带 > `QNavHitBlocked`(3001) · 探针 `BlocksLeftNav == true` |
| ③ **灭自证** | 3 | 喂**旧值 3170** ⇒ ②-A 必须判假 · 喂「抬到档案带之上（3400）」⇒ ②-A 也必须判假 · 旧整带下 ②-B 必须判假 |

**为什么这组断言不是自证**：期望值全部来自**跨文件的原版结构常量**（`PlayerProfileWindow.QShade` / `QPageBase` /
`MainMenuRuntime.QNavHit` / `QBarOverlay`），**没有**一条把 `3122` 这个实现数字抄进期望；
③ 那三条把「改回同号」「抬到档案带之上」两种坏法各喂一遍，**任一种都会红**。

**⚠️ 接线状态**：宿主（`Editor/ShellScene.cs` / `Editor/ShopScene.cs`）**不在本批白名单** ⇒ 本文件**未接进任何宿主**。
宿主里加两行即可（`B110_S1_Assert` 同一形状）：
```csharp
B141_W28_Assert.Run();
pass += B141_W28_Assert.Pass; fail += B141_W28_Assert.Fail;
```
⛔ **这不等于「断言已跑过」** —— 本批**未跑 Unity**，接入后必须由调度台在同步点实跑一次。

---

## 六、验证

- `cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w28 bash 工具/typecheck.sh`
  ⇒ **运行时程序集 0 错**；**编辑器程序集**当轮报的 2 条错**全部落在 `Editor/B141_W27_Assert.cs`**
  （别的在飞写手正在写它：mtime 与本次轮次同分钟，`CS8803`/`CS1022` 是**半截文件**的形状）
  ⇒ 按 `CLAUDE.md` 铁律 13·3「错误全部集中在不是你负责的文件上 ⇒ 那不是你的问题」。
  **本文件（`B141_W28_Assert.cs`）在两次编译里都 0 错**。
- 未跑 Unity（红线）、未动 git。

---

## 七、❓／遗留

1. **断言未接线**（见 §五）。这是本批**唯一**没有落地的一环，宿主是白名单外。
2. **审计那句話要就地改**（铁律 5）：`B131` 表三 #15 与 `B138` 4-17 的「原版两者都在同一 PopUp 画布按兄弟序」**不成立**
   —— 两扇在不同 holder、不同排序层（见 §二）。裁决权在调度台（本代理只读判据文件、不改账/报告正文）。
3. **更高一层的同族问题仍在**（`B131` 表三 #4：跨窗先后「后开在上」整体没做）—— 本批只治 4-17 这一对，
   没去动 `WindowsManager`（白名单外）。批内已把「本窗与 3120–3149 那族窗共用号段」如实记在 §四。
4. `QHint` / `QHintHit` 由 `const` 改 `public const`（断言要读带顶）。若调度台不接受扩大可见性，
   可改回 `const` 并把断言退化成「整带长度用 `QCard` 与带顶表达式推」—— 但那样断言会与实现同源，**不推荐**。
5. **白名单外还留着 4 处把旧号写死的文字**（判据都是**符号**读常量，只有字面是旧的 ⇒ 不影响行为，只误导读者；
   按铁律 5 该就地改，但本批不许碰）：
   `Editor/B98A5_Assert.cs:295`（注释「背景 = `QShade` 3170」）·
   `Editor/B98A8_Assert.cs:124`（注释）· **`Editor/B98A8_Assert.cs:136`（断言文案里印 `QShade(3170)`，跑起来会印旧值）** ·
   `Editor/ShopScene.cs:3631/3633`（注释）。⇒ 请调度台派给 `Editor/` 的写手一并订正。
6. 复查入口（本批只读）：`python -I -X utf8` 读 `Shell/BoosterPackOpenWindow.cs` 的 `QBase` 那 26 行注释即可看到完整判据链。
