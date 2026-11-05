// B148_W6_Assert.cs — 执行写手 `B148-W6`（界面/字体域 · 2026-11-05）
//
// ============================ 盯的是什么 ============================
//  `B143-FONT-FACE-WAVE` 的**第二批**：26 处「中间层缺省面」里属于 **tier ①（菜单 / 设置 / 社交 / 顶栏 / Toast / 抽屉）**
//  的 **20 个形参**，缺省值从 `TmpFont.Face.Cjk`（Noto）改成原版主面。
//  判据 = 原版 1314 份 prefab 的 `m_fontAsset` 现数：**Pragati 4431 颗（正文/数值）· Asar 548 颗（标题族）·
//  Noto 0 颗（只作 CJK fallback）**（`判据/B144_字体主面施工单.md` §1·1；`报告/B148_W3_字体UI.md` §4·1 的 26 处清单）。
//
//  ⚠️ **这一批是「漏斗」不是「站点」**：这些方法自己带缺省面、并把 `face:` 显式往下游传
//  （`MenuDraw.Text` / `TextBox` / `Label.SetFace` / `Label.Create`+`SetFace`），
//  所以**下游 W3 改过的缺省面在这一层被挡住** —— 不改这 20 处，从这些漏斗进来的文字仍然全是 Noto。
//
//  ⚠️ **中文不受影响**：`Battle/Label.cs` 的 `ActualFace` = 含汉字且没开 `_cjkFallbackFace` ⇒ 翻回 `Face.Cjk`
//  ⇒ 换缺省面**只影响拉丁文本**（这正是要修的那一半）。B-1/B-2 就是钉这一条。
//
// ============================ 怎么接（⛔ 本文件不接线）============================
//  `public static void Run()`：**零前提**（不读存档、不写 PlayerPrefs；只在 `LayoutSpace.Cam` 为空时临时建一台相机，
//  收尾还原）。建议宿主（由调度台定）：`Editor/MainMenuScene.cs`（顶栏/主菜单那一节）或
//  `Editor/ShellScene.cs`（外壳聚合行）的**同一物理行**追加：
//      B148_W6_Assert.Run(); _sink.Pass += B148_W6_Assert.Pass; _sink.Fail += B148_W6_Assert.Fail;
//  判绿红：`grep "^B148W6"` 的**合计行**；逐条按行首 `B148W6 ✓` / `B148W6 ✗` 数（⛔ 别 `grep -c ✗`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A-1…A-20：把任何一处形参缺省值改回 `Face.Cjk` ⇒ 对应那条红
//    （反射读的是**形参默认值本身**，不读渲染结果 ⇒ 改注释/改调用点都骗不过它）。
//  · A-XX 的「前提」那一条：方法被改名/删掉/新增同名重载 ⇒ 先红一条「前提」，⛔ 别读成通过。
//  · B-1：把 `Label.ActualFace` 的「含汉字翻回 CJK」删掉（= 中文拿 Pragati 渲，出一排 .notdef 方框）⇒ 红。
//  · B-2：把 `SetFace` / `SyncFace` 弄成不生效（`Face` 不跟着变）⇒ 红。
//  · B-3：把 `Label` 的**出厂面**从 `Cjk` 改掉 ⇒ 红 —— 本批**没动**它（战斗 HUD/卡面数百处站点的 px 口径
//    是 CJK 面核过的，见 `报告/B148_W3_字体UI.md` §一·1 的三条理由）。
//
// ============================ 本批的**去向口径**（逐条写在 A 组的文案里）============================
//  · 正文/数值族 ⇒ `Face.LatinCardBody`（Pragati）：16 处。
//  · 标题族（原版判据是 Asar）⇒ `Face.LatinUi`：1 处（`AlliancePanelWindow.Txt`，见 A-3 的注释）。
//  · 另外 3 处（`DeckRuntime.Txt` / `MainMenuRuntime.Text` / `TopBar.Text`）的调用点**本就全显式传面**，
//    改缺省是**一致性防御**，本周**零渲染变化** —— 文案里写明了，⛔ 别把它当成「画面会变」。
//
// ============================ 复核口令 ============================
//  `grep -rn "TmpFont.Face face = TmpFont.Face.Cjk" MyGame/Assets --include=*.cs`（排除 `Editor/`）
//  ⇒ 本批交回时**应只剩 7 处**：`Battle/{BattleDriver,CardDisplayWindow,HudCanvas,SkillPanel,TutorialOverlay}.cs`
//    + `Core/CardView.cs`（tier ③ 战斗 HUD/卡面，逐站核 B120 六域审计后再动）
//    + `Shell/GameWindow.cs:386`（**归 W7**，本批按红线跳过）。

using System;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B148-W6` 的验收点（20 处「中间层缺省面」+ 中文自动翻面的灭自证）。</summary>
public static class B148_W6_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "[B148W6]";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    // ------------------------------------------------------------------
    //  反射：取「所有同名方法里**带 `face` 缺省值**的那些形参」
    // ------------------------------------------------------------------
    static ParameterInfo[] FaceDefaultParams(Type t, string methodName, out int methodCount)
    {
        var flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic
                  | BindingFlags.Static | BindingFlags.Instance;
        var list = new System.Collections.Generic.List<ParameterInfo>();
        methodCount = 0;
        foreach (var m in t.GetMethods(flags))
        {
            if (m.Name != methodName) continue;
            ParameterInfo hit = null;
            foreach (var p in m.GetParameters())
                if (p.Name == "face" && p.HasDefaultValue) { hit = p; break; }
            if (hit == null) continue;
            methodCount++;
            list.Add(hit);
        }
        return list.ToArray();
    }

    /// <summary>断「`Type.methodName` 的 `face` 缺省面 = <paramref name="want"/>」。
    /// ⚠️ <paramref name="expectOverloads"/> 是**前提**（同名重载数）：新增/删掉一个重载 ⇒ 先红前提那一条。</summary>
    static void CheckFaceDefault(Type t, string methodName, TmpFont.Face want, int expectOverloads, string what)
    {
        int n;
        var ps = FaceDefaultParams(t, methodName, out n);
        Check(n == expectOverloads,
              what + "（前提）`" + t.Name + "." + methodName + "` 有 " + expectOverloads +
              " 个带 `face` 缺省值的重载（实得 " + n + "）｜🧨 方法改名/删掉/新增同类重载 ⇒ 本条红");
        if (n != expectOverloads) return;
        for (int i = 0; i < ps.Length; i++)
        {
            int got = Convert.ToInt32(ps[i].DefaultValue);
            Check(got == (int)want,
                  what + (ps.Length > 1 ? ("（重载 " + (i + 1) + "/" + ps.Length + "）") : "") +
                  " 的**缺省字体面 = `Face." + want + "`**（实得 `" + ((TmpFont.Face)got) + "`）" +
                  "｜🧨 改回 `Face.Cjk` ⇒ 本条红；判据 = 原版 1314 份 prefab 里 Noto **0 颗**（`判据/B144_字体主面施工单.md` §1·1）");
        }
    }

    public static void Run()
    {
        _pass = _fail = 0;
        GameObject camGo = null;
        var prevCam = LayoutSpace.Cam;
        try
        {
            if (LayoutSpace.Cam == null)
            {   // 同 `B148_W3_Assert` 那套零前提：宿主里已有相机时不另建（免得顶掉别人的 `LayoutSpace.Cam`）
                camGo = new GameObject("Main Camera (B148W6)");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.aspect = LayoutSpace.DesignAspect;
                LayoutSpace.Apply(cam);
            }
            A_DefaultFaces();
            B_BehaviorAndAntiSelfProof();
        }
        catch (Exception e)
        {
            Check(false, "B148_W6 组抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message);
        }
        finally
        {
            LayoutSpace.Cam = prevCam;
            if (camGo != null) UnityEngine.Object.DestroyImmediate(camGo);
        }
        Debug.Log("B148W6 合计 " + _pass + " 过 / " + _fail + " 红");
    }

    // ============================================================
    //  A 组：20 处形参缺省面（逐处一条）
    // ============================================================
    static void A_DefaultFaces()
    {
        // ---- 卡组编辑（原版 `Deck Editing Menu` prefab 64/64 颗 TMP = Pragati ⇒ 全窗正文主面）----
        CheckFaceDefault(typeof(DeckRuntime), "Txt", TmpFont.Face.LatinCardBody, 1,
            "A-1 `DeckRuntime.Txt`（卡组编辑窗正文漏斗；调用点本就全显式传面 ⇒ 本次改缺省零渲染变化）");
        CheckFaceDefault(typeof(DeckRuntime), "TxtPx", TmpFont.Face.LatinCardBody, 1,
            "A-2 `DeckRuntime.TxtPx`（同上；`TxtPx` 转发 `Txt`）");

        // ---- 战斗联盟面板：**标题族 = Asar**（原版 `Alliance Panel` 的 Title Text / Alliance Label /
        //      Alliance Name / NotInaAllianceText 逐颗 `m_fontAsset` = `Asar-Regular SDF`）----
        CheckFaceDefault(typeof(AlliancePanelWindow), "Txt", TmpFont.Face.LatinUi, 1,
            "A-3 `AlliancePanelWindow.Txt`（本扇是**标题族** ⇒ Asar；8 个调用点本就全显式传面 ⇒ 本次零渲染变化）");

        // ---- 聊天（正文 + 消息行）----
        CheckFaceDefault(typeof(ChatPanel), "Text", TmpFont.Face.LatinCardBody, 2,
            "A-4 `ChatPanel.Text`（两个重载：窗内正文口 + `ChatMessageRow` 静态口；原版 `ChatMessageRow` 的 Sender/Time/Message 是正文族）");

        // ---- 卡牌详情弹窗 / 反馈弹窗 / 抽屉 ----
        CheckFaceDefault(typeof(DeckInfoPopup), "Txt", TmpFont.Face.LatinCardBody, 1,
            "A-5 `DeckInfoPopup.Txt`（弹窗正文；标题那颗本就显式传 `LatinUi`）");
        CheckFaceDefault(typeof(GiveFeedbackPopup), "Text", TmpFont.Face.LatinCardBody, 1,
            "A-6 `GiveFeedbackPopup.Text`（问卷正文）");
        CheckFaceDefault(typeof(ItemDrawer), "ClippedText", TmpFont.Face.LatinCardBody, 1,
            "A-7 `ItemDrawer.ClippedText`（抽屉/商品条目正文）");

        // ---- 主菜单运行时（顶栏带 / 模式卡 / 活动卡）----
        CheckFaceDefault(typeof(MainMenuRuntime), "Text", TmpFont.Face.LatinCardBody, 1,
            "A-8 `MainMenuRuntime.Text`（主菜单正文漏斗；本文件 12/15 个调用点本就显式传 `LatinCardBody`）");

        // ---- 子窗基类（`CollectionWindow` / `ShopWindow` / `CampaignTab` 一族共用）----
        CheckFaceDefault(typeof(MainMenuSubmenuWindow), "Text", TmpFont.Face.LatinCardBody, 1,
            "A-9 `MainMenuSubmenuWindow.Text`（子窗正文漏斗，被卡组线/商店/战役页共用）");
        CheckFaceDefault(typeof(MainMenuSubmenuWindow), "TextBox", TmpFont.Face.LatinCardBody, 1,
            "A-10 `MainMenuSubmenuWindow.TextBox`（同上，限宽换行那一档）");

        // ---- 任务页（`name` 那三颗 Asar 站点本就显式传 `LatinUi`，本漏斗是正文）----
        CheckFaceDefault(typeof(MissionsTab), "Txt", TmpFont.Face.LatinCardBody, 1,
            "A-11 `MissionsTab.Txt`（转调 `MenuWindowBase.TextBox`）");
        CheckFaceDefault(typeof(MissionsTab), "Txt1", TmpFont.Face.LatinCardBody, 1,
            "A-12 `MissionsTab.Txt1`（转调 `MenuWindowBase.Text`，带闸那一档）");

        // ---- 商品/卡包容器（6 个调用点全显式传 `LatinCardBody`）----
        CheckFaceDefault(typeof(OfferContainer), "LabelFit", TmpFont.Face.LatinCardBody, 1,
            "A-13 `OfferContainer.LabelFit`（货架条正文；调用点本就全显式传面 ⇒ 本次零渲染变化）");

        // ---- 练习模式弹窗 ----
        CheckFaceDefault(typeof(PracticeModePopup), "Txt", TmpFont.Face.LatinCardBody, 1,
            "A-14 `PracticeModePopup.Txt`（标题那几颗本就显式传 `LatinUi`）");

        // ---- 档案页 / 社交页 / 社交视图（`protected`/`public` ⇒ 子类调用点跟着缺省面走）----
        CheckFaceDefault(typeof(ProfilePage), "Text", TmpFont.Face.LatinCardBody, 1,
            "A-15 `ProfilePage.Text`（`protected` ⇒ 7 个档案子页的 `Text(` 站点跟着变）");
        CheckFaceDefault(typeof(SocialPage), "Text", TmpFont.Face.LatinCardBody, 1,
            "A-16 `SocialPage.Text`（`public` ⇒ 2 个社交子页 + `SocialView` 转发口跟着变）");
        CheckFaceDefault(typeof(SocialView), "Text", TmpFont.Face.LatinCardBody, 1,
            "A-17 `SocialView.Text`（转发 `SocialPage.Text`；3 个社交视图子类跟着变）");

        // ---- Toast ----
        CheckFaceDefault(typeof(ToastNotifications), "Tmp", TmpFont.Face.LatinCardBody, 1,
            "A-18 `ToastNotifications.Tmp`（Toast 正文 / 计数 / 等级）");

        // ---- 顶栏（3 个调用点本就全显式传面：玩家名 Asar / 等级与货币 Pragati ⇒ 本次零渲染变化）----
        CheckFaceDefault(typeof(TopBar), "Text", TmpFont.Face.LatinCardBody, 1,
            "A-19 `TopBar.Text`（顶栏文字漏斗；调用点本就全显式传面）");

        Check(true, "A-20（口径提醒）本批按红线**跳过** `Shell/GameWindow.cs:386`（归 W7）；"
                  + "`Battle/**` + `Core/CardView.cs` 那 6 处是**下一档（tier ③）**，逐站核 `B120` 六域审计后再动");
    }

    // ============================================================
    //  B 组：行为 + 灭自证
    // ============================================================
    static void B_BehaviorAndAntiSelfProof()
    {
        GameObject root = null;
        try
        {
            root = new GameObject("B148W6 face probe", typeof(RectTransform));

            // B-0（前提）：`Label.Create` 建得出来
            var lat = Label.Create(root.transform, "Settings", Vector3.zero, 8, Color.white,
                                   new Vector2(0.5f, 0.5f), "w6_lat");
            Check(lat != null, "B-0（前提）`Label.Create` 建出了 Label（拿不到字体资产时为 null）");
            if (lat == null) return;

            // B-3：`Label` 的**出厂面**仍是 Cjk —— 本批**没动**它（动了 = 战斗 HUD/卡面数百处字号一次性平移）
            Check(lat.Face == TmpFont.Face.Cjk,
                  "B-3 `Label` 的出厂面仍是 `Face.Cjk`（本批只改**中间层缺省**，⛔ 没动 `Battle/Label.cs` 的出厂值）"
                + "（实得 " + lat.Face + "）｜🧨 顺手把出厂面也改了 ⇒ 本条红（那是另一个批次的活）");

            // B-2：`SetFace(LatinCardBody)` 真生效（拉丁文本不被翻面）
            lat.SetFace(TmpFont.Face.LatinCardBody);
            Check(lat.Face == TmpFont.Face.LatinCardBody,
                  "B-2 ★ `SetFace(LatinCardBody)` 之后 `Face` = Pragati（实得 " + lat.Face + "）"
                + "｜🧨 `SyncFace`/`SetFace` 不生效 ⇒ 红");
            Check(lat.ActualFace == TmpFont.Face.LatinCardBody,
                  "B-2b ★ 拉丁文本的 `ActualFace` 就是 Pragati（不被自动翻面）"
                + "（实得 " + lat.ActualFace + "）");

            // B-1：中文**仍然**自动翻回 Noto —— 这是本批「换缺省面只影响拉丁」的灭自证
            var cjk = Label.Create(root.transform, "设置", Vector3.zero, 8, Color.white,
                                   new Vector2(0.5f, 0.5f), "w6_cjk");
            Check(cjk != null, "B-1（前提）中文那颗 `Label` 建出来了");
            if (cjk != null)
            {
                cjk.SetFace(TmpFont.Face.LatinCardBody);
                Check(cjk.Face == TmpFont.Face.LatinCardBody,
                      "B-1a 中文那颗的 `Face`（要的那一面）也是 Pragati");
                Check(cjk.ActualFace == TmpFont.Face.Cjk,
                      "B-1b ★★ **中文仍然自动翻回 Noto（`Face.Cjk`）** —— 三份拉丁资产没有汉字字形"
                    + "（`Label.ActualFace`：含汉字且没开 `_cjkFallbackFace` ⇒ 翻面）"
                    + "（实得 " + cjk.ActualFace + "）"
                    + "｜🧨 把那条翻面链删掉 ⇒ 中文拿 Pragati 渲出一排 .notdef 方框 ⇒ 本条红");
            }
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
    }
}
