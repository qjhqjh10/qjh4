// MissionEvent.cs —— **任务活动的存档层 + 挑战监听接线**（原版 `MissionEvent<TS,TD> : LiveOpsEvent<TS,TD>`）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §四 `MissionEvent<TS,`（C 6 条）+ §四·b 那条
//     `UpdateChallengeProgress` 的**教程门**
//   · 账 `资料/完全复刻/账/泛型与启动链_补读.tsv`（`GetBaseSave` / `UpdateSaveGame` / `Expire` /
//     `DisableChallenge` / `GetContainer` / `ReleaseContainer`）+ `账/日常_奖励.tsv` 的两条
//     （`Expire` / `ReleaseContainer`）
// 原版真身（**泛型方法体本机有**，`d:/2/tools/decomp_generic_trial/out/`，641 个产物里命中 22 个）：
//   `MissionEvent_object__object___GetBaseSave__102e990.c` · `…_UpdateSaveGame__102fbd0.c` ·
//   `…_Expire__102e650.c` · `…_DisableChallenge__102e5c0.c` · `…_GetContainer__102ea40.c` ·
//   `…_ReleaseContainer__102f1c0.c`
//   谓词实体在 `d:/2/tools/decomp_full/MissionEvent.__c_object__object____Expire_b__29_0.c` /
//   `…__UpdateSaveGame_b__26_0.c`（两个 b__26 是**同一个函数体**：`p =&gt; p.Challenge`）；
//   字段表 = `d:/2/tools/il2cpp_out/dump.cs:60754-60840`（MissionEvent）/`:61024-61050`（MissionPersistence）/
//   `:61185-61200`（MissionChallengeProgress）/`:59613-59660`（MissionChallenge）。
//
// 🔴 **`Expire` 的闸是「还有可领的就不让过期」**：谓词实体
//   `MissionEvent.__c...__Expire_b__29_0.c` 的**全部内容** = `return *(undefined1 *)(p + 0x48);`，
//   而 `MissionChallengeProgress` 的 `+0x48` 是 **`canCollect`** ⇒
//   `if (SaveGame.challengeProgress.Any(p =&gt; p.canCollect)) { onComplete?.Invoke(); return; }`。
//   ⛔ 别把这条读成「过期就是要清掉可领的」——**正相反**。
//
// ⚠️ 与「原版」的如实差异（⛔ 不是「已经一样」）：
//   ① `LoadableReference<ChallengeWrapper>` / `MissionChallengeProvider` 我们这一层没有 ⇒
//      `MissionChallenge` 收**卡 id 字符串**（`MissionData.challengeIds`）。
//   ② 原版 `MissionEvent.Handler` 是 `Missions : LiveOpsHandler&lt;IMissionEvent&gt;`（只用来取
//      「同一 handler 下的事件表」）；我们走 `LiveOpsHandler.GetEvent&lt;T&gt;`，两处语义一致。
//   ③ `UiContainer` 容器池（`GetContainer` / `ReleaseContainer` 的基类那一半）属 `Core/UiContainer`（B33）
//      ⇒ 本类**只搬 MissionEvent 自己的那一跳**（`UseSmallContainer` 为真时走 `LiveOpsAssetUtility`），
//      基类那一跳转调 `LiveOpsEvent.GetContainer()`，**本批没有落点** ⇒ 出声一次。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 挑战（原版 `MissionChallenge` 的最小半边）
    /// <summary>原版 `ChallengeUpdatePayload`（**record**，两个只读字段）：
    /// `Challenge`（`+0x10`）/ `UpdateFunction`（`+0x18`，`Func&lt;ChallengeProgress,bool&gt;`）。
    /// 判据 = `dump.cs:59801-59819`。</summary>
    public class MissionChallengeUpdatePayload
    {
        public readonly MissionChallenge Challenge;
        public readonly Func<MissionChallengeProgress, bool> UpdateFunction;
        public MissionChallengeUpdatePayload(MissionChallenge challenge, Func<MissionChallengeProgress, bool> updateFunction)
        {
            Challenge = challenge;
            UpdateFunction = updateFunction;
        }
    }

    /// <summary>原版 `MissionChallenge`（抽象，`dump.cs:59613`）的**本批用得到的那半边**：
    /// `id`（`+0x10`）/ `rewards`（`+0x18`）/ `OnProgressUpdate`（`+0x48`，`Action&lt;ChallengeUpdatePayload&gt;`）/
    /// `Initialize()`。
    /// <para>⚠️ 原版还有 `expansion` / `reachMilestoneWhen` / `challengeType` / `cummulative` / `loopable` /
    /// 两个 playMode 名单等 9 格（判据 `dump.cs:59615-59648`）—— **本批的 6 条账一行都不读它们**
    /// ⇒ 没搬就是没搬（⛔ 别写个半套让人以为齐了）。</para></summary>
    [Serializable]
    public abstract partial class MissionChallenge
    {
        public string id;
        public ChallengeMilestone[] rewards;   // 原版 `ChallengeMilestone[] rewards; // 0x18`（B92-N8c 起真用：`AvailableRewards`/`MaxValue`）

        /// <summary>原版 `+0x48`：挑战进度变了就喊一声（`MissionEvent` 是**唯一**订阅方）。</summary>
        public event Action<MissionChallengeUpdatePayload> OnProgressUpdate;

        /// <summary>原版 `MissionChallenge.initialized`（`+0x50`，一次性闩）：`Initialize()` 与 `DisableChallenge()` 共用（🔴 **2026-10-28（B92_N8）按 `MissionChallenge__Initialize.c` 补一次性闸**）；`initialized` / `IsInitialized` / `GetID()` / `OnInitialize` 的完整判据见**文末 partial 块**（本处行数不变以免账的行号落点漂移）。</summary>
        public virtual void Initialize() { if (initialized) return; OnInitialize(); initialized = true; }

        /// <summary>内部广播口（子类在进度真的变了之后调；`Initialize` 也走这一个口）。</summary>
        protected void RaiseProgress()
        {
            var h = OnProgressUpdate;
            if (h != null) h(new MissionChallengeUpdatePayload(this, null));
        }
    }

    /// <summary>原版 `MissionChallengeProgress`（`dump.cs:61185-61200`，字段逐个抄）。
    /// <para>`canCollect`（`+0x48`）是 `MissionEvent.Expire` 那道闸的**唯一读者**（见文件头 🔴）。</para></summary>
    [Serializable]
    public partial class MissionChallengeProgress
    {
        public string entryId;
        public string challengeId;
        public int currentValue;
        public int difficulty;
        /// <summary>原版 `+0x48`：进度够了、还没领 ⇒ `true`。**它决定「活动能不能过期」**。</summary>
        public bool canCollect;
        public long willEndAt;
        public long completedAt;
        public long startedAt;

        private MissionChallenge challenge;
        /// <summary>原版 `Challenge` 属性：按 `challengeId` 查表（原版查 `MissionChallengeProvider`）。
        /// 这里是**由装载方注入**的引用（见 <see cref="MissionPersistence.Bind"/>）。</summary>
        public MissionChallenge Challenge
        {
            get { return challenge; }
            set { challenge = value; }
        }

        /// <summary>原版 `MissionChallengeProgress.IsComplete()` → 真体在文末 partial 块 `IsCompleteCore()`
        /// （**2026-10-28 B92-N8c 按 `.c` 落**：`loopable ? false : Count(AvailableRewards()) <= collectedRewards`）。
        /// ⚠️ 原来这里是 `completedAt != 0` 的保守近似，**与 `.c` 的算式不是一回事**，已换掉。
        /// `MissionPersistence.IsCompleted` / `IsChallengeComplete` 都读这一格 ⇒ 一并按原版改口径。</summary>
        public bool IsComplete() { return IsCompleteCore(); }
    }

    // ============================================================ 存档（原版 `MissionPersistence`）
    /// <summary>原版 `MissionPersistence : LiveOpsEventPersistence, IBeforeRead`（`dump.cs:61024-61050`，
    /// 五个字段逐个抄）。`ISave` 的泛型参数就是它。</summary>
    [Serializable]
    public partial class MissionPersistence : LiveOpsSave
    {
        public long lastRefillUpdate;
        public int availableRefills;
        public long nextRefresh;
        public MissionChallengeProgress[] challengeProgress;
        public List<string> completedChallengeIds = new List<string>();   // 原版 `Dictionary&lt;string,long&gt;`

        /// <summary>原版 `override bool IsCompleted`（`MissionPersistence__get_IsCompleted.c`，**逐句**）：
        /// <code>
        /// if (challengeProgress != null &amp;&amp; challengeProgress.Length != 0)
        ///     return challengeProgress.All(p =&gt; p.IsComplete());
        /// return completedChallenges.Any();
        /// </code>
        /// 🔴 **`+0x18` 是数组长度**（不是「有没有元素」的 bool）—— 空表才走第二支。</summary>
        public override bool IsCompleted
        {
            get
            {
                var a = challengeProgress;
                if (a != null && a.Length != 0)
                {
                    for (int i = 0; i < a.Length; i++)
                        if (a[i] == null || !a[i].IsComplete()) return false;
                    return true;                       // 全完成
                }
                return completedChallengeIds != null && completedChallengeIds.Count > 0;
            }
        }

        /// <summary>原版 `bool IsChallengeComplete(MissionChallenge challenge)`（`MissionPersistence__IsChallengeComplete.c`）：
        /// ① `completedChallenges.Contains(challenge.GetID())`；② 否则 `challengeProgress.FirstOrDefault(p =&gt; p.challengeId == GetID())?.IsComplete()`。
        /// 🔴 **2026-10-28（B92_N8）**：第②支原来漏了（只做了①）—— 完整体在文末 partial 块（本处行数不变免漂移）。</summary>
        public bool IsChallengeComplete(MissionChallenge challenge)
        {
            if (challenge == null) return false;
            return IsChallengeCompleteCore(challenge);
        }

        /// <summary>把存档行与挑战对象对起来（原版靠 `MissionChallengeProvider` 资产表查，
        /// 我们靠这个显式口 —— 差异 ①）。</summary>
        public void Bind(Func<string, MissionChallenge> lookup)
        {
            if (lookup == null || challengeProgress == null) return;
            for (int i = 0; i < challengeProgress.Length; i++)
            {
                var p = challengeProgress[i];
                if (p == null || string.IsNullOrEmpty(p.challengeId)) continue;
                p.Challenge = lookup(p.challengeId);
            }
        }
    }

    /// <summary>原版 `MissionData : LiveOpsEventData`（抽象，`dump.cs:60424`）的**最小半边**：
    /// 只搬「挑战表」这一格（`challenges`，原版 `List&lt;LoadableReference&lt;ChallengeWrapper&gt;&gt;`）。
    /// ⚠️ 其余 8 格（`challengeLists` / `missionType` / `maxActiveCount` / `maxStoredRefill` / `delayedRefill` …）
    /// **本批 6 条账一行都不读** ⇒ 没搬。</summary>
    [Serializable]
    public abstract class MissionData : LiveOpsEventData
    {
        public List<string> challengeIds = new List<string>();
    }

    // ============================================================ 任务活动（原版 `MissionEvent<TS,TD>`）
    /// <summary>原版 `MissionEvent&lt;TS,TD&gt; : LiveOpsEvent&lt;TS,TD&gt;`（`dump.cs:60754`）。
    /// <para>本类只做账里那 6 件事 + 两个属性：类型化存档、收到新存档（摘/挂监听）、过期、摘单个挑战、
    /// 容器的取/放。</para>
    /// <para>🔴 **它不改任务进度本身** —— 进度由挑战自己算、广播回来（`UpdateChallengeProgress` 那条
    /// 教程门属另一条账，见判据 §四·b）。</para></summary>
    public abstract class MissionEvent<TS, TD> : LiveOpsEvent<TS, TD>
        where TS : MissionPersistence
        where TD : MissionData
    {
        /// <summary>原版 `private bool UseSmallContainer { get; set; }`（`+0x0`，`dump.cs:60757`）：
        /// 真 ⇒ 取/放容器走 `LiveOpsAssetUtility`（小容器那一档）；假 ⇒ 走基类。</summary>
        public bool UseSmallContainer { get; set; }

        /// <summary>原版 `public IEnumerable&lt;MissionChallengeProgress&gt; CurrentChallenges`
        /// （`get_CurrentChallenges`，`+0x48` 那张表）。</summary>
        public IEnumerable<MissionChallengeProgress> CurrentChallenges
        {
            get
            {
                var s = SaveGame;
                if (s == null || s.challengeProgress == null) return Array.Empty<MissionChallengeProgress>();
                return s.challengeProgress;
            }
        }

        /// <summary>原版 `public new TS GetBaseSave()`（`…_GetBaseSave__102e990.c`）：
        /// 把 `base.GetBaseSave()` 的结果 **isinst 成 TSave**，类型不对就 **throw**（`FUN_1803f3780` = 抛）。
        /// ⚠️ 我们出声 + null（全工程口径：坏数据不让游戏崩），其余逐字。</summary>
        public new TS GetBaseSave()
        {
            var s = base.GetBaseSave();
            if (s == null) return null;
            var t = s as TS;
            if (t == null)
                Debug.LogError("[MissionEvent] GetBaseSave：存档类型不是 " + typeof(TS).Name
                             + "（实际 " + s.GetType().Name + "）；原版这一跳 throw");
            return t;
        }

        // ── 挑战监听的唯一入口/出口（原版是 `*this + 0x5d0` 那一个方法槽）────────────────────
        /// <summary>挑战广播回来时调（原版 `MissionEvent` 上 `+0x5d0` 那个方法）。
        /// 本批**只搬接线**：真语义（重算进度 / 教程门 `!FirstTwoTutorialCompleted() &amp;&amp; DailyLoginChallenge`）
        /// 属另一条账（判据 §四·b 最后一行）⇒ 这里给出**空的覆盖点**并出声（⛔ 不假装已实现）。</summary>
        protected virtual void OnChallengeProgressChanged(MissionChallengeUpdatePayload payload)
        {
            // 有意留空：进度重算 + 教程门不在本批 6 条账里。
        }

        /// <summary>原版 `protected void DisableChallenge(MissionChallenge challenge)`
        /// （`…_DisableChallenge__102e5c0.c`，**全部内容**）：
        /// <code>
        /// challenge.OnProgressUpdate -= &lt;this.+0x5d0&gt;;
        /// </code>
        /// 🔴 反汇编里**只有那一句** —— 没有「从活动列表移除」（账的判据要点是这么写的，
        /// 以 `.c` 为准：**摘监听就这一句**）。⚠️ `challenge` 为 null 时原版 NRE ⇒ 我们出声。</summary>
        public void DisableChallenge(MissionChallenge challenge)
        {
            if (challenge == null)
            {
                Debug.LogError("[MissionEvent] DisableChallenge：challenge 是 null（原版这一跳 NRE）");
                return;
            }
            challenge.OnProgressUpdate -= OnChallengeProgressChanged;
        }

        /// <summary>挂监听的那一半（`DisableChallenge` 的对称动作；原版只在 `UpdateSaveGame` 的「新来的挑战」
        /// 那一支里直接写 `+=`，**没有**单独的公开方法 ⇒ 本方法是 private 的等价物）。
        /// 顺序照反汇编：**先 `+=`、再 `Initialize()`**（`Initialize` 会立刻广播一次，监听必须在它之前挂上）。</summary>
        void EnableChallenge(MissionChallenge challenge)
        {
            if (challenge == null) return;
            challenge.OnProgressUpdate += OnChallengeProgressChanged;
            challenge.Initialize();
        }

        /// <summary>原版 `public void UpdateSaveGame(LiveOpsEventPersistence save)`
        /// （`…_UpdateSaveGame__102fbd0.c`）。可读的骨架：
        /// <code>
        /// var s = save as TS; if (s == null) throw;
        /// var oldSet = this.SaveGame.challengeProgress.Select(p =&gt; p.Challenge).ToHashSet();
        /// var newSet = s.challengeProgress.Select(p =&gt; p.Challenge).ToHashSet();
        /// foreach (var c in oldSet.Except(newSet)) c.OnProgressUpdate -= handler;   // 摘
        /// this.SaveGame = s;                                                        // ★ 写的是【存档】不是 Data
        /// foreach (var c in newSet.Except(oldSet))
        ///     if (s.challengeProgress.Any(p =&gt; p.Challenge == c)) { c.OnProgressUpdate += handler; c.Initialize(); }
        /// </code>
        /// 🔴 **`param_1[4]`（= `this + 0x20`）写的是 `SaveGame`**，不是 `Data`：
        /// `LiveOpsEvent` 的字段序是 `Handler +0x10 / Data +0x18 / SaveGame +0x20`
        /// （同一条判据在 `Expire` 里也成立：它读 `this+0x20 → +0x48` 拿到 `challengeProgress`）。
        /// ⛔ 别改成写 `Data`（那会让「进度」与「事件定义」两格互换，静默错）。
        /// <para>谓词实体 `b__26_0` / `b__26_1` **是同一个函数体**（`p =&gt; p.Challenge`），见文件头。</para>
        /// <para>⚠️ 反汇编里两支 `Except` 的先后与「哪一支摘、哪一支挂」被寄存器复用搅在一起（`lVar4`/`lVar5`），
        /// 语义只能取上面这条（**摘 = 旧的减新的 · 挂 = 新的减旧的**）—— 这也是唯一自洽的读法：
        /// 摘掉不再属于本活动的挑战、给新加入的挂上。</para>
        /// <para>⚠️ 原版 `save` 类型不对时 **throw**；我们出声 + 不动（全工程口径）。</para></summary>
        public override void UpdateSaveGame(LiveOpsSave save)
        {
            var s = save as TS;
            if (s == null)
            {
                Debug.LogError("[MissionEvent] UpdateSaveGame：save 不是 " + typeof(TS).Name
                             + "（实际 " + (save == null ? "<null>" : save.GetType().Name) + "）；原版这一跳 throw");
                return;
            }

            var oldSet = ChallengeSet(SaveGame);
            var newSet = ChallengeSet(s);

            foreach (var c in oldSet)
                if (c != null && !newSet.Contains(c)) c.OnProgressUpdate -= OnChallengeProgressChanged;

            SaveGame = s;                                    // ★ 写 SaveGame（见上）

            foreach (var c in newSet)
            {
                if (c == null || oldSet.Contains(c)) continue;
                if (!ContainsChallenge(s, c)) continue;
                EnableChallenge(c);
            }
        }

        /// <summary>`save.challengeProgress.Select(p =&gt; p.Challenge).ToHashSet()`（两个 b__26 谓词）。</summary>
        static HashSet<MissionChallenge> ChallengeSet(MissionPersistence save)
        {
            var set = new HashSet<MissionChallenge>();
            if (save == null || save.challengeProgress == null) return set;
            for (int i = 0; i < save.challengeProgress.Length; i++)
            {
                var p = save.challengeProgress[i];
                if (p != null && p.Challenge != null) set.Add(p.Challenge);
            }
            return set;
        }

        static bool ContainsChallenge(MissionPersistence s, MissionChallenge c)
        {
            if (s == null || s.challengeProgress == null) return false;
            for (int i = 0; i < s.challengeProgress.Length; i++)
                if (s.challengeProgress[i] != null && s.challengeProgress[i].Challenge == c) return true;
            return false;
        }

        /// <summary>原版 `public override void Expire(Action onComplete)`（`…_Expire__102e650.c`，**逐句骨架**）：
        /// <code>
        /// if (SaveGame == null) throw;                                  // 原版 NRE
        /// if (SaveGame.challengeProgress.Any(p =&gt; p.canCollect)) {      // ★ b__29_0 = `*(p+0x48)` = canCollect
        ///     onComplete?.Invoke();
        ///     return;                                                   // ★ 还有可领的 ⇒ 不过期
        /// }
        /// if (GetID().Equals(&lt;那个常量串&gt;)) SegmentationController.AddTag(0xb, 1);   // 分段活动的那一格标签
        /// base.Expire(onComplete);
        /// </code>
        /// <para>⚠️ 那个常量串（`DAT_184262ea8`）本批**没读出来**（是 metadata 指针，不是数值字面量）
        /// ⇒ 我们保留那句调用但**跳过**它并出声（⛔ 不猜一个串去比）。它只影响「分段活动加不加标签」，
        /// 不影响过期本身（`AddTag` 之后照样调 `base.Expire`）。</para></summary>
        public override void Expire(Action onComplete)
        {
            var s = SaveGame;
            if (s == null)
            {
                Debug.LogError("[MissionEvent] Expire：没有存档（原版这一跳 NRE）⇒ 只回调、不改状态");
                if (onComplete != null) onComplete();
                return;
            }
            if (s.challengeProgress != null)
            {
                for (int i = 0; i < s.challengeProgress.Length; i++)
                {
                    var p = s.challengeProgress[i];
                    if (p != null && p.canCollect)
                    {
                        if (onComplete != null) onComplete();       // ★ 还有可领的 ⇒ 不走过期
                        return;
                    }
                }
            }
            // 分段活动那一格标签：判据串未解出 ⇒ 出声一次（见上）
            Debug.Log("[MissionEvent] Expire：'" + GetID() + "' 的「分段活动加标签」那一跳本批没解出判据串，已跳过");
            base.Expire(onComplete);
        }

        /// <summary>原版 `public virtual T GetContainer()`（`…_GetContainer__102ea40.c`，**逐句**）：
        /// <code>
        /// if (UseSmallContainer) { LiveOpsAssetUtility.LoadComponent(this, 1 /*Container*/); return; }
        /// base.GetContainer();
        /// </code>
        /// <para>⚠️ 基类那一跳（`LiveOpsEvent.GetContainer`）与 `LiveOpsAssetUtility` 都还没有落点
        /// （前者属 `Core/UiContainer` = B33、后者属 B13）⇒ 本类**只搬分支判断**，
        /// 两支都出声返回 null（⛔ 不假装取到容器）。返回类型用 `UnityEngine.Object` 是因为
        /// 原版那一跳返回的就是一个组件（`UiContainer` 的实例）。</para></summary>
        public virtual UnityEngine.Object GetContainer()
        {
            if (UseSmallContainer)
            {
                Debug.LogWarning("[MissionEvent] GetContainer：走 `UseSmallContainer` 支 —— "
                               + "`LiveOpsAssetUtility.LoadComponent(this, Container)` 属 B13，本批没有落点 ⇒ null");
                return null;
            }
            Debug.LogWarning("[MissionEvent] GetContainer：走基类支 —— `LiveOpsEvent.GetContainer` 属 B33，本批没有落点 ⇒ null");
            return null;
        }

        /// <summary>原版 `public virtual void ReleaseContainer()`（`…_ReleaseContainer__102f1c0.c`，**逐句**）：
        /// <code>
        /// if (UseSmallContainer) { LiveOpsAssetUtility.Unload(this, 1 /*Container*/); return; }
        /// base.ReleaseContainer();
        /// </code>
        /// ⚠️ 与 <see cref="GetContainer"/> 同一处没有落点 ⇒ 出声，不做。</summary>
        public virtual void ReleaseContainer()
        {
            if (UseSmallContainer)
            {
                Debug.LogWarning("[MissionEvent] ReleaseContainer：走 `UseSmallContainer` 支 —— "
                               + "`LiveOpsAssetUtility.Unload(this, Container)` 属 B13，本批没有落点 ⇒ 不做事");
                return;
            }
            Debug.LogWarning("[MissionEvent] ReleaseContainer：走基类支 —— `LiveOpsEvent.ReleaseContainer` 属 B33，本批没有落点 ⇒ 不做事");
        }

        // ============================================================ 挑战查询（账：`MissionEvent<TS,>.IsActive`）
        /// <summary>原版 `public bool IsActive(MissionChallenge challenge)`（`cpp2il/out_cpp2il_dev_ilspy/MissionEvent.cs:770`，
        /// **控制流全部内容** = `return SaveGame.challengeProgress.Any(x =&gt; x.Challenge == challenge);`）：
        /// 谓词实体 = `MissionEvent.__c__DisplayClass4_0._IsActive_b__0`（捕获 `challenge`）；
        /// 旁证 = 同类里 `GetProgressFor`（`:722`）用的是**同一个谓词** + `FirstOrDefault`。
        /// ⚠️ 原版 `SaveGame` 为 null ⇒ NRE；我们出声 + `false`（全工程口径，⛔ 不让游戏崩）。</summary>
        public bool IsActive(MissionChallenge challenge)
        {
            var s = SaveGame;
            if (s == null)
            {
                Debug.LogError("[MissionEvent] IsActive：没有存档（原版这一跳 NRE）⇒ false");
                return false;
            }
            if (s.challengeProgress == null || challenge == null) return false;
            // 🔴 **2026-10-28（B92_N8）按 `.c` 订正**：谓词实体
            // `MissionEvent.__c__DisplayClass4_0_object__object____IsActive_b__0.c` 的内容是
            // `String.Equals(*(progress + 0x38), challenge.GetID())`，而 `progress + 0x38` 是
            // **`challengeId`**（`dump.cs:61190`）⇒ 比的是**字符串 id**。原来我们比的是对象引用
            // （`p.Challenge == challenge`），只在「`Bind()` 把同一个对象注进去」时才等价 —— 换个
            // 实例就错（而且原版从不这么比）。
            return FindProgressIndex(s, challenge.id) >= 0;
        }

        /// <summary>原版 `protected MissionChallengeProgress GetProgressFor(MissionChallenge challenge)`
        /// （`decomp_trial/MissionEvent_object__object___GetProgressFor__102eb40.c`）：
        /// <code>
        /// return SaveGame.challengeProgress.FirstOrDefault(x =&gt; x.challengeId == challenge.GetID());
        /// </code>
        /// 谓词实体与 <see cref="IsActive"/> **是同一个函数体**（`DisplayClass3_0` vs `DisplayClass4_0`，
        /// 机器码相同 ⇒ ICF 折叠；两处 `.c` 逐字节一致）⇒ 同样**按字符串 id 比**。
        /// ⚠️ 原版 `SaveGame` 为 null ⇒ NRE；我们出声 + `null`（全工程口径）。</summary>
        public MissionChallengeProgress GetProgressFor(MissionChallenge challenge)
        {
            var s = SaveGame;
            if (s == null)
            {
                Debug.LogError("[MissionEvent] GetProgressFor：没有存档（原版这一跳 NRE）⇒ null");
                return null;
            }
            if (challenge == null) return null;
            int i = FindProgressIndex(s, challenge.id);
            return i >= 0 ? s.challengeProgress[i] : null;
        }

        /// <summary>原版 `public void InitializeChallenge(MissionChallenge challenge)`
        /// （`decomp_trial/MissionEvent_object__object___InitializeChallenge__102ec50.c`，控制流全文）：
        /// <code>
        /// challenge.OnProgressUpdate -= &lt;this.OnProgressUpdate&gt;;           // ★ 先【摘】
        /// foreach (var c in Data.challenges)                                 // 原版遍历活动定义里的挑战表
        ///     if (SaveGame.challengeProgress.Any(p =&gt; p.challengeId == challenge.ID)) {
        ///         challenge.OnProgressUpdate += &lt;this.OnProgressUpdate&gt;;   // 再【挂】
        ///         challenge.Initialize();                                    // 然后初始化（幂等闸）
        ///     }
        /// </code>
        /// 🔴 **2026-10-28（B92_N8）按 `.c` 落**：与 <see cref="EnableChallenge"/>（只有「挂 + Initialize」）
        /// 的差别有两处，都是原版真语义：① **先摘一次**（幂等，重复调用不会挂两份）；
        /// ② **只有「存档进度表里真有这条挑战」才挂**（不在表里 ⇒ 什么都不做）。
        /// ⚠️ `Data.challenges`（原版 `MissionData.challenges`）那一圈只影响「要不要再判一次」，
        /// 而判定条件本身是 `Any(存档进度表)` —— 与遍历无关 ⇒ 我们直接判那一句（语义等价，省掉一圈空转）。
        /// ⚠️ 原版 `SaveGame` 为 null ⇒ NRE；我们出声 + 不挂（全工程口径）。</summary>
        public void InitializeChallenge(MissionChallenge challenge)
        {
            if (challenge == null)
            {
                Debug.LogError("[MissionEvent] InitializeChallenge：challenge 是 null（原版这一跳 NRE）");
                return;
            }
            challenge.OnProgressUpdate -= OnChallengeProgressChanged;      // ★ 原版第一跳：先摘
            var s = SaveGame;
            if (s == null)
            {
                Debug.LogError("[MissionEvent] InitializeChallenge：没有存档（原版这一跳 NRE）⇒ 不挂监听");
                return;
            }
            if (s.challengeProgress == null || FindProgressIndex(s, challenge.id) < 0) return;   // 不在进度表里 ⇒ 不挂
            challenge.OnProgressUpdate += OnChallengeProgressChanged;
            challenge.Initialize();
        }

        /// <summary>原版 `public string LocalizedDescription(MissionChallenge challenge)`
        /// （`decomp_trial/MissionEvent_object__object___LocalizedDescription__102f190.c`，**5 语句 / 1 分支**）：
        /// <code>
        /// var p = GetProgressFor(challenge);
        /// if (p == null) throw;                     // 原版 NRE
        /// return p.GetLocalizedDescription();       // ← 转发（本方法自己不算任何文案）
        /// </code>
        /// 🔴 **2026-10-28（B92-N8c）按 `.c` 落**：原来账里的落点是 `DailyData.DailyDesc`（**另一个方法**）。</summary>
        public string LocalizedDescription(MissionChallenge challenge)
        {
            var p = GetProgressFor(challenge);
            if (p == null)
            {
                Debug.LogError("[MissionEvent] LocalizedDescription：取不到该挑战的进度（原版这一跳 NRE）⇒ 空串");
                return "";
            }
            return p.GetLocalizedDescription();
        }

        /// <summary>按字符串 id 在 `SaveGame.challengeProgress` 里找下标（原版那两个 lambda 的等价物：
        /// `p.challengeId == challenge.GetID()`）。找不到 ⇒ `-1`。</summary>
        static int FindProgressIndex(MissionPersistence s, string id)
        {
            if (s == null || s.challengeProgress == null || id == null) return -1;
            var a = s.challengeProgress;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != null && a[i].challengeId == id) return i;
            return -1;
        }
    }

    /// <summary>具体的那一个任务活动（原版 `public class MissionEvent : MissionEvent&lt;MissionPersistence, MissionData&gt;`，
    /// `dump.cs:60549`）。它本身不加重写任何东西（原版也是空的声明类）—— 真正的语义与行为全在
    /// `MissionEvent&lt;TS,TD&gt;` 与 `MissionData` 的子类（`DailyMissionData` / `MainMenuMissionData`）上。
    /// <para>⚠️ 原版 `MissionData` 是**抽象**的，所以这个具体类要能编过就得有一个具体的数据类型；
    /// 我们给一个最小实现 <see cref="SimpleMissionData"/>（原版的三个具体子类属别的账）。</para></summary>
    public class MissionEvent : MissionEvent<MissionPersistence, SimpleMissionData> { }

    /// <summary>最小的具体任务数据（原版没有这个类）。
    /// <para>🔶 自己设计（非原版）：`MissionData` 是抽象类，而原版的三个具体子类
    /// （`DailyMissionData` / `TimedMissionData` / `MainMenuMissionData`，判据 `dump.cs:60306/60393`）
    /// 各有自己的字段与账行、都不在本批 ⇒ 要给 `MissionEvent` 一个能 new 的数据类型只能自己补一个空的。
    /// 它**不新增任何字段或规则**。调度台 C2 2026-10-27 定。</para></summary>
    [Serializable]
    public class SimpleMissionData : MissionData { }

    // ============================================================ 原版同名方法落点（B92_N8 · 2026-10-28）
    // 🔴 **为什么单开这一节**：`MissionChallenge` 的 `initialized` / `IsInitialized` / `GetID()` /
    //    `OnInitialize()` 与 `MissionPersistence.IsChallengeComplete` 的**完整判据**放在这里，
    //    是为了**不动上面已经生效的段落** —— 本文件有 6 条 A 行按 `文件:行号` 落点
    //    （207 `GetBaseSave` / 234 `DisableChallenge` / 274 `UpdateSaveGame` / 334 `Expire` /
    //     369 `GetContainer` / 387 `ReleaseContainer`），往类体中间插成员会让它们整段漂移（铁律 18·b）。
    public abstract partial class MissionChallenge
    {
        /// <summary>原版 `private bool initialized; // 0x50`（`dump.cs:59645`）—— `Initialize()` 与
        /// `DisableChallenge()` **共用同一格**（两处 `.c` 读的都是 `param_1[10]`）。</summary>
        [NonSerialized] bool initialized;

        /// <summary>原版 `MissionChallenge.IsInitialized { get; }`（`get_IsInitialized`，读 `+0x50`）。</summary>
        public bool IsInitialized { get { return initialized; } }

        /// <summary>原版 `public virtual string GetID()`（`MissionChallenge.cs`，**Slot: 6**）。
        /// 体 = `decomp_full/MissionChallenge__GetID.c` —— 该产物首行是
        /// `&lt;&gt;f__AnonymousType0&lt;object&gt;__get_itemId`（ICF 折叠：`return *(param_1 + 0x10);`），
        /// 与 `id` 的字段偏移 **`+0x10`**（`dump.cs:59619`）逐字吻合 ⇒ 体 = `return this.id;`。
        /// 🔴 **2026-10-28（B92_N8）按 `.c` 落**：原来本类只有公开字段 `id`、**没有 `GetID()`**，
        /// 账的落点只能写「按下标取行（`DailyData.At`）」—— 那是另一件事。</summary>
        public virtual string GetID() { return id; }

        /// <summary>原版 `protected abstract void OnInitialize()`（ILSpy `MissionChallenge.cs:273` —— **基类没有体**）。
        /// 真体在各子类：`DailyLoginChallenge.OnInitialize` = `UpdateProgress(&lt;Execute&gt;b__3_0)`
        /// （`DailyLoginChallenge__OnInitialize.c`）；`MissionComplete.OnInitialize` = 注册战斗信号
        /// （`MissionComplete__OnInitialize.c`）。
        /// 🔴 **2026-10-28（B92-N8c）按 `.c` 改成 `abstract`**：这里原来是 `virtual { RaiseProgress(); }`
        /// （= 历史行为的兜底，**原版没有这一档**）。改 abstract 之后两个子类都必须真实现 —— 都已落。
        /// ⇒ 账 `日常_奖励.tsv:445` 因此按 §3·2(a)「原版体是 abstract」判 **D**。</summary>
        protected abstract void OnInitialize();
    }

    public partial class MissionPersistence
    {
        /// <summary>原版 `IsChallengeComplete` 的**完整两句**（`decomp_full/MissionPersistence__IsChallengeComplete.c`）：
        /// <code>
        /// if (completedChallenges.Contains(challenge.GetID())) return true;
        /// var p = challengeProgress.FirstOrDefault(x =&gt; x.challengeId == challenge.GetID());
        /// return p != null &amp;&amp; p.IsComplete();
        /// </code>
        /// 🔴 **2026-10-28（B92_N8）补上第②句**：原来只做了①，「进度表里那条完成了也算」整支漏掉。
        /// 谓词实体 = `MissionPersistence.__c__DisplayClass7_0___IsChallengeComplete_b__0.c`：
        /// 它比的是 `*(progress + 0x38)`（= **`challengeId`**，`dump.cs:61190`）与 `challenge.GetID()`
        /// ⇒ **按字符串 id 比，不是按对象引用**。</summary>
        bool IsChallengeCompleteCore(MissionChallenge challenge)
        {
            string id = challenge.GetID();
            if (completedChallengeIds != null && id != null && completedChallengeIds.Contains(id)) return true;
            var a = challengeProgress;
            if (a != null && id != null)
                for (int i = 0; i < a.Length; i++)
                {
                    var p = a[i];
                    if (p != null && p.challengeId == id) return p.IsComplete();   // FirstOrDefault ⇒ 只取第一条
                }
            return false;
        }
    }

    // ============================================================ B92-N8b（2026-10-28）· 原版同名方法落点（续）
    // 补 `MissionChallenge.UpdateProgress` ×2 / `ValidateMatchConditions` / `DailyLoginChallenge` 那一族，
    // 以及 `MissionChallengeProgress` 缺的三个基类字段（`collectedRewards` / `synced` / `resetProgress`）。
    // ⚠️ 全部落在**文末 partial 块**里（同上一条的理由：本文件有 6 条 A 行按行号落点）。
    public abstract partial class MissionChallenge
    {
        /// <summary>原版 `private bool loopable; // 0x2D`（`dump.cs:59636`）。**`IsComplete()` 的第一道闸**：
        /// 循环挑战永不算「完成」（判据 → `MissionChallengeProgress__IsComplete.c` 第①支）。</summary>
        public bool loopable;

        /// <summary>原版 `private bool allowOnTutorials; // 0x34`（`dump.cs:59640`）。教程局算不算这条挑战。</summary>
        public bool allowOnTutorials;

        /// <summary>原版 `private bool allowOnDuels; // 0x35`（`dump.cs:59642`）。决斗局（`playMode == 1`）算不算。</summary>
        public bool allowOnDuels;

        /// <summary>原版 `private PlayModes[] playModeBlocklist; // 0x38`（`dump.cs:59644`）。**非空即优先**（见 `ValidateMatchConditions`）。</summary>
        public int[] playModeBlocklist;

        /// <summary>原版 `private PlayModes[] playModeAllowlist; // 0x40`（`dump.cs:59646`）。比 blocklist **更优先**。</summary>
        public int[] playModeAllowlist;

        // ── 战斗态注入口（原版直接问 `BattleManager`；本层不依赖战斗程序集）────────────────
        /// <summary>原版 `ValidateMatchConditions` 的第一跳是取 `BattleManager`，再读
        /// `matchData.playMode`（`matchData + 0x18`）/ `IsReplayMatch()` / `IsTutorialMatch()`。
        /// ⚠️ **这不是「自己设计」**（2026-10-28 B92-N8c 复核：原版 `.c` 第一句就是
        /// `if (BattleManager == null) return true;` ⇒ **`null` ⇒ 放行是原版行为**，见
        /// `MissionChallenge__ValidateMatchConditions.c:31-33`：`op_Implicit(BattleManager)` 为假 ⇒ `return true`）。
        /// 我们只是把「读 `BattleManager` 的三个字段」
        /// 换成三个注入口 —— 结构不同、**语义相同**（`CLAUDE.md` 18·0「⛔ 不许打标记」第 ② 类：实现风格）。
        /// 因此这里**不挂 `🔶 自己设计`**；`null` 默认 = 原版「没有 BattleManager」那一支。</summary>
        public static Func<int> MatchPlayMode;
        /// <inheritdoc cref="MatchPlayMode"/>
        public static Func<bool> MatchIsReplay;
        /// <inheritdoc cref="MatchPlayMode"/>
        public static Func<bool> MatchIsTutorial;

        /// <summary>原版 `private bool ValidateMatchConditions()`（`decomp_full/MissionChallenge__ValidateMatchConditions.c`，
        /// 45 语句 / 23 分支 —— 下面是**去掉静态初始化样板后的完整判定树**）：
        /// <code>
        /// if (BattleManager == null) return true;                 // ★ 没在打局 ⇒ 放行
        /// if (IsReplayMatch()) return false;                      // ★ 回放局 ⇒ 一律不算
        /// if (playMode == 1) return allowOnDuels;                 // ★ 决斗局只看 +0x35
        /// if (IsTutorialMatch()) return allowOnTutorials;         // ★ 教程局只看 +0x34
        /// if (allowlist != null &amp;&amp; allowlist.Length &gt; 0) return allowlist.Contains(playMode);   // +0x40 优先
        /// if (blocklist == null || blocklist.Length &lt; 1) return true;
        /// return !blocklist.Contains(playMode);                   // +0x38 兜底
        /// </code>
        /// ⚠️ 原版 `Contains` 是 `Enumerable.Contains&lt;PlayModes&gt;`（值比较）⇒ 我们用 `Array.IndexOf`。</summary>
        public bool ValidateMatchConditions()
        {
            if (MatchPlayMode == null) return true;
            int playMode = MatchPlayMode();
            if (MatchIsReplay != null && MatchIsReplay()) return false;
            if (playMode == 1) return allowOnDuels;
            if (MatchIsTutorial != null && MatchIsTutorial()) return allowOnTutorials;
            if (playModeAllowlist != null && playModeAllowlist.Length > 0)
                return System.Array.IndexOf(playModeAllowlist, playMode) >= 0;
            if (playModeBlocklist == null || playModeBlocklist.Length < 1) return true;
            return System.Array.IndexOf(playModeBlocklist, playMode) < 0;
        }

        /// <summary>原版 `protected void UpdateProgress(Func&lt;ChallengeProgress,bool&gt; updateFunction)`
        /// （`cpp2il/out_cpp2il_dev_ilspy/MissionChallenge.cs:297` 骨架 + 谓词实体
        /// `decomp_full/MissionChallenge.__c__DisplayClass34_0___UpdateProgress_b__0.c`）：
        /// <code>
        /// if (!ValidateMatchConditions()) return;                       // ★ 模式闸
        /// var handler = OnProgressUpdate;                                // +0x48
        /// handler?.Invoke(new ChallengeUpdatePayload(this, progress =&gt; {
        ///     bool changed = updateFunction(progress);
        ///     progress.synced = !changed;                                // ★ +0x18：改了 ⇒ 待上传
        ///     return changed;
        /// }));
        /// </code>
        /// 🔴 **挑战自己不改进度** —— 它把「怎么改」打包广播出去，由订阅方（`MissionEvent`）去改
        /// （这条口径见 `MissionEvent.cs` 文件头）。</summary>
        protected void UpdateProgress(Func<MissionChallengeProgress, bool> updateFunction)
        {
            if (!ValidateMatchConditions()) return;
            var handler = OnProgressUpdate;
            var payload = new MissionChallengeUpdatePayload(this, progress =>
            {
                bool changed = updateFunction(progress);
                progress.synced = !changed;
                return changed;
            });
            if (handler != null) handler(payload);
        }

        /// <summary>原版 `protected void UpdateProgress(Action&lt;ChallengeProgress&gt; updateFunction)`
        /// （`decomp_full/MissionChallenge__UpdateProgress.c` + `…DisplayClass36_0…__UpdateProgress_b__0.c`）：
        /// <code>
        /// UpdateProgress(progress =&gt; {
        ///     int before = progress.CurrentValue;
        ///     updateFunction(progress);
        ///     return before != progress.CurrentValue;      // ★ 变了才算一次推进
        /// });
        /// </code>
        /// （那个 `.c` 的文件名与**另一个重载**撞了 —— `decomp_full` 同名重载只留一份，
        ///  通用纪律 §六·11；这里以 ILSpy 骨架 + `b__36_0` 的真体为准。）</summary>
        protected void UpdateProgress(Action<MissionChallengeProgress> updateFunction)
        {
            UpdateProgress(progress =>
            {
                int before = progress.currentValue;
                updateFunction(progress);
                return before != progress.currentValue;
            });
        }
    }

    public partial class MissionChallengeProgress
    {
        /// <summary>原版基类 `ChallengeProgress.collectedRewards // 0x10`（`dump.cs:59765`）—— 已领的奖励条数。
        /// `IsComplete()` 的第③句拿它比 `AvailableRewards()` 的条数。</summary>
        public int collectedRewards;

        /// <summary>原版基类 `ChallengeProgress.synced // 0x18`（`dump.cs:59768`）——
        /// `UpdateProgress` 的包装器写 `!changed`（= 进度变了就标记「待上传」）。</summary>
        public bool synced;

        /// <summary>原版基类 `ChallengeProgress.resetProgress // 0x28`（`dump.cs:59772`）——
        /// `DailyLoginChallenge` 的「断天」那一支会把它置 `true`（`b__3_0.c`：`*(char *)(progress + 0x28) = 1`）。</summary>
        public bool resetProgress;

        /// <summary>原版 `ChallengeProgress.customData // 0x20`（`Dictionary&lt;string,JToken&gt;`）。
        /// ⚠️ **简化**：本仓没有 `JToken` 这一层，这一格只存**毫秒时间戳**（唯一用到它的键是
        /// `DailyLoginChallenge` 的 `lastUpdate`）⇒ 类型收窄成 `Dictionary&lt;string,long&gt;`（行为等价：
        /// 原版每次都是 `ToObject&lt;long&gt;()` 取出来当毫秒用）。</summary>
        public Dictionary<string, long> customData;
    }

    /// <summary>原版 `DailyLoginChallenge : MissionChallenge` —— 「每日登录」这条挑战。
    /// 行为全在**两个入口 + 一个谓词**里：
    /// <para>`Execute()`（`decomp_full/DailyLoginChallenge__Execute.c`，与 `OnInitialize` **是同一份机器码**，
    /// ICF 折叠 —— 两个 `.c` 的首行都印成 `DailyLoginChallenge__OnInitialize`）：
    /// <code>UpdateProgress(new Predicate&lt;ChallengeProgress&gt;(this.&lt;Execute&gt;b__3_0));</code></para>
    /// <para>谓词 `b__3_0`（`decomp_full/DailyLoginChallenge___Execute_b__3_0.c`）的判定树：
    /// <code>
    /// last = customData["lastUpdate"]（毫秒）
    /// if (last 的日期 &gt;= 今天) { 警告「同日重复执行」; return false; }            // ★ 同日幂等
    /// if (RequirementMet &amp;&amp; last 存在 &amp;&amp; (今天 - last 的日期).TotalDays &gt; 1) {
    ///     customData["lastUpdate"] = 今天0点的毫秒; CurrentValue = 1;
    ///     collectedRewards = 0; resetProgress = true; return true;               // ★ 断天 ⇒ 归 1
    /// }
    /// customData["lastUpdate"] = 今天0点的毫秒; CurrentValue = CurrentValue + 1; return true;   // ★ 跨天 +1
    /// </code></para>
    /// 🔴 「**同日幂等 / 断天归 1 / 跨天 +1**」这三条与 `DailyData.RecordLogin` 的口径**同构** ——
    /// 差别只在数据落点（那边是 `DailyData` 的静态字段 + 本地存档，这边是挑战自己的 `customData`）。
    /// ⚠️ 原版的「今天」取 `TimeHelper.ServerDateTime`；**我们没有服务器** ⇒ 本机时钟（本项目允许的偏离）。</summary>
    public partial class DailyLoginChallenge : MissionChallenge
    {
        /// <summary>原版 `customData` 里那个键（`DAT_184257ab8`；本仓按语义取同名 `lastUpdate`）。</summary>
        public const string LastUpdateKey = "lastUpdate";

        /// <summary>时钟（原版 = `TimeHelper.ServerDateTime`；本机时钟，见类注释）。</summary>
        public Func<System.DateTime> Now = () => System.DateTime.Now;

        /// <summary>原版谓词里 `(char)param_1[0xb]` 那一格（「今天的登录条件已满足」）——
        /// 原版读挑战自己的字段，本仓由装载方注入（`null` = 未知 ⇒ 只走「跨天 +1」那一支）。</summary>
        public Func<bool> RequirementMet;

        /// <summary>原版 `public void Execute()`：跑一次日登推进。</summary>
        public void Execute() { UpdateProgress(Advance); }

        /// <summary>原版 `protected override void OnInitialize()`：初始化即跑一次（**同一份**体）。</summary>
        protected override void OnInitialize() { UpdateProgress(Advance); }

        /// <summary>`b__3_0` 的判定树（逐句，见类注释）。</summary>
        bool Advance(MissionChallengeProgress p)
        {
            var today = Now().Date;
            System.DateTime last;
            bool hasLast = TryGetLast(p, out last);
            if (hasLast && last >= today)
            {
                UnityEngine.Debug.LogWarning("[DailyLoginChallenge] 同日重复执行（`" + GetID() + "`）："
                                           + "lastUpdate 的日期 " + last.ToString("yyyy-MM-dd")
                                           + " >= 今天 " + today.ToString("yyyy-MM-dd") + " ⇒ 不改进度（原版同）");
                return false;
            }
            if (hasLast && RequirementMet != null && RequirementMet() && (today - last).TotalDays > 1d)
            {
                SetLast(p, today);
                p.currentValue = 1;
                p.collectedRewards = 0;        // `.c`：`param_2[2] = 0`（+0x10）
                p.resetProgress = true;        // `.c`：`*(char *)(param_2 + 5) = 1`（+0x28）
                return true;
            }
            SetLast(p, today);
            p.currentValue = p.currentValue + 1;
            return true;
        }

        static bool TryGetLast(MissionChallengeProgress p, out System.DateTime last)
        {
            last = default(System.DateTime);
            long ms;
            if (p == null || p.customData == null) return false;
            if (!p.customData.TryGetValue(LastUpdateKey, out ms) || ms <= 0) return false;
            last = MillisToDate(ms).Date;
            return true;
        }

        static void SetLast(MissionChallengeProgress p, System.DateTime day)
        {
            if (p.customData == null) p.customData = new Dictionary<string, long>();
            p.customData[LastUpdateKey] = DateToMillis(day);
        }

        /// <summary>= `TimeHelper.MillisToDateTime`（Unix 毫秒 → 本地时间）。</summary>
        static System.DateTime MillisToDate(long ms)
        {
            return new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).AddMilliseconds(ms).ToLocalTime();
        }

        /// <summary>= `TimeHelper.MillisSinceEpoch(this.DateTime.Date)`。</summary>
        static long DateToMillis(System.DateTime day)
        {
            return (long)(day.ToUniversalTime() - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;
        }
    }

    // ============================================================ B92-N8b（续）· `MissionComplete` 那一族
    public abstract partial class MissionChallenge
    {
        /// <summary>原版 `protected void UpdateProgress(int value, bool shouldOverride = false)`
        /// （ILSpy `MissionChallenge.cs:337`）：先造一个 setter，再套进 `Action` 那一档 ——
        /// <code>
        /// Action&lt;ChallengeProgress&gt; set = p =&gt; p.CurrentValue = shouldOverride ? value : value + p.CurrentValue;
        /// UpdateProgress(set);      // ⇒ 走 Action 重载（只有「变了」才算一次推进）
        /// </code>
        /// ⚠️ ILSpy 把那一档反编译成了内联的 `Func` 版（`before != after`），语义与 `Action` 重载一致。</summary>
        protected void UpdateProgress(int value, bool shouldOverride = false)
        {
            UpdateProgress(progress =>
            {
                progress.currentValue = shouldOverride ? value : value + progress.currentValue;
            });
        }
    }

    /// <summary>原版 `IMission`（任务信号里那个**标记接口**，`dump.cs` 一族）。
    /// 本仓只在 <see cref="MissionCompleteSignal"/> 里用到它的 `GetType()`（= 判「这是哪种模式的任务」）。</summary>
    public interface IMission { }

    /// <summary>原版 `MissionCompleteSignal : IBattleSignal`（`dump.cs`，两个字段逐个抄）：
    /// `mission // 0x10` · `challenge // 0x18`。</summary>
    public class MissionCompleteSignal : ILiveOpsSignal
    {
        public readonly IMission mission;
        public readonly MissionChallenge challenge;
        public MissionCompleteSignal(IMission mission, MissionChallenge challenge)
        { this.mission = mission; this.challenge = challenge; }
    }

    /// <summary>原版 `MissionComplete : MissionChallenge` —— 「打完一局 ⇒ 给匹配的挑战记 1 次」。
    /// 判据 `decomp_full/MissionComplete__OnMissionComplete.c`（24 语句 / 8 分支，下面是**完整判定树**）：
    /// <code>
    /// if (obj == null || obj.challenge == null) throw;                  // 原版 NRE
    /// if (obj.challenge.GetID() == this.GetID()) return;                // ★ 同一条挑战 ⇒ 跳过
    /// if (!anyMissionType) {                                            // +0x58
    ///     var t = obj.mission.GetType();
    ///     if (!missionTypes.Contains(t)) return;                        // +0x60：只认白名单里的模式
    /// }
    /// UpdateProgress(1);                                                // ★ 推进 1（不是覆盖）
    /// </code>
    /// ⚠️ 原版 `missionTypes` 是 `List&lt;TypeReference&gt;`（TypeReferences 插件，本仓没有）⇒ 收窄成
    /// `List&lt;Type&gt;`（原版那一跳就是 `TypeReference.op_Implicit(mission.GetType())` 再 `Contains`，语义相同）。</summary>
    public class MissionComplete : MissionChallenge
    {
        /// <summary>原版 `+0x58`：`true` = 任何模式都算（跳过白名单那一支）。</summary>
        public bool anyMissionType;

        /// <summary>原版 `+0x60`：认哪些 `IMission` 实现（`anyMissionType == false` 时才看它）。</summary>
        public List<Type> missionTypes;

        /// <summary>原版 `protected override void OnInitialize()`（`decomp_full/MissionComplete__OnInitialize.c`，**全文两句**）：
        /// <code>
        /// Signal.Register&lt;MissionCompleteSignal&gt;(this.OnMissionComplete);
        /// </code>
        /// 🔴 **这一跳就是「谁会调 `OnMissionComplete`」的答案** —— 初始化时把处理函数挂到战斗信号上。
        /// 我们的信号总线 = `LiveOpsSignal`（`Shell/LiveOpsFilter.cs`，与 `Scaffold.Core.Events.Signal` 同角色）。
        /// ⚠️ 本仓**还没有人 `Raise` 过 `MissionCompleteSignal`**（战斗侧未接）⇒ 订阅上也不会被调（如实记）。
        /// ⚠️ 原版另有 `OnReset` 也注册一个（`MissionComplete__OnReset.c`，另一个信号表），本批只落 `OnInitialize`。</summary>
        protected override void OnInitialize()
        {
            LiveOpsSignal.Register<MissionCompleteSignal>(OnMissionComplete);
        }

        /// <summary>原版 `public void OnMissionComplete(MissionCompleteSignal obj)`，逐句见类注释。</summary>
        public void OnMissionComplete(MissionCompleteSignal obj)
        {
            if (obj == null || obj.challenge == null)
            {
                UnityEngine.Debug.LogError("[MissionComplete] OnMissionComplete：信号或挑战是 null（原版这一跳 NRE）⇒ 不算");
                return;
            }
            if (string.Equals(obj.challenge.GetID(), GetID())) return;      // ★ 同一条 ⇒ 跳过
            if (!anyMissionType)
            {
                if (obj.mission == null)
                {
                    UnityEngine.Debug.LogError("[MissionComplete] OnMissionComplete：mission 是 null（原版这一跳 NRE）⇒ 不算");
                    return;
                }
                if (missionTypes == null || !missionTypes.Contains(obj.mission.GetType())) return;
            }
            UpdateProgress(1);                                              // ★ 推进 1
        }
    }

    // ============================================================ B92-N8b（续）· 挑战的描述文案
    public abstract partial class MissionChallenge
    {
        /// <summary>原版描述词条的**前缀常量** `DAT_184236888`（`MissionChallenge__GetLocalizedDescription.c`
        /// 与 `MissionChallengeProgress__GetLocalizedDescription.c` 用的是**同一个**常量）。
        /// 🔴 **本机读不出那个字面量**（是 metadata 指针）；这里按**既有账判据**取 `"Missions/"`
        /// （账 `日常_奖励.tsv:441` 的判据要点「原版 LocalizedString `Missions/<id>`」+ `Core/Loc.cs` 里
        /// `Missions/Daily_*` 那一族键名逐条对得上）。⛔ 这不是原版字面量的证据，是**沿用既有账口径**。</summary>
        public const string DescriptionKeyPrefix = "Missions/";

        /// <summary>原版 `public LocalizedString GetLocalizedDescription()`（`decomp_full/MissionChallenge__GetLocalizedDescription.c`，
        /// **全文四条语句**）= `return new LocalizedString(&lt;前缀&gt; + GetID());`
        /// —— **不带 `string.Format`**（填参数那一跳在 `MissionChallengeProgress.GetLocalizedDescription` 里）。
        /// ⚠️ 本仓没有 `LocalizedString` 这一层，`Loc.T(键)` 返回的就是**未填参的模板**（与 `LocalizedString` 等价）。
        /// <para>🔴 **2026-10-28（B92_N8b）就地订正 N2d 的记法**：N2d 记这一行「原版体 42 语句 / 3 分支」，
        /// 那是 `MissionChallengeProgress__GetLocalizedDescription.c`（**另一个类**的同名方法）；
        /// 本行自己的 `.c` 只有四条语句。</para></summary>
        public virtual string GetLocalizedDescription() { return Loc.T(DescriptionKeyPrefix + GetID()); }
    }

    // ============================================================ B92-N8b（续）· 日登挑战的「下一次刷新时刻」
    public partial class DailyLoginChallenge
    {
        /// <summary>原版 `GetNextUpdate` 里读的 `Data.challenges.Count`（`*(*(this+0x18) + 0x18)` ——
        /// `this+0x18` 是 `LiveOpsEvent.Data`）。本仓这一层不持有 `Data` ⇒ 由装载方注入；
        /// **`0` 等价于「没有挑战表」**（那时 `count &lt;= currentValue` 恒真 ⇒ 走「今天 0 点」那一支，
        /// 与原版 `challenges` 为空的形状一致）。</summary>
        public int ChallengeCount;

        /// <summary>原版 `public DateTime GetNextUpdate(ChallengeProgress progress)`
        /// （`decomp_full/DailyLoginChallenge__GetNextUpdate.c`，28 语句 / 10 分支）：
        /// <code>
        /// if (!loopable) {                                        // +0x2D
        ///     if (progress == null) throw;
        ///     if (Data.challenges.Count &lt;= progress.CurrentValue) return ServerDateTime.Date;   // ★ 跑完了 ⇒ 今天 0 点
        /// }
        /// if (progress != null &amp;&amp; progress.customData != null) {
        ///     DateTime dt = 读 customData["lastUpdate"] 成功 ? MillisToDateTime(v) : ServerDateTime;
        ///     return dt.Date.AddDays(1);                          // ★ 否则 ⇒ 次日 0 点
        /// }
        /// throw;                                                  // 原版 NRE
        /// </code>
        /// ⚠️ 原版 `ServerDateTime` = `TimeHelper.ServerDateTime`；我们没有服务器 ⇒ 本机时钟。</summary>
        public System.DateTime GetNextUpdate(MissionChallengeProgress progress)
        {
            if (!loopable)
            {
                if (progress == null)
                {
                    UnityEngine.Debug.LogError("[DailyLoginChallenge] GetNextUpdate：progress 是 null（原版这一跳 NRE）⇒ 退回次日 0 点");
                    return Now().Date.AddDays(1);
                }
                if (ChallengeCount <= progress.currentValue) return Now().Date;      // ★ 跑完 ⇒ 今天 0 点
            }
            if (progress == null || progress.customData == null)
            {
                UnityEngine.Debug.LogError("[DailyLoginChallenge] GetNextUpdate：没有 progress/customData（原版这一跳 NRE）⇒ 退回次日 0 点");
                return Now().Date.AddDays(1);
            }
            System.DateTime last;
            if (!TryGetLast(progress, out last)) last = Now();                       // 读不到 ⇒ 用「现在」
            return last.Date.AddDays(1);
        }
    }

    // ============================================================ B92-N8c（2026-10-28）· 里程碑 / 描述 / 完成判定
    /// <summary>原版 `ChallengeMilestone`（`dump.cs:59731`，三个字段逐个抄）：
    /// `difficulty // 0x10`（`ChallengeDifficulty`：`Easy=1 / Medium=2 / Hard=4`，见 `dump.cs:59747`）·
    /// **`targetValue // 0x14`**（这一档的门槛值）· `rewards // 0x18`（`RewardInfo[]`，本仓只透传）。</summary>
    [Serializable]
    public class ChallengeMilestone
    {
        public int difficulty;
        public int targetValue;
        public object[] rewards;
    }

    public abstract partial class MissionChallenge
    {
        /// <summary>原版 `private ChallengeType challengeType; // 0x28`（`dump.cs:59624`）：
        /// `DifficultyBased = 1 / MilestonesBased = 2 / DailyLogin = 4`（`dump.cs:59736-59740`）。
        /// **`AvailableRewards()` 只有 `== 1` 才筛里程碑**（见 `MissionChallengeProgress.AvailableRewards`）。</summary>
        public int challengeType;
    }

    public partial class MissionChallengeProgress
    {
        /// <summary>原版 `IEnumerable&lt;ChallengeMilestone&gt; AvailableRewards()`
        /// （`decomp_full/MissionChallengeProgress__AvailableRewards.c`，全文就三跳）：
        /// <code>
        /// var c = AssetLocator.GetAsset(this.challengeId);            // 原版按 +0x38 取挑战；我们 = 注入的 `Challenge`
        /// if (c == null) throw;                                        // 原版 NRE
        /// if (c.challengeType != ChallengeType.DifficultyBased /* 1 */) return c.rewards;   // ★ 默认支：**全部**里程碑
        /// return c.rewards.Where(&lt;实例方法委托&gt;);                                          // 只有 DifficultyBased 才筛
        /// </code>
        /// 🔴 **唯一残差 = `challengeType == 1` 那一支的筛选谓词**：`.c` 里是
        /// `new Predicate&lt;ChallengeMilestone&gt;(this, DAT_1842b0c48)` —— **实例方法委托**（不是 `&lt;&gt;c` 静态 lambda），
        /// 所以 `decomp_full` 里**没有** `__c___AvailableRewards_b__NN.c` 可查，本机也拿不到那个方法名。
        /// ⇒ 那一支**出声一次 + 退回全集**（⛔ 不猜「按 difficulty 过滤」）。
        /// ✅ **本仓该分支恒不命中**：我们只有 `DailyLoginChallenge`（`challengeType = 4`）与 `MissionComplete`（默认 0）。</summary>
        public IEnumerable<ChallengeMilestone> AvailableRewards()
        {
            var c = Challenge;
            if (c == null)
            {
                Debug.LogError("[MissionChallengeProgress] AvailableRewards：取不到挑战（原版走 AssetLocator，这一跳 NRE）⇒ 空表");
                return System.Array.Empty<ChallengeMilestone>();
            }
            var a = c.rewards;
            if (a == null) return System.Array.Empty<ChallengeMilestone>();
            if (c.challengeType != 1) return a;
            SayUnfilteredOnce();
            return a;
        }

        /// <summary>「DifficultyBased 那一支的筛选谓词未解」只说一次（每次重建都吼会淹掉日志）。</summary>
        static bool _saidUnfiltered;
        static void SayUnfilteredOnce()
        {
            if (_saidUnfiltered) return;
            _saidUnfiltered = true;
            Debug.LogWarning("[MissionChallengeProgress] AvailableRewards：`challengeType == DifficultyBased(1)` 那一支的"
                           + "筛选谓词是**实例方法委托**（`Predicate<ChallengeMilestone>(this, DAT_1842b0c48)`），"
                           + "本机无符号可解 ⇒ **退回全集**（未按难度过滤）。原版这一支只服务 DifficultyBased 挑战，"
                           + "本仓暂无该类挑战。");
        }

        /// <summary>原版 `public int MaxValue { get; }`（`decomp_full/MissionChallengeProgress__get_MaxValue.c`）：
        /// `AvailableRewards()` 里取 `targetValue`（谓词实体 `…__c___get_MaxValue_b__19_0.c` = `m =&gt; m.targetValue`），
        /// 一条都没有 ⇒ **0**。
        /// ⚠️ 那一跳的 LINQ 是 `Last`/`Max` 两可（`.c` 只看到「取一个元素再读 `+0x14`」）；
        /// 里程碑按 `targetValue` 升序排列时两者同值 —— 这里取**最后一个**（`Last`）。</summary>
        public int MaxValue
        {
            get
            {
                int v = 0;
                foreach (var m in AvailableRewards()) v = m.targetValue;
                return v;
            }
        }

        /// <summary>原版 `bool IsComplete()`（`decomp_full/MissionChallengeProgress__IsComplete.c`，15 语句 / 4 分支）：
        /// <code>
        /// var c = AssetLocator.GetAsset(this.challengeId);              // 原版按 +0x38 取挑战；我们 = 注入的 `Challenge`
        /// if (c == null) throw;                                          // 原版 NRE
        /// if (c.loopable) return false;                                  // ★ +0x2D：循环挑战永不算完成
        /// return Count(AvailableRewards()) &lt;= this.collectedRewards;      // ★ +0x10：可领条数 ≤ 已领条数
        /// </code>
        /// 🔴 **2026-10-28（B92-N8c）按 `.c` 落**：原来本方法只有 `completedAt != 0` 那一格（最保守的近似，
        /// 而且与 `.c` 的算式**不是一回事**）。`MissionPersistence.IsCompleted` / `IsChallengeComplete`
        /// 都读这一格 ⇒ 一并按原版改口径。</summary>
        public bool IsCompleteCore()
        {
            var c = Challenge;
            if (c == null)
            {
                Debug.LogError("[MissionChallengeProgress] IsComplete：取不到挑战（原版走 AssetLocator，这一跳 NRE）⇒ false");
                return false;
            }
            if (c.loopable) return false;
            int n = 0;
            foreach (var _ in AvailableRewards()) n++;
            return n <= collectedRewards;
        }

        /// <summary>原版 `public string GetLocalizedDescription()`（`decomp_full/MissionChallengeProgress__GetLocalizedDescription.c`）：
        /// <code>
        /// var c = AssetLocator.GetAsset(this.challengeId);
        /// if (c == null) throw;
        /// string s = String.Concat(&lt;前缀常量&gt;, c.GetID());          // 与 MissionChallenge 那一份同一个常量
        /// return String.Format(new LocalizedString(s), this.MaxValue);  // ★ 这里才填 {0}
        /// </code>
        /// ⚠️ 前缀常量见 `MissionChallenge.DescriptionKeyPrefix`（取值理由写在那儿）。</summary>
        public string GetLocalizedDescription()
        {
            var c = Challenge;
            if (c == null)
            {
                Debug.LogError("[MissionChallengeProgress] GetLocalizedDescription：取不到挑战（原版这一跳 NRE）⇒ 空串");
                return "";
            }
            return string.Format(Loc.T(MissionChallenge.DescriptionKeyPrefix + c.GetID()), MaxValue);
        }
    }
}
