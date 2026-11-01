// ItemDrawerPartial_B114W24.cs — 第一百一十四会话 · 写手 `W-24`（2026-11-01）· 账 `B-256`：`Shop` 档头像边框变体
//
// 【这一条是什么】`CosmeticItemAvatarBorder` 在 `DrawerOverride.Shop`(20) 那一档的抽屉是
//   **`Avatar Border Drawer Shop Variant`** —— 原版是「**同一个类 `AvatarBorderDrawer` + 两套 prefab**」
//   （`ItemTypeSets`（`Shell/ItemDrawer.cs:421`）里它的 **Shop 列仍写 `AvatarBorderDrawer`**，
//    与 `B-249` 的 `PlayerAvatar` 是同形兄弟）。我们的 26 个 `case` 原先只有 `GenericArmy` 把 `ov` 往下传
//   ⇒ 这一档一直按基础档画，**多画了一张底板**。
//
// 【判据（现行 build 现读 · 逐字段，⛔ 不靠名字猜）】
//   ① 真包：`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Avatar Border Drawer Shop Variant"
//      --depth 4 --relative --no-sprite --no-ancestor-scale`（出处 `d:/2/新解包资源/assets_full/bundle_menus_assets_all`）：
//        · 根 `AspectRatioFitter` 高控宽 0.66 ⇒ 712.8×1080（与基础档同）
//        · `Content` 712×1080（与基础档同）
//        · **`Background` 行的 `act` = `INACT`（`m_IsActive = 0`）**，它那颗 `Image` 是
//          `m_Enabled=0` + `Simple (1,1,1,0)`；基础档那两处是 `1` 和 `(1,1,1,1)`
//        · `avatarBG` (139.8,233.3)-(571.0,712.3) · `avatar` (-65.0,65.6)-(795.2,796.1) ·
//          `Army` (-34.3,186.1)-(265.7,286.1)（`INACT`）· `Label` (36.0,810.0)-(676.8,918.0) ·
//          `Label/Name` (61.0,810.0)-(651.8,918.0)  —— **五格与基础档逐值相同**
//   ② 交叉源：`diff "d:/2/tools/assetripper_out/ExportedProject/Assets/Avatar Border Drawer.prefab"
//      "…/Avatar Border Drawer Shop Variant.prefab"` —— **整份只有 3 行不同**：
//        `m_Name` · `Background` 的 `m_IsActive: 1 → 0` · `Background` 那颗 `Image` 的 `m_Enabled: 1 → 0`
//        与 `m_Color.a: 1 → 0`。
//      ⇒ **唯一的行为差异 = 不画底板**；几何/字号/墨色**一处都不用改**（与 `B-249` 那档相反 ——
//        那一档 `Army` 还翻了显隐、字号从 75 变 90）。
//   ③ 判据链（「只看 prefab 字段会漏掉『谁会把它打开』这一步」，所以两头都读了）：
//        `ItemDrawer<T>.Draw`（`d:/2/tools/decomp_generic_trial/out/ItemDrawer_object___Draw.c`，全文 175 行）
//        只对 `quantity`(＋0x38) 与 `label`(＋0x30) 两格调过 `GameObject.SetActive`，**`background` 那格一次没碰**；
//        `AvatarBorderDrawer.Draw`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AvatarBorderDrawer.cs:15-24`）更是**全篇一句**
//        （`base.image.sprite = item.Image;`）⇒ 序列化态说了算 ⇒ `Shop` 档屏幕上**没有**底板。
//      ⚠️ 那颗 `Image` 的 `m_Enabled=0` / `m_Color.a=0` 与「整颗 GO 关着」**叠加同义**（`B114_W16` 报告 §八·4
//        对 `Avatar Drawer Shop Variant` 记过同一条）⇒ 按「不建」处理即可，⛔ 不要为它再建一个透明的节点。
//
// 【为什么单开这一节、而不是铺在 `ItemDrawerCosmetic.cs` 里】施工方式与 `B114_WC`（Icon 档）/ `B114_W16`（Shop Avatar）
//   同一套：`Dispatch` 的第一行 `{` 里**同物理行**加一句 `if (ov == Shop && ShopAvatarBorderVariant(...)) return;`
//   （零行数变化，账的 A 落点不动）；分流与实现在本文件。基础档的 `AvatarBorder` 只多一个
//   `bool drawBackground = true` 形参（默认值 ⇒ 原调用点逐字不变）。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        // ============================================================ B114_W24 · B-256：`Shop` 档头像边框（2026-11-01）
        //
        //  几何/字号/墨色**一处都不覆盖** —— 这一档与基础档**逐值相同**（判据见文件头 ①/②），
        //  唯一的差别是「不画 `Content/Background`」⇒ 实现只需要一个开关，不另抄一份几何。
        /// <summary>`Dispatch` 的 `Shop` 档分流（**只认 `DrawerAvatarBorder`**）：命中返回 `true`（调用方 `return`），
        /// 否则 `false`（照旧走 `switch`）。
        /// <para>⚠️ `Horizontal`(15) / `OfferPopups`(30) 两档**不接**：`ItemTypeSets:421` 里本族这两列都是 `null`
        /// （= 照原版「按键找不到 ⇒ 回落主档」），真包也没有对应的 `Avatar Border Drawer` 变体 prefab
        /// （`bundle_menus_assets_all` 现读只有 `Avatar Border Drawer` / `Avatar Border Drawer Shop Variant` 两份）。
        /// ⛔ 别顺手接上 —— 那会让我们画出原版没有的变体。</para></summary>
        static bool ShopAvatarBorderVariant(string drawer, Transform node, PxRect box, ItemSpec item, int quantity,
                                            ItemDrawerStyle style, ref ItemDrawResult res)
        {
            switch (drawer)
            {
                // `drawBackground: false` —— 本档 prefab 的 `Content/Background` 是 `m_IsActive: 0`
                // （基础档是 1）；其余走 `AvatarBorder` 的同一条实现（几何逐值相同，⛔ 别再抄一遍）。
                case DrawerAvatarBorder:
                    AvatarBorder(node, box, item, quantity, style, ref res, drawBackground: false);
                    return true;
                default: return false;
            }
        }
    }
}
