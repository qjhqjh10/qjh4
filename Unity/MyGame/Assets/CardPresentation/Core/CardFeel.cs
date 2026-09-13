// CardFeel.cs — 「手感补间」六件事：发牌入场 / 攻击位移 / 命中抖动 / 阵亡消散 / 数值过渡 / 手牌重排
//
// **这一版的原则：参数从原版数据里抄，不凭手感调。**
// 每一条都在下面标了出处；**原版查不到的写「我们挑的」**，绝不冒称原版。
//
// 出处分四类（都写在常量上一行）：
//   ① 原版 AnimationClip —— `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/AnimationClip/`
//   ② 原版 UnitTweenSO —— `d:/4/Unity/数据/游戏数据/tween/*.json`（74 个）
//   ③ 卡预制体的序列化字段 —— `d:/2/解包整理/08_预制体特效/战斗预制体/MonoBehaviour/MonoBehaviour_1744609728290659264.json`
//      （**全库只有这一份**带这些字段，不存在「多份实例值打架」的问题 —— 已按文件名 + 字段名全盘查过）
//   ④ 反编译方法体里的常量 —— `decomp_out*/…`，`_DAT_` 用 `资料/战斗规则与数值_出处.md` §三 的办法读
//
// ⚠️ **3D → 2D 的迁移，哪些是我们的**（这一条最要紧，别把迁移当成原版）：
//   原版的单位是**立在 3D 战场上的模型**，位移/旋转都在 3D 世界空间（`punch = (0,0,-0.1)` 的 z 是
//   「朝镜头/朝目标」那个方向）。我们的场卡是**2D 平面上的卡**，没有 z。
//   所以：
//     · **时长、缓动、vibrato、elasticity、缩放、透明度** —— 原样照搬（这些与维度无关）
//     · **位移方向** —— 把原版的 z 换成**屏幕平面上的方向**（攻击方朝目标、受击方背离攻击者）
//     · **位移幅度** —— 按「原版棋盘那一层的像素密度 ÷ 我们这层的像素密度」折算（见 `ToOurs`）
//   这三条是**我们的迁移**，不是原版的数值。
using DG.Tweening;
using UnityEngine;

namespace CardPresentation
{
    public static class CardFeel
    {
        // ==================================================================
        //  换算：原版世界单位 → 我们的世界单位
        // ==================================================================

        /// <summary>原版**棋盘那一层**的像素密度。
        /// 出处：`资料/对战排版_原版数值与改造方案.md` —— 场卡 137.2 px = 2DCard 宽 2.0927
        /// × desiredScale 0.36 × k **182.14**。`BoardLayout.cs` 头部也记着这条。</summary>
        public const float OriginalBoardPxPerUnit = 182.14f;

        /// <summary>我们这套的像素密度：可见高 10 世界单位 = 1080 px（`LayoutSpace`）</summary>
        public const float OurPxPerUnit = 108f;

        /// <summary>原版 1 世界单位 = 我们 1.6865 世界单位（= 182.14 / 108）</summary>
        public const float UnitsToOurs = OriginalBoardPxPerUnit / OurPxPerUnit;

        /// <summary>把一个**原版世界单位的位移**换成我们的世界单位（见文件头「3D → 2D 的迁移」）</summary>
        public static float ToOurs(float originalUnits) { return originalUnits * UnitsToOurs; }

        // ==================================================================
        //  ④ 挨打后坐：`CardScript.DoPushBack`
        // ==================================================================

        /// <summary>`DoPushBack` 那一支的 punch 时长。
        /// 出处：`decomp_out/CardScript__DoPushBack.c` ——
        /// `transform.DOPunchPosition(dir × mult, _DAT_1834b3158, 8, _DAT_1834b2dc8)`，
        /// 常量从 `d:/2/unity_run_ref/GameAssembly.dll` 读出（读法见 `资料/战斗规则与数值_出处.md` §三）：
        /// `_DAT_1834b3158 = 0.4`。**8 是调用里硬编码的 vibrato**。</summary>
        public const float PushBackDuration = 0.4f;

        /// <summary>同上，`vibrato` 在调用里写死 8</summary>
        public const int PushBackVibrato = 8;

        /// <summary>同上，`_DAT_1834b2dc8 = 0.3`（elasticity）</summary>
        public const float PushBackElasticity = 0.3f;

        // ==================================================================
        //  ② 攻击的前冲/后坐：`Recoil * Tween`（UnitTweenSO）
        // ==================================================================

        /// <summary>近战通用那一条 `Recoil Normal Tween` 的 punch：
        /// `PunchTween { targetUnit=self, punchType=position, punch=(0,0,-0.1), duration=0.3,
        ///  ease=OutQuad(6), vibratto=10, elasticity=1.0, loops=1 }`
        /// ⚠️ z 轴 → 我们换成「朝目标」的平面方向（见文件头）。</summary>
        public const float AttackPunchUnits = 0.1f;
        public const float AttackPunchDuration = 0.3f;
        public const int AttackPunchVibrato = 10;
        public const float AttackPunchElasticity = 1.0f;

        // ==================================================================
        //  ② 命中反馈：`Impact Light Tween`（UnitTweenSO）
        // ==================================================================

        /// <summary>挨打那一下的**幅度**（`Impact Light Tween` 的位置 punch `(0,0,-0.1)`）。
        /// ⚠️ 原版挨打的位置弹跳**形状**来自 `DoPushBack`（见 `HitReact` 的注释），
        /// 而**幅度**原版是 `GetUnitSize × GetPushBackFactor` —— **那两个方法体没被反编译，查不到**。
        /// 这里借的是 `Impact *` 这套 UnitTweenSO 的 punch 值当顶替：轻击 0.1 / 重击 0.3。
        /// `Impact Heavy Tween` 是重击档：位置 −0.3、旋转 (−15,4,0)。</summary>
        public const float HitPunchUnitsLight = 0.1f;
        public const float HitPunchUnitsHeavy = 0.3f;

        public const float HitRotLightDeg = 3f;      // (-3,-3,0) 的模长
        public const float HitRotHeavyDeg = 15.5f;   // (-15,4,0) 的模长
        public const float HitRotDuration = 0.5f;
        public const int HitRotVibrato = 7;
        public const float HitRotElasticity = 1f;

        /// <summary>复位（`ResetBodyTween` 的 duration / ease）</summary>
        public const float ResetDuration = 0.25f;

        /// <summary>「这一下算重击」的门槛 —— **我们挑的**。
        /// 原版是按武器/单位挑不同的 UnitTweenSO（`Impact Heavy` / `Impact Target Medium` …），
        /// 那张「谁用哪条」的表在卡自己的逐卡特效字段里，而**本地一条都没有**
        /// （见 `资料/规则引擎_进度与交接.md` 第二十四轮）。所以这里用伤害值分档。</summary>
        public const int HeavyHitDamage = 4;

        // ==================================================================
        //  ③ 卡预制体上的时序字段
        // ==================================================================

        /// <summary>`timeToChargeAttack` —— 出手前的蓄力时长</summary>
        public const float ChargeTime = 0.35f;
        /// <summary>`chargeAttackAngle` —— 蓄力时向后仰的角度（度）</summary>
        public const float ChargeAngleDeg = -10f;
        /// <summary>`chargeBackModifier` —— 蓄力时的后撤系数（乘在攻击位移上）</summary>
        public const float ChargeBackModifier = 0.5f;
        /// <summary>`chargeUpModifier` —— 蓄力时的上抬系数</summary>
        public const float ChargeUpModifier = 0.35f;
        /// <summary>`attackStepTime` —— 攻击的「一拍」。抬手 = ×2 秒（`_DAT_1834b2bbc = 2.0`，与
        /// `EventTiming.AttackWindUp` 是同一个数）</summary>
        public const float AttackStepTime = 0.1f;
        /// <summary>`attackRotationAngle` —— 出手时卡体的倾角（度）</summary>
        public const float AttackRotDeg = 25f;

        // ==================================================================
        //  ① 手牌 → 战场：`Card Hand To Board`（0.9167 s / 60 fps，17 条曲线）
        // ==================================================================

        /// <summary>各阶段的时间点，全部来自那条 clip 的曲线关键帧（单位：秒）。
        /// 关键帧原文（`AnimationClip_-3614292623624764332.json`）：
        /// `Board Elements.m_IsActive` 0→1 @0.1667；`2DCard.m_Alpha` 1→0 @0.3333→0.6667；
        /// `CardImage` scale 1→0.905 @0.3333→0.55；`CardFrame.m_Enabled` 0→1 @0.0333→0→@0.75。
        /// **事件** `CardHandToBoardAnimationFinished` 在 **0.55** 触发 —— 原版认为这条动画「完了」
        /// 就是这一刻，不是 0.9167。</summary>
        public const float HandToBoardLand = 0.1667f;
        public const float HandToBoardFrameOn = 0.0333f;
        public const float HandToBoardFadeStart = 0.3333f;
        public const float HandToBoardFadeEnd = 0.6667f;
        public const float HandToBoardDone = 0.55f;
        public const float HandToBoardEnd = 0.9167f;
        /// <summary>溶解走完的那一刻（`Board Elements/3DBody/Card 3D` 的 `material._DissolveAmount`
        /// 从 1 到 0 的**终点**）。⚠️ 别和 `HandToBoardFadeEnd`（0.6667，那是**2D 卡透明度**那条曲线）
        /// 混 —— 两条曲线的终点不是同一个数，第一版就是拿错了这个端点，消散时长算成了 0.5。</summary>
        public const float HandToBoardDissolveEnd = 0.7f;
        /// <summary>落地时卡面缩到 90.5%（clip 的 ScaleCurves）</summary>
        public const float HandToBoardShrink = 0.905f;

        /// <summary>溶解窗（出战那条 clip 里 `_DissolveAmount` 1→0 的区间）。
        /// ⚠️ **消失侧没有对应的 clip**（查过 99 条 AnimationClip，只有 `EC Heldrake Dissapear UP`
        /// 两个单体专用的消散 tween）→ 阵亡我们用**同一时长**，这一条是**我们对称采用**的。</summary>
        public const float DissolveTime = HandToBoardDissolveEnd - HandToBoardLand;   // 0.5333

        // ==================================================================
        //  ① 数值过渡：`InBattleDamageCounter Variation 1`（1.8333 s）
        // ==================================================================

        /// <summary>飘字的时间轴，全部来自那条 clip：
        /// 父 scale 0.6641→1.0 @0→0.2；`DamageText.m_fontColor.a` 0→1 @0→0.1167、保持到 1.6667、
        /// 到 1.8333 归 0；`DamageIcon.m_Color.a` 0→0.8235 @0→0.1167（图标我们没画）。</summary>
        public const float PopIn = 0.1167f;
        public const float PopScaleIn = 0.2f;
        public const float PopHoldUntil = 1.6667f;
        public const float PopOut = 1.8333f;
        /// <summary>出现时从 66.41% 放大到 100%（父节点的 ScaleCurves）</summary>
        public const float PopScaleFrom = 0.6641f;

        // ==================================================================
        //  出处等级：**自检会把整张表打出来**
        //
        //  为什么要有它：用户 2026-09-13 追问「这些确定都是原版解包资料里说明的参数吧」——
        //  逐条回溯后的答案是「**40 条里 30 条是原版资料的字面值**，另有 7 条是**从原版值推导**的
        //  （换算单位/换维度），**3 条是我们挑的**」，而那些「不是」的地方散在二十多个常量的注释里，
        //  谁也不会逐条读。所以把它们**集中登记**，并让自检**用反射核对「有没有常量没登记」**。
        //
        //  ⚠️ **常量之外还有几处「我们挑的」**（它们不是常量，登记表盖不住，单独列在这儿）：
        //    · **位移方向**：原版 punch 在 3D 的 z 轴上（朝镜头/朝目标），2D 卡没有 z
        //      → `Flat()` 把方向压到屏幕平面（攻击方朝目标、受击方背离攻击者）
        //    · **阵亡的表现形式**：原版是材质 `_DissolveAmount` 溶解；我们没溶解 shader
        //      → 用「透明 + 上浮 0.3 单位 + 缩到 85%」（时长照原版，形式是我们的）
        //    · **飘字的字号 / 颜色 / 位置偏移**（`PopNumber` 里那几个字面量）
        //    · **发牌的起点**=我方牌堆中心（原版从牌库抽，锚点是我们按版面取的）
        //    · **手牌重排的 0.18s** —— 在 `CardTween.RelayoutDuration` 里，原版查不到（见那个文件）
        //
        //  三档：`Field` = 原版资料里的字面值 · `Derived` = 从原版值推导（换单位/换维度）·
        //        `Ours` = 我们挑的（原版查不到，或那是另一套机制）
        // ==================================================================

        public enum Src
        {
            /// <summary>原版资料里的**字面值**：解包资产的序列化字段 / clip 关键帧 / 反编译常量</summary>
            Field,
            /// <summary>从原版值**推导**的（换算单位、换维度）—— 原版字段里没有这个数</summary>
            Derived,
            /// <summary>**我们挑的**：原版查不到，或原版走的是另一套机制</summary>
            Ours,
        }

        public struct Param
        {
            public string Name;      // 常量名（反射核对用，必须和字段名逐字一致）
            public string Value;     // 实际用的值（写成串，免得为了打印再拆一次）
            public Src From;
            public string Source;    // 出处一句话
        }

        static Param P(string n, object v, Src s, string src)
        {
            return new Param { Name = n, Value = v == null ? "" : v.ToString(), From = s, Source = src };
        }

        /// <summary>**每一个手感常量都要在这里登记**（自检用反射核对，没登记的会红）</summary>
        public static readonly Param[] Catalog =
        {
            // ---- 挨打后坐：DoPushBack（反编译 + DLL 常量）----
            P("PushBackDuration", PushBackDuration, Src.Field, "`CardScript__DoPushBack.c` 的 DOPunchPosition 第 3 参 + DLL `_DAT_1834b3158`"),
            P("PushBackVibrato", PushBackVibrato, Src.Field, "同上，vibrato 在调用里硬编码 8"),
            P("PushBackElasticity", PushBackElasticity, Src.Field, "同上 + DLL `_DAT_1834b2dc8`"),

            // ---- 攻击：Recoil Normal Tween（UnitTweenSO）----
            P("AttackPunchUnits", AttackPunchUnits, Src.Field, "`Recoil Normal Tween` 的 `punch=(0,0,-0.1)`"),
            P("AttackPunchDuration", AttackPunchDuration, Src.Field, "`Recoil Normal Tween` 的 `duration=0.3`"),
            P("AttackPunchVibrato", AttackPunchVibrato, Src.Field, "`Recoil Normal Tween` 的 `vibratto=10`"),
            P("AttackPunchElasticity", AttackPunchElasticity, Src.Field, "`Recoil Normal Tween` 的 `elasticity=1.0`"),

            // ---- 命中：幅度借 Impact，旋转借 Impact ----
            P("HitPunchUnitsLight", HitPunchUnitsLight, Src.Ours, "`Impact *` 是**另一套机制**（AnimFX 的 UnitTweenSO）；原版的挨打幅度走 `GetUnitSize × GetPushBackFactor`，**那两个方法体没被反编译** → 借它的 0.1 顶替"),
            P("HitPunchUnitsHeavy", HitPunchUnitsHeavy, Src.Ours, "同上（`Impact Heavy Tween` 的 0.3）"),
            P("HitRotLightDeg", HitRotLightDeg, Src.Derived, "`Impact Light Tween` 的 3D 旋转 punch `(-3,-3,0)` → **取模长当 2D 的 z 度数**（原版是 x/y 两个轴）"),
            P("HitRotHeavyDeg", HitRotHeavyDeg, Src.Derived, "`Impact Heavy Tween` 的 `(-15,4,0)` 模长"),
            P("HitRotDuration", HitRotDuration, Src.Field, "`Impact Light Tween` 旋转 punch 的 `duration=0.5`"),
            P("HitRotVibrato", HitRotVibrato, Src.Field, "同上 `vibratto=7`"),
            P("HitRotElasticity", HitRotElasticity, Src.Field, "同上 `elasticity=1.0`"),
            P("ResetDuration", ResetDuration, Src.Field, "`Impact Light Tween` 的 `ResetBodyTween duration=0.25`"),
            P("HeavyHitDamage", HeavyHitDamage, Src.Ours, "**分档门槛是我们挑的** —— 原版按武器挑不同的 UnitTweenSO，那张表在**逐卡特效字段**里，本地一条都没有"),

            // ---- 蓄力 / 出手：卡预制体 ----
            P("ChargeTime", ChargeTime, Src.Field, "卡预制体 `timeToChargeAttack`"),
            P("ChargeAngleDeg", ChargeAngleDeg, Src.Field, "卡预制体 `chargeAttackAngle`"),
            P("ChargeBackModifier", ChargeBackModifier, Src.Field, "卡预制体 `chargeBackModifier`（**怎么用它**是我们的：当成「幅度 × 系数」）"),
            P("ChargeUpModifier", ChargeUpModifier, Src.Field, "卡预制体 `chargeUpModifier`（同上）"),
            P("AttackStepTime", AttackStepTime, Src.Field, "卡预制体 `attackStepTime`"),
            P("AttackRotDeg", AttackRotDeg, Src.Field, "卡预制体 `attackRotationAngle`（⚠️ **还没用上**，见文件末尾「没接上的」）"),

            // ---- 手牌 → 战场 / 溶解窗：Card Hand To Board clip ----
            P("HandToBoardLand", HandToBoardLand, Src.Field, "clip 的 `Board Elements` 曲线关键帧"),
            P("HandToBoardFrameOn", HandToBoardFrameOn, Src.Field, "clip 的 `CardFrame.m_Enabled` 关键帧"),
            P("HandToBoardFadeStart", HandToBoardFadeStart, Src.Field, "clip 的 `2DCard.m_Alpha` 关键帧"),
            P("HandToBoardFadeEnd", HandToBoardFadeEnd, Src.Field, "同上"),
            P("HandToBoardDone", HandToBoardDone, Src.Field, "clip 的 AnimationEvent `CardHandToBoardAnimationFinished` 时刻"),
            P("HandToBoardEnd", HandToBoardEnd, Src.Field, "clip 最后一条关键帧（0.9167）"),
            P("HandToBoardDissolveEnd", HandToBoardDissolveEnd, Src.Field, "clip 的 `material._DissolveAmount` 终点"),
            P("HandToBoardShrink", HandToBoardShrink, Src.Field, "clip 的 `CardImage` ScaleCurves 终点"),
            P("DissolveTime", DissolveTime, Src.Derived, "`HandToBoardDissolveEnd − HandToBoardLand`（**阵亡用同一时长是我们的对称假设** —— 消失侧没有 clip）"),

            // ---- 数值过渡：InBattleDamageCounter Variation 1 ----
            P("PopIn", PopIn, Src.Field, "clip 里 `DamageText.m_fontColor.a` 的关键帧"),
            P("PopScaleIn", PopScaleIn, Src.Field, "clip 里父节点 ScaleCurves 的关键帧"),
            P("PopHoldUntil", PopHoldUntil, Src.Field, "同上"),
            P("PopOut", PopOut, Src.Field, "同上"),
            P("PopScaleFrom", PopScaleFrom, Src.Field, "同上（0.6641）"),

            // ---- 换算 ----
            P("OriginalBoardPxPerUnit", OriginalBoardPxPerUnit, Src.Derived, "棋盘那层的像素密度：场卡 137.2 px = `2DCard 2.0927 × desiredScale 0.36 × k`，k 是**量出来的**不是某个字段"),
            P("OurPxPerUnit", OurPxPerUnit, Src.Derived, "本工程：可见高 10 世界单位 = 1080 px"),
            P("UnitsToOurs", UnitsToOurs, Src.Derived, "182.14 ÷ 108 —— **3D 世界单位 → 我们世界单位**的桥"),

            // ---- 发牌 / 重排 ----
            P("DealDuration", DealDuration, Src.Derived, "`CardScript.DrawCard` 用 DOScale+DORotate+DOMove，但**时长字段（`timeToDraw`）没被序列化出来** → 借 `Card Hand To Board` 的完成时刻 0.55"),
        };

        /// <summary>没登记进 `Catalog` 的公开常量（自检拿它当断言：**漏一个就红**）</summary>
        public static System.Collections.Generic.List<string> Unclassified()
        {
            var known = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < Catalog.Length; i++) known.Add(Catalog[i].Name);

            var miss = new System.Collections.Generic.List<string>();
            var t = typeof(CardFeel);
            var fs = t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            for (int i = 0; i < fs.Length; i++)
            {
                if (!fs[i].IsLiteral) continue;                       // 只查 `const`
                if (fs[i].FieldType != typeof(float) && fs[i].FieldType != typeof(int)) continue;
                if (!known.Contains(fs[i].Name)) miss.Add(fs[i].Name);
            }
            return miss;
        }

        /// <summary>把整张出处表打成几行（自检用 —— 也是「哪些不是原版的」的唯一权威清单）</summary>
        public static string ProvenanceReport()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Catalog.Length; i++)
            {
                var p = Catalog[i];
                string tag = p.From == Src.Field ? "原版字段" : (p.From == Src.Derived ? "原版推导" : "我们挑的");
                sb.Append("     ").Append(tag).Append("  ").Append(p.Name.PadRight(24))
                  .Append(p.Value.PadRight(10)).Append("← ").Append(p.Source).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>各档各有多少条（自检断言用）</summary>
        public static int CountOf(Src s)
        {
            int n = 0;
            for (int i = 0; i < Catalog.Length; i++) if (Catalog[i].From == s) n++;
            return n;
        }

        // ==================================================================
        //  动作
        // ==================================================================

        /// <summary>攻击前冲：朝 `dir` 方向弹一下。
        /// 参数 = `DoPushBack` 的形状（0.4 s / vib 8 / 弹性 0.3，有出处）
        /// + `Recoil Normal Tween` 的幅度与缓动（0.1 原版单位 / OutQuad，有出处）。</summary>
        public static Tween Lunge(Transform tr, Vector3 dir, float delay = 0f)
        {
            if (tr == null) return null;
            Vector3 d = Flat(dir);
            return CardTween.Use(
                tr.DOPunchPosition(d * ToOurs(AttackPunchUnits), AttackPunchDuration,
                                   AttackPunchVibrato, AttackPunchElasticity)
                  .SetDelay(delay), Ease.OutQuad);
        }

        /// <summary>挨打：位置弹一下 + 转一下。
        ///
        /// ⚠️ **这两截来自两套不同的原版机制，别混着说**：
        /// · **位置那一截 = 原版挨打真正走的路**：`BattleManager._ResolveAttack` 里
        ///   `CardScript.ReceiveAttackAnim(target, pushDir, …)` → `CardScript.DoPushBack`。
        ///   形状照抄 `CardScript__DoPushBack.c` 的 `DOPunchPosition(dir × mult, 0.4, 8, 0.3)`
        ///   （0.4 / 0.3 是从 `GameAssembly.dll` 读出的两个 `_DAT_`，8 在调用里硬编码）。
        ///   **幅度原版是 `SupportMethods.GetUnitSize × GetPushBackFactor` —— 那两个方法体都没被反编译，
        ///   查不到**（按单位体型分档的表不在数据里）。这里用 `Impact Light Tween` 的 punch 0.1 顶替，
        ///   而那是**另一套机制**（UnitTweenSO，走 AnimFX）—— **这一步是我们挑的**。
        /// · **旋转那一截来自 `Impact Light Tween`**：3D 的 `punch=(-3,-3,0)`。
        ///   我们是 2D 卡、只有 z 一个转轴 → **取它的模长 3 当度数**（这一步也是我们的换算）。
        /// 重击档：`Impact Heavy Tween` 的位置 −0.3 / 旋转 `(-15,4,0)`（模长 15.5）。
        /// </summary>
        public static void HitReact(Transform tr, Vector3 away, bool heavy = false, float delay = 0f)
        {
            if (tr == null) return;
            Vector3 d = Flat(away);
            float mag = heavy ? HitPunchUnitsHeavy : HitPunchUnitsLight;
            float rot = heavy ? HitRotHeavyDeg : HitRotLightDeg;

            // 位置：DoPushBack 的**形状**（0.4 / vib 8 / 弹性 0.3）+ 顶替来的幅度
            CardTween.Use(
                tr.DOPunchPosition(d * ToOurs(mag), PushBackDuration, PushBackVibrato, PushBackElasticity)
                  .SetDelay(delay), Ease.OutQuad);

            // 旋转：`Impact Light Tween` 的第二条 punch（0.5s / vib 7 / 弹性 1 / InQuad）
            // ⚠️ 原版那条打的是 `abilityTarget`，位置那条打的是 `self` —— 这里都作用在挨打这张卡上
            CardTween.Use(
                tr.DOPunchRotation(new Vector3(0f, 0f, Sign(d) * rot), HitRotDuration,
                                   HitRotVibrato, HitRotElasticity), Ease.InQuad);
        }

        /// <summary>出手前的蓄力：后仰 + 微退（卡预制体的 `chargeAttackAngle/-10°`、
        /// `chargeBackModifier 0.5`、`chargeUpModifier 0.35`、`timeToChargeAttack 0.35s`）。
        /// 蓄力结束后自己弹回去 —— 攻击那一下由 `Lunge` 接。</summary>
        public static Tween Charge(Transform tr, Vector3 forward, float delay = 0f)
        {
            if (tr == null) return null;
            Vector3 f = Flat(forward);
            Vector3 back = -f * (ToOurs(AttackPunchUnits) * ChargeBackModifier)
                         + Vector3.up * (ToOurs(AttackPunchUnits) * ChargeUpModifier);

            Vector3 home = tr.position;                 // 世界坐标 —— 卡是被 `SetPose` 用世界坐标摆的
            var seq = DOTween.Sequence();
            // 后撤/上抬那半段（`timeToChargeAttack` 的前 60%），再收回
            seq.Append(tr.DOMove(home + back, ChargeTime * 0.6f).SetEase(Ease.OutQuad));
            seq.Join(tr.DORotate(new Vector3(0f, 0f, ChargeAngleDeg), ChargeTime * 0.6f));
            seq.Append(tr.DOMove(home, ChargeTime * 0.4f).SetEase(Ease.InQuad));
            seq.Join(tr.DORotate(Vector3.zero, ChargeTime * 0.4f));
            return CardTween.Use(seq, Ease.Linear).SetDelay(delay);
        }

        /// <summary>阵亡消散。原版是**材质 `_DissolveAmount`** 从 1 溶到 0
        /// （见 `Card Hand To Board` clip 里 `Card 3D` 那条曲线）。
        /// ⚠️ **我们没有溶解 shader**，退而用「透明度 + 轻微上浮/缩小」——
        ///    **时长照原版（0.5333 s），表现形式是我们的**。</summary>
        public static Tween Dissolve(CardView card, float delay = 0f, System.Action onDone = null)
        {
            if (card == null) return null;
            var tr = card.transform;
            Vector3 up = new Vector3(0f, ToOurs(0.3f), 0f);     // 上浮量：我们挑的
            var seq = DOTween.Sequence();
            seq.Append(tr.DOMove(tr.position + up, DissolveTime).SetEase(Ease.InSine));
            seq.Join(tr.DOScale(tr.localScale * 0.85f, DissolveTime).SetEase(Ease.InQuad));
            seq.Join(DOTween.To(() => card.Alpha, a => card.SetAlpha(a), 0f, DissolveTime));
            var t = CardTween.Use(seq, Ease.Linear).SetDelay(delay);
            // ⚠️ 回调**从这里接**，别让调用方去碰 DOTween —— `BattleDriver` 没有 `using DG.Tweening`
            //    （补间只在这几个文件里出现，是这个工程一直的划法）
            if (onDone != null) t.OnComplete(() => onDone());
            return t;
        }

        /// <summary>
        /// 数值飘字（伤害/治疗）。时间轴照原版 `InBattleDamageCounter Variation 1`。
        /// ⚠️ 原版那条是**挂在卡上的 `CardDamageCounterController`**（字段 `damageCounterParent` /
        /// `damageText`(TMP) / `myAnimation`(Animation)，见类桩），这里按同一时间轴自己搭一个。
        /// </summary>
        public static Label PopNumber(Transform parent, Vector3 worldPos, int amount, bool heal)
        {
            string txt = (heal ? "+" : "-") + Mathf.Abs(amount);
            // 字号：**我们挑的**（原版是 TMP + 预制体上的字号，那份预制体没解出来）
            var lbl = Label.Create(parent, txt, worldPos, 16,
                                   heal ? new Color(0.45f, 0.95f, 0.5f) : new Color(1f, 0.35f, 0.3f),
                                   new Vector2(0.5f, 0.5f), "Pop_" + txt);
            lbl.SetCapHeight(0.30f);
            lbl.SetColor(new Color(lbl.color.r, lbl.color.g, lbl.color.b, 0f));

            var tr = lbl.transform;
            tr.localScale = Vector3.one * PopScaleFrom;

            var seq = DOTween.Sequence();
            seq.Append(DOTween.To(() => 0f, a => SetLabelAlpha(lbl, a), 1f, PopIn).SetEase(Ease.Linear));
            // ⚠️ 用 `Insert(0f, …)` 而不是 `Join` —— `Join` 会接在**当前游标**（0.1167）后面，
            //    这一条 0.2s 的缩放就会拖到 0.3167，把后面整条时间轴往后推 0.12s。
            //    时间轴要**严格照 clip**：0.2 缩完 → 停到 1.6667 → 1.8333 消失。
            seq.Insert(0f, tr.DOScale(Vector3.one, PopScaleIn).SetEase(Ease.OutQuad));
            seq.AppendInterval(PopHoldUntil - PopScaleIn);
            seq.Append(DOTween.To(() => 1f, a => SetLabelAlpha(lbl, a), 0f, PopOut - PopHoldUntil)
                          .SetEase(Ease.Linear));
            seq.OnComplete(() => KillLabel(lbl));
            LastPopTween = CardTween.Use(seq, Ease.Linear);
            return lbl;
        }

        /// <summary>
        /// ⚠️ **批处理下 `Object.Destroy` 不生效**（它要等下一帧，而批处理没有帧循环）——
        /// 和 `BattleDriver.Kill` 是同一条规矩。
        /// </summary>
        static void KillLabel(Label l)
        {
            if (l == null) return;
            if (Application.isPlaying) Object.Destroy(l.gameObject);
            else Object.DestroyImmediate(l.gameObject);
        }

        static void SetLabelAlpha(Label l, float a)
        {
            if (l == null) return;
            var c = l.color;
            l.SetColor(new Color(c.r, c.g, c.b, a));
        }

        /// <summary>最近一次飘字的补间。**自检要拿它核对总时长** —— DOTween 的 `Join` 接的是
        /// 「当前游标」而不是序列开头，写错就会把整条时间轴悄悄拖长（见 `PopNumber` 里的注释）。</summary>
        public static Tween LastPopTween;

        /// <summary>发牌入场：从 `from`（牌堆）飞到手里的位置。
        /// 出处：`CardScript.DrawCard` —— 它用 `DOScale + DORotate + DOMove` 三条补间把牌抽进来
        /// （`decomp_out/CardScript__DrawCard.c` 的调用序列）。
        /// ⚠️ **时长查不到**：那三条补间的 duration 来自 `CardScript` 的嵌套动画配置类
        ///   （`timeToDraw` / `stepTime`），那部分字段没被序列化进解包出来的 MonoBehaviour。
        ///   这里用 `0.55 s` —— **有出处的近似**：`Card Hand To Board` 的完成事件就在 0.55，
        ///   原版「一张牌动到位」用的就是这一档。**这一条算半出处，别再往上加精度。**</summary>
        public const float DealDuration = HandToBoardDone;

        public static Tween DealIn(CardView card, Vector3 from, float delay = 0f)
        {
            if (card == null) return null;
            var tr = card.transform;
            Vector3 to = tr.position;
            tr.position = from;
            // ⚠️ alpha 要**当场**置 0：DOTween 的 `To(getter, setter, …)` 第一次被推进时才调 setter，
            //    不先置 0 的话「刚抽到的那一帧」是全不透明的（自检里抓到了：alpha 1.00 < 1 失败）
            card.SetAlpha(0f);

            var seq = DOTween.Sequence();
            seq.Append(tr.DOMove(to, DealDuration).SetEase(Ease.OutCubic));
            seq.Join(tr.DORotate(Vector3.zero, DealDuration).SetEase(Ease.OutCubic));
            seq.Join(tr.DOScale(Vector3.one, DealDuration).SetEase(Ease.OutCubic));
            seq.Join(DOTween.To(() => 0f, a => card.SetAlpha(a), 1f, DealDuration * 0.5f));
            var t = CardTween.Use(seq, Ease.Linear).SetDelay(delay);
            // ⚠️ **被打断时要把 alpha 补回 1**：`HandLayout.Place` 会 `DOKill()` 掉这张卡身上的补间
            //    （重排/让位时），补间死在半路的话卡会**永远半透明**地留在手里。
            //    位置不用管 —— 打断它的那条补间自己会把位置摆对。
            t.OnKill(() => { if (card != null) card.SetAlpha(1f); });
            return t;
        }

        // ==================================================================
        //  小工具
        // ==================================================================

        /// <summary>把方向压到屏幕平面（丢掉 z）—— 原版的 z 轴在我们这套里没有对应物</summary>
        static Vector3 Flat(Vector3 v)
        {
            var f = new Vector3(v.x, v.y, 0f);
            return f.sqrMagnitude < 1e-8f ? Vector3.up : f.normalized;
        }

        /// <summary>方向在屏幕上的「左右」符号 —— 旋转 punch 用它决定往哪边歪</summary>
        static float Sign(Vector3 flatDir) { return flatDir.x >= 0f ? 1f : -1f; }

        // ==================================================================
        //  查到了、但还没接上的原版参数（**别以为用上了**）
        //
        //  都来自卡预制体 `MonoBehaviour_1744609728290659264.json`。列在这儿是因为
        //  「字段存在」和「我们用上了」是两件事 —— 不写出来的话下一个人会以为已经还原了。
        // ==================================================================

        // · `attackRotationAngle 25`（出手时卡体的倾角）—— 3D 里的旋转，2D 卡上还没决定怎么表达
        // · `attackPositionYOffset 0.5`（攻击位移的 Y 偏移）—— 同上，要跟 `playerAttackMargin 1.7`
        //   一起用才对得上原版的「冲到哪个位置」，单独拿一个数没意义
        // · `playerAttackMargin 1.7` / `enemyAttackMargin 1.0` —— 攻击时停在离目标多远的地方
        // · `timeToLand 0.2` / `timeBeforeLand 0.05` —— ✅ **这一条已经接上了**：见 `DeploySequence`
        //   （不过那边用的是 `MinionManager.minionToConversionPointTime = 0.3`，
        //     `timeToLand` 是「落地那一下」的时长，等有落地特效时再用）
        // · `cardMovementSpeed 10.8` —— 卡的移动速度。⚠️ **用途查不到**：调用点没被反编译，
        //   拿它算「手牌飞多久」（距离÷速度）会得到 0.12s 这种明显不对的数 —— **别当它是时间基准**
        // · `meleeHitCameraShakePreset`（amplitude 2.0）—— 挨打震镜头；要做得先决定
        //   2D 战场怎么表达 3D 的相机抖动（动相机？动整个背景 quad？）
    }
}
