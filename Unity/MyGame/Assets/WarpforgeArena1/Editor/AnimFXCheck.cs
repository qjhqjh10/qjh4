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

    /// <summary>**历史上**解不出的音效 cue —— 🔴 **现为空数组**（`new string[0]`）。
    ///
    /// ⚠️ 2026-10-23 更正（铁律 5）：本行原来写「**已知**解不出的音效 cue」，读起来像「现在还有 4 个」。
    /// 实际那 4 个（`Buff Black Legion 3` / `Helbrute_plasma` / `Meltagun_Chaos` /
    /// `Sororitas Shrine Bombardment Audio`）**早在 2026-09-19 就补齐了**：它们当年报「bundle 里没有」，
    /// 真因是**第一次只读了 `soundcollection_assets_all` 一个包**，它们其实在
    /// `battleprefabs_vfxandmisc_assets_all` 里 —— **又一次「查不到 = 搜错了目录」**。
    /// 导入器已改成「按名字定位包」，408/408 全解得出。**错因**：把「历史缺口」写成了现在时。
    /// 🔑 **这个字段现在的用途 = 白名单机制**（**不是**「记录已知缺口」）：①b 那条断言写的是
    /// 「**除本表列出的以外**新增缺失 = 0」，所以留一个空数组（而不是删掉断言）⇒ **将来**真出现
    /// 解不出的 cue 会**立刻红**；查实是原版本身缺的，才往这里加一条并写清为什么。</summary>
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
        // ⚠️ 2026-10-22：join 键**两侧都要 Trim**。原版有一个效果名带**尾随空格**
        //    （`"Orbital 5 repeat "`，见 `EffectLibraryBuilder.JoinKey` 的注释），而库条目名来自
        //    **Windows 文件名**（文件系统落不了尾随空格）⇒ 逐字比会被两边**同时漏掉**：
        //    库修好之前恰好还是平的（它的 modules 是空的），修好之后 `withMods` / `totalMods`
        //    会各多 1 / 2 ⇒ 下面 :83 :84 两条断言转红。口径必须和 `EffectLibraryBuilder.JoinKey` 一致
        //    （CLAUDE.md §三「两处写同一条规则 = 迟早不一致」）—— 那个函数是 private，这里就地实现同一口径。
        var inLib = new HashSet<string>(entries.Select(e => (e.name ?? "").Trim()));
        if (System.IO.File.Exists(EffectLibraryBuilder.DefaultModulesJson))
        {
            var d = JsonUtility.FromJson<ModuleDoc>(
                System.IO.File.ReadAllText(EffectLibraryBuilder.DefaultModulesJson));
            if (d != null && d.effects != null)
                foreach (var me in d.effects)
                    if (me != null && me.modules != null && inLib.Contains((me.name ?? "").Trim()))
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
        // 判据：除了 `KnownMissingCues` 里列出的（🔴 **现为空数组**；⚠️ 2026-10-23 更正：原来这里写
        // 「已知的 4 个…不在 `soundcollection_assets_all.bundle` 里」，那 4 个 2026-09-19 已全部补齐、
        // 那个包也不是唯一来源，见 `资料/AnimFX_实现与接线.md` §11.4），不该再有任何解不出的 cue。
        // ⚠️ 断言口径保持「除白名单外 == 0」而不是「总数 == 0」：将来为「原版本身缺」加白名单时
        //    才不会**每次全红** —— 而一直红的断言等于没有断言。
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

        // ---- ③d 🆕 2026-10-23（R9）：`rawSignal` 信号表（原版 `Cinemachine.NoiseSettings`）----
        //   判据 → `资料/普查产出_1023b/R9_rawSignal_算式.md`（机器码级算式）
        //        + `资料/普查产出_1023b/特效线_执行报告.md` §二（逐轴 × 3 octave 全表）
        //   这一段只验**数据与解析**（表在不在、5 个带波形的 preset 各自落到哪份、默认给谁）；
        //   **算式**本身由 `BattleScene` 的 ⑦ 节逐点核（那里能用 `CardFeel.ShakeNoise01` 复算）。
        {
            var dflt = WFModuleScreenShake.DefaultSignal;
            Check(dflt != null && dflt.name == "Warpforge 6D Shake",
                  $"R9 默认信号 = `{dflt?.name}`（原版场景 `CinemachineImpulseSource.m_RawSignal`"
                + " = `Warpforge 6D Shake`，pathID 1276427422462347950）");

            var sigs = WFModuleScreenShake.Signals;
            Check(sigs != null && sigs.Count == 4,
                  $"R9 信号表收到 {sigs?.Count ?? 0} 份（原版 `bundle_tweenandshakes` 里 `Warpforge 6D Shake*` 共 4 份，"
                + "2 份被 preset 引用、2 份没有）");
            int badShape = 0;
            foreach (var s in sigs ?? new List<WFModuleScreenShake.SignalRec>().AsReadOnly())
            {
                if (s == null || s.name == null || s.position == null || s.position.Length != 3) { badShape++; continue; }
                for (int i = 0; i < s.position.Length; i++)
                {
                    var o = s.position[i];
                    // Y 轴是我们 2D 版**唯一**采的那一列 ⇒ 它必须有频率/幅度，且 Constant 只能是 0/1
                    if (o == null || o.fy <= 0f || o.ay <= 0f || (o.cy != 0 && o.cy != 1)) badShape++;
                }
            }
            Check(badShape == 0,
                  $"R9 每份信号都有 3 个 octave、`PositionNoise[*].Y` 的 f/a > 0 且 `Constant ∈ {{0,1}}`（异常 {badShape} 处）");

            // 4 份信号的 **Y 列必须逐位相同** —— 这是「2D 版只采 Y、且换信号不改变竖直波形」的前提。
            // ⛔ 一旦哪天原版数据里出现不同的 Y 列，这条会红：那时「只采 Y」的简化就要重新评估。
            int yDiff = 0;
            if (sigs != null && sigs.Count > 0 && sigs[0].position != null)
                foreach (var s in sigs)
                {
                    if (s == null || s.position == null || s.position.Length != sigs[0].position.Length) { yDiff++; continue; }
                    for (int i = 0; i < s.position.Length; i++)
                    {
                        var a = sigs[0].position[i];
                        var b = s.position[i];
                        if (a == null || b == null) { yDiff++; continue; }
                        if (Mathf.Abs(b.fy - a.fy) > 1e-5f || Mathf.Abs(b.ay - a.ay) > 1e-6f || b.cy != a.cy) yDiff++;
                    }
                }
            Check(yDiff == 0, $"R9 4 份信号的 `Position.Y` 三条带**逐位相同**（差异 {yDiff} 处）"
                            + " ⇒ 2D 竖直波形对全部 17 个 preset 一致，差别只在 `frequency`/幅度/方向");

            // 🆕 **2026-10-23（R9 尾账）：`OrientationNoise` 也进表了**（`CardFeel.ShakeOrientation` 要用）。
            //   判据：两份 `…No rotation` 的名字就写着 no rotation ⇒ `orientation` 必须是**空**；
            //   另外两份（`Warpforge 6D Shake` / `…One Axis`）各 3 个 octave，且**逐位相同**
            //   （= 「换信号不改角度波形」的前提，同 Y 列那条）。
            int oriBad = 0;
            foreach (var s in sigs)
            {
                if (s == null || s.name == null) continue;
                bool noRot = s.name.IndexOf("No rotation", System.StringComparison.Ordinal) >= 0;
                int n = s.orientation == null ? 0 : s.orientation.Length;
                if (noRot) { if (n != 0) oriBad++; }
                else
                {
                    if (n != 3) { oriBad++; continue; }
                    for (int i = 0; i < n; i++)
                    {
                        var o = s.orientation[i];
                        if (o == null || o.ax <= 0f || o.ay <= 0f || o.az <= 0f
                            || (o.cx != 0 && o.cx != 1) || (o.cy != 0 && o.cy != 1) || (o.cz != 0 && o.cz != 1)) oriBad++;
                    }
                }
            }
            Check(oriBad == 0,
                  $"R9 尾账 `OrientationNoise` 形状：`…No rotation` 两名为空、另两名各 3 octave × X/Y/Z（异常 {oriBad} 处）");

            var oriSrc = sigs != null ? sigs.FirstOrDefault(s => s != null && s.name == "Warpforge 6D Shake") : null;
            int oriDiff = 0;
            if (oriSrc != null && oriSrc.orientation != null)
                foreach (var s in sigs)
                {
                    if (s == null || s == oriSrc || s.orientation == null || s.orientation.Length == 0) continue;
                    if (s.orientation.Length != oriSrc.orientation.Length) { oriDiff++; continue; }
                    for (int i = 0; i < s.orientation.Length; i++)
                    {
                        var a = oriSrc.orientation[i];
                        var b = s.orientation[i];
                        if (a == null || b == null) { oriDiff++; continue; }
                        if (Mathf.Abs(b.fx - a.fx) > 1e-5f || Mathf.Abs(b.ax - a.ax) > 1e-6f
                            || Mathf.Abs(b.fy - a.fy) > 1e-5f || Mathf.Abs(b.ay - a.ay) > 1e-6f
                            || Mathf.Abs(b.fz - a.fz) > 1e-5f || Mathf.Abs(b.az - a.az) > 1e-6f
                            || b.cx != a.cx || b.cy != a.cy || b.cz != a.cz) oriDiff++;
                    }
                }
            Check(oriDiff == 0,
                  $"R9 尾账 两份带角度的信号 `OrientationNoise` **逐位相同**（差异 {oriDiff} 处）"
                + " ⇒ 换信号不改角度波形（同 `Position.Y` 那条的对称断言）");

            // preset → 信号：5 个非空（3 个 `One Axis` + 2 个 `No rotation`），其余 12 个落默认
            var expectSignal = new Dictionary<string, string>
            {
                { "Shake Forward Pull",   "Warpforge 6D Shake One Axis" },
                { "Shake Land on ground", "Warpforge 6D Shake One Axis" },
                { "Shake Upwards Hand",   "Warpforge 6D Shake One Axis" },
                { "Shake UI Earthquake",  "Warpforge 6D Shake No rotation" },
                { "Shake UI Hit",         "Warpforge 6D Shake No rotation" },
            };
            int mapBad = 0;
            foreach (var kv in expectSignal)
            {
                var r = WFModuleScreenShake.Resolve(new WFModuleScreenShake.ShakeEntry { preset = kv.Key });
                if (r.signal == null || r.signal.name != kv.Value) { mapBad++; Debug.LogError(P + $"  ✗ R9 `{kv.Key}` 的信号是 `{r.signal?.name}`，应为 `{kv.Value}`"); }
            }
            Check(mapBad == 0, $"R9 5 个带 `rawSignal` 的 preset 各自解析到正确的信号表（错 {mapBad} 个）");

            int dfltBad = 0;
            foreach (var n in new[] { "Shake Hit Small", "Shake Big", "Shake Hit Medium" })
            {
                var r = WFModuleScreenShake.Resolve(new WFModuleScreenShake.ShakeEntry { preset = n });
                if (r.signal == null || r.signal.name != "Warpforge 6D Shake") dfltBad++;
            }
            Check(dfltBad == 0, $"R9 没有 `rawSignal` 的 preset（抽查 3 个）落到**默认信号**（错 {dfltBad} 个）");
            Check(WFModuleScreenShake.MissingSignals.Count == 0,
                  $"R9 没有「preset 引用了但表里没有」的信号（缺 {WFModuleScreenShake.MissingSignals.Count} 个："
                + string.Join(" / ", WFModuleScreenShake.MissingSignals) + "）");
        }

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

        // ---- ⑥ 卡背（`WFModuleCardback`）—— 🆕 2026-09-19 接的线，这里钉住它 ----
        //
        // 为什么单开一段：这条线**原来是死的**（`WFModuleCardback.CardbackResolver` 全工程
        // 只有声明、从没赋值 ⇒ `ResolveCardback()` 恒 null ⇒ `Initialize` 在早退那支上
        // **一个粒子都不改**，18 个效果 / 30 个实例全都不出声）。两处修法见
        // `资料/普查产出_0918/孤儿待办_五条查证.md` §一：① 运行期接回调（`BattleDriver`）
        // ② 导出器补 `textureSheetAnimation` 的 sprite 列表。
        // ⚠️ 这一段**只验模块本身**（回调有没有被消费、拿不到时会不会如实报）——
        //    「导出器有没有把 tsa 的 sprite 导全」由「导出报告 + 资产引用」那一侧看，
        //    不在这里（那要读 prefab 资产，属于 EffectExporter 的账）。
        {
            var prevResolver = WFModuleCardback.CardbackResolver;
            WFModuleCardback.ResetDiagnostics();
            var host = new GameObject("chk_cardback_host");
            var psGo = new GameObject("chk_cardback_ps");
            var tex = new Texture2D(4, 4);
            try
            {
                psGo.transform.SetParent(host.transform, false);
                var ps = psGo.AddComponent<ParticleSystem>();
                var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
                // 工程里的一张**真 Sprite 资产**（导出器产的那批 `*_sprite.png` 是 Sprite 导入型）——
                // 用来对照「运行时造的 sprite」与「资产 sprite」在 `AddSprite` 下行为是否一样。
                const string assetPath = "Assets/WarpforgeVFX/Textures/Atlas_trait_icon_armour_sprite.png";
                var spriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                // ① 回调被调到 + `isPlayer` 真的传下去了（原版实参是 `actingCard.isPlayer`）
                bool asked = false, askedIsPlayer = false;
                WFModuleCardback.CardbackResolver = isPlayer =>
                { asked = true; askedIsPlayer = isPlayer; return spriteAsset != null ? spriteAsset : sprite; };
                var mod = host.AddComponent<WFModuleCardback>();
                mod.targets = new[] { ps };
                mod.Initialize(null);
                Check(asked, "卡背：★ 回调**被调到了**（没挂 `CardbackResolver` 时它恒返 null，这条就是那个洞）");
                Check(askedIsPlayer == false,
                      "卡背：★ `isPlayer` 传下去了（没有卡上下文时按 false 走 —— 原版那个参数就是 `actingCard.isPlayer`）");
                // 🔴 **判据是「列表里那一张**就是**卡背」，不是「`spriteCount` 涨了」**
                //    （2026-09-19 量出来的语义，别照直觉写）：
                //    新建的 `ParticleSystem` 的 tsa 列表里**已经有一条占位 sprite**（`spriteCount` 一开始就是 1），
                //    而 `AddSprite` **第一次是替换掉那条占位、之后才是追加**。
                //    实测（同一趟自检里的四个对照夹具）：设完 enabled/mode 连加两次 ⇒ `1 → 1 → 2`；
                //    先加后设 ⇒ `1 → 1`。⇒ **只看计数会判错**（替换那一趟计数不动，但图是对的）。
                var gotSprite = ps.textureSheetAnimation.spriteCount > 0
                              ? ps.textureSheetAnimation.GetSprite(0) : null;
                Check(gotSprite != null && gotSprite == (spriteAsset != null ? spriteAsset : sprite),
                      $"卡背：★ sprite **真的进了 `textureSheetAnimation` 的列表**"
                      + $"（第 0 条 =「{(gotSprite == null ? "null" : gotSprite.name)}」）");
                Check(ps.textureSheetAnimation.enabled
                      && ps.textureSheetAnimation.mode == ParticleSystemAnimationMode.Sprites,
                      "卡背：tsa 被设成 `enabled` + `Sprites` 模式（原版 `Initialize` 就这两步）");
                Check(WFModuleCardback.MissingCardback == 0, "卡背：这一趟没有「拿不到卡背」");

                // ①bis 🔴 **真游戏那条路给的是「运行时 `Sprite.Create` 出来的」**（`BattleDriver.CardBackSprite`）
                //     ⇒ 单独拿一个全新的粒子系统跑一遍**模块路径**，确认运行时造的 sprite 也进得去
                //     （资产 sprite 与运行时 sprite 在 `AddSprite` 下行为不一样 —— 2026-09-19 实测过）。
                {
                    var goR = new GameObject("chk_cb_runtime");
                    try
                    {
                        goR.transform.SetParent(host.transform, false);
                        var psR = goR.AddComponent<ParticleSystem>();
                        WFModuleCardback.CardbackResolver = _ => sprite;      // ← 运行时造的那张
                        var modR = goR.AddComponent<WFModuleCardback>();
                        modR.targets = new[] { psR };
                        modR.Initialize(null);
                        var gotR = psR.textureSheetAnimation.spriteCount > 0
                                 ? psR.textureSheetAnimation.GetSprite(0) : null;
                        Check(gotR == sprite,
                              $"卡背：**运行时 `Sprite.Create` 的那张也进得去**"
                              + $"（第 0 条 =「{(gotR == null ? "null" : gotR.name)}」）—— 真游戏走的就是这一条");
                    }
                    finally { Object.DestroyImmediate(goR); }
                }

                // ② 拿不到卡背 ⇒ 按原版处理（计数 + **一个粒子都不改**，这是「不许静默」那条）
                //    ⚠️ 判据取**第 0 条 sprite 本体没变** —— 只比 `spriteCount` 会漏（`AddSprite`
                //    第一次是**替换**占位项，计数本来就不动，见上面 ① 那段注释）。
                var spriteBefore2 = ps.textureSheetAnimation.spriteCount > 0
                                  ? ps.textureSheetAnimation.GetSprite(0) : null;
                WFModuleCardback.CardbackResolver = _ => null;
                var mod2 = psGo.AddComponent<WFModuleCardback>();
                mod2.targets = new[] { ps };
                mod2.Initialize(null);
                Check(WFModuleCardback.MissingCardback == 1,
                      "卡背：拿不到时**记账**（`MissingCardback` = 1 —— 原版这一支是 `LogError` 后直接 return）");
                var spriteAfter2 = ps.textureSheetAnimation.spriteCount > 0
                                 ? ps.textureSheetAnimation.GetSprite(0) : null;
                Check(spriteAfter2 == spriteBefore2,
                      "卡背：……而且**一个粒子都没改**（原版 `__Initialize.c:44-49` 就是直接 return）");

                // ③ 数组为空 ⇒ 原版另一条 `LogError` 路径（`Can't find particle system to assign card frame`）
                WFModuleCardback.CardbackResolver = _ => sprite;
                var mod3 = psGo.AddComponent<WFModuleCardback>();
                mod3.targets = new ParticleSystem[0];
                mod3.Initialize(null);
                Check(WFModuleCardback.EmptyArrayCount == 1,
                      "卡背：粒子系统数组为空时**记账**（`EmptyArrayCount` = 1，原版那条 LogError）");

                // ④ 库里那个模块的引用路径解得开 —— 已由上面 ③b 的全量路径覆盖，这里只报个数
                int cbMods = 0;
                foreach (var e in entries)
                {
                    if (e == null || e.modules == null) continue;
                    foreach (var d in e.modules) if (d != null && d.kind == "AnimFXModuleCardback") cbMods++;
                }
                Check(cbMods > 0, $"卡背：库里有 {cbMods} 个 `AnimFXModuleCardback` 模块实例（原版 30 实例 / 18 效果）");
            }
            finally
            {
                WFModuleCardback.CardbackResolver = prevResolver;
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(tex);
            }
        }

        // ================================================================
        // 🆕 2026-10-23（特效线 A1 R23 / R24）—— 两条「销毁时机」的回归断言
        //   判据 → `资料/普查产出_1022/A1_自造与兜底_特效动画.md` R23 / R24
        // ================================================================

        // ---- R23：原版 `AnimFXController.DoDestroy` 「有模块就不销毁、销毁权交给模块」 ----
        //   实例 = `Pray_Idle`（全工程唯一的 `AnimFXModuleDoDestroyAnimation`，`timeToDestroy = 1.0`）。
        {
            var e = entries.FirstOrDefault(x => x != null && x.name == "Pray_Idle");
            Check(e != null, "R23：库里找得到 `Pray_Idle`（唯一的 DoDestroyAnimation 实例）");
            if (e != null)
            {
                var p = WarpforgeEffectPlayer.Play(e.name, null, Vector3.zero, 1f);
                Check(p != null, "R23：`Pray_Idle` 起了");
                if (p != null)
                {
                    // ⚠️ 走**原版那条路**：`AnimFXController.DoDestroy`（调用方 = `BattleManager.RemoveDestroyableAnims`）——
                    //    ⛔ 不是 `Exit()` 的计时销毁（那条**一定**自毁，不交接；判据 `Battle/AnimFXController.cs:318-345`）。
                    var mod = p.GetComponentInChildren<WFModuleDoDestroyAnimation>(true);
                    Check(mod != null, "R23：`Pray_Idle` 上装着 `WFModuleDoDestroyAnimation`");
                    p.DoDestroy();
                    // 🔴 灭自证：**只看「没销毁」不够** —— 若 `TakesOverDestroy` 一直是 false，
                    //    这一跳会立刻销毁；所以必须再验「过了一会儿它真的被模块收掉」。
                    Check(p != null, "R23：有模块接管 ⇒ **`DoDestroy()` 之后对象还在**（原版 controller 不自毁）");
                    if (p != null) p.Tick(1.5f);             // timeToDestroy = 1.0
                    Check(p == null, "R23：……而模块在 `timeToDestroy`(1s) 之后**自己销毁**（不是永久泄漏）");
                }
            }
        }

        // ---- R24：`AnimFXModuleAnimation.AnimationExitEnd`（原版由 Exit clip 的动画事件调）----
        //   现在由 `ModuleTick` 按 clip 真实时长数到结束再调；实例 = `ShieldTraitEffect_Idle`。
        {
            var e = entries.FirstOrDefault(x => x != null && x.name == "ShieldTraitEffect_Idle");
            Check(e != null, "R24：库里找得到 `ShieldTraitEffect_Idle`");
            var exitClip = WarpforgeAnimatorBridge.TryGetClip("ShieldTraitEffect_Idle|Exit");
            Check(exitClip != null, "R24：`ShieldTraitEffect_Idle|Exit` 片段取得到（否则这条断言没意义）");
            if (e != null && exitClip != null)
            {
                var p = WarpforgeEffectPlayer.Play(e.name, null, Vector3.zero, 1f);
                Check(p != null, "R24：`ShieldTraitEffect_Idle` 起了");
                if (p != null)
                {
                    var mod = p.GetComponentInChildren<WFModuleAnimation>(true);
                    var modGo = mod != null ? mod.gameObject : null;
                    Check(mod != null && modGo != null, "R24：找得到 `WFModuleAnimation` 模块（挂在某个节点上）");
                    p.Tick(e.AutoLifetime() + 0.01f);        // 进 Exit ⇒ 播 Exit clip
                    p.Tick(exitClip.length + 0.05f);         // 数到片段结束
                    Check(mod == null || modGo == null,
                          $"R24：Exit 片段（{exitClip.length:F2}s）播完 ⇒ `AnimationExitEnd` 把宿主节点收掉了");
                    if (p != null) p.Kill();
                }
            }
        }

        // ---- R25：`AnimFXModuleEvent` 的 UnityEvent 真的接上了（原版 `m_Calls` = 2 条）----
        //   判据 → `资料/普查产出_1023/E_R25事件通道.md`
        //   🔴 灭自证：只断 `FiredCount == 1` 的话，「Invoke 了一个什么都不做的 listener」也能绿
        //   ⇒ 必须再断**副作用真的发生了**（`cm.materialInstance != null`）。
        {
            Check(WFModuleEvent.SelfCheck() == "",
                  "R25：事件表自洽" + (WFModuleEvent.SelfCheck() == "" ? "" : "（" + WFModuleEvent.SelfCheck() + "）"));
            Check(WFModuleEvent.TableCount == 2,
                  $"R25：表里 2 条（原版 `VanguardIdleEffect` 的 `m_Calls`；实测 {WFModuleEvent.TableCount}）");
            Check(WFModuleEvent.TableCovers("VanguardIdleEffect", 0, WFActionStart.Exit),
                  "R25：表覆盖 `VanguardIdleEffect#0 @ Exit`");

            var r25e = entries.FirstOrDefault(x => x != null && x.name == "VanguardIdleEffect");
            Check(r25e != null, "R25：库里找得到 `VanguardIdleEffect`");
            if (r25e != null)
            {
                WFModuleEvent.ResetDiagnostics();
                var p = WarpforgeEffectPlayer.Play(r25e.name, null, Vector3.zero, 1f);
                Check(p != null, "R25：`VanguardIdleEffect` 起了");
                if (p != null)
                {
                    var ev = p.GetComponentInChildren<WFModuleEvent>(true);
                    var cm = p.GetComponentInChildren<WFModuleChangeMaterial>(true);
                    Check(ev != null, "R25：`WFModuleEvent` 装配上了");
                    Check(cm != null, "R25：`WFModuleChangeMaterial` 装配上了（这条事件的第 1 个目标）");
                    p.Exit();                                  // preventDestroy=1 ⇒ 只能手动 Exit
                    Check(WFModuleEvent.UnresolvedCount == 0,
                          "R25：表里 2 条**一条不落**全接上（未接 " + WFModuleEvent.UnresolvedCount + "："
                          + string.Join(" / ", WFModuleEvent.WiringProblems) + "）");
                    Check(WFModuleEvent.FiredCount == 1,
                          $"R25：Exit 时事件真的 Invoke 了 1 次（实测 {WFModuleEvent.FiredCount}）");
                    Check(cm != null && cm.materialInstance != null,
                          "R25：`ToggleMaterial(true)` 真的换了材质（`materialInstance` 非空）");
                    p.Kill();
                }
            }
        }

    // ==================================================================
    //  🔴 2026-10-23（`T1` · Trait 全量权威化 / **去兜底**）—— 四组断言
    //  ------------------------------------------------------------------
    //  用户 2026-10-23 指令：「安卓存档有 CardTrait SO，把我们兜底之类的东西都去掉，
    //  **全面使用 CardTrait SO**」。对应到代码里三件事：
    //    ① 运行时逐卡特效表（`Resources/card_vfx_by_card.json`）**只剩 `authoritative`**；
    //    ② 5 条 `penitence`（`SOR8/10/21/27/32`）**不进播放表**，但**必须出声**；
    //    ③ `TraitFrames`（状态框两本）与 `TraitParticles`（FromCode 粒子）**由
    //       `Resources/trait_effect_bindings.json` 驱动** —— 里面每一行都能追到
    //       `DefinedTrait / TriggerType / effectAnim GUID / CardAnim / prefab`。
    //  ⚠️ **期望值一律从数据独立重算**（同一份 JSON，⛔ 不从被测实现读回 —— 那是自证）。
    //  ⚠️ 字面量那几条（`StealthEffect` / `Oath_Effect` …）**摘自原版资产**（`CardTrait` SO），
    //     写死是有意的：它们是「回归靶子」，实现改了就得红。
    // ==================================================================
    static void CheckTraitAuthority()
    {
        Debug.Log(P + "--- T1 · Trait 全量权威化（去兜底）---");

        // ⚠️ **先把逐卡表载进来**：`CardTableNonAuthoritativeRows` / `CardTableNotPlayed` 是**载表时**写的，
        //    不载就读到 0 ⇒ 下面那几条会变成「空断言自己绿」（本仓踩过的坑，见 `A1731` 那条注释）。
        CardPresentation.VfxMap.EnsureLoaded();
        Check(CardPresentation.VfxMap.CardTableCards > 1000,
              $"前提：逐卡表**载进来了**（{CardPresentation.VfxMap.CardTableCards} 张卡 / "
            + $"{CardPresentation.VfxMap.CardTableEntries} 行）—— 载不到的话下面「非权威 = 0」全是没有内容的绿");

        // ---------- ① 运行时逐卡表：非 `authoritative` 的行必须 = 0 ----------
        CardPresentation.VfxMap.RtFile vfxDto = null;
        {
            var vfxAsset = Resources.Load<TextAsset>(CardPresentation.VfxMap.CardTableResourcePath);
            if (vfxAsset != null)
            {
                try { vfxDto = JsonUtility.FromJson<CardPresentation.VfxMap.RtFile>(vfxAsset.text); }
                catch (System.Exception ex) { Debug.LogWarning(P + "① 读逐卡表失败：" + ex.Message); }
            }
            int rows = 0, nonAuth = 0;
            var confs = new Dictionary<string, int>();
            if (vfxDto != null && vfxDto.cards != null)
            {
                foreach (var card in vfxDto.cards)
                {
                    if (card == null || card.events == null) continue;
                    foreach (var ev in card.events)
                    {
                        if (ev == null || ev.es == null) continue;
                        foreach (var e in ev.es)
                        {
                            if (e == null || string.IsNullOrEmpty(e.n)) continue;
                            rows++;
                            var key = e.c ?? "(null)";
                            confs[key] = (confs.TryGetValue(key, out var n0) ? n0 : 0) + 1;
                            if (!string.Equals(key, CardPresentation.VfxMap.ConfAuthoritative,
                                               System.StringComparison.Ordinal)) nonAuth++;
                        }
                    }
                }
            }
            Check(rows > 2000,
                  $"① 前提：`Resources/{CardPresentation.VfxMap.CardTableResourcePath}.json` 里**读得出**逐卡特效行"
                + $"（实得 {rows} 行）—— 读成 0 行的话下面那条「非权威 = 0」就是**空断言自己绿**");
            Check(nonAuth == 0,
                  $"① 运行时逐卡表里**非 `authoritative` 的行 = 0**（实得 {nonAuth} 条 / 共 {rows} 行；"
                + $"conf 分布 {Describe(confs)}）"
                + " —— 🧨 把 `工具/gen_card_vfx_by_card.py` 里 `strip_unbacked_inference(...)` 那句删掉"
                + "（或把 `VfxMap.EnsureLoaded` 的第二道闸删掉）⇒ 17 条 `inferred-name` + 5 条 `unknown` 回来、本条红");
        }

        // ---------- ② 5 条 `penitence`：不播 + 出声；但 `ability` 那档的权威行**仍在** ----------
        {
            Check(CardPresentation.VfxMap.CardTableNonAuthoritativeRows == 0,
                  $"② 第二道闸：载入时被挡掉的**非权威行** = "
                + $"{CardPresentation.VfxMap.CardTableNonAuthoritativeRows}（应为 0 —— 数据侧已清空，这里是防回退的闸）");
            int npRows = 0, npWithWhy = 0;
            if (vfxDto != null && vfxDto.notPlayed != null)
            {
                foreach (var x in vfxDto.notPlayed)
                {
                    if (x == null) continue;
                    npRows++;
                    if (!string.IsNullOrEmpty(x.why)) npWithWhy++;      // 「出声」= 每条都带得出去的 `why`
                }
            }
            Check(npRows >= 5 && npWithWhy == npRows,
                  $"② `notPlayed[]` = {npRows} 条（改前表里那 22 条非权威行**一条不落**）、"
                + $"其中带 `why` 的 {npWithWhy} 条（必须**全部**带 —— 这就是「⛔ 不许静默」的落地）");

            var pen = new[] { "SOR8", "SOR10", "SOR21", "SOR27", "SOR32" };
            int inNotPlayed = 0, playedViaTrigger = 0, stillInAbility = 0;
            foreach (var id in pen)
            {
                if (CardPresentation.VfxMap.IsNotPlayed(id, CardPresentation.VfxMap.Trigger)) inNotPlayed++;
                var viaTrigger = CardPresentation.VfxMap.ResolveAllTimed(
                    CardPresentation.VfxMap.Trigger, "Sororitas", id, null, perCardOnly: true);
                for (int i = 0; i < viaTrigger.Length; i++)
                    if (viaTrigger[i].Name == "PenitenceEffect") playedViaTrigger++;
                var viaAbility = CardPresentation.VfxMap.ResolveAllTimed(
                    CardPresentation.VfxMap.Ability, "Sororitas", id, null, perCardOnly: true);
                bool has = false;
                for (int i = 0; i < viaAbility.Length; i++)
                    if (viaAbility[i].Name == "PenitenceEffect") has = true;
                if (has) stillInAbility++;
            }
            Check(inNotPlayed == 5,
                  $"② `penitence` 那 5 张（{string.Join("/", pen)}）的 `trigger` 档**都在 `notPlayed` 里**"
                + $"（实得 {inNotPlayed}/5）—— 🧨 把它们塞回事件桶或者干脆不记账 ⇒ 本条红");
            Check(playedViaTrigger == 0,
                  $"② 这 5 张**没有任何一条** `PenitenceEffect` 从 `trigger` 档播出去（实得 {playedViaTrigger} 条）"
                + " —— 🧨 把 `strip_unbacked_inference` 里那步「删重复」去掉 ⇒ 5 条回来、本条红");
            Check(stillInAbility == 5,
                  $"② **灭自证**：同一件 `PenitenceEffect` 在卡 SO 的 `ability` 档**仍然拿得到**"
                + $"（{stillInAbility}/5）—— 证明我们删掉的是「无依据的**触发行**」，"
                + "⛔ 不是「一怒之下把这张卡的动画整个删了」（那样上面那条会绿、这条会红）");
        }

        // ---------- ③ TraitFrames：两本字典由 `trait_effect_bindings.json` 驱动 ----------
        {
            Check(CardPresentation.TraitFrames.BindingsLoaded,
                  "③ 状态框绑定表**载进来了**（`Resources/trait_effect_bindings.json`）"
                + (CardPresentation.TraitFrames.BindingsLoaded ? ""
                   : " —— 载不到：" + CardPresentation.TraitFrames.BindingsProblem));

            var asset = Resources.Load<TextAsset>(CardPresentation.TraitFrames.BindingsResourcePath);
            CardPresentation.TraitFrames.BindingDoc dto = null;
            if (asset != null)
            {
                try { dto = JsonUtility.FromJson<CardPresentation.TraitFrames.BindingDoc>(asset.text); }
                catch (System.Exception ex) { Debug.LogWarning(P + "③ 读绑定表失败：" + ex.Message); }
            }
            int onPlay = 0, trigger = 0, fromCode = 0, rows = 0, mismatch = 0;
            int emptyOnBook = 0, leakedIntoBook = 0;
            if (dto != null && dto.rows != null)
            {
                foreach (var r in dto.rows)
                {
                    if (r == null || string.IsNullOrEmpty(r.trait)) continue;
                    rows++;
                    bool book = r.triggerType == 2 || r.triggerType == 3 || r.triggerType == 6;
                    if (string.IsNullOrEmpty(r.prefab))
                    {
                        // 原版 `effectAnim` 是空引用的那几条**本来就没动画** ⇒ 不该进两本字典。
                        // ⚠️ 断的口要**对着字典**断（不是「数据里没有这种行」—— 数据里确实有 7 条）。
                        if (book)
                        {
                            emptyOnBook++;
                            // 🔴 2026-10-23 订正：第二个参数语义是 `trigger`（`true` = **触发那一本**），
                            //    原来传的是 `r.triggerType == 2`（= OnPlay 才 true）⇒ **两本查反了**，
                            //    空 prefab 的行永远查不到 ⇒ 这条「不许漏进字典」曾是**恒绿的假断言**。
                            var g0 = CardPresentation.TraitFrames.BindingsFor(r.trait, r.triggerType != 2);
                            for (int i0 = 0; i0 < g0.Length; i0++)
                                if (g0[i0].TriggerType == r.triggerType && string.IsNullOrEmpty(g0[i0].Prefab))
                                    leakedIntoBook++;
                        }
                        continue;
                    }
                    if (r.triggerType == 2) onPlay++;
                    else if (r.triggerType == 3 || r.triggerType == 6) trigger++;
                    else if (r.triggerType == 5) fromCode++;

                    if (!book) continue;
                    // 逐行核对：数据里这一行必须在**对应那一本**里，且 TriggerType/GUID/DefinedTrait/prefab 四项全等
                    // 🔴 2026-10-23 订正：`BindingsFor` 第二参 = `trigger`（true = 触发那一本）。
                    //    原来传 `r.triggerType == 2` ⇒ 两本查反 ⇒ 16 条 OnPlay 行 + 25 条触发(3/6) 行
                    //    全部「查不到」= 41 条假红（数据本身没问题）。
                    var got = CardPresentation.TraitFrames.BindingsFor(r.trait, r.triggerType != 2);
                    bool found = false;
                    for (int i = 0; i < got.Length; i++)
                    {
                        if (got[i].TriggerType != r.triggerType) continue;
                        if (got[i].Prefab != r.prefab) continue;
                        if ((got[i].EffectGuid ?? "") != (r.effectGuid ?? "")) continue;
                        if (got[i].DefinedTrait != r.definedTrait) continue;
                        found = true;
                        break;
                    }
                    if (!found) mismatch++;
                }
            }
            Check(rows > 60 && onPlay > 10 && trigger > 20 && fromCode > 5,
                  $"③ 数据规模：{rows} 条 `TraitEffect` 行 / OnPlay(2) {onPlay} 条 / 触发(3·6) {trigger} 条 / "
                + $"FromCode(5) {fromCode} 条 —— 规模突然变小 = 生成器读错了源（原版共 75 行，少于 60 行就红）");
            Check(emptyOnBook > 0 && leakedIntoBook == 0,
                  $"③ 原版 `effectAnim` 是**空引用**的行**一条都没进两本字典**（数据里有 {emptyOnBook} 条这种行，"
                + $"混进字典的 {leakedIntoBook} 条）—— 那几条（`combo`/`daemonicAllegiance`/`large`/`mekaniak`/"
                + "`resolution`/`firstStrike`/`piercing`）原版本来就不演，⛔ 不是缺口；"
                + "🧨 把 `EnsureBindings` 里 `if (Prefab.Length == 0) continue;` 删掉 ⇒ 字典里多出空 prefab、本条红");
            Check(mismatch == 0,
                  "③ **逐行**：数据里每一条 (trait · TriggerType · GUID · DefinedTrait · prefab) 都能在"
                + $" `TraitFrames` 的两本字典里**原样查到**（对不上 {mismatch} 条）"
                + " —— 🧨 把 `TraitFrames.EnsureBindings` 换回任何**手写表** ⇒ 行数与 prefab 立刻对不上、本条红");

            // ---- 灭自证：手写表**被数据推翻**的那几条（反向断，⛔ 不是「新写法对」那种自证）----
            Check(CardPresentation.TraitFrames.PrefabFor("stealth", false) == "StealthEffect",
                  "③ `stealth` 的 OnPlay 框 = `StealthEffect`（原版 `StealthTraitPlay` → 它；手写表当年也绑对了，保号）");
            Check(CardPresentation.TraitFrames.PrefabFor("vanguard", false) == "VanguardIdleEffect",
                  "③ `vanguard` 的 OnPlay 框 = `VanguardIdleEffect`（原版 `VanguardIdleAnim`；"
                + "⛔ 不是具名字段 `vanguardFrame` 那件 `Vanguard Frame Animated VAT`）");
            Check(CardPresentation.TraitFrames.PrefabFor("swarm", false) == null,
                  "③ **灭自证**：`swarm` 在 OnPlay 那本**必须是 null** —— 原版 `Swarm.asset` 只有一条 "
                + "FromCode(5)（`SwarmTraitFromCode`），**没有 OnPlay effect**。"
                + "🧨 把手写表里那一行 `{ Swarm, \"Swarm_Trigger_OnTarget\" }` 抄回来 ⇒ 本条立刻红");
            Check(CardPresentation.TraitFrames.PrefabFor("slay", false) == null
                  && CardPresentation.TraitFrames.PrefabFor("slay", true) == "SlayEffect",
                  "③ **灭自证**：`slay` **不在 OnPlay 那本**、而在**触发那本** —— 原版 `Slay.asset` 唯一的 "
                + "`TraitEffect` 是 Trigger(3) → `Slay_Effect_target` → `SlayEffect`。"
                + $"(实得 OnPlay={(CardPresentation.TraitFrames.PrefabFor("slay", false) ?? "null")} · "
                + $"触发={(CardPresentation.TraitFrames.PrefabFor("slay", true) ?? "null")})"
                + " 🧨 把「附魔加身演一次」那条手写绑定抄回 OnPlay 那一本 ⇒ 本条红");
            Check(CardPresentation.TraitFrames.PrefabFor("oath", true) == "Oath_Effect"
                  && CardPresentation.TraitFrames.BlocksForWarlord("oath")
                  && CardPresentation.TraitFrames.TriggerBindingsFor("oath").Length == 1
                  && CardPresentation.TraitFrames.TriggerBindingsFor("oath")[0].DefinedTrait == 1275
                  && CardPresentation.TraitFrames.TriggerBindingsFor("oath")[0].TriggerType == 3,
                  "③ `oath`：一条 Trigger(3) · DefinedTrait 1275 · `playIfWarlord = false`"
                + "（督军不播）· prefab `Oath_Effect` —— 四项全来自 `trait_collection.json`");
            var bt = CardPresentation.TraitFrames.TriggerBindingsFor("bloodthirst");
            Check(bt.Length == 2
                  && bt[0].TriggerType == 3 && bt[0].Prefab == "BloodThirstEffect_Gain"
                  && bt[1].TriggerType == 6 && bt[1].Prefab == "BloodThirstEffect_Idle",
                  "③ `bloodthirst` 两行（3→`BloodThirstEffect_Gain` · 6→`BloodThirstEffect_Idle`，"
                + $"按资产顺序；实得 {bt.Length} 行）—— 🧨 只留一行 / 换成名字更像的 `BloodThirstEffect` ⇒ 红");
            int libMissing = 0;
            if (dto != null && dto.rows != null && WarpforgeEffectLibrary.Available)
            {
                foreach (var r in dto.rows)
                {
                    if (r == null || string.IsNullOrEmpty(r.prefab)) continue;
                    if (!WarpforgeEffectLibrary.Instance.TryGet(r.prefab, out _)) libMissing++;
                }
            }
            Check(libMissing == 0,
                  $"③ 绑定表里每一件 prefab **都在效果库里**（不在的 {libMissing} 件）"
                + " —— 🧨 改名 / 重导 prefab 没重建效果库 ⇒ 状态框静默挂不上、本条红");
        }

        // ---------- ④ TraitParticles：FromCode 那档同样由数据驱动 ----------
        {
            var asset = Resources.Load<TextAsset>(CardPresentation.TraitFrames.BindingsResourcePath);
            CardPresentation.TraitFrames.BindingDoc dto = null;
            if (asset != null)
            {
                try { dto = JsonUtility.FromJson<CardPresentation.TraitFrames.BindingDoc>(asset.text); }
                catch (System.Exception) { }
            }
            int want = 0, mismatch = 0;
            if (dto != null && dto.rows != null)
            {
                foreach (var r in dto.rows)
                {
                    if (r == null || r.triggerType != 5 || string.IsNullOrEmpty(r.prefab)) continue;
                    want++;
                    var got = CardPresentation.TraitParticles.PrefabFor(r.trait);
                    if (!string.Equals(got, r.prefab, System.StringComparison.Ordinal)) mismatch++;
                }
            }
            Check(want > 5 && CardPresentation.TraitParticles.BindingCount == want,
                  $"④ FromCode 粒子表由数据驱动：数据里 {want} 条、`TraitParticles` 载进来 "
                + $"{CardPresentation.TraitParticles.BindingCount} 条（原版 75 条 `TraitEffect` 里 TriggerType==5 的是 10 条）");
            Check(mismatch == 0,
                  $"④ 数据里每一条 FromCode 行的 prefab 与 `TraitParticles.PrefabFor` **逐条相等**（不等 {mismatch} 条）"
                + " —— 🧨 把手写表抄回来（`StompEffect` / `FerocityEffect`）⇒ 立刻对不上、本条红");
            Check(CardPresentation.TraitParticles.PrefabFor("stomp") == "Attack_Stomp",
                  "④ **灭自证**：`stomp` 的 FromCode 粒子 = `Attack_Stomp`"
                + $"（原版 `BluntImpact_Stomp_Forward` → 它；实得「{CardPresentation.TraitParticles.PrefabFor("stomp")}」）"
                + " —— 🧨 抄回手写表的 `StompEffect` ⇒ 本条红（`StompEffect` 不是任何一条 `TraitEffect` 的 prefab）");
            Check(CardPresentation.TraitParticles.PrefabFor("huntMark") == "HuntMark_DamageEffect",
                  "④ **灭自证**：`huntMark` **有** FromCode 粒子 = `HuntMark_DamageEffect`"
                + $"（实得「{CardPresentation.TraitParticles.PrefabFor("huntMark")}」）"
                + " —— 改前它被「故意留白」（按名字找不到绑定）；原版 `HuntMark.asset` 有一条 FromCode(5)"
                + " `HuntMark Damage Anim` ⇒ 🧨 把它改回 null（留白）⇒ 本条红");
            Check(CardPresentation.TraitParticles.PrefabFor("ferocity") == "Ferocity_RecallToDeck",
                  "④ `ferocity` 的 FromCode 粒子 = `Ferocity_RecallToDeck`"
                + $"（实得「{CardPresentation.TraitParticles.PrefabFor("ferocity")}」）"
                + " —— ⛔ `FerocityEffect` 是它 **Start(0)** 那条的 prefab，不是这一档的");
        }
    }

    static string Describe(Dictionary<string, int> d)
    {
        if (d == null || d.Count == 0) return "(空)";
        return string.Join(" · ", d.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + "=" + kv.Value));
    }

        CheckTraitAuthority();

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
