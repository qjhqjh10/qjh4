// DeploySequence.cs — 「手牌落到战场」的那条动画
//
// 原版这条是 `Card Hand To Board` clip：**0.92 秒、17 条曲线**（位置/旋转/缩放各分量），
// 是目前手上最完整的一条时序蓝图。这里先用「时长照搬 + 缓动取近似」，
// 等把 clip 的曲线拉出来（`资料/特效还原_进度与交接.md` 里有取原版数据的路子）再逐条对齐。
using System;
using DG.Tweening;
using UnityEngine;

namespace CardPresentation
{
    public static class DeploySequence
    {
        /// <summary>总时长 —— 取自原版 clip（0.92s）</summary>
        public const float Duration = 0.92f;

        /// <summary>落位：从手牌姿态滑到格位，中途稍微放大、落定时收住。</summary>
        public static Tween Play(CardView card, Vector3 slotPos, float slotScale, Action onDone = null)
        {
            var tr = card.transform;

            var seq = DOTween.Sequence();
            // 先抬一下（离开手牌），再滑过去 —— 直线滑过去会显得很「数据」
            var lift = tr.position + new Vector3(0f, 0.45f, -0.2f);
            seq.Append(tr.DOMove(lift, 0.18f).SetEase(Ease.OutQuad));
            seq.Append(tr.DOMove(slotPos, Duration - 0.18f).SetEase(Ease.InOutCubic));
            seq.Join(tr.DORotate(Vector3.zero, Duration).SetEase(Ease.OutCubic));
            seq.Join(tr.DOScale(Vector3.one * slotScale, Duration).SetEase(Ease.OutBack, 1.2f));
            CardTween.Use(seq, Ease.Linear);      // 各段自己带了缓动，序列层用线性串起来

            if (onDone != null) seq.OnComplete(() => onDone());
            return seq;
        }
    }
}
