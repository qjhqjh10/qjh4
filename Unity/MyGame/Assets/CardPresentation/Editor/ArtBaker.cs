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
    public static readonly string ArenaScene = ArenaBuilder.ScenePath(ArenaBuilder.DefaultArena);
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

    const string AudioDir = ArtDir + "/audio/vo";

    /// <summary>
    /// 给 `Resources/Art/audio/vo/` 下的**单位语音**定导入设置（2026-09-17 加）。
    ///
    /// 为什么必须跑：那 1787 条默认是 `DecompressOnLoad` —— 一条条全解成 PCM 放在内存里
    /// （42 MB 的 ogg 解出来是几百 MB）。语音是「一次只播一条」的东西，改成
    /// **`CompressedInMemory` + 不预载**（播到哪条才解哪条）就够。
    ///
    /// 生成音频本身：`工具/import_original_audio.py`（跑完再跑这个）。
    /// </summary>
    [MenuItem("Tools/CardPresentation/重设语音导入设置")]
    public static void ApplyAudioImportSettings()
    {
        if (!Directory.Exists(AudioDir))
        {
            Debug.LogWarning(P + $"没有 {AudioDir} —— 先跑 工具/import_original_audio.py");
            return;
        }
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ai = AssetImporter.GetAtPath(path) as AudioImporter;
            if (ai == null) continue;

            var s = ai.defaultSampleSettings;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.preloadAudioData = false;
            s.compressionFormat = AudioCompressionFormat.Vorbis;   // wav 那 115 条也压成 Vorbis（26 MB → 小一截）
            ai.defaultSampleSettings = s;
            ai.forceToMono = false;
            ai.SaveAndReimport();
            n++;
        }
        Debug.Log(P + $"语音导入设置已重设：{n} 条 → CompressedInMemory / 不预载 / Vorbis");
    }

    /// <summary>给 Art 下所有 PNG 定导入设置</summary>
    [MenuItem("Tools/CardPresentation/重设美术导入设置")]
    public static void ApplyImportSettings()    {
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
