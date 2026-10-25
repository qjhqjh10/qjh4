// ScoringIcons.cs — 原版 `ScoringIconsSO.GetScoringIcon(ScoringItemType)` 的等价表（2 条）
//
// ============================ 判据（全部现读）============================
// ① 方法体：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ScoringIconsSO.cs:64` ——
//    在 `scoringIcons` 列表里找**第一条** `scoringItemType == 入参` 的，返回它的 `icon`（Sprite）；
//    找不到 ⇒ `FirstOrDefault` 的 null（⛔ 不是兜底第一张）。
// ② 表内容：`ScoringIconsSO.asset` 的两条 = `Skulls` → icon GUID `556a870c34f63ce4e908a75dd029b11c` ·
//    `Victories` → `d90d07c01286c8546b7e32d3c48a4749`（`iconName.mTerm` 分别是 `Ranked/Skulls` / `Ranked/Victories`）。
//    GUID → 资产名：`ExportedProject/Assets/Sprite/40k_battle_Win Skull.asset.meta` 与
//    `…/Sprite/Rank Skull.asset.meta` 的 `guid:` **逐字相等** ⇒ 两张图 = `40k_battle_Win_Skull` / `Rank_Skull`。
// ③ 枚举：`ScoringItemType { Skulls = 0, Victories = 5 }`（`cpp2il/ScoringItemType.cs`）。
// ④ 消费者：全 build 只有 `SinglePlayerOnlyEnergyEventWindow.GetScoringIconFromEvent`（`:407-455`）——
//    事件计分物是 `ScoringPoint` 时**写死** `GetScoringIcon(ScoringItemType.Skulls)`
//    ⇒ 能源窗只要画，画的就是 Skulls 那一张。
//    （`Victories` 那一档目前**无消费者**；表里照样保留 —— 原版有，⛔ 别删成一条。）
// ⑤ 我们的取图口：`CardArt.MenuUi` 自带 `ui_menu/ → ui_deck/ → ui/` 三级兜底 ⇒
//    `Rank_Skull`（在 `Resources/Art/ui_menu/`）与 `40k_battle_Win_Skull`（在 `Resources/Art/ui/`）都能取到。
//    缺图 ⇒ 返回 null，由调用方出声（本工程红线：不许静默）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ScoringIconsSO`（计分图标表）。只有 2 条，见文件头判据 ②。</summary>
    public static class ScoringIcons
    {
        /// <summary>原版 `ScoringItemType`（值照抄，⛔ 别重排）。</summary>
        public enum ItemType
        {
            Skulls = 0,
            Victories = 5,
        }

        /// <summary>`ScoringItemType` → 原版图名（`Resources/Art/` 下的文件名，⛔ 不带扩展名）。
        /// 认不出 ⇒ **null**（= 原版 `FirstOrDefault` 落空那一档，⛔ 别兜底第一张）。</summary>
        public static string IconName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Skulls: return "40k_battle_Win_Skull";
                case ItemType.Victories: return "Rank_Skull";
                default: return null;
            }
        }

        /// <summary>`ScoringItemType` → 图（= 原版 `GetScoringIcon(type).Icon` 的等价物）。
        /// 认不出 / 缺图 ⇒ null，调用方负责出声。</summary>
        public static Texture2D Icon(ItemType type)
        {
            string name = IconName(type);
            return string.IsNullOrEmpty(name) ? null : CardArt.MenuUi(name);
        }
    }
}
