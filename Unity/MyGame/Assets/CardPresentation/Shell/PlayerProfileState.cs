// PlayerProfileState.cs — 原版 `PlayerDataManager` 上那**三条「整会话级」裸状态**的落点（B89-E10 建）。
//
//   账 `其他/基础设施`：344 `AddPracticeMatch` · 474 `RemovePracticeMatch` ·
//                      475 `ResetFactionPoints` · 490 `SetLocalStartTime`
//
// ============================ 这是什么、不是什么 ============================
//  · **只放状态**：三条都是「整局 / 整会话级」的裸状态，离线可完整复刻，没有更好的家
//    （施工单 `B88_R10` §2.4 的建议落点）。
//  · **不是** `PlayerDataManager` 的替代品：那 22 条里**带后端的 73 行用户已判 `D`**；
//    这里只收**本地状态**那三条。纯函数在 `Core/PlayerDataMath.cs`。
//
// ============================ 判据（逐条现读） ============================
//  🥇 `d:/2/tools/decomp_full/PlayerDataManager__{AddPracticeMatch,RemovePracticeMatch,
//      ResetFactionPoints,SetLocalStartTime,CurrentMillis}.c`
//  🥉 字段声明 / 偏移 = `d:/2/tools/il2cpp_out/dump.cs:85634-85785`
//     （`public int faction0Points; // 0x1B4` · `List<ChatMessageData> practiceMatchesList // 0x1C0` ·
//       `private long localStartTime; // 0x400`）
//
//  · `AddPracticeMatch(ChatMessageData)`（`__AddPracticeMatch.c`）全文 = **`List.Add`**
//    （`.c` 里那一大坨是 `List<T>.Add` 被内联展开的「容量够不够 → 写槽 / AddWithResize」，没有别的逻辑）。
//  · `RemovePracticeMatch(string playerId)`（`__RemovePracticeMatch.c`）：从头遍历，
//    `System_String__Equals(item.playerId(+0x20), 参数)` 命中就 `RemoveAt(i); return;`
//    ⇒ 🔴 **只删第一条命中**（同名多条留后面那些），**不是** `RemoveAll`。
//  · `ResetFactionPoints()`（`__ResetFactionPoints.c`）全文 2 行：`faction0Points(+0x1b4) = 0`。
//  · `SetLocalStartTime()`（`__SetLocalStartTime.c`）全文 2 行：`localStartTime(+0x400) = CurrentMillis()`。
//    `CurrentMillis()`（`__CurrentMillis.c`）= `(long)(DateTime.UtcNow − GetUtcOriginDate()).TotalMilliseconds`，
//    而 `GetUtcOriginDate()`（`SupportMethods__GetUtcOriginDate.c`）= `new DateTime(0x7b2, 1, 1, 0, 0, 0, Utc)`
//    = **1970-01-01 UTC** ⇒ **`CurrentMillis()` 就是 Unix 毫秒**（不是「开机以来的毫秒」）。
//
// ============================ 我们与原版的差异（只有一处，写死在这里） ============================
//  · 原版这些是**单例实例字段**；我们是**静态**（同 `BattleLogData` / `SocialData` 的既有做法：
//    原版已关服、单例只活一局，静态便于自检与断点）。⚠️ 因此多开一局**不会自动清** ⇒
//    自检/换档时必须显式 <see cref="ResetForTest"/>（同 `BattleLogData.ResetForTest` 的约定）。
//
// ⚠️ 没有 Unity 状态、不建 GameObject ⇒ 可以在批处理里随便调。

using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `PlayerDataManager` 上那三条「整会话级」裸状态的等价物
    /// （账 `其他/基础设施` :344/:474/:475/:490）。
    /// <para>**只放状态** —— 纯函数在 <see cref="PlayerDataMath"/>。</para></summary>
    public static class PlayerProfileState
    {
        // ======================================================== 344 / 474 practiceMatchesList

        /// <summary>原版 `PlayerDataManager.practiceMatchesList`（`List&lt;ChatMessageData&gt;` · `dump.cs:85831`）。
        /// 我们的 <c>ChatMessageData</c> 等价物 = <see cref="SocialData.ChatMessage"/>
        /// （唯一带 `PlayerId` 的聊天条目类型，`Shell/SocialData.cs:145`）。</summary>
        static readonly List<SocialData.ChatMessage> _practiceMatches = new List<SocialData.ChatMessage>();

        /// <summary>「约战」列表本体（原版 `practiceMatchesList`）。⚠️ 与 `SocialData.ChatMessages`
        /// **不是同一张表**：那张是聊天流（Global / Alliance 两个频道），这张是「和我约过战的人」。
        /// 原版两处都叫 `ChatMessageData`，但容器不同（`+0x1c0` vs 聊天窗自己的那份）。</summary>
        public static List<SocialData.ChatMessage> PracticeMatches { get { return _practiceMatches; } }

        /// <summary>原版 `PlayerDataManager.AddPracticeMatch(ChatMessageData chatEvent)`（账 344）。
        /// <para>判据 = `__AddPracticeMatch.c` 全文 —— `practiceMatchesList.Add(chatEvent)`，**没有别的**
        /// （不查重、不判 null、不排序）。⛔ 别顺手加判重或去重：原版同一个人约两次就有两条。</para></summary>
        public static void AddPracticeMatch(SocialData.ChatMessage chatEvent)
        {
            _practiceMatches.Add(chatEvent);
        }

        /// <summary>原版 `PlayerDataManager.RemovePracticeMatch(string playerId)`（账 474）。
        /// <para>判据 = `__RemovePracticeMatch.c`：遍历 → `item.playerId(+0x20) == 参数` → `RemoveAt(i)` → **`return`**。
        /// ⇒ 🔴 **只删第一条命中**（同 id 多条时后面的留着）。</para>
        /// <para>⚠️ 原版表里若有 `null` 条目，`.c` 的 `ldfld` 会抛 NRE；我们**跳过并出声**（不静默、
        /// 也不让一次脏数据把整条链炸掉）。</para></summary>
        public static void RemovePracticeMatch(string playerId)
        {
            for (int i = 0; i < _practiceMatches.Count; i++)
            {
                var item = _practiceMatches[i];
                if (item == null)
                {
                    Debug.LogWarning("[PlayerProfileState] `practiceMatchesList` 第 " + i
                                   + " 条是 null —— 原版在这里会抛 NRE，我们跳过它继续找。");
                    continue;
                }
                if (string.Equals(item.PlayerId, playerId))
                {
                    _practiceMatches.RemoveAt(i);
                    return;                       // 🔴 只删第一条（照原版那个 return）
                }
            }
        }

        // ======================================================== 490 localStartTime

        /// <summary>原版 `PlayerDataManager.localStartTime`（`private long` · +0x400）。
        /// 会话起始时刻（**Unix 毫秒**，见文件头那条 `CurrentMillis` 的判据推导）。
        /// <para>原版唯一的读者 = `get_currentServerTime`（`CurrentMillis() − localStartTime + 服务器时刻`）；
        /// 原版唯一的写点两处 = `SetLocalStartTime()` 与 `ProcessNewLoginResult`（后者是登录回包那条链，P3）
        /// ⇒ **原版没有「清空」的入口**，我们那个 <see cref="ResetForTest"/> 是自检要的。</para></summary>
        public static long LocalStartTime { get; private set; }

        /// <summary>原版 `PlayerDataManager.SetLocalStartTime()`（账 490）：`localStartTime = CurrentMillis()`。
        /// <para>判据 = `__SetLocalStartTime.c` 全文 2 行；
        /// `CurrentMillis()` == **Unix 毫秒**（推导见文件头）。</para></summary>
        public static void SetLocalStartTime()
        {
            // = 原版 `PlayerDataManager.CurrentMillis()`：
            //   `(long)(DateTime.UtcNow − new DateTime(1970,1,1,0,0,0,Utc)).TotalMilliseconds`
            //   ⇒ `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` 逐值相同（同样是截断到整毫秒）。
            LocalStartTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // ======================================================== 475 faction0Points

        /// <summary>原版 `PlayerDataManager.faction0Points`（`public int` · +0x1B4）——**活动阵营积分**。
        /// <para>⚠️ 它**不是**我们那三种阵营资源（信仰 / 灵魂石 / 任务点，见 `Core/*Faction*`）——
        /// 那是另一套（`useWaystone` 那三格）。原版另外两个写点
        /// （`RecordFactionPoints` / `RecordBossFactionPoints` 的回包闭包）**属服务器链（P3）**
        /// ⇒ 本地只有「读」与「清零」两件事（清零就是账 475）。</para></summary>
        public static int Faction0Points { get; set; }

        /// <summary>原版 `PlayerDataManager.ResetFactionPoints()`（账 475）。
        /// <para>判据 = `__ResetFactionPoints.c` 全文 2 行：`faction0Points(+0x1b4) = 0`。
        /// ⛔ 不夹、不校验、不广播 —— 就是清零。</para></summary>
        public static void ResetFactionPoints()
        {
            Faction0Points = 0;
        }

        // ======================================================== 自检用

        /// <summary>把本类三份状态清回出厂值（同 `BattleLogData.ResetForTest` / `SocialData.ResetForTest` 一族）。
        /// <para>🔶 自己设计（非原版）：原版是单例、进程结束即销毁，**没有这个入口**；
        /// 我们是静态 ⇒ 自检之间必须显式清，否则上一段的夹具会漏到下一段（`SocialData.ResetForTest`
        /// 的头注释记的就是这个坑）· 调度台 2026-10-28（B89-E10）裁定。</para></summary>
        public static void ResetForTest()
        {
            _practiceMatches.Clear();
            LocalStartTime = 0;
            Faction0Points = 0;
            DevToolsEnabled = false;      // 账 555：出厂 = 关（原版 obscurity 未解锁时的值）
            CollectorXpGranted = 0;       // 账 340：增量层清回 0（不做就是自检之间串味）
            _collectorXpLoaded = true;    // 🆕 B89-E10d：清完**别**再回头读盘（否则刚清掉的值又被盘上那份盖回来）
        }

        // ======================================================== 555 showDevTools（B89-E10b 追加在类尾）
        // 判据 = `decomp_full/PlayerDataManager__showDevTools.c`（全文现读 2026-10-28）：
        //   全是 `CodeStage.AntiCheat.ObscuredTypes.ObscuredBool.op_Implicit` 那一跳 ——
        //   **读一个静态 ObscuredBool 并隐式转 bool**，没有任何参数、没有任何副作用。
        //   `dump.cs` 的签名是 `public bool showDevTools()`（属性式写法，**不是 UI 页**）。
        //
        // ⚠️ **还没查清（如实记，⛔ 别当「我们的选择」）**：那个静态量属于哪个类是施工单 `B88_R10` §六·5
        //   挂着的未决项（`DAT_1842604c8` ≠ `GameStaticData` 的 `DAT_18427be00`）。最可能的来源是
        //   `PlayerDataKey.ActivateDevTools = 2`（我们 `Shell/SaveKeys.cs:43/247` **已经有这条键**、
        //   序号也是 2）⇒ 真接线应当读存档那一格，而不是另开一个开关。
        //   ⇒ **本格只是一个占位口**：默认 `false`（= 原版 obscurity 未解锁时的值），
        //      **没有写入方**（全仓 0 处写它），⛔ 不当成「原版就是这么来的」。

        /// <summary>原版 `PlayerDataManager.showDevTools()`（账 555）：读一个**全局 dev-tools 开关**。
        /// <para>⚠️ 数据源**还没查清**（见上方那段）：本格今天**没有任何写入方**、恒为 `false`，
        /// 真正的来源大概率是存档键 `SaveKeys.ActivateDevTools`（= `PlayerDataKey.ActivateDevTools(2)`）
        /// —— 接线时把 <see cref="DevToolsEnabled"/> 换成读那一格即可（**只有一个口，别在别处再判一次**）。</para>
        /// <para>⚠️ 它不是「devtools 页要不要建」那个问题（那是设置窗 UI 线的事）。</para></summary>
        public static bool DevToolsEnabled { get; set; }

        // ======================================================== 340 AddCollectorXp（B89-E10c 追加在类尾）
        // 判据 = `dll 反编译 PlayerDataManager.AddCollectorXp`（`cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:15335-15374`）
        // 逐句：
        //   `previousLevel = CurrentLevel;`
        //   `CurrentXp += grantedXp;`
        //   `Raise(new CollectorXPChangeSignal(newXp: CurrentXp, previousXp: …));`     ← 信号层
        //   `need = CardTierConfig.GetAccumulatedXPForLevel(previousLevel + 1);`
        //   `if (CurrentXp >= need) { Raise(new CollectorLevelChangeSignal(previousLevel, 新等级)); 升级 toast }`
        //   `if (showToast) { 「+{0} 收藏点数」toast }`
        // ⇒ **升级 toast 不受 `showToast` 管**（原版就是这样：`showToast` 只管经验那条）。
        //
        // 🔴 我们的存储形状不同（这一条是账 340 的真缺口）：
        //   原版 `CurrentXp` 是**一个可写的整数**；我们今天的收藏总 XP 是
        //   `CardTierConfig.GetTotalPlayerXPFromInventory()` **从背包反推**的**只读派生值** ⇒ **没有增量口**。
        //   ⇒ 本类加一层**增量**（`CollectorXpGranted`），总 XP = 库存反推 + 增量。
        // ⚠️ 待接点（报告 §5）：`CardTierConfig.PlayerLevel` 今天**看不到**这一层（它只算库存那份）
        //   ⇒ 顶栏/档案窗的等级要等那个文件加一句 `+ PlayerProfileState.CollectorXpGranted`（一行，不归本代理）。

        /// <summary>本地累计的「**奖励发下来的**收藏经验」（原版 `PlayerDataManager.CurrentXp` 里
        /// 「服务器发的那部分」）。
        /// <para>🔶 自己设计（非原版）：原版只有一个整数 `CurrentXp`（服务器每次登录下发）；
        /// 我们的总 XP 是**从背包反推**的派生值、不可写 ⇒ 只能在这一层加增量 ·
        /// 原版那个值是**服务器给的**、我们这层是**本地模拟** · 调度台 2026-10-28（B89-E10c）裁定。</para>
        /// <para>🆕 **2026-10-28（B89-E10d）已落盘**：走中央存档 `SaveCenter`（`persistentDataPath/WarpforgeSave/`），
        /// 键 = `SaveKeys.OursPrefix + "CollectorXp"`（`_ours_` 前缀 = 原版没有这条键，⛔ 不硬套 `PlayerDataKey`）。
        /// 第一次读走 `EnsureCollectorXpLoaded()`（懒加载，失败 ⇒ 出声 + 当 0，见 `SaveCenter.Get` 的四种 null）；
        /// 每次改完 `PersistCollectorXp()` 原子落盘。⚠️ 批处理/不在 Play 且没设 `SaveCenter.OverrideRoot` 时
        /// `SaveCenter` 自己会**丢弃写并出声一次**（⛔ 不碰玩家真档）。</para></summary>
        public static int CollectorXpGranted
        {
            get { EnsureCollectorXpLoaded(); return _collectorXpGranted; }
            private set { _collectorXpGranted = value; }
        }

        /// <summary>存档键（`_ours_` 自定键；见 `SaveKeys.OursPrefix` 那条）。
        /// ⚠️ 改这个串 = **老档读不回来**，⛔ 别「顺手改个更漂亮的名字」。</summary>
        public const string CollectorXpSaveKey = SaveKeys.OursPrefix + "CollectorXp";

        /// <summary>存档 payload（`SaveCenter.Get<T>` 要求 `T : class`）。
        /// <para>⚠️ 🔴 判「这一格在不在」**只能看 `SaveCenter.Get` 是不是 null**（= 整个 key 缺），
        /// **⛔ 不能用 `!= null` 去判字段** —— `JsonUtility` 会把缺的引用类型字段**物化成整棵子树**
        /// （本项目踩过，见记忆 `jsonutility-null-subtree`）。本 DTO 只有一个 `int`，缺字段 ⇒ 就是 `0`（可接受）。</para></summary>
        [System.Serializable]
        public sealed class CollectorXpSave
        {
            public int grantedXp;
        }

        static int _collectorXpGranted;
        static bool _collectorXpLoaded;

        /// <summary>懒加载：第一次读 <see cref="CollectorXpGranted"/> 时从盘上取一次。
        /// 取不到（第一次跑 / 档坏了 / 未来版本）⇒ 保持 0 并**由 `SaveCenter` 出声**（⛔ 不静默编数）。</summary>
        static void EnsureCollectorXpLoaded()
        {
            if (_collectorXpLoaded) return;
            _collectorXpLoaded = true;                       // 先置位：读失败也不反复重试/刷屏
            // 🔴 **批处理 / 不在 Play 且没设 `SaveCenter.OverrideRoot` ⇒ 不读盘**（`StoreOn` 就是那条闸）。
            //    理由：`PlayerLevel` 每次都会读这一格，若批处理里也去读玩家真档，
            //    **自检的等级断言会随玩家存档漂**（同一份代码在不同机器上结论不同）。
            if (!SaveCenter.StoreOn) return;
            var d = SaveCenter.Get<CollectorXpSave>(CollectorXpSaveKey);
            if (d != null) _collectorXpGranted = d.grantedXp;
        }

        /// <summary>落盘（原子写；`SaveCenter` 自己管「这一趟允不允许写」）。失败**只说一次**（批处理里会连续命中）。</summary>
        static bool _warnedPersistFail;
        static void PersistCollectorXp()
        {
            if (SaveCenter.Set(CollectorXpSaveKey, new CollectorXpSave { grantedXp = _collectorXpGranted })) return;
            if (_warnedPersistFail) return;
            _warnedPersistFail = true;
            Debug.LogWarning("[PlayerProfileState] `CollectorXp` 没写进存档（原因见上一条 `SaveCenter` 日志）"
                           + " ⇒ 增量**只活在内存里**（⛔ 不静默；这一句只说一次）。");
        }

        /// <summary>自检用：强制下一次读 <see cref="CollectorXpGranted"/> 走盘（用来验「存了能读回来」）。
        /// <para>🔶 自己设计（非原版）：原版没有这个口（它每次登录由服务器灌）；本仓是静态 + 懒加载 ⇒ 要验往返必须有它 ·
        /// 调度台 2026-10-28（B89-E10d）裁定。</para></summary>
        public static void ReloadCollectorXpFromSave()
        {
            _collectorXpLoaded = false;
            EnsureCollectorXpLoaded();
        }

        /// <summary>收藏总 XP = **库存反推的那份**（`CardTierConfig.GetTotalPlayerXPFromInventory`，原版同口径）
        /// **＋** 本层增量（<see cref="CollectorXpGranted"/>）。</summary>
        public static int TotalCollectorXp
        {
            get { return CardTierConfig.Instance.GetTotalPlayerXPFromInventory() + CollectorXpGranted; }
        }

        /// <summary>原版 `PlayerDataManager.AddCollectorXp(int grantedXp, bool showToast)`（账 340）：
        /// **加 XP → 过阈就升一级（弹升级 toast）→ `showToast` 时再弹「+N 收藏点数」**。
        /// <para>判据 = `PlayerDataManager.cs:15335-15374`（见上面那段逐句）。</para>
        /// <para>⚠️ 与 <see cref="PlayerDataMath.GetXPFromMatch"/> **没有调用关系**：原版那个方法
        /// 全树 **0 调用点**（本波反证），它只是一个独立的纯函数，⛔ 别把它当成这条链的上游。</para>
        /// <para>🔶 自己设计（非原版）：原版第 ③ 步 raise `CollectorXPChangeSignal` / `CollectorLevelChangeSignal`，
        /// 本仓**没有信号层** ⇒ 改成**直接调**已有的那两处 toast（`ToastNotifications.ShowCollectionLevelUp` /
        /// `ShowGrantedXpToast`）；顶栏/档案窗的刷新由它们各自的 `Refresh*` 口负责（不是本类的事） ·
        /// 调度台 2026-10-28（B89-E10c）裁定。</para></summary>
        /// <param name="grantedXp">本次加多少（可为负 —— 原版不判，照抄）。</param>
        /// <param name="showToast">`true` = 除了升级 toast，**再**弹一条「+N 收藏点数」（原版 `RewardService`
        /// 传的是 `false`，然后自己在外面弹 —— 见 <see cref="GrantMatchXp"/> 的形状）。</param>
        /// <returns>加完之后**按总 XP 算出的等级**（原版 `CurrentLevel`；今天它与 `CardTierConfig.PlayerLevel`
        /// 在「待接点」接上之前会差那一段增量 —— 已如实记）。</returns>
        public static int AddCollectorXp(int grantedXp, bool showToast)
        {
            int previousLevel = CardTierConfig.PlayerLevel;
            CollectorXpGranted += grantedXp;
            PersistCollectorXp();                             // 🆕 B89-E10d：改完就落盘（原版 `CurrentXp` 由服务器在下次登录带上）
            int total = TotalCollectorXp;
            int currentLevel = CardTierConfig.Instance.GetLevelForXP(total);
            float need = CardTierConfig.Instance.GetAccumulatedXPForLevel(previousLevel + 1);
            if (total >= need)
                ToastNotifications.ShowCollectionLevelUp(currentLevel);   // 原版这条**不受 showToast 管**
            if (showToast)
                ToastNotifications.ShowGrantedXpToast(grantedXp);
            return currentLevel;
        }

        /// <summary>🔶 自己设计（非原版）：**本地模拟**「一局打完服务器会把 XP 发下来」那半条链。
        /// <para>原版：`BattleManager.ProcessMatchResult` → liveops `RecordMatchFinished` **服务器回包** →
        /// `PlayerDataManager.ProcessMatchResult` → `RewardService.Collect(rewards)` →
        /// `Sum(RewardInfo.grantedXp)` → `AddCollectorXp(x, showToast:false)` ＋ `ShowGrantedXpToast(x)`。
        /// **值来自服务器**，客户端一分不算；`GetXPFromMatch` 在那条链里 **0 调用点**。</para>
        /// <para>我们：离线没有那条回包 ⇒ 用原版那个「客户端版公式」`GetXPFromMatch` 充作「服务器会给的数」，
        /// 其余三跳照原版顺序。🔴 **自设计的范围只有「值从哪来」这一件事**（B89-E10e 复核过）：
        /// · **公式本身 100% 原版** —— `GetXPFromMatch` 的分支表 / 常量（100 / 30 / 130 / 0）/ `matchWon == 1.0f`
        ///   **精确相等** / `Campaign` 赢返 130 那个 bug，全部逐句来自 `PlayerDataManager__GetXPFromMatch.c`，
        ///   **没有一格是我们定的**（它的 `default` 分支连告警串都是原文）。
        /// · **`xp <= 0` 就两条 toast 都不弹**也**不是我们加的闸** —— 原版 `RewardService.Collect` 自己就有
        ///   `if (grantedXp <= 0)` 跳过 `AddCollectorXp` / `ShowGrantedXpToast`（`RewardService.cs:390 / :438`）。
        /// ⇒ 所以只有**一条** `🔶`（谁给这个值），⛔ 别拆成「公式也自设计」—— 那会误导下一个人去改分支表。
        /// 原版是服务器回包 · 调度台 2026-10-28（B89-E10c/c 复核 E10e）裁定。</para>
        /// <para>⚠️ **生产调用点还没接**：应当在 `Battle/BattleDriver.cs` 的 `RecordBattleLog()`（局终）之后调一次；
        /// 那个文件归 E5c ⇒ 本波只写这一行待接点，⛔ 没去改。</para></summary>
        /// <param name="matchType">本局 `MatchType`（`GetXPFromMatch` 的分支键）。</param>
        /// <param name="won">是否获胜（原版 `matchWon == 1.0f` 才给满额）。</param>
        /// <param name="showToast">`true`（默认）⇒ 弹「+N 收藏点数」；`false` ⇒ 只记账（原版
        /// `RewardService` 是「先 `AddCollectorXp(…, false)` 再单独 `ShowGrantedXpToast`」，本方法按那个顺序做）。</param>
        /// <returns>本次发下去的 XP（`0` = 这个模式原版不给、或本来该给的数就是 0 ⇒ **两条 toast 都不弹**）。</returns>
        public static int GrantMatchXp(RuleEngine.MatchType matchType, bool won, bool showToast = true)
        {
            int xp = PlayerDataMath.GetXPFromMatch(matchType, won ? 1.0f : 0.0f);
            if (xp <= 0) return 0;
            AddCollectorXp(xp, false);                        // 原版 `AddCollectorXp(grantedXp, showToast: false)`
            if (showToast) ToastNotifications.ShowGrantedXpToast(xp);   // 原版同一条链的下一句
            return xp;
        }
    }
}
