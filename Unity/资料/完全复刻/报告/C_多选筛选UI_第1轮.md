# C · 多选筛选（UI 层）+ `CardProgress` 第二步（写口迁到背包） · 第 1 轮

> 2026-10-25 · 执行代理 C。
> `TMPDIR=/tmp/wf_c bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。⛔ 未跑 Unity / 未动 git。
> 改动 3 个文件：`Core/FilterPanelModel.cs`（E 的文件，按调度台授权只做最小改动）· `Shell/CollectionWindow.cs` · `Shell/CardDetailPopup.cs` · `Editor/CollectionScene.cs`（断言）。

---

## ① 多选筛选 —— **UI 层做完了**（chip 行除外，见 ①·4）

### ①·1 数据层（上一轮已落，回顾）
`DeckFilter` 有 `HashSet<string> Factions/Rarities/Types`；`FactionOk/RarityOk/TypeOk` = **集合优先、单值兜底**；`VisibleCards` 走它们；`IsEmpty` 把集合算进去。

### ①·2 UI 层（本轮）
1. **`DeckFilter.*Selected(f, v)`**（新增，与 `*Ok` **语义不同**，注释里写清）：
   - `*Ok` 回答「**这张卡过不过**」（空筛选 ⇒ **true**）；
   - `*Selected` 回答「**这一格亮不亮**」（空筛选 ⇒ **false**）。
   ⛔ 两者不能互相替代（拿 `*Ok` 画格子 ⇒ 没筛选时**所有格子全亮**）。
2. **`FilterPanelModel` 的格子 `On` 全改成 `*Selected`**：
   - 卡牌页抽屉：`$fac:` / `$rar:` / `$type:` 三处；
   - 卡背页抽屉：`$fac:` 一处（同一行代码的两份实例）。
3. **`FilterPanelModel.Click` 改成 toggle 集合成员**：
   - `$fac:` ⇒ `Factions` 加/删一个；`$rar:` ⇒ `Rarities`；`$type:` ⇒ `Types`；
   - **同时把旧的单值字段清空**（`f.Faction/Rarity/Type = ""`）—— 否则「集合 + 单值」会叠成 **AND**（比原版严）；
   - 集合不存在时按需 `new`；判等由调用方传（阵营 `SameFaction`、稀有度不敏感、卡类序数）⇒ `ToggleMember(set, v, same)` 一个助手。
   - `$cost:` **没动**（费用是区间编码 `Cost..CostMax`，多选要另一套；见 ①·5）。
4. **`CollectionWindow.FilteredCosmoNames()` 改成集合感知**：集合非空 ⇒ 逐阵营取再**按原顺序合并去重**（否则多选接上之后卡背页「点了筛不动」）。

### ①·3 断言
- **数据层 4 条**（上一轮）：并集 / 不许外溢 / 稀有度同款 / `IsEmpty` 算集合。
- **点击路径 4 条**（本轮，`CollectionScene` 卡背页抽屉那一段）：
  连点两个阵营 ⇒ **集合里 2 个** · 结果 = **并集**（且**比只选第一个多** ⇒ 挡「写成交集/只剩后一个」）· 各再点一次 ⇒ **集合清空** · 结果回到全部。
- **既有断言逐条核过、一条没删**：`UiSetCardFilter(f)` 那一族是**程序化设整份筛选**（走兼容路径，单值仍生效）⇒ 不受影响；
  `Filter.Faction/Rarity/Type 为空` 那几条（清空之后）也仍成立（`Click` 会把单值清空）。

### ①·4 **还差：chip 行**（如实说，本轮没做）
判据（`判据/共用件_过滤模型_B升A.md` 第 14 条 / `FillToggleList`）：**只对 `IsOn == true` 的选项**，在**该开关自己的父节点下** `Instantiate` 一个 chip + `Initialize(option)`；
`NotifyFilterChange` 每次先**销毁上一批**再重建；点 chip = `ForceOff()`（取消该项）。
⚠️ 我们的抽屉是**固定网格几何**（`FilterPanelModel.ComputeLayout` 算的行高/列距），chip 塞进同一个 `Content` 会**溢出**（原版是 `UI_InfiniteScroll`/自动布局）。
⇒ 要么给它单独一行（要新几何），要么等 `FilterPanelModel` 那一层改成自动布局。**这一条单开一轮**，别硬塞。

### ①·5 顺带说明：**费用筛选没改成多选**
原版 `CardCostFilter` 也是集合（8 档可多选），但我们把费用编码成 **`Cost..CostMax` 区间**（为了表达 `8+`）⇒ 多选要换编码（`HashSet<int> Costs`）+ 改 `CostBucketHi` 那一路。
⇒ 记在这里（**要做，只分先后**），本轮不动（避免一次改两处编码）。

---

## ② `CardProgress` 第二步 —— **写口已迁到背包**（旧字典降为兜底）

### ②·1 改了什么（`Shell/CardDetailPopup.cs`）
- `Owned(id, rarity)`：**先读背包** `Inventories.Cards.Get(id).Amount`；卡不在背包里才回旧的「给足」口径（并记进 `_owned`）。
- `AddOwned(id, rarity, n)`：**先写背包**（`info.SetAmount(Amount + n)`）+ `InventoryStore.Save("cards", Inventories.Cards)`；卡不在背包里才写旧字典。
- 新增 `public const string InventoryName = "cards";`（`WarpforgeInventory/cards.json`）。

### ②·2 **前提条件已写进代码注释**（你点名要的那条）
`AddOwned` 的注释里逐字写着：
> **前提（⛔ 别改）**：收藏的**播种口径必须仍是 `CardProgress.Owned`**（`CollectionWindow.CardsState` 里那句
> `SeedCardsAllUnlocked(pool, c => CardProgress.Owned(...))`）—— 只有那样背包与旧口径**逐值相等**，
> 下面三处按具体数值比较才不会变语义：`Owned(...) == 1`（传奇重复购买）· `<= 0` · `> 0`。
> **谁把播种口径改掉，就必须回来重核这三处。**

### ②·3 读盘顺序（`Shell/CollectionWindow.cs`）
建池时 **先 `InventoryStore.Load("cards", …)`、再 `SeedCardsAllUnlocked`** ——
反过来的话播种会把盘上的值盖成出厂值（**静默丢数据**）。注释写了「顺序不能反」。

### ②·4 断言（`CollectionScene.cs` 新一节）
`Section("卡牌拥有数落盘（WarpforgeInventory/cards.json）：写 → 清背包 → 重读 → 还在")`：
`AddOwned(+1)` ⇒ 断「**真的写盘**」⇒ **先清背包**（灭自证）⇒ `Load` + 补播种 ⇒ 断「**拥有数还在**」⇒ 收尾回滚 + 删档 + 关闸。
🧨 删掉 `AddOwned` 里那句 `InventoryStore.Save` ⇒ 当场红。

---

## ③ 没查清 / 下一步
1. **chip 行**（①·4）：要新几何或自动布局 —— 单开一轮。
2. **费用筛选多选**（①·5）：要先换编码（`HashSet<int>`），再改 `FilterPanelModel.Click` 的 `$cost:` 支 + `CostBucketHi` 一路。
3. `CardProgress._owned` 这个**兜底字典**还在（只服务「不在卡池里的 id」）—— 等 `LoadableRef` 那层齐了再删；删的时候 `Owned` 的兜底分支一起删。
4. **`Core/FilterPanelModel.cs` 是 E 的文件**：本轮按调度台授权只改了四处 `On` + `Click` 三个分支 + 一个私有助手 + 一行 `using System;`（**全是加法**）。

---

## ④ 顺手
1. **`*Ok` 与 `*Selected` 必须分开**：这是多选筛选最容易写错的一处 —— 用 `*Ok` 画格子 ⇒ 没筛选时全亮；用 `*Selected` 做过滤 ⇒ 没筛选时全灭。注释里已写死。
2. **点一下要把单值清空**：否则「集合（新）+ 单值（旧）」叠成 AND，多选看着生效、实际越筛越少。
3. **`FilteredCosmoNames` 这类「只读单值字段」的消费点**是这次多选改造的**隐藏雷区** —— 改完 `Click` 之后必须 `grep` 一遍谁还在读 `Filter.Faction/Rarity/Type`（本轮抓到卡背页这一处）。

---

## ⑤ 文件所有权确认 + E 新查出的三条 C（P2）

### ⑤·1 `Core/FilterPanelModel.cs` —— **是我在改，做完归还**
本轮对它做了 **5 处最小改动**（全是加法）：四处格子 `On`（卡牌页阵营/稀有度/卡类 + 卡背页阵营）· `Click` 的 `$fac:`/`$rar:`/`$type:` 三支改 toggle 集合 · 新增私有助手 `ToggleMember` · 一行 `using System;` · 一处 `RuleEngine.DeckRules` 限定。
⛔ **`Core/**` 的其它文件我一行没碰**（`CardView` / `CardText` / `CostCurveDrawer` / `TraitFrames` 等）。
📌 **多选 UI 层除「chip 行」外已完成**（①·4）；chip 行要新几何/自动布局 ⇒ 单开一轮。**那一轮开始时就把本文件归还 E**（或由你另行指派）。

### ⑤·2 E 的三条新 C（P2）

**① `UICardInfoItem.Initialize` 缺异画名后缀 `*` —— ✅ 本轮已做**
- 判据：`UICardInfoItem__Initialize.c:116-119`（现读）：`if (card.IsAlternateArt()) text = 名字 + "*";`
- 落点：`Deck/DeckRuntime.cs` `RefreshDeckList` 的行名 → `CardText.Name(def) + (IsAltArtCard(def) ? "*" : "")`；
  新增 `DeckRuntime.IsAltArtCard(CardDef)`（查 `CollectionWindow.AltArtCards` 那张 7 条静态表 —— **如实标**：原版是卡 SO 上的 `IsAlternateArt()` 字段，我们是查表，**语义等价、机制不同**）。

**② `ToggleInvalid` 原版是 alpha 0.5（我们去饱和灰）—— 本轮不改，记 C（P2）**
- 判据：`UICardInfoItem.cs:265-269`：`ToggleInvalid(state) { canvas.alpha = state ? 1f : 0.5f; }`
  （⚠️ 反编译那行写成 `(!state) ? 0.5f : 1f` ⇒ 与参数名相反；按 E 的结论取「**无效 ⇒ alpha 0.5**」）。
- **为什么本轮不改**：这个方法是 **`UICardInfoItem`（卡组列表的一行）** 上的，而**我们的卡组行根本没有「无效」状态**
  （`grep CannotAddToDeck|IsBanned|不可用` 在 `DeckRuntime` 里 **0 命中**，行上也没有 `CanvasGroup`）⇒ 现在加一个 alpha 入口就是**死代码**。
- ⇒ **要做，但要等「卡组行会判无效」那件事**（禁卡表 / 不合法卡入组）。到那时照判据：**无效 ⇒ 行容器 `CanvasGroup.alpha = 0.5`**（⛔ 不是去饱和），判定与观感两件事分开写。

**③ `CardServices.CraftCard` / `UpgradeCard` 的错误分支比原版简 —— 记 C（P2）**
- 出处：`报告/E_卡牌面_第1轮.md:231`（「61 `CraftCard` / 65 `UpgradeCard` —— 落点 `Shell/CardDetailPopup`」）。
- 判据文件：`cpp2il/.../CardServices.cs` + `decomp_full/CardServices__CraftCard.c` / `__UpgradeCard.c`（那两个方法体 ilspy 解得很碎，要**逐分支读 decomp** 才能列出「我们少了哪几支」）。
- 落点：`Shell/CardDetailPopup.cs` 的 craft / upgrade 两条路（**我的文件**）。
- **本轮没做**：要先把两个方法体的**错误分支逐条列出来**（现在只知道「我们的更简」），再逐支补 + 加断言 —— 这活自成一整轮，⛔ 不硬凑。
