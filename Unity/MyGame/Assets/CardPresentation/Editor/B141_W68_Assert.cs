// B141_W68_Assert.cs — 执行写手 `B141-W68`（第一百二十八会话 · 2026-11-04）· W129 `B141-TEXTAREA-REST`
//
// ============================ 它盯的是什么 ============================
//  `W25`（`报告/B141_W129_TextInputPad.md`）把 `TextInputField.Create` 的 `/Text Area` 按原版 pad 裁好了，
//  并在 §四 列出**剩余站点**：三处裸节点（8 个 `Text Area`）+ `ReferralPopupWindow` 的 `Placeholder` 漏裁
//  + 三条走 `TextInputField.Bind` 的路（裁的是外层页视口、不是 `Text Area` 的 pad）。
//  本批把这三类**全部**改成「`Text Area` 节点挂 `ViewportClip`」。
//
//  ── 原版判据（逐条带出处）──
//   · pad / softness：`GenericPromptWindow.prefab:1337-1338`
//       `m_Padding: {x: -8, y: -5, z: -8, w: -5}` · `m_Softness: {x: 0, y: 0}`
//       —— 全库 **19 个 prefab / 38 处**同名 `/Text Area` 逐值同一（扫描 → `报告/B141_W129_TextInputPad.md` §1）。
//       ⇒ 这一族**只有一套参数**（铁律 5·c 已核），本文件不再按站点抄第二份。
//   · 站点矩形（`menu_dump.py … --md` 现读，**不是**从我们的常量反算）：
//       联盟搜索框 `Social Submenu Variant ▸ …/Search Field/Text Area` = **1406.57,172.90 → 1758.57,229.86**（352.00×56.96）
//       好友搜索框 `Friends Menu Demo ▸ …/Search Field/Text Area`  = **403.34,173.36 → 885.42,230.31**（482.08×56.95）
//       聊天输入   `ChatPanel ▸ …/InputField (TMP)/Text Area`       = 宽 **1120.00**、高 **26.53**（序列化高 = 0，
//                  运行期由 CSF/VLG 定成 `Placeholder` 那一档 ⇒ 与 `Shell/ChatPanel.cs` 的 `InputR` 同值）
//   · 「掩码挂在内层 `Text Area`」：`Give Feedback Popup menu.prefab` 现读
//       `S2Q4 ▸ Text Area`（带 `Image`/`40K_dropdown_bg`）▸ `Text Area`（**挂 `RectMask2D`**）▸ `Placeholder` / `description text`
//       自由反馈那格：`Input field`（带 `Image`）▸ `Text Area`（**挂 `RectMask2D`**）▸ `Placeholder`
//
// ============================ 断言（绿跑共 55 条：A 36 · B 19）============================
//  ⚠️ 另有 7 条「这份源码读得到」的前提（`B0a`–`B0g`）—— 它们**只在读不到时报红**（绿跑不计数），
//     所以控制台合计会是 55；读不到时会先多出一条 ✗ 再 return（⛔ 不许静默跳过）。
//  A 组（运行时夹具 · 用完即销毁）—— 机制 + 原版矩形逐值：
//    A0×2  前提：临时根 / 三颗视口节点都建得出来
//    A2–A5 ★ pad 四分量 = **字面量** (−8,−5,−8,−5)（⛔ 不拿 `TextInputField.AreaPad` 比自己）
//    A6    ★ softness = (0,0)
//    A7a–A7d ★ 联盟那格 `BaseRect` = 原版 dump 那四个数
//    A8a–A8d ★ 联盟那格 `RenderClip` = 上面再各外扩 8/5（−8/−5 负 pad）
//    A9    ★ 结构性判别式：`RenderClip.W − BaseRect.W` = **16**、`.H − .H` = **10**（负 pad 的必然结果）
//    A10a–A10d ★ 好友那格 `BaseRect` = 原版 dump 那四个数
//    A11   ★ 聊天那格 `BaseRect` 的 W/H = **1120.00 / 26.53**（原版那两格）
//    A12a–A12b ★ 聊天那格 `RenderClip` = 1136.00 × 36.53
//    A13   ★ 挂在节点下的 `Label` **真**拿到 `ClippedTextGuard`（= 父链解析这条路带电）
//    A14   🧨 灭自证：同一颗字、**父链上没有节点** ⇒ 没有守卫（⇒ A13 不是恒真）
//    A15   🧨 灭自证：节点在 + **显式 `clip:`** ⇒ `ViewportClip.NodeShadowedByParam` **+1**
//            （这正是本批为什么要删掉 `clip: area`：形参非空 ⇒ 节点白挂、静默）
//  B 组（源码结构 · 每个站点一条 · 数**非注释行**）：
//    B0    前提：7 份源文件都读得到（读不到时源码闸等于没验 ⇒ 当场红）
//    B1–B3 `Shell/SettingsWindow.cs`（账号页/登录窗 4 处，同一个 `AcInput.Create`）：Hang 恰 1 · 旧 `Node` 0 · `clip: area` 0
//    B4–B5 `Shell/ProfileTab.cs`：Hang 恰 1 · 旧 `Node(fld, "Text Area"` 0
//    B6–B9 `Shell/GiveFeedbackPopup.cs`：自由反馈 Hang 恰 1 · 开题 Hang 恰 1 · 旧 `Node(inputNode,…)` 0 ·
//          外层带 `Image` 的那颗**仍是裸节点**（原版那颗**没有** `RectMask2D`，⛔ 别给它也挂掩码）
//    B10   `Shell/ReferralPopupWindow.cs`：`Placeholder` 的 `ClipText` 恰 1（`Text` 那两颗仍是 2：建时 + 每次刷新）
//    B11–B13 `Shell/ChatPanel.cs`：Hang 恰 1 · 两颗字挂 `inputArea` 恰 2 · 旧的 `Text(input, InputR` 0
//    B14–B16 `Shell/AllianceSearchTab.cs`：同上（`sfArea` / `SearchPhR`）
//    B17–B19 `Shell/FriendsTab.cs`：同上（`sfArea` / `PlaceholderR`）
//
// ============================ 🧨 什么坏法会让哪条红 ============================
//  · 任何一处把它改回 `MenuDraw.Node(...)` / `Node(...)` ⇒ 对应的 B 组「Hang 恰 1」+「旧写法 0」两条同时红。
//  · 任何一处把两颗字的父件改回去（`Text(input, …)` / `Text(sf, …)`）⇒ 对应的「挂 `xxx` 恰 2」与「旧写法 0」红。
//  · `SettingsWindow` 把 `clip: area` 加回来 ⇒ B3 红（且 A15 那条计数器会说话）。
//  · `ReferralPopupWindow` 删掉那句 `ClipText(_placeholder, …)` ⇒ B10 红。
//  · pad 改成 0 / 正值 ⇒ A2–A5 + A7/A8/A9/A11/A12 一片红（**不共因**：「值对不对」与「外扩量对不对」分开断）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主聚合）============================
//  `public static void Run();` —— **零前提**（自建临时根节点 + 读 7 份源码，用完即销毁；不写盘、不碰存档）。
//  调度台在宿主合计行之前、**同一物理行、该行第一个 `//` 之前**追加：
//      B141W68_Assert.Run(); _sink.Pass += B141W68_Assert.Pass; _sink.Fail += B141W68_Assert.Fail;
//  判绿红：按行首 `B141W68 ✓` / `B141W68 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 也出现在断言文案里）。
//  ⚠️ 它会多记若干次 `ViewportClip.NodeResolutions` / `NodeShadowedByParam`（走节点态/形参盖节点）——
//     全仓对这两个计数只有「> 0」「差分」两种断法（`Editor/RewardsScene.cs:5170` 那条 `== 0` 断的是
//     **无节点的场景里**的累计值，本断言在**自己的临时根**上跑、不进那一棵）⇒ 与 `B141_W25_Assert` 同一条纪律。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B141_W129_TextAreaRest.md`
//  · 判据：`资料/完全复刻/判据/B141_W129_对账_UI动画.md` W11 包（条目 `4-23`）＋ `报告/B141_W129_TextInputPad.md` §四/§七

using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141-W68`：`/Text Area` 族**剩余站点**（三处裸节点 + Referral 的 Placeholder + 三条 `Bind` 路）的回归断言。</summary>
public static class B141W68_Assert
{
    const string Tag = "B141W68";
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void C(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void N(float got, float want, float tol, string msg)
    {
        C(Mathf.Abs(got - want) <= tol, msg + "（实得 " + got.ToString("F4") + "，期望 " + want.ToString("F4") + "）");
    }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        A_Mechanism();
        B_Sites();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 运行时夹具：三颗【原版矩形】的 `Text Area` 掩码
    // ==================================================================
    //  期望值来源 = **原版 dump 字面量**（见文件头），⛔ 不是从被测常量反算。
    //    联盟 `Social Submenu Variant ▸ Search Field/Text Area`：1406.57,172.90 → 1758.57,229.86
    //      ⇒ 渲染框（负 pad 外扩 8/5）= 1398.57,167.90 → 1766.57,234.86
    //    好友 `Friends Menu Demo ▸ Search Field/Text Area`：403.34,173.36 → 885.42,230.31
    //    聊天 `ChatPanel ▸ InputField (TMP)/Text Area`：宽 1120.00、高 26.53（x 653.88→1773.88，与 `InputR` 同值）
    static readonly PxRect AllyR = new PxRect(1406.57f, 172.90f, 1758.57f, 229.86f);
    static readonly PxRect FrR = new PxRect(403.34f, 173.36f, 885.42f, 230.31f);
    static readonly PxRect ChatR = new PxRect(653.88f, 979.47f, 1773.88f, 1006.00f);

    static void A_Mechanism()
    {
        GameObject root = null, plain = null;
        try
        {
            root = new GameObject("B141W68Fixture", typeof(RectTransform));
            C(root != null && root.transform != null, "A0（前提）临时根节点建出来了");
            if (root == null) return;

            var ally = ViewportClip.Hang(root.transform, "Text Area", AllyR,
                                         TextInputField.AreaPad, TextInputField.AreaSoftness);
            var fr = ViewportClip.Hang(root.transform, "Text Area (Friends)", FrR,
                                       TextInputField.AreaPad, TextInputField.AreaSoftness);
            var chat = ViewportClip.Hang(root.transform, "Text Area (Chat)", ChatR,
                                         TextInputField.AreaPad, TextInputField.AreaSoftness);
            C(ally != null && fr != null && chat != null,
              "A0（前提）三颗 `Text Area` 视口节点都建出来了（`ViewportClip.Hang`）｜🧨 退回 `MenuDraw.Node` ⇒ 红");
            if (ally == null || fr == null || chat == null) return;

            // ---- ★ pad 逐分量 = 原版字面量（⛔ 不读 `TextInputField.AreaPad` —— 那会与该常量一起被改坏而全绿）----
            N(ally.padding.x, -8f, 0.0001f, "A2 ★ `m_Padding.x`（Left）= **−8**（负 = 往外扩）｜🧨 改成 0 ⇒ 红");
            N(ally.padding.y, -5f, 0.0001f, "A3 ★ `m_Padding.y`（Bottom）= **−5**｜🧨 改成 0 ⇒ 红");
            N(ally.padding.z, -8f, 0.0001f, "A4 ★ `m_Padding.z`（Right）= **−8**｜🧨 改成 0 ⇒ 红");
            N(ally.padding.w, -5f, 0.0001f, "A5 ★ `m_Padding.w`（Top）= **−5**｜🧨 改成 0 ⇒ 红");
            C(ally.softness.x == 0 && ally.softness.y == 0,
              "A6 ★ `m_Softness` = **(0,0)**（原版 38/38 都是硬边）｜实得 (" + ally.softness.x + "," + ally.softness.y + ")");

            // ---- ★ 联盟那格：框 = 原版 dump 那四个数；渲染框 = 再外扩 8/5 ----
            C(ally.HasBaseRect, "A7（前提）联盟那颗记下了设计矩形（`HasBaseRect` ⇒ 框与被比矩形同一帧）");
            if (ally.HasBaseRect)
            {
                var b = ally.BaseRect;
                N(b.x1, 1406.57f, 0.02f, "A7a ★ 联盟 `Text Area` 左沿 = **1406.57**（原版 dump）");
                N(b.y1, 172.90f, 0.02f, "A7b ★ 上沿 = **172.90**");
                N(b.x2, 1758.57f, 0.02f, "A7c ★ 右沿 = **1758.57**");
                N(b.y2, 229.86f, 0.02f, "A7d ★ 下沿 = **229.86**");
            }
            var arc = ally.State.RenderClip;
            C(arc.HasValue, "A8（前提）`State.RenderClip` 非空（= 这颗掩码真会裁）");
            if (arc.HasValue)
            {
                var c = arc.Value;
                N(c.x1, 1398.57f, 0.02f, "A8a ★ 渲染框左沿 = **1398.57**（1406.57 − 8，负 pad 往外扩）｜🧨 pad 改 0 ⇒ 1406.57 ⇒ 红");
                N(c.y1, 167.90f, 0.02f, "A8b ★ 上沿 = **167.90**（172.90 − 5）｜🧨 pad 改 0 ⇒ 红");
                N(c.x2, 1766.57f, 0.02f, "A8c ★ 右沿 = **1766.57**（1758.57 + 8）｜🧨 pad 改 0 ⇒ 红");
                N(c.y2, 234.86f, 0.02f, "A8d ★ 下沿 = **234.86**（229.86 + 5）｜🧨 pad 改 0 ⇒ 红");
                // 结构性判别式（与 A2–A5 不共因）
                float bw = 1758.57f - 1406.57f, bh = 229.86f - 172.90f;
                N(c.W - bw, 16f, 0.02f, "A9 ★ 渲染框宽 − 节点宽 = **16**（左右各 8 —— 负 pad 的必然结果）");
                N(c.H - bh, 10f, 0.02f, "A9 ★ 渲染框高 − 节点高 = **10**（上下各 5）");
            }

            // ---- ★ 好友那格 ----
            C(fr.HasBaseRect, "A10（前提）好友那颗记下了设计矩形");
            if (fr.HasBaseRect)
            {
                var b = fr.BaseRect;
                N(b.x1, 403.34f, 0.02f, "A10a ★ 好友 `Text Area` 左沿 = **403.34**（原版 dump）");
                N(b.y1, 173.36f, 0.02f, "A10b ★ 上沿 = **173.36**");
                N(b.x2, 885.42f, 0.02f, "A10c ★ 右沿 = **885.42**");
                N(b.y2, 230.31f, 0.02f, "A10d ★ 下沿 = **230.31**");
            }

            // ---- ★ 聊天那格（原版序列化高 = 0，运行期 = Placeholder 那一档 26.53）----
            C(chat.HasBaseRect, "A11（前提）聊天那颗记下了设计矩形");
            if (chat.HasBaseRect)
            {
                var b = chat.BaseRect;
                N(b.W, 1120.00f, 0.02f, "A11 ★ 聊天 `Text Area` 宽 = **1120.00**（原版 dump）");
                N(b.H, 26.53f, 0.02f, "A11 ★ 高 = **26.53**（= 原版 `Placeholder` 那一档）");
            }
            var crc = chat.State.RenderClip;
            C(crc.HasValue, "A12（前提）聊天那颗 `RenderClip` 非空");
            if (crc.HasValue)
            {
                N(crc.Value.W, 1136.00f, 0.02f, "A12a ★ 聊天渲染框宽 = **1136.00**（1120 + 16）｜🧨 pad 改 0 ⇒ 1120 ⇒ 红");
                N(crc.Value.H, 36.53f, 0.02f, "A12b ★ 聊天渲染框高 = **36.53**（26.53 + 10）｜🧨 pad 改 0 ⇒ 红");
            }

            // ---- ★ 父链解析这条路真带电：节点下的标签会拿到 `ClippedTextGuard` ----
            var lbInNode = MenuDraw.Text(ally.transform, AllyR, "W68", Color.white, "ProbeUnderNode", 20f, 3011);
            C(lbInNode != null, "A13（前提）节点下建得出 `Label`");
            C(lbInNode != null && lbInNode.GetComponent<ClippedTextGuard>() != null,
              "A13 ★ 节点下那颗字拿到 `ClippedTextGuard`（= `MenuDraw.Text` 的 `clip: null` 沿父链解析到 `ViewportClip`）"
            + "｜🧨 把父件换成没有节点的根 ⇒ 红");

            // ---- 🧨 灭自证①：父链上没有节点 ⇒ 不该有守卫（否则 A13 恒真）----
            plain = new GameObject("B141W68Plain", typeof(RectTransform));
            var lbPlain = MenuDraw.Text(plain.transform, AllyR, "W68", Color.white, "ProbeNoNode", 20f, 3011);
            C(lbPlain != null, "A14（前提）裸根下建得出 `Label`");
            C(lbPlain != null && lbPlain.GetComponent<ClippedTextGuard>() == null,
              "A14 ★ 裸根下那颗字**没有** `ClippedTextGuard`（不裁）｜🧨 若它也拿到守卫 ⇒ A13 那条恒真、没有判别力");

            // ---- 🧨 灭自证②：节点在 + 显式 `clip:` ⇒ 走「形参赢」那一支（计数器 +1）----
            //   这正是本批必须删掉 `Shell/SettingsWindow.cs` 里 `clip: area` 的理由。
            //   ⚠️ 断完**把计数器还原**：`Editor/RewardsScene.cs:5170` 有一条 `NodeShadowedByParam == 0` 的
            //      全局断言（那是「无节点的场景里不该出现形参盖节点」）—— 本夹具是**故意**造出那一档，
            //      不还原就会把它弄红（那不是它的缺陷，是我们的探针）。
            int shadow0 = ViewportClip.NodeShadowedByParam;
            var lbShadow = MenuDraw.Text(ally.transform, AllyR, "W68", Color.white, "ProbeShadow", 20f, 3011, clip: AllyR);
            C(lbShadow != null, "A15（前提）显式 `clip:` 那颗建得出来");
            int shadowDelta = ViewportClip.NodeShadowedByParam - shadow0;
            C(shadowDelta > 0,
              "A15 ★ 显式 `clip:` 盖住节点 ⇒ `NodeShadowedByParam` **+1**（形参赢、节点白挂 —— 静默）"
            + "（实测 +" + shadowDelta + "）｜🧨 把 `SettingsWindow` 的 `clip: area` 加回来就落进这一档（B3 也咬）");
            ViewportClip.NodeShadowedByParam = shadow0;   // ← 还原（理由见上）
        }
        catch (Exception e)
        {
            C(false, "A 段抛异常（不当成通过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (plain != null) UnityEngine.Object.DestroyImmediate(plain);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  B · 源码结构：每个站点一条（数**非注释行**）
    // ==================================================================
    static void B_Sites()
    {
        string st = Read("CardPresentation/Shell/SettingsWindow.cs", "B0a");
        string pf = Read("CardPresentation/Shell/ProfileTab.cs", "B0b");
        string gf = Read("CardPresentation/Shell/GiveFeedbackPopup.cs", "B0c");
        string rf = Read("CardPresentation/Shell/ReferralPopupWindow.cs", "B0d");
        string cp = Read("CardPresentation/Shell/ChatPanel.cs", "B0e");
        string al = Read("CardPresentation/Shell/AllianceSearchTab.cs", "B0f");
        string fr = Read("CardPresentation/Shell/FriendsTab.cs", "B0g");
        if (st == null || pf == null || gf == null || rf == null || cp == null || al == null || fr == null) return;

        // ---- ① `Shell/SettingsWindow.cs`（账号页 2 + 登录窗 2，同一个 `AcInput.Create`）----
        One(st, "ViewportClip.Hang(f.Root, \"Text Area\", area,", 1,
            "B1 ★ 设置窗 `AcInput` 的 `Text Area` 走 `ViewportClip.Hang`（恰 1 处 ⇒ 4 个实例都吃它）");
        Zero(st, "MenuDraw.Node(f.Root, \"Text Area\"",
            "B2 ★ 旧的「只建节点」写法已消失（`MenuDraw.Node(f.Root, \"Text Area\"` 0 处）");
        Zero(st, "clip: area",
            "B3 ★ 两颗字的**显式** `clip: area` 已删（形参非空 ⇒ `Resolve` 第 1 支 ⇒ 节点白挂、静默）");

        // ---- ② `Shell/ProfileTab.cs`（改名窗）----
        One(pf, "ViewportClip.Hang(fld, \"Text Area\"", 1,
            "B4 ★ 档案页改名窗的 `Text Area` 走 `ViewportClip.Hang`（恰 1 处）");
        Zero(pf, "Node(fld, \"Text Area\"",
            "B5 ★ 旧的裸节点写法已消失（`Node(fld, \"Text Area\"` 0 处）");

        // ---- ③ `Shell/GiveFeedbackPopup.cs`（自由反馈 1 + 开放题 3 个实例，同一个 `BuildOpen`）----
        One(gf, "ViewportClip.Hang(inputNode, \"Text Area\", InputTextR", 1,
            "B6 ★ 自由反馈那格的 `Text Area` 走 `ViewportClip.Hang`（恰 1 处）");
        One(gf, "ViewportClip.Hang(box, \"Text Area\", inner", 1,
            "B7 ★ 开放题那格的**内层** `Text Area` 走 `ViewportClip.Hang`（恰 1 处 ⇒ 3 个实例都吃它）");
        Zero(gf, "MenuDraw.Node(inputNode, \"Text Area\"",
            "B8 ★ 自由反馈那格旧的裸节点写法已消失");
        One(gf, "MenuDraw.Node(node, \"Text Area\", areaR", 1,
            "B9 ★ 开放题**外层**那颗 `Text Area` 仍是裸节点（原版那颗带 `Image`、**没挂** `RectMask2D`）"
          + "｜🧨 给外层也挂掩码 ⇒ 0 处 ⇒ 红");

        // ---- ④ `Shell/ReferralPopupWindow.cs`（Placeholder 漏裁）----
        One(rf, "MenuDraw.ClipText(_placeholder, TextClipRect(), Vector2.zero)", 1,
            "B10 ★ 邀请窗 `Placeholder` 也按同一份 `TextClipRect()` 裁（前只有 `Text` 那颗裁了）"
          + "｜🧨 删掉这一句 ⇒ 0 处 ⇒ 红");

        // ---- ⑤ `Shell/ChatPanel.cs`（Bind 路）----
        One(cp, "ViewportClip.Hang(input, \"Text Area\", InputR", 1,
            "B11 ★ 聊天输入行的 `Text Area` 挂上了 `ViewportClip`（恰 1 处）");
        One(cp, "Text(inputArea, InputR", 2,
            "B12 ★ `Placeholder` / `Text` 两颗字都挂在 `inputArea`（恰 2 处）｜🧨 只改一颗 ⇒ 1 处 ⇒ 红");
        Zero(cp, "Text(input, InputR",
            "B13 ★ 旧写法（直接挂 `InputField (TMP)`）已消失");

        // ---- ⑥ `Shell/AllianceSearchTab.cs`（Bind 路）----
        One(al, "ViewportClip.Hang(sf, \"Text Area\", SearchPhR", 1,
            "B14 ★ 联盟搜索框的 `Text Area` 挂上了 `ViewportClip`（恰 1 处，矩形 = 原版 1406.57→1758.57）");
        One(al, "Text(sfArea, SearchPhR", 2,
            "B15 ★ 两颗字都挂在 `sfArea`（恰 2 处）｜🧨 只改一颗 ⇒ 红");
        Zero(al, "Text(sf, SearchPhR",
            "B16 ★ 旧写法（直接挂 `Search Field`）已消失");

        // ---- ⑦ `Shell/FriendsTab.cs`（Bind 路）----
        One(fr, "ViewportClip.Hang(sf, \"Text Area\", PlaceholderR", 1,
            "B17 ★ 好友搜索框的 `Text Area` 挂上了 `ViewportClip`（恰 1 处，矩形 = 原版 403.34→885.42）");
        One(fr, "Text(sfArea, PlaceholderR", 2,
            "B18 ★ 两颗字都挂在 `sfArea`（恰 2 处）｜🧨 只改一颗 ⇒ 红");
        Zero(fr, "Text(sf, PlaceholderR",
            "B19 ★ 旧写法（直接挂 `Search Field`）已消失");
    }

    // ------------------------------------------------------------ 工具
    /// <summary>读一份源码；读不到 ⇒ 当场红（源码闸读不到 = 等于没验，⛔ 不许静默放过）。</summary>
    static string Read(string rel, string id)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel);
            if (!File.Exists(p))
            {
                C(false, id + " 读得到源码 `" + rel + "`（" + p + "）—— 读不到时源码闸**等于没验** ⇒ 当场红");
                return null;
            }
            return File.ReadAllText(p);
        }
        catch (Exception e)
        {
            C(false, id + " 读源码 `" + rel + "` 抛异常：" + e.GetType().Name + "：" + e.Message);
            return null;
        }
    }

    static void One(string src, string needle, int want, string msg)
    {
        int got = CountOfCode(src, needle);
        C(got == want, msg + "（实得 " + got + " · 期望 " + want + "）");
    }

    static void Zero(string src, string needle, string msg)
    {
        int got = CountOfCode(src, needle);
        C(got == 0, msg + "（实得 " + got + " · 期望 0）");
    }

    /// <summary>数**非注释行**里的子串出现次数（整行 `//` 开头的跳过 —— 本仓注释里大量出现这些字面量）。
    /// 口径逐字照 `B141_W25_Assert.CountOfCode`（同一工程、同一条纪律，⛔ 别另写一套）。</summary>
    static int CountOfCode(string src, string needle)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(needle)) return -1;
        int n = 0;
        foreach (var raw in src.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var t = line.TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;
            int at = 0;
            while ((at = line.IndexOf(needle, at, StringComparison.Ordinal)) >= 0) { n++; at += needle.Length; }
        }
        return n;
    }
}
