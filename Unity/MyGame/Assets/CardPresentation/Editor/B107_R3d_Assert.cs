// B107_R3d_Assert.cs — 执行代理 `B107_R5`（第一百零七会话 · 资源侧施工单 v2 §3·2 的 **G5** ＋ §3·3 的 **U3**）
//
// ============================ 它盯的是什么 ============================
//  **A 段 教程指针的「脉冲」**（R3-02 专表：`Tutorial Pointer Menu`，1.0 s Loop）
//     判据 = 原版那条 legacy clip，全部现读（`Resources/misc_clips.json`；源件
//     `bundle_generalgamewindows_assets_all/AnimationClip/AnimationClip_846904233807445656.json`）：
//       · `m_Legacy = true` · `m_SampleRate 60` · `m_WrapMode 2`(**Loop**) · `m_StopTime` = **1.0 s**
//       · 曲线 **2 条**（`path = ""` ⇒ 绑在 `Animation` 所在的 GO = **`Glow (1)` 自己**）：
//           `Transform m_LocalScale`  t=0 → **0.24**、t=1.0 → **6.0**
//           `MonoBehaviour m_Color.a` t=0 → **0.5** 、t=1.0 → **0**
//         两条都是「2 键 + `inSlope/outSlope` 全 0」的直线段。
//       · 挂载 = `Glow (1)` 上那颗 `Animation`（`m_PlayAutomatically = true`、`m_Animations` 只此一条）；
//         **Menu 与 Wildcards 两份 prefab 都有**（`Animation_-4428924928474591948.json` /
//         `Animation_-5936930161473697592.json`），`TutorialPointerBase` / `TutorialPointerCombat` 没有 `Glow (1)`。
//     我们那半边 = `Shell/TutorialPointerUi.cs` 文件尾那个 `partial` 块
//     （`GlowLoop/GlowScaleFrom/GlowScaleTo/GlowAlphaFrom/GlowAlphaTo` + `GlowScaleAt/GlowAlphaAt` + `GlowTick/GlowApply`）。
//
//  **B 段「bridge 真的播了」的证据（U3）**（R3-02 专表里那两条 **GUID 消费** clip）
//     `Dark Angels Void Combat animations`（12 s Loop）与 `LightAnimationOrbit`（3 s）：原版按 **assetGUID**
//     被 `ScenarioAnimationBlend` 取，运行时由 `WarpforgeVFX.WarpforgeAnimatorBridge` 从
//     `StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle` 取原件。本段走**生产那条读口**
//     `WarpforgeAnimatorBridge.TryGetClip(<裸名>)`，证明「桥真能拿到这条 clip 对象」。
//     ⚠️ 这条**不是**「已经播了」的全证 —— `Animation.AddClip` + 宿主的 `myAnimation` 那两跳由
//     `Editor/BattleScene.cs` 的 **A201** 覆盖（那边用 `ScenarioAnimationBlend.clipLoader`）。
//     两半合起来才是完整链：**桥取得到（本段）· 组件的 `Animation` 挂得上（A201）**。
//
// ============================ 🔴 灭自证 ============================
//  · A1：把 `GlowScaleFrom/To` 或 `GlowAlphaFrom/To` 任一改成别的数 ⇒ A1 对应那行红。
//  · A4：只写 `_fade`、不乘脉冲 alpha（= 改回「两态静值」）⇒ A4 的 alpha 那几条红。
//  · A5：不把 `GlowTick` 接进 `Tick`（播放头冻在 0）⇒ A5 的中段那几条红（起点那两条仍然绿 ⇒ 起点断言抓不到）。
//  · A6：把 `Tier` 判断改回「只有 Menu」⇒ Wildcards 那两条红；改成「所有 Tier」⇒ Base 那条红。
//  · B1/B2：`TryGetClip` 取不到 ⇒ 整段红（那正是「读不到就立账」的信号，⛔ 别把红线改成绿的）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  A 段只建临时 GO（用完 `DestroyImmediate`，不落盘、不碰存档）；B 段只读 bundle。
//  宿主里加：
//      B107R3d_Assert.Run(); _pass += B107R3d_Assert.Pass; _fail += B107R3d_Assert.Fail;
//  推荐宿主 = `Editor/ShellScene.cs`（9 s，最便宜）；`MainMenuScene` / `BattleScene` 同样适用。
using CardPresentation;
using UnityEngine;
using WarpforgeVFX;

/// <summary>执行代理 `B107_R5`：教程指针脉冲（G5）＋ 两条 GUID 消费 clip 的 bridge 读口证据（U3）。</summary>
public static class B107R3d_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B107R3d] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B107_R5 · G5：教程指针 `Glow (1)` 脉冲（原版 clip `Tutorial Pointer Menu`，1.0 s Loop）");
        SectionA_GlowPulse();
        MenuCheck.Section(_sink, "B107_R5 · U3：两条 GUID 消费 clip 的 bridge 读口（`WarpforgeAnimatorBridge.TryGetClip`）");
        SectionB_BridgeEvidence();
        Debug.Log(_sink.P + "B107R3d 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==========================================================================================
    //  A 段 —— `Glow (1)` 的脉冲
    // ==========================================================================================
    static void SectionA_GlowPulse()
    {
        // ---- A0：数据前提 ----
        var clip = B107MiscClips.Find("Tutorial Pointer Menu");
        T(clip != null, "A0 **前提**：`Resources/misc_clips.json` 里取得到 `Tutorial Pointer Menu`（没有 ⇒ 本段一条都没跑）");
        if (clip == null) return;
        T(clip.curves != null && clip.curves.Length == 2, "A0b **前提**：这条 clip 有 2 条曲线（1 缩放 + 1 alpha）");
        if (clip.curves == null || clip.curves.Length != 2) return;
        var scaleCu = B107MiscClips.FindCurve(clip, "m_LocalScale", "");
        var alphaCu = B107MiscClips.FindCurve(clip, "m_Color.a", "");
        T(scaleCu != null && scaleCu.keys != null && scaleCu.keys.Length == 2, "A0c **前提**：缩放曲线存在且是 2 键");
        T(alphaCu != null && alphaCu.keys != null && alphaCu.keys.Length == 2, "A0d **前提**：alpha 曲线存在且是 2 键");
        if (scaleCu == null || scaleCu.keys == null || scaleCu.keys.Length != 2
            || alphaCu == null || alphaCu.keys == null || alphaCu.keys.Length != 2) return;

        // ---- A1：常量 == clip 数据 ----
        C(clip.wrapMode, "Loop", "A1a 回绕档 = Loop（原版 `m_WrapMode = 2`）");
        N(clip.sampleRate, 60f, 1e-4f, "A1b 采样率 60");
        N(clip.length, TutorialPointerUi.GlowLoop, 1e-6f,
          "A1c ★ `TutorialPointerUi.GlowLoop` = clip `m_StopTime`（1.0 s）｜🧨 改 `GlowLoop` ⇒ 本行红");
        N(TutorialPointerUi.GlowScaleFrom, scaleCu.keys[0].v[0], 1e-4f,
          "A1d `GlowScaleFrom` = clip t=0 的 `m_LocalScale`（0.24）");
        N(TutorialPointerUi.GlowScaleTo, scaleCu.keys[1].v[0], 1e-4f,
          "A1e `GlowScaleTo` = clip t=1 的 `m_LocalScale`（6.0）");
        N(alphaCu.keys[0].t, 0f, 1e-6f, "A1f alpha 曲线首键时刻 = 0");
        N(alphaCu.keys[1].t, clip.length, 1e-6f, "A1g alpha 曲线末键时刻 = clip 长度");
        N(TutorialPointerUi.GlowAlphaFrom, alphaCu.keys[0].v[0], 1e-4f,
          "A1h `GlowAlphaFrom` = clip t=0 的 `m_Color.a`（0.5）");
        N(TutorialPointerUi.GlowAlphaTo, alphaCu.keys[1].v[0], 1e-4f,
          "A1i `GlowAlphaTo` = clip t=1 的 `m_Color.a`（0）");

        // ---- A2：曲线形状（2 键直线 ⇒ 采样应逐点线性）----
        N(TutorialPointerUi.GlowScaleAt(0f), 0.24f, 1e-3f, "A2a `GlowScaleAt(0)` = 0.24");
        N(TutorialPointerUi.GlowScaleAt(0.25f), 1.68f, 1e-3f, "A2b `GlowScaleAt(0.25)` = 1.68（线性）");
        N(TutorialPointerUi.GlowScaleAt(0.5f), 3.12f, 1e-3f, "A2c `GlowScaleAt(0.5)` = 3.12");
        N(TutorialPointerUi.GlowScaleAt(1f), 0.24f, 1e-3f, "A2d `GlowScaleAt(1.0)` 回到 0.24（Loop 回绕）");
        N(TutorialPointerUi.GlowAlphaAt(0f), 0.5f, 1e-3f, "A2e `GlowAlphaAt(0)` = 0.5");
        N(TutorialPointerUi.GlowAlphaAt(0.5f), 0.25f, 1e-3f, "A2f `GlowAlphaAt(0.5)` = 0.25");
        N(TutorialPointerUi.GlowAlphaAt(0.75f), 0.125f, 1e-3f, "A2g `GlowAlphaAt(0.75)` = 0.125");
        N(TutorialPointerUi.GlowAlphaAt(1f), 0.5f, 1e-3f, "A2h `GlowAlphaAt(1.0)` 回到 0.5");

        // ---- A3：图的前提（缺图 ⇒ 下面全是「没画」，必须单独说清）----
        T(CardArt.MenuUi(TutorialPointerUi.ArtBorderGlow) != null,
          "A3 **前提**：`Glow (1)` 的图 `Border_Circles_FX` 取得到（取不到 ⇒ 下面 A4/A5/A6 的 `GlowDrawn` 会假红）");

        // ---- A4：Menu 档的实况（造临时指针 → Show → 逐拍 Tick）----
        var root = new GameObject("B107R3d_ROOT");
        TutorialPointerUi p = null;
        try
        {
            p = TutorialPointerUi.Create(root.transform, TutorialPointerUi.Tier.Menu);
            T(p != null, "A4a **前提**：`TutorialPointerUi` 挂得上去（批处理里 `Awake` 不会自己跑 —— 本类也没有 `Awake`）");
            if (p == null) return;
            p.Show(new Vector2(900f, 500f), null);
            T(p.GlowDrawn, "A4b ★ Menu 档画了 `Glow (1)`（原版 `TutorialPointerMenu` 有这一件）");
            if (!p.GlowDrawn) return;   // 图缺 / 建失败已由 A3/A4b 报过，后面读的是 -1，不值得再叠三条假红

            N(p.GlowScaleNow, 0.24f, 1e-3f, "A4c 起手 scaling = 0.24（clip t=0）");
            N(p.GlowAlphaNow, 0.5f, 1e-3f, "A4d 起手 alpha = 0.5（clip t=0）");

            p.Tick(0.25f);
            N(p.GlowScaleNow, 1.68f, 1e-3f, "A4e ★ Tick(0.25) 后 scaling = 1.68｜🧨 把 `GlowTick` 从 `Tick` 里摘掉 ⇒ 停在 0.24 红");
            N(p.GlowAlphaNow, 0.375f, 1e-3f, "A4f ★ Tick(0.25) 后 alpha = 0.375｜🧨 只写 `_fade` 不乘脉冲 ⇒ 红");

            p.Tick(0.25f);
            N(p.GlowScaleNow, 3.12f, 1e-3f, "A4g Tick(0.5) 后 scaling = 3.12");
            N(p.GlowAlphaNow, 0.25f, 1e-3f, "A4h Tick(0.5) 后 alpha = 0.25");

            p.Tick(0.5f);
            N(p.GlowScaleNow, 0.24f, 1e-3f, "A4i 满一圈（1.0 s）⇒ **直接回 0.24**（原版 Loop 回绕，不插值）");
            N(p.GlowAlphaNow, 0.5f, 1e-3f, "A4j 满一圈 ⇒ alpha 回 0.5");

            // 整组 fade 与脉冲**相乘**（战役页滚动淡出那一路：`SetFade`）
            p.SetFade(0.5f);
            N(p.GlowAlphaNow, 0.25f, 1e-3f, "A4k `SetFade(0.5)` ⇒ glow alpha = 脉冲 0.5 × fade 0.5 = 0.25");
            p.SetFade(1f);
        }
        finally
        {
            if (p != null) Object.DestroyImmediate(p.gameObject);
            Object.DestroyImmediate(root);
        }

        // ---- A5：Wildcards 档也有 `Glow (1)`（原版那份 prefab 现读有）----
        var root2 = new GameObject("B107R3d_ROOT_W");
        TutorialPointerUi w = null;
        try
        {
            w = TutorialPointerUi.Create(root2.transform, TutorialPointerUi.Tier.Wildcards);
            if (w != null)
            {
                w.Show(new Vector2(900f, 500f), null);
                T(w.GlowDrawn, "A5 ★ Wildcards 档也画 `Glow (1)`（原版 `TutorialPointerWildcards` 里同样有这件）"
                             + "｜🧨 把 `if (_tier == Tier.Menu || _tier == Tier.Wildcards)` 改回只有 Menu ⇒ 本行红");
                if (w.GlowDrawn) N(w.GlowScaleNow, 0.24f, 1e-3f, "A5b Wildcards 档起手 scaling 与 Menu 同（同一条 clip）");
            }
            else T(false, "A5a **前提**：Wildcards 档的指针建得出来");
        }
        finally
        {
            if (w != null) Object.DestroyImmediate(w.gameObject);
            Object.DestroyImmediate(root2);
        }

        // ---- A6：Base 档**没有** `Glow (1)`（原版那份 prefab 现读只有 3 个子件）----
        var root3 = new GameObject("B107R3d_ROOT_B");
        TutorialPointerUi b = null;
        try
        {
            b = TutorialPointerUi.Create(root3.transform, TutorialPointerUi.Tier.Base);
            if (b != null)
            {
                b.Show(new Vector2(900f, 500f), null);
                T(!b.GlowDrawn, "A6 Base 档**不画** `Glow (1)`（原版 `TutorialPointerBase` 没有这一件）"
                              + "｜🧨 把 Tier 判断放宽成「所有档」⇒ 本行红");
            }
            else T(false, "A6a **前提**：Base 档的指针建得出来");
        }
        finally
        {
            if (b != null) Object.DestroyImmediate(b.gameObject);
            Object.DestroyImmediate(root3);
        }
    }

    // ==========================================================================================
    //  B 段 —— U3：两条 GUID 消费 clip 的 bridge 读口
    // ==========================================================================================
    static void SectionB_BridgeEvidence()
    {
        // B0：数据侧先确认「这两条就是要证的那两条」（名字 + 已重打进哪只包）
        var daDoc = B107MiscClips.Find("Dark Angels Void Combat animations");
        var orbitDoc = B107MiscClips.Find("LightAnimationOrbit");
        T(daDoc != null && orbitDoc != null,
          "B0 **前提**：`misc_clips.json` 里两条 GUID 消费 clip 都在（`Dark Angels Void Combat animations` / `LightAnimationOrbit`）");
        if (daDoc != null)
        {
            C(daDoc.alreadyRepacked, "wf_prefabs_extra.bundle",
              "B0a `Dark Angels Void Combat animations` 记录的重打包目标 = `wf_prefabs_extra.bundle`（桥取的就是这只）");
            N(daDoc.length, 12f, 1e-3f, "B0b 数据备份里它的长度 = 12.0 s（Loop）");
            C(daDoc.wrapMode, "Loop", "B0c 回绕档 = Loop");
        }
        if (orbitDoc != null)
        {
            C(orbitDoc.alreadyRepacked, "wf_prefabs_extra.bundle",
              "B0d `LightAnimationOrbit` 记录的重打包目标 = `wf_prefabs_extra.bundle`");
            N(orbitDoc.length, 3f, 1e-3f, "B0e 数据备份里它的长度 = 3.0 s（Once）");
            C(orbitDoc.wrapMode, "Once", "B0f 回绕档 = Once");
        }

        // B1：**生产读口**真的取得到（裸名是容器键；`_verify_prefab_bundle.py [2·b]` 三条别名都核过）
        var da = WarpforgeAnimatorBridge.TryGetClip("Dark Angels Void Combat animations");
        T(da != null, "B1 ★ `WarpforgeAnimatorBridge.TryGetClip(\"Dark Angels Void Combat animations\")` **取得到**"
                    + "（= 运行时那一路真能把原件拿到；取不到 ⇒ 先查 `StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle`）"
                    + "｜🧨 把该 clip 的容器别名从重打包里去掉 ⇒ 本行红");
        if (da != null)
        {
            C(da.name, "Dark Angels Void Combat animations", "B1b 取回的 clip 名字一致");
            N(da.length, 12f, 1e-3f, "B1c 取回的 clip 长度 = 12.0 s");
            C(da.wrapMode, WrapMode.Loop, "B1d 取回的 clip 回绕档 = Loop");
        }

        var orb = WarpforgeAnimatorBridge.TryGetClip("LightAnimationOrbit");
        T(orb != null, "B2 ★ `TryGetClip(\"LightAnimationOrbit\")` **取得到**"
                     + "（⚠️ 它**配不上**是原版自己的事 —— `filterCode` 差一个 `al`，见 `ScenarioAnimationBlend` 类注；⛔ 别为了让它播去改数据）");
        if (orb != null)
        {
            C(orb.name, "LightAnimationOrbit", "B2b 取回的 clip 名字一致");
            N(orb.length, 3f, 1e-3f, "B2c 取回的 clip 长度 = 3.0 s");
        }
    }
}
