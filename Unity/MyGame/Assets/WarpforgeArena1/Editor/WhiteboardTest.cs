// WhiteboardTest.cs — 特效「播 → 消失」闭环的批处理自检
//
// 验三件事（都是「能不能用」的硬指标，不是「好不好看」）：
//   1. 播放入口能用 —— 按名字从效果库取 prefab 播出来，渲染**确实有内容**（不是空/纯色块）
//   2. 生命周期闭环 —— 每个效果到点自己 Exit、再过 exitDestroyTime 自己销毁，ActiveCount 归 0
//   3. 不泄漏 —— 反复铺几轮，对象数/粒子系统数/材质数都不涨
//
// 为什么能在批处理里跑：粒子在编辑器下不会自己推进，所以由本工具用
// ParticleSystem.Simulate(dt) 显式推、用 WarpforgeEffectPlayer.Tick(dt) 显式推。
// 两边步长一致，等价于 play 模式的逐帧。**不进 play 模式**，结果可复现、可反复跑。
//
// ⚠️ 但「编辑器里过」不等于「打包后过」—— 这个项目踩过（shader 被 Shader.Find 找不到、
//    bundle 加载冲突都是只在一边发作）。所以本工具跑完，还要按交接文档 P0 那条走一次
//    **构建后的 player**。别把这里的绿灯当成发布绿灯。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod WhiteboardTest.Run -logFile "d:/4/_tmp_view/whiteboard.log"
//   筛输出：grep "^WB " d:/4/_tmp_view/whiteboard.log
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WarpforgeVFX;

public static class WhiteboardTest
{
    const string P = "WB ";

    /// <summary>要自检的效果名。**空 = 自动挑**：从台账里挑「对得上 + 高置信」的。
    /// 挑好的那些，「渲染有内容」这条断言才有意义 —— 挑一堆已知有问题的，失败了也说不清是谁的错。</summary>
    static readonly string[] NameFilter = { "ArtificeEffect", "Explosion_Ground", "Spore Explosion",
                                            "Sword_Slash", "Unit Freezing", "EnvironmentalCondition Tau Solar Eclipse",
                                            "Sororitas_BladeOfFaith", "Godspear Warhead Full",
                                            "BulletImpact_Tau_Sniper Pulse Rifle" };
    const int AutoPick = 20;

    const int LeakCycles = 5;               // 反复铺几轮
    static readonly float[] Times = { 0.15f, 0.30f, 0.50f, 0.75f, 1.00f, 1.50f, 2.00f, 3.00f };
    const float Dt = 1f / 30f;              // 推进步长（和 play 模式的帧率一个量级）
    const float TailMargin = 1.5f;          // 寿命之后再多推这么久，确认它真的自己走了

    const int W = 256, H = 256;
    static readonly Color Bg = new Color(0.07f, 0.08f, 0.10f, 1f);
    const string OutDir = @"d:/4/_tmp_view/whiteboard";
    const string ReportPath = "Assets/WarpforgeVFX/白板自检报告.tsv";
    const string LibraryPath = "Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset";

    static Camera _cam;

    [MenuItem("Tools/Warpforge/白板自检（播→消失闭环）")]
    public static void Run()
    {
        Debug.Log(P + "=== 白板自检 开始 ===");
        Directory.CreateDirectory(OutDir);

        var lib = AssetDatabase.LoadAssetAtPath<WarpforgeEffectLibrary>(LibraryPath);
        if (lib == null)
        {
            Debug.LogError(P + $"没有效果库资产 {LibraryPath} —— 先跑 Tools > Warpforge > 生成效果库");
            return;
        }
        WarpforgeEffectLibrary.Instance = lib;          // 让 Play 找得到（不进 play 模式也要能用）
        Debug.Log(P + $"效果库 {lib.Count} 个（{lib.generated}）");

        var names = Pick(lib);
        Debug.Log(P + $"本次自检 {names.Count} 个效果：{string.Join(", ", names)}");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Cam");
        _cam = camGo.AddComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Bg;

        var sb = new StringBuilder();
        sb.AppendLine("效果名\t判定\t置信度\t亮度比\tdestroyTime\texitDestroyTime\tpreventDestroy\t自然时长\t循环"
                    + "\t取景半径\t相机距离"
                    + "\t" + string.Join("\t", Times.Select(t => $"亮点@{t:F2}s"))
                    + "\t峰值亮点\t实际存活\t自己销毁\t预计寿命\t结论");

        int okRender = 0, okLife = 0, prevented = 0, lowConf = 0;
        var badRender = new List<string>();
        var badLife = new List<string>();

        // ---- 第一轮：逐个效果，看渲染 + 看它自己走不走 ----
        foreach (var n in names)
        {
            WFEffectEntry e;
            if (!lib.TryGet(n, out e)) { Debug.LogWarning(P + $"库里没有 {n}"); continue; }

            // ---- 1a. 渲染：播放器创建的实例**确实有内容**吗 ----
            // 两件事分开做，因为它们要的推进方式不一样，混在一起会互相污染：
            //   渲染检查要**精确落在采样时刻**上（瞬时闪光只有 0.15s，1/30 的步长会错过）
            //   寿命检查根本不需要粒子动，只推时钟
            // 这里用扫描工具的绝对时刻法（和台账同一把尺子），但实例走的是 Player.Play ——
            // 于是顺带验证了「按名字取 prefab + binder 重建材质」这一段。
            var pr = WarpforgeEffectPlayer.Play(e);
            if (pr == null) { Debug.LogWarning(P + $"  {n} 播不出来"); continue; }
            var frame = FrameForInstance(pr.gameObject);
            ApplyFrame(frame);

            var lits = new List<int>();
            foreach (var ts in Times)
            {
                foreach (var ps in pr.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Simulate(ts, withChildren: true, restart: true, fixedTimeStep: false);
                    ps.Play();
                }
                lits.Add(RenderLit());
            }
            int peak = lits.Count > 0 ? lits.Max() : 0;
            // 判定分档，并且**拿台账当期望值** ——
            //   · 台账 W/C 组（两边全程空 / 只有原版有）本来就是空的，播出来没内容是**符合预期**
            //   · 台账「低置信」的这几档本来就没法判定（原版自己也只有几十个亮点），
            //     差一点不算故障 —— 这正是台账加置信度这一列的意义，别在这里又硬给结论
            // 早先只按「峰值 < 60 就算失败」判，把 Bolter Casing_*（台账峰值 33）和
            // CardDisappear（台账峰值 0）报成了故障，白查一轮。
            const int NoiseFloor = 60;                    // 和台账的 LIT_MIN 同一个口径
            bool expectContent = e.verdict != "W" && e.verdict != "C";
            bool confident = e.confidence == "高" || e.confidence == "中";
            bool hasContent = peak >= NoiseFloor;

            if (hasContent || !expectContent) okRender++;
            else if (!confident) lowConf++;
            else badRender.Add(n);

            string note = "";
            if (expectContent && !hasContent)
                note = confident ? $"  ⚠️ 台账判「{e.verdict}」该有内容，实际只有 {peak} 个亮点"
                                 : $"  （台账「{e.verdict}」低置信，{peak} 个亮点，判定不了）";
            else if (!expectContent && hasContent)
                note = $"  （台账判「{e.verdict}」，实际有 {peak} 个亮点）";
            pr.Kill();
            WarpforgeEffectPlayer.KillAll();

            // ---- 1b. 生命周期：不碰粒子，只推时钟 ----
            var p = WarpforgeEffectPlayer.Play(e);
            if (p == null) continue;
            p.autoTick = false;
            float expect = p.ExpectedLifetime;

            float t = 0f;
            bool gone = false;
            float goneAt = -1f;
            float maxT = (float.IsPositiveInfinity(expect) ? 8f : expect) + TailMargin;

            while (t < maxT)
            {
                p.Tick(Dt);
                t += Dt;
                if (p == null) { gone = true; goneAt = t; break; }
            }

            // 没自己走的：不一定是 bug（原版 preventDestroy 的那 97 个就是要外部收），但要记下来
            if (!gone)
            {
                if (e.preventDestroy) prevented++;
                else { badLife.Add(n); }
                WarpforgeEffectPlayer.KillAll();
            }

            if (gone && !e.preventDestroy) okLife++;

            sb.AppendLine(string.Join("\t", new[]
            {
                n, e.verdict, e.confidence, e.ratio >= 0f ? e.ratio.ToString("F2") : "",
                e.destroyTime >= 0f ? e.destroyTime.ToString("F2") : "",
                e.exitDestroyTime >= 0f ? e.exitDestroyTime.ToString("F2") : "",
                e.preventDestroy ? "1" : "",
                e.natural >= 0f ? e.natural.ToString("F2") : "",
                e.loops ? "1" : "",
                frame.radius.ToString("F2", CultureInfo.InvariantCulture),
                Vector3.Distance(frame.pos, frame.look).ToString("F2", CultureInfo.InvariantCulture),
            }
            .Concat(lits.Select(x => x.ToString(CultureInfo.InvariantCulture)))
            .Concat(new[]
            {
                peak.ToString(CultureInfo.InvariantCulture),
                gone ? goneAt.ToString("F2", CultureInfo.InvariantCulture) : "没走",
                gone ? "1" : "0",
                float.IsPositiveInfinity(expect) ? "不自动收" : expect.ToString("F2", CultureInfo.InvariantCulture),
                note.Trim(),
            })));

            Debug.Log(P + $"  {n}  峰值亮点 {peak}  存活 {(gone ? goneAt.ToString("F2") + "s" : "没走")}"
                        + $"  预计 {expect:F2}s{note}");
        }

        // ---- 第二轮：白板铺开 + 反复播，看泄漏 ----
        var wbGo = new GameObject("Whiteboard");
        var wb = wbGo.AddComponent<VFXWhiteboard>();
        wb.columns = 5;
        wb.cellSize = 4f;
        wb.stagger = 0.05f;
        wb.fitToCell = true;

        var counts = new List<string>();
        int leakBad = 0;
        int baseAlive = -1, basePs = -1, baseMat = -1, baseAfterKill = -1;
        for (int cycle = 0; cycle < LeakCycles; cycle++)
        {
            wb.Spawn(names);
            float t = 0f;
            bool shot = false;
            // 会超时是正常的：preventDestroy 那几个本来就不自己走（这正是要覆盖的分支）。
            // 这一轮验的是「铺出去的会不会全清干净」，粒子动不动不影响结论 ——
            // 但**实拍图要粒子动起来才看得见东西**，所以照推不误（不然整张图是空的）。
            while (!wb.Done && t < 45f)
            {
                wb.Tick(Dt);
                StepParticles(Dt);
                t += Dt;
                // 第一轮播到 1.4 秒时留一张实拍图 —— 这个时刻全部铺开了（错峰 0.05s × 24 个 = 1.2s），
                // 而且大部分效果正在最亮的时候。取 2.5s 的话，一堆短命效果（destroyTime 2s 那批）已经死了，
                // 图上看过去只剩零星几个，会让人误以为「大部分没渲染」。
                if (cycle == 0 && !shot && t >= 1.4f && wb.SpawnedTotal > 0)
                {
                    shot = true;
                    ApplyFrame(FrameForCell(wb));
                    SavePng($"{OutDir}/whiteboard.png");
                    DumpWhiteboard(wb);
                }
            }

            int alive = WarpforgeEffectPlayer.ActiveCount;
            int psCount = UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true).Length;
            int mats = UnityEngine.Object.FindObjectsOfType<Renderer>(true)
                                       .SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().Count();

            // 判据是**跨轮不增长**，不是「必须为 0」—— preventDestroy 的效果本来就赖着不走。
            // ⚠️ 早先写的是 alive != 0 就算泄漏，于是每轮都被判失败（3 个 preventDestroy 常驻），
            //    而其实三轮的 3 / 33 / 27 一模一样，恰恰说明**没有泄漏**。
            if (cycle == 0) { baseAlive = alive; basePs = psCount; baseMat = mats; }
            bool grew = alive > baseAlive || psCount > basePs || mats > baseMat;
            if (grew) leakBad++;
            counts.Add($"第 {cycle + 1} 轮：耗时 {t:F2}s，全局存活 {alive}，粒子系统 {psCount}，材质 {mats}"
                     + (cycle == 0 ? "（基准）" : grew ? "  ⚠️ 比基准多" : "  与基准一致"));

            // 收干净再跑下一轮：KillAll 之后必须归零，否则就是 KillAll 漏了
            wb.Clear();
            WarpforgeEffectPlayer.KillAll();
            int afterKill = WarpforgeEffectPlayer.ActiveCount;
            if (cycle == 0) baseAfterKill = afterKill;
            if (afterKill != 0 || afterKill != baseAfterKill) leakBad++;
            if (afterKill != 0) counts.Add($"        ⚠️ KillAll 之后还剩 {afterKill} 个");
        }
        foreach (var c in counts) Debug.Log(P + "  " + c);
        UnityEngine.Object.DestroyImmediate(wbGo);

        File.WriteAllText(ReportPath, sb.ToString());

        // ---- 结论 ----
        Debug.Log(P + "=== 结束 ===");
        Debug.Log(P + $"  闭环：自己销毁 {okLife}/{names.Count}"
                    + $"（另有 {prevented} 个是原版 preventDestroy，本来就该外部收）");
        Debug.Log(P + $"  渲染：与台账一致 {okRender}/{names.Count}"
                    + (lowConf > 0 ? $"（另有 {lowConf} 个台账置信度低，判定不了，不算失败）" : ""));
        if (badRender.Count > 0) Debug.LogWarning(P + $"  ⚠️ 台账说该有内容却没渲出来：{string.Join(", ", badRender)}");
        if (badLife.Count > 0) Debug.LogWarning(P + $"  ⚠️ 该自己走却没走：{string.Join(", ", badLife)}");
        Debug.Log(P + (leakBad == 0
            ? $"  泄漏：{LeakCycles} 轮计数完全一致、且 KillAll 后归零 ✅"
            : $"  ⚠️ 有 {leakBad} 处计数异常，见上面的每轮统计"));
        Debug.Log(P + $"  报告 {ReportPath}，图 {OutDir}/whiteboard.png");
    }

    // ---- 粒子推进 ----
    // ⚠️ 只推粒子，**不推玩家** —— 玩家的 Tick 由调用方推（第一轮直接推、第二轮白板推）。
    //    两边都推就走两倍速，寿命检查会假性通过。
    static void StepParticles(float dt)
    {
        foreach (var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true))
        {
            if (ps == null) continue;
            try { ps.Simulate(dt, withChildren: false, restart: false, fixedTimeStep: false); }
            catch { }
        }
    }

    // ---- 白板实拍时把每个渲染器的 shader 和尺寸打出来 ----
    // 实拍图里出现「一整块品红」时要能立刻指出是哪一块 —— 品红（Hidden/InternalErrorShader）
    // 说明那个材质的 shader 没解析到/编译不了，是**渲染这一侧**的问题，不是还原的问题。
    static void DumpWhiteboard(VFXWhiteboard wb)
    {
        var rends = wb.GetComponentsInChildren<Renderer>(true);
        Debug.Log(P + $"  [实拍] 白板上共 {rends.Length} 个渲染器：");
        foreach (var r in rends)
        {
            var m = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
            string sh = (m != null && m.shader != null) ? m.shader.name : "(无)";
            bool bad = sh.StartsWith("Hidden/InternalErrorShader") || sh == "Sprites/Default";
            if (bad || r.bounds.extents.magnitude > 3f)
                Debug.Log(P + $"    {(bad ? "⚠️品红 " : "大块 ")}{r.name}  shader={sh}"
                            + $"  半径={r.bounds.extents.magnitude:F2}  材质={(m != null ? m.name : "无")}");
        }
    }

    static List<string> Pick(WarpforgeEffectLibrary lib)
    {
        var all = lib.Names.ToList();
        if (NameFilter.Length > 0)
            return all.Where(x => NameFilter.Any(k => x.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                      .Take(AutoPick).ToList();

        // 自动挑：优先「对得上 + 高置信」——这些是还原得最像原版的，
        // 它们要是渲染不出内容，那就是**播放这一侧**坏了，不是还原的锅。
        // 取的时候**跨全表等距抽样**，别只取字母序前 20 个（那样永远只测到 A 开头的）。
        var good = new List<string>();
        foreach (var n in all)
        {
            var e = lib.Get(n);
            if (e != null && e.verdict == "Z" && e.confidence == "高" && !e.preventDestroy) good.Add(n);
        }
        var picked = new List<string>();
        var fullscreen = new List<string>();
        int body = Mathf.Max(1, AutoPick - 4);
        // 跨全表等距取样（步长 = 表长/目标数 的一半，保证跳过全屏类之后还剩够）
        int stride = Mathf.Max(1, good.Count / Mathf.Max(1, body * 2));
        for (int i = 0; i < good.Count && picked.Count < body; i += stride)
        {
            var n = good[i];
            var e = lib.Get(n);
            // 全屏类（战场环境叠加）不进白板格子 —— 铺进去会糊满整屏，把别人的检查也盖掉
            if (VFXWhiteboard.IsFullscreen(e.prefab)) { fullscreen.Add(n); continue; }
            if (!picked.Contains(n)) picked.Add(n);
        }
        if (fullscreen.Count > 0)
            Debug.Log(P + $"  （跳过 {fullscreen.Count} 个全屏类效果，它们不是「摆一格的」："
                        + string.Join(", ", fullscreen.Take(5)) + (fullscreen.Count > 5 ? " …" : "") + "）");

        // 再补几个「特殊分支」的，保证那些代码路径每轮都被走到：
        // preventDestroy（原版不自己销毁）和「原版压根没有 AnimFXController」（寿命全靠兜底）
        foreach (var n in all)
        {
            if (picked.Count >= AutoPick + 4) break;
            var e = lib.Get(n);
            if (e == null || picked.Contains(n)) continue;
            if (e.preventDestroy || e.destroyTime < 0f) picked.Add(n);
        }
        return picked.Distinct().ToList();
    }

    // ---- 渲染 ----
    static int RenderLit()
    {
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        _cam.targetTexture = rt;
        _cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        _cam.targetTexture = null;

        var px = tex.GetPixels();
        int lit = 0;
        const float thr = 6f / 255f * 3f;
        foreach (var c in px)
            if (Mathf.Abs(c.r - Bg.r) + Mathf.Abs(c.g - Bg.g) + Mathf.Abs(c.b - Bg.b) > thr) lit++;

        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        return lit;
    }

    static void SavePng(string path)
    {
        var rt = RenderTexture.GetTemporary(W * 4, H * 4, 24, RenderTextureFormat.ARGB32);
        _cam.targetTexture = rt;
        _cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W * 4, H * 4, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W * 4, H * 4), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        _cam.targetTexture = null;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
    }

    struct Frame { public Vector3 pos, look; public float fov, near, far, radius; }

    /// <summary>取景：量**正在播的那个实例**在整个时间轴上的最大包围盒，保证每个采样时刻都框得住
    /// （只按某一帧构图会让另外几帧跑出画面，数字就假了）。
    ///
    /// ⚠️ 必须量「要渲染的那个实例」。早先版本量的是原样实例化的临时副本，而 Player.Play 会把实例
    /// 归位到父节点原点 —— 原版有一批效果把根节点停在场外 **x≈100**（Atk_GrotGrenade 在
    /// (99.59,1.24,-0.37)、Attack_Stomp 在 (100,0.725,0)），于是变成「相机看 x=100、效果在原点」，
    /// 渲染出来全是 0 亮点，看着像「还原失败」。**是尺子错了，不是资产错了。**</summary>
    static Frame FrameForInstance(GameObject root)
    {
        Bounds b = new Bounds(Vector3.zero, Vector3.one);
        bool first = true;
        var perTime = new List<string>();
        foreach (var t in Times)
        {
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Simulate(t, withChildren: true, restart: true, fixedTimeStep: false);
            }
            Bounds bt = new Bounds(Vector3.zero, Vector3.one);
            bool f2 = true;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (f2) { bt = r.bounds; f2 = false; } else bt.Encapsulate(r.bounds);
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            perTime.Add($"{t:F2}:{bt.extents.magnitude:F2}");
        }
        // 逐时刻半径打出来 —— 只要有一个时刻的包围盒爆掉（拖尾 / Stretch 粒子会），
        // 相机就会被推得极远，整片效果缩成一个点，看起来也像「什么都没渲染」
        Debug.Log(P + $"  [取景] {root.name} 各时刻半径 " + string.Join("  ", perTime));
        return FrameFromBounds(b);
    }

    /// <summary>白板整体取景：格子是垂直于相机的二维墙面（X 向右、Y 向下），
    /// 按墙的宽高算包围盒即可 —— 不是沿 Z 往后排，所以不用把深度也算进去。</summary>
    static Frame FrameForCell(VFXWhiteboard wb)
    {
        int cols = Mathf.Max(1, wb.columns);
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)wb.SpawnedTotal / cols));
        float wide = cols * wb.cellSize;
        float tall = rows * wb.cellSize * VFXWhiteboard.RowFactor;
        var center = new Vector3(0f, -(rows - 1) * wb.cellSize * VFXWhiteboard.RowFactor * 0.5f, 0f);
        float r = Mathf.Max(wide, tall) * 0.62f;
        return FrameFromBounds(new Bounds(center, new Vector3(r * 2f, r * 2f, r * 2f)));
    }

    static Frame FrameFromBounds(Bounds b)
    {
        float radius = Mathf.Max(0.5f, b.extents.magnitude);
        const float fov = 40f;
        float dist = radius / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 1.15f;
        return new Frame
        {
            look = b.center,
            pos = b.center + new Vector3(0f, 0f, -dist),
            fov = fov, near = 0.01f, far = radius * 100f,
            radius = radius,
        };
    }

    static void ApplyFrame(Frame f)
    {
        _cam.fieldOfView = f.fov;
        _cam.nearClipPlane = f.near;
        _cam.farClipPlane = f.far;
        _cam.transform.position = f.pos;
        _cam.transform.LookAt(f.look);
    }
}
