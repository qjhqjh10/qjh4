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
//   · **记录怎么产生** —— 原版是服务器在每局结束后写；**我们由结算处自己写**。
//     ⚠️ **2026-10-18 更正（铁律 5）**：原来这里写「我们**目前没有人在写**（⇒ 列表恒空）……**等用户拍板**」
//     —— **实际 2026-09-27 就接了**：`Battle/BattleDriver.cs:12807`（结算处）调 `RecordBattleLog()`，
//     实现在 `BattleDriver.cs:12844`（里面 `BattleLogData.Add(new BattleLogData.Match{…})`）。
//     **错因**：这句是 2026-09-26 建文件时写的，第二天接线后**没人回头改这句**。
//     ⇒ **列表不再恒空**（本机打过局之后就有记录）。⚠️ 仍成立的那半：**表默认是空的**（不编假数据，见上面用户口径）。
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
            /// <summary>回放编号（原版 `recordingIndex`，`+0x68`）—— 原版是**服务器分配的**，
            /// 我们**没有那个编号** ⇒ 恒 `-1`（**别拿它假装有**）。「这一局有没有录像」看 `ReplayFile`。</summary>
            public int RecordingIndex;

            /// <summary>🆕 2026-09-27：**这一局的本地录像文件名**（在 `ReplayStore.Dir` 下；空 = 没录上）。
            /// 🔴 **这是加功能、不是复刻** —— 原版回放整套在服务器（判据 → `资料/普查产出_0927/回放_入口与数据链.md`）。
            /// 点行上那颗 `ReplayButton` 就是播它（见 `Shell/MatchLogRow.cs` 的 `OnReplay`）。</summary>
            public string ReplayFile;
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

        /// <summary>🆕 2026-09-27：把刚存下的**录像文件名**挂到**最新那条**记录上。
        /// 调用点紧跟 `Add`（`BattleDriver` 的结算那一段，中间不会有别人插队）⇒ 认「第 0 条」是安全的。
        /// ⚠️ 不写第二份状态：**「这一局有没有录像」只认 `Match.ReplayFile`**。</summary>
        public static void AttachReplay(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                // 出声（红线）：录了却存不下，玩家会以为有回放可看
                UnityEngine.Debug.LogWarning("[BattleLog] 这一局的录像**没存下来**（文件名为空）—— 那一行点「回放」会如实说没有");
                return;
            }
            if (_all.Count == 0) { UnityEngine.Debug.LogWarning("[BattleLog] 没有对局记录可挂录像：" + fileName); return; }
            _all[0].ReplayFile = fileName;
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
