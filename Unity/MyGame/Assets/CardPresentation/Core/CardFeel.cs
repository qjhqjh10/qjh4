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

        /// <summary>**挨打后坐的幅度**：原版按「**攻击方**的近战攻击」分三档。
        ///
        /// 出处（2026-09-18 查实，**取代**原来「那两个方法体没被反编译、查不到」的说法）：
        /// · `decomp_full/SupportMethods__GetUnitSize.c:11-16` ——
        ///   `m = e.CurrentMeleeAttack; return m &lt;= 3 ? 0 : (m &lt;= 7 ? 2 : 3);`
        ///   ⇒ **只有 0 / 2 / 3 三档，永不返回 1**；判据是**近战**攻击，不是远程、不是生命。
        /// · `decomp_full/SupportMethods__GetPushBackFactor.c:5-11` ——
        ///   `size == 2 ? 1.0f : (size == 3 ? 1.5f : 0.5f)`；
        ///   常量在 `GameAssembly.dll` 的 .rdata 里硬编码（0x1834b2bb8=1.0 · 0x1834b3090=1.5 · 0x1834b2bb4=0.5），
        ///   **不在 `VarsGlobal` 那 48 项里**（全表逐项读过）。
        /// · 调用形状 `decomp_full/CardScript__ReceiveAttackAnim.c:27-28,38-89`：
        ///   `punch = normalize(受击方.pos − 攻击方.pos) × GetPushBackFactor(GetUnitSize(攻击方))`
        ///   （`|dir| ≤ 1e-5` 时不后坐），再 `DOPunchPosition(punch, 0.4, 8, 0.3)`。
        ///   第二处 `BattleManager._ResolveUnitShake_d__472__MoveNext.c:139-147` 同形互证。
        /// ⇒ **幅度只由「攻击方」决定，受击方不参与**；
        ///   `ToOurs` 之后 = **0.843 / 1.687 / 2.530**（我们的世界单位）。</summary>
        public static float PushBackMagnitude(int attackerMeleeAttack)
        {
            int size = attackerMeleeAttack <= 3 ? 0 : (attackerMeleeAttack <= 7 ? 2 : 3);
            return size == 2 ? 1.0f : (size == 3 ? 1.5f : 0.5f);
        }

        /// <summary>`SupportMethods.GetUnitSize` 本身（只有 0/2/3 三档）。
        /// 出处见 <see cref="PushBackMagnitude"/>。留着是因为**同一档还驱动相机震屏 2/4/8**
        /// （`CardFeel.ShakeAmpUnitSize*`）与单位抖动 `DoUnitShake(2/4/8)` —— 判据要共用一份。</summary>
        public static int UnitSizeOf(int meleeAttack)
        {
            return meleeAttack <= 3 ? 0 : (meleeAttack <= 7 ? 2 : 3);
        }

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

        /// <summary>⚠️ **现在只是兜底值**：只在**不知道攻击方是谁**时才用（疲劳、反伤那类伤害）。
        /// 常规路径走 <see cref="PushBackMagnitude"/> —— 原版幅度 = `GetPushBackFactor(GetUnitSize(攻击方))`。
        ///
        /// 这个值本身取自 `Impact *` 那套 UnitTweenSO 的 punch（轻击 0.1 / 重击 0.3），
        /// 而那是**另一套机制**；`Impact Heavy Tween` 是重击档：位置 −0.3、旋转 (−15,4,0)。
        /// 🔴 **2026-09-18 更正**：这里原写「`GetUnitSize × GetPushBackFactor` ——
        /// **那两个方法体没被反编译，查不到**」—— 那句**是错的**（旧 `decomp_il2cpp_0827` 的说法）；
        /// 全量反编译里两个方法体都在，值即 <see cref="PushBackMagnitude"/> 的出处。</summary>
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

        /// <summary>🆕 2026-09-29 **督军落场时，徽标淡回实心的时长**。
        ///
        /// 出处：`CardScript.&lt;HeroLandIntoField>d__318.MoveNext:86` ——
        /// `BattleCardUI.FadeAllTraitsIcons(fVar17, DAT_1834b2dc8)`，两个实参都是从
        /// `d:/2/unity_run_ref/GameAssembly.dll` 浮点池**实读**的：
        /// 目标 α `DAT_1834b2bb8` = **1.0f**、时长 `DAT_1834b2dc8` = **0.3f**。
        /// 🔴 这也是 `FadeAllTraitsIcons` 在**全量反编译里唯一的调用点**（逐文件 grep 过）。
        /// 落地：`BattleDriver.SyncUnits`（新视图建出来那一刻）+ `CardView.FadeBadges`。</summary>
        public const float HeroLandBadgeFadeTime = 0.3f;
        /// <summary>同上的目标 α（原版 `DAT_1834b2bb8`）</summary>
        public const float HeroLandBadgeAlpha = 1.0f;

        // ==================================================================
        //  ① 挨打震镜头：卡预制体 `meleeHitCameraShakePreset` → preset `Shake Hit Small`
        //
        //  这条链路原版是这么走的（反编译 `GameAssembly.dll` + 解包资产逐段查出）：
        //    卡预制体 `meleeHitCameraShakePreset`（指向 presetSO `Shake Hit Small`）
        //    → `CardScript.ResolveAttackAnimationEffects`（按 `GetUnitSize` 换 amplitude）
        //    → `OverwriteCameraShakePreset.Play` → `GetModifiedPreset` → `CameraShakePreset.Play`
        //    → `CameraShakerManager.DoShake` 协程 → `CinemachineImpulseSource.GenerateImpulseWithVelocity`
        //    → 挂在同一 GameObject 的 `CinemachineImpulseListener` 把偏移加到**主相机**上。
        //  🔴 **关键：震的是主相机，不是逐单位/逐卡片的位移。** 场景里那三件套
        //    （ImpulseSource / ImpulseListener / CameraShakerManager）全挂在 "Cinemachine Vcam" 上、驱动
        //    `BoardCamera`；另有独立 `UI Camera`，**没挂**这两件，而且场景 `useCanvasShake: 0`
        //    ⇒ **原版 UI 是不震的**。
        //  出处：preset 资产落盘在
        //        `d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/Shake Hit Small_4555857231027011253.json`；
        //        执行链是本次反汇编 `d:/2/unity_run_ref/GameAssembly.dll` 读出来的
        //        （那几个类的方法体不在 `decomp_out*` 里，只有签名桩）。
        // ==================================================================

        /// <summary>`CameraShakePreset.delay`（`Shake Hit Small` = 0）</summary>
        public const float ShakeDelay = 0f;
        /// <summary>`CameraShakePreset.attackTime`（= 0）—— 0 不是「没有起振」，是**一帧到满**</summary>
        public const float ShakeAttackTime = 0f;
        /// <summary>`CameraShakePreset.sustainTime`（= 0.1）—— 满幅保持</summary>
        public const float ShakeSustainTime = 0.1f;
        /// <summary>`CameraShakePreset.decayTime`（= 0.3）—— 衰减时长</summary>
        public const float ShakeDecayTime = 0.3f;
        /// <summary>衰减的**终点**（不是 0）。出处：场景 `CinemachineImpulseSource`
        /// （battlearena1 的 `MonoBehaviour_4914.json`）的 `m_DecayShape` = (0,1)→(1,**0.01**)，
        /// 两个关键帧 `weightedMode:0`（切线被忽略）⇒ **线性插值**，不是 EaseInOut。</summary>
        public const float ShakeDecayTo = 0.01f;

        /// <summary>原版按 `SupportMethods.GetUnitSize(card)` 分三档覆盖 preset 的 amplitude：
        /// 尺寸 0/1 → 2.0、尺寸 2 → 4.0、尺寸 3 → 8.0。
        /// 出处：`CardScript__ResolveAttackAnimationEffects.c:32-52` 把三个常量
        /// （`_DAT_1834b2bbc` / `_DAT_1834b2df0` / `_DAT_1834b2e00`）传给
        /// `OverwriteCameraShakePreset.SetAmplitude`；三个常量的值**从 `GameAssembly.dll` 读出**
        /// = 2.0 / 4.0 / 8.0（读法见 `资料/战斗规则与数值_出处.md` §三）。
        /// ⚠️ **卡上写的 `amplitude 2.0` 只是壳** —— 运行时会被这三个数覆盖。
        /// ⚠️ **我们只用到第 1 档**：我们的单位不占多格，没有「尺寸」这个概念
        /// （尺寸 2/3 那两档**查到了但接不上**，别以为用上了）。</summary>
        public const float ShakeAmpUnitSize1 = 2.0f;
        public const float ShakeAmpUnitSize2 = 4.0f;
        public const float ShakeAmpUnitSize3 = 8.0f;

        /// <summary>`CameraShakePreset.frequency`（= 0.05）。原版把它原封不动写进 Cinemachine 的
        /// `m_FrequencyGain`（ImpulseSource `MonoBehaviour_4914.json`）—— 那是**信号时间轴的缩放**
        /// （1 = 原速，0.05 = 放慢 20 倍）。我们**用它**：见下面 `ShakeBandHz` 的注释。</summary>
        public const float ShakeFrequencyGain = 0.05f;

        /// <summary>🔴 **2026-09-17 更正：波形资产找到了，不是「没解出来」。**
        /// 它叫 **`Warpforge 6D Shake`**，类是 **`Cinemachine.NoiseSettings`**（SignalSourceAsset 的子类）
        /// —— 之前按 `CinemachineFixedSignal` / `m_XCurve` / `m_Samples` 这些**猜的名字**去搜，
        /// 必然 0 命中（原版用的是**程序化噪声**，没有采样点数组）。
        /// 落盘：`d:/2/新解包资源/assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour/Warpforge 6D Shake.json`
        /// （旧解包同名件 `d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/Warpforge 6D Shake_1276427422462347950.json`，内容一致）。
        ///
        /// 噪声是 **3 条带 × 6 轴**（`PositionNoise` / `OrientationNoise` 各 X/Y/Z）。
        /// **屏幕上的竖直方向对应 PositionNoise 的 Y**（相机沿 +z 看 ⇒ 世界 z 是「朝/背镜头」、
        /// 屏幕上不动）；三条带照抄如下（`Amplitude` 就是信号里那一条的振幅）：</summary>
        static readonly float[] ShakeBandHz  = { 1.90f, 9.10f, 55.54f };     // PositionNoise[*].Y.Frequency
        static readonly float[] ShakeBandAmp = { 0.059f, 0.040f, 0.050f };   // PositionNoise[*].Y.Amplitude
        /// <summary>三条带全同相时的合振幅（= Σ `ShakeBandAmp` = 0.149）—— 归一化用，
        /// 这样 `ShakeWorldAmplitude` 表示的是**峰值**位移。</summary>
        public const float ShakeBandPeak = 0.149f;

        /// <summary>🔴 震动的**绝对幅度**（世界单位，峰值 = 第 1 档 / 尺寸 1）。
        ///
        /// **怎么推出来的**（每一环都有出处，但**中间有两处软连接**，见下）：
        ///   ① 峰值信号 = Σ 三条带振幅 = **0.149**（同上，三条带在 t=0 同相）
        ///   ② 原版 `m_AmplitudeGain` = `preset.amplitude × |direction|`
        ///      = 2.0（`GetUnitSize` 第 1 档覆盖值）× |(0, 0.2, 0.2)|(0.28284) = **0.5657**
        ///   ③ 传给 `GenerateImpulseWithVelocity` 的 velocity = `normalize(direction)` = (0, 0.7071, 0.7071)
        ///      ⇒ **屏幕竖直分量 = 0.7071**
        ///   ④ 原版相机位移（世界单位）= 1.0 × 0.5657 × 0.149 × 0.7071 = **0.0596**
        ///   ⑤ 换成屏幕像素：原版 BoardCamera 在**棋盘平面**（z≈3.66；相机 z=−13.57 ⇒ 距离 17.23）
        ///      的可见高 = 2 × 17.23 × tan(FOV 46.397/2) = **14.77 世界单位** = 1080 px
        ///      ⇒ **73.1 px / 世界单位**；0.0596 × 73.1 = **4.36 px** @1080p
        ///   ⑥ 换成本工程的世界单位（10 世界单位 = 屏高 = 1080 px ⇒ **108 px/单位**）
        ///      ⇒ 4.36 ÷ 108 = **0.0404** ⇒ 取 **0.04**
        ///
        /// ⚠️ **两处软连接（照这个数用之前先知道）**：
        ///   ① ②→④ 那一步按的是 **Cinemachine「Legacy impulse：位移 = velocity × AmplitudeGain × 信号」**
        ///      这条公式，**没有从原版二进制里验**（要验得跑原版实测）；
        ///   ② 「世界 y 分量 = 屏幕竖直、z 分量在屏幕上不动」是**我们的换算**（见文件头「3D → 2D 的迁移」）。
        /// ⚠️ 至于「向上还是向下」，静态数据**判不出来**（要跑原版实测），这里取**相机向上抬**。
        /// ⚠️ 另有一条**没有复刻**：原版 `m_Randomize: 1` ⇒ **每次抖的相位是随机的**
        ///   （所以「原版那一条波形」本来就不存在，复刻相位没有意义）。我们固定用 cos（t=0 在峰值）。</summary>
        public const float ShakeWorldAmplitude = 0.04f;

        // ==================================================================
        //  ③ 卡预制体上的时序字段
        // ==================================================================

        /// <summary>`timeToChargeAttack` —— 出手前的蓄力时长</summary>
        public const float ChargeTime = 0.35f;        /// <summary>`chargeAttackAngle` —— 蓄力时向后仰的角度（度）</summary>
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

        /// <summary>🆕 **AI 打出去时那一段「准星自己滑过去」的演出时长**（秒）= `VarsGlobal.targettingAnimTime`。
        ///
        /// 🔴 **字段偏移是 `0x88`，不是 `0x2C8`**（2026-09-29 查实并订正文档）——
        /// `dump.cs:120106` 写着 `public float targettingAnimTime; // 0x88`，而 `VarsGlobal` 最后一个字段在 `0xD8`。
        /// 实读 = **0.5**（工具 `d:/4/Unity/工具/read_varsglobal.py`；⚠️ C# 代码里的默认值是 `1.0f`，
        /// 资产把它覆盖成 0.5）。消费点 = `BattleManager.GetTargettingTime`（三个 AI 协程调）
        /// + `BattleManager__EnemyTargetingAnim.c:41/46`（内联的同一份代码，没走 getter）。
        ///
        /// 🔴 **只有 AI 那一侧有这段演出**：`EnemyTargetingAnim` 的三个调用点守卫都写着
        /// `施法方 isPlayer == false`（`…_ResolvePlayActiveAbility_d__479:524` ·
        /// `…_ResolvePlayCardFromHand_d__447:482` · `…_ResolveAttack_d__438:823,1476`）。
        /// 玩家那条路是 `MoveCrosshair` **每帧直接给位置**、没有补间（见 `TargetReticle` 文件头）。
        ///
        /// 时序（原版三条链都是同一个形状）：**演出（准星滑 0.5 s）** → `WaitForSeconds(0.5)` → 结算。
        /// 补间与等待**并行** ⇒ 准星滑到位与结算同一刻，中间没有额外间隔。</summary>
        public const float EnemyTargetingAnimTime = 0.5f;

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
        /// 两个单体专用的消散 tween）。
        /// 🔴 **2026-09-18：阵亡**不再用这个值** —— 原版有现成字段、而且**分档**：
        /// 小兵 `deathTimeMinionDuration = 0.2` · 督军 `deathTimeWarlordDuration = 0.5`
        /// （另有两个同值字段 `minionDeathTime = 0.2` / `timeToDissolveCard = 0.2`；
        /// 出处 `资料/VarsGlobal_原版数值.md`）⇒ 见 <see cref="DeathDissolve"/>。
        /// 这个常量只留给**未来要做出战侧溶解**时用（那是 clip 里真实存在的窗口）。</summary>
        public const float DissolveTime = HandToBoardDissolveEnd - HandToBoardLand;   // 0.5333

        /// <summary>**小兵阵亡**时长（秒）。原版 `deathTimeMinionDuration = 0.2`</summary>
        public const float DeathDissolveMinion = 0.2f;
        /// <summary>**督军阵亡**时长（秒）。原版 `deathTimeWarlordDuration = 0.5`
        /// （督军死 = 本局结束，那一下要比小兵慢一倍多）</summary>
        public const float DeathDissolveWarlord = 0.5f;

        /// <summary>阵亡消散时长 —— **判据只此一处**：督军 0.5 / 小兵 0.2（原版两个字段，见上）。</summary>
        public static float DeathDissolve(bool isWarlord)
        {
            return isWarlord ? DeathDissolveWarlord : DeathDissolveMinion;
        }

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
        //    · ~~**手牌重排的 0.18s**（原版查不到）~~ —— 🔴 **2026-09-17 已成正解**：
        //      原版 `VarsGlobal.timeToPositionCard = 0.2`，已换进 `CardTween.RelayoutDuration`。
        //      ⚠️ 那个常量**不在本文件的 `Catalog` 里**（它在 `CardTween` / `CardInteraction`）⇒
        //      `Unclassified()` 的反射核对**盖不到它** —— 改那几个常量时得**手工同步**它们自己的注释，
        //      别指望这张表会红。**这是本表已知的盲区**（同类：`PickUpDuration` / `dragScale` / `TapThreshold`
        //      也都在 `CardTween` / `CardInteraction` 里，不在本表）。
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

            // ---- 挨打震镜头：卡预制体 `meleeHitCameraShakePreset` → preset `Shake Hit Small` ----
            P("ShakeDelay", ShakeDelay, Src.Field, "`Shake Hit Small` 的 `delay=0`"),
            P("ShakeAttackTime", ShakeAttackTime, Src.Field, "`Shake Hit Small` 的 `attackTime=0`（**一帧到满**，不是「没有起振」）"),
            P("ShakeSustainTime", ShakeSustainTime, Src.Field, "`Shake Hit Small` 的 `sustainTime=0.1`"),
            P("ShakeDecayTime", ShakeDecayTime, Src.Field, "`Shake Hit Small` 的 `decayTime=0.3`"),
            P("ShakeDecayTo", ShakeDecayTo, Src.Field, "场景 `CinemachineImpulseSource`(battlearena1 MB_4914) 的 `m_DecayShape` 终点 **0.01**（两点、切线被忽略 ⇒ 线性）"),
            P("ShakeAmpUnitSize1", ShakeAmpUnitSize1, Src.Field, "`ResolveAttackAnimationEffects.c:38` 传给 `SetAmplitude` 的 `_DAT_1834b2bbc`，从 `GameAssembly.dll` 读出 = **2.0**（尺寸 0/1）"),
            P("ShakeAmpUnitSize2", ShakeAmpUnitSize2, Src.Field, "同上 `_DAT_1834b2df0` = **4.0**（尺寸 2）—— ⚠️ **我们接不上**（我们的单位不占多格，没有「尺寸」这个概念）"),
            P("ShakeAmpUnitSize3", ShakeAmpUnitSize3, Src.Field, "同上 `_DAT_1834b2e00` = **8.0**（尺寸 3）—— ⚠️ **同上，接不上**"),
            P("ShakeFrequencyGain", ShakeFrequencyGain, Src.Field, "`Shake Hit Small` 的 `frequency=0.05` → Cinemachine `m_FrequencyGain`（信号时间轴缩放，0.05 = 放慢 20 倍），乘在 `ShakeBandHz` 上"),
            P("ShakeBandPeak", ShakeBandPeak, Src.Field, "`Warpforge 6D Shake`(Cinemachine `NoiseSettings`) 的 `PositionNoise[*].Y.Amplitude` 三条带之和 = 0.059+0.040+0.050；拿去归一化，让 `ShakeWorldAmplitude` 表示**峰值**"),
            P("ShakeWorldAmplitude", ShakeWorldAmplitude, Src.Derived, "**由原版值推导**：0.149(带峰) × 0.5657(`amplitude × |direction|`) × 0.7071(velocity 的竖直分量) = 0.0596 原版世界单位 → 按原版 BoardCamera 在棋盘平面的 73.1 px/单位 → 4.36 px @1080p → ÷108(我们的 px/单位) = 0.0404。⚠️ **两处软连接**（Cinemachine impulse 公式未从二进制验；y→屏幕竖直是我们的换算）见常量注释"),

            // ---- 蓄力 / 出手：卡预制体 ----
            P("ChargeTime", ChargeTime, Src.Field, "卡预制体 `timeToChargeAttack`"),
            P("ChargeAngleDeg", ChargeAngleDeg, Src.Field, "卡预制体 `chargeAttackAngle`"),
            P("ChargeBackModifier", ChargeBackModifier, Src.Field, "卡预制体 `chargeBackModifier`（**怎么用它**是我们的：当成「幅度 × 系数」）"),
            P("ChargeUpModifier", ChargeUpModifier, Src.Field, "卡预制体 `chargeUpModifier`（同上）"),
            P("AttackStepTime", AttackStepTime, Src.Field, "卡预制体 `attackStepTime`"),
            P("AttackRotDeg", AttackRotDeg, Src.Field, "卡预制体 `attackRotationAngle`（✅ 2026-09-29 **已接上** —— 段2 的转向）"),
            P("EnemyTargetingAnimTime", EnemyTargetingAnimTime, Src.Field,
              "`VarsGlobal.targettingAnimTime`（偏移 **0x88**，实读 0.5）—— **AI 那侧**的准星滑动演出时长"
              + "（玩家那侧没有补间）；判据见常量自身的注释"),
            P("ReturnLiftTime", ReturnLiftTime, Src.Field,
              "回手那一下抬升的位移时长 = `DAT_1834b317c` 实读 0.208"
              + "（`CardScript._TransformUnitInPlayToCard_d__256:53-73`）"),
            P("ReturnLiftScale", ReturnLiftScale, Src.Field,
              "抬升高度 = `localScale.x × 3.0`（`DAT_1834b2e8c` 实读 3.0；方向 `Vector3.up`）"),
            P("DeathShakeX", DeathShakeX, Src.Field,
              "阵亡抖动强度 X = 0.3（`DAT_1834b2dc8` 实读；`CardScript._UnitDeath…:100-116`）"),
            P("DeathShakeY", DeathShakeY, Src.Field, "阵亡抖动强度 Y = 0.05（`DAT_1834b2f94` 实读）"),
            P("DeathShakeVibrato", DeathShakeVibrato, Src.Field, "阵亡抖动的 vibrato = 10（调用里硬编码）"),
            P("DeathShakeRandomness", DeathShakeRandomness, Src.Field, "阵亡抖动的 randomness = 90（`DAT_1834b2e04` 实读）"),

            // ---- 🆕 2026-09-29 近战三段式（原版 `CardScript._AttackMeleeAnim_d__357`）----
            P("PlayerAttackMargin", PlayerAttackMargin, Src.Field, "卡预制体 `playerAttackMargin` —— 段1 停在离目标多远（沿归一化方向）"),
            P("EnemyAttackMargin", EnemyAttackMargin, Src.Field, "卡预制体 `enemyAttackMargin`（同上，攻方是敌方时）"),
            P("AttackPositionYOffset", AttackPositionYOffset, Src.Field, "卡预制体 `attackPositionYOffset` —— 乘在 `(受击方 effectsAnchor.y − 受击方 y)` 上；⚠️ 我们的等价量恒 0"),
            P("AttackTurnDeg", AttackTurnDeg, Src.Field, "段2 先绕 `Vector3.up` 转的角度，`.rdata 0x1834b2e04` 从 DLL 读出 = 90°"),
            P("AttackHoldTime", AttackHoldTime, Src.Field, "段3「停在目标身上」的时长，`.rdata 0x1834b2dc4` = 0.1"),
            P("AttackReturnTime", AttackReturnTime, Src.Derived, "段4/段5 归位时长 = `2 × attackStepTime`（原版 `:260` 就是 `attackStepTime * 2`）"),

            // ---- 🆕 2026-09-29 徽标淡入 / 数值涨落 ----
            P("HeroLandBadgeFadeTime", HeroLandBadgeFadeTime, Src.Field, "`CardScript.<HeroLandIntoField>:86` 传给 `FadeAllTraitsIcons` 的时长，`.rdata 0x1834b2dc8` 实读 = 0.3"),
            P("HeroLandBadgeAlpha", HeroLandBadgeAlpha, Src.Field, "同一个调用点的目标 α，`.rdata 0x1834b2bb8` 实读 = 1.0"),
            P("StatBumpSize", StatBumpSize, Src.Field, "卡预制体 `cardTextBumpSize` = 0.75（**乘**在基准 scale 上当 punch 幅度）"),
            P("StatBumpTime", StatBumpTime, Src.Field, "卡预制体 `cardTextBumpTime` = 0.45（⚠️ `.ctor` 默认才是 0.4）"),
            P("StatBumpVibrato", StatBumpVibrato, Src.Field, "9 参 `SetText` 里 `mov r9d,0xa` —— `DOPunchScale` 的 vibrato = 10"),
            P("StatBumpElasticity", StatBumpElasticity, Src.Field, "9 参 `SetText` 栈上传的弹性常量，`.rdata 0x1834B2BBC` 实读 = 2.0"),

            // ---- 手牌 → 战场 / 溶解窗：Card Hand To Board clip ----
            P("HandToBoardLand", HandToBoardLand, Src.Field, "clip 的 `Board Elements` 曲线关键帧"),
            P("HandToBoardFrameOn", HandToBoardFrameOn, Src.Field, "clip 的 `CardFrame.m_Enabled` 关键帧"),
            P("HandToBoardFadeStart", HandToBoardFadeStart, Src.Field, "clip 的 `2DCard.m_Alpha` 关键帧"),
            P("HandToBoardFadeEnd", HandToBoardFadeEnd, Src.Field, "同上"),
            P("HandToBoardDone", HandToBoardDone, Src.Field, "clip 的 AnimationEvent `CardHandToBoardAnimationFinished` 时刻"),
            P("HandToBoardEnd", HandToBoardEnd, Src.Field, "clip 最后一条关键帧（0.9167）"),
            P("HandToBoardDissolveEnd", HandToBoardDissolveEnd, Src.Field, "clip 的 `material._DissolveAmount` 终点"),
            P("HandToBoardShrink", HandToBoardShrink, Src.Field, "clip 的 `CardImage` ScaleCurves 终点"),
            P("DissolveTime", DissolveTime, Src.Derived, "`HandToBoardDissolveEnd − HandToBoardLand`（⚠️ **只留给「将来要做出战侧溶解」** —— 阵亡侧 2026-09-18 起改用下面那两个原版字段）"),
            P("DeathDissolveMinion", DeathDissolveMinion, Src.Field, "`VarsGlobal.deathTimeMinionDuration` = 0.2（另有两个同值字段 `minionDeathTime` / `timeToDissolveCard`）"),
            P("DeathDissolveWarlord", DeathDissolveWarlord, Src.Field, "`VarsGlobal.deathTimeWarlordDuration` = 0.5（督军那档；出处 `资料/VarsGlobal_原版数值.md`）"),

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
            P("DealDuration", DealDuration, Src.Field, "`VarsGlobal.timeToDrawPlayerCard = 0.3`（抽**我方**牌用时）。出处 `资料/VarsGlobal_原版数值.md` §一 —— 🔴 2026-09-17 由 `Derived`（借 0.55）升级为 `Field`"),
            P("DealDurationFoe", DealDurationFoe, Src.Field, "`VarsGlobal.timeToDrawEnemyCard = 0.15`（抽**敌方**牌用时）—— 2026-09-17 加到「敌方手牌」那条线上，之前那一整件没建（见 `资料/敌方手牌_原版规格.md`）"),
            P("ReassembleTime", ReassembleTime, Src.Derived, "**让位 / 补位**的位移时长 = `MinionManager__ReassembleMinions.c:47` 那句 `globalVars+0xa0 ÷ 1.5`：`minionReassembleTime(0.15) ÷ DAT_1834b3090(1.5)` = **0.1 s**。⚠️ 缓动是**我们推的**（原版那句 `DOLocalMove` 没 `SetEase` ⇒ 走 DOTween 默认）。正本 → `资料/棋盘连续模型_实现与判据.md` §3.4"),
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

        // ── 🔴 近战攻击的**三段式**（原版 `CardScript._AttackMeleeAnim_d__357`）──────────────
        //
        //  2026-09-29 照原版重写（从前是 `Charge() 0.35s` 之后才 `Lunge()` ⇒ 观感是**先挨打、后前冲**）。
        //  原版时序（逐句从 `CardScript._AttackMeleeAnim_d__357__MoveNext.c` 读出来的）：
        //    段1  0.1s  `DOMove(endPos)` **Linear**      ← **命中就在这一段完成的那一帧**
        //    段2  0.1s  `DORotateQuaternion`（绕 up 转 90° 再叠 `attackRotationAngle 25°`）· **Join**
        //    段3  0.1s  `AppendInterval`（停在目标身上）
        //    段4  0.2s  `DOLocalMove(originalLocalPosInPlay)` **OutQuart**
        //    段5  0.2s  转向归位（回单位四元数）· **Join**
        //  ⇒ **整条 0.4 s、命中在 t = 0.1 s**（收招那 0.3 s 由**挨打动画**覆盖，见 `EventTiming.MeleeImpactLag`）。
        //  ⚠️ 段1 用 `DOMove`（世界）而段4 用 `DOLocalMove`（局部）—— **别混**，原版就是这么写的。

        /// <summary>原版卡预制体 `playerAttackMargin` —— 出手时停在**离目标多远**（世界单位，沿归一化方向）</summary>
        public const float PlayerAttackMargin = 1.7f;
        /// <summary>原版卡预制体 `enemyAttackMargin`</summary>
        public const float EnemyAttackMargin = 1.0f;
        /// <summary>🆕 2026-09-29 **卡面数值涨/落时的颜色**（原版 `CardTextCountersController.DoColorChange`
        /// 从 `textColorsSO` 取的那两个色；`statIncreaseIsPositiveToThePlayer = 1` ⇒
        /// `new > old` 取 `+0x18` 那一组、否则取 `+0x28` 那一组）。
        /// 数值出处：`CardTextCountersColorsSO` 资产 + `CardTextCountersController__DoColorChange.c:45-56`
        /// （见 `资料/待办判据_战场与战斗视图.md` §26 第 3 条那张表）。</summary>
        public static readonly Color StatUpColor = new Color(0f, 1f, 0f, 1f);
        /// <summary>见 <see cref="StatUpColor"/></summary>
        public static readonly Color StatDownColor = new Color(0.9725f, 0.306f, 0.353f, 1f);

        /// <summary>原版卡预制体 `cardTextBumpSize`（`MonoBehaviour_-3358613892933444672.json` 实读 = **0.75**；
        /// `.ctor` 默认是 1.5）。
        /// 🔴 **2026-09-29 从 9 参 `SetText` 的指令流里坐实**（VA `0x18060C180`，见 `CardView.FlashStat`）：
        /// 它是**乘**在基准 scale 上的（`base × 0.75` 当 punch 幅度），不是「打到这个值」；
        /// 而且**先把 `localScale` 复位到基准**再 punch。</summary>
        public const float StatBumpSize = 0.75f;
        /// <summary>原版卡预制体 `cardTextBumpTime`（同上一份 JSON 实读 = **0.44999998807907104**；
        /// `.ctor` 默认才是 0.4 —— 文档里一度写成 0.4，那读的是构造函数不是预制体）。</summary>
        public const float StatBumpTime = 0.45f;
        /// <summary>`DOPunchScale` 的第 3 个实参 —— 原版是**寄存器立即数 `0xa` = 10**（`mov r9d,0xa`）。</summary>
        public const int StatBumpVibrato = 10;
        /// <summary>`DOPunchScale` 的第 4 个实参 —— 原版是**栈上传的常量 `2.0f`**（浮点池 `0x1834B2BBC`）。</summary>
        public const float StatBumpElasticity = 2.0f;

        /// <summary>原版卡预制体 `attackPositionYOffset` —— 作用在 `(受击方 effectsAnchor.y − 受击方 y)` 上</summary>
        public const float AttackPositionYOffset = 0.5f;
        /// <summary>段2 先绕 `Vector3.up` 转的角度（`.rdata 0x1834b2e04` = 90°）</summary>
        public const float AttackTurnDeg = 90f;
        /// <summary>段3「停在目标身上」的时长（`.rdata 0x1834b2dc4` = 0.1）</summary>
        public const float AttackHoldTime = 0.1f;
        /// <summary>段4/段5 归位的时长 = `2 × attackStepTime`（原版 `:260` 是 `attackStepTime * 2`）</summary>
        public const float AttackReturnTime = AttackStepTime * 2f;

        /// <summary>
        /// **段1 的目标点**（原版 `:139-197` 那个表达式，逐项照写）：
        /// `endPos = 受击方位置 − Normalize(受击方 − 出手点) × margin + Vector3.up × dy`，
        /// `margin = 攻方 isPlayer ? 1.7 : 1.0`，`dy = (受击方 effectsAnchor.y − 受击方 y) × 0.5`。
        /// ⚠️ `|方向| ≤ 1e-5` 时方向取零（原版同一个哨兵 `0x1834b2f44`）。
        /// ⚠️ `dy` 我们**取 0**：原版那个 `effectsAnchor` 我们**没有等价物**（我们的卡根节点**就是卡中心**），
        ///    如实标注 —— 不是「查不到」，是「我们这棵树的等价量就是 0」。
        /// </summary>
        public static Vector3 MeleeEndPos(Vector3 origin, Vector3 targetPos, bool isPlayer, float dy = 0f)
        {
            Vector3 d = targetPos - origin;
            float len = d.magnitude;
            Vector3 dir = len <= 1e-5f ? Vector3.zero : d / len;
            float margin = isPlayer ? PlayerAttackMargin : EnemyAttackMargin;
            return targetPos - dir * margin + Vector3.up * (dy * AttackPositionYOffset);
        }

        /// <summary>近战攻击的整条序列（段1～段5）。`endPos` 由 <see cref="MeleeEndPos"/> 算好传进来；
        /// `home` = 出手前那个**静止位**（局部坐标）—— 段4 回的就是它，**不是出手那一帧的位置**。</summary>
        public static Tween MeleeAttack(Transform tr, Vector3 endPos, Vector3 home, float delay = 0f)
        {
            if (tr == null) return null;
            Quaternion homeRot = tr.rotation;
            // 段2 的朝向：先绕 up 转 90°，再叠 `attackRotationAngle`（倾角）
            Quaternion toTarget = homeRot
                                * Quaternion.Euler(0f, AttackTurnDeg, 0f)
                                * Quaternion.Euler(0f, 0f, AttackRotDeg);

            var seq = DOTween.Sequence();
            seq.Append(tr.DOMove(endPos, AttackStepTime).SetEase(Ease.Linear));       // 段1（命中同帧）
            seq.Join(tr.DORotateQuaternion(toTarget, AttackStepTime));               // 段2
            seq.AppendInterval(AttackHoldTime);                                      // 段3
            seq.Append(tr.DOLocalMove(home, AttackReturnTime).SetEase(Ease.OutQuart)); // 段4
            seq.Join(tr.DORotateQuaternion(homeRot, AttackReturnTime));              // 段5
            return CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
        }

        /// <summary>
        /// **瞄准时的「抬手 / 后撤」**（原版 `CardScript.OrientToTargetingDirection`，只在
        /// `cardState == inPlayAminingAttack(=17)` 时由 `CardScript.Update` 每帧调）：
        /// 目标位 = `基准 + Vector3.back × chargeBackModifier × (isPlayer ? +1 : −1) + Vector3.up × chargeUpModifier`，
        /// 每帧 `localPosition = lerp(当前, 目标位, clamp01(dt / timeToChargeAttack))`。
        /// 两个系数与那个 0.35 都是卡预制体字段（0.5 / 0.35 / 0.35），**不是**我们挑的。
        /// ⇒ 这就是以前被误当成「攻击前摇」的那 0.35 s：**它发生在瞄准期间，不在攻击序列里**。
        /// </summary>
        public static Vector3 LeanOffset(bool isPlayer)
        {
            float sign = isPlayer ? 1f : -1f;
            return Vector3.back * (ChargeBackModifier * sign) + Vector3.up * ChargeUpModifier;
        }

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
                  .SetDelay(delay), Ease.OutQuad, tr);
        }

        /// <summary>挨打：位置弹一下 + 转一下。        ///
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
            => HitReact(tr, away, heavy, delay, -1);

        /// <param name="attackerMeleeAttack">**攻击方**的当前近战攻击（`UnitState.Attack`）。
        /// **传 -1 = 不知道攻击方是谁**（疲劳、反伤那类没有攻击方的伤害）⇒ 退回 `Impact *` 那套顶替值。
        /// 判据与出处见 <see cref="PushBackMagnitude"/>：原版的幅度**只由攻击方的近战档决定**。</param>
        public static void HitReact(Transform tr, Vector3 away, bool heavy, float delay, int attackerMeleeAttack)
        {
            if (tr == null) return;
            Vector3 d = Flat(away);
            float mag = attackerMeleeAttack >= 0
                ? PushBackMagnitude(attackerMeleeAttack)                 // 原版口径（0.5 / 1.0 / 1.5）
                : (heavy ? HitPunchUnitsHeavy : HitPunchUnitsLight);     // 兜底：`Impact *` 顶替值
            float rot = heavy ? HitRotHeavyDeg : HitRotLightDeg;

            // 位置：DoPushBack 的**形状**（0.4 / vib 8 / 弹性 0.3）+ 顶替来的幅度
            CardTween.Use(
                tr.DOPunchPosition(d * ToOurs(mag), PushBackDuration, PushBackVibrato, PushBackElasticity)
                  .SetDelay(delay), Ease.OutQuad, tr);

            // 旋转：`Impact Light Tween` 的第二条 punch（0.5s / vib 7 / 弹性 1 / InQuad）
            // ⚠️ 原版那条打的是 `abilityTarget`，位置那条打的是 `self` —— 这里都作用在挨打这张卡上
            CardTween.Use(
                tr.DOPunchRotation(new Vector3(0f, 0f, Sign(d) * rot), HitRotDuration,
                                   HitRotVibrato, HitRotElasticity), Ease.InQuad, tr);
        }

        /// <summary>最近一次震镜头。**自检要拿它核对「有没有把相机放回去」** ——
        /// 半路被打断而不复位的话，镜头会**永久**歪着（而且 HUD 跟着歪）。</summary>
        public static Tween LastShake;

        /// <summary>
        /// 挨打震镜头。**原版是震主相机**（执行链见上面那一节的注释），不是逐个单位位移。
        ///
        /// **我们怎么落地**（这一段是**迁移**，不是原版做法）：
        ///   我们的战场只有**一台正交相机**、**没有 uGUI Canvas**，HUD 是**世界空间 quad**、
        ///   而且和棋盘挂在同一个根下 ⇒ **「震战场不震 UI」在现在的结构下做不到**。
        ///   要复刻原版的观感（战场晃、UI 不晃），这里用「**相机与 HUD 根同向平移**」：
        ///   相机往 `dir` 走 `d`，HUD 根也往 `dir` 走 `d`。正交相机下屏幕坐标 ∝ (world − cam)，
        ///   两者同向等量 ⇒ **HUD 在屏幕上纹丝不动**，而棋盘/卡牌/特效整体晃 `d`。
        ///   （⚠️ 是**同向**不是反向 —— 相机 +d 会让内容在屏幕上 −d，HUD 要留在原地就得也跟着 +d。）
        ///
        /// 方向：原版 `direction (0, 0.2, 0.2)` 是 **3D 世界方向**（相机沿 +z 看、略向下俯）。
        /// 它的 z 分量是「朝/背镜头」= **屏幕上不动**，只有 y 分量是屏幕上的上下 ⇒ 压到屏幕平面
        /// = **竖直**。⚠️ **这一步是我们的换算**；而且「向上还是向下」**静态数据判不出来**
        /// （要判得跑原版实测），这里取**相机向上抬**（读起来像「被撞得往后一仰」）。
        ///
        /// 时间轴严格照 preset：`attackTime 0`（一帧到满）→ 保持 `sustainTime` → **线性**衰减到
        /// `ShakeDecayTo`、历时 `decayTime`。总长 0.4 s。
        /// 位移 = 峰值 × **包络**(t) × **噪声**(t)，噪声是 `Warpforge 6D Shake` 的 Y 轴三条带按
        /// `m_FrequencyGain` 放慢后叠加（见 `ShakeBandHz` / `ShakeNoise01`）。
        /// </summary>
        public static Tween ShakeCamera(Camera cam, Transform hudRoot, float worldAmp, float delay = 0f,
                                        Camera extraCam = null)
        {
            if (cam == null) return null;

            // 🔴 **从「收干净的位置」起步**（2026-09-19 修，自检抓到的真 bug）。
            //    下面 `camHome/hudHome` 取的是**当前的 transform 值**；而上一次震动**没跑完**时，
            //    那两个值就是**被推歪的**位置 ⇒ 收尾时把偏移**固化成新的原点**
            //    （表现：打过几轮之后镜头/HUD 永久歪掉一个常量，而且**不报错**）。
            //    触发条件很常见：两次挨打靠得近（批处理里更必然 —— DOTween 不跑，震动全靠手动推）。
            //    实测（`BattleScene.Run` 的回放条/换牌两条断言）：HUD **竖直偏高 0.4210 世界单位**，
            //    x 分毫不差 —— 正是这里的方向（`dir = Vector3.up`）。
            SettleShake();

            var camTr = cam.transform;
            Vector3 camHome = camTr.position;
            Vector3 hudHome = hudRoot != null ? hudRoot.localPosition : Vector3.zero;
            // 🆕 2026-09-19：**3D 战场那台透视相机也跟着推**。不推的话挨打时「卡牌在动、背景纹丝不动」
            //    （战场现在是真 3D 了，原版震的本来就是战场相机）。两台相机同向等量平移 ⇒
            //    战场与卡牌的位移方向一致；像素幅度由各自投影决定（透视那侧略有视差，这是对的）。
            var extraTr = extraCam != null ? extraCam.transform : null;
            Vector3 extraHome = extraTr != null ? extraTr.position : Vector3.zero;
            Vector3 dir = Vector3.up;

            System.Action<float> apply = t =>
            {
                Vector3 d = dir * (worldAmp * ShakeEnvelope(t) * ShakeNoise01(t));
                if (camTr != null) camTr.position = camHome + d;
                if (hudRoot != null) hudRoot.localPosition = hudHome + d;
                if (extraTr != null) extraTr.position = extraHome + d;
            };

            float total = ShakeAttackTime + ShakeSustainTime + ShakeDecayTime;
            // `attackTime = 0` ⇒ **当场**上到峰值。别等第一次 Update ——
            // 批处理里没有帧循环（和 `DealIn` 里 alpha 那条同款注释），等更新会把第一帧漏掉。
            apply(0f);

            var seq = DOTween.Sequence();
            seq.Append(DOTween.To(() => 0f, t => apply(t), total, total).SetEase(Ease.Linear));

            var t = CardTween.Use(seq, Ease.Linear, cam).SetDelay(delay);
            // ⚠️ **跑完/被打断都要把相机和 HUD 放回原位** —— 不复位的话镜头会**永久**歪着
            //    （`DealIn` 里「打断也要把 alpha 补回 1」是同一条规矩）。
            //    DOTween 正常跑完时也会触发 `OnKill`，所以这一条就够。
            t.OnKill(() =>
            {
                if (camTr != null) camTr.position = camHome;
                if (hudRoot != null) hudRoot.localPosition = hudHome;
                if (extraTr != null) extraTr.position = extraHome;
            });
            LastShake = t;
            return t;
        }

        /// <summary>
        /// 把**还没跑完的震镜头**收尾（复位相机 + `HudRoot`）。
        ///
        /// **为什么要有它**：位移在**建 tween 的那一刻就落到 transform 上了**（`attackTime = 0`），
        /// 靠 tween 跑完 / 被杀时的 `OnKill` 才复位。而**批处理没有帧循环** ⇒ 一次由特效触发的
        /// 震动（`WFModuleScreenShake.OnShake` → `ShakeCamera`）如果没被推完，相机与 `HudRoot`
        /// 就**停在偏移位上**，后面「量 UI 世界坐标 / 按世界坐标点按钮」的断言会**同时**差一个常量。
        /// 🔴 实测（2026-09-19）：屏震钩子从 `Start()` 挪进 `Begin()` 之后，自检里第一次真的震起来，
        ///    回放条与换牌气泡两条断言都差 **0.4210** 世界单位（同一个常量、方向一致）。
        /// ⚠️ 真实播放里不需要（DOTween 在 `Update` 里跑，自己会收尾）——
        ///    这是**自检专用**的收尾口，而且按工程惯例**不让调用方直接碰 DOTween**
        ///    （`BattleDriver` / 自检都没有 `using DG.Tweening`）。
        /// </summary>
        public static void SettleShake()
        {
            var s = LastShake;
            if (s != null && s.IsActive()) s.Complete();     // `Complete()` 会触发 `OnKill` ⇒ 复位
        }

        /// <summary>包络：`attackTime 0` ⇒ 立刻 1；保持 `sustainTime`；再**线性**降到 `ShakeDecayTo`
        /// （**终点不是 0** —— 出处见 `ShakeDecayTo`）。</summary>
        static float ShakeEnvelope(float t)
        {
            float held = ShakeAttackTime + ShakeSustainTime;
            if (t <= held) return 1f;
            return Mathf.Lerp(1f, ShakeDecayTo, Mathf.Clamp01((t - held) / ShakeDecayTime));
        }

        /// <summary>归一化到 [−1, 1] 的噪声（三条带同相时 = 1，也就是 t=0）。
        /// ⚠️ 起相用 **cos** 不是 sin：原版 `attackTime 0` 是「一帧到满」，
        /// 而 sin 从 0 起会让第一帧完全没有位移。
        /// ⚠️ 原版是 Perlin 噪声、而且 `m_Randomize: 1`（**相位每次随机**）；我们用**同频同幅的正弦**叠加近似
        /// —— 这条近似是我们的，但因为原版自己每次都不同，「复刻原版那一条」本来就没有意义。</summary>
        static float ShakeNoise01(float t)
        {
            float s = 0f;
            for (int i = 0; i < ShakeBandHz.Length; i++)
                s += ShakeBandAmp[i] * Mathf.Cos(2f * Mathf.PI * ShakeBandHz[i] * ShakeFrequencyGain * t);
            return s / ShakeBandPeak;
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
            return CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
        }

        /// <summary>
        /// **回手 / 回牌库** —— 原版 `BattleCardUI.PlayBackToHandAnimation`：**把 `Card Hand To Board`
        /// 倒着播**（`Rewind()` → `state.speed = −1`（`DAT_1834b2bc8` 实读 **−1.0**）→
        /// `state.time = state.length` → `Play()`），随后 `DelayedResetMaterial(0.55)` 把 3D 体换回正常材质。
        ///
        /// 🔴 **反向时间轴 = 把正向关键帧镜像过来**（正向值见上面 `HandToBoard*` 那几个常量）：
        ///   · **0** —— 3D 体还完整（正向 0.9167 处 `_DissolveAmount = 0`）
        ///   · **0.2167**（= 0.9167 − 0.7）—— 2D 卡面**打开**（正向在 0.7 关掉）；3D 体开始溶解
        ///   · **0.25 – 0.5833**（= 正向 0.3333–0.6667 的镜像）—— 2D 卡面 alpha 0 → 1
        ///   · **0.75**（= 0.9167 − 0.1667）—— 3D 体**溶完**并被关掉（正向 0.1667 才打开）
        ///   · 位移：`DOMove(当前位置 + Vector3.up × localScale.x × 3.0, **0.208 s**)`、**线性**
        ///     （`SetEase(1)` = `DG.Tweening.Ease.Linear`；两个字面量 `DAT_1834b2e8c = 3.0` /
        ///      `DAT_1834b317c = 0.208` 都是实读的），在倒放**一开始**就起（与 clip 并行）。
        ///
        /// ⚠️ **我们没有那条 clip**（我们的卡没有 `Animation` 组件）⇒ 按**同一条时间轴用代码喂值**。
        /// ⚠️ 取不到原版溶解 shader 时**退回「淡出」并出声**（`CardView.DissolveSupported`）——
        ///    那是我们的做法，不是原版的（原版一定是溶解）。
        /// </summary>
        public static Tween ReturnToHand(CardView card, float delay = 0f, System.Action onDone = null)
        {
            if (card == null) return null;
            var tr = card.transform;

            // 反向时间轴上的四个时刻（全部由正向那组常量推出来，**别另填数字**）
            float artOn = HandToBoardEnd - HandToBoardDissolveEnd;          // 0.2167
            float artFadeStart = HandToBoardEnd - HandToBoardFadeEnd;       // 0.2500
            float artFadeEnd = HandToBoardEnd - HandToBoardFadeStart;       // 0.5833
            float bodyGone = HandToBoardEnd - HandToBoardLand;              // 0.7500

            bool canDissolve = card.BeginDissolve();
            if (!canDissolve)
                Debug.LogWarning("[CardFeel] 卡体溶解不可用（取不到原版 shader `Everguild/Cards/3D Card Dissolve`）"
                               + " ⇒ 回手这一下**用淡出顶上**，与原版表现不一致（原版是材质溶解）");

            var seq = DOTween.Sequence();
            seq.AppendInterval(HandToBoardEnd);      // 先把总长定成 0.9167，下面全是 Insert（不受游标影响）

            // ① 抬起：倒放一开始就起，0.208 s **线性**（原版 `DOMove(… + up × scale.x × 3, 0.208)`）
            float up = tr.localScale.x * ReturnLiftScale;
            seq.Insert(0f, tr.DOMove(tr.position + new Vector3(0f, up, 0f), ReturnLiftTime).SetEase(Ease.Linear));

            if (canDissolve)
            {
                // ② 3D 体溶解出：反向 [0.2167, 0.75] ⇒ `_DissolveAmount` 0 → 1
                seq.Insert(artOn, DOTween.To(() => card.DissolveAmount, a => card.SetDissolveAmount(a),
                                             1f, bodyGone - artOn).SetEase(Ease.Linear));
                // ③ 2D 卡面：0.2167 打开、[0.25, 0.5833] 淡回 1（原版那两个字段就是这两个时刻）
                seq.InsertCallback(artOn, () => card.SetArtAlpha(0f));
                seq.Insert(artFadeStart, DOTween.To(() => card.ArtAlpha, a => card.SetArtAlpha(a),
                                                    1f, artFadeEnd - artFadeStart).SetEase(Ease.Linear));
            }
            else
            {
                // 兜底：整卡淡出（**我们的做法**，见方法注释）
                seq.Insert(0f, DOTween.To(() => card.Alpha, a => card.SetAlpha(a), 0f, HandToBoardEnd));
            }

            var t = CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
            if (onDone != null) t.OnComplete(() => onDone());
            return t;
        }

        /// <summary>回手时那一下抬升的位移时长（秒）= `DAT_1834b317c` 实读 **0.208**
        /// （`CardScript._TransformUnitInPlayToCard_d__256:53-73`）。</summary>
        public const float ReturnLiftTime = 0.208f;
        /// <summary>抬升的高度 = `localScale.x × 3.0`（那个 3.0 = `DAT_1834b2e8c` 实读；
        /// 方向是 `Vector3.up`（静态字段偏移 `0x18` 实读 (0,1,0)，x/z 不动））。</summary>
        public const float ReturnLiftScale = 3.0f;

        // ==================================================================
        //  阵亡（原版 `CardScript.UnitDeath`）—— **不是把场上的卡溶解掉**
        // ==================================================================
        // 判据（2026-09-29 逐句读过；🔴 原文那条「阵亡 = 材质溶解 `_DissolveAmount`」**已作废** ——
        // 那是把**回手 / 出战**那条链的机制安到了阵亡头上）：
        //   ① 场上那张卡的 3D 体**直接关掉**：`battleCardUI.body3D(+0x158).SetActive(false)`
        //      （`CardScript._UnitDeath_d__446__MoveNext.c:330-333`）
        //   ② 在卡位**另生成**死亡爆散体：`Instantiate(cardDestroyFX(+0x1D8), 卡位置, 3D体旋转)`（`:194-347`），
        //      那件自带 Animator（controller `Card 3D WH40K Explosion`：1 layer / 1 state / 0 参数）播一条
        //      **0.8167 s** 的 clip `Card Explosion`；材质 `Card 3d WH40K Explosion` **从卡上借贴图**、
        //      外加 `_ExplosionTextureOffsetModifier = Random.value`（所以每次爆散纹理偏移都不同）
        //   ③ 卡自己抖一下：`DOShakePosition(时长 = 小兵 `deathTimeMinionDuration` 0.2 / 督军 0.5,
        //      强度 **(0.3, 0.05, 0)**, vibrato **10**, randomness **90**, fadeOut **true**)`（`:100-116`）
        //   ④ 之后 `WaitForSeconds(0.5)` → `WaitForSeconds(0.2)` → `GoToCemetery()`（另有两条音效）
        // ✅ **2026-10-01：「非 addressable 拿不到」那条缺口已经补上**（正本 → `项目任务.md` §〇 第 13 条）——
        //    它**不是 addressable**（被卡预制体字段引用）⇒ `AssetBundle.GetAllAssetNames()`（983 条）与
        //    `LoadAllAssets<GameObject>()`（965 个）**两条枚举路都不含它**（2026-09-29 实测），
        //    而同一份包用 UnityPy 数得出来（资产在、API 够不着）⇒ **另开了一条导入路**：
        //      `python 工具/extract_missing_shaders.py --prefabs`
        //        （把它 + **整棵依赖树**重打成 `StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle`；
        //         🔴 内层 CAB **必须改名**，否则与源包撞名、Unity 报「already loaded」直接拒收）
        //      → `EffectExporter.RunListed`（已改成同时扫 `StreamingAssets/WarpforgeVFX/`）→ `EffectLibraryBuilder.Run`
        //    判据 → `资料/已知的坑.md` 的「`GetAllAssetNames()` 只吐容器里的资产」那条。
        //    ⚠️ 那两步是**本地产物**（`.bundle` 与 `Assets/WarpforgeVFX/*` 都在 .gitignore 里）⇒
        //    别的机器没跑过时会走到下面的**退回分支**（出声，不静默）。

        /// <summary>死亡爆散体在我们效果库里的键（原版那件 prefab 的名字）。</summary>
        public const string DeathBodyFx = "Card 3D Death Explosion";
        /// <summary>阵亡抖动的强度 X（实读 `0x1834b2dc8` = 0.3）与 Y（`0x1834b2f94` = 0.05）</summary>
        public const float DeathShakeX = 0.3f;
        public const float DeathShakeY = 0.05f;
        /// <summary>阵亡抖动的 vibrato / randomness（字面量 10 / 90 实读）</summary>
        public const float DeathShakeVibrato = 10f;
        public const float DeathShakeRandomness = 90f;

        /// <summary>
        /// **阵亡**：关掉 3D 体 → 在卡位生成死亡爆散体 → 卡抖一下。
        /// 取不到那件爆散体时**退回** <see cref="Dissolve"/>（旧表现）并出声。
        /// </summary>
        public static Tween DeathExplosion(CardView card, float delay = 0f, System.Action onDone = null,
                                           bool isWarlord = false)
        {
            if (card == null) return null;
            if (!SpawnDeathBody(card))
            {
                // 取不到那件真爆散体 ⇒ 用**当初的替代品**顶上（`Explosion Fenrisian Monstrosities`）——
                // 它原来挂在 `BattleDriver.PlaySignal` 的 `EvtKind.Death` 那一格上，2026-10-01 那件真件
                // 进来了、从那里摘掉（否则两件同时播 = 原版没有的第二下），挪到这条退回路里。
                // （`SpawnDeathBody` 已经在上面**出过声**了。）
                WarpforgeVFX.WarpforgeEffectPlayer.Play(VfxMap.Death, card.transform);
                return Dissolve(card, delay, onDone, isWarlord);      // 旧表现：淡出 + 上浮 + 缩
            }

            float dur = DeathDissolve(isWarlord);
            var tr = card.transform;
            card.SetBody3DVisible(false);                             // ①
            var seq = DOTween.Sequence();
            // ③ 抖动（卡本身还在原地 —— 原版关的是「3D 体」，卡根那个 Transform 留着给它抖）
            seq.Append(tr.DOShakePosition(dur, new Vector3(DeathShakeX, DeathShakeY, 0f),
                                          (int)DeathShakeVibrato, DeathShakeRandomness, false, true));
            var t = CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
            if (onDone != null) t.OnComplete(() => onDone());
            return t;
        }

        /// <summary>在卡位生成死亡爆散体（原版 `Instantiate(cardDestroyFX, 卡位置, 3D体旋转)`）。
        /// 返回 false = 效果库里没有那件 ⇒ 调用方退回旧表现。**出声**，不静默。</summary>
        static bool SpawnDeathBody(CardView card)
        {
            var p = WarpforgeVFX.WarpforgeEffectPlayer.Play(DeathBodyFx, null, card.transform.position, 1f);
            if (p == null)
            {
                Debug.LogWarning($"[CardFeel] 效果库里没有 `{DeathBodyFx}` ⇒ 阵亡退回「淡出 + 上浮 + 缩」"
                               + "（原版是「关掉 3D 卡体 + 在卡位生成那个爆散体」）。补它两步（本机已跑过）："
                               + "`python 工具/extract_missing_shaders.py --prefabs` → `EffectExporter.RunListed`"
                               + " → `EffectLibraryBuilder.Run`；判据 → `资料/已知的坑.md` 的"
                               + "「`GetAllAssetNames()` 只吐容器里的资产」那条");
                return false;
            }
            // 原版从卡上借贴图：`_CardImage = rawCard.cardSprite.texture` · `_MatCap = GetCardMatCapByCardTier(tier)`，
            // 再给爆散纹理一个**随机偏移**（`_ExplosionTextureOffsetModifier = Random.value`）。
            var art = card.BodyArtTexture;
            var matcap = CardArt.Card3DMatcap();
            foreach (var r in p.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = r.material;
                if (m == null) continue;
                if (art != null && m.HasProperty("_CardImage")) m.SetTexture("_CardImage", art);
                if (matcap != null && m.HasProperty("_MatCap")) m.SetTexture("_MatCap", matcap);
                if (m.HasProperty("_ExplosionTextureOffsetModifier"))
                    m.SetFloat("_ExplosionTextureOffsetModifier", UnityEngine.Random.value);
            }
            return true;
        }

        /// <summary>阵亡消散（**旧表现 —— 只在取不到原版那件爆散体时用**）。原版是**材质 `_DissolveAmount`** 从 1 溶到 0
        /// （见 `Card Hand To Board` clip 里 `Card 3D` 那条曲线）。
        /// ⚠️ **我们没有溶解 shader**，退而用「透明度 + 轻微上浮/缩小」—— 表现形式是我们的。
        /// 🔴 **时长照原版分档**（2026-09-18）：小兵 `deathTimeMinionDuration 0.2` ·
        ///    督军 `deathTimeWarlordDuration 0.5` —— 见 <see cref="DeathDissolve"/>。
        ///    （改之前两边都用 0.5333，那是从出战 clip 对称借来的，**不是原版值**。）</summary>
        public static Tween Dissolve(CardView card, float delay = 0f, System.Action onDone = null,
                                     bool isWarlord = false)
        {
            if (card == null) return null;
            var tr = card.transform;
            float dur = DeathDissolve(isWarlord);
            Vector3 up = new Vector3(0f, ToOurs(0.3f), 0f);     // 上浮量：我们挑的
            var seq = DOTween.Sequence();
            seq.Append(tr.DOMove(tr.position + up, dur).SetEase(Ease.InSine));
            seq.Join(tr.DOScale(tr.localScale * 0.85f, dur).SetEase(Ease.InQuad));
            seq.Join(DOTween.To(() => card.Alpha, a => card.SetAlpha(a), 0f, dur));
            var t = CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
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
            // 🔴 **2026-09-17 照原版改回**：原版 `DamageText` 的 TMP `m_fontColor` = **纯白 (1,1,1)**、
            //    `HealText` **也是纯白**（`08_预制体特效/战斗预制体/MonoBehaviour_5890981294211439552` /
            //    `MonoBehaviour_-7677963611639080000`，2026-09-17 亲读）。我们原来按「受伤红 / 治疗绿」
            //    上色 —— **那是我们挑的**，不是原版的做法。
            // ⚠️ **字号仍是我们挑的**：原版 5.5 + autoSize(0.35~5.5)，单位与我们的 `Label` 不同，搬不过来。
            var lbl = Label.Create(parent, txt, worldPos, 16, Color.white,
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
            LastPopTween = CardTween.Use(seq, Ease.Linear, tr);
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
        ///
        /// 🔴 **2026-09-17 换成正解**：原版值 = `VarsGlobal.timeToDrawPlayerCard = 0.3`。
        /// **原来写的是「时长查不到 → 借 `Card Hand To Board` 的完成时刻 0.55」，
        /// 那条的因果链是错的**：`CardScript` 的 `timeToDraw` 确实没被序列化，但同一个时长
        /// 在 `VarsGlobal` 里有**独立字段**，而且是**分敌我的一对**（我方 0.3 / 敌方 0.15）——
        /// 当年是「按字段名在解包树上 grep 0 命中」就判了「查不到」，根因那份 `.assets` 没有 type tree。
        /// 出处：`资料/VarsGlobal_原版数值.md` §一。
        ///
        /// ✅ **2026-09-17 起敌我已经分开了**（原来这里注的是「我们还没分敌我，敌方牌堆只是 HUD 图片」——
        /// 那是**我们缺件**、不是原版没有）：敌方那一版走 `DealDurationFoe` + `DealInQuad`，
        /// 由 `BattleDriver.SyncFoeHand()` 驱动。**判据只此一处**：`DealSeconds(bool mine)`。</summary>
        public const float DealDuration = 0.3f;

        /// <summary>**敌方**发牌用时 = `VarsGlobal.timeToDrawEnemyCard = 0.15`。
        /// 原版敌我走**同一条代码路径**、只按 `isPlayer` 取不同字段
        /// （`VarsGlobal.TimeToDrawCard(bool isPlayer)`，调用点见 `资料/敌方手牌_原版规格.md` §五）。
        /// ⚠️ 2026-09-17 之前我们**根本没有敌方手牌**（那一整件没建），所以这个值无处可落 ——
        /// 不是「单机用不上」，是**缺件**。</summary>
        public const float DealDurationFoe = 0.15f;

        /// <summary>按敌我取发牌用时 —— 🔴 **判据只此一处**，别在外面各写一份。</summary>
        public static float DealSeconds(bool mine) { return mine ? DealDuration : DealDurationFoe; }

        // ==================================================================
        //  「让位 / 补位」那一下的位移（2026-10-01 加）
        // ==================================================================
        //
        // 原版：棋盘是**连续列表**（`RuleEngine/Core/BoardSlots.cs` 记着全套判据），
        // 插入/离场会把后面的单位整体推出去 / 拉回来一格，随后 `MinionManager.ReassembleMinions`
        // 逐个把它们补间到新格位 —— 那一句就是 `DOLocalMove(新位, 时长)`
        // （`CardScript__UpdateMinionInPlayPosition.c:89` 亲读；**只动位移**，旋转/缩放不碰）。
        //
        // 🔴 **时长是算出来的、不是抄的**：`MinionManager__ReassembleMinions.c:47` 写的是
        //    `时长 = globalVars+0xa0 ÷ 1.5`。两半都有出处：
        //      · `globalVars+0xa0` = **`minionReassembleTime` = 0.15**
        //        （偏移按 `VarsGlobal.cs` 桩的字段顺序推、字段起点 0x18；同法验过的四个锚点：
        //         `cardInHandMovingScale+0x44=1.1` · `minionInPlayScale+0x68=0.75` ·
        //         `enemyUnitsScaleRatio+0x74=0.86` · `targettingAnimTime+0x88=0.5` —— 全对得上；
        //         整表见 `资料/VarsGlobal_原版数值.md`）
        //      · `DAT_1834b3090` = **1.5**（`.rdata` 硬编码 —— `资料/普查产出_0918/第18行_手感与选牌_规格.md:14`）
        //    ⇒ **0.15 / 1.5 = 0.1 s**
        //
        // ⚠️ **缓动是我们推的**：原版那句 `DOLocalMove` **没有 `SetEase`** ⇒ 走 DOTween 的全局默认
        //    （= `Ease.OutQuad`，除非原版改过 `DOTween.defaultEaseType` —— 那个查不到）。这里照默认写。
        public const float ReassembleTime = 0.1f;

        /// <summary>让位 / 补位：把这张卡补间到它的新格位（只动位移）。
        /// ⚠️ 必须经 `CardTween.Use`（批处理要 `UpdateType.Manual`、且要 `SetLink` 防止卡销毁后补间还在跑）。</summary>
        public static Tween Reassemble(CardView card, Vector3 toLocalPos, float delay = 0f)
        {
            if (card == null) return null;
            var t = card.transform.DOLocalMove(toLocalPos, ReassembleTime);
            return CardTween.Use(t, Ease.OutQuad, card.transform).SetDelay(delay);
        }

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
            var t = CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
            // ⚠️ **被打断时要把 alpha 补回 1**：`HandLayout.Place` 会 `DOKill()` 掉这张卡身上的补间
            //    （重排/让位时），补间死在半路的话卡会**永远半透明**地留在手里。
            //    位置不用管 —— 打断它的那条补间自己会把位置摆对。
            t.OnKill(() => { if (card != null) card.SetAlpha(1f); });
            return t;
        }

        /// <summary>发牌入场 —— **敌方手牌那一版**：那张卡是 `ImageQuad` 画的**卡背**，
        /// 不是 `CardView`（原版敌方走 `CardScript.ShowCardBack(!isPlayer)`，见 `资料/敌方手牌_原版规格.md` §四）。
        /// 形状与 `DealIn` 同源，时长取 `DealSeconds(mine: false)` = **0.15**。
        /// ⚠️ 原版抽牌那一瞬还会 `TurnCardAround`（绕 Y 翻 180°）；我们是 2D，**没做那一下翻转**。</summary>
        public static Tween DealInQuad(ImageQuad q, Vector3 from, float delay = 0f)
        {
            if (q == null) return null;
            var tr = q.transform;
            Vector3 to = tr.position;
            tr.position = from;
            q.SetTint(new Color(1f, 1f, 1f, 0f));           // 见 `DealIn` 里那条「当场置 0」的注释
            float d = DealDurationFoe;

            var seq = DOTween.Sequence();
            seq.Append(tr.DOMove(to, d).SetEase(Ease.OutCubic));
            seq.Join(tr.DOScale(Vector3.one, d).SetEase(Ease.OutCubic));
            seq.Join(DOTween.To(() => 0f, a => q.SetTint(new Color(1f, 1f, 1f, a)), 1f, d * 0.5f));
            var t = CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
            t.OnKill(() => { if (q != null) q.SetTint(Color.white); });
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

        // · ~~`attackRotationAngle 25`（出手时卡体的倾角）—— 3D 里的旋转，2D 卡上还没决定怎么表达~~
        //   ✅ **2026-09-29 已接上**（`MeleeAttack` 段2 的转向，`AttackRotDeg`）
        // · ~~`attackPositionYOffset 0.5`（攻击位移的 Y 偏移）~~ ✅ **2026-09-29 已接上**
        //   （`MeleeEndPos` 的那个 `dy`；⚠️ 我们的等价量恒 0 —— 卡根就是卡中心）
        // · ~~`playerAttackMargin 1.7` / `enemyAttackMargin 1.0`~~ ✅ **2026-09-29 已接上**（`MeleeEndPos`）
        // · `timeToLand 0.2` / `timeBeforeLand 0.05` —— ✅ **这一条已经接上了**：见 `DeploySequence`
        //   （不过那边用的是 `MinionManager.minionToConversionPointTime = 0.3`，
        //     `timeToLand` 是「落地那一下」的时长，等有落地特效时再用）
        // · `cardMovementSpeed 10.8` —— 卡的移动速度。⚠️ **用途查不到**：调用点没被反编译，
        //   拿它算「手牌飞多久」（距离÷速度）会得到 0.12s 这种明显不对的数 —— **别当它是时间基准**
        // · ~~`meleeHitCameraShakePreset`（amplitude 2.0）—— 挨打震镜头；要做得先决定
        //   2D 战场怎么表达 3D 的相机抖动（动相机？动整个背景 quad？）~~ —— ✅ **2026-09-17 已接上**：
        //   见上面「① 挨打震镜头」那一节与 `ShakeCamera`。原版问的那个问题**原版自己有答案**：
        //   震**主相机**（Cinemachine Impulse）、**不震 UI**。我们单相机分不开，
        //   所以落地方式是「相机 + HUD 根**同向**等量平移」，观感与原版一致。
    }
}
