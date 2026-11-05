// B145_E7_Assert.cs — A-24 / A-22 / A-23 的断言（执行代理 `B145-E7`，2026-11-05）
//
// ============================ 它盯的是什么 ============================
//  A-24 压暗层 α：`Menu Dark Background` 的 `m_Color.a` 必须是 **197/255 = 0.77254903**，不是取整的 0.773。
//       五处落点 = `DailyRewardPopup.ShadeColor` · `DraftModeWindow` ×2 · `LeaderboardWindow.DarkBgTint` · `SkirmishEventWindow`。
//  A-22 反射相机：`ArenaMirror` 的 cullingMask / clearFlags / backgroundColor 必须是**原版序列化常数**
//       （1024 / SolidColor / (0,0,0,0)），⛔ 不许再抄 base 相机。
//  A-23 抓屏 501：`redrawLayerMask` 默认 = 64（层 6）· `redrawAt501` 默认 = 真（原版恒入队）·
//       且 `PC_Renderer.asset` 的 `m_TransparentLayerMask` **必须已清 bit6**（否则同一帧画两遍）。
//
// ============================ 期望值从哪来（⛔ 不是「我们改完以后的值」）============================
//  A-24：`d:/2/tools/assetripper_out/ExportedProject/Assets/` 里 `Menu Dark Background` 的 `m_Color`
//        第四分量 **0.77254903**（85 处同值；0.77254903 就是 **197/255**）⇒ 断言直接比 **197f/255f**。
//  A-22：`decomp_full/kTools.Mirrors.Mirror__RenderReflection.c` 的全部 Camera 赋值只有
//        `worldToCameraMatrix`(:123) / `projectionMatrix`(:150) / `allowHDR`(:159) / `allowMSAA`(:165) /
//        `enabled`(:171) / `targetTexture`(:175) —— **不写** mask / clear / 背景色
//        ⇒ 那三格取场景序列化值；8 场逐份现读（本代理 2026-11-05 亲读
//        `assets_full/bundle_scenes_scenes_<场>/Camera/Camera_1535.json`，arena3/aeldari/darkangels/emperorschildren/
//        genestealers/sororitas/spacewolves/tauviorla **8/8 全同**）：
//        `m_CullingMask.m_Bits 1024`（= 层 10 `ArenaSlots.BoardCardLayer`）· `m_ClearFlags 2`(SolidColor) ·
//        `m_BackGroundColor (0,0,0,0)`。
//  A-23：`d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/GrabScreenFeatureU6.asset:20`
//        `settings.LayerMask.m_Bits = 64`（层 6 `AfterTransparents`）；同一份 renderer 的
//        `m_TransparentLayerMask = 1207963447`（**bit6 = 0**，与原版自洽）。
//
// ============================ 灭自证 ============================
//  ⛔ 只断「新值对不对」不够 —— **旧值不许回来**：
//    A-24 断四个 Shell 文件里**不再出现** `0.773f`；A-22 断 ArenaMirror 里**不再出现** `= cam.cullingMask` /
//    `= cam.clearFlags` / `= cam.backgroundColor`；A-23 断 GrabPass 里**不再出现** `redrawAt501 = false` /
//    `redrawLayerMask = 0`。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  建议宿主：`CardPresentation/Editor/ShellScene.cs` 的**方法尾聚合行**（本件只读文本 + 读常量 + 反射取两个
//  静态字段，**不建对象、不改资产、不驱动生产对象** ⇒ 接在哪个宿主的聚合行都可以）。
//  在 `Debug.Log(P + $"=== 合计：…")` **之前**、同一物理行、该行第一个 `//` 之前追加：
//      B145_E7_Assert.Run(); pass += B145_E7_Assert.Pass; fail += B145_E7_Assert.Fail;
using System;
using System.IO;
using System.Reflection;
using CardPresentation;
using UnityEngine;
using WarpforgeVFX;

/// <summary>`B145-E7`（A-24 色彩 / A-22 反射 / A-23 抓屏）的断言集。只读文本 + 常量 + 反射，**不驱动生产对象**。</summary>
public static class B145_E7_Assert
{
    public static int Pass;
    public static int Fail;

    static void Check(bool ok, string what)
    {
        if (ok) { Pass++; Debug.Log("[B145E7] ✓ " + what); }
        else { Fail++; Debug.LogError("[B145E7] ✗ " + what); }
    }

    /// <summary>原版 `Menu Dark Background` 的 alpha：`m_Color.a = 0.77254903` = **197/255**（8 bit 的 197）。</summary>
    const float OriginalShadeAlpha = 197f / 255f;

    /// <summary>读 `Assets/` 下的文本文件。⚠️ 读不到返回 null，调用方必须把它**当红**（⛔ 不许静默跳过）。</summary>
    static string ReadAsset(string rel, out string err)
    {
        err = null;
        try
        {
            string p = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(p)) { err = "文件不存在：" + p; return null; }
            return File.ReadAllText(p);
        }
        catch (Exception e) { err = e.GetType().Name + ": " + e.Message; return null; }
    }

    /// <summary>反射取 `static readonly Color` 字段（`LeaderboardWindow.DarkBgTint` 是 private）。</summary>
    static bool TryGetStaticColor(Type t, string field, out Color c)
    {
        c = default(Color);
        var f = t.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (f == null || f.FieldType != typeof(Color)) return false;
        c = (Color)f.GetValue(null);
        return true;
    }

    /// <summary>把 `PC_Renderer.asset` 里某个 `m_Name:` 块下的一格读成字符串（只做缩进范围内的线性扫描）。</summary>
    static string BlockField(string src, string blockHeader, string field)
    {
        if (string.IsNullOrEmpty(src)) return null;
        int b = src.IndexOf(blockHeader, StringComparison.Ordinal);
        if (b < 0) return null;
        int i = src.IndexOf(field, b, StringComparison.Ordinal);
        if (i < 0) return null;
        int e = src.IndexOf('\n', i);
        return (e < 0 ? src.Substring(i) : src.Substring(i, e - i)).Trim();
    }

    public static void Run()
    {
        A24_ShadeAlpha();
        A22_ReflectionCamera();
        A23_Grab501();
    }

    // ==================================================================
    //  A-24 · 压暗层 α = 197/255（5 处）
    // ==================================================================
    static void A24_ShadeAlpha()
    {
        // ① 两个静态字段（`DailyRewardPopup.ShadeColor` public · `LeaderboardWindow.DarkBgTint` private）
        Color sh, db;
        bool okSh = TryGetStaticColor(typeof(DailyRewardPopup), "ShadeColor", out sh);
        Check(okSh, "前提：`DailyRewardPopup.ShadeColor` 字段存在且是 Color");
        if (okSh)
            Check(Mathf.Abs(sh.a - OriginalShadeAlpha) < 1e-6f,
                  "`DailyRewardPopup.ShadeColor.a` == 197/255 = 0.77254903（现读 " + sh.a.ToString("F8") + "；⛔ 0.773 是取整）");

        bool okDb = TryGetStaticColor(typeof(LeaderboardWindow), "DarkBgTint", out db);
        Check(okDb, "前提：`LeaderboardWindow.DarkBgTint` 字段存在且是 Color");
        if (okDb)
            Check(Mathf.Abs(db.a - OriginalShadeAlpha) < 1e-6f,
                  "`LeaderboardWindow.DarkBgTint.a` == 197/255 = 0.77254903（现读 " + db.a.ToString("F8") + "）");

        // ② 三处内联字面量：文本扫描（`DraftModeWindow` ×2 · `SkirmishEventWindow` ×1）
        string[] files =
        {
            "CardPresentation/Shell/DailyRewardPopup.cs",
            "CardPresentation/Shell/DraftModeWindow.cs",
            "CardPresentation/Shell/LeaderboardWindow.cs",
            "CardPresentation/Shell/SkirmishEventWindow.cs",
        };
        const string NewLiteral = "0f, 0f, 0f, 0.7725490f";
        const string OldLiteral = "0.773f";     // 取整旧值 —— 一个都不许留
        int newHits = 0;
        int oldHits = 0;
        int badRead = 0;
        for (int i = 0; i < files.Length; i++)
        {
            string err;
            string src = ReadAsset(files[i], out err);
            Check(src != null, "读得到 " + files[i] + "（前提）" + (err != null ? " · " + err : ""));
            if (src == null) { badRead++; continue; }
            newHits += CountOf(src, NewLiteral);
            oldHits += CountOf(src, OldLiteral);
        }
        if (badRead == 0)
        {
            Check(newHits == 5,
                  "四个 Shell 文件合计 `" + NewLiteral + "` 恰好 **5** 处（现读 " + newHits + "；A-24 五处落点）");
            Check(oldHits == 0,
                  "四个 Shell 文件里**不再出现** `" + OldLiteral + "`（灭自证：取整旧值不许回来；现读 " + oldHits + "）");
        }
    }

    static int CountOf(string hay, string needle)
    {
        if (string.IsNullOrEmpty(hay) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = hay.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    // ==================================================================
    //  A-22 · 反射相机的 mask / clear / 背景色 = 原版序列化常数
    // ==================================================================
    static void A22_ReflectionCamera()
    {
        // ① 层号映射：原版 mask 1024 = 层 10 = `BoardCardLayer`（只有卡）
        Check(ArenaSlots.BoardCardLayer == 10, "`ArenaSlots.BoardCardLayer` == 10（原版 1024 只含这一层）");
        Check((1 << ArenaSlots.BoardCardLayer) == 1024, "1 << BoardCardLayer == 1024（= 原版 `m_CullingMask.m_Bits`）");

        // ② 源码：三格改成常数，且旧抄法不许回来
        string err;
        string src = ReadAsset("WarpforgeVFX/Runtime/ArenaMirror.cs", out err);
        Check(src != null, "读得到 ArenaMirror.cs（前提）" + (err != null ? " · " + err : ""));
        if (src == null) return;

        Check(src.Contains("_refl.cullingMask         = 1 << 10;"),
              "ArenaMirror：`_refl.cullingMask = 1 << 10`（原版 1024；⛔ 不是 cam.cullingMask）");
        Check(src.Contains("_refl.clearFlags          = CameraClearFlags.SolidColor;"),
              "ArenaMirror：`_refl.clearFlags = CameraClearFlags.SolidColor`（原版 m_ClearFlags 2）");
        Check(src.Contains("_refl.backgroundColor     = new Color(0f, 0f, 0f, 0f);"),
              "ArenaMirror：`_refl.backgroundColor = new Color(0,0,0,0)`（原版 m_BackGroundColor）");

        // 灭自证：原来那三行「抄 base」必须消失
        Check(!src.Contains("_refl.cullingMask         = cam.cullingMask;"),
              "灭自证：不再抄 `cam.cullingMask`（旧写法会把战场几何反射进来）");
        Check(!src.Contains("_refl.clearFlags          = cam.clearFlags;"),
              "灭自证：不再抄 `cam.clearFlags`");
        Check(!src.Contains("_refl.backgroundColor     = cam.backgroundColor;"),
              "灭自证：不再抄 `cam.backgroundColor`");

        // ⓷ C-11 回归闸：斜切近平面**不许**被顺手加回来（判据见该文件 :27-32 / :306-309）
        Check(!src.Contains("_refl.projectionMatrix = Camera.CalculateObliqueMatrix")
              && !src.Contains("_refl.projectionMatrix = CalculateObliqueMatrix"),
              "C-11：斜切近平面没有被顺手加回 `_refl.projectionMatrix`（试过已回退，见 :306-309）");
    }

    // ==================================================================
    //  A-23 · 抓屏 501：mask 64 + 恒入队 + 透明掩码已清 bit6
    // ==================================================================
    static void A23_Grab501()
    {
        // ① 层号 / 掩码（挪层与清位共用的那一个数）
        Check(GrabScreen501.LayerIndex == 6,
              "`GrabScreen501.LayerIndex` == 6（原版 `AfterTransparents`；现读 " + GrabScreen501.LayerIndex + "）");
        Check(GrabScreen501.LayerMask == 64,
              "`GrabScreen501.LayerMask` == 64（原版 `settings.LayerMask.m_Bits`；现读 " + GrabScreen501.LayerMask + "）");

        // ② 源码默认值：恒入队 + mask 64，且旧默认不许回来
        string err;
        string src = ReadAsset("WarpforgeVFX/Runtime/GrabPassTransparentFeature.cs", out err);
        Check(src != null, "读得到 GrabPassTransparentFeature.cs（前提）" + (err != null ? " · " + err : ""));
        if (src != null)
        {
            Check(src.Contains("public bool redrawAt501 = true;"),
                  "GrabPass：`redrawAt501` 默认 = **true**（原版 501 恒入队）");
            Check(src.Contains("public LayerMask redrawLayerMask = 1 << 6;"),
                  "GrabPass：`redrawLayerMask` 默认 = `1 << 6` = 64（原版 settings.LayerMask）");
            Check(!src.Contains("public bool redrawAt501 = false;"),
                  "灭自证：`redrawAt501 = false` 旧默认不许回来");
            Check(!src.Contains("public LayerMask redrawLayerMask = 0;"),
                  "灭自证：`redrawLayerMask = 0` 旧默认不许回来");
        }

        // ③ 资产侧硬前置：`m_TransparentLayerMask` 必须**已清 bit6**，否则默认透明 pass 与 501 各画一遍。
        //    ⚠️ 这一格 `B120_W6_Assert` 没覆盖（它只断 redrawLayerMask 的 64）。
        string ras = ReadAsset("Settings/PC_Renderer.asset", out err);
        Check(ras != null, "读得到 Settings/PC_Renderer.asset（前提）" + (err != null ? " · " + err : ""));
        if (ras != null)
        {
            string at = BlockField(ras, "m_Name: GrabPassTransparent", "redrawAt501:");
            Check(at != null && at.EndsWith(": 1", StringComparison.Ordinal),
                  "资产 `PC_Renderer.asset` 的 `redrawAt501` == 1（现读 `" + (at ?? "<未找到>") + "`）");

            string lm = BlockField(ras, "m_Name: GrabPassTransparent", "m_Bits:");
            Check(lm != null && lm.EndsWith(": 64", StringComparison.Ordinal),
                  "资产 `redrawLayerMask.m_Bits` == 64（现读 `" + (lm ?? "<未找到>") + "`）");

            // `m_TransparentLayerMask` 那一块（后面紧跟 `m_DefaultStencilState`，取两者之间的第一个 m_Bits）
            string t = BlockField(ras, "m_TransparentLayerMask:", "m_Bits:");
            int bits = 0;
            bool parsed = t != null && int.TryParse(t.Substring(t.LastIndexOf(' ')).Trim(), out bits);
            Check(parsed, "解析得到 `m_TransparentLayerMask.m_Bits`（现读 `" + (t ?? "<未找到>") + "`）");
            if (parsed)
                Check((bits & 64) == 0,
                      "`m_TransparentLayerMask` 已清 **bit6**（= 层 6 只由 501 画，不画两遍；现读 " + bits + "）");
        }
    }
}
