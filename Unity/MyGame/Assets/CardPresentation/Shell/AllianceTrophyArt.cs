// AllianceTrophyArt.cs — 奖杯**档位框**的取图/卸载（原版 `AlliancesTrophyController.GetTierFrame / UnloadTierFrame`）
//
// ============================ 判据 ============================
// · `AlliancesTrophyController.tierFrames` = `AssetReferenceTyped<Sprite>[]`（桩字段实读）；
//   `GetTierFrame(int tier)`（伪码可读）：
//       `idx = Math.Max(0, tier - 1)`；取 `tierFrames[idx]` → `Load()`。
//   ⚠️ **原版越界会直接抛**（`*(uint*)(arr+0x18) <= idx` ⇒ `FUN_1803f4790` panic）——
//      我们**夹到 1..4 并出声**（同 `RankedRank` 表头那条纪律：不许静默越界）。
// · `UnloadTierFrame(int tier)`：`idx = Math.Max(0, tier - 1)` → `LiveOpsAssetUtility.Unload(tierFrames[idx])`
//   ⇒ **只是释放 Addressables 句柄**；本工程资产已导入 `Resources`、直接引用 ⇒ **无句柄可释放**（空实现，出声一次）。
// · 图：`Alliance_Trophies_TIER1..TIER4`（4 张，`bundle_alliancesbadgesimages_assets_all/Texture2D/` 实读），
//   2026-10-25 由本批复制进 `Resources/Art/ui_menu/`（`CardArt.MenuUi` 的取图根）。
using UnityEngine;

namespace CardPresentation
{
    public static class AllianceTrophyArt
    {
        /// <summary>`tierFrames` 数组长度（= 本地 4 张 `Alliance_Trophies_TIER1..4`）。</summary>
        public const int MaxTierFrame = 4;

        static bool _warnedUnload;

        /// <summary>= 原版 `GetTierFrame` 的下标换算：`idx = Math.Max(0, tier - 1)`（1 基 → 0 基）。
        /// 越界（`tier > 4`）原版 panic；我们夹住并**出声一次**（不是静默）。</summary>
        public static int TierIndexOf(int tier)
        {
            int idx = Mathf.Max(0, tier - 1);
            if (idx >= MaxTierFrame)
            {
                Debug.LogWarning("[AllianceTrophy] `tierFrames` 只有 " + MaxTierFrame + " 档，收到 tier=" + tier
                               + " ⇒ 夹到第 " + MaxTierFrame + " 档（原版这里会直接抛下标越界）");
                idx = MaxTierFrame - 1;
            }
            return idx;
        }

        /// <summary>图名（`Alliance_Trophies_TIER<n>`；`n` = 夹过的 1 基档位）。</summary>
        public static string TierFrameName(int tier) { return "Alliance_Trophies_TIER" + (TierIndexOf(tier) + 1); }

        /// <summary>= 原版 `GetTierFrame(int tier)`：取该档框的贴图。取不到 ⇒ null + 出声一次（调用方不画）。</summary>
        public static Texture2D GetTierFrame(int tier)
        {
            string name = TierFrameName(tier);
            var tex = CardArt.MenuUi(name);
            if (tex == null)
                Debug.LogWarning("[AllianceTrophy] 档位框图 `" + name + "` 取不到（`Resources/Art/ui_menu/`）⇒ 这一档不画框。"
                               + "原图在 `bundle_alliancesbadgesimages_assets_all/Texture2D/Alliance_Trophies_TIER1..4.png`");
            return tex;
        }

        /// <summary>= 原版 `UnloadTierFrame(int tier)`：**只释放 Addressables 句柄**。
        /// 本工程资产直接引用（`Resources`）⇒ 没有句柄；这里保留调用口（出声一次，免得以为它做了什么）。</summary>
        public static void UnloadTierFrame(int tier)
        {
            if (_warnedUnload) return;
            _warnedUnload = true;
            Debug.Log("[AllianceTrophy] `UnloadTierFrame(" + tier + ")`：本工程资产直接引用（`Resources`），"
                    + "**没有 Addressables 句柄可释放** ⇒ 空实现（原版这一句是 `LiveOpsAssetUtility.Unload`）");
        }

        /// <summary>自检用：档位→图名（1..4 夹取；0/负/越界都夹到边界）。</summary>
        public static bool SelfCheck(out string why)
        {
            why = null;
            if (TierFrameName(1) != "Alliance_Trophies_TIER1") { why = "tier1 图名错"; return false; }
            if (TierFrameName(0) != "Alliance_Trophies_TIER1") { why = "tier0 应夹到 TIER1"; return false; }
            if (TierFrameName(-5) != "Alliance_Trophies_TIER1") { why = "负 tier 应夹到 TIER1"; return false; }
            if (TierFrameName(4) != "Alliance_Trophies_TIER4") { why = "tier4 图名错"; return false; }
            if (TierFrameName(99) != "Alliance_Trophies_TIER4") { why = "越界应夹到 TIER4"; return false; }
            // 灭自证：TIER1 与 TIER4 必须**不同名**（若有人把下标写成常量，这两条不可能同时成立）
            if (TierFrameName(1) == TierFrameName(4)) { why = "tier1 与 tier4 同名（下标没按 tier-1 走）"; return false; }
            return true;
        }
    }
}
