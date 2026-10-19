// DeploySequence.cs — 「手牌落到战场」的那条动画（**原版是三段**）
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
//  · 缩放补间跨度 = `MoveTime`　→ **我们的**：原版是在起飞**之前**就把 `localScale` 设成
//    `GetUnitSizeInPlay`（瞬时，没有补间，`MoveMinionToConversionPoint.c:74-83`）；
//    我们保留了「0.3 s 收到落位尺寸」的观感（本件不动它）
//  · 缓动 `OutQuad`　→ 原版那两条 `DOLocalMove` **没有 `SetEase`** ⇒ 吃 DOTween 的
//    `defaultEaseType` = `OutQuad`（本工程 `Assets/Resources/DOTweenSettings.asset` 实读 `defaultEaseType: 6`）
//
// ---- 时长们为什么是这三个数（一次说清，别再拆开抄）----------------------
//  总长 = 0.3 + 0.05 + 0.2 = **0.55 s**（`TotalTime`）。`onDone` 挂在**序列末尾**：
//  原版「真正出牌」的那一半（`CardScript.ResolveCardPlayed` + 广播 + 墓地记账）在
//  `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c` 里是**等 `_MinionPlayedIntoField`
//  整条协程跑完**才做的（state 6 → yield → state 7 → `LAB_1809bd825`）⇒ **后演后出**，
//  不是「先出后演」。⚠️ 代价（如实记）：我们的引擎事件只有一条 `Deploy` ⇒ 登场那两件
//  （`Invoke Minion Card Fade Default` + 阵营召唤法阵）也跟着挪到了落地那一拍。
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

        /// <summary>整条序列的真实长度 = 三段之和（秒）。
        /// ⚠️ **别在别处再算一遍** —— `Editor/BattleScene.cs` 的两条断言都引用它。</summary>
        public const float TotalTime = MoveTime + HoldTime + LandTime;

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

        /// <summary>这张卡这一次落位**整条序列的真实长度**（秒）—— 单位卡 = <see cref="TotalTime"/>，
        /// 非单位卡 = <see cref="InlineCompleteEpsilon"/>。与 <see cref="Play"/> 的分支**同源**
        /// （都问 `IsInline`），⛔ 别写第二份「战术卡 0.001 / 单位卡 0.55」的字面量。</summary>
        public static float DurationFor(CardView card)
        {
            return IsInline(card) ? InlineCompleteEpsilon : TotalTime;
        }

        /// <summary>🆕 2026-10-19（战斗线 B3 · 偏差 8 的载体那一半）：**敌方手牌那张卡背**（`ImageQuad`）
        /// 走**同一条三段**（0.3 飞向格位上方 → 0.05 悬停 → 0.2 落下）。
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
        /// <para>`onDone` 同样挂在**序列末尾**（= 落地那一拍，0.55 s）。</para></summary>
        public static Tween PlayQuad(Transform tr, Vector3 abovePos, Vector3 landPos,
                                     float endScale, Action onDone = null)
        {
            if (tr == null) return null;
            var seq = DOTween.Sequence();
            seq.Append(tr.DOMove(abovePos, MoveTime).SetEase(Ease.OutQuad));
            seq.AppendInterval(HoldTime);
            seq.Append(tr.DOMove(landPos, LandTime).SetEase(Ease.OutQuad));
            // 同 <see cref="Play"/>：旋转/缩放用 `Insert(0f, …)`（⛔ 不是 `Join`，那会从 0.3 s 才起算）
            seq.Insert(0f, tr.DORotate(Vector3.zero, MoveTime).SetEase(Ease.OutCubic));
            seq.Insert(0f, tr.DOScale(Vector3.one * Mathf.Max(1e-4f, endScale), MoveTime).SetEase(Ease.OutQuad));
            CardTween.Use(seq, Ease.Linear, tr);
            if (onDone != null) seq.OnComplete(() => onDone());
            return seq;
        }

        /// <summary>落位：从手牌姿态**飞到槽位正上方**，再**垂直落下**。
        /// <para>`abovePos` = 「那一格的卡**抬高 `minionConversionHeight`** 之后」在**本补间所在空间**里的点
        /// （3D 战场下 = `BoardLayout.DropTargetWorldAtHeight(slot, which.MinionConversionHeight)`；
        ///  非 3D 时它就是 `landPos` ⇒ 退化成一记直飞）。</para>
        /// <para>`landPos` = 「那一格的卡」本来的补间终点（3D 下 = `BoardLayout.DropTargetWorld(slot)`）。</para>
        /// <para>`onDone` 在**序列末尾**（= 落地那一拍）触发 —— 理由见文件头最后一段（**不是**在 0.3 s 处）。</para>
        /// </summary>
        public static Tween Play(CardView card, Vector3 abovePos, Vector3 landPos, float slotScale,
                                Action onDone = null)
        {
            // 🆕 2026-10-19（战斗线 B1 · 偏差 5「战术卡多一段 0.55 s 飞向格位」）：
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
            //   ⚠️ `BattleScene` 里那条「落位序列真实长度 == 三段之和」的断言用的是**手牌第一张**做探针，
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

            var seq = DOTween.Sequence();
            // ② 飞到「槽位 + up × minionConversionHeight」—— 时长 = 原版 `minionToConversionPointTime`
            seq.Append(tr.DOMove(abovePos, MoveTime).SetEase(Ease.OutQuad));
            // 悬停（原版 d__314 state 1 的 `WaitForSeconds(timeBeforeLand)`）
            seq.AppendInterval(HoldTime);
            // ③ 垂直落下 —— 时长 = 原版 `timeToLand`。⚠️ 这两点的 x 相同（同格、只差一个 up 偏移），
            //    所以这一段在屏幕/HUD 平面上**逐位就是一条竖线**（战场相机俯仰是单位四元数 ⇒ up 就是屏幕竖直）
            seq.Append(tr.DOMove(landPos, LandTime).SetEase(Ease.OutQuad));
            // ⚠️ **旋转/缩放要用 `Insert(0f, …)`，不能用 `Join`**：`Join` 是接在**当前游标**上的
            //    （= 上一次 `Append` 的结束处），那样这两条补间会从 0.3s 起算、到 0.6s 才结束。
            //    2026-09-13 就是靠「落位 0.43s ≠ 0.30s」这条断言抓出来的
            //    （旧版更离谱：0.92s 的那两条从 0.18s 起算 → 实际 1.10s）。
            //    ⚠️ 跨度仍是 `MoveTime`（0.3）—— 原版的旋转补间吃 `minionToConversionPointTime`，
            //    即**只覆盖飞行那一段**，落下那 0.2s 里它已经归位。
            seq.Insert(0f, tr.DORotate(Vector3.zero, MoveTime).SetEase(Ease.OutCubic));
            seq.Insert(0f, tr.DOScale(Vector3.one * slotScale, MoveTime).SetEase(Ease.OutQuad));
            CardTween.Use(seq, Ease.Linear, tr);  // 各段自己带了缓动，序列层用线性串起来

            // 🔴 **这一段就是「引擎真正出牌」那一拍**（`onDone` → `BattleDriver.OnCardDeployed` → `DoPlay`
            //    → `RuleCore.PlayCard`）。**挂在序列末尾**（= **落定**那一刻，0.55 s），判据见文件头最后一段：
            //    原版 `_ResolvePlayCardFromHand` 是 `WaitForSeconds(收敛时长)` → `yield MinionPlayedIntoField`
            //    整条跑完 → 才 `ResolveCardPlayed`。
            //    🧨 **改坏法**：把 `HoldTime`/`LandTime` 从序列里删掉（只留 0.3）⇒ 落下那一段根本不放
            //    （交接在 0.3 s 就把视图搬进 3D 了），而**画面看起来「也动了」**（飞过去那一下还在）。
            if (onDone != null) seq.OnComplete(() => onDone());
            return seq;
        }
    }
}
