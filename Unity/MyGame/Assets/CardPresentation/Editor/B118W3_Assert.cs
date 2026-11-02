// B118W3_Assert.cs — 执行代理 `B118-W3`（第一百一十八会话 · 波 3）· 界面四件
//
// ============================ 它盯的是哪四条 ============================
//  · `B117-T03` 联盟窗头像立绘的 **y 偏 0.54 px**（原版 `Image.apos.y = 2.7` 是**局域单位**，
//    父链 ×0.8 ⇒ 屏幕位移 2.16；我们原来减的是未乘 0.8 的 2.7 ⇒ 中心 y = 120.50，真值 **121.00**）。
//    判据 → `资料/完全复刻/判据/B117-G10-05_名片与联盟窗.md` §5·1（`menu_dump` 现读 `(9.97,61.39)-(179.99,180.61)`）。
//  · `B117-T05` 多卡展示横向滚动 = 原版 `m_MovementType = 1 (Elastic)` + `m_Elasticity = 0.1`
//    （`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4823.json` 现读）。
//    判据 → `判据/B117-G10-14_选牌与多卡展示.md` §二·A。
//  · `B117-T09` 语音条卡图 = **画大再裁**：`RectMask2D` **144.457×157.465**（padding 0 / softness 0）
//    + 子图 `CardImage` **176×176**、`m_PreserveAspect 0`、`ap (−2.48, −5.00)`
//    （`RectTransform_2600/3347/3408` + `_2844/3108/3169` + `MonoBehaviour_4340/4724/4798`）。
//    判据 → `判据/B117-G10-22_战斗内聊天与语音条.md` §二 第 5 行。
//  · `B117-T10` `TutorialObjs` **只许有一棵**（原版 = `CardDisplayWindow` 的 `Card Display/TutorialObjs`，
//    闸 `SetupTutorialObj` 7 道）；`TutorialOverlay.Build()` 那第二棵已停建。
//    判据 → `判据/B117-G10-23_教程与战役脚本.md` §一 第 6 行。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `B118W3_Assert.Run();`
//  宿主里两行：`B118W3_Assert.Run(); pass += B118W3_Assert.Pass; fail += B118W3_Assert.Fail;`
//  · **不建任何 Unity 对象、不切场景、不改静态状态**（全部读静态值 / 源码文本）⇒ 任何宿主都能接。
//
// ============================ 🔴 灭自证（每条写清「改哪一处会红」）============================
//  · A4：把 `AvatarImageR` 的 y 改回 `60.9/180.1` ⇒ 中心 120.50 ⇒ A3/A4/A5 同时红。
//  · B2/B3：把 `_scrollPx -= dxPx` 改回 `Mathf.Clamp(…)`（拖动即夹）⇒ B2 红；
//           把 `SmoothDamp` 回弹那一段删掉 ⇒ B1/B3 红。
//  · C2/C3：把 `RectMask2D` 删掉 / 把 176 改回 144.46 ⇒ 红。
//  · D1/D2：把 `_annoGo = null` 改回 `NewNode("TutorialObjs", …)` ⇒ D1/D2 红。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B118-W3`：界面四件（联盟窗立绘 / 弹性滚动 / 语音条裁切 / 教学层重复）。
/// <para>⛔ 期望值全是**原版字面量**（`menu_dump` / prefab 字段）；源码闸只判「活代码里有没有那一句」，
/// 从不把被测实现自己的常量读回来当期望（那就是自证）。</para></summary>
public static class B118W3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B118W3] ") { Near = MenuNearStyle.Labeled4 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    const string SrcMultiCards = "Assets/CardPresentation/Battle/MultiCardDisplay.cs";
    const string SrcChat       = "Assets/CardPresentation/Battle/UnitChatPanel.cs";
    const string SrcChatPart   = "Assets/CardPresentation/Battle/UnitChatPanel.PartB118W3.cs";
    const string SrcTutorial   = "Assets/CardPresentation/Battle/TutorialOverlay.cs";
    const string SrcCardWin    = "Assets/CardPresentation/Battle/CardDisplayWindow.cs";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B118-W3 · T03 联盟窗立绘 y / T05 弹性滚动 / T09 语音条画大再裁 / T10 教学层只一棵");
        SectionA_AllianceAvatar();
        SectionB_ElasticScroll();
        SectionC_ChatPortraitClip();
        SectionD_TutorialObjsSingleTree();
        Debug.Log(_sink.P + $"B118_W3 合计：{_sink.Pass} 通过 / {_sink.Fail} 失败");
    }

    // ============================================================ A · T03 联盟窗头像立绘
    static void SectionA_AllianceAvatar()
    {
        MenuCheck.Section(_sink, "A · T03 联盟窗头像立绘 = 原版 `Image` 绝对框 `(9.97,61.39)-(179.99,180.61)` ×2");
        var r = AlliancePanelWindow.AvatarImageR;

        Tr(r.W > 1f, "A0 前提：`AvatarImageR` 真的读得到（宽 > 1；读不到 ⇒ 后面全是空转，⛔ 不许当绿）");
        // 原版 `Image Container` 170.02×119.21（`menu_dump`）× `m_LocalScale 2`
        Near(r.W, 340.04f, 0.10f, "A1 视觉宽 = **340.04**（原版容器 170.02 × 2）");
        Near(r.H, 238.44f, 0.10f, "A2 视觉高 = **238.44**（原版容器 119.21 × 2）");
        Near(r.CX, 94.98f, 0.05f, "A3 中心 x = **94.98**（原版 `Image` 绝对框中心；绕中心放大不动它）");
        // 🔴 本件的那 0.54 px：`apos.y = 2.7` 是**局域单位**、屏幕位移 = 2.7 × 0.8 = 2.16
        Near(r.CY, 121.00f, 0.05f, "A4 中心 y = **121.00**（`menu_dump` 现值；🧨 改回 `60.9/180.1` ⇒ 120.50 ⇒ 红）");
        Near(r.y1, 1.78f, 0.06f, "A5 视觉框**上沿** = **1.78**（121.00 − 119.22；🧨 旧那版是 1.30）");
        Tr(r.CY > 120.80f,
           "A6【判别式】中心 y **> 120.80** —— 挡「`apos.y` 少乘父链 0.8」那一版（120.50）");
    }

    // ============================================================ B · T05 弹性滚动
    static void SectionB_ElasticScroll()
    {
        MenuCheck.Section(_sink, "B · T05 多卡展示横向滚动 = 原版 `Elastic`（`m_MovementType 1` / `m_Elasticity 0.1`）");

        Near(MultiCardDisplay.ScrollElasticity, 0.1f, 1e-6f,
             "B1 `ScrollElasticity` = **0.1**（原版 `m_Elasticity`，UGUI 拿它当 `SmoothDamp.smoothTime`）");

        string src = ReadSrc(SrcMultiCards, "B");
        if (src == null) return;
        string live = StripLineComments(src);

        Tr(live.IndexOf("Mathf.SmoothDamp(_scrollPx, bound, ref _scrollVelPx, ScrollElasticity",
                        StringComparison.Ordinal) >= 0,
           "B2 ★ 松手/未拖拽时走 `Mathf.SmoothDamp(_scrollPx, bound, ref _scrollVelPx, ScrollElasticity…)`"
         + "（删掉回弹那一段 ⇒ 红）");
        Tr(live.IndexOf("Mathf.Clamp(_scrollPx - dxPx", StringComparison.Ordinal) < 0,
           "B3 ★ 拖动**不再夹**（`Mathf.Clamp(_scrollPx - dxPx, …)` 已消失；🧨 改回 `Clamped` 档 ⇒ 红）");
        Tr(live.IndexOf("if (fits) { _scrollPx = 0f;", StringComparison.Ordinal) < 0,
           "B4 ★ `ApplyCardLayout` 的 `fits` 支**不再把 `_scrollPx` 归零**（弹性位移要留得住）");
        Tr(live.IndexOf("_scrollPx += _scrollVelPx * dt;", StringComparison.Ordinal) >= 0,
           "B5 惯性段在（`_scrollPx += _scrollVelPx * dt`，越界不夹 ⇒ 交给下一帧 Elastic 回弹）");
        // 注释也要对：整份文件里不许再出现旧枚举名（历史留痕已就地改掉）
        Tr(src.IndexOf("m_MovementType = 1 (Clamped)", StringComparison.Ordinal) < 0,
           "B6 ★ 全文件**不含** `m_MovementType = 1 (Clamped)`（Unity 枚举 `Clamped = 2`，那一格是 `Elastic = 1`）");
        Tr(live.IndexOf("Clamped", StringComparison.Ordinal) < 0,
           "B7【判别式】活代码里**一处 `Clamped` 都没有**（改回「硬夹」那一版必然带它 ⇒ 红）");
    }

    // ============================================================ C · T09 语音条卡图「画大再裁」
    static void SectionC_ChatPortraitClip()
    {
        MenuCheck.Section(_sink, "C · T09 语音条卡图 = `RectMask2D` 144.457×157.465 + 子图 176×176 @(−2.48,−5.00)");

        string main = ReadSrc(SrcChat, "C-主");
        string part = ReadSrc(SrcChatPart, "C-分部");
        if (main == null || part == null) return;
        string lm = StripLineComments(main);
        string lp = StripLineComments(part);

        Tr(lp.IndexOf("AddComponent<RectMask2D>()", StringComparison.Ordinal) >= 0,
           "C1 ★ 卡图容器真的挂 `RectMask2D`（原版 `Mask` 的等价物；删掉它 = 回到「压满遮罩」那版 ⇒ 红）");
        Near(ConstOf(lp, "PortraitSpecPx"), 176f, 1e-3f,
             "C2 子图尺寸 = **176**（原版 `CardImage.m_SizeDelta`；🧨 写回 144.46 ⇒ 红）");
        Near(ConstOf(lp, "PortraitOffsetX"), -2.48f, 1e-3f, "C3 子图中心偏移 x = **−2.48**（原版 `m_AnchoredPosition.x`）");
        Near(ConstOf(lp, "PortraitOffsetYTop"), 5.00f, 1e-3f,
             "C4 子图中心偏移 y（本仓 y 向下）= **+5.00**（原版 `m_AnchoredPosition.y = −5.00`，y 向上）");
        Tr(lp.IndexOf("MakeImageInParent(mrt, tex, goName", StringComparison.Ordinal) >= 0,
           "C5 子图建在**遮罩容器**里（`MakeImageInParent(mrt, tex, goName, …)`）");
        Tr(lm.IndexOf("b.portrait = MakePortrait(", StringComparison.Ordinal) >= 0,
           "C6 主文件里卡图走 `MakePortrait(…)`（⛔ 别再直接 `Quad(…)` 压满遮罩）");
        Tr(lm.IndexOf("MovePortrait(b, left + PortraitCx", StringComparison.Ordinal) >= 0,
           "C7 `Place` 摆的是**遮罩容器**（`MovePortrait`；子图相对容器固定）");
        Tr(lm.IndexOf("if (b.portrait != null) b.portrait.SetAspect(PortraitW / PortraitH)",
                      StringComparison.Ordinal) < 0,
           "C8【灭自证】无守卫的 `SetAspect(PortraitW/PortraitH)` 已消失（写回来 ⇒ 子图被拽成 144.46×157.46、"
         + "「画大再裁」又变成「压满遮罩」）");
        Tr(lm.IndexOf("HasPortraitMask(b)", StringComparison.Ordinal) >= 0,
           "C9 三处 `SetAspect` 都过 `HasPortraitMask` 守卫（有遮罩 ⇒ 子图恒 176²，原版 `m_PreserveAspect = 0`）");
    }

    // ============================================================ D · T10 教学层只许一棵
    static void SectionD_TutorialObjsSingleTree()
    {
        MenuCheck.Section(_sink, "D · T10 `TutorialObjs` 只许一棵（原版 = `CardDisplayWindow.SetupTutorialObj` 那棵）");

        string tut = ReadSrc(SrcTutorial, "D-教程");
        string cardWin = ReadSrc(SrcCardWin, "D-展示窗");
        if (tut == null || cardWin == null) return;
        string lt = StripLineComments(tut);
        string lc = StripLineComments(cardWin);

        Tr(lt.IndexOf("_annoGo = null;", StringComparison.Ordinal) >= 0,
           "D1 ★ `TutorialOverlay` 的 `_annoGo` 恒 `null`（🧨 改回 `NewNode(\"TutorialObjs\", …)` ⇒ 红）");
        Tr(lt.IndexOf("\"TutorialObjs\"", StringComparison.Ordinal) < 0,
           "D2 ★ 激活代码里**不再建** `TutorialObjs` 节点（⛔ 别把第二棵树建回来）");
        Tr(lt.IndexOf("if (_annoGo != null) for (int i = 0; i < 4; i++)", StringComparison.Ordinal) >= 0,
           "D3 四颗标注的建件循环有 `_annoGo` 空闸（行数守恒下空转，删掉守卫 ⇒ NRE）");
        Tr(lt.IndexOf("AnnoGoneLog()", StringComparison.Ordinal) >= 0,
           "D4 ★ 停建是一次性**出声**（`AnnoGoneLog`；⛔ 不许静默）");
        Tr(lc.IndexOf("public void SetupTutorialObj(CardData d, bool forceHide = false)", StringComparison.Ordinal) >= 0,
           "D5 ★ 原版那**唯一**一棵的闸还在（`CardDisplayWindow.SetupTutorialObj`，7 道）");
        Tr(lc.IndexOf("\"TutorialObjs\"", StringComparison.Ordinal) >= 0,
           "D6 原版那棵树仍由 `CardDisplayWindow` 建（`TutorialObjs` 这个名字只该留在这里）");
    }

    // ============================================================ 小工具
    static string ReadSrc(string rel, string tag)
    {
        string s = null;
        try { if (File.Exists(rel)) s = File.ReadAllText(rel); }
        catch (Exception e) { Debug.LogWarning($"[B118W3] 读不到源码（{e.GetType().Name}）：{rel}"); }
        Tr(s != null, $"{tag}0 前提：源码读得到（`{rel}`；读不到 ⇒ 该段后面全是空转，⛔ 不许当绿）");
        return s;
    }

    /// <summary>从剥过注释的源码里抠 `const float NAME = <字面量>f;` 的值（抠不到 ⇒ `NaN`，断言自然红）。</summary>
    static float ConstOf(string live, string name)
    {
        string key = "const float " + name + " = ";
        int i = live.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return float.NaN;
        i += key.Length;
        int j = i;
        while (j < live.Length && (char.IsDigit(live[j]) || live[j] == '-' || live[j] == '+' || live[j] == '.')) j++;
        float v;
        return float.TryParse(live.Substring(i, j - i), System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out v) ? v : float.NaN;
    }

    /// <summary>逐行剥掉 `//` 之后的内容（够用就行：这几份源码里没有含 `//` 的字符串字面量）。</summary>
    static string StripLineComments(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
            }
            else sb.Append(s[i++]);
        }
        return sb.ToString();
    }
}
