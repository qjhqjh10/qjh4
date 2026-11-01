// ============================================================================ 抽屉补齐 · 第 25 档（原版 `GenericArmyItemDrawer`）
//
//  为什么还要单开一份：施工单 `判据/B100_D1_抽屉18档_施工单.md` 表 C2 把这一档记成
//  「本工程零消费点，可延后」⇒ 2026-10-30 `B101_E3` 那轮没做 ⇒ `DrawerImpls` 只覆盖 24/25。
//  本代理（`B109_E11`，2026-10-31）按铁律 11（全都要做，只有先后）把它补上 —— 补完
//  **25/25 全档登记**，`PickDrawer` 那条「这个抽屉我们没实现」的告警对原版全族都不会再响。
//
//  ── 判据（逐句）───────────────────────────────────────────────────────────────
//  · 方法体（第一权威）：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/GenericArmyItemDrawer.cs:12-37`
//    （`decomp_full/GenericArmyItemDrawer__Draw.c` 同形）：
//      `if (drawer) { if (ReflectionHelper.Is(item, drawer.ObtainableItemTypeToDraw)) { drawer.Draw(item, quantity); return; } }`
//      `if (drawer) Destroy(drawer);`
//      `obtainableItem = item.GetItem();`
//      `drawer = ItemDrawer.Draw(holder, obtainableItem, quantity, this.override /* +0x20 */);`
//      `if (drawer) drawer.ToggleShowInfoOnClick(false);`
//  · prefab 实读（2026-10-31 本代理现核，`bundle_menus_assets_all`）：
//    `MonoBehaviour_1212849660543184768.json`（`m_Script` = `GenericArmyItemDrawer`，全包**仅 1 个实例**）：
//      `showItemInfoOnClick = 1` · `holder` = `{m_FileID:0, m_PathID:5357205478649376640}` = **`Content` 节点**
//      （`GameObject/Content_-1449735925700546688.json` 的第一颗组件就是它；`RectTransform_-7212961018821857408`
//       的父 = 0 ⇒ 它的父就是 prefab 根）
//    `Generic Army Item Drawer` 树（`menu_dump --depth 3`）：根 **712.8×1080**（ARF 高控宽 0.66）·
//      `Content` (0.4,0) → (712.4,1080) = **712×1080** · `ItemDrawerComponents` 除 `ephemeralDisplay/ephemeralText`
//      外**全 null**，类自己的字段只有 `holder`（`drawer` 是运行期字段、不序列化）⇒ **外层自己什么也不画**，
//      整格内容全部由内层抽屉画在 `Content` 里。
//
//  ── 两条「如实记」（⛔ 别把下面这些当原版值）────────────────────────────────────
//  ① **`ReflectionHelper.Is` 那一支（复用）在真实数据上恒不命中**：`drawer.ObtainableItemTypeToDraw` 是**内层物品**
//     的类型（例如 `Wildcard`），而这一支传进去的是**外层 `GenericArmyItem`** ⇒ 只有「内层物品自身也是
//     `GenericArmyItem`」才为真。原版对真实掉落表**每一次都走 destroy + 重建** ⇒ 我们照「每票重建」实现
//     （`MenuDraw` 树的清空/重建就是那条路的等价物），**不另造一层实例缓存**。
//  ② **内层 `drawerOverride` 的来路**：`…__Draw.c` 把外层 `+0x20`（`ItemDrawer.override`）当第 4 个实参传给
//     `ItemDrawer.Draw` ⇒ 内层**继承外层的档**。我们的 `Dispatch` 原来不带这一格 ⇒ 本轮给它加了**可选形参**
//     （`Draw` 那一跳传真的 override）；⚠️ `ItemDrawerView.Refresh()` 那条重画入口拿不到 override（视图没存它）
//     ⇒ 重画时内层按 `Default` 选档（与原版差异**只可能**出现在「外层是 Icon/Shop/… 档 + 内层是
//     `GenericArmyItem` 物品」这一组合；今天零消费点，出声记在 `Note` 里）。
using UnityEngine;

namespace CardPresentation
{
    public partial struct ItemSpec
    {
        /// <summary>原版 `GenericArmyItem` 那张「阵营 → 物品」表（施工单 §四-2 列的 `Nested` 那一格）。
        /// <para>`null` = 判据空（不是这种物品）⇒ <see cref="ItemDrawer"/> 的 `GenericArmyItemDrawer` 那一档
        /// **出声 + 占位板**，⛔ 不静默画成别的样子。</para>
        /// <para>⚠️ 与 `ItemDrawerSpecExtra.cs` 里那几个字段同一条纪律：本体在 `ItemDrawer.cs:123` 被账钉着行号，
        /// 新字段一律写在 partial 续块里（铁律 18·b）。</para></summary>
        public GenericArmyItem Nested;
    }

    public static partial class ItemDrawer
    {
        /// <summary>原版 `GenericArmyItemDrawer : ItemDrawer&lt;GenericArmyItem&gt;`（第 25 个、也是最后一个未登记的类）。</summary>
        public const string DrawerGenericArmy = "GenericArmyItemDrawer";

        /// <summary>自检读口：最近一次画 `GenericArmyItemDrawer` 的实测结果（静态 ⇒ 连画多件时读最后一次）。</summary>
        public struct GenericArmyParts
        {
            /// <summary>`holder`（= prefab 的 `Content` 节点）建出来了。</summary>
            public bool Holder;
            /// <summary>`holder` 这一格的矩形（= `Content`：0.4..712.4 × 0..1080 经 `FrameOf` 折算）。</summary>
            public PxRect HolderRect;
            /// <summary>内层抽屉真的画出来了（`ItemDrawer.Draw` 返回了节点）。</summary>
            public bool InnerDrawn;
            /// <summary>内层物品 id（`GenericArmyItem.GetItem()` 取到的那条）。</summary>
            public string InnerId;
            /// <summary>内层实际用的抽屉（原版语汇里 = 被复用/新建的那个 `ItemDrawer` 实例的类名）。</summary>
            public string InnerDrawer;
            /// <summary>没画出内层时的原因（`"no-table"` / `"no-entry"` / `"no-drawer"` / `"depth"`）。</summary>
            public string Fail;
        }

        /// <summary>读口：最近一次 <see cref="GenericArmy"/> 的实测结果。</summary>
        public static GenericArmyParts LastGenericArmy;

        /// <summary>🔶 自己设计（非原版）：递归深度上限 4 —— 原版没有这道闸，但 `GenericArmyItem.GetItem()` 若被喂成
        /// 自引用（内层物品又是 `GenericArmyItem`）原版会无限递归；本工程零消费点、判据表由调用方手喂，加一道闸只为
        /// 不让一次夹具笔误把 Unity 批处理打崩。超限 ⇒ 出声 + 占位板（⛔ 不静默）。调度台 2026-10-31 裁定。</summary>
        const int GaMaxDepth = 4;
        static int _gaDepth;

        /// <summary>原版 `GenericArmyItemDrawer.Draw`（逐句见文件头）。
        /// <para>外层自己**不画任何图/字**（`ItemDrawerComponents` 六个槽除 ephemeral 那两颗外全 null）——
        /// 建 `Content`（= prefab 的 `holder`）→ 取内层物品 → `ItemDrawer.Draw(holder, inner, qty, override)`
        /// → 内层强制 `ToggleShowInfoOnClick(false)`。</para></summary>
        static void GenericArmy(Transform node, PxRect box, ItemSpec item, int quantity, ItemDrawerStyle st,
                                DrawerOverride ov, ref ItemDrawResult res)
        {
            LastGenericArmy = new GenericArmyParts();

            // holder = prefab 的 `Content`（0.4..712.4 × 0..1080，`FrameOf` 折进本格）
            var fr = FrameOf(box, fit: RootFit.HeightControlsWidth);   // `Generic Army Item Drawer`：ARF `m_AspectMode 2`（0.66）（B-260）
            var holder = DrawerContent(node, fr);
            var holderRect = fr.Map(DrCx1, 0f, DrCx2, DrRootH);
            LastGenericArmy.Holder = holder != null;
            LastGenericArmy.HolderRect = holderRect;
            if (holder == null) { LastGenericArmy.Fail = "no-drawer"; return; }

            // `item.GetItem()`：表在 `ItemSpec.Nested`（原版是 `GenericArmyItem` 的序列化 `entries`）
            if (item.Nested == null)
            {
                Note("genericarmy|table|" + (item.Id ?? "?"),
                     "`GenericArmyItemDrawer` 那一档**没有阵营表**（`ItemSpec.Nested` = null）⇒ 取不出内层物品，"
                     + "画占位板（原版这一步会 NRE；我们出声、⛔ 不静默）");
                LastGenericArmy.Fail = "no-table";
                Placeholder(holder, holderRect, item, quantity, st, ref res);
                return;
            }

            var inner = item.Nested.GetItem();
            if (inner.Id == null)
            {
                Note("genericarmy|entry|" + (item.Id ?? "?") + "|" + GenericArmyItem.CurrentArmy,
                     "`GenericArmyItem` 表里**没有当前阵营** `" + GenericArmyItem.CurrentArmy + "` 那一条"
                     + "（原版 `GetItem` 返回 null）⇒ 画占位板，⛔ 不拿别的阵营顶");
                LastGenericArmy.Fail = "no-entry";
                Placeholder(holder, holderRect, item, quantity, st, ref res);
                return;
            }

            if (_gaDepth >= GaMaxDepth)
            {
                Note("genericarmy|depth|" + (item.Id ?? "?"),
                     "`GenericArmyItem` 表套娃超过 " + GaMaxDepth + " 层（疑似自引用）⇒ 停在占位板");
                LastGenericArmy.Fail = "depth";
                Placeholder(holder, holderRect, item, quantity, st, ref res);
                return;
            }

            // `ItemDrawer.Draw(holder, obtainableItem, quantity, this.override)` —— 内层继承外层的档
            var innerStyle = st;
            innerStyle.NodeName = "Item Drawer";          // 原版内层是 instantiate 的抽屉 prefab（根名 = prefab 名）
            _gaDepth++;
            ItemDrawResult innerRes;
            try { innerRes = Draw(holder, holderRect, inner, quantity, ov, innerStyle); }
            finally { _gaDepth--; }

            LastGenericArmy.InnerId = inner.Id;
            LastGenericArmy.InnerDrawer = innerRes.Drawer;
            LastGenericArmy.InnerDrawn = innerRes.Node != null;
            if (innerRes.Node == null)
            {
                LastGenericArmy.Fail = "no-drawer";
                Note("genericarmy|inner|" + inner.Id,
                     "内层物品 `" + inner.Id + "`（类型 `" + (inner.Type ?? "<空>") + "`）选不出抽屉"
                     + "（`PickDrawer` 已为它单独出声）⇒ 这一格是空的");
            }
            else
            {
                // `drawer.ToggleShowInfoOnClick(false)` —— **内层不响应点击**（点上去归外层那条 `HandlePointerClick`）
                if (innerRes.View != null) innerRes.View.SetShowInfoOnClick(false);
            }

            res.Art = innerRes.Art;
            res.Placeholder = innerRes.Placeholder;
            res.MissingArt = innerRes.MissingArt;
            res.FallbackArt = innerRes.FallbackArt;
        }

        // ============================================================ A2：`WildcardIconDrawer` 的 `FirstOrDefault` 兜底
        //
        //  施工单表 A2：原版 `WildcardIconDrawer__Draw.c:20-30` 是**两句**
        //    `i = rarity - 1;  if (i >= iconsByRarity.Count) image.sprite = iconsByRarity.FirstOrDefault();`
        //    `image.sprite = iconsByRarity[i];`
        //  第二句**恒覆盖**第一句 ⇒ 原版那条兜底实际是**死码**（`iconsByRarity` 恒 4 条、`rarity ∈ 1..4`），
        //  而越界时第二句会 `List.get_Item` 抛异常。我们**照形状实现兜底这一支**（越界 ⇒ 取第一条可用图 + 出声），
        //  ⛔ **不复制那个崩溃**（原版本 build 里这一支进不去，复制崩溃只会把我们的批处理打崩）。

        /// <summary>`iconsByRarity.FirstOrDefault()` 的等价物 = **第一条取得到的稀有度图标**
        /// （顺序 = `common → rare → epic → legendary`）。全取不到 ⇒ `null`（调用方保持「不画 + 占位」）。</summary>
        public static string WildcardIconFirstRarity()
        {
            for (int i = 0; i < WildcardRarity.Length; i++)
            {
                string n = "40k_general_wildcard_" + WildcardRarity[i];
                if (Tex(n) != null) return n;
            }
            return null;
        }

        /// <summary>`WildcardIconDrawer` 的取图（两句照原版，见上面 A2 那节）。
        /// <para>① `i = rarity−1`；越界 ⇒ `FirstOrDefault(iconsByRarity)` 那一条（= <see cref="WildcardIconFirstRarity"/>）。
        /// ② 第二句 `iconsByRarity[i]` 恒覆盖 —— 在域内（`rarity ∈ 1..4`）就是原来那条 `WildcardTex` 链
        /// （主图 → `_small` 退档 → 数据层那张表）。域外**保留 ①** 的结果（原版会抛，我们出声不抛）。</para>
        /// <para>⚠️ 这是施工单表 A2 的施工项：原来我们对越界是「取不到 ⇒ 不画」，比原版**少画一层**（死码兜底也算形状）。</para></summary>
        internal static Texture2D WildcardIconTex(ItemSpec item, ref ItemDrawResult res)
        {
            int i = item.Rarity - 1;
            if (i < 0 || i >= WildcardRarity.Length)
            {
                string first = WildcardIconFirstRarity();          // `iconsByRarity.FirstOrDefault()`
                if (first != null)
                {
                    Note("wildcardicon|rarity|" + item.Rarity,
                         "`WildcardIconDrawer` 收到域外的 `cardRarity = " + item.Rarity + "`（原版在此会 `List.get_Item` 抛）"
                         + " ⇒ 照原版那句 `FirstOrDefault` 画第一条可用图 `" + first + "`（`FallbackArt` 置位）");
                    res.Art = first;
                    res.FallbackArt = true;
                    return Tex(first);
                }
                Note("wildcardicon|none|" + item.Rarity, "`iconsByRarity` 四条**一条都取不到** ⇒ 这一档画占位板");
            }
            return WildcardTex(item, ref res);                     // 原版第二句 `image.sprite = iconsByRarity[i]`
        }
    }
}
