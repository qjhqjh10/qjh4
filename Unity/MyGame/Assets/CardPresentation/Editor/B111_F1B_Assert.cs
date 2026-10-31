// B111_F1B_Assert.cs — 「临时卡 glitch 的 `_Noise` 到底是哪张图」的资产层断言（2026-11-01 · B-95）
//
// 为什么单开这一份（而不全塞进 `WarpforgeVFX/Editor/CardGlitchProbe.cs`）：
//   `CardGlitchProbe` 验的是**换材质那一整套**（可逆 / 幂等 / 参数 / uv）；这里只钉一件事 ——
//   **`_Noise` 绑的是不是原版那一张**。这条以前在探针里「绿」，但绿证明的是「我们错得一致」：
//   探针拿 `CardArt.CardbackSdfNoise()` 当期望值，而绑定也取同一个函数 ⇒ **两边一起错也全绿**
//   （`CLAUDE.md` §三「灭自证」那一条 / `灭自证` 的经典形状）。
//
// 判据（全部 2026-11-01 现读，⛔ 不是从旧文档抄的）：
//   ① 原版材质 `Card Frame Glitch`（`d:/2/新解包资源/assets_full/bundle_duplicateassetisolation_assets_all/`
//      `Material/Material_-2877403367212247079.json`）的 `m_SavedProperties.m_TexEnvs` 里
//      `_Noise` → `m_Texture.m_PathID = 1565928461195782613`（`m_FileID = 0` = 同包）。
//   ② 同一包里还有个材质 **`Sprite Scan Lines`**（shader `-5025784757600742591` = `Everguild/FX/Sprite Scan Lines`），
//      它的 `_Noise` 也指向 `1565928461195782613` ⇒ **这个 pathID 就是 `ScanLines`**。
//   ③ 卡背 SDF 的 `Card Backs SDF`（`Material_6996605638394099752.json`）`_Noise` → `-8214795764591836323`
//      = `Noise Combined`。**两张图都是 128×128** ⇒ ⛔ 尺寸分不出、名字里都带 "noise" 也分不出，**只有 pathID 能分**。
//   ④ 图本身：`assets_full/.../Texture2D/ScanLines.png` 128×128 RGBA；我们这份
//      `CardPresentation/Resources/Art/vfx/ScanLines.png` 与它**逐像素相同**
//      （RGBA 原始像素 sha256 `37ceacc877c78c0c…`；文件字节不同只因为重新编码）。
//   ⑤ 原版导入设置（`d:/2/tools/assetripper_out/ExportedProject/Assets/ScanLines.png.meta`）：
//      `enableMipMap 1` · `sRGBTexture 1` · `wrapU/V/W 0`（Repeat）· `filterMode 1`（Bilinear）· `textureType 0`。
//      ⇒ 我们的 `.meta` 照抄这五项（`wrap 0` 是必须的：shader 里 `_NoiseScaleAndSpeed.xy = 10 × 18.05` 平铺采样）。
//
// 怎么跑（主对话 · 串行 · 要跑 Unity）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B111_F1B_Assert.Run -logFile -
//   判绿红：日志里 `[B111_F1B] === X 过 / Y 红 ===` 那一行。
//
// ⚠️ 只读工程资产、不存盘（`CardView` 是临时建的 GameObject，结尾销毁）。
using UnityEditor;
using UnityEngine;
using CardPresentation;

public static class B111_F1B_Assert
{
    const string P = "[B111_F1B] ";

    static int _ok, _bad;

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    public static void Run()
    {
        _ok = _bad = 0;
        CardView v = null;
        try
        {
            // ---------------------------------------------------------------- 1 · 资产层
            var tex = CardArt.GlitchScanLinesNoise();
            Check(tex != null, "1 · `Resources/Art/vfx/ScanLines` 取得到（`CardArt.GlitchScanLinesNoise()`）");
            if (tex == null) { Done(); return; }

            Check(tex.name == "ScanLines",
                  "1 · 资产名是 `ScanLines`（" + tex.name + "）—— 同一 bundle 里还有一张 `Noise Combined`，⛔ 别拿错");
            Check(tex.width == 128 && tex.height == 128,
                  $"1 · 128×128（实际 {tex.width}×{tex.height}；原版 Texture2D 同为 128²）");
            Check(tex.wrapMode == TextureWrapMode.Repeat,
                  "1 · wrapMode = Repeat（原版 `.meta` wrapU/V/W = 0；shader 按 10×18.05 平铺采样）"
                  + $"（实际 {tex.wrapMode}）");
            Check(tex.mipmapCount == 8,
                  $"1 · mipmapCount = 8（128² 全链；原版 `.meta` enableMipMap = 1）（实际 {tex.mipmapCount}）");

            // 灭自证 · 资产层：绝不能是卡背 SDF 那张
            var wrong = CardArt.CardbackSdfNoise();
            Check(wrong != null, "（前提·不静默）`CardArt.CardbackSdfNoise()` 取得到 —— 否则下面的灭自证是空转");
            Check(wrong == null || !ReferenceEquals(tex, wrong),
                  "1 · 灭自证：`ScanLines` **不是** `Noise Combined`（两者都是 128²，只有 pathID/名字能分）");

            // ---------------------------------------------------------------- 2 · 材质层
            var d = CardData.Simple("B111 F1B", 3, 2, 4);
            d.faction = "Ultramarines";       // 真卡框那一支要 faction 非空
            d.rarity = "common";
            v = CardView.Create(null, d);
            Check(v != null, "2 · 前提：`CardView.Create` 建得出来");

            if (v != null)
            {
                v.SetGlitch(true);
                Check(v.GlitchShown, "2 · `SetGlitch(true)` ⇒ `GlitchShown == true`");
                var mat = v.FrameMatNowForTest;
                Check(mat != null, "2 · 前提：卡框层换上了 glitch 材质");
                if (mat != null)
                {
                    Check(mat.GetTexture("_Noise") == tex,
                          "2 · glitch 材质的 `_Noise` == `Art/vfx/ScanLines`"
                          + "（实际 " + (mat.GetTexture("_Noise") == null ? "null" : mat.GetTexture("_Noise").name) + "）");
                    // 灭自证 · 绑定层：把 `CardView.GlitchMaterial()` 里的绑定改回 `CardbackSdfNoise()` ⇒ 这条红
                    Check(wrong == null || mat.GetTexture("_Noise") != wrong,
                          "2 · 灭自证：`_Noise` **不是** `Noise Combined`（改回 `CardArt.CardbackSdfNoise()` ⇒ 红）");
                }
                v.SetGlitch(false);
            }
        }
        catch (System.Exception e)
        {
            _bad++;
            Debug.LogError(P + "✗ 断言抛异常：" + e);
        }
        finally
        {
            if (v != null) Object.DestroyImmediate(v.gameObject);
        }
        Done();
    }

    static void Done()
    {
        var line = $"[B111_F1B] === {_ok} 过 / {_bad} 红 ===";
        if (_bad == 0) Debug.Log(line); else Debug.LogError(line);
    }
}
