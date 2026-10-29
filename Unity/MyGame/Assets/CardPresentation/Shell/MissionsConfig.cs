// MissionsConfig.cs —— 原版 `MissionsConfig : IConfigData`（任务配置的数据容器）+ 它的 `CleanUp()`
//
// ============================ 这是什么、判据在哪 ============================
// 账 `日常_奖励:524`（`MissionsConfig.CleanUp()`）。判据（2026-10-29 `B98_H5` 已解全，本文件逐句照做）：
//   · 字段表   = `d:/2/tools/il2cpp_out/dump.cs` TypeDefIndex 1488（`MissionsConfig`）
//                + `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/MissionsConfig.cs`（ILSpy，体可读）
//   · `CleanUp` = `d:/2/tools/decomp_full/MissionsConfig__CleanUp.c`（全文 80 行）+ 上面那份 ILSpy
//   · 字段偏移 = `tutorialMissionRef +0x10` · `dailyLoginRef +0x18` · `configs +0x20` ·
//                `challenges +0x28` · `challengeLists +0x30`
//
// 【`CleanUp()` 的方法体 —— `.c:42-76` 逐句】
//     if (challenges(+0x28) == null) throw NRE;
//     foreach (ch in challenges) {
//         if (ch == null) throw NRE;
//         if (ch.initialized(+0x50) == false) continue;
//         ch.OnReset();                       // ★ 元素自己的虚槽 8（`(0x1b8-0x138)/0x10`）
//         ch.OnProgressUpdate(+0x48) = <>c__9__30_0;   // ★ **直接写委托字段**（不是 `-=`），
//                                                      //   目标 = `<DisableChallenge>b__30_0`，RVA 0x4B33B0
//                                                      //   = 全局共享空方法桩（机器码 `ret 0`）⇒ **空 lambda**
//         ch.initialized = false;
//     }
//   🔴 **`+0x48` 是「整格替换」**：`.c` 里是 `plVar2[9] = lVar4; write_barrier(...)` —— 直接把委托字段指向
//      那个空 lambda，**不是** `-=`（也就不需要知道原来的订阅者是谁）。语义 = 「把这条挑战的所有进度订阅一次摘光」。
//   🔴 **虚槽 8 = `protected abstract void OnReset()`**（`dump.cs` MissionChallenge 那一段 `Slot: 8` 紧跟着
//      一句 `protected abstract void OnReset();`）。⛔ 不是 `OnInitialize`（那是 Slot 9）、不是 `DisableChallenge`。
//
// ⚠️ **三处如实差异（⛔ 不是「已经一样」）**：
//   ① **基类没有槽 8 这个抽象位**。本仓子类把 `OnReset()` 写成**普通公有方法**（非 `override`），
//      既有约定写在 `Shell/TroopsKilledCount.cs:70` / `Shell/EnergySpent.cs:139`：
//      「基类 `MissionChallenge` 没有 `OnReset` 的抽象位 ⇒ 不是 override，由调用方显式调」。
//      若在这里给基类补一个 `virtual OnReset()`，子类那个同名方法会**隐藏**它（CS0108），而
//      `CleanUp` 拿 `MissionChallenge` 引用去调只会命中基类空体 = **静默不重置**。
//      故这里按「运行时类型上那个 `OnReset`」分发（`GetType().GetMethod("OnReset")`）—— 那是
//      **等价于虚槽 8 的分发**，不是新规则。
//   ② 原版 `challenges == null` / 元素为 null 是 **NRE**；我们**出声一次 + 返回**（全工程口径：坏数据不让游戏崩）。
//   ③ 原版 `ChallengeList : IdentifiableSO`（ScriptableObject）；本工程没有 `IdentifiableSO` 基类 ⇒
//      见 `Shell/ChallengeList.cs`。
//
// ⚠️ **数据仍然在远端**：`challenges` / `challengeLists` / `configs` 由 PlayFab Title Data
//    （`TitleDataKey.Challenges = 17` / `ChallengesNew = 18`）下发，本机 0 份 SO ⇒ 这个类型在本机
//    **永远是空的**（`ConfigProvider<MissionsConfig>` 取不到 key 时只 new 一个默认实例 + 打一行日志）。
//    ⇒ 今天 `CleanUp()` 的循环体**一次都不会进**。这不是「没做」，是「没有数据」（铁律 11 例外①）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `MissionsConfig : IConfigData`（`dump.cs` TypeDefIndex 1488，五个字段逐个抄）。
    /// <para>⚠️ 一处**可访问性**差异（⛔ 不是行为差异）：`MissionConfig.type` 的类型是本仓
    /// `internal enum MissionType`（`Shell/MissionInfoDisplays.cs:60`）⇒ 那一格只能是 `internal`。
    /// 原版那个枚举是 `public`，本仓不是 —— 所以 `type` 比原版窄一档；其余四格全是 `public` 照抄。</para></summary>
    [Serializable]
    public class MissionsConfig : IConfigData
    {
        /// <summary>原版嵌套类 `MissionsConfig.MissionConfig`（`dump.cs` TypeDefIndex 1487）：
        /// `MissionType type; // 0x10` · `int maxAmount; // 0x14`。
        /// <para>🔴 `maxAmount` 的**出厂值是 `-1`**（不是 0）—— 判据 = `.ctor` 的 `.c`
        /// （`MissionsConfig.MissionConfig__.ctor.c` 第一句 `*(undefined4 *)(param_1 + 0x14) = 0xffffffff;`）
        /// 与 ILSpy 的字段初始化式 `= -1`。<see cref="ContainerHolder"/> 那一族把 `maxAmount &lt;= 0` 读成
        /// 「不限量」，与 `-1` 这个出厂值自洽。</para></summary>
        [Serializable]
        public class MissionConfig
        {
            internal MissionType type;
            public int maxAmount = -1;
        }

        // 字段名逐字照 ILSpy（`dump.cs` 只给偏移，名字在 ILSpy 那份里）。
        private LoadableRef<MissionData> tutorialMissionRef;          // 原版 `+0x10`
        private LoadableRef<MissionChallenge> dailyLoginRef;          // 原版 `+0x18`
        private MissionConfig[] configs;                              // 原版 `+0x20`
        private List<MissionChallenge> challenges;                    // 原版 `+0x28`
        private List<ChallengeList> challengeLists;                   // 原版 `+0x30`

        /// <summary>原版 `MissionsConfig()`（`.ctor` 的 `.c` 只有**一句**）：`configs = new MissionConfig[0];`。
        /// ⚠️ `challenges` / `challengeLists` 原版**不初始化**（保持 null，等配置下发时反序列化填）——
        /// 这里照抄：谁要读它们就得自己 null 判（`CleanUp` 的第一句正是那个 null 检查）。</summary>
        public MissionsConfig()
        {
            configs = new MissionConfig[0];
        }

        /// <summary>原版 `public string ConfigKey`（`get_ConfigKey`，Slot 4）—— ILSpy 逐字解出 = **`"MissionsConfig"`**。</summary>
        public string ConfigKey { get { return "MissionsConfig"; } }

        /// <summary>原版 `public string TutorialMissionId =&gt; tutorialMissionRef.TargetId`（`get_TutorialMissionId`）。
        /// <para>⚠️ 原版 `tutorialMissionRef` 为 null ⇒ **NRE**；我们返回 null（全工程口径）。</para></summary>
        public string TutorialMissionId
        {
            get { return tutorialMissionRef != null ? tutorialMissionRef.TargetId : null; }
        }

        /// <summary>原版 `public MissionChallenge DailyLogin =&gt; dailyLoginRef.Reference`（`get_DailyLogin`）。</summary>
        public MissionChallenge DailyLogin
        {
            get { return dailyLoginRef != null ? dailyLoginRef.Reference : null; }
        }

        /// <summary>原版 `public MissionsConfig.MissionConfig[] Configs =&gt; configs;`（`get_Configs`）。</summary>
        public MissionConfig[] Configs { get { return configs; } }

        /// <summary>原版 `public List&lt;MissionChallenge&gt; Challenges =&gt; challenges;`（`get_Challenges`，读 `+0x28`）。</summary>
        public List<MissionChallenge> Challenges { get { return challenges; } }

        /// <summary>原版 `public List&lt;ChallengeList&gt; ChallengeLists =&gt; challengeLists;`（`get_ChallengeLists`，读 `+0x30`）。</summary>
        public List<ChallengeList> ChallengeLists { get { return challengeLists; } }

        /// <summary>装载口（原版由反序列化直接写私有字段；我们本机没有反序列化源，给一个显式口）。
        /// <para>⛔ 这不是原版的方法 —— 原版那五个字段是 `[SerializeField]`，由 `JsonWrapper` 灌。
        /// 我们只在自检与将来的本地数据源里用得到它；⛔ 别把它当成原版语义。</para></summary>
        public void SetData(List<MissionChallenge> challengeList, List<ChallengeList> lists, MissionConfig[] cfg)
        {
            challenges = challengeList;
            challengeLists = lists;
            configs = cfg ?? new MissionConfig[0];
        }

        /// <summary>原版 `public void CleanUp()`（`MissionsConfig__CleanUp.c`，Slot 6 = `IConfigData.CleanUp`）。
        /// <para>逐句见文件头。三个效果：**重置**（槽 8 `OnReset`）→ **摘光进度订阅**（`+0x48` 换空 lambda）
        /// → **清 initialized 闩**（`+0x50 = false`）。</para>
        /// <para>🔴 只有 `initialized == true` 的那几条会做这三件 —— 没初始化过的挑战原版**碰都不碰**。</para></summary>
        public void CleanUp()
        {
            if (challenges == null)
            {
                // 原版这一跳是 `FUN_1803f47a0()` = throw NRE（`.c:78-79`）
                Debug.LogError("[MissionsConfig] `CleanUp()`：`challenges` 是 null（原版这一跳 NRE）⇒ 没有可清理的，返回");
                return;
            }
            for (int i = 0; i < challenges.Count; i++)
            {
                var ch = challenges[i];
                if (ch == null)
                {
                    Debug.LogError("[MissionsConfig] `CleanUp()`：`challenges[" + i + "]` 是 null（原版这一跳 NRE）⇒ 跳过这一条");
                    continue;
                }
                if (!ch.IsInitialized) continue;              // `.c`: `if ((char)plVar2[10] == '\0') continue;`

                ResetChallenge(ch);                           // 槽 8 `OnReset()`
                ch.ReplaceOnProgressUpdate(EmptyProgressUpdate); // `+0x48` = `<>c__9__30_0`（空 lambda，RVA 0x4B33B0）
                ch.ClearInitialized();                        // `+0x50 = false`
            }
        }

        /// <summary>`<>c__9__30_0` = `MissionChallenge.&lt;DisableChallenge&gt;b__30_0`（RVA `0x4B33B0` = 全局空桩）。
        /// <para>🔴 **必须是「空方法」而不是 `null`** —— 原版把它**赋给** `+0x48`（`plVar2[9] = lVar4`），
        /// 不是 `-=`。两者在「订阅者被摘光」这一点上等价，但 `null` 会让 `RaiseProgress()` 走空判分支、
        /// 空 lambda 会真的调一次空方法 —— 原版是后者，照抄。</para></summary>
        static readonly Action<MissionChallengeUpdatePayload> EmptyProgressUpdate = EmptyProgressUpdateBody;

        static void EmptyProgressUpdateBody(MissionChallengeUpdatePayload payload) { }

        static readonly Dictionary<Type, System.Reflection.MethodInfo> _onResetByType =
            new Dictionary<Type, System.Reflection.MethodInfo>();
        static readonly HashSet<Type> _onResetMissingNoted = new HashSet<Type>();

        /// <summary>原版虚槽 8 的等价分发（为什么要走运行时类型：见文件头差异 ①）。
        /// 找不到 `OnReset` ⇒ 出声一次（原版那一位是 `abstract` ⇒ 真子类必有；本仓
        /// `MissionComplete` / `DailyLoginChallenge` 还没落那一格 ⇒ 如实报）。</summary>
        static void ResetChallenge(MissionChallenge ch)
        {
            var t = ch.GetType();
            System.Reflection.MethodInfo mi;
            if (!_onResetByType.TryGetValue(t, out mi))
            {
                mi = t.GetMethod("OnReset",
                                 System.Reflection.BindingFlags.Instance
                                 | System.Reflection.BindingFlags.Public
                                 | System.Reflection.BindingFlags.NonPublic,
                                 null, Type.EmptyTypes, null);
                // 防御：基类将来若真补了 `virtual OnReset()`，那一格是空体 ⇒ 不能当成「子类实现了」
                if (mi != null && mi.DeclaringType == typeof(MissionChallenge)) mi = null;
                _onResetByType[t] = mi;
            }
            if (mi == null)
            {
                if (_onResetMissingNoted.Add(t))
                    Debug.LogWarning("[MissionsConfig] `CleanUp()`：`" + t.Name + "` 没有 `OnReset()`"
                                   + "（原版 Slot 8 是 `abstract`，真子类必有；本仓这一格还没落）⇒ 这一步什么都不做");
                return;
            }
            try { mi.Invoke(ch, null); }
            catch (Exception e)
            {
                Debug.LogError("[MissionsConfig] `CleanUp()`：`" + t.Name + ".OnReset()` 抛异常 ⇒ " + e);
            }
        }

        /// <summary>自检用：清空反射缓存（避免测试之间互相影响）。</summary>
        internal static void ResetOnResetCacheForTest()
        {
            _onResetByType.Clear();
            _onResetMissingNoted.Clear();
        }
    }

    // ============================================================ `MissionChallenge` 的两格写口（partial 块）
    /// <summary>🔴 **为什么开 partial 块**：`CleanUp` 要写 `MissionChallenge` 的两格**私有状态** ——
    /// `+0x48`（field-like event `OnProgressUpdate`）与 `+0x50`（`private bool initialized`）。
    /// C# 里 field-like event 只有**声明它的那个类**能整格赋值（外部只能 `+=` / `-=`），私有字段同理
    /// ⇒ 只能开 partial 块（partial 声明 = 同一个类）。
    /// <para>⛔ **不碰 `Shell/MissionEvent.cs`**：那个文件另有多条账按行号钉着（铁律 18·b），本批文件所有权也不含它。</para></summary>
    public abstract partial class MissionChallenge
    {
        /// <summary>= 原版 `+0x48` 那一次**整格替换**（`plVar2[9] = lVar4`）：把委托字段直接指向 `handler`。
        /// ⛔ 不是 `-=`（原版也不需要知道原来订了谁）。`handler` 传 null 的效果与「清空订阅」相同，
        /// 但原版传的是**空 lambda**（见 `MissionsConfig.EmptyProgressUpdate` 的说明）。</summary>
        internal void ReplaceOnProgressUpdate(Action<MissionChallengeUpdatePayload> handler)
        {
            OnProgressUpdate = handler;
        }

        /// <summary>= 原版 `+0x50 = false`（`*(undefined1 *)(plVar2 + 10) = 0;`）。</summary>
        internal void ClearInitialized() { initialized = false; }
    }
}
