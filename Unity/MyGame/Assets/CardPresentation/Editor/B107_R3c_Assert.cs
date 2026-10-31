// B107_R3c_Assert.cs — 执行代理 `B107_R5`（第一百零七会话 · 资源侧施工单 v2 §3·2 的 **G3**）
//
// ============================ 它盯的是什么 ============================
//  R3-02 专表里「**手写等价已落、只差一条断言**」的那两条 legacy clip：
//
//   **A 段 `Card Display Open`**（`length = 0.3166667`）—— 值已逐键抄进 `Core/CardFan.cs`
//      （`OpenTime` / `OpenStartScale` / `OpenStartAnchoredX,Y` / `_x/_y/_rot/_scl` 的槽 0–4）。
//      本段把**这些常量**逐条对回 `Resources/misc_clips.json`（= 原版 clip 的逐键转录，⛔ 不是我们挑的），
//      锁住「值没被换错」。⛔ **不重复** `Editor/BattleScene.cs` 那套「行为 vs 常量」的自证式断言 ——
//      那一套只能证明「代码照常量跑」，证明不了「常量 = 原版」。
//
//   **B 段 `SearchingOpponentCog`**（`length = 10.016666`）—— 37 键 / Z 0→360° / Loop，
//      值在 `Shell/SearchingMatchPopup.cs` 的 `CogKeyT`/`CogKeyZ` + `CogLoop`；本段对回同一条数据备份，
//      并逐键 + 中段采样（**端点对的 ≠ 曲线对**，见灭自证）。
//
// ============================ 判据出处 ============================
//  · 数据备份 = `MyGame/Assets/CardPresentation/Resources/misc_clips.json`
//    （生成器 `工具/gen_misc_clips.py`；源件
//     `d:/2/新解包资源/assets_full/bundle_duplicateassetisolation_assets_all/AnimationClip/AnimationClip_1560242982351263260.json`
//     与 `bundle_menus_assets_all/AnimationClip/AnimationClip_-6716633893720174568.json`，逐键含 `si/so`）。
//  · `Card Display Open` 的消费方 = 13 个战场场景 + 主菜单的 `Card Display Window` 上的 `Animation`
//    （`m_PlayAutomatically = true`，`m_Animations` 只此一条）。
//  · `SearchingOpponentCog` 的消费方 = 5 个 `Skull` 上的 `Animation`（`m_PlayAutomatically = true`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1：把 `CardFan.OpenTime` 改成别的数 ⇒ A1 红（数据备份不吃我们这边改）。
//  · A4：把 `_x/_y/_rot/_scl` 任一格改掉 ⇒ 对应那一格红（**逐槽逐轴**，不合并成「差不多」）。
//  · A4 的旋转由**四元数现算**（`2·atan2(z,w)`），⛔ 不回读 `CardFan._rot` 以外的中间量。
//  · B4：把 `CogKeyZ` 任一格改掉 ⇒ 该键那一行红；把曲线偷懒成「首尾两点 0→360」⇒ 37 键那条与逐键那条同时红。
//  · B5：只保留 `(t,v)` 重建（= 把每段变成 ease-in/out 的老坑）⇒ **中段采样**那批红（端点仍然对得上 ⇒ 端点断言抓不到）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  纯数据断言 —— **不建场景、不建窗、不碰存档**。宿主里加：
//      B107R3c_Assert.Run(); _pass += B107R3c_Assert.Pass; _fail += B107R3c_Assert.Fail;
//  推荐宿主 = `Editor/ShellScene.cs`（9 s，最便宜；本断言与场景无关）；`BattleScene` / `MainMenuScene` 同样适用。
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B107_R5`：R3-02 两条「手写等价 vs 原版 clip 数据」的值对齐断言。</summary>
public static class B107R3c_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B107R3c] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B107_R5 · G3：R3-02 两条 legacy clip 的**值对齐**（Card Display Open / SearchingOpponentCog）");
        SectionA_CardDisplayOpen();
        SectionB_SearchingOpponentCog();
        Debug.Log(_sink.P + "B107R3c 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==========================================================================================
    //  A 段 —— `Card Display Open` vs `Core/CardFan.cs`
    // ==========================================================================================
    static string SlotPathA(int i) { return "Card Display/Cards/CardUI" + (i == 0 ? "" : " (" + i + ")"); }

    /// <summary>四元数 (0,0,z,w) 绕 Z 的角度（度）= `2·atan2(z, w)`；判据 = 「槽 3 是 12.750°」那条复算口径。</summary>
    static float AngleOfZ(B107MiscClips.Key k)
    {
        if (k == null || k.v == null || k.v.Length < 4) return float.NaN;
        return 2f * Mathf.Atan2(k.v[2], k.v[3]) * Mathf.Rad2Deg;
    }

    static void SectionA_CardDisplayOpen()
    {
        MenuCheck.Section(_sink, "A 原版 legacy clip `Card Display Open`：`CardFan` 那批常量逐条对回数据备份");

        var clip = B107MiscClips.Find("Card Display Open");
        T(clip != null, "A0 **前提**：`Resources/misc_clips.json` 里取得到 `Card Display Open`"
                      + "（没有 ⇒ 本段一条都没跑，看 A0 这条红即可）");
        if (clip == null) return;
        T(clip.curves != null && clip.curves.Length > 0, "A0b **前提**：这条 clip 的曲线表非空");
        if (clip.curves == null || clip.curves.Length == 0) return;

        // ---- A1：时长 ----
        N(clip.length, CardFan.OpenTime, 1e-5f,
          "A1 ★ `CardFan.OpenTime` = clip `length`（现读 " + clip.length.ToString("F7")
        + "；⚠️ 常量 `0.3166667f` 与数据差 **1 ULP**（2.98e-8）⇒ 容差 1e-5）｜🧨 改 `OpenTime` ⇒ 本行红");

        // ---- A2：曲线构成（20 条 = 5 旋转 + 5 缩放 + 10 位移）----
        int rot = 0, scl = 0, apx = 0, apy = 0;
        for (int i = 0; i < clip.curves.Length; i++)
        {
            var cu = clip.curves[i];
            if (cu == null || cu.property == null) continue;
            if (cu.property == "m_LocalRotation") rot++;
            else if (cu.property == "m_LocalScale") scl++;
            else if (cu.property == "m_AnchoredPosition.x") apx++;
            else if (cu.property == "m_AnchoredPosition.y") apy++;
        }
        C(rot, 5, "A2a 旋转曲线 5 条（槽 0–4 各一条）");
        C(scl, 5, "A2b 缩放曲线 5 条");
        C(apx, 5, "A2c `m_AnchoredPosition.x` 5 条");
        C(apy, 5, "A2d `m_AnchoredPosition.y` 5 条");
        C(clip.curves.Length, 20, "A2e 曲线总数 20 条（`misc_clips.json` 的 `keysTotal` 口径 = 20×键数之和，别混）");

        // ---- A3：每条曲线都收到 `length` ----
        float worstEnd = 0f; string worstWhat = "";
        for (int i = 0; i < clip.curves.Length; i++)
        {
            var cu = clip.curves[i];
            if (cu == null || cu.keys == null || cu.keys.Length == 0) continue;
            var last = cu.keys[cu.keys.Length - 1];
            float d = Mathf.Abs(last.t - clip.length);
            if (d > worstEnd) { worstEnd = d; worstWhat = cu.property + "@" + cu.path; }
        }
        T(worstEnd < 1e-6f,
          "A3 每条曲线的最晚键都落在 `length` 上（最大偏差 " + worstEnd.ToString("E2") + " @ " + worstWhat + "）");

        // ---- A4：起点（t = 0）—— 5 个槽**一模一样** ----
        float worstS0 = 0f, worstX0 = 0f, worstY0 = 0f, worstRot0 = 0f;
        for (int i = 0; i < 5; i++)
        {
            var s = B107MiscClips.FindCurve(clip, "m_LocalScale", SlotPathA(i));
            var ax = B107MiscClips.FindCurve(clip, "m_AnchoredPosition.x", SlotPathA(i));
            var ay = B107MiscClips.FindCurve(clip, "m_AnchoredPosition.y", SlotPathA(i));
            var rt = B107MiscClips.FindCurve(clip, "m_LocalRotation", SlotPathA(i));
            if (s == null || ax == null || ay == null || rt == null) continue;
            worstS0 = Mathf.Max(worstS0, Mathf.Abs(s.keys[0].v[0] - CardFan.OpenStartScale));
            worstX0 = Mathf.Max(worstX0, Mathf.Abs(ax.keys[0].v[0] - CardFan.OpenStartAnchoredX));
            worstY0 = Mathf.Max(worstY0, Mathf.Abs(ay.keys[0].v[0] - CardFan.OpenStartAnchoredY));
            worstRot0 = Mathf.Max(worstRot0, Mathf.Abs(AngleOfZ(rt.keys[0]) - CardFan.OpenStartRot));
        }
        N(worstS0, 0f, 1e-3f, "A4a 起点缩放（5 槽）都 = `CardFan.OpenStartScale` 210");
        N(worstX0, 0f, 1e-3f, "A4b 起点 `anchoredPosition.x` 都 = `CardFan.OpenStartAnchoredX` 0");
        N(worstY0, 0f, 1e-3f, "A4c 起点 `anchoredPosition.y` 都 = `CardFan.OpenStartAnchoredY` 60");
        N(worstRot0, 0f, 1e-3f, "A4d 起点旋转（四元数 ⇒ 角度）都 = `CardFan.OpenStartRot` 0°");

        // ---- A5：终点（t = length）—— 槽 0–4 逐槽逐轴 ----
        // 🔴 2026-10-31（`B107_D7` 诊断 · 本支 `B107_W32` 落）：原式 `cScl / ss.keys[0].v[0]` 的分母是
        //   **本槽的起点缩放**（键 0 = t0 = 210），而 `CardFan.Wpx(i)/FrontWpx` 的分母是
        //   `CardFan._scl[0]` = **槽 0 的终点缩放** 250（`Core/CardFan.cs:42,108,112,115`）
        //   ⇒ 期望值整体被放大 `250/210 = 1.190476`，`A5[0..4]s` 五条全红。
        //   判据（原版数据自证，⛔ 不回读 `CardFan`）：`rel_i = 末键(槽 i) / 末键(槽 0)`，
        //   两端都取 `misc_clips.json` 的 clip 末键（t = length）。⛔ 别改 `CardFan`，它和原版一比就对上。
        var s0Curve = B107MiscClips.FindCurve(clip, "m_LocalScale", SlotPathA(0));
        float s0End = (s0Curve != null && s0Curve.keys != null && s0Curve.keys.Length > 0)
                      ? s0Curve.keys[s0Curve.keys.Length - 1].v[0] : float.NaN;
        // ⚠️ 不额外加「前提断言」：曲线是否存在已由上面的 A0b / A2b 覆盖；万一本条取不到末键，
        //   `s0End` = NaN ⇒ `Mathf.Abs(got - NaN) <= tol` 恒 false ⇒ 五条会**红**，不会静默变绿。
        // ⚠️ 判据 `判据/B107_D7_Shell红诊断.md:68` 那段可粘贴代码是**对的**（`FindCurve(clip, property, path)`
        //   三个实参都与方法签名一致）；照抄前仍请先看一眼 `B107MiscClips.FindCurve` 的签名。

        for (int i = 0; i < 5; i++)
        {
            var sx = B107MiscClips.FindCurve(clip, "m_AnchoredPosition.x", SlotPathA(i));
            var sy = B107MiscClips.FindCurve(clip, "m_AnchoredPosition.y", SlotPathA(i));
            var sr = B107MiscClips.FindCurve(clip, "m_LocalRotation", SlotPathA(i));
            var ss = B107MiscClips.FindCurve(clip, "m_LocalScale", SlotPathA(i));
            if (sx == null || sy == null || sr == null || ss == null)
            { T(false, "A5[" + i + "] **前提**：槽 " + SlotPathA(i) + " 的四条曲线都在（缺 ⇒ 下面三条没跑）"); continue; }

            float cx = sx.keys[sx.keys.Length - 1].v[0];
            float cy = sy.keys[sy.keys.Length - 1].v[0];
            float cRot = AngleOfZ(sr.keys[sr.keys.Length - 1]);
            float cScl = ss.keys[ss.keys.Length - 1].v[0];

            N(CardFan.X(i), cx, 1e-3f, "A5[" + i + "]x 槽 " + i + " 横向 = clip 末键（px）");
            N(CardFan.Y(i), cy, 1e-3f, "A5[" + i + "]y 槽 " + i + " 纵向 = clip 末键（px，原版 y 向上）");
            N(CardFan.Rot(i), cRot, 0.01f, "A5[" + i + "]r 槽 " + i + " 倾角 = clip 四元数现算（度）");
            N(CardFan.Wpx(i) / CardFan.FrontWpx, cScl / s0End, 1e-4f,
              "A5[" + i + "]s 槽 " + i + " 相对倍率 = 末端缩放(i) / 末端缩放(槽 0)（"
              + cScl.ToString("F4") + " / " + s0End.ToString("F1") + "）"
            + "｜🧨 改 `CardFan._scl` 的末键 ⇒ 本行红");
        }

        // ---- A6：结构备注（槽 5–8 是**我们外推的**，别当成原版值）----
        C(CardFan.Slots, 9,
          "A6 槽数 = 9（**原版只有 1+4**；槽 5–8 是我们按 2026-09-26 用户授权外推的 ⇒ 上面 A5 只逐槽核 0–4）");
    }

    // ==========================================================================================
    //  B 段 —— `SearchingOpponentCog` vs `Shell/SearchingMatchPopup.cs`
    // ==========================================================================================
    static void SectionB_SearchingOpponentCog()
    {
        MenuCheck.Section(_sink, "B 原版 legacy clip `SearchingOpponentCog`：37 键 / Z 0→360° / Loop，值对回数据备份");

        var clip = B107MiscClips.Find("SearchingOpponentCog");
        T(clip != null, "B0 **前提**：`Resources/misc_clips.json` 里取得到 `SearchingOpponentCog`");
        if (clip == null) return;
        T(clip.curves != null && clip.curves.Length == 1, "B0b **前提**：这条 clip 只有 1 条曲线（`Transform.localEulerAnglesRaw` @ `Cog`）");
        if (clip.curves == null || clip.curves.Length != 1) return;
        var cu = clip.curves[0];
        T(cu.keys != null && cu.keys.Length > 0, "B0c **前提**：曲线键表非空");
        if (cu.keys == null || cu.keys.Length == 0) return;

        // ---- B1：时长 / 回绕档 ----
        N(clip.length, SearchingMatchPopup.CogLoop, 1e-6f,
          "B1 ★ `SearchingMatchPopup.CogLoop` = clip `length`（现读 " + clip.length.ToString("F7") + "）｜🧨 改 `CogLoop` ⇒ 本行红");
        C(clip.wrapMode, "Loop", "B1b 回绕档 = Loop（原版 `m_WrapMode = 2`）");

        // ---- B2：37 键 / 首尾 ----
        C(cu.keys.Length, 37, "B2a 曲线键数 = 37");
        var k0 = cu.keys[0];
        var kN = cu.keys[cu.keys.Length - 1];
        N(k0.t, 0f, 1e-6f, "B2b 首键时刻 = 0");
        N(k0.v[2], 0f, 1e-4f, "B2c 首键 Z = 0°");
        N(kN.t, SearchingMatchPopup.CogLoop, 1e-6f, "B2d 末键时刻 = `CogLoop`");
        N(kN.v[2], 360f, 1e-4f, "B2e 末键 Z = 360°");

        // ---- B3：每档 +20°（键之间要么不涨、要么涨 20°）----
        int badDelta = 0, steps = 0; string badWhat = "";
        for (int i = 1; i < cu.keys.Length; i++)
        {
            float d = cu.keys[i].v[2] - cu.keys[i - 1].v[2];
            if (d > 1e-4f) steps++;
            if (Mathf.Abs(d) > 1e-4f && Mathf.Abs(d - 20f) > 1e-3f)
            { badDelta++; if (badWhat.Length < 60) badWhat += "[" + i + "]=" + d.ToString("F3") + " "; }
        }
        C(badDelta, 0, "B3 相邻键的 Z 增量只有 0° / +20° 两种（现读 " + steps + " 次 +20°；异常：" + badWhat + "）");
        C(steps, 18, "B3b 一共 18 档 × 20° = 360°");

        // ---- B4：逐键采样（`CogAngleAt` 在键时刻必须等于键值）----
        //  ⚠️ **末键（t == `CogLoop`）不能用这一条核**：`CogLoop` 那一拍按 Loop 回绕到 0（B6a 单独断），
        //     拿它去比 360° 会假红。
        float worstKey = 0f; int worstKeyI = -1;
        for (int i = 0; i < cu.keys.Length; i++)
        {
            if (Mathf.Abs(cu.keys[i].t - SearchingMatchPopup.CogLoop) < 1e-6f) continue;
            float got = SearchingMatchPopup.CogAngleAt(cu.keys[i].t);
            float d = Mathf.Abs(got - cu.keys[i].v[2]);
            if (d > worstKey) { worstKey = d; worstKeyI = i; }
        }
        N(worstKey, 0f, 0.05f,
          "B4 ★ 键时刻上 `CogAngleAt` 都等于键值（末键回绕那一拍由 B6a 核；最大偏差 " + worstKey.ToString("E2") + "° @ 键 " + worstKeyI + "）"
        + "｜🧨 改 `CogKeyZ/CogKeyT` 任一格 ⇒ 本行红");

        // ---- B5：中段采样（**端点对的 ≠ 曲线对**：只存 (t,v) 重建会把每段变成 ease-in/out）----
        float worstMid = 0f; int worstMidI = -1;
        for (int i = 0; i + 1 < cu.keys.Length; i++)
        {
            float ta = cu.keys[i].t, tb = cu.keys[i + 1].t;
            if (tb - ta < 1e-5f) continue;                      // 同一时刻的两键（阶跃）不采样
            float za = cu.keys[i].v[2], zb = cu.keys[i + 1].v[2];
            for (int q = 1; q <= 3; q++)
            {
                float f = q / 4f;
                float t = ta + (tb - ta) * f;
                float want = za + (zb - za) * f;
                float got = SearchingMatchPopup.CogAngleAt(t);
                float d = Mathf.Abs(got - want);
                if (d > worstMid) { worstMid = d; worstMidI = i; }
            }
        }
        N(worstMid, 0f, 0.05f,
          "B5 ★ 每段 3 个四分点都落在**直线**上（最大偏差 " + worstMid.ToString("E2") + "° @ 段 " + worstMidI + "）"
        + "｜🧨 把插值换成 ease-in/out（端点仍对）⇒ 本行红");

        // ---- B6：回绕 ----
        N(SearchingMatchPopup.CogAngleAt(SearchingMatchPopup.CogLoop), 0f, 0.05f,
          "B6a `CogAngleAt(CogLoop)` 回到 0°（= Loop 回绕）");
        N(SearchingMatchPopup.CogAngleAt(SearchingMatchPopup.CogLoop + 0.25f),
          SearchingMatchPopup.CogAngleAt(0.25f), 0.05f,
          "B6b `CogAngleAt(t + CogLoop) == CogAngleAt(t)`（一圈一周期）");
    }
}

/// <summary>`Resources/misc_clips.json` 的只读装载器（`B107_R5` 建的；`B107R3d_Assert` 也用它）。
/// <para>schema 见 `工具/gen_misc_clips.py` 头部；这里只声明断言要用的字段（未知字段 JsonUtility 会忽略）。</para>
/// <para>⚠️ `JsonUtility` 对**未知字段**宽容、对**缺字段**给默认值 ⇒ 装载后必须显式判 `null`（各路都做了前提断言）。</para></summary>
public static class B107MiscClips
{
    [Serializable] public sealed class Key { public float t; public float[] v; public float[] si; public float[] so; }
    [Serializable] public sealed class Curve { public string type; public string property; public string path; public Key[] keys; }
    [Serializable] public sealed class Clip { public string name; public string pkg; public bool legacy; public float sampleRate; public string wrapMode; public float length; public int keysTotal; public bool orphan; public string alreadyRepacked; public Curve[] curves; }
    [Serializable] public sealed class Doc { public int schema; public Clip[] clips; }

    static Doc _doc;
    static bool _tried;

    /// <summary>装载（进程内缓存一次）。取不到 ⇒ 出声 + 返回 null（⛔ 不静默）。</summary>
    public static Doc Load()
    {
        if (_tried) return _doc;
        _tried = true;
        var ta = Resources.Load<TextAsset>("misc_clips");
        if (ta == null)
        {
            Debug.LogError("[B107MiscClips] 🔴 `Resources/misc_clips.json` 取不到 ⇒ 本轮 R3-02 的值对齐断言**一条都跑不了**。"
                         + "先跑 `python -I -X utf8 d:/4/Unity/工具/gen_misc_clips.py --write`");
            return null;
        }
        try { _doc = JsonUtility.FromJson<Doc>(ta.text); }
        catch (Exception e)
        {
            Debug.LogError("[B107MiscClips] 🔴 `misc_clips.json` 解析失败：" + e.GetType().Name + ": " + e.Message);
            _doc = null;
        }
        return _doc;
    }

    /// <summary>自检用：清缓存。</summary>
    public static void Reset() { _doc = null; _tried = false; }

    /// <summary>按名字取一条 clip；没有就返回 null（⛔ 别拿第一条顶）。</summary>
    public static Clip Find(string name)
    {
        var d = Load();
        if (d == null || d.clips == null) return null;
        for (int i = 0; i < d.clips.Length; i++)
            if (d.clips[i] != null && d.clips[i].name == name) return d.clips[i];
        return null;
    }

    /// <summary>按 `property` + `path` 取一条曲线；没有就返回 null。</summary>
    public static Curve FindCurve(Clip c, string property, string path)
    {
        if (c == null || c.curves == null) return null;
        for (int i = 0; i < c.curves.Length; i++)
        {
            var cu = c.curves[i];
            if (cu != null && cu.property == property && cu.path == path) return cu;
        }
        return null;
    }
}
