// RankedDivisionParticles.cs — 「段位变化窗」两棵粒子树的**运行时建件器**（账 `个人资料_成就_排行:221`）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【为什么需要它】原版 `RankedEventDivisionChangeWindowNewSystem.ChangeParticleEffectAndSpriteRank(
//   Image targetImage, ParticleSystem targetParticleSystem, Sprite rankSprite)` 的 ③④ 两跳要
//   「给 `targetParticleSystem.textureSheetAnimation.SetSprite(0, rankIcon.m_Sprite)` 并把它
//    `GetComponent<Renderer>().enabled = (rankSprite != null)`」。而 `targetParticleSystem` 指向的**不是**根节点，
//   是两棵树里各自的 **`Sparks Burn ▸ Promote Letter Effect`**：
//     · `divisionParticleSystem` = ps `1201952928742936910` = **Particles DivisionUp ▸ Sparks Burn ▸ Promote Letter Effect**
//     · `rankParticleSystem`     = ps `-6284612905696259762` = **Particles Rank Up ▸ Sparks Burn ▸ Promote Letter Effect**
//   判据 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-4938671765695524530.json` 的两个 PPtr
//   （同一个 MB 里 `rankIcon` = `-691165018575761074`、`rankNumberGlow` = `-8635674026998525618`）。
//   ⇒ 之前我们只建了空容器 + `NoteNoArt`（`Shell/RankedDivisionChangeWindow.cs:339-342`），③④ 无处可落。
//
// 【判据（全部机器生成，⛔ 没有一个是手抄）】
//   `Resources/Art/ranked_division_particles.json` ← 生成器 `工具/gen_ranked_division_particles.py`。
//   它按 `Transform.m_Children` 逐级下钻读 `bundle_menus_assets_all`，**同名子件按 `m_Father` 链配对**
//   （`Sparks Burn` / `Glow Big` / `Glow below` / `Promote Letter Effect` / `Sparks fast` 各有两份，
//   分属两棵树；⛔ 文件名的 `_<pid>` 后缀只是导出器给同名 GO 的序号，与树无关）。
//
// 【坐标桥（唯一一处推导，别在别处再乘除一次）】
//   原版画布是 **1 px = 1 单位**（`Ranked Division Change Window` 1920×1080、根 `scale = 1`），
//   而本工程 `LayoutSpace` 是 **108 px = 1 世界单位**（`DesignPxH / DesignHeight`）。两棵树在画布里带自己的
//   缩放（`Particles Rank Up` 根 `localScale = 164.237`、`Particles DivisionUp` 根 `1`；子件普遍 250 / 1.522 / 0.657），
//   粒子又走 `scalingMode = Hierarchy`（0）⇒ **长度量由父链缩放自动乘上**。
//   ⇒ 正确做法 = **只把两棵树的【根】** 的位置与缩放同除 108，**子树以下逐字照抄**：
//     · `Rank Up` 根：pos `(0,88.9,−26.6)` → `(0, 0.8231, −0.2463)` world；scale `164.237` → `1.52072`
//       ⇒ 子件 `Sparks Burn` 局部 pos `(−0.006,−0.038)` 乘上去 = `−0.98 px`（原版同）；其 `startSize 0.05`
//       乘上去 `= 12.5 px`（原版同）。
//     · `DivisionUp` 根：pos `(0,0)`、scale `1` → `0.0092593` ⇒ 子件局部 pos `(−1, 41)`（原版 px）= `(−1,41) px`；
//       其 scale `250` 乘上去 `= 2.315` 世界单位 `= 250 px`（原版同）。
//   ⚠️ **两条必需**：① 根以下的 `scale` 不除 108；② 根以下的 `pos` 不除 108。少任何一条，粒子的**大小**或
//   **位置**就会偏 108 倍（这是本文件最容易写错的一处）。
//
// 【没做到的（如实记，不是忘了）】
//   · `SubModule`（子发射器）**没接**：实测这 14 颗 `ParticleSystem` 的 `SubModule.enabled` **全部 = false**
//     ⇒ 接了也是空转，本批不写（数据里也没有内容）。
//   · `ParticleSystemRenderer.m_Materials[1]`（只有 `Trails R/L` 有第二张）没接 —— 它们 `m_RenderMode = 5 (None)`
//     **不画粒子本体**，拖尾用的是 `m_Materials[0]`。
//
// 【与 `CardDisplayEffects` 的关系】那份 `ApplyParticle` 是「卡片展示窗 15 条光效」专用的：它**无条件**
//   `shape.enabled = true`（本批 5 颗原版是 `false`），且**没有** Noise / Trail 模块 —— 照搬会把「点发射」做成「锥形发射」、
//   并把 4 颗火花粒子的噪声运动整片丢掉。⇒ 本文件自带一份铺满这批模块的 applier，**已记为待收口项**
//   （两份应合并成一个共用 builder，见报告「顺手发现」）。⛔ 本批**没有**改 `CardDisplayEffects.cs`（怕与在飞的特效线撞车）。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>段位变化窗两棵粒子树的运行时建件器（`Particles DivisionUp` / `Particles Rank Up`）。</summary>
    public static class RankedDivisionParticles
    {
        public const string DtoPath = "Art/ranked_division_particles";
        /// <summary>原版画布 px → 本工程世界单位的换算（`LayoutSpace.DesignPxH / DesignHeight`）。</summary>
        public const float PxPerUnit = 108f;

        static bool _warnedNoDto, _warnedNoMat, _warnedNoPs;

        /// <summary>建件结果（自检读口；`Ok = false` 时两棵树退化成「只有节点名」的空容器，与原行为一致）。</summary>
        public class Result
        {
            public bool Ok;
            public Transform Division, Rank;
            /// <summary>`Particles DivisionUp ▸ Sparks Burn`（clips 的 path 指向它）。</summary>
            public Transform SparksBurnDivision;
            /// <summary>`Particles DivisionUp ▸ Sparks Burn ▸ Promote Letter Effect` 的 `ParticleSystem`（③④ 的目标）。</summary>
            public ParticleSystem DivisionPs;
            /// <summary>`Particles Rank Up ▸ Sparks Burn ▸ Promote Letter Effect` 的 `ParticleSystem`（③④ 的目标）。</summary>
            public ParticleSystem RankPs;
            /// <summary>建出来的节点数 / 其中带 `ParticleSystem` 的 / 因材质缺失跳过的。</summary>
            public int Nodes, WithPs, NoMaterial;
        }

        /// <summary>按判据 JSON 建两棵树，挂到窗口根下。取不到判据 ⇒ 只建空容器并**出声一次**。</summary>
        public static Result Build(Transform window)
        {
            var res = new Result();
            if (window == null) return res;

            RdpDoc doc = null;
            var ta = Resources.Load<TextAsset>(DtoPath);
            if (ta == null)
            {
                Warn(ref _warnedNoDto, "取不到判据 `Resources/" + DtoPath + ".json` ⇒ 两棵粒子树退化成空容器"
                                     + "（生成器 = `工具/gen_ranked_division_particles.py --apply`）");
            }
            else
            {
                try { doc = JsonUtility.FromJson<RdpDoc>(ta.text); }
                catch (Exception e) { Warn(ref _warnedNoDto, "判据 JSON 解析失败：" + e.Message + " ⇒ 两棵粒子树退化成空容器"); }
            }

            if (doc == null || doc.nodes == null || doc.nodes.Length == 0)
            {
                res.Division = MenuDraw.Node(window, "Particles DivisionUp", new PxRect(0f, 0f, 1920f, 1080f));
                res.SparksBurnDivision = MenuDraw.Node(res.Division, "Sparks Burn", new PxRect(909f, 449f, 1009f, 549f));
                res.Rank = MainMenuSubmenuWindow.NewPlainTransform(window, "Particles Rank Up");
                return res;
            }

            var byId = new Dictionary<string, Transform>();
            for (int i = 0; i < doc.nodes.Length; i++)
            {
                var n = doc.nodes[i];
                if (n == null || string.IsNullOrEmpty(n.id)) continue;
                bool isRoot = string.IsNullOrEmpty(n.parent);
                var parent = isRoot ? window : (byId.ContainsKey(n.parent) ? byId[n.parent] : window);
                if (!isRoot && parent == window)
                {
                    Warn(ref _warnedNoPs, "判据里 `" + n.name + "` 的父件 `" + n.parent + "` 不存在 ⇒ 挂到窗口根（位置/缩放仍用判据值）");
                }

                Transform t = n.kind == "R"
                    ? new GameObject(n.name, typeof(RectTransform)).transform
                    : MainMenuSubmenuWindow.NewPlainTransform(parent, n.name);
                if (n.kind == "R") t.SetParent(parent, false);
                t.gameObject.layer = parent.gameObject.layer;      // 粒子渲染吃 layer 剔除 ⇒ 跟父件同层

                float k = isRoot ? (1f / PxPerUnit) : 1f;          // 见文件头「坐标桥」那一段
                t.localPosition = new Vector3(n.x * k, n.y * k, n.z * k);
                t.localRotation = Quaternion.Euler(0f, 0f, n.rotZ);
                t.localScale = new Vector3(n.sx * k, n.sy * k, n.sz * k);
                byId[n.id] = t;
                res.Nodes++;

                if (n.hasPs && n.ps != null)
                {
                    var mat = string.IsNullOrEmpty(n.ps.material)
                        ? null : Resources.Load<Material>("Art/vfx/" + n.ps.material);
                    if (mat == null)
                    {
                        res.NoMaterial++;
                        Warn(ref _warnedNoMat, "材质 `Resources/Art/vfx/" + n.ps.material + "` 取不到 ⇒ 那一颗**不建**"
                                             + " 粒子（导入入口 = `工具/import_mainmenu_effect_mats.py --apply`）");
                    }
                    else
                    {
                        var ps = t.gameObject.AddComponent<ParticleSystem>();
                        var pr = t.GetComponent<ParticleSystemRenderer>();
                        Apply(ps, pr, n.ps, mat);
                        res.WithPs++;
                    }
                }

                if (isRoot)
                {
                    if (n.name == "Particles DivisionUp") res.Division = t;
                    else if (n.name == "Particles Rank Up") res.Rank = t;
                }
            }

            if (res.Division != null)
            {
                res.SparksBurnDivision = res.Division.Find("Sparks Burn");
                var sb = res.SparksBurnDivision;
                if (sb != null) res.DivisionPs = PsOf(sb.Find("Promote Letter Effect"));
            }
            if (res.Rank != null)
            {
                var sb = res.Rank.Find("Sparks Burn");
                if (sb != null) res.RankPs = PsOf(sb.Find("Promote Letter Effect"));
            }
            res.Ok = res.Division != null && res.Rank != null;
            return res;
        }

        static ParticleSystem PsOf(Transform t) => t == null ? null : t.GetComponent<ParticleSystem>();
        static Renderer RenOf(ParticleSystem ps) => ps == null ? null : ps.GetComponent<Renderer>();

        /// <summary>`Renderer.enabled`（= 原版 ④ 写的那一位）。自检读口（不依赖渲染）。</summary>
        public static bool RendererEnabled(ParticleSystem ps)
        {
            var r = RenOf(ps);
            return r != null && r.enabled;
        }

        static void Warn(ref bool flag, string msg)
        {
            if (flag) return;
            flag = true;
            Debug.LogWarning("[RankedDivisionParticles] " + msg);
        }

        // ============================================================ 粒子：逐模块灌值（判据 = DTO 的每一格）
        static void Apply(ParticleSystem ps, ParticleSystemRenderer pr, RdpPs d, Material mat)
        {
            var main = ps.main;
            main.duration = Mathf.Max(0.01f, d.length);
            main.loop = d.looping;
            main.playOnAwake = d.playOnAwake;
            main.startDelay = d.startDelay;
            main.startLifetime = MM(d.life, 1f);
            main.startSpeed = MM(d.speed, 1f);
            main.startSize = MM(d.size, 1f);
            main.startSize3D = d.size3D;
            // ⚠️ 原版 `startRotation` 存的就是**弧度**（Unity 的 API 也要弧度）⇒ ⛔ 不换算
            main.startRotation = MM(d.rot, 1f);
            main.startColor = Grad(d.color, Color.white);
            main.gravityModifier = MM(d.gravity, 1f);
            main.maxParticles = Mathf.Max(1, d.maxParticles);
            main.simulationSpeed = Mathf.Max(0.01f, d.simulationSpeed);
            main.scalingMode = d.scalingMode == 0 ? ParticleSystemScalingMode.Hierarchy
                                                 : (ParticleSystemScalingMode)d.scalingMode;

            // 🔴 `Shape.enabled` 必须照原版：这批 14 颗里有 5 颗是 `false`（`Glow Big` / `Glow below` /
            //    `Promote Letter Effect`）—— 一律 `true` 会把「点发射」变成锥形发射（静默差异）。
            var sh = ps.shape;
            sh.enabled = d.shapeEnabled;
            sh.shapeType = (ParticleSystemShapeType)d.shapeType;
            sh.angle = d.shapeAngle;
            sh.radius = d.shapeRadius >= 0f ? d.shapeRadius : 1f;
            sh.radiusThickness = d.shapeRadiusThick;
            sh.scale = new Vector3(d.shapeScaleX, d.shapeScaleY, d.shapeScaleZ);
            sh.position = new Vector3(d.shapePosX, d.shapePosY, 0f);
            sh.rotation = new Vector3(d.shapeRotX, d.shapeRotY, 0f);
            sh.alignToDirection = d.alignToDirection;

            var em = ps.emission;
            em.enabled = d.emissionEnabled;
            em.rateOverTime = MM(d.rate, 1f);
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
            sol.enabled = HasCurve(d.sizeOverLife);
            if (sol.enabled) sol.size = new ParticleSystem.MinMaxCurve(1f, Curve(d.sizeOverLife));

            var rol = ps.rotationOverLifetime;
            rol.enabled = HasCurve(d.rotOverLife);
            if (rol.enabled)
            {
                rol.separateAxes = false;
                rol.z = new ParticleSystem.MinMaxCurve(1f, Curve(d.rotOverLife));
            }

            var col = ps.colorOverLifetime;
            col.enabled = d.colorOverLife != null
                          && ((d.colorOverLife.colorKeys != null && d.colorOverLife.colorKeys.Length > 0)
                              || d.colorOverLife.mode >= 2);
            if (col.enabled) col.color = Grad(d.colorOverLife, Color.white);

            var vel = ps.velocityOverLifetime;
            bool velOn = (d.velX != null && d.velX.mode != 0) || (d.velY != null && d.velY.mode != 0)
                         || (d.velX != null && Mathf.Abs(d.velX.a) > 1e-4f)
                         || (d.velY != null && Mathf.Abs(d.velY.a) > 1e-4f);
            vel.enabled = velOn;
            if (velOn)
            {
                if (d.velX != null) vel.x = MM(d.velX, 1f);
                if (d.velY != null) vel.y = MM(d.velY, 1f);
                vel.space = ParticleSystemSimulationSpace.Local;
            }

            var uv = ps.textureSheetAnimation;
            uv.enabled = d.uvEnabled;
            if (d.uvEnabled)
            {
                uv.numTilesX = Mathf.Max(1, d.uvTilesX);
                uv.numTilesY = Mathf.Max(1, d.uvTilesY);
                // 🔴 `animationType`（WholeSheet=0 / SingleRow=1）与 `timeMode`（Lifetime=0 / Speed=1 / FPS=2）
                //    是**两个**序列化字段，别互相当对方写（本批 `timeMode` 全 0、`animationType` 有 0 有 1）。
                uv.animation = (ParticleSystemAnimationType)d.uvAnimType;   // Unity 的 API 名是 `animation`，⛔ 不是 `animationType`
                uv.timeMode = (ParticleSystemAnimationTimeMode)d.uvTimeMode;
                uv.rowIndex = Mathf.Max(0, d.uvRowIndex);
                uv.cycleCount = Mathf.Max(1, Mathf.RoundToInt(d.uvCycles));
                uv.fps = d.uvFps;
                if (d.uvFrameOverTime != null) uv.frameOverTime = MM(d.uvFrameOverTime, 1f);
            }

            // ---- Noise（4 颗火花粒子用；`CardDisplayEffects` 那份没有这一块）----
            var noi = ps.noise;
            noi.enabled = d.noiseEnabled;
            if (d.noiseEnabled)
            {
                noi.separateAxes = d.noiseSeparateAxes;
                noi.strength = MM(d.noiseStrength, 1f);
                if (d.noiseSeparateAxes)
                {
                    noi.strengthYMultiplier = d.noiseStrengthY == null ? 1f : d.noiseStrengthY.a;
                    noi.strengthZMultiplier = d.noiseStrengthZ == null ? 1f : d.noiseStrengthZ.a;
                }
                noi.frequency = d.noiseFrequency;
                noi.damping = d.noiseDamping;
                noi.octaveCount = Mathf.Clamp(d.noiseOctaves, 1, 4);
                noi.octaveMultiplier = d.noiseOctaveMultiplier;
                noi.octaveScale = d.noiseOctaveScale;
                noi.quality = (ParticleSystemNoiseQuality)Mathf.Clamp(d.noiseQuality, 0, 2);
                noi.scrollSpeed = MM(d.noiseScrollSpeed, 1f);
                noi.remapEnabled = d.noiseRemapEnabled;
                if (d.noiseRemapEnabled && d.noiseRemap != null) noi.remap = MM(d.noiseRemap, 1f);
                noi.positionAmount = MM(d.noisePositionAmount, 1f);
                noi.rotationAmount = MM(d.noiseRotationAmount, 1f);
                noi.sizeAmount = MM(d.noiseSizeAmount, 1f);
            }

            // ---- Trail（`Trails R/L` 用；它们 `renderMode = 5 (None)`，只画拖尾）----
            var tr = ps.trails;
            tr.enabled = d.trailEnabled;
            if (d.trailEnabled)
            {
                tr.mode = d.trailMode == 0 ? ParticleSystemTrailMode.PerParticle : ParticleSystemTrailMode.Ribbon;
                tr.ratio = d.trailRatio;
                tr.lifetime = d.trailLifetime == null ? tr.lifetime : MM(d.trailLifetime, 1f);
                tr.minVertexDistance = d.trailMinVertexDistance;
                tr.textureMode = (ParticleSystemTrailTextureMode)d.trailTextureMode;
                tr.textureScale = new Vector2(d.trailTextureScaleX, d.trailTextureScaleY);
                tr.ribbonCount = Mathf.Max(1, d.trailRibbonCount);
                tr.worldSpace = d.trailWorldSpace;
                tr.dieWithParticles = d.trailDieWithParticles;
                tr.sizeAffectsWidth = d.trailSizeAffectsWidth;
                tr.inheritParticleColor = d.trailInheritParticleColor;
            }

            pr.renderMode = (ParticleSystemRenderMode)d.renderMode;
            pr.sortingOrder = d.sortingOrder;
            pr.pivot = new Vector3(d.pivotX, d.pivotY, 0f);
            pr.maxParticleSize = d.maxParticleSize;
            pr.lengthScale = d.lengthScale;
            pr.sharedMaterial = mat;
            // 原版 ④ 的目标就是这一位（`GetComponent<Renderer>().enabled`）——出厂 `true`，
            // 由 `RankedDivisionChangeWindow.ApplyParticleAndGlow()` 按 `rankSprite != null` 改写。
        }

        static bool HasCurve(RdpMM m) => m != null && m.curve != null && m.curve.Length > 1;

        /// <summary>`minMaxState`：0 Constant · 1 Curve · 2 TwoCurves · 3 TwoConstants（= Unity 的序数）。</summary>
        static ParticleSystem.MinMaxCurve MM(RdpMM m, float mul)
        {
            if (m == null) return new ParticleSystem.MinMaxCurve(0f);
            switch (m.mode)
            {
                case 1: return new ParticleSystem.MinMaxCurve(mul, Curve(m));
                case 2: return new ParticleSystem.MinMaxCurve(mul, Curve(m));
                case 3: return new ParticleSystem.MinMaxCurve(m.a * mul, m.b * mul);
                default: return new ParticleSystem.MinMaxCurve(m.a * mul);
            }
        }

        static AnimationCurve Curve(RdpMM m)
        {
            var keys = new List<Keyframe>();
            if (m != null && m.curve != null)
                for (int i = 0; i < m.curve.Length; i++) keys.Add(new Keyframe(m.curve[i].t, m.curve[i].v));
            if (keys.Count == 0) keys.Add(new Keyframe(0f, m == null ? 0f : m.a));
            return new AnimationCurve(keys.ToArray());   // 原版 `m_PreInfinity/m_PostInfinity = 2`（Clamp）= 默认
        }

        /// <summary>`ParticleSystemGradientMode`：0 Color · 1 Gradient · 2 TwoColors · 3 TwoGradients · 4 RandomColor。</summary>
        static ParticleSystem.MinMaxGradient Grad(RdpGrad g, Color fallback)
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
                        for (int i = 0; i < g.alphaKeys.Length; i++)
                            ak[i] = new GradientAlphaKey(g.alphaKeys[i].a, g.alphaKeys[i].t);
                    else ak[0] = new GradientAlphaKey(1f, 0f);
                    gr.SetKeys(ck, ak);
                    return new ParticleSystem.MinMaxGradient(gr);
                }
                case 2: return new ParticleSystem.MinMaxGradient(a, b);
                // `4 = RandomColor`：**在 min/max 两色之间随机**（本批 `Sparks Burn` 一族就是这一档）。
                // ⚠️ `CardDisplayEffects.Grad` 没有这一档（落到 default 只取 min 色）⇒ 那是那份的缺口，见报告「顺手发现」。
                case 4:
                {
                    var m = new ParticleSystem.MinMaxGradient(a, b);
                    m.mode = ParticleSystemGradientMode.RandomColor;
                    return m;
                }
                default: return new ParticleSystem.MinMaxGradient(a);
            }
        }

        // ============================================================ DTO（与 ranked_division_particles.json 逐字段对应）
        [Serializable] public class RdpPt { public float t; public float v; }
        [Serializable] public class RdpMM { public int mode; public float a; public float b; public RdpPt[] curve; }
        [Serializable] public class RdpCol { public float r; public float g; public float b; public float a; }
        [Serializable] public class RdpCk { public float t; public float r; public float g; public float b; }
        [Serializable] public class RdpAk { public float t; public float a; }
        [Serializable]
        public class RdpGrad
        {
            public int mode; public RdpCol ca; public RdpCol cb;
            public RdpCk[] colorKeys; public RdpAk[] alphaKeys;
        }
        [Serializable] public class RdpBurst { public float time; public float count; public int cycles; public float interval; }

        [Serializable]
        public class RdpPs
        {
            public float length; public bool looping; public bool playOnAwake; public float startDelay;
            public float simulationSpeed; public int scalingMode; public int maxParticles;
            public RdpMM life, speed, size, rot, gravity;
            public bool size3D; public RdpGrad color;
            public bool shapeEnabled; public int shapeType; public float shapeAngle, shapeRadius, shapeRadiusThick;
            public float shapeScaleX, shapeScaleY, shapeScaleZ;
            public float shapePosX, shapePosY, shapeRotX, shapeRotY; public bool alignToDirection;
            public bool emissionEnabled; public RdpMM rate; public RdpBurst[] bursts;
            public RdpMM sizeOverLife, rotOverLife; public RdpGrad colorOverLife;
            public RdpMM velX, velY;
            public bool uvEnabled; public int uvTilesX, uvTilesY, uvAnimType; public float uvFps; public RdpMM uvFrameOverTime;
            public int uvTimeMode, uvRowIndex; public float uvCycles;
            public bool noiseEnabled; public RdpMM noiseStrength, noiseStrengthY, noiseStrengthZ;
            public bool noiseSeparateAxes; public float noiseFrequency; public bool noiseDamping;
            public int noiseOctaves; public float noiseOctaveMultiplier, noiseOctaveScale; public int noiseQuality;
            public RdpMM noiseScrollSpeed; public bool noiseRemapEnabled; public RdpMM noiseRemap;
            public RdpMM noisePositionAmount, noiseRotationAmount, noiseSizeAmount;
            public bool trailEnabled; public int trailMode; public float trailRatio; public RdpMM trailLifetime;
            public float trailMinVertexDistance; public int trailTextureMode;
            public float trailTextureScaleX, trailTextureScaleY; public int trailRibbonCount;
            public bool trailWorldSpace, trailDieWithParticles, trailSizeAffectsWidth, trailInheritParticleColor;
            public int renderMode, sortingOrder; public float pivotX, pivotY, maxParticleSize, lengthScale;
            public string material;
        }

        [Serializable]
        public class RdpNode
        {
            public string id; public string name; public string parent;
            /// <summary>`"R"` = 原版是 `RectTransform`；`"T"` = 原版是**裸 `Transform`**（`Particles Rank Up` / `Wave Shine` 一族）。</summary>
            public string kind;
            public float x, y, z, sx, sy, sz, rotZ;
            public bool hasPs; public RdpPs ps;
        }

        [Serializable] public class RdpDoc { public RdpNode[] nodes; }
    }
}
