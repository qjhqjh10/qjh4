# `ItemDrawerConfig` 映射表（原版 `ObtainableItem` 子类型 → `ItemDrawer` prefab）

> 查证代理 I · 2026-10-04 · **纯离线原始字节**解出来的，脚本可复现：
> `d:/4/Unity/工具/read_itemdrawerconfig.py`（跑一次打整张表，7 条自检全绿，退出码 0）

---

## ① 结论

**解得出来，一条不缺：20 个 `ObtainableItem` 子类型 + 19 档 `customDrawerOverrides`（共 **39 行**；
去重后是 **36 个不同 GUID**，因为有 3 个 prefab 被两档共用）—— 39 行**全部**追到 GameObject 名字，
36/36 个 GUID 都验到了「prefab 身上的抽屉类」。零条靠猜。**

三条互相独立的证据链，全部闭合：

| # | 链路 | 判据出处 |
|---|---|---|
| 1 | SO 字节 → `drawerReference.m_AssetGUID` | `sharedassets0.assets` MonoBehaviour **pathID 453**，数据起点 `0x7FABE0`，长 **3932** 字节 |
| 2 | GUID → GameObject 名 | `assets_full/bundle_menus_assets_all/AssetBundle/AssetBundle_1.json` 的 `m_Container`（36/36 全命中，36 个名字互不重复） |
| 3 | prefab → 抽屉类 | 组件 `m_Script` → `bundle_Waprforge_monoscripts/MonoScript/MonoScript_<pathID>.json` 的 `m_ClassName`（36/36） |

**最强的一条自证**：按签名桩的字段顺序切字节，**正好吃掉整个对象**（终点 `0x7FBB3C` = 对象末尾 `0x7FBB3C`，
一个字节不多不少）。多一个字段少一个字段都会错位，所以这条等于把「字段顺序猜错了」这条路堵死了。

**另有一条对照**：`D:/2/Unity参照管线_0825/data/guid_map.tsv`（**别人早先建的**另一份 GUID→包 表）
对我们抽查的 GUID 给出的同样是 `menus_assets_all.bundle` —— 第 2 条链路的独立第三方印证。

---

## ② 表本身

**偏移写法**：`0x7FAC60` 这种 = **`sharedassets0.assets` 文件里的绝对字节偏移**，指向该 GUID 字符串的
**长度字段**（4 字节），字符串数据紧跟其后。整条记录的起点（`TypeReference` 的开头）在最后一列。

| `ObtainableItem` 子类型 | override | prefab（GameObject `m_Name`） | prefab 上的抽屉类 | GUID | 字节偏移(GUID) |
|---|---|---|---|---|---|
| `Currency` | `Default`(0) | `Currency Drawer` | `CurrencyDrawer` | `743e84546bcb066419596158e869afb0` | `0x7FAC60` |
| ↑ | `Icon`(10) | `Icon Currency Drawer Variant` | `CurrencyDrawer` | `aa7107496ba702742835d372df1a4088` | `0x7FAC94` |
| `PlayerAvatar` | `Default`(0) | `Avatar Drawer` | `AvatarDrawer` | `0ba983e75042cb04693b868e41374e9c` | `0x7FAD3C` |
| ↑ | `Icon`(10) | `Icon Avatar Drawer Variant` | `AvatarDrawer` | `9d32a6b8d9c256245a93fb9c03157b4b` | `0x7FAD70` |
| ↑ | `Shop`(20) | `Avatar Drawer Shop Variant` | `AvatarDrawer` | `ea2d69cf21262a24e9149d6518e87aa2` | `0x7FADA0` |
| `Everguild.LiveOps.ShopContainer` | `Default`(0) | `Container Drawer` | `ContainerDrawer` | `bd0792f52fcebcd45bab692fe4f02ae7` | `0x7FAE4C` |
| ↑ | `Icon`(10) | `Icon Container Drawer Variant` | `ContainerDrawer` | `6fd0524b14f113841bcb6218816255e4` | `0x7FAE80` |
| `RawCardScript` | `Default`(0) | `Card Drawer` | `CardDrawer` | `980c61d8282966b499f993399bed4fdc` | `0x7FAF08` |
| `DropTableItem` | `Default`(0) | `Random Card Drawer` | `RandomCardDrawer` | `01f12bb7c3f45e145be214a7a2b3d074` | `0x7FAF94` |
| ↑ | `Icon`(10) | `Icon Random Card Drawer Variant` | `RandomCardDrawer` | `1da17b000473aeb47a8d8eafac1f361b` | `0x7FAFC8` |
| `CosmeticItemCardback` | `Default`(0) | `Cardback Drawer` | `CardbackDrawer` | `df3256157beb8dd438de057c5b8200df` | `0x7FB058` |
| `Wildcard` | `Default`(0) | `Wildcard Drawer` | `WildcardDrawer` | `61a84679e87a2eb4ca1241db53be5241` | `0x7FB0FC` |
| ↑ | `Icon`(10) | `Icon Wildcard Drawer Variant` | `WildcardIconDrawer` | `c32e1be9a938c274daba03640d06f68e` | `0x7FB130` |
| `CampaignPoints` | `Default`(0) | `Campaign Points Drawer` | `CampaignPointDrawer` | `34d0e048114cfb247be342d2753c406a` | `0x7FB1B8` |
| ↑ | `Icon`(10) | `Icon Campaign Points Drawer Variant` | `CampaignPointDrawer` | `ee0d672dd8bf7594d9aeb1851470d200` | `0x7FB1EC` |
| `ForgePoints` | `Default`(0) | `Forge Points Drawer` | `ForgePointDrawer` | `512d2c70fc1d45e47a70e3f479705fe3` | `0x7FB270` |
| ↑ | `Icon`(10) | `Icon Forge Points Drawer Variant` | `ForgePointIconDrawer` | `7b39e648bc9cb6a43a253a55a57642d9` | `0x7FB2A4` |
| `Everguild.LiveOps.ExpansionPassPoints` | `Default`(0) | `Expansion Pass Points Drawer` | `ExpansionPassPointDrawer` | `d0399533d92d1934a8f9a7d089cdc095` | `0x7FB344` |
| ↑ | `Icon`(10) | `Icon Expansion Pass Points Drawer Variant` | `ExpansionPassPointDrawer` | `a9b8a9ef759954b0b8a59ecbccb859ad` | `0x7FB378` |
| `PrebuiltDeck` | `Default`(0) | `Deck Drawer` | `DeckDrawer` | `7ea5632eb059b5b4493c5047a53e1706` | `0x7FB400` |
| `CosmeticItemTitle` | `Default`(0) | `Title Drawer` | `TitleDrawer` | `654bab2d0e6e84bef8331cfb3dad081b` | `0x7FB490` |
| ↑ | `Icon`(10) | `Icon Title Drawer Variant` | `TitleIconDrawer` | `922e696b87b4ae54e9ecb7d11226d842` | `0x7FB4C4` |
| ↑ | `Horizontal`(15) | `Title Drawer Horizontal Variant` | `TitleDrawerHorizontal` | `b9799d0d3bc9161499d7d546a31646f9` | `0x7FB4F4` |
| ↑ | `OfferPopups`(30) | `Title Drawer Horizontal Variant` | `TitleDrawerHorizontal` | `b9799d0d3bc9161499d7d546a31646f9` | `0x7FB524` |
| `PremiumItem` | `Default`(0) | `Premium Drawer` | `PremiumDrawer` | `3a7daa95b4b743c49b1c911cef4c6c40` | `0x7FB5BC` |
| ↑ | `Icon`(10) | `Icon Premium Campaign Drawer Variant` | `PremiumDrawer` | `2909be62b490143d882d3b6053535e03` | `0x7FB5F0` |
| `VIPPremiumItem` | `Default`(0) | `Premium Drawer No Glow` | `PremiumDrawer` | `f58d8e003f579584c9eb92bbd7bd62db` | `0x7FB678` |
| ↑ | `Icon`(10) | `Icon VIP Premium Campaign Drawer Variant` | `PremiumIconDrawer` | `9d7339e9c7391504391a55cea2849583` | `0x7FB6AC` |
| `ExpansionPremiumItem` | `Default`(0) | `Expansion Pass Premium Drawer` | `ExpansionPassPremiumDrawer` | `d1f14b2699dec47c6adf7ffc3f7a1310` | `0x7FB73C` |
| ↑ | `Icon`(10) | `Icon Expansion Pass Premium Drawer Variant` | `ExpansionPassPremiumDrawer` | `ba0902825a504428abb529524d69da2f` | `0x7FB770` |
| ↑ | `OfferPopups`(30) | `Icon Expansion Pass Premium Drawer Variant` | `ExpansionPassPremiumDrawer` | `ba0902825a504428abb529524d69da2f` | `0x7FB7A0` |
| `AllianceTrophyData` | `Default`(0) | `Alliance Badge Drawer` | `AllianceBadgeDrawer` | `efbd6187c5499584cb8e19c86bfb9a57` | `0x7FB82C` |
| ↑ | `Icon`(10) | `Alliance Badge Drawer Icon` | `AllianceBadgeDrawer` | `4b455445d77014845bfd32de091e4bc0` | `0x7FB860` |
| `CosmeticItemAvatarBorder` | `Default`(0) | `Avatar Border Drawer` | `AvatarBorderDrawer` | `53146e9179e3dad489d7ded056dce3d6` | `0x7FB8F4` |
| ↑ | `OfferPopups`(30) | `Avatar Border Drawer Shop Variant` | `AvatarBorderDrawer` | `975f91257c4838d42b7aacb4bca68d82` | `0x7FB928` |
| ↑ | `Shop`(20) | `Avatar Border Drawer Shop Variant` | `AvatarBorderDrawer` | `975f91257c4838d42b7aacb4bca68d82` | `0x7FB958` |
| `XSollaBundleItem` | `Default`(0) | `XSolla Offer Drawer` | `XSollaOfferDrawer` | `4e3f8aa167d559241a455425e17289a1` | `0x7FB9E4` |
| `AlternateArtCard` | `Default`(0) | `Card Alternate Art Drawer` | `CardAlternateArtDrawer` | `1006f53bef637c44b82f3c4d7597ed39` | `0x7FBA74` |
| `GenericArmyItem` | `Default`(0) | `Generic Army Item Drawer` | `GenericArmyItemDrawer` | `40250df73d9365f47adcc1b01f259b07` | `0x7FBB00` |

> 表里的 `↑` = 这一档 override 属于上一行那个类型；`Default`(0) 那行 = 该类型的 `drawerReference`（基础档）。
> **`customDrawerOverrides` 里没写的档一律回落到基础档** —— 这是 `GetDrawer()` 的行为（判据见 ③）。

### 每个 guid 指向的**具体 prefab 资产**

| GUID | bundle | `m_PathID` | 名字 |
|---|---|---|---|
| 全部 36 个 | `menus_assets_all.bundle`（`assets_full` 里叫 `bundle_menus_assets_all`） | 见 `read_itemdrawerconfig.py --json` 的 `drawerPathId` / `pathId` | 见上表 |

### `options`（`ItemDrawerOptions`）与记录起点

`[记录 @…]` = 该 `ItemDrawerReference` 记录的**绝对起始偏移**（`TypeReference.GuidAssignmentFailed` 那 4 字节）。

| 类型 | stackable | showName | typeString | 记录起点 |
|---|---|---|---|---|
| `Currency` | 1 | 1 | `MenuShop/ShopItemType/Currency` | `0x7FAC14` |
| `PlayerAvatar` | 1 | 1 | `PlayerProfile/Avatar` | `0x7FACEC` |
| `Everguild.LiveOps.ShopContainer` | 1 | 0 | （空） | `0x7FADEC` |
| `RawCardScript` | **0** | 0 | （空） | `0x7FAEB8` |
| `DropTableItem` | 1 | 0 | （空） | `0x7FAF44` |
| `CosmeticItemCardback` | 1 | 1 | `Item_Description/Cardback` | `0x7FB000` |
| `Wildcard` | 1 | 0 | （空） | `0x7FB0B0` |
| `CampaignPoints` | 1 | 0 | （空） | `0x7FB168` |
| `ForgePoints` | 1 | 0 | （空） | `0x7FB224` |
| `Everguild.LiveOps.ExpansionPassPoints` | 1 | 0 | （空） | `0x7FB2DC` |
| `PrebuiltDeck` | 1 | 0 | （空） | `0x7FB3B0` |
| `CosmeticItemTitle` | 1 | 0 | `PlayerProfile/Title` | `0x7FB43C` |
| `PremiumItem` | **0** | 0 | （空） | `0x7FB570` |
| `VIPPremiumItem` | **0** | 0 | （空） | `0x7FB628` |
| `ExpansionPremiumItem` | 1 | 0 | （空） | `0x7FB6E4` |
| `AllianceTrophyData` | 1 | 0 | （空） | `0x7FB7D8` |
| `CosmeticItemAvatarBorder` | 1 | 0 | （空） | `0x7FB898` |
| `XSollaBundleItem` | 1 | **1** | （空） | `0x7FB990` |
| `AlternateArtCard` | 1 | 0 | （空） | `0x7FBA20` |
| `GenericArmyItem` | 1 | 0 | （空） | `0x7FBAB0` |

---

## ③ 证据

**SO 本体在哪个文件**（⚠️ 别按「grep 到名字」下结论）：

| 文件 | 命中 | 是不是本体 |
|---|---|---|
| `D:/2/unity_run_ref/Warpforge_Data/globalgamemanagers.assets` | `0x6DDC0` / `0x6DDE8` 两处 | **不是** —— 是 **MonoScript**（`m_Name` + `m_ExecutionOrder` + `m_PropertiesHash` 16 字节 + `m_ClassName` + `m_Namespace` + `m_AssemblyName`），pathID **2956** |
| `D:/2/unity_run_ref/Warpforge_Data/sharedassets0.assets` | `0x7FAC00` | **是** —— MonoBehaviour pathID **453**，`m_Script = {m_FileID 1, m_PathID 2956}` → `externals[0]` **正是** `globalgamemanagers.assets` ⇒ 链闭合 |

**MB 头的实测形状**（🔴 硬知识，与 `read_default_cardbacks.py` 那条一致）：

```text
[0]       m_GameObject = {fileID 0, pathID 0}           12 字节
[12]      m_Enabled    = 1                              4 字节（bool 1 字节 + 3 padding）
[16]      m_Script     = {fileID 1, pathID 2956}        12 字节
[28]      m_Name 长度  = 16                             4 字节
[32]      "ItemDrawerConfig"                            16 字节（已 4 对齐）
[48]  ← 首字段 `drawers` 从这里开始                      ← **头不是固定 32 字节**，是 32 + align4(len)
```

**字段顺序（判据 = 签名桩的声明顺序）**：

- `D:/2/Warpforge_code/Scripts/Assembly-CSharp/ItemDrawerConfig.cs` —— 内嵌 `ItemDrawerReference` / `KeyDrawerPair`
- `D:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/Addressables/ComponentReference.cs` → `: AssetReference`
- `D:/2/Warpforge_code/Scripts/Unity.Addressables/UnityEngine/AddressableAssets/AssetReference.cs` —— `m_AssetGUID` / `m_SubObjectName` / `m_SubObjectType`
- `D:/2/Warpforge_code/Scripts/TypeReferences/TypeReferences/TypeReference.cs` —— `GuidAssignmentFailed` / `GUID` / `_typeNameAndAssembly` / `_suppressLogs`
- `D:/2/Warpforge_code/Scripts/Assembly-CSharp/ItemDrawerOptions.cs`、`DrawerOverride.cs`

**字节规则**：`int32`/`float` 4 对齐 · `bool` 1 字节但**按 4 对齐**（写出来 4 字节）·
`string` = `int32 长度` + 字节 + **补齐到 4** · `List<T>` = `int32 条数` + 条数×T。

**GUID → prefab**：`assets_full/bundle_menus_assets_all/AssetBundle/AssetBundle_1.json` 的 `m_Container`
（**297 项，297 项的键全是 32 位 hex GUID**）→ 值 `{asset:{m_FileID:0, m_PathID:<big int64>}}` →
UnityPy 重读 `D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/menus_assets_all.bundle`
把 pathID 反解成 `GameObject.m_Name`。**36/36 命中，36 个名字互不重复、无空名。**

**prefab → 抽屉类**：组件 `m_Script` 在 MonoBehaviour raw 的**偏移 16**；pathID →
`assets_full/bundle_Waprforge_monoscripts/MonoScript/MonoScript_<pathID>.json` 的 `m_ClassName`。
⚠️ 文件名用**有符号** int64（负号形式），两种形式都要试。

**行为语义（已由调度台从反编译读出，本条只登记出处，未复核）**：
`D:/2/tools/decomp_full/ItemDrawerConfig__GetReference.c`、`ItemDrawerConfig.ItemDrawerReference__GetDrawer.c`
—— 先按 `Type` 精确匹配、没命中再 `IsAssignableFrom` 兜底；override 找不到**仍然回落到基础档**。
消费点：`D:/2/tools/decomp_full/GeneralOfferPopupDrawer__DrawRewards.c:319` 传 `0x1e`(=30=`OfferPopups`)。
⇒ **「分类页/详情页」那一类界面用 `OfferPopups`(30) 这一档**，而**全表里只有 3 个类型写了 30**
（见 ⑤）。

---

## ④ 没解出来的部分

**没有解不出的字段。** 20 条记录 / 36 个引用 / 3 条链路，**0 条未解出**，自检 7 条全绿。
逐条列一下「本来可能解不出、实际解出了」的地方，免得下个会话怀疑：

| 可能卡住的点 | 结果 |
|---|---|
| `ComponentReference.m_SubObjectName` / `m_SubObjectType` 能不能解 | **解出来了，且 36 条全是空串** ⇒ 引用指向的是 **GameObject 整卡 prefab**（不是 prefab 里某个子件的名字），所以只能靠 GUID 定位 |
| GUID 找不到宿主 bundle | 36/36 都在 `menus_assets_all` 的 `m_Container` 里 |
| pathID 反解不出名字 | 0 条（36/36 有名字） |
| prefab 上没有 `ItemDrawer` 组件 | 0 条（36/36 都解出了抽屉类，且 36 个类名**全部**落在签名桩的 `ItemDrawer` 家族里） |

**没做、也没打算做的两件事**（写清楚，别当成「查过了没有」）：

1. **没有 `m_PathID` 逐条贴进表里** —— 它在 `--json` 输出的 `drawerPathId` / `pathId` 里，32 条太长。
   要逐条核就 `python read_itemdrawerconfig.py --json <随便一个路径>`。
2. **没有去核 `ItemDrawerConfig.GetReference` 的方法体**（调度台已读过、本条只登记出处）。
   本表是从**数据**解出来的，与那条控制流**不矛盾**；但「某个没进表的子类运行时到底回落到哪一条」
   属于 `IsAssignableFrom` 的运行时行为，**不在本表的射程内**。

---

## ⑤ 顺手发现（只报，没改任何东西）

1. 🔴 **`prefab 名` 不能用来推 `类名`** —— 实测反例：
   `Icon Random Card Drawer Variant` 身上挂的是 **`RandomCardDrawer`**（基础档同一个类），
   而签名桩里**确实存在**一个 `RandomCardIconDrawer`。同理 `Icon Currency Drawer Variant` → `CurrencyDrawer`、
   `Icon Premium Campaign Drawer Variant` → `PremiumDrawer`。
   ⇒ 谁要是按名字去挑类，会静默挑错。**要类名就以本表「prefab 上的抽屉类」那一列为准。**
   （反面对照：`Icon Wildcard Drawer Variant` → `WildcardIconDrawer`、`Icon Title Drawer Variant` → `TitleIconDrawer`、
   `Icon Forge Points Drawer Variant` → `ForgePointIconDrawer`、`Icon VIP Premium Campaign Drawer Variant` → `PremiumIconDrawer`
   —— **两种写法混着用，没有规律**。）
2. 🔴 **`OfferPopups`(30) 全表只有 3 条**：`CosmeticItemTitle`（→ `Title Drawer Horizontal Variant`）、
   `ExpansionPremiumItem`（→ `Icon Expansion Pass Premium Drawer Variant`）、
   `CosmeticItemAvatarBorder`（→ `Avatar Border Drawer Shop Variant`）。
   其余 17 个类型**都没写 30** ⇒ 开包/奖励弹窗里那些类型**全都回落到基础档**。
   我们接「奖励弹窗」时按这一个事实定版面即可。
3. **`Shop`(20) 全表只有 2 条**：`PlayerAvatar`（→ `Avatar Drawer Shop Variant`）、
   `CosmeticItemAvatarBorder`（→ `Avatar Border Drawer Shop Variant`）。
   其余 18 个类型在商店里**用基础档**（`Currency` 除外 —— 它走的是 `Icon`(10)）。
   ⚠️ **`Currency` 在商店里用的是 `Icon Currency Drawer Variant`**（override 10，不是 20）——
   这条最容易被想当然写错。
4. **`CosmeticItemAvatarBorder` 的 override 顺序是 30 在 20 前面**（不是升序）——
   说明作者手写顺序，**别假设有序**；解析时也只应 `Find(p => p.Key == override)`。
5. **`stackable = 0` 的只有 3 个类型**：`RawCardScript`、`PremiumItem`、`VIPPremiumItem`
   （其余 17 个都是 1）。`showName = 1` 的只有 4 个：`Currency` / `PlayerAvatar` /
   `CosmeticItemCardback` / `XSollaBundleItem`。
6. **`typeString` 非空的只有 4 条**：`Currency` · `PlayerAvatar` · `CosmeticItemCardback` ·
   `CosmeticItemTitle` —— 像本地化 key 的路径（`分类/子项`）。
   ⚠️ **它和 `showName` 不是一回事**：`Currency`/`PlayerAvatar`/`CosmeticItemCardback` 是 `showName=1`，
   但 **`CosmeticItemTitle` 是 `showName=0` 却也有 `typeString='PlayerProfile/Title'`** ⇒ 别把两个字段当同义词。
   ⚠️ **本机没有原版语言表**（早先查过）⇒ 这 4 个串**可能只是 key、不是显示文本** ——
   **没验证，别当成文案用**。
7. **表里 20 个类型里有 2 个是「抽象基类在表外」的写法**（`GetReference` 的 `IsAssignableFrom` 兜底要用到）：
   · `Everguild.LiveOps.ShopContainer`（表里写**具体类型**）↔ `ContainerDrawer : ItemDrawer<ShopContainerBase>`（类只写到**抽象基类**）
   · `VIPPremiumItem`（表里单列一条）↔ `ExpansionPassPremiumDrawer : ItemDrawer<ExpansionPremiumItem>`
     （`VIPPremiumItem : ExpansionPremiumItem` ⇒ 命中 `IsAssignableFrom`；但表里**另外**给它单列了一条，
     所以它走的是**精确匹配**那一支，用的是 `Premium Drawer No Glow`）
8. **`Title Drawer Horizontal Variant` 身上除了 `TitleDrawerHorizontal` 还挂着 `EverguildButton`**，
   而其余 35 个 prefab 都是 `RectTransform + AspectRatioFitter + ItemDrawerComponents + 抽屉类`。
   （**只报了这一个差异，没去查 `EverguildButton` 干什么的**。）
9. **`guid_map.tsv`（`D:/2/Unity参照管线_0825/data/guid_map.tsv`，10325 行）是一份早先建好的
   GUID→bundle 表**，我们的抽查它判得一致。**以后要解 GUID 可以先查它，能省一次全盘 m_Container 扫描。**
   但它**只有 GUID/bundle 三列**（第 2、3 列全是 `None` / `?`），**给不出名字**。
