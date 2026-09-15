// ArtBaker.cs — 把「原版复刻」要用的美术烘进 Resources/Art
//
// 两件事：
//   1. **烘战场背景**：打开 `WarpforgeArena1/Scenes/BattleArena1.unity`（原版 battlearena1 重建场景），
//      用它的相机渲一张 1920×1080 存成 `Resources/Art/arena1_bg.png`。
//      批处理下粒子不会自己走，渲之前先 `Simulate` 几秒，否则烟/火全是空的。
//   2. **统一导入设置**：卡框要 Read/Write（`CardView` 运行时量它的不透明包围盒来对齐 UV）、
//      全部关 mipmap、开 alphaIsTransparency。
//
// 用法（菜单）：Tools > CardPresentation > 烘焙原版美术
// 用法（命令行）： -executeMethod ArtBaker.BakeFromCLI
//
// ⚠️ **这一段是「原版复刻」用的参考美术**（Everguild / Games Workshop 的资产），
//    发布前整个 `Resources/Art/` 目录删掉或换成自己的图即可 —— 代码那边有 `CardArt.Available`
//    兜底，没有美术时会退回程序生成的占位卡面 + 纯色背景。
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ArtBaker
{
    const string P = "ART ";

    public const string ArtDir = "Assets/CardPresentation/Resources/Art";
    public const string ArenaScene = "Assets/WarpforgeArena1/Scenes/BattleArena1.unity";
    public const string BackdropPng = ArtDir + "/arena1_bg.png";

    const int W = 1920, H = 1080;

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
            Debug.LogError(P + $"找不到战场场景：{ArenaScene}（先跑 BuildArena1.BuildFromCLI）");
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

    /// <summary>给 Art 下所有 PNG 定导入设置</summary>
    [MenuItem("Tools/CardPresentation/重设美术导入设置")]
    public static void ApplyImportSettings()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            bool isFrame = path.Contains("/cards/");
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
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = isBackdrop ? 2048 : (path.Contains("/ui_deck/") ? 2048 : 1024);
            // ⚠️ **必须关掉 NPOT 缩放**：默认会把非 2 次幂的图缩到最近的 2 次幂 ——
            //    背景图 1920×1080 会被拉成 2048×1024（宽高比从 1.78 变 2.0），
            //    背景一「铺满」就整体放大 12.5%，和原版量出来的槽位坐标对不上了（踩过）。
            //    UI 切片同理（182×112 这种尺寸全会被改）。
            ti.npotScale = TextureImporterNPOTScale.None;
            // 卡框：`CardView` 要在运行时量「不透明包围盒」来裁 UV，所以必须可读
            ti.isReadable = isFrame;
            ti.textureCompression = isBackdrop ? TextureImporterCompression.CompressedHQ
                                               : TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        Debug.Log(P + "导入设置已重设");
    }
}
