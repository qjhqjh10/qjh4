// B111_T1B_Assert.cs — 第一百一十一会话 · 执行代理 `B111_T1C`（2026-11-01）
//
// ============================ 本文件断什么（3 组 + 2 条灭自证）============================
//   ① **`.meta` 本身**：`CrosshairTrail` / `LightningTrail` / `Laser_Wave_2` 三张的 `wrapU/V/W`
//      必须都是 **0**(Repeat) —— 判据 = **原版 `ExportedProject` 的同名 `.png.meta`**（三张都是 0/0/0，逐份现读）。
//   ② **白名单必须在**：`ArtBaker.RepeatTextures` 里必须含这三张（否则下一次 `BakeAll` / `BakeFromCLI`
//      会把手改的 `.meta` **静默改回 Clamp** —— 该数组的 doc 里就是这么警告的）。
//   ③ **白名单 ↔ `.meta` 一致**：白名单里**每一个能在 `Resources/Art/` 下找到的**文件，`.meta` 的 `wrapU/V`
//      都必须是 0 ⇒ 谁往表里加名字却忘了改 `.meta`（或反过来），这里当场红。
//
//   🔴 **灭自证（两条，结构上不可能「两边一起改绿」）**：
//     ㈠ `CardBlobShadow.png`（原版就是 **Clamp**）**必须在白名单外**、且 `.meta` 的 `wrapU/V` 仍是 **1**
//        ⇒ 谁把白名单改成「全部 Repeat」、或把判定改成恒真，这一条先红。
//     ㈡ `40k_popup_texture.png` **必须在白名单外**（原版也是 Repeat，但我们走 `ImageQuad.CreateTiled`
//        的多 quad 平铺、**不依赖贴图 wrapMode**，见 `报告/B111_T1B.md` §七·4）
//        ⇒ 谁「顺手」把它加进白名单，这里红 —— 那会把一次**已经判过等价**的差异重新变成口径分歧。
//
// ============================ 怎么跑 ============================
//   `Unity.exe -batchmode -quit -projectPath ... -executeMethod B111_T1B_Assert.Run -logFile -`
//   ⛔ 本文件**不在** `工具/_run_8_checks.sh` 里（那个脚本归调度台）；要常态化请调度台接线。
//   ⚠️ 它**不建窗口、不动场景、不跑播放**，只读 `.meta` 文本 + 反射读一个静态数组 ⇒ 挂到哪个宿主都安全。
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>`B111_T1B`：`Resources/Art` 里「原版是 Repeat」的贴图 —— `.meta` 与 `ArtBaker` 白名单必须一致。</summary>
public static class B111_T1B_Assert
{
    public static int Pass;
    public static int Fail;

    const string ArtRoot = "Assets/CardPresentation/Resources/Art/";

    /// <summary>🆕 本批补进白名单的三张（原版 `.meta` 的 `wrapU/V/W` 都是 0/0/0）。</summary>
    static readonly string[] NewRepeat =
    {
        ArtRoot + "ui/CrosshairTrail.png",
        ArtRoot + "ui_menu/LightningTrail.png",
        ArtRoot + "ui_menu/Laser_Wave_2.png",
    };

    /// <summary>灭自证 ㈠：原版就是 Clamp 的对照件（⛔ 永远不许进白名单）。</summary>
    const string ClampControl = ArtRoot + "card3d/CardBlobShadow.png";

    /// <summary>灭自证 ㈡：原版是 Repeat，但**我们故意不为它开白名单**（多 quad 平铺，不依赖 wrap）。</summary>
    const string TiledControl = ArtRoot + "ui/40k_popup_texture.png";

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Debug.Log("[B111_T1B] ===== Repeat 白名单 / .meta wrap 断言开始 =====");

        CheckNewMetas();
        CheckWhitelistMembers();
        CheckWhitelistMatchesMetas();
        CheckControls();

        Debug.Log("[B111_T1B] ===== 合计 pass=" + Pass + " fail=" + Fail + " =====");
    }

    // ------------------------------------------------------------------ ①

    static void CheckNewMetas()
    {
        foreach (var p in NewRepeat)
        {
            int u, v, w;
            bool got = TryWrap(p, out u, out v, out w);
            Check("`" + Path.GetFileName(p) + "` 的 .meta 读得到", got);
            if (!got) continue;
            Check("`" + Path.GetFileName(p) + "` .meta wrapU == 0(Repeat)（实测 " + u + "）", u == 0);
            Check("`" + Path.GetFileName(p) + "` .meta wrapV == 0(Repeat)（实测 " + v + "）", v == 0);
            Check("`" + Path.GetFileName(p) + "` .meta wrapW == 0（原版三个数都是 0；实测 " + w + "）", w == 0);
        }
    }

    // ------------------------------------------------------------------ ②

    static void CheckWhitelistMembers()
    {
        var list = Whitelist();
        Check("`ArtBaker.RepeatTextures` 反射读得到（读不到 = 断言失去意义，⛔ 不许静默跳过）", list != null);
        if (list == null) return;
        foreach (var p in NewRepeat)
        {
            var n = Path.GetFileName(p);
            Check("白名单含 `" + n + "`（删掉这一项 ⇒ 下次烘焙会把 .meta 静默改回 Clamp）", Has(list, n));
        }
    }

    // ------------------------------------------------------------------ ③

    static void CheckWhitelistMatchesMetas()
    {
        var list = Whitelist();
        if (list == null) return;
        int checkedN = 0;
        foreach (var n in list)
        {
            if (string.IsNullOrEmpty(n)) continue;
            var p = FindUnder(ArtRoot, n);
            if (p == null) continue;                       // 表里可能有本机还没导进来的名字 ⇒ 不算红
            checkedN++;
            int u, v, w;
            if (!TryWrap(p, out u, out v, out w)) { Check("白名单项 `" + n + "` 的 .meta 读得到", false); continue; }
            Check("白名单项 `" + n + "` 的 .meta wrapU/V == 0（实测 " + u + "/" + v + "）", u == 0 && v == 0);
        }
        Check("白名单里能在 `Resources/Art/` 找到的项 ≥ 9（现读 " + checkedN + "；少了 = 表被削过）", checkedN >= 9);
    }

    // ------------------------------------------------------------------ 灭自证

    static void CheckControls()
    {
        var list = Whitelist();
        if (list == null) return;

        var cn = Path.GetFileName(ClampControl);
        Check("灭自证㈠：`" + cn + "`（原版 Clamp）**不在**白名单里", !Has(list, cn));
        int u, v, w;
        if (TryWrap(ClampControl, out u, out v, out w))
            Check("灭自证㈠：`" + cn + "` 的 .meta wrapU/V 仍是 1(Clamp)（实测 " + u + "/" + v + "；改成 0 = 有人一刀切）", u == 1 && v == 1);

        var tn = Path.GetFileName(TiledControl);
        Check("灭自证㈡：`" + tn + "` **不在**白名单里（我们走多 quad 平铺、不依赖 wrapMode）", !Has(list, tn));

        // 把「为什么这张要 Repeat」钉在代码侧：准星拖尾用原版 shader 且 U 方向 ×10 ⇒ 真的越界。
        var ret = ReadText("Assets/CardPresentation/Battle/TargetReticle.cs");
        Check("灭自证㈢：`TargetReticle.cs` 读得到", ret != null);
        if (ret != null)
        {
            Check("灭自证㈢：仍用 `CrosshairTrail`", ret.Contains("CrosshairTrail"));
            Check("灭自证㈢：仍用原版 shader `Everguild/FX/Unlit UV scroll`", ret.Contains("Everguild/FX/Unlit UV scroll"));
            Check("灭自证㈢：UV Scale 仍是 (10,1)（U 真的越界 ⇒ wrap 才重要）", ret.Contains("new Vector4(10f, 1f"));
        }
    }

    // ------------------------------------------------------------------ 工具

    static void Check(string what, bool ok)
    {
        if (ok) { Pass++; Debug.Log("[B111_T1B] ✓ " + what); }
        else    { Fail++; Debug.LogError("[B111_T1B] ✗ " + what); }
    }

    static string ReadText(string rel)
    {
        try { return File.Exists(rel) ? File.ReadAllText(rel) : null; } catch { return null; }
    }

    /// <summary>读 `.meta` 的三个 wrap（**直读文本**，不依赖导入状态）。</summary>
    static bool TryWrap(string assetRel, out int u, out int v, out int w)
    {
        u = v = w = -999;
        var t = ReadText(assetRel + ".meta");
        if (t == null) return false;
        var mu = Regex.Match(t, @"\bwrapU:\s*(-?\d+)");
        var mv = Regex.Match(t, @"\bwrapV:\s*(-?\d+)");
        var mw = Regex.Match(t, @"\bwrapW:\s*(-?\d+)");
        if (!mu.Success || !mv.Success || !mw.Success) return false;
        u = int.Parse(mu.Groups[1].Value);
        v = int.Parse(mv.Groups[1].Value);
        w = int.Parse(mw.Groups[1].Value);
        return true;
    }

    /// <summary>反射读 `ArtBaker.RepeatTextures`（`private static readonly string[]`）。</summary>
    static string[] Whitelist()
    {
        var f = typeof(ArtBaker).GetField("RepeatTextures", BindingFlags.NonPublic | BindingFlags.Static);
        if (f == null) return null;
        return f.GetValue(null) as string[];
    }

    static bool Has(string[] list, string name)
    {
        foreach (var n in list)
            if (string.Equals(n, name, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>在 `root` 子树里按文件名找（找不到返回 null —— 调用方按「本机还没导进来」处理）。</summary>
    static string FindUnder(string root, string fileName)
    {
        try
        {
            if (!Directory.Exists(root)) return null;
            foreach (var p in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
                if (string.Equals(Path.GetFileName(p), fileName, System.StringComparison.OrdinalIgnoreCase))
                    return p.Replace('\\', '/');
        }
        catch { }
        return null;
    }
}
