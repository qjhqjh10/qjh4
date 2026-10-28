// B94B7_Assert.cs — 执行代理 B94_B7（第九十四会话 · 波 2）：`LocalPlayerData` 总档三链的断言。
//
// ============================ 它盯的是什么 ============================
// 判据全文 → `资料/完全复刻/报告/B94_B7.md`；落点 → `Shell/LocalPlayerData.cs`。
// 三条账行：`其他_基础设施` 427（`LoadPlayerData`）/ 461（`ReadDataFromFile`）/ 481（`SavePlayerData`）。
//  · 原版 DTO 字段表 = `d:/2/Warpforge_code/.../LocalPlayerData.cs`（**40 个 public 字段**）
//  · `SavePlayerData` / `LoadPlayerData` / `ReadDataFromFile` 体 → 各自 `.c`（见落点文件头）
//
// ============================ 怎么用（宿主）============================
//   `B94B7_Assert.Run();`
//   宿主里两行：`_sink.Pass += B94B7_Assert.Pass; _sink.Fail += B94B7_Assert.Fail;`
//   （本代理的写手白名单**不含任何 `Editor/*Scene.cs` 宿主** ⇒ 接线由调度台合并时统一加。）
//   · 不跑 Unity 批处理；临时档走 `SaveCenter.OverrideRoot` 指到一个临时目录，`finally` 里删掉。
//   · 🔴 **不碰玩家的真设置**：全程 `LocalPlayerDataStore.PersistOverride = true`（只灌 `GameStaticData` 镜像）。
//
// ============================ 🔴 灭自证（改坏法写在文案里）============================
//   · B7-2：把 `Collect()` 里 `d.fxVolume = …` 那行删掉 ⇒ 音量映射三条红。
//   · B7-5：把 `ReadDataFromFile` 的版本闸删掉 ⇒ 「未来版本被拒」那条红（它会读回来）。
//   · B7-6：把 `CopyOf` 里任意一格删掉 ⇒ 40 字段往返那条红。
//   · B7-7：把 `Collect()` 改成 `new LocalPlayerData()`（不继承 `_last`）⇒「没有消费者的格子原样带回」红。
using System;
using System.IO;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 B7：`LocalPlayerData` 总档（`SavePlayerData` / `LoadPlayerData` / `ReadDataFromFile`）。</summary>
public static class B94B7_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B7] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    const int FieldCount = 40;      // 原版 `LocalPlayerData` 的 public 字段数（逐行数过 `LocalPlayerData.cs`）

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_B7 · LocalPlayerData 总档（Save/Load/ReadDataFromFile）");
        string tmp = Path.Combine(Path.GetTempPath(), "wf_b94b7_" + Guid.NewGuid().ToString("N"));
        string prevRoot = SaveCenter.OverrideRoot;
        bool prevPersist = LocalPlayerDataStore.PersistOverride;
        // 🔴 本断言会灌 `GameStaticData` 镜像（`SectionApplyMirror`）⇒ 跑完必须**逐格还原**，
        //    否则同宿主里后面的断言会读到我们改过的画质/帧率/音量（自检互相污染 —— 同 `A1068` 那条纪律）。
        Snapshot();
        try
        {
            Directory.CreateDirectory(tmp);
            // 🧨 前提（先断前提，再断结论 —— 前提不成立时下面的断言一条都不该跑）
            SaveCenter.OverrideRoot = tmp;
            LocalPlayerDataStore.PersistOverride = true;
            Tr(SaveCenter.StoreOn, "🧨 前提：`SaveCenter.StoreOn`（OverrideRoot 指到临时目录）");
            Tr(SaveCenter.PathOf(LocalPlayerDataStore.SaveKey).StartsWith(tmp), "🧨 前提：总档会落在临时目录里（不碰玩家真档）");
            if (!SaveCenter.StoreOn) return;

            SectionPremise();
            SectionRoundTrip();
            SectionVersionGate();
            SectionMapping();
            SectionApplyMirror();
            SectionCarryUnknownFields();
            SectionDemoData();
        }
        catch (Exception e)
        {
            Tr(false, "B7 断言抛异常：" + e.GetType().Name + " " + e.Message);
        }
        finally
        {
            Restore();
            LocalPlayerDataStore.PersistOverride = prevPersist;
            SaveCenter.OverrideRoot = prevRoot;
            LocalPlayerDataStore.ResetForTest();
            try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); } catch { /* 临时目录删不掉不影响结论 */ }
        }
    }

    // ============================================================ 现场快照 / 还原（自检互不污染）
    static float _mMusic, _mSfx, _mVo;
    static bool _mHiFps, _mVsync, _mDisableParticles, _mSuperSampling, _mExtCompat;
    static int _mFpsLimit, _mLangVersion, _mHandText;
    static long _mLastReg, _mLastAsk;
    static GameStaticData.EverguildQualitySetting _mQuality;

    static void Snapshot()
    {
        _mMusic = GameStaticData.MusicVolume;
        _mSfx = GameStaticData.SoundFxVolume;
        _mVo = GameStaticData.VoiceOversVolume;
        _mHiFps = GameStaticData.hiFPS;
        _mVsync = GameStaticData.vsync;
        _mFpsLimit = GameStaticData.FPSLimit;
        _mDisableParticles = GameStaticData._disableParticles;
        _mSuperSampling = GameStaticData.useSuperSampling;
        _mExtCompat = GameStaticData.graphicsExtendedCompatibility;
        _mLangVersion = GameStaticData.languageVersion;
        _mLastReg = GameStaticData.lastRegistrationShowTime;
        _mLastAsk = GameStaticData.lastTimeAskedForNotifications;
        _mQuality = GameStaticData.graphicsQuality;
        _mHandText = CardView.HandTextFormat;
    }

    static void Restore()
    {
        GameStaticData.MusicVolume = _mMusic;
        GameStaticData.SoundFxVolume = _mSfx;
        GameStaticData.VoiceOversVolume = _mVo;
        GameStaticData.hiFPS = _mHiFps;
        GameStaticData.vsync = _mVsync;
        GameStaticData.FPSLimit = _mFpsLimit;
        GameStaticData._disableParticles = _mDisableParticles;
        GameStaticData.useSuperSampling = _mSuperSampling;
        GameStaticData.graphicsExtendedCompatibility = _mExtCompat;
        GameStaticData.languageVersion = _mLangVersion;
        GameStaticData.lastRegistrationShowTime = _mLastReg;
        GameStaticData.lastTimeAskedForNotifications = _mLastAsk;
        GameStaticData.graphicsQuality = _mQuality;
        CardView.SetHandTextFormat(_mHandText);
    }

    // ============================================================ 0 结构前提
    static void SectionPremise()
    {
        // 🧨 前提：DTO 的字段数与原版一致（少一格 = 落盘格式与原版对不上）
        var fields = typeof(LocalPlayerData).GetFields(BindingFlags.Public | BindingFlags.Instance);
        C(fields.Length, FieldCount, "🧨 前提：`LocalPlayerData` 的 public 字段数 = 原版 40 格");

        // 🧨 前提：几个原版拼写怪的名字逐字在（`noFfullScreen` 三个 f）
        bool hasOdd = false;
        for (int i = 0; i < fields.Length; i++) if (fields[i].Name == "noFfullScreen") hasOdd = true;
        Tr(hasOdd, "🧨 前提：字段名照原版（`noFfullScreen` 三个 f）");

        // 🧨 前提：缺档时确实读不到（否则下面的「缺档/有档」两态分不开）
        SaveCenter.Delete(LocalPlayerDataStore.SaveKey);
        Tr(LocalPlayerDataStore.ReadDataFromFile() == null, "🧨 前提：没有档时 `ReadDataFromFile` 返回 null");
        C(LocalPlayerDataStore.LastReadOutcome, LocalPlayerDataStore.ReadOutcome.Missing,
          "缺档时 `LastReadOutcome = Missing`");
    }

    // ============================================================ 1 40 字段往返（= Save/Read 一对）
    static void SectionRoundTrip()
    {
        var d = FullSample();
        Tr(SaveCenter.Set(LocalPlayerDataStore.SaveKey, d), "把一份**逐格非零**的总档写进临时档");

        var back = LocalPlayerDataStore.ReadDataFromFile();
        Tr(back != null, "`ReadDataFromFile` 读回来了");
        if (back == null) return;
        C(LocalPlayerDataStore.LastReadOutcome, LocalPlayerDataStore.ReadOutcome.Ok, "读档结果档 = Ok");

        string a = JsonUtility.ToJson(d), b = JsonUtility.ToJson(back);
        Tr(a == b, "🧨 40 字段逐格往返完全相等（`JsonUtility.ToJson` 全文比；改坏 `CopyOf`/任何一格即红）");
        C(back.dataVersion, d.dataVersion, "`dataVersion` 原样带回");
        C(back.fpsLimit, 2, "`fpsLimit` 原样带回（样本 = 2 = 原版出厂值）");
        C(back.graphicsQuality, GameStaticData.EverguildQualitySetting.High, "`graphicsQuality` 枚举原样带回");
        C(back.cardsTextInHandFormat, GameStaticData.CardTextsInHandOptions.OnlyName, "`cardsTextInHandFormat` 枚举原样带回");
    }

    // ============================================================ 2 版本闸（原版「不读未来版本」）
    static void SectionVersionGate()
    {
        var d = FullSample();
        d.dataVersion = LocalPlayerDataStore.CurrentDataVersion + 1;
        SaveCenter.Set(LocalPlayerDataStore.SaveKey, d);
        Tr(LocalPlayerDataStore.ReadDataFromFile() == null, "🧨 未来版本的档**读不出来**（改坏版本闸即红）");
        C(LocalPlayerDataStore.LastReadOutcome, LocalPlayerDataStore.ReadOutcome.BadVersion, "结果档 = BadVersion");
        Tr(LocalPlayerDataStore.LastError.Length > 0, "拒绝时有**人话**（⛔ 不静默）");
    }

    // ============================================================ 3 映射（单源 → 档）
    static void SectionMapping()
    {
        LocalPlayerDataStore.ResetForTest();
        var d = LocalPlayerDataStore.Collect();
        Tr(d != null, "`Collect()` 有产出");
        if (d == null) return;

        C(d.musicVolume, Mathf.RoundToInt(WarpforgeAudio.Music * 100f),
          "🧨 `musicVolume` = `WarpforgeAudio.Music` × 100（改坏那一行即红）");
        C(d.fxVolume, Mathf.RoundToInt(WarpforgeAudio.SoundFx * 100f), "`fxVolume` = `WarpforgeAudio.SoundFx` × 100");
        C(d.voiceOversVolume, Mathf.RoundToInt(WarpforgeAudio.VoiceOver * 100f),
          "`voiceOversVolume` = `WarpforgeAudio.VoiceOver` × 100");
        C(d.languageVersion, (int)Loc.Current, "`languageVersion` = `Loc.Current`");
        C(d.lastBuildSaved, Application.version, "`lastBuildSaved` = `Application.version`（原版就是这一句）");
        C(d.dataVersion, LocalPlayerDataStore.CurrentDataVersion, "`dataVersion` 写的是本类的当前版本");
        C(d.graphicsQuality, GameStaticData.graphicsQuality, "`graphicsQuality` 取 `GameStaticData.graphicsQuality`");
        C(d.smallScreenUI, SmallScreenUI.Enabled, "`smallScreenUI` 取唯一来源 `SmallScreenUI.Enabled`");

        // 原版 `FPSLimit` 的三档换算（`SavePlayerData` 末段那三行）
        C(LocalPlayerDataStore.TargetFramerateFor(0), 30, "`FPSLimit = 0` ⇒ 30fps");
        C(LocalPlayerDataStore.TargetFramerateFor(1), 60, "`FPSLimit = 1` ⇒ 60fps");
        C(LocalPlayerDataStore.TargetFramerateFor(2), -1, "`FPSLimit = 2` ⇒ -1（不限帧）");
        C(LocalPlayerDataStore.TargetFramerateFor(3), 60, "`FPSLimit ≥ 3` ⇒ 60fps（原版那一支只有 0/1/2）");
    }

    // ============================================================ 4 Apply 只灌镜像（PersistOverride）
    static void SectionApplyMirror()
    {
        var d = FullSample();
        d.musicVolume = 42;
        d.voiceOversVolume = 7;
        d.fxVolume = 13;
        d.hiFPS = false;
        d.vsync = false;
        d.fpsLimit = 1;
        d.languageVersion = 99999;                  // 非法值 ⇒ 只出声、不动（本条只验「不崩 + 不动」）

        int prefsNotif = PlayerPrefs.GetInt(SettingsWindow.GeneralFlags.NotifyPrefKey, -1);
        int prefsTouch = PlayerPrefs.GetInt(SettingsWindow.GeneralFlags.TouchPrefKey, -1);
        LocalPlayerDataStore.Apply(d);

        C(GameStaticData.MusicVolume, 0.42f, "🧨 `Apply` 把 `musicVolume 42` 灌成 `MusicVolume = 0.42`");
        C(GameStaticData.SoundFxVolume, 0.13f, "`fxVolume 13` ⇒ `SoundFxVolume = 0.13`");
        C(GameStaticData.VoiceOversVolume, 0.07f, "`voiceOversVolume 7` ⇒ `VoiceOversVolume = 0.07`");
        Tr(!GameStaticData.hiFPS, "`hiFPS` 灌进镜像");
        Tr(!GameStaticData.vsync, "`vsync` 灌进镜像");
        C(GameStaticData.FPSLimit, 1, "`fpsLimit` 灌进镜像");

        // 🔴 `PersistOverride = true` ⇒ **一个字节都不许动玩家真设置**
        C(PlayerPrefs.GetInt(SettingsWindow.GeneralFlags.NotifyPrefKey, -1), prefsNotif,
          "🧨 `PersistOverride` 下 `Apply` **不写** `PlayerPrefs`（通知开关那条）");
        C(PlayerPrefs.GetInt(SettingsWindow.GeneralFlags.TouchPrefKey, -1), prefsTouch,
          "`PersistOverride` 下 `Apply` **不写** `PlayerPrefs`（触摸那条）");
    }

    // ============================================================ 5 没有消费者的格子原样带回
    static void SectionCarryUnknownFields()
    {
        var prev = new LocalPlayerData
        {
            noTipsModeOn = true,
            unrankedFastMode = true,
            crateNotificationsOn = true,
            chatInvisibleModeOn = true,
            noFfullScreen = true,
            loginName = "carried-name",
            playFabId = "carried-playfab",
            lastRegistrationShowTime = 1234567L,
            lastTimeAskedForNotifications = 7654321L,
        };
        LocalPlayerDataStore.SetLastForTest(prev);
        var d = LocalPlayerDataStore.Collect();
        Tr(d != null, "`Collect()` 在「有上一份」时仍有产出");
        if (d == null) return;

        Tr(d.noTipsModeOn, "🧨 没有消费者的格子**原样带回**（`noTipsModeOn`；改成 `new LocalPlayerData()` 即红）");
        Tr(d.unrankedFastMode, "`unrankedFastMode` 原样带回");
        Tr(d.crateNotificationsOn, "`crateNotificationsOn` 原样带回");
        Tr(d.chatInvisibleModeOn, "`chatInvisibleModeOn` 原样带回");
        Tr(d.noFfullScreen, "`noFfullScreen` 原样带回");
        C(d.loginName, "carried-name", "`loginName` 原样带回");
        C(d.playFabId, "carried-playfab", "`playFabId` 原样带回（⚠️ 原版 `SavePlayerData` **不写**这一格 ⇒ 我们也不改写它）");
        C(d.lastRegistrationShowTime, 1234567L, "`lastRegistrationShowTime` 原样带回");
        C(d.lastTimeAskedForNotifications, 7654321L, "`lastTimeAskedForNotifications` 原样带回");
    }

    // ============================================================ 6 `DemoData`（账 348 / 379 / 497 / 511）
    static void SectionDemoData()
    {
        MenuCheck.Section(_sink, "B94_B7 · DemoData（教程进度实体）+ 四谓词");

        // 🧨 前提：桥的两个方向都在（否则下面每条都在测空气）
        var emptyF = DemoDataStore.ToFile(null);
        Tr(emptyF != null, "🧨 前提：`ToFile(null)` 给一份非 null 载体");
        var emptyD = DemoDataStore.FromFile(null);
        Tr(emptyD != null && emptyD.tutorialsCompleted != null && emptyD.tutorialsCompleted.Count == 0,
           "🧨 前提：`FromFile(null)` 给一份空字典（不是 null 字典）");

        // ① 往返：字典 + 其余 9 格
        var d = new DemoData
        {
            gameAfterTutorialPlayed = true, suscriptionPopupShowed = true, suscribed = true,
            currentDeck = "skirmish/um/1", canEnterDraftMode = true, playerTitle = "ttl_1",
            feedbackSeen = true, feedbackSent = true,
        };
        d.tutorialsCompleted["0"] = true;
        d.tutorialsCompleted["SelectArmy"] = true;
        d.tutorialsCompleted["x"] = false;
        var back = DemoDataStore.FromFile(DemoDataStore.ToFile(d));
        C(back.tutorialsCompleted.Count, 3, "🧨 字典 3 条经「数组桥」往返后还是 3 条");
        Tr(back.tutorialsCompleted["0"], "往返保住 `true`");
        Tr(back.tutorialsCompleted.ContainsKey("x") && !back.tutorialsCompleted["x"], "往返保住 `false`（键在、值为假）");
        C(back.currentDeck, "skirmish/um/1", "`currentDeck` 往返");
        C(back.playerTitle, "ttl_1", "`playerTitle` 往返（原版那格是 `LoadableReference`，我们存 targetId）");
        Tr(back.gameAfterTutorialPlayed && back.canEnterDraftMode && back.feedbackSeen && back.feedbackSent
           && back.suscribed && back.suscriptionPopupShowed, "其余 6 个 bool 往返");

        // ② `ResetTutorial` = 原版那两句（⛔ 别顺手清干净）
        var r = DemoDataStore.FromFile(DemoDataStore.ToFile(d));
        r.ResetTutorial();
        C(r.tutorialsCompleted.Count, 0, "🧨 `ResetTutorial`：换新字典（旧 3 条没了）");
        Tr(!r.gameAfterTutorialPlayed, "🧨 `ResetTutorial`：`gameAfterTutorialPlayed = false`");
        C(r.currentDeck, "skirmish/um/1", "🧨 `ResetTutorial` **不动**其余 8 格（原版 `.c` 只有两句）");

        // ③ `IsTutorialCompleted` 的键 = 下标串（原版就是 `num.ToString()`，⛔ 不是 stageId）
        var k = new DemoData();
        k.tutorialsCompleted["0"] = true;
        Tr(k.IsTutorialCompleted(0), "🧨 `IsTutorialCompleted(0)` 读的是键 `\"0\"`");
        Tr(!k.IsTutorialCompleted(1), "字典里没有 `\"1\"` ⇒ 假");
        k.ToggleCanEnterDraftMode(true);
        Tr(k.canEnterDraftMode, "`ToggleCanEnterDraftMode(true)`");

        // ④ 存储口的读写（走临时档）
        DemoDataStore.ResetForTest();
        DemoDataStore.SetTutorial("B7_PROBE", true);
        bool found, val = DemoDataStore.TryGetTutorial("B7_PROBE", out found);
        Tr(found && val, "🧨 `SetTutorial` / `TryGetTutorial` 走通（落临时档）");
        bool found2, _2 = DemoDataStore.TryGetTutorial("B7_NOPE", out found2);
        Tr(!found2, "没写过的键 ⇒ `found = false`（调用方据此去读旧档兜底）");

        // ⑤ 四谓词：`MenuTutorialFlow` 现在读的就是 `DemoData`（旧 Prefs 只兜底）
        int nStages = MenuTutorialStages.Count;
        Tr(nStages > 0, "🧨 前提：阶段表非空（否则下面两条在测空气）");
        if (nStages > 0)
        {
            DemoDataStore.ResetForTest();
            for (int i = 0; i < nStages; i++)
            {
                var st = MenuTutorialStages.At(i);
                if (st == null) continue;
                DemoDataStore.SetTutorial(StageKeyOf(st), true);
            }
            Tr(MenuTutorialFlow.AllTutorialsCompleted(), "🧨 四个谓词：全部 stage 置完成 ⇒ `AllTutorialsCompleted` = 真");
            Tr(MenuTutorialFlow.FirstTwoTutorialCompleted(), "…且前两段都完成 ⇒ `FirstTwoTutorialCompleted` = 真");

            // 去掉第 2 段（下标 1）⇒ `&&` 的两态：全体完成假、前两段假；只去掉最后一段 ⇒ 前两段仍真
            var s1 = MenuTutorialStages.At(1);
            if (s1 != null)
            {
                DemoDataStore.SetTutorial(StageKeyOf(s1), false);
                Tr(!MenuTutorialFlow.FirstTwoTutorialCompleted(),
                   "🧨 `&&` 不是 `||`（清掉下标 1 ⇒ 前两段 = 假）");
                Tr(!MenuTutorialFlow.AllTutorialsCompleted(), "少一段 ⇒ 全体完成 = 假");
                Tr(MenuTutorialFlow.IsStageCompleted(s1) == false, "`IsStageCompleted` 跟着新档走（不信旧 Prefs）");
                DemoDataStore.SetTutorial(StageKeyOf(s1), true);
            }
        }

        // ⑥ 结构：`PracticeModePopup` 里那个私有 1 字段 DTO 必须已经不在了（两份写同一个 key 会互相清空）
        var stale = typeof(PracticeModePopup).GetNestedType("DemoDataDto", BindingFlags.NonPublic);
        Tr(stale == null, "🧨 `PracticeModePopup.DemoDataDto` 已删除（唯一写口 = `DemoDataStore`）");

        DemoDataStore.ResetForTest();
    }

    /// <summary>= `MenuTutorialFlow` 的存档键形状（`PrefDonePrefix + (StageId 或 "#"+ClassName)`）。
    /// ⚠️ 那是**私有** `DoneKey`，这里照它的形状复刻一份**只给断言用**（⛔ 不改产品代码的可视性）。</summary>
    static string StageKeyOf(MenuTutorialStageDef d)
    {
        if (d == null) return null;
        return MenuTutorialFlow.PrefDonePrefix + (string.IsNullOrEmpty(d.StageId) ? "#" + d.ClassName : d.StageId);
    }

    // ============================================================ 样本：40 格全部非默认
    static LocalPlayerData FullSample()
    {
        return new LocalPlayerData
        {
            loginName = null,                 // 原版 `SavePlayerData` 写它（我们无登录 ⇒ 恒 null）
            loginPassword = null,             // 同上（原版加密后落盘；我们拒绝非空）
            deviceId = null,
            playFabId = null,
            languageVersion = 110,            // Chinese
            lastRegistrationShowTime = 1111L,
            disableParticles = true,
            notificationsOn = true,
            crateNotificationsOn = true,
            chatInvisibleModeOn = true,
            friendlyBattle = true,
            loginWithGoogle = true,
            dontUseAltPortSetting = true,
            noBotsModeOn = true,
            noTipsModeOn = true,
            noFfullScreen = true,
            musicVolume = 55,
            voiceOversVolume = 65,
            fxVolume = 75,
            dataVersion = LocalPlayerDataStore.CurrentDataVersion,
            useMobileBattleScreenSize = true,
            lastBuildSaved = "test-build",
            hiFPS = true,
            vsync = true,
            fpsLimit = 2,
            smallScreenUI = true,
            graphicsQuality = GameStaticData.EverguildQualitySetting.High,
            useSuperSampling = true,
            useCombatAutoZoom = true,
            graphicsExtendedCompatibility = true,
            cardsTextInHandFormat = GameStaticData.CardTextsInHandOptions.OnlyName,
            cardsTextInHandFormatChosenManually = true,
            graphicsQualityChosenManually = true,
            touchInputChosenManually = true,
            smallUIChosenManually = true,
            autoCombatChosenManually = true,
            touchInput = true,
            unrankedMultiplayer = true,
            unrankedFastMode = true,
            lastTimeAskedForNotifications = 2222L,
        };
    }
}
