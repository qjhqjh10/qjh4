// AnimClipProbe.cs —— 体检「原版 Animator / 动画片段」这条路通没通（**常驻诊断入口**，定期跑）
//
// 它量两件事，各自对应一个**踩过的坑**（判据正本 = `资料/Animator动画_导入路与判据.md`）：
//
//  ① **落盘落不出真东西** —— 包里的 `AnimationClip` 是 **muscle / streamed 格式**
//     （`m_MuscleClipSize: 2808`、`m_StreamedClip.data` 是压缩曲线块），**编辑器那份曲线数据根本没打进包**。
//     `Object.Instantiate + AssetDatabase.CreateAsset` 落出来的 `.anim` 是**空壳**：
//     `m_FloatCurves` / `m_ClipBindingConstant` 空、`length` = **1**（Unity 空 clip 默认值），
//     而原件是 **0.8167**。⇒ 所以动画**只能在运行时从原版小包取原件**（`WarpforgeAnimatorBridge`）。
//  ② **运行时那条桥有没有接上** —— 实例化工程 prefab → `WarpforgeEffectBinder.Apply()` →
//     挂上的是不是**原件**、`Update(0)` 之后动画有没有**真的驱动**这个 prefab。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod AnimClipProbe.Run -logFile "d:/4/_tmp_view/animclip.log"
// 筛输出：grep "^ACP "
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AnimClipProbe
{
    const string Tag = "ACP ";
    const string Extra = "Assets/StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle";
    const string OutDir = "Assets/WarpforgeVFX/Animations/_probe";
    const string ClipName = "Card Explosion";
    const string PrefabPath = "Assets/WarpforgeVFX/Prefabs/Card 3D Death Explosion.prefab";
    /// <summary>原版真值（`assets_full/…/AnimationClip/AnimationClip_8359881181387397000.json` 的 `m_StopTime`）。</summary>
    const float OrigLen = 0.8166667f;

    static void ReportClip(string label, AnimationClip c)
    {
        if (c == null) { Debug.Log(Tag + $"{label}: **null**"); return; }
        int fb = 0, ob = 0;
        try { fb = AnimationUtility.GetCurveBindings(c).Length; } catch { }
        try { ob = AnimationUtility.GetObjectReferenceCurveBindings(c).Length; } catch { }
        Debug.Log(Tag + $"{label}: `{c.name}` length={c.length:0.####} frameRate={c.frameRate} "
                + $"legacy={c.legacy} | 编辑器曲线 {fb} 条 · 对象引用曲线 {ob} 条"
                + (Mathf.Abs(c.length - OrigLen) < 0.001f ? "  ✅ 是原件" : "  🔴 不是原件（空壳的 length 是 1）"));
    }

    static void ReportController(string label, RuntimeAnimatorController rc)
    {
        if (rc == null) { Debug.LogError(Tag + $"{label}：**控制器是 null**"); return; }
        var clips = rc.animationClips;
        Debug.Log(Tag + $"{label}：控制器 `{rc.name}`（{clips?.Length ?? 0} 个 clip）");
        foreach (var c in clips ?? new AnimationClip[0]) ReportClip(label + "   └ clip", c);
    }

    /// <summary>把一棵子树里所有**可能被动画驱动**的值抄成文本（用来判「播了没有」）。</summary>
    static string Snapshot(GameObject root)
    {
        var sb = new StringBuilder();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            sb.Append(t.name).Append(" act=").Append(t.gameObject.activeSelf)
              .Append(" p=").Append(t.localPosition.ToString("F4"))
              .Append(" s=").Append(t.localScale.ToString("F4")).Append(" | ");
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            sb.Append("R:").Append(r.name).Append(" en=").Append(r.enabled).Append(" | ");
        return sb.ToString();
    }

    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder("Assets/WarpforgeVFX/Animations"))
            AssetDatabase.CreateFolder("Assets/WarpforgeVFX", "Animations");

        // ═══ ② 运行时那条桥：实例化工程 prefab → 挂控制器 → 播一下看有没有真的驱动 ═══
        //  ⚠️ **`Apply()` 必须调【实例上】那个 binder** —— 调资产上那个会把运行时对象写进 prefab 资产
        //     （`m_Controller` 又变回 guid 全 0，判据 → `资料/已知的坑.md`「在 prefab 资产上跑运行时代码」）。
        WarpforgeVFX.WarpforgeAnimatorBridge.Reset();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError(Tag + $"找不到 `{PrefabPath}`"); return; }
        var prefabAnim = prefab.GetComponent<Animator>();
        var prefabBinder = prefab.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        Debug.Log(Tag + $"② 资产上：Animator.m_Controller={(prefabAnim == null || prefabAnim.runtimeAnimatorController == null ? "**空**（坏的那一档）" : prefabAnim.runtimeAnimatorController.name)}"
                      + $" · binder.animatorController=`{(prefabBinder == null ? "-" : prefabBinder.animatorController)}`");

        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.name = prefab.name;
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();
        var anim = inst.GetComponent<Animator>();
        var rc = anim != null ? anim.runtimeAnimatorController : null;
        ReportController("② 实例化后挂上的", rc);
        if (anim != null && rc != null)
        {
            string before = Snapshot(inst);
            anim.Play(0, 0, 0f);
            anim.Update(0f);
            Debug.Log(Tag + $"② 播一下（`Play(0,0,0)+Update(0)`）：快照"
                          + (Snapshot(inst) == before ? "**没变** 🔴 动画没驱动" : "变了 ✅ 动画真的驱动了这个 prefab")
                          + "（原版这条 clip 在 t=0 把子物体 `Minion Death` 关掉、≈0.1 s 又打开）");
        }
        UnityEngine.Object.DestroyImmediate(inst);

        // ═══ ① 落盘落不出真东西（**根因取证**，留着给别人看）═══
        //  ⚠️ 桥刚把包加载过了 ⇒ 这里 `LoadFromFile` 多半返回 null（同一份内容不能加载两次）。
        //     那就**去已加载的包里捡**（与桥的兜底同一条路）。
        AnimationClip src = null;
        var b = AssetBundle.LoadFromFile(Extra);
        if (b == null)
        {
            foreach (var x in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (x == null) continue;
                try { if (x.LoadAsset<AnimationClip>(ClipName) != null) { b = x; break; } }
                catch { }
            }
        }
        if (b != null)
        {
            try { src = b.LoadAsset<AnimationClip>(ClipName); } catch { }
            if (src == null) src = b.LoadAllAssets<AnimationClip>().FirstOrDefault(x => x.name == ClipName);
        }
        if (src == null) { Debug.LogWarning(Tag + "① 取不到那份 clip（包没加载成）⇒ 跳过落盘取证"); }
        else
        {
            ReportClip("① 原件（直接从包里取）", src);
            if (!AssetDatabase.IsValidFolder(OutDir))
                AssetDatabase.CreateFolder("Assets/WarpforgeVFX/Animations", "_probe");
            string p1 = $"{OutDir}/instantiate_copy.anim";
            AssetDatabase.DeleteAsset(p1);
            var c1 = UnityEngine.Object.Instantiate(src);
            c1.name = "instantiate_copy";
            AssetDatabase.CreateAsset(c1, p1);
            AssetDatabase.SaveAssets();
            ReportClip("① 落盘（Instantiate+CreateAsset，**现行做法**）", AssetDatabase.LoadAssetAtPath<AnimationClip>(p1));
            AssetDatabase.DeleteAsset(OutDir);        // 探针不留产物
        }
        Debug.Log(Tag + "结束");
    }
}
