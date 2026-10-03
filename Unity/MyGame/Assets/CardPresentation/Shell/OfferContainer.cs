// OfferContainer.cs — 「商品条目容器」族（原版 `General Basic Offer Container *`，**19 个 prefab**）的
//                    **共用骨架构造器 + 19 条变体表**
//
// ============================ 出处（唯一正本） ============================
//  · 正本：`资料/阶段二_商店_原版规格.md` **§五·一**（19 件清单 / 11 种抽屉 / 三条硬限制 / 两个别照抄的坑）
//  · 逐份实测（**本文件那张表就是它跑出来的**，2026-10-03 实跑 19 次）：
//      python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<prefab 名>" --depth 3 --relative
//      （`--depth 6` 再跑一遍看 `name-bg` / `Price Display Button` / `WebShop Button Square Variant` 的子树）
//      坐标一律是**相对根左上角**（左上原点 · y 向下）
//  · 抽屉库与「选哪个抽屉」：`资料/普查产出_1003/ItemDrawer_抽屉系统.md` + `Shell/ItemDrawer.cs`
//    —— **判据只此一份**，本文件只**转调** `ItemDrawer.Draw(...)`，不另写一套
//  · 原版谁在用这一族（反编译）：`d:/2/tools/decomp_full/ContainerService__OpenOfferContainer.c` +
//    `Everguild.LiveOps.ContainerOfferEvent.*` ⇒ **服务端驱动的 LiveOps 报价**
//  · 抽屉那一侧的字段/类名：`资料/普查产出_1003/ItemDrawer_抽屉系统.md` §二（23 个 `ItemDrawer<T>` 子类）
//
// ============================ 🔴 三条硬限制（决定了「只能建骨架」） ============================
//  ① **母版↔Variant 的派生关系查不到** —— 真包里 `PrefabInstance` 对象数 = 0、GameObject typetree 只剩
//     `m_Component/m_Layer/m_Name/m_Tag/m_IsActive` 5 键（player build 已剥掉 prefab 实例信息），
//     解包目录也没有 `Prefab/` 类目录 ⇒ **没有裸的母版文件**（`GameObject/` 与真包里都没有）。
//  ② **「什么时候挂哪个抽屉」读不到** —— `ShopOfferContainer.GeneralOfferPopupDrawer.DrawRewards`
//     的**方法体是空 stub**（`d:/2/tools/decomp_full/` 里那个 `.c` 只有函数壳）。
//  ③ **抽屉美术全是运行期赋值** —— `Content→Image` 一律 `<无图>`，sprite 名查不到；
//     可复用的固定件只有 `40k_main_bt_nametag` · `40k_Profile_display_title` · `40K_general_icon_lock` ·
//     `40k_campaign_Premium-icon` · `Player Profile Border`。
//
// ============================ 🔴 我们这条链【没有数据源】 ============================
// 原版这 19 件是**服务端 LiveOps 报价**的载体（`ContainerService.OpenOfferContainer` 那一族），
// 本地**一个报价数据都没有**（15 个 LiveOps SO 全是过期活动）。
// ⇒ 本文件**只建「构造器 + 19 条变体表」**，⛔ **不编假入口**（不挂到任何菜单上、不假装点了会开什么）。
//    `Build(...)` 的 `onClick` 由调用方给；**给 null 就如实出声**（红线：不许静默失败）。
//
// ============================ 骨架（19 份**逐字段同构**，实测） ============================
//   <根 · 名字 = prefab 名 · 尺寸见变体表>
//     ├─ `raycast target`              整根那么大（原版是 `Image,EverguildButton` · `trans=1` ⇒ 整卡可点）
//     ├─ `background`                  整根 · `Image.m_Enabled = 0`（**19 份里只有 `Small` 那件是 1**）
//     │    ├─ `foreground`             整根 · `Image.m_Enabled = 0`（19 份全是 0）
//     │    ├─ `Dynamic Content`        **锚框 100×100**（矩形逐份见变体表）· 无脚本无 Graphic
//     │    │    └─ <抽屉槽 × N>         **只有这里逐份不同**（名字 = 原版抽屉节点名，见变体表）
//     │    └─ `name-bg`                `Image` 无图 · `Simple` · `m_Color = (0,0,0,0.431)`
//     │         ├─ `name`              TMP · 逐代字号见下
//     │         ├─ `type`              TMP · 逐代字号见下
//     │         ├─ `Price Display Button`      （脚本 `PriceDisplayButton`）
//     │         │    └─ `Generic UI Button`    `40K_button` · `Simple` + `preserveAspect` · `m_Color = (0.902,0.637,0.18,1)`
//     │         │         └─ `Price Display` （`HorizontalLayoutGroup`）→ `text`
//     │         ├─ `WebShop Button Square Variant`  （`HL=40K_button_square_hover` · `P=40K_button_square_pressed`）
//     │         │    ├─ `Highlight`      **sprite 未解出 ⇒ 判据空 ⇒ 不建**
//     │         │    ├─ `Button Image`   `40K_button_square` 103×107 · `Sliced` · `ppuMul = 2.5`
//     │         │    ├─ `Icon`           **sprite 未解出 ⇒ 判据空 ⇒ 不建**
//     │         │    └─ `Image`          出厂 **INACT** · 无图（建节点、关着）
//     │         └─ `Available Counter`  TMP · 出厂 `'Available: 1/5'`
//     ├─ `Badge`                       （0,10 → 根宽,75.8）· `WF_Special offer_Value` 324×87 **九宫 162,0,162,0** ·
//     │                                 `Sliced` · `m_Color = (0.651,0,0,1)`
//     │    └─ `Text (TMP)`             出厂 `'+60% value'` fs36 · 白 · `Left/Top` · 折行=1
//     └─ `Timer`                       （10,71 → 231.6,96 · 逐代同矩形）
//          ├─ `Timer Text`             出厂 `'5d 20h 15m'` fs28 `auto[18,28]` · `Left/Midline`
//          └─ `Icon`                   `WF_icon_clock` 64×64 · `Simple` + `preserveAspect`
//
//  文字那几件的 **auto / 折行** 是**逐个节点实读**的，别一律开（见 `LabelFit`）：
//    `name` `auto[10,42]` 不折行 · `type` `auto[10,字号]` 不折行 · `Available Counter` `auto[10,字号]` 不折行 ·
//    `Price…/text` `auto[13.46,40]` 不折行 · **`Badge/Text (TMP)` fs36 钉死（无 auto）+ 折行** ·
//    **`Timer Text` `auto[18,28]` + 折行**。横对齐**除价签那个 `text`（`Center/Capline`）外全是 `Left`**。
//
// ============================ 三个「几何档」（`name-bg` 那几件的矩形/字号按代分） ============================
//  🔴 **19 份按 `name-bg` 那一窝分成三代**（都是实读，见 `Geo391 / Geo778 / GeoSmall` 三个常量的注释）：
//    · `Geo391`  = **391×930**（3 份：`Booster_CardOrAltArt` 那三家）· `name` fs**42** · `type`/`Available` fs**34**
//    · `Geo778`  = **339×778**（15 份）· `name` fs**36.7** · `type`/`Available` 有 fs**30** 与 fs**34** 两支
//                  （**同一份里 `type` 与 `Available Counter` 的字号恒等** —— 15 份逐份核过）
//    · `GeoSmall`= **339×390**（1 份：`Small …`）· `name` fs36.7 · `type`/`Available` fs34
//  ⇒ `type`/`Available` 的字号放在 `Variant.TypeFs` 上（它是**逐份**的，不是逐代的）。
//  ⚠️ **`name` 恒 `auto[10,42]` · `字距 −3` · 白**；`type` 恒 `auto[10,最大=字号]` · `m_Color (0.717,0.717,0.717,1)`。
//
// ============================ 🔴 两个「别照抄的坑」（正本 §五·一 末） ============================
//  ① `Small`(339×390) 的抽屉**仍写着 778 档的坐标**（例 `Icon Container Drawer Variant` 的 y1 = −31.7，
//     顶端越出卡顶 31.7px）——**用途未判** ⇒ 本文件**不照抄任何抽屉矩形**（见下一条）。
//  ② 带 `AspectRatioFitter` 的抽屉量出来的矩形**会退化**（实测：`Icon Premium Campaign Drawer Variant`
//     高 **9797.25** / y −4412.3→5384.9 · `scl = 0.04925`；`Icon Expansion Pass Premium Drawer Variant (1)`
//     宽 **0.00**）——那是**未展开的模板位**（列表布局没跑）。
//  ⇒ **做法**：抽屉槽一律摆在 **`Dynamic Content` 那个锚框（100×100）上**，并且**一律走
//     `ItemDrawer.Draw(slot, box, item, qty, override, style)`** —— 原版那 19 个抽屉 prefab 的内部几何
//     本地**查不到**（`ItemDrawerConfig` SO 没解包，见 `ItemDrawer.cs` 文件头「读不到的两样」）。
//
// ============================ 🔴 我们挑的（原版读不到，逐条出声） ============================
//  · **「哪个抽屉槽被填」是我们定的** —— 原版那个决定器（`DrawRewards`）是**空 stub**（硬限制②）
//    ⇒ 规则：**兄弟序里第一个「非 INACT」的槽**填（`FirstActiveIndex`），其余槽照建、留空。
//  · **每个变体的抽屉矩形**是我们定的（一律 = `Dynamic Content` 锚框，理由见上一条坑）。
//  · **`name` / `type` / `Price` / `Badge` / `Timer` 的文案与物品**全由调用方给（原版是服务端给的）。
//  · **文字纵排是近似**：原版 `Available Counter` 是 `Left/Bottom`、`Badge/Text (TMP)` 是 `Left/Top`，
//    我们统一按**框内居中**画（全工程口径，同 `BoosterPackOpenWindow` 那条）。字号/颜色/横对齐是原版值。
//  · **`Price Display` 那一段的排版是我们挑的**：原版它是 `HorizontalLayoutGroup`（`icon` + `text`），
//    而 `icon` 的图在 prefab 里是空的（运行期赋币种图标）、`text` 的量出来的宽是 **0**（布局没跑）
//    ⇒ 我们只建 `Price Display` 空节点 + 一个 `text`，按**按钮整框居中**画，不摆 HLG。
//  · **两段音频 / 那颗 `Highlight` 呼吸光**（`UIBorderGlow` · `animDelay/animTime/playOnEnable`）**没做** ——
//    图与 cue 都查不到（硬限制③）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>「商品条目容器」族（原版 `General Basic Offer Container *` **19 个 prefab**）的共用骨架构造器。
    /// <para>🔴 **这一族本期只建构造器 + 变体表 + 断言，没有入口** —— 原版它是**服务端 LiveOps 报价**的载体
    /// （`ContainerService.OpenOfferContainer`），本地没有报价数据源（见文件头）。</para></summary>
    public static class OfferContainer
    {
        // ============================================================ 节点名（照 prefab · 逐字 · 断言按它找）

        public const string NRaycast = "raycast target";
        public const string NBackground = "background";
        public const string NForeground = "foreground";
        public const string NDynamic = "Dynamic Content";
        public const string NNameBg = "name-bg";
        public const string NName = "name";
        public const string NType = "type";
        public const string NPrice = "Price Display Button";
        public const string NPriceBtn = "Generic UI Button";
        public const string NPriceBox = "Price Display";
        public const string NPriceText = "text";
        public const string NWebShop = "WebShop Button Square Variant";
        public const string NWebShopImage = "Button Image";
        public const string NWebShopGfx = "Image";
        public const string NAvail = "Available Counter";
        public const string NBadge = "Badge";
        public const string NBadgeText = "Text (TMP)";
        public const string NTimer = "Timer";
        public const string NTimerText = "Timer Text";
        public const string NTimerIcon = "Icon";

        /// <summary>`MenuDraw.Hit` 在里面那个 quad 的名字（命中区节点本身叫 `raycast target`）。</summary>
        public const string NHitQuad = "Hit";

        // ============================================================ 固定件的图（判据 = 正本 §五·一 + 实测）

        /// <summary>`Badge` 的图。⚠️ prefab 里那个名字带**空格**（`WF_Special offer_Value`），
        /// 工程里导入成下划线名（`CardArt.MenuUi` 不做「空格 → 下划线」转换）⇒ 代码里写导入后的名字。</summary>
        public const string BadgeArt = "WF_Special_offer_Value";
        /// <summary>`Badge` 的九宫格边界（贴图像素 · L,B,R,T）—— `m_Border` 实读，与 324×87 自洽。
        /// 🔴 **L+R = 324 = 贴图宽**（`Sprite/WF_Special offer_Value.json` 的 `m_Border = (162,0,162,0)`），
        /// 也就是**只有左右两个端帽、没有中段**。两处后果（都实测过，如实记）：
        /// ① 我们这份共用助手 `ImageQuad.CreateNineSlice` 会因为 `uR &lt;= uL` 打一条
        ///    `border 比图还大，退回单块` 的警告 —— **那是误报**（它是「端帽铺满整张图」这种合法形状，不是真超了）；
        /// ② 我们这里**两个端帽各被拉成 169.5px**（`sc = 339÷324 = 0.9688` 后 `wm` 归零），
        ///    而原版 uGUI 的 `GetAdjustedBorders` 只会在 `border.x + border.z **>** rect.width` 时才缩
        ///    ⇒ 原版是 **162 + 15px（中段取同一列纹素）+ 162**。差 7.5px/端帽（≈4.6%），**画面看不出来**。
        ///    ⚠️ 这一条**没改共用助手**（`MenuDraw` / `ImageQuad` 不在本次白名单），只记在这里。</summary>
        public static readonly Vector4 BadgeBorder = new Vector4(162f, 0f, 162f, 0f);
        public const float BadgeTexW = 324f, BadgeTexH = 87f;
        /// <summary>`Badge` 的 `Image.m_Color`（实读）。</summary>
        public static readonly Color BadgeTint = new Color(0.651f, 0f, 0f, 1f);

        /// <summary>`Timer/Icon` 的图（与商店页时间条同一张）。原版那张是 `preserveAspect` 的 64²，
        /// 而框只有 30×25 ⇒ 按 `min(框)` 内接，与商店页 `Clock Icon` 同一条口径。</summary>
        public const string ClockArt = "WF_icon_clock";

        /// <summary>`Price Display Button/Generic UI Button` 的图 + 色（实读；与商店格那颗**同一份值**
        /// ⇒ 色直接引 `ShopTabPage.PriceTint`，不抄第二份）。</summary>
        public const string PriceArt = "40K_button";

        /// <summary>`WebShop Button Square Variant/Button Image` 的图 —— 正本 §五·一 的骨架行写的
        /// `40K_button_square 103×107`（贴图本体实读也是 103×107，自洽）。
        /// 🔴 **不在** §五·一 那份「可复用固定件」清单里（那 5 个是别的），是**骨架行**给的。</summary>
        public const string WebShopArt = "40K_button_square";
        /// <summary>`40K_button_square` 的 `m_Border`（L,B,R,T）与本体尺寸 —— `Sprite/40K_button_square.json` 实读
        /// （在 `bundle_duplicateassetisolation_assets_all` 与 `sharedassets0` 里，两处同值）。</summary>
        public static readonly Vector4 WebShopBorder = new Vector4(36f, 28f, 37f, 29f);
        public const float WebShopTexW = 103f, WebShopTexH = 107f;
        /// <summary>`Button Image` 的 `m_PixelsPerUnitMultiplier`（实读 = 2.5）⇒ **画出来的角块**
        /// = `m_Border ÷ 2.5`（判据 → `MenuDraw.Nine` 的 `borderOutPx` 注释）。</summary>
        public const float WebShopPpuMul = 2.5f;

        /// <summary>`name-bg` 的 `Image.m_Color`（实读，无图 ⇒ 画一块半透明黑板）。</summary>
        public static readonly Color NameBgTint = new Color(0f, 0f, 0f, 0.431f);
        /// <summary>`type` 的 `m_fontColor`（实读；19 份一致）。</summary>
        public static readonly Color TypeColor = new Color(0.717f, 0.717f, 0.717f, 1f);
        /// <summary>`name` 的 `m_fontColor`（实读；19 份一致）。</summary>
        public static readonly Color NameColor = new Color(1f, 1f, 1f, 1f);

        // ============================================================ 出厂文本（实读 · 19 份逐字相同）

        /// <summary>`Badge/Text (TMP)` 的出厂文本（实读）。</summary>
        public const string DefBadgeText = "+60% value";
        /// <summary>`Timer/Timer Text` 的出厂文本（实读）。</summary>
        public const string DefTimerText = "5d 20h 15m";
        /// <summary>`Available Counter` 的出厂文本（实读）。</summary>
        public const string DefAvailText = "Available: 1/5";
        /// <summary>`name` / `type` 的出厂文本（实读；原版运行期由服务端覆盖）。</summary>
        public const string DefNameText = "Legendary Wildcard Bundle ";
        public const string DefTypeText = "Booster Pack";

        // ============================================================ 渲染队列（**我们挑的**：原版这一族没有宿主窗口）

        // 相对基准的偏移：
        // ⚠️ **`raycast target` 是整根那么大**（原版就是），而 `WebShop Button Square Variant` 是它里面的
        //    另一颗钮 ⇒ 后者的命中区**必须排得更靠上**（`QoWebShopHit > QoHit`），否则那一颗**点不动**
        //    （`PointerLayer` 取队列最高的那条；本工程 2026-10-03 在 `BoosterInfoPopup` 上刚栽过一次）。
        const int QoBg = 0, QoNameBg = 1, QoDrawer = 2, QoBadge = 3, QoText = 4, QoWebShop = 5,
                  QoHit = 9, QoWebShopHit = 10;

        // ============================================================ 一个「几何档」

        /// <summary>一代骨架的矩形表（`name-bg` 那一窝）。**每一格都是 `menu_dump --relative` 实读值**，
        /// 坐标是**相对根左上角**。三代见下三个常量。</summary>
        public struct Geo
        {
            public float W, H;                  // 根尺寸
            public float NameFs;                // `name` 的字号
            public PxRect NameBg, Name, Type, Price, WebShop, WebShopDummy, Avail,
                          BadgeText, Timer, TimerText, TimerIcon;
            /// <summary>`background` 的 `Image.m_Enabled`（**19 份里只有 `Small` 那件是 1**）。</summary>
            public bool BgOn;
            /// <summary>`foreground` 的 `Image.m_Enabled`（19 份**全是 0**）。</summary>
            public bool FgOn;
        }

        /// <summary>**391×930** 那一代（3 份：`Booster_CardOrAltArt` 三家）。
        /// `name` fs**42** `auto[10,42]` / `type` fs**34** `auto[10,34]`。</summary>
        public static readonly Geo Geo391 = new Geo
        {
            W = 391f, H = 930f, NameFs = 42f,
            NameBg = new PxRect(2.5f, 756.5f, 388.2f, 931.0f),
            Name = new PxRect(14.1f, 769.8f, 376.6f, 823.8f),
            Type = new PxRect(14.1f, 819.8f, 376.6f, 854.8f),
            Price = new PxRect(11.8f, 860.3f, 274.0f, 922.6f),
            WebShop = new PxRect(286.7f, 859.9f, 376.6f, 922.6f),
            WebShopDummy = new PxRect(298.2f, 861.2f, 368.9f, 911.8f),
            Avail = new PxRect(12.5f, 710.6f, 320.5f, 751.5f),
            BadgeText = new PxRect(16f, 26f, 380.2f, 59.8f),
            Timer = new PxRect(10f, 71f, 231.6f, 96f),
            TimerText = new PxRect(40f, 69f, 261.6f, 98f),
            TimerIcon = new PxRect(10f, 71f, 40f, 96f),
            BgOn = false, FgOn = false,
        };

        /// <summary>**339×778** 那一代（15 份）。`name` fs**36.7** `auto[10,42]`。
        /// ⚠️ `type`/`Available` 的字号**逐份**（30 或 34）⇒ 在 `Variant.TypeFs` 上。</summary>
        public static readonly Geo Geo778 = new Geo
        {
            W = 339f, H = 778f, NameFs = 36.7f,
            NameBg = new PxRect(2.5f, 632.8f, 336.2f, 779.0f),
            Name = new PxRect(12.5f, 631.9f, 326.2f, 685.9f),
            Type = new PxRect(12.5f, 681.9f, 326.2f, 716.9f),
            Price = new PxRect(10.5f, 719.8f, 237.3f, 772.0f),
            WebShop = new PxRect(248.4f, 719.5f, 326.2f, 772.0f),
            WebShopDummy = new PxRect(258.3f, 720.5f, 319.5f, 762.9f),
            Avail = new PxRect(12.5f, 586.9f, 320.5f, 627.8f),
            BadgeText = new PxRect(16f, 26f, 328.2f, 59.8f),
            Timer = new PxRect(10f, 71f, 231.6f, 96f),
            TimerText = new PxRect(40f, 69f, 261.6f, 98f),
            TimerIcon = new PxRect(10f, 71f, 40f, 96f),
            BgOn = false, FgOn = false,
        };

        /// <summary>**339×390** 那一代（1 份：`Small … Single Item Type`）。
        /// 🔴 与 `Geo778` 的差别**只有 `name-bg` 那一窝的 y**（往上平移 386.6）+ **`background` 的 `Image` 是 enabled**。
        /// ⚠️ 它的**抽屉**仍写着 778 档坐标（`Icon Container Drawer Variant` 的 y1 = **−31.7**，越出卡顶）
        /// —— 用途未判，我们照旧**不照抄抽屉矩形**（文件头那条坑）。</summary>
        public static readonly Geo GeoSmall = new Geo
        {
            W = 339f, H = 390f, NameFs = 36.7f,
            NameBg = new PxRect(2.5f, 246.2f, 336.2f, 391.0f),
            Name = new PxRect(12.5f, 244.6f, 326.2f, 298.6f),
            Type = new PxRect(12.5f, 294.6f, 326.2f, 329.6f),
            Price = new PxRect(10.5f, 332.4f, 237.3f, 384.1f),
            WebShop = new PxRect(248.4f, 332.1f, 326.2f, 384.1f),
            WebShopDummy = new PxRect(258.3f, 333.1f, 319.5f, 375.1f),
            Avail = new PxRect(12.5f, 200.4f, 320.5f, 241.2f),
            BadgeText = new PxRect(16f, 26f, 328.2f, 59.8f),
            Timer = new PxRect(10f, 71f, 231.6f, 96f),
            TimerText = new PxRect(40f, 69f, 261.6f, 98f),
            TimerIcon = new PxRect(10f, 71f, 40f, 96f),
            BgOn = true, FgOn = false,
        };

        // ============================================================ 19 条变体表

        /// <summary>一个变体。**四个字段的出处逐条写在 <see cref="Variants"/> 那张表上**。</summary>
        public struct Variant
        {
            /// <summary>**原 prefab 名**（= 我们建出来的根节点名，逐字照抄）。</summary>
            public string Prefab;
            /// <summary>几何档（见 `Geo391 / Geo778 / GeoSmall`）。</summary>
            public Geo G;
            /// <summary>`type` / `Available Counter` 的字号（**逐份**：30 或 34；同一份里两者恒等）。
            /// `name` 的字号在 `G.NameFs`（逐代）。</summary>
            public float TypeFs;
            /// <summary>`Dynamic Content` 锚框 —— **相对根左上角**，19 份**恒为 100×100**，只有 y 不同。</summary>
            public PxRect Dyn;
            /// <summary>`Dynamic Content` 下挂的抽屉节点名（**兄弟序**，与 prefab 逐字，含 `" (1)"` 这类重名后缀）。</summary>
            public string[] Drawers;
            /// <summary>其中**出厂 INACT** 的那几个（`null` = 一个都没有）。建成但 `SetActive(false)`。</summary>
            public string[] Off;

            public Variant(string prefab, Geo g, float typeFs, PxRect dyn, string[] drawers, string[] off)
            { Prefab = prefab; G = g; TypeFs = typeFs; Dyn = dyn; Drawers = drawers; Off = off; }
        }

        /// <summary>根宽 → `Badge` 的矩形（`0,10 → 根宽,75.8`，19 份**只有宽度跟根走**）。</summary>
        public static PxRect BadgeRect(Geo g) { return new PxRect(0f, 10f, g.W, 75.8f); }

        /// <summary>**19 条变体表**。每条上面那行短注释就是**它自己的记录**（根尺寸 · `Dynamic Content` 矩形 ·
        /// 抽屉槽数 · 出厂 INACT 数 · 字号档），**逐条**都来自同一条可复现命令（2026-10-03 逐份跑）：
        /// <code>python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "&lt;下面那条的名字&gt;" --depth 3 --relative</code>
        /// （`--depth 6` 那一遍用来核 `name-bg` / `Price Display Button` / `WebShop Button Square Variant` 的子树。）
        /// `INACT` 标记取自同一份 dump 的 `act` 列。**每一格都不是推的。**
        /// <para>⚠️ **没有裸的「General Basic Offer Container」母版文件**（`GameObject/` 与真包里都没有）——
        /// 正本 §五·一 已经写明，这里再钉一次。</para>
        /// <para>🆕 **2026-10-03 实读补充（正本 §五·一 说「去重后 17 种结构」，按这份实读可以再细分）**：
        /// `Dynamic Content` 的 **y 在 339×778 那一代里还有 4 个值**（358 ×9 份 · 295 ×1 · 280 ×3 · 272 ×2），
        /// 加上 `type`/`Available` 的 **fs30 / fs34 两支** ⇒ 若按「根尺寸 + Dyn 矩形 + 字号档 + 抽屉组合」
        /// 去重，**是 19 组、没有一对完全相同**；正本那个 17 是**只按结构（抽屉组合 + 卡高）**去重的口径。
        /// 两种口径都在，别把其中一个当另一个。</para></summary>
        public static readonly Variant[] Variants =
        {
            // ---- 391×930 那三家（`name` fs42 · `type`/`Available` fs34）----
            // 391×930 · Dyn(145.5,348)→(245.5,448) · **3 槽**（INACT **0**）
            new Variant("General Basic Offer Container Booster_CardOrAltArt", Geo391, 34f,
                        new PxRect(145.5f, 348f, 245.5f, 448f),
                        new[] { "Icon Container Drawer Variant", "Card Alternate Art Drawer", "Card Drawer" }, null),

            // 391×930 · Dyn(145.5,348)→(245.5,448) · **6 槽**（INACT **1** = `Card Drawer`）
            new Variant("General Basic Offer Container Booster_CardOrAltArt_Cardback_Avatar_Title", Geo391, 34f,
                        new PxRect(145.5f, 348f, 245.5f, 448f),
                        new[] { "Cardback Drawer", "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Card Alternate Art Drawer", "Card Drawer", "Title Drawer Horizontal Variant (1)" },
                        new[] { "Card Drawer" }),

            // 391×930 · Dyn(145.5,348)→(245.5,448) · **6 槽**（INACT **1** = `Card Drawer`）
            new Variant("General Basic Offer Container Booster_CardOrAltArt__AvatarORTitle", Geo391, 34f,
                        new PxRect(145.5f, 348f, 245.5f, 448f),
                        new[] { "Cardback Drawer", "Icon Container Drawer Variant", "Card Alternate Art Drawer",
                                "Card Drawer", "Title Drawer Horizontal Variant (1)", "Icon Avatar Drawer Variant" },
                        new[] { "Card Drawer" }),

            // ---- 339×778 那十五家 ----
            // 339×778 · Dyn(119.5,358)→(219.5,458) · **2 槽**（INACT 0）· `type`/`Available` fs**30**
            new Variant("General Basic Offer Container Variant 2 Currencies", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Currency Drawer Variant", "Icon Currency Drawer Variant (1)" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **3 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Booster + 2 Currencies", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Icon Currency Drawer Variant (1)",
                                "Icon Currency Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **4 槽**（INACT 0）· fs**30**
            // 🔴 正本 §五·一 拿它当「代表骨架」举例的那一份（也是本工程实拍抽的那份）
            new Variant("General Basic Offer Container Variant Booster_avatar_cardback_title", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Cardback Drawer",
                                "Title Drawer Horizontal Variant", "Icon Avatar Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **3 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Booster_avatar_resource", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Icon Currency Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **3 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Booster_cardback_resource", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Cardback Drawer",
                                "Icon Currency Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **3 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Booster_title_resource", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Icon Currency Drawer Variant",
                                "Title Drawer Horizontal Variant (1)" }, null),

            // 339×778 · Dyn(119.5,280)→(219.5,380) · **3 槽**（INACT 0）· fs**34**
            // 🔴 `Deck Drawer` **只有这一份用到**（正本 §五·一）
            new Variant("General Basic Offer Container Variant Deck_cardback_avatar", Geo778, 34f,
                        new PxRect(119.5f, 280f, 219.5f, 380f),
                        new[] { "Deck Drawer", "Cardback Drawer", "Icon Avatar Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **5 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Premium_Booster_avatar_cardback_title", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Cardback Drawer", "Title Drawer Horizontal Variant",
                                "Icon Avatar Drawer Variant", "Icon Premium Campaign Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **7 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Premium_Booster_avatar_cardback_title_resource",
                        Geo778, 30f, new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Container Drawer Variant", "Cardback Drawer", "Title Drawer Horizontal Variant",
                                "Icon Avatar Drawer Variant", "Icon Expansion Pass Premium Drawer Variant",
                                "Icon Currency Drawer Variant", "Icon Premium Campaign Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,280)→(219.5,380) · **4 槽**（INACT 0）· fs**34**
            new Variant("General Basic Offer Container Variant Premium_Premium_cardback_avatar", Geo778, 34f,
                        new PxRect(119.5f, 280f, 219.5f, 380f),
                        new[] { "Cardback Drawer", "Icon Avatar Drawer Variant",
                                "Icon Premium Campaign Drawer Variant",
                                "Icon Premium Campaign Drawer Variant (1)" }, null),

            // 339×778 · Dyn(119.5,358)→(219.5,458) · **3 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Premium_Resource", Geo778, 30f,
                        new PxRect(119.5f, 358f, 219.5f, 458f),
                        new[] { "Icon Currency Drawer Variant", "Icon Expansion Pass Premium Drawer Variant",
                                "Icon Premium Campaign Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,295)→(219.5,395) · **5 槽**（INACT 0）· fs**30**
            new Variant("General Basic Offer Container Variant Premium_booster_title_avatarOrResource", Geo778, 30f,
                        new PxRect(119.5f, 295f, 219.5f, 395f),
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Title Drawer Horizontal Variant", "Icon Currency Drawer Variant",
                                "Icon Premium Campaign Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,272)→(219.5,372) · **12 槽**（INACT **8**）· fs**34**
            // ⚠️ 这一份的抽屉**绝大多数出厂是 INACT** —— 原版运行期由那个空 stub 决定开哪个
            new Variant("General Basic Offer Container Variant Single Item Type", Geo778, 34f,
                        new PxRect(119.5f, 272f, 219.5f, 372f),
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Icon Avatar Drawer Variant 2", "Title Drawer Horizontal Variant",
                                "Icon Currency Drawer Variant", "Icon Currency Drawer Variant 2",
                                "Icon Currency Drawer Variant 3", "Icon Premium Campaign Drawer Variant",
                                "Card Drawer", "Icon Expansion Pass Premium Drawer Variant (1)",
                                // 🔴 这一件**只有这一份用到**（正本 §五·一）
                                "Icon Avatar Border Drawer", "Cardback Drawer" },
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Icon Avatar Drawer Variant 2", "Title Drawer Horizontal Variant",
                                "Icon Currency Drawer Variant", "Icon Currency Drawer Variant 2",
                                "Icon Currency Drawer Variant 3", "Icon Premium Campaign Drawer Variant" }),

            // 339×778 · Dyn(119.5,272)→(219.5,372) · **3 槽**（INACT 0）· fs**34**
            new Variant("General Basic Offer Container Variant avatarOrTitle_resource", Geo778, 34f,
                        new PxRect(119.5f, 272f, 219.5f, 372f),
                        new[] { "Icon Currency Drawer Variant", "Title Drawer Horizontal Variant (1)",
                                "Icon Avatar Drawer Variant" }, null),

            // 339×778 · Dyn(119.5,280)→(219.5,380) · **6 槽**（INACT 0）· fs**34**
            new Variant("General Basic Offer Container Variant cardback_premiumOrAvatarOrResource_titleOrResource",
                        Geo778, 34f, new PxRect(119.5f, 280f, 219.5f, 380f),
                        new[] { "Cardback Drawer", "Icon Avatar Drawer Variant", "Title Drawer Horizontal Variant",
                                "Icon Currency Drawer Variant", "Icon Currency Drawer Variant 2",
                                "Icon Premium Campaign Drawer Variant" }, null),

            // ---- 339×390 那一份（`Small …`）----
            // 339×**390**（唯一那个 `Small`）· Dyn(119.5,78)→(219.5,178) · **7 槽**（INACT **6**）· fs**34**
            // 🔴 它是 19 份里**唯一** `background` 的 `Image.m_Enabled = 1` 的一份（其余 18 份都是 0）
            new Variant("Small General Basic Offer Container Variant Single Item Type", GeoSmall, 34f,
                        new PxRect(119.5f, 78f, 219.5f, 178f),
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Title Drawer Horizontal Variant", "Icon Currency Drawer Variant",
                                "Cardback Drawer", "Icon Expansion Pass Premium Drawer Variant",
                                "Icon Premium Campaign Drawer Variant" },
                        new[] { "Icon Container Drawer Variant", "Icon Avatar Drawer Variant",
                                "Title Drawer Horizontal Variant", "Cardback Drawer",
                                "Icon Expansion Pass Premium Drawer Variant",
                                "Icon Premium Campaign Drawer Variant" }),
        };

        /// <summary>按名字取一条变体（自检按表逐条建时用）。找不到 ⇒ `false`。</summary>
        public static bool Find(string prefab, out Variant v)
        {
            for (int i = 0; i < Variants.Length; i++)
                if (Variants[i].Prefab == prefab) { v = Variants[i]; return true; }
            v = default(Variant);
            return false;
        }

        /// <summary>出厂**非 INACT** 的槽里第一个的下标；一个都没有 ⇒ −1。
        /// 🔴 **这是我们挑的** —— 原版那个决定器（`DrawRewards`）是**空 stub**（文件头硬限制②）。</summary>
        public static int FirstActiveIndex(Variant v)
        {
            if (v.Drawers == null) return -1;
            for (int i = 0; i < v.Drawers.Length; i++)
                if (!IsOff(v, v.Drawers[i])) return i;
            return -1;
        }

        /// <summary>这个抽屉节点名在该变体里是不是**出厂 INACT**。</summary>
        public static bool IsOff(Variant v, string drawer)
        {
            if (v.Off == null) return false;
            for (int i = 0; i < v.Off.Length; i++) if (v.Off[i] == drawer) return true;
            return false;
        }

        // ============================================================ 容器里装什么（**全由调用方给**）

        /// <summary>一个容器要显示的内容。原版这些**全由服务端报价给**（本地没有）。
        /// 空字符串 ⇒ 那一段**不画**（不是画个空框）。</summary>
        public struct Content
        {
            /// <summary>抽屉要画的东西（`ItemDrawer.Spec(...)` 组）。`Kind == None` ⇒ 抽屉**不画**（原版第②步）。</summary>
            public ItemSpec Item;
            public int Quantity;
            public string Name;         // `name`
            public string Type;         // `type`
            public string Price;        // `Price Display Button/…/text`
            public string BadgeText;    // `Badge/Text (TMP)`（出厂 `DefBadgeText`）
            public string TimerText;    // `Timer/Timer Text`（出厂 `DefTimerText`）
            public string Available;    // `Available Counter`（出厂 `DefAvailText`）；**空 = 不画**

            /// <summary>照出厂值填一份（**只有抽屉那一段是空的** —— 它要物品，出厂没有）。</summary>
            public static Content Def()
            {
                return new Content
                {
                    Item = default(ItemSpec), Quantity = 1,
                    Name = DefNameText, Type = DefTypeText, Price = null,
                    BadgeText = DefBadgeText, TimerText = DefTimerText, Available = DefAvailText,
                };
            }
        }

        /// <summary>建出来的东西（自检靠它认「填没填 / 填的是哪个槽」）。</summary>
        public struct Built
        {
            public Transform Root;      // 容器根（名字 = `Variant.Prefab`）
            public Transform Dynamic;   // `Dynamic Content`
            public Transform Filled;    // 被填的那个抽屉槽；`null` = 一个都没填
            public string Drawer;       // `ItemDrawer` 实际选的抽屉名；`null` = 没画
            public bool Placeholder;    // 走了占位板（调用方**必须出声**）
            public bool MissingArt;     // 有图取不到
            public bool FallbackArt;    // 用了退档图（原版那张取不到 —— 调用方**必须出声**）
        }

        // ============================================================ 建

        /// <summary>照变体表建一棵容器骨架，返回根节点。
        /// <para>坐标：`(x, y)` 是根左上角（画布像素 · 左上原点 · y 向下）；尺寸取 `v.G.W/H`。</para>
        /// <para>🔴 **落点**：原版这一族由 `ContainerService.OpenOfferContainer` 在**服务端报价**到达时实例化，
        /// 我们**没有那个数据源** ⇒ 这里只提供 `(x,y)` 让调用方摆，**不编入口**（文件头）。</para></summary>
        public static Built Build(Transform parent, Variant v, float x, float y, Content c,
                                  int qBase, System.Action onClick, bool fill = true)
        {
            var b = new Built();
            var g = v.G;
            PxRect R(float x1, float y1, float x2, float y2) { return new PxRect(x + x1, y + y1, x + x2, y + y2); }

            // ---- 根 ----
            b.Root = MenuDraw.Node(parent, v.Prefab, new PxRect(x, y, x + g.W, y + g.H));

            // ---- `background` / `foreground`：**只在该份的 `Image.m_Enabled = 1` 时画**（无图 ⇒ 纯色块）----
            var bg = MenuDraw.Node(b.Root, NBackground, R(0f, 0f, g.W, g.H));
            if (g.BgOn)
                MenuDraw.Rect(bg, CardArt.Solid(), R(0f, 0f, g.W, g.H), NBackground + " Gfx", qBase + QoBg,
                              new Color(1f, 1f, 1f, 1f));
            var fg = MenuDraw.Node(bg, NForeground, R(0f, 0f, g.W, g.H));
            if (g.FgOn)
                MenuDraw.Rect(fg, CardArt.Solid(), R(0f, 0f, g.W, g.H), NForeground + " Gfx", qBase + QoBg,
                              new Color(1f, 1f, 1f, 1f));

            // ---- `Dynamic Content`：**锚框**（原版无脚本无 Graphic）+ 抽屉槽 ----
            b.Dynamic = MenuDraw.Node(bg, NDynamic, R(v.Dyn.x1, v.Dyn.y1, v.Dyn.x2, v.Dyn.y2));
            int filled = fill ? FirstActiveIndex(v) : -1;
            if (v.Drawers != null)
            {
                for (int i = 0; i < v.Drawers.Length; i++)
                {
                    // 抽屉槽一律摆在 `Dynamic Content` 的框上（**不照抄 prefab 的抽屉矩形** —— 那是未展开的模板位，见文件头）
                    var slot = MenuDraw.Node(b.Dynamic, v.Drawers[i],
                                             R(v.Dyn.x1, v.Dyn.y1, v.Dyn.x2, v.Dyn.y2));
                    if (IsOff(v, v.Drawers[i])) slot.gameObject.SetActive(false);   // 照 prefab 出厂状态
                    if (i != filled) continue;

                    // 填槽 —— **一律走已有的 `ItemDrawer.Draw(...)`**（判据只此一份），档位 = `DrawerOverride.Shop`(20)
                    //（`CatalogItemContainer__OnInitialize.c` 用 0x14；这一族原版挂抽屉走的是哪个档**读不到**
                    //  —— `DrawRewards` 是空 stub，见文件头硬限制② ⇒ 取 Shop 档，这是我们挑的）
                    var st = ItemDrawerStyle.Default(qBase + QoDrawer, qBase + QoDrawer, qBase + QoText);
                    st.NodeName = "Item Drawer";
                    st.IconFill = 0.7f; st.QuantityPx = 0f; st.NamePx = 0f;
                    var drew = ItemDrawer.Draw(slot, R(v.Dyn.x1, v.Dyn.y1, v.Dyn.x2, v.Dyn.y2),
                                               c.Item, c.Quantity, DrawerOverride.Shop, st);
                    b.Filled = slot; b.Drawer = drew.Drawer;
                    b.Placeholder = drew.Placeholder; b.MissingArt = drew.MissingArt;
                    b.FallbackArt = drew.FallbackArt;
                    // 红线：不许静默失败 —— 判据说「该画」却没画出来 / 落了占位板 / 用了退档图，都要出声
                    if (drew.Node == null)
                        Debug.LogWarning("[OfferContainer] `" + v.Prefab + "` 的槽 `" + v.Drawers[i]
                                         + "`：`ItemDrawer.Draw` **什么都没画**（物品判据空，原版第②步）");
                    else if (drew.Placeholder)
                        Debug.LogWarning("[OfferContainer] `" + v.Prefab + "` 的槽 `" + v.Drawers[i]
                                         + "`：抽屉**落了占位板**（`" + (c.Item.Id ?? "<空>") + "` 的图取不到）");
                    else if (drew.FallbackArt)
                        Debug.LogWarning("[OfferContainer] `" + v.Prefab + "` 的槽 `" + v.Drawers[i]
                                         + "`：抽屉用了**退档图**（`" + drew.Art + "`）");
                }
            }

            // ---- `name-bg` 那一窝 ----
            var nb = MenuDraw.Node(bg, NNameBg, R(g.NameBg.x1, g.NameBg.y1, g.NameBg.x2, g.NameBg.y2));
            MenuDraw.Rect(nb, CardArt.Solid(), R(g.NameBg.x1, g.NameBg.y1, g.NameBg.x2, g.NameBg.y2),
                          "name-bg Gfx", qBase + QoNameBg, NameBgTint);

            LabelFit(nb, R(g.Name.x1, g.Name.y1, g.Name.x2, g.Name.y2), c.Name, NameColor, NName,
                     g.NameFs, 10f, qBase + QoText);
            LabelFit(nb, R(g.Type.x1, g.Type.y1, g.Type.x2, g.Type.y2), c.Type, TypeColor, NType,
                     v.TypeFs, 10f, qBase + QoText);
            BuildPrice(nb, R, g, c, qBase);
            BuildWebShop(nb, R, g, qBase);

            // `Available Counter` —— 空串 ⇒ **不画**（原版那一格是 `Available: 1/5`，0 次限购时它整件也关）
            if (!string.IsNullOrEmpty(c.Available))
                LabelFit(nb, R(g.Avail.x1, g.Avail.y1, g.Avail.x2, g.Avail.y2), c.Available,
                         NameColor, NAvail, v.TypeFs, 10f, qBase + QoText);

            // ---- `Badge`（根下，不在 `name-bg` 里）----
            {
                var br = BadgeRect(g);
                var bar = R(br.x1, br.y1, br.x2, br.y2);
                var badge = MenuDraw.Node(b.Root, NBadge, bar);
                MenuDraw.Nine(badge, CardArt.MenuUi(BadgeArt), bar, BadgeBorder, BadgeTexW, BadgeTexH,
                              qBase + QoBadge, BadgeTint, true, NBadge + " Gfx");
                if (!string.IsNullOrEmpty(c.BadgeText))
                {
                    var tr = g.BadgeText;
                    // 原版 `Text (TMP)`：**fs36 钉死（无 auto）· 折行=1 · Left/Top**
                    LabelFit(badge, R(tr.x1, tr.y1, tr.x2, tr.y2), c.BadgeText, NameColor, NBadgeText,
                             36f, 0f, qBase + QoText, true, true);
                }
            }

            // ---- `Timer`（根下）----
            {
                var tr = g.Timer;
                var timer = MenuDraw.Node(b.Root, NTimer, R(tr.x1, tr.y1, tr.x2, tr.y2));
                var ic = g.TimerIcon;
                var icr = R(ic.x1, ic.y1, ic.x2, ic.y2);
                float side = Mathf.Min(icr.W, icr.H);
                var icn = MenuDraw.Node(timer, NTimerIcon,
                                        new PxRect(icr.CX - side * 0.5f, icr.CY - side * 0.5f,
                                                   icr.CX + side * 0.5f, icr.CY + side * 0.5f));
                MenuDraw.Rect(icn, CardArt.MenuUi(ClockArt),
                              new PxRect(icr.CX - side * 0.5f, icr.CY - side * 0.5f,
                                         icr.CX + side * 0.5f, icr.CY + side * 0.5f),
                              "Icon Gfx", qBase + QoText, null, true);
                if (!string.IsNullOrEmpty(c.TimerText))
                {
                    var tt = g.TimerText;
                    // 原版 `Timer Text`：fs28 `auto[18,28]` · **折行=1** · Left/Midline
                    LabelFit(timer, R(tt.x1, tt.y1, tt.x2, tt.y2), c.TimerText, NameColor, NTimerText,
                             28f, 18f, qBase + QoText, true, true);
                }
            }

            // ---- `raycast target`：整根那么大的命中区（原版 `Image,EverguildButton` · `trans=1`）----
            // 🔴 原版点它走 `ShopOfferContainer.OnClick → OpenContainer`，**那一族我们没有** ⇒
            //    `onClick` 由调用方给。**给 null 就如实出声**（建的时候说一次 + 真点了再说一次）——
            //    红线「不许静默失败」+「点了必须有反应」，两条都要满足。
            if (onClick == null)
            {
                string who = v.Prefab;
                Debug.Log("[OfferContainer] `" + who + "`：整卡的点击入口**没有接**"
                          + "（原版 `ShopOfferContainer.OnClick → OpenContainer` 那族我们没有；"
                          + "而这一族的数据源是服务端 LiveOps 报价 ⇒ 本期只建骨架，见 `Shell/OfferContainer.cs` 文件头）");
                onClick = () => Debug.Log("[OfferContainer] `" + who + "` 被点了 —— 还是**没有入口**"
                                          + "（同上：这一族本期只建骨架，不编假入口）");
            }
            MenuDraw.Hit(b.Root, NRaycast, R(0f, 0f, g.W, g.H), qBase + QoHit, onClick);
            return b;
        }

        /// <summary>`Price Display Button` → `Generic UI Button` → `Price Display` → `text`。
        /// 🔴 `Price Display` 原版是个 `HorizontalLayoutGroup`（`icon` + `text`），而 `icon` 的图**出厂是空的**、
        /// `text` 量出来的宽是 **0**（布局没跑）⇒ 我们只建节点 + 一个 `text`，**按按钮整框居中**画（**我们挑的**）。</summary>
        static void BuildPrice(Transform nb, System.Func<float, float, float, float, PxRect> R, Geo g, Content c, int qBase)
        {
            var pr = g.Price;
            var node = MenuDraw.Node(nb, NPrice, R(pr.x1, pr.y1, pr.x2, pr.y2));
            var box = MenuDraw.Node(node, NPriceBtn, R(pr.x1, pr.y1, pr.x2, pr.y2));
            MenuDraw.Rect(box, CardArt.MenuUi(PriceArt), R(pr.x1, pr.y1, pr.x2, pr.y2), "Button Gfx",
                          qBase + QoText, ShopTabPage.PriceTint, true);
            var inner = MenuDraw.Node(box, NPriceBox, R(pr.x1, pr.y1, pr.x2, pr.y2));
            if (!string.IsNullOrEmpty(c.Price))
                LabelFit(inner, R(pr.x1, pr.y1, pr.x2, pr.y2), c.Price, NameColor, NPriceText, 38.15f, 13.46f,
                         qBase + QoText, false);
        }

        /// <summary>`WebShop Button Square Variant`。**底图有判据**（骨架行给了 `40K_button_square` 103×107），
        /// 而 `Highlight` 与 `Icon` 的 sprite 实读是「未解出」⇒ **判据空 ⇒ 不建**（出声）。
        /// 出厂那个空 `Image` 子件照建、**关着**。</summary>
        static void BuildWebShop(Transform nb, System.Func<float, float, float, float, PxRect> R, Geo g, int qBase)
        {
            var wr = g.WebShop;
            var node = MenuDraw.Node(nb, NWebShop, R(wr.x1, wr.y1, wr.x2, wr.y2));
            var n9 = MenuDraw.Nine(node, CardArt.MenuUi(WebShopArt), R(wr.x1, wr.y1, wr.x2, wr.y2),
                                   WebShopBorder, WebShopTexW, WebShopTexH, qBase + QoWebShop, null, true,
                                   NWebShopImage, WebShopBorder / WebShopPpuMul);
            var dummy = MenuDraw.Node(node, NWebShopGfx, R(g.WebShopDummy.x1, g.WebShopDummy.y1,
                                                          g.WebShopDummy.x2, g.WebShopDummy.y2));
            dummy.gameObject.SetActive(false);                      // 出厂 INACT（照抄）
            // 悬停/按下换图：原版这一颗的 `HL/P` 实读到名字（`HL=40K_button_square_hover` · `P=…_pressed`）
            var hit = MenuDraw.Hit(node, "Hit", R(wr.x1, wr.y1, wr.x2, wr.y2),
                                   qBase + QoWebShopHit, WebShopClick);
            var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
            if (wb != null && n9 != null) wb.BindNine(n9, WebShopArt, "40K_button_square_hover", "40K_button_square_pressed");
        }

        /// <summary>`WebShop` 那一颗点了干什么 —— **原版是打开网页商店**（`EverguildButton` 的
        /// `trans=2 target=<外部链接>`）。本地没有那个链接（`target` 是 PathID、内容查不到）⇒ **只出声**。</summary>
        static void WebShopClick()
        {
            Debug.Log("[OfferContainer] `WebShop Button Square Variant` 被打——原版是开网页商店"
                      + "（`EverguildButton.trans = 2` 的外链），**我们这里只出声、不跳转**（链接本体本地查不到）");
        }

        /// <summary>按原版那套 TMP 参数摆一段字（**逐项对着 dump 抄**）：
        /// `m_fontSize` 起手 · 有 `auto[最小,最大]` 的才开自适应（`minPx &gt; 0`）· 有 `折行=1` 的才限宽换行 · 左对齐。
        /// 🔴 **这两档是逐个节点实读的，别一律开**：`name`/`type`/`Available Counter`/`text` 是 **auto + 不折行**；
        /// `Badge/Text (TMP)` 是 **不 auto + 折行**（fs36 钉死）；`Timer Text` 是 **auto[18,28] + 折行**。
        /// 🔴 **顺序**：先 `SetAutoFitBox` 再对齐（对齐按**当前**文字宽算 —— 本工程踩过）。</summary>
        static Label LabelFit(Transform parent, PxRect r, string text, Color color, string name,
                              float fontPx, float minPx, int q, bool left = true, bool wrap = false)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var lb = MenuDraw.Text(parent, r, text, color, name, fontPx, q, wrap ? r.W : 0f);
            if (lb == null) return null;
            if (minPx > 0f && fontPx > minPx)
                lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), minPx, fontPx);
            if (left) MenuDraw.AlignLeft(lb, r);
            return lb;
        }

        /// <summary>19 份的清单（自检打印 + 诊断用）。</summary>
        public static string Dump()
        {
            int slots = 0, off = 0;
            for (int i = 0; i < Variants.Length; i++)
            {
                slots += Variants[i].Drawers != null ? Variants[i].Drawers.Length : 0;
                off += Variants[i].Off != null ? Variants[i].Off.Length : 0;
            }
            return "OfferContainer：变体 " + Variants.Length + " 个 · 抽屉槽 " + slots + " 个（其中出厂 INACT " + off + " 个）";
        }
    }
}
