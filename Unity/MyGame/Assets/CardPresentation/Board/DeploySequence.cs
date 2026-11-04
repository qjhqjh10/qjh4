// DeploySequence.cs — 「手牌落到战场」的那条动画（**原版是五段**：飞 → 等 0.1 → 换 3D 体 → 悬停 → 落下 → 落地尾拍 0.1）
//
// ---- 原版是什么（判据全部本机可核）--------------------------------------
//  ① `MinionManager.PlayMinionFromHand`：把手牌那张卡的 UI 位姿**换算进棋盘空间**、挂到
//     `MinionManager` 底下（`SetCardPosAndScaleToBoard` + `set_parent`）
//     —— `MinionManager__PlayMinionFromHand.c:8-20`。
//  ② `DOLocalMove` 飞到 **槽位 + up × `minionConversionHeight`**（**我方 1.0 / 敌 2.1**，
//     竞技场单位），时长 **0.30 s**；同一条 tween 上 `DOLocalRotate(zero, 0.30)`
//     —— `MinionManager__MoveMinionToConversionPoint.c:41-47,74-96`
//     · 高度：`MonoBehaviour_4372.json`（我，`MinionArea` z=−6.655）= **1.0** ·
//       `MonoBehaviour_4373.json`（敌，z=+1.043）= **2.1**（都是 `minionConversionHeight` 字段）
//     · 时长：同一个实例的 `minionToConversionPointTime` = **0.3**
//     ⚠️ 目标里的 `ExtraYOnBoard` 那一项两个实例**都是 0**（同一份 JSON），所以高度就是它自己。
//  ③ **垂直落下**：`CardScript.UpdateMinionInPlayPosition(slotPos, timeToLand, tween:true)`
//     —— 就是一句 `DOLocalMove`（`CardScript__UpdateMinionInPlayPosition.c:83-100`），
//     时长 **0.2 s** = 卡预制体 `MonoBehaviour_1744609728290659264.json` 的 `timeToLand`。
//     它前面还夹了一段等待 `timeBeforeLand` = **0.05 s**
//     （`CardScript._MinionPlayedIntoField_d__314__MoveNext.c` 的 state 1 末尾 `WaitForSeconds(+0x154)`；
//      `+0x154` = `CardScript.timeBeforeLand`，字段名见 `d:/2/tools/il2cpp_out/dump.cs:35382`）。
//  ④ 落地反馈（扬尘 + 震镜头 + 落地音）在**落下跑完的那一刻**才起
//     （同上 d__314 的 state 3 `CardScript__LandOnGround`）—— ⚠️ 不是「落地前 0.05 s」：
//     `timeBeforeLand` 那一段在 **state 1**（= 下落**开始之前**的悬停），
//     与「落点反馈」差着整个下落。判据与推演见 `资料/普查产出_1016战斗/W3_出牌三段与落地反馈.md`。
//
// ---- 哪些是原版、哪些是我们的 -------------------------------------------
//  · `MoveTime 0.3` / `HoldTime 0.05` / `LandTime 0.2`　→ **原版字段**（上面三个）
//  · 「先飞到槽位正上方、再垂直落下」　→ **原版形状**（我们 2026-10-16 之前是「抬一点再对角滑过去」，
//    那两句常量 `LiftHeight 0.35` / `LiftFraction 0.35` 是**我们挑的**，已删除）
//  · 旋转补间跨度 = `MoveTime`　→ 原版（同一条 `DOLocalRotate` 吃 `minionToConversionPointTime`）
//  · ✅ 缩放 = **瞬时赋值**（2026-10-22 `W64/B07` 改，原来是我们自己加的 0.3 s 补间）：
//    原版在起飞**之前**就把 `localScale` 设成 `GetUnitSizeInPlay`，**没有补间**
//    （`decomp_full/MinionManager__MoveMinionToConversionPoint.c:74-83`）⇒ 现在照它，`Play` /
//    `PlayQuad` 两处在建序列**之前**直接 `localScale = Vector3.one × slotScale`。
//  · 缓动 `OutQuad`　→ 原版那两条 `DOLocalMove` **没有 `SetEase`** ⇒ 吃 DOTween 的
//    `defaultEaseType` = `OutQuad`（本工程 `Assets/Resources/DOTweenSettings.asset` 实读 `defaultEaseType: 6`）
//
// ---- 时长们为什么是这几个数（一次说清，别再拆开抄）----------------------
//  🆕 2026-10-19（`W57` · `T05(c)(d)`）：原版是**五段**（前四段中间还夹着一次「换成 3D 体」的交接；第五段「落地尾拍」2026-10-27 由 `E3CMP` 补）；
//    🔴 2026-11-02（`B117-R15`）补回**漏掉的 0.55 s**（`GetHandToBoardAnimEventTime`）⇒ 五段共 1.3 s：
//    0 → 0.3      飞向「槽位 + up × `minionConversionHeight`」
//    0.3 → 0.95   🔴 `WaitForSeconds(GetMinionConversionTime = 0.3 + 0.55)` 再加 `timeToScaleIntoMoving` 0.1
//                 · 0.55 = clip `Card Hand To Board` 的 `m_Events[0].time`（`MinionManager__GetMinionConversionTime.c` · `CardScript__GetHandToBoardAnimEventTime.c`）
//                 · 0.1 = `_MinionPlayedIntoField_d__314__MoveNext.c:60-79`（`timeToScaleIntoMoving`）
//    **0.95**     🔴 **交接**：`SetUnitInPlayLayer()`（手牌卡面 → 场上 3D 体）+ `DefaultScale` + `HideCardShadow`
//                 （d__314 `:87` / `:108` / `:121`）—— 从这里起是**竞技场空间**、由透视相机画
//    0.95 → 1.0   `WaitForSeconds(timeBeforeLand = 0.05)`
//    1.0 → 1.2    `UpdateMinionInPlayPosition`（一句 `DOLocalMove`，**竞技场局部坐标**）
//    1.2          **落地反馈**（`LandOnGround`）
//    🆕 1.2 → 1.3  🔴 **落地后的尾拍** `WaitForSeconds(AttackStepTime = 0.1)`（d__314 **state 3**，`:186-205`）
//  ⇒ 总长 = 0.3 + **0.55** + **0.1** + 0.05 + 0.2 + **0.1** = **1.3 s**（`TotalTime`）。
//    🔴 2026-10-27（`E3CMP` · 账 804 ④）尾拍 0.1 s；🔴 2026-11-02（`B117-R15`）交接前那 0.55 s —— 漏了它 ⇒ 卡在 0.65 s 就落完并让引擎结算，而 `Board Elements` 要到绝对 1.0→1.2167 才浮回 0 ⇒ **落地之后还要再浮 0.35 s**。
//  `onDone` 挂在**序列末尾**：
//  原版「真正出牌」的那一半（`CardScript.ResolveCardPlayed` + 广播 + 墓地记账）在
//  `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c` 里是**等 `_MinionPlayedIntoField`
//  整条协程跑完**才做的（state 6 → yield → state 7 → `LAB_1809bd825`）⇒ **后演后出**，
//  不是「先出后演」。⚠️ 代价（如实记）：我们的引擎事件只有一条 `Deploy` ⇒ 登场那两件
//  （`Invoke Minion Card Fade Default` + 阵营召唤法阵）也跟着挪到了落地那一拍。🆕 **2026-10-26（`B9_E3P13`）**：账 `CardScript.MinionPlayedIntoField` 的 ① `orderInPlay`（原版 `EntityScript.orderInPlay` = 召唤序号，唯一消费者是 `MinionManager.GetMinionList()` 的 `OrderBy`）在本仓的等价物 = **引擎 `UnitState.PlayOrder`**（`BattleContext.TakePlayOrder()` 在部署那一刻发号；排序消费点 `RuleCore.StableSortByPlayOrder` / `EffectResolver` 那一族）⇒ **视图侧不另存一份**（我们的棋盘次序由槽位定）——⛔ 别为它加 `CardView.inPlayOrder` 那种没人读的字段（原版有、我们无消费者 = 死数据）。
//  ⚠️ **交接动作本身不在这里拼**：由 `BattleDriver.OnDeployHandoff` 一处给出（`Handoff.Apply`），
//     本类只负责「在 0.95 s 调它、然后让 3D 体在竞技场空间里落下 0.2 s」（铁律 6：判据只留一处）。
//
// ---- 权威实例（两个 `MinionManager` 的定性，别再翻案）-------------------
//  `MonoBehaviour_4372/4373.json` **不是「新的/旧的」，是 玩家 / 敌方 两份**
//  （`MinionArea` 的两个实例 local z 一个 −6.655（玩家、近）一个 +1.043（敌、远），
//   `BattleManager.GetUnitSizeInPlay` 按 `EntityScript.isPlayer` 选）。
//  敌行更远 ⇒ **缩放更大才对**：0.69 vs 0.36、步距 1.53 vs 0.82；屏幕上自洽
//  （玩家步距 0.82 × 182.14 = 149.3 px、敌 1.53 × 86.2 = 131.9 px）。
//  ⇒ 与 `CardsHorizontalLayout` MB 5271/4053（我方手牌/敌方手牌）是**同一个误判**，逐值见 `Board/ArenaSlots.cs`。
//  ⚠️ 原文那句「槽位数组只有 3 个」也一并作废：`FillMinionPositions` 在 `Awake` 里按
//  `slotsPerSide` 重填（`:41-45`），序列化数组只是个快照。
using System;
using DG.Tweening;
using UnityEngine;

namespace CardPresentation
{
    public static class DeploySequence
    {
        /// <summary>移动段（②）时长（秒）—— 原版 `MinionManager.minionToConversionPointTime = 0.3`
        /// （出处实例：`MonoBehaviour_4372.json` / `_4373.json`，两个实例同值）。</summary>
        public const float MoveTime = 0.3f;

        /// <summary>到顶之后、下落之前的那一段悬停（秒）—— 原版卡预制体 `CardScript.timeBeforeLand = 0.05`
        /// （`MonoBehaviour_1744609728290659264.json`；消费点见文件头 ③ 的 d__314 state 1）。</summary>
        public const float HoldTime = 0.05f;

        /// <summary>垂直落下（③）时长（秒）—— 原版卡预制体 `CardScript.timeToLand = 0.2`
        /// （同一份 JSON；消费点 `CardScript__UpdateMinionInPlayPosition.c` 的那个 `DOLocalMove`）。</summary>
        public const float LandTime = 0.2f;

        /// <summary>🆕 2026-10-27（`E3CMP` · 账 804 ④）：**落地之后、整条协程结束之前的那一拍等待**（秒）。
        /// <para>判据 🥇 `decomp_full/CardScript._MinionPlayedIntoField_d__314__MoveNext.c:186-205`
        /// （state 3）：`LandOnGround()` 之后 `yield WaitForSeconds(CardScript.get_AttackStepTime())`
        /// —— **不是**我们原来那条「落地即结束」。（`:199` 就是那一句。）
        /// 值 = `CardFeel.AttackStepTime`（**0.1**，卡预制体 `attackStepTime`；
        /// ⛔ 别在这里再写一个 0.1 的字面量 —— 判据只留一处）。</para>
        /// <para>🔴 **为什么要它**：`BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c` 是
        /// **等整条 `MinionPlayedIntoField` 协程跑完**才 `ResolveCardPlayed` ⇒ 原版的「真出牌」在
        /// 0.3+0.1+0.05+0.2+**0.1** = **0.75 s**，而我们原来在 0.65 s 就把 `onDone` 交出去了
        /// ⇒ **引擎比画面早 0.1 s 结算**（静默，只有对着原版掐表才看得出）。</para></summary>
        public const float LandTailTime = CardFeel.AttackStepTime;

        /// <summary>🆕 2026-10-19（`W57` · `T05(d)`）：飞完到「换成 3D 体」之间那段**纯等待**（秒）
        /// —— 原版 `CardScript._MinionPlayedIntoField_d__314__MoveNext.c` state 0 末尾的
        /// `WaitForSeconds(timeToScaleIntoMoving)`（`:60-79`，`:75` 那一句）；字段值 = **0.1**
        /// （`VarsGlobal.timeToScaleIntoMoving`，`资料/VarsGlobal_原版数值.md` §一）。
        /// <para>⚠️ **同一个字段还有一个用法**：`CardScript__ShowCardInCenter.c` 里它是**位移补间时长**
        /// （autoPlay 那条路，已落地）—— 那次是补间、这里是**纯等待**，⛔ 别拿那处当成这一处已做。</para></summary>
        public const float TimeToScaleIntoMoving = 0.1f;

        /// <summary>交接时刻（秒）= 飞行 0.3 + `GetHandToBoardAnimEventTime` 0.55 + 等待 0.1 = **0.95**：手牌卡面换成场上 3D 体、切进竞技场空间。
        /// 判据 → `MinionManager__GetMinionConversionTime.c`（`minionToConversionPointTime + GetHandToBoardAnimEventTime`）＋ 文件头（`_MoveNext.c` state 1 开头）。</summary>
        public const float HandoffTime = MoveTime + CardFeel.HandToBoardDone + TimeToScaleIntoMoving;

        /// <summary>整条序列的真实长度 = 五段之和（秒）= **1.3**
        /// （飞 0.3 + 交接前等 0.65 + 悬停 0.05 + 落下 0.2 + **落地后尾拍 0.1**）。
        /// ⚠️ **别在别处再算一遍** —— `Editor/BattleScene.cs` 的断言、
        /// `CardInteraction.Release` 报给驱动层的闸宽都引用它。
        /// <para>🔴 2026-10-27（`E3CMP` · 账 804 ④）：尾拍 0.1 s = 原版 state 3 末尾那一次 `WaitForSeconds(AttackStepTime)`。
        /// 🔴 2026-11-02（`B117-R15`）：交接前那 0.65 = `GetHandToBoardAnimEventTime` 0.55 + `timeToScaleIntoMoving` 0.1
        /// —— 原来只等了 0.1 ⇒ 交接 0.4 / 总长 0.75，比原版**早 0.55 s**（卡落地后 `Board Elements` 还要再浮 0.35 s）。
        /// ⚠️ `Editor/BattleScene.cs` 那条断言比的是「`Dur(Play(...))` == `TotalTime`」（两侧同源）⇒ 数值自动跟上；
        /// 另几条比的是**原版关系式**（交接 = 飞 + clip 事件 + 等 · 落下起点 = 飞 + 形态切换），改了本值它们会红。</para></summary>
        public const float TotalTime = HandoffTime + HoldTime + LandTime + LandTailTime;

        /// <summary>🆕 2026-10-19（`W57` · `T05(c)`）：**0.95 s 那一刻的交接** ——
        /// 「手牌卡面」换成「场上 3D 体」并把卡摆到竞技场空间的「槽位正上方」，
        /// 之后最后 0.2 s 由 <see cref="FallTarget"/> 在**竞技场局部坐标**里落下。
        ///
        /// <para>🔴 **判据只留一处**：<see cref="Apply"/> 由 `BattleDriver.OnDeployHandoff` 一处给出
        /// （`SetFace(Board)` + `SetParent(boardRoot)` + `SetLayer(ArenaLayer)` + `SetPose(aboveLocal, …)`），
        /// ⛔ 别在本类里再拼一次（本类不知道棋盘、相机、层）。
        /// <see cref="FallTarget"/> 与玩家那条的卡本体不同：**AI 手牌卡背**那一版交接后换的是
        /// **场上视图**（卡背在 <see cref="Apply"/> 里藏起来），所以落体要单独给。</para>
        /// <para>`default(Handoff)`（`Apply == null`）= **不做交接** ⇒ 退回旧的三段（HUD 平面直落），
        /// 那条只给「没有驱动层」的自检 / 纯时长探针用。</para></summary>
        public struct Handoff
        {
            /// <summary>0.95 s 那一刻执行一次（幂等；⛔ 不要在里面开局新的补间）。</summary>
            public Action Apply;
            /// <summary>1.0 → 1.2 s 做 `DOLocalMove` 的那个 transform（玩家 = 卡本体；AI = 场上视图）。</summary>
            public Transform FallTarget;
            /// <summary>竞技场局部坐标（`boardRoot` 的局部系）里的落点 = 落地后 `PoseFor` 那个位姿。</summary>
            public Vector3 LandLocal;
            /// <summary>@return 交接是不是齐了（`Apply` 与 `FallTarget` 都有）。</summary>
            public bool IsValid { get { return Apply != null && FallTarget != null; } }
        }

        /// <summary>🆕 2026-10-19（偏差 5）：**非单位卡**那条「立即完成」序列的长度（秒）。
        /// 不能真的写成 0：DOTween 的零长度 `Sequence` 会**当场自动 kill**，`OnComplete` 不一定派得到
        /// ⇒ 用一个 1 ms 的 interval 把它推到「下一次 update」完成（真机 = 下一帧；批处理 = 下一次
        /// `CardTween.Advance`）。</summary>
        public const float InlineCompleteEpsilon = 0.001f;

        /// <summary>这张卡走不走「立即完成」那一条（= **非单位卡**：战术 / 防御卡）。
        /// 🔴 **判据只此一处** —— `Play` 用它分支、`DurationFor` 用它定时长、`CardInteraction.Release`
        /// 用它报给驱动层的「这段飞行有多长」（残余①的闸）。⛔ 别在别处再写一遍 `!card.Data.isUnit`。
        /// </summary>
        public static bool IsInline(CardView card)
        {
            return card != null && !card.Data.isUnit;
        }

        /// <summary>这张卡这一次落位**整条序列的真实长度**（秒）—— 单位卡 = <see cref="TotalTime"/>（1.3），
        /// 非单位卡 = <see cref="InlineCompleteEpsilon"/>。与 <see cref="Play"/> 的分支**同源**
        /// （都问 `IsInline`），⛔ 别写第二份「战术卡 0.001 / 单位卡 1.3」的字面量。</summary>
        public static float DurationFor(CardView card)
        {
            return IsInline(card) ? InlineCompleteEpsilon : TotalTime;
        }

        /// <summary>🆕 2026-10-19（战斗线 B3 · 偏差 8 的载体那一半）：**敌方手牌那张卡背**（`ImageQuad`）
        /// 走**同一条五段**（0.3 飞向格位上方 → **0.65 等** → 0.95 交接 → 0.05 悬停 → 0.2 竞技场空间落下 → **0.1 落地尾拍**）。
        ///
        /// <para>判据（同一份）：原版 `MinionManager.PlayMinionFromHand` / `MoveMinionToConversionPoint` /
        /// `CardScript.UpdateMinionInPlayPosition` 搬的**就是手牌那张卡对象本体**
        /// （`CardScript._MinionPlayedIntoField_d__314__MoveNext.c` 全程在同一个 `CardScript` 上动
        /// `transform`）；敌方手牌露出来的是**卡背节点**（`Core/CardbackFace.cs` 的两段式）
        /// ⇒ 飞的就该是那张**卡背**，而不是 `SyncBoard` 刚建出来的「场上样式」视图。
        /// ⚠️ 时长与缓动**与 <see cref="Play"/> 共用同一组常量/同一条曲线**（⛔ 别在这里另写一套数）。</para>
        ///
        /// <para>与 <see cref="Play"/> 的差别只有两处（都是「载体不同」带来的）：
        /// · **起点就是它现在的手牌位姿**（不另设 `SetPose`）；
        /// · 缩放从**当前**（手牌卡背恒 1）补到 <paramref name="endScale"/> —— 那个数由调用方按
        ///   「场上那张卡的宽度 ÷ 卡背画出来的宽度」换算，判据在 `BattleDriver.StartAiDeployFlight`。</para>
        /// <para>`onDone` 同样挂在**序列末尾**（= 落地**再等完尾拍**，1.3 s）。
        /// 🆕 交接（0.95 s）那一版同 <see cref="Play"/>：<paramref name="handoff"/> 由调用方给，
        /// ⚠️ **交接之后落体换成了场上视图**（卡背在 `Apply` 里藏起来、落地拍再销毁）⇒
        /// <see cref="Handoff.FallTarget"/> 必须是那张场上视图的 transform。</para></summary>
        public static Tween PlayQuad(Transform tr, Vector3 abovePos, Vector3 landPos,
                                     float endScale, Action onDone = null)
        {
            return PlayQuad(tr, abovePos, landPos, endScale, default(Handoff), onDone);
        }

        /// <summary>同 <see cref="PlayQuad(Transform, Vector3, Vector3, float, Action)"/>，带 0.95 s 交接。</summary>
        public static Tween PlayQuad(Transform tr, Vector3 abovePos, Vector3 landPos,
                                     float endScale, Handoff handoff, Action onDone = null)
        {
            if (tr == null) return null;
            // 🔴 `B07`：**起飞之前**就把缩放设成落位尺寸（原版 `MoveMinionToConversionPoint.c:74-83`
            //   是**赋值**、没有补间）。⛔ 别加回 `DOScale`。
            tr.localScale = Vector3.one * Mathf.Max(1e-4f, endScale);
            var seq = DOTween.Sequence();
            seq.Append(LocalMoveToWorld(tr, abovePos, MoveTime));
            // 🆕 (d) + 🔴 2026-11-02（`B117-R15`）：这里等的是原版 `GetMinionConversionTime`（飞 0.3 + clip 事件 0.55）**再加** `timeToScaleIntoMoving` 0.1 ⇒ 交接落在 0.95 s
            seq.AppendInterval(TimeToScaleIntoMoving + CardFeel.HandToBoardDone);
            if (handoff.IsValid)
            {
                // 0.95 s：换成 3D 体（由驱动层一处给出），然后落体在竞技场空间里落下
                seq.InsertCallback(HandoffTime, () => handoff.Apply());
                seq.AppendInterval(HoldTime);
                seq.Append(handoff.FallTarget.DOLocalMove(handoff.LandLocal, LandTime).SetEase(Ease.OutQuad));
            }
            else
            {
                // 没有交接 ⇒ 旧行为（HUD 平面直落；只给「没有驱动层」的探针用）
                seq.AppendInterval(HoldTime);
                seq.Append(LocalMoveToWorld(tr, landPos, LandTime));
            }
            // 同 <see cref="Play"/>：旋转用 `Insert(0f, …)`（⛔ 不是 `Join`，那会从 0.3 s 才起算）
            seq.Insert(0f, tr.DOLocalRotate(Vector3.zero, MoveTime).SetEase(RotateEase));
            // 🆕 2026-10-27（`E3CMP` · 账 804 ④）：落地后的尾拍 —— 原版 d__314 **state 3** 的
            //   `LandOnGround()` → `WaitForSeconds(AttackStepTime)`（`.c:186-205`）。`onDone` 因此落在 1.3 s。
            seq.AppendInterval(LandTailTime);
            CardTween.Use(seq, Ease.Linear, tr);
            if (onDone != null) seq.OnComplete(() => onDone());
            return seq;
        }

        /// <summary>落位：从手牌姿态**飞到槽位正上方**，再**垂直落下**。
        /// <para>`abovePos` = 「那一格的卡**抬高 `minionConversionHeight`** 之后」在**世界坐标**里的点（`DropTargetWorld*` 的返回值；本类内部换算成父级局部系再走 `DOLocalMove`）
        /// （3D 战场下 = `BoardLayout.DropTargetWorldAtHeight(slot, which.MinionConversionHeight)`；
        ///  非 3D 时它就是 `landPos` ⇒ 退化成一记直飞）。</para>
        /// <para>`landPos` = 「那一格的卡」本来的补间终点（3D 下 = `BoardLayout.DropTargetWorld(slot)`）。</para>
        /// <para>`onDone` 在**序列末尾**（= 落地那一拍）触发 —— 理由见文件头最后一段（**不是**在 0.3 s 处）。</para>
        /// </summary>
        public static Tween Play(CardView card, Vector3 abovePos, Vector3 landPos, float slotScale,
                                Action onDone = null)
        {
            return Play(card, abovePos, landPos, slotScale, default(Handoff), onDone);
        }

        /// <summary>同 <see cref="Play(CardView, Vector3, Vector3, float, Action)"/>，
        /// 🆕 带 0.95 s 的**交接**（`T05(c)(d)`）：0.3 飞 → 0.65 等 → **0.95 `handoff.Apply()` 换成 3D 体** →
        /// 0.05 悬停 → 0.2 `DOLocalMove`（竞技场局部坐标）→ **0.1 落地尾拍** → 1.3 `onDone`。
        /// <para>⚠️ `handoff` 传 `default` 时退回旧的三段形状（HUD 平面直落、总长仍是 1.3 —— 那 0.65 的等待
        /// 与 0.1 的尾拍对单位卡恒定存在）—— 那条只给「没有驱动层」的纯时长探针 / `CardBaseDemo` 用。</para>
        /// </summary>
        public static Tween Play(CardView card, Vector3 abovePos, Vector3 landPos, float slotScale,
                                Handoff handoff, Action onDone = null)
        {
            // 🆕 2026-10-19（战斗线 B1 · 偏差 5「战术卡多一段飞向格位」）：
            //   **非单位卡（战术 / 防御）不走这一段飞行。**
            //   判据（`d:/2/tools/decomp_full/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c`
            //   逐句读）：那一支是 `CardScript.NonTargetSpellPlayed`（`:633` / 另两处 `:1263,:1533`）
            //   + `CardScript.DissolveWholeCard`（`:638`，时长 `VarsGlobal.timeToDissolveCard = 0.2`）
            //   —— **没有** `MinionPlayedIntoField`、没有「飞向格位」那三段。
            //   ⇒ 非单位卡：**不飞、不缩放**，这一帧就把 `onDone` 交出去（引擎尽快结算；
            //      「溶解」那一半由 `BattleDriver.DoPlay` 的战术卡分支补）。
            //   ⚠️ **`onDone` 仍走补间回调**（不是同步直调）：`CardInteraction.Release` 那一帧还在
            //      拖拽处理里，同步进 `DoPlay` 会边走边销毁自己那张卡。零长度序列由 `CardTween` 推进
            //      （真机 = DOTween 自带 Update；批处理 = `CardTween.Advance`）。
            //   ⚠️ `BattleScene` 里那条「落位序列真实长度 == 四段之和」的断言用的是**手牌第一张**做探针，
            //      若那张恰好是战术卡就会变成 0 —— 本批已把那条断言改成显式挑一张**单位卡**做探针（见
            //      `Editor/BattleScene.cs` 同一段注释）。
            if (IsInline(card))
            {
                var instant = DOTween.Sequence();
                instant.AppendInterval(InlineCompleteEpsilon);
                CardTween.Use(instant, Ease.Linear, card.transform);
                if (onDone != null) instant.OnComplete(() => onDone());
                return instant;
            }

            var tr = card.transform;
            // 🔴 `B07`：**起飞之前**就把缩放设成落位尺寸（原版 `MoveMinionToConversionPoint.c:74-83`
            //   是**赋值**、没有补间；原来那句 `DOScale(…, MoveTime)` 是**我们自己加的**观感，已删）。
            tr.localScale = Vector3.one * Mathf.Max(1e-4f, slotScale);

            var seq = DOTween.Sequence();
            // ② 飞到「槽位 + up × minionConversionHeight」—— 时长 = 原版 `minionToConversionPointTime`
            seq.Append(LocalMoveToWorld(tr, abovePos, MoveTime));
            // 🆕 (d) + 🔴 2026-11-02（`B117-R15`）：`WaitForSeconds(GetMinionConversionTime)` 0.3+0.55 **再加** `timeToScaleIntoMoving` 0.1 ⇒ 交接 0.95 s
            seq.AppendInterval(TimeToScaleIntoMoving + CardFeel.HandToBoardDone);
            if (handoff.IsValid)
            {
                // 🔴 **0.95 s = 交接**：原版 state 1 开头 `SetUnitInPlayLayer()` + `DefaultScale` + `HideCardShadow`
                //    —— 手牌卡面换成场上 3D 体、切进竞技场空间。⚠️ 动作由驱动层一处给（见 `Handoff` 的 doc）。
                seq.InsertCallback(HandoffTime, () => handoff.Apply());
                // 悬停（原版 d__314 state 1 的 `WaitForSeconds(timeBeforeLand)` = 0.05）
                seq.AppendInterval(HoldTime);
                // ③ 垂直落下 —— 时长 = 原版 `timeToLand`，**竞技场局部坐标**（卡已 `SetParent(boardRoot)`）
                seq.Append(handoff.FallTarget.DOLocalMove(handoff.LandLocal, LandTime).SetEase(Ease.OutQuad));
            }
            else
            {
                // 悬停（原版 d__314 state 1 的 `WaitForSeconds(timeBeforeLand)`）
                seq.AppendInterval(HoldTime);
                // ③ 垂直落下 —— 时长 = 原版 `timeToLand`。⚠️ 这两点的 x 相同（同格、只差一个 up 偏移），
                //    所以这一段在屏幕/HUD 平面上**逐位就是一条竖线**（战场相机俯仰是单位四元数 ⇒ up 就是屏幕竖直）
                seq.Append(LocalMoveToWorld(tr, landPos, LandTime));
            }
            // ⚠️ **旋转要用 `Insert(0f, …)`，不能用 `Join`**：`Join` 是接在**当前游标**上的
            //    （= 上一次 `Append` 的结束处），那样这条补间会从 0.3s 起算、到 0.6s 才结束。
            //    2026-09-13 就是靠「落位 0.43s ≠ 0.30s」这条断言抓出来的
            //    （旧版更离谱：0.92s 的那两条从 0.18s 起算 → 实际 1.10s）。
            //    ⚠️ 跨度仍是 `MoveTime`（0.3）—— 原版的旋转补间吃 `minionToConversionPointTime`，
            //    即**只覆盖飞行那一段**，落下那 0.2s 里它已经归位。
            //    🔴 缩放**不在这里**：`B07` 起改成**起飞前瞬时赋值**（见上面那一句 + 原版判据）。🔴 2026-11-02（`B117-R15` · `B117-GD` A2）：旋转改 **`DOLocalRotate`**（原版 `MinionManager__MoveMinionToConversionPoint.c:41-47` 吃的是局部角；父链今天无旋转 ⇒ 零可见差，`Hand/CardInteraction.cs:929-935` 已踩过同坑）。
            seq.Insert(0f, tr.DOLocalRotate(Vector3.zero, MoveTime).SetEase(RotateEase));
            // 🆕 2026-10-27（`E3CMP` · 账 804 ④）：落地后的尾拍 —— 原版 d__314 **state 3** 的
            //   `LandOnGround()` → `WaitForSeconds(AttackStepTime)`（`.c:186-205`，`:199` 就是那一句）。
            //   原版「真出牌」（`_ResolvePlayCardFromHand` 等整条协程跑完）因此在 **1.3 s**，
            //   不是旧版的 0.75 s。⛔ 别删：「画面看着也落了」看不出这 0.1 s。
            seq.AppendInterval(LandTailTime);
            CardTween.Use(seq, Ease.Linear, tr);  // 各段自己带了缓动，序列层用线性串起来

            // 🔴 **这一段就是「引擎真正出牌」那一拍**（`onDone` → `BattleDriver.OnCardDeployed` → `DoPlay`
            //    → `RuleCore.PlayCard`）。**挂在序列末尾**（= 落地**再等完尾拍**，1.3 s），判据见文件头：
            //    原版 `_ResolvePlayCardFromHand` 是 `WaitForSeconds(收敛时长)` → `yield MinionPlayedIntoField`
            //    整条跑完 → 才 `ResolveCardPlayed`。
            //    🧨 **改坏法**：把 `TimeToScaleIntoMoving` / `HoldTime` / `LandTime` 从序列里删掉（只留 0.3）
            //    ⇒ 落下那一段根本不放（交接在 0.3 s 就把视图搬进 3D 了），而**画面看起来「也动了」**
            //    （飞过去那一下还在）。
            if (onDone != null) seq.OnComplete(() => onDone());
            return seq;
        }

        /// <summary>登场旋转的缓动 —— 原版那条 `DOLocalRotate` **没有 `SetEase`** ⇒ 走 DOTween 默认缓动。
        /// <para>判据（`B117-R24`，只读代理 G2 现核）：原版起旋转那一句只有
        /// `DOLocalRotate(Vector3.zero, minionToConversionPointTime)`，**后面没有** `SetEase` / `SetOptions`；
        /// DOTween 的 `defaultEaseType` 出厂值 = **`OutQuad`**。我们原来写的是 `Ease.OutCubic`（自造）⇒ 2026-11-02 改回。</para>
        /// <para>⚠️ 两处调用点（`PlayQuad` / `Play`）共用这一个常量 —— ⛔ 别在一处单独写死字面量。</para></summary>
        public static readonly Ease RotateEase = Ease.OutQuad;

        // ✅ 已复刻（2026-11-04 · B141 · 11-08：`Board/DeploySequence.cs:204/218/282/301`）——
        //   原版两条都是 **`DOLocalMove`**：`MinionManager__MoveMinionToConversionPoint.c:66-96`（起飞 0.3）·
        //   `CardScript__UpdateMinionInPlayPosition.c:89`（落下 0.2），目标都写在**父级局部系**里
        //   （起飞目标 = `GetLeft/RightSlotPos` + `up × minionConversionHeight`，也是局部量）。
        //   我们原来这 4 处写的是 `DOMove`（世界系），与判据的调用形状不符 ⇒ 统一走这个窄口。
        //
        // 🔴 坐标帧判定（本批现读，R8 留下的「世界 vs 竞技场局部」问号已结）：
        //   **`abovePos / landPos` 是【世界坐标】**（HUD 平面 z=0）—— 唯一生产者是
        //   `BoardLayout.DropTargetWorld[AtHeight]`，它的 doc 与实现都明写「返回**世界坐标**」
        //   （`Board/BoardLayout.cs:515-554/621-634`，2026-11-03 `B124-W9` 已删掉那句 `InverseTransformPoint`），
        //   而 `BattleDriver.cs:25847-25849` 的 `aboveWorld = aboveLocal`（注释：「返回值**已是世界点**」）。
        //   要喂给 `DOLocalMove` 就必须**先换算成父级局部系**（这里做）——
        //   ⛔ **别直接 `DOLocalMove(abovePos)`**：父级一旦不是恒等（真实战斗中虽然当前是 `sceneRoot` 恒等），
        //   那样会落到 `parent.TransformPoint(abovePos)` 而不是 `abovePos`（本批断言专门盯这一条）。
        //
        // ⚠️ **诚实记一条**（铁律 5 · 供后来人别被「路径不同」误导）：对**静态仿射父级**
        //   （任意平移/旋转/非均匀缩放），`DOMove(world)` 与「世界点换算成父级局部后 `DOLocalMove`」的
        //   世界轨迹**在数学上逐点相同**（仿射映射与凸组合可交换）⇒ 本改动**不改变今天的画面**，
        //   它是「与判据同一调用形状」的保真修复；本仓当前父级恒等，两写法等价。
        //   唯一会拉开差别的情形 = **补间途中父级自己动了**（`DOLocalMove` 跟着父级走、`DOMove` 不跟）。
        /// <summary>把**世界点**换算成 `tr` 父级局部系后做 `DOLocalMove`（起飞/落下两段共用，判据只此一处）。
        /// <para>为什么是这个形状：原版 `minionToConversionPointTime` / `timeToLand` 两条补间都是
        /// `DOLocalMove`（目标在父级局部系）；而我们的 `abovePos/landPos` 由
        /// `BoardLayout.DropTargetWorld[AtHeight]` 给出、是**世界坐标** ⇒ 换算一次再走局部补间。</para>
        /// <para>⛔ 端点世界位置与换算前一致（不是改终点，是改**插值所在的坐标系**）；缓动仍是
        /// `OutQuad`（原版那两条没有 `SetEase` ⇒ DOTween 默认 6）。</para></summary>
        static Tweener LocalMoveToWorld(Transform tr, Vector3 worldTarget, float dur)
        {
            Vector3 local = tr.parent != null ? tr.parent.InverseTransformPoint(worldTarget) : worldTarget;
            return tr.DOLocalMove(local, dur).SetEase(Ease.OutQuad);
        }
    }
}
