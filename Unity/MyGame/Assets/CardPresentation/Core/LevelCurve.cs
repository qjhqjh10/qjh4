// LevelCurve.cs（原文件名 VarsGlobal.cs，2026-10-27 执行代理 E14 改名）— 原版 `VarsGlobal` 的**等级 / XP / 段位**数学层（纯函数，无 MonoBehaviour）
// 🔴 2026-10-27（执行代理 `E14`）**类名 `VarsGlobal` → `LevelCurve`**：原版那个类确实叫 `VarsGlobal`，
//   但本仓**已有** `RuleEngine/Core/GameplayVariables.cs:596` 的 `public static class VarsGlobal`，
//   两个同名类型在 `Editor/BattleScene.cs` 里撞成 `CS0104`（`VarsGlobal` 不明确）⇒ 编辑器程序集整包挂。
//   本类当时**全仓 0 调用点** ⇒ 改名零风险；判据（下面那些 `VarsGlobal__*.c`）**一律保留原名**，⛔ 别跟着改。
//
// ============================ 判据（🥇 decomp_full 逐句） ============================
// · `VarsGlobal__GetNextLevelXP.c`            → `return level * xpPerLevel;`（字段 +0xd0）
// · `VarsGlobal__GetHeroLevel.c`              → level=1..maxLevel(+0xd4)：阈值从 `xpPerLevel` 起每级 **+xpPerLevel**
//                                               （等差）；`exp < 阈值` ⇒ 返回该 level；否则 `exp -= 阈值`；否则返回 maxLevel
// · `VarsGlobal__GetXPInLevel.c`              → 同一条等差循环，返回**本级内还剩的 XP**
// · `VarsGlobal__GetXPMissingToNextLevel.c`   → `iVar6 - param_2`，其中 iVar6 = **本级阈值**、param_2 = 本级内剩余
//                                               ⇒ 等价 = `阈值(本级) - XPInLevel(exp)`
// · `VarsGlobal__GetPlayerDivision.c`         → 段位阈值数组（+0xd8 → 计数 @+0x18、int[] @+0x20）**从高到低扫**，
//                                               第一个 `thresholds[i] <= rating` 的 i 即段位；扫不到 ⇒ 0
// · `VarsGlobal__ConvertTierToCheckPrize.c`   → `(段位计数 - tier) - 1`
// · `VarsGlobal__get_MaxDivision.c`           → `段位计数 - 1`
// · `VarsGlobal__CheckIfPlayerChangedDivision.c` → `Division(old) < Division(new)`
//                                               ⚠️ **只管「升段」**（降段返回 false）—— 照抄，⛔ 别按方法名「changed」改成 `!=`
//
// ============================ 数据从哪来（🔶 自己设计） ============================
// 原版这三组数（`xpPerLevel` / `maxLevel` / 段位阈值数组）**都在服务端配置里**（`VarsGlobal` 是
// `SingletonBehaviour` 的数据持有者，字段由远端灌），本机没有那份配置 ⇒ 这里做成**可注入的参数**，
// 默认值只作「没有配置时」的兜底：
//   · `XpPerLevel` 缺省 **100**、`MaxLevel` 缺省 **50**、段位阈值缺省 **空表**（空表 ⇒ `Division` 恒 0 + **出声一次**）。
//   🔶 自己设计（非原版）：等级/段位配置的缺省值 · 原版在服务端，本机四条取证全负 ·
//      调度台 2026-10-27 裁定（B88_J2 第四批）· 为什么必须自己定：判据为空（不是没找到）。
//      ⛔ 别把这三个默认值当原版数；远端真配置出现时**只改注入那一处**。
// ⚠️ 消费者：本仓目前**没有**玩家等级/段位的 UI 数据源（档案窗那一格是空态 `-`）⇒ 本类先落数学层，
//    消费点接上时直接调；⛔ 别在别处再写一份等差公式（两处写同一条规则 = 迟早不一致）。
using System;

namespace CardPresentation
{
    /// <summary>原版 `VarsGlobal` 的等级 / XP / 段位数学（判据见文件头）。⛔ 纯函数，不持有玩家状态。</summary>
    public static class LevelCurve
    {
        /// <summary>每级所需 XP 的**基数**（原版字段 +0xd0）。阈值是等差：第 n 级门槛 = n × 本值。</summary>
        public static int XpPerLevel = 100;
        /// <summary>等级上限（原版字段 +0xd4）。</summary>
        public static int MaxLevel = 50;

        /// <summary>段位阈值（**升序**：`[0]` = 最低段位的分数下限）。空表 = 没有配置。</summary>
        public static int[] DivisionThresholds = new int[0];

        static bool _warnedNoDivisions;

        /// <summary>自检 / 远端配置装载用。</summary>
        public static void Configure(int xpPerLevel, int maxLevel, int[] divisionThresholds)
        {
            XpPerLevel = xpPerLevel > 0 ? xpPerLevel : 100;
            MaxLevel = maxLevel > 0 ? maxLevel : 50;
            DivisionThresholds = divisionThresholds ?? new int[0];
            _warnedNoDivisions = false;
        }

        /// <summary>自检用：回到缺省（🔶 缺省值不是原版，见文件头）。</summary>
        public static void ResetForTest() { Configure(100, 50, null); }

        /// <summary>≡ 原版 `GetNextLevelXP(int currentLevel)`：`level * xpPerLevel`。</summary>
        public static int GetNextLevelXP(int currentLevel) { return currentLevel * XpPerLevel; }

        /// <summary>≡ 原版 `GetHeroLevel(int expPoints)`：等差阈值逐级减，返回 1..<see cref="MaxLevel"/>。</summary>
        public static int GetHeroLevel(int expPoints)
        {
            int level = 1;
            if (MaxLevel > 0)
            {
                int threshold = XpPerLevel;
                do
                {
                    if (expPoints < threshold) return level;
                    expPoints -= threshold;
                    level++;
                    threshold += XpPerLevel;
                } while (level <= MaxLevel);
            }
            return MaxLevel;
        }

        /// <summary>≡ 原版 `GetXPInLevel(int expPoints)`：**本级内**还剩多少 XP（同一条等差循环）。</summary>
        public static int GetXPInLevel(int expPoints)
        {
            int level = 1;
            if (MaxLevel > 0)
            {
                int threshold = XpPerLevel;
                do
                {
                    if (expPoints < threshold) return expPoints;
                    expPoints -= threshold;
                    level++;
                    threshold += XpPerLevel;
                } while (level <= MaxLevel);
            }
            return expPoints;
        }

        /// <summary>≡ 原版 `GetXPMissingToNextLevel(int expPoints)`：`阈值(本级) - XPInLevel(exp)`
        /// （判据 = 那份 `.c` 末句 `iVar6 - param_2`；iVar6 正是本级阈值）。</summary>
        public static int GetXPMissingToNextLevel(int expPoints)
        {
            int level = GetHeroLevel(expPoints);
            int threshold = level * XpPerLevel;
            return threshold - GetXPInLevel(expPoints);
        }

        /// <summary>≡ 原版 `GetPlayerDivision(long rating)`：阈值数组**从高到低**扫，
        /// 第一个 `thresholds[i] &lt;= rating` 的 i；扫不到 ⇒ 0。空表 ⇒ 0 + **出声一次**（⛔ 不静默假装「最低段」）。</summary>
        public static int GetPlayerDivision(long rating)
        {
            var t = DivisionThresholds;
            if (t == null || t.Length == 0)
            {
                if (!_warnedNoDivisions)
                {
                    _warnedNoDivisions = true;
                    UnityEngine.Debug.LogWarning("[LevelCurve] 段位阈值表为空（原版来自服务端配置，本机没有）"
                                               + " ⇒ `GetPlayerDivision` 恒 0；⛔ 这不是「原版也返回 0」");
                }
                return 0;
            }
            for (int i = t.Length - 1; i >= 0; i--)
                if (rating >= t[i]) return i;
            return 0;
        }

        /// <summary>≡ 原版 `MaxDivision`（属性）：`段位计数 - 1`。</summary>
        public static int MaxDivision
        {
            get { return DivisionThresholds != null && DivisionThresholds.Length > 0 ? DivisionThresholds.Length - 1 : 0; }
        }

        /// <summary>≡ 原版 `ConvertTierToCheckPrize(int tier)`：`(段位计数 - tier) - 1`。</summary>
        public static int ConvertTierToCheckPrize(int tier)
        {
            int n = DivisionThresholds != null ? DivisionThresholds.Length : 0;
            return (n - tier) - 1;
        }

        /// <summary>≡ 原版 `CheckIfPlayerChangedDivision(long oldRating, long newRating)`：
        /// `Division(old) &lt; Division(new)` —— ⚠️ **只管升段**（判据 = 那份 `.c` 的 `(int)uVar4 &lt; (int)uVar3`），
        /// 降段返回 **false**。照抄，⛔ 别「修」成 `!=`。</summary>
        public static bool CheckIfPlayerChangedDivision(long oldRating, long newRating)
        {
            return GetPlayerDivision(oldRating) < GetPlayerDivision(newRating);
        }

        /// <summary>≡ 原版 `GetProgressInLevel(int expPoints)`：**本级进度** = `XPInLevel / (等级 × 每级基数)`，
        /// 0..1 的浮点（等级天花板那一档照同一条循环算，不特殊处理）。
        /// <para>判据 = `decomp_full/VarsGlobal__GetProgressInLevel.c` **逐句**（2026-10-28 `A4` 现读，109 行）：
        /// `local_res8[0]` = 第一段等差循环剩下的**本级 XP**（= `GetXPInLevel`）·
        /// `local_18` = 第二段循环算出的**等级**（= `GetHeroLevel`）·
        /// `local_14[0] = local_18 * xpPerLevel`（= 本级阈值，.c:51）·
        /// `local_res20[0] = (float)local_res8[0] / (float)local_14[0]`（.c:52）⇒ 就是下面那两行。
        /// （ILSpy 的变量名是乱配的（`num9 = num5` 一类），以 `.c` 为准。）</para>
        /// <para>原版同一个方法里**还打一条 debug 串**（`CustomDebug.Log`，`VarsGlobal.cs:455-480` 五个字面量 =
        /// `"GetProgressInLevel, expPoints = "` + expPoints + `"; heroLevel = "` + 等级 + `", xpInLevel = "` +
        /// 本级 XP + `" / nextLvlXP = "` + 阈值 + `" = "` + 进度）。原版那句被 `CustomDebug` 的**全局 debug 开关**
        /// 闸住（`CustomDebug__Log.c:32-40` 读一个 `ObscuredBool`），本机没有那份配置 ⇒ 用
        /// <see cref="VerboseProgressLog"/> 代替（默认关）。</para>
        /// <para>⚠️ 原版**没有**除零保护：`maxLevel &lt;= 0` ⇒ `nextLvlXP == 0` ⇒ float 得 `Infinity`/`NaN`。
        /// 照抄，⛔ 别加 `if (nextLvlXP == 0)`（那会与原版行为不同，而这条今天 0 调用点、没人依赖）。</para></summary>
        public static float GetProgressInLevel(int expPoints)
        {
            int level = GetHeroLevel(expPoints);
            int xpInLevel = GetXPInLevel(expPoints);
            int nextLvlXP = level * XpPerLevel;
            float progress = (float)xpInLevel / (float)nextLvlXP;
            if (VerboseProgressLog)
            {
                UnityEngine.Debug.Log("GetProgressInLevel, expPoints = " + expPoints
                                      + "; heroLevel = " + level
                                      + ", xpInLevel = " + xpInLevel
                                      + " / nextLvlXP = " + nextLvlXP
                                      + " = " + progress);
            }
            return progress;
        }

        /// <summary>🔶 自己设计（非原版）：原版 `GetProgressInLevel` 里那条 debug 串被 `CustomDebug` 的**全局 debug 开关**
        /// （IL2CPP 里是一个 `ObscuredBool`，`CustomDebug__Log.c:32-40`；本机无那份配置/无对应静态量）闸住
        /// ⇒ 这里换成一个**默认关**的同形开关。调度台 2026-10-28 裁定（`B88_A4`）。⛔ 别把它当原版字段，也别默认开
        /// （这条方法原版与我们都是 **0 调用点**，默认开会变成未来接消费时的日志噪声）。</summary>
        public static bool VerboseProgressLog;
    }
}
