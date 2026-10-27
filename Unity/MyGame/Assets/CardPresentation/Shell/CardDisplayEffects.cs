// CardDisplayEffects.cs — 「卡片展示窗」那 15 条光效的**运行时建件器**（界面线 R2 续）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【判据】`Resources/Art/mainmenu_effects.json`（判据正本，生成器 `工具/gen_mainmenu_effects.py`）+
//   `Resources/Art/card_display_effects.json`（本文件消费的扁平 DTO —— 同一份判据的机器可读版）。
//   逐条出处（父链 / active / 组件类 / 矩形 / 粒子 enabled 模块 / 材质）都在那份 JSON 里，⛔ 别再翻解包。
//
// 【原版这 15 条挂在哪 / 什么时候出现（复算过，**不是猜**）】
//   · 父件 = `Card Displayer Menu For Menu`（原版类 **`CardDisplayWindow`**），它 `m_IsActive = false`
//     ⇒ 主菜单静止时**一个都不跑**；它们只在**展示窗打开时**才播（`playOnAwake = 1` 但父件关着）。
//   · 层级：`Card Displayer Menu For Menu ▸ Card Display ▸ CardUI ▸ Cards ▸ Card Display ▸ 2DCard ▸ {Glow Explosion, Lighting Rays, …}`
//   · 10 颗粒子都挂在 `2DCard`（那张大卡）下面；`Level Up Effect Down` 挂在 `Level Up Effect`（默认关）。
//
// 【我们怎么摆（坐标换算，唯一一处）】
//   原版这些节点的父件 `2DCard` 的 `m_SizeDelta = 2.0927 × 3.3313`（**原版画布单位**），
//   而本窗的卡体容器 `Cards` 是 `CardWinBox.CardsS` **px** ⇒
//     `unitToPx = CardsS / 2.0927`（原版单位 → px），`pxToWorld = 1/108`（本工程的 px→世界单位换算，`LayoutSpace.Px`）。
//   粒子节点 `localScale = unitToPx/108` ⇒ 粒子里那些「原版单位」的量（startSize / speed / shape.radius / 位置）
//   由 Unity 的 `scalingMode = Hierarchy` 自动乘上这个缩放，**不必逐字段换算**（这也是原版 `scalingMode = 0` 的语义）。
//
// 🔶 自己设计（非原版）：`unitToPx` 这个换算系数 · 「原版单位 → 本窗 px」的桥在本机没有第二个判据可用
//   （原版那份 prefab 的 canvas 缩放值不在离线数据里）· 调度台 2026-10-27 裁定 ·
//   ⇒ 只影响**大小/位移的绝对量级**，不影响模块参数本身；Unity 里等比调 `unitToPx` 一处即可。
//
// 【没做的两条（如实记，不是忘了）】
//   · `EffectElement ×5` = 原版类 **`CardEffectItem`**（卡面"效果文字"件，字段 `enchanterText`/`effectText`）
//     ⇒ 属**卡面文字**那一族，与本窗的 `EffectList` 是两套结构；硬塞进来会是第三套文字排版。
//   · `Craft` = `EverguildButton` **按钮件**（不是光效）⇒ 归"按钮/交互"那一族。
//   这两条都**在原版同一棵默认关闭的子树里**，不影响"打开展示窗时看到什么"的主线。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`CardDisplayWindow` 的光效层：从判据 JSON 建 10 颗粒子 + 3 条图件，可整层开关。</summary>
    public static class CardDisplayEffects
    {
        public const string DtoPath = "Art/card_display_effects";
        public const string MatDir = "Art/vfx/";

        /// <summary>建一层，挂到卡体容器下。返回那层根节点（取不到判据/材质时返回 null 并**出声**）。</summary>
        /// <param name="cardParent">卡体容器（本窗是 `Cards`；局部单位 = **世界单位**）。</param>
        /// <param name="cardWidthPx">该容器的宽度（px）—— 原版 `2DCard` 宽 2.0927 单位，见文件头。</param>
        public static Transform Build(Transform cardParent, float cardWidthPx)
        {
            if (cardParent == null) return null;
            var ta = Resources.Load<TextAsset>(DtoPath);
            if (ta == null)
            {
                Debug.LogWarning("[CardDisplayEffects] 取不到判据 `Resources/" + DtoPath + ".json` ⇒ **光效层不建**"
                                 + "（生成器 = `工具/gen_mainmenu_effects.py --apply`）");
                return null;
            }
            CdeDoc doc;
            try { doc = JsonUtility.FromJson<CdeDoc>(ta.text); }
            catch (Exception e)
            {
                Debug.LogWarning("[CardDisplayEffects] 判据 JSON 解析失败：" + e.Message + " ⇒ 光效层不建");
                return null;
            }
            if (doc == null || doc.particles == null) return null;

            float unitToPx = cardWidthPx > 1f ? cardWidthPx / 2.0927f : 1f;
            float s = unitToPx / 108f;                       // 原版单位 → 世界单位（见文件头换算那一段）

            var root = new GameObject("Card Display Effects").transform;
            root.SetParent(cardParent, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            int built = 0, noMat = 0;
            for (int i = 0; i < doc.particles.Length; i++)
            {
                var d = doc.particles[i];
                if (d == null || string.IsNullOrEmpty(d.name)) continue;
                var mat = Resources.Load<Material>(MatDir + d.material);
                if (mat == null)
                {
                    if (noMat++ == 0)
                        Debug.LogWarning("[CardDisplayEffects] 材质 `Resources/" + MatDir + d.material + "` 取不到 ⇒ "
                                         + "那一颗**不建**（导入入口 = `工具/import_mainmenu_effect_mats.py --apply`）");
                    continue;
                }
                var go = new GameObject(d.name);
                go.transform.SetParent(root, false);
                // 位置/朝向：原版 `RectTransform` 的字面量（单位 = 原版画布单位）
                go.transform.localPosition = new Vector3(d.posX * s, d.posY * s, 0f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, d.rotZ);
                go.transform.localScale = new Vector3(s * d.scaleX, s * d.scaleY, s * d.scaleZ);
                var ps = go.AddComponent<ParticleSystem>();
                var pr = go.GetComponent<ParticleSystemRenderer>();
                ApplyParticle(ps, pr, d, mat);
                built++;
            }

            for (int i = 0; i < (doc.images == null ? 0 : doc.images.Length); i++)
            {
                var im = doc.images[i];
                if (im == null || string.IsNullOrEmpty(im.name)) continue;
                BuildImage(root, im, s);
                built++;
            }

            Debug.Log("[CardDisplayEffects] 光效层建好：" + built + " 件"
                      + (noMat > 0 ? "（另有 " + noMat + " 颗粒子因材质缺失跳过）" : ""));
            return root;
        }

        // ============================================================ 粒子：逐模块灌值
        static void ApplyParticle(ParticleSystem ps, ParticleSystemRenderer pr, CdeParticle d, Material mat)
        {
            var main = ps.main;
            main.duration = Mathf.Max(0.01f, d.length);
            main.loop = d.looping;
            main.playOnAwake = d.playOnAwake;
            main.startDelay = d.startDelay;
            main.startLifetime = MM(d.life, 1f, 1f);
            main.startSpeed = MM(d.speed, 0f, 1f);
            main.startSize = MM(d.size, 1f, 1f);
            main.startSize3D = d.size3D;
            main.startRotation = MM(d.rot, 0f, 1f);   // ⚠️ 原版存的就是**弧度**（`6.28318` = 2π），Unity 的 API 也要弧度 ⇒ ⛔ 不换算
            main.startColor = Grad(d.color, Color.white);
            main.gravityModifier = MM(d.gravity, 0f, 1f);
            main.maxParticles = Mathf.Max(1, d.maxParticles);
            main.simulationSpeed = Mathf.Max(0.01f, d.simulationSpeed);
            main.scalingMode = d.scalingMode == 0 ? ParticleSystemScalingMode.Hierarchy
                                                 : (ParticleSystemScalingMode)d.scalingMode;

            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = (ParticleSystemShapeType)d.shapeType;
            sh.angle = d.shapeAngle;
            sh.radius = d.shapeRadius >= 0f ? d.shapeRadius : 1f;
            sh.radiusThickness = d.shapeRadiusThick;
            sh.scale = new Vector3(d.shapeScaleX, d.shapeScaleY, d.shapeScaleZ);
            sh.position = new Vector3(d.shapePosX, d.shapePosY, 0f);
            sh.rotation = new Vector3(d.shapeRotX, d.shapeRotY, 0f);
            sh.alignToDirection = d.alignToDirection;

            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = MM(d.rate, 0f, 1f);
            if (d.bursts != null && d.bursts.Length > 0)
            {
                var arr = new ParticleSystem.Burst[d.bursts.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    var b = d.bursts[i];
                    arr[i] = new ParticleSystem.Burst(b.time, (short)Mathf.Clamp(b.count, 0f, 32000f));
                    arr[i].cycleCount = Mathf.Max(1, b.cycles);
                    arr[i].repeatInterval = Mathf.Max(0.01f, b.interval);
                }
                em.SetBursts(arr);
            }

            var sol = ps.sizeOverLifetime;
            sol.enabled = d.sizeOverLife != null && d.sizeOverLife.curve != null && d.sizeOverLife.curve.Length > 1;
            if (sol.enabled) sol.size = new ParticleSystem.MinMaxCurve(1f, Curve(d.sizeOverLife));

            var rol = ps.rotationOverLifetime;
            rol.enabled = d.rotOverLife != null && d.rotOverLife.curve != null && d.rotOverLife.curve.Length > 1;
            if (rol.enabled)
            {
                rol.separateAxes = false;
                rol.z = new ParticleSystem.MinMaxCurve(1f, Curve(d.rotOverLife));
            }

            if (d.colorOverLife != null && d.colorOverLife.colorKeys != null && d.colorOverLife.colorKeys.Length > 0)
            {
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = Grad(d.colorOverLife, Color.white);
            }

            if (d.velX != null || d.velY != null)
            {
                var vel = ps.velocityOverLifetime;
                bool on = (d.velX != null && d.velX.mode != 0) || (d.velY != null && d.velY.mode != 0)
                          || (d.velX != null && Mathf.Abs(d.velX.a) > 1e-4f) || (d.velY != null && Mathf.Abs(d.velY.a) > 1e-4f);
                vel.enabled = on;
                if (on)
                {
                    vel.x = MM(d.velX, 0f, 1f);
                    vel.y = MM(d.velY, 0f, 1f);
                }
            }

            var uv = ps.textureSheetAnimation;
            uv.enabled = d.uvEnabled;
            if (d.uvEnabled)
            {
                uv.numTilesX = Mathf.Max(1, d.uvTilesX);
                uv.numTilesY = Mathf.Max(1, d.uvTilesY);
                uv.timeMode = (ParticleSystemAnimationTimeMode)d.uvAnimType;
                uv.fps = d.uvFps;
                // 🔴 2026-10-27（B1UI 补）：UV 帧曲线。原版 `Ray Right` = mode 1（Curve、2 个键）
                // ⇒ 不接的话 Unity 走默认匀速，射线扫过的节奏与原版不同（静默差异）。判据 = 判据 JSON 的 `uvFrameOverTime`
                if (d.uvFrameOverTime != null) uv.frameOverTime = MM(d.uvFrameOverTime, 1f, 1f);
            }

            pr.renderMode = (ParticleSystemRenderMode)d.renderMode;
            pr.sortingOrder = d.sortingOrder;
            pr.pivot = new Vector3(d.pivotX, d.pivotY, 0f);
            pr.maxParticleSize = d.maxParticleSize;
            pr.lengthScale = d.lengthScale;
            pr.sharedMaterial = mat;
            // 世界空间的排序：卡体那一族同层，靠 `sortingOrder`（原版逐颗不同：Swirl/Ray 1 · Glow/Sparks 6）
        }

        // ============================================================ 图件（Level Up / 左右黑边）
        static void BuildImage(Transform root, CdeImage im, float s)
        {
            // 🔴 原版这 3 条挂在**默认关闭**的父件下（`Level Up Effect` / `Borders`）⇒ 我们建出来**也是关的**，
            //    由调用方 `SetOn(true)` 显式打开（⛔ 别默认开：那会让展示窗一打开就多出三条原版不画的东西）。
            var go = new GameObject(im.name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(im.posX * s, im.posY * s, 0f);
            // 尺寸：`sizeDelta` 是 px（原版画布 px），按 `unitToPx` 折成世界单位 —— 与粒子同一把尺
            float w = Mathf.Abs(im.sizeX) * s, h = Mathf.Abs(im.sizeY) * s;
            if (w > 0.0001f && h > 0.0001f)
            {
                var quad = ImageQuad.Create(go.transform, CardArt.Solid(), Vector3.zero, LayoutSpace.DesignHeight,
                                            new Vector2(0.5f, 0.5f), im.name + " Gfx");
                if (quad != null)
                {
                    quad.SetWorldHeight(h);
                    quad.SetAspect(w / h);
                    quad.SetTint(new Color(im.colorR, im.colorG, im.colorB, im.colorA));
                }
            }
            go.SetActive(false);
            // 原版 `Level Up Effect Down` 挂 `UIBorderGlow`（`animTime 1 / animDelay 0 / playOnEnable 0`）。
            // ⚠️ 它靠材质的 `_Reveal` 属性做"从中间往两边揭开"；本工程**没有带 `_Reveal` 的 shader**
            //    （`grep -rl _Reveal --include=*.shader` = 0）⇒ 参数**照原版灌**，画面效果**待那个 shader 到位**；
            //    `UIBorderGlow` 自己会出声（`[UIBorderGlow] 材质上没有 _Reveal`），⛔ 不是静默失败。
            if (im.hasGlow)
            {
                var glow = go.AddComponent<UIBorderGlow>();
                glow.animTime = im.glowTime;
                glow.animDelay = im.glowDelay;
                glow.playOnEnable = im.glowPlayOnEnable != 0;
                glow.revealFrom = -0.5f;   // 原版 `AnimateUIMaterialProperty.floatProperty1.value = -0.5`（判据 = 判据 JSON 的 fields）
                glow.revealTo = 1f;
            }
        }

        // ============================================================ 曲线 / 渐变 换算
        /// <summary>`minMaxState`：0 Constant · 1 Curve · 2 TwoCurves · 3 TwoConstants
        /// （= Unity `ParticleSystemCurveMode` 的序数，判据 = 原版序列化字段名 `minMaxState`）。</summary>
        static ParticleSystem.MinMaxCurve MM(CdeMM m, float mul, float scale)
        {
            if (m == null) return new ParticleSystem.MinMaxCurve(0f);
            float a = m.a * scale, b = m.b * scale;
            switch (m.mode)
            {
                case 1: return new ParticleSystem.MinMaxCurve(mul, CurveScaled(m, mul, scale));
                case 2: return new ParticleSystem.MinMaxCurve(mul, CurveScaled(m, mul, scale), CurveScaled(m, mul, scale));
                case 3: return new ParticleSystem.MinMaxCurve(a * mul, b * mul);
                default: return new ParticleSystem.MinMaxCurve(a * mul);
            }
        }

        static AnimationCurve Curve(CdeMM m)
        {
            var keys = new List<Keyframe>();
            if (m != null && m.curve != null)
                for (int i = 0; i < m.curve.Length; i++) keys.Add(new Keyframe(m.curve[i].t, m.curve[i].v));
            if (keys.Count == 0) keys.Add(new Keyframe(0f, 1f));
            var c = new AnimationCurve(keys.ToArray());
            // 原版 m_PreInfinity/m_PostInfinity = 2（Clamp）；`AnimationCurve` 默认 Clamp ⇒ 一致
            return c;
        }

        static AnimationCurve CurveScaled(CdeMM m, float mul, float scale)
        {
            var keys = new List<Keyframe>();
            if (m != null && m.curve != null)
                for (int i = 0; i < m.curve.Length; i++) keys.Add(new Keyframe(m.curve[i].t, m.curve[i].v * scale));
            if (keys.Count == 0) keys.Add(new Keyframe(0f, m == null ? 0f : m.a * scale));
            return new AnimationCurve(keys.ToArray());
        }

        /// <summary>`ParticleSystemGradientMode`：0 Color · 1 Gradient · 2 TwoColors · 3 TwoGradients · 4 RandomColor。</summary>
        static ParticleSystem.MinMaxGradient Grad(CdeGrad g, Color fallback)
        {
            if (g == null) return new ParticleSystem.MinMaxGradient(fallback);
            var a = new Color(g.ca.r, g.ca.g, g.ca.b, g.ca.a);
            var b = new Color(g.cb.r, g.cb.g, g.cb.b, g.cb.a);
            switch (g.mode)
            {
                case 1:
                case 3:
                {
                    var gr = new Gradient();
                    var ck = new GradientColorKey[Mathf.Max(1, g.colorKeys == null ? 0 : g.colorKeys.Length)];
                    if (g.colorKeys != null && g.colorKeys.Length > 0)
                        for (int i = 0; i < g.colorKeys.Length; i++)
                            ck[i] = new GradientColorKey(new Color(g.colorKeys[i].r, g.colorKeys[i].g, g.colorKeys[i].b),
                                                         g.colorKeys[i].t);
                    else ck[0] = new GradientColorKey(a, 0f);
                    var ak = new GradientAlphaKey[Mathf.Max(1, g.alphaKeys == null ? 0 : g.alphaKeys.Length)];
                    if (g.alphaKeys != null && g.alphaKeys.Length > 0)
                        for (int i = 0; i < g.alphaKeys.Length; i++) ak[i] = new GradientAlphaKey(g.alphaKeys[i].a, g.alphaKeys[i].t);
                    else ak[0] = new GradientAlphaKey(1f, 0f);
                    gr.SetKeys(ck, ak);
                    return new ParticleSystem.MinMaxGradient(gr);
                }
                case 2: return new ParticleSystem.MinMaxGradient(a, b);
                default: return new ParticleSystem.MinMaxGradient(a);
            }
        }

        // ============================================================ DTO（与 card_display_effects.json 逐字段对应）
        [Serializable] public class CdePt { public float t; public float v; }
        [Serializable] public class CdeMM { public int mode; public float a; public float b; public CdePt[] curve; }
        [Serializable] public class CdeCol { public float r; public float g; public float b; public float a; }
        [Serializable] public class CdeCk { public float t; public float r; public float g; public float b; }
        [Serializable] public class CdeAk { public float t; public float a; }
        [Serializable] public class CdeGrad
        {
            public int mode; public CdeCol ca; public CdeCol cb;
            public CdeCk[] colorKeys; public CdeAk[] alphaKeys;
        }
        [Serializable] public class CdeBurst { public float time; public float count; public int cycles; public float interval; }
        [Serializable]
        public class CdeParticle
        {
            public string name; public string parent;
            public float posX, posY, rotZ;
            public float scaleX, scaleY, scaleZ;
            public float length; public bool looping; public bool playOnAwake; public float startDelay;
            public float simulationSpeed; public int scalingMode; public int maxParticles;
            public CdeMM life, speed, size, rot, gravity;
            public bool size3D;
            public CdeGrad color;
            public int shapeType; public float shapeAngle, shapeRadius, shapeRadiusThick;
            public float shapeScaleX, shapeScaleY, shapeScaleZ;
            public float shapePosX, shapePosY, shapeRotX, shapeRotY;
            public bool alignToDirection;
            public CdeMM rate; public CdeBurst[] bursts;
            public CdeMM sizeOverLife, rotOverLife;
            public CdeGrad colorOverLife;
            public CdeMM velX, velY;
            public bool uvEnabled; public int uvTilesX, uvTilesY, uvAnimType; public float uvFps;
            public CdeMM uvFrameOverTime;
            public int renderMode, sortingOrder;
            public float pivotX, pivotY, maxParticleSize, lengthScale;
            public string material;
        }
        [Serializable]
        public class CdeImage
        {
            public string name;
            public float posX, posY, sizeX, sizeY;
            public float anchorMinX, anchorMinY, anchorMaxX, anchorMaxY;
            public float colorR, colorG, colorB, colorA;
            public float glowTime, glowDelay; public int glowPlayOnEnable;
            /// <summary>这一件原版**有没有** `UIBorderGlow`（★ 别用 `glowTime` 猜：缺组件时默认值也是 1.0）。</summary>
            public bool hasGlow;
        }
        [Serializable] public class CdeDoc { public CdeParticle[] particles; public CdeImage[] images; }
    }
}
