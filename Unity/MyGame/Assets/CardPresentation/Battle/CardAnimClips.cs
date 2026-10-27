// CardAnimClips.cs — 逐卡「技能动画」的**播放参数**（`AnimInfo` 里那几个本地没进运行期的字段）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【为什么需要这一层】
//   `card_vfx_by_card.json`（运行期那张）只带 **prefab 名 + 时长**（`VfxMap.ResolvedVfx`），
//   而原版播一拍技能动画还要三样位置参数：`startPosOption` / `endPosOption` / `pointTowardsOption`
//   与一个开关 `shouldMoveVFX`（判据 → `AnimInfo` 的字段表 `dump.cs:25205-25243`，逐档语义 → `Battle/AnimAnchors.cs`）。
//   这三样在**运行期 Resources 里没有**：
//     · `Resources/card_anim_order.json` 只收 `activeAbility`（62 张 / 99 拍，见 `CardAnimOrder` 文件头）；
//     · `CardDef.AnimTimes`（引擎数据）只有时间与 trigger 名，**没有**那三个 option；
//     · `数据/游戏数据/animinfo_lookup.json` 有，但它只覆盖 418 条、且不在 Assets 里。
//
// 【数据从哪来（2026-10-23 起）】**`Resources/cardanim_clips.json`** ——
//   由 `工具/gen_cardanim_clips.py` 从 `assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/
//   <CardAnim>.json` 的 `animInfo` 烘出来（**幂等**：连跑两遍 md5 相同）。现读 **1,067 行**。
//   ⚠️ 工作副本另有一份在 `Unity/数据/游戏数据/cardanim_clips.json`（**逐字节相同**，给工具/人看）。
//   🔴 **键 = `CardAnim` 资产名（`a`），⛔ 不是 prefab 名**：同一个 prefab 名会被多张 CardAnim
//   共用、参数却不同（现读 **215 组**，例 `Trait_GainQuest` 之于 `Trait_GainQuest_self`(sp=0) 与
//   `…_enemywarlord`(sp=4)）⇒ 按 prefab 名查必错。运行期那一侧由 `VfxMap.ResolvedVfx.Anim`
//   （= `CardPick.A`）把 CardAnim 名交出来。
//
// 【取不到怎么办】**出声 + 显式降级**（⛔ 没有「代码里的兜底表」—— 两份数据迟早不一致）：
//   · `Resources/cardanim_clips.json` 载不到 / 解析失败 ⇒ `Debug.LogError` **一次**，`Loaded = false`；
//   · 每一条查不到的 CardAnim ⇒ `Debug.LogWarning`（去重、上限 16）；
//   · **降级后玩家看到什么**：技能动画那一件**照播**，但它的起点/终点/朝向都退回
//     `sp = ep = pt = 0`（`PositionOption.self` = **施法者自己那一格**）且**不位移**
//     ⇒ 本该飞向目标 / 落在敌方督军 / 落到棋盘中心的那类件，会**贴在单位身上**播完。
//     ⛔ 不会静默：控制台有 error + warning，自检断 `CardAnimClips.Loaded`。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一条 `AnimInfo` 的「位置/移动」参数（判据见文件头）。</summary>
    public struct CardAnimPlacement
    {
        /// <summary>`startPosOption`（= `PositionOption` 数值）。</summary>
        public int StartPos;
        /// <summary>`endPosOption`。</summary>
        public int EndPos;
        /// <summary>`pointTowardsOption`。</summary>
        public int PointTowards;
        /// <summary>`shouldMoveVFX` —— 起→终位移的总开关（⛔ 别用 `sp != ep` 当替身）。</summary>
        public bool Move;
        /// <summary>`vfxDelayTime`（秒）。
        /// 🔴 **2026-10-23（P2 · `新账-75④`）**：它**只**用于 `GetAnimDuration()`（= `vd+ts+tm+te`）与
        /// 自检复算 —— ⛔ **别再加进「起播时刻」**：`VfxMap.ResolvedVfx.T` 里已经折算过同一次延迟
        /// （`T = Offset + CardAnimTime.Delay`），两处都加 = 等两次（原版 `_DelayedVFXPlay:17-27` 只等一次）。
        /// 消费点原来那处已删（`BattleDriver.PlayUnitSkillAnim`）。</summary>
        public float VfxDelay;
        /// <summary>🆕 **2026-10-25（`AU1` · `新账 N4`）：`startDelay`（秒，`AnimInfo` 偏移 `0x28`）**
        /// —— **每一拍起播之前先等这么久**（再进 `DelayedVFXPlay` 的 `vfxDelayTime`）。
        ///
        /// <para>**判据（第一权威）= `BattleManager._ResolveAnim_d__466__MoveNext.c:67-75`**：
        /// state 0 的末尾是 `WaitForSeconds(*(float *)(AnimInfo + 0x28))` → state 1 才
        /// `StartCoroutine(DelayedVFXPlay)` + 震屏/`UnitShake`/`UnitTweenSO`。
        /// `dump.cs` 的 `AnimInfo` 字段序（`vfxDelayTime 0x24` → **`startDelay 0x28`** → `timeAtStartPos 0x2c`）
        /// 与 `AnimInfo__GetAnimDuration.c` 读的 `0x24/0x2c/0x30/0x34` 逐项对齐 ⇒ `0x28` 就是 `startDelay`。
        /// ⚠️ **它不算进 `GetAnimDuration()`**（那是四字段直加）⇒ 只影响**起播时刻**，不影响这一拍占多久。</para>
        ///
        /// <para>**数据面**（`Resources/cardanim_clips.json` 的 `sd` 列，生成器只在非 0 时写）：
        /// 全池现读 **1 行** = `Cuts Brutal Warp Spider` **0.5 s**（`Empyric Ambush SaimHann` 的 `play` 桶）。
        /// ⚠️ 改前**整列零消费**（N4）⇒ 那一件比原版早 0.5 s 起播。默认 `0` = 原版绝大多数件。</para></summary>
        public float StartDelay;
        /// <summary>`timeAtStartPos`（秒）—— `PlayAbilityBeat` 的 `ts`。</summary>
        public float Ts;
        /// <summary>`timeMoving`（秒）—— `PlayAbilityBeat` 的 `tm`。</summary>
        public float Tm;
        /// <summary>`timeAtEndPos`（秒）—— **播放不用**它；只给 `GetAnimDuration()`（= `vd+ts+tm+te`）复核用
        /// （`CardAnimClips.DurationOf`；自检拿它核 `VfxMap.SharedRangedVfxDuration` 那几条「同 prefab 名多 CardAnim」）。</summary>
        public float Te;

        // ── 🆕 2026-10-23（`新账-83` · 弹道三列）：`AnimInfo` 的「VFX movement」那几位 ──
        //   判据 = `CardAnimController__Update.c:36-97` + `CardAnimController__Initialize.c:60-90`
        //   （字段偏移 `d:/2/tools/il2cpp_out/dump.cs:25219-25234`）。短名与
        //   `Resources/card_anim_order.json` 的 `CardAnimBeat` **逐字相同**（同一套运行期读取口径）。

        /// <summary>`easeCurve`(0x40) 的扁平关键帧 `[t0,v0,t1,v1,…]`（升序）。空 / null = 线性
        /// （原版默认曲线 `(0,0)→(1,1)`）。⚠️ **不飞的件（`Move == false`）这一列不写**（读它没意义）。
        /// 唯一非线性的是 `Jain Zar / Atk_JainasMor_Throw_Return`（飞出去再飞回来）。</summary>
        public float[] Ease;

        /// <summary>`orientToVelocity`(0x48) —— 移动期间**每帧**把特效转向速度方向
        /// （`Update` 里 `if (otv &amp;&amp; shouldMoveVFX)` 那个门）。现读 33 个攻击 movers 里 **22** 件为真。</summary>
        public bool OrientToVelocity;

        /// <summary>`applyMovementOffsets`(0x49) —— 位移 lerp **之后**要不要叠逐轴偏移曲线。
        /// 现读 33 个攻击 movers 里 **26** 件为真（其中 22 件带非零弧线 = **手雷/炮弹的抛物线**）。</summary>
        public bool OffsetAo;
        /// <summary>`offsetMovementAxis.useNonModifiedAnimTime`(0x10)：真 = 用**原始** `t01` 求值、
        /// 假 = 用**缓动后**的 `e`（`Update` 里 `fVar7 = ont ? t01 : e`）。</summary>
        public bool OffsetOnt;
        /// <summary>三轴偏移曲线（同 <see cref="Ease"/> 的扁平约定；空 = 0 = 该轴不偏移）。</summary>
        public float[] Ofx, Ofy, Ofz;
        /// <summary>三轴随机幅度区间 `[min,max]`（`minMax{X,Y,Z}Offset`；每拍初始化时抽一次）。</summary>
        public float Mnx, Mxx, Mny, Mxy, Mnz, Mxz;

        // ── 🆕 2026-10-25（`AU1` · `J3` §2）：`minionMovementInfo` 的**三轴旋转** ──
        //  判据（`J3` §2 现查，第一权威 `_d__358:284-365` + `CardScript__ApplyMovementDisappearAnimRotations.c`）：
        //   · 轴 = **世界 right(X) / up(Y) / forward(Z)**（`Vector3` 静态块 `+0x3C / +0x18 / +0x48`，
        //     `il2cpp.h:5149-5160` 的声明序 + 四条独立互证）；
        //   · 单轴：`enable==0 ⇒ identity`；否则 `Quaternion.SlerpUnclamped(startRot, AngleAxis(maxValue 度, axisVec),
        //     curve.Evaluate(elapsed/animTime))` —— 🔴 **必须 Unclamped**（曲线中键 −0.952，钳到 0 会抹掉「先反拧再甩」）；
        //   · 连乘：`transform.rotation = qX * qY * qZ * startRotation`，**三次 Apply 的 `cur` 都是 `startRotation`**。
        //  ⚠️ `maxValue` 单位 = **度**（现读两行都是 45.0）；字段名 = `rotateXAxis/YAxis/ZAxis`
        //     （`AnimationDefinition{enable@0x10, curve@0x18, maxValue@0x20}`）。

        /// <summary>`rotateXAxis`（`+0x40`）的 `enable` —— 绕**世界 X（right）**转不转。</summary>
        public bool RotXOn;
        /// <summary>`rotateXAxis.maxValue`（**度**）。</summary>
        public float RotXDeg;
        /// <summary>`rotateXAxis.curve`（扁平 `[t,v,…]`；`null` = 该轴没有曲线 ⇒ 按 identity 处理）。</summary>
        public float[] RotXCurve;
        /// <summary>`rotateYAxis`（`+0x48`）的 `enable` —— 绕**世界 Y（up）**。</summary>
        public bool RotYOn;
        /// <summary>`rotateYAxis.maxValue`（度）。</summary>
        public float RotYDeg;
        /// <summary>`rotateYAxis.curve`。</summary>
        public float[] RotYCurve;
        /// <summary>`rotateZAxis`（`+0x50`）的 `enable` —— 绕**世界 Z（forward）**。</summary>
        public bool RotZOn;
        /// <summary>`rotateZAxis.maxValue`（度）。</summary>
        public float RotZDeg;
        /// <summary>`rotateZAxis.curve`。</summary>
        public float[] RotZCurve;

        // ── 🆕 2026-10-25（`AU1` · `J1` §9/§10）：`minionMovementInfo` 与 `screenShake` ──
        //  判据（J1 现查，第一权威仍是 `BattleManager._ResolveAnim_d__466__MoveNext.c`）：
        //   · `:165-233`：`animInfo+0x70 != null` 时走 `CardScript.MoveAndDisappearAnim(...)` 并 `yield`
        //     —— 与「等 `GetAnimDuration()`」是 **if/else** ⇒ **这一拍时长变成 `delay + animTime`**（不是原来的
        //     `GetAnimDuration`）。`shouldActivateMinionMovement(+0x14) == 0` 时退回 `GetAnimDuration`。
        //   · `:79-105`：`DelayedVFXPlay` 起播的**同一帧**，`if (preset != null) OverwriteCameraShakePreset.Play()`
        //     （`CanPlay()` 就只判 preset 非空）⇒ 屏震与这一拍**并行**，不进时长。
        //  ⚠️ **运行期表的数据已在 2026-10-25 由 `TL` 补齐**（`工具/gen_cardanim_clips.py`）：现读
        //     `mvt==1` **2 行**（`Scarab_Explosion` / `SporeMine_Explosion`，`mvd=0` `mva=0.6` `mvvis=0`
        //     `mvxz=1` `mvy=1` `mox/moy/moz = −0.1/0.2/−0.1`）、`shk` **57 行**（10 个 preset）。
        //     缺键仍是 0/null = 「这一档不成立」（旧表 / 别的生成器跑的副本）。

        /// <summary>`minionMovementInfo.shouldActivateMinionMovement`（`+0x14`）—— 这一拍要不要**卡本体沿曲线位移**。
        /// 真 ⇒ 这一拍时长 = <see cref="MoveBodyDelay"/> + <see cref="MoveBodyTime"/>（`J1` §9）。</summary>
        public bool MoveBody;
        /// <summary>`minionMovementInfo.delay`（`+0x10`）—— 位移前先等多久（`_d__358:71-77`）。</summary>
        public float MoveBodyDelay;
        /// <summary>`minionMovementInfo.animTime`（`+0x58`）—— 位移本身跑多久（`_d__358:269-283` 的 `WaitUntil`）。</summary>
        public float MoveBodyTime;
        /// <summary>`visibilityStateAfterFinish` —— 位移结束后卡本体的可见性（现读 2 行都是 **0 = 隐藏**）。</summary>
        public int MoveBodyEndVis;
        /// <summary>`useXZCurve` / `useYCurve` —— 哪几条曲线参与（现读 2 行都是 1/1）。</summary>
        public bool MoveUseXZ, MoveUseY;
        /// <summary>三条位移曲线（扁平 `[t,v,…]`，同 <see cref="Ease"/> 的约定）。</summary>
        public float[] MoveCurveX, MoveCurveY, MoveCurveZ;
        /// <summary>三轴偏移（`xOffset/yOffset/zOffset`；现读 2 行都是 `(-0.1, 0.2, -0.1)`）。</summary>
        public float MoveOffX, MoveOffY, MoveOffZ;

        /// <summary>`screenShake.presetSO` 解析出来的 **preset 名**（空 = 这一拍不震；原版 `CanPlay()` 只判非空）。
        /// 消费点 = 拍子起播**同帧**（`_ResolveAnim:79-105`），走现成的 `WFModuleScreenShake.Resolve` + `OnShake`。
        /// ✅ **2026-10-25（`TL` §3）**：生成器已做 **PathID → 名字**那一跳（`m_Script` pid + `m_Name` 判据），
        /// 现读 **57 行全部解出名字**、distinct **10 个**、且都在这 17 个 preset 里；解不出的那档生成器**出声 + 留空**（⛔ 不给默认 preset）。</summary>
        public string ShakePreset;
        /// <summary>`screenShake.delay`（现读取值 0 / 1.0）。</summary>
        public float ShakeDelay;
        /// <summary>`screenShake.amplitude` 覆盖值（现读 34 条 0 = 用 preset 自身、1 条 4.0、1 条 6.0）。</summary>
        public float ShakeAmplitude;
        /// <summary>`screenShake.frequency` 覆盖值（同上，0 = 用 preset 自身）。</summary>
        public float ShakeFrequency;
        /// <summary>`overwriteAmplitude` / `overwriteFrequency` 两个开关（0/1）。</summary>
        public int ShakeOverAmplitude, ShakeOverFrequency;

        /// <summary>这一拍在进度 <paramref name="t01"/> / <paramref name="pEased"/> 处的偏移量
        /// （= 曲线值 × 随机幅度）。**判据只此一处** = `CardAnimOrder.OffsetAt`
        /// （`CardAnimBeat.OffsetAt` 也转发到那儿）。`OffsetAo == false` ⇒ 零。</summary>
        public Vector3 OffsetAt(float t01, float pEased, Vector3 randomAmp)
        {
            return CardAnimOrder.OffsetAt(OffsetAo ? 1 : 0, OffsetOnt ? 1 : 0,
                                          Ofx, Ofy, Ofz, t01, pEased, randomAmp);
        }

        // ── 🆕 2026-10-27（执行代理 `B9_W12USHK`）：`unitsShake`（`+0x78`）与 `minionAnimationClipInfo`（`+0x68`）──
        //   判据：字段表 `d:/2/tools/il2cpp_out/dump.cs:25239-25243` + `:25498-25511`（`UnitShake.UnitShakeInfo`）
        //   + `:25310-25340` / `:25259-25305`（`MinionAnimClipInfo` 及其 Definition）；
        //   消费点 `BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:64-108`（远程那一路）
        //   与 `BattleManager._ResolveAnim_d__466__MoveNext.c:108-125`（**同一段代码**，play/ability/trigger/
        //   attack_melee 那些桶也吃）—— 两处都是「`unitsShake` 非空 ⇒ 逐条 `ResolveUnitShake`，
        //   **否则**（if/else）才 `tweenAnim`」。
        //   数据 → `工具/gen_cardanim_clips.py` 的 `_unit_shake_block` / `_minion_clip_block`。

        /// <summary>`AnimInfo.unitsShake`（`+0x78`）—— 这一拍**逐条**要抖 / 要弹开的单位列表
        /// （原版遍历 List 逐条 `ResolveUnitShake`，**不是**只取第一条）。
        /// 空 / `null` = 这一拍没有 unit shake（现读 1,067 行里 **257 行有**，共 271 条）。
        /// 🔴 **两个口径别互相判红**：① **数据侧有效 = 36 条 / 35 个 CardAnim**（`TargetUnit ∈ {20,30}`；其余 235 条 `TargetUnit == 0` ⇒ 原版空转）；
        /// ② **与卡池求交后可达 = 28 条 / 28 个 CardAnim**（`B9_W29BDR2` 复核）。这 28 条**全部**是 `(target=30, awayFrom=10)`
        /// ⇒ 走的是**击退支**（`DoPushBack`），**不是** `DoUnitShake` 抖动支。⚠️ 消费口 = `CardFeel.ResolveUnitShake(...)`
        /// —— **每个元素调一次**，顺序 = 数组顺序（= 原版 List 顺序）。</summary>
        public CardAnimUnitShake[] UnitShakes;

        /// <summary>`minionAnimationClipInfo.enable`（`+0x68`）—— **0 ⇒ 卡体 `AnimationClip` 这条链整段不执行**
        /// （`_d__466:166-171` 是 `if (enable) ResolveAnimAnimationClip(...)`）。
        /// 现读全池 **2 行且都是 0** ⇒ 今天这条链**一次都不会跑**（见 `CardAnimClips.BodyClipOf` 那段）。</summary>
        public bool BodyClipEnabled;

        /// <summary>`minionAnimationClipInfo.delay`（秒）。现读 2 行都是 0。</summary>
        public float BodyClipDelay;

        /// <summary>`minionAnimationClipInfo.minionClips[]`（`null` = 没有）。现读 2 条，
        /// 且两条的 `AnimationClip` 引用**都是 null**（`m_PathID = 0`）⇒ 即便打开了 `enable` 也什么都不播。</summary>
        public CardAnimBodyClip[] BodyClips;

        /// <summary>这一拍**有没有一条真能生效**的 unit shake（`TargetUnit != 0`）。
        /// <para>🔴 **「表里有这一条」≠「一定会抖」**：`targetUnit == 0(undefined)` 时原版
        /// `BattleManagerSupport.GetTargetUnit` 对 0/其它值一律 `return null`
        /// （`decomp_full/BattleManagerSupport__GetTargetUnit.c:13-16`）⇒ 协程第一个非空检查就
        /// `return 0`（`_d__472:48-54`，只把 `shakeDelay` 白等掉）。现读 271 条里 **235 条是 `t == 0`**。
        /// ⛔ 别为了方便拿 `targetCard` 去顶 `t == 0` —— 那是静默多抖一次。</para></summary>
        public bool HasUnitShake
        {
            get
            {
                if (UnitShakes == null) return false;
                for (int i = 0; i < UnitShakes.Length; i++)
                    if (UnitShakes[i].Effective) return true;
                return false;
            }
        }
    }

    /// <summary>一拍要播的技能动画（= <see cref="CardAnimPlacement"/> + `VfxMap` 解出来的名字/时长）。</summary>
    public struct CardAnimClip
    {
        /// <summary>`CardAnim` 资产名（查 <see cref="CardAnimPlacement"/> 的键）。</summary>
        public string Anim;
        /// <summary>prefab 名（空 = **纯计时拍**，原版合法：`Wait_1s` 那种，只占时长不出件）。</summary>
        public string Prefab;
        /// <summary>相对事件起播的偏移（秒）= 组内 `Offset` + 自身 `vfxDelayTime`（`VfxMap.ResolvedVfx.T`）
        /// ＋🆕 自身 `startDelay`（<see cref="CardAnimPlacement.StartDelay"/>，2026-10-25 `AU1`）。</summary>
        public float T;
        /// <summary>这一拍演多久（秒，`AnimInfo.GetAnimDuration()`；0 = 没有逐卡时间）。</summary>
        public float Duration;
        public CardAnimPlacement Place;
        public bool IsTimingOnly { get { return string.IsNullOrEmpty(Prefab); } }
    }

    /// <summary>`VfxMap.UnitAttack` / `VfxMap.UnitDamaged` 两桶的运行期取数口。</summary>
    public static class CardAnimClips
    {
        /// <summary>`Resources` 里那张表的路径（**没有扩展名**）。生成器 → `工具/gen_cardanim_clips.py`。</summary>
        public const string ResourcePath = "cardanim_clips";

        // ---- 运行时那份 JSON 的形状（`JsonUtility` 只吃数组，不吃字典）----
        [Serializable] public class RtRow
        {
            public string a;      // CardAnim 名（**主键**）
            public string n;      // prefab 名（诊断用）
            public int sp, ep, pt, mv;
            public float vd, ts, tm, te;
            /// <summary>🆕 2026-10-25（`AU1`）：`startDelay`（只在非 0 时由生成器写 ⇒ 缺键 = 0）。
            /// 判据与消费点见 <see cref="CardAnimPlacement.StartDelay"/>。</summary>
            public float sd;
            // 🆕 2026-10-23（`新账-83`）：`AnimInfo` 的「VFX movement」那几位（短名同 `CardAnimBeat`）
            public int otv;                       // orientToVelocity
            public float[] ease;                  // easeCurve（扁平 [t,v,…]；只对 mv==1 的行写）
            public int ao, ont;                   // applyMovementOffsets / useNonModifiedAnimTime
            public float[] ofx, ofy, ofz;         // 三轴偏移曲线
            public float mnx, mxx, mny, mxy, mnz, mxz;   // 三轴随机幅度区间
            // 🆕 2026-10-25（`AU1` · `J1` §9/§10）—— ✅ **生成器已由 `TL` 补齐**（`gen_cardanim_clips.py`，现读 2 行 / 57 行）：
            //   · 卡本体位移：`mvt`(shouldActivateMinionMovement) `mvd`(delay) `mva`(animTime)
            //     `mvvis`(visibilityStateAfterFinish) `mvxz`/`mvy`(useXZCurve/useYCurve)
            //     `mvcx`/`mvcy`/`mvcz`(三条曲线，扁平 `[t,v,…]`) `mox`/`moy`/`moz`(三轴偏移)
            //   · 屏震：`shk`(**preset 名**，空 = 无) `shkd`(delay) `shka`(amplitude) `shkf`(frequency)
            //     `shkoa`/`shkof`(两个 overwrite 开关)
            //   缺键 ⇒ 0 / null = 改前行为（JsonUtility 不吃不来就不赋值）。
            public int mvt, mvvis, mvxz, mvy;
            public float mvd, mva, mox, moy, moz;
            public float[] mvcx, mvcy, mvcz;
            public string shk;
            public float shkd, shka, shkf;
            public int shkoa, shkof;
            // 🆕 2026-10-25（`AU1` · `J3` §2）：`minionMovementInfo` 的**三轴旋转**（生成器 2026-10-25 前就在写，现读 10 行）：
            //   `mmRxE/mmRxM/mmRxC`（X 轴 enable / maxValue 度 / curve 扁平）· 同形 `mmRy*` / `mmRz*`。
            public int mmRxE, mmRyE, mmRzE;
            public float mmRxM, mmRyM, mmRzM;
            public float[] mmRxC, mmRyC, mmRzC;
            // 🆕 2026-10-27（执行代理 `B9_W12USHK`）：两族原来**一整片零数据**的字段（判据见类尾那两个 DTO 的 doc）
            //   · `ush`  ← `AnimInfo.unitsShake`（`+0x78`）：`{t,aw,sz,du,dl}` 的数组；现读 257 行 / 271 条。
            //   · `mac*` ← `AnimInfo.minionAnimationClipInfo`（`+0x68`）：`macEn`/`macD`/`macs[]`；
            //              现读 2 行（`macEn` 全 0、`macs[].clip` 全 "0" = null ⇒ 今天无可观测差）。
            //   缺键 ⇒ `null` / 0 = 改前行为（`JsonUtility` 不给就保持默认）。
            public RtUnitShake[] ush;
            public int macEn;
            public float macD;
            public RtMinionClip[] macs;
        }

        /// <summary>`ush[]` 的一条 —— 原版 `UnitShake.UnitShakeInfo`（`dump.cs:25498-25511`）。
        /// 字段名 = 生成器 `_unit_shake_block` 的短名，⛔ 别改名（改了两边一起改）。</summary>
        [Serializable] public class RtUnitShake
        {
            /// <summary>`targetUnit`（`TargetUnit` 枚举**原样**：0 undefined · 10 self · 20 cardTarget ·
            /// 30 abilityTarget · 40 friendlyHero · 50 enemyHero —— `dump.cs:45603-45613`）。**0 = 原版解不出目标 ⇒ 空转**。</summary>
            public int t;
            /// <summary>`awayFromUnit`（同上枚举）。非 0 且解出的卡 ≠ `t` 解出的卡 ⇒ 走**击退支**（不是抖动）。</summary>
            public int aw;
            /// <summary>`shakeSize`（`UnitSize` 枚举：0 small · **2 medium** · 3 big —— `dump.cs:101810-101817`）。
            /// ⚠️ 不是 0/1/2 ⇒ 拿到 1 说明数据换过口径，别按「1 = medium」硬算。</summary>
            public int sz;
            /// <summary>`shakeDuration`（**秒**；只对抖动支生效 —— 击退支原版把 duration 写死 0.4）。</summary>
            public float du;
            /// <summary>`shakeDelay`（**秒**；>0 时先 `WaitForSeconds` 再判 `IsInPlay` 与二选一）。</summary>
            public float dl;
        }

        /// <summary>`macs[]` 的一条 —— 原版 `MinionAnimClipInfo.MinionAnimClipInfoDefinition`（`dump.cs:25259-25305`）。</summary>
        [Serializable] public class RtMinionClip
        {
            /// <summary>`target`（`TargetUnit` 枚举）。现读 2 条都是 10(self)。</summary>
            public int t;
            /// <summary>`animationClip.m_PathID` 的**十进制字符串**；`"0"` = **null 引用**（现读 2 条都是 "0"）。
            /// ⛔ 只是个记录 —— 我们工程里没有那个 `AnimationClip` 资产，运行期解不出来（见 `BodyClipOf`）。</summary>
            public string clip;
            /// <summary>`delay`（秒）。</summary>
            public float d;
            /// <summary>`waitForCompletion` / `toggleFlyOff` / `safeResetOnEnd`（0/1）。</summary>
            public int wc, tf, sr;
        }
        [Serializable] public class RtFile
        {
            public int schema;
            public int count;
            public RtRow[] rows;
        }

        static Dictionary<string, CardAnimPlacement> _table;
        static bool _tried;
        /// <summary>表**载进来了**（false = 降级：见文件头那一段）。</summary>
        public static bool Loaded { get; private set; }
        /// <summary>表里几条（= `cardanim_clips.json` 的 `count`；载不到 ⇒ 0）。</summary>
        public static int TableCount { get { EnsureLoaded(); return _table.Count; } }
        /// <summary>自检用：因为**表里没有**而退回默认参数的次数（去重后的 key 数）。</summary>
        public static int MissingLookups { get { return _spoke.Count; } }

        static void EnsureLoaded()
        {
            if (_tried) return;
            _tried = true;
            _table = new Dictionary<string, CardAnimPlacement>(StringComparer.Ordinal);
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[CardAnimClips] `Resources/{ResourcePath}.json` **没载到** ⇒ 技能动画的"
                             + "位置/移动参数**全部退回默认**（sp=ep=pt=0 = 施法者自己、不位移）—— "
                             + "**显式降级**，玩家会看到「该飞向目标/落在别处的件贴在单位身上播完」。"
                             + "重跑：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_cardanim_clips.py");
                return;
            }
            RtFile dto;
            try { dto = JsonUtility.FromJson<RtFile>(asset.text); }
            catch (Exception e)
            {
                Debug.LogError($"[CardAnimClips] `Resources/{ResourcePath}.json` 解析失败：{e.Message} ⇒ 同上降级");
                return;
            }
            if (dto == null || dto.rows == null || dto.rows.Length == 0)
            {
                Debug.LogError($"[CardAnimClips] `Resources/{ResourcePath}.json` 是空的 / 形状不对 ⇒ 同上降级");
                return;
            }
            for (int i = 0; i < dto.rows.Length; i++)
            {
                var r = dto.rows[i];
                if (r == null || string.IsNullOrEmpty(r.a)) continue;
                _table[r.a] = new CardAnimPlacement
                {
                    StartPos = r.sp, EndPos = r.ep, PointTowards = r.pt,
                    Move = r.mv != 0, VfxDelay = r.vd, Ts = r.ts, Tm = r.tm, Te = r.te,
                    StartDelay = r.sd,
                    // 🆕 2026-10-23（`新账-83`）：「VFX movement」三列（判据见 `CardAnimPlacement` 那段）
                    Ease = r.ease, OrientToVelocity = r.otv != 0,
                    OffsetAo = r.ao != 0, OffsetOnt = r.ont != 0,
                    Ofx = r.ofx, Ofy = r.ofy, Ofz = r.ofz,
                    Mnx = r.mnx, Mxx = r.mxx, Mny = r.mny, Mxy = r.mxy, Mnz = r.mnz, Mxz = r.mxz,
                    // 🆕 2026-10-25（`AU1` · `J1` §9/§10）：卡本体位移 + 屏震（缺键 = 0/null = 改前行为）
                    MoveBody = r.mvt != 0, MoveBodyDelay = r.mvd, MoveBodyTime = r.mva,
                    MoveBodyEndVis = r.mvvis, MoveUseXZ = r.mvxz != 0, MoveUseY = r.mvy != 0,
                    MoveCurveX = r.mvcx, MoveCurveY = r.mvcy, MoveCurveZ = r.mvcz,
                    MoveOffX = r.mox, MoveOffY = r.moy, MoveOffZ = r.moz,
                    ShakePreset = r.shk, ShakeDelay = r.shkd, ShakeAmplitude = r.shka, ShakeFrequency = r.shkf,
                    ShakeOverAmplitude = r.shkoa, ShakeOverFrequency = r.shkof,
                    // 🆕 2026-10-25（`AU1` · `J3` §2）：三轴旋转（字段名与数据列逐字对应）
                    RotXOn = r.mmRxE != 0, RotXDeg = r.mmRxM, RotXCurve = r.mmRxC,
                    RotYOn = r.mmRyE != 0, RotYDeg = r.mmRyM, RotYCurve = r.mmRyC,
                    RotZOn = r.mmRzE != 0, RotZDeg = r.mmRzM, RotZCurve = r.mmRzC,
                    // 🆕 2026-10-27（`B9_W12USHK`）：unit shake + 卡体 clip（缺键 ⇒ null / false / 0）
                    UnitShakes = MapUnitShakes(r.ush),
                    BodyClipEnabled = r.macEn != 0, BodyClipDelay = r.macD, BodyClips = MapBodyClips(r.macs),
                };
            }
            Loaded = true;
            Debug.Log($"[CardAnimClips] 技能动画位置参数表已载入：{_table.Count} 行"
                    + $"（`Resources/{ResourcePath}.json`；键 = `CardAnim` 名 —— 同 prefab 名不同参数的 215 组各存各的）");
        }

        static readonly HashSet<string> _spoke = new HashSet<string>();
        const int SpeakCap = 16;
        static void SpeakMissing(string cardAnim)
        {
            if (_spoke.Count >= SpeakCap || !_spoke.Add(cardAnim ?? "")) return;
            Debug.LogWarning($"[CardAnimClips] 技能动画 `{cardAnim}` 的位置参数**表里没有** "
                           + "⇒ 退回「施法者自己 + 不位移」（⛔ 不拿别的拍的参数顶）。"
                           + (Loaded ? "（表载进来了，是这一条缺）" : "（**表整张没载到** —— 见上面的 error）"));
        }

        /// <summary>某条 `CardAnim` 的 `AnimInfo.GetAnimDuration()`（= `vd + ts + tm + te`）。
        /// 表里没有 ⇒ **-1**（调用方自己决定要不要出声 —— 这里不当第二个「缺件」口径）。
        /// <para>用途：`VfxMap.SharedRangedVfxDuration` 里那几条**同 prefab 名被多张 CardAnim 共用**的
        /// （现读 `BulletImpact_2shot_trail_chaos` 0.75 vs `_quick` 0.3 · `BulletImpact_1shot_trail`
        /// 0.3 / 0.75 / 0.21）—— 自检拿它证明我们取的是**正常那一张**（`Atk_…`），不是 `_quick`。</para></summary>
        public static float DurationOf(string cardAnim)
        {
            var p = PlacementOf(cardAnim);
            return p.HasValue ? (p.Value.VfxDelay + p.Value.Ts + p.Value.Tm + p.Value.Te) : -1f;
        }

        /// <summary>🆕 **2026-10-23（P1 · 演出时序 ④）：远程攻击那一件的「原版时长」
        /// = 逐件 `AnimInfo.GetAnimDuration()`（`vd + ts + tm + te`，**四字段无条件直加**）。</summary>
        ///
        /// <para>**判据（第一权威）** = `d:/2/tools/decomp_full/AnimInfo__GetAnimDuration.c`
        /// （四个字段直接相加、没有任何条件分支）；调用点
        /// `BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:111-124` 用 `WaitForSeconds(...)`
        /// 等它，**等完才** `ReceiveDamage` ⇒ **这一件演多久 = 伤害晚多久**。</para>
        ///
        /// <para>**为什么需要这一档**：`VfxMap.ChainDuration` 只覆盖两档 ——
        /// ① 逐卡 `CardDef.AnimTimes`（只收 `cardAbilities[].abilityLogic[].cardAnimations[]`）·
        /// ② 共享件表（`SharedRangedVfxDuration` 的 7 个 prefab 名）。
        /// 而逐卡远程件大多挂在卡 SO 的**顶层** `customRangedAttackParticles` 上、**不进 `AnimTimes`**
        /// ⇒ 现读 **441 个（卡,件）组合**两档都查不到、落回 `EventTiming.RangedVfxFallback` 0.75
        /// （其中 **265 个真值 ≠ 0.75**：186 早 / 79 晚，极端 **−2.85 s** —— Exorcist 真值 3.6
        /// vs 我们 0.75）。本条补的就是**第三档**：按 `ResolvedVfx.Anim`（CardAnim 名）查本表。
        /// 🔴 **⛔ 别按 prefab 名查** —— 现读 **215 组**同名不同参（见文件头）。</para>
        ///
        /// <para>**返回值契约（与 `VfxMap.ChainDuration` 不同，注意）**：
        /// **≥ 0 = 真值**（**`0` 也是真值** —— 现读 4 张 Tau 无人机共用的
        /// `Atk_BulletImpact_Tau_Pulse Drone Dual` 四字段**全是 0**，原版那一下就是 `WaitForSeconds(0)`）；
        /// **&lt; 0 = 判据为空**（那几件的 CardAnim 不在表里 / 有一件没有 CardAnim 名 ⇒ 调用方按老口径兜底并出声）。
        /// ⛔ 别把「0」当「查不到」用 —— 那 4 张卡会被无端推迟 0.75 s。</para>
        ///
        /// <para>⚠️ **不吃 `rng`**：与 `VfxMap.ChainDuration` 同一条口径（队尾是**纯函数**、只按「按序全播」
        /// 那条路算）。⛔ 别把生产的 `randomAbilityChoice` 随机源传进来 —— 那会**多抽一次**、
        /// 把后面真正要播的那一组抽偏。</para></summary>
        public static float RangedChainDuration(string evt, string faction, string cardIdOrName,
                                               IReadOnlyList<CardDef.CardAnimTime> times)
        {
            if (string.IsNullOrEmpty(evt)) return -1f;

            // ① 前两档（逐卡 `AnimTimes` → 共享件表）：走 `VfxMap` 自己的口，⛔ 别在这儿再写一份 join。
            float d = VfxMap.ChainDuration(evt, faction, cardIdOrName, times);
            if (d > 0f) return d;

            // ② 第三档：按 **CardAnim 名**查本表（`vd+ts+tm+te`）。
            //    终点 = `T`（组内起点偏移，单候选恒 0）+ 该件自己的时长 —— 与 `ChainDuration` 的
            //    `max(T + Duration)` 同形（原版队列一条一条等完才弹下一条）。
            var items = VfxMap.ResolveAllTimed(evt, faction, cardIdOrName, times);
            if (items == null || items.Length == 0) return -1f;
            float end = 0f;
            int known = 0;
            for (int i = 0; i < items.Length; i++)
            {
                float a = DurationOf(items[i].Anim);     // -1 = 表里没有那条 CardAnim
                if (a < 0f) return -1f;                  // 有一件判据为空 ⇒ 整条链的收尾也是空的，⛔ 别拿半条当真值
                known++;
                float e2 = Mathf.Max(0f, items[i].T) + a;
                if (e2 > end) end = e2;
            }
            if (known == 0) return -1f;
            ChainTruthHits++;
            return end;
        }

        /// <summary>🆕 2026-10-23（P1）：<see cref="RangedChainDuration"/> 的**第三档真的被用到**的次数
        /// （自检口 —— 用来证明「不是前两档顺手算出来的」，也证明它没退回常数）。</summary>
        public static int ChainTruthHits { get; private set; }

        /// <summary>某个 `CardAnim` 的位置参数（查不到 ⇒ `null` **并出声一次**）。</summary>
        public static CardAnimPlacement? PlacementOf(string cardAnim)
        {
            if (string.IsNullOrEmpty(cardAnim)) return null;
            EnsureLoaded();
            CardAnimPlacement p;
            if (_table.TryGetValue(cardAnim, out p)) return p;
            SpeakMissing(cardAnim);
            return null;
        }

        /// <summary>一张卡在某个桶里要播的那些拍（**按原版 `abilityLogic` 顺序**）。
        /// <paramref name="times"/> = 那张卡的 `CardDef.AnimTimes`（null = 没有逐候选时间）。
        /// <para>⚠️ **只查逐卡那一档**（`perCardOnly: true`）：这两桶原版没有通用件。</para>
        /// <para>⚠️ 没有候选 ⇒ **空数组**（调用方直接 for，别判 null）。</para></summary>
        public static CardAnimClip[] Resolve(string evt, string faction, string cardIdOrName,
                                             IReadOnlyList<CardDef.CardAnimTime> times)
        {
            if (string.IsNullOrEmpty(evt)) return NoClips;
            var items = VfxMap.ResolveAllTimed(evt, faction, cardIdOrName, times,
                                               null, perCardOnly: true);
            if (items.Length == 0) return NoClips;

            var buf = new CardAnimClip[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                var place = PlacementOf(it.Anim);
                // 🆕 2026-10-25（`AU1` · `新账 N4`）：**`startDelay` 折进起播偏移**。
                //   原版 `_ResolveAnim` state 0 先 `WaitForSeconds(AnimInfo.startDelay)`（`:67-75`）
                //   才进 `DelayedVFXPlay`（那里再等 `vfxDelayTime` = 已含在 `it.T` 里）
                //   ⇒ 起播 = `startDelay + 组内偏移 + vfxDelayTime`。查不到参数 ⇒ 0（= 改前行为）。
                float sd = place.HasValue ? Mathf.Max(0f, place.Value.StartDelay) : 0f;
                buf[i] = new CardAnimClip
                {
                    Anim = it.Anim,
                    Prefab = it.Name,
                    T = it.T + sd,
                    Duration = it.Duration,
                    // 查不到 ⇒ 出声（PlacementOf 里）后按「自己 + 不移动」（显式降级，见文件头）
                    Place = place ?? new CardAnimPlacement(),
                };
            }
            return buf;
        }

        static readonly CardAnimClip[] NoClips = new CardAnimClip[0];

        // ===================== 卡体 `AnimationClip`（原版 `DoAnim` 的第一个参数）=====================
        //  🔴 **本 build 恒为 null —— 这是数据事实，不是没写**。判据（2026-10-23 现读）：
        //     `assets_full` 里含 `minionAnimationClipInfo` 的资产 **1,099** 个，`enable` 为 1 的 **0 个**；
        //     `minionClips` 非空的只有 2 个（`Atk_Gauss_Tesla` / `…_Annihilator`），而那两条的
        //     `animationClip` 都是 **null 引用**且自身 `enable` 也是 0。
        //     原版 `BattleManager._ResolveAnim_d__466__MoveNext.c:166-171` 那条分支是
        //     「`enable == 0` ⇒ **不调** `ResolveAnimAnimationClip`」⇒ 这份数据上原版的卡体 clip
        //     **一次都不会播**（铁律 11 例外①：判据是空的）。
        //  ⚠️ **⛔ 不许拿 `WarpforgeVFX/Animations/*.anim` 顶**：那 94 条全是**特效 prefab 内部**的
        //     曲线（`Vanguard Frame Animation` 绑的是 `material._EmissionColor` / `m_IsActive`），
        //     挂到卡体上**什么都不会动**，那是静默错（比不播更坏）。
        //  ⇒ `Card3DAnimationController` 的 `AnimationClip` 那一路**完整实现、可用**
        //     （自检用 `BuildProbeClip` 证明），生产路径走 `CardAnimPlayback.Prefab` 那一路。
        //  🆕 2026-10-27（`B9_W12USHK`）：这一族**数据列已经有了**（`RtRow.macEn/macD/macs` ← 生成器
        //     `_minion_clip_block`，现读 **2 行**：`macEn` 全 0、`macs[].clip` 全 `"0"`=null）⇒
        //     上面那条结论**由数据本身证明**（不是「我们没写」）。读完仍返回 null 的原因见下一条：
        //     我们工程里**没有**那些 `AnimationClip` 资产，「PathID → 资产」这一跳解不出来。
        static bool _spokeNoBodyClip;
        /// <summary>这张卡有没有卡体 `AnimationClip`。**今天恒 `null`**（原因见上面那一大段）。</summary>
        public static AnimationClip BodyClipOf(string cardIdOrName)
        {
            if (!_spokeNoBodyClip)
            {
                _spokeNoBodyClip = true;
                Debug.Log("[CardAnimClips] 卡体 `AnimationClip` 这一档**本 build 没有数据**"
                        + "（1,099 个含 `minionAnimationClipInfo` 的资产里 `enable` 全为 0，"
                        + "唯二声明了 `minionClips` 的那两条 `animationClip` 也是 null；判据见本文件那一节）"
                        + " ⇒ `DoAnim` 走「技能动画 prefab + 时长」那一路");
            }
            return null;
        }

        // ============== 🆕 2026-10-27（`B9_W12USHK`）：`unitsShake` 的读数口 + 两个 DTO 的映射 ==============
        //  🔴 **为什么新开一节（而不是把这些塞进上面那些方法之间）**：本文件被账按行号引用
        //     （`账/特效动画.tsv` 的 `CardAnimClips.cs:329`），新成员一律**追加在类尾**（CLAUDE.md 那条纪律）。

        /// <summary>这一拍要**逐个**处理的 unit shake 列表（原版 `unitsShake` 的顺序）。
        /// <para>**返回值契约**：`null` = 这一拍没有（原版绝大多数件，也含「表里没有这条 CardAnim」——
        /// 那种情况 `PlacementOf` 已经出声）；长度 ≥1 = **要逐条**交给
        /// `CardFeel.ResolveUnitShake`（⛔ 别只取第一条，也⛔ 别在这里先按 `TargetUnit` 过滤 ——
        /// 「解不出目标就空转」是**原版行为**，由 `ResolveUnitShake` 的调用方按 `Effective` 如实处理）。</para></summary>
        public static CardAnimUnitShake[] UnitShakesOf(string cardAnim)
        {
            var p = PlacementOf(cardAnim);
            return p.HasValue ? p.Value.UnitShakes : null;
        }

        /// <summary>`Resources/cardanim_clips.json` 的 `ush[]` → 运行期 `CardAnimUnitShake[]`。
        /// 空 / `null` ⇒ `null`（= 没有这一族，⛔ 不造空数组，免得消费方把「有 0 条」当「有这一类但 0 条」）。</summary>
        static CardAnimUnitShake[] MapUnitShakes(RtUnitShake[] src)
        {
            if (src == null || src.Length == 0) return null;
            var buf = new CardAnimUnitShake[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var s = src[i];
                if (s == null) continue;          // 理论上不会有；有也不炸（该条留默认 = TargetUnit 0 = 空转）
                buf[i] = new CardAnimUnitShake
                {
                    TargetUnit = s.t, AwayFromUnit = s.aw, ShakeSize = s.sz,
                    ShakeDuration = s.du, ShakeDelay = s.dl,
                };
            }
            return buf;
        }

        /// <summary>`macs[]` → 运行期 `CardAnimBodyClip[]`（`null` = 没有）。只搬，不改语义。</summary>
        static CardAnimBodyClip[] MapBodyClips(RtMinionClip[] src)
        {
            if (src == null || src.Length == 0) return null;
            var buf = new CardAnimBodyClip[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var s = src[i];
                if (s == null) continue;
                buf[i] = new CardAnimBodyClip
                {
                    TargetUnit = s.t, ClipPathId = s.clip, Delay = s.d, WaitForCompletion = s.wc != 0,
                    ToggleFlyOff = s.tf != 0, SafeResetOnEnd = s.sr != 0,
                };
            }
            return buf;
        }

        /// <summary>`minionAnimationClipInfo.minionClips[]` 的记录（`null` = 没有这一族）。
        /// <para>🔴 **今天全池只有 2 行，且两行的 `BodyClipEnabled` 都是 `false`、每条 `ClipPathId` 都是 `"0"`（null）**
        /// ⇒ 调用方即便拿到也**不该播**（原版那道闸是 `enable`，`_d__466:166-171`）。
        /// 这个口存在的意义是「判据留在表里，将来数据变了不用改代码」——
        /// ⛔ 别为了让它有用途去拿 `WarpforgeVFX/Animations/*.anim` 顶（理由见 `BodyClipOf` 上面那一大段）。</para></summary>
        public static CardAnimBodyClip[] BodyClipsOf(string cardAnim)
        {
            var p = PlacementOf(cardAnim);
            return p.HasValue ? p.Value.BodyClips : null;
        }
    }

    /// <summary>`AnimInfo.unitsShake` 的一条（原版 `UnitShake.UnitShakeInfo`，`dump.cs:25498-25511`；
    /// 运行期那一份 `UnitShake` 由 `UnitShake..ctor(info, actingCard, targetCard)` 把两个枚举解成卡，
    /// 判据 `decomp_full/UnitShake__.ctor.c:8-18`）。
    /// <para>⚠️ 消费口 `CardFeel.ResolveUnitShake` 收的是**已经解出来的两张卡**，所以「枚举 → 卡」这一跳
    /// （= `BattleManagerSupport.GetTargetUnit`：0/其它 ⇒ null · 10 ⇒ acting · 20/30 ⇒ target ·
    /// 40 ⇒ 同阵营督军 · 50 ⇒ 敌方督军）**必须由调用方做** —— 给 `BattleManagerSupport__GetTargetUnit.c` 那一份判据就知道该传谁。</para></summary>
    public struct CardAnimUnitShake
    {
        /// <summary>`TargetUnit` 枚举原值（0 undefined · 10 self · 20 cardTarget · 30 abilityTarget ·
        /// 40 friendlyHero · 50 enemyHero）。</summary>
        public int TargetUnit;
        /// <summary>`TargetUnit` 枚举原值 —— 非 0 且解出的卡 ≠ `TargetUnit` 解出的那张 ⇒ 走**击退支**。</summary>
        public int AwayFromUnit;
        /// <summary>`UnitSize` 枚举原值（0 small · 2 medium · 3 big）。⚠️ `CardFeel.ShakeScaleOfUnitSize`
        /// 就是把它换成 `DoUnitShake` 的 2/4/8 —— ⛔ 别在这里先换（那是抖动支才用的，击退支吃的是原值
        /// `GetPushBackFactor(size)`）。</summary>
        public int ShakeSize;
        /// <summary>抖动支的时长（秒）。击退支不用它（`DoPushBack` 的 duration 原版写死 0.4）。</summary>
        public float ShakeDuration;
        /// <summary>先等这么久（秒）再判 `IsInPlay` 与二选一。</summary>
        public float ShakeDelay;

        /// <summary>`TargetUnit != 0` —— 只有它为真这条才可能做出动作（0 ⇒ `GetTargetUnit` 返 null ⇒ 整条空转）。
        /// ⛔ 这只是「目标解得出」的必要条件，**不是**「一定抖」：`IsInPlay` 不过照样不播。</summary>
        public bool Effective { get { return TargetUnit != 0; } }
    }

    /// <summary>`AnimInfo.minionAnimationClipInfo.minionClips[]` 的一条
    /// （原版 `MinionAnimClipInfo.MinionAnimClipInfoDefinition`，`dump.cs:25259-25305`）。
    /// <para>🔴 **今天这条链跑不到**：`enable` 全池为 0 ⇒ 原版 `_d__466:166-171` 根本不进这一支；
    /// 唯二声明了 clips 的两条 `animationClip` 又都是 null。本结构只为**把判据留住**（数据已由生成器落表），
    /// ⛔ 别为了「让字段有用途」去凭空造一个 `AnimationClip`。</para></summary>
    public struct CardAnimBodyClip
    {
        /// <summary>`target`（`TargetUnit` 枚举）。</summary>
        public int TargetUnit;
        /// <summary>`animationClip.m_PathID` 的十进制字符串（`"0"` = null 引用）。</summary>
        public string ClipPathId;
        /// <summary>这条 clip 起播前等多久（秒）。</summary>
        public float Delay;
        /// <summary>等这条 clip 播完再继续。</summary>
        public bool WaitForCompletion;
        /// <summary>`toggleFlyOff` —— 播期间把「飞出去」的状态切一下。</summary>
        public bool ToggleFlyOff;
        /// <summary>`safeResetOnEnd` —— 收尾把对象上所有动画复位。</summary>
        public bool SafeResetOnEnd;
    }
}
