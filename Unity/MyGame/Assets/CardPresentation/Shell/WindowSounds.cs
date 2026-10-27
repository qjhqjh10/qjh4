// WindowSounds.cs — **壳侧音效的唯一一张表**（窗口开/关 · 按钮 · 开包窗）
//
// 为什么单独一个文件：这些 cue **不在** AnimFX 的 `sounds[]` / `exitSounds[]` 里
// （引它们的是组件的序列化 `AudioCue` 字段），所以 `工具/import_original_sfx.py` 收不到 ——
// 它们靠那份脚本的 `EXTRA_CUES` 显式登记（2026-10-17 音效线 `W_G` 加进去的）。
// 🔴 **表只此一份**：`GameWindow` / `WindowButton` / `BoosterPackOpenWindow` 一律转调本类，
// ⛔ 别在各自文件里再写一遍 cue 名（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
//
// ============================ 判据（逐条，2026-10-17 亲读） ============================
// | cue | 原版字段 | 消费者 | clip（`bundle_soundcollection_assets_all`） | 混音组 |
// |---|---|---|---|---|
// | `ButtonClick` | `SoundManager.defaultButtonSound`（MB pid `-5308416599568047990` 的
// |   `defaultButtonSound` = pid `6176553109044381692`） | `EverguildButton.PlayButtonSound`
// |   / `EverguildToggle.PlayButtonSound`（`useDefaultAudioClipIfNull = 1` 且自身 cue 为空时） |
// |   `UI Select Toggle` | FX(0) |
// | `ButtonClick Back` | `SoundManager.defaultCloseSound`（pid `8084683923307806969`） |
// |   `GameWindow.Close`（`GameWindow__Close.c`：守卫之后、`WindowsManager.CloseWindow` **之前**） |
// |   `UIButton Back` | FX(0) |
// | `CardClick 2` | `CardInBoosterPack.clickSound`（prefab pid `3481876391266756950`） |
// |   `CardInBoosterPack.UiColliderOnClick`（`AudioCue.Play2D(cue, 0)`） | `UI Click Card 2` | FX(0) |
// | `New Card` | `CardInBoosterPack.newCardSound`（pid `4962178071637942874`） |
// |   `_WaitForSpinAnimationEnd`（拥有数 == 1，即「这是你第一张」） | `New Card` | FX(0) |
// | `Booster Pack Window Close` | `BoosterPackOpenWindow.soundOnClose`（pid `3416723080278618344`） |
// |   `CloseButtonClick`（播完才 `WaitAnimationForClose` → `Close()`） | `Cardshuffle out` | FX(0) |
// | `Booster Pack Open Card Rarity 1..4` | `CardInBoosterPack.contentByRarities[].audioCue` |
// |   `ChangeState(3)`：`SoundManager.Play2D(cue, **MixerType 3 = Jingles**)` |
// |   `Cardflip_Common / Rare / Epic / Legendary` | **Jingles(3)** |
// | `Booster Open *` | `BoosterPackVisualItem.openBoosterPack`（`Initialize`，
// |   `SoundManager.Play2D(cue, **3**)`） | `Open_Cardpack_*` | **Jingles(3)** |
//
// `MixerType` 枚举（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/MixerType.cs`）= `FX = 0` ·
// `Music = 1` · `Voices = 2` · `Jingles = 3` ⇒ **组号就是这两档**，别互推。
//
// ============================ 播放口的分配 ============================
// · FX 那几条 → `WarpforgeVFX.WFSoundPlayer.Play(cue, …)`（它的音源挂 `FxGroup`，见 `WarpforgeAudio.RouteEffectSounds`）
// · Jingles 那两条 → `WarpforgeAudio.PlayJingle(cue)`（2026-10-17 新加的那个口 —— `WFSoundPlayer`
//   只挂 FX 组，而原版这两条走 Jingles；⛔ 别把它们塞回 FX「反正音量同值、听不出来」——
//   原版 `VolumeJingles` 与 `VolumeFX` 恰好同值，**听不出来正是它接错的保护色**，见 `WarpforgeAudio.JinglesGroup` 的注释）。
using System.Collections.Generic;
using UnityEngine;
using WarpforgeVFX;

namespace CardPresentation
{
    public static class WindowSounds
    {
        // ---- cue 名（表里就是这几串，别改名）----
        public const string DefaultButtonCue = "ButtonClick";        // clip: UI Select Toggle
        public const string DefaultCloseCue = "ButtonClick Back";    // clip: UIButton Back
        public const string BoosterCloseCue = "Booster Pack Window Close";  // clip: Cardshuffle out
        public const string BoosterCardClickCue = "CardClick 2";     // clip: UI Click Card 2
        public const string BoosterNewCardCue = "New Card";          // clip: New Card

        // ---- 逐按钮 cue（原版 `EverguildButton|EverguildToggle.m_onClickSound`）----
        // 🔴 判据 = `资料/普查产出_1017/UI/R3_S01按钮cue映射.md` §二·B（10 个 cue 的包装 / clip / 参数，
        //    全部本地可解析）+ §四·A（我们的节点 → 原版节点 → cue，73 条）。
        // ⚠️ `ButtonClick` / `ButtonClick Back` 就是上面那两条默认音，这里**不再重复定义**。
        // ⚠️ `Select Toggle` 的 clip 是 `UI Click 2_no delay`（**不是** `ButtonClick` 那条 `UI Select Toggle`）——
        //    名字像、资产不同，⛔ 别按名字合并。
        public const string PositiveCue = "ButtonClick Positive";    // clip: UIButtonPress（pitch 0.8–1.2 随机）
        public const string NegativeCue = "ButtonClick Negative";    // clip: UIButtonPress Negative
        public const string SelectToggleCue = "Select Toggle";       // clip: UI Click 2_no delay
        public const string SelectAvatarCue = "SelectAvatar";        // clip: SelectAvatar
        public const string PurchaseCue = "Purchase Click";          // clip: UI PurchaseClick
        public const string StartGameCue = "MenuStartGame";          // clip: StartMatch
        public const string SendMessageCue = "UI Send Message";      // clip: UI Send Message
        public const string Purchase2Cue = "UI Purchase 2";          // clip: UI Purchase 2

        // ---- 窗级 / 特殊 cue（2026-10-18 · 音效线缺口修复 `W_音效线_缺口修复`）----
        // 判据 → `资料/普查产出_1018/AUD_音效线_完整性复核.md` §三 A1/A2/B1/D（逐条带原版 MB pid）。

        /// <summary>卡牌详情窗（`CardDisplayWindow`）**开窗音** —— 场景
        /// `bundle_scenes_scenes_mainmenuwarpforge` 的 `Card Displayer Menu For Menu` 根 MB（pid **2033**）
        /// `openSound = PPtr(fileID 6, pid 4006604330728631164)`。cue 包装
        /// `bundle_soundcollection_assets_all/MonoBehaviour/CardExpand.json`（clip `CardExpand`，
        /// pitch 0.8–1.1 · vol 1–1 · `timeToPlayAgain` 0.1）。
        /// <para>🔴 **组 = FX（MixerType 0）** —— 消费点是继承来的 `GameWindow.TryOpen`
        /// （`SoundManager.Play2D(cue, MixerType.FX)`，与 FTUE 那一条同一条指令流）。
        /// 播放口 = <see cref="PlayFx"/>；`CardDetailPopup.Open()` 里喊它。</para></summary>
        public const string CardExpandCue = "CardExpand";

        /// <summary>卡牌详情窗**关窗音** —— 同一根 MB 的
        /// `closeSound = PPtr(fileID 6, pid 3524674544779823761)`；消费点 = `GameWindow.Close`
        /// （`GameWindow__Close.c` 在 `useDefaultCloseSoundIfNull` 兜底**之前**）。clip 同名（ttp 0.1）。
        /// <para>⚠️ 我们 `GameWindow.closeSound` 是 `AudioClip`、原版是 `AudioCue`（带 pitch/节流参数）
        /// ⇒ 走 cue 表（同 FTUE 的先例）；`CardDetailPopup` 同时把 `useDefaultCloseSoundIfNull`
        /// 置 **false**，防止基类再叠一声默认 `ButtonClick Back`（原版只会响一条）。</para></summary>
        public const string CloseCardDisplayCue = "Close Card Display";

        /// <summary>段位升降窗的 **`soundOnOpen`** —— `bundle_menus_assets_all` MB `-4938671765695524530`
        /// （`RankedEventDivisionChangeWindowNewSystem : GameWindow`，prefab `Ranked Division Change Window`）
        /// `soundOnOpen = PPtr(fileID 6, pid -2236843390636958589)`。cue 包装
        /// `bundle_soundcollection_assets_all/MonoBehaviour/Division Change.json`（clip `Division Increase`，
        /// **`timeToPlayAgain` 0.01**）。
        /// <para>🔴 **组 = Jingles（MixerType 3）** —— 判据 = 反编译
        /// `RankedEventDivisionChangeWindowNewSystem._DoPromoteAnimations_d__22.MoveNext.c:32-33`：
        /// `AudioCue__Play2D(cue, 3)`；`MixerType.cs` = `FX=0 · Music=1 · Voices=2 · Jingles=3`。
        /// ⛔ 别用 `PlayFx`（`WFSoundPlayer` 写死 FX 组）—— 走 <see cref="PlayJingles"/>。</para>
        /// <para>⚠️ 原版落点是「升级动画的起点」；我们的动画没建 ⇒ 落在 `Open()`（见那边的注释）。</para></summary>
        public const string DivisionChangeCue = "Division Change";

        /// <summary>🆕 **2026-10-17（界面线 T26 / 音效线 S02）**：`Choose Army Deck FTUE Window`
        /// （`FTUESelectDeckWindow`）的 **`openSound`** —— 根 MB `MonoBehaviour_-3133570412519170200.json`
        /// 实读 `openSound = PPtr(fileID 6, pid -1332049501696130703)`，那个 cue 的资产名就是这一串
        /// （`素材/Warpforge原版/音频/音效库/MonoBehaviour/Draft Mode Progress Jingle_-1332049501696130703.json`；
        /// `clipList = [Draft Mode GainSkull]` · `timeToPlayAgain = 0.1`）。
        /// <para>🔴 **组 = FX（不是 Jingles）** —— 判据 = 原版 `GameWindow.TryOpen` 的指令流（VA `0x180836120`）：
        /// `mov r8, [rbx+0x28]`（GameWindow 的 `openSound`）→ `xor r9d, r9d` → `call 0x1806FA6F0`，
        /// 而 `0x1806FA6F0` = `SoundManager.Play2D(AudioCue, MixerType)`（`d:/2/tools/il2cpp_out/dump.cs:17901`）
        /// ⇒ `MixerType = 0 = FX`（`MixerType.cs`）。⚠️ `工具/import_original_sfx.py:112` 那句「（Jingles 组）」
        /// 与指令流不符 —— 以指令流为准（同 `Shell/GameWindow.cs` 那条既有口径）。</para>
        /// <para>播放口 = <see cref="PlayFx"/>（`WFSoundPlayer` 挂 FX 组）；`FTUESelectDeckWindow.Open()` 里喊它。</para></summary>
        public const string FtueOpenCue = "Draft Mode Progress Jingle";

        /// <summary>`contentByRarities[]` 的 5 档 → cue（原版分组：`0/1 → Rarity 1` · `2` · `3` · `4/5 → Rarity 4`）。
        /// 🔴 与 `BoosterPackOpenWindow.CardFxFor` 那张**粒子**表同分组、不同资产，⛔ 别合并成一张。</summary>
        public static readonly string[] BoosterRarityCues =
        {
            "Booster Pack Open Card Rarity 1",
            "Booster Pack Open Card Rarity 2",
            "Booster Pack Open Card Rarity 3",
            "Booster Pack Open Card Rarity 4",
        };

        /// <summary>稀有度档（`BoosterPackOpenWindow.RarityInt` 的口径）→ cue 名。</summary>
        public static string BoosterRarityCue(int rarity)
        {
            if (rarity <= 1) return BoosterRarityCues[0];
            if (rarity == 2) return BoosterRarityCues[1];
            if (rarity == 3) return BoosterRarityCues[2];
            return BoosterRarityCues[3];
        }

        /// <summary>开包窗「拆包」那一声（原版 `BoosterPackVisualItem.openBoosterPack`）。
        /// 🔴 **映射的判据 = prefab 上同一组件里 `boosterName` ↔ `openBoosterPack` 的配对**（逐颗实读
        /// `bundle_boosterpacks_assets_all`）：`Armies/Ultramarines → Booster Open Ultramarines` ·
        /// `Armies/Sautekh_short → Booster Open Sautekh` · `Armies/SpaceWolves → Booster Open Space Wolves` ·
        /// **`Armies/Genestealers` 与 `Armies/Leviathan` 共用 `Booster Open Tyranid`** ·
        /// `MenuShop/Alpha/AllArmies → Booster Open Standard`。
        /// ⇒ 我们这扇窗的 `Army`（`Ultramarines/Sautekh/SpaceWolves/Leviathan`，空 = 全卡池）按这张表落。</summary>
        public static string BoosterOpenCue(string army)
        {
            switch (army)
            {
                case "Ultramarines": return "Booster Open Ultramarines";
                case "Sautekh": return "Booster Open Sautekh";
                case "SpaceWolves": return "Booster Open Space Wolves";
                case "Leviathan": return "Booster Open Tyranid";
                default: return "Booster Open Standard";
            }
        }

        // ============================ 播放 ============================

        /// <summary>自检用：真的交给了播放器的次数（不含被 `timeToPlayAgain` 挡掉的）。</summary>
        public static int Played { get; private set; }
        /// <summary>自检用：被 `timeToPlayAgain` 挡掉的次数（**不是错** —— 原版 `AudioCue.CanPlay` 就这么节流）。</summary>
        public static int Throttled { get; private set; }
        /// <summary>自检用：最后一次喊的 cue 名。</summary>
        public static string LastCue { get; private set; }

        public static void ResetCounters()
        {
            Played = 0; Throttled = 0; LastCue = null;
            _lastAt.Clear();
            _warned.Clear();
        }

        static readonly Dictionary<string, float> _lastAt = new Dictionary<string, float>();
        static readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>FX 组（原版 `MixerType.FX`）。</summary>
        public static bool PlayFx(string cueName, string context)
        {
            return Play(cueName, false, context);
        }

        /// <summary>Jingles 组（原版 `MixerType.Jingles`）—— **开包窗那两条**才走它，见文件头那张表。</summary>
        public static bool PlayJingles(string cueName, string context)
        {
            return Play(cueName, true, context);
        }

        static bool Play(string cueName, bool jingles, string context)
        {
            LastCue = cueName;
            if (string.IsNullOrEmpty(cueName)) return false;

            WFSoundCue cue;
            if (!WFSoundBank.TryGetCue(cueName, out cue) || cue == null)
            {
                // 红线（不许静默失败）：表里没有 ⇒ 这一声不会响，而它看起来像「原版就没声音」。
                // 每个名字**只报一次**（逐次刷屏会把别的告警淹掉）。
                if (_warned.Add(cueName))
                    Debug.LogWarning($"[WindowSounds] 音效 cue `{cueName}` 在表里查不到（{context}）—— "
                                     + "跑一次 `python 工具/import_original_sfx.py`（它由 `EXTRA_CUES` 收壳侧这几条）。");
                return false;
            }

            // 原版 `AudioCue.timeToPlayAgain`（这批都是 0.1 s，`Booster Open *` 是 1.0 s）：
            // 同一条刚要过就不再播 —— 这是原版行为，**不是失败**。
            if (cue.timeToPlayAgain > 0f)
            {
                float last;
                if (_lastAt.TryGetValue(cueName, out last) &&
                    Time.realtimeSinceStartup - last < cue.timeToPlayAgain)
                {
                    Throttled++;
                    return false;
                }
            }
            _lastAt[cueName] = Time.realtimeSinceStartup;

            if (jingles) WarpforgeAudio.PlayJingle(cue);
            else WFSoundPlayer.Play(cue, Vector3.zero, true);   // is2d = true ⇒ 原版 `Play2D`

            Played++;
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 **2026-10-27（音频线 `B9` · 执行代理 `B2AU`）：cue→WindowSounds 映射表里那 20 条 UI 缺口的 key**
        //  判据 → `资料/完全复刻/报告/B9_B2AU6_cue映射补缺.md`（① 逐条归类 ② 证据 ③ 待接线）。
        //  这批原来**连 cue 都没有**（不是没接线）：本轮已把它们收进 `工具/import_original_sfx.py` 的
        //  `EXTRA_CUES` ⇒ 音频拷进 `Resources/Art/audio/sfx/`（含 `.meta`）+ `animfx_sounds.json` 重生
        //  （473 → 510 条 cue）。参数（pitch/vol/ttp）由生成器**从原版 cue 资产实读**，⛔ 别在这里再抄一份。
        //  ⚠️ **本段只是 key 注册表**：谁去播由各自的窗决定（本类 `Play(cue)` 是唯一播放口）。
        //  ⚠️ **7 条 `Booster Open *`** 的消费者**已经写在文件头那张表里**（`WindowSounds.cs:28`：
        //     `BoosterPackVisualItem.openBoosterPack`）⇒ 接线时不用再翻 prefab。
        //  ⚠️ cue 名 ≠ clip 名（`Booster Open Orks` → `Open_Cardpack_Goff` 等 8 处）⇒ 见报告 §① 那张表。
        // ══════════════════════════════════════════════════════════════════════════════════════

        // ---- 抽卡开包（7 条；消费者 = `BoosterPackVisualItem.openBoosterPack`）----
        public const string BoosterOpenAstraMilitarumCue = "Booster Open AstraMilitarum";   // clip: Open_Cardpack_AstraMilitarum
        public const string BoosterOpenBlackLegionCue = "Booster Open BlackLegion";         // clip: Open_Cardpack_BlackLegion
        public const string BoosterOpenEmperorsChildrenCue = "Booster Open Emperors Children"; // clip: Open_Cardpack_EmperorsChildren
        public const string BoosterOpenOrksCue = "Booster Open Orks";                       // clip: Open_Cardpack_Goff（⚠️ 名字不同）
        public const string BoosterOpenSaimHannCue = "Booster Open SaimHann";               // clip: Open_Cardpack_SaimHann
        public const string BoosterOpenSororitasCue = "Booster Open Sororitas";             // clip: Open_Cardpack_Sororitas
        public const string BoosterOpenTauCue = "Booster Open Tau";                         // clip: Open_Cardpack_Tau

        // ---- 其余 UI（13 条）----
        public const string ButtonClick1Cue = "ButtonClick 1";                    // clip: UI Click 1（⚠️ 与 `ButtonClick` 的 `UI Select Toggle` **不是同一个 clip**）
        public const string DeckEditingValidDeckCue = "Deck Editing Valid Deck";  // clip: Progress Jingle（`.ogg`）
        public const string DraftPackAppearCue = "Draft Pack Appear";             // clip: CardMove
        public const string DraftPackFinishAnimCue = "Draft Pack Finish Animation"; // clip: UI Click Card 3（⚠️ 与下一条**共用同一个 clip**）
        public const string DraftPackSelectedMovingCue = "Draft Pack Selected Moving"; // clip: CardDraw
        public const string DraftPrebuiltPackPreviewCue = "Draft Prebuilt Pack preview"; // clip: UI Click Card 3（同上，共用）
        public const string EnergyAccumulatedCue = "Energy Accumulated";          // clip: Energy 1
        public const string EnergyAccumulatedUsedCue = "Energy Accumulated Used"; // clip: Energy 2
        public const string ForgeCardCue = "Forge Card";                          // clip: Forge Card
        public const string GachaVaultOpenCue = "Gacha vault open";               // clip: Vault Crate Open Full（`.ogg`）
        public const string RankedSealCue = "Ranked Seal SFX";                    // clip: UI Click 6
        public const string RaidClaimChingCue = "UI Raid Claim Reward Ching";     // clip: UI RaidClaimRewardChing
        public const string RaidClaimWaitCue = "UI Raid Claim Reward Wait";       // clip: UI RaidClaimRewardWait
    }
}
