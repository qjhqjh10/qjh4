// B117_G10_18_Assert.cs — 执行代理 `B117-G10-18`（战斗域第 18 部分：**回合流转 · 加时 · 等待提示**）
//                          的断言宿主。
//
// ============================ 它盯的是什么 ============================
//   本批**当场改掉**的两处 + 三个「原来就没有判据」的口，全部按**第一权威反编译**写：
//
//   A. `TipPrebattle` 三段时间线（原版 `TipPrebattle._OpenTip/_CloseTip/_DeactivateTip_d__*_MoveNext.c`）
//        · `OpenTip`  = 展开起播(0.3，**不等待**) → 等 `timeToOpenBg×0.5` → 换字 + 淡入(0.3，**不等待**)
//                       → 再等 `timeToOpenBg` ⇒ **总 0.45 s**（改前我们**串行**等成 1.05 s）
//        · `CloseTip` = 淡出 + 压扁同时起播 → 只等 `timeToOpenBg×0.5` **一次** ⇒ **总 0.15 s**（改前 0.3 s）
//        · `DeactivateTip` = 等 **`timeToOpenBg`**（原版 `+0x34`，完整一段）⇒ **0.3 s**（改前取一半 0.15 s）
//       乘数 `DAT_1834b2bb4` 用 `工具/read_literal.py` 对 `GameAssembly.dll` 实读 = **0.5f**。
//
//   B. `TipPrebattle` 收起态的 **`localScale = (0, y, 0)`**（原版 `TipPrebattle__SetupClosed.c:24-27`
//       与 `_OpenTip_d__17__MoveNext.c:37-40`：`local_18 = (ulonglong)*(uint *)(lVar3 + 4) << 0x20; local_10 = 0`）
//       —— 改前我们把 **z 保留成 1**。
//
//   C. `WaitBanner`（原版 `WaitText`）**本来就有**的几何/层级/吃点击那一组，本轮补上判定读口
//      （原来只有 `Editor/BattleScene.cs` 的几条，且**没有**「压暗只吃上中部」这一条的判据）。
//
// 判据正本 → `资料/完全复刻/判据/B117-G10-18_回合流转与等待提示.md`
//   ⚠️ 本文件里写死的**期望值全部是原版的原始数值**（反编译 `.c` / 运行时 dump / prefab 实读），
//      ⛔ 不是「读我们的常量来证明我们的常量」（那就是自证）。
//
// ============================ 怎么用（宿主）============================
//   `B117_G10_18_Assert.Run();`
//   · 本文件**零前提**：自己建相机 + `LayoutSpace.Apply` + 一棵临时根，收尾自己 `DestroyImmediate`。
//   · ⚠️ 它**不销毁** `HudCanvas` 根画布（静态件，别处还要复用；同 `Editor/BattleScene.cs` 的做法）。
//   · 批处理跑法（**只有调度台/主对话能跑**，子代理不许 `-executeMethod`）：
//       unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//         -projectPath "D:\4\Unity\MyGame" -executeMethod B117_G10_18_Assert.Run -logFile -
//   · 判绿红看 `B117G1018 ✓` / `B117G1018 ✗` 的**行首**；⛔ 别 `grep -c ✗`（`✗` 会出现在文案里）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1/A2/A3：把三条时间线改回「串行 / 多等一段 / 取一半」⇒ 三条**各自**红。
//   · A5：把 `OpenTipTotalSecondsFor` 写成「展开完再等 0.15 再淡入 0.3 再等 0.3」（= 改前那版 1.05 s）
//          ⇒ A5（"≠1.05"）与 A1（"==0.45"）**同时**红 —— 光断"==0.45"也能红，A5 是把**改前那个值**也钉死。
//   · A6：把 `OpenTipTextChangeSeconds` 改成「等展开完再换字」（= 串行）⇒ 它不再 `<= timeToOpenBg`
//          ⇒ A6 红（这条断的是**结构**：换字必须**早于**展开结束，两个补间是并行的）。
//   · B2/B3：把 `SetupClosed` 写回「保留 z」（`new Vector3(0, s.y, s.z)`）⇒ B3 红；
//          把 y 也一起压成 0 ⇒ B2 红（两条方向相反、一起钉死）。
//   · C5：把压暗 / 框 / 字三层的 z 任意写反（例如字也写成 `Z + 0.02`）⇒ 那一组红。
//       ⚠️ 只断「其中一层等于某个数」的写法**抓不住**这种反 —— 所以断的是**三段的大小关系**。
//   · C6/C7：把 `m_RaycastPadding` 的下沿内缩删掉（或写成外扩）⇒ 这一对**方向相反**的断言同时红
//       （`-1000` 那一档会被判"吃点击"，`-500` 那一档会被判"不吃"）。
using CardPresentation;
using UnityEngine;

public static class B117_G10_18_Assert
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1018 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1018 ✗ " + msg); }
    }

    static void Num(float got, float want, string msg)
    {
        Check(Mathf.Abs(got - want) <= 1e-3f, msg + "（我们 " + got.ToString("F4") + " · 原版 " + want.ToString("F4") + "）");
    }

    static void Num2(Vector2 got, Vector2 want, string msg)
    {
        Check(Mathf.Abs(got.x - want.x) <= 0.05f && Mathf.Abs(got.y - want.y) <= 0.05f,
              msg + "（我们 (" + got.x.ToString("F2") + ", " + got.y.ToString("F2") + ")"
                  + " · 原版 (" + want.x.ToString("F2") + ", " + want.y.ToString("F2") + ")）");
    }

    /// <summary>一组跑挂了**不许**把后面几组一起拖没（否则报告里只剩一条异常、别的全是「没验」）。</summary>
    static void Group(string name, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, "组【" + name + "】抛异常（这一组后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var root = new GameObject("B117G1018_ProbeRoot");
        GameObject camGo = null;
        try
        {
            camGo = new GameObject("Main Camera (B117G1018)");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.orthographic = true;
            cam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(cam);

            Group("A 时间线（原版 `.c` 逐值）", () => SectionA_时间线());
            Group("B 收起态几何（`localScale = (0, y, 0)`）", () => SectionB_收起态(root.transform));
            Group("C 等待提示（原版 `WaitText`）", () => SectionC_等待提示(root.transform));
        }
        finally
        {
            // ⚠️ 只销毁**自己建的**：根画布是静态件（别处还要用），不销毁。
            if (root != null) Object.DestroyImmediate(root);
            if (camGo != null) Object.DestroyImmediate(camGo);
            Debug.Log("B117G1018 —— 合计 通过 " + _pass + " / 失败 " + _fail);
        }
    }

    // ============================================================ A · 时间线
    static void SectionA_时间线()
    {
        // 原版两个字段的出厂值（ILSpy `TipPrebattle` :408 / :410）
        var probe = new GameObject("B117G1018_TipProbe").AddComponent<TipPrebattle>();
        try
        {
            Num(probe.openBgWidth, 1.8f, "A0 `openBgWidth` 出厂值 = 原版 1.8");
            Num(probe.timeToOpenBg, 0.3f, "A0 `timeToOpenBg` 出厂值 = 原版 0.3");

            float t = 0.3f;                                  // 原版 `timeToOpenBg`
            Num(TipPrebattle.OpenTipTotalSecondsFor(t), 0.45f,
                "A1 `OpenTip` 全程 = 0.3×0.5 + 0.3 = 0.45 s（两段 WaitForSeconds 之和；补间不额外加时间）");
            Num(TipPrebattle.CloseTipTotalSecondsFor(t), 0.15f,
                "A2 `CloseTip` 全程 = 0.3×0.5 = 0.15 s（`.c` 里只有一条 WaitForSeconds）");
            Num(TipPrebattle.DeactivateWaitSecondsFor(t), 0.3f,
                "A3 `DeactivateTip` 等 = timeToOpenBg（原版 +0x34）= 0.3 s，**不是**一半");

            // 🔴 灭自证：把「改前那三个错值」也钉死 —— 只断「等于对的值」时，
            //    一个把 0.45 改回 1.05 的实现会被 A1 抓住；一个「两边一起改」的写法（把常量与期望一起改）
            //    会被下面这三条抓住（改前的值是**另一种写法**算出来的，不会跟着一起变）。
            Check(Mathf.Abs(TipPrebattle.OpenTipTotalSecondsFor(t) - 1.05f) > 1e-3f,
                  "A5a `OpenTip` ≠ 1.05 s（那是**串行**写法：0.3 展开 + 0.15 + 0.3 淡入 + 0.3）");
            Check(Mathf.Abs(TipPrebattle.CloseTipTotalSecondsFor(t) - 0.30f) > 1e-3f,
                  "A5b `CloseTip` ≠ 0.30 s（那是「等压扁完 + 再等一次 half」的写法）");
            Check(Mathf.Abs(TipPrebattle.DeactivateWaitSecondsFor(t) - 0.15f) > 1e-3f,
                  "A5c `DeactivateTip` 的等待 ≠ 0.15 s（那是 `timeToOpenBg × 0.5` 的写法）");

            // 结构判据（并行）：换字必须**早于**底板展开结束。
            //   串行写法里换字发生在「展开结束**之后**」⇒ 这条必红。
            float change = probe.OpenTipTextChangeSeconds;
            Num(change, 0.15f, "A6a `OpenTip` 换字时刻 = timeToOpenBg×0.5 = 0.15 s");
            Check(change < probe.timeToOpenBg,
                  "A6b 换字时刻(" + change.ToString("F3") + ") **早于** 底板展开结束(" + probe.timeToOpenBg.ToString("F3")
                  + ") ⇒ 两个补间是**并行**的（串行写法必然更晚）");
        }
        finally { Object.DestroyImmediate(probe.gameObject); }
    }

    // ============================================================ B · 收起态几何
    static void SectionB_收起态(Transform root)
    {
        var tip = TipPrebattle.Create(root, 100f, new Vector3(0f, 300f, 0f), new Vector2(0.5f, 0.5f),
                                      22f, "B117G1018_Tip");
        Check(tip != null, "B0 `TipPrebattle.Create` 建得出来（建不出来 = 这一组后面全是「没验」）");
        if (tip == null) return;

        var bg = tip.GetComponentInChildren<ImageQuad>(true);
        var txt = tip.GetComponentInChildren<Label>(true);
        Check(bg != null && txt != null, "B1 底板(`ImageQuad`)与文字(`Label`)都在（bg=" + (bg != null) + " text=" + (txt != null) + "）");

        if (bg != null)
        {
            var s = bg.transform.localScale;
            // 原版 `TipPrebattle__SetupClosed.c:24-27`：`local_18 = y << 32; local_10 = 0;`
            //   ⇒ x = 0 · **y 保留** · z = 0
            Num(s.x, 0f, "B2 收起态 `localScale.x` = 0（原版压扁到 0）");
            Num(s.z, 0f, "B3 收起态 `localScale.z` = 0（原版 `local_10 = 0`；改前我们保留成 1）");
            Check(Mathf.Abs(s.y - 1f) < 1e-3f,
                  "B4 收起态 `localScale.y` **保留原值 1**（原版只把 x 压扁、y 不动）—— 与 B3 一起钉死「只改 x 和 z」");
        }
        if (txt != null)
            Num(txt.color.a, 0f, "B5 收起态文字 α = 0（原版 `SupportMethods.SetTextAlpha(tipText, 0)`）");
    }

    // ============================================================ C · 等待提示
    static void SectionC_等待提示(Transform root)
    {
        var host = HudCanvas.Ensure(LayoutSpace.Cam);
        Check(host != null, "C0 根画布建得出来（`HudCanvas.Ensure`）");
        if (host == null) return;

        // ⚠️ 用**白图**建（`CreateWithArt`）—— 本组验的是几何/层级/吃点击，不是「美术取没取到」
        //   （美术那一条由 `Editor/BattleScene.cs` 那几条断言盯）。
        var bar = WaitBanner.CreateWithArt(root, Texture2D.whiteTexture, Texture2D.whiteTexture, host);
        Check(bar != null, "C1 等待提示建得出来");
        if (bar == null) return;

        try
        {
            // ---- 出厂是收起的（原版 `WaitText` / `Dark Shade` 在 dump 里 activeSelf=False）----
            Check(!bar.Visible, "C2 出厂 `Visible` = false（原版 dump 里 `WaitText` 的 activeSelf 就是 False）");

            // ---- 几何（原版实读，见 `WaitBanner` 文件头那张子件表）----
            Num2(bar.RootSizePx, new Vector2(1344f, 79.44f), "C3 根（原版 `WaitText`）= 1344 × 79.44 px");
            Num2(bar.FrameSizePx, new Vector2(1323f, 90f), "C4 框（原版 `Generic Popup Background`）= 1323 × 90 px");
            Num2(bar.BarCenterPx, new Vector2(967f, 175.1f),
                 "C5 条心 = `anchor(0.5,1)` + `ap(7.0, −175.1)` ⇒ (960+7, 175.1)");
            Check(bar.FrameIsSliced, "C6 框是 `Image.Type.Sliced`（原版 `m_Type = 1`）");
            Check(bar.FillIsTiled, "C7 填充是 `Image.Type.Tiled`（原版 `m_Type = 2`）");
            Num(bar.FillPpuMulValue, 2f, "C8 填充 `pixelsPerUnitMultiplier` = 2.0（原版；格宽 = 128÷2 = 64 px）");
            Num2(bar.ShadeSizePx, new Vector2(3963.5f, 3366f), "C9 压暗层（原版 `Dark Shade`）= 3963.5 × 3366 px");
            Check(!bar.ShadeHasSprite, "C10 压暗层**无图、纯色**（原版 `m_Sprite = null`）");

            // ---- 层级（🔴 灭自证：断三段的大小关系，不断单值）----
            //   `HudCanvas.Add` 的 z **大 ⇒ 画在底下** ⇒ 压暗 > 框 > 字。
            //   任意两层写反（例如把字也写成 `Z + 0.02`）⇒ 这一条红。
            float zs = bar.ShadeOrderZ, zp = bar.PopupOrderZ, zt = bar.TextOrderZ;
            Check(zs > zp && zp > zt,
                  "C11 三层兄弟序 压暗(" + zs.ToString("F3") + ") > 框(" + zp.ToString("F3") + ") > 字("
                  + zt.ToString("F3") + ")（z 大 = 画在底下 ⇒ 压暗压在最下、字在最上）");

            // ---- 吃点击（原版 `Dark Shade`：`m_RaycastTarget = 1` + `m_RaycastPadding = (0,1000,0,0)`）----
            bar.SetVisible(true);
            Check(bar.ShadeVisible && bar.TextVisible,
                  "C12 `SetVisible(true)` 三层一起亮（⛔ 不许出现「半截挂着一块压暗」）");
            Check(bar.ShadeRaycastTarget, "C13 压暗层 `raycastTarget = true`（原版 `m_RaycastTarget = 1`）");
            var pad = bar.ShadeRaycastPaddingPx;
            Check(Mathf.Abs(pad.x) < 1e-3f && Mathf.Abs(pad.y - 1000f) < 1e-3f
                  && Mathf.Abs(pad.z) < 1e-3f && Mathf.Abs(pad.w) < 1e-3f,
                  "C14 压暗层 `raycastPadding = (0, 1000, 0, 0)`（uGUI 顺序 X左/Y下/Z右/W上；下沿内缩 1000 px）"
                  + "（我们 (" + pad.x + ", " + pad.y + ", " + pad.z + ", " + pad.w + ")）");

            // 几何判定：条心要吃、屏底手牌那一带**不吃**（下沿从 −1683 抬到 −683）
            var shadeRt = bar.ShadeRect;
            if (shadeRt != null)
            {
                Vector3 centre = shadeRt.TransformPoint(Vector3.zero);
                Vector3 lowArea = shadeRt.TransformPoint(new Vector3(0f, -1000f, 0f));
                Vector3 upArea = shadeRt.TransformPoint(new Vector3(0f, 800f, 0f));
                Check(bar.ShadeContains(centre), "C15 压暗层中心**吃**输入（原版语义：挡上中部）");
                Check(!bar.ShadeContains(lowArea),
                      "C16 压暗层**下沿之外不吃**输入（局部 y=−1000 < −1683+1000=−683）——"
                      + "原版是「手牌那一带照旧能点」");
                Check(bar.ShadeContains(upArea), "C17 压暗层上中部**吃**输入（局部 y=+800）");
            }
            else Check(false, "C15~C17 拿不到压暗层的 `RectTransform`（判据读口变了）⇒ 这 3 条**没验**");

            // ---- 词条（原版 `Localize.mTerm`）----
            Check(WaitBanner.WaitTerm == "Battle/Mulligan/WaitEnemy",
                  "C18 提示条词条键 = 原版 `mTerm` `Battle/Mulligan/WaitEnemy`");
            Num(WaitBanner.WaitTextBoxW, 1270f, "C19 文字框宽 = 原版 1270 px");
            Num(WaitBanner.WaitTextBoxH, 70f, "C20 文字框高 = 原版 70 px");
            Num(WaitBanner.WaitTextFsMin, 18f, "C21 字号下限 = 原版 `m_fontSizeMin` 18");
            Num(WaitBanner.WaitTextFsMax, 55.3f, "C22 字号上限 = 原版 `m_fontSizeMax` 55.3");
            Num(WaitBanner.WaitTextFsBase, 36f, "C23 自适应起点 = 原版 `m_fontSizeBase` 36");
        }
        finally
        {
            // 🔴 **画布档下三层挂在【根画布】上、不在本组件节点下** ⇒ 只销毁 `bar.gameObject` 会
            //    把 `wait_shade` / `wait_popup` / `wait_fillRoot` / `wait_text` **留在静态根画布上**
            //    （下一次断言就多出一整屏 α0.6118 的黑罩 —— 本工程的「画面全绿、实际盖死」那类坑）。
            //    ⇒ 按**本件自己的命名前缀**逐个清掉（只删 `wait_*`，⛔ 别碰别人的件）。
            if (host != null)
            {
                var kids = new System.Collections.Generic.List<GameObject>();
                foreach (Transform c in host) if (c != null && c.name.StartsWith("wait_")) kids.Add(c.gameObject);
                for (int i = 0; i < kids.Count; i++) if (kids[i] != null) Object.DestroyImmediate(kids[i]);
            }
            if (bar != null) Object.DestroyImmediate(bar.gameObject);
        }
    }
}
