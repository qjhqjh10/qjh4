// B108_A13_R3_Assert.cs — 执行代理 `B108_A13`（第一百零八会话 · `判据/B107_资源侧施工单_v2.md` §2·R3-02）
//
// ============================ 先说结论：R3-02 四件里三件早就在仓库里了 ============================
//  ⛔ 本文件**不重复**那三件（重复 = 两处写同一条规则）。逐条证据（行号是 2026-10-31 本代理现读）：
//
//   **G3-a `Card Display Open`**（0.3166667 s）
//     → `Editor/B107_R3c_Assert.cs` A 段（`B107R5`，提交 `93e37dc6`）：A1 断 `CardFan.OpenTime` == clip `length`；
//       A5[0..4] 逐槽逐轴断 `CardFan.X/Y/Rot/Wpx` == clip 末键（旋转由四元数**现算** `2·atan2(z,w)`，
//       缩放比 = 末键(i)/末键(0)）⇒ 施工单要的那两条**都在**。⚠️ 施工单写「`Core/CardFan.cs:77` 只差一条断言」
//       当时是对的；断言在**同一个会话稍晚**（14:06→15:15）才落，施工单没回头改。
//
//   **G3-b `SearchingOpponentCog`**（10.016666 s）
//     → 同一个文件 B 段：B1 断 `SearchingMatchPopup.CogLoop` == clip `length`；B2 断 **37 键** + 首尾
//       （t=0/Z=0° · t=`CogLoop`/Z=360°）；B4/B5 还加了「键时刻逐键」与「每段四分点走直线」两条灭自证。
//
//   **G5 教程指针脉冲 `Tutorial Pointer Menu`**（1.0 s Loop）
//     → 实现：`Shell/TutorialPointerUi.cs:589-641`（`GlowLoop` / `GlowScaleFrom=0.24` / `GlowScaleTo=6.0` /
//       `GlowAlphaFrom=0.5` / `GlowAlphaTo=0` + `GlowScaleAt/GlowAlphaAt/GlowTick/GlowApply`，接在 `Tick` 的 `:209`）。
//     → 断言：`Editor/B107_R3d_Assert.cs` A 段（A1 常量 == clip 两键；A4 逐拍 0.25/0.5/1.0 s；
//       A4k `SetFade` 与脉冲**相乘**；A5 Wildcards 档也有 `Glow (1)`；A6 Base 档没有）。
//
//   **G5 同族证据 `Dark Angels Void Combat animations` / `LightAnimationOrbit`**
//     → `Editor/B107_R3d_Assert.cs` B 段：走**生产读口** `WarpforgeAnimatorBridge.TryGetClip(<裸名>)`，
//       断取得到 + 名字/长度/回绕档（12 s Loop / 3 s Once）。
//     → 「真的挂到组件上」那一跳 = `Editor/BattleScene.cs` A201（`:5463-5570`）：`clipLoader`（= 生产那份
//       `ScenarioBlendableFactory.AnimationClipByGuid`）→ `AddClip` → `GetClip` 取回，并且**现读**断言
//       「`VoidCombatAnimations` 这一对配得上」+「另一颗 `LightAnimationOrbital` 在 SO 侧一个都配不上
//       （差一个 `al`）⇒ 原版自己配不上、照抄」—— 这正是铁律 11 例外①的**封账**，已经写成断言了。
//
// ============================ 本文件补的那一件：**G4 引原件的验收面** ============================
//  G4（`Booster Open Standard` 2.3667 s / `Booster Open Sororitas` 2.0 s）的**实现早就在**：
//    · `Shell/BoosterPackOpenWindow.cs:1508` `BuildVisualItemLayer` → `BoosterPackAssets.TryGetPrefab` →
//      `Instantiate` 17 棵**原版 prefab**（`StreamingAssets/WarpforgeVFX/wf_boosters_extra.bundle`）；
//    · 炸开 / 粒子 / 阵营字淡出 / **t = 2.0 的 `StartCardAnimationEvent`** 全在 prefab 自带那条 legacy clip 里
//      （`Animation.m_PlayAutomatically = true`）⇒ 实例化即播，⛔ 我们没有第二份手写补间。
//    · 运行时那一跳已有 `Editor/B98A5_Assert.cs` §4 核过 —— **但它只核了一棵**（Leviathan 那一包）。
//
//  ⇒ **没被任何断言覆盖的那一格 = 「引原件」到底引对了没有**：17 棵 prefab 各自绑的是哪条 clip、
//    长度对不对、是不是空壳。本文件把它补上，判据 = `Resources/misc_clips.json` 的 `owners[]`
//    （= 原版 `Animation.m_Animations[0]` 的逐实例实读，⛔ 不是我挑的）。
//
//  🔴 **这一格为什么必须核**：本工程有一条硬坑 —— legacy `AnimationClip` 从 bundle 搬进工程会变**空壳**
//    （`length` 砸成 1、曲线/事件全丢，见 `WarpforgeAnimatorBridge.cs` 文件头）。「实例化原版 prefab」
//    之所以没踩，是因为**没有搬**、直接在包里用；但一旦哪天有人为了「统一管理」把 clip 拷进工程，
//    这条断言会在**第一秒**红，而不是等有人发现包不炸。
//
// ============================ 判据出处 ============================
//  · 数据备份 = `MyGame/Assets/CardPresentation/Resources/misc_clips.json`（生成器 `工具/gen_misc_clips.py`）
//    —— 本文件只读它，⛔ 不改。
//  · 两条 clip 的源件 = `bundle_boosterpacks_assets_all/AnimationClip/AnimationClip_7302051762796698856.json`
//    （`Booster Open Standard`）与 `…_5156805757413551419.json`（`Booster Open Sororitas`）。
//  · 17 棵 prefab 的取件口 = `CardPresentation/Core/BoosterPackAssets.cs`（`PrefabNames` 17 条 ·
//    `TryGetPrefab` / `TryGetPrefabForArmy`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A4：把 `BoosterPackAssets.PrefabNames` 里任一条改名 / 删一条 ⇒ A4/A5 红（数据表不吃我们这边改）。
//  · B*：把某棵 prefab 的 `Animation.m_Animations[0]` 换成别的 clip（或把 clip 拷成工程资产再绑）
//    ⇒ 那一棵的「clip 名 / 长度」红；**只有**拷成资产时长度会砸成 1 ⇒ 长度那半条独立抓得到。
//  · C2/C4/C5：把 clip 换成一个「名字对、长度对、但曲线是空」的替身 ⇒ 曲线那几条红
//    （名字+长度仍然绿 ⇒ 它们各自是独立的一面）。
//  · E*：把 `TryGetPrefabForArmy` 的某一条分支写错（例：`sororitas` 落到 Standard）⇒ E 那一行红。
//  · A6：把两条 clip 当成同一条（同一个 `AnimationClip` 实例）⇒ A6 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  纯资产断言 —— **不建场景、不建窗、不建相机、不碰存档**（只从 StreamingAssets 的包里读 prefab）。
//  🔴 **推荐宿主 = `Editor/ShellScene.cs`（9 s，最便宜）的第 4812 行**（`B107R3c_Assert` / `B107R3d_Assert`
//     那一行）**同一物理行末尾追加**（⛔ 别加行）：
//        B108A13R3_Assert.Run(); _sink.Pass += B108A13R3_Assert.Pass; _sink.Fail += B108A13R3_Assert.Fail;
//  🔴 **必须追加在【行尾】** —— 那一行（本代理现读）**没有** `//` 注释，安全；
//     ⚠️ 但 `ShellScene.cs:11093` 与 `ShopScene.cs:6515` 那两条聚合行**行尾各挂着 `//` 注释**，
//     往那两条行尾追加会**落进注释里、静默不执行**（2026-10-30 一天踩 6 次的那个坑）⇒ 要接那边必须插在 `//` **之前**。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEditor;
using UnityEngine;

/// <summary>执行代理 `B108_A13`：R3-02 **G4**「引原件」的验收面 —— 17 棵原版卡包 prefab 逐棵的
/// prefab↔clip 配对 / 长度 / 事件 / 曲线非空壳。见文件头。</summary>
public static class B108A13R3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B108A13] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    //  `Resources/misc_clips.json` 的最小只读解析
    //
    //  ⚠️ **为什么不复用 `B107MiscClips`**（同目录、同一份 json）：它的 DTO 没有 `owners`（它只服务 G3 的
    //     「常量 == clip 值」），而本文件要的正是 **`owners[]` 的 prefab→clip 配对**。
    //     ⛔ 那个文件是 `B107_R5` 的落点、不在本代理白名单里 ⇒ **不改别人的文件**，自带一份最小解析。
    //     （`Resources.Load<TextAsset>` 本身有缓存，两次解析同一份 233 KB 文本没有实际代价。）
    // ══════════════════════════════════════════════════════════════════════════════════════════
    [Serializable] public sealed class Key { public float t; public float[] v; }
    [Serializable] public sealed class Curve { public string type; public string property; public string path; public Key[] keys; }
    [Serializable] public sealed class Owner { public string go; public string animation; public bool playAutomatically; }
    [Serializable] public sealed class Clip
    {
        public string name; public float length; public string wrapMode; public bool legacy;
        public int keysTotal; public bool orphan; public string alreadyRepacked;
        public Owner[] owners; public Curve[] curves;
    }
    [Serializable] public sealed class Doc { public int schema; public Clip[] clips; }

    static Doc _doc;
    static bool _tried;

    static Doc LoadClips()
    {
        if (_tried) return _doc;
        _tried = true;
        var ta = Resources.Load<TextAsset>("misc_clips");
        if (ta == null)
        {
            Debug.LogError("[B108A13] 🔴 `Resources/misc_clips.json` 取不到 ⇒ 本轮 G4 的配对断言**一条都跑不了**。"
                         + "先跑 `python -I -X utf8 d:/4/Unity/工具/gen_misc_clips.py --write`");
            return null;
        }
        try { _doc = JsonUtility.FromJson<Doc>(ta.text); }
        catch (Exception e)
        {
            Debug.LogError("[B108A13] 🔴 `misc_clips.json` 解析失败：" + e.GetType().Name + ": " + e.Message);
            _doc = null;
        }
        return _doc;
    }

    static Clip Find(string name)
    {
        var d = LoadClips();
        if (d == null || d.clips == null) return null;
        for (int i = 0; i < d.clips.Length; i++)
            if (d.clips[i] != null && d.clips[i].name == name) return d.clips[i];
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    //  入口
    // ══════════════════════════════════════════════════════════════════════════════════════════
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        MenuCheck.Section(_sink, "B108_A13 · R3-02 G4：卡包 `Booster Open *` 引原件验收（17 棵 prefab ↔ clip）");

        Clip std, sor;
        if (!SectionA_Data(out std, out sor)) return;      // 前提不成立已逐条报红，别叠假红
        SectionB_Pairing(std, sor);
        SectionC_ClipContent(std, sor);
        SectionD_ArmyMapping();

        Debug.Log(_sink.P + "B108A13 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    //  A 段 —— 数据前提 + 取件口表
    // ══════════════════════════════════════════════════════════════════════════════════════════
    static bool SectionA_Data(out Clip std, out Clip sor)
    {
        std = sor = null;
        MenuCheck.Section(_sink, "A 数据前提（`misc_clips.json` 的 11 条里那两条）");

        var doc = LoadClips();
        T(doc != null && doc.clips != null && doc.clips.Length == 11,
          "A0 **前提**：`misc_clips.json` 解析得出来且是 11 条（实得 "
          + (doc == null || doc.clips == null ? "**解析失败 / 空表**" : doc.clips.Length.ToString()) + " 条）");

        std = Find("Booster Open Standard");
        sor = Find("Booster Open Sororitas");
        T(std != null, "A1a **前提**：数据表里有 `Booster Open Standard`");
        T(sor != null, "A1b **前提**：数据表里有 `Booster Open Sororitas`");
        if (std == null || sor == null) return false;

        // 锚：这两个长度**不取自本文件要核的任何一边**（施工单 §2·R3-02 专表 + 源 clip 实读都写着同一对数）。
        N(std.length, 2.3666666f, 1e-3f, "A2a `Booster Open Standard` 长度 = **2.3667 s**（原版 `m_StopTime`）");
        N(sor.length, 2.0f, 1e-3f, "A2b `Booster Open Sororitas` 长度 = **2.0 s**");
        C(std.owners == null ? -1 : std.owners.Length, 16, "A3a `Booster Open Standard` 的原版实例数 = **16** 棵 prefab");
        C(sor.owners == null ? -1 : sor.owners.Length, 1, "A3b `Booster Open Sororitas` 的原版实例数 = **1** 棵（`Booster Pack Standard Sororitas`）");

        C(BoosterPackAssets.PrefabNames == null ? -1 : BoosterPackAssets.PrefabNames.Length, 17,
          "A4 取件口 `BoosterPackAssets.PrefabNames` = **17** 棵（容器 38 条 = 17 prefab + 2 clip 里的那一半）");
        C(BoosterPackAssets.StandardPrefab, "Booster Pack Standard",
          "A4b `StandardPrefab` 常量 = `Booster Pack Standard`（原版通用那一棵）");

        // A5：两侧**集合相等** —— 数据表 owners 的 `go` 全集 ↔ 取件口的 `PrefabNames` 全集。
        //     逐条报「表里有、取件口没有」与「取件口有、表里没有」，⛔ 不只报个数量相等（数量相等掩盖一进一出）。
        var fromData = new HashSet<string>(StringComparer.Ordinal);
        if (std.owners != null) for (int i = 0; i < std.owners.Length; i++) if (std.owners[i] != null && std.owners[i].go != null) fromData.Add(std.owners[i].go);
        if (sor.owners != null) for (int i = 0; i < sor.owners.Length; i++) if (sor.owners[i] != null && sor.owners[i].go != null) fromData.Add(sor.owners[i].go);
        var fromCode = new HashSet<string>(StringComparer.Ordinal);
        if (BoosterPackAssets.PrefabNames != null) for (int i = 0; i < BoosterPackAssets.PrefabNames.Length; i++) fromCode.Add(BoosterPackAssets.PrefabNames[i]);

        string missing = "", extra = "";
        foreach (var n in fromData) if (!fromCode.Contains(n)) missing += "`" + n + "` ";
        foreach (var n in fromCode) if (!fromData.Contains(n)) extra += "`" + n + "` ";
        C(fromData.Count, 17, "A5a 数据表里这两条 clip 的原版实例名去重后 = **17**（实得 " + fromData.Count + "）");
        T(missing.Length == 0, "A5b ★ 数据表里有、取件口 `PrefabNames` 里**没有**的 prefab = 0（实得：" + (missing.Length == 0 ? "0" : missing) + "）");
        T(extra.Length == 0, "A5c ★ 取件口里有、数据表里**没有**的 prefab = 0（实得：" + (extra.Length == 0 ? "0" : extra) + "）");
        return fromData.Count > 0 && fromCode.Count > 0;
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    //  B 段 —— 17 棵逐棵：prefab 根部那颗 `Animation` 绑的是不是原版那条 clip
    // ══════════════════════════════════════════════════════════════════════════════════════════
    static void SectionB_Pairing(Clip std, Clip sor)
    {
        MenuCheck.Section(_sink, "B 17 棵 prefab 逐棵：根部 `Animation.playAutomatically` / `clip` 名 / 长度 / legacy");

        // 期望表：prefab 名 → 它原版绑的那条 clip（**从 `misc_clips.json` 的 owners 现读**，⛔ 不是我抄一份）
        var expect = new Dictionary<string, Clip>(StringComparer.Ordinal);
        if (std.owners != null) for (int i = 0; i < std.owners.Length; i++) { var o = std.owners[i]; if (o != null && !string.IsNullOrEmpty(o.go)) expect[o.go] = std; }
        if (sor.owners != null) for (int i = 0; i < sor.owners.Length; i++) { var o = sor.owners[i]; if (o != null && !string.IsNullOrEmpty(o.go)) expect[o.go] = sor; }

        // 🔴 自检前先清缓存：别的宿主 / 两阶段批处理可能 `UnloadAllAssetBundles` 过，句柄死了但 `_tried` 还是真
        //    ⇒ `TryEnsureLoaded` 会**不再重试**（`Reset` 的注释里写着「只给自检 / 编辑器工具用」，就是这一档）。
        BoosterPackAssets.Reset();

        T(BoosterPackAssets.PrefabNames != null && BoosterPackAssets.PrefabNames.Length > 0,
          "B0 **前提**：取件口表非空（空 ⇒ 本段一条都跑不了）");
        if (BoosterPackAssets.PrefabNames == null) return;

        int got = 0;
        for (int i = 0; i < BoosterPackAssets.PrefabNames.Length; i++)
        {
            string name = BoosterPackAssets.PrefabNames[i];
            Clip want;
            if (!expect.TryGetValue(name, out want))
            {
                T(false, "B[" + name + "] **前提**：数据表里查得到这棵 prefab 该绑哪条 clip（查不到 ⇒ 本棵没跑）");
                continue;
            }

            GameObject pf = null;
            bool ok = BoosterPackAssets.TryGetPrefab(name, out pf) && pf != null;
            var anim = ok ? pf.GetComponent<Animation>() : null;
            T(ok && anim != null,
              "B[" + name + "] 原版 prefab 取得回来（包在 `StreamingAssets/WarpforgeVFX/wf_boosters_extra.bundle`）"
              + "且**根部**有 `Animation` 组件（实得 prefab " + (ok ? "✓" : "✗") + " · Animation "
              + (anim != null ? "✓" : "✗") + "）");
            if (anim == null) continue;

            var runtimeClip = anim.clip;
            string gotName = runtimeClip != null ? runtimeClip.name : "<null>";
            float gotLen = runtimeClip != null ? runtimeClip.length : float.NaN;
            bool gotLegacy = runtimeClip != null && runtimeClip.legacy;
            bool pairOk = anim.playAutomatically
                          && runtimeClip != null
                          && gotName == want.name
                          && Mathf.Abs(gotLen - want.length) <= 1e-3f
                          && gotLegacy;
            T(pairOk,
              "★ B[" + name + "] `playAutomatically=true` 且绑的是原版那条 clip"
              + " —— 期望 `" + want.name + "` / " + want.length.ToString("F4") + " s / legacy"
              + "；实得 auto=" + (anim.playAutomatically ? "true" : "**false**")
              + " · clip=`" + gotName + "`"
              + " · len=" + (float.IsNaN(gotLen) ? "NaN" : gotLen.ToString("F4")) + " s"
              + " · legacy=" + (gotLegacy ? "true" : "**false**")
              + "｜🧨 换成别的 clip / 把 clip 拷成工程资产（空壳 ⇒ 长度砸成 1）⇒ 本行红");
            if (pairOk) got++;
        }

        C(got, BoosterPackAssets.PrefabNames.Length,
          "B★ 17 棵**逐棵**都对上了（这一条是汇总；逐棵那 17 行的失败条自己会说清是哪一棵）");
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    //  C 段 —— 两条 clip 自己的内容（不是空壳）
    // ══════════════════════════════════════════════════════════════════════════════════════════
    static void SectionC_ClipContent(Clip std, Clip sor)
    {
        MenuCheck.Section(_sink, "C 两条 clip 的内容：炸包曲线的绑定 + `t=2.0` 那个 AnimationEvent");

        AnimationClip a = null, b = null;
        GameObject pfA = null, pfB = null;
        if (BoosterPackAssets.TryGetPrefab("Booster Pack Standard", out pfA) && pfA != null)
        {
            var an = pfA.GetComponent<Animation>();
            if (an != null) a = an.clip;
        }
        if (BoosterPackAssets.TryGetPrefab("Booster Pack Standard Sororitas", out pfB) && pfB != null)
        {
            var an = pfB.GetComponent<Animation>();
            if (an != null) b = an.clip;
        }
        T(a != null, "C0a **前提**：`Booster Pack Standard` 那条 clip 取得到");
        T(b != null, "C0b **前提**：`Booster Pack Standard Sororitas` 那条 clip 取得到");
        if (a == null || b == null) return;

        // C1：两条是**两个不同实例**且长度不同 —— 挡「所有 prefab 共用一条」这种假通过。
        T(!ReferenceEquals(a, b) && Mathf.Abs(a.length - b.length) > 0.3f,
          "C1 两条 clip 是不同实例、长度也不同（`" + a.name + "` " + a.length.ToString("F4")
          + " s vs `" + b.name + "` " + b.length.ToString("F4") + " s）");

        // C2：`StartCardAnimationEvent` 在 t = 2.0（= 原版「包炸完 → 开包动画开始」那一拍）
        EventFacts(a, "C2a");
        EventFacts(b, "C2b");

        // C3/C4/C5：曲线绑定 —— 「名字对、长度对、内容空」这一档只能靠它抓
        var ba = AnimationUtility.GetCurveBindings(a);
        var bb = AnimationUtility.GetCurveBindings(b);
        T(ba != null && ba.Length > 0,
          "C3a ★ 原版 clip 的曲线绑定非空（`AnimationUtility.GetCurveBindings` 实得 " + (ba == null ? -1 : ba.Length) + " 条）"
          + " —— 🧨 拷成工程资产的空壳 clip ⇒ 这里 = 0");
        T(bb != null && bb.Length > 0,
          "C3b ★ Sororitas 那条同样非空（实得 " + (bb == null ? -1 : bb.Length) + " 条）");
        T(HasProperty(ba, "_ExplodeRange"), "C4 ★ 绑定里有 `material._ExplodeRange`（炸开半径：1 → 0）");
        T(HasProperty(ba, "_InflateDistance"), "C5 ★ 绑定里有 `material._InflateDistance`（鼓起：0 → 1.03）");
        T(HasProperty(ba, "m_fontColor.a"), "C6 ★ 绑定里有 `m_fontColor.a`（`Army Text` / `Army Text 2` 的阵营字淡出）");
        T(HasProperty(ba, "m_IsActive"), "C7 ★ 绑定里有 `m_IsActive`（`Booster Pack Particles` / `ParticleExplosion` 的开关）");

        // C8：数据表逐条对回**运行时对象**（反方向：表里有的曲线，运行时那条也得有）
        CheckCurvesAgainstData(a, std, "C8");
        CheckCurvesAgainstData(b, sor, "C8s");
    }

    static void EventFacts(AnimationClip clip, string tag)
    {
        var evs = clip != null ? clip.events : null;
        int n = evs == null ? -1 : evs.Length;
        bool found = false;
        float at = -1f;
        if (evs != null)
            for (int i = 0; i < evs.Length; i++)
                if (evs[i] != null && evs[i].functionName == "StartCardAnimationEvent") { found = true; at = evs[i].time; }
        T(found && n >= 1, tag + " ★ `" + clip.name + "` 有 AnimationEvent `StartCardAnimationEvent`（实得 " + n + " 个事件）"
                           + " —— 它就是转调 `BoosterPackOpenWindow.StartOpenAnimations()` 那一跳");
        N(at, 2.0f, 1e-3f, tag + " ★ 它在 **t = 2.0 s**（原版「包炸完 → 开包动画开始」那一拍）");
    }

    static bool HasProperty(EditorCurveBinding[] bs, string needle)
    {
        if (bs == null) return false;
        for (int i = 0; i < bs.Length; i++)
            if (bs[i].propertyName != null && bs[i].propertyName.IndexOf(needle, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    /// <summary>数据表里记的每条曲线（`property` + `path`）在运行时那条 clip 上都得有对应绑定。
    /// <para>🔴 **两边的字段名口径不同**：`misc_clips.json` 抄的是**序列化字段名**（`m_LocalRotation` / `m_LocalScale`
    /// / `m_AnchoredPosition.x`），而 `AnimationUtility.GetCurveBindings` 报的是**编辑器属性名**，且
    /// `m_RotationCurves` / `m_ScaleCurves` / `m_PositionCurves` 这三类会被**拆成分量**（`localRotation.x` 等 4 条、
    /// `localScale.x` 等 3 条）。逐字比会**成片假红** ⇒ 结构曲线按「分量前缀」认，其余（`m_FloatCurves`：
    /// `material._ExplodeRange` / `m_IsActive` / `m_fontColor.a` …）逐字比。</para></summary>
    static void CheckCurvesAgainstData(AnimationClip clip, Clip data, string tag)
    {
        if (data.curves == null || data.curves.Length == 0) return;
        var bs = AnimationUtility.GetCurveBindings(clip);
        string miss = "";
        int hit = 0;
        for (int i = 0; i < data.curves.Length; i++)
        {
            var cu = data.curves[i];
            if (cu == null || string.IsNullOrEmpty(cu.property)) continue;
            bool ok = false;
            for (int j = 0; j < bs.Length && !ok; j++) ok = PropMatches(bs[j], cu, bs[j].path);
            if (ok) hit++; else miss += "`" + cu.property + "`@" + (cu.path ?? "") + " ";
        }
        C(hit, data.curves.Length,
          tag + " ★ 数据表里 `" + data.name + "` 记录的 " + data.curves.Length + " 条曲线，运行时对象上**逐条都在**"
          + "（缺：" + (miss.Length == 0 ? "0" : miss) + "）");
    }

    /// <summary>一条运行时绑定 `b` 是不是数据表里那条 `cu`（口径差见 `CheckCurvesAgainstData` 的注释）。</summary>
    static bool PropMatches(EditorCurveBinding b, Curve cu, string bindingPath)
    {
        if ((bindingPath ?? "") != (cu.path ?? "")) return false;
        if (b.propertyName == cu.property) return true;
        if (cu.type != "Transform") return false;
        string p = b.propertyName ?? "";
        string cp = cu.property ?? "";
        // 🔴 2026-10-31（B108 收尾）：Unity 对 Transform 的绑定名有两种形态（带/不带 `m_`，旋转还可能是 EulerRaw）
        if (cp == "m_LocalRotation" || cp == "localRotation")
            return p.StartsWith("m_LocalRotation.", StringComparison.Ordinal)
                || p.StartsWith("localRotation.", StringComparison.Ordinal)
                || p.StartsWith("m_LocalEulerAnglesRaw.", StringComparison.Ordinal)
                || p.StartsWith("localEulerAnglesRaw.", StringComparison.Ordinal);
        if (cp == "m_LocalScale" || cp == "localScale")
            return p.StartsWith("m_LocalScale.", StringComparison.Ordinal)
                || p.StartsWith("localScale.", StringComparison.Ordinal);
        if (cp == "m_LocalPosition" || cp == "localPosition")
            return p.StartsWith("m_LocalPosition.", StringComparison.Ordinal)
                || p.StartsWith("localPosition.", StringComparison.Ordinal);
        if (cp == "m_LocalEulerAnglesRaw" || cp == "localEulerAnglesRaw")
            return p.StartsWith("m_LocalEulerAnglesRaw.", StringComparison.Ordinal)
                || p.StartsWith("localEulerAnglesRaw.", StringComparison.Ordinal);
        return false;
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════
    //  D 段 —— 阵营 → prefab 的取件口真的覆盖到 17 棵里
    // ══════════════════════════════════════════════════════════════════════════════════════════
    static void SectionD_ArmyMapping()
    {
        MenuCheck.Section(_sink, "D 阵营 → prefab（`BoosterPackAssets.TryGetPrefabForArmy`）");

        var known = new HashSet<string>(StringComparer.Ordinal);
        if (BoosterPackAssets.PrefabNames != null)
            for (int i = 0; i < BoosterPackAssets.PrefabNames.Length; i++) known.Add(BoosterPackAssets.PrefabNames[i]);

        var armies = ForgeData.Armies;
        T(armies != null && armies.Length > 0, "D0 **前提**：`ForgeData.Armies` 非空");
        if (armies == null) return;

        int ok = 0; string bad = "";
        for (int i = 0; i < armies.Length; i++)
        {
            string n;
            bool got = BoosterPackAssets.TryGetPrefabForArmy(armies[i], out n);
            bool inTable = got && n != null && known.Contains(n);
            if (inTable) ok++;
            else bad += "`" + armies[i] + "`→`" + (n ?? "<null>") + "` ";
        }
        C(ok, armies.Length,
          "D★ `ForgeData.Armies` 的 " + armies.Length + " 个阵营**逐个**都落到 `PrefabNames` 里的某一棵"
          + "（落不上的：" + (bad.Length == 0 ? "0" : bad) + "）"
          + "｜🧨 把 `TryGetPrefabForArmy` 里某条分支写错（例 `sororitas` 落到 Standard）⇒ 本行红");

        string neutral;
        bool nOk = BoosterPackAssets.TryGetPrefabForArmy("Neutral", out neutral);
        C(nOk ? neutral : "<null>", BoosterPackAssets.StandardPrefab,
          "D1 `Neutral` ⇒ `StandardPrefab`（通用那一棵）");

        // D2：17 棵里「13 阵营 + Neutral、其余是额外档」—— 把 extra 那 4 棵点出来，防「表悄悄变了没人知道」。
        //     ⚠️ 这 4 棵是**原版真有**、但我们的阵营 id 里没有对应项的（Expansion 两档 + All Armies + Standard）。
        string[] extras =
        {
            "Booster Pack Standard",
            "Booster Pack All Armies Variant",
            "Booster Pack Standard Space Marines Ultramarines Expansion 1",
            "Booster Pack Standard Orks Goff Expansion",
        };
        int extraOk = 0;
        for (int i = 0; i < extras.Length; i++) if (known.Contains(extras[i])) extraOk++;
        C(extraOk, extras.Length,
          "D2 4 棵「阵营之外的额外档」都在 `PrefabNames` 里（现读 " + extraOk + "/" + extras.Length + "）："
          + "通用 / 全阵营变体 / Ultramarines Expansion 1 / Goff Expansion");
    }
}
