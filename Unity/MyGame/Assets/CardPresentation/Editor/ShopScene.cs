// ShopScene.cs — 阶段二第 3 层「商店」的**自检入口**
//
// 用法：… -executeMethod ShopScene.Run        自检（结构 + 版面 + 交互 + 截图），退出码 0 = 全过
//
// 🔴 **每一条断言的期望值都盯「原版值」，不是盯我们自己写的常量**（否则就是自证）。
//    期望值来自三个页签 prefab 的**原始 JSON 走链**：
//      `工具/menu_rect.py bundle_menus_assets_all "<名>" --size 1752.83x1009.06 --relative` **再加 (167.17, 70.94)**
//      （`Card/Daily/Item Shop Tab` 的 `m_Father = 0`，是**独立 prefab 根**、运行期才挂进 `Tabs`）；
//      以及 `工具/menu_rect.py … "Catalog Item Shop Container" --root-size 335.6x475 --relative`（格内几何）。
//    交叉验证：这样算出来的 `Packs Scroll View` 与直接量 `Shop Menu Variant` 的同一节点**逐位相同**。
//
// ⚠️ **为什么没有 `BuildAndSaveScene`**：同 `RewardsScene` —— 商店是**挂在 `Shell` 锚点上的窗口**，
//    由主菜单左竖导航的 SHOP 钮开（原版 `OpenWindowButton.closeOtherMenus = 1` ⇒ `closeAll: true`）。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShopScene
{
    const string P = "[Shop] ";
    const string ShotDir = "d:/4/_tmp_view/shop";

    static int _pass, _fail;
    static readonly List<string> _failures = new List<string>();
    /// <summary>🆕 A8：19 个商品条目容器建在这棵（摆在屏外）—— 实拍那一段要把它挪进画面再拍一张。
    /// 🔴 **这棵树的孩子序号是既有读者**（下面 §⑥ 分档那一段要取「第一个容器」）
    /// ⇒ ⛔ **别往里塞别的东西** —— 2026-10-04（W1-1）就是这么红的：字号标尺插了 7 条进去，
    /// 把 19 个容器推到下标 7 起，`GetChild(0)` 抓到的是一条**没有后代**的标尺。</summary>
    static Transform _offerScratch;
    /// <summary>🆕 2026-10-04（A34-F4；**W1-1 拆出来**）：字号标尺**单独一棵**（也摆在屏外）。
    /// 原来 7 条标尺挂在 `_offerScratch` 上 ⇒ 它们成了那棵树的直接孩子 `0..6`。</summary>
    static Transform _offerRuler;

    // ============================================================ 🆕 A34-F1/F2/F4：19 份的**原版字面量**
    //
    // 🔴 **这张表是从原版 dump 抄下来的**，⛔ **不是**从 `OfferContainer.Variants` 读回来的 ——
    //    2026-10-04（**A34-F4**）订正：原来那一段的期望值（根矩形 / `Dyn` / 抽屉名 / 兄弟序 / INACT 数）
    //    **全是从被测的那张表里取的** ⇒ `Variants` 改坏了也**不会红**（= 自证）。
    // 📌 出处（19 份逐份跑，2026-10-04 整份复核了一遍）：
    //      `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<Name>" --depth 5 --relative`
    //    （坐标相对根左上角 · 左上原点 · y 向下；`act` 列 = 出厂 `m_IsActive`。）
    // 每行的字段：
    //    `W,H` 根尺寸 · `Dx1/Dy1/Dx2/Dy2` `Dynamic Content` 矩形 · `NameFs` 的 `name` 字号 ·
    //    `TypeFs` `type`/`Available Counter` 字号（同一份里两者恒等，19 份逐份核过）·
    //    `TimerFs/TimerMin/TimerMax` `Timer Text` 的 `m_fontSize` 与 `auto[min,max]` ·
    //    `Slots` `Dynamic Content` 下的槽（`|` 分隔、**兄弟序**、`*` 前缀 = 出厂 INACT）·
    //    `BgSlots` **直接挂在 `background` 下**的槽（`null` = 没有）。
    struct VExp
    {
        public string Name;
        public float W, H, Dx1, Dy1, Dx2, Dy2, NameFs, TypeFs, TimerFs, TimerMin, TimerMax;
        public string Slots, BgSlots;
        public VExp(string n, float w, float h, float dx1, float dy1, float dx2, float dy2,
                    float nameFs, float typeFs, float timerFs, float timerMin, float timerMax,
                    string slots, string bgSlots)
        {
            Name = n; W = w; H = h; Dx1 = dx1; Dy1 = dy1; Dx2 = dx2; Dy2 = dy2;
            NameFs = nameFs; TypeFs = typeFs; TimerFs = timerFs; TimerMin = timerMin; TimerMax = timerMax;
            Slots = slots; BgSlots = bgSlots;
        }
    }

    static readonly VExp[] VExpAll =
    {
        new VExp("General Basic Offer Container Booster_CardOrAltArt",
                 391f, 930f, 145.5f, 348f, 245.5f, 448f, 42f, 34f, 30.6f, 10f, 32f,
                 "Icon Container Drawer Variant|Card Alternate Art Drawer|Card Drawer",
                 null),
        new VExp("General Basic Offer Container Booster_CardOrAltArt_Cardback_Avatar_Title",
                 391f, 930f, 145.5f, 348f, 245.5f, 448f, 42f, 34f, 30.6f, 10f, 32f,
                 "Cardback Drawer|Icon Container Drawer Variant|Icon Avatar Drawer Variant|Card Alternate Art Drawer|*Card Drawer|Title Drawer Horizontal Variant (1)",
                 null),
        new VExp("General Basic Offer Container Booster_CardOrAltArt__AvatarORTitle",
                 391f, 930f, 145.5f, 348f, 245.5f, 448f, 42f, 34f, 30.6f, 10f, 32f,
                 "Cardback Drawer|Icon Container Drawer Variant|Card Alternate Art Drawer|*Card Drawer|Title Drawer Horizontal Variant (1)|Icon Avatar Drawer Variant",
                 null),
        new VExp("General Basic Offer Container Variant 2 Currencies",
                 339f, 778f, 119.5f, 358f, 219.5f, 458f, 36.7f, 30f, 28f, 18f, 28f,
                 "Icon Currency Drawer Variant|Icon Currency Drawer Variant (1)",
                 null),
        new VExp("General Basic Offer Container Variant Booster + 2 Currencies",
                 339f, 778f, 119.5f, 358f, 219.5f, 458f, 36.7f, 30f, 28f, 18f, 28f,
                 "Icon Container Drawer Variant|Icon Currency Drawer Variant (1)|Icon Currency Drawer Variant",
                 null),
        new VExp("General Basic Offer Container Variant Booster_avatar_cardback_title",
                 339f, 778f, 119.5f, 358f, 219.5f, 458f, 36.7f, 30f, 28f, 18f, 28f,
                 "Icon Container Drawer Variant|Cardback Drawer|Title Drawer Horizontal Variant|Icon Avatar Drawer Variant",
                 null),
        new VExp("General Basic Offer Container Variant Booster_avatar_resource",
                 339f, 778f, 119.5f, 358f, 219.5f, 458f, 36.7f, 30f, 28f, 18f, 28f,
                 "Icon Container Drawer Variant|Icon Avatar Drawer Variant|Icon Currency Drawer Variant",
                 null),
        new VExp("General Basic Offer Container Variant Booster_cardback_resource",
                 339f, 778f, 119.5f, 358f, 219.5f, 458f, 36.7f, 30f, 28f, 18f, 28f,
                 "Icon Container Drawer Variant|Cardback Drawer|Icon Currency Drawer Variant",
                 null),
        // 🔴 **19 份里唯一一份 `background` 有 **4** 个孩子的** —— 第 4 个（`BgSlots`）不在 `Dynamic Content` 下。
        new VExp("General Basic Offer Container Variant Booster_title_resource",
                 339f, 778f, 119.5f, 358f, 219.5f, 458f, 36.7f, 30f, 28f, 18f, 28f,
                 "Icon Container Drawer Variant|Icon Currency Drawer Variant",
                 "Title Drawer Horizontal Variant (1)"),
        new VExp("General Basic Offer Container Variant Deck_cardback_avatar",
                 339f, 778f, 119.5f, 280f, 219.5f, 380f, 36.7f, 34f, 30.6f, 10f, 32f,
                 "Deck Drawer|Cardback Drawer|Icon Avatar Drawer Variant",
                 null),
        new VExp("General Basic Offer Container Variant Premium_Booster_avatar_cardback_title",
                 339f, 778f, 119.5f, 358f, 219.5f, 458f, 36.7f, 30f, 28f, 18f, 28f,
                 "Icon Container Drawer Variant|Cardback Drawer|Title Drawer Horizontal Variant|Icon Avatar Drawer Variant|Icon Premium Campaign Drawer Variant",
                 null),
        new VExp("General Basic Offer Container Variant Premium_Booster_avatar_cardback_title_resource",
                 339f, 778f, 119.5f, 358f, 219.5f, 458f, 36.7f, 30f, 28f, 18f, 28f,
                 "Icon Container Drawer Variant|Cardback Drawer|Title Drawer Horizontal Variant|Icon Avatar Drawer Variant|Icon Expansion Pass Premium Drawer Variant|Icon Currency Drawer Variant|Icon Premium Campaign Drawer Variant",
                 null),
        new VExp("General Basic Offer Container Variant Premium_Premium_cardback_avatar",
                 339f, 778f, 119.5f, 280f, 219.5f, 380f, 36.7f, 34f, 30.6f, 10f, 32f,
                 "Cardback Drawer|Icon Avatar Drawer Variant|Icon Premium Campaign Drawer Variant|Icon Premium Campaign Drawer Variant (1)",
                 null),
        new VExp("General Basic Offer Container Variant Premium_Resource",
                 339f, 778f, 119.5f, 358f, 219.5f, 458f, 36.7f, 30f, 28f, 18f, 28f,
                 "Icon Currency Drawer Variant|Icon Expansion Pass Premium Drawer Variant|Icon Premium Campaign Drawer Variant",
                 null),
        new VExp("General Basic Offer Container Variant Premium_booster_title_avatarOrResource",
                 339f, 778f, 119.5f, 295f, 219.5f, 395f, 36.7f, 30f, 28f, 18f, 28f,
                 "Icon Container Drawer Variant|Icon Avatar Drawer Variant|Title Drawer Horizontal Variant|Icon Currency Drawer Variant|Icon Premium Campaign Drawer Variant",
                 null),
        new VExp("General Basic Offer Container Variant Single Item Type",
                 339f, 778f, 119.5f, 272f, 219.5f, 372f, 36.7f, 34f, 30.6f, 10f, 32f,
                 "*Icon Container Drawer Variant|*Icon Avatar Drawer Variant|*Icon Avatar Drawer Variant 2|*Title Drawer Horizontal Variant|*Icon Currency Drawer Variant|*Icon Currency Drawer Variant 2|*Icon Currency Drawer Variant 3|*Icon Premium Campaign Drawer Variant|Card Drawer|Icon Expansion Pass Premium Drawer Variant (1)|Icon Avatar Border Drawer|Cardback Drawer",
                 null),
        new VExp("General Basic Offer Container Variant avatarOrTitle_resource",
                 339f, 778f, 119.5f, 272f, 219.5f, 372f, 36.7f, 34f, 30.6f, 10f, 32f,
                 "Icon Currency Drawer Variant|Title Drawer Horizontal Variant (1)|Icon Avatar Drawer Variant",
                 null),
        new VExp("General Basic Offer Container Variant cardback_premiumOrAvatarOrResource_titleOrResource",
                 339f, 778f, 119.5f, 280f, 219.5f, 380f, 36.7f, 34f, 30.6f, 10f, 32f,
                 "Cardback Drawer|Icon Avatar Drawer Variant|Title Drawer Horizontal Variant|Icon Currency Drawer Variant|Icon Currency Drawer Variant 2|Icon Premium Campaign Drawer Variant",
                 null),
        new VExp("Small General Basic Offer Container Variant Single Item Type",
                 339f, 390f, 119.5f, 78f, 219.5f, 178f, 36.7f, 34f, 30.6f, 10f, 32f,
                 "*Icon Container Drawer Variant|*Icon Avatar Drawer Variant|*Title Drawer Horizontal Variant|Icon Currency Drawer Variant|*Cardback Drawer|*Icon Expansion Pass Premium Drawer Variant|*Icon Premium Campaign Drawer Variant",
                 null),
    };

    // ============================================================ 🆕 2026-10-04（A43）：**按物品类型选槽**的原版字面量
    //
    // 判据（全部读过，逐条见每行的 `Why`）：
    //   · `d:/2/tools/decomp_full/GeneralOfferPopupDrawer__DrawRewards.c:87-372` —— 池 = `GetComponentsInChildren` 的
    //     **先序**（含 `background` 下那一格）· `ItemDrawer.GetDrawerConfig(itemType, **0x1e**)`（:319）·
    //     `ReflectionHelper.Is(池键, cfg 类)` 命中（`…DisplayClass1_0___DrawRewards_b__3.c:21`）·
    //     `First()` → `Setup` → `SetActive(true)`（:363）→ **`RemoveAt(0)`**（:364）
    //   · `ItemDrawerConfig__GetReference.c`（第一轮**精确** · 第二轮 **is-a** 兜底 · 都没命中 = **空配置**）
    //   · `ItemDrawerConfig.ItemDrawerReference__GetDrawer.c`（按键找 override，**找不到回落主档**）
    //   · 映射表 = `资料/普查产出_1004/ItemDrawerConfig_映射表.md`（20 类型 / 36 个 GUID / **零条靠猜**）
    //   · 槽的类 = 19 份逐份实读（节点组件的 `m_Script` → `MonkeyScript.m_ClassName` —— 与
    //     `工具/read_itemdrawerconfig.py` 同一条链）
    // 🔴 **期望值一律是【原版字面量】**（类型名 / 槽节点名 / 类名 / 「一个都不填」）——
    //    ⛔ **不读** `OfferContainer` 那两张表、也**不从它算下标**：这一块只按**名字**认槽。
    struct SlotExp
    {
        public string Variant;      // 变体（原 prefab 名）
        public string ItemType;     // 原版 `ObtainableItem` 子类型名（`ItemDrawerConfig` 那张表的键）
        public string Slot;         // 期望被填的槽（**原版 prefab 里的节点名**）；`null` = 期望**一个都不填**
        public string Under;        // 该槽挂在谁下面（`Dynamic Content` / `background`）；不填时为 `null`
        public string Why;          // 原版依据（哪一条映射 / 哪一跳匹配）
        public bool SlotWasOff;     // 该槽在**原版 dump 里是出厂 INACT**（`act` 列 = `*`）
        public int Ordinal;         // **同类第几个**（0 = 第一个；`RemoveAt(0)` 的语义）
        public SlotExp(string v, string t, string slot, string under, string why,
                       bool wasOff = false, int ord = 0)
        { Variant = v; ItemType = t; Slot = slot; Under = under; Why = why; SlotWasOff = wasOff; Ordinal = ord; }
    }

    static readonly SlotExp[] SlotExpAll =
    {
        // ---- ① 普通档：类型 → 主档 ----（`CosmeticItemCardback` / `RawCardScript` / `AlternateArtCard` …）
        new SlotExp("General Basic Offer Container Variant Booster_avatar_cardback_title", "CosmeticItemCardback",
                    "Cardback Drawer", "Dynamic Content",
                    "映射表 :47 —— `CosmeticItemCardback` 主档 = `Cardback Drawer`（类 `CardbackDrawer`）"),
        new SlotExp("General Basic Offer Container Variant Booster_avatar_cardback_title", "CosmeticItemTitle",
                    "Title Drawer Horizontal Variant", "Dynamic Content",
                    "映射表 :59/:60 —— 30 档那三条之一（`Title Drawer Horizontal Variant`，类 `TitleDrawerHorizontal`）；"
                    + "该变体 4 个槽里类为 `TitleDrawerHorizontal` 的只有这**第 3 个**"),
        new SlotExp("General Basic Offer Container Booster_CardOrAltArt", "AlternateArtCard",
                    "Card Alternate Art Drawer", "Dynamic Content",
                    "映射表 :74 —— `AlternateArtCard` 主档 = `Card Alternate Art Drawer`（类 `CardAlternateArtDrawer`）"),
        new SlotExp("General Basic Offer Container Variant Single Item Type", "RawCardScript",
                    "Card Drawer", "Dynamic Content",
                    "映射表 :44 —— `RawCardScript` 主档 = `Card Drawer`（类 `CardDrawer`）"),
        new SlotExp("General Basic Offer Container Variant Premium_Resource", "ExpansionPremiumItem",
                    "Icon Expansion Pass Premium Drawer Variant", "Dynamic Content",
                    "映射表 :67 —— 30 档那三条之一（类 `ExpansionPassPremiumDrawer`）"),
        new SlotExp("General Basic Offer Container Variant Booster_avatar_resource",
                    "Everguild.LiveOps.ShopContainer", "Icon Container Drawer Variant", "Dynamic Content",
                    "映射表 :42/:43 —— 表里那条的类型名**带命名空间**；类 = `ContainerDrawer`"
                    + "（`ContainerDrawer : ItemDrawer<ShopContainerBase>`）"),
        // ---- ② **is-a 那两跳**（不靠等号的两处，正是原版 `ReflectionHelper.Is` 存在的理由）----
        new SlotExp("General Basic Offer Container Variant Deck_cardback_avatar", "PrebuiltDeck",
                    "Deck Drawer", "Dynamic Content",
                    "映射表 :56 —— `PrebuiltDeck` 主档 = `Deck Drawer` prefab（类 **`DeckDrawer`**），"
                    + "而**容器里这一格**挂的是它的**子类 `DeckAndCardbackDrawer`**（19 份逐份实读）"
                    + "⇒ **靠 is-a 命中**；**严格相等会一个槽都配不上**"),
        new SlotExp("General Basic Offer Container Variant Deck_cardback_avatar", "PrebuiltSortedDeck",
                    "Deck Drawer", "Dynamic Content",
                    "`PrebuiltSortedDeck : PrebuiltDeck`（签名桩 `PrebuiltSortedDeck.cs:1`）—— 表里**没有**这一条"
                    + "⇒ 走 `GetReference` **第二轮**（沿基类链上溯）落到 `PrebuiltDeck`"),
        new SlotExp("General Basic Offer Container Variant 2 Currencies", "Energy",
                    "Icon Currency Drawer Variant", "Dynamic Content",
                    "`Energy : Currency`（`Energy.cs:1`）⇒ 第二轮兜底到 `Currency` → 主档 `Icon Currency Drawer Variant`"
                    + "（类 `CurrencyDrawer`）⇒ 池里**第一个**同类槽"),
        new SlotExp("General Basic Offer Container Variant Booster_avatar_resource", "DlcBundle",
                    "Icon Container Drawer Variant", "Dynamic Content",
                    "`DlcBundle : ShopContainer`（`DlcBundle.cs:4`）⇒ 第二轮上溯到 `Everguild.LiveOps.ShopContainer`"),
        // ---- ③ `RemoveAt(0)`：同一个类型的**第 2 个**落**第 2 个**同类槽 ----
        new SlotExp("General Basic Offer Container Variant 2 Currencies", "Currency",
                    "Icon Currency Drawer Variant (1)", "Dynamic Content",
                    "同一类第 **2** 个 —— 原版填完一个就 `removeAt(0)` 把它从池里消费掉（`DrawRewards.c:364`）"
                    + "⇒ 第 2 个落**第 2 个**同类槽", false, 1),
        // ---- ④ 出厂 INACT 的那一格：**选中就要打开**（原版 `:363`）----
        new SlotExp("General Basic Offer Container Booster_CardOrAltArt_Cardback_Avatar_Title", "RawCardScript",
                    "Card Drawer", "Dynamic Content",
                    "映射表 :44 —— `RawCardScript` 主档 = `Card Drawer`（类 `CardDrawer`）；"
                    + "🔴 **这一格出厂 INACT**（原版 dump 的 `act` 列 = `*Card Drawer`，本文件 `VExpAll` 里逐字写着）"
                    + "⇒ 选中时会被 `SetActive(true)` 打开（原版 `:363`）", true),
        new SlotExp("General Basic Offer Container Variant Single Item Type", "CosmeticItemAvatarBorder",
                    "Icon Avatar Border Drawer", "Dynamic Content",
                    "映射表 :71/:72 —— 30 档那三条之一（`Avatar Border Drawer Shop Variant`，类 `AvatarBorderDrawer`）；"
                    + "⚠️ 这一格**出厂是 ACTIVE**（原版 dump 的 `act` 列没 `*` —— 别想当然当它关着）"),
        // ---- ⑤ **池包含 `background` 下那一格**（19 份里只有这一份有）----
        new SlotExp("General Basic Offer Container Variant Booster_title_resource", "CosmeticItemTitle",
                    "Title Drawer Horizontal Variant (1)", "background",
                    "🔴 那一格**挂在 `background` 下**（19 份里只有这一份如此）—— 原版池 = 整棵树的**先序**"
                    + "（`DrawRewards.c:87`）⇒ 它在池里、排在 `Dynamic Content` 那批**之后**；"
                    + "该变体 `Dynamic Content` 下那两个槽是 Container / Currency，**都对不上**"),
        // ---- ⑥ 负例：**什么都不填**（不是「随便挑一个槽」）----
        new SlotExp("General Basic Offer Container Variant Booster_avatar_cardback_title", "Wildcard",
                    null, null,
                    "映射表 :48 —— `Wildcard` 主档 = `Wildcard Drawer`（类 `WildcardDrawer`）；"
                    + "而 **19 份的 18 个槽名里一个 `WildcardDrawer` 都没有**（逐份实读）"
                    + "⇒ 原版 `First` 找不到 ⇒ `continue`（`:346`）"),
        new SlotExp("General Basic Offer Container Variant Booster_avatar_cardback_title", "DropTableContainer",
                    null, null,
                    "`DropTableContainer : ShopContainerBase`（`DropTableContainer.cs:8`），而表里那条写的是**具体类**"
                    + " `Everguild.LiveOps.ShopContainer`（**兄弟**，不是祖先）⇒ 第二轮 is-a **也命中不了** ⇒ 空配置"),
        new SlotExp("General Basic Offer Container Variant Booster_avatar_cardback_title", "",
                    null, null,
                    "物品类型**判据空**（`Content.ItemType` 空、`Item.Kind` 也推不出）⇒ "
                    + "照原版「没有配置 ⇒ 这一项什么都不画」+ **出声**"),

        // ============================================================ 🆕 2026-10-04（**F7 补覆盖**）
        //  🔴 **为什么补**：A43 那一轮把 §⑥ 的 19 份循环改成 `fill: false`（那 19 棵改比**出厂态**）之后，
        //     「填槽」这条路的覆盖**从 19 个变体掉到 9 个**（上面 ①~⑥ 那几组只在 9 个变体上跑）。
        //     下面这 **10 行**把**剩下的 10 个变体**各补一条 —— 判据、期望值写法**完全同上**（槽名/类名全是
        //     原版字面量），于是「每个变体都真跑过一次 `Build(fill: true)`」这件事由下面**（a2）那条覆盖闸**钉住。
        //  ⚠️ 未覆盖那 10 个里**含唯一的 `Small …`（339×390）** ⇒ 这一条尤其不能省。
        // ---- ⑦ 391×930 那两份（第三条 `Booster_CardOrAltArt` 已在 ①~④ 里跑过）----
        new SlotExp("General Basic Offer Container Booster_CardOrAltArt__AvatarORTitle", "RawCardScript",
                    "Card Drawer", "Dynamic Content",
                    "映射表 :44 —— `RawCardScript` 主档 = `Card Drawer`（类 `CardDrawer`）；"
                    + "该变体 6 个槽里类为 `CardDrawer` 的只有**第 4 个**，且它**出厂 INACT**（`VExpAll` 写着 `*Card Drawer`）", true),
        // ---- ⑧ 339×778 那八份 ----
        new SlotExp("General Basic Offer Container Variant Booster + 2 Currencies", "Currency",
                    "Icon Currency Drawer Variant (1)", "Dynamic Content",
                    "映射表 :37 —— `Currency` 主档 = `Currency Drawer`（类 `CurrencyDrawer`）；该变体池序 = "
                    + "Container / Currency**(1)** / Currency ⇒ 第一个同类槽是**带 ` (1)` 后缀那个**"
                    + "（**池序≠名字序** —— 按名字猜会猜错，这条正是那个反例）"),
        new SlotExp("General Basic Offer Container Variant Booster_cardback_resource", "CosmeticItemCardback",
                    "Cardback Drawer", "Dynamic Content",
                    "映射表 :47 —— `CosmeticItemCardback` 主档 = `Cardback Drawer`（类 `CardbackDrawer`）"),
        new SlotExp("General Basic Offer Container Variant Premium_Booster_avatar_cardback_title", "PremiumItem",
                    "Icon Premium Campaign Drawer Variant", "Dynamic Content",
                    "映射表 :61 —— `PremiumItem` 主档 = `Premium Drawer`（类 `PremiumDrawer`）；"
                    + "该变体 5 个槽里类为 `PremiumDrawer` 的只有**第 5 个**"),
        new SlotExp("General Basic Offer Container Variant Premium_Booster_avatar_cardback_title_resource",
                    "ExpansionPremiumItem", "Icon Expansion Pass Premium Drawer Variant", "Dynamic Content",
                    "映射表 :67 —— 30 档那三条之一（类 `ExpansionPassPremiumDrawer`）"),
        new SlotExp("General Basic Offer Container Variant Premium_Premium_cardback_avatar", "PremiumItem",
                    "Icon Premium Campaign Drawer Variant", "Dynamic Content",
                    "映射表 :61 —— `PremiumItem` 主档（类 `PremiumDrawer`）；该变体有**两个**同类槽"
                    + "（`…Variant` 与 `…Variant (1)`）⇒ 第 1 个同类项落**第 1 个**"),
        new SlotExp("General Basic Offer Container Variant Premium_booster_title_avatarOrResource", "PlayerAvatar",
                    "Icon Avatar Drawer Variant", "Dynamic Content",
                    "映射表 :39 —— `PlayerAvatar` 主档 = `Avatar Drawer`（类 `AvatarDrawer`）"),
        new SlotExp("General Basic Offer Container Variant avatarOrTitle_resource", "CosmeticItemTitle",
                    "Title Drawer Horizontal Variant (1)", "Dynamic Content",
                    "映射表 :59/:60 —— 30 档那三条之一（类 `TitleDrawerHorizontal`）；"
                    + "该变体 3 个槽里类为 `TitleDrawerHorizontal` 的只有**第 2 个**（名字带 ` (1)` 的那个）"),
        new SlotExp("General Basic Offer Container Variant cardback_premiumOrAvatarOrResource_titleOrResource",
                    "Currency", "Icon Currency Drawer Variant", "Dynamic Content",
                    "映射表 :37 —— `Currency` 主档（类 `CurrencyDrawer`）；该变体 6 个槽里前三个都不是它 ⇒ 第 4 个"),
        // ---- ⑨ 339×390 那份（**唯一的小尺寸变体**）----
        new SlotExp("Small General Basic Offer Container Variant Single Item Type", "CosmeticItemCardback",
                    "Cardback Drawer", "Dynamic Content",
                    "映射表 :47 —— `CosmeticItemCardback` 主档（类 `CardbackDrawer`）；🔴 这一格**出厂 INACT**"
                    + "（`VExpAll` 写着 `*Cardback Drawer`）⇒ 原版选中就 `SetActive(true)`（`:363`）打开它；"
                    + "而该变体**唯一出厂 ACTIVE** 的那一格（`Icon Currency Drawer Variant`）必须**被关掉**"
                    + "（原版 `:112` 的「整池先全关」—— 见下面那条「只有命中的槽开着」）", true),
    };

    // ============================================================ 🆕 2026-10-04（F7/F9）：**override 30 档的期望类**
    //
    // 🔴 **这是【原版字面量】**（⛔ 不从 `ItemDrawer.ItemTypeSets` 读回来）：出处 = `ItemDrawerConfig_映射表.md`
    //    ② 那张表 —— **写 30 的只有 3 条**（`CosmeticItemTitle` / `ExpansionPremiumItem` / `CosmeticItemAvatarBorder`，
    //    映射表 §⑤.2），其余类型的 30 档按原版 `GetDrawer` **回落主档** ⇒ 类 = 它的主档类。
    // 用途（两处）：① 逐条用例比 `Built.DrawerClass`（**那个字段 2026-10-04 之前全库没有读者** —— R-X1 的 F9）；
    //              ② 顺带把「30 档这一跳」的类型覆盖面从 3 条扩到**本表 15 条**（判别力比原来那 3 条强）。
    // ⚠️ **表里故意没有** `DropTableContainer`（它 `: ShopContainerBase`，与表里那条 `Everguild.LiveOps.ShopContainer`
    //    是**兄弟**）与空字符串 —— 这两个的期望就是 **`null`**（`ClassAt30` 查不到 ⇒ 返回 `null`）。
    struct TypeCls
    {
        public string Type, Cls;
        public TypeCls(string t, string c) { Type = t; Cls = c; }
    }

    static readonly TypeCls[] DrawerAt30 =
    {
        new TypeCls("Currency", "CurrencyDrawer"),                     // 映射表 :37
        new TypeCls("PlayerAvatar", "AvatarDrawer"),                   // :39
        new TypeCls("Everguild.LiveOps.ShopContainer", "ContainerDrawer"),   // :42/:43
        new TypeCls("RawCardScript", "CardDrawer"),                    // :44
        new TypeCls("CosmeticItemCardback", "CardbackDrawer"),         // :47
        new TypeCls("Wildcard", "WildcardDrawer"),                     // :48
        new TypeCls("PrebuiltDeck", "DeckDrawer"),                     // :56（类 = `DeckDrawer`）
        new TypeCls("PrebuiltSortedDeck", "DeckDrawer"),               // 上溯到 `PrebuiltDeck`（签名桩 `:1`）
        new TypeCls("Energy", "CurrencyDrawer"),                       // 上溯到 `Currency`（`Energy.cs:1`）
        new TypeCls("DlcBundle", "ContainerDrawer"),                   // 上溯到 `Everguild.LiveOps.ShopContainer`
        new TypeCls("CosmeticItemTitle", "TitleDrawerHorizontal"),     // :60 —— **写 30 的三条之一**
        new TypeCls("ExpansionPremiumItem", "ExpansionPassPremiumDrawer"),   // :67 —— 同上
        new TypeCls("CosmeticItemAvatarBorder", "AvatarBorderDrawer"), // :71/:72 —— 同上
        new TypeCls("PremiumItem", "PremiumDrawer"),                   // :61（30 档没写 ⇒ 回落主档）
        new TypeCls("AlternateArtCard", "CardAlternateArtDrawer"),     // :74
    };

    /// <summary>原版类型名 → **override 30 档解出的抽屉类**（表里没有 ⇒ `null` = **期望就是「没解出」**）。</summary>
    static string ClassAt30(string t)
    {
        for (int i = 0; i < DrawerAt30.Length; i++) if (DrawerAt30[i].Type == t) return DrawerAt30[i].Cls;
        return null;
    }

    // ============================================================ 🆕 2026-10-03（A79②）：**override 10/15/20 三档的期望类**
    //
    // 🔴 **这是【原版字面量】**（⛔ 不从 `ItemDrawer.ItemTypeSets` 读回来）：出处 = `ItemDrawerConfig_映射表.md` §②
    //    那张表，键 = `类型|override 值`。全表 **19 档 override** 里 `OfferPopups`(30) 那 **3** 条已由 `DrawerAt30` 钉住
    //    ⇒ 这里列**剩下 16 条、一条不少**（`Icon`(10) **13** · `Horizontal`(15) **1** · `Shop`(20) **2**）。
    // ⚠️ 每行的 `:NN` = **映射表 §② 的行号**（可逐条复查）；⛔ 别照 `ItemTypeSets` 抄这一列 —— 那就成了自证。
    // ⚠️ 表里**只列「写了这一档」的**：其余类型/档位**没写** ⇒ 照原版 `GetDrawer.c:23-33` **回落主档**
    //    （那一条由上面 `RawCardScript|Icon` 那条单独钉）。四条「**变体 ≠ 主档**」的行在后面标了 🔴。
    static readonly TypeCls[] DrawerAtOv =
    {
        new TypeCls("Currency|10", "CurrencyDrawer"),                                          // :38
        new TypeCls("PlayerAvatar|10", "AvatarDrawer"),                                        // :40
        new TypeCls("PlayerAvatar|20", "AvatarDrawer"),                                        // :41
        new TypeCls("Everguild.LiveOps.ShopContainer|10", "ContainerDrawer"),                 // :43
        new TypeCls("DropTableItem|10", "RandomCardDrawer"),                                   // :46
        new TypeCls("Wildcard|10", "WildcardIconDrawer"),                                      // :49 🔴（主档 `WildcardDrawer`）
        new TypeCls("CampaignPoints|10", "CampaignPointDrawer"),                               // :51
        new TypeCls("ForgePoints|10", "ForgePointIconDrawer"),                                 // :53 🔴（主档 `ForgePointDrawer`）
        new TypeCls("Everguild.LiveOps.ExpansionPassPoints|10", "ExpansionPassPointDrawer"),   // :55
        new TypeCls("CosmeticItemTitle|10", "TitleIconDrawer"),                                // :58 🔴（主档 `TitleDrawer`）
        new TypeCls("CosmeticItemTitle|15", "TitleDrawerHorizontal"),                          // :59
        new TypeCls("PremiumItem|10", "PremiumDrawer"),                                        // :62
        new TypeCls("VIPPremiumItem|10", "PremiumIconDrawer"),                                 // :64 🔴（主档 `PremiumDrawer`）
        new TypeCls("ExpansionPremiumItem|10", "ExpansionPassPremiumDrawer"),                  // :66
        new TypeCls("AllianceTrophyData|10", "AllianceBadgeDrawer"),                           // :69
        new TypeCls("CosmeticItemAvatarBorder|20", "AvatarBorderDrawer"),                      // :72
    };

    /// <summary>🆕 2026-10-04（F3/F9）：一棵容器里**开着**的抽屉槽名（`|` 分隔；按池的**先序**）。
    /// 口径 = 池序那一套（`Dynamic Content` 的直接孩子 + `background` 里 `foreground`/`Dynamic Content`/`name-bg`
    /// 之外的直接孩子），但**名字从建出来的树读** —— 给「填完之后**只有命中的槽**开着」那条当**实得值**。
    /// ⚠️ 滤掉 `… Gfx`（我们这套 `MenuDraw` 的产物，口径同 `KidNames`）。</summary>
    static string ActiveSlotNames(Transform root)
    {
        var sb = new System.Text.StringBuilder();
        var dyn = FindChild(root, OfferContainer.NDynamic);
        CollectActive(dyn, sb);
        var bg = FindChild(root, OfferContainer.NBackground);
        if (bg != null)
            for (int k = 0; k < bg.childCount; k++)
            {
                var ch = bg.GetChild(k);
                if (ch == dyn || ch.name == OfferContainer.NForeground || ch.name == OfferContainer.NNameBg) continue;
                if (ch.name.EndsWith(" Gfx")) continue;
                if (!ch.gameObject.activeSelf) continue;
                if (sb.Length > 0) sb.Append("|");
                sb.Append(ch.name);
            }
        return sb.ToString();
    }

    static void CollectActive(Transform t, System.Text.StringBuilder sb)
    {
        if (t == null) return;
        for (int k = 0; k < t.childCount; k++)
        {
            var ch = t.GetChild(k);
            if (ch.name.EndsWith(" Gfx")) continue;
            if (!ch.gameObject.activeSelf) continue;
            if (sb.Length > 0) sb.Append("|");
            sb.Append(ch.name);
        }
    }


    /// <summary>`name-bg` 那 6 个孩子的**名字 + 兄弟序**（19 份逐字相同；原版 `m_Children` 实读）。
    /// 🆕 A34-F3：那颗出厂 INACT 的 `Image` **在这里**、不在 `WebShop` 里 —— 断言就是拿它钉住的。
    /// 🔴 **2026-10-04（首跑红了，就地订正）**：`KidNames(…)` 会给**出厂 INACT** 的孩子**加 `*` 前缀**
    /// （这是本文件那套串的约定，另两条 `bgKids4` 用的也是它）⇒ 期望串里那一格必须是 **`*Image`**；
    /// 原来写成 `Image`（照 dump 抄的，dump 不标 activeSelf）⇒ 19 份全红。</summary>
    const string NameBgKids = "name|type|Price Display Button|WebShop Button Square Variant|*Image|Available Counter";
    static readonly Color TypeColorLit = new Color(0.717f, 0.717f, 0.717f, 1f);

    // 🆕 **A34-F3**：`WebShop Button Square Variant` 那两个子件的矩形 —— **逐代**，全是原版 dump 的字面量。
    //   ⚠️ **不读 `OfferContainer.Geo`**（那是被测的表）；按**原版根高**选代（930 / 778 / 390 都是 dump 里的数）。
    //   顺序 = { 391×930 · 339×778 · 339×390 }。
    static readonly PxRect[] HighlightRect =
    {
        new PxRect(265.4f, 841.7f, 397.9f, 940.8f),
        new PxRect(227.0f, 701.2f, 347.5f, 790.2f),
        new PxRect(227.0f, 313.9f, 347.5f, 402.3f),
    };
    /// <summary>原版 `WebShop Button Square Variant/Icon` 的**布局矩形**（**未乘 `scl 1.2`**、也
    /// **未跑 `AspectRatioFitter`**）—— dump 的字面量。
    /// 🔴 **2026-10-04（W1-3）**：它的 **`W` 不是渲染宽**（那颗挂着 `HeightControlsWidth` 的
    /// `AspectRatioFitter` ⇒ 宽被改写成 = 高，见 <see cref="IconSquare"/>）。本表留着当
    /// **中心**与「布局矩形」的记录（中心那一条断言用它；渲染尺寸用 `IconSquare`）。
    /// ⚠️ 中心已独立复核（按原始 RT 的 anchors/sizeDelta × 父链解算）= **(331.654,891.273) /
    /// (287.260,745.730) / (287.260,358.105)**，与本表算出来的差 ≤ 0.05px ✓。</summary>
    static readonly PxRect[] IconRect =
    {
        new PxRect(298.3f, 863.7f, 365.0f, 918.8f),
        new PxRect(258.4f, 723.2f, 316.2f, 768.2f),
        new PxRect(258.4f, 335.9f, 316.2f, 380.3f),
    };

    /// <summary>🆕 **2026-10-04（W1-3）**：`WebShop/Icon` **渲出来的正方形边长**（还没乘 `m_LocalScale` 的那一步）。
    /// 原版那颗挂着 `AspectRatioFitter`（`m_Enabled 1` · `m_AspectMode 2 HeightControlsWidth` · `m_AspectRatio 1.0`；
    /// 19 份**逐份实读全同**）⇒ 宽被改写成 = 高（`pivot 0.5` ⇒ 中心不动）⇒ **渲染 = 高² 的正方形**。
    /// <para>**独立复算**（⛔ **不读** `OfferContainer` 的常量/`Geo`）：它的 RT
    /// （`RectTransform_3907242956008901081`）纵向拉满（`anchorMin.y 0` / `anchorMax.y 1`）
    /// + `m_SizeDelta.y = −7.5547` ⇒ **高 = `WebShop` 的高 − 7.5547**；
    /// `WebShop` 的高 = 由它自己的 anchors/sizeDelta × `name-bg` 的实算高得 **62.690 / 52.547 / 52.032**
    /// （19 份逐份解算，三档）。⇒ 55.135 / 44.992 / 44.477（审查 R-W1 独立算的 55.13 / 44.99 / 44.47 同档 ✓）。
    /// ⚠️ 这里**不用 dump 那一列的一位小数**（55.1 / 45.0 / 44.4）—— 那是 `menu_dump` 的打印精度，
    /// 本条按原始 RT 的 anchors/sizeDelta 解到 0.001px。</para>
    /// <para>顺序 = { 391×930 · 339×778 · 339×390 }（同 <see cref="GenOf"/>）。</para></summary>
    static readonly float[] IconSquare = { 62.690f - 7.5547f, 52.547f - 7.5547f, 52.032f - 7.5547f };
    /// <summary>原版根高 → 上面那**三**张表（`HighlightRect` / `IconRect` / `IconSquare`）的下标（930 / 778 / 390）。</summary>
    static int GenOf(float rootH) { return rootH > 900f ? 0 : (rootH > 500f ? 1 : 2); }

    /// <summary>把一棵 `Dynamic Content`（或 `background` / `name-bg`）下的孩子列成 `a|b|c`，**名字 + 兄弟序**，
    /// 出厂关着的加 `*` 前缀（与原版 dump 的 `act` 列同一个写法）。
    /// <para>🔴 **`… Gfx` 那些子件不计入** —— 它们是**我们这套 `MenuDraw` 的产物**（`name-bg Gfx` / `background Gfx`
    /// 都是「无图 ⇒ 画一块纯色块」那一支建出来的实心 quad），**原版那棵树里没有**。
    /// ⇒ 不过滤的话下面每一条「孩子名 + 顺序」都会多出一格、**假红**。
    /// ⚠️ 它们的**存在**另有断言（`background Gfx` 只该在 `Small` 那一份上、`name-bg Gfx` 19 份都有）。</para></summary>
    static string KidNames(Transform t)
    {
        if (t == null) return null;
        var sb = new System.Text.StringBuilder();
        for (int k = 0; k < t.childCount; k++)
        {
            var ch = t.GetChild(k);
            if (ch.name.EndsWith(" Gfx")) continue;
            if (sb.Length > 0) sb.Append("|");
            if (!ch.gameObject.activeSelf) sb.Append("*");
            sb.Append(ch.name);
        }
        return sb.ToString();
    }

    /// <summary>有没有这个名字的**直接**孩子。</summary>
    static bool HasChild(Transform t, string name)
    {
        return t != null && t.Find(name) != null;
    }

    /// <summary>🆕 **§A251-L2/L4（2026-10-13）**：`t` 的**直接子件名集合**必须恰好是给定的那几个。
    /// <para>🔴 **为什么不能拿 `FindChild` 写层级断言**：它走 `GetComponentsInChildren`（**整棵子树**）
    /// ⇒ 「节点挂在错的父下面」照样捞得到、断言**恒真**（`TrophyInfoPopup` 那条「层级错了会静默」的
    /// 教训就是这么来的）。这里**只数直系**（`KidNameArr`，与 `KidNames` 同一份过滤口径）。</para>
    /// <para>⚠️ **实参约定**（本节的调用点统一这么写）：**最后一个实参 = 断言的文字**，前面全是**期望的名字**。
    /// 顺序**不参与比对**（原版的兄弟序另有 `KidNames` 那条断言盯着）。</para></summary>
    static void CheckHasKids(Transform t, params string[] namesThenMsg)
    {
        if (namesThenMsg == null || namesThenMsg.Length == 0) return;
        string msg = namesThenMsg[namesThenMsg.Length - 1];
        var want = new List<string>();
        for (int i = 0; i < namesThenMsg.Length - 1; i++) want.Add(namesThenMsg[i]);
        var got = new List<string>(KidNameArr(t));
        var miss = new List<string>(); var extra = new List<string>(got);
        foreach (var w in want) { if (extra.Contains(w)) extra.Remove(w); else miss.Add(w); }
        bool ok = t != null && miss.Count == 0 && extra.Count == 0;
        CheckTrue(ok, $"{msg}（实测 [{(t == null ? "节点不在" : string.Join("|", got.ToArray()))}]"
                     + (miss.Count > 0 ? $" · 缺 [{string.Join("|", miss.ToArray())}]" : "")
                     + (extra.Count > 0 ? $" · 多 [{string.Join("|", extra.ToArray())}]" : "") + "）");
    }

    /// <summary>🆕 **A34-F4：字号标尺**。`Label` 没有「我传进去的是多少 px」这个读口 ——
    /// `Label.FontSize` 是 TMP 自己的量纲、而且 `SetAutoFitBox` 之后会被自适应改掉；
    /// 能拿到**标称值**的只有 `Label.DumpSizes()` 里的 `fontSize=`（= `TmpFontSize()`，
    /// 源码注释明写「**不含自适应结果**」）。
    /// ⇒ 用它当读口，并**用同一个读口建一条参考 label**，把「原版 `m_fontSize` 的字面量」翻成同一个量纲。
    /// ⚠️ 参考条摆在**屏外**（不会进实拍），文字/框与原版 `Timer Text` 一致。
    /// 返回值 &lt;= 0 ⇒ 读口坏了 / label 没建出来（**下面那些断言会红，不会静默**）。</summary>
    static float RulerFontSize(Transform host, float fontPx)
    {
        if (host == null) return -1f;
        var r = new PxRect(-9000f, -5000f, -8778.44f, -4971f);      // 宽 = 原版 `Timer Text` 的 221.56
        // 🔴 **2026-10-16（A799 · 夹具现核）：这一句【不会】新裁 —— ⛔ 别把它当成「漏补的 A799 站点」。**
        //   R2 全量表逐条解过父链（`资料/普查产出_1015/R2_A799全量表.md` §二 的 `Editor/ShopScene.cs:512` 行，
        //   置信度「中」）⇒ 判 **不会**；本写手现核同结论：`host` 的唯一实参来源是下面那句
        //   `_offerRuler = new GameObject("OfferRulers_A34F4").transform`（6 个调用点一律 `RulerFontSize(_offerRuler, …)`）
        //   ⇒ **独立根、父链上不可能有 `ViewportClip`** ⇒ A781 那一刀落 `Resolve` 第 3 支 ⇒ **逐位不变**
        //   （标尺要的正是「建出来就量」，多一刀反而会动它的 `textBounds`）。
        var lb = MenuDraw.Text(host, r, "5d 20h 15m", Color.white, "FontRuler_" + fontPx, fontPx, 3000);
        return lb != null ? NominalFontSize(lb) : -1f;
    }

    /// <summary>从 `Label.DumpSizes()` 里读 `fontSize=`（**标称值**，不是自适应之后的）。
    /// 读不到 ⇒ −1（断言会红）。</summary>
    static float NominalFontSize(Label lb)
    {
        if (lb == null) return -1f;
        string s = lb.DumpSizes();
        int i = s.IndexOf("fontSize=");
        if (i < 0) return -1f;
        i += "fontSize=".Length;
        int j = s.IndexOf(' ', i);
        if (j < 0) j = s.Length;
        float f;
        return float.TryParse(s.Substring(i, j - i), System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out f) ? f : -1f;
    }

    /// <summary>某条路径上的 `Label`（`A/B/C`；找不到 ⇒ `null`）。</summary>
    static Label LabelAt(Transform root, string path)
    {
        var t = FindPath(root, path);
        return t != null ? t.GetComponentInChildren<Label>() : null;
    }

    static bool ColorEq(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 1e-3f && Mathf.Abs(a.g - b.g) < 1e-3f && Mathf.Abs(a.b - b.b) < 1e-3f;
    }

    /// <summary>一棵子树里第一个 `ImageQuad` 的渲染队列（量「分层」用）；没有 ⇒ −1。</summary>
    static int QOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        return q != null ? q.RenderQueue : -1;
    }

    /// <summary>🆕 A34-F3：**九宫格**那一件的渲染矩形 —— 取整棵子树里**所有** `ImageQuad` 的并集。
    /// ⚠️ 不能拿 `CheckRectPx`：它只取 `GetComponentInChildren` 的**第一张**，九宫格的第一张是**角块**
    ///    （`Highlight` 那张 `border 52 ÷ ppuMul 2` = **26px**）⇒ 会拿 26 去比 120.5，**假红**。</summary>
    static void CheckRectPxNine(Transform root, float x1, float x2, float y1, float y2, string what)
    {
        var qs = root != null ? root.GetComponentsInChildren<ImageQuad>() : null;
        if (qs == null || qs.Length == 0) { CheckTrue(false, what + "（一整棵里没有 `ImageQuad`）"); return; }
        float ax1 = float.MaxValue, ay1 = float.MaxValue, ax2 = float.MinValue, ay2 = float.MinValue;
        for (int i = 0; i < qs.Length; i++)
        {
            if (qs[i] == null) continue;
            float cx = PxOf(qs[i].transform.position.x), cy = PxYOf(qs[i].transform.position.y);
            float w = qs[i].WorldW * 108f, h = qs[i].WorldH * 108f;
            ax1 = Mathf.Min(ax1, cx - w * 0.5f); ax2 = Mathf.Max(ax2, cx + w * 0.5f);
            ay1 = Mathf.Min(ay1, cy - h * 0.5f); ay2 = Mathf.Max(ay2, cy + h * 0.5f);
        }
        CheckTrue(qs.Length == 9, what + $"（九宫格应该是 **9** 块，实得 {qs.Length}）");
        CheckNear(ax2 - ax1, x2 - x1, 2.0f, what + " 宽(px)");
        CheckNear(ay2 - ay1, y2 - y1, 2.0f, what + " 高(px)");
        CheckNear((ax1 + ax2) * 0.5f, (x1 + x2) * 0.5f, 1.0f, what + " 中心 x");
        CheckNear((ay1 + ay2) * 0.5f, (y1 + y2) * 0.5f, 1.0f, what + " 中心 y");
    }

    /// <summary>🆕 **2026-10-05（A50⑧）**：一个九宫格 `… Gfx` 那一棵的**三列宽**（画布 px）
    /// —— 从**画出来的** `ImageQuad` 读（`WorldW` 是本工程的渲染真值，⛔ **不是**回读传进去的
    /// `borderOutPx` 那种入参，否则就是自证）。
    /// 口径：横向恒 **3 列**（`CreateNineSlice` 的 `i = 0/1/2`）⇒ 三列左边缘各不同，
    /// `wl = 第2列左 − 第1列左`、`wm = 第3列左 − 第2列左`、`wr = 最右边缘 − 第3列左`。
    /// 列数不是 3（或一棵里没有 quad）⇒ 返回 false，调用方自己红。</summary>
    static bool NineColsPx(Transform gfx, out float wl, out float wm, out float wr)
    {
        wl = wm = wr = -1f;
        if (gfx == null) return false;
        var qs = gfx.GetComponentsInChildren<ImageQuad>();
        if (qs == null || qs.Length == 0) return false;
        var edge = new List<float>();
        float right = float.MinValue;
        for (int i = 0; i < qs.Length; i++)
        {
            var q = qs[i];
            if (q == null) continue;
            float l = (q.transform.localPosition.x - q.WorldW * 0.5f) * 108f;
            float r = (q.transform.localPosition.x + q.WorldW * 0.5f) * 108f;
            if (r > right) right = r;
            bool dup = false;
            for (int k = 0; k < edge.Count; k++) if (Mathf.Abs(edge[k] - l) < 0.5f) { dup = true; break; }
            if (!dup) edge.Add(l);
        }
        if (edge.Count != 3) return false;
        edge.Sort();
        wl = edge[1] - edge[0];
        wm = edge[2] - edge[1];
        wr = right - edge[2];
        return true;
    }

    /// <summary>🆕 **2026-10-04（W1-4）**：一棵节点的**直接孩子名数组** —— 给那几个「总数」断言当
    /// **实得值**读口（期望值在 `VExpAll` 那张字面量表里）。
    /// 🔴 那 4 个总数（`slots` / `offSlots` / `bgSlots` / `bgKids4`）原来**两边同源**（都从 `VExpAll`
    /// 的串里 `Split` 出来）⇒ 常量比常量、**改坏实现永不红**；这里补上「从建出来的树数」那一半。
    /// ⚠️ **过滤口径与 <see cref="KidNames"/> 完全一致**：`… Gfx` 那些子件不计入（那是我们这套
    /// `MenuDraw` 的产物，原版那棵树里没有）；**不带** `*` 前缀（INACT 另有 <see cref="KidOffCount"/>）。</summary>
    static string[] KidNameArr(Transform t)
    {
        if (t == null) return new string[0];
        var l = new List<string>();
        for (int k = 0; k < t.childCount; k++)
        {
            var ch = t.GetChild(k);
            if (ch.name.EndsWith(" Gfx")) continue;
            l.Add(ch.name);
        }
        return l.ToArray();
    }

    /// <summary>一棵节点的直接孩子里**出厂关着**（`activeSelf = false`）的个数。
    /// 同样滤掉 `… Gfx`（口径见 <see cref="KidNameArr"/>）—— 给「出厂 INACT 合计」那条当实得值。</summary>
    static int KidOffCount(Transform t)
    {
        if (t == null) return 0;
        int n = 0;
        for (int k = 0; k < t.childCount; k++)
        {
            var ch = t.GetChild(k);
            if (ch.name.EndsWith(" Gfx")) continue;
            if (!ch.gameObject.activeSelf) n++;
        }
        return n;
    }

    static void Section(string t) { Debug.Log(P + $"--- {t} ---"); }

    // ============================================================ 🆕 A327：两态夹具（一条共用 · 四份【函数体】逐字同源）
    //
    // 🔴 **为什么要它**：2026-10-11（W4）把「世界 → 设计」那一族（`MenuDraw.PosInDesignSpace` / 各窗的
    //   `Local`·`Local3` / `CampaignTab.BuildLine` / `ShopWindow.BuildTimeCounter` / `CampaignTab.BuildArmyItems`）
    //   修完之后发现：**`k == 1`（小屏缩放开关出厂关着）时新旧两式逐位相同** ⇒ 那些处是**潜伏缺陷**
    //   —— **今天一条现有断言都不会红**（不是「有断言挡着」，是**还没有断言**）。
    //   ⚠️ **2026-10-11（FX3）收窄一处口径**：其中**基准恰好就是窗根**的那几处，新旧两式在生产里
    //   **永远**逐位相同（`basis == 窗根` ⇒ 除的是 Holder，恒单位缩放）⇒ 是 **no-op**，不是「潜伏」。
    //   判据 / 逐处清单 / 「该断言什么」→ `资料/普查产出_1011/W4_子3.md` §四·b。
    //
    // 🔴 **四份副本的【函数体】逐字同源**（文件头各记本文件的调用形状）：`Editor/ShellScene.cs` · `Editor/CollectionScene.cs` · `Editor/RewardsScene.cs` · 本文件。
    //   ⛔ **改一份就得改四份**（铁律 6：同一件事两套口径 = 迟早不一致）—— **2026-10-11（A350）**
    //   就是把本处与 `RewardsScene` 那两份**从旧口径同步过来的**（此前只有 Shell/Collection 两份是新口径）。
    //
    // 🔴 **夹具形状**（判据给的就是这一条，⛔ 别另设计一套）：
    //   ① 态一 = 开关**关**（出厂态）⇒ 量一次 → `p1`；② 态二 = 开关**开** + **被乘的那一级**乘 M（走**生产那条路**
    //   `TransformScalerBySmallScreenUI`：`SetScale(M)` + `Tick()`，批处理没有帧循环）⇒ 再量同一个对象 → `p2`；
    //   ③ 断 **`p2 == M × p1`**（⛔ **一个我们自己的常量都不读** —— 只读 M）。
    //
    // 🔴 **2026-10-11（FX3）三处订正 —— 上一版夹具【自己把这条恒等式砸了】**（Shell 4 + Collection 4 条红；
    //   判据全文 → `资料/普查产出_1011/DIAG-A_Shell与Collection八条红.md`）：
    //   ① **M 加在【基准的父级】那一级**，⛔ **不是基准自己** —— `PosInDesignSpace` 除的正是
    //      `t.parent.lossyScale`（`Shell/MenuDraw.cs:74-78`）；那一级是单位缩放时新旧两式**逐位相同**。
    //   ② **可观测余量** = 「**基准相对被乘那一级的位移** ≥1 设计单位」（⛔ 不是「离**世界原点**」——
    //      上一版量的就是后者，所以它逼着调用方去挪窗根、又把恒等式砸了）。坏式与好式相差
    //      `(1−M)×|基准在态二的世界位置|` ≈ `0.2×|基准|`，容差 **0.02 单位（2.2px）** ⇒ 位移 ≥ 1 时
    //      偏差 ≥ 0.24 单位 = **26px**，远远超出容差 ⇒ 真会红。
    //   ③ **态二的 `measure` 里必须【重建】**（`Open()`/`Setup()`/`RefreshNodes()` 首句都清子件）—— 不重建时
    //      被量的局部位置是 `k == 1` 那一趟**冻结**下来的值，新旧两式在那时**逐位相同** ⇒ 断言恒真（= 假绿）。
    //      带牙口的判据 → `资料/普查产出_1011/W4_子3.md:91-106`；同族先例 = `Editor/ShopScene.cs` 的 **A294** 那一段（同文件的两态探针）。
    // 🔴 **四份副本的调用形状【分两族】**：Shell/Collection 那四个调用点参数1 = **窗根的父级探针根**；
    //   本文件与 `RewardsScene` 的调用点参数1 = **窗根自己**（`win.gameObject`），`basis` 是**窗根的子件**。
    //   两族都满足「参数1 那一级被乘 M」+「参数1 那一级就是 `basis.parent`」⇒ 前提①② 照样成立。
    // ⚠️ **态二会把那一级乘 M 再还原**（`localScale` 放回 1 · 组件销毁 · 开关放回关）—— 直线写法，没有提前 return。
    static void CheckScaleTwo(GameObject scaleRoot, Transform basis, System.Func<Vector3> measure, float m, string what)
    {
        CheckTrue(scaleRoot != null && basis != null && measure != null, "（前提）" + what + "：夹具的件齐了");
        if (scaleRoot == null || basis == null || measure == null) return;
        SmallScreenUI.Set(false);                              // 态一：开关**关**（出厂态）
        Vector3 p1 = measure();
        Vector3 b1 = basis.position;                           // 态一的基准位置 = 它的**设计**位置（k == 1）
        Vector3 w1 = scaleRoot.transform.position;             // 被乘那一级的位置（态一；生产里 = 原点）
        // （前提②·可观测余量）**基准相对被乘那一级的位移** ≥1 设计单位 —— 基准落在那一级的原点上时
        // 「除不除缩放」两式**恒等** ⇒ 断言「什么都不中」也全绿。⛔ 上一版量的是「离**世界原点**」，
        // 逼着调用方去挪窗根、又把恒等式砸了（见本段文件头 ①②）。
        CheckTrue(Mathf.Abs(b1.x - w1.x) > 1f || Mathf.Abs(b1.y - w1.y) > 1f,
                  $"（前提）{what}：**基准相对被乘 M 那一级的位移 ≥1 设计单位**（实测 {b1.x - w1.x:F2},{b1.y - w1.y:F2}）"
                + " —— 位移≈0 时「除不除缩放」两式恒等 ⇒ 这一条会退化成假绿");
        CheckNear(scaleRoot.transform.localScale.x, 1f, 1e-4f, "（前提）" + what + "：态一那一级没被谁乘过");
        SmallScreenUI.Set(true);                               // 态二：开关**开** + 那一级乘 M
        var sc = scaleRoot.GetComponent<TransformScalerBySmallScreenUI>();
        if (sc == null) sc = scaleRoot.AddComponent<TransformScalerBySmallScreenUI>();
        sc.SetScale(m);
        sc.Tick();                                            // 批处理没有帧循环 ⇒ 手动推一次
        CheckNear(scaleRoot.transform.localScale.x, m, 1e-4f,
                  "（前提）" + what + "：态二那一级 `localScale` = M（真走的生产那条路）");
        // （前提①）**被除的那一级真的被乘了 M** —— `PosInDesignSpace` 除的是 `basis.parent.lossyScale`；
        // 那一级是单位缩放时新旧两式**逐位相同** ⇒ 下面那条 ★ 等于没查。改坏法：M 仍加在 `basis` 自己身上
        // （= 上一版那种塞法）⇒ 这条红。
        CheckNear(basis.parent != null ? basis.parent.lossyScale.x : 1f, m, 1e-3f,
                  "（前提）" + what + "：**基准的【父级】在态二被乘了 M**（`PosInDesignSpace` 除的正是这一级，"
                + "`Shell/MenuDraw.cs:74-78`）—— 父级单位缩放时新旧两式**恒等**，这条断言就等于没查");
        Vector3 p2 = measure();
        CheckNear(p2.x, m * p1.x, 0.02f,
                  $"★ {what}：**态二 == M × 态一**（x：{p2.x:F3} vs {m:F2}×{p1.x:F3}）"
                + " —— 两态合起来才证明「这一处的落位真的跟着被乘 M 的那一级缩放走」"
                + "（`k == 1` 时新旧两式逐位相同 ⇒ 只断态一的话，改坏了照样绿）");
        CheckNear(p2.y, m * p1.y, 0.02f, "★ " + what + "：……y 分量同理（只改 x 不改 y 时只有上一条红）");
        Object.DestroyImmediate(sc);                           // 还原
        scaleRoot.transform.localScale = Vector3.one;
        SmallScreenUI.Set(false);
        CheckNear(measure().x, p1.x, 0.02f, "（收尾）" + what + "：那一级放回 1 之后位置也回到态一那一份");
    }

    /// <summary>🆕 **A327 · A298 探针用**：一棵（九宫格）子树里**所有活着的 `ImageQuad` 的并集**，
    /// 换算回**设计 px**。
    /// <para>每块的矩形 = 世界位置 ± 「几何尺寸 × **父链缩放**」（`WorldW/H` 只是**建它时传进去的那个数**、
    /// 不含父链缩放 —— 判据见 `Shell/SettingsWindow.cs` 的 `Screen()` 那条订正），再按
    /// `c + (v − c)/m` 除回设计 px（窗根乘 `m` 时，设计点 `d` 渲出来在 `m·d`；`c` = 画布中心）。
    /// ⛔ **不读被测实现的任何常量**（`m` 由调用方给、`c` 是画布中心、尺寸是几何量）。
    /// ⚠️ 九宫格那 9 块**无缝铺满整块**，所以「活着那几块的并集」就是「画出来的那一块」——
    /// 全在框内 ⇒ 并集 = 原矩形；部分越界 ⇒ 并集 = `R ∩ clip`。</para></summary>
    static PxRect NineUnionPx(Transform root, float m)
    {
        const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
        float cx = LayoutSpace.DesignPxW * 0.5f, cy = LayoutSpace.DesignPxH * 0.5f;
        float x1 = float.MaxValue, y1 = float.MaxValue, x2 = float.MinValue, y2 = float.MinValue;
        int n = 0;
        foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
        {
            if (q == null || !q.gameObject.activeInHierarchy) continue;
            var p = q.transform.position; var ls = q.transform.lossyScale;
            float hw = Mathf.Abs(q.WorldW * Mathf.Abs(ls.x)) * K * 0.5f, hh = Mathf.Abs(q.WorldH * Mathf.Abs(ls.y)) * K * 0.5f;
            float wx = LayoutSpace.PxX(p.x), wy = LayoutSpace.PxY(p.y);
            x1 = Mathf.Min(x1, cx + (wx - hw - cx) / m); x2 = Mathf.Max(x2, cx + (wx + hw - cx) / m);
            y1 = Mathf.Min(y1, cy + (wy - hh - cy) / m); y2 = Mathf.Max(y2, cy + (wy + hh - cy) / m);
            n++;
        }
        return n > 0 ? new PxRect(x1, y1, x2, y2) : default(PxRect);
    }

    static void Check<T>(T got, T want, string msg)
    {
        if (EqualityComparer<T>.Default.Equals(got, want)) { _pass++; Debug.Log(P + $"   ✓ {msg}"); }
        else
        {
            _fail++;
            var line = $"{msg} —— 期望 [{want}]，实得 [{got}]";
            _failures.Add(line);
            Debug.LogError(P + $"   ✗ {line}");
        }
    }

    static void CheckTrue(bool c, string msg) { Check(c, true, msg); }

    /// <summary>🆕 A17：把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
    /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
    static void CheckHoverSwap(Transform root, string what)
    {
        int n; string bad = WindowButton.AuditHoverSwap(root, out n);
        CheckTrue(n > 0, what + "：**确实有**接了悬停换图的按钮（n=" + n + "，否则这条等于没查）");
        if (bad.Length > 0) CheckTrue(false, what + "：换图要「悬停换得动 + 离开还原得回」—— " + bad);
    }

    /// <summary>🆕 **2026-10-05（A50②）**：**按下**换图那一路的审计（`CheckHoverSwap` 的镜像）——
    /// 这条**全工程此前一条都没有**（`grep "\.Press()" Editor/` = 0 命中），而原版那一档
    /// （`m_SpriteState.m_PressedSprite`）在 `trans=2` 的 630 颗上**全非空**。
    /// 判据与「为什么不算自证」→ `WindowButton.AuditPressedSwap` 的注释。
    /// ⚠️ 直调 `Press/Release` **不会派发 `onClick`**（那要 `PointerLayer` 判「按下与抬起同一件」）。</summary>
    static void CheckPressedSwap(Transform root, string what)
    {
        int n; string bad = WindowButton.AuditPressedSwap(root, out n);
        CheckTrue(n > 0, what + "：**确实有**能按下换图的按钮（n=" + n + "，否则这条等于没查）");
        if (bad.Length > 0) CheckTrue(false, what + "：按下要「换得动 + 松开还原得回」—— " + bad);
    }

    static void CheckNoMissingSwapArt(string what)
        => CheckTrue(WindowButton.MissingSwapArt.Count == 0,
                     what + "：**悬停图一张都不缺**（缺的会列在这里：" + string.Join("、", WindowButton.MissingSwapArt.ToArray()) + "）");

    /// <summary>🆕 **2026-10-15（A810①）**：「**窗自己的 `MissingArt`**」这条判据的**唯一一份** ——
    /// **0 才绿**（这几十件图在原版本来就有、`Resources/Art/` 里也在；缺一张 ⇒ 那一件**根本没画出来**）。
    /// <para>🔴 本助手是 **A810①「缺图三套口径收口」的落点**：本文件里同一条规矩原先**在 5 处各写了一遍**
    /// （`bp` / `gop` / `ppw` / `rre` / `rp`），措辞与「失败时列不列名字」各不相同 ⇒ 现在只此一份
    /// （CLAUDE.md「两处写同一条规则 = 迟早不一致」）。</para>
    /// <para>⚠️ **它与 `CheckNoMissingSwapArt` 是两条池子、⛔ 不许互相顶替**：这一份记的是
    /// **主图取不到 ⇒ 这一件没画**（`MenuWindowBase.Art()` 记）；那一份记的是
    /// **悬停图取不到 ⇒ 悬停换不动**（`WindowButton.Bind` 记）。两者都「0 才绿」，取自两个不同的表。</para>
    /// <para>⚠️ **更不许把「按下图」那一池（`WindowButton.MissingPressedArt`）也收进来**：原版只有
    /// `m_Transition = 2`（SpriteSwap）那 630 颗有按下图（逐颗实测非空）、`trans=1/0` 的**本来就没有**
    /// ⇒ 我们取不到时退回高亮图**是合法的** ⇒ 那一池**只出声、不当缺点断**
    /// （口径与判据 → 本文件 `CheckNoMissingSwapArt` 调用点上面那一段注释）。</para>
    /// <para>🔴 **失败时把名字列出来**（照 `MissingArt` 的约定；A810① 之前只有 `bp` 那一处列）——
    /// `A808` 记着：`Resources/Art/ui_menu/` 下那十几张手拷图**没有任何导入器登记** ⇒ 谁跑一次
    /// `工具/import_original_art.py`（或删 `Resources/Art/`），这几条会**静默翻红**：届时先看这份名单。</para></summary>
    static void CheckNoMissingArt(List<string> miss, string what)
        => CheckTrue(miss != null && miss.Count == 0,
                     what + "：**一张图都不缺**（缺的会列在这里："
                     + (miss == null ? "`MissingArt` 表本身是 null" : string.Join("、", miss.ToArray())) + "）");

    static void CheckNear(float got, float want, float tol, string msg)
        => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F2} ≈ {want:F2}±{tol:F2}）");

    /// <summary>🆕 **§A251-L3（2026-10-13）**：`marker` 那 4 份 `Outline` 副本的 tint 与给定色比（容差 1e-3）。
    /// 收的是**整组**（4 份必须同色 —— 原版刷的就是同一个 `Outline.effectColor`）。
    /// 读到 `null` / 不是 4 份 ⇒ **直接返回 false（红）**，⛔ 不让「没建出来」被当成「颜色对」
    /// （那正是本工程「弱断言分不出两种状态」要挡的形状）。</summary>
    static bool ColNear(ImageQuad[] arr, float r, float g, float b)
    {
        if (arr == null || arr.Length != 4) return false;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) return false;
            var c = arr[i].Tint;
            if (Mathf.Abs(c.r - r) > 1e-3f || Mathf.Abs(c.g - g) > 1e-3f || Mathf.Abs(c.b - b) > 1e-3f)
                return false;
        }
        return true;
    }

    /// <summary>🔴 **2026-10-14**：**左对齐**的文字那一档 —— 渲染块的**左沿**落在原版矩形的 `x1` 上、
    /// 垂直仍居中。
    /// <para>**为什么另起一条**：原版这几颗 TMP 的 `m_HorizontalAlignment = 1 (Left)`，我们走
    /// `MenuDraw.AlignLeft` ⇒ 节点的**中心**当然**不在**框中心（`AlignLeftOn` 是「把块的左沿摆到 `x1`」）
    /// ⇒ 拿 `CheckAt`（断中心）量它**永远红**。本批实测：`Title` 差 132.67px，正是
    /// `(框宽 − 文字宽)/2` 那一跳。⚠️ 这两条断言在 A803 修好之前**从未真正执行过**（当时整棵树不存在）。</para>
    /// <para>`Left` = `节点.x − Label.WorldW / 2`（= 工程里「左沿」的**唯一口径**，同 `Label.AlignLeftOn`）。</para></summary>
    static void CheckLeftAt(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var lb = t.GetComponent<Label>();
        if (lb == null)
        {
            CheckTrue(false, what + "（节点上没有 `Label` ⇒ 量不出渲染宽度，⛔ 不拿框宽顶替）");
            return;
        }
        var want = LayoutSpace.RectCenter(x1, y1, x2, y2);
        float wantLeft = LayoutSpace.FromPixel(x1, 0f).x;
        float gotLeft = t.position.x - lb.WorldW * 0.5f;
        float dLeft = Mathf.Abs(gotLeft - wantLeft);
        float dY = Mathf.Abs(t.position.y - want.y);
        CheckTrue(dLeft <= 0.01f && dY <= 0.01f,
                  $"{what} **左对齐**到原版矩形（左沿差 {dLeft * 108f:F2}px · 竖向中心差 {dY * 108f:F2}px）"
                + $" —— 实得左沿 {gotLeft * 108f:F2}px / 中心 {t.position.x * 108f:F2},{t.position.y * 108f:F2}px；"
                + $"期望左沿 {wantLeft * 108f:F2}px / 中心 {want.x * 108f:F2},{want.y * 108f:F2}px");
    }

    /// <summary>节点**在世界里的位置**要落在原版像素矩形的中心。
    /// 🔴 **2026-10-14**：失败文案里**带上实得坐标与期望坐标** —— 原来只打一个距离，
    /// 出了红要判「是没建、建歪了、还是被对齐挪走了」时**没法一次说清**（本批实测就踩了这条）。</summary>
    static void CheckAt(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var want = LayoutSpace.RectCenter(x1, y1, x2, y2);
        float d = Vector3.Distance(t.position, want);
        CheckTrue(d <= 0.01f, $"{what} 在原版矩形中心（差 {d:F4} 世界单位 = {d * 108f:F2}px）"
                           + $" —— 实得 {t.position.x:F2},{t.position.y:F2} 世界 / "
                           + $"{t.position.x * 108f:F2},{t.position.y * 108f:F2} px；"
                           + $"期望 {want.x:F2},{want.y:F2} 世界 / {want.x * 108f:F2},{want.y * 108f:F2} px");
    }

    /// <summary>🆕 **2026-10-04（A47 接线批）**：压暗层（「点窗外关窗」）命中区那条不变量。
    /// 🔴 **2026-10-07（A77⑬⑥）本文件里的副本已删** —— 唯一一份在 `MenuDraw.CheckShadeRule`。
    /// ⛔ 别在本文件里再长回来：调用点一律写 `MenuDraw.CheckShadeRule(CheckTrue, …)`。
    /// <para>🔴 **2026-10-07（A77⑬③）那条判据的期望值也换了**：不再比「调用方传进来的常量」
    /// （与 `ShadeHit` 的实参同一个符号 = 同义反复），改成**量同一扇窗里「视觉压暗层」那颗 quad 的
    /// `RenderQueue`**。🔴 **为什么仍要问 `WasShadeHit`**：档本来就对的那几扇窗，走不走公共件
    /// **没有任何可见行为差异** ⇒ 只有那一句能分出两种状态（改回自己那份 `MenuDraw.Hit` 就红）。</para></summary>

    /// <summary>🆕 **2026-10-15（A825）：本文件原来那一份 `CheckAbsorbRule` 已【收口】——
    /// 唯一一份实现在 `MenuDraw.CheckAbsorbRule`。**</summary>
    /// <para>**签名与 26 个调用点一个字都没动**（本包装的形参表与原来那份逐字相同）；
    /// 「点哪儿 / 为什么钉死 (5,5) / 六步各查什么」的判据全文 → `Shell/MenuDraw.cs` 的 `CheckAbsorbRule`
    /// （⛔ 别在本文件里再抄第二份）。</para>
    /// <para>本文件原来那份里读过的 `EdgeInset` 常量表随函数一起搬进 `MenuDraw.AbsorbEdgeInset`
    /// （本文件那一份**只被这一处读**，2026-10-15 实读）。理由 = 「**两处写同一条规则 = 迟早不一致**」
    /// （`CLAUDE.md` §三）—— 与 `CheckShadeRule`（2026-10-07 · A77⑬⑥）同族。</para>
    static void CheckAbsorbRule(string what, Transform winRoot, string nodeName,
                                float x1, float y1, float x2, float y2,
                                int qShade, int qContentMin, System.Func<WindowState> state)
    {
        MenuDraw.CheckAbsorbRule(CheckTrue, CheckNear, what, winRoot, nodeName,
                                 x1, y1, x2, y2, qShade, qContentMin, state);
    }

    /// <summary>一张图**渲出来的像素矩形**（`WorldW/H` = 渲染真值，不是回读我们传进去的数）。
    /// ⚠️ 只取 `GetComponentInChildren` 的**第一张** quad ⇒ **一块被切成几格时会量到其中一格**
    /// （软边那把刀 —— 见 <see cref="CheckRectPxUnion"/>）。该用并集的地方别用这一条。</summary>
    static void CheckRectPx(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
        CheckNear(q.WorldW * 108f, x2 - x1, 2.0f, what + " 宽(px)");
        CheckNear(q.WorldH * 108f, y2 - y1, 2.0f, what + " 高(px)");
    }

    /// <summary>🆕 **2026-10-04（W2a 软边接线）**：一棵子树里**所有 `ImageQuad` 的并集矩形**
    /// —— 同 `CheckRectPxNine` 的口径，但**块数不定**（软边那把刀切几块都行）。
    /// 🔴 **为什么必须有它**：`MenuDraw.ApplySoftEdges` 会把一块**沿渐隐带的内沿切开**（主格留在原节点、
    /// 其余格建成子 quad）⇒ `CheckRectPx` / <see cref="RectOf"/> 取的「第一张」只是**其中一格** ⇒ **假红**。
    /// （实测：商店格底第 1 行报 458.00、期望 477.0；第 2 行格报 446.38、期望 471.38；滚到底的主格报 451、期望 477。）
    /// ⚠️ **只算 `activeSelf` 的块**（同 `CheckRectPxNine`）：整块在裁切框外的格会被 `SetActive(false)`，
    /// 把它们算进并集会**把并集撑回未裁切的大小**（那也是假的）。
    /// ⚠️ 软边那条路上**块与块之间不重叠**（是切分、不是叠图）⇒ 把并集当「渲出来的矩形」是成立的。</summary>
    static bool RectOfUnion(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        var qs = t != null ? t.GetComponentsInChildren<ImageQuad>() : null;
        if (qs == null || qs.Length == 0) return false;
        x1 = float.MaxValue; y1 = float.MaxValue; x2 = float.MinValue; y2 = float.MinValue;
        int n = 0;
        for (int i = 0; i < qs.Length; i++)
        {
            if (qs[i] == null) continue;
            float cx = PxOf(qs[i].transform.position.x), cy = PxYOf(qs[i].transform.position.y);
            float w = qs[i].WorldW * 108f, h = qs[i].WorldH * 108f;
            x1 = Mathf.Min(x1, cx - w * 0.5f); x2 = Mathf.Max(x2, cx + w * 0.5f);
            y1 = Mathf.Min(y1, cy - h * 0.5f); y2 = Mathf.Max(y2, cy + h * 0.5f);
            n++;
        }
        return n > 0;
    }

    /// <summary>并集版的 `CheckRectPx`（见 <see cref="RectOfUnion"/>）。</summary>
    static void CheckRectPxUnion(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        float ax1, ay1, ax2, ay2;
        if (!RectOfUnion(t, out ax1, out ay1, out ax2, out ay2))
        {
            CheckTrue(false, what + "（一整棵里没有 `ImageQuad`）");
            return;
        }
        CheckNear(ax2 - ax1, x2 - x1, 2.0f, what + " 宽(px)");
        CheckNear(ay2 - ay1, y2 - y1, 2.0f, what + " 高(px)");
    }

    static void CheckArt(Transform t, string want, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        CheckTrue(q != null && q.Texture != null && q.Texture.name == want, $"{what} = `{want}`");
    }

    static Color TintOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) return new Color(0f, 0f, 0f, 0f);
        var mr = q.GetComponent<MeshRenderer>();
        return mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.color : new Color(0f, 0f, 0f, 0f);
    }

    static string TextOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.Text : null;
    }

    /// <summary>一个件**渲出来**的像素矩形（画布像素 · 左上原点 · y 向下）。
    /// 图走 `ImageQuad.WorldW/H`（**材质的真测量**，不是回读常量）、字走 **TMP 自己渲出来那块 `textBounds`**
    /// （🆕 2026-10-15 · **A796′ 换口** —— 见体内那段注释与 <see cref="TmpRenderedRect"/>）。
    /// 🔴 取的是**组件自己的 transform** —— `AlignLeft/Right` 会把 `Label` 的节点挪走，
    ///    拿外层容器的位置去算就会偏（本工程踩过「字飘走了而矩形断言全绿」）。
    /// ⚠️ 图那一支同样只取**第一张** `ImageQuad` ⇒ **一块被软边切成几格时只量到其中一格**；
    ///    那种场合要用 <see cref="RectOfUnion"/>（见 <see cref="CheckRectPxUnion"/>）。</summary>
    static bool RectOf(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        if (t == null) return false;
        Transform node = t; float w, h;
        var lb = t.GetComponentInChildren<Label>();
        var q = t.GetComponentInChildren<ImageQuad>();
        if (lb != null)
        {
            node = lb.transform;
            // 🔴 **2026-10-15（A796′）换口**：宽/高不再读**字段缓存** `Label.WorldW/H`（= `_tmpW/_tmpH`，
            //    只有 `RefreshBounds()` 写 —— 也就是**被测实现自己**），改读 TMP 自己渲出来那块
            //    `textBounds`（= `TmpRenderedRect` 那条口；落地口径 → `资料/普查产出_1014/RO_缓存口径与输入三件.md` §一·3）。
            //    **为什么这是灭自证**：`SetFontSize` / `SetCharSpacing` 这一族（`Battle/Label.cs`，现读 `:601`/`:631`）
            //    **只重排 mesh、不刷新缓存** ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**、断言照样绿。
            //    ⛔ **别换成「TMP 网格顶点」那条口**（实测首字左边距 2.22px ⇒ 一批期望值会集体偏 1–2px）。
            //    ⚠️ **只换「从哪个口读那个数」**：中心仍是**节点位置**（`AlignLeft/Right` 把节点挪走的口径
            //       一个字没动）。若改成直接用 `textBounds` 那块矩形，矩形会跟着**两项**整体挪 ——
            //       TMP 子节点上的 `_vOffset`（A712 字墨校正，`Battle/Label.cs` 的 `RefreshBounds` 里那句）
            //       与 `(0.5 − anchor)` 那一项 —— 两者今天**都不保证为 0** ⇒ 期望值会集体漂（那就不是「换口」了）。
            //    ⚠️ 量不到（点阵后端 / 底下没有 TMP 网格）⇒ **退回旧口**：那条路**本来就没有** `textBounds`
            //       （`Label.WorldW` 的点阵支读的是 `_texW`），不是「静默吞掉新口」。
            float rx1, ry1, rx2, ry2;
            if (TmpRenderedRect(node, out rx1, out ry1, out rx2, out ry2)) { w = rx2 - rx1; h = ry2 - ry1; }
            else { w = lb.WorldW * 108f; h = lb.WorldH * 108f; }
        }
        else if (q != null) { node = q.transform; w = q.WorldW * 108f; h = q.WorldH * 108f; }
        else return false;
        float cx = PxOf(node.position.x), cy = PxYOf(node.position.y);
        x1 = cx - w * 0.5f; x2 = cx + w * 0.5f;
        y1 = cy - h * 0.5f; y2 = cy + h * 0.5f;
        return true;
    }

    /// <summary>两个矩形**有没有重叠**（像素矩形的标准 AABB 判据）。</summary>
    static bool Overlaps(float ax1, float ay1, float ax2, float ay2,
                         float bx1, float by1, float bx2, float by2)
    {
        return ax1 < bx2 - 0.5f && bx1 < ax2 - 0.5f && ay1 < by2 - 0.5f && by1 < ay2 - 0.5f;
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>**测试编排**（不是断言）：A7 之后「买一件卡包」会弹出**全屏**的
    /// `Booster Pack Open Window`（`type = 0 Fullscreen` ⇒ `OpenWindowCO` 会把商店关掉），
    /// 后面那些断言/实拍要的是**商店开着**的状态 ⇒ 这里把它关掉、把商店重新开起来。</summary>
    static void ClosePackAndReopenShop(ShopWindow win)
    {
        var pg = win.PageOf(0);
        var bp = pg != null ? pg.LastBoosterPack : null;
        if (bp != null && bp.CurrentState != WindowState.Closed)
        {
            Debug.Log(P + "  （编排）买完弹出的开包窗先关掉，把商店重新开起来");
            bp.Close();
        }
        if (win.CurrentState == WindowState.Closed && win.Manager != null) win.Manager.OpenWindow(win);
        CheckTrue(win.CurrentState == WindowState.Open, "（前提）商店重开之后真的开着 —— 下面几段都靠它");
    }

    /// <summary>世界 x → 画布像素 x（×108 + 960）。⚠️ **只能用在 x 上**。</summary>
    static float PxOf(float worldX) { return worldX * 108f + 960f; }
    /// <summary>世界 y → 画布像素 y。**y 是反的**（像素 y 向下）⇒ `540 − worldY × 108`。
    /// 🔴 拿 `PxOf` 去量 y 会得到**假警报**（本工程踩过，见 `RewardsScene` 的 `PxYOf`）。</summary>
    static float PxYOf(float worldY) { return 540f - worldY * 108f; }

    /// <summary>🆕 **2026-10-13（A750）**：量一段文字 **TMP 自己渲出来那块**的像素矩形
    /// （1920×1080 · 左上原点 · y 向下）—— **这一份才是「字真的从哪开始画」**。
    /// <para>写法与契约**照抄** `Editor/MainMenuScene.cs` 的同名助手（`TmpRenderedRect`，A490/A617 收口的那一份）；
    /// 本文件再留一份，是因为四个自检各自一套辅助函数（`CollectionScene` 开头那条注释已经明记这是一笔明账）。</para>
    /// <para>🔴 **为什么要用它（灭自证）**：`Label.WorldW/WorldH` 读的是**字段缓存** `_tmpW/_tmpH`，
    /// 而那份缓存**只有 `RefreshBounds()` 写**（`Battle/Label.cs`）—— 也就是**被测实现自己**；
    /// 反过来 `SetCharSpacing`（`:524-533`）· `SetFontSize`（`:503-514`）这一族**只重排 mesh、不刷新缓存**
    /// ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**（实现与检测器共用一个口 = 自证）。
    /// 本助手读的是 TMP 自己的 `textBounds`（mesh 的**活值**），**不在实现那条链上**。</para>
    /// <para>⚠️ **取组件带 `true`（含 inactive）** —— 左栏第 4 键是**关着的母版**（`tabButtonPrefab`），
    /// 单参那版只找激活的对象，正好会在这里踩坑。
    /// ⚠️ **量不到时（`t` 不在 / 底下没有 TMP）返回 `false`、四个 out 全 0**（与 `RectOf` 同一契约）
    /// ⇒ 调用方**必须先判它**，否则 `0 ≤ 期望值` 会**假绿**。
    /// ⚠️ TMP 在、但字是**空串**时 `textBounds` 是 TMP 的未定义值（哨兵 **4.29e9**）⇒ 那时报出来的是
    /// **天文数字 = 量法没生效**，⛔ 别照它去改实现。</para></summary>
    static bool TmpRenderedRect(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        if (t == null) return false;
        var tmp = t.GetComponentInChildren<TMPro.TextMeshPro>(true);
        if (tmp == null) return false;
        var b = tmp.textBounds;                       // 局部空间的行盒（`Bounds`）
        var M = tmp.transform.localToWorldMatrix;
        x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue;
        for (int c = 0; c < 4; c++)
        {
            var corner = M.MultiplyPoint3x4(new Vector3((c % 2 == 0) ? b.min.x : b.max.x,
                                                        (c < 2) ? b.min.y : b.max.y, 0f));
            float px = LayoutSpace.PxX(corner.x), py = LayoutSpace.PxY(corner.y);
            x1 = Mathf.Min(x1, px); x2 = Mathf.Max(x2, px);
            y1 = Mathf.Min(y1, py); y2 = Mathf.Max(y2, py);
        }
        return true;
    }

    /// <summary>**按路径**找（`FindChild` 是按名字找的、不认识 `A/B/C` —— 见 `RewardsScene` 里那条注释）。</summary>
    static Transform FindPath(Transform root, string path) { return root != null ? root.Find(path) : null; }

    static int CountByPrefix(Transform root, string prefix)
    {
        if (root == null) return 0;
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(prefix)) n++;
        return n;
    }

    static int CountVisible(Transform root, string prefix)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(prefix) && t.gameObject.activeInHierarchy) n++;
        return n;
    }

    // ============================================================ 场景

    static ShopWindow Build(out Transform root)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.aspect = LayoutSpace.DesignAspect;      // ⚠️ 必须在建任何东西之前定死（批处理默认 4:3）
        LayoutSpace.Apply(cam);

        // 商店的 `windowsPlacement = 10 (World)`（**与奖励窗的 5 Canvas 不同**）
        // 🆕 A7：`Booster Pack Open Window` 是 `windowsPlacement = **5 (Canvas)**`
        //（MB `MonoBehaviour_9012570135841684515.json` 原文）⇒ 本场景也得有那个锚点，
        // 否则 `GetWindowAnchor(Canvas)` 会报「找不到锚点」、窗口建在场景根上（能跑，但那不是原版的挂法）。
        // 🔴 **2026-10-11（A351）就地订正（铁律 5）**：上面这两句原来是**手抄的**（`MakeHolder` 两份 +
        //    `AddComponent<WindowsManager>()`）—— 那套手抄在**批处理（编辑模式）**下有一处硬伤：
        //    · `WindowsManager` **没有 `[ExecuteAlways]`**（`Shell/WindowsManager.cs` 里**只有** `WindowHolder` 那颗**有**）
        //      ⇒ `Awake` 不跑 ⇒ **`Instance` 恒 null**（`Instance` 只在 `Awake` 里赋，
        //      **批处理下那句从不执行**）。判据（**四条独立记录**，全是踩过的坑）：
        //      `Shell/PromptPopup.cs` 的 `WindowButton` 类注 · `Shell/MainMenuRuntime.cs` 里那条「编辑模式下 `Awake` 不跑」 · `Shell/PointerLayer.cs:47-48`
        //      · `资料/已知的坑.md:704`（「编辑模式下 `Awake/OnEnable`/`Update` **只对带 `[ExecuteAlways]`
        //      的脚本**才跑」）。
        //    · ⇒ **任何走 `WindowsManager.EnsureHost()` 的开窗路径都会【再建一台】**（`Instance == null` 时
        //      不查「场景里是不是已经有一台」，直接再建一套管理器 + 锚点）
        //      ⇒ 窗落进**第二台**、而 `wm` 是第一台 ⇒ `wm.openWindows` 里没有它。2026-10-11 那 8 条红就是这个
        //      形状造成的（判据 → `资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §二·#1）；A351 = 剩下几处一起收口。
        //    **最小改法 = 走公共件**（`EnsureHost()` 的文档注释：「壳与「单独打开某个界面场景」**都走它**，
        //      两处各建一次 = 迟早不一致」）。
        //    ⚠️ **与手抄那份有一处【有意的差异】**：手抄只建 `1 - World` / `2 - Canvas` 两颗；
        //      `EnsureHost()` **三颗都建** ⇒ 补上 `3 - PopUp Holder`（原版那三颗 Holder **缺一不可**，
        //      `3 - PopUp Holder{15}` 正在其中）。**已核本文件无副作用**：本场景里**没有**走弹窗档(15)的窗
        //      —— `Booster Info Popup` 的 `type` 是 `Popup`（1）但 `windowsPlacement` 是 **10 (World)**
        //      （本文件 A6 那一段的 `Check(pop.windowsPlacement, 10, …)` 断言就是核它的）⇒ 新锚点挂上去也不会有人用。
        //    ⚠️ 顺带把 `PointerLayer` 的创建时机提前到 `Build()` 那一刻（`EnsureHost` 第一句就是
        //      `PointerLayer.Ensure(root)`）—— **已核：无可观测差异**（两边都是无父的
        //      `new GameObject("Pointer Layer")`；`RegisterScroll` 读的是惰性 getter ⇒ 登记表内容一字不变。
        //      同 `RewardsScene`，见 `资料/普查产出_1011/FX4_Rewards十一条红修复.md` §六·6）。
        //    **改坏法（如实说 —— 今天【照不出来】，它是一笔【去掉地雷】的改动，⛔ 不是「修好了一条会红的断言」）**：
        //      把这一句换回手抄的 `AddComponent<WindowsManager>()` ⇒ `Instance` 又变回 null；而**本自检今天没有**
        //      走 `WindowsManager.EnsureHost()` / `OpenByRef()` 的开窗入口（现场全部是直调 `win.Manager.OpenWindow(...)`）
        //      ⇒ **改坏它，本文件一条断言都不会红**。它的判别力在【将来】：`WindowsManager.OpenByRef()` 的**第一句**
        //      就是 `EnsureHost()` —— 谁在这几扇窗里接一条走它的入口（`BattleLogTab` / `LeaderboardRow` 那一族就是
        //      这么接的），第一次跑就会**另建一台管理器 + 第二套锚点**、窗落进第二台 ⇒ 现象与判据 →
        //      `资料/普查产出_1011/DIAG-B_Rewards十一条红.md` §二·#1（`RewardsScene` 那 8 条红就是同一个形状）。
        var wm = WindowsManager.EnsureHost();      // 它自己建 "Window Anchors" + 三颗 Holder + 管理器，并**登记 `Instance`**

        var win = ShopWindow.Create(wm);
        wm.OpenWindow(win);
        root = win.transform;
        return win;
    }

    /// <summary>⚠️ **2026-10-11（A351）起 `Build()` 不再调它** —— 那几颗 Holder 现在由
    /// `WindowsManager.EnsureHost()` 建（同一个形状、名字与 placement 逐字相同，见 `Build()` 里那段订正）。
    /// **它留着不删**：这是「单独打开某个界面场景」那条路的**形状存档**（同形手抄全仓原有 4 处，A351 全收口）
    /// —— 留着比删掉更能让下一个会话看出「原来长什么样」。⛔ 新代码别调它。
    /// ⚠️ 它**不是** `WindowsManager` 里那份同名私有件（那份在 `Shell/WindowsManager.cs` 里是 `static` 私有、复用不了）
    /// —— 这正是当年四处各抄一份的来由。</summary>
    static void MakeHolder(Transform parent, string name, WindowsPlacement p)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        var h = t.gameObject.AddComponent<WindowHolder>();
        h.placement = p;
        h.RegisterNow();
    }

    static void Shoot(string file, bool allowBlank = false)
    {
        var cam = Camera.main;
        if (cam == null) return;
        const int W = 1920, H = 1080;
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(ShotDir, file), tex.EncodeToPNG());
        // 🔴 **空图护栏**（同 `RewardsScene`）：全黑的图不拦就白拍
        float lum = MeanBrightness(tex);
        if (allowBlank) Debug.Log(P + $"  截图 {file} 平均亮度 {lum:F1}（**这一张按已知情况放行**）");
        else CheckTrue(lum > 3f, $"{file} 不是空图（平均亮度 {lum:F1} > 3）");
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
    }

    static float MeanBrightness(Texture2D t)
    {
        if (t == null) return 0f;
        var px = t.GetPixels32();
        if (px.Length == 0) return 0f;
        long sum = 0;
        for (int i = 0; i < px.Length; i += 7) sum += px[i].r + px[i].g + px[i].b;
        return sum / 3f / ((px.Length + 6) / 7);
    }

    // ============================================================ 自检

    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        Directory.CreateDirectory(ShotDir);
        ShopData.ResetForTest();
        Debug.Log(P + "=== 「商店」自检 开始 ===");

        var win = Build(out var root);

        // ---------------- 窗口参数（`MonoBehaviour_6184803956894681212.json` 原文）----------------
        Section("窗口参数（`Shop Menu Variant` 的 MB 原文）");
        Check(win.type, WindowType.Fullscreen, "`type` = 0 Fullscreen（原文）");
        Check(win.placement, WindowsPlacement.World,
              "`windowsPlacement` = **10 World**（⚠️ **奖励窗是 5 Canvas** —— 两个窗不同，别互推）");
        Check(win.closeOnEsc, true, "`closeOnESC` = 1（⚠️ 奖励窗是 0）");
        CheckNear(win.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = 1.0（原文）");

        // ---------------- 外壳（与奖励窗**同一套**，常量已收口到 `MainMenuSubmenuWindow`）----------------
        Section("外壳 `Content Area` / 左栏 / `Tabs`（原版实测，与奖励窗同值）");
        var area = FindChild(root, "Content Area");
        CheckAt(area, 167.17f, 1920.01f, 70.94f, 1080f, "`Content Area`");
        var bar = FindChild(root, "Tab Buttons");
        CheckAt(bar, 167.17f, 332.17f, 70.94f, 1080f, "`Tab Buttons`（左栏，165 宽）");
        CheckAt(FindChild(bar, "Shadow"), 167.18f, 214.81f, 70.94f, 1080f, "`Tab Buttons/Shadow`（47.64 宽）");
        CheckAt(FindChild(area, "Tabs"), 166.69f, 1920f, 69.20f, 1080f, "`Tabs`");

        Section("左栏键（3 个页 + 1 个母版；**母版照建但关着**）");
        Check(CountByPrefix(bar, "ShopTabButton_"), 4, "建了 **4** 个键（3 页 + `tabButtonPrefab` 母版）");
        Check(CountVisible(bar, "ShopTabButton_"), 3, "**可见的键恰好 3 个**（母版照原版 `TabButtons.Initialize` 关掉）");
        CheckTrue(FindChild(bar, "ShopTabButton_3") != null && !FindChild(bar, "ShopTabButton_3").gameObject.activeSelf,
                  "第 4 键（母版）**建成但关着**");
        for (int i = 0; i < ShopData.Pages.Length; i++)
        {
            var b = FindChild(bar, "ShopTabButton_" + i);
            CheckArt(FindChild(b, "Icon"), ShopData.Pages[i].Icon,
                     $"第 {i + 1} 键的图标 = `{ShopData.Pages[i].Icon}`");
            CheckTrue(TextOf(FindChild(b, "Text")) == ShopData.Pages[i].Label.ToUpperInvariant(),
                      $"第 {i + 1} 键的文案 = `{ShopData.Pages[i].Label.ToUpperInvariant()}`");
        }
        // 🆕 **2026-10-08（波 C3 · A212 主表 #31 验收）：左栏键文案的【渲染】断言**（四窗这一族原来一条都没有）。
        //   判据 = 原版四窗左栏键文案的 `m_TextWrappingMode` **一律 `0`（`NoWrap`）**—— 逐窗现读的四窗表
        //   只写一处：`Shell/MenuWindowBase.cs` 的 `BuildTabButton`（此处不抄第二份，铁律 6）。
        //   ⚠️ 本窗原版**只序列化了一只母版**（`Shop Icon > Label > TabButtonLabel`，`'Pacotes'`，`折行=0`）——
        //   三个页键由 `ShopWindow.CreateStoreTab` 在运行期克隆母版 ⇒ 同样吃这个 0；我们四个键都走
        //   `MenuWindowBase.BuildTabButton` ⇒ 一起断（含第 4 键那只母版）。
        //   ⛔ **为什么必须量渲染、不能只比字号**：`SetAutoFitBox` 只把 `fontSizeMin/Max` 交出去，
        //   **装不装得下由 TMP 算** ⇒ 字号对而字冲出去，自检照样全绿（`AutoFitBox` 那条教训，2026-09-22 踩过）。
        //   框宽 **155** = 原版 `Tab Buttons/*/Label` 的 `sz=(155,37.86)`
        //   （⛔ 不读 `BuildTabButton` 的 `labW` —— 那也是被测实现里的数，读了就是自证）。
        //   **改坏法**：删掉 `MenuWindowBase.BuildTabButton` 末句 `txt.SetWrapping(false)` ⇒
        //   ① `折行=` 那条立刻红（0 → 1）；② 第 4 键的 `BOOSTER PACKS` 带空格 ⇒ 折成两行 ⇒ 「就一行」那条也红。
        {
            int tabN = 0;
            for (int i = 0; i < 4; i++)                     // 0..2 = 三个页键 · 3 = 母版（`tabButtonPrefab`）
            {
                var k = FindChild(bar, "ShopTabButton_" + i);
                var klb = k != null ? k.GetComponentInChildren<Label>() : null;
                CheckTrue(klb != null, $"（左栏渲染断言 · 前提）第 {i + 1} 键的文案 `Label` 取得到");
                if (klb == null) continue;
                tabN++;
                CheckTrue(klb.WrappingMode >= 0,
                          $"（左栏渲染断言 · 前提）第 {i + 1} 键 `{klb.Text}` 走的是 **TMP 后端**"
                        + "（`-1` = 点阵后端 ⇒ 下面两条渲染断言不成立，如实红、不假装）");
                Check(klb.WrappingMode, 0, $"★ 第 {i + 1} 键 `{klb.Text}`：**`折行=0`**（原版四窗左栏键一律 0）");
                Check(klb.LineCount, 1, $"★ …而且渲出来**就一行**（`Normal` 会把带空格的 `BOOSTER PACKS` 折成两行）");
                // 🔴 **2026-10-13（A750）换口**：量法 `Label.WorldW`（= 缓存 `_tmpW`）→ **TMP 自己渲出来那块网格**
                //    —— 与 `Editor/MainMenuScene.cs` 那几处（A709/A714/A715/A718）**同一条口径**；本条 = 这一族的
                //    **第五处**（第四/六处 = `Editor/RewardsScene.cs` · `Editor/CollectionScene.cs`，同一批改完）。
                //    **为什么这是灭自证**：`Label.WorldW` 那份缓存**只有 `RefreshBounds()` 写**（`Battle/Label.cs`）——
                //    也就是**被测实现自己**；而 `SetCharSpacing`（`:524-533`）· `SetFontSize`（`:503-514`）这一族
                //    **只重排 mesh、不刷新缓存** ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**、这条照样绿。
                //    新口读的是 TMP 自己的 `textBounds`，**不在实现那条链上**。
                //    期望值 `155f` / 容差 `+ 0.5f` / 文案全文**一位未动** —— 变的只有「从哪个口读那个数」与缩进。
                // ⚠️ **量不到 ⇒ 必须显式红**：`TmpRenderedRect` 失败时四个 out **全 0** ⇒ 让下面那句拿到 0 的话，
                //    `0 ≤ 155.5` 会**假绿**。本条的判据句**没有** `> 0f` 那一半 ⇒ **不能**用 `-1f` 哨兵
                //    （`-1 ≤ 155.5` 也恒真）—— 只能像 A715/A718 那样把原句包进 `else {}` + 补一条显式红。
                // ⚠️ 量的是 **`klb.transform`**（上面那句 `GetComponentInChildren<Label>()` **一字未动**；
                //    TMP 是 `Label` 的子件 —— `Battle/Label.cs` 的 `Create` 里 `TmpFont.NewText` 那一句 `TmpFont.NewText(transform, …)`）。
                //    ⛔ 别改成 `TmpRenderedRect(k, …)`：那会捞 `k` 子树里**第一颗 TMP（含 inactive）**，
                //    与「只找激活」的 `Label` **未必是同一颗** ⇒ 会把「节点不在（红）」与「量到了别一颗（绿）」混成一档。
                //    ⚠️ 第 4 键（母版 `ShopTabButton_3`）是**关着**的 —— `Label` 那一半照样取得到
                //    （实测 `_tmp_view/shop.log`：「第 4 键的文案 `Label` 取得到 ✓」），本助手取 TMP 时**带 `true`**，
                //    所以这一档也量得到。
                // **改坏法（只咬旧口）**：在 `Shell/MenuWindowBase.cs` 的 `BuildTabButton` 里那句 `SetWrapping(false)` 那句
                //    `if (txt != null) txt.SetWrapping(false);` **之后**插一句 `if (txt != null) txt.SetCharSpacing(5f);`
                //    ——（那句话是四窗左栏键刷**最后一次**缓存的地方：`SetWrapping` → 模式真的变了 → `ForceRelayout`
                //    → `RefreshBounds()`，`Battle/Label.cs` 的 `SetWrapping` → `ForceRelayout` → `RefreshBounds` 那一路）⇒ 网格重排了、**缓存不动** ⇒ 两个口读到的数
                //    **必然不同**。⚠️ 三件套里的第三件（新口红 / 旧口绿）**本地证不出来**，见下一条如实标。
                // ⚠️ **如实标**：这四颗键都开着 **autosize**（`BuildTabButton` 的 `SetAutoFitBox`，
                //    `enableAutoSizing = true`）⇒ TMP 重排时会把字号缩回去、渲出来的宽**仍 ≤ 框宽**
                //    ⇒ 上面那个改坏法**不一定**把绿翻红（「两个口读到的数不一样」才是换口的全部意义）。
                //    实测（`_tmp_view/shop.log` 那一次；⚠️ 那是**换口之前**的跑，四颗读的都是**旧口**）：
                //    `CARDS` 89.0 · `DAILY` 78.9 · `ITEMS` 84.1 · `BOOSTER PACKS` **151.6**
                //    ⇒ 最紧的第 4 键余量只有 **3.9px**（151.6 vs 155.5），而它的 `fontSizeMin = 12`
                //    离标称 25.65 还很远 ⇒ 有充分的缩字空间。
                {
                    float rx1, ry1, rx2, ry2;
                    if (!TmpRenderedRect(klb.transform, out rx1, out ry1, out rx2, out ry2))
                    {
                        CheckTrue(false, $"★ 第 {i + 1} 键 `{klb.Text}`（前提）这一颗 `Label` 底下没有 TMP 网格"
                                       + " ⇒ 「渲出来的宽」量不到（⛔ 不是实现把字冲出去了）");
                    }
                    else
                    {
                        float wTabPx = rx2 - rx1;
                        CheckTrue(wTabPx <= 155f + 0.5f,
                                  $"★ …而且**渲出来的宽 {wTabPx:F1} ≤ 框宽 155**"
                                + "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");
                    }
                }
            }
            Check(tabN, 4, "四颗键的文案都量到了（少于 4 ⇒ 上面那几条等于没查）");
        }

        // ---------------- 页签：切页 ----------------
        Section("页签切换（三个页各自的层 × 参数）");
        for (int p = 0; p < ShopData.Pages.Length; p++)
        {
            win.tabButtons.Click(p);
            Check(win.CurrentTab, (WindowTabType)(10 + p), $"点第 {p + 1} 键 ⇒ 切到 `{ShopData.Pages[p].Label}` 页");

            var pg = FindChild(root, ShopData.Pages[p].Prefab);
            CheckTrue(pg != null && pg.gameObject.activeSelf, $"`{ShopData.Pages[p].Prefab}` 开着");
            // 页根 = `Tabs` 整矩形（三页的根都是 aMin(0,0)/aMax(1,1)/pos(0,0)/sizeDelta(0,0) ⇒ 撑满父）
            CheckAt(pg, 167.17f, 1920.00f, 70.94f, 1080.00f, "页根矩形 = `Tabs` 的整矩形");

            // `daily shop header`（高 85）／`TimeCounter`／`Packs Scroll View`
            var hdr = FindChild(pg, "daily shop header");
            CheckAt(hdr, 167.17f, 1920.00f, 70.94f, 155.94f, "`daily shop header`（高 85）");
            CheckTrue(FindChild(pg, "Line") == null,
                      "`Line` **不建**（出厂 `m_IsActive = false`）");
            var tc = FindChild(hdr, "TimeCounter");
            CheckAt(tc, 367.47f, 678.87f, 70.94f, 150.94f, "`TimeCounter`（311.40 × 80）");
            var sv = FindChild(pg, "Packs Scroll View");
            CheckAt(sv, 329.76f, 1920.00f, 127.62f, 1080.00f, "`Packs Scroll View`");
            // ⚠️ **别拿 `CheckRectPx(sv, …)`** —— `sv` 自己**没有 Graphic**（原版那个 `Image` 是 `UIMask`、
            //    `m_Color.a = 0` ⇒ 我们照纪律没画），所以 `GetComponentInChildren<ImageQuad>()` 会
            //    一路找到**子树里第一格**（337.6 × 477），量出来的是格的尺寸、不是 Scroll View 的。
            //    实测踩过：那两条断言报「宽 337.60 ≈ 1590.24」，**看着像版面错，其实是量错了节点**。
            //    `sv` 的矩形由上面那条 `CheckAt` 钉住；「渲出来的尺寸」留给下面的格去量。

            // 🔴 **三页唯一的结构差**：`TimeCounter` 的第一个子件 —— 文字 vs 时钟图标
            bool asText = ShopData.Pages[p].TimerAsText;
            CheckTrue(FindChild(tc, "RefreshText") != null || FindChild(tc, "Clock Icon") != null,
                      "`TimeCounter` 的第一个子件建了（文字 或 时钟图标）");
            CheckTrue((FindChild(tc, "RefreshText") != null) == asText,
                      asText ? "本页时间条是**文字**（原版 `Refreshes in:`）"
                             : "本页时间条是**时钟图标**（原版 `Clock Icon` = `WF_icon_clock`，preferredWidth 26）");
            if (!asText)
            {
                CheckArt(FindChild(tc, "Clock Icon"), "WF_icon_clock", "时钟图标");
                // 🔴 **量渲染真值**：只断「节点在不在」拦不住「画到屏外去了」
                //    （第一版就是这么错的：只改了 x、y 留在错初值，而断言全绿）。
                var ic = FindChild(tc, "Clock Icon");
                var iq = ic != null ? ic.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(iq != null, "时钟图标有 `ImageQuad`");
                if (iq != null)
                {
                    CheckNear(iq.WorldH * 108f, 26f, 2f, "时钟图标**渲出来的高 = 26**（原版 `preferredWidth 26` + `preserveAspect`）");
                    float pxc = PxOf(iq.transform.position.x), pyc = PxYOf(iq.transform.position.y);
                    CheckNear(pyc, 110.94f, 3f, "时钟图标**在 `TimeCounter` 的竖向中心上**（y ≈ 110.94）");
                    CheckTrue(pxc > 367.47f && pxc < 678.87f,
                              $"时钟图标**落在 `TimeCounter` 的横向范围内**（实测 x {pxc:F1}，容器 367.47..678.87）");
                }
            }
            CheckTrue(TextOf(FindChild(tc, "Time")) == ShopData.RefreshTime,
                      $"倒计时文本 = `{ShopData.RefreshTime}`（原版是 `TimerDisplay` 给的真时间；**值是我们挑的**）");

            // ---- 栅格：`GridLayoutGroup cell 335.6×475 · spacing 0 · padding.top 7 · 2 列 · UpperLeft` ----
            var offers = ShopData.Offers(p);
            var content = FindPath(pg, "Packs Scroll View/Viewport/Content");
            CheckTrue(content != null, "`Content`（栅格挂点）建了");
            CheckByPrefix(content, "CatalogItemShopContainer_", offers.Length,
                          $"格数 = 商品数 {offers.Length}（照 `ShopTab.CreateOffers` 逐 offer 建）");
            for (int i = 0; i < offers.Length; i++)
            {
                int col = i % ShopTabPage.GridCols, row = i / ShopTabPage.GridCols;
                float x1 = 329.76f + col * ShopTabPage.CellW;
                float y1 = 127.62f + ShopTabPage.GridPadT + row * ShopTabPage.CellH;
                var cell = FindChild(content, "CatalogItemShopContainer_" + i);
                CheckAt(cell, x1, x1 + ShopTabPage.CellW, y1, y1 + ShopTabPage.CellH,
                        $"第 {i + 1} 格的位置（col {col} / row {row} ⇒ 原版栅格算式）");
                CheckArt(FindChild(cell, "background"), "UI_Deck_Selection_Back_simple", "格底");
                // 格底**渲出来**的尺寸 = 格 + 2（原版 `background` 的 `sizeDelta = (2,2)`，锚点拉满）
                // 🆕 **2026-10-03**：接上纵向滚动 + 裁切之后，**照原版 `RectMask2D` 先按
                //   `Packs Scroll View` 的视口裁一刀** —— 最后一行会压在视口底边上（内容高 957 > 视口 952.38）。
                float bx1 = Mathf.Max(x1 - 1f, ShopTabPage.ScrollView.x1);
                float bx2 = Mathf.Min(x1 + ShopTabPage.CellW + 1f, ShopTabPage.ScrollView.x2);
                float by1 = Mathf.Max(y1 - 1f, ShopTabPage.ScrollView.y1);
                float by2 = Mathf.Min(y1 + ShopTabPage.CellH + 1f, ShopTabPage.ScrollView.y2);
                CheckRectPxUnion(FindChild(cell, "background"), bx1, bx2, by1, by2,
                                 "格底渲出来的矩形（= 格 + 2，**已按 `Packs Scroll View` 视口裁过**）");
                CheckArt(FindChild(cell, "Generic UI Button"), "40K_button", "价格钮底图");
                CheckNear(TintOf(FindChild(cell, "Generic UI Button")).g, 0.637f, 0.01f,
                          "价格钮的色 = **(0.902,0.637,0.18)**（原版 `m_Color` 原文）");
                CheckTrue(TextOf(FindChild(cell, "Button Text")) == offers[i].Price,
                          $"格 {i + 1} 的价格文本 = `{offers[i].Price}`");
                CheckArt(FindChild(cell, "Counter"), "40K_main_deck_card_counter", "拥有数角标底");
                CheckTrue(TextOf(FindChild(cell, "Text (TMP)")) == "x" + ShopData.OwnedOf(p, i),
                          $"格 {i + 1} 的拥有数 = `x{ShopData.OwnedOf(p, i)}`");
                // `Available Counter`：**有限购信息才画**
                bool wantAvail = offers[i].AvailableMax > 0;
                CheckTrue((FindChild(cell, "Available Counter") != null) == wantAvail,
                          wantAvail ? $"格 {i + 1} 有 `Available Counter`（限购 {offers[i].AvailableMax}）"
                                    : $"格 {i + 1} 没有限购信息 ⇒ **不画** `Available Counter`");
                // 两个出厂 INACT 的角标**不建**
                CheckTrue(FindChild(cell, "TimedOffer") == null && FindChild(cell, "New") == null,
                          $"格 {i + 1}：`TimedOffer` / `New` **不建**（出厂 INACT，且原版锚点算出来落在格外面）");

                // ---- 🔴 「谁压谁 / 谁出界」这一类：**只能量【渲出来】的矩形**，量节点位置量不到 ----
                //     （2026-09-23 实拍才发现的：名字/类型两行**压在商品图上**、`Available` **跨到下一行**，
                //      而当时的 178 条断言**全绿** —— 它们只管「件在不在」「节点在不在原版矩形中心」。）
                var artNode = FindChild(cell, "Art") ?? FindChild(cell, "ArtPlaceholder");
                float ax1, ay1, ax2, ay2, tx1, ty1, tx2, ty2, nx1, ny1, nx2, ny2, vx1, vy1, vx2, vy2;
                bool hasArt = RectOf(artNode, out ax1, out ay1, out ax2, out ay2);
                bool hasTy = RectOf(FindChild(cell, "Type"), out tx1, out ty1, out tx2, out ty2);
                bool hasNm = RectOf(FindChild(cell, "Name"), out nx1, out ny1, out nx2, out ny2);
                CheckTrue(hasArt, $"格 {i + 1} 有主图（真图或占位板）");
                if (hasArt && hasTy)
                    CheckTrue(!Overlaps(ax1, ay1, ax2, ay2, tx1, ty1, tx2, ty2),
                              $"格 {i + 1}：**主图不压类型行**（图 y {ay1:F0}..{ay2:F0} · 类型 y {ty1:F0}..{ty2:F0}）");
                if (hasTy && hasNm)
                    CheckTrue(!Overlaps(tx1, ty1, tx2, ty2, nx1, ny1, nx2, ny2),
                              $"格 {i + 1}：**类型行不压名字行**（类型 y {ty1:F0}..{ty2:F0} · 名字 y {ny1:F0}..{ny2:F0}）");
                // 🔴 **2026-10-12：这一格上当天试挂过一条「`name` 上限 = 42」的断言（F1），已被 H46 整条挪走** ——
                //   它量的是商店格里那行 `Name`（`CatalogItemShopContainer_*/Name`），而那是
                //   `Shell/ShopWindow.BuildCell` 里**我们自己加的一行字**（上限写死 **30** = `ShopWindow.cs:662`
                //   传给 `SetAutoFitBox` 的实参；`Label.cs:615` 把 `FontSizeToPx(FontSizeMax)` 反算回**恒等于**那个实参）
                //   ⇒ 判据文案说的 `OfferContainer` 的 `name`（上限 **42**）**是另一棵树**，
                //   两棵树在代码上没有任何调用关系（`OfferContainer.Build` 的调用点全在本文件的脚手架里）。
                //   ⇒ 它**结构上永远不可能绿**（实测三页各印一次「实得 30.00」），
                //   而且那句「改坏法」对它自己无效（去掉 `OfferContainer` 的实参改不到这行字）。
                //   ⚠️ **那三处（`ShopWindow` 的 `Name`/`Type`/占位名）是我们自加件、原版查不到**（`F1_字号线.md:194`）
                //      ⇒ ⛔ 别再把 42（或任何原版值）断到这里，也⛔ 别为了变绿去改 `ShopWindow.cs:662` 的 30。
                //   真正的落点 = §A8 那 19 份变体的循环（见下面 `[i + 1] name 的自适应上限` 那条）。
                // `Available Counter`：**整条要在格子里面**（第一版它中心在格底边上 ⇒ 半截跨到下一行）
                if (RectOf(FindChild(cell, "Available Counter"), out vx1, out vy1, out vx2, out vy2))
                {
                    CheckTrue(vy1 >= y1 - 0.5f && vy2 <= y1 + ShopTabPage.CellH + 0.5f,
                              $"格 {i + 1}：`Available Counter` **竖向不越出格子**"
                              + $"（实测 y {vy1:F0}..{vy2:F0}，格 {y1:F0}..{y1 + ShopTabPage.CellH:F0}）");
                    CheckTrue(vx1 >= x1 - 0.5f && vx2 <= x1 + ShopTabPage.CellW + 0.5f,
                              $"格 {i + 1}：`Available Counter` **横向不越出格子**");
                }
                // 格内所有件都别横着越界（价格钮 / 名字 / 类型 / 主图 一起过一遍）
                string[] insideNames = { "Name", "Type", "Art", "ArtPlaceholder", "Generic UI Button" };
                for (int k = 0; k < insideNames.Length; k++)
                {
                    float ix1, iy1, ix2, iy2;
                    if (!RectOf(FindChild(cell, insideNames[k]), out ix1, out iy1, out ix2, out iy2)) continue;
                    CheckTrue(ix1 >= x1 - 2f && ix2 <= x1 + ShopTabPage.CellW + 2f,
                              $"格 {i + 1}：`{insideNames[k]}` **横向在格内**（实测 x {ix1:F0}..{ix2:F0}）");
                    CheckTrue(iy1 >= y1 - 2f && iy2 <= y1 + ShopTabPage.CellH + 2f,
                              $"格 {i + 1}：`{insideNames[k]}` **竖向在格内**（实测 y {iy1:F0}..{iy2:F0}）");
                }
            }

            // ---- 🆕 2026-10-03：`Packs Scroll View` 的纵向滚动 + 视口裁剪 ----
            //   判据 = 原版实读 `menu_dump.py bundle_menus_assets_all "Card Shop Tab"`：
            //          `ScrollRect h=0 v=1 mode=1(**Elastic**) inertia=1 elasticity=0.1 decel=0.135` +
            //          `Viewport` 上的 `RectMask2D`；`Content` 由 `ContentSizeFitter(V=Preferred)` 撑到 `行数×475+7`。
            {
                var gs = win.GridScrollOf(p);
                CheckTrue(gs != null, "本页的栅格滚动区建了（`MenuScroll.TopAligned`）");
                if (gs != null)
                {
                    CheckTrue(gs.Vertical, "是**纵向**滚动（原版 `m_Vertical = 1` / `m_Horizontal = 0`）");
                    // 🔴 2026-10-05（A28）：原版 `m_MovementType = 1` = UGUI **Elastic**（真值 `0 Unrestricted / 1 Elastic /
                    //   2 Clamped`，本地 UGUI 源码亲读）。这条**原来断言的是 `!gs.Elastic`（= Clamped）** ——
                    //   把「实现还是默认档」这个缺口**钉死了**（同族教训：断言要盯**原版参数**，不是盯我们自己的常量）。
                    //   **真红法**：去掉 `ShopWindow.BuildGrid` 里那句 `_gridScroll.Elastic = true;` ⇒ 这里立刻红。
                    CheckTrue(gs.Elastic, $"页 {p}：是 **Elastic**（原版这一件的 `m_MovementType = 1`）");
                    float wantH = (offers.Length + ShopTabPage.GridCols - 1) / ShopTabPage.GridCols
                                  * ShopTabPage.CellH + ShopTabPage.GridPadT;
                    CheckNear(gs.ContentX2 - gs.ContentX1, wantH, 0.5f,
                              "`Content` 高 = 行数×475+7（原版 `ContentSizeFitter(Vertical = Preferred)`）");
                    CheckNear(gs.MaxOffset, wantH - ShopTabPage.ScrollView.H, 0.5f,
                              "可滚范围 = 内容高 − 视口高（`Packs Scroll View` 127.62..1080）");

                    // 顶：最后一格**压在视口底边上** ⇒ 原版 `RectMask2D` 把它裁掉一截
                    var lastCell = FindPath(pg, "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_"
                                                + (offers.Length - 1));
                    float cx1, cy1, cx2, cy2;
                    bool has0 = RectOf(FindChild(lastCell, "background"), out cx1, out cy1, out cx2, out cy2);
                    CheckTrue(has0 && cy2 <= ShopTabPage.ScrollView.y2 + 0.5f,
                              $"在顶时最后一格的格底**被视口裁住**（实测底边 {cy2:F2} ≤ 视口底 {ShopTabPage.ScrollView.y2:F2}）");

                    // 滚到底：内容末尾对齐视口底 ⇒ 最后一格**完整**了
                    gs.SetOffset(gs.MaxOffset);
                    var lastCell2 = FindPath(pg, "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_"
                                                 + (offers.Length - 1));
                    CheckTrue(lastCell2 != null, "滚到底之后最后一格**还在**（没被裁掉）");
                    if (lastCell2 != null)
                    {
                        // ⚠️ 最后一格的列号**按页不同**（4 件 ⇒ 最后在 col 1；3 件 ⇒ col 0）—— 别写死
                        int lc = (offers.Length - 1) % ShopTabPage.GridCols;
                        CheckAt(lastCell2, ShopTabPage.ScrollView.x1 + lc * ShopTabPage.CellW,
                                ShopTabPage.ScrollView.x1 + (lc + 1) * ShopTabPage.CellW,
                                ShopTabPage.ScrollView.y2 - ShopTabPage.CellH, ShopTabPage.ScrollView.y2,
                                "滚到底 ⇒ 最后一格**贴着视口底**（内容末尾 = 视口底）");
                        float hx1, hy1, hx2, hy2;
                        // ⚠️ **并集版**（2026-10-04，W2a 软边接线）：格底会被渐隐带的内沿**切开**
                        //    ⇒ `RectOf` 只取第一张 quad、量到的是**其中一格**（实测主格 451、期望 477）。
                        if (RectOfUnion(FindChild(lastCell2, "background"), out hx1, out hy1, out hx2, out hy2))
                            CheckNear(hy2 - hy1, ShopTabPage.CellH + 2f, 2f,
                                      "滚到底 ⇒ 格底**不再被裁**（高回到 格高 + 2）");
                    }
                    // 回顶必须精确回到 0（`SetOffset` 是**绝对**设值 —— 2026-09-23 那条重入教训）
                    gs.SetOffset(0f);
                    CheckNear(gs.Offset, 0f, 0.01f, "回顶后偏移精确 = 0");
                    // 滚轮一格 = 48px（照卡组编辑那条已验过的路：`dy * 0.4f`，新输入系统一格 ±120）
                    // 🔴 2026-10-05（A28）：档位改成 **Elastic** 之后**滚轮不再被夹**（照 UGUI `OnScroll`：**Clamped 才夹回**）
                    //    ⇒ 一格真的走 48px、越出可滚范围（本页只有 4.62px 可滚），随后由 `Tick` 的 `SmoothDamp`
                    //    回弹到上界（`smoothTime = m_Elasticity`；回弹那一支同 `RewardsScene` 的写法）。
                    //    **真红法**：去掉 `_gridScroll.Elastic = true;` ⇒ 第一条立刻红（量到的是被夹回的 4.62、不是 48）。
                    gs.Wheel(-120f);
                    CheckNear(gs.Offset, 48f, 0.5f, "往下滚一格 ⇒ 偏移 +48px（Elastic ⇒ **不被夹**，允许越界）");
                    CheckTrue(gs.OutOfRange,
                              $"…此刻**确实越出了**可滚范围（{gs.Offset:F2} > 上界 {gs.MaxOffset:F2} —— Clamped 到不了这个态）");
                    for (int i = 0; i < 400; i++) gs.Tick(1f / 60f);          // 松手/停滚 ⇒ 回弹
                    CheckNear(gs.Offset, gs.MaxOffset, 0.5f, "…随后 `SmoothDamp` **回弹到上界 4.62**（越界是暂态）");
                    CheckTrue(!gs.OutOfRange, "回弹到位 ⇒ 不再越界");
                    gs.SetOffset(0f);
                }
            }

            // 空态遮罩：出厂 INACT，我们每页都有商品 ⇒ 恒不显示
            var ew = FindChild(pg, "Empty Collection Warning");
            CheckTrue(ew != null && !ew.gameObject.activeSelf,
                      "`Empty Collection Warning` **建成但不显示**（原版在列表为空时才开）");
        }

        // ---------------- 买一件（红线：点了必须有反应）----------------
        Section("买一件：**点了必须有反应**（红线：不许静默失败；边界②：不做真实经济 ⇒ 不扣钱）");
        win.tabButtons.Click(0);
        var pg0 = FindChild(root, ShopData.Pages[0].Prefab);
        int before = ShopData.OwnedOf(0, 0);
        var hit = FindPath(pg0, "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_0/Hit");
        CheckTrue(hit != null, "第 1 格的点击区建了");
        if (hit != null)
        {
            var wb = hit.GetComponent<WindowButton>();
            CheckTrue(wb != null, "点击区挂着 `WindowButton`");
            if (wb != null) wb.ClickForTest();
            Check(ShopData.OwnedOf(0, 0), before + 1, "买了之后**拥有数 +1**");
            CheckTrue(TextOf(FindChild(FindChild(root, ShopData.Pages[0].Prefab),
                                       "Text (TMP)")) == "x" + (before + 1),
                      "画面上的拥有数也跟上了（**卡变了就重建视图**）");
        }
        ClosePackAndReopenShop(win);   // A7：买卡包会弹全屏的开包窗 ⇒ 还原成「商店开着」再往下做

        // ---------------- 🆕 2026-10-03：传奇重复购买确认（§三 第 29 条 A16）----------------
        //   判据 = `d:/2/tools/decomp_full/CatalogItemContainer__TryPurchase.c`：
        //   `Item.Rarity == 4 && GetOwnedCount(Item) == 1` ⇒ 先弹 `MenuShop/ExtraLegendaryWarning`，
        //   **确认回调才走真正的购买**；其余情况直接买。
        Section("传奇重复购买确认（原版 `CatalogItemContainer__TryPurchase`）");
        {
            win.tabButtons.Click(0);
            CheckTrue(ShopData.NeedsLegendaryConfirm(0, 2),
                      "第 3 件（`Space Wolves Booster`：Rarity 4 + 已拥有 **1**）⇒ **要弹确认**");
            CheckTrue(!ShopData.NeedsLegendaryConfirm(0, 0),
                      "第 1 件（已拥有 3）⇒ **不弹**（判据盯的是 `GetOwnedCount == 1`）");
            CheckTrue(!ShopData.NeedsLegendaryConfirm(0, 1),
                      "第 2 件（Rarity 0）⇒ **不弹**");

            int before2 = ShopData.OwnedOf(0, 2);
            var pg = win.PageOf(0);
            CheckTrue(pg != null, "第 1 页的 `ShopTabPage` 拿得到");
            string got = pg != null ? pg.Buy(2) : "没有页";
            Check(got, "", "点了传奇那一件 ⇒ **这一次没买**（原版先弹确认框）");
            Check(ShopData.OwnedOf(0, 2), before2, "…拥有数**没变**（取消分支 = 什么都不做）");
            var wm = win.Manager;
            CheckTrue(wm != null && wm.popUpWindow != null, "确认框**真弹出来了**");
            if (wm != null && wm.popUpWindow != null) wm.popUpWindow.Close();
            // 非传奇那件照旧直接买（单机行为一字不改）
            int before0 = ShopData.OwnedOf(0, 0);
            if (pg != null) pg.Buy(0);
            Check(ShopData.OwnedOf(0, 0), before0 + 1, "非传奇那一件**照旧直接买**（不弹框）");
        }
        ClosePackAndReopenShop(win);   // A7：同上（这一件也是卡包 ⇒ 也会弹开包窗）

        // ---------------- 🆕 2026-10-12：**A439** —— 买「商品档」⇒ 弹领奖窗（5 条）----------------
        //   判据（第一权威 = 反编译方法体；出处全文在 `Shell/ShopWindow.cs` 的 `DoBuy` 那段注释里）：
        //     · **商品档**（`ShopOfferDataV2`）买到手 ⇒ `RewardService.Collect(rewards, showAnimation: **1**, …)`
        //       （`…ShopOfferEventV2…HandleSuccess_0.c:23`）⇒ **弹一扇 `Reward Window`**，逐条画「这一笔买到的东西」；
        //     · **容器档**（`ContainerOfferData`，买卡包那种）⇒ 同一个位置是 `**0**`
        //       （`…ContainerService…OnComplete_0.c:107`）⇒ **不开**领奖窗，走开包窗。
        //   ⇒ 落点 = `Shell/ShopWindow.cs` 的 `DoBuy` 里那两路 `if/else`；奖励表在 `ShopData`（`ShopOffer.Grants`）。
        //   📌 三个把手（`FindOpenRewardWindow` / `OpenRewardWindowCount` / `DismissRewardWindows`）
        //      = **共用件** `Editor/RewardWindowFixture.cs`（🆕 **A450** 从 `RewardsScene` 搬出来的，
        //      原来 `static` 私有 ⇒ 本宿主用不了；⛔ 别在本文件里照抄一份）。
        //   ⚠️ 夹具卫生：弹出来的窗是**弹窗**（`PointerLayer` 会把底下全判成点不到）⇒ 本段**开完必关**，
        //      末尾两条 `（编排）` 断言把「场上不留开着的领奖窗」钉住。
        Section("A439：买「商品档」⇒ 弹领奖窗（原版 `Collect(…, showAnimation: 1, …)`）");
        {
            win.tabButtons.Click(1);                       // 第 2 页 = Daily（三件**都不是**卡包）
            var pgD439 = win.PageOf(1);
            CheckTrue(pgD439 != null, "（前提）第 2 页的 `ShopTabPage` 拿得到");
            Check(ShopData.Offers(1)[0].Type, "Gold Item",
                  "（前提）第 1 件 `Daily Gold Cache` 的 `Type` **不是** `Booster Pack` ⇒ 走**商品档**那一路");
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                  "（前提）买之前场上没有遗留的领奖窗");

            var grants439 = ShopData.GrantsOf(1, 0);
            CheckTrue(grants439 != null && grants439.Length == 1,
                      "（前提）第 2 页第 1 件有奖励表（`ShopData.GrantsOf(1, 0)`，1 条）");

            int owned439 = ShopData.OwnedOf(1, 0);
            if (pgD439 != null) pgD439.Buy(0);             // 真购买路径（`ShopTabPage.Buy` → `DoBuy`）
            Check(ShopData.OwnedOf(1, 0), owned439 + 1, "…拥有数 +1（走的还是原来那条记账路）");

            // ★ ① 买商品档 ⇒ **弹出领奖窗**
            var rw439 = RewardWindowFixture.FindOpenRewardWindow();
            CheckTrue(rw439 != null,
                      "★ ① 买**商品档** ⇒ **弹出 `Reward Window`**（判据 = 原版那一拍传 `showAnimation: 1`）"
                      + "—— 删掉 `Shell/ShopWindow.cs` 里 `RewardWindow.ShowCollected(...)` 那一句 ⇒ 这条红");
            if (rw439 != null)
            {
                // ★ ② 窗里那几条 = `ShopData.GrantsOf(1, 0)` **逐条相等**（Id / 数量 / 档位）
                var rc439 = rw439.Context;
                CheckTrue(rc439 != null && rc439.Rewards != null, "（前提）那扇窗带着 `Context.Rewards`");
                if (rc439 != null && rc439.Rewards != null && grants439 != null)
                {
                    Check(rc439.Rewards.Length, grants439.Length,
                          "★ ② 窗里的**条数** = `GrantsOf(1, 0)` 的条数（传错数组 / 少传一条 ⇒ 红）");
                    int m439 = Mathf.Min(rc439.Rewards.Length, grants439.Length);
                    for (int k = 0; k < m439; k++)
                        Check(rc439.Rewards[k].Id + "×" + rc439.Rewards[k].Quantity + "/" + rc439.Rewards[k].Tier,
                              grants439[k].Id + "×" + grants439[k].Quantity + "/" + grants439[k].Tier,
                              $"★ ② 窗里第 {k + 1} 条 = `GrantsOf(1, 0)` 的第 {k + 1} 条（逐字段：id × 数量 / 档位）");
                    // ⚠️ 上面那两条比的是**同一个数组**（`ShowCollected` 收的就是 `GrantsOf` 那个引用）
                    //   ⇒ 它们断的是**接线**（`DoBuy` 有没有把这一页这一件的表原样递进去）。
                    //   下面两条**钉住数据层那一列的字面量**（`Shell/ShopData.cs` 的 `_daily[0]`）——
                    //   传错页 / 传错件（例如递成 `GrantsOf(1, 1)` 那张野牌）⇒ 这里红。
                    //   ⚠️ **那两个值是我们挑的**（表里逐条标了），⛔ 别当原版值读。
                    //   ⚠️ 外面套 `Length > 0`：条数那条已经红过了，这里别把整条 `Run()` 掀掉（NRE/越界）。
                    if (rc439.Rewards.Length > 0)
                    {
                        // 🔴 **2026-10-15（A544）就地订正（铁律 5）**：这条**期望字面量**随数据层一起改了 ——
                        //   `_daily[0]` 的金币图**从 small 族统一到 big 族**
                        //   （`40k_topmarquee_currency_gold` → `40k_general_icon_currency_gold`；
                        //   档位判据 = 原版货币抽屉主图取 `GetIcon(item, Large)` ⇒ `bigIcon`，
                        //   出处 → `Shell/ShopData.cs` 的 `_daily[0]` 那一段 / `资料/普查产出_1015/R6_A544商品图标判据.md` §二·§四）。
                        //   ⚠️ **判别力不变** —— 它仍然断的是「窗里第 1 条 = 数据层 `_daily[0]` 那一列的**字面量**」
                        //   （⛔ 没有改成「读 `GrantsOf` 自证」）：传错页 / 传错件 ⇒ 照样红。
                        Check(rc439.Rewards[0].Id, "40k_general_icon_currency_gold",
                              "★ ② …第 1 条的 **id** = 数据层 `_daily[0]` 那一列（⛔ 换页/换件就会变）");
                        Check(rc439.Rewards[0].Quantity, 150, "★ ② …第 1 条的**数量** = 同一列");
                    }
                    // ★ ② 画出来的**格子数** = 条数（`RewardWindow.BuildItem` 那句
                    //   `st.NodeName = "Item_" + …` 自己写着「自检按 `Item_` 前缀数格子」）
                    Check(CountByPrefix(rw439.ListHolder, "Item_"), grants439.Length,
                          "★ ② …真画出来的格子数 = 条数（⛔ 不是读 `Context` 自证：数的是 `ListHolder` 下的节点）");
                }
                // 这一格该走**真图**（`40k_general_icon_currency_gold` = 2026-10-15（A544）换上的 big 族那张，
                //   ⚠️ **要先跑过导入腿**才在工程里 —— `工具/import_original_art.py --only-menu`；
                //   没跑 ⇒ 这条红，那正是它该有的反应：`Shell/ShopData.cs` 填的名字工程里没有 = 占位板）
                //   ⚠️ 断的是 `NoIconItems` **不包含**它（⛔ 别断某个 sprite 名 —— 那是实现细节）
                CheckTrue(!rw439.NoIconItems.Contains(grants439[0].Id),
                          "…这一格的图走的是真图（`RewardWindow.NoIconItems` 里没有它 ⇒ 没退化成占位板）"
                          + "—— ⚠️ 没跑导入腿（`工具/import_original_art.py --only-menu`）时它会红，"
                          + "因为那三张 big 族币种图是 2026-10-15（A544）新登记的");

                Check(RewardWindowFixture.DismissRewardWindows(), 1,
                      "（编排）把它关掉 —— 关窗 ⇒ `NotifyClosed` → `ShowPreviousWindow` 把商店带回 `Open`");
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                      "（编排）…场上不留开着的领奖窗（后面那些夹具／截图才拍得干净）");
            }

            // ★ ③ **负例（分档那一条）**：买卡包 ⇒ 一扇领奖窗都不许弹，且**照旧开包**
            win.tabButtons.Click(0);
            var pgC439 = win.PageOf(0);
            CheckTrue(pgC439 != null && ShopData.Offers(0)[3].Type == "Booster Pack",
                      "（前提）第 1 页第 4 件是卡包（容器档）");
            if (pgC439 != null)
            {
                pgC439.Buy(3);
                Check(RewardWindowFixture.OpenRewardWindowCount(), 0,
                      "★ ③ **容器档不开领奖窗**（原版同一个位置传 `showAnimation: 0`）"
                      + "—— 把 `DoBuy` 里那个 `else` 去掉、两条一起接 ⇒ 这条红");
                CheckTrue(pgC439.LastBoosterPack != null, "★ ③ …而且**照旧开包**（走 `OpenBoosterPack`，那条路一个字没改）");
            }
            ClosePackAndReopenShop(win);                   // 开包窗是全屏 ⇒ 还原成「商店开着」

            // ★ ④ 数据层的**可观测口径**：`Dump()` 里那一段（⛔ 不是静默数据、⛔ 不是读 `Grants.Length` 自证）
            string dump439 = ShopData.Dump();
            CheckTrue(dump439.Contains("有奖励表 6/10 件"),
                      "★ ④ `ShopData.Dump()` 报「有奖励表 6/10 件」（3 页 10 件里 6 件填了 `Grants`）"
                      + "—— 给卡包那四件也填 ⇒ 变 10/10 ⇒ 红（实读：" + dump439 + "）");

            // ★ ⑤ **红线**：商品档却**没有奖励表** ⇒ 必须**出声**（不许静默失败）
            //   ⚠️ 用**越界**那一支造出这个状态（本店 10 件都填齐了，而 `Shell/ShopData.cs` 不在本件白名单
            //      ⇒ 不往里塞假货）：`ShopTabPage.Buy(99)` → `DoBuy(99)` ⇒ `inRange = false` ⇒
            //      `ShopData.GrantsOf` 回 `null` ⇒ `ShopWindow.DoBuy` 那条 `LogWarning`。
            win.tabButtons.Click(1);
            int w439 = 0; string last439 = null;
            Application.LogCallback h439 = (cond, st, type) =>
            {
                if (type == LogType.Warning && cond != null && cond.Contains("没有奖励表"))
                { w439++; last439 = cond; }
            };
            Application.logMessageReceived += h439;
            var pgD439b = win.PageOf(1);
            if (pgD439b != null) pgD439b.Buy(99);
            Application.logMessageReceived -= h439;
            CheckTrue(w439 >= 1,
                      $"★ ⑤ 商品档却没有奖励表 ⇒ **出声**（红线：不许静默失败；实测 {w439} 条"
                      + (last439 != null ? "：" + last439 : "") + "）"
                      + "—— 把 `DoBuy` 里那句 `Debug.LogWarning` 删掉 ⇒ 这条红");
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "★ ⑤ …而且**不弹**领奖窗（没有可装的表）");
            win.tabButtons.Click(0);                       // 还原成第 1 页，接着往下做
        }

        // ---------------- 🆕 2026-10-15：**A544** —— 页 3（`Items`）第 1 件的图不是占位板 ------------------
        //   判据（原版侧）→ `资料/普查产出_1015/R6_A544商品图标判据.md` §二 / §四：
        //     · 原版 `Blackstone` SO 的 **`bigIcon`** = `40K_general_icon_currency blackstone`
        //       （⚠️ **带空格** —— 同族里只有它这样；`smallIcon` 才是 `40k_topmarquee_currency_blackstone`）；
        //     · 我们这一格**只画一张图** ⇒ 取**主图那一档 = `bigIcon`**（三条互证 = `CurrencyDrawer__Draw.c`
        //       的 `GetIcon(item, 1)` / `Currency__GetIcon.c` 的 `+0x58` / `dump.cs` 的 `bigIcon // 0x58`
        //       + `enum IconSize{Small=0, Large=1}`）。
        //   🔴 **为什么单开这一段**（R6 §六·3）：本文件里 `GrantsOf(` 原来只有 `(1, 0)` 那一处 ⇒
        //     页 2/3 的 `Grants` **零断言** —— 把 `Id` 改成一个工程里没有的名字，只会 `LogWarning` +
        //     画**占位板**，**自检全绿**（`RewardWindow.NoIconItems` 那条牙口原来只长在页 1）。
        //   ⚠️ **页码口径**：本节说的是 **0 基 `pageIndex = 2`**（= `ShopData.Offers(2)` = `_items`，
        //     界面上的**第 3 页** `Items`）；R6 那份文档里管它叫「**页 2 第 1 件**」（它按 0 基序号数）。
        //   ✅ 两条一起钉：② 值 = R6 选定的那一个（⛔ **不是读数据自证** —— 退回水晶那张**照样红**）·
        //     ③ 图**画得出来**（名字写错 / 没跑导入腿 ⇒ 红）。
        //   ⚠️ 夹具卫生同 A439：本段**开完必关**（弹窗会把底下全判成点不到）。
        Section("A544：页 3（`Items` · `pageIndex 2`）第 1 件发的图 = 黑石那张（原版 `Blackstone` SO 的 `bigIcon`）");
        {
            win.tabButtons.Click(2);                       // 第 3 页 = Items
            var pgI544 = win.PageOf(2);
            CheckTrue(pgI544 != null, "（前提）第 3 页的 `ShopTabPage` 拿得到");
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "（前提）买之前场上没有遗留的领奖窗");
            var gI544 = ShopData.GrantsOf(2, 0);
            CheckTrue(gI544 != null && gI544.Length == 1,
                      "（前提）第 3 页第 1 件有奖励表（`ShopData.GrantsOf(2, 0)`，1 条）");

            if (pgI544 != null) pgI544.Buy(0);             // 真购买路径（`ShopTabPage.Buy` → `DoBuy`）
            var rwI544 = RewardWindowFixture.FindOpenRewardWindow();
            CheckTrue(rwI544 != null, "★ ① 买第 3 页第 1 件（商品档）⇒ 弹出领奖窗（与 A439 同一条路）");
            if (rwI544 != null)
            {
                var rc544 = rwI544.Context;
                CheckTrue(rc544 != null && rc544.Rewards != null && rc544.Rewards.Length > 0,
                          "（前提）那扇窗带着 `Context.Rewards`");
                if (rc544 != null && rc544.Rewards != null && rc544.Rewards.Length > 0)
                {
                    Check(rc544.Rewards[0].Id, "40K_general_icon_currency_blackstone",
                          "★ ② 页 3 第 1 件的奖励 id = 原版 `Blackstone` SO 的 **`bigIcon`**"
                          + "（这是**落盘名**：原版原名 `40K_general_icon_currency blackstone` 带空格，"
                          + "导入器的惯例是空格换下划线）—— 退回 `40k_general_icon_currency_crystal`"
                          + "（A544 报的就是这个缺陷）⇒ 这条红");
                    CheckTrue(!rwI544.NoIconItems.Contains(rc544.Rewards[0].Id),
                              "★ ③ 这一格的图**真画出来了、不是占位板**（`NoIconItems` 里没有它）"
                              + "—— 名字写错 / 没跑 `工具/import_original_art.py --only-menu` ⇒ 这条红");
                }
                Check(RewardWindowFixture.DismissRewardWindows(), 1,
                      "（编排）把它关掉 —— 关窗 ⇒ `NotifyClosed` → `ShowPreviousWindow` 把商店带回 `Open`");
            }
            Check(RewardWindowFixture.OpenRewardWindowCount(), 0, "（编排）…场上不留开着的领奖窗");
            win.tabButtons.Click(0);                       // 还原成第 1 页，接着往下做
        }

        // ---------------- 🆕 2026-10-03：`Booster Info Popup`（§三 第 29 条 A6）----------------
        //   判据 = `资料/阶段二_商店_原版规格.md` **§五·二 / §五·二·一**（19 行逐节点几何表）
        //        + MB `MonoBehaviour_7872967592023106223.json` 的窗口字段（type/placement/closeOnESC/scale）。
        //   🔴 **入口是我们定的**：点卡包格的**商品主图**（原版走 `ShopOfferContainer.OnClick → OpenContainer`，
        //      那一族我们没有）⇒ 见 `Shell/BoosterInfoPopup.cs` 文件头。只在 `Booster Pack` 上接。
        Section("`Booster Info Popup`（A6；⚠️ 入口是我们定的，不是复刻）");
        {
            win.tabButtons.Click(0);
            var pc0 = FindChild(root, ShopData.Pages[0].Prefab);
            CheckTrue(CountByPrefix(pc0, "InfoHit") == 4,
                      "Cards 页 4 件**都是卡包** ⇒ 4 个 `InfoHit`（实测 " + CountByPrefix(pc0, "InfoHit") + "）");
            win.tabButtons.Click(1);
            var pc1 = FindChild(root, ShopData.Pages[1].Prefab);
            CheckTrue(CountByPrefix(pc1, "InfoHit") == 0, "Daily 页（非卡包）**没有**这个入口");
            win.tabButtons.Click(0);

            var pgB = win.PageOf(0);
            var pop = pgB != null ? pgB.OpenBoosterInfo(0) : null;
            CheckTrue(pop != null, "`Booster Info Popup` 开出来了（入口 `ShopTabPage.OpenBoosterInfo`）");
            if (pop != null)
            {
                var t = pop.transform;
                // ---- 窗口字段（MB 原文，逐个抄的）----
                Check(pop.type, WindowType.Popup, "`type` = 1 (**Popup**)（MB 原文）");
                Check(pop.placement, WindowsPlacement.World,
                      "`windowsPlacement` = 10 (**World**，不是弹窗那档 15)（MB 原文）");
                CheckTrue(pop.closeOnEsc, "`closeOnESC` = 1（MB 原文）");
                CheckNear(pop.extraScaleSmallScreen, 1.2f, 1e-4f, "`extraScaleSmallScreen` = 1.2（MB 原文）");
                CheckTrue(pop.Host == win, "宿主商店窗挂上了（购买那条链要走它的传奇确认闸门）");

                // ---- 层一：压暗层 + 窗底 ----
                CheckAt(FindChild(t, "Menu Dark Background"), -1327.30f, 3247.30f, -746.18f, 1826.18f,
                        "压暗层 `Menu Dark Background`");
                CheckNear(TintOf(FindChild(t, "Menu Dark Background")).a, 0.773f, 0.005f, "压暗层 α = 0.773");
                var wn = FindChild(t, "window");
                CheckAt(wn, 395.72f, 1524.28f, 188.35f, 851.65f, "`window`");
                CheckArt(FindChild(wn, "Generic Window Red Background Big"), "UI_Deck_Information_Back",
                         "窗底 `Generic Window Red Background Big`");

                // ---- 关闭钮（**这一件自己的底图是画出来的**，与对局历史那扇不同）----
                var cb2 = FindChild(wn, "Generic Close Button Orange");
                CheckAt(cb2, 1487.06f, 1561.45f, 159.83f, 235.44f, "`Generic Close Button Orange`");
                CheckArt(FindChild(cb2, "Background"), "40k_general_bt_yellow", "关闭钮 `Background`");
                CheckArt(FindChild(cb2, "Icon"), "40k_general_bt_yellow_close", "关闭钮 `Icon`");

                // ---- 主图：`background` 画商品图、`foreground` 只建节点（原版两处都空）----
                CheckArt(FindPath(wn, "Artwork/background"), ShopData.Offers(0)[0].Art, "主图 = 商品表的 `Art`");
                var fg = FindPath(wn, "Artwork/foreground");
                CheckTrue(fg != null && fg.GetComponentInChildren<ImageQuad>() == null,
                          "`foreground` **只建节点、不画**（原版 `m_Sprite` 也是空，判据不足 ⇒ 留白不猜）");

                // ---- 四段字：文本 + 字号 + 字距（盯**原版参数**，不是盯我们自己的常量）----
                var title = FindPath(wn, "Text/Title");
                Check(TextOf(title), ShopData.Offers(0)[0].Name, "`Title` = 商品名");
                // ⚠️ 原版这三条 TMP 都是 **`auto(min-max)`**（Title 3–40 · Category 3–39 · Descr 3–35）
                //    ⇒ **字号是被框缩过的**（实测 Title 34.29 / Category 29.69），**不能断「≈ 40」**。
                //    判据改成「落在原版的 auto 区间里」+「渲染宽不超出框」（同 `已知的坑.md` 那条 AutoFitBox 教训）。
                CheckTrue(title.GetComponentInChildren<Label>().FontPxNow <= 40.01f
                          && title.GetComponentInChildren<Label>().FontPxNow >= 3f,
                          "`Title` 字号落在原版 `auto(3-40)` 区间（实测 "
                          + title.GetComponentInChildren<Label>().FontPxNow.ToString("F2") + "）");
                var cat = FindPath(wn, "Text/Category");
                Check(TextOf(cat), "Booster Pack", "`Category`（Rarity 0 ⇒ 用商品表的 `Type`）");
                CheckTrue(cat.GetComponentInChildren<Label>().FontPxNow <= 39.01f
                          && cat.GetComponentInChildren<Label>().FontPxNow >= 3f,
                          "`Category` 字号落在原版 `auto(3-39)` 区间（实测 "
                          + cat.GetComponentInChildren<Label>().FontPxNow.ToString("F2") + "）");
                CheckNear(cat.GetComponentInChildren<Label>().CharSpacing, -1.8f, 0.01f,
                          "`Category` **字距 −1.8**（原版 `m_characterSpacing`）");
                var desc = FindPath(wn, "Text/Descripton");
                Check(TextOf(desc), BoosterInfoPopup.DescSample,
                      "`Descripton` = **prefab 出厂文本**（原版运行期由服务端 item 覆盖）");
                var cc2 = FindPath(wn, "Text/CrateCounter");
                Check(TextOf(cc2), BoosterInfoPopup.CrateCounterText, "`CrateCounter` 出厂文本");
                CheckNear(cc2.GetComponentInChildren<Label>().CharSpacing, -2f, 0.01f,
                          "`CrateCounter` **字距 −2**（原版）");

                // ---- 保底进度条：底 / 填充 / 描边 / 计数 / 说明图标 ----
                var sl = FindPath(wn, "Text/Booster pack guarantee Slider");
                CheckArt(FindChild(sl, "Background"), "40k_campaign_bar_bg", "进度条底");
                CheckArt(FindChild(sl, "Outline"), "40k_campaign_bar_outline", "进度条描边");
                CheckArt(FindPath(sl, "Fill Area/Fill"), "40k_campaign_bar_fill", "进度条填充");
                CheckArt(FindPath(sl, "Fill Area/Fill/end"), "40k_campaign_bar_end", "填充端帽 `end`");
                // 填充宽 = 值比例 × 406.58（原版 `Fill Area` 的宽）
                CheckNear(pop.SliderFillW, 203.29f, 0.5f,
                          "填充宽 = 100/200 × 406.58（原版 `Slider.m_FillRect` 的语义）");
                Check(TextOf(FindChild(sl, "counter")), BoosterInfoPopup.CounterSample, "`counter` 出厂占位值");

                // ================================================================
                //  🆕 A275（2026-10-10 · W5 件②）：`CrateCounter` / `counter` 的**对齐 = 原版 Center**
                //
                //  判据 = 原版 prefab 的 TMP 字段（第一权威 · 现读现核）：
                //    · `CrateCounter` → `MonoBehaviour_7693323916674998959.json`：
                //        `m_HorizontalAlignment = **2 (Center)**` · `m_VerticalAlignment = 4096 (Midline)`
                //        —— ⛔ 同窗三个兄弟 `Title` / `Category` / `Descripton` **全是 `H=1 (Left)`**
                //        （本段上面三条已各断过），**别把它们一起改**。
                //    · `counter` → `MonoBehaviour_8510044244370989743.json`：`m_HorizontalAlignment = **2 (Center)**`
                //  框（原版 RT 现读；**Center 说的是「框内居中」，两个框本身都是左锚的、位置一个字没动**）：
                //    · `RectTransform_1062604289391534767`（CrateCounter）：锚 (0,1)-(0,1) · `ap (16, −437.8)`
                //      · `sd (485.284, 45)` · pivot (0, 0.5)；父 `Text` 左沿 = **960.000**
                //      ⇒ 绝对矩形 **976.000 → 1461.284** ⇒ **框心 x = 1218.642**
                //    · `RectTransform_7090266904154481327`（counter）：锚 (.2,.2)-(.8,.7) · `sd (0,0)` · pivot (0, 0.5)
                //      ；父 `Slider`（`RectTransform_7655938153796608687`）= **1004.414 → 1410.990**（宽 406.576）
                //      ⇒ 绝对矩形 **1085.729 → 1329.674** ⇒ **框心 x = 1207.702**
                //  ⇒ 我们的等效做法 = **不调 `AlignLeft`**：`MenuDraw.Text` 建的 Label pivot 是 `(0.5,0.5)`、
                //     `TmpFont` 把所有新 TMP 建成 `Center`、`RefreshBounds` 再把字块**居中**放在锚点上
                //     ⇒ **不调它 = 居中 = 原版 Center**（本仓**没有 `AlignCenter` 助手**，别自己加一个）。
                //
                //  ⛔ 期望值 = 上面那两个**原版框心字面量**（⛔ **不读** `BoosterInfoPopup.CrateCounterR` /
                //     `SliderCounterR` —— 那是被测实现传进去的实参，同式自证）；
                //  ⛔ 也**不许**断「调过 / 没调过 `AlignLeft`」——那是自证（断言要量**渲出来的几何**）。
                //  **改坏法**：任一处改回 `MenuDraw.AlignLeft(...)` ⇒ 字块左缘被钉到**框左缘**
                //     （976.000 / 1085.729）⇒ 下面「块左缘 = 框心 − 块宽/2」立刻红。
                //  ⚠️ **两个实例的判别力不一样（如实标）**：
                //     · `counter`：字块 ~90px、框 243.945px ⇒ 两种对齐差 ~76px ⇒ **强判据**（那两条就是它）；
                //     · `CrateCounter`：那句长文案被 `SetAutoFitBox(485.284, 45, 3, 35)` 缩到**几乎正好填满**框
                //       ⇒ 两种对齐差 ≈ (485.284 − 块宽)/2 ≈ 0 ⇒ 它那一格**只能断「块心 = 框心」**、
                //       分不出两种状态（**只覆盖到「居中」这一半**）。
                {
                    var ccLb = cc2 != null ? cc2.GetComponentInChildren<Label>() : null;
                    var cntNode = FindChild(sl, "counter");
                    var cntLb = cntNode != null ? cntNode.GetComponentInChildren<Label>() : null;
                    CheckTrue(ccLb != null && cntLb != null && ccLb.CanRenderChinese && cntLb.CanRenderChinese,
                              "（前提）两处都是**真 TMP** 标签 —— 点阵后端（`_tmp == null`）没有「整块居中」这回事、"
                              + "`AlignLeftOn` 在那边**是空操作** ⇒ 两种对齐等价、下面两条会退化成没查"
                              + "（`Label.CanRenderChinese` ⟺ `_tmp != null`）");
                    if (ccLb != null)
                    {
                        float tx1, ty1, tx2, ty2;
                        if (!RectOf(cc2, out tx1, out ty1, out tx2, out ty2))
                            CheckTrue(false, "`CrateCounter` 的**字块矩形量不到**（这一条测不到就等于没查）");
                        else
                            CheckNear(tx1, 1218.642f - (tx2 - tx1) * 0.5f, 0.5f,
                                      "★ `CrateCounter` **字块左缘 = 框心 1218.642 − 块宽/2**（原版"
                                      + " `m_HorizontalAlignment = 2 (Center)`；`AlignLeft` 会把它钉在框左缘 976.000 上）"
                                      + "（实得左缘 " + tx1.ToString("F2") + " · 块宽 " + (tx2 - tx1).ToString("F2") + "）");
                    }
                    if (cntLb != null)
                    {
                        float bx1, by1, bx2, by2;
                        if (!RectOf(cntNode, out bx1, out by1, out bx2, out by2))
                            CheckTrue(false, "`counter` 的**字块矩形量不到**（这一条测不到就等于没查）");
                        else
                        {
                            CheckNear(bx1, 1207.702f - (bx2 - bx1) * 0.5f, 0.5f,
                                      "★ `counter` **字块左缘 = 框心 1207.702 − 块宽/2**（原版"
                                      + " `m_HorizontalAlignment = 2 (Center)`）（实得左缘 " + bx1.ToString("F2")
                                      + " · 块宽 " + (bx2 - bx1).ToString("F2") + "）");
                            // 下面两条 = **判别力的前提**（块比框窄才分得出两种状态；等宽时两法等价）
                            CheckTrue(bx2 - bx1 < 243.945f - 20f,
                                      "★ …**这一格确实是「块宽 < 框宽」那个实例**（原版框宽 243.945）"
                                      + " —— 否则上面那条会退化成「两种对齐等价」= 没查（实得块宽 "
                                      + (bx2 - bx1).ToString("F2") + "）");
                            CheckTrue(bx1 > 1085.729f + 20f,
                                      "★ …块左缘**明显不在框左缘上**（框左缘 1085.729，原版 RT 复算）"
                                      + " —— `AlignLeft` 那一路正好落在它上面（实得 " + bx1.ToString("F2") + "）");
                        }
                    }
                }
                CheckArt(FindChild(sl, "Tooltip"), "40k_generic_bt_info", "`Tooltip` 图标");

                // ---- 两个购买钮 ----
                var pd2 = FindPath(wn, "Text/Purchase buttons/Price Display/Generic UI Button");
                CheckArt(pd2, "40K_button", "`Price Display` 的 `40K_button`");
                Check(TextOf(FindChild(pd2, "Button Text")), ShopData.Offers(0)[0].Price, "价格文本 = 商品表的价格");
                var wsB = FindPath(wn, "Text/Purchase buttons/WebShop Button");
                CheckArt(FindChild(wsB, "Highlight"), "OctagonUI_Filled_Fade_SDF", "`WebShop` 的 `Highlight`");
                CheckArt(FindChild(wsB, "Button Image"), "40K_button", "`WebShop` 的 `Button Image`");
                CheckArt(FindChild(wsB, "Icon"), "40K_Icon_Discount_Gold", "`WebShop` 的 `Icon`");
                float ix1, iy1, ix2, iy2;
                if (RectOf(FindChild(wsB, "Icon"), out ix1, out iy1, out ix2, out iy2))
                    CheckNear(iy2 - iy1, 51.88f * 1.2f, 1.5f,
                              "`Icon` **画出来 = 51.88 × `localScale 1.2` = 62.26**（原版就带这个缩放）");
                Check(TextOf(FindChild(wsB, "Button Text")), BoosterInfoPopup.WebShopText, "`Save More!`");
                // 🆕 **2026-10-08（波 C3 · A214③）：`BoosterInfoPopup.WebTextR` 那对常量的断言。**
                //   此前**全仓 0 处引用**（A62 ⑫④ 把它按修好的 `menu_dump.py` 从 `1291.22/1423.22` 改成
                //   `1296.42/1428.42`，而**改回去也不会红**）。这里量**渲出来的节点**把它钉死。
                //   期望值 = **原版 dump 的字面量**（⛔ 不从 `BoosterInfoPopup.WebTextR` 读 —— 那是自证）：
                //   `python 工具/menu_dump.py bundle_menus_assets_all "Booster Info Popup" --depth 16 --md`
                //   ⇒ `…/WebShop Button > Button Text` = **1296.42,725.86 → 1428.42,777.74**（宽 132.00 ·
                //   `折行=0 auto[12~38]`）。
                //   🔴 那 **+5.19** 的机理：`WebShop Button` 是 `HorizontalLayoutGroup`（spacing 0），
                //   前一件 `Icon` 的**布局框 51.88 自带 `m_LocalScale = 1.2`** ⇒ uGUI 推进量按
                //   `childSize × scaleFactor`、组内居中的起始偏移又按乘过缩放的 requiredSpace 折半
                //   ⇒ 净位移 `51.88 × (1.2 − 1) ÷ 2` = **+5.19**（旧的 1291.22 是**旧工具**的读数）。
                //   ⚠️ 这一格走的是 `MenuDraw.Text`（**不是 `TextBox`**）且**没有** `alignLeft`
                //   （`Shell/BoosterInfoPopup.cs` 建它那三行：`MenuDraw.Text(ws, WebTextR, …)`）⇒ 节点中心 = 矩形中心。
                //   **改坏法**：把 `WebTextR` 改回 `1291.22/1423.22` ⇒ 中心左移 5.19px ⇒ 前两条红；
                //   只把宽度改成别的（中心不动）⇒ 第三条红。
                {
                    var wsTx = FindChild(wsB, "Button Text");
                    CheckTrue(wsTx != null, "（前提）`WebShop Button > Button Text` 那个节点在（不然下面三条等于没查）");
                    if (wsTx != null)
                    {
                        CheckNear(PxOf(wsTx.position.x), 1362.42f, 0.5f,
                                  "★ `WebShop Button > Button Text` 中心 x = **1362.42** = (1296.42+1428.42)/2"
                                + "（原版 dump 的左右沿；旧值那对给出 1357.23 ⇒ 这一条红）");
                        CheckNear(PxYOf(wsTx.position.y), 751.80f, 0.5f,
                                  "★ …中心 y = **751.80** = (725.86+777.74)/2（同一对常量的另一半）");
                        // 🔴 **2026-10-08 我加的一行**（报告 §四·4 说「宽度不增加鉴别力」——那是就**位移机理**说的；
                        //    这里断的是**这对常量的另一半**：宽度只进 `SetAutoFitBox`、**不影响中心** ⇒ 不单独断就漏）
                        var wsTmp = wsTx.GetComponentInChildren<TMPro.TextMeshPro>();
                        CheckNear(wsTmp != null ? wsTmp.rectTransform.sizeDelta.x * 108f : -1f, 132f, 0.5f,
                                  "★ …而它的**文本框宽 = 132.00**（= 1428.42 − 1296.42；`SetWrapWidth` 写的就是这个宽）");
                    }
                }
                // ---- 🆕 **2026-10-05（A50① 的「另一处」）**：**这一格是全工程唯一 `maxPx ≠ 字号` 且余量较大的**
                //   （fs 34.2 而 `auto[12,38]`）—— `资料/待办判据_审查发现_1005.md:104` 明写「两种换算给出
                //   **相反**答案」⇒ 补一条**它自己的**端到端探针（原来 0 条）。
                //   🔴 期望值 = **原版 dump 的字面量** `auto[12,38]`（出处 `资料/阶段二_商店_原版规格.md:344`
                //      那一行 `TMP fs34.2 auto[12,38] …不折行`；那是**直接读原始 JSON** 出来的施工图）
                //      —— ⛔ **不读** `SetAutoFitBox` 的入参、也不读 `Label` 里任何由调用方传进去的值（那是被测实现）。
                //   🔴 牙口：`Label.SetAutoFitBox` 的上限改回「调用方字号」（旧写法 `fontSizeMax = cur`）
                //      ⇒ 上限量成 **34.2**≠38；下限按旧的 `cur·minPx/maxPx` 算 ⇒ 量成 **10.8**≠12。
                {
                    var wtLb = FindChild(wsB, "Button Text") != null
                             ? FindChild(wsB, "Button Text").GetComponentInChildren<Label>() : null;
                    CheckNear(Label.FontSizeToPx(wtLb != null ? wtLb.FontSizeMax : 0f), 38f, 0.05f,
                              "`WebShop Button/Button Text` 的**自适应上限** = 原版 `m_fontSizeMax` **38px**"
                              + "（⚠️ 它 ≠ 本件字号 34.2 —— 这一格就是「两种换算会给出相反答案」那处）");
                    CheckNear(Label.FontSizeToPx(wtLb != null ? wtLb.FontSizeMin : 0f), 12f, 0.05f,
                              "…**下限** = 原版 `m_fontSizeMin` **12px**（别按 `字号×min/max` 算，那是旧写法 = 10.8）");
                }

                // ---- 换一件传奇的：`Category` 走原文那档 ----
                pop.Show(0, 2);
                // ⚠️ 路径要**从弹窗根算起**（`Text` 是 `window` 的子节点）—— 第一版漏了 `window/`
                //    ⇒ `FindPath` 返回 null、`TextOf` 什么都读不到（自检报「实得 []」）。
                Check(TextOf(FindPath(pop.transform, "window/Text/Category")), "Legendary Booster Pack",
                      "Rarity 4 ⇒ `Category` = `Legendary Booster Pack`（原文那档）");

                // ---- 🆕 2026-10-03（A12-P1 欠下的断言）：`Tooltip` 图标的悬停 tooltip ----
                //   判据 = 原版 `EverguildTooltipTrigger`（`text = "MenuShop/BoosterInfo/LegendaryTooltip"`，
                //   词条表在远端 CCD ⇒ 本地没有）+ **`m_RaycastPadding = (−15,−15,−15,−15)`**
                //   （**负值 = 外扩**，反证写在 `BoosterInfoPopup.BuildTooltipHit` 的注释里）。
                Section("`Booster Info Popup` 的 `Tooltip` 图标：悬停出面板 + 命中区外扩 15px");
                {
                    CheckTrue(BoosterInfoPopup.TipBody.Trim() == "",
                              "`BoosterInfoPopup.TipBody` **一个字都没编**（面板照弹、正文空）");
                    var layerS = PointerLayer.Instance;
                    var tipNode = FindPath(pop.transform, "window/Text/Booster pack guarantee Slider/Tooltip");
                    var tipQ = tipNode != null ? tipNode.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(tipQ != null, "`Tooltip` 图标在（下面拿它的**渲染矩形**当判据）");
                    float tx1, ty1, tx2, ty2;
                    bool hasTip = RectOf(tipNode, out tx1, out ty1, out tx2, out ty2);
                    CheckTrue(hasTip, "`Tooltip` 图标的渲染矩形量得到");
                    if (hasTip)
                    {
                        // ⚠️ 先钉**图标自己的位置**（原版 `TooltipR` = 1411.00,667.18 → 1455.88,712.09）
                        //    —— 这样下面「外扩 15」那条就不是自证
                        CheckNear((tx1 + tx2) * 0.5f, 1433.44f, 1f, "图标中心 x = **1433.44**（原版 `TooltipR` 的中心）");
                        CheckNear((ty1 + ty2) * 0.5f, 689.635f, 1f, "图标中心 y = **689.635**（同上）");
                        CheckNear(tx2 - tx1, 44.88f, 1f, "图标宽 = **44.88**（原版 `TooltipR` 的 sizeDelta）");
                    }
                    int scS = Tooltip.ShowCount;
                    float tipCx = (tx1 + tx2) * 0.5f, tipCy = (ty1 + ty2) * 0.5f;
                    var hbS = layerS != null ? layerS.HoverAt(tipCx, tipCy) : null;
                    // 🔴 这一条要的是**真鼠标**那条路（`PointerLayer.HoverAt`）。
                    //    ⚠️ **若它红了，先看下面这句诊断**：两条命中区**同队列**时，`PointerLayer.HitButton`
                    //    那句「同队列再比 z」就退化成「`FindObjectsByType` 的枚举顺序」—— 因为
                    //    `ImageQuad` 的世界 z **恒为 0**（`LayoutSpace.ToWorld` 就给 0）⇒ **谁吃到命中不可控**
                    //    （正是 CLAUDE.md §三 那条「同队列谁盖谁不可控」）。
                    // 🔴 **2026-10-03 就地更正（铁律 5）**：这里原来写着「本件**只写断言、不改实现**」——
                    //    **当天就改了实现**：压暗层的命中区从 `QHit` 降到压暗层自己那一档
                    //    （`BoosterInfoPopup.QShadeHit`），窗内四个命中区不再被抢。
                    //    分档的三条断言在**下一节**（断的是「谁高谁低」这个关系，不是某一个点的巧合）。
                    CheckTrue(hbS != null && hbS == BoosterInfoPopup.TipHit,
                              "`PointerLayer.HoverAt(图标中心)` 打到的**就是** `BoosterInfoPopup.TipHit`"
                              + (hbS != null && hbS != BoosterInfoPopup.TipHit
                                 ? $" —— ⚠️ 实得 `{hbS.name}`；若是压暗层的 `CloseHit`，说明"
                                   + "「压暗层命中区低于窗内命中区」这条分档被改回去了（见下一节）"
                                 : ""));
                    // ② 悬停**接线**本身：直调 `Enter/Exit`（**同 `WindowButton.AuditHoverSwap` 的口径**，
                    //    批处理里没有帧循环 ⇒ 这是唯一入口）—— 这一条**不依赖**上面那次命中，
                    //    所以「同队列不可控」不会把它一起拖红。
                    // 🔴 **2026-10-03 就地更正（铁律 5）**：这里原来少了「**先把指针挪开**」这一步 ——
                    //    上面 `HoverAt(图标中心)`（`:645`）那次**真悬停**已经派发过一次 `Enter()` 了
                    //    （`TipHovers` 0→1，面板也正是那一次弹出来的，所以 `:664/:666/:667` 都是绿的），
                    //    而 `WindowButton.Enter()` 是**幂等**的（`Shell/PromptPopup.cs` 的 `WindowButton.Enter` 的 `if (Hovered) return;`，
                    //    等价原版 `IPointerEnterHandler`「每次进入只发一次」）⇒ 这里再叫一次计数不涨，
                    //    断言报「期望 2 实得 1」。**实现是对的、是断言少了前提**（不是漏触发）。
                    //    ⇒ 挪到空白清掉悬停态、再量增量；顺带把「幂等」也钉一条（免得下次又把 `repeat` 当漏触发）。
                    var wbTip = BoosterInfoPopup.TipHit;
                    if (layerS != null) layerS.HoverAt(5f, 5f);     // 挪开 ⇒ `Exit()`（`onExit = Tooltip.Hide`）
                    int tip1 = BoosterInfoPopup.TipHovers;
                    if (wbTip != null) wbTip.Enter();
                    Check(BoosterInfoPopup.TipHovers, tip1 + 1,
                          "悬停 ⇒ `TipHovers` **+1**（原版 `EverguildTooltipTrigger.OnPointerEnter`）");
                    if (wbTip != null) wbTip.Enter();                // 已悬停时再来一次
                    Check(BoosterInfoPopup.TipHovers, tip1 + 1,
                          "…**已悬停**时再叫一次 `Enter()` **不重复计**（原版 `IPointerEnterHandler` 每次进入只发一次；"
                          + "`WindowButton.Enter` 开头那句 `if (Hovered) return;`）");
                    CheckTrue(Tooltip.ShowCount >= scS + 1,
                              "…`Tooltip.ShowCount` 也涨了（面板**照原版的时机弹出来了**）");
                    CheckTrue(Tooltip.Visible, "…`Tooltip.Visible == true`");
                    CheckTrue((Tooltip.ShownBody ?? "").Trim() == "", "…面板里的**正文是空的**（一个字都没编）");
                    // 面板**钉在图标上**：原版这一件 `tooltipAnchor = 0`（None）⇒ pivot (.5,.5)
                    //   ⇒ 面板中心 == 图标中心（**不跟鼠标**）
                    CheckNear(LayoutSpace.PxX(Tooltip.PanelCenter.x), tipCx, 0.5f,
                              "面板中心 x = 图标中心 x（`tooltipAnchor = 0` ⇒ pivot (.5,.5)）");
                    CheckNear(LayoutSpace.PxY(Tooltip.PanelCenter.y), tipCy, 0.5f,
                              "…y 同（`offset = (0,0,0)`）—— 与锻造页那件的 anchor 25 是**两个不同的值**，别互抄");
                    // ③ 离开 ⇒ 收
                    if (wbTip != null) wbTip.Exit();
                    Tooltip.FinishFade();                           // ⚠️ 批处理没有帧循环 ⇒ 手动结束淡出
                    CheckTrue(!Tooltip.Visible, "指针离开 ⇒ `Tooltip.Visible == false`（原版 `OnPointerExit` 立刻收）");
                    // 🔴 命中区**外扩 15px**（原版 `m_RaycastPadding = (−15,−15,−15,−15)`）
                    // ⚠️ `RectOf` 那句要在 `if` 的**条件里**再调一次（不是偷懒）：C# 的确定赋值分析
                    //    只认「同一个布尔表达式里 `out` 出来」的变量，存进 `bool hasHit` 之后就不认了（CS0165）。
                    //    多调一次是**纯读**、无副作用。
                    float hx1, hy1, hx2, hy2;
                    var hitNodeS = BoosterInfoPopup.TipHit != null ? BoosterInfoPopup.TipHit.transform : null;
                    bool hasHit = hitNodeS != null && RectOf(hitNodeS, out hx1, out hy1, out hx2, out hy2);
                    CheckTrue(hasHit, "命中区的渲染矩形量得到");
                    if (hasHit && hasTip && RectOf(hitNodeS, out hx1, out hy1, out hx2, out hy2))
                    {
                        CheckNear(hx1, 1396.00f, 1f, "命中区左边缘 = **1396.00**（= 原版 `TooltipR.x1 1411.00 − 15`）");
                        CheckNear(hy1, 652.18f, 1f, "命中区上边缘 = **652.18**（= 667.18 − 15）");
                        CheckNear(hx2, 1470.88f, 1f, "命中区右边缘 = **1470.88**（= 1455.88 + 15）");
                        CheckNear(hy2, 727.09f, 1f, "命中区下边缘 = **727.09**（= 712.09 + 15）");
                        CheckNear(hx2 - hx1, (tx2 - tx1) + 30f, 1f,
                                  $"…宽 = 图标宽 + 30（外扩，**不是内缩**：实测 {hx2 - hx1:F2} vs 图标 {tx2 - tx1:F2}）");
                    }
                    // 收尾：把指针层的悬停态清掉（离开 ⇒ 收；批处理没有帧循环 ⇒ 手动结束淡出）
                    if (layerS != null) layerS.HoverAt(5f, 5f);
                    Tooltip.FinishFade();
                    CheckTrue(!Tooltip.Visible, "指针挪到空白 ⇒ `Tooltip.Visible == false`");
                }

                // ---- 🆕 2026-10-03：**分档** —— 压暗层不许抢走窗内命中 ----
                //   判据 = `CLAUDE.md` §三「**分层要用渲染队列，不能用 z**」。
                //   🔴 机制：`ImageQuad.Create` 造出来的 quad **世界 z 恒为 0**（`LayoutSpace.ToWorld` 就给 0）
                //      ⇒ 两条命中区**同队列**时 `PointerLayer.HitButton` 的「再比 z」退化成
                //      `FindObjectsByType` 的**枚举顺序** ⇒ 谁吃到命中不可控。
                //   🔴 修前实测（`_tmp_view/shop.log:11896`）：压暗层 `Menu Dark Background/CloseHit`
                //      与窗内**四个**命中区同档（都是 `QHit = 3079`）⇒ 它把 `Tooltip` 图标、**价签**、
                //      `WebShop Button` 全抢走了 —— 那两颗钮**点不动、点下去只会关窗**。
                //   ⚠️ 这一节断的是**分档关系**（队列谁高谁低）+ **真鼠标那条路上命中的是谁**，
                //      不是某一个点的巧合 ⇒ 下次谁再把压暗层的命中区提回内容那一档，这里会红。
                Section("`Booster Info Popup`：压暗层命中区**严格低于**窗内命中区（分档 ⇒ 命中唯一）");
                {
                    var layerQ = PointerLayer.Instance;
                    var darkHitN = FindPath(t, "Menu Dark Background/CloseHit");
                    var darkHitQ = darkHitN != null ? darkHitN.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(darkHitQ != null,
                              "压暗层 `CloseHit` 带 `ImageQuad`（`PointerLayer` 靠它量矩形 + 读队列）");
                    // 🆕 A47：同一条不变量的**公共断言**（唯一一份 → `MenuDraw.CheckShadeRule`）
                    //   +「这个节点确实是 `ShadeHit` 建的」。
                    //   🔴 A77⑬③：期望值改成**量**本窗那块 `Menu Dark Background` 的 quad 档
                    //      （`Shell/BoosterInfoPopup.cs` 的 `Build` 里 `Node(transform, "Menu Dark Background", …)` 那一处：`Node(...)` 建节点 + 子件 `Image` 带 quad）。
                    MenuDraw.CheckShadeRule(CheckTrue, "卡包详情窗", darkHitN,
                                            t.Find("Menu Dark Background"), BoosterInfoPopup.QHit);
                    // ⚠️ 矩形的中心一律量 **`ImageQuad` 自己**的位置 + `WorldW/H`（`RectOf` 就是这条口径）——
                    //    `MenuDraw.Hit` 把命中区那个**节点**摆在父原点，拿节点 `position` 当中心会量歪。
                    string[] innerHits =
                    {
                        "window/Generic Close Button Orange/Hit",                  // 关闭钮
                        "window/Text/Purchase buttons/Price Display/Hit",          // 价签（购买）
                        "window/Text/Purchase buttons/WebShop Button/Hit",         // `WebShop`（只出声、不跳转）
                        "window/Text/Booster pack guarantee Slider/Tooltip/Hit",   // 说明图标（悬停出 tooltip）
                    };
                    for (int i = 0; i < innerHits.Length; i++)
                    {
                        var hn = FindPath(t, innerHits[i]);
                        var wbH = hn != null ? hn.GetComponent<WindowButton>() : null;
                        var hq = hn != null ? hn.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(wbH != null && hq != null,
                                  "`" + innerHits[i] + "` 在（`WindowButton` + `ImageQuad` 都有）");
                        if (wbH == null || hq == null) continue;
                        float x1, y1, x2, y2;
                        if (!RectOf(hn, out x1, out y1, out x2, out y2))
                        { CheckTrue(false, "…它的渲染矩形量得到"); continue; }
                        float hcx = (x1 + x2) * 0.5f, hcy = (y1 + y2) * 0.5f;
                        var gotH = layerQ != null ? layerQ.ButtonAt(hcx, hcy) : null;
                        CheckTrue(gotH == wbH,
                                  "…`PointerLayer.ButtonAt` 打在它的中心 ⇒ 命中的**就是它自己**"
                                  + "（实得 `" + (gotH != null ? gotH.name : "<null>") + "`）");
                        CheckTrue(darkHitQ == null || hq.RenderQueue > darkHitQ.RenderQueue,
                                  "…它的命中队列 **>** 压暗层的（" + hq.RenderQueue + " > "
                                  + (darkHitQ != null ? darkHitQ.RenderQueue.ToString() : "?")
                                  + "）—— 唯一命中，不靠枚举顺序");
                    }
                    // 反方向：**点窗外仍然关窗**（压暗层的命中区不许低到商店页那一档 —— 那会穿透到商品格上）。
                    //   取 `(360,1010)`：在 `DarkR` 内、在 `window` 之外（窗底到 851.65）、离顶栏（y ≤ 148）很远；
                    //   ⚠️ 这个点上压着的商店页命中区最高 `ShopWindow.QCellInfoHit = 3031` ⇒ 正是要证明「压暗层在它之上」。
                    if (layerQ != null && darkHitN != null)
                    {
                        var gotD = layerQ.ButtonAt(360f, 1010f);
                        CheckTrue(gotD != null && gotD == darkHitN.GetComponent<WindowButton>(),
                                  "点**窗外**（360,1010）打到的仍是压暗层 `CloseHit`"
                                  + "（原版 `BackgroundCloseButton` ⇒ 关窗）"
                                  + "（实得 `" + (gotD != null ? gotD.name : "<null>") + "`）");
                    }
                }

                // ---- 实拍（开着的状态）----
                pop.Show(0, 0);
                Shoot("04_商店_卡包详情窗.png");

                // 🆕 A17：这一扇的换图按钮（关闭钮的**圆底** + 价签 + `WebShop`）逐个悬停验一遍
                CheckHoverSwap(pop.transform, "Booster Info Popup");

                // ---- 点窗外/关闭钮 ⇒ 关窗 ----
                var darkHit = FindPath(pop.transform, "Menu Dark Background/CloseHit");
                CheckTrue(darkHit != null, "压暗层上有 `CloseHit`（原版 `BackgroundCloseButton`）");
                if (darkHit != null) darkHit.GetComponent<WindowButton>().ClickForTest();
                Check(pop.CurrentState, WindowState.Closed, "点窗外 ⇒ 关窗");
                CheckTrue(!pop.gameObject.activeSelf, "…且节点也关了");

                // 🆕 **2026-10-06（A94 相 2）**：本窗面板底图的**吸收层**（点窗内空白处 ⇒ 原版什么都不发生）。
                //   期望矩形 = **原版 prefab** `Booster Info Popup > window >
                //   Generic Window Red Background Big` 那颗 `Image` 的 rect（395.72,178.35 → 1547.28,895.80）；
                //   ⛔ 不写 `BoosterInfoPopup.BgR`（那是被测实现**传进去的实参**，同式自证）。
                //   ⚠️ 上面那扇 `pop` 刚关（那是它自己的「点窗外 ⇒ 关窗」）⇒ 走**同一个入口**另开一扇来做这一组，
                //      上面那两条断言才是真在断那条路（把窗开回来再断会让它们变成空断）。
                var popA = pgB != null ? pgB.OpenBoosterInfo(0) : null;
                CheckTrue(popA != null && popA.CurrentState == WindowState.Open, "（A94 现场）又开出一扇卡包详情窗");
                if (popA != null)
                    CheckAbsorbRule("卡包详情窗", popA.transform, "AbsorbHit",
                                    395.72f, 178.35f, 1547.28f, 895.80f,
                                    BoosterInfoPopup.QShade, BoosterInfoPopup.QHit, () => popA.CurrentState);

                // 🆕 **2026-10-15（A796）**：压暗层「**点了会不会关**」—— 走公共口
                //   `MenuDraw.CheckShadeClickRule`（唯一一份 → `Shell/MenuDraw.cs` 的 `CheckShadeClickRule`）；
                //   逐站点表 / 与账上 24 的对账 → `资料/普查产出_1015/W7_A796调用点.md`。
                //   🔴 **本口会把窗【真的关掉】** ⇒ 必须排在**本窗其它断言之后**（这里就是本窗的收尾）；
                //      同族翻车留档 → `Editor/RewardsScene.cs` 的 `Run` 里「探针跑在关闭的窗上」那一段「探针跑在关着的窗上」⇒ ⛔ 别往上挪。
                //   ⚠️ 上面那组吸收层收尾**已经把 `popA` 点关了** ⇒ 这里先开回来（无参 `TryOpen()`：
                //      不碰 `Data`；`Closed` 支会重建内容 ⇒ 下面那颗命中区是**现取**的）。
                //   ⚠️ 本窗那颗命中区的路径是 `Menu Dark Background/CloseHit`（同 `:2125` 的 `darkHitN`）。
                CheckTrue(popA != null && popA.TryOpen(),
                          "（A796 现场）把卡包详情窗开回来 —— 下面那条要在**开着**的窗上点");
                if (popA != null)
                    MenuDraw.CheckShadeClickRule(CheckTrue, "卡包详情窗", popA.transform,
                                                 FindPath(popA.transform, "Menu Dark Background/CloseHit"),
                                                 () => popA.CurrentState);

                // 🆕 **2026-10-15（A810①）**：**卡包详情窗**那一份 `MissingArt` 也收进「0 才绿」
                //   （此前同样是「只打日志」那一档 —— `Shell/BoosterInfoPopup.cs` 只在 `Count > 0` 时出声）。
                //   📌 实读依据 = 紧接着下面那行 `pop.Dump()` 里的 `取不到的图 0 张`
                //   （`BoosterInfoPopup.Dump()`）。
                //   ⚠️ 读的是 **`pop`**（本段的被测那一扇：上面 `OpenBoosterInfo` 开的、被「点窗外 ⇒ 关窗」
                //   点关的那一扇）—— ⛔ 别改成 `popA`（那是吸收层 / 压暗层那几组另开的现场，
                //   `CheckShadeClickRule` 已把它关掉）。
                //   ⚠️ 也**不是**「没跑到」的平凡 0：`pop` 已被 `Show(0, 0)` 建过
                //   （`Build` 里 `MissingArt.Clear()` 之后每一次取图都累加），本段的几何/换图断言都跑在它身上。
                CheckNoMissingArt(pop.MissingArt, "★ 卡包详情窗");
                Debug.Log(P + "   " + pop.Dump());
            }
        }

        ClosePackAndReopenShop(win);   // A7 的购买会把商店关掉 ⇒ 实拍前先还原

        // ---------------- 🆕 2026-10-03：`Booster Pack Open Window`（§三 第 29 条 A7）----------------
        //   判据 = `资料/阶段二_商店_原版规格.md` **§五·三**（几何/依赖）+
        //          MB `MonoBehaviour_9012570135841684515.json`（窗口字段）+
        //          `Booster Window Open` clip 的**末帧关键帧**（5 张卡的位姿）+
        //          `d:/2/tools/decomp_full/BoosterPackOpenWindow__*.c`（行为）。
        //   🔴 **期望值全部盯原版**：卡位 x/scale 来自 clip（−730/−360/0/360/730 · 151），
        //      父级缩放 0.876259982585907 来自 RT 实读 ⇒ `CardK = 151 × 0.87626 = 132.3153`。
        Section("`Booster Pack Open Window`（A7；5 张卡的位姿来自原版 clip，不是我们挑的）");
        {
            win.tabButtons.Click(0);
            var pgA = win.PageOf(0);
            CheckTrue(pgA != null, "第 1 页拿得到");
            CheckTrue(pgA != null && ShopData.Offers(0)[3].Type == "Booster Pack",
                      "第 4 件是卡包（`Type == \"Booster Pack\"`）");
            int beforeA = ShopData.OwnedOf(0, 3);
            if (pgA != null) pgA.Buy(3);
            Check(ShopData.OwnedOf(0, 3), beforeA + 1, "买了第 4 件 ⇒ 拥有数 +1");

            var bp = pgA != null ? pgA.LastBoosterPack : null;
            CheckTrue(bp != null, "**买完自动开包**（入口是我们定的：`ShopTabPage.DoBuy` → `OpenBoosterPack`）");
            if (bp == null) { }
            else
            {
                var t = bp.transform;

                // ---- 窗口字段（MB 原文，逐个抄的）----
                Check(bp.type, WindowType.Fullscreen, "`type` = 0 (**Fullscreen**)（MB 原文）");
                Check(bp.placement, WindowsPlacement.Canvas,
                      "`windowsPlacement` = **5 (Canvas)**（MB 原文；⚠️ 与商店的 10 / 详情窗的 10 / 弹窗 15 **都不同**）");
                Check(bp.closeOnEsc, false, "`closeOnESC` = **0**（MB 原文）");
                CheckNear(bp.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = 1.0（MB 原文）");
                CheckTrue(t.parent != null && t.parent.name == "2 - Canvas Holder Above upper bar",
                          "挂在 **`Canvas` 锚点**下（`windowsPlacement = 5` ⇒ `2 - Canvas Holder Above upper bar`；实得 `"
                          + (t.parent != null ? t.parent.name : "<null>") + "`）");

                // ---- ① 背景：**原版那一件 `Image` 的 `m_Sprite` 是 0** ⇒ 纯色块 ----
                var bg = FindChild(t, "Booster pack Background");
                CheckAt(bg, -100f, 2020f, -100f, 1180f, "背景 `Booster pack Background`（−100,−100 → 2020,1180）");
                CheckRectPx(bg, -100f, 2020f, -100f, 1180f, "背景**渲出来**的矩形（2120×1280，四周出血 100）");
                bool hasLeg = false;
                if (bp.Cards != null)
                    for (int i = 0; i < bp.Cards.Length; i++)
                        if (BoosterPackOpenWindow.RarityInt(bp.Cards[i]) == 4) hasLeg = true;
                var bgc = TintOf(bg);
                if (hasLeg)
                {
                    // `BoosterPackBackground.thereIsALegendaryCardbackgroundColor`（实读，**红分量 >1 是原版值**）
                    CheckNear(bgc.r, 1.513579f, 0.02f, "有传奇 ⇒ 背景色 R = **1.513579**（`thereIsALegendaryCardbackgroundColor`）");
                    CheckNear(bgc.g, 0.459480f, 0.02f, "…G = **0.459480**（同上）");
                    CheckNear(bgc.b, 0f, 0.02f, "…B = **0**（同上）");
                }
                else
                {
                    CheckNear(bgc.r, 1f, 0.02f, "无传奇 ⇒ 背景色 = 那张 `Image` 的 `m_Color` **(1,1,1,1)** · R");
                    CheckNear(bgc.g, 1f, 0.02f, "…G");
                    CheckNear(bgc.b, 1f, 0.02f, "…B");
                }
                CheckNear(bgc.a, 1f, 0.02f, "…α = 1");

                // ---- ② 卡位容器 + ⑤ 5 张卡（**位姿逐个对 clip 末帧**）----
                CheckAt(FindChild(t, "Booster Animation Parent"), 910f, 1010f, 490f, 590f,
                        "`Booster Animation Parent`（100×100，中心 = 屏心）");
                CheckAt(FindChild(t, "Cards"), 910f, 1010f, 490f, 590f, "`Cards`（与父同矩形）");
                float[] wantX = { -730f, -360f, 0f, 360f, 730f };
                const float AncS = 0.876259982585907f;      // `Booster Animation Parent` 的 RT 实测
                const float CardK = 151f * AncS;            // = 132.3153（clip 末帧 scale 151 × 父级缩放）
                for (int i = 0; i < 5; i++)
                {
                    var s = FindChild(t, "CardInBoosterPack UI " + (i + 1));
                    float cx = 960f + wantX[i] * AncS;
                    CheckTrue(s != null, "第 " + (i + 1) + " 格 `CardInBoosterPack UI " + (i + 1) + "` 建了");
                    if (s == null) continue;
                    CheckNear(PxOf(s.position.x), cx, 0.6f,
                              "第 " + (i + 1) + " 格 x = **" + cx.ToString("F2") + "**（clip 末帧 x=" + wantX[i]
                              + " × 父级 scl 0.87626 + 960）");
                    CheckNear(PxYOf(s.position.y), 540f, 0.6f, "第 " + (i + 1) + " 格 y = 540（容器中心）");
                }
                // 卡背**渲出来**的尺寸 = 2.17×3.14 卡单位 × `CardK` = 287.12 × 415.47
                CheckRectPx(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 3/Cardback Container/Cardback"),
                            960f - 2.17f * CardK * 0.5f, 960f + 2.17f * CardK * 0.5f,
                            540f - 3.14f * CardK * 0.5f, 540f + 3.14f * CardK * 0.5f,
                            "第 3 格卡背渲出来的矩形（2.17×3.14 卡单位 × CardK=132.3153）");

                // ================================================================
                //  🆕 A183（2026-10-10 · W5 件①）：**卡体命中区（原版 `CardUI/2DCard/UI Collider`）的双轴比例**
                //
                //  判据 = **解包原件字段**（第一权威 · 现读现核）：`Booster Pack Open Window` 名下共 **5 颗**
                //    `UI Collider`（五张卡各一颗）—— **5 颗逐字段相同**，且与 A156 核过的战斗/菜单侧那颗**逐位一致**：
                //      · `bundle_menus_assets_all/RectTransform/RectTransform_-8719435385506612189.json`
                //        （其余四颗 pid `6429976486133054499` / `876504314555284515` / `-2166550367152489437`
                //         / `1600754383137618979`，逐字段读出来一模一样）
                //        `m_AnchorMin (0,0)` · `m_AnchorMax (1,1)`（**拉伸锚**）· `m_Pivot (0.5,0.5)`
                //        · `m_AnchoredPosition (0, −0.02)` · `m_SizeDelta (−0.2, −0.44)`
                //      · 父件 `2DCard`（`RectTransform_1205956342134619171.json`）：**`m_SizeDelta = 2.0927 × 3.3313`**
                //    ⇒ 点击区 = **(2.0927−0.2) × (3.3313−0.44)** = **1.8927 × 2.8913 卡单位** —— 那两个 `m_SizeDelta`
                //      分量是**绝对卡单位、不是百分比** ⇒ **x 比 ≠ y 比**。
                //    ⇒ × `CardK`（上面那句 `151 × 0.876259982585907` 的 `const`）= **250.4331 × 382.5631 px**，
                //      与卡心同心（第 3 格卡心 = 960,540）⇒ **四沿 = 834.7835 / 348.7184 / 1085.2165 / 731.2816**
                //    ⇒ x 比 = 1.8927/2.0927 = **0.90443** · y 比 = 2.8913/3.3313 = **0.86792**
                //
                //  ⛔ 期望值写**上面那套复算出来的原版字面量**，⛔ **不读** `BoosterPackOpenWindow.HitRatioX/Y`
                //     —— 那是被测实现传进去的实参（同式自证：改实现它照样绿）。
                //  🔴 **为什么两个比例要分开断**：改前这里是一只 `HitRatio = 0.8679`（**y** 那个比）**双轴同用**
                //     ⇒ 只断一个「合成值」的话，把 x 写回 `0.8679`、或把 y 单写对，都看不出来。
                //  **改坏法**：退回「一个比例双轴同用」⇒ 命中区宽 240.318（≠250.4331）、左沿 839.841（≠834.7835）
                //     ⇒ 下面 x 比那条 + 左沿那条**立刻红**（差 ~10px，远超容差）。
                //  ⚠️ 原版那颗还有 `m_AnchoredPosition (0, −0.02)` 的**卡单位中心下移**（战斗侧 = 5px @ scale 250）——
                //     **本窗折算成几 px 没查清**（本窗卡的渲染尺度是 `Card2DController.cardScales` /
                //     `bigSizeMultiplier` **运行期**喂的，静态读不出 ⇒ `普查产出_1009/查证V1_原版prefab四件.md` §五·1）
                //     ⇒ 我们**仍按卡心居中、那个偏置一个字没动**，所以下面四条边都是**关于卡心对称**的。
                //  ⚠️ 别与 `Tap to close/Collider`（`NonDrawingGraphic`，3853.09 × 2232.53 的**全窗吸收层**，
                //     上面单独断过）混为一谈 —— 是两件东西。
                {
                    var hq = bp.SlotHits[2] != null ? bp.SlotHits[2].GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(hq != null,
                              "第 3 格命中区**带 `ImageQuad`**（`PointerLayer` 只认它 —— 裸节点真鼠标点不动，A26）");
                    float qx1, qy1, qx2, qy2;
                    if (!RectOf(bp.SlotHits[2] != null ? bp.SlotHits[2].transform : null,
                                out qx1, out qy1, out qx2, out qy2))
                    {
                        CheckTrue(false, "第 3 格命中区的**渲染矩形量不到**（这一条测不到就等于没查）");
                    }
                    else
                    {
                        // ① 两个比例**分开断**（⛔ 别合成一个）：期望值 = 原版字面量之比，分母是**原版卡体尺寸**
                        CheckNear((qx2 - qx1) / (2.0927f * CardK), 0.90443f, 0.0005f,
                                  "★ 卡体命中区 **x 比** = **0.90443** = (2.0927−0.2)/2.0927（原版 `UI Collider` `sd(−0.2,−0.44)`）"
                                  + "（实得 " + ((qx2 - qx1) / (2.0927f * CardK)).ToString("F5") + "）");
                        CheckNear((qy2 - qy1) / (3.3313f * CardK), 0.86792f, 0.0005f,
                                  "★ …**y 比** = **0.86792** = (3.3313−0.44)/3.3313（**与 x 不是同一个数** —— 原版那两个"
                                  + " `sd` 分量是绝对卡单位）（实得 "
                                  + ((qy2 - qy1) / (3.3313f * CardK)).ToString("F5") + "）");
                        // ② 量出来的**命中区矩形**（四沿各对；期望值 = 上面那套原版字面量复算）
                        CheckNear(qx1, 834.7835f, 0.5f,
                                  "★ …第 3 格命中区**左沿** = **834.7835**（卡心 960 − 125.2165；左缩 13.2315 = 0.2 卡单位 ÷ 2 × CardK）");
                        CheckNear(qx2, 1085.2165f, 0.5f, "★ …**右沿** = **1085.2165**（右缩同上）");
                        CheckNear(qy1, 348.7184f, 0.5f,
                                  "★ …**上沿** = **348.7184**（卡心 540 − 191.2816；上缩 29.1094 = 0.44 卡单位 ÷ 2 × CardK）");
                        CheckNear(qy2, 731.2816f, 0.5f, "★ …**下沿** = **731.2816**（下缩同上）");
                    }
                }

                // ---- 出场态：**卡背开着、卡面关着、三个角标全关、两段提示字全关** ----
                for (int i = 1; i <= 5; i++)
                {
                    var bc = FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI " + i + "/Cardback Container");
                    var fd = FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI " + i + "/2DCard");
                    CheckTrue(bc != null && bc.gameObject.activeSelf, "第 " + i + " 格**卡背**开着（原版 `ChangeState(1)`）");
                    CheckTrue(fd != null && !fd.gameObject.activeSelf, "第 " + i + " 格**卡面**关着（翻之前）");
                }
                var up1 = FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Card Ready for level up");
                var nb1 = FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/New Card Badge");
                var ban1 = FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Ban Icon");
                CheckTrue(up1 != null && !up1.gameObject.activeSelf,
                          "`Card Ready for level up` 建成、出厂 **INACT**（翻牌后才按判据决定）");
                CheckTrue(nb1 != null && !nb1.gameObject.activeSelf,
                          "`New Card Badge` 建成、关着（`BasicCardUI.SetRawCardData` 一进来就 `SetActive(false)`）");
                CheckTrue(ban1 != null && !ban1.gameObject.activeSelf,
                          "`Ban Icon` 建成、**恒不显示**（`SetRawCardData` 末尾关它，翻牌链从不 `ToggleBanned`）");
                CheckArt(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Card Ready for level up/Image"),
                         "Card_Ready_For_Level_Up", "`Card Ready for level up` 的图");
                CheckArt(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Ban Icon/Image"),
                         "40k_Cross_icon_cross_big_Banned_card", "`Ban Icon` 的图");
                CheckTrue(bp.NewBadges[0] != null && bp.BanIcons[0] != null && bp.UpBadges[0] != null,
                          "三件角标都建出来了（不是没做）");
                Check(TextOf(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/New Card Badge/Text")),
                      BoosterPackOpenWindow.NewBadgeText, "`New Card Badge/Text` 的文案");
                Check(TextOf(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Ban Icon/Banned Text")),
                      BoosterPackOpenWindow.BannedText, "`Ban Icon/Banned Text` 的文案");

                // ---- ⑤⑥ 两段提示字 ----
                var disc = FindChild(t, "Tap to discover");
                var clos = FindChild(t, "Tap to close");
                CheckTrue(disc != null && !disc.gameObject.activeSelf, "`Tap to discover` 出场**关着**（原版 `OnEnable` 里 `SetActive(false)`）");
                CheckTrue(clos != null && !clos.gameObject.activeSelf, "`Tap to close` 出场**关着**（5 张全翻开才出现）");
                Check(TextOf(FindChild(disc, "Text")), BoosterPackOpenWindow.DiscoverText, "`Tap to discover` 的文本（原版出厂英文）");
                Check(TextOf(FindChild(clos, "Text")), BoosterPackOpenWindow.CloseText, "`Tap to close` 的文本（原版出厂英文）");
                CheckAt(disc, 1156.67f, 1865f, 977f, 1080f, "`Tap to discover` 的矩形（708.33×103）");
                CheckAt(clos, 1156.67f, 1865f, 977f, 1080f, "`Tap to close` 的矩形（同上）");
                var dl = FindChild(disc, "Text") != null ? FindChild(disc, "Text").GetComponentInChildren<Label>() : null;
                if (dl != null)
                {
                    CheckNear(dl.FontPxNow, 49.82f, 0.6f, "提示字字号 = **49.82**（原版 `m_fontSize`）");
                    CheckNear(dl.color.r, 0.5566f, 0.01f, "提示字色 R = **0.5566**（原版 `m_fontColor`）");
                }
                // `Tap to close/Collider`：盖满整屏那块（原版 `NonDrawingGraphic`）
                var col = FindPath(t, "Tap to close/Collider");
                // 🔴 **2026-10-04（A47 接线批）改判据**：这一块改走 `MenuDraw.ShadeHit` 之后，`Collider` 那个
                //   **节点**摆在**父原点**（`MenuDraw.Hit` 的既有摆法：节点在父原点、quad 在矩形中心 —— ⛔ 别改）
                //   ⇒ 原来那句量**节点位置**的 `CheckAt` 会量出「以父原点为中心」的**假矩形**
                //   （同 `MainMenuScene.HitQuadRect` 注释里那条「判对了实现、量错了东西」的假红）。
                //   几何改由**那颗 quad 自己**量 —— 落点在下面「全翻开 ⇒ `Tap to close` 出现」之后的那个块里
                //   （⚠️ 此处这一整棵出厂是**关着**的，`GetComponentInChildren<ImageQuad>()` 在这一刻搜不到东西）。
                CheckTrue(col != null, "`Tap to close/Collider` 节点在");
                CheckTrue(bp.CloseSurfaceHit != null, "整屏那块**带 `ImageQuad` + `WindowButton`**"
                          + "（裸节点 `PointerLayer` 收不到 —— 卡组格那颗就是这么点不动的）");

                // ================================================================
                //  🆕 **2026-10-12（A426）**：`Tap to close` 那颗字在原版**是会呼吸的**
                //   判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Booster Pack Open Window" --depth 4`
                //     的组件表 = **`TextMeshProUGUI,Localize,UIGenericEventCatcher,BlinkGraphic`**
                //     （那颗件挂在该节点**自己**身上、节点自己就是那颗字；同 rect 的兄弟 `Tap to discover`
                //      **没有**这颗件 —— ⛔ 别顺手也给那一颗接一个）；
                //     `(blinkSpeed, colorVariation) = (1.0, 0.5)` = 原版 `.ctor` 的两个立即数（36/36 个实例全没覆盖过）。
                //   公式判据 = `BlinkGraphic__Update.c`：`:17` 读时钟算 `t = Clamp01(|cos(blinkSpeed × currentTime)|)`
                //     → `:24-26` **只改 alpha** 写色 → **`:31` 才算完推进** `currentTime += deltaTime`。
                //  ⛔ **期望值一个都不读被测实现**（`Shell/BoosterPackOpenWindow.cs` 的常量 / `BlinkGraphic.Default*`）
                //     —— 全部写**原版立即数**与**手算值**。
                //  ⛔ **不断「时钟的绝对值」**（它是 `Σdt`，夹具给几拍它就几拍）—— 只断**相对关系**与**写进去的那一档**。
                var bBlk = bp.Blink;
                var bCls = bp.CloseLabel;
                CheckTrue(bBlk != null, "★ A426：`Tap to close` 那颗字**接了 `BlinkGraphic`**"
                          + "（删掉 `Shell/BoosterPackOpenWindow.cs` 的 `BindBlink(cl)` ⇒ 红）");
                CheckTrue(bCls != null, "★ …而且自检拿得到那颗字（`BoosterPackOpenWindow.CloseLabel`）");
                if (bBlk != null && bCls != null)
                {
                    CheckTrue(bBlk.blinkSpeed == 1f, "★ `blinkSpeed` = **1.0**（原版 `.ctor` 立即数 `0x3f800000`）");
                    CheckTrue(bBlk.colorVariation == 0.5f, "★ `colorVariation` = **0.5**（原版 `.ctor` 立即数 `0x3f000000`）");
                    CheckNear(bCls.color.a, 1f, 1e-4f, "★ 出厂那一档 α = **原色**（原版 `m_fontColor` 的 α = 1 —— "
                              + "`Restart()` 只取原色、**一帧都不写**；把 `Restart()` 改成也写一次色 ⇒ 这条红）");
                    // ---- 负例①：5 张没翻完 ⇒ `Tap to close` 关着 ⇒ **时钟一动不动** ----
                    CheckTrue(!clos.gameObject.activeInHierarchy, "（前提）`Tap to close` 现在**关着**（5 张还没翻完）");
                    float cB0 = bBlk.Clock;
                    bp.Tick(1f);
                    CheckNear(bBlk.Clock, cB0, 1e-6f, "★ …关着时 `Tick(1f)` ⇒ 时钟**一动不动**（= 原版那颗件随节点停；"
                              + "把 `BoosterPackOpenWindow.Tick` 里那句 `activeInHierarchy` 判断删掉 ⇒ 红）");
                }

                // ---- `timeToShowHelpText = 10`（MB 原文）：静止 10 s ⇒ `Tap to discover` 淡入 ----
                bp.AddTime(9f);
                CheckTrue(!disc.gameObject.activeSelf, "静止 **9 s** ⇒ `Tap to discover` **还没出来**（< 10 s）");
                bp.AddTime(1.5f);
                CheckTrue(disc.gameObject.activeSelf, "静止 **10.5 s** ⇒ `Tap to discover` 出现（原版 `timeToShowHelpText = 10`）");

                // ---- 稀有度 → 粒子名的映射（判据 = `CardInBoosterPack.contentByRarities[]` 的实测分组；
                //      **这一条与「导出器跑没跑」无关**，所以卡包窗一建出来就该绿）----
                Check(BoosterPackOpenWindow.CardFxFor(0), "Boosterpack Open Card Rarity 1",
                      "稀有度 0 ⇒ 粒子 `…Rarity 1`（`contentByRarities[0]` 实读）");
                Check(BoosterPackOpenWindow.CardFxFor(1), "Boosterpack Open Card Rarity 1",
                      "稀有度 1 ⇒ 同上（`contentByRarities[1]` 与 `[0]` **同一个 pid**）");
                Check(BoosterPackOpenWindow.CardFxFor(2), "Boosterpack Open Card Rarity 2",
                      "稀有度 2 ⇒ `…Rarity 2`（`contentByRarities[2]`）");
                Check(BoosterPackOpenWindow.CardFxFor(3), "Boosterpack Open Card Rarity 3",
                      "稀有度 3 ⇒ `…Rarity 3`（`contentByRarities[3]`）");
                Check(BoosterPackOpenWindow.CardFxFor(4), "Boosterpack Open Card Rarity 4",
                      "稀有度 4 ⇒ `…Rarity 4`（`contentByRarities[4]`）");
                // 角标判据的两条纯函数（**与拥有数口径无关的那部分**）
                CheckTrue(bp.Cards != null && bp.Cards.Length == 5, "这一包正好 5 张 `CardDef`");
                CheckTrue(BoosterPackOpenWindow.RarityInt(null) == 0, "`RarityInt(null)` = 0（不抛）");

                // ---- 翻牌（原版 `CardInBoosterPack.UiColliderOnClick` → `ChangeState(3)`）----
                int left0 = bp.CardsLeft;
                Check(left0, 5, "出场时 5 张都还没翻");
                CheckTrue(bp.SlotHits[0] != null, "第 1 格的命中区建了");
                if (bp.SlotHits[0] != null) bp.SlotHits[0].ClickForTest();
                CheckTrue(bp.Opened[0], "点第 1 格 ⇒ **翻了**（`Opened[0]`）");
                Check(bp.CardsLeft, 4, "…`cardsLeftToOpen` 自减（原版 `CardOpened`）");
                CheckTrue(!FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/Cardback Container").gameObject.activeSelf,
                          "…卡背关掉");
                CheckTrue(FindPath(t, "Booster Animation Parent/Cards/CardInBoosterPack UI 1/2DCard").gameObject.activeSelf,
                          "…卡面开出来");
                CheckTrue(bp.SlotHits[0] == null || !bp.SlotHits[0].gameObject.activeSelf,
                          "…翻开的卡**不再吃点击**（原版 `ChangeState` 只在该卡 state==2 时可交互）");
                CheckTrue(!disc.gameObject.activeSelf, "…`Tap to discover` 收掉（原版 `CardOpened`）");
                // 播粒子（原版 `Instantiate(contentByRarities[rarity].particles, card.transform)`）
                CheckTrue(bp.PlayedFx.Count >= 1,
                          "翻牌**播了粒子**（" + bp.PlayedFx.Count + " 次 · `" + string.Join("`,`", bp.PlayedFx.ToArray())
                          + "`）—— ⚠️ 为 0 说明 `BoosterPackExporter.Run` + `EffectLibraryBuilder.Run` 还没跑");

                // 剩下 4 张全翻 ⇒ `Tap to close` 出现（原版 `cardsLeftToOpen` 归零那条）
                for (int i = 1; i < 5; i++)
                    if (bp.SlotHits[i] != null) bp.SlotHits[i].ClickForTest();
                Check(bp.CardsLeft, 0, "5 张全翻开");
                CheckTrue(clos.gameObject.activeSelf, "全翻开 ⇒ `Tap to close` 出现");

                // ================================================================
                // 🆕 **2026-10-12（A426）**：全翻开 ⇒ `Tap to close` **亮** ⇒ 那一拍开始**真的推时钟、真的写色**
                //  ⚠️ 与上面那条负例是**一对**：只断这一头分不出「按节点开关」与「恒推」
                //     （把 `activeInHierarchy` 判断删掉 ⇒ 上面那条负例红、这两条照样绿）。
                if (bBlk != null && bCls != null)
                {
                    CheckTrue(bCls.gameObject.activeInHierarchy, "★ （前提）`Tap to close` 那颗字现在**真的亮着**");
                    // ① `Tick(0f)`：时钟 0 ⇒ `|cos 0| = 1` ⇒ α = `Lerp(A, 0.5A, 1)` = **0.5A**
                    float aB2 = bCls.color.a;
                    bp.Tick(0f);
                    CheckNear(bCls.color.a, aB2 * 0.5f, 1e-4f,
                              "★ `Tick(0f)`（时钟 0）⇒ α = **0.5 × 原色**（原色 α=1 × 原版 `colorVariation` 立即数 0.5；"
                              + "把公式里的 `cos` 写成 `sin` ⇒ 这里得**原色** ⇒ 红）");
                    // ② 时钟**走**：给多少 `dt` 就走多少（⛔ 不写死绝对值 —— 夹具给几拍它就几拍）
                    float cB1 = bBlk.Clock;
                    bp.Tick(1f);
                    CheckNear(bBlk.Clock, cB1 + 1f, 1e-4f,
                              "★ …亮起来之后 `Tick(1f)` ⇒ 时钟**走了 1 s**（`currentTime += deltaTime`，"
                              + "判据 = `BlinkGraphic__Update.c:31`；把 `Tick` 里那一句删掉 ⇒ 红）");
                }

                // 🆕 A47：压暗层命中区（整屏那块 `Tap to close/Collider`）—— 档 = `QShade`(3170)
                //   （**压暗层自己那一档**；改前是 `QCloseSurface = QBase − 1` = 3169），
                //   严格低于卡命中区最低档 `QCard`(3171) ⇒ **卡照样能点**。
                //   ⚠️ 放在这里是因为这一块出厂是关着的（`Tap to close` 关 ⇒ `GetComponentInChildren`
                //     搜不到 `ImageQuad`），全翻开之后才量得到 —— 上面那句「出场关着」已经钉住了这个前提。
                {
                    var colNow = FindPath(t, "Tap to close/Collider");
                    //   🔴 A77⑬③：期望值改成**量**本窗的**视觉底层** —— 本窗**没有**压暗层那块图
                    //      （原版 `Tap to close/Collider` 是 `NonDrawingGraphic`，什么都不画）⇒ 它该对齐的是
                    //      同一档位上那块**画出来的**底：`Booster pack Background`（`Shell/BoosterPackOpenWindow.cs` 的 `Build` 里 `Rect(…, "Booster pack Background", …)` 那一句
                    //      的 `Rect(transform, …, "Booster pack Background", QShade, …)`，**另一个对象**）。
                    MenuDraw.CheckShadeRule(CheckTrue, "开包窗（点哪儿都关那块）", colNow,
                                            t.Find("Booster pack Background"), BoosterPackOpenWindow.QCard);
                    // 几何（原版 `Collider` = 3853.09 × 2232.53 · −534.74,−204.55 → 3318.35,2027.98）：
                    //   **量那颗 quad 自己** —— `MenuDraw.Hit` 把节点摆在父原点，量节点位置是量错东西。
                    //   ⚠️ 只在这一刻量得到（`Tap to close` 开之前，`GetComponentInChildren<ImageQuad>()` 搜不到）。
                    float cox1, coy1, cox2, coy2;
                    CheckTrue(RectOf(colNow, out cox1, out coy1, out cox2, out coy2),
                              "`Tap to close/Collider` 的渲染矩形量得到（那颗 `ImageQuad`）");
                    CheckNear(cox1, -534.74f, 1f, "…左 = **−534.74**");
                    CheckNear(cox2, 3318.35f, 1f, "…右 = **3318.35**（比屏幕大一圈：3853.09 宽）");
                    CheckNear(coy1, -204.55f, 1f, "…上 = **−204.55**");
                    CheckNear(coy2, 2027.98f, 1f, "…下 = **2027.98**（2232.53 高）");
                    // 真行为：**卡命中区严格高于它**（否则点卡会被判成「点背景」⇒ 直接关窗）。
                    // ⚠️ 翻开的卡那一格命中区是 **`SetActive(false)`**（原版「翻开的卡不再吃点击」）
                    //    ⇒ 这里读它的队列要带 `includeInactive`（量的是它建出来时那个档，不是当前可见性）。
                    var colQ = colNow != null ? colNow.GetComponentInChildren<ImageQuad>(true) : null;
                    var s0q = bp.SlotHits[0] != null ? bp.SlotHits[0].GetComponentInChildren<ImageQuad>(true) : null;
                    CheckTrue(colQ != null && s0q != null,
                              "整屏那块与第 1 格卡命中区的 `ImageQuad` 都量得到（量不到这条就等于没查）");
                    CheckTrue(colQ == null || s0q == null || s0q.RenderQueue > colQ.RenderQueue,
                              "…整屏那块**严格低于**卡命中区（第 1 格 "
                              + (s0q != null ? s0q.RenderQueue.ToString() : "?") + " > 整屏 "
                              + (colQ != null ? colQ.RenderQueue.ToString() : "?")
                              + "）—— 同档时 `ImageQuad` 的 z 恒 0，点卡可能被判成点背景直接关窗");
                }

                // 点整屏 ⇒ 关（原版 `Tap to close/Collider` 的 `NonDrawingGraphic` 盖满整屏）
                if (bp.CloseSurfaceHit != null) bp.CloseSurfaceHit.ClickForTest();
                Check(bp.CurrentState, WindowState.Closed, "点整屏 ⇒ 关窗");

                // ---- 规则与缺口 ----
                bool anyRare = false;
                if (bp.Cards != null)
                    for (int i = 0; i < bp.Cards.Length; i++)
                        if (BoosterPackOpenWindow.RarityInt(bp.Cards[i]) >= 2) anyRare = true;
                CheckTrue(anyRare, "**至少一张 Rare or better**（原版出厂文案："
                          + "`At least one of the cards is guaranteed to be Rare or better`）");
                Check(bp.Cards != null ? bp.Cards.Length : 0, 5,
                      "**5 张**（原版出厂文案 `Contains 5 cards …` + prefab 里正好 5 个 `CardInBoosterPack`）");
                Check(bp.Army, "Leviathan", "第 4 件（`40K_shop_offer_booster_leviathan`）⇒ 阵营 Leviathan");
                // 🆕 2026-10-15（A810①）：走公共口 `CheckNoMissingArt`（「0 才绿」的唯一一份）
                CheckNoMissingArt(bp.MissingArt, "开包窗（这一扇）");
                Debug.Log(P + "   " + bp.Dump());
                // 收尾：这一扇已经关掉了，把商店开回来给实拍用
                ClosePackAndReopenShop(win);
            }
        }
        // ---------------- 🆕 2026-10-03（§三 第 29 条 A8）：商品条目族（19 个 `General Basic Offer Container *`）----------------
        //   判据 = `资料/阶段二_商店_原版规格.md` **§五·一**（19 件清单 / 11 种抽屉 / 三条硬限制 / 两个别照抄的坑）
        //        + 逐份实测 `python 工具/menu_dump.py bundle_menus_assets_all "<prefab 名>" --depth 3 --relative`（19 次）
        //   🔴 **期望值全部盯原版**：根尺寸 / `Dynamic Content` 矩形 / 抽屉节点名与兄弟序 / 出厂 INACT
        //      —— 一格都不是我们编的；那张表在 `Shell/OfferContainer.cs` 的 `Variants`（那边逐条写了出处）。
        //   ⚠️ 这一族**没有入口**（原版是服务端 LiveOps 报价的载体，本地一个报价数据都没有 —— 见 `OfferContainer.cs` 文件头）
        //      ⇒ 这里只断**骨架本身**，不断任何「点了会怎样」。
        //   🆕 **2026-10-04（A43）**：`OfferContainer.cs` 文件头的「三条硬限制」**第②条（哪个槽被填）当天已解出**
        //      ⇒ 选槽改成原版规则（按物品类型），断在下面那块独立的自检里；
        //      上面这个 19 份的循环改成 `fill: false`（出厂态），因为「选中就 `SetActive(true)`」之后
        //      **填过的树本来就不该**再等于出厂态（那是两件事，分开断）。
        //   🔴 **2026-10-04（F7）**：`fill: false` 让「填槽」路的覆盖**从 19 个变体掉到 9 个** ——
        //      已由 `SlotExpAll` 补足（**19 个变体每个至少一条 `Build(fill: true)` 用例**，
        //      并由 A43 块里那条**覆盖闸**钉住）⇒ 这一块只比出厂态，两边不重叠、也不留空洞。
        Section("商品条目族（A8）：19 个变体的骨架 + 抽屉槽（期望值 = 原版逐份 dump 的**字面量**）");
        {
            Debug.Log(P + "   " + OfferContainer.Dump());
            Check(OfferContainer.Variants.Length, VExpAll.Length,
                  "变体表 **19 条**（18 个 `General Basic Offer Container *` + 1 个 `Small …`）"
                  + "，与上面那份**原版字面量清单**条数相同");
            CheckTrue(!OfferContainer.Find("General Basic Offer Container", out _),
                      "**没有裸的母版文件名**（`GameObject/` 与真包里都没有 —— 正本 §五·一）");

            // 19 份全建出来，摆到**屏外**（不吃后面的实拍；断言量的是世界坐标，与在不在屏内无关）
            _offerScratch = new GameObject("OfferContainers_A8").transform;

            // 🔴 **字号标尺**（A34-F4）：`Label` 没有「传进去多少 px」的读口 ⇒ 用 `DumpSizes()` 的
            //    `fontSize=`（标称值）当读口，并**用原版 `m_fontSize` 的字面量**各建一条参考条。
            //    这 **7** 个值就是下面所有字号断言的**期望**来源 —— 全是原版 dump 里的字面量。
            //    （W1-7：原来这里的注释写「这 **6** 个值」，实际建了 7 条 —— 数一遍就现形，纯文档错。）
            // 🔴 **⚠️ 标尺挂在自己那棵 `_offerRuler` 上，⛔ 别挂回 `_offerScratch`**（2026-10-04 **W1-1 修**）：
            //    `RulerFontSize` → `MenuDraw.Text` → `Label.Create` 建出来的是宿主的**直接孩子**
            //    ⇒ 挂过去会**插队**（7 条占了下标 0..6），而 §⑥ 分档那一段按 `GetChild(0)` 取「第一个容器」
            //    ⇒ 抓到 `FontRuler_42`（**一个后代都没有**）⇒ `CheckTrue(qA != null && qC != null, …)` 必红，
            //    而且外层 `if` 不成立 ⇒ 它下面那两条队列断言被**整块跳过**。
            _offerRuler = new GameObject("OfferRulers_A34F4").transform;
            float R42 = RulerFontSize(_offerRuler, 42f), R367 = RulerFontSize(_offerRuler, 36.7f);
            float R36 = RulerFontSize(_offerRuler, 36f);
            float R34 = RulerFontSize(_offerRuler, 34f), R306 = RulerFontSize(_offerRuler, 30.6f);
            float R30 = RulerFontSize(_offerRuler, 30f), R28 = RulerFontSize(_offerRuler, 28f);
            CheckTrue(R42 > R367 && R367 > R36 && R36 > R34 && R34 > R306 && R306 > R30 && R30 > R28,
                      "字号标尺**单调**（42 > 36.7 > 36 > 34 > 30.6 > 30 > 28）—— 不然下面那一批字号断言等于没查"
                      + $"（实测 {R42:F4} / {R367:F4} / {R36:F4} / {R34:F4} / {R306:F4} / {R30:F4} / {R28:F4}）");

            int slots = 0, offSlots = 0, bgSlots = 0, bgKids4 = 0;
            // 🔴 **W1-1**：§⑥ 分档那一段要「第一个容器」—— **不按 `_offerScratch` 的孩子序号取**
            //    （那棵树的孩子序号会被别的东西插队，见上面标尺那条），改取**循环里真建出来的第一个根**。
            Transform firstRoot = null;
            // 🔴 **W1-3 覆盖度**：`Icon` 那条正方形断言按代取值 ⇒ 收尾要确认**三档都真跑到过**。
            bool[] iconGenSeen = new bool[3];
            for (int i = 0; i < VExpAll.Length; i++)
            {
                var e = VExpAll[i];
                OfferContainer.Variant v;
                if (!OfferContainer.Find(e.Name, out v))
                {
                    CheckTrue(false, $"[{i + 1}] 表里有 `{e.Name}`（原版 prefab 名）—— **找不到，跳过这一条**");
                    continue;
                }
                float bx = -8000f - (i % 5) * 500f, by = -6000f - (i / 5) * 1000f;
                var c = OfferContainer.Content.Def();
                c.Item = ItemDrawer.Spec("WildcardUltramarines1", null, "Ultramarines");  // 喂**真物品**，抽屉才画得出来
                c.Price = "1 800";
                // 🔴 **A43（2026-10-04）**：这一棵**不填槽**（`fill: false`）—— 下面 ③④⑤ 比的是 **prefab 的出厂态**
                //   （`Slots` 那个字面量串里的 `*`），而 A43 起「被选中的槽会被 `SetActive(true)`」
                //   （原版 `DrawRewards.c:363`）⇒ **填过的树本来就不该**再等于出厂态。
                //   **选槽那条规则**单独在下面那一块（A43）断，期望值 = 原版字面量。
                var b = OfferContainer.Build(_offerScratch, v, bx, by, c, 3000, null, false);
                if (firstRoot == null) firstRoot = b.Root;      // §⑥ 分档那块用（见上面声明处）
                CheckTrue(b.Root != null && b.Root.name == e.Name,
                          $"[{i + 1}] 根节点建出来了（名字 = 原 prefab 名 `{e.Name}`）");

                // ---- ① 根尺寸（**原版 dump 的字面量**）----
                CheckAt(b.Root, bx, bx + e.W, by, by + e.H, $"[{i + 1}] 根矩形 = **{e.W}×{e.H}**（原版 dump 字面量）");
                CheckRectPx(FindChild(b.Root, OfferContainer.NRaycast), bx, bx + e.W, by, by + e.H,
                            $"[{i + 1}] `raycast target` **渲出来** = 整根那么大");

                // ---- ② `Dynamic Content` 矩形（**字面量**）----
                var dyn = FindChild(b.Root, OfferContainer.NDynamic);
                CheckAt(dyn, bx + e.Dx1, bx + e.Dx2, by + e.Dy1, by + e.Dy2,
                        $"[{i + 1}] `Dynamic Content` = **({e.Dx1},{e.Dy1})→({e.Dx2},{e.Dy2})**（原版 dump 字面量）");
                CheckTrue(Mathf.Abs((e.Dx2 - e.Dx1) - 100f) < 0.01f && Mathf.Abs((e.Dy2 - e.Dy1) - 100f) < 0.01f,
                          $"[{i + 1}] …它是个 **100×100 的锚框**（原版 19 份恒为 100×100）");

                // ---- ③ 槽：**名字 + 兄弟序 + 出厂 active** 一次比完（**字面量串**，`*` = 关着）----
                string wantSlots = e.Slots;
                int wantN = wantSlots.Split('|').Length;
                Check(KidNames(dyn), wantSlots,
                      $"[{i + 1}] `Dynamic Content` 下那 {wantN} 个槽**名字 / 兄弟序 / 出厂 active 逐个对上**"
                      + "（`*` = 出厂 INACT）");
                // 🔴 **实得值从【建出来的树】数**（2026-10-04 **W1-4 修**）：这两条原来拿 `wantSlots` 自己算
                //    （`Split('|').Length` / `Split('*').Length - 1`）⇒ **期望与实得同源** ⇒ 改坏实现**永不红**
                //    （常量比常量 = 自证）。⚠️ 口径与 `KidNames` 一致：`… Gfx` 那些后代不计入。
                slots += KidNameArr(dyn).Length;
                offSlots += KidOffCount(dyn);

                // ---- ④ `background` 的孩子（**A34-F1 的抓手**）----
                //   19 份里只有 `…Variant Booster_title_resource` 那一份有第 4 个孩子（一个抽屉槽），
                //   而且它**排在 `name-bg` 之后**。这一条就是「那格挂错父节点」的探测器。
                var bgT = FindChild(b.Root, "background");
                string wantBg = "foreground|Dynamic Content|name-bg" + (e.BgSlots != null ? "|" + e.BgSlots : "");
                Check(KidNames(bgT), wantBg, $"[{i + 1}] `background` 的孩子（名字 + 顺序 + 个数）对上原版");
                // 🔴 **W1-4**：实得值**从建出来的 `background` 数** —— 三个基线孩子
                //    （`foreground` / `Dynamic Content` / `name-bg`）之外的都是**直接挂这儿的抽屉槽**。
                var bgKidArr = KidNameArr(bgT);
                for (int k = 0; k < bgKidArr.Length; k++)
                    if (bgKidArr[k] != "foreground" && bgKidArr[k] != "Dynamic Content" && bgKidArr[k] != "name-bg")
                        bgSlots++;
                if (bgKidArr.Length == 4) bgKids4++;
                // `KidNames` 把 `… Gfx` 滤掉了 ⇒ 这里补断它们的**存在**（不然「过滤」就成了把断言改软）。
                // 原版实读：`background` 的 `Image.m_Enabled` **只有 `Small`（339×390）那一份是 1**。
                CheckTrue(HasChild(bgT, "background Gfx") == (e.H < 500f),
                          $"[{i + 1}] `background Gfx`（那块实心色块）**只有 `Image.m_Enabled=1` 的那一份才画**"
                          + "（原版 19 份里只有 `Small …` 是 1）");

                // ---- ⑤ `name-bg` 的孩子序（**A34-F3 的抓手**：那颗 INACT `Image` 必须挂这儿）----
                var nbT = FindChild(b.Root, "name-bg");
                Check(KidNames(nbT), NameBgKids,
                      $"[{i + 1}] `name-bg` 的 6 个孩子（含出厂 INACT 的 `Image`）**名字 + 顺序**对上原版");
                // 同上：`KidNames` 滤掉的 `… Gfx` 要单独断（这一块是半透明黑板，19 份都有）
                CheckTrue(HasChild(nbT, "name-bg Gfx"), $"[{i + 1}] `name-bg Gfx`（α0.431 的黑板）画出来了");

                // ---- ⑤·2 `Timer` 的孩子序（🆕 2026-10-05 · 件 C 的抓手）----
                //   判据 = **原版 19/19 份** `General Basic Offer Container*` 的 `Timer` 节点 `m_Children`
                //   **有序表实读**（`RectTransform/*.json`；`GameObject/*.json` 那份是 `null`）：
                //   **`Timer Text` 在前、`Icon` 在后**，且**只有这两个**孩子。
                //   ⛔ 期望值是**原版字面量**，**不是**从 `OfferContainer.Geo` / `KidNames` 自己读回来的（那会自证）。
                //   🔴 牙口：`OfferContainer.Build` 里把那两块建反（先 `Icon` 后 `Timer Text` —— 改动前就是这样）
                //      ⇒ 这条读成 `Icon|Timer Text` ⇒ **红**。
                var tmT = FindChild(b.Root, "Timer");
                Check(KidNames(tmT), "Timer Text|Icon",
                      $"[{i + 1}] `Timer` 的孩子 = **`Timer Text` 在前、`Icon` 在后**（原版 19/19 `m_Children` 顺序）");
                // 同上：`Icon Gfx`（那只钟）被 `KidNames` 滤掉了，单独断它的**存在**（否则「过滤」= 把断言改软）
                //   ⚠️ 它是 **`Icon` 的孩子**、不是 `Timer` 的 ⇒ 必须按路径找（`HasChild` 只有一层：
                //      `Transform.Find("Icon Gfx")` 不带 `/` 时**只认直接孩子** ⇒ 那样写会拿到 null、**假红**）。
                CheckTrue(FindPath(b.Root, "Timer/Icon/Icon Gfx") != null,
                          $"[{i + 1}] `Icon Gfx`（那只钟）画出来了（在 `Timer/Icon` 下）");

                // ---- ⑥ 文字参数（**A34-F2 / F4**：字号 / 颜色 / 折行 —— 原来整段一条都没有）----
                //   期望值一律是**原版 `m_fontSize` 的字面量**（经标尺翻成同一量纲）。
                var lbName = LabelAt(b.Root, "background/name-bg/name");
                var lbType = LabelAt(b.Root, "background/name-bg/type");
                var lbAvail = LabelAt(b.Root, "background/name-bg/Available Counter");
                var lbTimer = LabelAt(b.Root, "Timer/Timer Text");
                var lbBadge = LabelAt(b.Root, "Badge/Text (TMP)");
                float rName = e.NameFs > 40f ? R42 : R367;
                CheckNear(NominalFontSize(lbName), rName, 1e-3f, $"[{i + 1}] `name` 字号 = 原版 **{e.NameFs}px**");
                // 🆕 **2026-10-12（A333 · F1 §三 #4 —— 落点由 H46 挪正到这里）**：`name` 的**自适应上限**
                //   = **42px**，**与标称那一档（36.7 / 42）不是一回事**：原版那 21 颗 `m_text='Legendary Wildcard Bundle'`
                //   的 `m_fontSizeMax` **全是 42**（3 份标称 42 + 18 份标称 36.7，逐颗实读；
                //   出处 = `资料/普查产出_1012/F1_字号线.md:64` 的 #20 行）· `m_fontSizeBase = 46`。
                //   🔴 **期望值 = 原版字面量 42**（⛔ 不回读 `OfferContainer` 里 `LabelFit(…, 42f, 46f)` 那个实参
                //      —— 那是被测实现里的值 = 自证；⛔ 也不用 `R42` 那条标尺 —— 标尺是 TMP `fontSize` 量纲，
                //      而这里量的是**画布 px**，同一份语义的两条口径别混）。
                //   ⚠️ **⛔ 这条只能挂在这里**：商店栅格里那行 `Name` 是 `Shell/ShopWindow.BuildCell` 里**我们自加**的字
                //      （上限写死 30、原版查不到 ⇒ `F1:194`）—— 挂到那儿 = 拿「我们挑的值」当「原版值」断，
                //      而且**永远量不出 42**（H46 之前就是这么错的：三页各红一次「实得 30.00」）。
                //   改坏法：把 `Shell/OfferContainer.cs` 里那处 `maxPx`（42f, 46f）实参 那两个实参（`42f, 46f`）去掉 ⇒ `LabelFit` 里 `maxPx` 退回
                //      `fontPx`（= 标称 `g.NameFs`）⇒ `Geo778`/`GeoSmall` 那 **16 份**量出 **36.7** ≠ 42 ⇒ 红
                //      （三份 `Geo391` 标称本来就是 42 ⇒ 它们那三条照样绿，判据的牙口在这 16 份上）。
                float nMaxPx = Label.FontSizeToPx(lbName != null ? lbName.FontSizeMax : 0f);
                CheckNear(nMaxPx, 42f, 0.35f,
                          $"[{i + 1}] `name` 的**自适应上限** = 原版 `m_fontSizeMax` **42px**（实得 {nMaxPx:F2}）"
                          + "—— 在**生产调用点**那个 label 上量的（`label==null` ⇒ 读出 0 ⇒ 照红，不静默）");
                CheckNear(NominalFontSize(lbType), e.TypeFs > 32f ? R34 : R30, 1e-3f,
                          $"[{i + 1}] `type` 字号 = 原版 **{e.TypeFs}px**");
                CheckNear(NominalFontSize(lbAvail), e.TypeFs > 32f ? R34 : R30, 1e-3f,
                          $"[{i + 1}] `Available Counter` 字号 = 原版 **{e.TypeFs}px**（同一份里与 `type` 恒等）");
                //   🔴 **F2 的抓手**：这一格原来对 19 份恒传 28 ⇒ 下面这条对那 9 份（30.6）会红。
                CheckNear(NominalFontSize(lbTimer), e.TimerFs > 29f ? R306 : R28, 1e-3f,
                          $"[{i + 1}] `Timer Text` 字号 = 原版 **{e.TimerFs}px**（`TypeFs={e.TypeFs}` 那一档；"
                          + $"原版 `auto[{e.TimerMin},{e.TimerMax}]`）");
                //   🆕 **2026-10-05（A50①）端到端**：下面这一对量的是**生产调用点上那个 label** 里真写进去的
                //   自适应区间 —— 共用件级的探针在 `Editor/BattleScene.cs` §4.6（量的是 `Label` 自己那条路），
                //   而上面那条只断「字号」，**断不出「上限 ≠ 字号」这件事**（34 那一支是 30.6 对 32）。
                //   🔴 期望值 = **原版 dump 的字面量** `e.TimerMin/e.TimerMax`（`VExpAll` 那张表，出处见它上面那段）
                //      —— ⛔ **不读 `OfferContainer.TimerTextFit` 的返回值**（被测实现，读它 = 自证）。
                //   🔴 牙口（改坏就红）：`TimerTextFit` 的 34 支退回 `28/18/28` ⇒ 那 9 份量成 28≠32、18≠10；
                //      `LabelFit` 那一格漏传 `tmax` ⇒ 上限量成 30.6≠32。
                float tMaxPx = Label.FontSizeToPx(lbTimer != null ? lbTimer.FontSizeMax : 0f);
                float tMinPx = Label.FontSizeToPx(lbTimer != null ? lbTimer.FontSizeMin : 0f);
                CheckNear(tMaxPx, e.TimerMax, 0.05f,
                          $"[{i + 1}] `Timer Text` 的**自适应上限** = 原版 `m_fontSizeMax` **{e.TimerMax}px**"
                          + $"（在**生产调用点**那个 label 上量的；`TypeFs={e.TypeFs}` 那一档）");
                CheckNear(tMinPx, e.TimerMin, 0.05f,
                          $"[{i + 1}] …`Timer Text` 的**自适应下限** = 原版 `m_fontSizeMin` **{e.TimerMin}px**（同上一处）");
                CheckNear(NominalFontSize(lbBadge), R36, 1e-3f,
                          $"[{i + 1}] `Badge/Text (TMP)` 字号 = 原版 **36px**（钉死 · 无 auto）");

                // ---- ⑥·d 🆕 **2026-10-05（A50⑧）**：`Badge` 九宫格的**两个端帽 / 中段**宽（**只加断言**）----
                //   判据（两条例原版依据，**都不从我们自己的常量读**）：
                //     · `WF_Special offer_Value` 的 `m_Border = (162,0,162,0)`（贴图 **324×87**，L+R = 324 = 贴图宽）；
                //     · uGUI `Image.GetAdjustedBorders`（**逐轴**：只在 `border.x + border.z **>** rect.width`
                //       时才按 `rect.width ÷ (bL+bR)` 缩角块）。
                //   那 19 份的框宽是 **339 / 391**，**都 > 324** ⇒ **端帽恒 162px**、
                //   中段取同一列纹素 = **框宽 − 324**（339 ⇒ **15**，391 ⇒ **67**）。
                //   🔴 实得值从**画出来的** 9 张 quad 量（`NineColsPx`）—— ⛔ 不回读 `OfferContainer.BadgeBorder`
                //      （那是被测的常量）、也不回读 `borderOutPx` 那种入参。
                //   🔴 牙口：`ImageQuad.CreateNineSlice` 退回「两轴共用一个比例 + `uR <= uL` 就退化成单块」
                //      那种旧写法 ⇒ 这里要么**列数不是 3**（退化单块）、要么端帽量成 `框宽÷2`（169.5 / 195.5）⇒ 红。
                {
                    var badgeGfx = FindChild(FindChild(b.Root, OfferContainer.NBadge), OfferContainer.NBadge + " Gfx");
                    float wl, wm, wr;
                    if (!NineColsPx(badgeGfx, out wl, out wm, out wr))
                        CheckTrue(false, $"[{i + 1}] `Badge Gfx` 九宫格**量不出三列**（节点在否 / 块数 / 列数不对）");
                    else
                    {
                        CheckNear(wl, 162f, 1.0f, $"[{i + 1}] `Badge` 九宫格**左端帽** = 原版 **162px**（`m_Border.x` 不缩）");
                        CheckNear(wr, 162f, 1.0f, $"[{i + 1}] `Badge` 九宫格**右端帽** = 原版 **162px**（`m_Border.z` 不缩）");
                        CheckNear(wm, e.W - 324f, 1.0f,
                                  $"[{i + 1}] …**中段** = 框宽 {e.W} − 324 = **{e.W - 324}px**"
                                  + "（原版只有左右两个端帽、中段取同一列纹素 —— 不是把端帽拉成 `框宽÷2`）");
                    }
                }
                CheckTrue(ColorEq(lbType != null ? lbType.color : Color.clear, TypeColorLit),
                          $"[{i + 1}] `type` 的颜色 = 原版 `(0.717,0.717,0.717,1)`（实得 "
                          + (lbType != null ? lbType.color.ToString() : "<没有 label>") + "）");
                CheckTrue(lbName != null && ColorEq(lbName.color, Color.white),
                          $"[{i + 1}] `name` 的颜色 = 原版**白**");
                CheckTrue(lbName != null && !lbName.Wrapping, $"[{i + 1}] `name` **不折行**（原版 `折行=0`）");
                CheckTrue(lbType != null && !lbType.Wrapping, $"[{i + 1}] `type` **不折行**（原版 `折行=0`）");
                CheckTrue(lbTimer != null && lbTimer.Wrapping, $"[{i + 1}] `Timer Text` **折行=1**（原版）");
                CheckTrue(lbBadge != null && lbBadge.Wrapping, $"[{i + 1}] `Badge/Text (TMP)` **折行=1**（原版）");

                // ---- ⑦ `WebShop` 那三件（**A34-F3**：`Highlight` / `Icon` 原来是「判据空 ⇒ 不建」）----
                //   🔴 期望值一律是**原版 dump 的字面量**（图名 / 矩形 / 缩放）—— ⛔ **不读 `OfferContainer`
                //      的常量与 `Geo`**（那是被测的表，读它 = 改坏了也绿）。
                var ws = FindPath(b.Root, "background/name-bg/WebShop Button Square Variant");
                CheckTrue(ws != null, $"[{i + 1}] `WebShop Button Square Variant` 建出来了");
                int gIx = GenOf(e.H);
                // ⚠️ 用 `Transform.Find`（**只看直接孩子**）—— `FindChild` 会一路搜子树，九宫格那 9 个子块
                //    可能撞名（本工程踩过「按名字找找到别人家」的坑）。
                var wsHi = ws != null ? ws.Find("Highlight") : null;
                var wsIc = ws != null ? ws.Find("Icon") : null;
                CheckArt(wsHi, "OctagonUI_Filled_Fade_SDF",
                         $"[{i + 1}] `WebShop/Highlight` = `OctagonUI Filled Fade SDF`"
                         + "（原版实读 · 128² · 九宫 52 · `Sliced` · α0.8 · `ppuMul 2`）");
                CheckNear(TintOf(wsHi).a, 0.8f, 1e-3f,
                          $"[{i + 1}] …它的 α = **0.8**（原版 `m_Color (1,1,1,0.8)`）");
                CheckRectPxNine(wsHi, bx + HighlightRect[gIx].x1, bx + HighlightRect[gIx].x2,
                                by + HighlightRect[gIx].y1, by + HighlightRect[gIx].y2,
                                $"[{i + 1}] …**渲出来的矩形** = ({HighlightRect[gIx].x1},{HighlightRect[gIx].y1})"
                                + $"→({HighlightRect[gIx].x2},{HighlightRect[gIx].y2})（原版 dump · 比按钮大一圈的发光）");
                CheckArt(wsIc, "40K_Icon_Discount_Gold",
                         $"[{i + 1}] `WebShop/Icon` = `40K_Icon_Discount_Gold`（原版实读 · 128² · `Simple` · `scl 1.2`）");
                // 🔴 **原版渲出来是【正方形】**（2026-10-04 **W1-3 修**）—— 原来按布局矩形的 **W**×1.2 断，
                //    宽比原版多 21%~28%，而且那条断言**恰好绿**（把错值钉死了）。判据链：
                //    · 那个节点上挂着 `AspectRatioFitter`（原始 MB `MonoBehaviour_8337996828984527321`）=
                //      `m_Enabled 1` · **`m_AspectMode 2 (HeightControlsWidth)`** · **`m_AspectRatio 1.0`**（19 份全同）；
                //    · uGUI `AspectRatioFitter.cs` 的 `case AspectMode.HeightControlsWidth:`
                //      → `rectTransform.SetSizeWithCurrentAnchors(Horizontal, rectTransform.rect.height * m_AspectRatio)`
                //      ⇒ **宽 := 高**（`pivot (0.5,0.5)` ⇒ 以**中心**为轴改宽，高与中心都不动）；
                //    · 它的 RT（`RectTransform_3907242956008901081`）= 纵向拉满（`anchorMin.y 0` / `anchorMax.y 1`）
                //      + `m_SizeDelta.y = −7.5547` ⇒ **高 = `WebShop` 的高 − 7.5547**；
                //      再乘节点自己的 `m_LocalScale 1.2` ⇒ **渲染边长 = 那个正方形 ×1.2**。
                //    ⚠️ **别照抄 dump 的布局宽**（57.78 那一格）：`工具/menu_dump.py` **不模拟 `AspectRatioFitter`**
                //      ⇒ dump 给的是**序列化值**、不是运行时值 —— 铁律 5·c「一个值 ≠ 全部情况」。
                //      （同一条纪律在 `OfferContainer.cs` 文件头「两个别照抄的坑」② 已经对**抽屉**用过一次。）
                //    期望值 = 上面 `IconSquare[]`（**独立复算**，⛔ 不读 `OfferContainer` 的常量/`Geo`）。
                if (wsIc != null)
                {
                    iconGenSeen[gIx] = true;                   // 三档覆盖度（见循环外那一条）
                    var q = wsIc.GetComponentInChildren<ImageQuad>();
                    float sq = IconSquare[gIx] * 1.2f;         // `1.2f` = 原版 `m_LocalScale` 字面量
                    CheckNear(q != null ? q.WorldW * 108f : -1f, sq, 1.5f,
                              $"[{i + 1}] …`Icon` 渲出来的**宽 = 布局高 × 1.2**（`AspectRatioFitter` ⇒ 宽 := 高）");
                    CheckNear(q != null ? q.WorldH * 108f : -1f, sq, 1.5f,
                              $"[{i + 1}] …`Icon` 渲出来的**高 = 布局高 × 1.2**（`m_LocalScale`；与上一条同值 ⇒ **正方形**）");
                    // 中心：那条 `AspectRatioFitter` 只改宽（`SetSizeWithCurrentAnchors(Horizontal, …)`
                    // + `pivot 0.5`）⇒ **中心仍 = 布局矩形的中心**。期望值 = `IconRect[]` 字面量。
                    CheckAt(wsIc, bx + IconRect[gIx].x1, bx + IconRect[gIx].x2,
                            by + IconRect[gIx].y1, by + IconRect[gIx].y2,
                            $"[{i + 1}] …它的**中心** = 布局矩形的中心（`HeightControlsWidth` 只改宽、不动中心）");
                }
                CheckTrue(ws != null && ws.Find("Image") == null,
                          $"[{i + 1}] 那颗出厂 INACT 的 `Image` **不是 `WebShop` 的直接孩子**（它在 `name-bg` 下）");

                // ---- ⑧ 「哪个槽被填」----
                //   🔴 **A43（2026-10-04）搬走了**：这里原来按「兄弟序里第一个非 INACT 的槽」(`FirstActiveIndex`)
                //   断 —— 那是**我们挑的启发式**，原版规则当天解出（按物品类型选，判据齐全）
                //   ⇒ 换到**本 Section 末尾**那块（`Section("商品条目族（A43）…")`）里断，
                //   **期望值 = 原版字面量**（类型名 / 槽名 / 类名）。
                //   ⚠️ **不能留在这里**：这一棵现在**不填槽**（见上面 `fill: false` 那条注释）。
            }
            // 这两个总数**由 19 份 dump 逐份数出来**（3+6+6+2+3+4+3+3+2+3+5+7+4+3+5+12+3+6+7 = **87**；
            // INACT 1+1+8+6 = 16）—— 钉在这里，改表时会被强制看见。
            // 🔴 **88 → 87**（A34-F1）：`…Variant Booster_title_resource` 那一格**不在 `Dynamic Content` 下**。
            // 🔴 **W1-4 修**：期望值 = 下面那串字面量（原版逐份数出来的）；**实得值 = 从【建出来的树】数**
            //    （`KidNameArr` / `KidOffCount` / `background` 的直接孩子）—— 原来是**两边同源**（都从
            //    `VExpAll` 的字面量串算）⇒ 改坏实现**永不红**，那是自证。现在这一半**能红**。
            Check(slots, 87, "19 个变体的**`Dynamic Content` 抽屉槽合计 87 个**（期望 = 原版逐份数出来；实得 = 数建出来的树）");
            Check(offSlots, 16, "其中**出厂 INACT 合计 16 个**（期望 = 原版逐份数；实得 = 数建出来的树里 `activeSelf=0` 的槽）");
            Check(bgSlots, 1, "**直接挂在 `background` 下的槽合计 1 个**（期望 = 原版：只有 `…Booster_title_resource` 那一份有；"
                              + "实得 = 数建出来的 `background` 里 `foreground`/`Dynamic Content`/`name-bg` 之外的直接孩子）");
            Check(bgKids4, 1, "…`background` 有 **4 个孩子**的变体也只 1 份（其余 18 份都是 3 个；实得 = 数建出来的孩子数）");
            // 🔴 **W1-3 覆盖度**：`Icon` 那两条正方形断言按**代**取值（`IconSquare[gIx]`）—— 收尾要确认
            //    **三档都真的跑到过**（`GenOf` 万一将来只落一档，那另外两档的期望值就**从没被比过**）。
            CheckTrue(iconGenSeen[0] && iconGenSeen[1] && iconGenSeen[2],
                      "`Icon` 正方形那两条断言**三档（391×930 / 339×778 / 339×390）都真的跑到过**"
                      + $"（实得 {iconGenSeen[0]}/{iconGenSeen[1]}/{iconGenSeen[2]}）");

            // ---- ⑥ 分档：`WebShop` 那颗命中区**必须排在整卡命中区之上**（否则它点不动）----
            //   判据 = `CLAUDE.md` §三「分层要用渲染队列，不能用 z」+ `PointerLayer` 取队列最高的那条。
            //   （2026-10-03 在 `BoosterInfoPopup` 上刚栽过一次：压暗层与窗内四颗同档 ⇒ 那四颗点不动。）
            {
                // 🔴 **W1-1 修**：取**循环里真建出来的第一个根**，⛔ **不按 `_offerScratch.GetChild(0)` 取** ——
                //    那棵树的孩子序号会被别的东西（如字号标尺）插队，届时这里会**静默抓错节点**。
                var root0 = firstRoot;
                CheckTrue(root0 != null,
                          "拿到了 19 个容器里的**第一个根**（`Build` 的返回值 —— 不靠 `_offerScratch` 的孩子序号）");
                var cardsHit = FindChild(root0, OfferContainer.NRaycast);
                var wsHit = FindPath(root0, OfferContainer.NBackground + "/" + OfferContainer.NNameBg + "/"
                                          + OfferContainer.NWebShop + "/Hit");
                var qA = cardsHit != null ? cardsHit.GetComponentInChildren<ImageQuad>() : null;
                var qC = wsHit != null ? wsHit.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(qA != null && qC != null,
                          "整卡那句 `raycast target` 与 `WebShop` 那颗**两条命中区都建了**（各带 `ImageQuad`）");
                if (qA != null && qC != null)
                    CheckTrue(qC.RenderQueue > qA.RenderQueue,
                              "`WebShop` 命中队列 **>** 整卡 `raycast target` 的"
                              + $"（{qC.RenderQueue} > {qA.RenderQueue}）—— 不然那颗被整卡盖住、**点不动**");
                // 🆕 A34-F3：`WebShop` 那一窝现在**三层都画得出来**（原版兄弟序 `Highlight` → `Button Image`
                //   → `Icon`）⇒ **必须各占一档**（同档 = 谁压谁不定，`CLAUDE.md` §三那条）。
                //   还要**都高过那两行字**：原版 `name-bg` 的孩子序里 `WebShop` 在 `name`/`type` 之后。
                var hiT = FindPath(root0, "background/name-bg/WebShop Button Square Variant/Highlight");
                var biT = FindPath(root0, "background/name-bg/WebShop Button Square Variant/Button Image");
                var icT = FindPath(root0, "background/name-bg/WebShop Button Square Variant/Icon");
                int qHi = QOf(hiT), qBi = QOf(biT), qIc = QOf(icT);
                // 🔴 **W1-2 修**：这里原来写的是 `qHi > qBi && qBi > qIc && qIc > qC.RenderQueue` ——
                //    **比较方向整个反了**（它自己的消息、实现那三个常量、`OfferContainer.cs:263-269` 的注释
                //    三处都是 `<`）。实得 `qHi/qBi/qIc/qC = 3006/3007/3008/3010` ⇒ 原来那条**必红**。
                //    队列读数都是 `QOf(...)` 从**真建出来的 `ImageQuad`** 上读的（`q.RenderQueue`），
                //    不是拿实现里的常量当期望值 —— 这一点保持不动。
                CheckTrue(qHi < qBi && qBi < qIc && qIc < qC.RenderQueue,
                          "`Highlight` < `Button Image` < `Icon` < `Hit` **四档严格递增**"
                          + $"（{qHi} < {qBi} < {qIc} < {qC.RenderQueue}）—— 同档 = 谁压谁不定");
                CheckTrue(qHi > 3000 + 4,
                          $"`Highlight` 的队列 **> `name`/`type` 那两行字（{3000 + 4}）**（实得 {qHi}）"
                          + " —— 原版 `name-bg` 里 `WebShop` 排在 `name`/`type` 之后 ⇒ 那圈光盖在字上");
            }

            // ============================================================ 🆕 A43：**按物品类型选槽**
            //   期望值一律是**原版字面量**（类型名 / 槽节点名 / 类名 / 「一个都不填」）—— 逐行依据见 `SlotExpAll` 的 `Why`；
            //   ⛔ 本块**不读** `ItemDrawer.ItemTypeSets` / `OfferContainer.SlotTypes` 里任何一格的**值**（那两张表是被测对象），
            //     只调它的**查表接口**（`DrawerClassOf` / `Find` / `PickSlot`）—— 「表本身对不对」由上面那些字面量钉住。
            Section("商品条目族（A43）：按物品类型选槽（期望值 = 原版类型名 / 槽名 / 类名的字面量）");
            {
                Debug.Log(P + "   A43：按物品类型选槽 —— 类型表 " + ItemDrawer.ItemTypeSets.Length + " 条 / 槽类表 "
                          + OfferContainer.SlotTypes.Length + " 条 / 用例 " + SlotExpAll.Length + " 条");

                // （a）**覆盖面**：期望集从**本文件那张原版字面量表**（`VExpAll`）数出来 —— 两个方向都对一下。
                var wantNames = new HashSet<string>();
                for (int q = 0; q < VExpAll.Length; q++)
                {
                    if (!string.IsNullOrEmpty(VExpAll[q].Slots))
                        foreach (var nm in VExpAll[q].Slots.Split('|'))
                            wantNames.Add(nm.StartsWith("*") ? nm.Substring(1) : nm);
                    if (!string.IsNullOrEmpty(VExpAll[q].BgSlots)) wantNames.Add(VExpAll[q].BgSlots);
                }
                Check(OfferContainer.SlotTypes.Length, wantNames.Count,
                      "槽类表的**条数** = 19 份里出现过的**不同槽名**数（期望集数自本文件的原版字面量表 `VExpAll`；"
                      + "⛔ 不从实现读）");
                var noClass = new List<string>();
                foreach (var nm in wantNames) if (OfferContainer.DrawerClassOf(nm) == null) noClass.Add(nm);
                Check(string.Join("、", noClass.ToArray()), "",
                      "19 份里出现过的槽名**每一个都查得到类**（查不到的列在左边 —— 空串 = 一个都不缺）");
                var stale = new List<string>();
                for (int q = 0; q < OfferContainer.SlotTypes.Length; q++)
                    if (!wantNames.Contains(OfferContainer.SlotTypes[q].Slot)) stale.Add(OfferContainer.SlotTypes[q].Slot);
                Check(string.Join("、", stale.ToArray()), "",
                      "槽类表里**没有多余 / 过期的槽名**（多一条也是错 —— 两个方向都断，才算覆盖住了）");
                // 名字 ≠ 类的那两格（**原版 prefab 逐份实读的字面量**）
                Check(OfferContainer.DrawerClassOf("Deck Drawer"), "DeckAndCardbackDrawer",
                      "`Deck Drawer` 那一格挂的是 **`DeckAndCardbackDrawer`**（不是同名的 `DeckDrawer` prefab）"
                      + " —— 这正是原版必须用 is-a 而不是等号的原因");
                Check(OfferContainer.DrawerClassOf("Icon Avatar Border Drawer"), "AvatarBorderDrawer",
                      "`Icon Avatar Border Drawer` 挂的是 `AvatarBorderDrawer`（⚠️ 这一格**不在 `ItemDrawerConfig` 那张表里**，"
                      + "类是从它自己身上读的）");
                CheckTrue(OfferContainer.DrawerClassOf("Wildcard Drawer") == null,
                          "槽名不在表里 ⇒ `null`（不是静默给个默认类）—— 这条同时证明上面两条**不是恒真**");

                // 🔴 **`OfferPopups`(30) 那一档**必须单独钉：只靠上面那些「填哪个槽」的用例**抓不到这一档改错** ——
                //    因为 `CosmeticItemTitle` 的主档类是 `TitleDrawer`，而槽是它的**子类** `TitleDrawerHorizontal`
                //    （`is-a` 兜底照样会命中）⇒ 拿主档顶替、画面上**看不出差别**。期望值 = 映射表那一行的字面量。
                string dummy;
                Check(OfferContainer.ResolveDrawerClass("CosmeticItemTitle", DrawerOverride.OfferPopups, out dummy),
                      "TitleDrawerHorizontal",
                      "映射表 :60 —— `CosmeticItemTitle` 在 `OfferPopups`(30) 这一档写的是 `Title Drawer Horizontal Variant`"
                      + "（类 `TitleDrawerHorizontal`）");
                Check(OfferContainer.ResolveDrawerClass("CosmeticItemTitle", DrawerOverride.Default, out dummy),
                      "TitleDrawer",
                      "…而**主档**（`override == 0`）是 `Title Drawer`（类 `TitleDrawer`）—— 两者**不同** ⇒ 上面那条不是恒真"
                      + "（也正是「靠 is-a 兜底会看不出来」的原因）");
                Check(OfferContainer.ResolveDrawerClass("Currency", DrawerOverride.OfferPopups, out dummy),
                      "CurrencyDrawer",
                      "映射表 §⑤.2 —— `Currency` **没写** 30 ⇒ 这一档**回落主档**（类仍是 `CurrencyDrawer`）");
                // 🆕 **2026-10-03（A79②）：四档 override 抄齐了** —— 这一格原来是**正经守卫**
                //   （「本表**还没抄** `Icon`(10) ⇒ 返回 `null` + 出声，⛔ 不拿主档顶」），四档进表之后它必然失效
                //   ⇒ **同批翻成正値断言**（期望值 = 映射表 §② 那一列的字面量）。**只改表不改这几条 = 这一块红。**
                Check(OfferContainer.ResolveDrawerClass("CosmeticItemTitle", DrawerOverride.Icon, out dummy),
                      "TitleIconDrawer",
                      "映射表 :58 —— `CosmeticItemTitle` 在 `Icon`(10) 这一档写的是 `Icon Title Drawer Variant`"
                      + "（类 `TitleIconDrawer`；⚠️ **与主档 `TitleDrawer` 不是同一个类** ⇒ 拿主档顶会被这条抓出来）");
                Check(OfferContainer.ResolveDrawerClass("CosmeticItemTitle", DrawerOverride.Horizontal, out dummy),
                      "TitleDrawerHorizontal",
                      "映射表 :59 —— `Horizontal`(15) 那一档 = `Title Drawer Horizontal Variant`（类 `TitleDrawerHorizontal`）");
                Check(OfferContainer.ResolveDrawerClass("PlayerAvatar", DrawerOverride.Shop, out dummy),
                      "AvatarDrawer",
                      "映射表 :41 —— `PlayerAvatar` 在 `Shop`(20) 这一档写的是 `Avatar Drawer Shop Variant`（类 `AvatarDrawer`）");
                Check(OfferContainer.ResolveDrawerClass("ForgePoints", DrawerOverride.Icon, out dummy),
                      "ForgePointIconDrawer",
                      "映射表 :53 —— `ForgePoints` 的 `Icon`(10) 档是 `ForgePointIconDrawer`，"
                      + "**与它的主档 `ForgePointDrawer` 不同** ⇒ 这一列要是照主档抄，这条就红");
                Check(OfferContainer.ResolveDrawerClass("RawCardScript", DrawerOverride.Icon, out dummy),
                      "CardDrawer",
                      "🔴 **按键找不到 ⇒ 回落主档**（`GetDrawer.c:23-33` / `GetReference.c:62-72`）——"
                      + "映射表 :44 里 `RawCardScript` **只写了主档**，所以 `Icon`(10) 这一档回落的仍是 `CardDrawer`"
                      + "（⛔ 这条**不是**「返回 `null`」：原版本来就会回落，`null` = 我们比原版少画）");
                // 🔴 **逐格全覆盖**（期望值 = 本文件的**原版字面量表** `DrawerAtOv`，⛔ 不从 `ItemTypeSets` 读回来）：
                //   `Icon`(10) / `Horizontal`(15) / `Shop`(20) 三档 **16 条一条不少** —— 手抄的表一旦抄错一格，这里就红。
                for (int q = 0; q < DrawerAtOv.Length; q++)
                {
                    var rowOv = DrawerAtOv[q];
                    int barOv = rowOv.Type.LastIndexOf('|');
                    string tyOv = rowOv.Type.Substring(0, barOv);
                    var ovOv = (DrawerOverride)int.Parse(rowOv.Type.Substring(barOv + 1));
                    Check(OfferContainer.ResolveDrawerClass(tyOv, ovOv, out dummy), rowOv.Cls,
                          $"映射表 §② —— `{tyOv}` 在 `{ovOv}`({(int)ovOv}) 这一档 ⇒ 类 `{rowOv.Cls}`");
                }
                Check(DrawerAtOv.Length, 16,
                      "三档的用例数 = 映射表里 `Icon`(10) 13 + `Horizontal`(15) 1 + `Shop`(20) 2 = **16 条**"
                      + "（少一条 = 上面那圈没盖全；⚠️ 30 档那 3 条由 `DrawerAt30` 钉）");

                // ============================================================ 🆕 A79①：**真表那一跳**（`PickDrawer` 走哪条路）
                //   🔴 为什么要这四条：`PickDrawer` 是「**真表 → 兜底**」两条路，而**兜底对野牌那两档与真表同结果**
                //   （`OverrideDrawer(Wildcard, Icon)` = `WildcardIconDrawer`）⇒ 把真表那一跳**整段删掉**，
                //   别的断言**照样全绿**（前三条也会绿）。**只有第 4 条**（`via` 以「走**真表**」开头且**不含「兜底」**）
                //   能钉住「这一票是**哪条路**答的」⇒ 它就是这四条里唯一不可替代的那条。
                Check(ItemDrawer.ItemTypeOf(ItemDrawer.Spec("WildcardUltramarines1", null, null)), "Wildcard",
                      "A79①-1：`ItemDrawer.Spec` 从 id 填的**原版类型**（= 查真表的键）—— 期望值是原版类名 `Wildcard`"
                      + "（`bundle_menus_assets_all/MonoBehaviour/WildcardUltramarines1.json` 的 `m_Script` 解出的类）");
                Check(ItemDrawer.ImplOfDrawerClass("WildcardDrawer"), ItemDrawer.DrawerWildcard,
                      "A79①-2：**原版抽屉类 → 我们的实现**（左 = 原版 `WildcardDrawer : ItemDrawer<Wildcard>`，右 = 我们的常量）");
                Check(ItemDrawer.ImplOfDrawerClass("TitleIconDrawer"), null,
                      "A79①-2b：**判据有、实现没有的抽屉类 ⇒ `null`**（`TitleIconDrawer` 在映射表里、我们没实现它）"
                      + " —— 这条同时证明上面那条**不是恒真**（表若写成「有类名就给实现」它会红）");
                string viaA79;
                Check(ItemDrawer.PickDrawer(ItemDrawer.Spec("WildcardUltramarines1", null, null),
                                            DrawerOverride.Default, out viaA79),
                      ItemDrawer.DrawerWildcard, "A79①-3：野牌**主档** ⇒ `WildcardDrawer`（= 原版那个类的名字）");
                CheckTrue(viaA79.StartsWith("走**真表**") && !viaA79.Contains("兜底"),
                          "A79①-4：而且**答这一票的确实是「真表那一跳」**（`via` 以「走**真表**」开头、且**不含「兜底」**）"
                          + " —— ⛔ 不是兜底恰好撞对：**删掉真表那一跳这条就红**（上面那几条会照旧全绿）。via = " + viaA79);
                // `Wildcard` 那一档的**类型名**是从物品自己推得出来的（唯一一条有判据的）
                Check(OfferContainer.TypeOfKind(ItemKind.Wildcard), "Wildcard",
                      "`ItemKind.Wildcard` ⇒ 原版类型 `Wildcard`（`ItemDrawer.Spec` 认出的野牌，其 SO 就是 `Wildcard` 这个类）");
                CheckTrue(OfferContainer.TypeOfKind(ItemKind.Generic) == null && OfferContainer.TypeOfKind(ItemKind.Unknown) == null,
                          "其余 `ItemKind` ⇒ **判据空**（`Generic`/`Unknown` 是**我们**的枚举、不是原版的类型 —— 不猜）");

                // 🔴 **（a2）覆盖闸（2026-10-04 · F7）**：**19 个变体每一个都必须有一条「真跑 `Build(fill: true)`」的用例**
                //   —— A43 把 §⑥ 那 19 份循环改成 `fill: false` 之后，填槽路**只剩 9 个变体**在跑（覆盖空洞）。
                //   期望集 = 本文件那张原版字面量清单（`VExpAll`）；实得 = `SlotExpAll` 里 `Ordinal == 0` 那一批
                //   （`Ordinal != 0` 那些走 `PickSlot`、不建树 ⇒ 不算「跑过填槽」）。**少一行就在这里红。**
                {
                    var filledVar = new HashSet<string>();
                    for (int k = 0; k < SlotExpAll.Length; k++)
                        if (SlotExpAll[k].Ordinal == 0) filledVar.Add(SlotExpAll[k].Variant);
                    var noFill = new List<string>();
                    for (int q = 0; q < VExpAll.Length; q++)
                        if (!filledVar.Contains(VExpAll[q].Name)) noFill.Add(VExpAll[q].Name);
                    Check(string.Join("、", noFill.ToArray()), "",
                          $"19 个变体**每一个都有一条填槽用例**（`Build(fill: true)`；没覆盖到的列在左边 —— 空串 = 一个不缺）"
                          + $"（实得覆盖 {filledVar.Count} / {VExpAll.Length}）");
                }

                // （b）逐条用例：**填哪个槽**（`Build(fill: true)` 走的就是 `ordinal = 0`）
                for (int k = 0; k < SlotExpAll.Length; k++)
                {
                    var row = SlotExpAll[k];
                    OfferContainer.Variant v;
                    if (!OfferContainer.Find(row.Variant, out v))
                    { CheckTrue(false, $"[A43-{k + 1}] 变体 `{row.Variant}` 在实现那张表里找得到"); continue; }

                    // 期望的**池下标**（**从本文件那张原版字面量 `VExpAll` 算**，⛔ 不从实现算）：
                    //   `Dynamic Content` 那批按兄弟序在前、`background` 下那一格排在它们**之后**。
                    //   顺带把 `VExpAll` 那张表的**槽序**也当成了判据（槽序错 ⇒ 下标错 ⇒ 红）。
                    int wantPool = -1;
                    {
                        // ⚠️ 变量名避开外层那个 `slots`（A8 那个「合计槽数」计数器）—— 同名会 CS0136。
                        string litSlots = null, litBg = null;
                        for (int q = 0; q < VExpAll.Length; q++)
                            if (VExpAll[q].Name == row.Variant) { litSlots = VExpAll[q].Slots; litBg = VExpAll[q].BgSlots; break; }
                        if (litSlots != null)
                        {
                            var arr = litSlots.Split('|');
                            for (int q = 0; q < arr.Length; q++)
                                if ((arr[q].StartsWith("*") ? arr[q].Substring(1) : arr[q]) == row.Slot) wantPool = q;
                            if (wantPool < 0 && !string.IsNullOrEmpty(litBg))
                            {
                                var bgArr = litBg.Split('|');
                                for (int q = 0; q < bgArr.Length; q++) if (bgArr[q] == row.Slot) wantPool = arr.Length + q;
                            }
                        }
                    }

                    if (row.Ordinal != 0)
                    {
                        // `Build` 走的是第 1 个同类项（`ordinal = 0`）⇒ 这一行**只**用 `PickSlot` 的次序接口比，
                        // 并顺带证明「`ordinal 0` 与 `ordinal 1` 落的是**两个不同的**槽」（否则这条等于没查）。
                        var pk = OfferContainer.PickSlot(v, row.ItemType, DrawerOverride.OfferPopups, row.Ordinal);
                        var pk0 = OfferContainer.PickSlot(v, row.ItemType, DrawerOverride.OfferPopups, 0);
                        Check(OfferContainer.SlotNameAt(v, pk.PoolIndex), row.Slot,
                              $"[A43-{k + 1}] `{row.ItemType}` 的**第 {row.Ordinal + 1} 个** ⇒ 填 **{row.Slot}**（{row.Why}）");
                        CheckTrue(pk.PoolIndex != pk0.PoolIndex,
                                  $"[A43-{k + 1}] …而第 1 个落的是**另一个**槽"
                                  + $"（`{OfferContainer.SlotNameAt(v, pk0.PoolIndex)}` ≠ `{OfferContainer.SlotNameAt(v, pk.PoolIndex)}`）");
                        // F9：`Pick.DrawerClass` 原来**没有读者**（只有 `Built.DrawerClass` 转抄一次）——
                        //   期望值 = 本文件那张**原版字面量表**（`DrawerAt30`，映射表 ②/§⑤.2）。
                        Check(pk.DrawerClass, ClassAt30(row.ItemType),
                              $"[A43-{k + 1}] …且解出的抽屉类 = **`{ClassAt30(row.ItemType)}`**"
                              + "（本文件 `DrawerAt30`：映射表里 override 30 这一档的字面量；表里没有 ⇒ 期望 `null`）");
                        continue;
                    }

                    // 摆**屏外**，且**避开上面那 19 份占的 x 区间**（`-10000..-8000`）—— 免得将来谁去
                    // `_offerScratch` 里按矩形找节点时抓错（本工程的「按名字/按位置抓错」踩过多次）。
                    float bx = -11000f - (k % 5) * 460f, by = -4000f - (k / 5) * 1000f;
                    var c = OfferContainer.Content.Def();
                    // 🔴 **2026-10-04（复跑时 `A43-17` 红了，就地订正 = 夹具自相矛盾，实现是对的）**：
                    //   第 17 行（`ItemType` 空那一行）的前提写的是「`ItemType` 空**且 `Item.Kind` 也推不出**」，
                    //   而这里原来**一律**喂 `Spec("WildcardUltramarines1", …)` ⇒ `Kind = Wildcard`
                    //   ⇒ 实现按**原版那条路**（`item.GetType()`）从物品推出类型 `Wildcard`
                    //   （`OfferContainer.TypeOfKind`，**有判据、不是猜**）⇒ 解出 `WildcardDrawer` ——
                    //   **即实现照原版做了、是这一行的夹具没满足它自己的前提**。
                    //   ⇒ `ItemType` 为空的行改用「**认不出的 id + 无图**」⇒ `Kind = Unknown` ⇒ `TypeOfKind` → `null`
                    //   （诚实负例：两条判据都空 ⇒ 一个槽都不填 + 出声）。
                    c.Item = string.IsNullOrEmpty(row.ItemType)
                           ? ItemDrawer.Spec("__no_drawer__", null, "（判据空）")
                           : ItemDrawer.Spec("WildcardUltramarines1", null, "Ultramarines");  // 喂真物品 ⇒ 抽屉内部画得出来
                    c.ItemType = row.ItemType;
                    c.Price = null;
                    var b = OfferContainer.Build(_offerScratch, v, bx, by, c, 3000, null, true);

                    Check(b.Filled != null ? b.Filled.name : null, row.Slot,
                          $"[A43-{k + 1}] `{row.ItemType}` @ `{row.Variant}` ⇒ 填的槽 = **"
                          + (row.Slot ?? "（一个都不填）") + "**（" + row.Why + "）");
                    // 🔴 **F9**（2026-10-04）：`Built.FilledPool` / `Built.DrawerClass` 原来**全库没有读者**
                    //   （审查代理 R-X1 查出：`FilledPool` 的默认值 `0` 还是个**合法池下标** ⇒ 与「没填」不可分）。
                    //   现在给它们**两个真读者**：下标按上面那张**原版字面量**算，类按 `DrawerAt30` 算。
                    Check(b.FilledPool, row.Slot != null ? wantPool : -1,
                          $"[A43-{k + 1}] …`Built.FilledPool` = **{(row.Slot != null ? wantPool.ToString() : "-1（没填）")}**"
                          + "（期望值 = 该槽在本文件 `VExpAll` 那张原版字面量里的**池序下标**；没填 ⇒ `-1`）");
                    Check(b.DrawerClass, ClassAt30(row.ItemType),
                          $"[A43-{k + 1}] …`Built.DrawerClass` = **`{ClassAt30(row.ItemType) ?? "<null：期望就是没解出>"}`**"
                          + "（本文件 `DrawerAt30` = 映射表 override 30 档那一列的字面量）");
                    // 🔴 **F3**（2026-10-04）：原版第①步是**池里每一个先 `SetActive(false)`**（`DrawRewards.c:112`），
                    //   命中的那个再被 `:363` 打开 ⇒ **填完之后开着的槽只有命中的那一个**（没命中 ⇒ 一个都不开）。
                    //   ⚠️ 这条**专抓「整池没全关」**：出厂的 ACTIVE 槽若没被关，实得会多出名字来
                    //   （例：`…Single Item Type` 那 12 槽里 4 个 ACTIVE、`Small …` 那 7 槽里 1 个 ACTIVE）。
                    Check(ActiveSlotNames(b.Root), row.Slot ?? "",
                          $"[A43-{k + 1}] …填完之后**开着的槽只有命中的那个**（原版 `:112` 整池先全关 + `:363` 开命中那个）"
                          + "（没命中 ⇒ 空串）");
                    // 整棵容器里**只有一个** `Item Drawer`（被选中那个）—— 其余槽（`Dynamic Content` 那批 + `background` 那格）都是空的
                    Check(CountByPrefix(b.Root, "Item Drawer"), row.Slot != null ? 1 : 0,
                          $"[A43-{k + 1}] …整棵容器里的 `Item Drawer` **恰好 {(row.Slot != null ? 1 : 0)} 个**（其余槽一个都没被填）");
                    if (row.Slot != null)
                    {
                        Check(b.Filled != null && b.Filled.parent != null ? b.Filled.parent.name : null, row.Under,
                              $"[A43-{k + 1}] …它挂在 **`{row.Under}`** 下（原版池 = 整棵树的**先序** ⇒ `background` 下那一格也在池里）");
                        CheckTrue(b.Filled != null && b.Filled.Find("Item Drawer") != null,
                                  $"[A43-{k + 1}] …槽里**真有一个抽屉**（`…/Item Drawer/Icon`）");
                        Check(b.Drawer, ItemDrawer.DrawerWildcard,
                              $"[A43-{k + 1}] 喂野牌 ⇒ 抽屉仍 = `WildcardDrawer`（槽**里面**画什么走 `ItemDrawer.PickDrawer`，"
                              + "与「填哪个槽」是两层 —— 那一边还是我们推的，见 `ItemDrawer.cs` 文件头）");
                        CheckTrue(b.Filled != null && b.Filled.gameObject.activeSelf,
                                  $"[A43-{k + 1}] …而且它**开着**（原版选中就 `SetActive(true)` · `DrawRewards.c:363`）");
                        if (row.SlotWasOff)
                        {
                            string lit = null;
                            for (int q = 0; q < VExpAll.Length; q++)
                                if (VExpAll[q].Name == row.Variant) { lit = VExpAll[q].Slots; break; }
                            CheckTrue(lit != null && System.Array.IndexOf(lit.Split('|'), "*" + row.Slot) >= 0,
                                      $"[A43-{k + 1}] …而它在**原版 dump 里是出厂 INACT**（本文件 `VExpAll` 的字面量写着 `*"
                                      + row.Slot + "`）⇒「选中就把它打开」这条**不是空断**（两个状态都钉住了）");
                        }
                    }
                    else
                    {
                        CheckTrue(b.Filled == null && b.Drawer == null,
                                  $"[A43-{k + 1}] …**一个槽都没填**（照原版 `continue` · `DrawRewards.c:346`）"
                                  + " —— ⛔ 不是「退回去挑第一个槽」");
                        CheckTrue(!string.IsNullOrEmpty(b.SlotNote),
                                  $"[A43-{k + 1}] …而且**出声了**（`Built.SlotNote` 里有理由 —— 红线：不许静默失败）");
                    }
                }
            }
        }

        // ---------------- 🆕 2026-10-03（A8）：商店格**那条链**（原版 `CatalogItemContainer.OnInitialize`）----------------
        //   判据 = `d:/2/tools/decomp_full/CatalogItemContainer__OnInitialize.c`：
        //     `SupportMethods.DestroyAllChildren(drawerHolder);`
        //     `this.drawer = ItemDrawer.Draw(this.drawerHolder, item, 1, DrawerOverride.Shop(0x14), 0);`
        //   ⇒ 原版那一格的图是**运行时由抽屉铺的**，不是自己填的。
        //   🔴 **这条链是加法**：`HasArt` 真 ⇒ 走抽屉；假 ⇒ 老路（`ShopOffer.Art` + 占位板）一字不动。
        Section("商店格（A8）：**走抽屉 / 走兜底 两条路各走一次**");
        {
            win.tabButtons.Click(0);
            var pgA = win.PageOf(0);
            CheckTrue(pgA != null && pgA.DrawerCells == 4 && pgA.FallbackCells == 0,
                      "Cards 页 **4 件都走抽屉**（`ItemDrawer.HasArt(spec, Shop)` 为真）"
                      + $"（实测 抽屉 {pgA?.DrawerCells} / 兜底 {pgA?.FallbackCells}）");

            var cell0 = FindPath(FindChild(root, ShopData.Pages[0].Prefab),
                                 "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_0");
            CheckTrue(cell0 != null, "第一格找得到");
            var art0 = FindChild(cell0, "Art");
            // ⚠️ 节点名仍是 `Art`（老路留下的名字）—— 既有四条断言按它找主图，改名 = **把断言改软**，见 `DrawerStyle` 注释
            CheckTrue(art0 != null && art0.Find(ItemDrawer.NodeIcon) != null,
                      "第一格的主图**是抽屉铺的**（`Art/Icon` ⇒ `ItemDrawer.Icon()` 建的）");
            CheckArt(FindPath(cell0, "Art/" + ItemDrawer.NodeIcon), ShopData.Offers(0)[0].Art,
                     "…画的还是商品表那张图（`spec.Art` = `ShopOffer.Art`）");

            // 🔴 **换路之后渲出来的矩形一字未变**（老路 `MenuDraw.Rect(314.6×208, keepAspect)` 内接）。
            //   ⚠️ **2026-10-04（A34-F7）订正这条的原话**：原文写「抽屉 `Square(box,1)=208²` + `keepAspect`
            //   ⇒ **同一个矩形**」—— 那**只对「图不比框宽」（`aspect ≤ 1`）成立**；`Sautekh Booster`（959×914）
            //   两轴会各缩 **4.7%**，而当时**断言只覆盖第 0 格** ⇒ 静默。现在：① 实现侧把抽屉框按图长宽比放大
            //   （`ShopTabPage.DrawerBox`）；② 下面**逐格 4 件全断**，把「一字未变」变成**可证**的。
            float ax1, ay1, ax2, ay2;
            CheckTrue(RectOf(art0, out ax1, out ay1, out ax2, out ay2), "第一格主图的**渲染矩形**量得到");
            var tex0 = CardArt.MenuUi(ShopData.Offers(0)[0].Art);
            CheckTrue(tex0 != null, "商品表那张图在本地取得到（判据下面两条要用它的宽高比）");
            float wantArt = tex0 != null ? 208f * (float)tex0.width / tex0.height : 0f;
            CheckNear(ay2 - ay1, 208f, 1.5f, "主图**渲出来的高 = 208**（= `CellArtBox` 短边 × `IconFill 1`）");
            CheckNear(ax2 - ax1, wantArt, 1.5f,
                      $"主图**渲出来的宽 = 208 × 图宽高比（{tex0?.width}×{tex0?.height}）** —— 与老路 `keepAspect` 同值");
            CheckNear((ax1 + ax2) * 0.5f, 329.76f + 168.3f, 1f,
                      "主图中心 x = 格左 329.76 + `CellArtBox` 的中心 168.3（**位置也没动**）");
            CheckNear((ay1 + ay2) * 0.5f, 127.62f + 7f + 168f, 1f,
                      "主图中心 y = `Packs Scroll View` 顶 127.62 + 栅格 pad 7 + `CellArtBox` 中心 168（同上）");

            // ---- 🆕 **A34-F7**：**4 件逐格全断**（原来只断第 0 格 —— `Sautekh Booster` 那 4.7% 就是这么漏的）----
            //   期望值在**测试里独立重写一遍老路那条算式**（内接进 `CellArtBox` = 314.6×208）：
            //     `aspect > 314.6/208` ⇒ 宽顶满、高 = 314.6/aspect；否则高顶满、宽 = 208×aspect。
            //   ⚠️ **不读 `ShopTabPage.DrawerStyle` / `DrawerBox`**（那是被测的实现）—— 只借版面常量 `CellArtBox`。
            for (int i = 0; i < ShopData.Offers(0).Length; i++)
            {
                var cellN = FindPath(FindChild(root, ShopData.Pages[0].Prefab),
                                     "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_" + i);
                if (cellN == null) { CheckTrue(false, $"（F7）第 {i + 1} 格找得到"); continue; }
                float w1, t1, w2, t2;
                if (!RectOf(FindChild(cellN, "Art"), out w1, out t1, out w2, out t2))
                { CheckTrue(false, $"（F7）第 {i + 1} 格的渲染矩形量得到"); continue; }
                var txN = CardArt.MenuUi(ShopData.Offers(0)[i].Art);
                float asp = txN != null && txN.height > 0 ? (float)txN.width / txN.height : 1f;
                float bw = ShopTabPage.CellArtBox.W, bh = ShopTabPage.CellArtBox.H;
                float ew = asp > bw / bh ? bw : bh * asp;
                float eh = asp > bw / bh ? bw / asp : bh;
                CheckNear(w2 - w1, ew, 1.5f,
                          $"（F7）第 {i + 1} 件 `{ShopData.Offers(0)[i].Name}` 主图渲出来的**宽** = 内接 `CellArtBox`"
                          + $"（图 {txN?.width}×{txN?.height}）");
                CheckNear(t2 - t1, eh, 1.5f, $"（F7）第 {i + 1} 件 …渲出来的**高** = 内接 `CellArtBox`");
            }

            win.tabButtons.Click(1);
            var pgD = win.PageOf(1);
            CheckTrue(pgD != null && pgD.DrawerCells == 0 && pgD.FallbackCells == 3,
                      "Daily 页 **3 件全走兜底**（`Art = null` ⇒ `ItemDrawer.Spec` 判 `Unknown` ⇒ `HasArt` 假）"
                      + $"（实测 抽屉 {pgD?.DrawerCells} / 兜底 {pgD?.FallbackCells}）");
            var dcell0 = FindPath(FindChild(root, ShopData.Pages[1].Prefab),
                                  "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_0");
            CheckTrue(FindChild(dcell0, "ArtPlaceholder") != null,
                      "…画的是**中性灰占位板 + 短名**（老路**一字未动**）");
            CheckTrue(FindChild(dcell0, "Art") == null, "…**没有** `Art` 节点（这一件本来就没图）");
            CheckTrue(pgD != null && pgD.NoArtOffers.Count == 3,
                      "…`NoArtOffers` 照旧记了 3 件（「不许静默失败」那条出声链没断）");
            win.tabButtons.Click(0);
        }

        ClosePackAndReopenShop(win);   // 上面若没走到（`bp == null`）也保证商店是开着的

        // ---------------- 实拍 ----------------
        CheckHoverSwap(win.transform, "商店窗");   // 🆕 A17：格内价签（`40K_button` → `_hover`）
        CheckPressedSwap(win.transform, "商店窗");  // 🆕 2026-10-05 A50②：按下那一档（此前全工程 0 条断言）
        // 🆕 **A34-F5**：这个助手**一直存在、却从没被调用过**（审查代理 2026-10-04 查出）——
        //   而 `WindowButton.Bind` **只在 hover 缺**时记 `MissingSwapArt`、`AuditHoverSwap` 遇到
        //   `_hoverTex == null` 会**静默跳过** ⇒ 「缺图」这一档此前**一条断言都没有**。
        //   ⚠️ 它是**全局表**（同一进程里前面几扇窗 bind 过的都算进来）⇒ 这条同时也是
        //   「这一轮碰过的按钮**悬停图一张都不缺**」的总闸。
        //   🆕 **2026-10-05（A50②）更正**：原来这里写「**按下图**（`_pressedTex`）**不在**这张表里
        //   （`Bind` 不记它）……那是 `WindowButton` 那一侧的口径，**不在本批白名单**」——
        //   **已经改了**：`WindowButton.Bind` 现在把取不到的按下图记进 `MissingPressedArt`（镜像表）。
        //   ⚠️ 但那张表**只报不断**：原版只有 `m_Transition = 2`（SpriteSwap）那 630 颗有按下图
        //   （逐颗实测 630/630 非空），而 `trans=1/0` 的本来就空 ⇒ 我们取不到时**退回高亮图**多数是合法的
        //   （口径与判据见 `WindowButton.MissingPressedArt` 的注释）。**这里只把它打进日志**。
        //   📌 那只 `WebShop` 钮的按下图（`40K_button_square_pressed`）是**上一批导进工程**的
        //   （`工具/import_original_art.py` 的 `MENU_IMAGES`）⇒ 它的按下态**真起作用**。
        //   🆕 **2026-10-15（A810①）**：**商店窗自己那一份 `MissingArt` 也收进「0 才绿」** ——
        //   本宿主此前只断过「它开出来的那几扇窗」（`bp`/`gop`/`ppw`/`rre`/`rp`），**窗本身一张都没断**
        //   （`Shell/ShopWindow.cs:139` 只在 `MissingArt.Count > 0` 时打一条 `[Shop] ⚠️` 日志
        //    ⇒ 「窗口自己的图取不到」在自检里**没有牙**）。这正是 A810① 说的三套口径里的「只打日志」那一档。
        //   📌 **实读依据**（不是「大概没有」）：本轮最后一次全套跑（`_tmp_view/shop.log`）里
        //   `Shop：… 取不到的图 0 张`（`ShopWindow.Dump()`，`Shell/ShopWindow.cs:194`）⇒ 现在就是 0。
        //   ⚠️ 它**不是**「跑没跑到」的平凡 0：本段之前商店已 `Build()` 过、三页都点过（`MissingArt`
        //   在 `BuildShell` 里 `Clear()`、之后由每一次取图累加）。
        CheckNoMissingArt(win.MissingArt, "★ 商店窗自身（页签底 / 左栏键图标 / 价签 / 滚动条那几件）");
        CheckNoMissingSwapArt("商店窗（含 `OfferContainer` 那 19 份的 `WebShop` 钮）");
        Debug.Log(P + "   [按下图] 取不到的是 **" + WindowButton.MissingPressedArt.Count + " 条**"
                  + (WindowButton.MissingPressedArt.Count > 0
                     ? "（例：" + string.Join("、", WindowButton.MissingPressedArt.GetRange(
                           0, Mathf.Min(3, WindowButton.MissingPressedArt.Count)).ToArray()) + "）"
                     : ""));
        // ---------------- 🆕 **2026-10-15（A810①）：本文件「缺图」= 三条池子，一张表（⛔ 别再各写一套）** ----------------
        //   | 池子 | 谁记的 | 取不到的后果 | 本文件的判据 |
        //   | 主图 `MissingArt`        | `MenuWindowBase.Art()`（**每扇窗一份**） | 那一件**根本没画** | **0 才绿** → `CheckNoMissingArt(...)`（唯一一份） |
        //   | 悬停 `MissingSwapArt`    | `WindowButton.Bind`（**全进程一份**）    | 悬停**换不动** | **0 才绿** → `CheckNoMissingSwapArt(...)` |
        //   | 按下 `MissingPressedArt` | 同上（**镜像表**）                      | 退回高亮图（**多数合法**） | **只出声、不断言**（上面那段注释） |
        //   ⇒ A810① 的收口 = ① 主图那一条原先**在 5 处各写了一遍**（措辞/失败时列不列名字都不同）
        //      → 收成上面那两个助手各一份；② 「窗口自己那一份」原先**一处都没断**（只出声）
        //      → 本宿主里**三扇**都补上了：本段（商店窗自身）· 卡包详情窗段（`pop`）· 成员选项面板段（`amop`）。
        //   ⚠️ **第三行⛔ 别「顺手统一」成 0**：原版 `trans=1/0` 的钮本来就没有按下图（只有 `trans=2` 那 630 颗有）
        //      ⇒ 取不到时退回高亮图**是合法的**；把它断成 0 = 逼着我们去编图名。
        //   ⚠️ **同族但【不是】缺图、⛔ 别混进来**：`ShopWindow.NoArtOffers` / `ShopTabPage.FallbackCells`
        //      （`Shell/ShopWindow.cs`）记的是**数据侧本来就没图**（Daily 页 3 件 `Art = null`）⇒
        //      期望值就是 **3**（上面那几条断言），**不是 0**。
        // ---------------- 🆕 2026-10-11（A294 / A292）：共用件的两条口径 ----------------
        //   **A294** = `MenuDraw.Local` 的**量纲**（设计空间 vs 世界空间）：小屏缩放开关一开，
        //     窗根被 `TransformScalerBySmallScreenUI` 乘 M ⇒ 子件的世界位置 = M × 设计位置，
        //     而 `Local` 原来写的是 `RectCenter − parent.position`（**少除一次父链缩放**）
        //     ⇒ **整扇窗的文字与图**都按 `(1−M)·nl` 偏（`nl` = 父件到窗根的距离）。
        //     判据 / 算式 / 「除的是哪一级」→ `Shell/MenuDraw.cs` 的 `PosInDesignSpace`（唯一一份，两处）
        //     · 裁定 → `资料/普查产出_1010/调度台_口径裁定_1011.md` §A228（同一条量纲病）。
        //   **A292** = `ImageQuad.SetTexture` 会把 `_aspect` **静默**冲成新贴图的比例 ⇒ 新增
        //     `SetTexture(t, keepAspect:)` 这个**显式**口（单参那一份的行为**逐字不变**）。
        //   🔴 **为什么必须两态**：开关出厂是**关**的（原版 `GameStaticData.cctor` = 0 / 我们的
        //     `PlayerPrefs` 默认 0）⇒ 只断「关」那一态的话，**改坏了也照样绿**（本工程那条系统性毛病：
        //     弱断言分不出两种状态）。A292 那两态 = 「保留比例 / 显式改比例」。
        //   ⚠️ **宿主为什么是 ShopScene**：简报建议 `Editor/ShellScene.cs`，但那一支本批**有别的写手**
        //     （同批在跑的还有 `RewardsScene` / `CollectionScene` / `MainMenuScene` / `DeckScene` /
        //     `SettingsScene` / `BattleScene`）⇒ 换到这个**没人占用**的宿主；它本来就是壳里的一员
        //     （`ShopWindow` 走的是生产那条挂法：`EnsureHost` 建锚点 → `AttachToAnchor` 把窗根归到原点）。
        Section("A294：`MenuDraw.Local` 的量纲（关 = 逐值不变 / 开 = 设计点渲出来在 M·点）+ A292：`SetTexture` 保留比例口");
        {
            SmallScreenUI.PersistOverride = true;   // ⛔ 自检不许动玩家的真设置（同 `SettingsScene`）
            SmallScreenUI.Set(false);               // 从出厂态起步

            // 🔴 **裁定点名要核的那一格**（`PosInDesignSpace` 只在**窗根落在世界原点**时精确）：
            //    先在**本场景真实的那扇窗**上核一次 —— 生产侧同档（`EnsureHost` 的三个 Holder 与
            //    `AttachToAnchor` 全在 `ShellRuntime` 的无父 `Shell` 根下）。不在原点 ⇒ **停手报回来**（改选 (c)）。
            CheckNear(win.transform.position.x, 0f, 1e-4f,
                      "（前提）A294：商店窗的**窗根**落在世界原点 x（= `PosInDesignSpace` 的适用范围；不在原点就该改选 (c)）");
            CheckNear(win.transform.position.y, 0f, 1e-4f, "（前提）A294：……窗根 y 也在原点");

            // 探针用**固定矩形**（不许落在原点：父件摆在离窗根 2 个设计单位处 —— 不除 `k` 时的偏差
            // 正好是 `(1−k)×父世界位置`，父件摆 0 处两式恒等 ⇒ 断言会退化成假绿）。
            var a294R = new PxRect(700f, 400f, 900f, 500f);
            Vector3 d294 = LayoutSpace.RectCenter(a294R.x1, a294R.y1, a294R.x2, a294R.y2);   // 设计空间那个点（独立算，⛔ 不读实现）
            const float ParentDx = 2f, ParentDy = 1.5f;
            // 态一实测到的那两个世界坐标 —— 态二那条**相对**断言拿它乘 1.2（⛔ 不读 `LayoutSpace`、不读任何我们的常量）
            float a294offX = 0f, a294offY = 0f;

            // ---- 态一：开关**关**（出厂态）⇒ 窗根不缩放 ⇒ k == 1 ⇒ 与旧写法**逐值相同** ----
            var a294r1 = new GameObject("a294 probe (flag off)");
            var a294p1 = new GameObject("a294 parent").transform;
            a294p1.SetParent(a294r1.transform, false);
            a294p1.localPosition = new Vector3(ParentDx, ParentDy, 0f);
            var a294q1 = MenuDraw.Rect(a294p1, CardArt.Solid(), a294R, "a294 quad", 3000);
            CheckTrue(a294q1 != null, "（前提）A294 态一：探针 quad 建起来了（走 `MenuDraw.Rect`，= 生产那条路）");
            if (a294q1 != null)
            {
                CheckNear(a294p1.lossyScale.x, 1f, 1e-4f,
                          "（前提）态一：父链**没有**缩放 ⇒ 下一条那句「逐值不变」才有意义");
                CheckNear(a294q1.transform.localPosition.x, d294.x - a294p1.position.x, 1e-4f,
                          "① 关：`MenuDraw.Local` 与**旧式**（`设计点 − 父世界位置`）逐值相同 —— 这一态钉的是"
                        + "「开关关着时**逐位不变**」（k=1 ⇒ 新旧两式恒等）；改坏法：把「减父世界位置」那一项"
                        + "整个丢掉（= 文件头坑①那个旧缺陷，件会飞到屏幕外）⇒ 这一条红");
                CheckNear(a294q1.transform.localPosition.y, d294.y - a294p1.position.y, 1e-4f,
                          "① 关：……y 分量同理（只改 x 不改 y 时只有这一条红）");
                CheckNear(a294q1.transform.position.x, d294.x, 1e-3f,
                          "① 关：**渲出来**的中心 x = 设计点 x（= 原版那个像素矩形的中心）");
                CheckNear(a294q1.transform.position.y, d294.y, 1e-3f, "① 关：……渲出来的中心 y 同理");
                a294offX = a294q1.transform.position.x;
                a294offY = a294q1.transform.position.y;
            }
            Object.DestroyImmediate(a294r1);

            // ---- 态二：开关**开** + `extra = 1.2` ⇒ 窗根乘 1.2，父节点的世界位置 = 1.2 × 设计值 ----
            SmallScreenUI.Set(true);
            var a294r2 = new GameObject("a294 probe (flag on)");
            var a294w2 = a294r2.AddComponent<GameWindow>();
            // 🔴 **2026-10-15（A672）夹具探针加固**：裸 `AddComponent<GameWindow>()` 建出来的探针
            //    **从不赋 `type`** ⇒ 字段停在哨兵 `GameWindow.UnsetType`(-1)，靠「今天没人在这些探针上读它」
            //    保平安（W8 报告 §三-③ 点名的那一档 ——「不是靠赋过值，是靠没人读」）。
            //    这里显式钉成 **`Fullscreen`**：它正是 A672 之前那个默认值 ⇒ 与改前**逐位同义**；
            //    将来谁把这些探针改成走 `OpenWindow`，也不会静默落进「弹窗支」（那是更难认的一种坏法）。
            //    ⚠️ **这不是「本窗的档位判据」**（探针没有原版对应物）；哨兵口径 → `Shell/WindowsManager.cs:100`。
            a294w2.type = WindowType.Fullscreen;
            a294w2.extraScaleSmallScreen = 1.2f;
            CheckTrue(a294w2.TryOpen(null), // = 生产那条路（挂缩放器 + `SetScale`）
                      "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
            var a294s2 = a294r2.GetComponent<TransformScalerBySmallScreenUI>();
            if (a294s2 != null) a294s2.Tick();                      // 批处理没有帧循环 ⇒ 手动推一次
            CheckNear(a294r2.transform.localScale.x, 1.2f, 1e-4f,
                      "（前提）态二：开关开 + `extraScaleSmallScreen = 1.2` ⇒ 窗根 `localScale` = 1.2");
            CheckNear(a294r2.transform.position.x, 0f, 1e-4f, "（前提）态二：探针窗根也在世界原点");
            var a294p2 = new GameObject("a294 parent").transform;
            a294p2.SetParent(a294r2.transform, false);
            a294p2.localPosition = new Vector3(ParentDx, ParentDy, 0f);
            CheckNear(a294p2.position.x, 1.2f * ParentDx, 1e-3f,
                      "（前提）态二：父节点的**世界** x = 设计值 × 1.2（**≠** 设计值 ⇒ 两态的量纲差真的存在，"
                    + "后面两条不是在断一个恒等式）");
            CheckNear(a294p2.position.y, 1.2f * ParentDy, 1e-3f, "（前提）态二：……父节点的世界 y 同理");
            var a294q2 = MenuDraw.Rect(a294p2, CardArt.Solid(), a294R, "a294 quad", 3000);
            CheckTrue(a294q2 != null, "（前提）A294 态二：探针 quad 建起来了");
            if (a294q2 != null)
            {
                CheckNear(a294q2.transform.position.x, 1.2f * d294.x, 0.01f,
                          "★② 开：`MenuDraw.Rect` 出来的件**渲出来**的中心 = **设计点 × 1.2**（整扇窗一起缩那一档）。"
                        + "改坏法：`PosInDesignSpace` 里不除父链 `lossyScale`（= A294 那条潜伏缺陷）"
                        + "⇒ 偏 `(1−1.2) × 2.4 = −0.48` 世界单位 = **−51.8px** ⇒ 红");
                CheckNear(a294q2.transform.position.y, 1.2f * d294.y, 0.01f,
                          "★② 开：……y 同理（偏 `(1−1.2) × 1.8 = −0.36` 世界单位 = **−38.9px**；"
                        + "两个分量各一条：只把 x 除对了、y 没除时只有这一条红）");
                CheckNear(a294q2.transform.position.x, 1.2f * a294offX, 0.01f,
                          "★②（**相对**那一断）把「开关**关**」那一态**实测到**的世界 x 乘 1.2，就该等于「开关**开**」"
                        + "这一态的 x —— ⛔ 这一条**不读 `LayoutSpace`、不读任何我们自己的常量**（只有开关那个 1.2），"
                        + "所以就算上面那条的期望值抄错了，它也照样能把「不除父链缩放」照出来"
                        + "（前提 = 态一那颗探针健在；两态用的是同一个 `CardArt.Solid()` ⇒ 取不到图时上面那条「（前提）」会先红）");
                CheckNear(a294q2.transform.position.y, 1.2f * a294offY, 0.01f,
                          "★②（**相对**那一断）……世界 y 同理");
            }
            Object.DestroyImmediate(a294r2);
            SmallScreenUI.Set(false);               // 放回出厂态（下面还有截图，必须在原版出厂态下拍）

            // ---------------------------------------------------------- A292
            //   两态：**保留比例** ⇒ quad 的世界宽高**一个字节都不变**（图真的换了）；
            //        **显式改比例**（单参那个老口）⇒ 宽跟着**新贴图自己的**比例变。
            //   🔴 期望值**不从被测实现里读**：两个比例分别来自**塞进去那两张贴图自己的 `width/height`**
            //      与**那个像素矩形自己的宽高**（`a292R` = 200×100）。
            var a292R = new PxRect(0f, 0f, 200f, 100f);
            var a292r = new GameObject("a292 probe");
            // ⚠️ 两张图的**比例都要与那个矩形（2）不同**（4 / 8）—— 否则下面那条「前提」就成了恒等式：
            //    它量的正是「`MenuDraw.Rect` 有没有把**矩形**的比例写进去」（`SetAspect(200/100)`），
            //    贴图比例若也恰好是 2，`SetAspect` 早退不早退都看不出区别。
            var a292ta = new Texture2D(4, 1, TextureFormat.RGBA32, false);
            var a292tb = new Texture2D(8, 1, TextureFormat.RGBA32, false);
            a292ta.Apply(); a292tb.Apply();
            var a292q = MenuDraw.Rect(a292r.transform, a292ta, a292R, "a292 quad", 3000);
            CheckTrue(a292q != null, "（前提）A292：探针 quad 建起来了");
            if (a292q != null)
            {
                float a292w0 = a292q.WorldW, a292h0 = a292q.WorldH;
                Check(a292q.Texture, a292ta, "（前提）A292：`MenuDraw.Rect` 之后贴图 = 塞进去的那一张");
                CheckNear(a292w0, a292h0 * (a292R.W / a292R.H), 1e-4f,
                          "（前提）A292：显示比例 = **那个像素矩形自己的宽高比 2**（`Rect` 会 `SetAspect`，与贴图无关）"
                        + " —— 这是下面「保留 / 改」两态的分界，不成立的话那两条都是空断");
                a292q.SetTexture(a292tb, true);                     // = 「保留当前比例」那一档
                Check(a292q.Texture, a292tb, "★ A292 ①`keepAspect: true` **真的换了图**（不然下面两条是空的）");
                CheckNear(a292q.WorldH, a292h0, 1e-6f, "★ A292 ①……而且**高**一个字节都没变");
                CheckNear(a292q.WorldW, a292w0, 1e-6f,
                          "★ A292 ①……**宽也没变**（= 保留当前比例：`_aspect` 不被冲成新贴图的比例）。"
                        + "改坏法：把新口退化成老行为（无条件写 `_aspect`）⇒ 宽变成 `高 × 8`（≈ "
                        + (a292h0 * 8f).ToString("F3") + "）⇒ 红");
                a292q.SetTexture(a292tb);                           // = 单参老口（行为必须仍是「按贴图改比例」）
                CheckNear(a292q.WorldH, a292h0, 1e-6f,
                          "★ A292 ②单参老口**也不动高**（`WorldW = WorldH × _aspect` ⇒ 动的一直只有宽）");
                CheckNear(a292q.WorldW, a292h0 * (a292tb.width / (float)a292tb.height), 1e-4f,
                          "★ A292 ②单参老口（= 今天的行为）**按新贴图改比例** ⇒ 宽 = `高 × 8`。"
                        + "改坏法：把老口也改成「保留比例」⇒ 这一条红（那条纪律有 6 个调用点依赖着它："
                        + "`WindowButton.SetOn` / `BattleLogPanel` / `AlliancesTab` / `DeckRuntime` ×2 / `BattleDriver`）");
                CheckTrue(Mathf.Abs(a292q.WorldW - a292w0) > 1f,
                          "★ A292 ②（**相对**那一断）两态**确实不同**：宽从 " + a292w0.ToString("F3")
                        + " 变到 " + a292q.WorldW.ToString("F3") + " —— ⛔ 这条一个常量都不读，"
                        + "所以就算上面那两条的期望值抄错了，它也照样能把「新口退化成老行为」照出来");
                // ⚠️ **如实记一条**：`SetTexture` 只改 `_aspect` **不重建网格** ⇒ 上面量的是**字段**
                //    （= 全工程 6 个调用点判「比例对不对」用的就是它）；渲染网格要等调用侧那句
                //    `SetAspect(...)`（内含 `RebuildMesh`）或软边重切才跟上 —— 这正是那条纪律
                //    「`SetTexture` 后面必须紧跟一句 `SetAspect`」的由来。本件**没改**这条纪律的任何一处调用点。
            }
            Object.DestroyImmediate(a292r);
            Object.DestroyImmediate(a292ta);
            Object.DestroyImmediate(a292tb);

            SmallScreenUI.PersistOverride = false;  // 把注入点也放回去（开关的内存态上面已放回出厂值）
            CheckTrue(!SmallScreenUI.Enabled, "（收尾）A294：自检跑完把开关放回**出厂值 关**");
        }

        // ---------------- 🆕 2026-10-11（A297）：`MainMenuSubmenuWindow.Local` 那份**同形副本** ----------------
        //   **A294** 修的是 `MenuDraw.Local` 那 4 处；**本件**修的是**第二份**
        //   （`Shell/MenuWindowBase.cs` 的 `MainMenuSubmenuWindow.Local`，改法是**转调** `MenuDraw.PosInDesignSpace`）——
        //   它服务的正是**本场景这扇窗**（`ShopWindow : MainMenuSubmenuWindow`）与另外三个同族窗
        //   （`RewardsWindow` / `SocialWindow` / `CollectionWindow`）：`Node`（容器节点）· `Text` / `TextBox`（文字）
        //   · 左栏键 · 内容区渐变背景全走它。判据 / 算式 → `Shell/MenuDraw.cs` 的 `PosInDesignSpace`（全壳唯一一份）。
        //   🔴 **为什么必须两态**：开关出厂是**关**的 ⇒ 只断「关」那一态的话，**「不转调」（退回旧式）也照样绿**
        //     （k=1 ⇒ 新旧两式恒等）。两态 = **关**（逐值不变）/ **开**（设计点 × M）。
        //   ⚠️ **宿主为什么还是 ShopScene**：A294 那一段就在本文件里，本件与它**同源同题**
        //     （同一份量纲病的两处），放一起便于下个会话一起读；且本批别的宿主都有人占着（简报点名可用本宿主）。
        Section("A297：`MainMenuSubmenuWindow.Local` 的量纲（关 = 逐值不变 / 开 = 设计点 × M）");
        {
            SmallScreenUI.PersistOverride = true;    // ⛔ 自检不许动玩家的真设置（同 `SettingsScene`）
            SmallScreenUI.Set(false);                // 从出厂态起步

            // 探针的矩形与「点」**都不许落在原点附近**：父件摆在离窗根 2 个设计单位处 —— 不除 `k` 时的偏差
            // 正好是 `(1−k) × 父件的设计位置`，父件摆 0 处两式恒等 ⇒ 断言会退化成假绿。
            var a297R = new PxRect(700f, 400f, 900f, 500f);
            Vector3 d297 = LayoutSpace.RectCenter(a297R.x1, a297R.y1, a297R.x2, a297R.y2);   // 设计点（独立算，⛔ 不读实现）
            const float A297Px = 540f, A297Py = 270f;
            Vector3 d297pt = LayoutSpace.FromPixel(A297Px, A297Py);                          // 「像素点」那一份的设计点
            const float A297Dx = 2f, A297Dy = 1.5f;
            // 探针文字不许被本窗**当前**的 `Clip` 裁掉（那会把「建起来了」这条前提弄红）；跑完原样放回
            var a297Clip = win.Clip;
            win.Clip = null;
            float a297w1x = 0f, a297w1y = 0f, a297qw1x = 0f, a297qw1y = 0f;   // 态一**实测到**的世界 / 局部值

            // ---- 态一：开关**关**（出厂态）⇒ 父链无缩放 ⇒ k == 1 ⇒ 与旧写法**逐值相同** ----
            var a297r1 = new GameObject("a297 probe (flag off)");
            var a297p1 = new GameObject("a297 parent").transform;
            a297p1.SetParent(a297r1.transform, false);
            a297p1.localPosition = new Vector3(A297Dx, A297Dy, 0f);
            CheckNear(a297p1.lossyScale.x, 1f, 1e-4f,
                      "（前提）态一：父链**没有**缩放 ⇒ 下面那几条「逐值不变」才有意义");
            var a297lp1 = MainMenuSubmenuWindow.Local(a297p1, a297R.x1, a297R.y1, a297R.x2, a297R.y2);
            CheckNear(a297lp1.x, d297.x - a297p1.position.x, 1e-5f,
                      "① 关：`MainMenuSubmenuWindow.Local`（矩形那一份）与**旧式**（`设计点 − 父的世界位置`）逐值相同"
                    + " —— 这一态钉的是「开关关着时**逐位不变**」（k=1 ⇒ 新旧两式恒等）。"
                    + "改坏法：把「减父的世界位置」那一项整个丢掉（件会飞到屏幕外）⇒ 这一条红");
            CheckNear(a297lp1.y, d297.y - a297p1.position.y, 1e-5f,
                      "① 关：……y 分量同理（只改 x 不改 y 时只有这一条红）");
            var a297qt1 = MainMenuSubmenuWindow.Local(a297p1, A297Px, A297Py);
            CheckNear(a297qt1.x, d297pt.x - a297p1.position.x, 1e-5f,
                      "① 关：**「像素点」那一份重载**同样逐值不变（两个生产调用点 = `Shell/SettingsWindow.cs` 里那两处 `MenuDraw.Local`"
                    + " 设置窗两根滑块的落位 · `Shell/ShopWindow.cs` 里 `MainMenuSubmenuWindow.Local(ic, …)` 那一句（时间计数器图标） —— 它们**不走**"
                    + " `MenuDraw.Local` ⇒ 两个重载各要一条）");
            CheckNear(a297qt1.y, d297pt.y - a297p1.position.y, 1e-5f, "① 关：……「像素点」那一份的 y 同理");
            a297qw1x = a297qt1.x; a297qw1y = a297qt1.y;

            var a297n1 = MainMenuSubmenuWindow.Node(a297p1, "a297 node", a297R);
            CheckTrue(a297n1 != null,
                      "（前提）A297 态一：`Node` 建起来了（= 生产那条路，`BuildShell` 建容器节点走的就是它）");
            if (a297n1 != null)
            {
                CheckNear(a297n1.position.x, d297.x, 1e-3f,
                          "① 关：`Node` **渲出来**的中心 x = 设计点 x（= 原版那个像素矩形的中心）");
                CheckNear(a297n1.position.y, d297.y, 1e-3f, "① 关：……`Node` 渲出来的中心 y 同理");
                a297w1x = a297n1.position.x; a297w1y = a297n1.position.y;
            }
            var a297t1 = win.Text(a297p1, "A297", a297R.x1, a297R.x2, a297R.y1, a297R.y2, 6, Color.white, "a297 label");
            CheckTrue(a297t1 != null, "（前提）态一：`Text` 建起来了（`MenuWindowBase.Text` 走的是**同一个** `Local`）");
            if (a297t1 != null)
            {
                CheckNear(a297t1.transform.position.x, d297.x, 1e-3f,
                          "① 关：`Text` 出来的字**渲出来**也在设计点 x");
                CheckNear(a297t1.transform.position.y, d297.y, 1e-3f, "① 关：……字的 y 同理");
            }
            Object.DestroyImmediate(a297r1);

            // ---- 态二：开关**开** + `extra = 1.2` ⇒ 窗根乘 1.2 ⇒ 父件的世界位置 = 1.2 × 设计值 ----
            SmallScreenUI.Set(true);
            var a297r2 = new GameObject("a297 probe (flag on)");
            var a297gw2 = a297r2.AddComponent<GameWindow>();
            a297gw2.type = WindowType.Fullscreen;   // 🆕 A672：夹具探针显式钉 `type`（口径 → 上面 `a294w2` 那一处）
            a297gw2.extraScaleSmallScreen = 1.2f;
            CheckTrue(a297gw2.TryOpen(null), // = 生产那条路（挂缩放器 + `SetScale`）
                      "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
            var a297sc2 = a297r2.GetComponent<TransformScalerBySmallScreenUI>();
            if (a297sc2 != null) a297sc2.Tick();                     // 批处理没有帧循环 ⇒ 手动推一次
            CheckNear(a297r2.transform.localScale.x, 1.2f, 1e-4f,
                      "（前提）态二：开关开 + `extraScaleSmallScreen = 1.2` ⇒ 窗根 `localScale` = 1.2");
            CheckNear(a297r2.transform.position.x, 0f, 1e-4f,
                      "（前提）态二：探针窗根也在世界原点（= `PosInDesignSpace` 的适用范围）");
            CheckNear(a297r2.transform.position.y, 0f, 1e-4f, "（前提）态二：……窗根 y 同理");
            var a297p2 = new GameObject("a297 parent").transform;
            a297p2.SetParent(a297r2.transform, false);
            a297p2.localPosition = new Vector3(A297Dx, A297Dy, 0f);
            CheckNear(a297p2.position.x, 1.2f * A297Dx, 1e-3f,
                      "（前提）态二：父节点的**世界** x = 设计值 × 1.2（**≠** 设计值 ⇒ 两态的量纲差真的存在，"
                    + "后面几条不是在断一个恒等式）");
            CheckNear(a297p2.position.y, 1.2f * A297Dy, 1e-3f, "（前提）态二：……父节点的世界 y 同理");

            var a297n2 = MainMenuSubmenuWindow.Node(a297p2, "a297 node", a297R);
            CheckTrue(a297n2 != null, "（前提）A297 态二：`Node` 建起来了");
            if (a297n2 != null)
            {
                CheckNear(a297n2.position.x, 1.2f * d297.x, 0.01f,
                          "★② 开：`MainMenuSubmenuWindow.Node` 出来的件**渲出来**的中心 = **设计点 × 1.2**"
                        + "（整扇窗一起缩那一档）。**改坏法：这两处不转调 `MenuDraw.PosInDesignSpace`"
                        + "（退回旧式 `设计点 − parent.position`）⇒ 偏 `(1−1.2) × 2 = −0.4` 设计单位"
                        + "= −0.48 世界单位 = **−51.8px** ⇒ 这一条红**");
                CheckNear(a297n2.position.y, 1.2f * d297.y, 0.01f,
                          "★② 开：……y 同理（偏 `(1−1.2) × 1.5 = −0.3` 设计单位 = −0.36 世界单位 = **−38.9px**；"
                        + "两个分量各一条：只把 x 除对了、y 没除时只有这一条红）");
                CheckNear(a297n2.position.x, 1.2f * a297w1x, 0.01f,
                          "★②（**相对**那一断）把「开关**关**」那一态**实测到**的世界 x 乘 1.2，就该等于「开」这一态的 x"
                        + " —— ⛔ 这一条**不读 `LayoutSpace`、不读任何我们自己的常量**（只有开关那个 1.2），"
                        + "所以就算上面那条的期望值抄错了，它也照样能把「不除父链缩放」照出来");
                CheckNear(a297n2.position.y, 1.2f * a297w1y, 0.01f, "★②（**相对**那一断）……世界 y 同理");
            }
            var a297qt2 = MainMenuSubmenuWindow.Local(a297p2, A297Px, A297Py);
            CheckNear(a297qt2.x, a297qw1x, 1e-5f,
                      "★② 开：「像素点」那一份返回的是**设计空间里的局部坐标**（`设计点 − 父件在设计空间的位置`）"
                    + "⇒ 两态**同值**（这条比的是态一**实测**到的那个数）。**改坏法：退回旧式（减父件的世界位置）"
                    + "⇒ 态二给 `设计点 − 2.4`，与态一的 `设计点 − 2` 差 **0.4**（局部单位；父链乘 1.2"
                    + " ⇒ 世界 −0.48 = −51.8px）⇒ 红**");
            CheckNear(a297qt2.y, a297qw1y, 1e-5f,
                      "★② 开：……「像素点」那一份的 y 同理（差 **0.3** 局部单位 ⇒ 世界 −0.36 = −38.9px）");
            var a297pn2 = new GameObject("a297 point probe").transform;
            a297pn2.SetParent(a297p2, false);
            a297pn2.localPosition = MainMenuSubmenuWindow.Local(a297p2, A297Px, A297Py);   // = `SettingsWindow` 那个用法
            CheckNear(a297pn2.position.x, 1.2f * d297pt.x, 0.01f,
                      "★② 开：把「像素点」那一份的返回值写进 `localPosition` ⇒ **渲出来**正好落在设计点 × 1.2"
                    + "（= `Shell/SettingsWindow.cs` 里那两处 `MenuDraw.Local` 那个用法：把中心喂给 `WfSlider`）");
            CheckNear(a297pn2.position.y, 1.2f * d297pt.y, 0.01f, "★② 开：……那个探针的 y 同理");

            var a297t2 = win.Text(a297p2, "A297", a297R.x1, a297R.x2, a297R.y1, a297R.y2, 6, Color.white, "a297 label");
            CheckTrue(a297t2 != null, "（前提）态二：`Text` 建起来了");
            if (a297t2 != null)
            {
                CheckNear(a297t2.transform.position.x, 1.2f * d297.x, 0.01f,
                          "★② 开：`MenuWindowBase.Text` 出来的字**渲出来**也在设计点 × 1.2 —— 这一条与上面 `Node`"
                        + "那两条合起来 = 「两个消费方（容器节点 / 文字）都收口了」；改坏法同上 ⇒ 偏 −51.8px ⇒ 红");
                CheckNear(a297t2.transform.position.y, 1.2f * d297.y, 0.01f, "★② 开：……字的 y 同理（偏 −38.9px）");
            }
            Object.DestroyImmediate(a297r2);

            win.Clip = a297Clip;                     // 本窗的裁切原样放回（探针那一段临时摘掉过）
            SmallScreenUI.Set(false);                // 放回出厂态（下面还有截图，必须在原版出厂态下拍）
            SmallScreenUI.PersistOverride = false;   // 把注入点也放回去
            CheckTrue(!SmallScreenUI.Enabled, "（收尾）A297：自检跑完把开关放回**出厂值 关**");
        }

        // ---------------- 🆕 2026-10-11（A327 · A298 + A306⑥）：两态夹具 ----------------
        //   判据 / 断言什么 / 为什么这个形状能照出它 → `资料/普查产出_1011/W4_子3.md` §四·b（A298 · A306⑥ 两行）。
        //   🔴 两处都是**潜伏缺陷**：`k == 1`（开关出厂关）时新旧两式**逐位相同** ⇒ 只断态一 = 假绿。
        SmallScreenUI.PersistOverride = true;      // ⛔ 自检不许动玩家的真设置（同 `SettingsScene`）
        Section("A327 · A298：`MenuDraw.QuadRectPx` 的位置项（关 = 逐值不变 / 开 = 设计点 × 1.2）");
        {
            SmallScreenUI.Set(false);
            // 探针矩形**离画布中心够远**：偏差 ∝ 「该 quad 到画布中心的距离 × (1 − 1/M)」
            // ⇒ 摆在中心附近时两态几乎重合、断言退化成假绿。
            var a298R = new PxRect(1200f, 700f, 1800f, 1000f);
            var a298C = new PxRect(1150f, 650f, 1600f, 950f);                     // `clip`：与 `R` **部分重叠**
            var a298Want = new PxRect(Mathf.Max(a298R.x1, a298C.x1), Mathf.Max(a298R.y1, a298C.y1),
                                      Mathf.Min(a298R.x2, a298C.x2), Mathf.Min(a298R.y2, a298C.y2));
            CheckTrue(a298Want.W > 10f && a298Want.H > 10f
                      && a298Want.W < a298R.W - 10f && a298Want.H < a298R.H - 10f,
                      "（前提）探针矩形与裁切框**部分重叠**（全在框内 ⇒ `ClipNineChildren` 一趟都不跑；"
                    + "全在框外 ⇒ 连节点都不建 ⇒ 两种情况这条断言都等于没查）");
            const float A298Dx = 2f, A298Dy = 1.5f;
            PxRect a298u1 = default(PxRect), a298u2 = default(PxRect);

            // ---- 态一：开关**关**（出厂态）⇒ k == 1 ⇒ 与改前逐位相同 ----
            var a298r1 = new GameObject("a298 probe (flag off)");
            var a298p1 = new GameObject("a298 parent").transform;
            a298p1.SetParent(a298r1.transform, false);
            a298p1.localPosition = new Vector3(A298Dx, A298Dy, 0f);
            var a298go1 = MenuDraw.Nine(a298p1, CardArt.Solid(), a298R, new Vector4(40f, 40f, 40f, 40f),
                                        100f, 100f, 3000, null, true, "a298 nine", null, a298C);
            CheckTrue(a298go1 != null, "（前提）态一：九宫格建出来了（`CardArt.Solid()` 取得到）");
            if (a298go1 != null)
            {
                a298u1 = NineUnionPx(a298go1.transform, 1f);
                CheckNear(a298u1.x1, a298Want.x1, 1f, "① 关：各子块的并集（设计 px）左沿 = `R ∩ clip` 左沿");
                CheckNear(a298u1.x2, a298Want.x2, 1f, "① 关：……右沿");
                CheckNear(a298u1.y1, a298Want.y1, 1f, "① 关：……上沿");
                CheckNear(a298u1.y2, a298Want.y2, 1f, "① 关：……下沿");
            }
            Object.DestroyImmediate(a298r1);

            // ---- 态二：开关**开** + 窗根 ×1.2（= 生产那条路）----
            SmallScreenUI.Set(true);
            var a298r2 = new GameObject("a298 probe (flag on)");
            var a298gw2 = a298r2.AddComponent<GameWindow>();
            a298gw2.type = WindowType.Fullscreen;   // 🆕 A672：夹具探针显式钉 `type`（口径 → 上面 `a294w2` 那一处）
            a298gw2.extraScaleSmallScreen = 1.2f;
            CheckTrue(a298gw2.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
            var a298sc2 = a298r2.GetComponent<TransformScalerBySmallScreenUI>();
            if (a298sc2 != null) a298sc2.Tick();               // 批处理没有帧循环 ⇒ 手动推一次
            CheckNear(a298r2.transform.localScale.x, 1.2f, 1e-4f, "（前提）态二：窗根 `localScale` = 1.2");
            var a298p2 = new GameObject("a298 parent").transform;
            a298p2.SetParent(a298r2.transform, false);
            a298p2.localPosition = new Vector3(A298Dx, A298Dy, 0f);
            var a298go2 = MenuDraw.Nine(a298p2, CardArt.Solid(), a298R, new Vector4(40f, 40f, 40f, 40f),
                                        100f, 100f, 3000, null, true, "a298 nine", null, a298C);
            CheckTrue(a298go2 != null, "（前提）态二：九宫格建出来了");
            if (a298go2 != null)
            {
                a298u2 = NineUnionPx(a298go2.transform, 1.2f);
                CheckNear(a298u2.x1, a298Want.x1, 1f,
                          "★② 开：**各子块的并集（除回设计 px）= `R ∩ clip`**（左沿）—— "
                        + "**改坏法**：把 `MenuDraw.QuadRectPx` 的 `ToPixel(PosInDesignSpace(q.transform))` "
                        + "换回裸 `ToPixel(q.transform.position)` ⇒ 每块的求交偏 `(1−1/M)×|块中心 − 画布中心|`"
                        + "（探针在 x 1200..1800 ⇒ 偏 **40~168px**）⇒ 并集越出 `clip` / 尺寸不对 ⇒ **这一条红**");
                CheckNear(a298u2.x2, a298Want.x2, 1f, "★② 开：……右沿");
                CheckNear(a298u2.y1, a298Want.y1, 1f, "★② 开：……上沿");
                CheckNear(a298u2.y2, a298Want.y2, 1f, "★② 开：……下沿");
                // ⛔ 这四条**不读 `LayoutSpace`、不读任何我们自己的常量**（只比两态）
                CheckNear(a298u2.x1, a298u1.x1, 1f, "★②（相对那一断）态二的设计空间并集 == 态一那一份（左沿）");
                CheckNear(a298u2.x2, a298u1.x2, 1f, "★②（相对那一断）……右沿");
                CheckNear(a298u2.y1, a298u1.y1, 1f, "★②（相对那一断）……上沿");
                CheckNear(a298u2.y2, a298u1.y2, 1f, "★②（相对那一断）……下沿");
            }
            Object.DestroyImmediate(a298r2);
            SmallScreenUI.Set(false);
            CheckTrue(!SmallScreenUI.Enabled, "（收尾）A298：开关放回**出厂值 关**");
        }

        Section("A327 · A306⑥：`ShopWindow.BuildTimeCounter` 的 else 支（关 = 逐值不变 / 开 = 设计点 × 1.2）");
        {
            var pgA327 = win.PageOf(0);
            // 🔴 **2026-10-11（A346）补牙口**：`measure` 里**必须重建**（判据 = 夹具文件头 ③）——
            //   不重建时 `Clock Icon` 的 `localPosition` 是 `k == 1` 那一趟**冻结**下来的值，
            //   而 `p2 == M × p1` 那时对**任何**实现都成立（恒等式）⇒ 那条 ★ 就**不是**在验 `BuildTimeCounter`。
            // ✔ 重建口 = `pgA327.Setup()`：它首句 `_win.DestroyChildren(_root)` 把本页整棵清掉、
            //   再重跑 `BuildTimeCounter`（`Shell/ShopWindow.cs` 的 `ShopTabPage.Setup()` + `BuildTimeCounter()`）。
            // 🔴 **但基准不能取本页的 `TimeCounter`**：那句 `Setup()` 会把它**一并销毁**，而夹具在两次
            //   `measure()` 之后**还要读** `basis.position` / `basis.parent.lossyScale` —— 读一个已销毁的
            //   `Transform` 抛的是 `MissingReferenceException`（**不是**断言红，会把整条 `Run()` 掀掉）。
            //   ⇒ 基准取【**另一页**】的 `TimeCounter`：同窗、同 `TabsRect`、`TimeCounter` 是静态常量、
            //   同一条 `BuildTimeCounter` 建出来的，而它**不参与重建** ⇒ 全程活着。
            //   它不是「随手找个替代品」—— 下面两条断言把它钉死：① 两页的 `TimeCounter` **同位**（跑夹具之前先证）；
            //   ② 夹具的前提① 当场再核一次「它的 `parent`（`daily shop header`）就是窗根那一级、`lossyScale == M`」。
            var pgBasis327 = win.PageOf(1);
            CheckTrue(pgA327 != null, "（前提）第 1 页的 `ShopTabPage` 取得到");
            CheckTrue(pgBasis327 != null, "（前提）第 2 页的 `ShopTabPage` 取得到（A306⑥ 的基准取它，理由见上面那段）");
            // 🔴 **三页一起翻**（`TimerAsText` 是逐页的；本夹具只重建第 1 页，翻全部才不会踩到页号映射）
            var keepTimer = new bool[ShopData.Pages.Length];
            for (int i = 0; i < ShopData.Pages.Length; i++)
            { keepTimer[i] = ShopData.Pages[i].TimerAsText; ShopData.Pages[i].TimerAsText = false; }
            if (pgA327 != null) pgA327.Setup();                // 重建页头 ⇒ 重跑 `BuildTimeCounter`
            // ⚠️ 从**本页的根**找（`pgA327.transform`），不从 `win.transform` 找 —— 三页各有一个同名
            //   `daily shop header`，「全窗第一颗」靠遍历序，别把判据挂在遍历序上。
            var hdrA327 = pgA327 != null ? FindChild(pgA327.transform, "daily shop header") : null;
            var tcA327 = hdrA327 != null ? FindChild(hdrA327, "TimeCounter") : null;
            var icA327 = tcA327 != null ? FindChild(tcA327, "Clock Icon") : null;
            CheckTrue(icA327 != null,
                      "（前提）`daily shop header/TimeCounter/Clock Icon` 在（= 走的是 `TimerAsText = false` 那一支）");
            // （基准那一条的前提）**两页的 `TimeCounter` 必须同位** —— 否则拿另一页那颗当基准就是空转
            //（`TimeCounter` 是静态常量矩形、两页的根都是同一个 `TabsRect` ⇒ 逐位相同；这条当场证它）
            var tcBasis327 = pgBasis327 != null ? FindChild(pgBasis327.transform, "TimeCounter") : null;
            CheckTrue(tcBasis327 != null, "（前提·A306⑥ 基准）第 2 页的 `TimeCounter` 取得到");
            if (tcBasis327 != null && tcA327 != null)
                CheckNear(Vector3.Distance(tcBasis327.position, tcA327.position), 0f, 1e-3f,
                          "（前提·A306⑥ 基准）两页的 `TimeCounter` **同位** ⇒ 拿第 2 页那颗当基准**不是空转**"
                        + "（夹具的前提①/② 量的就是同一处几何）");
            if (icA327 != null && tcBasis327 != null)
                CheckScaleTwo(win.gameObject, tcBasis327,
                              () =>
                              {
                                  pgA327.Setup();                 // 🔴 态二**必须重建**（见夹具文件头 ③）
                                  var nHdr = FindChild(pgA327.transform, "daily shop header");
                                  var nTc = nHdr != null ? FindChild(nHdr, "TimeCounter") : null;
                                  var nIc = nTc != null ? FindChild(nTc, "Clock Icon") : null;
                                  // （前提③）**真的重建了**：拿到的是**新**节点 ⇒ 被量的 `localPosition` 是在
                                  // `k ≠ 1` 那一趟**重算**出来的，不是态一冻结的那份。
                                  // 改坏法：`measure` 改回纯读（`() => icA327.position`）⇒ 这条红
                                  //（而 ★ 同时退化成假绿 —— 两件事一起被挡住）。
                                  CheckTrue(nIc != null && nIc != icA327,
                                            "（前提）A306⑥：态二的 measure **真的重建了** `Clock Icon`"
                                          + "（`Setup()` 首句 `DestroyChildren(_root)` ⇒ 拿到的是新节点）");
                                  if (nIc != null) icA327 = nIc;
                                  return nIc != null ? nIc.position : Vector3.zero;   // 缺件 ⇒ ★ 也会红，⛔ 不静默
                              },
                              1.2f,
                              "A306⑥ `BuildTimeCounter` 的 else 支（`Clock Icon` 只改 x 那一行）"
                            + " —— 改坏法：`Shell/ShopWindow.cs` 里把 `PosInDesignSpace(tc).x` 换回裸"
                            + " `tc.position.x` ⇒ 偏 `(M−1)×|基准在态二的世界位置|` = 0.24×|(−4.04,3.97)|"
                            + " ⇒ x 分量 ≈ 0.97 设计单位 = **105px**（容差 0.02 = 2.2px）⇒ 红"
                            + "（`|基准|` 的实测值见 `资料/普查产出_1011/FX3_夹具砸脚八条红修复.md` §四·3）；"
                            + "⚠️ 同一处 `if/else` 的**另一支**（`AlignLeftOn`）已经由 A228 的断言守着"
                            + "（本处补的就是「一支修了一支没修」里缺的那一支）");
            for (int i = 0; i < ShopData.Pages.Length; i++) ShopData.Pages[i].TimerAsText = keepTimer[i];
            if (pgA327 != null) pgA327.Setup();                // 原样重建回去
            SmallScreenUI.Set(false);
            SmallScreenUI.PersistOverride = false;
            CheckTrue(!SmallScreenUI.Enabled, "（收尾）A327：自检跑完把开关放回**出厂值 关**");
        }

        Section("实拍");        win.tabButtons.Click(0);
        Shoot("01_商店_Cards.png");
        win.tabButtons.Click(1);
        Shoot("02_商店_Daily.png");
        win.tabButtons.Click(2);
        Shoot("03_商店_Items.png");
        win.tabButtons.Click(0);
        Debug.Log(P + "   " + win.Dump());
        Debug.Log(P + "   " + ShopData.Dump());

        // ---------------- 🆕 2026-10-03（A8）实拍：商品条目族的骨架（抽两份并排）----------------
        //   为什么单拍一张：骨架里有**九宫格 `Badge`**、**半透明 `name-bg`**、**抽屉里那张野牌图** 三处
        //   是「断言绿了但画歪」的典型场合（本工程踩过好几次）⇒ 至少留一张**能看的**。
        //   抽的这两份：`…Variant Booster_avatar_cardback_title`（正本 §五·一 拿来当「代表骨架」的那一份，
        //   339×778）+ `Small …`（339×390，唯一一份 `background` 的 `Image` 是 enabled 的）。
        Section("商品条目族（A8）实拍");
        {
            win.Close();                                   // 商店是全屏窗，不关就什么都看不见
            if (_offerScratch != null) Object.DestroyImmediate(_offerScratch.gameObject);
            if (_offerRuler != null) Object.DestroyImmediate(_offerRuler.gameObject);   // 🆕 W1-1：标尺单独一棵
            var shot = new GameObject("OfferContainer_Shot").transform;
            var vA = OfferContainer.Variants[5];           // `…Variant Booster_avatar_cardback_title`
            var vB = OfferContainer.Variants[18];          // `Small … Single Item Type`
            Check(vA.Prefab, "General Basic Offer Container Variant Booster_avatar_cardback_title",
                  "抽的是正本 §五·一 当「代表骨架」用的那一份");
            Check(vB.Prefab, "Small General Basic Offer Container Variant Single Item Type",
                  "另一份是唯一那个 `Small`（339×390）");
            for (int k = 0; k < 2; k++)
            {
                var v = k == 0 ? vA : vB;
                var c = OfferContainer.Content.Def();
                c.Item = ItemDrawer.Spec("WildcardUltramarines" + (k + 1), null, "Ultramarines");
                // 🆕 **A43**：填哪个槽由 `ItemType` 定（原来是「第一个非 INACT 的槽」，已换）。
                //   ⚠️ 这两层是分开的：**类型**选槽（本行）、**槽里画什么**走 `ItemDrawer.PickDrawer`（`ItemSpec.Kind`）。
                //   我们这一族**真物品的美术件没有**（卡背/称号的图不在工程里）⇒ 这里仍旧用野牌那张图，
                //   **只是为了实拍看得见抽屉**；类型给的是这两份都有的那两格（`Cardback Drawer` / `Title Drawer Horizontal Variant`）。
                c.ItemType = k == 0 ? "CosmeticItemCardback" : "CosmeticItemTitle";
                c.Price = k == 0 ? "1 800" : "2 000";
                var b = OfferContainer.Build(shot, v, k == 0 ? 420f : 1161f, 151f, c, 3000, null);
                CheckTrue(b.Filled != null && b.Drawer == ItemDrawer.DrawerWildcard,
                          $"并排第 {k + 1} 份的抽屉真填上了（槽 `{(b.Filled != null ? b.Filled.name : "<没填>")}` · "
                          + $"抽屉 `{b.Drawer}`）");
            }
            Shoot("05_商店_商品条目族骨架.png");
        }

        // ============================================================ §A218 `sizeDelta`（2026-10-11 新增）
        //  窗口根那一格：判据 = 原版同名 prefab `Shop Menu Variant` 的 `RectTransform`
        //  （`anchor (0,0)-(1,1)` · `sizeDelta (0,0)` · 绝对矩形 **(0,0)-(1920,1080)**，2026-10-11 现读）
        //  ⇒ 我们写进去的是**整屏矩形**。⛔ 期望值是原版那对 px，不是我们的常量。
        Section("§A218 商店窗根的 `rect` = 整屏矩形");
        {
            var a218rt = win.GetComponent<RectTransform>();
            CheckTrue(a218rt != null, "（前提）商店窗根是 `RectTransform`（A92 那半）");
            if (a218rt != null)
            {
                // 🔴 **父链缩放核查**：`rect` 与「设计 px」同量纲只在 `lossyScale == 1` 时成立。
                //    本窗 `extraScaleSmallScreen = 1.0`（不是 1.07 那种），而小屏缩放器只在**开关开**
                //    且 `≠ 1` 时才乘**窗根**那一级 ⇒ 今天恒 1（开关出厂关，见 `TransformScalerBySmallScreenUI`）。
                CheckNear(win.transform.lossyScale.x, 1f, 1e-3f,
                          "（前提·父链缩放）商店窗根的 `lossyScale.x` = 1 ⇒ `rect` 与设计 px 同量纲");
                CheckNear(a218rt.rect.width, LayoutSpace.Px(1920f), 0.01f,
                          "★ 商店窗根 `rect.width` = **1920px**（原版 `Shop Menu Variant`：stretch + `sizeDelta (0,0)`）");
                CheckNear(a218rt.rect.height, LayoutSpace.Px(1080f), 0.01f, "★ …`rect.height` = **1080px**");
                CheckTrue(a218rt.anchorMin == a218rt.anchorMax && a218rt.pivot == new Vector2(0.5f, 0.5f),
                          "★ 锚点重合 + pivot 居中（`rect` 只由 `sizeDelta` 决定；锚点**不复刻**，见 `MenuDraw.SetPxSize`）");
            }
        }

        // ============================================================ §A251-L1 `BaseOfferPopup`（2026-10-13 新增）
        //  原版那一族 = **21 个 prefab**（`Base Offer Popup` + 20 个 `General Basic Offer Popup …`），
        //  全在 `bundle_menus_assets_all`。判据 = 逐份现读：
        //    python 工具/menu_dump.py bundle_menus_assets_all "<prefab 名>" --depth 12 --relative --md
        //  （21 份逐个跑过；对账脚本 `_tmp_view/wl1/check_table.py`：**363 项 0 不符**。）
        //  🔴 **期望值一律是【冻结的原版字面量】** —— ⛔ **不读** `BaseOfferPopup.GeoSmall` /
        //     `Variants[…]`（那是**自证**：改坏 `GeoSmall` 时两处一起变、断言照样绿）。
        //  🔴 **一条不能省的判别力**：`OpenByRef()` 的**第一句**就是 `WindowsManager.EnsureHost()`
        //     —— 本文件此前**没有任何站点**走 `OpenByRef()`（现场全是直调 `win.Manager.OpenWindow(...)`，
        //     见上面 `Build()` 那段订正的最后一句）⇒ 本节是**第一个**走到那条路的，`EnsureHost`
        //     那句「`Instance` 要显式登记」改坏**从这一节开始会红**。
        Section("§A251-L1 `BaseOfferPopup` 母版 + 20 变体（窗口 / 两档几何 / 出厂显隐 / 抽屉槽 / no-data 分支 / 复用）");
        {
            // ⚠️ **本族所有变体共用同一个复用键**（= 母版 prefab 名）⇒ 下面每次 `OpenBaseOfferPopup(名)`
            //    拿回来的都是**同一个实例**（换变体 = 在它身上 `Show`）。所以断言必须**随开随断**，
            //    ⛔ 不能先开一串、再回头读第一扇的字段。
            var bo = WindowsManager.OpenBaseOfferPopup();
            Check(WindowsManager.PrefabRefBaseOfferPopup, "Base Offer Popup",
                  "★ 注册键 = **母版 prefab 名**（= `BaseOfferPopup.Variants[0].Prefab`，两处同源）");
            Check(bo.name, "Base Offer Popup", "★ 根节点名 = prefab 名");
            Check(bo.VariantName, "Base Offer Popup", "★ 默认开的是**母版**");
            Check(bo.type, WindowType.Popup, "`type` = 1 Popup（MB 原文，21/21 同值）");
            Check(bo.placement, WindowsPlacement.Popup,
                  "`windowsPlacement` = **15 Popup**（⚠️ **不是 10** —— 「商店窗是 10」那条别互推）");
            Check(bo.closeOnEsc, true, "`closeOnESC` = 1（MB 原文，21/21）");
            CheckNear(bo.extraScaleSmallScreen, 1.2f, 1e-4f, "`extraScaleSmallScreen` = **1.2**（21/21 原文）");
            // 🔴 **前提（别的 px 断言全靠它）**：窗根 `lossyScale == 1` ⇒ 世界坐标与设计 px 同量纲。
            //    小屏缩放器只在「开关开 **且** `extra ≠ 1`」时才乘窗根那一级；开关**出厂关**
            //    （`TransformScalerBySmallScreenUI`）⇒ 今天恒 1。改坏它 ⇒ 下面整段红（**先红这一条**，好在能一眼归因）。
            CheckNear(bo.transform.lossyScale.x, 1f, 1e-3f,
                      "（前提·父链缩放）窗根 `lossyScale.x` = 1 ⇒ `RectOf` 的 px 与设计 px 同量纲");

            // ---- ① 母版骨架：`window` / `Title` / `Category`（原版《--relative》现读字面量）----
            var wrt = bo.WindowNode as RectTransform;
            bool okWin = wrt != null;
            CheckTrue(okWin, "（前提）`window` 是 `RectTransform`（`MenuDraw.Node` 建的就是它）");
            if (okWin)
            {
                // 原版 `Base Offer Popup > window` = 395.72,188.35→1524.28,851.65（1128.55×663.296）
                CheckNear(wrt.rect.width, LayoutSpace.Px(1128.55f), 0.05f,
                          "★ `window` 宽 = **1128.55px**（原版 `Base Offer Popup` 现读）");
                CheckNear(wrt.rect.height, LayoutSpace.Px(663.296f), 0.05f, "★ `window` 高 = **663.296px**");
                CheckNear(PxOf(bo.WindowNode.position.x), 960.00f, 0.05f, "★ `window` 中心 x = **960.00**（原版）");
                CheckNear(PxYOf(bo.WindowNode.position.y), 520.00f, 0.05f, "★ `window` 中心 y = **520.00**（原版）");
            }
            // 右半边两段字：`Title` = 976.00,260.56→1492.72,312.56 · `Category` = 976.00,307.85→1492.72,352.85
            //  ⚠️ **量的是【渲出来】的字块**（`RectOf` 走 `Label.WorldW`）⇒ 比的是**左沿**（`AlignLeft` 把它钉在框左沿）
            //     与**中心 y**（= 框中心），不是整框 —— TMP 那段字的宽 ≠ 框宽。
            //  ⚠️ 前提：**点阵后端下 `AlignLeftOn` 是空操作**（`_tmp == null` 时它直接 return）⇒ 两条左沿断言
            //     在那种后端下会退化成没查，先断 `CanRenderChinese`（= 真 TMP 的判据，同本文件 1746 行那处）。
            { var t1 = FindChild(bo.transform, "Title");
              var t2 = FindChild(bo.transform, "Category");
              var t1lb = t1 != null ? t1.GetComponentInChildren<Label>() : null;
              CheckTrue(t1lb != null && t1lb.CanRenderChinese,
                        "（前提）`Title` 是**真 TMP** 标签 —— 点阵后端没有「按 `AlignLeft` 钉左沿」这回事");
              float ax1, ay1, ax2, ay2;
              bool okT = RectOf(t1, out ax1, out ay1, out ax2, out ay2);
              CheckTrue(okT, "（前提）`Title` 量得到矩形");
              if (okT)
              {
                  CheckNear(ax1, 976.00f, 2.0f, "★ `Title` 左沿 = **976.00**（原版现读，`AlignLeft` 钉的）");
                  CheckNear((ay1 + ay2) * 0.5f, 286.56f, 2.0f, "★ `Title` 中心 y = **286.56**（原版）");
              }
              float bx1, by1, bx2, by2;
              bool okC = RectOf(t2, out bx1, out by1, out bx2, out by2);
              CheckTrue(okC, "（前提）`Category` 量得到矩形");
              if (okC)
              {
                  CheckNear(bx1, 976.00f, 2.0f, "★ `Category` 左沿 = **976.00**（原版）");
                  CheckNear((by1 + by2) * 0.5f, 330.35f, 2.0f, "★ `Category` 中心 y = **330.35**（原版）");
              } }
            // 关闭钮（`Generic Close Button Orange`：**一颗节点带 Image + 两个孩子**）
            //  ⚠️ 这一件的图带 `keepAspect`（`UI_Button_Round_background` 是 **237×237 正方形**，而框
            //     74.39×75.61 不是正方形）⇒ **渲出来的框比设计框小 0.61px**、两边各内缩 ⇒
            //     断**中心**（`keepAspect` 保中心）而**不断边**（断边会假红 0.61px）。
            { var cl = FindChild(bo.transform, "Generic Close Button Orange");
              float cx1 = 0f, cy1 = 0f, cx2 = 0f, cy2 = 0f;
              bool okCl = cl != null && RectOf(cl, out cx1, out cy1, out cx2, out cy2);
              CheckTrue(okCl, "（前提）关闭钮量得到矩形");
              if (okCl)
              {
                  CheckNear((cx1 + cx2) * 0.5f, 1524.275f, 0.6f, "★ 关闭钮中心 x = **1524.275**（原版 `1487.08→1561.47`）");
                  CheckNear((cy1 + cy2) * 0.5f, 197.65f, 0.6f, "★ 关闭钮中心 y = **197.65**（原版 `159.85→235.45`）");
                  CheckTrue(FindChild(cl, "Background") != null && FindChild(cl, "Icon") != null,
                            "★ 关闭钮的两个孩子 `Background` / `Icon` 都在（原版三层同矩形）");
              } }

            // ---- ② 母版的 no-data 分支：三件关 + 价签那颗 `Button Text` 关 ----
            //  判据 = 原版**自己的**分支（不是我们挑的）：
            //   `SetTimer.c`（非计时活动 ⇒ `SetActive(false)`）· `SetBadgeText.c`（标签空 ⇒ 关父件）·
            //   `SetAvailableText.c`（TMP 自己 `SetActive(bool)`）· 价签 `Button Text` prefab 21/21 `act=F`
            foreach (var nm in new[] { "Timer", "Offer Badge", "Available Counter" })
            {
                var nd = FindChild(bo.transform, nm);
                CheckTrue(nd != null, $"★ `{nm}` **建出来了**（不是没建）");
                CheckTrue(nd != null && !nd.gameObject.activeSelf,
                          $"★ `{nm}` **建成关着** —— 原版没有报价时走的就是这一支（见 `BaseOfferPopup.cs` 文件头）");
            }
            // 🔴 **灭自证/弱断言**：上面三条「关着」若被改成「整棵树都没建」也会绿 ⇒ 补一条**反例**：
            //    同一棵树上必须有**开着**的兄弟件（`Title` / `Preview`）。
            { var tOK = FindChild(bo.transform, "Title");
              var pOK = FindChild(bo.transform, "Preview");
              CheckTrue(tOK != null && tOK.gameObject.activeInHierarchy && pOK != null && pOK.gameObject.activeInHierarchy,
                        "★ 反例：同一棵树上 `Title` / `Preview` **是开着的** ⇒ 上面那三条不是「整棵树都没建」"); }
            CheckTrue(bo.PriceBtnTextNode != null && !bo.PriceBtnTextNode.gameObject.activeSelf,
                      "★ 价签里那颗 `Button Text` 关着（原版 prefab 21/21 实读 `act=F`）");
            CheckTrue(bo.DrawerNodes.Count == 0, "★ 母版**一个抽屉槽都没有**（原版 37 节点，与 `Just Foreground` 逐节点同构）");

            // ---- ③ 出厂显隐：**逐份不同、而且不是同一批**（铁律 5·c「一个值 ≠ 全部情况」）----
            { bool okArt = bo.ArtworkNode != null && bo.ArtFgNode != null;
              CheckTrue(okArt, "（前提）`Artwork` / `foreground` 两个节点都建出来了");
              if (okArt)
                  CheckTrue(bo.ArtworkNode.gameObject.activeSelf && bo.ArtFgNode.gameObject.activeSelf,
                            "母版：`Artwork` 与 `foreground` 出厂**都开**"); }
            var artv = WindowsManager.OpenBaseOfferPopup("General Basic Offer Popup Booster_CardOrAltArt");
            CheckTrue(ReferenceEquals(artv, bo), "（前提）同一复用键 ⇒ 拿回来的是**同一个实例**（换变体走 `Show`）");
            Check(artv.VariantName, "General Basic Offer Popup Booster_CardOrAltArt", "★ 变体切过去了");
            { bool okArt = artv.ArtworkNode != null && artv.ArtFgNode != null;
              CheckTrue(okArt, "（前提）换变体之后两个节点都还在");
              if (okArt)
              {
                  CheckTrue(!artv.ArtworkNode.gameObject.activeSelf,
                            "★ 这一份的 `Artwork` **出厂关**（原版 `act=F`）");
                  CheckTrue(artv.ArtFgNode.gameObject.activeSelf,
                            "★ …而它的 `foreground` 出厂**开** ⇒ 同一份里两件不同（两件不能合并成一个开关）");
              } }
            Check(artv.DrawerNodes.Count, 3, "★ 这一份 3 个抽屉槽（原版现读）");
            { var cd = FindChild(artv.transform, "Card Drawer");
              CheckTrue(cd != null, "（前提）`Card Drawer` 槽建出来了");
              CheckTrue(cd != null && !cd.gameObject.activeSelf,
                        "★ `Card Drawer` **出厂关**（这一份的槽里唯一关着的那个）"); }

            // ---- ④ 两档几何（**必须能分辨**）----
            var big = WindowsManager.OpenBaseOfferPopup("General Basic Offer Popup Variant Premium_Resource");
            var brt = big.WindowNode as RectTransform;
            bool okBigWin = brt != null;
            CheckTrue(okBigWin, "（前提）大档的 `window` 也是 `RectTransform`");
            if (okBigWin)
            {
                // 原版 `…Variant Premium_Resource > window` = 246.94,159.58→1673.06,880.42（1426.11×720.843）
                CheckNear(brt.rect.width, LayoutSpace.Px(1426.11f), 0.05f,
                          "★ 大档 `window` 宽 = **1426.11px**（≠ 母版 1128.55 ⇒ 两档**可分辨**）");
                CheckNear(brt.rect.height, LayoutSpace.Px(720.843f), 0.05f, "★ 大档 `window` 高 = **720.843px**");
                CheckNear(PxOf(big.WindowNode.position.x), 960.00f, 0.05f, "★ 大档 `window` 中心 x = 960.00（两档同中心）");
            }
            // 🔴 与 ③ 的**另一态**：这一份是 `Artwork` **开**、`foreground` **关** —— 与 `Booster_CardOrAltArt` **正好相反**。
            { bool okArt = big.ArtworkNode != null && big.ArtFgNode != null;
              CheckTrue(okArt, "（前提）大档两个节点都建出来了");
              if (okArt)
                  CheckTrue(big.ArtworkNode.gameObject.activeSelf && !big.ArtFgNode.gameObject.activeSelf,
                            "★ `Premium_Resource`：`Artwork` 开 / `foreground` 关 —— 与 `Booster_CardOrAltArt` **正好相反**"
                            + "（两处合用一个开关的话，这两条必有一条红）"); }
            { var wt = FindChild(big.transform, "Text");
              float x1 = 0f, y1 = 0f, x2 = 0f, y2 = 0f;
              bool okBig = wt != null && RectOf(FindChild(wt, "Title"), out x1, out y1, out x2, out y2);
              CheckTrue(okBig, "（前提）大档 `Title` 量得到矩形");
              if (okBig)
              {
                  CheckNear(x1, 1120.00f, 1.5f, "★ 大档 `Title` 左沿 = **1120.00**（≠ 小档 976.00 ⇒ 大档不是整棵平移）");
              } }

            // ---- ⑤ 抽屉槽：**个数 / 兄弟序 / 旋转 / 出厂态**（抽槽最多的那一份 `Single Item Type`，10 槽）----
            var sit = WindowsManager.OpenBaseOfferPopup("General Basic Offer Popup Variant Single Item Type");
            Check(sit.DrawerNodes.Count, 10, "★ `…Single Item Type` **10 个槽**（21 份里最多；原版现读）");
            string[] wantSit =
            {
                "Icon Container Drawer Variant", "Title Drawer Horizontal Variant",   // 兄弟序逐字照 prefab
                "Title Drawer Horizontal Variant (1)", "Icon Avatar Drawer Variant",
                "Icon Avatar Drawer Variant (1)", "Icon Currency Drawer Variant",
                "Icon Currency Drawer Variant (1)", "Icon Currency Drawer Variant (2)",
                "Cardback Drawer", "Icon Premium Campaign Drawer Variant",
            };
            for (int i = 0; i < wantSit.Length && i < sit.DrawerNodes.Count; i++)
                Check(sit.DrawerNodes[i].name, wantSit[i], $"★ 第 {i + 1} 槽的名字（**兄弟序**照 prefab）");
            if (sit.DrawerNodes.Count == 10)
            {
                // 原版 `Icon Currency Drawer Variant (2)` 的 `m_LocalRotation` 绕 z = **−0.25°** ⇒ `eulerAngles.z` = 359.75
                CheckNear(sit.DrawerNodes[7].localRotation.eulerAngles.z, 359.75f, 0.05f,
                          "★ 第 8 槽（`…(2)`）的旋转 = **−0.25°**（原版 `m_LocalRotation` 实读）");
                // 原版 `Cardback Drawer` 的 rot = **4.99°**
                CheckNear(sit.DrawerNodes[8].localRotation.eulerAngles.z, 4.99f, 0.05f,
                          "★ 第 9 槽（`Cardback Drawer`）的旋转 = **4.99°**");
                // 判据「**转的是它自己**」：同窗里另一个带旋转的槽不等于它（防「一个值套全窗」）
                CheckTrue(Mathf.Abs(sit.DrawerNodes[8].localRotation.eulerAngles.z
                                    - sit.DrawerNodes[7].localRotation.eulerAngles.z) > 1f,
                          "★ 反例：两个槽的旋转**不一样** ⇒ 上面两条不是「整窗一个角度」");
                // 抽屉槽的矩形（画布 px · 左沿/上沿）—— 冻结原版字面量
                CheckNear(PxOf(sit.DrawerNodes[0].position.x), (416.34f + 971.46f) * 0.5f, 0.6f,
                          "★ 第 1 槽中心 x = **693.90**（原版 `416.34→971.46`）");
                CheckNear(PxYOf(sit.DrawerNodes[0].position.y), (252.74f + 807.86f) * 0.5f, 0.6f,
                          "★ 第 1 槽中心 y = **530.30**");
            }
            // 抽屉类名：**本表自带**（实读 prefab 的 `m_Script`）+ 与 `OfferContainer.SlotTypes` **互核**
            //  🔴 两处**独立的**读数相同 ⇒ 不是自证；共有的名字两边必须一致，本族独有的 6 个
            //  （`Card Drawer (1)` / `Icon Currency Drawer Variant (2)` / `Icon Avatar Drawer Variant (1)` /
            //    `Icon Premium Campaign Drawer Variant 2` / `Avatar Border Drawer Shop Variant` /
            //    `Icon Expansion Pass Premium Drawer Variant Variant`）在那边**查不到** ⇒ 本表必须自带类名。
            {
                int nSlot = 0, nShared = 0, nOnly = 0, nMismatch = 0, nNull = 0;
                foreach (var vv in BaseOfferPopup.Variants)
                {
                    if (vv.Drawers == null) continue;
                    foreach (var d in vv.Drawers)
                    {
                        nSlot++;
                        if (string.IsNullOrEmpty(d.Cls)) nNull++;
                        var oc = OfferContainer.DrawerClassOf(d.Name);
                        if (oc == null) nOnly++;
                        else { nShared++; if (oc != d.Cls) nMismatch++; }
                    }
                }
                Check(nSlot, 86, "★ 21 份的抽屉槽**合计 86 个**（逐份现读相加）");
                Check(nShared, 80, "★ 其中 **80 个**槽名在 `OfferContainer.SlotTypes` 里也有一份");
                Check(nOnly, 6, "★ 本族独有 **6 个**槽名（`OfferContainer` 那张表里没有 ⇒ 类名只能本表自带）");
                Check(nNull, 0, "★ 86 个槽**每一个都有类名**（⛔ 没有一个留空 —— 留空就是「静默不认路」）");
                Check(nMismatch, 0, "★ 共有的 80 个槽，**两处的类名逐个一致**（两张独立读数的表互核）");
            }

            // ---- ⑥ 复用 / 关掉再开（**两态可分**，照原版 `automaticallyLoadedWindows`）----
            {
                var titleBefore = FindChild(sit.transform, "Title");
                var again = WindowsManager.OpenBaseOfferPopup("General Basic Offer Popup Variant Single Item Type");
                CheckTrue(ReferenceEquals(again, sit), "★ 同键 + 同变体再开 ⇒ **同一实例**（原版缓存命中复用）");
                CheckTrue(ReferenceEquals(titleBefore, FindChild(again.transform, "Title")),
                          "★ 而且**内容没重建**（`Title` 还是同一个节点对象）—— 照原版 `TryOpen` 的 `Open` 支"
                          + "（同窗再开一个字段都不写；⛔ 把 `Show` 里那道「没变⇒不重建」的守卫删掉这条就红）");
                var swapped = WindowsManager.OpenBaseOfferPopup("General Basic Offer Popup Variant Premium_Resource");
                Check(swapped.VariantName, "General Basic Offer Popup Variant Premium_Resource",
                      "★ 换变体 ⇒ 同一实例上**真的换了**（`Show` 重建）");
                CheckTrue(!ReferenceEquals(titleBefore, FindChild(swapped.transform, "Title")),
                          "★ 反例：**换了变体就重建**（`Title` 是新的节点对象）⇒ 与上面那条合起来能分辨两种状态");
            }
            // 关掉之后再开 ⇒ **新建一扇**（原版 `CloseWindowCO` 会把缓存条目删掉；我们惰性删）
            {
                var old = sit;
                old.Close();
                CheckTrue(old.CurrentState == WindowState.Closed, "（前提）关掉了");
                var fresh = WindowsManager.OpenBaseOfferPopup();
                CheckTrue(!ReferenceEquals(fresh, old),
                          "★ 关过之后再开 ⇒ **新建一扇**（照原版：关窗会把缓存条目删掉，复用只在「还开着」时成立）");
                Check(fresh.VariantName, "Base Offer Popup", "★ 新建的这扇回到**母版**（`variant` 传了 null）");
                fresh.Close();     // 收干净：本族是弹窗，留着会压在后面的断言/实拍上
            }
        }

        // ============================================================ §A251-L2/L4 四扇小窗（2026-10-13 新增）
        //  原版四扇，全在 `bundle_menus_assets_all`（判据 = 逐份现读）：
        //    `Generic Options Panel`(8) · `Member Options Panel`(22) · `Purchase Premium Window`(34) ·
        //    `Ranked Boost Reward Event Window`(17)
        //    python 工具/menu_dump.py bundle_menus_assets_all "<prefab 名>" --depth 12 --relative --md
        //  对账脚本 `_tmp_view/wl2/check_table.py`：**80 项 0 不符**（表 ↔ 现读逐格比）。
        //  🔴 **期望值一律是【冻结的原版字面量】**（下面每一个数都能在上面那条命令的输出里逐字找到）
        //     —— ⛔ **不读** `GenericOptionsPanel.RootH` / `Recon` / `PurchasePremiumWindow.Abs` 那几张表
        //     （那是**自证**：改坏常量时两处一起变、断言照样绿）。
        //  ⚠️ **本节的四个宿主都在 `Shell/` 新文件里**，注册口在 `WindowsManager`（四条 `PrefabRef*` + 四个 `Open*`）。
        //     走 `Open*` 而不是直调 `Create` ⇒ 顺带盯住 `OpenByRef()` 那条路（它第一句是 `EnsureHost()`）。
        Section("§A251-L2/L4 四扇小窗（Generic Options Panel / Member Options Panel / Purchase Premium Window / Ranked Boost Reward Event Window）");
        {
            // ---------------- L2-① `GenericOptionsPanel`（8 节点） ----------------
            var gop = WindowsManager.OpenGenericOptionsPanel();
            Check(WindowsManager.PrefabRefGenericOptionsPanel, "Generic Options Panel", "★ 注册键 = prefab 根名");
            Check(gop.name, "Generic Options Panel", "★ 根节点名 = prefab 名（`WindowsManager` 复用的键同源）");
            Check(gop.type, WindowType.Popup, "`type` = 1 Popup（MB 原文）");
            Check(gop.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup");
            // 🔴 **判别力最强的一条**：同族三扇根上带成品的窗，**只有这一扇 `closeOnESC = 0`**
            //    （`Alliance Trophy Info Popup` = 1、`Member Options Panel` = 1）⇒ 拿 1 去断这里**必红**。
            Check(gop.closeOnEsc, false, "★ `closeOnESC` = **0**（MB 原文 —— 同族另两扇是 1，⛔ 别互推）");
            CheckNear(gop.extraScaleSmallScreen, 1.0f, 1e-4f, "`extraScaleSmallScreen` = 1.0（= **不覆盖**）");
            var gopSc = gop.GetComponent<TransformScalerBySmallScreenUI>();
            CheckTrue(gopSc != null, "（前提）窗根上**烤着**一颗 `TransformScalerBySmallScreenUI`（原版 prefab 上就有）");
            CheckNear(gopSc != null ? gopSc.menuScale : -1f, 1.35f, 1e-3f,
                      "★ 烤着的 `menuScale` = **1.35**（`extra = 1.0` 的「不覆盖」那一档真正生效的就是它）");
            // 层级（**直接子件**，⛔ `FindChild` 走整棵子树 ⇒ 那样写恒真，见 `TrophyInfoPopup` 那条教训）
            Check(KidNames(gop.transform),
                  "Menu Dark Background|BackgroundHit|bg shadow|bg|Name|*Template|Buttons",
                  "★ 根的直接子件（兄弟序照 `m_Children`；`BackgroundHit` 是**我们**的命中区节点；"
                  + "`*Template` 那个 `*` = **出厂关着** —— `KidNames` 的约定）");
            // 🔴 `Template` 运行期是**关着**的（`GenericOptionsPanel__Start.c` 第一句 SetActive(false)）
            CheckTrue(HasChild(gop.transform, "Template"), "（前提）`Template` 节点在");
            Check(gop.TemplateNode != null && gop.TemplateNode.gameObject.activeSelf, false,
                  "★ `Template` 建成**关着**的（判据 = `GenericOptionsPanel__Start.c`：`SetActive(false)`）");
            // 反例（灭自证）：同一棵树上必须有**开着**的件 ⇒「整棵树都没建」蒙不过去
            CheckTrue(HasChild(gop.transform, "Name") && FindChild(gop.transform, "Name").gameObject.activeSelf,
                      "★（反例）`Name` **是开着的** ⇒ 「三件关着」不是「整棵树没建」蒙出来的");
            // no-data 分支：0 颗按钮 ⇒ `Buttons` 是空容器（原版 `SetButtons` 传空表就这一支）
            Check(gop.ButtonNames().Count, 0, "★ no-data：`Buttons` 直系子件 **0** 个（原版 `SetButtons(空表)`）");
            Check(gop.BuiltButtons, 0, "★ no-data：`BuiltButtons` = 0");
            // 运行期几何（**冻结字面量**；根顶 636.70 = 1080 − (540 + (−96.7)) 那一档，见文件头）
            CheckAt(FindChild(gop.transform, "Name"), 782.50f, 1137.50f, 646.70f, 683.30f,
                    "★ `Name` 在运行期那一档的框里（顶 = 面板顶 636.70 + padding 10）");
            CheckAt(FindChild(gop.transform, "Buttons"), 960.00f, 960.00f, 688.30f, 688.30f,
                    "★ 运行期 `Buttons` 落在 `Name` 下（636.70 + 10 + 36.6 + 5 = **688.30**）"
                    + " —— 这一条就是「`Template` 关掉之后布局重算」的判别式（出厂档它应在 753.30）");
            CheckAt(FindChild(gop.transform, "bg"), 766.35f, 1153.65f, 636.70f, 698.30f,
                    "★ 面板底 `bg` = **运行期**那道框（高 61.6：10 + 36.6 + 5 + 0 + 10）");
            // 出厂显隐：`Menu Dark Background` 的 tint 是 **a = 0**（这一族特有，别的窗是 0.773）
            var gopShade = FindChild(gop.transform, "Menu Dark Background");
            CheckTrue(gopShade != null && gopShade.gameObject.activeSelf,
                      "★（反例）整屏底建出来了而且**开着**（a = 0，靠它吃点击）");
            // 缺图（⚠️ **2026-10-14 订正**：原来写「`OctagonUI Filled SDF` 工程里没有 ⇒ 这一格不画」——
            //   那张图已于 2026-10-06 12:31 由素材腿拷进 `Resources/Art/ui_menu/OctagonUI_Filled_SDF.png`
            //   （`.meta` 12:40 生成，早于本 run 的 12:58）⇒ 本窗**一张都不缺**，原来的 `Count == 1` 是过期期望。
            //   判据 = ① 图在盘上 ② 日志里 `[OptionsPanel]` 只有 2 行、两行都是「开了…」，
            //   **一条「图取不到」都没有** —— `Tex()` 取不到必 `Debug.LogWarning("[OptionsPanel] 图取不到：…")`
            //   （`Shell/GenericOptionsPanel.cs:479-489`），而 `Tex(ArtShadow, …)` 在 `Build()` 路径上是
            //   **无条件**调的（`Shell/GenericOptionsPanel.cs:364`）⇒ 这个 `0` 是**实读数**、不是「没跑到所以空表」。
            //   🔴 但**别把它当成稳的**：`Resources/Art/ui_menu/` 下那 13 张手拷图**没有任何导入器登记**
            //   （= A808）⇒ 谁跑一次 `import_original_art.py`（或删 `Resources/Art/`）这条会**静默翻回红**。）
            // 🆕 2026-10-15（A810①）：走公共口（「0 才绿」的唯一一份）；上面那整段订正留档不动
            CheckNoMissingArt(gop.MissingArt,
                "★ 泛用选项窗（`OctagonUI Filled SDF` 已进 `Resources/Art/ui_menu/` —— 源在 `bundle_duplicateassetisolation_assets_all`）");
            // 复用：同一个键再开一次 ⇒ **同一实例**（照原版 `automaticallyLoadedWindows`）
            var gop2 = WindowsManager.OpenGenericOptionsPanel();
            CheckTrue(ReferenceEquals(gop2, gop), "★ 同键再开 ⇒ **同一实例**（照原版 `automaticallyLoadedWindows` 命中复用）");
            gop.Close();
            var gop3 = WindowsManager.OpenGenericOptionsPanel();
            CheckTrue(!ReferenceEquals(gop3, gop), "★ **关过再开 ⇒ 新建一扇**（复用只在「还开着」时成立）");
            gop3.Close();

            // ---------------- L2-② `AllianceMemberOptionsPopup`（22 节点） ----------------
            var amop = WindowsManager.OpenAllianceMemberOptions();
            Check(amop.name, "Member Options Panel", "★ 根节点名 = prefab 名");
            Check(amop.type, WindowType.Popup, "`type` = 1 Popup（MB 原文）");
            Check(amop.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup");
            Check(amop.closeOnEsc, true, "★ `closeOnESC` = **1**（同族的 `GenericOptionsPanel` 是 0 —— 逐扇实读）");
            CheckNear(amop.extraScaleSmallScreen, 1.0f, 1e-4f, "`extraScaleSmallScreen` = 1.0（= 不覆盖）");
            // 层级：**没有 `Template`**（这一扇根的直接子件只有 5 个 —— ⛔ 别照上一扇顺手补一颗）
            Check(KidNames(amop.transform), "Menu Dark Background|BackgroundHit|bg shadow|bg|Name|Buttons",
                  "★ 根的直接子件（**没有 `Template`** —— 两扇的骨架差就在这一件上）");
            // 八颗按钮：**直系子件 + 兄弟序**（字段偏移那一列是**另一回事**：`Challenge` 排第一却挂在 `+0xA8`）
            Check(string.Join("|", amop.ButtonNames().ToArray()),
                  "Challenge|Add as a friend|Profile|Promote|Demote|Kick|Quit|Debug Add Skulls",
                  "★ 八颗按钮的**兄弟序**（照 `m_Children`，`Challenge` 第一）");
            CheckHasKids(amop.BtnNode(0), "Image", "Hit", "Button Text", "★ 每颗按钮的直系子件 = `Image` / `Hit` / `Button Text`");
            // 运行期显隐模型（`Open()` 逐句）：**没数据 ⇒ 照 prefab 出厂态**（八颗全开，`Debug` 除外）
            Check(amop.HasData, false, "★ 出厂那一档：`HasData = false`（本地没有服务器）");
            Check(amop.BtnNode(7) != null && amop.BtnNode(7).gameObject.activeSelf, false,
                  "★ `Debug Add Skulls` **关着**（判据 = `Awake()` 第一句无条件 `SetActive(false)`）");
            CheckTrue(amop.BtnNode(0) != null && amop.BtnNode(0).gameObject.activeSelf,
                      "★（反例）`Challenge` **开着** ⇒ 「有一颗关着」不是「八颗都没建」蒙出来的");
            Check(amop.BtnText(3) != null ? amop.BtnText(3).Text : "-", "Promote",
                  "★ `Promote` 那颗的字 = prefab 出厂原文（原版运行期会按 role 换成两个 I2 词条之一，本地没有词条表）");
            Check(amop.BtnText(7) != null ? amop.BtnText(7).Text : "-", "ADD SKULLS TO CURRENT EVENT",
                  "★ `Debug Add Skulls` 的字（**唯一一颗带全大写长文案的**）");
            // 几何（冻结字面量 · 绝对框）
            CheckAt(FindChild(amop.transform, "Name"), 782.50f, 1137.50f, 196.70f, 233.30f, "★ `Name` 的框");
            CheckAt(amop.BtnNode(0), 781.35f, 1138.65f, 238.30f, 298.30f, "★ 第 1 颗按钮（`Challenge`）的框");
            CheckAt(amop.BtnNode(7), 781.35f, 1138.65f, 693.30f, 753.30f, "★ 第 8 颗按钮的框（步进 65 × 7）");
            CheckAt(FindChild(amop.transform, "Buttons"), 960.00f, 960.00f, 238.30f, 753.30f,
                    "★ `Buttons` 容器（零宽 · 高 515 = 60×8 + 5×7）");
            // 两态：喂一份数据 ⇒ `Quit`（`isSelf`）与 `Challenge`（`!isSelf`）**互斥**（判别式）
            //  ⚠️ **2026-10-14 订正（夹具）**：`MyRole` 原来写 `3` ⇒ `Role(0) < MyRole(3)` **成立** ⇒ `outrank = true`
            //     ⇒ 下面那条「`Promote` 关着」本来就**必红**（实现是对的，是夹具与断言文案的前提不符）。
            //     判据（原版规则，`Shell/AllianceMemberOptionsPopup.cs` 文件头 `:26` + `:179` + `:396-399`）：
            //       `outrank = sameGroup && role < myRole` · `Promote = outrank && role < 3` ·
            //       `Demote = outrank && role > 0` · `Kick = outrank` · `Quit = isSelf` · `Challenge = !isSelf`
            //     ⇒ 要断「`outrank` 那道闸把 `Promote` 关住」，夹具必须给 `MyRole = 0`（`0 < 0` 为假）。
            //     ⛔ **别改成把下面那条 `Promote` 的期望写成 `true`** —— 那样就不再检验 `outrank` 了（弱化）。
            //     影响面已逐一核过：同段 `Quit` 那条只看 `isSelf`、`Challenge` 那条只看 `!isSelf`，都不含 `MyRole`。
            var mv = new AllianceMemberOptionsPopup.MemberView
            { Name = "Tester", Role = 0, IsSelf = true, IsFriend = false, SameGroup = true, MyRole = 0 };
            amop.SetMember(mv);
            Check(amop.HasData, true, "（两态）`SetMember` 之后 `HasData = true`");
            CheckTrue(amop.BtnNode(6) != null && amop.BtnNode(6).gameObject.activeSelf,
                      "★ 两态·自己那一员 ⇒ `Quit` 开（`Open()`: `quitButton.SetActive(myId == member.Id)`）");
            CheckTrue(amop.BtnNode(0) != null && amop.BtnNode(0).gameObject.activeSelf == false,
                      "★ 两态·自己那一员 ⇒ `Challenge` **关**（`!isSelf`）—— 与上一条**互斥**");
            CheckTrue(amop.BtnNode(3) != null && amop.BtnNode(3).gameObject.activeSelf == false,
                      "★ 两态·`Role = Member(0)` ⇒ `Promote` 也要 `outrank`（`role < MyRole` 不满足那一支关着）");
            // 🆕 **2026-10-15（A810①）**：这一扇的主图池也收进「0 才绿」（它与 `win`/`pop` 是同一档 ——
            //   此前**只有出声、没有牙**：`Shell/AllianceMemberOptionsPopup.cs` 的 `Tex()` 在取不到时
            //   `LogWarning("[MemberOptions] 图取不到：…")` 并记账，但**全仓没有一处读 `MissingArt`**）。
            //   📌 实读依据：本轮最后一次全套跑的 stdout（`d:/4/_tmp_view/shop.log`）里
            //   `[MemberOptions] 开了 'Member Options Panel'` 在、而 **`[MemberOptions] 图取不到` 零命中**
            //   ⇒ 当时就是 0。⚠️ 不是「没跑到」的平凡 0：本窗在上面已经建过、喂过两态数据（`SetMember`）。
            CheckNoMissingArt(amop.MissingArt, "★ 成员选项面板");
            amop.Close();

            // ---------------- L2-③ `PurchasePremiumWindow`（34 节点） ----------------
            var ppw = WindowsManager.OpenPurchasePremiumWindow();
            Check(ppw.name, "Purchase Premium Window", "★ 根节点名 = prefab 名");
            Check(ppw.type, WindowType.Popup, "`type` = 1 Popup（MB 原文）");
            Check(ppw.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup");
            Check(ppw.closeOnEsc, true, "`closeOnESC` = 1（MB 原文）");
            CheckNear(ppw.extraScaleSmallScreen, 1.0f, 1e-4f, "`extraScaleSmallScreen` = 1.0");
            Check(ppw.FocusArmy, 10, "★ `TryOpen` 里 `data` 为空 ⇒ 聚焦 **`10` = `CardArmy.Ultramarines`**（枚举实读）");
            // 层级：根 8 个直系子件，且 **`Scrollbar Collection` 是 `Scroll View` 的子件**（⭐ 这条是对账脚本抓出来的）
            Check(KidNames(ppw.transform),
                  "Menu Dark Background|BackgroundHit|Generic Window Red Background Big|Title|*SubTitle|"
                  + "Premium image|Army Info|Scroll View|Generic Close Button Orange",
                  "★ 根的直接子件（原版 8 件 + 我们的 `BackgroundHit` = **9**；兄弟序照 `m_Children`；"
                  + "`*SubTitle` 那个 `*` = 出厂 `act = F`）");
            CheckHasKids(FindChild(ppw.transform, "Scroll View"), "Viewport", "Scrollbar Collection",
                         "★ `Scrollbar Collection` 是 **`Scroll View` 的子件**（⛔ 不是根的直系子件）");
            CheckHasKids(FindChild(ppw.transform, "Generic Close Button Orange"), "Image", "Hit", "Background", "Icon",
                         "★ 关窗钮的直系子件（`Background`/`Icon` 是**子件**，⛔ 不是兄弟）");
            // 出厂显隐三件（prefab 实测）
            CheckTrue(FindChild(ppw.transform, "SubTitle") != null
                      && FindChild(ppw.transform, "SubTitle").gameObject.activeSelf == false,
                      "★ `SubTitle` 出厂 **关着**（prefab `act = F`）");
            CheckTrue(FindChild(ppw.transform, "Button Text") != null
                      && FindChild(ppw.transform, "Button Text").gameObject.activeSelf == false,
                      "★ 价签钮的 `Button Text` 出厂 **关着**（那是备用文本）");
            CheckTrue(FindChild(ppw.transform, "Scrollbar Collection") != null
                      && FindChild(ppw.transform, "Scrollbar Collection").gameObject.activeSelf == false,
                      "★ `Scrollbar Collection` 出厂 **关着**（⇒ 它的 `Sliding Area` / `Handle` 整棵不画）");
            CheckTrue(FindChild(ppw.transform, "Purchased Text") != null
                      && FindChild(ppw.transform, "Purchased Text").gameObject.activeSelf,
                      "★（反例）`Purchased Text` **开着** ⇒ 上面三条不是「整棵树没建」蒙出来的");
            // no-data：0 个容器、模板关着（= 原版 `Initialize()` 尾段那一句）
            Check(ppw.Offers.Length, 0, "★ no-data：报价 **0** 条（`premiumStoreReference` 在 prefab 里就是空引用）");
            Check(ppw.Containers.Count, 0, "★ no-data：`Content` 下 **0** 个容器");
            CheckTrue(ppw.ContainerTemplate != null && ppw.ContainerTemplate.gameObject.activeSelf == false,
                      "★ `Army Container` **模板关着**（判据 = `Initialize.c` 尾段 `SetActive(false)`）");
            CheckHasKids(ppw.ContentNode, "Army Container", "★ `Content` 的直系子件只有模板那一颗（+ `Content` 上那颗 VLG）");
            // 开场动画（`OnEnable`：alpha 0→1 + scale 0.8→1，0.3s）—— 两态都断
            // 🔴 **2026-10-14 就地订正**：这一段原来排在**几何那一段之后** ⇒ 下面那四条 `CheckAt` 量到的
            //   是**动画起点**（根 scale = 0.8）⇒ 四条的坐标**全体偏 20%**、全红（实测 Title 差 188.09px、
            //   Army Info 61.55px、窗体底 4.07px、Content 119.40px —— 反解出来 scale 恰 = 0.8、原点 = 屏幕中心）。
            //   **批处理没有帧循环** ⇒ `Update()` 不推、动画永远停在起点 ⇒ **必须先 `FinishPopForTest()`
            //   再量几何**（这是本文件的规矩，同样适用于以后往这一段里插的断言）。
            Check(ppw.PopDone, false, "★ 刚开出来动画**还没跑完**（`canvasGroup.alpha` 从 0 起）");
            ppw.FinishPopForTest();
            Check(ppw.PopDone, true, "★ 推到终点之后 `PopDone = true`");
            CheckNear(ppw.transform.localScale.x, 1f, 1e-3f, "★ 动画终点缩放 = **1**（起点是 0.8）");
            // 几何（冻结字面量 · **绝对框 = 相对框 + (167.175, 70.94)**，逐位核过）
            // 🔴 **2026-10-14**：`Title` 那颗原版是 **`H=1 (Left)`**、我们走 `MenuDraw.AlignLeft`
            //   ⇒ 断**左沿**（`CheckLeftAt`），⛔ 别拿「框中心」量它（那一条永远红，本批实测差 132.67px）。
            CheckLeftAt(FindChild(ppw.transform, "Title"), 923.505f, 1664.845f, 128.43f, 212.65f, "★ `Title` 的框");
            CheckAt(FindChild(ppw.transform, "Army Info"), 809.925f, 1714.345f, 254.40f, 942.60f, "★ `Army Info` 的框");
            CheckAt(FindChild(ppw.transform, "Generic Window Red Background Big"),
                    182.265f, 1772.935f, 55.64f, 1044.89f, "★ 窗体底（比根大一圈：1590.67×989.256）");
            CheckAt(FindChild(ppw.transform, "Content"), 207.715f, 775.615f, 75.57f, 263.89f,
                    "★ `Content`（`CSF v:PreferredSize` 跑完那一档：高 188.326）");
            // 缺图（⚠️ **2026-10-14 订正**：原来写「`UI_HIghlight Internal` 只在 `Art/原版/0_mainmenu/` 里躺着、
            //   没进 `Resources/`」—— 那张图已于 2026-10-06 12:31 拷进
            //   `Resources/Art/ui_menu/UI_HIghlight_Internal.png`（`.meta` 12:40）⇒ 期望是 **0**。
            //   判据 = ① 图在盘上 ② 日志里**没有** `UI_HIghlight Internal` 的「图取不到」告警 —— 同 run 里
            //   `[Premium]` 只报了 `Army Icon` 那一张 ⇒ 这条口是活的、没对高亮图报缺。
            //   `Tex(ArtHightlight, …)`（`Shell/PurchasePremiumWindow.cs:701`）在 `Build()` 路径上**无条件**调
            //   ⇒ A803 补上 `Build()` 之后这个 `0` 才是**实读数**（本 run 拿到的 0 是 F1 那段「没跑」的平凡值）。
            //   ⚠️ 原来紧随其后还有两行「缺的是哪个名字」（`Count == 0` 时三元取 `"-"`、与任何图名恒不等，
            //   留着必红且已无可断言对象）⇒ **已随之删掉**。
            //   🔴 那 13 张手拷图**没有任何导入器登记**（= A808）⇒ 谁跑一次 `import_original_art.py` 就翻红。）
            // 🆕 2026-10-15（A810①）：走公共口（「0 才绿」的唯一一份）；上面那整段订正留档不动
            CheckNoMissingArt(ppw.MissingArt,
                  "★ 高级包窗（`UI_HIghlight Internal` 已进 `Resources/Art/ui_menu/`）");
            // 开场动画那三条断言**已上移到几何之前**（见上面那段 🔴 订正 —— 量几何前必须先推到动画终点）。
            // 两态：喂一条报价 ⇒ 容器建出来、`Purchased` 决定价签与 `Purchased!` 谁开
            ppw.OpenEx(10, new[]
            {
                new PurchasePremiumWindow.ArmyOffer { Army = 10, ArmyName = "Ultramarines", Purchased = false, PriceText = "300,00", PremiumUnlocked = true },
                new PurchasePremiumWindow.ArmyOffer { Army = 20, ArmyName = "Goff",         Purchased = true,  PriceText = "300,00", PremiumUnlocked = false },
            });
            Check(ppw.Containers.Count, 2, "★ 两态：喂 2 条报价 ⇒ `Content` 下 **2** 个容器");
            CheckTrue(ppw.ContainerTemplate != null && ppw.ContainerTemplate.gameObject.activeSelf == false,
                      "★（灭自证）建完容器之后**模板仍然是关的**（原版 `Initialize` 尾段那一句）");
            CheckTrue(ppw.Containers.Count > 0 && ppw.Containers[0].gameObject.activeSelf,
                      "★（反例）第 1 个容器**开着**");
            CheckAt(ppw.Containers.Count > 0 ? ppw.Containers[0] : null,
                    226.055f, 757.285f, 100.57f, 263.89f, "★ 第 1 个容器的框（`Content` 顶 + 25 起排）");
            ppw.Close();

            // ---------------- L4 `RankedRewardEventWindow`（17 节点） ----------------
            var rre = WindowsManager.OpenRankedRewardEvent();
            Check(rre.name, "Ranked Boost Reward Event Window", "★ 根节点名 = prefab 名");
            Check(rre.type, WindowType.Fullscreen, "★ `type` = **0 Fullscreen**（MB 原文 —— 四扇里只有它是全屏档）");
            Check(rre.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup");
            Check(rre.closeOnEsc, true, "`closeOnESC` = 1（MB 原文）");
            CheckNear(rre.extraScaleSmallScreen, 1.2f, 1e-4f, "★ `extraScaleSmallScreen` = **1.2**（不是 1.0 —— 逐扇实读）");
            CheckTrue(rre.GetComponent<TransformScalerBySmallScreenUI>() == null,
                      "★（反例·层级对照）这一扇窗根上**没有**烤 `TransformScalerBySmallScreenUI`"
                      + "（「窗口根上带成品的 3 扇」不含它 —— 对照组 = `GenericOptionsPanel` 那条）");
            Check(KidNames(rre.transform), "Menu Dark Background|BackgroundHit|window",
                  "★ 根的直接子件 = 3 件（压暗层 / 命中区 / `window`）");
            //  ⚠️ **2026-10-14 订正（期望串）**：`Title` / `Description` / `Timer` 三件在**这一档（no-data）**
            //     是**出厂关着**的（判据 = 紧随其后的三条「XX 关着」断言，本 run 全绿）
            //     ⇒ `KidNames` 会给它们加 `*` 前缀（`:459-472` 的约定）。原串漏了三个 `*`，同一节里自相矛盾。
            Check(KidNames(rre.WindowNode),
                  "Generic Window Red Background Big|Generic Close Button Orange|*Title|*Description|*Timer|Bonus points|Scroll View",
                  "★ `window` 的直系子件（7 件 · 兄弟序照 `m_Children`；`*` = no-data 那一档出厂关着）");
            CheckHasKids(rre.ContentNode, "★ `Content` 下出厂**一个子件都没有**（阵营卡是运行期实例化的子预制体）");
            // no-data：**走的就是原版自己的兜底分支**（`Title`/`Description` 空 ⇒ 关；`Timer` 的 DurationHours == 0 ⇒ 关）
            Check(rre.HasData, false, "★ 出厂那一档：`HasData = false`（LiveOps 配置在远端 CCD）");
            CheckTrue(rre.TitleLabel != null && rre.TitleLabel.gameObject.activeSelf == false,
                      "★ no-data：`Title` **关着**（原版那一支：取到的串为空 ⇒ `enabled = false`）");
            CheckTrue(rre.DescLabel != null && rre.DescLabel.gameObject.activeSelf == false,
                      "★ no-data：`Description` **关着**（同上）");
            CheckTrue(rre.TimerNode != null && rre.TimerNode.gameObject.activeSelf == false,
                      "★ no-data：`Timer` **整件关着**（原版 `DurationHours == 0` 那一支）");
            CheckTrue(rre.BonusLabel != null && rre.BonusLabel.gameObject.activeSelf,
                      "★（反例）`Bonus points text` **开着**但**空串**（`string.Format(空模板, x)` 就是空串）"
                      + " ⇒ 上面三条不是「整棵树没建」蒙出来的");
            Check(rre.BonusLabel != null ? rre.BonusLabel.Text : "-", "",
                  "★ no-data：`Bonus points text` 是**空串**");
            // 几何（冻结字面量 · 本扇根是拉伸根 ⇒ 绝对框 == 相对框）
            CheckAt(rre.WindowNode, 395.72f, 1524.28f, 188.35f, 851.65f, "★ `window` 的框");
            CheckAt(FindChild(rre.transform, "Title"), 432.90f, 1487.10f, 216.35f, 268.35f, "★ `Title` 的框");
            CheckAt(FindChild(rre.transform, "Bonus points"), 432.90f, 1487.10f, 288.25f, 366.74f, "★ `Bonus points` 的框");
            CheckAt(FindChild(rre.transform, "Timer"), 806.77f, 1113.23f, 772.30f, 851.65f, "★ `Timer` 的框");
            CheckAt(rre.ContentNode, 960.00f, 960.00f, 470.39f, 763.61f, "★ `Content`（0 张卡 ⇒ 零宽、中心在 960）");
            // 两个纯函数的判别式（期望值是手算字面量）
            Check(RankedRewardEventWindow.FillBonus("+{0} Classic points", 20), "+20 Classic points",
                  "★ `pointsBonus` 那一跳是 **`string.Format`**（词条带 `{0}` 占位）");
            Check(RankedRewardEventWindow.FillBonus("", 20), "", "★ 空模板 ⇒ 空串（= `string.Format(\"\", x)`）");
            // 缺图（⚠️ **2026-10-14 订正**：原来写「`40k_UI_Banner BW` 没进 `Resources/`」—— 那张图已于
            //   2026-10-06 12:31 拷进 `Resources/Art/ui_menu/40k_UI_Banner_BW.png` ⇒ 期望是 **0**。
            //   判据 = ① 图在盘上 ② 本 run 里同一条口（`Shell/RankedRewardEventWindow.cs:558-560`）
            //   **确有一条**「图取不到」告警，但名字是 `40K_shop_offer_bg_Sororitas_0`（`:46627`，来自 `SetBoost`）
            //   —— 口是活的、**没对横幅报缺** ⇒ 横幅**取到了**。
            //   `Tex(ArtBanner, …)`（`Shell/RankedRewardEventWindow.cs:378`）在 `Build()` 路径上**无条件**调
            //   ⇒ 这个 `0` 是**实读数**。⚠️ 原来紧随其后那两行「缺的是哪个名字」**已随之删掉**
            //   （`Count == 0` 时三元取 `"-"`、与任何图名恒不等）。
            //   🔴 那 13 张手拷图**没有任何导入器登记**（= A808）⇒ 谁跑一次 `import_original_art.py` 就翻红。
            //   🔴 **⛔ 别顺手把下面 `SetBoost` 那一段的缺图也改成 0**：`40K_shop_offer_bg_Sororitas_0`
            //   （阵营卡的底）是**真缺**（`Resources/` 下一张都没有，`Shell/RankedRewardEventWindow.cs:168-170`）。）
            // 🆕 2026-10-15（A810①）：走公共口（「0 才绿」的唯一一份）；上面那整段订正留档不动
            CheckNoMissingArt(rre.MissingArt,
                "★ 排位奖励活动窗（`40k_UI_Banner BW` 已进 `Resources/Art/ui_menu/`）");
            // 两态：喂一份数据 ⇒ 三栏开出来、卡片按 `AffectedArmies` 建
            rre.SetBoost(new RankedRewardEventWindow.BoostView
            {
                Armies = new[] { 10, 20 },
                ArmyNames = new[] { "Ultramarines", "Goff" },
                Label0 = "FEATURE FACTIONS",
                Label1 = "for every Ranked victory gained with a featured faction.",
                Label2 = "+{0} Classic points",
                PointsBonus = 20,
                DurationHours = 24,
                EndTime = Time.realtimeSinceStartup + 3600f * 23f + 60f * 34f,
            });
            Check(rre.HasData, true, "（两态）`SetBoost` 之后 `HasData = true`");
            CheckTrue(rre.TitleLabel != null && rre.TitleLabel.gameObject.activeSelf, "★ 两态：有 `Label0` ⇒ `Title` **开**");
            CheckTrue(rre.DescLabel != null && rre.DescLabel.gameObject.activeSelf, "★ 两态：有 `Label1` ⇒ `Description` **开**");
            CheckTrue(rre.TimerNode != null && rre.TimerNode.gameObject.activeSelf, "★ 两态：`DurationHours = 24` ⇒ `Timer` **开**");
            Check(rre.BonusLabel != null ? rre.BonusLabel.Text : "-", "+20 Classic points",
                  "★ 两态：`Bonus points text` = `Format(Label2, PointsBonus)`");
            Check(rre.Cards_.Count, 2, "★ 两态：`AffectedArmies` 两条 ⇒ **2 张**阵营卡");
            CheckTrue(rre.TimerTextLabel != null, "★ 两态：`Timer/Timer Text` 建出来了");
            rre.Close();

            // 四扇全部收干净（本族都是弹窗/全屏窗，留着会压在后面的实拍上）
            Debug.Log(P + "  （§A251-L2/L4：四扇都验过 `Open*` → 复用一个实例 → 关过再开新建 ⇒ 已全部 `Close()`）");
        }

        // ============================================================ §A251-L3 `Referral Popup`（2026-10-13 新增）
        //  🔴 **它不在 `menus` 包里** —— 原版 prefab 在 `bundle_generalgamewindows_assets_all`（A251 那七扇里
        //     只有它与已裁「不建」的 `Debug Reactivate Event Window` 在这个包）。
        //      python 工具/menu_dump.py bundle_generalgamewindows_assets_all "Referral Popup" --depth 12 --relative --md
        //  对账脚本 `_tmp_view/wl3/check_table.py`：**78 项 0 不符**（表 ↔ 现读逐格比；两条白名单走原读，
        //  理由见 `ReferralPopupWindow.cs` 文件头「两处我们算的」②）。
        //  🔴 **期望值一律是【冻结的原版字面量】**（下面每一个数都能在上面那条命令的输出里逐字找到）
        //     —— ⛔ **不读** `ReferralPopupWindow.Recon` / 那几个 `…R` 常量
        //     （那是**自证**：改坏常量时两处一起变、断言照样绿）。
        Section("§A251-L3 `Referral Popup`（窗口参数 / 78 节点层级 / 出厂显隐 / 几何 / no-data / 两态 / 交互）");
        {
            var rp = WindowsManager.OpenReferralPopup();
            // ---------------- 窗口参数（MB 逐字段实读）+ 前提 ----------------
            Check(WindowsManager.PrefabRefReferralPopup, "Referral Popup", "★ 注册键 = prefab 根名");
            Check(rp.name, "Referral Popup", "★ 根节点名 = prefab 名（`WindowsManager` 复用的键同源）");
            Check(rp.type, WindowType.Popup, "`type` = 1 Popup（MB 原文）");
            Check(rp.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup");
            Check(rp.closeOnEsc, true, "`closeOnESC` = 1（MB 原文）");
            // 🔴 **判别力最强的一条**：同批五扇里**四扇是 1.0**（= 「不覆盖」），只有这一扇是 1.35
            //    ⇒ 拿 1.0 去断这里**必红**（同族的 `GenericOptionsPanel` 那条断言是反向的对照）。
            CheckNear(rp.extraScaleSmallScreen, 1.35f, 1e-4f,
                      "★ `extraScaleSmallScreen` = **1.35**（MB 原文 —— 同批里唯一一扇不是 1.0 的）");
            //  ⚠️ 这一条**两档都要成立**（不能写成 `GetComponent == null`）：`SmallScreenUI.Enabled` 由
            //     `PlayerPrefs("SmallScreenUI")` 决定，开着时基类 `ApplySmallScreenScale()` 会**主动加**一颗
            //     （判据 → `Shell/WindowsManager.cs` 的 `GameWindow.ApplySmallScreenScale`）⇒ 「根上有没有」那一问**不是**这一件的前置。
            //     真正的判据是「**烤着的**那一颗不存在」：原版这扇窗前面只挂了 `ReferralPopupWindow` 一颗组件。
            var rpSc = rp.GetComponent<TransformScalerBySmallScreenUI>();
            CheckTrue(rpSc == null || Mathf.Abs(rpSc.menuScale - 1.35f) < 1e-3f,
                      "★ 根上**没有烤着** `TransformScalerBySmallScreenUI`（原版这一件只挂一颗窗口组件；"
                      + "`SmallScreenUI.Enabled` 开着时基类按 `extraScaleSmallScreen` 主动加的那一颗，"
                      + "倍数也必须是 **1.35**）");
            // ---------------- 层级（**直接子件**，⛔ `FindChild` 走整棵子树 ⇒ 那样写恒真）----------------
            Check(KidNames(rp.transform), "Menu Dark Background|BackgroundHit|AbsorbHit|window",
                  "★ 根的直接子件（原版 2 件 + 我们的 `BackgroundHit`/`AbsorbHit` = 4；兄弟序照 `m_Children`）");
            Check(KidNames(rp.WindowNode), "Generic Window Red Background Big|Generic Close Button Orange|content",
                  "★ `window` 的直系子件（**3 件** · 兄弟序照 `m_Children`）");
            //  ⚠️ **2026-10-14 订正（期望串）**：`Referred View` 与 `counter` 两件**出厂关着**
            //     （判据 = 下面那两条「`Referred View` / `counter` 关着（prefab `act = F`）」断言，本 run 全绿）
            //     ⇒ `KidNames` 会给它们加 `*` 前缀（`:459-472` 的约定）。原串漏了两个 `*`。
            Check(KidNames(FindChild(rp.transform, "content")),
                  "Referral Title|Input View|*Referred View|Divisor line members|spacing (1)|Title|Descripton|counter number|*counter",
                  "★ `content` 的直系子件（**9 件** · 兄弟序照 `m_Children` —— `counter` 在最末；`*` = 出厂关着）");
            Check(KidNames(rp.InputViewNode), "Label|input|spacing|Generic Simplified UI Button|error",
                  "★ `Input View` 的直系子件（**5 件**）");
            CheckHasKids(FindChild(rp.transform, "Text Area"), "Placeholder", "Text",
                         "★ `input/Text Area` 的直系子件 = 2 件（原版那颗 `RectMask2D` 就在这一件上）");
            CheckHasKids(FindChild(rp.transform, "Generic Close Button Orange"), "Background", "Icon", "Hit",
                         "★ 关窗钮的直系子件（`Background`/`Icon` 是**子件**、⛔ 不是兄弟；`Hit` 是我们的命中区）");
            CheckHasKids(FindChild(rp.transform, "Generic Simplified UI Button"), "Image", "Button Text", "Hit",
                         "★ 确认钮的直系子件 = 3 件");
            // 78 个节点里 **50 颗**是 `counter` 下的 `marker`（原版 `referralCounterSteps` 数组长 = 50）
            Check(rp.CounterNode != null ? rp.CounterNode.childCount : -1, 50,
                  "★ `counter` 的直系子件 = **50** 颗 `marker`（= 原版 `referralCounterSteps.Length`，现读 50）");
            int badName = 0, badKids = 0, outlineQuads = 0;
            if (rp.CounterNode != null)
                for (int i = 0; i < rp.CounterNode.childCount; i++)
                {
                    var mk = rp.CounterNode.GetChild(i);
                    if (mk.name != "marker") badName++;
                    if (mk.childCount != 4) badKids++;
                    var qs = rp.MarkerOutlineQuads(i);
                    if (qs != null) outlineQuads += qs.Length;
                }
            Check(badName, 0, "★ 50 颗都叫 `marker`（逐颗核过）");
            Check(badKids, 0, "★ 每颗 `marker` 下面**恰好 4 份** `Outline` 副本（uGUI `Outline` = 四个斜角各一份）");
            Check(outlineQuads, 200, "★ `Outline` 副本合计 **200** 份（50 × 4 —— 本壳没有 uGUI 的 `Outline`，逐份建成 quad）");
            // ---------------- 出厂显隐（prefab 序列化 + 原版 `Refresh()` 的 no-data 分支）----------------
            CheckTrue(rp.InputViewNode != null && rp.InputViewNode.gameObject.activeSelf,
                      "★ 出厂/no-data：`Input View` **开着**（原版 `Refresh()`：`GetReferrer() == null` ⇒ `SetActive(true)`）");
            CheckTrue(rp.ReferredViewNode != null && rp.ReferredViewNode.gameObject.activeSelf == false,
                      "★ 出厂：`Referred View` **关着**（prefab `act = F`）");
            CheckTrue(rp.CounterNode != null && rp.CounterNode.gameObject.activeSelf == false,
                      "★ **`counter` 关着**（prefab `act = F` —— 且原版运行期**也没有任何代码开它**："
                      + "十个序列化字段没一个是它、`Refresh()` 只 `SetActive` 了另外两扇、全包按 pid 搜零引用）");
            CheckTrue(FindChild(rp.transform, "Title") != null
                      && FindChild(rp.transform, "Title").gameObject.activeSelf,
                      "★（反例）`Title` **开着** ⇒ 上面三条不是「整棵树没建」蒙出来的");
            Check(rp.HasData, false, "★ 出厂那一档：`HasData = false`（`ReferralManager` 在服务端，本地一条都没有）");
            Check(rp.ReferButtonEnabled, true, "★ 出厂：`referButton.interactable = !has` = **true**（原版 `Refresh()` ③）");
            CheckTrue(FindChild(FindChild(rp.transform, "Generic Simplified UI Button"), "Hit") != null
                      && FindChild(FindChild(rp.transform, "Generic Simplified UI Button"), "Hit").gameObject.activeSelf,
                      "★ 出厂：确认钮的命中区**开着**（`interactable = true` 那一档 —— 与下面「两态」那条配成一对）");
            Check(rp.ErrorText, "AN ERROR HAS OCURRED",
                  "★ 出厂：`error` = **prefab 原文**（原版 `Refresh()` 才把它清成空串 —— 见下面「两态」）");
            Check(rp.CounterText, "You have collected {0} referral rewards!",
                  "★ `counter number` = prefab 原文（= I2 词条 `MenuShop/referral/counter` 印在资产里的那一串）");
            Check(rp.InputTextShown, "\u200B", "★ `Text Area/Text` 出厂内容 = **U+200B 零宽空格**（画出来是空的）");
            // ---------------- 几何（冻结字面量 · 绝对框；本扇根是拉伸根 ⇒ 相对框 == 绝对框）----------------
            CheckAt(rp.WindowNode, 364.03f, 1555.97f, 287.11f, 900.68f, "★ `window` 的框");
            CheckAt(FindChild(rp.transform, "Generic Window Red Background Big"),
                    503.93f, 1434.17f, 287.11f, 900.68f, "★ 窗体底（比 `window` 窄 261.71，竖直同高）");
            CheckAt(FindChild(rp.transform, "Generic Close Button Orange"),
                    1361.78f, 1436.17f, 287.11f, 362.72f, "★ 关窗钮（贴 `window` 右上角）");
            CheckAt(FindChild(rp.transform, "content"), 526.04f, 1397.11f, 327.79f, 839.83f, "★ `content`");
            CheckAt(FindChild(rp.transform, "Referral Title"),
                    526.04f, 1397.11f, 327.79f, 380.93f, "★ `Referral Title`（content 的第一格：顶沿 == content 顶沿）");
            CheckAt(FindChild(rp.transform, "Input View"),
                    526.04f, 1397.11f, 385.93f, 560.93f,
                    "★ `Input View`（顶 = content 顶 + 53.1405 + 间距 5 = **385.93** —— 这一条就是"
                    + "「`content` 的 VLG 只排**活着**的孩子」的判别式）");
            CheckAt(FindChild(rp.transform, "input"), 560.37f, 1362.79f, 438.16f, 483.16f, "★ `input`（九宫底）");
            CheckAt(FindChild(rp.transform, "Text Area"), 570.37f, 1352.79f, 445.16f, 477.16f, "★ `input/Text Area`");
            CheckAt(FindChild(rp.transform, "Generic Simplified UI Button"),
                    871.53f, 1051.62f, 496.13f, 544.86f, "★ 确认钮");
            CheckAt(FindChild(rp.transform, "Button Text"), 877.74f, 1045.59f, 498.64f, 542.37f,
                    "★ `Button Text`（`AspectRatioFitter` 宽控高跑完那一档：高 **43.73** > 锚点框的 39.18）");
            CheckAt(FindChild(rp.transform, "error"), 757.27f, 1165.89f, 544.86f, 569.07f, "★ `error`");
            CheckAt(FindChild(rp.transform, "Divisor line members"), 526.04f, 1397.11f, 565.93f, 569.70f,
                    "★ 分隔条（正落在 `Input View` 下沿 + 间距 5）");
            CheckAt(FindChild(rp.transform, "Title"), 526.04f, 1397.11f, 592.67f, 644.67f, "★ `Title`");
            CheckAt(FindChild(rp.transform, "Descripton"), 526.04f, 1397.11f, 649.67f, 789.67f, "★ `Descripton`");
            CheckAt(FindChild(rp.transform, "counter number"), 526.04f, 1397.11f, 794.67f, 820.17f, "★ `counter number`");
            CheckAt(rp.CounterNode, 532.08f, 1391.08f, 481.45f, 543.64f,
                    "★ `counter`（**关着** ⇒ 位置 = prefab 序列化值；⛔ 别按「排在 `counter number` 下面」推）");
            CheckAt(rp.ReferredViewNode, 526.04f, 1397.11f, 588.43f, 763.43f, "★ `Referred View`（同上：序列化值）");
            // 50 颗 `marker` 的网格（**首尾两颗**；`MarkerRect` 是一条算式 ⇒ 首尾对了、中间那 48 颗
            // 由对账脚本逐格核过 —— 见报告 §五）
            CheckAt(rp.CounterNode != null ? rp.CounterNode.GetChild(0) : null,
                    535.67f, 545.67f, 507.54f, 517.54f, "★ `marker` #0 的框（`counter` 左沿 + 8.59 − 5）");
            CheckAt(rp.CounterNode != null ? rp.CounterNode.GetChild(49) : null,
                    1377.49f, 1387.49f, 507.54f, 517.54f,
                    "★ `marker` #49 的框（步长 **17.18** × 49 —— 这一条把「网格步长」钉死）");
            // `Referred View` 两个孩子：**prefab 序列化值**（那颗 HLG 关着 ⇒ 布局不跑；`menu_dump` 在那一块量不出宽）
            CheckAt(FindChild(rp.ReferredViewNode, "Label"), 770.87f, 982.01f, 647.08f, 719.78f,
                    "★ `Referred View/Label`（序列化框 211.14 宽 —— ⛔ 不是 `menu_dump` 那格的 0.00）");
            CheckAt(FindChild(rp.ReferredViewNode, "Name"), 996.93f, 1152.28f, 647.08f, 719.78f,
                    "★ `Referred View/Name`（序列化框 155.35 宽）");
            // ---------------- 压暗层那条不变量（唯一定义处 = `MenuDraw.CheckShadeRule`）----------------
            MenuDraw.CheckShadeRule(CheckTrue, "推荐人窗", FindChild(rp.transform, "BackgroundHit"),
                                    FindChild(rp.transform, "Menu Dark Background"), ReferralPopupWindow.QHit);
            // ---------------- 两态：喂一份「已经有推荐人」的数据 ⇒ 原版 `Refresh()` 的七跳逐个落位 ----------------
            rp.SetReferral(new ReferralPopupWindow.ReferralView
            { HasReferrer = true, ReferrerName = "Tester", RewardCount = 3, MaxRewards = 50, InputText = "" });
            Check(rp.HasData, true, "（两态）`SetReferral` 之后 `HasData = true`");
            CheckTrue(rp.ReferredViewNode.gameObject.activeSelf,
                      "★ 两态：`has` ⇒ `Referred View` **开**（原版 `SetActive(bVar8)`）");
            CheckTrue(rp.InputViewNode.gameObject.activeSelf == false,
                      "★ 两态：`has` ⇒ `Input View` **关** —— 与上一条**互斥**（同一句的两个分支）");
            Check(rp.ReferrerNameText, "Tester", "★ 两态：`Referred View/Name` = `referrer.Name`（原版 `+0x18`）");
            Check(rp.ReferButtonEnabled, false, "★ 两态：`has` ⇒ `referButton.interactable = !has` = **false**");
            var bHit = FindChild(FindChild(rp.transform, "Generic Simplified UI Button"), "Hit");
            CheckTrue(bHit != null && bHit.gameObject.activeSelf == false,
                      "★ 两态：`interactable = false` 那一档**命中区也关掉**（我们这套没有 uGUI 的 `interactable`"
                      + " ⇒ 用「点不动」表达；与出厂那条配成一对）");
            CheckTrue(rp.CounterNode.gameObject.activeSelf == false,
                      "★（灭自证）**有数据也不开 `counter`** —— 原版 `Refresh()` 里根本没有它（逐句核过），"
                      + "所以「50 颗进度点」在原版成品里**永远不画**");
            Check(rp.CounterText, "You have collected 3 referral rewards!",
                  "★ 两态：`count < max` ⇒ `string.Format(词条, 3)`（原版 ⑤ 那一支）");
            Check(rp.ErrorText, "", "★ 两态：`Refresh()` 尾段把 `error` **清成空串**（字面量实读 = `\"\"`）");
            // ③ 逐颗 `marker` 的描边色（`i < count` ⇒ `achievedColor`；否则 `defaultColor`）—— 三个点必成一对判别式
            var oq0 = rp.MarkerOutlineQuads(0);
            var oq2 = rp.MarkerOutlineQuads(2);
            var oq3 = rp.MarkerOutlineQuads(3);
            var oq49 = rp.MarkerOutlineQuads(49);
            CheckTrue(ColNear(oq0, 0.29293f, 0.83962f, 0.19406f),
                      "★ 两态：`marker` #0 的 `Outline` = **`achievedColor`**（MB 实读 0.29293 / 0.83962 / 0.19406）");
            CheckTrue(ColNear(oq2, 0.29293f, 0.83962f, 0.19406f),
                      "★ 两态：`marker` #2（= `count−1`）仍是 `achievedColor`（**边界那一颗**）");
            CheckTrue(ColNear(oq3, 0.91765f, 0.76863f, 0.48235f),
                      "★ 两态：`marker` #3（= `count`）翻成 **`defaultColor`**（MB 实读 0.91765 / 0.76863 / 0.48235）"
                      + " —— #2 与 #3 这一对就是「`i < count`」的判别式");
            CheckTrue(ColNear(oq49, 0.91765f, 0.76863f, 0.48235f), "★ 两态：`marker` #49 仍是 `defaultColor`");
            // ---------------- 交互：确认钮那两跳（原版 `OnSetReferrer()`）----------------
            //  ⚠️ 挑**这一刻**做这条是有意的：上面刚断言过 `interactable = false`（`has == true`）⇒
            //     `SubmitReferrer()` 那两跳是一次**真的状态转移**（false → true），不是同义反复。
            Check(rp.ReferButtonEnabled, false, "（前提）按下之前 `referButton` 是**不可用**那一档");
            rp.SubmitReferrer();
            Check(rp.ReferButtonEnabled, true,
                  "★ 交互：`SubmitReferrer()` 之后按钮**恢复可用**（原版失败回调 `<OnSetReferrer>b__13_0` 那一步 ——"
                  + "本地没有 `ReferralManager`，当场出声、⛔ 不假装提交成功）");
            // ---------------- 第三档：**`ReferralManager` 活着、但 `GetReferrer()` 是 null** ----------------
            //  🔴 这一档才是「有服务端、只是我还没填推荐人」的原版真实状态，而且它是**唯一**会写
            //     `counter number` 与清 `error` 的档（`Refresh()` 里那两跳在 `if (bVar8)` **之外**、
            //      但在 `if (referButton != null)` **之内** —— 逐句读出来的块结构）
            rp.SetReferral(new ReferralPopupWindow.ReferralView
            { HasReferrer = false, ReferrerName = "", RewardCount = 0, MaxRewards = 50, InputText = "who" });
            CheckTrue(rp.InputViewNode.gameObject.activeSelf && rp.ReferredViewNode.gameObject.activeSelf == false,
                      "★ 第三档：没有推荐人 ⇒ `Input View` 开 / `Referred View` 关（与「有推荐人」那档**互斥**）");
            Check(rp.ReferButtonEnabled, true, "★ 第三档：`interactable = !has` = **true**（可以填）");
            Check(rp.CounterText, "You have collected 0 referral rewards!",
                  "★ 第三档：**管理活着就写计数串**（0 也写 —— 与原版那一段的块结构一致）");
            Check(rp.ErrorText, "", "★ 第三档：`error` 被清成空串（原版 `Refresh()` 尾段）");
            CheckTrue(ColNear(rp.MarkerOutlineQuads(0), 0.91765f, 0.76863f, 0.48235f),
                      "★ 第三档：`count = 0` ⇒ **连 #0 都是 `defaultColor`**（与前面「两态」那档配成一对）");
            Check(rp.InputTextShown, "who",
                  "★ 第三档：输入框的显示 = `ReferralView.InputText`（原版那一格装的是**用户敲进去的**那一串；"
                  + "这一条顺带把 `ShowTyped` + `MenuDraw.ClipText` 那条路在批处理里带一次电）");
            // ---------------- 缺图 / 换图（**缺了必须出声、且这里一条都不缺**）----------------
            // 🆕 2026-10-15（A810①）：两条池子各走自己的公共口（「0 才绿」各只有一份）
            CheckNoMissingArt(rp.MissingArt, "★ 推荐人窗（9 张全在 `Resources/Art/` 里）");
            CheckNoMissingSwapArt("推荐人窗");
            //  ⛔ **2026-10-14 删掉一条断言**（原来这里两行：`CheckTrue(WindowButton.MissingPressedArt.Count == 0, …)`）：
            //    `WindowButton.MissingPressedArt` 那张表的注释自己写着「**这份表只出声、不当缺点断**」
            //    （`Shell/PromptPopup.cs:589-591`）—— 原版 1276 颗带 `m_SpriteState` 的 `Selectable` 逐颗核过：
            //    **悬停图空 ⇔ 按下图空，0 处不一致**，且我们取不到按下图时**退回高亮图**
            //    （`Press()` 里那个 `??`）⇒ 这一档**多数是合法的**。本 run 表里那 1 条是 `" → "`
            //    （`Shell/InboxWindow.cs` 当时把常态图名传成 `null`）—— 属于注释里说的「多数是合法的」那一档。
            //    🔴 **2026-10-15（A810③）就地订正（铁律 5）**：那一条**已经修了**（该处现在传它自己的常态
            //    图名 `UI_Button_Round_background`）⇒ 今天这张表里的条目**都认得出是谁**（不再是两个空格的 `" → "`）。
            //    同一条信息**本文件「实拍」那一段已经打进日志了**（`Debug.Log(P + "   [按下图] 取不到的是 **N 条**"`，
            //    ⛔ 别按行号找 —— 那是本文件里唯一一处 `[按下图]` 日志）⇒ 这条断言是**重复**。
            //    真要保留闸，得先有「我们这一颗 → 原版哪一颗」的映射（= A15 那笔账），⛔ 不在本批。
            CheckHoverSwap(rp.transform, "推荐人窗");
            CheckPressedSwap(rp.transform, "推荐人窗");
            // ---------------- 复用 / 关过再开（照原版 `automaticallyLoadedWindows`）----------------
            var rp2 = WindowsManager.OpenReferralPopup();
            CheckTrue(ReferenceEquals(rp2, rp), "★ 同键再开 ⇒ **同一实例**（照原版 `automaticallyLoadedWindows` 命中复用）");
            rp.Close();
            var rp3 = WindowsManager.OpenReferralPopup();
            CheckTrue(!ReferenceEquals(rp3, rp), "★ **关过再开 ⇒ 新建一扇**（复用只在「还开着」时成立）");
            // 新建那一扇必须回到**出厂那一档**（⛔ 别把上一扇喂的数据带过去）
            Check(rp3.HasData, false, "★ 关过再开 ⇒ 新建的那一扇 `HasData = false`（出厂态）");
            CheckTrue(rp3.InputViewNode.gameObject.activeSelf && rp3.ReferredViewNode.gameObject.activeSelf == false,
                      "★ 关过再开 ⇒ 新建的那一扇回到 `Input View` 开 / `Referred View` 关");
            rp3.Close();

            // 🆕 **2026-10-15（A796）**：压暗层「点了会不会关」走公共口（判据 → `:2204` 那一段）。
            //   ⚠️ 本窗**是**「只配了几何断言」那一档的一扇（`MenuDraw.CheckShadeRule` 一条 +
            //      **没有** `CheckAbsorbRule`、也没有别的窗那种 ad-hoc 点击）⇒ 本口是本窗**唯一**的
            //      「点了压暗层 ⇒ 关窗」站立点；把 `ShadeHit(…, () => Close())` 换成空动作/吸收层，
            //      在加本块之前**一条断言都不会红**。
            //   🔴 本口**会把窗关掉** ⇒ 排在收尾之后；本块走的是上面刚用过两次的入口
            //      `WindowsManager.OpenReferralPopup()`（`rp` / `rp3` 都是它开的）。
            var rpA = WindowsManager.OpenReferralPopup();
            CheckTrue(rpA != null && rpA.CurrentState == WindowState.Open,
                      "（A796 现场）再开一扇推荐人窗 —— 下面那条要在**开着**的窗上点");
            if (rpA != null)
                MenuDraw.CheckShadeClickRule(CheckTrue, "推荐人窗", rpA.transform,
                                             FindChild(rpA.transform, "BackgroundHit"), () => rpA.CurrentState);
            Debug.Log(P + "  （§A251-L3：`Referral Popup` 78 节点验完 ⇒ 已 `Close()`；`Name`/`Title` 那几行字是"
                        + " **prefab 出厂原文**（俄文），原版运行期过 I2 词条、词条表在远端 CCD）");
        }

        // ======== 🆕 2026-10-13（A435 阶段 2 · 乙）：裁切状态迁到【视口节点】上（A26）+ A465 ========
        //   契约 → `Shell/ViewportClip.cs` 文件头；逐站点对照 → `资料/普查产出_1013/A435_迁移表.md`。
        //   两个变异源分开下毒（谁红就知道是哪一档坏）：
        //     ① 挪节点的 `localPosition` ⇒ 只动「框在哪」，`MenuScroll.Viewport` 一个字节没动（A465 的灭自证条）；
        //     ② 收小节点的 `sizeDelta` ⇒ 只动「框多大」，看**渲出来的真几何**（`ImageQuad.WorldW/H`）。
        //   ⛔ 全段一条计数器都不读（`NodeResolutions` / `NodeShadowedByParam` 都不碰）。
        //   ⚠️ 本段**自己开一扇商店窗**：上面「商品条目族实拍」那一段已经把 `win` 关掉了（`win.Close()`）。
        Section("A435 阶段 2 · 乙：裁切迁到视口节点上（A26）+ A465（构建循环吃节点）");
        {
            var mgr435 = WindowsManager.EnsureHost();     // = 本场景那一台（`Build()` 里建的；`Instance` 已登记）
            var shop435 = ShopWindow.Create(mgr435);
            mgr435.OpenWindow(shop435);
            shop435.tabButtons.Click(0);                       // 第 1 页（`Card Shop Tab`）
            var pg435 = shop435.PageOf(0);
            CheckTrue(pg435 != null, "（前提）商店第 1 页（`Card Shop Tab`）在");
            var vp435 = pg435 != null ? pg435.transform.Find("Packs Scroll View/Viewport") : null;
            CheckTrue(vp435 != null, "（前提）`Packs Scroll View/Viewport` 在");
            var vc435 = vp435 != null ? vp435.GetComponent<ViewportClip>() : null;
            CheckTrue(vc435 != null,
                      "★ A26：这颗 `Viewport` 上挂着 `ViewportClip`（= 原版那个 `RectMask2D`）——"
                    + " 迁移前这里是**裸节点** + `BuildGrid` 里那对「把本窗 `Clip` 设成货架视口、"
                    + " `ClipSoftness` 设成 `PacksSoft`」→ 画 → 两件原样放回」（**那四行已整对删掉**）"
                    + "。**改坏法**：把那句 `AddComponent<ViewportClip>()` 拿掉 ⇒ 本条红");

            if (vc435 != null && vp435 != null && pg435 != null)
            {
                // ---- ① 两个字段 = 原版实读值（逐页出处 → `Shell/ShopWindow.cs` 的 `PacksSoft` 那段注释）----
                Check(vc435.padding, Vector4.zero,
                      "★ A26：节点 `padding` = (0,0,0,0)（原版三页的 `m_Padding` 实读）");
                Check(vc435.softness.x, 0,
                      "★ A26：节点 `softness.x` = 0（**硬边** = 原版那个 `m_Softness = (0,25)` 的 x 分量）");
                Check(vc435.softness.y, 25,
                      "★ A26：节点 `softness.y` = **25**（纵向 25px 渐隐带）—— **改坏法**：把那句"
                    + " `_gridVp.softness = ...` 改掉/删掉 ⇒ 本条红，且下面「格底被裁到 94 高」那条也会红");
                // 框 = 原版视口字面量（⛔ 不回读实现传进去的那一份：`ScrollView` 是**入参来源**、不是判据）
                var box435 = vc435.ClipPx;
                CheckTrue(box435.HasValue, "（前提）这颗节点给得出框（`RectTransform` 且 `sizeDelta` 已写）");
                if (box435.HasValue)
                {
                    CheckNear(box435.Value.x1, 329.76f, 0.5f, "★ A26：节点框**左沿** = 原版 329.76");
                    CheckNear(box435.Value.y1, 127.62f, 0.5f, "★ A26：……**上沿** 127.62");
                    CheckNear(box435.Value.x2, 1920.00f, 0.5f, "★ A26：……**右沿** 1920.00");
                    CheckNear(box435.Value.y2, 1080.00f, 0.5f, "★ A26：……**下沿** 1080.00");
                }

                // ---- ② A465 的**灭自证**条：框的来源是【节点】，不是 `MenuScroll.Viewport` ----
                var sc435 = shop435.GridScrollOf(0);
                CheckTrue(sc435 != null, "（前提）第 1 页的栅格滚动区在（`GridScrollOf(0)`）");
                if (sc435 != null)
                {
                    var inVp435 = new PxRect(500f, 400f, 700f, 500f);       // 原视口里的一小块
                    CheckTrue(sc435.Intersects(inVp435), "（前提）这一小块**落在原视口里** ⇒ 正常态判「可见」");
                    var keepPos435 = vp435.localPosition;
                    vp435.localPosition = keepPos435 + new Vector3(0f, 200f, 0f);   // 往上挪 200 世界单位 ≈ 21600px
                    CheckTrue(!sc435.Intersects(inVp435),
                              "★ A465：把**节点**搬走 21600px 之后，落在 `MenuScroll.Viewport` 里那一块"
                            + "**必须判不可见** —— 这条钉的是「`Intersects` 的框来自**节点**」。"
                            + "**改坏法**：把 `MenuScroll.Intersects` 写回 `MenuDraw.Visible(onScreen, Viewport)`"
                            + "（2026-10-13 之前的老写法，也是块 4 建议的那个**空操作**版本）⇒ 立刻红");
                    vp435.localPosition = keepPos435;
                    CheckTrue(sc435.Intersects(inVp435),
                              "（还原）把节点放回去 ⇒ 又判可见 —— 这一条同时钉住「上一条不是因为别的原因红的」");
                }

                // ---- ③ 节点态真的驱动【裁切】：收小节点 ⇒ 重建 ⇒ **渲出来的真几何**跟着变 ----
                //   判据独立算：正常态格底 = 格 ±1 再按**原版视口**裁；下毒后按**我选的 100px 带子**裁。
                var content435 = vp435.Find("Content");
                CheckTrue(content435 != null, "（前提）`Packs Scroll View/Viewport/Content` 在");
                if (content435 != null)
                {
                    var offers435 = ShopData.Offers(0);
                    CheckByPrefix(content435, "CatalogItemShopContainer_", offers435.Length,
                                  "（前提）正常态：格数 = 商品数");
                    var b0 = FindChild(FindChild(content435, "CatalogItemShopContainer_0"), "background");
                    CheckRectPxUnion(b0, 329.76f, 666.36f, 133.62f, 610.62f,
                                     "（前提）正常态第 1 格的格底（格 +1 四周、顶沿贴 134.62 ⇒ 被视口裁到 133.62）");

                    // 下毒：把**节点框**收成「视口顶端 100px 的一条带」（`MenuScroll.Viewport` 一个字节没动）
                    MenuDraw.ApplyPxRect(vp435, vp435.parent,
                                         new PxRect(329.76f, 127.62f, 1920f, 127.62f + 100f));
                    if (sc435 != null) sc435.OnChanged();          // 走生产那条重建路（滚轮/拖拽也指它）
                    CheckByPrefix(content435, "CatalogItemShopContainer_",
                                  Mathf.Min(ShopTabPage.GridCols, offers435.Length),
                                  "★ A435·乙（A26）：把节点框收成顶端 100px 之后**只剩第 1 行**被建"
                                + $"（正常态 {offers435.Length} 格）—— **改坏法**：把 `_gridScroll.ClipNode = _gridVp;`"
                                + " 那一句删掉（退回只看 `MenuScroll.Viewport`）或把 `AddComponent<ViewportClip>()`"
                                + " 拿掉 ⇒ 本条红");
                    var b0p = FindChild(FindChild(content435, "CatalogItemShopContainer_0"), "background");
                    CheckRectPxUnion(b0p, 329.76f, 666.36f, 133.62f, 227.62f,
                                     "★ A435·乙（A26）：第 1 格的格底被**节点那条带**裁到 **94 高**"
                                   + "（= 227.62 − 133.62；正常态 477）—— 量的是**渲出来的真值**"
                                   + "（`ImageQuad.WorldW/H` 的并集，⛔ 不是计数器）");

                    MenuDraw.ApplyPxRect(vp435, vp435.parent, ShopTabPage.ScrollView);       // 还原
                    if (sc435 != null) sc435.OnChanged();
                    CheckByPrefix(content435, "CatalogItemShopContainer_", offers435.Length,
                                  "（还原）格数回到正常态");
                    CheckRectPxUnion(FindChild(FindChild(content435, "CatalogItemShopContainer_0"), "background"),
                                     329.76f, 666.36f, 133.62f, 610.62f,
                                     "（还原）第 1 格格底回到正常态 —— 毒解干净了（上一条不是残留状态蒙对的）");
                }

                // ---- ④ 「两态」之二：**显式实参**仍然赢（`Resolve` 第 1 支，逐字未改）----
                //   探针挂在**带节点的子树里**、显式给一个「与节点框部分重叠」的框 ⇒ 结果必须按显式那个算。
                //   ⚠️ 这一条会让 `ViewportClip.NodeShadowedByParam` **+1**（设计如此 —— 它是「显式传参盖住节点」
                //   的探测器，**不是缺陷计数**）；那条「== 0」的断言只落在 `RewardsScene.Run`（A489 的时点纪律），
                //   本场景不断它，而且每次 `*Scene.Run` 是**独立进程**，不会串到别人那儿。
                {
                    var expClip435 = new PxRect(200f, 50f, 900f, 700f);       // 与节点框（左 329.76）**部分重叠**
                    var probe435 = MenuDraw.Rect(vp435, CardArt.Solid(),
                                                 new PxRect(329.76f, 200f, 1200f, 800f), "A435b probe", 3000,
                                                 null, false, expClip435, default(Vector2));
                    CheckTrue(probe435 != null, "（前提）探针件建出来了（`CardArt.Solid()` 取得到）");
                    if (probe435 != null)
                    {
                        CheckNear(probe435.WorldW * 108f, 900f - 329.76f, 2f,
                                  "★ A435·乙（`Resolve` 第 1 支）：**显式 `clip` 形参仍逐字优先**"
                                + "（宽 = 900 − 329.76 = **570.24**；⛔ 节点框那一档会给 **870.24** = 1200 − 329.76）"
                                + " —— 这一档管着全壳「不传 `null` 时的老行为」；**改坏法**：把 `Resolve` 的优先级"
                                + "翻成「节点优先」⇒ 立刻红");
                        CheckNear(probe435.WorldH * 108f, 700f - 200f, 2f,
                                  "★ A435·乙（`Resolve` 第 1 支）：……高 = 700 − 200 = 500（同样按显式那个框算）");
                        Object.DestroyImmediate(probe435.gameObject);
                    }
                }
            }
            shop435.Close();
            Debug.Log(P + "  （A435 阶段 2 · 乙：商店窗验完 ⇒ 已 `Close()`）");
        }

        // ---------------- 收尾 ----------------
        ShopData.ResetForTest();
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        // 🔴 **2026-10-11（A350 · 调度台裁定）**：这一串是失败表的【重列】（`Check` 里已经逐条打过）
        //   ⇒ 行首标记统一成 `失败重列：`（原来写的是 `失败 N：` —— 同一件事四个宿主四种标记：
        //   `ShellScene`/`RewardsScene` 用 `✗`、`CollectionScene` 用 `✗`（拼在 StringBuilder 里）、
        //   本处用 `失败 N：`。`✗` 那两种会让日志里 `✗` 行数 = 失败数 ×2，`grep -c ✗` 直接数错）。
        for (int i = 0; i < _failures.Count; i++)
            Debug.LogError(P + "   失败重列：" + (i + 1) + "/" + _failures.Count + ". " + _failures[i]);
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    static void CheckByPrefix(Transform root, string prefix, int want, string msg)
    {
        int n = CountByPrefix(root, prefix);
        CheckTrue(n == want, $"{msg}（实测 {n}）");
    }
}
