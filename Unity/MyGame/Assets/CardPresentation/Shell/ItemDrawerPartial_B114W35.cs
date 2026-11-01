// ItemDrawerPartial_B114W35.cs — 第一百一十四会话 · 写手 `W-35`（2026-11-01）· 账 `B-277`：
//   `Alliance Badge Drawer Icon` 的根是**固定尺寸**（原版**根本不跟调用框走**）
//
// ============================ 这一条是什么 ============================
//   原版 `AllianceBadgeDrawer` 是「同一个类 + 两套 prefab」：
//     · 基础档 `Alliance Badge Drawer`   —— 根挂 `AspectRatioFitter`（`m_AspectMode 2`、`m_AspectRatio 0.66`），
//       锚 (0,0)-(1,1)、`sizeDelta (0,0)` ⇒ **根 = 调用框**（高铺满、宽 = 高×0.66），`Frame`/`Badge` 满根。
//     · Icon 档 `Alliance Badge Drawer Icon` —— **根上没有 ARF**、锚 **(0.5,0.5)-(0.5,0.5)**、
//       `m_AnchoredPosition (0, 44.6322)`、`m_SizeDelta **165.9539 × 155.7357**`、pivot (0.5,0.5)
//       ⇒ 原版**永远画 165.95×155.74 那么大**（相对父框中心上移 44.63），**父框多大都不变**。
//
// ============================ 判据（prefab 直读 · 逐字段）============================
//   文件 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Alliance Badge Drawer Icon.prefab`
//          （**不是 variant**：`m_SourcePrefab` 0 命中；全份 7,572 字节、只有 3 个 GO）
//   · 根 RectTransform `&224618682377417806`（GO `Alliance Badge Drawer Icon`，`m_IsActive 1`）：
//       `m_AnchorMin {0.5,0.5}` · `m_AnchorMax {0.5,0.5}` · `m_AnchoredPosition {0, 44.6322}`
//       `m_SizeDelta {165.9539, 155.7357}` · `m_Pivot {0.5,0.5}` · `m_LocalScale 1`
//   · 根 `m_Component` = **`ItemDrawerComponents` + `AllianceBadgeDrawer`**（**没有 `AspectRatioFitter`**，
//     也没有 `EverguildButton` —— 与基础档的差别就在这里）
//   · `Frame`  `&224099118695303065`：锚 (0,0)-(1,1) · `sizeDelta (0,0)` · pivot (0,0) ⇒ **满根**
//   · `Badge`  `&224709867586078768`：锚 (0,0)-(1,1) · `sizeDelta (0,0)` · pivot (0,0.5) ⇒ **满根**
//   · 两颗 `Image` 都没有 `m_Sprite`（出厂空，由 `AllianceBadgeDrawer.Draw` 运行期灌 —— 与基础档同一条链）
//   · `armyText` / `label` / `quantity` / `background` 四个槽 **全 null**（`ItemDrawerComponents` 实读）
//     ⇒ 这一档只有 `Frame` + `Badge` 两层，没有底板、没有阵营名、没有名字/数量。
//   复核命令：
//     `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Alliance Badge Drawer Icon" --depth 4 --relative`
//     ⇒ 根 `165.95 × 155.74`、`Frame` / `Badge` 都满根。
//
// ============================ 我们原来错在哪 ============================
//   `B114_WC`（`ItemDrawerPartial_B111R3W.cs` 的 `IconAllianceBadge`）写的是
//       `var frameR = box;   // 无 ARF ⇒ `Frame`/`Badge` 的锚 (0,0)-(1,1) 就是整个根`
//   —— 「锚 (0,0)-(1,1)」这一句只解释了 **`Frame`/`Badge` 相对根** 的锚，**没读根的锚与 `sizeDelta`**
//   ⇒ 等效于「把根当成调用框」⇒ **框大画大、框小画小**（Pareto：166×156 的调用框下才碰巧对）。
//   正确模型 = 「**固定尺寸**、以框心为基准 + 原版那 44.63 的上移」：
//       root = center(box.CX, box.CY − 44.6322) ± (165.9539/2, 155.7357/2)
//   （Unity 的 `m_AnchoredPosition.y` **向上为正**，本工程 `PxRect` **y 向下** ⇒ 那一格减、不是加。）
//
// ============================ 为什么放在这个文件 / 怎么接 ============================
//   `B114_WC` 那份 `IconAllianceBadge(...)` 在 `ItemDrawerPartial_B111R3W.cs`（**不在本写手白名单**）
//   ⇒ 本文件**另起一条**正确实现，由 `Shell/ItemDrawer.cs` 的 `Dispatch` **第一行同物理行**、在
//   `IconVariant(...)` **之前**分流（零行数变化，账的 A 落点不动）：
//       if (ov == DrawerOverride.Icon && IconAllianceBadgeFixed(drawer, node, box, item, quantity, style, ref res)) return;
//   ⚠️ **副作用（如实记）**：接上之后 `ItemDrawerPartial_B111R3W.cs:280-298` 的 `IconAllianceBadge` 就**没有
//   任何调用点了**（`IconVariant` 只从 `Dispatch` 那一行被调）。它该由那份文件的所有者删掉，
//   ⛔ 本文件不动别人的文件。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        // ============================================================ B114_W35 · B-277：`Alliance Badge Drawer Icon` 的固定根（2026-11-01）
        //
        //  数字出处 = 上面「判据」那一节的 prefab 字段（⛔ 别从调用框反推、也别拿 `FrameOf` 的任何口径去凑）。

        /// <summary>`Alliance Badge Drawer Icon` 根的**设计尺寸**（`m_SizeDelta 165.9539 × 155.7357`）。</summary>
        public const float AllianceBadgeIconW = 165.9539f, AllianceBadgeIconH = 155.7357f;
        /// <summary>根相对父框中心的**上移量**（`m_AnchoredPosition.y = 44.6322`，Unity 向上为正 ⇒ 本工程 y 向下要减）。</summary>
        public const float AllianceBadgeIconAnchorY = 44.6322f;

        /// <summary>原版**根**在这一格框里的矩形（左上原点 · y 向下）。
        /// <para>⚠️ 与 `RootRectOf` 的区别：那个是「跟框走」的两条口径（ARF / 内接）；**这一档压根不跟框** ——
        /// 矩形**只由 prefab 常量决定**，框只决定它的**中心**在哪。</para></summary>
        public static PxRect AllianceBadgeIconRootOf(PxRect box)
        {
            float cx = box.CX, cy = box.CY - AllianceBadgeIconAnchorY;
            return new PxRect(cx - AllianceBadgeIconW * 0.5f, cy - AllianceBadgeIconH * 0.5f,
                              cx + AllianceBadgeIconW * 0.5f, cy + AllianceBadgeIconH * 0.5f);
        }

        /// <summary>`Dispatch` 的 Icon 档分流（`AllianceBadgeDrawer` 那一族用**固定尺寸根**）。
        /// 命中返回 `true`（调用方 `return`），否则 `false`（照旧走 `IconVariant` / switch）。
        /// <para>⚠️ **只认 `DrawerAllianceBadge` + `DrawerOverride.Icon`** —— 其余档一律 `false`（形参表与
        /// `IconVariant` 同形，方便在 `Dispatch` 那一行并排摆放）。</para></summary>
        static bool IconAllianceBadgeFixed(string drawer, Transform node, PxRect box, ItemSpec item, int quantity,
                                           ItemDrawerStyle style, ref ItemDrawResult res)
        {
            if (drawer != DrawerAllianceBadge) return false;
            IconAllianceBadgeFixedDraw(node, box, item, quantity, style, ref res);
            return true;
        }

        /// <summary>原版 `AllianceBadgeDrawer.Draw` 在 `Alliance Badge Drawer Icon` 上的等价实现。
        /// <para>与基础档 `AllianceBadge(...)`（`ItemDrawerAlliance.cs`）的差别**只有几何**：
        /// ① 没有 `Content` 层（`Frame`/`Badge` 直接挂根）；
        /// ② 根是**固定 165.95×155.74**（见 <see cref="AllianceBadgeIconRootOf"/>），⛔ 不跟框缩放；
        /// ③ 没有底板 / 阵营名 / 名字 / 数量（`background`/`armyText`/`label`/`quantity` 四个槽全 null）。
        /// 「`SetTierFrame(quantity)`（tier = **quantity** 不是 `item.tier`）+ 未达档置灰」两句**照旧保留**
        /// （与几何无关，判据 → `ItemDrawerAlliance.cs` 文件头）。</para>
        /// <para>⚠️ `Frame` / `Badge` 两份 `Image` 都是 `m_PreserveAspect = 1`（`AllianceBadgeArt.Draw*`
        /// 内部已传 `keepAspect: true`）⇒ 方图在这条非方框里画成 `155.74 × 155.74`、居中。</para></summary>
        static void IconAllianceBadgeFixedDraw(Transform node, PxRect box, ItemSpec item, int quantity,
                                               ItemDrawerStyle st, ref ItemDrawResult res)
        {
            if (string.IsNullOrEmpty(item.BadgeId) && string.IsNullOrEmpty(item.Id))
                return;                                        // 原版 `if (!item) return;`（没有徽章数据 ⇒ 什么都不画）

            var rootR = AllianceBadgeIconRootOf(box);           // 固定尺寸 + 上移 44.63（⛔ 不是 `box`）
            if (AllianceBadgeArt.DrawTierFrame(node, rootR, quantity, NodeTrophyFrame, st.QArt) != null)
                res.Art = AllianceTrophyArt.TierFrameName(quantity);
            string badgeId = string.IsNullOrEmpty(item.BadgeId) ? item.Id : item.BadgeId;
            var badge = AllianceBadgeArt.Draw(node, rootR, badgeId, NodeTrophyIcon, st.QArt + 1);
            if (badge != null)
            {
                res.Art = AllianceBadgeArt.SpriteNameOf(badgeId);
                if (quantity <= 0) badge.SetGrey(1f);           // 原版 `DoMaterialRefresh(interactable = tier > 0)`
            }
            else
                res.MissingArt = true;                         // `AllianceBadgeArt.Get` 已 `LogError` 一次（不静默）
        }
    }
}
