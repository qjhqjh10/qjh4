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

        // ---- B 组：**卡 prefab 上那一块**（原版 `CardScript.battleCardSounds` + `CardScript` 自己那 3 条）----
        // 🔴 **2026-10-16（`T06` 第三轮）新增。** 这一族**不是**逐卡序列化（`T06` §二·3 原来那句「逐卡」
        //    是错的，已由 `普查产出_1016战斗/T06c_B组九条触发点.md` 推翻并复核）：全 `assets_full` 里带
        //    `invokeCUELegendary` 的 MonoBehaviour **只有 1 份** =
        //    `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour_1744609728290659264.json`（挂在 `CardScript` 上），
        //    ⇒ **一张全局默认表**就够了，⛔ 不用去逐卡取。
        // ⚠️ 每个调用点在原版里都有同一道守卫 `if (cardScript+0x1d0 != 0) … else bail`（= 那一格没挂这个块）
        //    ⇒ **没挂的卡原版就是静音**；我们有表就播，如实记着这一层差异（当前卡池里那一份是有的）。
        // ⚠️ `meleeAttackHitSmall/Medium/Big` 在原版**指向同一个 pathID**（`-3640304923993188049`），
        //    档位（`SupportMethods.GetUnitSize`：0/1→small · 2→medium · 3→big）只影响**震动与粒子**、
        //    **不影响声音** ⇒ 我们只喊 `MeleeAttackHitSmall` 这一条（听感逐位等价）。
        // ⚠️ `minionDeathCUE` / `landOnGround` 原版走 **`Play3D`**（要落点）⇒ 用下面那个带 `worldPos` 的重载，
        //    ⛔ 别拿 2D 那个顶上。
        public const string InvokeCUE              = "invokeCUE";
        public const string InvokeCUELegendary     = "invokeCUELegendary";
        public const string MinionDeathCUE         = "minionDeathCUE";
        public const string MeleeAttackHitSmall    = "meleeAttackHitSmall";
        public const string MeleeAttackHitMedium   = "meleeAttackHitMedium";
        public const string MeleeAttackHitBig      = "meleeAttackHitBig";
        public const string BackToHand             = "backToHand";
        public const string LandOnGround           = "landOnGround";
        public const string CreateCardInHand       = "createCardInHand";
        public const string ChangeToMeleeAttackCUE = "changeToMeleeAttackCUE";
        public const string ChangeToRangeAttackCUE = "changeToRangeAttackCUE";
        /// <summary>⚠️ **原版这一格是空 PPtr**（`m_FileID:0, m_PathID:0`）—— 但**调用点存在**
        /// （`CardScript__ChangeAttackType.c:53`）⇒ 归铁律 11 例外①：**原版自己就不响**，我们不接。
        /// 常量留在表里只为「查得到」+ `gaps[]` 里那一条。</summary>
        public const string ChangeToActiveAbilityCUE = "changeToActiveAbilityCUE";

        /// <summary>原版 `CardScript.ChangeAttackType(card, type, playSound)` 里 **`type` → 该喊哪一条 cue**。
        /// 返回 `null` = **这一档原版就不播**。判据 = `CardScript__ChangeAttackType.c:13-53` 逐支实读：
        /// `0`(None) 不播（但类型照写）· `1`→`+0x1e0`(melee) · `2`→`+0x1e8`(range) ·
        /// **`3`(Spell) 直接 `return`** · `4`(Active)→`+0x1f0` · 其余**抛异常**（我们不抛，出声 + 回 null）。
        /// ⚠️ 原版那句 `Play2D` 在 `if (playSound &amp;&amp; type != 0)` 里面，而**外面**还有一道
        /// 「类型真的变了才写」—— 那一道由调用方保证（只有真改了才喊）。</summary>
        public static string ChangeAttackTypeField(int type)
        {
            switch (type)
            {
                case 1: return ChangeToMeleeAttackCUE;
                case 2: return ChangeToRangeAttackCUE;
                case 4: return ChangeToActiveAbilityCUE;   // 值在表里，但本 build 那格是空 PPtr ⇒ 播不出声
                case 0: case 3: return null;               // 0 = 原版不播；3(Spell) 原版直接 return
                default:
                    Debug.LogWarning($"[BattleSfx] 攻击类型 `{type}` 不在原版那五档（0/1/2/3/4）里 ——"
                                   + "原版这一支会抛 `ArgumentOutOfRangeException`，我们**不抛**：出声 + 不播。");
                    return null;
            }
        }

        /// <summary>自检用：一共喊过几次（**不受 `Enabled` 影响**）。</summary>
        public static int Played { get; private set; }
        /// <summary>自检用：经本类**请求播放**（`Play` 直放 / `EnqueueLegacySoundAsset` 出队）、被 `WFSoundPlayer` 的冷却挡掉几次（**不是错**，原版就这么节流）。</summary>
        public static int Throttled { get; private set; }
        /// <summary>自检用：最后一次喊的字段名（没喊过 = null）。</summary>
        public static string LastField { get; private set; }
        /// <summary>自检用：最后一次真的播出去的 clip 名（没播 = null）。</summary>
        public static string LastClip { get; private set; }

        /// <summary>自检用：把**全部读口**清零（计数 / 出队日志 / 节流表）。⚠️ **不碰待播队列** —— 那是 `ResetQueueForTest` 的活（`B117-57`：原来这里连 `Log`/`Drained` 都不清，名字与行为不符）。
        /// 🆕 2026-11-02 `B120-W5`：节流表原来只清 `WFSoundPlayer` 那张（键 = cue 资产）⇒ 战斗域另一张
        /// （`RemnantSfx`，键 = 档名）没跟着清，doc 与行为有缝。这里补上。
        /// 🆕 2026-11-02 `B120-W5b`：壳侧那张（`WindowSounds`，键 = cue 实例）也一起收了 ⇒ 现在「全部节流表」这条
        /// 契约是**真的**成立的。调用链 = 本方法 → `WFSoundPlayer.ResetThrottle()`（①④）·
        /// `RemnantSfx.ResetThrottle()`（②）· `WindowSounds.ResetThrottle()`（③）。</summary>
        public static void ResetCounters()
        {
            Played = 0;
            Throttled = 0;
            LastField = null;
            LastClip = null;
            Drained = 0; Log.Clear(); WarpforgeVFX.WFSoundPlayer.ResetThrottle();
            RemnantSfx.ResetThrottle();        // ② 那张字符串键的节流表（判据 = 「全部读口」这条契约本身）
            WindowSounds.ResetThrottle();      // ③ 壳侧那张（2026-11-02 `B120-W5b` 补；调用链见 `WindowSounds.ResetThrottle` 的 doc）
        }

        // 🔴 2026-11-02 `B117-WSOUNDCOOL`：这里原有 `_lastAt`（**字段名** → 上次播的时刻）—— 已删。
        //   冷却表现在住在 `WFSoundPlayer`（键 = **cue 资产**，原版 `AudioCue.lastTimePlayed` 的位置）。
        /// <summary>取不到 cue 的字段（**只警告一次** —— 逐次刷屏会把别的告警淹掉）。</summary>
        static readonly HashSet<string> _warnedMissing = new HashSet<string>();

        /// <summary>
        /// 按**原版字段名**喊一条战斗音效（原版一律 `SoundManager.Play2D(cue, MixerType.FX)` ⇒ 2D）。
        /// 返回 true = **cue 找到了、当场交给了播放器**（2026-11-02 `B120-W5` 起**直放**，原版同一拍就是这么做的；
        /// 见下面 `PlayCore` 的判据）。**取不到 cue 会出声警告**（项目红线：不许静默失败）。
        /// </summary>
        public static bool Play(string field) { return PlayCore(field, Vector3.zero, true); }

        /// <summary>**3D 那一档**（原版 `SoundManager.Play3D`）：`minionDeathCUE`（在单位位置）与
        /// `landOnGround`（在格位）走它，其余 B 组那条一律 `Play2D`。
        /// 判据 → `普查产出_1016战斗/T06c_B组九条触发点.md`（逐条带反编译出处）。</summary>
        public static bool Play(string field, Vector3 worldPos) { return PlayCore(field, worldPos, false); }

        static bool PlayCore(string field, Vector3 worldPos, bool is2d)
        {
            Played++;
            LastField = field;

            WarpforgeVFX.WFSoundCue cue;
            if (!TryResolve(field, out cue)) return false;

            // 🔴 **2026-11-02（`B120-W5` · F-4）：这里【直放】，不再入队。**
            //    判据（逐处现读 `d:/2/tools/decomp_full/`）—— 原版**每一条战斗 cue 的调用点都是当场直接播**：
            //      · `CardScript__DrawCard.c:50` → `SoundManager__Play2D`
            //      · `CardScript__ChangeAttackType.c:53` → `SoundManager__Play2D`
            //      · `CardScript__LandOnGround.c:41` → `SoundManager__Play3D`（带落点）
            //      · `CardScript._UnitDeath_d__446__MoveNext.c:368` → `SoundManager__Play3D`
            //      · `BattleManager__EndTurnClick.c:53,173` · `ClockManager__SetNoActionsUi.c:53`
            //        · `ClockManager__EndCountdown.c:34` · `CardScript__StartMovingFromHand.c:45`
            //        · `CardScript__ResolveActiveAbilityPlayed.c:144` · `CardScript__Stun.c:155` …（同一形状）
            //    而 `BattleManager.soundsToPlay`（`this+0x428`）那条 FIFO **只**吃
            //    `BattleManager.PlaySoundAsset(SoundAsset_OLD)`（`BattleManager__PlaySoundAsset.c` 全文只有「append」一句；
            //    唯一生产方 = `AiScripted.cs:2060`），泵 = `BattleManager__Update.c:152` 的
            //    `PlayNextSoundInQueue`（唯一消费方）。⇒ **两条路互不相干**，战斗 cue 不该进那条 FIFO。
            //    ⚠️ 改前我们把整族 27 条 cue 全塞进 FIFO、`Tick` 每帧出队一条
            //    ⇒ 同一拍的多条（落地 + 攻击 + 受击）被摊到**连续帧**（≈16 ms × N），原版是同一帧一起响。
            //    ⚠️ `timeToPlayAgain` 那道闸本来就在 `WFSoundPlayer.Play` 里（原版 = `AudioCue.ConfigAudioSource.c:19-21`），
            //    改直放之后闸的位置不变，只是早了一帧。
            var played = WarpforgeVFX.WFSoundPlayer.Play(cue, worldPos, is2d);
            // 被冷却挡掉 = `Play` 返回 null 且它把 `LastCueThrottled` 置真（本侧只观察、不在这里判）
            if (played == null && WarpforgeVFX.WFSoundPlayer.LastCueThrottled) Throttled++;
            if (played != null) LastClip = played.name;
            return true;
        }

        /// <summary>
        /// 原版 `BattleManager.PlaySoundAsset(SoundAsset_OLD soundAsset)` 那条路：**只入队、不立刻播**。
        /// 判据 = `d:/2/tools/decomp_full/BattleManager__PlaySoundAsset.c:18-52`（`soundAsset != null &amp;&amp;
        /// soundAsset.clip != null` ⇒ 往 `soundsToPlay`（`this+0x428`，`List&lt;SoundAsset_OLD&gt;`）尾部 append，
        /// **整支没有第二句**）＋ `BattleManager__PlayNextSoundInQueue.c:20-67`（一次消费一条，播不播都 `RemoveAt(0)`）
        /// ＋ 泵点 `BattleManager__Update.c:152`（每帧一句）。
        /// <para>⚠️ **本 build 里这条路我们零生产方**：原版唯一调用 = `AiScripted.cs:2060`（AI 脚本演出），
        /// 我们还没复刻那一段（见 `报告/B120_F_音效语音BGM.md` §七）。本方法**故意留着**，只为不丢原版形状
        /// —— 同一条 FIFO 的参数/复位契约由 `B117_G10_12_Assert` A 组盯着（那一组改成调本方法）。</para>
        /// <para>🔴 2026-11-02（`B120-W17` · F18）：**clip / 音高 / 音量在入队这一刻就定死**（原版入队即持
        /// `SoundAsset_OLD` 的 clip + volume）⇒ `DrainOne` 出队时不再查表、不再随机挑。见 <see cref="Pending"/>。</para>
        /// </summary>
        public static bool EnqueueLegacySoundAsset(string field)
        {
            Played++;
            LastField = field;
            WarpforgeVFX.WFSoundCue cue;
            if (!TryResolve(field, out cue)) return false;
            Enqueue(field, cue);   // 原版 `PlaySoundAsset` 只有 2D 一档（没有落点参数）
            return true;
        }

        /// <summary>原版字段名 → cue（两个播放入口共用；取不到**只警告一次**，逐次刷屏会把别的告警淹掉）。</summary>
        static bool TryResolve(string field, out WarpforgeVFX.WFSoundCue cue)
        {
            cue = null;
            if (string.IsNullOrEmpty(field)) return false;
            if (!WarpforgeVFX.WFSoundBank.TryGetCue(field, out cue) || cue == null)
            {
                if (_warnedMissing.Add(field))
                    Debug.LogWarning($"[BattleSfx] 战斗音效 `{field}` 在 cue 表里查不到 —— 跑一次 " +
                                     "`python 工具/import_battle_sfx.py` 生成 `Resources/battle_sounds.json`。" +
                                     "（⚠️ 若它属于「原版自己就静音的那些」，见 `Battle/BattleSfx.cs` 文件头。）");
                return false;
            }
            return true;
        }

        // ============================================================================================
        //  音效 FIFO（原版 `BattleManager.soundsToPlay` + `PlayNextSoundInQueue`）· 2026-10-26 `B9_E5P12`
        //  🔴 **2026-11-02（`B120-W5` · F-4）：这条 FIFO 现在【只】服务 `EnqueueLegacySoundAsset`
        //     （= 原版 `PlaySoundAsset(SoundAsset_OLD)`）**。战斗 cue 那 27 条已经改回**直放**（见 `PlayCore`）——
        //     改前它们全被塞进这里、`Tick` 每帧出队一条 ⇒ 同一拍的多条被摊到连续帧。
        //     ⚠️ 本条 FIFO 目前**零生产方**（原版唯一生产方 = `AiScripted.cs:2060`，我们还没复刻），
        //     ⇒ 机制与复位契约保留，泵（`Tick` / `InstallPump`）照旧，只是不再有战斗 cue 进来。
        //  🔴 **2026-11-02（`B120-W17` · F18）：载荷改成「入队时已解好的 clip」**（原版 `SoundAsset_OLD`
        //     在入队那一刻就带着 clip + volume；`PlayNextSoundInQueue` 出队时只读那个结构体）。
        //     改前我们只存「字段名 + cue」，clip 要等出队才由 `WFSoundPlayer` 随机挑 ⇒ 与原件不同形。
        //     现在 clip / 音高 / 音量在 `Enqueue` 里定死，`DrainOne` **不查表**；冷却闸仍在出队（见 `DrainOne`）。
        // ============================================================================================
        //  判据（第一权威，逐句现读 `d:/2/tools/decomp_full/`，并用 ILSpy `out_cpp2il_dev_ilspy` 交叉）：
        //   · **入队** `BattleManager__PlaySoundAsset.c:18-52`（ILSpy `BattleManager.cs:41132 PlaySoundAsset`）：
        //       `if (soundAsset != null && soundAsset.clip != null)` ⇒ 往 `soundsToPlay`（`this+0x428`，
        //       `List<SoundAsset_OLD>`）**尾部 append**。**整支没有第二句** —— 入队 ≠ 播。
        //   · **出队** `BattleManager__PlayNextSoundInQueue.c:20-67`（ILSpy `:47960`）：
        //       ① `soundsToPlay.Count < 1` ⇒ return；
        //       ② 取 `[0]`，`clip != null` 且 `GameStaticData.SoundFxVolume > 0`
        //          ⇒ `audioSource.PlayOneShot(clip, SoundFxVolume * soundAsset.volume)`；
        //       ③ **无论播没播**都 `RemoveAt(0)`（`.c:64-65` / ILSpy 末句）—— 一次消费**一条**；
        //       ④ 泵点 = `BattleManager__Update.c:152`，**每帧一句、不分道**（队列只有一条，不参与 4 条动作道）。
        //     ⚠️ 全库 `PlayNextSoundInQueue` 只有这一个调用点（`grep -rl` 两棵树都只有 `Update`）。
        //
        //  🔴 **出队节奏：默认 = 原版「每帧取一条」**（2026-10-26 调度台裁定，依据 = 他实读
        //    `decomp_full/BattleManager__Update.c:152` 里那句 `BattleManager__PlayNextSoundInQueue(param_1, 0);`
        //    就在 `Update` 里）⇒ 同一帧入队的 3 条会在**连续 3 帧**（≈16 ms 间隔）里放出去，
        //    `PlayOneShot` 之间**仍然是叠着响的**。
        //    ⚠️ 派活包原来那句「3 条**串行**（间隔 ≈ clip 时长）」**是我们自己编的验收口、不是原版** ——
        //    调度台已在 `判据/B9_剩余C_派活包.md` §2·P-12 就地订正（带更正痕迹）。
        //  ⇒ 两档都在（`Gap` 开关），**默认照原版**：
        //     · `GapPolicy.OnePerFrame`（**默认 = 原版形状**）= 每次 Tick 至多一条、不看时长；
        //     · `GapPolicy.SerialByClipLength` = 🔶 **我们多出来的可选行为**（等前一条 clip 播完再取下一条）。
        //  ⚠️ 串行档会**拉长演出窗口**（施工单自己也点了这条风险）⇒ 只当演示/调试用，⛔ 别当默认。
        //
        //  ⚠️ **`LastField` 仍然在【入队】那一刻写**（不是出队）—— 既有自检
        //     （`Editor/BattleScene.cs:39322-39331` / `:40203`）问的是「这一拍**请求**了哪条 cue」，
        //     改成出队才写会让它们在批处理（没有帧循环、队列不排）里全部读空。

        /// <summary>出队节奏（见上「出队节奏」那一节）。</summary>
        public enum GapPolicy
        {
            /// <summary>**原版形状**：每次 <see cref="Tick"/> 至多出队一条，不等时长
            /// （判据 = `BattleManager__Update.c:152`：`PlayNextSoundInQueue` 就在 `Update` 里，一帧一句）。
            /// ⛔ **这一档不是「我们挑的」**，是照抄。</summary>
            OnePerFrame = 0,
            /// <summary>🔶 自己设计（非原版，**可选、默认关**）：等前一条 clip 播完再取下一条
            /// （`间隔 ≈ clip.length`）。为什么是我们挑的：原版是 `Update` 每帧取一条
            /// （`BattleManager__Update.c:152`），**没有**按 clip 时长排队的逻辑；这一档只为
            /// 「演示 / 调试时不想三条音效叠在一起」而留（调度台 2026-10-26 裁定：
            /// 默认必须是原版那一档，串行只作可选项）。⚠️ 会**拉长演出窗口**。</summary>
            SerialByClipLength = 1,
        }

        /// <summary>出队节奏 —— **默认 = 原版「每帧取一条」**（`BattleManager__Update.c:152`）。
        /// 串行档（<see cref="GapPolicy.SerialByClipLength"/>）是 🔶 可选演示，⛔ 别当默认。</summary>
        public static GapPolicy Gap = GapPolicy.OnePerFrame;

        /// <summary>队列里一条待播。**载荷 = 原版 `SoundAsset_OLD` 的三样**：clip + 音量（+ 我们的音高）。
        /// 🔴 2026-11-02（`B120-W17` · F18）：改前这里只存「字段名 + cue」，clip 在**出队那一刻**才由
        /// `WFSoundPlayer` 随机挑 —— 与原版「入队时已持有 clip」（`BattleManager__PlaySoundAsset.c:18-52`
        /// 把整个 `SoundAsset_OLD`（`+0x10 = clip` / `+0x18 = volume`）压进 `soundsToPlay`）不同。
        /// 现在 **clip / 音高 / 音量都在入队那一刻定死**（`Enqueue` 里从 cue 的区间取一次随机，
        /// 分布与原版 cue 的 `Random.Range(min,max)` 相同），`DrainOne` **不再查任何表**。
        /// ⚠️ 原版这条 FIFO **只有 2D 一档**（`PlaySoundAsset` 没有落点参数）⇒ 本结构不再带 `pos`/`is2d`。
        /// `cue` 留着只为一件事：**冷却键**（原版 `lastTimePlayed` 住在 `AudioCue` 资产上，偏移 `+0x34`）。</summary>
        struct Pending
        {
            public string field;
            public WarpforgeVFX.WFSoundCue cue;
            public AudioClip clip;        // 入队时已解好（`WFSoundBank.PickClip`）—— 取不到 = null（那一条出队时静音）
            public float pitch;
            public float volume;
        }

        /// <summary>**出队**的一条（自检读口：队列归 0 / 间隔 ≈ clip 时长都靠这张表）。⚠️ `clip == null` = 这一条**没播出去**（被 `timeToPlayAgain` 挡掉）—— 原版「播不播都出队」，所以这张表是**出队记录**、不是「响过几次」；要数真响的用非 null 的条目（`B117-57` 同族）。</summary>
        public struct PlayRecord
        {
            public string field;      // 原版字段名
            public string clip;       // 真播出去的那条 clip 名（没播 = null）
            public float at;          // 出队时刻（本类自己的时钟，见 `Clock`）
            public float length;      // 那条 clip 的时长（没播 = 0）
        }

        /// <summary>队首待播条数（原版 `soundsToPlay.Count`）。</summary>
        public static int QueueLength { get { return _queue.Count; } }
        /// <summary>自检：本次共出队几条（**含被 `timeToPlayAgain` 挡掉的**）。</summary>
        public static int Drained { get; private set; }
        /// <summary>自检：按**出队**顺序记下的记录（最多保留 <see cref="MaxLog"/> 条）。⚠️ 含被 `timeToPlayAgain` 挡掉的那些（`clip == null`）⇒ 它是「出队日志」、**不是**「播放次数」（播放次数看 `Played`；`B117-57` 同族）。</summary>
        public static readonly List<PlayRecord> Log = new List<PlayRecord>();
        /// <summary>日志上限（只防无界增长；正常一局几千条）。</summary>
        public const int MaxLog = 512;
        /// <summary>结构化兜底：队列涨到这个数就出声 + 丢弃（原版没有上限；我们防「泵没接 / 一直入队」跑飞）。</summary>
        public const int MaxQueued = 256;

        static readonly Queue<Pending> _queue = new Queue<Pending>();
        /// <summary>本类自己的时钟（秒）。⚠️ **不用 `Time.time`** —— 批处理没有帧循环、它不走；
        /// 由 <see cref="Tick"/> 的 `dt` 推进 ⇒ 自检可以拿假时间精确验「间隔 ≈ clip 时长」。</summary>
        public static float Clock { get; private set; }
        static float _nextAt;                 // 串行档：下一条最早可播的时刻
        static bool _queueOverflowWarned;

        static void Enqueue(string field, WarpforgeVFX.WFSoundCue cue)
        {
            if (_queue.Count >= MaxQueued)
            {
                if (!_queueOverflowWarned)
                {
                    _queueOverflowWarned = true;
                    Debug.LogError($"[BattleSfx] 音效队列已经 {_queue.Count} 条还没被抽干 ⇒ **泵没接上**？"
                                 + "（原版 `PlayNextSoundInQueue` 由 `BattleManager.Update` 每帧调一次；"
                                 + "我们的泵 = `BattleSfx.Install()` 的 PlayerLoop 子系统，或宿主手调 `Tick(dt)`）"
                                 + "。本条之后**继续丢弃**新入队的声音（不静默：只喊这一次）。");
                }
                return;
            }
            // 🔴 2026-11-02（`B120-W17` · F18）：**clip / 音高 / 音量在入队这一刻定死** ——
            //   原版 `PlaySoundAsset` 压进 `soundsToPlay` 的是整个 `SoundAsset_OLD`（`+0x10 = clip`、`+0x18 = volume`）
            //   ⇒ 出队时**不再查表、不再随机挑**（`PlayNextSoundInQueue.c:20-67` 只读那个结构体）。
            //   随机分布：**音量**仍取 cue 的 `minVolume..maxVolume` 一次（与 `WFSoundPlayer.PlayPrepared` 同取法）；
            //   **音高不取**（原版 `SoundAsset_OLD` 没有 pitch ⇒ 见下面那五行）。
            //   ⚠️ clip 取不到（`Resources/Art/` 是 gitignore 的）⇒ 存 null，那一条出队时**静音**但照旧出队
            //   （原版 `.c:64-65`「播不播都 RemoveAt(0)」）；`WFSoundBank.Clip` 已经为缺失的 clip 出过一次声。
            var clip = cue != null ? WarpforgeVFX.WFSoundBank.PickClip(cue, Rng) : null;
            // 🔴 2026-11-03（调度台裁定 · `B120-W29` §⑤·2）：**音高恒 1.0** —— 原版 `SoundAsset_OLD` 只有
            //   `+0x10 clip` / `+0x18 volume`（**没有 pitch**）⇒ 这条 FIFO 不许再掷音高（cue 的 `min/maxPitch`
            //   只服务直放那条路）。⛔ 别为了「听起来更活」再把它加回来。
            float vol = 1f;
            if (cue != null)
                vol = Mathf.Lerp(cue.minVolume, cue.maxVolume, (float)Rng.NextDouble());
            _queue.Enqueue(new Pending { field = field, cue = cue, clip = clip, pitch = 1f, volume = vol });
        }

        /// <summary>推进一步（宿主每帧调：`BattleSfx.Tick(Time.unscaledDeltaTime)`）。
        /// 返回 true = 这一拍真的出队并处理了一条。**串行档**下若上一条还没播完则什么都不做。</summary>
        public static bool Tick(float dt)
        {
            if (dt > 0f) Clock += dt;
            if (_queue.Count == 0) return false;
            if (Gap == GapPolicy.SerialByClipLength && Clock < _nextAt) return false;
            return DrainOne();
        }

        /// <summary>出队一条（原版 `PlayNextSoundInQueue`）。返回 true = 处理了（播了或按节流跳过）。</summary>
        public static bool DrainOne()
        {
            if (_queue.Count == 0) return false;
            var p = _queue.Dequeue();
            Drained++;

            // 原版 `.c:64-65`：**播不播都出队** —— 所以「clip 取不到 / 静音 / 节流」这三种都不留队。
            float length = 0f;
            string clipName = null;
            // 冷却闸 = 全项目**唯一**实现 `WFSoundPlayer.CanPlayByCooldown(cue)`（判据 = `AudioCue.ConfigAudioSource.c:19-21`，
            //   键 = **cue 资产**：原版 `lastTimePlayed` 就住在 `AudioCue` 上，偏移 `+0x34`）。
            //   2026-11-02 `B117-WSOUNDCOOL` 把这里原来按**字段名**自己判的那一份删了（同一道闸写两份 = 迟早不一致）。
            //   三件时序照旧：`LogAdd(字段, null, 0)`（`Log` 是**出队**日志、不是播放次数）· `ScheduleNext(0)`（⛔ 别改值）。
            //   ⚠️ 闸排在 `!Enabled` **之外** ⇒ 批处理关掉出声时仍旧冷却（旧写法、原版都这样；也是 `B117_G10_12` 的 A2 前提）。
            //   冷却状态只有一份，`ResetCounters()` 会一并清。

            // 🔴 2026-11-02（`B120-W17` · F18）：**冷却闸仍在出队这一拍**（原版 `SoundManager.Play` 里那道
            //    `AudioCue.ConfigAudioSource.c:19-21`，键 = cue 资产）—— 闸一过就播**入队时已经解好的那条 clip**。
            //    ⇒ 出队这一侧**不再查任何表、也不再随机挑**：原版 `PlayNextSoundInQueue.c:20-67` 只读那个
            //    `SoundAsset_OLD`（clip + volume 都在入队时定死）。⚠️ 闸排在 `!Enabled` **之外**（批处理关掉出声时
            //    仍旧冷却）—— 这一条是 `B117_G10_12` A2 的前提，⛔ 别把它挪进 `Enabled` 分支里。
            if (p.cue != null && !WarpforgeVFX.WFSoundPlayer.CanPlayByCooldown(p.cue))
            {
                Throttled++;
            }
            else if (p.clip != null && WarpforgeVFX.WFSoundPlayer.Enabled)   // 🔴 2026-11-03（`B120-W29` · F22）：出队那一跳改走**原版 `BattleManager.audioSource`**（类尾 `LegacySource()`）—— 改前是 `WFSoundPlayer.Play(AudioClip…)`，它把源写死 `FxGroup`
            {
                // 原版 `PlayNextSoundInQueue.c:37-40/44-60`：`clip != null` ∧ `SoundFxVolume > 0` ⇒ `PlayOneShot(clip, SoundFxVolume * soundAsset.volume)`
                // ⇒ 源**不挂混音组**（场景实例 `AudioSource_1595` 的 `OutputAudioMixerGroup {0,0}` = Master）、音量 = 滑块 × 入队音量（两条都在 `PlayLegacyOnMaster` 里）
                // 🔴 2026-11-03（`B120-W29` · F22）：⛔ 别再退回 `WFSoundPlayer.Play(AudioClip…)` —— 那个口把源写死 `FxGroup`（本行上面那句注释里的结构差就是它）
                if (PlayLegacyOnMaster(p.clip, p.volume, p.pitch))   // 闸挡了返回 false ⇒ 照原版「播不播都出队」走 `LogAdd(field, null, 0)`
                {
                    clipName = p.clip.name;
                    LastClip = clipName;
                    length = p.clip.length;
                }
            }
            LogAdd(p.field, clipName, length);
            ScheduleNext(length);
            return true;
        }

        static void ScheduleNext(float playedLength)
        {
            if (Gap == GapPolicy.OnePerFrame) { _nextAt = Clock; return; }   // 原版：下一次 Tick 立刻可取
            _nextAt = Clock + (playedLength > 0f ? playedLength : 0f);
        }

        static void LogAdd(string field, string clip, float length)
        {
            if (Log.Count >= MaxLog) Log.RemoveAt(0);
            Log.Add(new PlayRecord { field = field, clip = clip, at = Clock, length = length });
        }

        /// <summary>**把队列抽干**（自检用：批处理没有帧循环，靠它一次跑完）。
        /// 串行档下 `dt` 冒充「时间流逝」—— 每抽一条就按上一条的时长 >> 推进时钟，
        /// 这样记录里的 `at` 差值就真的等于「上一条 clip 的时长」。</summary>
        public static int DrainAll()
        {
            int n = 0;
            int guard = 0;
            while (_queue.Count > 0 && guard++ < MaxQueued * 4)
            {
                if (Gap == GapPolicy.SerialByClipLength)
                {
                    float need = _nextAt - Clock;
                    if (need > 0f) Clock += need;          // 假时间：跳到「上一条播完」
                }
                if (DrainOne()) n++;
            }
            return n;
        }

        /// <summary>**整场重来**：清空待播队列 + 时钟，**并**把全部读口清零（`B117-57`：原来这里不清计数）。
        /// 与 <see cref="ResetCounters"/> 的分工 = 那个只清读口、**不碰队列**；要一次清干净就用本方法。</summary>
        public static void ResetQueueForTest()
        {
            _queue.Clear();
            Clock = 0f;
            _nextAt = 0f;
            _queueOverflowWarned = false;
            ResetCounters();
        }

        // ---- 每帧泵（原版 = `BattleManager.Update` 里那一句；我们没有帧循环，挂 Unity 的 PlayerLoop）----
        /// <summary>进 Play / 启动时装一次泵。
        /// ⚠️ 这个入口**故意挂在 `BattleSfx` 自己（而不是嵌套类 `Pump`）**上 ——
        /// `RuntimeInitializeOnLoadMethod` 只扫「静态方法」，嵌套类里的那条**不保证被扫到**
        /// （Unity 文档没写死，本仓按保守做法放在外层）。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InstallPump() { Pump.Attach(); }

        /// <summary>把 <see cref="Tick"/> 接到 Unity 自带每帧泵的 `Update` 阶段。
        /// <para>**为什么需要它**：入队之后**没人抽**就等于「全部静音」，而批处理/编辑模式**不跑 PlayerLoop**
        /// （那是 `EditorApplication.update`）⇒ 自检走 <see cref="DrainAll"/> / 手调 <see cref="Tick"/>。
        /// 同款先例 = `Shell/LiveOpsHandler.cs` 的 `LiveOpsPump`（含「为什么不是 `Application.update`」那条实测）。</para></summary>
        public static class Pump
        {
            static bool _installed;
            /// <summary>已插进 PlayerLoop 没有（诊断读口）。</summary>
            public static bool Attached { get { return _installed; } }

            /// <summary>幂等安装（域重载关掉时静态字段会跨 Play 存活 ⇒ 不查就会越插越多）。</summary>
            public static void Attach()
            {
                var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
                if (Contains(loop)) { _installed = true; return; }
                if (!Insert(loop, typeof(UnityEngine.PlayerLoop.Update)))
                {
                    Debug.LogError("[BattleSfx] 没找到 PlayerLoop 的 `Update` 阶段 ⇒ 战斗音效**不会自动出队**"
                                 + "（表现层会静音；自检的 `DrainAll` 不受影响）");
                    return;
                }
                UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
                _installed = true;
            }

            /// <summary>摘掉（自检 / 手动接管时间时用）。重复调用安全。</summary>
            public static void Detach()
            {
                var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
                if (Remove(loop)) UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
                _installed = false;
            }

            static void OnUpdate() { Tick(Time.unscaledDeltaTime); }

            static bool Contains(UnityEngine.LowLevel.PlayerLoopSystem sys)
            {
                var subs = sys.subSystemList;
                if (subs == null) return false;
                for (int i = 0; i < subs.Length; i++)
                {
                    if (subs[i].type == typeof(Pump)) return true;
                    if (Contains(subs[i])) return true;
                }
                return false;
            }

            static bool Insert(UnityEngine.LowLevel.PlayerLoopSystem sys, System.Type phase)
            {
                var subs = sys.subSystemList;
                if (subs == null) return false;
                for (int i = 0; i < subs.Length; i++)
                {
                    if (subs[i].type == phase)
                    {
                        var inner = subs[i].subSystemList;
                        var list = new List<UnityEngine.LowLevel.PlayerLoopSystem>(inner == null ? 0 : inner.Length);
                        if (inner != null) list.AddRange(inner);
                        list.Add(new UnityEngine.LowLevel.PlayerLoopSystem
                        {
                            type = typeof(Pump),
                            updateDelegate = OnUpdate,
                        });
                        subs[i].subSystemList = list.ToArray();
                        return true;
                    }
                    if (Insert(subs[i], phase)) return true;
                }
                return false;
            }

            static bool Remove(UnityEngine.LowLevel.PlayerLoopSystem sys)
            {
                var subs = sys.subSystemList;
                if (subs == null) return false;
                for (int i = 0; i < subs.Length; i++)
                {
                    if (subs[i].type == typeof(Pump))
                    {
                        var inner = subs[i].subSystemList ?? new UnityEngine.LowLevel.PlayerLoopSystem[0];
                        var list = new List<UnityEngine.LowLevel.PlayerLoopSystem>(inner);
                        list.RemoveAll(s => s.type == typeof(Pump));
                        subs[i].subSystemList = list.ToArray();
                        return true;
                    }
                    if (Remove(subs[i])) return true;
                }
                return false;
            }
        }

        // ============================================================================================
        //  🆕 2026-11-02（`B120-W17` · F18）· 挂在**类尾**（⛔ 别往中间插：本文件被 `账/战斗逻辑驱动.tsv`
        //  按 `文件:行号` 引用）
        // ============================================================================================

        /// <summary>入队时挑 clip / 取音高音量用的伪随机序列（与 `WFSoundPlayer.Rng` 同规矩：
        /// 用 `System.Random` 固定种子而不是 `UnityEngine.Random`，免得「同一段录屏重跑一遍音不一样」）。</summary>
        static readonly System.Random Rng = new System.Random(20261102);

        /// <summary>自检读口：**队首那条在入队时已经解好的 clip 名**（队列空 = null）。
        /// 🧨 它非空 = 「入队即持 clip」这条契约成立（改回「出队那一刻才挑」⇒ 入队后读它恒 null）。</summary>
        public static string FrontClip
        {
            get
            {
                if (_queue.Count == 0) return null;
                var p = _queue.Peek();
                return p.clip != null ? p.clip.name : null;
            }
        }

        // ============================================================================================
        //  🆕 2026-11-03（`B120-W29` · `B120-F22`）：legacy FIFO 的**输出源** = 原版 `BattleManager.audioSource`
        //  —— 追加在**类尾**（⛔ 别往中间插：本文件被 `账/战斗逻辑驱动.tsv:461/464` 按 `文件:行号` 引用）
        // ============================================================================================
        //  判据（逐句现读）：
        //   · `decomp_full/BattleManager__PlayNextSoundInQueue.c:48` —— `PlayOneShot` 打的是 `*(this+0x90)` 那颗 2D 源；
        //   · 场景实例 `bundle_scenes_scenes_battlearena1/MonoBehaviour/AudioSource_1595.json`：
        //     `OutputAudioMixerGroup {fileID:0, pathID:0}` ⇒ **无混音组 = Master**（不是 FX、不是 Voices）；
        //     同一份里 `m_Volume = 1.0` · `Priority = 154` · `Spatialize = false`。
        //   · `.c:44-46` 闸 = `SoundFxVolume > 0`（=0 整条不播，**不是**以 0 音量播）；`:55-60` 音量 =
        //     `SoundFxVolume * soundAsset.volume`。
        //  ⇒ 另起一颗源（`WFSoundPlayer` 的池把 `outputAudioMixerGroup` 写死 `FxGroup`，⛔ 别去改它：
        //     那一族服务 900+ 条特效音效）。形状与 `Battle/CampaignScriptRunner.SoundSource()` 逐句一致
        //     （同一条原版路、同一份判据 —— 那边是战役脚本宿主，这边是战斗 FIFO）。
        //  ⚠️ 改前走 `WFSoundPlayer.Play(AudioClip…)` ⇒ 被写成 `FxGroup`（结构差；该路径今天**零生产方**、
        //     无可听差异，判据见 `报告/B120_W17_执行.md` §二 末尾）。

        static AudioSource _legacySrc;

        /// <summary>legacy FIFO 的音频源（原版 `BattleManager.audioSource` = 场景实例 `AudioSource_1595`）。
        /// **故意不写 `outputAudioMixerGroup`** —— 原版是 `{fileID:0, pathID:0}` ⇒ 走 Master
        /// （⛔ 别挂 `FxGroup` / `VoicesGroup`）。</summary>
        static AudioSource LegacySource()
        {
            if (_legacySrc != null) return _legacySrc;
            var go = new GameObject("WFBattleLegacySfx");
            // 批处理自检里的运行时对象别存进场景（同 `WarpforgeAudio.PlayJingle` 的做法）；
            // `DontDestroyOnLoad` 只在播放态调 —— 它在编辑器 / 批处理里会抛（同族先例见 `RemnantSfx`）。
            go.hideFlags = HideFlags.DontSave;
            if (Application.isPlaying) Object.DontDestroyOnLoad(go);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;      // 原版 `Spatialize false`（2D）
            src.volume = 1f;            // 原版 `m_Volume = 1.0` —— 音量全在 `PlayOneShot` 的实参上
            src.priority = 154;         // 原版 `Priority 154`
            // ⛔ 这里**故意**不写 `outputAudioMixerGroup`：原版 `{fileID:0, pathID:0}` ⇒ Master。
            _legacySrc = src;
            return src;
        }

        /// <summary>自检读口：那条 Master 源（还没建 = null）。⚠️ `outputAudioMixerGroup == null` 是**契约**
        /// （不是「还没设」）—— 原版那颗源的 `OutputAudioMixerGroup` 全 0。</summary>
        public static AudioSource LegacySourceForTest { get { return _legacySrc; } }

        /// <summary>自检用：把那条 Master 源建出来（**不播**）—— 断言要读它的 `outputAudioMixerGroup` / 三个字段。</summary>
        public static AudioSource EnsureLegacySourceForTest() { return LegacySource(); }

        /// <summary>自检读口：最近一次 legacy 播放的**音量实参**（= `SoundFx × 入队音量`；没播过 / 被闸挡 = `-1` / `0`）。</summary>
        public static float LastLegacyPlayVolume { get; private set; } = -1f;

        /// <summary>原版 `PlayNextSoundInQueue` 的**播放那一跳**（`.c:44-60`）：闸 `SoundFxVolume > 0` ·
        /// 音量 = `SoundFxVolume * soundAsset.volume` · 源 = 无混音组（Master）。
        /// 返回 false = 这一条**没响**（闸挡 / 没 clip）—— 调用方照原版「播不播都出队」。</summary>
        static bool PlayLegacyOnMaster(AudioClip clip, float volume, float pitch)
        {
            if (clip == null) return false;
            float fx = WarpforgeAudio.SoundFx;              // = 原版 `GameStaticData.SoundFxVolume`（活值，同 `CampaignScriptRunner.PlayNextSoundInQueue`）
            if (fx <= 0f) { LastLegacyPlayVolume = 0f; return false; }   // `.c:44-46`：0 ⇒ **整条不播**
            var src = LegacySource();
            if (src == null) return false;
            src.pitch = pitch <= 0f ? 1f : pitch;           // ⚠️ 音高是**我们这一侧的额外量**（原版 `SoundAsset_OLD` 只带 clip + volume，`:0x10` / `:0x18`）
            LastLegacyPlayVolume = fx * volume;             // `.c:55-60`：`SoundFxVolume * soundAsset.volume`
            src.PlayOneShot(clip, LastLegacyPlayVolume);
            return true;
        }

        /// <summary>自检用：按 legacy FIFO 的形状播一条**已解好的 clip**（⛔ 产线别调；只为验路由与音量实参）。</summary>
        public static bool PlayLegacyOnMasterForTest(AudioClip clip, float volume, float pitch)
        {
            return PlayLegacyOnMaster(clip, volume, pitch);
        }
    }
}
