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
        // 控制器上那条**已知没接线**的：音效。记账而不是假装 —— 数字要看得到。
        Debug.Log(P + $"  已知未接线：原版音效 `sounds/exitSounds` 累计见过 "
                    + $"{WarpforgeEffectPlayer.UnwiredSoundCues} 条（音效的 AudioClip 在一层 "
                    + "MonoBehaviour 包装里面，要接得先解开那层）");

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

        Done();
    }

    // 只用来算「库里该有多少」——形状要跟 `数据/游戏数据/animfx_modules.json` 对齐
    [System.Serializable] class ModuleDoc { public int effect_count; public int module_count; public ModuleEffect[] effects; }
    [System.Serializable] class ModuleEffect { public string name; public WFModuleDef[] modules; }

    static void Done()
    {
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }
}
