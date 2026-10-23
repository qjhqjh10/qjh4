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
using System.Collections.Generic;
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

        /// <summary>🔴🔴 **2026-10-22（W64 · `B02`）：这五个常量【已不再被 `HitReact` 使用】** ——
        /// 原版**近战挨打没有旋转**（`CardScript__ReceiveAttackAnim.c:80-92` 只有一句
        /// `DOPunchPosition`），那两条旋转 punch 属于 `Impact Light/Heavy Tween` = **远程弹着特效的
        /// AnimFX 模块**（90 件 `BulletImpact_*` 引用它），把它当近战挨打是机制错位。
        /// ⚠️ **`HitRotDuration` 仍有一个消费者**：`EventTiming.cs` 的
        /// `DurationOf(EvtKind.Hit) = HitRotDuration + ResetDuration` —— 那一拍的总时长口径不变，
        /// 但**它现在不代表"转了 0.5 秒"**，只是那一拍的长度。⛔ 别因为「没人用」把它删掉。
        /// 🔴 **2026-10-23（P2 · `新账-78②`）**：既然这两条 punch 是**远程弹着**的，「`Hit` 拍 = 0.5 + 0.25」
        /// 这个**出处也就不成立了** —— 值仍是 0.75（我们的），但**出处待补**；
        /// 候选真值 = `CardScript._ReceiveDamage_d__381:342` 的 `WaitForSeconds(1.0)`
        /// （被 `+0x34` bool 把着、**语义未查清**）⇒ 判据未闭合，⛔ 别猜着改（改它会动整条节奏基线）。
        /// 下面这两段旧注释保留，它们记的是那两条 punch **本身的**读数（将来做远程弹着要用）。
        ///
        /// 命中旋转 punch 的度数 —— **两处一律取 punch 向量的「模长」**（原版那两条 punch
        /// 是 3D 的 x/y 两个轴，我们的 2D 只有一个 z 轴 ⇒ 取模长当度数）。
        /// 🔴 **2026-10-18 之后 · 第五会话（§26 ⑨）：用户拍板 = 甲「统一到模长」** ——
        ///   `HitRotLightDeg` 的旧值 `3f` 是**「最大分量」**那个读法，而旧注释却写着「模长」
        ///   ⇒ 两处口径互相矛盾；已改 `4.2426f`（`|(-3,-3,0)| = √18 = 4.24264…`）。
        ///   ⛔ 这不是「手感回调」：改的是**我们自己的 3D→2D 口径**，两条注释从此只说一种话。
        /// 判据 = `d:/2/新解包资源/assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour/`
        ///   的 `Impact Light Tween.json`（`punchType 2` · `punch(-3,-3,0)`）与
        ///   `Impact Heavy Tween.json`（`punch(-15,4,0)`），**我们采的那几个字段**两条相同
        ///   （`duration 0.5` · `vibratto 7` · `elasticity 1.0`）。
        /// ⚠️ **2026-10-09 顺手订正**：原来这里写「**其余字段**两条相同」—— **不成立**：
        ///   `targetUnit` 两条**不同**（Light = **30** · Heavy = **10**，实读）；`ease` 两条都是 `5`。
        ///   `punchType 2` 下 `targetUnit` 的语义**未查** ⇒ 只如实标，⛔ **别当成缺陷去改**。</summary>
        public const float HitRotLightDeg = 4.2426f;  // |(-3,-3,0)| = √18 = 4.24264…
        public const float HitRotHeavyDeg = 15.5f;    // |(-15,4,0)| = 15.5242…（这条本来就按模长，取一位小数）
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
        /// 🔴 **2026-10-14（`A1767` / #10）就地订正（铁律 5）**：这里原来写着
        ///   「**我们只用到第 1 档**：我们的单位不占多格，没有「尺寸」这个概念（尺寸 2/3 查到了但接不上）」
        ///   —— **那句是错的**，而且错在两个地方：
        ///   ① `GetUnitSize` **不是**「占几格」，它的判据是**单位的当前近战攻击**
        ///      （`SupportMethods__GetUnitSize.c:11-16`：`m = e.CurrentMeleeAttack; m ≤ 3 ? 0 : (m ≤ 7 ? 2 : 3)`）
        ///      —— 这一份判据**我们在本地早就有**（同文件 `UnitSizeOf`，`PushBackMagnitude` 就在用它）；
        ///   ② 所以三档**全都接得上**，只要按攻击方的近战档取（见下面 `ShakeAmplitudeForUnitSize`）。
        ///   **错因**：把 `size` 读成了「棋盘格占用数」（那是另一件事），于是拿「我们的单位不占多格」当结论。
        ///   判据同 `PushBackMagnitude` 的那段注释（那里一直是按近战攻击分档的，两处本该同源）。</summary>
        public const float ShakeAmpUnitSize1 = 2.0f;
        public const float ShakeAmpUnitSize2 = 4.0f;
        public const float ShakeAmpUnitSize3 = 8.0f;

        /// <summary>原版 preset 真件 `Shake Hit Small` 的 `amplitude` 字段。
        /// 出处：`d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/Shake Hit Small_4555857231027011253.json`
        /// 的 `preset.amplitude = 2.0`（`CameraShakePreset`）。⚠️ 它**只是壳** —— 尺寸 2/3 那两档
        /// 会被 `ShakeAmpUnitSize2/3` 覆盖（见上）。</summary>
        public const float ShakePresetAmplitude = 2.0f;

        /// <summary>同一个真件的 `preset.direction = (0, 0.2, 0.2)` 的**模长** = 0.28284271…
        /// 原版 `m_AmplitudeGain = preset.amplitude × |direction|` ⇒ 这一位**单独**量出来，
        /// ⛔ 不要再把 0.5657 这个**乘积**当字面量写死（拆开才能按档换 amplitude）。</summary>
        public const float ShakePresetDirMag = 0.28284f;

        /// <summary>原版 `m_AmplitudeGain` = `preset.amplitude × |direction|`，`Shake Hit Small` 那件
        /// = 2.0 × 0.28284 = **0.5657**。
        /// 🔴 它**同时是旋转那一路的 gain**：`LegacySignalSource::GetSignal`（RVA 0xA2B350）里
        /// `gain = m_Velocity.magnitude × m_AmplitudeGain`（`m_Velocity` 传入前已归一化 ⇒ |v| = 1），
        /// 然后 `rot = SlerpUnclamped(identity, rot, gain)` —— 也就是说**同一份 gain**同时缩位置与角度。
        /// （判据 → `资料/普查产出_1023b/R9_rawSignal_算式.md` §2·5；旋转数据见 `ShakeOrientation`。）</summary>
        public const float ShakeHitRotationGain = ShakePresetAmplitude * ShakePresetDirMag;

        /// <summary>原版 preset `Shake Hit Small` 的 `direction = (0, 0.2, 0.2)`
        /// （真件 `d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/Shake Hit Small_4555857231027011253.json`）。
        /// 🔴 **2026-10-23（R9 尾账）起，这一位不再折成一个 `ShakeVerticalFrac` 常量** ——
        /// 屏幕竖直分量改由原版公式**现算**：<see cref="ShakeScreenUpOf"/>。</summary>
        public static readonly Vector3 ShakeHitDirection = new Vector3(0f, 0.2f, 0.2f);

        /// <summary>原版 preset `Shake Land on ground` 的 `direction = (0, −1, 0)`
        /// （真件 `bundle_tweenandshakes_assets_all/MonoBehaviour/Shake Land on ground.json`）。</summary>
        public static readonly Vector3 ShakeLandDirection = new Vector3(0f, -1f, 0f);

        /// <summary>🔴 **原版方向链的唯一实现** —— 屏幕竖直分量由它给（**符号和幅度一起**）。
        ///
        /// 判据（机器码，`资料/普查产出_1023b/R9_rawSignal_算式.md` §2·5）：
        /// `CinemachineImpulseDefinition+LegacySignalSource::GetSignal`（RVA 0xA2B350）
        /// <code>pos = Quaternion.FromToRotation(Vector3.down, m_Velocity) * pos;</code>
        /// 而 `m_Velocity = normalize(preset.direction)`（`CameraShakerManager._DoShake_d__33__MoveNext.c`）。
        /// ⇒ 原始信号里 **−Y 指向 velocity**、+Y 指向 velocity 的反向。
        ///
        /// 我们 2D 只采 `PositionNoise` 的 **Y 轴** ⇒ raw pos = `(0, s, 0)`
        /// ⇒ 旋转后的屏幕竖直分量 = `s × 本函数返回值`。
        ///
        /// ⚠️ 返回值**带幅度**：`±|normalize(direction).y|`，**可以是 0**
        /// （`Shake Forward Pull` 的 `(0,0,2)` ⇒ 0：原版那一下是纯「前后推」，屏幕上不上下晃）。
        /// ⇒ `worldAmp` 里**不再**另乘竖直比例，否则会重复计入（0.7071²）。
        ///
        /// ⛔ **别改回「取 `direction.y` 的符号」**：`FromToRotation(Vector3.down, ·)` 把 **−Y** 转到 velocity
        /// ⇒ 挨打（`direction.y = +0.2`）相机会**朝下**、落地（`−1`）**朝上** —— 与直觉相反，但机器码如此。
        /// 旧实现（`verticalSign = sign(direction.y)`）对这两件的**方向恰好都反着**
        /// （判据 → `R9_落地_执行报告.md` §六·1 那条「仍开着」的尾巴）。</summary>
        public static float ShakeScreenUpOf(Vector3 direction)
        {
            Vector3 v = direction.sqrMagnitude > 1e-12f ? direction.normalized : ShakeHitDirection.normalized;
            return (Quaternion.FromToRotation(Vector3.down, v) * Vector3.up).y;
        }

        /// <summary>原版 **BoardCamera 在棋盘平面**上的像素密度（px / 原版世界单位）。
        /// 推导：相机 z = −13.57、棋盘平面 z ≈ 3.66 ⇒ 距离 17.23；可见高 = 2 × 17.23 × tan(FOV 46.397°/2)
        /// = 14.77 世界单位 = 1080 px ⇒ **73.1 px/单位**。
        /// 🔴 **它和 `OriginalBoardPxPerUnit`(182.14) 不是同一个东西**：那个是**棋盘排版**的密度
        /// （用来折算卡的体型/位移），这个是**相机像平面**的密度（用来折算**相机位移**在屏幕上走多少像素）。
        /// 混用会把震屏幅度算错 2.5 倍。</summary>
        public const float ShakeOriginalPxPerUnit = 73.1f;

        /// <summary>按 `SupportMethods.GetUnitSize` 的档取 preset 的 amplitude 覆盖值（2 / 4 / 8）。
        /// 判据：`CardScript__ResolveAttackAnimationEffects.c:32-52`（三个 `_DAT_` 从 DLL 实读）。
        /// ⚠️ **传 0 与 1 是同一档**（`GetUnitSize` 永不返回 1 —— 见 `UnitSizeOf`）。</summary>
        public static float ShakeAmplitudeForUnitSize(int unitSize)
        {
            return unitSize >= 3 ? ShakeAmpUnitSize3
                 : unitSize == 2 ? ShakeAmpUnitSize2
                                 : ShakeAmpUnitSize1;
        }

        /// <summary>🆕 **某一档的世界单位峰值幅度**（我们这套坐标，**旋转前**的 pos 峰值）——
        /// **由真件字段现算，不再写死**。
        ///
        /// `= ShakeAmplitudeForUnitSize(size) × ShakePresetDirMag × ShakeBandPeak
        ///    × (ShakeOriginalPxPerUnit ÷ OurPxPerUnit)`
        ///
        /// 🔴 **2026-10-23（R9 尾账）**：这里**不再乘竖直比例** —— 那一半改由原版公式
        /// `Quaternion.FromToRotation(Vector3.down, velocity)` 现算（见 `ShakeScreenUpOf`），
        /// 它**自带符号和 `|vy|`**。挨打那件的最终竖直峰值仍是
        /// `0.0570 × 0.7071 × 0.5(信号峰值) = 0.0202`（与 R9 报的「峰值 0.02 = 原版值」逐位一致）。
        ///
        /// 逐项出处与两处软连接见 `ShakeWorldAmplitude` 的注释（那条注释整段仍然成立，
        /// 只是**那个数从此是算出来的**）。
        /// 🔴 与老写法的差别：老的只有第 1 档那一个数（0.04），**第 2/3 档永远用不上**
        ///   —— 而原版是按攻击方的近战档换幅度的（`ResolveAttackAnimationEffects`）⇒ 那是实打实的偏离。
        /// 🔴 **2026-10-23（R9）注意**：`origUnits` 里的 `ShakeBandPeak`(0.149) **不是**信号的真实峰值
        /// （真实峰值 = 0.5·ΣA = 0.0745），这一项**故意保留** —— 它与 `ShakeNoise01` 的 ±0.5 值域
        /// **相抵一次**，合起来才是原版的位移峰值。⛔ 别单独把任何一边「修」成 0.0745。详见 `ShakeBandPeak`。</summary>
        public static float ShakeWorldAmplitudeFor(int unitSize)
        {
            float ampGain = ShakeAmplitudeForUnitSize(unitSize) * ShakePresetDirMag;   // 0/1 档 = 0.5657
            float origUnits = ampGain * ShakeBandPeak;                                // 0/1 档 = 0.0843（**旋转前**的 pos 峰值）
            return origUnits * (ShakeOriginalPxPerUnit / OurPxPerUnit);               // 0/1 档 = 0.0570
        }

        /// <summary>**按攻击方的近战攻击**取震屏的档（`SupportMethods.GetUnitSize`）——
        /// 🔴 **判据只此一处**：`UnitSizeOf`（挨打后坐 `PushBackMagnitude` 用的也是它）。
        /// 传 `&lt; 0` = 不知道攻击方是谁（疲劳 / 反伤那类）⇒ 退回第 1 档，与原版「尺寸 0」同档。</summary>
        public static int ShakeUnitSizeOf(int attackerMeleeAttack)
        {
            return attackerMeleeAttack < 0 ? 0 : UnitSizeOf(attackerMeleeAttack);
        }

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
        /// <summary>🆕 **2026-10-23（R9）**：三条带的 `Constant` = **1 / 0 / 1**
        /// （低 1.9 Hz 与高 55.54 Hz 两条**正弦**、中 9.1 Hz 一条 **Perlin**）。
        /// 判据（**机器码级**，`NoiseSettings+NoiseParams::GetValueAt` RVA 0xA10610）：
        /// `Constant != 0` ⇒ `A×0.5×sin(2πu)`；`== 0` ⇒ `A×(Mathf.PerlinNoise(u, 0) − 0.5)`。
        /// ⛔ **别把 `Constant` 理解成「常数 / 不随机化」** —— tooltip 是那么写的，但机器码是「正弦 vs Perlin」，
        /// 按 tooltip 实现会得到一条**平线**（那是最容易踩的一处）。
        /// 出处真件：`Warpforge 6D Shake.json` 的 `PositionNoise[0..2].Y.Constant`（3 份 `…One Axis` / `…No rotation`
        /// 的 Y 列**逐位相同**）· 算式全文 → `资料/普查产出_1023b/R9_rawSignal_算式.md`。</summary>
        public static readonly int[] ShakeBandConstant = { 1, 0, 1 };
        /// <summary>三条带的振幅之和（= Σ `ShakeBandAmp` = 0.149）—— **归一化分母，不是峰值**。
        ///
        /// 🔴 **2026-10-23（R9）⛔ 别把这个数改成 0.0745**：原版**每条带**的输出是 ±0.5·A
        /// （Perlin 那一支减 0.5、正弦那一支乘 0.5），所以三条带同相时的**真实合峰 = 0.5·ΣA = 0.0745**。
        /// 而 `ShakeWorldAmplitudeFor` 推导时用的「峰值 = ΣA」这一假设**本来就多算了 2×** ⇒
        /// 分母保持 0.149，`ShakeNoise01` 的值域就是 ±0.5，两者相抵后**位移峰值恰好落在原版值上**。
        /// 改成 0.0745 会让屏震只剩原版的一半（判据 → `R9_rawSignal_算式.md` §五·5）。</summary>
        public const float ShakeBandPeak = 0.149f;

        /// <summary>一条信号带（某 octave 的 Y 轴）：`hz` = 原版 `Frequency`、`amp` = `Amplitude`、
        /// `constant` = 原版 `Constant`（**1 = 正弦、0 = Perlin**）。</summary>
        public struct ShakeBandSignal
        {
            public float hz, amp;
            public int constant;
            public ShakeBandSignal(float hz, float amp, int constant)
            {
                this.hz = hz; this.amp = amp; this.constant = constant;
            }
        }

        /// <summary>🆕 **2026-10-23（R9）** 一份 `rawSignal`（原版 `Cinemachine.NoiseSettings`）在 2D 版里的等价物。
        ///
        /// 装两半：
        ///   · **`PositionNoise` 的 Y 轴三条带**（X/Z 不采 —— 2D 只走屏幕竖直，见 `R9_rawSignal_算式.md` §五·6）；
        ///   · 🆕 **`OrientationNoise` 的 X/Y/Z 各三条带**（2026-10-23 R9 尾账接上：原版
        ///     `GetSignal` 里 `rot = Quaternion.Euler(ΣOrientation × π/180)`，见 `ShakeOrientation`）。
        /// 逐带由 `ShakeNoise01` / `ShakeOrientation` 求值（三条带**相加**）。</summary>
        public sealed class ShakeSignal
        {
            public readonly string name;
            readonly ShakeBandSignal[] _bands;
            readonly ShakeBandSignal[] _orientation;   // 轴优先：axis * 3 + octave（0 = X, 1 = Y, 2 = Z）
            readonly int _orientationOctaves;

            public ShakeSignal(string name, ShakeBandSignal[] bands)
                : this(name, bands, null, 0) { }

            /// <param name="orientation">`OrientationNoise` 平铺表，**轴优先**（`axis * octaves + i`）。
            /// `null` / `octaves = 0` ⇒ 这份信号不动角度（原版 `…No rotation` 那一份就是这样）。</param>
            public ShakeSignal(string name, ShakeBandSignal[] bands, ShakeBandSignal[] orientation, int octaves)
            {
                this.name = name ?? "";
                _bands = bands ?? new ShakeBandSignal[0];
                _orientation = orientation ?? new ShakeBandSignal[0];
                _orientationOctaves = Mathf.Max(0, octaves);
            }
            public int Count { get { return _bands.Length; } }
            public ShakeBandSignal this[int i] { get { return _bands[i]; } }
            /// <summary>`OrientationNoise` 每轴几条带（0 = 这份信号不带角度）。</summary>
            public int OrientationOctaves { get { return _orientationOctaves; } }
            /// <summary>取 `OrientationNoise[axis]` 的第 `octave` 条带；越界返回 0 带（不抛）。</summary>
            public ShakeBandSignal Orientation(int axis, int octave)
            {
                int i = axis * _orientationOctaves + octave;
                return (i >= 0 && i < _orientation.Length) ? _orientation[i] : new ShakeBandSignal(0f, 0f, 0);
            }
            public override string ToString() { return name; }
        }

        /// <summary>默认信号 = 原版场景里那台 `CinemachineImpulseSource` 的 `m_RawSignal`
        /// = **`Warpforge 6D Shake`**（pathID 1276427422462347950）。**17 个 preset 里 12 个没有
        /// `rawSignal` ⇒ 全落到它**，而且 4 份信号的 Y 列**逐位相同** ⇒ 2D 竖直波形对全部 17 个都一致，
        /// 差别只在 `frequency`（时间缩放）与幅度/方向。
        ///
        /// 🆕 **2026-10-23（R9 尾账）**：连 **`OrientationNoise`（3 轴 × 3 octave）** 一起装进来。
        /// 数据现读 `Resources/WarpforgeVFX/shake_presets.json` 的
        /// `signals[name=="Warpforge 6D Shake"].orientation[0..2]`（生成器
        /// `工具/gen_shake_presets.py` 从 `Warpforge 6D Shake.json` 的 `OrientationNoise` 抽的）：
        ///   · X: f 5.83/0.09/c1 · 9.17/0.14/c1 · 57.17/0.15/c1
        ///   · Y: f 1.80/0.059/c1 · 11.35/0.041/c1 · 54.17/0.048/c1
        ///   · Z: f 2.38/0.017/c1 · 10.52/0.009/c1 · 63.76/0.016/c1
        /// `Warpforge 6D Shake No rotation` 那一份 **orientation 为空** ⇒ 不动角度
        /// （连名字都写着 no rotation，正好互证）。</summary>
        public static readonly ShakeSignal DefaultShakeSignal = new ShakeSignal("Warpforge 6D Shake",
            new[]
            {
                new ShakeBandSignal(ShakeBandHz[0], ShakeBandAmp[0], ShakeBandConstant[0]),
                new ShakeBandSignal(ShakeBandHz[1], ShakeBandAmp[1], ShakeBandConstant[1]),
                new ShakeBandSignal(ShakeBandHz[2], ShakeBandAmp[2], ShakeBandConstant[2]),
            },
            new[]
            {
                // axis * 3 + octave（X / Y / Z）
                new ShakeBandSignal(5.83f, 0.090f, 1), new ShakeBandSignal(9.17f, 0.140f, 1), new ShakeBandSignal(57.17f, 0.150f, 1),
                new ShakeBandSignal(1.80f, 0.059f, 1), new ShakeBandSignal(11.35f, 0.041f, 1), new ShakeBandSignal(54.17f, 0.048f, 1),
                new ShakeBandSignal(2.38f, 0.017f, 1), new ShakeBandSignal(10.52f, 0.009f, 1), new ShakeBandSignal(63.76f, 0.016f, 1),
            }, 3);

        /// <summary>🔴 震动的**绝对幅度**（世界单位，峰值 = 第 1 档 / 尺寸 1）。
        ///
        /// **怎么推出来的**（每一环都有出处，但**中间有两处软连接**，见下）：
        ///   ① 峰值信号 = Σ 三条带振幅 = **0.149**（同上，三条带在 t=0 同相）
        ///   ② 原版 `m_AmplitudeGain` = `preset.amplitude × |direction|`
        ///      = 2.0（`GetUnitSize` 第 1 档覆盖值）× |(0, 0.2, 0.2)|(0.28284) = **0.5657**
        ///   ③ 传给 `GenerateImpulseWithVelocity` 的 velocity = `normalize(direction)` = (0, 0.7071, 0.7071)
        ///      ⇒ 屏幕竖直分量 = `ShakeScreenUpOf(direction)` = **−0.7071**（**含符号**，见那个方法）
        ///   ④ 原版相机位移（世界单位）= 0.5657 × 0.149 × 0.7071 = **0.0596**
        ///      （`0.149` 与 `ShakeNoise01` 的 ±0.5 值域相抵一次 ⇒ 真实峰值 0.0202 我们的世界单位）
        ///   ⑤ 换成屏幕像素：原版 BoardCamera 在**棋盘平面**（z≈3.66；相机 z=−13.57 ⇒ 距离 17.23）
        ///      的可见高 = 2 × 17.23 × tan(FOV 46.397/2) = **14.77 世界单位** = 1080 px
        ///      ⇒ **73.1 px / 世界单位**；0.0596 × 73.1 = **4.36 px** @1080p
        ///   ⑥ 换成本工程的世界单位（10 世界单位 = 屏高 = 1080 px ⇒ **108 px/单位**）
        ///      ⇒ 4.36 ÷ 108 = **0.0404** ⇒ 本常量 = **0.0570（旋转前）× 0.7071（竖直分量）= 0.0404**
        ///
        /// 🔴 **2026-10-23（R9 尾账）**：本常量现在是**旋转前**的 pos 峰值（0.057，= `ShakeWorldAmplitudeFor(0)`），
        /// **不再**含 `ShakeVerticalFrac` —— 竖直分量与符号由 `ShakeScreenUpOf` 按原版
        /// `FromToRotation(Vector3.down, velocity)` 现算。⚠️ 挨打那一下相机的实际峰值位移**没变**（0.0202），
        /// 变的是**方向**：原版 `direction = (0,+0.2,0.2)` ⇒ **朝下**（旧实现取「向上」是错的）。
        ///
        /// ⚠️ **两处软连接（照这个数用之前先知道）**：
        ///   ① ②→④ 那一步按的是 **Cinemachine「Legacy impulse：位移 = velocity × AmplitudeGain × 信号」**
        ///      这条公式 —— 🔴 **2026-10-23 起已从机器码坐实**（`LegacySignalSource::GetSignal` RVA 0xA2B350，
        ///      见 `R9_rawSignal_算式.md` §2·5），不再是旁证；
        ///   ② 「世界 y 分量 = 屏幕竖直、z 分量在屏幕上不动」是**我们的换算**（见文件头「3D → 2D 的迁移」）。
        /// ⚠️ 至于「向上还是向下」，**已由机器码定案**：raw **−Y 指向 velocity** ⇒ 见 `ShakeScreenUpOf`。
        /// ✅ **2026-10-14（`A1767` / #10）**：「原版 `m_Randomize: 1` ⇒ 每次抖的相位随机」**已经从
        ///   本地资产坐实**（原来这里写「没有复刻」）—— 真件 = 场景里那台 `CinemachineImpulseSource`
        ///   的 `m_ImpulseDefinition`（battlearena1 `MonoBehaviour_4914.json`）：
        ///   `m_ImpulseType = 3`（**Legacy**）· `m_RawSignal` = `Warpforge 6D Shake` ·
        ///   `m_RepeatMode = 0`（Stretch）· `m_ImpulseDuration = 1.0` · `m_AmplitudeGain = 0.1` ·
        ///   `m_FrequencyGain = 0.05` · **`m_Randomize = 1`**。而 `m_Randomize` 的语义写在
        ///   **本地签名桩自己的 Tooltip 上**（`d:/2/Warpforge_code/Scripts/Cinemachine/Cinemachine/
        ///   CinemachineImpulseDefinition.cs`）：**「Legacy mode only: Randomize the signal start time」**
        ///   ⇒ 它随机的**不是方向、也不是幅度，是「从噪声信号的哪一刻开始播」**。
        ///   ⇒ 复刻方式见下面的 `ShakePhaseOffset` / `ShakeNoise01`。
        ///   🔴 **2026-10-23（R9）订正**：这句原写「**Perlin** + 每次随机的起播时刻」—— 波形**不是全 Perlin**：
        ///   默认信号的三条带是 **sin / Perlin / sin**（`Constant` = 1/0/1），算式已从机器码坐实
        ///   （→ `资料/普查产出_1023b/R9_rawSignal_算式.md`）。
        /// 🔴 **2026-10-14（`A1767` / #10）幅度改成按档现算**：本常量**只是「尺寸 0/1」那一档**
        ///   （= `ShakeWorldAmplitudeFor(0)` 四舍五入到两位），**运行时该走 `ShakeWorldAmplitudeFor`**
        ///   （按攻击方的近战档取 2 / 4 / 8）。留它是因为 `BattleDriver` 那两处调用点还在用它
        ///   —— ⚠️ **那两处是待接的一半**（见 `ShakeCameraForHit` 的注释）。</summary>
        public const float ShakeWorldAmplitude = 0.057f;

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
        /// 到 1.8333 归 0；`DamageIcon.m_Color.a` 0→0.8235 @0→0.1167。
        /// 🔴 **2026-10-21（第六十会话）**：`DamageIcon`（红色血痕）**已接** —— `PopNumber` 从效果库的
        /// `CardPrefab` 取 `DamageIcon` 节点，按原版 offset/scale/sortingOrder（1 &lt; 数字 2）挂进同一条时间线。</summary>
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
            // 🆕 2026-10-23（`W63` 收口 ②）：方向那一半的「两点重合」阈值
            P("PushBackDegenEpsilon", PushBackDegenEpsilon, Src.Field,
              "`CardScript__ReceiveAttackAnim.c` 的 `if (DAT_1834b2f44 < len)`（比的是**长度**不是平方）；"
            + "DLL `_DAT_1834b2f44` 4 字节 float 实读 = **1e-5**"),

            // ---- 🆕 2026-10-23（`新账-71`）：近战位移的**深度 → 缩放**曲线 ----
            //   原件 `MinionScaleByPosition.json`（`assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/`）；
            //   消费点 = `CardScript.__c__DisplayClass357_0___AttackMeleeAnim_b__0.c` 的逐帧回调 → `MinionScaleByPositionSO.GetScale`
            P("DepthScaleKey0Time", DepthScaleKey0Time, Src.Field,
              "`MinionScaleByPosition.scaleCurve` 第 0 键 time（原版 SO 实读 0.002811431884765625）"),
            P("DepthScaleKey0Value", DepthScaleKey0Value, Src.Field,
              "同上第 0 键 value = 0.36000001430511475（我方那一行）"),
            P("DepthScaleKey1Time", DepthScaleKey1Time, Src.Field,
              "同上第 1 键 time = 7.699999809265137（= 两行 z 差 7.698）"),
            P("DepthScaleKey1Value", DepthScaleKey1Value, Src.Field,
              "同上第 1 键 value = 0.699999988079071（敌方那一行；敌方 `desiredScale` 0.69 是另一回事，差 1.4% 别混）"),

            // ---- 攻击：Recoil Normal Tween（UnitTweenSO）----
            P("AttackPunchUnits", AttackPunchUnits, Src.Field, "`Recoil Normal Tween` 的 `punch=(0,0,-0.1)`"),
            P("AttackPunchDuration", AttackPunchDuration, Src.Field, "`Recoil Normal Tween` 的 `duration=0.3`"),
            P("AttackPunchVibrato", AttackPunchVibrato, Src.Field, "`Recoil Normal Tween` 的 `vibratto=10`"),
            P("AttackPunchElasticity", AttackPunchElasticity, Src.Field, "`Recoil Normal Tween` 的 `elasticity=1.0`"),

            // ---- 命中：幅度借 Impact，旋转借 Impact ----
            P("HitPunchUnitsLight", HitPunchUnitsLight, Src.Ours, "`Impact *` 是**另一套机制**（AnimFX 的 UnitTweenSO）；原版的挨打幅度走 `GetUnitSize × GetPushBackFactor`，**那两个方法体没被反编译** → 借它的 0.1 顶替"),
            P("HitPunchUnitsHeavy", HitPunchUnitsHeavy, Src.Ours, "同上（`Impact Heavy Tween` 的 0.3）"),
            P("HitRotLightDeg", HitRotLightDeg, Src.Derived, "`Impact Light Tween` 的 3D 旋转 punch `(-3,-3,0)` → **取模长当 2D 的 z 度数**（原版是 x/y 两个轴）。⚠️ **2026-10-22（`B02`）起 `HitReact` 不再用它**（近战挨打没有旋转），只为「远程弹着」那一族留着"),
            P("HitRotHeavyDeg", HitRotHeavyDeg, Src.Derived, "`Impact Heavy Tween` 的 `(-15,4,0)` 模长（同上，已不参与近战挨打）"),
            P("HitRotDuration", HitRotDuration, Src.Field, "`Impact Light Tween` 旋转 punch 的 `duration=0.5`。⚠️ 现在**唯一的消费者是 `EventTiming.DurationOf(Hit)` 的时长口径**，不代表「转了 0.5 秒」"),
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
            P("ShakeAmpUnitSize2", ShakeAmpUnitSize2, Src.Field, "同上 `_DAT_1834b2df0` = **4.0**（尺寸 2）—— 🔴 **2026-10-14 订正**：原来这里写「我们接不上（我们的单位不占多格）」**是错的**，`GetUnitSize` 的判据是**近战攻击**（`SupportMethods__GetUnitSize.c:11-16`），我们本地就有（`UnitSizeOf`）⇒ **接得上**，见 `ShakeWorldAmplitudeFor`"),
            P("ShakeAmpUnitSize3", ShakeAmpUnitSize3, Src.Field, "同上 `_DAT_1834b2e00` = **8.0**（尺寸 3）—— 🔴 **同上，2026-10-14 订正：也接得上**"),
            P("ShakePresetAmplitude", ShakePresetAmplitude, Src.Field, "preset 真件 `Shake Hit Small` 的 `preset.amplitude = 2.0`（`d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/Shake Hit Small_4555857231027011253.json`）"),
            P("ShakePresetDirMag", ShakePresetDirMag, Src.Field, "同一个真件的 `preset.direction = (0,0.2,0.2)` 的模长 0.28284271 —— 从 `ShakeWorldAmplitude` 那个**乘积** 0.5657 里拆出来（拆开才能按档换 amplitude）"),
            P("ShakeHitRotationGain", ShakeHitRotationGain, Src.Field, "原版 `m_AmplitudeGain` = `preset.amplitude × |direction|` = 2.0 × 0.28284 = 0.5657 ——**位置与角度共用同一个 gain**（`LegacySignalSource::GetSignal`：`pos *= gain` 与 `rot = SlerpUnclamped(identity, rot, gain)`）；🔴 2026-10-23（R9 尾账）起 **`ShakeVerticalFrac` 已删**：屏幕竖直分量与符号改由 `ShakeScreenUpOf`（= 原版 `FromToRotation(Vector3.down, velocity)`）现算"),
            P("ShakeOriginalPxPerUnit", ShakeOriginalPxPerUnit, Src.Derived, "原版 BoardCamera **在棋盘平面**的像素密度 73.1 px/单位（2×17.23×tan(46.397/2)=14.77 单位=1080px）。⚠️ **别和 `OriginalBoardPxPerUnit`(182.14) 混** —— 那个是棋盘排版密度，这个是相机像平面密度"),
            P("ShakePhaseRange", ShakePhaseRange, Src.Field, "噪声的**起播时刻**取值范围（信号时间轴秒）= `Random.Range(−1000, +1000)`（`CinemachineImpulseDefinition+LegacySignalSource` ctor RVA 0xA2B5E0；两个常量 `c47a0000`/`447a0000` 从 GameAssembly.dll 实读）。⚠️ 原写「取 100 s 是我们的（Cinemachine 没写范围）」—— 2026-10-23 机器码解出后**已改成原版值**；唯一保留的代理是**用 `System.Random` 派生**（可复现红线）"),
            P("ShakeSeedBaseDefault", ShakeSeedBaseDefault, Src.Ours, "相位种子的出厂基线（固定常量 ⇒ 同一串事件给同一串相位 = 可复现）。⛔ 不是原版值。⚠️ **只在「没接锚点」的场合生效** —— 真机开局由 `AnchorShakeSeedToBattle(对局种子)` 覆盖它（`A1892`，落地那一行在 `BattleDriver.Begin`）"),
            P("ShakeFrequencyGain", ShakeFrequencyGain, Src.Field, "`Shake Hit Small` 的 `frequency=0.05` → Cinemachine `m_FrequencyGain`（信号时间轴缩放，0.05 = 放慢 20 倍），乘在 `ShakeBandHz` 上"),
            P("ShakeBandPeak", ShakeBandPeak, Src.Field, "`Warpforge 6D Shake`(Cinemachine `NoiseSettings`) 的 `PositionNoise[*].Y.Amplitude` 三条带之和 = 0.059+0.040+0.050；拿去归一化，让 `ShakeWorldAmplitude` 表示**峰值**"),
            P("ShakeWorldAmplitude", ShakeWorldAmplitude, Src.Derived, "**只在「尺寸 0/1」这一档** = `ShakeWorldAmplitudeFor(0)` 的近似：0.149(带峰) × 0.5657(`amplitude × |direction|`) = 0.0843 原版世界单位（**旋转前**；竖直分量 0.7071 由 `ShakeScreenUpOf` 现算）→ 按 73.1 px/单位 → ÷108 = **0.0570**。🔴 **2026-10-14 起运行时应走 `ShakeWorldAmplitudeFor(size)`**（2/4/8 三档），本常量只是兜底。⚠️ 剩下的一处软连接 = 「世界 y 分量 = 屏幕竖直」是我们的换算（见常量注释）"),

            // ---- 🆕 2026-10-16 落地那一下：preset 真件 `Shake Land on ground` ----
            //   （消费点 `CardScript__LandOnGround.c:26-29`；见 `ShakeCameraForLand`）
            P("ShakeLandPresetAmplitude", ShakeLandPresetAmplitude, Src.Field, "preset 真件 `Shake Land on ground` 的 `preset.amplitude = 2.0`（`bundle_tweenandshakes_assets_all/MonoBehaviour/Shake Land on ground.json`）—— ⚠️ 与挨打那件**同值**，但卡预制体 `shakeWhenLand` 的三个 `overwrite*` 全是 0 ⇒ **不会被尺寸档覆盖**（`GetModifiedPreset` 原样用 preset）"),
            P("ShakeLandPresetDirMag", ShakeLandPresetDirMag, Src.Field, "`Shake Land on ground` 的 `direction = (0,−1,0)` 的模长 = 1.0（挨打那件 0.28284，这就是两者幅度差 3.5 倍的来源）。🔴 2026-10-23（R9 尾账）：`ShakeLandVerticalFrac` **已删** —— 竖直分量与**符号**改由 `ShakeScreenUpOf((0,−1,0)) = +1（朝上）` 现算"),

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
            P("AfterAttackDelay", AfterAttackDelay, Src.Field,
              "🆕 新账-73 远程收招**第二段**（`CardScript.AfterAttack`）的 `SetDelay` = "
            + "`.rdata 0x1834b315c` 从 `GameAssembly.dll` 实读 **0.65**"
            + "（同一地址在 `FatigueCard._DrawFatigue_d__7:64-70` 当 0.65 的字面量用，两处互证）"),

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
            P("HandToBoardFrameOff", HandToBoardFrameOff, Src.Field, "同上第二条关键帧（0.75 关掉卡框）"),
            P("HandToBoardArtY", HandToBoardArtY, Src.Field, "clip 的 `CardImage.m_LocalPosition.y`（只有 0 s 一个关键帧）"),
            P("HandToBoardFaceZ", HandToBoardFaceZ, Src.Field, "clip 的 `2DCard.m_LocalPosition.z` 终点（0.1667）"),
            P("HandToBoardArtShrinkStart", HandToBoardArtShrinkStart, Src.Field, "clip 的 `CardImage` ScaleCurves 第二条关键帧时刻"),
            P("HandToBoardFrameDissolveStart", HandToBoardFrameDissolveStart, Src.Field,
              "clip 的 `floatProperty1.value`（`_DissolveAmount`）第一条关键帧 = 0.0167（0→0.0167 恒 1）"),
            P("HandToBoardSpawnParticlesOn", HandToBoardSpawnParticlesOn, Src.Field,
              "clip 的 `ToBoardFX/Spawn Particles.m_IsActive` 0→1 关键帧 = 0.0167"),
            P("HandToBoardTrailOn", HandToBoardTrailOn, Src.Field,
              "clip 的 `ToBoardFX/Spawn Particles/Trail.m_IsActive` 0→1 关键帧 = **0.55**"
              + "（与 AnimationEvent `CardHandToBoardAnimationFinished` 同刻）"),
            P("HandToBoardFadeStart", HandToBoardFadeStart, Src.Field, "clip 的 `2DCard.m_Alpha` 关键帧"),
            P("HandToBoardFadeEnd", HandToBoardFadeEnd, Src.Field, "同上"),
            P("HandToBoardDone", HandToBoardDone, Src.Field, "clip 的 AnimationEvent `CardHandToBoardAnimationFinished` 时刻"),
            P("HandToBoardEnd", HandToBoardEnd, Src.Field, "clip 最后一条关键帧（0.9167）"),
            P("HandToBoardDissolveEnd", HandToBoardDissolveEnd, Src.Field, "clip 的 `material._DissolveAmount` 终点"),
            P("HandToBoardShrink", HandToBoardShrink, Src.Field, "clip 的 `CardImage` ScaleCurves 终点"),
            P("HandToBoardBodyY0", HandToBoardBodyY0, Src.Field, "clip 的 `Board Elements.m_LocalPosition.y` 起始关键帧（0 s）"),
            P("HandToBoardBodyY1", HandToBoardBodyY1, Src.Field, "同上，0.1667 / 0.7 两个关键帧的低点"),
            P("DissolveTime", DissolveTime, Src.Derived, "`HandToBoardDissolveEnd − HandToBoardLand`（⚠️ **只留给「将来要做出战侧溶解」** —— 阵亡侧 2026-09-18 起改用下面那两个原版字段）"),
            P("DeathDissolveMinion", DeathDissolveMinion, Src.Field, "`VarsGlobal.deathTimeMinionDuration` = 0.2（另有两个同值字段 `minionDeathTime` / `timeToDissolveCard`）"),
            P("DeathDissolveWarlord", DeathDissolveWarlord, Src.Field, "`VarsGlobal.deathTimeWarlordDuration` = 0.5（督军那档；出处 `资料/VarsGlobal_原版数值.md`）"),
            P("DeathHoldBeforeFx", DeathHoldBeforeFx, Src.Field,
              "阵亡第①拍末尾的等待 = `UnitDeath` state 0 的 `WaitForSeconds(gVars + 0x34)` = `deathTimeWarlordDuration` **0.5**"
              + "（`CardScript._UnitDeath_d__446__MoveNext.c:120-122`，**不分档**）"),
            P("DeathFxHold", DeathFxHold, Src.Field,
              "爆散体生成之后那一拍 = `WaitForSeconds(gVars + 0x8c)` = `minionDeathTime` **0.2**"
              + "（`CardScript._UnitDeath_d__446__MoveNext.c:377-379`）"),
            P("CemeteryCardX", CemeteryCardX, Src.Field, "`VarsGlobal.cardInCemeteryPos`（0x98，Vector2）的 x = −8.14（`CardScript__SetAsCemetery.c:35-41`，**瞬移**）"),
            P("CemeteryCardY", CemeteryCardY, Src.Field, "同上的 y = −0.2"),
            P("CemeteryCardScale", CemeteryCardScale, Src.Field, "`VarsGlobal.cardInCemeteryScale` = 0.65（`CardScript__SetAsCemetery.c:66-78`，**绝对**缩放不是乘）"),
            P("CemeteryGrowTime", CemeteryGrowTime, Src.Field, "`VarsGlobal.growInCemeteryTime` = 0.3（`CardScript__SetAsCemetery.c:75-77`，`SetEase(9)` = OutCubic）⚠️ 那条补间在原版被紧跟着的 `SetActive(false)` 盖掉、画面上看不见"),

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
            P("TurnCardAroundDeg", TurnCardAroundDeg, Src.Field,
              "抽牌翻面的**单段**角度 90° —— `.rdata 0x1834b2e04` 用 `工具/read_literal.py` 实读 90.0"
              + "（`CardScript__TurnCardAround.c:30-32`；同址也是 `AttackTurnDeg` / `DeathShakeRandomness` 的出处）"),
            P("TurnCardAroundFrac", TurnCardAroundFrac, Src.Field,
              "翻面每段占整次抽牌时长的 0.25 —— `.rdata 0x1834b2e7c` 实读 0.25"
              + "（`CardScript__TurnCardAround.c:32` + `CardScript__FinishCardDrawRotation.c:23`）"),
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
        //  ⇒ **整条 0.4 s、命中在 t = 0.1 s**（收招那 0.3 s 由**挨打动画**覆盖；
        //     命中帧在时间线上的位置 = `EventTiming.HitFrameLag`，伤害结算 = `EventTiming.MeleeDamageLag`）。
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

        /// <summary>🆕 2026-10-23（战斗线 · **新账-73**）：**远程收招【第二段】之前那段等待 = 0.65 s**。
        /// <para>判据 = `decomp_full/CardScript__AfterAttack.c`：
        /// `DOLocalMove(transform, 静止位, attackStepTime)` 之后紧跟
        /// `TweenSettingsExtensions.SetDelay&lt;object&gt;(tween, DAT_1834b315c, DAT_1842d0b20)`。</para>
        /// <para>那个常量**不是**「查不到」：`GameAssembly.dll` 的 `.rdata` `0x1834b315c` 4 字节 float
        /// **实读 = 0.65**（ImageBase `0x180000000` → PE 节表换文件偏移，同 `CLAUDE.md` §三 那套读法）。
        /// 互证：同一地址在 `FatigueCard._DrawFatigue_d__7__MoveNext.c:64-70` 里被当成
        /// `DOScale` 的目标向量分量（`local_48 = (0.65, 0.65, …)`）用 ⇒ 它就是一个 0.65 的字面量。</para>
        /// <para>⚠️ 与 `AttackStepTime` 的关系：两段**串联**（本段等待 → 再 0.1 s 位移）⇒
        /// `AttackRecover` 的整条序列 = `0.1 + 0.65 + 0.1 = 0.85 s`。</para></summary>
        public const float AfterAttackDelay = 0.65f;

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

        // ── 🔴 2026-10-23（战斗线 · 新账-71）：**按「战场深度」重算缩放**（原版 `MinionScaleByPositionSO`）──
        //
        //  原版**任何**棋盘位移都在 Sequence 的 `OnUpdate` 里**每帧**重写一次 `localScale`：
        //    · 回调本体 `decomp_full/CardScript.__c__DisplayClass357_0___AttackMeleeAnim_b__0.c`（近战）与
        //      `CardScript___AfterAttack_b__370_0.c`（收招）**逐字同形**：
        //        `scale = so.GetScale(isPlayer, transform.localPosition, defaultScale); transform.localScale = scale`
        //    · `MinionScaleByPositionSO__GetScale.c` 逐句：
        //        x = isPlayer ? localZ : (curve.lastKey.time + localZ)    ← localZ = **相对自己那一行**（`MinionManager` 局部 z）
        //        v = curve.Evaluate(x)                                     ← 两端 Clamp（`m_PreInfinity/m_PostInfinity = 2`）
        //        scale = 基准(Vector3.one) × v × (defaultScale.x ÷ 末端键值)
        //      末端 = **isPlayer 取 keys[0]、敌方取 keys[last]**（反编译 `LAB_180935561` 那两跳）
        //      ⇒ **在自家那一行上系数恒 1**，在对面那一行上系数 = 曲线两端之比。
        //    · 曲线真件 `d:/2/解包整理/08_预制体特效/战斗预制体/MonoBehaviour/MinionScaleByPosition.json`：
        //        (0.002811431884765625, 0.36000001430511475) → (7.699999809265137, 0.699999988079071)，两键斜率相等 ⇒ 直线。
        //      🔑 横轴 7.70 与两行 `MinionArea` 的 z 差自洽：`EnemyZ − PlayerZ` = 1.043 − (−6.655) = **7.698**
        //      ⇒ 这条曲线就是「自己这行 → 对面那行」的深度→缩放表，目的 = **抵消透视、屏幕上看起来大小不变**。
        //  ⚠️ 我们原来**一次都没算** ⇒ 近战时卡在 0.1 s 内视觉缩到约 55%、再用 0.2 s 涨回来
        //     （= 用户 2026-10-23 真机点名的「速度很快 / 细节上有点问题 / 不是原版的样子」）。
        //
        //  ⚠️ **z 用【世界】z**：我们卡父节点是 `Battle`（`boardRoot` = `sceneRoot`，恒在原点、缩放 1）；
        //     而原版的 localZ 是「相对 `MinionManager`（= 自己那一行）」⇒ 这里用 `worldZ − 自己那行的 z` 现算。
        //  ⚠️ **只有活在 3D 战场上的卡才吃这条**：2D 兜底布局的卡全在 HUD 正交平面（z = 0），
        //     照这条曲线算出来恒 = 1.75×（那是纯错）。门 = 挂没挂 `ArenaSlots.ArenaLayer`
        //     —— `BattleDriver.SyncBoard` 正是按 `use3DBoard` 决定挂不挂（同族那条 `★ A849` 钉着）。

        /// <summary>`MinionScaleByPosition.scaleCurve` 第 0 键的 time —— 原版那份 SO **实读值**，⛔ 别「顺手取整」。</summary>
        public const float DepthScaleKey0Time = 0.002811431884765625f;
        /// <summary>第 0 键的 value（= 我方场上卡 `desiredScale` 0.36）</summary>
        public const float DepthScaleKey0Value = 0.36000001430511475f;
        /// <summary>第 1 键的 time（= 两行 z 差 7.698）</summary>
        public const float DepthScaleKey1Time = 7.699999809265137f;
        /// <summary>第 1 键的 value（曲线给 0.70；敌方 `desiredScale` 是 0.69，两者差 1.4%，别混）</summary>
        public const float DepthScaleKey1Value = 0.699999988079071f;

        /// <summary>曲线求值（`AnimationCurve.Evaluate` 的两端 Clamp 语义）。两键斜率相等 ⇒ 就是直线。</summary>
        public static float DepthScaleCurve(float x)
        {
            return Mathf.Lerp(DepthScaleKey0Value, DepthScaleKey1Value,
                              Mathf.Clamp01((x - DepthScaleKey0Time) / (DepthScaleKey1Time - DepthScaleKey0Time)));
        }

        /// <summary>深度 → **相对**缩放系数（自家那一行上恒 = 1）。`worldZ` 传卡的**世界** z。</summary>
        public static float DepthScaleFactor(float worldZ, bool isPlayer)
        {
            float rowZ = isPlayer ? ArenaSlots.PlayerZ : ArenaSlots.EnemyZ;   // 自己那一行（`MinionArea` 的 z）
            float localZ = worldZ - rowZ;                                     // 原版的 localPosition.z
            float x = isPlayer ? localZ : DepthScaleKey1Time + localZ;        // 原版 `GetScale` 那一句，逐字
            float homeX = isPlayer ? 0f : DepthScaleKey1Time;                 // 自家那一行 ⇒ localZ = 0
            return DepthScaleCurve(x) / DepthScaleCurve(homeX);
        }

        /// <summary>这张卡是不是活在 **3D 战场**里（那一档才有「深度」可言）。
        /// 判据 = 挂没挂 `ArenaSlots.ArenaLayer` —— `BattleDriver.SyncBoard` 正是按 `use3DBoard` 决定挂不挂。</summary>
        public static bool IsArenaCard(CardView card)
        {
            return card != null && card.gameObject.layer == ArenaSlots.ArenaLayer;
        }

        /// <summary>「这张卡的静止位在哪一侧」—— 给没有 `isPlayer` 可传的调用点兜底（我们 `CardView` 不带阵营字段，
        /// 阵营只存在于 `BattleDriver` 的两个字典里）。判据 = 静止位离哪一行近：两行 z 差 7.698，结构上不会误判
        /// （督军另有 `heroExtraOffset` 0.42 / 0.75，离本行仍近一个数量级 → 也判得对）。</summary>
        public static bool IsPlayerRow(float worldZ)
        {
            return Mathf.Abs(worldZ - ArenaSlots.PlayerZ) <= Mathf.Abs(worldZ - ArenaSlots.EnemyZ);
        }

        /// <summary>把「卡当前世界 z」对应的深度缩放写给这张卡（**经 `CardView.SetBaseScale` 的口**）。
        /// <param name="baseScale">它**在自己那一行**上的静止缩放 —— 序列开始时抓一次。</param>
        /// ⛔ **别拿 `card.BaseScale` 当 baseScale**：那个值每帧都被本方法改写着 ⇒ 会复利放大（静默）。</summary>
        public static float ApplyDepthScale(CardView card, bool isPlayer, float baseScale)
        {
            if (card == null) return baseScale;
            float s = baseScale * DepthScaleFactor(card.transform.position.z, isPlayer);
            card.SetBaseScale(s);
            return s;
        }

        /// <summary>给一条「棋盘位移补间」挂上**逐帧**深度缩放（原版 `Sequence.OnUpdate` 的等价物），
        /// 并在收尾把静止缩放写回去（原版序列跑完就停在最后一帧；我们显式复位，见本条报告「残余不确定」）。
        /// <para>`isPlayer` 传 null ⇒ 按静止位离哪一行近来判（<see cref="IsPlayerRow"/>）。</para>
        /// <para>`activeFrom` &gt; 0 ⇒ **前 `activeFrom` 秒一个字节都不写**（原版 `AfterAttack` 那条序列是
        /// 「先 `SetDelay(0.65)` 再跑 0.1 s」，而 `MeleeAttack` / `Reassemble` 是整条都算 ⇒ 默认 0 = 老行为）。
        /// 🧨 少了这道门 ⇒ 收招第一段那个缩放补间会和本钩子**逐帧抢同一个 `_baseScale`**（静默）。</para>
        /// <para>`restingScale` = 「这一侧静止该多大」的**基础缩放**；传 null ⇒ 抓钩子那一刻的
        /// `card.BaseScale`（`MeleeAttack` / `Reassemble` 就是这一档）。</para>
        /// ⚠️ 本方法自己**不做 3D 判定之外的决定**：不在 3D 战场上的卡**一个字节都不写**。</summary>
        static void HookDepthScale(Tween t, Transform tr, bool? isPlayer,
                                   float activeFrom = 0f, float? restingScale = null)
        {
            var card = tr != null ? tr.GetComponent<CardView>() : null;
            if (t == null || card == null || !IsArenaCard(card)) return;
            bool player = isPlayer ?? IsPlayerRow(tr.position.z);
            float baseScale = restingScale ?? card.BaseScale;   // 静止缩放：**只在这里抓一次**（逐帧改的是它乘出来的值）
            t.OnUpdate(() =>
            {
                if (card == null) return;
                // ⚠️ `Elapsed(false)` 要**排掉 `SetDelay`** —— 否则「钩子挂的那条 tween 自己被延迟」时
                //    这道门会整体往后飘（原版的 0.65 在 tween 内部，不在外面）。
                if (activeFrom > 0f && t.Elapsed(false) < activeFrom) return;
                ApplyDepthScale(card, player, baseScale);
            });
            // OnKill 在「正常跑完 autoKill」与「被打断」两种情况下都会来 ⇒ 复位写这一处就够。
            t.OnKill(() => { if (card != null) card.SetBaseScale(baseScale); });
        }

        /// <summary>近战攻击的整条序列（段1～段5）。`endPos` 由 <see cref="MeleeEndPos"/> 算好传进来；
        /// `home` = 出手前那个**静止位**（局部坐标）—— 段4 回的就是它，**不是出手那一帧的位置**。</summary>
        /// <param name="isPlayer">有没有「我方」—— 原版是卡自己的 `isPlayer` 字段（`CardScript+0x40`）。
        /// 我们 `CardView` 不带阵营 ⇒ 不传就按静止位离哪一行近来判（<see cref="IsPlayerRow"/>）；
        /// 调用点知道得准就显式传（⛔ 别在别处再写第二份推断）。</param>
        public static Tween MeleeAttack(Transform tr, Vector3 endPos, Vector3 home, float delay = 0f,
                                        bool? isPlayer = null)
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
            // 🆕 2026-10-23（新账-71）：**逐帧按战场深度重算缩放**（原版那条 `seq.OnUpdate`）。
            //   ⛔ 别把它写成 `tr.DOScale(...)` —— 原版不是补间、是每帧按**当前**位置现算的。
            HookDepthScale(seq, tr, isPlayer);
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

        /// <summary>挨打：**只有位置弹一下**（原版近战挨打就是这一条）。
        ///
        /// 🔴 **2026-10-22（W64 · `B02`）就地订正（铁律 5 + 用户「不要兜底」口径）**：
        /// 这里原来还有**第二条补间：旋转 punch**，取自 `Impact Light Tween` 的 `punch(-3,-3,0)`。
        /// **那是机制错位、已删**：
        /// · 原版**近战**挨打只有位置 —— `decomp_full/CardScript__ReceiveAttackAnim.c:27-28,80-92`：
        ///   整条方法体里只有一句 `DOPunchPosition(dir × GetPushBackFactor(GetUnitSize(攻击方)),
        ///   **0.4**, **8**, **0.3**)`，**没有任何旋转**；同形的第二处入口
        ///   `BattleManager._ResolveUnitShake_d__472__MoveNext.c:139-147` 也只有位置（互为佐证）。
        /// · 被错当成「重击旋转」的 `Impact Light/Heavy Tween` 是 **AnimFX 的远程弹着模块**
        ///   （`animfx_modules.json`：引用 `Impact Light Tween` 的有 **90 件**效果，全是
        ///   `BulletImpact_*` / `Atk_*` 这类弹着；而近战那件 `AttackHitSmall` 的模块里**没有** tween 模块）
        ///   ⇒ 它是**命中特效**那一侧的东西，不是近战挨打。
        ///
        /// 形状（位置那一条，全部照原版）：
        /// · 方向 = `normalize(受击方.pos − 攻击方.pos)`（压到屏幕平面，见 `Flat`）；
        /// · **幅度** = `GetPushBackFactor(GetUnitSize(攻击方))` = 0.5 / 1.0 / 1.5（<see cref="PushBackMagnitude"/>，
        ///   判据 = `SupportMethods__GetUnitSize.c:11-16` / `__GetPushBackFactor.c:5-11`，两个方法体本地都在）；
        /// · 时长 **0.4** / vibrato **8** / 弹性 **0.3**（上引 `.c` 的调用点，前两个是 `.rdata` 常量）。
        /// ⚠️ 原版**没有 `SetEase`** ⇒ 吃 DOTween 全局默认；本工程 `DOTweenSettings.asset:31`
        ///   `defaultEaseType = 6`（OutQuad）⇒ 保留 `Ease.OutQuad`（这一句与原来一致）。
        /// </summary>
        public static void HitReact(Transform tr, Vector3 away, bool heavy = false, float delay = 0f)
            => HitReact(tr, away, heavy, delay, -1);

        /// <param name="attackerMeleeAttack">**攻击方**的当前近战攻击（`UnitState.Attack`）。
        /// **传 -1 = 不知道攻击方是谁**（疲劳、反伤那类没有攻击方的伤害）⇒ 退回 `Impact *` 那套顶替值
        /// （= 审计 A06 那一条合法例外：原版这条输入不存在）。
        /// 判据与出处见 <see cref="PushBackMagnitude"/>：原版的幅度**只由攻击方的近战档决定**。</param>
        public static void HitReact(Transform tr, Vector3 away, bool heavy, float delay, int attackerMeleeAttack)
        {
            if (tr == null) return;
            PunchBack(tr, Flat(away), heavy, delay, attackerMeleeAttack);
        }

        // ==================================================================
        //  🆕 2026-10-23（`W63` 收口 ②）：**受击方向按「自己 − 攻击者」算**
        // ==================================================================
        //  原版判据（逐句，`d:/2/tools/decomp_full/CardScript__ReceiveAttackAnim.c`）：
        //    `dx = 受击方.position.x − 攻击方.position.x` · `dz = 受击方.z − 攻击方.z` ·
        //    `len = sqrt(dx² + dz²)`；`len > DAT_1834b2f44(=**1e-5**)` 时
        //    `dir = (dx/len, **0**, dz/len)`；否则取一条**静态常量向量**（`DAT_1842da2a8 + 0xb8` 那一格，
        //    反编译里读不出名字）⇒ 然后 `DOPunchPosition(dir × GetPushBackFactor(GetUnitSize(攻方)), 0.4, 8, 0.3)`。
        //  🔴 **我们换了一条轴**：原版棋盘是 XZ 地面（y 是高度），我们的棋盘画在 **XY**（z 是景深）
        //    ⇒ `dir` 取 `(dx, dy, 0)`（与 `Flat` 同一个平面）。这是**换算**，不是原版原样，如实记。
        //  ⚠️ 退路那一档：原版的静态常量读出不来（见上），沿用本文件 `Flat` 既有的 `Vector3.up`。
        /// <summary>原版那个「两点重合」阈值（`DAT_1834b2f44`，**4 字节 float 实读 = 1e-5**）——
        /// 比的是**长度**（不是平方），判据见 `ReceiveAttackAnim.c` 那一句 `if (DAT < len)`。</summary>
        public const float PushBackDegenEpsilon = 1e-5f;

        /// <summary>原版 `normalize(受击方 − 攻击方)`（屏幕平面版，见上面那段换算说明）。
        /// 两点距离 ≤ <see cref="PushBackDegenEpsilon"/> ⇒ 返回 `Vector3.up`（原版那一档取常量向量）。</summary>
        public static Vector3 PushBackDir(Vector3 victimPos, Vector3 attackerPos)
        {
            Vector3 d = victimPos - attackerPos;
            d.z = 0f;                                  // 原版 y 恒 0（XZ 地面）；我们 z 恒 0（XY 平面）
            float len = d.magnitude;
            if (len <= PushBackDegenEpsilon) return Vector3.up;
            return d / len;
        }

        /// <summary>本会话**知道攻击方**的后坐次数（自检用：证明生产走的是新那条路）。</summary>
        public static int AwayFromAttackerCount { get; private set; }
        /// <summary>本会话**不知道攻击方**（疲劳 / 反噬 / 反伤）而后退回旧方向的后坐次数。</summary>
        public static int NoAttackerFallbackCount { get; private set; }
        /// <summary>最近一次后坐用的**平面单位方向**（自检用）。</summary>
        public static Vector3 LastPushBackDir { get; private set; }

        static readonly HashSet<string> _warnedNoAttacker = new HashSet<string>();
        const int NoAttackerWarnCap = 8;
        static void SpeakNoAttacker(string who)
        {
            if (_warnedNoAttacker.Count >= NoAttackerWarnCap || !_warnedNoAttacker.Add(who ?? "")) return;
            Debug.LogWarning($"[CardFeel] `{who}` 的挨打后坐**不知道攻击方是谁** ⇒ 方向退回「往自己半场外侧」"
                           + "（原版这一档不存在：`CardScript__ReceiveAttackAnim.c` 的方向恒为 `受击方 − 攻击方`）");
        }

        /// <summary>🆕 2026-10-23：**知道攻击方**时的挨打后坐（方向 = <see cref="PushBackDir"/>）。
        /// <para><paramref name="attacker"/> 为 null ⇒ 退回 <paramref name="fallbackAway"/>（= 旧口径的恒定上/下）
        /// **并出声**（⛔ 不装成原版）。</para>
        /// <param name="attackerMeleeAttack">攻击方的近战值（-1 = 不知道 ⇒ 幅度走 `Impact *` 退路）。</param></summary>
        public static Tween HitReactFrom(Transform tr, Transform attacker, Vector3 fallbackAway,
                                         bool heavy, float delay, int attackerMeleeAttack)
        {
            if (tr == null) return null;
            Vector3 d;
            if (attacker != null)
            {
                d = PushBackDir(tr.position, attacker.position);
                AwayFromAttackerCount++;
            }
            else
            {
                d = Flat(fallbackAway);
                NoAttackerFallbackCount++;
                SpeakNoAttacker(tr.name);
            }
            return PunchBack(tr, d, heavy, delay, attackerMeleeAttack);
        }

        /// <summary>后坐本体（两条入口共用一份 —— 判据只留一处）。</summary>
        static Tween PunchBack(Transform tr, Vector3 d, bool heavy, float delay,
                               int attackerMeleeAttack)
        {
            if (tr == null) return null;
            LastPushBackDir = d;
            // 🔴 `B02`：档位轴 = **攻击方的近战档**（不是伤害值）。`heavy` 只剩「不知道攻击方」那条退路，
            //   `HeavyHitDamage` 也只在那条退路上当分档门槛（见 `BattleDriver` 的两处调用）。
            float mag = attackerMeleeAttack >= 0
                ? PushBackMagnitude(attackerMeleeAttack)                 // 原版口径（0.5 / 1.0 / 1.5）
                : (heavy ? HitPunchUnitsHeavy : HitPunchUnitsLight);     // 退路：`Impact *` 顶替值（A06）

            // 位置：DoPushBack 的**形状**（0.4 / vib 8 / 弹性 0.3）+ 上面的幅度。**近战只有这一条。**
            return CardTween.Use(
                tr.DOPunchPosition(d * ToOurs(mag), PushBackDuration, PushBackVibrato, PushBackElasticity)
                  .SetDelay(delay), Ease.OutQuad, tr);
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
        /// 方向（🔴 **2026-10-23（R9 尾账）已改走原版公式**）：参数 `direction` = preset 真件的
        /// `preset.direction`（3D 世界方向），屏幕竖直分量 = <see cref="ShakeScreenUpOf"/> =
        /// `(FromToRotation(Vector3.down, normalize(direction)) · up).y` —— **符号与 `|vy|` 都由它给**。
        /// ⇒ 原版 raw **−Y** 指向 velocity：「向上还是向下」**不再是我们的选择**，
        /// 挨打 `(0,+0.2,0.2)` 朝下、落地 `(0,−1,0)` 朝上、`(0,0,2)` 在屏幕上不上下晃。
        /// ⚠️ 「世界 y 分量 = 屏幕竖直」仍是**我们的 3D→2D 换算**（见 `ShakeWorldAmplitude`）。
        ///
        /// 时间轴严格照 preset：`attackTime` 线性 0→1 → 保持 `sustainTime` → **线性**衰减到
        /// `ShakeDecayTo`、历时 `decayTime`。
        /// 位移 = 峰值 × **包络**(t) × **噪声**(t)，噪声是该 preset 的 `rawSignal`（`Warpforge 6D Shake*`）
        /// 按 `frequencyGain`（原版 `m_FrequencyGain` = preset 的 `frequency`）采样后叠加
        /// （算式见 `ShakeNoise01`）。
        /// 🆕 **2026-10-23（R9 尾账）**：还叠一层 **`OrientationNoise` 旋转**（见 `ShakeOrientation`）——
        /// 相机空间后乘，HUD 根做同一个刚体变换保持在屏幕上不动。
        /// 🆕 **2026-10-23（R9）**：`signal / frequencyGain / attack / sustain / decay` 全部**按 preset 传**
        /// （默认参数 = `Shake Hit Small` 那一套 ⇒ 既有调用点行为不变）。
        /// 原来这些是写死的常量 ⇒ AnimFX 那条支路（`BattleDriver.HookAnimFxShake`）上**别的 preset
        /// 一律用 Hit Small 的时间轴**（`Shake Earthquake` 的 5 s 保持会被压成 0.1 s）。
        /// </summary>
        /// <param name="worldAmp">**旋转前**的 pos 峰值（世界单位）。⚠️ **取哪一档由调用方决定** ——
        /// 原版是按攻击方的近战档取 2/4/8（→ 世界单位 0.057 / 0.114 / 0.228），
        /// 走 `ShakeWorldAmplitudeFor` / 更省事的 `ShakeCameraForHit`。
        /// 🔴 屏幕竖直分量与符号由 <paramref name="direction"/> 现算（⛔ 别在这里再乘竖直比例）。</param>
        /// <param name="direction">原版 `preset.direction`（3D，未归一化也行）；空向量 ⇒ `Shake Hit Small` 那件。
        /// 判据 = `FromToRotation(Vector3.down, normalize(direction))`（见 <see cref="ShakeScreenUpOf"/>）。</param>
        /// <param name="rotationGain">原版 `m_AmplitudeGain` = `preset.amplitude × |direction|`
        /// （= 位置与角度**共用**的那个 gain，见 `ShakeOrientation`）。</param>
        /// <param name="signal">该 preset 的波形表（原版 `rawSignal`）。`null` ⇒ `DefaultShakeSignal`。</param>
        /// <param name="frequencyGain">信号时间轴缩放（原版 preset 的 `frequency`，逐 preset 不同：0.03 / 0.05 / 0.1 / 0.2 …）。</param>
        /// <param name="attackTime">包络上升时长（原版 preset 的 `attackTime`）。</param>
        /// <param name="sustainTime">包络满幅保持时长（原版 preset 的 `sustainTime`）。</param>
        /// <param name="decayTime">包络衰减时长（原版 preset 的 `decayTime`）。</param>
        /// <param name="phaseSeed">噪声的**起播时刻**种子（见 `ShakePhaseOffset`）。
        /// `-1`（默认）= `ShakeSeedBase + 本局序号` ⇒ **同一局必然给同一串相位**（对局可复现）；
        /// `ShakeSeedBase` 由开局时的 <see cref="AnchorShakeSeedToBattle"/> 定成该局的对局种子
        /// （没接锚点的场合 = 出厂常量 `ShakeSeedBaseDefault`，与改动前逐字相同）。
        /// 传具体值 = 逐帧可复算（自检用）。</param>
        public static Tween ShakeCamera(Camera cam, Transform hudRoot, float worldAmp, float delay = 0f,
                                        Camera extraCam = null, int phaseSeed = -1,
                                        Vector3 direction = default(Vector3),
                                        ShakeSignal signal = null,
                                        float frequencyGain = ShakeFrequencyGain,
                                        float attackTime = ShakeAttackTime,
                                        float sustainTime = ShakeSustainTime,
                                        float decayTime = ShakeDecayTime,
                                        float rotationGain = ShakeHitRotationGain)
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
            Quaternion camHomeRot = camTr.rotation;
            // HUD 那一半**用世界坐标记账**（`hudRoot` 的父级 = 战斗根、identity ⇒ 与 local 逐位相同；
            // 但「绕相机位置做刚体反补」这条算式的自然表述是世界坐标）。local 也留一份给收尾兜底。
            Vector3 hudHome = hudRoot != null ? hudRoot.position : Vector3.zero;
            Quaternion hudHomeRot = hudRoot != null ? hudRoot.rotation : Quaternion.identity;
            Vector3 hudHomeLocal = hudRoot != null ? hudRoot.localPosition : Vector3.zero;
            Quaternion hudHomeLocalRot = hudRoot != null ? hudRoot.localRotation : Quaternion.identity;
            // 🆕 2026-09-19：**3D 战场那台透视相机也跟着推**。不推的话挨打时「卡牌在动、背景纹丝不动」
            //    （战场现在是真 3D 了，原版震的本来就是战场相机）。两台相机同向等量平移 ⇒
            //    战场与卡牌的位移方向一致；像素幅度由各自投影决定（透视那侧略有视差，这是对的）。
            var extraTr = extraCam != null ? extraCam.transform : null;
            Vector3 extraHome = extraTr != null ? extraTr.position : Vector3.zero;
            Quaternion extraHomeRot = extraTr != null ? extraTr.rotation : Quaternion.identity;
            // 🔴 **2026-10-23（R9 尾账）原版方向链 —— `verticalSign` 那一档已删掉。**
            //   `LegacySignalSource::GetSignal`（RVA 0xA2B350）：`pos = FromToRotation(Vector3.down, velocity) * pos`。
            //   我们 2D 只采 PositionNoise 的 Y ⇒ raw pos = (0, s, 0) ⇒ 屏幕竖直 = `s × ShakeScreenUpOf(direction)`。
            //   ⚠️ 该值**自带 |vy|**（`worldAmp` 里不再乘竖直比例）且**自带符号**：
            //     挨打 `(0,+0.2,0.2)` ⇒ **−0.7071（相机朝下）**；落地 `(0,−1,0)` ⇒ **+1（朝上）**。
            if (direction.sqrMagnitude < 1e-12f) direction = ShakeHitDirection;   // 默认 = `Shake Hit Small` 那件
            float screenUp = ShakeScreenUpOf(direction);
            LastShakeVertical = screenUp;

            // 🆕 2026-10-14（`A1767` / #10）：**每次震的起播时刻不同**（原版 `m_Randomize = 1`，
            //   见 `ShakeWorldAmplitude` 的注释）。⚠️ 种子必须**可复现** —— 一律 `System.Random`
            //   派生，⛔ 绝不用 `UnityEngine.Random`（工程红线：对局必须可复现）。
            LastShakeSeed = phaseSeed >= 0 ? phaseSeed : (ShakeSeedBase + _shakeSeq++);
            float phase = ShakePhaseOffset(LastShakeSeed);
            LastShakePhase = phase;

            // 🆕 2026-10-23（R9）：**每个 preset 自己的**时间轴与信号（原来是写死的 Hit Small 那套）。
            if (signal == null) signal = DefaultShakeSignal;
            LastShakeFrequencyGain = frequencyGain;
            LastShakeSignalName = signal.name;
            LastShakeDuration = attackTime + sustainTime + decayTime;

            System.Action<float> apply = t =>
            {
                float env = ShakeEnvelope(t, attackTime, sustainTime, decayTime);
                // ---- 平移（PositionNoise 的 Y，经原版 `FromToRotation` 投影到屏幕竖直）----
                Vector3 d = Vector3.up * (worldAmp * env * ShakeNoise01(t, phase, frequencyGain, signal) * screenUp);
                if (camTr != null) camTr.position = camHome + d;

                // ---- 旋转（OrientationNoise —— 🆕 2026-10-23（R9 尾账）接上）----
                //   原版：`rot = SlerpUnclamped(identity, Euler(ΣOrientation×π/180), |v|·AmplitudeGain)`
                //   （`LegacySignalSource::GetSignal`），再过一次 `SlerpUnclamped(identity, rot, 包络)`
                //   （`GetDecayedSignal`）；listener 里 `m_UseCameraSpace = 1` ⇒ 落在**相机空间**
                //   （`CameraState.GetFinalOrientation = RawOrientation × OrientationCorrection`）
                //   ⇒ 后乘在相机自身旋转上。
                Quaternion q = ShakeOrientation(t, phase, frequencyGain, signal, rotationGain, env);
                if (camTr != null) camTr.rotation = camHomeRot * q;
                if (extraTr != null) extraTr.rotation = extraHomeRot * q;
                LastShakeRotation = q;

                if (hudRoot != null)
                {
                    // HUD **在屏幕上纹丝不动**（原版 UI 相机没挂 impulse listener）。
                    // 相机既平移 d、又旋转 q ⇒ HUD 跟着做**同一个刚体变换**（绕**相机位置**转 M）：
                    //   X' = C + d + M·(X − C)，  M = R·q·R⁻¹（q 在相机空间 ⇒ 换回世界空间）
                    Quaternion M = camHomeRot * q * Quaternion.Inverse(camHomeRot);
                    hudRoot.position = camHome + d + M * (hudHome - camHome);
                    hudRoot.rotation = M * hudHomeRot;
                }
            };

            float total = LastShakeDuration;
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
                if (camTr != null) { camTr.position = camHome; camTr.rotation = camHomeRot; }
                if (hudRoot != null)
                {
                    hudRoot.position = hudHome; hudRoot.rotation = hudHomeRot;
                    hudRoot.localPosition = hudHomeLocal; hudRoot.localRotation = hudHomeLocalRot;   // 父级非 identity 时也对
                }
                if (extraTr != null) { extraTr.position = extraHome; extraTr.rotation = extraHomeRot; }
                LastShakeRotation = Quaternion.identity;
            });
            LastShake = t;
            return t;
        }

        /// <summary>🆕 2026-10-14（`A1767` / #10）**挨打震屏的完整一档** —— 幅度自己按
        /// **攻击方的近战攻击**取（原版 `ResolveAttackAnimationEffects` 的 2/4/8 档），
        /// 而不是永远第 1 档。
        ///
        /// 🔴 **为什么要有这条**：`BattleDriver` 现在两处都传的是 `ShakeWorldAmplitude`
        /// （写死第 1 档），而原版是按 `SupportMethods.GetUnitSize(攻击方)` 换幅度的
        /// ⇒ 尺寸 2 / 3 的单位挨打时，**我们的镜头抖得比原版小 2 倍 / 4 倍**。
        /// 判据链全程本地可复核（`GetUnitSize` → `ResolveAttackAnimationEffects` 那三个 `_DAT_`）。
        ///
        /// ⚠️ **它是 opt-in 的**：调用方不换过来，档位仍然只用得到第 1 档 ——
        ///    `BattleDriver.cs` **不在本件的白名单里** ⇒ 落地那一处写在写手报告里
        ///    （要换的只有 `PlaySignal` 里那一句：`ShakeCamera(cam, hudRoot, CardFeel.ShakeWorldAmplitude, 0f, boardCam)`
        ///     → `ShakeCameraForHit(cam, hudRoot, 攻击方的 CurrentMeleeAttack, 0f, boardCam)`）。
        /// </summary>
        /// <param name="attackerMeleeAttack">**攻击方**的当前近战攻击（`UnitState.Attack`）。
        /// 传 `-1` = 不知道攻击方是谁（疲劳 / 反伤那类）⇒ 第 1 档，与原版「尺寸 0」同档。</param>
        public static Tween ShakeCameraForHit(Camera cam, Transform hudRoot, int attackerMeleeAttack,
                                              float delay = 0f, Camera extraCam = null,
                                              int phaseSeed = -1)
        {
            int size = ShakeUnitSizeOf(attackerMeleeAttack);
            // 🔴 2026-10-23（R9 尾账）：`direction` = `Shake Hit Small` 的真件值（**不再是 verticalSign**）；
            //   旋转 gain 与位置同源 = `amplitude × |direction|`（尺寸 2/3 档一起放大，原版也是这样）。
            return ShakeCamera(cam, hudRoot, ShakeWorldAmplitudeFor(size), delay, extraCam, phaseSeed,
                               direction: ShakeHitDirection,
                               rotationGain: ShakeAmplitudeForUnitSize(size) * ShakePresetDirMag);
        }

        // ---- 🆕 2026-10-16 落地那一下（原版 `CardScript.LandOnGround`）----
        //   判据：卡预制体 `MonoBehaviour_1744609728290659264.json` 的 `shakeWhenLand`
        //   （`OverwriteCameraShakePreset`，三个 `overwrite*` **全是 0**）→ presetSO → 真件
        //   `Shake Land on ground`（`bundle_tweenandshakes_assets_all/MonoBehaviour/Shake Land on ground.json`）。
        //   消费点：`CardScript__LandOnGround.c:26-29`（`OverwriteCameraShakePreset__Play`）。

        /// <summary>落地那件 preset 真件 `Shake Land on ground` 的 `preset.amplitude` = 2.0
        /// （与 `Shake Hit Small` 同值）。⚠️ 它**不会**被尺寸档覆盖 —— 卡预制体上那三个
        /// `overwriteAmplitude/Frequency/SustainTime` 全是 0 ⇒ `GetModifiedPreset()` 原样用 preset。</summary>
        public const float ShakeLandPresetAmplitude = 2.0f;

        /// <summary>同一件真件的 `preset.direction = (0, −1, 0)` 的**模长** = 1.0
        /// （挨打那件是 (0, 0.2, 0.2) ⇒ 0.28284，这也是两者幅度差 3.5 倍的来源）。</summary>
        public const float ShakeLandPresetDirMag = 1.0f;

        /// <summary>🆕 2026-10-16：**落地震镜头的世界单位峰值幅度**（我们这套坐标，**旋转前**的 pos 峰值）。
        /// 算式与 <see cref="ShakeWorldAmplitudeFor"/> **逐项同形**（同一个 `ShakeBandPeak`、
        /// 同一个 `ShakeOriginalPxPerUnit ÷ OurPxPerUnit`），只换 preset 那一组：
        ///   `2.0(amplitude) × 1.0(|direction|) × 0.149(三条带同相峰值)`
        ///   = **0.298 原版世界单位** ⇒ × 73.1 px/单位 = **21.8 px** @1080p ⇒ ÷ 108 = **0.2017**。
        /// 🔴 **2026-10-23（R9 尾账）**：这里**不再乘竖直比例** —— 落地那件的
        /// `ShakeScreenUpOf((0,−1,0)) = **+1**`（原版 `FromToRotation(down, down)` = 恒等 ⇒ raw +Y 朝上），
        /// 由 `ShakeCamera` 现算。⚠️ 竖直峰值不变（×1.0），但**符号从此是「朝上」**
        /// （旧实现传 `verticalSign: -1` ⇒ 朝下，方向反了）。
        /// ⚠️ 它比挨打那一下（`ShakeWorldAmplitudeFor(0)` = 0.0570）**大 3.5 倍** —— 不是笔误：
        ///   挨打的 |direction| 只有 0.28284，落地这件是 1.0（再乘竖直分量 0.7071 vs 1.0 ⇒ 5 倍位移差）。</summary>
        public static float ShakeWorldAmplitudeForLand()
        {
            float ampGain = ShakeLandPresetAmplitude * ShakeLandPresetDirMag;    // = 2.0
            float origUnits = ampGain * ShakeBandPeak;                           // = 0.298
            return origUnits * (ShakeOriginalPxPerUnit / OurPxPerUnit);          // = 0.2017
        }

        /// <summary>🆕 2026-10-16 **落地那一下的震镜头** —— 原版 `CardScript.LandOnGround` 里那个
        /// `OverwriteCameraShakePreset.Play()`（preset = `Shake Land on ground`）。
        /// 做法与「为什么是相机 + HUD 根同向平移」→ <see cref="ShakeCamera"/> 的注释（同一套，不另起一处）。
        /// ⚠️ 与 <see cref="ShakeCameraForHit"/> 的区别：**没有尺寸档**（原版那个组件不覆盖任何参数）。</summary>
        public static Tween ShakeCameraForLand(Camera cam, Transform hudRoot, float delay = 0f,
                                              Camera extraCam = null, int phaseSeed = -1)
        {
            // 🔴 **2026-10-23（R9 尾账）**：传原版 preset 的 `direction = (0, −1, 0)`，
            //   符号由 `FromToRotation(Vector3.down, direction)` 现算 = **+1 ⇒ 相机朝上**
            //   （旧实现传 `verticalSign: -1` 是反的）。旋转 gain = `2.0 × 1.0 = 2.0`（同一条链）。
            return ShakeCamera(cam, hudRoot, ShakeWorldAmplitudeForLand(), delay, extraCam, phaseSeed,
                               direction: ShakeLandDirection,
                               rotationGain: ShakeLandPresetAmplitude * ShakeLandPresetDirMag);
        }

        /// <summary>🆕 **震屏噪声的起播时刻基线**（原版 `m_Randomize = 1` 那一位）。
        /// 每次震动用的种子 = `ShakeSeedBase + 递增序号`。
        ///
        /// 🔴 **可复现性怎么保证**（工程红线：对局必须可复现）：
        ///   · 种子**全部**来自 `System.Random`（纯托管、定序），⛔ **一处都没用 `UnityEngine.Random`**；
        ///   · 序号是**本进程内的单调计数** ⇒ **同一串事件流**（= 同一局重放两次，事件顺序与条数相同）
        ///     必然给**同一串种子** ⇒ 逐帧位移逐位相同；
        ///   · **基线按局接** —— 开局时由 <see cref="AnchorShakeSeedToBattle"/> 把它设成**该局的对局种子**，
        ///     并把序号归零。这一格出厂值是固定的（`ShakeSeedBaseDefault`），所以
        ///     **没有接锚点的场合（自检、纯特效震动）行为与从前逐字相同**。
        ///
        /// 🔴 **2026-10-14（`A1892`）就地订正（铁律 5）**：这一段原来写的是
        ///   «`ShakeSeedBase` 是**可写**的：要让「两局之间也不同」——**开局时把它设成该局的种子即可**。
        ///   ⚠️ **那一句还没接**（`BattleDriver` 不在本件白名单；而且 `BattleContext` **没有暴露对局种子**）»
        ///   —— 三处都要改口径：
        ///   ① **`BattleContext` 确实没有对局种子**（`Core/BattleContext.cs:558` 的 `.ctor(int seed)`
        ///      只拿它造 `Rng` / `ShowRng` / `AiRng` 三个 `System.Random`，**一个字段都不存**）——
        ///      这一句**成立、不用改**；但它**不是卡点**：对局种子在**表现层**本来就有，
        ///      就是 `BattleDriver._seed`（`Battle/BattleDriver.cs:852` 的字段、`:2769` 在 `Begin` 里赋值，
        ///      一次 `Begin` 覆盖本地 / 联机 / 回放 / 重开 / 教程**所有开局入口**）。
        ///   ② 所以真正的卡点**只有**「`Begin` 里少一行调用」—— 见 <see cref="AnchorShakeSeedToBattle"/>。
        ///   ③ 「**同一局可复现 ✅**」这句**带着一个没写出来的前提**：那个「进程内单调序号」在开局时
        ///      **必须正好是 0**。而 `ResetShakePhases()` 全仓**一个调用点都没有** ⇒
        ///      同一进程里「先打 A 局、再放 A 局的录像」时序号是从半途接上的，
        ///      两次的相位**并不相同**（回放与原始那一局的观感对不上）。
        ///      锚点里那句 `_shakeSeq = 0` 就是把这个前提**做实**（不再靠调用方自觉）。
        /// </summary>
        public static int ShakeSeedBase = ShakeSeedBaseDefault;
        /// <summary>`ShakeSeedBase` 的出厂值（固定常量 ⇒ 可复现）</summary>
        public const int ShakeSeedBaseDefault = 0x5D2A17;

        /// <summary>🆕 **把震屏种子接到「这一局」上**（`A1892`）—— **每局开局调一次**。
        ///
        /// 🔴 **调用点（唯一一处，一行）**：`Battle/BattleDriver.cs` 的 `Begin(...)` 里
        /// `_seed = seed;`（`:2769`）那一句**的下一行**：
        /// <code>CardFeel.AnchorShakeSeedToBattle(seed);</code>
        /// ⚠️ 那一处**不在本件的白名单里**（本件只被授权改 `Core/CardFeel.cs`）⇒
        ///    这一行由**主对话 / battle 线**落，落地前本锚点**没有任何调用者**
        ///    （= 行为与改动前逐字相同，不会静默改变现状）。
        ///
        /// **它管三件事**（都在 `A1892` 的验收标准里）：
        ///   ① `ShakeSeedBase = 对局种子` ⇒ **两局之间「第一下」的相位不同**
        ///      （本局第一下的种子 = `battleSeed + 0`，相位 = <see cref="ShakePhaseOffset"/>(`battleSeed`)，
        ///      纯函数 ⇒ **这一条可直接算着验**，不必真跑抖动）；
        ///   ② `_shakeSeq = 0` ⇒ **同一局（同一个种子）无论在本进程里第几次被播放**，
        ///      相位序列都**逐位相同**（「同一局重放两次逐位相同」从此**无条件**成立，
        ///      不再依赖「序号恰好从 0 起」——见 `ShakeSeedBase` 的注释 ③）；
        ///   ③ 顺手 `SettleShake()`：上一局尾巴上没跑完的那一次先收掉，别把「歪着的位」带进新一局
        ///      （`ShakeCamera` 自己开头也会收，这里是**进新局**这个语义点上再收一次，幂等）。
        ///
        /// **为什么是「对局种子」而不是别的**：原版 `CinemachineImpulseDefinition.m_Randomize`
        /// （Tooltip：«Legacy mode only: Randomize the signal start time»）只要求
        /// 「**每次**起播时刻不同」，**没说**它跟对局种子是什么关系 ⇒
        /// 用对局种子派生是**我们挑的**（⛔ 不是原版做法）。挑它的理由只有一个：
        /// 工程红线「对局必须可复现」—— 拿时间 / `UnityEngine.Random` 当种子都会让同一局两次不一样。
        /// </summary>
        /// <param name="battleSeed">**这一局的对局种子**（`BattleDriver._seed`，也就是
        /// `Begin(..., int seed, ...)` 收到的那个 `seed`）。同一个种子 ⇒ 同一条相位序列。</param>
        public static void AnchorShakeSeedToBattle(int battleSeed)
        {
            SettleShake();                    // ① 见上面 ③
            ShakeSeedBase = battleSeed;       // ② 本局的第一下 = `ShakePhaseOffset(battleSeed)`
            _shakeSeq = 0;                    // ③ 序号从 0 起 ⇒ 同种子必然同序列
            LastShakeSeed = 0;                // ④ 诊断那些格别留着上一局的读数（同 `ResetShakePhases`）
            LastShakePhase = 0f;
            LastShakeSignalName = ""; LastShakeFrequencyGain = 0f; LastShakeDuration = 0f;
            LastShakeVertical = 0f; LastShakeRotation = Quaternion.identity;
        }

        /// <summary>起播时刻的取值范围（秒，**信号时间轴**上的量，不是真实秒）——
        /// 🔴 **2026-10-23（R9）订正：`±1000`，且含负**。判据（机器码）：`CinemachineImpulseDefinition+LegacySignalSource`
        /// 的 ctor（RVA 0xA2B5E0）在 `m_Randomize=1` 且 `SignalDuration ≤ 0`（`NoiseSettings` 恒 0）时
        /// `m_StartTimeOffset = Random.Range(−1000f, +1000f)`；两个常量 `c47a0000` / `447a0000` 从
        /// `GameAssembly.dll` 实读（`R9_rawSignal_算式.md` §2·4）。
        /// ⚠️ 原来写的 100 是**代理值**（当时方法体读不到，注释里也如实标了）—— 现在有判据，改用 ±1000。</summary>
        public const float ShakePhaseRange = 1000f;

        /// <summary>本进程内已经震过几次（诊断 / 自检复位用）。**它决定相位种子的序号**。</summary>
        public static int ShakeSeq { get { return _shakeSeq; } }
        static int _shakeSeq;
        /// <summary>最近一次震动用的**种子**与**起播时刻**（自检读它核「种子确实进了噪声」）。</summary>
        public static int LastShakeSeed { get; private set; }
        public static float LastShakePhase { get; private set; }
        /// <summary>🆕 2026-10-23（R9）最近一次震动用的**信号表名 / 时间缩放 / 总时长**。
        /// 自检靠它钉「preset 自己的 `frequency` / `attack` / `sustain` / `decay` 真的传到底了」——
        /// 原来 `BattleDriver.HookAnimFxShake` 把后四个全丢了，AnimFX 那条支路一律用 `Shake Hit Small` 的时间轴。</summary>
        public static string LastShakeSignalName { get; private set; } = "";
        public static float LastShakeFrequencyGain { get; private set; }
        public static float LastShakeDuration { get; private set; }
        /// <summary>🆕 2026-10-23（R9 尾账）最近一次震动用的**屏幕竖直分量** =
        /// `ShakeScreenUpOf(direction)`（含符号与 `|vy|`；挨打 **−0.7071** / 落地 **+1**）。
        /// 自检靠它钉「方向确实走原版 `FromToRotation(Vector3.down, velocity)`」。</summary>
        public static float LastShakeVertical { get; private set; }
        /// <summary>🆕 2026-10-23（R9 尾账）最近一次震动在**t=0** 施加的**相机空间旋转修正**
        /// （`ShakeOrientation` 的结果；`ResetShakePhases` / 收尾时归 `identity`）。</summary>
        public static Quaternion LastShakeRotation { get; private set; } = Quaternion.identity;

        /// <summary>把「震过几次」的计数清零（**自检专用**：批处理里跑两遍要拿到同一串相位）。
        /// ⚠️ **2026-10-14（`A1892`）实测：本方法全仓【一个调用点都没有】**（只在本文件里有定义）——
        /// 真机上的「每局从 0 起」现在由 <see cref="AnchorShakeSeedToBattle"/> 负责
        /// （它多做一件：把基线也换成该局的对局种子）。留着它是给自检用的复位口。</summary>
        public static void ResetShakePhases()
        {
            _shakeSeq = 0; LastShakeSeed = 0; LastShakePhase = 0f;
            LastShakeSignalName = ""; LastShakeFrequencyGain = 0f; LastShakeDuration = 0f;   // R9 的三格读数
            LastShakeVertical = 0f; LastShakeRotation = Quaternion.identity;                 // R9 尾账的两格读数
        }

        /// <summary>一个种子 → 该次震动的**起播时刻**（秒，信号时间轴）。
        /// 判据：原版 `m_Randomize = 1` ⇒ `m_StartTimeOffset = Random.Range(−1000f, +1000f)`
        /// （`LegacySignalSource` ctor RVA 0xA2B5E0；**含负**）。
        /// ⚠️ 唯一**我们的代理**：原版用 `UnityEngine.Random`，我们用 `System.Random` 派生
        /// （工程红线「对局必须可复现」，⛔ 不用 `UnityEngine.Random`）；**区间与分布（均匀）照原版**。
        /// **纯函数**：同 `seed` 恒得到同一个值（自检靠这条钉「可复现」）。</summary>
        public static float ShakePhaseOffset(int seed)
        {
            var rng = new System.Random(seed);
            return (float)(rng.NextDouble() * (2.0 * ShakePhaseRange) - ShakePhaseRange);
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
        ///    这是**自检专用**的收尾口，而且按工程惯例**不让调用方直接碰 DOTween**。
        ///    🔴 **2026-10-19（W）就地订正（铁律 5）**：本行原来写「`BattleDriver` / 自检都没有
        ///    `using DG.Tweening`」—— **前半句已过期**：`BattleDriver.cs:14` 自 2026-09-30 起
        ///    就有 `using DG.Tweening`（reveal 那段「飞到 HUD 钮」用 `DOMove/DOFade`）。
        ///    ✅ 仍成立的是「`BattleScene`（本自检的宿主）没有那个 using」；「回调从这里接」
        ///    现在只是本文件的划法，**不是因为调用方编不过** —— ⛔ 别拿旧理由限制后续写法。
        /// </summary>
        public static void SettleShake()
        {
            var s = LastShake;
            if (s != null && s.IsActive()) s.Complete();     // `Complete()` 会触发 `OnKill` ⇒ 复位
        }

        /// <summary>包络：**attack 段线性 0→1**（`attackTime 0` ⇒ 立刻 1）→ 保持 `sustainTime` →
        /// **线性**降到 `ShakeDecayTo`（**终点不是 0** —— 出处见 `ShakeDecayTo`）。
        /// 🆕 2026-10-23（R9）：三个时间改成**按 preset 传**（不同 preset 差很多：`Shake Earthquake`
        /// attack 0.5 / sustain 5.0 / decay 1.0，`Shake Very Light 4s` 3.0 s 保持）。
        /// attack 段的形状出处 = 场景 `CinemachineImpulseSource`（battlearena1 `MonoBehaviour_4914.json`）
        /// 的 `m_TimeEnvelope.m_AttackShape` = (0,**0**)→(1,**1**)、两帧 `weightedMode:0`（切线被忽略）⇒ 线性。</summary>
        static float ShakeEnvelope(float t, float attackTime, float sustainTime, float decayTime)
        {
            if (attackTime > 0f && t < attackTime) return Mathf.Clamp01(t / attackTime);
            float held = attackTime + sustainTime;
            if (t <= held) return 1f;
            return Mathf.Lerp(1f, ShakeDecayTo, Mathf.Clamp01((t - held) / decayTime));
        }

        /// <summary>归一化噪声（值域 ±0.5，见 `ShakeBandPeak` 的注释）。**默认信号 + `ShakeFrequencyGain`** 的
        /// 便捷重载 —— 挨打 / 落地那两条走它（`Shake Hit Small` 与 `Shake Land on ground` 的 `frequency` 都是 0.05）。
        ///
        /// 🔴 **2026-10-23（R9）算式已从机器码坐实，本函数照抄它**（判据 → `资料/普查产出_1023b/R9_rawSignal_算式.md`）：
        /// ```text
        /// T   = t × frequencyGain + phase                 // 信号时间（秒），**三条带共享同一个 T**
        /// u_i = 带i.Frequency × T
        /// 带i = Constant == 1 ? Amplitude × 0.5 × sin(2π·u_i)
        ///                     : Amplitude × (Mathf.PerlinNoise(u_i, 0) − 0.5)
        /// 输出 = (Σ 三条带) / ShakeBandPeak
        /// ```
        /// 三条关键细节（每条都踩过或差点踩）：
        ///   ① **`Constant = 1` 走正弦、`0` 走 Perlin** —— 默认信号是 **1/0/1**，不是「三条全 Perlin」
        ///      （旧实现就是三条全 Perlin ⇒ 低频 1.9 Hz 与高频 55.54 Hz 的**质感整个不对**）。
        ///   ② **Perlin 的变化坐标在 x 轴**：`PerlinNoise(u, 0)`。旧实现写的是 `(0, u)` —— 2D 梯度噪声
        ///      `noise(0,y) ≠ noise(y,0)`，整条波形都不同。
        ///   ③ **每带输出是 ±0.5·A**（`−0.5` / `×0.5`），不是 ±A。归一化分母仍是 ΣA = 0.149
        ///      ⇒ 峰值位移落在原版值上（推导见 `ShakeBandPeak`）。
        /// 原版来源：`Cinemachine.NoiseSettings`（`GetValueAt` RVA 0xA10610 / `GetCombinedFilterResults`
        /// RVA 0xA10670 = 三条带**相加**）。起播偏移（`phase` = 原版 `m_StartTimeOffset`）由
        /// <see cref="ShakePhaseOffset"/> 给（**每次不同、且可复现**）。
        /// </summary>
        public static float ShakeNoise01(float t, float phase)
        {
            return ShakeNoise01(t, phase, ShakeFrequencyGain, DefaultShakeSignal);
        }

        /// <summary>按指定信号表 / 时间缩放求值（同一个算式，见上面那个重载的注释）。
        /// `signal == null` ⇒ 默认信号。</summary>
        public static float ShakeNoise01(float t, float phase, float frequencyGain, ShakeSignal signal)
        {
            if (signal == null) signal = DefaultShakeSignal;
            float T = t * frequencyGain + phase;      // ⚠️ 共享 T：相位由「T × 各带 f」自然分带
            float s = 0f;
            for (int i = 0; i < signal.Count; i++)
            {
                ShakeBandSignal b = signal[i];
                float u = b.hz * T;
                s += b.constant != 0
                    ? b.amp * 0.5f * Mathf.Sin(u * 2f * Mathf.PI)          // Constant=1 ⇒ 正弦
                    : b.amp * (Mathf.PerlinNoise(u, 0f) - 0.5f);           // Constant=0 ⇒ Perlin（x 轴变化）
            }
            return s / ShakeBandPeak;
        }

        /// <summary>🆕 **2026-10-23（R9 尾账）原版 `OrientationNoise`（角度抖动）** ——
        /// 返回**相机空间**的旋转修正（后乘在相机自身旋转上）。
        ///
        /// 判据（全部机器码，`资料/普查产出_1023b/R9_rawSignal_算式.md` §2·3/§2·4/§2·5）：
        /// ```text
        /// euler_a = Σ_{octave} AxisValue(f, A, Constant, T)      // 三轴各自求和（同一套 sin/Perlin 算式）
        /// rot     = Quaternion.Euler(euler)                      // 原版：×π/180 后走 Internal_FromEulerRad
        /// rot     = SlerpUnclamped(identity, rot, gain)          // LegacySignalSource：gain = |v|·m_AmplitudeGain
        /// rot     = SlerpUnclamped(identity, rot, envelope)      // GetDecayedSignal：再乘一次包络
        /// ```
        /// `T` 与位置那一路**共用同一个**（`t × frequencyGain + phase`）。
        ///
        /// 施加方式：原版 listener 的 `m_UseCameraSpace = 1`（场景 `MonoBehaviour_4546.json`）⇒
        /// `state.OrientationCorrection = ... × (RawOrientation × rot)`，而
        /// `CameraState.GetFinalOrientation = RawOrientation × OrientationCorrection`
        /// （`CameraState.txt` 的 `get_CorrectedOrientation` / `get_FinalOrientation` 反汇编）
        /// ⇒ 对相机就是**相机空间**的修正。⚠️ 我们两台相机（正交主相机 + 3D 战场相机）都后乘同一个 q。
        ///
        /// ⚠️ **数据**：`signal.OrientationOctaves == 0`（例：`Warpforge 6D Shake No rotation`）⇒ 恒等旋转。
        /// 幅度量级（默认信号，`Shake Hit Small`）：X/Y/Z 三条带之和 = 0.38°/0.148°/0.042°（**峰值**）；
        /// 乘 `0.5`（每带 ±0.5A）再乘 `ShakeHitRotationGain` 0.5657 ⇒ 实际 ≤ **0.11°/0.04°/0.01°**
        /// —— 很小但原版确实有（`Shake Big` 那档 gain 4.47 ⇒ 俯仰 ~0.86°，能看出来）。
        /// </summary>
        /// <param name="rotationGain">原版 `m_AmplitudeGain` = `preset.amplitude × |direction|`
        /// （`Shake Hit Small` = `ShakeHitRotationGain`）。</param>
        /// <param name="envelope">包络（0…1）—— 原版第二次 `SlerpUnclamped` 的 `gain`。</param>
        public static Quaternion ShakeOrientation(float t, float phase, float frequencyGain,
                                                  ShakeSignal signal, float rotationGain, float envelope)
        {
            if (signal == null) signal = DefaultShakeSignal;
            if (signal.OrientationOctaves <= 0) return Quaternion.identity;

            float T = t * frequencyGain + phase;
            float rx = 0f, ry = 0f, rz = 0f;
            for (int axis = 0; axis < 3; axis++)
            {
                float s = 0f;
                for (int i = 0; i < signal.OrientationOctaves; i++)
                {
                    ShakeBandSignal b = signal.Orientation(axis, i);
                    float u = b.hz * T;
                    s += b.constant != 0
                        ? b.amp * 0.5f * Mathf.Sin(u * 2f * Mathf.PI)
                        : b.amp * (Mathf.PerlinNoise(u, 0f) - 0.5f);
                }
                if (axis == 0) rx = s; else if (axis == 1) ry = s; else rz = s;
            }
            Quaternion q = Quaternion.Euler(rx, ry, rz);      // 原版：这是**度**（内部 ×π/180 走 FromEulerRad）
            q = Quaternion.SlerpUnclamped(Quaternion.identity, q, rotationGain);
            q = Quaternion.SlerpUnclamped(Quaternion.identity, q, envelope);
            return q;
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

            bool canDissolve = !ForceNoDissolveForTest && card.BeginDissolve();
            LastReturnFallback = canDissolve ? FallbackPath.MaterialDissolve : FallbackPath.HideBody;
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
                // 🔴 2026-10-23（`F1` 前置）：**兜底不再淡卡体**（不透明之后 `_Color.a` 对 3D 体无意义）。
                //   改成原版那个形状：**到点直接关 3D 体**（`SetBody3DVisible(false)`），
                //   2D 卡面照**同一条原版时间轴**打开/淡入（与上面 `canDissolve` 那一支逐时刻相同）。
                //   判据 → `资料/普查产出_1023/D3_F1判据与方案.md` §③ C4；
                //   旁证 = 原版阵亡正路 `DeathExplosion` 也是 `SetBody3DVisible(false)`（不淡）。
                seq.InsertCallback(artOn, () => { card.SetBody3DVisible(false); card.SetArtAlpha(0f); });
                seq.Insert(artFadeStart, DOTween.To(() => card.ArtAlpha, a => card.SetArtAlpha(a),
                                                    1f, artFadeEnd - artFadeStart).SetEase(Ease.Linear));
            }

            var t = CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
            // 🔴 **2026-10-23（第六十六会话 · `D5` 的发现）：回手演完要还原卡体材质。**
            //   原版 `PlayBackToHandAnimation` 末尾有 `DelayedResetMaterial(0.55)` 把 3D 体换回正常材质
            //   （见本方法头那段判据）；我们原来只喂 `_DissolveAmount` 不换回来 ⇒ 那张卡会**一直挂着溶解材质**
            //   （`OnDissolveMaterial == true`），靠「下次登场 `HandToBoard` 再 `BeginDissolve`」才兜住。
            //   `OnKill` 也补一次：补间被 kill（换局 / 视图销毁）时同样要还原。
            //   ⚠️ `CardView.EndDissolve` 自 2026-10-23（`D5`）起是**幂等且安全**的（无体 / 无材质直接返回）。
            // ⚠️ `OnComplete` 在 DOTween 里是**单槽**（再设一次会覆盖前一个）⇒ 两件事必须写在**同一个 lambda** 里。
            t.OnComplete(() =>
            {
                if (onDone != null) onDone();
                if (canDissolve) card.EndDissolve();
            });
            // `OnKill` 在「正常播完」时也会跟着触发一次 ⇒ `EndDissolve` 会被叫两次；
            // `CardView.EndDissolve` 自 2026-10-23（`D5`）起幂等（无体 / 无材质直接返回），所以安全。
            if (canDissolve) t.OnKill(() => card.EndDissolve());
            return t;
        }

        /// <summary>回手时那一下抬升的位移时长（秒）= `DAT_1834b317c` 实读 **0.208**
        /// （`CardScript._TransformUnitInPlayToCard_d__256:53-73`）。</summary>
        public const float ReturnLiftTime = 0.208f;
        /// <summary>抬升的高度 = `localScale.x × 3.0`（那个 3.0 = `DAT_1834b2e8c` 实读；
        /// 方向是 `Vector3.up`（静态字段偏移 `0x18` 实读 (0,1,0)，x/z 不动））。</summary>
        public const float ReturnLiftScale = 3.0f;

        // ==================================================================
        //  ① 手牌 → 战场（**正向** `Card Hand To Board`）—— 2026-10-21（W61 第 2 批 · 第 1 条）
        // ==================================================================
        //  判据（逐条现读）：
        //   · 播的时刻 = `MinionManager__MoveMinionToConversionPoint.c:95-99` 那条 `DOLocalMove(0.3)`
        //     的 **OnComplete**（委托 → `CardScript.CardToMinionAnim` → `BattleCardUI.PlayInvokeAnimation`）
        //     ⇒ **飞行结束那一刻**（我们 = `DeploySequence.MoveTime` 0.3）。
        //   · `BattleCardUI__PlayInvokeAnimation.c:30-40`：`Animation.Play(clip)`、`speed = 1.0`
        //     （`DAT_1834b2bb8` 实读）、并 `ToggleBody3D(true)`。
        //   · clip 本身 = `AnimationClip_-3614292623624764332.json`（17 条曲线，关键帧 → 上面 `HandToBoard*`）。
        //   · 0.55 s 的 AnimationEvent `CardHandToBoardAnimationFinished` → `DelayedResetMaterial(0.55)`
        //     → `BattleCardUI__SetCardMaterial(默认材质, initializeWithCardImage: true)`。

        /// <summary>`Board Elements` 那一组（**3D 体 + 计数器**）的 y 起始值（卡单位）。
        /// clip 的 `m_PositionCurves`：0 → −0.43、0.1667 → −1.347（停到 0.7）、0.9167 → 0。</summary>
        public const float HandToBoardBodyY0 = -0.43f;
        /// <summary>同上的低点（0.1667 / 0.7 两个关键帧都是它）= −1.347（卡单位）。</summary>
        public const float HandToBoardBodyY1 = -1.347f;

        // ---- 🔴 2026-10-22（W64 · `B04`）：补上的四条曲线（全部照 clip 关键帧原值）---------------
        // 判据（一手）：`bundle_battleprefabs_vfxandmisc_assets_all/AnimationClip/
        //   AnimationClip_-3614292623624764332.json`（`Card Hand To Board`，17 条曲线）。
        /// <summary>`2DCard/Front/CardFrame.m_Enabled` 关掉的时刻 = **0.75**（1 → 0）。
        /// ⚠️ 打开那一端用的是既有的 <see cref="HandToBoardFrameOn"/>（0.0333，上面那一组常量里已定义）。
        /// 🔴 **这条曲线的目标不是卡框的显隐**，而是挂在 `CardFrame` 上的 **`AnimateUIMaterialProperty`
        /// 组件**（`classID = 114`、`script` 与 `floatProperty1` 同一条 PPtr）⇒ 它开/关的是
        /// **卡框材质置换的窗口**（判据 → `资料/普查产出_1022/W64_B04_floatProperty1材质.md`）。</summary>
        public const float HandToBoardFrameOff = 0.75f;
        /// <summary>卡框 `_DissolveAmount` 那条曲线的**起点** = **0.0167**（0 → 0.0167 恒为 1）。
        /// ⚠️ 别和 `HandToBoardFrameOn`（0.0333，组件打开）混：曲线从 0.0167 起，但组件 0.0333 才醒
        /// ⇒ 真正开始动的值 = 曲线在 0.0333 处的值 ≈ **0.998**（差 0.18%，肉眼无差）。</summary>
        public const float HandToBoardFrameDissolveStart = 0.0167f;
        /// <summary>`ToBoardFX/Spawn Particles.m_IsActive` 0 → 1 的时刻 = **0.0167**（= 同一个数，
        /// 但语义是「粒子节点打开」；⛔ 别写成「卡框溶解起点」）。</summary>
        public const float HandToBoardSpawnParticlesOn = 0.0167f;
        /// <summary>`ToBoardFX/Spawn Particles/Trail.m_IsActive` 0 → 1 的时刻 = **0.55**
        /// （与 AnimationEvent `CardHandToBoardAnimationFinished` 同刻）。</summary>
        public const float HandToBoardTrailOn = 0.55f;
        /// <summary>`2DCard/Front/CardImage.m_LocalPosition.y` = **−0.065**（整条曲线只有一个关键帧 ⇒ 常量）。</summary>
        public const float HandToBoardArtY = -0.065f;
        /// <summary>`2DCard.m_LocalPosition.z` 的终点 = **−0.043**，在 **0.1667** 到位（0 → −0.043）。</summary>
        public const float HandToBoardFaceZ = -0.043f;
        /// <summary>`2DCard/Front/CardImage` 的 ScaleCurves：1 → 1 @**0.3333** → **0.9049578 @0.55**
        /// （= 上面 `HandToBoardShrink` 那个数的动作）。</summary>
        public const float HandToBoardArtShrinkStart = 0.3333f;

        /// <summary>🆕 2026-10-22（`B06`）：clip 那几条曲线**精确**的插值器。
        /// 每一条的 `inSlope` / `outSlope` 全 0、`weightedMode = 0` ⇒ Hermite 插值**精确等于
        /// smoothstep `3t² − 2t³`**（不是正弦、也不是任何别的缓动）。这里用一条
        /// **两端切线为 0 的 `AnimationCurve`** 表示它 —— `SetEase(curve)` 会逐点求值，
        /// 与 `AnimationClip` 采样**逐位同形**。⛔ 别改回 `Ease.InOutSine`（那是近似）。
        /// ⚠️ 曲线对象共享（DOTween 只读它），只在静态构造里建一次。</summary>
        public static readonly AnimationCurve ClipSmoothStep = BuildClipSmoothStep();
        static AnimationCurve BuildClipSmoothStep()
        {
            var c = new AnimationCurve();
            c.AddKey(new Keyframe(0f, 0f, 0f, 0f));   // inTangent = outTangent = 0 ⇒ Hermite 退化成 smoothstep
            c.AddKey(new Keyframe(1f, 1f, 0f, 0f));
            return c;
        }

        /// <summary>「登场溶解不可用」只出声一次（别每张登场都刷一行）。</summary>
        static bool _warnedHandToBoardDissolve;
        /// <summary>「2D 卡框溶解不可用」只出声一次（同上）。</summary>
        static bool _warnedFrameDissolve;

        /// <summary>**登场正向 `Card Hand To Board`**（原版 0.3 s 起播、全长 **0.9167 s**）。
        ///
        /// <para>覆盖到的那几条曲线（全部按 clip 的关键帧与切线喂值）：
        /// · `Board Elements.m_IsActive` 0→1 @**0.1667**（3D 体在那一刻出现）；
        /// · `Board Elements/3DBody/Card 3D` 的 `material._DissolveAmount` **1→0** @[0.1667, 0.7]
        ///   （两端切线为 0 ⇒ Hermite = smoothstep，这里用 `InOutSine` 近似）；
        /// · `Board Elements` 的 y：−0.43 → −1.347 @0.1667 · 停到 0.7 · → 0 @0.9167；
        /// · `2DCard.m_Alpha` **1→0** @[0.3333, 0.6667]；
        /// · `2DCard.m_IsActive` **1→0 @0.7** + `Base Attack/Health Counters` 的 alpha **→1 @0.7**
        ///   （= `SetFace(Board)` 那一刻）+ 换回正常材质（原版 0.55 的 `DelayedResetMaterial`）。
        /// </para>
        ///
        /// <para>🔴 **我们这一侧的差异（如实记，⛔ 别当成原版）**：
        /// · 我们的卡没有 `Animation` 组件 ⇒ 按同一条时间轴**用代码喂值**（同 <see cref="ReturnToHand"/>）；
        /// · 原版 `2DCard` 是一个 `CanvasGroup`（一处 α 管整组），我们的 2D 层是**逐层材质** ⇒
        ///   用 `CardView.SetAlpha2D`（2026-10-22 新加：**只乘 2D 那一套、跳过 3D 卡体**，
        ///   与 `2DCard` 的 CanvasGroup 同义）。
        /// · ✅ **2026-10-22（`B05`/`B06`/`B04`）这几条近似已换成原版真值**：
        ///   ① 材质复位 **0.7 → 0.55**（原版 AnimationEvent `CardHandToBoardAnimationFinished`
        ///      @0.55 → `DelayedResetMaterial(0.55)`）；② 三处缓动 `InOutSine` → `ClipSmoothStep`
        ///      （clip 切线全 0 ⇒ Hermite 精确 = smoothstep）；③ 补上 `CardFrame.m_Enabled` /
        ///      `CardImage` 的 y 与 scale / `2DCard.z` 四条曲线；④ **`floatProperty1` 已实现**：
        ///      0.0333 把卡框换成 `SpriteUI Frame Dissolve` 实例（`CardView.BeginFrameDissolve`）、
        ///      `_DissolveAmount` 1→0 over [0.0167, 0.7]、0.75 还原并销毁（判据 →
        ///      `资料/普查产出_1022/W64_B04_floatProperty1材质.md`）。
        /// · ✅ **`ToBoardFX` / `Spawn Particles` / `Trail` 已做**（`W64 B04` 尾巴②；见下面 `②'''`
        ///   那一段与 <see cref="HandToBoardSpawnParticlesOn"/> / <see cref="HandToBoardTrailOn"/>）。
        ///   ⚠️ **2026-10-23 更正（铁律 5）**：原来这里写「**仍然没做的曲线**：`ToBoardFX` /
        ///   `Spawn Particles` / `Trail`（原版是卡预制体自带的粒子节点，我们这套卡没有 ⇒ 另账）」
        ///   —— **已过期，错因是 B04 做完后没回来改这一行**。实况：
        ///   ① `ToBoardFX` 自己恒 1（原版曲线只有一个键）⇒ 不需要动；
        ///   ② `SetSpawnParticlesActive` / `SetTrailActive` 在 0.0167 / 0.55 拨开关；
        ///   ③ 实例由驱动层在登场那一刻挂上来 —— `CardView.MountSpawnParticles` ←
        ///      `BattleDriver.MountSpawnParticlesUnderToBoardFx`（`StartHandToBoardClip` 里，早于 clip 起播），
        ///      ⇒ 「我们这套卡没有粒子节点」这句也不再成立。
        /// </para>
        ///
        /// <para>⚠️ **3D 体是懒建的**（`SetFace(Board)` 才建）⇒ 这里先借它把体建出来、再立刻退回原形态：
        /// 原版 `2DCard` 要到 0.7 才关，一直藏着就演不出「2D 淡出 + 3D 溶解」的交叉。</para>
        /// </summary>
        public static Tween HandToBoard(CardView card, System.Action onDone = null)
        {
            if (card == null) return null;
            var tr = card.transform;
            // 已经是场上形态 = 没有 2D 卡面可淡 ⇒ 这条 clip 不适用（调用方按 null 走老路）
            if (card.Face == CardFace.Board) return null;

            // ① 先借此卡的 `Board` 形态把 3D 体 / 数值层建出来，然后马上退回原形态（见上面那段）
            var prevFace = card.Face;
            card.SetFace(CardFace.Board);
            card.SetFace(prevFace);
            card.SetBody3DVisible(false);

            var body = tr.Find("body3D");
            Vector3 bodyBase = body != null ? body.localPosition : Vector3.zero;
            if (body == null)
                Debug.LogWarning("[CardFeel] 登场 clip：这张卡建不出 `body3D`（3D 卡体）⇒ "
                               + "`Board Elements` 的位移与 `_DissolveAmount` 两段**不演**"
                               + "（原版一定有 3D 体，判据 = clip 的 `Board Elements/3DBody/Card 3D` 曲线）");

            var seq = DOTween.Sequence();
            seq.AppendInterval(HandToBoardEnd);     // 先把总长定成 0.9167；下面全是 Insert（不受游标影响）

            // ① 🆕 `B04`：`CardImage.m_LocalPosition.y = −0.065` 是**常量曲线** ⇒ 开场就摆好；
            //    它的缩放从 1 起（scale 曲线 0 → 1，第一段是常量）。
            card.SetArtLocalY(HandToBoardArtY);
            card.SetArtLocalScale(1f);
            // ② 🆕 `B04`：`CardFrame.m_Enabled`（= 那个 `AnimateUIMaterialProperty` **组件**）
            //    0 → 1 **@0.0333** ⇒ 把卡框换成**溶解材质实例**（原版 `OnEnable` → `CreateInstanceMaterial`）。
            seq.InsertCallback(HandToBoardFrameOn, () =>
            {
                bool ok = card.BeginFrameDissolve();
                if (!ok && !_warnedFrameDissolve)
                {
                    _warnedFrameDissolve = true;
                    Debug.LogWarning("[CardFeel] 登场 clip：2D 卡框溶解不可用（取不到原版 shader "
                                   + "`Everguild/FX/Sprite Dissolve Mask`，或这张卡没有卡框层）"
                                   + " ⇒ 卡框**不溶**、照常显示；与原版不一致（不静默）");
                }
            });
            // ②' 🆕 `B04`：`floatProperty1.value`（`_DissolveAmount`）1 → 0 over **[0.0167, 0.7]**。
            //     ⚠️ 起止用的是**曲线**的关键帧（0.0167 / 0.7），不是组件开关那两个时刻。
            seq.Insert(HandToBoardFrameDissolveStart,
                       DOTween.To(() => 1f, v => card.SetFrameDissolveAmount(v), 0f,
                                  HandToBoardDissolveEnd - HandToBoardFrameDissolveStart)
                              .SetEase(ClipSmoothStep));
            // ②'' 🆕 `B04`：`CardFrame.m_Enabled` 1 → 0 **@0.75** ⇒ 还原原材质 + 销毁实例
            //      （原版 `OnDisable` → `ResetToOriginalMaterial`，`OnDestroy` → `Destroy(instancedMaterial)`）。
            seq.InsertCallback(HandToBoardFrameOff, () => card.EndFrameDissolve());

            // ②''' 🆕 `B04`：`ToBoardFX` 那两颗粒子的开关（clip 的三条 `m_IsActive`）。
            //   · `ToBoardFX` 自己**恒 1**（原版曲线只有 t=0 → 1 一个键）⇒ 不动；
            //   · `ToBoardFX/Spawn Particles` 0 → 1 **@0.0167**；
            //   · `ToBoardFX/Spawn Particles/Trail` 0 → 1 **@0.55**（与 `CardHandToBoardAnimationFinished` 同刻）。
            //   实例由驱动层在登场那一刻挂上来（原版 `CardSetup`），这里只拨开关。
            seq.InsertCallback(HandToBoardSpawnParticlesOn, () => card.SetSpawnParticlesActive(true));
            seq.InsertCallback(HandToBoardTrailOn, () => card.SetTrailActive(true));

            // ②'' 🆕 `B04`：`CardImage` ScaleCurves 1 → 1 @0.3333 → 0.9049578 @0.55
            seq.Insert(HandToBoardArtShrinkStart,
                       DOTween.To(() => 1f, v => card.SetArtLocalScale(v), HandToBoardShrink,
                                  HandToBoardDone - HandToBoardArtShrinkStart)
                              .SetEase(ClipSmoothStep));

            // ②''' 🆕 `B04`：`2DCard.m_LocalPosition.z` 0 → −0.043 @0.1667
            seq.Insert(0f, DOTween.To(() => 0f, v => card.SetFaceRootLocalZ(v), HandToBoardFaceZ,
                                      HandToBoardLand).SetEase(ClipSmoothStep));

            // ③ 0.1667：3D 体出现 + 开始溶解（`m_IsActive 0→1` 与 `_DissolveAmount 1→0` 同刻起）
            seq.InsertCallback(HandToBoardLand, () =>
            {
                card.SetBody3DVisible(true);
                bool ok = card.BeginDissolve();
                if (!ok && !_warnedHandToBoardDissolve)
                {
                    _warnedHandToBoardDissolve = true;
                    Debug.LogWarning("[CardFeel] 登场 clip：卡体溶解不可用（取不到原版 shader "
                                   + "`Everguild/Cards/3D Card Dissolve`，或这张卡没有 3D 体）"
                                   + " ⇒ 3D 体**直接出现**，与原版的「溶解出」不一致（不静默）");
                }
                if (ok) card.SetDissolveAmount(1f);
            });
            // ⚠️ 起点用**捕获的局部变量**（不是 `card.DissolveAmount`）—— `DOTween.To` 的起点是它**开始那一刻**
            //    去读 getter 的，而同一刻的 `InsertCallback` 刚把溶解材质装好（`BeginDissolve` 会把量归 0）⇒
            //    直接读 getter 会把起点读成 0（= 一开场就全实）。局部量 = 原版曲线的 1。
            float dissolveFrom = 1f;
            seq.Insert(HandToBoardLand,
                DOTween.To(() => dissolveFrom, v => { dissolveFrom = v; card.SetDissolveAmount(v); }, 0f,
                           HandToBoardDissolveEnd - HandToBoardLand).SetEase(ClipSmoothStep));

            // ④ `Board Elements` 的 y：−0.43 → −1.347 →（停）→ 0
            if (body != null)
            {
                seq.Insert(0f, BodyYTo(body, bodyBase.y + HandToBoardBodyY0,
                                       bodyBase.y + HandToBoardBodyY1, HandToBoardLand));
                seq.Insert(HandToBoardDissolveEnd,
                           BodyYTo(body, bodyBase.y + HandToBoardBodyY1, bodyBase.y,
                                   HandToBoardEnd - HandToBoardDissolveEnd));
            }

            // ⑤ `2DCard.m_Alpha` 1 → 0（[0.3333, 0.6667]）—— 起点同样是**捕获量**（原版那条曲线从 1 起）
            float alphaFrom = 1f;
            seq.Insert(HandToBoardFadeStart,
                DOTween.To(() => alphaFrom, v => { alphaFrom = v; card.SetAlpha2D(v); }, 0f,
                           HandToBoardFadeEnd - HandToBoardFadeStart).SetEase(ClipSmoothStep));

            // ⑥ 🔴 `B05`：**0.55** 换回正常材质 —— 原版 AnimationEvent `CardHandToBoardAnimationFinished`
            //    在 0.55 → `DelayedResetMaterial(0.55)`。原来放在 0.7 是因为整卡 `SetAlpha` 会把 3D 体
            //    也再淡一遍；现在 2D 那一档走 `SetAlpha2D`（跳过 3D 体）⇒ 可以照原版 0.55 换。
            seq.InsertCallback(HandToBoardDone, () =>
            {
                card.SetDissolveAmount(0f);
                card.RestoreOriginalMaterial();
            });

            // ⑦ 0.7：`2DCard.m_IsActive` 关 + 计数器 α→1（= 换到场上形态）+ 两档 α 复位
            seq.InsertCallback(HandToBoardDissolveEnd, () =>
            {
                card.SetFace(CardFace.Board);
                card.SetDissolveAmount(0f);
                card.RestoreOriginalMaterial();
                card.SetAlpha2D(1f);
                card.SetAlpha(1f);
            });

            var t = CardTween.Use(seq, Ease.Linear, tr);
            // ⚠️ 被打断时**两档 α 都要复位**（同 `DealIn` 那条注释）：2D 那一档跑到一半被 kill 的话
            //    这张卡会**永久半透明**（静默）。复位成 1 是安全的一侧 —— 正常跑完时它本来就是 1。
            t.OnKill(() => { if (card != null) { card.SetAlpha2D(1f); } });
            if (onDone != null) t.OnComplete(() => onDone());
            return t;
        }

        /// <summary>把 <paramref name="body"/> 的**局部 y** 从 <paramref name="from"/> 补到
        /// <paramref name="to"/>（秒）。clip 里那两条位置曲线的两端切线都是 0
        /// （`inSlope`/`outSlope` 全 0、`weightedMode = 0`）⇒ Hermite 插值**精确等于 smoothstep**，
        /// 这里用 <see cref="ClipSmoothStep"/>（2026-10-22 `B06`：原来的 `InOutSine` 是近似）。</summary>
        static Tween BodyYTo(Transform body, float from, float to, float dur)
        {
            if (body == null || dur <= 0f) return null;
            // ⚠️ 起点用**捕获量**：`DOTween.To` 的起点是在 tween **开始那一刻**读 getter 的，
            //   而同一串里前一条 tween 早把 `localPosition.y` 改成别的值了 ⇒ 读 getter 会拿到错的起点。
            float y = from;
            return DOTween.To(() => y,
                              v => { y = v; var p = body.localPosition; p.y = v; body.localPosition = p; },
                              to, dur).SetEase(ClipSmoothStep);
        }

        /// <summary>缩放补间 —— **只经 `CardView.SetBaseScale` 这一个口**（⛔ 不写 `transform.localScale`）。
        /// <para>⚠️ 原版那一侧**没有**「基础 × 高亮」这一层（`DOScale` 写的就是最终 `localScale`）；
        /// 我们的等价物必须把 `_baseScale` 一起带过去，否则补间一结束，任何一次 `SetPose` /
        /// `SetHighlightScale`（→ `ApplyScale`）都会把卡**弹回旧缩放**（静默 —— `T04` 那一族）。</para>
        /// <para>拿不到 `CardView` 时**不做缩放**并出声一次（⛔ 不退回 `tr.DOScale`：那会重新制造
        /// 「`_baseScale` 变陈」这条缺陷，而这个入口存在的意义就是堵它）。</para></summary>
        static Tween ScaleTo(CardView card, Transform tr, float toBase, float dur, Ease ease)
        {
            if (card == null)
            {
                if (!_warnedNoCardView)
                {
                    _warnedNoCardView = true;
                    Debug.LogWarning("[CardFeel] 缩放补间：这个 transform 上没有 `CardView`（"
                                   + (tr != null ? tr.name : "null") + "）⇒ **不做缩放**"
                                   + "（不退回直写 `transform.localScale`，见 `ScaleTo` 的注释）");
                }
                return null;
            }
            if (dur <= 0f) { card.SetBaseScale(toBase); return null; }
            // ⚠️ 起点用**捕获量**：`DOTween.To` 的起点是在它开始那一刻读 getter 的，
            //   而同一条 Sequence 里前面那几段可能已经改过缩放（同 `BodyYTo` 那条注释的坑）。
            float v = card.BaseScale;
            return DOTween.To(() => v, x => { v = x; if (card != null) card.SetBaseScale(x); },
                              toBase, dur).SetEase(ease);
        }

        /// <summary>「**可见**缩放」（= `localScale.x`，含高亮系数）→ `CardView` 的**基础缩放**。
        /// 只给「调用点传的是 `localScale` 现读值」的那几处用（见 `AttackRecover` 的 `<paramref name="scale"/>`）。</summary>
        static float ToBaseScale(CardView card, float visible)
        {
            if (card == null) return visible;
            float mul = card.HighlightMul;
            return mul > 1e-4f ? visible / mul : visible;
        }

        static bool _warnedNoCardView;

        /// <summary>**远程攻击的收招** —— 原版是**两段**，我们原来只有第一段。
        ///
        /// <para>**段1**（原版 `CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c:80-104`）：
        /// 一个 `Sequence` 里三条**同时**（时长全是 `attackStepTime` = 0.1，无 `SetEase` ⇒
        /// DOTween 默认 `OutQuad`）：`DOLocalMove(handHomePos)` + `DORotateQuaternion(identity)`
        /// + `DOScale(DefaultScale)`；然后 `WaitForCompletion`（= 这三条跑完），再
        /// `WaitForSeconds(attackStepTime)` 0.1 ⇒ **合计 0.2 s 才发 VFX**
        /// （我们的 `EventTiming.AttackStepRanged` 正是这 0.2）。</para>
        ///
        /// <para>🆕 **段2**（2026-10-23 · 战斗线 **新账-73**）—— 原版 `CardScript.AfterAttack`
        /// （`CardScript__AfterAttack.c`，调用点 `_ResolveRangedAttack_d__364__MoveNext.c:39`，
        /// 在段1 那条协程**跑完之后**才调）：
        /// `DOLocalMove(transform, 静止位, attackStepTime)` + `SetDelay`（= <see cref="AfterAttackDelay"/> **0.65**）
        /// + `OnUpdate: localScale = MinionScaleByPositionSO.GetScale(isPlayer, localPosition, defaultScale)`
        /// + `OnComplete: FinishAfterAttack`（`CardScript__FinishAfterAttack.c`：
        /// `set_localPosition(静止位)` / `set_rotation(identity)` / `set_localScale(DefaultScale)`）。
        /// 🔴 **我们原来整条缺**⇒ 丢的正好是带**深度缩放**的那一段（段1 只有 `DOScale` 一路补到静止值）。
        /// 深度缩放的三处消费点原来只有段1 那一处的近战版接了，这一处是第二处（见 `DepthScaleFactor` 那节）。</para>
        ///
        /// <para>🔴 **2026-10-23（新账-74）**：缩放**一律经 `CardView.SetBaseScale`**（`ScaleTo`）——
        /// 原来的 `DOScale` 直写 `transform.localScale` ⇒ `_baseScale` 变陈，与刚接上的深度缩放迟早打架。</para></summary>
        /// <param name="scale">**可见**缩放（= `transform.localScale.x` 那一档，**含**高亮系数）。
        /// 调用点传的就是 `v.transform.localScale.x`（`BattleDriver.PlayAttackFeel` /
        /// `ScheduleRetaliationPresentation`）⇒ 内部先 ÷ `HighlightMul` 换成基础缩放再写。</param>
        /// <param name="isPlayer">原版是卡自己的 `isPlayer` 字段（`CardScript+0x40`）；不传 ⇒ 按静止位
        /// 离哪一行近来判（`IsPlayerRow`，与 `MeleeAttack` 同一个兜底）。</param>
        public static Tween AttackRecover(Transform tr, Vector3 home, float scale, float delay = 0f,
                                          bool? isPlayer = null)
        {
            if (tr == null) return null;
            var card = tr.GetComponent<CardView>();
            float toBase = ToBaseScale(card, Mathf.Max(1e-4f, scale));   // 静止/默认缩放（= 原版 `DefaultScale`）

            var seq = DOTween.Sequence();
            // ---- 段1：回正三条（同时，0.1 s）----
            seq.Append(tr.DOLocalMove(home, AttackStepTime));
            seq.Join(tr.DORotateQuaternion(Quaternion.identity, AttackStepTime));
            var s1 = ScaleTo(card, tr, toBase, AttackStepTime, Ease.OutQuad);
            if (s1 != null) seq.Join(s1);
            // ---- 段2：原版 `AfterAttack`（0.65 s 之后再来一段 0.1 s 的归位 + 逐帧深度缩放）----
            seq.AppendInterval(AfterAttackDelay);
            seq.Append(tr.DOLocalMove(home, AttackStepTime));

            var used = CardTween.Use(seq, Ease.Linear, tr);
            // 深度缩放只挂在**段2**那 0.1 s 上（`activeFrom` = 两段的分界 0.75 s）；
            // 静止缩放**显式**给 `toBase` —— 段2 的 OnUpdate 是拿它当底数的，⛔ 不能拿那一刻的 `BaseScale`
            //（段1 的缩放补间刚把它写成别的值 ⇒ 会复利）。
            HookDepthScale(used, tr, isPlayer, AttackStepTime + AfterAttackDelay, toBase);
            // ---- 原版 `FinishAfterAttack`：收尾把位姿钉回静止态 ----
            //   ⚠️ 缩放那一半由 `HookDepthScale` 的 `OnKill` 写回（同值）；这里只补位置与朝向。
            used.OnComplete(() =>
            {
                if (tr == null) return;
                tr.localPosition = home;
                tr.rotation = Quaternion.identity;      // 原版是 `set_rotation`（世界），不是 local
                if (card != null) card.SetBaseScale(toBase);
            });
            return used.SetDelay(delay);
        }

        // ==================================================================
        //  阵亡（原版 `CardScript.UnitDeath`）—— **不是把场上的卡溶解掉**
        // ==================================================================
        // 判据（2026-09-29 逐句读过；🔴 原文那条「阵亡 = 材质溶解 `_DissolveAmount`」**已作废** ——
        // 那是把**回手 / 出战**那条链的机制安到了阵亡头上）：
        //   ① 卡自己抖一下：`DOShakePosition(时长 = 小兵 `deathTimeMinionDuration` 0.2 / 督军 0.5,
        //      强度 **(0.3, 0.05, 0)**, vibrato **10**, randomness **90**, fadeOut **true**)`（`:100-116`）
        //      —— ⚠️ **这是【第①拍】**（state 0），**这时那张卡的 3D 体还开着**
        //   ② `WaitForSeconds(0.5)`（state 0 尾巴，`:120-122`）**之后**才：
        //      场上那张卡的 3D 体**关掉**（`battleCardUI.body3D(+0x158).SetActive(false)`，`:330-333`）
        //      + 在卡位**另生成**死亡爆散体：`Instantiate(cardDestroyFX(+0x1D8), 卡位置, 3D体旋转)`
        //      （`:194-347`），那件自带 Animator（controller `Card 3D WH40K Explosion`：1 layer / 1 state /
        //      0 参数）播一条 **0.8167 s** 的 clip `Card Explosion`；材质 `Card 3d WH40K Explosion`
        //      **从卡上借贴图**、外加 `_ExplosionTextureOffsetModifier = Random.value`（所以每次爆散纹理偏移都不同）
        //   ③ 再 `WaitForSeconds(0.2)`（`:374-379`）→ ④ `GoToCemetery()`（`:152`）（另有两条音效）
        // 🔴 **2026-10-16 订正**：上面 ①② 的**顺序原来写反了** —— 原文把「关 3D 体 + 生成爆散体」摆在
        //    第①位、抖动摆在第③位，实据是**抖动在前、0.5 s 之后才生成爆散体**（`UnitDeath` 是 state 机，
        //    第一处 `Instantiate` 在 `:233`、而它属于 **state 1**）。⇒ 我们原来的实现是「死的瞬间就爆散、
        //    0.2 s 后卡没了」，原版是「先抖、先站 0.5 s，再爆散，0.7 s 才走」。
        //    错因：只按 `.c` 里的**行号顺序**读，没看那段属于哪个 state（`param_1 + 0x10` 那个游标）。
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

        // ------------------------------------------------------------------
        //  🆕 2026-10-16：**阵亡之后那三拍**（原版 `CardScript.UnitDeath` 的 ④ 那一段）
        // ------------------------------------------------------------------
        //  原版时间轴（`CardScript._UnitDeath_d__446__MoveNext.c`，行号现核；**state 机**）：
        //    ① state 0（:80-127）：`SetCardStateInCemetery` → 死亡音效 → `DOShakePosition`
        //       → `WaitForSeconds（gVars + 0x34）` → state 1
        //    ② state 1 前半（:200-333）：**这时才**生成爆散体 + `body3D.SetActive(false)` + 播 3D 音
        //    ③ state 1 后半（:374-379）：`StopAnimsInPlay` → `WaitForSeconds（gVars + 0x8c）` → state 2
        //    ④ state 2（:152）：`GoToCemetery()`
        //  ⇒ **爆散体不是「死的瞬间」放的，是 0.5 s 之后**（我们原来是 t=0 放，
        //     所以「先留一拍」这一半从来没出现过 —— 见 `W_卡牌飞行族普查.md` §一 #8）。

        /// <summary>第①拍到第②拍之间的等待 = `WaitForSeconds(gVars + **0x34**)` = **0.5 s**
        /// （`CardScript._UnitDeath_d__446__MoveNext.c:117-122`；`+0x34` = `VarsGlobal.deathTimeWarlordDuration`，
        /// 字段名从 `il2cpp_out/dump.cs` 的 `VarsGlobal` 块按偏移实读）。
        /// 🔴 **这一句不分档**：它在 `if (cardType == 10)` 那个分支的**外面**（`:100-109` 才是分档 —— 那是
        /// **抖动**时长：督军 0.5 / 小兵 0.2，见 `DeathDissolve`）⇒ 小兵抖完 0.2 s 之后**还要多站 0.3 s**
        /// 才轮到爆散体。⛔ 别因为「小兵 0.2」就把这里也改成分档 —— 那是把上一拍的值抄到了下一拍。</summary>
        public const float DeathHoldBeforeFx = 0.5f;

        /// <summary>第②拍到第④拍之间的等待 = `WaitForSeconds(gVars + **0x8c**)` = `VarsGlobal.minionDeathTime`
        /// **0.2 s**（`CardScript._UnitDeath_d__446__MoveNext.c:374-379`）。
        /// ⇒ 阵亡视图从「引擎说它死了」到被收掉一共 **0.9 s（小兵）/ 1.2 s（督军）**。</summary>
        public const float DeathFxHold = 0.2f;

        /// <summary>墓地卡位的**世界**坐标 = `VarsGlobal.cardInCemeteryPos`（偏移 0x98，**Vector2**）
        /// 实读 **(−8.14, −0.2)** —— `CardScript__SetAsCemetery.c:35-41` 把它原样 `set_position` 上去。
        /// ⚠️ **软连接**：它算「我们这套 HUD 世界系」（可见高 10 单位，`LayoutSpace.DesignHeight`）里
        ///    —— 原版那台 HUD 相机的 `orthographicSize` 本地没读到（`cardInCemeteryPos` 与卡位同系
        ///    这一条是**推定**，依据是同一句 `set_position` 吃的是 `CardScript.transform`）。
        ///    按 16:9 半宽 8.889 算，−8.14 落在**左边缘内侧约 81 设计 px**、y 基本居中。</summary>
        public const float CemeteryCardX = -8.14f;
        public const float CemeteryCardY = -0.2f;
        /// <summary>墓地里那张卡的缩放 = `VarsGlobal.cardInCemeteryScale` 实读 **0.65**
        /// （`CardScript__SetAsCemetery.c:66-78`）。⭐ 注意它是**绝对**缩放（`Vector3.one × 0.65`），
        /// 不是「在原缩放上乘 0.65」—— 原版那句是 `DOScale(one × 系数, …)`。</summary>
        public const float CemeteryCardScale = 0.65f;
        /// <summary>进墓地那一下的缩放用时 = `VarsGlobal.growInCemeteryTime` 实读 **0.3 s**，
        /// `SetEase(9)` = DOTween **`OutCubic`**（`CardScript__SetAsCemetery.c:75-77`；
        /// 「9 = OutCubic」的判据：同族 `SetEase(0xc)` 在 `CardScript._AttackMeleeAnim…:266-267`
        /// 就是我们 `CardFeel.cs` 段4 写的 `Ease.OutQuart` ⇒ 两个值互相印证）。</summary>
        public const float CemeteryGrowTime = 0.3f;

        /// <summary>阵亡那条链的**时刻表**（秒，相对 <see cref="DeathExplosion"/> 起播）——
        /// 「什么时候发生什么」的**判据只此一处**。
        /// <para>🔴 **2026-10-21（W61 战斗线「新账-65」第 1 批 · 第 1 条）**：三拍改成**并行起算** ——
        /// 原版 `DOShakePosition` 之后**没有** `yield return` 它，紧接着就是
        /// `WaitForSeconds(gVars + 0x34)`（`CardScript._UnitDeath_d__446__MoveNext.c:114-126`，
        /// 抖动那两行 `:110-116`、等待那几行 `:117-126`）⇒ **爆散体恒在 +0.5 s**，与抖动时长无关。
        /// 我们原来写成 `Append(shake) → AppendInterval(0.5)`（串行）⇒ 爆散体 +0.7（小兵）/ +1.0（督军）、
        /// 总寿命 0.9 / 1.2 —— **那 0.2/0.5 s 的偏差就是这条修掉的**。</para></summary>
        public struct DeathBeats
        {
            /// <summary>抖动**结束**的时刻（小兵 0.2 / 督军 0.5）。⚠️ 它**不是**任何一拍的开始 —— 原版不等它。</summary>
            public float ShakeEnd;
            /// <summary>关 3D 体 + 在卡位生成爆散体（原版 state 1）。**恒 0.5**（`deathTimeWarlordDuration`，不分档）。</summary>
            public float BodyAt;
            /// <summary>`GoToCemetery`（原版 state 2）。恒 **0.7**（= 0.5 + `minionDeathTime` 0.2）。</summary>
            public float CemeteryAt;
        }

        /// <summary>阵亡三拍的时刻表。判据见 <see cref="DeathBeats"/> 的注释（`_UnitDeath_d__446:110-126,374-379`）。</summary>
        public static DeathBeats DeathBeatsOf(bool isWarlord)
        {
            DeathBeats b;
            b.ShakeEnd = DeathDissolve(isWarlord);
            b.BodyAt = DeathHoldBeforeFx;
            b.CemeteryAt = DeathHoldBeforeFx + DeathFxHold;
            return b;
        }

        /// <summary>阵亡视图的总寿命（秒）—— 从「引擎说它死了」到被收掉。
        /// = <see cref="DeathBeats.CemeteryAt"/> = **0.7 s，小兵/督军同**（抖动与那 0.5 s 等待同帧起算）。
        /// **判据只此一处**（自检要按它推时间轴，别各写一份）。
        /// ⚠️ 参数保留只是为了让调用点不用改、并提醒「这一档现在**不分档**」——
        ///   分档只体现在 <see cref="DeathBeats.ShakeEnd"/>（抖动长度），而抖动**不影响**总寿命。</summary>
        public static float DeathTotal(bool isWarlord)
        {
            return DeathBeatsOf(isWarlord).CemeteryAt;
        }

        /// <summary>最近一次 <see cref="DeathExplosion"/> 里「爆散体生成」那一刻的**相对时刻**（秒，相对起播）。
        /// 自检用它钉「抖动与那 0.5 s 等待**同帧起算**」—— 改回串行时这里会变成
        /// `DeathDissolve + 0.5`（小兵 0.7 / 督军 1.0），断言当场红。</summary>
        public static float LastDeathBodyAt { get; private set; }

        /// <summary>
        /// **阵亡**：卡原地抖（**不等它**）→ **同帧起算的 0.5 s 之后**在卡位生成死亡爆散体（并把 3D 体关掉）
        /// → 再 0.2 s 后 `GoToCemetery` 把它挪到墓地卡位 ⇒ **总 0.7 s，小兵/督军同**。
        ///
        /// 时间轴 / 每段的判据见上面那一段头注与 <see cref="DeathBeats"/>（原版是四拍协程，我们这里是一串 `Sequence`）。
        /// 取不到那件爆散体时**退回** <see cref="Dissolve"/>（旧表现）并出声。
        /// 🆕 2026-10-22（P6 · M1）：`collectedWaystone`（收集灵魂石带出来的那条 `Death`）⇒ **回 `null`** ——
        /// 调用方当场把视图收掉（原版 `CollectWaystoneEffect` 末尾就是 `Destroy(残骸体)`，没有 `UnitDeath` 时间轴；
        /// 收集粒子由 `EvtKind.CollectWaystone` 那条事件自己放）。
        /// ⚠️ 收掉视图仍是**调用方**那件事（原版是 `CemeteryManager.AddCardToCemetery` 尾巴上的
        /// `SetActive(false)`，我们这边是 `onDone` → `Kill`）。
        /// </summary>
        public static Tween DeathExplosion(CardView card, float delay = 0f, System.Action onDone = null,
                                           bool isWarlord = false, RuleEngine.CardDef def = null,
                                           bool collectedWaystone = false)
        {
            if (card == null) return null;

            // 🔴 2026-10-16：**先把「这一张该用哪件爆散体」定下来**，再排时间轴 ——
            //   原版那句话在第②拍（0.5 s 之后）才执行，而我们得**事先**决定走真件还是退回路
            //   （退回路是不一样的一条 `Dissolve` 时间轴）。挑件的判据全在 `PickDeathFx` 里。
            // 🆕 2026-10-22（P6 · M2/M3）：`def` / `collectedWaystone` 两个形参把「残骸被打掉」
            //   与「收集路标石带出来的消失」两条原版分支带进来（判据见 `PickDeathFx`）：
            //     · `def` = 这一张的 `CardDef`（判「是不是残骸那一族」用 —— 与 `PlayRemnantSfx` 同一份判据）；
            //     · `collectedWaystone` = 这条 `Death` 是 `CollectWaystone` 那一下带出来的 ⇒ **一件都不播**。
            var pick = PickDeathFx(card, def, collectedWaystone);
            // ① 🆕 2026-10-22（P6 · M1/M2）：**收集路标石带出来的残骸消失 ⇒ 当场收掉、一件不播。**
            //   原版 `RemnantAeldari__CollectWaystoneEffect.c` 末尾就是 `Destroy(残骸体)` ——
            //   它**根本不进** `CardScript.UnitDeath`（那 0.5 s 站桩 + 0.2 s + 墓地都不该有），
            //   收集粒子由 `EvtKind.CollectWaystone` 那条事件自己放（见 `VfxMap.ByEvent` 那一行）。
            //   回 `null` ⇒ 调用方 `PlayDeathFeel` 走 `tw == null` 那一支**当场 `Kill`** 这个视图。
            if (pick.Suppressed) return null;
            if (pick.Fx == null)
            {
                // 取不到那件真爆散体 ⇒ 用**当初的替代品**顶上（`Explosion Fenrisian Monstrosities`）——
                // 它原来挂在 `BattleDriver.PlaySignal` 的 `EvtKind.Death` 那一格上，2026-10-01 那件真件
                // 进来了、从那里摘掉（否则两件同时播 = 原版没有的第二下），挪到这条退回路里。
                // （`PickDeathFx` 已经在上面**出过声**了。）
                // 🔴 **2026-10-16（`A2080` 普查顺手查出的真缺陷）就地修**：这里原来写的是
                //    `Play(VfxMap.Death, …)` —— 而 **`VfxMap.Death` 是【事件名】常量（`"death"`）**，
                //    ⛔ **不是 prefab 名** ⇒ 那一跳**恒不命中**、每次都打一行
                //    「效果库里没有 'death'」（实机日志实测 13 条）。
                //    ⇒ 按注释里写的本意改成走**兜底链**（`Resolve` → 我们自己挑的替代品
                //    `Explosion Fenrisian Monstrosities`，见 `PickDeathFx` 的 doc）。
                PlayDeathFallbackFx(card.transform);
                return Dissolve(card, delay, onDone, isWarlord);      // 旧表现：淡出 + 上浮 + 缩
            }

            var tr = card.transform;
            var beats = DeathBeatsOf(isWarlord);
            var seq = DOTween.Sequence();
            // ①② **同帧起算**（🔴 2026-10-21 · W61 第 1 批 · 第 1 条）：
            //   原版 `DOShakePosition` **不等**它 —— 紧接着的那一句就是 `WaitForSeconds(gVars + 0x34)`
            //   （`CardScript._UnitDeath_d__446__MoveNext.c:110-126`）⇒ 爆散体落在 **+0.5 s**。
            //   ⛔ 别改回 `Append(shake) → AppendInterval(0.5)`：那是串行，爆散体会晚
            //      0.2 s（小兵）/ 0.5 s（督军），总寿命变 0.9/1.2（本批修掉的正是它）。
            seq.AppendInterval(beats.BodyAt);
            // ① 抖（原版 state 0）—— ⚠️ **这时 3D 体还开着**（原版到第②拍才 `SetActive(false)`）；
            //    用 `Insert(0f, …)` 让它与上面那句 `AppendInterval` **同帧起播**（= 原版的不 await）。
            seq.Insert(0f, tr.DOShakePosition(DeathDissolve(isWarlord), new Vector3(DeathShakeX, DeathShakeY, 0f),
                                              (int)DeathShakeVibrato, DeathShakeRandomness, false, true));
            // ② 这一帧才生成爆散体、关 3D 体
            seq.AppendCallback(() =>
            {
                card.SetBody3DVisible(false);
                bool ok = pick.Remnant ? SpawnRemnantDestroy(card, pick.Fx)
                                       : SpawnDeathBody(card, pick.PerCard, pick.Fx);
                if (!ok)
                    Debug.LogWarning($"[CardFeel] `{pick.Fx}` 这一刻放不出来（`WarpforgeEffectPlayer.Play` 返回空）"
                                   + " ⇒ 这一张阵亡**没有爆散体**（原版 `UnitDeath` state 1 会 `Instantiate` 它）");
            });
            // ③ 再等 `minionDeathTime`（原版还有一句 `StopAnimsInPlay`，我们这套卡没有 Animation）
            seq.AppendInterval(beats.CemeteryAt - beats.BodyAt);
            // ④ `GoToCemetery`（原版紧接着还有一句 `SetActive(false)`，那是调用方的 `onDone`）
            seq.AppendCallback(() => GoToCemetery(card));

            // 自检口：这一串里「爆散体」那一刻的**相对时刻**（由 `beats.BodyAt` 算出来的，
            // ⛔ 不是另算一份）—— 断言拿它钉「同帧起算」，改回串行时它会变 0.2/0.5+0.5。
            LastDeathBodyAt = beats.BodyAt;

            var t = CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
            if (onDone != null) t.OnComplete(() => onDone());
            return t;
        }

        /// <summary>
        /// **进墓地**（原版 `CardScript.GoToCemetery` → `BattleManager.AddToCemetery` →
        /// `CemeteryManager.AddCardToCemetery` → `CardScript.SetAsCemetery`）。
        ///
        /// 原版那三步（`CardScript__GoToCemetery.c` / `BattleManager__AddToCemetery.c` /
        /// `CemeteryManager__AddCardToCemetery.c` / `CardScript__SetAsCemetery.c`，全部亲读）：
        ///   ① `RawCardScript.IsUnit` ⇒ `ResetToValuesInHand(true)`（内部复位：手牌那一档的位姿/数值）
        ///   ② 没有 `trait 5` ⇒ `CemeteryManager`：**挂到它名下**（换父）→ `SetAsCemetery(列表长度)`
        ///      → `gameObject.SetActive(false)`（**同一帧，紧挨着**）
        ///   ③ `SetAsCemetery(order)`：`transform.position = cardInCemeteryPos`（**瞬移，不是补间**）
        ///      → `SetSortingLayer/Order(order)` → `SetObjectVisibility(cardState=6)`
        ///      → `collider2D.SetColliderNotInHand(6)` → `DOScale(one × cardInCemeteryScale, growInCemeteryTime)`
        ///
        /// 🔴 **两件必须如实说、别美化**：
        ///   ① 位移是**瞬移**（`set_position`），**没有「飞进墓地」那一段补间**；
        ///   ② 那条 0.3 s 的缩放补间**在原版里被紧跟着的 `SetActive(false)` 盖掉** ⇒ 画面上看不见，
        ///      所以**可见的只有「留一拍 + 到点收掉」**。我们照抄它（`DOScale` 照发，随后 `onDone` 收掉），
        ///      ⛔ 不假装它可见、也不自己发明一条飞行动画（那会变成原版没有的画面）。
        ///   ③ `order`（排序层里的序号 = 墓地列表长度）我们**没有**墓地列表 ⇒ 这两条不接，见报告。
        /// </summary>
        public static void GoToCemetery(CardView card)
        {
            if (card == null) return;
            var tr = card.transform;
            // z 也照原版写 0（`SetAsCemetery` 那个 Vector3 的第三格是显式的 `0.0`；我们的分层走渲染队列、
            // 不看 z，见 `CLAUDE.md` 那条「分层要用渲染队列」）。
            tr.position = new Vector3(CemeteryCardX, CemeteryCardY, 0f);
            // 原版那条补间（被同一帧的 `SetActive(false)` 盖掉）；我们照发 —— 紧跟的 `Kill` 会把它带走
            // （`CardTween.Use` 的 `SetLink` 保证卡一销毁这条补间就停，不会留 DOTween 的安全模式告警）。
            CardTween.Use(tr.DOScale(Vector3.one * CemeteryCardScale, CemeteryGrowTime), Ease.OutCubic, tr);
        }

        // ------------------------------------------------------------------
        //  🆕 2026-10-22（P6 · M2/M3）：**残骸被打掉**那一拍（原版 `RemnantBody.DoDestroy*`）
        // ------------------------------------------------------------------
        //  判据（第一权威，逐句读）：
        //   · `RemnantBody__DoDestroyByAttack.c`：`deathSound.Play3D` → `Instantiate(particleDeath = +0x9,
        //     GetDeathParticlePosition(), particleDeath.transform.rotation)` → `SetData(card)`
        //     → **`inst.transform.localScale = 残骸体.transform.lossyScale`** → `Destroy(残骸体)`。
        //     ⇒ 件是**残骸 prefab 自己序列化的 `particleDeath`**（⛔ 不是按阵营拼名字）；
        //       位姿 = 位置走各阵营的 `GetDeathParticlePosition`、**旋转用 prefab 自身**、
        //       尺度**赋值**成残骸体的 `lossyScale`。
        //   · `RemnantAeldari__GetDeathParticlePosition.c`：`this + 0x68`（= `wayStoneIdle`）的 `position`
        //     ⇒ 节点路径见 <see cref="CardView.RemnantIdleNodePath"/>。
        //   · `RemnantNecrons__GetDeathParticlePosition.c`：残骸体**自己的** `transform.position`。
        //   · `RemnantAeldari__DoDestroy.c`：`deathType == 50(collectWaystone)` ⇒ 走收集那条链，
        //     **不**走 `DoDestroyByAttack`；`RemnantNecrons__DoDestroy` **恒走** `DoDestroyByAttack`。
        //     ⇒ 收集那一下带出来的残骸消失**不播 Destroy 件**（<see cref="DeathFxPick.Suppressed"/>）。
        //   · `CardScript._UnitDeath_d__446__MoveNext.c:196`：只有 `isRemnant(+0x65) == 0` 那一支才
        //     `Instantiate(cardDestroyFX)` ⇒ **残骸阵亡不播普通阵亡件**（= 本支存在的理由）。
        //  逐件的 `particleDeath` 真值（P5 实读 prefab 的 MB 字段，⛔ 不是照名字推）：
        //   · `RemnantBody3D Aeldari` → `Remnant Aeldari Destroy particles`
        //     （`资料/普查产出_1022/P5_四未查清.md:137`，pathID 6910315040393640781）
        //   · `RemnantBody3D Necrons` → `Remnant Necrons Destroy particles`
        //     （同上 `:154` / `W64_素材导出缺口.md:50`，pathID -4358118985528859315）

        /// <summary>残骸体的 `particleDeath` 那一件 + 它的落点规则（判据见上面那一段注释）。</summary>
        struct RemnantDestroySpec
        {
            /// <summary>效果库键（= prefab 名）。</summary>
            public string Fx;
            /// <summary>true = 落点取 `Idle/Spirit Stone Idle`（灵族）；false = 残骸体根（死灵）。</summary>
            public bool IdleAnchor;
        }

        /// <summary>残骸体 prefab → 它被打掉时原版播的那一件。**显式表**（⛔ 别改成字符串拼接猜名字）。</summary>
        static readonly System.Collections.Generic.Dictionary<string, RemnantDestroySpec> RemnantDestroyByBody =
            new System.Collections.Generic.Dictionary<string, RemnantDestroySpec>
        {
            { "RemnantBody3D Aeldari", new RemnantDestroySpec { Fx = "Remnant Aeldari Destroy particles", IdleAnchor = true  } },
            { "RemnantBody3D Necrons", new RemnantDestroySpec { Fx = "Remnant Necrons Destroy particles", IdleAnchor = false } },
        };

        /// <summary>查表（键 = <see cref="CardView.RemnantBodyPrefabName"/>）。查不到 ⇒ `false` —— 调用方**出声**。</summary>
        static bool TryRemnantDestroySpec(string bodyPrefab, out RemnantDestroySpec spec)
        {
            spec = default(RemnantDestroySpec);
            return !string.IsNullOrEmpty(bodyPrefab) && RemnantDestroyByBody.TryGetValue(bodyPrefab, out spec);
        }

        /// <summary>🔴 2026-10-22（P6 · M1–M3）：**这一张卡是不是「残骸那一族」** —— 判据只此一处。
        ///
        /// <para>原版判据 = `SupportMethods.HasToTransformIntoRemnant`（两条当前特质 `0x41a Remnant` /
        /// `0x474 Waystone`）；落到我们这边就是卡 SO 的两个关键词位。
        /// `BattleDriver.PlayRemnantSfx`（音效：出现/收集/被打掉）与 <see cref="PickDeathFx"/>
        /// （粒子：Destroy 那件）**都调本函数** —— ⛔ 别在任一处再抄一份
        /// `Has(Waystone) || Has(Remnant)`（那是同一条判据的第二份）。</para>
        /// <param name="waystone">true = 灵族那一支（`Waystone`：收集灵魂石 / `RemnantBody3D Aeldari`）；
        /// false = 死灵那一支（`Remnant`：`RemnantBody3D Necrons`）。</param></summary>
        public static bool IsRemnantFamily(RuleEngine.CardDef card, out bool waystone)
        {
            waystone = card != null && card.Has(RuleEngine.KeywordTable.Waystone);
            return waystone || (card != null && card.Has(RuleEngine.KeywordTable.Remnant));
        }

        /// <summary><see cref="PickDeathFx"/> 的结论 —— 一个结构体装齐「播哪件 / 走哪条生成路 / 这一拍该不该有件」。
        /// <para>⚠️ 拆成结构体（而不是一堆 `out`）是为了**自检能一条断言比完**，见 `Editor/BattleScene.cs`。</para></summary>
        public struct DeathFxPick
        {
            /// <summary>要播的那件（效果库键）。null = 一件都没有（`Suppressed == false` 时调用方走退回路）。</summary>
            public string Fx;
            /// <summary>逐卡覆盖件（原版 `customDeathParticles`）；没有 = null。只有**非残骸**那一路会填。</summary>
            public string PerCard;
            /// <summary>true = 走「残骸 Destroy」那一条（落点 / 尺度规则与普通爆散体不同，见 `SpawnRemnantDestroy`）。</summary>
            public bool Remnant;
            /// <summary>true = **这一拍本来就不该有件** —— 收集路标石带出来的残骸消失，原版 `DoDestroy` 的
            /// `deathType == 50` 那一支只播**收集**粒子（`CollectWaystone` 那条事件），没有 Destroy 件、
            /// 也没有普通阵亡件，而且 `Destroy(残骸体)` 是**当场**（不进 `UnitDeath` 那 0.7 s 时间轴）。
            /// ⚠️ `DeathExplosion` 见到它**直接回 `null`** ⇒ 调用方当场收掉视图。</summary>
            public bool Suppressed;
        }

        /// <summary>挑出这一张该用哪件（**出声**，不静默）。
        /// 返回的 `Fx == null && !Suppressed` ⇒ 库里一件都没有 ⇒ 调用方走退回路。
        ///
        /// 🔴🔴 规则 = 原版 `_UnitDeath…:200-347` 的「**二选一**」，不是「叠加」——
        ///   整个函数**只有两处 `Instantiate`**：`:233` 逐卡 `customDeathParticles`、
        ///   `:262` 默认 `cardDestroyFX`（它的序列化引用就是 GameObject **`Card 3D Death Explosion`**），
        ///   两者**共享同一段后续处理**（`:266` set localScale · `:275-328` 换 `_CardImage`/`_MatCap`），
        ///   判别式在 **`:240`**：`Object.op_Equality(生成体, 0)` —— **逐卡那件取不到（或判空）才用默认件**。
        ///   ⚠️ 这里必须问 **`ResolvePerCard`**（只查逐卡那一档），⛔ **不是 `Resolve`**：
        ///      后者的兜底链会返回我们自己挑的替代品 `Explosion Fenrisian Monstrosities`，
        ///      而那件**已经在退回路里**了 ⇒ 算进来就是**原版没有的第二下**。
        ///   ⚠️ 逐卡的 66 张卡解出来**是同一件** `Necrons death explosion`
        ///      （判据 → 生成器 `gen_card_vfx_by_card.py` 的 `RT_SKIP_SOFIELDS` 那段）
        ///      ⇒ **别指望它是「一卡一件」**，也别因为「都一样」就以为这一跳没意义。
        ///
        /// 🆕 2026-10-22（P6）：**残骸那一支排在最前**（判据见上面那一段注释）——
        ///   死的是残骸 ⇒ 该具残骸自己的 `particleDeath`，**不**走上面那条二选一
        ///   （原版 `CardScript._UnitDeath…:196` 对残骸根本不进 `Instantiate(cardDestroyFX)` 那一支）。
        /// <para>🔴 **公开**是给自检直接验「选了哪一件」（不用跑一局、也不用推补间）。</para></summary>
        public static DeathFxPick PickDeathFx(CardView card, RuleEngine.CardDef def, bool collectedWaystone = false)
        {
            var pick = new DeathFxPick();

            // ① **收集路标石那一下**带出来的残骸消失 ⇒ 原版只播收集粒子、没有 Destroy 件
            //    （`RemnantAeldari__DoDestroy.c` 的 `deathType == 50(collectWaystone)` 那一支）。
            if (collectedWaystone)
            {
                pick.Suppressed = true;
                return pick;
            }

            // 防御：没有视图就没有「这一张卡」可言 ⇒ 回一件都不播（调用方走退回路）。
            // ⚠️ 正常路径到不了（`DeathExplosion` 开头就挡 null）；留着是为了让本函数**处处有定义**。
            if (card == null)
            {
                Debug.LogWarning("[CardFeel] PickDeathFx：`card` 为 null ⇒ 这一拍没有阵亡件（调用方不该这么传）");
                return pick;
            }

            // 防御：视图上盖着残骸体、调用方却没给 `CardDef` ⇒ 判不了「是不是残骸」。
            //   ⛔ 不静默退回普通阵亡链 —— 那会播出一件原版没有的 `Card 3D Death Explosion`。
            if (def == null && card.RemnantBodyVisible)
                Debug.LogWarning($"[CardFeel] 卡 `{card.Data.id}` 正盖着残骸体（`{card.RemnantBodyPrefabName}`），"
                               + "但调用方没给 `CardDef` ⇒ 判不出「是不是残骸那一族」⇒ 这一拍会退回普通阵亡链。"
                               + "修法：调用方把这一张的 `CardDef` 传进 `DeathExplosion`（判据 = "
                               + "`CardFeel.IsRemnantFamily`，与 `PlayRemnantSfx` 同一份）");

            // ② **残骸阵亡** ⇒ 该具残骸自己的 `particleDeath`（`RemnantBody__DoDestroyByAttack.c` 的 `+0x9`）。
            //    ⚠️ 判「是不是残骸」**只调 <see cref="IsRemnantFamily"/>**（与 `PlayRemnantSfx` 同一份判据）。
            bool waystone;
            if (IsRemnantFamily(def, out waystone))
            {
                RemnantDestroySpec spec;
                if (!TryRemnantDestroySpec(card != null ? card.RemnantBodyPrefabName : null, out spec))
                {
                    // 出声：表里没有这具残骸体 ⇒ 不猜名字（猜出来的多半是同名不同物）
                    Debug.LogWarning($"[CardFeel] 残骸阵亡：残骸体 `{(card != null ? card.RemnantBodyPrefabName : null) ?? "<null>"}`"
                                   + $"（{(waystone ? "灵族 Waystone" : "死灵 Remnant")}）**不在 `RemnantDestroyByBody` 表里** "
                                   + "⇒ 这一拍退回现有阵亡链（原版播的是该残骸 prefab 自己的 `particleDeath`，"
                                   + "⛔ 不按阵营拼名字猜）。补它：把那一具的 `particleDeath` 解出来、加进那张表");
                }
                else if (!FxInLibrary(spec.Fx))
                {
                    Debug.LogWarning($"[CardFeel] 残骸阵亡粒子 `{spec.Fx}`（残骸体 `{(card != null ? card.RemnantBodyPrefabName : null)}` 的 "
                                   + "`particleDeath`）**在效果库里没有** ⇒ 这一拍退回现有阵亡链。补它："
                                   + "`python 工具/extract_missing_shaders.py --prefabs` → `EffectExporter.RunListed`"
                                   + " → `EffectLibraryBuilder.Run`");
                }
                else
                {
                    pick.Fx = spec.Fx;
                    pick.Remnant = true;
                    return pick;
                }
                // 取不到 ⇒ 上面已出声，**退回下面的现有链**（不静默、也不硬凑一件）
            }

            // ③ 普通卡（或残骸那件取不到时的退路）：原版那条二选一
            pick.PerCard = VfxMap.ResolvePerCard(VfxMap.Death, card.Data.faction, card.Data.id);
            if (!string.IsNullOrEmpty(pick.PerCard) && !FxInLibrary(pick.PerCard))
            {
                // 逐卡那件在库里没有 ⇒ **出声**，再退回默认件试一次。
                // ⛔ 不静默吞掉、也不静默降级 —— 这两件事都违反了「不许静默失败」那条红线。
                Debug.LogWarning($"[CardFeel] 逐卡阵亡粒子 `{pick.PerCard}`（卡 `{card.Data.id}` 的原版 "
                               + "`customDeathParticles`）**在效果库里没有** ⇒ 退回默认件 "
                               + $"`{DeathBodyFx}`（原版这一步正是「逐卡取不到就用默认件」，"
                               + "见 `CardScript._UnitDeath_d__446__MoveNext.c:240`）。"
                               + "补它：`python 工具/extract_missing_shaders.py --prefabs` → "
                               + "`EffectExporter.RunListed` → `EffectLibraryBuilder.Run`");
                pick.PerCard = null;
            }
            pick.Fx = string.IsNullOrEmpty(pick.PerCard) ? DeathBodyFx : pick.PerCard;
            if (FxInLibrary(pick.Fx)) return pick;
            Debug.LogWarning($"[CardFeel] 效果库里没有 `{DeathBodyFx}` ⇒ 阵亡退回「淡出 + 上浮 + 缩」"
                           + "（原版是「关掉 3D 卡体 + 在卡位生成那个爆散体」）。补它两步（本机已跑过）："
                           + "`python 工具/extract_missing_shaders.py --prefabs` → `EffectExporter.RunListed`"
                           + " → `EffectLibraryBuilder.Run`；判据 → `资料/已知的坑.md` 的"
                           + "「`GetAllAssetNames()` 只吐容器里的资产」那条");
            pick.Fx = null;
            return pick;
        }

        /// <summary>效果库里有这一件吗（自检里判同一件事用的也是这一对）。</summary>
        static bool FxInLibrary(string name)
        {
            var lib = WarpforgeVFX.WarpforgeEffectLibrary.Instance;
            if (lib == null || string.IsNullOrEmpty(name)) return false;
            WarpforgeVFX.WFEffectEntry e;
            return lib.TryGet(name, out e);
        }

        /// <summary>「这张卡没有 `body3D` 节点」只出声一次（别每张阵亡都刷一行）。</summary>
        static bool _warnedNoBody3D;

        /// <summary>在卡位生成死亡爆散体（原版 `Instantiate(cardDestroyFX, 卡位置, 3D体旋转)`）。
        /// `perCard` / `fx` 由 <see cref="PickDeathFx"/> 定好；返回 false = 这一刻放不出来（调用方出声）。
        /// <para>🔴 **2026-10-21（W61 第 1 批 · 第 6 条）：位姿照原版**。判据逐句 =
        /// `CardScript._UnitDeath_d__446__MoveNext.c`：`:245-263` 位置取**卡 transform** 的 `position`、
        /// 旋转取 **3D 体 transform 的 `rotation`**，`Instantiate(prefab, pos, rot)`；
        /// 紧接着 `:266-274` `set_localScale(**3D 体 lossyScale**)`（**绝对赋值**，不是乘）。
        /// 我们原来只传位置（`scale = 1f`、无旋转）⇒ 尺寸差 2.8×/1.45×（见对账 §2·5 #5）。</para></summary>
        static bool SpawnDeathBody(CardView card, string perCard, string fx)
        {
            // 3D 卡体那一层（`CardView.BuildBody3D` 建的子节点，名字恒为 `body3D`；
            // 同一个口 `Editor/BattleScene.cs:14092` 也在用）。取不到 = 这张卡没有 3D 体
            // （例如 `CardView.DebugNoBody3D` 那条退回档）⇒ 退回卡根自己的位姿并**出声一次**。
            var body = card.transform.Find("body3D");
            if (body == null && !_warnedNoBody3D)
            {
                _warnedNoBody3D = true;
                Debug.LogWarning($"[CardFeel] 阵亡爆散体：卡 `{card.Data.id}` 上找不到 `body3D` 节点 "
                               + "⇒ 位姿退回**卡根**（原版取 3D 体的 rotation / lossyScale；"
                               + "没有 3D 体的卡本来就没有那个等价物）");
            }
            Quaternion rot = body != null ? body.rotation : card.transform.rotation;
            Vector3 scale = body != null ? body.lossyScale : card.transform.lossyScale;

            var p = WarpforgeVFX.WarpforgeEffectPlayer.Play(fx, null, card.transform.position, 1f);
            // 逐卡那件**在库里**、但这一刻放不出来 ⇒ 照老写法退回默认件再试一次（原版 `:240` 那个判别式）。
            if (p == null && !string.IsNullOrEmpty(perCard))
                p = WarpforgeVFX.WarpforgeEffectPlayer.Play(DeathBodyFx, null, card.transform.position, 1f);
            if (p == null) return false;
            // 位姿：**两个出口都要写**（逐卡件 / 默认件）—— 原版那一句 `set_localScale` 在二选一**之后**。
            // ⚠️ 父节点是 null ⇒ local 就是 world；`Play` 只在 `scale != 1` 时乘，这里传的就是 1 ⇒ 直接赋值等价。
            p.transform.rotation = rot;
            p.transform.localScale = scale;
            // 原版从卡上借贴图：`_CardImage = rawCard.cardSprite.texture` · `_MatCap = GetCardMatCapByCardTier(tier)`，
            // 再给爆散纹理一个**随机偏移**（`_ExplosionTextureOffsetModifier = Random.value`）。
            // 🔴 **2026-10-21（W61 第 2 批 · 第 6 条）**：matcap **按卡等级分四档**（原来恒 Level1）。
            //   判据 = `CardMaterialHelper__GetCardMatCapByCardTier.c`（拿 `RawCardScript.CardTier` 当数组下标）
            //   + `CardMaterialHelper.matcapTexturesByCardTier`（`MonoBehaviour_4274` 的 4 个 Texture2D 引用，
            //   UnityPy 逐 PathID 解出：Tier1 `MatCap Card Level 1` / Tier2 `Matcap Bronze` /
            //   Tier3 `Matcap Silver` / Tier4 `Matcap Gold`）。档位口径与卡框同一个：
            //   `CardProgress.Level(artId)` 是 **1 基**（= 原版 `CardTier + 1`）。
            var art = card.BodyArtTexture;
            int tierLevel = string.IsNullOrEmpty(card.Data.artId) ? 0 : CardProgress.Level(card.Data.artId);
            var matcap = CardArt.Card3DMatcap(tierLevel);
            foreach (var r in p.GetComponentsInChildren<MeshRenderer>(true))
            {
                // 🔴 **2026-10-19（`A1967` 残尾）**：这里原来写的是 `r.material` ——
                //   编辑模式下每调一次，Unity 就实例化一份材质并打一条
                //   `Instantiating material due to calling renderer.material during edit mode …`
                //   （2026-10-14 的 `BattleScene.Run` 日志里这一族 162 条；每趟自检 3~4 条，
                //    是 `A1967` 普查里**唯一剩下的生产源** —— 分布表见
                //    `资料/普查产出_1019/W_自检断言线_A1967_1019b.md` §一）。
                //   ⇒ 走 `WritableMaterial`：显式 `new Material(sharedMaterial)` 再赋给 `sharedMaterial`，
                //     语义与 `.material` 逐字等价（一份逐渲染器私有实例），但不触发那条警告。
                //   ⛔ **不能**用 `CardView` 那个 `(源材质, 队列)` 共享缓存：这一路要按**这一张卡**写
                //     `_CardImage` / `_MatCap` / 随机偏移 —— 共享一份会把上一张阵亡卡的脸留在下一张上
                //     （与 `BattleDriver.StartRevealFlight` 必须独占一份是同一条理由）。
                var m = WritableMaterial(r);
                if (m == null) continue;
                if (art != null && m.HasProperty("_CardImage")) m.SetTexture("_CardImage", art);
                if (matcap != null && m.HasProperty("_MatCap")) m.SetTexture("_MatCap", matcap);
                if (m.HasProperty("_ExplosionTextureOffsetModifier"))
                    m.SetFloat("_ExplosionTextureOffsetModifier", UnityEngine.Random.value);
            }
            return true;
        }

        /// <summary>🆕 2026-10-22（P6 · M2/M3）：**在残骸体上生成「残骸 Destroy」粒子**。
        /// 判据（第一权威，逐句读）= `RemnantBody__DoDestroyByAttack.c`：
        /// `Instantiate(particleDeath = +0x9, GetDeathParticlePosition(), particleDeath.transform.rotation)`
        /// → `SetData(card)` → **`inst.transform.localScale = 残骸体.transform.lossyScale`** → `Destroy(残骸体)`。
        ///
        /// <list type="bullet">
        /// <item>**位置** = `GetDeathParticlePosition()`（两阵营各一份，见下面那两支）；
        ///   ⛔ 不是卡心、不是格心 —— 那正是 `CardFeel.SpawnDeathBody`（普通爆散体）的规则。</item>
        /// <item>**旋转** = prefab 自身（原版第三个参数是 `particleDeath.transform.rotation`，
        ///   也就是 prefab 自己的朝向）⇒ 我们**不动**它（`WarpforgeEffectPlayer.Play` 默认就不动旋转）。</item>
        /// <item>**尺度** = **赋值**成残骸体的 `lossyScale`（原版那句 `set_localScale`）——
        ///   `Play(..., scale: 1f)` 不缩放 prefab、随后我们显式赋值一次。
        ///   父节点是 null ⇒ local 就是 world，赋值即等价。</item>
        /// </list>
        /// <para>返回 `false` = 这一刻放不出来 / 取不到位姿（调用方**出声**）。</para></summary>
        static bool SpawnRemnantDestroy(CardView card, string fx)
        {
            var root = card.RemnantBodyRoot;
            if (root == null)
            {
                Debug.LogWarning($"[CardFeel] 残骸阵亡粒子 `{fx}`：这张卡上**没有残骸体**"
                               + "（`CardView.RemnantBodyRoot` 为 null）⇒ 位姿取不到、这一拍不生成。"
                               + "原版 `RemnantBody.DoDestroyByAttack` 取的就是残骸体自己的 transform");
                return false;
            }

            RemnantDestroySpec spec;
            if (!TryRemnantDestroySpec(card.RemnantBodyPrefabName, out spec))
            {
                // 理论上到不了（`PickDeathFx` 已经查过一次表并出声）—— 但这是**另一条出口**，
                // 按铁律 10 第 5 条「多个入口要带齐状态」，这里也出声、并退回残骸体根。
                Debug.LogWarning($"[CardFeel] 残骸阵亡粒子 `{fx}`：残骸体 `{card.RemnantBodyPrefabName}`"
                               + " 不在 `RemnantDestroyByBody` 表里 ⇒ 落点退回残骸体根");
            }

            Vector3 pos = root.transform.position;
            if (spec.IdleAnchor)
            {
                // 灵族：`RemnantAeldari.GetDeathParticlePosition` 取 `wayStoneIdle`（`+0x68`）的 position。
                Transform node;
                if (card.TryGetRemnantNode(CardView.RemnantIdleNodePath, out node) && node != null)
                    pos = node.position;
                else
                    Debug.LogWarning($"[CardFeel] 残骸阵亡粒子 `{fx}`：灵族那具残骸体里取不到 "
                                   + $"`{CardView.RemnantIdleNodePath}` ⇒ 落点退回残骸体根"
                                   + "（原版取 `wayStoneIdle` 的 position，见 `RemnantAeldari__GetDeathParticlePosition.c`）");
            }

            var p = WarpforgeVFX.WarpforgeEffectPlayer.Play(fx, null, pos, 1f);
            if (p == null) return false;
            // 原版 `inst.transform.localScale = 残骸体.transform.lossyScale`（**赋值**，不是乘）
            p.transform.localScale = root.transform.lossyScale;
            return true;
        }

        /// <summary>把这一层的材质**复制一份私有实例**再返回（⛔ 不走 `Renderer.material`）。
        /// <para>🔴 **为什么不是 `.material`**（`A1967`）：编辑模式下 `Renderer.material` 每次实例化
        /// 都会打一条 `Instantiating material due to calling renderer.material during edit mode …`
        /// （14–16 行托管栈）。`BattleScene.Run` 2026-10-14 实测这一族 **10,642 条 = 全文 52.6%**；
        /// `CardView.StampQueue` / `BattleDriver.StartRevealFlight` 已在 2026-10-19 修掉
        /// （判据 → `资料/普查产出_1019/W_自检断言线_A1967_1019b.md`），
        /// 本函数收的是**最后一个生产源**（同一份报告：`CardFeel.SpawnDeathBody`，162 条）。</para>
        /// <para>🔴 **为什么不并进 `CardView` 的 `(源材质, 队列)` 共享缓存**：那套缓存的前提是
        /// 「同源同队列的消费者**不写**逐实例属性」；这里每一个爆散体都要按**这一张卡**写
        /// `_CardImage` / `_MatCap` / `_ExplosionTextureOffsetModifier` ⇒ 共享会把上一张阵亡卡的
        /// 脸留在下一张上。显式 `new Material(...)` 与 `.material` 的语义**逐字等价**
        /// （一份逐渲染器私有实例），只是不触发那条警告、也不去写 prefab 上的共享材质。</para>
        /// <para>⚠️ 返回 `null` = 这个渲染器没有材质（与 `.material` 一样什么都不做）。</para></summary>
        static Material WritableMaterial(Renderer r)
        {
            var src = r.sharedMaterial;
            if (src == null) return null;
            var m = new Material(src) { name = src.name + " (CardFeel death)" };
            r.sharedMaterial = m;
            return m;
        }

#if UNITY_EDITOR
        /// <summary>🆕 2026-10-19（`A1967` 残尾）**只给 Editor 自检用**的探针（产品路径不调用）。
        /// <para>判据不是「我们自己说没走 `.material`」，而是 **Unity 自己那条警告**：
        /// ① 负例（**灭自证**）故意调一次 `.material` ⇒ 必须抓到 ≥1 条；
        /// ② 正例调 <see cref="WritableMaterial"/> ⇒ 必须 **0 条**；
        /// ③ 在副本上写贴图 ⇒ 源材质一个字节都不许变（`renderQueue` / `mainTexture` 都不变）；
        /// ④ 拿到的是独立实例、并已赋给 `sharedMaterial`。
        /// ① 抓不到 ⇒ 检测器失效，② 的 0 条没有意义（所以两条必须一起断）。</para>
        /// <para>宿主 = `Editor/RewardsScene.cs`（`A2038` 那段旁）；`Editor/BattleScene.cs` 里的
        /// `miCounter` 是**全程闸**，但它不在本批白名单 ⇒ 这里给一个不依赖战斗场景的独立口。</para></summary>
        public static MaterialWriteProbe ProbeWritableMaterial()
        {
            var result = new MaterialWriteProbe();
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) { result.Error = "Shader.Find(\"Sprites/Default\") 取不到"; return result; }

            var src = new Material(sh) { name = "A1967 Probe Source", renderQueue = 3000 };
            src.mainTexture = Texture2D.whiteTexture;
            var goNeg = new GameObject("A1967 Probe Negative");
            var goPos = new GameObject("A1967 Probe Positive");
            var mrNeg = goNeg.AddComponent<MeshRenderer>();
            var mrPos = goPos.AddComponent<MeshRenderer>();
            mrNeg.sharedMaterial = src;
            mrPos.sharedMaterial = src;

            int warns = 0;
            Application.LogCallback h = (cond, stack, type) =>
            {
                if (type == LogType.Warning && cond != null
                    && cond.Contains("Instantiating material due to calling renderer.material")) warns++;
            };
            Material matNeg = null, matPos = null;
            Application.logMessageReceived += h;
            try
            {
                // ① 负例：**故意**走一次 `.material`（只在本探针里；产品代码禁止）——
                //    抓不到警告 = 「编辑模式警告」这条检测器坏了 ⇒ ② 的 0 条是假绿。
                matNeg = mrNeg.material;
                result.NegativeCaught = warns;
                result.NegativeWasDistinct = matNeg != null && matNeg != src;
                warns = 0;

                // ② / ④ 正例：被测件
                matPos = WritableMaterial(mrPos);
                result.WritableCaught = warns;
                result.CopyIsDistinct = matPos != null && matPos != src && mrPos.sharedMaterial == matPos;

                // ③ 源材质不许被写脏（在副本上写，源必须原样）
                var texBefore = src.mainTexture;
                if (matPos != null) matPos.mainTexture = Texture2D.blackTexture;
                result.SourceDirty = src.mainTexture != texBefore;
                result.SourceQueueDirty = src.renderQueue != 3000;
                result.SourceName = src.name;
            }
            finally
            {
                Application.logMessageReceived -= h;
                Object.DestroyImmediate(goNeg);
                Object.DestroyImmediate(goPos);
                if (matNeg != null && matNeg != src) Object.DestroyImmediate(matNeg);
                if (matPos != null && matPos != src) Object.DestroyImmediate(matPos);
                Object.DestroyImmediate(src);
            }
            return result;
        }

        /// <summary><see cref="ProbeWritableMaterial"/> 的读数（调用方用宿主自己的 `Check` 逐项断）。</summary>
        public struct MaterialWriteProbe
        {
            /// <summary>`null` = 跑起来了；非空 = 探针自己起不来（调用方要断它为空）。</summary>
            public string Error;
            /// <summary>负例抓到的警告条数（**期望 ≥1**；0 = 检测器失效）。</summary>
            public int NegativeCaught;
            /// <summary>负例拿到的是一份独立实例（`.material` 的语义）。</summary>
            public bool NegativeWasDistinct;
            /// <summary>正例抓到的警告条数（**期望 0**）。</summary>
            public int WritableCaught;
            /// <summary>正例拿到的是一份独立实例、并已挂到 `sharedMaterial` 上。</summary>
            public bool CopyIsDistinct;
            /// <summary>在副本上写过贴图之后，**源材质**的 `mainTexture` 变了（**期望 false**）。</summary>
            public bool SourceDirty;
            /// <summary>源材质的 `renderQueue` 变了（**期望 false**）。</summary>
            public bool SourceQueueDirty;
            /// <summary>探针源的材质名（出错时好定位）。</summary>
            public string SourceName;
        }
#endif

        /// <summary>「取不到兜底爆散体」只出声一次（别每张阵亡刷一行）。</summary>
        static bool _warnedDeathFxNull;

        /// <summary>阵亡**退回路**的那个根特效（`PickDeathFx` 返回 null 时走这里）。
        /// <para>🔴 **2026-10-22（`W61` 收口）：取到 null 就【只出声、不调 `Play`】**。
        /// 原因：`VfxMap.Resolve(VfxMap.Death)` 现在**返回 null**（自造件已在 B3 删掉），而
        /// `WarpforgeEffectLibrary.TryGet(null, …)` 里是 `Dictionary.TryGetValue(null)` ⇒
        /// **`ArgumentNullException`**。本机有效果库时跑不到这条（prefab 是 gitignore 产物，
        /// 换台机器就会崩），所以必须在这一跳挡住。</para>
        /// <para>⛔ 删掉这个守卫 = 把「没件」变成「崩」；也**别**改成静默返回（不静默是红线）。</para></summary>
        public static void PlayDeathFallbackFx(Transform card)
        {
            var name = VfxMap.Resolve(VfxMap.Death);
            if (string.IsNullOrEmpty(name))
            {
                if (!_warnedDeathFxNull)
                {
                    _warnedDeathFxNull = true;
                    Debug.LogWarning("[CardFeel] 阵亡退回路取不到兜底爆散体"
                                   + "（`VfxMap.Resolve(\"death\")` = null —— 那件自造品已在 B3 删掉）"
                                   + " ⇒ 这一档**不播任何根特效**，只走「淡出 + 上浮 + 缩」"
                                   + "（本条只出声一次；⛔ 不把 null 递给特效库）");
                }
                return;
            }
            if (card == null) return;
            WarpforgeVFX.WarpforgeEffectPlayer.Play(name, card);
        }

        /// <summary>自检口：把「已出过声」那一位清零（让「恰好一条警告」可重复断言）。</summary>
        public static void ResetDeathFxNullWarnForTest() { _warnedDeathFxNull = false; }

        /// <summary>阵亡消散（**旧表现 —— 只在取不到原版那件爆散体时用**）。原版是**材质 `_DissolveAmount`** 从 1 溶到 0
        /// （见 `Card Hand To Board` clip 里 `Card 3D` 那条曲线）。
        /// ⚠️ **我们没有溶解 shader**，退而用「透明度 + 轻微上浮/缩小」—— 表现形式是我们的。
        /// 🔴 **时长照原版分档**（2026-09-18）：小兵 `deathTimeMinionDuration 0.2` ·
        ///    督军 `deathTimeWarlordDuration 0.5` —— 见 <see cref="DeathDissolve"/>。
        ///    （改之前两边都用 0.5333，那是从出战 clip 对称借来的，**不是原版值**。）</summary>
        // ==================================================================
        //  🆕 2026-10-23（`F1` 前置；判据 → `资料/普查产出_1023/D3_F1判据与方案.md` §③ C3/C4）
        //  卡体改成**不透明**（`Queue=Geometry` / `Blend One Zero`）之后，**`_Color.a` 对卡体不再生效**
        //  ⇒ 两条靠「整卡淡出」的兜底会变成「卡体啪地留在场上」。这两条兜底是 F1 之后**最后的安全网**，
        //  所以它们必须改走**原版那条路**（材质 `_DissolveAmount`）：
        //    · 阵亡兜底 `Dissolve()`：`BeginDissolve()` 成功 ⇒ 驱动 `SetDissolveAmount(0→1)`；
        //      取不到溶解材质 ⇒ **退回旧的整卡淡出并出声**（D3 原文：「`DissolveSupported==false` 时退回现状（并出声）」）。
        //    · 回手兜底 `ReturnToHand()` 的 `!canDissolve` 分支：**0.2167 时直接关掉 3D 体**
        //      （`SetBody3DVisible(false)`）+ 2D 卡面照原版时间轴打开/淡入 —— ⛔ 不再淡卡体。
        //      判据：原版阵亡/小兵溶解本来就是「溶完直接关体」（`DeathExplosion` 正路 = `SetBody3DVisible(false)`）。
        //  `ForceNoDissolveForTest` 是**自检专用**开关（同 `CardTween.LinkEnabled` 的定位）：
        //  它让上面两条走「取不到溶解材质」那一支，好让断言能验**兜底本身**（否则真机上永远走不到）。
        // ==================================================================
        /// <summary>这两条兜底**实际走的是哪条路**（自检盯它；⛔ 别拿「代码里写了」当证据）。</summary>
        public enum FallbackPath
        {
            None = 0,
            /// <summary>材质溶解（原版那条路：`BeginDissolve` + `_DissolveAmount` 0→1）。</summary>
            MaterialDissolve = 1,
            /// <summary>旧的整卡淡出（**只剩「取不到溶解材质」这一档**；会出声）。</summary>
            WholeCardAlpha = 2,
            /// <summary>回手兜底：直接关 3D 体 + 2D 卡面照原版时间轴淡入（⛔ 不碰 `_Color.a`）。</summary>
            HideBody = 3,
        }
        /// <summary>最近一次**阵亡兜底**走的路（<see cref="Dissolve"/>）。</summary>
        public static FallbackPath LastDeathFallback { get; private set; }
        /// <summary>最近一次**回手**走的路（<see cref="ReturnToHand"/>；正常档 = `MaterialDissolve`）。</summary>
        public static FallbackPath LastReturnFallback { get; private set; }
        /// <summary>自检专用：置真 ⇒ 两条兜底都按「取不到溶解材质」处理（见上面那一段）。</summary>
        public static bool ForceNoDissolveForTest { get; set; }

        static bool _warnedDeathFallbackAlpha;
        static void SpeakDeathFallbackAlpha()
        {
            if (_warnedDeathFallbackAlpha) return;
            _warnedDeathFallbackAlpha = true;
            Debug.LogWarning("[CardFeel] 阵亡兜底：**取不到溶解材质**（`CardView.BeginDissolve()` = false）"
                           + " ⇒ 退回「整卡淡出」—— ⚠️ 卡体不透明之后 `_Color.a` 对 3D 体**不生效**"
                           + "（F1；判据 → `D3_F1判据与方案.md` §③ C3）⇒ 这一段只剩 2D 层会淡。"
                           + "原版那条路是材质 `_DissolveAmount`，所以正常档不会走到这里。");
        }
        /// <summary>自检口：把「已出过声」那一位清零。</summary>
        public static void ResetDeathFallbackWarnForTest() { _warnedDeathFallbackAlpha = false; }

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
            // 🔴 2026-10-23（`F1` 前置）：**先走原版那条路**（材质溶解）；拿不到溶解材质才退回整卡淡出。
            bool canDissolve = !ForceNoDissolveForTest && card.BeginDissolve();
            LastDeathFallback = canDissolve ? FallbackPath.MaterialDissolve : FallbackPath.WholeCardAlpha;
            if (canDissolve)
            {
                seq.Join(DOTween.To(() => card.DissolveAmount, a => card.SetDissolveAmount(a), 1f, dur)
                               .SetEase(Ease.Linear));
            }
            else
            {
                SpeakDeathFallbackAlpha();
                seq.Join(DOTween.To(() => card.Alpha, a => card.SetAlpha(a), 0f, dur));
            }
            var t = CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
            // ⚠️ 回调**从这里接**，别让调用方去碰 DOTween —— 这是本文件的划法。
            //    🔴 **2026-10-19（W）就地订正（铁律 5）**：本行原来写「`BattleDriver` 没有
            //    `using DG.Tweening`」—— **已过期**（`BattleDriver.cs:14` 自 2026-09-30 起就有）。
            //    ✅ 仍然成立的是「补间只在这几个 `CardFeel`/`CardTween` 文件里出现」这个现状。
            if (onDone != null) t.OnComplete(() => onDone());
            return t;
        }

        /// <summary>🆕 **2026-10-17（`A1213②`）**：原 `CardFeel.PopNumber` / `LastPopTween` **已删**（全仓 0 消费者）。
        /// 真实现 = `Battle/PopNumber.cs` 的 `PopDamage` / `PopHeal` 两条路径（判据 = 原版两颗 TMP 组件，
        /// 有效字号比 **1.21 : 1**）。⛔ 别在这里补回来 —— 两处写同一条规则 = 迟早不一致。
        /// ⚠️ **2026-10-18（`N54②`）就地订正（铁律 5）**：本行原来写「12.1:1」—— **错的**。两颗都是
        /// 3D `TextMeshPro`，`m_isOrthographic` Damage=0 ⇒ 渲染 ×0.1 / Heal=1 ⇒ ×1.0，有效比 = 0.847:0.70 =
        /// 1.21:1（判据 → `资料/普查产出_1018/N54_飘字绝对字号_判据.md`）。</summary>

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
        //  抽牌那一下的「翻面」（原版 `CardScript.TurnCardAround` + `FinishCardDrawRotation`）
        // ==================================================================
        //  判据（`d:/2/tools/decomp_full/`，**引方法体前首行函数名逐一核过**）：
        //   · `CardScript__DrawCard.c:52-59` —— **只有抽「自己的」牌才翻**：
        //       `if (!isPlayer) SetObjectVisibility(卡, cardState)` / `else TurnCardAround(this, 时长)`
        //     （`+0x40` = `EntityScript.<isPlayer>k__BackingField`，`il2cpp_out/dump.cs:21973` 实读）
        //   · `CardScript__DrawCard.c:65-75` —— **翻面那一支【不】再转到底**：那条
        //       `DORotate(Vector3.zero, 时长)` 只在不翻面（敌方）那一支里（⚠️ 它**没写 `SetEase`**
        //       ⇒ 吃 DOTween 的全局默认缓动，我们这边照 `OutCubic` 写了 —— 见报告 §A-4 的顺手发现）
        //       ⇒ 我方的旋转归 `TurnCardAround` 管、敌方那条我们没做（`DealInQuad` 只有位移/缩放/淡入）。
        //   · `CardScript__TurnCardAround.c:29-37` ——
        //       `DORotate(transform, (0, DAT_1834b2e04, 0), 时长 × DAT_1834b2e7c, RotateMode.Fast)`
        //       + `SetEase(1)`（DOTween `Ease.Linear`）+ `SetDelay(param3)`
        //       + `OnComplete → FinishCardDrawRotation(this, 时长)`（`__c__DisplayClass246_0…b__0.c:6`）
        //   · `CardScript__FinishCardDrawRotation.c:13-24` —— 同一个倍率、同一个 `Ease(1)`，
        //       终点 = `Vector3.zero`（判据：`DAT_1842da2a8` = `UnityEngine.Vector3` 的 Il2CppClass，
        //       `+0xb8` 静态块 `+0x0` 就是 `zero` —— 字段表 `zero 0x0 / one 0xC / up 0x18 …`）
        //   ⇒ **两段各 90°、连起来 180°**：背面 → 侧立（90°）→ 正面（0°）。
        //     🔴 那个 `.rdata` 常量**实读是 90，不是 180**（`工具/read_literal.py`：`0x1834b2e04` = 90.0f；
        //        同地址在本文件里已经是 `AttackTurnDeg` / `DeathShakeRandomness` 的出处，三处互相印证）。
        //        「翻 180°」是**两段之和**，⛔ 别把单段写成 180（写成 180 就一下转过去、回不来，
        //        而且与同址常量 90 自相矛盾）。⚠️ `资料/敌方手牌_原版规格.md:90` 的「按语义应是 180°」
        //        是**猜的**（同一行自承「字面值没读出来」）；`资料/普查产出_1016战斗/W_卡牌飞行族普查.md:27`
        //        照抄了这个猜测 —— **以实读的 90 为准**（铁律 5）。
        //   ⚠️ 我们这套卡是【正交平面上的两维片】，不是三维卡体：绕 Y 转 90° = 卡**侧立成一条线**
        //      （在原版那里就是「看不见的那一帧」）⇒ 视觉上就是「翻出去再翻回来」。
        //   ✅ 🔴 **2026-10-22（W64 · `B13` ①）就地订正（铁律 5）**：本行原来写「**「前半段显示卡背」
        //      这一半【没做】**，`CardView` 没有卡背层」—— **已过期**。`CardView` 2026-10-16 就加了卡背层
        //      （`CardView.SetBackVisible`：`_backLayer` / `CardBackZ = −0.20`、网格取**卡身** `Width × Height`），
        //      消费点就是 `DealInPose`：**0 s 亮卡背 → `DealDuration × TurnCardAroundFrac`（= 侧立那一帧、
        //      卡宽收到 0）换回卡面 → `OnKill` 补收**。原版机制 = 90° 那一帧
        //      `SetObjectVisibility(卡, 手牌态)` 换子件，我们的等价物就是那两条 `InsertCallback`。
        //      ⚠️ 敌方那一支**不翻**（`DrawCard.c:52-59` 判 `isPlayer`），它补的是
        //      `DealInQuad` 里那条 `DORotate(zero)`（2026-10-22 补）。

        /// <summary>抽牌翻转的**单段**角度 = **90°** —— `.rdata 0x1834b2e04` 用 `工具/read_literal.py`
        /// 实读 **90.0**（同一地址也是 `AttackTurnDeg` / `DeathShakeRandomness` 的出处）。
        /// ⚠️ **两段加起来才是 180°**，别拿这个常量当「总共转多少」。</summary>
        public const float TurnCardAroundDeg = 90f;
        /// <summary>翻转**每段**占整次抽牌时长的比例 = **0.25** —— `.rdata 0x1834b2e7c` 实读 **0.25**
        /// （`CardScript__TurnCardAround.c:32` 与 `CardScript__FinishCardDrawRotation.c:23` 两处同源）。
        /// ⇒ 两段共占抽牌时长的**一半**（我方 0.3 s ⇒ 每段 0.075 s）。</summary>
        public const float TurnCardAroundFrac = 0.25f;

        /// <summary>抽牌时那一记「翻面」= 两段各 <see cref="TurnCardAroundDeg"/> 度、
        /// 每段 `drawSeconds × TurnCardAroundFrac`，**线性**缓动（原版 `SetEase(1)`，两段都是）。
        /// 落点角 `restRotZ` 取自手牌那一档（原版终点是 `Vector3.zero`，我们的手牌有 z 倾角）。
        /// ⚠️ 必须经 `CardTween.Use`（批处理要 `UpdateType.Manual`、`SetLink` 防卡销毁后补间还在跑）；
        /// 叶子补间**自己也显式 `SetUpdate`** —— 嵌套序列的 `UpdateType` 在批处理的 Manual 档下
        /// 不靠继承（`Tween.SetUpdate` 不会往下传），这里是「两个模型下都对」的写法。</summary>
        public static Tween TurnCardAround(Transform tr, float restRotZ, float drawSeconds)
        {
            float half = drawSeconds * TurnCardAroundFrac;
            var seq = DOTween.Sequence();
            seq.Append(Flip(tr, new Vector3(0f, TurnCardAroundDeg, restRotZ), half));
            seq.Append(Flip(tr, new Vector3(0f, 0f, restRotZ), half));
            return CardTween.Use(seq, Ease.Linear, tr);
        }

        /// <summary>翻转的单段叶子补间（`RotateMode.Fast` —— 原版 `DORotate` 第 4 参传的是 0）。</summary>
        static Tween Flip(Transform tr, Vector3 to, float duration)
        {
            return tr.DORotate(to, duration, RotateMode.Fast).SetEase(Ease.Linear).SetUpdate(CardTween.Mode);
        }

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
            // 🆕 2026-10-23（新账-71）：**与 `MeleeAttack` 共用一个口** —— 原版 `ReassembleMinions` 那条
            //   `DOLocalMove` 所在的序列，`OnUpdate` 挂的是**同一份** `MinionScaleByPositionSO`
            //   （`CardScript___AfterAttack_b__370_0.c` 与近战那条逐字同形）。
            //   ⚠️ 今天零可见差：同行的让位 z 不变 ⇒ 系数恒 1；只有**督军位**那 0.42 / 0.75 的 z 偏移会动到它。
            var used = CardTween.Use(t, Ease.OutQuad, card.transform);
            HookDepthScale(used, card.transform, null);
            return used.SetDelay(delay);
        }

        /// <summary>
        /// 发牌入场 —— **落点由调用方显式给出**的那一版（`DealIn` 读的是卡**当时**的位置）。
        ///
        /// 🔴 🆕 2026-10-11（`A1540`）为什么要有它：`HandLayout.Place` 在 `animateRelayout == true` 时
        ///   **只开一条补间、不当场落位**（`Hand/HandLayout.cs` 的 `Place`），而 `DealIn` 是「**读当时位置**当落点」
        ///   ⇒ 同一帧里读到的是**父级原点**（= 画布正中 960,540）。手牌那一档**只有存盘场景里
        ///   `animateRelayout == 1`**（批处理里出厂 `false`）⇒ **自检结构上复现不出来**。
        ///   ⇒ 调用方先 `SetPose` 落实、再把真位姿传进来（`BattleDriver.SyncHand`）。
        ///
        /// ⚠️ `to` 是**世界坐标**（与 `DOMove` / `DealIn` 同一套）。
        /// ⚠️ `rotZ` / `scale` 也取**落点那一套**，⛔ 不是 `DealIn` 的「一律 0 / 1」——
        ///   实测（`_tmp_view/ui_1019/17_battle.png`）按 0 / 1 收尾时，牌会停在**原尺寸**
        ///   （226 设计 px，而手牌档应是 165）且不倾斜。
        /// </summary>
        public static Tween DealInPose(CardView card, Vector3 from, Vector3 to,
                                       float rotZ, float scale, float delay = 0f,
                                       Texture2D back = null)
        {
            if (card == null) return null;
            var tr = card.transform;
            tr.position = from;
            // ⚠️ alpha 要**当场**置 0（同 `DealIn`：DOTween 的 `To(getter, setter, …)` 第一次被推进时才调 setter）
            card.SetAlpha(0f);

            var seq = DOTween.Sequence();
            seq.Append(tr.DOMove(to, DealDuration).SetEase(Ease.OutCubic));
            // 🔴 **2026-10-16：这一条从「一路 OutCubic 转到底」换成原版那记翻面**
            //    （`CardScript.TurnCardAround` + `FinishCardDrawRotation`，判据与出处全在
            //     `TurnCardAround` 上面那一段头注里，这里不抄第二份）。
            //    ⚠️ 只换**我方**这条（`DealInPose` = 抽自己的牌）；敌方那条走 `DealInQuad`，
            //       原版对敌方**不翻**（`DrawCard.c:52-59` 判的 `isPlayer`）⇒ 那条不动。
            seq.Join(TurnCardAround(tr, rotZ, DealDuration));
            // 🔴 2026-10-23（新账-74）：缩放走 `CardView.SetBaseScale` 的**唯一口**（`ScaleTo`）——
            //   原来那句 `tr.DOScale(...)` 直写 `transform.localScale` ⇒ 补间跑完 `_baseScale` 仍是旧值，
            //   下一次 `SetPose`（布局 / 切分辨率）会把它弹回去（`T04` 那一族，静默）。
            //   ⚠️ 这里的 `scale` 与 `SetPose` 第三参**同一个口径**（**基础**缩放 —— 落位时布局还会再写一遍）。
            var sc = ScaleTo(card, tr, scale, DealDuration, Ease.OutCubic);
            if (sc != null) seq.Join(sc);
            seq.Join(DOTween.To(() => 0f, a => card.SetAlpha(a), 1f, DealDuration * 0.5f));
            // 🆕 2026-10-16：**翻面的前半段看的是卡背**（判据 → `CardView.SetBackVisible` 那段头注）。
            //   两段各占 `DealDuration × TurnCardAroundFrac`（0.25）⇒ 「侧立那一刻」在
            //   `DealDuration × 0.25` 处，**卡宽正好收到 0** ⇒ 在那一刻换回卡面**看不见**。
            //   ⚠️ 用 `InsertCallback` 而不是在方法体里直接调 —— 直接调会在 `delay` 那一段就亮着卡背。
            if (back != null)
            {
                seq.InsertCallback(0f, () => { if (card != null) card.SetBackVisible(true, back); });
                seq.InsertCallback(DealDuration * TurnCardAroundFrac,
                                   () => { if (card != null) card.SetBackVisible(false, null); });
            }
            var t = CardTween.Use(seq, Ease.Linear, tr).SetDelay(delay);
            // ⚠️ **被打断时要把 alpha 补回 1**（同 `DealIn`：`Place` 会 `DOKill()` 这张卡身上的补间）
            //    🆕 卡背也要收掉 —— 死在翻面半路的话，它会**一直盖着卡面**（静默）。
            t.OnKill(() => { if (card != null) { card.SetAlpha(1f); card.SetBackVisible(false, null); } });
            return t;
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
            // 🔴 2026-10-23（新账-74）：同 `DealInPose` —— 只经 `CardView.SetBaseScale`（`ScaleTo`），
            //   ⛔ 不再 `tr.DOScale` 直写 `localScale`。目标 1 = **基础**缩放（手牌档的静止值）。
            var sc = ScaleTo(card, tr, 1f, DealDuration, Ease.OutCubic);
            if (sc != null) seq.Join(sc);
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
        /// 🔴 **2026-10-16 订正**（原来写「原版抽牌那一瞬还会 `TurnCardAround`；我们没做那一下翻转」）：
        ///   **原版这一档【不翻】** —— `CardScript__DrawCard.c:52-59` 判的是 `EntityScript.isPlayer`，
        ///   `TurnCardAround` 只在 **`isPlayer == true`** 那一支；敌方那一支走 `SetObjectVisibility`，
        ///   而且它那条 `DORotate(Vector3.zero, 时长)`（`:65-75`）是**我方不走的**。
        ///   ⇒ **没做的那一半只在【我方】（`DealInPose`），已经补上了**（见 `TurnCardAround`）；
        ///     本方法（敌方）**判据上就不该有翻转**，保持不动。
        ///   ⚠️ `资料/敌方手牌_原版规格.md:109` 把这一支写成「**仅 isPlayer==0 才翻面**」—— **读反了**（铁律 5）。</summary>
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
            // 🆕 2026-10-22（W64 · `B13` ②）：**敌方那一支的 `DORotate(zero)`**（原来漏了）。
            //   判据 = `decomp_full/CardScript__DrawCard.c:65-75`：`if (isPlayer == 0)` 那一支里
            //   `DORotate(transform, Vector3.zero, 时长)` —— 它**只有敌方走**（我方那一支归
            //   `TurnCardAround` 管，见 `TurnCardAround` 上面那段头注）。
            //   ⚠️ 原版这一句**没有 `SetEase`** ⇒ 吃 DOTween 的全局默认缓动。本工程
            //   `Assets/Resources/DOTweenSettings.asset:31` 实读 `defaultEaseType: 6`（= `OutQuad`）
            //   ⇒ 这里写 `Ease.OutQuad`（审计文里那句「OutCubic＝DOTween 默认」与 .asset 实读不符，
            //   按 .asset）。
            seq.Join(tr.DORotate(Vector3.zero, d).SetEase(Ease.OutQuad));
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

        // ⚠️ 原来的 `Sign(flatDir)`（旋转 punch 的左右符号）**已随 `B02` 删除** —— 近战挨打没有旋转了，
        //    它没有任何消费者。⛔ 别因为「看起来还会用到」把它加回来（那就等于把自造旋转又接上了）。

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
        // · ~~`timeToLand 0.2` / `timeBeforeLand 0.05`~~ ✅ **2026-10-16 已接上**（`DeploySequence.LandTime` /
        //   `DeploySequence.HoldTime`）。**订正痕迹**：本行原来写着「✅ 这一条已经接上了」而实据是**零消费**
        //   （同一个括号里又自承「等有落地特效时再用」，两说打架）—— 那次订正成「未接」，今天真的接上了。
        //   判据 → `资料/普查产出_1016战斗/W_卡牌飞行族普查.md` §一 #1/#1b · `资料/3DBody_原版场上卡体规格.md` §四。
        //   ⚠️ **两处与那份规格文档的说法不同（反编译实读，以反编译为准）**：
        //     ① 「落地前 0.05 s 起播落地反馈」**不成立** —— `timeBeforeLand` 那一段在
        //        `_MinionPlayedIntoField_d__314` 的 **state 1**（= 下落**开始之前**），而 `LandOnGround`
        //        在 **state 3**（= `WaitForSeconds(timeToLand)` **之后**）⇒ 落地反馈在**落定那一刻**，
        //        不是提前 0.05 s。极性判据：飞行路传的 `param4 = 1` 走 state 3、Teleport 路传 0 走 state 0
        //        （`_ResolvePlayCardFromHand_d__447…:659-661,892-894`）。
        //     ② 原版在 state 0 还有一段 `WaitForSeconds(timeToScaleIntoMoving = 0.1)`（`VarsGlobal` 字段，
        //        `资料/VarsGlobal_原版数值.md:43`）—— **我们没做**：它覆盖的是「卡片换成 3D 体」那一拍
        //        （state 1 `SetUnitInPlayLayer`），而我们的整套交接（`BattleDriver.DoPlay`）挂在序列**末尾**，
        //        照抄会得到一段**什么都不发生的死等**。如实记，别当已复刻。
        // · ~~`minionConversionHeight`（**我方 1.0 / 敌 2.1**，竞技场单位）~~ ✅ **2026-10-16 已接上** —— 判据落在
        //   `BoardLayout.PlayerMinionConversionHeight` / `EnemyMinionConversionHeight`（+`MinionConversionHeight`），
        //   消费点 = `BoardLayout.DropTargetWorldAtHeight` ← `Hand/CardInteraction.Release`。
        //   它同时**取代了**原来那两句**我们挑的**抬升常量（`LiftHeight 0.35` / `LiftFraction 0.35`，已删）。
        // · ~~`shakeWhenLand`（落地震镜头）~~ ✅ **2026-10-16 已接上** —— `ShakeCameraForLand`（同 `_MinionPlayedIntoField`）。
        //   ⚠️ 它和落地**音**（`BattleDriver.PlayBattleSfx3D(BattleSfx.LandOnGround, …)`）、落地**扬尘**
        //   （`BattleDriver` 的 `LandDustFieldFor`）现在是**三件**，都跟着同一条 `EvtKind.Deploy` 引擎事件走。
        // · `landOnGroundParticles` / `…Legendary`（落地扬尘的两件 prefab）+ `landOnGroundParticlesLocalOffset (0,0.015,0)`
        //   —— ✅ **2026-10-16 已接**（`BattleDriver.LandDustFieldFor`）。⚠️ **一处没做到、如实记**：原版
        //   把它们 `SetParent(MinionManager)` 之后 `localScale = Vector3.one × desiredScale`（0.36/0.69，
        //   `CardScript__LandOnGround.c:87-106`），而我们的 `CardEffects.Fire` 钩子**没有 scale 参数**
        //   （`WarpforgeVFX/Runtime/WarpforgeEffectHook.cs` 硬编码 `1f`，那个文件不在本件白名单）⇒ 现在按
        //   prefab 自带尺度播，**多半比原版大**（原版是 0.36 倍）。
        // · `cardMovementSpeed 10.8` —— 卡的移动速度。⚠️ **用途查不到**：调用点没被反编译，
        //   拿它算「手牌飞多久」（距离÷速度）会得到 0.12s 这种明显不对的数 —— **别当它是时间基准**
        // · ~~`meleeHitCameraShakePreset`（amplitude 2.0）—— 挨打震镜头；要做得先决定
        //   2D 战场怎么表达 3D 的相机抖动（动相机？动整个背景 quad？）~~ —— ✅ **2026-09-17 已接上**：
        //   见上面「① 挨打震镜头」那一节与 `ShakeCamera`。原版问的那个问题**原版自己有答案**：
        //   震**主相机**（Cinemachine Impulse）、**不震 UI**。我们单相机分不开，
        //   所以落地方式是「相机 + HUD 根**同向**等量平移」，观感与原版一致。
    }
}
