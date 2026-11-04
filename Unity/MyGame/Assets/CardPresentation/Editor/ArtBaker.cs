// ArtBaker.cs — 把「原版复刻」要用的美术烘进 Resources/Art
//
// 两件事：
//   1. **烘战场背景**：打开 `WarpforgeArena1/Scenes/battlearena1.unity`（原版 battlearena1 重建场景），
//      用它的相机渲一张 1920×1080 存成 `Resources/Art/arena1_bg.png`。
//      批处理下粒子不会自己走，渲之前先 `Simulate` 几秒，否则烟/火全是空的。
//   2. **统一导入设置**：卡框要 Read/Write（`CardView` 运行时量它的不透明包围盒来对齐 UV）、
//      mip / wrap / filter / aniso **按原版真值表逐张设**（`数据/游戏数据/vfx_texture_mips.tsv` + `arena_texture_import.tsv`；表外保留现值）、开 alphaIsTransparency。⚠️ 2026-11-04 更正：原来这里写「全部关 mipmap」—— 那是**无条件覆盖**，会把按原版改好的 `.meta` 静默打回（`B139_H` / `B139-I2`）。
//
// 用法（菜单）：Tools > CardPresentation > 烘焙原版美术
// 用法（命令行）： -executeMethod ArtBaker.BakeFromCLI
//
// ⚠️ **这一段是「原版复刻」用的参考美术**（Everguild / Games Workshop 的资产）。
//    🔴 **2026-09-22 更正**：原来这里写「发布前整个 `Resources/Art/` 目录删掉或换成自己的图」——
//       **那条口径已作废**（用户 2026-09-18「取消掉什么版权红线，这是个人学习使用的」⇒ `CLAUDE.md` §四）。
//    兜底机制仍在（和版权无关）：`CardArt.Available` 判空 —— 删掉整个目录游戏照样能跑，退回占位卡面 + 纯色背景。
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ArtBaker
{
    const string P = "ART ";

    public const string ArtDir = "Assets/CardPresentation/Resources/Art";
    public static readonly string ArenaScene = ArenaBuilder.ScenePath(ArenaBuilder.DefaultArena);
    public const string BackdropPng = ArtDir + "/arena1_bg.png";

    const int W = 1920, H = 1080;

    /// <summary>
    /// 🔴 **2026-10-22：`Resources/Art` 下【原版是 Repeat】的贴图白名单**（不在表里的一律 Clamp）。
    ///
    /// **为什么要有这张表**：`ApplyImportSettings()` 原来对 `Resources/Art` 下所有 Texture2D
    /// **无条件** `ti.wrapMode = TextureWrapMode.Clamp` ⇒ 手工把下面这几张改成 Repeat 之后，
    /// **下一次美术烘焙（`BakeAll` / `BakeFromCLI` 都会调本方法）就会把它们静默改回 Clamp** ——
    /// 没有报错、没有日志，只有画面上的接缝不对（亮弧滚不过接缝、纹理在边缘被拉成一条线）。
    ///
    /// **判据 = 原版 bundle 实读**（UnityPy 直读 `Texture2D.m_TextureSettings`，`m_WrapU/V = 0` 即 Repeat）：
    /// | 原版贴图名 | 我们的文件 | 原版 m_WrapU/V |
    /// |---|---|---|
    /// | `Spark UI`（128²）            | `CanAct_SparkUI.png`     | 0 / 0 |
    /// | `Circle_Hoop`（256²）         | `CanAct_CircleHoop.png`  | 0 / 0 |
    /// | `WF 3D Card_Card 3D_BaseColor`| `Card3D_BaseColor.png`   | 0 / 0 |
    /// | `MatCap Card Level 1`         | `MatCap_Card_Level1.png` | 0 / 0 |
    /// | `Matcap Bronze`               | `MatCap_Bronze.png`      | 0 / 0 |
    /// | `Matcap Silver`               | `MatCap_Silver.png`      | 0 / 0 |
    /// | `Matcap Gold`                 | `MatCap_Gold.png`        | 0 / 0 |
    /// 出处 → `资料/普查产出_1022/W63_场上卡牌与底座绿光_调查.md` §2（代理 R 现核）。
    ///
    /// ⛔ **不要改成「全部 Repeat」** —— 同目录的 `CardBlobShadow.png`（原版 `Card blob shadow`）
    ///    原版实读就是 **Clamp(1,1)**，改成 Repeat 反而与原版不符。
    /// ⚠️ 原版数据只给了 `m_WrapU/V`，**`m_WrapW` 不在判据里** ⇒ 代码只写 U/V（`.meta` 的 `wrapW` 保持现值）。🔴 2026-11-01（`B111_T1C`）：`CrosshairTrail` / `LightningTrail` / `Laser_Wave_2` 三张的**原版 `.meta` 三个数都是 `0`**，已补进下表、并把它们的 `.meta` `wrapW` 也改成 0。🔴 2026-11-04（`B139_H` · `B139-I3`）：**本表只是 `(0,0)` 那一档**；三档（`0,0` Repeat / `1,1` Clamp / `0,1` 混合）由 `OriginalWrap` 从 `vfx_texture_mips.tsv` **逐张查原版 `m_WrapU/V`**，`Shine trail` 的 `(0,1)` 就是靠它表达的（布尔白名单表达不了 U≠V）。
    /// </summary>
    static readonly string[] RepeatTextures =
    {
        "CanAct_SparkUI.png",       // 原版 `Spark UI`      —— 底座绿光的 UV 滚动环（`Sparks UI Additive Scroll`）
        "CanAct_CircleHoop.png",    // 原版 `Circle_Hoop`   —— `CanActParticles` 的粒子贴图
        "Card3D_BaseColor.png",     // 原版 `WF 3D Card_Card 3D_BaseColor`
        "MatCap_Card_Level1.png",   // 原版 `MatCap Card Level 1`
        "MatCap_Bronze.png",        // 原版 `Matcap Bronze`
        "MatCap_Silver.png",        // 原版 `Matcap Silver`
        "MatCap_Gold.png",          // 原版 `Matcap Gold`
        "Noise_Combined.png",       // 原版 `Noise Combined`（128², RGBA32）—— wrapU/V/W = 0/0/0（UnityPy 直读原版 bundle + AssetRipper 导出的原版 importer meta，双证；判据 → 资料/完全复刻/判据/B111_R2_判据缺口两处.md §1）
        "cardback_noise.png",       // 上面那张的**另一份拷贝**（`card_sdf/`；卡框/卡背 SDF + 卡框溶解 + 3D 卡牌溶解都用它，同一个原版 AssetReference）
        "CrosshairTrail.png", "LightningTrail.png", "Laser_Wave_2.png" };   // 🆕 2026-11-01（B111_T1C）：这三张原版 `.meta` 的 `wrapU/V/W` 都是 **0/0/0**(Repeat)——判据 → `资料/完全复刻/报告/B111_T1C.md`。⚠️ `40k_popup_texture.png` **故意不加**（原版也是 Repeat，但我们走 `ImageQuad.CreateTiled` 多 quad、不依赖 wrap，见 `B111_T1B` §七·4）

    /// <summary>本路径是否命中 Repeat 白名单（按文件名比、忽略大小写）。</summary>
    static bool IsRepeatTexture(string path)
    {
        var name = Path.GetFileName(path);
        foreach (var n in RepeatTextures)
            if (string.Equals(n, name, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    [MenuItem("Tools/CardPresentation/烘焙原版美术")]
    public static void BakeAll()
    {
        BakeBackdrop();
        ApplyImportSettings();
        AssetDatabase.Refresh();
        Debug.Log(P + "完成：背景 + 导入设置");
    }

    public static void BakeFromCLI() { BakeAll(); }

    /// <summary>渲染 battlearena1 场景 → arena1_bg.png</summary>
    [MenuItem("Tools/CardPresentation/只烘背景")]
    public static void BakeBackdrop()
    {
        if (!File.Exists(ArenaScene))
        {
            Debug.LogError(P + $"找不到战场场景：{ArenaScene}（先跑 ArenaBuilder.BuildFromCLI）");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ArenaScene);
        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null) { Debug.LogError(P + "战场场景里没有相机"); return; }

        // 批处理下没有帧循环，粒子不会自己推进 —— 不 Simulate 的话烟/火/蒸汽全是空的
        int n = 0;
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            ps.Simulate(4f, withChildren: true, restart: true, fixedTimeStep: false);
            n++;
        }

        cam.aspect = (float)W / H;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = prev;
        rt.Release();

        Directory.CreateDirectory(ArtDir);
        File.WriteAllBytes(BackdropPng, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        Debug.Log(P + $"背景已烘：{BackdropPng}（{W}×{H}，推进了 {n} 个粒子系统，场景 {scene.name}）");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 音频导入设置（2026-09-17 加；2026-10-19 `T06` 从「只盖 vo」扩到全部四类）
    //
    // 判据（**原版直证**）：`资料/普查产出_1019/W_工具线_T06音频导入_1019b.md` §二 ——
    //   读的是原版**打包后 `AudioClip` 的序列化字段**（`m_LoadType` / `m_CompressionFormat` /
    //   `m_PreloadAudioData` / `m_LoadInBackground`），也就是导入设置落地后在运行时真正生效的值
    //   （原版工程侧的 `AudioImporter` 不进包：`assets_full` 里 `AudioImporter` 目录 = 0 个）。
    //   我们这边 2434 条**逐条按名字对上了原版 clip**（vo 1892/1892 · sfx 515/515 · bgm 25/25；
    //   music 两条按秒长核对）⇒ 规则表 = **目录 + 扩展名 + 例外名单**，⛔ 不是逐文件硬编码。
    // ⚠️ 判不出的项（不在 `AudioClip` 序列化里）：`quality`（现状 1.0，**不动**）·
    //   `normalize` · `conversionMode` —— 见报告 §2·3 / §2·4，⛔ 别拿猜测填。
    // ─────────────────────────────────────────────────────────────────────────
    const string AudioRoot = ArtDir + "/audio";

    // `sfx/` 里原版是 **Streaming + Vorbis** 的条目（其余 sfx 一律 CompressedInMemory + ADPCM）。
    // 判据 = 原版 `soundcollection_assets_all` 包：`m_LoadType == 2` 的**全部 11 条**（报告 §2·2）。
    // 我们 `sfx/` 现在有 6 条（`MatchVictory` / `MatchDefeat` / `MatchDraw` / `Draft Mode GainSkull` /
    // `StartMatch` / `OvertimeStart`）；另 5 条原版有、我们还没导 —— **一并列进来**，
    // 免得以后补导时又漏（`Progress Jingle` / `Draft Mode Loss Jingle` 是 2026-10-19 现读补进报告的
    // 两条：原报告 §2·2 的「11 条」只列了 9 个名字，现核原版是 7 条 preload=true + 4 条 false）。
    static readonly string[] SfxStreaming = {
        "MatchVictory", "MatchDefeat", "MatchDraw", "Draft Mode GainSkull", "OvertimeStart",
        "Progress Jingle", "Draft Mode Loss Jingle",
        "StartMatch", "Vault Crate Open Full",
        "Ork expansion pop up jingle", "Aeldari Reinforcement drop 2 jingle",
    };

    // 上面那 11 条里 `m_PreloadAudioData == false` 的 4 条（其余 7 条都是 true）。
    static readonly string[] SfxStreamingNoPreload = {
        "StartMatch", "Vault Crate Open Full",
        "Ork expansion pop up jingle", "Aeldari Reinforcement drop 2 jingle",
    };

    // `music/` 两条（原版 `menumusic_assets_all`）：都是 Streaming + Vorbis，预载一真一假。
    static readonly string[] MusicPreload = { "main_theme" };            // 原版 `Main Theme`
    static readonly string[] MusicNoPreload = { "menu_idle_theme" };     // 原版 `Menu Idle Theme`

    // `bgm/` 里原版 `m_PreloadAudioData == true` 的 **15 条** = 这 5 个阵营 × High/Medium/Low
    // （原版包 `battlearena2`(Orks) / `battlearena3`(Necrons) / `battlearenaaeldari` /
    //   `battlearenablacklegion` / `battlearenaleviathan`(Tyranid)）；其余 10 条是 false。
    static readonly string[] BgmPreload = {
        "Warpforge Orks Combat High", "Warpforge Orks Combat Medium", "Warpforge Orks Combat Low",
        "Warpforge Necrons Combat High", "Warpforge Necrons Combat Medium", "Warpforge Necrons Combat Low",
        "Warpforge Aeldari Combat High", "Warpforge Aeldari Combat Medium", "Warpforge Aeldari Combat Low",
        "Warpforge Black Legion Combat High", "Warpforge Black Legion Combat Medium", "Warpforge Black Legion Combat Low",
        "Warpforge Tyranid Combat High", "Warpforge Tyranid Combat Medium", "Warpforge Tyranid Combat Low",
    };

    /// <summary>
    /// 给 `Resources/Art/audio/{vo,sfx,music,bgm}/` 定导入设置（2026-09-17 加，2026-10-19 扩面）。
    ///
    /// 为什么必须跑：这些音频默认是 `DecompressOnLoad` —— 一条条全解成 PCM 放在内存里
    /// （`bgm` 每条 ~120 s 的立体声解出来约 20 MB，25 条 ≈ 500 MB 峰值）。原版落地的是
    /// 下表那套，照抄即可：
    ///
    /// | 目录 | loadType | compressionFormat | preloadAudioData |
    /// |---|---|---|---|
    /// | `vo/*.ogg` | CompressedInMemory | Vorbis | false |
    /// | `vo/*.wav` | CompressedInMemory | **ADPCM** | false |
    /// | `sfx/*.wav`（默认） | CompressedInMemory | ADPCM | false |
    /// | `sfx/<SfxStreaming 名单>` | **Streaming** | **Vorbis** | 见 `SfxStreamingNoPreload` |
    /// | `music/main_theme` | Streaming | Vorbis | true |
    /// | `music/menu_idle_theme` | Streaming | Vorbis | false |
    /// | `bgm/*` | CompressedInMemory | Vorbis | `BgmPreload` 那 15 条 true，其余 false |
    ///
    /// 公共项（原版这 2434 条**全部一致**）：`loadInBackground = true`（我们原来**一条都没设**，
    /// 全是 0）· `forceToMono = false`（我们源文件的声道数逐类与原版 `m_Channels` 一致，
    /// 不靠它改声道）· `sampleRateSetting = PreserveSampleRate`（原版频率不统一，我们源文件头
    /// 与原版 `m_Frequency` 逐条吻合）。
    ///
    /// 🔴 **枚举走磁盘、不走 `FindAssets`**（`A1931`）：`AssetDatabase.FindAssets("t:AudioClip")`
    ///   会**静默漏**导入失败的条目（2026-10-19 实测 `vo/` 有 7 条磁盘上在、`FindAssets` 不返回）。
    ///   这里按目录枚举，拿不到 `AudioImporter` 的**当场报出来**（⛔ 不许静默）。
    ///   定因（是导入失败、还是只是没刷新）用下面的 `DiagnoseAudioImportCoverageCLI`，**默认 dry**。
    ///
    /// 生成音频本身：`工具/import_original_audio.py`（vo）· `工具/import_original_sfx.py` /
    /// `import_battle_sfx.py`（sfx）· `工具/import_battle_bgm.py`（bgm）。跑完再跑这个。
    /// </summary>
    [MenuItem("Tools/CardPresentation/重设音频导入设置")]
    public static void ApplyAudioImportSettings()
    {
        if (!Directory.Exists(AudioRoot))
        {
            Debug.LogWarning(P + $"没有 {AudioRoot} —— 先跑 工具/import_original_audio.py 等导入脚本");
            return;
        }

        int n = 0, alreadyOk = 0, noRule = 0, noImporter = 0;
        var counts = new System.Collections.Generic.Dictionary<string, int>();
        var problems = new System.Text.StringBuilder();

        foreach (var raw in Directory.GetFiles(AudioRoot, "*.*", SearchOption.AllDirectories))
        {
            string ext = Path.GetExtension(raw).ToLowerInvariant();
            if (ext != ".ogg" && ext != ".wav") continue;      // `sfx/` 里还有一个 `remnant_cue_props.json`
            string path = raw.Replace('\\', '/');

            // 类别 = `audio/` 下一级目录名（vo / sfx / music / bgm）
            string rel = path.Substring(AudioRoot.Length).TrimStart('/');
            int slash = rel.IndexOf('/');
            string sub = (slash < 0 ? "" : rel.Substring(0, slash)).ToLowerInvariant();
            string name = Path.GetFileNameWithoutExtension(raw);

            AudioClipLoadType load;
            AudioCompressionFormat fmt;
            bool preload;
            if (!TryGetAudioRule(sub, name, ext, out load, out fmt, out preload))
            {
                // 规则表里没有 ⇒ 报出来，⛔ 不猜（原版判据只覆盖了这四类里的已知形状）
                problems.Append($"\n    没有规则：{path}");
                noRule++;
                continue;
            }

            var ai = AssetImporter.GetAtPath(path) as AudioImporter;
            if (ai == null)
            {
                // `A1931`：磁盘上有、AssetDatabase 里没有 ⇒ 导入失败/未导入。⛔ 不静默。
                problems.Append($"\n    拿不到 AudioImporter：{path}");
                noImporter++;
                continue;
            }

            if (ApplyAudioRule(ai, load, fmt, preload, name)) n++;
            else alreadyOk++;
            int c;
            counts.TryGetValue(sub, out c);
            counts[sub] = c + 1;
        }

        var byCat = new System.Text.StringBuilder();
        foreach (var kv in counts) byCat.Append($" {kv.Key}={kv.Value}");
        Debug.Log(P + $"音频导入设置已重设：{n} 条（已符合跳过 {alreadyOk} 条 · {byCat.ToString().Trim()}）→ 按报告 §二 的规则表");
        if (noRule > 0 || noImporter > 0)
            Debug.LogWarning(P + $"🔴 未覆盖 {noRule + noImporter} 条（规则表没写 {noRule} · 拿不到 importer {noImporter}）：{problems}");
        else
            Debug.Log(P + "覆盖完整：磁盘上的 .ogg/.wav 全部拿到了 AudioImporter ✅");
    }

    /// <summary>规则表：目录 + 扩展名 + 例外名单 → (loadType, compressionFormat, preload)。</summary>
    static bool TryGetAudioRule(string sub, string name, string ext,
                                out AudioClipLoadType load, out AudioCompressionFormat fmt, out bool preload)
    {
        load = AudioClipLoadType.CompressedInMemory;
        fmt = AudioCompressionFormat.Vorbis;
        preload = false;

        switch (sub)
        {
            case "vo":
                // 原版 1892 条直证：ogg → Vorbis（1778 条）· wav → ADPCM（114 条）；都不预载。
                fmt = ext == ".wav" ? AudioCompressionFormat.ADPCM : AudioCompressionFormat.Vorbis;
                return true;

            case "sfx":
                if (NameIn(name, SfxStreaming))
                {
                    load = AudioClipLoadType.Streaming;
                    fmt = AudioCompressionFormat.Vorbis;
                    preload = !NameIn(name, SfxStreamingNoPreload);
                    return true;
                }
                // 默认：原版 509 条 wav 是 ADPCM（`OvertimeStart` 是上面例外表里的 Streaming）。
                // 非名单内的 .ogg 原版没有先例 ⇒ 报出来，⛔ 不猜成 ADPCM。
                if (ext != ".wav") return false;
                fmt = AudioCompressionFormat.ADPCM;
                return true;

            case "music":
                if (NameIn(name, MusicPreload)) { load = AudioClipLoadType.Streaming; preload = true; return true; }
                if (NameIn(name, MusicNoPreload)) { load = AudioClipLoadType.Streaming; preload = false; return true; }
                return false;   // 新曲目没判据 ⇒ 报出来

            case "bgm":
                // 原版 25/25：CompressedInMemory + Vorbis；preload 见 `BgmPreload`（15 true / 10 false）。
                preload = NameIn(name, BgmPreload);
                return true;

            default:
                return false;   // `audio/` 根下或未知子目录 ⇒ 报出来
        }
    }

    /// <summary>
    /// 把一条规则落到 `AudioImporter` 上（公共项见 `ApplyAudioImportSettings` 的注释）。
    /// 返回 **true = 真的改了并重导**；false = 已经符合（⛔ 不重复 `SaveAndReimport` ——
    /// 2434 条音频全量重编码很贵，而本方法要求幂等、可反复跑）。
    /// </summary>
    static bool ApplyAudioRule(AudioImporter ai, AudioClipLoadType load,
                               AudioCompressionFormat fmt, bool preload, string clipName)
    {
        bool lib = TruthLoadInBackground(clipName);
        var s = ai.defaultSampleSettings;
        bool changed = s.loadType != load
                    || s.compressionFormat != fmt
                    || s.preloadAudioData != preload
                    || s.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate
                    || ai.loadInBackground != lib
                    || ai.forceToMono;
        if (!changed) return false;

        s.loadType = load;
        s.compressionFormat = fmt;
        s.preloadAudioData = preload;
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        // ⚠️ `s.quality` **不动**：判不出原版值（不在 AudioClip 里），现状 1.0 —— 报告 §2·4。
        ai.defaultSampleSettings = s;
        ai.loadInBackground = lib;     // 🆕 B112_R1W：按原版真值写（21 条打击音是 false），查不到才回退 true
        ai.forceToMono = false;        // 原版声道与我们源文件一致 ⇒ 不靠它改声道
        ai.SaveAndReimport();
        return true;
    }

    /// <summary>大小写不敏感的名字匹配（Windows 文件名；例外表照原版 clip 名写）。</summary>
    static bool NameIn(string name, string[] set)
    {
        foreach (var s in set)
            if (string.Equals(name, s, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // `A1931` 探针：`AssetDatabase.FindAssets("t:AudioClip")` 返回集 vs 磁盘实际文件。
    //
    // 为什么要有它：2026-10-19 静态普查发现 `vo/` 有 7 条磁盘上在、`FindAssets` 不返回
    // （其中 6 条是整棵音频树里**唯一 6 个**「Ogg 末页 granule = 0」的文件）。
    // 本探针把三种可能分开：α = 只是 AssetDatabase 没刷新 · β = Unity 导入失败（Ogg 容器坏了）·
    // γ = 导入成功、只是没跑过 `ApplyAudioImportSettings`。
    // 判读矩阵 → `资料/普查产出_1019/W_工具线_T06音频导入_1019b.md` §3·2。
    //
    // ⚠️ **默认 dry**：只读、不 `ImportAsset`。要复测「强制导入后能不能找得到」再用 Force 那支。
    // ⚠️ 2026-10-19 加进来时**没有跑过**（写手代理不跑 Unity）。
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/CardPresentation/诊断音频导入覆盖(A1931·dry)")]
    public static void DiagnoseAudioImportCoverageCLI() { DiagnoseAudioImportCoverage(false); }

    [MenuItem("Tools/CardPresentation/诊断音频导入覆盖(A1931·强制重导)")]
    public static void DiagnoseAudioImportCoverageForceCLI() { DiagnoseAudioImportCoverage(true); }

    static void DiagnoseAudioImportCoverage(bool forceReimport)
    {
        foreach (var sub in new[] { "vo", "sfx", "music", "bgm" })
        {
            string dir = AudioRoot + "/" + sub;
            if (!Directory.Exists(dir)) { Debug.Log($"[AUDIT] {sub}: 目录不存在 {dir}"); continue; }

            var onDisk = new System.Collections.Generic.List<string>();
            var diskSet = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var f in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                string e = Path.GetExtension(f).ToLowerInvariant();
                if (e != ".ogg" && e != ".wav") continue;
                string p = f.Replace('\\', '/');
                onDisk.Add(p);
                diskSet.Add(p);
            }

            var found = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { dir });
            foreach (var g in guids) found.Add(AssetDatabase.GUIDToAssetPath(g));

            var missing = new System.Collections.Generic.List<string>();
            foreach (var p in onDisk) if (!found.Contains(p)) missing.Add(p);
            int extra = 0;
            foreach (var p in found) if (!diskSet.Contains(p)) extra++;

            Debug.Log($"[AUDIT] {sub}: onDisk={onDisk.Count} findAssets={guids.Length} " +
                      $"missing={missing.Count} extra={extra}");

            // ① 漏项：importer / clip 各是什么（区分 α: FindAssets 漏 vs β: 导入失败 vs γ: 只是没跑设置）
            foreach (var p in missing)
            {
                var ai = AssetImporter.GetAtPath(p);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                Debug.Log($"[AUDIT]   MISSING {p}  guid={AssetDatabase.AssetPathToGUID(p)}  " +
                          $"importer={(ai == null ? "null" : ai.GetType().Name)}  clip={(clip == null ? "null" : "ok")}");
            }
            // ② 找到的：看有没有「importer 类型不对 / clip 取不到」的静默异常
            foreach (var p in found)
            {
                var ai = AssetImporter.GetAtPath(p);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                if (ai == null || ai.GetType() != typeof(AudioImporter) || clip == null)
                    Debug.Log($"[AUDIT]   ABNORMAL {p}  importer={(ai == null ? "null" : ai.GetType().Name)}  clip={(clip == null ? "null" : "ok")}");
            }
            // ③ 强制重导后能否被 FindAssets 找到（α/β 的分水岭）—— 只在 Force 那支跑
            if (!forceReimport) continue;
            foreach (var p in missing)
            {
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                bool now = false;
                foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { dir }))
                    if (string.Equals(AssetDatabase.GUIDToAssetPath(g), p, System.StringComparison.OrdinalIgnoreCase)) { now = true; break; }
                var ai = AssetImporter.GetAtPath(p);
                Debug.Log($"[AUDIT]   AFTER-IMPORT {p}  found={now}  importer={(ai == null ? "null" : ai.GetType().Name)}");
            }
        }
    }

    /// <summary>给 Art 下所有 PNG 定导入设置</summary>
    [MenuItem("Tools/CardPresentation/重设美术导入设置")]
    public static void ApplyImportSettings()    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            // 🔴 **2026-10-18（`A1655④` 同族 · T09）**：只给**卡框**开 Read/Write —— 立绘不再可读。
            //   判据：`CardView.FrameUv` 的兜底（`GetPixels32` 扫不透明 bbox）只对**卡框**有意义；
            //   立绘（`art_*.png`）与卡背（`back_*.png`）运行期**没有任何人读像素**
            //   （`CardArt.Portrait` / `CardBack` 只把贴图交给材质采样）。
            //   ⚠️ 改前是 `path.Contains("/cards/")` ⇒ **2336 张立绘也各留一份 CPU 侧副本**（现读 cards/ 下 300 张 `frame_*`）。
            //   ⚠️ 卡框有**两处**：`cards/frame_*`（自绘兜底 150 张）+ `cards/clan/frame_clan_*`（原版 SO 那一路 104 张，`CardArtClanFrames.FrameTexture`）；`frame_uv.json` 查不到的那几张仍要能回读 ⇒ 这一支保持 `true`。🔴 2026-10-30（B100_F13）：改前只判 `/cards/frame_` ⇒ clan 那 104 张 `.meta` **全 `isReadable:0`** ⇒ `CardView.FrameMaskData`/`FrameArtWindow` 的 `isReadable` 闸恒 false ⇒ 战术/防御卡的卡框开口裁切静默失效（受影响 = 52 张 `stratagem` 卡框）；⛔ 别放宽成裸 `"frame_"`（会把无关贴图也烤一份 CPU 副本）。
            bool isFrame = path.Contains("/cards/frame_") || path.Contains("/cards/clan/frame_");
            bool isBackdrop = path.EndsWith("arena1_bg.png");
            // 🔴 **2026-10-31（B108_A17）卡框 SDF 要走 `Uncompressed`** —— 判据 = `判据/B108_AUD09_纹理格式.md`
            //   §二 D 行 + §3·1（原版 bundle 实读）：13 阵营 104 张 `40k_Cardframes_*_SDF_tier*` **全部**
            //   `m_TextureFormat = 4`(RGBA32) · 128² · 未压缩；`cardback_noise` 那张（原版 `Noise Combined`）
            //   也是 RGBA32。SDF 是**平滑的软光/影渐变**，被 BC7 压过会在光带边缘出方块/色阶（128² 还放大铺满卡面）。
            //   ⚠️ **两张例外**：`generic` 与 `Card_board_frame_SDF` 都源自原版 `Card board frame SDF`
            //   （原版就是 **BC7 · fmt=25**）⇒ 它们保持 `CompressedHQ`，与盘上已改好的 `.meta` 对齐。
            //   ⚠️ 只改压缩档：`sRGBTexture`（Linear 工程要 0，见 `工具/import_original_card_sdf.py` docstring）
            //      与 `isReadable`（`CardView.FrameUv` 的 `GetPixels32` 兜底要靠它）**都不动**。
            bool isSdf = path.Contains("/card_sdf/")
                         && !path.EndsWith("/card_sdf/generic.png")
                         && !path.EndsWith("/card_sdf/Card_board_frame_SDF.png");

            ti.textureType = OriginalTextureType(path, ti.textureType);   // 原版有真值就照原版；表外的仍是 Default（我们自己按 UV 摆，不要 Sprite）
            // 🔴 **必须关掉 `alphaIsTransparency`**（2026-09-15 踩到）：
            //    开着它时 Unity 会把**透明区的 RGB 用最近邻填充补上**，而填充的划分边界正好是
            //    **多边形格子** ⇒ 整张立绘变成**彩色马赛克**（实测卡面糊成一块块的）。
            //    我们**恰恰要用透明区的 RGB** —— 底层 `ArtOpaque` 拿它补卡框拱窗里的背景。
            //    这条判据与 `工具/import_original_art.py` 的 `fix_art_meta()` **是同一条**
            //    （那个脚本直接改 `.meta` 文本）—— 这里也设一遍，免得**两个工具互相打架**：
            //    我这次就是跑了本方法把 `fix_art_meta` 关掉的开关又打开了，卡面当场变马赛克。
            ti.alphaIsTransparency = false;
            // 🔴 2026-11-04（`B139_H` · 修 15-03）：本行原来是无条件 `false` ⇒ 把按原版 `m_MipCount>1` 改好的 21 张（`ui_menu/40k_Crate_*` / `Background` / `InputFieldBackground` / `40K_icon_*` …）在下次烘焙时**静默打回**；判据 = `vfx_texture_mips.tsv` / `arena_texture_import.tsv` 的 `mipCount`，**表里查不到保留现值**。
            ti.mipmapEnabled = OriginalMipEnabled(path, ti.mipmapEnabled);
            // 🔴 **2026-10-22 起是白名单；2026-11-04（`B139_H` · `B139-I3`）改成【三档表】** —— U/V 分开写，
            //   才能表达 `Shine trail` 那种原版 `(0,1)`（U Repeat / V Clamp）的**混合档**（布尔白名单表达不了）。
            //   表外的仍按旧口径 Clamp（`CardBlobShadow.png` 原版就是 Clamp，别一起改）。
            int _wu, _wv;
            if (OriginalWrap(path, out _wu, out _wv)) { ti.wrapModeU = (TextureWrapMode)_wu; ti.wrapModeV = (TextureWrapMode)_wv; }
            else ti.wrapMode = TextureWrapMode.Clamp;
            { FilterMode _fm; int _an; if (OriginalFilterAniso(path, out _fm, out _an)) { ti.filterMode = _fm; ti.anisoLevel = Mathf.Max(1, _an); } else ti.filterMode = FilterMode.Bilinear; }   // 🆕 B139_H · B139-I3：aniso 也照原版逐张（ui_menu/Background 等 3 张原版是 16）
            // 🔴 **2026-10-31（B110_T1）：`ui_menu` / `boosterpack` 也抬到 2048** —— 别再压掉一半像素。
            //   判据（现读源 PNG 的 IHDR，逐张量的）：这两个子树里**只有 66 张**源边 > 1024 ——
            //   `ui_menu` 65 张（64 张 2048² 的 `UI_Deck_Warlord_*` + `UI_Deck_Master_Zacharial`，
            //   外加 `UI_Main_Upper_bar` 2048×92）· `boosterpack` 1 张（`40K_shop_offer_booster_UM_Expansion_1` 2048²）。
            //   其余源图全部 ≤ 1024 ⇒ 抬到 2048 **不会改变它们的导入结果**（上采样不会发生），只救回这 66 张。
            //   ⚠️ 原来对 `ui_menu` 写 1024（2026-09-15 那次），而督军方形立绘是 **2026-10-18** 才导进来的
            //   （`工具/import_deck_warlord_art.py`）⇒ **不是有意的取舍，是规则写在素材之前**。
            //   构建产物实测（`_tmp_view/player/WarpforgePlayer_Data/resources.assets`）：65 张落到 1024²、
            //   `UI_Main_Upper_bar` 落到 1024×46 ⇒ 2048² 的方形立绘**掉一半像素**（玩家可见）。
            //   ⚠️ 这个值**只写进 `platformSettings` 的 `DefaultTexturePlatform` 块**（顶层 `maxTextureSize` 不生效，
            //   Unity 存盘时会把它写回 2048）—— 同族记录 → `工具/import_original_card_sdf.py:113`。
            //   判据出处 = 原版 `bundle_uiwarlords_assets_all` 里这批就是 2048²（`工具/import_deck_warlord_art.py:7`）。
            //   ⚠️ 因为整批重导只影响这 66 张，按**子树**写规则（不按 66 个文件名硬编码）：将来往这两个子树里
            //   再导 2048 的图会自动拿对，也不会外溢到 `cards/` / `avatars/` / `vfx/` 那些不需要 2048 的目录。
            bool hiResUi = path.Contains("/ui_menu/") || path.Contains("/boosterpack/");
            ti.maxTextureSize = (isBackdrop || hiResUi || path.Contains("/ui_deck/")) ? 2048 : 1024;
            // ⚠️ **必须关掉 NPOT 缩放**：默认会把非 2 次幂的图缩到最近的 2 次幂 ——
            //    背景图 1920×1080 会被拉成 2048×1024（宽高比从 1.78 变 2.0），
            //    背景一「铺满」就整体放大 12.5%，和原版量出来的槽位坐标对不上了（踩过）。
            //    UI 切片同理（182×112 这种尺寸全会被改）。
            ti.npotScale = TextureImporterNPOTScale.None;
            // 卡框：`CardView` 要在运行时量「不透明包围盒」来裁 UV，所以必须可读
            ti.isReadable = isFrame;
            // 🔴 **2026-10-18（T09 / `A1655④`）：非背景图不再用 `Uncompressed`** ——
            //   原来这一行是 `isBackdrop ? CompressedHQ : Uncompressed`，于是**卡框 + 立绘**
            //   （`Resources/Art/cards/*.png`；现读 1478 张里 **1306 张 `isReadable:1`**）全按
            //   RGBA32 **未压缩**进包 —— 单张 1024² = **4 MB** 常驻内存。
            //   判据（**这是「我们怎么打包」，不是原版外观参数**）：原版 bundle 里这批确实是
            //   `m_TextureFormat = 4`(RGBA32)（`资料/普查产出_第二十一会话/N4_收藏窗50秒.md`），
            //   但那不要求我们照抄打包格式；账 `A1655④` 的原话就是「`ArtBaker` 仍 Uncompressed → 改压缩」
            //   （→ `资料/待办判据_第三十三会话.md` 的 `A1655`）。
            //   选 **`CompressedHQ`（BC7）而不是 `Compressed`（DXT）**：这批图带 alpha，而且
            //   `ArtOpaque` **要用透明区的 RGB**（见上面 `alphaIsTransparency=false` 那段）——
            //   BC1/DXT1 丢 alpha、DXT5 在渐变色带上出块状伪影；BC7 是本工程保真度最高的一档
            //   （同用法先例 = 本文件 `ApplyArenaImportSettings`：原版那批战场图是 **DXT5Crunched(fmt=29)** ⇒ `CompressedHQ`。
            //   🔴 **2026-10-31 订正（B108_A17）**：原注释把 `fmt=29` 说成 BC7 —— **29 = DXT5Crunched，BC7 = 25**
            //   （`判据/B108_AUD09_纹理格式.md` §1·3 实读枚举）。选 `CompressedHQ` 的结论不变，只是判据写错了。）
            //   ⚠️ `isReadable` 那一行**不动**：`CardView.FrameUv` 对 `frame_uv.json` 里查不到的卡框
            //   仍要退回 `GetPixels32()`（运行时回读），关掉 Read/Write 会让那条兜底静默失效。
            //   ⚠️ **本行只改「以后导入」**：现存的 `.meta` 要重跑本方法（菜单 / `-executeMethod`）才生效。
            //   🔴 **2026-10-31（B108_A17）**：卡框 SDF（`isSdf`，见上面 `isFrame` 那一节）改 `Uncompressed`
            //   —— 判据 = R7 原版 128² RGBA32 未压缩；其余仍 `CompressedHQ`（原版 BC7）。
            //   平台块也要一起写：`.meta` 里 `buildTarget: Standalone` 那条若不是 `overridden`，它就是死值；
            //   写一遍保证将来谁开了覆盖也是 0（与盘上 105 份 `.meta` 的两处改动对齐）。
            ti.textureCompression = isSdf ? TextureImporterCompression.Uncompressed
                                          : TextureImporterCompression.CompressedHQ;
            if (isSdf)
            {
                var sdfPs = ti.GetPlatformTextureSettings("Standalone");
                sdfPs.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SetPlatformTextureSettings(sdfPs);
            }
            // ✅ 已复刻（2026-11-04 · B141 · 15-10 · 判据 = `判据/B141_W130_对账_资源.md` §一 15-10 + 原版 `m_TextureFormat`）
            //   表内（`CompTruths`，现读 144 条）照原版压缩档；表外保持上面的 `CompressedHQ`（原口径不动）。
            //   本方法接表后 `Resources/Art` 的 UI 切片也走真值表（15-09 并入 137 个文件 / 136 个原版名）；旧注释「只影响 1 张」已过期。
            if (!isSdf) ApplyCompressionTruth(ti, path);
            ti.SaveAndReimport();
        }
        Debug.Log(P + "导入设置已重设");

        // 🔴 **2026-10-31（B110_T1）：实读校验 —— 判据是「导入后的真实尺寸」，不是 `.meta`、也不是「日志说改了 N 张」。**
        //   只查 `ui_menu` / `boosterpack` 里**源边 > 1024** 的那些（现读 66 张）：其余源图本来就 ≤ 1024，
        //   抬到 2048 也不会变，查了只是白加载一遍贴图。
        //   ⚠️ 源尺寸必须用 `GetSourceTextureWidthAndHeight` —— `LoadAssetAtPath<Texture2D>` 拿到的是
        //   **已导入**的那一份（被上一次的 `maxTextureSize` 砍过），拿它当「源」= 自证
        //   （同 `ApplyArenaImportSettings` 里 `:604` 那次踩的坑）。
        int hiChecked = 0, hiShrunk = 0;
        var hiShrunkList = new System.Text.StringBuilder();
        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
        {
            var p2 = AssetDatabase.GUIDToAssetPath(g).Replace('\\', '/');
            if (!(p2.Contains("/ui_menu/") || p2.Contains("/boosterpack/"))) continue;
            var ti2 = AssetImporter.GetAtPath(p2) as TextureImporter;
            if (ti2 == null) continue;
            int sw2 = 0, sh2 = 0;
            try { ti2.GetSourceTextureWidthAndHeight(out sw2, out sh2); } catch (System.Exception) { }
            int want2 = Mathf.Max(sw2, sh2);
            if (want2 <= 1024) continue;                    // 不在本规则能救的范围内
            hiChecked++;
            var tex2 = AssetDatabase.LoadAssetAtPath<Texture2D>(p2);
            if (tex2 == null) { Debug.LogWarning(P + "实读校验：取不到贴图 " + p2); continue; }
            if (Mathf.Max(tex2.width, tex2.height) < want2)
            {
                hiShrunk++;
                if (hiShrunk <= 8)
                    hiShrunkList.Append($"\n    {System.IO.Path.GetFileName(p2)}：源 {sw2}×{sh2} → 导入 {tex2.width}×{tex2.height}");
            }
        }
        // ⚠️ 先断「前提成立」再断结论：0 张 = 这条校验**什么都没验到**，不能算绿（本工程踩过「`if (x != null)` ⇒ 静默跳过」）。
        if (hiChecked == 0)
            Debug.LogWarning(P + "实读校验：ui_menu/boosterpack 里**一张源边>1024 的都没找到** —— 前提不成立，" +
                                "这条校验没验到东西（图没导进来？还是 `GetSourceTextureWidthAndHeight` 读不到？）");
        else if (hiShrunk > 0)
            Debug.LogWarning(P + $"🔴 hi-res UI（ui_menu/boosterpack）仍有 {hiShrunk}/{hiChecked} 张被缩：" + hiShrunkList);
        else
            Debug.Log(P + $"实读校验：hi-res UI（ui_menu/boosterpack）源边>1024 的 {hiChecked} 张**都没有被缩** ✅");
    }

    /// <summary>
    /// 🆕 **2026-09-22：战场贴图的导入设置**（`WarpforgeArena1/arenas/&lt;场&gt;/Textures/`）。
    ///
    /// 🔴 **为什么单开一个方法**：这一批**一直没人管**（`ApplyImportSettings` 只扫 `Resources/Art`）
    /// ⇒ 用的是 **Unity 默认**（`maxTextureSize: 2048` + 带 mipmap）。实测对不上原版：
    /// | 原版贴图 | 尺寸 | mipCount |
    /// |---|---|---|
    /// | `Battle Arena Space Wolves Props 1` | **8192²** | 14 |
    /// | `Battle Arena Space Wolves Floor` | 4096² | **1** |
    /// | `Space Wolves Battle Arena Baked Atlas…` | 4096² | **1** |
    /// 而我们**一律 2048 + mip** ⇒ **半分辨率、还采样了原版根本没有的 mip**。
    /// 症状：大面积贴图（雪地/地面）**糊**、细节被抹平 —— 用并排图看得出来，
    /// **全局亮度却是对的**（所以「亮度比」那类指标**照不出这个错**）。
    ///
    /// ⚠️ 判据来自**原始 bundle 实读**（`Texture2D.m_Width/m_MipCount`），不是照截图猜的。
    /// ⚠️ `alphaIsTransparency` **必须 true** —— 战场图集的全透明区要靠它才不按自己的 RGB 画出来
    ///    （与上面卡牌立绘那条**相反**，两类贴图判据不同）。
    /// </summary>
    [MenuItem("Tools/CardPresentation/重设战场贴图导入设置")]
    public static void ApplyArenaImportSettings()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/WarpforgeArena1/arenas" });
        int n = 0, big = 0;
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            // 「不要缩」= 上限取到**源图的边长**（2 的幂向上取，封顶 8192）。
            // 🔴 **2026-09-22 修：必须用 `GetSourceTextureWidthAndHeight`，不能用 `LoadAssetAtPath<Texture2D>`。**
            //    后者拿到的是**已经导入过的那一份**（被上一次的 `maxTextureSize` 砍过），
            //    于是算出来还是 2048 ⇒ **整个方法等于没跑**（实测：`BattleArena1 Texture Baked.png`
            //    源是 **2048×4096**、`Battle Arena 1 Floor.png` 源是 **4096²**，meta 里却一直写着 2048）。
            //    判据 = **改完去读 `.meta` 的 `maxTextureSize`**，不是「日志说改了 N 张」。
            int sw = 0, sh = 0;
            try { ti.GetSourceTextureWidthAndHeight(out sw, out sh); } catch (System.Exception) { }
            int src = Mathf.Max(sw, sh);
            if (src <= 0)
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                src = t != null ? Mathf.Max(t.width, t.height) : 2048;
            }
            int cap = 512;
            while (cap < src && cap < 8192) cap *= 2;

            ti.textureType = OriginalTextureType(path, ti.textureType);   // 原版有真值就照原版；表外仍是 Default
            ti.maxTextureSize = cap;
            ti.mipmapEnabled = OriginalMipEnabled(path, ti.mipmapEnabled);   // 照原版逐张（15-12 修：原来无条件 false ⇒ 把包 I 的 arena mip 打回）
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaIsTransparency = true;                 // ⚠️ 与卡牌立绘**相反**（见上）
            { int _wu, _wv; if (OriginalWrap(path, out _wu, out _wv)) { ti.wrapModeU = (TextureWrapMode)_wu; ti.wrapModeV = (TextureWrapMode)_wv; } else ti.wrapMode = TextureWrapMode.Repeat; }
            { FilterMode _fm; int _an; if (OriginalFilterAniso(path, out _fm, out _an)) { ti.filterMode = _fm; ti.anisoLevel = Mathf.Max(1, _an); } else ti.filterMode = FilterMode.Bilinear; }
            // 原版那几张是 **DXT5Crunched（fmt=29）** —— 对应 `CompressedHQ`。用普通 `Compressed`（DXT）会把
            // 大面积渐变压出块状色带。
            // 🔴 **2026-10-31 订正（B108_A17）**：原注释写的是「BC7（fmt=29）」—— **29 = DXT5Crunched，BC7 = 25**
            // （`判据/B108_AUD09_纹理格式.md` §1·3 实读枚举）。`CompressedHQ` 这个结论不变，只订正判据。
            // ✅ 已复刻（2026-11-04 · B141 · 15-10：`判据/B141_W130_对账_资源.md` §一 15-10 + 报告 `B141_W130_ArtBaker.md` §3）
            //   原来这里**无条件**写 `CompressedHQ` ⇒ 15-10 那 8 个名字（原版 RGBA32 / DXT1；共 39 个文件实例）被压到 BC7，
            //   比原版**低档**。现在表内（`CompTruths`，现读 144 条 · 70 条 crunched=true）照原版，表外仍是 `CompressedHQ`（原口径不动）。
            var ctruth = OriginalCompression(path);
            ti.textureCompression = ctruth.Compression;
            // 🔴 **2026-09-22 补：平台覆盖也要一起设** ——
            //    meta 里除了顶层 `maxTextureSize`，还有一条 **`buildTarget: Standalone`** 的覆盖块，
            //    它带着自己的 `maxTextureSize`（我们这份一直是 **2048**）。
            //    顶层改成 4096 之后编辑器里**未必生效**（实测：改完顶层，导入后仍是 2048）。
            //    ⇒ 两边都写，并用下面的**实读判据**确认。
            try
            {
                var ps = ti.GetPlatformTextureSettings("Standalone");
                ps.maxTextureSize = cap;
                ps.textureCompression = ctruth.Compression;   // ✅ B141 · 15-10：平台块也照原版（否则顶层被它盖掉）
                ps.crunchedCompression = ctruth.Crunched;   // ✅ B141 · 15-09/15-10：原版是 *Crunched 时平台块也带 Crunch（现表内 144 条里 70 条 true ⇒ **不是「无行为变化」**）
                if (ctruth.ExplicitFormat >= 0) ps.format = (TextureImporterFormat)ctruth.ExplicitFormat;   // 原版 DXT1 那三张要显式格式
                ps.overridden = true;
                ti.SetPlatformTextureSettings(ps);
            }
            catch (System.Exception e) { Debug.LogWarning(P + $"设 Standalone 平台覆盖失败：{e.Message}"); }
            ti.SaveAndReimport();
            n++;
            if (cap >= 4096) big++;
        }
        Debug.Log(P + $"战场贴图导入设置已重设：{n} 张（其中 {big} 张放开到 4096 以上）");

        // 🔴 **判据 = 实读导入后的尺寸**（不是 meta、也不是日志说改了几张）——
        //    顶层 `maxTextureSize` 会被平台覆盖块**盖掉**（踩过：改完顶层，导入后仍是 2048）。
        int shrunk = 0;
        var shrunkList = new System.Text.StringBuilder();
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var ti2 = AssetImporter.GetAtPath(path) as TextureImporter;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (ti2 == null || tex == null) continue;
            int sw2 = 0, sh2 = 0;
            try { ti2.GetSourceTextureWidthAndHeight(out sw2, out sh2); } catch (System.Exception) { }
            int want = Mathf.Max(sw2, sh2);
            if (want <= 0) continue;
            if (Mathf.Max(tex.width, tex.height) < want)
            {
                shrunk++;
                if (shrunk <= 6)
                    shrunkList.Append($"\n    {System.IO.Path.GetFileName(path)}：源 {sw2}×{sh2} → 导入 {tex.width}×{tex.height}");
            }
        }
        if (shrunk > 0)
            Debug.LogWarning(P + $"🔴 **仍有 {shrunk} 张被缩**（源比导入大）：" + shrunkList);
        else
            Debug.Log(P + "实读校验：**没有一张被缩**（导入尺寸 = 源尺寸）✅");
    }

    // ───────── 🆕 2026-11-01 `B112_R1W`：`loadInBackground` 按原版真值，不再无条件 true ─────────
    // 判据 = `Unity/工具/_sfx_audio_truth.json`（`_sfx_meta.py --scan-truth` 从 30 个含 AudioClip 的
    // bundle 的 typetree 机生成；`clips[名] = [loadType, compressionFormat, preloadAudioData, loadInBackground]`）。
    // 🔴 为什么必须改：真值里有 **21 条打击音**（`Hit *` / `Squig Growl 1`）是 `false`，而这里原来无条件写
    //   `true` ⇒ 重跑一次就把 `_sfx_meta.py` 按真值修好的那 21 条**改回去**（净回退 `B-92`）。
    // 取不到真值（文件不在 / 解析失败）⇒ 回退 `true` 并**出声**，⛔ 不静默。
    static System.Collections.Generic.Dictionary<string, bool> _truthLoadInBg;
    static bool _truthLoadInBgTried;

    static bool TruthLoadInBackground(string clipName)
    {
        if (!_truthLoadInBgTried)
        {
            _truthLoadInBgTried = true;
            _truthLoadInBg = LoadTruthLoadInBackground();
        }
        bool v;
        if (_truthLoadInBg != null && clipName != null && _truthLoadInBg.TryGetValue(clipName, out v)) return v;
        return true;
    }

    static System.Collections.Generic.Dictionary<string, bool> LoadTruthLoadInBackground()
    {
        try
        {
            // 工程根 = Assets/.. ⇒ `d:/4/Unity`（工具目录在 `<工程根>/工具/`，不在 Assets 下）
            string p = Path.GetFullPath(Path.Combine(Application.dataPath, "../../工具/_sfx_audio_truth.json"));
            if (!File.Exists(p))
            {
                Debug.LogWarning(P + $"🔴 读不到音频真值表（{p}）⇒ `loadInBackground` 回退 true（可能把 21 条打击音改错）");
                return null;
            }
            var text = File.ReadAllText(p);
            var ms = System.Text.RegularExpressions.Regex.Matches(text,
                "\"([^\"]+)\"\\s*:\\s*\\[\\s*(-?\\d+)\\s*,\\s*(-?\\d+)\\s*,\\s*(-?\\d+)\\s*,\\s*(-?\\d+)\\s*\\]");
            var d = new System.Collections.Generic.Dictionary<string, bool>();
            foreach (System.Text.RegularExpressions.Match x in ms)
                d[x.Groups[1].Value] = x.Groups[5].Value != "0";
            int off = 0;
            foreach (var kv in d) if (!kv.Value) off++;
            Debug.Log(P + $"音频真值表已载入：{d.Count} 条（`loadInBackground=false` {off} 条）");
            return d.Count > 0 ? d : null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(P + "🔴 读音频真值表失败：" + e.GetType().Name + " " + e.Message);
            return null;
        }
    }

    // ───────── 🆕 2026-11-04（`B139_H` · `B139-I2`/`B139-I3`）：**按原版真值设导入字段，不再无条件覆盖** ─────────
    //
    // **为什么要有这一节**：`ApplyImportSettings` / `ApplyArenaImportSettings` 原来对
    // `Resources/Art` 与 `arenas/**` 的贴图**无条件**写 `textureType=Default` / `mipmapEnabled=false` /
    // `wrapMode=Clamp` ⇒ 包 I 按原版逐张改好的 55 个 `.meta`（15-02/03/04/06/07）会在下一次烘焙时
    // **静默打回**（没有报错、没有日志，只有画面上接缝/糊度不对）。
    //
    // **判据（旁挂表，⛔ 不写死在代码里）**：
    //   · `数据/游戏数据/vfx_texture_mips.tsv`（2,984 个原版 `Texture2D` 名，`工具/gen_vfx_texture_mips.py` 生成）
    //   · `数据/游戏数据/arena_texture_import.tsv`（127 个战场名，`工具/gen_arena_texture_import.py` 生成）
    //   🔴 **2026-11-04 订正（B141 · 顺手发现 #1）**：这里原来写「两张表列序在要用的五格上一致：
    //      `mipCount=3 · wrapU=6 · wrapV=7 · filterMode=8 · aniso=9`」—— **错**，只有 `mipCount=3` 一致：
    //      `vfx_texture_mips.tsv` = `name,width,height,mipCount,bundle,wrapU,wrapV,filterMode,aniso,npotScale`
    //      （`wrapU` = **第 5 列**）；`arena_texture_import.tsv` 多一列 `colorSpace`
    //      = `…mipCount,colorSpace,bundle,wrapU,wrapV,filterMode,aniso`（`wrapU` = **第 6 列**）。
    //      ⇒ 现在**按表头认列**（`HeaderIndex`），⛔ 不再写死偏移。判据 → `判据/B141_W130_对账_资源.md` §四·1。
    //
    // **三条口径**（与 `ArenaBuilder.ApplyOriginalImport` 同一套）：
    //   ① 表里有 ⇒ **照原版写**（`mipCount > 1` 才开 mip；wrap U/V **分开写**，能表达 `(0,1)` 混合档）；
    //   ② 表里没有 ⇒ `mipmapEnabled` / `wrapMode` **保留现值**（⛔ 不再无条件覆盖）；wrap 另有
    //      `RepeatTextures` 老白名单兜底（那 12 张原版 (0,0)）；
    //   ③ 法线图（13 个原版名）**一律保留现值** —— 见 `OriginalTextureType` 的说明（15-01 三步里的第 ① 步）。

    struct ArtTexTruth
    {
        public int MipCount, WrapU, WrapV, Filter, Aniso;
        public bool HasWrap, HasFilter;
    }

    static System.Collections.Generic.Dictionary<string, ArtTexTruth> _artTruth;
    static bool _artTruthTried;

    /// <summary>原版法线图的名字（去后缀后的原名）。判据 → `报告/B136_审计_纹理与图集.md` #16 /
    /// `判据/B110_R5_资源侧放回11条.md:174-196`。</summary>
    static readonly string[] NormalMapNames =
    {
        "Card Booster Pack Normal", "Distortion Star", "EarthCrackNormal", "GoopSprayNormal",
        "GoopStreakNormal", "RippleNormal", "SandDecalNormal", "SmokePuffNormal",
        "Explosion Distort", "TinyStonesNormals", "RippleSubtle", "NoiseNRM", "SplatNormal",
    };

    /// <summary>贴图文件名 → 原版名（去掉我们的 `_sprite` / `_0` / `_1` 后缀；同一套判据见
    /// `d:/4/_tmp_view/b139_i/analyze2.py`，包 I 用它把 3,009 个原版对象对回 1,724 张）。</summary>
    static string[] OriginalNameCandidates(string path)
    {
        string k = Path.GetFileNameWithoutExtension(path);
        var l = new System.Collections.Generic.List<string>(8) { k };
        foreach (var s in new[] { "_sprite", "_0", "_1" })
            if (k.EndsWith(s, System.StringComparison.OrdinalIgnoreCase))
                l.Add(k.Substring(0, k.Length - s.Length));
        // ✅ 已复刻（2026-11-04 · B141 · `判据/B141_W130_对账_资源.md` §四·2）：
        //   原版贴图名用**空格**，我们很多文件名用**下划线**（`Card_Booster_Pack_Normal` ↔ 原版 `Card Booster Pack Normal`）
        //   ⇒ 只比下划线名永远匹配不上（`IsOriginalNormalMap` 恒 false ⇒ `OriginalTextureType` 返 Default，
        //   下次烘焙把法线 `textureType: 1` 静默打回 0）。这里再补一档「下划线 → 空格」的形态；
        //   ⛔ 精确名仍排在最前（顺序 = 原样 → 剥后缀 → 空格形态），不会改变已有的精确命中。
        //   ⚠️ 副作用（已记账）：另有 47 个 `Resources/Art` 文件会因此**首次**对回原版真值（mip/wrap/filter），
        //   逐个都是同名同物的下划线写法（`Booster_Pack_Explosion` ↔ `Booster Pack Explosion` 等），
        //   清单见 `报告/B141_W130_ArtBaker.md` §2。
        int n = l.Count;
        for (int i = 0; i < n; i++)
        {
            var spaced = l[i].Replace('_', ' ');
            if (!string.Equals(spaced, l[i], System.StringComparison.Ordinal)) l.Add(spaced);
        }
        return l.ToArray();
    }

    static System.Collections.Generic.Dictionary<string, ArtTexTruth> LoadArtTruth()
    {
        if (_artTruthTried) return _artTruth;
        _artTruthTried = true;
        var d = new System.Collections.Generic.Dictionary<string, ArtTexTruth>();
        // 顺序：vfx 表先、arena 表后（arena 同名覆盖 —— 战场贴图以战场那张表为准）
        foreach (var tsv in new[]
                 {
                     Path.GetFullPath(Path.Combine(Application.dataPath, "../../数据/游戏数据/vfx_texture_mips.tsv")),
                     Path.GetFullPath(Path.Combine(Application.dataPath, "../../数据/游戏数据/arena_texture_import.tsv")),
                 })
        {
            if (!File.Exists(tsv))
            {
                Debug.LogWarning(P + $"读不到原版贴图真值表（{tsv}）⇒ mip/wrap/filter/aniso **一律保留现值**（不会打回已改好的 `.meta`）");
                continue;
            }
            // ✅ 已复刻（2026-11-04 · B141 · `判据/B141_W130_对账_资源.md` §四·1：按表头认列，两张表列序不同）
            int bad = 0, iMip = -1, iWu = -1, iWv = -1, iFm = -1, iAn = -1;
            bool headerRead = false;
            foreach (var line in File.ReadLines(tsv))
            {
                var c = line.Split('\t');
                if (!headerRead)
                {
                    // 第一行 = 表头：**按列名取下标**（vfx 表 `wrapU` 在 5、arena 表在 6）——
                    // 原实现两张表共用 `6/7/8/9` ⇒ vfx 行被读成 `U←wrapV · V←filterMode · filter←aniso · aniso←npotScale`。
                    headerRead = true;
                    iMip = HeaderIndex(c, "mipCount"); iWu = HeaderIndex(c, "wrapU"); iWv = HeaderIndex(c, "wrapV");
                    iFm = HeaderIndex(c, "filterMode"); iAn = HeaderIndex(c, "aniso");
                    if (iMip < 0 || iWu < 0 || iWv < 0 || iFm < 0 || iAn < 0)
                    {
                        Debug.LogWarning(P + $"🔴 贴图真值表 {Path.GetFileName(tsv)} 表头认不出列（mipCount/wrapU/wrapV/filterMode/aniso）⇒ 本表整份跳过（⛔ 不退回固定偏移去猜）");
                        break;
                    }
                    continue;
                }
                if (c.Length < 5 || string.IsNullOrEmpty(c[0])) continue;
                int mip;
                if (iMip >= c.Length || !int.TryParse(c[iMip], out mip)) { bad++; continue; }
                int wu = ParseCol(c, iWu), wv = ParseCol(c, iWv), fm = ParseCol(c, iFm), an = ParseCol(c, iAn);
                d[c[0]] = new ArtTexTruth
                {
                    MipCount = mip,
                    WrapU = wu, WrapV = wv, Filter = fm, Aniso = an,
                    HasWrap = wu >= 0 && wu <= 3 && wv >= 0 && wv <= 3,
                    HasFilter = fm >= 0 && fm <= 2,
                };
            }
            if (bad > 0) Debug.LogWarning(P + $"贴图真值表 {Path.GetFileName(tsv)} 有 {bad} 行 `mipCount` 解析不了（已跳过）");
        }
        Debug.Log(P + $"贴图真值表已载入：{d.Count} 条（vfx_texture_mips + arena_texture_import）");
        _artTruth = d;
        return d;
    }

    static int ParseCol(string[] c, int i)
    {
        if (c == null || i < 0 || i >= c.Length) return -1;
        int v;
        return int.TryParse(c[i], out v) ? v : -1;
    }

    static bool TryFindTruth(string path, out ArtTexTruth row)
    {
        var t = LoadArtTruth();
        var canon = ArtTruthCanonical();
        // ✅ 已复刻（2026-11-04 · B141 · W61 · 判据 = `报告/B141_W130_ArtBakerTruth.md` §5·1 + `vfx_texture_mips.tsv` 原版名）
        //   🔴 第三档 `CanonName`：原版名**混用下划线/空格**（`UI_Deck_Warlord_Acolyte Iconward`），而我们的落盘名把空格全换成 `_`
        //   ⇒ 只比「原样 / 剥后缀 / 全空格」三档必有漏。实测 W40 那 137 张里 **44 张**查得到真值却漏掉（停在现值）。
        //   ⛔ 精确名仍排在最前（顺序 = 原样 → 剥后缀 → 空格形态 → 规范名），不会改变已有命中。
        foreach (var n in OriginalNameCandidates(path))
        {
            if (t != null && t.TryGetValue(n, out row)) return true;
            if (canon.TryGetValue(CanonName(n), out row)) return true;
        }
        row = default(ArtTexTruth);
        return false;
    }

    /// <summary>`_artTruth` 的规范名索引（键 = `CanonName`，忽略大小写）。**首次查表时才建**，
    /// 与 `CompTruthsCanonical()`（同一个文件、同一套做法）一致 —— ⛔ 别写成 `static readonly`：静态字段按**文本顺序**初始化，
    /// 放在 `_artTruth` 前面会是 null。⚠️ 建索引本身是幂等只读，`LoadArtTruth()` 失败时给空表（⇒ 走「表外保留现值」）。</summary>
    static System.Collections.Generic.Dictionary<string, ArtTexTruth> _artTruthCanon;
    static System.Collections.Generic.Dictionary<string, ArtTexTruth> ArtTruthCanonical()
    {
        if (_artTruthCanon != null) return _artTruthCanon;
        var d = new System.Collections.Generic.Dictionary<string, ArtTexTruth>(System.StringComparer.OrdinalIgnoreCase);
        var t = LoadArtTruth();
        if (t != null) foreach (var kv in t) d[CanonName(kv.Key)] = kv.Value;
        _artTruthCanon = d;
        return d;
    }

    public static bool IsOriginalNormalMap(string path)
    {
        foreach (var n in OriginalNameCandidates(path))
            foreach (var nn in NormalMapNames)
                if (string.Equals(n, nn, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>`textureType` —— 原版法线图 **保留现值**（⛔ 不写 Default、也不写 NormalMap，见 15-01 三步），
    /// 其余仍是 `Default`（我们自己按 UV 摆，不要 Sprite）。</summary>
    public static TextureImporterType OriginalTextureType(string path, TextureImporterType current)
        => IsOriginalNormalMap(path) ? current : TextureImporterType.Default;

    /// <summary>`mipmapEnabled` —— 原版 `m_MipCount > 1` 才开；表外**保留现值**。</summary>
    public static bool OriginalMipEnabled(string path, bool current)
    {
        ArtTexTruth r;
        if (!TryFindTruth(path, out r)) return current;
        return r.MipCount > 1;
    }

    /// <summary>wrap 的**三档**判据：`0,0` Repeat · `1,1` Clamp · `(0,1)` 混合（`Shine trail`）。
    /// 真值表 → `RepeatTextures` 老白名单（原版 (0,0)）→ 表外 = false（调用方按旧口径走）。
    /// 🔴 **U/V 必须分开写** —— 原版真有 U≠V 的贴图（`vfx_texture_mips.tsv` 2,984 张里 4 张）。</summary>
    public static bool OriginalWrap(string path, out int u, out int v)
    {
        ArtTexTruth r;
        if (TryFindTruth(path, out r) && r.HasWrap) { u = r.WrapU; v = r.WrapV; return true; }
        if (IsRepeatTexture(path)) { u = 0; v = 0; return true; }
        u = v = -1;
        return false;
    }

    /// <summary>`filterMode` / `anisoLevel` —— 表里有就照原版，没有 = false（调用方保留现值）。</summary>
    static bool OriginalFilterAniso(string path, out FilterMode fm, out int aniso)
    {
        fm = FilterMode.Bilinear; aniso = 1;
        ArtTexTruth r;
        if (!TryFindTruth(path, out r)) return false;
        bool any = false;
        if (r.HasFilter) { fm = (FilterMode)r.Filter; any = true; }
        if (r.Aniso > 0) { aniso = r.Aniso; any = true; }
        return any;
    }

    // ───────── 🆕 2026-11-04（B141 · 15-10）：**战场贴图的压缩档真值** ─────────
    //
    // **为什么要有这一节**：`ApplyArenaImportSettings` 原来**无条件**写 `CompressedHQ`（BC7）⇒
    //   原版是 RGBA32（未压缩）或 DXT1 的贴图被压成 BC7，**比原版低档**（RGBA32 的 alpha 与
    //   噪声细节被块压缩吃掉；DXT1 的 4bpp 变成 8bpp，不是外观问题但与原版不符）。
    // **判据（数值来源）**：
    //   · 原版 `Texture2D.m_TextureFormat`（UnityPy 直读 84 个 `StandaloneWindows64/*.bundle`；
    //     缓存件 `_tmp_view/b139_i/orig_textures.json`，3,009 对象 / 2,984 唯一名，与 `B136_审计_纹理与图集.md` #11 同口径）
    //   · `报告/B136_审计_纹理与图集.md` #11（`Noise Combined` RGBA32 · `ExplosionEmission` BC7 · `MatCap Polish` DXT1）
    //   · 条目判定：`判据/B141_W130_对账_资源.md` §一 15-10
    // **fmt → 导入档映射**：`4`(RGBA32) → `Uncompressed` · `10`(DXT1) → `Compressed` + 显式平台 `DXT1` ·
    //   `25`(BC7) → `CompressedHQ`（= 现状，无需入表）· `12`(DXT5) / `29`(DXT5Crunched) → 我们给的是 BC7，
    //   **比原版高**（`B136` 判为「更高档、不是缺陷」）⇒ 不入表。
    // 🔴 **只列「原版比我们低」的 8 个名字 —— 精确名单是现算的、不是猜的**：
    //   把 `WarpforgeArena1/arenas/**/Textures/*.png` 的 116 个唯一名逐张对回 `orig_textures.json`，
    //   `fmt ∈ {4,10}` 的恰好这 8 个（`fmt 4` × 5 + `fmt 10` × 3，覆盖 22+16 个文件实例；零未命中）。
    //   ⚠️ `ExplosionEmission`（原版 BC7）**不入表** —— 现状 `CompressedHQ` 已与原版同档。
    //   ⚠️ `Noise Combined` 的 PNG 实测 alpha 0..196 ⇒ **必须** Uncompressed（DXT1/DXT5 会吃掉渐变 alpha）；
    //      三个 DXT1 名的 PNG alpha 实测恒 255 ⇒ 显式 DXT1 安全。
    struct ArtCompressionTruth
    {
        public TextureImporterCompression Compression;
        public int ExplicitFormat;   // -1 = 不指定显式平台格式（走压缩档）；否则 = (int)TextureImporterFormat
        /// <summary>原版 `m_TextureFormat` 是 *Crunched（28 DXT1C / 29 DXT5C）时 = true ⇒ 写 `crunchedCompression: 1`（B141 · 15-09）。</summary>
        public bool Crunched;
    }

    static readonly System.Collections.Generic.Dictionary<string, ArtCompressionTruth> CompTruths =
        new System.Collections.Generic.Dictionary<string, ArtCompressionTruth>(System.StringComparer.OrdinalIgnoreCase)
    {
        // 原版 `m_TextureFormat = 4`(RGBA32) ⇒ 未压缩
        { "Noise Combined",               new ArtCompressionTruth { Compression = TextureImporterCompression.Uncompressed, ExplicitFormat = -1 } },
        { "NoiseNRM",                     new ArtCompressionTruth { Compression = TextureImporterCompression.Uncompressed, ExplicitFormat = -1 } },
        { "Default-Particle",             new ArtCompressionTruth { Compression = TextureImporterCompression.Uncompressed, ExplicitFormat = -1 } },
        { "MuzzleFlash_Side",             new ArtCompressionTruth { Compression = TextureImporterCompression.Uncompressed, ExplicitFormat = -1 } },
        { "Matcap Gold Emperos Children", new ArtCompressionTruth { Compression = TextureImporterCompression.Uncompressed, ExplicitFormat = -1 } },
        // 原版 `m_TextureFormat = 10`(DXT1) ⇒ `Compressed` + 显式平台 DXT1（只写 Compressed 时 Unity 会按 alpha 选 DXT5）
        { "CandleFlame",                  new ArtCompressionTruth { Compression = TextureImporterCompression.Compressed, ExplicitFormat = (int)TextureImporterFormat.DXT1 } },
        { "Flame01",                      new ArtCompressionTruth { Compression = TextureImporterCompression.Compressed, ExplicitFormat = (int)TextureImporterFormat.DXT1 } },
        { "MatCap Polish",                new ArtCompressionTruth { Compression = TextureImporterCompression.Compressed, ExplicitFormat = (int)TextureImporterFormat.DXT1 } },
        // ✅ 已复刻（2026-11-04 · B141-W50 · 15-09 · 判据 = `报告/B141_W130_UICompression.md` §6 + 原版 `m_TextureFormat` 逐张）
        // ── 15-09 · UI 切片（`Resources/Art/{ui,ui_menu,ui_deck}`，137 个文件 / 136 个原版名）的压缩档真值 ──
        //   · 为什么必须并入：这 137 张的 `.meta` 已由 `B141-W40` 按原版改好，但不在本表内 ⇒ 下次 `BakeAll` / `BakeFromCLI`
        //     会由 `ApplyImportSettings` 无条件写回 `CompressedHQ`(BC7)，**静默打回**。
        //   · fmt→档（原版 `m_TextureFormat`）：`3`(RGB24)/`4`(RGBA32) ⇒ 未压缩 · `10`(DXT1) ⇒ Compressed+显式 DXT1 ·
        //     `28`(DXT1Crunched) ⇒ 同上 + Crunch · `12`(DXT5) ⇒ Compressed+显式 DXT5 · `29`(DXT5Crunched) ⇒ 同上 + Crunch。
        //   · 🔴 原版名**混用两种分隔符**（`UI_Deck_Warlord_Acolyte Iconward`、`40K_shop_offer_booster_Space Wolves`）：
        //     `OriginalNameCandidates` 只产出「全下划线 / 全空格」两档 ⇒ 136 个名字里 **44 个对不上**（137 个文件只命中 93）。
        //     故 `OriginalCompression` 加第三档 `CanonName`（去掉 `_` 与空格、忽略大小写）—— 三档合计 136/136 全命中、0 歧义。
        //   · ⚠️ 按**原版名**键 ⇒ 同名原版图在别的目录也会一起命中（B141 实测 4 个：`vfx/Shine trail`、
        //     `boosterpack/40K_shop_offer_booster_Space_Wolves`、两处 arena 拷贝）。清单与逐张核对 → `报告/B141_W130_ArtBakerTruth.md` §4。
        // ── 原版 `fmt 3`(RGB24) / `fmt 4`(RGBA32) ⇒ 未压缩（Default `(-1,0,0)` · Standalone `(-1,0,0)`）──
        { "40k_Rewards_icon_base card SDF", UiUncompressed() },
        { "2_40k_cardframe_rarity_rare", UiUncompressed() },
        { "40K_ArmyTrack_bar_position", UiUncompressed() },
        { "40k_battle_Win Skull SDF", UiUncompressed() },
        { "40k_DeckSelection_icon_FactionBlackLegion", UiUncompressed() },
        { "40k_DeckSelection_icon_FactionDarkAngels", UiUncompressed() },
        { "40k_DeckSelection_icon_FactionUM", UiUncompressed() },
        { "40k_Generic Smooth line", UiUncompressed() },
        { "40k_popup_texture", UiUncompressed() },
        { "40k_shop_popup_info_bg", UiUncompressed() },
        { "40k_topmarquee_currency_blackstone", UiUncompressed() },
        { "40k_topmarquee_currency_crystal", UiUncompressed() },
        { "40k_topmarquee_currency_energy", UiUncompressed() },
        { "40k_topmarquee_currency_gold", UiUncompressed() },
        { "40k_topmarquee_currency_ticket", UiUncompressed() },
        { "40k_voicelines_radio_wave equalizer", UiUncompressed() },
        { "Background", UiUncompressed() },
        { "Card Text smooth background", UiUncompressed() },
        { "Crate Border Highlight", UiUncompressed() },
        { "Glow UI W40K", UiUncompressed() },
        { "InputFieldBackground", UiUncompressed() },
        { "OctagonUI Border SDF 2", UiUncompressed() },
        { "Smooth background square", UiUncompressed() },
        { "Tutorial Highlight", UiUncompressed() },
        { "WF_icon_clock for shader", UiUncompressed() },
        // ── 原版 `fmt 10`(DXT1) ⇒ `Compressed` + 显式 DXT1（Standalone 需 `overridden: 1`，否则该块是死值）──
        { "White Square", UiDxt1(false) },
        // ── 原版 `fmt 28`(DXT1Crunched) ⇒ `Compressed` + 显式 DXT1 + `crunchedCompression: 1` ──
        { "40k_main_GameMode_Skirmish", UiDxt1(true) },
        { "Container Image Draft", UiDxt1(true) },
        { "Container Image Ranked", UiDxt1(true) },
        { "Container Image Tutorial", UiDxt1(true) },
        { "Space Wolves Container 1x2", UiDxt1(true) },
        { "UM Guilliman Container 1x2", UiDxt1(true) },
        // ── 原版 `fmt 12`(DXT5) ⇒ `Compressed` + 显式 DXT5 ──
        { "01-Rook", UiDxt5(false) },
        { "02-Veteran", UiDxt5(false) },
        { "03-Commander", UiDxt5(false) },
        { "04-Admiral", UiDxt5(false) },
        { "05-Conqueror", UiDxt5(false) },
        { "06-Galactic Threat", UiDxt5(false) },
        { "07-Legend", UiDxt5(false) },
        { "Alliance_Trophies_AM", UiDxt5(false) },
        { "Alliance_Trophies_Black Legion event 1", UiDxt5(false) },
        { "Alliance_Trophies_BlackLegion", UiDxt5(false) },
        { "Alliance_Trophies_Dark Angels", UiDxt5(false) },
        { "Alliance_Trophies_Default_v2", UiDxt5(false) },
        { "Alliance_Trophies_Emperors Children", UiDxt5(false) },
        { "Alliance_Trophies_Goff", UiDxt5(false) },
        { "Alliance_Trophies_GSC", UiDxt5(false) },
        { "Alliance_Trophies_Leviathan", UiDxt5(false) },
        { "Alliance_Trophies_Ork", UiDxt5(false) },
        { "Alliance_Trophies_SaimHann", UiDxt5(false) },
        { "Alliance_Trophies_Sautekh", UiDxt5(false) },
        { "Alliance_Trophies_Sororitas", UiDxt5(false) },
        { "Alliance_Trophies_Space Marine 2 event", UiDxt5(false) },
        { "Alliance_Trophies_Space Wolves", UiDxt5(false) },
        { "Alliance_Trophies_Tau", UiDxt5(false) },
        { "Alliance_Trophies_TIER1", UiDxt5(false) },
        { "Alliance_Trophies_TIER2", UiDxt5(false) },
        { "Alliance_Trophies_TIER3", UiDxt5(false) },
        { "Alliance_Trophies_TIER4", UiDxt5(false) },
        { "Alliance_Trophies_Ultramarines", UiDxt5(false) },
        { "Alliance_Trophies_Ultramarines event", UiDxt5(false) },
        { "Copy@3x", UiDxt5(false) },
        { "CrosshairTrail", UiDxt5(false) },
        { "LightningTrail", UiDxt5(false) },
        { "Roman I", UiDxt5(false) },
        { "Roman II", UiDxt5(false) },
        { "Roman III", UiDxt5(false) },
        { "Roman IV", UiDxt5(false) },
        { "Roman V", UiDxt5(false) },
        { "Roman VI", UiDxt5(false) },
        { "Shine trail", UiDxt5(false) },
        { "UI Dirt And Noise skratches", UiDxt5(false) },
        // ── 原版 `fmt 29`(DXT5Crunched) ⇒ `Compressed` + 显式 DXT5 + `crunchedCompression: 1` ──
        { "40K_shop_offer_booster_Space Wolves", UiDxt5(true) },
        { "Container Image Practice 1x1", UiDxt5(true) },
        { "UI_Deck_Master Zacharial", UiDxt5(true) },
        { "UI_Deck_Warlord_Abaddon", UiDxt5(true) },
        { "UI_Deck_Warlord_Abaddon_AA_HB", UiDxt5(true) },
        { "UI_Deck_Warlord_Acolyte Iconward", UiDxt5(true) },
        { "UI_Deck_Warlord_Anvirr Keltoc", UiDxt5(true) },
        { "UI_Deck_Warlord_Asmodai", UiDxt5(true) },
        { "UI_Deck_Warlord_AunVa", UiDxt5(true) },
        { "UI_Deck_Warlord_Azrael", UiDxt5(true) },
        { "UI_Deck_Warlord_AzraelAA", UiDxt5(true) },
        { "UI_Deck_Warlord_Beastboss Morgrim", UiDxt5(true) },
        { "UI_Deck_Warlord_Belial", UiDxt5(true) },
        { "UI_Deck_Warlord_Boss Zagstruk", UiDxt5(true) },
        { "UI_Deck_Warlord_Celestian Sacresant Aveline", UiDxt5(true) },
        { "UI_Deck_Warlord_Chaplain Letharius", UiDxt5(true) },
        { "UI_Deck_Warlord_Commander OMaisos", UiDxt5(true) },
        { "UI_Deck_Warlord_Commissar Deckler", UiDxt5(true) },
        { "UI_Deck_Warlord_Daemon Prince", UiDxt5(true) },
        { "UI_Deck_Warlord_Eliac Zephyrblade", UiDxt5(true) },
        { "UI_Deck_Warlord_Erika Luminas", UiDxt5(true) },
        { "UI_Deck_Warlord_Ghazghkull Thraka", UiDxt5(true) },
        { "UI_Deck_Warlord_Ghazghkull_AA_HB", UiDxt5(true) },
        { "UI_Deck_Warlord_Gordrang", UiDxt5(true) },
        { "UI_Deck_Warlord_Grukk Face-Rippa", UiDxt5(true) },
        { "UI_Deck_Warlord_Guilliman", UiDxt5(true) },
        { "UI_Deck_Warlord_Haarken Worldclaimer", UiDxt5(true) },
        { "UI_Deck_Warlord_Hektor Thenmann", UiDxt5(true) },
        { "UI_Deck_Warlord_Imotekh the Stormlord", UiDxt5(true) },
        { "UI_Deck_Warlord_Imotekh_AA_HB", UiDxt5(true) },
        { "UI_Deck_Warlord_Jain Zar", UiDxt5(true) },
        { "UI_Deck_Warlord_Junith Eruita", UiDxt5(true) },
        { "UI_Deck_Warlord_Leontus", UiDxt5(true) },
        { "UI_Deck_Warlord_Lhaska Szenari", UiDxt5(true) },
        { "UI_Deck_Warlord_Lhykhis", UiDxt5(true) },
        { "UI_Deck_Warlord_Logan Grimnar", UiDxt5(true) },
        { "UI_Deck_Warlord_Logan_AA_HB", UiDxt5(true) },
        { "UI_Deck_Warlord_Lord Exultant", UiDxt5(true) },
        { "UI_Deck_Warlord_Lucius", UiDxt5(true) },
        { "UI_Deck_Warlord_Magus", UiDxt5(true) },
        { "UI_Deck_Warlord_Marneus Calgar", UiDxt5(true) },
        { "UI_Deck_Warlord_Medreyal Ghaelyn", UiDxt5(true) },
        { "UI_Deck_Warlord_Morvenn Vahl", UiDxt5(true) },
        { "UI_Deck_Warlord_Mozrog Skragbad", UiDxt5(true) },
        { "UI_Deck_Warlord_Nemesor Zahndrekh", UiDxt5(true) },
        { "UI_Deck_Warlord_Neurothrope", UiDxt5(true) },
        { "UI_Deck_Warlord_Njal Stormcaller", UiDxt5(true) },
        { "UI_Deck_Warlord_Orikan the Diviner", UiDxt5(true) },
        { "UI_Deck_Warlord_Ragnar Blackmane", UiDxt5(true) },
        { "UI_Deck_Warlord_Ramatekh", UiDxt5(true) },
        { "UI_Deck_Warlord_Saffa Rhiannor", UiDxt5(true) },
        { "UI_Deck_Warlord_Shadowsun", UiDxt5(true) },
        { "UI_Deck_Warlord_Swarmlord", UiDxt5(true) },
        { "UI_Deck_Warlord_Sylar Hexcorn", UiDxt5(true) },
        { "UI_Deck_Warlord_Tempestor Prime", UiDxt5(true) },
        { "UI_Deck_Warlord_Tervigon", UiDxt5(true) },
        { "UI_Deck_Warlord_Titus", UiDxt5(true) },
        { "UI_Deck_Warlord_Titus_AA_HB", UiDxt5(true) },
        { "UI_Deck_Warlord_Uriel Ventris", UiDxt5(true) },
        { "UI_Deck_Warlord_Ursula Creed", UiDxt5(true) },
        { "UI_Deck_Warlord_Ursula_AA_HB", UiDxt5(true) },
        { "UI_Deck_Warlord_Valius Paxor", UiDxt5(true) },
        { "UI_Deck_Warlord_Varro Tigurius", UiDxt5(true) },
        { "UI_Deck_Warlord_Winged Tyranid Prime", UiDxt5(true) },
    };

    /// <summary>15-09 · UI 切片的三档构造器（与上面 `CompTruths` 的五个分组一一对应）。</summary>
    static ArtCompressionTruth UiUncompressed() => new ArtCompressionTruth { Compression = TextureImporterCompression.Uncompressed, ExplicitFormat = -1, Crunched = false };
    static ArtCompressionTruth UiDxt1(bool crunched) => new ArtCompressionTruth { Compression = TextureImporterCompression.Compressed, ExplicitFormat = (int)TextureImporterFormat.DXT1, Crunched = crunched };
    static ArtCompressionTruth UiDxt5(bool crunched) => new ArtCompressionTruth { Compression = TextureImporterCompression.Compressed, ExplicitFormat = (int)TextureImporterFormat.DXT5, Crunched = crunched };

    /// <summary>`CompTruths` 的规范名索引（键 = `CanonName`，忽略大小写）。**首次查表时才建** ——
    /// ⛔ 不能写成 `static readonly` 字段：C# 静态字段初始化按**文本顺序**跑，放在 `CompTruths` 前面会是 null。</summary>
    static System.Collections.Generic.Dictionary<string, ArtCompressionTruth> _compTruthsCanon;
    static System.Collections.Generic.Dictionary<string, ArtCompressionTruth> CompTruthsCanonical()
    {
        if (_compTruthsCanon != null) return _compTruthsCanon;
        var d = new System.Collections.Generic.Dictionary<string, ArtCompressionTruth>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var kv in CompTruths) d[CanonName(kv.Key)] = kv.Value;
        _compTruthsCanon = d;
        return d;
    }

    /// <summary>原版名的规范形：去掉 `_` 与空格（`UI_Deck_Warlord_Acolyte Iconward` ⇒ `UIDeckWarlordAcolyteIconward`）。
    /// 为什么要它：原版资产名**混用**下划线/空格，而我们的落盘名把空格全换成了 `_` ⇒ 只比这两档必有漏（B141 实测 44/136 漏）。</summary>
    static string CanonName(string s) => s.Replace("_", "").Replace(" ", "");

    /// <summary>15-10 / 15-09：按**原版名**查压缩档真值；表外 = `CompressedHQ`（原口径，不动）。
    /// 查表三档：① 精确名 ② `OriginalNameCandidates` 的「全下划线 / 全空格」形态 ③ `CanonName`（去 `_` 与空格）。</summary>
    static ArtCompressionTruth OriginalCompression(string path)
    {
        var canon = CompTruthsCanonical();
        foreach (var n in OriginalNameCandidates(path))
        {
            ArtCompressionTruth t;
            if (CompTruths.TryGetValue(n, out t)) return t;
            if (canon.TryGetValue(CanonName(n), out t)) return t;   // ③ 混合分隔符（B141：44/136 只靠这一档）
        }
        return new ArtCompressionTruth { Compression = TextureImporterCompression.CompressedHQ, ExplicitFormat = -1 };
    }

    /// <summary>把真值表的四键（format / compression / crunched / overridden）落到 importer 的 Default 块 + Standalone 块；表外**什么都不做**。
    /// 🔴 **两个块都写**：`.meta` 里 `DefaultTexturePlatform` 那条虽 `overridden: 0`，它**就是**这个平台的真实设置 ⇒
    ///    只写 Standalone 会让 Default 块停在 `textureFormat -1 / textureCompression 2`(BC7)，与原版不符。
    /// 🔴 **Default 块走 `Get/SetDefaultPlatformTextureSettings`，⛔ 别用 `ti.textureFormat`** —— 后者已 **obsolete(CS0618)**，
    ///    原话「textureFormat is no longer accessible at the TextureImporter level … Using this setter will setup various
    ///    parameters to match the new automatic system as well as possible」⇒ 它会**顺带改别的键**，不是直写 `textureFormat` 那一格。
    /// ⚠️ `maxTextureSize` 从 `ti.maxTextureSize` 回填：调用方（`ApplyImportSettings`）刚设过它（`ui_menu` 要 2048），
    ///    不能让 Default 块的这一格被旧快照盖回去。
    /// ⚠️ Standalone 只在**有显式格式**时 `overridden: 1`（未压缩那 26 张保持 0，与盘上 `.meta` 逐张一致）。</summary>
    static void ApplyCompressionTruth(TextureImporter ti, string path)
    {
        var t = OriginalCompression(path);
        if (t.Compression == TextureImporterCompression.CompressedHQ && t.ExplicitFormat < 0 && !t.Crunched) return;   // 表外：原口径，什么都不做

        // ── Default 块（⚠️ 顺序：先顶层 compression / crunched，再把 Default 平台块整份写一遍 —— 让它**最后落笔**，
        //    这样 `format` 不会被别的 setter 顺带重置回 Automatic）──
        ti.textureCompression = t.Compression;
        ti.crunchedCompression = t.Crunched;
        var def = ti.GetDefaultPlatformTextureSettings();
        if (string.IsNullOrEmpty(def.name)) def.name = "DefaultTexturePlatform";   // 兜底：Set 靠这个字符串认平台（空名会抛，整轮烘焙就断在这儿）
        def.textureCompression = t.Compression;
        def.crunchedCompression = t.Crunched;
        def.format = t.ExplicitFormat >= 0 ? (TextureImporterFormat)t.ExplicitFormat : TextureImporterFormat.Automatic;
        def.maxTextureSize = ti.maxTextureSize;   // 不让上面的 2048 被旧快照盖回 1024
        ti.SetPlatformTextureSettings(def);

        // ── Standalone 块 ──
        var ps = ti.GetPlatformTextureSettings("Standalone");
        ps.textureCompression = t.Compression;
        ps.crunchedCompression = t.Crunched;
        ps.format = t.ExplicitFormat >= 0 ? (TextureImporterFormat)t.ExplicitFormat : TextureImporterFormat.Automatic;
        if (t.ExplicitFormat >= 0) ps.overridden = true;
        ti.SetPlatformTextureSettings(ps);
    }

    /// <summary>表头里的列名 → 下标（大小写不敏感、去首尾空白；找不到 = -1）。
    /// ⛔ **别退回硬编码偏移** —— `vfx_texture_mips.tsv` 与 `arena_texture_import.tsv` 列序不同（差一列 `colorSpace`）。</summary>
    static int HeaderIndex(string[] header, string name)
    {
        for (int i = 0; i < header.Length; i++)
            if (string.Equals(header[i].Trim(), name, System.StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }
}
