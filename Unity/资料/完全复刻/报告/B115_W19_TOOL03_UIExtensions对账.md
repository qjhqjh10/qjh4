# TOOL-03 · `UI.Extensions`（155 类）对账 —— 只在「卡包 / 商店 / 列表 / 滚动」这条链上筛

> 执行写手 `B115_W19` · 2026-11-01
> 交付：`资料/完全复刻/资源/B115_TOOL03_UIExtensions对账.tsv`（裸 tab · **LF** · UTF-8 · 11 行 = 表头 + 10）
> 纪律：⛔ 没改任何 `.cs` · 没动账主表 / `活状态表.md` / 正本 · 没跑 Unity · 没动 git。

---

## 〇 一句话结论

**155 类里原版真正挂上去的只有 5 类**（`NonDrawingGraphic` 42 · `SoftMaskScript` 28 · `UIFlippable` 24 · `Gradient2` 18 · `UI_InfiniteScroll` 2 —— 1,389 prefab + 17 场景**全扫**的结果）。
我们这 5 类里 **3 类等价已实现 · 2 类部分（`UI_InfiniteScroll` / `UIFlippable`）· 真缺口 0**；另有 2 类是**调试专用**、2 类是**名字撞车**（`EasingCore.Ease` 撞 DOTween 的 `Ease`、`Gradient` 撞 `UnityEngine.Gradient`）。
🔴 最该知道的一条：**账里这 5 类的对账行 = 0**，而 `FancyScroll*`（原版游戏代码 **0 引用**）却有 **30 行**。

---

## 一 筛选口径（怎么从 155 类筛到 10 行）

**⛔ 不是按类名筛，是按「原版真的把它挂上了没有」筛。**

| 步 | 做法 | 结果 |
|---|---|---|
| ① **prefab 全扫** | 从 `D:/2/tools/assetripper_out/ExportedProject/Assets/Plugins/Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/**/*.cs.meta` 取 **146** 个 guid → 扫 **1,389 个 `.prefab`** 的 `m_Script.guid` | 命中 **6** 类：`NonDrawingGraphic` 42 · `SoftMaskScript` 28 · `UIFlippable` 24 · `Gradient2` 18 · `UI_InfiniteScroll` 2 · `AutoCompleteComboBox` 1 |
| ② **场景全扫** | 同一份 guid 扫 **17 个 `.unity`** | 再命中 `DropDownList`（13 个战场场景，GO 名就叫 `DropDownList`，第三方 demo 残留） |
| ③ **代码引用** | `Assembly-CSharp` 里只有 **7 个文件** `using UnityEngine.UI.Extensions;`（`CardImageController` · `CardInBoosterPack` · `CarouselOfferPopupDrawer` · `DebugCardPickerGrid` · `DebugRewardWindow` · `MissionHeaderDisplay` · `RankedArmyBonusContainer`）⇒ **逐标识符**与该命名空间下的类名比 | 真用到：`SoftMaskScript` · `NonDrawingGraphic` · `UI_InfiniteScroll`(×2) · `Gradient2`；**假命中 2 个**：`Ease`（其实是 `DG.Tweening.Ease`）· `Gradient`（其实是 `UnityEngine.Gradient`） |
| ④ **按链收敛** | 只留「卡包 / 商店 / 列表 / 滚动」会碰到的 | 10 行（见 TSV）；其余 **146 类 0 挂载、0 引用** ⇒ 汇总成 1 行 |

> ⚠️ **判据靠行为不靠类名**（简报第 2 条）—— 本轮就抓到两个反例：`Ease` 与 `Gradient`。
> 若不查 `using DG.Tweening;`，会把「DOTween 的缓动」误判成「EasingCore 没复刻」⇒ 造出两条不存在的账。

---

## 二 四档计数

| 判定 | 条数 | 类 |
|---|---:|---|
| **等价已实现** | **3** | `SoftMaskScript` · `NonDrawingGraphic` · `Gradient2` |
| **部分** | **2** | `UI_InfiniteScroll`（离散近似 + 三格没实现）· `UIFlippable`（水平镜像已有先例／垂直翻转无先例／节点级未对） |
| **真缺口** | **0** | —— |
| **不适用** | **5** | `AutoCompleteComboBox`（调试）· `DropDownList`（调试/demo 残留）· `EasingCore.Ease`（名字撞车）· `Gradient`（名字撞车）· 其余 146 类 |
| **合计** | **10 行** | —— |

---

## 三 `MainMenuOfferContainer` 那个**第二个实例**差什么（简报第 3 条）

「第二个实例」= `Shell/MainMenuOfferContainer.cs:111-176` 的 `MainMenuCarouselDrawer`（另一个是 `ShopCarouselView`）。
它今天**只到「一屏 N 槽」**：`Visible`（`:116`，默认 1）由宿主填，`State.Initialize(…, Visible)` → `OfferContainer.cs:1072-1076` 的 `ManualInit` 就只做 `Visible = visible`。

**原版 `UI_InfiniteScroll` 在同一步做的是 5 件事**（真体 `out_il2csharp/.../UI_InfiniteScroll.cs`）：

| # | 原版做 | 判据（行号） | 我们 |
|---|---|---|---|
| 1 | `_itemCount = _scrollRect.m_Content.childCount`；`_recordOffsetX/Y` = 前两件的间距；`_disableMarginX/Y = _itemCount × 间距 × 0.5` | `:186` · `:249-250` · `:257-258` | ❌ **不量**，`Visible` 由宿主给 |
| 2 | `scrollRect.onValueChanged += OnScroll`；`OnScroll` 逐件转世界坐标，越过 `_disableMargin + _threshold` 就 `anchoredPosition ±= _itemCount * _recordOffset` 再 `SetAsFirstSibling()/SetAsLastSibling()` | `:200` · `:281-350` | ❌ 无环：靠 `OfferContainer.Carousel.IndexAt` 取模**算**，不搬节点 |
| 3 | `AutoScroll` / `AutoScrollSpeed`（`Update` 里推进 `content.anchoredPosition`） | `:19-20` · `:82-90` | ❌ 无 |
| 4 | `_threshold` 出厂 **100**（序列化可覆盖） | `:367` | ❌ 无（离散模型不需要） |
| 5 | `SetNewItems(ref List<Transform>)` 换数据 | `:97-` | ⚠️ 用 `State.Initialize` 重建代替 |

**还给不出的一格**：抽屉落点 `SlotBox`（`:135`）= **我们挑的** 200×200 常量；原版是 `ItemDrawer.Draw` 按 `contentAnchor` 的布局自己摆。

**今天的可达性**：`MainMenuCarouselDrawer` **没有任何调用方**（`MainMenuOfferContainer.cs:34` 自陈），且我们每份报价只有 1 条奖励 ⇒ 上面 5 条**今天都不产生画面差异**。所以它是「`部分`」而不是「真缺口」——**先把判据补齐、接线时再谈**。

---

## 四 `UIFlippable` 的现状与施工面（判「部分」，不是「真缺口」）

原版实例（逐份现读，不是按类名猜）：

| 节点 | 翻转 | 出现在哪 |
|---|---|---|
| `Counter` | `m_Horizontal 0` / `m_Veritical 1`（**垂直**） | `Card Shop Tab` · `Item Shop Tab` · `Daily Shop Tab` · `Generic Shop Tab` · `Packs Tab` · `Shop Menu Variant` · `Cosmetic Item Shop Container` · `Catalog Item Shop Container`（8 份）+ `MainMenu Warpforge.unity` |
| `Arrow Left 1` | `m_Horizontal 1` / `m_Veritical 0`（**水平镜像**） | `Generic Shop Tab` · `Packs Tab`（各 2 颗：`Arrow Left 1` · `Arrow Left 1 2`） |

**我们侧现状 = 一半**（🔴 我第一版写「完全没有」，自查 `SetUvRect` 调用点时**推翻了** —— 记在这里防别人再踩）：

| 档 | 现读证据 |
|---|---|
| **水平镜像（H=1/V=0）** | ✅ **有等价物，而且是工程里既定的招**：`ImageQuad.SetUvRect(new Rect(1f,0f,-1f,1f))` —— `Shell/CollectionWindow.cs:2453`（`mirror` 时）· `Shell/MatchLogRow.cs:395`（`!mine` 时）· `Shell/CardDetailPopup.cs:2036`（左箭头）；`Shell/MatchLogRow.cs:392` 还明写「翻转走 `SetUvRect`（与本工程 `CollectionWindow.BuildStyleArrow` 那条同款，别另造）」 |
| **垂直翻转（H=0/V=1，原版 `Counter` 用的就是这档）** | ❌ **全仓没有一个负高的 `SetUvRect` 调用**（把 21 处调用逐条看过）。API 是同一句（`height` 取负），但**从来没被验证过** |
| **节点级** | ❓ 我们的商店页里**没有 `Counter` / `Arrow Left 1` 这两颗同名件**（`Shell/ShopTabPage.cs` · `Shell/ShopWindow.cs` 里 `grep "Arrow"` = 0）——是「节点没建」还是「换了名字等价」**未逐节点对** |

**施工面（两格，都很小）**：
1. **垂直翻转**：补一条断言证明 `RebuildMesh` 对**负高** UV 真的上下翻（水平那档已有三处生产先例，可拿它们当对照）。
2. **节点级**：先确认 `Counter` 在我们商店页里以什么名字活着（或压根没建）。⛔ 别据此直接判「商店页缺节点」——那是另一条账（本批只做对账，不动节点）。

---

## 五 顺手发现（⛔ 本批都没改）

1. 🔴 **账的覆盖是「反的」**：`账/` 下 **21 份 TSV 共 9,687 数据行**，其中
   · `原版类` 命中这 5 个**真在用**的类（`UI_InfiniteScroll` / `SoftMaskScript` / `NonDrawingGraphic` / `Gradient2` / `UIFlippable`）= **0 行**；
   · 命中 `FancyScroll*`（`Assembly-CSharp` 全仓 `grep "FancyScroll"` = **0 引用**）= **30 行**。
   ⇒ 那 30 行是「读到（泛型方法体）」不是「用到」，⛔ **别按它们推动复刻**。
   ⚠️ 简报写的「1,066 条对账产物 = 0」这个**分母我没能复现**（可能是某个中间产物）；上面给的是我能复算的口径（行级、可脚本重跑）。
2. 🔴 **三处注释已过期**：`Shell/OfferContainer.cs:1031` 与 `:1072`、`Shell/MainMenuOfferContainer.cs:115` 都写着「`UI_InfiniteScroll` 那份源码本地没有」——
   **不成立**：真体在 `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/UI_InfiniteScroll.cs`（448 行，`ManualInit`/`Init`/`OnScroll`/`AutoScroll` 全在）。`Shell/ShopCarousel.cs` 的文件头（2026-10-31）已经找到它，另两处没跟着订正。
   ⛔ 本批白名单只有 TSV + 报告 ⇒ **没动 `.cs`**，请派给 `Shell/OfferContainer.cs` / `Shell/MainMenuOfferContainer.cs` 的写手（铁律 5 就地改）。
3. **`DropDownList` 留在 13 个战场场景里且 `m_IsActive: 1`**（GO 名就叫 `DropDownList`，含子件）——第三方库 demo 残留。战场线哪天复刻场景时要不要搬它，建议按其「不是游戏功能」判不搬（本批只留痕）。
4. **`assets_full` 那 84 个 bundle 我没扫**：本轮只扫了 AssetRipper 导出的 1,389 prefab + 17 场景（UI 组件都落在这两处）；若要做到「绝对不漏」，把同一脚本的根换成 `assets_full` 再跑一遍即可（脚本逻辑 = `.cs.meta` 取 guid → 扫 `m_Script.guid`）。

---

## 六 边界（如实记）

- 本表只覆盖**「原版挂上/引用」这个事实**，**不判「我们该不该做」**——`UIFlippable` 那两颗节点在不在，我没对到节点级（见 §四）。
- 「等价已实现」三条（`SoftMaskScript` / `NonDrawingGraphic` / `Gradient2`）是**行为等价**，不是组件等价：我们用「量 alpha / 透明 quad / 烘贴图」三套不同机制达到同一可见结果，判据都在各自文件的 doc 里（TSV 第 2-4 行给了行号）。
- 数字口径：**155 类** = 简报口径；我实际数到 **177 个 `.cs` 文件 / 146 个带 `.cs.meta`**（含 `ColorPicker` / `EasingCore` / `Tweens` 三个子目录）。两者不冲突（一个文件里可能有多个类型），**引用张数一律以 TSV 为准**。

---

## 三处注释订正 + UIFlippable 节点核对（`B115_W19` 第二波 · 2026-11-01）

> 白名单：`Shell/OfferContainer.cs` · `Shell/MainMenuOfferContainer.cs`（**只改注释**）。
> 🔴 **行数守恒是硬要求**：`OfferContainer.cs` 的账落点一直钉到 **:1805**（`账/商店.tsv` 有 :103/:128/:150/:191/:211/:239/:264/:282/:295/:304/:956/:1052/:1082/:1161/:1199/:1339 等 29 处、`账/未分类_其他.tsv` 另有 :1082/:1805）⇒ 插一行就全漂。
> **结果：`OfferContainer.cs` 2010 → 2010 · `MainMenuOfferContainer.cs` 250 → 250；两个文件都是纯 LF，改前 = 改后。**

### 一 · 三处过期注释（铁律 5 · 同物理行就地订正）

| 文件:行 | 改前（摘要） | 改后 |
|---|---|---|
| `Shell/OfferContainer.cs:1031-1032` | 「`ManualInit` 的一屏槽数（…原版是 `UI_InfiniteScroll` 从布局自己量的，**那份源码本地没有** ⇒ 这一格由调用方给）」 | 仍是「由调用方给」，但把错的理由换成 **`⚠️ 2026-11-01（B115_W19）就地订正（铁律 5）**：真体在 `out_il2csharp/…/UI_InfiniteScroll.cs`（448 行，`ManualInit` :55 → `DelayedInit` :67 → `_itemCount` :186）；`ShopCarousel.cs`（10-31）已据它建成离散等价」 |
| `Shell/OfferContainer.cs:1070-1072` | 「`⚠️ **那份源码本地没有**（有 prefab、无脚本）⇒ 我们只留它保证的那一件事…」 | 同一位置改成「**订正：真体在** `…/UI_InfiniteScroll.cs`（448 行：`ManualInit` :55 · `Init` :186 量 `_itemCount`/`_recordOffset`/`_disableMargin` · `OnScroll` :281 · `AutoScroll` :19）⇒ 我们只留它保证的那一件事…」 |
| `Shell/MainMenuOfferContainer.cs:115` | 「一屏几槽（= `UI_InfiniteScroll` 从布局量的那个数；**那份源码本地没有** ⇒ 由宿主给）」 | 单行内订正：真体在哪 + `Init :186` 从 `content.childCount` 量 + `ShopCarousel.cs`（10-31）已建离散等价、本类仍由宿主给 |

⛔ **代码行为一个字没动**（`Visible` 仍由调用方给、`ManualInit` 仍是两句）。

### 二 · 三闸读数

```text
$ TMPDIR=/tmp/wf_w19e bash d:/4/Unity/工具/typecheck.sh
运行时错误数: 0
编辑器错误数: 0

$ python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py
合计：A 5831 条 · 通过 5831（其中告警 864 条）· 漂移 0 · 失败 0

$ python -I -X utf8 d:/4/Unity/工具/check_aggregate_lines.py \
    MyGame/Assets/CardPresentation/Shell/OfferContainer.cs MyGame/Assets/CardPresentation/Shell/MainMenuOfferContainer.cs
合计：文件 2 · 聚合行 0 · 调用 0 · 违规 0  ⇒ PASS   (rc=0)
```

### 三 · `UIFlippable` 的节点级核对 —— 结论：**两颗都是「没建」，不是换名等价**

先把原版那两颗拆到底（判据 = prefab 逐字段 + 代码按名搜）：

| 原版节点 | 它到底是什么（判据） | 我们有没有 |
|---|---|---|
| **`Counter`**（8 个商店 prefab 的 `background` 下） | GO 组件 = `Image` + `UIFlippable(H=0,V=1)` + 两颗自定义件（`counter` / `greyscale`）＋ 子件 `Text (TMP)`；**Image 的 sprite = `40K_main_deck_card counter`**（`Sprite/40K_main_deck_card counter.asset`）；`anchorMin (0, 0.1493)` + `sizeDelta (0,0)` = **拉伸填满 background** | ❌ **没建**。我们的商店页里 `Counter` 只以 **`Available Counter`（`Available: 1/5` 那行字）** 与 **`TimeCounter`** 出现 —— 两者都是**页头/商品格上的文本**，不是这颗 chip（`ShopTabPage.cs:149/:152` 的矩形也说明它们各有自己的框）。底图我们**有**：`CollectionWindow.cs:261 CardsCounterSprite = "40K_main_deck_card_counter"`，但**只在收藏窗卡组视图用**，商店 0 处 |
| **`Arrow Left 1` / `Arrow Left 1 2`**（只在 `Generic Shop Tab` · `Packs Tab`） | 单 GO（36.57×36.69）+ `Image`（sprite = **`40k_general_bt_arrow_0`**）+ `UIFlippable(H=1,V=0)`；**没有子件** | ❌ **没建**。`Shell/ShopTabPage.cs` · `Shell/ShopWindow.cs` 里 `grep "Arrow|箭头"` = **0**；同一素材 `40k_general_bt_arrow` 我们只在**战斗教程**用过（`Battle/TutorialOverlay.cs:726` · `Battle/CardDisplayWindow.cs:2111`） |

**换名也排除了**：`40K_main_deck_card_counter` 与 `40k_general_bt_arrow*` 这两张图，在 `Shell/` 的商店链路里**一次都没被加载过**（按贴图名 grep）⇒ 不是「改了名字的等价件」。

**两条旁证（决定优先级）**：
- 原版**游戏代码里 0 处按名引用**这两颗：`grep -rn "Arrow Left" Assembly-CSharp` = 0；`ShopTab.cs` 里那颗 `NotificationCounter`（`UiBadgeNotification`，`+0x60`）**是页签红点，不是这颗 `Counter`**。
- ⇒ 它们是**纯 prefab 静态件**（无运行时接线）⇒ 差异只在**观感**，不改任何行为。

### 四 · 施工面（判「没建」之后要不要补）

| 要做 | 具体 | 依赖 |
|---|---|---|
| ① 补 `Counter` chip（8 个商店页签） | 底图已有（`CardsCounterSprite`）＋ TMP 数字＋`greyscale`；落点 = `ShopTabPage` 建页头那一段 | **必须先有垂直翻转**（原版 `H=0/V=1`）——我们**零先例** |
| ② 补 `Arrow Left 1` ×2（`Packs`/`Generic` 页签） | 底图有（`40k_general_bt_arrow`）；水平镜像**已有先例**（`Rect(1,0,-1,1)` 三处） | 无（水平这档现成） |
| ③ 补一条「负高 UV 真翻转」的断言 | `ImageQuad.SetUvRect` 传 `height<0` → `RebuildMesh` 上下翻；拿三处**负宽**生产先例当对照 | 无（纯断言，不跑 Unity 也能写、要跑一次才验） |

⚠️ **优先级 P2**：两颗都无代码引用、只影响页签观感 ⇒ 排在「有行为差异的账」之后；但按铁律 11 **属于「要做，只有先后」**，⛔ 不是「不做」。
⚠️ 本批**只出对账**，⛔ 没建任何节点（`ShopTabPage.cs` 一个字没碰 —— 它不在本波白名单里）。

### 五 · 顺手发现（本波）

5. **`Counter` 与 `Arrow Left 1` 在原版是「死装饰」**：`Assembly-CSharp` 全仓 0 处按名引用、`UI_InfiniteScroll` 也没接它们（`OnScroll` 只搬 `content` 的子件）⇒ 它们不会随滚动/翻页动。复刻时**照静态件建**即可，⛔ 别给它编交互。
6. **我们的商店页与收藏窗共用了一张图但两条路**：`40K_main_deck_card_counter` 在收藏窗是一条「张数条」（`CollectionWindow.cs:359-369`），在商店页签里是「chip」——同一张贴图两种用法，⛔ 别拿收藏窗那段直接搬（矩形与翻转都不一样）。
