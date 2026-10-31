// B108_A10_Aud05_Assert.cs — 执行代理 `B108_A10`（2026-10-31）落地的 AUD05 S1 / S2 / S3 断言
//
// ============================ 覆盖（逐条对上 `判据/B108_AUD05_施工单.md` §1·1）============================
//  · `RunNewsletter(SettingsWindow win)` —— **S1**「设置·订阅新闻通讯自动弹窗」：
//      原版 `AccountTab__Refresh.c:369-380`：已登录时
//        `if (!demoData.suscriptionPopupShowed && PlayerDataManager.AllTutorialsCompleted()) TrySubscribeNewsletter();`
//      `TrySubscribeNewsletter`（`out_il2csharp/Assembly-CSharp/AccountTab.cs:298-318` 逐句）：
//        `ShowPopUp("MainMenu/Register/SuscribeNewsletter", 1, true, Cancel, OK)` → `suscriptionPopupShowed = true`
//        → `UploadOnlineDemoData()`（P3 ⇒ 我们只出声）。
//      ⇒ 断言四态：① 未登录不弹 · ② 已弹过不弹 · ③ 教程没全通不弹 · ④ 三条件齐 ⇒ 弹一次，
//        且 ⑤ 再刷新**不叠第二扇**（灭自证：没置位的话 `ShowMessagePopUp` 会销毁旧的建新的 ⇒ 引用变）。
//  · `RunGraphicsApplyOnClose(SettingsWindow win)` —— **S2**「Vsync / FPS 生效时机 = 关窗」：
//      原版 `GraphicsTab__VSyncToggleClick.c` / `__FPSLimitValueChanged.c` **只写 `GameStaticData` + 置脏位**
//      `[baseWindow+0xc0]=1`；真应用在 `SettingsMenu__Close.c` → `QualitySettingsManager__ApplyGraphicsQuality.c`
//      （`QualitySettings.vSyncCount`）→ `PlayerDataManager__ApplyGraphicsQuality.c`（`SetTargetFramerate` 0→30/1→60/2→−1）。
//      ⇒ 断言：点完**不当场写** `QualitySettings.vSyncCount` / `Application.targetFrameRate`，只置脏位；
//        `ApplyGraphicsQualityOnClose()` 之后才真写。**灭自证**：VSync 连点两下必须回到原值
//        （若还在读 `QualitySettings.vSyncCount` 取反 ⇒ 第二下算出同一个目标、回不去）。
//  · `RunModeViewport()` —— **S3**「主菜单模式卡视口左右缘随可见宽」：
//      原版 `GameModes/Viewport` 左缘 = **0.087 × 画布宽**、右缘 = **画布右缘**（AUD05 主菜单 §4·4④：
//      16:9 ⇒ 167.04/1920 · 21:9 ⇒ 219.24/2520）；`y1=71.33 / 高 1080` **留 16:9 档**（非 16:9 纵向锚点
//      **没有判据** → J5）。另：`CardCol0` 的左内缩 `pad L38`（实测 37.96）按 `LayoutSpace.Scale` 缩。
//
// ============================ 口径（⛔ 别拿我们自己的常量证明我们自己的实现）============================
//  · 期望值全部来自**原版判据**（反编译方法体 / AUD05 矩阵），逐条写进用例文案。
//  · 前提断言与结论断言**分开**：⛔ 没有 `if (x != null) Check(...)` 那种静默跳过。
//  · 每条 ★ 结论句都带 🧨「改回旧写法时哪条会红」。
//  · 两段都会**备份 / 还原**玩家真数据（账号态 · DemoData · `GameStaticData` · `Application.targetFrameRate`）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  · `Editor/SettingsScene.cs`（**调度台**）：
//        B108A10_Assert.RunNewsletter(win); _sink.Pass += B108A10_Assert.Pass; _sink.Fail += B108A10_Assert.Fail;
//        B108A10_Assert.RunGraphicsApplyOnClose(win); _sink.Pass += B108A10_Assert.Pass; _sink.Fail += B108A10_Assert.Fail;
//    ⚠️ **先改夹具**：`RefreshAccount()` 现在会在「已登录 + 教程全通 + 未弹过」时弹一扇窗
//       ⇒ 现有夹具（若已登录）要先 `DemoDataStore.Current.suscriptionPopupShowed = true;`，否则会多一扇弹窗打断后面各段。
//    ⚠️ 现有断言 `Editor/SettingsScene.cs:2236-2293`（点完当刻读 `targetFrameRate`）与 `:2008-2013`
//       （`VSyncSetterOverride` 抓 `vAsked`）**会红** —— 那是旧口径，改成「读脏位 + 关窗后再读值」。
//  · `Editor/MainMenuScene.cs`（**调度台**）：
//        B108A10_Assert.RunModeViewport(); _sink.Pass += B108A10_Assert.Pass; _sink.Fail += B108A10_Assert.Fail;
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B108_A10`：AUD05 S1（订阅新闻通讯弹窗）/ S2（Vsync·FPS 关窗才生效）/ S3（模式卡视口左右缘）的断言。</summary>
public static class B108A10_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B108A10] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ============================================================ 入口

    /// <summary>S1：要一具**建好的** `SettingsWindow`（`RefreshAccount()` 是它的实例方法）＋ 活的 `WindowsManager`。</summary>
    public static void RunNewsletter(SettingsWindow win)
    {
        MenuCheck.Section(_sink, "B108_A10 · AUD05 S1（设置·订阅新闻通讯自动弹窗）");
        try { A_Newsletter(win); }
        catch (Exception e) { Tr(false, "「订阅新闻通讯弹窗」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B108A10（S1）合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>S2：要一具**建好的** `SettingsWindow`（`SetFpsIndex` / `ToggleVsync` 都是它的成员）。</summary>
    public static void RunGraphicsApplyOnClose(SettingsWindow win)
    {
        MenuCheck.Section(_sink, "B108_A10 · AUD05 S2（Vsync / FPS：只写值 + 脏位，**关窗**才应用）");
        try { B_GraphicsApplyOnClose(win); }
        catch (Exception e) { Tr(false, "「Vsync / FPS 生效时机」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B108A10（S2）合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>S3：自包含（自己造一台相机注入 `LayoutSpace.Cam`，跑完还原）。</summary>
    public static void RunModeViewport()
    {
        MenuCheck.Section(_sink, "B108_A10 · AUD05 S3（模式卡视口左右缘随可见宽；y/高留 16:9 档）");
        try { C_ModeViewport(); }
        catch (Exception e) { Tr(false, "「模式卡视口」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B108A10（S3）合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ S1

    static void A_Newsletter(SettingsWindow win)
    {
        Tr(win != null, "（前提）`SettingsWindow` 实例在（S1 要在真窗上调 `RefreshAccount()`）");
        if (win == null) return;
        var wm = WindowsManager.Instance;
        Tr(wm != null, "（前提）`WindowsManager.Instance` 在（弹窗要经 `ShowMessagePopUp` 开）");
        if (wm == null) return;

        bool wasReg = SettingsWindow.AccountState.Registered;
        string wasMail = SettingsWindow.AccountState.Email;
        bool wasPersist = SettingsWindow.AccountState.PersistOverride;
        var demo = DemoDataStore.Current;
        bool wasShown = demo.suscriptionPopupShowed;
        var wasTut = demo.tutorialsCompleted == null
            ? new Dictionary<string, bool>()
            : new Dictionary<string, bool>(demo.tutorialsCompleted);
        try
        {
            SettingsWindow.AccountState.PersistOverride = true;    // 自检不动盘

            // ① 未登录 ⇒ 不弹（原版那一支整段在 `flag2`（已登录）里面）
            SettingsWindow.AccountState.SignOut();
            demo.suscriptionPopupShowed = false;
            SetAllTutorials(true);
            var popBefore = wm.popUpWindow;
            win.RefreshAccount();
            Tr(ReferenceEquals(wm.popUpWindow, popBefore),
               "① 未登录 ⇒ **不弹**订阅新闻通讯弹窗（原版 `AccountTab.Refresh` 那一支只在登录态里）");

            // ② 已登录 + 已弹过 ⇒ 不弹
            SettingsWindow.AccountState.SignIn("b108a10@example.invalid");
            demo.suscriptionPopupShowed = true;
            popBefore = wm.popUpWindow;
            win.RefreshAccount();
            Tr(ReferenceEquals(wm.popUpWindow, popBefore),
               "② 已登录但 `suscriptionPopupShowed = true` ⇒ 不弹（原版 `!demoData.suscriptionPopupShowed` 那道与门）");

            // ③ 已登录 + 未弹过 + 教程**没全通** ⇒ 不弹
            demo.suscriptionPopupShowed = false;
            SetAllTutorials(false);
            Tr(!MenuTutorialFlow.AllTutorialsCompleted(), "（前提）夹具置完 ⇒ `AllTutorialsCompleted()` = 假");
            popBefore = wm.popUpWindow;
            win.RefreshAccount();
            Tr(ReferenceEquals(wm.popUpWindow, popBefore),
               "③ 教程没全通 ⇒ 不弹（原版 `PlayerDataManager.AllTutorialsCompleted()` 那道与门；"
             + "🧨 删掉这个条件 ⇒ 这条红）");

            // ④ 已登录 + 未弹过 + 教程全通 ⇒ 弹一扇
            SetAllTutorials(true);
            Tr(MenuTutorialFlow.AllTutorialsCompleted(), "（前提）夹具置完 ⇒ `AllTutorialsCompleted()` = 真");
            win.RefreshAccount();
            var pop = wm.popUpWindow as PopUpGameWindow;
            Tr(pop != null, "④ 三条件齐 ⇒ **弹出订阅新闻通讯弹窗**（原版 `TrySubscribeNewsletter()` 那一跳）");
            if (pop != null)
            {
                C(pop.MessageKey, "MainMenu/Register/SuscribeNewsletter",
                  "④ 正文键 = 官方 I2 表的原键 `MainMenu/Register/SuscribeNewsletter`"
                + "（⚠️ `Suscribe` 是**原版自己的笔误**，⛔ 别「顺手修正」成 `Subscribe`）");
                C(pop.PrimaryKey, "MainMenu/General/Cancel",
                  "④ 左钮 = `Cancel`（原版 `GameWindowButton(\"MainMenu/General/Cancel\", …)` 作 primaryButton）");
                C(pop.SecondaryKey, "MainMenu/General/OK",
                  "④ 右钮 = `OK`（原版 `GameWindowButton(\"MainMenu/General/OK\", OnSuscribe)` 作 secondButton）");
                Tr(pop.HasSecondary, "④ 两钮那一档（原版 `ShowPopUp` 传了两粒 `GameWindowButton`）");
                string official = Loc.OfficialCurrent("MainMenu/Register/SuscribeNewsletter");
                if (official != null)
                    C(pop.MessageShown, official,
                      "④ 画出来的正文 = 官方 I2 表该键的**当前语档**文本（⛔ 不是拿 `MessageKey` 冒充渲染结果）");
            }
            Tr(demo.suscriptionPopupShowed, "④ 弹完 ⇒ `suscriptionPopupShowed` 置真（原版 `TrySubscribeNewsletter` 里那句）");

            // ⑤ 再刷新 ⇒ 不叠第二扇
            var popNow = wm.popUpWindow;
            win.RefreshAccount();
            Tr(ReferenceEquals(wm.popUpWindow, popNow),
               "⑤ 再刷新 ⇒ **不弹第二扇**（`suscriptionPopupShowed` 挡住；🧨 若那句置位被删，"
             + "`ShowMessagePopUp` 会「销毁旧的、新建一具」⇒ 引用变 ⇒ 这条红）");
        }
        finally
        {
            if ((wm.popUpWindow as PopUpGameWindow) != null) wm.HidePopUp(true);
            demo.suscriptionPopupShowed = wasShown;
            demo.tutorialsCompleted = wasTut;
            DemoDataStore.Save();                                   // 把备份写回去（覆盖本节改过的教程位）
            SettingsWindow.AccountState.RestoreForTest(wasReg, wasMail);
            SettingsWindow.AccountState.PersistOverride = wasPersist;
        }
    }

    /// <summary>把所有教程阶段完成位一次设成 `done`（键口径 = `MenuTutorialFlow.DoneKey`：`PrefDonePrefix + (StageId | "#" + ClassName)`）。
    /// ⚠️ 只给夹具用；读侧仍走生产口 `MenuTutorialFlow.AllTutorialsCompleted()`（调用前都先断它，防止键口径漂移后静默假绿）。</summary>
    static void SetAllTutorials(bool done)
    {
        int n = MenuTutorialStages.Count;
        for (int i = 0; i < n; i++)
        {
            var d = MenuTutorialStages.At(i);
            if (d == null) continue;
            string k = MenuTutorialFlow.PrefDonePrefix + (string.IsNullOrEmpty(d.StageId) ? "#" + d.ClassName : d.StageId);
            DemoDataStore.SetTutorial(k, done);
        }
    }

    // ============================================================ S2

    static void B_GraphicsApplyOnClose(SettingsWindow win)
    {
        Tr(win != null, "（前提）`SettingsWindow` 实例在（S2 要调它的 `SetFpsIndex` / `ToggleVsync`）");
        if (win == null) return;

        int f0 = Application.targetFrameRate;
        bool vs0 = GameStaticData.vsync;
        int fsl0 = GameStaticData.FPSLimit;
        int idx0 = win.FpsIndex;
        bool dirty0 = SettingsWindow.ApplyGraphicsChangesOnClose;
        bool wasPersist = SuperSampling.PersistOverride;
        int asked = -1;                       // VSyncSetterOverride 记「要求写进去的 vSyncCount」
        try
        {
            SuperSampling.PersistOverride = true;                 // 自检不动盘
            SettingsWindow.VSyncSetterOverride = c => asked = c;   // 把 `QualitySettings.vSyncCount` 的写拦下来观测（别写脏工程资产）
            SettingsWindow.ApplyGraphicsChangesOnClose = false;
            Application.targetFrameRate = 144;                     // 哨兵：与三档（30 / 60 / −1）都不同

            // ① FPS：只写 `GameStaticData.FPSLimit` + 置脏位，**不当场应用**
            win.SetFpsIndex(0, true);
            C(GameStaticData.FPSLimit, 0, "① 选 30 档 ⇒ `GameStaticData.FPSLimit` = 0（原版 `FPSLimitValueChanged.c` 写的就是这一格）");
            Tr(SettingsWindow.ApplyGraphicsChangesOnClose, "① ……而且置了「关窗才生效」脏位（原版 `[baseWindow+0xc0] = 1`）");
            C(Application.targetFrameRate, 144,
              "① ★ **不当场应用**：`Application.targetFrameRate` 仍是 144（🧨 改回立即直写 ⇒ 红；"
            + "原版真写点在 `SettingsMenu.Close → PlayerDataManager.ApplyGraphicsQuality`）");

            // ② 关窗那一跳才真落值
            asked = -1;
            SettingsWindow.ApplyGraphicsQualityOnClose();
            C(Application.targetFrameRate, 30, "② `ApplyGraphicsQualityOnClose()` ⇒ `targetFrameRate` = 30（原版 0→`SetTargetFramerate(0x1e)`）");
            Tr(!SettingsWindow.ApplyGraphicsChangesOnClose, "② ……脏位清 0（原版 `+0xc0 = 0`）");
            C(asked, GameStaticData.vsync ? 1 : 0, "② ……并写了 `QualitySettings.vSyncCount`（经注入口观测 = 原版 `set_vSyncCount` 那一句）");

            // ③ VSync 那颗：只写 `GameStaticData.vsync` + 脏位，**不当场应用**
            SettingsWindow.ApplyGraphicsChangesOnClose = false;
            bool vsBefore = GameStaticData.vsync;
            asked = -1;
            // 「点那颗 `VSync` 行走的就是 `ToggleVsync`」这条**接线**由宿主的 `Click(FindChild(…, "VSync"), "Hit")` 断；
            // 这里只断**方法行为** ⇒ 直接经反射调生产方法（⛔ 不用 `SendMessage`：编辑模式对未激活对象不可靠）。
            var vsMethod = typeof(SettingsWindow).GetMethod("ToggleVsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Tr(vsMethod != null, "（前提）`SettingsWindow.ToggleVsync()`（private）在 —— 下面几条要调它");
            if (vsMethod != null) vsMethod.Invoke(win, null);
            C(GameStaticData.vsync, !vsBefore, "③ 点 VSync ⇒ 翻转 `GameStaticData.vsync`（原版 `VSyncToggleClick.c` 第一句）");
            Tr(SettingsWindow.ApplyGraphicsChangesOnClose, "③ ……置脏位（原版 `VSyncToggleClick.c` 末句）");
            C(asked, -1, "③ ★ **不当场应用**：`QualitySettings.vSyncCount` 没被写（🧨 改回立即 `ApplyVSync` ⇒ 红）");

            // ③·灭自证：再点一下必须回到原值 —— 只有「按 `GameStaticData.vsync` 取反」才做得到
            if (vsMethod != null) vsMethod.Invoke(win, null);
            C(GameStaticData.vsync, vsBefore,
              "③ ★ 再点一下 ⇒ 回到原值（🧨 若还在读 `QualitySettings.vSyncCount` 取反，第二下算出**同一个**目标 ⇒ 回不去 ⇒ 这条红）");

            // ④ 关窗后 VSync 真落值
            asked = -1;
            SettingsWindow.ApplyGraphicsQualityOnClose();
            C(asked, GameStaticData.vsync ? 1 : 0, "④ 关窗 ⇒ 把 `GameStaticData.vsync` 写进 `QualitySettings.vSyncCount`");
        }
        finally
        {
            SettingsWindow.VSyncSetterOverride = null;
            Application.targetFrameRate = f0;
            GameStaticData.vsync = vs0;
            win.SetFpsIndex(idx0, false);                          // 不 fire ⇒ 只摆值、不置脏位
            GameStaticData.FPSLimit = fsl0;
            SettingsWindow.ApplyGraphicsChangesOnClose = dirty0;
            SuperSampling.PersistOverride = wasPersist;
        }
    }

    // ============================================================ S3

    /// <summary>三档宽高比下读 <see cref="MainMenuRuntime.ModeViewportForTest"/>。
    /// ⚠️ 期望值写**字面量**（⛔ 不拿被测属性反算）：16:9 ⇒ 1920/167.04 · 21:9 ⇒ 2520/219.24 · 4:3 ⇒ 1440/125.28。</summary>
    static void C_ModeViewport()
    {
        var cam0 = LayoutSpace.Cam;
        float asp0 = cam0 != null ? cam0.aspect : 0f;
        var probe = new GameObject("B108_A10 Aspect Probe Cam");
        var cam = probe.AddComponent<Camera>();
        try
        {
            LayoutSpace.Cam = cam;

            cam.aspect = 1920f / 1080f;
            var v = MainMenuRuntime.ModeViewportForTest;
            Near(v.x1, 167.04f, 0.05f, "16:9 ⇒ 视口左缘 = 0.087 × 1920 = **167.04**（原版 `GameModes/Viewport`）");
            Near(v.x2, 1920f, 0.05f, "16:9 ⇒ 视口右缘 = 画布右缘 = **1920**");
            Near(v.y1, 71.33f, 0.001f, "y1 仍留 **71.33**（非 16:9 的纵向锚点**没有判据** → AUD05 J5，⛔ 别按比例推）");
            Near(v.y2, 1080f, 0.001f, "y2 / 高 仍留 **1080**（同上，J5）");
            Near(MainMenuRuntime.ModeCardCol0ForTest, 205f, 0.05f,
                 "16:9 ⇒ 第 0 列左沿 = 167.04 + 左内缩 37.96 = **205**（与原常量同值 —— 16:9 档画面零变化）");

            cam.aspect = 21f / 9f;
            v = MainMenuRuntime.ModeViewportForTest;
            Near(v.x1, 219.24f, 0.1f, "21:9 ⇒ 视口左缘 = 0.087 × 2520 = **219.24**（判据 = AUD05 主菜单 §4·4④ 给的期望值）");
            Near(v.x2, 2520f, 0.1f, "21:9 ⇒ 视口右缘 = **2520**（= 画布右缘；🧨 仍是 `const 1920` ⇒ 右边空 433px 且这条红）");
            Near(v.y1, 71.33f, 0.001f, "21:9 ⇒ y1 **不变**（J5：没有纵向判据）");
            Near(MainMenuRuntime.ModeCardCol0ForTest, 219.24f + 37.96f, 0.1f,
                 "21:9 ⇒ 第 0 列左沿 = 新视口左缘 + 左内缩（`Scale` = 1 ⇒ 不缩）= **257.20**");
            Near(MainMenuRuntime.ModeContentX2ForTest, 257.20f + 3f * 555f + 535f, 0.2f,
                 "21:9 ⇒ 内容右端 = 257.20 + 3×555 + 535 = **2457.20**（列宽/步进不变，只有起点跟视口走）");

            cam.aspect = 4f / 3f;
            v = MainMenuRuntime.ModeViewportForTest;
            Near(v.x1, 125.28f, 0.1f, "4:3 ⇒ 视口左缘 = 0.087 × 1440 = **125.28**");
            Near(v.x2, 1440f, 0.1f, "4:3 ⇒ 视口右缘 = **1440**（🧨 仍是 1920 ⇒ 越界 480px）");
            Near(MainMenuRuntime.ModeCardCol0ForTest, 125.28f + 37.96f * 0.75f, 0.1f,
                 "4:3 ⇒ 左内缩按 `LayoutSpace.Scale` = 0.75 缩（37.96 × 0.75 = 28.47）⇒ 第 0 列左沿 = **153.75**");
        }
        finally
        {
            LayoutSpace.Cam = cam0;
            if (cam0 != null) cam0.aspect = asp0;
            UnityEngine.Object.DestroyImmediate(probe);
        }
    }
}
