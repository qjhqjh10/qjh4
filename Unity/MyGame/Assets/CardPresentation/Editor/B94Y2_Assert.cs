// B94Y2_Assert.cs — 执行代理 B94_Y2（第九十四会话 · 波 1.5）：`FeatureChecker` 的 **4 个挂点接线**断言。
//
// ============================ 它盯的是什么 ============================
// 判据全文 → `资料/完全复刻/报告/B94_Y2.md`；施工单 → `判据/B94_R-B2_无落点_其他.md` 第 8 项。
// 原版 `FeatureChecker.OnEnable` → `FeatureConfig.CheckFeature(featureKey)` → `OnFeatureChecked(bool).Invoke`，
// 挂点用 `SetActive` / `set_interactable` 决定显隐。4 个 key 的**原版节点级真值**（现读 `assetripper_out` 的
// 6 个 prefab/scene，`FeatureChecker` 组件的 `m_GameObject` 父链 + `OnFeatureChecked` 的 target 父链）：
//   · `PlayerLeaderboard` → `RankedEventWindowV2/Ranked Division Info/LeaderboardButton`（target = 自身，`set_interactable`）
//   · `Alliances`         → `Social Submenu Variant/.../Tab Buttons/Alliances Tab Button`（SetActive 自身）
//                         + `MainMenu Warpforge/.../Main Menu Navigation Button - Social`（SetActive 自身）
//                         + `RankedSkirmishLeaderboardPopup/Tab Buttons/Alliances`（SetActive 自身）
//   · `FactionLeaderboard`→ `{Draft,Skirmish,Classic}/Tab Buttons/Armies` 那颗 checker 的 target = `Tab Buttons/Alliances`
//                           （Classic 那份 target = null = no-op）⇒ **关掉 `FactionLeaderboard` ⇒ 隐 `Alliances` 页签**
//   · `FactionRanking`    → `{Draft,Skirmish,Classic}/Ranking Display`（SetActive 自身）
//
// ============================ 本批【实接】的 3 处（可断言）============================
//   1) `Shell/RankedEventWindow.LeaderboardButtonClick()`  ← `PlayerLeaderboard`
//   2) `Shell/LeaderboardWindow.BuildTabs()` 的 `Alliances` 页签 ← `Alliances && FactionLeaderboard`
//   3) `Shell/LeaderboardWindow.BuildPopup()` 的 `Ranking Display` ← `FactionRanking`
//   4) `Shell/MainMenuRuntime.NavItemShown(4)`（SOCIAL 导航钮）   ← `Alliances`
//   5) `Shell/SocialWindow.Build()` 的 `Alliances` 页签钮        ← `Alliances`
//   ⚠️ **`Shell/SocialWindow` 的 `Alliances` 页签**（原版 `Alliances Tab Button`）本批已接（Z1 交回后由本代理落）——
//      判据 = 该窗 `SocialTabButton_0` 节点被 `FeatureChecker(Alliances).SetActive` 控。
//
// ============================ 怎么用（宿主）============================
//   `B94Y2_Assert.Run();`
//   宿主里两行：`_sink.Pass += B94Y2_Assert.Pass; _sink.Fail += B94Y2_Assert.Fail;`
//   建议宿主 = `Editor/MainMenuScene.cs`（C 段本就开 RankedEventWindow / LeaderboardWindow / SocialWindow）。
//   · **不跑 Unity 批处理**、不建持久资产；本文件临时建的窗口 GameObject 在 `finally` 里 `DestroyImmediate`。
//
// ============================ 🔴 灭自证（改坏法写在文案里）============================
//   · Y1a：删掉 `LeaderboardButtonClick` 开头那句 guard ⇒ `LeaderboardBlockedForTest` 不再 +1 ⇒ 红。
//   · Y2*：把 `BuildTabs` 里那句 `Alliances` 页签的 `SetActive(...)` 删掉 ⇒ OFF 态页签仍 active ⇒ 红。
//   · Y3*：把 `BuildPopup` 里 `Ranking Display` 的 `SetActive(...)` 删掉 ⇒ OFF 态面板仍 active ⇒ 红。
//   · Y4 ：把 `NavItemShown` 里 `FeatureChecker.IsEnabled(FeatureChecker.Alliances)` 删掉 ⇒ 源码结构条红。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 Y2：`FeatureChecker` 4 key 的挂点接线（PlayerLeaderboard / Alliances / FactionLeaderboard / FactionRanking）。</summary>
public static class B94Y2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[Y2] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_Y2 · FeatureChecker 4 key → 挂点接线");
        try
        {
            SectionPremise();
            SectionRankedPlayerLeaderboard();
            SectionLeaderboardTabsAndPanel();
            SectionMainMenuSocialNav();
            SectionSocialWindow();
        }
        finally
        {
            FeatureChecker.ResetForTest();          // 收尾复位（别把覆写漏给后面的断言）
        }
        Debug.Log(_sink.P + "B94Y2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ 0 前提：4 个 key 确实存在
    static void SectionPremise()
    {
        FeatureChecker.ResetForTest();
        C(FeatureChecker.KnownKeys.Length, 4, "Y0 前提：资产里只读出 4 个不同 key（`FeatureChecker.KnownKeys`）");
        Tr(Array.IndexOf(FeatureChecker.KnownKeys, FeatureChecker.PlayerLeaderboard) >= 0, "Y0b 前提：`PlayerLeaderboard` 在 KnownKeys");
        Tr(Array.IndexOf(FeatureChecker.KnownKeys, FeatureChecker.Alliances) >= 0, "Y0c 前提：`Alliances` 在 KnownKeys");
        Tr(Array.IndexOf(FeatureChecker.KnownKeys, FeatureChecker.FactionLeaderboard) >= 0, "Y0d 前提：`FactionLeaderboard` 在 KnownKeys");
        Tr(Array.IndexOf(FeatureChecker.KnownKeys, FeatureChecker.FactionRanking) >= 0, "Y0e 前提：`FactionRanking` 在 KnownKeys");
    }

    // ------------------------------------------------------------ 1 `RankedEventWindow.LeaderboardButtonClick` ← PlayerLeaderboard
    static void SectionRankedPlayerLeaderboard()
    {
        MenuCheck.Section(_sink, "Y1 `RankedEventWindow.LeaderboardButtonClick` ← `PlayerLeaderboard`（原版 `set_interactable`）");

        RankedEventWindow rk = null;
        try
        {
            rk = RankedEventWindow.Create(null);
            Tr(rk != null, "Y1 前提：`RankedEventWindow` 建得出来（`Create(null)`，自检同款）");
            if (rk == null) return;
            rk.Open();

            // OFF 态：点榜钮**早退**（counter +1）——行为面两态里「关」的那一半
            FeatureChecker.SetEnabledForTest(FeatureChecker.PlayerLeaderboard, false);
            int b0 = rk.LeaderboardBlockedForTest;
            rk.LeaderboardButtonClick();
            C(rk.LeaderboardBlockedForTest, b0 + 1,
              "Y1a ★ `PlayerLeaderboard` 关 ⇒ 点榜钮**早退一次**（原版 `set_interactable(false)` ⇒ 点不动）"
            + "｜🧨 删掉 guard ⇒ 计数不变 ⇒ 红");
            FeatureChecker.SetEnabledForTest(FeatureChecker.PlayerLeaderboard, true);

            // 「开」的那一半：guard 条件是 `!IsEnabled(PlayerLeaderboard)` ⇒ 开时不满足 ⇒ 不早退。
            //   ⛔ 本句**不真去开榜**（那会把 WindowsManager / 一扇榜留在宿主里干扰后面的断言）；
            //   改用**源码结构**断言：`LeaderboardButtonClick` 体内确实按 `PlayerLeaderboard` 分叉。
            string src = ReadShellSource("RankedEventWindow.cs");
            Tr(src != null, "Y1b 前提：读得到 `Shell/RankedEventWindow.cs` 源码");
            if (src != null)
            {
                string body = SliceMethod(src, "public void LeaderboardButtonClick()");
                Tr(body != null, "Y1c 前提：定位到 `LeaderboardButtonClick()` 方法体");
                Tr(body != null && body.IndexOf("FeatureChecker.IsEnabled(FeatureChecker.PlayerLeaderboard)", StringComparison.Ordinal) >= 0,
                   "Y1d ★ 两态另一半：`LeaderboardButtonClick` 按 `FeatureChecker.IsEnabled(PlayerLeaderboard)` 分叉"
                 + "（开 ⇒ 不满足 `!IsEnabled` ⇒ 不进早退）｜🧨 删掉这句 ⇒ 红");
            }
        }
        catch (Exception e)
        {
            Tr(false, "Y1 段异常（算红，⛔ 不是跳过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (rk != null) UnityEngine.Object.DestroyImmediate(rk.gameObject);
        }
    }

    // ------------------------------------------------------------ 2 / 3 `LeaderboardWindow` ← Alliances·FactionLeaderboard·FactionRanking
    static void SectionLeaderboardTabsAndPanel()
    {
        MenuCheck.Section(_sink, "Y2/Y3 `LeaderboardWindow`（遭遇榜）页签 `Alliances` 与面板 `Ranking Display` 的显隐");

        LeaderboardWindow lb = null;
        try
        {
            lb = LeaderboardWindow.Create(null, LeaderboardKind.Skirmish);
            Tr(lb != null, "Y2 前提：`LeaderboardWindow`（Skirmish）建得出来");
            if (lb == null) return;
            lb.Open();

            // 前提：这两颗节点在（Skirmish 有 Player/Armies/Alliances 三个页签 + Ranking Display 面板）
            var tab = lb.transform.Find("Tab Buttons/Alliances");
            var panel = lb.transform.Find("Ranking Display");
            Tr(tab != null, "Y2a 前提：`Tab Buttons/Alliances` 页签节点在（Skirmish 有这个页签）");
            Tr(panel != null, "Y2b 前提：`Ranking Display` 面板节点在");
            if (tab == null || panel == null) return;

            // 全开（生产态）：都显示
            C(tab.gameObject.activeSelf, true, "Y2c ★ 全开 ⇒ `Alliances` 页签**显示**（生产恒走这一态）");
            C(panel.gameObject.activeSelf, true, "Y3c ★ 全开 ⇒ `Ranking Display` 面板**显示**");

            // OFF: Alliances
            FeatureChecker.SetEnabledForTest(FeatureChecker.Alliances, false);
            lb.RebuildForTest();
            tab = lb.transform.Find("Tab Buttons/Alliances");
            Tr(tab != null, "Y2d 前提：重建后仍取得到 `Alliances` 页签");
            if (tab == null) return;
            C(tab.gameObject.activeSelf, false,
              "Y2e ★ `Alliances` 关 ⇒ `Alliances` 页签**隐藏**（原版 `Tab Buttons/Alliances` 的 `SetActive`）"
            + "｜🧨 删掉 `BuildTabs` 那句 SetActive ⇒ 仍 active ⇒ 红");
            FeatureChecker.SetEnabledForTest(FeatureChecker.Alliances, true);

            // OFF: FactionLeaderboard（同样隐 Alliances 页签；其 checker 挂在 Armies 上、target = Alliances）
            FeatureChecker.SetEnabledForTest(FeatureChecker.FactionLeaderboard, false);
            lb.RebuildForTest();
            tab = lb.transform.Find("Tab Buttons/Alliances");
            C(tab != null && tab.gameObject.activeSelf, false,
              "Y2f ★ `FactionLeaderboard` 关 ⇒ `Alliances` 页签也**隐藏**（原版 Draft/Skirmish 那颗 checker 的 target 就是 `Tab Buttons/Alliances`）");
            FeatureChecker.SetEnabledForTest(FeatureChecker.FactionLeaderboard, true);

            // OFF: FactionRanking（隐整个 Ranking Display 面板）
            FeatureChecker.SetEnabledForTest(FeatureChecker.FactionRanking, false);
            lb.RebuildForTest();
            panel = lb.transform.Find("Ranking Display");
            Tr(panel != null, "Y3d 前提：重建后仍取得到 `Ranking Display` 面板");
            if (panel == null) return;
            C(panel.gameObject.activeSelf, false,
              "Y3e ★ `FactionRanking` 关 ⇒ `Ranking Display` 面板**隐藏**（原版三扇榜的 `Ranking Display` 的 `SetActive`）"
            + "｜🧨 删掉 `BuildPopup` 那句 SetActive ⇒ 仍 active ⇒ 红");
            // 而页签此时应仍显示（FactionRanking 只管面板、不管页签）—— 两态交叉证明两条挂点不是同一处
            tab = lb.transform.Find("Tab Buttons/Alliances");
            C(tab != null && tab.gameObject.activeSelf, true,
              "Y3f 两态交叉：`FactionRanking` 关**只隐面板**，`Alliances` 页签仍显示（两条挂点各自独立）");

            // 收尾复位 ⇒ 都回来
            FeatureChecker.ResetForTest();
            lb.RebuildForTest();
            tab = lb.transform.Find("Tab Buttons/Alliances");
            panel = lb.transform.Find("Ranking Display");
            C(tab != null && tab.gameObject.activeSelf, true, "Y2g 收尾复位 ⇒ `Alliances` 页签回显");
            C(panel != null && panel.gameObject.activeSelf, true, "Y3g 收尾复位 ⇒ `Ranking Display` 面板回显");

            // ---- Draft 的 Alliances 页签：原版 Draft **只挂 `FactionLeaderboard`** 那颗 checker ⇒ 不受 `Alliances` key 控 ----
            LeaderboardWindow dr = null;
            try
            {
                dr = LeaderboardWindow.Create(null, LeaderboardKind.Draft);
                Tr(dr != null, "Y5 前提：`LeaderboardWindow`（Draft）建得出来");
                if (dr != null)
                {
                    dr.Open();
                    FeatureChecker.SetEnabledForTest(FeatureChecker.Alliances, false);
                    FeatureChecker.SetEnabledForTest(FeatureChecker.FactionLeaderboard, true);
                    dr.RebuildForTest();
                    var dtab = dr.transform.Find("Tab Buttons/Alliances");
                    Tr(dtab != null, "Y5a 前提：Draft 取得到 `Alliances` 页签");
                    C(dtab != null && dtab.gameObject.activeSelf, true,
                      "Y5b ★ Draft 的 `Alliances` 页签**不受 `Alliances` key 控**（原版 Draft 只挂 `FactionLeaderboard` 那颗 checker）"
                    + "｜🧨 让 Draft 也查 Alliances ⇒ 这里会隐、红");
                    FeatureChecker.SetEnabledForTest(FeatureChecker.FactionLeaderboard, false);
                    dr.RebuildForTest();
                    dtab = dr.transform.Find("Tab Buttons/Alliances");
                    C(dtab != null && dtab.gameObject.activeSelf, false,
                      "Y5c ★ `FactionLeaderboard` 关 ⇒ Draft 的 `Alliances` 页签隐（原版 Draft 那颗 checker 的 target）");
                }
            }
            finally
            {
                FeatureChecker.ResetForTest();
                if (dr != null) UnityEngine.Object.DestroyImmediate(dr.gameObject);
            }
        }
        catch (Exception e)
        {
            Tr(false, "Y2/Y3 段异常（算红，⛔ 不是跳过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (lb != null) UnityEngine.Object.DestroyImmediate(lb.gameObject);
        }
    }

    // ------------------------------------------------------------ 4 `MainMenuRuntime.NavItemShown(4)` ← Alliances（源码结构）
    static void SectionMainMenuSocialNav()
    {
        MenuCheck.Section(_sink, "Y4 `MainMenuRuntime.NavItemShown(4)`（SOCIAL 导航钮）← `Alliances`");

        // 行为面：当前 `OnlyPlayAndCollection = true`（用户 2026-10-24 口径「只留两颗」）⇒ idx≥2 恒隐，
        //   `Alliances` 那一刀被它**遮住**、两态不可分 ⇒ 这一处只能做**源码结构断言**（Z1 那套 `ReadShellSource` 的先例）。
        //   恢复开关（`OnlyPlayAndCollection=false`）翻回来那天，本处的 `Alliances` 门才真正生效。
        C(MainMenuRuntime.NavItemShown(0), true, "Y4a 前提：idx 0（PLAY）恒显示");
        C(MainMenuRuntime.NavItemShown(-1), false, "Y4b 前提：越界 -1 ⇒ false");
        C(MainMenuRuntime.NavItemShown(6), false, "Y4c 前提：越界 6 ⇒ false");

        string src = ReadShellSource("MainMenuRuntime.cs");
        Tr(src != null, "Y4d 前提：读得到 `Shell/MainMenuRuntime.cs` 源码");
        if (src == null) return;
        string body = SliceMethod(src, "public static bool NavItemShown(int idx)");
        Tr(body != null, "Y4e 前提：定位到 `NavItemShown(int idx)` 的方法体");
        if (body == null) return;
        Tr(body.IndexOf("FeatureChecker.IsEnabled(FeatureChecker.Alliances)", StringComparison.Ordinal) >= 0,
           "Y4f ★ `NavItemShown` 体内含 `FeatureChecker.IsEnabled(FeatureChecker.Alliances)`（SOCIAL 纽受 Alliances 门控）"
         + "｜🧨 删掉它 ⇒ 红（判据 = `MainMenu Warpforge.unity` 的 `Main Menu Navigation Button - Social` 被那颗 `FeatureChecker(Alliances).SetActive` 控）");
    }

    // ------------------------------------------------------------ 5 `SocialWindow` 的 Alliances 页签 ← Alliances
    static void SectionSocialWindow()
    {
        MenuCheck.Section(_sink, "Y6 `SocialWindow` 的 `Alliances` 页签钮（`SocialTabButton_0`）← `Alliances`");

        FeatureChecker.ResetForTest();
        SocialWindow sw = null; SocialWindow sw2 = null;
        try
        {
            sw = SocialWindow.Create(null);
            Tr(sw != null, "Y6 前提：`SocialWindow` 建得出来");
            if (sw == null) return;
            sw.Build();                                   // 只建按钮壳（不跑 Open 的页 Setup，避免依赖页数据）
            var tab0 = FindByName(sw.transform, "SocialTabButton_0");
            Tr(tab0 != null, "Y6a 前提：`SocialTabButton_0`（= Alliances 页签根）节点在");
            if (tab0 == null) return;
            C(tab0.gameObject.activeSelf, true, "Y6b ★ 全开 ⇒ `Alliances` 页签钮**显示**（生产恒走这一态）");

            // OFF：另建一扇（避免 Build 不清理旧子件的重入问题）
            FeatureChecker.SetEnabledForTest(FeatureChecker.Alliances, false);
            sw2 = SocialWindow.Create(null);
            Tr(sw2 != null, "Y6c 前提：第二扇 SocialWindow 建得出来");
            if (sw2 != null)
            {
                sw2.Build();
                var tab0b = FindByName(sw2.transform, "SocialTabButton_0");
                C(tab0b != null && tab0b.gameObject.activeSelf, false,
                  "Y6d ★ `Alliances` 关 ⇒ `SocialWindow` 的 `Alliances` 页签钮**隐藏**"
                + "（原版 `Social Submenu Variant/.../Tab Buttons/Alliances Tab Button` 的 `SetActive`）"
                + "｜🧨 删掉 `Build` 里那句 SetActive ⇒ 仍 active ⇒ 红");
            }
        }
        catch (Exception e)
        {
            Tr(false, "Y6 段异常（算红，⛔ 不是跳过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            FeatureChecker.ResetForTest();
            if (sw != null && sw.gameObject != null) UnityEngine.Object.DestroyImmediate(sw.gameObject);
            if (sw2 != null && sw2.gameObject != null) UnityEngine.Object.DestroyImmediate(sw2.gameObject);
        }
    }

    // ============================================================ 工具
    /// <summary>在 <paramref name="root"/> 的**所有后代（含 inactive）**里按名字找一个节点。</summary>
    static Transform FindByName(Transform root, string name)
    {
        if (root == null) return null;
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++) if (all[i] != null && all[i].name == name) return all[i];
        return null;
    }

    static string ReadShellSource(string file)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, "CardPresentation", "Shell", file);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch (Exception e) { Debug.LogError("[Y2] 读源码失败：" + e.Message); return null; }
    }

    /// <summary>取出以 <paramref name="sig"/> 开头的成员**方法体**（从签名到配对的右花括号）。
    /// 找不到返回 null（⛔ 不退回整份源码 —— 那会让「方法没了」也通过）。</summary>
    static string SliceMethod(string src, string sig)
    {
        int i = src.IndexOf(sig, StringComparison.Ordinal);
        if (i < 0) return null;
        int open = src.IndexOf('{', i + sig.Length);
        if (open < 0) return null;
        int depth = 0;
        for (int k = open; k < src.Length; k++)
        {
            if (src[k] == '{') depth++;
            else if (src[k] == '}')
            {
                depth--;
                if (depth == 0) return src.Substring(open, k - open + 1);
            }
        }
        return null;
    }
}
