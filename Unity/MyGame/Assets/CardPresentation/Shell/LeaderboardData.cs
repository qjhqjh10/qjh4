// LeaderboardData.cs — 排行榜的**本地数据**（默认**一条都没有**）
//
// ============================ 这个文件为什么存在 ============================
// 🔴 **原版的排行榜数据全在服务器**（`LeaderboardManager` / 各 `RankingDataProvider` 走 PlayFab），
//    本地**一条都取不到**。用户 2026-09-26 的口径是「**有什么复刻什么，具体的数据和排名这些可以空着**」
//    ⇒ 界面照原版建、**数据留空**（`LeaderboardWindow` 在空数据下就是原版那个「一行都没有」的样子 ——
//    四棵榜**都没有空态节点**，我们**不自己造一个**）。
//
// 那这个表有什么用：🔴 **给自检一个能证明「行画得对」的入口** ——
//    没有它的话，「行族 builder」这段代码在本地永远走不到，等于没做也看不出来。
//    用法照 `Shell/BattleLogData.cs` 与 `BattleLogPopup.RebuildForTest()` 那套先例：
//    自检 `InjectForTest(...)` → `RebuildForTest()` → 断行数与几何 → `ClearForTest()`。
//
// ⚠️ **它不是「本地进度模拟」** —— 那是另一个口径问题（要不要给玩家攒本地分），
//    记在 `项目任务.md` §三 第 18 条「同一轮查出、还没做的四件」第 ②，**等用户拍板**。
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>排行榜的本地数据表（**默认全空**；只有自检会往里塞）。</summary>
    public static class LeaderboardData
    {
        // 🔴 **key 用「榜 + 页签」两个维度**（原版就是这么分的：每个页签挂一个 provider，
        //    provider 各自向自己的服务器榜要数据）。别压成一个维度 —— 四棵榜的页签不重样。
        static readonly Dictionary<string, List<LeaderboardRowData>> _injected =
            new Dictionary<string, List<LeaderboardRowData>>();

        static readonly List<LeaderboardRowData> Empty = new List<LeaderboardRowData>();

        static string Key(LeaderboardKind kind, LeaderboardTab tab) { return kind + "/" + tab; }

        /// <summary>这一页的行。**本地默认 = 空表**（原版读服务器）。</summary>
        public static List<LeaderboardRowData> Rows(LeaderboardKind kind, LeaderboardTab tab)
        {
            List<LeaderboardRowData> l;
            return _injected.TryGetValue(Key(kind, tab), out l) ? l : Empty;
        }

        /// <summary>本地有没有任何数据 —— **窗口用它决定要不要出声**（不许静默）。</summary>
        public static bool HasAny { get { return _injected.Count > 0; } }

        /// <summary>只给自检用：塞一批行进去（证明行族 builder 画得对）。</summary>
        public static void InjectForTest(LeaderboardKind kind, LeaderboardTab tab, List<LeaderboardRowData> rows)
        {
            _injected[Key(kind, tab)] = rows ?? new List<LeaderboardRowData>();
        }

        /// <summary>只给自检用：清掉（**必须清** —— 否则下一条断言量到的是上一条的数据）。</summary>
        public static void ClearForTest() { _injected.Clear(); }
    }
}
