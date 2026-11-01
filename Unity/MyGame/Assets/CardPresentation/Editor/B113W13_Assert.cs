// B113W13_Assert.cs — 第 113 会话第 1 波写手代理 `W13` 的断言（2026-11-01 · 账 `B-119`）
//
// ============================ 本件挡什么 ============================
//  · `B-119` —— 战场那 7 张调色 LUT（`Assets/WarpforgeArena1/Textures/LUT/LUT Battle Arena *.png`）
//    的 wrap 必须是 **Repeat**，而且**下次建场 / 重导不许再改回 Clamp**。
//
// ============================ 判据（2026-11-01 现读，⛔ 不是从旧文档抄的）============================
//  ① 原版导入设置：`d:/2/tools/assetripper_out/ExportedProject/Assets/Texture2D/LUT Battle Arena <场>.png.meta`
//     七份**全是** `textureSettings:` → `wrapU: 0` / `wrapV: 0` / `wrapW: 0`（= `TextureWrapMode.Repeat`）。
//  ② 第二来源（包内实读）：`数据/游戏数据/arena_texture_import.tsv:62-68` 那七行 `wrapU=0 / wrapV=0`
//     （← `工具/gen_arena_texture_import.py` 用 UnityPy 读源 bundle 的 `m_TextureSettings`，不是抄 meta）。
//  ③ 枚举值：`Repeat = 0` / `Clamp = 1`（`d:/2/tools/il2cpp_out/dump.cs` 的 `TextureWrapMode`）。
//  ④ 生成器：唯一的写手是 `ArenaBuilder.EnsureLutImportSettings`（`ApplyOriginalImport` 的 LUT 分支
//     **只核对不写**并 `return false`；`ApplyTextureImportSettings` 因此对它 `continue`）。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//  · A 组读的是**产物**：盘上 7 份 `.meta` 的文本。把任一 `wrapU/V/W` 改回 `1` ⇒ 对应那条红。
//  · B 组读的是**生成器**：`ArenaBuilder.cs` 里 `EnsureLutImportSettings` 的**方法体源码文本**。
//    把 `Repeat` 改回 `Clamp` ⇒ 对应那条红。
//    ⚠️ A/B 读**两个不同文件、两个不同方向**（产物 vs 生成器）⇒ 一次改动只能修好/弄坏其中一边：
//       「只改 `.meta` 没改代码」= B 红；「只改代码没重导」= A 红。**结构上不可能被一个错误同时骗过**。
//  · C 组把**合成输入**喂给两个纯函数（`WrapOf` / `ExtractMethodBody`）—— 它们必须能把 `1` 读成 `1`、
//    把「找不到」读成哨兵、把带 `Clamp` 的合成方法体抠出来；否则 A/B 两组会**恒真**（本仓最常踩的「断言自证」）。
//  · D 组数文件个数（7）—— 防「改名/删掉之后 A 组只检查了剩下的几份 → 悄悄变绿」。
//  · E 组回读**原版**那 7 份 `.meta` —— 证明期望值 `0` 来自原版，⛔ 不是「我们自己的产物证明我们自己」。
//
// ============================ F 组（2026-11-01 `W14` 追加 · 同一缺陷的第二处）============================
//  · `Assets/Resources/WarpforgeVFX/LUT/` 那 **14 张 AnimFX LUT** 2026-11-01 现读也全是 Clamp（`1`）——
//    错因相同：`.meta` 由 `工具/import_original_luts.py` **克隆模板**生成，而模板当时是 Clamp
//    （14 份一起继承）。判据同 A 组，另加第二来源 `数据/游戏数据/vfx_texture_mips.tsv`。
//  · **灭自证**：F1x 读**产物**（盘上 14 份 `.meta`）· F3x 抠**生成器**（python 函数体，退回「逐字克隆」就红）
//    · F5x 用合成输入钉住两个解析器（`ExtractPyFunc` / `WrapFromTsv`），否则上面那些「0」可能恒真。
//
// ============================ 怎么跑 / 怎么接（⚠️ 本代理不动宿主）============================
//   单独跑（编辑模式 / 批处理都行）：
//     unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//       -projectPath "D:\4\Unity\MyGame" -executeMethod B113W13_Assert.Run -logFile -
//   接宿主（**同一物理行**末尾追加 ⇒ 行数零变化；⚠️ 追加前**先看那一行已有的 `//` 在哪**）：
//       B113W13_Assert.Run(); pass += B113W13_Assert.Pass; fail += B113W13_Assert.Fail;
//   判绿红看「[B113W13] === 合计：X 通过 / Y 失败 ===」那一行，⛔ 不看退出码。
//
// ⚠️ 本件**只读文件**（`.meta` 文本 + `ArenaBuilder.cs` 源码文本 + 原版 `.meta`），不建对象、不改任何状态。

using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>写手代理 `W13`：账 `B-119` —— 战场 7 张 LUT 的 `wrap` 必须是 Repeat，且生成器不许再写回 Clamp。</summary>
public static class B113W13_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B113W13] ");

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    // ============================================================ 常量

    /// <summary>`WrapOf` 找不到那个键时的哨兵 —— ⛔ **不能是 0**（0 正是我们要的期望值，用 0 兜底 = 恒真）。</summary>
    public const int SentinelMissing = -999;

    /// <summary>7 张专属 LUT 的资产名（原版 13 场里只有这 7 场包内自带 LUT；其余 6 场指共享的 `LUT Normal`）。</summary>
    static readonly string[] LutNames =
    {
        "LUT Battle Arena Aeldari",
        "LUT Battle Arena Emperors Children",
        "LUT Battle Arena Genestaler Cults",
        "LUT Battle Arena Leviathan",
        "LUT Battle Arena Sororitas",
        "LUT Battle Arena Space Wolves",
        "LUT Battle Arena Tau Viorla",
    };

    /// <summary>工程内 LUT 目录（相对 `Assets/`）。</summary>
    const string LutDirRel = "WarpforgeArena1/Textures/LUT";
    /// <summary>生成器源码（相对 `Assets/`）。</summary>
    const string ArenaBuilderRel = "WarpforgeArena1/Editor/ArenaBuilder.cs";
    /// <summary>要抠出来的那个方法的签名（唯一出现处 = 定义处）。</summary>
    const string LutMethodSig = "static void EnsureLutImportSettings(string path)";
    /// <summary>原版 AssetRipper 导出工程的纹理目录（判断据源；见文件头 ①）。</summary>
    const string OriginalTexMetaDir = "d:/2/tools/assetripper_out/ExportedProject/Assets/Texture2D";

    // ============================================================ 入口

    public static void Run()
    {
        _sink.Pass = 0;
        _sink.Fail = 0;
        _sink.Failures.Clear();

        MenuCheck.Section(_sink, "B113W13 · B-119 战场 7 张 LUT 的 wrap = Repeat（产物 + 生成器 + 判据源）");
        A_MetaFiles();
        B_GeneratorSource();
        C_ParserSelfCheck();
        D_FileCount();
        E_OriginalMeta();
        F_AnimFxLuts();

        Debug.Log(_sink.P + $"=== 合计：{_sink.Pass} 通过 / {_sink.Fail} 失败 ===");
    }

    // ------------------------------------------------------------ A 组：7 份 .meta 的 wrapU/V/W == 0（产物）
    static void A_MetaFiles()
    {
        MenuCheck.Section(_sink, "A · 盘上 7 份 `.meta` 的 wrapU/V/W 必须都是 0（= Repeat）");

        string dir = AssetDir(LutDirRel);
        for (int i = 0; i < LutNames.Length; i++)
        {
            string full = dir + "/" + LutNames[i] + ".png.meta";
            string text = ReadText(full);
            Tr(!string.IsNullOrEmpty(text), $"A{i + 1}a 前提：`{LutNames[i]}.png.meta` 读得到且非空（{full}）");

            C(WrapOf(text, "wrapU"), 0, $"A{i + 1}b `{LutNames[i]}` wrapU == 0（原版 Repeat）");
            C(WrapOf(text, "wrapV"), 0, $"A{i + 1}c `{LutNames[i]}` wrapV == 0（原版 Repeat）");
            C(WrapOf(text, "wrapW"), 0, $"A{i + 1}d `{LutNames[i]}` wrapW == 0（原版也是 0；`ti.wrapMode` 一次写三轴）");
        }
    }

    // ------------------------------------------------------------ B 组：生成器方法体里不许再有 Clamp（生成器）
    static void B_GeneratorSource()
    {
        MenuCheck.Section(_sink, "B · `ArenaBuilder.EnsureLutImportSettings` 的方法体必须是 Repeat、且不再出现 Clamp");

        string path = AssetDir(ArenaBuilderRel);
        string src = ReadText(path);
        Tr(!string.IsNullOrEmpty(src), "B1 前提：`Assets/" + ArenaBuilderRel + "` 读得到且非空");

        bool found;
        string body = ExtractMethodBody(src, LutMethodSig, out found);
        Tr(found && !string.IsNullOrEmpty(body),
           "B2 前提：`" + LutMethodSig + "` 的方法体抠得出来（抠不出 ⇒ 下面三条不可信，别当绿）");

        Tr(body.IndexOf("TextureWrapMode.Repeat", StringComparison.Ordinal) >= 0,
           "B3 方法体里出现 `TextureWrapMode.Repeat`（原版值）");

        Tr(body.IndexOf("TextureWrapMode.Clamp", StringComparison.Ordinal) < 0,
           "B4 方法体里**不再出现** `TextureWrapMode.Clamp`（灭自证：改回去这条就红）");

        int bad = CountWrapLinesWithoutRepeat(body);
        Tr(body.Length > 0 && bad == 0,
           "B5 方法体里每一行提到 `wrapMode` 的都必须带 `Repeat`（堵 `(TextureWrapMode)1` 这种绕法）"
           + $"（违规行数 {bad}）");
    }

    // ------------------------------------------------------------ C 组：纯函数反向自证
    static void C_ParserSelfCheck()
    {
        MenuCheck.Section(_sink, "C · 灭自证 —— 两个纯函数喂合成输入，必须能把 `1` 读成 `1`");

        // ⚠️ 合成输入**必须带前导 `\n`** —— `WrapOf` 找的是 `"\n    <axis>: "`（真 `.meta` 里这个键永远不是文件第一行）。
        //    不带的话这条会「红得莫名其妙」，而它红的原因跟解析器对不对无关。
        Tr(WrapOf("\n    wrapU: 1\n", "wrapU") == 1,
           "C1 `WrapOf` 喂 `wrapU: 1` 必须读成 1（恒 0 的解析器在这里红）");
        Tr(WrapOf("\n    wrapU: 0\n", "wrapU") == 0,
           "C2 `WrapOf` 喂 `wrapU: 0` 必须读成 0");
        Tr(WrapOf("nothing here\n", "wrapU") == SentinelMissing,
           $"C3 `WrapOf` 找不到键必须返回哨兵 {SentinelMissing}（⛔ 不是 0 —— 0 就是期望值，用 0 兜底 = 恒真）");
        Tr(WrapOf("\n    wrapV: 1\n", "wrapU") == SentinelMissing,
           "C4 `WrapOf` 必须分得清轴：只有 `wrapV` 时查 `wrapU` 要返回哨兵");

        const string Trick = "static void Foo(int a) { if (a > 0) { t = TextureWrapMode.Clamp; } }";
        bool b1;
        string tb = ExtractMethodBody(Trick, "static void Foo(int a)", out b1);
        Tr(b1 && tb.IndexOf("TextureWrapMode.Clamp", StringComparison.Ordinal) >= 0 && tb.IndexOf("Repeat", StringComparison.Ordinal) < 0,
           "C5 `ExtractMethodBody` 能从合成源码里抠出带 `Clamp` 的体（抠不出/抠成空 ⇒ B 组恒真）");

        bool b2;
        string nb = ExtractMethodBody(Trick, "static void DoesNotExist(int a)", out b2);
        Tr(!b2 && nb.Length == 0,
           "C6 `ExtractMethodBody` 找不到签名时 `found = false` 且返回空（⛔ 不许返回半截）");
    }

    // ------------------------------------------------------------ D 组：个数
    static void D_FileCount()
    {
        MenuCheck.Section(_sink, "D · LUT 目录里恰好 7 份 `.png.meta`（防改名/删掉之后 A 组悄悄变绿）");

        string dir = AssetDir(LutDirRel);
        Tr(Directory.Exists(dir), "D1 前提：目录 `Assets/" + LutDirRel + "` 存在（" + dir + "）");

        int nMeta = 0, nLut = 0;
        try
        {
            if (Directory.Exists(dir))
            {
                foreach (var f in Directory.GetFiles(dir))
                {
                    string fn = Path.GetFileName(f);
                    if (!fn.EndsWith(".png.meta", StringComparison.OrdinalIgnoreCase)) continue;
                    nMeta++;
                    if (fn.StartsWith("LUT ", StringComparison.Ordinal)) nLut++;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(_sink.P + "枚举 `" + dir + "` 抛异常：" + e.GetType().Name + ": " + e.Message);
        }

        C(nMeta, LutNames.Length, "D2 目录里 `.png.meta` 的份数 == 7");
        C(nLut, LutNames.Length, "D3 其中以 `LUT ` 开头的份数 == 7");
    }

    // ------------------------------------------------------------ E 组：判据源（原版那 7 份 .meta 也是 0）
    static void E_OriginalMeta()
    {
        MenuCheck.Section(_sink, "E · 判据源交叉核对：原版那 7 份 `.meta` 的 wrapU/V/W 也是 0");

        Tr(Directory.Exists(OriginalTexMetaDir),
           "E1 前提：原版导出工程 `" + OriginalTexMetaDir + "` 存在"
           + "（不存在 ⇒ 期望值 `0` 就只剩我们自己的产物当证据，本组**必须**红而不是静默跳过）");

        for (int i = 0; i < LutNames.Length; i++)
        {
            string full = OriginalTexMetaDir + "/" + LutNames[i] + ".png.meta";
            string text = ReadText(full);
            Tr(!string.IsNullOrEmpty(text), $"E{i + 1}a 前提：原版 `{LutNames[i]}.png.meta` 读得到且非空");
            Tr(WrapOf(text, "wrapU") == 0 && WrapOf(text, "wrapV") == 0 && WrapOf(text, "wrapW") == 0,
               $"E{i + 1}b 原版 `{LutNames[i]}` 的 wrapU/V/W 都是 0（= Repeat）—— 我们照它改的");
        }
    }

    // ============================================================ 纯函数（被 C 组直接喂合成输入）

    /// <summary>工程 `Assets/` 的绝对路径（批处理下 = `&lt;project&gt;/Assets`）。</summary>
    static string AssetsRoot { get { return Application.dataPath.Replace('\\', '/'); } }

    /// <summary>把 `相对 Assets 的路径` 变成绝对路径（统一正斜杠 —— Windows 上 `Application.dataPath` 是反斜杠）。</summary>
    public static string AssetDir(string rel)
    {
        if (string.IsNullOrEmpty(rel)) return AssetsRoot;
        return AssetsRoot + "/" + rel.Replace('\\', '/').TrimStart('/');
    }

    /// <summary>读文本；读不到 ⇒ **空串 + 出声**（⛔ 不抛、也不返回半截）。</summary>
    public static string ReadText(string fullPath)
    {
        try
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) return "";
            return File.ReadAllText(fullPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[B113W13] 读不到 `" + fullPath + "`：" + e.GetType().Name + ": " + e.Message);
            return "";
        }
    }

    /// <summary>从 `.meta` 文本里抠 `    wrapU: N` 这类值；找不到 / 解析不了 ⇒ <see cref="SentinelMissing"/>。</summary>
    public static int WrapOf(string metaText, string axis)
    {
        if (string.IsNullOrEmpty(metaText) || string.IsNullOrEmpty(axis)) return SentinelMissing;
        string key = "\n    " + axis + ": ";
        int i = metaText.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return SentinelMissing;
        int s = i + key.Length, e = s;
        while (e < metaText.Length && (char.IsDigit(metaText[e]) || metaText[e] == '-')) e++;
        int v;
        return int.TryParse(metaText.Substring(s, e - s), out v) ? v : SentinelMissing;
    }

    /// <summary>从源码文本里按**花括号配平**抠出某个方法的体（不含最外层那对花括号）。
    /// <para>`found = false` ⇒ 签名不在 / 配平失败，返回空串（⛔ 不返回半截 —— C6 钉着这条）。</para>
    /// <para>会跳过 `//` 行注释与字符串字面量里的花括号（否则深度会算错）。</para></summary>
    public static string ExtractMethodBody(string src, string signatureMarker, out bool found)
    {
        found = false;
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(signatureMarker)) return "";

        int i = src.IndexOf(signatureMarker, StringComparison.Ordinal);
        if (i < 0) return "";
        int b = src.IndexOf('{', i);
        if (b < 0) return "";

        int depth = 0;
        for (int k = b; k < src.Length; k++)
        {
            char ch = src[k];
            if (ch == '{') { depth++; continue; }
            if (ch == '}')
            {
                depth--;
                if (depth == 0) { found = true; return src.Substring(b + 1, k - b - 1); }
                continue;
            }
            if (ch == '/' && k + 1 < src.Length && src[k + 1] == '/')
            {
                int nl = src.IndexOf('\n', k);
                k = nl < 0 ? src.Length - 1 : nl;
                continue;
            }
            if (ch == '"')
            {
                k++;
                while (k < src.Length && src[k] != '"')
                {
                    if (src[k] == '\\') k++;
                    k++;
                }
            }
        }
        return "";
    }

    /// <summary>方法体里「提到 `wrapMode` 却没带 `Repeat`」的行数（跳过 `//` 注释行）——
    /// 挡住 `ti.wrapMode = (TextureWrapMode)1;` 这种「不写 Clamp 字样」的绕法。</summary>
    public static int CountWrapLinesWithoutRepeat(string body)
    {
        if (string.IsNullOrEmpty(body)) return 0;
        int bad = 0;
        foreach (var raw in body.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("//", StringComparison.Ordinal)) continue;
            if (line.IndexOf("wrapMode", StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (line.IndexOf("Repeat", StringComparison.Ordinal) < 0) bad++;
        }
        return bad;
    }

    // ============================================================ F 组（写手 `W14` 追加 · `B-119` 续账）
    //
    // 🔴 **同一缺陷的第二处**：`Assets/Resources/WarpforgeVFX/LUT/` 那 **14 张 AnimFX LUT**，2026-11-01 现读
    //    三轴**全是 `1`（Clamp）**，而原版是 `0`（Repeat）—— 与 A 组那 7 张是同一个错因换了个目录：
    //    那个目录的 `.meta` 是**生成器克隆出来的**，而 `工具/import_original_luts.py` 当时克隆的模板正是
    //    `LUT Battle Arena Aeldari.png.meta` 的**改前（Clamp）那一版** ⇒ 14 份一起继承。
    //
    // 判据（两源一致，14 张全部）：
    //  ① `d:/2/tools/assetripper_out/ExportedProject/Assets/Texture2D/LUT*.png.meta` —— 三轴 `wrapU/V/W` 全 `0`；
    //  ② `数据/游戏数据/vfx_texture_mips.tsv` 的 `LUT *` 行 —— `wrapU=0 / wrapV=0`
    //     （含导出工程里没有的 `LUT Normal`，那一行是 `battlesharedresources_assets_all`）。
    //
    // 🔴 **灭自证（怎么才能红）** —— 与 A/B 组同款「产物 + 生成器」两面互证：
    //  · 「产物」= F 组逐份读**盘上** 14 份 `.meta`。把任一轴改回 `1` ⇒ 对应那条红。
    //  · 「生成器」= F 组抠**python 源码**里 `force_repeat_wrap` / `meta_wrap_ok` 的**函数体**。
    //    退回「逐字克隆模板」⇒ F4x 红。**两个不同文件、两个方向**，一次改动只能修好/弄坏一边。
    //  · 「解析器自证」= 用合成 python 文本喂 `ExtractPyFunc`（F60/F61）—— 抠不出 / 抠成一整份文件
    //    会让 F4x 恒真，所以先把抠取器本身钉住。

    /// <summary>14 张 AnimFX LUT 的资产名（`数据/游戏数据/animfx_modules.json` 的 `customLUT1/2` 收上来的 13 张 + `LUT Normal`）。</summary>
    static readonly string[] AnimFxLutNames =
    {
        "LUT Blind",
        "LUT Blizzard",
        "LUT Blue Tint",
        "LUT Dimensional Breach",
        "LUT Invert",
        "LUT Normal",
        "LUT Nuclear",
        "LUT Overexpose High",
        "LUT Pink Emperors Children",
        "LUT Pink Emperors Children Extreme",
        "LUT Poster",
        "LUT Red Hell",
        "LUT Red Tint",
        "LUT_Dark",
    };

    /// <summary>AnimFX LUT 目录（相对 `Assets/`；⚠️ 在 `Resources/` 下，因为运行期按名字 `Resources.Load` 取）。</summary>
    const string AnimFxLutDirRel = "Resources/WarpforgeVFX/LUT";
    /// <summary>AnimFX LUT 的导入器（绝对路径 —— 它在 `工具/` 下，不在 `Assets/` 里）。</summary>
    const string LutImporterAbs = "d:/4/Unity/工具/import_original_luts.py";
    /// <summary>导入器克隆的**模板源**（战场那份`.meta`；`META_TEMPLATE` 指向它）。</summary>
    const string LutTemplateAbs = "d:/4/Unity/MyGame/Assets/WarpforgeArena1/Textures/LUT/LUT Battle Arena Aeldari.png.meta";
    /// <summary>包内实读的纹理导入参数表（第二判据源；列序 `name width height mipCount bundle wrapU wrapV filterMode aniso npotScale`）。</summary>
    const string VfxTexMipsTsvAbs = "d:/4/Unity/数据/游戏数据/vfx_texture_mips.tsv";

    static void F_AnimFxLuts()
    {
        MenuCheck.Section(_sink, "F · AnimFX 14 张 LUT 的 wrapU/V/W 也必须是 0（产物 + 生成器 + 份数 + 判据源）");

        // ---------- 前提（硬断言先写：文件/目录/份数不成立时，下面那些「0」是假的）----------
        string dir = AssetDir(AnimFxLutDirRel);
        Tr(Directory.Exists(dir), "F1 前提：目录 `Assets/" + AnimFxLutDirRel + "` 存在（" + dir + "）");

        int nMeta = 0, nPng = 0;
        try
        {
            if (Directory.Exists(dir))
            {
                foreach (var f in Directory.GetFiles(dir))
                {
                    string fn = Path.GetFileName(f);
                    if (fn.EndsWith(".png.meta", StringComparison.OrdinalIgnoreCase)) nMeta++;
                    else if (fn.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) nPng++;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(_sink.P + "枚举 `" + dir + "` 抛异常：" + e.GetType().Name + ": " + e.Message);
        }

        // ⚠️ 只数 `.png.meta`，**不按 `LUT ` 前缀筛** —— `LUT_Dark.png.meta` 的前缀是 `LUT_` 不是 `LUT `，
        //    按前缀筛会数成 13 ⇒ 那条断言本身就是错的（本组踩过一次）。
        C(nMeta, AnimFxLutNames.Length, "F2 前提：目录里 `.png.meta` 的份数 == 14（防改名/删掉之后下面悄悄变绿）");
        C(nPng, AnimFxLutNames.Length, "F3 前提：目录里 `.png` 的份数 == 14（meta 与图必须成对）");

        // ---------- 产物：14 份 `.meta` 三轴都是 0 ----------
        int readOk = 0;
        for (int i = 0; i < AnimFxLutNames.Length; i++)
        {
            string name = AnimFxLutNames[i];
            string full = dir + "/" + name + ".png.meta";
            string text = ReadText(full);
            Tr(!string.IsNullOrEmpty(text), $"F{10 + i}a 前提：`{name}.png.meta` 读得到且非空（{full}）");
            if (!string.IsNullOrEmpty(text)) readOk++;

            // ⚠️ 无条件写结论（⛔ 不写成 `if (text != null) Check(...)` —— 读不到就静默跳过是本仓最常踩的坑）。
            C(WrapOf(text, "wrapU"), 0, $"F{10 + i}b `{name}` wrapU == 0（原版 Repeat）");
            C(WrapOf(text, "wrapV"), 0, $"F{10 + i}c `{name}` wrapV == 0（原版 Repeat）");
            C(WrapOf(text, "wrapW"), 0, $"F{10 + i}d `{name}` wrapW == 0（原版也是 0；`ti.wrapMode` 一次写三轴）");
        }
        C(readOk, AnimFxLutNames.Length, "F30 汇总：14 份 `.meta` 全部读得到（读到 13 份也算红）");

        // ---------- 生成器：不许再「逐字克隆一个可能是 Clamp 的模板」----------
        string gen = ReadText(LutImporterAbs);
        Tr(!string.IsNullOrEmpty(gen), "F31 前提：生成器 `" + LutImporterAbs + "` 读得到且非空"
           + "（读不到 ⇒ 下面生成器那几条**必须**红而不是静默跳过）");

        bool f1, f2;
        string bodyFix = ExtractPyFunc(gen, "def force_repeat_wrap(", out f1);
        string bodyChk = ExtractPyFunc(gen, "def meta_wrap_ok(", out f2);
        Tr(f1 && bodyFix.Length > 0, "F32 前提：`force_repeat_wrap` 的函数体抠得出来（抠不出 ⇒ F33-F35 不可信）");
        Tr(f2 && bodyChk.Length > 0, "F33 前提：`meta_wrap_ok` 的函数体抠得出来（抠不出 ⇒ F37-F38 不可信）");

        Tr(bodyFix.IndexOf("wrap%s: 0", StringComparison.Ordinal) >= 0,
           "F34 `force_repeat_wrap` **显式**写 `wrap%s: 0`（⛔ 不依赖「模板恰好是对的」—— 那正是这次的错因）");
        Tr(bodyFix.IndexOf("([UVW])", StringComparison.Ordinal) >= 0,
           "F35 那条替换是**三轴一起**（`([UVW])`）—— 只写 U/V 会让 meta 与运行期 `ti.wrapMode` 不一致");
        Tr(bodyFix.IndexOf(": 1", StringComparison.Ordinal) < 0,
           "F36 `force_repeat_wrap` 里**不出现** `: 1`（灭自证：退回写 Clamp 这条就红）");
        Tr(bodyChk.IndexOf("!= \"0\"", StringComparison.Ordinal) >= 0,
           "F37 `meta_wrap_ok` 里必须有「读到非 `0` 就判不过」的比较（⛔ 不许只检查键在不在）");

        int callSites = CountOccurrences(gen, "meta_wrap_ok(");
        Tr(callSites >= 2,
           "F38 `meta_wrap_ok(` 至少 2 处（1 处定义 + ≥1 处调用）—— 只定义不调用 = 自检形同不存在"
           + $"（现读 {callSites} 处）");

        Tr(gen.IndexOf("\"--apply\"", StringComparison.Ordinal) >= 0,
           "F39 生成器是**写盘类**：真写盘必须显式 `--apply`（CLAUDE.md：会写盘的脚本不带显式动作参数不许干活）");
        Tr(gen.IndexOf("len(_wf_help_sys.argv) <= 1", StringComparison.Ordinal) >= 0,
           "F40 **不带参**也只打印用法并退出（先例：`merge_ledger_parts.py --help` 直接真合并、冲掉 232 行）");

        // ---------- 判据源：生成器克隆的那份模板本身也必须是 Repeat ----------
        string tplText = ReadText(LutTemplateAbs);
        Tr(!string.IsNullOrEmpty(tplText), "F41 前提：模板源 `" + LutTemplateAbs + "` 读得到且非空");
        Tr(WrapOf(tplText, "wrapU") == 0 && WrapOf(tplText, "wrapV") == 0 && WrapOf(tplText, "wrapW") == 0,
           "F42 模板源（战场 Aeldari 那份）三轴也是 0 —— 万一哪天 `force_repeat_wrap` 被摘掉，克隆出来的仍然是对的");

        // ---------- 判据源交叉核对（两面）：原版导出 `.meta` + `vfx_texture_mips.tsv` ----------
        // ⚠️ `LUT Normal` **不在**原版导出工程里（W13 已核实：它在 `battlesharedresources_assets_all`，
        //    不在那批 `battleprefabs` 的导出目录）⇒ 那 13 份读导出 `.meta`，14 份**全部**再过一遍 TSV。
        //    TSV 的来源不是抄 meta：`工具/gen_arena_texture_import.py` 用 UnityPy 直读 bundle 的 `m_TextureSettings`。
        string tsv = ReadText(VfxTexMipsTsvAbs);
        Tr(!string.IsNullOrEmpty(tsv), "F43 前提：`" + VfxTexMipsTsvAbs + "` 读得到且非空（包内实读的 wrap 判据）");

        for (int i = 0; i < AnimFxLutNames.Length; i++)
        {
            string name = AnimFxLutNames[i];

            // ① 原版导出 `.meta`（13 份；`LUT Normal` 没有，跳过但**不假装通过**）
            if (name != "LUT Normal")
            {
                string full = OriginalTexMetaDir + "/" + name + ".png.meta";
                string text = ReadText(full);
                Tr(!string.IsNullOrEmpty(text), $"F{44 + i}a 前提：原版 `{name}.png.meta` 读得到且非空");
                Tr(WrapOf(text, "wrapU") == 0 && WrapOf(text, "wrapV") == 0 && WrapOf(text, "wrapW") == 0,
                   $"F{44 + i}b 原版 `{name}` 的 wrapU/V/W 都是 0（= Repeat）—— 我们照它改的，⛔ 不是自证");
            }

            // ② `vfx_texture_mips.tsv`（14 份全覆盖，含 `LUT Normal`）
            int tu, tv;
            WrapFromTsv(tsv, name, out tu, out tv);
            Tr(tu == 0, $"F{44 + i}c `vfx_texture_mips.tsv` 里 `{name}` 的 wrapU == 0（包内实读；现读 {tu}）");
            Tr(tv == 0, $"F{44 + i}d `vfx_texture_mips.tsv` 里 `{name}` 的 wrapV == 0（包内实读；现读 {tv}）");
        }

        // ---------- 抠取器自身的灭自证（否则 F34-F37 可能恒真）----------
        const string PyTrick = "def Foo(a):\n    x = 1\n\ndef Bar(b):\n    y = 2\n";
        bool g1, g2;
        string tb = ExtractPyFunc(PyTrick, "def Foo(", out g1);
        Tr(g1 && tb.IndexOf("def Foo(", StringComparison.Ordinal) >= 0 && tb.IndexOf("def Bar(", StringComparison.Ordinal) < 0,
           "F60 `ExtractPyFunc` 必须**停在下一个顶格 `def`**（抠成一整份文件 ⇒ F36 会被别的函数里的 `: 1` 误伤/误保）");
        string nb = ExtractPyFunc(PyTrick, "def DoesNotExist(", out g2);
        Tr(!g2 && nb.Length == 0, "F61 `ExtractPyFunc` 找不到名字时 `found = false` 且返回空（⛔ 不许退回整份源码）");

        // TSV 解析器也要喂合成输入（否则 F4x-c/d 可能因为「解析器恒返 0」而恒真 —— 0 正是期望值）
        const string TsvTrick = "name\tw\th\tm\tb\twrapU\twrapV\tfm\taniso\tnp\n"
                              + "LUT Fake\t256\t16\t9\tbag\t1\t1\t1\t1\t-1\n";
        int su, sv;
        WrapFromTsv(TsvTrick, "LUT Fake", out su, out sv);
        Tr(su == 1 && sv == 1, "F62 `WrapFromTsv` 喂 `wrapU=1 / wrapV=1` 必须读成 1/1（恒 0 的解析器在这里红）");
        WrapFromTsv(TsvTrick, "LUT NotThere", out su, out sv);
        Tr(su == SentinelMissing, $"F63 `WrapFromTsv` 找不到名字必须返回哨兵 {SentinelMissing}（⛔ 不是 0 —— 0 就是期望值）");
    }

    /// <summary>从 python 源码里抠某个**顶格**函数的整段（到下一个顶格 `def` 为止）。
    /// <para>`found = false` ⇒ 名字不在 / 不是顶格，返回空串（⛔ 不返回整份源码 —— 那会让调用方恒真）。</para></summary>
    public static string ExtractPyFunc(string src, string defMarker, out bool found)
    {
        found = false;
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(defMarker)) return "";
        int i = src.IndexOf(defMarker, StringComparison.Ordinal);
        if (i < 0) return "";
        if (i > 0 && src[i - 1] != '\n') return "";   // 只认顶格（缩进里的 `def` 是嵌套函数）
        int next = src.IndexOf("\ndef ", i + defMarker.Length, StringComparison.Ordinal);
        found = true;
        if (next < 0) return src.Substring(i);
        return src.Substring(i, next - i);
    }

    /// <summary>子串出现次数（F38 用：区分「只定义」与「定义 + 调用」）。</summary>
    public static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    /// <summary>在 `vfx_texture_mips.tsv` 里查某张纹理的 `(wrapU, wrapV)`。
    /// <para>找不到行 / 解析不了 ⇒ 两个都是 <see cref="SentinelMissing"/>（⛔ 不用 0 兜底 —— 0 就是期望值，用 0 兜底 = 恒真）。</para></summary>
    public static void WrapFromTsv(string tsv, string name, out int u, out int v)
    {
        u = SentinelMissing;
        v = SentinelMissing;
        if (string.IsNullOrEmpty(tsv) || string.IsNullOrEmpty(name)) return;
        foreach (var raw in tsv.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (line.Length == 0) continue;
            string[] cols = line.Split('\t');
            if (cols.Length < 7) continue;
            if (cols[0] != name) continue;
            int a, b;
            if (!int.TryParse(cols[5].Trim(), out a) || !int.TryParse(cols[6].Trim(), out b)) return;
            u = a;
            v = b;
            return;
        }
    }
}
