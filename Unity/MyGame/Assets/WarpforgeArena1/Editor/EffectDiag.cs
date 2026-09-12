// EffectDiag.cs — 诊断：把某个效果的原版 prefab 与导出 prefab 逐节点、逐渲染器、逐材质属性 dump 出来
//
// 用法（效果名在 Args 里改）：
//   Unity.exe -batchmode -quit -projectPath ... -executeMethod EffectDiag.Run -logFile -
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class EffectDiag
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";

    // 要诊断的效果名
    static readonly string[] Args = { "ArtificeEffect", "Explosion_Ground", "Spore Explosion" };

    // 为 true 时连材质属性也全量列出（很长）；false 只列关键字
    const bool DumpProps = false;
    // 只列贴图属性（排查品红/丢贴图时打开）
    const bool OnlyTextures = false;
    // 只列这个 shader 的材质（空 = 全列）
    static readonly string OnlyShader = "";

    public static void Run()
    {
        foreach (var name in Args) Diag(name);
    }

    static void Diag(string effectName)
    {
        Debug.Log($"########## 诊断 {effectName} ##########");

        AssetBundle vfx = null;
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b != null && Path.GetFileName(f) == VfxBundleName) vfx = b;
        }
        if (vfx == null) { Debug.LogError("特效 bundle 未加载"); return; }

        GameObject orig = null;
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g != null && g.name == effectName) { orig = g; break; }
        }
        if (orig == null) { Debug.LogError($"原版里找不到 {effectName}"); return; }

        Debug.Log("=========== 原版 ===========");
        DumpTree(orig);

        var expPath = $"{PrefabDir}/{effectName}.prefab";
        var exp = AssetDatabase.LoadAssetAtPath<GameObject>(expPath);
        if (exp == null) { Debug.LogError($"导出 prefab 不存在: {expPath}"); return; }
        Debug.Log("=========== 导出 ===========");
        DumpTree(exp);
    }

    static void DumpTree(GameObject root)
    {
        Debug.Log($"根「{root.name}」 active={root.activeSelf}");
        var all = root.GetComponentsInChildren<Transform>(true);
        Debug.Log($"  Transform 总数 {all.Length}（含 inactive）");
        foreach (var t in all)
        {
            var go = t.gameObject;
            string path = PathOf(t, root.transform);
            var comps = go.GetComponents<Component>();
            int missing = comps.Count(c => c == null);
            var types = string.Join(",", comps.Where(c => c != null).Select(c => c.GetType().Name));
            Debug.Log($"  [{path}] active={go.activeSelf} 组件={types}{(missing > 0 ? $" 缺失脚本x{missing}" : "")}");

            foreach (var r in go.GetComponents<Renderer>())
                DumpRenderer(r, "    ");

            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                Debug.Log($"    PS main: startLifetime={main.startLifetime.mode}/{main.startLifetime.constant} " +
                          $"startSize={main.startSize.mode}/{main.startSize.constant} " +
                          $"startColor={main.startColor.mode}/{main.startColor.color} " +
                          $"maxParticles={main.maxParticles} simSpace={main.simulationSpace} " +
                          $"playOnAwake={main.playOnAwake} startDelay={main.startDelay.mode}/{main.startDelay.constant}");
                Debug.Log($"    PS emission: rateOverTime={ps.emission.rateOverTime.mode}/{ps.emission.rateOverTime.constant} " +
                          $"bursts={ps.emission.burstCount} enabled={ps.emission.enabled}");
                Debug.Log($"    PS 其余模块: colorOverLifetime={ps.colorOverLifetime.enabled} sizeOverLifetime={ps.sizeOverLifetime.enabled} " +
                          $"velocityOverLifetime={ps.velocityOverLifetime.enabled} noise={ps.noise.enabled} " +
                          $"trails={ps.trails.enabled} shape={ps.shape.enabled}");
            }
        }
    }

    static void DumpRenderer(Renderer r, string ind)
    {
        Debug.Log($"{ind}<{r.GetType().Name}> enabled={r.enabled} " +
                  $"sortingLayer={r.sortingLayerName}/{r.sortingOrder} " +
                  $"bounds(c={r.bounds.center} s={r.bounds.size}) " +
                  $"mats={r.sharedMaterials.Length}");
        var mats = r.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) { Debug.Log($"{ind}  [{i}] <null>"); continue; }
            DumpMaterial(mats[i], $"{ind}  [{i}]");
        }

        var spr = r as SpriteRenderer;
        if (spr != null)
            Debug.Log($"{ind}  Sprite: {(spr.sprite == null ? "<null>" : $"「{spr.sprite.name}」 rect={spr.sprite.rect} ppu={spr.sprite.pixelsPerUnit} tex={(spr.sprite.texture == null ? "null" : spr.sprite.texture.name)}")} drawMode={spr.drawMode} size={spr.size} color={spr.color} flipX={spr.flipX}");

        var psr = r as ParticleSystemRenderer;
        if (psr != null)
        {
            Debug.Log($"{ind}  PSR: renderMode={psr.renderMode} mesh={(psr.mesh == null ? "null" : psr.mesh.name)} " +
                      $"trailMat={(psr.trailMaterial == null ? "null" : psr.trailMaterial.shader.name)} " +
                      $"alignment={psr.alignment} lengthScale={psr.lengthScale}");
            if (psr.trailMaterial != null) DumpMaterial(psr.trailMaterial, $"{ind}  trail");
        }
    }

    static void DumpMaterial(Material m, string ind)
    {
        var sh = m.shader;
        // 关键字分两处：shaderKeywords 是材质上的局部关键字；IsKeywordEnabled 还会看全局关键字
        // （URP 粒子的 _EMISSION 就是全局的，光看 shaderKeywords 会以为它没开）
        var local = new HashSet<string>(m.shaderKeywords ?? new string[0]);
        var enabled = new List<string>();
        if (sh != null)
            foreach (var n in sh.keywordSpace.keywordNames)
                try { if (m.IsKeywordEnabled(n)) enabled.Add(n); } catch { }

        Debug.Log($"{ind} 材质「{m.name}」 shader=「{(sh == null ? "<null>" : sh.name)}」 queue={m.renderQueue} " +
                  $"props={(sh == null ? 0 : sh.GetPropertyCount())}");
        Debug.Log($"{ind}   局部关键字=[{string.Join(",", local)}]");
        Debug.Log($"{ind}   生效关键字=[{string.Join(",", enabled)}]");

        if (!DumpProps || sh == null) return;
        if (!string.IsNullOrEmpty(OnlyShader) && sh.name != OnlyShader) return;
        int n2 = sh.GetPropertyCount();
        for (int p = 0; p < n2; p++)
        {
            var pn = sh.GetPropertyName(p);
            string val;
            try
            {
                if (OnlyTextures && sh.GetPropertyType(p) != ShaderPropertyType.Texture) continue;
                switch (sh.GetPropertyType(p))
                {
                    case ShaderPropertyType.Color:   val = m.GetColor(pn).ToString("F4"); break;
                    case ShaderPropertyType.Vector:  val = m.GetVector(pn).ToString("F4"); break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:   val = m.GetFloat(pn).ToString("F4"); break;
                    case ShaderPropertyType.Int:     val = m.GetInt(pn).ToString(); break;
                    case ShaderPropertyType.Texture:
                        var t = m.GetTexture(pn);
                        val = t == null ? "<null>" : $"「{t.name}」 {t.width}x{t.height}";
                        break;
                    default: val = "?"; break;
                }
            }
            catch (Exception e) { val = $"<异常 {e.GetType().Name}>"; }
            Debug.Log($"{ind}   {pn} ({sh.GetPropertyType(p)}) = {val}");
        }
    }

    static string PathOf(Transform t, Transform root)
    {
        var s = new List<string>();
        while (t != null && t != root) { s.Add(t.name); t = t.parent; }
        s.Reverse();
        return s.Count == 0 ? "(根)" : string.Join("/", s);
    }
}
