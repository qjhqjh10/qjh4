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
// 控制器 `TargetReticleController`：`colorPresets`（按 `attackType` 给准星色/拖尾色）、
// `colorChangeSpeed = 8.0`、`floorReference` → 地板参考点、`scaleOnChangeModifier = 0.2`。
// 🔴 **2026-10-25 就地订正（铁律 5）**：这两行原来写的类名是 `AttackTargetReticleController` ——
//   全量反编译里**没有这个类**；那份序列化数据（`bundle_scenes_scenes_battlearena1/MonoBehaviour/
//   MonoBehaviour_5098.json`，挂在 GO 347 上）的 `m_Script` 就是 **`TargetReticleController`**
//   （四档 preset：`attackType` 1 / 2 / 3 / 4，逐值与下面 `Melee`/`Ranged`/`Gold` 相同）。
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
using DG.Tweening;
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

        /// <summary>z 分层：越负越靠前。摆在卡牌前面、攻击方式选择器（-2.0）后面。
        /// ⚠️ 它是**HUD 分层**用的，不是几何 —— 所以自检断言「落点」要看
        /// <see cref="LastAim"/>，不能看准星节点的 z（那个被这里改写成 HUD 平面）。</summary>
        public const float Z = -1.50f;

        // ==================================================================
        //  淡入淡出 + 换打法 punch —— 2026-09-29 补（原版 `TargetReticleController`）
        //  出处：`TargetReticleController__Update.c` · `TargetReticleController__SetAttackType.c`
        //        · 常量从 `GameAssembly.dll` 浮点池实读
        // ==================================================================

        /// <summary>原版 `TargetReticleController.colorChangeSpeed`（字段 `+0x38`）——
        /// **alpha ramp 的速率（每秒）**。⇒ 0→1 用 **0.125 s**。</summary>
        const float ColorChangeSpeed = 8.0f;

        /// <summary>原版 `scaleOnChangeModifier`（字段 `+0x48`）—— 换打法时准星 punch 的幅度：
        /// **punch = 原始 scale × 0.2**。</summary>
        const float ScaleOnChangeModifier = 0.2f;

        /// <summary>原版那次 `DOPunchScale` 的时长 = `DAT_1834b2bb4`（浮点池实读 `0000003f`）= **0.5 s**；
        /// 另外两个实参：**vibrato = 0** · **elasticity = `DAT_1834b2bb8` = 1.0f**。</summary>
        const float ScalePunchTime = 0.5f;

        /// <summary>当前淡入淡出进度（1 = 完全不透明）。自检断言用。</summary>
        public float FadeAlpha { get { return _fadeAlpha; } }

        float _fadeAlpha;
        /// <summary>`+1` 淡入 / `-1` 淡出 / `0` 停 —— 就是原版那个
        /// `toggleFadeAnimationDirection`(`+0xA8`) + `enabled` 两个东西的两态。</summary>
        int _fadeDir;

        /// <summary>准星「原始 scale」（原版 `spriteOriginalScale` `+0xAC`，`Awake` 里存下来）——
        /// punch 的基准就是它。</summary>
        Vector3 _crossOriginalScale = Vector3.one;

        /// <summary>上一次用的打法（换打法才 punch —— 原版 `SetAttackType` 只在「设打法」时调）。</summary>
        AttackKind _lastPunchKind = AttackKind.None;

        /// <summary>
        /// 推进淡入淡出（原版 `TargetReticleController.Update`）。
        ///
        /// **原版逐帧做的两件事**（逐句照 `TargetReticleController__Update.c:39-68`）：
        ///   · 准星 sprite 的 `color.a` **+= dir × dt × colorChangeSpeed**（`dir` = `toggleFadeAnimationDirection ? +1 : -1`）
        ///   · **弧线材质的 `color.a` 走【同一个】方向**（同一段里两块代码都是
        ///     `iVar = 1; if (+0xa8 == 0) iVar = -1;` —— **同一个字段、同一个符号**）。
        ///   · sprite 的 alpha 到 `1`（淡入完）或 `0`（淡出完）就 `enabled = false`；
        ///     **淡出那一支还会顺手 `ToggleCrosshair(false)` 把节点关掉**。
        /// 🔴🔴 **2026-10-25 就地订正（铁律 5）**：本段原来写着「弧线走【相反】方向 …… 别当 bug 改掉」——
        ///   **那是读错了**：`.c` 里那两块是**同一方向**（见上；`grep -n "iVar10\|iVar7" TargetReticleController__Update.c`
        ///   两处守卫完全一样）。按「反向」实现（`1 − fadeAlpha`）的后果是**准星完全亮起来那一刻，
        ///   弧线材质 alpha 正好是 0 ⇒ 整条弧线在正常游玩里【始终看不见】**（批处理自检也看不出来，
        ///   因为没有任何断言读弧线的颜色）。
        /// ⚠️ 泵只有一个：`BattleDriver.AdvanceTimeline`（自检走 `BattleScene.Step`）。
        ///   ⛔ **本件【不】再自带 `Update`** —— 2026-10-25 之前这里有一个 `void Update() { TickFade(dt); }`，
        ///   而 `BattleDriver.Update` 每帧又调 `AdvanceTimeline(dt)` ⇒ 真机上淡入淡出**按 2 倍速**跑
        ///   （0.125 s 变 0.0625 s；批处理里 `Update` 不跑，所以自检永远看不出来）。
        /// </summary>
        public bool TickFade(float dt)
        {
            if (_fadeDir == 0 || dt <= 0f) return false;

            _fadeAlpha += _fadeDir * dt * ColorChangeSpeed;
            if (_fadeDir > 0 && _fadeAlpha >= 1f) { _fadeAlpha = 1f; _fadeDir = 0; }
            else if (_fadeDir < 0 && _fadeAlpha <= 0f) { _fadeAlpha = 0f; _fadeDir = 0; Deactivate(); }
            ApplyFade();
            return true;
        }

        // ⛔ **2026-10-25 删掉过一句 `void Update() { TickFade(Time.deltaTime); }`** ——
        //    唯一的泵是 `BattleDriver.AdvanceTimeline`（它每帧调一次 `TickFade`），
        //    两处一起泵 = 真机 2 倍速、批处理看不出来（判据 → `TickFade` 的 doc）。

        /// <summary>把当前的 `_fadeAlpha` 铺到准星与弧线上（**同一方向**，见 <see cref="TickFade"/>）。
        /// <para>准星：`preset.a × fade`（原版 `SpriteRenderer.set_color` 只改 a，rgb 不动）。
        /// 弧线：只改材质 `_Color` 的 a（rgb 恒 1，颜色由 `LineRenderer.colorGradient` 供）。
        /// ⚠️ 原版**不夹** alpha（`+=` 到越界就靠 `set_color` 自己收），我们在 `TickFade` 里夹到 [0,1]，
        /// 终值一致（`ToggleCrosshair(immediate)` 那两支都是直接写 0/1）。</para></summary>
        void ApplyFade()
        {
            if (_cross != null)
                _cross.SetTint(new Color(CrossColor.r, CrossColor.g, CrossColor.b, CrossColor.a * _fadeAlpha));

            var mt = (_line != null) ? _line.sharedMaterial : null;
            if (mt != null) mt.SetColor("_Color", new Color(1f, 1f, 1f, _fadeAlpha));
        }

        /// <summary>把两个节点真的关掉（淡出走完 / `Hide(immediate)` 走这条）。</summary>
        void Deactivate()
        {
            if (_cross != null) _cross.gameObject.SetActive(false);
            if (_line != null) _line.gameObject.SetActive(false);
        }

        /// <summary>原版 `CrosshairLineEffect.curvePoints`（字段 `+0x28`）= **段数**。
        ///
        /// 🔴 **2026-09-29 更正：它是段数，不是点数** —— 原版 `CrosshairLineEffect__SetPoints.c` 里
        /// 循环是 `uVar10 = 0; do{ … } while ((int)uVar10 &lt;= curvePoints)` ⇒ **一个个来，含两端** ⇒
        /// **点数 = `curvePoints + 1` = 11**；曲线的参数也是 `t = uVar10 ÷ curvePoints`。
        /// 我们原来把 10 直接当 `positionCount`（= 9 段）、`t = i ÷ 9` —— **点数少 1、采样也不对**。
        /// （出处：`d:/2/tools/decomp_full/CrosshairLineEffect__SetPoints.c`）
        /// </summary>
        const int CurvePoints = 10;

        /// <summary>弧线**点数**（= 原版 `curvePoints + 1`）。`LineRenderer.positionCount` 用这个。</summary>
        const int ArcVerts = CurvePoints + 1;

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

        /// <summary>打法 → 配色。红 = 近战、紫 = 远程、金 = 技能 —— 和三个按钮的配色一一对应。
        /// <para>🔴 **2026-10-25 就地订正（铁律 5）：`AttackKind.None`（attackType 0）【没有】配色。**
        /// 判据 = `TargetReticleController__SetAttackType.c` 里那个
        /// `if (*(int *)(preset + 0x10) == param_2)` 循环 —— 预置表只有 **1 / 2 / 3 / 4** 四条
        /// （`MonoBehaviour_5098.json` 实读：`attackType 3` 与 `4` 逐值相同），**0 一条都没有**
        /// ⇒ 那一下**一个颜色都不写**（准星保持 `SpriteRenderer.m_Color = (1,1,1,1)` 的白、
        /// 弧线保持材质/渐变原色）。原来我们 `default: return Melee` ⇒ 起手（还没选打法）那一帧
        /// **凭空变红**，与原版不符（`BattleDriver.OpenCommand` 里第一次 `Show` 喂的就是 `_command = None`）。</para></summary>
        static bool TryPresetOf(AttackKind k, out Preset p)
        {
            switch (k)
            {
                case AttackKind.Melee:   p = Melee;  return true;
                case AttackKind.Ranged:  p = Ranged; return true;
                case AttackKind.Ability: p = Gold;   return true;
                default: p = Melee; return false;          // attackType 0 / 不在表内 ⇒ 原版不写颜色
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

        /// <summary>现在用的准星色 / 拖尾色（自检断言「近战红、远程紫、技能金」用）。
        /// <para>初值 = **原版序列化值**：准星 `SpriteRenderer_1754.m_Color = (1,1,1,1)`；
        /// 弧线 `LineRenderer_1609` 的 `colorGradient` 前两个颜色键也是白 ⇒ 两条都取**白**。
        /// 🔴 这一条有判据意义：`attackType 0` 那一下原版**不改色**（见 `TryPresetOf`）⇒ 起手看到的就是它们。</para></summary>
        public Color CrossColor { get; private set; } = Color.white;
        public Color TrailColor { get; private set; } = Color.white;

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

        /// <summary>自检用：弧线是不是**世界空间**（原版 `CrosshairLine 3D` 的 `m_UseWorldSpace = true`）。</summary>
        public bool ArcUseWorldSpace { get { return _line != null && _line.useWorldSpace; } }

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

            // 🔴 **2026-10-11（A218）判「不改」**（这处**故意**保持**裸 `Transform`**，⛔ 别补 `RectTransform`）：
            //    判据 = 原版这件就是裸 `Transform` —— `bundle_scenes_scenes_battlearena1` 实读：
            //    `CrosshairLine 3D`（go_pid 848）= **`Transform`**（`localPos (147.8, −0.0014, −88.84)`、
            //    `scale 1.6301`），它的 `LineRenderer`（`LineRenderer_1609`）宿主就是它；
            //    同一批 7 个 `LineRenderer` 宿主（`Smoke Column *`）**全是 `Transform`**。
            //    它是**世界空间的 3D 线**（`m_UseWorldSpace = true`，见下），没有矩形语义
            //    ⇒ 写 `sizeDelta` 只会造一个**没有判据的数**（铁律 3）。
            var lineGo = new GameObject("CrosshairLine");
            lineGo.transform.SetParent(transform, false);
            _line = lineGo.AddComponent<LineRenderer>();
            // 🔴 2026-09-29 照原版改：场景里 `CrosshairLine 3D` 的 **`m_UseWorldSpace = true`**。
            //    我们原来写 false（点按**本节点**的局部坐标解），在父级恒为单位阵时看不出差别 ——
            //    但原版是明写的 true，而且我们 `Show()` 收到的是**世界坐标**（卡体 `transform.position`）
            //    ⇒ 现在把点算好之后过 `transform.TransformPoint` 换成世界坐标再喂（父级非单位阵也稳）。
            _line.useWorldSpace = true;
            _line.numCapVertices = 0;                        // 原版 0
            _line.numCornerVertices = 0;                     // 原版 0
            _line.alignment = LineAlignment.View;            // 原版 `alignment` = 1
            _line.textureMode = LineTextureMode.Stretch;     // 原版 0
            _line.shadowBias = 0.5f;                         // 原版 0.5
            _line.widthMultiplier = W(LineW);
            _line.positionCount = ArcVerts;                  // 11 个点（原版 `curvePoints + 1`）
            _line.sortingOrder = 5;                          // 原版 `m_SortingOrder` = 5
            _line.material = BuildLineMaterial();

            // 原版 `Awake` 把准星的 `localScale` 存成 `spriteOriginalScale`（`+0xAC`）—— punch 的基准
            if (_cross != null) _crossOriginalScale = _cross.transform.localScale;

            Hide(true);          // 初始化直接关（别走淡出：那时还没有帧循环，会卡在半透明）
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

        /// <summary>收起来。**不销毁节点** —— 选目标时每帧都可能开关，重建会掉帧。
        /// 🆕 2026-09-29：默认走**淡出**（原版 `TargetReticleController.Update` 那条 `colorChangeSpeed 8.0`
        /// 的 ramp，淡到底自己把节点关掉）。</summary>
        public void Hide() { Hide(false); }

        /// <summary>`immediate = true` ⇒ **不淡出、直接关**（原版 `ToggleCrosshair(show, immediate)` 里
        /// `immediate` 那一支：`param_3 != 0` 时直接 `SetActive(show)` + 把颜色 a 打成 0/1）。
        /// 初始化与自检的「我要它立刻没了」用它。</summary>
        public void Hide(bool immediate)
        {
            Visible = false;
            Cursor.visible = true;      // 原版 `Cursor.set_visible(show ^ 1)` 的另一半
            if (immediate) { _fadeDir = 0; _fadeAlpha = 0f; ApplyFade(); Deactivate(); return; }
            _fadeDir = -1;              // 开始淡出（原版 `toggleFadeAnimationDirection = show = false`）
            ApplyFade();
        }

        // ==================================================================
        //  双平面求交（原版 `TargetReticleController.UpdateTrail`）
        //
        //  原版那条链（逐句从 `TargetReticleController__UpdateTrail.c` + `__Initialize.c` 读出来的）：
        //   ① `screen = boardCamera.WorldToScreenPoint(准星世界位)`
        //   ② `ray = hudCamera.ScreenPointToRay(screen)`
        //   ③ 对**两个平面**各做一次 `Plane.Raycast`，**取近的那个**（打不中记 `float.MaxValue`，
        //      哨兵 `DAT_1834b2d94` 实读 = `3.4028e38`）
        //   ④ 命中点写回 `desiredWorldPosition`，再 `CrosshairLineEffect.SetPoints(anchor, 那一点)`
        //
        //  🔴 **两个平面是什么、从哪来**（`TargetReticleController__Initialize.c:78/105/128`，
        //     全工程唯一的写入点；`Plane(inNormal, inPoint)` 两参构造 ⇒ `distance = −Dot(normal, inPoint)`）：
        //   · `floorPlane`      = `new Plane(Vector3.up,    floorReference.position)`
        //                        ⇒ normal **(0,1,0)**、distance = **−floorReference.position.y**
        //   · `enemyMinionPlane` = `new Plane(Vector3.back, enemyMinionManager.transform.position)`
        //                        ⇒ normal **(0,0,−1)**、distance = **+pos.z**
        //   · （同族的 `playerMinionPlane` `+0x80` = `Plane(Vector3.forward, playerMinionManager.position)`
        //      只有 `RaycastToWorld` 的 `isPlayer` 那一支用，**`UpdateTrail` 不碰它**）
        //  ⚠️ 原版那两处的 `position` 值在预制体/场景里（`floorReference` 还是 `[SerializeField]`）⇒ 我们取**本工程实测的那两个**：
        //     地面 `y = 0`（卡站在地面上，见 `BattleDriver` 里 3D 落点那一段）、敌兵行 `z = ArenaSlots.EnemyZ`（`MinionArea` 的 local z）。
        //  🔴 **2026-10-28（`B91`）**：本支的判据是 `TargetReticleController.UpdateTrail`（两条 `Plane__Raycast`，`:89/:117`），⛔ **不是** `BattleManager.GetTargetInPos` —— 后者的 2D 支传 `Physics2D.GetRayIntersectionNonAlloc(..., -1f)` ⇒ **恒 0 命中**（负距离 = 原版怪值，照抄、⛔ 别改成 `Mathf.Infinity`），真生效的是它自己的 3D 回退支（见 `CardInteraction.Frontmost` 的注）。
        // ==================================================================

        /// <summary>原版 `boardCamera`（透视/正交的**战场**相机）—— `WorldToScreenPoint` 用它</summary>
        public Camera boardCam;
        /// <summary>原版 `hudCamera`（UHD 那一台，`ScreenPointToRay` 用它）。空则退回 <see cref="Camera.main"/></summary>
        public Camera hudCam;
        /// <summary>`floorPlane` 的高度（原版 = `floorReference.position.y`）；我们地面在 y=0</summary>
        public float floorY = 0f;
        /// <summary>`enemyMinionPlane` 所在的 z（原版 = 敌方 `MinionManager` 的 z）</summary>
        public float enemyRowZ = ArenaSlots.EnemyZ;

        /// <summary>
        /// 把「准星世界位」这条射线打到两个平面上、**取近的那个命中点**（原版 `UpdateTrail` 的 ①～④）。
        /// 两个平面都没打中（射线背对）⇒ 原版落点写 **世界原点**（双哨兵 `fVar9<=fVar10` 取第二平面默认值 `Vector3.zero`），并把 `AimMisses` 记一笔（自检看得见）。
        /// </summary>
        public Vector3 ResolveAim(Vector3 reticleWorld)
        {
            // 🔴 **没有独立的战场相机 ⇒ 不求交**（2D 兜底布局）：那种布局里「世界坐标」本来就是屏幕平面
            //    上的坐标，拿射线去撞平面等于把点搬走。原版那套几何只对**透视的 3D 战场**成立。
            //    （3D 战场由 `BattleScene` 建出来并把 `boardCam` 交给 `BattleDriver`。）
            if (boardCam == null) { LastAim = reticleWorld; LastAimOnPlane = false; return reticleWorld; }   // 🔶 自己设计（非原版）：2D 兜底布局没有战场相机 ⇒ 不做平面求交（原版此处会 NRE）；调度台 2026-10-29 B94_X2

            var bc = boardCam;
            var hc = hudCam != null ? hudCam : Camera.main;
            if (hc == null) { AimMisses++; LastAim = reticleWorld; LastAimOnPlane = false; return reticleWorld; }

            Vector3 screen = bc.WorldToScreenPoint(reticleWorld);
            if (screen.z < 0f)
            {
                AimMisses++; LastAim = reticleWorld; LastAimOnPlane = false;   // 在相机背后
                return reticleWorld;
            }

            Ray ray = hc.ScreenPointToRay(screen);
            var floor = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));                 // normal (0,1,0)
            var enemy = new Plane(Vector3.back, new Vector3(0f, 0f, enemyRowZ));            // normal (0,0,-1)

            // 原版：两个各打一次、**取近的**；打不中记 `float.MaxValue`（哨兵 3.4028e38）
            float dFloor = float.MaxValue, dEnemy = float.MaxValue, d;
            if (floor.Raycast(ray, out d)) dFloor = d;
            if (enemy.Raycast(ray, out d)) dEnemy = d;

            float best = Mathf.Min(dFloor, dEnemy);
            if (best == float.MaxValue)
            {   // 原版两平面都打空 ⇒ 两个距离都是哨兵 float.MaxValue，`fVar9 <= fVar10`（相等）取第二平面那一支的默认点 = 静态 `Vector3.zero` ⇒ 落点写【世界原点】，⛔ 不是 reticleWorld（照抄原版怪值）
                AimMisses++; LastAim = Vector3.zero; LastAimOnPlane = false;
                return LastAim;
            }
            LastAim = ray.GetPoint(best);
            LastAimOnPlane = true;
            return LastAim;
        }

        /// <summary>最近一次 <see cref="ResolveAim"/> 的落点（**求交之后、抹 z 之前**）。
        /// 自检用它断言「落点真的落在平面上」—— 准星节点自己的 z 是 HUD 分层用的，看不出这件事。</summary>
        public Vector3 LastAim { get; private set; }
        /// <summary>最近一次求交是否**真的打中了平面**（`false` = 没有战场相机 / 射线背对 / 两个平面都没中）。</summary>
        public bool LastAimOnPlane { get; private set; }

        /// <summary>自检用：`ResolveAim` 打空了几次（原版那条路上没有计数器，这是我们加的观测点）。</summary>
        public int AimMisses { get; private set; }

        /// <summary>
        /// **选目标状态开着时，每帧把指针喂进来**（原版 `BattleManager.MoveCrosshair`）——
        /// `anchor` = 攻击方、`pointerWorld` = 指针的世界坐标。
        /// 与 <see cref="Show"/> 的区别：**不重启淡入、不重放 punch**（那些只在状态开始那一下做）。
        /// </summary>
        public void Aim(Vector3 anchor, Vector3 pointerWorld, AttackKind kind)
        {
            if (!Visible) return;                       // 状态没开 ⇒ 什么都别做（`Show` 才是开状态那一刻）
            // 换打法那一拍 = 原版 `AttackButtonClick → SetAttackType`（改色 + punch，见 `ApplyAttackType`）
            if (kind != _lastPunchKind) ApplyAttackType(kind);
            var aim = ResolveAim(pointerWorld);
            aim.z = Z;
            if (_cross != null) { var c = pointerWorld; c.z = Z; _cross.transform.localPosition = c + new Vector3(0f, 0f, -0.02f); }   // 🔴 准星本体跟【原始指针】走（原版 `MoveCrosshair.c` 第一句 set_position(targetPositionUISpace)）；弧线终点才是 `UpdateTrail` 求交点
            anchor.z = Z;
            UpdateArc(anchor, aim, kind);
        }


        /// <summary>
        /// 🆕 2026-09-29：**AI 演出**用 —— 把准星摆到**已经算好的那个世界点**上
        /// （不做 <see cref="ResolveAim"/> 的平面求交：原版那条路的终点是目标的 2D 位置，本来就是算好的）。
        /// 与 <see cref="Aim"/> 的区别只有这一条；弧线照样从 `anchor`（施法者）连过去。
        ///
        /// 判据 → `BattleManager__EnemyTargetingAnim.c:32-51`：先把准星**瞬移**到施法者那儿
        /// （`PrepareMovement`，`…__PrepareMovement.c:16-37` 里是 `set_position`，**没有补间**），
        /// 再 `DoCrosshairMove(目标, VarsGlobal.targettingAnimTime)` = `DOMove(准星, 目标, 0.5)`
        /// **不带 `SetEase`** ⇒ 走 DOTween 的默认缓动（`…__DoCrosshairMove.c:21-28`；全库没人改过
        /// `DOTween.defaultEaseType`）。调用方每帧把插值后的点喂进来（`BattleDriver.TickAiTargetingAnim`）。
        /// </summary>
        public void AnimTo(Vector3 anchor, Vector3 world, AttackKind kind)
        {
            if (!Visible) return;
            if (kind != _lastPunchKind) ApplyAttackType(kind);      // 同 `Aim`：换打法就换色 + punch
            var aim = world;
            aim.z = Z;
            if (_cross != null) _cross.transform.localPosition = aim + new Vector3(0f, 0f, -0.02f);
            anchor.z = Z;
            UpdateArc(anchor, aim, kind);
        }

        /// <summary>
        /// **开**（选目标状态开始那一下）。`from` = 攻击方、`to` = **准星指到的那一点**（都是世界坐标）。
        /// 攻击方式决定颜色和弧线的拱高（近战拱得高、远程几乎是直的 —— 原版两条 profile 曲线）。
        /// ⚠️ `to` 应当已经过 <see cref="ResolveAim"/> 换算（原版弧线终点 = 射线打到地板/敌兵平面的那个命中点），
        /// 不是目标卡的中心。
        /// 🔴 调用时机照原版：**进入选目标状态就亮**，不是「指针压在合法目标上才亮」
        /// （原版 `ToggleCrosshair(true, false)` 的六个调用点全在 `BattleManager` 的「开始选目标」那几支，
        /// 见 `资料/待办判据_战场与战斗视图.md` Q7 那张表第 6 条）。
        /// </summary>
        public void Show(Vector3 from, Vector3 to, AttackKind kind)
        {
            Build();

            from.z = Z; to.z = Z;

            if (_cross != null)
            {
                _cross.gameObject.SetActive(true);
                // 准星**压在目标上**（比弧线再靠前一点，别被线穿过去）
                _cross.transform.localPosition = to + new Vector3(0f, 0f, -0.02f);
            }

            // 原版 `PrepareMovement` 的收尾三句就是 SetPoints → **SetAttackType** ⇒
            //   「颜色（有 preset 才写）+ 换打法 punch」都发生在**每次开始选目标**这一拍，
            //   ⛔ 不是「只在换打法时」（见 <see cref="ApplyAttackType"/>）。
            ApplyAttackType(kind);
            UpdateArc(from, to, kind);
            Visible = true;
            // 🆕 2026-09-29 照原版：准星亮起来时**藏掉系统光标**（`TargetReticleController__ToggleCrosshair`
            //   里的 `Cursor.set_visible(show ^ 1)`）—— 原版就是「准星代替鼠标指针」。
            Cursor.visible = false;

            // 🆕 淡入（原版 `ToggleCrosshair(show=true)` ⇒ `toggleFadeAnimationDirection = true`
            //   ⇒ `Update` 里 sprite 的 alpha 往 1 走；弧线材质**同方向**，见 `TickFade`）
            _fadeDir = +1;
            ApplyFade();
        }

        /// <summary>原版 `TargetReticleController.SetAttackType(attackType)` **整件事**：
        /// ① 表里有这一档才写准星 / 弧线的颜色（`attackType 0` 不写 —— 见 `TryPresetOf`）；
        /// ② **无条件** punch（原版那个方法里颜色循环之后紧接着就是 `DOPunchScale`，没有「同一档就不弹」的闸）。
        /// <para>调用点（原版）= `PrepareMovement`（**每次开始选目标**，`StartAttackFrom` / `StartSpellFrom` /
        /// `StartTargetedActiveAbility` / `StartChoosingAbilityTargetByClick` / `EnemyTargetingAnim` 都走它）
        /// ＋ `UnitOnBoardAttackTypeSelector.AttackButtonClick`（**换打法**那一拍）。
        /// ⇒ 我们这边：`Show`（= PrepareMovement）每次调；`Aim`/`AnimTo` 在打法**变了**时调（= 点击换打法）。</para>
        /// <para>🔴 **2026-10-25 就地订正（铁律 5）**：原来只有一个 `PunchIfKindChanged`，注释写「只在换打法那一下，
        /// 不是每次显示」—— **与 `.c` 不符**（`PrepareMovement.c` 收尾就调 `SetAttackType`）。
        /// 而且它**顺带漏了另一件事**：换打法时我们原来**只换了弧线颜色**（`Aim` 里 `PresetOf(kind)`），
        /// **准星本体不换色**（`CrossColor` 只在 `Show` 里写）⇒ 近战→远程时「弧线紫、准星还红」。</para></summary>
        void ApplyAttackType(AttackKind kind)
        {
            Preset p;
            if (TryPresetOf(kind, out p))
            {
                CrossColor = p.Cross;
                TrailColor = p.Trail;
                ApplyFade();                     // 准星 tint 立刻跟上（保持当前淡入进度）
            }
            _lastPunchKind = kind;
            Punch();
        }

        /// <summary>原版那一拍 `DOPunchScale(原始Scale × scaleOnChangeModifier(0.2), 0.5s, vibrato 0, elasticity 1.0)`：
        /// **正在播就 `DORestart`、没在播就先归位再 punch**（`TargetReticleController__SetAttackType.c`）。
        /// 自检用 <see cref="PunchCount"/> 数「弹过几次」（⛔ 它不判弹成什么形状 —— 形状由 `BattleScene` 那两条采样断言管）。</summary>
        void Punch()
        {
            if (_cross == null) return;
            PunchCount++;
            var t = _cross.transform;
            if (DOTween.IsTweening(t)) { t.DORestart(); return; }        // 原版 `IsTweening ⇒ DORestart(tween, true)`
            t.localScale = _crossOriginalScale;                          // 原版 `set_localScale(originalScale)`
            t.DOPunchScale(_crossOriginalScale * ScaleOnChangeModifier, ScalePunchTime, 0, 1f)
             .SetUpdate(CardTween.Mode);
        }

        /// <summary>自检用：换打法 punch 触发过几次（**只在换打法那一下**，不是每次显示）。</summary>
        public int PunchCount { get; private set; }

        /// <summary>自检用：准星当前的 `localScale.x`。</summary>
        public float CrossScaleX { get { return _cross != null ? _cross.transform.localScale.x : 0f; } }

        /// <summary>自检用：**弧线材质 `_Color.a`** —— 原版与准星**同方向**（`TickFade` 那段的判据）：
        /// 淡入完 = 1、淡出完 = 0。没建出线 ⇒ `NaN`（⛔ 别拿 0 冒充「量到了」）。
        /// <para>🔴 这条探针是给「2026-10-25 之前写成 `1 − fade`（⇒ 稳定态恒 0 = 弧线整条看不见）」
        /// 那个缺陷配的断言用的：断开进后 ≈ 1、断淡出后 ≈ 0。</para></summary>
        public float LineColorAlpha
        {
            get
            {
                var mt = (_line != null) ? _line.sharedMaterial : null;
                return mt != null ? mt.GetColor("_Color").a : float.NaN;
            }
        }

        /// <summary>弧线形状 + 颜色。`kind` 只决定**拱高曲线**（原版 `CrosshairLineEffect__SetAttackType.c`：
        /// `attackType == 2` ⇒ range 曲线，其余（含 **0**）⇒ melee 曲线；⚠️ 原版对 `0` 还会
        /// `CustomDebug.LogWarning` 一句「这一档本不该走到」—— 我们**不打这条 warning**：`attackType 0`
        /// 在「还没选打法」那一整段时间里是**正常态**，逐帧刷会淹掉真告警）。
        /// 颜色取**当前 `TrailColor`**（`ApplyAttackType` 只在有 preset 时改它 ⇒ 0 档保持上一次/初始白）。</summary>
        void UpdateArc(Vector3 from, Vector3 to, AttackKind kind)
        {
            if (_line == null) return;
            _line.gameObject.SetActive(true);

            // 拱高随距离长 —— 远了才拱到满（`distanceToMaxCurveHeight`）
            float dist = Vector3.Distance(from, to);
            float peak = Mathf.Lerp(W(MinArcH), W(MaxArcH), Mathf.Clamp01(dist / W(DistToMaxArc)));

            var profile = (kind == AttackKind.Ranged) ? RangeProfile : MeleeProfile;

            _line.positionCount = ArcVerts;              // 原版 `curvePoints + 1` 个点
            for (int i = 0; i < ArcVerts; i++)
            {
                // 原版 `t = i ÷ curvePoints`（**不是 ÷(点数−1)**，见 `CrosshairLineEffect__SetPoints.c`）
                float t = i / (float)CurvePoints;
                var pt = Vector3.Lerp(from, to, t) + Vector3.up * (profile.Evaluate(t) * peak);
                _line.SetPosition(i, transform.TransformPoint(pt));   // `useWorldSpace = true` ⇒ 喂世界坐标
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
                new[] { new GradientColorKey(TrailColor, 0f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 377f / 65535f),      // 原版 atime1
                    new GradientAlphaKey(1f, 63522f / 65535f),    // 原版 atime2
                    new GradientAlphaKey(0f, 1f),
                });
            _line.colorGradient = g;
        }

        // ==================================================================
        //  🆕 2026-10-29（`B94_X2` · 账 `战斗逻辑驱动:381`）：**准星本体与弧线终点分开喂**的重载。
        //  判据 = 原版 `TargetReticleController` 是**两个不同的点**：
        //    · 准星（HUD 画布帧）跟**原始指针** —— `TargetReticleController__MoveCrosshair.c` 第一句
        //      `crosshair.transform.set_position(targetPositionUISpace)`（原始指针世界点）；
        //    · 弧线终点（棋盘 3D 帧）才走 `UpdateTrail` 的双平面求交（`+0x64 desiredWorldPosition`）。
        //  3 参 <see cref="Show"/> 把两者都当 `to`（求交点）—— 那是**旧的不精确口径**，保留只为既有调用点零改动；
        //  调用方应改用本重载，并把**原始指针世界点**传 `crossAt`（弧线终点仍传 `to` = 求交点）。
        // ==================================================================
        public void Show(Vector3 from, Vector3 to, AttackKind kind, Vector3 crossAt)
        {
            Build();

            from.z = Z; to.z = Z; crossAt.z = Z;

            if (_cross != null)
            {
                _cross.gameObject.SetActive(true);
                // 准星压在【原始指针】上（比弧线再靠前一点，别被线穿过去）
                _cross.transform.localPosition = crossAt + new Vector3(0f, 0f, -0.02f);
            }

            ApplyAttackType(kind);
            UpdateArc(from, to, kind);
            Visible = true;
            Cursor.visible = false;

            _fadeDir = +1;
            ApplyFade();
        }
    }
}
