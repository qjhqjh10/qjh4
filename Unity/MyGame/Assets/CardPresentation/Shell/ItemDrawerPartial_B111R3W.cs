// ItemDrawerPartial_B111R3W.cs — 第 111 会话第 1 波执行代理 `B111_R3W`（2026-11-01）· 账 N138 的 ①（Icon 铺满档）
//
// 判据：`判据/B106_奖励预览链_施工单.md §三 A2` —— 原版 Icon 抽屉的主图是「**铺满 + PA=1**」：
//   本次现读 `d:/2/tools/assetripper_out/ExportedProject/Assets/` 的 Icon 抽屉 prefab（Title / Currency / Random Card 三张），
//   drawer 主图节点 `Image` = `m_AnchorMin (0,0)` · `m_AnchorMax (1,1)` · `m_SizeDelta (0,0)`（整框拉伸）· `m_PreserveAspect 1`（PA=1）。
//   我们的 `Icon(...)` 抽屉原来画 `Square(box, st.IconFill)`（`min(W,H)*k` 居中方形）⇒ 对非方框**不是铺满**。
//
// 本文件只做**加法**（默认行为逐字节不变）：
//   · `ItemDrawerStyle.IconFillBox`（默认 false）。**false ⇒ 走原来的 `Square(box, IconFill)`**（既有 100+ 调用点不受影响）；
//     true ⇒ 主图 = 整个 `box`（= 原版那套拉伸铺满，配合 `MenuDraw.Rect(..., keepAspect: true)` = PA=1）。
//   · `IconRect(box, st)`：`Icon` / `WildcardIcon` / `Placeholder` 三处 `Square(box, st.IconFill)` 改调它（见 ItemDrawer.cs 的那 3 行）。
//   · `DrawIconFill(...)`：给两扇日奖励窗用的便捷口 —— 直接以「Icon 档 + 铺满 + PA=1 + 不画数量/名字」建一格
//     （= 原版 `ItemDrawer.Draw(rewardDrawerHolder, item, qty, Icon)` 的结构等价）。
// ⚠️ 本类是 `partial`（`ItemDrawerStyle` / `ItemDrawer` 在主文件里就是 partial）⇒ 新成员全在这里，**不推主文件任何账落点**。
using UnityEngine;

namespace CardPresentation
{
    public partial struct ItemDrawerStyle
    {
        /// <summary>主图几何档：**false = 居中方形**（`min(box)×IconFill`，既有行为）· **true = 铺满整个 `box`**
        /// （= 原版 Icon 抽屉变体的 `Image` 锚 `(0,0)-(1,1)` + `sizeDelta 0`，见文件头）。默认 false。
        /// <para>🔴 2026-11-01（`B111_R3W` · 账 N138）：新增档，**默认 false ⇒ 既有调用点逐字节不变**。</para></summary>
        public bool IconFillBox;
    }

    public static partial class ItemDrawer
    {
        /// <summary>主图矩形：`IconFillBox` 为真 ⇒ 整个 `box`（铺满）；否则 = 原来的 `Square(box, IconFill)`（居中方形）。
        /// <para>⛔ 三处 `Icon` / `WildcardIcon` / `Placeholder` 必须都走这里 —— 别在两处各写一份分支（CLAUDE.md §三）。</para></summary>
        static PxRect IconRect(PxRect box, ItemDrawerStyle st)
        {
            return st.IconFillBox ? box : Square(box, st.IconFill);
        }

        /// <summary>= 原版 `ItemDrawer.Draw(rewardDrawerHolder, item, qty, Icon)` 的结构等价：**Icon 档 + 铺满 + PA=1 + 不画数量/名字**。
        /// <para>给两扇日奖励窗（`DailyRewardPopup` / `DailyStreakPopup`）的奖格用：它们的主图原先是裸 `MenuDraw.Rect(..., keepAspect: true)`，
        /// 换成本口之后抽屉结构（`Item Drawer` 根 → `Icon` 图）与原版对上，而**几何不变**（icon 仍在 `box` 里铺满 + PA=1）。</para>
        /// <para>`art` = 图名（走 `Tex` = `CardArt.MenuUi`，与两窗各自的 `Art()` 同一解析口）；`null`/空 ⇒ **不画**（返回 null，
        /// 由调用方照旧把缺图记进 `MissingArt` / 出声 —— ⛔ 别静默画个占位板）。</para>
        /// <para>`qArt` = 主图的渲染队列（两窗原先用的是 `QContent`）。数量/名字两档显式置 0 ⇒ 不建那两层
        /// （日奖励那格自己画名字、也不显示数量）。</para></summary>
        public static ItemDrawerView DrawIconFill(Transform parent, PxRect box, string art, string label, int qArt,
                                                  PxRect? clip = null, Vector2 clipSoftness = default(Vector2))
        {
            if (string.IsNullOrEmpty(art)) return null;
            // 明确按 `Generic`（有图、未知原版子类型）建 —— 走 `Icon` 档抽屉（`PickDrawer` 的 `case ItemKind.Generic`）。
            var spec = new ItemSpec { Id = "ItemReward", Kind = ItemKind.Generic, Art = art, Label = label };
            var st = ItemDrawerStyle.Default(qArt, qArt, qArt + 1);
            st.IconFill = 1f; st.IconFillBox = true; st.QuantityPx = 0f; st.NamePx = 0f;
            st.Clip = clip; st.ClipSoftness = clipSoftness;   // 调用方原话：给它原来那条路的裁切（`null` = 沿父链找 `ViewportClip`，与原先裸 `MenuDraw.Rect(..., clip: null)` 同义）
            return DrawView(parent, box, spec, 1, DrawerOverride.Default, st);
        }

#if UNITY_EDITOR
        /// <summary>自检读口（⛔ 非原版件、只许自检调）：把私有 `IconRect` 暴露成只读，供 `Editor/B111_R3WB_Assert.cs` 断几何。
        /// <para>灭自证：谁把 `IconRect` 改回「永远 `Square(...)`」⇒ 铺满那一档的断言红。</para></summary>
        public static PxRect IconRectForTest(PxRect box, ItemDrawerStyle st) { return IconRect(box, st); }
#endif

        // ============================================================ B114_WC · B-223：Icon 变体几何 **6** 档（2026-11-01）
        //  🔴 **2026-11-01 `B114_W38`（`B-281`）就地订正（铁律 5）**：本节原文写「**7 档**」——
        //     第 7 档（#14 `Alliance Badge Drawer Icon`）已于同日移交 `ItemDrawerPartial_B114W35.cs`
        //     （`B-277` 查出它的根是**固定 165.95×155.74、不跟框走**，与本节其余各档口径不同）⇒ **本节只服务 6 族**。
        //
        //  【为什么单开这一节】原版有 7 族抽屉是「**同一个类 + 两套 prefab**」：
        //    基础档 `Avatar Drawer` / Icon 档 `Icon Avatar Drawer Variant` ……（映射表 §②；`ItemTypeSets` 的
        //    Icon(10) 列写的**仍是主档类名**）。`Dispatch` 早就把 `DrawerOverride ov` 收进来了，
        //    但 26 个 `case` 里原先**只有 `GenericArmy` 把 `ov` 往下传** ⇒ 这 7 档永远按基础档几何画。
        //  【施工方式】`Dispatch` 的第一行 `{` 里**同物理行**加一句 `if (ov == Icon && IconVariant(...)) return;`
        //    （零行数变化，账的 A 落点不动）；实现全在本节 —— 主文件与各 `ItemDrawer*.cs` 一行业务代码都不动。
        //
        //  【判据（逐张 prefab 现读 · `d:/2/tools/assetripper_out/ExportedProject/Assets/`）】
        //    统一读法：`!u!224` RectTransform 的 `anchorMin/anchorMax/sizeDelta` + `!u!114` 的
        //    `ItemDrawerComponents`（`m_Script.guid = 26d6053ad69df806e09ae9cd86daa71f`）五个槽；
        //    根形 = 根上的 `AspectRatioFitter`（`m_AspectMode` 2=HeightControlsWidth / 3=FitInParent）。
        //    逐张数字与「我们改了哪几处」→ `资料/完全复刻/报告/B114_WC_B223_Icon几何.md`。
        //
        //    #  原版 prefab                          根        主图(image 槽)                  background/label/quantity/armyText
        //    -- ----------------------------------- ---------- ------------------------------ ---------------------------------
        //     2 Icon Avatar Drawer Variant          1080²      `avatar` 过扫 (-307.8,-210.6)-(1387.8,1490.4)  null/null/null/**Army(节点 m_IsActive=0)**
        //     4 Icon Container Drawer Variant       1080²      `Image` 满帧 ×1.1              null/null/null/—
        //     5 Icon Currency Drawer Variant        1080²      `Image` 满帧 ×1.0              null/null/null/—
        //     6 Icon Expansion Pass Points Variant  1080²       `Image` 满帧                   null/null/null/—
        //    10 Icon Random Card Drawer Variant     712.8×1080  `Image` 满帧 · `Random Image` (71.3,108)-(641.5,972)  Background/null/null/—
        //    13 Icon Wildcard Drawer Variant       1080²      `Image` 满帧 ×0.94             null/null/null/—
        //    14 Alliance Badge Drawer Icon         **无 ARF**  `Badge` 满根 · `Frame` 同框     null/null/null/—   ← 🔴 **已移交 W35/W38**（根是固定 165.95×155.74，本节的 `frameR = box` 模型不适用）
        //
        //  【三条如实记（⛔ 别当成原版值）】
        //   ① 「根 1080²」⇒ 用 `FrameOf(box, DrIconRoot, DrIconRoot)`（与 `TitleIcon`/`ForgePointIcon`/`PremiumIcon` 同一套），
        //      非方框调用下按 `min` 等比内接 —— 原版 ARF 在窄容器里是溢出还是按宽缩**还没读**（`ItemDrawerLayout.cs` 文件头 ① 那条 TODO）。
        //   ② #2 的**阵营名不画**：`Icon Avatar Drawer Variant` 的 `Army` 节点 `m_IsActive = 0`（prefab 现读；
        //      **基础档 `Avatar Drawer` 也是 0**），而原版 `AvatarDrawer.Draw` 只写 `armyText.text`、
        //      `SetActive` 一次都没调 ⇒ 屏幕上不出现。⭐ 基础档那条老分支仍在画（不在本波白名单）→ 已记进报告「顺手发现」。
        //   ③ #13 用 `Scale(root, 0.94f)`（`root = FrameOf(...).Map(0,0,1080,1080)`）。方框调用时它**逐值等于**
        //      判据里写的 `Scale(box, 0.94f)`；非方框时按 ① 的同一套内接（⛔ 不复用 `IconFill` —— 那是「居中方块」档的量）。

        /// <summary>`Dispatch` 的 Icon 档分流：命中这 **6** 族返回 `true`（调用方 `return`），否则 `false`（照旧走 switch）。
        /// <para>🔴 **2026-11-01 `B114_W38`（`B-281`）**：第 7 族 `AllianceBadgeDrawer` **已从这里移走**
        /// （`B-277`：它的根是固定尺寸、不跟框走）⇒ 现在由 `ItemDrawerPartial_B114W35.cs` 的 `IconAllianceBadgeFixed`
        /// 在 `Dispatch` 里**更早**分流；本函数对那一族恒返回 `false`（不会再被调用，但保留 `false` 是正确语义）。</para>
        /// <para>⚠️ **只认 `DrawerOverride.Icon`** —— `Horizontal`(15)/`Shop`(20)/`OfferPopups`(30) 三档的变体几何本波没查，⛔ 别顺手接上。</para></summary>
        static bool IconVariant(string drawer, Transform node, PxRect box, ItemSpec item, int quantity,
                                ItemDrawerStyle style, ref ItemDrawResult res)
        {
            switch (drawer)
            {
                case DrawerAvatar:             IconAvatar(node, box, item, quantity, style, ref res); return true;
                case DrawerContainer:          IconContainer(node, box, item, quantity, style, ref res); return true;
                case DrawerCurrency:           IconCurrency(node, box, item, quantity, style, ref res); return true;
                case DrawerExpansionPassPoint: IconExpansionPassPoint(node, box, item, quantity, style, ref res); return true;
                case DrawerRandomCard:         IconRandomCard(node, box, item, quantity, style, ref res); return true;
                case DrawerWildcardIcon:       IconWildcard(node, box, item, quantity, style, ref res); return true;
                // ⛔ `case DrawerAllianceBadge` 于 2026-11-01 `B114_W38`（`B-281`）删除 —— 那一族已由
                //    `ItemDrawerPartial_B114W35.cs` 的 `IconAllianceBadgeFixed`（固定根）在 `Dispatch` 里**更早**分流。
                default: return false;
            }
        }

        /// <summary>#2 `Icon Avatar Drawer Variant`（根 1080² · `avatar` 过扫 · 无背景/名字/数量）。
        /// <para>`avatar` 锚 `-0.285..1.285 × -0.195..1.38`（`sizeDelta 0`）⇒ 设计 px **(-307.8,-210.6)-(1387.8,1490.4)**，
        /// 是**故意的 1.57× 根宽过扫**（整棵 prefab 无 `Mask`）—— ⛔ 别「修」成居中方块。</para></summary>
        static void IconAvatar(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle st,
                               ref ItemDrawResult res)
        {
            var fr = FrameOf(box, DrIconRoot, DrIconRoot, RootFit.HeightControlsWidth);   // ARF 2 · 1（B-260）
            var content = DrawerIconContent(node, fr);
            var tex = CardArt.Cosmetics(item.Art);
            if (!DrawerImage(content, fr.Map(-307.8f, -210.6f, 1387.8f, 1490.4f), tex, item.Art, st, ref res))
                Note("avatar|art|" + (item.Id ?? "?"),
                     "头像 `" + (item.Id ?? "?") + "` 的图名 `" + (item.Art ?? "<空>") + "` 取不到"
                     + "（`CardArt.Cosmetics` 的 `Resources/Art/avatars/`）⇒ 这一层**不画**（⛔ 不拿别的图顶替）");
            // ⛔ 不画阵营名 / 名字 / 数量 —— 见本节「三条如实记」②（`Army` 节点 `m_IsActive = 0`；`label`/`quantity` 槽都是 null）。
        }

        /// <summary>#4 `Icon Container Drawer Variant`（根 1080² · `Image` 满帧 ×1.1 · **无阵营徽记/名字/数量**）。
        /// <para>`armyIcon` 槽在 Icon 变体里是 null（基础档是 `Content/Army Icon`）⇒ 原版这一档根本不画徽记。</para></summary>
        static void IconContainer(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle st,
                                  ref ItemDrawResult res)
        {
            var fr = FrameOf(box, DrIconRoot, DrIconRoot, RootFit.HeightControlsWidth);   // ARF 2 · 1（B-260）
            var content = DrawerIconContent(node, fr);
            var imgR = DrawerFrame.Scale(fr.Map(0f, 0f, DrIconRoot, DrIconRoot), 1.1f);   // prefab `m_LocalScale = 1.1`
            if (!DrawerImage(content, imgR, Tex(item.Art), item.Art, st, ref res))
                Note("container|art|" + (item.Id ?? "?"),
                     "商品容器 `" + (item.Id ?? "?") + "` 的预览图 `" + (item.Art ?? "<空>") + "` 取不到"
                     + "（`ContainerDrawer` 画的是 `ShopContainerBase.PreviewImage`）⇒ 这一层**不画**"
                     + "（⛔ 不拿别的图顶替）");
        }

        /// <summary>#5 `Icon Currency Drawer Variant`（根 1080² · `Image` 满帧 ×1.0 · **整格只有一张主图**）。
        /// <para>原版 Icon 变体里 `background` / `icon`（小图标）/ `label` / `quantity` **四个槽全是 null**
        /// ⇒ 基础档那「底板 + 名条行 + 小图标 + 数量」四段**一段都不建**。</para>
        /// <para>⚠️ `LastCurrency` 同步成「只有主图」那一格（自检读口；基础档那份见 `ItemDrawerCurrency.cs:60-72`）。</para></summary>
        static void IconCurrency(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle st,
                                 ref ItemDrawResult res)
        {
            var tex = Tex(item.Art);
            if (tex == null)                                   // 主图都取不到 ⇒ 与基础档一样退占位板
            {
                LastCurrency = new CurrencyParts();
                Placeholder(node, box, item, quantity, st, ref res);
                res.MissingArt = true;
                return;
            }
            var fr = FrameOf(box, DrIconRoot, DrIconRoot, RootFit.HeightControlsWidth);   // ARF 2 · 1（B-260）
            var content = DrawerIconContent(node, fr);
            LastCurrency = new CurrencyParts { MainImage = true };
            DrawerImage(content, fr.Map(0f, 0f, DrIconRoot, DrIconRoot), tex, item.Art, st, ref res, NodeCurrencyImage);
        }

        /// <summary>#6 `Icon Expansion Pass Points Drawer Variant`（根 1080² · `Image` 满帧 ·
        /// `armyText`/`label`/`quantity` 全 null）。
        /// <para>⚠️ 本族**没有**独立的 Icon 类（`ItemTypeSets` 的 Icon(10) 列 = `ExpansionPassPointDrawer`）
        /// ⇒ 分流只能挂在这一档上。</para></summary>
        static void IconExpansionPassPoint(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle st,
                                           ref ItemDrawResult res)
        {
            int army = item.CardArmy == 0 ? GenericArmyItem.CurrentArmy : item.CardArmy;   // 与基础档同一句（`Draw` 的第 1 句）
            var fr = FrameOf(box, DrIconRoot, DrIconRoot);
            var content = DrawerIconContent(node, fr);
            DrawCampaignGlowIcon(content, fr, st);   // 🆕 B243（B114_W12）：原版 `Content.m_Children[0]` = `Campaign Glow`（恒显、在 `Image` 之前 ⇒ 压在它下面）；判据/队列 → `ItemDrawerPartial_B114W12.cs` 文件头
            var tex = ArmyIcons.Texture(army);
            if (tex != null)
                MenuDraw.Rect(content, tex, fr.Map(0f, 0f, DrIconRoot, DrIconRoot), NodeImage, DrawerArtQueue(st), null,
                              true, st.Clip, st.ClipSoftness);
            else if (army != 0)                                // `Neutral` 没图标是**正常**的（原版 `GetArmyIcon` 同支返 null）
                Note("expansionPoint|army|" + army,
                     "阵营 " + army + " 的图标取不到（`ArmyIcons`）⇒ 这一层不画（原版 `GetArmyIcon` 同支返 null）");
        }

        /// <summary>#10 `Icon Random Card Drawer Variant`（根 **712.8×1080 —— 与我们的 `FrameOf(box)` 同形**，
        /// 不用换帧；`Image` 满帧、`Random Image` (71.3,108)-(641.5,972) = 570.2×864）。
        /// <para>底图两层（`Base Background` / 稀有度 `Background`）与基础档**同一套**（prefab 现读已一致，本波不动）；
        /// 差的是「`image` 满帧 vs 中间横带」+「`Random Image` 收窄」+ 不画阵营名/名字/数量。</para></summary>
        static void IconRandomCard(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle st,
                                   ref ItemDrawResult res)
        {
            var fr = FrameOf(box, fit: RootFit.HeightControlsWidth);   // `Icon Random Card Drawer Variant`：根 712.8×1080、ARF 2 · 0.66（B-260）
            var content = DrawerContent(node, fr);
            int rarity = item.Rarity;                          // = `item.cardRarity`
            int army = item.CardArmy;                          // = `item.cardArmy`（0 = Neutral）

            // ① / ② 底图两层（互斥）—— 与基础档逐字相同
            if (rarity == 0)
            {
                var baseTex = Tex(RandomBaseSprite);
                if (!DrawerImage(content, fr.Map(DrCx1, 0f, DrCx2, DrRootH), baseTex, RandomBaseSprite, st,
                                 ref res, "Base Background"))
                    Note("randomcard|base", "随机卡抽屉的 `Base Background` 图 `" + RandomBaseSprite
                         + "` 取不到 ⇒ 这一层不画（⛔ 不拿别的图顶替）");
            }
            else
            {
                var bgTex = Tex(RandomRaritySprite(rarity));
                if (!DrawerImage(content, fr.Map(DrCx1, 0f, DrCx2, 1134f), bgTex, RandomRaritySprite(rarity), st,
                                 ref res, NodeBackground))
                    Note("randomcard|rarity|" + rarity,
                         "随机卡抽屉的 `cardRarity = " + rarity + "` 没有对应底图"
                         + "（`raritySprites` 只有 1..4 → `" + string.Join("/", RandomRarity) + "`）⇒ 这一层不画");
            }

            // ③ / ④ 右半边（互斥）：中立 ⇒ `Random Image`（570.2×864）；否则 ⇒ 阵营徽记铺满整帧
            if (army == 0)
            {
                var riR = fr.Map(71.3f, 108f, 641.5f, 972f);   // prefab 现读：锚 (0.1,0.1)-(0.9,0.9) of 712.8×1080
                var round = Tex(RandomArmyIconSprite);
                if (round != null)
                {
                    var randomImage = MenuDraw.Rect(content, round, riR, "Random Image", st.QArt, null, true,
                                                    st.Clip, st.ClipSoftness);
                    if (randomImage != null) DrawRandomIconVariant(randomImage.transform, riR, st);
                }
                else
                    Note("randomcard|round", "`Random Image` 的图 `" + RandomArmyIconSprite + "` 取不到 ⇒ 中立那一格空着");
            }
            else
            {
                var aTex = ArmyIcons.Texture(army);
                if (!DrawerImage(content, fr.Map(DrCx1, 0f, DrCx2, DrRootH), aTex, ArmyIcons.IconName(army), st, ref res))
                    Note("randomcard|army|" + army,
                         "随机卡抽屉的阵营 " + army + " 没有徽记图（`ArmyIcons.Texture`）⇒ 原版 `GetArmyIcon` 返 null"
                         + "、那层 `image` 关着，我们也不画");
            }
        }

        /// <summary>#10 的 `Content/Random Image/Icon`（问号 62×117）在 **Icon 变体**里的那一格。
        /// <para>🔴 与基础档的差别（prefab 现读）：Icon 变体的锚是 **(0.2,0.3)-(0.8,0.7)**，
        /// 基础档是 (0.2,0.2)-(0.8,0.8) ⇒ ⛔ 别直接复用 `DrawRandomIcon` 的那四个常量（那是按基础档 `Random Image`
        /// (0.4,108)-(712.4,648) 算出来的）。</para></summary>
        static void DrawRandomIconVariant(Transform parent, PxRect riRect, ItemDrawerStyle st)
        {
            var tex = Tex(RandomIconSprite);
            if (tex == null)
            {
                Note("randomcard|icon", "随机卡抽屉的 `Content/Random Image/Icon` 图 `" + RandomIconSprite
                     + "` 在 `Resources/` 里取不到 ⇒ 这一层不画"
                     + "（导入口 = `工具/import_original_art.py` 的 `MENU_IMAGES` 那一行）");
                return;
            }
            var r = new PxRect(riRect.x1 + riRect.W * 0.2f, riRect.y1 + riRect.H * 0.3f,
                               riRect.x1 + riRect.W * 0.8f, riRect.y1 + riRect.H * 0.7f);
            MenuDraw.Rect(parent, tex, r, "Icon", st.QArt, null, true, st.Clip, st.ClipSoftness);
        }

        /// <summary>#13 `Icon Wildcard Drawer Variant`（根 1080² · `Image` 满帧 ×0.94 · 无背景/名字/数量）。
        /// <para>⚠️ 0.94 是**这一档的固定量**，⛔ 别去改 `ItemDrawerStyle.IconFill`（那个量服务的是
        /// 「居中方块」档，基础档 `Wildcard` 也在用）。</para></summary>
        static void IconWildcard(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle st,
                                 ref ItemDrawResult res)
        {
            var tex = WildcardIconTex(item, ref res);
            if (tex == null)
            {
                Placeholder(node, box, item, quantity, st, ref res);
                res.MissingArt = true;
                return;
            }
            var fr = FrameOf(box, DrIconRoot, DrIconRoot, RootFit.HeightControlsWidth);   // ARF 2 · 1（B-260）
            var r = DrawerFrame.Scale(fr.Map(0f, 0f, DrIconRoot, DrIconRoot), 0.94f);   // prefab `m_LocalScale = 0.94`
            MenuDraw.Rect(node, tex, r, NodeIcon, st.QArt, null, true, st.Clip, st.ClipSoftness);
        }

        // ⛔ **2026-11-01 `B114_W38`（`B-281`）已删除本节的 `IconAllianceBadge(...)`**（原先在这条注释下面）。
        //    原因：`B114_W35`（`B-277`）在 `Dispatch` 的**第一句**插了 `IconAllianceBadgeFixed` 分流、且 `IconVariant`
        //    只从那一行被调 ⇒ 旧实现**一个调用点都没有了**（写的是 `frameR = box` = 跟框画，与 prefab 的固定根相反）。
        //    现在的实现 = `Shell/ItemDrawerPartial_B114W35.cs` 的 `IconAllianceBadgeFixedDraw`（几何）
        //    + `Shell/ItemDrawerPartial_B114W38.cs` 的 `IconAllianceBadgeFixedNamed`（同一份几何 + 节点名改回 `Badge`）。
        //    ⇒ 本节 `IconVariant` **只服务其余 6 族**（Avatar / Container / Currency / ExpansionPassPoint / RandomCard / WildcardIcon）。
    }
}
