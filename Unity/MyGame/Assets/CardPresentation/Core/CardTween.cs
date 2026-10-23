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

        /// <summary>松手回弹（不合法落点 / 拖回手牌）—— **这个 0.22 是我们挑的，原版判据没定位到**。
        ///
        /// <para>🔴 **2026-10-23（R16）如实标注（铁律 3 / 11）**：本常量原来**零注释、零出处**，
        /// 账上那条待办写的是「查 `CardScript.OnTouchDrag` 取消出牌那一支」—— **去查了，那一支不存在**：
        /// `d:/2/tools/decomp_full/CardScript__OnTouchDrag.c` 全文 **52 行**，只有一句
        /// `if (*(int *)(lVar4 + 0x1c) != 0xa0) …`（卡状态闸），**没有任何「回弹 / DOMove / 补间时长」**。
        /// ⇒ 今天**判据仍然缺**，不许拿 `VarsGlobal.timeToPositionCard`（0.2，那是**手牌重排**
        /// `RelayoutDuration` 的出处）硬套过来 —— 那是「看着像」的等价，不是读出来的。</para>
        ///
        /// <para>⚠️ 用法：`Hand/CardInteraction` 的两处（回弹到扇形原位 + 缩放回手牌尺寸）。
        /// 若将来查到原版那一支，**改这一个数**即可（两处共用，⛔ 别在调用点各写一个）。</para></summary>
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

        /// <summary>把一张卡补间到「位置 + 倾角 + 缩放」。
        ///
        /// <para>🔴 **`ease` 只对「整条 Sequence」生效，而它一定是二次弯曲**（2026-10-24 · A22 审计）。
        /// 本方法把三条**没有各自 `SetEase`** 的子补间 `Join` 进一个 `DOTween.Sequence()`，再把 <paramref name="ease"/>
        /// 挂到 **Sequence 上**（`DOTween.XML`：`SetEase` "If applied to Sequences eases the whole sequence animation"）。
        /// 反编译 `DOTween.dll` 实读：`Tweener.Setup` 给子补间 `DOTween.defaultEaseType`（本工程 = 6 OutQuad），
        /// 而 `Sequence.Setup` 把序列自己的 `easeType` 硬编码成 `Ease.Linear`；`Sequence.DoApplyTween` 在
        /// `easeType == Linear` 时**整段跳过**缓动 ⇒ **原版不写 `SetEase` 的 Sequence 是「不弯曲」的**。
        /// ⇒ **传 `Linear`** = 三条子补间各自 OutQuad（= 原版的形状）；**传任何别的 ease** = 在其上再弯一次。
        /// ⛔ 所以调用方「想要 OutQuad」时**必须**传 `Ease.Linear`，别把 `OutQuad` 传进来。</para>
        /// <para>⚠️ 判据不是「本工程的约定」，是原版方法体：`CardDisplayWindow__ChangeCardPosition.c`
        /// 全文一个 `DOTween.Sequence()` + 六条 `Join`、**没有** `SetEase`。</para></summary>
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
