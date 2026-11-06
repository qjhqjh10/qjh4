// B141_W4_Assert.cs — 执行代理 `B141-W4`（第一百二十八会话 · 波次 `W129` · B138 §4 `4-03`：教学指针层序）
//
// ============================ 它盯的是什么 ============================
//  **教学指针（`TutorialPointerUi`）整条链的渲染队列层带**必须落在原版 `PopUps` / **order 2** 那一档：
//  高于「窗(0) / 错误横幅(1)」、**低于**「教学聚光(601) / toast(900) / Blocking(tooltip) 」。
//
//  原版判据（全部本地现读）：
//    · `d:/2/tools/assetripper_out/ExportedProject/Assets/TutorialPointerMenu.prefab:140-147` 的 `Canvas`：
//        `m_SortingLayerID = 1054366423`（= `ProjectSettings/TagManager.asset:69` 的 **`PopUps`**，层序第 7）·
//        `m_SortingOrder = 2` · `m_OverrideSorting = 1` · `m_RenderMode = 2`。
//      Unity 同一排序层里 **order 小的先画（在下）** ⇒
//        `PopUps/0` 窗 < `PopUps/1` 错误横幅 < **`PopUps/2` 教学指针** < `PopUps/601` 聚光 < `PopUps/900` toast < `Blocking`
//    · 601 / 900 两份 Canvas 同层：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/Canvas/`
//      `Canvas_1073.json`（聚光，order 601）· `Canvas_1074.json`（toast，order 900），两者 `m_SortingLayerID = 1054366423`。
//    · 判据全文 → `资料/普查产出_1020/W58_界面线_Toast黑场次序判据.md` §2·3。
//
//  我们那半边：`Shell/TutorialPointerUi.cs` 的 `QGlow/QPointer/QLight/QBubble/QBubbleFill/QBubbleText`
//  （= **4041–4046**；旧值 4110–4116 是反的，见 B138 §4 `4-03`）。教学聚光 `MenuTutorialHighlight.QShade` = **4047**
//  （2026-11-04 从 4040 抬上来，给本链让出 6 格 —— 原版 `PopUps/2` 在 `PopUps/601` **之下**）。
//  梯子与重排经过的完整说明 → 那个文件尾的「层序」一节 + `资料/完全复刻/报告/B141_W129_TutorialPointer.md`。
//
// ============================ 🔴 灭自证 ============================
//  · A 段：**绝对值回归闸**（4041..4046 逐值）—— 改任何一个 ⇒ A 段红。
//  · B 段：**梯子两端钉在【外部件既有常量】上**（`AlliancePanelWindow.QText` · `ErrorMessageBanner.MenuQueue` ·
//    `MenuTutorialHighlight.QShade` · `ToastNotifications.QShade` · `Tooltip.Queue` · `LoadingMenu.QPanel`）。
//    **把本链改回 4110–4116 ⇒ B3/B4/B5 立刻红**（4116 不再 < 4072 / 4078 / 4090）；
//    **把本链抬到聚光之上（或把 `QShade` 降回 4040）⇒ B2b 立刻红**。
//    想「两边一起挪」把它改绿，就得同时改 toast / tooltip / 黑场 / 聚光的常量 —— 那几处各自的断言会先在别的地方红。
//  · C 段：**实例级** —— 真建一只指针（带气泡）⇒ 逐件读 `ImageQuad.RenderQueue` / `Label.RenderQueue`，
//    证明这些值**真写进了材质**（⛔ 不是只改了常量）。建不出来 / 图缺 ⇒ **前提断言先红**（不静默跳过）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主里加：
//      B141_W4_Assert.Run(); _pass += B141_W4_Assert.Pass; _fail += B141_W4_Assert.Fail;
//  推荐宿主 = `Editor/ShellScene.cs`（9 s，最便宜；教学指针与聚光都挂在壳上）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B141_W4`：教学指针层序（原版 `PopUps` / order 2 那一档）。</summary>
public static class B141_W4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B141_W4] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B141_W4 · 4-03 教学指针层序（原版 `PopUps` / order 2 那一档）");
        SectionA_Values();
        SectionB_Ladder();
        SectionC_Applied();
    }

    // ============================================================ A · 绝对值（回归闸，[我们挑的]）
    // 原版只给**相对次序**（PopUps 层内 order 2），绝对值必须落进我们这张显式队列梯子 ⇒ 这一段是回归闸，
    // 不是「原版值」。原版依据在 B 段（钉外部件）。
    static void SectionA_Values()
    {
        C(TutorialPointerUi.QGlow, 4041, "A1a `QGlow` = 4041（旧 4110 = 压住 toast 的反序值）");
        C(TutorialPointerUi.QPointer, 4042, "A1b `QPointer` = 4042");
        C(TutorialPointerUi.QLight, 4043, "A1c `QLight` = 4043");
        C(TutorialPointerUi.QBubble, 4044, "A1d `QBubble` = 4044");
        C(TutorialPointerUi.QBubbleFill, 4045, "A1e `QBubbleFill` = 4045");
        C(TutorialPointerUi.QBubbleText, 4046, "A1f `QBubbleText` = 4046（旧 4116）");

        // 链内次序 = 原版 prefab 的**子件序**（同一 Canvas 里后画的在上面）：
        //   `TutorialPointerWildcards.prefab` 根的子件依次 `Glow (1)` / `Pointer` / `Generic Popup Background`，
        //   而 `light` 是 `Pointer` 的子件、`Background fill` 与 `TutorialText` 在气泡下 ⇒
        //   Glow < Pointer < light < 气泡底 < 气泡填充 < 气泡文字。
        T(TutorialPointerUi.QGlow < TutorialPointerUi.QPointer, "A2a 链内：`Glow (1)` 在 `Pointer` **之下**（原版根的第 1 个子件）");
        T(TutorialPointerUi.QPointer < TutorialPointerUi.QLight, "A2b 链内：`light` 在手形**之上**（原版 `light` 是 `Pointer` 的子件）");
        T(TutorialPointerUi.QLight < TutorialPointerUi.QBubble, "A2c 链内：气泡在指针之上（原版气泡是根的第 3 个子件）");
        T(TutorialPointerUi.QBubble < TutorialPointerUi.QBubbleFill, "A2d 链内：`Background fill` 在气泡底之上");
        T(TutorialPointerUi.QBubbleFill < TutorialPointerUi.QBubbleText, "A2e 链内：`TutorialText` 在填充之上");
    }

    // ============================================================ B · 梯子（两端钉外部件常量）
    static void SectionB_Ladder()
    {
        int winMax = AlliancePanelWindow.QText;        // 4030 窗带最高（原版 PopUps/0 那一族）
        int banner = ErrorMessageBanner.MenuQueue;     // 4035 菜单错误横幅（原版 PopUps/1；占 4035/4036 两格）
        int shade = MenuTutorialHighlight.QShade;      // 4047 教学聚光（原版 PopUps/601；2026-11-04 从 4040 抬上来给本链让位）
        int toast = ToastNotifications.QShade;         // 4072 toast 最低一条（原版 PopUps/900 那一族）
        int tip = Tooltip.Queue;                       // 4078 tooltip（原版 Blocking 层）
        int black = LoadingMenu.QPanel;                // 4090 加载黑场（我们的梯子最高档）

        // 前提：梯子本身没被别人改坏（钉外部件的既有值；这几条红了说明**别的件**动了，不是本链）
        T(winMax < banner, "B1 前提：窗带最高 < 菜单错误横幅（" + winMax + " < " + banner + "）");
        T(banner < shade, "B1b 前提：错误横幅 < 教学聚光（" + banner + " < " + shade + "）");
        T(toast < tip && tip < black, "B1c 前提：toast < tooltip < 黑场（" + toast + " < " + tip + " < " + black + "）");

        // ★ 原版相对次序（`PopUps/1` < `PopUps/2` < `PopUps/601` < `PopUps/900` < `Blocking`）
        T(banner < TutorialPointerUi.QGlow,
          "B2 ★ 教学指针在错误横幅**之上**（原版 `PopUps/2` > `PopUps/1`）");
        T(TutorialPointerUi.QBubbleText < shade,
          "B2b ★ 教学指针整链在**教学聚光之下**（原版 `PopUps/2` < `PopUps/601`）｜🧨 把本链抬到聚光之上、或把 `QShade` 降回 4040 ⇒ 本行红");
        T(TutorialPointerUi.QBubbleText < toast,
          "B3 ★ 教学指针整链在 **toast 之下**（原版 `PopUps/2` < `PopUps/900`）｜🧨 改回 4116 ⇒ 本行红");
        T(TutorialPointerUi.QBubbleText < tip,
          "B4 ★ 教学指针整链在 **tooltip 之下**（原版 `PopUps` < `Blocking`）｜🧨 改回 4116 ⇒ 本行红");
        T(TutorialPointerUi.QBubbleText < black,
          "B5 ★ 教学指针整链在 **加载黑场之下**（我们取黑场最高档 " + black + "；原版转圈件在 `Default` 层、本就在 PopUps 之下）｜🧨 改回 4116 ⇒ 本行红");

        // 六个队列互不相同 —— 同队列时透明队列按「到相机的 3D 距离」排序，谁压谁不可控（`CLAUDE.md` §三）。
        int[] chain = { TutorialPointerUi.QGlow, TutorialPointerUi.QPointer, TutorialPointerUi.QLight,
                        TutorialPointerUi.QBubble, TutorialPointerUi.QBubbleFill, TutorialPointerUi.QBubbleText };
        int distinct = 0;
        for (int i = 0; i < chain.Length; i++)
        {
            bool dup = false;
            for (int j = 0; j < i; j++) if (chain[i] == chain[j]) dup = true;
            if (!dup) distinct++;
        }
        T(distinct == chain.Length,
          "B6 ★ 本链 " + chain.Length + " 个队列**互不相同**（现读 " + distinct + "/" + chain.Length + "）"
          + "｜🧨 让任意两层共用一个队列 ⇒ 本行红");
    }

    // ============================================================ C · 实例级（值真的写进材质了吗）
    static ImageQuad QuadOf(Transform parent, string name)
    {
        if (parent == null) return null;
        var t = parent.Find(name);
        return t != null ? t.GetComponent<ImageQuad>() : null;
    }

    /// <summary>容器根下**每一块** `ImageQuad`。🔴 `MenuDraw.Nine` / `MenuDraw.Tiled` 返回的是**容器根**
    /// （`Battle/ImageQuad.cs` 的 `CreateNineSlice` / `CreateTiled`：`var root = new GameObject(name)`，
    /// 组件挂在 9 / N 个子块上，根上**没有**）⇒ ⛔ 不能对根取 `GetComponent`（会恒 null，`B150` #12/#13）。</summary>
    static ImageQuad[] QuadsUnder(Transform parent, string name)
    {
        var t = parent != null ? parent.Find(name) : null;
        return t != null ? t.GetComponentsInChildren<ImageQuad>(true) : null;
    }

    /// <summary>一组子块**全部** `RenderQueue == want`（空/缺 ⇒ false，⛔ 不静默跳过）。</summary>
    static bool AllQueued(ImageQuad[] qs, int want)
    {
        if (qs == null || qs.Length == 0) return false;
        for (int i = 0; i < qs.Length; i++) if (qs[i] == null || qs[i].RenderQueue != want) return false;
        return true;
    }

    /// <summary>一组子块**全部** `RenderQueue &lt; ceiling`（空/缺 ⇒ false）。</summary>
    static bool AllBelow(ImageQuad[] qs, int ceiling)
    {
        if (qs == null || qs.Length == 0) return false;
        for (int i = 0; i < qs.Length; i++) if (qs[i] == null || qs[i].RenderQueue >= ceiling) return false;
        return true;
    }

    static void SectionC_Applied()
    {
        var root = new GameObject("B141W4_ROOT");
        TutorialPointerUi p = null;
        try
        {
            p = TutorialPointerUi.Create(root.transform, TutorialPointerUi.Tier.Menu);
            T(p != null, "C0 **前提**：`TutorialPointerUi` 建得出来（批处理里 `Awake` 不会自己跑 —— 本类没有 `Awake`）");
            if (p == null) return;

            p.Show(new Vector2(900f, 500f), "教学指针层序自检");

            T(p.GlowDrawn, "C1 **前提**：`Glow (1)` 画出来了（图缺 ⇒ 下面读不到队列）");
            T(p.PointerImageDrawn, "C2 **前提**：手形图 `Pointer` 画出来了");
            T(p.BubbleShown, "C3 **前提**：气泡建起来了（文字非空 ⇒ 原版 `SetTutorialText` 那一支）");

            var glow = QuadOf(p.AnchorNode, "Glow (1)");
            var hand = QuadOf(p.PointerNode, "Image");
            var light = QuadOf(p.PointerNode, "light");
            var bubbleBg = QuadsUnder(p.BubbleNode, "Image");
            var bubbleFill = QuadsUnder(p.BubbleNode, "Background fill");
            var bubbleTextT = p.BubbleNode != null ? p.BubbleNode.Find("TutorialText") : null;
            var bubbleText = bubbleTextT != null ? bubbleTextT.GetComponent<Label>() : null;

            T(glow != null, "C4a **前提**：`Glow (1)` 上有 `ImageQuad`");
            T(hand != null, "C4b **前提**：`Pointer/Image` 上有 `ImageQuad`");
            T(light != null, "C4c **前提**：`Pointer/light` 上有 `ImageQuad`");
            T(bubbleBg != null && bubbleBg.Length > 0, "C4d **前提**：`Generic Popup Background/Image` 的九宫**子块**上有 `ImageQuad`（根上没有组件；`Nine` 的 9 块）");
            T(bubbleFill != null && bubbleFill.Length > 0, "C4e **前提**：`Background fill` 的平铺**子块**上有 `ImageQuad`（根上没有组件）");
            T(bubbleText != null, "C4f **前提**：`TutorialText` 上有 `Label`");
            if (glow == null || hand == null || light == null || bubbleBg == null || bubbleBg.Length == 0 || bubbleFill == null || bubbleFill.Length == 0 || bubbleText == null)
                return;   // 前提已逐条报红，不再叠假红

            // ★ 真正写进材质的那几个数
            C(glow.RenderQueue, TutorialPointerUi.QGlow, "C5a ★ `Glow (1)` 材质队列 = `QGlow`（写进去了，不是只改常量）");
            C(hand.RenderQueue, TutorialPointerUi.QPointer, "C5b ★ 手形图材质队列 = `QPointer`");
            C(light.RenderQueue, TutorialPointerUi.QLight, "C5c ★ `light` 材质队列 = `QLight`");
            T(AllQueued(bubbleBg, TutorialPointerUi.QBubble), "C5d ★ 气泡底**每一块**（" + bubbleBg.Length + " 块）材质队列都 = `QBubble`（写进去了，不是只改常量）");
            T(AllQueued(bubbleFill, TutorialPointerUi.QBubbleFill), "C5e ★ `Background fill` **每一块**（" + bubbleFill.Length + " 块）材质队列都 = `QBubbleFill`");
            C(bubbleText.RenderQueue, TutorialPointerUi.QBubbleText, "C5f ★ `TutorialText` 文字队列 = `QBubbleText`");

            // 现场复核：四个有材质的件都 <= QBubbleText 且都低于 toast（把常数与实物串起来）
            int toast = ToastNotifications.QShade;      // 4072（钉外部件，不在这里写死数字）
            T(glow.RenderQueue < toast && hand.RenderQueue < toast && light.RenderQueue < toast
              && AllBelow(bubbleBg, toast) && AllBelow(bubbleFill, toast) && bubbleText.RenderQueue < toast,
              "C6 ★ 实物复核：六个件（气泡的**每一块子件**都算）**全部**低于 toast（" + toast + "）｜🧨 只改常量不改调用点 ⇒ 本行红");
        }
        finally
        {
            if (p != null) Object.DestroyImmediate(p.gameObject);
            Object.DestroyImmediate(root);
        }
    }
}
