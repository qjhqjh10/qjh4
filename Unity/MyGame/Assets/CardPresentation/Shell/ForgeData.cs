// ForgeData.cs — 锻造厂页的**本地数据源**（单机版）
//
// ============================ 这是**我们自建**的，不是原版的做法 ============================
// 🔴 原版这一整套是**服务端**的：反编译实证 `ForgeWindowTab.SelectArmy` 取的是
//    `LiveOpsManager.GetHandler<ForgeHandler>()` 里的 Forge 存档，`currentPoints` / `rewardsCollected` /
//    每级所需 `Points` / 奖励物品 `RewardInfo` **全在远端**；`Debug Set Forge` 走的是
//    PlayFab 云脚本 `"SetForgeLevel"`。原版已关服 ⇒ **本地一条都取不到**。
//    ⇒ 照「日常」那一层的先例（`Shell/DailyData.cs`）：**数据由我们自建，并在这里逐条标明「我们挑的」**。
//
// ✅ **照原版的**：底层的**状态机语义** —— 四态 `Locked(0) / InProgress(1) / ToCollect(2) / Collected(4)`
//    （枚举出处 `d:/2/tools/il2cpp_out/dump.cs:68148-68157`，判定公式出处
//    `d:/2/tools/decomp_full/ForgeRewardItemContainer__Initialize.c:57-75` 与 `__GetLevelState.c:6-15`）：
//      · `rewardsCollected == index`（这格是下一个该领的）且 `currentPoints >= 本格 Points` → **ToCollect**
//      · `currentPoints < 本格 Points` → **InProgress**
//      · 前一格还没达标 → **Locked**
//      · `index < rewardsCollected` → **Collected**
//    ⇒ **经验是累计的、领取只把 `rewardsCollected` 加一**（不扣经验）—— 这条照原版。
//    · 升级文案 `"{0} {1}/{2}"` + 本地化键 `"MainMenu/Level"`（出处 `ForgeWindowTab__RefreshLevel.c:40-50`，
//      三个字面量地址已解析）⇒ 显示成 `Level 3/50`。
//
// ❌ **我们挑的**：阵营列表的初值、每级所需经验的曲线、每级奖励的图标与数量、起始等级与经验。
//
// ⏭ **接线点**：以后若做 P2P / 存档，这里是唯一要换掉的地方（调用方只认下面这几个静态入口）。
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>单机版的「锻造厂」状态。**纯内存 + 可复现**（不用 `UnityEngine.Random`）。</summary>
    public static class ForgeData
    {
        /// <summary>轨道格数（= `LevelText` 的分母）。⚠️ **我们挑的 50** ——
        /// 唯一的依据是 prefab 里那句占位文案 `Level 1/50`（`l3_rect_full.txt:229` 那一格，
        /// 原版真实格数在服务端）。**别把它当成原版数字。**</summary>
        public const int MaxLevel = 50;

        /// <summary>13 个阵营。**顺序照卡池实测**（`Assets/RuleEngine/Resources/cards_engine.json` 里 faction 的出现频次序）。
        /// ⚠️ 图标**不在这里写死** —— 走 `DeckRuntime.FactionIcon`（那条映射是全工程唯一一份，别抄第二份）。</summary>
        public static readonly string[] Armies =
        {
            "Ultramarines", "Goff", "SaimHann", "BlackLegion", "DarkAngels", "Genestealers", "TauEmpire",
            "Sautekh", "AstraMilitarum", "Leviathan", "Sororitas", "EmperorsChildren", "SpaceWolves",
        };

        /// <summary>一格奖励。⚠️ **我们挑的**（原版 `RewardInfo` 在服务端）。</summary>
        public struct Reward
        {
            public string Art;      // `Resources/Art/ui_menu/` 里的图名
            public string Text;     // 显示在奖励下方的数量
        }

        // ⚠️ **我们挑的奖励表**：按「每 10 级一档、档内轮换」编，图标全用工程里已有的原版图。
        static readonly Reward[] Palette =
        {
            new Reward { Art = "40k_topmarquee_currency_gold",        Text = "150"  },   // 金币
            new Reward { Art = "40K_general_icon_Forge_points",       Text = "250"  },   // 锻造点
            new Reward { Art = "40k_Achievements_icon_seal_points",   Text = "20"   },   // 印章点
            new Reward { Art = "40K_missions_icon_Daily_skulls",      Text = "5"    },   // 骷髅
        };

        /// <summary>第 `level` 格（0 基）的奖励。⚠️ 我们挑的。</summary>
        public static Reward RewardAt(int level) { return Palette[((level % Palette.Length) + Palette.Length) % Palette.Length]; }

        /// <summary>第 `level` 格（0 基）**累计**需要的经验。⚠️ **我们挑的曲线**（原版曲线在服务端）：
        /// 每级 +100，每满 5 级再叠一个 20·n·(n/5) 的台阶 —— **单调递增**、好读。
        /// **累计**语义照原版（判据是 `currentPoints >= 本格 Points`，本格 Points 就是累计门槛 —— 见文件头）。</summary>
        public static int NeededAt(int level)
        {
            int n = level + 1;
            return 100 * n + 20 * n * (n / 5);
        }

        // ---- 每个阵营的进度。⚠️ **初值全是我们挑的**（原版在服务端存档里）。
        //      故意造出四种状态都出现的情况，好让四种格子都能在画面上验到：
        //        第 1 个：已满级（全 Collected）· 第 2 个：正好可领（ToCollect）
        //        第 3 个：差一点（InProgress）· 其余：按索引错开
        static readonly Dictionary<string, int> _level = new Dictionary<string, int>();
        static readonly Dictionary<string, int> _points = new Dictionary<string, int>();

        static ForgeData()
        {
            for (int i = 0; i < Armies.Length; i++) Init(Armies[i], i);
        }

        static void Init(string army, int i)
        {
            // ⚠️ 我们挑的初值。`NeededAt` 是累计门槛，所以「正好可领」= points 恰好等于 NeededAt(level)。
            switch (i)
            {
                case 0: _level[army] = MaxLevel; _points[army] = NeededAt(MaxLevel - 1); break;          // 满级
                case 1: _level[army] = 3;        _points[army] = NeededAt(3); break;                     // **正好可领**
                case 2: _level[army] = 3;        _points[army] = NeededAt(3) - 40; break;                // 差 40
                default: _level[army] = 1 + (i % 6); _points[army] = NeededAt(_level[army]) - 30 * (i % 5); break;
            }
        }

        /// <summary>当前选中的阵营（照原版：开窗时从战役/存档取，我们给个固定的起点）。</summary>
        public static string Selected { get; private set; } = Armies[1];

        public static void Select(string army)
        {
            if (string.IsNullOrEmpty(army)) return;
            foreach (var a in Armies) if (a == army) { Selected = a; return; }
        }

        /// <summary>已领格数（原版 `rewardsCollected`）。</summary>
        public static int LevelOf(string army) { int v; return _level.TryGetValue(army, out v) ? v : 0; }

        /// <summary>累计经验（原版 `currentPoints`）。</summary>
        public static int PointsOf(string army) { int v; return _points.TryGetValue(army, out v) ? v : 0; }

        /// <summary>第 `level` 格（0 基）的状态。**照原版 `ForgeRewardItemContainer.GetLevelState` 的公式**。</summary>
        public static int StateAt(string army, int level)
        {
            int collected = LevelOf(army), points = PointsOf(army);
            if (level < collected) return Collected;
            if (level == collected) return points >= NeededAt(level) ? ToCollect : InProgress;
            return Locked;
        }

        /// <summary>四态（**值照原版** `ForgeRewardItemContainer.LevelState`，见 `dump.cs:68148-68157`）。</summary>
        public const int Locked = 0, InProgress = 1, ToCollect = 2, Collected = 4;

        /// <summary>现在有没有「可领」的格子（原版 `ForgeRewardSelector.hasToCollectReward`）——
        /// **`Ready for level up` 那团光效的判据就是它**（出处 `ForgeRewardSelector__CreateRewards.c:83-87`
        /// + `__FinishedSnapping.c:5`）。</summary>
        public static bool HasToCollect(string army)
        {
            int lv = LevelOf(army);
            return lv < MaxLevel && PointsOf(army) >= NeededAt(lv);
        }

        /// <summary>领第 `level` 格。**照原版语义：只把 `rewardsCollected` 加一，不扣经验**。
        /// 返回「真的领到了吗」（领不到时**出声**，不静默）。</summary>
        public static bool Claim(string army, int level, out string why)
        {
            why = null;
            if (level < 0 || level >= MaxLevel) { why = "格号越界"; return false; }
            int st = StateAt(army, level);
            if (st == Collected) { why = "这一格已经领过了"; return false; }
            if (st == Locked) { why = "前面还有没领的格"; return false; }
            if (st == InProgress) { why = "经验还不够"; return false; }
            _level[army] = level + 1;
            return true;
        }

        /// <summary>升级文案。**照原版**：`"{0} {1}/{2}"` 套本地化键 `"MainMenu/Level"`（我们这里直接用英文词 `Level`，
        /// 原版那句是本地化词条）⇒ `Level 3/50`。</summary>
        public static string LevelText(string army) { return "Level " + LevelOf(army) + "/" + MaxLevel; }

        /// <summary>阵营 → **锻造点图标**（原版 `ForgePointIconDrawer.DrawForArmy` 换的那张）。
        /// 🔴 切片名里有**空格**，导入器把空格换成下划线 ⇒ 这里给的已经是**工程里的文件名**。
        /// ⚠️ 与 `DeckRuntime.FactionIcon`（**阵营徽记**，另一族图）**不是一回事** —— 那个是 `40k_DeckSelection_icon_*`。
        /// 两族图各自的映射**各留一份**，别互相抄。</summary>
        public static string ForgePointIcon(string army)
        {
            switch (army)
            {
                case "Ultramarines": return "40K_general_icon_Forge_points_Ultramarines";
                case "Goff": return "40K_general_icon_Forge_points_Goff";
                case "SaimHann": return "40K_general_icon_Forge_points_SaimHann";
                case "BlackLegion": return "40K_general_icon_Forge_points_BlackLegion";
                case "DarkAngels": return "40K_general_icon_Forge_points_Dark_Angels";
                case "Genestealers": return "40K_general_icon_Forge_points_Genestealers";
                case "TauEmpire": return "40K_general_icon_Forge_points_TauEmpire";
                case "Sautekh": return "40K_general_icon_Forge_points_Sautekh";
                case "AstraMilitarum": return "40K_general_icon_Forge_points_AstraMilitarum";
                case "Leviathan": return "40K_general_icon_Forge_points_Leviathan";
                case "Sororitas": return "40K_general_icon_Forge_points_Sororitas";
                case "EmperorsChildren": return "40K_general_icon_Forge_points_Emperors_Children";
                case "SpaceWolves": return "40K_general_icon_Forge_points_SpaceWolves";
                default: return "40K_general_icon_Forge_points";   // 不带阵营后缀的那张（兜底）
            }
        }

        /// <summary>一格的经验文案。**照原版** `"{currentPoints}/{neededPoints}"`（`ForgeRewardItemContainer__Initialize.c:200-202`）。</summary>
        public static string PointsText(string army, int level) { return PointsOf(army) + "/" + NeededAt(level); }

        /// <summary>格内进度条比例。**照原版公式**（`__Initialize.c:213-215`）：
        /// `(currentPoints − 上一格 Points) / (本格 Points − 上一格 Points)`，夹到 0..1。</summary>
        public static float Bar01(string army, int level)
        {
            float lo = level == 0 ? 0f : NeededAt(level - 1);
            float hi = NeededAt(level);
            if (hi - lo <= 0f) return 1f;
            float v = (PointsOf(army) - lo) / (hi - lo);
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }

        /// <summary>自检用：把状态推回初值（自检之间互不影响）。</summary>
        public static void ResetForTest()
        {
            _level.Clear(); _points.Clear();
            for (int i = 0; i < Armies.Length; i++) Init(Armies[i], i);
            Selected = Armies[1];
        }
    }
}
