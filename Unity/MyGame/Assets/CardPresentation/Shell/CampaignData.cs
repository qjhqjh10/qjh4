// CampaignData.cs — 战役（Campaign）的**本地数据源**
//
// ============================ 两半，来源不同，别混 ============================
// ✅ **照原版（本地可读）**：**Ultramarines 那一套 47 个节点**的
//    `NodeId` / 坐标 `(x,y)` / `Cost` / `IsRepeatable` / 后继图 ——
//    出处 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/Campaign Node Data*.json`
//    （**47 个 SO**，`NodeId` 前缀只有 `UM`；`CampaignGraph` 本体没导出，但**边全在节点的 `nexts` 里**）。
//    🔴 **下表是脚本从 SO 直接抄出来的，不是手抄**（生成脚本见 `资料/阶段二_锻造厂与战役页_原版规格.md` §十二）。
//    另有 `Ultramarines Campaign.json`（`CampaignEventData`）：`eventId "UMCampaign"` · `filter.army 10` ·
//    **`PointsPerLevel 100`** · `Premium.targetId "premium_campaign_um"`。
//
// ❌ **我们挑的（原版取不到）**：**其余 12 个阵营的节点表**（本地只有 UM 这一套）·
//    当前进度（原版在 PlayFab 存档里）· 战役点余额的规则（见下面 `POINTS` 的注释）·
//    每个节点奖励物品的**图标**（原版走 `ItemDrawer` + 服务端 `RewardInfo`，本轮**不画物品图标**，见 §九）。
//
// ⚠️ **本地**只读得出 UM 一套 ⇒ 阵营选择条上其余 12 个阵营**照原版那样列出来**（`Campaign Army Selector`
//    的列表本来就是「当前有战役的阵营」），但选中它们时**明说「这一套本地没有」**（不许静默失败）。
using System;
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>战役的**本地**状态。**纯内存 + 可复现**（不用 `UnityEngine.Random`）。</summary>
    public static class CampaignData
    {
        // ============================================================ 原版常量

        /// <summary>奖励档。**值照原版** `RewardTier{ Basic = 0, Premium = 10 }`（`dump.cs:46232`）。</summary>
        public const int TierBasic = 0, TierPremium = 10;

        /// <summary>节点状态。**值照原版** `CampaignNode.NodeState`
        /// （出处 `资料/阶段二_锻造厂与战役页_原版规格.md` §十三「状态 → 外观」表）。</summary>
        public const int Locked = 0, Unlocked = 10, BaseCollected = 20, AllCollected = 30, Repeatable = 40;

        /// <summary>每升一级给的战役点。**照原版** `CampaignEventData.PointsPerLevel = 100`（本地实读到）。</summary>
        public const int PointsPerLevel = 100;

        /// <summary>🔴 **我们挑的**：战役点余额**固定给足**（照用户 2026-09-17 的边界
        /// 「不做真实经济：资源固定 9999、卡池/锻造厂/卡牌/头像全解锁」）。
        /// ⇒ **领取不扣点**，进度只由**节点图**卡着（原版是真扣的）。
        /// 好处：47 个节点可以一路点到底，页面上五种状态都能亲眼看到。</summary>
        public const int Points = 9999;

        // ============================================================ 节点表（照 SO 抄出）

        public struct NodeSpec
        {
            public string Id;
            public int X, Y, Cost;
            public bool Repeatable;
            /// <summary>该节点**有没有高级档奖励**（原版 `Setup()` 用它决定 `premiumMark` 显不显：
            /// `Data.Rewards.Any(r =&gt; r.Tier == 10)`）。</summary>
            public bool HasPremium;
            /// <summary>后继节点的**下标**（原版 `nexts` 是 SO 的 pathID，这里已解析成下标）。</summary>
            public int[] Next;
            public NodeSpec(string id, int x, int y, int cost, bool rep, bool prem, int[] next)
            { Id = id; X = x; Y = y; Cost = cost; Repeatable = rep; HasPremium = prem; Next = next; }
        }

        /// <summary>UM 战役：47 节点（`UM0`–`UM47`，**缺 `UM14`** —— 原版就没有这个 NodeId）。
        /// 🔴 **本表由脚本从 SO 直接抄出**（不手抄）：源 = `bundle_menus_assets_all/MonoBehaviour/Campaign Node Data*.json`。
        /// 列 = `NodeId, x, y, Cost, IsRepeatable, HasPremium, NextIndexes[]`（`HasPremium` =
        /// 该节点有没有 `rewardTier == 10` 的奖励 —— 原版拿它决定 `premiumMark` 显不显）。
        /// 规模：坐标 `x` 从 −8 到 **6712**（步长 **320**）· `y ∈ {−392,−264,−136,−8,120,248,376}`（**7 行**）·
        /// 花费档 **100…1100** · **唯一 `IsRepeatable` 的是 UM47（终点）**。</summary>
        static readonly NodeSpec[] Nodes =
        {
            // 列 = NodeId, x, y, Cost, IsRepeatable, HasPremium, NextIndexes[]
            new NodeSpec("UM0", -8, -8, 100, false, true, new int[]{ 1, 2 }),
            new NodeSpec("UM1", 312, -136, 200, false, false, new int[]{ 3, 4 }),
            new NodeSpec("UM2", 312, 120, 200, false, false, new int[]{ 5, 6 }),
            new NodeSpec("UM3", 632, -392, 300, false, true, new int[]{ 7 }),
            new NodeSpec("UM4", 632, -136, 300, false, true, new int[]{ 8 }),
            new NodeSpec("UM5", 632, 120, 300, false, true, new int[]{ 9 }),
            new NodeSpec("UM6", 632, 376, 300, false, true, new int[]{ 10 }),
            new NodeSpec("UM7", 952, -392, 400, false, false, new int[]{ 11 }),
            new NodeSpec("UM8", 952, -136, 400, false, false, new int[]{ 11 }),
            new NodeSpec("UM9", 952, 120, 400, false, false, new int[]{ 12 }),
            new NodeSpec("UM10", 952, 376, 400, false, false, new int[]{ 12 }),
            new NodeSpec("UM11", 1272, -136, 500, false, false, new int[]{ 13 }),
            new NodeSpec("UM12", 1272, 120, 500, false, false, new int[]{ 13 }),
            new NodeSpec("UM13", 1592, -8, 500, false, true, new int[]{ 14, 15 }),
            new NodeSpec("UM15", 1912, -136, 500, false, false, new int[]{ 16 }),
            new NodeSpec("UM16", 1912, 120, 500, false, false, new int[]{ 17, 18 }),
            new NodeSpec("UM17", 2232, -264, 500, false, true, new int[]{ 19 }),
            new NodeSpec("UM18", 2232, -8, 500, false, true, new int[]{ 20 }),
            new NodeSpec("UM19", 2232, 248, 500, false, true, new int[]{ 21 }),
            new NodeSpec("UM20", 2552, -264, 600, false, false, new int[]{ 22 }),
            new NodeSpec("UM21", 2552, -8, 600, false, false, new int[]{ 22 }),
            new NodeSpec("UM22", 2552, 248, 600, false, false, new int[]{ 23 }),
            new NodeSpec("UM23", 2872, -136, 700, false, false, new int[]{ 24 }),
            new NodeSpec("UM24", 2872, 248, 700, false, false, new int[]{ 24 }),
            new NodeSpec("UM25", 3192, -8, 800, false, true, new int[]{ 25, 26, 27 }),
            new NodeSpec("UM26", 3512, -264, 900, false, false, new int[]{ 28 }),
            new NodeSpec("UM27", 3512, -8, 900, false, false, new int[]{ 28 }),
            new NodeSpec("UM28", 3512, 248, 900, false, false, new int[]{ 29 }),
            new NodeSpec("UM29", 3832, -264, 1000, false, true, new int[]{ 30 }),
            new NodeSpec("UM30", 3832, 248, 1000, false, true, new int[]{ 31, 32 }),
            new NodeSpec("UM31", 4152, -264, 1000, false, false, new int[]{ 33 }),
            new NodeSpec("UM32", 4152, -8, 1000, false, false, new int[]{ 33 }),
            new NodeSpec("UM33", 4152, 248, 1000, false, false, new int[]{ 33 }),
            new NodeSpec("UM34", 4472, -8, 1000, false, true, new int[]{ 34, 35, 36 }),
            new NodeSpec("UM35", 4792, -264, 1000, false, false, new int[]{ 37 }),
            new NodeSpec("UM36", 4792, -8, 1000, false, false, new int[]{ 37, 38 }),
            new NodeSpec("UM37", 4792, 248, 1000, false, false, new int[]{ 38 }),
            new NodeSpec("UM38", 5112, -136, 1000, false, true, new int[]{ 39, 40 }),
            new NodeSpec("UM39", 5112, 120, 1000, false, true, new int[]{ 40, 41 }),
            new NodeSpec("UM40", 5432, -264, 1000, false, false, new int[]{ 42 }),
            new NodeSpec("UM41", 5432, -8, 1000, false, false, new int[]{ 42, 43 }),
            new NodeSpec("UM42", 5432, 248, 1000, false, false, new int[]{ 43 }),
            new NodeSpec("UM43", 5752, -136, 1000, false, true, new int[]{ 44 }),
            new NodeSpec("UM44", 5752, 120, 1000, false, true, new int[]{ 44 }),
            new NodeSpec("UM45", 6072, -8, 1000, false, true, new int[]{ 45 }),
            new NodeSpec("UM46", 6392, -8, 1000, false, true, new int[]{ 46 }),
            new NodeSpec("UM47", 6712, -8, 1100, true, false, new int[]{  }),
        };

        public static int NodeCount { get { return Nodes.Length; } }
        public static NodeSpec At(int i) { return Nodes[i]; }
        public static int IndexOf(string nodeId)
        {
            for (int i = 0; i < Nodes.Length; i++) if (Nodes[i].Id == nodeId) return i;
            return -1;
        }

        /// <summary>节点坐标的跨度（给轨道算缩放比用）：x 的 min/max、y 的绝对最大值。</summary>
        public static void Span(out int minX, out int maxX, out int maxAbsY)
        {
            minX = int.MaxValue; maxX = int.MinValue; maxAbsY = 0;
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].X < minX) minX = Nodes[i].X;
                if (Nodes[i].X > maxX) maxX = Nodes[i].X;
                int ay = Math.Abs(Nodes[i].Y); if (ay > maxAbsY) maxAbsY = ay;
            }
        }

        // ============================================================ 进度（⚠️ 我们挑的初值）

        static readonly bool[] _base = new bool[Nodes.Length];
        static readonly bool[] _prem = new bool[Nodes.Length];
        static int[][] _prev;

        static CampaignData()
        {
            BuildPrev();
            ResetForTest();
        }

        static void BuildPrev()
        {
            _prev = new int[Nodes.Length][];
            var tmp = new List<int>[Nodes.Length];
            for (int i = 0; i < Nodes.Length; i++) tmp[i] = new List<int>();
            for (int i = 0; i < Nodes.Length; i++)
                foreach (int n in Nodes[i].Next) if (n >= 0 && n < Nodes.Length) tmp[n].Add(i);
            for (int i = 0; i < Nodes.Length; i++) _prev[i] = tmp[i].ToArray();
        }

        /// <summary>这一格是不是「开着」的。**照原版语义**：根节点起手就开；其余只要**任一前驱已领基础档**就开。</summary>
        public static bool IsUnlocked(int i)
        {
            if (i == 0) return true;                       // `UM0` 是根
            var pr = _prev[i];
            for (int k = 0; k < pr.Length; k++) if (_base[pr[k]]) return true;
            return false;
        }

        /// <summary>第 `i` 格的**基础档**状态。**照原版 `CampaignTrack.GetNodeState` 的口径**：
        /// 基础+高级都领 → `AllCollected(30)`；只领基础 → `BaseCollected(20)`；
        /// 开着 → `Unlocked(10)`；没开 → `Locked(0)`。
        /// ⚠️ `Repeatable(40)` 原版只在「终点且可重复」时用 —— 我们让 `UM47` 在领过之后**回到 40**（可重复领）。</summary>
        public static int StateOf(int i)
        {
            if (_base[i] && _prem[i]) return AllCollected;
            if (_base[i]) return Nodes[i].Repeatable ? Repeatable : BaseCollected;
            return IsUnlocked(i) ? Unlocked : Locked;
        }

        /// <summary>这一格现在能不能领（原版 `CampaignRewardsWindowContext.Claimable` 的口径：
        /// `State ∈ {Unlocked, Repeatable}` **且** `AvailablePoints ≥ NodeCost`）。
        /// ⚠️ 我们把战役点固定给足 ⇒ 后半句恒真，卡的是**节点图**（见 `Points` 的注释）。</summary>
        public static bool Claimable(int i) { int s = StateOf(i); return s == Unlocked || s == Repeatable; }

        /// <summary>基础档领过没有（`CampaignRewardsWindowContext.BaseCollected`）。</summary>
        public static bool BaseClaimed(int i) { return i >= 0 && i < _base.Length && _base[i]; }

        /// <summary>高级档领过没有（`CampaignRewardsWindowContext.PremiumCollected`）。</summary>
        public static bool PremiumClaimed(int i) { return i >= 0 && i < _prem.Length && _prem[i]; }

        /// <summary>领一格。**照原版 `CampaignNode.Collect(tier)`**：
        /// 基础档 → 记 `_base`；高级档 → 记 `_prem`。领完**自动把后继解锁**（那一步靠 `IsUnlocked` 现算，不用写）。
        /// 返回「真的领到了吗」；领不到时 `why` 说清原因（红线：**不许静默失败**）。</summary>
        public static bool Claim(int i, int tier, out string why)
        {
            why = null;
            if (i < 0 || i >= Nodes.Length) { why = "格号越界"; return false; }
            if (!Claimable(i))
            {
                int s = StateOf(i);
                why = s == Locked ? "这一格还没解锁（前驱没领）"
                                  : "这一格的基础档已经领过了";
                return false;
            }
            if (tier == TierPremium)
            {
                if (_prem[i]) { why = "高级档已经领过了"; return false; }
                _prem[i] = true;
                if (!_base[i]) _base[i] = true;            // 原版 `Collect(Premium)` 直接把 State 推到 30（基础+高级都算领了）
                return true;
            }
            if (_base[i]) { why = "基础档已经领过了"; return false; }
            _base[i] = true;
            return true;
        }

        /// <summary>自检用：把进度推回初值（只解锁根节点）。</summary>
        public static void ResetForTest()
        {
            for (int i = 0; i < Nodes.Length; i++) { _base[i] = false; _prem[i] = false; }
        }

        /// <summary>已领的格数（诊断用）。</summary>
        public static int ClaimedCount
        {
            get { int n = 0; for (int i = 0; i < Nodes.Length; i++) if (_base[i]) n++; return n; }
        }

        // ============================================================ 阵营 / 背景

        /// <summary>13 个阵营。**顺序照卡池实测**（与 `ForgeData.Armies` 同一批；徽记图走 `DeckRuntime.FactionIcon`）。
        /// ⚠️ **本地只有 Ultramarines 那一套战役** ⇒ 其余 12 个在页面上**照列**，选中时**如实说明**。</summary>
        public static readonly string[] Armies = ForgeData.Armies;

        /// <summary>本地有战役内容的阵营。**照实说：只有 UM**（原版 13 个阵营各一套，本地只导出这一套的节点表）。</summary>
        public const string ArmiesWithContent = "Ultramarines";
        public static bool HasContent(string army) { return army == ArmiesWithContent; }

        /// <summary>阵营 → 战役背景图。**GUID ↔ 阵营 已闭环**（`Ultramarines Campaign.json` 的
        /// `Background.m_AssetGUID` = `2cca2c1f…` → `Campaign_Faction_Bck_Ultramarines`）；
        /// 13 张都是 **1024² 整图**（`m_Rect = 0,0,1024,1024`）⇒ 不用裁。落在 `Resources/Art/ui_campaign/`。</summary>
        public static string Background(string army)
        {
            switch (army)
            {
                case "Ultramarines": return "Campaign_Faction_Bck_Ultramarines";
                case "Goff": return "Campaign_Faction_Bck_Orks";
                case "SaimHann": return "Campaign_Faction_Bck_Saim-Hann";
                case "BlackLegion": return "Campaign_Faction_Bck_BlackLegion";
                case "DarkAngels": return "Campaign_Faction_Bck_Dark_Angels";
                case "Genestealers": return "Campaign_Faction_Bck_Genestealers";
                case "TauEmpire": return "Campaign_Faction_Bck_Tau_Empire";
                case "Sautekh": return "Campaign_Faction_Bck_Sautekh";
                case "AstraMilitarum": return "Campaign_Faction_Bck_AstraMilitarum";
                case "Leviathan": return "Campaign_Faction_Bck_Leviathan";
                case "Sororitas": return "Campaign_Faction_Bck_Sororitas";
                case "EmperorsChildren": return "Campaign_Faction_Bck_Emperors_Children";
                case "SpaceWolves": return "Campaign_Faction_Bck_Space_Wolves";
                default: return "Campaign_Faction_Bck_Ultramarines";
            }
        }

        /// <summary>当前选中的阵营（照原版 `CampaignHandler.SelectedArmy`；我们给个固定的起点）。</summary>
        public static string Selected { get; private set; } = ArmiesWithContent;
        public static void Select(string army)
        {
            if (string.IsNullOrEmpty(army)) return;
            foreach (var a in Armies) if (a == army) { Selected = a; return; }
        }

        /// <summary>节点图上某一格的**显示名**（我们只有 `NodeId`；原版节点上也不印名字，这里给 tooltip/诊断用）。</summary>
        public static string NodeName(int i) { return Nodes[i].Id; }

        // ============================================================ 奖励表（照 SO 抄出）

        /// <summary>一条奖励。**照原版 `RewardInfo`**：`{ item.targetId, quantity, rewardTier }`。
        /// `Tier` 用 `TierBasic(0)` / `TierPremium(10)`（原版 `RewardTier`）。</summary>
        public struct RewardSpec
        {
            public string Id;          // = SO 的 `item.targetId`
            public int Quantity;
            public int Tier;
            public RewardSpec(string id, int qty, int tier) { Id = id; Quantity = qty; Tier = tier; }
        }

        /// <summary>UM 这 47 个节点的奖励表。🔴 **由 `工具/gen_campaign_rewards.py` 从 SO 直接生成，别手改**
        /// （源 = `bundle_menus_assets_all/MonoBehaviour/Campaign Node Data*.json` 的 `Rewards[]`）。
        /// 规模：**89 条**（基础档 70 · 高级档 19）· **32 个唯一 `targetId`**。
        /// 与 `Nodes[i]` **同序**（UM0, UM1, … UM13, UM15, … UM47 —— 原版缺 UM14）。</summary>
        static readonly RewardSpec[][] Rewards =
        {
            new RewardSpec[] { new RewardSpec("UM_SK_Starter", 1, TierBasic), new RewardSpec("DT Ultramarines R4", 1, TierPremium) },	// UM0
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic) },	// UM1
            new RewardSpec[] { new RewardSpec("WildcardUltramarines2", 1, TierBasic), new RewardSpec("DT Ultramarines All", 3, TierBasic) },	// UM2
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic), new RewardSpec("WildcardUltramarines3", 1, TierPremium) },	// UM3
            new RewardSpec[] { new RewardSpec("WildcardUltramarines2", 1, TierBasic), new RewardSpec("DT Ultramarines All", 3, TierBasic), new RewardSpec("Booster Pack Ultramarines", 1, TierPremium) },	// UM4
            new RewardSpec[] { new RewardSpec("C2", 400, TierBasic), new RewardSpec("DT Ultramarines All", 3, TierBasic), new RewardSpec("Booster Pack Ultramarines", 1, TierPremium) },	// UM5
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic), new RewardSpec("C2", 400, TierPremium) },	// UM6
            new RewardSpec[] { new RewardSpec("UM34", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM7
            new RewardSpec[] { new RewardSpec("UM2", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM8
            new RewardSpec[] { new RewardSpec("UM3", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM9
            new RewardSpec[] { new RewardSpec("UM27", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM10
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic) },	// UM11
            new RewardSpec[] { new RewardSpec("C2", 400, TierBasic), new RewardSpec("DT Ultramarines All", 3, TierBasic) },	// UM12
            new RewardSpec[] { new RewardSpec("643ee10a8aeb99a4990e3bb9a2a0faf8", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic), new RewardSpec("c79939c3d8c97438980edf66e212c412", 1, TierPremium) },	// UM13
            new RewardSpec[] { new RewardSpec("UM39", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM15
            new RewardSpec[] { new RewardSpec("UM8", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM16
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic), new RewardSpec("Booster Pack Ultramarines", 1, TierPremium) },	// UM17
            new RewardSpec[] { new RewardSpec("WildcardUltramarines1", 3, TierBasic), new RewardSpec("WildcardUltramarines2", 1, TierPremium) },	// UM18
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic), new RewardSpec("Booster Pack Ultramarines", 1, TierPremium) },	// UM19
            new RewardSpec[] { new RewardSpec("UM13", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM20
            new RewardSpec[] { new RewardSpec("UM17", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM21
            new RewardSpec[] { new RewardSpec("C2", 500, TierBasic) },	// UM22
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic) },	// UM23
            new RewardSpec[] { new RewardSpec("UM24", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM24
            new RewardSpec[] { new RewardSpec("d139f53f941ce40f88bd07adf20c08ad", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic), new RewardSpec("WildcardUltramarines3", 1, TierPremium) },	// UM25
            new RewardSpec[] { new RewardSpec("UM44", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM26
            new RewardSpec[] { new RewardSpec("UM22", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM27
            new RewardSpec[] { new RewardSpec("C2", 500, TierBasic) },	// UM28
            new RewardSpec[] { new RewardSpec("C2", 500, TierBasic), new RewardSpec("C2", 500, TierPremium) },	// UM29
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic), new RewardSpec("Booster Pack Ultramarines", 1, TierPremium) },	// UM30
            new RewardSpec[] { new RewardSpec("UM55", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM31
            new RewardSpec[] { new RewardSpec("UM28", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM32
            new RewardSpec[] { new RewardSpec("WildcardUltramarines1", 3, TierBasic) },	// UM33
            new RewardSpec[] { new RewardSpec("10a1e3f4c77c0491196b655ad4d7d72d", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic), new RewardSpec("WildcardUltramarines3", 1, TierPremium) },	// UM34
            new RewardSpec[] { new RewardSpec("UM37", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM35
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic) },	// UM36
            new RewardSpec[] { new RewardSpec("UM18", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic) },	// UM37
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic), new RewardSpec("Booster Pack Ultramarines", 1, TierPremium) },	// UM38
            new RewardSpec[] { new RewardSpec("C2", 500, TierBasic), new RewardSpec("WildcardUltramarines2", 1, TierPremium) },	// UM39
            new RewardSpec[] { new RewardSpec("WildcardUltramarines2", 1, TierBasic) },	// UM40
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic) },	// UM41
            new RewardSpec[] { new RewardSpec("DT Ultramarines R3", 1, TierBasic) },	// UM42
            new RewardSpec[] { new RewardSpec("WildcardUltramarines2", 1, TierBasic), new RewardSpec("3a65e0454212c48d0a1c03a500b83262", 1, TierPremium) },	// UM43
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic), new RewardSpec("WildcardUltramarines3", 1, TierPremium) },	// UM44
            new RewardSpec[] { new RewardSpec("dd590709eb8591e4c997d3079221294c", 1, TierBasic), new RewardSpec("DT Ultramarines All", 4, TierBasic), new RewardSpec("8c4524d276408314bb4af76ff397c62f", 1, TierPremium) },	// UM45
            new RewardSpec[] { new RewardSpec("DT Ultramarines R4", 1, TierBasic), new RewardSpec("WildcardUltramarines4", 1, TierPremium) },	// UM46
            new RewardSpec[] { new RewardSpec("Booster Pack Ultramarines", 1, TierBasic) },	// UM47
        };

        /// <summary>第 `i` 格的全部奖励（**原样，不分档**）。</summary>
        public static RewardSpec[] RewardsOf(int i)
        {
            return (i >= 0 && i < Rewards.Length) ? Rewards[i] : new RewardSpec[0];
        }

        /// <summary>第 `i` 格**某一档**的奖励。**照原版 `Open()`**：`Rewards.Where(r =&gt; r.rewardTier == tier)`
        /// （`CampaignRewardsWindow__Open.c` 里两个 `Enumerable.Any`，谓词就是 `rewardTier == 0 / == 10`）。</summary>
        public static RewardSpec[] RewardsOf(int i, int tier)
        {
            var all = RewardsOf(i);
            int n = 0;
            for (int k = 0; k < all.Length; k++) if (all[k].Tier == tier) n++;
            var o = new RewardSpec[n];
            n = 0;
            for (int k = 0; k < all.Length; k++) if (all[k].Tier == tier) o[n++] = all[k];
            return o;
        }

        /// <summary>第 `i` 格的奖励列表在 `Rewards` 里的下标（导出数据与 `Nodes` 对不上时报出来）。</summary>
        public static void SelfCheck(out int nodeCount, out int rewardCount, out int mismatch)
        {
            nodeCount = Nodes.Length;
            rewardCount = 0;
            mismatch = 0;
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (i >= Rewards.Length) { mismatch++; continue; }
                rewardCount += Rewards[i].Length;
                // `HasPremium` 是同一份 SO 里算出来的 ⇒ 两处必须一致（不一致就是抄错了）
                bool any = false;
                for (int k = 0; k < Rewards[i].Length; k++) if (Rewards[i][k].Tier == TierPremium) any = true;
                if (any != Nodes[i].HasPremium) mismatch++;
            }
        }

        // ============================================================ 奖励物品的**图标**

        /// <summary>奖励物品的图标 sprite 名。`null` = **本地拿不到**（调用方**必须出声**，不许静默）。
        /// <para>⚠️ **原版这一步走 `ItemDrawer.Draw()` —— 而 `ItemDrawerConfig` SO 与那批抽屉 prefab
        /// 本地都没有**（2026-09-23 全库搜 `ItemDrawer*` 资产 **0 命中**；`ItemDrawer__Draw.c` 证实
        /// 它是从配置里 `Instantiate` 一个抽屉 prefab 再 `Initialize(item, quantity, …)`）。
        /// ⇒ 下表是**我们建的**，逐条标出处：</para>
        /// <list type="bullet">
        /// <item>`Booster Pack Ultramarines` → **原版字段原文**：它的 SO（`bundle_menus_assets_all/MonoBehaviour/
        ///   Booster Pack Ultramarines.json`）里 `containerPreviewImage.m_SubObjectName = "40K_shop_offer_booster_UM"`。</item>
        /// <item>`WildcardUltramarines1..4` → **我们建的映射**：SO 的 `cardRarity` 是 1/2/3/4，
        ///   对应图族 `40k_general_wildcard_{common,rare,epic,legendary}_small`（那四张图本工程已有）。</item>
        /// <item>其余（`DT Ultramarines *` · `C2` · `UMxx` · 32 位 hex）**没有图标** ——
        ///   `DT Ultramarines All` 等 SO **没有** `containerPreviewImage`；`C2`／hex 连 SO 都没导出。
        ///   ⇒ 返回 `null`，由 `CampaignRewardWindow` **逐条打日志**（项目红线：不许静默失败）。</item>
        /// </list></summary>
        public static string ItemIcon(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return null;
            if (targetId == "Booster Pack Ultramarines") return "40K_shop_offer_booster_UM";
            if (targetId.StartsWith("WildcardUltramarines"))
            {
                // 后缀 1..4 = SO 的 `cardRarity` 1..4（已逐条实读）
                switch (targetId.Substring("WildcardUltramarines".Length))
                {
                    case "1": return "40k_general_wildcard_common_small";
                    case "2": return "40k_general_wildcard_rare_small";
                    case "3": return "40k_general_wildcard_epic_small";
                    case "4": return "40k_general_wildcard_legendary_small";
                }
            }
            return null;
        }

        /// <summary>物品的**短名**（没有图标时画在占位板上 / 诊断用）：hex id 取前 8 位。</summary>
        public static string ItemShortName(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return "";
            if (targetId.Length == 32 && IsHex(targetId)) return targetId.Substring(0, 8);
            return targetId;
        }

        static bool IsHex(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!ok) return false;
            }
            return true;
        }
    }
}
