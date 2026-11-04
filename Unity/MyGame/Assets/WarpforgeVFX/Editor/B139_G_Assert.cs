// B139_G_Assert.cs — 特效生命周期 / 粒子运行时 的断言（包 G · 2026-11-04）
//
// 覆盖面 = `报告/B138_审计汇总_问题清单.md` 里包 G 的 9 条：
//   12-01 特效吃父/卡旋转（原版不吃）· 12-02 `doParentRelation` 用父子替代了 `ParentConstraint`
//   12-03 `unParentAtStart` 之后仍被「持有者离场」收掉 · 12-05 寿命那一帧的音效被吞
//   13-01 `DestroyByTime` 作用域（子节点收掉整件）· 13-03 `SyncParticleSeeds` 超集
//   13-04 `SubEmitterManualTrigger` 超集 · 13-05 `prewarm` 被 `Clear` 清掉 · 13-07 `ParticleEmitOnEnable` 两份
//
// 判据（⛔ 别在这里重查，出处已写进各条断言文案）：
//   `报告/B135_审计_特效生命周期与排序.md` A7/B5/B8/C7 · `报告/B135_审计_战斗粒子全模块.md` F1..F5 + §六·1
//   原版方法体：`d:/2/tools/decomp_full/{BattleManager__CreateAnim,AnimFXModuleScaleByTarget__Initialize,
//   AnimFXModuleTransformModifier__Initialize,AnimFXController__OnEnable,AnimFXController__Update,
//   DestroyByTime__Start,SyncParticleSeeds__Awake,SubEmitterManualTrigger__Update}.c`
//
// ⚠️ 期望值一律**独立**算（从 prefab 资产 / `effect_extras.json` 现读），⛔ 不从被测实现读回。
// 用法：`-executeMethod B139_G_Assert.Run`。与 `AnimFXCheck.Run` 同一套日志口径（`=== 合计：N 通过 / M 失败 ===`）。
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WarpforgeVFX;

public static class B139_G_Assert
{
    const string P = "B139G ";
    static int _pass, _fail;

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log(P + "  ✓ " + what); }
        else { _fail++; Debug.LogError(P + "  ✗ " + what); }
    }

    static void Done()
    {
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
    }

    public static void Run()
    {
        Debug.Log(P + "=== B139-G 特效生命周期 / 粒子运行时 断言 开始 ===");
        var lib = WarpforgeEffectLibrary.Instance;
        Check(lib != null, "效果库读得到（读不到 ⇒ 下面每条都是空跑）");
        if (lib == null) { Done(); return; }

        // 音效一律静音跑（`WFSoundPlayer.Enabled = false` 仍然记账 —— `Played` 照样涨）
        bool prevSnd = WFSoundPlayer.Enabled;
        WFSoundPlayer.Enabled = false;
        try
        {
            Check12_01_Rotation(lib);
            Check12_02_Constraint();
            Check12_03_UnParent(lib);
            Check12_05_SoundOnLifetimeFrame(lib);
            Check13_01_DestroyByTimeScope();
            Check13_03_SyncSeeds();
            Check13_04_SubEmitter();
            Check13_05_Prewarm(lib);
            Check13_07_EmitOnEnableDedupe();
        }
        finally { WFSoundPlayer.Enabled = prevSnd; }

        Done();
    }

    // ======================================================================
    //  12-01 · 特效世界旋转不吃父旋转（原版 `CreateAnim` 的第 3 个实参）
    // ======================================================================
    static void Check12_01_Rotation(WarpforgeEffectLibrary lib)
    {
        var e = lib.entries.FirstOrDefault(x => x != null && x.prefab != null);
        Check(e != null, "12-01（前提）：库里至少有一个带 prefab 的效果");
        if (e == null) return;

        var host = new GameObject("b139g_rot_host");
        host.transform.rotation = Quaternion.Euler(0f, 47f, 0f);
        try
        {
            var prefabRot = e.prefab.transform.rotation;

            WarpforgeEffectPlayer.ResetB139Diagnostics();
            var p = WarpforgeEffectPlayer.Play(e, host.transform, Vector3.zero, 1f, 5f,
                                              assignScale: false, layer: -1, worldRotationStays: true);
            Check(p != null, $"12-01：《{e.name}》播得起来");
            if (p != null)
            {
                Check(Quaternion.Angle(p.transform.rotation, prefabRot) < 0.5f,
                      $"12-01：`worldRotationStays: true` ⇒ 世界旋转 = **prefab 自身**（原版 `Instantiate(prefab, pos, prefab.localRotation, parent)`）"
                    + $"（差 {Quaternion.Angle(p.transform.rotation, prefabRot):F3}°）");
                var inherit = host.transform.rotation * e.prefab.transform.localRotation;
                Check(Quaternion.Angle(p.transform.rotation, inherit) > 10f,
                      "12-01：……而且与「父旋转 × prefab.local」**明显不同**（父转 47° ⇒ 若只差 0° 这条就没有分辨力）");
                Check(WarpforgeEffectPlayer.WorldRotationStaysApplied == 1,
                      "12-01：计数 `WorldRotationStaysApplied` = 1（🧨 删掉那行 `t.rotation = …` ⇒ 得 0 ⇒ 红）");
                p.Kill();
            }

            // 对照：默认（false）**必须**吃父旋转 —— 那是 `CardScript.PlayTriggerAnim`（两参 Instantiate）那条路，
            // ⛔ 别为了修 12-01 把默认改掉（状态框那一族要继承）。
            var p2 = WarpforgeEffectPlayer.Play(e, host.transform, Vector3.zero, 1f, 5f);
            if (p2 != null)
            {
                var inherit = host.transform.rotation * e.prefab.transform.localRotation;
                Check(Quaternion.Angle(p2.transform.rotation, inherit) < 0.5f,
                      $"12-01 对照：默认档仍吃父旋转（差 {Quaternion.Angle(p2.transform.rotation, inherit):F3}°）"
                    + " —— 🧨 把 `worldRotationStays` 的默认值改成 true ⇒ 本条红（会打断 `PlayTriggerAnim` 那条原版路）");
                p2.Kill();
            }
            else Check(false, "12-01 对照：默认档播不起来");
        }
        finally { Object.DestroyImmediate(host); }
    }

    // ======================================================================
    //  12-02 · `doParentRelation` = 约束（不是父子）：层级不变 · 偏移保持 · 不继承缩放 · 卡毁不连坐
    // ======================================================================
    static void Check12_02_Constraint()
    {
        var host = new GameObject("b139g_cstr_host");
        var psGo = new GameObject("ps");
        var acting = new GameObject("acting");
        var target = new GameObject("target");
        try
        {
            psGo.transform.SetParent(host.transform, false);
            var ps = psGo.AddComponent<ParticleSystem>();
            Check(ps != null, "12-02（前提）：夹具粒子系统建得出来");

            acting.transform.position = new Vector3(-3f, 0f, 0f);
            target.transform.position = new Vector3(2f, 0f, 0f);
            target.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            psGo.transform.position = new Vector3(2.3f, 0.2f, 0f);   // 相对锚点的初始偏移 ≠ 0

            var mod = host.AddComponent<WFModuleScaleByTarget>();
            mod.particleSystems = new[] { ps };
            mod.actingCard = acting.transform;
            mod.targetCard = target.transform;
            mod.doParentRelation = true;
            WFModuleScaleByTarget.CardResolver = null;               // 夹具直接把两张卡挂在模块上

            WFModuleScaleByTarget.ResetDiagnostics();
            mod.Initialize(null);

            Check(mod.ConstraintCount == 1,
                  $"12-02：`doParentRelation` ⇒ 挂了 **1 条约束**（实测 {mod.ConstraintCount}；"
                + "🧨 改回 `SetParent` ⇒ 得 0 ⇒ 红）");
            Check(psGo.transform.parent == host.transform,
                  "12-02：**层级不变**（原版 `ToggleParentingOptions` 是约束，不是父子 —— "
                + $"实测父 = `{(psGo.transform.parent == null ? "null" : psGo.transform.parent.name)}`）");
            Check(WFModuleScaleByTarget.ConstraintAnchorFallbacks == 1,
                  "12-02：`target` 不是 `CardView` ⇒ 退回卡根当约束源、并**出声计数**（原版走 `targetCard.GetEffectAnchor()`）");

            // ① 平移跟随、偏移保持（= `ParentConstraint` 的 offset 语义）
            Vector3 rel = psGo.transform.position - target.transform.position;
            target.transform.position += new Vector3(0f, 1f, 0f);
            int ticks0 = WFModuleScaleByTarget.ConstraintTicks;
            mod.ModuleTick(0.016f);
            Check(Vector3.Distance(psGo.transform.position, target.transform.position + rel) < 1e-3f,
                  $"12-02：锚点平移 ⇒ 粒子**跟着平移且偏移保持**（实测偏差 {Vector3.Distance(psGo.transform.position, target.transform.position + rel):F5}）");
            Check(WFModuleScaleByTarget.ConstraintTicks > ticks0,
                  "12-02：`ModuleTick` 真的贴过去了（🧨 删掉那个 override ⇒ 计数不涨 ⇒ 红）");

            // ② 旋转跟随、**距离不变**（绕锚点转，而不是被甩出去/原地不动）
            float d0 = Vector3.Distance(psGo.transform.position, target.transform.position);
            target.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            mod.ModuleTick(0.016f);
            float d1 = Vector3.Distance(psGo.transform.position, target.transform.position);
            Check(Mathf.Abs(d1 - d0) < 1e-3f,
                  $"12-02：锚点旋转 ⇒ 粒子绕它转（距离 {d0:F4} → {d1:F4} 必须不变）");

            // ③ 锚点（目标卡）被销毁 ⇒ 特效**留在世界**（原版约束源没了，物体留在原地）
            var keep = psGo.transform.position;
            Object.DestroyImmediate(target);
            mod.ModuleTick(0.016f);
            Check(psGo != null && Vector3.Distance(psGo.transform.position, keep) < 1e-3f,
                  "12-02：目标卡销毁后粒子**仍在世界原处**（原版行为）—— 🧨 父子化会让它跟着一起没 ⇒ 红");
        }
        finally
        {
            Object.DestroyImmediate(host);      // psGo 是它的子物体
            Object.DestroyImmediate(acting);
            if (target != null) Object.DestroyImmediate(target);
        }
    }

    // ======================================================================
    //  12-03 · `unParentAtStart` ⇒ 摘掉「持有者离场 ⇒ 自毁」（原版这两件永久驻留）
    // ======================================================================
    static void Check12_03_UnParent(WarpforgeEffectLibrary lib)
    {
        // 原版那两个：`Teleport Trait Summon` / `Teleport Trigger Effect Quick`（都 `preventDestroy = 1` + 模块 `unParentAtStart = 1`）
        const string Name = "Teleport Trait Summon";
        WFEffectEntry e;
        Check(lib.TryGet(Name, out e) && e != null, $"12-03（前提）：库里找得到 `{Name}`");
        if (e == null) return;

        var host = new GameObject("b139g_owner_host");
        try
        {
            WarpforgeEffectPlayer.ResetB139Diagnostics();
            var p = WarpforgeEffectPlayer.Play(e, host.transform, Vector3.zero, 1f, 30f);
            Check(p != null, $"12-03：《{Name}》播得起来");
            if (p != null)
            {
                Check(!p.HasOwnerWatch,
                      "12-03：模块 `unParentAtStart` 把特效根脱父之后，**持有者监视被摘掉**"
                    + "（原版此后与卡无任何关系 ⇒ 卡离场不收它；这两件 `preventDestroy = 1` ⇒ 永久驻留，照抄）"
                    + " —— 🧨 不调 `ClearOwner()` ⇒ `HasOwnerWatch` 为真 ⇒ 红");
                Check(WarpforgeEffectPlayer.OwnerCleared > 0,
                      "12-03：计数 `OwnerCleared` > 0（🧨 删掉 `Controller.ClearOwner()` 那一句 ⇒ 0 ⇒ 红）");
                p.Kill();
            }
        }
        finally { Object.DestroyImmediate(host); }

        // 对照：`preventDestroy` 但**没有** `unParentAtStart` 的效果，仍必须盯着持有者
        var ctrl = lib.entries.FirstOrDefault(x => x != null && x.prefab != null && x.preventDestroy
                        && (x.modules == null || !x.modules.Any(d => d != null
                             && d.kind == "AnimFXModuleTransformModifier"
                             && d.GetBool("unParentAtStart"))));
        Check(ctrl != null, "12-03 对照（前提）：库里有一个 `preventDestroy` 但 `unParentAtStart = 0` 的效果");
        if (ctrl == null) return;

        var host2 = new GameObject("b139g_owner_host2");
        try
        {
            var p2 = WarpforgeEffectPlayer.Play(ctrl, host2.transform, Vector3.zero, 1f, 30f);
            if (p2 != null)
            {
                Check(p2.HasOwnerWatch,
                      $"12-03 对照：《{ctrl.name}》没脱父 ⇒ **仍然盯着持有者**（原版「挂在卡下、卡走整棵没」那条路）"
                    + " —— 🧨 把 `ClearOwner()` 写成无条件调用 ⇒ 本条红");
                p2.Kill();
            }
            else Check(false, "12-03 对照：播不起来");
        }
        finally { Object.DestroyImmediate(host2); }
    }

    // ======================================================================
    //  12-05 · 到寿命那一帧的音效照响（原版 `Object.Destroy` 帧末生效）
    // ======================================================================
    static void Check12_05_SoundOnLifetimeFrame(WarpforgeEffectLibrary lib)
    {
        // 挑一个「有 `sounds[]` 且第一条 `time == 0`」的效果（现读 937 条里 91.5% 是 time 0）
        WFEffectEntry e = null;
        foreach (var x in lib.entries)
        {
            if (x == null || x.prefab == null || x.modules == null) continue;
            foreach (var d in x.modules)
            {
                if (d == null || d.kind != "AnimFXController") continue;
                if (!d.HasPrefix("sounds[0].")) continue;
                if (string.IsNullOrEmpty(d.GetString("sounds[0].sound"))) continue;
                if (Mathf.Abs(d.GetFloat("sounds[0].time")) > 1e-4f) continue;
                e = x; break;
            }
            if (e != null) break;
        }
        Check(e != null, "12-05（前提）：库里有一个 `sounds[0].time == 0` 的效果");
        if (e == null) return;

        WarpforgeEffectPlayer.ResetSoundDiagnostics();
        var p = WarpforgeEffectPlayer.Play(e, null, Vector3.zero, 1f, 0.01f);   // 寿命 = 0.01 s
        Check(p != null, $"12-05：《{e.name}》播得起来");
        if (p == null) return;

        Check(p.SoundSlotCount > 0, $"12-05（前提）：它挂上了 {p.SoundSlotCount} 条音效（没有的话下面等于没查）");
        int before = WFSoundPlayer.Played;
        p.Tick(0.02f);      // **同一帧跨过寿命**：这一帧既要判销毁、又要推音效
        int played = WFSoundPlayer.Played - before;
        Check(played > 0,
              $"12-05：**寿命那一帧的音效照响**（实测这一帧播了 {played} 声）—— 原版 `AnimFXController.Update` 先推 "
            + "`sounds[i].Update`、销毁是 `Object.Destroy(go, t)` **帧末**生效 ⇒ 那一帧音照响。"
            + "🧨 把 `TickSounds()` 挪回两条寿命判定之后 ⇒ 这一帧一条都不播 ⇒ 红");
        Check(p == null, "12-05：……而且这一帧**确实**销毁了（没销毁 = 寿命判据没生效，上面那条就不成立）");
    }

    // ======================================================================
    //  13-01 · `DestroyByTime` 只收**自己那个节点**
    // ======================================================================
    static void Check13_01_DestroyByTimeScope()
    {
        // ① 数据：`Blast_Big_Ork_Missiles` 子节点上那条 1.5 s 的 DestroyByTime 在原版是 **m_Enabled = 0**
        var blast = WFEffectExtras.For("Blast_Big_Ork_Missiles");
        var dbt = blast.FirstOrDefault(x => x != null && x.kind == "DestroyByTime");
        Check(dbt != null, "13-01（前提）：`Blast_Big_Ork_Missiles` 表里有一条 `DestroyByTime`");
        Check(dbt != null && dbt.disabled > 0,
              "13-01：那条 `DestroyByTime` 在原版是**禁用**的（`m_Enabled = 0`）⇒ 我们不挂它 "
            + "（🧨 不读 `m_Enabled` ⇒ 会按 1.5 s 把整件爆炸收掉，原版根是 4.0 s）");

        // ② 行为：子节点到点**只收自己**，整件特效还在
        var host = new GameObject("b139g_dbt_host");
        try
        {
            WFExtraDestroyByTime.ResetDiagnostics();
            var p = WarpforgeEffectPlayer.Play("UM_MasterOfArcanaEffect", host.transform, Vector3.zero, 1f, 100f);
            Check(p != null, "13-01：《UM_MasterOfArcanaEffect》播得起来（子节点上挂着 4.0 s 的 DestroyByTime）");
            if (p == null) return;

            Check(WFExtraDestroyByTime.Scheduled == 1,
                  $"13-01：排上了 1 次（实测 {WFExtraDestroyByTime.Scheduled}；0 = 那个组件没补上，下面没意义）");
            Transform node = p.transform.Find("Trait Icon");
            node = node != null ? node.Find("TalentEffect") : null;
            Check(node != null, "13-01（前提）：那个带 `DestroyByTime` 的子节点在（`Trait Icon#1/TalentEffect#1`）");

            p.Tick(4.1f);
            Check(node == null,
                  "13-01：**子节点**到点自己没了（原版 `Object.Destroy(this.gameObject, lifeTime)`）");
            Check(p != null,
                  "13-01：**整件特效仍在** —— 🧨 旧实现（`owner.DestroyAfter`）在这里会把**根**收掉 ⇒ `p` 变 null ⇒ 红");
            if (p != null) p.Kill();
        }
        finally { Object.DestroyImmediate(host); }
    }

    // ======================================================================
    //  13-03 · `SyncParticleSeeds` 只同步原版点名的那几颗
    // ======================================================================
    static void Check13_03_SyncSeeds()
    {
        const string Name = "Sword_Slash_Simple";       // 原版 4 颗（审计 F2 的例子：4 → 8）
        WFEffectEntry e;
        Check(WarpforgeEffectLibrary.Instance.TryGet(Name, out e) && e != null && e.prefab != null,
              $"13-03（前提）：库里找得到 `{Name}`");
        if (e == null || e.prefab == null) return;

        // 期望值**独立算**：从表里数「第一个空槽之前」的非空路径
        var extras = WFEffectExtras.For(Name).Where(x => x != null && x.kind == "SyncParticleSeeds").ToList();
        Check(extras.Count == 1, $"13-03（前提）：`{Name}` 表里有 1 个 `SyncParticleSeeds` 挂载（实测 {extras.Count}）");
        if (extras.Count != 1) return;
        string[] paths = extras[0].ps;
        Check(paths != null && paths.Length > 0,
              "13-03（前提）：表里有 `ps` 名单（空/缺 = 旧表 ⇒ 运行时会退回整棵子树的超集行为）");
        if (paths == null || paths.Length == 0) return;
        int expect = 0;
        while (expect < paths.Length && !string.IsNullOrEmpty(paths[expect])) expect++;

        int subtree = e.prefab.GetComponentsInChildren<ParticleSystem>(true).Length;
        int baseLocked = e.prefab.GetComponentsInChildren<ParticleSystem>(true).Count(x => !x.useAutoRandomSeed);
        Check(subtree > expect,
              $"13-03（前提）：子树 {subtree} 颗 > 点名的 {expect} 颗（相等就分不出「超集」与「子集」）");

        WFExtraSyncParticleSeeds.ResetDiagnostics();
        var p = WarpforgeEffectPlayer.Play(e, null, Vector3.zero, 1f, 30f);
        Check(p != null, $"13-03：《{Name}》播得起来");
        if (p == null) return;
        try
        {
            Check(WFExtraSyncParticleSeeds.Synced == expect,
                  $"13-03：只同步了 **{expect} 颗**（原版 `particlesSystems` 手挑的；实测 {WFExtraSyncParticleSeeds.Synced}）"
                + " —— 🧨 退回 `GetComponentsInChildren` 整棵子树 ⇒ 得 " + subtree + " ⇒ 红");
            Check(WFExtraSyncParticleSeeds.LegacySuperset == 0,
                  "13-03：没有走「旧表退回超集」那一支（🧨 表里删掉 `ps` 字段 ⇒ 得 1 ⇒ 红）");
            Check(WFExtraSyncParticleSeeds.PathMisses == 0,
                  $"13-03：点名的路径**零解析失败**（失败 {WFExtraSyncParticleSeeds.PathMisses} 处）");

            // 独立观测量：实例里 `useAutoRandomSeed == false` 的颗数 = prefab 自带的 + 这次同步的
            int locked = p.GetComponentsInChildren<ParticleSystem>(true).Count(x => !x.useAutoRandomSeed);
            Check(locked == baseLocked + expect,
                  $"13-03：实例里被锁种子的 = prefab 自带 {baseLocked} + 本次 {expect} = {baseLocked + expect}（实测 {locked}）");
        }
        finally { p.Kill(); }
    }

    // ======================================================================
    //  13-04 · `SubEmitterManualTrigger` 只按 `subEmitterIndexes` 触发
    // ======================================================================
    static void Check13_04_SubEmitter()
    {
        // ① 机制（夹具）：空数组 = 一次都不触发；`null`（旧表）= 退回全下标并出声；给定数组 = 按它
        var go = new GameObject("b139g_sub_host");
        try
        {
            go.AddComponent<ParticleSystem>();
            var c = go.AddComponent<WFExtraSubEmitterManualTrigger>();
            c.triggerOnSecond = 0.01f;
            WFExtraSubEmitterManualTrigger.ResetDiagnostics();

            c.Apply(new int[0]);
            Check(c.Indexes != null && c.Indexes.Length == 0,
                  "13-04：原版**空数组** ⇒ 一颗都不触发（原版 12 个挂载里 11 个是空数组）");
            c.Apply(null);
            Check(WFExtraSubEmitterManualTrigger.LegacyAllIndexes == 1,
                  "13-04：`null`（旧表没有这个字段）⇒ 退回「全下标」并**出声计数**（不静默）");
            c.Apply(new[] { 1 });
            Check(c.Indexes != null && c.Indexes.Length == 1 && c.Indexes[0] == 1,
                  "13-04：给定数组 ⇒ **只按下标 [1]**（🧨 仍写成「全部子发射器」⇒ 长度 = 子发射器数 ⇒ 红）");
        }
        finally { Object.DestroyImmediate(go); }

        // ② 数据：唯一非空那颗（`Sau_ParticleWhip`）在原版是**禁用**的（m_Enabled = 0）
        var sau = WFEffectExtras.For("Sau_ParticleWhip").Where(x => x != null && x.kind == "SubEmitterManualTrigger").ToList();
        Check(sau.Count == 1, $"13-04（前提）：`Sau_ParticleWhip` 表里有 1 个挂载（实测 {sau.Count}）");
        if (sau.Count == 1)
        {
            Check(sau[0].se != null && sau[0].se.Length == 1 && sau[0].se[0] == 1,
                  "13-04：它的 `subEmitterIndexes` = **[1]**（原版原值；🧨 读成「四个元素 1/0/0/0」或「全部」⇒ 红）");
            Check(sau[0].disabled > 0,
                  "13-04：而且该组件 `m_Enabled = 0` ⇒ **原版一次都不触发**（我们不挂它）");
        }

        // ③ 端到端：那件特效播出来之后**没有**这个组件（禁用不挂）⇒ 触发次数 0
        WFExtraSubEmitterManualTrigger.ResetDiagnostics();
        var p = WarpforgeEffectPlayer.Play("Sau_ParticleWhip", null, Vector3.zero, 1f, 30f);
        Check(p != null, "13-04：《Sau_ParticleWhip》播得起来");
        if (p != null)
        {
            var comps = p.GetComponentsInChildren<WFExtraSubEmitterManualTrigger>(true);
            Check(comps.Length == 0,
                  $"13-04：**禁用的组件不挂** ⇒ 0 个（实测 {comps.Length}）—— 🧨 忽略 `m_Enabled` ⇒ 挂上 1 个 ⇒ 红");
            p.Tick(1.0f);
            Check(WFExtraSubEmitterManualTrigger.Triggered == 0,
                  $"13-04：tick 1 s 之后触发 {WFExtraSubEmitterManualTrigger.Triggered} 次（原版 0 次；我们改前 19 次）");
            p.Kill();
        }

        // ④ 端到端：`NecronGauss_Hexmark` 那 6 个挂载原版数组为空
        var p2 = WarpforgeEffectPlayer.Play("NecronGauss_Hexmark", null, Vector3.zero, 1f, 30f);
        Check(p2 != null, "13-04：《NecronGauss_Hexmark》播得起来");
        if (p2 != null)
        {
            var comps = p2.GetComponentsInChildren<WFExtraSubEmitterManualTrigger>(true);
            Check(comps.Length == 6, $"13-04：`NecronGauss_Hexmark` 6 个挂载都补上了（实测 {comps.Length}）");
            int nonEmpty = comps.Count(x => x != null && x.Indexes != null && x.Indexes.Length > 0);
            Check(nonEmpty == 0,
                  $"13-04：它们**全部是空数组**（非空 {nonEmpty} 个）—— 原版这 6 处什么都不做");
            p2.Kill();
        }
    }

    // ======================================================================
    //  13-05 · `prewarm` 的循环系统不再被 `Clear`
    // ======================================================================
    static void Check13_05_Prewarm(WarpforgeEffectLibrary lib)
    {
        const string Name = "Blast_Big_Ork_Missiles";     // 审计 F5：这件独占 16 颗 prewarm 系统
        WFEffectEntry e;
        Check(lib.TryGet(Name, out e) && e != null && e.prefab != null, $"13-05（前提）：库里找得到 `{Name}`");
        if (e == null || e.prefab == null) return;

        var prefabPs = e.prefab.GetComponentsInChildren<ParticleSystem>(true);
        int prewarm = prefabPs.Count(x => x.main.prewarm);
        Check(prewarm > 0, $"13-05（前提）：该 prefab 有 {prewarm} 颗 `prewarm` 循环系统（0 的话下面等于没查）");
        if (prewarm == 0) return;

        WarpforgeEffectPlayer.ResetB139Diagnostics();
        var p = WarpforgeEffectPlayer.Play(e, null, Vector3.zero, 1f, 30f);
        Check(p != null, $"13-05：《{Name}》播得起来");
        if (p == null) return;
        try
        {
            int total = p.GetComponentsInChildren<ParticleSystem>(true).Length;
            Check(WarpforgeEffectPlayer.PrewarmKept == prewarm,
                  $"13-05：`prewarm` 的系统**只 Play、不 Clear**（{prewarm} 颗；实测 {WarpforgeEffectPlayer.PrewarmKept}）"
                + " —— 🧨 改回无条件 `Clear(true)` ⇒ 得 0 ⇒ 红");
            Check(WarpforgeEffectPlayer.InitCleared == total - prewarm,
                  $"13-05 对照：其余 {total - prewarm} 颗仍走 `Clear + Play`（实测 {WarpforgeEffectPlayer.InitCleared}）"
                + " —— 🧨 把整个循环删掉 ⇒ 得 0 ⇒ 红");
        }
        finally { p.Kill(); }
    }

    // ======================================================================
    //  13-07 · `ParticleEmitOnEnable` 不再同节点挂两份
    // ======================================================================
    static void Check13_07_EmitOnEnableDedupe()
    {
        // ① 机制：同节点上已经有等价件 ⇒ 认得出来
        var a = new GameObject("b139g_pem_a");
        var b = new GameObject("b139g_pem_b");
        try
        {
            a.AddComponent<ParticleSystem>();          // 先有 PS，免得 `ParticleEmitOnEnable.OnEnable` 打「没有 ParticleSystem」的警告
            a.AddComponent<CardPresentation.ParticleEmitOnEnable>();
            Check(WFEffectExtras.HasSameSemanticsComponent(a, "ParticleEmitOnEnable"),
                  "13-07：同节点已有 `CardPresentation.ParticleEmitOnEnable` ⇒ 认得出来（不重复挂）");
            Check(!WFEffectExtras.HasSameSemanticsComponent(b, "ParticleEmitOnEnable"),
                  "13-07 对照：干净节点 ⇒ 认不出（否则「认得出来」这条没有分辨力）");
        }
        finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }

        // ② 端到端：表里那件特效的 `Trail` 上只有**一份** `ParticleEmitOnEnable`
        var p = WarpforgeEffectPlayer.Play("Invoke Minion Card Fade Default", null, Vector3.zero, 1f, 30f);
        Check(p != null, "13-07：《Invoke Minion Card Fade Default》播得起来");
        if (p == null) return;
        try
        {
            // ⚠️ 2026-11-04 更正（调度台）：`effect_extras.json` 里那条 `ParticleEmitOnEnable` 挂的是 **`Trail#2`**，
            //    不是 `Trail`（同名节点有两颗）⇒ 先找 `Trail#2`，再退回 `Trail`。
            // ⚠️ 2026-11-04 更正（调度台）：用**递归**找 —— `Find` 只搜直接子件；
            //    表里那条 `ParticleEmitOnEnable` 的 node 是 `…/Trail#2`（同名有两颗）。
            Transform trail = null;
            foreach (var tr in p.GetComponentsInChildren<Transform>(true))
            {
                if (tr != null && (tr.name == "Trail#2" || tr.name == "Trail"))
                {
                    if (tr.name == "Trail#2") { trail = tr; break; }   // 优先 #2（表里点的是它）
                    if (trail == null) trail = tr;
                }
            }
            Check(trail != null, "13-07（前提）：`Trail` 节点在（表里那一条 `ParticleEmitOnEnable` 就挂在它上面）");
            if (trail != null)
            {
                int n = trail.GetComponents<MonoBehaviour>()
                            .Count(m => m != null && (m.GetType().Name == "ParticleEmitOnEnable" || m.GetType().Name == "WFExtraParticleEmitOnEnable"));   // ⚠️ 2026-11-04 更正（调度台）：两种等价件都算 —— 表里挂的是 WFExtraParticleEmitOnEnable
                Check(n == 1, $"13-07：`Trail` 上**恰好一份** `ParticleEmitOnEnable`（实测 {n}）");
            }

            // ③ 幂等：再跑一次 `AttachAll` ⇒ 这一种被跳过（不出现第二份）
            int d0 = WFEffectExtras.DuplicateSkipped;
            WFEffectExtras.AttachAll(p);
            Check(WFEffectExtras.DuplicateSkipped == d0 + 1,
                  $"13-07：重复 `AttachAll` **幂等**（`DuplicateSkipped` {d0} → {WFEffectExtras.DuplicateSkipped}，应为 +1）"
                + " —— 🧨 去掉那两行同名判据 ⇒ 0 ⇒ 红");
            if (trail != null)
            {
                int n = trail.GetComponents<MonoBehaviour>()
                            .Count(m => m != null && (m.GetType().Name == "ParticleEmitOnEnable" || m.GetType().Name == "WFExtraParticleEmitOnEnable"));   // ⚠️ 2026-11-04 更正（调度台）：两种等价件都算 —— 表里挂的是 WFExtraParticleEmitOnEnable
                Check(n == 1, $"13-07：重复一次之后 `Trail` 上仍然只有一份（实测 {n}）");
            }
        }
        finally { p.Kill(); }
    }
}
