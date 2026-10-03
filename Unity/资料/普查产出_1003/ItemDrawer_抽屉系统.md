# `ItemDrawer` 抽屉系统 —— 普查（2026-10-03）

> **为什么有这一份**：商店格、战役节点奖励、日常奖励窗、**商品条目族那 19 个变体**都要用它，
> 而它在本地是**一套跨 prefab 的运行时装配**（不是写死在某个 prefab 上）。2026-10-03 一次查清，落成这份。
> **命令**：`cat d:/2/tools/decomp_full/ItemDrawer__*.c` · `grep -rn ": ItemDrawer<" d:/2/Warpforge_code/Scripts/Assembly-CSharp/`
> · `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<名字>" --depth 6 --relative`

---

## 一、怎么选抽屉（反编译逐步读出来的）

```
ItemDrawer.Draw(parent, item, quantity = 1, drawerOverride = Default)
 ① GetDrawerConfig(item, drawerOverride)
      · 静态缓存 + AssetLocator.GetUniqueAsset<ItemDrawerConfig>(DAT_1842b70c8)   // 唯一资产、无路径字面量
      · type = item.GetType()
      · 第一轮：FirstOrDefault(cfg+0x18 /*List<ItemDrawerReference>*/, x => x.TypeReference.Type == type)   // **精确类型相等**
      · 第二轮兜底：x => ReflectionHelper.Is(type, x.TypeReference.Type)                                     // **is-a / 派生匹配**
      · 命中后取抽屉：override == Default ⇒ +0x18 drawerReference
                      override != Default ⇒ List.Find(+0x20 customDrawerOverrides, pair.Key == override)，**找不到回落 +0x18**
 ② drawer == null ⇒ 返回 null（**不画**）
 ③ Instantiate(prefab, parent)；把 drawerOverride 写进对象 **+0x20**
 ④ 调虚表 **+0x188** = `Draw(item, quantity, options)`
```
- `ItemDrawerReference` 字段偏移：`TypeReference@0x10 / drawerReference@0x18 / customDrawerOverrides@0x20 / options@0x28`。
- **`DrawerOverride` 取值**（`Assembly-CSharp/DrawerOverride.cs`）：
  `Default = 0 · Icon = 10 · Horizontal = 15 · **Shop = 20** · OfferPopups = 30`。
- 🔴 **查不到**：`ItemDrawerConfig` 的 **SO 实例没有解包** —— `grep -rl customDrawerOverrides assets_full` **0 命中**，
  `assets_full/globalgamemanagers/MonoBehaviour/` 是**空目录**（只有 `MonoScript_2956.json` = 类名）。
  ⇒ **「类型 → prefab」那张映射表本地读不出来**。

## 二、23 个具体抽屉（`ItemDrawer<T>` 的子类，全列）

`AllianceBadgeDrawer<AllianceTrophyData>` · `AvatarBorderDrawer<CosmeticItemAvatarBorder>` · `AvatarDrawer<PlayerAvatar>` ·
`AvatarDisplay<PlayerAvatar>` · `CampaignPointDrawer<CampaignPoints>` · `CardAlternateArtDrawer<AlternateArtCard>` ·
`CardDrawer<RawCardScript>` · `CardbackDrawer<CosmeticItemCardback>` · `ContainerDrawer<ShopContainerBase>` ·
`CosmeticDrawer<CosmeticItem>` · `CurrencyDrawer<Currency>` · `DeckDrawer<PrebuiltDeck>` ·
`ExpansionPassPremiumDrawer<ExpansionPremiumItem>` · `ExpansionPassPointDrawer<ExpansionPassPoints>` ·
`ForgePointDrawer<ForgePoints>` · `ForgePointIconDrawer<ForgePoints>` · `GenericArmyItemDrawer<GenericArmyItem>` ·
`PremiumDrawer<PremiumItem>` · `RandomCardDrawer<DropTableItem>` · `RandomCardIconDrawer<DropTableItem>` ·
`TitleDrawer<CosmeticItemTitle>` · `TitleIconDrawer<CosmeticItemTitle>` · `XSollaOfferDrawer<XSollaBundleItem>` ·
`WildcardDrawer<Wildcard>` · `WildcardIconDrawer<Wildcard>`。

- **`ObtainableItemTypeToDraw` 没有任何子类覆写** —— 唯一实现 = 泛型基类 `ItemDrawer.cs:112` 的 `=> typeof(T)`
  ⇒ **它的值就是上表尖括号里那个类型**。
- ⚠️ `ItemDrawer<T>` 泛型类本体（sealed `Draw`/`Refresh`/`Toggle*`）**在 `decomp_full/` 里没有**；
  字段偏移靠子类反推：`+0x20 drawerOverride / +0x28 options / +0x30 components / +0x38 item / +0x44 showItemInfoOnClick`。

## 三、6 个抽屉 prefab（**不是同一份**）

| prefab | 根尺寸 / 布局 | 特征 |
|---|---|---|
| `Item Drawer`（RT `-3096826980488349134`） | 1044.65×220 · `GridLayoutGroup cell 180² spacing 25/50 pad 13,0,40,0` | 子 `Avatar Item Small_Ref` 180²；孙 `Highlight`=`Player_Avatar_selected` 256² scl1.9 · `Border`=`Player Profile Border` 256×286 scl1.25 · `Avatar Name` fs36 |
| `Item Drawer_-436770581587566429` | 1554.03×377 · `cell 298×354 spacing 0/15` | 子 `TrophyDisplay` 298×354 → `BadgeDrawer` 230.05²（挂 `ItemDrawerComponents`+`AllianceBadgeDrawer`）· `title` fs38 · `Progress/ProgressBar` |
| `Item Drawer_4972767040676198962` | 1044.65×**0** · `cell 325.9×130` | **0 子节点**（空模板） |
| `Item Drawer_8971722438566583161` | 同 #1 的 cell/spacing/子节点名 | **同形不同实例**（对象与 target pid 都不同），布局未跑（子 rect 全 0） |
| `Daily Reward Popup Item Drawer` | 327.42×423.13 scl0.8 | `BG`=`UI_Deck_Information_submenu_Back_opaque` 九宫18 · `Highlight`=`OctagonUI Border SDF 2` 九宫53 · `Reward Holder/Icon Container Drawer Variant` · `EverguildTextMeshPro` fs45 · `Premium Indicator`=`WF_Login_CornerBanner`+`WF Lock Icon Simple` · `Gacha Reward Claimed`=`WF_Special offer_Value` |
| `Generic Army Item Drawer` | 1920×1080 · `AspectRatioFitter` + `ItemDrawerComponents` | 内容近乎空（真实抽屉是运行时由 `ItemDrawer.Draw(holder, …)` 内嵌的） |

🔴 **那 4 个 `Item Drawer` 实为 `GridLayoutGroup` 容器**（根上**没有** `ItemDrawerComponents`），**不是抽屉本体**。
⚠️ 它们**同名** ⇒ 用 `menu_dump.py` 要**用 `--rt <RT pid>` 直取**，别按名字找。

## 四、它们画什么（判据出处）

- **`WildcardIconDrawer.Draw`**（`decomp_full/WildcardIconDrawer__Draw.c:17-30`）：
  `sprite = iconsByRarity[item.cardRarity - 1]`（`Count <= i` 时 `FirstOrDefault`）。
  **无染色 · 无 `_small` 拼接 · 不看 `cardArmy`**。
- **`WildcardDrawer.Draw`**（`WildcardDrawer__Draw.c:19-41`）：
  `background.sprite = wildcardBackgrounds[cardRarity - 1]` ·
  `image.sprite = ArmyUtilities.GetArmyIcon(item.cardArmy)` · `armyText.SetText(GameStaticData.CardArmyToString(cardArmy))`
  ⇒ **卡面靠 `cardArmy`、背景靠 `cardRarity`**。
- **`WildcardDrawer.GetItemName`**（`WildcardDrawer__GetItemName.c:18-30`）：`string.Format(loc, CardRarityToString(cardRarity))`，
  词条 key = **`MenuShop/RarityWildcard`**（`MonoBehaviour_-3179028736579401299.json` 的 `mTerm`）。
- **实际贴图清单**（pid→名，`_tmp_view/sprite_pids_ALL.json` 实读）：
  `iconsByRarity` **与** `wildcardBackgrounds` 是**同一组 4 张** ——
  **`40k_general_wildcard_common / _rare / _epic / _legendary`（无 `_small` 后缀）**，顺序 = rarity 1..4。
  ⚠️ `WildcardDrawer.cardIcon`(0x80) = `40k_general_wildcard_legendary_small`，**`Draw`/`GetItemName` 都没用它**。
- **`GenericArmyItemDrawer.Draw`**（`GenericArmyItemDrawer__Draw.c:25-64`）：字段 `holder@0x58`、`drawer@0x60`；
  已缓存的 drawer 同名同类 ⇒ 复用；否则 `Destroy` 后 `ItemDrawer.Draw(holder, GenericArmyItem.GetItem(item), quantity, drawerOverride)`，
  再 `ToggleShowInfoOnClick(showItemInfoOnClick)`。

🔴 **纠正一条曾经的假设**：**不存在「wildcard ⇒ 按 `cardArmy` 给 `40k_general_wildcard_{rarity}_small` 染色」这条规则。**
`_small` 那套图只挂在 `cardIcon` 字段上且**代码未使用**。

## 五、与商店格的关系（2026-10-03 反编译查实）

`decomp_full/CatalogItemContainer__OnInitialize.c`：
```
SupportMethods.DestroyAllChildren(this.drawerHolder);          // +0x48 = drawerHolder
var item = (Item != null && Item.GetType() == <某类型>) ? Item : null;   // il2cpp 类型判等
this.drawer = ItemDrawer.Draw(this.drawerHolder, item, 1, DrawerOverride.Shop(0x14), 0);
```
⇒ **商店格的图是运行时由 `ItemDrawer` 铺进 `DrawerHolder` 的**，覆盖档 = **`Shop = 20`**。
⚠️ 那个类型判等指向的类**没解出来**（il2cpp 的 `Type` 指针，不是字面量）⇒ **「哪些 item 才走抽屉」读不出来**。
⇒ 我们格子里那张图是**我们自己填的 `ShopOffer.Art`**（正本 §五·一 末尾也记着这条）。

## 六、对我们的用处 / 还缺什么

- **能做**：按第 2 节的类型表 + 第 4 节的画法，**自建一棵「抽屉库」**（`WildcardDrawer` / `CardbackDrawer` /
  `IconCurrencyDrawer` …），喂给 `CampaignTab` 的节点奖励、`DailyRewardPopup`、以及 §五·一 那 19 个变体。
- **缺**：① **类型 → prefab 的映射表**（`ItemDrawerConfig` SO 没解包）；
  ② **「什么时候挂哪个抽屉」** —— 🔴 **2026-10-04 更正（审查代理查出）：原来这里写「`ShopOfferContainer.GeneralOfferPopupDrawer.DrawRewards` **方法体是空 stub**」，是假的** ——
  那是个 **14,920 字节的完整方法体**（含三个 LINQ lambda + `Enumerable.First` + `List.RemoveAt` + `ComponentReference.Release`；**空 stub 不会有 lambda**；
  对照：真小的 `ItemDrawer__Draw.c` 才 1,659 字节）。它就在本族 19 个 prefab 的**根组件列**上 ⇒ **相关**。
  ⇒ **正确记法**：「**目前【没解出来】，不是「读不到」**」—— 要解它得读 `DAT_` 常量（`资料/战斗规则与数值_出处.md` §三 那条路）+ 逐 call 追，**是一件待做的活**（→ `项目任务.md` §三 **A34-F6**）；
  ③ 抽屉美术**全是运行期赋值**（`Content→Image` 一律无图）⇒ 只能从**固定件**反推外观
  （`40k_main_bt_nametag` · `40k_Profile_display_title` · `40K_general_icon_lock` · `40k_campaign_Premium-icon` · `Player Profile Border`）。
⇒ **要建就建「抽屉库 + 19 个骨架」，但「谁用哪个」只能我们定并如实标**（`项目任务.md` §三 第 29 条 A8/A12）。
