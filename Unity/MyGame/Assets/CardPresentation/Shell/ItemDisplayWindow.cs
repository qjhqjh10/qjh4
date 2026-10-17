// ItemDisplayWindow.cs — **物品展示窗**（原版 `ItemDisplayWindow : GameWindow`）
//
// ============================ 出处（唯一判据，全部本地现读） ============================
// · prefab 树 / 矩形 / 组件 / 文字档位：
//     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Item Display Window" --depth 8 --relative --md`
//     （2026-10-17 现读；同一份记录在 `资料/普查产出_1017/UI/T18_ItemDisplayWindow_prefab.md`）
// · 窗参（MB 逐字段实读）= `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3926313906056799325.json`：
//     `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` · `updateNavPanel = 0` ·
//     `extraScaleSmallScreen = 1.0` · `openSound = {0,0}` · `closeSound = {0,0}` ·
//     `useDefaultCloseSoundIfNull = 1`
//   ⇒ 关窗音走家族默认（`GameWindow.useDefaultCloseSoundIfNull` 的既有实现），本窗**不赋任何 clip**。
// · 行为（第一权威 = 反编译 `d:/2/tools/decomp_full/`）：
//   · `CardbackCollectionTab__OnItemSelected.c`：点一张卡背 ⇒ `ItemDisplayWindowContext(item, 1)` →
//     `WindowsManager.OpenWindow<ItemDisplayWindow>` —— **这就是 T25 那个入口**（不是猜的）。
//   · `ItemDisplayWindowContext__.ctor.c`（符号错配成 `UpgradeCardSignal___ctor`）：
//     `+0x10 = item`（`CosmeticItem`）· `+0x18 = amount`（int）。
//   · `ItemDisplayWindow__Open.c` 逐句：
//       ① `GameWindow.Open()`
//       ② `ItemDrawer.Draw(itemHolder, item, amount)` —— 抽屉自己把 `components.image.sprite` 换成
//          `item.CosmeticSprite`（`CardbackDrawer__Draw.c` 只有这一句）
//       ③ `loreText.text = I2.GetTermTranslation(item + 0x20)`（= `ObtainableItem.itemLoreKey`）
//          空白 ⇒ `lorePanel.gameObject.SetActive(false)`
//       ④ `FlavourTextSO.GetClanFlavorBackground(item + 0x2c)`（= `cardArmy`）⇒ `lorePanel.sprite`
//       ⑤ `ItemInformationPanel.Initialize(item, drawer.Options)`
//   · `ItemInformationPanel__Initialize.c`：`nameText ← item.LocalizedName`（仅当 `options.ShowName`）·
//     `descriptionText ← I2(item + 0x18)`（= `itemDescriptionKey`）·
//     `typeText ← I2(options.TypeString)`；三格**全空**才把面板关掉。
//   · `ItemDisplayWindow__OnEnable.c`：把 `GameWindow.Close` 挂到 `closeButton.onClick` 上 ——
//     ⚠️ prefab 里 `BackgroundCloseButton.window` 的 PPtr 是 **0**（`BackgroundCloseButton__OnPointerClick.c`
//     先判它、非空才调 `Close()`），**所以「点暗底关窗」靠的是 OnEnable 这一挂**。我们这一侧 = `MenuDraw.ShadeHit`。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版） ============================
// · **Title** = `Loc.BulkOr("Cardbacks/<nameTextReference>", 反推名)`。
//   🔴 `nameTextReference` 与 SO 名**不等**（243 个卡背 SO 里 **56 个不同**，多为 `_AA_HB` 异画指向本体的名），
//   而 `Resources/Cardbacks.json` 那一行**只存 `so`**（= `m_Name`）⇒ 本窗只能拿 **`so`** 拼键：
//   187/243 命中；其余 56 个走 `BulkOr` 的兜底（+ 出声一次）。⛔ 别把「so」写成「nameTextReference」。
// · **Description** = `Loc.T("Item_Description/CardbackGeneric")`（官方两列逐字，2026-10-17 现读）。
//   243 个卡背 SO 里 **237 个**用这个键、**6 个**（Ranked/Skirmish Tier1–3）用 `Item_Description/CardbackSeasson`
//   —— 后者**官方表里没有**，而 `Cardbacks.json` 不存 `itemDescriptionKey` 这一列
//   ⇒ 本窗对这 6 张**同样显示 Generic 文案**（如实标注；补它要动 `工具/gen_cardbacks.py`，不在本轮白名单）。
// · **Type** = 原版 `ItemDrawerOptions.TypeString`（在 `ItemDrawerConfig` 资产里）。🔴 **查不到**：
//   全 `assets_full` 84 个 bundle 的 `MonoBehaviour/*.json` 逐字节搜 `"drawers"` / `"drawerReference"` /
//   `"typeString"` **零命中**（该资产不在解包树里）⇒ **留空 + 出声一次**，⛔ 不编一个「Cardback」顶上。
// · **LoreText** = `Loc.LoreByUniqueId(uniqueId)`（`Cosmetic_Lore/<uniqueId>`；口早就开着，本窗是**首个消费点**）。
//   没有 ⇒ **整块 `FlavourTextBG` 关掉**（照 `ItemDisplayWindow__Open.c` ③ 的 `SetActive(!IsNullOrWhiteSpace)`）。
// · **FlavourTextBG 的底图** = `CardArt.FlavorBg(army)`（= 原版 `FlavourTextSO.GetClanFlavorBackground` 那张表，
//   判据 → 那个方法的 doc）。`m_PreserveAspect = 0`（实读）⇒ **拉伸**铺满 1320×178。
//   节点上那颗 `TransformScalerBySmallScreenUI` 的 **`menuScale = 1.12`**（MB `-6000825424073008221` 实读）
//   ⇒ 小屏开关开着时整块按 1.12 放大，pivot (0.5, 0)（底边中点）。
// · **Item Holder 里的卡背** = `CardArt.Cosmetic(名)`。画法走 `MenuDraw.Rect(..., keepAspect: true)`
//   （= `CardbackFace` 的两段式，与收藏窗卡背格同一份口径）。⚠️ **近似**：原版那一格是
//   `ItemDrawer.Draw(itemHolder, …)` 实例化出来的 `Cardback Drawer`，其根带
//   `AspectRatioFitter(m_AspectMode = 2 高控宽, m_AspectRatio = 0.66)`（26 份 prefab 逐份同值，锚点 (0,0)-(1,1)）
//   ⇒ 借 650×650 的 Holder 得到 **429×650** 的画框；抽屉内部那层 `Content`(712×1080 → `UIScaleToFit`)
//   与 `Image`(锚点 -0.075~1.075、`preserveAspect`) 的二级几何**没有复刻**（判据不足以定到 px）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ItemDisplayWindow`（`GameWindow` 子类）—— **大图 + 右侧信息面板 + 底部风味条**。
    /// <para>本轮（2026-10-17 · T18/T25）唯一的入口 = 收藏窗卡背页点一张卡背
    /// （原版 `CardbackCollectionTab.OnItemSelected`）。</para>
    ///
    /// <para>🔴 **它不在任何一页的层带里**：自成一档 **3362–3370**
    /// （`ReferralPopupWindow` 3348–3361 之上、`DuelPopupWindow` 3400–3405 之下 —— 层带不许重叠）。</para></summary>
    public class ItemDisplayWindow : GameWindow
    {
        // ============================================================ 队列档（自成一档 · 3362–3370）
        // 顺序 = 原版兄弟序：根 = [Menu Dark Background, Options, Card Display, Item Holder]。
        // ⚠️ `MenuDraw.ShadeHit` 建出来的命中区**用的是传进去的 `qShade`**（没有单独的命中档形参）；
        //    同档不会打架 —— 只有挂 `WindowButton` 的节点才进命中候选，那层视觉 quad 不挂。
        public const int QShade = 3362,        // 压暗整屏（`Menu Dark Background` 的 quad + `BackgroundHit`）
                         QPanel = 3364,        // `Options/Panel` 九宫底
                         QPanelText = 3365,    // `Title` / `Description` / `Type`
                         QFlavour = 3366,      // `Card Display/FlavourTextBG/Image`
                         QLoreText = 3367,     // `Card Display/FlavourTextBG/LoreText`
                         QItemArt = 3368;      // `Item Holder` 里的卡背
        /// <summary>本窗**内容命中区**那一档（压暗层命中区必须**严格低于**它 —— `MenuDraw.ShadeHit` 现场核）。
        /// 吸收层由 `MenuDraw.Absorb` 取 `QHit − 1` = 3369。</summary>
        public const int QHit = 3370;

        // ============================================================ 几何（dump 现读 · 1920×1080 画布 px）
        /// <summary>`Menu Dark Background`：4574.60×2572.36，**以屏幕中心为中心**（−1327.30,−746.18 → 3247.30,1826.18）。</summary>
        public const float ShadeW = 4574.60f, ShadeH = 2572.36f;
        /// <summary>`Options/Panel`（392.93×500）。</summary>
        public const float PanelL = 1263.53f, PanelT = 298.35f, PanelR = 1656.47f, PanelB = 798.35f;
        /// <summary>`Title`（314.35×50）· `Description`（314.35×300）· `Type`（314.35×50）—— 三格首尾相接。</summary>
        public const float TextL = 1302.83f, TextR = 1617.17f;
        public const float TitleT = 323.35f, TitleB = 373.35f, DescB = 673.35f, TypeB = 723.35f;
        /// <summary>`Card Display`（752×868，空容器）。</summary>
        public const float CardL = 584f, CardT = 106f, CardR = 1336f, CardB = 974f;
        /// <summary>`Card Display/FlavourTextBG`（1320×178）与 `LoreText`（1250×93.47）。</summary>
        public const float FlavourL = 300f, FlavourT = 880.34f, FlavourR = 1620f, FlavourB = 1058.34f;
        public const float LoreL = 335f, LoreT = 922.61f, LoreR = 1585f, LoreB = 1016.08f;
        /// <summary>`Item Holder`（650×650）。</summary>
        public const float HolderL = 635f, HolderT = 215f, HolderR = 1285f, HolderB = 865f;
        /// <summary>`Cardback Drawer` 根的 `AspectRatioFitter`（高控宽 · 0.66）⇒ 650 高的抽屉框宽 **429**。</summary>
        public const float ItemBoxRatio = 0.66f;
        /// <summary>`FlavourTextBG` 上那颗 `TransformScalerBySmallScreenUI.menuScale`（MB 实读 1.12）。</summary>
        public const float FlavourMenuScale = 1.12f;

        /// <summary>`Menu Dark Background` 的 `Image.m_Color`（实读 (0,0,0,0.773)）。</summary>
        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.773f);

        /// <summary>`Options/Panel` 的底图（`UI_Deck_Selection_Back` 439×664 · 九宫 (0,325,0,35) · Sliced · tint 白）。</summary>
        public const string ArtPanel = "UI_Deck_Selection_Back";
        public const float PanelTexW = 439f, PanelTexH = 664f;

        /// <summary>原版 `CosmeticItem.itemDescriptionKey`（卡背那一批 237/243 用这一条）。</summary>
        public const string DescTerm = "Item_Description/CardbackGeneric";

        // 文字档位（prefab 逐颗实读）
        public const float TitleFont = 37f, TitleFontMin = 10f, TitleFontMax = 37f, TitleFontBase = 36f;
        public const float DescFont = 35f,  DescFontMin = 10f,  DescFontMax = 35f,  DescFontBase = 36f;
        public const float TypeFont = 35f,  TypeFontMin = 10f,  TypeFontMax = 35f,  TypeFontBase = 36f;
        public const float LoreFont = 32f,  LoreFontMin = 12f,  LoreFontMax = 32f,  LoreFontBase = 36f;

        // ============================================================ 数据（= 原版 ItemDisplayWindowContext + 已解析好的文本）
        /// <summary>本窗要铺的一份内容。**文本已经本地化好**（窗里不再查词条，除 `Type` 的空判定）。
        /// 建法见 <see cref="ForCardback"/>。</summary>
        public struct Context
        {
            public string Title;        // 物品名（原版 `CosmeticItem.LocalizedName`）
            public string Description;  // 原版 `itemDescriptionKey` 的词条值
            public string Type;         // 原版 `ItemDrawerOptions.TypeString` 的词条值（查不到 ⇒ null）
            public string Army;         // `CardArmy` 枚举名（`CardArt.FlavorBg` 的键）
            public string Lore;         // 原版 `itemLoreKey` 的词条值（null ⇒ 整块 FlavourTextBG 关）
            public Texture2D Art;       // `Item Holder` 里那张
            public string ArtName;      // 出声用的名字（取不到图时报告它）
        }

        /// <summary>出厂值 = prefab 里那三颗 TMP 的原文（`Title` / `New Text` / `Type`），无图、无 lore。
        /// 走到这一档 = 调用方没给内容（原版此时是「空上下文」）。</summary>
        public static Context DefContext()
        {
            return new Context
            {
                Title = "Title", Description = "New Text", Type = "Type",
                Army = null, Lore = null, Art = null, ArtName = null,
            };
        }

        static Context FillDef(Context? src) { return src.HasValue ? src.Value : DefContext(); }

        /// <summary>最近一次开出来的那一扇（自检读口，同 `GenericOptionsPanel.LastOpened`）。</summary>
        public static ItemDisplayWindow LastOpened { get; private set; }

        Context _ctx = DefContext();
        /// <summary>当前铺的那一份（自检读口）。</summary>
        public Context Ctx { get { return _ctx; } }

        // 自检读口（节点引用）
        Transform _panel, _flavourBg, _holder;
        Label _title, _desc, _type, _lore;

        // ============================================================ 从卡背数据建 Context
        /// <summary>把 `Resources/Cardbacks.json` 里的一条卡背转成本窗的内容（= 原版
        /// `CardbackCollectionTab.OnItemSelected` 构造 `ItemDisplayWindowContext(item, 1)` 那一跳的等价物）。
        /// <para>⚠️ 查不到的每一格都**出声**（红线：不许静默失败），⛔ 不拿别的字顶（尤其不拿 lore 顶 Description）。</para></summary>
        public static Context ForCardback(string cardbackName)
        {
            var c = new Context { ArtName = cardbackName, Title = "", Description = null, Type = null, Army = null, Lore = null };
            c.Art = CardArt.Cosmetic(cardbackName);
            if (c.Art == null)
                Debug.LogWarning("[ItemDisplay] 卡背图取不到：`" + cardbackName + "`（`Resources/Art/cardbacks/` 里没有）"
                               + " ⇒ `Item Holder` **这一格不画**");

            var it = CardbackTable.Find(cardbackName);
            if (it == null)
            {
                Debug.LogWarning("[ItemDisplay] `Resources/Cardbacks.json` 里没有卡背 `" + cardbackName + "`"
                               + " ⇒ 阵营/uniqueId 都取不到：标题用反推名、Description / Lore / 风味底图**全空**");
                c.Title = ReverseName(cardbackName);
                return c;
            }

            c.Army = it.army;
            // 原版 `CosmeticItemCardback.LocalizedName` = I2(`"Cardbacks/" + nameTextReference`)。
            // ⚠️ 我们这一行只有 `so`（= 原版 m_Name；56/243 与 nameTextReference 不同）⇒ 见文件头那条。
            c.Title = Loc.BulkOr("Cardbacks/" + it.so, ReverseName(it.so));
            c.Description = Loc.HasEntry(DescTerm) ? Loc.T(DescTerm) : null;
            if (c.Description == null)
                Debug.LogWarning("[ItemDisplay] 词条表里没有 `" + DescTerm + "` ⇒ `Description` 那一格留空（⛔ 不编）");
            c.Lore = Loc.LoreByUniqueId(it.uniqueId);
            return c;
        }

        /// <summary>官方名取不到时的**反推名**：`Cardback_AM_Cold Blood` → `Cold Blood`。
        /// 规则 = 去掉 `Cardback_` 前缀 → 去掉紧跟的**阵营代号**（≤4 个字符、全大写/数字，如 `AM`/`GOF`/`UM`）
        /// → 下划线换空格。这是我们挑的兜底，⛔ 不是原版做法（原版永远走 I2）。</summary>
        public static string ReverseName(string so)
        {
            if (string.IsNullOrEmpty(so)) return "";
            string s = so.StartsWith("Cardback_") ? so.Substring("Cardback_".Length) : so;
            int u = s.IndexOf('_');
            if (u > 0 && u <= 4)
            {
                bool code = true;
                for (int i = 0; i < u; i++)
                    if (!char.IsUpper(s[i]) && !char.IsDigit(s[i])) { code = false; break; }
                if (code) s = s.Substring(u + 1);
            }
            return s.Replace('_', ' ');
        }

        // ============================================================ 开
        /// <summary>建一扇（`WindowsManager.OpenItemDisplay` 的 `create` 那一跳）。</summary>
        public static ItemDisplayWindow Create(WindowsManager mgr, Context? ctx = null)
        {
            var go = new GameObject("Item Display Window");     // 节点名照原版（`OpenByRef` 的键同源）
            var win = go.AddComponent<ItemDisplayWindow>();
            win.type = WindowType.Popup;                  // 实读 `type = 1`
            win.placement = WindowsPlacement.Popup;       // 实读 `windowsPlacement = 15`
            win.closeOnEsc = true;                        // 实读 `closeOnESC = 1`
            win.updateNavPanel = false;                   // 实读 `updateNavPanel = 0`
            win.extraScaleSmallScreen = 1f;               // 实读 1.0（= 不覆盖）
            // `openSound` / `closeSound` 实读都是空 PPtr、`useDefaultCloseSoundIfNull = 1`
            //   ⇒ 一个 clip 都不赋，关窗音走 `GameWindow.Close()` 的家族默认（`ButtonClick Back`）。
            win.Manager = mgr;
            win._ctx = FillDef(ctx);
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[ItemDisplay] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
            if (string.IsNullOrEmpty(_ctx.Type))
                Debug.LogWarning("[ItemDisplay] `Type` 那一行**留空**：原版 = I2(`ItemDrawerOptions.TypeString`)，"
                               + "而那份 `ItemDrawerConfig` 资产在解包树里**不存在**（84 个 bundle 的 MB 逐字节搜 "
                               + "`\"drawers\"` / `\"typeString\"` 零命中）⇒ 判据不足，⛔ 不编一个「Cardback」顶上");
        }

        /// <summary>换一份内容（复用同一扇窗时用；同 `GenericOptionsPanel.Show`）。
        /// <para>⚠️ `GameWindow.TryOpen` 在 `CurrentState == Open` 时照原版**早退**（不重建），
        /// 所以复用那一支必须显式调本方法。</para></summary>
        public void Show(Context? ctx = null)
        {
            _ctx = FillDef(ctx);
            Build();
        }

        // ============================================================ 建
        /// <summary>照 prefab 逐节点搭（**先清空重来** —— 换内容就是重建，同 `GenericOptionsPanel.Build`）。</summary>
        public void Build()
        {
            var root = transform;
            MenuDraw.ClearChildren(root);
            MissingArt.Clear();
            _panel = _flavourBg = _holder = null;
            _title = _desc = _type = _lore = null;

            // ---- 1) `Menu Dark Background`：压暗整屏 + 「点它 = 关窗」----
            //   判据 = 原版那颗 `Image` 的 `m_Color = (0,0,0,0.773)`；`<无图>` ⇒ 用 1×1 白图 + tint。
            var shade = ShadeRect();
            MenuDraw.Rect(root, CardArt.Solid(), shade, "Menu Dark Background", QShade, ShadeTint);
            //   关窗那一跳照 `ItemDisplayWindow__OnEnable`（`closeButton.onClick += Close`）——
            //   原版 prefab 的 `BackgroundCloseButton.window` 是 **空 PPtr**，所以不能照它那条判据不挂。
            MenuDraw.ShadeHit(root, shade, QShade, QHit, () => Close(), "BackgroundHit");

            // ---- 2) `Options` → `Panel`（九宫）→ Title / Description / Type ----
            var options = MenuDraw.Node(root, "Options", new PxRect(0f, 0f, 1920f, 1080f));
            var panelR = new PxRect(PanelL, PanelT, PanelR, PanelB);
            var panelTex = Tex(ArtPanel, "`Options/Panel` 的九宫底");
            GameObject panelGo = panelTex != null
                ? MenuDraw.Nine(options, panelTex, panelR, new Vector4(0f, 325f, 0f, 35f),
                                PanelTexW, PanelTexH, QPanel, Color.white, true, "Panel")
                : null;
            // 图缺 / `Nine` 建不出来 ⇒ **节点照建**（容器仍在，字仍要落在原版矩形里）
            _panel = panelGo != null ? panelGo.transform : MenuDraw.Node(options, "Panel", panelR);

            var titleR = new PxRect(TextL, TitleT, TextR, TitleB);
            var descR = new PxRect(TextL, TitleB, TextR, DescB);
            var typeR = new PxRect(TextL, DescB, TextR, TypeB);
            // Title / Type 原版 `m_TextWrappingMode = 0`（不折行）+ autosize[10~37]；Description 折行 ⇒ `TextBox`。
            // ⚠️ **如实标注（我们挑的写法）**：本仓的 `Label.SetAutoFitBox` **内部必调 `SetWrapWidth`**
            //   （判据 → `Battle/Label.cs:1022`）⇒ 走它就没法「不折行只缩字号」。四格统一用 `TextBox`
            //   （折行 + 自适应）：最长的那几个官方名（如 `Grit and Determination`）会**折成两行并把字号缩到框内**，
            //   而原版是**单行缩字号** —— 两者都落在 314×50 的框里，差别只在那一档长名的排版。
            _title = MenuDraw.TextBox(_panel, titleR, _ctx.Title ?? "", Color.white, "Title",
                                      TitleFont, TitleFontMin, QPanelText, TitleFontMax, TitleFontBase, face: TmpFont.Face.LatinUi);
            _desc = MenuDraw.TextBox(_panel, descR, _ctx.Description ?? "", Color.white, "Description",
                                     DescFont, DescFontMin, QPanelText, DescFontMax, DescFontBase, face: TmpFont.Face.LatinCardBody);
            _type = MenuDraw.TextBox(_panel, typeR, _ctx.Type ?? "", Color.white, "Type",
                                     TypeFont, TypeFontMin, QPanelText, TypeFontMax, TypeFontBase, face: TmpFont.Face.LatinCardBody);

            // 面板底那颗 `Image` 的 `m_RaycastTarget = 1`、父链上没有点击处理器 ⇒ 原版点面板**什么都不发生**
            MenuDraw.Absorb(_panel, "AbsorbHit", panelR, QShade, QHit);
            // 三格全空才关面板（`ItemInformationPanel__Initialize` 末句的 SetActive 判据）。
            // ⚠️ 必须在**建完字之后**再关：TMP 在未激活的对象里量不出尺寸。
            if (string.IsNullOrWhiteSpace(_ctx.Title) && string.IsNullOrWhiteSpace(_ctx.Description)
                && string.IsNullOrWhiteSpace(_ctx.Type))
                _panel.gameObject.SetActive(false);

            // ---- 3) `Card Display` → `FlavourTextBG`(Image) → `LoreText` ----
            var card = MenuDraw.Node(root, "Card Display", new PxRect(CardL, CardT, CardR, CardB));
            var fbR = FlavourRect(out float s);
            _flavourBg = MenuDraw.Node(card, "FlavourTextBG", fbR);
            var fbTex = CardArt.FlavorBg(_ctx.Army);
            // 🔴 原版 `FlavourTextSO.GetClanFlavorBackground(army)` 的**兜底支**：表里没有这个 army ⇒
            //   返回 `defaultBackground`（`FlavorTextSO__GetClanFlavorBackground.c` 末段那句 Load）。
            //   而工程的 `defaultBackground` 与 `army = 10 (Ultramarines)` 是**同一张**（判据 →
            //   `CardArt.FlavorBg` 的 doc：「`defaultBackground` 与 army=0(Neutral)/10(Ultramarines) 是同一张」）
            //   ⇒ 卡背里 `Neutral`（`Cardback_All_*` 那一批，armyId = 0）走它。⛔ 不静默留白。
            if (fbTex == null) fbTex = CardArt.FlavorBg("Ultramarines");
            if (fbTex != null)
                MenuDraw.Rect(_flavourBg, fbTex, fbR, "Image", QFlavour);   // `m_PreserveAspect = 0` ⇒ 拉伸铺满
            else
                Debug.LogWarning("[ItemDisplay] 阵营「" + (_ctx.Army ?? "<null>") + "」没有风味底图"
                               + "（`CardArt.FlavorBg` 取不到）⇒ 风味条**只画字 / 无字就整块不亮**");

            var loreR = LoreRect(s);
            _lore = MenuDraw.TextBox(_flavourBg, loreR, _ctx.Lore ?? "", Color.white, "LoreText",
                                     LoreFont, LoreFontMin, QLoreText, LoreFontMax, LoreFontBase, face: TmpFont.Face.LatinUi);
            MenuDraw.Absorb(_flavourBg, "AbsorbHit", fbR, QShade, QHit);
            // 照 `ItemDisplayWindow__Open.c` ③：**没有 lore ⇒ 整块不亮**（⛔ 不拿 Description 顶）。
            bool loreOn = !string.IsNullOrWhiteSpace(_ctx.Lore);
            _flavourBg.gameObject.SetActive(loreOn);

            // ---- 4) `Item Holder`（卡背图）----
            _holder = MenuDraw.Node(root, "Item Holder", new PxRect(HolderL, HolderT, HolderR, HolderB));
            var box = ItemBoxRect();
            if (_ctx.Art != null)
            {
                // 两段式（`CardbackFace`）：uGUI 先按 sprite 的 `m_Rect` 定框、再按 `padding` 内缩并挪位。
                var q = MenuDraw.Rect(_holder, _ctx.Art, box, "Image", QItemArt, null, true);
                if (q == null)
                    Debug.LogWarning("[ItemDisplay] 卡背 quad 建不出来（`MenuDraw.Rect` 返回 null）—— 不静默");
                else MenuDraw.Absorb(_holder, "AbsorbHit", box, QShade, QHit);
            }
        }

        // ============================================================ 几何
        /// <summary>压暗层：**以屏幕中心为中心**的 4574.60×2572.36（prefab 现读）。</summary>
        public static PxRect ShadeRect()
        {
            return new PxRect(960f - ShadeW * 0.5f, 540f - ShadeH * 0.5f,
                              960f + ShadeW * 0.5f, 540f + ShadeH * 0.5f);
        }

        /// <summary>`Item Holder` 里那张卡背的**框**：650 高、宽 = 650 × 0.66 = **429**（`Cardback Drawer`
        /// 根的 `AspectRatioFitter` 实读），居中在 Holder 中心。</summary>
        public static PxRect ItemBoxRect()
        {
            float w = (HolderB - HolderT) * ItemBoxRatio;
            float cx = (HolderL + HolderR) * 0.5f, cy = (HolderT + HolderB) * 0.5f;
            return new PxRect(cx - w * 0.5f, cy - (HolderB - HolderT) * 0.5f,
                              cx + w * 0.5f, cy + (HolderB - HolderT) * 0.5f);
        }

        /// <summary>`FlavourTextBG` 的矩形（小屏开关开着时按 `menuScale = 1.12`、绕 pivot (0.5, 0)
        /// —— uGUI 的 y 向上 ⇒ pivot 在**底边中点** ⇒ 本坐标系里**底边 y2 不动**、向左右/向上放大）。
        /// <paramref name="scale"/> = 实际用的倍数（1 = 没放大，自检读它）。</summary>
        public static PxRect FlavourRect(out float scale)
        {
            var r = new PxRect(FlavourL, FlavourT, FlavourR, FlavourB);
            scale = SmallScreenUI.Enabled ? FlavourMenuScale : 1f;
            // 🔴 原版那颗组件必挂在本节点上（`TransformScalerBySmallScreenUI.LateUpdate` 乘 `localScale`），
            //    而它在 `Initialize()` 里还有一道 `enabled = (menuScale != 1) && smallScreenUI` ⇒ 开关关着时不动。
            return ScaleAboutBottomCenter(r, scale);
        }

        /// <summary>`LoreText` 的矩形：**跟着 `FlavourTextBG` 的同一颗 `localScale` 一起缩放**
        /// （它是 `FlavourTextBG` 的子件，原版那一次 `localScale` 对整棵子树生效）。</summary>
        public static PxRect LoreRect(float scale)
        {
            var r = new PxRect(LoreL, LoreT, LoreR, LoreB);
            var pivot = new Vector2((FlavourL + FlavourR) * 0.5f, FlavourB);   // 父节点 pivot (0.5,0) = 底边中点
            return new PxRect(pivot.x + (r.x1 - pivot.x) * scale, pivot.y + (r.y1 - pivot.y) * scale,
                              pivot.x + (r.x2 - pivot.x) * scale, pivot.y + (r.y2 - pivot.y) * scale);
        }

        /// <summary>绕**底边中点**缩放的 `PxRect`（本坐标系 y 向下 ⇒ 底边是 `y2`）。</summary>
        public static PxRect ScaleAboutBottomCenter(PxRect r, float s)
        {
            if (Mathf.Approximately(s, 1f)) return r;
            float cx = (r.x1 + r.x2) * 0.5f, y2 = r.y2;
            return new PxRect(cx + (r.x1 - cx) * s, y2 + (r.y1 - y2) * s,
                              cx + (r.x2 - cx) * s, y2);
        }

        // ============================================================ 取图（**取不到必须出声**）
        readonly System.Collections.Generic.List<string> _missArt = new System.Collections.Generic.List<string>();
        /// <summary>本窗取不到的图（自检读口 —— 非空就是「有件根本没画」）。</summary>
        public System.Collections.Generic.List<string> MissingArt { get { return _missArt; } }

        Texture2D Tex(string art, string what)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[ItemDisplay] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        // ============================================================ 自检读口
        public Transform PanelNode { get { return _panel; } }
        public Transform FlavourNode { get { return _flavourBg; } }
        public Transform HolderNode { get { return _holder; } }
        public Label TitleLabel { get { return _title; } }
        public Label DescriptionLabel { get { return _desc; } }
        public Label TypeLabel { get { return _type; } }
        public Label LoreLabel { get { return _lore; } }

        /// <summary>自检用：把当前铺出来的东西摊开。</summary>
        public string DebugDump()
        {
            return "title=\"" + (_title != null ? _title.Text : "-") + "\""
                 + " desc=\"" + (_desc != null ? _desc.Text : "-") + "\""
                 + " type=\"" + (_type != null ? _type.Text : "-") + "\""
                 + " loreOn=" + (_flavourBg != null && _flavourBg.gameObject.activeSelf)
                 + " art=" + (_ctx.Art != null ? _ctx.Art.name : "<null>")
                 + " missingArt=" + _missArt.Count;
        }
    }
}
