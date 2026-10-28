// GameStaticData.cs — 原版 `GameStaticData` 的**静态设置面**（本批落账 234 / 237 / 460 三条）
//
// ============================ 判据（一手，逐行读过 2026-10-28） ============================
//  · `d:/2/tools/decomp_full/GameStaticData__CheckIfNewCards.c` —— 全文只有一句
//      `return newCards.Contains(uniqueId);`（`param_2 != 0` 才走，null ⇒ NRE 帮助函数）。
//  · `d:/2/tools/decomp_full/GameStaticData__FoundNewCard.c` —— 全文：
//      `h = LocalizationManager.GetTranslation("MenuDeck/HUD/NewCard").ToLower();`
//      `if (!h.Contains(text)) return false;` → `return newCards.Contains(uniqueId);`
//      （键字面量 `DAT_1842d01e8` = `"MenuDeck/HUD/NewCard"`；现读 `Resources/official_loc_13lang.json`
//        该键 **en=`New!` / zh=`新的！`**）。
//  · `d:/2/tools/decomp_full/PlayerDataManager__LoadSharedDefaultValues.c` —— 全文 13 次写入，
//    **次序** = 下面 `LoadSharedDefaultValues()` 的语句序（判据：`.c` 的写入顺序逐条对）。
//  · 字段名 / 类型 / 默认值 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/GameStaticData.cs:150-260`（声明）
//    + `:2342-2370`（`.cctor`）。
//  · 三个"默认值 getter"的体：
//      - `GameStaticData__get_DefaultQualitySetting.c` = **`return 4;`**（= `Ultra`）。
//        🔴 施工单 `B88_R10` §4.3 写「`GetDefaultQualitySetting()` 那一跳**方法体缺失**」
//        —— **不成立**，`.c` 全文就在那儿、只有一句 `return 4`（本文件按它落地）。
//      - `GameStaticData__get_DefaultCardTextsInHandOptions.c` = 读 `smallScreenUI(+0x11c)`
//        （= ILSpy `:486-494` 的 `smallScreenUI ? OnlyDescription : Default`）。
//      - `GameStaticData__get_DefaultTouchInput.c` = `SteamUtils.IsSteamRunningOnSteamDeck()`。
//
// ============================ 本工程已有的「单源」（⛔ 别在本类再存一份） ============================
//  原版这 13 格是**全局静态**，本工程对应的消费者各自已有**唯一一份**单源 ⇒ 本类**只持有原版
//  的默认值快照**（`LoadSharedDefaultValues` 写的就是这个），**⛔ 不当消费源**：
//    · 三个音量 → `Core/WarpforgeAudio.cs` 的 `Music` / `SoundFx` / `VoiceOver`
//      （`PlayerPrefs["MusicVolume"]` 落盘；出厂 1f，与原版默认一致）。
//    · `smallScreenUI` → **唯一来源 = `CardPresentation.SmallScreenUI.Enabled`**（见下 `SmallScreenUI` 属性）。
//    · `useCombatAutoZoom` → `Shell/SettingsWindow.cs` 的 `AutoZoom.Enabled`（`PlayerPrefs["AutoZoom"]`）。
//    · `touchInput` → `Shell/SettingsWindow.cs` 的 `GeneralFlags.TouchInput`（`PlayerPrefs["TouchInput"]`）。
//    · `notificationsOn` → 原版 `GeneralTab.ToggleDisableNotifications` 把**开关值原样**写进 `+0xE8`
//      （名字与语义不符 —— 原版就是这样，⛔ 别「修好」）；本工程存同一格的是
//      `SettingsWindow.GeneralFlags.DisableNotifications`（`PlayerPrefs["DisableNotifications"]`）。
//    · `cardsTextInHandFormat` → `Core/CardView.cs` 的 `HandTextFormat`（写口 `SetHandTextFormat`；
//      `Shell/SettingsWindow.cs` 的 `TextInHand.Value` 是持久那一半 —— ⚠️ **那两半今天还没接上**，
//      已记进报告 §3「顺手发现」，⛔ 不是本类的缺口）。
//    · `graphicsQuality` → 本工程真画质走 Unity 的 `QualitySettings`（`SettingsWindow` 的 Graphics 页），
//      原版那个 `EverguildQualitySetting` 枚举我们没有消费者 ⇒ 本类只落默认值。
//
//  🔴 **为什么 `LoadSharedDefaultValues` 不覆盖上面那些"单源"格**：原版这个方法的调用序是
//    「先灌默认值 → 再由玩家存档覆盖」（`PlayerDataManager__LoadPlayerData.c` 里
//    `LoadSharedDefaultValues()` 在 `LoadStarterData` 之前）。我们那几格**本身就是"带默认值的存档"**
//    （各自的 ctor 缺省就是原版默认值）⇒ 在这里再写一遍只会**冲掉玩家自己的设置**。
//    唯一仍然"读穿"的是 `smallScreenUI`（下面那个属性），因为它是判据里点名要接的唯一来源。
//
// ============================ 两处如实标「本机拿不到」 ============================
//  ① `DefaultTouchInput` 原版 = `SteamUtils.IsSteamRunningOnSteamDeck()`。本工程**没有 Steamworks 绑定**
//     （全仓 `grep SteamUtils` = 0）⇒ 在 PC / 编辑器上这个值**就是 false**（SteamDeck 之外的真机也是 false）
//     ⇒ 本类取 `false`，**不是我们编的**，是那个 API 在我们平台上返回的值。
//  ② `graphicsQuality` 的**消费者**在原版是 `QualitySettingsManager`（那一整条链读 `EverguildQualitySetting`
//     → 改 URP 资产）；本工程走 Unity 自己的 `QualitySettings` 档位（档数由工程资产定），
//     **两套档位不是一一对应** ⇒ 本类只保留原版那一格的默认值，**不做映射**（不猜）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `GameStaticData` 的静态设置面（只落本批需要的三段：
    /// 新卡标记两条 + <see cref="LoadSharedDefaultValues"/> 那 13 格默认值）。全静态、零状态。</summary>
    public static class GameStaticData
    {
        // ============================================================ 原版的两个枚举（精确值）
        /// <summary>原版 `EverguildQualitySetting`（`d:/2/Warpforge_code/.../EverguildQualitySetting.cs`，五个值逐字照抄）。</summary>
        public enum EverguildQualitySetting { VeryLow = 0, Low = 1, Medium = 2, High = 3, Ultra = 4 }

        /// <summary>原版 `CardTextsInHandOptions`（`d:/2/Warpforge_code/.../CardTextsInHandOptions.cs`）。</summary>
        public enum CardTextsInHandOptions { Default = 0, OnlyDescription = 1, OnlyName = 2 }

        // ============================================================ 13 个静态量（`LoadSharedDefaultValues` 写的就是这 13 格）
        //
        // 声明序 / 名字 / 类型 = ILSpy `GameStaticData.cs:150-260`；默认值 = `PlayerDataManager__LoadSharedDefaultValues.c`。
        // ⚠️ **字面默认值放在字段初始化器上**（= 原版 `.cctor` 里那批值），这样「没调 `LoadSharedDefaultValues` 之前」
        //    也是对的（批处理/自检里 `Awake` 不跑 —— 同族坑见 `MainMenuRuntime` 那一节）。

        /// <summary>原版 `MusicVolume`（`+0xC0`）。默认 **1f**。⚠️ 消费者 = `WarpforgeAudio.Music`（见文件头）。</summary>
        public static float MusicVolume = 1f;

        /// <summary>原版 `SoundFxVolume`（`+0xD4`）。默认 **1f**。</summary>
        public static float SoundFxVolume = 1f;

        /// <summary>原版 `VoiceOversVolume`（`+0xD0`）。默认 **1f**。</summary>
        public static float VoiceOversVolume = 1f;

        /// <summary>原版 `_disableParticles`（`+0xD8`，配套属性 <see cref="DisableParticles"/>）。默认 **false**。</summary>
        public static bool _disableParticles;

        /// <summary>原版 `lastRegistrationShowTime`（`+0xE0`，`long`）。默认 **0**。</summary>
        public static long lastRegistrationShowTime;

        /// <summary>原版 `notificationsOn`（`+0xE8`）。默认 **false**。
        /// ⚠️ 原版 `GeneralTab.ToggleDisableNotifications` 把**开关值原样**写进这一格（名字与语义不符 —— 原版就是这样）。</summary>
        public static bool notificationsOn;

        /// <summary>原版 `hiFPS`（`+0x11B`）。默认 **true**（`.cctor` 与 `LoadSharedDefaultValues` 都写 1 —— 别按「假」猜）。</summary>
        public static bool hiFPS = true;

        /// <summary>原版 `useCombatAutoZoom`（`+0x125`）。默认 = `DefaultTouchInput`。
        /// ⚠️ 消费者 = `SettingsWindow.AutoZoom.Enabled`（见文件头）。</summary>
        public static bool useCombatAutoZoom;

        /// <summary>原版 `touchInput`（`+0x11D`）。默认 = `Application.isMobilePlatform`（PC 上 = false）。
        /// ⚠️ 消费者 = `SettingsWindow.GeneralFlags.TouchInput`。</summary>
        public static bool touchInput;

        /// <summary>原版 `graphicsQuality`（`+0x120`）。默认 = `GetDefaultQualitySetting()` = **Ultra(4)**。</summary>
        public static EverguildQualitySetting graphicsQuality = EverguildQualitySetting.Ultra;

        /// <summary>原版 `cardsTextInHandFormat`（`+0x140`）。默认 = `DefaultCardTextsInHandOptions`
        /// （= `smallScreenUI ? OnlyDescription : Default`；出厂 smallScreenUI=false ⇒ **Default**）。
        /// ⚠️ 消费者 = `CardView.HandTextFormat`。</summary>
        public static CardTextsInHandOptions cardsTextInHandFormat;

        /// <summary>原版 `lastTimeAskedForNotifications`（`+0x138`，`long`）。默认 **0**。</summary>
        public static long lastTimeAskedForNotifications;

        /// <summary>原版 `disableParticles` 属性（`get/set` 都只是 `_disableParticles` 的透传，ILSpy `:472-483`）。</summary>
        public static bool DisableParticles
        {
            get { return _disableParticles; }
            set { _disableParticles = value; }
        }

        /// <summary>= 原版 `GameStaticData.smallScreenUI`（`+0x11C`）。
        /// 🔴 **唯一来源 = `CardPresentation.SmallScreenUI.Enabled`**（那里面写清了它的持久化与判据链）——
        /// ⛔ 本类不另存一份（两份迟早不一致）。</summary>
        public static bool SmallScreenUI { get { return global::CardPresentation.SmallScreenUI.Enabled; } }

        // ============================================================ 三个"默认值 getter"（原版静态属性）
        /// <summary>= 原版 `GameStaticData.DefaultTouchInput`：`SteamUtils.IsSteamRunningOnSteamDeck()`。
        /// ⚠️ 本工程没有 Steamworks 绑定（见文件头 ①）⇒ 在 PC / 编辑器上恒 **false**（那正是该 API 在本平台的值）。</summary>
        public static bool DefaultTouchInput { get { return false; } }

        /// <summary>= 原版 `GameStaticData.DefaultQualitySetting`：`=> EverguildQualitySetting.Ultra`（`.c` 里 `return 4`）。</summary>
        public static EverguildQualitySetting DefaultQualitySetting { get { return EverguildQualitySetting.Ultra; } }

        /// <summary>= 原版 `GameStaticData.DefaultCardTextsInHandOptions`：
        /// `!smallScreenUI ⇒ Default`、否则 `OnlyDescription`（`.c` 读 `+0x11c`；ILSpy `:486-494` 同）。</summary>
        public static CardTextsInHandOptions DefaultCardTextsInHandOptions
        {
            get { return SmallScreenUI ? CardTextsInHandOptions.OnlyDescription : CardTextsInHandOptions.Default; }
        }

        // ============================================================ 460 LoadSharedDefaultValues
        /// <summary>
        /// 原版 `PlayerDataManager.LoadSharedDefaultValues()`（`decomp_full/PlayerDataManager__LoadSharedDefaultValues.c` 全文）。
        /// **13 次写入、次序照 `.c`**：3 个音量 → `_disableParticles` → `lastRegistrationShowTime` →
        /// `notificationsOn` → `hiFPS` → `smallScreenUI` → `useCombatAutoZoom` → `touchInput` →
        /// `graphicsQuality` → `cardsTextInHandFormat` → `lastTimeAskedForNotifications`。
        ///
        /// <para>🔴 **`smallScreenUI` 这一格不写**：它在本工程的唯一来源是 `SmallScreenUI.Enabled`
        /// （`PlayerPrefs` 落盘、出厂 false = 原版 `DefaultTouchInput` 在 PC 上的值）。在这里写它
        /// 会**把玩家手动选过的值冲掉**，而原版的语义是「先灌默认、后由玩家存档覆盖」——
        /// 我们的 `SmallScreenUI` 本身就是"带默认值的存档" ⇒ 等价，⛔ 不是漏做（见文件头那段）。</para>
        ///
        /// <para>⚠️ 其余几格（音量 / `useCombatAutoZoom` / `touchInput` / `notificationsOn` /
        /// `cardsTextInHandFormat`）**照抄原版写本类自己的字段**，但它们各自的消费者读的是自己的单源
        /// （见文件头那张表）—— 本类这些格是原版静态量的落地与默认值快照，⛔ 别当消费源。</para>
        /// </summary>
        public static void LoadSharedDefaultValues()
        {
            MusicVolume = 1f;                                     // +0xC0
            SoundFxVolume = 1f;                                   // +0xD4
            VoiceOversVolume = 1f;                                // +0xD0
            _disableParticles = false;                            // +0xD8
            lastRegistrationShowTime = 0L;                        // +0xE0
            notificationsOn = false;                              // +0xE8
            hiFPS = true;                                         // +0x11B
            // +0x11C smallScreenUI —— 见方法注释：唯一来源是 SmallScreenUI.Enabled，这里**不写**。
            useCombatAutoZoom = DefaultTouchInput;                // +0x125
            touchInput = Application.isMobilePlatform;            // +0x11D
            graphicsQuality = DefaultQualitySetting;              // +0x120
            cardsTextInHandFormat = DefaultCardTextsInHandOptions;// +0x140
            lastTimeAskedForNotifications = 0L;                   // +0x138
        }

        // ============================================================ 234 CheckIfNewCards
        /// <summary>原版 `CheckIfNewCards(string uniqueId, List&lt;string&gt; newCards)`（`GameStaticData__CheckIfNewCards.c` 全文）：
        /// <c>return newCards.Contains(uniqueId);</c>
        /// <para>数据源 = `PlayerDataKey.NewCards`（第 34 项；本工程 `Shell/SaveKeys.cs:162` 同名同号）。</para>
        /// <para>⚠️ 原版列表为 null 时走 NRE 帮助函数（`FUN_1803f47a0`）；本工程**不抛**（红线：不崩宿主）
        /// ⇒ 返回 <c>false</c>。</para></summary>
        public static bool CheckIfNewCards(string uniqueId, List<string> newCards)
        {
            if (newCards == null) return false;       // 原版：NRE（见 summary）
            return newCards.Contains(uniqueId);
        }

        // ============================================================ 237 FoundNewCard
        /// <summary>原版 `FoundNewCard(string uniqueId, string text, List&lt;string&gt; newCards)`
        /// （`GameStaticData__FoundNewCard.c` 全文）：
        /// <code>
        /// var h = Loc("MenuDeck/HUD/NewCard").ToLower();   // 只 lower 头部，**不 lower text**
        /// if (!h.Contains(text)) return false;
        /// return newCards.Contains(uniqueId);
        /// </code>
        /// <para>⚠️ <paramref name="text"/> **必须是已经 ToLower 过的那一档**（原版就只 lower `h` 一边）。
        /// 现读官方表：`MenuDeck/HUD/NewCard` = en `New!` / zh `新的！` ⇒ 调用方传的应是 `"new!"` / `"新的！"`。</para>
        /// <para>⚠️ <paramref name="newCards"/> 为 null ⇒ 原版 NRE；本工程返回 <c>false</c>（同 `CheckIfNewCards`）。</para></summary>
        public static bool FoundNewCard(string uniqueId, string text, List<string> newCards)
        {
            // 原版 `System_String__ToLower(lVar2, 0)` = 当前文化的 ToLower()。⚠️ 用 `ToLowerInvariant` 会改变
            // 土耳其语等文化下的行为 ⇒ 照原版用 `ToLower()`。
            if (text == null) return false;            // 原版 `Contains(h, null)` 会抛（ArgumentNull）；本工程不抛
            string h = Loc.T("MenuDeck/HUD/NewCard").ToLower();
            if (!h.Contains(text)) return false;       // 原版 `System_String__Contains(h, text)`（序数比较）
            if (newCards == null) return false;        // 原版：NRE（见 summary）
            return newCards.Contains(uniqueId);
        }

        // ============================================================ 486 `dontUseAltPortSetting`（B89-E10c 追加在类尾）
        // 判据 = `decomp_full/PlayerDataManager__SetAltPorts.c`（全文现读 2026-10-28）第 ① 句：
        //   `*(bool*)(GameStaticData+0xEF) = (旧值 == 0);`  ← **取反**，不是赋值。
        // 原版这一格就在 `GameStaticData` 上（`dump.cs` 的字段 `dontUseAltPortSetting`），
        // 🆕 **2026-10-28（B89-E10c）从 `Net/NetConfig.cs` 搬回本类**（上一波本仓还没有这个类，才暂寄在那儿）。
        // 🔴 **唯一读写点 = `NetConfig.SetAltPorts`**（那一句 `= !本身`）—— 全仓没有第二个读写处，
        //    ⇒ 搬迁成本为零、也不存在「两处写同一格」。
        // ⚠️ 名字与语义**不符是原版就这样的**（`dontUseAltPortSetting` 每次被 `SetAltPorts` 翻一次），⛔ 别「修好」。

        /// <summary>原版 `GameStaticData.dontUseAltPortSetting`（`GameStaticData+0xEF`）。
        /// <para>⚠️ 它不是「altPorts 当前值」，而是一个**每调一次 `SetAltPorts` 就取反一次**的全局开关
        /// （判据见上）。出厂 `false`（= 原版 `.cctor` 不写它、默认零值）。</para>
        /// <para>⚠️ 「为什么要先翻全局再写设置值」的语义**没解出来**（机器码直读；施工单 `B88_R10` §六·4
        /// 也挂成「照抄即可、语义存疑」）—— 本类只提供**存储**，切换逻辑在 `NetConfig.SetAltPorts`（照抄）。</para></summary>
        public static bool DontUseAltPortSetting { get; set; }
    }
}
