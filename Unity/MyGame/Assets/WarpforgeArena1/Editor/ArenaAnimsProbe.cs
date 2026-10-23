// ArenaAnimsProbe.cs — 战场线**动画素材小包**的取件探针（2026-10-22）
//
// 为什么有它：战场线要「重新用素材布置」，第一步是确认那批**非重打包不可**的素材
// （5 条 muscle clip + 5 个 AnimatorController）真的能从我们自己的包里按名字取到。
// 这批是 `工具/extract_missing_shaders.py --arena-anims` 打出来的三只包：
//   wf_arena_anims_arena1.bundle / wf_arena_anims_arena2.bundle / wf_arena_anims_shared.bundle
//
// 判据：
//   · 入口路线与运行时一致 —— `AssetBundle.LoadFromFile` + `LoadAsset<T>(名字)`
//     （L-A 报告 §4 给的 pilot 断言；容器里同时登记了「裸名」与 `assets/<小写>.anim|.controller` 两条别名）
//   · 名字 = `数据/游戏数据/animator_controllers.json` 里那 5 个控制器 +
//     `工具/gen_arena_scene_clips.py` 判为 muscle 的 5 条 clip（⛔ 不是 J-A 备忘录里那份旧名单：
//     `Battle Arena 2 Animations` 实读是 muscle，已归到这里）
//   · **取不到就报错**（本仓红线：不许静默失败）
//
// 用法：
//   … -executeMethod ArenaAnimsProbe.Run
// 判绿红看日志里的 `AAP ===` 行。

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ArenaAnimsProbe
{
    const string P = "AAP ";
    const string Dir = "Assets/StreamingAssets/WarpforgeVFX";

    static readonly string[] Clips =
    {
        // ① 5 条 muscle（只能重打包）
        "Cannon front Idle",
        "Battle Arena 2 Animations",
        "Dynamic Skulls",
        "Pump Animation",
        "SporeArmature|LeviathanBigSpore",
        // ② 🆕 2026-10-23：9 条 legacy「场景动画」（取原件，比重建曲线忠实）
        "Battle Arena 3 Animations",
        "Astra Militarum Scene Animations",
        "Black Legion Scene Animation",
        "Dark Angels Scene animations",
        "Emperor's Children Scene Animations",
        "Genestealers Scene Animations",
        "Railgun BIG",
        "Railgun Turret 1",
        "Railgun Turret 2",
    };

    static readonly string[] Controllers =
    {
        "Cannon 1",
        "Pump 1",
        "Dynamic Skulls",
        "Battle Arena 2",
        "Battle Arena Leviathan",
    };

    public static void Run()
    {
        int ok = 0, bad = 0;
        var packs = new List<AssetBundle>();
        foreach (var f in Directory.GetFiles(Dir, "wf_arena_anims*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b == null) { Debug.LogError(P + "✗ 载不进来：" + f); bad++; continue; }
            packs.Add(b);
            Debug.Log(P + "已载入 " + Path.GetFileName(f) + "（" + b.GetAllAssetNames().Length + " 条容器项）");
        }
        if (packs.Count == 0) { Debug.LogError(P + "✗ 一只 wf_arena_anims*.bundle 都没找到 —— 先跑 --arena-anims"); Debug.Log(P + "=== 0 / 0 ==="); return; }

        foreach (var n in Clips)
        {
            AnimationClip c = null; string from = null, via = "裸名";
            foreach (var b in packs) { c = b.LoadAsset<AnimationClip>(n); if (c != null) { from = b.name; break; } }
            if (c == null)
            {
                // ⚠️ **同名冲突档**（2026-10-22 实测）：`Dynamic Skulls` 的**容器键指向同名 AnimatorController**，
                //    那条 clip 只是作为依赖被带进包 ⇒ 裸名 `LoadAsset<AnimationClip>` 取不到。
                //    运行时的真实用法本来就**经 controller 拿 clip**（`controller.animationClips[0]`），
                //    所以这里走同一条路 —— 这不是「缺素材」。
                foreach (var b in packs)
                {
                    foreach (var ctl in b.LoadAllAssets<RuntimeAnimatorController>())
                    {
                        if (ctl == null || ctl.animationClips == null) continue;
                        foreach (var cc in ctl.animationClips)
                            if (cc != null && cc.name == n) { c = cc; from = b.name; via = "controller(" + ctl.name + ")"; break; }
                        if (c != null) break;
                    }
                    if (c != null) break;
                }
            }
            if (c != null) { ok++; Debug.Log(P + "✓ clip `" + n + "`（length=" + c.length.ToString("F4") + " · 来自 " + from + " · via " + via + "）"); }
            else { bad++; Debug.LogError(P + "✗ clip `" + n + "` **取不到**（裸名与 controller 两条路都没有）"); }
        }

        foreach (var n in Controllers)
        {
            RuntimeAnimatorController c = null; string from = null;
            foreach (var b in packs) { c = b.LoadAsset<RuntimeAnimatorController>(n); if (c != null) { from = b.name; break; } }
            if (c != null) { ok++; Debug.Log(P + "✓ controller `" + n + "`（" + (c.animationClips != null ? c.animationClips.Length : 0) + " 条 clip · 来自 " + from + "）"); }
            else { bad++; Debug.LogError(P + "✗ controller `" + n + "` **取不到**"); }
        }

        // ---- ③ 🆕 2026-10-23 端到端：把 `arena_scene_anims.json` 接到真 prefab 上 ----
        //   验收 = 每场「接上的 Animation/Animator 数 == 表里条数」且 `LastMissed == 0`。
        //   ⚠️ 这一步**故意在包还载着的时候跑** —— 运行期 `WarpforgeAnimatorBridge` 先自己
        //      `LoadFromFile`（会因「同一份内容已加载」返回 null）再走「已加载的包」兜底，
        //      正好把那条兜底路也验了。
        var doc = Resources.Load<TextAsset>("arena_scene_anims");
        if (doc == null)
        {
            bad++;
            Debug.LogError(P + "✗ `Resources/arena_scene_anims.json` 取不到 —— 先跑 `工具/gen_arena_scene_anims.py`");
        }
        else
        {
            var d = JsonUtility.FromJson<CardPresentation.ArenaAnimsDoc>(doc.text);
            int arenas = 0, bindOk = 0, bindBad = 0;
            foreach (var a in d.arenas)
            {
                int wantA = a.animations != null ? a.animations.Length : 0;
                int wantN = a.animators != null ? a.animators.Length : 0;
                if (wantA == 0 && wantN == 0) continue;
                arenas++;
                var prefab = Resources.Load<GameObject>("ArenaPrefabs/" + a.key);
                if (prefab == null)
                {
                    bindBad++;
                    Debug.LogError(P + "✗ 场景绑定 `" + a.key + "`：`Resources/ArenaPrefabs/" + a.key + ".prefab` 取不到");
                    continue;
                }
                var go = Object.Instantiate(prefab);
                CardPresentation.ArenaSceneAnims.Bind(go.transform, a.key);
                bool pass = CardPresentation.ArenaSceneAnims.LastMissed == 0
                         && CardPresentation.ArenaSceneAnims.LastAnimated == wantA
                         && CardPresentation.ArenaSceneAnims.LastAnimators == wantN;
                if (pass) bindOk++; else bindBad++;
                Debug.Log(P + (pass ? "✓" : "✗") + " 场景绑定 `" + a.key + "`：Animation "
                        + CardPresentation.ArenaSceneAnims.LastAnimated + "/" + wantA + " · Animator "
                        + CardPresentation.ArenaSceneAnims.LastAnimators + "/" + wantN + " · 没接上 "
                        + CardPresentation.ArenaSceneAnims.LastMissed
                        + (CardPresentation.ArenaSceneAnims.LastMissed > 0
                           ? "（" + string.Join(" / ", CardPresentation.ArenaSceneAnims.LastMissedWhat.ToArray()) + "）"
                           : ""));
                Object.DestroyImmediate(go);
            }
            Debug.Log(P + "=== 场景绑定：" + bindOk + " 过 / " + bindBad + " 红（" + arenas + " 场）===");
        }

        Debug.Log(P + "=== " + ok + " 取到 / " + bad + " 没取到 ===");
        foreach (var b in packs) b.Unload(false);
    }
}
