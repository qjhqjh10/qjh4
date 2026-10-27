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
    public abstract class MissionChallenge
    {
        public string id;
        public object[] rewards;      // 原版 `ChallengeMilestone[]`（里程碑表），本批只透传

        /// <summary>原版 `+0x48`：挑战进度变了就喊一声（`MissionEvent` 是**唯一**订阅方）。</summary>
        public event Action<MissionChallengeUpdatePayload> OnProgressUpdate;

        /// <summary>原版 `Initialize()`：重算一次并广播（`UpdateSaveGame` 里「新来的挑战」会调它一次）。</summary>
        public virtual void Initialize() { RaiseProgress(); }

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
    public class MissionChallengeProgress
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

        /// <summary>原版 `MissionChallengeProgress.IsComplete()`（`ChallengeProgress.IsComplete`）：
        /// 判据取「已经完成的时刻记上了」这一支（`completedAt != 0`）——
        /// ⚠️ 原版那个方法体本批**没读**，这里只用最保守的那一格（`completedAt != 0`），
        /// `MissionPersistence.IsCompleted` 那条判据只依赖它。⛔ 别拿它去当「达标」判据。</summary>
        public bool IsComplete() { return completedAt != 0; }
    }

    // ============================================================ 存档（原版 `MissionPersistence`）
    /// <summary>原版 `MissionPersistence : LiveOpsEventPersistence, IBeforeRead`（`dump.cs:61024-61050`，
    /// 五个字段逐个抄）。`ISave` 的泛型参数就是它。</summary>
    [Serializable]
    public class MissionPersistence : LiveOpsSave
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

        /// <summary>原版 `bool IsChallengeComplete(MissionChallenge challenge)`
        /// （`MissionPersistence__IsChallengeComplete.c`）：在 `completedChallenges` 字典里查
        /// `challenge.GetID()`（那一跳是 `challenge.&lt;+0x198&gt;()` = `GetID()`，再 `ContainsKey`）。</summary>
        public bool IsChallengeComplete(MissionChallenge challenge)
        {
            if (challenge == null || completedChallengeIds == null) return false;
            return completedChallengeIds.Contains(challenge.id);
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
}
