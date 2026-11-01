// B114_W14_Assert.cs — 第一百一十四会话 · 写手 `W-14`（2026-11-01）· 战场线两条真缺陷的断言
//
// ============================ 它盯的是什么 ============================
//  两条都是「全仓账 0 命中」的战场差异（`B114_R3` 查出的新问题），用户已授权修：
//
//   A · **`maxParticleSize` 被静默按 Unity 默认 0.5 跑**
//       原版 13 场 578 颗里有 **99 颗** `maxParticleSize > 0.5`（被上限夹着）。
//       `ArenaBuilder.cs` 原来**故意不设**这个字段（2026-09-21 用**一个全局值**在 arena3 做的 A/B）。
//       现改成**按清单逐颗设值**（清单里 578/578 颗都有真值）。
//       🔴 **样点只用【非恒等】的**：`TinyFlames` 5.0 · `SmokeEffect` 10.0 · `WildFire` 1.0。
//          （`Embers` = 0.5 = Unity 默认 ⇒ 「接没接上」在这个名字上**看不出来**，⛔ 别拿它当判据。）
//
//   B · **6 场整块缺 `ColorLookup`**
//       原版 **13/13 场**都有这个槽（`active=1` · `contribution=1.0` · `texture.overrideState=1`）；
//       其中 7 场贴包内自带的 `LUT <场>`、6 场贴 `battlesharedresources` 里那份**共享恒等 `LUT Normal`**。
//       我们原来「本地找不到专属 LUT ⇒ 整块省掉」⇒ 构建出的 profile 只有 7 份带。
//       现改成**回退到共享恒等 LUT**（`ArenaBuilder.SharedIdentityLutPath`）。
//       🔴 **恒等 LUT 不是 no-op**：URP 的 `ColorLookup` 走 16³ 查找 + Bilinear ⇒
//          过一遍就有 8-bit 量化 / 端点钳制行为，「省掉」与「原版过一遍」不是同一张图。
//
// ============================ 期望值从哪来（⛔ 不从被断的实现里读）============================
//   A 段的期望值 = **原版资源逐颗亲读**（`d:/2/解包整理/07_场景/battlearena1/`）：
//       `GameObject/TinyFlames.json` 的第 3 个 component → `ParticleSystemRenderer/ParticleSystemRenderer_1703.json`
//         的 `m_MaxParticleSize: 5.0` / `m_MinParticleSize: 0.0`
//       `GameObject/SmokeEffect.json` → `ParticleSystemRenderer_1697.json` 的 `m_MaxParticleSize: 10.0`
//       `GameObject/WildFire.json`   → `ParticleSystemRenderer_1716.json` 的 `m_MaxParticleSize: 1.0`
//       （`battlearena2` 的 `TinyFlames` 同为 5.0 —— `battlearena2_manifest.json` 现读）
//   B 段的期望值 = **原版 `ColorLookup` 的 `texture.m_Value` 逐场亲读**：
//       7 场 `m_FileID = 0`（包内自带 `LUT <profile 名去掉 " PostProcessing">`）；
//       6 场 `m_FileID = 9`（`arena2` 是 `10`，externals 下标）+ `PathID 382974660631151556`
//       = `battlesharedresources` 的 `LUT Normal`（本地那份逐字节相同，
//       md5 `41c14e8e7a1648098d9705fc68a026cf` · 256×16 RGB）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：把 `rend.maxParticleSize = p.maxParticleSize;` 删掉 / 注释掉 ⇒ prefab 又是 0.5 ⇒ 红。
//   · A 段**三道不同值的样点**（5.0 / 10.0 / 1.0）**结构上不可能同时满足于任何「一刀切」**：
//       有人改成「全部设成 5」⇒ SmokeEffect(10.0) 与 WildFire(1.0) 红；
//       改成「全部设成 10」⇒ TinyFlames 与 WildFire 红。⇒ 它必须真的是**逐颗**。
//   · A 段读的是**产物**（`Resources/ArenaPrefabs/<场>.prefab`），⛔ 不是读 `ArenaBuilder` 的常量、
//     ⛔ 不是读清单 —— 所以「代码改了但没重建 prefab」照样红（那正是本段要抓的）。
//   · B 段：把那 6 场的回退分支删掉 ⇒ profile 里没有 `ColorLookup` ⇒ 红。
//     期望的**贴图名**来自清单的 `postFx.profile`（原版数据）+ 那 6 场的「共享恒等」判据 ⇒
//     有人把 6 场也塞一张专属 LUT、或把 7 场也改成 Normal ⇒ 名字那一条红。
//
// ============================ 怎么接（⚠️ 只由调度台/本波写手改宿主）============================
//  唯一入口 = `B114_W14_Assert.Run()`；宿主 `Editor/BattleScene.cs` 那条聚合行
//  **同一物理行、`Debug.Log(P + "=== 合计…")` 之前**追加：
//      B114_W14_Assert.Run(); pass += B114_W14_Assert.Pass; fail += B114_W14_Assert.Fail;
//
// ============================ 本件会红到什么时候 ============================
//  它断的是**产物**，而产物要跑 Unity 重建：
//    A → `WF_ARENA=<场> -executeMethod ArenaBuilder.BuildFromCLI` + `ArenaBuilder.BuildPrefabFromCLI`（逐场）
//    B → 同上（`BuildFromCLI` 里就调 `ApplyPostFx`；只改 profile 时也要**重建 prefab**，
//         因为 prefab 里嵌的是那份 profile 的引用/内容）
//  ⇒ **重建之前这里必然红**（arena1/2 的 maxParticleSize 6 条 + 6 场的 ColorLookup 若干条），
//    **不是本件写错**。重建之后应全绿。
//
// ============================ 边界（如实记）============================
//  · A 段只覆盖**有非恒等真值**的族（TinyFlames / SmokeEffect / WildFire）。其余 570 颗里绝大多数
//    本来就是 0.5（恒等），断言它们等于 0.5 抓不到任何东西；⛔ 不拿它们凑条数。
//  · A 段**不断**「Unity 的 `maxParticleSize` 到底是夹取还是剔除」—— 那是渲染语义，要看重渲图。
//  · B 段只断**盘上的 profile 资产**，不断运行时补槽那条路（`Battle/BattlePostFx.cs` 的第 ② 条：
//    本改动落地后它应当**不再被触发**，`MissingColorLookup` 恒 0 —— 那条由 `BattleScene` 原有断言覆盖）。
//  · 本文件**只读**（`AssetDatabase.LoadAssetAtPath`），⛔ 不写盘、不建对象。
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CardPresentation;

/// <summary>写手 `W-14`：战场线两条真缺陷 —— `maxParticleSize` 逐颗真值 · 13/13 场 `ColorLookup`。</summary>
public static class B114_W14_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W14] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    /// <summary>唯一入口。</summary>
    public static void Run()
    {
        RunMaxParticleSize();
        RunColorLookup();
    }

    // ==================================================================
    //  A · `maxParticleSize` 逐颗真值
    // ==================================================================

    /// <summary>有**非恒等** `maxParticleSize` 的族（期望值 = 原版资源亲读，见文件头）。
    /// ⛔ 别加 `Embers`：它是 0.5 = Unity 默认值 ⇒ 断了也看不出来「接没接上」。</summary>
    struct MaxPsCase
    {
        public string Scene, Go;
        public float Want;
        public string Src;
        public MaxPsCase(string scene, string go, float want, string src)
        { Scene = scene; Go = go; Want = want; Src = src; }
    }

    static readonly MaxPsCase[] MaxPsCases = {
        new MaxPsCase("battlearena1", "TinyFlames",  5f,
                      "d:/2/解包整理/07_场景/battlearena1/ParticleSystemRenderer/ParticleSystemRenderer_1703.json"),
        new MaxPsCase("battlearena1", "SmokeEffect", 10f,
                      "d:/2/解包整理/07_场景/battlearena1/ParticleSystemRenderer/ParticleSystemRenderer_1697.json"),
        new MaxPsCase("battlearena1", "WildFire",    1f,
                      "d:/2/解包整理/07_场景/battlearena1/ParticleSystemRenderer/ParticleSystemRenderer_1716.json"),
        new MaxPsCase("battlearena2", "TinyFlames",  5f,
                      "battlearena2_manifest.json particles[] 现读（原版同源）"),
    };

    static void RunMaxParticleSize()
    {
        MenuCheck.Section(_sink, "A · `maxParticleSize` 按清单逐颗设值（原版真值 → 建出的 prefab）");

        // prefab 每场只 Load 一次（4 条用例只跨 2 场）
        var cache = new Dictionary<string, GameObject>();
        foreach (var c in MaxPsCases)
        {
            GameObject prefab;
            if (!cache.TryGetValue(c.Scene, out prefab))
            {
                string path = ArenaBuilder.ArenaPrefabPath(c.Scene);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                cache[c.Scene] = prefab;
                MenuCheck.True(_sink, prefab != null,
                    $"`{path}` 取得到（取不到 = 这场的 prefab 从没建过 / 被挪走了）");
            }
            if (prefab == null) continue;

            var hits = new List<ParticleSystemRenderer>();
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != c.Go) continue;
                var r = t.GetComponent<ParticleSystemRenderer>();
                if (r != null) hits.Add(r);
            }
            MenuCheck.True(_sink, hits.Count > 0,
                $"{c.Scene}：找得到 `{c.Go}` 的 `ParticleSystemRenderer`（{hits.Count} 个）");
            if (hits.Count == 0) continue;

            // 同名多个实例时**逐个都要对**（原版这几个名字的值是一致的；有一个不对就是接漏了）
            float worst = c.Want;
            foreach (var r in hits) if (Mathf.Abs(r.maxParticleSize - c.Want) > Mathf.Abs(worst - c.Want)) worst = r.maxParticleSize;
            MenuCheck.Near(_sink, worst, c.Want, 0.0005f,
                $"{c.Scene}/`{c.Go}`：`maxParticleSize` = 原版 {c.Want}（= 0.5 就是「还在吃 Unity 默认值」，"
                + $"判据 {c.Src}）");
        }
    }

    // ==================================================================
    //  B · 13/13 场 `ColorLookup`
    // ==================================================================

    /// <summary>原版**各带一张专属 LUT** 的那 7 场（其余 6 场指向共享的恒等 `LUT Normal`）。
    /// 🔴 判据 = **逐场亲读原版** `ColorLookup.texture.m_Value` 的 `m_FileID`：
    ///   `= 0`（包内自带）的就是这 7 场；`= 9`（`arena2` 是 `10`）+ `PathID 382974660631151556` 的是另外 6 场。
    /// ⚠️ 与 `Editor/BattleScene.cs` 的 `ArenasWithOwnLut` **同源**（那里是 A243 的期望表）。
    /// 两份都留着是因为一份服务旧断言、一份服务新断言；**改原版判据时两处一起改**。</summary>
    static readonly string[] OwnLutArenas = {
        "battlearenaaeldari", "battlearenaemperorschildren", "battlearenagenestealers",
        "battlearenaleviathan", "battlearenasororitas", "battlearenaspacewolves",
        "battlearenatauviorla",
    };

    static void RunColorLookup()
    {
        MenuCheck.Section(_sink, "B · 13/13 场 PostFx profile 都有 `ColorLookup`（原版 13/13 场都有这个槽）");

        int withCl = 0;
        foreach (var arena in ArenaBuilder.AllArenas)
        {
            string path = $"{ArenaBuilder.ProfileDir(arena)}/{arena}_PostFx.asset";
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            MenuCheck.True(_sink, prof != null, $"{arena}：PostFx profile 存在（`{path}`）");
            if (prof == null) continue;

            ColorLookup cl = null;                       // ⚠️ 先赋 null：`TryGet` 在短路里 ⇒ 编译器判不了
            bool has = prof.TryGet(out cl) && cl != null && cl.active;
            if (has) withCl++;
            MenuCheck.True(_sink, has,
                $"{arena}：profile 里有 `ColorLookup` 且 `active`（原版 13/13 场都有；"
                + "缺了 = 那场少一趟 16³ LUT 采样）");
            if (!has) continue;

            // 打了 override 才进得了 Volume 栈（本机 `VolumeComponent.Override` 只搬 overrideState==true 的参数）
            MenuCheck.True(_sink, cl.texture.overrideState && cl.contribution.overrideState,
                $"{arena}：`texture` / `contribution` 都打了 override（没打 = 这一路根本没进栈，且看不出来）");

            bool own = System.Array.IndexOf(OwnLutArenas, arena) >= 0;
            string wantName = own ? OwnLutName(arena) : "LUT Normal";
            string gotName = cl.texture.value != null ? cl.texture.value.name : "(null)";
            MenuCheck.Check(_sink, gotName, wantName,
                $"{arena}：贴的是{(own ? "包内自带的 `LUT <场>`" : "**共享恒等** `LUT Normal`（那 6 场原版就是这样）")}");

            // 原版 13/13 场 `contribution.m_Value = 1.0`（清单里也是 1.0）
            MenuCheck.Near(_sink, cl.contribution.value, 1f, 0.0005f,
                $"{arena}：`contribution` = 原版 1.0");
        }

        MenuCheck.Check(_sink, withCl, ArenaBuilder.AllArenas.Length,
            $"13/13 场的 profile 都带 `ColorLookup`（现读 {withCl}/{ArenaBuilder.AllArenas.Length}）");
    }

    /// <summary>该场专属 LUT 的名字 —— 原版命名规则：
    /// `&lt;profile 名&gt; − " PostProcessing"` 前面加 `LUT `（7 个场景包的 `Texture2D/` 里各只有这一张）。
    /// 名字**从清单的 `postFx.profile` 取**（= 原版数据），⛔ 不从 `ArenaBuilder` 的拼串里抄。</summary>
    static string OwnLutName(string arena)
    {
        string baseName = "";
        var mf = ArenaBuilder.LoadManifest(arena);
        if (mf != null && mf.postFx != null && mf.postFx.profile != null)
            baseName = mf.postFx.profile.Replace(" PostProcessing", "").Trim();
        return "LUT " + baseName;
    }
}
