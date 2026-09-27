// BattleLogData.cs — 档案窗「Battle Log」页的**本地对局记录**（单机版）
//
// ============================ 这是什么、不是什么 ============================
// 🔴 原版这一页的数据**在服务器**：`BattleLogTab.OnOpen` 读 `PlayerDataManager.battleLogData`
//    （单例字段 `+0x2c0`，一串 `BattleLogMatch`），逐条 `Instantiate(logPrefab, holder)`。
//    出处 → `资料/普查产出_0927/档案窗_BattleLog与页签按钮.md` §B·3（伪码在那里）。
//    ⇒ **本地一条都取不到**（原版已关服）。
// ⇒ 照「日常 / 锻造厂 / 战役」那三层的先例（`DailyData` / `ForgeData` / `CampaignData`）：
//    **数据由我们自建**，并在这里逐条标明「我们挑的」。
//    用户 2026-09-26 定的口径：「有什么复刻什么，**具体的数据和排名这些可以空着**」
//    ⇒ 这张表**默认是空的**（**不编对局**）—— 页面照样是空态。**别为了「好看」塞假数据。**
//
// ✅ **照原版的**：
//   · 一条记录里**行渲得出来的那些字段**（名字照 `BattleLogMatch`）：`ownHeroName` / `ownName` /
//     `playerClan` / `ownSkulls` / `pinned`(+0x80) / `recordingIndex`(+0x68) / `matchType`(+0x70)；
//   · **钉住色两个常量**：未钉 `(1,1,1,1)` / 已钉 `(0,1,0,1)`（绿）—— `工具/read_literal.py`
//     从 `GameAssembly.dll` 实读（`0x1834b2e50..5c` / `0x1834b2c00..0c`）；
//   · **结果只有胜 / 负 / 平三态**（原版 `victoryKey`/`defeatKey`/`drawKey` =
//     `Battle/BattleEnd/{Victory,Defeat,Draw}`）⇒ 文案走我们工程**已有的那一份**
//     `CardText.Phrase("VICTORY"/"DEFEAT"/"DRAW")`（与结算面板同一个源，别另写一份）。
//
// ❌ **我们挑的 / 没做**：
//   · **记录怎么产生** —— 原版是服务器在每局结束后写；我们**目前没有人在写**（⇒ 列表恒空）。
//     要接就接在 `BattleDriver` 的结算处（`EndPanel.Show` 那一段），**等用户拍板**。
//   · **`matchType` → 模式文案的映射查不到**（原版那条是服务端本地化键）⇒ 我们直接存字符串。
//   · 原版 `BattleLogMatch` 还有 `season` / `ownExp` / `enemyExp` / `ownTotalExp` / `botAltid` /
//     `replaysVersion` 等字段 —— **行里没有对应的格子**（普查 §B·2 那棵树只看得到这些）⇒ 不建。
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>对局历史（本地）。**纯内存**，和另外三个本地数据源同一套路。</summary>
    public static class BattleLogData
    {
        /// <summary>一局的结果（原版三态：胜 / 负 / 平）。</summary>
        public enum Outcome { Victory = 0, Defeat = 1, Draw = 2 }

        /// <summary>一行（= 原版 `BattleLogMatch` 里**我们渲得出来的那部分**）。</summary>
        public class Match
        {
            public Outcome Result;
            /// <summary>两边的督军名（原版 `ownHeroName` / `enemyHeroName`）。</summary>
            public string OwnHeroName, EnemyHeroName;
            /// <summary>两边的玩家名（原版 `ownName` / `enemyName`）。</summary>
            public string OwnName, EnemyName;
            /// <summary>两边的联盟名（原版 `playerClan` / `enemyClan`）—— 没有就空串（那一格本来就常是空的）。</summary>
            public string PlayerClan, EnemyClan;
            /// <summary>两边的骷髅数（原版 `ownSkulls` / `enemySkulls`），显示成 `x3`。</summary>
            public int OwnSkulls, EnemySkulls;
            /// <summary>两边那两格评分文字（原版 `ownGlobalRating` / `enemyGlobalRating` + `RankedRating`）。
            /// ⚠️ 原版那是**数字 + 段位图标**；本地没有段位数据 ⇒ 我们存**整串文字**（空 = 那一格空着）。</summary>
            public string OwnScore, EnemyScore;
            /// <summary>模式那行（原版 `matchType` → 本地化文案；**映射表本地查不到** ⇒ 直接存文案）。</summary>
            public string Mode;
            /// <summary>钉住没有（原版 `pinned`，`+0x80`）。</summary>
            public bool Pinned;
            /// <summary>回放编号（原版 `recordingIndex`，`+0x68`）—— 我们的回放还没做（任务 §三 第 18 条 第 6 件）。</summary>
            public int RecordingIndex;
        }

        static readonly List<Match> _all = new List<Match>();

        /// <summary>全部记录（**新的在前** —— 原版是服务器的列表顺序，我们按「最近一局在最上面」排）。</summary>
        public static List<Match> All { get { return _all; } }
        public static int Count { get { return _all.Count; } }

        /// <summary>记一局。**唯一写点**（将来接在结算那一段）。</summary>
        public static void Add(Match m)
        {
            if (m == null) return;
            _all.Insert(0, m);
        }

        /// <summary>自检用：清空（与 `ForgeData.ResetForTest` / `CampaignData.ResetForTest` 同族）。</summary>
        public static void ResetForTest() { _all.Clear(); }

        // ---- 结果文案：**走工程已有的那一份**（`CardText.Phrase`，与结算面板同一个源）----
        public static string ResultText(Outcome o)
        {
            return o == Outcome.Victory ? CardText.Phrase("VICTORY")
                 : o == Outcome.Defeat ? CardText.Phrase("DEFEAT")
                 : CardText.Phrase("DRAW");
        }

        // ---- 钉住色：**照原版两个常量**（`工具/read_literal.py` 从 DLL 实读）----
        /// <summary>未钉：`(1,1,1,1)`。</summary>
        public static readonly UnityEngine.Color PinOff = new UnityEngine.Color(1f, 1f, 1f, 1f);
        /// <summary>已钉：`(0,1,0,1)`（绿）—— 原版 `BattleLogItem.SetPinState` 直接写 `pinButton.image.color`。</summary>
        public static readonly UnityEngine.Color PinOn = new UnityEngine.Color(0f, 1f, 0f, 1f);
    }
}
