// ArtBaker.cs — 把「原版复刻」要用的美术烘进 Resources/Art
//
// 两件事：
//   1. **烘战场背景**：打开 `WarpforgeArena1/Scenes/battlearena1.unity`（原版 battlearena1 重建场景），
//      用它的相机渲一张 1920×1080 存成 `Resources/Art/arena1_bg.png`。
//      批处理下粒子不会自己走，渲之前先 `Simulate` 几秒，否则烟/火全是空的。
//   2. **统一导入设置**：卡框要 Read/Write（`CardView` 运行时量它的不透明包围盒来对齐 UV）、
//      全部关 mipmap、开 alphaIsTransparency。
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
    /// ⚠️ 原版数据只给了 `m_WrapU/V`，**`m_WrapW` 不在判据里** ⇒ 这里也只写 U/V，`.meta` 的 `wrapW` 保持 1。
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
    };

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

            if (ApplyAudioRule(ai, load, fmt, preload)) n++;
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
                               AudioCompressionFormat fmt, bool preload)
    {
        var s = ai.defaultSampleSettings;
        bool changed = s.loadType != load
                    || s.compressionFormat != fmt
                    || s.preloadAudioData != preload
                    || s.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate
                    || !ai.loadInBackground
                    || ai.forceToMono;
        if (!changed) return false;

        s.loadType = load;
        s.compressionFormat = fmt;
        s.preloadAudioData = preload;
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        // ⚠️ `s.quality` **不动**：判不出原版值（不在 AudioClip 里），现状 1.0 —— 报告 §2·4。
        ai.defaultSampleSettings = s;
        ai.loadInBackground = true;    // 原版 2434 条全是 true（我们原来一条都没设，全是 0）
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

            ti.textureType = TextureImporterType.Default;      // 我们自己按 UV 摆，不要 Sprite
            // 🔴 **必须关掉 `alphaIsTransparency`**（2026-09-15 踩到）：
            //    开着它时 Unity 会把**透明区的 RGB 用最近邻填充补上**，而填充的划分边界正好是
            //    **多边形格子** ⇒ 整张立绘变成**彩色马赛克**（实测卡面糊成一块块的）。
            //    我们**恰恰要用透明区的 RGB** —— 底层 `ArtOpaque` 拿它补卡框拱窗里的背景。
            //    这条判据与 `工具/import_original_art.py` 的 `fix_art_meta()` **是同一条**
            //    （那个脚本直接改 `.meta` 文本）—— 这里也设一遍，免得**两个工具互相打架**：
            //    我这次就是跑了本方法把 `fix_art_meta` 关掉的开关又打开了，卡面当场变马赛克。
            ti.alphaIsTransparency = false;
            ti.mipmapEnabled = false;
            // 🔴 **2026-10-22：Repeat 白名单放行** —— 原版那 7 张（见 `RepeatTextures` 的表）是 Repeat，
            //   这里若仍无条件 Clamp，会把手工改好的 `.meta` **静默改回 Clamp**（本方法每次烘焙都会跑）。
            //   白名单之外**仍然是 Clamp**（`CardBlobShadow.png` 原版就是 Clamp，别一起改）。
            if (IsRepeatTexture(path))
            {
                ti.wrapModeU = TextureWrapMode.Repeat;
                ti.wrapModeV = TextureWrapMode.Repeat;
            }
            else
            {
                ti.wrapMode = TextureWrapMode.Clamp;
            }
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = isBackdrop ? 2048 : (path.Contains("/ui_deck/") ? 2048 : 1024);
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
            //   （同用法先例 = 本文件 `ApplyArenaImportSettings`：原版那批战场图是 BC7(fmt=29) ⇒ `CompressedHQ`）。
            //   ⚠️ `isReadable` 那一行**不动**：`CardView.FrameUv` 对 `frame_uv.json` 里查不到的卡框
            //   仍要退回 `GetPixels32()`（运行时回读），关掉 Read/Write 会让那条兜底静默失效。
            //   ⚠️ **本行只改「以后导入」**：现存的 `.meta` 要重跑本方法（菜单 / `-executeMethod`）才生效。
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }
        Debug.Log(P + "导入设置已重设");
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

            ti.textureType = TextureImporterType.Default;
            ti.maxTextureSize = cap;
            ti.mipmapEnabled = false;                      // 照原版（atlas/floor 的 mipCount = 1）
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaIsTransparency = true;                 // ⚠️ 与卡牌立绘**相反**（见上）
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Repeat;
            // 原版那几张是 BC7（fmt=29）—— 对应 `CompressedHQ`。用普通 `Compressed`（DXT）会把
            // 大面积渐变压出块状色带。
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            // 🔴 **2026-09-22 补：平台覆盖也要一起设** ——
            //    meta 里除了顶层 `maxTextureSize`，还有一条 **`buildTarget: Standalone`** 的覆盖块，
            //    它带着自己的 `maxTextureSize`（我们这份一直是 **2048**）。
            //    顶层改成 4096 之后编辑器里**未必生效**（实测：改完顶层，导入后仍是 2048）。
            //    ⇒ 两边都写，并用下面的**实读判据**确认。
            try
            {
                var ps = ti.GetPlatformTextureSettings("Standalone");
                ps.maxTextureSize = cap;
                ps.textureCompression = TextureImporterCompression.CompressedHQ;
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
}
