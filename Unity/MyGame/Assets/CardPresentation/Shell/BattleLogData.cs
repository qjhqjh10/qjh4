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
//     —— **实际 2026-09-27 就接了**：`Battle/BattleDriver.cs` 的 `RecordBattleLog()`（结算处）调 `RecordBattleLog()`，
//     实现在 `BattleDriver.cs` 的 `BattleLogData`（里面 `BattleLogData.Add(new BattleLogData.Match{…})`）。
//     **错因**：这句是 2026-09-26 建文件时写的，第二天接线后**没人回头改这句**。
//     ⇒ **列表不再恒空**（本机打过局之后就有记录）。⚠️ 仍成立的那半：**表默认是空的**（不编假数据，见上面用户口径）。
//   · **`matchType` → 模式文案的映射查不到**（原版那条是服务端本地化键）⇒ 我们直接存字符串。
//   · 原版 `BattleLogMatch` 还有 `season` / `ownExp` / `enemyExp` / `ownTotalExp` / `botAltid` /
//     `replaysVersion` 等字段 —— **行里没有对应的格子**（普查 §B·2 那棵树只看得到这些）⇒ 不建。
//
// ---- 🆕 2026-10-24（E13）：**落盘 + 录像兜底** ----
// 🔴 原文这里写「**纯内存**」—— 那句**已过期**（就地订正，铁律 5）：现在这张表**落盘**。
//   · 为什么：原版的表在服务器，重启后仍在；我们纯内存 ⇒ **关掉游戏再进来，历史页就空了**
//     （而录像文件其实还躺在 `persistentDataPath/WarpforgeReplays/` 里，只是没人列它们）。
//   · 形状照 `Shell/DailyData.cs` 那一套（独立小 JSON + `OverrideDir` 改写口 + 批处理不碰真档）：
//     `persistentDataPath/WarpforgeBattleLog/battlelog.json`。
//   · **留多少条 = `ReplayStore.MaxKept`（50）** —— 口径只写一处，⛔ 别另立一个数。
//     （对局记录与录像一一对应：每局记录一条、录一份；两边都留 50 才不会「有记录没录像」。）
//   · 🔴 **仍然是我们自建、不是复刻**（原版由服务器写、按赛季清理）。
//   · 🆕 `DisplayRows()` = **给页面用的那一份**：本表 + 「盘上有录像但本表没有对应记录」的兜底行
//     —— 历史项丢了（老存档 / 记录被清 / 录像还在）时，玩家**仍然看得到并点得动**那些录像。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;          // `CardDatabase` / `CardDef`（`ResolveArmy` 用；同 `Shell/CollectionWindow.cs` 的口径）

namespace CardPresentation
{
    /// <summary>对局历史（本地）。**落盘**在 `persistentDataPath/WarpforgeBattleLog/battlelog.json`
    /// （🆕 2026-10-24 E13 —— 原来纯内存，见文件头那一节）。</summary>
    public static class BattleLogData
    {
        /// <summary>一局的结果（原版三态：胜 / 负 / 平）。</summary>
        public enum Outcome { Victory = 0, Defeat = 1, Draw = 2 }

        /// <summary>一行（= 原版 `BattleLogMatch` 里**我们渲得出来的那部分**）。
        /// <para>⚠️ `[System.Serializable]` 是**落盘要的**（`JsonUtility` 只认标了它的自定义类）。</para></summary>
        [System.Serializable]
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

            /// <summary>🆕 **2026-10-19（界面线尾巴 · `MatchLogRow` 那一格阵营徽记）**：
            /// **我方阵营**（原版 `CardArmy` 枚举值，10…130；`0` = 没记录/认不出）。
            /// <para>判据 = 原版**不存 army，存的是督军名**、画行时现解：`BattleLogPlayerDisplay.Initialize`
            /// 拿 `playerData.Hero` 去 `PlayerDataManager.GetHeroCard` 取 hero 卡，再读它 `+0x2c` 的
            /// `army` 字段喂 `ArmyIconsSO.GetArmyIcon`（`d:/2/tools/decomp_full/BattleLogPlayerDisplay__Initialize.c`）。
            /// ⇒ 本表加这一格只是把**那一跳的结果**缓存下来；填法见 <see cref="Add"/>（按督军名反解）。</para></summary>
            public int Army;
            /// <summary>🆕 **2026-10-19**：**敌方阵营**（同上；行右半边那一格）。
            /// ⚠️ 审计原文只写了「加 `Army` 字段」—— 但原版那一格是**每边各一张**（`Player Info` / `Enemy Info`
            /// 各一份 `Score Icon (1)`）⇒ 多这一格是把同一条判据落到另一半，⛔ 不是第二套口径。</summary>
            public int EnemyArmy;

            /// <summary>🆕 2026-09-27：**这一局的本地录像文件名**（在 `ReplayStore.Dir` 下；空 = 没录上）。
            /// 🔴 **这是加功能、不是复刻** —— 原版回放整套在服务器（判据 → `资料/普查产出_0927/回放_入口与数据链.md`）。
            /// 点行上那颗 `ReplayButton` 就是播它（见 `Shell/MatchLogRow.cs` 的 `OnReplay`）。</summary>
            public string ReplayFile;

            /// <summary>🆕 **2026-10-24（E13）**：这一行是**从盘上的录像文件兜底造出来的**
            /// （本表里没有对应记录 —— 见 <see cref="DisplayRows"/>）。
            /// <para>用途：分辨「真打过的记录」与「只剩录像文件的行」（后者没骷髅数/没玩家名，
            /// 钉住它也没有意义）。⚠️ 它**不落盘**是没意义的担心 —— 兜底行本来就只在内存里活一帧。</para></summary>
            public bool FromDisk;
        }

        static readonly List<Match> _all = new List<Match>();

        /// <summary>全部记录（**新的在前** —— 原版是服务器的列表顺序，我们按「最近一局在最上面」排）。
        /// 🔶 自己设计（非原版）：战报列表「最近一局在最上面」的排序 · 原版是服务器的列表顺序（原版判据拿不到） · TODO(谁定的)｜原注：⚠️ 这是**本表原文**；页面要的那一份（含录像兜底行）走 <see cref="DisplayRows"/>。</summary>
        public static List<Match> All { get { return _all; } }
        public static int Count { get { return _all.Count; } }

        /// <summary>记一局。**唯一写点** = `BattleDriver` 结算那一段的 `RecordBattleLog()`
        /// （2026-09-27 就接了 —— ⛔ 别把它当成「还没人写」）。
        /// 🆕 **2026-10-19**：写进来的这一条会先**补齐两边阵营**（`Army` / `EnemyArmy` 还空着时）——
        /// 判据/为什么按「督军名」反解 → <see cref="ResolveArmy"/>。
        /// 🆕 **2026-10-24（E13）**：写完**落盘**（见下面「本地存档」那一节）。</summary>
        public static void Add(Match m)
        {
            if (m == null) return;
            EnsureLoaded();
            if (m.Army == 0 && !string.IsNullOrEmpty(m.OwnHeroName)) m.Army = ResolveArmy(m.OwnHeroName);
            if (m.EnemyArmy == 0 && !string.IsNullOrEmpty(m.EnemyHeroName)) m.EnemyArmy = ResolveArmy(m.EnemyHeroName);
            _all.Insert(0, m);
            Trim();
            SaveIfAuto();
        }

        /// <summary>🆕 **2026-10-19**：**督军显示名 → 阵营**（`CardArmy` 值；认不出 ⇒ `0`）。
        ///
        /// <para>🔴 **这不是我们发明的口径 —— 它就是原版 `BattleLogPlayerDisplay.Initialize` 里那一跳**：
        /// `PlayerDataManager.GetHeroCard(playerData.Hero)` → hero 卡 `+0x2c` 的 `army`
        /// → `ArmyIconsSO.GetArmyIcon`（`d:/2/tools/decomp_full/BattleLogPlayerDisplay__Initialize.c`；
        /// `BattleLogMatch` 里**没有** army 字段，见 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleLogMatch.cs`）。
        /// 我们这一侧的差别只有「卡池 = `CardDatabase.Load()`」和「记录里存的是**显示名**」⇒
        /// 三个名字都认：`CardDef.Name`（英文资产名）/ `CardDef.NameZh` / `CardText.Name(c)`（当前语档）。</para>
        /// <para>⚠️ 只认 `Type == "hero"`（原版 `GetHeroCard` 的语义）；池内 56 位督军**英文名与中文名都没有重名**
        /// （2026-10-19 现核 `cards_engine.json`）⇒ 按名反解无歧义。认不出 ⇒ 返回 `0`（调用方**保留原图 + 出声**）。</para></summary>
        public static int ResolveArmy(string heroName)
        {
            if (string.IsNullOrEmpty(heroName)) return 0;
            var pool = CardDatabase.Load();
            if (pool == null) return 0;
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c == null || c.Type != "hero") continue;
                if (c.Name == heroName || c.NameZh == heroName || CardText.Name(c) == heroName)
                    return ArmyIcons.Id(c.Faction);
            }
            return 0;
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
            EnsureLoaded();
            if (_all.Count == 0) { UnityEngine.Debug.LogWarning("[BattleLog] 没有对局记录可挂录像：" + fileName); return; }
            _all[0].ReplayFile = fileName;
            SaveIfAuto();        // 🆕 2026-10-24（E13）：挂上录像也是**状态变更**，跟 `Add` 一样要落盘
        }

        /// <summary>自检用：清空**内存里的表**（与 `ForgeData.ResetForTest` / `CampaignData.ResetForTest` 同族）。
        /// ⚠️ **不碰盘**（要清盘走 <see cref="DeleteStoreForTest"/>）—— 与 `DailyData.ResetForTest` 同一分工。</summary>
        public static void ResetForTest() { _all.Clear(); }

        // ============================================================ 🆕 2026-10-24（E13）本地存档（唯一一份）
        //
        // 🔴 形状照 `Shell/DailyData.cs` 那一套（⛔ 别自创第二种写法）：
        //   独立目录 + 一个小 JSON + `OverrideDir`（自检改写口）+ **批处理不碰玩家真档**。
        // ⚠️ 原版这张表**在服务器**（`PlayerDataManager.battleLogData`）⇒ 这一层是**我们自建的等价物**。

        /// <summary>落盘目录名（`persistentDataPath` 下，跟 `ReplayStore.DirName` 同理）。</summary>
        public const string StoreDirName = "WarpforgeBattleLog";
        /// <summary>落盘文件名。⚠️ **文件名里不带路径分隔符**（`ReplayStore` 那条纪律）。</summary>
        public const string StoreFileName = "battlelog.json";

        /// <summary>自检用的**目录改写口**：设了就用它，不设就走玩家目录 —— 与 `ReplayStore.OverrideDir`
        /// / `DailyData.OverrideDir` 同一套路、同一个理由（**自检不该动玩家的真存档**）。</summary>
        public static string OverrideDir;

        static string StoreDir
        {
            get
            {
                return string.IsNullOrEmpty(OverrideDir)
                     ? System.IO.Path.Combine(Application.persistentDataPath, StoreDirName)
                     : OverrideDir;
            }
        }

        /// <summary>存档文件的**完整路径**（日志里要打出来 —— 编辑器里它在 `AppData` 深处，不好找）。</summary>
        public static string StorePath { get { return System.IO.Path.Combine(StoreDir, StoreFileName); } }

        /// <summary>🔴 **门：这一趟允许碰盘吗**（与 `DailyData.StoreOn` 逐条同义）。
        /// ① `OverrideDir` 设了 ⇒ 允许（自检注入的临时档）；② 否则只有真在一局里才允许
        /// （`isPlaying &amp;&amp; !isBatchMode`）⇒ **11 条批处理自检一律不碰玩家真档**（结构上保证）。</summary>
        static bool StoreOn
        {
            get { return !string.IsNullOrEmpty(OverrideDir) || (Application.isPlaying && !Application.isBatchMode); }
        }

        /// <summary>盘上那一份（字段名就是 JSON 键 ⇒ 改名 = 换版本）。</summary>
        [System.Serializable]
        class StoreDto
        {
            /// <summary>格式版本。**改结构就 +1**；读到不认识的版本 ⇒ **出声 + 不读它**（按空表走），
            /// ⛔ 别硬解 —— 缺字段会被 `JsonUtility` 静默读成默认值（`ReplayStore.Load` 同一条口径）。</summary>
            public int version = 1;
            public Match[] matches;
        }

        static bool _loaded;
        static bool _storeOffSaid;

        /// <summary>🔴 读盘的唯一入口 = 静态构造函数（同 `DailyData`：C# 保证本类任何成员被碰到之前跑完）。</summary>
        static BattleLogData() { EnsureLoaded(); }

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;                       // 先置位：读失败也不重试（避免每次进页面刷一遍日志）
            if (!StoreOn)
            {
                if (!_storeOffSaid)
                {
                    _storeOffSaid = true;
                    Debug.Log("[BattleLog] 落盘**关着**（没设 `OverrideDir`，且这一趟不在真的一局里）⇒ 本进程"
                            + "不碰玩家真档，对局历史只活在内存里。⚠️ 自检要走盘上那一份：先 "
                            + "`BattleLogData.OverrideDir = <临时目录>` 再 `DeleteStoreForTest()` + `ReloadForTest()`。");
                }
                return;
            }
            LoadFromDisk();
        }

        static void LoadFromDisk()
        {
            var path = StorePath;
            // 🆕 Step 4 双写期过渡读法：**先中央档、再旧档**（`ReadThroughLegacy` 内部判落盘门 ⇒
            // 批处理/自检照旧只读旧档那一份）。⛔ 别删旧读。
            string text = SaveCenter.ReadThroughLegacy(SaveKeys.BattleLogData, path);
            if (text == null) return;                          // 第一次跑：空表就是出厂态，不吵
            try
            {
                var dto = JsonUtility.FromJson<StoreDto>(text);
                if (dto == null) { Debug.LogWarning("[BattleLog] 存档解不出内容（" + path + "）⇒ 按空表走"); return; }
                if (dto.version != new StoreDto().version)
                {
                    // ⛔ 不硬解（缺字段 = 拿默认值冒充真值）；**出声**，下一拍落盘会盖掉它
                    Debug.LogWarning("[BattleLog] 存档版本 " + dto.version + " 不认识（本代码只认 "
                                   + new StoreDto().version + "）⇒ 不读，按空表走：" + path);
                    return;
                }
                var list = dto.matches;
                if (list == null) return;
                for (int i = 0; i < list.Length; i++)
                {
                    var m = list[i];
                    if (m == null) continue;
                    // 阵营那一格老档可能没有 ⇒ 按督军名现解（与 `Add` 同一判据，⛔ 不写第二份换算）
                    if (m.Army == 0 && !string.IsNullOrEmpty(m.OwnHeroName)) m.Army = ResolveArmy(m.OwnHeroName);
                    if (m.EnemyArmy == 0 && !string.IsNullOrEmpty(m.EnemyHeroName)) m.EnemyArmy = ResolveArmy(m.EnemyHeroName);
                    _all.Add(m);
                }
                Trim();
                Debug.Log("[BattleLog] 读回 " + _all.Count + " 条对局记录（`" + path + "`）");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[BattleLog] 读存档失败（" + path + "）：" + e.Message + " ⇒ 按空表走");
            }
        }

        /// <summary>超出上限就从**尾部**（最旧）删 —— 上限 = <see cref="ReplayStore.MaxKept"/>（口径只写一处）。</summary>
        static void Trim()
        {
            while (_all.Count > ReplayStore.MaxKept) _all.RemoveAt(_all.Count - 1);
        }

        /// <summary>写盘（正式入口）。批处理/编辑器非 Play 由 <see cref="SaveIfAuto"/> 挡掉。</summary>
        public static void SaveNow()
        {
            EnsureLoaded();
            if (!StoreOn) return;
            var dto = new StoreDto();
            dto.matches = _all.ToArray();
            try
            {
                string json = JsonUtility.ToJson(dto, true);      // 带缩进（逐字段比较不受影响：不比空白）
                // 🆕 2026-10-25（执行代理 R4 · 存档 Step 4「双写期」）：**不再自己写盘** —— 走
                // `SaveCenter.SaveThroughLegacy`：中央档（带 `_v` 信封）+ 旧档 `battlelog.json`（**不带 `_v`**）各一份。
                // · 上面 `if (!StoreOn) return;` 已挡住批处理（一个字节都不碰玩家真档）；
                // · 切换口内部再判一次 `SaveCenter.StoreOn`：关着 ⇒ 退回「只写旧档」= 与切换前逐字节同
                //   （自检只注入 `OverrideDir` 的那一档照旧能验落盘）。
                string werr;
                if (!SaveCenter.SaveThroughLegacy(SaveKeys.BattleLogData, StorePath, json, out werr))
                {
                    // 不许静默：写不进去要说出来（原版这一步是服务器写的）
                    Debug.LogWarning("[BattleLog] 存档写盘失败（" + StorePath + "）：" + werr);
                }
            }
            catch (System.Exception e)
            {
                // 不许静默：写不进去要说出来（原版这一步是服务器写的）
                Debug.LogWarning("[BattleLog] 存档写盘失败（" + StorePath + "）：" + e.Message);
            }
        }

        static void SaveIfAuto() { if (StoreOn) SaveNow(); }

        /// <summary>🆕 自检专用：强制写盘（批处理下也能验落盘 —— 只有设了 `OverrideDir` 才真写）。</summary>
        public static void SaveNowForTest() { EnsureLoaded(); SaveNow(); }

        /// <summary>🆕 自检专用：清空内存 + 从盘上重读（验「写 → 读回」往返）。</summary>
        public static void ReloadForTest() { _loaded = true; _all.Clear(); LoadFromDisk(); }

        /// <summary>🆕 自检专用：删掉盘上那一份。**没设 `OverrideDir` 时拒绝执行**（否则删的是玩家真档 —— 硬挡，同 `DailyData`）。</summary>
        public static void DeleteStoreForTest()
        {
            if (string.IsNullOrEmpty(OverrideDir))
            {
                Debug.LogError("[BattleLog] 自检：**拒绝删档** —— 没设 `OverrideDir` 时删的就是玩家的真存档。"
                             + "先 `BattleLogData.OverrideDir = <临时目录>`。");
                return;
            }
            try { if (System.IO.File.Exists(StorePath)) System.IO.File.Delete(StorePath); }
            catch (System.Exception e) { Debug.LogWarning("[BattleLog] 删档失败：" + e.Message); }
            // 🆕 Step 4 双写期：中央档那一份也一起清 —— 否则「删了旧档再 `ReloadForTest`」会从中央档
            // 把上一趟的记录读回来（读口是先中央后旧档）。走 `SaveCenter.Delete`（**不真删**：改名到 `.del-*`）。
            if (SaveCenter.StoreOn && SaveCenter.Exists(SaveKeys.BattleLogData)) SaveCenter.Delete(SaveKeys.BattleLogData);
        }

        // ============================================================ 🆕 2026-10-24（E13）：页面用的那一份（含录像兜底）
        //
        // 🔴 **为什么要兜底**：本表是**我们自建**的落盘（原版在服务器），它会因为「老存档 / 换了机器 /
        //    记录被裁掉」而缺行 —— 而**录像文件还在** `persistentDataPath/WarpforgeReplays/` 里。
        //    照原版语义「历史项丢了 = 什么都看不到」的话，玩家那些真打过的录像就**点不到了**。
        //    ⇒ 页面读这一份：本表在前，**盘上有、本表没有对应行**的录像接在后面（各自可播）。
        // ⚠️ 只补**读得出来的**录像：`ReplayStore.Load` 返回 null（版本不认识 / 文件坏了）⇒ **如实出声并跳过**，
        //    ⛔ 不造一条「名字和结果都靠猜」的假行。

        public static List<Match> DisplayRows()
        {
            EnsureLoaded();
            var rows = new List<Match>(_all);
            rows.AddRange(FallbackRows());
            return rows;
        }

        /// <summary><see cref="DisplayRows"/> 的**后半段**：只算「盘上有录像、本表没有对应记录」那几条。
        /// <para>🔴 单独开这一口是为了 `Shell/BattleLogTab.cs`：它建行时要**本表实时**（滚轮重画、
        /// 自检喂数据都走 `RebuildRows`），只有**读盘那一半**是贵的 ⇒ 那一半按页缓存（`_extra`），
        /// 本表每次现读。⛔ 别把整个 `DisplayRows()` 塞进滚动重画里（那会每滚一格读 50 个 JSON）。</para></summary>
        public static List<Match> FallbackRows()
        {
            EnsureLoaded();
            var rows = new List<Match>();
            // 🔴 **门（与落盘同一道 `StoreOn`）**：批处理自检没注入 `OverrideDir` 时**不读玩家的真录像目录** ——
            //    否则自检现场会读到开发机上真打过的录像，把「本页空态」那几条断言弄红（如实记：
            //    这是 2026-10-24 自查出来的：`Editor/MainMenuScene.cs` 那一段**没有**设 `ReplayStore.OverrideDir`，
            //    而 `Application.persistentDataPath/WarpforgeReplays/` 里有真录像）。
            //    ⚠️ 真 Play / 注入了 `OverrideDir` 的自检 ⇒ 照常兜底（两种验收路都能看到旧录像）。
            if (!StoreOn) return rows;
            var seen = new HashSet<string>();
            for (int i = 0; i < _all.Count; i++)
                if (!string.IsNullOrEmpty(_all[i].ReplayFile)) seen.Add(_all[i].ReplayFile);
            var files = ReplayStore.List();                  // 新 → 旧
            for (int i = 0; i < files.Count; i++)
            {
                if (seen.Contains(files[i])) continue;
                var rec = ReplayStore.Load(files[i]);        // 失败时它自己出声（带完整路径与版本）
                if (rec == null)
                {
                    Debug.LogWarning("[BattleLog] 盘上有录像 `" + files[i] + "`，但**读不出来**（版本不认识或文件坏了）"
                                   + "⇒ 不列这一条（⛔ 不造一条名字/结果靠猜的行）");
                    continue;
                }
                var m = new Match
                {
                    Result = rec.result == 1 ? Outcome.Defeat : rec.result == 2 ? Outcome.Draw : Outcome.Victory,
                    OwnHeroName = rec.myHero ?? "", EnemyHeroName = rec.foeHero ?? "",
                    OwnName = rec.start != null && !string.IsNullOrEmpty(rec.start.myName)
                            ? rec.start.myName : ProfileData.PlayerName,
                    EnemyName = rec.start != null ? (rec.start.foeName ?? "") : "",
                    PlayerClan = "", EnemyClan = "",
                    // 骷髅数 / 评分不在录像头里（`ReplayRecord` 没记过）⇒ **留 0 / 空**，⛔ 不猜
                    OwnSkulls = 0, EnemySkulls = 0,
                    OwnScore = "", EnemyScore = "",
                    Mode = rec.start != null ? PlayModeNames.Name(PlayModeNames.Parse(rec.start.mode)) : "",
                    Pinned = false,
                    RecordingIndex = -1,
                    ReplayFile = files[i],
                    FromDisk = true,
                };
                if (!string.IsNullOrEmpty(m.OwnHeroName)) m.Army = ResolveArmy(m.OwnHeroName);
                if (!string.IsNullOrEmpty(m.EnemyHeroName)) m.EnemyArmy = ResolveArmy(m.EnemyHeroName);
                rows.Add(m);
            }
            return rows;
        }

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
