// LocalPlayerData.cs — 原版 `LocalPlayerData`（本地玩家设置总档）＋ 它的三条读写链
// = `PlayerDataManager.SavePlayerData` / `LoadPlayerData` / `ReadDataFromFile` 的等价物
// （账 `其他_基础设施` 427 / 461 / 481 三条的落点）。
//
// ============================ 判据（一手） ============================
//  · 字段名 / 字段序 / 类型 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/LocalPlayerData.cs`（全文 85 行，
//    **40 个 public 字段**，本文件逐字照抄、**名字与序都不改** —— 这是落盘格式）。
//  · `SavePlayerData` 体 = `cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:10170-10266`
//    （= `decomp_full/PlayerDataManager__SavePlayerData.c` 153 行）。**逐字段的赋值源**见下面 `Collect()`。
//  · `LoadPlayerData` 体 = 同文件 `:10268-10403`（`decomp_full/PlayerDataManager__LoadPlayerData.c` 243 行）。
//  · `ReadDataFromFile` 体 = `decomp_full/PlayerDataManager__ReadDataFromFile.c` 82 行
//    （`BinaryFormatter.Deserialize(persistentDataPath + "/PlayerDecks" + uid + ".dat")` ＋ 版本比较）。
//  · 末段「写完立刻重新应用」= `SavePlayerData` 尾：`SetMusicAndFxVolumes()` + `SetTargetFramerate(...)`；
//    `FPSLimit` 的映射见 `TargetFramerateFor`。
//
// ============================ 与原版的【形态】差异（如实记，⛔ 不是漏做） ============================
//  ① **介质**：原版 `BinaryFormatter` 写 `persistentDataPath/PlayerDecks<uid>.dat` 一份二进制档。
//     我们走 `SaveCenter`（每 key 一份 JSON，原子写 tmp→rename）。⇒ **一份总档的形状照旧**（同一个 key、
//     一起写、一起读），只是编码是 JSON。🔶 自己设计（非原版）：**编码与文件名** — 原版判据是二进制
//     `BinaryFormatter`，本仓没有 BinaryFormatter 的落地先例且它是**不可移植的**（Unity 6 已把它标成不安全 API）
//     · 用户 2026-10-28 授权「有需要决策的地方由你复刻原版，除非和服务器有关」⇒ 介质这一层属于**存档格式**，
//     本仓既有唯一介质就是 `SaveCenter`（`SaveKeys` 51 条全走它）⇒ 不新开第二种介质。谁定的：调度台 2026-10-29（本文件）。
//  ② **加密**：原版 `loginPassword` / `deviceId` 过 `CryptoString.EncryptRijndael(..., dataVersion)`。
//     本仓**没有** `CryptoString`，也**没有登录链**（`loginName/loginPassword/deviceId/playFabId` 全恒 null，
//     没有任何写入点）⇒ 那两跳**无可加密的对象**。本文件**不实现 Rijndael**，且**若它们非空则拒绝写盘并出声**
//     （⛔ 不把明文密码写进档里冒充原版）。
//  ③ **`deviceId` 恒空**（同 ②）：原版 `LoadPlayerData` 末句 `if (string.IsNullOrEmpty(deviceId)) SavePlayerData();`
//     = 「首次运行把它补上」。我们**照抄这一句**（判据形状不变），后果是「每次读档都会再写一次」——
//     **如实记在这里**，⛔ 别悄悄加一条「我们产不出 deviceId 就跳过」的自造闸。
//  ④ **单源不变**：每一格在本仓**仍然只有一个真源**（`PlayerPrefs` 那几族）—— 本类只是把它们的值**汇总成一份总档**，
//     读档时按原版的 `*ChosenManually` 闸灌回去。⛔ 别让本类成为第二真源（CLAUDE.md §三）。
//
// ============================ 「字段 → 消费点」映射表（原版 `LocalPlayerData` 40 格） ============================
//  格                              原版写源（GameStaticData.*）        本仓的唯一消费点（Collect 读 / Apply 写）
//  ------------------------------  ----------------------------------  --------------------------------------------
//  musicVolume   (int 0..100)      MusicVolume      ×100                Core/WarpforgeAudio.cs `Music` / `SetMusic`
//  voiceOversVolume (int 0..100)   VoiceOversVolume ×100                Core/WarpforgeAudio.cs `VoiceOver` / `SetVoiceOver`
//  fxVolume      (int 0..100)      SoundFxVolume    ×100                Core/WarpforgeAudio.cs `SoundFx` / `SetSoundFx`
//  languageVersion (int)           languageVersion                      Core/Loc.cs `Current` / `SetLanguage`
//  notificationsOn (bool)          notificationsOn  ⚠️名字与语义不符      SettingsWindow.GeneralFlags.DisableNotifications
//  noBotsModeOn  (bool)            noBotsModeOn                         SettingsWindow.GeneralFlags.DisableBots
//  touchInput    (bool)            touchInput                           SettingsWindow.GeneralFlags.TouchInput
//  useCombatAutoZoom (bool)        useCombatAutoZoom                    CardPresentation.AutoZoom.Enabled / Set
//  smallScreenUI (bool)            smallScreenUI                        SmallScreenUI.Enabled / Set
//  graphicsQuality (enum 0..4)     graphicsQuality                      GameStaticData.graphicsQuality / GraphicQuality.Save
//  cardsTextInHandFormat (enum)    cardsTextInHandFormat                Core/CardView.cs `HandTextFormat` / `SetHandTextFormat`
//  disableParticles (bool)         _disableParticles                    GameStaticData._disableParticles（今天**无消费者**）
//  hiFPS / vsync / fpsLimit        hiFPS / vsync / FPSLimit             GameStaticData 三个静态量（今天**无消费者**）
//  useSuperSampling (bool)         useSuperSampling                     CardPresentation.SuperSampling.Enabled / Set
//  graphicsExtendedCompatibility   graphicsExtendedCompatibility        GameStaticData（今天**无消费者**）
//  dataVersion (int)               dataVersion（本类自己的版本号）        本类 `CurrentDataVersion`
//  lastBuildSaved (string)         Application.version                  `Application.version`
//  *ChosenManually (5 个 bool)     GameStaticData 同名格                 本仓**等价物 = `PlayerPrefs` 里有没有那个键**
//                                    （`graphicsQualityChosenManually` 的原注释已经写明这条等价，见
//                                     `Shell/SettingsWindow.cs` 的 `GraphicQuality` 类注释）
//  loginName / loginPassword /     GameStaticData 同名格 / DeviceId      **无**（本仓没有登录链 ⇒ P3；字段照样在档里，
//    deviceId / playFabId                                               值恒 null，见差异 ②③）
//  lastRegistrationShowTime /      GameStaticData 同名格                 **无**（没有注册弹窗 / 通知请求链）
//    lastTimeAskedForNotifications
//  crateNotificationsOn / chatInvisibleModeOn / friendlyBattle /        **无**（同上一格：本仓没有对应设置窗）
//    dontUseAltPortSetting / noTipsModeOn / noFfullScreen /
//    useMobileBattleScreenSize / unrankedMultiplayer / unrankedFastMode /
//    loginWithGoogle / *ChosenManually 之外的两个 bool
//  ⇒ 「无」的那几格**照样在 DTO 里、照样原样读回原样写回**（原版就是全量序列化），
//     ⛔ 别给它们编默认值去冒充「玩家改过」（同 `PracticeModePopup.DemoDataDto` 那条口径）。
//
// 🔴 **原版自己的一处不对称（照抄，⛔ 别「修好」）**：`SavePlayerData` **不写** `playFabId` 与
//    `useMobileBattleScreenSize` 两格，而 `LoadPlayerData` **会读** `useMobileBattleScreenSize`
//    ⇒ 原版这两个字段永远是「档里没有 ⇒ 默认值」。本文件 `Collect()` **照抄这个缺席**（见那两行注释）。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `LocalPlayerData`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/LocalPlayerData.cs` 逐字照抄）。
    /// <para>🔴 **这是落盘格式**：字段名 / 字段序 / 类型与顺序都照原版，⛔ 别改名、别增删（会与将来的旧档对不上）。</para>
    /// <para>⚠️ 40 格里**今天有消费者的只有 12 格**（见文件头映射表）；其余照原版全量序列化 ——
    /// 它们是「档里的格子」，不是「我们没用上的代码」。</para></summary>
    [Serializable]
    public class LocalPlayerData
    {
        public string loginName;
        public string loginPassword;
        public string deviceId;
        public string playFabId;

        public int languageVersion;
        public long lastRegistrationShowTime;

        public bool disableParticles;
        public bool notificationsOn;
        public bool crateNotificationsOn;
        public bool chatInvisibleModeOn;
        public bool friendlyBattle;
        public bool loginWithGoogle;
        public bool dontUseAltPortSetting;
        public bool noBotsModeOn;
        public bool noTipsModeOn;
        public bool noFfullScreen;      // 原版拼写就是 `noFfullScreen`（三个 f）—— 落盘格式，⛔ 别改

        public int musicVolume;         // 0..100（= `GameStaticData.MusicVolume` × 100）
        public int voiceOversVolume;
        public int fxVolume;

        public int dataVersion;         // 本类自己写；原版 `PlayerDataManager.dataVersion`

        public bool useMobileBattleScreenSize;
        public string lastBuildSaved;   // = `Application.version`

        public bool hiFPS;
        public bool vsync;
        public int fpsLimit;            // 0 = 30fps · 1 = 60fps · 2 = 不限帧（换算见 `TargetFramerateFor`）

        public bool smallScreenUI;
        public GameStaticData.EverguildQualitySetting graphicsQuality;
        public bool useSuperSampling;
        public bool useCombatAutoZoom;
        public bool graphicsExtendedCompatibility;
        public GameStaticData.CardTextsInHandOptions cardsTextInHandFormat;

        public bool cardsTextInHandFormatChosenManually;
        public bool graphicsQualityChosenManually;
        public bool touchInputChosenManually;
        public bool smallUIChosenManually;
        public bool autoCombatChosenManually;

        public bool touchInput;
        public bool unrankedMultiplayer;
        public bool unrankedFastMode;
        public long lastTimeAskedForNotifications;
    }

    /// <summary>= 原版 `PlayerDataManager` 的「本地玩家设置总档」那三条链：
    /// <see cref="SavePlayerData"/> / <see cref="LoadPlayerData"/> / <see cref="ReadDataFromFile"/>。
    /// <para>⚠️ 名字照原版（双侧方法级可对）。判据与「字段 → 消费点」表见文件头。</para></summary>
    public static class LocalPlayerDataStore
    {
        /// <summary>总档在 `SaveCenter` 里的 key。
        /// <para>⚠️ 原版**不是** `PlayerDataKey` 里的某一条 —— 它是**单独一份文件** `PlayerDecks&lt;uid&gt;.dat`
        /// （`SavePlayerData` 的 `persistentDataPath + "/PlayerDecks" + uid + ".dat"`）⇒ 本 key 是我们这一侧的文件名，
        /// 与 `Shell/SaveKeys.cs` 那 51 条（= 原版 `PlayerDataKey` 逐条）**不是同一族**，⛔ 别把它并进去；**但必须带 `_ours_` 前缀**（`SaveKeys.OursPrefix`），否则 `SaveCenter.ValidateKey → SaveKeys.IsKnown` 只放行「51 条 ∪ `_ours_` 前缀」⇒ Set/Get/Delete 全部被拒、整条链静默失效。</para></summary>
        public const string SaveKey = SaveKeys.OursPrefix + "LocalPlayerData";   // = "_ours_LocalPlayerData" · 2026-10-29（B94_FX4 · DG2 R2）：原裸串 `"LocalPlayerData"` 过不了键表闸，生产里从来没落盘过

        /// <summary>= 原版 `PlayerDataManager.dataVersion` 的当前值。原版拿它做两件事：写进档里、给 `CryptoString` 当解密参数。
        /// 我们没有加密（见文件头差异②）⇒ 它今天只用于 <see cref="ReadDataFromFile"/> 的**版本闸**。</summary>
        public const int CurrentDataVersion = 1;

        /// <summary>🔴 **自检注入点**：true ⇒ <see cref="Apply"/> **只灌 `GameStaticData` 镜像**、
        /// 不写任何 `PlayerPrefs` / 不动 mixer（本工程规矩：自检不许动玩家的真设置 —— 同
        /// `GraphicQuality.PersistOverride` / `SmallScreenUI.PersistOverride` 那一族）。</summary>
        public static bool PersistOverride;

        /// <summary>上一次失败的人话（成功时是空串）。⛔ 别在调用点自己再判一遍。</summary>
        public static string LastError { get; private set; } = "";

        /// <summary>最近一次 `ReadDataFromFile` 的形状（给自检/调用点判「第一次跑」还是「档丢了」）。</summary>
        public enum ReadOutcome { Ok, Missing, BadVersion, Unserializable }

        /// <summary>最近一次读档的结果档。</summary>
        public static ReadOutcome LastReadOutcome { get; private set; } = ReadOutcome.Missing;

        /// <summary>上一次读到 / 写出内存的那一份 —— 用来**保住那些没有消费者的格子**（再写时原样带回去）。</summary>
        static LocalPlayerData _last;

        static bool _booted;

        // ================================================================== ① 汇总（= SavePlayerData 的赋值段）
        /// <summary>把各单源的值收成一份总档（= `SavePlayerData` 里那串
        /// `localPlayerData.X = GameStaticData.X` 的等价物）。
        /// <para>🔴 **没有消费者的格子从 <see cref="_last"/> 原样继承**（读到什么就写回什么），
        /// ⛔ 不编默认值 —— 与 `PracticeModePopup.DemoDataDto` 那条口径一致。</para>
        /// <para>⚠️ 原版**不写** `playFabId` 与 `useMobileBattleScreenSize` ⇒ 这里**也不写**（见文件头那条不对称）。</para></summary>
        public static LocalPlayerData Collect()
        {
            var d = _last != null ? CopyOf(_last) : new LocalPlayerData();

            // ---- 三个音量：原版 `GameStaticData.XVolume * 100f` → int（`Convert.ToInt32` 那一跳是**四舍五入**）
            d.musicVolume = Mathf.RoundToInt(WarpforgeAudio.Music * 100f);
            d.voiceOversVolume = Mathf.RoundToInt(WarpforgeAudio.VoiceOver * 100f);
            d.fxVolume = Mathf.RoundToInt(WarpforgeAudio.SoundFx * 100f);

            // ---- 语言
            d.languageVersion = (int)Loc.Current;

            // ---- 关/开那几格
            d.disableParticles = GameStaticData._disableParticles;
            // ⚠️ 原版 `GeneralTab.ToggleDisableNotifications` 把**开关值原样**写进 `notificationsOn`
            //    （名字与语义不符 —— 原版就是这样，⛔ 别「修好」）。本仓存同一格的是 `DisableNotifications`。
            d.notificationsOn = SettingsWindow.GeneralFlags.DisableNotifications;
            d.noBotsModeOn = SettingsWindow.GeneralFlags.DisableBots;

            // ---- 画质 / 屏幕 / 自动缩放 / 手牌文字
            d.graphicsQuality = GameStaticData.graphicsQuality;
            d.smallScreenUI = SmallScreenUI.Enabled;
            d.useCombatAutoZoom = CardPresentation.AutoZoom.Enabled;
            d.useSuperSampling = CardPresentation.SuperSampling.Enabled;
            d.cardsTextInHandFormat = (GameStaticData.CardTextsInHandOptions)Mathf.Clamp(CardView.HandTextFormat, 0, 2);

            // ---- `*ChosenManually` 五个：本仓的等价物 = 「`PlayerPrefs` 里有没有那个键」
            //      （原注释已定：`Shell/SettingsWindow.cs` 的 `GraphicQuality` 类注释 —— 原版这一位唯一的消费者
            //        就是 `LoadPlayerData`，我们这一层就是它的等价物）。
            d.graphicsQualityChosenManually = PlayerPrefs.HasKey(GraphicQuality.PrefKey);
            d.touchInputChosenManually = PlayerPrefs.HasKey(SettingsWindow.GeneralFlags.TouchPrefKey);
            d.smallUIChosenManually = PlayerPrefs.HasKey(SmallScreenUI.PrefKey);
            d.autoCombatChosenManually = PlayerPrefs.HasKey(CardPresentation.AutoZoom.PrefKey);
            // ⚠️ 手牌文字那一格**本仓没有持久键**（`CardView.SetHandTextFormat` 只改内存；
            //    `GameStaticData` 文件头已记「那两半今天还没接上」）⇒ 没有「手动选过」的证据 ⇒ 恒 false（不编）。
            d.cardsTextInHandFormatChosenManually = _last != null && _last.cardsTextInHandFormatChosenManually;

            // ---- 只有 GameStaticData 有、没有别的消费者的四格（照抄原版写源，读档时也照抄灌回去）
            d.hiFPS = GameStaticData.hiFPS;
            d.graphicsExtendedCompatibility = GameStaticData.graphicsExtendedCompatibility;
            d.vsync = GameStaticData.vsync;
            d.fpsLimit = GameStaticData.FPSLimit;
            d.touchInput = SettingsWindow.GeneralFlags.TouchInput;

            // ---- 本类自己的两格
            d.dataVersion = CurrentDataVersion;
            d.lastBuildSaved = Application.version;

            // ⛔ 不写 `playFabId` / `useMobileBattleScreenSize` —— 见文件头「原版自己的一处不对称」。
            return d;
        }

        // ================================================================== ② 灌回（= LoadPlayerData 的赋值段）
        /// <summary>把一份总档按**原版的分支**灌回各单源（= `LoadPlayerData` 体）。
        /// <para>🔴 五处 `*ChosenManually` 闸**照抄**：`touchInput` / `graphicsQuality` / `smallScreenUI` /
        /// `useCombatAutoZoom` / `cardsTextInHandFormat` **只有**「手动选过」才用档里的值，否则走原版的 `Default*` 支。
        /// 本仓的 `Default*` 支 = **不动**（我们的单源本身出厂就是原版默认值，见 `GameStaticData` 文件头那段）。</para>
        /// <para>⚠️ `PersistOverride` 为真时只灌 `GameStaticData` 镜像、不写 `PlayerPrefs` / 不动 mixer。</para></summary>
        public static void Apply(LocalPlayerData d)
        {
            LastError = "";
            if (d == null) { LastError = "Apply(null)"; return; }
            _last = CopyOf(d);

            // ---- ① 三个音量（原版无 `chosenManually` 闸，一律灌；档里是 0..100，除 100 回去）
            float m = Mathf.Clamp01(d.musicVolume / 100f);
            float fx = Mathf.Clamp01(d.fxVolume / 100f);
            float vo = Mathf.Clamp01(d.voiceOversVolume / 100f);
            GameStaticData.MusicVolume = m;          // 镜像（原版灌的是 `MusicVolume__BackingField`）
            GameStaticData.SoundFxVolume = fx;
            GameStaticData.VoiceOversVolume = vo;
            if (!PersistOverride)
            {
                WarpforgeAudio.SetMusic(m);
                WarpforgeAudio.SetSoundFx(fx);
                WarpforgeAudio.SetVoiceOver(vo);
            }

            // ---- ② 语言（原版只灌静态量；本仓语言只有 `Loc.Current` 一份单源 ⇒ 灌它）
            GameStaticData.languageVersion = d.languageVersion;
            if (!PersistOverride)
            {
                // ⚠️ `AvailableLanguages` 是**稀疏枚举**（0/10/20/…/110）⇒ 不能用 `Clamp` 兜，
                //    要判「这个值是不是合法成员」（`Enum.IsDefined`）；不合法就**出声 + 不动**（⛔ 不静默落到 English）。
                var lang = (AvailableLanguages)d.languageVersion;
                if (System.Enum.IsDefined(typeof(AvailableLanguages), lang))
                    Loc.SetLanguage(lang);
                else
                    Debug.LogWarning("[LocalPlayerData] 档里的 `languageVersion` = " + d.languageVersion
                                   + " 不是 `AvailableLanguages` 的合法值 ⇒ 保持当前语言（如实出声）");
            }

            // ---- ③ 无消费者的几格：只灌 `GameStaticData` 镜像（原版就是灌它）
            GameStaticData._disableParticles = d.disableParticles;
            GameStaticData.hiFPS = d.hiFPS;
            GameStaticData.vsync = d.vsync;
            GameStaticData.FPSLimit = d.fpsLimit;
            GameStaticData.graphicsExtendedCompatibility = d.graphicsExtendedCompatibility;
            GameStaticData.useSuperSampling = d.useSuperSampling;
            GameStaticData.lastRegistrationShowTime = d.lastRegistrationShowTime;
            GameStaticData.lastTimeAskedForNotifications = d.lastTimeAskedForNotifications;

            // ---- ④ 有消费者的几格（`*ChosenManually` 闸照抄）
            if (!PersistOverride)
            {
                // 原版 `GeneralTab` 那个开关**名字与语义不符**（见文件头）；我们照它的字面落点。
                SettingsWindow.GeneralFlags.DisableNotifications = d.notificationsOn;
                SettingsWindow.GeneralFlags.DisableBots = d.noBotsModeOn;

                if (d.touchInputChosenManually) SettingsWindow.GeneralFlags.TouchInput = d.touchInput;
                // else：原版 `touchInput = DefaultTouchInput`（本仓那一位就是 `GeneralFlags.TouchInput` 的出厂 0）⇒ 不动。
            }
            if (d.graphicsQualityChosenManually)
            {
                int q = Mathf.Clamp((int)d.graphicsQuality, 0, 4);
                GameStaticData.graphicsQuality = (GameStaticData.EverguildQualitySetting)q;
                if (!PersistOverride) GraphicQuality.Save((GameStaticData.EverguildQualitySetting)q);
            }
            // else：原版 `graphicsQuality = GetDefaultQualitySetting()`（= Ultra）；本仓 `GraphicQuality.Load`
            //       在「Prefs 里没那个键」时同样停在 `GameStaticData` 的出厂值 Ultra ⇒ 等价、不动。

            // ⚠️ `GameStaticData.smallScreenUI` 在本仓是**只读属性**（唯一来源 = `SmallScreenUI.Enabled`，
            //    见 `GameStaticData` 那一格）⇒ 这里只灌源，⛔ 不去写那个属性。
            if (d.smallUIChosenManually && !PersistOverride) SmallScreenUI.Set(d.smallScreenUI);
            if (d.autoCombatChosenManually && !PersistOverride) CardPresentation.AutoZoom.Set(d.useCombatAutoZoom);
            if (d.cardsTextInHandFormatChosenManually)
                CardView.SetHandTextFormat(Mathf.Clamp((int)d.cardsTextInHandFormat, 0, 2));

            // ---- ⑤ 原版末句：`deviceId` 为空 ⇒ 当场再存一次（「首次运行把它补上」）。
            //      ⚠️ 本仓 `deviceId` **恒空**（没有登录链）⇒ 这一支每次都命中、每次读档都会再写一次 ——
            //      这是文件头「差异③」点名的原版形状，⛔ 别加自造闸把它跳掉。
            if (string.IsNullOrEmpty(d.deviceId)) SavePlayerData();
        }

        // ================================================================== ③ 写（= SavePlayerData）
        /// <summary>= 原版 `PlayerDataManager.SavePlayerData()`：
        /// 汇总 → 写**一份总档** → 立刻把设置重新应用一遍（原版 `SetMusicAndFxVolumes()` + `SetTargetFramerate(...)`）。
        /// <para>🔴 与 `GameStaticData` 文件头那条「我们的 setter 是应用 + 落盘一体」的关系：本仓
        /// `WarpforgeAudio.SetMusic` 之类**本来就是原子地改 mixer + 写 Prefs** ⇒ 原版「先攒在对象里、落盘后统一应用」
        /// 那个**时序**在我们这里不存在；这里照样调一次，保证「改档即生效」这一点与原版一致。</para></summary>
        public static bool SavePlayerData()
        {
            LastError = "";
            var d = Collect();

            // 原版这两格是**加密后**落盘的；我们没有 `CryptoString`、也没有登录链
            // ⇒ 有值就拒绝写（⛔ 不把明文密码/设备号写进档）
            if (!string.IsNullOrEmpty(d.loginPassword) || !string.IsNullOrEmpty(d.deviceId))
            {
                LastError = "`loginPassword` / `deviceId` 非空，而本仓没有 `CryptoString.EncryptRijndael` "
                          + "（原版这两格是加密落盘的）⇒ 拒绝写档（⛔ 不写明文）";
                Debug.LogWarning("[LocalPlayerData] " + LastError);
                return false;
            }

            bool ok = SaveCenter.Set(SaveKey, d);
            if (!ok)
            {
                LastError = "没写进存档：" + (SaveCenter.LastWriteError ?? "（`SaveCenter` 没给出原因）");
                Debug.LogWarning("[LocalPlayerData] " + LastError);
                return false;
            }
            _last = d;

            // ---- 原版尾巴：写完立刻重新应用
            if (!PersistOverride)
            {
                WarpforgeAudio.SetMusic(Mathf.Clamp01(d.musicVolume / 100f));
                WarpforgeAudio.SetSoundFx(Mathf.Clamp01(d.fxVolume / 100f));
                WarpforgeAudio.SetVoiceOver(Mathf.Clamp01(d.voiceOversVolume / 100f));
                Application.targetFrameRate = TargetFramerateFor(d.fpsLimit);
            }
            return true;
        }

        /// <summary>= 原版 `SavePlayerData` 末段那三行 `FPSLimit` 换算（`cpp2il` `:10252-10264` 逐句）：
        /// <code>
        /// flag = FPSLimit == 0;
        /// if (!flag) target = (FPSLimit - 1 == 1) ? -1 : 60;   // 2 ⇒ 不限帧；其余 ⇒ 60
        /// else       target = 30;
        /// </code>
        /// ⚠️ 本仓**没有** FPS 设置窗 ⇒ `fpsLimit` 今天恒是 `.cctor` 的出厂值 **2** = 不限帧
        /// ⇒ `Application.targetFrameRate = -1`（与原版「没改过设置时」的取值一致，不是我们改的口径）。
        /// 🔴 **这条会影响真跑**：原版出厂也是 2，所以两边一致；若将来加 FPS 设置窗，改这一格就够。</summary>
        public static int TargetFramerateFor(int fpsLimit)
        {
            if (fpsLimit == 0) return 30;
            if (fpsLimit == 2) return -1;
            return 60;
        }

        // ================================================================== ④ 读（= ReadDataFromFile / LoadPlayerData）
        /// <summary>= 原版 `PlayerDataManager.ReadDataFromFile()`：
        /// 读那一份总档；**缺档 / 坏档 / 版本不认 ⇒ null**（原版返回 null，由 `LoadPlayerData` 转 `LoadStarterData`）。
        /// <para>⚠️ 原版还做 `Version` 构造与比较（`dataVersion`）；本仓把 `dataVersion` 与
        /// <see cref="CurrentDataVersion"/> 直接比 —— **未来版本拒绝、旧版本照收**（原版 `Version.CompareTo` 也是这个方向，
        /// 见 `.c` 里拿 `dataVersion` 当解密参数那一路）。</para></summary>
        public static LocalPlayerData ReadDataFromFile()
        {
            LastError = "";
            var d = SaveCenter.Get<LocalPlayerData>(SaveKey);
            if (d == null)
            {
                LastReadOutcome = ReadOutcome.Missing;      // 缺档与坏档在 `SaveCenter` 里都是 null（它自己出声）
                return null;
            }
            if (d.dataVersion > CurrentDataVersion)
            {
                LastReadOutcome = ReadOutcome.BadVersion;
                LastError = "档的 `dataVersion` = " + d.dataVersion + " 比本进程的 " + CurrentDataVersion
                          + " 还新 ⇒ 拒绝（原版同样不读未来版本）";
                Debug.LogWarning("[LocalPlayerData] " + LastError);
                return null;
            }
            LastReadOutcome = ReadOutcome.Ok;
            return d;
        }

        /// <summary>= 原版 `PlayerDataManager.LoadPlayerData()`：读档 → 有则 <see cref="Apply"/>，
        /// 无则 <see cref="LoadStarterData"/>。
        /// <para>返回「这一趟读到档了吗」。⚠️ 它**不**保证 `Apply` 成功（那看 <see cref="LastError"/>）。</para></summary>
        public static bool LoadPlayerData()
        {
            var d = ReadDataFromFile();
            if (d == null) { LoadStarterData(); return false; }
            Apply(d);
            return true;
        }

        /// <summary>= 原版 `PlayerDataManager.LoadStarterData()` 的**本仓等价面**：
        /// 原版是「把出厂值灌满 + 建首套数据」。本仓出厂值各自住在自己的单源里（`GameStaticData` 的 13 格默认值
        /// + 各 `PlayerPrefs` 族），唯一要动的就是那份**共享默认值** ⇒ 调
        /// <see cref="GameStaticData.LoadSharedDefaultValues"/>，其余不动（⛔ 不覆盖玩家已有设置）。</summary>
        public static void LoadStarterData()
        {
            GameStaticData.LoadSharedDefaultValues();
            Debug.Log("[LocalPlayerData] 没读到总档 ⇒ 走 `LoadStarterData`（原版同一支）：只灌出厂默认值，"
                    + "各设置仍以它们自己的单源为准");
        }

        // ================================================================== ⑤ 启动/退出接线
        /// <summary>启动那一次把总档读回来（= 原版 `PlayerDataManager.Awake → LoadPlayerData` 的时机；
        /// 照 `GraphicQuality.LoadAtStartup` 的既有先例用 `AfterSceneLoad`）。
        /// <para>🔴 **只在真跑里起**：编辑器批处理（`ShellScene.Run` 那类）走这一行会把玩家的真 `PlayerPrefs` 改掉 ⇒
        /// 与 `MenuTutorialFlow` / `SmallScreenUI` 那几处的 `Application.isPlaying` 闸同一条纪律。</para></summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void LoadAtStartup()
        {
            if (_booted) return;
            _booted = true;
            if (!Application.isPlaying || Application.isBatchMode) return;   // 见上面那条纪律
            LoadPlayerData();
            Application.quitting += OnQuitting;                              // 退出前再存一次（`SavePlayerData` 的原版调用点之一）
        }

        static void OnQuitting()
        {
            try { SavePlayerData(); }
            catch (Exception e) { Debug.LogWarning("[LocalPlayerData] 退出前存总档失败：" + e.Message); }
        }

        // ================================================================== ⑥ 自检口
        /// <summary>自检用：清掉进程内状态（⛔ 不动 `PlayerPrefs`、不动 `SaveCenter` 里的档）。</summary>
        public static void ResetForTest()
        {
            _last = null;
            _booted = false;
            LastError = "";
            LastReadOutcome = ReadOutcome.Missing;
        }

        /// <summary>自检用：把一份总档**直接**放进内存（= 模拟「上一趟读到/写过」），不走盘。</summary>
        public static void SetLastForTest(LocalPlayerData d) { _last = d == null ? null : CopyOf(d); }

        /// <summary>浅拷贝（40 个字段全是值类型或 string ⇒ 浅拷贝就够；⛔ 别改成引用赋值 —— 那会让
        /// 「继承没有消费者的格子」变成「继承同一个对象」）。</summary>
        public static LocalPlayerData CopyOf(LocalPlayerData s)
        {
            if (s == null) return null;
            return new LocalPlayerData
            {
                loginName = s.loginName,
                loginPassword = s.loginPassword,
                deviceId = s.deviceId,
                playFabId = s.playFabId,
                languageVersion = s.languageVersion,
                lastRegistrationShowTime = s.lastRegistrationShowTime,
                disableParticles = s.disableParticles,
                notificationsOn = s.notificationsOn,
                crateNotificationsOn = s.crateNotificationsOn,
                chatInvisibleModeOn = s.chatInvisibleModeOn,
                friendlyBattle = s.friendlyBattle,
                loginWithGoogle = s.loginWithGoogle,
                dontUseAltPortSetting = s.dontUseAltPortSetting,
                noBotsModeOn = s.noBotsModeOn,
                noTipsModeOn = s.noTipsModeOn,
                noFfullScreen = s.noFfullScreen,
                musicVolume = s.musicVolume,
                voiceOversVolume = s.voiceOversVolume,
                fxVolume = s.fxVolume,
                dataVersion = s.dataVersion,
                useMobileBattleScreenSize = s.useMobileBattleScreenSize,
                lastBuildSaved = s.lastBuildSaved,
                hiFPS = s.hiFPS,
                vsync = s.vsync,
                fpsLimit = s.fpsLimit,
                smallScreenUI = s.smallScreenUI,
                graphicsQuality = s.graphicsQuality,
                useSuperSampling = s.useSuperSampling,
                useCombatAutoZoom = s.useCombatAutoZoom,
                graphicsExtendedCompatibility = s.graphicsExtendedCompatibility,
                cardsTextInHandFormat = s.cardsTextInHandFormat,
                cardsTextInHandFormatChosenManually = s.cardsTextInHandFormatChosenManually,
                graphicsQualityChosenManually = s.graphicsQualityChosenManually,
                touchInputChosenManually = s.touchInputChosenManually,
                smallUIChosenManually = s.smallUIChosenManually,
                autoCombatChosenManually = s.autoCombatChosenManually,
                touchInput = s.touchInput,
                unrankedMultiplayer = s.unrankedMultiplayer,
                unrankedFastMode = s.unrankedFastMode,
                lastTimeAskedForNotifications = s.lastTimeAskedForNotifications,
            };
        }
    }
}
