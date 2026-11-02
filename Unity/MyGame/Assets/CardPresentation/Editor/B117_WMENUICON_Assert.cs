// B117_WMENUICON_Assert.cs — 执行代理 `B117-WMENUICON` 的断言宿主
//
// 任务 = 修掉真机发现的「**主界面（模式选择）三枚阵营图标浮在屏幕中间**」。
// 判据 → `资料/完全复刻/判据/B117-GC_界面四条.md` §GC-1（原版实拍 `主界面参考图.png` 的模式卡上**没有任何徽章**）。
//
// 病灶（已查实，`文件:行号`）：
//   `Shell/RankedArmyBonusCard.cs` 的 `Build()` 把卡根摆在**屏幕中心**（原版 prefab 的 `anchoredPosition (0,−43.8868)`
//   是相对窗口说的），随后宿主 `Shell/MainMenuRuntime.cs:5221` 把卡根挪到 first-fit 网格的第 5 列（x 2425…2960，**屏外**）。
//   子件跟着父件走，但 `Initialize()` 里建 `Simple Army Icon` 用的绝对矩形是**建树那一份**（屏幕中心）
//   ⇒ `MenuDraw.Local` 的 `localPosition = RectCenter(abs) − PosInDesignSpace(Content)` 恰好把父件位移抵消掉
//   ⇒ 三枚徽记落回屏幕中心，压在小冲突 / 排位两张模式卡上。
//
// 本文件断的是「**修好了没有**」，不是「我们的常量自洽」——
//   期望值一律来自**原版判据**（prefab 的 `sizeDelta.x 254.90341` / HLG `spacing 20` / `Content` 相对矩形
//   `2.54,11.16→532.34,291.99`，见 `判据/B90_C2b_远端prefab几何.md` §E-1 第 207-209 行）与**宿主的生产矩形**
//   （`MainMenuRuntime.cs:5221` 那条 first-fit 第 5 列），⛔ 不读 `RankedArmyBonusCard` 的私有常量反推。
//
// 🧨 **灭自证**：第 ⑤ 组那条「把卡挪 (137,61) ⇒ 图标必须跟着挪 (137,61)」在**旧实现下必红**
//   （旧实现里图标的**绝对**位置与卡无关，delta 恒 = 0）；第 ④ 组「三枚图标全在屏外」同样必红（旧 = 685/960/1235）。
//   两条都是**结构性**判别式：改回旧写法不可能同时满足。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_WMENUICON_Assert.Run -logFile -
//
// 判绿红：`grep "^B117WMENUICON"` 的**合计行**；逐条按**行首** `B117WMENUICON ✓` / `B117WMENUICON ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// ⚠️ **接线状态（如实记）**：本代理的写权限不含 `Editor/*Scene.cs` ⇒ 本 `Run()` 目前**没有宿主调用它**。
//   由调度台挂进 `Editor/MainMenuScene.cs`（或 `ShellScene.cs`）一行即可：
//   `B117_WMENUICON_Assert.Run();` —— 不需要场景里任何东西（本文件自建探测根）。
using UnityEngine;
using CardPresentation;

public static class B117_WMENUICON_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    const string Tag = "B117WMENUICON";

    /// <summary>原版 prefab 判据（`判据/B90_C2b_远端prefab几何.md` §E-1，⛔ 不是我们的常量）：
    /// `Simple Army Icon` 的 `sizeDelta.x` = 254.90341；`Content` 的 `HorizontalLayoutGroup.spacing` = 20。</summary>
    const float OriginalIconW = 254.90341f, OriginalSpacing = 20f;
    /// <summary>原版 `Content` 的相对矩形（卡左上原点）：`2.54,11.16 → 532.34,291.99`。</summary>
    const float ContentX1 = 2.54f, ContentX2 = 532.34f, ContentY1 = 11.16f, ContentY2 = 291.99f;
    /// <summary>宿主摆放用的卡尺寸（`MainMenuRuntime.cs:2395` 的 `CardW=535 / CardH=414.4`）。</summary>
    const float HostCardW = 535f, HostCardH = 414.4f;
    /// <summary>设计基准宽度（= `LayoutSpace.DesignPxW`；屏幕右缘 x = 1920）。</summary>
    const float ScreenW = 1920f;

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
    /// <summary>一个节点中心 → **设计 px**（左上原点、y 向下）。`ToDesignPixel` 是 `FromPixel` 的逆，
    /// 与 `MenuDraw.ApplyPxRect` 用的是同一条 `VisibleWidth` ⇒ 任意宽高比下都成立。</summary>
    static Vector2 DesignPxOf(Transform t)
        => LayoutSpace.ToDesignPixel(MenuDraw.PosInDesignSpace(t));

    /// <summary>`t` 中心相对 `refT` 中心的**设计 px** 位移（x 向右 / y 向下，与 `PxRect` 同向）。
    /// ⚠️ 两个量都从**世界坐标差**换算 ⇒ 画布中心那个仿射偏移自己约掉，与相机在哪无关。</summary>
    static Vector2 DeltaPx(Transform t, Transform refT)
    {
        var p = t.position; var q = refT.position;
        return new Vector2((p.x - q.x) * LayoutSpace.PxPerWorldX,
                           -(p.y - q.y) * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight));
    }

    /// <summary>`Dynamic Content ▸ Viewport ▸ Content`（= 原版 `armyContentAnchor`）。⛔ 用 `Transform.Find` 走路径，
    /// 不加公开读口（那几个私有 List 是老账 `:197/:198` 的判据对象，别为了断言改它的可见性）。</summary>
    static Transform ContentOf(RankedArmyBonusCard card)
        => card == null ? null : card.transform.Find("Dynamic Content/Viewport/Content");

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  入口
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    public static void Run()
    {
        _pass = _fail = 0;
        var root = new GameObject("B117_WMENUICON_Probe", typeof(RectTransform)).transform;
        try
        {
            var card = RankedArmyBonusCard.Create(root);
            Check(card != null, "（前提）`Reward Event Container 1x1` 建得出来（`Create` 只在 parent 为空时返 null）");
            if (card == null) { Summary(); return; }

            // 3 个阵营：与生产数据 `Resources/Art/ranked_army_bonus.json` 的 `affectedArmies:[10,50,110]` 同长度
            // （判据 `B117-GC` §GC-1 第 14 行）；⛔ 不读 JSON ⇒ 断言与数据文件解耦。
            var evt = new RankedArmyBonusData.Event { title = "T", endsInSeconds = 3600f, affectedArmies = new[] { 10, 50, 110 } };

            // ── 生产矩形：`MainMenuRuntime.cs:5221` 的 first-fit 第 5 列（16:9 ⇒ CardCol0 205 + 4×555 = 2425）
            //    y = CardRow0Top 182.8；尺寸 = 535 × 414.4（`MainMenuRuntime.cs:2395`）
            var placeA = new PxRect(2425f, 182.8f, 2425f + HostCardW, 182.8f + HostCardH);
            MenuDraw.ApplyPxRect(card.transform, root, placeA);
            card.Initialize(evt);

            var content = ContentOf(card);
            Check(content != null, "（前提）`Dynamic Content ▸ Viewport ▸ Content` 找得到（= 原版 `armyContentAnchor`）");
            Check(card.ArmyImageCount == 3, "（前提）`Initialize` 后 `currentArmyImages.Count == 3`（实得 "
                                            + card.ArmyImageCount + "）");
            if (content == null || content.childCount != 3)
            {
                Summary(); return;                                  // 前提不成立 ⇒ 下面的量尺没有对象（⛔ 不静默跳过）
            }
            Check(card.ContentChildCount == 3, "（前提）`Content.childCount == 3`");

            // ───────────────────────────────────────────────────────────────────────────────
            // ④ ★ 三枚图标**都不在屏幕里**（旧实现落回屏幕中心 685 / 960 / 1235 ⇒ 三条全红）
            // ───────────────────────────────────────────────────────────────────────────────
            for (int i = 0; i < content.childCount; i++)
            {
                var px = DesignPxOf(content.GetChild(i));
                Check(px.x - OriginalIconW * 0.5f >= ScreenW + 60f,
                      "★ 图标[" + i + "] 的**左沿**在设计 px " + (px.x - OriginalIconW * 0.5f).ToString("F1")
                    + " ⇒ 整颗在 1920 宽屏幕之外（旧实现 = 557.6 / 832.5 / 1107.4 ⇒ 压在小冲突/排位两张卡上）");
            }

            // ───────────────────────────────────────────────────────────────────────────────
            // ⑤ ★ 图标在**自己那张卡**的坐标系里：横向偏离 ≤ 全组半宽、中位那枚 ≈ 卡中线、纵向 = `Content` 中线
            // ───────────────────────────────────────────────────────────────────────────────
            float groupHalf = (3 * OriginalIconW + 2 * OriginalSpacing) * 0.5f;      // = 402.3551
            float contentMidX = (ContentX1 + ContentX2) * 0.5f;                       // = 267.44（相对卡左上）
            float contentMidY = (ContentY1 + ContentY2) * 0.5f;                       // = 151.575
            float cardMidX = HostCardW * 0.5f, cardMidY = HostCardH * 0.5f;           // = 267.5 / 207.2

            for (int i = 0; i < content.childCount; i++)
            {
                var d = DeltaPx(content.GetChild(i), card.transform);
                CheckNear(d.x, contentMidX - cardMidX + (i - 1) * (OriginalIconW + OriginalSpacing), 0.6f,
                          "★ 图标[" + i + "] 相对卡中心的横向位移 = `Content` 中线 + (i−1)×(254.90341+20)");
                CheckNear(d.y, contentMidY - cardMidY, 1.0f,
                          "★ 图标[" + i + "] 相对卡中心的纵向位移 = `Content` 中线（⛔ 不是建树时的屏幕中心）");
                bool inside = Mathf.Abs(d.x) <= groupHalf + 1f && Mathf.Abs(d.y) <= cardMidY + 1f;
                Check(inside, "★ 图标[" + i + "] 落在卡根的矩形范围内（全组半宽 " + groupHalf.ToString("F2")
                            + " / 卡半高 " + cardMidY.ToString("F2") + "；实得 Δ=("
                            + d.x.ToString("F1") + ", " + d.y.ToString("F1") + ")）");
            }

            // 相邻间距 == 原版 `sizeDelta.x(254.90341) + HLG spacing(20)`（几何自检，⛔ 别按我们的常量反推）
            for (int i = 1; i < content.childCount; i++)
            {
                var d = DeltaPx(content.GetChild(i), content.GetChild(i - 1));
                CheckNear(d.x, OriginalIconW + OriginalSpacing, 0.05f, "间距 图标[" + i + "] − 图标[" + (i - 1) + "]");
                CheckNear(d.y, 0f, 0.05f, "同一行：图标[" + i + "] 与图标[" + (i - 1) + "] 的 y 相同");
            }

            // ───────────────────────────────────────────────────────────────────────────────
            // 🧨 判别式（**旧实现必红**）：卡整体挪 (137, 61) ⇒ 三枚图标必须**逐颗跟着挪**同样多
            //    旧实现里图标被算回「建树时的绝对矩形」 ⇒ delta 恒 = (0, 0) ⇒ 本组三条全红
            // ───────────────────────────────────────────────────────────────────────────────
            var before = new Vector2[3];
            for (int i = 0; i < 3; i++) before[i] = DesignPxOf(content.GetChild(i));

            var placeB = new PxRect(placeA.x1 + 137f, placeA.y1 + 61f, placeA.x2 + 137f, placeA.y2 + 61f);
            MenuDraw.ApplyPxRect(card.transform, root, placeB);
            card.Clean();
            Check(card.ContentChildCount == 0, "（判别式前提）`Clean()` 之后 `Content.childCount == 0`");
            card.Initialize(evt);

            var content2 = ContentOf(card);
            Check(content2 != null && content2.childCount == 3, "（判别式前提）挪位重建后仍有 3 颗图标");
            if (content2 != null && content2.childCount == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    var after = DesignPxOf(content2.GetChild(i));
                    var moved = after - before[i];
                    CheckNear(moved.x, 137f, 0.5f,
                              "🧨 判别式：卡挪 +137px ⇒ 图标[" + i + "] 跟着挪 +137px（旧实现恒 0 ⇒ 红）");
                    CheckNear(moved.y, 61f, 0.5f,
                              "🧨 判别式：卡挪 +61px ⇒ 图标[" + i + "] 跟着挪 +61px（旧实现恒 0 ⇒ 红）");
                }
                // 反证：挪位后**仍然**相对卡中线（⛔ 不是相对某个绝对坐标）—— 与上面两条互为冗余，
                // 挡「把卡和图标一起挪回去」那种两边同时改的骗法。
                var c2 = DesignPxOf(content2.GetChild(1)) - DesignPxOf(card.transform);
                CheckNear(c2.x, contentMidX - cardMidX, 0.6f, "🧨 判别式：挪位后中位图标仍 = 卡中线（⛔ 不是旧屏幕中心）");
            }

            Summary();
        }
        finally { Object.DestroyImmediate(root.gameObject); }
    }

    static void Summary()
        => Debug.Log(Tag + " 合计 通过=" + _pass + " 失败=" + _fail);
}
