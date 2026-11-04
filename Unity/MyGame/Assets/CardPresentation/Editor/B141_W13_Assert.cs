// B141_W13_Assert.cs — 执行代理 `B141-W13`（第一百二十八会话 · 波次 `W129`）
// 只做 **`Shell/SettingsWindow.cs` 自己那一份** 的四条（B138 §4）：`4-05` · `4-10` · `4-13` · `4-01`。
//
// ============================ 它盯的是什么 ============================
// ① `4-05` **语言下拉面板高按内容收缩** —— 原版 `TMP_Dropdown.Show()`（本机
//    `Library/PackageCache/com.unity.ugui@…/Runtime/TMP/TMP_Dropdown.cs:926-932`）：
//      `sizeDelta.y = itemSize.y × n + offsetMin.y − offsetMax.y`；
//      `extraSpace = 面板高 − 内容高`；`if (extraSpace > 0) sizeDelta.y -= extraSpace`
//    ⇒ 面板高 = **n × 40.8707 + 0.8519**（12 项 = **491.30**、13 项 = 532.17）。
//    我们以前恒画满高 **573.96**（B138 §4 `4-05` 的差 +82.66）。`Template` 原型仍是 573.96（prefab 序列化值）。
// ② `4-10` **页标题的垂直对齐档** —— 原版 `…/General Tab/Tab Title` 等 5 颗
//    `m_VerticalAlignment = **8192 (Capline)**`（`Main Menu Settings Window.prefab` 逐颗实读）；
//    我们以前只有 `AlignLeft`（水平），档停在 `Label` 出厂档 `Middle`。
// ③ `4-13` **行距 / 字距 / 溢出** —— 原版整窗 102 颗 TMP 里：行距非 0 的**只有 2 颗**
//    （`Support Tab/Faq Text` · `Support Tab/Faq Text Mobile`，`m_lineSpacing = **15**`）；
//    `m_characterSpacing` / `m_overflowMode` **102/102 全是 0**。
// ④ `4-01` **字体面** —— 原版整窗 **102/102 颗 TMP 都指向 `Pragati-Regular SDF`**
//    （guid `2548b731a71f84745a1b13c211c923ca`；`Settings Menu Tab Toggle.prefab` 同值）
//    ⇒ 我们的等价物 = `TmpFont.Face.LatinCardBody`（`FaceResource[2] = "Fonts/Pragati-Regular SDF"`）。
//    🔴 汉字照样回 CJK（`Label.ActualFace` 见 `Loc.HasCjk(文本)` 就翻面）—— 那 102 颗里也含中文文案。
//
// ============================ 🔴 灭自证 ============================
//  · A9   —— 面板高**严格小于**满高 516.56px ⇒ 把实现改回 573.96 立刻红（A7/A8 同时红）。
//  · A2   —— `Template` 原型仍 573.96 ⇒ 「把原型也缩了」这条写法也会红（那**不是**原版的形状）。
//  · B*   —— 改回「只 `AlignLeft`」⇒ 档回到 `Middle` ⇒ B3 红。
//  · C*   —— 把 `SpText` 的 `lineSpacingPx` 拿掉 ⇒ C3 红；同时 C5（邻位对照 `Contact Text` = 0）证明
//           这条**不是**「整窗一律 15」那种假绿（原版确实只有那两颗非 0）。
//  · D3   —— 拿掉 `Text()` 漏斗里的 `face: face` ⇒ 只剩 4 颗直调 `MenuDraw.Text` 的仍是拉丁面 ⇒ 红；
//           `D1` 再钉「`LatinCardBody` 到底是不是 Pragati 那一份资产」（换枚举序也会红）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主以外的文件）============================
//  `Editor/SettingsScene.cs` 的 `RunCore()` 里（A862 语言下拉那一节之后、图像页之前）加：
//      B141_W13_Assert.Run(win); _sink.Pass += B141_W13_Assert.Pass; _sink.Fail += B141_W13_Assert.Fail;
using CardPresentation;
using TMPro;
using UnityEngine;

/// <summary>执行代理 `B141_W13`：设置窗四条 UI 参数（`4-05` / `4-10` / `4-13` / `4-01`）。</summary>
public static class B141_W13_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B141_W13] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }
    /// <summary>失败明细（宿主合并进自己的 `Failures` 用）。</summary>
    public static readonly System.Collections.Generic.List<string> Failures = _sink.Failures;

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    public static void Run(SettingsWindow win)
    {
        MenuCheck.Section(_sink, "B141_W13 · 设置窗四条（4-05 下拉高 · 4-10 页标题档 · 4-13 行距 · 4-01 字体面）");
        T(win != null, "（前提）`SettingsWindow` 拿得到");
        if (win == null) return;
        SectionA_DropdownHeight(win);
        SectionB_TitleVAlign(win);
        SectionC_LineSpacingAndOverflow(win);
        SectionD_Face(win);
        win.OpenTab(SettingsTab.General);      // 收尾：回到通用页（后面的段落接着从 General 起）
    }

    // ============================================================ A · 4-05 语言下拉面板高
    static void SectionA_DropdownHeight(SettingsWindow win)
    {
        // 公式只此一份（`TMP_Dropdown.Show():926-932`）：面板高 = n × Item 高 + (Content 模板高 − Item 高)。
        // 40.8707 = 原版 `Item.m_SizeDelta.y`；0.8519 = 41.7226（`Content` 模板高）− 40.8707。
        N(12f * SettingsWindow.LstItemH + SettingsWindow.LstItemGap, 491.30f, 0.02f,
          "A1 ★ 12 项 ⇒ 面板高 = 12 × 40.8707 + 0.8519 = **491.30**（B138 §4 `4-05` 的原版值）");
        N(13f * SettingsWindow.LstItemH + SettingsWindow.LstItemGap, 532.17f, 0.02f,
          "A1b ★ 13 项（现读 `Loc.Languages.Length`）⇒ 532.17 —— 同一个公式，⛔ 别写死 491.30");
        N(SettingsWindow.LstB - SettingsWindow.LstT, 573.96f, 0.02f,
          "A2 ★ `Template` **原型**仍是序列化的 **573.96**（prefab 逐字段实读；🧨 把它一起缩了 ⇒ 本行红，那不是原版形状）");

        bool wasOpen = win.LangListOpen;
        if (!wasOpen) win.ShowLangList();
        try
        {
            T(win.LangListOpen, "A3（前提）语言列表开出来了（下面要量它的**活克隆体**）");
            T(win.LangRowCount == 13, $"A3b（前提）列表装了 13 行（实得 {win.LangRowCount}）—— 面板高就是按它算的");
            var list = win.LangListNode;
            T(list != null, "A4（前提）`Dropdown List` 克隆体在");
            var bg = MenuCheck.FindChild(list, "bg");
            float lx = 0f, ty = 0f, rx = 0f, by = 0f;      // ⛔ 先归零：`&&` 短路时编译器要求 out 已赋值
            bool ok = bg != null && MenuDraw.UnionQuadRectPx(bg, MenuDraw.QuadGate.InHierarchy, true,
                                                            out lx, out ty, out rx, out by);
            T(ok, "A5（前提）面板底图**量得到**（一个 active 的 `ImageQuad` 都没有 ⇒ 下面两条等于没查）");
            if (!ok) return;
            T(Mathf.Abs(lx - 632.90f) <= 1.5f && Mathf.Abs(ty - 405.60f) <= 1.5f && Mathf.Abs(rx - 989.00f) <= 1.5f,
              $"A6 左/顶/右仍是原版那一格（实得 [{lx:F2},{ty:F2}]–[{rx:F2},?]，应 ≈ [632.90,405.60]–[989.00,…]）"
            + "｜⛔ 本次只改**底**（内容收缩），左右顶一个字不该动");
            // 底 = 540 + (390.667 + 532.171 − 540) × 0.9 = 884.55（屏幕 px；本窗根那道 0.9 烘在 Screen() 里）
            N(by, 884.55f, 1.5f,
              "A7 ★ 面板**底**按内容收缩 ⇒ 884.55（= 540 + (390.667 + 13×40.8707 + 0.8519 − 540) × 0.9）"
            + "｜🧨 改回满高 573.96 ⇒ 底是 922.16 ⇒ 本行红");
            N(by - ty, 478.95f, 1.5f,
              "A8 ★ 面板高 = 内容高 532.171 × 0.9 = **478.95**（原版 `Show():929-932` 的 `sizeDelta.y -= extraSpace`）");
            T(by - ty < 500f,
              "A9 ★ **灭自证**：面板高**严格小于**满高 573.96 × 0.9 = 516.56"
            + "（改回旧写法 ⇒ 516.56 ⇒ 本行红；这条与 A7/A8 的期望值不同源，两处一起改才编得圆）");
        }
        finally
        {
            if (!wasOpen) win.HideLangList();
        }
    }

    // ============================================================ B · 4-10 页标题垂直档（Capline）
    static void SectionB_TitleVAlign(SettingsWindow win)
    {
        T(TmpFont.Available,
          "B0（前提）TMP 后端在 —— 点阵后端下 `Label.VAlignTier` 永远报 `Middle`，下面几条就无从谈起");
        // 原版四颗真页标题 + 通用页；逐页现读（页标题在 `Build()` 里只建一次，切页只是显隐）
        CheckPageTitle(win, SettingsTab.General, "General Tab", "General");
        CheckPageTitle(win, SettingsTab.Graphics, "Graphics Tab", "Graphics");
        CheckPageTitle(win, SettingsTab.Audio, "Media Tab", "Media");
        CheckPageTitle(win, SettingsTab.Account, "Account Tab", "Account");
        CheckPageTitle(win, SettingsTab.Support, "Support Tab", "Support");
    }

    static void CheckPageTitle(SettingsWindow win, SettingsTab tab, string pageName, string label)
    {
        win.OpenTab(tab);
        var page = MenuCheck.FindChild(win.transform, pageName);
        T(page != null, $"B1（前提）`{pageName}` 在（否则 {label} 页标题那条无从谈起）");
        if (page == null) return;
        var node = MenuCheck.FindChild(page, "Tab Title");
        var lb = node != null ? node.GetComponentInChildren<Label>() : null;
        T(lb != null, $"B1b（前提）`{pageName} > Tab Title` 上有一颗 `Label`");
        if (lb == null) return;
        C(lb.VAlignTier, Label.VAlign.Capline,
          $"B2 ★ `{pageName} > Tab Title` 的垂直档 = 原版 `m_VerticalAlignment = 8192 (Capline)`（实得 {lb.VAlignTier}）");
        T(lb.VAlignTier != Label.VAlign.Middle,
          $"B3 ★ **灭自证**：它不是 `Label` 的出厂档 `Middle`（`{pageName}`）"
        + "｜🧨 把 `AlignLeft` 旁边那句 `MenuDraw.SetVAlign(… Capline …)` 删掉 ⇒ 本行红");
    }

    // ============================================================ C · 4-13 行距 / 字距 / 溢出
    static void SectionC_LineSpacingAndOverflow(SettingsWindow win)
    {
        win.OpenTab(SettingsTab.Support);
        var page = MenuCheck.FindChild(win.transform, "Support Tab");
        T(page != null, "C0（前提）`Support Tab` 在");
        if (page == null) return;

        N(LineSpacingOf(page, "Faq Text"), 15f, 0.01f,
          "C1 ★ `Support Tab > Faq Text` 的 `m_lineSpacing = **15**`（原版 prefab 实读；单位 = 字体单位，⛔ 不乘 0.9）");
        N(LineSpacingOf(page, "Faq Text Mobile"), 15f, 0.01f,
          "C2 ★ `Support Tab > Faq Text Mobile` = **15**（同上；只有这两颗非 0）");
        N(LineSpacingOf(page, "Contact Text"), 0f, 0.01f,
          "C3 ★ 邻位**对照**：`Contact Text` 原版 `m_lineSpacing = 0` ⇒ 仍是 0"
        + "（🧨 若这里读成 15，说明写成了「整页一律 15」——那不是原版）");

        // 字距 / 溢出：原版整窗 102 颗 TMP **全是 0** ⇒ 我们一棵树上也不该有任何非 0。
        var all = win.GetComponentsInChildren<Label>(true);
        T(all.Length >= 15, $"C4（前提）窗里扫到 **{all.Length}** 个 `Label`（≥ 15 这一扫才有意义）");
        int badChar = 0, badOf = 0; string badList = "";
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i].GetComponentInChildren<TMP_Text>(true);
            if (t == null) continue;
            if (!Mathf.Approximately(t.characterSpacing, 0f)) { badChar++; if (badList.Length < 500) badList += $"{all[i].name}(字距 {t.characterSpacing:0.##}) "; }
            if (t.overflowMode != TextOverflowModes.Overflow) { badOf++; if (badList.Length < 500) badList += $"{all[i].name}(溢出 {t.overflowMode}) "; }
        }
        T(badChar == 0,
          $"C5 ★ 本窗**没有**非 0 字距（原版 `m_characterSpacing` 102/102 = 0）"
        + (badChar > 0 ? $" —— **有 {badChar} 颗非 0**：{badList}" : ""));
        T(badOf == 0,
          $"C6 ★ 本窗**没有**改过溢出模式（原版 `m_overflowMode` 102/102 = `Overflow`；Truncate 在本窗 = **0 颗**）"
        + (badOf > 0 ? $" —— **有 {badOf} 颗不是 `Overflow`**：{badList}" : ""));
    }

    /// <summary>取某个节点子树里第一颗 `Label` 的 TMP `lineSpacing`；取不到返回 `NaN`（断言会红，⛔ 不静默当 0）。</summary>
    static float LineSpacingOf(Transform page, string nodeName)
    {
        var node = MenuCheck.FindChild(page, nodeName);
        if (node == null) return float.NaN;
        var t = node.GetComponentInChildren<TMP_Text>(true);
        return t != null ? t.lineSpacing : float.NaN;
    }

    // ============================================================ D · 4-01 字体面（本窗 = Pragati）
    static void SectionD_Face(SettingsWindow win)
    {
        var asset = TmpFont.FaceAsset(TmpFont.Face.LatinCardBody);
        T(asset != null && asset.name.Contains("Pragati"),
          "D1 ★ `Face.LatinCardBody` 取到的就是原版那一份资产（`Pragati-Regular SDF`，"
        + "guid `2548b731a71f84745a1b13c211c923ca`）｜🧨 换枚举序 / 换绑定 ⇒ 本行红"
        + "｜实得「" + (asset != null ? asset.name : "<null>") + "」");

        var all = win.GetComponentsInChildren<Label>(true);
        T(all.Length >= 15, $"D2（前提）窗里扫到 **{all.Length}** 个 `Label`");
        int latin = 0; string otherList = "";
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].Face == TmpFont.Face.LatinCardBody) latin++;
            else if (otherList.Length < 600) otherList += $"{all[i].name}={all[i].Face} ";
        }
        T(latin == all.Length,
          $"D3 ★ 本窗 **{latin}/{all.Length}** 个 `Label` 的字体面 = `LatinCardBody`（Pragati）"
        + " —— 原版整窗 **102/102** 颗 TMP 都指向 `Pragati-Regular SDF`（`Main Menu Settings Window.prefab` 实读）"
        + (latin != all.Length ? $"｜不是拉丁面的：{otherList}" : "")
        + "｜🧨 把 `SettingsWindow.Text()` 漏斗里的 `face: face` 去掉 ⇒ 只剩 4 颗直调 `MenuDraw.Text` 的仍是拉丁面 ⇒ 本行红");
    }
}
