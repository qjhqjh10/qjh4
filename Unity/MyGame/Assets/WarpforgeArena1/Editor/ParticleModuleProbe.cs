// ParticleModuleProbe.cs — 原版 prefab vs 导出 prefab，**逐粒子系统 · 逐模块 · 逐字段**对照
//
// 为什么要它（C 组最后一个 `PinDownEffect`）：
//   · `CEmitProbe` 已经打到「渲染器」那一层 —— 渲染器 / 活粒子数 / 材质 / shader / render queue **五项全同**；
//   · `MatIsoProbe` 把「材质属性值」那一层也排掉了 —— 原版 prefab + 全部导出材质 = **相对基准 0.0%**；
//   ⇒ 两个方向都排除之后，「渲出来了但极暗」只可能差在**粒子系统的模块值**上
//     （`startColor` / `colorOverLifetime` / `startSize` 这一族），而那两个探针**都没打这一层**。
//   出处与推理链：`资料/特效还原_进度与交接.md` §〇之三 三。
//
// 判据：按**层级路径**把两侧的 ParticleSystem 配上号，逐模块逐字段比。
//   · 曲线/渐变类字段按 **9 个采样点**比（形状一样才算同）—— 比首尾两个点更能抓住「中间塌了」。
//   · 只打印**不同的**字段（打 `★`），相同的只计数 —— 一个效果几百个字段，全打没法看。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod ParticleModuleProbe.Run -logFile "d:/4/_tmp_view/pmprobe.log"
//   筛输出：grep "^PM " d:/4/_tmp_view/pmprobe.log
//   只看差异：grep "^PM " ... | grep "★"
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ParticleModuleProbe
{
    const string P = "PM ";
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";

    /// <summary>要比的效果。
    /// 默认组 = C 组最后一个 + D 组（「只有导出有内容」）里最大的两族各取一个样本 ——
    /// D 组那两族（`Plasma_basic_*` 10 条 / `BulletImpact_*` 11 条）**几乎全是低置信**，
    /// 且原版侧**所有时段都是「—」**；而它们是游戏里天天在播的常见特效
    /// ⇒ 先验「**原版 prefab 自己到底有没有粒子**」，判断是资产问题还是**原版那一趟没渲出来**。</summary>
    static readonly string[] Targets = {
        "PinDownEffect",
        "Plasma_basic_blue", "Plasma_basic_red", "Plasma_basic OLD",
        "BulletImpact_artillery_big", "BulletImpact_autogun_Automatic",
        "Sword_Slash_User_DA", "Spore Explosion", "Godspear Warhead Full",
    };

    /// <summary>先只比这几个粒子系统（留空 = 全比）。`PinDownEffect` 的问题集中在 `Trait Icon` 那一支</summary>
    static readonly string[] OnlyPaths = { };

    const int Samples = 9;      // 曲线/渐变采样点数

    static int _diff, _same;

    public static void Run()
    {
        Debug.Log(P + "=== 粒子模块逐字段对照 开始 ===");
        Debug.Log(P + "  判据：按层级路径配对；曲线/渐变按 " + Samples + " 点采样比；只打**不同**的字段");

        AssetBundle vfx = null;
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b != null && Path.GetFileName(f) == VfxBundleName) vfx = b;
        }
        if (vfx == null) { Debug.LogError(P + "特效 bundle 未加载：" + VfxBundleName); return; }

        var originals = new Dictionary<string, GameObject>();
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g != null) originals[g.name] = g;
        }

        foreach (var name in Targets)
        {
            GameObject orig;
            if (!originals.TryGetValue(name, out orig)) { Debug.LogWarning(P + $"原版没有 {name}"); continue; }
            var expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
            if (expPrefab == null) { Debug.LogWarning(P + $"导出没有 {name}"); continue; }

            _diff = 0; _same = 0;
            Debug.Log(P + $"########## {name} ##########");
            Compare(name, orig, expPrefab);
            Debug.Log(P + $"  [{name}] 汇总：**{_diff} 个字段不同** / {_same} 个相同");
        }

        Debug.Log(P + "=== 结束 ===");
    }

    static void Compare(string label, GameObject orig, GameObject expPrefab)
    {
        // 两侧都要跑一遍 binder（导出侧靠它把 shader 映射上）—— 但**模块值不受它影响**，
        // 这里只是为了和另外两个探针的工况一致。
        var o = UnityEngine.Object.Instantiate(orig);
        var e = UnityEngine.Object.Instantiate(expPrefab);
        try
        {
            var ob = o.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
            if (ob != null) ob.Apply();
            var eb = e.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
            if (eb != null) eb.Apply();

            var oPs = Map(o);
            var ePs = Map(e);

            // 🔴 **2026-09-17 补的三层**：前两轮已经把「渲染器 / 材质 / 粒子模块值」都比过了，
            //    所以这里补上**从没比过**的三样 —— 变换、Mesh、以及**完整的材质表**。
            //    `MatIsoProbe` 的结论是「原版 prefab + 导出材质 = 0.0%」，但那是**拿原版 prefab 当载体**；
            //    导出 prefab 自己的 transform / mesh 从没被比过。
            CompareTransforms(o, e);
            CompareRenderables(o, e);

            // ⚠️ 上面那两条都是**按序号配对**的 —— 而 `PinDownEffect` 底下**两个渲染器同名**，
            //    两侧的同级顺序**不保证一致**（导出器克隆时可能换序）。所以这里再**无条件把两侧
            //    每个可渲染件的关键状态都打出来**，一眼就能看出「是不是同一批配置、只是顺序不同」。
            DumpRenderers("原版", o);
            DumpRenderers("导出", e);

            // 只为看一眼规模：Simulate 之后的活粒子数（沿用另两个探针的 SimTime）
            foreach (var kv in oPs) { kv.Value.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
            foreach (var kv in ePs) { kv.Value.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
            foreach (var kv in oPs) kv.Value.Simulate(1.2f, withChildren: true, restart: true, fixedTimeStep: false);
            foreach (var kv in ePs) kv.Value.Simulate(1.2f, withChildren: true, restart: true, fixedTimeStep: false);

            var keys = new List<string>(oPs.Keys);
            foreach (var k in ePs.Keys) if (!oPs.ContainsKey(k)) keys.Add(k);
            keys.Sort(StringComparer.Ordinal);

            foreach (var k in keys)
            {
                if (OnlyPaths.Length > 0 && Array.IndexOf(OnlyPaths, k) < 0) continue;

                ParticleSystem a, b;
                bool hasA = oPs.TryGetValue(k, out a), hasB = ePs.TryGetValue(k, out b);
                if (!hasA || !hasB)
                {
                    _diff++;
                    Debug.Log(P + $"    ★ {k,-40} 只有{(hasA ? "原版" : "导出")}有");
                    continue;
                }
                Debug.Log(P + $"  ── {k}（活粒子 原版 {a.particleCount} / 导出 {b.particleCount}）");
                OneSystem(k, a, b);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(o);
            UnityEngine.Object.DestroyImmediate(e);
        }
    }

    /// <summary>逐 Transform 比（按层级路径配对）—— **这一层以前从没比过**。
    /// 位置/旋转/缩放任一不同，效果就会渲到别处、或者大小不对（亮度和会跟着变）。</summary>
    static void CompareTransforms(GameObject o, GameObject e)
    {
        var byPath = new Dictionary<string, Transform>();
        foreach (var t in e.GetComponentsInChildren<Transform>(true))
            byPath[PathOf(t, e.transform)] = t;

        foreach (var t in o.GetComponentsInChildren<Transform>(true))
        {
            var p = PathOf(t, o.transform);
            Transform et;
            if (!byPath.TryGetValue(p, out et)) { _diff++; Debug.Log(P + $"    ★ [变换] {p} 只有**原版**有"); continue; }
            F(p, "T.localPosition", V(t.localPosition), V(et.localPosition));
            F(p, "T.localRotation", Q(t.localRotation), Q(et.localRotation));
            F(p, "T.localScale", V(t.localScale), V(et.localScale));
        }
    }

    /// <summary>逐个**可渲染件**比：完整材质表 + **Mesh** + **Sprite**。
    /// ⚠️ `MatIsoProbe` 只看了「**原版 prefab** 换导出材质」；导出 prefab 自己挂的 mesh / sprite
    /// **从没被比过**。`Trait Icon/Chains` 的 `renderMode = Mesh` ⇒ 这条最可疑。</summary>
    static void CompareRenderables(GameObject o, GameObject e)
    {
        var byPath = new Dictionary<string, Renderer>();
        foreach (var r in e.GetComponentsInChildren<Renderer>(true))
            byPath[PathOf(r.transform, e.transform)] = r;

        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            var p = PathOf(r.transform, o.transform);
            Renderer er;
            if (!byPath.TryGetValue(p, out er)) { _diff++; Debug.Log(P + $"    ★ [可渲染件] {p} 只有**原版**有"); continue; }

            var om = r.sharedMaterials; var em = er.sharedMaterials;
            F(p, "R.materialCount", om.Length.ToString(), em.Length.ToString());
            int n = Mathf.Min(om.Length, em.Length);
            for (int i = 0; i < n; i++)
            {
                F(p, $"R.material[{i}].name", om[i] == null ? "<null>" : om[i].name,
                                              em[i] == null ? "<null>" : em[i].name);
                F(p, $"R.material[{i}].shader", om[i] == null ? "<null>" : om[i].shader.name,
                                                em[i] == null ? "<null>" : em[i].shader.name);
                F(p, $"R.material[{i}].mainTexture", TexName(om[i]) + MatDetail(om[i]), TexName(em[i]) + MatDetail(em[i]));
            }
            F(p, "R.enabled", r.enabled.ToString(), er.enabled.ToString());
            F(p, "R.sortingOrder", r.sortingOrder.ToString(), er.sortingOrder.ToString());

            var pr = r as ParticleSystemRenderer; var pe = er as ParticleSystemRenderer;
            if (pr != null || pe != null)
            {
                F(p, "PSR.mesh", pr == null || pr.mesh == null ? "<null>" : pr.mesh.name,
                                pe == null || pe.mesh == null ? "<null>" : pe.mesh.name);
                F(p, "PSR.meshVertexCount", pr == null || pr.mesh == null ? "-" : pr.mesh.vertexCount.ToString(),
                                            pe == null || pe.mesh == null ? "-" : pe.mesh.vertexCount.ToString());
            }
            var sr = r as SpriteRenderer; var se = er as SpriteRenderer;
            if (sr != null || se != null)
            {
                F(p, "SpriteRenderer.sprite", sr == null || sr.sprite == null ? "<null>" : sr.sprite.name,
                                              se == null || se.sprite == null ? "<null>" : se.sprite.name);
                F(p, "SpriteRenderer.drawMode", sr == null ? "-" : sr.drawMode.ToString(),
                                                se == null ? "-" : se.drawMode.ToString());
                F(p, "SpriteRenderer.size", sr == null ? "-" : Rng(sr.size), se == null ? "-" : Rng(se.size));
                // 🔴 **`maskInteraction` 是关键那一个**：`VisibleInsideMask` 的渲染器**只在遮罩区域内可见**，
                //    遮罩一空/一移位它就整个消失 —— 而它**不是** `SpriteMask` 上的字段（`showSprite` 不存在，
                //    2026-09-17 在这里编译错过一次）。「Show Mask Graphic」= 这个枚举。
                F(p, "SpriteRenderer.maskInteraction", sr == null ? "-" : sr.maskInteraction.ToString(),
                                                       se == null ? "-" : se.maskInteraction.ToString());
            }
            // ⚠️ **`SpriteMask` 不是 `SpriteRenderer`** —— 上面那段 `as SpriteRenderer` 对 `SpriteMask`
            //    返回 **null**，所以「遮罩那份的 sprite 是什么」从来没被比过。补上。
            var smA = r as SpriteMask; var smB = er as SpriteMask;
            if (smA != null || smB != null)
            {
                F(p, "SpriteMask.sprite", smA == null || smA.sprite == null ? "<null>" : smA.sprite.name,
                                          smB == null || smB.sprite == null ? "<null>" : smB.sprite.name);
                F(p, "SpriteMask.alphaCutoff", smA == null ? "-" : G(smA.alphaCutoff),
                                               smB == null ? "-" : G(smB.alphaCutoff));
                F(p, "SpriteMask.isCustomRangeActive", smA == null ? "-" : smA.isCustomRangeActive.ToString(),
                                                        smB == null ? "-" : smB.isCustomRangeActive.ToString());
                F(p, "SpriteMask.frontSortingOrder", smA == null ? "-" : smA.frontSortingOrder.ToString(),
                                                     smB == null ? "-" : smB.frontSortingOrder.ToString());
                F(p, "SpriteMask.backSortingOrder", smA == null ? "-" : smA.backSortingOrder.ToString(),
                                                    smB == null ? "-" : smB.backSortingOrder.ToString());
            }
        }
    }

    /// <summary>材质本体的诊断：它是**工程资产**还是**bundle 里的原件**？有没有 `_BaseMap` / `_MainTex`？
    /// —— 「贴图丢了」有两条完全不同的路：① 属性名对不上（`HasProperty` false ⇒ 搬运时静默 continue）；
    /// ② 属性在、值是空（搬运时没拿到源贴图）。这两个数一眼分开。</summary>
    static string MatDetail(Material m)
    {
        if (m == null) return "";
        var path = UnityEditor.AssetDatabase.GetAssetPath(m);
        var bm = m.GetTexture("_BaseMap");
        return $" [asset={(string.IsNullOrEmpty(path) ? "**bundle内原件**" : System.IO.Path.GetFileName(path))}"
             + $" id={m.GetInstanceID()} name={m.name}"
             + $" has_BaseMap={m.HasProperty("_BaseMap")} has_MainTex={m.HasProperty("_MainTex")}"
             + $" _BaseMap={(bm == null ? "<null>" : bm.name)}]";
    }

    static string TexName(Material m)
    {
        if (m == null) return "<null>";
        var t = m.mainTexture;
        if (t == null) t = m.GetTexture("_BaseMap");
        if (t == null) return "<null>";
        // 🔴 2026-09-17：把**纹理自己**也打出来 —— 「导出是 <null>」有好几种根因
        //   （不是 Texture2D / 不可读 / Blit 失败 / 立方图被跳过），不打这几个数就只能猜。
        var t2 = t as Texture2D;
        return $"{t.name} {t.width}x{t.height} type={t.GetType().Name}"
             + (t2 != null ? $" readable={t2.isReadable} fmt={t2.format}" : " **不是 Texture2D**");
    }

    static string Q(Quaternion q) { return $"({q.x:G5},{q.y:G5},{q.z:G5},{q.w:G5})"; }

    /// <summary>把一侧的每个可渲染件的关键状态打一行 —— **顺序无关**，用来核对
    /// 「两侧是不是同一批配置」。（按序号配对在同名兄弟节点上不可靠，见 `PathOf` 的注释。）</summary>
    static void DumpRenderers(string side, GameObject root)
    {
        Debug.Log(P + $"  ── [{side}] 可渲染件清单");
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var m = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
            var sr = r as SpriteRenderer;
            var pr = r as ParticleSystemRenderer;
            var sm = r as SpriteMask;
            Debug.Log(P + $"     {side} | {r.GetType().Name,-22} {PathOf(r.transform, root.transform),-42}"
                        + $" enabled={r.enabled,-5}"
                        + $" mat={(m == null ? "<null>" : m.name),-24}"
                        + $" shader={(m == null ? "<null>" : m.shader.name),-40}"
                        + (sr != null ? $" sprite={(sr.sprite == null ? "<null>" : sr.sprite.name)}"
                                        + $" mask={sr.maskInteraction}" + SpriteGeom(sr.sprite) : "")
                        + (sm != null ? $" masksprite={(sm.sprite == null ? "<null>" : sm.sprite.name)}"
                                        + $" cutoff={sm.alphaCutoff:G4}" + SpriteGeom(sm.sprite) : "")
                        + (pr != null ? $" mesh={(pr.mesh == null ? "<null>" : pr.mesh.name)}" : ""));
        }
    }

    /// <summary>精灵的**几何**：它这一格在原图里的矩形 + 原图多大 + 轴心 + 每单位像素。
    /// 🔴 名字一样不代表**内容**一样 —— 导出侧那张是 `ImportSprite` 从**图集里裁一格**重导的 PNG，
    /// 裁错区域的话名字照样对、像素全错。这几个数能把「裁错没裁错」暴露出来。</summary>
    static string SpriteGeom(Sprite s)
    {
        if (s == null) return "";
        var r = s.rect;
        var t = s.texture;
        return $" [rect={r.x:F0},{r.y:F0} {r.width:F0}x{r.height:F0}"
             + $" tex={(t == null ? "?" : t.name + " " + t.width + "x" + t.height)}"
             + $" pivot={s.pivot.x:F1},{s.pivot.y:F1} ppu={s.pixelsPerUnit:F1}"
             + $" bounds={s.bounds.size.x:F2}x{s.bounds.size.y:F2}]";
    }

    static void OneSystem(string path, ParticleSystem a, ParticleSystem b)
    {        // ---------------- Main ----------------
        var ma = a.main; var mb = b.main;
        F(path, "Main.duration", G(ma.duration), G(mb.duration));
        F(path, "Main.loop", ma.loop.ToString(), mb.loop.ToString());
        F(path, "Main.prewarm", ma.prewarm.ToString(), mb.prewarm.ToString());
        F(path, "Main.playOnAwake", ma.playOnAwake.ToString(), mb.playOnAwake.ToString());
        F(path, "Main.maxParticles", ma.maxParticles.ToString(), mb.maxParticles.ToString());
        F(path, "Main.simulationSpace", ma.simulationSpace.ToString(), mb.simulationSpace.ToString());
        F(path, "Main.simulationSpeed", G(ma.simulationSpeed), G(mb.simulationSpeed));
        F(path, "Main.startDelay", S(ma.startDelay), S(mb.startDelay));
        F(path, "Main.startLifetime", S(ma.startLifetime), S(mb.startLifetime));
        F(path, "Main.startSpeed", S(ma.startSpeed), S(mb.startSpeed));
        F(path, "Main.startSize", S(ma.startSize), S(mb.startSize));
        F(path, "Main.startSize3D", ma.startSize3D.ToString(), mb.startSize3D.ToString());
        if (ma.startSize3D || mb.startSize3D)
        {
            F(path, "Main.startSizeX", S(ma.startSizeX), S(mb.startSizeX));
            F(path, "Main.startSizeY", S(ma.startSizeY), S(mb.startSizeY));
            F(path, "Main.startSizeZ", S(ma.startSizeZ), S(mb.startSizeZ));
        }
        F(path, "Main.startRotation3D", ma.startRotation3D.ToString(), mb.startRotation3D.ToString());
        F(path, "Main.startRotation", S(ma.startRotation), S(mb.startRotation));
        F(path, "Main.startColor", S(ma.startColor), S(mb.startColor));
        F(path, "Main.gravityModifier", S(ma.gravityModifier), S(mb.gravityModifier));
        F(path, "Main.flipRotation", G(ma.flipRotation), G(mb.flipRotation));
        F(path, "Main.scalingMode", ma.scalingMode.ToString(), mb.scalingMode.ToString());
        F(path, "Main.emitterVelocityMode", ma.emitterVelocityMode.ToString(), mb.emitterVelocityMode.ToString());

        // ---------------- Emission ----------------
        var ea = a.emission; var eb = b.emission;
        F(path, "Emission.enabled", ea.enabled.ToString(), eb.enabled.ToString());
        F(path, "Emission.rateOverTime", S(ea.rateOverTime), S(eb.rateOverTime));
        F(path, "Emission.rateOverDistance", S(ea.rateOverDistance), S(eb.rateOverDistance));
        F(path, "Emission.burstCount", ea.burstCount.ToString(), eb.burstCount.ToString());
        int nb = Mathf.Min(ea.burstCount, eb.burstCount);
        for (int i = 0; i < nb; i++)
        {
            var ba = ea.GetBurst(i); var bb = eb.GetBurst(i);
            F(path, $"Emission.burst[{i}].time", G(ba.time), G(bb.time));
            F(path, $"Emission.burst[{i}].count", S(ba.count), S(bb.count));
            F(path, $"Emission.burst[{i}].cycleCount", ba.cycleCount.ToString(), bb.cycleCount.ToString());
            F(path, $"Emission.burst[{i}].repeatInterval", G(ba.repeatInterval), G(bb.repeatInterval));
            F(path, $"Emission.burst[{i}].probability", G(ba.probability), G(bb.probability));
        }

        // ---------------- Shape ----------------
        var sa = a.shape; var sb = b.shape;
        F(path, "Shape.enabled", sa.enabled.ToString(), sb.enabled.ToString());
        F(path, "Shape.shapeType", sa.shapeType.ToString(), sb.shapeType.ToString());
        F(path, "Shape.radius", G(sa.radius), G(sb.radius));
        F(path, "Shape.radiusThickness", G(sa.radiusThickness), G(sb.radiusThickness));
        F(path, "Shape.angle", G(sa.angle), G(sb.angle));
        F(path, "Shape.arc", G(sa.arc), G(sb.arc));
        F(path, "Shape.position", V(sa.position), V(sb.position));
        F(path, "Shape.rotation", V(sa.rotation), V(sb.rotation));
        F(path, "Shape.scale", V(sa.scale), V(sb.scale));
        F(path, "Shape.alignToDirection", sa.alignToDirection.ToString(), sb.alignToDirection.ToString());
        F(path, "Shape.randomDirectionAmount", G(sa.randomDirectionAmount), G(sb.randomDirectionAmount));

        // ---------------- ColorOverLifetime ----------------
        var ca = a.colorOverLifetime; var cb = b.colorOverLifetime;
        F(path, "ColorOverLifetime.enabled", ca.enabled.ToString(), cb.enabled.ToString());
        F(path, "ColorOverLifetime.color", S(ca.color), S(cb.color));

        // ---------------- SizeOverLifetime / BySpeed ----------------
        var za = a.sizeOverLifetime; var zb = b.sizeOverLifetime;
        F(path, "SizeOverLifetime.enabled", za.enabled.ToString(), zb.enabled.ToString());
        F(path, "SizeOverLifetime.separateAxes", za.separateAxes.ToString(), zb.separateAxes.ToString());
        F(path, "SizeOverLifetime.size", S(za.size), S(zb.size));
        if (za.separateAxes || zb.separateAxes)
        {
            F(path, "SizeOverLifetime.x", S(za.x), S(zb.x));
            F(path, "SizeOverLifetime.y", S(za.y), S(zb.y));
            F(path, "SizeOverLifetime.z", S(za.z), S(zb.z));
        }
        var sba = a.sizeBySpeed; var sbb = b.sizeBySpeed;
        F(path, "SizeBySpeed.enabled", sba.enabled.ToString(), sbb.enabled.ToString());
        F(path, "SizeBySpeed.size", S(sba.size), S(sbb.size));
        F(path, "SizeBySpeed.range", Rng(sba.range), Rng(sbb.range));

        // ---------------- RotationOverLifetime ----------------
        var ra = a.rotationOverLifetime; var rb = b.rotationOverLifetime;
        F(path, "RotationOverLifetime.enabled", ra.enabled.ToString(), rb.enabled.ToString());
        F(path, "RotationOverLifetime.z", S(ra.z), S(rb.z));

        // ---------------- VelocityOverLifetime ----------------
        var va = a.velocityOverLifetime; var vb = b.velocityOverLifetime;
        F(path, "VelocityOverLifetime.enabled", va.enabled.ToString(), vb.enabled.ToString());
        F(path, "VelocityOverLifetime.space", va.space.ToString(), vb.space.ToString());
        F(path, "VelocityOverLifetime.x", S(va.x), S(vb.x));
        F(path, "VelocityOverLifetime.y", S(va.y), S(vb.y));
        F(path, "VelocityOverLifetime.z", S(va.z), S(vb.z));

        // ---------------- Noise ----------------
        var na = a.noise; var nb2 = b.noise;
        F(path, "Noise.enabled", na.enabled.ToString(), nb2.enabled.ToString());
        F(path, "Noise.strength", S(na.strength), S(nb2.strength));
        F(path, "Noise.frequency", G(na.frequency), G(nb2.frequency));
        F(path, "Noise.scrollSpeed", S(na.scrollSpeed), S(nb2.scrollSpeed));
        F(path, "Noise.damping", na.damping.ToString(), nb2.damping.ToString());
        F(path, "Noise.octaveCount", na.octaveCount.ToString(), nb2.octaveCount.ToString());

        // ---------------- TextureSheetAnimation ----------------
        var ta = a.textureSheetAnimation; var tb = b.textureSheetAnimation;
        F(path, "TSA.enabled", ta.enabled.ToString(), tb.enabled.ToString());
        F(path, "TSA.mode", ta.mode.ToString(), tb.mode.ToString());
        F(path, "TSA.numTilesX", ta.numTilesX.ToString(), tb.numTilesX.ToString());
        F(path, "TSA.numTilesY", ta.numTilesY.ToString(), tb.numTilesY.ToString());
        F(path, "TSA.cycleCount", ta.cycleCount.ToString(), tb.cycleCount.ToString());
        F(path, "TSA.frameOverTime", S(ta.frameOverTime), S(tb.frameOverTime));
        F(path, "TSA.startFrame", S(ta.startFrame), S(tb.startFrame));

        // ---------------- Renderer ----------------
        var rra = a.GetComponent<ParticleSystemRenderer>();
        var rrb = b.GetComponent<ParticleSystemRenderer>();
        if (rra == null || rrb == null)
        {
            F(path, "Renderer.存在", (rra != null).ToString(), (rrb != null).ToString());
        }
        else
        {
            F(path, "Renderer.renderMode", rra.renderMode.ToString(), rrb.renderMode.ToString());
            F(path, "Renderer.alignment", rra.alignment.ToString(), rrb.alignment.ToString());
            F(path, "Renderer.sortMode", rra.sortMode.ToString(), rrb.sortMode.ToString());
            F(path, "Renderer.sortingOrder", rra.sortingOrder.ToString(), rrb.sortingOrder.ToString());
            F(path, "Renderer.minParticleSize", G(rra.minParticleSize), G(rrb.minParticleSize));
            F(path, "Renderer.maxParticleSize", G(rra.maxParticleSize), G(rrb.maxParticleSize));
            F(path, "Renderer.lengthScale", G(rra.lengthScale), G(rrb.lengthScale));
            F(path, "Renderer.velocityScale", G(rra.velocityScale), G(rrb.velocityScale));
            F(path, "Renderer.cameraVelocityScale", G(rra.cameraVelocityScale), G(rrb.cameraVelocityScale));
            F(path, "Renderer.pivot", V(rra.pivot), V(rrb.pivot));
            F(path, "Renderer.trailMaterial", MatName(rra.trailMaterial), MatName(rrb.trailMaterial));
            F(path, "Renderer.material", MatName(rra.sharedMaterial), MatName(rrb.sharedMaterial));
        }
    }

    // ==================================================================
    //  比较与格式化
    // ==================================================================

    static void F(string path, string field, string a, string b)
    {
        if (a == b) { _same++; return; }
        _diff++;
        Debug.Log(P + $"    ★ [{path}] {field,-36}\n"
                    + P + $"        原版 = {a}\n"
                    + P + $"        导出 = {b}");
    }

    static Dictionary<string, ParticleSystem> Map(GameObject root)
    {
        var d = new Dictionary<string, ParticleSystem>();
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            d[PathOf(ps.transform, root.transform)] = ps;
        return d;
    }

    /// <summary>层级路径 —— ⚠️ **必须带同级序号**。
    /// 2026-09-17 踩过：`PinDownEffect` 底下**两个渲染器都叫 `Sprite Mask`**，
    /// 只按名字做键的话字典会**撞车**（后一个覆盖前一个）⇒ 比对的是**两个不同的对象**，
    /// 报出来的「差异」全是假的。带上 `#同级序号` 之后才配得准。</summary>
    static string PathOf(Transform t, Transform root)
    {
        var s = t.name + "#" + t.GetSiblingIndex();
        while (t.parent != null && t.parent != root)
        {
            t = t.parent;
            s = t.name + "#" + t.GetSiblingIndex() + "/" + s;
        }
        return s;
    }

    static string MatName(Material m) { return m == null ? "<null>" : m.name + " [" + m.shader.name + "]"; }

    static string G(float v) { return v.ToString("G6"); }
    static string V(Vector3 v) { return $"({v.x:G5},{v.y:G5},{v.z:G5})"; }
    static string Rng(Vector2 v) { return $"({v.x:G5},{v.y:G5})"; }

    /// <summary>`MinMaxCurve` → 串。**按采样点比**，不看它是 Constant 还是 Curve ——
    /// 形状一样就是一样（原版两种写法都能表达同一个东西）。</summary>
    static string S(ParticleSystem.MinMaxCurve c)
    {
        switch (c.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return "C" + G(c.constant);
            case ParticleSystemCurveMode.TwoConstants:
                return $"C2[{G(c.constantMin)}..{G(c.constantMax)}]";
            case ParticleSystemCurveMode.Curve:
                return "K" + CurveS(c.curve);
            case ParticleSystemCurveMode.TwoCurves:
                return "K2[" + CurveS(c.curveMin) + " | " + CurveS(c.curveMax) + "]";
        }
        return "?";
    }

    static string CurveS(AnimationCurve c)
    {
        if (c == null) return "<null>";
        var sb = new StringBuilder("(");
        for (int i = 0; i < Samples; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(c.Evaluate(i / (float)(Samples - 1)).ToString("G4"));
        }
        return sb.Append(')').ToString();
    }

    /// <summary>`MinMaxGradient` → 串。同样按采样点比。</summary>
    static string S(ParticleSystem.MinMaxGradient g)
    {
        switch (g.mode)
        {
            case ParticleSystemGradientMode.Color: return "C" + Col(g.color);
            case ParticleSystemGradientMode.TwoColors: return $"C2[{Col(g.colorMin)}..{Col(g.colorMax)}]";
            case ParticleSystemGradientMode.Gradient: return "G" + Grad(g.gradient);
            case ParticleSystemGradientMode.TwoGradients:
                return "G2[" + Grad(g.gradientMin) + " | " + Grad(g.gradientMax) + "]";
            case ParticleSystemGradientMode.RandomColor: return "R" + Grad(g.gradient);
        }
        return "?";
    }

    static string Col(Color c)
    {
        // 量化到 3 位 —— 免得浮点末位把「其实一样」报成不同
        return $"({c.r:F3},{c.g:F3},{c.b:F3},{c.a:F3})";
    }

    static string Grad(Gradient g)
    {
        if (g == null) return "<null>";
        var sb = new StringBuilder("(");
        for (int i = 0; i < Samples; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(Col(g.Evaluate(i / (float)(Samples - 1))));
        }
        sb.Append(") keys=").Append(g.colorKeys.Length).Append("c/").Append(g.alphaKeys.Length).Append("a");
        sb.Append(" mode=").Append(g.mode);
        return sb.ToString();
    }
}
