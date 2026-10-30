// ============================================================================ 点数三档（`ForgePointDrawer` / `ForgePointIconDrawer` / `ExpansionPassPointDrawer`）
//
//  判据（施工单 B15 / B16 / B17；方法体 = `cpp2il/out_cpp2il_dev_ilspy/*.cs`）：
//    · `ForgePointDrawer.Draw` —— **一句**：`DrawForArmy(CardArmy.Neutral);`（Ghidra 版那句的实参确实是中性，
//      `item.cardArmy` 那一格**没被读**）。`DrawForArmy(army)` 体内两句：
//        `if (armyText) { armyText.gameObject.SetActive(army != Neutral); armyText.text = CardArmyToString(army); }`
//        `if (image)    { image.gameObject.SetActive(army != Neutral);    image.sprite = GetArmyIcon(army); }`
//      🔴 ⇒ **默认这一档两层都关**（`army == Neutral`）。这是原版真行为，⛔ 别「修好」成画一个默认图标。
//    · `ForgePointIconDrawer.Draw` —— 一句：`image.sprite = GetForgeIconForArmy(CardArmy.Neutral);`
//      （ILSpy `:1-10` 写死 `Neutral`；`.c` 里那个 `+0x2c` 是形参位置复用，⛔ 别读成 `item.cardArmy`）
//      ⇒ `Neutral` 查不到锻造图标 ⇒ `sprite = null`（**那层 `Image` 仍启用、只是空**）。旁边的 `DrawForArmy(army)`
//      才是「按阵营取」的那条路，`Draw` 不走它。
//    · `ExpansionPassPointDrawer.Draw`：
//        `CardArmy army = (item.cardArmy == 0) ? LiveOpsManager.GetHandler<CampaignHandler>().SelectedArmy : item.cardArmy;`
//        然后与 `DrawForArmy` 同样的两句（**这一档不关**，只写 text/sprite）。
//      ⚠️ `.c` 里 `GetHandler` 的泛型实参是未解出的 `DAT_`，**ILSpy 写的是 `CampaignHandler`**（施工单 B17 ①）
//      ⇒ 按 ILSpy 实现；我们那一格的等价物 = `GenericArmyItem.CurrentArmy`（默认取 `DailyData.HeaderArmyValue`，
//        判据 → `ItemDrawer.cs` 的 `GenericArmyItem.CurrentArmyProvider`）。
//
//  ── 版面（prefab 实读）──────────────────────────────────────────────────────
//    `Forge Points Drawer` / `Expansion Pass Points Drawer`：根 712.8×1080 · `Content/Image` (0.4,216)-(712.4,648) ·
//      `Content/Army` (0.4,702)-(712.4,810) · `Content/Label`（`Quantity` + `Name`）
//    `Icon Forge Points Drawer Variant`：根 **1080×1080**、`Content` 1080²、图节点名是 **`Army`**、铺满
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `ForgePointDrawer : ItemDrawer&lt;ForgePoints&gt;`。</summary>
        public const string DrawerForgePoint = "ForgePointDrawer";
        /// <summary>原版 `ForgePointIconDrawer : ItemDrawer&lt;ForgePoints&gt;`（`Icon` 档）。</summary>
        public const string DrawerForgePointIcon = "ForgePointIconDrawer";
        /// <summary>原版 `ExpansionPassPointDrawer : ItemDrawer&lt;ExpansionPassPoints&gt;`。</summary>
        public const string DrawerExpansionPassPoint = "ExpansionPassPointDrawer";

        /// <summary>原版 `ForgePointDrawer.Draw` —— 传的**恒是 `Neutral`** ⇒ 两层都关（见文件头）。</summary>
        static void ForgePoint(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                               ref ItemDrawResult res)
        {
            PointsForArmy(node, box, item, qty, st, ref res, 0, 0, hideOnNeutral: true, forgeIcon: false,
                          noteKey: "forgePoint");
        }

        /// <summary>原版 `ForgePointIconDrawer.Draw` —— `GetForgeIconForArmy(Neutral)` ⇒ 取不到 ⇒ 空。
        /// Root 1080²、图节点名 `Army`、铺满。⚠️ 那层 `Image` 原版**仍启用**（只是 sprite 为 null）
        /// ⇒ 我们也不画占位、也不出声吵（`Neutral` 没图标是**正常**的，不是缺资产）。</summary>
        static void ForgePointIcon(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                   ref ItemDrawResult res)
        {
            var fr = FrameOf(box, DrIconRoot, DrIconRoot);
            var content = DrawerIconContent(node, fr);
            var tex = Tex(ArmyIcons.ForgeIconName(0));      // `Neutral` ⇒ `ForgeSpriteName(0)` 返 null ⇒ `Tex(null)` = null
            if (tex != null)                                 // 今天恒 null；图上真有了也照画
                MenuDraw.Rect(content, tex, fr.Map(0f, 0f, DrIconRoot, DrIconRoot), "Army", st.QArt, null, true,
                              st.Clip, st.ClipSoftness);
        }

        /// <summary>原版 `ExpansionPassPointDrawer.Draw` —— `cardArmy == 0` ⇒ 用「当前选中阵营」。</summary>
        static void ExpansionPassPoint(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                       ref ItemDrawResult res)
        {
            int army = item.CardArmy == 0 ? GenericArmyItem.CurrentArmy : item.CardArmy;
            PointsForArmy(node, box, item, qty, st, ref res, army, army, hideOnNeutral: false, forgeIcon: false,
                          noteKey: "expansionPoint");
        }

        /// <summary>「阵营徽记 + 阵营名」那两口（`ForgePointDrawer.DrawForArmy` 与
        /// `ExpansionPassPointDrawer.Draw` 的公共形）。`hideOnNeutral` = 原版有没有那两句
        /// `SetActive(army != Neutral)`（`ForgePointDrawer` 有、`ExpansionPassPointDrawer` **没有**）。</summary>
        static void PointsForArmy(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                                  ref ItemDrawResult res, int armyIcon, int armyName, bool hideOnNeutral,
                                  bool forgeIcon, string noteKey)
        {
            var fr = FrameOf(box);
            var content = DrawerContent(node, fr);
            DrawerBackground(content, fr, st);

            if (!hideOnNeutral || armyIcon != 0)
            {
                var tex = forgeIcon ? Tex(ArmyIcons.ForgeIconName(armyIcon)) : ArmyIcons.Texture(armyIcon);
                if (tex != null)
                    MenuDraw.Rect(content, tex, fr.Map(DrCx1, DrImgY1, DrCx2, DrImgY2), NodeImage, st.QArt, null,
                                  true, st.Clip, st.ClipSoftness);
                else if (armyIcon != 0)
                    Note(noteKey + "|army|" + armyIcon,
                         "阵营 " + armyIcon + " 的图标取不到（`ArmyIcons`）⇒ 这一层不画"
                         + "（原版 `GetArmyIcon` 同支返 null）");
            }
            if (!hideOnNeutral || armyName != 0) DrawerArmyName(content, fr, armyName, st);

            DrawerNameQuantity(content, fr, item, qty, st, hasName: true, hasQuantity: true);
        }
    }
}
