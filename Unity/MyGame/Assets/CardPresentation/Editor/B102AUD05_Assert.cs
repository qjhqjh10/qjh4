// B102AUD05_Assert.cs — 执行代理 `B102_AUD05`（第一百零二会话 · `AUD02` 普查查出的 B 档）：**聊天预览底板缺 `fillAmount`**
//
// ============================ 它盯的是什么 ============================
//  · 原版 `ChatPreview/Container` 是 `Image.Type = Filled`（真包 prefab 实读：
//    `m_Type=3` · `m_FillMethod=0`(Horizontal) · `m_FillOrigin=1`(Right) · `m_FillClockwise=1`），
//    出厂 `fillAmount = 0`（`ChatPreview__OnBeforeInitialization.c` 的 `UnityEngine_UI_Image__set_fillAmount(…, 0)`），
//    `ChatPreview__Show.c` 再 `DOFillAmount(container, 1, 0.4).From(0).SetDelay(0.2)`
//    —— 常量 `0x1834b2bb8 = 1` / `0x1834b3158 = 0.4` / `0x1834b2bb0 = 0.2`（`工具/read_literal.py` 现读）。
//  · 我们过去**没有**这一档：`Shell/MainMenuRuntime.ShowChatPreview` 里挂着一句「`DOFillAmount` 没有对等物」的警告。
//    修法 = `Battle/ImageQuad.cs` 补 `SetFillAmount`（纯 C# 侧按 uGUI `Image.GenerateFilledSprite` 裁网格 + uv，⛔ 没动 shader）
//    ＋ `MainMenuRuntime.BuildChatPreview` 出厂置 0、`ShowChatPreview`/`StepChatPreview` 按 0.4 s + 0.2 s 延迟插值。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · **A1（★ 出厂值）** 读的是**生产那一块 quad 的真实状态** `ImageQuad.FillAmount`
//    ＋ **网格顶点数**（填 0 ⇒ 0 顶点 = 原版「一个像素都不画」；只在字段里存个 0、图照画满 = 红）。
//    ⛔ 不去读 `MainMenuRuntime` 自己的任何常量（读了 = 拿实现证明实现）。
//  · **A2（灭自证）**：`ImageQuad.Create` 建出来的**出厂 fill = 1**（不裁）。
//    ⇒ A1 那个 `0` **只能**来自 `BuildChatPreview` 里那一句 `SetFillAmount(0f)`：
//    把它改成 1（或删掉）⇒ A1 红、A2 仍绿；反过来若谁把 `Create` 的出厂默认改成 0（让 A1 无因也绿）⇒ A2 红。
//    —— 两条**结构上不可能同时满足**。
//  · **A3（语义 / 判别式）**：fill=0.5 时网格必须只剩**右半**（x ∈ [0, +0.5]、uv x ∈ [0.5, 1]）
//    —— 判据 = uGUI `Image.GenerateFilledSprite` 的 `m_FillOrigin == 1` 支（`v.x = v.z − (v.z−v.x)·amount` ·
//    `tx0 = tx1 − span·amount`）。把「自右向左」做成「自左向右」或「整体缩到中间」⇒ A3 红。
//  · **A4（别过头）**：fill 回 1 ⇒ 4 顶点、整框复位（⛔ 不是「填过一次就永远少一块」）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  🔴 接在 `CardPresentation/Editor/MainMenuScene.cs` 的 `Run()` 里、**主菜单树建好之后**（那条聚合行**同一物理行**追加，行数零变化）：
//      B102AUD05_Assert.Run(); pass += B102AUD05_Assert.Pass; fail += B102AUD05_Assert.Fail;
//  ⚠️ **前提（宿主必须满足，不满足 = 红、⛔ 不是跳过）**：`MainMenuRuntime.Instance != null` 且 `Build()` 已经跑过
//     （本件靠 `Instance.Find("ChatPreview")` 找生产那一块）。`ShellScene` 里主菜单树也建着，同样可跑。
//  ⚠️ 本件会**临时调一次生产路径** `ShowChatPreview()` + `StepChatPreview(0.7f)`（= 把出场动画走完），
//     收尾把 `activeSelf`（`SetChatPreviewVisible`）**还原**；fill 停在 1（那块默认关着，不影响画面）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_AUD05`：聊天预览底板 `ChatPreview/Container` 的 `Image.Type = Filled`（fillAmount 0 → 1）。</summary>
public static class B102AUD05_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102AUD05] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>唯一入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B102_AUD05：聊天预览底板 `ChatPreview/Container` = `Image.Type=Filled`"
                                + "（`m_FillMethod=0` · `m_FillOrigin=1` · 出厂 fillAmount 0）");
        A_Production();
        B_ControlQuad();
    }

    // ==================================================================
    //  A 段：生产那一块（`MainMenuRuntime.BuildChatPreview` 建出来的 `ChatPreview/Container`）
    // ==================================================================
    static void A_Production()
    {
        var menu = MainMenuRuntime.InstanceOrFind();   // 🔴 编辑模式下 `AddComponent` 不触发 `Awake` ⇒ `Instance` 恒 null；走统一读口（`Shell/MainMenuRuntime.cs:1704`）
        T(menu != null, "A0（前提）宿主里找得到 `MainMenuRuntime`（`InstanceOrFind() != null`；⛔ 不用 `Instance` —— 编辑模式 `Awake` 不跑）");
        if (menu == null) return;

        var cp = menu.Find("ChatPreview");
        T(cp != null, "A0（前提）主菜单里建出了 `ChatPreview` 节点");
        var qt = cp != null ? MenuCheck.FindChild(cp, "Container") : null;
        var q = qt != null ? qt.GetComponent<ImageQuad>() : null;
        T(q != null, "A0（前提）`ChatPreview/Container` 存在、且挂着 `ImageQuad`（= 原版那块 `Image.Type=Filled` 的底板）");
        if (q == null) return;

        var mf = q.GetComponent<MeshFilter>();
        T(mf != null && mf.sharedMesh != null, "A0（前提）那块 quad 有 `MeshFilter` + 网格");

        bool visBefore = menu.ChatPreviewVisible;
        try
        {
            // ---- A1 ★ 出厂值（判据 = 原版 prefab 的 fillAmount 0 / `OnBeforeInitialization` 的 set_fillAmount(0)）----
            N(q.FillAmount, 0f, 1e-4f,
              "★ A1 出厂 `fillAmount` = **0**（原版 `ChatPreview/Container`：`Image.Type=Filled` + `ChatPreview__OnBeforeInitialization.c` 的 `set_fillAmount(…, 0)`）");
            T(mf.sharedMesh != null && mf.sharedMesh.vertexCount == 0,
              "★ A1b 出厂 fill=0 ⇒ 网格 **0 顶点**（= 原版「一个像素都不画」；只在字段里存个 0、图照画满 ⇒ 本条红）");

            // ---- A1c ★ 走一次**生产路径**的「显示」：`DOFillAmount(…, 1, 0.4).From(0).SetDelay(0.2)` 的终态 ----
            menu.ShowChatPreview();                 // = 原版 `ChatPreview.Show()`（`SetActive(true)` + From(0) + 起 tween）
            menu.StepChatPreview(0.7f);             // > `scaleDur` 0.6 ⇒ 终态（批处理没有帧循环，必须手动步进）
            N(q.FillAmount, 1f, 1e-4f,
              "★ A1c `Show()` 走完 ⇒ `fillAmount` = **1**（原版 `DOFillAmount(container, 1, 0.4)` 的终态；"
            + "本仓靠 `StepChatPreview` 逐帧插值 —— 🧨 把 `StepChatPreview` 里那一句 `SetFillAmount` 删掉 ⇒ 本条红）");
            T(mf.sharedMesh != null && mf.sharedMesh.vertexCount == 4,
              "★ A1d `fillAmount` = 1 ⇒ 网格回到 **4 顶点**（整块画满 —— 证明画面上真的画回来了，不只是字段变了）");
        }
        finally
        {
            menu.SetChatPreviewVisible(visBefore);  // 收尾：显隐还原（`Show()` 会把它点亮）
        }
    }

    // ==================================================================
    //  B 段：独立探针（灭自证 + 「自右向左」语义 + 回 1 复位）
    // ==================================================================
    static void B_ControlQuad()
    {
        var root = new GameObject("B102AUD05_Probe");
        Texture2D tex = null;
        try
        {
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.Apply();
            // 高 1 世界单位 + 2×2 图 ⇒ WorldW = WorldH × 1 = **1**（下面所有期望值都用这个 1 算，⛔ 不读被测的那块 quad 的量）
            var q = ImageQuad.Create(root.transform, tex, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "Probe");
            T(q != null, "A2（前提）`ImageQuad.Create` 建出了探针（2×2 图 · 高 1 世界单位 · 锚点居中）");
            if (q == null) return;
            var mf = q.GetComponent<MeshFilter>();
            T(mf != null && mf.sharedMesh != null, "A2（前提）探针有 `MeshFilter` + 网格");

            // ---- A2 灭自证：`Create` 的出厂 = 1（不裁）----
            N(q.FillAmount, 1f, 1e-4f,
              "A2【灭自证】`ImageQuad.Create` 的出厂 fill = **1（不裁）** —— 所以 A1 那个 0 只能来自 "
            + "`BuildChatPreview` 里那句 `SetFillAmount(0f)`（把它改成 1 / 删掉 ⇒ A1 红、本条仍绿；"
            + "反过来把出厂默认改成 0 让 A1 无因也绿 ⇒ 本条红。两条**结构上不可能同时满足**）");
            N(q.WorldW, 1f, 1e-3f, "A2（前提）探针 `WorldW` = **1**（2×2 图 ⇒ 比例 1）—— 下面的期望值按它算");

            // ---- A3 语义：`m_FillOrigin=1` = **自右向左**填（uGUI `GenerateFilledSprite` 那一支）----
            q.SetFillAmount(0.5f);
            var b = mf.sharedMesh.bounds;
            N(b.min.x, 0f, 1e-3f,
              "★ A3 fill=0.5 ⇒ 网格左沿 = **中缝 x=0**（原版 `m_FillOrigin=1` = 自右向左填：`v.x = v.z − (v.z−v.x)·amount`）");
            N(b.max.x, 0.5f, 1e-3f,
              "★ A3 fill=0.5 ⇒ 网格右沿**不动**（x=+0.5 = 框右缘）—— 换成「自左向右」或「整体缩到中间」都会红");
            var uv = mf.sharedMesh.uv;
            N(uv.Length >= 4 ? uv[0].x : -1f, 0.5f, 1e-3f,
              "★ A3 uv 左沿 = **0.5**（`tx0 = tx1 − span·amount` —— 裁掉的是左边那半张图，不是把整张图压进去）");
            N(uv.Length >= 4 ? uv[2].x : -1f, 1f, 1e-3f, "★ A3 uv 右沿 = **1**（右缘的纹素位置不动）");

            // ---- A4 边界与复位 ----
            q.SetFillAmount(0f);
            T(mf.sharedMesh.vertexCount == 0,
              "A4 fill=0 ⇒ **0 顶点**（uGUI `GenerateFilledSprite` 开头 `m_FillAmount < 0.001f` 那一支：整块不画）");
            q.SetFillAmount(1f);
            N(q.FillAmount, 1f, 1e-4f, "A4 回到 fill=1 ⇒ 读数复位");
            T(mf.sharedMesh.vertexCount == 4, "A4 回到 fill=1 ⇒ **4 顶点**（整框复位，⛔ 不是「填过一次就永远少一块」）");
            b = mf.sharedMesh.bounds;
            N(b.min.x, -0.5f, 1e-3f, "A4 fill=1 ⇒ 左沿回到 −0.5（= −WorldW/2，锚点居中）");
            N(b.max.x, 0.5f, 1e-3f, "A4 fill=1 ⇒ 右沿回到 +0.5（= +WorldW/2）");
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
            if (tex != null) Object.DestroyImmediate(tex);
        }
    }
}
