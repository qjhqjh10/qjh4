// B142_E33_Assert.cs — 执行写手 `B142-E33`（第一百二十九会话 · 2026-11-04）
//   ★ 补的是 **`B141-W1`（`B141_W130_ArtBaker` 三处修复）的断言缺口** —— 现有 `B111_R2W_Assert`
//     只盯 `Noise Combined` 的 wrap + 白名单（`B-19`），**不覆盖**这三处；`B139_H_Assert` 只覆盖了
//     `OriginalWrap` 的三档（含 `vfx/Shine trail` (0,1)），**也不覆盖** `textureType` 与压缩档真值表。
//
// ============================ 它盯的是什么（逐条对判据）============================
//  **修复一（表头认列）**：`ArtBaker.LoadArtTruth` 原用固定偏移 `6/7/8/9` 读两张列序不同的 TSV
//    ⇒ vfx 行被读成 `U←wrapV · V←filterMode · filter←aniso · aniso←npotScale`。
//    现在按表头取下标 ⇒ `Shine trail` 读出 **(0,1)**（U Repeat / V Clamp 的**混合档**）、
//    `Noise Combined` 读出 **(0,0)**。
//    判据 = `报告/B141_W130_ArtBaker.md` §1 · `判据/B141_W130_对账_资源.md` §四·1 ·
//           原版 `m_TextureSettings.m_WrapU/V`（`数据/游戏数据/vfx_texture_mips.tsv:2428` = `0 1` / `:2021` = `0 0`）。
//  **修复二（下划线 → 空格命名）**：`OriginalNameCandidates` 补一档「下划线 → 空格」⇒
//    `boosterpack/Card_Booster_Pack_Normal.png` 命中原版名 `Card Booster Pack Normal`
//    ⇒ `OriginalTextureType` **保留现值 `NormalMap`(1)**（改前返 `Default`(0) ⇒ 下次烘焙静默打回）。
//    判据 = `报告/B141_W130_ArtBaker.md` §2 · 盘上 `.meta:57 textureType: 1` ·
//           `WarpforgeArena1/Editor/B139_H_Assert.cs` 的三步口径（⛔ 别直接写 NormalMap，只「保留现值」）。
//  **修复三（15-10 压缩档真值表）**：`CompTruths` 现读 **144 条**（其中 **70 条 `crunched=true`**）；
//    15-10 那 8 个原版名（rgba32 × 5 · dxt1 × 3）必须逐条 = 原版档。
//    判据 = `报告/B141_W130_ArtBaker.md` §3 · `判据/B141_W130_对账_资源.md` §一 15-10 ·
//           原版 `Texture2D.m_TextureFormat`（4 = RGBA32 · 10 = DXT1）。
//  **文档订正（`B142-ARTBAKER-DOC`）**：`ArtBaker.cs:531`/`:635`/`:648` 三条「表内 8 条 / 只影响 1 张 /
//    都是 false ⇒ 无行为变化」已不成立 ⇒ 源码里必须**归零**，且注释里的数字必须 = 表里的真实数字。
//
// ============================ 分组 ============================
//  A · wrap 三档（`.meta` 现值 + `ArtBaker.OriginalWrap` 生产路径 + TSV 表头结构）
//  B · `OriginalTextureType` == 1（下划线命名 + `.meta` 现值 + 反例）
//  C · `CompTruths` 144 条 / 70 条 crunched + 15-10 那 8 名逐条 + 反例 + 盘上已应用的 UI 切片
//  D · 三条过期注释归零 + 注释数字 == 表内数字
//  E · 灭自证（解析器 / 陈旧检测器都能给出「非 0 / 非默认」）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · `LoadArtTruth` 改回固定偏移 `6/7/8/9` ⇒ **A5/A8/A9 红**（`Shine trail` 读成 (1,1)、表头下标对不上）。
//  · `RepeatTextures` / 真值表里删掉 `Shine trail` 那条 ⇒ A6/A7/A9 红。
//  · `OriginalNameCandidates` 删掉「下划线 → 空格」那一档 ⇒ **B2 红**（回 `Default`）。
//  · `CompTruths` 删掉/改档任一条 ⇒ C3–C10 / C11–C18 对应条红；条数变化 ⇒ C1/C2 红。
//  · 把三条注释写回旧说法 ⇒ **D1–D3 红**；注释数字与表脱节 ⇒ D4 红。
//  · 盘上 `.meta` 被某次烘焙打回（`textureType` 0 / `textureFormat` 2 = BC7）⇒ A1/A2/A3/A4/B1/C20/C21 红。
//
// ============================ 怎么跑 / 怎么接（⚠️ 本代理不动宿主）============================
//   单独跑：
//     unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//       -projectPath "D:\4\Unity\MyGame" -executeMethod B142_E33_Assert.Run -logFile -
//   接宿主（**零前提**：只读文件 + 调纯静态函数 + 反射，不建对象、不改状态、不烘焙）：
//     同一物理行末尾追加（行数零变化）：
//       B142_E33_Assert.Run(); pass += B142_E33_Assert.Pass; fail += B142_E33_Assert.Fail;
//   ⚠️ 追加前先看那一行已有的 `//` 在哪（本仓踩过 6 次「代码落进行末注释」）。
//   判绿红看 `B142E33 === 合计` 那一行，⛔ 不看退出码。

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>写手 `B142-E33`：`B141-W1`（ArtBaker 表头认列 · 下划线命名 · 15-10 压缩档）的回归断言 + 三条注释订正的守卫。</summary>
public static class B142_E33_Assert
{
    const string Tag = "B142E33";
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Eq<T>(T got, T want, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(got, want);
        Check(ok, ok ? msg : msg + $" —— 期望 [{want}]，实得 [{got}]");
    }

    static void Section(string t) { Debug.Log(Tag + " --- " + t + " ---"); }

    // ============================================================ 常量
    const string ArtBakerSrc = "CardPresentation/Editor/ArtBaker.cs";
    const string MetaShineVfx = "CardPresentation/Resources/Art/vfx/Shine trail.png.meta";
    const string MetaShineUi = "CardPresentation/Resources/Art/ui_menu/Shine_trail.png.meta";
    const string MetaNoiseUi = "CardPresentation/Resources/Art/ui_menu/Noise_Combined.png.meta";
    const string MetaNormal = "CardPresentation/Resources/Art/boosterpack/Card_Booster_Pack_Normal.png.meta";
    const string MetaAbaddon = "CardPresentation/Resources/Art/ui_menu/UI_Deck_Warlord_Abaddon.png.meta";
    const string MetaWhiteSq = "CardPresentation/Resources/Art/ui/White_Square.png.meta";

    const string ShineVfxAsset = "Assets/" + "CardPresentation/Resources/Art/vfx/Shine trail.png";
    const string ShineUiAsset = "Assets/" + "CardPresentation/Resources/Art/ui_menu/Shine_trail.png";
    const string NoiseUiAsset = "Assets/" + "CardPresentation/Resources/Art/ui_menu/Noise_Combined.png";
    const string NormalAsset = "Assets/" + "CardPresentation/Resources/Art/boosterpack/Card_Booster_Pack_Normal.png";

    /// <summary>`CompTruths` 现读条数 / 其中 `crunched=true` 的条数（`B142-ARTBAKER-DOC` 的判据就是这个数）。
    /// ⚠️ 表扩了就要**同刻**改这里 + `ArtBaker.cs:531/:635/:648` 三条注释，否则 D 组红。</summary>
    const int CompTruthsCount = 144;
    const int CompTruthsCrunched = 70;

    /// <summary>15-10 的 8 个原版名（`报告/B141_W130_ArtBaker.md` §3）。</summary>
    struct Expect
    {
        public string Name;
        public string Compression;   // TextureImporterCompression 的枚举名
        public int Format;           // -1 = 不指定显式格式
        public bool Crunched;
    }

    static Expect[] Eight()
    {
        int dxt1 = (int)TextureImporterFormat.DXT1;
        return new[]
        {
            new Expect { Name = "Noise Combined",               Compression = "Uncompressed", Format = -1,   Crunched = false },
            new Expect { Name = "NoiseNRM",                     Compression = "Uncompressed", Format = -1,   Crunched = false },
            new Expect { Name = "Default-Particle",             Compression = "Uncompressed", Format = -1,   Crunched = false },
            new Expect { Name = "MuzzleFlash_Side",             Compression = "Uncompressed", Format = -1,   Crunched = false },
            new Expect { Name = "Matcap Gold Emperos Children", Compression = "Uncompressed", Format = -1,   Crunched = false },
            new Expect { Name = "CandleFlame",                  Compression = "Compressed",   Format = dxt1, Crunched = false },
            new Expect { Name = "Flame01",                      Compression = "Compressed",   Format = dxt1, Crunched = false },
            new Expect { Name = "MatCap Polish",                Compression = "Compressed",   Format = dxt1, Crunched = false },
        };
    }

    // ============================================================ 入口
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("B142-E33 · B141-W1 三处修复断言（wrap 三档 / textureType / 15-10 压缩档）+ 三条注释订正守卫");

        WrapGroup();
        TextureTypeGroup();
        CompressionGroup();
        DocGroup();
        NegativeControls();

        Debug.Log(Tag + $" === 合计：{_pass} 通过 / {_fail} 失败 ===");
    }

    // ------------------------------------------------------------ A · wrap 三档
    static void WrapGroup()
    {
        Section("A · wrap 三档：`Shine trail` (0,1) 混合档 · `Noise Combined` (0,0)（判据 = 报告/B141_W130_ArtBaker.md §1）");

        string shine = ReadText(MetaShineVfx);
        string noise = ReadText(MetaNoiseUi);
        Check(!string.IsNullOrEmpty(shine), "A0 前提：`" + MetaShineVfx + "` 读得到且非空");
        Check(!string.IsNullOrEmpty(noise), "A0b 前提：`" + MetaNoiseUi + "` 读得到且非空");

        // 盘上现值（已被 B139_I / B141 改好；⛔ 这里断的是 `.meta`，不是「烘焙日志」）
        Eq(MetaInt(shine, "wrapU"), 0, "A1 `vfx/Shine trail.png.meta` wrapU == 0（U = Repeat）");
        Eq(MetaInt(shine, "wrapV"), 1, "A2 `vfx/Shine trail.png.meta` wrapV == 1（V = Clamp ⇒ **混合档 (0,1)**，不是 (1,1)）");
        Eq(MetaInt(noise, "wrapU"), 0, "A3 `ui_menu/Noise_Combined.png.meta` wrapU == 0（原版 Repeat）");
        Eq(MetaInt(noise, "wrapV"), 0, "A4 `ui_menu/Noise_Combined.png.meta` wrapV == 0（原版 Repeat）");

        // 生产路径：`ArtBaker.OriginalWrap` 现在必须给出混合档（改前固定偏移读成 (1,1)）
        int u, v;
        bool ok = ArtBaker.OriginalWrap(ShineVfxAsset, out u, out v);
        Check(ok && u == 0 && v == 1, "A5 `ArtBaker.OriginalWrap(vfx/Shine trail.png)` ⇒ (0,1) 混合档（改前固定偏移会读成 (1,1)）"
                                     + $"［实得 ok={ok} ({u},{v})］");

        // 下划线命名那一份：盘上 `.meta` 仍是 (1,1)（等下次烘焙），但**查表必须已经给出 (0,1)**
        ok = ArtBaker.OriginalWrap(ShineUiAsset, out u, out v);
        Check(ok && u == 0 && v == 1, "A6 `ArtBaker.OriginalWrap(ui_menu/Shine_trail.png)` ⇒ (0,1)"
                                     + "（下划线 → 空格那档命中 `Shine trail`；⛔ 不要求盘上 `.meta` 已经重导）"
                                     + $"［实得 ok={ok} ({u},{v})］");
        // 对照：盘上那份 `.meta` 的 U 侧**还可能是旧值**（等下次烘焙才 1 → 0）⇒ 这里只钉 V 侧，不钉 U，
        // 否则「正确重导」反而会把断言打红（同族坑：断言钉住一个本该变的状态）。
        string shineUi = ReadText(MetaShineUi);
        Eq(MetaInt(shineUi, "wrapV"), 1, "A6b `ui_menu/Shine_trail.png.meta` wrapV == 1（V 侧恒 Clamp；U 侧要等下次烘焙才从旧值 1 → 0）");
        Debug.Log(Tag + "   · 提示：`ui_menu/Shine_trail.png.meta` 盘上 wrapU = " + MetaInt(shineUi, "wrapU")
                       + "（1 = 尚未重导、0 = 已重导 —— 两者都正常，所以这条不钉 U）");

        ok = ArtBaker.OriginalWrap(NoiseUiAsset, out u, out v);
        Check(ok && u == 0 && v == 0, "A7 `ArtBaker.OriginalWrap(ui_menu/Noise_Combined.png)` ⇒ (0,0)" + $"［实得 ok={ok} ({u},{v})］");

        // 结构：两张 TSV 的 `wrapU` 列下标**必须不同**（= 固定偏移那一类 bug 的根因）
        string[] vfxHead = TsvHeader("vfx_texture_mips.tsv");
        string[] arenaHead = TsvHeader("arena_texture_import.tsv");
        Check(vfxHead != null && arenaHead != null, "A8 前提：两张真值表表头都读得到（vfx_texture_mips / arena_texture_import）");
        int iWuVfx = HeaderIndex(vfxHead, "wrapU"), iWvVfx = HeaderIndex(vfxHead, "wrapV");
        int iWuArena = HeaderIndex(arenaHead, "wrapU");
        Eq(iWuVfx, 5, "A9 vfx 表 `wrapU` 在第 5 列（旧固定偏移读的是第 6 列 = `wrapV`）");
        Eq(iWuArena, 6, "A10 arena 表 `wrapU` 在第 6 列（多一列 `colorSpace`）");
        Check(iWuVfx >= 0 && iWuArena >= 0 && iWuVfx != iWuArena,
              $"A11 两张表 `wrapU` 下标**确实不同**（{iWuVfx} vs {iWuArena}）⇒ 「一张索引套两张表」必然读错，必须按表头认列");

        string[] rowShine = TsvRow("vfx_texture_mips.tsv", "Shine trail");
        Check(rowShine != null, "A12 前提：vfx 表里有 `Shine trail` 行");
        Eq(TsvInt(rowShine, iWuVfx), 0, "A13 `vfx_texture_mips.tsv` 的 `Shine trail` 行 wrapU == 0");
        Eq(TsvInt(rowShine, iWvVfx), 1, "A14 `vfx_texture_mips.tsv` 的 `Shine trail` 行 wrapV == 1（原版混合档的出处）");

        string[] rowNoise = TsvRow("vfx_texture_mips.tsv", "Noise Combined");
        Check(rowNoise != null, "A15 前提：vfx 表里有 `Noise Combined` 行");
        Eq(TsvInt(rowNoise, iWuVfx), 0, "A16 `Noise Combined` 行 wrapU == 0");
        Eq(TsvInt(rowNoise, iWvVfx), 0, "A17 `Noise Combined` 行 wrapV == 0");
    }

    // ------------------------------------------------------------ B · OriginalTextureType（下划线命名）
    static void TextureTypeGroup()
    {
        Section("B · 下划线命名能对回原版名 ⇒ `OriginalTextureType` 保留 `NormalMap`(1)（判据 = 报告/B141_W130_ArtBaker.md §2）");

        string meta = ReadText(MetaNormal);
        Check(!string.IsNullOrEmpty(meta), "B0 前提：`" + MetaNormal + "` 读得到且非空");
        Eq(MetaInt(meta, "textureType"), 1, "B1 `boosterpack/Card_Booster_Pack_Normal.png.meta` textureType == 1（盘上现值）");
        Eq((int)ArtBaker.OriginalTextureType(NormalAsset, TextureImporterType.NormalMap), 1,
           "B2 `ArtBaker.OriginalTextureType(Card_Booster_Pack_Normal, NormalMap)` ⇒ 1（保留现值；改前返 Default(0)）");
        Eq((int)ArtBaker.OriginalTextureType(NoiseUiAsset, TextureImporterType.NormalMap), 0,
           "B3 反例：`Noise_Combined` **不是**法线名 ⇒ 仍返 `Default`(0)（挡「干脆全部保留现值」这种改法）");
    }

    // ------------------------------------------------------------ C · 压缩档真值表
    static void CompressionGroup()
    {
        Section("C · `CompTruths` 144 条 / 70 条 crunched + 15-10 那 8 名逐条（判据 = 报告/B141_W130_ArtBaker.md §3）");

        IDictionary table = CompTruths();
        Check(table != null, "C0 前提：反射拿到 `ArtBaker.CompTruths`（私有的原版名 → 压缩档表）");
        if (table == null)
        {
            Check(false, "C0b `CompTruths` 取不到 ⇒ C1–C18 无法判定（⛔ 不静默跳过）");
            return;
        }

        Eq(table.Count, CompTruthsCount,
           $"C1 `CompTruths` 条数 == {CompTruthsCount}（`B142-ARTBAKER-DOC` 的判据；表扩了要同刻改注释 + 本断言）");
        Eq(CountCrunched(table), CompTruthsCrunched, $"C2 `crunched=true` 的条数 == {CompTruthsCrunched}（旧注释「8 条都是 false」就是错在这里）");

        var eight = Eight();
        foreach (var e in eight)
        {
            bool has = table.Contains(e.Name);
            string comp = has ? Field(TableValue(table, e.Name), "Compression") : "<缺>";
            int fmt = has ? ToInt(Field(TableValue(table, e.Name), "ExplicitFormat")) : -2;
            bool cr = has && string.Equals(Field(TableValue(table, e.Name), "Crunched"), "True", StringComparison.OrdinalIgnoreCase);
            Check(has && comp == e.Compression && fmt == e.Format && cr == e.Crunched,
                  $"C3[{e.Name}] `CompTruths[\"{e.Name}\"]` ⇒ {e.Compression} / format={e.Format} / crunched={e.Crunched}"
                  + $"［实得 has={has} {comp} / {fmt} / {cr}］");
        }

        // 查表生产路径（含 `CanonName` 那一档）：用战场真实路径喂 `OriginalCompression`
        foreach (var e in eight)
        {
            string p = "Assets/WarpforgeArena1/arenas/battlearena1/Textures/" + e.Name + ".png";
            object t = OriginalCompression(p);
            string comp = t == null ? "<null>" : Field(t, "Compression");
            int fmt = t == null ? -2 : ToInt(Field(t, "ExplicitFormat"));
            bool cr = t != null && string.Equals(Field(t, "Crunched"), "True", StringComparison.OrdinalIgnoreCase);
            Check(t != null && comp == e.Compression && fmt == e.Format && cr == e.Crunched,
                  $"C4[{e.Name}] `ArtBaker.OriginalCompression(arena/…/{e.Name}.png)` ⇒ {e.Compression} / format={e.Format} / crunched={e.Crunched}"
                  + $"［实得 {comp} / {fmt} / {cr}］");
        }

        // 反例：原版 BC7 的名字**不入表** ⇒ 必须回落到「表外 = CompressedHQ / -1」原口径
        object outs = OriginalCompression("Assets/WarpforgeArena1/arenas/battlearena1/Textures/ExplosionEmission.png");
        Check(outs != null && Field(outs, "Compression") == "CompressedHQ" && ToInt(Field(outs, "ExplicitFormat")) == -1,
              "C5 反例：`ExplosionEmission`（原版 BC7）**不在**表内 ⇒ 回 `CompressedHQ` / format=-1（挡「全都塞进表」）"
              + $"［实得 {(outs == null ? "<null>" : Field(outs, "Compression"))}］");

        // 盘上已应用的那批 UI 切片（15-09 · W40 已按原版改过 `.meta`）—— 防下一次烘焙静默打回
        string abaddon = ReadText(MetaAbaddon);
        Check(!string.IsNullOrEmpty(abaddon), "C6 前提：`" + MetaAbaddon + "` 读得到且非空");
        Eq(StandaloneInt(abaddon, "textureFormat"), 12, "C7 `UI_Deck_Warlord_Abaddon`（原版 DXT5Crunched）Standalone textureFormat == 12");
        Eq(StandaloneInt(abaddon, "crunchedCompression"), 1, "C8 同上 crunchedCompression == 1（`*Crunched` 必须带 Crunch 位）");

        string white = ReadText(MetaWhiteSq);
        Check(!string.IsNullOrEmpty(white), "C9 前提：`" + MetaWhiteSq + "` 读得到且非空");
        Eq(StandaloneInt(white, "textureFormat"), 10, "C10 `ui/White_Square`（原版 DXT1）Standalone textureFormat == 10");
        Eq(StandaloneInt(white, "crunchedCompression"), 0, "C11 同上 crunchedCompression == 0（非 *Crunched 不许带 Crunch）");
    }

    // ------------------------------------------------------------ D · 注释订正守卫（B142-ARTBAKER-DOC）
    static void DocGroup()
    {
        Section("D · `ArtBaker.cs:531/:635/:648` 三条过期注释归零，且注释数字 == 表内数字");

        string src = ReadText(ArtBakerSrc);
        Check(!string.IsNullOrEmpty(src), "D0 前提：`" + ArtBakerSrc + "` 读得到且非空");
        if (string.IsNullOrEmpty(src)) return;

        Eq(CountOf(src, "都是 false ⇒ 无行为变化"), 0,
           "D1 旧说法「（现表内 8 条都是）false ⇒ 无行为变化」== 0（现读 70 条 crunched=true）");
        Eq(CountOf(src, "`CompTruths`，8 个名字"), 0,
           "D2 旧说法「表内（`CompTruths`，8 个名字）」== 0（现读 144 条）");
        Eq(CountOf(src, "本方法现读只影响 1 张"), 0,
           "D3 旧说法「本方法现读只影响 1 张」== 0（15-09 并入后 `Resources/Art` 的 UI 切片也走真值表）");

        Check(src.IndexOf("现读 144 条", StringComparison.Ordinal) >= 0,
              "D4 正向标记「现读 144 条」在位（挡「把整段注释删掉也算过」）");
        Check(src.IndexOf("70 条 crunched=true", StringComparison.Ordinal) >= 0,
              "D5 正向标记「70 条 crunched=true」在位");
        Check(src.IndexOf("144 条里 70 条 true", StringComparison.Ordinal) >= 0,
              "D6 正向标记「144 条里 70 条 true」在位（`:648` 的平台块那句）");

        // 注释里的数字必须 **= 表里的真实数字**（双向：不是只信注释写了 144）
        IDictionary table = CompTruths();
        if (table != null)
        {
            Check(table.Count == CompTruthsCount && CountCrunched(table) == CompTruthsCrunched,
                  $"D7 注释写的 144 / 70 == 表内真实 {table.Count} / {CountCrunched(table)}（三处同刻改，⛔ 别各写各的）");
        }
        else Check(false, "D7 `CompTruths` 取不到 ⇒ 注释数字无法与表对账（⛔ 不静默跳过）");
    }

    // ------------------------------------------------------------ E · 灭自证
    static void NegativeControls()
    {
        Section("E · 反自证：解析器 / 陈旧检测器都能给出「非默认」");

        Eq(MetaInt("  textureSettings:\n    wrapU: 1\n", "wrapU"), 1,
           "E1 `MetaInt` 对「wrapU: 1」的合成文本读出 1（不是恒返 0）");
        Eq(MetaInt("  textureSettings:\n    wrapV: 0\n", "wrapU"), -1,
           "E2 `MetaInt` 找不到该键时返回 -1（⛔ 不是 0 ⇒ 挡「把那一行删掉也算过」）");
        Eq(CountOf("foo // 现表内 8 条都是 false ⇒ 无行为变化", "都是 false ⇒ 无行为变化"), 1,
           "E3 `CountOf` 对合成文本能数出 1（挡「陈旧检测器恒返 0」）");
        Eq(HeaderIndex(new[] { "name", "width", "wrapU", "wrapV" }, "wrapV"), 3,
           "E4 `HeaderIndex` 按名取下标（不是按固定偏移）");
    }

    // ============================================================ 反射 / 读盘
    static IDictionary CompTruths()
    {
        try
        {
            var f = typeof(ArtBaker).GetField("CompTruths", BindingFlags.NonPublic | BindingFlags.Static);
            return f == null ? null : f.GetValue(null) as IDictionary;
        }
        catch (Exception e) { Debug.LogWarning(Tag + " 反射取 `CompTruths` 失败：" + e.Message); return null; }
    }

    static object TableValue(IDictionary d, string key) { try { return d[key]; } catch (Exception) { return null; } }

    static object OriginalCompression(string path)
    {
        try
        {
            var m = typeof(ArtBaker).GetMethod("OriginalCompression", BindingFlags.NonPublic | BindingFlags.Static);
            return m == null ? null : m.Invoke(null, new object[] { path });
        }
        catch (Exception e) { Debug.LogWarning(Tag + " 反射调 `OriginalCompression` 失败：" + e.Message); return null; }
    }

    /// <summary>读私有结构体 `ArtCompressionTruth` 的公有字段（枚举返名字、整数返字符串）。</summary>
    static string Field(object truth, string name)
    {
        if (truth == null) return "<null>";
        try
        {
            var f = truth.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (f == null) return "<缺字段>";
            object v = f.GetValue(truth);
            return v == null ? "<null>" : v.ToString();
        }
        catch (Exception e) { return "<" + e.Message + ">"; }
    }

    static int ToInt(string s) { int v; return int.TryParse(s, out v) ? v : -2; }

    static int CountCrunched(IDictionary d)
    {
        int n = 0;
        foreach (DictionaryEntry kv in d)
            if (string.Equals(Field(kv.Value, "Crunched"), "True", StringComparison.OrdinalIgnoreCase)) n++;
        return n;
    }

    static string ReadText(string assetsRel)
    {
        string p = Path.Combine(Application.dataPath, assetsRel);
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }

    static string DataFile(string name)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../../数据/游戏数据/" + name));
    }

    static string[] TsvHeader(string name)
    {
        string p = DataFile(name);
        if (!File.Exists(p)) { Debug.LogWarning(Tag + " 读不到真值表：" + p); return null; }
        using (var r = new StreamReader(p, System.Text.Encoding.UTF8))
        {
            string line = r.ReadLine();
            return line == null ? null : line.Split('\t');
        }
    }

    static string[] TsvRow(string name, string rowName)
    {
        string p = DataFile(name);
        if (!File.Exists(p)) return null;
        foreach (var line in File.ReadAllLines(p))
        {
            var c = line.Split('\t');
            if (c.Length > 0 && c[0] == rowName) return c;
        }
        return null;
    }

    static int TsvInt(string[] row, int i)
    {
        if (row == null || i < 0 || i >= row.Length) return -1;
        int v;
        return int.TryParse(row[i], out v) ? v : -1;
    }

    static int HeaderIndex(string[] header, string name)
    {
        if (header == null) return -1;
        for (int i = 0; i < header.Length; i++)
            if (string.Equals(header[i].Trim(), name, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    /// <summary>从 `.meta` 文本抠 `key: N`（找不到 ⇒ -1；⛔ 不是 0）。</summary>
    static int MetaInt(string meta, string key)
    {
        if (string.IsNullOrEmpty(meta) || string.IsNullOrEmpty(key)) return -1;
        var m = Regex.Match(meta, @"(?m)^[ \t]*" + Regex.Escape(key) + @":[ \t]*(-?\d+)[ \t]*\r?$");
        return m.Success ? int.Parse(m.Groups[1].Value) : -1;
    }

    /// <summary>只在 `buildTarget: Standalone` 那一块里抠（`.meta` 里 Default 块与 Standalone 块各有一份同名键）。</summary>
    static int StandaloneInt(string meta, string key)
    {
        if (string.IsNullOrEmpty(meta)) return -1;
        int i = meta.IndexOf("buildTarget: Standalone", StringComparison.Ordinal);
        if (i < 0) return -1;
        int j = meta.IndexOf("- serializedVersion:", i + 1, StringComparison.Ordinal);
        string seg = j > i ? meta.Substring(i, j - i) : meta.Substring(i);
        return MetaInt(seg, key);
    }

    static int CountOf(string text, string needle)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = text.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }
}
