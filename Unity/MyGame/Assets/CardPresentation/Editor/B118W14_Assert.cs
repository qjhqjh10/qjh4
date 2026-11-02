// B118W14_Assert.cs — 执行代理 `B118-W14`（卡牌表示层四修）的断言宿主 · 2026-11-02
//
// 覆盖本代理改的四件事（判据全文 → `资料/完全复刻/判据/B118_J6_卡牌表示层矩阵.md`）：
//   A · 关键词粒子（`TraitParticles.PlayCore`）**要登记到所属那张卡的排序段**（§5-②）
//   B · 回手倒放要**整张 2D 卡一起淡**（§4-③⑤），⛔ 不是只淡立绘
//   C · 回手/换形态的两档 2D α 必须**有复位**（§4-④）
//   D · 「白」源的兜底：缺件 ⇒ 出声一次 + 占位色（§3）
//   E · 源码级灭自证（防止「两边一起改回去」）
//
// 怎么跑（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B118W14_Assert.Run -logFile -
//
// 判绿红：看 `B118W14 合计` 那一行；逐条按**行首** `B118W14 ✓` / `B118W14 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主要求：`Run()` **零前提** —— 不读 `BattleDriver` / 场景 / 存档；自带临时根 + 收尾 `DestroyImmediate`；
//   自己把 `CardTween.Mode` 拨成 `Manual`、`LinkEnabled` 拨成 `false`，退出原样还原（批处理没有帧循环）。
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using UnityEngine;
using CardPresentation;

public static class B118W14_Assert
{
    const string Tag = "B118W14";
    static int _pass, _fail;

    /// <summary>给宿主聚合行用的读口（否则失败不会让宿主变红 = 静默失败）。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
                  + "；实得 " + got.ToString("F4") + "）");
    }

    static void Step(float dt) { CardTween.Advance(dt); }

    /// <summary>抓「某条告警出现了几次」—— 按 `<paramref name="needle"/>` 过滤，别人的告警不进来。</summary>
    sealed class LogSpy : System.IDisposable
    {
        readonly List<string> _msgs = new List<string>();
        readonly Application.LogCallback _h;
        public LogSpy(string needle)
        {
            _h = (msg, stack, type) =>
            {
                if (type == LogType.Warning && msg != null && msg.Contains(needle)) _msgs.Add(msg);
            };
            Application.logMessageReceived += _h;
        }
        public int Count { get { return _msgs.Count; } }
        public void Dispose() { Application.logMessageReceived -= _h; }
    }

    /// <summary>按 `GameObject` 名找某层的渲染器（`null` = 这层没建）。</summary>
    static MeshRenderer Layer(CardView v, string name)
    {
        if (v == null) return null;
        foreach (var r in v.GetComponentsInChildren<MeshRenderer>(true))
            if (r != null && r.gameObject.name == name) return r;
        return null;
    }

    static float LayerAlpha(CardView v, string name)
    {
        var r = Layer(v, name);
        var m = r != null ? r.sharedMaterial : null;
        return (m != null && m.HasProperty("_Color")) ? m.color.a : -1f;
    }

    static bool LayerActive(CardView v, string name)
    {
        var r = Layer(v, name);
        return r != null && r.gameObject.activeSelf;
    }

    static CardView MakeCard(Transform root, string name, CardFace face)
    {
        var d = CardData.Simple(name, 3, 2, 4);
        d.faction = "Ultramarines";   // ⚠️ 不给阵营 ⇒ 卡框/3D 体都建不出来（`CardArt.Frame` 返 null）
        d.rarity = "common";
        return CardView.Create(root, d, name, face);
    }

    /// <summary>走**兜底那一支**的回手（`ForceNoDissolveForTest`）—— 两支的时间轴逐刻相同，取兜底是为了不依赖溶解 shader。</summary>
    static DG.Tweening.Tween StartReturn(CardView card)
    {
        bool keep = CardFeel.ForceNoDissolveForTest;
        CardFeel.ForceNoDissolveForTest = true;
        var tw = CardFeel.ReturnToHand(card, 0f, null);
        CardFeel.ForceNoDissolveForTest = keep;
        return tw;
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        var root = new GameObject("B118W14_probe");
        root.transform.position = new Vector3(0f, 120f, 0f);
        try
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            CardTween.LinkEnabled = false;

            Group("A·关键词粒子登记排序段", () => A_TraitParticleStamp(root.transform));
            Group("B·回手倒放整张 2D 卡一起淡", () => B_ReturnToHandGroupFade(root.transform));
            Group("C·两档 2D α 的复位", () => C_AlphaReset(root.transform));
            Group("D·「白」源兜底", () => D_WhiteSources(root.transform));
            Group("E·源码级灭自证", E_SourceGuards);
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            CardTween.Mode = keepMode;
            CardTween.LinkEnabled = keepLink;
            CardView.ResetMissingDiagnosticsForTest();
            ImageQuad.ResetMissingDiagnosticsForTest();
            CardArt.ResetMissingDiagnosticsForTest();
            Object.DestroyImmediate(root);
        }
        Debug.Log(Tag + " 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ==================================================================
    //  A · 关键词粒子：**登记到所属那张卡的排序段**
    // ==================================================================
    //  缺陷（判据 §5-②）：`TraitParticles.PlayCore` 原来直接 `WarpforgeEffectPlayer.Play(...)`、
    //  **从不登记** ⇒ 实例停在默认队列 3000；场上卡整段 2962–2979 ⇒ 粒子恒画在所有场上卡之上。
    //  本组直接打**那个登记口**（`TraitParticles.StampToOwnerCard`，`PlayCore` 唯一调用的就是它），
    //  ⛔ 不去调 `WarpforgeEffectPlayer.Play`（那要真效果库，且与本条的判据无关）。
    static void A_TraitParticleStamp(Transform root)
    {
        var card = MakeCard(root, "B118W14_board", CardFace.Board);
        Check(card != null, "A0 ★ 前提：场上档探针卡建出来了");
        if (card == null) return;

        card.SetQueue(2970);
        Check(card.Queue == 2970, "A0b ★ 前提：`SetQueue(2970)` 生效（下面要读这一段的号）");
        Check(card.Queue >= 2962 && card.Queue <= 2979,
              "A0c 前提：探针卡的号 2970 落在场上那一段 2962–2979 里（`BoardLayout.QBoardTop/Floor`）");

        // 造一颗「和特效实例同形」的根：一粒 renderer + 队列 3000 的透明材质（= prefab 默认）
        var fxGo = new GameObject("B118W14_fx");
        fxGo.transform.SetParent(card.EffectAnchorBoard, false);
        var mr = fxGo.AddComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.renderQueue = 3000;
        mr.sharedMaterial = mat;

        bool ok = TraitParticles.StampToOwnerCard(card.transform, fxGo.transform);
        Check(ok, "A1 登记成功（`StampToOwnerCard` 从卡根向上找到了 `CardView`）");
        Check(mr.sharedMaterial.renderQueue == card.Queue,
              "A2 ★ 粒子的 `renderQueue` = 这张卡那一段（" + card.Queue + "）"
            + "｜实得 " + mr.sharedMaterial.renderQueue + "｜🧨 删掉 `PlayCore` 里那句 `StampToOwnerCard` ⇒ 停在 3000 ⇒ 红");
        Check(mr.sharedMaterial.renderQueue != CardView.QCard,
              "A2b ★ 不再等于 3000（3000 的透明件**恒画在所有场上卡之上** —— 那就是用户报的「粒子穿过前排卡」）"
            + "｜实得 " + mr.sharedMaterial.renderQueue);
        Check(mr.sortingOrder == CardView.EffectSortingOrder,
              "A2c `sortingOrder` 抬到 `EffectSortingOrder`(" + CardView.EffectSortingOrder + ") + prefab 原值(0)"
            + "｜实得 " + mr.sortingOrder);
        Check(card.EffectRootCount == 1, "A3 登记的根进了这张卡的清单（`EffectRootCount` = 1）｜实得 " + card.EffectRootCount);

        // A4：**卡换段时粒子跟着换** —— `CardView.SetQueue` 会遍历 `_effectRoots` 重盖
        card.SetQueue(2965);
        Check(mr.sharedMaterial.renderQueue == 2965,
              "A4 ★ 卡换段（2970 → 2965）后粒子**跟着换**｜实得 " + mr.sharedMaterial.renderQueue
            + "｜🧨 把 `SetQueue` 里那段 `_effectRoots` 循环删掉 ⇒ 红");

        // A5/A6：挂点不在任何卡下 ⇒ 保持原行为（不盖章）+ **出声一次**（同名只报一次）
        var orphan = new GameObject("B118W14_orphan");
        var fixture = new GameObject("B118W14_fx_orphan");
        fixture.transform.SetParent(orphan.transform, false);
        int before = 0, after = 0;
        using (var spy = new LogSpy("不在任何 `CardView` 下"))
        {
            bool ok2 = TraitParticles.StampToOwnerCard(orphan.transform, fixture.transform);
            before = spy.Count;
            TraitParticles.StampToOwnerCard(orphan.transform, fixture.transform);   // 第二次：同一挂点名
            after = spy.Count;
            Check(!ok2, "A5 挂点不在任何卡下 ⇒ 返回 false（保持原行为：不盖章 —— 世界/棋盘特效是另一笔账）");
            Check(before == 1, "A5b ★ 那种情况**出声一次**（⛔ 不静默）｜实得 " + before + " 条");
            Check(after == 1, "A5c 同一个缺件**只报一次**（再来一次不再刷）｜实得 " + after + " 条");
        }
        Object.DestroyImmediate(orphan);
    }

    // ==================================================================
    //  B · 回手倒放：**整张 2D 卡一起淡**（原版 `2DCard` = 一个 CanvasGroup）
    // ==================================================================
    static void B_ReturnToHandGroupFade(Transform root)
    {
        var card = MakeCard(root, "B118W14_hand", CardFace.Hand);
        Check(card != null, "B0 ★ 前提：手牌档探针卡建出来了（`_frame/_title/_keywords` 只有非场上档才建）");
        if (card == null) return;

        var frame = Layer(card, "frame");
        var art = Layer(card, "art");
        Check(frame != null, "B0b ★ 前提：`frame` 层存在（`Build` 在 `CardFace.Board` 那一档**不建**它 —— 见报告「没做」）");
        Check(art != null, "B0c ★ 前提：`art` 层存在");
        if (frame == null || art == null) return;

        card.SetFace(CardFace.Board);      // = 「从手牌打出去」那一步
        Check(!LayerActive(card, "frame"),
              "B1 ★ 前提复现缺陷：切到场上形态后 `frame` 是**关着**的 ⇒ 回手那 0.6 s 里如果只淡立绘，看到的就是一块**无框矩形**");
        Check(!LayerActive(card, "art"), "B1b 前提：场上形态下 `art` 也是关着的（与 3D 体二选一）");

        var tw = StartReturn(card);
        Check(tw != null, "B2 ★ 前提：`CardFeel.ReturnToHand` 建出了补间（`CardTween.Mode = Manual`）");
        if (tw == null) return;

        // 推进到 [0.25, 0.5833] 淡入区间的中段
        Step(0.45f);
        float a2d = card.Alpha2D;
        Check(a2d > 0.15f && a2d < 0.9f, "B3 ★ 中点取样落在淡入区间里（0 < α2D < 1）｜实得 " + a2d.ToString("F3"));
        Check(LayerActive(card, "frame"),
              "B4 ★★ 中点时 **`frame` 是开着的**（= 原版 `2DCard.m_IsActive` 0→1）"
            + "｜🧨 退回「只 `SetArtAlpha`」⇒ 红（那正是用户看到的无框矩形）");
        Check(LayerActive(card, "art"), "B4b 中点时 `art` 开着（整组一起）");

        float fMid = LayerAlpha(card, "frame"), rMid = LayerAlpha(card, "art");
        float a2dMid = a2d;

        // 跑到终点（0.9167）
        Step(0.6f);
        float fEnd = LayerAlpha(card, "frame"), rEnd = LayerAlpha(card, "art");
        CheckClose(card.Alpha2D, 1f, 1e-3f, "B5 ★ 终态 `Alpha2D` = 1（2D 那一档整组回满）");
        CheckClose(fEnd, 1f, 1e-3f, "B5b 终态卡框 α = 1");
        CheckClose(rEnd, 1f, 1e-3f, "B5c 终态立绘 α = 1");

        // 同步性：各层的「中点/终点」比 = α2D 的比（⛔ 不是几个固定数字 —— 各层底色 α 不同）
        Check(fEnd > 0.5f && rEnd > 0.5f, "B6 ★ 前提：终态两层都不透明（否则下面两条除法没意义）");
        CheckClose(fMid / Mathf.Max(fEnd, 1e-3f), a2dMid, 3e-3f,
                   "B6 ★★ 卡框与 2D 组**同步淡**（框的中点/终点比 = α2D）");
        CheckClose(rMid / Mathf.Max(rEnd, 1e-3f), a2dMid, 3e-3f,
                   "B6b ★★ 立绘与 2D 组**同步淡**（原来只淡它一个 ⇒ 与框/文字脱节）"
                 + "｜🧨 把 `ArtOpaque.shader` 的 `c.a = i.color.a` 改回 `c.a = 1` ⇒ 立绘的 α 根本没上屏 ⇒ 红");

        tw.Kill();
    }

    // ==================================================================
    //  C · 两档 2D α（`_artAlpha` / `_alpha2D`）必须有复位
    // ==================================================================
    static void C_AlphaReset(Transform root)
    {
        // C1：**真切换形态**时两档 α 回 1（🧨 去掉 `ResetFaceAlphaOnSwitch` ⇒ 红）
        var a = MakeCard(root, "B118W14_reset", CardFace.Hand);
        Check(a != null, "C0 ★ 前提：探针卡建出来了");
        if (a == null) return;
        a.SetFace(CardFace.Board);
        a.SetAlpha2D(0.35f);
        a.SetArtAlpha(0.30f);
        Check(Mathf.Abs(a.Alpha2D - 0.35f) < 1e-3f && Mathf.Abs(a.ArtAlpha - 0.30f) < 1e-3f,
              "C1 前提：两档 α 被弄脏了（模拟「补间被打断」的残留）");
        a.SetFace(CardFace.Hand);          // ← 真切换
        CheckClose(a.Alpha2D, 1f, 1e-4f,
                   "C2 ★★ 换形态后 `Alpha2D` 回初值 1（🧨 删掉 `SetFace` 里那句 `ResetFaceAlphaOnSwitch()` ⇒ 停在 0.35 ⇒ 红）");
        CheckClose(a.ArtAlpha, 1f, 1e-4f,
                   "C2b ★★ 换形态后 `ArtAlpha` 回初值 1（原来**没有任何地方**复位它）");
        Object.DestroyImmediate(a.gameObject);

        // C3：**同形态重调不打断正在播的淡入**（复位点必须排在 `if (_faceMode == f) return;` 之后）
        var b = MakeCard(root, "B118W14_sameface", CardFace.Hand);
        if (b == null) { Check(false, "C3 ★ 前提：第二张探针卡建不出来"); return; }
        b.SetFace(CardFace.Board);
        var tw = StartReturn(b);
        if (tw == null) { Check(false, "C3 ★ 前提：回手补间建不出来"); Object.DestroyImmediate(b.gameObject); return; }
        Step(0.45f);
        float mid = b.Alpha2D;
        Check(mid > 0.15f && mid < 0.9f, "C3 ★ 前提：已推进到淡入中段｜实得 " + mid.ToString("F3"));
        b.SetFace(CardFace.Board);         // 同形态：`ReturnToHand` 全程 Face == Board，这条链会真的发生
        CheckClose(b.Alpha2D, mid, 1e-3f,
                   "C4 ★★ 同形态重调 `SetFace` **不打断**正在播的淡入（α 仍在中点）"
                 + "｜🧨 把复位挪到 `if (_faceMode == f) return;` **之前** ⇒ 这里被拍回 1 ⇒ 红");
        Object.DestroyImmediate(b.gameObject);

        // C5：**倒放到一半 kill** ⇒ 两档 α 都必须复位（🧨 去掉 `OnKill` 里那两句 ⇒ 红）
        var c = MakeCard(root, "B118W14_kill", CardFace.Hand);
        if (c == null) { Check(false, "C5 ★ 前提：第三张探针卡建不出来"); return; }
        c.SetFace(CardFace.Board);
        var tw2 = StartReturn(c);
        if (tw2 == null) { Check(false, "C5 ★ 前提：回手补间建不出来"); Object.DestroyImmediate(c.gameObject); return; }
        Step(0.45f);
        Check(c.Alpha2D < 0.95f, "C5 ★ 前提：kill 之前淡入确实在跑｜α2D = " + c.Alpha2D.ToString("F3"));
        tw2.Kill();
        CheckClose(c.Alpha2D, 1f, 1e-4f,
                   "C6 ★★ 倒放被打断后 `Alpha2D` 复位到 1（否则这张视图**永久偏淡**、零报错）"
                 + "｜🧨 删掉 `ReturnToHand` 的 `OnKill` 里那句 `SetAlpha2D(1f)` ⇒ 红");
        CheckClose(c.ArtAlpha, 1f, 1e-4f,
                   "C6b ★★ 倒放被打断后 `ArtAlpha` 复位到 1｜🧨 删掉 `OnKill` 里那句 `SetArtAlpha(1f)` ⇒ 红");
        Check(!LayerActive(c, "frame"),
              "C6c 被打断时形态仍是场上（`Face == Board`）⇒ 2D 那几层**收起**（别让场上卡突然多出一张 2D 卡面）");
        Object.DestroyImmediate(c.gameObject);
    }

    // ==================================================================
    //  D · 「白」源兜底（判据 §3 那张表）
    // ==================================================================
    static void D_WhiteSources(Transform root)
    {
        CardView.ResetMissingDiagnosticsForTest();
        ImageQuad.ResetMissingDiagnosticsForTest();
        CardArt.ResetMissingDiagnosticsForTest();

        // D1：`_MatCap` 缺 ⇒ 占位中性灰（0.5917 → pow(0.5917×1.69,1.24) ≈ 1），⛔ 不是白
        using (var spy = new LogSpy("3D 卡体的贴图 `_MatCap`"))
        {
            var cap = CardView.Card3DTex("_MatCap", null);
            int n1 = spy.Count;
            var cap2 = CardView.Card3DTex("_MatCap", null);
            int n2 = spy.Count;
            Check(cap != null, "D1 ★ 缺 `_MatCap` 时有占位图（原来是 shader 的 `\"white\"` 默认值）");
            Check(cap != null && cap != Texture2D.whiteTexture, "D1b 占位图**不是**内置白图");
            Check(cap != null && cap.GetPixel(0, 0).r < 0.9f && cap.GetPixel(0, 0).r > 0.4f,
                  "D1c 占位色是**中性灰**（≈0.59 ⇒ `pow(0.59×1.69,1.24)≈1`，不烧白也不压黑）"
                + "｜实得 " + (cap != null ? cap.GetPixel(0, 0).ToString("F3") : "null"));
            Check(n1 == 1 && n2 == 1, "D1d 出声**一次**（第二次取同一个槽不再刷）｜" + n1 + " / " + n2);
            Check(cap2 == cap, "D1e 同一个槽复用同一张占位图（不每帧新建）");
        }
        var real = CardArt.Card3DMatcap(1);
        Check(CardView.Card3DTex("_MatCap", real) == real,
              "D1f `t != null` ⇒ **原样返回**（这一档零改动；`real != null` = " + (real != null) + "）");

        // D2：`_CardImage` 缺 ⇒ 出声 + 明显不是白的占位色（原来是 `whiteTexture` ⇒ 正面纯白）
        using (var spy = new LogSpy("3D 卡体的贴图 `_CardImage`"))
        {
            var img = CardView.Card3DTex("_CardImage", null);
            Check(img != null && img != Texture2D.whiteTexture, "D2 ★ 缺立绘时占位图不是白图");
            Check(img != null && img.GetPixel(0, 0).r < 0.9f, "D2b 占位色不是白（画面上是「明显不对」而不是「一块白」）");
            Check(spy.Count == 1, "D2c 出声一次｜实得 " + spy.Count);
        }

        // D3：`AddLayer` 的 `tex` 与 `basis` **都** null ⇒ 占位（原来是 Unity 绑内置白图 = 不透明白块）
        using (var spy = new LogSpy("卡面层 `B118W14_layer`"))
        {
            var l1 = CardView.MissingLayerTexture("B118W14_layer");
            var l2 = CardView.MissingLayerTexture("B118W14_layer");
            Check(l1 != null && l1 != Texture2D.whiteTexture, "D3 ★ 两参都 null 时有占位图");
            Check(l1 != null && l1.GetPixel(0, 0).r < 0.9f, "D3b 占位色不是白");
            Check(spy.Count == 1 && l1 == l2, "D3c 同一层名只出声一次、且复用同一张｜" + spy.Count);
        }

        // D4：`ImageQuad.SetTexture(null)` 运行期那一口
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.SetPixels(new Color[16]); tex.Apply();
        var host = new GameObject("B118W14_quad");
        var q = ImageQuad.Create(host.transform, tex, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "B118W14_quad");
        Check(q != null, "D4 ★ 前提：`ImageQuad.Create` 建出来了");
        if (q != null)
        {
            var prevTex = q.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
            q.SetTexture(null);
            var now = q.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
            Check(now != null && now != Texture2D.whiteTexture,
                  "D4 ★★ 运行期 `SetTexture(null)` **不再**绑 null（Unity 会拿内置白图 = 一块不透明白块）；保留上一张");
            Check(now == prevTex, "D4b 保留的正是**上一张**（壳侧「清空」的调用点都是紧跟 `SetActive(false)`）");
            Check(q.Texture == null, "D4c 逻辑上的 `Texture` 仍如实是 null（读口不撒谎）");
        }
        Object.DestroyImmediate(host);

        // D5：**一张图都没有**的 quad 被 `SetTexture(null)` ⇒ 出声一次 + 占位
        using (var spy = new LogSpy("没有上一张图"))
        {
            var bare = new GameObject("B118W14_bare_quad");
            bare.AddComponent<MeshFilter>();
            var bmr = bare.AddComponent<MeshRenderer>();
            bmr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            var bq = bare.AddComponent<ImageQuad>();
            bq.RebindAfterClone();      // ⚠️ `_mr/_mf` 是运行期字段（`AddComponent` 出来的那份没有）⇒ 先重绑，否则 `SetTexture` 整段空转
            bq.SetTexture(null);
            int n1 = spy.Count;
            bq.SetTexture(null);
            int n2 = spy.Count;
            var bound = bmr.sharedMaterial.mainTexture;
            Check(bound != null && bound != Texture2D.whiteTexture, "D5 ★ 无旧图时绑占位图（不是白）");
            Check(bound != null && ((Texture2D)bound).GetPixel(0, 0).r < 0.9f, "D5b 占位色不是白");
            Check(n1 == 1 && n2 == 1, "D5c 出声一次｜" + n1 + " / " + n2);
            Object.DestroyImmediate(bare);
        }

        // D6：`CardArt.Get` 的**负缓存**要出声（否则「图没导进来」表现成一堆白块，没人知道为什么）
        using (var spy = new LogSpy("负缓存"))
        {
            int c0 = CardArt.MissingCount;
            var miss1 = CardArt.Portrait("b118w14_missing_probe");
            int c1 = CardArt.MissingCount;
            var miss2 = CardArt.Portrait("b118w14_missing_probe");
            int c2 = CardArt.MissingCount;
            Check(miss1 == null && miss2 == null, "D6 ★ 前提：那条路径确实取不到（否则本组没意义）");
            Check(c1 == c0 + 1, "D6 ★★ 缺件被记账（`MissingCount` +1）｜" + c0 + " → " + c1);
            Check(c2 == c1, "D6b 同一个路径**只记一次**（负缓存不重复刷）｜" + c2);
            Check(spy.Count == 1, "D6c 出声一次｜实得 " + spy.Count);
        }
    }

    // ==================================================================
    //  E · 源码级灭自证（防「两边一起改回去」）
    // ==================================================================
    static string ReadSrc(string rel)
    {
        try
        {
            var p = Path.Combine(Application.dataPath, rel);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch (System.Exception) { return null; }
    }

    static int Count(string hay, string needle)
    {
        if (hay == null || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = hay.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    static void E_SourceGuards()
    {
        const string ViewPath = "CardPresentation/Core/CardView.cs";
        const string FeelPath = "CardPresentation/Core/CardFeel.cs";
        const string TraitPath = "CardPresentation/Core/TraitParticles.cs";
        const string QuadPath = "CardPresentation/Battle/ImageQuad.cs";
        const string ShaderPath = "CardPresentation/Shaders/ArtOpaque.shader";

        var view = ReadSrc(ViewPath);
        var feel = ReadSrc(FeelPath);
        var trait = ReadSrc(TraitPath);
        var quad = ReadSrc(QuadPath);
        var shader = ReadSrc(ShaderPath);

        Check(view != null && feel != null && trait != null && quad != null && shader != null,
              "E0 ★ 前提：五份源码都读得到（读不到 ⇒ 下面几条会一起红，不是空转绿）");
        if (view == null || feel == null || trait == null || quad == null || shader == null) return;

        Check(Count(trait, "StampToOwnerCard(at, p != null ? p.transform : null)") == 1,
              "E1 ★ `TraitParticles.PlayCore` 真的调了登记口（删掉 ⇒ 粒子回到队列 3000）");
        Check(Count(view, "MissingLayerTexture(name)") == 1,
              "E2 ★ `AddLayer` 的两参 null 兜底还接着（⛔ 别退回「Unity 绑内置白图」）");
        Check(Count(view, "Card3DTex(\"_CardImage\", artTex)") == 1,
              "E3 ★ `_CardImage` 走 `Card3DTex`（⛔ 别再退回 `Texture2D.whiteTexture`）");
        Check(Count(view, "Card3DTex(\"_MatCap\"") == 1, "E3b `_MatCap` 走 `Card3DTex`");
        Check(Count(view, "ResetFaceAlphaOnSwitch()") == 2,
              "E4 ★ `SetFace` 里那句复位还在（1 处调用 + 1 处定义 = 2）｜实得 " + Count(view, "ResetFaceAlphaOnSwitch()"));
        Check(Count(feel, "SetFaceGroupVisible(true)") == 2,
              "E5 ★ `ReturnToHand` 的**两支**（溶解 / 兜底）都把整组打开了｜实得 " + Count(feel, "SetFaceGroupVisible(true)"));
        Check(Count(feel, "SetArtAlpha(0f)") == 1,
              "E5b ⛔ 回手那两支**不再**只淡立绘（`SetArtAlpha(0f)` 全文件应只剩 `UnDissolve` 那一处 = 1；"
            + "回手退回老写法 ⇒ 变 3）｜实得 " + Count(feel, "SetArtAlpha(0f)"));
        Check(Count(shader, "c.a = i.color.a;") == 1 && Count(shader, "c.a = 1;") == 0,
              "E6 ★ `ArtOpaque` 保留**材质/顶点** alpha、只丢**贴图** alpha（改回去 ⇒ 立绘的 α 静默失效）");
        Check(Count(quad, "MissingQuadTexture(gameObject.name)") == 1,
              "E7 ★ `ImageQuad.SetTexture` 运行期那一口有兜底");
    }
}
