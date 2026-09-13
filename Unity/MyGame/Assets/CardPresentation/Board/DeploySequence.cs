// DeploySequence.cs — 「手牌落到战场」的那条动画
//
// ⚠️ **2026-09-13 换过参数**：原来这条的注释写着「时长照搬 `Card Hand To Board` clip 的 0.92s，
//   缓动取近似」—— 那个 0.92 是**认错了来源**：`Card Hand To Board` 是「2D 卡 → 3D 身体」的
//   **交接**动画（2D 卡在 0.3333–0.6667 淡出、3D 身体溶解出现），不是「手牌飞到场位」的移动。
//   移动的时长在 **`MinionManager.minionToConversionPointTime`** 里，而且我们本地有那个组件实例：
//
//   · **权威实例**：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/
//     MonoBehaviour/MonoBehaviour_4372.json`
//       `minionToConversionPointTime = 0.3` · `minionConversionHeight = 1.0`
//       `desiredScale = 0.36` · `MinionSeparation = 0.82` · `slotsPerSide = 4`
//     **怎么判定它是权威的**：`desiredScale 0.36` 与 `MinionSeparation 0.82` 正是我们一直在用的
//     尺寸桥（场卡 137.2 px = 2.0927 × 0.36 × 182.14；相邻中心距 149.3 px = 0.82 × 182.14），
//     而且 `slotsPerSide = 4` 与 9 格棋盘一致。
//   · ⚠️ **另一份 MB 4373 是旧预设，别用**：`desiredScale 0.69` / `MinionSeparation 1.53`
//     （算出来是 263 px 的大卡），而且槽位数组**只有 3 个** —— 正是那个被推翻的「7 格」误读的来源。
//   · 调用链（真反编译）：`BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c`
//     → `MinionManager.PlayMinionFromHand(card, slot, timeToMove, …)`
//     → `WaitForSeconds(MinionManager.GetMinionConversionTime(card))`。
//     `GetMinionConversionTime` 的方法体没被反编译，但它要的时间就是上面那个字段。
//
// **哪些是原版、哪些是我们的**：
//   · 总时长 0.3 s　→ **原版字段**（`minionToConversionPointTime`）
//   · 「先抬起一点再滑过去」这个形状、抬起的高度与占比　→ **我们挑的**（原版是从手牌直接到转换点，
//     没有中段抬升；我们是 2D 手牌，直线滑过去会显得很「数据」）
using System;
using DG.Tweening;
using UnityEngine;

namespace CardPresentation
{
    public static class DeploySequence
    {
        /// <summary>移动段总时长（秒）—— 原版 `MinionManager.minionToConversionPointTime = 0.3`
        /// （出处实例见文件头，`MonoBehaviour_4372.json`）。</summary>
        public const float MoveTime = 0.3f;

        /// <summary>「先抬起」那一段占总时长的比例 —— **我们挑的**</summary>
        const float LiftFraction = 0.35f;

        /// <summary>抬起的高度（世界单位）—— **我们挑的**
        /// （原版没有这一下：`minionConversionHeight 1.0` 是 3D 身体落下来的高度，不是手牌抬升量）</summary>
        const float LiftHeight = 0.35f;

        /// <summary>落位：从手牌姿态滑到格位。`MoveTime` 秒内走完（原版字段）。</summary>
        public static Tween Play(CardView card, Vector3 slotPos, float slotScale, Action onDone = null)
        {
            var tr = card.transform;

            var seq = DOTween.Sequence();
            // 先抬一下（离开手牌），再滑过去 —— 直线滑过去会显得很「数据」
            var lift = tr.position + new Vector3(0f, LiftHeight, -0.2f);
            seq.Append(tr.DOMove(lift, MoveTime * LiftFraction).SetEase(Ease.OutQuad));
            seq.Append(tr.DOMove(slotPos, MoveTime * (1f - LiftFraction)).SetEase(Ease.OutCubic));
            // ⚠️ **旋转/缩放要用 `Insert(0f, …)`，不能用 `Join`**：`Join` 是接在**当前游标**上的
            //    （= 上一次 `Append` 的结束处），那样这两条 0.3s 的补间会从 0.105s 起算、
            //    到 0.405s 才结束 —— **整个序列被拖长 35%**，`OnComplete`（→ 引擎真的出牌）
            //    也跟着晚 0.1s。2026-09-13 就是靠「落位 0.43s ≠ 0.30s」这条断言抓出来的
            //    （旧版更离谱：0.92s 的那两条从 0.18s 起算 → 实际 1.10s）。
            seq.Insert(0f, tr.DORotate(Vector3.zero, MoveTime).SetEase(Ease.OutCubic));
            seq.Insert(0f, tr.DOScale(Vector3.one * slotScale, MoveTime).SetEase(Ease.OutQuad));
            CardTween.Use(seq, Ease.Linear);      // 各段自己带了缓动，序列层用线性串起来

            if (onDone != null) seq.OnComplete(() => onDone());
            return seq;
        }
    }
}
