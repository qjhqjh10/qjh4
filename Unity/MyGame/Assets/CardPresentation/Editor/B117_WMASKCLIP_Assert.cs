// B117_WMASKCLIP_Assert.cs — 执行代理 `B117-WMASKCLIP` 的断言宿主
//
// 任务 = `B117-R59`：主菜单「奖励活动卡」`Dynamic Content` 缺 **`RectMask2D`** 裁切（原版靠它裁掉溢出的两枚图标）。
//
// 判据（全部现读，⛔ 没有一个是猜的）：
//   · `判据/B90_C2b_远端prefab几何.md:207`：`Dynamic Content` 组件行 = `ScrollRect`·MatModifier·`UI_InfiniteScroll`·**`RectMask2D`**；
//     同一张表 `:210` = `Content` 的 `HorizontalLayoutGroup`（spacing **20** · align 4 = MiddleCenter）。
//   · 原版 prefab 现读（`d:/2/tools/assetripper_out/ExportedProject/Assets/Reward Event Container 1x1.prefab`）：
//     MB `&114571718729274231` → `m_GameObject: 1709867586078768`（= `Dynamic Content`），
//     `m_Padding {x:0, y:-15, z:0, w:0}` · `m_Softness {x:0, y:0}`。
//   · `Simple Army Icon` 的 `sizeDelta.x` = **254.90341**；`Dynamic Content` = `Content` = `2.54,11.16 → 532.34,291.99`
//     ⇒ 视口宽 **529.8**。三枚图标总宽 `3×254.90341 + 2×20 = 804.71023` ⇒ 居中摆（align 4）时
//     **第 1、3 颗各溢出 `137.4551` px**，原版靠遮罩裁掉。
//
// 🧨 **灭自证（结构性判别式）**：
//   ① 去掉遮罩 ⇒ `MenuDraw.Rect` 不截几何 ⇒ 图标[0]/[2] 的 **quad 宽 = 254.90341（全幅）**、且左/右沿越出视口
//      ⇒ 本文件「quad 落在遮罩内」「quad 宽 = 254.90341 − 137.4551」「左沿 = 视口左沿」三组同时红。
//   ② 若把 `Initialize` 里那句 `_clip.SetBaseRect(contentAbs)` 删掉 ⇒ 遮罩框留在**建树时的屏幕中心**
//      （x ≈ 960），而卡在 first-fit 第 5 列（x 2425…2960）⇒ 三枚图标整块在框外 ⇒ `MenuDraw.Rect` 直接返 null
//      ⇒ 「（前提）每颗图标都有一个 `Image` quad」与后面全部量尺红（⛔ 不会静默变绿）。
//   ③ 第 ⑥ 组把卡再挪 `(137, 61)` 重建 ⇒ 遮罩框必须**跟着挪**（旧/漏写 `SetBaseRect` 的实现必红）。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_WMASKCLIP_Assert.Run -logFile -
//
// 判绿红：`grep "^B117WMASKCLIP"` 的**合计行**；逐条按**行首** `B117WMASKCLIP ✓` / `B117WMASKCLIP ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// ⚠️ **接线状态（如实记）**：本代理的写权限不含 `Editor/*Scene.cs` ⇒ 本 `Run()` 目前**没有宿主调用它**。
//   由调度台挂进 `Editor/MainMenuScene.cs`（或 `ShellScene.cs`）一行即可：`B117_WMASKCLIP_Assert.Run();`
//   —— 不需要场景里任何东西（本文件自建探测根）。
using UnityEngine;
using CardPresentation;

public static class B117_WMASKCLIP_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    const string Tag = "B117WMASKCLIP";

    // ---- 原版判据（`判据/B90_C2b_远端prefab几何.md` §E-1，⛔ 不是我们的常量）----
    /// <summary>`Simple Army Icon.sizeDelta.x`（原版 254.90341）。</summary>
    const float OriginalIconW = 254.90341f;
    /// <summary>`Content` 的 `HorizontalLayoutGroup.spacing`（原版 20）。</summary>
    const float OriginalSpacing = 20f;
    /// <summary>`Dynamic Content` / `Viewport` / `Content` 三层的相对矩形（卡左上原点）：`2.54,11.16 → 532.34,291.99`。</summary>
    const float ContentX1 = 2.54f, ContentX2 = 532.34f, ContentY1 = 11.16f, ContentY2 = 291.99f;
    /// <summary>视口宽（= 原版 `RectMask2D` 的框宽）**529.8**。</summary>
    const float ViewW = ContentX2 - ContentX1;
    /// <summary>视口高 **280.83**。</summary>
    const float ViewH = ContentY2 - ContentY1;
    /// <summary>原版 `RectMask2D.m_Padding.y`（= Bottom；**负 = 外扩**）⇒ 下边外扩 15px。</summary>
    const float PadBottom = -15f;
    /// <summary>卡根设计尺寸（`RankedArmyBonusCard.CardW/CardH`；本文件按**原版判据** 535×414 反算 `Content` 帧）。</summary>
    const float CardW = 535f, CardH = 414f;
    /// <summary>宿主摆放用的卡尺寸（`Shell/MainMenuRuntime.cs:2395` 的 `CardW=535 / CardH=414.4`）。</summary>
    const float HostCardW = 535f, HostCardH = 414.4f;
    /// <summary>第 1 / 3 颗图标的溢出量 = `(3×254.90341 + 2×20 − 529.8) / 2`。</summary>
    const float Overflow = (3f * OriginalIconW + 2f * OriginalSpacing - ViewW) * 0.5f;

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
        => Check(Mathf.Abs(got - want) <= tol,
                 msg + "（期望 " + want.ToString("F3") + " ± " + tol.ToString("F3")
                     + "；实得 " + got.ToString("F3") + "）");

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  量尺（与建件那条路**互逆**：`MenuDraw.Local` 除的就是 `PosInDesignSpace`，这里再乘回去）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static Vector2 DesignPxOf(Transform t)
        => LayoutSpace.ToDesignPixel(MenuDraw.PosInDesignSpace(t));

    /// <summary>一个节点的设计矩形（画布 px · 左上原点）。尺寸走 `rect × (DesignPxH ÷ DesignHeight)`，
    /// **与 `ViewportClip.ClipPx` / `MenuDraw.QuadRectPx` 同一份口径**。</summary>
    static PxRect NodeRectPx(Transform t)
    {
        var c = DesignPxOf(t);
        var rt = t as RectTransform;
        float hw = 0f, hh = 0f;
        if (rt != null)
        {
            const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
            hw = rt.rect.width * K * 0.5f;
            hh = rt.rect.height * K * 0.5f;
        }
        return new PxRect(c.x - hw, c.y - hh, c.x + hw, c.y + hh);
    }

    /// <summary>`Content`（= 原版 `armyContentAnchor`）—— 用 `Transform.Find` 走路径，⛔ 不为断言改生产读口。</summary>
    static Transform ContentOf(RankedArmyBonusCard card)
        => card == null ? null : card.transform.Find("Dynamic Content/Viewport/Content");

    /// <summary>`Dynamic Content` 节点（原版挂 `RectMask2D` 的那一层）。</summary>
    static Transform ScrollOf(RankedArmyBonusCard card)
        => card == null ? null : card.transform.Find("Dynamic Content");

    /// <summary>一颗 `Simple Army Icon` 的 `Image` quad（`MenuDraw.Rect` 建的那块，名字恒 `Image`）。</summary>
    static ImageQuad QuadOf(Transform icon)
    {
        if (icon == null) return null;
        var t = icon.Find("Image");
        return t == null ? null : t.GetComponent<ImageQuad>();
    }

    /// <summary>`Content` 的**期望**绝对矩形：卡根中心（实测）+ **原版**相对矩形。
    /// ⚠️ 卡根尺寸用 `CardW/CardH`（535×414，= 建 `Content` 时用的那一份）——
    /// 宿主摆的是 535×414.4，那 0.4px 只让 `Content` 中线偏 0.2px（见 `RankedArmyBonusCard.ContentAbsNow` 的注释）。</summary>
    static PxRect ExpectedContentAbs(Transform card)
    {
        var c = DesignPxOf(card);
        float x1 = c.x - CardW * 0.5f, y1 = c.y - CardH * 0.5f;
        return new PxRect(x1 + ContentX1, y1 + ContentY1, x1 + ContentX2, y1 + ContentY2);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  入口
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    public static void Run()
    {
        _pass = _fail = 0;
        var root = new GameObject("B117_WMASKCLIP_Probe", typeof(RectTransform)).transform;
        try
        {
            var card = RankedArmyBonusCard.Create(root);
            Check(card != null, "（前提）`Reward Event Container 1x1` 建得出来");
            if (card == null) { Summary(); return; }

            // 3 个阵营 = 生产数据 `Resources/Art/ranked_army_bonus.json` 的 `affectedArmies:[10,50,110]` 同长度
            var evt = new RankedArmyBonusData.Event { title = "T", endsInSeconds = 3600f, affectedArmies = new[] { 10, 50, 110 } };

            // 生产摆放：`MainMenuRuntime.cs:5221` 的 first-fit 第 5 列（16:9 ⇒ 205 + 4×555 = 2425），y = 182.8
            var placeA = new PxRect(2425f, 182.8f, 2425f + HostCardW, 182.8f + HostCardH);
            MenuDraw.ApplyPxRect(card.transform, root, placeA);
            card.Initialize(evt);

            var content = ContentOf(card);
            Check(content != null, "（前提）`Dynamic Content ▸ Viewport ▸ Content` 找得到（= 原版 `armyContentAnchor`）");
            Check(card.ArmyImageCount == 3, "（前提）`Initialize` 后 `currentArmyImages.Count == 3`（实得 " + card.ArmyImageCount + "）");
            if (content == null || content.childCount != 3) { Summary(); return; }   // 前提不成立 ⇒ ⛔ 不静默跳过

            var scroll = ScrollOf(card);
            Check(scroll != null, "（前提）`Dynamic Content` 节点找得到（原版 `RectMask2D` 就在这一层）");
            var vc = scroll != null ? scroll.GetComponent<ViewportClip>() : null;
            Check(vc != null, "（前提）`Dynamic Content` 上挂着 `ViewportClip`（= 原版那颗 `RectMask2D` 的等效物）");
            if (vc == null) { Summary(); return; }

            var contentAbs = NodeRectPx(content);
            var expectAbs = ExpectedContentAbs(card.transform);

            // ───────────────────────────────────────────────────────────────────────────────
            //  ① `Content` 视口 = 原版 529.8 × 280.83，且**在卡的实际位置**（⛔ 不是建树时的屏幕中心）
            // ───────────────────────────────────────────────────────────────────────────────
            CheckNear(contentAbs.W, ViewW, 0.5f, "① `Content` 视口宽 = 原版 `532.34 − 2.54` = 529.8");
            CheckNear(contentAbs.H, ViewH, 0.5f, "① `Content` 视口高 = 原版 `291.99 − 11.16` = 280.83");
            CheckNear(contentAbs.CX, expectAbs.CX, 0.6f, "① `Content` 中线 x = 卡实测中心 + 原版相对矩形（⛔ 不是屏心 960）");
            CheckNear(contentAbs.CY, expectAbs.CY, 0.6f, "① `Content` 中线 y = 卡实测中心 + 原版相对矩形");
            Check(contentAbs.CX > 1920f, "① `Content` 在屏外那一列（实得 x 中线 " + contentAbs.CX.ToString("F1")
                                        + "；旧实现会落在屏心 960）");

            // ───────────────────────────────────────────────────────────────────────────────
            //  ② 遮罩框 = `Content` 视口（+ 原版 padding）；🧨 漏写 `SetBaseRect` 那一步必红
            // ───────────────────────────────────────────────────────────────────────────────
            var clip = vc.ClipPx;
            Check(clip.HasValue, "② 遮罩节点给得出框（`ClipPx` 非 null；null = 节点不是 `RectTransform`/框算不出）");
            if (clip.HasValue)
            {
                var c = clip.Value;
                CheckNear(c.W, ViewW, 0.5f, "② 遮罩框宽 = `Content` 视口宽 529.8");
                CheckNear(c.H, ViewH, 0.5f, "② 遮罩框高 = `Content` 视口高 280.83");
                CheckNear(c.CX, contentAbs.CX, 0.6f, "② 遮罩框中线 x = `Content` 中线 x（框跟着卡走，⛔ 不留在建树位置）");
                CheckNear(c.CY, contentAbs.CY, 0.6f, "② 遮罩框中线 y = `Content` 中线 y");

                CheckNear(vc.padding.x, 0f, 0.001f, "② 遮罩 `m_Padding.x` = 原版 0（Left）");
                CheckNear(vc.padding.y, PadBottom, 0.001f, "② 遮罩 `m_Padding.y` = 原版 −15（Bottom，负 = 外扩）");
                CheckNear(vc.padding.z, 0f, 0.001f, "② 遮罩 `m_Padding.z` = 原版 0（Right）");
                CheckNear(vc.padding.w, 0f, 0.001f, "② 遮罩 `m_Padding.w` = 原版 0（Top）");
                Check(vc.softness == Vector2Int.zero, "② 遮罩 `m_Softness` = 原版 (0,0)（实得 " + vc.softness + "）");

                var render = vc.State.RenderClip;
                Check(render.HasValue, "② `RenderClip` 非 null（渲染那一份 = `Clip − pad`）");
                if (render.HasValue)
                {
                    CheckNear(render.Value.x1, c.x1, 0.01f, "② padding 只作用在下边 ⇒ 左沿不变");
                    CheckNear(render.Value.x2, c.x2, 0.01f, "② padding 只作用在下边 ⇒ 右沿不变");
                    CheckNear(render.Value.y1, c.y1, 0.01f, "② padding 只作用在下边 ⇒ 上沿不变");
                    CheckNear(render.Value.y2, c.y2 + 15f, 0.01f, "② `RenderClip` 下沿 = 框下沿 + 15（负 padding = 外扩）");
                }
            }

            // ───────────────────────────────────────────────────────────────────────────────
            //  ③ 三颗图标：每颗都有 quad；quad **全部落在遮罩框内**（= 原版被 `RectMask2D` 裁掉的情形）
            // ───────────────────────────────────────────────────────────────────────────────
            var quads = new ImageQuad[3];
            for (int i = 0; i < 3; i++)
            {
                quads[i] = QuadOf(content.GetChild(i));
                Check(quads[i] != null, "③ 图标[" + i + "] 有一颗 `Image` quad（null ⇒ 整块在裁切框外 / 没建）");
            }
            if (quads[0] == null || quads[1] == null || quads[2] == null) { Summary(); return; }

            var renderAll = vc.State.RenderClip;
            if (renderAll.HasValue)
            {
                var m = renderAll.Value;
                for (int i = 0; i < 3; i++)
                {
                    var q = MenuDraw.QuadRectPx(quads[i]);
                    bool inside = q.x1 >= m.x1 - 0.3f && q.x2 <= m.x2 + 0.3f
                               && q.y1 >= m.y1 - 0.3f && q.y2 <= m.y2 + 0.3f;
                    Check(inside, "③ 图标[" + i + "] 画出来的那块在遮罩框内（q=" + Fmt(q) + " · mask=" + Fmt(m) + "）");
                }
            }

            // ───────────────────────────────────────────────────────────────────────────────
            //  ④ 🧨 灭自证：第 1 / 3 颗被**裁掉 137.455px**，中间那颗**不裁**（去掉遮罩 ⇒ 三条必红）
            // ───────────────────────────────────────────────────────────────────────────────
            var node0 = NodeRectPx(content.GetChild(0));
            var node1 = NodeRectPx(content.GetChild(1));
            var node2 = NodeRectPx(content.GetChild(2));
            CheckNear(node0.x1, contentAbs.x1 - Overflow, 0.6f,
                      "④ 🧨 图标[0] 的**整块**左沿 = 视口左沿 − 137.455（去掉遮罩后它就是这么越界的）");
            CheckNear(node2.x2, contentAbs.x2 + Overflow, 0.6f,
                      "④ 🧨 图标[2] 的**整块**右沿 = 视口右沿 + 137.455");

            var q0 = MenuDraw.QuadRectPx(quads[0]);
            var q1 = MenuDraw.QuadRectPx(quads[1]);
            var q2 = MenuDraw.QuadRectPx(quads[2]);
            CheckNear(q0.x1, contentAbs.x1, 0.6f, "④ 🧨 图标[0] **画出来**的左沿 = 视口左沿（被遮罩截住）");
            CheckNear(q0.W, OriginalIconW - Overflow, 0.8f, "④ 🧨 图标[0] 画出来的宽 = 254.90341 − 137.455 = 117.448");
            CheckNear(q2.x2, contentAbs.x2, 0.6f, "④ 🧨 图标[2] **画出来**的右沿 = 视口右沿（被遮罩截住）");
            CheckNear(q2.W, OriginalIconW - Overflow, 0.8f, "④ 🧨 图标[2] 画出来的宽 = 254.90341 − 137.455 = 117.448");
            CheckNear(q1.x1, node1.x1, 0.6f, "④ 图标[1] 整块在视口内 ⇒ 左沿 = 它自己的左沿（没被裁）");
            CheckNear(q1.W, OriginalIconW, 0.8f, "④ 图标[1] 整块在视口内 ⇒ 宽 = 254.90341（没被裁）");
            Check(q0.W < OriginalIconW - 1f && q2.W < OriginalIconW - 1f,
                  "④ 🧨 两侧那两颗**确实被裁过**（去掉遮罩 ⇒ quad 宽回到全幅 254.90341）");

            // ───────────────────────────────────────────────────────────────────────────────
            //  ⑤ 裁切**只吃几何、不改布局**：三颗图标中心仍按 `sizeDelta + spacing` 等距排（align 4 居中）
            // ───────────────────────────────────────────────────────────────────────────────
            for (int i = 1; i < 3; i++)
            {
                var d = new Vector2(DesignPxOf(content.GetChild(i)).x - DesignPxOf(content.GetChild(i - 1)).x,
                                    DesignPxOf(content.GetChild(i)).y - DesignPxOf(content.GetChild(i - 1)).y);
                CheckNear(d.x, OriginalIconW + OriginalSpacing, 0.05f, "⑤ 图标[" + i + "] − 图标[" + (i - 1) + "] 的 x 间距");
                CheckNear(d.y, 0f, 0.05f, "⑤ 图标[" + i + "] 与图标[" + (i - 1) + "] 同一行（y 相同）");
            }
            CheckNear((DesignPxOf(content.GetChild(1)).x - contentAbs.CX), 0f, 0.6f,
                      "⑤ 中间那枚的中心 = `Content` 中线（HLG `childAlignment = 4` MiddleCenter）");

            // ───────────────────────────────────────────────────────────────────────────────
            //  ⑥ 🧨 判别式：整张卡再挪 (137,61) 重建 ⇒ **遮罩框跟着挪**（漏写 `SetBaseRect` 的实现必红）
            // ───────────────────────────────────────────────────────────────────────────────
            var beforeClip = vc.ClipPx;
            var placeB = new PxRect(placeA.x1 + 137f, placeA.y1 + 61f, placeA.x2 + 137f, placeA.y2 + 61f);
            MenuDraw.ApplyPxRect(card.transform, root, placeB);
            card.Clean();
            Check(card.ContentChildCount == 0, "⑥ （判别式前提）`Clean()` 之后 `Content.childCount == 0`");
            card.Initialize(evt);

            var content2 = ContentOf(card);
            Check(content2 != null && content2.childCount == 3, "⑥ （判别式前提）挪位重建后仍有 3 颗图标");
            if (content2 != null && content2.childCount == 3)
            {
                var content2Abs = NodeRectPx(content2);
                var clip2 = vc.ClipPx;
                Check(clip2.HasValue && beforeClip.HasValue, "⑥ （判别式前提）挪位前后都读得到遮罩框");
                if (clip2.HasValue && beforeClip.HasValue)
                {
                    CheckNear(clip2.Value.CX, content2Abs.CX, 0.6f,
                              "⑥ 🧨 挪位后遮罩框中线 x 跟着卡走 +137（实得位移 "
                            + (clip2.Value.CX - beforeClip.Value.CX).ToString("F2") + "）");
                    CheckNear(clip2.Value.CY, content2Abs.CY, 0.6f,
                              "⑥ 🧨 挪位后遮罩框中线 y 跟着卡走 +61（实得位移 "
                            + (clip2.Value.CY - beforeClip.Value.CY).ToString("F2") + "）");
                }
                // 挪完仍然裁掉两侧（不只是框在动、几何没动）
                var nq0 = QuadOf(content2.GetChild(0));
                var nq2 = QuadOf(content2.GetChild(2));
                Check(nq0 != null && nq2 != null, "⑥ （判别式前提）挪位重建后每颗图标仍有 quad");
                if (nq0 != null && nq2 != null)
                {
                    CheckNear(MenuDraw.QuadRectPx(nq0).W, OriginalIconW - Overflow, 0.8f,
                              "⑥ 🧨 挪位重建后图标[0] 仍被裁到 117.448 宽");
                    CheckNear(MenuDraw.QuadRectPx(nq2).W, OriginalIconW - Overflow, 0.8f,
                              "⑥ 🧨 挪位重建后图标[2] 仍被裁到 117.448 宽");
                }
            }

            Summary();
        }
        finally { Object.DestroyImmediate(root.gameObject); }
    }

    static string Fmt(PxRect r)
        => "(" + r.x1.ToString("F1") + "," + r.y1.ToString("F1") + ")→("
               + r.x2.ToString("F1") + "," + r.y2.ToString("F1") + ")";

    static void Summary()
        => Debug.Log(Tag + " 合计 通过=" + _pass + " 失败=" + _fail);
}
