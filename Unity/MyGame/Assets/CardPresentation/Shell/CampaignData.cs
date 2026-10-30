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
    /// <summary>战役的**本地**状态。**可复现**（不用 `UnityEngine.Random`）。
    /// <para>🆕 2026-10-27（`E3CMP`）：进度**不再是纯内存** —— `_base` / `_prem` / `Selected` 落
    /// `SaveKeys.ActiveCampaign`（见文件尾「进度存档」那段）；批处理自检仍拿到干净起手态（读档被 `SaveCenter.StoreOn` 挡住）。</para></summary>
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

        /// 🔶 自己设计（非原版）：战役点余额固定给足（不做真实经济） · 用户边界「资源固定、卡池/锻造厂/卡牌/头像全解锁」（判据不适用：原版是真实经济、本项由用户拍板） · 用户 2026-09-17｜原注：<summary>🔴 **我们挑的**：战役点余额**固定给足**（照用户 2026-09-17 的边界
        /// 「不做真实经济：资源固定 2000、卡池/锻造厂/卡牌/头像全解锁」——
        /// ⚠️ 钱包那笔的固定值用户 2026-10-24 已由 9999 改为 2000；
        /// **本行这个战役点 `Points` 仍是 9999、本批未动**，用户 2026-10-24 只改 `Wallet.StartAmount`）。
        /// ⇒ **领取不扣点**，进度只由**节点图**卡着（原版是真扣的）。
        /// 好处：47 个节点可以一路点到底，页面上五种状态都能亲眼看到。</summary>
        public const int Points = 9999;      // ⚠️ 战役点（不是 `Wallet` 的钱包余额）—— 2026-10-24 未改

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

        /// <summary>根节点（**无前置**的那个）的 Y —— 原版 `CampaignTrack.GetNodesOffset` 的
        /// `FirstOrDefault(n => n.UnlockedBy.Count &lt;= 0)` 取 `+0x3c`（= Y）。
        /// 判据：`decomp_full/CampaignTrack__GetNodesOffset.c:34-52` + `__c___GetNodesOffset_b__11_0.c`（谓词 = `Count ≤ 0`）。
        /// ⚠️ 我们是**从 `Next` 反推**（表里没有 `UnlockedBy` 这一列）：第一个「没有被任何人指向」的节点。
        /// 本表第一个就是 `UM0`（`Y = −8`）⇒ 与原版同值。
        /// 🔴 2026-10-27（B88·E13）：**这一个数决定整条轨道的缩放比与竖向对齐**
        /// （`CalculateRatio` 的分母是「相对它的最大偏差」，`GetNodePosition` 的 y 也减它）—— ⛔ 别改成「minX 那个节点」。</summary>
        public static int RootY
        {
            get
            {
                for (int i = 0; i < Nodes.Length; i++)
                {
                    bool hasParent = false;
                    for (int j = 0; j < Nodes.Length && !hasParent; j++)
                    {
                        var nx = Nodes[j].Next;
                        if (nx == null) continue;
                        for (int k = 0; k < nx.Length; k++) if (nx[k] == i) { hasParent = true; break; }
                    }
                    if (!hasParent) return Nodes[i].Y;
                }
                return 0;                                   // 全都有前置（不该发生）⇒ 退化成「不偏移」
            }
        }

        /// <summary>节点坐标的跨度（给轨道算缩放比用）：x 的 min/max、**Y 相对根节点的最大绝对偏差**。
        /// 判据（`decomp_full/CampaignTrack__CalculateRatio.c:40-71`）：`max(|maxY − 根Y|, |minY − 根Y|)`
        /// —— ⚠️ 不是 `max|Y|`：本表根 Y = −8、极端是 −392 与 376 ⇒ 偏差 **384**（原来按 `max|Y|` 算得 392）。</summary>
        public static void Span(out int minX, out int maxX, out int maxAbsDevY)
        {
            minX = int.MaxValue; maxX = int.MinValue; maxAbsDevY = 0;
            int ry = RootY;
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].X < minX) minX = Nodes[i].X;
                if (Nodes[i].X > maxX) maxX = Nodes[i].X;
                int d = Math.Abs(Nodes[i].Y - ry); if (d > maxAbsDevY) maxAbsDevY = d;
            }
        }

        // 🔶 自己设计（非原版）：战役进度初值 · 原版在服务端存档、本地没有 · TODO(谁定的)｜原注：============================================================ 进度（⚠️ 我们挑的初值）

        static readonly bool[] _base = new bool[Nodes.Length];
        static readonly bool[] _prem = new bool[Nodes.Length];
        static int[][] _prev;

        static CampaignData()
        {
            BuildPrev();
            ResetForTest();
            LoadFromSave();   // 🆕 2026-10-27（E3CMP）：进度落 `SaveKeys.ActiveCampaign`（见文件尾「进度存档」那段）
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

        /// <summary>这一格是不是「开着」的。**照原版语义**：根节点起手就开；其余只要**任一前驱已领基础档**就开；外加 `_sessionUnlocked`（原版 `Collect` 当场把相邻 `locked` 置 10 那一拍，见类尾）。</summary>
        public static bool IsUnlocked(int i)
        {
            if (i == 0 || _sessionUnlocked[i]) return true;    // `UM0` 是根；`_sessionUnlocked` 见类尾「会话内解锁」
            var pr = _prev[i];
            for (int k = 0; k < pr.Length; k++) if (_base[pr[k]]) return true;
            return false;
        }

        /// <summary>第 `i` 格的**基础档**状态。**照原版 `CampaignTrack.GetNodeState` 的口径**：
        /// 高级档列表命中 → `AllCollected(30)`；基础档列表命中 → `BaseCollected(20)`；
        /// 开着 → `Unlocked(10)`；没开 → `Locked(0)`；**20/30 两支都再被 `IsRepeatable` 覆盖成 40**
        /// （`CampaignTrack__GetNodeState.c:27-58` 的 `LAB_1806b9a30` ⇒ 可重复节点永远可再领）。</summary>
        public static int StateOf(int i)
        {
            if (_prem[i]) return Nodes[i].Repeatable ? Repeatable : AllCollected;   // 原版：premList 命中即 30，再被 IsRepeatable 覆盖成 40
            if (_base[i]) return Nodes[i].Repeatable ? Repeatable : BaseCollected;
            return IsUnlocked(i) ? Unlocked : Locked;
        }

        /// <summary>这一格现在能不能领（原版 `CampaignRewardsWindowContext.Claimable` 的口径：
        /// `State ∈ {Unlocked, Repeatable}` **且** `AvailablePoints ≥ NodeCost`）。
        /// ⚠️ 我们把战役点固定给足 ⇒ 后半句恒真，卡的是**节点图**（见 `Points` 的注释）。</summary>
        public static bool Claimable(int i) { int s = StateOf(i); return s == Unlocked || s == Repeatable; }

        /// <summary>基础档领过没有（原版 `CampaignRewardsWindowContext.BaseCollected` = `State ∈ {20,30}`；⚠️ 可重复节点待领时 `State=40` ⇒ **false**，与 `_base[i]` **不等价**）。</summary>
        public static bool BaseClaimed(int i) { if (i < 0 || i >= _base.Length) return false; int s = StateOf(i); return s == BaseCollected || s == AllCollected; }

        /// <summary>高级档领过没有（原版 `CampaignRewardsWindowContext.PremiumCollected` = `State == 30`；可重复节点 `State=40` ⇒ **false**）。</summary>
        public static bool PremiumClaimed(int i) { return i >= 0 && i < _base.Length && StateOf(i) == AllCollected; }

        /// <summary>领一格。**照原版 `CampaignNode.Collect(tier)`**：
        /// 基础档 → 记 `_base`；高级档 → **只**记 `_prem`（原版不写 basicList）。领完把相邻节点记进 `_sessionUnlocked`（`IsUnlocked` 派生用）。
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
            // 🔴 原版 `CampaignNode.Collect` 的闸**只有** `State ∈ {10, 40}`（`CampaignNode__Collect.c:29-33`）⇒ **没有逐档拒绝**：
            //    原来那两句 `if (_prem[i]) …高级档已经领过了` / `if (_base[i]) …基础档已经领过了` 是原版没有的，还会把**可重复节点的重复领**挡死 ⇒ 已删，⛔ 别加回来。
            // 🆕 **2026-10-31（`B104_P3`）次序改了**：原来是「改 `_base`/`_prem` → `MarkSuccessorsUnlocked` → `SaveToSave()`」，
            //    而 `SaveToSave()` 的返回值被丢掉 ⇒ **写盘失败也 `return true`（窗上显示「已领取」，重启后却没有）**。
            //    现在 = **改标记 → 落盘 → 成功了才做那两件副作用**；落盘失败 ⇒ 撤销标记 + `why` 说清 + `false`。
            //    （`MarkSuccessorsUnlocked` 与 `NotifyCampaignCompletionIfAllClaimed` 都**只读**标记表 ⇒ 挪到落盘之后语义不变。）
            if (tier == TierPremium)
            {
                _prem[i] = true;                           // 原版：高级档**只**写 premList、⛔ 不写 basicList（`…_g__OnCollect_2.c:140-165`）
                if (!SaveToSave()) { _prem[i] = false; why = "这一格的进度**没能落盘**（存档写不进去）⇒ 这次领取不算数"; return false; }
                MarkSuccessorsUnlocked(i);                 // 原版 `Collect` 末段：相邻 `locked(0)` 节点 → `unlocked(10)`（会话内有效）
                // 🆕 **2026-10-25（执行代理 `X18`）**：`Ach_16`（完成战役）的唯一写点（判据 §B-3 #8）。
                NotifyCampaignCompletionIfAllClaimed();
                return true;
            }
            _base[i] = true;
            if (!SaveToSave()) { _base[i] = false; why = "这一格的进度**没能落盘**（存档写不进去）⇒ 这次领取不算数"; return false; }
            MarkSuccessorsUnlocked(i);                     // 原版 `Collect` 末段（同上）
            // 🆕 同上：这一格领完可能就是**最后一格** ⇒ 每次成功领取都问一次（幂等，见那个方法）。
            NotifyCampaignCompletionIfAllClaimed();
            return true;
        }

        /// <summary>🆕 **2026-10-25（执行代理 `X18`）**：**战役「完成」的本地判据** = `Ach_16`（`CompleteCampaign`）的输入。
        /// <para>🔴 **2026-10-29（`B99_G2`）改成原版口径**：`Everguild.LiveOps.Campaign.get_IsComplete`
        /// = `basicList.Contains(Last(GetCampaignNodes()).NodeId)`（`Campaign__get_IsComplete.c:29-31`）
        /// ⇒ **「末节点在基础档列表里」**，⛔ **不是**「47 格基础档全真」（那是我们原来的近似）。
        /// <para>「末节点」= `GetCampaignNodes()` 的**表末项**（我们 `Nodes` 表末项 = 终点 `UM47`，与生成的 SO 顺序一致）。
        /// ⚠️ 名字为兼容保留（全仓调用点只在本类的 `NotifyCampaignCompletionIfAllClaimed`）。</para></summary>
        public static bool AllNodesClaimed()
        {
            if (Nodes.Length == 0) return false;
            // 原版 `IsComplete` = 末节点 ∈ 基础档列表（`Campaign__get_IsComplete.c:29-31`）——「末节点」= `Nodes` 表末项 `UM47`
            return _base[Nodes.Length - 1];
        }

        /// <summary>🆕 **2026-10-25（执行代理 `X18`）**：**战役全通 ⇒ `Ach_16` 记 1**（唯一写点 = `Claim` 成功之后）。
        /// <para>口径（判据 §B-3 #8）：原版 `checkAlreadyCompleted = 1` = **只记一次** ⇒ 用
        /// `ProfileData.SetAchievementValueAtLeast("Ach_16", 1)`（天然幂等），
        /// ⛔ **不是**「每领一个节点 +1」、⛔ 也不是 `AddAchievementValue`。
        /// **不依赖** F6（`_base/_prem` 全内存、不落盘）：成就进度自己落在 `profile.json` 里
        /// ⇒ 重开游戏后战役进度归零、再走一遍全通，也**不会**从 1 涨到 2。</para>
        /// <returns>本次真的把进度抬上去了吗（自检用；`false` = 没全领 / 已经记过）。</returns></summary>
        public static bool NotifyCampaignCompletionIfAllClaimed()
        {
            if (!AllNodesClaimed()) return false;
            if (ProfileData.AchievementValue(ProfileData.IdAchCompleteCampaign) >= 1) return false;
            ProfileData.SetAchievementValueAtLeast(ProfileData.IdAchCompleteCampaign, 1);
            return true;
        }

        /// <summary>自检用：把进度推回初值（只解锁根节点）。
        /// ⚠️ **只清内存、不动盘上的档**（批处理里 `SaveCenter.StoreOn == false` ⇒ 本来也没读进来；
        /// 真机里调它 = 内存清空，下一次 `Claim`/`Select` 才把清空后的状态写回去）。</summary>
        public static void ResetForTest()
        {
            for (int i = 0; i < Nodes.Length; i++) { _base[i] = false; _prem[i] = false; _sessionUnlocked[i] = false; }
        }

        /// <summary>已领**基础档**的格数（诊断用；= 原版 `basicList` 的条数口径，**高级档不计**）。</summary>
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
        /// 13 张都是 **1024² 整图**（`m_Rect = 0,0,1024,1024`）⇒ 不用裁。
        /// 🔴 **2026-10-17 就地订正（铁律 5）**：原来这里写「落在 `Resources/Art/ui_campaign/`」——
        /// **不成立**：那个目录是**空的**、也没有任何取图口读它；这 13 张实际在
        /// **`Resources/Art/ui_menu/`**（现读磁盘）。调用方走 `_win.Rect/_win.Art`（`MenuUi` 口径 ⇒
        /// `ui_menu/ → ui_deck/ → ui/` 三级兜底），所以**行为一直是对的，错的只是这条注释**。
        /// 判据 → `普查产出_1017/BSC62/S3_取图口错目录普查.md` §顺手发现 ①。</summary>
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
            // 🆕 **2026-10-31（`B104_P3`）**：落盘失败 ⇒ **撤销选中**（否则窗上显示新阵营、重启却回旧的）。
            foreach (var a in Armies) if (a == army) { string prev = Selected; Selected = a; if (!SaveToSave()) { Selected = prev; } return; }   // 🆕 2026-10-27（E3CMP）：选中的阵营也落盘
        }

        /// <summary>节点图上某一格的**显示名**（我们只有 `NodeId`；原版节点上也不印名字，这里给 tooltip/诊断用）。</summary>
        public static string NodeName(int i) { return Nodes[i].Id; }

        // ============================================================ 奖励表（照 SO 抄出）

        /// <summary>一条奖励。**照原版 `RewardInfo`**：`{ item.targetId, quantity, rewardTier }`
        /// **+ 2026-10-11（A311）补的两样**（原版 `RewardInfo` 自己的 `+0x30`/`+0x38` 与它基类
        /// `ItemInfo&lt;ObtainableItem&gt;` 的 `+0x28`）—— 见各字段的 doc。
        /// `Tier` 用 `TierBasic(0)` / `TierPremium(10)`（原版 `RewardTier`）。</summary>
        public struct RewardSpec
        {
            public string Id;          // = SO 的 `item.targetId`
            public int Quantity;
            public int Tier;

            /// <summary>🆕 **2026-10-11（A311）**：原版 `RewardInfo.IsEphemeral`
            /// （`d:/2/tools/decomp_full/RewardInfo__get_IsEphemeral.c` 逐句实读：
            /// `ephemeralState(+0x28) != null &amp;&amp; (ephemeralDuration(+0x20) &gt; 0
            /// || (boundEvent(+0x10) != null &amp;&amp; 它的 EventReference 解得出来))`）。
            /// 只有它为真，`RewardWindow.Open` 才会调 `SetEphemeralDisplay`（那四跳的第 2 跳）。
            /// ⚠️ **我们的 89 条战役奖励这一格全是 `false`**（判据 = 47 份 `Campaign Node Data*.json` 的
            /// `Rewards[].ephemeralState` 逐条实读：`ephemeralStartTime 0` / `ephemeralDuration 0`、
            /// `boundEvent` 是空引用）⇒ **今天这条链一次都不触发**（⛔ 不是「没实现」）。</summary>
            public bool IsEphemeral;

            /// <summary>🆕 **2026-10-11（A311）**：原版 `EphemeralState.ephemeralDuration`
            /// （字段上标着 `[DrawMillis(1)]` ⇒ 单位是**毫秒**）。
            /// ⚠️ 原版画的那段字**不是**「剩余时间」而是 **`EphemeralEndTime − EphemeralStartTime`**
            /// = **总时长**（`ItemDrawer&lt;object&gt;.SetEphemeralDisplay` 的方法体实读，见 `Shell/ItemDrawer.cs`）
            /// ⇒ 这里存的就是它，`IsEphemeral` 为假时无意义。</summary>
            public long EphemeralMs;

            /// <summary>🆕 **2026-10-11（A311）**：原版 `RewardInfo.convertedInto`（`+0x38`，**一个嵌套的 `RewardInfo`**）。
            /// `null` = 原版那个 `null`（**不是**「判据空」—— 判据就是 SO 里的 `convertedInto` 键，
            /// 47 份 `Campaign Node Data*.json` 的 89 条奖励**逐条实读：一个都没有**，
            /// 因为 `[JsonIgnoreIfEmpty]` 把它省略掉了）。
            /// 非 `null` 时 `RewardWindow.Open` 走四跳的第 3 跳（`SetConvertedItem`）。</summary>
            public RewardConversion? ConvertedInto;

            /// <summary>三参 ctor（`工具/gen_campaign_rewards.py` 生成的 89 行走的就是它 ⇒ **签名与行为都不许动**）
            /// —— 直接把 A311 那两个新量送到默认档。</summary>
            public RewardSpec(string id, int qty, int tier) : this(id, qty, tier, false, 0L, null) { }

            /// <summary>带 A311 那两个新量的完整 ctor（三参那个转调到这里 ⇒
            /// 生成脚本那 89 行**一个字都不用改**）。</summary>
            public RewardSpec(string id, int qty, int tier, bool isEphemeral, long ephemeralMs,
                              RewardConversion? convertedInto)
            {
                Id = id; Quantity = qty; Tier = tier;
                IsEphemeral = isEphemeral; EphemeralMs = ephemeralMs; ConvertedInto = convertedInto;
            }
        }

        /// <summary>🆕 **2026-10-11（A311）**：原版 `RewardInfo.convertedInto` 的替身 —— 那是一个
        /// **递归的 `RewardInfo`**（`RewardInfo convertedInto` 里还能再有 `convertedInto`）。
        /// ⛔ **不能照抄成 `RewardSpec? ConvertedInto`**：`Nullable&lt;自身&gt;` 是**结构布局循环**
        /// （`CS0523`，编不过）⇒ 单开这个小结构，只留 `SetConvertedItem` **真正读的两样**
        /// （方法体实读：`convertedTo.Quantity` → `PriceDisplay.text`、`convertedTo.Item` → 走 `ICurrency.GetIcon`
        /// 取那张图；`rewardTier` / `isExtraReward` / `ephemeralState` **一个都不读**）。</summary>
        public struct RewardConversion
        {
            /// <summary>= `convertedInto.item.targetId`（查图走它）。</summary>
            public string Id;
            /// <summary>= `convertedInto.quantity`（画成 `PriceDisplay` 的那串数字）。</summary>
            public int Quantity;
            public RewardConversion(string id, int qty) { Id = id; Quantity = qty; }
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
        /// <para>⚠️ **原版这一步走 `ItemDrawer.Draw()`**（`ItemDrawer__Draw.c` 证实它从配置里
        /// `Instantiate` 一个抽屉 prefab 再 `Initialize(item, quantity, …)`）。
        /// 🔴 **2026-10-03 就地更正（铁律 5 · A86）**：本行原来写「**而 `ItemDrawerConfig` SO 与那批抽屉
        /// prefab 本地都没有**（2026-09-23 全库搜 `ItemDrawer*` 资产 **0 命中**）」—— **两半都是假的**：
        /// ① 那张 `ItemDrawerConfig` SO **2026-10-04 已整张解出**（`资料/普查产出_1004/ItemDrawerConfig_映射表.md`
        ///   + 可复现脚本 `工具/read_itemdrawerconfig.py`：20 类型 + 19 档 override + 36 个 GUID，零条靠猜。
        ///   SO 本体在 `sharedassets0.assets`，**没有 type tree** ⇒ **按字段名 grep 对它是无效否定**）；
        /// ② 那批抽屉 prefab **也 dump 得出来**（`python 工具/menu_dump.py bundle_menus_assets_all "Deck Drawer"`
        ///   能把整棵子树摊开：矩形 / `act` / 组件类名 / sprite 名）。
        /// **错因** = 拿「按字段名 grep 0 命中」当成了「本地没有」（**没找到 ≠ 不存在**）。
        /// ⇒ 真正成立的只有**下半句**：下表（id → **图**）是**我们建的** —— 它给的是**数据层**那点判据，
        /// 与「类型 → 抽屉」那张真表**不是同一件事**（判据全文与更正痕迹见 `Shell/ItemDrawer.cs` 文件头）。</para>
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

        // ============================================================ Premium 战役（13 阵营，本地全在）
        //
        //  🆕 2026-10-27（`E3CMP` · 界面线任务表「`Premium Campaign` 15 个 SO 只抄了 UM 一套」）：
        //  原版**每个阵营一份 `PremiumItem` SO** —— `bundle_menus_assets_all/MonoBehaviour/Premium Campaign <X>.json`
        //  （逐份实读：`id` · `cardArmy` · `itemIcon` · `localizationKey` 全是 `MenuShop/PremiumCampaign`）。
        //  🔴 **是 14 份不是 15**：13 阵营 `PremiumItem` + **1 份 `Premium Campaign Store`**
        //  （`eventId PremiumCampaignStore` = LiveOps 商店定义，**不是** `PremiumItem`，归 `ShopData`）。
        //  判据 → `判据/T4_532与PremiumCampaign_判据.md` §「摘要」第 2 条（那里就地订正过「15」这个数）。
        //  ⚠️ 文件头「本地只有 UM 这一套」说的是**节点表**（`Campaign Node Data*.json` 只有 `UM` 前缀）；
        //     **premium 定义 13 个阵营本地全在** —— 两件事别混。
        //  ⚠️ 图标**不另存一份**：那 13 张就是 `ArmyIcons` 那一族的 `40k_DeckSelection_icon_Faction*`
        //     （逐条比对过：GOF→`…FactionOrks` / GSC→`…Genestealers` / SW→`…SpaceWolves` …）⇒ 走 `ArmyIcons.IconName`。

        /// <summary>一个阵营的 premium 战役定义（= 原版 `PremiumItem` SO 里我们真正用到的那三格）。</summary>
        public struct PremiumItem
        {
            /// <summary>`CardArmy` 值（10…130；= SO 的 `cardArmy`）。</summary>
            public int Army;
            /// <summary>SO 的 `id`（= 商店 `items[].targetId`，`premium_campaign_xx`）。</summary>
            public string Id;
            /// <summary>SO 名（= `m_Name`，只给诊断/tooltip 用）。</summary>
            public string SoName;
        }

        /// <summary>13 条，`Army` / `Id` / `SoName` **逐份从 SO 实读**（见上那段判据）。顺序 = `ArmyIcons` 的序列化顺序。</summary>
        static readonly PremiumItem[] PremiumTable =
        {
            new PremiumItem { Army =  10, Id = "premium_campaign_um",  SoName = "Premium Campaign UM"  },
            new PremiumItem { Army =  20, Id = "premium_campaign_gof", SoName = "Premium Campaign GOF" },
            new PremiumItem { Army =  30, Id = "premium_campaign_ash", SoName = "Premium Campaign ASH" },
            new PremiumItem { Army =  40, Id = "premium_campaign_sau", SoName = "Premium Campaign SAU" },
            new PremiumItem { Army =  50, Id = "premium_campaign_bl",  SoName = "Premium Campaign BL"  },
            new PremiumItem { Army =  60, Id = "premium_campaign_tl",  SoName = "Premium Campaign TL"  },
            new PremiumItem { Army =  70, Id = "premium_campaign_tau", SoName = "Premium Campaign TAU" },
            new PremiumItem { Army =  80, Id = "premium_campaign_sor", SoName = "Premium Campaign SOR" },
            new PremiumItem { Army =  90, Id = "premium_campaign_gsc", SoName = "Premium Campaign GSC" },
            new PremiumItem { Army = 100, Id = "premium_campaign_am",  SoName = "Premium Campaign AM"  },
            new PremiumItem { Army = 110, Id = "premium_campaign_da",  SoName = "Premium Campaign DA"  },
            new PremiumItem { Army = 120, Id = "premium_campaign_ec",  SoName = "Premium Campaign EC"  },
            new PremiumItem { Army = 130, Id = "premium_campaign_sw",  SoName = "Premium Campaign SW"  },
        };

        /// <summary>13 条（自检读口）。</summary>
        public static PremiumItem[] PremiumAll { get { return (PremiumItem[])PremiumTable.Clone(); } }

        /// <summary>阵营名（`CardDef.Faction` 口径）→ premium 定义；认不出 ⇒ `false`（`out` = `default`，⛔ 别拿它当查到了）。</summary>
        public static bool TryPremium(string army, out PremiumItem it)
        {
            it = default(PremiumItem);
            if (string.IsNullOrEmpty(army)) return false;
            for (int i = 0; i < PremiumTable.Length; i++)
            {
                ArmyIcons.Entry e;
                if (!ArmyIcons.TryGet(PremiumTable[i].Army, out e)) continue;
                if (e.Faction == army) { it = PremiumTable[i]; return true; }
            }
            return false;
        }

        /// <summary>阵营名 → premium 那条的**图标** sprite 名（`ArmyIcons` 那一族）；认不出 ⇒ **`null`**
        /// （⛔ 别兜底成某个阵营的图 —— 那会把「这一格没有」画成别人的徽记）。</summary>
        public static string PremiumIcon(string army)
        {
            PremiumItem it;
            return TryPremium(army, out it) ? ArmyIcons.IconName(it.Army) : null;
        }

        // ============================================================ 进度存档（`SaveKeys.ActiveCampaign`）
        //
        //  🆕 2026-10-27（`E3CMP`）：原来 `_base/_prem` **只在内存**（静态构造里 `ResetForTest()`）⇒
        //  退出即丢，`Ach_16`（完成战役）也永远只是「这一趟领完了」。现在落到 `SaveKeys.ActiveCampaign`
        //  （那个键早就登记着、**全仓零使用**，判据 `SaveKeys.cs:68`/`:254`）。
        //  🔴 **按节点 id 存、不按下标**：节点表将来加一格不会把旧档错位。
        //  🔴 **读档只在「会写盘的那一趟」**（`SaveCenter.StoreOn`）：批处理自检 / 探针保持**干净起手态**
        //     —— 与 `SaveCenter.StoreOn` 自己的那条 doc（「② 否则只有真在一局里才允许」）同源。

        /// <summary>`SaveKeys.ActiveCampaign` 的 payload。字段名即 JSON 键（`JsonUtility`）。</summary>
        [Serializable]
        public sealed class SaveBlob
        {
            public int ver = 1;
            /// <summary>选中的阵营（`Selected`）。</summary>
            public string selected;
            /// <summary>领过**基础档**的节点 id。</summary>
            public List<string> baseClaimed = new List<string>();
            /// <summary>领过**高级档**的节点 id。</summary>
            public List<string> premClaimed = new List<string>();
        }

        /// <summary>把盘上的进度并进来（静态构造的最后一步）。**认不出的 id 跳过**（表变过也不炸）。</summary>
        static void LoadFromSave()
        {
            if (!SaveCenter.StoreOn) return;
            SaveBlob b = SaveCenter.Get<SaveBlob>(SaveKeys.ActiveCampaign);
            if (b == null) return;
            if (!string.IsNullOrEmpty(b.selected))
                foreach (var a in Armies) if (a == b.selected) { Selected = a; break; }
            if (b.baseClaimed != null)
                for (int k = 0; k < b.baseClaimed.Count; k++) { int i = IndexOf(b.baseClaimed[k]); if (i >= 0) _base[i] = true; }
            if (b.premClaimed != null)
                for (int k = 0; k < b.premClaimed.Count; k++) { int i = IndexOf(b.premClaimed[k]); if (i >= 0) _prem[i] = true; }
        }

        /// <summary>把当前进度写回盘（`Claim` 成功 / `Select` 之后）。`StoreOn == false` 时它自己会丢弃并出声。</summary>
        /// <returns>🆕 **2026-10-31（`B104_P3`）**：这次算不算成功 —— 落盘门**关着**（批处理 / 自检）⇒ 设计上的空操作
        /// ⇒ `true`；门**开着却写失败** ⇒ `false`（已 `LogError` 出声）。调用方据此**撤销内存改动 / 不报成功**。</returns>
        static bool SaveToSave()
        {
            var b = new SaveBlob { selected = Selected };
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (_base[i]) b.baseClaimed.Add(Nodes[i].Id);
                if (_prem[i]) b.premClaimed.Add(Nodes[i].Id);
            }
            bool ok = SaveCenter.Set(SaveKeys.ActiveCampaign, b);   // 🔴 原来这句的返回值被丢掉 ⇒ 写盘失败也当成功
            if (ok || !SaveCenter.StoreOn) return true;
            UnityEngine.Debug.LogError("[Campaign] 战役进度**没能落盘**（" + (SaveCenter.LastWriteError ?? "`SaveCenter` 没给出原因")
                         + "）⇒ 这次领取 / 选择**不算数**（原版进度由服务器持久；本地写不下去就当没做，⛔ 不假装成功）");
            return false;
        }

        // ============================================================ 会话内解锁（原版 `Collect` 的当场副作用）
        //
        //  原版 `CampaignNode.Collect` 成功那一拍会 `foreach (conn in connections) if (conn.State == locked(0)) { conn.State = unlocked(10); SetNodeStyle(); }`
        //  （`decomp_full/CampaignNode__Collect.c:29-47`，现读那两段 `if (*(int *)(local_50 + 0x38) == 0)`）——
        //  它**当场改写相邻节点**；而 `CampaignTrack.GetNodeState` 重算时**只看基础档列表**
        //  （`CampaignTrack__GetNodeState.c:27-58`）⇒ 只领**高级档**的节点，其后续在**重开窗**后会退回 `locked`。
        //  🔴 **那是原版怪癖②，照抄、⛔ 别「修好」**（调度台 2026-10-29 裁定：凡需拍板一律照原版，含原版 bug）。
        //  我们的 `IsUnlocked` 是纯派生（每次按 `_base[前驱]` 现算）⇒ 不补这一档的话，只领高级档会**当场**就退回
        //  `locked`（比原版更严）⇒ 用 `_sessionUnlocked` 记「本会话内被 `Collect` 当场解锁过的节点」。

        static readonly bool[] _sessionUnlocked = new bool[Nodes.Length];

        /// <summary>原版 `CampaignNode.Collect` 末段：把本节点的**相邻节点**里还锁着（`State == locked(0)`）的置为已解锁
        /// （会话内有效）。⚠️ 只写 `_sessionUnlocked`，⛔ **不写** `_base` —— 高级档不得污染基础档列表。</summary>
        static void MarkSuccessorsUnlocked(int i)
        {
            int[] nx = Nodes[i].Next;
            for (int k = 0; k < nx.Length; k++)
            {
                int n = nx[k];
                if (n >= 0 && n < _sessionUnlocked.Length) _sessionUnlocked[n] = true;
            }
        }
    }
}
