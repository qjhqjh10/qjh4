// B141_W33_Assert.cs — 执行写手 `B141-W33` / `B141-W33b` 的断言
//   · A 组（`B141-W33`）：`Core/CardView.cs` 的**阵营行 / 兵种行两条文字色**
//   · B 组（`B141-W33b`）：卡框 / 卡背的 **4-tap 超采样**（`_AAScale` 只在这两层 = 1.25、共享基材 = 0、
//     全局关键字 `SUPERSAMPLING_ON` 随画质档 3/4 ↔ 0/1/2 切换）
//
// ============================ A 组盯的是什么 ============================
// 判据来源 = `报告/B131_审计_色彩材质与渲染.md` 的 `B3` / `B5` · `判据/B141_W129_对账_卡组色彩.md` 的 `5-10`：
//   · 阵营行（`_army`，原版 `ArmyText*`）= **#FF9A58** = (255,154,88)
//       ← 原版 `m_fontColor = (1,1,1,1)` × 材质 `Pragati-Regular Card Unit type` 的 `_FaceColor = (1, 0.605498, 0.344340)`
//   · 兵种行（`_race`，原版 `RaceText`）= **#FD9A58** = (253,154,88)
//       ← 原版 `m_fontColor = (0.992157, 0.603922, 0.345098)` × 材质 `Pragati-Regular Card Content` 的 `_FaceColor = 纯白`
//   ⚠️ **两行是两条独立的判据链**，值只差 R 通道 2 —— 这正是本包容易「一并改成 253」的地方，
//      所以下面既断 253、**也断 255**（改错方向两边都会红）。
//
// ============================ B 组盯的是什么（2026-11-04 `B141-W33b`）============================
// 判据（一手，**原版编译字节码反汇编**）：`Custom/SuperSampled Texture`（`wf_shaders_extra.bundle` 的
//   `compressedBlob` LZ4 解开 = 4 个 DXBC 程序块，`D3DDisassemble` 逐块现读）的 `SUPERSAMPLING_ON` 变体
//   = 4-tap RGSS（偏移 `±0.125·dx ±0.375·dy` / `±0.375·dx ∓0.125·dy`，`dx/dy = ddx/ddy(uv×_AAScale)`，
//   求和 ×0.25 × 顶点色）；OFF 变体 = `sample × vertexColor`。
//   材质 `Card Frame and Cardback for combat`：`_AAScale = 1.25`，挂 `CardPrefab` 的 `CardFrame` / `Cardback`。
//   开关 = 全局 `SUPERSAMPLING_ON`（原版 `QualitySettingsManager.ApplyGraphicsQuality` 按 per-tier
//   `enableCardsAntiAliasing`；`Intro.unity` 的 `qualityDefinitions` 现读 = quality 0/1/2 关、3/4 开）。
//   ⚠️ **shader 能不能编译只有 Unity 能验**（本批记录 `W132`）：本组只静态断「属性/关键字/取值门」，
//      真正编译 + 画面差异必须下一次 Unity 同步点验收。
//
// ============================ 怎么用 ============================
//   `B141_W33_Assert.Run();`   （静态类 + 无参 `Run()`；全局命名空间、类名 == 文件名）
//   ⚠️ 本文件**不建持久资产**：临时 GameObject / Material / Texture 在 `finally` 里 `DestroyImmediate`，
//      并把 `GameStaticData.graphicsQuality` 与 `SUPERSAMPLING_ON` 关键字状态原样还原。
//   ⚠️ **接线待办**：白名单只有 `CardView.cs` + `CardSprite.shader` + `SettingsWindow.cs`（画质应用那一跳）
//      + 本文件 + 报告，⛔ 不能改任何宿主 ⇒ 本类**尚未挂进任何宿主**（铁律 13·3 的「建了必须接线」由调度台另派）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A★ 兵种行 R=253  ← `_race` 用回 `InkArmy`（255）⇒ A★ 红；`InkUnit` 若被改成 255 也红。
//   · A5 阵营行 R=255  ← 把 `InkArmy` 一并改成 253（本包最容易犯的那个错）⇒ A5 红。
//   · A10/A11 ← 只改建层、漏改 `ApplyTint`（`:8178`）⇒ `SetAlpha2D` 之后兵种行回到 255 ⇒ A11 红。
//   · B4/B5 ← 若把 `BaseMaterial`（共享基材）也设成 1.25 ⇒ `info` 层读到 1.25 ⇒ B4★ 红。
//   · B7/B8 ← 把 `CardsAntiAliasingAllowed` 的阈值写错（比如 `> High` 或恒 true）⇒ 至少一条红。
using System.IO;
using TMPro;
using UnityEngine;
using CardPresentation;

public static class B141_W33_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "B141W33";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol, msg + $"\n      （实得 {got:F3} · 期望 {want:F3} · 容差 {tol:G3}）");
    }

    public static void Run()
    {
        _pass = _fail = 0;

        // 前提硬检查（⛔ 不静默跳过 —— 字体不可用时阵营行/兵种行根本建不出来，A 组会拿 null 假绿）
        Check(TmpFont.Available, "P0 **前提**：TMP 字体资产可用（`TmpFont.Available`）");
        if (!TmpFont.Available)
        {
            Debug.LogError(Tag + " ===== 前提不成立 ⇒ 本宿主什么都没验到（不是通过）=====");
            return;
        }

        var root = new GameObject("B141W33_ROOT");
        try
        {
            A_TwoLines(root.transform);
            B_AntiAliasing(root.transform);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        Debug.Log(Tag + " ===== 合计：通过 " + _pass + " · 失败 " + _fail + " =====");
    }

    // ---------------------------------------------------------------- A · 阵营行 255 / 兵种行 253
    static void A_TwoLines(Transform parent)
    {
        GameObject go = null;
        try
        {
            go = new GameObject("B141W33_A");
            go.transform.SetParent(parent, false);

            // 夹具：**必须显式给 `type` / `subtype`** —— 否则 `SubtypeLine` 走老路径、兵种行可能为空，
            //   `Fill` 对空串直接返回 null ⇒ `_race` 根本没建、A★ 会拿 null 假红/假绿。
            var d = CardData.Placeholder(0);
            d.type = "unit";
            d.subtype = "Infantry";
            d.faction = "Ember";

            var card = CardView.Create(go.transform, d, "B141W33_card", CardFace.Full);
            Check(card != null, "A0 **前提**：夹具卡建出来了（`CardView.Create`）");
            if (card == null) return;

            TextMeshPro army = null, race = null;
            foreach (var t in card.GetComponentsInChildren<TextMeshPro>(true))
            {
                if (t.name == "army") army = t;
                else if (t.name == "race") race = t;
            }
            Check(army != null, "A1 **前提**：阵营行层 `army` 建出来了（`Fill(_army, \"army\", …)`）");
            Check(race != null, "A2 **前提**：兵种行层 `race` 建出来了（`SubtypeLine` 非空）");
            if (army == null || race == null) return;

            // 前提：字体材质的 `_FaceColor` 是白 ⇒ 屏上色 = 顶点色本身（原版两条链里至少有一侧是白）
            var fm = army.fontMaterial;
            bool hasFace = fm != null && fm.HasProperty(ShaderUtilities.ID_FaceColor);
            Check(hasFace, "A3 **前提**：阵营行拿到了字体材质、且有 `_FaceColor`");
            if (hasFace)
            {
                var fc = fm.GetColor(ShaderUtilities.ID_FaceColor);
                Check(Mathf.Abs(fc.r - 1f) < 2e-2f && Mathf.Abs(fc.g - 1f) < 2e-2f && Mathf.Abs(fc.b - 1f) < 2e-2f,
                      "A4 **前提**：阵营行材质 `_FaceColor` = 白（实得 " + fc.ToString("F3") + "）");
            }

            var ca = army.color;
            var cr = race.color;

            Near(ca.r * 255f, 255f, 1f,
                 "A5 ★ 阵营行 R = **255**（原版 = 白 `m_fontColor` × `Pragati-Regular Card Unit type` 的 "
               + "`_FaceColor = 1.0`）｜🧨 把 `InkArmy` 一并改成 253 ⇒ 红");
            Near(ca.g * 255f, 154f, 1f, "A6 阵营行 G = **154**（材质 `_FaceColor.g = 0.605498`）");
            Near(ca.b * 255f, 88f, 1f, "A6b 阵营行 B = **88**（材质 `_FaceColor.b = 0.344340`）");

            Near(cr.r * 255f, 253f, 1f,
                 "A★ 兵种行 R = **253**（原版 `m_fontColor = (0.992157, 0.603922, 0.345098)` × 白材质 = `#FD9A58`）"
               + "｜🧨 用回 `InkArmy`(255) ⇒ 红");
            Near(cr.g * 255f, 154f, 1f, "A7 兵种行 G = **154**");
            Near(cr.b * 255f, 88f, 1f, "A8 兵种行 B = **88**");

            // 灭自证（结构上不可能与上面同时满足）：两行**不能是同一个 R**
            Check(Mathf.Abs(ca.r * 255f - 255f) <= 1f && Mathf.Abs(cr.r * 255f - 255f) > 1.5f,
                  "A9（灭自证）阵营行 = 255 且兵种行 ≠ 255 —— 两行是两条独立判据链，⛔ 不能合并成一个常量");

            // ② `ApplyTint` 那条路（`:8177` / `:8178`）—— 只改建层、漏改这里 ⇒ 淡出/高亮一次就打回旧色
            card.SetAlpha2D(0.5f);
            var ca2 = army.color;
            var cr2 = race.color;
            Near(ca2.r * 255f, 255f, 1f, "A10（ApplyTint 后）阵营行 R 仍是 **255**");
            Near(cr2.r * 255f, 253f, 1f, "A11（ApplyTint 后）兵种行 R 仍是 **253**"
                + "｜🧨 只改建层、漏改 `:8178` ⇒ 这里回到 255 ⇒ 红");
            Near(cr2.a, 0.5f, 0.03f, "A12（前提）`SetAlpha2D(0.5)` 之后顶点色 α ≈ 0.5（证明确实走了 `ApplyTint`）");
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    // ---------------------------------------------------------------- B · 卡框 / 卡背超采样（`B141-W33b`）
    static void B_AntiAliasing(Transform parent)
    {
        const string ShaderName = "CardPresentation/CardSprite";

        // B0/B1：shader 存在、属性表有 `_AAScale`，且**默认 = 0（关）** —— 本工程这支 shader 是全卡面层共用的，
        //   默认 1.25 会把立绘/底板/数值层一起超采样（见 `CardView.BaseMaterial` 的注释）。
        var sh = Shader.Find(ShaderName);
        Check(sh != null, "B0 **前提**：找得到 `" + ShaderName + "`");
        if (sh == null) return;

        Material probe = null;
        try
        {
            probe = new Material(sh);
            Check(probe.HasProperty("_AAScale"),
                  "B1 **前提**：shader 属性表里有 `_AAScale`（原版 `Custom/SuperSampled Texture` 的属性；"
                + "没有它 = shader 没更新 ⇒ 下面全部无意义）");
            if (!probe.HasProperty("_AAScale")) return;
            Near(probe.GetFloat("_AAScale"), 0f, 1e-4f,
                 "B2 **默认值 = 0（关）** —— 本支 shader 是卡面各层共用基材（含 `ImageQuad` 直接 `new Material(sh)`）"
               + "｜🧨 默认改成 1.25 ⇒ 所有非卡框层一起超采样（原版只有卡框/卡背）");
        }
        finally
        {
            if (probe != null) Object.DestroyImmediate(probe);
        }

        // B3：shader 源码里三个关键片段（shader 编译只能 Unity 验，属于本批 `W132`；这里只做静态自检）
        try
        {
            string path = Path.Combine(Application.dataPath,
                                       "CardPresentation/Resources/Shaders/CardSprite.shader");
            string src = File.Exists(path) ? File.ReadAllText(path) : null;
            Check(src != null, "B3 **前提**：读得到 shader 源文件（" + path + "）");
            if (src != null)
            {
                Check(src.Contains("SUPERSAMPLING_ON") && src.Contains("#pragma multi_compile"),
                      "B3a shader 声明了 `#pragma multi_compile __ SUPERSAMPLING_ON`"
                    + "（原版关键字名；⛔ 改名 = 全局开关失效且不报错）");
                Check(src.Contains("ddx(") && src.Contains("ddy(") && src.Contains("0.375") && src.Contains("0.125"),
                      "B3b shader 里有 4-tap RGSS 的 ddx/ddy 与 ±0.125/±0.375 偏移（原版 DXBC 反汇编）");
                Check(src.Contains("_AAScale"),
                      "B3c shader 用到 `_AAScale`（乘在 uv 上再取导数 = 原版 `v1.xyxy * cb0[2].x`）");
            }
        }
        catch (System.Exception e)
        {
            Check(false, "B3 读 shader 源文件抛异常：" + e.Message);
        }

        // B7：材质档 —— 卡框 / 卡背 = 1.25，共享基材（`info` 层）= 0。
        GameObject go = null;
        Texture2D back = null;
        try
        {
            go = new GameObject("B141W33_B");
            go.transform.SetParent(parent, false);
            var d = CardData.Placeholder(0);
            d.type = "unit";
            d.subtype = "Infantry";
            var card = CardView.Create(go.transform, d, "B141W33_cardB", CardFace.Full);
            Check(card != null, "B7 **前提**：夹具卡建出来了");
            if (card == null) return;

            MeshRenderer frameMr = null, infoMr = null, backMr = null;
            foreach (var r in card.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.gameObject.name == "frame") frameMr = r;
                else if (r.gameObject.name == "info") infoMr = r;
            }
            Check(frameMr != null && frameMr.sharedMaterial != null, "B8 **前提**：卡框层 `frame` 建出来了");
            Check(infoMr != null && infoMr.sharedMaterial != null, "B9 **前提**：共享基材层 `info` 建出来了");

            if (frameMr != null && frameMr.sharedMaterial != null)
            {
                Check(frameMr.sharedMaterial.HasProperty("_AAScale"), "B10 **前提**：卡框材质有 `_AAScale`");
                if (frameMr.sharedMaterial.HasProperty("_AAScale"))
                {
                    float v = frameMr.sharedMaterial.GetFloat("_AAScale");
                    Near(v, CardView.CardAaScale, 1e-4f,
                         "B★ 卡框 `_AAScale = **1.25**`（原版材质 `Card Frame and Cardback for combat` 的字面量）");
                    Near(v, 1.25f, 1e-4f, "B11 且它就是原版那个数（1.25）");
                }
            }

            if (infoMr != null && infoMr.sharedMaterial != null && infoMr.sharedMaterial.HasProperty("_AAScale"))
            {
                float v = infoMr.sharedMaterial.GetFloat("_AAScale");
                Near(v, 0f, 1e-4f,
                     "B4★ 共享基材（`info` 层）`_AAScale = **0**`（原版只有卡框/卡背超采样）"
                   + "｜🧨 把 `BaseMaterial` 也设 1.25 ⇒ 红");
                Check(Mathf.Abs(v - CardView.CardAaScale) > 1e-3f,
                      "B12（灭自证）共享基材 **不是** 1.25 —— 立绘/底板/数值层/`ImageQuad` 不许跟着超采样");
            }

            // 卡背：原版 `Cardback` 与 `CardFrame` **同一颗材质** ⇒ 也必须 1.25
            back = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 255);
            back.SetPixels32(px);
            back.Apply();
            card.SetBackVisible(true, back);
            foreach (var r in card.GetComponentsInChildren<MeshRenderer>(true))
                if (r.gameObject.name == "cardBack") { backMr = r; break; }
            Check(backMr != null && backMr.sharedMaterial != null, "B13 **前提**：卡背层 `cardBack` 建出来了");
            if (backMr != null && backMr.sharedMaterial != null && backMr.sharedMaterial.HasProperty("_AAScale"))
            {
                Near(backMr.sharedMaterial.GetFloat("_AAScale"), CardView.CardAaScale, 1e-4f,
                     "B14 卡背 `_AAScale = **1.25**`（原版 `Cardback` 与 `CardFrame` 挂的是同一颗材质）");
            }
        }
        finally
        {
            if (back != null) Object.DestroyImmediate(back);
            if (go != null) Object.DestroyImmediate(go);
        }

        // B15：全局关键字随画质档切换（原版 `enableCardsAntiAliasing` = 五档 0/0/0/1/1）。
        var q0 = GameStaticData.graphicsQuality;
        bool kw0 = Shader.IsKeywordEnabled("SUPERSAMPLING_ON");
        try
        {
            GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.VeryLow;
            SuperSampling.ApplyCardAntiAliasingKeyword();
            Check(!Shader.IsKeywordEnabled("SUPERSAMPLING_ON"),
                  "B15 画质 VeryLow(0) ⇒ 关键字 **关**（`enableCardsAntiAliasing = 0`）");

            GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.Medium;
            SuperSampling.ApplyCardAntiAliasingKeyword();
            Check(!Shader.IsKeywordEnabled("SUPERSAMPLING_ON"),
                  "B16 画质 Medium(2) ⇒ 关键字 **关**（0/1/2 都是 0）");

            GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.High;
            SuperSampling.ApplyCardAntiAliasingKeyword();
            Check(Shader.IsKeywordEnabled("SUPERSAMPLING_ON"),
                  "B17 ★ 画质 High(3) ⇒ 关键字 **开**（原版 `qualityDefinitions` 现读：quality 3 = 1）");

            GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.Ultra;
            SuperSampling.ApplyCardAntiAliasingKeyword();
            Check(Shader.IsKeywordEnabled("SUPERSAMPLING_ON"),
                  "B18 ★ 画质 Ultra(4) ⇒ 关键字 **开**（出厂默认档，原版默认就是超采样）"
                + "｜🧨 阈值写错（`> High` / 恒 true）⇒ B15/B16 或 B17 红");
        }
        finally
        {
            // 原样还原（⛔ 不给别的宿主留后遗症）
            GameStaticData.graphicsQuality = q0;
            if (kw0) Shader.EnableKeyword("SUPERSAMPLING_ON");
            else Shader.DisableKeyword("SUPERSAMPLING_ON");
        }
    }
}
