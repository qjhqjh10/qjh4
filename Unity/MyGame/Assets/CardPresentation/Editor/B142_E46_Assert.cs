// B142_E46_Assert.cs — 执行写手 `B142-E46`（第一百二十九会话 · 波次 `B142`）的断言
//   盯的是本批四处「我们自己编的 / 我们自己加的」东西：
//     A `B142-TIMER-TOKENS`       —— `TimerDisplay` 的三个 `_Only` 格式串（原来是机械拼的）
//     B `B142-NAVTIMER-EXTRA`     —— `NavTimerText` 到点 `"0m"` + 按秒泵不关节点
//     C `B142-NAVTIMER-EXTRA` ②   —— `EphemeralState.FormatTime` 多夹了一句负数
//     D `B142-DROPDOWN-LANGROWS`  —— 语言下拉重建前不清 `_langRows/_btnLangRows`
//
// ============================ 判据（逐行现读 2026-11-04）============================
//   A · **I2 键的真身**：`d:/2/tools/il2cpp_out/script.json` 的 `ScriptString` 表按 VA 反查
//       （VA = RVA + ImageBase 0x180000000）：
//         `DAT_1842c1c10`(0x42C1C10) = `MainMenu/Time/D_Only`
//         `DAT_1842c2010`(0x42C2010) = `MainMenu/Time/H_Only`
//         `DAT_1842c2310`(0x42C2310) = `MainMenu/Time/M_Only`
//       （= `decomp_full/SupportMethods__GetTimeString.c` 那三处 `GetTermTranslation` 的实参 token）
//       官方值（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 逐字）：
//         D_Only  en `{0:0}d`      zh `{0:0}天`
//         H_Only  en `{0:0}h`      zh `{0:0}小时`
//         M_Only  en `{0:0}m`      zh **`{0:0}分`**（⚠️ 不是「分钟」—— 我们原来机械拼成「分钟」是错的）
//   B · `decomp_full/TimerDisplay__UpdateTimer.c` 的 `remain <= 0` 那一支 = `text = ""` → `gameObject.SetActive(false)`
//       ⇒ 到点**清字 + 关节点**，⛔ 不是我们原来那个 `"0m"` 占位。
//   C · `decomp_full/EphemeralState__FormatTime.c` = `GetTimeString(ts, 1, 0, 0, 0)`（`clampTo0 = 0`）⇒ **不夹负值**。
//       ⚠️ 如实记：`EphemeralState` 的 `EphemeralEndTime` 在 `DurationMs <= 0` 时回 `DateTime.MaxValue`
//       ⇒ **负 ts 结构性不可达**（今天也全仓 0 生产调用点）⇒ C 只做源码级断言，不假造一个负值用例。
//   D · 原版 `TMP_Dropdown.Show()` 每次 `Instantiate(Template)` 重建列表；我们的等价件在
//       `_langList` 被销毁后（Unity 假 null）会再建一次 ⇒ 不清表就**行数翻倍**
//       （`LangRowCount` / `LangRowBg(i)` 全指到已销毁件）。
//
// ============================ 🔴 灭自证（🧨 什么坏法会让哪条红）============================
//  · **A1/A2/A3** 的期望值写的是**官方表原文**（不是把 `Loc.T` 再念一遍）⇒ 🧨 把 `FmtMOnly` 改回
//    机械拼的 `{0:0}分钟` ⇒ A1 红（`分` ≠ `分钟`）。
//  · **B1** 断「到点 = 空串」、**B1b** 断「未到点 ≠ 空串」⇒ 两条**结构上不可能同时成立**（🧨 恒空/恒 `"0m"` 都会各红一条）。
//  · **B2** 断「泵把节点关了」、**B3** 断「未到点的节点仍开着」⇒ 🧨 把 `SetActive` 那句删掉 ⇒ B2 红；
//    🧨 改成「无条件关」⇒ B3 红。
//  · **D1** 断源码里真有那两句 `Clear()`；**D2** 断 `withRows=false`（`Template` 原型）那一档被 `if (withRows)` 挡住。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主里加：
//      B142_E46_Assert.Run(); pass += B142_E46_Assert.Pass; fail += B142_E46_Assert.Fail;
//  推荐宿主 = `Editor/ShellScene.cs`（9 s，最便宜；`B142_E25` / `B142_E34` 也接在那儿）。
//  ⚠️ B2/B3 会在**临时 GameObject** 上建一颗 `Timer Text` 并登记进按秒泵，收尾 `DestroyImmediate`
//     （泵里那条登记会在下一次推进时按 `Button == null` 自己摘掉）。
//  判绿红：按行首 `B142E46 ✓` / `B142E46 ✗` 数（⛔ 别 `grep -c ✗`）。

using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B142-E46`：`_Only` 三个 I2 键 · 倒计时到点清字 + 关节点 · 负值不夹 · 下拉表重建前清空。</summary>
public static class B142_E46_Assert
{
    const string Tag = "B142E46";
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/CardPresentation/" + rel); }
        catch (Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_OnlyTokens();
        GroupB_NavTimerExpiry();
        GroupC_EphemeralNoClamp();
        GroupD_LangRowsClear();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 三个 `_Only` token = 官方 I2 键
    // ==================================================================
    static void GroupA_OnlyTokens()
    {
        bool zh = Loc.Current == AvailableLanguages.Chinese;
        string dOnly = zh ? "{0:0}天" : "{0:0}d";
        string hOnly = zh ? "{0:0}小时" : "{0:0}h";
        string mOnly = zh ? "{0:0}分" : "{0:0}m";     // ⚠️ 官方就是「分」，⛔ 不是「分钟」

        Check(Loc.T("MainMenu/Time/D_Only") == dOnly,
              "A0a 官方表 `MainMenu/Time/D_Only` = `" + dOnly + "`｜实得 [" + Loc.T("MainMenu/Time/D_Only") + "]");
        Check(Loc.T("MainMenu/Time/H_Only") == hOnly,
              "A0b 官方表 `MainMenu/Time/H_Only` = `" + hOnly + "`｜实得 [" + Loc.T("MainMenu/Time/H_Only") + "]");
        Check(Loc.T("MainMenu/Time/M_Only") == mOnly,
              "A0c ★ 官方表 `MainMenu/Time/M_Only` = `" + mOnly + "`（⛔ 不是机械拼的「分钟」）｜实得 ["
            + Loc.T("MainMenu/Time/M_Only") + "]");

        // 三档取值路径（逐档对分支，⛔ 不是「转发到 Loc.T」那种同义反复）
        string gotD = TimerDisplay.FormatTimeString(new TimeSpan(3, 0, 0, 0), eventTimer: true, hide0Values: true);
        Check(gotD == string.Format(dOnly, 3),
              "A1 3d0h + `hide0Values` ⇒ 天档 `D_Only(3)`｜got=[" + gotD + "]");
        string gotH = TimerDisplay.FormatTimeString(TimeSpan.FromHours(5), eventTimer: true);
        Check(gotH == string.Format(hOnly, 5),
              "A2 5h0m（`Minutes == 0`）⇒ 小时档 `H_Only(5)`｜got=[" + gotH + "]");
        string gotM = TimerDisplay.FormatTimeString(TimeSpan.FromMinutes(5), eventTimer: true, hide0Values: true);
        Check(gotM == string.Format(mOnly, 5),
              "A3 5m0s + `hide0Values` ⇒ 分钟档 `M_Only(5)`｜got=[" + gotM + "]");

        // 源码：三条不再走 `Zh ? … : …` 的机械拼
        var td = Src("Shell/TimerDisplay.cs");
        Check(td.Contains("FmtDOnly() { return Loc.T(\"MainMenu/Time/D_Only\"); }")
              && td.Contains("FmtHOnly() { return Loc.T(\"MainMenu/Time/H_Only\"); }")
              && td.Contains("FmtMOnly() { return Loc.T(\"MainMenu/Time/M_Only\"); }"),
              "A4 源码：三个 `Fmt*Only()` 已改成取官方 I2 键（不再是机械拼的 `Zh ? … : …`）");
        Check(!td.Contains("\"{0:0}分钟\"; }"),
              "A4b ★ 源码里那个机械拼的 `\"{0:0}分钟\"` 已经不在（🧨 改回去 ⇒ 本红）");
    }

    // ==================================================================
    //  B · 到点 = 清字 + 关节点
    // ==================================================================
    static void GroupB_NavTimerExpiry()
    {
        string past = MainMenuRuntime.NavTimerTextForTest(DateTime.UtcNow.AddMinutes(-1));
        Check(past == "",
              "B1 ★ 已到点 ⇒ **空串**（原版 `TimerDisplay__UpdateTimer.c` 的 `text=\"\"` + `SetActive(false)`）"
            + "｜got=[" + past + "]｜🧨 改回 `\"0m\"` ⇒ 本红");
        string future = MainMenuRuntime.NavTimerTextForTest(DateTime.UtcNow.AddMinutes(95));
        Check(future.Length > 0,
              "B1b（灭自证）未到点 ⇒ 非空串（⛔ 不是恒空）｜got=[" + future + "]");

        // ---- 泵：到点那一秒真的把字清掉 + 把节点关掉 ----
        var expiredBtn = new GameObject("B142E46 Expired Timer Button", typeof(RectTransform));
        var liveBtn = new GameObject("B142E46 Live Timer Button", typeof(RectTransform));
        try
        {
            var expLbl = MenuDraw.Text(expiredBtn.transform, new PxRect(0f, 0f, 200f, 30f), "STALE", Color.white,
                                       "Timer Text", 26.35f, 0);
            var liveLbl = MenuDraw.Text(liveBtn.transform, new PxRect(0f, 0f, 200f, 30f), "STALE", Color.white,
                                        "Timer Text", 26.35f, 0);
            Check(expLbl != null && liveLbl != null,
                  "B2a（前提）两颗 `Timer Text` 都建出来了｜⛔ 建不出就别读下面两条（先红，不静默跳过）");
            if (expLbl != null && liveLbl != null)
            {
                MainMenuRuntime.RegisterEventNavTimerForTest(expiredBtn.transform, DateTime.UtcNow.AddMinutes(-1));
                MainMenuRuntime.RegisterEventNavTimerForTest(liveBtn.transform, DateTime.UtcNow.AddMinutes(95));
                int n = MainMenuRuntime.StepEventNavTimersForTest(1.0f);
                Check(n >= 1, "B2b（前提）按秒泵这一拍真的改写了东西（n = " + n + "）｜⛔ 0 次说明泵没跑，下面两条不算数");

                Check(expLbl.Text == "",
                      "B2 ★ 到点那一拍：字被**清空**（⛔ 不写 `\"0m\"`）｜实得 [" + expLbl.Text + "]");
                Check(!expLbl.gameObject.activeSelf,
                      "B2c ★ 到点那一拍：`Timer Text` 节点被 **`SetActive(false)`**（原版同一支）"
                    + "｜🧨 删掉泵里那句 `SetActive` ⇒ 本红");

                Check(liveLbl.gameObject.activeSelf && liveLbl.Text.Length > 0,
                      "B3 ★ 未到点的那一颗**仍在显示**（灭自证：⛔ 不是「无条件全关」）"
                    + "｜active=" + liveLbl.gameObject.activeSelf + " text=[" + liveLbl.Text + "]");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(expiredBtn);
            UnityEngine.Object.DestroyImmediate(liveBtn);
        }
    }

    // ==================================================================
    //  C · `EphemeralState.FormatTime` 不夹负值（源码级 —— 负 ts 结构性不可达，见文件头 C）
    // ==================================================================
    static void GroupC_EphemeralNoClamp()
    {
        var inv = Src("Shell/Inventory.cs");
        Check(inv.Length > 0, "C0（前提）读得到 `Shell/Inventory.cs`");
        Check(inv.Contains("EphemeralState__FormatTime.c"),
              "C1 `EphemeralState.FormatTime` 的 doc 仍指着 `decomp_full/EphemeralState__FormatTime.c`");
        Check(!inv.Contains("if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;"),
              "C2 ★ 那句「负 ⇒ 夹成 0」已经不在（原版 `GetTimeString(ts,1,0,0,0)` 的 `clampTo0 = 0`）"
            + "｜🧨 加回去 ⇒ 本红");
        Check(inv.Contains("string s = TimerDisplay.FormatTimeString(ts, eventTimer: true);"),
              "C3 那一行转发仍在（C2 删的是夹取，不是转发）");
    }

    // ==================================================================
    //  D · 语言下拉重建前清空 `_langRows / _btnLangRows`
    // ==================================================================
    static void GroupD_LangRowsClear()
    {
        var sp = Src("Battle/SettingsPanel.cs");
        Check(sp.Length > 0, "D0（前提）读得到 `Battle/SettingsPanel.cs`");
        Check(sp.Contains("_langRows.Clear(); _btnLangRows.Clear();"),
              "D1 ★ 重建 `Dropdown List` 那一支里**先清空** `_langRows / _btnLangRows`"
            + "｜🧨 删掉 ⇒ 旧子树被销毁后重开列表会行数翻倍（旧件是 Unity 假 null，判不出来）");
        Check(sp.Contains("if (withRows) { _langRows.Clear(); _btnLangRows.Clear(); }"),
              "D2 ★ 清空被 `withRows` 挡住 —— `Template` 原型（`withRows=false`）那一档不清"
            + "（它与 `Dropdown List` 共用这个函数，`:1724`）");
    }
}
