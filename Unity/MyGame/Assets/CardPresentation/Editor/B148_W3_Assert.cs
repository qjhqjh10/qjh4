// B148_W3_Assert.cs — 执行写手 `B148-W3`（界面/字体域 · 2026-11-05）
//
// ============================ 盯的是什么（五件活）============================
//  ① `B143-FONT-FACE-WAVE`：`MenuDraw.Text` / `TextCore` / `TextBox` / `TmpFont.NewText`(5 参) 的**缺省字体面**
//     从 `Face.Cjk` 改成 **`Face.LatinCardBody`（Pragati）**。
//     判据 = 原版 1314 份 prefab 的 `m_fontAsset` 现数：**Pragati 4431 颗 / Asar 548 颗 / Noto 0 颗**
//     （`判据/B144_字体主面施工单.md` §1·1；`判据/B147_对账_R3界面外壳.md` 的 `B143-FONT-FACE-WAVE` 行）。
//     ⚠️ 中文仍由 `Label.ActualFace` 自动翻回 CJK —— 那条链**必须仍然活着**（见 A-6）。
//  ② `B146-MULLIGAN-AUTOFIT`：`MulliganPanel` 三颗 TMP 的自适应三档（原版 `m_fontSizeMin/Max/Base`）
//     必须经 `Label.ApplyAutoFitEm` 折成 px 口径再喂 `SetAutoFitBox`。
//     判据 = 原版字段 `ContinueText` 45/36/[18,45]（`Battle Arena 1.unity:225355-225360`）·
//            `TurnText` 55/36/[18,55]（`MonoBehaviour_3856.json`）· 提示行 65/36/[18,65]（`MonoBehaviour_3737` 一族）。
//     🔴 **CJK 面的 px 口径是 0.853 em**（`TmpFont.EmPerFontSize(Face.Cjk)` = 汉字墨高）⇒ 直传 45 ⇒ `fontSizeMax` 变 **52.7**。
//  ③ `B146-CHOOSETEXT-WIDTH`：选牌标题的**真实折行宽** = 原版子件 `/ChooseText/Text` 的 rect **1307.06**
//     （父件 1344 是容器；`panel_0914b/p4_choose_tree.tsv`）⇒ 不是 1344。
//  ④ `B123-ARMYTITLE` 的**离线半**：`Label` 的 `_pendAlignX` 唯一兑现口原来是 `RefreshBounds` 尾句，
//     而 `OnEnable` 只调 `TryApplyPendingWrap`（首句 `_pendWrapW < 0 ⇒ return`）⇒ **没设过折行宽**的标签
//     （`Txt()` 建的那一族）激活时**永远不会兑现**未激活时欠下的对齐。
//     修法 = `OnEnable` / `Start` 各调一次 `TryApplyPendingAlignOnEnable()`（⛔ 没删 `TryApplyPendingWrap` 的早退）。
//
// ============================ 怎么接（⛔ 本文件不接线）============================
//  `public static void Run()`：**零前提**（自己建相机/临时 GameObject，不读存档、不写 PlayerPrefs）。
//  建议宿主（调度台定）：`Editor/BattleScene.cs`（换牌/选牌面板那一节）或 `Editor/DeckScene.cs`（「军队」抬头那一节）
//  的聚合行**同一物理行**追加：
//      B148_W3_Assert.Run(); _sink.Pass += B148_W3_Assert.Pass; _sink.Fail += B148_W3_Assert.Fail;
//  判绿红：`grep "^B148W3"` 的**合计行**；逐条按行首 `B148W3 ✓` / `B148W3 ✗` 数（⛔ 别 `grep -c ✗`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A-1/A-2/A-3/A-4：把任何一处缺省面改回 `Face.Cjk` ⇒ 对应那条红（反射读的是**形参默认值本身**）。
//  · A-5：缺省面改回去 ⇒ 那个 Label 的 `Face` 是 Cjk ⇒ 红。
//  · A-6：把 `Label.ActualFace` 的「含汉字翻回 CJK」删掉（= 中文拿 Pragati 渲，出一排 .notdef 方框）⇒ 红。
//  · B-1/B-2：把 `Label.ApplyAutoFitEm` 换回「直传常量」⇒ 中文那颗 `fontSizeMax` 变 **52.7**（不是 45）⇒ 红；
//             把 `SetDoneText` 里那句重放删掉 ⇒ 换文案后仍停在旧面的 px ⇒ 同样红。
//  · B-3：把 `SetDoneText` 的语种换算删掉 ⇒ 英文档 `fontSize` 掉到 36(base) 或别处 ⇒ 红。
//  · C-1：把 `ChoosePanel` 的 `TitleW` 改回 1344 ⇒ 红（1307.064 ± 0.02）。
//  · D-1：把 `TryApplyPendingAlignOnEnable` 从 `OnEnable` 里摘掉 ⇒ 未激活时欠下的对齐不兑现 ⇒ 左沿停在框心（≈ −W/2）⇒ 红。
//  · D-3：把 D-1 那条改回「只记待办、不兑现」⇒ D-1 与 D-3 一起红（两条互为反面对照）。

using System;
using System.Reflection;
using CardPresentation;
using TMPro;
using UnityEngine;

/// <summary>执行写手 `B148-W3` 的验收点（字体缺省面 / 换牌自适应 / 选牌标题宽 / 未激活对齐兑现）。</summary>
public static class B148_W3_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "[B148W3]";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol, msg + "（实得 " + got.ToString("F4") + " / 期望 " + want.ToString("F4") + " ± " + tol + "）");
    }

    public static void Run()
    {
        _pass = _fail = 0;
        GameObject camGo = null;
        var prevCam = LayoutSpace.Cam;
        try
        {
            if (LayoutSpace.Cam == null)
            {   // 同 `B145_E6_Assert` 那套零前提：宿主里已有相机时不另建（免得顶掉别人的 `LayoutSpace.Cam`）
                camGo = new GameObject("Main Camera (B148W3)");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.aspect = LayoutSpace.DesignAspect;
                LayoutSpace.Apply(cam);
            }
            A_FontFaceDefault();
            B_MulliganAutoFit();
            C_ChooseTextWidth();
            D_PendingAlignOnEnable();
        }
        catch (Exception e)
        {
            Check(false, "B148_W3 组抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message);
        }
        finally
        {
            LayoutSpace.Cam = prevCam;
            if (camGo != null) UnityEngine.Object.DestroyImmediate(camGo);
        }
        Debug.Log("B148W3 合计 " + _pass + " 过 / " + _fail + " 红");
    }

    // ============================================================
    //  ① A 组：缺省字体面（4 处）
    // ============================================================
    static ParameterInfo FaceDefaultParam(Type t, string methodName, bool nonPublic)
    {
        var flags = BindingFlags.Static | (nonPublic ? BindingFlags.NonPublic : BindingFlags.Public);
        foreach (var m in t.GetMethods(flags))
        {
            if (m.Name != methodName) continue;
            foreach (var p in m.GetParameters())
                if (p.Name == "face" && p.HasDefaultValue) return p;
        }
        return null;
    }

    static void CheckFaceDefault(Type t, string methodName, bool nonPublic, string what)
    {
        var p = FaceDefaultParam(t, methodName, nonPublic);
        Check(p != null, what + "（前提）找得到那个带 `face` 缺省值的形参");
        if (p == null) return;
        int got = Convert.ToInt32(p.DefaultValue);
        Check(got == (int)TmpFont.Face.LatinCardBody,
              what + " 的**缺省字体面 = `Face.LatinCardBody`（Pragati）**"
            + "（实得 `" + ((TmpFont.Face)got) + "`）"
            + "｜🧨 改回 `Face.Cjk` ⇒ 本条红；判据 = 原版 1314 份 prefab 里 Noto **0 颗**（`判据/B144_字体主面施工单.md` §1·1）");
    }

    static void A_FontFaceDefault()
    {
        CheckFaceDefault(typeof(MenuDraw), "Text", false, "A-1 `MenuDraw.Text`");
        CheckFaceDefault(typeof(MenuDraw), "TextCore", true, "A-2 `MenuDraw.TextCore`（施工单 §1·2 漏的那一处）");
        CheckFaceDefault(typeof(MenuDraw), "TextBox", false, "A-3 `MenuDraw.TextBox`（176 个站点，施工单漏的那一处）");

        // A-4：`TmpFont.NewText` 五参重载没有可选参数（它直接转发）⇒ 只能**行为**判
        GameObject root = null;
        try
        {
            root = new GameObject("B148W3 font probe", typeof(RectTransform));
            var t = TmpFont.NewText(root.transform, "probe", "abc", 20f, Color.white);
            Check(t != null, "A-4（前提）`TmpFont.NewText` 建出了 TMP（拿不到字体资产时为 null）");
            if (t != null)
            {
                var want = TmpFont.FaceAsset(TmpFont.Face.LatinCardBody);
                Check(want != null && ReferenceEquals(t.font, want),
                      "A-4 ★ `TmpFont.NewText`(5 参) 的**缺省面 = Pragati（`Face.LatinCardBody`）**"
                    + "（实得 " + (t.font != null ? t.font.name : "<null>") + "）"
                    + "｜🧨 转发改回 `Face.Cjk` ⇒ 实得 Noto CJK ⇒ 红");
            }

            // A-5 / A-6：`MenuDraw.Text` 的**真实行为**（拉丁走 Pragati、汉字仍然自动翻回 Noto）
            var r = new PxRect(100f, 100f, 500f, 160f);
            var lat = MenuDraw.Text(root.transform, r, "Settings", Color.white, "lat", 32f, MenuDraw.QText);
            Check(lat != null, "A-5（前提）`MenuDraw.Text` 建出了 Label");
            if (lat != null)
            {
                Check(lat.Face == TmpFont.Face.LatinCardBody,
                      "A-5 ★ 不传 `face:` 时 `Label.Face` = **Pragati**（`LatinCardBody`）"
                    + "（实得 " + lat.Face + "）｜🧨 缺省面改回 Cjk ⇒ 红");
                Check(lat.ActualFace == TmpFont.Face.LatinCardBody,
                      "A-5b 拉丁文本的 `ActualFace` 就是 Pragati（不被翻面）");
            }
            var cjk = MenuDraw.Text(root.transform, r, "设置", Color.white, "cjk", 32f, MenuDraw.QText);
            Check(cjk != null && cjk.ActualFace == TmpFont.Face.Cjk,
                  "A-6 ★★ **中文仍然自动翻回 Noto（`Face.Cjk`）** —— 三份拉丁资产没有汉字字形"
                + "（`Label.ActualFace`），改缺省面只影响拉丁文本"
                + "｜🧨 把那条翻面链删掉 ⇒ 中文拿 Pragati 渲出一排 .notdef 方框 ⇒ 本条红");
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
    }

    // ============================================================
    //  ② B 组：换牌面板三颗 TMP 的自适应（原版 `m_fontSize*` 是 fontSize 单位，不是 px）
    // ============================================================
    static void B_MulliganAutoFit()
    {
        GameObject root = null;
        MulliganPanel p = null;
        try
        {
            root = new GameObject("B148W3 mulligan probe");
            p = MulliganPanel.Create(root.transform);
            Check(p != null, "B-0（前提）换牌面板建得出来（`MulliganPanel.Create`；拿不到 `HudCanvas` 时为 null）");
            if (p == null) return;

            var done = p.DoneLabelView;
            var turn = p.TurnLabel;
            var prompt = p.PromptLabel;
            Check(done != null && turn != null && prompt != null, "B-0b（前提）三颗 `Label` 都建出来了");
            if (done == null || turn == null || prompt == null) return;

            // `Build()` 收尾会 `SetVisible(false)`；未激活的 TMP 量不出尺寸 ⇒ 断之前先打开（同 `B145_E6_Assert`）
            done.gameObject.SetActive(true);
            turn.gameObject.SetActive(true);
            prompt.gameObject.SetActive(true);

            // ---- 「继续」钮：原版 `m_fontSizeMin/Max/Base = 18/45/36`、框 368.929×62.242 ----
            p.SetDoneText("继续");       // 强制中文档（`SetDoneText` 会按语种重设字号，并按新面重放三档）
            Near(done.FontSizeMax, 45f, 0.02f,
                 "B-1 ★★ 中文档「继续」的 `fontSizeMax` = **45**（原版字段）"
               + "｜🧨 把 `ApplyAutoFitEm` 换回「直传常量」⇒ CJK 面折成 45/0.853 = **52.7** ⇒ 红");
            Near(done.FontSizeMin, 18f, 0.02f,
                 "B-1b 中文档「继续」的 `fontSizeMin` = **18**（同上；直传会变 21.1）");
            Near(done.FontSize, 45f, 1.0f,
                 "B-2 ★ 中文档「继续」自适应收敛到 **45**（框高 62.242 ÷ 中文行盒 1.2933 = 48.1 > max ⇒ 停在 max）"
               + "｜🧨 直传三档 ⇒ max 52.7 ⇒ 收敛 48.1（原版没有这一档）⇒ 红");

            p.SetDoneText("Continue");   // 拉丁档：换算恒等，同样停在 45
            Near(done.FontSizeMax, 45f, 0.02f, "B-3 拉丁档「Continue」的 `fontSizeMax` 仍是 **45**（拉丁面换算恒等，⛔ 别把它也乘 0.853）");
            Near(done.FontSize, 45f, 1.0f, "B-3b 拉丁档「Continue」收敛到 **45**（原版 dump/序列化值）");

            // ---- 先手/后手那一行：原版 55/36/[18,55] ----
            Near(turn.FontSizeMax, 55f, 0.02f,
                 "B-4 `TurnText` 的 `fontSizeMax` = **55**（原版 `MonoBehaviour_3856.json`）"
               + "｜🧨 直传 ⇒ 中文档变 64.5 ⇒ 红（本条的期望与语种无关：拉丁面换算恒等、CJK 面折算后正好 55）");
            Near(turn.FontSizeMin, 18f, 0.02f, "B-4b `TurnText` 的 `fontSizeMin` = **18**");

            // ---- 提示行：原版 65/36/[18,65] ----
            Near(prompt.FontSizeMax, 65f, 0.02f,
                 "B-5 提示行的 `fontSizeMax` = **65**（原版字段；同上，期望与语种无关）");
            Near(prompt.FontSizeMin, 18f, 0.02f, "B-5b 提示行的 `fontSizeMin` = **18**");
        }
        finally
        {
            if (p != null) UnityEngine.Object.DestroyImmediate(p.gameObject);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ============================================================
    //  ③ C 组：选牌标题的真实折行宽 = 原版子件 rect 1307.06（不是父件 1344）
    // ============================================================
    static void C_ChooseTextWidth()
    {
        GameObject root = null;
        ChoosePanel p = null;
        try
        {
            root = new GameObject("B148W3 choose probe");
            p = ChoosePanel.Create(root.transform);
            Check(p != null, "C-0（前提）选牌面板建得出来（`ChoosePanel.Create`；拿不到 `HudCanvas` 时为 null）");
            if (p == null) return;
            var t = p.TitleLabelForTest;
            Check(t != null, "C-0b（前提）标题那颗 `Label` 建出来了");
            if (t == null) return;

            Near(t.WrapWidthForTest, 1307.064f, 0.02f,
                 "C-1 ★★ 选牌标题的**折行宽 = 1307.064**（原版 TMP 挂在子件 `/ChooseText/Text`："
               + "`sd = −36.94,0` + 父 1344 ⇒ `rect = 1307.06`；`panel_0914b/p4_choose_tree.tsv`）"
               + "｜🧨 改回父件的 1344（差 37 px，超出本容差两个量级）⇒ 红");
            Check(t.WrapWidthForTest < 1340f,
                  "C-1b 折行宽**不是**父容器那个 1344（原版那颗 TMP 不在父件上：父件只有 79.44 高的容器）");
        }
        finally
        {
            if (p != null) UnityEngine.Object.DestroyImmediate(p.gameObject);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ============================================================
    //  ④ D 组：未激活时欠下的对齐，激活后必须兑现（`B123-ARMYTITLE` 离线半）
    // ============================================================
    static void D_PendingAlignOnEnable()
    {
        GameObject root = null, root2 = null;
        Label a = null, b = null;
        try
        {
            // 🔴 夹具必须复刻**真实那条路**：`Txt()` 是在**父节点还关着**的时候建标签 + 设对齐的
            //   （`Deck/DeckRuntime.cs` 的「军队」抬头），那时 TMP 从没生成过 ⇒ `_measured == false`、
            //   `textBounds` 是哨兵 ⇒ `AlignLeftOn` 只能把请求记成待办。⛔ 别用「先建活的、再关掉」
            //   —— 那一路 `_measured` 已经是 true，对齐会**当场生效**，断言就测不到待办兑现（假绿）。
            root = new GameObject("B148W3 align probe", typeof(RectTransform));
            root.SetActive(false);
            a = Label.CreateUgui(root.transform, "军队", Vector3.zero, 1, Color.white, new Vector2(0.5f, 0.5f), "pendAlignProbe");
            Check(a != null, "D-0（前提）夹具 Label 建出来了（`Label.CreateUgui`；拿不到字体资产时为 null）");
            if (a == null) return;

            a.AlignLeftOn(300f);                 // 未激活 ⇒ 量不出宽 ⇒ 只剩待办
            float centered = a.transform.localPosition.x;
            Near(centered, 0f, 0.5f,
                 "D-0b（前提）未激活时那次 `AlignLeftOn` **没有当场生效**（节点仍在框心 x = 0）"
               + " —— 它只能是一条待办（否则本组测的不是待办兑现，会假绿）");

            root.SetActive(true);                // OnEnable 必须把它兑现

            float left1 = a.transform.localPosition.x - a.WorldW * 0.5f;
            Check(a.WorldW > 1f, "D-0c（前提）激活后量到了真宽度（WorldW = " + a.WorldW.ToString("F2") + "）");
            Near(left1, 300f, 1.0f,
                 "D-1 ★★ **未激活时 `AlignLeftOn(300)` 欠下的对齐，激活后兑现**：文字块左沿 = 300"
               + "｜🧨 把 `TryApplyPendingAlignOnEnable()` 从 `OnEnable` 里摘掉 ⇒ 字块停在框心（左沿 ≈ −W/2 = "
               + (-a.WorldW * 0.5f).ToString("F1") + "）⇒ 红");

            // D-2 先激活、后设对齐（既有路径，本来就对 —— 反面照）
            root2 = new GameObject("B148W3 align probe2", typeof(RectTransform));
            b = Label.CreateUgui(root2.transform, "军队", Vector3.zero, 1, Color.white, new Vector2(0.5f, 0.5f), "liveAlignProbe");
            Check(b != null, "D-2（前提）第二颗夹具建出来了");
            if (b != null)
            {
                b.AlignLeftOn(300f);
                float left2 = b.transform.localPosition.x - b.WorldW * 0.5f;
                Near(left2, 300f, 1.0f, "D-2 ★ 已激活那条路（先激活后对齐）仍然落在 300（既有行为零回归）");
                Near(left1, left2, 0.05f,
                     "D-3 ★★ 两条路的**落点相同**（先对齐后激活 == 先激活后对齐）—— "
                   + "这是「待办那一路真的兑现了」的结构判据，⛔ 只修一条会在这儿分叉");
            }
        }
        finally
        {
            if (a != null) UnityEngine.Object.DestroyImmediate(a.gameObject);
            if (b != null) UnityEngine.Object.DestroyImmediate(b.gameObject);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (root2 != null) UnityEngine.Object.DestroyImmediate(root2);
        }
    }
}
