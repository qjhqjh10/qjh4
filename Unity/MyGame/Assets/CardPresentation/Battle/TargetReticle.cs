// TargetReticle.cs — 原版的「选目标反馈」：准星 + 弧线
//
// 它回答的是「**我现在指着谁**」—— 和「**哪些**目标是合法的」（`BattleDriver.HighlightTargets`
// 把合法目标点亮）是两件事。原版这两套反馈是互补的，我们以前只有后者。
//
// 出处：解包场景 `07_场景/battlearena1/`（**这些 3D 的东西不在 UI dump 里**，只能从场景 JSON 挖）。
// `NoCanvas2D/Attack Target Reticle/` 下两个**同级**子节点：
//   · `Crosshair`        —— SpriteRenderer，sprite = 图集里的 `Atack_Icon BW`
//                           （180×182 px、`m_PixelsToUnits 100`），`m_LocalScale = 4.83`，
//                           父链（Attack Target Reticle ← NoCanvas2D ← BattlePrefab）scale 全是 1
//                           → 世界尺寸 **8.694 × 8.791**
//   · `CrosshairLine 3D` —— LineRenderer，`widthMultiplier 0.1` × lossyScale 1.6301 → 宽 **0.163**，
//                           `m_UseWorldSpace = true`；场景里存了 21 个采样点
// 控制器 `AttackTargetReticleController`：`colorPresets`（按 `attackType` 给准星色/拖尾色）、
// `colorChangeSpeed = 8.0`、`floorReference` → 地板参考点、`scaleOnChangeModifier = 0.2`。
//
// 🔴 **尺寸换算（2026-09-18 更正）**：那 8.694 / 0.163 / 2.0 是**原版自己的世界单位**，
//    但**它们属于不同的帧** —— 准星 sprite 在 **hudCamera 画布平面帧**（14.84 px/单位），
//    弧线在 **棋盘 3D 帧**（182.14 px/单位）。旧注释写「原版槽距 3.0 世界单位 = 149.3 px
//    ⇒ 49.77 px/单位」是**错的**（`3.0` 是编辑器占位值，运行时被换成 `MinionSeparation 0.82`）。
//    **逐条推导、两条独立验算与旧值的合理性反证，都写在下面那组常量的注释里**，改之前先读它。
//
// ✅ **「渐变两端谁是谁」这条 2026-09-17 更正**：原版 `CrosshairLineEffect.GenerateGradient(Color)`
//    **只写 1 个颜色键**（`Fixed` 模式）⇒ 整条线**单色**，「两端各一个色」这件事**不存在**。
//    我们原来写成 Trail→Cross 两键渐变，是**我们挑的、而且是错的**（已改回单色，见下面构造渐变那段）。
//
// ✅ 弧线的**材质是原版的**（曾经误以为拿不到，已纠正）：
//    原版材质叫 `CroshairTrail`，shader = `Everguild/FX/Unlit UV scroll`（URP ShaderGraph，双层
//    UV 滚动），`_MainTex` = 一张 64×64 的 `CrosshairTrail`（横向亮度渐变）。
//    ⚠️ 材质 JSON 里的 `m_Shader` 是**跨文件引用**（`m_FileID: 2`，只有 PathID 没有名字）。
//       我一开始**按名字**找、没找到，就误判成「原版没解出来」并顶替了 —— **错的**：
//       **解包工具的文件名里带 PathID**，`11_着色器/shaders/Shader_5091587426579848444.json`
//       就是它（`Everguild/FX/Unlit UV scroll`），按 PathID 找一秒就有，连 bundle 都不用扫。
//       贴图那次才是真的不在解包目录里：PathID 4731861827877171429 = `CrosshairTrail`，
//       在 `battlesharedresources_assets_all.bundle` 里，扫 bundle 才拿到。
//       **教训：引用只留 PathID —— 先按 PathID 找文件名，再回源 bundle。**
using UnityEngine;

namespace CardPresentation
{
    public class TargetReticle : MonoBehaviour
    {
        // ---- 原版量的值（世界单位）→ px ----
        // 🔴 **2026-09-18 更正：准星这两个节点本来就在【不同的帧】里，用一个系数套两边 = 两个方向都错。**
        //    （旧注释写「原版槽距 3.0 世界单位 = 149.3 px ⇒ 1 原版世界单位 = 49.77 px」——**作废**，
        //      `3.0` 从来不是运行时世界单位，见下。）
        //
        //   ① `Crosshair`（准星 sprite）→ **hudCamera 的画布平面帧**
        //      位置由 `BattleManager.Get2DWorldPosFromBoardPos` 给：
        //      `boardCamera.WorldToScreenPoint` → `hudCamera.ScreenToWorldPoint(…, Canvas.planeDistance)`
        //      ⇒ px/单位 = 1080 / (2 × planeDistance 100 × tan(40°/2)) = **14.84**
        //   ② `CrosshairLine 3D`（弧线）→ **棋盘 3D 帧**
        //      `LineRenderer.m_UseWorldSpace = true`、点集落在棋盘 x≈100、layer 10（只有 BoardCamera 的 mask 含它）
        //      ⇒ px/单位 = **182.14**（**玩家行处**；敌行处 86.2，透视逐点变 —— 按玩家行取，
        //        与原版弧线起点 z=-7.43 一致，也和 `BoardLayout` 文件头那同一个 182.14 对得上）
        //
        //   182.14 的**独立验算**（不靠我们自己的量）：BoardCamera FOV 46.3972°、z=-13.572，玩家行 z=-6.655
        //   ⇒ d=6.917 ⇒ 1080/(2×6.917×tan23.1986°) = **182.2 px/单位** ⇒ ×`MinionSeparation 0.82` = **149.4 px**
        //   ✓（实测 149.3）；敌行另算 ⇒ 86.2 ⇒ ×1.53 = **131.9 px** ✓ —— **两行同时对上，不是巧合**。
        //
        //   🔴 **`3.0` 的真身**：那是场景里 `leftSlotPosNormal` 的**编辑器占位值**（`-3.0/-6.0/-9.0`），
        //      `MinionManager.Awake` / `FillMinionPositions` 在运行时把它换成 `-0.82, -1.64, -2.46`
        //      （步距 = `MinionSeparation`）⇒ **它从不是世界单位**，拿它做分母得出来的 49.77 是错的。
        //
        //   合理性旁证：按新值，准星是 **129 px**（≈屏高 12%，一张场卡宽的 0.94）—— 像个准星；
        //   按旧值 432.7 px = 屏高 **40%**，那是「比三张场卡还宽」的一块 —— 明显不像。

        /// <summary>**准星 sprite** 的帧（`Crosshair`，hudCamera 画布平面）：1080/(2·planeDistance·tan(fov/2))</summary>
        const float SrcPxPerUnit = 14.84f;

        /// <summary>**弧线**的帧（`CrosshairLine 3D`，棋盘 3D，玩家行处）—— 与 `BoardLayout` 文件头同一个 182.14</summary>
        const float SrcPxPerUnitBoard = 182.14f;

        /// <summary>原版 `Crosshair` 的世界尺寸 8.694 × 8.791（**HUD 帧**）→ px ≈ 129.0 × 130.5</summary>
        const float CrossW = 8.694f * SrcPxPerUnit;
        const float CrossH = 8.791f * SrcPxPerUnit;

        /// <summary>原版 `CrosshairLine 3D` 的线宽 0.163（**棋盘帧**）→ px ≈ 29.7</summary>
        const float LineW = 0.163f * SrcPxPerUnitBoard;

        /// <summary>原版 `maxCurveProfileHeight` / `minCurveProfileHeight`：2.0 / 0.2（**棋盘帧**）→ px ≈ 364 / 36.4</summary>
        const float MaxArcH = 2.0f * SrcPxPerUnitBoard;
        const float MinArcH = 0.2f * SrcPxPerUnitBoard;

        /// <summary>原版 `distanceToMaxCurveHeight` = 8.0（**棋盘帧**）→ px ≈ 1457。
        /// 与棋盘纵深同量级（原版两行 z 跨 7.88）—— 即「跨满整个棋盘时弧线到最高」</summary>
        const float DistToMaxArc = 8.0f * SrcPxPerUnitBoard;

        /// <summary>本工程 1 世界单位 = 108 px（@1080p）—— 和 `AttackSelector` 同口径</summary>
        const float PxPerUnit = 108f;
        static float W(float px) { return px / PxPerUnit; }

        /// <summary>z 分层：越负越靠前。摆在卡牌前面、攻击方式选择器（-2.0）后面</summary>
        const float Z = -1.50f;

        /// <summary>原版脚本里的段数</summary>
        const int CurvePoints = 10;

        // ==================================================================
        //  原版 `colorPresets`（原样照抄，字段名就是 `crossHairColor` / `trailColor`）
        // ==================================================================

        struct Preset { public Color Cross, Trail; }

        static readonly Preset Melee = new Preset
        {
            Cross = new Color(0.9529412f, 0.08235294f, 0.003921569f, 1f),
            Trail = new Color(0.8117648f, 0f, 0f, 1f),
        };
        static readonly Preset Ranged = new Preset
        {
            Cross = new Color(0.8274510f, 0.3058824f, 0.9215687f, 1f),
            Trail = new Color(0.3568628f, 0f, 0.8117648f, 1f),
        };
        /// <summary>`attackType` 3 和 4 在原件里**数值完全相同**，都是金</summary>
        static readonly Preset Gold = new Preset
        {
            Cross = new Color(0.9137256f, 0.7764707f, 0.2039216f, 1f),
            Trail = new Color(0.9137256f, 0.5411765f, 0f, 1f),
        };

        /// <summary>打法 → 配色。红 = 近战、紫 = 远程、金 = 技能 —— 和三个按钮的配色一一对应</summary>
        static Preset PresetOf(AttackKind k)
        {
            switch (k)
            {
                case AttackKind.Ranged: return Ranged;
                case AttackKind.Ability: return Gold;
                default: return Melee;
            }
        }

        // ==================================================================
        //  弧线形状 —— 原版那两个 `AnimationCurve`，**连切线一起照抄**
        //  （原件是 AnimationCurve 不是裸数组；`weightedMode` 全是 0(None)，所以权重不参与求值）
        // ==================================================================

        static AnimationCurve _meleeProfile, _rangeProfile;

        static AnimationCurve MeleeProfile
        {
            get
            {
                if (_meleeProfile == null)
                    _meleeProfile = new AnimationCurve(
                        new Keyframe(-0.0099999998f, -0.0023584918f, 4.8671656f, 4.8671656f),
                        new Keyframe(0.5f, 1.0000005f, -0.017295659f, -0.017295659f),
                        new Keyframe(1f, 0f, -4.6727180f, -4.6727180f));
                return _meleeProfile;
            }
        }

        static AnimationCurve RangeProfile
        {
            get
            {
                if (_rangeProfile == null)
                    _rangeProfile = new AnimationCurve(
                        new Keyframe(0f, 0f, 0.19393052f, 0.19393052f),
                        new Keyframe(0.51249999f, 0.099389389f, -0.0049725696f, -0.0049725696f),
                        new Keyframe(1f, 0f, -0.20387566f, -0.20387566f));
                return _rangeProfile;
            }
        }

        // ==================================================================
        //  节点
        // ==================================================================

        ImageQuad _cross;
        LineRenderer _line;
        bool _built;

        public bool Visible { get; private set; }

        /// <summary>现在用的准星色 / 拖尾色（自检断言「近战红、远程紫、技能金」用）</summary>
        public Color CrossColor { get; private set; }
        public Color TrailColor { get; private set; }

        /// <summary>准星的世界尺寸（宽 × 高）。自检断言「那个 px 换算对不对」用 —— **判据只有这一份**</summary>
        public static Vector2 CrosshairWorldSize { get { return new Vector2(W(CrossW), W(CrossH)); } }

        /// <summary>弧线现在用的 shader 名（自检断言「到底有没有用上原版材质」用 —— 截图看不出这个）</summary>
        public string LineShaderName
        {
            get
            {
                var mt = (_line != null) ? _line.sharedMaterial : null;
                return (mt != null && mt.shader != null) ? mt.shader.name : "<无>";
            }
        }

        /// <summary>弧线材质的 `_MainTex` 叫什么（同上）</summary>
        public string LineTextureName
        {
            get
            {
                var mt = (_line != null) ? _line.sharedMaterial : null;
                var t = (mt != null) ? mt.GetTexture("_MainTex") : null;
                return (t != null) ? t.name : "<无>";
            }
        }

        /// <summary>准星正指着哪儿（自检断言用）。没开着返回 false</summary>
        public bool CrossPosition(out Vector3 world)
        {
            world = (_cross != null) ? _cross.transform.position : Vector3.zero;
            return Visible && _cross != null && _cross.gameObject.activeSelf;
        }

        /// <summary>弧线的采样点数（自检断言用）。没开着返回 0</summary>
        public int ArcPointCount
        {
            get { return (_line != null && _line.gameObject.activeSelf) ? _line.positionCount : 0; }
        }

        /// <summary>
        /// 弧线拱起多少 —— 各点到「起点→终点」那条直线的最大偏离。
        /// 自检断言「近战拱得比远程高」用（两条 profile 曲线差很多，但人在截图上看不出「够不够」）。
        /// </summary>
        public float ArcBulge
        {
            get
            {
                if (_line == null || !_line.gameObject.activeSelf || _line.positionCount < 2) return 0f;
                var a = _line.GetPosition(0);
                var b = _line.GetPosition(_line.positionCount - 1);
                var ab = b - a;
                float len2 = Mathf.Max(1e-6f, ab.sqrMagnitude);

                float max = 0f;
                for (int i = 1; i < _line.positionCount - 1; i++)
                {
                    var p = _line.GetPosition(i);
                    float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
                    max = Mathf.Max(max, Vector3.Distance(p, Vector3.Lerp(a, b, t)));
                }
                return max;
            }
        }

        void Awake() { Build(); }

        public void Build()
        {
            if (_built) return;
            _built = true;

            CardArt.Load();

            // 准星：原版那个 sprite 名**拼错了**（`Atack` 少个 t），文件名照抄，别改
            _cross = ImageQuad.Create(transform, CardArt.Ui("Atack_Icon_BW"), Vector3.zero,
                                      W(CrossH), new Vector2(0.5f, 0.5f), "Crosshair");

            var lineGo = new GameObject("CrosshairLine");
            lineGo.transform.SetParent(transform, false);
            _line = lineGo.AddComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.numCapVertices = 0;                        // 原版 0
            _line.numCornerVertices = 0;                     // 原版 0
            _line.alignment = LineAlignment.View;            // 原版 `alignment` = 1
            _line.textureMode = LineTextureMode.Stretch;     // 原版 0
            _line.shadowBias = 0.5f;                         // 原版 0.5
            _line.widthMultiplier = W(LineW);
            _line.positionCount = CurvePoints;
            _line.sortingOrder = 5;                          // 原版 `m_SortingOrder` = 5
            _line.material = BuildLineMaterial();

            Hide();
        }

        /// <summary>
        /// 弧线的材质 —— **原版的**：材质名 `CroshairTrail`，shader 是
        /// `Everguild/FX/Unlit UV scroll`（URP ShaderGraph，双层 UV 滚动），
        /// `_MainTex` = 64×64 的 `CrosshairTrail`（横向亮度渐变）。
        /// 各属性值照抄 `08_预制体特效/共享资源/Material/Material_2571589003054829852.json`。
        ///
        /// ⚠️ **拿不到原版 shader 时不静默** —— 退回一个看得见的替身并**打警告**。
        ///    （shader bundle 在 `StreamingAssets/WarpforgeVFX/`，`WarpforgeShaderLoader` 运行时加载。）
        /// </summary>
        Material BuildLineMaterial()
        {
            const string ShaderName = "Everguild/FX/Unlit UV scroll";

            Shader sh;
            if (!WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(ShaderName, out sh) || sh == null)
            {
                Debug.LogWarning("[TargetReticle] 取不到原版 shader「" + ShaderName +
                                 "」—— 弧线退回 Sprites/Default 替身（**不是原版的观感**）。" +
                                 "查 StreamingAssets/WarpforgeVFX/wf_shaders.bundle。");
                sh = Shader.Find("Sprites/Default");
            }

            var m = new Material(sh) { name = "CroshairTrail" };

            m.SetColor("_Color", new Color(1f, 1f, 1f, 1f));
            var tex = CardArt.Ui("CrosshairTrail");
            if (tex != null) m.SetTexture("_MainTex", tex);
            // `Vector4_62056e41ff4042358d02be808b0352f9` 的 property 名就是
            // 「UV Scale (XY) Speed (ZW)」—— ShaderGraph 里没起名字的 Vector4 会落成一串哈希名，**照抄**
            m.SetVector("Vector4_62056e41ff4042358d02be808b0352f9", new Vector4(10f, 1f, -0.63f, 0f));
            m.SetVector("Vector4_1", new Vector4(1f, 1f, 0.4f, 0f));      // 第二层（`_SecondaryTex` 是空的，不生效）
            m.SetFloat("_Layers_Blend_Opacity", 0f);
            m.SetFloat("_FinalAlphaMultiplier", 1f);
            m.SetFloat("_PREMULTIPLY", 0f);
            m.SetFloat("_SOFT", 0f);
            m.SetVector("_Depth_X_Falloff_Y", new Vector4(0.5f, 0.5f, 0f, 0f));

            // 混合状态（原版材质里就是这么写的：透明但**开 ZWrite**、关背面剔除）
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 1f);
            m.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);   // 原版 4 = LEqual
            m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            m.SetFloat("_AlphaClip", 0f);
            m.SetFloat("_AlphaToMask", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return m;
        }

        /// <summary>收起来。**不销毁节点** —— 选目标时每帧都可能开关，重建会掉帧</summary>
        public void Hide()
        {
            Visible = false;
            if (_cross != null) _cross.gameObject.SetActive(false);
            if (_line != null) _line.gameObject.SetActive(false);
        }

        /// <summary>
        /// 指向某个目标。`from` = 攻击方、`to` = 目标（都是世界坐标，取卡的中心）。
        /// 攻击方式决定颜色和弧线的拱高（近战拱得高、远程几乎是直的 —— 原版两条 profile 曲线）。
        /// </summary>
        public void Show(Vector3 from, Vector3 to, AttackKind kind)
        {
            Build();

            var p = PresetOf(kind);
            CrossColor = p.Cross;
            TrailColor = p.Trail;
            from.z = Z; to.z = Z;

            if (_cross != null)
            {
                _cross.gameObject.SetActive(true);
                _cross.SetTint(p.Cross);
                // 准星**压在目标上**（比弧线再靠前一点，别被线穿过去）
                _cross.transform.localPosition = to + new Vector3(0f, 0f, -0.02f);
            }

            UpdateArc(from, to, kind, p);
            Visible = true;
        }

        void UpdateArc(Vector3 from, Vector3 to, AttackKind kind, Preset p)
        {
            if (_line == null) return;
            _line.gameObject.SetActive(true);

            // 拱高随距离长 —— 远了才拱到满（`distanceToMaxCurveHeight`）
            float dist = Vector3.Distance(from, to);
            float peak = Mathf.Lerp(W(MinArcH), W(MaxArcH), Mathf.Clamp01(dist / W(DistToMaxArc)));

            var profile = (kind == AttackKind.Ranged) ? RangeProfile : MeleeProfile;

            _line.positionCount = CurvePoints;
            for (int i = 0; i < CurvePoints; i++)
            {
                float t = i / (float)(CurvePoints - 1);
                _line.SetPosition(i, Vector3.Lerp(from, to, t) + Vector3.up * (profile.Evaluate(t) * peak));
            }

            // 渐变：**alpha 四个键是原样的**（0→1→1→0，两头淡出）；
            // 🔴 **颜色键只有 1 个**（2026-09-17 照反编译更正）——
            //    `CrosshairLineEffect.GenerateGradient(Color color)`（`CrosshairLineEffect__GenerateGradient.c`）：
            //    `new Gradient()` → `set_mode(1)`（**Fixed**）→ **只 new 1 个 `GradientColorKey`（time=0）**
            //    → `set_colorKeys` → 再把 **lineRenderer 现有的 alphaKeys 原样搬过来**。
            //    ⇒ **整条线是单色**，没有两端渐变。
            //    ⚠️ 我们原来写成「起点 Trail、终点 Cross」两键渐变 —— 那是**我们挑的**（注释也这么写的），
            //       **错的**：原版签名只吃一个 `Color`，不存在「两端各一个色」这回事。
            //    用哪个色：原版那个 `Color` 由调用方传入，而**调用点不在反编译集里**（只有这个方法本身）；
            //    按字段语义取**拖尾色 `trailColor`**（`crossHairColor` 是准星那几片 sprite 的色）。
            var g = new Gradient();
            g.mode = GradientMode.Fixed;                 // 原版 `set_mode(1)`
            g.SetKeys(
                new[] { new GradientColorKey(p.Trail, 0f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 377f / 65535f),      // 原版 atime1
                    new GradientAlphaKey(1f, 63522f / 65535f),    // 原版 atime2
                    new GradientAlphaKey(0f, 1f),
                });
            _line.colorGradient = g;
        }
    }
}
