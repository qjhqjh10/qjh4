// B141_W41_Assert.cs — 执行写手 `B141-W41`（2026-11-04 · 第一百二十八会话 · 批次 B141）
// 盯的是 §11-08（`报告/B138_审计汇总_问题清单.md` §11 的 11-08 · `判据/B141_W129_对账_UI动画.md` §E-1）：
//   `Board/DeploySequence.cs` 的落位补间原来有 **4 处 `DOMove`（世界系）**，与判据的 `DOLocalMove` 不同形 ⇒
//   本批统一改成「世界点 →（父级局部系）→ `DOLocalMove`」（`:204` · `:218` · `:282` · `:301`；
//   其中 **`:282` 才是生产路径** —— `CardInteraction.cs:1481` 经 `Play` 走到，`PlayQuad` 只给 AI/探针）。
//
// ============================ 判据（一手 · 逐句亲读）============================
//   · `decomp_full/MinionManager__MoveMinionToConversionPoint.c`（起飞 0.3）：
//       `param_4 == '\0'` 那条正常分支里是 `DG_Tweening_ShortcutExtensions__DOLocalMove(...)`
//       —— 目标 `local_68` = `GetLeft/RightSlotPos` + `up × minionConversionHeight`（都是**父级局部量**）。
//   · `decomp_full/CardScript__UpdateMinionInPlayPosition.c:89`（落下 0.2）：同样是 `DOLocalMove`。
//   ⇒ 原版两条都是**局部空间**补间。我们的 `abovePos/landPos` 是**世界坐标**（见下），所以要换算。
//
// ============================ 坐标帧判定（R8 留的「世界 vs 竞技场局部」问号，本批结）============================
//   `abovePos / landPos` = **世界坐标**（HUD 平面 z = 0），唯一生产者 = `BoardLayout.DropTargetWorld[AtHeight]`：
//     · `Board/BoardLayout.cs:515-525` `DropTargetWorld` —— doc 与实现都明写「返回**世界坐标**」；
//     · `Board/BoardLayout.cs:621-634` `DropTargetWorldAtHeight` —— 同空间（「与 `DropTargetWorld` 同一个空间」）；
//     · `BattleDriver.cs:25847-25849` `aboveWorld = aboveLocal`（注释：「返回值**已是世界点**」）。
//   ⇒ 要喂给 `DOLocalMove` 必须**先换算成父级局部系**（`LocalMoveToWorld` 做这一跳）。
//
// ============================ ⚠️ 诚实记一条（铁律 5）============================
//   对**静态仿射父级**（任意平移/旋转/非均匀缩放），`DOMove(world)` 与「世界点换算成父级局部后 `DOLocalMove`」
//   的世界轨迹**数学上逐点相同**（仿射映射与凸组合可交换）⇒ 本改动**不改变今天的画面**（本仓父级当前恒等）；
//   它是「与判据同一调用形状」的保真修复。**唯一会拉开差别的情形 = 补间途中父级自己动了**
//   （`DOLocalMove` 跟着父级走、`DOMove` 不跟）—— 本文件的 B 段就是拿这一条做灭自证。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · A1/A3 ← 把 `LocalMoveToWorld` 写成 `DOLocalMove(abovePos)`（**漏换算**）⇒ 非恒等父级下端点落到
//        `parent.TransformPoint(abovePos)` 而不是 `abovePos` ⇒ A1/A3 端点断言红（A1/A3 里还有一条显式「不是错点」）。
//   · A2/A4 ← 落下段同病（`:218/:301`）。
//   · B ← 把 `LocalMoveToWorld` 换回 `tr.DOMove(...)` ⇒ 补间途中被移动的父级**带不动**这张卡
//        ⇒ `dNoRide ≈ 0`（卡停在旧世界直线上）⇒ B 的第二条红。**这一条结构上不可能被「DOMove 实现」满足。**
//   · C ← 生产路径（带交接的 `Play`）若没走 `:282`/`:293`，交接计数或落体终点会不对 ⇒ 红。
//   · D ← 任意一处回退成 `tr.DOMove(` ⇒ 源码级计数红。
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//   · **不跑 Unity 帧循环**：编辑模式下把 `CardTween.Mode` 拨到 `Manual`、自己 `CardTween.Advance` 推补间
//     （退出时**原样还原** `Mode` / `LinkEnabled`）。
//   · **只验「坐标系/端点/是否跟父级」，不验手感**（那是真 Play 的事，见 `资料/真Play待验清单.md`）。
//   · 不验 `PlayQuad` 的 AI 分支时长耦合（另在 `Editor/BattleScene.cs` 等处）。
using System;
using DG.Tweening;
using UnityEngine;
using CardPresentation;

/// <summary>`B141-W41` 的断言集：`Board/DeploySequence.cs` 的 11-08（4 处 `DOMove → DOLocalMove`）。</summary>
public static class B141_W41_Assert
{
    const string P = "B141_W41 ";

    static int _pass, _fail;
    /// <summary>通过的条数（宿主聚合行用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败的条数（宿主聚合行用；⚠️ 不接进聚合行 = 失败不会让宿主变红 = 静默失败）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    /// <summary>只记录、不计分（前提读数）。</summary>
    static void Tr(string msg) { Debug.Log(P + "· " + msg); }

    /// <summary>跑一组并把异常收成一条失败（⛔ 不让一组炸掉后面各组）。</summary>
    static void Group(string tag, Action body)
    {
        try { body(); }
        catch (Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    /// <summary>DOTween 的 `Ease.OutQuad` = `t*(2-t)` = `1-(1-t)²`（原版两条补间没 `SetEase` ⇒ 默认 6）。</summary>
    static float OutQuad(float u) { float v = 1f - Mathf.Clamp01(u); return 1f - v * v; }

    /// <summary>非恒等父级：位置 / 旋转 / **非均匀缩放**都不平凡 —— 只有这样才能抓「漏换算」。</summary>
    static GameObject MakeParent(Vector3 pos, Vector3 euler, Vector3 scale)
    {
        var go = new GameObject("B141W41_Parent");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        return go;
    }

    static void AdvanceBy(Tween seq, float seconds, int steps)
    {
        if (seq == null) return;
        float s = seconds / Mathf.Max(1, steps);
        for (int i = 0; i < steps; i++) CardTween.Advance(s);
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        try
        {
            CardTween.Mode = UpdateType.Manual;   // 批处理没有帧循环 ⇒ 补间不许自己跑
            CardTween.LinkEnabled = false;        // 夹具会被 DestroyImmediate ⇒ 别让 tween 绑在它身上（同 B120_W24）

            // ⚠️ 顺序有意：A/B/C 都要 `CardTween.Advance` 推补间；D 是纯源码级、最后跑。
            Group("A·坐标帧 = 世界（4 处端点都落到世界目标）", A_FrameWorld);
            Group("B·灭自证（父级中途移动 ⇒ 卡跟父级走，DOMove 会被抓）", B_ParentMoveDiscriminates);
            Group("C·生产路径 :282 覆盖（带交接的 Play）", C_ProductionPath);
            Group("D·源码级：4 处已无 tr.DOMove（防回退）", D_NoDomoveRemains);
        }
        catch (Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            CardTween.Mode = keepMode;
            CardTween.LinkEnabled = keepLink;
        }
        Debug.Log(P + "B141_W41 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ==================================================================
    //  A · 坐标帧 = 世界：4 处（PlayQuad :204/:218 · Play :282/:301）的端点都落在**世界目标**
    //      —— 若把 helper 写成「DOLocalMove(world) 漏换算」，非恒等父级下端点会落到错点。
    // ==================================================================
    static void A_FrameWorld()
    {
        const float tol = 3e-2f;
        Vector3 pPos = new Vector3(0.5f, -0.3f, 0f);
        Vector3 pEuler = new Vector3(0f, 0f, 25f);
        Vector3 pScale = new Vector3(1.6f, 0.7f, 1f);
        Vector3 abovePos = new Vector3(2.0f, 1.2f, 0f);
        Vector3 landPos = new Vector3(2.0f, -1.0f, 0f);
        float slotScale = 0.36f;

        // ---- A1 · PlayQuad 起飞（:204）带交接 ----
        {
            var parent = MakeParent(pPos, pEuler, pScale);
            var quad = new GameObject("B141W41_quadA1");
            quad.transform.SetParent(parent.transform, false);
            quad.transform.localPosition = Vector3.zero;
            var handoff = new DeploySequence.Handoff { FallTarget = quad.transform, LandLocal = Vector3.zero, Apply = () => { } };
            var seq = DeploySequence.PlayQuad(quad.transform, abovePos, landPos, slotScale, handoff, null);
            Check(seq != null, "A1 前提：PlayQuad（带交接）建出了序列");
            if (seq != null)
            {
                AdvanceBy(seq, DeploySequence.MoveTime + 0.02f, 32);
                float d = Vector3.Distance(quad.transform.position, abovePos);
                Tr($"A1 端点读数：世界距 {d:E3}（法 0）");
                Check(d < tol, $"A1 · :204 起飞终点 = 世界目标 abovePos（差 {d:E3}）");
                Vector3 naive = parent.transform.TransformPoint(abovePos);   // 漏换算会落这里
                float dn = Vector3.Distance(quad.transform.position, naive);
                Check(dn > 0.5f, $"A1 · 灭自证：**不是**「DOLocalMove(world) 漏换算」的错点（距错点 {dn:F3}）"
                               + "｜🧨 把 helper 改成 DOLocalMove(abovePos) ⇒ 端点落到错点 ⇒ 红");
                seq.Kill(false);
            }
            UnityEngine.Object.DestroyImmediate(quad);
            UnityEngine.Object.DestroyImmediate(parent);
        }

        // ---- A2 · PlayQuad 落下（:218）无交接 = 平面直落 ----
        {
            var parent = MakeParent(pPos, pEuler, pScale);
            var quad = new GameObject("B141W41_quadA2");
            quad.transform.SetParent(parent.transform, false);
            quad.transform.localPosition = Vector3.zero;
            var seq = DeploySequence.PlayQuad(quad.transform, abovePos, landPos, slotScale, default(DeploySequence.Handoff), null);
            Check(seq != null, "A2 前提：PlayQuad（无交接）建出了序列");
            if (seq != null)
            {
                float tEnd = DeploySequence.HandoffTime + DeploySequence.HoldTime + DeploySequence.LandTime + 0.02f;
                AdvanceBy(seq, tEnd, 64);
                float d = Vector3.Distance(quad.transform.position, landPos);
                Tr($"A2 端点读数：世界距 {d:E3}（法 0）");
                Check(d < tol, $"A2 · :218 落下终点 = 世界目标 landPos（差 {d:E3}）");
                Vector3 naive = parent.transform.TransformPoint(landPos);
                Check(Vector3.Distance(quad.transform.position, naive) > 0.5f,
                      "A2 · 灭自证：不是「DOLocalMove(world) 漏换算」的错点｜🧨 漏换算 ⇒ 红");
                seq.Kill(false);
            }
            UnityEngine.Object.DestroyImmediate(quad);
            UnityEngine.Object.DestroyImmediate(parent);
        }

        // ---- A3 · Play 起飞（:282）**生产路径** 带交接 ----
        {
            var parent = MakeParent(pPos, pEuler, pScale);
            var cv = CardView.Create(parent.transform, CardData.Placeholder(2), "B141W41_cvA3", CardFace.Hand);
            cv.transform.localPosition = Vector3.zero;
            cv.transform.localRotation = Quaternion.identity;
            cv.transform.localScale = Vector3.one;
            var handoff = new DeploySequence.Handoff { FallTarget = cv.transform, LandLocal = Vector3.zero, Apply = () => { } };
            var seq = DeploySequence.Play(cv, abovePos, landPos, slotScale, handoff, null);
            Check(seq != null, "A3 前提：Play（带交接）建出了序列");
            if (seq != null)
            {
                AdvanceBy(seq, DeploySequence.MoveTime + 0.02f, 32);
                float d = Vector3.Distance(cv.transform.position, abovePos);
                Tr($"A3 端点读数：世界距 {d:E3}（法 0）");
                Check(d < tol, $"A3 · :282（生产路径）起飞终点 = 世界目标 abovePos（差 {d:E3}）");
                Vector3 naive = parent.transform.TransformPoint(abovePos);
                Check(Vector3.Distance(cv.transform.position, naive) > 0.5f,
                      "A3 · 灭自证：不是「漏换算」的错点｜🧨 漏换算 ⇒ 红");
                seq.Kill(false);
            }
            UnityEngine.Object.DestroyImmediate(cv.gameObject);
            UnityEngine.Object.DestroyImmediate(parent);
        }

        // ---- A4 · Play 落下（:301）无交接 ----
        {
            var parent = MakeParent(pPos, pEuler, pScale);
            var cv = CardView.Create(parent.transform, CardData.Placeholder(3), "B141W41_cvA4", CardFace.Hand);
            cv.transform.localPosition = Vector3.zero;
            cv.transform.localRotation = Quaternion.identity;
            cv.transform.localScale = Vector3.one;
            var seq = DeploySequence.Play(cv, abovePos, landPos, slotScale, default(DeploySequence.Handoff), null);
            Check(seq != null, "A4 前提：Play（无交接）建出了序列");
            if (seq != null)
            {
                float tEnd = DeploySequence.HandoffTime + DeploySequence.HoldTime + DeploySequence.LandTime + 0.02f;
                AdvanceBy(seq, tEnd, 64);
                float d = Vector3.Distance(cv.transform.position, landPos);
                Tr($"A4 端点读数：世界距 {d:E3}（法 0）");
                Check(d < tol, $"A4 · :301 落下终点 = 世界目标 landPos（差 {d:E3}）");
                Vector3 naive = parent.transform.TransformPoint(landPos);
                Check(Vector3.Distance(cv.transform.position, naive) > 0.5f,
                      "A4 · 灭自证：不是「漏换算」的错点｜🧨 漏换算 ⇒ 红");
                seq.Kill(false);
            }
            UnityEngine.Object.DestroyImmediate(cv.gameObject);
            UnityEngine.Object.DestroyImmediate(parent);
        }
    }

    // ==================================================================
    //  B · 灭自证（结构性）：补间途中把父级平移 delta
    //      · `DOLocalMove` ⇒ 卡跟着父级走：actual == newParent.TransformPoint(localAt) == delta + noRide；
    //      · `DOMove`     ⇒ 卡停在旧的「世界直线」上：actual == noRide（与 delta 无关）。
    //      数学上 ride − noRide 恰为 delta ⇒ 两条断言（跟父级 / 不在旧线上）互斥，DOMove 无法同时满足。
    // ==================================================================
    static void B_ParentMoveDiscriminates()
    {
        Vector3 pPos = new Vector3(0.3f, -0.2f, 0f);
        Vector3 pEuler = new Vector3(0f, 0f, 20f);
        Vector3 pScale = new Vector3(1.4f, 0.8f, 1f);
        Vector3 abovePos = new Vector3(1.5f, 1.0f, 0f);
        Vector3 landPos = new Vector3(1.5f, -1.0f, 0f);
        float slotScale = 0.4f;
        Vector3 startLocal = new Vector3(0.1f, -0.05f, 0f);

        var parent = MakeParent(pPos, pEuler, pScale);
        var cv = CardView.Create(parent.transform, CardData.Placeholder(4), "B141W41_cvB", CardFace.Hand);
        cv.transform.localPosition = startLocal;
        cv.transform.localRotation = Quaternion.identity;
        cv.transform.localScale = Vector3.one;

        // 父级移动**之前**先按原矩阵算好「目标局部点」与「起点世界」（= helper / DOTween 建补间时用的那一份）
        Quaternion q0 = Quaternion.Euler(pEuler);
        Matrix4x4 m0 = Matrix4x4.TRS(pPos, q0, pScale);
        Vector3 targetLocal = m0.inverse.MultiplyPoint3x4(abovePos);
        Vector3 origStartWorld = m0.MultiplyPoint3x4(startLocal);

        var seq = DeploySequence.Play(cv, abovePos, landPos, slotScale, default(DeploySequence.Handoff), null);
        Check(seq != null, "B 前提：Play（无交接）建出了序列");
        if (seq != null)
        {
            // 推进到 0.1 s（u = 1/3）—— 让补间先把 start 定住、跑到一半
            AdvanceBy(seq, DeploySequence.MoveTime * (1f / 3f), 20);
            Vector3 delta = new Vector3(3f, 2f, 0f);
            parent.transform.position = pPos + delta;   // 🔴 父级在补间途中移动

            // 再推进到 0.2 s（u = 2/3）
            AdvanceBy(seq, DeploySequence.MoveTime * (1f / 3f), 20);
            float e = OutQuad(2f / 3f);
            Vector3 localAt = Vector3.LerpUnclamped(startLocal, targetLocal, e);
            Matrix4x4 m1 = Matrix4x4.TRS(pPos + delta, q0, pScale);
            Vector3 expectedRide = m1.MultiplyPoint3x4(localAt);                 // DOLocalMove：跟父级
            Vector3 noRide = Vector3.LerpUnclamped(origStartWorld, abovePos, e);  // DOMove：不跟父级

            float dRide = Vector3.Distance(cv.transform.position, expectedRide);
            float dNoRide = Vector3.Distance(cv.transform.position, noRide);
            Tr($"B 读数：dRide {dRide:E3} · dNoRide {dNoRide:F3}（delta 长 {delta.magnitude:F3}）");
            Check(dRide < 5e-2f,
                  $"B · 补间途中父级移动 ⇒ 卡**跟着父级走**（距期望 {dRide:E3}）—— 这是 DOLocalMove 的行为");
            Check(dNoRide > 1.5f,
                  $"B · 灭自证：卡**没有**停在旧的「世界直线」上（距 DOMove 轨迹 {dNoRide:F3}）"
                + "｜🧨 把 LocalMoveToWorld 换回 tr.DOMove ⇒ 卡不跟父级 ⇒ dNoRide≈0 ⇒ 红（结构上 DOMove 不可能同时过这两条）");
            seq.Kill(false);
        }
        UnityEngine.Object.DestroyImmediate(cv.gameObject);
        UnityEngine.Object.DestroyImmediate(parent);
    }

    // ==================================================================
    //  C · 生产路径 :282 覆盖：`CardInteraction.cs:1481` 走的是「Play + 有效 handoff」
    //      ⇒ 0.95 s 交接必须真跑、起飞端点对、交接后落体（:293）到达竞技场局部落点。
    // ==================================================================
    static void C_ProductionPath()
    {
        Vector3 pPos = new Vector3(-0.2f, 0.1f, 0f);
        Vector3 pEuler = new Vector3(0f, 0f, -15f);
        Vector3 pScale = new Vector3(1.2f, 0.9f, 1f);
        Vector3 abovePos = new Vector3(1.0f, 0.9f, 0f);
        Vector3 landPos = new Vector3(1.0f, -0.7f, 0f);
        float slotScale = 0.36f;
        Vector3 landLocal = new Vector3(0.2f, -0.3f, 0f);

        var parent = MakeParent(pPos, pEuler, pScale);
        var cv = CardView.Create(parent.transform, CardData.Placeholder(5), "B141W41_cvC", CardFace.Hand);
        cv.transform.localPosition = Vector3.zero;
        cv.transform.localRotation = Quaternion.identity;
        cv.transform.localScale = Vector3.one;

        var fallGo = new GameObject("B141W41_fallTarget");
        fallGo.transform.SetParent(parent.transform, false);
        int applyCount = 0;
        var handoff = new DeploySequence.Handoff
        {
            Apply = () => { applyCount++; },
            FallTarget = fallGo.transform,
            LandLocal = landLocal,
        };
        var seq = DeploySequence.Play(cv, abovePos, landPos, slotScale, handoff, null);
        Check(seq != null, "C 前提：Play（带交接 = 生产路径）建出了序列");
        if (seq != null)
        {
            float tEnd = DeploySequence.HandoffTime + DeploySequence.HoldTime + DeploySequence.LandTime + 0.05f;
            AdvanceBy(seq, tEnd, 64);
            Check(applyCount == 1, $"C · :282 生产路径的 0.95 s 交接**真的跑了**（applyCount={applyCount}）");
            float dCard = Vector3.Distance(cv.transform.position, abovePos);
            Check(dCard < 3e-2f,
                  $"C · 生产路径起飞终点 = 世界目标（差 {dCard:E3}）—— `CardInteraction` 走的正是这条（:282）");
            float dFall = Vector3.Distance(fallGo.transform.localPosition, landLocal);
            Check(dFall < 2e-2f,
                  $"C · 交接后的落体到竞技场局部落点（差 {dFall:E3}，:293 `DOLocalMove` 本来已是局部）");
            seq.Kill(false);
        }
        UnityEngine.Object.DestroyImmediate(fallGo);
        UnityEngine.Object.DestroyImmediate(cv.gameObject);
        UnityEngine.Object.DestroyImmediate(parent);
    }

    // ==================================================================
    //  D · 源码级防回退：`tr.DOMove(` 必须为 0；`LocalMoveToWorld(` 应 ≥ 5（1 定义 + 4 调用）
    // ==================================================================
    static void D_NoDomoveRemains()
    {
        string path = Application.dataPath + "/CardPresentation/Board/DeploySequence.cs";
        string src = null;
        try { src = System.IO.File.ReadAllText(path); }
        catch (Exception e) { Check(false, "D 前提：读得到 `Board/DeploySequence.cs` —— " + e.Message); return; }
        Check(src != null && src.Length > 0, "D 前提：源码非空");

        int domove = CountOccur(src, "tr.DOMove(");
        int local = CountOccur(src, "LocalMoveToWorld(");
        Tr($"D 读数：`tr.DOMove(` {domove} 处 · `LocalMoveToWorld(` {local} 处");
        Check(domove == 0, $"D · 4 处起飞/落下已无 `tr.DOMove(`（现读 {domove} 处）｜🧨 任意一处回退 ⇒ 红");
        Check(local >= 5, $"D · `LocalMoveToWorld(` 出现 ≥ 5 次（1 定义 + 4 调用；现读 {local}）");
    }

    static int CountOccur(string s, string needle)
    {
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }
}
