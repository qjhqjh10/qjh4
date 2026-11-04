// B144_D_ColorMaterial_Assert.cs — 执行写手 `B144-D`（第一百三十会话 · 2026-11-05）
//   域 = 色彩 / 材质 / 渲染队列；来源 = `报告/B131_审计_色彩材质与渲染.md` 的 🟠13 + 五条重点。
//
// ============================ 它盯的是什么 ============================
//  本波**动过**的四件（其余 🟠 在报告里逐条给了四档判定，没动的这里不断）：
//    ① `WarpforgeVFX/Shaders/WFMatcap.shader` —— 补 `_USEEMISSION` **加法**分支（B131 D18 / 5-05）
//    ② `WarpforgeVFX/Materials/{Embers Circle,Smoke02,Glow Additive Soft,Flame02}.mat` —— 渲染队列
//       （B131 D14 / 5-12：原版 3004 / 2995 / 3004 / 5000，我们原来四颗全被 `m_CustomRenderQueue: 3000` 拍平）
//    ③ `Shell/*.cs` 19 处压暗层常量 —— `0.773f` → `0.7725490f`（B131 A28 / 5-09；原版 = 197/255）
//    ④ `Shell/MainMenuRuntime.cs` 主菜单背景渐变色 —— 取整值 → 精确值（B131 E2 / 5-15）
//
// ============================ 断言 ============================
//  A 组（纯文本，零前提：只读源码 / 材质 YAML；⛔ 不建窗口、不跑渲染）
//    A1  WFMatcap 有 `#pragma multi_compile_local_fragment _ _USEEMISSION`
//        （用 multi_compile 而非 shader_feature：4 颗材质的 `m_ValidKeywords` 是空，
//         关键字由 `WarpforgeEffectBinder` 运行时 `EnableKeyword` 补 ⇒ shader_feature 的变体会被 build 剥掉）
//    A2  `_EmissionTex` 真的被声明 + 采样，且 UV = `IN.uv`
//    A3  加法形状逐字成立：`col += emiCol * emiTex.rgb;`，且 `emiCol = LinearToSrgb(_EmissionColor.rgb)`
//        （判据 = `Matcap Full Options` emission 变体 DXBC：`mad o0.xyz, r1.xyzx, r2.xyzx, r0.xyzx`
//          —— 原版对 `_EmissionColor` **也做 sRGB 编码**，与 `_Color` 同一段指令）
//    A4  四颗兜底材质的 `m_CustomRenderQueue` = 3004 / 2995 / 3004 / 5000
//    A5  19 个外壳文件里压暗层**不再有** `new Color(0f, 0f, 0f, 0.773f)`，且都有 `0.7725490f`
//    A6  主菜单背景渐变两色 = 原版精确值
//
//  🧨 灭自证（什么坏法会让哪条红）
//    · 把 emission 写成 `col *= emiCol * emiTex.rgb`（乘法）⇒ A3 红（A3 只认 `+=`，且 B1 会钉住「不得出现 `col *= ...Emission...`」）
//    · 漏掉 `LinearToSrgb(_EmissionColor.rgb)`（直接拿线性色加）⇒ A3b 红
//    · 把 `_EmissionTex` 的 UV 换成 `IN.matcapUV` ⇒ A2b 红
//    · 把 pragma 换回 `shader_feature_local_fragment _USEEMISSION` ⇒ A1 红
//    · 把四颗材质的队列改回 3000 ⇒ A4 红
//    · 任一处压暗层改回 0.773f ⇒ A5 红
//    · 主菜单渐变改回 0.224/0.012/0.020 ⇒ A6b 红（**只比新值不够** —— 旧值必须被判为「不存在」才算改到）
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读文件文本；⛔ 不建窗口、不写盘）。
//  挂法（由调度台决定；⚠️ 本写手白名单不含 `Editor/*Scene.cs`）：
//      B144_D_ColorMaterial_Assert.Run(); pass += B144_D_ColorMaterial_Assert.Pass; fail += B144_D_ColorMaterial_Assert.Fail;
//  判绿红：按行首 `B144D ✓` / `B144D ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using UnityEngine;

/// <summary>执行写手 `B144-D`：色彩 / 材质 / 渲染队列三条改动的回归断言（B131 色彩 D18 · D14 · A28 · E2）。</summary>
public static class B144_D_ColorMaterial_Assert
{
    const string Tag = "[B144D]";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    /// <summary>读 `Assets/<rel>`（相对 Assets 的路径，正斜杠）。</summary>
    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/" + rel); }
        catch (System.Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }

    /// <summary>把整份文本里的空白压成一个空格（免得被折行/缩进骗）。</summary>
    static string Flat(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return System.Text.RegularExpressions.Regex.Replace(s, "\\s+", " ");
    }

    const string MatcapShader = "WarpforgeVFX/Shaders/WFMatcap.shader";

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_EmissionBranch();
        GroupA_MaterialQueue();
        GroupA_DarkAlpha();
        GroupA_MainMenuGradient();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A1–A3 · MatCap `_USEEMISSION` 加法分支（判据 = 原版 DXBC 逐条）
    // ==================================================================
    static void GroupA_EmissionBranch()
    {
        var raw = Src(MatcapShader);
        var f = Flat(raw);
        if (f.Length == 0) { Check(false, "A0 读不到 WFMatcap.shader"); return; }

        // ---- A1 · 关键字变体的生成方式 ----
        Check(f.Contains("#pragma multi_compile_local_fragment _ _USEEMISSION"),
              "A1 `_USEEMISSION` 走 multi_compile（材质里 m_ValidKeywords 为空、关键字由 binder 运行时开 ⇒ shader_feature 会被 build 剥掉）");
        Check(!f.Contains("#pragma shader_feature_local_fragment _USEEMISSION"),
              "A1b 不得退回 shader_feature（会静默丢变体）");

        // ---- A2 · `_EmissionTex` 声明 + 采样 + UV ----
        Check(f.Contains("TEXTURE2D(_EmissionTex);") && f.Contains("SAMPLER(sampler_EmissionTex);"),
              "A2 `_EmissionTex` 已声明为 Texture2D + Sampler");
        Check(f.Contains("SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, IN.uv)"),
              "A2b `_EmissionTex` 用 `IN.uv` 采样（= 主贴图那一路；⛔ 不是 matcapUV）");

        // ---- A3 · 加法形状 + sRGB 编码 ----
        Check(f.Contains("col += emiCol * emiTex.rgb;"),
              "A3 加法项逐字成立 `col += emiCol * emiTex.rgb;`（判据 = DXBC `mad o0.xyz, r1, r2, r0`）");
        Check(f.Contains("half3 emiCol = LinearToSrgb(_EmissionColor.rgb);"),
              "A3b `_EmissionColor` 走 `LinearToSrgb`（原版同一段编码指令；⛔ 不是直接拿线性色加）");
        Check(f.Contains("#ifdef _USEEMISSION") && f.Contains("#endif"),
              "A3c 分支有 `#ifdef _USEEMISSION` 闸（默认档 = 4 颗未带该关键字的材质逐字不变）");

        // ---- 🧨 灭自证：乘法形状不得回来（本仓 2026-09-15 踩过「乘黑」）----
        Check(!f.Contains("col *= _EmissionColor") && !f.Contains("col *= emiCol"),
              "A3d 灭自证：emission **不得**写成乘法（乘进去 = 2026-09-15 那次的「乘黑」）");
    }

    // ==================================================================
    //  A4 · 四颗兜底材质的渲染队列（判据 = 原版 Material JSON 的 m_CustomRenderQueue）
    // ==================================================================
    static void GroupA_MaterialQueue()
    {
        var cases = new[]
        {
            new { Name = "Embers Circle.mat",     Queue = 3004 },
            new { Name = "Smoke02.mat",           Queue = 2995 },
            new { Name = "Glow Additive Soft.mat", Queue = 3004 },
            new { Name = "Flame02.mat",           Queue = 5000 },
        };
        foreach (var c in cases)
        {
            var s = Src("WarpforgeVFX/Materials/" + c.Name);
            if (string.IsNullOrEmpty(s)) { Check(false, "A4 读不到 " + c.Name); continue; }
            Check(s.Contains("m_CustomRenderQueue: " + c.Queue),
                  "A4 " + c.Name + " 的 `m_CustomRenderQueue` = " + c.Queue + "（原版 Material JSON 现读）");
            // 🧨 灭自证：被拍平的 3000 不得回来（四颗原来全是 3000）
            Check(!s.Contains("m_CustomRenderQueue: 3000"),
                  "A4b 灭自证：" + c.Name + " 不得退回被拍平的 3000");
        }
    }

    // ==================================================================
    //  A5 · 压暗层 α（原版 `Menu Dark Background.m_Color.a` = 197/255）
    // ==================================================================
    static readonly string[] DarkAlphaFiles =
    {
        "BaseOfferPopup.cs", "BattleLogPopup.cs", "BoosterInfoPopup.cs",
        "CampaignRewardWindow.cs", "DailyStreakPopup.cs", "DeckInfoPopup.cs",
        "DeckSelectionPopup.cs", "DuelPopupWindow.cs", "FTUESelectDeckWindow.cs",
        "InboxWindow.cs", "ItemDisplayWindow.cs", "MissionRerollPopup.cs",
        "PracticeModePopup.cs", "SearchingMatchPopup.cs", "TutorialModePopup.cs",
        "WhereToGetCardsPopup.cs", "EnergySinglePlayerOnlyEventWindow.cs",
        "TrophyInfoPopup.cs", "RankedEventWindow.cs",
    };

    static void GroupA_DarkAlpha()
    {
        foreach (var name in DarkAlphaFiles)
        {
            var s = Src("CardPresentation/Shell/" + name);
            if (string.IsNullOrEmpty(s)) { Check(false, "A5 读不到 " + name); continue; }
            Check(!s.Contains("new Color(0f, 0f, 0f, 0.773f)"),
                  "A5 " + name + " 的压暗层不再是取整值 0.773f");
            Check(s.Contains("new Color(0f, 0f, 0f, 0.7725490f)") || s.Contains("float ShadeA = 0.7725490f;"),
                  "A5b " + name + " 的压暗层 = 原版 0.7725490f（197/255）");
        }

        // 🧨 灭自证：这些文件里**不许**出现任何 `0f, 0f, 0f, 0.773f`（含注释外的其它写法）
        var bad = 0;
        foreach (var name in DarkAlphaFiles)
        {
            var s = Src("CardPresentation/Shell/" + name);
            if (s.Contains("(0f, 0f, 0f, 0.773f)")) bad++;
        }
        Check(bad == 0, "A5c 灭自证：19 个文件里 `(0f,0f,0f,0.773f)` 命中数 = 0｜实得 " + bad);
    }

    // ==================================================================
    //  A6 · 主菜单背景渐变（B131 E2：原版 c1/c2 精确值，原来这一处独一份取整）
    // ==================================================================
    static void GroupA_MainMenuGradient()
    {
        var s = Src("CardPresentation/Shell/MainMenuRuntime.cs");
        if (string.IsNullOrEmpty(s)) { Check(false, "A6 读不到 MainMenuRuntime.cs"); return; }
        var f = Flat(s);

        Check(f.Contains("new Color(0.22352943f, 0.011764707f, 0.019607844f)"),
              "A6 主菜单背景渐变 c1 = 原版精确值 (0.22352943, 0.011764707, 0.019607844)");
        Check(f.Contains("new Color(0.047058828f, 0f, 0.015686275f)"),
              "A6b 主菜单背景渐变 c2 = 原版精确值 (0.047058828, 0, 0.015686275)");
        // 🧨 灭自证：取整那三个字面量必须消失（否则「只加了精确值、旧值还在用另一处」也会绿）
        Check(!f.Contains("new Color(0.224f, 0.012f, 0.020f)"),
              "A6c 灭自证：取整值 `new Color(0.224f, 0.012f, 0.020f)` 已不存在");
        Check(!f.Contains("new Color(0.047f, 0f, 0.016f)"),
              "A6d 灭自证：取整值 `new Color(0.047f, 0f, 0.016f)` 已不存在");
    }
}
