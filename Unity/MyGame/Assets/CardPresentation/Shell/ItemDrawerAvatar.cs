// ============================================================================ 头像抽屉（原版 `AvatarDrawer`）
//
//  判据（施工单 B1；方法体 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AvatarDrawer.cs`）——
//    `protected override void Draw(PlayerAvatar item, int quantity = 1)`
//      `{ Image image = base.image; image.sprite = item.Image;`
//      `  if ((bool)armyText) { string localizedArmyName = ArmyUtilities.GetLocalizedArmyName(item.cardArmy);`
//      `                        armyText.text = localizedArmyName; } }`
//  ⇒ 三件：① 主图 = `item.Image` ② 阵营名 = `GetLocalizedArmyName(cardArmy)`（`Armies/<枚举名>` 词条）——
//    🔴 **但那句只写 `.text`、`SetActive` 一次没调**，而 `Content/Army` 在 `Avatar Drawer` /
//    `Icon Avatar Drawer Variant` 两份 prefab 里都是 **`m_IsActive: 0`**（现读）⇒ **屏幕上不出现**（见下面 ② 那条）
//    ③ **quantity 槽是 null**（`Avatar Drawer` / `Avatar Drawer Shop Variant` 两份都 null，施工单 §三-2）
//    ⇒ 原版永不在这档画数量。
//
//  ── 版面（prefab 实读 · `Avatar Drawer`）──────────────────────────────────────
//    根 712.8×1080（ARF 0.66）· `Content` 712×1080 · `Content/Background` 铺满 ·
//    `Content/avatarBG` (139.8,233.3)-(571.0,712.3) 里挂 `avatar`（**860.21×730.48 过扫**，见 Cosmetic 文件头 ①）·
//    `Content/Army` (0.4,702)-(712.4,810) · `Content/Label/Name` (61,810)-(651.8,918)（**没有 Quantity 子件**）
//  ⇒ 与 `ItemDrawerLayout` 的公共尺一致，只有 `image` 那一格用 `avatarBG`。
using UnityEngine;

namespace CardPresentation
{
    public static partial class ItemDrawer
    {
        /// <summary>原版 `AvatarDrawer : ItemDrawer&lt;PlayerAvatar&gt;`。</summary>
        public const string DrawerAvatar = "AvatarDrawer";

        /// <summary>原版 `AvatarDrawer.Draw`。
        /// <para>⚠️ `avatarBG` 那件（原版 `Player Profile Border`）是 **prefab 自带的第二层**、`Draw` 不碰它
        /// ⇒ 我们不建（施工单 B1「还差什么」②）。⚠️ 头像本体 `avatar` 是 **860.21×730.48 的过扫节点**
        /// （anchors `-0.475..1.52 × -0.175..1.35`、整个 prefab 无 mask）⇒ **按它自己的矩形画**
        /// （`AvatarImageFrame`，2026-10-30 `B101_R8` 订正：原来画在 `avatarBG` 那一格里 = **小 2 倍**）。</para></summary>
        static void Avatar(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st,
                           ref ItemDrawResult res)
        {
            var fr = FrameOf(box, fit: RootFit.HeightControlsWidth);   // ARF `m_AspectMode 2`（0.66）：框更窄时原版**横向溢出**（B-260）
            var content = DrawerContent(node, fr);
            DrawerBackground(content, fr, st);

            // ① `image.sprite = item.Image` —— 头像图在 `Resources/Art/avatars/`（`CardArt.Cosmetics`，
            //    ⚠️ 那一批**名字含空格、原样保留**，⛔ 别 slug 化，见 `CardArt.Cosmetics` 的 doc）
            var tex = CardArt.Cosmetics(item.Art);
            if (!DrawerImage(content, AvatarImageFrame(fr), tex, item.Art, st, ref res))
                Note("avatar|art|" + (item.Id ?? "?"),
                     "头像 `" + (item.Id ?? "?") + "` 的图名 `" + (item.Art ?? "<空>") + "` 取不到"
                     + "（`CardArt.Cosmetics` 的 `Resources/Art/avatars/`）⇒ 这一层**不画**（⛔ 不拿别的图顶替）");

            // ② 阵营名（`B-242`）：原版 `AvatarDrawer.Draw` 那三句**照做**（写文本），
            //    ⛔ 但**建完立刻 `SetActive(false)`** —— 原版 `Draw` 里 `SetActive` **一次都没调**，
            //    而 `Content/Army` 这颗 GameObject 在 prefab 里就是 **`m_IsActive: 0`**
            //    ⇒ 文本写进去了、屏幕上**不出现**阵营名。
            //    判据 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Avatar Drawer.prefab` 的
            //    `!u!1 &1618682377417806`（`m_Name: Army` … `m_IsActive: 0`）与
            //    `Icon Avatar Drawer Variant.prefab` 的 `!u!1 &1724804207875695`（同样 0），两份逐字现读。
            //    （`Neutral` 时那串本来就是空串 ⇒ 原版也不写，两件事不冲突。）
            //    ⚠️ **`Avatar Drawer Shop Variant` 是例外**：那份 `Army` 是 `m_IsActive: 1`（现读）⇒ 原版在
            //    商店那一档**会**显示阵营名；照做需按 `DrawerOverride.Shop` 分流，而 `Avatar(...)` 现在拿不到
            //    `ov`（调用点 = `ItemDrawer.cs:1925` 那条聚合行，本波不归我们改）⇒ **如实记、未接**（见本波报告）。
            DrawerArmyName(content, fr, item.CardArmy, st);
            var armyNode = content.Find(NodeArmy);      // `DrawerArmyName` 建的正是 `Content/Army`（直接子件）
            if (armyNode != null) armyNode.gameObject.SetActive(false);

            // ③ `label` 由基类写（`GetItemName`）；`quantity` 槽 null ⇒ 不画数量
            DrawerNameQuantity(content, fr, item, qty, st, hasName: true, hasQuantity: false);
        }
    }
}
