// RarityColors.cs — 「稀有度 → 颜色」的**唯一一份**表（2026-10-18 · `N102` 收口）
//
// ============================ 它服务的是哪一件事 ============================
// **卡牌行**（原版 `UICardInfoItem`）上 `imagesToChangeColorByRarity` 那两颗 ——
// `Rarity Gradient`（行右侧色条）与 `Background Border`（行边框）—— 的染色。
// 卡组编辑窗左栏（`Deck/DeckRuntime.cs`）与 `Deck info Popup`（`Shell/DeckInfoPopup.cs`）
// 画的都是这一种行 ⇒ **两边转调本类**，⛔ 别再各抄一份表。
//
// ============================ 判据链（每步都能复查） ============================
// ① **谁上色**：`d:/2/tools/decomp_full/UICardInfoItem__Initialize.c:139-156` ——
//    `uVar6 = PlayerItem__get_Rarity(card, 0)`（= `ObtainableItem.cardRarity`，
//    经 `SupportMethods.GetCardRarityFromInt` 转成 `CardRarity` 枚举）→
//    `CardRarityColorsSO__GetColor(colorsByRarity, uVar6)` → 循环
//    `(**(code**)(*plVar1 + 0x2a8))(plVar1, color)`（vtable `+0x2a8` = `Image.set_color`）。
// ② **哪两颗**：`tools/_probe_deckinfo.py bundle_menus_assets_all "Deck Selector Card Info button"
//    --class UICardInfoItem --pid 2994867481076140456`（= `Deck info Popup ▸ Info Panel ▸ Deck List ▸ Content`
//    那一颗，⛔ 不是同名的另外几颗）现读 =
//    **`imagesToChangeColorByRarity` = [`Rarity Gradient`, `Background Border`]**
//    ⇒ **`Background`（行底）不在名单里** ⇒ 它保持 `m_Color = (1,1,1,1)` 白。
// ③ **哪张表**：同一颗 prefab 的 `colorsByRarity` = `m_FileID 0 / m_PathID -2272266055686004801`，
//    在真包 `menus_assets_all.bundle` 里按 pid 反查出 **`m_Name = CardUIDisplayColorsSO`**
//    ⇒ 数值 = `bundle_menus_assets_all/MonoBehaviour/CardUIDisplayColorsSO.json` 的 `colors[]`
//    六档逐条现读（下面 `BySo`）。
// ④ **档号 ↔ 名字**：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardRarity.cs` =
//    `None 0 · Common 1 · Rare 2 · Epic 3 · Legendary 4 · Special 5`。
//
// 🔴 **`N102` 收口的现核证据（2026-10-18）**：`bundle_menus_assets_all` 里
//    **全部 15 颗** `UICardInfoItem` 实例（`Deck Editing Menu / … / Deck List drawer / Content` 的
//    卡行 / 督军行 / 防御卡槽 + `Deck info Popup` + `Draft Mode` 两扇 + `Practice Mode Menu` +
//    `RankedEventWindow` + `Card in deck container` 那几个独立 prefab）的 `colorsByRarity`
//    **逐颗都是 pid `-2272266055686004801`（= `CardUIDisplayColorsSO`）**，
//    **没有一颗**指向 `CardRarityColorsSO`（pid `764426219195092032`）。
//
// ⚠️ **同一包里另一份同类 SO `CardRarityColorsSO` 是【另一个用途】，⛔ 别合进来**：
//    它的消费侧是 **`CardInBoosterPack`**（字段名 `cardRarityColors`，不是 `colorsByRarity`），
//    实例逐颗在 `Booster Pack Open Window / Booster Animation Parent / Cards / CardInBoosterPack UI 1..5`
//    （同一次现核：pid `764426219195092032` 在本包只被这 5 份 MonoBehaviour 引用）。
//    那份表 `Common` 的 **α = 0**、`Legendary` 的 **r = 1.28**，与行色完全不同 —— 别拿它顶替。
//
// ============================ 两个如实记的保留项 ============================
// ⚠️ **色值的量纲（gamma vs linear，未决）**：本工程 `ImageQuad.SetTint` 走 `material.color`，
//    实测**给什么数就显示什么数**（`0.934` 的卡名实拍最大像素 = `0xEE` = 238 ⇒ 端到端按 **gamma/sRGB**）。
//    兄弟 SO `CardRarityColorsSO` 的数值**恰好是 n/255**（`0.5647059 = 144/255`）⇒ 那份坐实 gamma；
//    本表数值**不是** n/255，另一种读法是「Linear 工程里存的 linear 值」——
//    按 linear→sRGB 折过去，五个色相与游戏自带的 `Resources/Art/ui_deck/{1..5}_40k_cardframe_rarity_*.png`
//    军衔图**对得上**（Epic 的 R：173 vs 173、Legendary 的 R：231 vs 236）。
//    **按 gamma 落**（与兄弟 SO、与本工程既有口径一致），linear 读法记在这里备查。
// ⚠️ 认不出的档（空串 / null）落 **`None`（档 0）**：原版那条 fallback 是 `_DAT_1834b2e50`
//    那 4 个字节（**没解出来**），只能如实按「表里第 0 档」处理。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `CardUIDisplayColorsSO` 的六档色 —— **逐条从资产里现读**，⛔ 没有一处是挑的。
    /// 判据链与「为什么不是 `CardRarityColorsSO`」→ 本文件头注。</summary>
    public static class RarityColors
    {
        static readonly Color[] BySo =
        {
            /* 0 None      */ new Color(0.509433985f, 0.509433985f, 0.509433985f, 1f),
            /* 1 Common    */ new Color(0.158419371f, 0.281238854f, 0.377358496f, 1f),
            /* 2 Rare      */ new Color(0.188660726f, 0.349056602f, 0.074092217f, 1f),
            /* 3 Epic      */ new Color(0.415094316f, 0.178177282f, 0.395884871f, 1f),
            /* 4 Legendary */ new Color(0.792452812f, 0.438179821f, 0f, 1f),
            /* 5 Special   */ new Color(0.622641504f, 0.072625607f, 0.055802763f, 0.639215708f),
        };

        /// <summary>卡面 `rarity` 串 → 原版 `CardRarity` 档号（逐条对着 `CardRarity.cs` 抄，⛔ 别按字母排）。
        /// 数据侧取值 = `cards_engine.json` 的 `rarity`（common/rare/epic/legendary/special）。</summary>
        public static int Tier(string rarity)
        {
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "common": return 1;
                case "rare": return 2;
                case "epic": return 3;
                case "legendary": return 4;
                case "special": return 5;
                default: return 0;          // `None`（含空串）—— 见本文件头注的 fallback 那条
            }
        }

        /// <summary>行上那两颗（`Rarity Gradient` + `Background Border`）该上的色
        /// = 原版 `CardRarityColorsSO.GetColor(card.cardRarity)`（那个方法只是个按档取值的访问器，
        /// 取的是**传进去那颗 SO** —— 本 prefab 传的是 `CardUIDisplayColorsSO`，见本文件头注）。
        /// ⚠️ **`Background`（行底）不在这条路上**。</summary>
        public static Color Tint(string rarity)
        {
            return BySo[Tier(rarity)];
        }
    }
}
