# C · 收藏 / 卡组编辑 / 商店 —— 补 C 第 1 轮

> 2026-10-25 · 执行代理 C。范围 = 我名下三本账的 **C**（收藏 80 · 卡组编辑 13 · 商店 54），按调度台口径：**活动线那部分只记账不动工**，其余要做。
> `TMPDIR=/tmp/wf_c bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（本轮有代码改动）。⛔ 未跑 Unity / 未动 git / 未跑 `gen_replica_ledger.py`。

---

## ① 数字

| 账 | 本轮 C→A | C→D（裁定） | 仍 C | 变化后 |
|---|---:|---:|---:|---|
| `收藏.tsv` | 0（见下：搜索那条**本来就是 A**，本轮把 ④⑤ 真正接通） | 0 | 80 | A113 / B59 / **C80** / D99 |
| `卡组编辑.tsv` | **1**（空位行） | **4**（`CardDeck` 载体不同） | 13 | A**177** / B**55** / C13 / D**78** |
| `商店.tsv` | 0 | **4**（货币/价格载体不同） | 54 | A114 / B**5** / C54 / D**169** |

三本账仍 **11 列 / E = 0 / LF 未翻**（`351 / 323 / 342` 行，列数全 11）。

---

## ② 已落地的裁定（调度台两条）

1. **`CarouselOfferPopupDrawer` ×4 = C（要做）**，等级 **P2**，`备注` 写死「**登记为待做**（轮播是真实交互、不是表现细节）」。
2. **「载体不同」8 条 = D**，`备注` 按裁定写死理由：
   - 货币/价格 4 条（`CurrencyValue.GetCurrencyIcon` / `HasEnoughCurrency` / `ToString` · `Price.ToCurrencyAmount`）：
     「用户 2026-09-17 拍板**不做真实经济**，资源固定 2000（2026-10-24）」；
   - `CardDeck.GetID` / `Equals` / `GetHashCode` / `CosmeticsSetup`：
     「**替代物 = Name + 有序列表**（本库按下标寻址、`DeckStore` 存有序列表，同名不会选错）」。
   ⛔ 两条都**没有**写成「影响小」。

---

## ③ 本轮真正做完的 C（代码改动）

### ③·1 卡池搜索的 **④兵种本地化名 / ⑤资料片** —— 收尾（`Deck/DeckEditorState.cs`）

上一轮我记的「卡在别人文件上」两条，**上游都补齐了**，本轮接通：
- ④ 兵种：代理 E 已把卡面那一条**同款**显示名暴露成 `CardView.RaceDisplayName(subtype)`（`Core/CardView.cs:5325`）⇒ `NameSearchMatch` 现在 `Hit(c.Subtype) || Hit(CardView.RaceDisplayName(c.Subtype))`（**3 步口径只此一处**，⛔ 没抄第二份）。
- ⑤ 资料片：代理 A 已给 `CardDef` 加 `Expansion` + **`ExpansionSearchName`**（`RuleEngine/Core/CardDef.cs:162,183-…`，就是原版枚举成员名的小写；`<= 0` 返回 `null` ⇒ 正好是原版「`!= Default` 才比」那一档）⇒ `Hit(c.ExpansionSearchName, needle)`。
- 至此 `CardNameFilter.CheckInputTextSearch` 的**六项全部落地**：①卡名 ②效果文字（先剥 `<…>`）③阵营名 ④兵种本地化名 ⑤资料片 ⑥稀有度；另加口径 1 的**结果反序**。
- 账：`收藏.tsv` 的 `CardNameFilter.CheckInputTextSearch` 那行**本来就是 A**（第一轮判的），本轮把 `差异` 改准（写清六项全落地 + 我们多查的两路），备注标「2026-10-25 ④⑤ 已接通」。

**新增断言（`Editor/DeckScene.cs`，`待自检恢复时跑`）**：两条**超集式**断言 ——
① 搜 `leviathandrop1` ⇒ **所有 `Expansion == 10` 的卡必须全部在结果里**（挡「⑤ 断了」）；
② 搜 `psychic power` ⇒ **所有 `Subtype == "Psychic Power"` 的卡必须全部在结果里**（挡「④ 断了」）。
⚠️ **故意写成超集而不是"结果恰等于该档"**：原版六项任一命中即保留（描述/卡名里出现同样的词也会命中）⇒ 等值断言会假红。
🧨 改坏法：把 `Hit(CardView.RaceDisplayName(...))` 或 `Hit(c.ExpansionSearchName)` 任一删除 ⇒ 对应那条红。

### ③·2 卡组列表的空位行 —— C → **A**（`Deck/DeckRuntime.cs`，上一轮实现）

判据 `DeckListDrawer.cs:1061-1080`：没督军 / 没防御卡 ⇒ 那一槽**留空行**（底板照画、名字空、不印费用/份数）。
上一轮已实现（`DeckRows()` + `RefreshDeckList` 的 `emptySlot` 支 + 4 处消费点守卫）；账里当时仍是 C，**本轮改成 A** 并把落点精确到符号。

---

## ④ 仍需别人做的（逐条给出可照抄的改动）

### ④·1 `DeckUtility` 长/短两档错误文案 → **代理 A + `SkirmishEventWindow` 的 owner**

**判据（已逐句读）**：
- `DeckUtility.cs:86,126` 短档 = `MenuDeck/Error/{枚举}/Short`；官方表里**只有两条短档**：
  `InvalidDeck/Short` = `Invalid Deck` · `InvalidDeckBannedCards/Short` = `The deck has banned cards`（其余 13 条无 `/Short`）。
- 唯一外部调用点：**`SkirmishEventWindow.cs:505`** —— `bannedCardInDeckText = DeckUtility.ToShortLocalizedString(deckError)`
  ⇒ **遭遇事件窗左列**「这副卡组为什么不可用」那一行**用的是短档**（空间窄）；我们那一行用的是**长档**。

**改动（两处，都不在我名下）**：
1. `RuleEngine/Core/DeckRules.cs` 加：
   ```csharp
   /// <summary>短档错误文案的**词条键**（原版 `DeckUtility.ToShortLocalizedString` / `…ShortRawLocalizationString`）。
   /// ⚠️ 官方表只有 `InvalidDeck/Short` 与 `InvalidDeckBannedCards/Short` 两条 ⇒ 其余枚举**没有短档**，
   /// 照原版 `TermData == null` 那一支**回长档**（⛔ 别拼出缺键、把键名印上屏）。</summary>
   public static string DescribeShort(DeckError e)
   {
       string k = Describe(e);                      // 已经是 `MenuDeck/Error/{枚举}`（含 EffectOnlyCard → InvalidDeck 的兜底）
       string s = k + "/Short";
       return (Loc.HasEntry(s) || Loc.HasBulk(s)) ? s : k;
   }
   ```
2. `Shell/SkirmishEventWindow.cs:505` 那一行改成 `DeckRules.DescribeShort(deckError)`（该文件归 **H/外壳**，请转）。

### ④·2 轮播式奖励抽屉（`CarouselOfferPopupDrawer` ×4，P2，**登记为待做**）

**判据（已读）**：
- 类 `CarouselOfferPopupDrawer : GeneralOfferPopupDrawerBase`，字段 `UI_InfiniteScroll scroll` + `Transform contentAnchor`。
- `Initialize` = `base.Initialize(offer)` + **`scroll.ManualInit()`**（无限循环滚动初始化）。
- `DrawRewards` = 先 `Where(<过滤：物品类那一个虚调用>)`，再逐条 `DrawItemReward`。
- `DrawItemReward` = `ItemDrawer.Draw(contentAnchor, item, qty)` → 收进 `currentItemDrawers` → **`ToggleShowInfoOnClick(false)`**。
- `DoReset` = `ReleaseAssets()` + 销毁全部 drawer。

**为什么我这一轮没动它（如实说，不是拖延）**：我们的报价弹窗是**固定槽池**（`OfferContainer.PoolCount/SlotNameAt/PickSlot`，照原版 `GetComponentsInChildren` 的「池里找同类槽」口径），
**没有横向无限滚动这一层**；要补得先有「横向循环滚动 + 复用 + 输入（拖/滚轮）」三件，**且我当前跑不了自检/真机**（自检暂停）⇒
**写进去就是一段没人验过的 UI 系统**，风险高于收益。⇒ 已按裁定**登记为待做（P2）**，建议单独一批做，并配一次真机验收。

### ④·3 其余 C 的分族（**都不在我能改的文件里**或**属活动线暂停**）

**收藏 80 C**：`Inventory` 族 **51**（G §三 #3 的框架层，未做）· 红点/通知 **18**（`UiBadgeNotification*`/`BadgeNotificationDefinition`，**域外 = 菜单外壳线**）·
拖拽排序 **9**（`CollectionItem` 7 + `CollectionDisplay` 2，卡在 A 的 `DeckLibrary.UpdateOrder` + 共用件 `PointerLayer`/`MenuDraw`）·
`CollectionCard.OnRectTransformDimensionsChange` **1**（自适应缩放；⚠️ 照原版公式会把卡底压住张数条 = 用户报过的 `W11` 缺陷 ⇒ 要改得先定一条口径）·
`WildcardDisplay.OnEnable` **1**（徽记「闪一下」；缺 `shinyEffect` 那颗件的 prefab 参数）。

**卡组编辑 13 C**：费用曲线 **5**（E 的 `Core/CostCurveDrawer.cs`，请求早已转出）· 拖拽 **5**（A + 共用件）· 复制后缀 **1**（A 的 `DeckLibrary`）· 短档 **2**（见 ④·1）。

**商店 54 C**：**活动线 36**（`Gacha*` 26 + `CampaignPremiumPointsPanel` 5 + `ExpansionPassPurchasePremiumPanel` 5 —— **只记账不动工**）·
轮播 **4**（见 ④·2）· LiveOps 容器数据层 **5**（`ContainerOfferEvent` 2 + `ShopContainer` 2 + `V2` 1）· `Inventory` 族 **9**（`CurrencyExtensions` 4 + `ShopContainerInventory` 3 + `ShopOfferInventory` 2）。

---

## ⑤ 没查清

1. **`CarouselOfferPopupDrawer.DrawRewards` 那个 `Where` 谓词**：解出来是一次 `ObtainableItem` 上的虚调用（`+0x1C8`/`+0x1D0` 两个槽），**没追出到底过滤什么**（像「排除容器类物品」）⇒ 真要做轮播时必须先补这一条（否则会多画/少画奖励）。
2. **短档文案的「其余枚举怎么办」**：官方只有两条 `/Short`；原版 `TermData == null` 那一支我没读到（`ToShortRawLocalizationString` 里 `if (termData == null)` 之后的分支被反编译成了 `local` 赋值，看不到返回什么）⇒ 我按「回长档」写进请求，**这条是推断、要 A 落地时用 `decomp_full` 复核一次**。
3. **`CollectionCard.OnRectTransformDimensionsChange` 照抄会不会压住张数条**：原版公式 `scale = sizeDelta.y * 150 / 512` 折成我们的是 1.0417（比现用值大 8.4%）⇒ 卡块高 374.83 > 格子可用高 345.728，**会压住张数条**（用户 2026-10-18 报的 `W11`）。这跟「原版实拍量出来卡宽 ≈ 格的 81.7%」矛盾 ⇒ **原版的 `sizeDelta.y` 到底等于格高还是别的**没查清（可能要按 cell 的 350×512 原型算）。⇒ 这条 C 要一条口径才能动。

---

## ⑥ 顺手

1. **`CardDef.Expansion` 的默认值是 `-1`（未知）而不是 `0`**，而 `ExpansionSearchName` 对 `<= 0` 都返回 `null` ⇒ 「未知」与「Default」在搜索上行为相同、但语义不同（A 的注释已写）。⛔ 将来别把 `-1` 当 `Default` 用。
2. **`CardView.RaceDisplayName` 是 `CardView` 上的 `public static`**（`Core/CardView.cs:5325`）—— 以后任何「按屏上印的兵种名搜」的调用点都应该转它，⛔ 别再抄 `RaceKeyBySubtype` 那张表。
3. **官方表里 `MenuDeck/Error/*` 共 15 条**，其中**只有 2 条有 `/Short`**（`InvalidDeck` · `InvalidDeckBannedCards`）—— 所以「短档」不是普遍能力，是**这两条的特例**。⛔ 别照「每条都加 Short」的思路补文案。
