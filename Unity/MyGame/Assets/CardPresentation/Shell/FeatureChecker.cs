// FeatureChecker.cs —— 原版 `FeatureChecker` 组件 + `FeatureConfig.CheckFeature` 的**本地等价**（账 未分类:301）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（本会话现读，2026-10-29）：
//   · `decomp_full/FeatureChecker__OnEnable.c` 全文：`cfg = ConfigManager.GetConfig<FeatureConfig>()`；
//     `cfg == null` ⇒ **抛**；否则 `v = cfg.CheckFeature(featureKey(+0x28), out n)` → `OnFeatureChecked(+0x20).Invoke(v)`
//     （= `UnityEvent<bool>`，挂点都接在 prefab 的 `m_MethodName` 上：`SetActive` / `set_interactable`）。
//   · **语义所在 = `decomp_full/FeatureConfig__CheckFeature.c`**：key 先做一次 `String.Replace` 归一化 →
//     命中**缓存字典**直接返回 → 否则 `list.FirstOrDefault(name == key)`；
//     🔴 **`lVar4 == 0`（找不到）⇒ `cVar2 = '\x01'` = true**；找到 ⇒ 读 `+0x1c`。
//   · 配置本体来自**远端 LiveOps**（`ConfigManager` 只管下发）⇒ **本机 0 份 `FeatureConfig`**（关服）。
//
// ============================ 资产面：4 个 key / 10 个实例 / 6 个文件 ============================
// 本会话逐份现读（`d:/2/新解包资源/assets_full/`，按 `"featureKey"` 直搜：`PlayerLeaderboard`×1 ·
// `Alliances`×3 · `FactionLeaderboard`×3 · `FactionRanking`×3 ⇒ **只有这 4 个不同 key**）：
//   | prefab / 场景 | featureKey | OnFeatureChecked 打到 |
//   |---|---|---|
//   | `RankedEventWindowV2.prefab`            | `PlayerLeaderboard`  | `set_interactable` |
//   | `Social Submenu Variant.prefab`         | `Alliances`          | `SetActive` |
//   | `DraftLeaderboardPopup.prefab`          | `FactionLeaderboard` · `FactionRanking` | `SetActive` ×2 |
//   | `RankedSkirmishLeaderboardPopup.prefab` | `FactionLeaderboard` · `Alliances` · `FactionRanking` | `SetActive` ×3 |
//   | `RankedClassicLeaderboardPopup Variant.prefab` | `FactionLeaderboard` · `FactionRanking` | `SetActive` ×2 |
//   | `MainMenu Warpforge.unity`              | `Alliances`          | `SetActive` |
//   （文件都在 `bundle_menus_assets_all/MonoBehaviour/` 下；`MainMenu Warpforge.unity` 是场景。）
//
// ============================ 为什么是「本地模拟」而不是「复刻」 ============================
// 🔶 自己设计（非原版）：**开关的取值表由我们本地模拟** —— 原版这张表的判据 = 远端 `FeatureConfig`
//   （`ConfigManager.GetConfig<FeatureConfig>()` 下发，key→{enabled(+0x1c), …} 列表），**服务器已关服、判据在远端** ⇒
//   本地永远拿不到。**调度台 2026-10-29 裁定**：按原版 `CheckFeature` 自己给出的默认支落地 ——
//   **表里没有的 key ⇒ true（显示/可点）**，于是「本机恒显」这件事从**隐式**变成**显式且有据**。
//   ⛔ 别在我们这张表里凭感觉填 false 去「关掉某个功能」—— 原版没有那份数据，关了就是**我们编的行为**。
//   ⛔ 也⛔ 别把本类说成「原版开关」：它只是**原版默认支**的落地。
//
// ============================ 与挂点的关系（本批**未接线**） ============================
// 原版挂点（4 处，判据 = 上表）：`RankedEventWindowV2`（那颗钮的 `interactable`）· `Social Submenu`（社交页签）·
// 三扇 leaderboard 弹窗（Tab 显隐）· `MainMenu Warpforge`（左竖导航第 5 键）。对应本仓文件：
// `Shell/LeaderboardWindow.cs` · `Shell/RankedEventWindow.cs` · `Shell/SocialWindow.cs` · `Shell/MainMenuRuntime.cs`。
// ⚠️ **本批（B94_X1）只建组件**（施工单只给了本文件的白名单）⇒ 接线由调度台另行派；接的时候照原版用
//   `IsEnabled(<key>)` 决定 `SetActive` / `interactable`，⛔ 别改成别的语义（比如拿来当权限闸）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `FeatureChecker` 的**本地等价**：一个 key ⇒ bool 的开关表。
    /// <para>🔶 自己设计（非原版）：取值表本地模拟（原版由远端 `FeatureConfig` 下发，服务器已关服、判据在远端）·
    /// 未登记的 key ⇒ **true** 这一支是**原版 `FeatureConfig.CheckFeature` 自己的默认支**（不是我们编的）·
    /// 调度台 2026-10-29 裁定。</para></summary>
    public static class FeatureChecker
    {
        // ============================================================ 4 个 key（原版资产里**只有**这 4 个）
        /// <summary>`RankedEventWindowV2.prefab` —— 那颗排位榜钮的 `interactable`。</summary>
        public const string PlayerLeaderboard = "PlayerLeaderboard";
        /// <summary>`Social Submenu Variant.prefab` · `MainMenu Warpforge.unity` · 两扇 leaderboard 弹窗 —— 社交/联盟页显隐。</summary>
        public const string Alliances = "Alliances";
        /// <summary>三扇 leaderboard 弹窗 —— 阵营榜 Tab 显隐。</summary>
        public const string FactionLeaderboard = "FactionLeaderboard";
        /// <summary>三扇 leaderboard 弹窗 —— 阵营排名 Tab 显隐。</summary>
        public const string FactionRanking = "FactionRanking";

        /// <summary>资产里能读到的全部 key（顺序无意义；自检用来遍历）。</summary>
        public static readonly string[] KnownKeys = { PlayerLeaderboard, Alliances, FactionLeaderboard, FactionRanking };

        // ============================================================ 判定
        static readonly Dictionary<string, bool> overrides = new Dictionary<string, bool>();
        static readonly HashSet<string> warnedUnknown = new HashSet<string>();

        /// <summary>原版 `FeatureConfig.CheckFeature(string key)` 的**默认支**：
        /// 表里**没登记 ⇒ true**；登记了 ⇒ 读那一格。（原版：`lVar4 == 0 ⇒ cVar2 = 1`。）
        /// <para>⚠️ 我们这张表**只有自检能写**（`SetEnabledForTest`）—— 生产环境恒走「未登记 ⇒ true」，
        /// 因为原版那张表的数据在**远端**、本地没有。</para>
        /// <para>⚠️ 原版方法体里那句 `String.Replace(...)` 是 key **归一化**（把某个分隔符换掉再查表）；
        /// 那两个字面量是 IL 里的元数据指针，本会话**没解出**是哪一个字符 ⇒ 我们**不做归一化**
        /// （4 个 key 都是单个大写单词，不含分隔符 ⇒ 归一化对它们恒等）。若以后出现带 `_`/`-` 的 key，要回来查。</para></summary>
        /// <param name="featureKey">原版 `featureKey`（= prefab 上 `FeatureChecker` 那颗字段）。</param>
        public static bool IsEnabled(string featureKey)
        {
            if (string.IsNullOrEmpty(featureKey))
            {
                // 原版：`param_2 == 0 ⇒` 落到末尾 `FUN_1803f47a0()`（不返回 = NRE）⇒ 我们出声 + 取安全默认 true。
                Debug.LogError("[FeatureChecker] IsEnabled(null/空)：原版这一跳 NRE ⇒ 按「未登记」处理，返回 true");
                return true;
            }

            bool v;
            if (overrides.TryGetValue(featureKey, out v)) return v;

            // ★ 原版 `CheckFeature`：`list.FirstOrDefault(name == key)` 找不到 ⇒ 返回 true。
            if (warnedUnknown.Add(featureKey))
                Debug.Log("[FeatureChecker] key '" + featureKey + "' 未登记 ⇒ true"
                        + "（= 原版 `FeatureConfig.CheckFeature` 的默认支；远端配置本地拿不到）");
            return true;
        }

        // ============================================================ 自检口（生产不写表）
        /// <summary>自检用：把某个 key 的判定固定成 `enabled`（模拟「原版远端配置登记了这条」那一支）。</summary>
        public static void SetEnabledForTest(string featureKey, bool enabled)
        {
            if (string.IsNullOrEmpty(featureKey)) { Debug.LogError("[FeatureChecker] SetEnabledForTest：key 是空"); return; }
            overrides[featureKey] = enabled;
        }

        /// <summary>自检收尾：清空覆写表 + 告警去重。</summary>
        public static void ResetForTest()
        {
            overrides.Clear();
            warnedUnknown.Clear();
        }
    }
}
