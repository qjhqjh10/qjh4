// ============================================================================ 联盟徽章抽屉（原版 `AllianceBadgeDrawer`）
//
//  判据（施工单 B18；方法体 = `cpp2il/out_cpp2il_dev_ilspy/AllianceBadgeDrawer.cs` + `.c:20-63`）：
//    `Draw(AllianceTrophyData item, int quantity = 1)`
//      `if (!item) return;`
//      `Components.image.sprite = item.reward.Reference.badgeImage.Load();`
//      `SetTierFrame(**quantity**);`                              ← 🔴 实参是 quantity，**不是 `item.tier`**
//      `if ((bool)armyText) armyText.text = CardArmyToString(item.reward.Reference.cardArmy);`
//    `SetTierFrame(int tier)` 体内：`UnloadTierFrame(旧)` → `tierFrames[Math.Max(0, tier-1)].Load()` →
//      对子件 `EverguildButtonMaterialModifier` 做 `DoMaterialRefresh(interactable = tier > 0)`（材质刷新，见下）
//
//  零件本机已齐（施工单 B18「还差什么」✅）⇒ **直接调，不重写**：
//    · `Shell/AllianceBadgeArt.cs` 的 `Draw(parent, rect, badgeId, name, q)`（含「查不到 id ⇒ 回落 `Default_Badge`」
//      那条原版语义）· `DrawTierFrame(parent, rect, tier, name, q)` → `AllianceTrophyArt.GetTierFrame`
//    · 徽章图与档位框的 `Resources/Art/ui_menu/Alliance_Trophies_*.png` 现读都在。
//
//  ── 版面（prefab 实读 · `Alliance Badge Drawer`）──────────────────────────────
//    根 712.8×1080 · `Content/Background` 铺满 · `Content/Frame` **(0.4,216)-(712.4,648)** =
//    `tierFrame`（档位框）· 其子件 `Content/Frame/Trophy Icon` **同矩形** = `Components.image`（徽章本体）·
//    `Content/Army` (0.4,702)-(712.4,810) · `Content/Label`（`Quantity` + `Name`；`Quantity` 出厂 INACT 但
//    槽在 ⇒ 原版会写它）· ⚠️ `Alliance Badge Drawer Icon` 变体 `armyText` 是 null ⇒ 那一档不画阵营名
//    （本函数只服务基础档；`Icon` 档的差别见报告「未覆盖」）。
//
//  ✅ 原版 `SetTierFrame` 末尾那句 `DoMaterialRefresh(interactable = tier > 0)` **已实现**（2026-10-30 `B101_R8`）：
//     未达档（`qty <= 0`）⇒ 徽章本体 `SetGrey(1)`。作用范围 = `Trophy Icon`（= `Components.image`）+ 两层价格显示，
//     **档位框不灰**（prefab 现读：`Frame` 上没有 `EverguildButtonMaterialModifier`）。
//     ⚠️ 机制不同、行为等价：原版换 `Everguild/UI/Greyscale` 材质，我们走同名的 `_GreyScale` 属性。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `AllianceBadgeDrawer : ItemDrawer&lt;AllianceTrophyData&gt;`。</summary>
        public const string DrawerAllianceBadge = "AllianceBadgeDrawer";

        /// <summary>原版 `AllianceBadgeDrawer.Draw`。</summary>
        static void AllianceBadge(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                  ref ItemDrawResult res)
        {
            var fr = FrameOf(box);
            var content = DrawerContent(node, fr);
            DrawerBackground(content, fr, st);

            if (string.IsNullOrEmpty(item.BadgeId) && string.IsNullOrEmpty(item.Id))
                return;                                  // 原版 `if (!item) return;`（没有徽章数据 ⇒ 什么都不画）

            var frameR = fr.Map(DrCx1, DrImgY1, DrCx2, DrImgY2);
            // ① 档位框（`tierFrames[Math.Max(0, tier-1)]`，tier = **quantity**）
            if (AllianceBadgeArt.DrawTierFrame(content, frameR, qty, NodeTrophyFrame, st.QArt) != null)
                res.Art = AllianceTrophyArt.TierFrameName(qty);
            // ② 徽章本体（`item.reward.Reference.badgeImage`）—— 查不到 id ⇒ 原版回落 `Default_Badge`
            string badgeId = string.IsNullOrEmpty(item.BadgeId) ? item.Id : item.BadgeId;
            var badge = AllianceBadgeArt.Draw(content, frameR, badgeId, NodeTrophyIcon, st.QArt + 1);
            if (badge != null)
            {
                res.Art = AllianceBadgeArt.SpriteNameOf(badgeId);
                // ③ 原版 `SetTierFrame` 末尾：`DoMaterialRefresh(interactable = tier > 0)` —— 未达档的徽章**置灰**。
                //    作用范围 = `GetComponentsInChildren<EverguildButtonMaterialModifier>()`（prefab 现读 = 5 个：
                //    **`Trophy Icon` 就是 `Components.image` 这一颗** + 两层价格显示的 icon/text）⇒ **档位框不灰**。
                //    ⚠️ 机制不同、行为等价（铁律 18）：原版把材质换成 `Everguild/UI/Greyscale`（`_GreyScale = 1`），
                //    我们走 `ImageQuad.SetGrey(1)`（同一属性名 `_GreyScale`，见 `Battle/ImageQuad.cs` 的 `SetGrey` doc）。
                if (qty <= 0) badge.SetGrey(1f);
            }
            else
                res.MissingArt = true;                   // `AllianceBadgeArt.Get` 已 `LogError` 一次（不静默）

            // ③ `armyText.text = CardArmyToString(item.reward.Reference.cardArmy)`
            DrawerArmyName(content, fr, item.CardArmy, st);
            // ④ `label` + `quantity`（两颗槽都在）
            DrawerNameQuantity(content, fr, item, qty, st, hasName: true, hasQuantity: true);
        }
    }
}
