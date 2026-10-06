// ReplayStore.cs — **本地「录像」的存放与读写**（一局一个文件）
//
// ============================ 这东西是什么 ============================
// 原版的回放**整套在服务器上**（PlayFab CloudScript 430/440/620/650），本地录不了也放不了
// （判据 → `资料/普查产出_0927/回放_入口与数据链.md` §C/§D）。用户 2026-09-27 拍板：
// **我们自己做一份本地的** —— 这是**加功能、不是复刻**（同「结算写本地对局记录」那条）。
//
// 🔴 **录的不是画面，是「动作流 + 起始条件」** —— 与原版 `BattleRecordData` 同一个形状
//    （两条 `PlayerAction` 列表 + `rngNum` + 双方牌组 + 换牌）。为什么这样就够：
//    · 引擎的随机走 `Ctx.Rng`（`System.Random(seed)`，种子定死）⇒ 同样的动作序列消耗同样多次;
//    · **AI 的骰子走 `Ctx.AiRng`，而它只用在「挑哪条动作」上**（`SimpleAI.cs:821` 那一处），
//      `ExecuteAction` 不碰它 ⇒ **把挑出来的动作原样重放即可**，AI 不必重跑。
//    ⇒ **一条动作 = 一个 `MsgAction`**，这正是联机重连重放用的那个结构（`NetApply.Apply` 是唯一的落地实现）。
//
// ---- 存在哪（用户 2026-09-27 问的「专门的文件夹」）----
// `Application.persistentDataPath/WarpforgeReplays/` —— 与 `NetConfig`（`WarpforgeNet.json`）·
// `DeckStore` 同一套（`JsonUtility` + 一个给自检用的 `OverrideDir`）。
// ⚠️ 编辑器里 `persistentDataPath` 在 `C:/Users/<你>/AppData/LocalLow/<公司>/<产品>/` ——
//    **看不见** ⇒ 每次存盘/读盘都 `Debug.Log` **完整路径**（用户要能找得到）。
// ⚠️ 出 player 之后这个路径才会变到真正的存档位置；**这是第一处会写用户数据的地方**，
//    所以：只写这一个目录、文件名里不带路径分隔符、写失败一律出声。
//
// ---- 留多少局（用户 2026-09-27 问的「50 局合不合理、多大」）----
// `MaxKept = 50`，**超出就删最旧的**（按文件名的时间戳排序 —— 文件名自带时间，不靠文件系统时间）。
// 📏 **实测一局多大**见 `ReplayStore.LastSavedBytes`（自检每次打印），别拍脑袋估。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CardPresentation.Net;

namespace CardPresentation
{
    /// <summary>一局录像。**头 = `MsgStart`（种子/模式/双方卡组/战场/名字/先手），身子 = 动作序列**。</summary>
    [Serializable]
    public class ReplayRecord
    {
        /// <summary>格式版本。**将来改结构就 +1**，读到不认识的版本**出声跳过**，别硬解。
        /// 🔴 **2026-10-01：1 → 2** —— 棋盘从「固定 9 格、可留洞」改成**连续无洞**模型
        /// （`RuleEngine/Core/BoardSlots.cs`），**同一串动作在新引擎里会落到不同的格号**
        /// （旧档里「打到 0 号格」现在会夹到最里那一格）⇒ 旧档放出来是**另一局**，必须拒绝而不是硬解。</summary>
        public int version = 2;
        /// <summary>存盘时刻（`yyyy-MM-dd HH:mm:ss`，**本地时间**，只给人看）。</summary>
        public string savedAt = "";
        /// <summary>本机那局的座位（**绝对编号**）—— 放的时候要 `FromStart(start, isHost: mySeat == 0)`。</summary>
        public int mySeat;
        /// <summary>结果，照 `BattleLogData.Outcome`：0 胜 / 1 负 / 2 平。</summary>
        public int result;
        /// <summary>🔴 **结算那一刻的引擎指纹**（`NetProtocol.Fingerprint(Ctx)`）——
        /// 放的时候再算一次对比：**不相等就说明有动作没录全**，回放看到的是**另一局**
        /// （判据与用法 → `BattleDriver.PlayReplay`；这是「不许静默失败」那条红线的落地）。</summary>
        public int finalHash;
        /// <summary>两个督军名（**列表里显示用**；引擎不看它）。</summary>
        public string myHero = "", foeHero = "";
        /// <summary>开局参数（**重建这一局要的全部东西**）。</summary>
        public MsgStart start;
        /// <summary>动作流（含换牌那两条：`kind = 100/101`）。</summary>
        public List<MsgAction> actions = new List<MsgAction>();
        /// <summary>🆕 2026-09-27：**逐条动作之后的引擎指纹**（`trace[i]` = 第 i 条动作落地后的状态）。
        /// 用途只有一个但很关键：**回放时找「第一条对不上的动作」** —— 光看终局指纹只知道「不一样」，
        /// 不知道**从哪一步开始不一样**（2026-09-27 就是靠它把分叉点钉出来的）。
        /// ⚠️ 它让每局多约 `4×N` 字节（N=50 时约 200 字节/局）—— 值。</summary>
        public List<int> trace = new List<int>();
        /// <summary>🔴 **诊断用**（2026-09-27）：每条动作落地后 `ActionLog` 尾那一句。
        /// 光看哈希只能知道「哪一条分叉」，看这一句才知道**引擎当时干了什么不一样的事**。
        /// ⚠️ 正式收口时可以去掉（它让每局多十几 KB）—— 留着的话就当「出问题时的黑匣子」。</summary>
        public List<string> traceLogTail = new List<string>();
        /// <summary>🆕 **诊断用**（2026-09-27）：每条动作落地后的**局面速写**
        /// （`BattleDriver.StateBrief`：双方每格的卡名+血+有没有动过、手牌/牌库/弃牌张数、能量、
        /// 任务点、督军血、问了几次选择、面板答案队列还剩几条）。
        ///
        /// 为什么要它：`trace`（哈希）只能把分叉**定位到第几条**，看不出「哪里不一样」——
        /// 2026-09-27 实测：分叉点那一行光有两个 `int`（`827436913` vs `-23128108`），
        /// 人读不出「录的时候 2 号格站着 `Ravenwing Bikes`、回放时那一格是空的」。
        /// ⚠️ 与 `traceLogTail` 一样是**黑匣子**，正式收口时可以一起去掉（每局约 +5 KB/50 条）。</summary>
        public List<string> traceState = new List<string>();
    }

    /// <summary>本地录像库（一局一个 `.json`）。</summary>
    public static class ReplayStore
    {
        public const string DirName = "WarpforgeReplays";
        public const string Ext = ".json";

        /// <summary>最多留几局（**超出就删最旧的**）。用户 2026-09-27 定的是 **50**。
        /// 📏 一局多大见 `LastSavedBytes`；50 局的总占用 = 50 × 那个数。</summary>
        public const int MaxKept = 50;

        /// <summary>🔴 **黑匣子开关**：开着才逐条记 `traceLogTail` / `traceState`（那两份字符串速写）。
        /// 关着只留 `trace`（每条 4 字节的哈希，够定位到「第几条分叉」）。
        /// 📏 实测：45 条动作 —— **关 9.0 KB / 开 23.1 KB**（差的那十几 KB 全是这两份速写）。
        /// ⇒ **平时关**（真打的录像要省），**自检与查问题时开**。
        /// ⚠️ 开不开**不影响回放能不能演**（`trace` 总是记的）。</summary>
        public static bool VerboseTrace;

        /// <summary>自检用的目录改写口（设了就不碰玩家的真录像）—— 与 `NetConfig.OverridePath` 同一套路。</summary>
        public static string OverrideDir;

        /// <summary>🔴 **跨场景交接**：下一局战场要放哪一份录像。与 `NetPendingBattle.Current` 同一条路数
        /// （`LoadScene` 会清掉普通静态量之外的东西 ⇒ 只能这么带过去）。
        /// ⚠️ 战场那边**取一次就清**（`TakePending`），别留着 —— 不然「放完一局再开新局」会又放一遍。</summary>
        public static ReplayRecord PendingPlay;

        public static ReplayRecord TakePending() { var p = PendingPlay; PendingPlay = null; return p; }

        /// <summary>最近一次写盘的字数（自检与「50 局占多大」都用它，别猜）。</summary>
        public static long LastSavedBytes { get; private set; }
        /// <summary>最近一次写盘的文件名。</summary>
        public static string LastSavedFile { get; private set; }

        /// <summary>录像目录（**日志里会把完整路径打出来** —— 编辑器里它在 AppData 深处，不好找）。</summary>
        public static string Dir
        {
            get
            {
                var d = string.IsNullOrEmpty(OverrideDir)
                      ? Path.Combine(Application.persistentDataPath, DirName)
                      : OverrideDir;
                return d;
            }
        }

        // ---------------------------------------------------------- 写

        /// <summary>存一局。返回文件名（失败返回 null 并**出声**）。存完顺手按 `MaxKept` 修剪。</summary>
        public static string Save(ReplayRecord rec)
        {
            if (rec == null) { Debug.LogError("[Replay] `Save(null)` —— 不存"); return null; }
            try
            {
                Directory.CreateDirectory(Dir);
                string name = FileNameFor(rec);
                string path = Path.Combine(Dir, name);
                string json = JsonUtility.ToJson(rec);           // **紧凑格式**（不 prettyPrint：省一半）
                File.WriteAllText(path, json);
                LastSavedBytes = new System.Text.UTF8Encoding(false).GetByteCount(json);
                LastSavedFile = name;
                Debug.Log($"[Replay] 已存：`{path}`（{LastSavedBytes / 1024f:F1} KB · "
                        + $"{rec.actions.Count} 条动作 · {rec.myHero} vs {rec.foeHero}）");
                TrimToLast(MaxKept);
                return name;
            }
            catch (Exception e)
            {
                // 🔴 写盘失败**必须出声**（红线）—— 玩家以为录上了、其实没有，是最坏的一种静默
                Debug.LogError("[Replay] 写盘失败：" + e.Message + "（目录 " + Dir + "）");
                return null;
            }
        }

        /// <summary>文件名：`时间_我_vs_敌.json`。⚠️ 名字里**只留字母数字**（督军名里可能有空格/斜杠，
        /// 带进去会变成子目录或非法路径）。</summary>
        static string FileNameFor(ReplayRecord rec)
        {
            string t = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return t + "_" + Safe(rec.myHero) + "_vs_" + Safe(rec.foeHero) + Ext;
        }

        static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }

        // ---------------------------------------------------------- 读 / 列 / 删

        /// <summary>目录里全部录像的文件名（**按时间倒序** —— 文件名自带时间戳，不靠文件系统时间）。</summary>
        public static List<string> List()
        {
            var outp = new List<string>();
            try
            {
                if (!Directory.Exists(Dir)) return outp;
                foreach (var f in Directory.GetFiles(Dir, "*" + Ext)) outp.Add(Path.GetFileName(f));
                outp.Sort();
                outp.Reverse();
            }
            catch (Exception e) { Debug.LogError("[Replay] 列目录失败：" + e.Message); }
            return outp;
        }

        public static int Count { get { return List().Count; } }

        /// <summary>读一局。**读不出来返回 null 并出声**（版本不认识 / 文件坏了 / 被删了）。</summary>
        public static ReplayRecord Load(string fileName)
        {
            try
            {
                string path = Path.Combine(Dir, fileName);
                if (!File.Exists(path)) { Debug.LogWarning("[Replay] 没有这个文件：" + path); return null; }
                var rec = JsonUtility.FromJson<ReplayRecord>(File.ReadAllText(path));
                if (rec == null) { Debug.LogWarning("[Replay] 解不出内容：" + path); return null; }
                // 🔴 **2026-10-01：改成比 `new ReplayRecord().version`** —— 原来写死的那个 `1`
                //    在棋盘模型换代时**不会跟着变**（判据只写一份：`ReplayRecord.version`）。
                if (rec.version != new ReplayRecord().version)
                {
                    // ⚠️ 版本不认识就**别硬解**（字段对不上会静默读成默认值；而引擎换代之后
                    //    同一串动作会落到**别的格号** ⇒ 硬解出来的是**另一局**）
                    Debug.LogWarning($"[Replay] 版本 {rec.version} 不认识（本代码只认 "
                                   + $"{new ReplayRecord().version}）⇒ 不播：" + path);
                    return null;
                }
                if (rec.actions == null) rec.actions = new List<MsgAction>();
                return rec;
            }
            catch (Exception e) { Debug.LogError("[Replay] 读盘失败：" + e.Message); return null; }
        }

        /// <summary>超过 `keep` 局就**删最旧的**。返回删掉了几个。
        /// ⚠️ 删的是**录像文件**（用户数据）—— 只在超出上限时删，且**删之前先出声**。</summary>
        public static int TrimToLast(int keep)
        {
            int removed = 0;
            try
            {
                var all = List();                       // 已按新→旧
                for (int i = keep; i < all.Count; i++)
                {
                    string p = Path.Combine(Dir, all[i]);
                    Debug.Log("[Replay] 超出上限（" + keep + " 局）⇒ 删最旧的一份：" + all[i]);
                    File.Delete(p);
                    removed++;
                }
            }
            catch (Exception e) { Debug.LogError("[Replay] 修剪失败：" + e.Message); }
            return removed;
        }

        /// <summary>全删（自检用；**也顺手清掉目录**）。</summary>
        public static void ResetForTest()
        {
            try
            {
                if (!Directory.Exists(Dir)) return;
                foreach (var f in Directory.GetFiles(Dir, "*" + Ext)) File.Delete(f);
            }
            catch (Exception e) { Debug.LogError("[Replay] 清理失败：" + e.Message); }
        }
    }
}
