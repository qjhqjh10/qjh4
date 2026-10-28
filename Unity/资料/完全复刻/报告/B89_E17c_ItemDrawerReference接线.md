# B89 · E17c —— `ItemDrawerReference.options` 接线（`165` 的 `showName` + `58` 的小图标槽）

> 执行代理 `E17c` · 2026-10-28 · 第八十九会话（最后一件）
> ⛔ 未跑 Unity / 未跑 `ruleprobe` / 未动 git / 未改账主表（只写 `_parts/B89_E17c.tsv`）/ 未改正本 / 未碰 `d:/2`（只读）。
> 白名单内改动：**新建** `Shell/ItemDrawerOptions.cs` · `Shell/ItemDrawer.cs`（4 行**就地改写**、行数不变）。

---

## 〇 一句话

那张表的**语义查全了**（20 条 × 3 格，三源印证），并且发现它**不是**单独起作用的 ——
**「画不画」= `options` 那格 ∧ prefab 上有没有承载件**（`ItemDrawerComponents.label` / `quantity` 是不是 null）。
结论有两条**推翻了我上一批的推测**：

1. **`165` 的名字原版根本不显示**：`PremiumItem.showName = 0`（而且 `Premium Drawer` 的 label 是**有**的 ⇒ 只靠 options 才藏住）。
2. **`58` 的名字在 Currency 抽屉里也没地方写**：两个 currency prefab 的 `label` **都是 `{fileID: 0}`**（= null）—— 所以 `showName = 1` 也看不到名字。

⇒ 这两个名字的接线**没有可画的落点**（⛔ 不为让函数有调用方去造一个原版没有的名字 UI）。
真正**接上并且有可观察差异**的是第三格 **`stackable`**（已在 4 个数量口生效）。

---

## §1 表本身（现读结果）

### 1·1 三格各管什么

| 字段 | 管什么 | 我们的消费者 |
|---|---|---|
| `stackable` | 这一档**画不画数量** | ✅ **已接**（`ItemDrawer` 的 4 个数量口） |
| `showName` | 这一档**画不画名字**（还要 prefab 上有 `label` 才可能画出来） | ⛔ **表里有值、没有消费者**（理由见 §2/§3，是**查明的结论**不是漏接） |
| `typeString` | 类型行那条串（4 条非空） | ⛔ 只读用途 —— **不是文案**（`CosmeticItemTitle` 是 `showName=0` 却也有 `typeString` ⇒ 两字段不同义；且没验证它是不是最终显示串）⇒ ⛔ 不印 |

**哪些原版 `ItemDrawer` 用它**：消费点是 `ItemDrawer<T>.Draw`（算完 `GetItemName` 之后按这两格决定画不画）；
`GeneralOfferPopupDrawer__DrawRewards.c:319` 传 `0x1e`(=30=`OfferPopups`) ⇒ 奖励弹窗走 override 30，
而**全表只有 3 个类型写了 30**（`CosmeticItemTitle` / `ExpansionPremiumItem` / `CosmeticItemAvatarBorder`），
其余 17 个类型在奖励弹窗里**回落基础档**。

20 条的 `stackable`/`showName`/`typeString` 全文 → 已抄进 `Shell/ItemDrawerOptions.cs`（key = 原版类型名逐字）。

### 1·2 🔴 本轮新查的一条：`options` 只是**半个闸**，另一半在 prefab 上

读取方式：`d:/2/tools/assetripper_out/ExportedProject/Assets/<prefab>.prefab` 里 `ItemDrawerComponents` 的字段引用
（`fileID: 0` = **null**）。

| prefab | `image` | `label` | `quantity` | 抽屉类 | 谁用 |
|---|---|---|---|---|---|
| `Currency Drawer` | ✅ | **null** | ✅ | `CurrencyDrawer` | `Currency` 基础档（奖励弹窗） |
| `Icon Currency Drawer Variant` | ✅ | **null** | **null** | `CurrencyDrawer` | `Currency` override **10**（**商店里用的就是它**） |
| `Premium Drawer` | ✅ | **✅ 非空** | **null** | `PremiumDrawer` | `PremiumItem` 基础档（奖励弹窗） |
| `Icon Premium Campaign Drawer Variant` | ✅ | **null** | **null** | `PremiumDrawer` | `PremiumItem` override 10 |

⇒ 这就是为什么 **`Currency` 的 `showName = 1` 却看不到名字**（`label` 是 null，没有地方写）；
而 **`PremiumItem` 的 `showName = 0` 是唯一挡住名字的那道闸**（`Premium Drawer` 的 label 是**有**的）。
两格数量都对得上 `stackable = 0`（`Premium Drawer` / Icon 两档 `quantity` 全 null）。

---

## §2 `165` 的 `showName` 接线 —— **结论：原版不显示，我们不画 = 等价**

- 原版链：`PremiumItem.showName = 0`（`ItemDrawerOptions` 表）**∧** `Premium Drawer.label` 非空
  ⇒ 名字那一块被 `options` 关掉（这一件**恰好是唯一「prefab 有 label、靠 options 才藏住」的案例**，值得记一笔）。
- 另外 `PremiumItem.stackable = 0` **∧** `Premium Drawer.quantity = null` ⇒ 名字和数量**都不画**。
- 我们这一侧：`Icon` 抽屉**没有 label 槽**、数量口已在 §4 接上 `stackable` ⇒ **两者都不画**。
- ⇒ 「接上 `showName`」的正确结果就是**什么都不画**（⛔ 不是「画一个 `MenuShop/PremiumCampaign`」）。
  **值仍然记死**：哪天要画，必须 `Loc.T("MenuShop/PremiumCampaign")`（EN `Premium Campaign` · ZH「高级战役」），
  ⛔ **不是** `CampaignData.PremiumTable[].SoName` —— `soName` 全仓只 4 处、全是诊断/夹具，今天没有界面对着它印字。
- `ItemDrawerOptions.ShowName` 里把这个理由写死在注释上，并且**判据空时返回 `false`**（= 少画）；
  与 `Stackable` 的兜底 `true`（= 多数档，保持改前行为）**刻意相反** —— 判据选的是「宁可少画一个原版可能没有的件」。

---

## §3 `58` 的小图标槽 —— **槽在那儿，但只在基础档，且我们的抽屉没有那一行**

- 原版只有 **`Currency Drawer`（基础档）** 的 `ItemDrawerComponents.icon` 是**非空**的
  （`CurrencyDrawer.Draw` 里那句 `if ((bool)icon) icon.sprite = item.GetIcon(Small);` 就是它）。
  `Icon Currency Drawer Variant`（商店那档）`icon = {fileID: 0}` ⇒ **商店里的小图标也不画**。
- 槽的位置**逐格读到了**（`menu_dump.py … "Currency Drawer"`，绝对矩形 · 1920×1080 · y 向下）：

| 节点 | 绝对矩形 | 说明 |
|---|---|---|
| `Currency Drawer` | `603.6,0 → 1316.4,1080`（712.8×1080） | `AspectRatioFitter` 高控宽 0.66 |
| `Content` | `604,0 → 1316,1080`（712×1080） | |
| `Content/Image` | `604,0 → 1316,864` | 主图槽（`×0.9` ⇒ 视觉 640.8×777.6） |
| `Content/Label` | `639.6,810 → 1280.4,918`（640.8×108） | 名条底 `40k_main_bt_nametag` ＋ `HorizontalLayoutGroup`（spacing 10 · pad 25,25,0,0） |
| **`Content/Label/Icon`** | **`901,810 → 1009,918`（108×108）** | **= 小图标槽**（`preserveAspect`；出厂 sprite `40k_topmarquee_currency_gold`） |
| `Content/Label/Quantity` | `1019,810 → …（宽 0，由布局给）` | 数量（fs 100 / 基准 36 / auto 12–100 · Left/Midline） |

- 🔴 **为什么本轮不把它画出来**：小图标**只**活在 `Content/Label` 这条名条行里，而我们的 `Icon` 抽屉是
  **居中方块主图 + 数量**（`Square(box, IconFill=0.7)`），**没有名条行**。往那个版面上硬塞一颗小图标 =
  **编一个原版没有的排布**（铁律 3 明令：参数不能自己挑）。要真接，正确做法是**照上表实现一整个
  `CurrencyDrawer`**（背景 `40k_Rewards_icon_base` + 主图 0.9 + 名条行 + 小图标 + 数量），
  那是**一件独立活**（约 60 行），不是「接一个槽」⇒ 本轮**如实留作下一步**，几何已备齐（上表可直接用）。
- ⛔ 也没有「填不出」的字段需要出声 —— 位置/尺寸全读到了，只是**版面前置**还没建。

---

## §4 本轮**真正接上**的那一格：`stackable`（4 个数量口）

| 文件:行 | 改了什么 |
|---|---|
| `Shell/ItemDrawer.cs:1250 / 1307 / 1322 / 1820` | `if (st.QuantityPx > 0f)` ⇒ `if (st.QuantityPx > 0f && ItemDrawerOptions.Stackable(item))`（`Wildcard` / `Icon` / `Placeholder` / `Deck` 四个口；**同一行就地改**，行数不变） |
| `Shell/ItemDrawerOptions.cs`（新建 145 行） | 20 条表 + `TypeOf` / `Stackable` / `ShowName` / `TypeString` / `TryGet` / `Count` |

**类型怎么求的（⛔ 没有按前缀推）**：① `ItemSpec.Type`（`IdTypes` 实据表已填的）② 货币 code `C2/C3/C5`
（三份 `Currency` SO 的 `code` 实读）⇒ `Currency` ③ `premium_campaign_*` ⇒ `PremiumItem`，
**走 `CampaignData.PremiumAll` 那 13 条实读表**（单一来源，⛔ 不在这里再抄一份 id 清单）④ 其余 ⇒ `null`。

**可观察差异（这是本批唯一的画面变化）**：
- `PremiumItem` / `VIPPremiumItem` / `RawCardScript` 三档**不再画数量**（原先会印 `1`）。
  例：战役页那 13 张 `premium_campaign_*` 的格子上那颗「1」现在不画了 —— 与 prefab `quantity: {fileID: 0}`
  **和** `stackable = 0` 两源一致。
- 其余 17 档 `stackable = 1`，**行为一字不变**；判据空（类型求不出）时也仍是 `true` ⇒ 改前行为。

**观测口 / 改坏法**：把 `&& ItemDrawerOptions.Stackable(item)` 去掉 ⇒ 那三档的数量又冒出来。
🧨 自检建议（`Editor/RewardsScene.cs` 或 `ShopScene` 的写手做，本代理**没加**断言）：拿一件 `premium_campaign_*`
的抽屉，数 `node.Quantity` 那颗节点是否存在 / 文本是否为空；再拿一件 `Wildcard` 的作正例（必须有数量）。

---

## §5 读数

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_B89E17c bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0** ✅（这次全绿；上一轮那两条 `RuleEngine/Core/TutorialScript.cs` 已由别人修掉） |
| `check_ledger_points.py 商店` | A 124 · 通过 124 · **漂移 0** · 告警 1 · 失败 0（改前改后同数） |
| 行数守恒 | `Shell/ItemDrawer.cs` 仍 **2152 行**（4 处都是同行改写）· `ItemDrawerOptions.cs` 145 行（新）· `CurrencyNames.cs` 89 行 |
| 行尾 | 全部 **LF**、`bareCR = 0`（二进制核） |

---

## §6 顺手发现（只报）

1. 🔴 **「有值」≠「会显示」**：`options` 那两格只是**半个闸**，另一半是 prefab 上的承载件（`label` / `quantity` 是否 `{fileID: 0}`）。
   凡以后要判「原版这一格画不画」，**必须两样一起读** —— 只看 `options` 会得出「Currency 有名字」这个错结论。
2. 🔴 **`Icon Currency Drawer Variant` 的 `quantity` 也是 null** ⇒ 商店里那一档**连数量都不画**（只有主图）。
   我们的商店格走的是 `ShopOffer.Art` + `MenuDraw.Rect` 这条路、不是抽屉，所以今天没有偏差；但**接手的人**若把商店格切到抽屉体系，
   这一档要按「无数量」实现。
3. 🟡 **`Premium Drawer` 是全表唯一「prefab 有 label、靠 `showName=0` 才藏住名字」的案例** ⇒ 它是这条链的**判别样本**：
   谁要验证 `showName` 那道闸，用它最灵（改 `showName` 就能看出画不画）。
4. 🟡 **本轮没加断言**（`Editor/*Check.cs`、`Editor/{ShellScene,MenuCheck,…}.cs` 都在别人名下；`Editor/ShopScene.cs` 虽是我的，
   但 §4 的变化面在**战役页/奖励窗**，放 ShopScene 里断不合适）⇒ 建议 `RewardsScene` 的写手补 §4 末尾那两条。

---

## §7 还差（按下一个动作写）

1. **`CurrencyDrawer` 整件**（= 58 的小图标槽 + 名条行）：几何已备齐（§3 那张表），动作 = 新建一个抽屉实现
   （`Background 40k_Rewards_icon_base` · 主图 0.9 · `Content/Label` 名条行 · 小图标 108² · 数量 fs100/auto12–100）
   ＋ 在 `DrawerImpls` 里把 `CurrencyDrawer` 从「没实现」改成它。⚠️ 接之前要确认 `ItemDrawerConfig` 的
   `DrawerImpls` 那条注释（它现在写「只覆盖野牌那两档」）不会因此要连着改。
2. **`165` 若哪天真要画名字**：需要先有 label 槽（同 1 的前置），且 `showName` 要**同时**为真 —— 而原版 `PremiumItem` 是 0
   ⇒ 除非原版数据改了，否则永远不画。
3. **断言**：见 §6·4。
