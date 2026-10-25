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
        /// 认不出 ⇒ `null`（`Neutral` 也 `null` —— 原版 `Armies/` 里没有 Neutral 那条）。
        /// <para>🔴 **2026-10-25（`P6`）补齐短名支** —— 原版方法体（`cpp2il/ArmyUtilities.cs:99-124`）：
        /// `useShortName` 为真时**先**取 `Armies/&lt;枚举名&gt;_short`，**取不到（空）才回落** `Armies/&lt;枚举名&gt;`。
        /// 我们这一侧的键尾 = `CardDef.Faction`（= `CardArmy` 枚举名，见 <see cref="Entry.Faction"/>），
        /// ⇒ 短名键 = `Armies/&lt;Faction&gt;_short`。
        /// <para>⚠️ 官方全表里**只有 6 条 `_short`**（`Goff` / `Leviathan` / `Sautekh` / `Sororitas` /
        /// `Genestealers` / `AstraMilitarum`；现读 `Resources/official_loc_13lang.json` 的 `Armies/` 20 条里）
        /// ⇒ 其余 7 个阵营走「回落全名」那一支，与原版同（原版也是空则回落）。</para>
        /// <para>🔴 **可达性**（原版调用点逐个现读，2026-10-25）：全库 **5 个**调用点，
        /// **1 个真传 `true`** —— `RankedDeckSelector.ChangeDeck`（`cpp2il/RankedDeckSelector.cs:645`，
        /// 排位窗空态那句 `MainMenu/Ranked/NoDeckMessage` 的 `{1}`）；另 4 个不传
        /// （`AvatarDrawer.cs:19` · `CampaignHeaderDisplay.cs:124` · `DeckSelectionTabController.cs:449/605`）
        /// ⇒ **不是死代码**。</para></summary>
        public static string LocalizedName(int army, bool useShortName = false)
        {
            var f = Faction(army);
            return LocalizedName(f, useShortName);
        }

        /// <summary>阵营名（`CardDef.Faction` / `CardArmy` 枚举名）→ 本地化阵营名。**同一套短名判据**，
        /// 只是入参换成我们代码里更常见的字符串形状（<see cref="LocalizedName(int, bool)"/> 的那个入参口径）。
        /// 认不出（`null` / 空）⇒ `null`。</summary>
        public static string LocalizedName(string faction, bool useShortName = false)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            string full = CardText.Faction(faction);
            if (!useShortName) return full;
            // 先 `_short`、取不到回落全名：`CardText.TermOr` 的语义正是「表里有⇒取它；没有⇒出声+回 fallback」，
            // 而且它内部已按 13 语档 + 官方全表 + 中文字体闸取值（⛔ 别在这里另开一条取值路）。
            return CardText.TermOr(CardText.ArmyTermPrefix + faction + "_short", full);
        }

        /// <summary>🔴 **中立阵营** —— 原版 `GameStaticData.IsNeutralArmy(CardArmy)` 的等价实现。
        /// <para>原版判据（`cpp2il/GameStaticData.cs:981-984`）= `Enumerable.Contains(neutralArmies, army)`；
        /// 那个静态数组在 cctor 里建成长度 **1**、元素取默认值 0
        /// （`GameStaticData.cs:2444-2445`：`CardArmy[] array2 = new CardArmy[1]; neutralArmies = array2;`
        /// ⇒ 集合内容 = `{Neutral(0)}`）—— **⛔ 不是「army == 0」这个内联比较**：
        /// 原版留的是一个**集合**，以后加成员只改数据。</para>
        /// <para>我们照那个形状留成一张表（今天只有一项 = <see cref="Neutral"/>），
        /// 语义与内联 `army == 0` **逐值等价**（`neutralArmies` 长度 1、元素 0）。
        /// 调用点（我们这一侧的等价物）：`Shell/ProfileData.cs` 的成就 / 头衔按阵营筛、
        /// `Shell/DraftPacks.cs` 的卡包数据校验。</para></summary>
        public static bool IsNeutral(int army)
        {
            for (int i = 0; i < NeutralArmies.Length; i++)
                if (NeutralArmies[i] == army) return true;
            return false;
        }

        /// <summary>原版 `neutralArmies` 那个数组（**当前只有 `Neutral(0)`**，见 <see cref="IsNeutral"/>）。</summary>
        static readonly int[] NeutralArmies = { Neutral };

        /// <summary>`CardArmy` 值 → 贴图（`CardArt.MenuUi` 三级兜底）；取不到 ⇒ `null`
        /// （取不到**不要静默**：调用方自己出声，同 `PurchasePremiumWindow.Tex` 那条纪律）。</summary>
        public static Texture2D Texture(int army)
        {
            var n = IconName(army);
            return string.IsNullOrEmpty(n) ? null : CardArt.MenuUi(n);
        }

        // ══════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-24（`W10`）· **主动能力按钮图标**（原版 `ArmyUtilities.GetActiveAbilityIconForArmy`）
        //
        //  ============================ 判据链（逐字节现读，2026-10-24） ============================
        //  ① 反编译：`ArmyUtilities__GetActiveAbilityIconForArmy.c` = `ArmyIconsSO.GetArmyIcon(this+0x48, army)`
        //     —— **`+0x48` 就是 `activeAbilityIcons`**（`ArmyUtilities` 的序列化字段；本文件头 ② a) 那 6 个
        //     PPtr 里 `0x5c activeAbilityIcons → {2, 416}` ⇒ `sharedassets0.assets` 的路径 **416**）。
        //  ② 原始字节（`工具/read_default_cardbacks.py` 那套手工切，本次实读）：
        //     · `sharedassets0.assets` 对象 **416**：`m_Name = "ActiveAbilityIconsByArmySO"` ·
        //       `m_Script = {1, 660}`（= `ArmyIconsSO`，与本文件头同一个 MonoScript）· 总长 **144 B** ·
        //       首字段偏移 **60**（`m_Name` 26 字符对齐后）→ `[int32 count = 5]` + 5 ×
        //       `[int32 army][PPtr{m_FileID:0, m_PathID:i64}]`，**尾部 0 字节**。
        //     · 5 个 PPtr 全 `m_FileID = 0`（同文件）⇒ 按 pathID 读那 5 个 Sprite 的 `m_Name`：
        //         army  80 → pathID 138 → **`Attack type button Pray`**
        //         army 100 → pathID 123 → **`Attack type button Duty`**
        //         army 110 → pathID 125 → **`Attack type button Agenda`**
        //         army 130 → pathID 157 → **`Attack type button Ferocity`**
        //         army  10 → pathID 153 → **`Attack type button Oath`**
        //       （5 个 Sprite 的 `m_Rect` 都是 128×128；Oath 那张 `m_PixelsToUnits` 83.117、其余 50 ——
        //        与画法无关，我们照固定 rect 拉满，见 `AttackSelector.ButtonW/H`。）
        //  ③ 交叉印证：`ArmyUtilities__GetActiveAbilityIconForArmy` 的**消费点**是
        //     `UnitOnBoardAttackTypeSelector__Toggle.c:74-80`（拿 `cardData+0x2c` = 阵营值去查）⇒
        //     「技能钮的图标 = **按军队**查这一张表」，**不是**按关键词查。本表就是那个口径。
        //
        //  🔴 **只有 5 条**（全 13 阵营里只有这 5 个有「主动能力」图标）—— `TryGet` 认不出 ⇒ `null`，
        //     而原版拿到 `null` 时**整颗技能钮都不显示**（`AttackTypesButtonsSelector.c:54-56`：
        //     `bVar4 = (iconSprite != null)`，随后 `CardDisplayAttackTypeButton__Toggle(btn, bVar4 & param_2)`）
        //     ⇒ ⛔ 别给它兜底某一张通用图（那是我们自造的）。
        //
        //  ⚠️ 本族的 sprite 名与 `armyIcons` 那一族**不同名**（那几个是 `40k_DeckSelection_icon_*`），
        //     ⛔ 别把 `IconName()` 与 `ActiveAbilityIconName()` 混用。
        // ══════════════════════════════════════════════════════════════════════════════════

        public struct ActiveAbilityEntry
        {
            /// <summary>`CardArmy` 枚举值。</summary>
            public int Army;
            /// <summary>原版 Sprite 名（`ActiveAbilityIconsByArmySO` 逐条解出，**带空格**）。</summary>
            public string Sprite;
        }

        /// <summary>5 条，**顺序 = `ActiveAbilityIconsByArmySO` 的序列化顺序**（80 / 100 / 110 / 130 / 10）。</summary>
        static readonly ActiveAbilityEntry[] ActiveAbilityTable =
        {
            new ActiveAbilityEntry { Army =  80, Sprite = "Attack type button Pray"     },   // Sororitas
            new ActiveAbilityEntry { Army = 100, Sprite = "Attack type button Duty"     },   // AstraMilitarum
            new ActiveAbilityEntry { Army = 110, Sprite = "Attack type button Agenda"   },   // DarkAngels
            new ActiveAbilityEntry { Army = 130, Sprite = "Attack type button Ferocity" },   // SpaceWolves
            new ActiveAbilityEntry { Army =  10, Sprite = "Attack type button Oath"     },   // Ultramarines
        };

        /// <summary>5 条（自检读口）。</summary>
        public static ActiveAbilityEntry[] AllActiveAbility { get { return (ActiveAbilityEntry[])ActiveAbilityTable.Clone(); } }

        /// <summary>`CardArmy` 值 → 原版 sprite 名；认不出（含 `Neutral 0`）⇒ **`null`**
        /// （= 原版 `ArmyIconsSO.TryGetItem` 没命中，那时整颗技能钮不显示）。</summary>
        public static string ActiveAbilitySpriteName(int army)
        {
            for (int i = 0; i < ActiveAbilityTable.Length; i++)
                if (ActiveAbilityTable[i].Army == army) return ActiveAbilityTable[i].Sprite;
            return null;
        }

        /// <summary>阵营名（`CardDef.Faction`）→ 原版 sprite 名；认不出 ⇒ `null`。</summary>
        public static string ActiveAbilitySpriteName(string faction)
        {
            int a = Id(faction);
            return a < 0 ? null : ActiveAbilitySpriteName(a);
        }

        /// <summary>原版 sprite 名 → **我们 `Resources/Art/ui/` 下的文件名**（空格换下划线）。
        /// 五张都在（`Attack_type_button_{Pray,Duty,Agenda,Ferocity,Oath}.png`，2026-10-24 现数 5/5）。</summary>
        public static string ResourceNameOf(string spriteName)
        {
            return string.IsNullOrEmpty(spriteName) ? null : spriteName.Replace(' ', '_');
        }

        /// <summary>`CardArmy` 值 → **我们工程里的按钮图文件名**（`Resources/Art/ui/` 下的名字，
        /// 交给 `CardArt.Ui`）；认不出 ⇒ `null`（调用方必须判：原版那一档是「整颗钮不显示」）。</summary>
        public static string ActiveAbilityIconName(int army)
        {
            return ResourceNameOf(ActiveAbilitySpriteName(army));
        }

        /// <summary>阵营名 → 按钮图文件名；认不出 ⇒ `null`。</summary>
        public static string ActiveAbilityIconName(string faction)
        {
            return ResourceNameOf(ActiveAbilitySpriteName(faction));
        }

        /// <summary>`CardArmy` 值 → 贴图；认不出 ⇒ `null`（**不兜底**，同 <see cref="ActiveAbilityIconName(int)"/>）。</summary>
        public static Texture2D ActiveAbilityTexture(int army)
        {
            var n = ActiveAbilityIconName(army);
            return string.IsNullOrEmpty(n) ? null : CardArt.Ui(n);
        }
    }
}
