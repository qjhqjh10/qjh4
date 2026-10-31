// B109_E1_SettingsPersist_Assert.cs — 执行代理 `B109_E1`（2026-10-31 · 第一百零九会话第 1 波）落地的两条断言
//
// ============================ 覆盖（逐条对 `判据`）============================
//  · `Run()` —— **A：`GameStaticData.vsync` / `FPSLimit` 跨会话保留**（缺陷：原来只有运行期静态字段）
//      原版写：`PlayerDataManager__SavePlayerData.c` 尾部两句
//        `*(undefined1*)(lpd + 0x69) = *(undefined1*)(GameStaticData + 0x127);`   // vsync
//        `*(undefined4*)(lpd + 0x6c) = *(undefined4*)(GameStaticData + 0x128);`   // FPSLimit
//        （ILSpy 同处：`cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:10241-10242`）
//      原版读：`PlayerDataManager.LoadPlayerData()`（ILSpy 同文件 `:10397-10398`）
//        `GameStaticData.vsync = localPlayerData.vsync; GameStaticData.FPSLimit = localPlayerData.fpsLimit;`
//      原版调用点：`SettingsMenu__Close.c` **第一句** `PlayerDataManager__SavePlayerData(...)`
//        （无条件、在 `GameWindowWithTabs.Close` 之前）⇒ 我们的等价物 = `SettingsWindow.Close()` 开头那一句。
//      ⇒ 断言：改 → `SavePlayerData()` → 清进程内缓存 → `ReadDataFromFile()` 读回**新值**；
//        再做一次「换一组值」的往返（灭自证：只测一次分不出「真写了」与「读的是出厂值」）；
//        最后用 `LoadPlayerData()` 模拟「重开游戏」⇒ 灌回 `GameStaticData` 的是档里的值。
//  · `RunInitialValue(win)` —— **B：开窗初值取自已存的 `GameStaticData.FPSLimit`**（不是 `Application.targetFrameRate`）
//      原版 `GraphicsTab__OnSetup.c`：那颗 `fpsLimit` Slider 的「只摆值」与紧邻的
//        `vsync.SetIsOnWithoutNotify(GameStaticData.vsync)` 同族（ILSpy `GraphicsTab.cs:516` 那一段；
//        它自己那一跳被译成 `Indirect call … +438` 未解析，见报告「没查清的部分」）。
//      ⇒ 断言：把 `GameStaticData.FPSLimit` 与 `Application.targetFrameRate` 摆成**反查结果不同**的两个值，
//        关窗（顺带断「关窗那一下真的写总档」= 原版第一句）→ 重开窗 → `win.FpsIndex` 必须跟 `GameStaticData.FPSLimit`。
//
// ============================ 口径 ============================
//  · 期望值全部来自**原版判据**（反编译方法体），逐条写进用例文案；⛔ 不拿 `LocalPlayerDataStore` 自己的常量证明它自己。
//  · 前提断言与结论断言**分开**：⛔ 没有 `if (x != null) Check(...)` 那种静默跳过。
//  · 每段都**备份 / 还原**：`SaveCenter.OverrideRoot` · `LocalPlayerDataStore.PersistOverride` ·
//    `GameStaticData` 那一组静态量 · `Application.targetFrameRate` · `SettingsWindow.VSyncSetterOverride` /
//    `ApplyGraphicsChangesOnClose` · `SaveCenter` 的临时根目录（跑完删掉）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  · `Editor/SettingsScene.cs`（调度台）—— A、B 两段都要（B 需要一具**建好的** `SettingsWindow`）：
//        B109_E1_SettingsPersist_Assert.Run(win);
//        _sink.Pass += B109_E1_SettingsPersist_Assert.Pass; _sink.Fail += B109_E1_SettingsPersist_Assert.Fail;
//    ⚠️ **B 段会调一次 `win.Close()`**（= 原版 `SettingsMenu.Close` 那一条链，本缺陷的落点就是它）⇒
//      会连带跑 `SuperSampling.ApplyIfDirty()`。宿主已有 `SuperSampling.PersistOverride = true; ResetForTest();`
//      （`SettingsScene.cs:618-623`）⇒ 请把本段放在**超采样那一节的脏位已经清掉**之后（本段自己也会把
//      `ApplyGraphicsChangesOnClose` 置 false、并用 `VSyncSetterOverride` 拦下 `vSyncCount` 的写入）。
//  · 只想跑 A（不需要窗口）：`B109_E1_SettingsPersist_Assert.Run()`。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B109_E1`：设置窗「vsync / 帧率上限」的**跨会话保留**（A）与**开窗初值源**（B）断言。</summary>
public static class B109_E1_SettingsPersist_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E1] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口

    /// <summary>A 段（自包含，不需要窗口）：改 → 存 → 重读 → 仍是新值。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B109_E1 · A（vsync / FPSLimit 跨会话保留：改 → 存 → 重读 → 仍是新值）");
        try { A_PersistRoundTrip(); }
        catch (Exception e) { Tr(false, "「vsync / FPSLimit 跨会话保留」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B109E1（A）合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>A + B 两段（B 需要一具**建好的** `SettingsWindow`）。</summary>
    public static void Run(SettingsWindow win)
    {
        Run();
        RunInitialValue(win);
    }

    /// <summary>B 段：开窗初值 = `GameStaticData.FPSLimit`；顺带断「关窗那一下真的写总档」。</summary>
    public static void RunInitialValue(SettingsWindow win)
    {
        MenuCheck.Section(_sink, "B109_E1 · B（开窗初值 = `GameStaticData.FPSLimit`；关窗 = 原版 `SettingsMenu.Close` 第一句 `SavePlayerData`）");
        try { B_InitialValueFromStore(win); }
        catch (Exception e) { Tr(false, "「开窗初值 / 关窗落盘」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B109E1（B）合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A

    static void A_PersistRoundTrip()
    {
        string prevRoot = SaveCenter.OverrideRoot;
        bool prevPersist = LocalPlayerDataStore.PersistOverride;
        // `Apply()` 会灌这一组镜像 —— 全备份，跑完还原（⛔ 别让自检把别的段的前提改掉）
        bool v0 = GameStaticData.vsync;
        int f0 = GameStaticData.FPSLimit;
        bool dp0 = GameStaticData._disableParticles;
        bool hi0 = GameStaticData.hiFPS;
        bool ss0 = GameStaticData.useSuperSampling;
        bool ec0 = GameStaticData.graphicsExtendedCompatibility;
        long lr0 = GameStaticData.lastRegistrationShowTime;
        long ln0 = GameStaticData.lastTimeAskedForNotifications;
        float mv0 = GameStaticData.MusicVolume, sv0 = GameStaticData.SoundFxVolume, vv0 = GameStaticData.VoiceOversVolume;
        int lv0 = GameStaticData.languageVersion;
        int htf0 = CardView.HandTextFormat;

        string root = Path.Combine(Path.GetTempPath(), "wf_b109e1_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            SaveCenter.OverrideRoot = root;                   // 自检临时档根 ⇒ 不碰玩家真档
            LocalPlayerDataStore.PersistOverride = true;      // 只走盘，不动 mixer / PlayerPrefs / targetFrameRate
            LocalPlayerDataStore.ResetForTest();
            Tr(SaveCenter.StoreOn, "（前提）临时档根已注入 ⇒ 落盘门 `SaveCenter.StoreOn` = true");
            Tr(LocalPlayerDataStore.ReadDataFromFile() == null,
               "（前提）临时目录里**还没有**总档（`ReadDataFromFile` = null）—— 否则下面「读回新值」可能是读到残留档");

            // ---- ① 一组「与出厂值都不同」的取值：vsync 出厂 true（`GameStaticData.cs` 的 `.cctor` = 1）
            //      · FPSLimit 出厂 2（= 不限帧）⇒ 这里写 0（= 30 档）
            GameStaticData.vsync = false;
            GameStaticData.FPSLimit = 0;
            Tr(LocalPlayerDataStore.SavePlayerData(),
               "① `SavePlayerData()` 写盘成功（= 原版 `SettingsMenu.Close` 第一句调的那一个方法）");
            LocalPlayerDataStore.ResetForTest();               // 清进程内 `_last` ⇒ 下面这一读只能来自盘
            var d1 = LocalPlayerDataStore.ReadDataFromFile();
            Tr(d1 != null, "（前提）② 档读得回来（`ReadDataFromFile` 非 null；outcome = "
                         + LocalPlayerDataStore.LastReadOutcome + "）");
            if (d1 == null) return;
            C(d1.vsync, false, "② ★ 档里的 `vsync` = **false**（原版 `PlayerDataManager.cs:10241` "
                             + "`localPlayerData.vsync = GameStaticData.vsync`；🧨 不写这一格 ⇒ 读回 `true`（出厂值）⇒ 红）");
            C(d1.fpsLimit, 0, "② ★ 档里的 `fpsLimit` = **0**（原版 `PlayerDataManager.cs:10242` "
                            + "`localPlayerData.fpsLimit = GameStaticData.FPSLimit`(`+0x128`)；🧨 同上）");

            // ---- ③ 换一组**相反**的值再走一遍。
            //      灭自证：只测一次分不出「真写了」与「读的正好是出厂值」—— 这里两组都跟出厂值不同、且彼此相反。
            GameStaticData.vsync = true;
            GameStaticData.FPSLimit = 1;
            Tr(LocalPlayerDataStore.SavePlayerData(), "③ 换一组相反的值，第二次写盘成功");
            LocalPlayerDataStore.ResetForTest();
            var d2 = LocalPlayerDataStore.ReadDataFromFile();
            Tr(d2 != null, "（前提）③ 第二次读得回来");
            if (d2 != null)
            {
                C(d2.vsync, true, "③ ★ 档里的 `vsync` = **true**（第二次写的值 —— 第一组是 false ⇒ 这一条挡「一直返回同一个值」的假绿）");
                C(d2.fpsLimit, 1, "③ ★ 档里的 `fpsLimit` = **1**（第二次写的值；第一组是 0）");
            }

            // ---- ④ 「重开游戏」= `LoadPlayerData()`：先把内存摆成**与档不同**的值，再读档 ⇒ 必须变成档里的值
            GameStaticData.vsync = false;
            GameStaticData.FPSLimit = 2;
            LocalPlayerDataStore.ResetForTest();
            Tr(LocalPlayerDataStore.LoadPlayerData(),
               "④ `LoadPlayerData()` 读到了档（= 原版 `PlayerDataManager.Awake → LoadPlayerData` 那一次）");
            C(GameStaticData.vsync, true, "④ ★ 灌回 `GameStaticData.vsync` = 档里的 `true`"
                                        + "（原版 `PlayerDataManager.cs:10397`；🧨 删掉那一句 ⇒ 保持 `false` ⇒ 红）");
            C(GameStaticData.FPSLimit, 1, "④ ★ 灌回 `GameStaticData.FPSLimit` = 档里的 `1`"
                                        + "（原版 `PlayerDataManager.cs:10398`；🧨 同上）");
        }
        finally
        {
            SaveCenter.OverrideRoot = prevRoot;
            LocalPlayerDataStore.PersistOverride = prevPersist;
            LocalPlayerDataStore.ResetForTest();
            GameStaticData.vsync = v0;
            GameStaticData.FPSLimit = f0;
            GameStaticData._disableParticles = dp0;
            GameStaticData.hiFPS = hi0;
            GameStaticData.useSuperSampling = ss0;
            GameStaticData.graphicsExtendedCompatibility = ec0;
            GameStaticData.lastRegistrationShowTime = lr0;
            GameStaticData.lastTimeAskedForNotifications = ln0;
            GameStaticData.MusicVolume = mv0;
            GameStaticData.SoundFxVolume = sv0;
            GameStaticData.VoiceOversVolume = vv0;
            GameStaticData.languageVersion = lv0;
            CardView.SetHandTextFormat(htf0);
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { /* 临时目录删不掉不算失败 */ }
        }
    }

    // ============================================================ B

    static void B_InitialValueFromStore(SettingsWindow win)
    {
        Tr(win != null, "（前提）拿到一具**建好的** `SettingsWindow`（`SetFpsIndex` / `FpsIndex` 都是它的成员）");
        if (win == null) return;
        var manager = win.Manager;
        Tr(manager != null, "（前提）窗上挂着 `WindowsManager`（关窗后要经它 `OpenWindow` 重开）");
        if (manager == null) return;

        string prevRoot = SaveCenter.OverrideRoot;
        bool prevPersist = LocalPlayerDataStore.PersistOverride;
        bool prevDirty = SettingsWindow.ApplyGraphicsChangesOnClose;
        Action<int> prevVsync = SettingsWindow.VSyncSetterOverride;
        bool prevSsPersist = SuperSampling.PersistOverride;
        bool ss0 = SuperSampling.Enabled;
        int f0 = Application.targetFrameRate;
        int fsl0 = GameStaticData.FPSLimit;
        int idx0 = win.FpsIndex;

        string root = Path.Combine(Path.GetTempPath(), "wf_b109e1b_" + Guid.NewGuid().ToString("N"));
        bool closed = false;
        try
        {
            Directory.CreateDirectory(root);
            SaveCenter.OverrideRoot = root;
            LocalPlayerDataStore.PersistOverride = true;          // 只走盘，不动 targetFrameRate / mixer
            SuperSampling.PersistOverride = true;                 // 关窗那一下 `ApplyIfDirty` 不写 `PlayerPrefs`
            SettingsWindow.ApplyGraphicsChangesOnClose = false;   // ⛔ 别让关窗去应用画质（会把下面的哨兵 144 覆盖掉）
            SettingsWindow.VSyncSetterOverride = c => { };        // 拦下 `QualitySettings.vSyncCount` 的写入（⛔ 不写脏工程设置）
            LocalPlayerDataStore.ResetForTest();

            // ---- ① 把两个**源**摆成「反查结果不同」的值：
            //        `Application.targetFrameRate`(已应用值) = 144 ⇒ 反查档 2；
            //        `GameStaticData.FPSLimit`(原版初值源) = 1 ⇒ 档 1。
            Application.targetFrameRate = 144;
            GameStaticData.FPSLimit = 1;
            // 前提·分辨力：旧写法（读 `Application.targetFrameRate`）与判据写法在这个取值下**必须不同**
            Tr(SettingsWindow.FpsIndexOfTarget(Application.targetFrameRate) != GameStaticData.FPSLimit,
               "（前提·分辨力）`FpsIndexOfTarget(144)` = " + SettingsWindow.FpsIndexOfTarget(Application.targetFrameRate)
             + " ≠ `GameStaticData.FPSLimit` = " + GameStaticData.FPSLimit + " —— 两种写法在这组取值下**可分辨**"
             + "（否则这条断言证明不了任何事）");

            // ---- ② 关窗：= 原版 `SettingsMenu__Close.c` 第一句 `SavePlayerData()` + 后面的关窗/应用
            win.Close();
            closed = true;
            var saved = LocalPlayerDataStore.ReadDataFromFile();
            Tr(saved != null, "（前提）关窗后临时档根里**有**总档（= 原版 `SettingsMenu.Close` 第一句那一跳真的写了盘）");
            if (saved != null)
                C(saved.fpsLimit, 1, "② ★ 关窗 ⇒ 档里的 `fpsLimit` = **1**（= 关窗那一刻的 `GameStaticData.FPSLimit`；"
                                   + "🧨 把 `Close()` 开头那一句 `SavePlayerData()` 删掉 ⇒ 这里读不到档 / 还是上一个值 ⇒ 红）");

            // ---- ③ 重开窗：`Build()` → `BuildGraphicsPage()` 重跑，初值必须读 `GameStaticData.FPSLimit`
            manager.OpenWindow(win);
            closed = false;
            C(win.FpsIndex, 1, "③ ★ 开窗初值 = `GameStaticData.FPSLimit`(1) —— 此时 `Application.targetFrameRate` = "
                             + Application.targetFrameRate + "（反查档 "
                             + SettingsWindow.FpsIndexOfTarget(Application.targetFrameRate) + "）"
                             + "；🧨 改回 `SetFpsIndex(FpsIndexOfTarget(Application.targetFrameRate), false)` ⇒ 摆成档 2 ⇒ 红");
        }
        finally
        {
            if (closed) { try { manager.OpenWindow(win); } catch { } }   // 别把一个关着的窗留给后面的段
            SettingsWindow.VSyncSetterOverride = prevVsync;
            SettingsWindow.ApplyGraphicsChangesOnClose = prevDirty;
            SuperSampling.PersistOverride = prevSsPersist;
            if (SuperSampling.Enabled != ss0) SuperSampling.Set(ss0);    // 还原内存态（`Set` 会置脏位，见外层的既有约定）
            Application.targetFrameRate = f0;
            GameStaticData.FPSLimit = fsl0;
            win.SetFpsIndex(idx0, false);
            SaveCenter.OverrideRoot = prevRoot;
            LocalPlayerDataStore.PersistOverride = prevPersist;
            LocalPlayerDataStore.ResetForTest();
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
        }
    }
}
