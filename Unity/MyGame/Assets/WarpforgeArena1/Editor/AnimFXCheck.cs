// AnimFXCheck.cs — AnimFX 模块层的自检（断言，不是截图）
//
// 验四件事：
//   ① **数据到位**：库里有多少效果带模块、一共多少模块、各类各多少；
//   ② **实现覆盖**：数据里出现的每个 kind，工厂是不是都认得（不认得的列出来 —— 这是「还差多少」的判据）；
//   ③ **装配真的发生**：按 kind 各挑一个效果播一遍，断言模块组件真被建出来、且挂在对的节点上；
//   ④ **引用解析没偷偷失败**：`WFEffectModule.ResolveFailed` 与屏震 preset 缺失都必须为 0。
//
// 判据看末尾 `=== 合计：N 通过 / M 失败 ===` 与批处理退出码。
// 用法：`-executeMethod AnimFXCheck.Run`
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class AnimFXCheck
{
    const string P = "ANIMFX ";
    static int _pass, _fail;

    /// <summary>**已知**解不出的音效 cue。
    ///
    /// ⚠️ 2026-09-19 实测：**已是空表** —— 曾经有 4 个（`Buff Black Legion 3` / `Helbrute_plasma` /
    /// `Meltagun_Chaos` / `Sororitas Shrine Bombardment Audio`）报「bundle 里没有」，
    /// 真因是**第一次只读了 `soundcollection_assets_all` 一个包**，它们其实在
    /// `battleprefabs_vfxandmisc_assets_all` 里 —— **又一次「查不到 = 搜错了目录」**。
    /// 导入器已改成「按名字定位包」，408/408 全解得出。
    /// 保留这个白名单机制（而不是删掉断言）：**新增**的缺失能立刻红出来。
    /// 真有查实的缺口就往这里加一条，并在注释里写清为什么。</summary>
    static readonly string[] KnownMissingCues = new string[0];

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log(P + "  ✓ " + what); }
        else { _fail++; Debug.LogError(P + "  ✗ " + what); }
    }

    public static void Run()
    {
        Debug.Log(P + "=== AnimFX 模块层 自检 开始 ===");

        var lib = WarpforgeEffectLibrary.Instance;
        Check(lib != null, "效果库读得到");
        if (lib == null) { Done(); return; }
        var entries = lib.entries ?? new WFEffectEntry[0];

        // ---- ① 数据到位 ----
        var kinds = new Dictionary<string, int>();
        int withMods = 0, totalMods = 0;
        foreach (var e in entries)
        {
            if (e == null || e.modules == null || e.modules.Length == 0) continue;
            withMods++;
            foreach (var d in e.modules)
            {
                if (d == null || string.IsNullOrEmpty(d.kind)) continue;
                totalMods++;
                kinds[d.kind] = (kinds.TryGetValue(d.kind, out var n) ? n : 0) + 1;
            }
        }
        Debug.Log(P + $"  数据：{withMods} 个效果带模块 / 共 {totalMods} 个模块 / {kinds.Count} 种 kind");
        foreach (var kv in kinds.OrderByDescending(x => x.Value))
            Debug.Log(P + $"      {kv.Key,-34} {kv.Value}");
        Check(totalMods > 0, "库里带了模块数据（没有的话先跑 dump_animfx.py + gen_animfx_modules.py）");

        // ⚠️ **期望值要按「交集」算，不是 dump 的原始总数**：`animfx_modules.json` 有 912 个效果 /
        //    2346 个模块，但里面有 17 个是 **UI / 棋盘 prefab 上的 AnimFX 控制器**
        //    （`Content` · `Scenario` · `Wildcard_Use_Button*` · `TurnTimerTickPulse` …），
        //    **它们不在效果库的 958 个里**，本来就不该收。所以断言必须用交集，
        //    否则每次都会红 —— 而「一直红的断言」等于没有断言。
        int expectEff = 0, expectMods = 0;
        var inLib = new HashSet<string>(entries.Select(e => e.name));
        if (System.IO.File.Exists(EffectLibraryBuilder.DefaultModulesJson))
        {
            var d = JsonUtility.FromJson<ModuleDoc>(
                System.IO.File.ReadAllText(EffectLibraryBuilder.DefaultModulesJson));
            if (d != null && d.effects != null)
                foreach (var me in d.effects)
                    if (me != null && me.modules != null && inLib.Contains(me.name))
                    { expectEff++; expectMods += me.modules.Length; }
        }
        Check(withMods == expectEff, $"带模块的效果数 = 两个文件的交集（期望 {expectEff}，实测 {withMods}）");
        Check(totalMods == expectMods, $"模块总数 = 交集的模块数（期望 {expectMods}，实测 {totalMods}）");

        // ---- ② 实现覆盖 ----
        WFModuleFactory.ResetMissing();
        // ⚠️ `AnimFXController` **不算模块** —— 它是播放器自己（`destroyTime` 那三个字段早就在用），
        //    所以「每种 kind 都要有模块实现」这条要把它排掉，否则会永远红着，
        //    而**一直红的断言等于没有断言**。
        var moduleKinds = kinds.Keys.Where(k => k != "AnimFXController").ToList();
        var missing = moduleKinds.Where(k => !WFModuleFactory.IsImplemented(k)).OrderBy(k => k).ToList();
        int covered = moduleKinds.Count(k => WFModuleFactory.IsImplemented(k));
        Debug.Log(P + $"  工厂认得 {covered}/{moduleKinds.Count} 种模块 kind；没实现的 {missing.Count} 种："
                    + (missing.Count == 0 ? "（无）" : string.Join(" / ", missing)));
        Check(missing.Count == 0, "数据里每种模块 kind 都有实现（没实现的会打警告并少一层行为）");
        // ---- ①b 音效 cue 覆盖（✅ 2026-09-19 接线；这里做**全量**覆盖检查）----
        // ⚠️ 不能只查「播过的那些」—— 自检只播几个效果，那样覆盖不到全表。
        //    所以直接扫**数据文件**里全部 `sounds[*].sound` / `exitSounds[*].sound`。
        // 判据：除了**已知的 4 个**（那 4 个 cue 不在 `soundcollection_assets_all.bundle` 里，
        // 见 `资料/AnimFX_实现与接线.md` §11.4），不该再有任何解不出的 cue。
        // ⚠️ 写成「必须 == 0」的话会**每次全红** —— 而一直红的断言等于没有断言。
        if (System.IO.File.Exists(EffectLibraryBuilder.DefaultModulesJson))
        {
            var sdoc = JsonUtility.FromJson<ModuleDoc>(
                System.IO.File.ReadAllText(EffectLibraryBuilder.DefaultModulesJson));
            var allCues = new HashSet<string>();
            int soundEntries = 0;
            if (sdoc != null && sdoc.effects != null)
                foreach (var me in sdoc.effects)
                {
                    if (me == null || me.modules == null) continue;
                    foreach (var m in me.modules)
                    {
                        if (m == null || m.kind != "AnimFXController") continue;
                        foreach (var k in new[] { "sounds", "exitSounds" })
                            for (int i = 0; i < m.CountList(k); i++)
                            {
                                string v = m.GetString(k + "[" + i + "].sound");
                                if (string.IsNullOrEmpty(v)) continue;      // 没有声音的槽位
                                soundEntries++;
                                string kk, tt, rest;
                                WFModuleDef.SplitRef(v, out kk, out tt, out rest);
                                if (kk == "asset" && tt == "MonoBehaviour") allCues.Add(rest);
                                else Check(false, $"音效引用形状认不出：`{v}`（{me.name}）");
                            }
                    }
                }
            var miss = allCues.Where(c => !WFSoundBank.HasCue(c)).OrderBy(c => c).ToList();
            var unexpected = miss.Where(c => !KnownMissingCues.Contains(c)).ToList();
            Debug.Log(P + $"  音效：数据里 {soundEntries} 条 · 不同 cue {allCues.Count} 个 · "
                        + $"表里查得到 {allCues.Count - miss.Count} 个 · 缺 {miss.Count} 个"
                        + (miss.Count > 0 ? $"（{string.Join(" / ", miss)}）" : ""));
            Check(unexpected.Count == 0,
                  $"除已知缺失的 {KnownMissingCues.Length} 个 cue 外没有新增解不出的"
                  + (unexpected.Count > 0 ? $"（新缺：{string.Join(" / ", unexpected)}）" : ""));
            Check(soundEntries == 941, $"音效条目数 = 941（`sounds` 有值的 934 + `exitSounds` 7；实测 {soundEntries}）");
        }

        // ---- ③ 装配真的发生：每种 kind 挑第一个效果播一遍 ----
        WFEffectModule.ResetResolveFailed();
        WFModuleScreenShake.ResetDiagnostics();
        int shakeFired = 0;
        var prevHook = WFModuleScreenShake.OnShake;
        WFModuleScreenShake.OnShake = r => shakeFired++;        // 自检期间自己接一下，顺便验钩子通
        try
        {
            foreach (var kind in moduleKinds.OrderBy(k => k))
            {
                var eff = entries.FirstOrDefault(e => e != null && e.modules != null
                                                      && e.modules.Any(d => d != null && d.kind == kind));
                if (eff == null) continue;
                var p = WarpforgeEffectPlayer.Play(eff.name, null, Vector3.zero, 1f);
                if (p == null) { Check(false, $"[{kind}] 效果《{eff.name}》播不起来"); continue; }
                p.Tick(0.016f);
                // ⚠️ 判据是「**这个 kind** 的模块建出来了」，不是「装了模块数 > 0」——
                //    一个效果通常带好几个模块，写松了会让没实现的 kind 被别的模块顶成绿的。
                bool hasKind = p.Modules.Any(m => WFModuleFactory.KindOf(m) == kind);
                Check(hasKind, $"[{kind}] 《{eff.name}》装上了这个模块（该效果共 {p.ModuleCount} 个模块）");
                p.Kill();
            }
        }
        finally { WFModuleScreenShake.OnShake = prevHook; }

        Check(WFEffectModule.ResolveFailed == 0,
              $"模块引用路径**零解析失败**（实测 {WFEffectModule.ResolveFailed} —— 非 0 会在上面留下逐条警告）");

        // ---- ③b 全量路径：把**所有**效果的 `__node` 与 `@node:` 引用都解一遍 ----
        // 上面每种 kind 只挑了一个效果，覆盖不到「别的效果里有没有对不上的路径」。
        // 这一段不走播放（不实例化），直接在 prefab 资产上解路径 —— **快，而且是全量**。
        // 它同时验了 python 的 `_path_of` 与 C# 的 `ResolvePath` 两边约定一致（`#N` 的语义）。
        WFEffectModule.ResetResolveFailed();
        int nodeRefs = 0, nodeRefFail = 0, nodeHostFail = 0, effectsTouched = 0;
        foreach (var e in entries)
        {
            if (e == null || e.prefab == null || e.modules == null || e.modules.Length == 0) continue;
            effectsTouched++;
            var root = e.prefab.transform;
            foreach (var d in e.modules)
            {
                if (d == null || string.IsNullOrEmpty(d.kind) || d.kind == "AnimFXController") continue;

                var np = d.GetString("__node");
                if (!string.IsNullOrEmpty(np) && WFEffectModule.ResolvePath(root, np, d.kind) == null)
                    nodeHostFail++;

                for (int i = 0; i < d.keys.Length; i++)
                {
                    string v = d.values[i];
                    if (string.IsNullOrEmpty(v) || !v.StartsWith("@node:")) continue;
                    nodeRefs++;
                    string kind, type, rest;
                    WFModuleDef.SplitRef(v, out kind, out type, out rest);
                    if (WFEffectModule.ResolvePath(root, rest, d.kind) == null) nodeRefFail++;
                }
            }
        }
        Debug.Log(P + $"  全量路径：{effectsTouched} 个效果 / `__node` 失败 {nodeHostFail} / "
                    + $"`@node:` 引用 {nodeRefs} 条、失败 {nodeRefFail}");
        Check(nodeHostFail == 0, $"全部效果的 `__node`（模块挂哪个节点）都解得开（失败 {nodeHostFail}）");
        Check(nodeRefFail == 0, $"全部 `@node:` 引用（{nodeRefs} 条）都解得开（失败 {nodeRefFail}）");
        WFEffectModule.ResetResolveFailed();
        Check(WFModuleScreenShake.MissingPresets.Count == 0,
              $"屏震 preset 全都找得到（缺 {WFModuleScreenShake.MissingPresets.Count} 个："
              + string.Join(" / ", WFModuleScreenShake.MissingPresets) + "）");

        // ---- ④ 屏震那条链真的通：播一个有自动档屏震的效果，钩子应当被调到 ----
        var shakeEff = entries.FirstOrDefault(e => e != null && e.modules != null
                        && e.modules.Any(d => d != null && d.kind == "AnimFXModuleScreenShake"));
        if (shakeEff != null)
        {
            var prev = WFModuleScreenShake.OnShake;
            WFModuleScreenShake.OnShake = r => shakeFired++;
            var p = WarpforgeEffectPlayer.Play(shakeEff.name, null, Vector3.zero, 1f);
            if (p != null) p.Kill();
            WFModuleScreenShake.OnShake = prev;
            Check(shakeFired > 0, $"屏震钩子被调到（《{shakeEff.name}》），实测 {shakeFired} 次");
        }

        // ---- ⑤ `ScaleByTarget.ChangeShapeAngle` 的锥角修正真的算了（2026-09-18 还原）----
        //    公式（照 VA 反汇编，见 `WFModuleScaleByTarget.cs` 文件头）：
        //      shape.angle = atan2( tan(旧角×Deg2Rad) × 兵线距 × **目标卡**.localScale.x , 两卡3D距离 ) × Rad2Deg
        //    判据**不是**「计数 > 0」就完事 —— 那只证明代码跑了。这里拿 **prefab 里的原始标定角** 按公式算一遍
        //    期望值，再和**运行时**那个粒子系统的角比 ⇒ 「算对了」才有依据。
        //    ⚠️ 2026-09-18 之前这一段是没有的：那时代码只打 `ShapeAngleNotApplied` + 一条运行期 LogWarning，
        //    **没有任何自检汇总它** ⇒ 文档里那个「97/314」谁也看不见。
        {
            var prevCards = WFModuleScaleByTarget.CardResolver;
            var prevLines = WFModuleScaleByTarget.MinionLines;
            WFModuleScaleByTarget.ResetDiagnostics();
            try
            {
                // 挑一个 `particleSystemsShapeAngle` **真的非空**的模块（出货数据里 314 个里只有 97 个是这种）
                WFEffectEntry angleEff = null;
                string angleRef = null;
                foreach (var e in entries)
                {
                    if (e == null || e.prefab == null || e.modules == null) continue;
                    foreach (var d in e.modules)
                    {
                        if (d == null || d.kind != "AnimFXModuleScaleByTarget") continue;
                        var refs = WFModuleScaleByTarget.ReadRefStrings(d, "particleSystemsShapeAngle");
                        if (refs != null && refs.Length > 0 && !string.IsNullOrEmpty(refs[0]))
                        { angleEff = e; angleRef = refs[0]; break; }
                    }
                    if (angleEff != null) break;
                }
                Check(angleEff != null && angleRef != null,
                      "库里找得到一个 `particleSystemsShapeAngle` 非空的 ScaleByTarget 效果");
                if (angleEff != null && angleRef != null)
                {
                    var prefabPs = WFEffectModule.ResolveNode<ParticleSystem>(
                        angleEff.prefab.transform, angleRef, "AnimFXCheck");
                    Check(prefabPs != null, $"……那个粒子系统在 prefab 里找得到（`{angleRef}`）");

                    // 🔴 **公式本身必须用「可控输入」验** —— 第一版拿库里这个真实效果试的，结果它的标定角是
                    //    **0.00°**：`tan(0)=0` ⇒ 公式对不对都算出 0，**过了也是假的**（正是本项目说的
                    //    「尺子的假象」；而且连「写回生不生效」都分不出来 —— 没写进去时读出来也是 0）。
                    //    所以这里**自己建宿主**：标定角 **30°**（tan≠0）、卡距 3、兵线距 2.241，三个用例分别钉住
                    //    「按公式算」·「目标卡 scale 真的进公式」·「取的是**目标卡**那侧的 scale」。
                    var host = new GameObject("chk_sbt_host");
                    var psGo = new GameObject("chk_sbt_ps");
                    var goA = new GameObject("chk_acting");
                    var goB = new GameObject("chk_target");
                    try
                    {
                        psGo.transform.SetParent(host.transform, false);
                        var ps = psGo.AddComponent<ParticleSystem>();
                        Check(Mathf.Abs(SetShapeAngle(ps, 30f) - 30f) < 0.01f,
                              "夹具：`shape.angle` **写得进去**（写回不生效的话下面全都无意义）");

                        var mod = host.AddComponent<WFModuleScaleByTarget>();
                        mod.particleSystemsShapeAngle = new[] { ps };
                        mod.actingCard = goA.transform;
                        mod.targetCard = goB.transform;
                        WFModuleScaleByTarget.CardResolver = null;      // 夹具直接把两张卡挂在模块上

                        goA.transform.position = new Vector3(-1.5f, 0f, 0f);
                        goB.transform.position = new Vector3(1.5f, 0f, 0f);      // 卡距 = 3
                        const float lineD = 2.241f;    // 我们两条兵线中心线的距离 = 原版屏上 242 px
                        const float cardD = 3f;
                        WFModuleScaleByTarget.MinionLines = (out Vector3 pl, out Vector3 el) =>
                        { pl = Vector3.zero; el = new Vector3(0f, lineD, 0f); return true; };
                        System.Func<float, float> Expect = sx =>
                            Mathf.Atan2(Mathf.Tan(30f * Mathf.Deg2Rad) * lineD * sx, cardD) * Mathf.Rad2Deg;

                        // ---- 用例 A：两张卡 scale 都是 1 ----
                        goA.transform.localScale = Vector3.one;
                        goB.transform.localScale = Vector3.one;
                        WFModuleScaleByTarget.ResetDiagnostics();
                        SetShapeAngle(ps, 30f);
                        mod.Initialize(null);
                        float angA = ps.shape.angle;
                        Check(Mathf.Abs(Mathf.DeltaAngle(angA, Expect(1f))) < 0.01f,
                              $"★ 锥角**按公式算**：期望 {Expect(1f):F4}° · 实得 {angA:F4}°"
                              + $"（标定角 30° · 兵线距 {lineD} · 卡距 {cardD} · scaleX 1）");
                        Check(Mathf.Abs(Mathf.DeltaAngle(angA, 30f)) > 1f,
                              "……**确实变了**（若只是把原角抄回去，上面那条就没有分辨力）");
                        Check(WFModuleScaleByTarget.ShapeAngleApplied == 1
                              && WFModuleScaleByTarget.ShapeAngleNotApplied == 0,
                              "……走的是「算过」那支，**不是**「两条兵线没接上」那支");

                        // ---- 用例 B：**目标卡** scale.x → 2 ⇒ 分子翻倍 ⇒ 角跟着变 ----
                        goB.transform.localScale = new Vector3(2f, 1f, 1f);
                        SetShapeAngle(ps, 30f);
                        mod.Initialize(null);
                        float angB = ps.shape.angle;
                        Check(Mathf.Abs(Mathf.DeltaAngle(angB, Expect(2f))) < 0.01f,
                              $"★ **目标卡的 scale 真的进公式**：期望 {Expect(2f):F4}° · 实得 {angB:F4}°（scaleX 1→2）");
                        Check(Mathf.Abs(Mathf.DeltaAngle(angA, angB)) > 1f, "……而且与用例 A 明显不同");

                        // ---- 用例 C：**出招卡** scale.x → 5（目标卡仍是 1）⇒ 结果必须**与 A 相同** ----
                        goB.transform.localScale = Vector3.one;
                        goA.transform.localScale = new Vector3(5f, 1f, 1f);
                        SetShapeAngle(ps, 30f);
                        mod.Initialize(null);
                        float angC = ps.shape.angle;
                        Check(Mathf.Abs(Mathf.DeltaAngle(angC, angA)) < 0.01f,
                              $"★ 取的是**目标卡**那侧：出招卡 scale 1→5 后结果**不变**（{angC:F4}° = {angA:F4}°）"
                              + "—— 接成出招卡的话这条会红");
                    }
                    finally
                    {
                        Object.DestroyImmediate(host);      // psGo 是它的子物体，一起没
                        Object.DestroyImmediate(goA);
                        Object.DestroyImmediate(goB);
                    }
                }
            }
            finally
            {
                WFModuleScaleByTarget.CardResolver = prevCards;
                WFModuleScaleByTarget.MinionLines = prevLines;
            }
        }

        // ---- ⑤ 音效**真的会播**（🆕 2026-09-19 接线：调度算对没有）----
        // 只验「调度」不验「响声」：`WFSoundPlayer.Enabled = false` 时**仍然记账**（`Played` 照样涨），
        // 所以批处理里既不出声也验得到。⚠️ 恢复 `Enabled` 要在 finally 里 —— 这是**全局**开关。
        {
            bool prevEnabled = WFSoundPlayer.Enabled;
            WFSoundPlayer.Enabled = false;
            try
            {
                var withSound = entries.FirstOrDefault(e => e != null && e.modules != null
                    && e.modules.Any(d => d != null && d.kind == "AnimFXController" && d.HasPrefix("sounds[")));
                Check(withSound != null, "库里找得到带音效的效果");
                if (withSound != null)
                {
                    WarpforgeEffectPlayer.ResetSoundDiagnostics();
                    var p = WarpforgeEffectPlayer.Play(withSound.name, null, Vector3.zero, 1f);
                    Check(p != null, $"效果《{withSound.name}》播得起来");
                    if (p != null)
                    {
                        Check(p.SoundSlotCount > 0, $"它挂上了 {p.SoundSlotCount} 条音效条目");
                        int before = WFSoundPlayer.Played;
                        p.Tick(0.016f);       // 大多数条目 `time = 0` ⇒ 第一帧就该播
                        Check(WFSoundPlayer.Played > before,
                              $"★ 推进一帧后播了 {WFSoundPlayer.Played - before} 声（调度真的在跑，不是只记了账）");
                        Check(WarpforgeEffectPlayer.UnwiredSoundCues == 0,
                              "这条链上没有一个解不出的 cue");
                        p.Kill();
                    }
                }
            }
            finally { WFSoundPlayer.Enabled = prevEnabled; }
        }

        Done();
    }

    // 只用来算「库里该有多少」——形状要跟 `数据/游戏数据/animfx_modules.json` 对齐
    [System.Serializable] class ModuleDoc { public int effect_count; public int module_count; public ModuleEffect[] effects; }
    [System.Serializable] class ModuleEffect { public string name; public WFModuleDef[] modules; }

    /// <summary>写 `shape.angle` 并**读回来**（`ShapeModule` 是结构体，写回靠它内部的引用 ——
    /// 写不进去的话读回来的还是旧值，所以返回值就是「写回生不生效」的判据）。</summary>
    static float SetShapeAngle(ParticleSystem ps, float deg)
    {
        var s = ps.shape;
        s.angle = deg;
        return ps.shape.angle;
    }

    static void Done()
    {
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }
}
