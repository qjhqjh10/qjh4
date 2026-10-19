// ArmyIcons.cs — **阵营徽记 / 阵营名的唯一一份运行期映射**（`CardArmy` → `40k_DeckSelection_icon_*`）
//
// ============================ 判据（原版运行期那条链，逐字节现读 2026-10-19） ============================
//
// ① 调用链（第一权威 = 反编译）：
//    · `ArmyUtilities.GetArmyIcon(CardArmy)`（`d:/2/tools/decomp_full/ArmyUtilities__GetArmyIcon.c:5-8`）
//      → `armyIcons.GetArmyIcon(army)`；`armyIcons` = `ArmyUtilities` 的序列化字段
//      （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/ArmyUtilities.cs`）。
//    · `ArmyIconsSO.GetArmyIcon`（`ArmyIconsSO__GetArmyIcon.c`）= 遍历 `+0x18` 那个
//      `List<ArmyItem<Sprite>>`（字段名 `armyIcons`，见 `ArmyIconsSO.cs`），逐条 `TryGetItem(army, out sprite)`；
//      **表里没有 `Neutral(0)`** ⇒ 0 / 认不出的阵营**返回 null**（⛔ 别拿默认图顶）。
//    · 阵营名：`ArmyUtilities.GetLocalizedArmyName(army, useShort)`（`ArmyUtilities__GetLocalizedArmyName.c`）
//      = `I2.GetTermTranslation(string.Format(<前缀> + Enum.ToString() [+ 后缀]))` ⇒ 键 = `Armies/<枚举名>`；
//      本仓那 14 条键在 `Core/Loc.cs` 的手写表里（`CardText.Faction(faction)` 是它的唯一读口）。
//
// ② **那个 SO 不在 `assets_full` 的 JSON 导出里**（全树 `grep armyIcons` 只命中一个同名 **GameObject**；
//    `grep IconsByArmySO` / `grep ArmyIconsSO` 在 `sharedassets0.assets` 里 0 命中）——
//    ⚠️ **这就是 U4 `_报告.md` §五·2 没找到它的原因：资产名 ≠ 类名**。
//    照 `工具/read_default_cardbacks.py` 那套「原始字节手工切」现读，链路逐环：
//      a) `d:/2/unity_run_ref/Warpforge_Data/level0` 的 `ArmyUtilities` MonoBehaviour（pathID **391**，
//         `m_Script` = `{fileID 1, pathID 3281}`）尾部 6 个 12 字节 PPtr（首字段偏移 32，`m_Name` 为空串）：
//           0x20 `defaultCardBacks` → `{2, 420}` · **0x2c `armyIcons` → `{2, 422}`** ·
//           0x38 `armyBackgrounds` → `{2, 417}`（= U4 那份 `ArmyBackgroundsSO`）·
//           0x44 `forgeIcons` → `{2, 421}` · 0x50 `cursors` → `{2, 419}` · 0x5c `activeAbilityIcons` → `{2, 416}`。
//         （`m_FileID = 2` = `level0` 的 externals 第 2 项 = `sharedassets0.assets`。）
//      b) `sharedassets0.assets` 对象 **422**：`m_Script = {1, 660}`（`globalgamemanagers.assets` 里
//         MonoScript 660 的类名 = **`ArmyIconsSO`**）· **`m_Name = "IconsByArmySO"`** · 总长 260 B。
//         首字段偏移 48（`m_Name` 13 字符、对齐后）→ `[int32 count = 13]` +
//         13 × `[int32 army][PPtr{m_FileID:i32, m_PathID:i64}]`，**尾部 0 字节**（逐字节核过）。
//         13 个 PPtr 全部 `m_FileID = 0`（同文件）⇒ 按 pathID 读到的 13 个对象名就是下面那张表。
//      c) 交叉印证：`bundle_armyicons_assets_all`（`SpriteAtlas` 名 `Army Icons`）里正好 **13 个**
//         `40k_DeckSelection_icon_*` sprite，**名字与这条链解出来的一字不差**（另 4 个 `*_icon_ranked*` /
//         `gamemode_icon_*` 不在 SO 里）。`m_Rect` = **256×256**（`Sprite/*.json` 实读）。
//
// 🔴 **名字不规则**（⛔ 别拼前缀）：`Genestealers` 与 `SpaceWolves` **不带 `Faction`**；
//    `Goff` → `FactionOrks`（阵营枚举名 ≠ 显示名）。⇒ **必须查表**。
//
// ③ 我们的图：13 张**全部已导入** `Resources/Art/ui_deck/40k_DeckSelection_icon_*.png`（2026-10-19 现数 13/13），
//    `CardArt.MenuUi` 自带 `ui_menu/ → ui_deck/ → ui/` 三级兜底 ⇒ 运行期直接取得到。
//
// ⚠️ 本表**只覆盖 `ArmyUtilities.GetArmyIcon` 这一族**（= `armyIcons` 字段）。
//    同一个 `ArmyIconsSO` 类还被另两个字段复用：`forgeIcons`（421 = `IconsByArmySO` 同族，
//    值是 `40K_general_icon_Forge points_*`）与 `activeAbilityIcons`（416，**只有 5 条**）——
//    它们**不是**这一族，⛔ 别把本表拿去当那两族用。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>阵营徽记 + 阵营名。**全工程唯一一份 `CardArmy` ↔ `40k_DeckSelection_icon_*` 映射。**
    /// <para>判据链（原版 `ArmyUtilities.GetArmyIcon` → `ArmyIconsSO` 的原始字节实读）→ 本文件头。</para></summary>
    public static class ArmyIcons
    {
        /// <summary>`CardArmy.Neutral` —— 原版 `ArmyIconsSO` **没有**这一条 ⇒ 取图返回 `null`。</summary>
        public const int Neutral = 0;

        public struct Entry
        {
            /// <summary>`CardArmy` 枚举值（`dump.cs:45637-45650`：10…130）。</summary>
            public int Army;
            /// <summary>`CardArmy` 枚举名（= `Armies/<名字>` 的键尾，也是我们 `CardDef.Faction` 的取值）。</summary>
            public string Faction;
            /// <summary>原版 sprite 名（`IconsByArmySO` 逐条解出）。</summary>
            public string Sprite;
        }

        /// <summary>13 条，**顺序与值逐字照 `IconsByArmySO` 的序列化顺序**（army 10…130）。</summary>
        static readonly Entry[] Table =
        {
            new Entry { Army =  10, Faction = "Ultramarines",     Sprite = "40k_DeckSelection_icon_FactionUM" },
            new Entry { Army =  20, Faction = "Goff",             Sprite = "40k_DeckSelection_icon_FactionOrks" },
            new Entry { Army =  30, Faction = "SaimHann",         Sprite = "40k_DeckSelection_icon_FactionSaimHann" },
            new Entry { Army =  40, Faction = "Sautekh",          Sprite = "40k_DeckSelection_icon_FactionSautekh" },
            new Entry { Army =  50, Faction = "BlackLegion",      Sprite = "40k_DeckSelection_icon_FactionBlackLegion" },
            new Entry { Army =  60, Faction = "Leviathan",        Sprite = "40k_DeckSelection_icon_FactionLeviathan" },
            new Entry { Army =  70, Faction = "TauEmpire",        Sprite = "40k_DeckSelection_icon_FactionTauEmpire" },
            new Entry { Army =  80, Faction = "Sororitas",        Sprite = "40k_DeckSelection_icon_FactionSororitas" },
            new Entry { Army =  90, Faction = "Genestealers",     Sprite = "40k_DeckSelection_icon_Genestealers" },
            new Entry { Army = 100, Faction = "AstraMilitarum",   Sprite = "40k_DeckSelection_icon_FactionAstraMilitarum" },
            new Entry { Army = 110, Faction = "DarkAngels",       Sprite = "40k_DeckSelection_icon_FactionDarkAngels" },
            new Entry { Army = 120, Faction = "EmperorsChildren", Sprite = "40k_DeckSelection_icon_FactionEmperorsChildren" },
            new Entry { Army = 130, Faction = "SpaceWolves",      Sprite = "40k_DeckSelection_icon_SpaceWolves" },
        };

        /// <summary>13 条（自检读口）。</summary>
        public static Entry[] All { get { return (Entry[])Table.Clone(); } }

        /// <summary>`CardArmy` 值 → 那一条；`0` / 认不出 ⇒ `false`（原版 `TryGetItem` 的语义）。
        /// ⚠️ `out` 在 `false` 时是 `default`，⛔ 别拿它的字段当「查到了」。</summary>
        public static bool TryGet(int army, out Entry e)
        {
            for (int i = 0; i < Table.Length; i++)
                if (Table[i].Army == army) { e = Table[i]; return true; }
            e = default(Entry);
            return false;
        }

        /// <summary>`CardArmy` 值 → sprite 名；`0` / 认不出 ⇒ **`null`**（= 原版那次返回 null）。
        /// ⛔ 别改成「兜底返回某个阵营的图」—— 那会把「这一格不该有图」画成别人的徽记。</summary>
        public static string IconName(int army)
        {
            Entry e;
            return TryGet(army, out e) ? e.Sprite : null;
        }

        /// <summary>阵营名（`CardDef.Faction` / `CardArmy` 枚举名）→ sprite 名；认不出 ⇒ `null`。</summary>
        public static string IconName(string faction)
        {
            Entry e;
            return TryGet(faction, out e) ? e.Sprite : null;
        }

        /// <summary>`CardArmy` 值 → 阵营名；认不出 ⇒ `null`。</summary>
        public static string Faction(int army)
        {
            Entry e;
            return TryGet(army, out e) ? e.Faction : null;
        }

        /// <summary>阵营名 → `CardArmy` 值；认不出 ⇒ `-1`（⚠️ **不是 0** —— 0 是 `Neutral`，是个合法值）。</summary>
        public static int Id(string faction)
        {
            Entry e;
            return TryGet(faction, out e) ? e.Army : -1;
        }

        static bool TryGet(string faction, out Entry e)
        {
            if (!string.IsNullOrEmpty(faction))
                for (int i = 0; i < Table.Length; i++)
                    if (Table[i].Faction == faction) { e = Table[i]; return true; }
            e = default(Entry);
            return false;
        }

        /// <summary>`CardArmy` 值 → **本地化阵营名**（原版 `ArmyUtilities.GetLocalizedArmyName`）。
        /// 认不出 ⇒ `null`（`Neutral` 也 `null` —— 原版 `Armies/` 里没有 Neutral 那条）。</summary>
        public static string LocalizedName(int army)
        {
            var f = Faction(army);
            return f == null ? null : CardText.Faction(f);
        }

        /// <summary>`CardArmy` 值 → 贴图（`CardArt.MenuUi` 三级兜底）；取不到 ⇒ `null`
        /// （取不到**不要静默**：调用方自己出声，同 `PurchasePremiumWindow.Tex` 那条纪律）。</summary>
        public static Texture2D Texture(int army)
        {
            var n = IconName(army);
            return string.IsNullOrEmpty(n) ? null : CardArt.MenuUi(n);
        }
    }
}
