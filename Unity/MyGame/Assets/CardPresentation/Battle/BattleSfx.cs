// BattleSfx.cs — 原版**战斗**的「时机 → cue」那一族音效（用户在 2026-10-15 `T06` 点名：现在战斗是「默片」）
//
// ============================================================================================
//  判据链（每一跳都亲读过，**不是按 cue 名字猜**）
// ============================================================================================
//  ① **挂在谁身上**：`BattleManager.soundCollection`（`+0x80`）→ `SoundAssetCollection`
//     （`BattleManager__Initialize.c` 那一路；字段名来自 `d:/2/tools/il2cpp_out/dump.cs`）。
//     原版代码里那一句就长这样：`SoundAssetCollection__get_StartTurn(*(longlong *)(param_1 + 0x80), 0)`
//     —— **字段名就是本文件下面这些常量**。
//  ② **字段 → 资产**：每个字段是一个 Addressables `AssetReference`（`m_AssetGUID`）。
//     解析走 `工具/_guid_resolve_probe.py`（catalog_main.json → 束内容器键）。
//     实测 28 个字段：**16 个解得出** · **6 个空引用** · **6 个悬挂 GUID**（见下面「本 build 静音的 12 条」）。
//  ③ **资产 → clip**：cue 的 `clipList` 存 PathID，**必须读束**才有名字。
//     ⚠️ **cue 名 ≠ clip 名**（`startTurn` → cue `Start Trun` → clip `StartTurn`；
//        `minionInitiatingAttack` → clip `SelectUnit`；`sneakAttack` → clip `battle_attack_soft`）
//        ⇒ ⛔ **别拿字段名去 `Resources/` 里找文件**，找不到。
//  ④ **MixerType 恒为 `FX`**：反编译调用点第 4 实参**逐条都是 0** —— 核过 `EndTurnClick:50,171` ·
//     `_NextTurn_d__395:450` · `_MulliganCountdown_d__347:33` · `EndCountdown:34` · `SetNoActionsUi:53` ·
//     `DrawCard:50` · `_Heal_d__468:148,154` ⇒ `WFSoundPlayer` 挂 FX 组那一档即可。
//     （`EndBattleDoors` 的胜/败/平三支走 `Jingles`，那是**另一族**，不在本文件。）
//
//  数据与音频的产出方 = `工具/import_battle_sfx.py`（表 → `Resources/battle_sounds.json`；
//  clip → `Resources/Art/audio/sfx/`，`Resources/Art/` 整个被 `.gitignore` 排除）。
//  ⚠️ 表由 `WFSoundBank` 与 `animfx_sounds.json` 装进**同一个字典** ⇒ 播放口仍只有 `WFSoundPlayer` 一处。
//
// ============================================================================================
//  🔴 本 build 里**原版自己就静音的 12 条**（⛔ 我们**故意不接**，不是没做 —— 铁律 11 例外①）
// ============================================================================================
//  · **空引用 6 条**（亲读 **shipped bundle** 的 `SoundAssetCollection`，解包 JSON 逐字段一致）：
//      `abilityBoxEnter` · `abilityBoxExit` · `syphon` · `activateBlast` · `activateSacrifice` ·
//      `dissolveCardInHand`
//    ⇒ 原版那 6 格 `AssetReference` 是空的 ⇒ 调到也**什么都不播**。
//  · **悬挂 GUID 6 条**（GUID 在本地**任何 catalog / 任何束的容器键**里都解不出；
//     且全 `assets_full` 里没有任何以这些语义命名的 cue 资产）：
//      `abilityBoxClick` · `activateUnstunnable` · `activateSurvivor` · `activateTerror` ·
//      `activateSentinel` · `activateAutoAttack`
//    ⇒ 本 build 也不响。⚠️ **还没查清的部分**：远端 catalog（关服后拿不到）里是否有这几条 ——
//      判据缺口写在 `资料/任务线/战斗线/任务/T06_战斗音响接线.md`。
//    ⛔ **别为了「补齐 28 条」给这 12 条随便找一条 clip 顶上** —— 那是静默造假数据。
// ============================================================================================
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 战斗音效（原版 `BattleManager.soundCollection` 那一族）。**键 = 原版字段名**
    /// （`battle_sounds.json` 的 `name` 列）—— ⛔ 不是 cue 资产名，也不是 clip 名。
    /// </summary>
    public static class BattleSfx
    {
        // ---- 字段名（= 原版 `SoundAssetCollection` 的字段，逐字照抄，含原版自己的拼写）----
        public const string DrawCardFromDeck       = "drawCardFromDeck";
        public const string MinionHeal             = "minionHeal";
        public const string MinionInitiatingAttack = "minionInitiatingAttack";
        public const string MoveCardIntoBattle     = "moveCardIntoBattle";
        public const string PlayMinionFromHand     = "playMinionFromHand";
        public const string SneakAttack            = "sneakAttack";
        public const string UnitDamage             = "unitDamage";
        public const string ChatShow               = "chatShow";
        public const string EndTurnButton          = "endTurnButton";
        public const string EndTurnButtonInactive  = "endTurnButtonInactive";
        public const string TimerExpired           = "timerExpired";
        public const string StartTurn              = "startTurn";
        public const string Mulligan               = "mulligan";
        public const string OutOfActions           = "outOfActions";
        public const string DefaultActiveAbility   = "defaultActiveAbility";
        public const string Toggle                 = "toggle";

        /// <summary>自检用：一共喊过几次（**不受 `Enabled` 影响**）。</summary>
        public static int Played { get; private set; }
        /// <summary>自检用：因为 `timeToPlayAgain` 被挡掉几次（**不是错**，原版就这么节流）。</summary>
        public static int Throttled { get; private set; }
        /// <summary>自检用：最后一次喊的字段名（没喊过 = null）。</summary>
        public static string LastField { get; private set; }
        /// <summary>自检用：最后一次真的播出去的 clip 名（没播 = null）。</summary>
        public static string LastClip { get; private set; }

        /// <summary>自检用：清零（每一段之前调）。</summary>
        public static void ResetCounters()
        {
            Played = 0;
            Throttled = 0;
            LastField = null;
            LastClip = null;
            _lastAt.Clear();
        }

        /// <summary>各字段上一次播的时刻（`Time.time`）—— 实现原版 `AudioCue.timeToPlayAgain`。</summary>
        static readonly Dictionary<string, float> _lastAt = new Dictionary<string, float>();
        /// <summary>取不到 cue 的字段（**只警告一次** —— 逐次刷屏会把别的告警淹掉）。</summary>
        static readonly HashSet<string> _warnedMissing = new HashSet<string>();

        /// <summary>
        /// 按**原版字段名**喊一条战斗音效（原版一律 `SoundManager.Play2D(cue, MixerType.FX)` ⇒ 2D）。
        /// 返回 true = 真的交给播放器了。**取不到 cue 会出声警告**（项目红线：不许静默失败）。
        /// </summary>
        public static bool Play(string field)
        {
            Played++;
            LastField = field;

            if (string.IsNullOrEmpty(field)) return false;

            WarpforgeVFX.WFSoundCue cue;
            if (!WarpforgeVFX.WFSoundBank.TryGetCue(field, out cue) || cue == null)
            {
                if (_warnedMissing.Add(field))
                    Debug.LogWarning($"[BattleSfx] 战斗音效 `{field}` 在 cue 表里查不到 —— 跑一次 " +
                                     "`python 工具/import_battle_sfx.py` 生成 `Resources/battle_sounds.json`。" +
                                     "（⚠️ 若它属于「原版自己就静音的那 12 条」，见 `Battle/BattleSfx.cs` 文件头。）");
                return false;
            }

            // 原版 `AudioCue.timeToPlayAgain`（本表实测恒 0.1 s）：同一条刚要过就不再播。
            // ⚠️ 被挡掉**不是失败** —— 调用方别把它当错误报（看 `Throttled`）。
            if (cue.timeToPlayAgain > 0f)
            {
                float last;
                if (_lastAt.TryGetValue(field, out last) && Time.time - last < cue.timeToPlayAgain)
                {
                    Throttled++;
                    return false;
                }
            }
            _lastAt[field] = Time.time;

            WarpforgeVFX.WFSoundPlayer.Play(cue, Vector3.zero, true);   // is2d = true ⇒ 原版 `Play2D`
            var clip = WarpforgeVFX.WFSoundBank.PickClip(cue, null);
            LastClip = clip != null ? clip.name : null;
            return true;
        }
    }
}
