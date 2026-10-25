// AllianceTrophyModel.cs — 联盟奖杯的**本地模型**（原版 `AllianceTrophy` / `AllianceTrophyData` 的本地半边）
//
// ============================ 判据（逐个方法体可读）============================
// · `AllianceTrophy.CurrentTier`（`AllianceTrophy.cs:97`，getter 可读）：
//       `Math.Max(save.tier, Enumerable.Count(challenge.rewards, r => r.targetValue <= save.groupScore))`
//   ⇒ 当前档 = **已达成里程碑数**，且**不低于**存档里记过的档（`save.tier` 是服务端记的最高档）。
// · `AllianceTrophy.GetTargetMilestone()`（`:228`）：
//       `var m = Enumerable.ElementAtOrDefault(challenge.rewards, CurrentTier)`；取不到 ⇒ `Enumerable.Last(rewards)`。
// · `AllianceTrophy.GetCurrentStatus()`（`:270`）：`new GroupBadge { trophy = Data, tierLevel = CurrentTier }`。
// · `AllianceTrophyData` 字段（`AllianceTrophyData.cs` 现读）：`id / title / description / challenge(MissionChallenge) /
//   keepPointsOnLeave / hideIfNotAchieved / dontShowProgress / reward(LoadableReference<AllianceBadge>)`。
// · `MissionChallenge.rewards` 其实是 **`ChallengeMilestone[]`**（字段名 historical）：每项
//   `{ difficulty, targetValue, rewards[] }` —— **`targetValue` 就是里程碑阈值**。
//
// ============================ 本地能做 / 不能做 ============================
// ✅ 本地能做（本文件）：档位换算、目标里程碑、状态（trophy + tierLevel）、可见性（`hideIfNotAchieved`）、
//    「进度变了重算档」这一条订阅语义的本地等价（`OnProgress`）。
// ❌ 仍是服务器：**`save.groupScore`（公会总分）与 `save.tier`（服务端记住的最高档）** —— 原版从
//    `AllianceTrophySave`（公会存档，PlayFab Groups 下发）拿；本地没有 ⇒ `WhyNoGroupScore`，
//    默认 0，由 `SetScoreForTest` / 将来的真数据入口注入。
// ❌ **奖杯定义**：本地只有 **1 份**模板 `Default_Trophy.asset`
//    （`assetripper_out/ExportedProject/Assets/MonoBehaviour/Default_Trophy.asset`，`challengeType 2`、
//     `targetValue 0`、`targetId Default_Badge`）—— 成套的 `AllianceTrophyData` SO **不在本地**
//    （`assets_full` 只有奖杯**图**：`bundle_alliancesbadgesimages_assets_all/Sprite/Alliance_Trophies_*.json`）
//    ⇒ `Defs` 默认为空，用 `SetDefsForTest` 注入；空表时 `BuildTrophyRows` 退回原来的
//    `SocialData.AllianceTrophies` 计数（**行为不变**）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一个奖杯的定义（= 原版 `AllianceTrophyData` 的本地最小等价）。
    /// `MilestoneTargets` = `MissionChallenge.rewards[i].targetValue`。</summary>
    public sealed class AllianceTrophyDef
    {
        public string Id;
        public string TitleTerm;          // 原版 `title`（LocalizedLabel → 词条 key）
        public string DescTerm;           // 原版 `description`
        public int[] MilestoneTargets;    // 升序（原版表就是升序；本类不排序、按原顺序用）
        public string RewardBadgeId;      // 原版 `reward`（AllianceBadge 的 id）
        public bool KeepPointsOnLeave;    // 原版 `keepPointsOnLeave`
        public bool HideIfNotAchieved;    // 原版 `hideIfNotAchieved`
        public bool DontShowProgress;     // 原版 `dontShowProgress`
        /// <summary>🆕 **2026-10-25（N9 · 账 73/76/89）**：原版 `AllianceTrophy.IsFeatured` ——
        /// `AllianceTrophiesView.Draw` **只对 `IsFeatured` 为真的那一枚**调 `ToggleSelected` 点亮描边
        /// （判据 `AllianceTrophiesView__Draw.c:192-196`）。⚠️ 本地没有可读的真值 ⇒ 默认 `false`。</summary>
        public bool IsFeatured;
    }

    /// <summary>一个奖杯的进度态（= 原版 `AllianceTrophySave` 的那两个字段）。</summary>
    public struct AllianceTrophyState
    {
        public int GroupScore;   // 公会总分（服务器下发）
        public int SavedTier;    // 服务端记住的最高档
    }

    /// <summary>联盟奖杯本地模型（档位换算 / 目标里程碑 / 可见性）。</summary>
    public static class AllianceTrophyModel
    {
        public const string WhyNoGroupScore =
            "判据不可及：`save.groupScore` / `save.tier` 来自公会存档 `AllianceTrophySave`" +
            "（PlayFab Groups 下发，离线无）；本类默认全是 0，由 `SetScoreForTest` 或将来真数据入口注入";

        public const string WhyNoDefs =
            "本地只有 1 份模板 `Default_Trophy.asset`；成套 `AllianceTrophyData` SO 不在本地" +
            "（assets_full 只有奖杯图）⇒ `Defs` 默认空，行为退回 `SocialData.AllianceTrophies` 计数";

        static AllianceTrophyDef[] _defs = Array.Empty<AllianceTrophyDef>();
        static readonly Dictionary<string, AllianceTrophyState> _states =
            new Dictionary<string, AllianceTrophyState>();
        static bool _warnedDefs;

        public static AllianceTrophyDef[] Defs { get { return _defs; } }
        public static bool HasDefs { get { return _defs != null && _defs.Length > 0; } }

        // ============================================================ 档位（1:1 复刻）
        /// <summary>= `AllianceTrophy.CurrentTier`：`Math.Max(SavedTier, 已达成里程碑数)`。
        /// 「已达成」= `milestone.targetValue &lt;= GroupScore`（原版谓词的比较方向可读）。</summary>
        public static int CurrentTier(AllianceTrophyDef def, AllianceTrophyState state)
        {
            if (def == null || def.MilestoneTargets == null) return Math.Max(0, state.SavedTier);
            int reached = 0;
            for (int i = 0; i < def.MilestoneTargets.Length; i++)
                if (def.MilestoneTargets[i] <= state.GroupScore) reached++;
            return Math.Max(Math.Max(0, state.SavedTier), reached);
        }

        /// <summary>= `AllianceTrophy.GetTargetMilestone()`：`ElementAtOrDefault(milestones, CurrentTier)`；
        /// 越界 ⇒ **最后一条**（原版 `Enumerable.Last`）。没有里程碑 ⇒ 返回 `int.MinValue`（调用方据此不画）。</summary>
        public static int TargetMilestone(AllianceTrophyDef def, AllianceTrophyState state)
        {
            if (def == null || def.MilestoneTargets == null || def.MilestoneTargets.Length == 0) return int.MinValue;
            int tier = CurrentTier(def, state);
            return tier < def.MilestoneTargets.Length
                ? def.MilestoneTargets[tier]
                : def.MilestoneTargets[def.MilestoneTargets.Length - 1];
        }

        /// <summary>= `AllianceTrophy.GetCurrentStatus()`：`(trophyId, tierLevel)`（原版 `GroupBadge` 那两个字段）。</summary>
        public static void CurrentStatus(AllianceTrophyDef def, AllianceTrophyState state,
                                         out string trophyId, out int tierLevel)
        {
            trophyId = def != null ? def.Id : null;
            tierLevel = CurrentTier(def, state);
        }

        /// <summary>`hideIfNotAchieved` 的本地判定（原版字段语义：一档都没到就整格不显示）。</summary>
        public static bool IsVisible(AllianceTrophyDef def, AllianceTrophyState state)
        {
            if (def == null) return false;
            if (!def.HideIfNotAchieved) return true;
            return CurrentTier(def, state) > 0;
        }

        /// <summary>`BuildTrophyRows` 用的格数：**有定义表 ⇒ 可见定义数**；没有 ⇒ `-1`（调用方退回旧计数）。</summary>
        public static int VisibleCountOrMinusOne
        {
            get
            {
                if (!HasDefs)
                {
                    if (!_warnedDefs) { _warnedDefs = true; Debug.Log("[AllianceTrophy] " + WhyNoDefs); }
                    return -1;
                }
                int n = 0;
                for (int i = 0; i < _defs.Length; i++)
                    if (IsVisible(_defs[i], StateOf(_defs[i]))) n++;
                return n;
            }
        }

        /// <summary>`BuildTrophyRows` 的第 i 格对应哪个定义（**只数可见的**，与格数口径一致）。
        /// 越界 / 无定义表 ⇒ null（调用方按「无数据」处理）。</summary>
        public static AllianceTrophyDef VisibleAt(int index)
        {
            if (!HasDefs || index < 0) return null;
            int n = 0;
            for (int i = 0; i < _defs.Length; i++)
            {
                if (!IsVisible(_defs[i], StateOf(_defs[i]))) continue;
                if (n == index) return _defs[i];
                n++;
            }
            return null;
        }

        public static AllianceTrophyState StateOf(AllianceTrophyDef def)
        {
            AllianceTrophyState s;
            if (def != null && def.Id != null && _states.TryGetValue(def.Id, out s)) return s;
            return new AllianceTrophyState();
        }

        /// <summary>进度更新（= 原版 `AllianceTrophy.OnChallengeUpdate` 的本地等价：写分 → 重算档）。
        /// 返回 (旧档, 新档)；调用方可据此决定要不要重画那一格。</summary>
        public static void OnProgress(string trophyId, int groupScore, out int tierBefore, out int tierAfter)
        {
            var def = Find(trophyId);
            var st = StateOf(def);
            tierBefore = CurrentTier(def, st);
            st.GroupScore = Math.Max(0, groupScore);
            if (def != null && def.Id != null) _states[def.Id] = st;
            tierAfter = CurrentTier(def, st);
        }

        public static AllianceTrophyDef Find(string id)
        {
            if (string.IsNullOrEmpty(id) || _defs == null) return null;
            for (int i = 0; i < _defs.Length; i++)
                if (_defs[i] != null && _defs[i].Id == id) return _defs[i];
            return null;
        }

        // ============================================================ 自检 / 测试口
        public static void SetDefsForTest(AllianceTrophyDef[] defs)
        {
            _defs = defs ?? Array.Empty<AllianceTrophyDef>();
            _warnedDefs = false;
        }

        public static void SetScoreForTest(string trophyId, int groupScore, int savedTier = 0)
        {
            _states[trophyId] = new AllianceTrophyState { GroupScore = Math.Max(0, groupScore), SavedTier = Math.Max(0, savedTier) };
        }

        public static void ResetForTest()
        {
            _defs = Array.Empty<AllianceTrophyDef>();
            _states.Clear();
            _warnedDefs = false;
        }

        /// <summary>自检：**含一条灭自证**（阈值两侧必须换档；任何「恒返回同一档」的实现过不了）。</summary>
        public static bool SelfCheck(out string why)
        {
            why = null;
            ResetForTest();
            var def = new AllianceTrophyDef
            {
                Id = "selfcheck",
                MilestoneTargets = new[] { 100, 300, 600 },
                HideIfNotAchieved = true,
            };
            SetDefsForTest(new[] { def });

            SetScoreForTest(def.Id, 0);
            var s0 = StateOf(def);
            if (CurrentTier(def, s0) != 0) { why = "0 分应 0 档"; return false; }
            if (IsVisible(def, s0)) { why = "hideIfNotAchieved 且 0 档 ⇒ 不该可见"; return false; }
            if (TargetMilestone(def, s0) != 100) { why = "0 档的目标应是第一个里程碑 100"; return false; }

            SetScoreForTest(def.Id, 99);
            var s99 = StateOf(def);
            SetScoreForTest(def.Id, 100);
            var s100 = StateOf(def);
            // 🔴 灭自证：阈值两侧跨档
            if (CurrentTier(def, s99) == CurrentTier(def, s100))
            { why = "99 与 100 同档（档位没按 targetValue 比较）"; return false; }
            if (CurrentTier(def, s100) != 1) { why = "100 分应 1 档"; return false; }

            SetScoreForTest(def.Id, 300);
            var s300 = StateOf(def);
            if (CurrentTier(def, s300) != 2) { why = "300 分应 2 档"; return false; }
            if (TargetMilestone(def, s300) != 600) { why = "2 档的目标应是 600"; return false; }

            // 满档之后目标退化成最后一个（原版 ElementAtOrDefault 取不到 ⇒ Last）
            SetScoreForTest(def.Id, 9999);
            var sMax = StateOf(def);
            if (CurrentTier(def, sMax) != 3) { why = "超满分应到第 3 档（3 个里程碑）"; return false; }
            if (TargetMilestone(def, sMax) != 600) { why = "满档目标应为最后一条 600"; return false; }

            // SavedTier 是下界（原版 Math.Max(save.tier, reached)）
            var sSaved = new AllianceTrophyState { GroupScore = 0, SavedTier = 2 };
            if (CurrentTier(def, sSaved) != 2) { why = "SavedTier=2 应把档位托到 2"; return false; }

            // 空定义 ⇒ VisibleCount 返回 -1（调用方退回旧计数，不静默变成 0 格）
            ResetForTest();
            if (VisibleCountOrMinusOne != -1) { why = "空定义表应返回 -1"; return false; }

            ResetForTest();
            return true;
        }
    }
}
