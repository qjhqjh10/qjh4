// CardDetailPopup.cs — 卡组线最后一件：**卡片详情窗**（原版菜单版 `CardDisplayWindow`）
//
// ============================ 出处（唯一正本） ============================
// 🔴 **`资料/阶段二_卡片详情窗_原版规格.md`**（2026-09-24 建，逐节点表 + 三块面板 + 计数条 + 入口链 + 查不到的）。
//
// 两条**与直觉相反**的实证（详见正本 §〇）：
//   ① 这扇窗**不是 `bundle_menus_assets_all` 里的 prefab，是 `mainmenuwarpforge` 场景里的场景对象**
//      （出厂 `m_IsActive=false`），所以「按 prefab 找」是找不到的；
//   ② 🔴 **2026-10-12 就地订正（铁律 5）**：本行原来写「**菜单版 ≠ 战斗版**：菜单卡 523.25×832.75 px
//      （`CardUI (4)` scale 250），战斗 `CardUI Reference` scale 223.14 ⇒ **467×743**，工程里已有的
//      `Battle/CardDisplayWindow.cs` **是战斗那扇**（743px），**不能直接复用**」—— **两处都错**：
//      `CardUI Reference`（scale 223.14）**不属于这扇窗**，它是**多卡展示窗**的模板
//      （`Safe area FrontCanvas/Generic Multi Card Display Combat/Viewport/Content/CardUI Reference`）；
//      而**战斗那扇窗的 5 个卡槽同样是 scale 250、引的还是同一条 clip**
//      ⇒ **两扇窗的卡体是同一个尺寸 523.175×832.825 px**（正本 §〇·2 与 §十·4 的 2026-09-28 订正）。
//      ⇒ 「不能直接复用」这句**不成立**（正因为同源，卡格那几组数已收口到共用件 `Core/CardFan.cs`）。
//
// ============================ 结构（照正本 §一/§二/§三 抄）============================
//   · `Menu Dark Background` rect 比屏幕大（盖住任何画幅）· 色 **(0,0,0,.773)** → **点它关窗**
//     🔴 原版**全树没有关闭钮**（`Close*` 只命中 `BackgroundCloseButton`）⇒ 关 = 点遮罩 / ESC
//   · `Card Display` **584,106 → 1336,974**（752×868）· 卡本体在 `Cards/CardUI (4)/2DCard` = **523.25×832.75**
//   · `LowerSection` **300,921.34 → 1620,1058.34**
//     ├ `FlavourTextBG` 300,900.84→1620,1078.84（原版 `spr=0`、运行时由 `FlavourTextSO` 喂 ⇒ **本地没有**）
//     ├ `LoreText` 335,943.11→1585,1036.58 **fs35 hAlign=Right**
//     ├ `Voice Over Button` 1650,945.51→1738.66,1034.17（88.655²，`40k_UI_bt_voicelines`）
//     └ `Show Card Text` 181.34,945.51→270,1034.17（88.655²，`40k_UI_bt_eye`）
//   · `Panel`（VLG align4）**1294,146.37 → 1744,903.35**，三块 **x=[1294,1744] 宽 450**、y **146.71 / 411.12 / 673.42**
//     ├ `Crafting Panel`（创建副本）· `Upgrade Panel`（升级）· `Alternate Art Panel`（异画）
//   · `Card Counter` **825.07,829.81 → 1094.93,909.19**（三个 TMP：`x{n}` / `"/ "` / `{n}`）
//   · `Wildcard Segment`（0,71→1920,156）+ `WIldcard Counter`（1470,71→1870,156）
//
// ============================ 没建的 / 我们挑的（逐条出声）============================
//   · **`FlavourTextBG` 那张底图本地没有**（`sprite=0`，预设喂的是 `FlavourTextSO`）⇒ 我们只画字、不画底。
//   · **`Lore Text` 的内容**：原版是**背景故事**；我们引擎里**没有 lore 字段** ⇒ 这里画**卡面效果文字**
//     （= `项目任务.md` §三 第 13-E 条第 8 项那条老账的同一件事），**如实标**。
//   · **升级表数值是我们编的**（原版在服务端 `CardTierConfig`）—— 照实拍那个数据点
//     （等级 3 = 4 张 + 2000 + 500 锻造点）做梯度，见 `CardProgress`。**用户 2026-09-22 已裁决照办。**
//   · **不扣货币**：单机资源固定 9999（同上裁决）⇒ 货币那一格**永不动**。
//     🔴 **2026-10-12（A1699）就地订正**：本行原文写「⇒ 升级/创建副本只改**本地拥有数与等级**」——
//     **两块面板恢复后，那两颗钮本版都不改任何数**：**升级**钮 = `Interactable = false`（不可交互，
//     用户 2026-10-12 口径「默认最高级」）；**创建副本**钮 = 点了只出声、**什么都不做**
//     （见 `OnCraftClicked`；原口径下它走 `DoCraft` 加副本 —— 那一支**保留但没接线**）。
//     🆕 **2026-10-12（第二笔 · 用户同日第二条裁定）**：**「默认最高级」已落到取数口** ——
//     `CardProgress.Level` 的缺省 **1 → `MaxLevel`**（见那个方法的 doc），同一裁定下
//     「创建副本」那块面板的消费文案**已按红线缀上 `*` 标识本版未实现**（见 `BuildCrafting`）。
//   · `Debug Add cards`（开发用）· `Item Information Panel`(act=F) · `Game Mode Icon` 那类 **不建**。
//   · `Alternate Art Panel`：**本地只有 7 张督军异画**（2 种风格）⇒ 不是这张卡就**没有异画可切**
//     （如实说，不给别的督军换 —— 异画**绑死在那张卡上**，见 `CardArt.AltArt` 的注释）。
//     ✅ **2026-10-12（A1699）：左右两颗 `Select Art Button` 已建**（原来缺，见 `BuildAltArrow`）——
//     它们**照原版**在 `0..N` 档之间回绕（0 = 原画）、换的是**前台那张**的立绘
//     （`CardData.artOverride`，与收藏窗 Styles 页同一份表 `CollectionWindow.AltArtCards`）。
//     🔴 **2026-10-17（D17 = 账上 A856）**：原版 `AlternateArtPanel.Initialize` 在**没有异画风格**时
//     **整块 `GameObject.SetActive(false)`**（判据 = `decomp_full/AlternateArtPanel__Initialize.c:22-38`），
//     我们原来**不管有没有都画那一块** ⇒ 已改成「没有 ⇒ 整块关」（**节点留着、只关**，同原版；
//     判据与「为什么判据是数据不是贴图」→ `HasAltArtStyle` / `BuildAltArt` 的注释）。
//     ✅ **2026-10-17 订正（铁律 5）**：本行原来写「原版 `CardDisplayOptions` 在**同一个** `CardDisplayWindow`
//     prefab 上、战斗侧 `showOptions` 传 `DisplayCardEffects(...)` ⇒ **原版战斗侧可能出这块、我们是缺口**」
//     —— 🔴 **记反了**。逐字段现核：**13 个竞技场**的 `CardDisplayWindow.options` **全是 `{0,0}` = null**
//     （`informationPanel` / `tryShowResources` 同样 13/13 为 0），**只有菜单版有**
//     （`bundle_scenes_scenes_mainmenuwarpforge`）；而 `CardDisplayWindow__ShowCard.c:159` 那句守卫
//     （`if (options == null || …) goto …`）⇒ **战斗侧永远不出这一块**。`ShowBattleCard` 传的那个
//     `showOptions` 在战斗侧是**空转**（它真正切的是 `cardEffectsGroup`）。⇒ **我们那扇不建 = 与原版一致**
//     （**不是缺口**）；同族的眼睛钮恰好相反（原版战斗版也没有、**是我们多建了一颗** ⇒ 已删）。
//     判据全文 → `资料/普查产出_1017/W_B4_战斗详情窗.md`（+ 六条断言在 `Editor/BattleScene.cs` 的 §A860）。
using System.Collections.Generic;
using DG.Tweening;          // 换位那两条补间（`CardTween.ToPose` 返回 `Tween`，`OnComplete` 是它的扩展方法）
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>单机口径的卡牌进度（拥有数 / 等级）。**升级表数值是我们编的**，见文件头。</summary>
    public static class CardProgress
    {
        /// <summary>升到第 i 级**累计**要几张副本（下标 = 等级）。`MaxLevel = 4`。</summary>
        public static readonly int[] NeedCopies = { 0, 0, 2, 5, 9 };
        /// <summary>升到第 i 级要多少金币（**我们编的，不是原版**）。</summary>
        public static readonly int[] NeedGold = { 0, 0, 500, 1000, 2000 };
        /// <summary>升到第 i 级给多少锻造点（照实拍那个数据点做的梯度；**我们编的**）。</summary>
        public static readonly int[] Points = { 0, 0, 100, 250, 500 };
        public const int MaxLevel = 4;

        /// <summary>能放进卡组的张数上限（规则书：经典 **传说 ≤1**、普通/稀有/史诗 ≤2）。
        /// ⚠️ **不带卡型这一档 = 「非督军」档**（等价于 <c>DeckCap(rarity, null)</c>）。
        /// 一旦手上拿得到 <c>CardDef.Type</c>、而且那张卡可能是督军，就**必须**走下面那个重载 ——
        /// 🔴 判据 = 原版 `GetMaxCopiesInDeck` 那句 `if (cardType == 10) return 1;`（**在稀有度之前**）：
        /// `d:/2/tools/decomp_full/Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c:7-9`。
        /// 我们池子里 **28 位督军不是传说**（epic 15 / rare 13）⇒ 只看稀有度会把它们算成 2（A894）。</summary>
        public static int DeckCap(string rarity) { return DeckCap(rarity, null); }

        /// <summary>能放进卡组的张数上限 —— **带卡型那一档**（判据同 <see cref="DeckRules.CopyLimit(string,string)"/>）。
        /// 🔴 规则**只有一处实现**：转发给 `DeckRules.CopyLimit`（`RuleEngine/Core/DeckRules.cs`）；
        /// ⛔ 别在这里再写一份 `rarity == "legendary" ? 1 : 2`（本工程「两处写同一条规则 = 迟早不一致」）。</summary>
        public static int DeckCap(string rarity, string type) { return DeckRules.CopyLimit(rarity, type); }

        static readonly Dictionary<string, int> _owned = new Dictionary<string, int>();
        static readonly Dictionary<string, int> _lvl = new Dictionary<string, int>();

        /// <summary>拥有数。**口径「给足」**（用户 2026-09-22 裁决）：够 `卡组上限 + 升满所需`。
        /// <para>🔴 **2026-10-17（A894）卡型那一档的残留（如实标注，别当成已做）**：本函数**吃不到
        /// `CardDef.Type`**（缓存 `_owned` 按 id 存、`AddOwned` 那一路也没有卡型），所以这里的「卡组上限」
        /// 走的是**不带卡型**那一档 ⇒ **对 28 张非传说督军，`Owned` 会比「按督军档」多 1**（11 而不是 10）。
        /// 要真收口得让 `Owned/AddOwned/Spares/CanUpgrade` 这一族都带上卡型（= 要么改签名、
        /// 要么按 id 反查 `CardDef`）—— **不在 A894 白名单里，已记进
        /// `资料/普查产出_1017/W_B31_徽记与上限.md`**。⚠️ 但**印在卡面/卡片详情窗上的那个数
        /// （`min(拥有, 卡组上限)`）是对的** —— 它的**上限**那一项已经走 `DeckCap(rarity, type)`。</para></summary>
        public static int Owned(string id, string rarity)
        {
            int v;
            if (_owned.TryGetValue(id, out v)) return v;
            v = DeckCap(rarity) + (NeedCopies[MaxLevel] - NeedCopies[1]);
            _owned[id] = v;
            return v;
        }
        public static void AddOwned(string id, string rarity, int n)
        {
            _owned[id] = Owned(id, rarity) + n;
        }
        /// <summary>这张卡的等级 —— **等价于原版 `RawCardScript.get_CardTier()` 的 1 基写法**。
        /// 🔴 **缺省 = `MaxLevel`（2026-10-12 用户裁定「默认最高级」）。**
        ///
        /// <para>**改动前后**：缺省原来是 `1` ⇒ 详情窗的 `Upgrade Panel` 印成「升级到第 **2** 级」
        /// （`BuildUpgrade` 的 `Title` = `Min(Level + 1, MaxLevel)`），而卡框那一路早就**一律最高档**
        /// （`CardArt.TierOf` 恒返 `CardArt.MaxTier`，用户 2026-09-27 拍板「所有卡一律取最高档卡框」）
        /// ⇒ **同一个数在两处打架**，正是下面这条原版事实要消掉的东西。</para>
        ///
        /// <para>🔴 **原版里这两处本来就是【同一个数】**（本笔亲读判据）：
        /// 卡框走 `CardFramesSO.GetClanFrame(army, **tier**, cardType)`、
        /// 升级面板走 `CardTierConfig.GetNextTierInfo(**tier**, rarity)` ——
        /// 两个入参都取自同一句 `RawCardScript__get_CardTier(lVar7, 0)`
        /// （`d:/2/tools/decomp_full/UpgradePanel__CheckUpgradeRequirements.c:57-60`）；
        /// 我们这边把它拆成了 `CardArt.TierOf`（画框）与 <see cref="Level"/>（升级面板）两条路
        /// ⇒ **`Level` 必须跟着 `TierOf` 走**，不能各表一套。</para>
        ///
        /// <para>⚠️ **没有存档，也没有第二个写入点**：`_lvl` 是纯内存静态字典（`ResetForTest` 只清它）；
        /// 全仓唯一的 `SetLevel` 调用 = `DoUpgrade()`，而那颗钮自 2026-10-12 起
        /// `Interactable = false` ⇒ **本版实际永远取这个缺省值**。
        /// 连带后果（逐处现核，都被如实订正过）：`CanUpgrade` 恒假 ⇒
        /// `BoosterPackOpenWindow.ReadyForLevelUp`（开包那枚「可升级」角标）**由恒真变恒假**
        /// —— 那才是「默认最高级」该有的答案。</para></summary>
        public static int Level(string id) { int v; return _lvl.TryGetValue(id, out v) ? v : MaxLevel; }
        public static void SetLevel(string id, int l) { _lvl[id] = l; }

        /// <summary>多余副本数 = 拥有 − min(拥有, 卡组上限)（**原版 `CardCounterDisplay.Initialize` 的判据**）。</summary>
        public static int Spares(string id, string rarity)
        {
            int o = Owned(id, rarity), cap = DeckCap(rarity);
            return o - Mathf.Min(o, cap);
        }
        /// <summary>升到下一级够不够（**只吃多余副本**，同原版）。</summary>
        public static bool CanUpgrade(string id, string rarity)
        {
            int l = Level(id);
            return l < MaxLevel && Spares(id, rarity) >= NeedCopies[l + 1];
        }

        public static void ResetForTest() { _owned.Clear(); _lvl.Clear(); }
    }

    /// <summary>`CardDetailPopup : GameWindow` —— 点一张卡开的那扇详情窗（原版菜单版 `CardDisplayWindow`）。</summary>
    public class CardDetailPopup : GameWindow
    {
        // 层：**高于收藏窗那一整片**（它最高到 `CollectionWindow.QFltHit = 3043`）；
        //    低于 `Deck info Popup`(3120+) / `Deck Selection Popup`(3125+) / `PromptPopup`(3140+)
        //    —— 提示窗要能压在**所有**窗之上。
        //
        // 🔴 **2026-09-28 重排（原来画错了）**：原来是 `QCd = 3110` 而卡格在 `3009–3017`
        //    ⇒ **遮罩画在卡格【之上】⇒ 整叠卡被压暗了一半**（拿实拍量出来的：前台卡名峰值
        //    只有 131/255，而原版同一处是 232/255 —— 131 正好 = 白 ×(1−0.773) 在**线性空间**下
        //    编码回 sRGB；判据全文 → 正本 §十·5）。
        //    原版那棵树的**兄弟序**是 `Menu Dark Background`(0) → … → `Card Display`
        //    ⇒ **遮罩在最底下**。现在三段分明：
        //    **遮罩 3105 < 卡格 3106–3114 < 本窗的钮/字/面板 3115+**，整段仍在收藏窗与卡组编辑之上。
        // ✅ **2026-10-11（A252）可见性收窄**：这两行原是 2026-10-04（A47 接线批）**整行**放宽成 `public` 的；
        //   本件只留**本窗之外真被引用**的那一个 —— `QCdHit`（`Editor/CollectionScene.cs` 的卡片详情自检）。
        //   量法（可复跑）：脚本扫全工程 **301** 个 `.cs` 的限定名 `CardDetailPopup.<常量>`、**剔注释**、
        //   剔本文件；另核过两件会「看着对、其实编不过」的事：① 同文件里的另一个类 `CardProgress` **不**用它们；
        //   ② **没有任何类从本窗派生**（`class X : CardDetailPopup` 全 0）—— 所以「回 `const`」是安全的。
        const int QShade = 3105;            // 3105 整屏遮罩（`Menu Dark Background`）
        const int QCardStack = 3106;        // 3106 卡格那一叠（`Card Display`）
        const int QCd = 3115;               // 3115 本窗的钮 / 字 / 面板
        const int QCdRow = 3116;
        const int QCdText = 3117;
        public const int QCdHit = 3118;     // ✅ 留 `public`：`Editor/CollectionScene.cs` 的自检引用（1 处）
        // 🆕 **2026-10-12（A1699）`QCdIcon = 3119`**：只给**异画那两颗圆钮的箭头**用。
        //   为什么要单开一档：那两颗钮是**三层同心**（圆底盘 74.39² / 黄底 56.86×58.13 / 箭头同框），
        //   而本工程**透明件同渲染队列时「谁盖谁」不可控**（按到相机的 3D 距离排，见 `CLAUDE.md` §三
        //   与 `资料/已知的坑.md`）⇒ 三层必须**各占一个递增的队列**。
        //   同族的现成先例 = 收藏窗那两颗圆钮（`CollectionWindow.BuildStyleArrow`：圆底 `QPagePanel`
        //   < 黄底 `QPageRow` < 箭头 `QPageOverlay`，注释里记着「同队列会把箭头盖掉、而矩形断言全绿」）。
        //   ⚠️ 它**高于命中区那一档**（3118）是**有意的**：命中区那颗 quad 是 `alpha = 0` 的透明件
        //   （见本文件的 `Hit(...)`），画面的次序与它无关；`PointerLayer` 只按「注册成命中区的那些 quad」
        //   挑，所以下面那颗 `ArrowHit` 照样被点得到。
        const int QCdIcon = 3119;

        // ---- 几何（**两处摆放共用一份** → `CardWinBox`；本文件只留别名，值不再各写一套）----
        public static readonly Color ShadeColor = CardWinBox.ShadeColor;
        public const float CardL = CardWinBox.CardL, CardT = CardWinBox.CardT;
        public const float CardR = CardWinBox.CardR, CardB = CardWinBox.CardB;   // `Card Display` 752×868
        public const float LowerT = CardWinBox.LowerT, LowerB = CardWinBox.LowerB;
        public const float LowerL = CardWinBox.LowerL, LowerR = CardWinBox.LowerR;
        public const float LoreBgT = CardWinBox.LoreBgT, LoreBgB = CardWinBox.LoreBgB;
        public const float LoreL = CardWinBox.LoreL, LoreT = CardWinBox.LoreT;
        public const float LoreR = CardWinBox.LoreR, LoreB = CardWinBox.LoreB, LorePx = CardWinBox.LorePx;
        /// <summary>两个圆钮各 **88.655²**（`pivot` 在 LowerSection 内水平两端 ±734.33）。</summary>
        public const float BtnS = CardWinBox.BtnS;
        public const float VoiceCx = CardWinBox.VoiceCx, EyeCx = CardWinBox.EyeCx, BtnCy = CardWinBox.BtnCy;
        /// <summary>`Panel`（VLG align4）与三块面板的 y（**布局组跑之前的模板位按 ±1px 抄**，正本 §二）。</summary>
        public const float PanL = 1294f, PanR = 1744f, PanT = 146.37f, PanB = 903.35f;
        public const float CraftT = 146.71f, CraftB = 410.44f;
        public const float UpgT = 411.12f, UpgB = 672.74f;
        public const float AltT = 673.42f, AltB = 903.01f;
        /// <summary>🆕 **2026-10-12（A1699）异画面板那两颗换画圆钮**（`Select Art Button Left/Right`）。
        ///
        /// <para>🔴 **尺寸与位置取自【运行时实况 dump】**（`资料/原版实拍/menu_forge_1016/runtime_ui_dump_menu_forge.tsv:154,157`）——
        /// 场景 JSON 那条路**量不到**：`Alternate Art Panel` 整条父链的 `m_LocalScale = 0,0`
        /// （动画/隐藏态，`工具/menu_rect.py` 实测全零，正本 §六·3 也记了这一条）
        /// ⇒ 照铁律 4「**实况与解包字段冲突时以实况为准**」。
        /// 实读（`pos` 列 = `m_AnchoredPosition`，锚点/pivot 都是 `(0.5,0.5)` ⇒ 相对**面板中心**）：
        /// `Right` = `(171.9, 65.0)` · `Left` = **`(−177.1, 65.0)`** · 两者 `m_SizeDelta` 都是 `(74.4, 75.6)`。
        /// ⚠️ **那两个 x 不对称**（−177.1 / +171.9，差 4.2px）—— **原版就是这样，别「顺手对称化」**。
        /// 子件 `Background` / `Icon` 是 **stretch 锚点**（`(0.114,0.124)-(0.873,0.888)` + `sd (0.4,0.4)`、
        /// `pos (−0.1, 0.3)` ≈ 居中）⇒ 解出来 **56.86×58.13**，与收藏窗那两颗圆钮的
        /// `CollectionWindow.StyleArrowIconW/H = 56.86 / 58.13` **同值**（同一件预制体）。
        /// 贴图三层（实读同两行）：根节点自己 = `UI_Button_Round_background`
        /// （Simple + `preserveAspect`、sprite 237² ⇒ `PA=1` 内接画 74.39²）·
        /// 子件 `Background` = `40k_general_bt_yellow` · 子件 `Icon` = `40k_general_bt_arrow`。</para></summary>
        public const float AltBtnW = 74.39f, AltBtnH = 75.61f;
        public const float AltArrowW = 56.86f, AltArrowH = 58.13f;
        /// <summary>两颗钮的**中心**（屏 px；由本窗自己的 `Panel` 矩形 + 上面那两个原版 offset 解出）。
        /// 换算只有这一处 —— 下面的 `BuildAltArrow` 与将来任何断言都读它。</summary>
        public const float AltBtnCxL = (PanL + PanR) * 0.5f - 177.1f;   // 1341.9
        public const float AltBtnCxR = (PanL + PanR) * 0.5f + 171.9f;   // 1690.9
        public const float AltBtnCy = (AltT + AltB) * 0.5f - 65.0f;     // 723.215
        /// <summary>`Card Counter` 825.07,829.81→1094.93,909.19；`Duplicate Counter` 底 840,834.21→1080,904.79。</summary>
        public const float CcBgL = 840f, CcBgT = 834.21f, CcBgR = 1080f, CcBgB = 904.79f;
        public const float CcX1L = 869.41f, CcX1R = 942.69f, CcY1 = 846.61f, CcY2 = 917.19f, CcX1Px = 52.6f;
        public const float CcSlL = 942.69f, CcSlR = 970.71f, CcSlPx = 57f;
        public const float CcX2L = 970.71f, CcX2R = 1014.80f, CcX2T = 856.90f, CcX2B = 906.90f;
        public const float CcImL = 1014.80f, CcImR = 1056.74f, CcImT = 853f, CcImB = 903f;
        /// <summary>🔴 **2026-10-13（A404）**：三颗计数 TMP 的**自适应容器宽** = 原版那一颗的 `m_SizeDelta.x`
        /// （TMP 的折行/自适应容器就是这个 `sizeDelta`；判据 → `资料/阶段二_卡片详情窗_原版规格.md:90-92` 的 `sd`）。
        /// ⚠️ **「容器 = `m_SizeDelta.x`」只对【点锚点】的件成立** —— TMP 真读的是**解析后的 `rect.width`**
        /// （`ComputeMarginSize`），stretch 的件（anchors `(0,0)-(1,1)`、`sd = 0`）容器 = **父宽**。
        /// 本颗（`Duplicate Counter` 那三件）都是点锚点 ⇒ 这两个数同值；⚠️ `Single Counter` 那颗**不是**（见 `CcSgWrapX`）。
        /// ⚠️ 与上面那三个 rect **不是同一个数**：rect 由 `CcX1L/R` 等给出（原版坐标只抄到 0.01 ⇒ 宽 73.28），
        /// 而原版 `sd.x` 是 **73.283**（差 0.003px）—— 这里取**原版 `sd`**（喂 TMP 容器的是它，不是画 rect 那个）。</summary>
        public const float CcWrapX1 = 73.283f, CcWrapSl = 28.02f, CcWrapX2 = 44.09f;
        /// <summary>🔴 **2026-10-13（A574）**：三颗计数 TMP 的 **`m_fontSizeBase`**（= 自适应二分的起点；
        /// 与 `autoMinPx` / `wrapPx` **同量纲** = 画布 px）。逐颗现读原版 MB：
        /// `Counter` **36.0**（pid `1892`）· `Slash` **36.0**（pid `1848`）· `Duplicates text` **35.0**（pid `1823`）
        /// （`d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/`）。
        /// ⚠️ **三颗不是同一个数**（36 / 36 / 35）—— 铁律 5·c：别拿一个顶其余的。
        /// ⛔ 不传（= 原来那样）⇒ base 落到「调用方那一档」的标称字号（52.6 / 57 / 52.6）；
        /// 它**只影响二分起点**、渲染差 ≤ 0.05 fontSize 单位（`TextMeshPro.cs:2148-2149` 起手 `Clamp(base,min,max)`），
        /// **但确实不是 0**，且原版值现成 —— `Label.SetAutoFitBox` 的 `basePx` 参数就是为它加的（A305 ①）。</summary>
        public const float CcBaseX1 = 36f, CcBaseSl = 36f, CcBaseX2 = 35f;
        /// <summary>🔴 **2026-10-13（A691）**：`spares == 0` 那一支走的**另一棵树** ——
        /// `Card Counter/Single Counter`（**170×50**，`m_AnchoredPosition = (0,0)` ⇒ **居中**在 `Card Counter` 里）。
        /// 判据 = **本笔现读原版 prefab**（`bundle_scenes_scenes_mainmenuwarpforge`）：
        /// `RectTransform_1531`（GO `Single Counter`，`m_IsActive = **False**` = 出厂关着）·
        /// anchors `(0.5,0.5)-(0.5,0.5)` · pos `(0,0)` · **`m_SizeDelta = (170, 50)`** · pivot `(0.5,0.5)` · scale 1 · rot 0；
        /// 父链 = `RectTransform_1596`（`Card Counter`，`m_SizeDelta = (269.857, 79.37)`、pos `(0,−329.5)`）。
        /// ⇒ 绝对矩形 `875, 844.50 → 1045, 894.50`（`UguiRect.Child` 复算；与正本 §三 末行同值）。
        /// ⚠️ **它和 `CcBgL/T/R/B` 那一套（=`Duplicate Counter`，240×70.586）不是同一棵树** ——
        /// 本笔之前我们只把 `Duplicate Counter` **改了个名**，几何/字号/结构全部沿用 ⇒ 两棵树被当成一棵。</summary>
        public const float CcSgL = 875f, CcSgT = 844.5f, CcSgR = 1045f, CcSgB = 894.5f;
        /// <summary>🔴 **2026-10-13（A691）**：`Single Counter/Counter` 那颗 TMP 的**逐字段原版值**
        /// （本笔现读 MB `pid 1893` ← `CardCounterDisplay.singleCardCounter`，其 GO = `Counter_227` → `RectTransform_1151`）：
        /// `m_fontSize = **35.0**` · `m_fontSizeMin = **25.0**` · `m_fontSizeMax = **35.0**` · `m_fontSizeBase = **36.0**` ·
        /// `m_enableAutoSizing = 1` · `m_HorizontalAlignment = **2**(Center)` · `m_TextWrappingMode = **0**` · `m_text = "x2"`。
        /// ⚠️ `CcSgWrapX` = **容器宽**：那颗 TMP 的 RT（`1151`）是 **stretch**（anchors `(0,0)-(1,1)`、`sd (0,0)`）
        /// ⇒ 它的 TMP 容器 = **解析后的 170**、⛔ **不是 `m_SizeDelta.x = 0`**
        /// （同族提醒见 `CcWrapX1` 的注释：那一族的 `sd.x` 才是容器宽 —— **两支的读法不同，别互相套**）。
        /// ⚠️ **三颗 `Duplicate Counter` 的值一个都不能套过来**（35 ≠ 52.6 · 25 ≠ 18 · 容器 170 ≠ 73.283/44.09）。</summary>
        public const float CcSgPx = 35f, CcSgMin = 25f, CcSgMax = 35f, CcSgBase = 36f, CcSgWrapX = 170f;
        /// <summary>通配符条：`Segment` 0,71→1920,156 · 底 1550,91.5→1870,135.5 · 四组 30×44 图标 + 41×44 数字。</summary>
        public const float WcBgL = 1550f, WcBgT = 91.5f, WcBgR = 1870f, WcBgB = 135.5f;
        public const float WcStep = 75f, WcX0 = 1565f, WcPx = 32.6f;

        static readonly Vector4 RedBorder = new Vector4(42f, 363f, 655f, 81f);
        const float RedTexW = 1100f, RedTexH = 701f;
        const float BtnTexW = 410f, BtnTexH = 124f;

        /// <summary>正在展示的卡（`CardData` 是 struct ⇒ 另用一个 bool 判「有没有」）。</summary>
        public CardDef Card;
        public bool HasCard;

        // ============================================================ 卡片那一叠（1 主卡 + 8 相关卡）
        //
        // 🔴 **扇形位姿 / 分层 / 着色那三组数，判据只此一份 = `Core/CardFan.cs`**（2026-09-28 收口）。
        //    为什么：**原版是同一个脚本 `CardDisplayWindow`、两处摆放**（菜单版 = 本窗，
        //    战斗版 = `Battle/CardDisplayWindow.cs`），两组数两边都要用 ⇒ 写两份迟早不一致。
        //    真值来源（原版那条 legacy clip `Card Display Open`）、槽 5–8 的外推规则、
        //    分层为什么要「每格一个队列」、相关卡为什么要压暗到 0.65 —— **全在那个文件的注释里**。
        //    ⚠️ 顺带订正一处：本文件 2026-09-27 记的槽 3 转角 **12.648°** 是错的，clip 原文复算是 **12.750°**。

        /// <summary>卡位个数：**1 主卡 + 8 相关卡**（原版是 1+4；**8 这一档是我们挑的**，用户授权的冗余）。</summary>
        public const int Slots = CardFan.Slots;
        /// <summary>换位动画时长 —— 原版 `CardDisplayWindow.relatedCardSwapTime`（字段 `+0xC0`）。</summary>
        public const float SwapTime = CardFan.SwapTime;
        /// <summary>槽名（照原版命名：**前台那张不带后缀**，其余 `CardUI (1..8)`）。</summary>
        public static string SlotName(int i) { return CardFan.SlotName(i); }
        /// <summary>命中区所在的 z 阶梯（**只给命中区用** —— 它是 alpha=0 的透明件，不影响画面；
        /// `PointerLayer` 同队列时**取 z 小的那个**，所以前台那张要最小）。</summary>
        const float HitZStep = 0.08f;

        // 🔴 **「把整格卡搬到同一个队列」那段（含「为什么必须走 `.material`」那个坑）已收口到
        //    `CardFan.SetCardQueue`** —— 两扇窗共用一份。卡格的队列基数见 `QCardStack`（本窗）。

        /// <summary>每个位姿槽上的卡（下标 = **位姿槽**，0 = 前台位）。</summary>
        public readonly CardView[] SlotViews = new CardView[Slots];
        readonly CardDef[] _slotDefs = new CardDef[Slots];
        bool _swapping;

        /// <summary>位姿槽上有几张卡（= 1 + 相关卡数，最多 `Slots`）。</summary>
        public int SlotCount { get; private set; }
        /// <summary>**前台那张**（永远是位姿槽 0）。原版 `CardSwapFinished` 之后 lore / 语音认的就是它。</summary>
        public CardDef FrontDef { get { return _slotDefs[0]; } }
        /// <summary>换位动画在播没有（= 原版那个 `swappingCards` 闸）。</summary>
        public bool IsSwapping { get { return _swapping; } }
        /// <summary>位姿槽 `i` 上的那张卡（越界返回 null）。</summary>
        public CardView SlotView(int i) { return (i >= 0 && i < Slots) ? SlotViews[i] : null; }
        /// <summary>位姿槽 `i` 上那张卡的卡表项（越界返回 null）。</summary>
        public CardDef SlotDef(int i) { return (i >= 0 && i < Slots) ? _slotDefs[i] : null; }
        public int SlotIndexOf(string cardId)
        {
            for (int i = 0; i < Slots; i++) if (_slotDefs[i] != null && _slotDefs[i].Id == cardId) return i;
            return -1;
        }

        /// <summary>效果文字条（`LowerSection/FlavourTextBG` + `LoreText`）现在露着没有（原版 `Show Card Text` 切它）。</summary>
        public bool LoreVisible = true;
        /// <summary>下缘那两颗钮的**整组节点**（图 + 命中区一起开/关）—— 建不建由卡决定，
        /// 判据只此一份 → `CardButtons`（原版 `SetCardVoiceOver` / `showCardTextButton`）。</summary>
        Transform _voiceBox, _eyeBox;
        /// <summary>最近一次开的（自检用）。</summary>
        public static CardDetailPopup LastOpened;
        /// <summary>最近一次播的语音文件（自检用）。</summary>
        public string LastVoiceFile { get; private set; }

        public Transform ShadeHit { get { return Find(transform, "BackgroundHit"); } }
        public Transform VoiceHit { get { return Find(transform, "VoiceHit"); } }
        public Transform EyeHit { get { return Find(transform, "ShowTextHit"); } }
        public Transform CraftHit { get { return Find(transform, "CraftHit"); } }
        public Transform UpgradeHit { get { return Find(transform, "UpgradeHit"); } }
        /// <summary>🆕 异画面板那两颗换画圆钮的命中区（自检用；`side` = `"Left"` / `"Right"`）。
        /// ⚠️ 名字**必须带左右** —— 两颗各有一个，同名的话 `Find` 只会拿到第一颗。</summary>
        public Transform AltArrowHit(string side) { return Find(transform, "ArrowHit " + side); }
        /// <summary>🆕 异画面板那颗价签的命中区（自检用）。</summary>
        public Transform BuyHit { get { return Find(transform, "BuyHit"); } }

        // ============================================================ 🆕 异画当前档（A1699）
        //
        // 原版字段 = `AlternateArtPanel.currentStateIndex`（`dump.cs` 的 `+0x50`）——
        // **0 = 原画，1..N = 这张卡的第 N 种异画**（不是 0..N-1！）。
        // 判据（`d:/2/tools/decomp_full/`，本笔逐字读过）：
        //   · `AlternateArtPanel__SwapStyle.c:25` `*piVar1 += (isLeft ^ 1) * 2 − 1`
        //     ⇒ **left(+1) → −1 · right(0) → +1**；
        //   · `:30` `index < 0 ⇒ index = list.Count`、`:36` `Count < index ⇒ index = 0` ⇒ **回绕**；
        //   · `:47` `if (index == 0)` 取 `+0x58`（= `nonAlternateArtCard` = **原画**）、
        //     `:60` 否则取 `list[index − 1]`（**1 基**，`lVar9 = iVar2 + (−1)`）——
        //     这就是「0 = 原画」的铁证；
        //   · `AlternateArtPanel__Initialize.c:82-95` 扫 `list` 找「当前这张卡的风格」并把它写成
        //     **`下标 + 1`** ⇒ 起来的时候落在**这张卡真正在用的那种异画**上。

        /// <summary>当前选的异画档：**0 = 原画 · 1..N = 这张卡的第 N 种异画**（原版 `currentStateIndex`）。
        /// 换卡（`ShowCard`）时归零（原版每开一次窗重新 `Initialize`）。</summary>
        public int AltStyleIndex { get; private set; }

        /// <summary>这张卡**有几种异画**（= `AltStylesOf(Card.Id).Count`；自检用）。
        /// ⚠️ 与「贴图加载出来没有」无关 —— 判据同 <see cref="HasAltArtStyle"/>。</summary>
        public int AltStyleCount { get { return AltStylesOf(Card != null ? Card.Id : null).Count; } }

        public Transform Counter { get { return Find(transform, "Card Counter"); } }
        /// <summary>升级面板的 `No Upgrade Warning`（满级时才露）。</summary>
        public Transform NoUpgradeWarn { get { return Find(transform, "No Upgrade Warning"); } }
        /// <summary>三块面板的 `Title` 文本（自检用）。</summary>
        public string TitleOf(string which) { var t = Find(transform, which + " Title"); return t != null ? TextOf(t) : "(无)"; }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        static string TextOf(Transform t)
        {
            var l = t != null ? t.GetComponent<Label>() : null;
            return l != null ? (l.Text ?? "") : "";
        }

        public static CardDetailPopup Create(WindowsManager mgr)
        {
            var go = new GameObject("Card Displayer Menu For Menu");
            var win = go.AddComponent<CardDetailPopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            // ⚠️ 原版 MB 里 `windowsPlacement = 0 (None)`，但它的**实际父节点是 `3 - PopUp Holder`**
            //    （= 我们的 `WindowsPlacement.Popup`）⇒ 我们按**实际父链**挂，**不按那个字段**
            //    （同 `资料/阶段二_战斗入口_原版规格.md` 开头那条「别按 placement 去猜它挂哪」）。
            win.placement = WindowsPlacement.Popup;
            win.closeOnEsc = true;                       // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>开/换一张卡。**复用同一个窗**（原版 `ShowCard` 里 `WindowsManager.OpenWindow(this)` 就是这个意思）。
        ///
        /// <para>🔴 **2026-10-12（A217②）就地订正（铁律 5）**：本方法原来**靠「同窗再开 ⇒ 重建内容」换卡**
        /// （`Manager.OpenWindow(this)` → `GameWindow.TryOpen` → `Open()` → `Build()`），
        /// 并在 `WindowsManager` 里如实标注过这条偏离（"换了会静默坏"）。
        /// A217② 把 `TryOpen` 按原版改成**按 `CurrentState` 分三档**（`Open` 支**一个字段都不写就 return**）
        /// ⇒ **再开不重建**（照原版），所以「换卡」必须**自己显式来一次** —— 下面那句 `Build()` 就是它。
        /// 这与原版同构：原版靠的是**窗自己 override 的 `TryOpen`/`Open` 里那一段刷新**
        /// （`decomp_full/…/CardDisplayWindow__ShowCard.c` 那条链每一扇自己要刷的窗都要有这一层）。</para>
        ///
        /// <para>⚠️ **两处如实标注（铁律 3）**：
        /// ① **首开那一次会 `Build()` 两遍**（`OpenWindow` → `TryOpen` 的 `Closed` 支 → `Open()` → `Build()`，
        ///    回来再走下面这一句）—— **有意如此**：按 `CurrentState` 去猜「这一次会不会重建」
        ///    就等于把判据抄成第二份，而 `TryOpen` 里万一早退了，换卡会**静默失败**。
        ///    `Build()` 是**幂等**的（首句把窗根的子件全销毁重建），代价 = 一屏 9 张卡面建两遍（一帧内、无动画）。
        /// ② **`Build()` 必须排在 `OpenWindow` 【之后】**：`Build()` 里那些 `MenuDraw.Text` / TMP 用法
        ///    要窗**已激活**（批处理与运行时同理：未激活对象上量不出 TMP 尺寸 —— 见 `CLAUDE.md` §三那条）。
        ///    窗在 `ShowCard` 之前一定是关着的（新建的那一扇 `CurrentState = Closed`）⇒ 先开再建。</para></summary>
        public void ShowCard(CardDef card)
        {
            Card = card;
            HasCard = card != null;
            LoreVisible = true;                 // 原版每次 `ShowCard` 都把 `loreObjectBG` 复位
            // 🆕 A1699：异画也复位回**原画**（原版每开一次窗都重跑 `AlternateArtPanel.Initialize`，
            // 而 `Initialize` 把 `currentStateIndex` 从 0 起算、再扫出「这张卡在用的那种」）。
            // ⚠️ 我们**不照抄「落在该卡在用的那种」那一步**：本地 7 张异画**都是未装备的收藏品**
            //（原版那一步读的是服务端「当前装备的卡」）⇒ 起来一律是**原画**，如实标注。
            AltStyleIndex = 0;
            if (Manager != null)
            {
                Manager.OpenWindow(this);       // 还开着就复用同一扇（原版 `WindowsManager.OpenWindow(this)`）
                Build();                        // ⟵ 🔴 换卡靠这一句，⛔ 不是靠「再开一次会重建」
                LastOpened = this;
                return;
            }
            // 没有 `WindowsManager`（自检里可能）⇒ 照旧自给自足（`Build()` 前先激活，理由同 ②）
            gameObject.SetActive(true);
            Build();
            LastOpened = this;
        }

        public override void Open() { Build(); }

        // ============================================================ 建

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(root.GetChild(i).gameObject);
            _voiceBox = null; _eyeBox = null;      // 旧的那两颗随子件一起没了 ⇒ 引用也要清
            if (!HasCard) return;

            // 1) 压暗 + 点遮罩关窗（原版唯一的「关」）
            //    🔴 队列 `QShade`（**3105**）—— **必须低于卡格**（`QCardStack = 3106`）：
            //    原版那棵树里 `Menu Dark Background` 是**第一个孩子**（画在最下），卡在它上面。
            MenuDraw.Rect(root, CardArt.Solid(),
                          new PxRect(CardWinBox.MaskL, CardWinBox.MaskT, CardWinBox.MaskR, CardWinBox.MaskB),
                          "Menu Dark Background", QShade, ShadeColor);
            // 🔴 **2026-10-04（A47 接线批）订正档号：`QCdHit − 1`(3117) → `QShade`(3105)** ——
            //   规矩 = 「压暗层的命中区落在**压暗层自己那一档**，且严格低于本窗任何内容命中区档」
            //   （判据 → `MenuDraw.ShadeHit` 的注释 · `资料/待办判据_阶段二与联机.md` §A25·补（一））。
            //   ⚠️ 3117 恰好是 `QCdText`（**文字那一档**）——「内容档 − 1」看着像派生，其实落在别的层上。
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QShade, QCdHit, () => Close(), "BackgroundHit");

            // 2) 卡片那一叠（1 主卡 + 8 相关卡 · 扇形）—— 判据与真值 → `CardFan`
            var cardNode = MenuDraw.Node(root, "Card Display", new PxRect(CardL, CardT, CardR, CardB));
            BuildCardStack(cardNode);

            // 3) `LowerSection`：效果文字条 + 语音钮 + 「显示卡面文字」钮
            var lower = MenuDraw.Node(root, "LowerSection", new PxRect(LowerL, LowerT, LowerR, LowerB));
            // ⚠️ `FlavourTextBG` 那张底图**本地有**（13 张按阵营的 `40K_display_Flavortext <阵营>.png`，
            //    工程里还没导 —— §三 第 22 条 第 2 项）⇒ **现在只画字**
            var bg = MenuDraw.Node(lower, "FlavourTextBG", new PxRect(LowerL, LoreBgT, LowerR, LoreBgB));
            BuildLoreText(bg);

            // 两颗钮：**整组一个节点**（图 + 命中区一起开/关）—— 建不建由卡决定，见 `RefreshButtons`
            float vx = VoiceCx - BtnS * 0.5f, ex = EyeCx - BtnS * 0.5f, vy = BtnCy - BtnS * 0.5f;
            _voiceBox = MenuDraw.Node(lower, "Voice Over Button", new PxRect(vx, vy, vx + BtnS, vy + BtnS));
            var voiceQ = MenuDraw.Rect(_voiceBox, CardArt.Ui("40k_UI_bt_voicelines"),
                          new PxRect(vx, vy, vx + BtnS, vy + BtnS), "Image", QCdRow, null, true);
            // 🆕 A17：原版 `Card Displayer Menu For Menu>…>Voice Over Button` 是 SpriteSwap
            //（高亮 `40k_UI_bt_voicelines_hover`，普查 §块 1 那两条 ❌ 之一）
            Hit(_voiceBox, "VoiceHit", new PxRect(vx, vy, vx + BtnS, vy + BtnS), () => PlayVoice(), QCdHit,
                voiceQ, "40k_UI_bt_voicelines");
            _eyeBox = MenuDraw.Node(lower, "Show Card Text", new PxRect(ex, vy, ex + BtnS, vy + BtnS));
            // 🔴 **2026-10-03 就地更正**：这里**不变**常态图 —— 直接读原版 prefab
            //（`bundle_scenes_scenes_mainmenuwarpforge` → `Card Displayer Menu For Menu`）：
            // **`Show Card Text` = `40k_UI_bt_eye`（256×256）· HL = `40k_UI_bt_eye_hover` · P = `40k_UI_bt_eye_pressed`**
            // ⇒ 我们画的这张**本来就是对的**（普查 §块 1 第 14 行同值），只是**一直没接换图**。
            // ⚠️ 别和 **`40k_bt_eye`（88×87，另一张）** 混 —— 那张是 `LiveOpsEventWindow` 卡组四圆钮用的。
            var eyeQ = MenuDraw.Rect(_eyeBox, CardArt.Ui("40k_UI_bt_eye"),
                          new PxRect(ex, vy, ex + BtnS, vy + BtnS), "Image", QCdRow, null, true);
            Hit(_eyeBox, "ShowTextHit", new PxRect(ex, vy, ex + BtnS, vy + BtnS), ToggleLore, QCdHit,
                eyeQ, "40k_UI_bt_eye");
            RefreshLore();
            RefreshButtons();
            if (CardArt.Ui("40k_UI_bt_voicelines") == null)
                Debug.Log("[CardDetail] `40k_UI_bt_voicelines` 不在 `Resources/Art/ui/` ⇒ **语音钮画不出来**（不摆白方块）");

            // 4) 三块面板
            var panel = MenuDraw.Node(root, "Card Options Panel", new PxRect(0f, 0f, 1920f, 1080f));
            var vlg = MenuDraw.Node(panel, "Panel", new PxRect(PanL, PanT, PanR, PanB));
            // ============================================================ 🔴 2026-10-12（A1699）：两块面板**恢复**
            //
            // **用户原话（本笔是照他下的命令做的）**：「「创建副本 / 升级」两块面板：维持你 9-27 的停用，
            //   还是恢复？**请你恢复**。我说的是不需要升级卡牌系统，**默认最高级就是不能点击升级卡牌按钮**。」
            // ⇒ ① **UI 都恢复**（照原版版式）；② **「升级」那颗钮做成【不可交互】**
            //    （**不是**把面板删掉 —— 见 `BuildUpgrade` 末尾那一句，以及它引的原版判据）；
            // ③ 「创建副本」那颗钮**点下去出声**（**本版不实现真机制**，见 `OnCraftClicked`）。
            //
            // 本行原来是一条 `Debug.Log`（"本作不做升级/合成 ⇒ 两块面板都不建"），2026-09-27 用户拍板的
            //   那条口径**已被 2026-10-12 这句取代**。旧口径的两条理由（`CardArt.TierOf` 一律最高档、
            //   本作全解锁）**仍然成立**，但它们现在只解释「钮为什么点不动」，不再解释「面板为什么不在」。
            //   📌 `Craftable()` / `WildcardIconFor()` 这两个判据（上一轮为「创建副本」做的）**现在有调用点了**。
            //   ⚠️ 顺序照原版 `Panel` 那个 VLG 的次序（创建副本 146.71 → 升级 411.12 → 异画 673.42）；
            //   我们按绝对矩形摆、不吃布局组，所以次序只影响 `Find` 的先后与生成的兄弟序。
            if (Craftable(Card.Rarity)) BuildCrafting(vlg);
            else Debug.Log("[CardDetail] 这张卡的稀有度是「" + Card.Rarity + "」⇒ **「创建副本」那块整块不建**"
                         + "（判据 = `Craftable`，等价于原版 `CraftingPanel.CheckButtons` 里"
                         + " `CheckForCardCrafting` 给出空价 ⇒ `gameObject.SetActive(0)`，见 `Craftable` 的注释）");
            BuildUpgrade(vlg);
            BuildAltArt(vlg);
            BuildCounter(panel);
            BuildWildcards(panel);
        }

        // ============================================================ 卡片那一叠（扇形 + 相关卡）

        // 🔴 **命中区的判据只此一份 → `CardFan`**（原版 `CardUI/2DCard/UI Collider` 的
        //    `m_SizeDelta(-0.2,-0.44)` + `m_AnchoredPosition.y=-0.02` ⇒ **两轴比例不同 + 中心下移 5px**）。
        //    ⚠️ **不要再在本文件里放一个 `HitRatio` 常量** —— 2026-10-07 之前那只 `0.9043` 是 **x** 那个比，
        //    双轴同用 + 居中 ⇒ y 轴多吃两条窄带（A156）。用 `CardFan.HitW/HitH/HitCy`。

        /// <summary>把「1 主卡 + N 相关卡」按**原版那条 clip 的扇形**摆出来（判据与真值 → `CardFan`）。
        /// ⚠️ 位姿槽 0 = 前台（原版 `cardUIs[0]`：屏心 (960,480) · scale 250 · 转角 0）。</summary>
        void BuildCardStack(Transform cardNode)
        {
            var cards = new List<CardDef>();
            if (Card != null) cards.Add(Card);
            // 相关卡：判据只此一份（`RelatedCards.Find`）—— 与原版那 4 个 `relatedCard1..4` 字段的关系见那个文件头
            cards.AddRange(RelatedCards.Find(Card, Slots - 1));
            SlotCount = Mathf.Min(cards.Count, Slots);
            for (int i = 0; i < Slots; i++)
            {
                SlotViews[i] = null; _slotDefs[i] = null;
                if (i >= SlotCount) continue;
                var def = cards[i];
                float w = CardFan.Wpx(i), h = CardFan.Hpx(i);
                float cx = CardFan.Cx(i), cy = CardFan.Cy(i);           // 屏坐标（左上原点；原版 pos.y 向上 ⇒ 取负）
                var px = new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
                var node = MenuDraw.Node(cardNode, SlotName(i), px);

                // 🔴 **命中区【先建】、且【不看 `CardView.Create` 的返回值】**（A157，2026-10-07）：
                //   原来 `if (v == null) { …; continue; }` 排在下面 ⇒ `CardView.Create` 失败的那一格
                //   **连命中区一起没有**，点上去会穿透它落到压暗层 `BackgroundHit` 上**把窗关掉**。
                //   原版每一格都挂自己的 `UI Collider`（吃射线的是它，不是卡面那些图）——
                //   **「这一格点不点得到」与「这一格的卡面画不画得出来」是两件事**：
                //   卡面画不出来时，玩家的那一下仍然该被这一格吃掉（回调 `SwapToFront` 自己会闸掉），
                //   ⛔ 不能让一次渲染失败变成「点卡片 = 关窗」。⇒ 命中区与 `CardView` 解耦。
                // ⚠️ 卡面那张建不出来时**照旧出声**（`LogWarning`，不静默）—— 见下面那一支。
                BuildCardHit(node, i);

                var cd = BattleDriver.ToCardData(def, def.Faction);
                // 🆕 **2026-10-12（A1699）异画预览：前台那张吃 `CardData.artOverride`**。
                //   原版 = `AlternateArtPanel__SetStyle.c:47-52` 的 `BasicCardUI.SetCardImage(前台那张, 该风格图)`
                //   —— `currentCardUI`（`+0x60`）就是 `CardDisplayOptions.Initialize` 传进来的那张（= `cardUIs[0]`）。
                //   🔴 **口径 = 与另外 4 处【逐字相同】的那一个**：`CardData.artOverride` 非空 ⇒
                //   `CardView.ArtTexture` 用它、不再按 `artId` 取（`Core/CardView.cs:5574`），
                //   抠图清单也跟着换成异画那份（`:1911`）；另两处消费点 = `Battle/BattleDriver.cs:4766`
                //   （进攻卡整张卡面用它）与收藏窗 Styles 页 `Shell/CollectionWindow.cs:1756-1757`。
                //   ⚠️ **必须在 `CardView.Create` 【之前】设**（`Create` 那一趟就按它建了立绘层；
                //   口径同 Styles 页那条注释「`d.artOverride` 是在 `Create` 之前设好的」）。
                if (i == 0)
                {
                    var alt = AltArtTexture();
                    if (alt != null) cd.artOverride = alt;
                }
                var v = CardView.Create(node, cd, SlotName(i));
                if (v == null)
                {
                    Debug.LogWarning("[CardDetail] 第 " + i + " 格（" + def.Name + "）的 `CardView` 建不出来"
                                   + " —— 那一格**没画卡面，但命中区照样在**（点它不会关窗，出声）");
                    continue;
                }
                v.gameObject.SetActive(true);
                v.SetPose(MenuDraw.Local(node, px.x1, px.y1, px.x2, px.y2), CardFan.Rot(i),
                          h / (CardView.Height * 108f));
                v.SetData(cd);
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);
                // **前台白 / 相关卡压暗到 0.65**（原版 `cardInBackGroundColorTint`）——
                // ⚠️ **必须排在 `SetHighlight` 之后**（那个自己也写 `SetTint`，谁后调谁生效）。
                CardFan.ApplySlotTint(v, i == 0);
                SlotViews[i] = v; _slotDefs[i] = def;
            }
            // 🔴 **每格一个独立的渲染队列**（越靠前台越高）—— 判据与坑（为什么必须走 `.material`）→ `CardFan`。
            //    ⚠️ **这一段必须落在【遮罩之上】**（`QShade = 3105`）：原来卡格在 `3009–3017`、遮罩在 `3110`
            //    ⇒ **整叠卡被压暗了一半**（2026-09-28 实拍量出来的，见本文件头部那段订正）。
            CardFan.ApplyQueues(SlotViews, SlotCount, QCardStack);
            _swapping = false;
            Debug.Log($"[CardDetail] 卡片那一叠：**{SlotCount} 格**（1 主卡 + {Mathf.Max(0, SlotCount - 1)} 相关卡）"
                    + " —— 扇形：槽 0–4 照原版 clip `Card Display Open`，槽 5–8 是**我们外推的**（正本 §8·7）");
        }

        /// <summary>第 `i` 格的点击区（原版 `CardUI/2DCard/UI Collider` —— **每格一个**），
        /// 矩形判据只此一份 → `CardFan.HitW/HitH/HitCy`。
        /// **前台那格也要**：它的回调在闸②直接 return，作用是「吃掉这一下」，
        /// 否则会落到 `BackgroundHit` 上**把窗关掉**；<b>与卡面有没有画出来无关</b>（A157）—— 见调用点的注释。
        /// 🔴 z 阶梯：`PointerLayer.HitButton` 同队列时**取 z 小的那个** ⇒ 越靠前台 z 越小。</summary>
        void BuildCardHit(Transform node, int i)
        {
            float hy = CardFan.HitCy(i);                       // 点击区中心**比卡心低**（原版 `ap.y = −0.02`）
            float hw = CardFan.HitW(i) * 0.5f, hh = CardFan.HitH(i) * 0.5f;
            float fx = CardFan.Cx(i);
            var hx = new PxRect(fx - hw, hy - hh, fx + hw, hy + hh);
            var hit = Hit(node, "CardHit " + i, hx, () => SwapToFront(i));
            if (hit != null)
                hit.localPosition = new Vector3(hit.localPosition.x, hit.localPosition.y, i * HitZStep);
        }

        // 🔴 **「相关卡是谁」那段（点名那一支 + 池子那一支）已收口到 `Core/RelatedCards.cs`**
        //    —— 两扇窗共用一份。那里也写着**原版其实走的是卡上自带的 `relatedCard1..4` 四个字段**
        //    （值在服务端，本地拿不到 ⇒ 我们这套是替身），判据全文 → 正本 §十·1。

        /// <summary>效果文字条那行字。原版这里是 **Lore 背景故事**；我们没有 lore 字段 ⇒ 画**效果文字**（出声）。
        /// ⚠️ 换位收尾（原版 `CardSwapFinished` 的 `SetCardLore(新前台)`）会**重建**它 ⇒ 抽成独立方法。
        /// 🔴 **2026-09-28：原版是【居中】不是右对齐** —— `LoreText` 的 TMP
        /// `m_HorizontalAlignment = 2`（Center；位标志 Left=1/Center=2/Right=4），实拍也一致
        /// （正本 §十·5 第 4 条）。`MenuDraw.Text` 默认居中 ⇒ **不要**再 `AlignRight`。</summary>
        void BuildLoreText(Transform bg)
        {
            if (bg == null) return;
            for (int i = bg.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(bg.GetChild(i).gameObject);
            var def = FrontDef ?? Card;
            // ① 底板：**按阵营**选图（原版 `FlavourTextSO.GetClanFlavorBackground`）—— 认的是**当前前台**那张；
            //    换位收尾会重建本方法 ⇒ 阵营跟着换。⚠️ 原版 `m_PreserveAspect = 0`（实读）⇒ **拉伸**，别开 keepAspect。
            var tex = CardArt.FlavorBg(def != null ? def.Faction : null);
            if (tex != null)
                MenuDraw.Rect(bg, tex, new PxRect(LowerL, LoreBgT, LowerR, LoreBgB), "Image", QCdRow, null, false);
            else
                Debug.LogWarning("[CardDetail] 阵营「" + (def != null ? def.Faction : "?")
                               + "」没有风味底图（`CardArt.FlavorBg` 取不到）⇒ **只画字**（不静默）");
            // ② 那行字
            // 🆕 **2026-10-12（A333 + A336③）**：上限 = 原版 `m_fontSizeMax`（**35** = 标称 `LorePx`，本来就对）·
            //   base = 原版 `m_fontSizeBase` = **36.0**。判据 = 本窗那一颗 `LoreText` 的 MB 实读
            //   `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1866.json`
            //   （`m_text` 是那张卡的风味文本、`fs 35.0 · auto[10.0~35.0] · base 36.0 · 折行 1`；
            //   正本 §一 记的 `LoreText … fs35 hAlign=Right` 与 `fs` 这一格吻合 ⇒ 同一颗）。
            string lore = def != null ? BattleDriver.FaceTextFull(def).body : "";
            var r = new PxRect(LoreL, LoreT, LoreR, LoreB);
            MenuDraw.Text(bg, r, lore, Color.white, "LoreText", LorePx, QCdText, LoreR - LoreL, 10f, LorePx, 36f);
        }

        /// <summary>点第 `idx` 格 ⇒ **和前台那张两两换位**（原版 `CardDisplayWindow.ChangeCardPosition`，
        /// 253 行逐行读过 —— 判据全文 → 正本 §9·6）。
        /// **三个闸**（任一中就整个 return，什么都不做）：① 上一次没播完（`swappingCards`）·
        /// ② 点的是前台自己 · ③ 有 `Animation` 在播。
        /// ⚠️ 闸③ **本工程不适用** —— 原版那条摆位用的 `Animation` 我们没有（我们按 `CardFan` 的常量直接摆），
        /// 这一点是**如实说明的偏离**。
        /// ⚠️ 层级对调照原版是 `SetSiblingIndex` **两两互换**（**不是「提到最前」**）；
        /// 我们**另外**还要按新槽位重排**渲染队列**（这套的前后由队列决定，见 `CardFan`）。
        /// ⚠️ 收尾还要**跟着换着色**（原版 `ChangeCardPosition.c:203-221`）。</summary>
        public void SwapToFront(int idx)
        {
            if (_swapping)
            {
                Debug.Log("[CardDetail] 换位被挡：上一次还没播完（原版闸① `swappingCards`）");
                return;
            }
            if (idx <= 0 || idx >= Slots) return;                  // 闸②：点的是前台自己（位姿槽 0）
            var a = SlotViews[0]; var b = SlotViews[idx];
            if (a == null || b == null) return;
            _swapping = true;

            var pa = a.transform.position; var pb = b.transform.position;
            float ra = a.transform.eulerAngles.z, rb = b.transform.eulerAngles.z;
            float sa = a.transform.localScale.x, sb = b.transform.localScale.x;

            // 六条 tween 全用同一个时长（原版 `relatedCardSwapTime` = 0.25s）：
            // 被点那张 → 前台位；前台那张 → 被点卡的槽位。
            CardTween.ToPose(b.transform, pa, ra, sa, SwapTime, DG.Tweening.Ease.OutQuad).OnComplete(() =>
            {
                // 层级对调：**两两互换** sibling 序（照原版 `SetSiblingIndex`）
                int ia = a.transform.GetSiblingIndex(), ib = b.transform.GetSiblingIndex();
                a.transform.SetSiblingIndex(ib);
                b.transform.SetSiblingIndex(ia);
                // 记账：两个**位姿槽**上的卡对调（前台永远是位姿槽 0）
                var td = _slotDefs[0]; _slotDefs[0] = _slotDefs[idx]; _slotDefs[idx] = td;
                var tv = SlotViews[0]; SlotViews[0] = SlotViews[idx]; SlotViews[idx] = tv;
                // 🔴 **队列要按【新的槽位】重排**（sibling 序换了、我们这套的**前后由渲染队列决定**）
                //    —— 不重排的话「刚换到前台的那张」还带着原来那个较低的号、会被身后的卡盖住。
                CardFan.ApplyQueues(SlotViews, SlotCount, QCardStack);
                _swapping = false;
                // 收尾（原版 `CardSwapFinished`）：开闸（上面已做）· 更新前台指针（上面已做）·
                // **重设 lore**（`SetCardLore`）· **重设语音 / 眼睛钮**（`SetCardVoiceOver` 那两条）。
                BuildLoreText(Find(transform, "FlavourTextBG"));
                RefreshLore();
                RefreshButtons();
                Debug.Log("[CardDetail] 换位完成：前台现在是「" + (FrontDef != null ? FrontDef.Name : "?")
                        + "」（收尾重设了 lore / 语音 / 眼睛钮 —— 原版 `CardSwapFinished`）");
            });
            CardTween.ToPose(a.transform, pb, rb, sb, SwapTime, DG.Tweening.Ease.OutQuad);
            // 着色跟着换（原版 `ChangeCardPosition.c:203-221` 那两条 tween，同一时长）：
            // 去前台那张 → 白，让出前台那张 → `cardInBackGroundColorTint`(0.65)。
            CardFan.TweenTint(b, CardFan.BackTint, CardFan.FrontTint, SwapTime);
            CardFan.TweenTint(a, CardFan.FrontTint, CardFan.BackTint, SwapTime);
            Debug.Log($"[CardDetail] 换位：第 {idx} 格 ⇄ 前台（{SwapTime}s = 原版 `relatedCardSwapTime`）");
        }

        /// <summary>这张卡**能不能「创建副本」（合成）** —— 决定那块面板建不建。
        ///
        /// 🔴 **2026-09-27 用户指出：`special` 那批不能合成**。而那 50 张**不是「第五档稀有度」**，
        ///   是 **防御卡 39 + 药剂卡 6（`Combat Elixir`）+ 秘仪 5（`Secret`）** 在源数据里
        ///   **没有稀有度被塞的占位值**（`cards_engine.json` 实测：`special` 全是 `type=defence/tactic`）。
        ///   ⇒ **判据 = 稀有度是不是那四档之一**（与 `WildcardIconFor` 同一张表）。
        ///
        /// ⚠️ **原版运行时长什么样，关服查不到**（可能整块隐藏、也可能是个灰掉的按钮）
        ///   ⇒ 这里选**整块不建**，**不造一个假的禁用态**（同 `CLAUDE.md`「宁可没有，不可错着显示」）。
        ///   真要改成「建出来但点不动」是一行的事 —— 先把口径记在这儿。</summary>
        static bool Craftable(string rarity)
        {
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "common": case "rare": case "epic": case "legendary": return true;
                default: return false;
            }
        }

        // ---- ① 创建副本 ----
        /// <summary>万能卡图标 —— **四档稀有度各一张**（原版图集 `40k_general_wildcard_*_small`）。
        /// 🔴 **稀有度是四档**（`common/rare/epic/legendary`）—— 卡池里那个 `special`
        ///   （**防御卡 39 + 战术卡 11**，`cards_engine.json` 实测）**不是第五档**，
        ///   是「这张卡没有稀有度」被源数据塞了个占位值（2026-09-27 用户指出、逐条核过）。
        /// 🔴 **判据只此一处** —— 通配符计数条（`BuildWildcards`）与「创建副本」按钮的图标（`BuildCrafting`）共用它。</summary>
        static readonly string[] WildcardIcons =
        {
            "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
            "40k_general_wildcard_epic_small",   "40k_general_wildcard_legendary_small",
        };

        /// <summary>按稀有度取万能卡图标。**认不出时返回原版 prefab 的默认那张** ——
        /// 原版 `Craft/WC icon` 的 `m_Sprite` 存的就是 `40k_general_wildcard_epic_small`，
        /// 而 `CraftingPanel` 上有 `wildcardIcon` / `wildcardSprites` 两个字段
        /// ⇒ **运行时是脚本按卡换的**；**防御卡那 39 张运行时画哪张，关服查不到**
        /// ⇒ 不猜，用 prefab 的原值，并如实说出来。</summary>
        static string WildcardIconFor(string rarity)
        {
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "common":    return WildcardIcons[0];
                case "rare":      return WildcardIcons[1];
                case "epic":      return WildcardIcons[2];
                case "legendary": return WildcardIcons[3];
                default:          return WildcardIcons[2];   // = prefab 原值（`_epic_small`）
            }
        }

        void BuildCrafting(Transform panel)
        {
            var p = PanelBox(panel, "Crafting Panel", CraftT, CraftB);
            Title(p, "Crafting Title", "Create a copy of this card", 1307.5f, 165.17f, 1726f, 244.29f, 42f);
            var b = new PxRect(1384f, 244.29f, 1654f, 304.95f);
            // ⚠️ `UI_Button_Mulligan` **不是九宫格图**（实读 `Sprite/UI_Button_Mulligan.json`：`m_Border = None`，
            //    图 410×124）—— 原版标 `type=Sliced` 但**没有 border 就等于拉伸** ⇒ 我们直接走 `Rect`。
            //    （走 `Nine` 会吐一串「border 比图还大，退回单块」的警告，而结果一样。）
            var craftBg = MenuDraw.Rect(p, CardArt.MenuUi("UI_Button_Mulligan"), b, "Craft Bg", QCdRow);
            MenuDraw.Text(p, new PxRect(b.x1, b.y1, b.x1 + 122.88f, b.y2), "1", Color.white, "Craft Text", 40f, QCdText);
            // 图标原版 `scl=1.5`、落在 1506.88..1654
            // 🔴 **2026-09-27 修：这里原来画的是 `40k_main_collection_icon`（卡池/收藏图标）—— 取错图了。**
            //    原版那颗叫 **`WC icon`**（万能卡图标），`menu_dump` 实读：
            //      `…/Crafting Panel/…/Craft/WC icon` = `40k_general_wildcard_epic_small` 41×51
            //      · **Simple + preserveAspect** · 框 135×50.66 · pivot(0,0.5) ⇒ 实绘 40.7×50.66
            //    同格下面那行字就是 `This will consume a wildcard`（消耗一张万能卡）⇒ 画万能卡图标才对。
            float ix = 1506.88f, iy = b.y1 + 10f, iw = 147.12f, ih = b.y2 - b.y1 - 20f;
            MenuDraw.Rect(p, CardArt.MenuUi(WildcardIconFor(Card.Rarity)), new PxRect(ix, iy, ix + iw, iy + ih),
                          "Craft Icon", QCdRow, null, true);
            // 🔴 **2026-10-18（A1126 · A1 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 40px。
            //   判据 = 逐颗现读原版那一颗（`bundle_scenes_scenes_mainmenuwarpforge`，按 `m_text` 认人：
            //   `MonoBehaviour_1830.json` ← `RectTransform_1501` ← GO `Explanation`，本窗是**场景对象**、不是 prefab）：
            //   `'This will consume a wildcard'` · `m_fontSize` **40.0** · `m_fontSizeBase` **36.0** ·
            //   **`m_enableAutoSizing = 1`** · `m_fontSizeMin` **10.0** · `m_fontSizeMax` **40.0** ·
            //   对齐 `Center/Middle` · **`m_TextWrappingMode = 1`**（折行）⇒ 与本处传 `wrapPx` 同档。
            //   ⚠️ 原版那一颗的 `m_SizeDelta = (0,0)` + 祖先链上 `m_LocalScale = (0,0)`（动画/隐藏态）
            //   ⇒ **原版框宽量不到**，本处的 `1307.5 → 1726`（418.5）**是我们摆的**（铁律 3：查不到就说查不到）。
            //   ✅ **2026-10-12（A1699）就地订正（铁律 5）**：本处原来记着「这条调用**当前没有生产路径**
            //   …… `BuildCrafting` 全库**零调用点**」—— **已过期**：两块面板按用户 2026-10-12 的口径恢复，
            //   `Build()` 里现在会调 `BuildCrafting`（稀有度可合成的卡）⇒ **这一句是活的**，
            //   `Editor/ShellScene.cs` 那条「读源码文本」的静态断言之外，本处现在也走得通了。
            //   📌 也一并销掉那句「『要恢复：把下面那两行注释掉的条件去掉即可』与现状不符」——
            //   恢复**已经做完**，那句话（连同它指出的事实）都不必再留。
            // 🆕 **2026-10-12（第二笔 · 用户裁定：「只出声、不做机制」）**：这一行**必须让玩家看出
            //   「这块本版没实现」** —— 因为上面那颗钮点下去**什么都不改**（`OnCraftClicked` 只 `Debug.Log`）。
            //   ⛔ 原样印 `This will consume a wildcard` 就是**文案与行为对不上、且毫无标识**（红色线：不许静默失败）。
            //
            //   🔴 **记号用本工程现成的那一个 —— 末尾缀 `*`，⛔ 没有发明新记号。**
            //     同族先例（唯一的一处，全仓就这一族）：
            //       · `Battle/BattleDriver.cs:12064-12097` `UnimplementedNotes(CardDef)` ——
            //         `list.Add("效果本版结算不了*")` / `list.Add("效果本版没作用*")`；
            //         纯名字那一形 = `CardText.Keyword(kw) + "*"`（同文件 `:12040/12046`）。
            //       ⇒ 形状 = **「文案自己把状态说清楚」+ 末尾一个 `*`**，`*` 就是「本版未实现 / 没作用」。
            //       · 拼接形状（原句 + 两个空格 + 缀注）照 `BattleDriver.FaceTextFull`（`:11974`）
            //         那句 `string.IsNullOrEmpty(notes) ? body : body + "  " + notes`。
            //   ⚠️ **语言选英文**：本窗**每一句都是原版 prefab 的英文原文**（我方**没有** I2 词条表，
            //     见 `资料/…` 那几条老账），往英文面板里插一句中文会更难读 ⇒ 缀注也写英文。
            //     （中文先例 `效果本版没作用*` 是卡面那一路，它走 `CardText`，本来就按语档切。）
            //   ✅ **行为一字未改**：那颗钮照旧可点、照旧 `OnCraftClicked` 出声（本笔**没**动 `Hit(...)` 那一句）。
            //   ⚠️ **`"1 〔万能卡图标〕"` 那两件【没有】加记号**（理由）：`Craft Text` 就是一格**数字**
            //     （原版 `Button Text` = `"1"`），它是「价格」槽不是「承诺」句 —— 往数字后面缀 `*`
            //     会被读成「价格是 1\*」（另一件事）。**承诺那一句是这一行**，记号落在这里。
            //   ⚠️ **字号未在真机/批处理里量过**（本笔红线：不跑 Unity）：这一格是 autosize
            //     `[10~40] base 36`、折行开、框 418.5×63.29 ⇒ 63 个字符估下来落在 fs≈25 / 2 行
            //     （**是估算，不是量出来的**；要复核就量渲染后的 `fontSize` 与块高，⛔ 别只比字号）。
            MenuDraw.Text(p, new PxRect(1307.5f, 315.5f, 1726f, 378.79f),
                          "This will consume a wildcard  Not implemented in this build*",
                          Color.white, "Craft Explanation", 40f, QCdText,
                          1726f - 1307.5f, 10f, 40f, 36f);
            MenuDraw.Rect(p, CardArt.MenuUi("40K_generic_bt_info"), new PxRect(1740.65f, 161.91f, 1782f, 203.26f),
                          "Craft Info Icon", QCdRow, null, true);
            // 🆕 **2026-10-12（A1699）命中区归真**：原来这一句是 `Hit(p, "CraftHit", b, DoCraft, QCdHit)`
            //   （只传 5 个实参 ⇒ `WindowButton` 的 `target` 是空的）。现在①把**画出来那颗黄底**传进去当
            //   `target`（口径 = 原版那颗 `EverguildButton.m_TargetGraphic` 就是它自己的底图，
            //   `m_Transition = 2 (SpriteSwap)` · `m_HighlightedSprite` / `m_PressedSprite` 都非空），
            //   ②点下去走 `OnCraftClicked()`（**本版不实现真机制，出声**）—— 不是 `DoCraft()`。
            Hit(p, "CraftHit", b, () => { OnCraftClicked(); }, QCdHit, craftBg, "UI_Button_Mulligan");
        }

        // ---- ② 升级 ----
        void BuildUpgrade(Transform panel)
        {
            var p = PanelBox(panel, "Upgrade Panel", UpgT, UpgB);
            Title(p, "Upgrade Title", "Upgrade this card\nto level " + Mathf.Min(CardProgress.Level(Card.Id) + 1, CardProgress.MaxLevel),
                  1307.5f, 437.28f, 1726f, 515.77f, 41.4f);
            var b = new PxRect(1384f, 524.96f, 1654.38f, 585.31f);
            var upBg = MenuDraw.Rect(p, CardArt.MenuUi("UI_Button_Mulligan"), b, "Upgrade Bg", QCdRow);
            // `Upgrade` 里那条：副本数（`cards` HLG）+ 金币（`cost` HLG）
            bool can = CardProgress.CanUpgrade(Card.Id, Card.Rarity);
            int need = CardProgress.NeedCopies[Mathf.Min(CardProgress.Level(Card.Id) + 1, CardProgress.MaxLevel)];
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 40px。
            //   判据 = 原版那一颗（`bundle_scenes_scenes_mainmenuwarpforge` ▸ `Upgrade Panel/Content/
            //   Upgrade/cards/quantity`，逐颗现读 TMP 组件字段）：**`m_enableAutoSizing = 1`** ·
            //   `m_fontSizeMin = **10**` · `m_fontSizeMax = **40**` · `m_fontSizeBase = **12**` · **折行 = 0**。
            //   ⚠️ **刻度 = 原版刻度**（本窗 `Title` 那一族的实参就是原版原值 —— `auto[10~42] base36`
            //   与 `Explanations` 三颗逐值吻合，见 `Title` 的头注）⇒ **不做任何比例换算**，四格照抄。
            //   ⚠️ 折行 0 ⇒ `SetAutoFitBox` 内部那句 `SetWrapWidth` 会**无条件**开成 `Normal`
            //   ⇒ 紧跟一句 `SetWrapping(false)` 还原自己那一档。
            var upNeed = MenuDraw.Text(p, new PxRect(b.x1, b.y1, 1443.83f, b.y2), need.ToString(), Color.white,
                                       "Upgrade Need", 40f, QCdText, 1443.83f - b.x1, 10f, 40f, 12f);
            if (upNeed != null) upNeed.SetWrapping(false);          // 原版折行=0（A205：关这一下顺带推版面）
            MenuDraw.Rect(p, CardArt.MenuUi("40k_general_icon_card_amount"), new PxRect(1450f, b.y1 + 8f, 1490f, b.y2 - 8f),
                          "Upgrade Card Icon", QCdRow, null, true);
            // 同 `Upgrade Need`：原版 `Upgrade Panel/Content/Upgrade/cost/quantity` =
            // `auto[10~40] · base 12 · 折行 0`（见上面那一段的判据）。
            var upCost = MenuDraw.Text(p, new PxRect(1499.72f, b.y1, 1570f, b.y2),
                          CardProgress.NeedGold[Mathf.Min(CardProgress.Level(Card.Id) + 1, CardProgress.MaxLevel)].ToString(),
                          Color.white, "Upgrade Cost", 40f, QCdText, 1570f - 1499.72f, 10f, 40f, 12f);
            if (upCost != null) upCost.SetWrapping(false);          // 原版折行=0
            MenuDraw.Rect(p, CardArt.MenuUi("40k_topmarquee_currency_gold"), new PxRect(1578f, b.y1 + 8f, 1643f, b.y2 - 8f),
                          "Upgrade Gold Icon", QCdRow, null, true);
            // 同族：原版 `Upgrade Panel/Content/Upgrade/Explanation/Text Explanation` =
            // `auto[10~40] · **base 36** · 折行 0`（base 与上面两颗不同，⛔ 别一刀切）。
            var upExpl = MenuDraw.Text(p, new PxRect(1309f, 594.53f, 1729f, 659.33f),
                          "Will get: +" + CardProgress.Points[Mathf.Min(CardProgress.Level(Card.Id) + 1, CardProgress.MaxLevel)],
                          Color.white, "Upgrade Explanation", 40f, QCdText, 1729f - 1309f, 10f, 40f, 36f);
            if (upExpl != null) upExpl.SetWrapping(false);          // 原版折行=0
            MenuDraw.Rect(p, CardArt.MenuUi("40K_generic_bt_info"), new PxRect(1740.65f, 426.42f, 1782f, 467.77f),
                          "Upgrade Info Icon", QCdRow, null, true);
            // ============================================================ 🔴 「升级」那颗钮：**显示、但不可交互**
            //
            // **用户 2026-10-12 原话**：「不需要升级卡牌系统，**默认最高级就是不能点击升级卡牌按钮**」。
            //
            // 🔴 **「不可交互」用的是本仓同族现成的那一个口 —— `WindowButton.Interactable`**（**没有发明新机制**）。
            //   它就是原版那一句的等价物：`UpgradePanel__CheckUpgradeRequirements.c`（本笔亲读）末尾那三行
            //     `UnityEngine_UI_Selectable__set_interactable(*(param_1 + 0x40), uVar2)` —— `+0x40` 正是
            //     `Upgrade` 那颗钮（`UpgradePanel` 的字段序：0x20 currentCardUI · 0x28 options · 0x30 Content ·
            //     0x38 No Upgrade Warning · **0x40 那颗钮**）。
            //   本仓那一份的**两半**都落地（`Shell/WindowButton.cs`）：setter → `RefreshGray()`
            //   （把子树图形件换成原版灰化 shader `Everguild/UI/Greyscale`）+ `Click()` 头一句
            //   `if (!_interactable) { 出声; return; }`（= 原版 `Selectable.OnPointerClick` 的第一句）。
            //   同族先例（**逐字照它的两条纪律**）：`Shell/CampaignRewardWindow.cs:820-849` ——
            //     ① 存量实现只有一处（`SoftDisabled` 与 `Interactable` 共用 `RefreshGray`），我们转调它；
            //     ② 🔴 **必须排在 `Bind` 【之后】**：`GrayTargets()` = `target` + 子树，而 `target` 是
            //        `Bind` 才设成那颗**画出来的**底图的 —— 排在 `Bind` 之前，头一次 `RefreshGray()`
            //        只灰到那颗**透明的命中区 quad**，而 `GrayedForTest` 随即为真 ⇒ 后面再设都早退
            //        ⇒ **画面上一像素都不变**（静默）。`Hit(...)` 内部就 `Bind`，所以这一句放在它后面即可。
            //   ✅ 顺带正确：`Shell/PointerLayer.cs` 的命中收集读的是 `WindowButton`，
            //      而 `Click()` 自己挡在派发之前 ⇒ 点上去**连派发都没有**（并出声，见 `Click()`）。
            //
            // ⚠️ **一处仍开着的如实标注（铁律 3）+ 一处已销（铁律 5）**：
            //   ① 🔴 **仍开着，交调度台裁**：原版**满级时走的是另一支**：`GetNextTierInfo` 给出空 ⇒
            //      **`Content` 整组 `SetActive(false)` + `No Upgrade Warning` 露出**
            //      （`CheckUpgradeRequirements.c:113-119` 那三行），**不是**「停在场上、变灰」。
            //      用户这次要的是「面板恢复 + **显示**但不可交互」⇒ 本笔**照用户口径**（面板在、钮灰着）。
            //      🔴 它**不是可有可无的版式选择，是硬约束**：那两件**同 rect**（见下面 `No Upgrade Warning`
            //      那一段），同时 active 就是**两行字叠在同一处** ⇒ 本版只能「`Content` 在 ⇒ warning 不露」。
            //   ② ✅ **2026-10-12（第二笔 · 铁律 5 就地订正）**：本行原来写「`CardProgress.Level` 的缺省是
            //      **1**（不是 `MaxLevel`）⇒ 上面那颗 `Title` 会印 "Upgrade this card to level **2**"、
            //      `No Upgrade Warning` 也不露 —— 与「默认最高级」**不完全自洽**。本笔**不动**，已报回。」
            //      —— **已经改掉了**：用户裁定「**改成默认最高级**」⇒ `CardProgress.Level` 的缺省
            //      **1 → `MaxLevel`**（见那个方法的 doc）。现在这一格的读数变成：
            //        · `Title` 印 `Min(4 + 1, 4)` = **「Upgrade this card to level 4」**（`Mathf.Min` 那一下
            //          就是为「已经满级」写的钳位，⛔ 别删 —— 删了 `NeedCopies[Level + 1]` 会越界）；
            //        · `need` / `cost` / `points` 三格同样钳到**第 4 级那一行**（9 张 · 2000 金 · +500 点）；
            //        · `CanUpgrade` 恒假 ⇒ 满级那一支成立（下一句 `No Upgrade Warning` 见下面那一段）。
            //      ⚠️ **如实标（本笔没改，⛔ 不发明文案）**：原版**满级时根本不写这颗 `Title`**
            //      （`CheckUpgradeRequirements.c:117-118` 在写 Title 之前就 `return` 了）⇒ 原版没有
            //      「满级时 Title 该印什么」的判据 ⇒ 我们这里印的是钳位后的数（读起来像「还要升到 4 级」）。
            //      要改成「满级就不印」= 落乙 §6·3·1 第 1 条那条待裁（`Content` 整组关），见下面那一段。
            var upHit = Hit(p, "UpgradeHit", b, () => { DoUpgrade(); }, QCdHit, upBg, "UI_Button_Mulligan");
            var upWb = upHit != null ? upHit.GetComponent<WindowButton>() : null;
            if (upWb != null)
            {
                upWb.Interactable = false;
                Debug.Log("[CardDetail] 「升级」那颗钮 = **不可交互**（`WindowButton.Interactable = false`；"
                        + "用户 2026-10-12 口径：「默认最高级就是不能点击升级卡牌按钮」）"
                        + " —— 原版同一位：`UpgradePanel__CheckUpgradeRequirements.c` 的 "
                        + "`Selectable.set_interactable(+0x40, …)`。"
                        + "⚠️ 变灰要 `Everguild/UI/Greyscale` 那张原版 shader（取不到则**只有行为、没有灰**，见 `RefreshGray`）；"
                        + "⚠️ 面板版式仍是「升级到下一级」那一副（`CardProgress.Level` 缺省 = `MaxLevel` ⇒ 上面"
                        + "三格钳在第 4 级那一行；原版满级支其实是把 `Content` 整块关掉、换 `No Upgrade Warning`）"
                        + "—— 见本段的如实标注①：那两件**同 rect**、本版按用户口径让 `Content` 常显 ⇒ warning 只能关着");
            }
            else Debug.LogWarning("[CardDetail] 「升级」那颗钮的 `WindowButton` 拿不到 ⇒ **『不可交互』没落地**（不静默）");

            // `No Upgrade Warning`：**满级才露**（原版那件与 `Content` 同级、同 rect）
            var w = MenuDraw.Node(p, "No Upgrade Warning", new PxRect(PanL, UpgT, PanR, UpgB));
            // 🆕 **2026-10-12（A333 + A336③）**：上限 = 原版 `m_fontSizeMax` = **40**（= 标称，本来就对）·
            //   base = **36.0**。判据 = `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1867.json`
            //   （`m_text = 'Maximum card tier reached'` · `fs 40.0 · auto[25.0~40.0] · base 36.0 · 折行 1`
            //   —— 全库唯一一颗，与我们的文案逐字相同 ⇒ 就是它）。
            MenuDraw.Text(w, new PxRect(1314f, 430.62f, 1724f, 644.92f), "Maximum card tier reached",
                          Color.white, "No Upgrade Title", 40f, QCdText, 410f, 25f, 40f, 36f);
            // 🔴 **2026-10-12（第二笔）现读：本版它【恒不露】，而且这是【几何上的必然】**，不是漏做。
            //
            //   判据（本笔亲读）—— 原版这两件**互斥**，靠的是同一个布尔的两面：
            //     `UpgradePanel__CheckUpgradeRequirements.c:113-119`
            //       `SetActive(+0x30 /*Content*/, next != null);`
            //       `SetActive(+0x38 /*No Upgrade Warning*/, next == null);`
            //       `if (next == null) return;`          // ← 满级就到此为止
            //   而我们这两件**同 rect、同子树、同渲染队列**：
            //     · 本文件的这两句摆在**原版逐值相同**的位置（`No Upgrade Title` @ `1314,430.62,1724,644.92`，
            //       `Label` 出厂对齐 = Center/Middle ⇒ 单行落在 y≈537）；
            //     · `Upgrade Bg` / `Upgrade Need` / `Upgrade Cost` 就在 `524.96..585.31`
            //     ⇒ 同时 active = `Maximum card tier reached` **正好压在 `Upgrade` 那颗钮上**（两行字重叠）。
            //
            //   ⚠️ 本笔**照用户 2026-10-12 口径让 `Content` 常显**（那句是「**显示**但不可交互」，
            //      ⛔ 不是「面板整组关」）⇒ 就只能让 warning 关着。
            //   🔴 **要它露，必须先落那条待裁**（→ 乙 §6·3·1 第 1 条 / 账 `A1699`）：口径改成
            //      「满级 ⇒ `Content` 整组关」时，把下面这一句换成 `w.gameObject.SetActive(maxed);`
            //      并把上面那 7 件（`Title`/`Bg`/`Need`/`Card Icon`/`Cost`/`Gold Icon`/`Explanation`）
            //      一起 `SetActive(false)` 即可 —— **一处改，别分两处写**。
            bool contentShown = true;   // = 本版口径（用户 2026-10-12）：升级面板的 `Content` 常显
            bool maxed = CardProgress.Level(Card.Id) >= CardProgress.MaxLevel;
            w.gameObject.SetActive(!contentShown && !can && maxed);
        }

        // ---- ③ 异画（Alternate Art）----

        /// <summary>这张卡**有没有异画风格** —— 等价于原版 `RawCardScript.HasAlternativeArtStyles(card)`，
        /// 也就是 `AlternateArtPanel.Initialize` 拿来决定**整块 `SetActive`** 的那个布尔。
        ///
        /// <para>🔴 **判据是【数据】，不是「图加载出来没有」**：原版那句查的是服务端配置
        /// （`RawCardScript__HasAlternativeArtStyles.c` → `CardsSpecialConfig.CanShowAACard`，
        /// LiveOps 配置，**本地取不到**）⇒ 用**同一批数据**的本地副本 =
        /// `CollectionWindow.AltArtCards`（本机 7 张督军异画，出处 = `工具/import_original_art.py` 的 `ALT_ART`）。
        /// ⛔ **别改成 `CardArt.AltArt(id) != null`** —— 那是「贴图加载成功没有」，
        /// 一旦 `Resources/Art/` 缺图就会把**所有**卡的这一块都关掉（把「没有原版美术」误当成「没有异画」）。</para></summary>
        public static bool HasAltArtStyle(string cardId)
        {
            return AltStylesOf(cardId).Count > 0;
        }

        /// <summary>🆕 **2026-10-12（A1699）**：这张卡的那几种异画风格（**按 `CollectionWindow.AltArtCards`
        /// 的次序**）。
        ///
        /// <para>🔴 **本表只有一份** —— `Shell/CollectionWindow.cs:1478` 的 `AltArtCards`（7 张 · 2 种风格，
        /// 出处 = `工具/import_original_art.py` 的 `ALT_ART`）。**这里只是按卡 id 筛一遍，不是新写一张表**
        /// （本仓红线「两处写同一条规则 = 迟早不一致」；收藏窗第 4 页签 Styles 那套是**同一份数据**的另一个视图）。
        /// ⛔ **别在这里自己枚举 `Resources/Art/altarts/`，也别改成 `CardArt.AltArt(id) != null`** ——
        /// 后者是「贴图加载成功没有」，理由同 <see cref="HasAltArtStyle"/> 的注释。</para>
        /// <para>⚠️ 本地 7 张里**每张卡只有 1 种风格**（`AA_HB` ×6 / `v2` ×1）⇒ 实际总是 0 或 1 张；
        /// 但原版的 `currentStateIndex` 是**按张卡的风格表**走的（1 基）⇒ 这里照**列表**建模、不写死「1 种」，
        /// 将来远端 CCD 那批 `alternateartstyles` 到手时不用改结构。</para></summary>
        public static List<CollectionWindow.AltArtCard> AltStylesOf(string cardId)
        {
            var list = new List<CollectionWindow.AltArtCard>();
            if (string.IsNullOrEmpty(cardId)) return list;
            foreach (var a in CollectionWindow.AltArtCards) if (a.CardId == cardId) list.Add(a);
            return list;
        }

        /// <summary>当前异画档要喂给卡面的那张图 —— **0 档（原画）返回 `null`**（= 不改 `artOverride`）。
        /// ⚠️ 取不到图时返回 `null` 并**出声**（红线：不许静默 —— 否则「切了没反应」没人知道为什么）。</summary>
        Texture2D AltArtTexture()
        {
            if (AltStyleIndex <= 0 || Card == null) return null;
            var tex = CardArt.AltArt(Card.Id);
            if (tex == null)
                Debug.LogWarning("[CardDetail] 异画立绘取不到：「" + Card.Id + "」的第 " + AltStyleIndex
                               + " 档（`CardArt.AltArt` 返回 null）⇒ **卡面退回普通立绘**（不静默）");
            return tex;
        }

        void BuildAltArt(Transform panel)
        {
            var p = PanelBox(panel, "Alternate Art Panel", AltT, AltB);
            // 这张卡的异画风格表（**只此一份**，见 `AltStylesOf`）；`has` = 原版那句
            // `RawCardScript.HasAlternativeArtStyles(card)` 的等价物（决定整块 `SetActive`）。
            var styles = AltStylesOf(Card != null ? Card.Id : null);
            var has = styles.Count > 0;
            AltStyleIndex = has ? Mathf.Clamp(AltStyleIndex, 0, styles.Count) : 0;   // 0..Count（原版 1 基那一段）

            // ---- ① 左右两颗换画圆钮（原版 `Alternate Art Panel` 的 `Select Art Button Left/Right`）----
            // 判据链（`d:/2/tools/decomp_full/`，本笔逐字读过）：
            //   `CardDisplayOptions__Initialize.c:31`（第三块 = `+0x40 alternateArtPanel`）
            //   → `AlternateArtPanel__Start.c`（给两颗钮各挂一个 `NavigationButtonClick(bool isLeft)`）
            //   → `AlternateArtPanel__NavigationButtonClick.c:9`（先两颗都 `interactable = 0`、换完再开回来）
            //   → `AlternateArtPanel__SwapStyle.c:25`（`index ±1`，`:30/:36` 回绕）
            //   → `AlternateArtPanel__SetStyle.c:47-52`（`BasicCardUI.SetCardImage(前台那张, 该风格图)`）。
            // ⚠️ 这两颗钮**只在有异画时才建得出画面**（整块 `SetActive(has)` 那一句在末尾）——
            //    但节点**照建**（原版也是建好、整块关），这样自检两向都断得到。
            BuildAltArrow(p, true);
            BuildAltArrow(p, false);

            // ⚠️ 原版这一格的 `Title` 印的是**升级文案**（复制粘贴 bug）⇒ **我们不抄那个 bug**，
            //    照这一格自己的意思写（**出声**）。
            Title(p, "AltArt Title", has ? "Alternate art (owned)" : "Alternate art", 1307.5f, 690f, 1726f, 760f, 31.7f);
            // `Current Style`：原版那一格跟着当前档走（`SetStyle` 里写 `+0x30 styleImageContainer` 的
            //   `set_enabled` + `set_sprite(该风格的图标)`）。**风格图标 SO 在服务端**（本地没有）
            //   ⇒ 我们印**风格名 + 第几档**（**显示名与档号是我们挑的**，如实标；风格名走
            //   `CollectionWindow.StyleLabel` 那一份，不另写映射）。
            string cur = !has ? "No alternate art"
                       : AltStyleIndex == 0 ? "Original art  (0/" + styles.Count + ")"
                       : CollectionWindow.StyleLabel(styles[AltStyleIndex - 1].Style)
                         + "  (" + AltStyleIndex + "/" + styles.Count + ")";
            MenuDraw.Text(p, new PxRect(1307.5f, 762f, 1726f, 800f), cur, Color.white, "Current Style", 28f, QCdText);
            // 🔴 **异画绑死在那张卡上**（`AlternateArtCard.GetIdForClonedCard` 对原卡 id 做一次 `String.Replace`）
            //    ⇒ 这张卡没有异画就是没有，不给别的督军换。本地只有 7 张督军异画。
            float bw = 328.71f, bh = 73.38f;
            float bx = PanL + (PanR - PanL - bw) * 0.5f, by = 820f;
            var buyBg = MenuDraw.Rect(p, CardArt.MenuUi("UI_Button_Mulligan"), new PxRect(bx, by, bx + bw, by + bh),
                          "Buy Bg", QCdRow);
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 32px。
            //   判据 = 原版 `Alternate Art Panel/Buy Original Card Button/Generic UI Button/Price Display/text`
            //   （= 画面上**可见**的那一颗；那颗钮底下另一颗 `Button Text` 是 `INACT` 空串）：
            //   **`m_enableAutoSizing = 1`** · `m_fontSizeMin = **12**` · `m_fontSizeMax = **54**` ·
            //   `m_fontSizeBase = **39**` · **折行 = 0**。
            //   ⚠️ 我们这一格的框 `328.71 × 73.38` 与原版那颗钮**逐位相同** ⇒ 就是它。
            //   ⚠️ 本窗刻度 = 原版刻度 ⇒ 四格照抄；⚠️ 折行 0 ⇒ 紧跟一句 `SetWrapping(false)`。
            var buyLb = MenuDraw.Text(p, new PxRect(bx, by, bx + bw, by + bh), has ? "Alternate art" : "No alternate art",
                          Color.white, "Buy Original Card", 32f, QCdText, bw, 12f, 54f, 39f);
            if (buyLb != null) buyLb.SetWrapping(false);            // 原版折行=0
            MenuDraw.Rect(p, CardArt.MenuUi("WF_Lock_Icon_Simple"), new PxRect(1310f, 690f, 1360f, 740f),
                          "Lock Icon", QCdRow, null, true);

            // 🆕 **2026-10-12（A1699）：这颗价签**原来**一个命中区都没有**（`Buy Bg` 只是画了一张图）
            //   ⇒ 画面上立着一颗「看起来能点、点下去什么都不发生」的钮（本仓红线：不许静默失败）。
            //   原版它**有** onClick（`AlternateArtPanel.Start.c:39-42` 把 `PriceDisplayButton.OnClick`
            //   接到 `BuyOriginalCard`），而那条链**本地走不通**：`BuyOriginalCard.c` 是
            //   `WindowsManager.ShowPopUp(<确认弹窗>)` + `PlayfabWrapper.GenericCloudScriptHandler(0x385, …)`
            //   —— **服务端 CloudScript**，而且**价格 / 余额 / 持有物全在远端**（正本 §六·7 那几条老账）。
            //   ⇒ 按本笔的口径：**接上命中区 + 出声**（⛔ 不留哑钮，也不假装买成功）。
            Hit(p, "BuyHit", new PxRect(bx, by, bx + bw, by + bh),
                () => { OnBuyOriginalCardClicked(); }, QCdHit, buyBg, "UI_Button_Mulligan");

            // 🔴 **2026-10-17（D17 = 账上 A856）：「没有异画 ⇒ 整块关」** —— 判据 = 第一权威（反编译方法体）
            //    `d:/2/tools/decomp_full/AlternateArtPanel__Initialize.c` **:22-38**（现读）：
            //      · 卡拿不到 / `RawCardScript.HasAlternativeArtStyles(card) == false`（`cVar4 == '\0'`）
            //        ⇒ 填两个 0（`+0x48` / `+0x58`）之后 **`UnityEngine.GameObject.SetActive(gameObject, 0)`** ——
            //        **整块关**（不是「画成 0 张」）；
            //      · 有异画风格那一支才 `SetActive(…, 1)`（`:35-38`），再按 `IsAlternateArt` 分「原画 / 异画」两支取值。
            //    ⚠️ **注意不是「不建那几件」**：原版是**建好、整块关**（`SetActive(false)` 的节点留在树上）——
            //      我们照这个来（`Build()` 里子件照建，最后关根节点），自检才能两向都断（见 `CollectionScene.Run`）。
            //    ⚠️ 改前我们**不管有没有异画都画那一块**（画的是 `Alternate art / 0 of 0 / No alternate art`）——
            //      那几句「没有异画」的文案**现在都是死支**（面板被关掉，谁也看不见），**留着**是因为
            //      `has` 只有两个取值、不值得为它拆一遍版面。
            p.gameObject.SetActive(has);
        }

        /// <summary>异画面板上一颗换画圆钮（`left` = 左钮）。三层**各占一个递增的渲染队列**（见 `QCdIcon`）。
        /// 左钮的箭头**镜像**（原版 `UIFlippable(m_Horizontal=1)`，同 `CollectionWindow.BuildStyleArrow`）。
        /// 🔴 **接线按位置**：左 = 上一个(−1)、右 = 下一个(+1) —— 与收藏窗 Styles 页那份**同一条口径**
        /// （`Shell/CollectionWindow.cs:1570-1572` 的注释：脚本字段名 `leftStyleButton` 挂的其实是**右边**那颗
        /// ⇒ 那里也是按位置接的）。</summary>
        void BuildAltArrow(Transform p, bool left)
        {
            string side = left ? "Left" : "Right";
            float cx = left ? AltBtnCxL : AltBtnCxR;
            var r = new PxRect(cx - AltBtnW * 0.5f, AltBtnCy - AltBtnH * 0.5f,
                               cx + AltBtnW * 0.5f, AltBtnCy + AltBtnH * 0.5f);
            // 圆底盘画在**节点自己**身上（原版实读：`Select Art Button Left` 自己带
            //   `Image[UI_Button_Round_background]`、Simple + `preserveAspect`；同 `CollectionWindow.BuildStyleArrow`
            //   引 A1149 那条「它不是子件 `Background Round`」）。
            var round = MenuDraw.Rect(p, CardArt.MenuUi("UI_Button_Round_background"), r,
                                      "Select Art Button " + side, QCdRow, null, true);
            var node = round != null ? round.transform : MenuDraw.Node(p, "Select Art Button " + side, r);
            float ix = cx - AltArrowW * 0.5f, iy = AltBtnCy - AltArrowH * 0.5f;
            var ir = new PxRect(ix, iy, ix + AltArrowW, iy + AltArrowH);
            // 子件两层**必须逐级高一层队列** —— 三层同心（74.39² 圆底 / 56.86×58.13 黄底 / 同框箭头），
            //   同队列时「谁盖谁」由到相机的距离决定 ⇒ 黄底或箭头会凭空消失（同族实拍先例：
            //   `CollectionWindow.BuildStyleArrow` 那条「两颗钮里只看得见黄底、箭头没出现，而矩形断言全绿」）。
            var face = MenuDraw.Rect(node, CardArt.MenuUi("40k_general_bt_yellow"), ir, "Background", QCdText, null, true);
            var arrow = MenuDraw.Rect(node, CardArt.MenuUi("40k_general_bt_arrow"), ir, "Icon", QCdIcon, null, true);
            // 左钮**镜像**（原版 `UIFlippable(m_Horizontal=1)`）—— uv 的 u 从 1 到 0
            if (arrow != null && left) arrow.SetUvRect(new Rect(1f, 0f, -1f, 1f));
            // 命中区：① 名字**带左右**（两颗各一个，同名只会拿到第一颗）；② **先建、且不看上面建没建出来**
            //   —— 「这一格点不点得到」与「这一格的图画不画得出来」是两件事（同 `BuildCardHit` 那条 A157）。
            // 悬停/按下换图：原版那颗 `EverguildButton` 的 `m_Transition = 2 (SpriteSwap)`、
            //   `m_TargetGraphic` = 子件 `Background`（`40k_general_bt_yellow`）⇒ 把黄底那层当 `target`，
            //   图名走 `40k_general_bt_yellow`（高亮图 `_hover` / 按下图 `_pressed` 本地都在，
            //   出处 = 同 `CollectionWindow` 那两颗圆钮的接法）。取不到时 `WindowButton` 会记进
            //   `MissingSwapArt` / `MissingPressedArt`（自检会红），**不静默画成没反应**。
            Hit(node, "ArrowHit " + side, r,
                () => { ChangeAltStyle(left ? -1 : +1); }, QCdHit, face != null ? face : round,
                "40k_general_bt_yellow");
        }

        // ---- 计数条（`Card Counter`，原版 §三）----
        void BuildCounter(Transform panel)
        {
            var c = MenuDraw.Node(panel, "Card Counter", new PxRect(825.07f, 829.81f, 1094.93f, 909.19f));
            // 🔴 **2026-10-13（A691）**：`spares > 0` / `== 0` 在原版是**两棵不同的树**（不是同一棵改个名）——
            //   判据 = `decomp_full/CardCounterDisplay__Initialize.c`：`spares > 0` ⇒
            //   `singleCounterContent.SetActive(0)` + `duplicateCounterContent.SetActive(1)`（0x38 / 0x20）；
            //   `spares == 0` ⇒ **反过来**，并且**只填 `singleCardCounter`**（0x40，`SetSingleCounter` 同一个写法）。
            //   ⇒ 框、字号、auto 区间、结构**各是各的**（见 `CcSg*` 那一族常量）。
            bool dup = CardProgress.Spares(Card.Id, Card.Rarity) > 0;
            var rBox = dup ? new PxRect(CcBgL, CcBgT, CcBgR, CcBgB)      // `Duplicate Counter` 240×70.586
                           : new PxRect(CcSgL, CcSgT, CcSgR, CcSgB);     // `Single Counter`    170×50
            var box = MenuDraw.Node(c, dup ? "Duplicate Counter" : "Single Counter", rBox);
            MenuDraw.Rect(box, CardArt.MenuUi("40K_main_deck_card_counter"), rBox, "Background", QCdRow, null, true);
            // 🔴 **2026-10-17（A894）**：喂**带卡型那一档** —— 督军（`Type == "hero"`）的原版上限是
            //   **恒定 1**（`GetMaxCopiesInDeck` 那句在稀有度判断之前的 `return 1`，判据见 `CardProgress.DeckCap`）。
            //   ⚠️ **残留（如实标注）**：本段下面那个 `owned` 走 `CardProgress.Owned`，而它的「给足」公式
            //   （= 卡组上限 + 9）吃的是**不带卡型**那一档（`_owned` 缓存按 id 存、`AddOwned` 那一路也没有卡型）
            //   ⇒ **对 28 张非传说督军，这颗印的「多余副本」会比「按督军档」多 1**（9 变 10）。
            //   本颗**印进画面**的那个数（`x{inDeck}` = `min(拥有, 上限)`）**是对的**。
            int cap = CardProgress.DeckCap(Card.Rarity, Card.Type);
            int owned = CardProgress.Owned(Card.Id, Card.Rarity);
            int inDeck = Mathf.Min(owned, cap);
            int spares = owned - inDeck;
            // 🔴 **2026-10-13（A404）**：原来这三处 `wrapPx` 传的是 `0f` —— 而 `MenuDraw.Text` 的自适应那一段
            //   **整段写在 `if (wrapPx > 0f)` 里面**（`Shell/MenuDraw.cs` 的 `Text(...)`）⇒ 只传 `autoMinPx`
            //   等于传了**死实参**：这三颗**从来没开过自适应**（字段有、画面没有 —— 静默失败）。
            //   补上 `wrapPx` = **原版那一颗的 `m_SizeDelta.x`**（`73.283` / `28.02` / `44.09`，
            //   判据 → `资料/阶段二_卡片详情窗_原版规格.md:90-92`；原版三颗都带 `auto`）。
            //   ⚠️ 传 `wrapPx` 会**顺带把折行模式开成 `Normal(1)`**（`SetWrapWidth` 无条件写，见
            //   `TmpFont.SetWrapWidthRect`），而**原版这三颗都是 `折行=0`（`NoWrap`）**
            //   （逐颗实读：MB pid `1892` / `1848` / `1823`，`bundle_scenes_scenes_mainmenuwarpforge`，
            //   `m_TextWrappingMode = 0`）⇒ 紧接着 `SetWrapping(false)` 还原自己那一档
            //   （成对写法同 A205 / A34-F4 那一族；`SetWrapping` 顺带把版面推下去）。
            // 🔴 **2026-10-13（A574）**：三颗都补上第 10 参 `autoBasePx` = 原版那一颗的 **`m_fontSizeBase`**
            //   （36 / 36 / 35，逐颗现读 MB pid `1892` / `1848` / `1823`，见 `CcBase*` 那几个常量）。
            // 🔴 **2026-10-13（A575）**：三颗的**水平对齐档**照原版逐颗接 —— 原版 `m_HorizontalAlignment`
            //   = **4 / 2 / 1**（TMP 枚举 `Left=1, Center=2, Right=4, Justified=8, Flush=0x10`、
            //   `Geometry=0x20`，判据 = `Runtime/TMP/TMP_Text.cs:74-77`）⇒ **第 1 颗 `Right` · 第 2 颗 `Center` ·
            //   第 3 颗 `Left`**。而我们三颗一律是 `TmpFont.NewText` 的**出厂档 `Center`**
            //   （`MenuDraw.Text` 全程不设 `alignment`）⇒ 第 1、3 颗**在格内的落位**与原版不同（半格量级）。
            //   做法照本仓惯例走 **`MenuDraw.AlignLeft/AlignRight`（挪整块）**，⛔ **不是**去改 TMP 的
            //   `alignment` —— `Label.AlignLeftOn` 的注释写着：改 `alignment` **管不了折行之后每一行在块内怎么排**。
            //   ⚠️ **顺序不能反**：`SetWrapping` 会把版面推下去（`Battle/Label.cs` 的 `SetWrapping` 头那条「要在对齐/量宽之前调」「要在对齐/量宽之前调」）
            //   ⇒ **先 `SetWrapping(false)`、后 `Align*`**。
            //   ⚠️ 第 2 颗（`Slash`，原版 `Center`）**不调** `Align*` —— 调了反而偏（铁律 5·c：别一个档顶三颗）。
            //   ✅ **2026-10-13（A691）已另建**：上面这两件（base / 对齐）取的是原版 **`Duplicate Counter`**
            //      那一棵树的三个节点（pid 1892/1848/1823）—— 而 `spares == 0` 时原版走的是**另一棵树**
            //      `Card Counter/Single Counter`（本笔现读原版 prefab：框 **875,844.50→1045,894.50** ·
            //      `Counter` **fs35** · auto[**25~35**] · base **36** · `H=2(Center)`）⇒ **本笔照原版另建**
            //      （见下面 `if (!dup)` 那一支 + `CcSg*` 常量族）。
            //      原来我们只把 `Duplicate Counter` **改了个名**、几何/字号/结构全部沿用（= 两棵树当成一棵），
            //      那是 WM2 §七·1 记下的偏离，**本笔销账**。
            var rC = new PxRect(CcX1L, CcY1, CcX1R, CcY2);
            if (!dup)
            {
                // `Single Counter/Counter`（原版 MB pid `1893` · GO `Counter_227` · RT `1151`）：
                //   fs **35** · auto[**25~35**] · base **36** · `m_HorizontalAlignment = 2 (Center)` = 出厂档
                //   ⇒ **不调 `Align*`**（⛔ 别把下面那颗 `Counter` 的 `AlignRight` 抄过来 —— 铁律 5·c）；
                //   容器宽 = **170**（那颗 RT 是 stretch ⇒ 容器 = 解析后的父宽，见 `CcSgWrapX`）。
                //   文案 = **`x{0}` 填 `min(拥有, 卡组上限)`**：`Initialize.c` 这一支走的是 `System_String__Format(DAT_18425ce10, iVar2)`
                //   —— **和 `spares > 0` 支填 `cardCounter`（0x28）用的是同一个格式串符号**；
                //   该字面量 = `stringliteral.json` 里 RVA `0x425CE10` 的 `'x{0}'`（`0x18425ce10 − ImageBase 0x180000000`）。
                //   ⚠️ 多余副本数那个 `{0}`（RVA `0x4265D10`）**只喂 `duplicateCardCounter`**，这一支不印它。
                // ⚠️ 这一支**没有** `Slash` / `Duplicates text` / `Duplicate image` —— 原版那三件只长在
                //   `Duplicate Counter` 那棵树上（`Initialize.c` 只 `SetActive` 两支的根，不会给这一支补件）。
                // ⚠️ **如实标注（`资料/普查产出_1013/WM3_SingleCounter.md` §七·1）**：原版这颗 MB `1893`
                //   还有 **`m_VerticalAlignment = 1024 (Bottom)`**（对照：`Duplicate Counter` 那三颗都是 `512 (Middle)`）。
                //   ✅ **2026-10-16（A712 阶段 2）销账**：那条「要真做需要在 `Battle/Label.cs` 加一个 `AlignBottomOn`、
                //   白名单里没有那个文件 ⇒ 只报不改」**已过期** —— A712 阶段 1 把**垂直档那个口**
                //   （`Label.SetVAlign(档, 框高)`，`AlignBottomOn` 只是当时设想的名字）落进了 `Battle/Label.cs`
                //   ⇒ 本处现在**真落**（见下面那句调用；框高 = `Single Counter` 170×50 的 50）。
                var lbSg = MenuDraw.Text(box, rBox, "x" + inDeck, Color.white, "Counter",
                              CcSgPx, QCdText, CcSgWrapX, CcSgMin, CcSgMax, CcSgBase);
                // 成对写法同上（传 `wrapPx` 顺带把折行模式开成 `Normal(1)`，而原版这颗 `m_TextWrappingMode = 0`）。
                if (lbSg != null) lbSg.SetWrapping(false);
                // 🆕 **2026-10-16（A712 阶段 2）**：纵向档 = 原版这颗 `Single Counter/Counter` 的
                //   **`m_VerticalAlignment = 1024 (Bottom)`**（判据 = 上面那条如实标注里的 MB `1893` 直读；
                //   原版 prefab 框 **875,844.50→1045,894.50** ⇒ 170×50）。`Bottom` 吃框高 ⇒ 传 `rBox`。
                //   文案恒 `"x"+在卡组里的张数`（单行，且上面刚 `SetWrapping(false)`）⇒ 不踩多行那个坑。
                MenuDraw.SetVAlign(lbSg, Label.VAlign.Bottom, rBox);
                return;
            }
            var lbC = MenuDraw.Text(box, rC, "x" + inDeck, Color.white, "Counter",
                          CcX1Px, QCdText, CcWrapX1, 25f, 0f, CcBaseX1);
            if (lbC != null)
            {
                lbC.SetWrapping(false);
                MenuDraw.AlignRight(lbC, rC);        // 原版 `…/Counters/Counter` 的 `align = 4 (Right)`
            }
            {   // ⟵ 走到这里就是 `dup == true`（`!dup` 那一支上面已 `return`）；这个花括号只是收 `Slash`/`Duplicates text`/
                //    `Duplicate image` 三个 `var` 的作用域，**不是** `if` 体（A691 把原来的 `if (dup)` 换成早返回，
                //    免得把那一大段整块缩进一级、把 A574/A575 的 diff 冲掉）。
                var rSl = new PxRect(CcSlL, CcY1, CcSlR, CcY2);
                var lbSl = MenuDraw.Text(box, rSl, "/ ", Color.white, "Slash", CcSlPx, QCdText,
                              CcWrapSl, 25f, 0f, CcBaseSl);
                if (lbSl != null) lbSl.SetWrapping(false);   // 原版 `align = 2 (Center)` = 出厂档 ⇒ 不调 `Align*`
                var rX2 = new PxRect(CcX2L, CcX2T, CcX2R, CcX2B);
                var lbX2 = MenuDraw.Text(box, rX2, spares.ToString(), Color.white,
                              "Duplicates text", CcX1Px, QCdText, CcWrapX2, 18f, 0f, CcBaseX2);
                if (lbX2 != null)
                {
                    lbX2.SetWrapping(false);
                    MenuDraw.AlignLeft(lbX2, rX2);   // 原版 `…/Slash/Duplicates text` 的 `align = 1 (Left)`
                }
                MenuDraw.Rect(box, CardArt.MenuUi("40k_general_icon_card_amount"),
                              new PxRect(CcImL, CcImT, CcImR, CcImB), "Duplicate image", QCdRow, null, true);
            }
        }

        // ---- 通配符条（`Wildcard Segment` + `WIldcard Counter`）----
        void BuildWildcards(Transform panel)
        {
            MenuDraw.Rect(panel, CardArt.MenuUi("40k_topmarquee_currency_display_BW"),
                          new PxRect(WcBgL, WcBgT, WcBgR, WcBgB), "Wildcard Bg", QCdRow);
            for (int i = 0; i < WildcardIcons.Length; i++)
            {
                float x = WcX0 + WcStep * i;
                MenuDraw.Rect(panel, CardArt.MenuUi(WildcardIcons[i]), new PxRect(x, WcBgT, x + 30f, WcBgB),
                              "Wildcard Icon " + i, QCdRow, null, true);
                // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版 `WIldcard Counter/Counters/*/Icon`
                //   4 件全是 PA=1 + Simple（RT1211/1354/1344/1583），贴图 **42×51 / 41×51** 塞进 30×44
                //   ⇒ 原版实绘 **30×36.4（37.3）**，我们拉伸成 30×**44** ⇒ **高 ×1.18~1.21**。
                //   ⚠️ 同一件在收藏窗（`Shell/CollectionWindow.cs` 里那处 `keepAspect` 补）与卡组编辑（`Deck/DeckRuntime.cs` 里那处 `keepAspect` 补）也是同一错，三处一起修。
                // 🔴 **2026-09-28 用户拍板：万能卡数字一律恒定 `99`**（原来这里写 `9999`）。
                //    原版这四个数是 `WildcardDisplay` 的库存直出、跟着**指针悬停那张卡**的阵营走；
                //    单机没有发放源 ⇒ 三处（详情窗 / 收藏窗 / 卡组编辑）统一写 99。
                //    判据 → `项目任务.md` §三 第 15 条 第 29 项。
                MenuDraw.Text(panel, new PxRect(x + 30f, WcBgT, x + 71f, WcBgB), "99", Color.white,
                              "Wildcard Count " + i, WcPx, QCdText);
            }
            var fac = DeckRuntime.FactionIcon(Card.Faction);
            if (!string.IsNullOrEmpty(fac))
                MenuDraw.Rect(panel, CardArt.MenuUi(fac), new PxRect(1470f, 71f, 1550f, 156f),
                              "Army Icon", QCdRow, null, true);
        }

        // ============================================================ 行为

        /// <summary>点遮罩以外的东西不该关窗 —— 这条给自检/上层用。</summary>
        public void ToggleLore()
        {
            LoreVisible = !LoreVisible;
            RefreshLore();
            Debug.Log("[CardDetail] `Show Card Text` ⇒ 效果文字条 " + (LoreVisible ? "显示" : "藏起"));
        }
        void RefreshLore()
        {
            var bg = Find(transform, "FlavourTextBG");
            if (bg != null) bg.gameObject.SetActive(LoreVisible);
        }

        /// <summary>下缘那两颗钮**按卡决定建不建**（原版 `ShowCard` 与 `CardSwapFinished` 里那两条
        /// `SetActive`）。判据只此一份 → `CardButtons`（那里写了原版出处与我们的等价物：
        /// **语音钮** = 这张卡有没有声音；**眼睛钮** = `type` 是 `unit`/`hero`）。
        /// ⚠️ 换位收尾要跟着换 —— 认的是**新前台**那张（原版 `CardSwapFinished` 同款）。</summary>
        void RefreshButtons()
        {
            if (_voiceBox != null) _voiceBox.gameObject.SetActive(HasCard && CardButtons.HasVoice(Card));
            if (_eyeBox != null) _eyeBox.gameObject.SetActive(HasCard && CardButtons.HasTextButton(Card));
        }

        /// <summary>🆕 **2026-10-12（A1699）切异画档** —— 原版 `AlternateArtPanel.NavigationButtonClick(bool isLeft)`
        /// → `SwapStyle(bool isLeft)`（判据全文 → `AltStyleIndex` 那一节的注释）。
        ///
        /// <para>口径逐条照原版：**① 只在「有异画风格」时动**（没有时那两颗钮的原版实现是整块关着的）；
        /// **② `index ± 1`**（`dir`）+ **③ 在 `0..Count` 上回绕**（0 = 原画，1..Count = 该卡的第 N 种异画）；
        /// **④ 换完重画** —— 原版是 `SetStyle` 里 `BasicCardUI.SetCardImage(前台那张, 该风格图)`，
        /// 我们这一侧等价物 = 重跑 `Build()`（`CardView` 的立绘层在 `Create` 那一刻按 `artOverride` 建，
        /// 见 `BuildCardStack` 里那句）。</para>
        /// <para>⚠️ **原版 `NavigationButtonClick` 那两下 `set_interactable(0)` / `(1)` 我们没有复刻**：
        /// 它挡的是「切图是异步加载、切到一半再点会串档」；我们这侧是**同步取 `Resources` 缓存**
        /// （`CardArt.AltArt` → `Get` 走 `_cache`），不存在那个窗口 —— **如实标注为偏离**，
        /// ⛔ 不要为了「像」而加一个没有对象的闸。</para></summary>
        public void ChangeAltStyle(int dir)
        {
            var styles = AltStylesOf(Card != null ? Card.Id : null);
            if (styles.Count == 0)
            {
                Debug.Log("[CardDetail] 切异画：这张卡**一种异画都没有** ⇒ 什么都不做"
                        + "（原版那一支里整块面板本来就是关着的；出声，不静默）");
                return;
            }
            int n = styles.Count + 1;                                  // 0 = 原画 + N 种异画
            AltStyleIndex = ((AltStyleIndex + dir) % n + n) % n;       // 回绕（原版 `SwapStyle.c:30/:36`）
            string which = AltStyleIndex == 0
                ? "原画"
                : CollectionWindow.StyleLabel(styles[AltStyleIndex - 1].Style);
            Debug.Log("[CardDetail] 切异画 → 第 " + AltStyleIndex + "/" + styles.Count + " 档「" + which
                    + "」（原版 `AlternateArtPanel.SwapStyle`：`index ±1` 后回绕 · 0 = 原画；"
                    + "换的是**前台那张**的图 —— 判据 `SetStyle.c:47-52` 的 `BasicCardUI.SetCardImage`）");
            RebuildKeepingState();
        }

        /// <summary>🆕 **2026-10-12（A1699）**：点「创建副本」那颗钮 —— **点了什么都不改，只出声**。
        ///
        /// <para>🔴 **口径：用户 2026-10-12 裁定「只出声、不做机制」**（= 当时列的候选 **②**，已定案；
        /// ⛔ **不要去接 `DoCraft` / 不要去实现扣通配符**）。理由（逐条）：扣万能卡那半边原版走
        /// `MenuDeck/Craft/*` + 服务端，**本地没有万能卡发放源** —— 真做出来只会是「凭空加副本」。</para>
        /// <para>🔴 **配套（同一裁定）**：`BuildCrafting` 那一行消费文案**必须让玩家看出这块没实现**
        /// ⇒ 已把 `Craft Explanation` 改成
        /// `This will consume a wildcard  Not implemented in this build*`（记号 = 本工程现成的末尾 `*`，
        /// 判据/同族先例 → 那一行的注释）。⇒ **改这一句文案之前先读那条注释**，两者是一件事。</para>
        /// <para>⚠️ **不许静默**：本方法**必须**打成一句日志（下面那句），它是这一条链的「出声」落点，
        /// 也是 `Editor/CollectionScene.cs` 那条「点了不影响拥有数」断言的配对。</para></summary>
        public void OnCraftClicked()
        {
            Debug.Log("[CardDetail] **本版未实现：创建副本** —— 那颗钮点了**什么都不做**"
                    + "（「" + (Card != null ? Card.Name : "?") + "」的拥有数仍是 "
                    + CardProgress.Owned(Card.Id, Card.Rarity) + "、万能卡一张没扣）。"
                    + "用户 2026-10-12 裁定「只出声、不做机制」；引擎里那份半成品实现是 "
                    + "`DoCraft()`（加副本、**不扣**万能卡），本版**没有**接它。"
                    + "⚠️ 面板上那句文案已按红线「不许静默失败」缀上 `Not implemented in this build*`"
                    + "（记号同卡面的 `*`）—— 改文案与本方法是一件事，别只改一半。");
        }

        /// <summary>🆕 **2026-10-12（A1699）**：点异画面板那颗价签（`Buy Original Card`）。
        /// 原版那条链**本地走不通** —— `AlternateArtPanel__BuyOriginalCard.c` 是
        /// `WindowsManager.ShowPopUp(<确认弹窗>)` + `PlayfabWrapper.GenericCloudScriptHandler(0x385, …)`
        /// （**服务端 CloudScript**），价格还来自 `ItemsConfig.GetOriginalArtPrice`（LiveOps 配置，本地没有）。
        /// ⇒ 照本笔口径：**接上命中区 + 出声**（⛔ 不留一颗「看起来能点、点下去什么都不发生」的钮）。</summary>
        public void OnBuyOriginalCardClicked()
        {
            Debug.Log("[CardDetail] **本版未实现：购买原卡面**（`Buy Original Card`）—— 那颗钮点了**什么都不做**"
                    + "（原版走 `PriceDisplayButton` → `AlternateArtPanel.BuyOriginalCard` → "
                    + "`PlayfabWrapper.GenericCloudScriptHandler` = **服务端**，且价格在 "
                    + "`ItemsConfig.GetOriginalArtPrice`（LiveOps 配置，本地取不到））。");
        }

        /// <summary>创建副本：**拥有数 +1**。⚠️ 单机资源固定 9999 ⇒ **不扣万能卡**，出声。
        ///
        /// <para>🆕 **2026-10-12（A1699）：本版【没有】把这一支接到那颗钮上**（钮走 `OnCraftClicked`）。
        /// **保留不删** —— 它是「真扣通配符并加副本」那一条候选里**已有的一半**：
        /// 要接回去只需把 `BuildCrafting` 末尾那句的 lambda 换回 `DoCraft`，
        /// 另一半（扣通配符）本地**没有发放源**（正本 §六·7 那几条老账）⇒ 得先裁「万能卡是不是也走『给足』那套口径」。
        /// 🔴 **2026-10-12 当天的第二条裁定已定了另一条**：用户选了「**只出声、不做机制**」⇒
        /// 本方法**本版不会被执行**，也**不要去接**；上面那段只是留给出账时看的回退路。</para></summary>
        public void DoCraft()
        {
            CardProgress.AddOwned(Card.Id, Card.Rarity, 1);
            Debug.Log("[CardDetail] 创建副本：「" + Card.Name + "」拥有数 → "
                      + CardProgress.Owned(Card.Id, Card.Rarity)
                      + "（单机版**不扣万能卡**，出声；原版 `MenuDeck/Craft/*` 那套走服务端）");
            RebuildKeepingState();
        }

        /// <summary>升级：够副本才升（**只吃多余副本**，同原版 `拥有数 >= 卡组上限 + 需要张数`）。
        ///
        /// <para>🆕 **2026-10-12（A1699）**：那颗钮现在是 `Interactable = false`（用户口径「默认最高级」）
        /// ⇒ **本方法从那颗钮上到不了**（`WindowButton.Click()` 在派发之前就返回了）。
        /// 保留它是因为它是一条**真实现**（等级 +1 会走 `RebuildKeepingState` 重画），
        /// 且 `UpgradePanel` 那套「够不够 / 满级」的判据本来就写在 `CardProgress` 里 ——
        /// 将来口径若改成「可升级」，只需把 `BuildUpgrade` 里那一句 `Interactable = false` 去掉。</para></summary>
        public bool DoUpgrade()
        {
            if (CardProgress.Level(Card.Id) >= CardProgress.MaxLevel)
            {
                Debug.Log("[CardDetail] 已经满级（" + CardProgress.MaxLevel + " 级）⇒ 升不了");
                return false;
            }
            if (!CardProgress.CanUpgrade(Card.Id, Card.Rarity))
            {
                Debug.Log("[CardDetail] 副本不够 ⇒ 升不了（多余副本 "
                          + CardProgress.Spares(Card.Id, Card.Rarity) + " < 需要 "
                          + CardProgress.NeedCopies[CardProgress.Level(Card.Id) + 1] + "）");
                return false;
            }
            CardProgress.SetLevel(Card.Id, CardProgress.Level(Card.Id) + 1);
            Debug.Log("[CardDetail] 升级：「" + Card.Name + "」→ 等级 " + CardProgress.Level(Card.Id)
                      + "（**不扣货币**：单机资源固定 9999；升级表数值**是我们编的**）");
            RebuildKeepingState();
            return true;
        }

        /// <summary>播这张卡的单位语音（复用 `VoiceLines` —— **只此一处**，不另写后缀表）。
        /// ⚠️ 认的是**前台那张**（`FrontDef`）—— 原版换位收尾会 `SetCardVoiceOver(新前台)`
        /// （判据 → 正本 §9·6 ⑤），所以点了相关卡之后这颗钮该播**新前台**的语音。</summary>
        public bool PlayVoice()
        {
            LastVoiceFile = null;
            var c = FrontDef ?? Card;
            if (c == null) return false;
            string key = !string.IsNullOrEmpty(c.Id) ? c.Id : c.Name;
            if (!VoiceLines.Has(key))
            {
                Debug.Log("[CardDetail] 「" + c.Name + "」没有单位语音（`VoiceLines.Has` 为假）—— 这次没声音");
                return false;
            }
            string file, text;
            if (!VoiceLines.TryPick(key, VoiceLines.ForVoiceOver, null, out file, out text)) return false;
            var clip = VoiceLines.Clip(file);
            if (clip == null) return false;
            WarpforgeVFX.WFSoundPlayer.Play(clip, true, 1f, 1f);
            LastVoiceFile = file;
            return true;
        }

        void RebuildKeepingState()
        {
            Build();
            LastOpened = this;
        }

        // ============================================================ 小工具

        Transform PanelBox(Transform panel, string name, float y1, float y2)
        {
            var p = MenuDraw.Node(panel, name, new PxRect(PanL, y1, PanR, y2));
            // 🔴 **原版这里确实是 `UI_Deck_Information_Back` + `type=Sliced`**（2026-09-24 实读
            //    `工具/menu_dump.py … "Crafting Panel"` 确认），但那块面板只有 **450 宽**，
            //    而这张图的 `m_Border=(42,363,655,81)` 左+右 = **697 > 450** ⇒ **九宫格根本放不下**
            //    （原版 Unity 会把它挤成「左右边框各占一半」，我们这套 `CreateNineSlice` 会**退回单块**）。
            //    ⇒ 我们**显式选「单块拉伸」**（不静默、也不吐一串警告），**如实记成一处偏差**，
            //    见 `项目资料` 那边的 §三 第 15 条。
            var tex = CardArt.MenuUi("UI_Deck_Information_Back");
            if (tex == null) Debug.LogWarning("[CardDetail] 面板底图取不到：UI_Deck_Information_Back");
            else MenuDraw.Rect(p, tex, new PxRect(PanL, y1, PanR, y2), "Background", QCdRow);
            return p;
        }

        void Title(Transform p, string nodeName, string text, float x1, float y1, float x2, float y2, float px)
        {
            // 限宽换行 + 自适应（原版这几处都是 `auto(min-max)` 的 TMP）
            // 🆕 **2026-10-12（A333 + A336③）**：三处面板标题的原版读数**逐值相同** ——
            //   `m_fontSizeMax = **42**`（三颗都 42，**其中两颗的标称字号不是 42**：41.4 / 31.7 ⇒
            //   旧写法把 `fontPx` 当上限，**天花板比原版矮**）· `m_fontSizeBase = **36.0**` · `m_fontSizeMin = 10`。
            //   判据 = `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/` 逐颗按文案现读：
            //     `'Create a copy of this card'` → `MonoBehaviour_1772.json` = `fs 42.0 · auto[10~42] · base 36.0`
            //     `'Upgrade this card\nto level {'` → `MonoBehaviour_1883.json` = `fs 41.4 · auto[10~42] · base 36.0`
            //     （另一份 `MonoBehaviour_1776.json` 是 `fs 31.7 · auto[10~42] · base 36.0` = **异画面板那一颗**）
            MenuDraw.Text(p, new PxRect(x1, y1, x2, y2), text, Color.white, nodeName, px, QCdText, x2 - x1, 10f,
                          42f, 36f);
        }

        Transform Hit(Transform parent, string name, PxRect r, System.Action onClick, int q = QCdHit,
                      ImageQuad target = null, string art = null, string hoverArt = null)
        {
            var hit = MenuDraw.Node(parent, name, r);
            var quad = ImageQuad.Create(hit, CardArt.Solid(), Vector3.zero, LayoutSpace.Px(r.H),
                                        new Vector2(0.5f, 0.5f), "Hit");
            if (quad != null)
            {
                quad.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                quad.SetTint(new Color(0f, 0f, 0f, 0f));
                quad.SetRenderQueue(q);
            }
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = onClick;
            if (target != null) wb.Bind(target, art, hoverArt);
            return hit;
        }
        Transform Hit(Transform parent, string name, PxRect r, System.Func<bool> onClick, int q)
        {
            return Hit(parent, name, r, () => { onClick(); }, q);
        }
    }
}
