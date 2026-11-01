// B114_B204_AudioProbe.cs — `B-204`：**Unity/FMOD 到底看不看【中间页 granule】**的三变体读数探针。
//
// ============================ 为什么单开一份 ============================
//  · 背景判据 = `Unity/资料/完全复刻/判据/B112W_B197_granule假说.md`。它的结论是：
//    那 450 份备份的 **Vorbis 包流与 FSB5 真值同一份**，错的只有 **AssetRipper 写给「非末页」的
//    granule**（一律 `31,872` = `(250 段 − 1) × 128`，而且是**逐页重置、不累计**）；
//    把中间页 granule 改成真值（**每份只动 5–14 个字节、大小不变**）之后，
//    vgmstream / libsndfile / ffmpeg 三条解码路径**全部** `corr = 1.000000`。
//  · 但**FMOD 属于哪一类是未知的**（那一轮按红线没跑 Unity）：
//      ① 若 FMOD **像 ffmpeg native**（不看中间页 granule）⇒ 那 450 份**在游戏里本来就是对的**，
//         第四波「重编码成 q8、+24.73 MB」整批多余 ⇒ **该回滚**；
//      ② 若 FMOD **像 vgmstream / libsndfile**（看）⇒ 那 450 份**确实播错** ⇒ **该改用 granule 修**。
//    这两档的收益差很大（回滚 = 白赚 24.73 MB 且恢复逐样点无损），所以必须**实测一次**再决定。
//  · 本文件只做**读数**，不做断言、不改任何既有资产 —— 判是调度台/用户的活。
//
// ============================ 三变体 ============================
//   A = 备份原样           `_tmp_view/b112_r1w_backup/mono_setup/vo/*.ogg`（末页 granule 已对、中间页仍是 31,872）
//   B = 只改中间页 granule `_tmp_view/b114_w4/fix/vo/*.ogg`（A 的逐字节副本，仅 granule + CRC 变动，**大小不变**）
//   C = 现盘上的 q8 重编码 `CardPresentation/Resources/Art/audio/vo/*.ogg`（第四波产物）
//   ⛔ **B 不在本文件里生成**：Ogg 页解析 + CRC 重算是离线脚本的活（已经做过，
//      `_tmp_view/b114_w4/gen_b.py` 复用 `b112w_b197/batch2.py::fix_all` 的那套算法）。
//      C# 里只负责「**把三份丢进 Unity，让 FMOD 解一遍，看解出什么**」。
//
// ============================ 判读口径（重要）============================
//  🔴 **`AudioClip.samples` 不足以判**：实测 A 与 B 用 vgmstream 解出的**总样点数完全相同**
//    （都是末页 granule），差别全在**中间那一段的内容被前移/错位**上
//    （实测 `Sautekh_Orikan_gen3` 首个不同样点 = 第 164,352 帧，正好是 page2 边界 164,928 附近；
//      A vs B 的 corr 只有 0.29–0.81）。
//    ⇒ 真正的判别量 = **解码后 PCM 的逐样点校验和**（本探针为此给 FNV-1a 64 + rms + peak + 两两 corr）。
//  🔴 **`A == B` ⇒ FMOD 不看中间页 granule**（结论 ①，可回滚）；
//    **`A != B` ⇒ FMOD 看**（结论 ②，改用 granule 修）。
//    两种情况下 `B` 都是**无损**的，所以即使落在 ②，B 也严格优于 C（C 是二次有损 + 体积 ×1.6–3.3）。
//
// ============================ 怎么跑（批处理，串行）============================
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod B114_B204_AudioProbe.RunFromCLI -logFile -
//
//   默认 **抽样 20 条**（等距）。全量 450 条：
//     … -executeMethod B114_B204_AudioProbe.RunFromCLI **-b204all** -logFile -
//   改抽样条数：`-b204n 50`
//   改目录（少见）：`-b204A <dir> -b204B <dir> -b204C <dir>`（绝对路径或 `Assets/…` 相对路径都行）
//   调试时保留临时目录不删：`-b204keep`
//   ⚠️ 判读数看 `B204` 前缀那几行，**不看退出码**（本探针不做断言，恒 0）。
//   ⚠️ `--all`（450 份 × 3 = 1350 次导入 + 解码）会跑很久，且 `Resources.UnloadAsset` 之后仍可能
//      有编辑器侧缓存；先跑默认 20 条看结论，需要更稳的统计再 `-b204all`。
//
// ============================ 临时目录 ============================
//   `Assets/_tmp_b204/{A,B,C}/<文件名>.ogg`（Unity **只导入 `Assets/` 下的文件**，所以必须进 Assets）。
//   跑完 `AssetDatabase.DeleteAsset("Assets/_tmp_b204")` + 兜底删残留，并打印删了多少个文件。
//   开工前也会先清一次（防上次崩在半路留下残骸）。⛔ 不要把它签进 git（`.gitignore` 里没有这条，
//   所以本文件负责「跑完必删」；若你在中途 Ctrl-C，请手动删掉 `Assets/_tmp_b204`）。

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class B114_B204_AudioProbe
{
    const string Tag = "B204";
    const string StageAssetDir = "Assets/_tmp_b204";
    const int DefaultSampleCount = 20;
    const long MaxSamplesPerClip = 50L * 1000 * 1000;   // 防御：异常头字段会把分配撑爆

    static readonly string[] Variants = { "A", "B", "C" };

    /// <summary>批处理入口（`-executeMethod B114_B204_AudioProbe.RunFromCLI`）。</summary>
    public static void RunFromCLI() { Run(); }

    // ---------------------------------------------------------------- 主流程
    public static void Run()
    {
        var ci = CultureInfo.InvariantCulture;
        bool keep = HasFlag("-b204keep");
        bool all = HasFlag("-b204all") || HasFlag("--all");
        int n = DefaultSampleCount;
        string nArg = ArgValue("-b204n");
        if (!string.IsNullOrEmpty(nArg) && int.TryParse(nArg, NumberStyles.Integer, ci, out int parsed) && parsed > 0)
            n = parsed;

        // ---- 目录 -------------------------------------------------------
        string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));   // d:/4
        string dirA = AbsPath(ArgValue("-b204A") ?? Path.Combine(repo, "_tmp_view", "b112_r1w_backup", "mono_setup", "vo"));
        string dirB = AbsPath(ArgValue("-b204B") ?? Path.Combine(repo, "_tmp_view", "b114_w4", "fix", "vo"));
        string dirC = AbsPath(ArgValue("-b204C") ?? Path.Combine(Application.dataPath, "CardPresentation", "Resources", "Art", "audio", "vo"));
        _dirA = dirA; _dirB = dirB; _dirC = dirC;   // `SourcePath` 用

        Log($"==== {Tag} B-204 三变体读数（Unity/FMOD 中间页 granule）====");
        Log($"{Tag} DIR A={dirA}");
        Log($"{Tag} DIR B={dirB}");
        Log($"{Tag} DIR C={dirC}");
        Log($"{Tag} Unity {Application.unityVersion} · 抽样 {n} 条 · all={all} · keep={keep}");

        // ---- 清残骸 + 收集名单 ------------------------------------------
        PurgeStage();
        var names = new List<string>();
        var missB = new List<string>(); var missC = new List<string>();
        if (!Directory.Exists(dirA)) { LogError($"{Tag} ✗ 目录 A 不存在：{dirA}"); }
        if (!Directory.Exists(dirB)) { LogError($"{Tag} ✗ 目录 B 不存在：{dirB}"); }
        if (!Directory.Exists(dirC)) { LogError($"{Tag} ✗ 目录 C 不存在：{dirC}"); }

        foreach (var f in SafeListOgg(dirA))
        {
            string nm = Path.GetFileName(f);
            bool inB = File.Exists(Path.Combine(dirB, nm));
            bool inC = File.Exists(Path.Combine(dirC, nm));
            if (!inB) missB.Add(nm);
            if (!inC) missC.Add(nm);
            if (inB && inC) names.Add(nm);
        }
        names.Sort(StringComparer.Ordinal);
        Log($"{Tag} 名单：A={SafeListOgg(dirA).Count} 三边都有={names.Count} "
          + $"A有B无={missB.Count} A有C无={missC.Count}");
        if (names.Count == 0) { LogError($"{Tag} ✗ 没有任何一份文件三边齐全，停手"); return; }

        var picked = Pick(names, all ? names.Count : Math.Min(n, names.Count));

        // ---- 拷贝到 Assets 下 -------------------------------------------
        int copied = 0;
        for (int i = 0; i < picked.Count; i++)
        {
            string nm = picked[i];
            copied += Stage(dirA, "A", nm) ? 1 : 0;
            copied += Stage(dirB, "B", nm) ? 1 : 0;
            copied += Stage(dirC, "C", nm) ? 1 : 0;
        }
        Log($"{Tag} 已拷入临时目录 {copied} 个文件，开始导入 …");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        // ---- 逐文件读数 -------------------------------------------------
        int okA = 0, okB = 0, okC = 0;
        int abSame = 0, abCmp = 0, abCorrN = 0; double abCorrMin = 9, abCorrSum = 0, abCorrMax = -9; double abMaxDiff = 0;
        int acSame = 0, acCmp = 0, acCorrN = 0; double acCorrMin = 9, acCorrSum = 0, acCorrMax = -9; double acMaxDiff = 0;
        int bcCmp = 0, bcCorrN = 0; double bcCorrMin = 9, bcCorrSum = 0, bcCorrMax = -9;
        int abLenDiff = 0;

        for (int i = 0; i < picked.Count; i++)
        {
            string nm = picked[i];
            var r = new Dictionary<string, Reading>();
            foreach (var v in Variants)
            {
                var rd = Measure(v, nm);
                r[v] = rd;
                if (rd.Err == null)
                {
                    if (v == "A") okA++; else if (v == "B") okB++; else okC++;
                }
                Log($"{Tag} FILE | {nm} | {v} | bytes={rd.Bytes} | unity_samples={rd.Samples} | ch={rd.Channels} "
                  + $"| freq={rd.Freq} | length={rd.Length.ToString("F6", ci)} | fnv={rd.Hash ?? "-"} "
                  + $"| rms={(rd.Rms >= 0 ? rd.Rms.ToString("F6", ci) : "-")} | peak={(rd.Peak >= 0 ? rd.Peak.ToString("F6", ci) : "-")}"
                  + (rd.Err != null ? $" | ERR={rd.Err}" : ""));
            }

            // AvsB
            var a = r["A"]; var b = r["B"]; var c = r["C"];
            if (a.Err == null && b.Err == null)
            {
                abCmp++;
                bool same = a.Hash != null && a.Hash == b.Hash;
                double corr = Compare(a, b, out double md);
                if (same) abSame++;
                if (a.Samples != b.Samples) abLenDiff++;
                if (corr > -8) { abCorrN++; abCorrSum += corr; if (corr < abCorrMin) abCorrMin = corr; if (corr > abCorrMax) abCorrMax = corr; }
                if (md > abMaxDiff) abMaxDiff = md;
                Log($"{Tag} CMP  | {nm} | AvsB | samples={a.Samples}/{b.Samples} | hash_same={same} "
                  + $"| corr={F(corr, ci)} | maxdiff={md.ToString("F8", ci)} | bytes={a.Bytes}/{b.Bytes}");
            }
            if (a.Err == null && c.Err == null)
            {
                acCmp++;
                bool same = a.Hash != null && a.Hash == c.Hash;
                double corr = Compare(a, c, out double md);
                if (same) acSame++;
                if (corr > -8) { acCorrN++; acCorrSum += corr; if (corr < acCorrMin) acCorrMin = corr; if (corr > acCorrMax) acCorrMax = corr; }
                if (md > acMaxDiff) acMaxDiff = md;
                Log($"{Tag} CMP  | {nm} | AvsC | samples={a.Samples}/{c.Samples} | hash_same={same} "
                  + $"| corr={F(corr, ci)} | maxdiff={md.ToString("F8", ci)} | bytes={a.Bytes}/{c.Bytes}");
            }
            if (b.Err == null && c.Err == null)
            {
                bcCmp++;
                double corr = Compare(b, c, out _);
                if (corr > -8) { bcCorrN++; bcCorrSum += corr; if (corr < bcCorrMin) bcCorrMin = corr; if (corr > bcCorrMax) bcCorrMax = corr; }
                Log($"{Tag} CMP  | {nm} | BvsC | samples={b.Samples}/{c.Samples} | corr={F(corr, ci)}");
            }

            // 释放，避免 `--all` 时把 1350 条 PCM 全驻留
            foreach (var v in Variants) Release(v, nm);
            a.Pcm = b.Pcm = c.Pcm = null;
        }

        // ---- 汇总 -------------------------------------------------------
        Log($"{Tag} SUMMARY files={picked.Count} variants_ok A={okA} B={okB} C={okC}");
        Log($"{Tag} SUMMARY AvsB compared={abCmp} hash_identical={abSame} len_differs={abLenDiff} "
          + $"corr[min/mean/max]={F(abCorrMin, ci)}/{F(abCorrN > 0 ? abCorrSum / abCorrN : -9, ci)}/{F(abCorrMax, ci)} "
          + $"maxdiff={abMaxDiff.ToString("F8", ci)}");
        Log($"{Tag} SUMMARY AvsC compared={acCmp} hash_identical={acSame} "
          + $"corr[min/mean/max]={F(acCorrMin, ci)}/{F(acCorrN > 0 ? acCorrSum / acCorrN : -9, ci)}/{F(acCorrMax, ci)} "
          + $"maxdiff={acMaxDiff.ToString("F8", ci)}");
        Log($"{Tag} SUMMARY BvsC compared={bcCmp} "
          + $"corr[min/mean/max]={F(bcCorrMin, ci)}/{F(bcCorrN > 0 ? bcCorrSum / bcCorrN : -9, ci)}/{F(bcCorrMax, ci)}");

        if (abCmp > 0)
        {
            bool fmIgnore = abSame == abCmp;
            Log($"{Tag} VERDICT 结论{(fmIgnore ? "①" : "②")}："
              + (fmIgnore
                 ? "**A 与 B 的 PCM 逐样点相同 ⇒ FMOD 不看中间页 granule** ⇒ 那 450 份在游戏里本来就是对的，"
                   + "第四波重编码（+24.73 MB、二次有损）**整批多余，该回滚**"
                 : $"**A 与 B 的 PCM 不同（{abCmp - abSame}/{abCmp} 份不同）⇒ FMOD 会看中间页 granule** ⇒ 那 450 份在游戏里确实播错，"
                   + "**该改用「只改 granule」（B）而不是重编码（C）**"));
            if (!fmIgnore)
                Log($"{Tag} VERDICT 附注：BvsC 的 corr 若接近 1，说明现有 q8 在**时间轴**上已经把错位移修过来了"
                  + "（只是二次有损 + 体积大）；可用 `-b204all` 看全量分布再定回滚范围。");
        }
        else LogError($"{Tag} VERDICT 无有效 AvsB 读数 —— 导入/解码失败，结论不成立");

        // ---- 收尾 -------------------------------------------------------
        if (keep) Log($"{Tag} -b204keep 生效，临时目录保留：{StageAssetDir}");
        else Cleanup();
        Log($"{Tag} DONE");
    }

    // ---------------------------------------------------------------- 读数
    class Reading
    {
        public string Variant, Name, Hash, Err;
        public long Bytes = -1;
        public int Samples = -1, Channels = -1, Freq = -1;
        public double Length = -1, Rms = -1, Peak = -1;
        public float[] Pcm;
    }

    static Reading Measure(string variant, string name)
    {
        var rd = new Reading { Variant = variant, Name = name };
        string src = SourcePath(variant, name);
        string assetPath = StageAssetDir + "/" + variant + "/" + name;
        try
        {
            if (File.Exists(src)) rd.Bytes = new FileInfo(src).Length;

            // 导入设置：只要不是「解压到内存 + 保留采样率」，`GetData` 就可能拿不到 PCM
            //   （`Streaming` 下 `GetData` 直接返回 false；`OverrideSampleRate` 会改样点数 ⇒ 三个变体不再可比）。
            var ai = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (ai == null) { rd.Err = "AudioImporter==null（导入没成功？）"; return rd; }
            var s = ai.defaultSampleSettings;
            bool dirty = false;
            if (s.loadType != AudioClipLoadType.DecompressOnLoad) { s.loadType = AudioClipLoadType.DecompressOnLoad; dirty = true; }
            if (s.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate) { s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate; dirty = true; }
            if (ai.forceToMono) { ai.forceToMono = false; dirty = true; }
            if (dirty)
            {
                ai.defaultSampleSettings = s;
                ai.SaveAndReimport();
                ai = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (ai != null) s = ai.defaultSampleSettings;
            }
            if (!_loggedSettings)
            {
                _loggedSettings = true;
                Log($"{Tag} IMPORTER 首个文件实际生效：loadType={s.loadType} sampleRateSetting={s.sampleRateSetting} "
                  + $"compressionFormat={s.compressionFormat} quality={s.quality.ToString("F2", CultureInfo.InvariantCulture)} "
                  + $"forceToMono={(ai != null && ai.forceToMono)} preload={s.preloadAudioData} "
                  + $"| 三个变体都强制 DecompressOnLoad + PreserveSampleRate ⇒ 只比「FMOD 解出来的东西」");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null) { rd.Err = "LoadAssetAtPath<AudioClip> == null"; return rd; }

            rd.Samples = clip.samples;
            rd.Channels = clip.channels;
            rd.Freq = clip.frequency;
            rd.Length = clip.length;

            long total = (long)clip.samples * Math.Max(1, clip.channels);
            if (total <= 0 || total > MaxSamplesPerClip)
            {
                rd.Err = $"样点数异常 {total}（samples={clip.samples} ch={clip.channels}）";
                return rd;
            }

            var buf = new float[total];
            if (!clip.GetData(buf, 0)) { rd.Err = "clip.GetData 返回 false"; return rd; }

            rd.Pcm = buf;
            rd.Hash = Fnv1a64(buf).ToString("x16", CultureInfo.InvariantCulture);
            double sum2 = 0, peak = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                double x = buf[i];
                sum2 += x * x;
                double ax = Math.Abs(x);
                if (ax > peak) peak = ax;
            }
            rd.Rms = Math.Sqrt(sum2 / buf.Length);
            rd.Peak = peak;
            return rd;
        }
        catch (Exception e) { rd.Err = e.GetType().Name + ": " + e.Message; return rd; }
    }

    static double Compare(Reading x, Reading y, out double maxDiff)
    {
        maxDiff = -1;
        if (x.Pcm == null || y.Pcm == null) return -9;
        int n = Math.Min(x.Pcm.Length, y.Pcm.Length);
        if (n < 2) return -9;
        double sa = 0, sb = 0, saa = 0, sbb = 0, sab = 0, md = 0;
        for (int i = 0; i < n; i++)
        {
            double a = x.Pcm[i], b = y.Pcm[i];
            sa += a; sb += b; saa += a * a; sbb += b * b; sab += a * b;
            double d = Math.Abs(a - b);
            if (d > md) md = d;
        }
        maxDiff = md;
        double va = saa - sa * sa / n, vb = sbb - sb * sb / n;
        double cov = sab - sa * sb / n;
        if (va <= 1e-18 || vb <= 1e-18) return (md == 0 ? 1.0 : 0.0);   // 静音段：方差为 0，用「是否逐位相同」定性
        return cov / Math.Sqrt(va * vb);
    }

    // ---------------------------------------------------------------- 杂项
    static string SourcePath(string variant, string name)
    {
        string dir = variant == "A" ? _dirA : variant == "B" ? _dirB : _dirC;
        return Path.Combine(dir, name);
    }

    static string _dirA, _dirB, _dirC;
    static bool _loggedSettings;

    static string F(double v, CultureInfo ci) => v <= -8.5 ? "n/a" : v.ToString("F6", ci);

    static ulong Fnv1a64(float[] buf)
    {
        ulong h = 14695981039346656037UL;
        for (int i = 0; i < buf.Length; i++)
        {
            int bits = BitConverter.SingleToInt32Bits(buf[i]);
            h = (h ^ (byte)(bits & 0xFF)) * 1099511628211UL;
            h = (h ^ (byte)((bits >> 8) & 0xFF)) * 1099511628211UL;
            h = (h ^ (byte)((bits >> 16) & 0xFF)) * 1099511628211UL;
            h = (h ^ (byte)((bits >> 24) & 0xFF)) * 1099511628211UL;
        }
        return h;
    }

    static List<string> Pick(List<string> all, int n)
    {
        if (n >= all.Count) return all;
        var o = new List<string>(n);
        if (n == 1) { o.Add(all[0]); return o; }
        for (int i = 0; i < n; i++) o.Add(all[(int)((long)i * (all.Count - 1) / (n - 1))]);
        return o;
    }

    static List<string> SafeListOgg(string dir)
    {
        var o = new List<string>();
        if (!Directory.Exists(dir)) return o;
        foreach (var f in Directory.GetFiles(dir, "*.ogg", SearchOption.TopDirectoryOnly)) o.Add(f);
        o.Sort(StringComparer.Ordinal);
        return o;
    }

    static bool Stage(string srcDir, string variant, string name)
    {
        string src = Path.Combine(srcDir, name);
        if (!File.Exists(src)) return false;
        string dstDir = Path.Combine(Application.dataPath, "_tmp_b204", variant);
        Directory.CreateDirectory(dstDir);
        File.Copy(src, Path.Combine(dstDir, name), true);
        return true;
    }

    static void Release(string variant, string name)
    {
        try
        {
            string assetPath = StageAssetDir + "/" + variant + "/" + name;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null) Resources.UnloadAsset(clip);
        }
        catch (Exception e) { Log($"{Tag} WARN 释放 {variant}/{name} 失败：{e.Message}（不影响读数）"); }
    }

    static void PurgeStage() { CleanupQuiet("上次残留"); }

    static void Cleanup()
    {
        int n = CleanupQuiet("临时目录");
        Log($"{Tag} 清理：删除 {n} 个文件 + 目录 `{StageAssetDir}`");
    }

    static int CleanupQuiet(string what)
    {
        int deleted = 0;
        string abs = Path.Combine(Application.dataPath, "_tmp_b204");
        if (!Directory.Exists(abs) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(StageAssetDir) == null) return 0;
        try
        {
            // 先让 AssetDatabase 删（它连带删 .meta），再兜底删残留
            if (AssetDatabase.IsValidFolder(StageAssetDir))
            {
                AssetDatabase.DeleteAsset(StageAssetDir);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }
        catch (Exception e) { LogError($"{Tag} DeleteAsset 失败：{e.Message}"); }
        if (Directory.Exists(abs))
        {
            try
            {
                foreach (var f in Directory.GetFiles(abs, "*", SearchOption.AllDirectories)) deleted++;
                Directory.Delete(abs, true);
            }
            catch (Exception e) { LogError($"{Tag} 兜底删除失败：{e.Message}"); }
        }
        // `Assets/_tmp_b204.meta`（父目录的 meta）也可能留下
        string parentMeta = Path.Combine(Application.dataPath, "_tmp_b204.meta");
        if (File.Exists(parentMeta)) { try { File.Delete(parentMeta); deleted++; } catch { } }
        if (deleted > 0) Log($"{Tag} ({what}) 兜底删掉 {deleted} 个残留文件");
        return deleted;
    }

    static string AbsPath(string p)
    {
        if (string.IsNullOrEmpty(p)) return p;
        p = p.Replace('\\', '/');
        if (p.Length > 1 && p[1] == ':') return p;                 // 已经是绝对路径
        if (p.StartsWith("/")) return p;
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", p));
    }

    static string ArgValue(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }

    static bool HasFlag(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static void Log(string s) { Debug.Log(s); }
    static void LogError(string s) { Debug.LogError(s); }
}
