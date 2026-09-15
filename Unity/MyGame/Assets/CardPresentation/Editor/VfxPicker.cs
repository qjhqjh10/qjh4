// VfxPicker.cs — 特效候选「试片台」：把候选特效一个个放在**战斗场景的相机下**播一遍并截图
//
// 为什么需要它：`effect_index.json` 的判定列是 VFX 线用**他们自己那套相机/渲染设置**量出来的，
// 「对得上」不等于「在我们的战斗场景里也好看」—— 实测踩到两个：`Sword_Slash_Simple` 渲成一块
// 不透明的淡蓝大板子、`Attack_Stomp` 渲成一个黑菱形，而 `Antimatter Explosion` / `Tap Firepit` 正常。
// 所以挑特效要**在真实场景里看**，别只看台账。
//
// 用法：-executeMethod VfxPicker.Run  → 图在 d:/4/_tmp_view/vfx/<名字>.png
// 看完把选中的填进 `Core/VfxMap.cs`。
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CardPresentation;
using WarpforgeVFX;

public static class VfxPicker
{
    const string P = "VFX ";
    const string OutDir = @"d:/4/_tmp_view/vfx";

    /// <summary>
    /// 每类事件的候选（都在 `effect_index.json` 里判定「对得上 + 高置信」）。
    ///
    /// ⚠️ **第一项就是 `VfxMap` 现在用的那个** —— 试片台是拿来「换」特效的，
    ///    不把现役的摆在第一个，看半天不知道在跟什么比。
    /// ⚠️ 事件名和 `VfxMap` 的常量**一一对应**，别在这儿另起一套名字。
    /// </summary>
    static readonly (string evt, string[] names)[] Candidates =
    {
        ("play",   new[] { "Tap Blue Glow", "Tap Firepit 2", "Tap Plasma generator" }),
        ("deploy", new[] { "Sororitas Summon Basic", "Tau_SummonCircle", "Tyranid Spawn",
                           "Tap Firepit 2", "GSC Summon" }),
        ("melee",  new[] { "BulletImpactBurst_crowd", "Attack_Stomp", "Axe_Slash_User_SW",
                           "Recon Hit", "Sword_Slash_Simple" }),
        ("ranged", new[] { "BulletImpact_2shot", "BulletImpact_1shot_pointblank",
                           "BulletImpact_3shot" }),
        ("hit",    new[] { "Tap Firepit", "BulletImpact_1shot", "BulletImpactBurst_Blood",
                           // 🆕 2026-09-15：原版在攻击结算处接的是 `AttackHitSmall`，但那一件
                           //    在 `effect_index.json` 里判「**两边全程空（完全脚本驱动）**」⇒ 用不了。
                           //    下面这几个是「对得上 + 高置信 + ratio≈1」里语义最接近**命中**的。
                           //    ⚠️ 现役的 `Tap Firepit` 台账判 **`TAP_SKIP`** = 3D 战场火盆的**点击反馈**，
                           //      语义上和「挨打」没关系（2026-09-15 查台账才发现）。
                           "BulletImpact_1shotSniper", "BulletImpact_3shot" }),
        ("death",  new[] { "Explosion Fenrisian Monstrosities", "Explosion_Possession", "Antimatter Explosion",
                           // 🆕 2026-09-15：原版死亡爆散接的是 `Card 3D Death Explosion`（**不在我们的导出索引里**）。
                           //    ⚠️ 原来现役的 `Explosion_Possession` 台账判 **`ATK_EVENT`** —— 它是
                           //      BL 战术卡 `Rites of Possession` 的**命中**特效，不是通用阵亡。
                           //    实拍（`_tmp_view/pick_death.png`）后换成第一项：**一整个橙色爆炸，五个里最好**。
                           //    ⚠️ `Explosion_Short` 渲成**洋红色方块**、`Necrons death explosion` 渲成**黑方块**（坏 shader）。
                           "Explosion_Short", "Necrons death explosion" }),
        // 2026-09-12 新增：这两类以前压根播不出来（引擎里没有这两种事件），
        // 所以从来没被挑过。补上候选、**试片之后按实拍定的**（见 VfxMap 里的注释）：
        //   ability  Tap Webway Portal 试片里几乎看不见 → Buff_Blue_SW（蓝色光柱）
        //   trigger  Tap Fire Eldar 试片里是硬边绿方块 → Faith_trigger_unit（金色符记）
        ("ability", new[] { "Buff_Blue_SW", "Chronomancer_Buff", "Buff_Sororitas_Intense",
                            "Buff_UM_Intense", "Buff_Eldar_Warp Spider_intense", "Tap Webway Portal" }),
        ("trigger", new[] { "Faith_trigger_unit", "MarkerlightProcEffect", "Stealth_Proc",
                            "BulletImpact_MagicMissile_EC", "Tap Fire Eldar" }),
    };

    /// <summary>⚠️ **必须多个时刻各拍一张** —— 同一个效果不同时刻差别极大：
    /// `Attack_Stomp` 早期是尘环、晚期是一大块黑菱形（shader 坏的那种）。
    /// 只拍一个时刻会得出完全相反的结论（踩过）。</summary>
    static readonly float[] Times = { 0.20f, 0.60f, 1.50f };

    [MenuItem("Tools/CardPresentation/特效候选试片")]
    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        Debug.Log(P + "=== 特效试片开始 ===");
        if (!WarpforgeEffectLibrary.Available)
        {
            Debug.LogError(P + "没有特效库（先跑 Tools > Warpforge > 生成效果库）");
            return;
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        // ⚠️ 背景要用**中灰**（不是近黑）：有一批效果是「不透明的黑方块里画个图案」
        //    （`Axe_Slash_User_SW` / `Attack_Stomp` / `BulletImpactBurst_Blood` 都中招），
        //    背景一黑就跟正常效果分不出来了 —— 用深色背景挑特效会漏掉这类（踩过）。
        cam.backgroundColor = new Color(0.42f, 0.44f, 0.48f);
        LayoutSpace.Apply(cam);
        cam.aspect = LayoutSpace.DesignAspect;

        int made = 0;
        foreach (var (evt, names) in Candidates)
        {
            foreach (var name in names)
            {
                var pos = LayoutSpace.ToWorld(0.5f, 0.55f);
                var fx = WarpforgeEffectPlayer.Play(name, null, pos, 1f, -1f);
                if (fx == null) { Debug.LogWarning(P + $"  ✗ 播不出来：{name}"); continue; }

                float t = 0f;
                for (int k = 0; k < Times.Length; k++)
                {
                    while (t < Times[k]) { Step(1f / 30f); t += 1f / 30f; }
                    Shot(cam, $"{evt}_{name}_{k}", 640, 400);
                    made++;
                }
                Debug.Log(P + $"  ✓ {evt,-7} {name}（{Times.Length} 个时刻）");

                fx.Kill();
                Step(0.05f);
            }
        }
        Debug.Log(P + $"=== 试片 {made} 张 → {OutDir} ===");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    static void Step(float dt)
    {
        foreach (var ps in Object.FindObjectsOfType<ParticleSystem>(true))
            if (ps != null) ps.Simulate(dt, withChildren: false, restart: false, fixedTimeStep: true);
    }

    static void Shot(Camera cam, string tag, int w, int h)
    {
        cam.aspect = (float)w / h;
        var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        File.WriteAllBytes($"{OutDir}/{tag.Replace('/', '_')}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
    }
}
