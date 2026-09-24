// MinMaxGradientModeProbe.cs — 判定 `ParticleSystemGradientMode.Color`（序列化里 `minMaxState=0`）下，
//   Unity 到底读 `minColor` 还是 `maxColor`。
//
// 为什么需要它（2026-09-24；`项目任务.md` §三 第 3 条 第 1 项「arena1 (920,280) 那团蒸汽的颜色」）：
//   · 原版 battlearena1 的 `Steam` 三兄弟（`ParticleSystem_1638/1639/1640.json`）里
//     `InitialModule.startColor.minMaxState = 0`，而 **两个槽都是有意义的灰**：
//       `minColor = (0.7075472, …, a 0.7568628)`  ← 亮
//       `maxColor = (0.4245283, …, a 0.4980392)`  ← 暗
//     且 `maxGradient` 仍是 Unity 出厂默认（白→白→透明×4）⇒ 作者没走 Gradient 模式。
//   · 我们的清单生成器 `gen_unity_arena_manifest.py  gradient_color()` **只取 `maxColor`**
//     （全文件 `minColor` 出现 0 次），落到清单里就是 `0.424528` —— 与原版文件逐字吻合。
//   · 症状（`资料/战场13场_逐场对账_0920.md` §一 ①-c）：同一处、形状位置一致，但
//     **原版是一团白色蒸汽、我们是深灰色的一坨** ⇒ **若引擎读的是 min 槽，我们就是错的。**
//
// ⚠️ **不能拿 Unity 自己的 getter 当判据**：`MinMaxGradient.color` 无论 mode 是什么都返回
//   `maxColor`（`ParticleModuleProbe.cs:639` 的 `case Color: return "C" + Col(g.color)` 走的就是它）
//   ⇒ 拿它验这件事等于自证。
// ⇒ **唯一一手判据 = 真正发出来的粒子**：`ParticleSystem.Particle.startColor`。
//
// 判据：三档，每档的输入色故意选「一眼可分」的红 / 蓝。
//   ① `mode = Color` + `colorMin=红` `colorMax=蓝`  → **发出来的粒子是什么色，答案就是什么**
//   ② `mode = TwoColors`（对照：确定两边都会用）
//   ③ `colorOverLifetime.color` 同做一遍（同一个 MinMaxGradient 结构，确认结论可外推到颜色曲线）
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod MinMaxGradientModeProbe.Run -logFile "d:/4/_tmp_view/mmg.log"
//   筛输出：grep "^MMG " d:/4/_tmp_view/mmg.log
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MinMaxGradientModeProbe
{
    const string P = "MMG ";

    static readonly Color Red  = new Color(1f, 0f, 0f, 1f);
    static readonly Color Blue = new Color(0f, 0f, 1f, 1f);

    public static void Run()
    {
        Debug.Log(P + "=== MinMaxGradient 取值槽判定 开始 ===");
        Debug.Log(P + "  判据 = ParticleSystem.Particle.startColor（真正发出来的粒子），"
                    + "**不是** MinMaxGradient.color（那个 getter 恒返回 max，验了等于自证）");

        Case("① mode=Color  · colorMin=红 colorMax=蓝", ParticleSystemGradientMode.Color, false);
        Case("② mode=TwoColors · colorMin=红 colorMax=蓝（对照）", ParticleSystemGradientMode.TwoColors, false);
        Case("③ colorOverLifetime · mode=Color · colorMin=红 colorMax=蓝", ParticleSystemGradientMode.Color, true);

        OriginalPrefab();

        Debug.Log(P + "=== 结束 ===");
    }

    // ==================================================================
    //  ④ 拿**原版 prefab** 实发粒子（比合成用例硬：数据是原版自己的）
    // ==================================================================

    const string BundleDir = @"D:\2\unity_run_ref\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";

    /// <summary>原版包里带 `Steam` 的 prefab 名（2026-09-24 用 UnityPy 实测这一批）。
    /// ⚠️ 原版包里**没有**裸名 `Steam` / `Steam (1)` —— battlearena1 场景里那三兄弟是**场景内组件**
    /// （`ParticleSystem_1638/1639/1640.json`），不在这个 prefab 包里。这里挑的是同族的 prefab。</summary>
    static readonly string[] OrigNames = { "Steam (2)", "Steam (3)", "Steam (4)", "Steam (5)",
                                           "PressurisedSteam", "PressurisedSteam (1)" };

    static void OriginalPrefab()
    {
        Debug.Log(P + "=== ④ 原版 prefab 实发粒子（判据同上：Particle.Particle.startColor）===");
        var path = System.IO.Path.Combine(BundleDir, VfxBundleName);
        if (!System.IO.File.Exists(path)) { Debug.LogWarning(P + "  原版包不在：" + path); return; }

        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) { Debug.LogWarning(P + "  原版包加载失败"); return; }
        try
        {
            foreach (var name in OrigNames)
            {
                GameObject prefab = null;
                try { prefab = bundle.LoadAsset<GameObject>(name); } catch { }
                if (prefab == null) { Debug.LogWarning(P + $"  原版没有 {name}"); continue; }

                var go = Object.Instantiate(prefab);
                try
                {
                    var systems = go.GetComponentsInChildren<ParticleSystem>(true);
                    Debug.Log(P + $"--- 原版 {name}（{systems.Length} 个 ParticleSystem）---");
                    foreach (var ps in systems)
                    {
                        ps.gameObject.SetActive(true);
                        var g = ps.main.startColor;
                        string where = PathOf(ps.transform);
                        Debug.Log(P + $"    [{where}] mode={g.mode} "
                                    + $"colorMin={Col(g.colorMin)} colorMax={Col(g.colorMax)} "
                                    + $"· getter.color={Col(g.color)}");

                        ps.Stop();
                        ps.Play();
                        ps.Emit(8);
                        var buf = new ParticleSystem.Particle[64];
                        int n = ps.GetParticles(buf);
                        if (n == 0) { ps.Simulate(1.2f, false, true); n = ps.GetParticles(buf); }
                        if (n == 0) { Debug.Log(P + $"        实发 0 颗 ⇒ 这一颗**无效**，不当结论"); continue; }

                        Debug.Log(P + $"        实发 {n} 颗，前 5 颗 startColor = "
                                    + string.Join(" ", FirstN(buf, n, 5)));

                        // 与两个槽比对（容差 0.01）
                        var c0 = buf[0].startColor;
                        Debug.Log(P + $"        ⇒ 第 1 颗 vs colorMin：{Near(c0, g.colorMin)} · "
                                    + $"vs colorMax：{Near(c0, g.colorMax)}"
                                    + (g.mode != ParticleSystemGradientMode.Color
                                        ? "（非 Color 档，两槽本来就该都用）" : ""));
                    }
                }
                finally { Object.DestroyImmediate(go); }
            }
        }
        finally { bundle.Unload(true); }
    }

    static string[] FirstN(ParticleSystem.Particle[] buf, int n, int k)
    {
        var outv = new string[Mathf.Min(n, k)];
        for (int i = 0; i < outv.Length; i++) outv[i] = Col(buf[i].startColor);
        return outv;
    }

    static string Near(Color a, Color b)
        => Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f
        && Mathf.Abs(a.b - b.b) < 0.01f && Mathf.Abs(a.a - b.a) < 0.01f ? "**相等 ✓**" : "不等";

    static string PathOf(Transform t)
    {
        var s = t.name;
        for (var p = t.parent; p != null; p = p.parent) s = p.name + "/" + s;
        return s;
    }

    /// <summary>造一颗最小粒子系统，把 startColor（或 colorOverLifetime.color）按给定 mode 设成红/蓝，模拟后读粒子。</summary>
    /// <param name="overLifetime">true = 改 `colorOverLifetime.color`；false = 改 `main.startColor`</param>
    static void Case(string label, ParticleSystemGradientMode mode, bool overLifetime)
    {
        var go = new GameObject("MMG_Tmp");
        try
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.playOnAwake   = false;
            main.loop          = true;
            main.duration      = 5f;
            main.prewarm       = false;
            main.startLifetime = 5f;          // 粒子活得久，才读得到
            main.startSpeed    = 0f;
            main.startSize     = 1f;
            main.maxParticles  = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            // 先给一个中性白，免得别的档干扰
            main.startColor = Color.white;

            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = 10f;

            var g = new ParticleSystem.MinMaxGradient();
            g.mode     = mode;
            g.colorMin = Red;
            g.colorMax = Blue;

            string slot = overLifetime ? "colorOverLifetime.color" : "main.startColor";
            if (overLifetime)
            {
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = g;
            }
            else
            {
                main.startColor = g;
            }

            // 记录 Unity 自己的 getter 说了什么（**只作对照，不作判据**）
            var back = overLifetime ? ps.colorOverLifetime.color : ps.main.startColor;
            Debug.Log(P + $"--- {label} ---");
            Debug.Log(P + $"    [{slot}] getter: mode={back.mode} "
                        + $"color={Col(back.color)} colorMin={Col(back.colorMin)} colorMax={Col(back.colorMax)}");

            // 发粒子：批处理下没有帧循环 ⇒ 手动发（`Emit` 比 `Simulate` 更确定；两者都试）
            var buf = new ParticleSystem.Particle[64];
            ps.Play();
            ps.Emit(8);
            int n = ps.GetParticles(buf);
            if (n == 0) { ps.Simulate(0.6f, false, true); n = ps.GetParticles(buf); }
            var sb = new StringBuilder();
            sb.Append(P).Append($"    实发粒子 {n} 颗，逐颗 startColor = ");
            for (int i = 0; i < n; i++) sb.Append(Col(buf[i].startColor)).Append(i < n - 1 ? " " : "");
            Debug.Log(sb.ToString());

            // 结论行：按「红 = min 槽 / 蓝 = max 槽」判
            string verdict;
            if (n == 0) verdict = "★ 没发出粒子 ⇒ 本档**无效**（Simulate 没生效），别当结论";
            else
            {
                int nMin = 0, nMax = 0, nOther = 0;
                for (int i = 0; i < n; i++)
                {
                    var c = buf[i].startColor;
                    if (c.r > c.b) nMin++;
                    else if (c.b > c.r) nMax++;
                    else nOther++;
                }
                if (mode == ParticleSystemGradientMode.Color)
                    verdict = nMax > nMin ? "★ 读的是 **maxColor（蓝）** ⇒ 我们只取 max **是对的**"
                            : nMin > nMax ? "★ 读的是 **minColor（红）** ⇒ 我们只取 max **是错的**，要改生成器"
                            : $"★ 红蓝混杂（红 {nMin} / 蓝 {nMax} / 其它 {nOther}）⇒ 本档判不了，看上面的逐颗值";
                else
                    verdict = $"对照档：红 {nMin} / 蓝 {nMax} / 其它 {nOther}"
                            + (nMin > 0 && nMax > 0 ? " ⇒ 两槽都用（符合 TwoColors 预期）" : " ⇒ ⚠️ 只用了单边？");
            }
            Debug.Log(P + "    " + verdict);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    static string Col(Color c) => $"({c.r:G5},{c.g:G5},{c.b:G5},{c.a:G5})";
}
