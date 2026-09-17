// CardTween.cs — 补间的统一入口，顺便解决「批处理里怎么验动画」
//
// 两个作用：
//   1. **一处切换补间的推进方式**。play 模式下 DOTween 自己跟着 Update 走；
//      批处理自检里没有 play 循环，要换成 `UpdateType.Manual` 再由测试手动
//      `DOTween.ManualUpdate(dt)` 推进。写死 `UpdateType.Normal` 的话自检根本验不了动画。
//   2. 把手感参数（时长/缓动）集中放，方便和原版数值对照。
//
// 手感数值的来源：原版 `Card Hand To Board` clip（0.92s，17 条曲线）+ 74 个 `UnitTweenSO`
// 的 ease/duration。**目前用的是「照着量级取的近似值」**，等把 clip 的曲线拉出来再逐个对齐。
using DG.Tweening;
using UnityEngine;

namespace CardPresentation
{
    public static class CardTween
    {
        /// <summary>补间推进方式。批处理自检会把它设成 Manual。</summary>
        public static UpdateType Mode = UpdateType.Normal;

        /// <summary>松手回弹（不合法落点）</summary>
        public const float SnapBackDuration = 0.22f;
        /// <summary>拿起时的放大。
        /// 🔴 **2026-09-17 换成正解**：原版值 = `VarsGlobal.timeToScaleIntoMoving = 0.1`
        /// （手牌「起身」用时）。**原来写 0.12 是「我们挑的」**，已替换。
        /// 出处：`资料/VarsGlobal_原版数值.md` §一（`工具/read_varsglobal.py` 从
        /// `Warpforge_Data/sharedassets0.assets` 按签名桩字段顺序解出）。</summary>
        public const float PickUpDuration = 0.1f;

        /// <summary>手牌重排（邻牌让位/补位）。
        /// 🔴 **2026-09-17 换成正解**：原版值 = `VarsGlobal.timeToPositionCard = 0.2`。
        /// **原来写 0.18 是「我们挑的」，当时的理由是「原版的 `timeToPositionCard` 在没被反编译的调用方里」
        /// —— 那条理由已经作废**，两半都被补上了：
        ///   ① 调用方反编译出来了：`decomp_out2/PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:76-80`
        ///      —— 先取 `BattleManager.get_globalVars()`，再读 `*(float*)(globalVars + 0xA4)`，
        ///      作为第 4 实参喂给 `PlayerHand.PositionCardInHand`；
        ///   ② `VarsGlobal` 整表已在 `Warpforge_Data/sharedassets0.assets` 里解出（那份 .assets
        ///      **没有 type tree** ⇒ UnityPy 读不出字段名 ⇒ 当年按字段名 grep 全树 0 命中，是**没找到**不是**不存在**）。
        /// 出处：`资料/VarsGlobal_原版数值.md` §一。</summary>
        public const float RelayoutDuration = 0.2f;
        /// <summary>把手感参数挂到 tween 上（推进方式 + 缓动 + **绑定生命周期**）。
        ///
        /// 🔴 **`link` 是必填的**（2026-09-16 加）：不绑的话，**卡视图被销毁时这条补间没人杀**，
        /// DOTween 的安全模式会**每帧**记一条
        /// `DOTWEEN ► Target or field is missing/null … Transform.set_position/set_rotation/set_localScale`。
        /// **实据**：构建后 player 验证在**真包里抓到 22 条**，而**编辑器日志 0 条** ——
        /// 当时工程里 `DOMove/DOScale/DORotate` 共 **21 处调用、`SetLink` 0 处**。
        /// （⚠️ 「编辑器 0 条」**不等于没问题**，只是那条路没被走到 —— 别拿它当「已经没事了」。）
        ///
        /// 做成**必填参数是有意的**：漏一处就**编译不过**，不会静默漏掉（这正是这个 bug 的成因）。
        /// **传谁**：补间作用的那张卡 / 那个物体的 Transform。</summary>
        /// <summary>诊断开关：置 `false` 就退回「**不**绑生命周期」（= 2026-09-16 之前的行为）。
        /// **只给 A/B 用** —— 用来回答「某个现象是不是 `SetLink` 引起的」。
        /// 从 player 命令行开：环境变量 `WF_NO_SETLINK=1`（见 `PlayerBoot`）。</summary>
        public static bool LinkEnabled = true;

        public static Tween Use(Tween t, Ease ease, Component link)
        {
            t = t.SetEase(ease).SetUpdate(Mode);
            if (LinkEnabled) t = t.SetLink(link.gameObject);
            return t;
        }

        /// <summary>把一张卡补间到「位置 + 倾角 + 缩放」。</summary>
        public static Tween ToPose(Transform tr, Vector3 pos, float rotZ, float scale,
                                   float duration, Ease ease)
        {
            var seq = DOTween.Sequence();
            seq.Join(tr.DOMove(pos, duration));
            seq.Join(tr.DORotate(new Vector3(0f, 0f, rotZ), duration));
            seq.Join(tr.DOScale(Vector3.one * scale, duration));
            return Use(seq, ease, tr);
        }

        /// <summary>批处理自检用：推进所有 Manual 模式的补间</summary>
        public static void Advance(float dt)
        {
            DOTween.ManualUpdate(dt, dt);
        }
    }
}
