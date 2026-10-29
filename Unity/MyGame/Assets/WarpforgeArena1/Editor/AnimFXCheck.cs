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

        // ---- ③c 🆕（`B99_W4` · 自检 #16）：`effect_extras` 表的**全量 sweep** ----
        //   判据 → `资料/完全复刻/判据/B99_R3b_施工单_其余按文件.md` §2·11。
        //   ③b 只扫 AnimFX **模块**那一族（`__node` / `@node:`）；`effect_extras` 是**另一族** ——
        //   特效 prefab 上那些**非 AnimFX 模块**的原版组件（`SyncParticleSeeds` / `DestroyByTime` /
        //   `MaterialFlickerEffect` …）。此前这一族**一条全量断言都没有**（只抽样看过 `EffectCount > 60`，
        //   以及「`Unimplemented` 不增」），**`NodeMisses` 一条断言都没有**。
        //   现读表 = 88 件 / 164 处挂载。⛔ **别把 88 / 164 写死**：分母全部从 `effect_extras.json` 现读。
        //   ⚠️ `Attached` / `NodeMisses` / `Unimplemented` 都是**累计量** ⇒ 必须先 `ResetDiagnostics()`，
        //      否则 ③ 那一批 `Play` 的挂载会混进来，`Attached == 164` 就成了「恒大于」的假绿。
        //   ⚠️ 这一段**只 Play + Kill，不 Tick**：额外件的挂载发生在 `WarpforgeEffectPlayer.Init → AttachAll`
        //      （`AddComponent` 当场跑 `OnEnable`/`Apply`），不需要帧循环；不 Tick 也顺便避开
        //      `WFExtraShakeAndSound` 在批处理里连播 88 条屏震/音效的副作用。
        //   🧨 灭自证：① 删掉 `WFEffectExtras.AttachAll` 里那句 `Attach(p, list[i])` ⇒ `Attached == 0` ⇒ 红；
        //      ② 把 `Attach` 里 `NodeMisses++` 那支改成静默 `return` ⇒ 挂载数掉、`Attached == 164` 红；
        //      ③ `exPlayed` 若不过滤 `null`（恒等于 `exRecs.Length`）⇒ 那条「全起得来」永远绿（⛔ 别这么写）。
        {
            WFEffectExtras.ResetDiagnostics();
            var exTa = Resources.Load<TextAsset>(WFEffectExtras.ResourcesPath);
            Check(exTa != null, "`effect_extras.json` 读得到（读不到 ⇒ 下面几条等于没查）");
            var exDoc = exTa != null ? JsonUtility.FromJson<AnimFxExtraDoc>(exTa.text) : null;
            var exRecs = (exDoc != null && exDoc.effects != null) ? exDoc.effects : new AnimFxExtraRec[0];
            int exMounts = 0;
            for (int i = 0; i < exRecs.Length; i++)
                if (exRecs[i] != null && exRecs[i].extras != null) exMounts += exRecs[i].extras.Length;

            // ---- 前提（**硬检查**）—— ⛔ 不写 `if (前提) Check(结论)`：前提不成立时那会静默跳过 ----
            Check(exRecs.Length > 0 && exMounts > 0,
                  $"（前提）表里现读到件数与挂载数（{exRecs.Length} 件 / {exMounts} 处）—— 否则下面等于没查");
            Check(WFEffectExtras.EffectCount == exRecs.Length,
                  $"组件读到的条目数 = json 的件数（{WFEffectExtras.EffectCount} vs {exRecs.Length}）"
                + "—— 少一件就是 `Ensure()` 漏读了（前面那条 ③b 覆盖不到这一族）");

            int exPlayed = 0, exNull = 0;
            for (int i = 0; i < exRecs.Length; i++)
            {
                if (exRecs[i] == null || string.IsNullOrEmpty(exRecs[i].effect)) continue;
                var ep = WarpforgeEffectPlayer.Play(exRecs[i].effect);
                if (ep == null) { exNull++; continue; }
                exPlayed++;
                ep.Kill();             // 批处理无帧循环 ⇒ 必须显式收，否则留一地实例
            }
            Debug.Log(P + $"  extras 全量 sweep：{exPlayed}/{exRecs.Length} 件起得来 · 挂载 "
                        + $"{WFEffectExtras.Attached}/{exMounts} · 节点未命中 {WFEffectExtras.NodeMisses} · "
                        + $"未实现 {WFEffectExtras.Unimplemented}");
            Check(exPlayed == exRecs.Length,
                  $"表里 **{exRecs.Length} 件特效全起得来**（实测 {exPlayed}，起不来 {exNull} 件 —— "
                + "起不来 = 效果库没这一条 / 那件 prefab 引用空了，见上面逐条警告）");
            Check(WFEffectExtras.NodeMisses == 0,
                  $"`AttachAll` 的**节点路径零失败**（失败 {WFEffectExtras.NodeMisses} 处 —— "
                + "非 0 = 那条 `node` 路径在工程 prefab 里对不上，见上面逐条警告）");
            Check(WFEffectExtras.Unimplemented == 0,
                  $"表里每一种 `kind` 都有等价组件（未实现 {WFEffectExtras.Unimplemented} 处）");
            Check(WFEffectExtras.Attached == exMounts,
                  $"挂载数 = 表里的 **{exMounts} 处**（实测 {WFEffectExtras.Attached}；"
                + "🧨 删掉 `AttachAll` 里那句 `Attach(p, list[i])` ⇒ 得 0 ⇒ 红）");
        }

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
            // 🔴 **2026-10-26（B9 · `X4` 就地订正，α：断言过期）**：`Resolve` 的第二参
            //   `usePresetSignal` **今天的默认是 `false`**（判据在 `WFModuleScreenShake.Resolve` 方法体
            //   那段机器码级注释里：`OverwriteCameraShakePreset.GetModifiedPreset` 只写 7 个字段、
            //   **从不写 `rawSignal`** ⇒ 池化副本的 `rawSignal` 恒 null ⇒ 原版恒播 `manager.defaultRawSignal`）。
            //   ⇒ 本组测的是「preset ↔ 它自己那份信号」的**映射表本身**，就必须**显式**要那一支
            //   （= 原版 `CameraShakePresetSO.Play()` 那条链；那条链今天没有调用者）；
            //   ⛔ 不许靠「顺手把默认值改回去」让它绿 —— 那正是 `G12` 修掉的东西（反向那条见下）。
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
                var r = WFModuleScreenShake.Resolve(new WFModuleScreenShake.ShakeEntry { preset = kv.Key },
                                                  usePresetSignal: true);
                if (r.signal == null || r.signal.name != kv.Value) { mapBad++; Debug.LogError(P + $"  ✗ R9 `{kv.Key}` 的信号是 `{r.signal?.name}`，应为 `{kv.Value}`"); }
            }
            Check(mapBad == 0, $"R9 5 个带 `rawSignal` 的 preset 各自解析到正确的信号表（错 {mapBad} 个；"
                             + "显式 `usePresetSignal: true` = 原版 `CameraShakePresetSO.Play()` 那条链）");

            // 🔴 **灭自证（2026-10-26 · B9 `X4`）**：上面那条走的是**显式**支；这 5 个 preset 的**默认支**
            //   必须回 `DefaultSignal` —— 上一条单靠「显式传 `true`」就能绿，若有人把 `Resolve` 的默认值
            //   改回 `true`（或删掉 `else` 那一支），上一条照样绿、**这一条会红**。
            //   判据 = `WFModuleScreenShake.Resolve` 里 `G12` 那段（`OverwriteCameraShakePreset` 从不写 `rawSignal`）。
            int rawDfltBad = 0;
            foreach (var kv in expectSignal)
            {
                var d = WFModuleScreenShake.Resolve(new WFModuleScreenShake.ShakeEntry { preset = kv.Key });
                if (d.signal == null || WFModuleScreenShake.DefaultSignal == null
                    || d.signal.name != WFModuleScreenShake.DefaultSignal.name) rawDfltBad++;
            }
            Check(rawDfltBad == 0,
                  $"R9 这 5 个带 `rawSignal` 的 preset 走**默认支**一律回 `DefaultSignal`（错 {rawDfltBad} 个）"
                + "｜🧨 把 `Resolve` 的 `usePresetSignal` 默认值改回 `true` ⇒ 本行红（那正是 `G12` 修掉的口径）");

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
                // ⚠️ **不给 `autoDestroySeconds`**（2026-10-25 改）：`Pray_Idle` 原版
                //    `preventDestroy = 1`（`animfx_components.json` 现读）⇒ 原版**从不**给它排计时销毁，
                //    唯一的销毁入口就是 `DoDestroy` → `AnimFXModuleDoDestroyAnimation`（`timeToDestroy` 1.0）。
                //    以前这里传 `1f` 是因为「到寿命会先调 Exit」那套老语义；现在到寿命是**直接销毁**
                //    （见 `WarpforgeEffectPlayer.DestroyAtLifetime`）⇒ 传 1f 会让**控制器**先把它删掉，
                //    这条断言就测不到「模块接管」了（会假绿）。
                var p = WarpforgeEffectPlayer.Play(e.name);
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
                    // ⚠️ **2026-10-25 改成显式 `Exit()`**：到寿命那条路现在是**直接销毁**
                    //    （原版 `AnimFXController.OnEnable` 的 `Object.Destroy(gameObject, destroyTime)`），
                    //    不再顺路调 `Exit()`。而原版这条 `Exit` clip 的入口本来就是**外部**调 Exit
                    //    （`BattleCardUI__CleanStatusAnims.c` / `__RemoveStatusAnim.c` 那两处，
                    //     状态动画收场）⇒ 这里照原版那条路走。
                    p.Exit();
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

        // ---------- ② 5 条 `penitence`：数据侧已从事件桶移出（`notPlayed[]` 带 `why`）；----------
        //  播放由 ⑤ 的 trigger 归口负责（⛔ 不再靠事件桶）；这里只核「记账 + 归口形状 + 桶归属」。
        //  🔴 **2026-10-24（第七十三会话 · 执行代理 `W6` / 上游 `W2` 桶归口）就地改口径（铁律 5）**：
        //     `PenitenceEffect` 的原版 trigger = **`Rage`(130)**（routed）⇒ 生成器已把这 5 行从
        //     **`ability` 挪进 `trigger`** ⇒ 原来那两条期望（`playedViaTrigger == 0` / `stillInAbility == 5`）
        //     **画像过期**、必红。现在改成**生产形状**（`times` + `keyword="penitence"`，= `BattleDriver.PlaySignal`
        //     传下来的形态）：每张卡**恰好 1 条** `PenitenceEffect`（合计 5）· `ability` 桶里 **0 条**。
        //     ⚠️ `times = null` 那一档**结构上拿不到 trigger 名**（运行期表没有 `tn` 那一列）⇒ 粗桶必然把这
        //     5 条卡级行一起播 —— 那不是判据、是那条路的已知边界（`W2` §5·2 已如实记），⛔ 别拿它当回归。
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
            int inNotPlayed = 0, playedViaTrigger = 0, stillInAbility = 0, oneEach = 0;
            var penPool = RuleEngine.CardDatabase.Load();
            foreach (var id in pen)
            {
                if (CardPresentation.VfxMap.IsNotPlayed(id, CardPresentation.VfxMap.Trigger)) inNotPlayed++;
                // 生产形状：`times` + `keyword`（原版 trigger 名 `Rage` 由 `CanonicalTriggerName` 归一）。
                var cd = RuleEngine.CardDatabase.FindById(penPool, id);
                Check(cd != null, $"②（前提）：卡池里拿得到 `{id}`（生产形状那一条要它的 `AnimTimes`）");
                if (cd != null)
                {
                    var viaTrigger = CardPresentation.VfxMap.ResolveAllTimed(
                        CardPresentation.VfxMap.Trigger, "Sororitas", id, cd.AnimTimes,
                        perCardOnly: true, keyword: "penitence");
                    int here = 0;
                    for (int i = 0; i < viaTrigger.Length; i++)
                        if (viaTrigger[i].Name == "PenitenceEffect") here++;
                    playedViaTrigger += here;
                    if (here == 1) oneEach++;
                }
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
            Check(oneEach == 5 && playedViaTrigger == 5,
                  $"② 这 5 张在**生产形状**（`times` + `keyword=\"penitence\"` ⇒ 原版 `Rage`）下"
                + $"**各恰好 1 条** `PenitenceEffect`（实得 {oneEach}/5 张、共 {playedViaTrigger} 条；生产路径见 ⑤）"
                + " —— 🧨 把 `VfxMap.CanonicalTriggerName` 的 `penitence → Rage` 删掉（归口失效 ⇒ 掉回粗桶，"
                + "`SOR8` 还会带出 `Arco-Flagellant Strike`）／把这 5 行从数据里删掉 ⇒ 本条红");
            Check(stillInAbility == 0,
                  $"② 同一件 `PenitenceEffect` 在 `ability` 桶里**已经一条都没有**（实得 {stillInAbility} 条）"
                + " —— 判据 = 原版这一行的 trigger 是 `Rage`(130)（`penitence` 的枚举名）、**不是**「发动技能」那一拍；"
                + "🧨 把它挪回 `ability` 桶 ⇒ 本条红（而且同一件会在两拍各播一次）；"
                + "⚠️ 「没丢」这一半由上面那条 `oneEach == 5` 钉（一删数据它就红）");
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

        // ---------- ⑤ 🆕 2026-10-23（特效线「新账-86」）：`penitence` 那一拍**播对件** ----------
        //  背景（判据 → `资料/普查产出_1023c/Trait_pentitence与静音桶溯源.md` §A/§D）：
        //   `penitence` 是 `rage`（`DefinedTrait 280`）的**官方显示名**，而候选侧的原版 trigger 名
        //   是 `AbilityTrigger.Rage = 130`（`CardAnimTime.TriggerName = "Rage"`）⇒ 引擎事件里的
        //   `keyword="penitence"` 与候选名**永不相交**。改前：`SOR8` 在忏悔那一拍播 `FlankEffect`
        //   （`trigger` 粗桶，**错件**）、`SOR10/21/27/32` 什么都不播（连 prefab 自带的
        //   `Trait Penitence` 音效也没有 —— 音效挂在 `PenitenceEffect` 的 AnimFX 模块上）。
        //  修法 = `VfxMap.CanonicalTriggerName` 把 `penitence` 归一成 `Rage`（**判据只那一处**）。
        //  ⚠️ 期望值**一律从卡池数据（`CardDef.AnimTimes`）独立推**，⛔ 不从被测实现读回（那是自证）。
        {
            var pool = RuleEngine.CardDatabase.Load();
            var c8 = RuleEngine.CardDatabase.FindById(pool, "SOR8");
            var c10 = RuleEngine.CardDatabase.FindById(pool, "SOR10");
            Check(c8 != null && c10 != null, "⑤（前提）：卡池里拿得到 `SOR8` / `SOR10`");
            if (c8 != null && c10 != null)
            {
                bool rageRow = false;
                for (int i = 0; i < c10.AnimTimes.Count; i++)
                {
                    var t = c10.AnimTimes[i];
                    if (t != null && t.Prefab == "PenitenceEffect"
                        && string.Equals(t.TriggerName, "Rage", System.StringComparison.OrdinalIgnoreCase))
                        rageRow = true;
                }
                Check(rageRow, "⑤（前提）：`SOR10` 的 `AnimTimes` 里 `PenitenceEffect` 标到原版 trigger `Rage`"
                             + "（= `AbilityTrigger.Rage` 130；`penitence` 只是它的显示名）");

                var pen10 = CardPresentation.VfxMap.ResolveAllTimed(
                    CardPresentation.VfxMap.Trigger, "Sororitas", "SOR10", c10.AnimTimes,
                    perCardOnly: true, keyword: "penitence");
                Check(HasVfx(pen10, "PenitenceEffect"),
                      "⑤ 正例：`keyword=\"penitence\"` 那一拍**播得出 `PenitenceEffect`**"
                    + $"（实得 {VfxNames(pen10)}）"
                    + " —— 🧨 把 `VfxMap.CanonicalTriggerName` 的 `penitence → Rage` 删掉 ⇒ 本条红");

                var pen8 = CardPresentation.VfxMap.ResolveAllTimed(
                    CardPresentation.VfxMap.Trigger, "Sororitas", "SOR8", c8.AnimTimes,
                    perCardOnly: true, keyword: "penitence");
                Check(HasVfx(pen8, "PenitenceEffect") && !HasVfx(pen8, "FlankEffect"),
                      "⑤ 反例：`SOR8` 那一拍播 `PenitenceEffect`、**不再播 `FlankEffect`**"
                    + $"（实得 {VfxNames(pen8)}）"
                    + " —— 🧨 删掉归一 ⇒ 退回 `trigger` 粗桶 = `FlankEffect`（错件），本条红");

                // 🔑 **灭自证（结构上不可能同时满足的一对）**：同一个入口、同一张卡、同一份 `times`，
                //    **换一个未归口的 keyword** 必须换回**另一组**结果 ——
                //      · `keyword="Flank"` ⇒ 回 `trigger` 桶，但按拍分件 ⇒ SOR8 那条是 Start(0) ⇒ **空**；
                //      · `keyword="Strike"`  ⇒ 同一入口在 `BL14` 上 ⇒ **必须播 StrikeEffect**（TriggerType 3）。
                //    两条一起才钉死「按拍分件」：⛔ 「整桶照播」⇒ 第一条红；⛔ 「trigger 桶一律停掉」⇒ 第二条红。
                //
                //  🔴 **2026-10-24（`E3` · `DG3` 红 #6·α）：第一条的期望改成【空】** ——
                //    `D4`（`A2`）有意把 `trigger` 桶里 `TriggerType ∉ {3,6}` 的行滤掉
                //    （判据 = `BattleCardUI__DisplayTriggerAnim.c:71`：触发那一拍**只处理 3/6**；
                //     `Start(0)` 归召唤/附魔，见 `DisplayStatusAnim.c:86-119`）。
                //    `SOR8` 的 trigger 桶**只有** `FlankEffect`（`FlankTraitStart`，
                //    `trait_effect_bindings.json` 里 `flank` 的 `triggerType = 0`）⇒ **空是正确结果**。
                //    原来那条「必须只有 `FlankEffect`」写于 10-23，期望值被 D4 的改口径**过期**了。
                //  🔴 **2026-10-24（第七十三会话 `W6`）：现在「空」有两个原因，两个都要在** ——
                //    上游 `W2` 的桶归口把 `PenitenceEffect`（原版 `Rage`）/ `Arco-Flagellant Strike`
                //    从 `ability` 挪进了 `trigger` ⇒ 光有 D4 那道闸时这里会播 **2 件**（都是卡级行、
                //    `TriggerTypeOf` 回 `-1`，D4 管不到它们）。所以 `VfxMap.TriggerBeatRowsMatching`
                //    又加了一道「卡级行要对得上本拍的 `CardAnimTime.TriggerName`」——`Flank` 对不上 `Rage`
                //    ⇒ 这两件被挡。⇒ 本条现在同时钉**两道**闸（判据 / 灭自证 → 那个方法的 doc）。
                var flank8 = CardPresentation.VfxMap.ResolveAllTimed(
                    CardPresentation.VfxMap.Trigger, "Sororitas", "SOR8", c8.AnimTimes,
                    perCardOnly: true, keyword: "Flank");
                Check(flank8.Length == 0
                      && !HasVfx(flank8, "FlankEffect") && !HasVfx(flank8, "PenitenceEffect"),
                      "⑤ 按拍分件：未归口的 `keyword=\"Flank\"` 回 `trigger` 桶，但 `SOR8` 那条是 **Start(0)**"
                    + " ⇒ 触发这一拍**什么都不播**（它的卡级行标的是原版 `Rage`，也不是这一拍）"
                    + $"（实得 {VfxNames(flank8)}）"
                    + " —— 🧨 把 `D4` 的分件闸去掉（回整桶）⇒ 播 `FlankEffect`（召唤那一刻的件，错拍）⇒ 本条红；"
                    + "把 `TriggerBeatRowsMatching` 那道「卡级行对 trigger 名」的闸去掉 ⇒ "
                    + "播 `PenitenceEffect` / `Arco-Flagellant Strike`（`Rage` 那一拍的件）⇒ 本条红");

                // 🔑 **正例**（证明「按拍分件」不是「把 trigger 桶整个关掉」）：
                //    🔴 **2026-10-24（执行代理 `W5` · AN1 `R5`/`R8`）就地改判（铁律 5）**：这里原来用
                //    `BL14`（trigger 桶只有 `StrikeEffect`，`StrikeTraitTrigger` / TriggerType 3）当正例，
                //    断的是 `keyword="Strike"` **必须播 `StrikeEffect`**。**两个前提都已过期**：
                //      ① `Strike` 现在是**白名单**里的（`VfxMap.IsRoutedTriggerName` 的 `case "strike"`）；
                //      ② `StrikeEffect` 是 **trait 框**（`trait_effect_bindings.json` 的 `strike` → 3 档），
                //         触发那一拍它归 `TraitFrames.DisplayTrigger`（链②）⇒ `VfxMap` 这一侧让开
                //         （`VfxMap.IsTraitFrameDuplicate`，AN1 `R8` 记的双播）。
                //    ⇒ 正例改成 `BL15`（两条**卡级** `Strike` 行）：`keyword="strike"` 取到那两条、
                //    **不含** `StrikeEffect`。（`BL14` 反过来成了 `R8` 的判别式：那一件让开 ⇒ 空。）
                var c15 = RuleEngine.CardDatabase.FindById(pool, "BL15");
                Check(c15 != null, "⑤（前提）：卡池里拿得到 `BL15`（Strike 卡级行正例）");
                if (c15 != null)
                {
                    var strike15 = CardPresentation.VfxMap.ResolveAllTimed(
                        CardPresentation.VfxMap.Trigger, c15.Faction, "BL15", c15.AnimTimes,
                        perCardOnly: true, keyword: "strike");
                    Check(HasVfx(strike15, "MasterOfExecutions") && HasVfx(strike15, "BulletImpactBurst_Blood")
                          && !HasVfx(strike15, "StrikeEffect"),
                          "⑤ 正例：`keyword=\"strike\"`（引擎发的**小写**形态）在 `BL15` 上取到它自己那两条卡级行、"
                        + "**不含** `StrikeEffect`（那一条是 trait 框 ⇒ 归 `TraitFrames`）"
                        + $"（实得 {VfxNames(strike15)}）"
                        + " —— 🧨 把 `D4` 的分件闸写成「trigger 桶一律回空」⇒ 两条卡级行也没了 ⇒ 本条红"
                        + "（⚠️ 本条是**回归闸**、**不是**放行的判别式：放行与否它都绿 —— 不放行时走第 ④ 档，"
                        + "`R8` 那道让开会挡掉 `StrikeEffect`、卡级行照样按名字挑。判别式见 `CheckR5R8` 的 ① / ④）");
                }

                // 再补一刀（防「万金油」）：归口取的是 keyword **本身**，不是「任取一个已归口名字」。
                var amb10 = CardPresentation.VfxMap.ResolveAllTimed(
                    CardPresentation.VfxMap.Trigger, "Sororitas", "SOR10", c10.AnimTimes,
                    perCardOnly: true, keyword: "Ambush");
                Check(amb10.Length == 0,
                      "⑤ 反例（不是万金油）：`keyword=\"Ambush\"` 在 `SOR10` 上**什么都不播**"
                    + $"（实得 {VfxNames(amb10)}）"
                    + " —— 🧨 把「按名字筛」写成「已归口就整桶播」⇒ 本条红");
            }
        }
        // ---------- ⑥ 🆕 2026-10-23（`W65` · 特效线）：`overrideByArmies` 10 条**真的被消费** ----------
        //  原版判据 = `TraitEffect.GetEffect(army)`（`TraitEffect__GetEffect.c:9-34`）：
        //  逐条比 `override.army`，**命中就返回 override 的 `effectAnim`**；全不命中才回默认那条。
        //  期望值**一律从 JSON 现算**（`dto.rows[].overrides`）—— ⛔ 不从 `TraitFrames` 读回（那是自证）。
        //  改前：`PrefabFor` 根本没有阵营参数 ⇒ 这 10 条（`shield` 4 / `invulnerable` 6）**一条都不生效**。
        {
            var asset = Resources.Load<TextAsset>(CardPresentation.TraitFrames.BindingsResourcePath);
            CardPresentation.TraitFrames.BindingDoc dto = null;
            if (asset != null)
            {
                try { dto = JsonUtility.FromJson<CardPresentation.TraitFrames.BindingDoc>(asset.text); }
                catch (System.Exception ex) { Debug.LogWarning(P + "⑥ 读绑定表失败：" + ex.Message); }
            }

            int ovRows = 0, ovTotal = 0, ovHit = 0, ovMissAuDefault = 0, ovPlainLeak = 0, ovEmpty = 0;
            int ovProbes = 0;
            var split = new Dictionary<string, int>();
            if (dto != null && dto.rows != null)
            {
                foreach (var r in dto.rows)
                {
                    if (r == null || string.IsNullOrEmpty(r.trait)
                        || r.overrides == null || r.overrides.Length == 0) continue;
                    ovRows++;
                    split[r.trait] = (split.TryGetValue(r.trait, out var n0) ? n0 : 0) + r.overrides.Length;

                    bool trigger = r.triggerType == 3 || r.triggerType == 6;
                    if (r.triggerType != 2 && !trigger)
                    {   // 0/1/5 这几档不归两本字典（`PreloadTraitEffect` 那类），这里只计不计判
                        ovEmpty += r.overrides.Length;
                        continue;
                    }

                    // **不带阵营**的重载必须**忽略**覆盖（老调用点语义不变）—— 期望值 = 数据里那条默认 `prefab`
                    var plain = CardPresentation.TraitFrames.PrefabFor(r.trait, trigger);
                    if (!string.Equals(plain, r.prefab, System.StringComparison.Ordinal)) ovPlainLeak++;

                    // **一个数据里不存在的 army** 必须回落默认件（= 原版「全不命中 ⇒ 默认 effectAnim」）
                    var miss = CardPresentation.TraitFrames.PrefabFor(r.trait, trigger, 9999);
                    ovProbes++;
                    if (string.Equals(miss, r.prefab, System.StringComparison.Ordinal)) ovMissAuDefault++;
                    else Debug.LogWarning(P + $"⑥ 阵营回落失败：`{r.trait}` army=9999 得到「{miss}」，"
                                            + $"期望默认件「{r.prefab}」");

                    foreach (var o in r.overrides)
                    {
                        if (o == null) continue;
                        ovTotal++;
                        if (string.IsNullOrEmpty(o.prefab)) { ovEmpty++; continue; }
                        var got = CardPresentation.TraitFrames.PrefabFor(r.trait, trigger, o.army);
                        if (string.Equals(got, o.prefab, System.StringComparison.Ordinal)) ovHit++;
                        else Debug.LogWarning(P + $"⑥ 阵营覆盖没生效：`{r.trait}` army={o.army} 得到「{got}」，"
                                                + $"期望「{o.prefab}」");
                    }
                }
            }

            Check(ovRows == 2 && ovTotal == 10
                  && split.TryGetValue("shield", out var nShield) && nShield == 4
                  && split.TryGetValue("invulnerable", out var nInvuln) && nInvuln == 6,
                  $"⑥ 前提：数据里**只有** `shield`(4) 与 `invulnerable`(6) 带 `overrideByArmies`，共 {ovTotal} 条"
                + $"（实得 {ovRows} 行 / {Describe(split)}）—— 张数突然变少 = 生成器漏读了 override 段");

            Check(ovHit > 0 && ovHit == ovTotal - ovEmpty,
                  $"⑥ **每条 override 都真的被消费**：按它自己的 `army` 查，拿到的就是**它自己的 prefab**"
                + $"（命中 {ovHit} / 可判 {ovTotal - ovEmpty} 条；空 prefab 的 {ovEmpty} 条原版本就不演）"
                + " —— 🧨 把 `TraitFrames.PrefabOf` 里的 override 循环删掉 ⇒ 全部回落到默认件、本条红");

            Check(ovProbes > 0 && ovMissAuDefault == ovProbes,
                  $"⑥ **不命中 ⇒ 回落默认件**（拿数据里不存在的 army=9999 探针 {ovProbes} 次，全部回落 {ovMissAuDefault} 次）"
                + " —— 🧨 把「命中判断」写成「只要有 override 就用第一条」⇒ 本条红");

            Check(ovPlainLeak == 0,
                  $"⑥ 不带阵营的两参重载**不套覆盖**（残留 {ovPlainLeak} 条）—— 老调用点的语义必须逐字不变");

            // **灭自证（结构上不可能同时满足的一对）**：同一个 trait、同一个入口，
            //   **不带阵营** 与 **带某个有覆盖的阵营** 必须拿到**不同**的件；缺任何一半都说明解析没分流。
            var shieldDefault = CardPresentation.TraitFrames.PrefabFor("shield", false);
            var shieldSautekh = CardPresentation.TraitFrames.PrefabFor("shield", false, 40);   // `Sautekh` = 40
            Check(shieldDefault == "ShieldTraitEffect_Idle"
                  && shieldSautekh == "ShieldTraitEffect_Idle Necron Variant"
                  && shieldDefault != shieldSautekh,
                  "⑥ **灭自证**：`shield` 默认件 = `ShieldTraitEffect_Idle`、"
                + "而 `army=40`（Sautekh）⇒ `ShieldTraitEffect_Idle Necron Variant`"
                + $"（实得「{shieldDefault}」/「{shieldSautekh}」）"
                + " —— 🧨 只做「有覆盖就用」或「恒用默认」⇒ 二者必有一个对不上、本条红");

            Check(CardPresentation.TraitFrames.ArmyOfFaction("Sautekh") == 40
                  && CardPresentation.TraitFrames.ArmyOfFaction("BlackLegion") == 50
                  && CardPresentation.TraitFrames.ArmyOfFaction("TauEmpire") == 70
                  && CardPresentation.TraitFrames.ArmyOfFaction(null) == -1
                  && CardPresentation.TraitFrames.ArmyOfFaction("Neutral") == -1,
                  "⑥ `CardData.faction` → 原版 army id 的换算（`Sautekh`=40 · `BlackLegion`=50 · `TauEmpire`=70；"
                + "`null`/`Neutral` = -1 = 不套覆盖）—— 换算口只此一处（`ArmyIcons.Id`）");
        }

        // ---------- ⑦ 🆕 2026-10-23（`W65`）：`ambush` 的槽位**不是「待接」，是「原版没有 FromCode(5)」** ----------
        //  真值（`数据/游戏数据/trait_collection.json` 的 `Ambush` 段）：`Ambush`(1200) **只有一条**
        //  `TraitEffect` = **End(1)** → `Ambush Effect` → `AmbushEffect`。
        //  原版「从代码播」那条链硬编码 `FromCode(5)`（`CardScript__ActivateTraitParticlesFromCode.c:62`）
        //  ⇒ ambush 在这一档**本来就什么都不播**（铁律 11 例外①）。
        //  ⚠️ 这两条也是**反向断言**：谁把 `AmbushEffect` 按名字塞进 `TraitParticles` / `ByEvent`
        //     当「通用件」，立刻红。
        {
            Check(CardPresentation.TraitParticles.PrefabFor("ambush") == null,
                  "⑦ **灭自证**：`ambush` 在 `TraitParticles`（FromCode 那档）**必须没有绑定** —— "
                + "原版 `Ambush.asset` 唯一的 `TraitEffect` 是 **End(1)**、没有 FromCode(5)"
                + $"（实得「{CardPresentation.TraitParticles.PrefabFor("ambush")}」）"
                + " ⇒ 🧨 把 `AmbushEffect` 按名字塞回 FromCode 表（或者让它当 `ambush_exit` 的通用件）⇒ 本条红");

            bool ambushEnd = false, ambushAnyFromCode = false;
            var all = CardPresentation.TraitFrames.RowsOfTriggerType(1);     // End(1)
            for (int i = 0; i < all.Length; i++)
                if (string.Equals(all[i].Trait, "ambush", System.StringComparison.OrdinalIgnoreCase)
                    && all[i].Prefab == "AmbushEffect") ambushEnd = true;
            var fcs = CardPresentation.TraitFrames.RowsOfTriggerType(5);      // FromCode(5)
            for (int i = 0; i < fcs.Length; i++)
                if (string.Equals(fcs[i].Trait, "ambush", System.StringComparison.OrdinalIgnoreCase))
                    ambushAnyFromCode = true;
            Check(ambushEnd && !ambushAnyFromCode,
                  "⑦ `ambush` 的真值：**有** End(1) → `AmbushEffect`、**没有** FromCode(5) —— "
                + "所以 `VfxMap.AmbushExit` 仍留空 + 出声（原版没有消费 End(1) 的调用点，照铁律 3 不猜）");
        }

    }

    // ================================================================
    //  ⑧ 🆕 2026-10-23（特效线 A7 · 第二批）—— `Start(0)` 那 13 条 trait effect
    //  ------------------------------------------------------------------
    //  判据 = `BattleCardUI__DisplayStatusAnim.c:86-119`：`if (TraitEffect.TriggerType == 0)` 时
    //  **拿这条 effect 直接 `BattleManager.ResolveTraitAnim` + `StartCoroutine`** ——
    //  **不进字典、不查重、不写回调**（对比 `== 2` 那一支 `:121-198` 才有 `TraitFrameEffectsPlay`）。
    //  ⚠️ 这一档**不过 `playIfWarlord` 门**（那道门只在 `…__DisplayTriggerAnim.c:62-68`）。
    //  改前：`TraitFrames` 只收 2/3/6 ⇒ 这 13 条**一条都没接**（11 条有动画、2 条原版就是空引用）。
    //  ⚠️ 期望值**一律从 `trait_effect_bindings.json` 现算**，⛔ 不从 `TraitFrames` 读回（那是自证）。
    // ================================================================
    static void CheckTraitStartEffects()
    {
        Debug.Log(P + "--- A7 · trait Start(0) 十三档 ---");

        // ---- 期望值：从数据独立重算 ----
        var asset = Resources.Load<TextAsset>(CardPresentation.TraitFrames.BindingsResourcePath);
        CardPresentation.TraitFrames.BindingDoc dto = null;
        if (asset != null)
        {
            try { dto = JsonUtility.FromJson<CardPresentation.TraitFrames.BindingDoc>(asset.text); }
            catch (System.Exception ex) { Debug.LogWarning(P + "⑧ 读绑定表失败：" + ex.Message); }
        }
        Check(dto != null && dto.rows != null && dto.rows.Length > 0, "⑧（前提）：绑定表读得到");
        if (dto == null || dto.rows == null) return;

        var startRows = new List<CardPresentation.TraitFrames.BindingRow>();
        for (int i = 0; i < dto.rows.Length; i++)
            if (dto.rows[i] != null && dto.rows[i].triggerType == 0) startRows.Add(dto.rows[i]);

        int wantWithPrefab = 0, wantEmpty = 0;
        var playable = new List<CardPresentation.TraitFrames.BindingRow>();
        var emptyOnes = new List<string>();
        for (int i = 0; i < startRows.Count; i++)
        {
            if (string.IsNullOrEmpty(startRows[i].prefab)) { wantEmpty++; emptyOnes.Add(startRows[i].trait); }
            else { wantWithPrefab++; playable.Add(startRows[i]); }
        }

        Check(startRows.Count == 13 && wantWithPrefab == 11 && wantEmpty == 2,
              $"⑧ 前提：`TriggerType == 0 (Start)` 共 {startRows.Count} 条 —— 有动画 {wantWithPrefab} 条 /"
            + $" 原版 `effectAnim` 空引用 {wantEmpty} 条（{string.Join(" / ", emptyOnes)}）"
            + " —— 🧨 生成器漏读 / 把别的档混进 0 ⇒ 张数变、本条红");

        Check(CardPresentation.TraitFrames.StartRowCount == startRows.Count
              && CardPresentation.TraitFrames.StartTraitCount == wantWithPrefab,
              $"⑧ `TraitFrames` 的 Start(0) 那本真的载进来了：{CardPresentation.TraitFrames.StartRowCount} 条 /"
            + $" 能播的 {CardPresentation.TraitFrames.StartTraitCount} 个 trait"
            + " —— 🧨 只要 `EnsureBindings` 没把 0 那档分出来，这里就是 0");

        // ---- 逐条：解析出来的 prefab 必须 = 数据里那一条（⛔ 不按名字猜） ----
        int mismatch = 0;
        for (int i = 0; i < playable.Count; i++)
        {
            var got = CardPresentation.TraitFrames.StartPrefabsFor(playable[i].trait);
            if (got.Length != 1 || !string.Equals(got[0], playable[i].prefab, System.StringComparison.Ordinal))
            {
                mismatch++;
                Debug.LogWarning(P + $"⑧ `{playable[i].trait}` 的 Start(0) 解析成 "
                                    + (got.Length == 1 ? "「" + got[0] + "」" : $"{got.Length} 件")
                                    + $"，数据里是「{playable[i].prefab}」");
            }
        }
        Check(mismatch == 0,
              $"⑧ 逐条：数据里**每一条** Start(0) 的 prefab 都原样查得到（{playable.Count} 条，对不上 {mismatch} 条）"
            + " —— 🧨 把 Start 那档按名字硬绑 / 拿 FromCode 那件的名字顶替 ⇒ 立刻对不上");

        // ---- 空引用那两条：**留空 + 出声**，⛔ 不许猜替身 ----
        int emptyBad = 0;
        for (int i = 0; i < emptyOnes.Count; i++)
        {
            if (!CardPresentation.TraitFrames.StartIsEmptyByDesign(emptyOnes[i])
                || CardPresentation.TraitFrames.StartPrefabsFor(emptyOnes[i]).Length != 0) emptyBad++;
        }
        Check(emptyBad == 0 && emptyOnes.Contains("firstStrike") && emptyOnes.Contains("piercing"),
              "⑧ 灭自证：`firstStrike` / `piercing` 的 Start(0) **原版 `effectAnim` 是空引用** ⇒"
            + " 我们**留空**（`StartIsEmptyByDesign` 为真、`StartPrefabsFor` 为空数组），只出声一次。"
            + " 🧨 给它们猜一个替身（任何非空 prefab）⇒ 本条红");

        // ---- 资产顺序：凡同时有 0 与 2 的 trait，0 都在 2 **前面**（我们的「先 Start 再 OnPlay」才等价） ----
        int orderBad = 0, both = 0;
        {
            var first0 = new Dictionary<string, int>();
            var first2 = new Dictionary<string, int>();
            for (int i = 0; i < dto.rows.Length; i++)
            {
                var r = dto.rows[i];
                if (r == null || string.IsNullOrEmpty(r.trait)) continue;
                var k = r.trait.Trim().ToLowerInvariant();
                if (r.triggerType == 0 && !first0.ContainsKey(k)) first0[k] = i;
                if (r.triggerType == 2 && !first2.ContainsKey(k)) first2[k] = i;
            }
            foreach (var kv in first0)
            {
                int i2;
                if (!first2.TryGetValue(kv.Key, out i2)) continue;
                both++;
                if (!(kv.Value < i2)) orderBad++;
            }
        }
        Check(both > 0 && orderBad == 0,
              $"⑧ 资产顺序：同时有 Start(0) 与 OnPlay(2) 的 {both} 个 trait 里，**0 都在 2 前面**"
            + $"（倒序 {orderBad} 个）—— 「先播 Start、再处理 OnPlay」的等价前提；数据顺序一变这里先红");

        // ---- 运行时：**每一条真的播了**（11 个 trait 逐个过 `DisplayStatus`） ----
        var host = new GameObject("A7_Start0Probe");
        try
        {
            var tf = CardPresentation.TraitFrames.Attach(host.transform);
            Check(tf != null && tf.Anchor != null, "⑧（前提）：探针上挂出状态框那一套（含 `effectsAnchor`）");
            if (tf == null) return;

            int playedOk = 0;
            var lastSeen = new Dictionary<string, string>();
            for (int i = 0; i < playable.Count; i++)
            {
                var r = playable[i];
                tf.DisplayStatus(r.trait);
                lastSeen[r.trait] = tf.LastStartPrefab;
                if (tf.StartPlayCountOf(r.trait) == 1
                    && string.Equals(tf.LastStartPrefab, r.prefab, System.StringComparison.Ordinal)) playedOk++;
                else
                    Debug.LogWarning(P + $"⑧ `{r.trait}` 的 Start(0) 没播成：次数 {tf.StartPlayCountOf(r.trait)}、"
                                        + $"最近一件「{tf.LastStartPrefab}」，期望「{r.prefab}」");
            }
            Check(playedOk == playable.Count,
                  $"⑧ **确实播了**：11 个有 Start(0) 动画的 trait 逐个走 `DisplayStatus`，每个都真建出 player"
                + $"（{playedOk}/{playable.Count}）—— 🧨 不接这一档 / 只在数据里登记不播 ⇒ 本条红");

            Check(tf.StartPlayCount == playable.Count,
                  $"⑧ 汇总：Start(0) 累计播了 {tf.StartPlayCount} 次（期望 {playable.Count}）"
                + " —— 只计**真建出 player** 的（库里缺件时 `Play` 返回 null、不计）");

            // 空引用那两条：**真的调一次** `DisplayStatus`，必须仍然一次都不播（留空 + 出声）
            tf.DisplayStatus("firstStrike");
            tf.DisplayStatus("piercing");
            Check(tf.StartPlayCountOf("firstStrike") == 0 && tf.StartPlayCountOf("piercing") == 0,
                  "⑧ 空引用那两条**调了 `DisplayStatus` 也一次都没播**（`firstStrike` / `piercing`）"
                + " —— 🧨 用「随便找一件」兜底 ⇒ 本条红");

            // ---- 灭自证 A：**不进字典、不去重** ⇒ 再调一次必须**再演一次**，而 OnPlay 那本**一件都不多** ----
            int before = tf.StartPlayCountOf("armour");
            int playBefore = tf.PlayCount;
            tf.DisplayStatus("armour");
            Check(tf.StartPlayCountOf("armour") == before + 1 && tf.PlayCount == playBefore,
                  "⑧ **灭自证（不去重）**：`DisplayStatus(\"armour\")` 再调一次 ⇒ Start(0) **又播一次**"
                + $"（{before} → {tf.StartPlayCountOf("armour")}），而 OnPlay 那本**一件都不多**"
                + $"（{playBefore} → {tf.PlayCount}）。判据 `DisplayStatusAnim.c:86-119` 这一支没有任何守卫。"
                + " 🧨 把 Start 塞进 `_play` 那本（最省事的错写法）⇒ 第二次被去重挡住、这条红");

            // ---- 灭自证 B：**同一次调用里 0 与 2 两条都播**（`huntMark`），且是**两件不同**的 prefab ----
            var huntStart = CardPresentation.TraitFrames.StartPrefabsFor("huntMark");
            var huntPlay = CardPresentation.TraitFrames.PrefabFor("huntMark", false);
            Check(tf.StartPlayCountOf("huntMark") == 1 && tf.HasPlayFrame("huntMark")
                  && huntStart.Length == 1 && !string.IsNullOrEmpty(huntPlay)
                  && !string.Equals(huntStart[0], huntPlay, System.StringComparison.Ordinal),
                  "⑧ **灭自证（0 与 2 共存）**：`huntMark` 一次 `DisplayStatus` ⇒ Start(0)"
                + $"「{(huntStart.Length == 1 ? huntStart[0] : "?")}」与 OnPlay(2)「{huntPlay}」**都播了**、"
                + "而且是**两件不同**的 prefab（`HuntMark_StackEffect` / `HuntMark_IdleEffect`）"
                + $" —— 实得 Start 次数 {tf.StartPlayCountOf("huntMark")}、OnPlay 框 {(tf.HasPlayFrame("huntMark") ? "在" : "不在")}。"
                + " 🧨 写成「有 OnPlay 就走 OnPlay、否则才播 Start」⇒ 本条红");

            // ---- 灭自证 C：**名字像、但不是同一条链**（`ferocity` 的 Start ≠ 它的 FromCode） ----
            {
                var fStart = CardPresentation.TraitFrames.StartPrefabsFor("ferocity");
                var fFromCode = CardPresentation.TraitParticles.PrefabFor("ferocity");
                Check(fStart.Length == 1 && fStart[0] == "FerocityEffect"
                      && fFromCode == "Ferocity_RecallToDeck"
                      && !string.Equals(fStart[0], fFromCode, System.StringComparison.Ordinal),
                      "⑧ **灭自证（两条链别串）**：`ferocity` 的 **Start(0)** = `FerocityEffect`、"
                    + $"**FromCode(5)** = `Ferocity_RecallToDeck`（实得「{fStart[0]}」/「{fFromCode}」）"
                    + " —— 🧨 把名字最像的那件（`FerocityEffect`）塞进 FromCode、或把 FromCode 那件当 Start 播 ⇒ 红");

                Check(CardPresentation.TraitFrames.StartPrefabsFor("stomp").Length == 0
                      && !CardPresentation.TraitFrames.StartIsEmptyByDesign("stomp")
                      && CardPresentation.TraitParticles.PrefabFor("stomp") == "Attack_Stomp",
                      "⑧ 反向：`stomp` **没有** Start(0) 那一条（它只有 FromCode(5) = `Attack_Stomp`）"
                    + " ⇒ 🧨 把 FromCode 那 10 条也当 Start 播（「反正都是 trait 特效」）⇒ 本条红");
            }

            // ---- 灭自证 D：`playIfWarlord` 这一档**不管**（判据：那门只在 DisplayTriggerAnim） ----
            {
                bool armourGate = true;
                for (int i = 0; i < startRows.Count; i++)
                    if (startRows[i].trait == "armour") armourGate = startRows[i].playIfWarlord;
                Check(armourGate == false && lastSeen.ContainsKey("armour")
                      && lastSeen["armour"] == "ArmourEffect",
                      "⑧ **灭自证（`playIfWarlord` 不挡 Start）**：`armour` 那条的 `playIfWarlord = false`，"
                    + "但 `DisplayStatusAnim` **没有**那道门（`:62-68` 只在 `DisplayTriggerAnim` 里）"
                    + $" ⇒ 它照样播（实得「{(lastSeen.ContainsKey("armour") ? lastSeen["armour"] : "(没播)")}」）。"
                    + " 🧨 给 `DisplayStatus` 也加一道 `BlocksForWarlord` ⇒ 本条红");
            }
        }
        finally
        {
            // 探针必须在同一个批处理里自己收干净（粒子在批处理下不会自己走时间）
            var players = host.GetComponentsInChildren<WarpforgeEffectPlayer>(true);
            for (int i = 0; i < players.Length; i++) if (players[i] != null) players[i].Kill();
            Object.DestroyImmediate(host);
        }
    }


    // ================================================================
    //  ⑨ 🆕 2026-10-24（执行代理 `W5` · AN1 `R5` / `R8`）
    //  ------------------------------------------------------------------
    //  R5：`trigger` 桶里「名字没放行 ⇒ 掉回整桶」那 **36 行 / 32 卡**
    //      （判据 = 逐行把 `资料/普查产出_1024c/data/an1_*_emit.tsv` 里 `emit == TRIGGER_BUCKET(归口待定)`
    //      那 36 行的 `(cardanim, prefab)` 钉到 `cards_engine.json` 的 `animTimes[].tn` 上；现读**全部**是
    //      `Strike`（21 行 / 19 张）与 `Slay`（15 行 / 13 张））。两个都有真发射口
    //      （`RuleCore.cs:6096` / `:5603`，都走 `FireTriggerAt` → `EvtKind.Trigger`）⇒ 已补进
    //      `VfxMap.IsRoutedTriggerName` 的白名单（`case "strike"` / `case "slay"`）+ 两张锁表。
    //  R8：同一条 trait 行以前会被**两条链各播一次** ——
    //      ① `VfxMap`（整桶）：`BattleDriver.PlaySignal` 的 `EvtKind.Trigger` 支 → `VfxMap.ResolveAllTimed`
    //         → `FireEventAll`；
    //      ② `TraitFrames`：`BattleDriver.PlayTraitPulse`（调用点 = `BattleDriver` 里
    //         `case EvtKind.Trigger:` 那一行，现读 `:21330` —— ⛔ 别把行号当判据）→ `TraitFrames.DisplayTrigger`
    //         （`Core/TraitFrames.cs:869`）→ `_triggerBook`。
    //      原版只播一次（`CardScript__ActivateTriggerTraitAnim.c` **只**调 `DisplayTriggerAnim`；
    //      `AbilityLogic.PlayAnim` 播的是卡自己的 `cardAnimations[]`，不含 trait frame）
    //      ⇒ 整桶那一侧让开「本拍 `TraitFrames` 会播的那一件」（`VfxMap.IsTraitFrameDuplicate`）。
    //  ⚠️ 期望值**一律按数据现算**（`cards_engine.json` 的 `animTimes` + `trait_effect_bindings.json`），
    //     ⛔ 不写死张数（数据一变，断言要么跟着走、要么把差额报出来）。
    //  ⚠️ 这一节**只走生产解释路径**（`ResolveAllTimed(..., perCardOnly: true)`），与 `BattleDriver` 逐参一致。
    // ================================================================
    static void CheckR5R8()
    {
        Debug.Log(P + "--- R5/R8 · trigger 白名单（Strike/Slay）与 trait 双播 ---");

        // ---------- ① 名字级：白名单那两条常驻闸（正向锁 + 反向闸 + 大小写三态）----------
        var names = CardPresentation.VfxMap.SelfCheckRoutedTriggerNames();
        Check(names.Length == 0,
              "R5① 白名单·名字级：`MustRouteTriggerNames`（含新增 `Strike`/`Slay`）全在白名单里、"
            + "`NoEmitTriggerNames` 全不在、小写 / 全大写两态都命中"
            + (names.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", names))
            + " —— 🧨 删掉 `case \"strike\"` / `case \"slay\"`、或把 `switch` 改回大小写敏感 ⇒ 本条红");

        // ---------- ② 件级：按卡池现算「逐卡不多不少」（= R5 的证明；含新增那两个名字）----------
        //  ⚠️ 卡池没载到时它**单列一条「没验」**（⛔ 不是静默通过）⇒ 这里不再补一条同样的前提。
        var perCard = CardPresentation.VfxMap.SelfCheckRoutedTriggers();
        Check(perCard.Length == 0,
              "R5② 白名单·件级（生产解释路径逐卡）：每个名字那一拍**不多播、不漏播** —— 新增的 "
            + "`Strike`（现读 19 张 / 21 行）与 `Slay`（13 张 / 15 行）只取卡自己标到这个名字的那几条"
            + (perCard.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", perCard))
            + " —— 🧨 把 `case \"strike\"` / `case \"slay\"` 删掉（回第 ④ 档整桶）⇒ 这一口**报出多播**"
            + "（现读 4 张：`AM72` / `DA36` / `SOR16` / `EC12` —— 它们桶里还有**别的 trait** 的框；"
            + "其余卡因为 `R8` 的让开 + `W6` 的名字闸，取件集合恰好不变 ⇒ 那一半只有 `R5①` 的正向锁挡得住）");

        var pool = RuleEngine.CardDatabase.Load();
        Check(pool != null && pool.Count > 0,
              "R5/R8（前提）：卡池载得到（载不到的话下面每一条都**没验到**，别读成通过）");
        if (pool == null || pool.Count == 0) return;

        // ---------- ③ R5 正例（点名一张卡：失败时一眼看得出是哪件）----------
        //  ⚠️ 这一条是**回归闸**（放行 / 不放行都应绿，因为不放行时 `R8` 那道让开也会挡掉同一件）——
        //     它守的是「`strike` 这个**小写**形态真的把卡级行挑出来」（= C3 D6 那类大小写坑的复发闸）。
        var bl15 = RuleEngine.CardDatabase.FindById(pool, "BL15");   // Master of Executions
        Check(bl15 != null, "R5③（前提）：卡池里拿得到 `BL15`");
        if (bl15 != null)
        {
            var got = CardPresentation.VfxMap.ResolveAllTimed(
                CardPresentation.VfxMap.Trigger, bl15.Faction, "BL15", bl15.AnimTimes,
                perCardOnly: true, keyword: "strike");
            Check(HasVfx(got, "MasterOfExecutions") && HasVfx(got, "BulletImpactBurst_Blood")
                  && !HasVfx(got, "StrikeEffect"),
                  "R5③ `keyword=\"strike\"`（引擎发的**小写**形态）在 `BL15` 上取到它自己那两条卡级行"
                + "（`MasterOfExecutions` / `BulletImpactBurst_Blood`），**且没有** `StrikeEffect`"
                + $"（实得 {VfxNames(got)}）—— 🧨 把 `D4` 的分件闸写成「一律回空」⇒ 这两条卡级行也没了 ⇒ 本条红。"
                + "⚠️ **本条不是放行判别式**（放行 / 不放行都应绿：前者走第 ③ 档、后者走第 ④ 档 + `R8` 让开，结果相同）；"
                + "判别式 = `R5①`（PascalCase / 小写 / 全大写三态）与 `R5④`（`AM72` / `DA36` 的错件）");
        }

        // ---------- ④ R5 判别式：`Slay` 那一拍不许播 `Strike`/`Talent` 的 trait 框 ----------
        //  这两张卡**真的能分辨放行与否**（`R8` 那道让开只管「本拍那一件」，管不到**别的 trait** 的框）：
        //   · `AM72` 的桶 = `GuardSummon`（卡级、标 `Slay`）+ `StrikeEffect`（trait `strike` 的框）；
        //   · `DA36` 的桶 = `CreateCard Gold`（卡级、标 `Slay`）+ `TalentEffect`（trait `talent` 的框）。
        //  不放行 `slay` ⇒ 回第 ④ 档 ⇒ 那个**别的 trait 的框**跟着一起播（错件）。
        var am72 = RuleEngine.CardDatabase.FindById(pool, "AM72");
        Check(am72 != null, "R5④（前提）：卡池里拿得到 `AM72`");
        if (am72 != null)
        {
            var slay = CardPresentation.VfxMap.ResolveAllTimed(
                CardPresentation.VfxMap.Trigger, am72.Faction, "AM72", am72.AnimTimes,
                perCardOnly: true, keyword: "slay");
            Check(HasVfx(slay, "GuardSummon") && !HasVfx(slay, "StrikeEffect"),
                  "R5④ `keyword=\"slay\"` 在 `AM72` 上只播它自己那条 `GuardSummon`、**不播** `StrikeEffect`"
                + "（后者是 `strike` 那一拍的 trait 框）"
                + $"（实得 {VfxNames(slay)}）"
                + " —— 🧨 删掉 `case \"slay\"` ⇒ 回第 ④ 档整桶 ⇒ `StrikeEffect` 回来 ⇒ 本条红");
        }
        var da36 = RuleEngine.CardDatabase.FindById(pool, "DA36");
        Check(da36 != null, "R5④b（前提）：卡池里拿得到 `DA36`");
        if (da36 != null)
        {
            var slay36 = CardPresentation.VfxMap.ResolveAllTimed(
                CardPresentation.VfxMap.Trigger, da36.Faction, "DA36", da36.AnimTimes,
                perCardOnly: true, keyword: "slay");
            Check(HasVfx(slay36, "CreateCard Gold") && !HasVfx(slay36, "TalentEffect"),
                  "R5④b `keyword=\"slay\"` 在 `DA36` 上只播它自己那条 `CreateCard Gold`、**不播** `TalentEffect`"
                + $"（实得 {VfxNames(slay36)}）"
                + " —— 🧨 删掉 `case \"slay\"` ⇒ 回第 ④ 档整桶 ⇒ `TalentEffect` 回来 ⇒ 本条红");
        }

        // ---------- ⑤ R8：**结构性互斥**（同一拍里同一件只能由一条链播）----------
        //  期望值的**唯一来源 = `trait_effect_bindings.json`**（= 原版 `CardTrait` SO），
        //  ⛔ 不从 `TraitFrames.TriggerBindingsFor` 读回 —— 那是拿实现验实现。
        var bAsset = Resources.Load<TextAsset>(CardPresentation.TraitFrames.BindingsResourcePath);
        CardPresentation.TraitFrames.BindingDoc bdto = null;
        if (bAsset != null)
        {
            try { bdto = JsonUtility.FromJson<CardPresentation.TraitFrames.BindingDoc>(bAsset.text); }
            catch (System.Exception ex) { Debug.LogWarning(P + "R8⑤ 读绑定表失败：" + ex.Message); }
        }
        Check(bdto != null && bdto.rows != null && bdto.rows.Length > 0,
              "R8⑤（前提）：`Resources/" + CardPresentation.TraitFrames.BindingsResourcePath + ".json` 读得出");
        if (bdto == null || bdto.rows == null) return;

        //  trait → 触发那一拍那几件（**只收 3/6**，= `DisplayTriggerAnim` 会播的那一批）。
        //  `CardAnim` 与 `prefab` 两把键都收（候选可能只带其中一个）；`overrides` 也必须收 ——
        //  `gen_card_vfx_by_card.py` 在**生成期**就把 owner 阵营的 override 套上了（例：`shield` 4 条）。
        var traitKeyOf = new Dictionary<string, string>();                      // 归一键 → 原样名
        var traitAnims = new Dictionary<string, HashSet<string>>();
        var traitPrefabs = new Dictionary<string, HashSet<string>>();
        System.Action<Dictionary<string, HashSet<string>>, string, string> addKey =
            (d, k, v) =>
            {
                if (string.IsNullOrEmpty(v)) return;
                HashSet<string> set;
                if (!d.TryGetValue(k, out set)) { set = new HashSet<string>(); d[k] = set; }
                set.Add(v);
            };
        foreach (var r in bdto.rows)
        {
            if (r == null || string.IsNullOrEmpty(r.trait)) continue;
            if (r.triggerType != 3 && r.triggerType != 6) continue;             // 触发那一拍只认 3/6
            string key = r.trait.Trim().ToLowerInvariant();
            traitKeyOf[key] = r.trait;
            addKey(traitAnims, key, r.cardAnim);
            addKey(traitPrefabs, key, r.prefab);
            if (r.overrides == null) continue;
            foreach (var o in r.overrides)
            {
                if (o == null) continue;
                addKey(traitAnims, key, o.cardAnim);
                addKey(traitPrefabs, key, o.prefab);
            }
        }
        Check(traitKeyOf.Count > 0,
              $"R8⑤（前提）：绑定表里收得到 `Trigger(3)`/`TriggerOnPlay(6)` 那几档（{traitKeyOf.Count} 个 trait）");

        //  逐 (卡, trait)：用 **trait 名本身**当关键词解析（= `PlayTraitPulse` 传给链② 的就是
        //  `BattleEvent.Keyword`），这一拍 `VfxMap` 的取件里**不许**出现那条 trait 的那几件。
        int pairs = 0, collide = 0, witness0 = CardPresentation.VfxMap.TraitFrameDuplicates;
        string firstBad = null;
        for (int ci = 0; ci < pool.Count; ci++)
        {
            var cd = pool[ci];
            if (cd == null) continue;
            foreach (var kv in traitKeyOf)
            {
                var v = CardPresentation.VfxMap.ResolveAllTimed(
                    CardPresentation.VfxMap.Trigger, cd.Faction, cd.Id, cd.AnimTimes,
                    null, true, kv.Value);
                if (v == null || v.Length == 0) continue;
                pairs++;
                HashSet<string> anims, pf;
                traitAnims.TryGetValue(kv.Key, out anims);
                traitPrefabs.TryGetValue(kv.Key, out pf);
                for (int i = 0; i < v.Length; i++)
                {
                    bool dup = (pf != null && pf.Contains(v[i].Name))
                            || (anims != null && !string.IsNullOrEmpty(v[i].Anim) && anims.Contains(v[i].Anim));
                    if (!dup) continue;
                    //  ⚠️ **反例排除**：这一件可能同时是**卡自己那条 `cardAnimations[]`**（`animTimes` 里标着同一个
                    //  trigger 名）—— 那样原版本来就会播**两次**（卡动画一次 + trait 框一次），不是我们的缺陷。
                    //  现读 = `UM84` / `UM85` / `UM86`（`CodexTraitTrigger_warlord` → `CodexEffect`）三行，
                    //  它们走第 ③ 档（`codex` 已放行），`IsTraitFrameDuplicate` **根本不参与**。
                    bool labelled = false;
                    if (cd.AnimTimes != null)
                        for (int t = 0; t < cd.AnimTimes.Count; t++)
                        {
                            var at = cd.AnimTimes[t];
                            if (at == null || string.IsNullOrEmpty(at.TriggerName)) continue;
                            if (!string.Equals(at.TriggerName, kv.Value, System.StringComparison.OrdinalIgnoreCase)) continue;
                            if (!string.IsNullOrEmpty(v[i].Anim)
                                    ? string.Equals(at.Anim, v[i].Anim, System.StringComparison.Ordinal)
                                    : string.Equals(at.Prefab, v[i].Name, System.StringComparison.Ordinal))
                            { labelled = true; break; }
                        }
                    if (labelled) continue;
                    collide++;
                    if (firstBad == null)
                        firstBad = $"{cd.Id}（{cd.Name}）的 `{kv.Value}` 那一拍：`VfxMap` 与 `TraitFrames` **都会播** `{v[i].Name}`";
                }
            }
        }
        int witness = CardPresentation.VfxMap.TraitFrameDuplicates - witness0;
        Check(collide == 0 && pairs > 0,
              $"R8⑤ 结构性互斥：同一拍里「`TraitFrames` 会播的那一件」**不再**由 `VfxMap` 播"
            + $"（扫了 {pairs} 个「有取件」的 (卡, trait) 对，撞车 {collide} 次）"
            + (collide == 0 ? "" : "\n  ✗ " + firstBad + $"（共 {collide} 次）")
            + (pairs > 0 ? "" : "\n  ✗ **空放**：一对都没扫到 ⇒ 这条断言没有内容")
            + " —— 🧨 删掉 `VfxMap.IsTraitFrameDuplicate`（或不再把关键词传进 `TriggerBeatRows`）⇒ "
            + "两条链同时播同一件 ⇒ 本条红");
        Check(witness > 0,
              $"R8⑤·灭自证的另一半：那道闸**真的咬到了**（诊断计数 `TraitFrameDuplicates` 增量 = {witness} 件）"
            + " —— 本口扫的是**全部**带 `Trigger(3)`/`TriggerOnPlay(6)` 档的 trait 名 ⇒ 这个数比生产**多**"
            + "（生产只发得到 `codex` / `strike` / `artifice` / `backlash` / `mob` / `cruelty` / `ecstasy` 这几个）"
            + " —— ⛔ 别把它当断言上界，数据一变它就变；"
            + " 🧨 闸被删掉（增量 0）/ 反过来把链② 那本清空（两件都不播）⇒ 本条红");
    }

    /// <summary>`⑤` 用：结果里有没有这个特效名（大小写逐字比 —— prefab 名是精确 join 键）。</summary>
    static bool HasVfx(CardPresentation.VfxMap.ResolvedVfx[] arr, string name)
    {
        if (arr == null) return false;
        for (int i = 0; i < arr.Length; i++)
            if (string.Equals(arr[i].Name, name, System.StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>`⑤` 用：把结果里的特效名拼成一行（断言失败时要看得见「实得什么」）。</summary>
    static string VfxNames(CardPresentation.VfxMap.ResolvedVfx[] arr)
    {
        if (arr == null) return "(null)";
        if (arr.Length == 0) return "(空)";
        var s = new System.Text.StringBuilder();
        for (int i = 0; i < arr.Length; i++)
        {
            if (i > 0) s.Append(" / ");
            s.Append(arr[i].Name);
        }
        return s.ToString();
    }

    /// <summary>`⑤d` 用：给 <paramref name="cardId"/> 的 <paramref name="ev"/> 换 24 个种子挑，
    /// 是不是**每次都**取到 <paramref name="file"/>（期望值来自 `voice_lines.json`，⛔ 不从被测函数反推）。</summary>
    static bool AlwaysPicks(string cardId, string ev, string file)
    {
        for (int seed = 0; seed < 24; seed++)
        {
            string clip, text, hitEv;
            if (!CardPresentation.VoiceLines.TryPick(cardId, new[] { ev }, new System.Random(seed),
                                                     out clip, out text, out hitEv)) return false;
            if (!string.Equals(clip, file, System.StringComparison.Ordinal)) return false;
        }
        return true;
    }

    /// <summary>`⑤d` / `⑤f` 用：24 个种子的取值去重后拼成一行（失败时要看得见「实得什么」）。</summary>
    static string PickedSet(string cardId, string ev)
    {
        var seen = new List<string>();
        for (int seed = 0; seed < 24; seed++)
        {
            string clip, text, hitEv;
            if (!CardPresentation.VoiceLines.TryPick(cardId, new[] { ev }, new System.Random(seed),
                                                     out clip, out text, out hitEv)) clip = "(miss)";
            if (!seen.Contains(clip)) seen.Add(clip);
        }
        return string.Join(" / ", seen);
    }

    /// <summary>`⑤e` 用：24 个种子下出现了几种不同的取值（用来证明「随机池还在」）。</summary>
    static int DistinctPicks(string cardId, string ev)
    {
        var set = new HashSet<string>();
        for (int seed = 0; seed < 24; seed++)
        {
            string clip, text, hitEv;
            if (!CardPresentation.VoiceLines.TryPick(cardId, new[] { ev }, new System.Random(seed),
                                                     out clip, out text, out hitEv)) clip = "(miss)";
            set.Add(clip);
        }
        return set.Count;
    }

    /// <summary>`⑤f` / `⑤g` 用：按「<paramref name="first"/> → <paramref name="second"/>」这条兜底链取
    /// （rng 传 null = 定死），命中的文件名是否正好是 <paramref name="file"/>。</summary>
    static bool PicksFile(string cardId, string first, string second, string file)
    {
        string clip, text, hitEv;
        bool hit = CardPresentation.VoiceLines.TryPick(cardId, new[] { first, second }, null,
                                                       out clip, out text, out hitEv);
        return hit && string.Equals(clip, file, System.StringComparison.Ordinal);
    }

    static string Describe(Dictionary<string, int> d)
    {
        if (d == null || d.Count == 0) return "(空)";
        return string.Join(" · ", d.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + "=" + kv.Value));
    }

        CheckTraitAuthority();

        CheckTraitStartEffects();          // 🆕 2026-10-23（A7）：`Start(0)` 那 13 条 trait effect

        CheckR5R8();                       // 🆕 2026-10-24（执行代理 `W5`）：AN1 `R5`（trigger 白名单）/ `R8`（trait 双播）

        // ---- ⑤b 语音表：卡 SO 的 `summonAudio` 都要有 `deploy` 行（🆕 2026-10-24 · A8 落地 A4 的片段）----
        //  判据 = `Resources/voice_lines.json`（经 `VoiceLines` 读回来），⛔ 不是我们自己的常量：
        //    · `EC57`（Xarahan, Daemon Prince）的 `summonAudio` 在原版卡 SO 里就指着
        //      `VO_UM_Primaris Intercessor - For the Imperium`（**原版自己抄错了**）⇒ **照原版播**。
        //    · `UM6` 与它共用同一条 clip ⇒ 两张卡都必须有这一行
        //      （生成器 `import_original_audio.py` 的「多卡共用同一 clip 照原版都挂」）。
        //  🧨 **灭自证**：把 `voice_lines.json` 里 EC57 的 `deploy` 行删掉 ⇒ `TryPick(…, ForDeploy, …)`
        //     返回 false ⇒ 下面那两条（`ev == ExactDeploy` / `clip == …`）**当场红**；
        //     把生成器那道「多卡共用」护栏改回「唯一才认」⇒ UM6 / EC57 各红一条。
        Check(CardPresentation.VoiceLines.Ready,
              "voice_lines.json 读得到（" + CardPresentation.VoiceLines.LoadError + "）");
        Check(CardPresentation.VoiceLines.CardCount >= 600,
              "语音表卡数 " + CardPresentation.VoiceLines.CardCount + " ≥ 600");
        foreach (var vcId in new[] { "EC57", "UM6" })
        {
            string vclip, vtext, vev;
            bool vhit = CardPresentation.VoiceLines.TryPick(
                vcId, CardPresentation.VoiceLines.ForDeploy, null, out vclip, out vtext, out vev);
            Check(vhit && vev == CardPresentation.VoiceLines.ExactDeploy,
                  "⑤b " + vcId + " 的 `deploy` 行存在（命中 ev=" + vev + "）"
                + "｜🧨 删掉 `voice_lines.json` 里这一行 ⇒ 本条红（这就是「照原版都挂」那条护栏的反向闸）");
            Check(vhit && vclip == "VO_UM_Primaris Intercessor - For the Imperium.ogg",
                  "⑤b " + vcId + " 的 deploy 指 `VO_UM_Primaris Intercessor - For the Imperium.ogg`（现读 "
                + (vhit ? vclip : "无") + "）—— 原版卡 SO 就是这么指的（**原版自己抄错 UM 的语音**，照原版）");
            Check(vhit && CardPresentation.VoiceLines.Clip(vclip) != null,
                  "⑤b " + vcId + " 的 deploy 音频资源真的在 `Resources/"
                + CardPresentation.VoiceLines.ClipRoot + "…`");
        }

        // ---- ⑤c 件2 现状：`D5` 删掉那两条「按名字猜」的死行之后，两处都只剩 SO 指名的 **1 行** ----
        //  🔴 **2026-10-24（`E3` · `DG3` 红 #7/#8·α）：期望从 2 行/1 SO 改成 **1 行/1 SO**。**
        //    被删的是 `ASH2/gen1` 的 `_gen1.ogg` 与 `UM88/gen3` 的 `_gen3.wav`（按名字猜的），
        //    删除理由 = `D5_语音归属两处.md` §五 B 表第 1 / 9 条：同卡同 ev **已有 SO 指名行**
        //    ⇒ `orphan_is_dead`（`VoiceLines.IsSoAttributed` + `TryPick` 的 `soOnly` 让它们**永远被压掉**）
        //    ⇒ 删的是**死数据**，不是数据退化。⛔ 别为了让它变回 2 行去恢复那两条死行。
        //  ⛔ 期望值**从数据独立算**（行数都是 JSON 里的原值）—— `LineRowCount`/`SoRowCount` 量的是原始数据，
        //     不是 `TryPick` 的取词逻辑。
        Check(CardPresentation.VoiceLines.LineRowCount("ASH2", "gen1") == 1
              && CardPresentation.VoiceLines.SoRowCount("ASH2", "gen1") == 1,
              "⑤c ASH2 的 `gen1` 只剩 1 行、就是卡 SO 指名那条（D5 已按 `orphan_is_dead` 删掉按名字猜的死行；现读 行/SO = "
            + CardPresentation.VoiceLines.LineRowCount("ASH2", "gen1") + "/"
            + CardPresentation.VoiceLines.SoRowCount("ASH2", "gen1") + "）");
        Check(CardPresentation.VoiceLines.LineRowCount("UM88", "gen3") == 1
              && CardPresentation.VoiceLines.SoRowCount("UM88", "gen3") == 1,
              "⑤c UM88 的 `gen3` 只剩 1 行、就是卡 SO 指名那条（同上；现读 行/SO = "
            + CardPresentation.VoiceLines.LineRowCount("UM88", "gen3") + "/"
            + CardPresentation.VoiceLines.SoRowCount("UM88", "gen3") + "）");

        // ---- ⑤d 件2 行为：同 ev 多行时**永远**取 SO 指名那一条（换 24 个种子都不许变）----
        //  🔴 **2026-10-24（`E3` · `DG3` 顺手发现 3）：从「真实数据夹具」改成「合成数据驱动」。**
        //    原来靠 `ASH2/gen1`、`UM88/gen3` 各 2 行当夹具；`D5` 删掉死行之后全表只剩 4 组
        //    「(卡, ev) 多行」（`ASH3/BL35/BL36/SAU36` 的 `intro-vs`；`D7` 之前记作 `deploy`），**没有一组含 `src=cardSO`**
        //    ⇒ 原断言恒真（一条重要规则长期无人守）。⛔ 不许就这样删掉这条判据。
        //    ⇒ 用 `VoiceLines.SetRowsForTest`（形状照 `E2` 的 `SetExactForTest`：成对 / 幂等 /
        //      抛异常也还原 / `WasInjected` 可断言）注入「1 条 SO 指名 + 1 条按名字猜」的同 ev 两行，断：
        //      ① 24 个种子**都**取到 SO 那条；② 出作用域还原后这个合成 id **取不到任何东西**（防「把表写死」）。
        //  🧨 把 `TryPick` 里的 `soOnly` 去掉、回到纯随机 ⇒ 24 个种子下几乎必然抽到猜的那条 ⇒ 红。
        {
            const string testId = "ZZ_E3_SO_PRIORITY", testEv = "gen1";
            const string soClip = "VO_E3_SO_priority_gen4.ogg", guessClip = "VO_E3_guess_gen1.ogg";
            bool injected = false;
            using (var inj = CardPresentation.VoiceLines.SetRowsForTest(testId, testEv, soClip, guessClip))
            {
                injected = inj.WasInjected;
                Check(injected, "⑤d（前提）：合成两行注入进台词表（`VoiceLines.SetRowsForTest`）"
                              + "｜没注进去 ⇒ 下面两条**没验到**，别读成通过");
                if (injected)
                {
                    Check(CardPresentation.VoiceLines.LineRowCount(testId, testEv) == 2
                          && CardPresentation.VoiceLines.SoRowCount(testId, testEv) == 1,
                          "⑤d（前提）：合成夹具确实是「2 行 / 其中 1 行卡 SO 指名」（现读 行/SO = "
                        + CardPresentation.VoiceLines.LineRowCount(testId, testEv) + "/"
                        + CardPresentation.VoiceLines.SoRowCount(testId, testEv) + "）");
                    Check(AlwaysPicks(testId, testEv, soClip),
                          $"⑤d 同 ev 两行时 24 个种子**都**取卡 SO 指的那条 `{soClip}`"
                        + "（实得：" + PickedSet(testId, testEv) + "）"
                        + "｜🧨 `TryPick` 去掉 `soOnly`（回纯随机）⇒ 抽到猜的那条 ⇒ 红");
                }
            }
            // 还原：合成 id 不在表里 ⇒ 取不到（挡「为了让它绿把表写死 / Dispose 不还原」）
            string backClip, backText, backEv;
            bool backHit = CardPresentation.VoiceLines.TryPick(
                testId, new[] { testEv }, new System.Random(0), out backClip, out backText, out backEv);
            Check(!backHit && CardPresentation.VoiceLines.LineRowCount(testId, testEv) == 0,
                  $"⑤d（还原）出作用域后合成 id `{testId}` 不在表里（取到 `{(backHit ? backClip : "无")}`；"
                + $"残留行数 {CardPresentation.VoiceLines.LineRowCount(testId, testEv)}）"
                + "｜🧨 把注入做成「永久写进表」/ `Dispose` 不还原 ⇒ 本条红");
        }

        // ---- ⑤e 灭自证（另一面）：**同 ev 多行、且没有一行是 SO 指名**时，随机池必须还在 ----
        //  🔴 **2026-10-24（`E10` · 宿主 151/1 里唯一那条红）：从【真实数据夹具】改成【注入驱动】。**
        //    ⑤e 原本拿 `BL35` 的 `deploy`（4 条 `vs_*`、全无 SO 归属）当自然夹具；**`D7` 合法地把那批
        //    `_vs_` 行从 `ev=deploy` 改记成 `ev=intro-vs` + `src=orphan`**（判据 →
        //    `资料/普查产出_1024/D7_语音脏数据订正.md` §一/§二）⇒ 现读 `voice_lines.json`：
        //      · `BL35` 的 `deploy` 行数 = **0**（全表**已没有任何 `deploy` 多行组**）；
        //      · 仅存的 4 组多行全是 `intro-vs`（`ASH3` 3 行 / `BL35` 4 行 / `BL36` 7 行 / `SAU36` 4 行，
        //        SO 均 0）—— 那是对手台词归档桶，走 `vs~` 虚拟 ev 通路，不适合当本条的夹具。
        //    ⇒ 旧断言前半 `SoRowCount("BL35","deploy")==0` 恒真、后半 `DistinctPicks>=2` 恒假
        //      （0 行 ⇒ 24 个种子全是 `(miss)`）。
        //    ⛔ **这不是「为了绿而改」**：⑤e 守的规则一字未变 —— **同 ev 多行、且无一行 SO 指名 ⇒ 仍然随机**；
        //       变的只是夹具来源（仿 ⑤d 用 `VoiceLines.SetRowsForTest`：成对 / 幂等 / 可还原）。
        //  🧨 **灭自证**：① 把 `TryPick` 改成「永远取第一条」⇒ 夹具①的 24 个种子只剩 1 种取值 ⇒ 红；
        //                ② 把 `soOnly`（SO 优先）删掉、回纯随机 ⇒ 夹具②的 24 个种子几乎必然抽到猜的那条 ⇒ 红
        //                （这条闸与 ⑤d 同源，这里在 `deploy` 上再钉一次；⑤d 本身不动）。
        {
            const string ev = "deploy";
            const string randId = "ZZ_E10_NO_SO_RANDOM";        // 夹具①：2 条**无 SO** 的 `deploy` 行
            const string soId   = "ZZ_E10_SO_PRIORITY_DEPLOY";  // 夹具②：1 条 SO 指名 + 1 条按名字猜
            const string guessA = "VO_E10_noSO_deploy_a.ogg", guessB = "VO_E10_noSO_deploy_b.ogg";
            const string soClip = "VO_E10_so_deploy.ogg",     guessC = "VO_E10_guess_deploy_c.ogg";

            bool randInj = false, soInj = false, allSo = false;
            int randRows = 0, randSo = 0, soRows = 0, soSo = 0, distinct = 1;
            string soPicked = "";
            using (var inj = CardPresentation.VoiceLines.SetRowsForTest(randId, ev, null, guessA, guessB))
            {
                randInj = inj.WasInjected;
                randRows = CardPresentation.VoiceLines.LineRowCount(randId, ev);
                randSo = CardPresentation.VoiceLines.SoRowCount(randId, ev);
                if (randInj) distinct = DistinctPicks(randId, ev);
            }
            using (var inj = CardPresentation.VoiceLines.SetRowsForTest(soId, ev, soClip, guessC))
            {
                soInj = inj.WasInjected;
                soRows = CardPresentation.VoiceLines.LineRowCount(soId, ev);
                soSo = CardPresentation.VoiceLines.SoRowCount(soId, ev);
                if (soInj) { allSo = AlwaysPicks(soId, ev, soClip); soPicked = PickedSet(soId, ev); }
            }

            // 还原：两个合成 id 都不在表里 ⇒ 取不到（挡「为了让它绿把表写死 / Dispose 不还原」）
            string b1, t1, e1, b2, t2, e2;
            bool back1 = CardPresentation.VoiceLines.TryPick(
                randId, new[] { ev }, new System.Random(0), out b1, out t1, out e1);
            bool back2 = CardPresentation.VoiceLines.TryPick(
                soId, new[] { ev }, new System.Random(0), out b2, out t2, out e2);
            int backRows = CardPresentation.VoiceLines.LineRowCount(randId, ev)
                         + CardPresentation.VoiceLines.LineRowCount(soId, ev);

            Check(randInj && soInj
                  && randRows == 2 && randSo == 0 && distinct >= 2
                  && soRows == 2 && soSo == 1 && allSo
                  && !back1 && !back2 && backRows == 0,
                  "⑤e 同 ev 多行、无一行 SO 指名时仍然随机（夹具由 `SetRowsForTest` 注入；原自然夹具已由 D7 合法清空）："
                + "① 2 条无 SO ⇒ 24 个种子出现 " + distinct + " 种取值（应 ≥ 2；行/SO = "
                + randRows + "/" + randSo + "，注入 " + (randInj ? "成功" : "**失败**") + "）"
                + "；② 1 条 SO + 1 条猜 ⇒ " + (allSo ? "24 个种子都取 SO 那条" : "实得：" + soPicked)
                + "（行/SO = " + soRows + "/" + soSo + "，注入 " + (soInj ? "成功" : "**失败**") + "）"
                + "；还原后取不到 [" + (back1 ? b1 : "无") + " / " + (back2 ? b2 : "无")
                + "]，残留行数 " + backRows
                + "｜🧨 `TryPick` 改成永远取第一条 ⇒ ①红；删掉 `soOnly`（SO 优先）⇒ ②红");
        }

        // ---- ⑤f 件1 现状：`defaultChatSound` 仍是**原版真兜底**（⛔ 别按 B2 §八·1 删掉）----
        //  判据 = `RawCardScript$$GetChatSound` 的**指令流**（全文见 `VoiceLines.cs` 文件头）：
        //    `default:`（枚举 >10）支在 `CustomDebug.LogWarning` 之后**不跳走**，直接落进各 case 支共用的尾巴；
        //    那条尾巴在 clip 取不到时读 `+0x188`（= `defaultChatSound`）并 `Load`。
        //  ⚠️ 本条**故意**断言「取得到」：原来那份待办想断言「取不到」——**那个口径被指令流推翻了**，
        //     照它改会**偏离原版**。这里用「主次 ev 都不存在 → 落到 `default`」模拟原版那条兜底。
        //  🧨 灭自证：把 `"default"` 从数据行里删掉 / 让 `TryPick` 跳过 `default` ⇒ 本条红。
        Check(PicksFile("ASH2", "__no_such_ev__", "default", "VO_SaimHann_Zephyrblade_attack.ogg"),
              "⑤f ASH2 的 `default` 行（= 原版 `defaultChatSound`，与 `attack` 同 clip）仍可兜底取到"
            + "（实得：" + PickedSet("ASH2", "default") + "）");
        Check(PicksFile("EC57", "__no_such_ev__", "default", "VO_CSM_EC_Daemon Prince Xarahan_attack.ogg"),
              "⑤f EC57 的 `default` 行（B2 刚补的那一条）也取得到 —— 应为 "
            + "`VO_CSM_EC_Daemon Prince Xarahan_attack.ogg`（实得：" + PickedSet("EC57", "default") + "）");

        // ---- ⑤g 防「一刀切静音」：正常事件照常取得到，且音频实体真在工程里 ----
        {
            string gclip, gtext, gev;
            bool ghit = CardPresentation.VoiceLines.TryPick(
                "EC57", new[] { "greet" }, null, out gclip, out gtext, out gev);
            Check(ghit && gev == "greet" && gclip == "VO_CSM_EC_Daemon Prince Xarahan_greet.ogg"
                  && CardPresentation.VoiceLines.Clip(gclip) != null,
                  "⑤g EC57 的 `greet` 照常取得到且音频在 `Resources/"
                + CardPresentation.VoiceLines.ClipRoot + "…`（现读 " + (ghit ? gclip : "无") + "）");
        }

        // ---- ⑥ 根特效层 / 卡根参考系（战斗线 `C6·Q1` / `C6·Q2`，2026-10-24）----
        //  判据 → `资料/普查产出_1024c/C6_表现界面轮抽剩余判据.md` §一 / §二。
        //  ⚠️ 它自己会**附加打开** `Scenes/Battle.unity` 去取真相机（`BoardCamera*` / tag=MainCamera），
        //    跑完关掉；相机**不是**自己造的（造一对 = 拿我们的常量证明我们的常量）。
        //  🧨 灭自证：同一段生产代码在 2D 档（卡在层 0）下必须落到 HUD 那台 ⇒ 把层写死成 0 或 8 都红。
        CardPresentation.RootVfxLayerAssert.Run(Check);

        // ================================================================
        //  🆕 2026-10-25（特效动画域 · 完全复刻第 1 轮 · K5 `P1-5`）
        //  R26：`WFEffectEntry.AutoLifetime` 的三档语义必须与原版 `AnimFXController.OnEnable` 一致
        // ================================================================
        //  判据：`d:/2/tools/decomp_full/AnimFXController__OnEnable.c`
        //        `if (0.0 < destroyTime && !preventDestroy) Object.Destroy(gameObject, destroyTime);`
        //        ⇒ **`destroyTime == 0` 是「不排销毁」**，不是「落到兜底时长」。
        //  2026-10-25 之前 0 会落到 `natural`（2–7s）⇒ **状态还在、框自己没了**
        //  （`trait_onplay` 桶 142 行最明显）。已改；这条断言把它钉住。
        //  🧨 灭自证：四档各断一次 —— 只断「0 现在很大」而不断「>0 仍取原值 / <0 仍走兜底」，
        //     把实现改成「一律 ∞」也会绿。
        {
            var probe = new WFEffectEntry { name = "__lifetime_probe__" };

            probe.destroyTime = 4f; probe.natural = 2f;
            Check(Mathf.Approximately(probe.AutoLifetime(), 4f),
                  "R26：destroyTime>0 ⇒ 取原版那个值（不受 natural 影响）");

            probe.destroyTime = 0f; probe.natural = 2f;
            Check(float.IsPositiveInfinity(probe.AutoLifetime()),
                  "R26：destroyTime==0 ⇒ **∞**（原版不排销毁；持续型状态框不该自己消失）");

            probe.destroyTime = -1f; probe.natural = 2f;
            Check(Mathf.Approximately(probe.AutoLifetime(), 2f),
                  "R26：destroyTime<0（原版没有 AnimFXController）⇒ 才落到 natural 兜底");

            probe.destroyTime = -1f; probe.natural = -1f;
            Check(Mathf.Approximately(probe.AutoLifetime(), WarpforgeEffectPlayer.SafeDestroyTime),
                  "R26：连 natural 都没有 ⇒ SAFE_DESTROY_TIME(3s) 兜底");

            // 反向抽样：库里真有一批 `destroyTime==0` 的实例，且它们现在都报 ∞。
            int zero = 0, inf = 0;
            foreach (var x in entries)
            {
                if (x == null || x.prefab == null || x.destroyTime != 0f || x.preventDestroy) continue;
                zero++;
                if (float.IsPositiveInfinity(x.AutoLifetime())) inf++;
            }
            Check(zero > 20, $"R26：库里 `destroyTime==0 且非 preventDestroy` 的实例数 > 20（实测 {zero}）");
            Check(zero == inf, $"R26：它们**全部**报 ∞（{inf}/{zero}）");
        }

        // ================================================================
        //  🆕 2026-10-25（特效动画域 · 完全复刻第 1 轮 · K5 `P1-4` / `AnimFXModuleBase.Initialize`）
        //  R27：`preventDestroy` 件的两条「原版语义」—— ① 持有者离场就被收 ② `isRetaliation` 算得出
        // ================================================================
        //  判据：`CardScript.cs:17167`（原版实例化在 `battleCardUI.effectsAnchor` 下 ⇒ 卡离场整棵子树一起没）
        //        `decomp_full/AnimFXModuleBase__Initialize.c:26-50`（`isRetaliation = actingCard.isPlayer != IsPlayerTurn()`）
        //  🧨 灭自证：① 断「还活着时不收」+「离场后真收」两半；② 两个方向各断一次（只断一边，
        //     把 `IsRetaliation` 写成常量 false / 常量 true 都会绿）。
        {
            WFEffectEntry pd = null;
            for (int i = 0; i < entries.Length; i++)
            {
                var x = entries[i];
                if (x != null && x.prefab != null && x.preventDestroy) { pd = x; break; }
            }
            Check(pd != null, "R27：库里找得到一件 `preventDestroy=1` 的效果（找不到这条断言没意义）");

            if (pd != null)
            {
                // ---- ① 持有者离场 ⇒ 收掉 ----
                var owner = new GameObject("__wf_owner_probe__");
                int before = WarpforgeEffectPlayer.OwnerReleased;
                var p = WarpforgeEffectPlayer.Play(pd.name, owner.transform, Vector3.zero, 1f);
                Check(p != null, "R27：`preventDestroy` 件起得来");
                if (p != null)
                {
                    p.Tick(0.05f);
                    Check(p != null && p.IsPlaying, "R27：持有者还活着 ⇒ 实例**不**被收（∞ 寿命 + 有持有者）");
                    owner.SetActive(false);                    // = 那张卡离场 / 视图收起
                    p.Tick(0.05f);
                    Check(WarpforgeEffectPlayer.OwnerReleased > before,
                          "R27：持有者 `activeInHierarchy=false` ⇒ 实例**被收掉**（原版：父节点连子件一起没）");
                    if (p != null) p.Kill();                   // 兜底（play 模式下 Destroy 会延迟到帧末）
                }
                UnityEngine.Object.DestroyImmediate(owner);

                // ---- ② `isRetaliation` 两个方向 ----
                var savedResolver = WFEffectCards.Resolver;
                var savedTurn = WFEffectCards.IsLocalPlayersTurn;
                try
                {
                    WFEffectCards.IsLocalPlayersTurn = () => true;      // 本地玩家回合
                    WFEffectCards.Resolver = _ => new WFEffectCardContext
                    { actingIsPlayer = true, valid = true };           // 出手的是我方卡
                    var same = WarpforgeEffectPlayer.Play(pd.name);
                    Check(same != null && !same.IsRetaliation,
                          "R27：我方卡 + 我方回合 ⇒ `IsRetaliation=false`（不是反击）");

                    WFEffectCards.Resolver = _ => new WFEffectCardContext
                    { actingIsPlayer = false, valid = true };          // 出手的是对方卡
                    var counter = WarpforgeEffectPlayer.Play(pd.name);
                    Check(counter != null && counter.IsRetaliation,
                          "R27：对方卡 + 我方回合 ⇒ `IsRetaliation=true`（反击）");
                    if (same != null) same.Kill();
                    if (counter != null) counter.Kill();
                }
                finally
                {
                    WFEffectCards.Resolver = savedResolver;
                    WFEffectCards.IsLocalPlayersTurn = savedTurn;
                }
            }
        }

        // ================================================================
        //  🆕 2026-10-25（D 域 · 特效重建链丢模块）
        //  R28：**特效 prefab 上那些「非 AnimFX 模块」的组件**现在会被补挂（原来一个都不携带）
        // ================================================================
        //  判据：普查表 `Resources/WarpforgeVFX/effect_extras.json`（88 件特效 / 164 处挂载）+ 原版方法体
        //        （`SyncParticleSeeds.cs:8-40` · `DestroyByTime.cs:11-30` · `SubEmitterManualTrigger.cs:17-50` …）。
        //  这三件是本轮点名的「已知会丢」：`SyncParticleSeeds`（36 prefab）· `DestroyByTime`（7 prefab）·
        //  `SubEmitterManualTrigger`（3 prefab）。
        //  🧨 灭自证：三件**各断一次**；只断一件时，把另外两件的 attach 删掉也照样绿。
        {
            Check(WFEffectExtras.EffectCount > 60,
                  $"R28：普查表读得到（88 件，实测 {WFEffectExtras.EffectCount}）");

            // ① `SyncParticleSeeds`
            var p1 = WarpforgeEffectPlayer.Play("Arco-Flagellant Strike");
            Check(p1 != null, "R28：`Arco-Flagellant Strike` 起得来（它带 `SyncParticleSeeds`）");
            if (p1 != null)
            {
                var seed = p1.GetComponentInChildren<WFExtraSyncParticleSeeds>(true);
                Check(seed != null, "R28：`SyncParticleSeeds` 被补上了（原来整类丢失）");
                // 真正的行为：被它管到的粒子必须 `useAutoRandomSeed=false` 且**共用同一个种子**
                var pss = p1.GetComponentsInChildren<ParticleSystem>(true);
                int fixedSeed = 0, seeded = 0, total = 0;
                foreach (var q in pss)
                {
                    if (q == null) continue;
                    total++;
                    if (q.useAutoRandomSeed) continue;
                    seeded++;
                    int s = unchecked((int)q.randomSeed);
                    if (fixedSeed == 0) fixedSeed = s;
                    else if (fixedSeed != s) fixedSeed = -1;
                }
                Check(total > 1 && seeded >= 1 && fixedSeed > 0,
                      $"R28：被 `SyncParticleSeeds` 管到的粒子**共用同一个随机种子**"
                    + $"（{seeded}/{total} 个被播种，种子 {fixedSeed}）");
                p1.Kill();
            }

            // ② `DestroyByTime`
            var p2 = WarpforgeEffectPlayer.Play("Blast_Big_Ork_Missiles");
            Check(p2 != null, "R28：`Blast_Big_Ork_Missiles` 起得来（它带 `DestroyByTime lifeTime=1.5`）");
            if (p2 != null)
            {
                var d = p2.GetComponentInChildren<WFExtraDestroyByTime>(true);
                Check(d != null && Mathf.Approximately(d.lifeTime, 1.5f),
                      "R28：`DestroyByTime` 被补上且 `lifeTime` 读到 1.5（我们以前走兜底时长）");
                p2.Tick(1.6f);          // 超过 1.5 ⇒ 应当被销毁
                Check(p2 == null || !p2.IsPlaying, "R28：……而且到 1.5s 真的被销毁（不是只挂了个空组件）");
                if (p2 != null) p2.Kill();
            }

            // ③ `SubEmitterManualTrigger`
            var ei = System.Array.FindIndex(entries, x => x != null && x.name == "Sau_ParticleWhip");
            if (ei >= 0)
            {
                var p3 = WarpforgeEffectPlayer.Play("Sau_ParticleWhip");
                Check(p3 != null, "R28：`Sau_ParticleWhip` 起得来（它带 `SubEmitterManualTrigger`）");
                if (p3 != null)
                {
                    var t = p3.GetComponentInChildren<WFExtraSubEmitterManualTrigger>(true);
                    Check(t != null, "R28：`SubEmitterManualTrigger` 被补上了（子发射器不再永不触发）");
                    p3.Kill();
                }
            }
            else Check(true, "R28：本库没有 `Sau_ParticleWhip`（跳过第 ③ 项的一半）");

            // ④ 🆕 2026-10-25 下半场：`ShakeAndSoundObject` / `MaterialFlickerEffect` / `SpriteFlickerEffect`
            //    也补上了 ⇒ 表里**每一种 kind 都有等价组件**，`Unimplemented` 必须停在 0。
            //    （将来新增 kind 但忘了在 `WFEffectExtras.Attach` 里加 case ⇒ 这条会红。）
            int beforeUnimpl = WFEffectExtras.Unimplemented;

            // ㋐ 屏震 + 定时音效（7 件带 `presetSO` 的其中之一）
            var p5 = WarpforgeEffectPlayer.Play("Environmental Condition Dark Angels Asteroid Zone");
            Check(p5 != null, "R28：`… Dark Angels Asteroid Zone` 起得来（带 `ShakeAndSoundObject`）");
            if (p5 != null)
            {
                var ss = p5.GetComponentInChildren<WFExtraShakeAndSound>(true);
                Check(ss != null, "R28：`ShakeAndSoundObject` 被补上了（原来环境卡开卡既没屏震也没声音）");
                if (ss != null)
                {
                    Check(ss.presets != null && ss.presets.Length > 0 && !string.IsNullOrEmpty(ss.presets[0]),
                          $"R28：……而且屏震 preset 读到了（`{(ss.presets != null && ss.presets.Length > 0 ? ss.presets[0] : "<空>")}`）");
                    Check(ss.sounds != null && ss.sounds.Length > 0 && !string.IsNullOrEmpty(ss.sounds[0].cue),
                          "R28：……而且音效 cue 读到了");
                }
                p5.Kill();
            }

            // ㋑ 材质闪烁（复用 `WFMaterialFlicker`，参数必须从数据灌进来）
            var p6 = WarpforgeEffectPlayer.Play("Environmental Condition Astra Militarum Dawn Attack");
            Check(p6 != null, "R28：`… Astra Militarum Dawn Attack` 起得来（带 `MaterialFlickerEffect`）");
            if (p6 != null)
            {
                var fl = p6.GetComponentInChildren<WFMaterialFlicker>(true);
                Check(fl != null, "R28：`MaterialFlickerEffect` 补成了 `WFMaterialFlicker`");
                if (fl != null)
                    Check(Mathf.Abs(fl.amplitude - 0.4f) < 1e-4f && Mathf.Abs(fl.frequency - 1f) < 1e-4f,
                          $"R28：……而且参数从数据灌进来了（amplitude={fl.amplitude} frequency={fl.frequency}，原版 0.4 / 1）");
                p6.Kill();
            }

            // ㋒ Sprite 闪烁（写 `SpriteRenderer.color`，不是材质）
            var p7 = WarpforgeEffectPlayer.Play("Orks Environmental Condition Night");
            Check(p7 != null, "R28：`Orks Environmental Condition Night` 起得来（带 `SpriteFlickerEffect`）");
            if (p7 != null)
            {
                var sf = p7.GetComponentInChildren<WFExtraSpriteFlicker>(true);
                Check(sf != null, "R28：`SpriteFlickerEffect` 被补上了");
                p7.Kill();
            }

            Check(WFEffectExtras.Unimplemented == beforeUnimpl,
                  $"R28：表里每一种 kind 都有等价组件 ⇒ 未实现计数**不增加**"
                + $"（{beforeUnimpl} → {WFEffectExtras.Unimplemented}；若变红 = 有人加了 kind 却没加 case）");
        }

        Done();
    }

    // 只用来算「库里该有多少」——形状要跟 `数据/游戏数据/animfx_modules.json` 对齐
    [System.Serializable] class ModuleDoc { public int effect_count; public int module_count; public ModuleEffect[] effects; }
    [System.Serializable] class ModuleEffect { public string name; public WFModuleDef[] modules; }

    // ③c 用来**现读分母**（`Resources/WarpforgeVFX/effect_extras.json` 的最小形状；`extras` 里的
    // 其余字段本段不用，`JsonUtility` 忽略未知键）。
    // ⚠️ ⛔ 别改成复用 `WarpforgeVFX.WFEffectExtraDoc` / `WFEffectExtraRec` —— 那两个是 `internal`，
    //   编辑器程序集（`Assembly-CSharp-Editor`）看不见（本仓没有 `InternalsVisibleTo`，
    //   同 `RuleEngine/Core/EffectResolver.cs:7794` 那条记录）。
    [System.Serializable] class AnimFxExtraDoc { public AnimFxExtraRec[] effects; }
    [System.Serializable] class AnimFxExtraRec { public string effect; public AnimFxExtraItem[] extras; }
    [System.Serializable] class AnimFxExtraItem { public string kind; public string node; }

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
