// ItemDrawerPartial_B114W38.cs — 第一百一十四会话 · 写手 `W-38`（2026-11-01）· 账 `B-282②`：
//   `Alliance Badge Drawer Icon` 的徽章节点名 = 原版的 **`Badge`**，我们建的是 `Trophy Icon`
//
// ============================ 这一条是什么 ============================
//   原版 `Alliance Badge Drawer Icon.prefab` 的根下**只有两颗**子件（逐字段现读）：
//     · `Frame` `&224099118695303065` —— 档位框（锚 (0,0)-(1,1)、`sizeDelta (0,0)`、pivot (0,0)）⇒ **满根**
//     · `Badge` `&224709867586078768` —— 徽章本体（锚 (0,0)-(1,1)、`sizeDelta (0,0)`、pivot (0,0.5)）⇒ **满根**
//   ⇒ **Icon 档那颗叫 `Badge`**；而**基础档** `Alliance Badge Drawer` 同一位置那颗叫
//     `Content/Frame/Trophy Icon`（两套 prefab 各自的名字，⛔ 不是一个通则）。
//   我们原来的实现（`B114_WC` 的 `IconAllianceBadge`，现在 = `B114_W35` 的 `IconAllianceBadgeFixedDraw`）
//   **沿用了基础档的节点名** `NodeTrophyIcon`（= `Trophy Icon`）。
//   ⇒ **画面一模一样**（节点名不参与渲染），但**按名查树**的断言、以及将来逐节点核 prefab 时会张冠李戴。
//
// ============================ 为什么在这里、而不是改 W35 那一份 ============================
//   `Shell/ItemDrawerPartial_B114W35.cs` 是 `W-35` 的产物（**本波不归我写**）⇒ 本文件在它外面包一层：
//     · 几何**仍旧只有一份**（转调 `IconAllianceBadgeFixedDraw`，⛔ 不复制那套常量/算式 ——
//       `CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）；
//     · 建完之后**当场把那一颗改名**（`node.Find(NodeTrophyIcon).name = NodeIconBadge`）。
//   接线 = `Shell/ItemDrawer.cs` 的 `Dispatch` 那一行：把 `IconAllianceBadgeFixed` 换成
//   `IconAllianceBadgeFixedNamed`（**同一物理行**，行数零变化，账的 A 落点不动）。
//
// ⚠️ **如实记**：这是「先照旧名字建、再改名」的一跳，**不是**把名字从创建处就写对。
//   之所以不直接改创建处：`AllianceBadgeArt.Draw(node, r, id, name, q)` 要吃名字，而调用它的
//   `IconAllianceBadgeFixedDraw` 在 W35 的文件里；等那份文件的下一个写手把 `NodeTrophyIcon` 换成
//   `NodeIconBadge`，本文件这一跳就可以整段删掉（届时 `B114_W38_Assert` 的 C 段仍会绿 —— 它断的是**结果名**）。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        // ============================================================ B114_W38 · B-282②：Icon 档徽章节点名 = `Badge`
        //   判据 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Alliance Badge Drawer Icon.prefab`
        //   的 `&224709867586078768`（GO 名 `Badge`）；复核 = `menu_dump.py bundle_menus_assets_all
        //   "Alliance Badge Drawer Icon" --depth 2 --relative`（根下两颗：`Frame` / `Badge`）。

        /// <summary>`Dispatch` 的 Icon 档分流（在 `B114_W35` 的 `IconAllianceBadgeFixed` 外面再包一层，只多做一件事：
        /// **把徽章那一颗的节点名改成原版的 `Badge`**）。
        /// <para>命中返回 `true`（调用方 `return`），其余档返回 `false`（照旧走 `IconVariant` / switch）。</para>
        /// <para>⚠️ 只改**名字**，不改几何、不改灰阶、不改 `res` 的任何一格 —— 那些全在 `IconAllianceBadgeFixedDraw` 里。</para></summary>
        static bool IconAllianceBadgeFixedNamed(string drawer, Transform node, PxRect box, ItemSpec item, int quantity,
                                                ItemDrawerStyle style, ref ItemDrawResult res)
        {
            if (!IconAllianceBadgeFixed(drawer, node, box, item, quantity, style, ref res)) return false;
            // `AllianceBadgeArt.Draw(node, …)` 是**直接挂在根下**的（prefab 现读也是根的直接子件）
            // ⇒ `Transform.Find` 找直接子件就够，⛔ 不用递归（递归反而会撞到将来的同名子件）。
            var badge = node != null ? node.Find(NodeTrophyIcon) : null;
            if (badge != null) badge.name = NodeIconBadge;
            return true;
        }
    }
}
