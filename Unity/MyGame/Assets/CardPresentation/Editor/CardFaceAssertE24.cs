// CardFaceAssertE24.cs — 执行代理 `E24` 的断言宿主（2026-10-24 · 卡牌线）
//
// 覆盖三件（判据全在各自方法头的注释里）：
//   A. **立绘中心 y 按原版预制体分两族**（战斗卡 `CardPrefab` = −0.065 / 菜单·HUD 卡 UI = −0.044）
//   B. **兵种行（`RaceText`）y 按原版两颗节点分档**（单位·督军 −0.452 / 战术·防御 −0.490）
//   C. **`RenderPixelsIsolated` 连 UGUI 的 `CanvasRenderer` 一起关**（原来只关 `Renderer`，UGUI 一条都没关）
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod CardFaceAssertE24.Run -logFile -
//
// 判绿红：`grep "^E24"` 的**合计行**；逐条按**行首** `E24 ✓` / `E24 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// ⚠️ 本宿主**不建场景、不存盘**：只在临时根下建对象，收尾全部 `DestroyImmediate`。
// ⚠️ `CardProgress.ResetForTest()` 会**关掉落盘**并清空内存态 —— 必须先调，
//    否则会改写开发机上的玩家存档（`WarpforgeCardLevels.json`）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;
using CardPresentation;

public static class CardFaceAssertE24
{
    static int _pass, _fail;

    // ==================================================================
    //  期望值（**原版字面量**，⛔ 不是读 `CardView` 的常量再和自己比）
    // ==================================================================
    //
    //  判据出处（一手，逐字段实读；普查口径 = GO 名 `CardImage` / `RaceText` → `m_Component`
    //  → `RectTransform.m_AnchoredPosition`，91 个 bundle 全扫）：
    //    A · `bundle_battleprefabs_vfxandmisc_assets_all` 的
    //        `CardPrefab / 2DCard / Front / CardImage`：apos.y = **−0.065**（全库**只有这 1 个**）；
    //        其余 **151 个** `…/CardUI( Reference) / 2DCard / Front / CardImage`：apos.y = **−0.044**
    //        （`bundle_menus_assets_all` 41 · 13 个 arena 场景各 8 · mainmenuwarpforge 5 · staticgeneralassets 2 …）。
    //    B · 三份 bundle **逐位相同**的两颗兵种行：
    //        `MinionOrWarlord Container / RaceText`（90×21）apos.y = **−0.452**；
    //        `Tactic Container / RaceText`（**155×19**）apos.y = **−0.490**。
    //        换算成卡面 y01（`y01 = 0.5 + (0.7745 − apos.y)/3.3313`，父链 `Name and description` = −0.7745）：
    //          · 单位/督军：0.5 + (0.452 + 0.7745)/3.3313 = **0.868128**
    //          · 战术/防御：0.5 + (0.490 + 0.7745)/3.3313 = **0.879585**
    const float ArtYUiFamily = -0.044f;      // 菜单 / HUD 卡 UI（151 个实例）
    const float ArtYFightFamily = -0.065f;   // 战斗卡 `CardPrefab`（唯一那 1 个）
    const float RaceY01Unit = 0.868128f;
    const float RaceY01Tactic = 0.879585f;
    const float CardUnitH = 3.3313f;         // 2DCard.m_SizeDelta.y
    const float RaceParentY = -0.7745f;      // `Name and description` 的 apos.y

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepTier = CardView.DebugFrameTier;
        bool keepNoClamp = CardView.DebugDisableArtClamp;
        try
        {
            CardProgress.ResetForTest();          // ⛔ 先关卡进度落盘（见文件头）
            CardView.DebugFrameTier = 0;          // 卡框档 = 卡等级（正常口径）
            CardView.DebugDisableArtClamp = false;

            var pool = CardDatabase.Load();
            Debug.Log("[E24] 卡池 " + pool.Count + " 张 ⇒ 开始跑断言");

            RunArtFamily(pool);
            RunRaceAnchor(pool);
            RunIsolation();
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message
                       + "\n" + e.StackTrace);
        }
        finally
        {
            CardView.DebugFrameTier = keepTier;
            CardView.DebugDisableArtClamp = keepNoClamp;
        }
        Debug.Log("E24 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ==================================================================
    //  A · 立绘中心 y 分两族（`CardView.ArtUnitYFight` 那一节）
    // ==================================================================
    //
    //  为什么必须分族：上一轮账写「原版 −0.065 vs 我们 −0.044 ⇒ 照原版改成 −0.065」——
    //  那只对**手牌/场上**那张卡（`CardPrefab`）成立；−0.044 是**菜单/HUD 卡 UI** 那 151 个实例的原版值。
    //  全局改成 −0.065 会把放大窗 / 卡池 / 收藏 / 详情窗全部改错 0.021 卡单位。
    //
    //  ⚠️ 立绘中心的 y **烘在网格顶点里**（`ArtExtent` 的 `acy`，节点 `localPosition` 不含它）
    //     ⇒ 只能量 `_art` 那块网格的包围盒中心。
    //  ⚠️ 样本必须是**单位卡**：非单位卡的立绘网格被裁到卡框开口（`clipWindow`），包围盒中心 ≠ `acy`。
    static void RunArtFamily(List<CardDef> pool)
    {
        var def = FindByName(pool, "Howling Banshee Exarch") ?? FirstOfType(pool, "unit");
        Check(def != null, "A0 样本齐（要一张**单位卡**）");
        if (def == null) { Check(false, "A 组：没有单位卡样本 ⇒ 判不了（⛔ 不静默跳过）"); return; }

        var data = BattleDriver.ToCardData(def, def.Faction);
        var rootFull = new GameObject("E24_A_full");
        var rootHand = new GameObject("E24_A_hand");
        var rootSwap = new GameObject("E24_A_swap");
        try
        {
            var vFull = CardView.Create(rootFull.transform, data, "a_full", CardFace.Full);
            var vHand = CardView.Create(rootHand.transform, data, "a_hand", CardFace.Hand);
            Check(vFull != null && vHand != null, "A1 两张视图都建出来了（Full / Hand）");
            if (vFull == null || vHand == null) return;

            var mFull = vFull.ArtLayerMeshForTest;
            var mHand = vHand.ArtLayerMeshForTest;
            Check(mFull != null && mFull.vertexCount >= 4, "A2 前提：`Full` 那张建出了底层立绘网格（`_art`）");
            Check(mHand != null && mHand.vertexCount >= 4, "A3 前提：`Hand` 那张建出了底层立绘网格（`_art`）");
            if (mFull == null || mHand == null) return;

            // `s = Width / CardUnitW = 2.0927 / 2.0927 = 1` ⇒ 世界单位的 y 就是卡单位。
            float yFull = mFull.bounds.center.y;
            float yHand = mHand.bounds.center.y;

            CheckClose(yFull, ArtYUiFamily, 2e-3f,
                       "A4 菜单/HUD 卡 UI 族（`CardFace.Full`）立绘中心 = 原版 **−0.044**");
            CheckClose(yHand, ArtYFightFamily, 2e-3f,
                       "A5 战斗卡族（`CardFace.Hand`）立绘中心 = 原版 **−0.065**（`CardPrefab`）");
            CheckClose(yHand - yFull, ArtYFightFamily - ArtYUiFamily, 2e-3f,
                       "A6 两族之差 = 0.021 卡单位（**灭自证**：把两族合成一处赋值 ⇒ A5/A6 同时红）");

            // A7/A8：**跨族换形态**（`SetFace` 那个入口）也要跟着重烘 —— 否则网格停在旧族（静默）。
            var vSwap = CardView.Create(rootSwap.transform, data, "a_swap", CardFace.Full);
            var m0 = vSwap.ArtLayerMeshForTest;
            Check(m0 != null && Mathf.Abs(m0.bounds.center.y - ArtYUiFamily) < 2e-3f,
                  "A7 前提：换形态那张出生是 `Full`（立绘中心 −0.044）");
            vSwap.SetFace(CardFace.Hand);
            var m1 = vSwap.ArtLayerMeshForTest;
            Check(m1 != null && Mathf.Abs(m1.bounds.center.y - ArtYFightFamily) < 2e-3f,
                  "A8 `SetFace(Full → Hand)` 之后立绘网格重烘到 **−0.065**"
                  + "（实得 " + (m1 != null ? m1.bounds.center.y.ToString("F4") : "null") + "）");

            // A9：卡框的 **uv2** 是拿立绘的板算的（`FrameMesh` ← `ArtUvAt` ← `ArtBoard`）——
            //     立绘中心一动，遮罩必须跟着动。量 uv2 的**上下沿差**（与卡框自身的 uv 无关）。
            float fvFull = Uv2Mean(vFull.LayerMeshForTest("frame"));
            float fvHand = Uv2Mean(vHand.LayerMeshForTest("frame"));
            Check(!float.IsNaN(fvFull) && !float.IsNaN(fvHand) && Mathf.Abs(fvFull - fvHand) > 0.002f,
                  "A9 卡框 uv2（立绘遮罩）也跟着族走：两族 v 的均值差 = "
                  + (Mathf.Abs(fvFull - fvHand)).ToString("F4") + "（要 > 0.002 · 期望 0.021/2.858≈0.0073）");
        }
        finally
        {
            Kill(rootFull); Kill(rootHand); Kill(rootSwap);
        }
    }

    /// <summary>网格 uv2 的 **v 均值**（⚠️ 不能用 max−min：那是平移不变量，遮罩整体下移时它**不变**）。
    /// 没有 uv2 返回 NaN。</summary>
    static float Uv2Mean(Mesh m)
    {
        if (m == null) return float.NaN;
        var uv = m.uv2;
        if (uv == null || uv.Length == 0) return float.NaN;
        float s = 0f;
        for (int i = 0; i < uv.Length; i++) s += uv[i].y;
        return s / uv.Length;
    }

    // ==================================================================
    //  B · 兵种行 y 分档（`CardView.TacticRaceAt` 那一节）
    // ==================================================================
    //
    //  原版有**两颗** `RaceText`（判据见那一段表）：单位/督军 = `MinionOrWarlord Container`（−0.452）、
    //  战术/防御 = `Tactic Container`（−0.490，且框宽 155 vs 90）。选哪颗由原版
    //  `CardTextsController.SetTexts` 的 `state = cardType ∈ {20,40} ? 3+(p!=0) : 1+…` 决定。
    //
    //  ⚠️ 「印不印那一行」是**另一条判据**（`CardView.FaceShowsRace` / `SubtypeLine`）—— 本组不碰它。
    //  ⚠️ 两条读数一起断：`RaceAnchorY01ForTest`（判据值）与 `RacePlacedCenterYForTest`（实况几何）。
    static void RunRaceAnchor(List<CardDef> pool)
    {
        var unitDef = FirstOfType(pool, "unit");
        var tactDef = FirstMatching(pool, "tactic", true);
        var tactNoLine = FirstMatching(pool, "tactic", false);
        var defDef = FirstOfType(pool, "defence");

        Check(unitDef != null && tactDef != null && defDef != null,
              "B0 样本齐（unit=" + (unitDef != null) + " tactic(印兵种行)=" + (tactDef != null)
              + " defence=" + (defDef != null) + "）");
        if (unitDef == null || tactDef == null || defDef == null)
        { Check(false, "B 组：样本不齐 ⇒ 判不了（⛔ 不静默跳过）"); return; }

        MeasureRace(unitDef, out float a1, out float a2);
        MeasureRace(tactDef, out float b1, out float b2);
        MeasureRace(defDef, out float d1, out float d2);

        Check(!float.IsNaN(a1), "B1 前提：单位卡印着兵种行（`RaceText` 那一层建出来了）");
        if (!float.IsNaN(a1))
        {
            CheckClose(a1, RaceY01Unit, 2e-4f, "B2 单位卡兵种行锚点 y01 = 原版 **0.8681**（`MinionOrWarlord` 的 −0.452）");
            CheckClose(a2, (0.5f - RaceY01Unit) * CardUnitH, 3e-3f,
                       "B3 单位卡兵种行**实摆**中心 y = (0.5 − 0.8681) × 3.3313 = −1.2265 卡单位");
        }
        Check(!float.IsNaN(b1), "B4 前提：这张战术卡印着兵种行");
        if (!float.IsNaN(b1))
        {
            CheckClose(b1, RaceY01Tactic, 2e-4f, "B5 战术卡兵种行锚点 y01 = 原版 **0.8796**（`Tactic Container` 的 −0.490）");
            CheckClose(b2, (0.5f - RaceY01Tactic) * CardUnitH, 3e-3f,
                       "B6 战术卡兵种行**实摆**中心 y = (0.5 − 0.8796) × 3.3313 = −1.2645 卡单位");
        }
        // ⚠️ 账上那个 **0.038** 是**原版 apos 之差**（−0.490 比 −0.452），不是 y01 之差 ——
        //    y01 之差 = 0.038 / 3.3313 = 0.0114。这里换回卡单位去比，才对得上那条账。
        Check(!float.IsNaN(a1) && !float.IsNaN(b1) && Mathf.Abs((b1 - a1) * CardUnitH - 0.038f) < 5e-4f,
              "B7 两档之差 = 原版 apos 的 **0.038 卡单位**（账上那条数；**灭自证**：两档合并 ⇒ B5/B7 同时红）"
              + "（实得 " + ((b1 - a1) * CardUnitH).ToString("F4") + " 卡单位 / " + (b1 - a1).ToString("F5") + " y01）");

        Check(!float.IsNaN(d1), "B8 前提：这张防御卡印着兵种行（原版 `cardType` 也是 20 ⇒ 同一颗 `Tactic RaceText`）");
        if (!float.IsNaN(d1))
            CheckClose(d1, RaceY01Tactic, 2e-4f, "B9 防御卡走**战术**那一档（y01 0.8796，⛔ 不是 0.8681）");

        // B10：不印那一行的战术卡 ⇒ 层不存在（`NaN`）—— ⛔ 别把 NaN 读成通过。
        if (tactNoLine != null)
        {
            MeasureRace(tactNoLine, out float n1, out float n2);
            Check(float.IsNaN(n1), "B10 不印兵种行的战术卡**没有**那一层（锚点应为 NaN，实得 "
                                   + n1.ToString("F4") + "；卡 `" + tactNoLine.Name + "`）");
        }
        else Debug.Log("[E24] B10 跳过：卡池里找不到「不印兵种行的战术卡」样本（不是通过，是没验到）");
    }

    /// <summary>建一张 `Full` 形态的卡，读两个兵种行读数，然后**当场销毁**（本宿主不留残余对象）。</summary>
    static void MeasureRace(CardDef def, out float anchorY01, out float placedCenterY)
    {
        anchorY01 = placedCenterY = float.NaN;
        var root = new GameObject("E24_B_" + SafeName(def.Name));
        try
        {
            var d = BattleDriver.ToCardData(def, def.Faction);
            var v = CardView.Create(root.transform, d, "b_card", CardFace.Full);
            if (v == null) return;
            anchorY01 = v.RaceAnchorY01ForTest;
            placedCenterY = v.RacePlacedCenterYForTest;
        }
        finally { Kill(root); }
    }

    // ==================================================================
    //  C · `RenderPixelsIsolated` 连 UGUI `CanvasRenderer` 一起关
    // ==================================================================
    //
    //  旧实现只把 `Renderer.forceRenderingOff = true` —— `MeshRenderer` / `SpriteRenderer` /
    //  `ParticleSystemRenderer` 都是 `Renderer`，但 UGUI 那几个 Graphic 画的是 `CanvasRenderer`
    //  （`Component` 的子类，**不在** `Renderer` 的继承树上）⇒ 一条都没关掉，落在相机前面的
    //  `ScreenSpaceCamera` / `WorldSpace` Canvas 会混进像素 A/B 的差集。
    //
    //  C 组三件：
    //    C0/C1/C2 · **普通渲染器**那条路（对照组，证明夹具与量具都真在动）
    //    C3/C4    · **UGUI 那条路**（新补的：关掉 + 还原）
    //    C5       · **像素证据**（前提成立时才断；不成立时**大声说判不了**，⛔ 不静默当通过）
    static void RunIsolation()
    {
        var root = new GameObject("E24_C_root");
        var outsider = new GameObject("E24_C_outsider");
        var canvasGo = (GameObject)null;
        try
        {
            var rootQuad = MakeQuad("rootQuad", root.transform, new Vector3(0f, -0.03f, 0f), 0.5f,
                                    new Color(0f, 1f, 0f, 1f));
            var outQuad = MakeQuad("outQuad", outsider.transform, new Vector3(0f, -0.03f, -10f), 3f,
                                   new Color(1f, 0f, 0f, 1f));
            if (rootQuad == null || outQuad == null)
            {
                Check(false, "C0 夹具建不出来（没有可用的纯色 shader）⇒ C 组判不了（⛔ 不静默跳过）");
                return;
            }
            Check(true, "C0 夹具建出来了（两块纯色 quad；shader = " + QuadShader.name + "）");

            var plain = CardFaceProbe.RenderPixelsForTest();
            Color32 cP = PixelAt(plain, 0.5f, 0.5f);
            Check(Near(cP, new Color(1f, 0f, 0f, 1f), 0.15f),
                  "C1 前提：**不隔离**的整场渲染里，挡在前面的那块红 quad 画得出来（中心像素 = " + Show(cP) + "）");

            var iso = CardFaceProbe.RenderPixelsIsolatedForTest(root);
            Color32 cI = PixelAt(iso, 0.5f, 0.5f);
            Check(Near(cI, new Color(0f, 1f, 0f, 1f), 0.15f),
                  "C2 隔离渲染只留 `root` 子树 ⇒ 中心像素 = 绿（实得 " + Show(cI) + "）；"
                  + "关掉了 " + CardFaceProbe.LastIsolatedCulledRenderers + " 个 `Renderer`");

            // ---- UGUI：WorldSpace Canvas + Image 当第二个「入侵者」----
            canvasGo = MakeWorldCanvas(new Vector3(0f, -0.03f, -8f), new Color(0f, 0f, 1f, 1f));
            var cr = canvasGo != null ? canvasGo.GetComponentInChildren<CanvasRenderer>(true) : null;
            Check(cr != null, "C3 前提：UGUI 那块 `CanvasRenderer` 建出来了");
            if (cr == null) return;

            float alpha0 = cr.GetAlpha();
            bool cull0 = cr.cull;
            CardFaceProbe.RenderPixelsIsolatedForTest(root);
            Check(CardFaceProbe.LastIsolatedCulledCanvasRenderers >= 1,
                  "C4 隔离渲染**真的关到了** UGUI 的 `CanvasRenderer`（计数 = "
                  + CardFaceProbe.LastIsolatedCulledCanvasRenderers + "，改之前恒 **0**）"
                  + "（实得 " + CardFaceProbe.LastIsolatedCulledCanvasRenderers + "）");
            Check(!cr.cull && Mathf.Abs(cr.GetAlpha() - alpha0) < 1e-4f,
                  "C5 渲完**原样还原**（`cull` " + cull0 + "→" + cr.cull + " · alpha "
                  + alpha0.ToString("F3") + "→" + cr.GetAlpha().ToString("F3") + "）");

            // ---- 像素证据（前提：这块 UGUI 在不隔离的渲染里画得出来）----
            var plain2 = CardFaceProbe.RenderPixelsForTest();
            Color32 cBlue = PixelAt(plain2, 0.5f, 0.5f);
            bool uGUI可见 = Near(cBlue, new Color(0f, 0f, 1f, 1f), 0.15f);
            if (uGUI可见)
            {
                var iso2 = CardFaceProbe.RenderPixelsIsolatedForTest(root);
                Color32 cIso2 = PixelAt(iso2, 0.5f, 0.5f);
                Check(!Near(cIso2, new Color(0f, 0f, 1f, 1f), 0.15f),
                      "C6 隔离后那块 UGUI（蓝）**不在画面里**（中心像素 = " + Show(cIso2) + "）—— 逐像素证据"
                      + "（**灭自证**：不关 `CanvasRenderer` ⇒ 这里仍是蓝 ⇒ 红）");
            }
            else
            {
                // ⛔ 不许静默当通过：大声说这一条**没验到**，以及为什么。
                Debug.Log("[E24] C6 **判不了**（不是通过）：这块 UGUI 在**不隔离**的整场渲染里也没画出来 "
                        + "（中心像素 = " + Show(cBlue) + "）—— 批处理/编辑器下 Canvas 的几何没进相机 RT。"
                        + "⇒ UGUI 那条路以 C4/C5（组件状态）为准；要拿到像素级证据，得先让这块 Canvas 在编辑器里真渲出来。");
            }
        }
        finally
        {
            Kill(root); Kill(outsider); Kill(canvasGo);
        }
    }

    static Shader _quadShader;
    static Shader QuadShader
    {
        get
        {
            if (_quadShader == null)
                _quadShader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            return _quadShader;
        }
    }

    /// <summary>一块**双面**的纯色 quad（相机在 z = −20 往 +z 看 ⇒ 正反两面都要画）。</summary>
    static GameObject MakeQuad(string name, Transform parent, Vector3 pos, float half, Color c)
    {
        var sh = QuadShader;
        if (sh == null) return null;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var mat = new Material(sh);
        if (mat.HasProperty("_Color")) mat.color = c;
        mr.sharedMaterial = mat;
        var m = new Mesh { name = "E24quad_" + name };
        m.vertices = new[]
        {
            new Vector3(-half, -half, 0f), new Vector3(half, -half, 0f),
            new Vector3(half, half, 0f), new Vector3(-half, half, 0f),
        };
        m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };   // 两个绕向都写 ⇒ 不受背面剔除影响
        m.RecalculateNormals();
        m.RecalculateBounds();
        mf.sharedMesh = m;
        return go;
    }

    /// <summary>一块 WorldSpace 的 UGUI Canvas（+ 一张铺满它的 `Image`）。取不到就返回 null。</summary>
    static GameObject MakeWorldCanvas(Vector3 pos, Color c)
    {
        var go = new GameObject("E24_C_canvas");
        go.transform.position = pos;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(6f, 6f);
        rt.localScale = Vector3.one * 0.5f;
        var imgGo = new GameObject("panel");
        imgGo.transform.SetParent(go.transform, false);
        var irt = imgGo.AddComponent<RectTransform>();
        irt.sizeDelta = new Vector2(6f, 6f);
        var img = imgGo.AddComponent<UnityEngine.UI.Image>();
        img.color = c;
        Canvas.ForceUpdateCanvases();
        return go;
    }

    /// <summary>按**归一化 uv**（0..1，左下原点）取一个像素 —— 用 `RenderFrameForTest` 把世界坐标换算过去，
    /// ⛔ 不在断言里另抄一遍相机参数。</summary>
    static Color32 PixelAt(Color32[] px, float u, float v)
    {
        int W = CardFaceProbe.RenderWidthForTest, H = CardFaceProbe.RenderHeightForTest;
        int x = Mathf.Clamp(Mathf.RoundToInt(u * (W - 1)), 0, W - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(v * (H - 1)), 0, H - 1);
        return px[y * W + x];
    }

    static bool Near(Color32 a, Color b, float tol)
    {
        return Mathf.Abs(a.r / 255f - b.r) <= tol
            && Mathf.Abs(a.g / 255f - b.g) <= tol
            && Mathf.Abs(a.b / 255f - b.b) <= tol;
    }

    static string Show(Color32 c) { return "(" + c.r + "," + c.g + "," + c.b + ")"; }

    // ==================================================================
    //  小工具
    // ==================================================================
    static CardDef FindByName(List<CardDef> pool, string name)
    {
        foreach (var d in pool) if (d != null && d.Name == name) return d;
        return null;
    }

    static CardDef FirstOfType(List<CardDef> pool, string type)
    {
        foreach (var d in pool) if (d != null && d.Type == type) return d;
        return null;
    }

    /// <summary>第一张 `type` 匹配、且（`wantLine` ⇒ 印兵种行 / 否则不印）的卡。
    /// 「印不印」走**生产口径** `CardView.FaceShowsRace`（⛔ 不在断言里另写一份 `SubtypeLine` 的规则）。</summary>
    static CardDef FirstMatching(List<CardDef> pool, string type, bool wantLine)
    {
        foreach (var d in pool)
        {
            if (d == null || d.Type != type) continue;
            var cd = BattleDriver.ToCardData(d, d.Faction);
            if (CardView.FaceShowsRace(cd) == wantLine) return d;
        }
        return null;
    }

    static string SafeName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "x";
        var sb = new System.Text.StringBuilder();
        foreach (var ch in s) sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        return sb.ToString();
    }

    static void Kill(GameObject go) { if (go != null) Object.DestroyImmediate(go); }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("E24 ✓ " + msg); }
        else { _fail++; Debug.LogError("E24 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol, msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
                                             + "；实得 " + got.ToString("F4") + "）");
    }
}
