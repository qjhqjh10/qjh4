// B117_G10_21_Assert.cs — 执行代理 `B117-G10-21`（2026-11-02）· 战斗域第 21 分片「错误提示池 · 疲劳横幅」
//
// ============================ 它盯的是什么 ============================
//  `Battle/ErrorMessageBanner.cs`（原版 `UI Error Message Controller` + `UIErrorMessageItem`）**本轮新修的两条**，
//  外加把「池 / 轮转 / 时序」三条既有判据钉在本文件里（本文件自成一体，⛔ 不依赖 `Editor/*Scene.cs`）：
//    A. 池 = 5 条、模板不进池、轮转 0→4→0（`UIMessageController__ShowMessage.c` / `__Awake.c`）
//    B. 🔴 **堆叠次序按【显示先后】，⛔ 不是池下标**（本轮修的那条，见下）
//    C. 🔴 **原版那块字材质 `Pragati-Regular Atlas Material Thick Outline`**（本轮修的第二条）
//    D. 时序：0.3×→1 入场 0.15 s · 停到 2.0 s · 0.25 s 淡出 · 2.25 s 关自身
//
// ============================ 判据（全部实读，出处逐条写在断言里）============================
//  · `d:/2/tools/decomp_full/UIMessageController__ShowMessage.c`：空串 ⇒ LogError + return；
//    `item = messageItems[currentMessageIndex].Show(串, 色, localize)`；`currentMessageIndex = ++i; if (4 < i) i = 0`；
//    之后**遍历整池** `LayoutRebuilder.ForceRebuildLayoutImmediate(item.background)`。
//  · `d:/2/tools/decomp_full/UIErrorMessageItem__Show.c` 第一句 = `cacheRectTransform.SetAsLastSibling()`
//    （`UIErrorMessageItem__Show.c` 里 `UnityEngine_Transform__SetAsLastSibling`）。
//  · 容器是 `VerticalLayoutGroup`（`m_ChildAlignment = 7` LowerCenter · `m_ChildControlWidth/Height = 0`），
//    uGUI 的 `LayoutGroup.GetChildList` **只收活动子件、按兄弟序排**，再从上往下累计 ⇒
//    **兄弟序最后 = 最下面**。所以「刚弹的那条堆在最下面」由 `SetAsLastSibling` 决定。
//    ⚠️ 我们的 `_items` 是**池下标序**，池绕回去之后与兄弟序**会分叉**（例：池 5 条、第 6 次 Show 又落回 0 格）。
//  · 字材质：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_3740.json` 的
//    `m_sharedMaterial = {m_FileID 2, m_PathID -455814643003338116}` →
//    `bundle_fonts_assets_all/Material/Material_-455814643003338116.json`
//    = `Pragati-Regular Atlas Material Thick Outline`：`_OutlineWidth 0.097` ·
//    `_OutlineColor (0,0,0,0.7882353)` · `_FaceDilate 0.105` · `_OutlineSoftness 0` ·
//    `_GradientScale 21` · `_ScaleRatioA 0.95238096`。
//  · 几何（`RectTransform_3373/3154/2823.json`）：根上缘 213.5997314453125 · `RootH 336.6400146484375` ·
//    容器高 266.2900085449219 · 项高 80（`m_ChildControlHeight = 0` ⇒ 取子件自己的 `sizeDelta.y`）
//    ⇒ 单条条心 = 213.5997314453125 + 336.6400146484375 − 40 = **510.23974609375**；
//    5 条堆满时 j=0..4 = **190.2397 / 270.2397 / 350.2397 / 430.2397 / 510.2397**（自上而下）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 B 组：池绕回后仍然断言 **`ItemCenterPxOf(0)` = 最下面那一档、`ItemCenterPxOf(1)` = 最上面那一档** ——
//     退回「按池下标排」⇒ 0 号会跑到最上面 ⇒ **必红**（B4/B5 两条互为反相，改一个方向不会一起变绿）。
//     条心数值一律由 `SingleItemCenterYpx`（B0 钉死 = 原版布局算法算出的 **510.23974609375**）+ `ItemHeightPx`
//     （B0b 钉死 = **80**）推出来，⛔ 不写死一串绝对像素 —— 这样世界档/uGUI 档任何**整体**偏移都不会造成假红。
//  🧨 C 组：同时断言「**实例材质**上有描边」＋「**共享材质**上 `_OutlineWidth` 仍是 0」——
//     图省事把描边写到 `fontSharedMaterial`（会让全工程的字一起变）⇒ 第二条红。
//  🧨 C 组：同一条**先弹中文、再弹英文**，最后那条必须仍带描边 —— 只把描边贴在【建件那一刻】⇒
//     `SetText` 里的 `SyncFace` 换面时实例材质被换掉 ⇒ 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（自带临时根 + 收尾 `DestroyImmediate`），但**要求本场景里此刻没有别的横幅**
//  （有 ⇒ 单例守卫会把新建那颗销毁、`Create` 交回活的另一颗 ⇒ 本文件的池状态前提不成立；届时**当场红并说明**）。
//  挂法（由调度台决定）：`Editor/BattleScene.cs` 的聚合行**同一物理行**追加
//      B117_G10_21_Assert.Run(); _sink.Pass += B117_G10_21_Assert.Pass; _sink.Fail += B117_G10_21_Assert.Fail;
//  ⚠️ 必须排在 `BattleDriver` 建出战斗横幅**之前**（`BuildErrorBanner` 会在同一场景建一颗）。
//  判绿红：`grep "^B117G1021"` 的**合计行**；逐条按**行首** `B117G1021 ✓` / `B117G1021 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// ⚠️ 本文件**不碰任何场景/存档**：只在内存里造一颗临时横幅，`finally` 里拆掉。

using TMPro;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B117_G10_21`：错误提示池的**堆叠次序**与**字材质**两条本轮修复 + 池/轮转/时序三条既有判据。</summary>
public static class B117_G10_21_Assert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1021 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1021 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol + "；实得 " + got.ToString("F4") + "）");
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var root = new GameObject("B117_G10_21_Root");
        try
        {
            // ---- 前提（硬检查，⛔ 不许 `if (x != null) Check(...)` 那种静默跳过）----
            int before = BannerCountInActiveScene();
            Check(before == 0,
                  "（前提）本场景此刻**没有**别的横幅（实得 " + before + " 颗）—— 有的话单例守卫会把新建那颗销毁，"
                + "本文件的池状态前提不成立（本组必须排在建战斗横幅之前跑）");
            if (before != 0) return;

            var b = ErrorMessageBanner.Create(root.transform, ErrorMessageBanner.BattlePreset);
            Check(b != null && b.TemplateBuilt,
                  "（前提）`Create` 交出**建完池**的一颗（`TemplateBuilt` = " + (b != null ? b.TemplateBuilt.ToString() : "<null>") + "）");
            if (b == null || !b.TemplateBuilt) return;

            Group("A·池与轮转", () => A_PoolAndRotation(b));
            Group("B·堆叠次序", () => B_StackOrder(b));
            Group("C·字材质描边", () => C_OutlineMaterial(b));
            Group("D·时序", () => D_Timing(b));
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
        Debug.Log("B117G1021 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    // ---------------------------------------------------------------- A 池与轮转
    static void A_PoolAndRotation(ErrorMessageBanner b)
    {
        // `UIMessageController.NUMBER_OF_MESSAGES = 5`（const）；`__ctor` 里 `List(capacity: 5)`；
        // `Awake` 里 `for i in 0..4: Instantiate(ref, parent)`（`if (4 < iVar9) return`）。
        Check(ErrorMessageBanner.NumberOfMessages == 5,
              "A1 池容量常量 = **5**（原版 `NUMBER_OF_MESSAGES`，`UIMessageController__Awake.c` 的 `4 < iVar9 ⇒ return`）"
            + "（实得 " + ErrorMessageBanner.NumberOfMessages + "）");
        Check(b.ItemCount == 5,
              "A2 池里**实际**建了 5 条（模板不算：原版 `messageItems` 只装 `Instantiate` 的 5 份）（实得 " + b.ItemCount + "）");
        Check(b.TemplateBuilt && b.VisibleCount == 0,
              "A3 模板建出来了、但**一条都没亮**（原版 `Awake` 第一句就是 `errorMessageItemRef.SetActive(false)`）"
            + "（`TemplateBuilt`=" + b.TemplateBuilt + " · 亮着 " + b.VisibleCount + "）");

        // 轮转：`currentMessageIndex = ++i; if (4 < i) i = 0`
        for (int k = 0; k < 5; k++) b.ShowError("A轮转" + k);
        Check(b.CurrentIndex == 0 && b.ShowCount == 5,
              "A4 弹 5 次 ⇒ 游标回到 **0**（原版 `if (4 < i) i = 0`）（游标 " + b.CurrentIndex + " · 弹过 " + b.ShowCount + "）");
        Check(b.VisibleCount == 5, "A5 5 条全亮着（池满）（实得 " + b.VisibleCount + "）");
        b.HideAll();
        Check(b.VisibleCount == 0, "A6 `HideAll()` 收干净（= 原版 `OnKill` 那一支的等价物）（实得 " + b.VisibleCount + "）");
    }

    // ---------------------------------------------------------------- B 堆叠次序
    static void B_StackOrder(ErrorMessageBanner b)
    {
        b.HideAll();
        float single = b.SingleItemCenterYpx, h = b.ItemHeightPx;
        CheckClose(single, 510.23974609375f, 0.01f,
                   "B0 单条亮着时的条心 = **510.23974609375**（原版：根上缘 213.5997314453125 + RootH 336.6400146484375 − 项高 80/2）");
        CheckClose(h, 80f, 0.01f,
                   "B0b 项高 = **80**（容器 `VerticalLayoutGroup` 的 `m_ChildControlHeight = 0` ⇒ 取子件自己的 `sizeDelta.y`）");

        // 第 1 圈：池 0..4 依次亮 ⇒ 兄弟序/显示次序与池下标**一致**（旧实现也过这一档 —— 所以它是背景，不是判据）
        for (int k = 0; k < 5; k++) b.ShowError("B第" + (k + 1) + "条");
        for (int i = 0; i < 5; i++)
            CheckClose(b.ItemCenterPxOf(i).y, single - (4 - i) * h, 0.25f,
                       "B1 第 1 圈：池 " + i + " 格（= 第 " + (i + 1) + " 条）= 单条条心往上 " + (4 - i) + " 档（每档 80）");

        // 🔴 第 2 圈：第 6 次 Show 落回**池 0 格** —— 原版 `SetAsLastSibling()` 让它变成**兄弟序最后** ⇒ 最下面。
        //    按池下标排的实现会把它顶到最上面（= 本轮修掉的那条）。
        b.HideAll();
        string[] texts = { "B第1条", "B第2条", "B第3条", "B第4条", "B第5条", "B第6条" };
        for (int k = 0; k < 6; k++) b.ShowError(texts[k]);
        Check(b.TextOf(0) == texts[5],
              "B3 前提：第 6 次 Show 确实落在**池 0 格**（文案「" + b.TextOf(0) + "」）—— 不成立 ⇒ 下面几条测的不是这件事");
        CheckClose(b.ItemCenterPxOf(0).y, single - 0f * h, 0.25f,
                   "B4 ★ 池绕回后：**刚弹的那条（池 0 格）堆在最下面**（= 单条条心 " + single + "）"
                 + " —— 原版 `UIErrorMessageItem__Show.c` 第一句 `SetAsLastSibling()` + 布局组按**兄弟序**从上往下堆；"
                 + "「按池下标排」的实现这里是**最上面**那一档 ⇒ 红");
        CheckClose(b.ItemCenterPxOf(1).y, single - 4f * h, 0.25f,
                   "B5 ★ 池绕回后：**最旧的那条（池 1 格）被顶到最上面**（= 单条条心往上 4 档）"
                 + " （= B4 的反相：两条不可能同时被「按下标」和「按先后」满足）");
        CheckClose(b.ItemCenterPxOf(2).y, single - 3f * h, 0.25f, "B6 池 2 格 = 往上 3 档");
        CheckClose(b.ItemCenterPxOf(3).y, single - 2f * h, 0.25f, "B7 池 3 格 = 往上 2 档");
        CheckClose(b.ItemCenterPxOf(4).y, single - 1f * h, 0.25f, "B8 池 4 格 = 往上 1 档");

        // 灭自证②：五条的条心**必须正好落在那五档上**（不许靠「整体压缩/重排成别的间距」蒙过去）
        bool permOk = true;
        for (int i = 0; i < 5; i++)
        {
            float y = b.ItemCenterPxOf(i).y;
            bool hit = false;
            for (int j = 0; j < 5; j++) if (Mathf.Abs(y - (single - j * h)) <= 0.25f) { hit = true; break; }
            if (!hit) permOk = false;
        }
        Check(permOk && b.VisibleCount == 5,
              "B9（灭自证）5 条的条心**正好落在**那 5 档上（⛔ 不是整体平移/压缩后的近似）（亮着 " + b.VisibleCount + "）");

        // 收尾：让后面几组从干净状态起步
        b.HideAll();
    }

    // ---------------------------------------------------------------- C 字材质描边
    static void C_OutlineMaterial(ErrorMessageBanner b)
    {
        b.HideAll();
        // 模板那条的文字 = 参考串 `ERROR MESSAGE CONTENT`（拉丁面）⇒ 建件时就该带上原版那块字材质。
        var tmpl = TmpOfChild(b, 0);
        Check(tmpl != null,
              "C1（前提）模板那颗 `ErrorMesage Text` 上取得到 `TMP_Text`（取不到 ⇒ 下面几条**没验**）");
        if (tmpl == null) return;

        CheckMat(tmpl, "C2 ★ **模板**那颗字带原版的厚描边");

        // 生产口：刚 Show 的那条（`ShowItem` 那条路）
        b.ShowError("C Outline");
        int slot = SlotJustShown(b);
        var item = TmpOfChild(b, 1 + slot);     // 容器子件序：[0] 模板 ⇒ 池 i 格 = [i + 1]
        Check(item != null, "C3（前提）刚弹的那条（池 " + slot + " 格）取得到 `TMP_Text`");
        if (item != null) CheckMat(item, "C4 ★ **Show 出来的那条**同样带原版的厚描边");

        // 🧨 灭自证①：描边必须落在**实例材质**上 —— 字体资产那份共享材质上 `_OutlineWidth` 必须仍是 0。
        //    （写到共享材质会让全工程所有字一起变粗；`TmpFont.ApplyOutline` 的注释也钉着这条。）
        //
        // 🔴 **2026-11-02（`B117-FIXD`）就地订正（铁律 5）**：原来这两条拿 `fontSharedMaterial` 当「共享材质」比，
        //    **在本工程的 TMP 版本下恒不成立** —— `TextMeshPro.GetMaterial`（`com.unity.ugui@27635d171b1a/
        //    Runtime/TMP/TextMeshPro.cs:1182-1201`，`TextMeshProUGUI.cs:1474-1497` 同构）里那句
        //    `m_sharedMaterial = m_fontMaterial; return m_sharedMaterial;` ⇒ **读过一次 `fontMaterial` 之后
        //    `fontSharedMaterial` 返回的就是那份实例**。所以：
        //      · 旧 C5 `fontMaterial != fontSharedMaterial` **结构性恒假**（34/36 里的那两条就是从没绿过）；
        //      · 旧 C6 读到 0.097 是**实例**的值，不是共享材质被污染（本条实现 `ApplyBannerOutline` 写的就是实例）。
        //    ⇒ 判据改成与**字体资产自己的材质**比：`TMP_Text.font.material`（`TMP_FontAsset.material`，那份才是全工程共享的）。
        if (item != null && item.fontMaterial != null)
        {
            var assetMat = item.font != null ? item.font.material : null;
            Check(assetMat != null && !ReferenceEquals(item.fontMaterial, assetMat),
                  "C5（灭自证）描边落在**私有实例材质**上（`fontMaterial` ≠ 字体资产 `font.material`；实得 "
                + (assetMat == null ? "<font.material 为 null>" : ReferenceEquals(item.fontMaterial, assetMat) ? "同一份" : "不是同一份") + "）");
            float sw = assetMat != null && assetMat.HasProperty(ShaderUtilities.ID_OutlineWidth)
                     ? assetMat.GetFloat(ShaderUtilities.ID_OutlineWidth) : -1f;
            Check(assetMat != null && Mathf.Abs(sw) <= 1e-4f,
                  "C6（灭自证）**字体资产共享材质**的 `_OutlineWidth` 仍是 **0**（⛔ 没被污染）—— 实得 " + sw.ToString("F4")
                + "｜🧨 把描边写到共享材质 ⇒ 红");
        }

        // 🧨 灭自证②：同一条**先中文、再英文**，英文那条必须**重新**带上描边。
        //    `Label` 在中文档会 `SyncFace` 换成 CJK 面（材质跟着换）⇒ 只贴在建件那一刻的实现，这里会丢。
        //    轮转一圈回到同一格：池 0 格就是刚 Show 过的那格。
        b.ShowError("中文一条");                                     // 这一条把某一格换成 CJK 面
        int slotCjk = SlotJustShown(b);
        for (int k = 1; k < 5; k++) b.ShowError("C 填充" + k);       // 转一圈，回到**同一格**
        b.ShowError("C Back To Latin");                              // 同一格再弹一次，这次是拉丁
        int slot2 = SlotJustShown(b);
        var again = TmpOfChild(b, 1 + slot2);
        Check(slot2 == slotCjk && again != null && again.text == "C Back To Latin",
              "C7 前提：转一圈后又回到**那条中文占过的格**（池 " + slotCjk + " → " + slot2 + "）且文案是拉丁"
            + "（实得「" + (again != null ? again.text : "<null>") + "」）");
        if (again != null && again.text == "C Back To Latin")
            CheckMat(again, "C8 ★（灭自证）**中文档之后**再弹拉丁 ⇒ 描边**重新贴上**（只在建件时贴的实现这里会丢）");

        b.HideAll();
    }

    /// <summary>一条「这颗字带原版厚描边吗」—— 三个数逐条对 `Material_-455814643003338116.json`。</summary>
    static void CheckMat(TMP_Text tmp, string head)
    {
        var m = tmp.fontMaterial;
        if (m == null) { Check(false, head + "：拿不到 `fontMaterial`"); return; }
        float w = m.HasProperty(ShaderUtilities.ID_OutlineWidth) ? m.GetFloat(ShaderUtilities.ID_OutlineWidth) : -1f;
        Color c = m.HasProperty(ShaderUtilities.ID_OutlineColor) ? m.GetColor(ShaderUtilities.ID_OutlineColor) : new Color(0, 0, 0, -1);
        float d = m.HasProperty(ShaderUtilities.ID_FaceDilate) ? m.GetFloat(ShaderUtilities.ID_FaceDilate) : -1f;
        bool kw = m.IsKeywordEnabled("OUTLINE_ON");
        Check(kw && Mathf.Abs(w - ErrorMessageBanner.OutlineWidth) <= 1e-4f
              && Mathf.Abs(c.r) <= 1e-4f && Mathf.Abs(c.g) <= 1e-4f && Mathf.Abs(c.b) <= 1e-4f
              && Mathf.Abs(c.a - 0.7882353f) <= 1e-3f
              && Mathf.Abs(d - ErrorMessageBanner.OutlineFaceDilate) <= 1e-4f,
              head + "：`OUTLINE_ON` + `_OutlineWidth 0.097` + `_OutlineColor` **黑 α 0.788**（⛔ 不是白）+ `_FaceDilate 0.105`"
            + "（判据 = `bundle_fonts_assets_all/Material/Material_-455814643003338116.json`）"
            + "｜实得 kw=" + kw + " width=" + w.ToString("F4") + " color=("
            + c.r.ToString("F3") + "," + c.g.ToString("F3") + "," + c.b.ToString("F3") + "," + c.a.ToString("F3")
            + ") dilate=" + d.ToString("F4"));
    }

    // ---------------------------------------------------------------- D 时序
    static void D_Timing(ErrorMessageBanner b)
    {
        b.HideAll();
        b.ShowError("D 时序");
        int i = SlotJustShown(b);
        CheckClose(b.ScaleOf(i), 0.3f, 1e-3f, "D1 入场从 **0.3×** 起步（原版 `appearScaleFromMultiplier = 0.3`）");
        b.Advance(ErrorMessageBanner.AppearAnimationTime);
        CheckClose(b.ScaleOf(i), 1f, 1e-3f, "D2 0.15 s 后缩放到 **1×**（`appearAnimationTime = 0.15`）");
        CheckClose(b.TextColorOf(i).a, 1f, 1e-3f, "D3 0.15 s 时文字 α 仍 = 1（淡出延迟 `timeToStartFading = 2.0` 还没到）");
        b.Advance(ErrorMessageBanner.TimeToStartFading - ErrorMessageBanner.AppearAnimationTime);
        CheckClose(b.TextColorOf(i).a, 1f, 1e-3f, "D4 到 **2.0 s** 那一刻 α 还是 1（原版 `DOFade(0, 0.25).SetDelay(2.0)` 的延迟边界）");
        b.Advance(ErrorMessageBanner.TimeToFade * 0.5f);
        // OutQuad 在 x=0.5 处 = 0.75 ⇒ α = 1 − 0.75 = 0.25
        CheckClose(b.TextColorOf(i).a, 0.25f, 5e-3f,
                   "D5 淡出到一半（2.125 s）α = **0.25**（`Ease.OutQuad`：1 − (1−0.5)² = 0.75 已走完）");
        b.Advance(ErrorMessageBanner.TimeToFade * 0.5f + 0.02f);
        Check(!b.ActiveOf(i) && b.VisibleCount == 0,
              "D6 2.25 s 之后**关掉**（原版 fade tween 的 `OnComplete → SetActive(false)`）"
            + "（亮着 " + b.VisibleCount + " · 那条 " + (b.ActiveOf(i) ? "还亮" : "关了") + "）");
        b.HideAll();
    }

    // ---------------------------------------------------------------- 工具
    /// <summary>容器里的第 `child` 个子件的 `TMP_Text`（`0` = 模板 `Error Mensage_Ref`，`1..5` = 池 0..4）。
    /// ⚠️ 池**不重排兄弟序**（原版靠 `SetAsLastSibling` 重排、我们用渲染队列 + 记账次序）⇒ 子件序恒 = 池下标序。</summary>
    static TMP_Text TmpOfChild(ErrorMessageBanner b, int child)
    {
        var cont = b.transform.Find("Container");
        if (cont == null || child < 0 || child >= cont.childCount) return null;
        return cont.GetChild(child).GetComponentInChildren<TMP_Text>(true);
    }

    /// <summary>刚 Show 出去的落在**池第几格**（`ShowMessage` 是「先取当前格、再 `++`」⇒ 上一次那格 = 游标 − 1）。</summary>
    static int SlotJustShown(ErrorMessageBanner b)
    {
        return (b.CurrentIndex + ErrorMessageBanner.NumberOfMessages - 1) % ErrorMessageBanner.NumberOfMessages;
    }

    /// <summary>本场景里现存的横幅个数（含 inactive；⛔ 按场景数 —— 壳是 `DontDestroyOnLoad`，它那颗不算）。</summary>
    static int BannerCountInActiveScene()
    {
        int n = 0;
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i] != null) n += roots[i].GetComponentsInChildren<ErrorMessageBanner>(true).Length;
        return n;
    }
}
