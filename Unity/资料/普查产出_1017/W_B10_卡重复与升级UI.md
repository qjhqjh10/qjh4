# W_B10 · 「卡重复与升级」三件 UI —— 写手报告（2026-10-17）

> 账：`资料/盘点_战斗部分文档.md:104`（三件 UI）· `:105`（D 段 7 条「查不到」）。
> 判据正本：`资料/卡牌重复与升级_原版规格.md`。落点 = 施工表 `资料/普查产出_1017/现核_按文件施工表.md:124-126`
> （① → `Shell/CollectionWindow.cs` · ②③ → `Shell/CardDetailPopup.cs`）。

---

## ① 卡池格子 per-card 计数 —— ✅ **本笔新做（原来是零）**

- **原版**：卡格 `Collection Card/Content/Counter`（预置里 `m_IsActive = True`）= 底图
  `40K_main_deck_card counter` + 子件 `Text (TMP)`。文案 = `String.Format("x{0}", InventoryManager.GetOwnedCount(card))`
  = **原始拥有数**（**不**与卡组上限取 min）；同一处 `ToggleGreyScale(拥有数 < 1)` 置灰。
- **判据**：`decomp_full/CardCollectionDisplay__SetCell.c`（与 `__UpdateCardVisuals.c:45-47` 逐字同段）·
  预置逐字段现读（`bundle_menus_assets_all`）：`RectTransform_-5006247910548507304`（Counter）·
  `RectTransform_-4591212569639258792`（Text）· `MonoBehaviour_435197177245070680`（TMP）·
  `MonoBehaviour_-6064368872558693032`（`CollectionCardCounter` 的 `counter`/`greyscale` 两个引用）；
  逐件表另见 `资料/普查产出_0923/A3_Cards页.md:136-137`。
- **我们（改前）**：`RebuildCardsCells` 只有 `CardView` + 一个命中区，**一个 per-card 计数都没有**。
- **改成什么**：每格按其**滚动后的矩形**建 `Counter i`（底图，`keepAspect`）+ 子件 `Text (TMP)`；
  几何**全部由锚点现算**（`CardsCntMinX/MaxX/MinY/MaxY` + `CardsCntTxtMinY/MaxY` × 格尺寸），
  **不写死** 75/187.5/37.504 那一组数。
- **落点**：`Shell/CollectionWindow.cs:160-234`（常量 + `CardsCounterRect`/`CardsCounterTextRect`/`CardsCounterText`/
  `BuildCardsCounter`）· 调用点 `:904-906` · 自检访问口 `:916-923`。

**回归面**：`Rect`/`Text` 两个助手自动吃 `<holder/Viewport>` 那颗 `ViewportClip`（父链解析），与卡同一套裁切；
渲染队列用页内 `QPageRow/QPageText`（3032/3033 > 卡的 3000 ⇒ 压得住卡；< 筛选栏 3040+ ⇒ 抽屉仍压得住它）。

## ② 详情弹窗「计数」—— ✅ **已存在，本笔【不改】**

- `Shell/CardDetailPopup.cs` 的 `BuildCounter(panel)`（`:741-842`，`Build()` 里 `:404` 调用）**已经建好了**
  `Card Counter` / `Duplicate Counter` / `Single Counter` 两支树，且 2026-10-13～10-16 已按原版逐颗核过
  （框/字号/auto/base/对齐/竖排档，`CcSg*` 一族）。⇒ 三件里的「计数」**不是缺口**，账上那句是 2026-09-22 的旧态。
- 「创建副本」「升级」两块：**用户 2026-09-27 已拍板不做**（`Build()` `:392-402` 明写 + `Debug.Log` 出声）。
  ⛔ 本笔没建、也没拿它当缺口（`Craftable`/`WildcardIconFor` 保留不删）。
- **战斗侧那扇**（`Battle/CardDisplayWindow.cs`）**不适用**：13 个竞技场的 `CardDisplayWindow.options` 全是
  `{0,0}`（= null）⇒ 原版战斗侧**永远不出** `CardDisplayOptions` 那一块（`W_B4_战斗详情窗.md:17` 的侧证同此）。
  属 `Battle/**`，本笔白名单外 ⇒ 未动。

## ③ 通配符计数条 —— 数字已拍板（不改）· **补了「按阵营」那颗 `Army Icon`**

- **数字「从哪来 / 切阵营怎么变」= 查清了**：`WildcardDisplay.SetCountersText` 把 `Inventory<Wildcard>` 按
  `Item.Army == 当前阵营` 过滤后按 `Rarity 1/2/3/4` 取 `Quantity`（**库存直出、无换算**，2026-09-26 已订正过口径）；
  「切阵营」的触发者 = `CardCollectionDisplay__CheckFocusedArmy.c`（`Reference Card Pointer` 的 rect
  `OverlapsAny` 卡位 ⇒ 命中那张卡的 `cardArmy` ⇒ `Initialize(army)`）。
- **我们**：三处（卡组编辑 / 收藏窗 / 详情窗）数字 = **恒定 `99`** —— **用户 2026-09-28 拍板**，`DeckScene.cs:1103-1105`
  已有断言。⛔ 不是「写死 `0`」（那只在 `DeckRuntime.BuildHeader` 的出厂字面量里，见下）。
- **本笔补的**：收藏窗那条 `WIldcard Display` **缺第三个孩子 `Army Icon`**（原版 1470,70.94→1550,155.94 · 80×85 ·
  sprite `40k_DeckSelection_icon_FactionBlackLegion` · PA=1）⇒ 已按**预置出厂那一张**补上。
  判据 `A3_Cards页.md:192,205`；落点 `Shell/CollectionWindow.cs:800-807`。
- `DeckRuntime` 出厂字面量 `"0"` → **`"99"`**（`DeckRuntime.cs:881`）：原来靠 `RefreshHeader()` 事后改，
  中间有一个瞬间它**真是 `0`**。值仍与 `:2468` 那句同源。

---

## 改动清单（含断言）

| 文件 | 改动 |
|---|---|
| `Shell/CollectionWindow.cs`（+128 行，纯新增） | ① 卡格计数常量/几何/文案 + `BuildCardsCounter` + 调用点 + `CardsCellDef` 自检口 · ② 计数条 `Army Icon` |
| `Deck/DeckRuntime.cs`（+6/−1） | 通配符条出厂字面量 `"0"` → `"99"` |
| `Editor/CollectionScene.cs`（+81 行，追加在 Cards 页那一节） | **14 条断言调用**，其中一条是「17 格逐格」的聚合（`Section` 在 `:2937-2998`，`Army Icon` 在 `:3000-3011`） |

**断言（`CollectionScene.cs`）**：节点建出来了 · 底图 = `40K_main_deck_card_counter` · 渲出 **112.5 宽** ·
渲出 **34.91 高** · 落位 `格左+131.25 / 格顶+364.48` ±1.5px · 有子件 `Text (TMP)` ·
**17 格逐格**比「传说 `x1` / 其余 `x2`」 · `Army Icon` 在 1470…1550 × 70.94…155.94 · 徽记 = `…FactionBlackLegion`。
**三条判别式**：
1. `keepAspect` 抽掉 ⇒ 高从 34.91 变 **37.50**（差 2.59 > 容差 1.6）⇒ 红；
2. 文案 ≠ `"x" + CardProgress.Owned(...)`（= 原始拥有数）⇒ 谁「按原版公式复原」把 `Mathf.Min` 删掉就红；
3. `Army Icon` 改喂 `FactionIcon(null)`（= `40k_collection_bt_decks`）⇒ 红。
（期望值按稀有度**独立写死**，⛔ 不取 `CardsCounterText` —— 那会自证。）
⚠️ 量落位前先把 `CardsScroll.SetOffset(0)`、量完还原（与紧随其后的 A12 那段同做法）。

## 没查清 / 拍板不做 / 顺手发现

1. 🔴 **一处「我们挑的」偏离，如实标注**：卡格那条印 **`min(拥有, 卡组上限)`**，**原版印裸拥有数**。
   理由：`CardProgress.Owned` 是「给足」口径（每张 10~11）⇒ 照原式会把每格印成 `x11`；
   这一档**沿用同一个节点**在 `DeckRuntime.PoolCounterText`（「还没有督军」支）里**用户 2026-09-28 已拍板**的写法。
2. **置灰那一支本版不可达 ⇒ 没画**（出声）：原版 `拥有数 < 1` ⇒ `CollectionCardCounter.Set(…, true)` 走
   `UIImageGreyscaleController`（`Image` + 灰材质对）；我们 `Owned ≥ 10` 恒真不了，且 `ImageQuad` 没有判据灰值 ⇒ 只记不画。
3. ⚠️ **顺手发现（未动）**：`DeckRuntime.cs:891` 的 `hdr_army` 喂的是 `FactionIcon(null)` =
   **`40k_collection_bt_decks`**（一张**卡组**图标，不是阵营徽记），且 `DeckScene` 只断言「建起来了」。
   ⇒ 现在两扇窗同一个位置**显示不同图标**（卡组编辑 = 卡组图标 / 收藏窗 = BlackLegion 徽记）。
   **判据倾向后者**（预置 `armyIcon` 的 `m_Sprite` 实读 = BlackLegion）。**请主对话裁定是否统一**（未改，白名单外不作为）。
4. ⚠️ **顺手发现（未动）**：原版 `GetMaxCopiesInDeck` 有条 **`cardType == 10 (督军) → 1`** 的硬编码；
   我们的 `CardProgress.DeckCap` / `DeckRules.CopyLimit` **只看稀有度** ⇒ 非传说的督军会算成 2。
   三处（卡格 / 卡池 / 详情窗）同源，**要改得三处一起**（`DeckRules` 在 `RuleEngine/`，白名单外）。
5. ⚠️ `A3_Cards页.md:137` 把卡格那颗 TMP 的对齐记成 `hAlign = Right`；**现读字段 = `2 (Center)`**
   （MB `435197177245070680`）⇒ **以字段为准**，本处**不调** `Align*`。建议就地订正那张表（本笔未改该文档）。
6. **D 段 7 条**（`requiredCopies`/`Cost.Amount`/`pointReward`/`xpReward`/货币/通配符发放/卖重复卡单价/
   其他阵营通配符资产/战将 `x1x0` 走哪条路）：**判据在服务端 LiveOps `CardTierConfig` ⇒ 一律留白，本笔一个数都没填**
   （本笔也不碰升级/创建副本，见 ②）。
7. ⚠️ **未跑 Unity**（禁令）：断言**只写不跑**。⚠️ 上面 ① 的「渲出 34.91 高」依赖工程里那张 PNG = **116×36**
   （已用 IHDR 二进制核对 `Resources/Art/ui_deck|ui_menu/40K_main_deck_card_counter.png`）——若将来换图，这条要跟着改。

## 类型检查（原样贴）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
命令：`TMPDIR=/tmp/wf_b10 bash d:/4/Unity/工具/typecheck.sh`（改完 `.cs` 各跑一次，末次在全部改动之后）。
行尾核对（二进制计数）：`CollectionWindow.cs` `crlf/lf = 0/3023`（LF）· `CollectionScene.cs` `0/6883`（LF）·
`DeckRuntime.cs` `5289/5289`（**纯 CRLF，未翻行尾**）。
`git diff --numstat`：`CollectionWindow.cs 128/0`（**全是本笔**）· `CollectionScene.cs 271/0`（本笔占 81 行，
其余是他人既有改动，同一份未提交工作树）· `DeckRuntime.cs 915/154`（**本笔只占 6/1**）。
