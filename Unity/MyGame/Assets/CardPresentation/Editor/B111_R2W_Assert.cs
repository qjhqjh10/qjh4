// B111_R2W_Assert.cs — 第 111 会话第 1 波写手代理 `B111_R2W` 的断言（2026-11-01）
//
// ============================ 本件挡什么 ============================
//  · `B-19` —— 原版 `Noise Combined` 的 wrapMode = **Repeat**（`m_WrapU/V/W = 0/0/0`），
//    我们工程那两份拷贝必须跟它一致，且**下次美术烘焙不许再改回 Clamp**。
//  · `B-32` —— 只在**注释层**：4 个 `.cs` 里「原版 `DOTweenSettings` 查不到」的过期说法必须归零，
//    并保留「已定位 / 实读值」这条正向标记（防「整段删掉」也算过）。
//
// ============================ 判据 ============================
//  `资料/完全复刻/判据/B111_R2_判据缺口两处.md` §1（B-19）/ §2（B-32）
//  原版 `Texture2D` `Noise Combined`（pathID `-8214795764591836323`）**UnityPy 1.25.3 直读**：
//    `m_TextureSettings = FilterMode 1 / Aniso 1 / **WrapU 0 / WrapV 0 / WrapW 0**`
//    `m_ColorSpace 0`（线性）· `m_TextureFormat 4`（RGBA32）· `m_MipCount 1` · 128×128
//  第二来源 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Noise Combined.png.meta`
//    → `textureSettings: wrapU: 0 / wrapV: 0 / wrapW: 0`
//  枚举 = `d:/2/tools/il2cpp_out/dump.cs:530360-530368`（`Repeat = 0` / `Clamp = 1`）
//  B-32 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Resources/DOTweenSettings.asset:31`
//    → `defaultEaseType: 6`（`Ease.OutQuad`，`d:/2/Warpforge_code/Scripts/DOTween/DG/Tweening/Ease.cs:11`）
//
// ============================ 🔴 灭自证（怎么才能红）============================
//  · A 组：把任一 `.meta` 的 `wrapU/V/W` 任一格改回 `1`（或删掉那一行）⇒ 对应那条红
//          （断言读的就是盘上那份 `.meta` 的文本）。
//  · B 组：从 `ArtBaker.RepeatTextures` 删掉 `Noise_Combined.png` / `cardback_noise.png`
//          ⇒ B2/B4 红（白名单查找），B6/B8 红（`IsRepeatTexture` 真实行为）。
//          ⚠️ **这一组是本件的关键**：只改 `.meta` 不改白名单，下一次 `BakeAll` 会把 wrap **静默改回 Clamp**。
//  · C 组（反向自证 = 解析器/判定器不是恒真）：`WrapOf` 喂一份「把 `wrapU` 写成 1」的合成文本必须读出 1；
//          从白名单副本里删掉一条后 `ContainsFile` 必须 false。
//  · D 组：把「查不到 / 找不到 / 本地没有 / 没核过」再写回那 4 个文件里 ⇒ 对应那条红；
//          「已定位 / 实读值」标记被整段删掉 ⇒ 正向那条红。
//  · E 组（端到端）：Unity **按新 `.meta` 重导之后**，`Texture2D.wrapModeU/V/W` 三个都必须是 `Repeat`。
//          ⚠️ 若 E 组红而 A 组绿 ⇒ **不是 `.meta` 错**，是 Unity 没重导：右键 Reimport 再看。
//
// ============================ 怎么跑 / 怎么接（⚠️ 本代理不动宿主）============================
//   单独跑（编辑模式 / 批处理都行）：
//     unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//       -projectPath "D:\4\Unity\MyGame" -executeMethod B111_R2W_Assert.Run -logFile -
//   接宿主（**任何**宿主可跑；本件只读文件 + 读两张贴图，不建对象、不改状态）：
//     **同一物理行**末尾追加（行数零变化）：
//       B111_R2W_Assert.Run(); _sink.Pass += B111_R2W_Assert.Pass; _sink.Fail += B111_R2W_Assert.Fail;
//     ⚠️ 追加前**先看那一行已有的 `//` 在哪**（本仓踩过 6 次「代码落进行末注释」）。
//   判绿红看「B111R2W 合计」那一行，⛔ 不看退出码。

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

/// <summary>写手代理 `B111_R2W`：`B-19`（`Noise Combined` 的 wrap = Repeat + 白名单防回退）· `B-32`（注释层的过期说法归零）。</summary>
public static class B111_R2W_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111R2W] ");

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    // ============================================================ 常量（Assets/ 相对路径）
    const string MetaMenu = "CardPresentation/Resources/Art/ui_menu/Noise_Combined.png.meta";
    const string MetaCard = "CardPresentation/Resources/Art/card_sdf/cardback_noise.png.meta";
    const string ArtBakerSrc = "CardPresentation/Editor/ArtBaker.cs";
    /// <summary>已知原版是 **Clamp** 的对照件（`ArtBaker.cs` 的注释点名的反例：`CardBlobShadow` 原版就是 1/1）
    /// —— 用它证明「白名单不是把所有东西都判成 Repeat」。</summary>
    const string ClampControl = "CardPresentation/Resources/Art/card3d/CardBlobShadow.png";

    static readonly string[] WantInWhitelist = { "Noise_Combined.png", "cardback_noise.png" };

    /// <summary>{相对路径, 必须出现的正向标记} —— B-32 那几处注释订正后必须留下的证据字样。</summary>
    static readonly string[][] DotweenFiles =
    {
        new[] { "CardPresentation/Battle/ErrorMessageBanner.cs", "defaultEaseType: 6" },
        new[] { "CardPresentation/Core/CardFeel.cs",             "defaultEaseType: 6" },
        new[] { "CardPresentation/Shell/CollectionWindow.cs",    "DOTweenSettings.asset:31 = 6" },
    };

    /// <summary>过期说法的关键词（只在**含 `DOTweenSettings` 的那一行**里判，避免误伤别处的「查不到」）。</summary>
    static readonly string[] StaleWords = { "找不到", "查不到", "本地没有", "没核过", "本地没定位到" };

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0;
        _sink.Fail = 0;
        _sink.Failures.Clear();

        MenuCheck.Section(_sink, "B111_R2W · B-19 `Noise Combined` wrap = Repeat + 白名单防回退 / B-32 注释订正");
        B19_MetaFiles();
        B19_Whitelist();
        B19_NegativeControls();
        B32_Comments();
        B19_EndToEnd();

        Debug.Log(_sink.P + $"=== 合计：{_sink.Pass} 通过 / {_sink.Fail} 失败 ===");
    }

    // ------------------------------------------------------------ A 组：两份 .meta 的 wrap == 0
    static void B19_MetaFiles()
    {
        MenuCheck.Section(_sink, "A · 两份 `.meta` 的 wrapU/V/W 必须都是 0（= Repeat；判据 B111_R2 §1）");

        string menu = ReadText(MetaMenu);
        string card = ReadText(MetaCard);

        Tr(!string.IsNullOrEmpty(menu), "A1 前提：`" + MetaMenu + "` 读得到且非空");
        Tr(!string.IsNullOrEmpty(card), "A2 前提：`" + MetaCard + "` 读得到且非空");

        C(WrapOf(menu, "wrapU"), 0, "A3 `ui_menu/Noise_Combined.png.meta` wrapU == 0（原版 Repeat）");
        C(WrapOf(menu, "wrapV"), 0, "A4 `ui_menu/Noise_Combined.png.meta` wrapV == 0（原版 Repeat）");
        C(WrapOf(menu, "wrapW"), 0, "A5 `ui_menu/Noise_Combined.png.meta` wrapW == 0（原版 Repeat）");

        C(WrapOf(card, "wrapU"), 0, "A6 `card_sdf/cardback_noise.png.meta` wrapU == 0（同一张原版贴图）");
        C(WrapOf(card, "wrapV"), 0, "A7 `card_sdf/cardback_noise.png.meta` wrapV == 0（同一张原版贴图）");
        C(WrapOf(card, "wrapW"), 0, "A8 `card_sdf/cardback_noise.png.meta` wrapW == 0（同一张原版贴图）");
    }

    // ------------------------------------------------------------ B 组：白名单防回退
    static void B19_Whitelist()
    {
        MenuCheck.Section(_sink, "B · `ArtBaker.RepeatTextures` 必须含这两张（否则下次 BakeAll 静默改回 Clamp）");

        var names = ReadWhitelist();
        Tr(names != null && names.Length > 0, "B1 前提：拿得到 `ArtBaker.RepeatTextures`（反射优先，退路 = 读源码字面量）");
        Tr(names != null && names.Length >= 9,
           $"B2 前提：白名单条目 >= 9（改前 7 条 + 本次 2 条；实得 {names?.Length ?? -1} —— 拿到的若 < 9 说明解析退化了，下面几条不可信）");

        C(ContainsFile(names, "Noise_Combined.png"), true,
          "B3 白名单含 `Noise_Combined.png`（`ui_menu/` 那一份）");
        C(ContainsFile(names, "cardback_noise.png"), true,
          "B4 白名单含 `cardback_noise.png`（`card_sdf/` 那一份）");

        // 走真实行为（`IsRepeatTexture` 是 private static ⇒ 反射），不是只看数组。
        Tr(IsRepeat("Assets/" + MetaMenu.Replace(".meta", "")) == true,
           "B5 `ArtBaker.IsRepeatTexture(\".../ui_menu/Noise_Combined.png\")` == true（bake 时会按 Repeat 写）");
        Tr(IsRepeat("Assets/" + MetaCard.Replace(".meta", "")) == true,
           "B6 `ArtBaker.IsRepeatTexture(\".../card_sdf/cardback_noise.png\")` == true");

        // 反例：原版就是 Clamp 的那张**不许**被白名单放行（挡「干脆全改成 Repeat」这种改法）。
        Tr(IsRepeat("Assets/" + ClampControl) == false,
           "B7 反例：`card3d/CardBlobShadow.png`（原版 Clamp 1/1）**不在**白名单 —— 挡「把白名单改成全部 Repeat」");
    }

    // ------------------------------------------------------------ C 组：反向自证（解析器/判定器不是恒真）
    static void B19_NegativeControls()
    {
        MenuCheck.Section(_sink, "C · 反向自证：解析器与判定器都能给出「非 0 / false」");

        string menu = ReadText(MetaMenu);

        // C1：把 wrapU 改成 1 的合成文本必须读出 1 —— 证明 `WrapOf` 不是恒返 0。
        string mutated = MutateWrap(menu, "wrapU", 1);
        C(WrapOf(mutated, "wrapU"), 1,
          "C1 `WrapOf` 对「wrapU: 1」的合成文本读出 1（不是恒返 0）—— 灭自证：把盘上那份改回 1，A3 必红");

        // C2：`WrapOf` 对不存在的键要给哨兵值（不是 0）—— 挡「删掉 wrapU 那一行也算过」。
        C(WrapOf("textureSettings:\n    wrapV: 0\n", "wrapU"), SentinelMissing,
          "C2 `WrapOf` 找不到该键时返回哨兵 " + SentinelMissing + "（不是 0）—— 挡「把 wrapU 那行删掉」");

        // C3：把白名单副本删掉一条，`ContainsFile` 必须 false。
        var names = ReadWhitelist();
        var minusOne = RemoveOne(names, "Noise_Combined.png");
        C(ContainsFile(minusOne, "Noise_Combined.png"), false,
          "C3 从白名单副本删掉 `Noise_Combined.png` 后 `ContainsFile` == false —— 灭自证：删掉那条，B3 必红");
    }

    // ------------------------------------------------------------ D 组：B-32 注释层
    static void B32_Comments()
    {
        MenuCheck.Section(_sink, "D · `B-32`：过期说法归零 + 正向标记在位（判据 B111_R2 §2）");

        foreach (var pair in DotweenFiles)
        {
            string rel = pair[0], marker = pair[1];
            string text = ReadText(rel);

            Tr(!string.IsNullOrEmpty(text), "D 前提：`" + rel + "` 读得到");
            C(StaleDotweenLineCount(text), 0,
              $"D `{rel}`：含 `DOTweenSettings` 的行里「找不到/查不到/本地没有/没核过」= 0"
              + "（灭自证：把「本地查不到」写回去这条就红）");
            Tr(!string.IsNullOrEmpty(text) && text.IndexOf(marker, StringComparison.Ordinal) >= 0,
               $"D `{rel}`：正向标记 `{marker}` 在位（挡「把整段注释删掉也算过」）");
        }
    }

    // ------------------------------------------------------------ E 组：端到端（重导后的运行时 wrap）
    static void B19_EndToEnd()
    {
        MenuCheck.Section(_sink, "E · 端到端：Unity 按新 `.meta` 重导后，两张贴图的 wrapModeU/V/W 都是 Repeat");

        EndToEndOne("Assets/" + MetaMenu.Replace(".meta", ""), "ui_menu/Noise_Combined.png", "E1");
        EndToEndOne("Assets/" + MetaCard.Replace(".meta", ""), "card_sdf/cardback_noise.png", "E2");
    }

    static void EndToEndOne(string assetPath, string label, string tag)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        Tr(tex != null, tag + " 前提：`" + label + "` 导得进来（空 ⇒ 资源没导入，下面三条不可信）");

        // ⛔ 不用 `if (tex != null) Check(...)`（那是「前提不成立就静默跳过」）；
        //    取不到时喂一个不可能相等的哨兵，让结论照红。
        var u = tex == null ? (TextureWrapMode)(-1) : tex.wrapModeU;
        var v = tex == null ? (TextureWrapMode)(-1) : tex.wrapModeV;
        var w = tex == null ? (TextureWrapMode)(-1) : tex.wrapModeW;
        C(u, TextureWrapMode.Repeat, tag + " `" + label + "` 运行时 wrapModeU == Repeat（红而 A 组绿 ⇒ 先 Reimport）");
        C(v, TextureWrapMode.Repeat, tag + " `" + label + "` 运行时 wrapModeV == Repeat");
        C(w, TextureWrapMode.Repeat, tag + " `" + label + "` 运行时 wrapModeW == Repeat");
    }

    // ============================================================ 纯函数（下面这几条被 C 组直接喂合成输入）
    const int SentinelMissing = -999;

    /// <summary>从 `.meta` 文本里抠 `    wrapU: N` 这类值；找不到 ⇒ <see cref="SentinelMissing"/>（⛔ 不是 0）。</summary>
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

    /// <summary>把 `    <axis>: <旧值>` 换成新值（只用于 C 组造合成输入，⛔ 不写盘）。</summary>
    public static string MutateWrap(string metaText, string axis, int newValue)
    {
        if (string.IsNullOrEmpty(metaText) || string.IsNullOrEmpty(axis)) return metaText;
        string key = "\n    " + axis + ": ";
        int i = metaText.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return metaText;
        int s = i + key.Length, e = s;
        while (e < metaText.Length && (char.IsDigit(metaText[e]) || metaText[e] == '-')) e++;
        return metaText.Substring(0, s) + newValue + metaText.Substring(e);
    }

    /// <summary>白名单里有没有这个**文件名**（忽略大小写 —— 与 `ArtBaker.IsRepeatTexture` 同一口径）。</summary>
    public static bool ContainsFile(string[] names, string file)
    {
        if (names == null || string.IsNullOrEmpty(file)) return false;
        foreach (var n in names)
            if (string.Equals(n, file, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static string[] RemoveOne(string[] names, string file)
    {
        if (names == null) return null;
        var list = new List<string>();
        foreach (var n in names)
            if (!string.Equals(n, file, StringComparison.OrdinalIgnoreCase)) list.Add(n);
        return list.ToArray();
    }

    /// <summary>含 `DOTweenSettings` 且同时含任一个过期词的行数；读不到 ⇒ -1（让断言照红）。</summary>
    public static int StaleDotweenLineCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return -1;
        int n = 0;
        foreach (var line in text.Split('\n'))
        {
            if (line.IndexOf("DOTweenSettings", StringComparison.Ordinal) < 0) continue;
            foreach (var w in StaleWords)
                if (line.IndexOf(w, StringComparison.Ordinal) >= 0) { n++; break; }
        }
        return n;
    }

    // ============================================================ 读盘 / 反射
    static string ReadText(string assetsRel)
    {
        string p = Path.Combine(Application.dataPath, assetsRel);
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }

    /// <summary>`ArtBaker.RepeatTextures`（private static readonly string[]）—— 反射优先；拿不到就从源码字面量抠。</summary>
    static string[] ReadWhitelist()
    {
        try
        {
            var f = typeof(ArtBaker).GetField("RepeatTextures", BindingFlags.NonPublic | BindingFlags.Static);
            if (f != null)
            {
                var arr = f.GetValue(null) as string[];
                if (arr != null && arr.Length > 0) return arr;
            }
        }
        catch (Exception e) { Debug.LogWarning(_sink.P + "反射取 `RepeatTextures` 失败（退源码字面量）：" + e.Message); }

        string src = ReadText(ArtBakerSrc);
        if (string.IsNullOrEmpty(src)) return null;
        int i = src.IndexOf("static readonly string[] RepeatTextures", StringComparison.Ordinal);
        if (i < 0) return null;
        int end = src.IndexOf("};", i, StringComparison.Ordinal);
        if (end <= i) return null;
        var names = new List<string>();
        int p2 = i;
        while ((p2 = src.IndexOf('"', p2)) >= 0 && p2 < end)
        {
            int q = src.IndexOf('"', p2 + 1);
            if (q < 0 || q > end) break;
            names.Add(src.Substring(p2 + 1, q - p2 - 1));
            p2 = q + 1;
        }
        return names.ToArray();
    }

    /// <summary>调 `ArtBaker.IsRepeatTexture(string)`（private static）。调用不到 ⇒ `null`。</summary>
    static bool? IsRepeat(string path)
    {
        try
        {
            var m = typeof(ArtBaker).GetMethod("IsRepeatTexture", BindingFlags.NonPublic | BindingFlags.Static);
            if (m == null) return null;
            return (bool)m.Invoke(null, new object[] { path });
        }
        catch (Exception e)
        {
            Debug.LogWarning(_sink.P + "反射调 `IsRepeatTexture` 失败：" + e.Message);
            return null;
        }
    }
}
