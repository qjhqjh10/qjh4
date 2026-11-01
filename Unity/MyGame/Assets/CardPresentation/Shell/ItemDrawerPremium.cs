// ============================================================================ 高级/礼包三档（`PremiumDrawer` / `PremiumIconDrawer` / `ExpansionPassPremiumDrawer`）
//
//  判据（施工单 B12 / B13 / B14；方法体 = `cpp2il/out_cpp2il_dev_ilspy/*.cs`）：
//    · `PremiumDrawer.Draw(PremiumItem item, int quantity = 1)`
//        `if ((bool)icon && item.itemIcon.RuntimeKeyIsValid()) icon.sprite = item.itemIcon.Load();`
//        `else { icon.gameObject.SetActive(false); }`
//        `if ((bool)armyText) armyText.text = GameStaticData.CardArmyToString(item.cardArmy);`
//      ⇒ **两态**：有图 ⇒ 换图；无图 ⇒ **把 icon 关掉**（⛔ 不是画占位）。
//    · `PremiumIconDrawer : PremiumDrawer` —— `base.Draw(item, quantity);` 之后再：
//        `if ((bool)item.BackgroundSprite) { background.sprite = item.BackgroundSprite; }`
//        `else { background.gameObject.SetActive(false); }`
//      ⚠️ 这一档的 prefab 是 `Icon VIP Premium Campaign Drawer Variant`、**`armyText` 槽是 null**
//      ⇒ 原版这一档**不画阵营名**（施工单 §三-2）⇒ 本实现显式关掉（`armyText: false`）。
//    · `ExpansionPassPremiumDrawer.Draw(ExpansionPremiumItem item, int quantity = 1)`
//        `if ((bool)icon) icon.sprite = item.itemIcon.Load();`   ← **没有 `RuntimeKeyIsValid` 那道守卫**
//        `if ((bool)armyText) armyText.text = CardArmyToString(item.cardArmy);`
//      ⇒ 与 `PremiumDrawer` 的**唯一差别**：本档**不判「有没有配图」、也**不会把 icon 关掉**
//        （施工单 B14「还差什么」：⛔ 别把两档合并成一个函数）。
//
//  ── 版面（prefab 实读）──────────────────────────────────────────────────────
//    `Premium Drawer` / `Expansion Pass Premium Drawer`：根 712.8×1080 · `Content/Image` (0.4,216)-(712.4,648) ·
//      `Content/Label` **只有 `Name`**（没有 `Quantity` 子件）· `Content/Army` (0.4,702)-(712.4,810)
//      🔴 **2026-10-30 `B101_R8` 订正**（原来写「`Campaign Glow` 是静态装饰层、`Draw` 不碰 ⇒ 不建」—— **错**）：
//      `Icon VIP Premium Campaign Drawer Variant` 的 `ItemDrawerComponents.background` **指向 `Content/Campaign Glow`**
//      （PPtr 实读）⇒ 它**就是** `PremiumIconDrawer` 那一档的 `background` 槽，`Draw` 会写它的 sprite / 或把它关掉。
//      🔴 **2026-10-31 订正（`B108_A15` 现读）**：出厂图 `40K_genearl_icon_Campaign points_big` **在** `Resources/Art/ui_menu/`（原句「没有」已过期）。
//    `Icon VIP Premium Campaign Drawer Variant`：根 **1080×1080**、`Content/Image` 铺满
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `PremiumDrawer : ItemDrawer&lt;PremiumItem&gt;`（`VIPPremiumItem` 的基础档也指向它）。</summary>
        public const string DrawerPremium = "PremiumDrawer";
        /// <summary>原版 `PremiumIconDrawer : PremiumDrawer`（`VIPPremiumItem` 的 `Icon` 档）。</summary>
        public const string DrawerPremiumIcon = "PremiumIconDrawer";
        /// <summary>原版 `ExpansionPassPremiumDrawer : ItemDrawer&lt;ExpansionPremiumItem&gt;`。</summary>
        public const string DrawerExpansionPremium = "ExpansionPassPremiumDrawer";

        /// <summary>`ItemDrawerComponents.background` 在 `Icon VIP Premium Campaign Drawer Variant` 上指向的那颗
        /// （= `Content/Campaign Glow`，PPtr 实读；**不是**名叫 `Background` 的节点 —— 那份 prefab 里没有那种节点）。</summary>
        public const string NodeCampaignGlow = "Campaign Glow";

        /// <summary>原版 `PremiumDrawer.Draw` 的等价实现。
        /// <param name="armyText">该 prefab 上 `armyText` 槽在不在（`false` = 原版那句被 null 守卫跳过）。
        /// <param name="iconGuard">`true` = 走 `PremiumDrawer` 的「无图就把 icon 关掉」；
        /// `false` = 走 `ExpansionPassPremiumDrawer` 的「无条件 `Load()`，永不清空」。</param>
        static void PremiumBody(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                ref ItemDrawResult res, bool armyText, bool iconGuard)
        {
            var fr = FrameOf(box, fit: RootFit.HeightControlsWidth);   // `Premium Drawer` / `… No Glow` / `Expansion Pass Premium Drawer` / 两档 Icon 变体：全是 ARF 2（B-260）
            var content = DrawerContent(node, fr);
            // 🆕 **2026-10-31（B108_A15）**：这两层**不是一个固定形状** —— 5 份 prefab 逐份直读
            //   （判据 → `资料/完全复刻/判据/B108_AUD01_余项.md` §二·2 表）：
            //     · `Premium Drawer` / `Expansion Pass Premium Drawer` / `Premium Drawer No Glow` 有 `Content/Background`；
            //     · `Icon Premium Campaign Drawer Variant` **没有** `Background` 节点，只有一条 `Campaign Glow` **横带**；
            //     · 横带 anchors 三份相同、sizeDelta 不同 ⇒ 设计 px 矩形也**各不相同**（⛔ 别写成一组全局常量）。
            //   `PremiumDrawer.Draw` 自己只写 `background` 槽（仅 `Icon VIP …` 那种把槽指向 `Campaign Glow` 的）；
            //   静态横带是 prefab 自带的层、原版方法体**不碰** ⇒ 我们按 prefab 名置位重建（调用点 = `BaseOfferPopup`）。
            var layers = st.PremiumLayers ?? PremiumLayerFlags.Original;
            if (layers.BaseBackground) DrawerBackground(content, fr, st);
            else
                Note("premium|nobase",
                     "高级抽屉这一格的原版 prefab **没有 `Content/Background` 节点**"
                     + "（`Icon Premium Campaign Drawer Variant`：`background` 槽指向 `Campaign Glow`；"
                     + "`Icon Expansion Pass Premium Drawer Variant`：`background` 槽 = `{fileID: 0}`、只有一条静态横带）"
                     + "⇒ 不画整卡底板（判据 → `判据/B108_AUD01_余项.md` §二·2 · `判据/B109_R4_ExpansionPassPremium.md` §2）");
            if (layers.CampaignGlow) DrawCampaignGlow(content, fr, st, layers);

            var tex = Tex(item.Art);                    // = `item.itemIcon.Load()`
            if (tex != null)
                DrawerImage(content, fr.Map(DrCx1, DrImgY1, DrCx2, DrImgY2), tex, item.Art, st, ref res);
            else
            {
                res.MissingArt = true;
                if (iconGuard)
                    Note("premium|icon|" + (item.Id ?? "?"),
                         "高级物品 `" + (item.Id ?? "?") + "` 的 `itemIcon` 判据空（图名 `" + (item.Art ?? "<空>")
                         + "`）⇒ 与原版同一支：**把 `icon` 关掉**（`RuntimeKeyIsValid()` 假），⛔ 不画占位板");
                else
                    Note("expansionPremium|icon|" + (item.Id ?? "?"),
                         "礼包物品 `" + (item.Id ?? "?") + "` 的 `itemIcon` 取不到（图名 `" + (item.Art ?? "<空>")
                         + "`）⇒ 原版这一档**没有守卫**（`icon.sprite = Load()` 直接赋 null）⇒ 这一层空着，⛔ 不画占位板");
            }
            if (armyText) DrawerArmyName(content, fr, item.CardArmy, st);
            DrawerNameQuantity(content, fr, item, qty, st, hasName: true, hasQuantity: false);
        }

        /// <summary>原版 `PremiumDrawer.Draw`。</summary>
        static void Premium(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                            ref ItemDrawResult res)
        {
            PremiumBody(node, box, item, qty, st, ref res, armyText: true, iconGuard: true);
        }

        /// <summary>原版 `PremiumIconDrawer.Draw`（`base.Draw` + `background` 那一层）。
        /// Icon 变体根 1080²、`Content` 1080²、`Image` 铺满；`armyText` 槽 null ⇒ 不画阵营名。</summary>
        static void PremiumIcon(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                ref ItemDrawResult res)
        {
            var fr = FrameOf(box, DrIconRoot, DrIconRoot, RootFit.HeightControlsWidth);   // `Icon VIP Premium Campaign Drawer Variant`：ARF 2 · 1（B-260）
            var content = DrawerIconContent(node, fr);

            var tex = Tex(item.Art);
            if (tex != null)
                DrawerImage(content, fr.Map(0f, 0f, DrIconRoot, DrIconRoot), tex, item.Art, st, ref res);
            else
            {
                res.MissingArt = true;
                Note("premiumIcon|icon|" + (item.Id ?? "?"),
                     "VIP 高级物品 `" + (item.Id ?? "?") + "` 的 `itemIcon` 判据空（图名 `" + (item.Art ?? "<空>")
                     + "`）⇒ 与原版同一支：把 `icon` 关掉（`RuntimeKeyIsValid()` 假）");
            }

            // `background.sprite = item.BackgroundSprite` / 取不到 ⇒ 把 background 关掉
            // ⚠️ 这一档的 `background` 槽 = **`Content/Campaign Glow`**（PPtr 实读），⛔ 不叫 `Background`。
            var bg = Tex(item.BackgroundArt);
            if (bg != null)
                MenuDraw.Rect(content, bg, fr.Map(0f, 0f, DrIconRoot, DrIconRoot), NodeCampaignGlow,
                              st.QBoard, null, false, st.Clip, st.ClipSoftness);
            else if (!string.IsNullOrEmpty(item.BackgroundArt))
                Note("premiumIcon|bg|" + (item.Id ?? "?"),
                     "VIP 高级物品 `" + (item.Id ?? "?") + "` 的 `BackgroundSprite`（图名 `" + item.BackgroundArt
                     + "`）取不到 ⇒ 原版这一支会把 `background` **关掉**，我们也不画");

            // ⚠️ `armyText` 槽 null ⇒ PremiumBody 那一段不调（见文件头）
            DrawerNameQuantity(content, fr, item, qty, st, hasName: false, hasQuantity: false);
        }

        /// <summary>原版 `ExpansionPassPremiumDrawer.Draw`（无 `RuntimeKeyIsValid`、永不关 icon）。</summary>
        static void ExpansionPremium(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                     ref ItemDrawResult res)
        {
            PremiumBody(node, box, item, qty, st, ref res, armyText: true, iconGuard: false);
        }

        // ============================================================ 高级抽屉的两层开关（🆕 2026-10-31 · B108_A15）
        //
        //  判据（一手）：`资料/完全复刻/判据/B108_AUD01_余项.md` §二·2 —— 5 份 prefab 逐份直读，`Campaign Glow` 的
        //  几何**各不相同**（`Premium Drawer` / `Icon Premium Campaign Drawer Variant` 一组，`Expansion Pass …` 另一组）。
        //  🆕 **2026-10-31（B109_E5）：第 6 份 `Icon Expansion Pass Premium Drawer Variant` 的判据补齐** ——
        //     它**没有 `Background` 节点**、`background` 槽 = `{fileID: 0}`，只有一条与 `Image` **同框**的静态横带
        //     （`(0,216)-(712,648)` · sprite = `40K_general_icon_Expansion Pass XP`），且 `label` / `quantity` / `armyText`
        //     三个槽**全是 null**。判据 → `资料/完全复刻/判据/B109_R4_ExpansionPassPremium.md` §2（prefab 直读 + 3 实例复核）。
        //  ⛔ 别把这里改回「`PremiumDrawer` 恒画 `Background`」：`Icon Premium Campaign Drawer Variant`（根 ARF 0.66）
        //     原版那格根本没有 `Background` 节点，凭空建一个整卡底板 = 多画一层。

        /// <summary>高级抽屉的「底板 / 静态横带」两层开关（`ItemDrawerStyle.PremiumLayers` 的载荷）。
        /// <para>⚠️ 这 4 个矩形是**设计 px**（左上原点 · y 向下），与 `DrawerFrame.Map` 同一口径。</para></summary>
        public struct PremiumLayerFlags
        {
            /// <summary>`true` = 画 `Content/Background`（图恒 `40k_Rewards_icon_base card`）。</summary>
            public bool BaseBackground;
            /// <summary>`true` = 画 `Content/Campaign Glow` 静态横带（原版 prefab 自带的层）。</summary>
            public bool CampaignGlow;
            /// <summary>`Campaign Glow` 的设计 px 矩形（左上原点 · y 向下）。</summary>
            public float GlowX1, GlowY1, GlowX2, GlowY2;
            /// <summary>`Campaign Glow` 的图（`CardArt.MenuUi` 名 · 空格→下划线）。`null` = 用 `PremiumGlowSprite`
            /// （= `40K_genearl_icon_Campaign_points_big`，只服务 `Premium Drawer` 那两档）。
            /// 🔴 2026-10-31（B109_E5）：横带的 sprite **三档互不相同**（prefab 的 `Campaign Glow.m_Sprite` guid 逐份实读），
            /// ⛔ 别让所有档共用 `PremiumGlowSprite`。</summary>
            public string GlowSprite;

            /// <summary>原版旧形状（= `ItemDrawerStyle.Default` 那一档）：画 `Background`、**不画**横带。
            /// `ItemDrawerStyle.PremiumLayers` 为 `null` 时也走这一档 ⇒ 既有调用点一个字节不变。</summary>
            public static readonly PremiumLayerFlags Original =
                new PremiumLayerFlags { BaseBackground = true };

            /// <summary>`Premium Drawer` 的横带（`Content` 固定 712×1080 ⇒ (−31.26,197.05)-(744.06,666.95)，R6 现算）。</summary>
            public static readonly PremiumLayerFlags BaseWithBand =
                new PremiumLayerFlags { BaseBackground = true, CampaignGlow = true,
                                        GlowX1 = -31.26f, GlowY1 = 197.05f, GlowX2 = 744.06f, GlowY2 = 666.95f };

            /// <summary>`Icon Premium Campaign Drawer Variant`：**没有** `Background`、只有同一条横带。</summary>
            public static readonly PremiumLayerFlags NoBaseWithBand =
                new PremiumLayerFlags { BaseBackground = false, CampaignGlow = true,
                                        GlowX1 = -31.26f, GlowY1 = 197.05f, GlowX2 = 744.06f, GlowY2 = 666.95f };

            /// <summary>`Expansion Pass Premium Drawer`：有 `Background`，横带 sizeDelta 不同
            /// ⇒ (−114.66,146.46)-(826.66,717.54)。
            /// 🔴 2026-10-31（B109_E5）订正：横带图**不是** `PremiumGlowSprite` —— 该 prefab 的
            /// `Campaign Glow.m_Sprite` guid `d2dc932d3e8aec642a97a91df78b78c1` = **`40k_campaign_Expansion Pass`**
            /// （落盘名 `40k_campaign_Expansion_Pass`；判据 → `判据/B109_R4_ExpansionPassPremium.md` §2 对照表）。</summary>
            public static readonly PremiumLayerFlags BaseWithWideBand =
                new PremiumLayerFlags { BaseBackground = true, CampaignGlow = true,
                                        GlowX1 = -114.66f, GlowY1 = 146.46f, GlowX2 = 826.66f, GlowY2 = 717.54f,
                                        GlowSprite = "40k_campaign_Expansion_Pass" };

            /// <summary>`Icon Expansion Pass Premium Drawer Variant`（`BaseOfferPopup.cs:473/565/573` 三个槽）：
            /// **没有 `Background` 节点、`background` 槽 = null**；只有一条与 `Image` **同框**的 `Campaign Glow`
            /// （`aMin(0,0.4) aMax(1,0.8) sizeDelta ≈ 0` ⇒ `(0,216)-(712,648)` · `PA=1` · 白）；
            /// sprite = **`40K_general_icon_Expansion Pass XP`**（guid `6c77cbd6b01d7c549952437eb07de765`）。
            /// 判据 → `资料/完全复刻/判据/B109_R4_ExpansionPassPremium.md` §2（prefab 直读 + 3 个实例复核）。</summary>
            public static readonly PremiumLayerFlags NoBaseWithExpansionGlow =
                new PremiumLayerFlags { BaseBackground = false, CampaignGlow = true,
                                        GlowX1 = DrCx1, GlowY1 = DrImgY1, GlowX2 = DrCx2, GlowY2 = DrImgY2,
                                        GlowSprite = "40K_general_icon_Expansion_Pass_XP" };
        }

        /// <summary>按**抽屉槽的 prefab 名**取那两层的开关（`null` = 不认识 ⇒ 保持旧形状）。
        /// 调用点 = `Shell/BaseOfferPopup.BuildDrawers`（`d.Name` = popup prefab 里那颗实例的名字）。</summary>
        public static PremiumLayerFlags? PremiumLayersOfSlot(string slotName)
        {
            if (string.IsNullOrEmpty(slotName)) return null;
            // ⚠️ 「No Glow」必须先判 —— 它整串以 `Premium Drawer` 开头。
            if (slotName.StartsWith("Premium Drawer No Glow", System.StringComparison.Ordinal))
                return PremiumLayerFlags.Original;
            if (slotName.StartsWith("Icon Premium Campaign Drawer Variant", System.StringComparison.Ordinal))
                return PremiumLayerFlags.NoBaseWithBand;
            // 🆕 2026-10-31（B109_E5）：`Icon Expansion Pass Premium Drawer Variant`（`:473` 那颗实例名多一个
            //   `Variant` = `…Variant Variant`，所以要 `StartsWith` 而不是 `==`）· 判据 → `判据/B109_R4_…md` §2。
            if (slotName.StartsWith("Icon Expansion Pass Premium Drawer Variant", System.StringComparison.Ordinal))
                return PremiumLayerFlags.NoBaseWithExpansionGlow;
            if (slotName.StartsWith("Premium Drawer", System.StringComparison.Ordinal))
                return PremiumLayerFlags.BaseWithBand;
            if (slotName.StartsWith("Expansion Pass Premium Drawer", System.StringComparison.Ordinal))
                return PremiumLayerFlags.BaseWithWideBand;
            return null;
        }

        /// <summary>`ItemDrawerStyle` 是值语义 struct —— 给某个槽单独置位时**复制一份**再交给 `Draw`
        /// （`BaseOfferPopup` 在同一行里用；⛔ 别改成改原 `st` —— 同一份变体里各槽的 prefab 不同）。</summary>
        public static ItemDrawerStyle WithPremiumLayers(ItemDrawerStyle st, string slotName)
        {
            st.PremiumLayers = PremiumLayersOfSlot(slotName);
            return st;
        }

        /// <summary>`Content/Campaign Glow` 静态横带的**默认**出厂图 —— `Icon Premium Campaign Drawer Variant.prefab` 的
        /// `Campaign Glow.m_Sprite` 实读名字（落盘名 = 空格→下划线，现读在 `Resources/Art/ui_menu/`）。
        /// ⚠️ 只有 `Premium Drawer` / `NoBaseWithBand` 两档用它；另两档在预设里各自带 `GlowSprite`（见 `PremiumLayerFlags`）。</summary>
        public const string PremiumGlowSprite = "40K_genearl_icon_Campaign_points_big";

        /// <summary>画 `Content/Campaign Glow`（原版 prefab 自带的静态层，`PremiumDrawer.Draw` **不碰**它）。
        /// 取不到图 ⇒ 出声一次、**不画**（⛔ 不拿别的图顶替 —— 铁律 2 / 项目红线）。
        /// 🔴 2026-10-31（B109_E5）：图**按预设取**（`lay.GlowSprite`）—— 三档的横带 sprite 各不相同
        /// （`Premium Drawer` 两档 = `…Campaign points_big` · `Expansion Pass Premium Drawer` = `40k_campaign_Expansion Pass`
        /// · `Icon Expansion Pass Premium Drawer Variant` = `40K_general_icon_Expansion Pass XP`）。</summary>
        static void DrawCampaignGlow(Transform content, DrawerFrame fr, ItemDrawerStyle st, PremiumLayerFlags lay)
        {
            string name = string.IsNullOrEmpty(lay.GlowSprite) ? PremiumGlowSprite : lay.GlowSprite;
            var tex = Tex(name);
            if (tex == null)
            {
                Note("premium|glow|" + name,
                     "高级抽屉的 `Campaign Glow` 出厂图 `" + name + "` 在 `Resources/` 里取不到"
                     + "（`CardArt.MenuUi` 三级全空）⇒ 这一层**不画**（导入口 = `工具/import_original_art.py` 的 `MENU_IMAGES`）");
                return;
            }
            MenuDraw.Rect(content, tex, fr.Map(lay.GlowX1, lay.GlowY1, lay.GlowX2, lay.GlowY2),
                          NodeCampaignGlow, st.QBoard, null, true, st.Clip, st.ClipSoftness);
        }

        // ---- 自检读口（⛔ 只给 `Editor/B108_R6_Assert.cs`，生产路径不调）----
        //   为什么放这里：那三个算式是 `private static`（`Shell/ItemDrawerCosmetic.cs`），而断言在 Editor 程序集里；
        //   那个文件本波**无主** ⇒ 不改它的可见性，改由本 partial 类开读口。

        /// <summary>自检读口：`AvatarImageFrame`（`K = 1` ⇒ 设计 px 原值）。</summary>
        public static PxRect AvatarImageFrameForTest(float k)
        {
            return AvatarImageFrame(new DrawerFrame { K = k });
        }

        /// <summary>自检读口：`CosmeticAvatarFrame`（`avatarBG` 那一格）。</summary>
        public static PxRect CosmeticAvatarFrameForTest(float k)
        {
            return CosmeticAvatarFrame(new DrawerFrame { K = k });
        }

        /// <summary>自检读口：`CardbackOverdrawFrame`（1.15 倍过扫那一格）。</summary>
        public static PxRect CardbackOverdrawFrameForTest(float k)
        {
            return CardbackOverdrawFrame(new DrawerFrame { K = k });
        }
    }

    /// <summary>`ItemDrawerStyle`（**命名空间级**的那个 struct，`ItemDrawer.cs:150`）的分片 —— 本波只加一格。
    /// <para>🆕 **2026-10-31（B108_A15）**：高级抽屉那两层（`Background` / `Campaign Glow`）的开关。</para>
    /// <para>`null` = 原版旧形状（`ItemDrawer.PremiumLayerFlags.Original`）⇒ `ItemDrawerStyle.Default`
    /// 造出来的调用点**零影响**（`Default()` 一个字节没改）。</para>
    /// <para>🔴 **为什么挂在 style 上**：`ItemDrawerView.Refresh()` 重画时只带 `_style`
    /// （`ItemDrawer.cs:2030`）—— 开关不跟着 style 走的话，一次 `Refresh` 就会把横带丢掉、把底板多画回来。</para></summary>
    public partial struct ItemDrawerStyle
    {
        /// <summary>高级抽屉那两层的开关；`null` = 保持旧形状。</summary>
        public ItemDrawer.PremiumLayerFlags? PremiumLayers;
    }
}
