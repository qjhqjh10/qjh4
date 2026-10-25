// CardGlitchProbe.cs — 「临时卡 glitch 材质」的验收探针（2026-10-25 · E7 批次 3）
//
// 验什么（完工后应该全绿）：
//   A 组 · **资产层**：自建 shader `CardPresentation/CardFrameGlitch` 在（`Resources/Shaders/` ⇒ 自动进包）；
//         原版那 9 个属性名一个不缺 + 我们加的 3 个（`_Color` / `_GreyScale` / `_UvRect`）；
//         噪声图 `Art/card_sdf/cardback_noise` 取得到（= 原版 `Noise Combined`，逐字节相同）。
//   B 组 · **换上去的那一份材质 = 原版材质的值**（不是 shader 默认值）：逐条断
//         `_GlitchPercentaje` 0.1 · `_ScanLinesIntensity` 0.547 · `_NoiseIntensity` 0.0035 ·
//         `_NoiseColor` (0.056604, 0.040851, 0.056604) · `_NoiseScaleAndSpeed` (10, 18.05, 1, 1) ·
//         `_Noise` == `CardArt.CardbackSdfNoise()`；并且 `_MainTex` 与 renderQueue **跟着原卡框那一份走**。
//   C 组 · **可逆 + 幂等**：`SetGlitch(false)` 逐引用回到换之前那一份材质；再开一次**不新建**材质。
//   D 组 · **灭自证**（结构上不可能「两边一起改回去」还绿）：
//         ① 开/关两档的卡框材质**不是同一个对象**、shader 名不同，但 `_MainTex` **相同**
//            ⇒ 把 `SetGlitch` 写成空实现，这条立刻红。
//         ② 参数**不等于 shader 默认值**（默认 `_ScanLinesIntensity` 是 1、`_NoiseColor` 是纯白）
//            ⇒ 只 `new Material(shader)` 不吃原版材质值，这条立刻红。
//         ③ `ShowEphemeral(true)` 必须**同时**打开角标与 glitch（原版 `CardScript.cs:8046` 是两行）
//            ⇒ 只接其中一条，这条立刻红。
//         ④ `_UvRect` == `CardView.FrameUv(卡框图)` 四分量（`0..1` 归因）—— 卡框 mesh 的 uv 是**图集子矩形**，
//            写成 (0,0,1,1) 假值这条立刻红。
//
// 怎么跑（主对话，串行）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod CardGlitchProbe.Run -logFile -
//   判绿红：日志里 `CGP === X 过 / Y 红 ===` 那一行（逐条按行首 `CGP ✓` / `CGP ✗` 数）。
//
// ⚠️ **不验「画出来像不像」**（批处理没有帧循环、也拍不了 RT 图）。真机口径见报告 E7_第3轮 §断言。
// ⚠️ 只读工程资产、不存盘（`CardView` 是本轮临时建的 GameObject，结尾销毁）。
using System;
using UnityEditor;
using UnityEngine;
using CardPresentation;

public static class CardGlitchProbe
{
    const string P = "CGP ";
    const string GlitchShaderName = "CardPresentation/CardFrameGlitch";

    static int _ok, _bad;

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    static bool Near(float a, float b, float eps = 1e-4f) { return Mathf.Abs(a - b) <= eps; }

    public static void Run()
    {
        _ok = _bad = 0;
        CardView v = null;
        try
        {
            // ---------------------------------------------------------------- A · 资产层
            var sh = Shader.Find(GlitchShaderName);
            Check(sh != null, $"A · 自建 shader `{GlitchShaderName}` 在（`Resources/Shaders/CardFrameGlitch.shader`）");

            // 原版 `Shader_-5025784757600742591.json` 的 `m_PropInfo` 里那 9 个自定义属性，逐个断
            var origProps = new[]
            {
                "_MainTex", "_MainColorIntensityFix", "_Noise", "_NoiseScaleAndSpeed",
                "_GlitchPercentaje", "_NoiseIntensity", "_ScanLinesIntensity", "_NoiseColor", "_AntialiasScale",
            };
            // ⚠️ 属性名是**材质**上的概念（`Shader.HasProperty` 不存在）⇒ 拿一份临时材质问
            Material probeMat = sh != null ? new Material(sh) : null;
            if (probeMat != null)
            {
                foreach (var pn in origProps)
                    Check(probeMat.HasProperty(pn), $"A · 原版属性 `{pn}` 在我们的 shader 里（名字逐字相同 ⇒ 原版材质能整套灌进来）");
                foreach (var pn in new[] { "_Color", "_GreyScale", "_UvRect" })
                    Check(probeMat.HasProperty(pn), $"A · 我们这边多出来的 `{pn}` 也在（原版没有这一条，见 shader 文件头）");
                UnityEngine.Object.DestroyImmediate(probeMat);
            }

            var noise = CardArt.CardbackSdfNoise();
            Check(noise != null, "A · 噪声图 `Art/card_sdf/cardback_noise` 取得到（= 原版 `Noise Combined`，逐字节相同）");
            // ③a 的前提：关键词角标那张图要在（它和 glitch 是原版同一个调用点上的两件事）
            Check(CardArt.Trait("ephemeral") != null,
                  "A · 关键词角标图 `Art/traits/ephemeral` 取得到（原版 `CardUI.ShowEphemeral()` 那一半）");

            // ---------------------------------------------------------------- B · 材质 = 原版材质值
            // 真卡框那一支要 `faction` 非空（`CardArt.Frame(faction,…)`）—— `CardData.Simple` 给的是 null
            var d = CardData.Simple("Glitch Probe", 3, 2, 4);
            d.faction = "Ultramarines";
            d.rarity = "common";          // `CardArt.Frame` 的稀有度键（小写，空/不认识会退 common）
            v = CardView.Create(null, d);

            Check(!v.GlitchShown, "B · 出厂 `GlitchShown == false`（原版 `Initialize` 不换材质）");

            var before = v.FrameMatNowForTest;
            Check(before != null, "B · 前提：这张卡有卡框层（`_frame != null` 才有材质可换）");
            if (before == null) { Done(); return; }
            int qBefore = before.renderQueue;
            var texBefore = before.mainTexture;

            v.SetGlitch(true);
            Check(v.GlitchShown, "B · `SetGlitch(true)` ⇒ `GlitchShown == true`");
            var now = v.FrameMatNowForTest;
            Check(now != null && now.shader != null && now.shader.name == GlitchShaderName,
                  "B · 卡框层现在的 shader = `" + GlitchShaderName + "`"
                  + (now != null && now.shader != null ? "（实际 " + now.shader.name + "）" : "（材质为空）"));

            if (now != null && now.shader != null && now.shader.name == GlitchShaderName)
            {
                // 逐条 = 原版材质 `Card Frame Glitch` 的实读值
                Check(Near(now.GetFloat("_GlitchPercentaje"), 0.1f), "B · `_GlitchPercentaje` = 0.1（原版材质值）");
                Check(Near(now.GetFloat("_ScanLinesIntensity"), 0.547f), "B · `_ScanLinesIntensity` = 0.547（原版材质值）");
                Check(Near(now.GetFloat("_NoiseIntensity"), 0.0035f), "B · `_NoiseIntensity` = 0.0035（原版材质值）");
                var nc = now.GetColor("_NoiseColor");
                Check(Near(nc.r, 0.056604f) && Near(nc.g, 0.040851f) && Near(nc.b, 0.056604f) && Near(nc.a, 1f),
                      $"B · `_NoiseColor` = (0.056604, 0.040851, 0.056604, 1)（实际 {nc.r:F6},{nc.g:F6},{nc.b:F6},{nc.a:F3}）");
                var ns = now.GetVector("_NoiseScaleAndSpeed");
                Check(Near(ns.x, 10f) && Near(ns.y, 18.05f) && Near(ns.z, 1f) && Near(ns.w, 1f),
                      $"B · `_NoiseScaleAndSpeed` = (10, 18.05, 1, 1)（实际 {ns.x},{ns.y},{ns.z},{ns.w}）");

                Check(now.mainTexture == texBefore, "B · `_MainTex` 跟着原卡框那一份走（只换 shader、不换图）");
                Check(now.GetTexture("_Noise") == noise, "B · `_Noise` = `CardArt.CardbackSdfNoise()`");
                Check(now.renderQueue == qBefore,
                      $"B · renderQueue 没被抹（{qBefore}；`SetQueue` 靠 `renderQueue == _queue` 早退）");

                // ④ 灭自证：uv 矩形必须按卡框图真值来
                var r = CardView.FrameUv(texBefore as Texture2D);
                var ur = now.GetVector("_UvRect");
                Check(Near(ur.x, r.x) && Near(ur.y, r.y) && Near(ur.z, r.width) && Near(ur.w, r.height),
                      $"B · ④ `_UvRect` == `CardView.FrameUv(卡框图)` = ({r.x:F4},{r.y:F4},{r.width:F4},{r.height:F4})"
                      + $"（实际 {ur.x:F4},{ur.y:F4},{ur.z:F4},{ur.w:F4}）");
            }

            // ---------------------------------------------------------------- C · 可逆 + 幂等
            var glitchMat = v.GlitchMatForTest;
            Check(v.FrameMatBeforeGlitchForTest == before, "C · 记住了「换之前那一份」材质（= 原版 `originalFrameMaterial`）");

            v.SetGlitch(false);
            Check(!v.GlitchShown, "C · `SetGlitch(false)` ⇒ `GlitchShown == false`");
            Check(ReferenceEquals(v.FrameMatNowForTest, before), "C · 卡框层**逐引用**回到换之前那一份材质");
            Check(v.FrameMatBeforeGlitchForTest == null, "C · 关掉之后不再记着旧材质（防下一张卡误还原）");

            v.SetGlitch(true);
            Check(ReferenceEquals(v.GlitchMatForTest, glitchMat), "C · 再开一次**不新建**材质（同一份实例）");
            Check(ReferenceEquals(v.FrameMatNowForTest, glitchMat), "C · 再开一次挂的还是那一份");

            // ---------------------------------------------------------------- D · 灭自证
            // ① 换材质这一跳真的发生了（且只换了 shader、没换图）
            Check(!ReferenceEquals(before, v.FrameMatNowForTest)
                  && before.shader.name != v.FrameMatNowForTest.shader.name
                  && before.mainTexture == v.FrameMatNowForTest.mainTexture,
                  "D · ① 开/关两档的卡框材质是**两个不同对象 + 不同 shader**，但 `_MainTex` 相同"
                  + "（`SetGlitch` 写成空实现 ⇒ 这条红）");

            // ② 参数不是 shader 默认值（默认 `_ScanLinesIntensity` = 1、`_NoiseColor` = 纯白）
            var fresh = sh != null ? new Material(sh) : null;
            if (fresh != null)
            {
                bool diff = !Near(fresh.GetFloat("_ScanLinesIntensity"), v.FrameMatNowForTest.GetFloat("_ScanLinesIntensity"))
                         && fresh.GetColor("_NoiseColor") != v.FrameMatNowForTest.GetColor("_NoiseColor");
                Check(diff, "D · ② 挂上去的参数**不等于 shader 默认值**（只 `new Material(shader)` 不吃原版材质值 ⇒ 这条红）");
                UnityEngine.Object.DestroyImmediate(fresh);
            }

            // ③ `ShowEphemeral` = 原版那两件事一起做
            v.SetGlitch(false);
            v.ShowEphemeral(true);
            Check(v.EphemeralShown, "D · ③a `ShowEphemeral(true)` 打开关键词角标（原版 `CardUI.ShowEphemeral()`）");
            Check(v.GlitchShown, "D · ③b **同一个入口**也打开了 glitch（原版 `CardScript.cs:8046` 紧挨着的 `ToggleGlitch(true)`）");
            v.ShowEphemeral(false);
            Check(!v.GlitchShown && !v.EphemeralShown, "D · ③c `ShowEphemeral(false)` 两条一起关");

            // ④ 溶解优先（两条都换 `_frame` 那一层材质；`ShowEphemeral` 每轮 SyncHand 都重刷 ⇒
            //    不挡的话 glitch 会每帧把溶解材质抢回来，卡就溶不掉）
            if (v.FrameDissolveSupported)
            {
                v.SetGlitch(false);
                bool began = v.BeginFrameDissolve();
                v.SetGlitch(true);
                Check(began && v.OnFrameDissolveMaterial && !v.GlitchShown,
                      "D · ④ 卡框正在溶解时 `SetGlitch(true)` **不抢**材质"
                      + "（删掉那条 `OnFrameDissolveMaterial` 闸 ⇒ 这条红）");
                v.EndFrameDissolve();
            }
        }
        catch (Exception e)
        {
            _bad++;
            Debug.LogError(P + "✗ 探针抛异常：" + e);
        }
        finally
        {
            if (v != null) UnityEngine.Object.DestroyImmediate(v.gameObject);
        }
        Done();
    }

    static void Done()
    {
        var line = $"CGP === {_ok} 过 / {_bad} 红 ===";
        if (_bad == 0) Debug.Log(line); else Debug.LogError(line);
    }
}
