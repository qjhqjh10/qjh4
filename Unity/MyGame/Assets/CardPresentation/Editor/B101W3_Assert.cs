// B101W3_Assert.cs — 执行代理 `B101_W3`（第一百零一会话 · `AUD01` 普查查出的三条**视觉参数缺口**）
//
// ============================ 它盯的是什么 ============================
//  判据全文 → `资料/完全复刻/报告/B101_P1_prefab视觉参数普查.md` §2·1 / §2·2 / §3·1~§3·3
//  逐条数据 → `资料/完全复刻/判据/B101_P1_prefab视觉参数.tsv`
//
//   · A 段 **`Avatar Item Small` 三层倍率**（`Shell/AllianceMemberTab.cs` 的成员行头像）：
//     `Highlight ×1.9` / `Border ×1.25` / `Image ×2.0` —— 原来三处各自内联一个 `PxRect`、
//     **倍率一个都没乘** ⇒ 成员行立绘只画了原版的 **50%**（98.9×65.7 vs 197.75×131.39）。
//     修法 = `AllianceMemberRowAvatarGeo.{HighlightRect,ImageRect,BorderRect}`（绕各自布局框中心放大，
//     转调 `Shell/AvatarTab.cs` 的 `ScaleAboutCenter` + `ScaleImage/ScaleBorder/ScaleHighlight`）。
//   · B 段 **战斗侧联盟面板的立绘**（`Shell/AlliancePanelWindow.cs:284` 的 `AvatarImageR`）：
//     原来是**未缩放的布局框** 170.0×119.2；原版 `Image.m_LocalScale = (2,2,1)` ⇒ 视觉 **340.05×238.43**。
//   · C 段 **`Deck Editing Menu ▸ Footer ▸ Done ▸ Done Highlight` 的 90°**（`Deck/DeckRuntime.cs:697`）：
//     原版 `m_LocalRotation.z = 90.000°` ⇒ 视觉是 **223.22×81.36 的横条**；我们原来画成了**竖条**
//     （81.4×223.2，y 到 1157 ⇒ 戳出 1080 高的窗口）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  三段各有一条「判别式」，与「期望值」那条**互相独立** —— 只改一处不改另一处照样红：
//   · A：`AllianceMemberRowAvatarGeo.HighlightRect` 把 `AvatarTab.ScaleHighlight` 换成 `1f`
//        ⇒ 视觉宽 194.20 → **102.21**（未缩放的布局框）⇒ A1 / A2 / A3 三条同时红。
//     把 `AvatarTab.ScaleImage` 换成 `1f` ⇒ A4 红；`ScaleBorder` 换成 `1f` ⇒ A7 红。
//   · B：`AvatarImageR` 的 `AvatarTab.ScaleImage` 换成 `1f` ⇒ 宽 340.0 → **170.0** ⇒ B1 / B3 红。
//   · C：`DoneHlW / DoneHlH` 换回 `81.4 / 223.2`（= 旋转前的字段值）⇒ C1 / C3 红（横条判据翻转）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  🔴 三处落点全在**壳侧**，本体是**纯几何/纯常量**、不建窗 ⇒ 接在 `CardPresentation/Editor/ShellScene.cs`
//  的**方法尾聚合行**（现读 `:11023`，`Debug.Log(P + $"=== 合计：…` 之前）**同一物理行追加**（行数零变化）：
//      B101W3_Assert.Run(); _sink.Pass += B101W3_Assert.Pass; _sink.Fail += B101W3_Assert.Fail;
//  ⚠️ 它**不需要**任何 Transform / Canvas：读的全是 `public const` / `public static readonly` 常量与纯函数
//  ⇒ 在 `BatchMode` 下也能跑（区别于那些要建窗的断言）。
//
// ============================ 判据 / 报告 ============================
//  · 原版：`bundle_menus_assets_all` 的 `RectTransform.m_LocalScale`（9/9 实例同值）+
//          `Deck Editing Menu ▸ … ▸ Done Highlight` 的 `m_LocalScale` / `m_LocalRotation`
//  · 报告：`资料/完全复刻/报告/B101_W3_AUD01三处修复.md`
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B101_W3`：`AUD01` 三条视觉参数缺口（成员行头像三层 / 联盟面板立绘 / Done 外发光 90°）。</summary>
public static class B101W3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B101W3] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>唯一的入口（宿主 `ShellScene` 的聚合行里调）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B101_W3：AUD01 三条视觉参数缺口（成员行头像 ×1.9/×1.25/×2 · 联盟面板立绘 ×2 · Done 外发光 90°）");
        A_MemberRowAvatarScale();
        B_AlliancePanelAvatar();
        C_DeckDoneHighlightRotation();
    }

    // ==================================================================
    //  A 段：`Alliance Member Entry` 成员行头像的三层倍率（报告 §2·1 / §3·1）
    // ==================================================================
    //  夹具 = 报告 §A·2·3 那张表的根尺寸（行 **750×100**、左上原点）
    //  ⇒ `av` = `(r.x1+50.57, r.y1+11.87, r.x1+149.44, r.y1+114.93)`（与 `AllianceMemberRow.Build:1905` 同一式）。
    static void A_MemberRowAvatarScale()
    {
        // 前提（硬）：`av` 的宽高必须就是 `Avatar Item Small` 的 98.87 × 103.06 —— 不成立时下面全都不值得看
        // （prefab 实读 `m_SizeDelta = 98.877 × 103.062`、`m_AnchoredPosition = (100, −13.4)`、锚 `(0,0.5)`）。
        var av = new PxRect(50.57f, 11.87f, 149.44f, 114.93f);
        N(av.W, 98.87f, 0.01f, "A0（前提）`Avatar Item Small` 宽 = 98.87（行 750 宽 · 左 50.57 / 右 149.44）");
        N(av.H, 103.06f, 0.01f, "A0（前提）`Avatar Item Small` 高 = 103.06（上 11.87 / 下 114.93）");

        // ---- `Highlight`（图 `Player_Avatar_selected`）×1.9 ----
        // 布局框 = `(av.x1, av.y1−3.53, av.x2+3.34, av.y2−36.37)` ⇒ 102.21 × 70.22
        // ⚠️ **宽度**与 prefab 五元组实算逐位吻合（98.877+3.3384 = 102.215），**高度不吻合**：
        //    实算 = 65.694+1.8547 = **67.55**（y 8.34..75.89），我们沿用旧布局框的 70.22 ⇒ 见报告 §没查清 ①。
        //    本批**只加倍率、不动布局**（简报口径）⇒ 这里只断宽度与比例。
        var hl = AllianceMemberRowAvatarGeo.HighlightRect(av);
        const float hlLayoutW = 102.21f;
        N(hl.W, 194.20f, 0.10f, "A1 `Highlight` 视觉宽 = **194.20**（= 布局框 102.21 × 1.9，原版普查 §2·1）");
        N(hl.W / hlLayoutW, 1.9f, 0.0005f, "A2 `Highlight` 视觉/布局 宽比 = **1.9**（prefab `m_LocalScale`）");
        T(hl.W > hlLayoutW * 1.5f,
          "A3【判别式】`Highlight` 视觉宽 > 布局宽 × 1.5 —— 把倍率改回 `1f` ⇒ 194.20 变 102.21 ⇒ 这条红");
        N(hl.CX, 101.675f, 0.01f, "A3b `Highlight` 绕**布局框中心**放大（中心不动；布局框左右不对称（右多 3.34）⇒ 中心 101.675，⛔ 不是 `av` 的 100.005）");

        // ---- `Image`（立绘）×2.0 ----
        // 布局框 = `(av.x1, av.y1−2.70, av.x2, av.y1+62.99)` ⇒ 98.87 × 65.69
        var im = AllianceMemberRowAvatarGeo.ImageRect(av);
        const float imLayoutW = 98.87f;
        N(im.W, 197.75f, 0.10f, "A4 `Image`（立绘）视觉宽 = **197.75**（= 98.87 × 2.0，原版普查 §2·1）");
        N(im.H, 131.39f, 0.10f, "A5 `Image`（立绘）视觉高 = **131.39**（= 65.69 × 2.0）");
        T(im.W > imLayoutW * 1.5f,
          "A6【判别式】立绘视觉宽 > 布局宽 × 1.5 —— 改回 `1f` ⇒ 197.75 变 98.87（= 用户报的「立绘只有一半」）⇒ 红");
        N(im.CX, 100.005f, 0.01f, "A6b `Image` 绕布局框中心放大（中心不动 = `(50.57+149.44)/2`）");

        // ---- `Border`（图 `Player_Profile_Border`）×1.25 ----
        // 布局框 = `(av.x1, av.y1+6.57, av.x2, av.y1+72.26)` ⇒ 98.87 × 65.69
        var bd = AllianceMemberRowAvatarGeo.BorderRect(av);
        const float bdLayoutW = 98.87f;
        N(bd.W, 123.60f, 0.10f, "A7 `Border` 视觉宽 = **123.60**（= 98.87 × 1.25，原版普查 §2·1）");
        N(bd.W / bdLayoutW, 1.25f, 0.0005f, "A8 `Border` 视觉/布局 宽比 = **1.25**（prefab `m_LocalScale`）");
        T(bd.W > bdLayoutW * 1.1f,
          "A9【判别式】`Border` 视觉宽 > 布局宽 × 1.1 —— 改回 `1f` ⇒ 123.60 变 98.87 ⇒ 红");

        // ---- 画序（兄弟序）唯一判据：立绘必须**最大**（放大后压住框；反了会整块黑）----
        T(im.W > bd.W && bd.W > 0f,
          "A10 立绘视觉宽（197.75）> 框视觉宽（123.60）—— 三者重叠更多 ⇒ ⛔ 画序必须 `Highlight → Border → Image`");
    }

    // ==================================================================
    //  B 段：战斗侧 `Alliance Panel` 的立绘 ×2（报告 §3·2）
    // ==================================================================
    static void B_AlliancePanelAvatar()
    {
        // 原版：容器 170.02×119.21（`Image Container`）× `Image.m_LocalScale = 2`（9/9 实例同值）
        // 我们的布局框 = `(10.0, 60.9)-(180.0, 180.1)`（容器 63.6..182.8 **上移 2.7** = `Image.apos.y`）
        var r = AlliancePanelWindow.AvatarImageR;
        N(r.W, 340.05f, 0.10f, "B1 联盟面板立绘视觉宽 = **340.05**（= 170.0 × 2；普查 340.05，差 0.05 = 容器宽 170.0 vs 170.02）");
        N(r.H, 238.43f, 0.10f, "B2 联盟面板立绘视觉高 = **238.43**（= 119.21 × 2）");
        N(r.CX, 95.00f, 0.05f, "B3 中心 x = 95.00（容器中心，未被倍率推动 ⇒ 是「绕中心放大」不是「从左上放大」）");
        N(r.CY, 120.50f, 0.05f, "B4 中心 y = 120.50（= 容器中心 123.2 − `apos.y` 2.7）");
        T(r.W > 300f,
          "B5【判别式】视觉宽 > 300 —— 用**未缩放的布局框**（170.0）时这条与 B1 同时红（那正是修前的形状）");
    }

    // ==================================================================
    //  C 段：`Deck Editing Menu ▸ Footer ▸ Done ▸ Done Highlight` 的 90°（报告 §2·2 / §3·3）
    // ==================================================================
    static void C_DeckDoneHighlightRotation()
    {
        // 原版：`m_SizeDelta = 81.36×223.22` + `m_LocalRotation.z = 90.000°` ⇒ 视觉 **223.22×81.36 横条**
        // 我们的口径 =「把旋转乘进矩形」（先例 `AvatarTab.ScaleAboutCenter`）⇒ 常量直接写视觉框。
        N(DeckRuntime.DoneHlW, 223.22f, 0.10f, "C1 `Done Highlight` 视觉宽 = **223.22**（原版 `m_SizeDelta.y`，旋转 90° 后变成宽）");
        N(DeckRuntime.DoneHlH, 81.36f, 0.10f, "C2 `Done Highlight` 视觉高 = **81.36**（原版 `m_SizeDelta.x`，旋转 90° 后变成高）");
        T(DeckRuntime.DoneHlW > DeckRuntime.DoneHlH,
          "C3【判别式】视觉宽度 > 高度（= **横条**）—— 换回旋转前的 81.4×223.2 ⇒ 翻转成竖条 ⇒ 红（那正是修前的形状）");
        N(DeckRuntime.DoneHlX + DeckRuntime.DoneHlW * 0.5f, 107.90f, 0.05f,
          "C4 中心 x = **107.90** 不动（= 原版 `Done` 中心 107.25 那一族；换 w/h 时 x **必须跟着改**，否则整条横条平移）");
        N(DeckRuntime.DoneHlY + DeckRuntime.DoneHlH * 0.5f, 1045.10f, 0.05f,
          "C5 中心 y = **1045.10** 不动（= `Done` 钮 1020.5..1070.7 的中心 1045.6，差 0.5 是原版自己的）");
        // 原版 `Done` 钮 = `(13, 1020.5)-(201.5, 1070.7)`（`DeckRuntime.DoneX/DoneY/DoneW/DoneH`，R:159），
        // 外发光必须**罩住**它 ⇒ 这两条一起才说明「横条是围着钮的」而不是随便摆一条横线。
        const float doneL = 13f, doneT = 1020.5f, doneR = 201.5f, doneB = 1070.7f;
        T(DeckRuntime.DoneHlX <= doneL && DeckRuntime.DoneHlX + DeckRuntime.DoneHlW >= doneR,
          "C6【判别式】视觉框**横向罩住** `Done` 钮（x −3.7..219.5 ⊇ 13..201.5）—— 旋转前那条竖条 x 只有 67.2..148.6 ⇒ 这条红");
        T(DeckRuntime.DoneHlY <= doneT && DeckRuntime.DoneHlY + DeckRuntime.DoneHlH >= doneB,
          "C7 视觉框**纵向罩住** `Done` 钮（y 1004.4..1085.8 ⊇ 1020.5..1070.7；⚠️ 底沿 1085.8 **超出 1080 画布** —— 原版就这样，照抄）");
    }
}
