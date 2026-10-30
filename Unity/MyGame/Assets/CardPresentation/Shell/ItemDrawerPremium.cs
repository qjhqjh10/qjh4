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
//      我们 `Resources/` 里没有它的出厂图 `40K_genearl_icon_Campaign points_big` ⇒ 按「无图 ⇒ 关掉」实现。
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
            var fr = FrameOf(box);
            var content = DrawerContent(node, fr);
            DrawerBackground(content, fr, st);

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
            var fr = FrameOf(box, DrIconRoot, DrIconRoot);
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
    }
}
