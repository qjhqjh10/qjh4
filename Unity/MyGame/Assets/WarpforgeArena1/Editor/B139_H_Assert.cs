// B139_H_Assert.cs — 工作包 H 的自检断言（战场场景生成器 · 纹理烘焙器）
//
// 覆盖两件活的**产物级**证据（全部只读文本/资产，⛔ 不建场、不烘图）：
//   ① H-1：`ArenaBuilder` 生成 13 场场景时写出的原版 ambient / lightmap / bake / renderer 参数
//   ② H-2：`ArtBaker` 不再无条件覆盖已按原版设好的 `.meta`（mip / wrap 三档 / aniso / textureType）
//
// 判据出处：`资料/完全复刻/报告/B139_Y_YAML与ProjectSettings.md` §一（14-02/03/04/05）
//          · `报告/B136_审计_纹理与图集.md` #14/#16/#19 · `报告/B139_I_纹理导入.md` §四·3
//
// 用法（菜单）：Tools > Warforge > B139_H 自检
// 用法（命令行）：-executeMethod B139_H_Assert.RunFromCLI
//
// ⚠️ 本文件**只做检查**：所有写入都在 `ArenaBuilder` / `ArtBaker` 里。断言失败一律用 Warning（红），
//    并且在开头先断「前提成立」（文件在不在、条条数对不对）——「0 条 = 什么都没验到」不能算绿。

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class B139_H_Assert
{
    const string T = "[B139_H] ";
    static int _ok, _bad;

    [MenuItem("Tools/Warforge/B139_H 自检（战场生成器 + ArtBaker）")]
    public static void RunFromCLI() { VerifyAll(); }

    static void Check(bool cond, string what)
    {
        if (cond) { _ok++; Debug.Log(T + "✓ " + what); }
        else { _bad++; Debug.LogWarning(T + "✗ " + what); }
    }

    public static void VerifyAll()
    {
        _ok = _bad = 0;
        CheckManifests();
        CheckScenes();
        CheckParticleMaterials();
        CheckArtBakerTruthTable();
        CheckMetas();
        Debug.Log(T + $"合计：✓ {_ok} · ✗ {_bad}" + (_bad == 0 ? "（全绿）" : "（有红，见上）"));
    }

    // ───────────────────────── ① H-1：清单里必须带新抽出来的字段 ─────────────────────────
    static void CheckManifests()
    {
        int have = 0;
        foreach (var scene in ArenaBuilder.AllArenas)
        {
            var p = ArenaBuilder.ManifestPath(scene);
            if (!File.Exists(p)) { Check(false, $"{scene}：清单不存在（{p}）"); continue; }
            have++;
            string txt = File.ReadAllText(p);
            int nM = Regex.Matches(txt, "\"renderingLayerMask\":").Count;
            int nL = Regex.Matches(txt, "\"lightProbeUsage\":").Count;
            int nS = Regex.Matches(txt, "\"sortingOrder\":").Count;
            int nP = Regex.Matches(txt, "\"cullingMode\":").Count;
            int nE = Regex.Matches(txt, "\"emitterVelocityMode\":").Count;
            int nSh = Regex.Matches(txt, "\"shapeLength\":").Count;
            int nUv = Regex.Matches(txt, "\"uvStartFrame\":").Count;
            int nSub = Regex.Matches(txt, "\"subtractiveShadow\":").Count;
            Check(nM > 0 && nM == nL && nL == nS, $"{scene}：网格渲染器三格齐（renderingLayerMask {nM} = lightProbeUsage {nL} = sortingOrder {nS}）");
            Check(nP > 0 && nP == nE && nE == nSh, $"{scene}：粒子三格齐（cullingMode {nP} = emitterVelocityMode {nE} = shapeLength {nSh}）");
            Check(nUv > 0, $"{scene}：UV 模块 7 格已抽（uvStartFrame {nUv} 条）");
            Check(nSub == 1, $"{scene}：`ambient.subtractiveShadow` 在（{nSub} 条，应为 1）");
        }
        Check(have == ArenaBuilder.AllArenas.Length, $"13 场清单都在（{have}/{ArenaBuilder.AllArenas.Length}）");
    }

    // ───────────────────────── ② H-1：生成的场景 YAML 必须带原版值 ─────────────────────────
    // 原版值（13 场 `!u!104` / `!u!157` 实读）：equator (0.3254717,0.5862051,1,1) ·
    // ground (0.5896226,0.5896226,1,1) · subtractive (0.6367924,0.7275943,1,1) ·
    // bake 4 格 = 0/500/5/2 · `m_StitchLightmapSeams` 全 0。
    static void CheckScenes()
    {
        int seen = 0;
        foreach (var scene in ArenaBuilder.AllArenas)
        {
            var p = ArenaBuilder.ScenePath(scene);
            if (!File.Exists(p)) continue;                      // 未生成的场不判（先断「生成过几场」）
            seen++;
            string txt = File.ReadAllText(p);
            Check(txt.Contains("m_AmbientEquatorColor: {r: 0.3254717, g: 0.5862051, b: 1, a: 1}"),
                  $"{scene}：`m_AmbientEquatorColor` = 原版值");
            Check(txt.Contains("m_AmbientGroundColor: {r: 0.5896226, g: 0.5896226, b: 1, a: 1}"),
                  $"{scene}：`m_AmbientGroundColor` = 原版值");
            Check(txt.Contains("m_SubtractiveShadowColor: {r: 0.6367924, g: 0.7275943, b: 1, a: 1}"),
                  $"{scene}：`m_SubtractiveShadowColor` = 原版值");
            Check(txt.Contains("m_BakeBackend: 0") && txt.Contains("m_PVRSampleCount: 500")
                  && txt.Contains("m_PVRFilteringGaussRadiusIndirect: 5") && txt.Contains("m_PVRFilteringGaussRadiusAO: 2"),
                  $"{scene}：烘焙四格 = 0 / 500 / 5 / 2");
            int stitchOn = Regex.Matches(txt, "m_StitchLightmapSeams: 1").Count;
            int stitchAny = Regex.Matches(txt, "m_StitchLightmapSeams:").Count;
            Check(stitchAny > 0 && stitchOn == 0, $"{scene}：`m_StitchLightmapSeams` 全 0（{stitchAny} 个渲染器 · 残留 1 = {stitchOn}）");
        }
        Check(seen > 0, $"至少有一份生成的场景可判（{seen} 份）—— ⚠️ 0 份 = 本段什么都没验到");
    }

    // ───────────────────────── ③ 8-01：加性粒子的混合 ─────────────────────────
    // 判据 = 原版 shader `Mobile/Particles/Additive` ⇒ `Blend SrcAlpha One`（5/1），alpha 侧同值。
    static readonly string[,] AdditiveMats =
    {
        { "battlearena3", "PS_citclr_light" }, { "battlearena3", "PS_treasure_ray" },
        { "battlearena3", "PS_default_001_add" }, { "battlearenaaeldari", "PS_Fire1" },
        { "battlearenaemperorschildren", "PS_bubble_light" }, { "battlearenatauviorla", "PS_MuzzleFlash_Side1" },
    };

    static void CheckParticleMaterials()
    {
        for (int i = 0; i < AdditiveMats.GetLength(0); i++)
        {
            var rel = AdditiveMats[i, 0] + "/" + AdditiveMats[i, 1];
            var p = $"{ArenaBuilder.ArenaDir(AdditiveMats[i, 0])}/Materials/{AdditiveMats[i, 1]}.mat";
            if (!File.Exists(p)) { Check(false, $"{rel}：材质不存在（{p}）"); continue; }
            string t = File.ReadAllText(p);
            Check(FieldIs(t, "_SrcBlend", 5) && FieldIs(t, "_DstBlend", 1)
                  && FieldIs(t, "_SrcBlendAlpha", 5) && FieldIs(t, "_DstBlendAlpha", 1),
                  $"{rel}：加性混合 5/1（彩 & alpha 两侧）");
        }
    }

    static bool FieldIs(string txt, string key, int want)
    {
        var m = Regex.Match(txt, "^    - " + key + ":\\s*(-?\\d+)\\s*$", RegexOptions.Multiline);
        return m.Success && int.Parse(m.Groups[1].Value) == want;
    }

    // ───────────────────────── ④ H-2：ArtBaker 的三档表真能表达 (0,1) ─────────────────────────
    static void CheckArtBakerTruthTable()
    {
        int u, v;
        Check(ArtBaker.OriginalWrap("Assets/WarpforgeVFX/Textures/Card Damage_sprite.png", out u, out v) && u == 0 && v == 0,
              "ArtBaker：`Card Damage_sprite` ⇒ 原版 (0,0) Repeat");
        Check(ArtBaker.OriginalWrap("Assets/WarpforgeVFX/Textures/ring2.png", out u, out v) && u == 1 && v == 1,
              "ArtBaker：`ring2` ⇒ 原版 (1,1) Clamp");
        Check(ArtBaker.OriginalWrap("Assets/CardPresentation/Resources/Art/vfx/Shine trail.png", out u, out v) && u == 0 && v == 1,
              "ArtBaker：`Shine trail` ⇒ 原版 (0,1) **混合档**（布尔白名单表达不了的那种）");
        Check(ArtBaker.OriginalMipEnabled("Assets/CardPresentation/Resources/Art/ui_menu/Background.png", false),
              "ArtBaker：`ui_menu/Background` ⇒ 原版有 mip（不能被无条件关成 false）");
        Check(!ArtBaker.OriginalMipEnabled("Assets/CardPresentation/Resources/Art/ui_menu/40K_shop_offer_bg_UM.png", true),
              "ArtBaker：`40K_shop_offer_bg_*` ⇒ 原版无 mip");
    }

    // ───────────────────────── ⑤ B139-I3：7 张 wrap / 3 张 aniso 已按原版 ─────────────────────────
    static readonly string[,] MetaWant =
    {
        { "Assets/WarpforgeVFX/Textures/Card Damage_sprite.png.meta",              "0", "0", null },
        { "Assets/WarpforgeVFX/Textures/Card Heal_sprite.png.meta",                "0", "0", null },
        { "Assets/WarpforgeVFX/Textures/Card Ready For Level Up_sprite.png.meta",  "0", "0", null },
        { "Assets/WarpforgeVFX/Textures/Up Rays_sprite.png.meta",                  "0", "0", null },
        { "Assets/WarpforgeVFX/Textures/ring2.png.meta",                           "1", "1", null },
        { "Assets/CardPresentation/Resources/Art/vfx/Shine trail.png.meta",        "0", "1", null },
        { "Assets/CardPresentation/Art/原版/去重资源/2_40k_cardframe_rarity_rare.png.meta", "1", "1", null },
        { "Assets/CardPresentation/Resources/Art/ui_menu/Background.png.meta",     null, null, "16" },
        { "Assets/CardPresentation/Resources/Art/ui_menu/InputFieldBackground.png.meta", null, null, "16" },
        { "Assets/WarpforgeVFX/Textures/Debris_sprite.png.meta",                   null, null, "16" },
    };

    static void CheckMetas()
    {
        for (int i = 0; i < MetaWant.GetLength(0); i++)
        {
            var p = MetaWant[i, 0];
            if (!File.Exists(p)) { Check(false, $"{p}：不存在"); continue; }
            string t = File.ReadAllText(p);
            if (MetaWant[i, 1] != null)
                Check(MetaField(t, "wrapU") == MetaWant[i, 1] && MetaField(t, "wrapV") == MetaWant[i, 2],
                      $"{Path.GetFileName(p)}：wrap = ({MetaWant[i, 1]},{MetaWant[i, 2]})");
            else
                Check(MetaField(t, "aniso") == MetaWant[i, 3], $"{Path.GetFileName(p)}：aniso = {MetaWant[i, 3]}");
        }
    }

    static string MetaField(string txt, string key)
    {
        var m = Regex.Match(txt, "^    " + key + ":\\s*(\\S+?)\\r?$", RegexOptions.Multiline);
        return m.Success ? m.Groups[1].Value : null;
    }
}
