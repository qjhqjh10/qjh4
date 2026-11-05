// B148_W10_Assert.cs — 战场粒子四道闸门收口后的**产物自检**（`B148-W10` · 2026-11-05）
//
// ⚠️ **只建不接线**：本文件没有任何宿主调它（`Run()` 需要将来接进 `Editor/BattleScene.cs` 之类才会跑）。
//
// 【它盯什么】`资料/完全复刻/判据/B148_核_战场粒子缺口.md`（R7 逐场逐颗取证）：
//   原版 13 场 `ParticleSystem`（`!u!198`）= **578**；我方旧 prefab = **469**；差 109 = 80（原版 `m_IsActive=0`）
//   + 20（`renderMode=None`）+ 8（原版材质无贴图）+ 1（画质档）。本轮把前三类**照建**（闸③ 不动），
//   于是新期望 = **576**（只剩 1 颗「无贴图且 shader 不在白名单」+ 1 颗画质档不建）。
//
// 【四条断言】
//   ① 逐场 `!u!198` 数 == 清单按**现行闸门**算出来的应建数（`!u!198` 与 `!u!199` 还必须相等）。
//   ② **清单下标集合**：每颗建出来的粒子都挂了 `WarpforgeVFX.ArenaParticleIndex.index`（= 清单下标）⇒ 把
//      prefab 文本里的 index 集合与「应建的下标集合」**逐场比**。**这就是灭自证**：把闸② 改回「跳过」，
//      那 80 颗的下标就从集合里消失 ⇒ 红（只数 `!u!198` 总数也能发现，但按**下标**比能点名是哪颗）。
//   ③ `renderMode = 5 (None)` 的颗数：`m_RenderMode: 5` 的处数 == 清单里 renderMode==5 且应建的颗数
//      —— 这一条盯的是 `ArenaBuilder.cs:2877` 的 `Mathf.Clamp(p.renderMode, 0, 5)`：**只删闸④ 不改 clamp**
//      会把 None 悄悄夹成 `Mesh(4)`，那时 ① 仍然全绿（组件数对）、只有这一条会红。
//   ④ tau 的两处动画落点（判据 §4.1）：
//      · `…/Railgun BIG (1)/Big Gun Effect` 子树 `ParticleSystem` = **47**，且该节点**自己关着**
//        （`Railgun BIG.anim:10279-10280` 会把它 0→1）；改回「跳过」⇒ 这个是 0。
//      · `…/Railgun Turret 1` / `…/Railgun Turret 2` 两个子树里**层级关着**的 `ParticleSystem` 各 = **10**。
//      ⚠️ `B148` 任务简报里写的「`Big Gun Effect` 子树 = 67」是**把 tau 的 67 颗总数当成了子树数**：
//        实测 47 在 `Big Gun Effect` 下、另 20 在两条 `Railgun Turret */…/Bullet Particle*/Muzzle Flash glow` 下
//        （判据 = 原版 prefab 逐路径现数）⇒ 这里按**实测值**断言，并在报告里记一笔。
//
// ⚠️ 依赖：只在**重建过 13 件 `Resources/ArenaPrefabs/*.prefab`**（`ArenaBuilder.BuildArenaPrefabs`）之后才有意义
//   —— 盘上资产由下一次 Unity 窗口写。

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class B148_W10_Assert
{
    const string PrefabDir = "Assets/Resources/ArenaPrefabs";

    // 判据 §4.1 的两个落点（路径逐字来自原版 prefab，2026-11-05 现读）
    const string TauBigGunPath  = "Scenario/Battle Arena Tau Viorla Baked/Railgun BIG (1)/Big Gun Effect";
    const string TauTurret1Path = "Scenario/Battle Arena Tau Viorla Baked/Railgun Turret 1";
    const string TauTurret2Path = "Scenario/Battle Arena Tau Viorla Baked/Railgun Turret 2";
    const int    TauBigGunPsCount = 47;          // 原版 = 47（不是 67 —— 见文件头那条更正）
    const int    TauTurretInactivePsEach = 10;

    static int s_total, s_fail;

    public static void Run()
    {
        s_total = 0; s_fail = 0;
        int originTotal = 0, expectTotal = 0;
        var skipped = new List<string>();
        var allGot = new Dictionary<string, HashSet<int>>();

        foreach (var s in ArenaBuilder.AllArenas)
        {
            var mf = ArenaBuilder.LoadManifest(s);
            string prefabPath = PrefabDir + "/" + s + ".prefab";
            if (mf == null || mf.particles == null) { Check(false, $"{s}：清单载不到"); continue; }
            if (!File.Exists(prefabPath)) { Check(false, $"{s}：prefab 不在 `{prefabPath}`（先跑 BuildArenaPrefabs）"); continue; }

            string txt = File.ReadAllText(prefabPath);
            var quality = ArenaBuilder.LoadInactiveByQuality(s);
            var expectIdx = new List<int>();
            int expectMode5 = 0;
            for (int i = 0; i < mf.particles.Length; i++)
            {
                var p = mf.particles[i];
                bool noTex = string.IsNullOrEmpty(p.texFile);
                bool meshOk = ArenaBuilder.NoTexMeshModeOk(p, s);
                bool whitelisted = ArenaBuilder.NoTexWhitelistedOk(p, s);
                bool skipNoTex = noTex && !meshOk && !whitelisted;           // 闸①（只剩「不在白名单」那种）
                bool skipQuality = !skipNoTex && quality.Contains(p.go);      // 闸③（画质档，不动）
                if (skipNoTex || skipQuality)
                {
                    skipped.Add($"{s}/{p.go}（{(skipNoTex ? "无贴图且不在白名单" : "画质档")}）");
                    continue;
                }
                expectIdx.Add(i);
                if (p.renderMode == 5) expectMode5++;
            }
            originTotal += mf.particles.Length;
            expectTotal += expectIdx.Count;

            int ps198 = CountLinePrefix(txt, "--- !u!198 &");
            int ps199 = CountLinePrefix(txt, "--- !u!199 &");
            Check(ps198 == expectIdx.Count,
                  $"{s}：`!u!198` = {ps198}，应建 {expectIdx.Count}（清单 {mf.particles.Length}，跳过 {mf.particles.Length - expectIdx.Count}）");
            Check(ps199 == ps198, $"{s}：`!u!199` = {ps199} 与 `!u!198` = {ps198} 不等（有组件没渲染器）");

            var gotIdx = ReadParticleIndices(txt);
            var gotSet = new HashSet<int>(gotIdx);
            var missing = new List<int>();
            for (int i = 0; i < expectIdx.Count; i++) if (!gotSet.Contains(expectIdx[i])) missing.Add(expectIdx[i]);
            var expectSet = new HashSet<int>(expectIdx);
            var extra = new List<int>();
            for (int i = 0; i < gotIdx.Count; i++) if (!expectSet.Contains(gotIdx[i])) extra.Add(gotIdx[i]);
            Check(missing.Count == 0 && extra.Count == 0,
                  $"{s}：`ArenaParticleIndex` 下标 —— 缺 {missing.Count} 个{(missing.Count > 0 ? "（" + JoinNames(mf, missing) + "）" : "")}"
                + $" · 多 {extra.Count} 个{(extra.Count > 0 ? "（" + string.Join(",", extra.ConvertAll(x => x.ToString()).ToArray()) + "）" : "")}");
            allGot[s] = gotSet;

            int mode5 = CountSub(txt, "m_RenderMode: 5");
            Check(mode5 == expectMode5,
                  $"{s}：`m_RenderMode: 5` = {mode5}，应 {expectMode5}（不等 ⇒ `Mathf.Clamp(renderMode,0,5)` 没改，None 被夹成了 Mesh）");
        }

        // ---- 总账（判据：578 → 576，只剩 2 颗不建）----
        Check(originTotal == 578, $"原版清单合计 = {originTotal}，判据是 578");
        Check(expectTotal == 576, $"应建合计 = {expectTotal}，判据是 576（578 − 无贴图不在白名单 1 − 画质档 1）");
        Check(skipped.Count == 2, $"实际跳过 = {skipped.Count}：{string.Join(" / ", skipped.ToArray())}");
        Debug.Log($"[B148_W10] 总账：原版 {originTotal} → 应建 {expectTotal}（跳过 {skipped.Count}：{string.Join(" / ", skipped.ToArray())}）");

        // ---- 灭自证：闸② 放行的 80 颗必须**全部**在 prefab 里（改回「跳过」⇒ 这一条立刻红）----
        int inactiveTotal = 0;
        var inactiveMissing = new List<string>();
        foreach (var s in ArenaBuilder.AllArenas)
        {
            var mf = ArenaBuilder.LoadManifest(s);
            if (mf == null || mf.particles == null) continue;
            allGot.TryGetValue(s, out var got);
            if (got == null) got = new HashSet<int>();
            for (int i = 0; i < mf.particles.Length; i++)
            {
                var p = mf.particles[i];
                if (p.active) continue;
                inactiveTotal++;
                if (!got.Contains(i)) inactiveMissing.Add($"{s}:{i}:{p.go}");
            }
        }
        Check(inactiveTotal == 80, $"清单里 `active=false` 的粒子 = {inactiveTotal}，判据是 80");
        Check(inactiveMissing.Count == 0,
              $"闸②（原版关着）放行的 {inactiveTotal} 颗里，没建出来的 = {inactiveMissing.Count}"
            + (inactiveMissing.Count > 0 ? "：" + string.Join(" / ", inactiveMissing.ToArray()) + "（**闸② 被改回「跳过」了？**）" : ""));

        // ---- tau 的两处动画落点 ----
        TauAnchorsCheck();

        Debug.Log($"[B148_W10] 断言合计：{s_total} 项 · 失败 {s_fail} 项"
                + (s_fail == 0 ? " —— **全绿**" : " —— 🔴 有红，见上面每条"));
    }

    // ───────────────────────── 判据 §4.1 的两个动画落点 ─────────────────────────
    static void TauAnchorsCheck()
    {
        const string scene = "battlearenatauviorla";
        string path = PrefabDir + "/" + scene + ".prefab";
        if (!File.Exists(path)) { Check(false, $"{scene}：prefab 不在 `{path}`"); return; }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Check(false, $"{scene}：`AssetDatabase` 载不到 prefab（没导入？）"); return; }

        var bigGun = prefab.transform.Find(TauBigGunPath);
        Check(bigGun != null, $"{scene}：树里找不到 `{TauBigGunPath}`");
        if (bigGun != null)
        {
            int n = bigGun.GetComponentsInChildren<ParticleSystem>(true).Length;
            Check(n == TauBigGunPsCount, $"{scene}：`Big Gun Effect` 子树 ParticleSystem = {n}，判据 {TauBigGunPsCount}");
            Check(!bigGun.gameObject.activeSelf,
                  $"{scene}：`Big Gun Effect` 节点**应该是关着的**（原版 `m_IsActive=0`，由 `Railgun BIG.anim` 打开）—— 现在是开的");
        }

        foreach (var branch in new[] { TauTurret1Path, TauTurret2Path })
        {
            var t = prefab.transform.Find(branch);
            Check(t != null, $"{scene}：树里找不到 `{branch}`");
            if (t == null) continue;
            int off = 0;
            foreach (var ps in t.GetComponentsInChildren<ParticleSystem>(true))
                if (!IsActiveInHierarchy(ps.transform)) off++;
            Check(off == TauTurretInactivePsEach,
                  $"{scene}：`{branch}` 子树里**层级关着**的 ParticleSystem = {off}，判据 {TauTurretInactivePsEach}");
        }
    }

    // ───────────────────────── 小工具 ─────────────────────────
    static void Check(bool ok, string what)
    {
        s_total++;
        if (ok) { Debug.Log($"[B148_W10] ✓ {what}"); return; }
        s_fail++;
        Debug.LogError($"[B148_W10] ✗ {what}");
    }

    /// <summary>数「行首就是 `<prefix>`」的行数（`--- !u!198 &` 这类 YAML 块头）。
    /// ⚠️ 手写扫描而不用 `Regex.Multiline`：13 件 prefab 合计 60 MB 级文本，正则回溯太贵。</summary>
    static int CountLinePrefix(string txt, string prefix)
    {
        int n = 0, i = 0;
        while (i < txt.Length)
        {
            int e = txt.IndexOf('\n', i);
            if (e < 0) e = txt.Length;
            int len = e - i; if (len > 0 && txt[i + len - 1] == '\r') len--;
            if (len >= prefix.Length && string.CompareOrdinal(txt, i, prefix, 0, prefix.Length) == 0) n++;
            i = e + 1;
        }
        return n;
    }

    static int CountSub(string txt, string sub)
    {
        int n = 0, i = 0;
        while ((i = txt.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }

    /// <summary>从 prefab 文本里抽 `WarpforgeVFX.ArenaParticleIndex.index`（建场时逐颗刻上去的**清单下标**）。
    /// ⚠️ 必须锚在 `m_EditorClassIdentifier` 上 —— 别的组件也可能有 `index` 字段。</summary>
    static readonly Regex IndexRe = new Regex(
        @"m_EditorClassIdentifier: Assembly-CSharp::WarpforgeVFX\.ArenaParticleIndex\s*\r?\n\s*index:\s*(\d+)",
        RegexOptions.Compiled);
    static List<int> ReadParticleIndices(string txt)
    {
        var list = new List<int>();
        foreach (Match m in IndexRe.Matches(txt)) list.Add(int.Parse(m.Groups[1].Value));
        return list;
    }

    static bool IsActiveInHierarchy(Transform t)
    {
        for (var c = t; c != null; c = c.parent) if (!c.gameObject.activeSelf) return false;
        return true;
    }

    static string JoinNames(ArenaBuilder.Manifest mf, List<int> idx)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < idx.Count && i < 12; i++)
        {
            if (sb.Length > 0) sb.Append(" / ");
            int k = idx[i];
            sb.Append(k).Append(':').Append(k >= 0 && k < mf.particles.Length ? mf.particles[k].go : "?");
        }
        if (idx.Count > 12) sb.Append(" …");
        return sb.ToString();
    }
}
