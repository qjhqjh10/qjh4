using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>🔴 **A608 实验（2026-10-14）**：`Resources.Load` **失败一次之后会不会「负缓存」**
/// —— 即：资源当时不存在 ⇒ 返回 `null`；之后把资源**建出来并导入**，**同一次会话**里再 `Load` 还拿不拿得到。
///
/// <para>**为什么要有它**：账上写「没判据」（`decomp_full` 里查不到），而**我们自己有若干处代码**靠
/// `Resources.Load` 的失败当判据（例：`MissingArt` 记账、`[OptionsPanel] 图取不到` 告警、
/// `CardArt` 的三级兜底）—— 如果它负缓存，那么「先跑一次自检（图还没导）→ 再导图 → 再跑」
/// 这条最常见的操作序列会**静默拿到旧结论**（图在盘上、断言却照样说「缺」）。</para>
///
/// <para>**判据 = 打印的 ② / ③ / ④ 三行**：② 就是答案；③ 顺手验「`UnloadUnusedAssets` 是不是那个解」；
/// ④ 是**控制组**（同一次会话里「先建后载」必须成功 —— 否则说明实验本身坏了，② 的 null 不足为凭）。</para>
///
/// <para>⛔ **跑完自己清理**（删掉三张探针图与它们的 `.meta`）；⚠️ 它**只碰 `Assets/Resources/` 下的
/// `A608Probe*` 三个名字**，不碰任何既有资产。</para></summary>
public static class ResourceLoadCacheProbe
{
    const string ResDir = "Assets/Resources";
    const string N1 = "A608Probe1";      // ①→② 先失败、后建（**主实验**）
    const string N2 = "A608Probe2";      // ③ 与主实验同形，但中间插一次 `UnloadUnusedAssets`
    const string N3 = "A608Probe3";      // ④ 控制组：**先建后载**（必须成功）

    static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    static string PathOf(string n) { return ResDir + "/" + n + ".png"; }

    static void Clean(string n)
    {
        if (File.Exists(PathOf(n))) AssetDatabase.DeleteAsset(PathOf(n));
    }

    static void Make(string n)
    {
        File.WriteAllBytes(PathOf(n), OnePixelPng);
        AssetDatabase.ImportAsset(PathOf(n), ImportAssetOptions.ForceUpdate);
    }

    [MenuItem("Tools/Warpforge/A608 · Resources.Load 负缓存实验")]
    public static void Run()
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(PathOf(N1)) != null) Clean(N1);
        Clean(N2); Clean(N3);
        AssetDatabase.Refresh();

        // ---------- 主实验 ----------
        var a = Resources.Load<Texture2D>(N1);
        Debug.Log("[A608] ① 文件不存在时加载 `" + N1 + "` → " + (a == null ? "null（预期）" : "非空（?!）"));

        Make(N1);
        var b = Resources.Load<Texture2D>(N1);
        Debug.Log("[A608] ② **建出文件 + ImportAsset 之后**再加载 → "
                  + (b == null ? "**null ⇒ 有负缓存！**" : "非空（" + b.name + "）**⇒ 没有负缓存**"));

        // ---------- ③ 负缓存的「解」是不是 UnloadUnusedAssets ----------
        var c0 = Resources.Load<Texture2D>(N2);
        Make(N2);
        Resources.UnloadUnusedAssets();
        var c1 = Resources.Load<Texture2D>(N2);
        Debug.Log("[A608] ③ 先失败 → 建文件 → `Resources.UnloadUnusedAssets()` → 再加载 → "
                  + (c1 == null ? "仍 null" : "非空（" + c1.name + "）")
                  + "（同形于 ②；若 ② 非空则本条只是复算）");
        if (c0 != null) Debug.Log("[A608] ③′ 前提异常：`" + N2 + "` 一开始就存在");

        // ---------- ④ 控制组 ----------
        Make(N3);
        var d = Resources.Load<Texture2D>(N3);
        Debug.Log("[A608] ④ **控制组**（先建后载）→ "
                  + (d == null ? "null（**实验本身坏了** —— ② 的结论不足为凭）" : "非空（" + d.name + "）✓ 机制正常"));

        // ---------- 清理 ----------
        Clean(N1); Clean(N2); Clean(N3);
        AssetDatabase.Refresh();
        Debug.Log("[A608] 探针已清理（三张图与 `.meta` 都删掉了）");
    }
}
