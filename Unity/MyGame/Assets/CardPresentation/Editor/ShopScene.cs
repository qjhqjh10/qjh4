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
    static void CheckNear(float got, float want, float tol, string msg)
        => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F2} ≈ {want:F2}±{tol:F2}）");

    /// <summary>节点**在世界里的位置**要落在原版像素矩形的中心。</summary>
    static void CheckAt(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var want = LayoutSpace.RectCenter(x1, y1, x2, y2);
        float d = Vector3.Distance(t.position, want);
        CheckTrue(d <= 0.01f, $"{what} 在原版矩形中心（差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    /// <summary>🆕 **2026-10-04（A47 接线批）**：压暗层（「点窗外关窗」）命中区那条不变量。
    /// 🔴 **2026-10-07（A77⑬⑥）本文件里的副本已删** —— 唯一一份在 `MenuDraw.CheckShadeRule`。
    /// ⛔ 别在本文件里再长回来：调用点一律写 `MenuDraw.CheckShadeRule(CheckTrue, …)`。
    /// <para>🔴 **2026-10-07（A77⑬③）那条判据的期望值也换了**：不再比「调用方传进来的常量」
    /// （与 `ShadeHit` 的实参同一个符号 = 同义反复），改成**量同一扇窗里「视觉压暗层」那颗 quad 的
    /// `RenderQueue`**。🔴 **为什么仍要问 `WasShadeHit`**：档本来就对的那几扇窗，走不走公共件
    /// **没有任何可见行为差异** ⇒ 只有那一句能分出两种状态（改回自己那份 `MenuDraw.Hit` 就红）。</para></summary>

    /// <summary>🆕 **2026-10-06（A94 相 2）**：窗内面板「吸收层」（`MenuDraw.Absorb`）那一组 ——
    /// **四条不变量 + 两条真能分辨的行为**。
    ///
    /// <para>语义（判据 → `Shell/MenuDraw.Absorb` 的注释）：原版窗内面板那颗 `Image` 的
    /// `m_RaycastTarget = 1`、而「点它关窗」那颗 `BackgroundCloseButton` **全库都挂在压暗层上**
    /// ⇒ 点窗内空白处**原版什么都不发生**；我们这边命中候选只收 `WindowButton` ⇒ 射线会**穿过面板**
    /// 落到压暗层那颗「点窗外关窗」上（这就是 A94 那个缺陷）。</para>
    ///
    /// <para>🔴 **期望值全是原版值**：矩形 = **原版 prefab 里那块面板 `Image` 的 rect 字面量**
    /// （⛔ 不写被测那份实现**传进去的实参** —— 那是最浅一档的同式自证）；
    /// 档 = 该窗自己的**原版档常量**（`qShade` / `qContentMin`，与本文件已有的 `CheckShadeRule` 同一个来源）。</para>
    ///
    /// <para>🔴 **为什么两条行为必须一起断**：只断「点面板 ⇒ 不关」时，一个**根本关不掉的窗**也能绿；
    /// 只断「点面板外 ⇒ 关」时，把窗建小到「点哪儿都关」也绿。两条互为对照才分得出这两条路。</para>
    ///
    /// <para>⚠️ **点哪儿**：先试**原版矩形中心**，被窗内真件（按钮）盖住时沿一圈**固定的**候选点找
    /// 一个「命中是吸收层」的点。⛔ 「命中是谁」**不是期望值**，它只是**选点的条件**；
    /// 这一组断的是**窗的状态**（`state()`）。</para></summary>
    static void CheckAbsorbRule(string what, Transform winRoot, string nodeName,
                                float x1, float y1, float x2, float y2,
                                int qShade, int qContentMin, System.Func<WindowState> state)
    {
        // ① 节点在 ② 是公共件建的
        // ⚠️ **先按窗根的直接子件取**（相 1：20 个吸收层都是窗根的直接子件；只有 `RankedEventWindow`
        //   那个嵌在 `General Red Background` 底下）—— 直接子件取不到再退到递归查找。
        //   🔴 为什么不能一上来就递归找：`SkirmishEventWindow` 里**嵌着** `Searching Oponent Popup`，
        //   那扇自己也有一个 `AbsorbHit` ⇒ 递归找会按层级序先撞上谁不好说（本窗自己的那个排在前面，
        //   但那是**层级序的巧合**，不是判据）。
        var node = winRoot != null ? winRoot.Find(nodeName) : null;
        if (node == null) node = FindChild(winRoot, nodeName);
        CheckTrue(node != null,
                  $"{what}：吸收层节点 `{nodeName}` 在（`MenuDraw.Absorb` 建的 —— 原版面板那颗 `Image` 的等价物）");
        CheckTrue(MenuDraw.WasAbsorb(node),
                  $"{what}：它是**公共件 `MenuDraw.Absorb` 建的**（`MenuDraw.WasAbsorb`；哪扇窗自己再写一份就红）");
        // ③ 矩形 = 原版那块面板底图的 rect（量 `ImageQuad` 自己的渲染真值）
        float gx1, gy1, gx2, gy2;
        if (!RectOf(node, out gx1, out gy1, out gx2, out gy2))
        {
            CheckTrue(false, $"{what}：吸收层下面**没有 `ImageQuad`**（`PointerLayer` 的命中候选靠它 ⇒ 这一层等于没建）");
        }
        else
        {
            CheckNear(gx1, x1, 1.5f, $"{what}：吸收层渲染矩形**左沿** = 原版面板底图");
            CheckNear(gy1, y1, 1.5f, $"{what}：…**上沿**");
            CheckNear(gx2, x2, 1.5f, $"{what}：…**右沿**");
            CheckNear(gy2, y2, 1.5f, $"{what}：…**下沿**");
            // ④ 档 = 内容命中区档 − 1，且**严格夹在**压暗层与内容命中区之间
            var q = node.GetComponentInChildren<ImageQuad>();
            int wantQ = qContentMin - 1;
            Check(q != null ? q.RenderQueue : -1, wantQ,
                  $"{what}：吸收层的档 = **内容命中区档 − 1**（{qContentMin} − 1 = {wantQ}）");
            CheckTrue(q != null && qShade < q.RenderQueue && q.RenderQueue < qContentMin,
                      $"{what}：**{qShade} < 吸收层档 < {qContentMin}** —— 严格夹在压暗层与内容命中区之间"
                      + "（同档时 `ImageQuad` 的世界 z 恒 0，谁吃到命中退化成枚举顺序）");
        }
        Check(MenuDraw.AbsorbTierWarns, 0,
              $"{what}：`MenuDraw.Absorb` 的**档位告警一次都没响过**（响过 = 该窗没有空档，档算错了）");

        // ⑤⑥ 两条行为（互为对照）
        var pl = PointerLayer.Instance;
        CheckTrue(pl != null, $"{what}：场景里有指针层（没有的话下面两条等于没查）");
        if (pl == null) return;
        float ccx = (x1 + x2) * 0.5f, ccy = (y1 + y2) * 0.5f;
        // 候选点：**原版矩形中心**优先 → 中心外一圈(±80) → 最后**贴着四条边内缩的那一圈**
        // （面板的边框那一圈通常没有内容件；例：练习窗选卡组那一列中间**全被卡组格盖住**，
        //  只有左边距那 25px 是空的）。⛔ 候选是**固定**的（不扫描全图）⇒ 点了哪儿可复现。
        var cand = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(0f, -80f), new Vector2(0f, 80f), new Vector2(-80f, 0f), new Vector2(80f, 0f),
            new Vector2(-80f, -80f), new Vector2(80f, -80f), new Vector2(-80f, 80f), new Vector2(80f, 80f),
        };
        for (int k = 0; k < EdgeInset.Length; k++)
        {
            float e = EdgeInset[k];
            cand.Add(new Vector2(x1 + e - ccx, y1 + e - ccy)); cand.Add(new Vector2(x2 - e - ccx, y1 + e - ccy));
            cand.Add(new Vector2(x1 + e - ccx, y2 - e - ccy)); cand.Add(new Vector2(x2 - e - ccx, y2 - e - ccy));
            cand.Add(new Vector2(x1 + e - ccx, 0f));           cand.Add(new Vector2(x2 - e - ccx, 0f));
            cand.Add(new Vector2(0f, y1 + e - ccy));           cand.Add(new Vector2(0f, y2 - e - ccy));
        }
        float px = 0f, py = 0f; bool found = false;
        for (int i = 0; i < cand.Count && !found; i++)
        {
            float tx = ccx + cand[i].x, ty = ccy + cand[i].y;
            if (tx <= x1 + 3f || tx >= x2 - 3f || ty <= y1 + 3f || ty >= y2 - 3f) continue;   // 必须落在**原版**矩形里
            if (tx < 2f || tx > 1918f || ty < 2f || ty > 1078f) continue;                    // 而且**在屏幕里**（玩家点不到屏外的点）
            var hb = pl.ButtonAt(tx, ty);
            if (hb != null && hb.absorbOnly) { px = tx; py = ty; found = true; }
        }
        // 兜底：上面那圈**全都撞上内容件**时，按 **40px 固定步长**在矩形里走一遍（确定性 —— 不是随机），
        // 取第一个「命中是吸收层」的点。⚠️ 它只决定**点哪儿**，不参与任何期望值。
        for (float gy = y1 + 4f; gy <= y2 - 4f && !found; gy += 40f)
            for (float gx = x1 + 4f; gx <= x2 - 4f && !found; gx += 40f)
            {
                if (gx < 2f || gx > 1918f || gy < 2f || gy > 1078f) continue;
                var hbg = pl.ButtonAt(gx, gy);
                if (hbg != null && hbg.absorbOnly) { px = gx; py = gy; found = true; }
            }
        CheckTrue(found, $"{what}：**原版面板矩形以内找得到一个点、它的命中是吸收层**"
                         + "（找不到 ⇒ 窗内空白处没吃下这一下，射线会穿到压暗层上 ⇒ A94 那个缺陷还在）");
        if (!found) return;
        Check(state(), WindowState.Open, $"{what}：（前提）这一刻窗是开着的");
        CheckTrue(pl.ClickAt(px, py), $"{what}：点面板（真路径 `PointerLayer.ClickAt`，实点 ({px:F1},{py:F1})）");
        Check(state(), WindowState.Open,
              $"{what}：**点面板 ⇒ 窗不关**（原版面板那颗 `m_RaycastTarget = 1` 的 `Image` 吃掉了这一下）");
        // 点面板外：**钉死屏幕左上角 (5,5)**（本批 20 个吸收矩形都不覆盖它，相 1 逐条核过）。
        // ⛔ 不许改成「扫一圈找第一个命中压暗层的点」—— 命中区一旦又变得过大，搜索会从别的点**绕过去**、
        //   这条就再也查不出那个缺陷了（本工程那一族「弱断言分不出两种状态」；判据全文 → `CollectionScene.cs` 那一版）。
        const float OutX = 5f, OutY = 5f;
        var oHit = pl.ButtonAt(OutX, OutY);
        // 🔴 **判据 = 两条合起来**，⛔ 不许再退回「非吸收层 ∧ 属于本窗」那种**分不出两种状态**的弱条件
        //   （旧写法下**超大的内容命中区**三条全满足 ⇒ 照样绿，正是它把 A94 那个缺陷放过去了）：
        //     · `oHit.transform.IsChildOf(winRoot)` = **是这一扇自己的**（别家的窗顶掉它就红）；
        //     · `MenuDraw.WasShadeHit(oHit.transform)` = **是压暗层那一颗**（`ShadeHit` 建的，按节点上的**标记**认、
        //       ⛔ **不按名字认** —— 本族那颗节点有多个名字：`BackgroundHit` / `CloseHit`
        //       （名字是各调用点自己传的 `MenuDraw.ShadeHit(..., name)` 形参）⇒ 按名字写 `Find("BackgroundHit")`
        //       会把聊天窗那条**误判成红**）。
        //   ⛔ **别只写 `WasShadeHit`**：它认「是不是压暗层那颗」、**不认「是哪一扇的」**。
        CheckTrue(oHit != null && oHit.transform.IsChildOf(winRoot) && MenuDraw.WasShadeHit(oHit.transform),
                  $"{what}：**({OutX:F0},{OutY:F0}) 命中的就是这扇窗自己的压暗层那一颗**"
                  + "（命中区过大 / 吸收层 / 别家的窗把它顶掉时**这条红** —— 旧写法分辨不出，就是它放过了 A94）"
                  + "（实得 `" + (oHit != null ? oHit.name : "<null>") + "`"
                  + (oHit == null ? " = **什么都没命中**"
                     : !oHit.transform.IsChildOf(winRoot) ? " = **别家的窗**"
                     : !MenuDraw.WasShadeHit(oHit.transform) ? " = **本窗的，但不是压暗层那一颗**" : "")
                  + "）");
        CheckTrue(pl.ClickAt(OutX, OutY), $"{what}：点面板外 ({OutX:F0},{OutY:F0})（真路径）");
        Check(state(), WindowState.Closed, $"{what}：**点面板外 ⇒ 关窗**（两条互为对照才分得出）");
    }

    /// <summary>`CheckAbsorbRule` 贴边候选的**内缩**距离（px，固定三档；见那段注释）。</summary>
    static readonly float[] EdgeInset = { 6f, 20f, 40f };

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
    /// 图走 `ImageQuad.WorldW/H`、字走 `Label.WorldW/H`（**都是 TMP/材质的真测量**，不是回读常量）。
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
        if (lb != null) { node = lb.transform; w = lb.WorldW * 108f; h = lb.WorldH * 108f; }
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
    }

    /// <summary>世界 x → 画布像素 x（×108 + 960）。⚠️ **只能用在 x 上**。</summary>
    static float PxOf(float worldX) { return worldX * 108f + 960f; }
    /// <summary>世界 y → 画布像素 y。**y 是反的**（像素 y 向下）⇒ `540 − worldY × 108`。
    /// 🔴 拿 `PxOf` 去量 y 会得到**假警报**（本工程踩过，见 `RewardsScene` 的 `PxYOf`）。</summary>
    static float PxYOf(float worldY) { return 540f - worldY * 108f; }

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

        var anchors = new GameObject("Window Anchors").transform;
        // 商店的 `windowsPlacement = 10 (World)`（**与奖励窗的 5 Canvas 不同**）
        MakeHolder(anchors, "1 - Below Upper Bar Holder", WindowsPlacement.World);
        // 🆕 A7：`Booster Pack Open Window` 是 `windowsPlacement = **5 (Canvas)**`
        //（MB `MonoBehaviour_9012570135841684515.json` 原文）⇒ 本场景也得有那个锚点，
        // 否则 `GetWindowAnchor(Canvas)` 会报「找不到锚点」、窗口建在场景根上（能跑，但那不是原版的挂法）。
        MakeHolder(anchors, "2 - Canvas Holder Above upper bar", WindowsPlacement.Canvas);

        var wmGo = new GameObject("WindowsManager");
        var wm = wmGo.AddComponent<WindowsManager>();

        var win = ShopWindow.Create(wm);
        wm.OpenWindow(win);
        root = win.transform;
        return win;
    }

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
                CheckTrue(klb.WorldW * 108f <= 155f + 0.5f,
                          $"★ …而且**渲出来的宽 {klb.WorldW * 108f:F1} ≤ 框宽 155**"
                        + "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");
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
                    //    而 `WindowButton.Enter()` 是**幂等**的（`PromptPopup.cs:435` 的 `if (Hovered) return;`，
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
                    //      （`BoosterInfoPopup.cs:244-246`：`Node(...)` 建节点 + 子件 `Image` 带 quad）。
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

                // 🆕 A47：压暗层命中区（整屏那块 `Tap to close/Collider`）—— 档 = `QShade`(3170)
                //   （**压暗层自己那一档**；改前是 `QCloseSurface = QBase − 1` = 3169），
                //   严格低于卡命中区最低档 `QCard`(3171) ⇒ **卡照样能点**。
                //   ⚠️ 放在这里是因为这一块出厂是关着的（`Tap to close` 关 ⇒ `GetComponentInChildren`
                //     搜不到 `ImageQuad`），全翻开之后才量得到 —— 上面那句「出场关着」已经钉住了这个前提。
                {
                    var colNow = FindPath(t, "Tap to close/Collider");
                    //   🔴 A77⑬③：期望值改成**量**本窗的**视觉底层** —— 本窗**没有**压暗层那块图
                    //      （原版 `Tap to close/Collider` 是 `NonDrawingGraphic`，什么都不画）⇒ 它该对齐的是
                    //      同一档位上那块**画出来的**底：`Booster pack Background`（`Shell/BoosterPackOpenWindow.cs:278`
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
                CheckTrue(bp.MissingArt.Count == 0,
                          "这一扇用到的图**一张都不缺**（缺的会列在这里：" + string.Join("、", bp.MissingArt.ToArray()) + "）");
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
        CheckNoMissingSwapArt("商店窗（含 `OfferContainer` 那 19 份的 `WebShop` 钮）");
        Debug.Log(P + "   [按下图] 取不到的是 **" + WindowButton.MissingPressedArt.Count + " 条**"
                  + (WindowButton.MissingPressedArt.Count > 0
                     ? "（例：" + string.Join("、", WindowButton.MissingPressedArt.GetRange(
                           0, Mathf.Min(3, WindowButton.MissingPressedArt.Count)).ToArray()) + "）"
                     : ""));
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

        // ---------------- 收尾 ----------------
        ShopData.ResetForTest();
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        for (int i = 0; i < _failures.Count; i++) Debug.LogError(P + "失败 " + (i + 1) + "：" + _failures[i]);
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    static void CheckByPrefix(Transform root, string prefix, int want, string msg)
    {
        int n = CountByPrefix(root, prefix);
        CheckTrue(n == want, $"{msg}（实测 {n}）");
    }
}
