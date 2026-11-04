// B140_WF_Assert.cs — 执行写手 `B140_WF`（2026-11-04）
//
// 本文件装两件事（同一处维护，⛔ 别把规则复制到两个文件里）：
//   ① **生成器补丁**：`BuildAndSaveScene()` 存盘前把 `m_StitchLightmapSeams` 写 0（`14-05` 在 `Battle.unity` 盘上回退成 1）；
//   ② **回读自证 + 断言**：存盘后读**盘上的 YAML**（⛔ 不读内存字段）验 `B139-N1` 的 7 格 + `14-05` 的渲染器全 0，
//      以及 `B139-H1`（`ArenaBuilder.LoadSpriteAsset` 的 mip 必须走真值表）。
//
// ============================ 判据（逐条带出处） ============================
// · `B139-N1`（7 格）→ `判据/B140_对账_G4_B139新账.md:29` ·
//   原版 `Battle Arena 1.unity` `!u!104 &2 RenderSettings` + `!u!157 &3 LightmapSettings`（13 场逐字同值，B139-Y 回读通过）：
//     `m_AmbientEquatorColor (0.3254717, 0.5862051, 1, 1)` · `m_AmbientGroundColor (0.5896226, 0.5896226, 1, 1)` ·
//     `m_SubtractiveShadowColor (0.6367924, 0.7275943, 1, 1)` ·
//     `m_BakeBackend 0` · `m_PVRSampleCount 500` · `m_PVRFilteringGaussRadiusIndirect 5` · `m_PVRFilteringGaussRadiusAO 2`
//   ⚠️ 这 7 格的**唯一来源** = `ArenaBuilder` 的公开常量（`AmbientEquatorYaml` / `BakeBackendValue` …），
//      生产侧（`PatchLightmapEditorSettings`）与本文件**引用同一组**，⛔ 不在断言里另抄一份数值。
// · `14-05`（`m_StitchLightmapSeams`）→ `判据/B140_对账_G6_审计指针.md:159-162`（现读 `Battle.unity` 18 个渲染器 **全 1**、
//   HEAD 也是 1；原版 **0**）。`B139-Y` 那句「Battle.unity 18 个全 0」已被一次场景重存覆盖 ⇒ 必须写进生成器。
// · `B139-H1`（精灵 mip）→ `判据/B140_对账_G4_B139新账.md:22`：`LoadSpriteAsset` 里无条件 `ti.mipmapEnabled = false`
//   与同文件 `ApplyOriginalImport` 的逐张真值表（`arena_texture_import.tsv` / `vfx_texture_mips.tsv`，
//   经 `ArtBaker.OriginalMipEnabled` 读）打架。原版 `m_MipCount > 1` 才开 mip。
//
// ============================ 三组断言 ============================
//  A · 生成器源码（纯文本，不跑 Unity）：调用点在、旧写法 0 残留 —— 挡「有人改回去」
//  B · 真值表（纯查表，不跑 Unity）：`SpriteMipEnabled` 两向都成立（灭自证：恒 false ⇒ B2 红；恒 true ⇒ B1 红）
//  C · 盘上 YAML（`CheckSceneRenderBake`，`BuildAndSaveScene()` 存盘后自动调）：7 格 + 18 个渲染器全 0
//  判绿红：`grep "^B140_WF "` 的合计行；逐条按**行首** `B140_WF ✓` / `B140_WF ✗` 数（⛔ 别 `grep -c ✗`）。
//
// ⚠️ C 组在**没重建过场景**时会红（盘上 `m_StitchLightmapSeams` 现在还是 1）—— 那是如实状态：
//    跑一次 `BattleScene.BuildAndSaveScene` 才会绿。本文件不跑 Unity。

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>`B140-WF`：`B139-N1`（7 格写回 + 回读自证）· `14-05`（`m_StitchLightmapSeams` 写进生成器）· `B139-H1`（精灵 mip 走真值表）。</summary>
public static class B140_WF_Assert
{
    const string P = "B140_WF";
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    /// <summary>`Battle.unity`（含 `.meta` 之外的纯 YAML）—— C 组与 `Run()` 的默认目标。</summary>
    public static string BattleScenePath
    {
        get { return Path.Combine(Application.dataPath, "CardPresentation/Scenes/Battle.unity"); }
    }

    // ---------------------------------------------------------------- Check / 合计
    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + " ✓ " + msg); }
        else { _fail++; Debug.LogError(P + " ✗ " + msg); }
    }

    static void Group(string name, Action body)
    {
        try { body(); }
        catch (Exception e)
        {
            Check(false, name + "：**这一段抛异常**（后面的断言没跑到，⛔ 别读成通过）—— "
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    static void Total(string what)
    {
        Debug.Log(P + " 合计[" + what + "]：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ================================================================ ① 生成器补丁（存盘前）
    /// <summary>`14-05`：把场景里**全部** `MeshRenderer` / `SkinnedMeshRenderer` 的 `m_StitchLightmapSeams` 写成 0。
    /// <para>原版 `Battle.unity` 18 个渲染器**全是 0**、13 场 arena 1,206 个也全是 0；
    /// Unity 新建渲染器的默认值是 **1** ⇒ 不在 `SaveScene` 之前写一遍，每次重建都会回退成 1
    /// （`B139-Y` 手改的盘上值已经被一次重存覆盖过，实证见 `判据/B140_对账_G6_审计指针.md:159-162`）。</para>
    /// <para>⚠️ 与 `ArenaBuilder.ApplyRendererParams` **同一手法**（`SerializedObject`，该字段没有公开 API），
    /// 不是第二套规则。返回「成功写到的渲染器个数」。</para></summary>
    public static int ApplyStitchSeams(Scene scene)
    {
        int n = 0, missing = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) if (SetStitch(r)) n++; else missing++;
            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (SetStitch(r)) n++; else missing++;
        }
        if (missing > 0)
            Debug.LogError(P + " ✗ `m_StitchLightmapSeams` 属性在 " + missing + " 个渲染器上找不到 ⇒ 没能写 0（Unity 版本变了？）");
        Debug.Log(P + " 14-05：`m_StitchLightmapSeams` 已对 " + n + " 个渲染器写 0（原版全 0；Unity 默认 1）");
        return n;
    }

    static bool SetStitch(Renderer r)
    {
        if (r == null) return false;
        var so = new SerializedObject(r);
        var p = so.FindProperty("m_StitchLightmapSeams");
        if (p == null) return false;
        if (p.boolValue) { p.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
        return true;
    }

    // ================================================================ ② 存盘后回读 YAML（C 组）
    /// <summary>存盘后**回读刚写出的那份 YAML**：7 格 = 原版值、全部 `m_StitchLightmapSeams` = 0。
    /// <para>⚠️ 读文件不读对象：`SaveScene` 之后对象上的值与落盘值可能不一致（`SetDirty` 那类坑）。</para></summary>
    public static void CheckSceneRenderBake(string scenePath)
    {
        _pass = 0; _fail = 0;
        Group("C·存盘后的场景 YAML", () => CheckSceneYamlCore(scenePath));
        Total("场景 YAML");
    }

    static void CheckSceneYamlCore(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
        { Check(false, "场景文件不存在（`" + scenePath + "`）⇒ 存盘那一步没成功"); return; }

        var blocks = ParseBlocks(scenePath);
        Block rs = null, lms = null;
        foreach (var b in blocks)
        {
            if (IsHead(b, 104)) rs = b;
            else if (IsHead(b, 157)) lms = b;
        }
        // 先断前提：两个块都在，再断结论（否则「找不到块」会被读成「值对」）
        Check(rs != null, "C0a 场景里有 `!u!104 RenderSettings` 块");
        Check(lms != null, "C0b 场景里有 `!u!157 LightmapSettings` 块");
        if (rs == null || lms == null) return;

        Check(YamlColorIs(rs, "m_AmbientEquatorColor", ArenaBuilder.AmbientEquatorYaml),
              "C1 `m_AmbientEquatorColor` = 原版 " + ArenaBuilder.AmbientEquatorYaml + "（现读 " + Field(rs, "m_AmbientEquatorColor") + "）");
        Check(YamlColorIs(rs, "m_AmbientGroundColor", ArenaBuilder.AmbientGroundYaml),
              "C2 `m_AmbientGroundColor` = 原版 " + ArenaBuilder.AmbientGroundYaml + "（现读 " + Field(rs, "m_AmbientGroundColor") + "）");
        Check(YamlColorIs(rs, "m_SubtractiveShadowColor", ArenaBuilder.SubtractiveShadowYaml),
              "C3 `m_SubtractiveShadowColor` = 原版 " + ArenaBuilder.SubtractiveShadowYaml + "（现读 " + Field(rs, "m_SubtractiveShadowColor") + "）");
        Check(SubInt(lms, "m_LightmapEditorSettings", "m_BakeBackend") == ArenaBuilder.BakeBackendValue,
              "C4 `m_BakeBackend` = " + ArenaBuilder.BakeBackendValue + "（现读 " + SubField(lms, "m_LightmapEditorSettings", "m_BakeBackend") + "；Unity 6 默认 2）");
        Check(SubInt(lms, "m_LightmapEditorSettings", "m_PVRSampleCount") == ArenaBuilder.PvrSampleCountValue,
              "C5 `m_PVRSampleCount` = " + ArenaBuilder.PvrSampleCountValue + "（现读 " + SubField(lms, "m_LightmapEditorSettings", "m_PVRSampleCount") + "）");
        Check(SubInt(lms, "m_LightmapEditorSettings", "m_PVRFilteringGaussRadiusIndirect") == ArenaBuilder.GaussIndirectValue
              && SubInt(lms, "m_LightmapEditorSettings", "m_PVRFilteringGaussRadiusAO") == ArenaBuilder.GaussAOValue,
              "C6 Gauss 半径 Indirect/AO = " + ArenaBuilder.GaussIndirectValue + " / " + ArenaBuilder.GaussAOValue
              + "（现读 " + SubField(lms, "m_LightmapEditorSettings", "m_PVRFilteringGaussRadiusIndirect")
              + " / " + SubField(lms, "m_LightmapEditorSettings", "m_PVRFilteringGaussRadiusAO") + "）");

        // ---- 14-05：所有渲染器的 m_StitchLightmapSeams 必须 0（先断「至少有 1 个」再断「全是 0」）----
        int total = 0, ones = 0, other = 0;
        foreach (var b in blocks)
        {
            if (!IsHead(b, 23) && !IsHead(b, 137)) continue;       // 23 = MeshRenderer · 137 = SkinnedMeshRenderer
            foreach (var l in b.Lines)
            {
                if (!l.StartsWith("  m_StitchLightmapSeams:", StringComparison.Ordinal)) continue;
                total++;
                string v = l.Substring("  m_StitchLightmapSeams:".Length).Trim();
                if (v == "0") continue;
                if (v == "1") ones++; else other++;
            }
        }
        Check(total > 0, "C7 盘上有渲染器携带 `m_StitchLightmapSeams`（现读 " + total + " 个）");
        Check(total > 0 && ones == 0 && other == 0,
              "C8 **全部** `m_StitchLightmapSeams` = 0（现读 " + total + " 个里 1 有 " + ones + " 个、其它值 " + other
              + " 个；原版 Battle.unity 18 个全 0 / 13 场 1,206 个全 0）"
              + (ones > 0 || other > 0 ? " —— 盘上值未落，需跑一次 `BattleScene.BuildAndSaveScene`" : ""));
    }

    // ================================================================ ③ 入口 Run()（A/B 组，不跑 Unity）
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Group("A·生成器源码", CheckSourceWiring);
        Group("B·精灵 mip 真值表", CheckSpriteMipTruth);
        Group("C·盘上场景 YAML", () => CheckSceneYamlCore(BattleScenePath));
        Total("B140_WF");
    }

    // ---------------------------------------------------------------- A 组：源码接线
    static void CheckSourceWiring()
    {
        string battleRel = "CardPresentation/Editor/BattleScene.cs";
        string arenaRel = "WarpforgeArena1/Editor/ArenaBuilder.cs";
        var battle = LiveLines(Path.Combine(Application.dataPath, battleRel));
        var arena = LiveLines(Path.Combine(Application.dataPath, arenaRel));
        if (battle.Count == 0) { Check(false, "读不到 `" + battleRel + "` ⇒ A 组一条都证明不了"); return; }
        if (arena.Count == 0) { Check(false, "读不到 `" + arenaRel + "` ⇒ A 组 H1 那几条证明不了"); return; }

        Check(Has(battle, "ArenaBuilder.PatchLightmapEditorSettings(ScenePath, true);"),
              "A1 `BuildAndSaveScene` 存盘后**真的调**了 7 格补丁（`PatchLightmapEditorSettings(ScenePath, true)`）");
        Check(Has(battle, "B140_WF_Assert.CheckSceneRenderBake(ScenePath);"),
              "A2 存盘后**真的调**了回读自证（`CheckSceneRenderBake`）");
        Check(Has(battle, "B140_WF_Assert.ApplyStitchSeams(scene);"),
              "A3 存盘**前**真的调了 `ApplyStitchSeams`（14-05 写进生成器）");

        // H1：`LoadSpriteAsset` 的方法体里必须走真值表、且不得再有旧的「无条件 false」
        string body = MethodBody(arena, "static Sprite LoadSpriteAsset(");
        Check(body.Length > 0, "A4 取得到 `ArenaBuilder.LoadSpriteAsset` 的方法体");
        Check(body.Contains("SpriteMipEnabled(path, false)"),
              "A5 `LoadSpriteAsset` 的 mip 走 `SpriteMipEnabled(path, false)`（真值表）");
        Check(!body.Contains("ti.mipmapEnabled      = false"),
              "A6 旧的**无条件** `ti.mipmapEnabled      = false` 0 残留（与真值表打架的那一行）");
    }

    // ---------------------------------------------------------------- B 组：真值表（灭自证）
    static void CheckSpriteMipTruth()
    {
        // ⚠️ 只借文件名做**查表**（`OriginalNameCandidates` 取的是 basename）⇒ 目录不要求真存在，也⛔ 不去动任何 `.meta`。
        string dir = "battlearena1/Sprites/";
        // B1：真值表说「没有 mip」的精灵 ⇒ false（恒 true 的实现会红在这里）
        Check(!ArenaBuilder.SpriteMipEnabled(dir + "Atack_Icon BW.png", false),
              "B1 `Atack_Icon BW`（原版 `m_MipCount = 1`）⇒ mip **关**");
        // B2：真值表说「有 mip」的名字 ⇒ true（恒 false 的实现 —— 也就是修前那行 —— 会红在这里）
        Check(ArenaBuilder.SpriteMipEnabled(dir + "Explosion.png", false),
              "B2 `Explosion`（原版 `m_MipCount = 12`）⇒ mip **开**（⛔ 正是修前无条件 false 弄错的那一档）");
        // B3/B4：表里没有 ⇒ **保留现值**（⛔ 不许猜成 false）
        Check(ArenaBuilder.SpriteMipEnabled(dir + "__B140_WF_NoSuchTexture.png", true),
              "B3 表外名字 ⇒ 保留现值 true（不猜、也不静默当成「原版没有 mip」）");
        Check(!ArenaBuilder.SpriteMipEnabled(dir + "__B140_WF_NoSuchTexture.png", false),
              "B4 表外名字 ⇒ 保留现值 false（两向都断 ⇒ 不可能靠「恒真/恒假」蒙过去）");
    }

    // ================================================================ 小工具
    static List<string> LiveLines(string path)
    {
        var list = new List<string>();
        if (!File.Exists(path)) return list;
        foreach (var l0 in File.ReadAllLines(path))
        {
            if (l0.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
            list.Add(l0);
        }
        return list;
    }

    static bool Has(List<string> lines, string must)
    {
        foreach (var l in lines) if (l.Contains(must)) return true;
        return false;
    }

    /// <summary>取以 `signature` 开头的那一行之后、到下一个「4 空格缩进的成员注释/声明」之前的方法体（拼成一个字符串）。</summary>
    static string MethodBody(List<string> lines, string signature)
    {
        int start = -1;
        for (int i = 0; i < lines.Count; i++) if (lines[i].Contains(signature)) { start = i; break; }
        if (start < 0) return string.Empty;
        var sb = new System.Text.StringBuilder();
        for (int i = start; i < lines.Count; i++)
        {
            if (i > start && (lines[i].StartsWith("    ///", StringComparison.Ordinal)
                           || lines[i].StartsWith("    static ", StringComparison.Ordinal)
                           || lines[i].StartsWith("    public ", StringComparison.Ordinal)))
                break;
            sb.AppendLine(lines[i]);
        }
        return sb.ToString();
    }

    // ---- Unity YAML：`--- !u!<cls> &<id>` 分块
    sealed class Block { public string Head; public readonly List<string> Lines = new List<string>(); }

    static List<Block> ParseBlocks(string path)
    {
        var res = new List<Block>();
        Block cur = null;
        foreach (var raw in File.ReadAllLines(path))
        {
            if (raw.StartsWith("--- !u!", StringComparison.Ordinal))
            {
                cur = new Block { Head = raw.Trim() };
                res.Add(cur);
            }
            else if (cur != null) cur.Lines.Add(raw);
        }
        return res;
    }

    static bool IsHead(Block b, int cls)
    {
        return b != null && b.Head.StartsWith("--- !u!" + cls + " &", StringComparison.Ordinal);
    }

    /// <summary>`  key: value`（两个空格缩进的顶格字段）。</summary>
    static string Field(Block b, string key)
    {
        if (b == null) return null;
        string pref = "  " + key + ":";
        foreach (var l in b.Lines)
            if (l.StartsWith(pref, StringComparison.Ordinal)) return l.Substring(pref.Length).Trim();
        return null;
    }

    /// <summary>`subBlock:` 子块里 `    key: value`（四个空格缩进）的 value。</summary>
    static string SubField(Block b, string subBlock, string key)
    {
        if (b == null) return null;
        bool inSub = false;
        foreach (var l in b.Lines)
        {
            if (l.StartsWith("  " + subBlock + ":", StringComparison.Ordinal)) { inSub = true; continue; }
            if (!inSub) continue;
            if (l.Length > 2 && !l.StartsWith("    ")) return null;           // 出了子块
            if (l.StartsWith("    " + key + ":", StringComparison.Ordinal))
                return l.Substring(("    " + key + ":").Length).Trim();
        }
        return null;
    }

    static int SubInt(Block b, string subBlock, string key)
    {
        string v = SubField(b, subBlock, key);
        int i;
        return int.TryParse(v, NumberStyles.Integer, Inv, out i) ? i : -99999;
    }

    static bool YamlColorIs(Block b, string key, string wantYaml)
    {
        var got = ParseColor(Field(b, key));
        var want = ParseColor(wantYaml);
        return got.HasValue && want.HasValue
            && Mathf.Abs(got.Value.r - want.Value.r) <= 1e-6f && Mathf.Abs(got.Value.g - want.Value.g) <= 1e-6f
            && Mathf.Abs(got.Value.b - want.Value.b) <= 1e-6f && Mathf.Abs(got.Value.a - want.Value.a) <= 1e-6f;
    }

    static Color? ParseColor(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        float r, g, b, a;
        if (!TryKeyed(s, "r", out r) || !TryKeyed(s, "g", out g) || !TryKeyed(s, "b", out b) || !TryKeyed(s, "a", out a)) return null;
        return new Color(r, g, b, a);
    }

    static bool TryKeyed(string s, string key, out float val)
    {
        val = 0f;
        int i = s.IndexOf(key + ":", StringComparison.Ordinal);
        if (i < 0) return false;
        string rest = s.Substring(i + key.Length + 1).TrimStart();
        int j = rest.IndexOfAny(new[] { ',', '}' });
        if (j >= 0) rest = rest.Substring(0, j);
        return float.TryParse(rest.Trim(), NumberStyles.Float, Inv, out val);
    }

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
}
