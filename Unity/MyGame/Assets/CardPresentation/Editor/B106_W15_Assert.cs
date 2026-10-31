// B106_W15_Assert.cs — 执行代理 `B106_W15`（2026-10-31 · 完全复刻 · AUD05 设置窗 5 条状态缺口）
//
// ============================ 它钉的是什么 ============================
// 判据源 = `资料/完全复刻/判据/AUD05_state_matrix_设置窗.md` §4·3（G2 / G3 / G4 / G5 / G6）。
//   G2 `Disable Notifications` 该隐藏（原版 `GeneralTab.OnSetup` 首句）＋ `Touch Input` 随 VLG 上移一格；
//   G3 PC 上 `Delete Button` 该关（原版 `Buttons > PlatformBasedComponents` PC 组 = 0，`Start` 无条件跑 PC 组）；
//   G4 已登录态：密码框 `**********` + 两颗输入框 `interactable = false`（`AccountTab__Refresh.c` 登录支）；
//   G5 小屏字号档：`Text()` 漏斗按 §3·11 逐族传 `(max, min)`（**接线口径**，批处理里不生效 —— 见下）；
//   G6 登录弹窗：长度闸（`email > 4` ∧ `password > 5`）+ 等待态（钮禁用 / `TryingToLogin` / 加载图亮）。
//
// ============================ 怎么用（宿主接线归调度台）============================
//   `Editor/SettingsScene.cs` 里**已经有 `SettingsWindow win`** 的那一段（账号页/General 页断言之后）：
//       B106W15_Assert.Run(win);
//       pass += B106W15_Assert.Pass; fail += B106W15_Assert.Fail;
//   ⚠️ 本文件**会改** `AccountState`（登录/登出）与弹窗状态 ⇒ 跑完在 `finally` 里逐项还原
//      （`AccountState.PersistOverride` 全程 true ⇒ **不落盘**）。
//
// ============================ 🔴 灭自证（🧨 改坏法写在各条文案里）============================
//   · G2：去掉 `BuildGeneralPage` 里 `notifRow.gameObject.SetActive(false)` ⇒ G2-a 红；把它改回第 3 格
//         （`GenChkT + 2f * ChkRowStep`）⇒ G2-b 红（行距变 2 格）。
//   · G3：把 `SetActive(Application.isMobilePlatform)` 改成 `SetActive(true)` ⇒ G3 红（PC 上仍亮）。
//   · G4：把 `_acPwd.SetText(on ? "**********" : "")` 改回 `""` ⇒ G4-a 红；删掉两句 `SetInteractable` ⇒ G4-b 红。
//   · G5：把 `Text()` 里那句 `SetSmallScreenFontSizes` 的**两格参数**去掉（或那一站不传）⇒ G5 红。
//   · G6：把长度闸改回「判非空」⇒ G6-a 红；`LoginWindowSubmit` 里不调 `BeginLoginWaiting`（恢复当场登录）⇒ G6-b 红。
//   · G6 与「当场登录」**结构上不可能同时绿**：等待态断言要求「提交后仍然开着 + 钮不可点」，
//     而当场登录会把窗关掉 ⇒ 两边读数互相排斥。
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B106_W15`：设置窗 5 条状态缺口（AUD05 §4·3 G2/G3/G4/G5/G6）的**驱动断言**。</summary>
public static class B106W15_Assert
{
    static readonly CheckSink _s = new CheckSink("[B106W15] ") { Near = MenuNearStyle.Compact2 };
    public static int Pass { get { return _s.Pass; } }
    public static int Fail { get { return _s.Fail; } }

    static void Eq<X>(X got, X want, string msg) { MenuCheck.Check(_s, got, want, msg); }
    static void Ok(bool c, string msg) { MenuCheck.True(_s, c, msg); }

    /// <summary>`win` = 宿主已经开着的设置窗（`Build()` 已跑完 ⇒ 5 页都在树上）。</summary>
    public static void Run(SettingsWindow win)
    {
        _s.Pass = 0; _s.Fail = 0;
        MenuCheck.Section(_s, "B106_W15：设置窗 5 条状态缺口（AUD05 §4·3 G2/G3/G4/G5/G6）");
        Ok(win != null, "（前提）拿到一扇 `SettingsWindow`");
        if (win == null) return;

        var root = win.transform;
        var gtab = MenuCheck.FindChild(root, "General Tab");
        var acPage = MenuCheck.FindChild(root, "Account Tab");

        // ---------------- ① G2 `Disable Notifications` 该隐藏 + `Touch Input` 上移 ----------------
        MenuCheck.Section(_s, "① G2 `Disable Notifications` 隐藏（原版 `GeneralTab.OnSetup` 首句）");
        var notif = gtab != null ? MenuCheck.FindChild(gtab, "Disable Notifications") : null;
        var bots = gtab != null ? MenuCheck.FindChild(gtab, "Disable Bots") : null;
        var touch = gtab != null ? MenuCheck.FindChild(gtab, "Touch Input") : null;
        Ok(notif != null && bots != null && touch != null,
           "（前提）三行**都照原样建出来了**（铁律 11：不因「用不上」而不建）");
        Ok(notif != null && !notif.gameObject.activeSelf,
           "…而且 `Disable Notifications` **关着**（🧨 去掉 `notifRow.gameObject.SetActive(false)` ⇒ 本条红）");
        if (bots != null && touch != null)
        {
            // `Checkboxes` = VLG（childControlHeight 0 · spacing 5）⇒ 跳过 inactive ⇒ `Touch Input` 落第 2 格。
            // 🔴 2026-10-31（`B106_W75` 修 R6 · 量纲）：`localPosition` 是**设计空间的世界单位**
            //   （`1080 px ↔ 10 单位` ⇒ 108 px/单位，`Core/LayoutSpace.cs:205`）⇒ `画布 px = 540 − world.y×108`。
            //   ⚠️ 但本窗 `Screen()` 先把**原版 px 过了一遍 0.9**（`S(v,c)=c+(v−c)×RootScale`，
            //   `Shell/SettingsWindow.cs:1584`）⇒ 要还原成**原版设计 px** 还得 `画布 px ÷ RootScale`
            //   （同一口径 = `Editor/SettingsScene.cs:126 OrigPxY`）。
            //   旧写法只 `/ RootScale`、没乘 108 ⇒ 0.672/0.9 = 0.7467 ⇒ `Round 1`（= 日志「实得 1」）；
            //   ⚠️ **只补 `× 108` 也不对** —— 那得 72.58 ⇒ `Round 73`，与 `ChkRowStep` 81 仍不等。两样都要。
            float dyPx = Mathf.Abs(LayoutSpace.PxY(bots.localPosition.y) - LayoutSpace.PxY(touch.localPosition.y));  // 画布 px（已含 0.9 缩放）
            float dyDesign = dyPx / SettingsWindow.RootScale;                                                        // ⇒ 原版设计 px
            Eq((int)Mathf.Round(dyDesign), (int)Mathf.Round(SettingsWindow.ChkRowStep),
               "`Touch Input` 紧挨 `Disable Bots`（设计行距 = `ChkRowStep` 80.641 ⇒ 中间那格由 VLG 跳过；"
             + "🧨 改回 `GenChkT + 2f * ChkRowStep` ⇒ 行距变 2 格、本条红）");
        }

        // ---------------- ② G3 PC 上 `Delete Button` 该关 ----------------
        MenuCheck.Section(_s, "② G3 平台档：PC 上 `Delete Button` 该关（原版 `PlatformBasedComponents` PC 组 = 0）");
        var del = acPage != null ? MenuCheck.FindChild(acPage, "Delete Button") : null;
        Ok(del != null, "（前提）`Delete Button` 照原样建出来（换个平台就该亮 ⇒ ⛔ 不删件）");
        Eq(del != null && del.gameObject.activeSelf, Application.isMobilePlatform,
           "PC ⇒ 关 / 移动端 ⇒ 开（🧨 改回无条件 `SetActive(true)` ⇒ 本条红）");

        // ---------------- ③ G4 已登录态：掩码 + 两颗输入框不可交互 ----------------
        MenuCheck.Section(_s, "③ G4 已登录态（原版 `AccountTab__Refresh.c` 登录支）");
        bool wasReg = SettingsWindow.AccountState.Registered;
        string wasMail = SettingsWindow.AccountState.Email;
        bool wasPersist = SettingsWindow.AccountState.PersistOverride;
        SettingsWindow.AccountState.PersistOverride = true;      // 自检不动盘
        try
        {
            SettingsWindow.AccountState.SignIn("selfcheck@example.invalid");
            win.RefreshAccount();
            Eq(win.AccountPassword != null ? win.AccountPassword.Text : null, "**********",
               "登录后密码框 = **10 颗星**（🧨 改回空串 ⇒ 本条红）");
            Eq(win.AccountEmail != null ? win.AccountEmail.Interactable : true, false, "登录后邮箱框 `interactable = false`");
            Eq(win.AccountPassword != null ? win.AccountPassword.Interactable : true, false, "登录后密码框 `interactable = false`");

            SettingsWindow.AccountState.SignOut();
            win.RefreshAccount();
            Eq(win.AccountPassword != null ? win.AccountPassword.Text : null, "", "登出后密码框清空");
            Eq(win.AccountEmail != null ? win.AccountEmail.Interactable : false, true, "登出后邮箱框可编辑");
            Eq(win.AccountPassword != null ? win.AccountPassword.Interactable : false, true, "登出后密码框可编辑");
        }
        finally
        {
            SettingsWindow.AccountState.RestoreForTest(wasReg, wasMail);
            SettingsWindow.AccountState.PersistOverride = wasPersist;
            win.RefreshAccount();
        }

        // ---------------- ④ G5 小屏字号档：漏斗收到的 (max, min) ----------------
        MenuCheck.Section(_s, "④ G5 小屏字号档接线（§3·11 逐族值；⚠️ 批处理里 `isPlaying=false` ⇒ 只断「传了」）");
        CheckSS("General Tab/Tab Title", 62f, 10f, "各页页标题 `Tab Title` = 62/10");
        CheckSS("General Tab/VersionText", 32f, 5f, "`VersionText` = 32/5");
        CheckSS("General/Tab Toggle Title", 38f, 10f, "页签 `Tab Toggle Title` = 38/10");
        CheckSS("Disable Bots/Label", 50f, 10f, "勾选行 `Label` = 50/10");
        CheckSS("Touch Input/Label", 50f, 10f, "勾选行 `Label` = 50/10（同上）");
        CheckSS("LanguagesDropdown/Label", 45f, 10f, "语言 dropdown caption = 45/10");
        CheckSS("Language Selector/SelectLanguageText", 50f, 10f, "`SelectLanguageText` = 50/10");
        CheckSS("Account Form/EmailText", 47f, 10f, "账号页 `EmailText` = 47/10");
        CheckSS("Account Form/PasswordText", 47f, 10f, "账号页 `PasswordText` = 47/10");
        CheckSS("Account Form/Error Message", 45f, 10f, "账号页 `Error Message` = 45/10");
        CheckSS("Redeem Code/Button Text", 44f, 12f, "大钮 `Button Text` = 44/12");

        // ---------------- ⑤ G6 登录弹窗：长度闸 + 等待态 ----------------
        MenuCheck.Section(_s, "⑤ G6 登录弹窗：长度闸（email>4 ∧ password>5）+ 等待态");
        bool wasReg2 = SettingsWindow.AccountState.Registered;
        string wasMail2 = SettingsWindow.AccountState.Email;
        bool wasPersist2 = SettingsWindow.AccountState.PersistOverride;
        SettingsWindow.AccountState.PersistOverride = true;
        // 🔴 2026-10-31（`B106_W75` 修 R8 · **夹具前提**）：`Login Window` 长在 `Account Tab` 子树里
        //   （`Shell/SettingsWindow.cs:3218`），而 `OpenTab` 只把**当前页**置 active ⇒ 进本段时当前页可能不是账号页
        //   （实测 = `Graphics`：`Editor/B94W2_Assert.cs:225` 切过去没还原）⇒ `Account Tab` 整棵 inactive
        //   ⇒ `activeInHierarchy` 恒假。**生产没问题**（真机上玩家就在账号页）。
        //   ⚠️ 必须排在 `win.ShowLoginWindow()` **之前** —— `OpenTab` 内部会 `HideLoginWindow()`。
        var tabWasG6 = win.Current;
        win.OpenTab(SettingsTab.Account);
        try
        {
            Ok(win.Current == SettingsTab.Account, "（前提）当前页 = 账号页（`Login Window` 长在它的子树里，`activeInHierarchy` 才可能为真）");
            win.ShowLoginWindow();
            Ok(win.LoginWindowOpen, "（前提）登录弹窗打开了（原版 `Switch Account` 那条路）");
            var le = win.LoginWindowEmail;
            var lp = win.LoginWindowPassword;
            Ok(le != null && lp != null, "（前提）弹窗两颗输入框在");
            if (le != null && lp != null)
            {
                le.SetText("abcd"); lp.SetText("12345");          // 4 / 5 —— 两格都差 1
                Eq(win.LoginButtonEnabled, false, "email=4 / password=5 ⇒ 长度闸**不过**（原版 `>4` ∧ `>5`）");
                le.SetText("abcde");                              // 5 —— 过
                Eq(win.LoginButtonEnabled, false, "只补邮箱 ⇒ 仍不过（密码还差一位）");
                lp.SetText("123456");                             // 6 —— 过
                Eq(win.LoginButtonEnabled, true, "email>4 ∧ password>5 ⇒ 可以点");
                Eq(win.LoginButtonInteractableForTest, true, "…那颗 `Login Button` 的 `interactable` 真的跟着开");

                win.LoginWindowSubmit();
                Ok(win.LoginWaitingForTest, "提交 ⇒ 进入等待态（🧨 恢复「当场登录」⇒ 本条红）");
                Eq(win.LoginButtonInteractableForTest, false, "等待态那颗钮不可点");
                Eq(win.LoginWaitingTextForTest, Loc.T("MainMenu/Login/TryingToLogin"),
                   "错误行换成官方 `MainMenu/Login/TryingToLogin`（en `Logging in` / zh `登录中`）");
                Ok(win.LoginLoadingForTest != null && win.LoginLoadingForTest.gameObject.activeInHierarchy,
                   "`Animated Loading Image` 亮着（原版 `+0xa0 SetActive(true)`）");

                win.PumpLoginWaitForTest(5f);                      // 本地模拟：推进 5 秒 ⇒ 收尾
                Ok(!win.LoginWaitingForTest, "推进 5 秒 ⇒ 等待态收尾");
                Ok(!win.LoginWindowOpen, "…按原版 A6 关窗");
                Eq(SettingsWindow.AccountState.Email, "abcde", "…登录态 = 弹窗里那个邮箱");
            }
        }
        finally
        {
            if (win.LoginWindowOpen) win.HideLoginWindow();
            SettingsWindow.AccountState.RestoreForTest(wasReg2, wasMail2);
            SettingsWindow.AccountState.PersistOverride = wasPersist2;
            win.RefreshAccount();
            win.OpenTab(tabWasG6);   // 还原：本段进来之前是哪一页就退回哪一页（B106_W75）
        }
    }

    /// <summary>断「`Text()` 漏斗在 `<paramref name="key"/>` 这一站收到了 (max, min)」。
    /// ⚠️ 这是**接线**口径（批处理里那两格不生效，见 `SettingsWindow.SmallScreenRequestedForTest` 的 doc）。</summary>
    static void CheckSS(string key, float maxPx, float minPx, string what)
    {
        Vector2 got;
        bool has = SettingsWindow.SmallScreenRequestedForTest.TryGetValue(key, out got);
        Ok(has, "（前提）`Text()` 漏斗在 `" + key + "` 这一站**收到过**小屏两格（没收到 = 那一站没接线）");
        if (has)
            Ok(Mathf.Abs(got.x - maxPx) < 0.01f && Mathf.Abs(got.y - minPx) < 0.01f,
               what + "（实得 " + got.x + " / " + got.y + "）");
    }
}
