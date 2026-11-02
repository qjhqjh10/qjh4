// TransformPresentation.cs — **督军变身演出**（0.9 s 三拍）
//
// 🔶 自己设计（非原版）：整套「督军变身」演出（溶解 / 闪白 / materialize 三拍 + 槽位能量环 + 登场白光）
//   · 为什么必须自己定：**原版没有这段演出**。原版的「督军变身」= `BattleManager.CheckForHeroDeckTransform`
//     在 `SetupBoardPhase`（开局装配、换牌之前）**一次换掉 `rawBattleData.deckWarlord`**，
//     只多做一件表现层的事 = `VoiceLinesController.SetTransformedWarlordNewChats`（换对白池），
//     **没有任何专属 VFX / 动画 / cue**（判据链见 `RuleCorePartial_B96CB.cs` 文件末「§变身」那一大段；
//     `d:/2/tools/decomp_full/BattleManager__CheckForHeroDeckTransform.c` 全文 101 行，逐句读过）。
//   · 用户 2026-11-03 裁定：「督军变身的特效/动画，按照你的建议做一个自我设计。」
//   · 规格正本 = `资料/完全复刻/报告/B121_E03F_变身卡与演出设计.md` §二（三拍参数表）。
//
// ────────────────────────────────────────────────────────────────────────────────
//  拍子（三拍，全部自设计）：
//    0.00 – 0.30 s  旧督军**溶解**：alpha 1→0 · 缩放 1→0.90 · 上浮 +0.15 世界单位（OutQuad）
//    0.30 – 0.45 s  槽位**白光闪**：alpha 0→0.85→0（Sin 包络），颜色 (1, 0.95, 0.80, 1)
//    0.45 – 0.90 s  新督军 **materialize**：alpha 0→1（OutQuad）· 缩放 0.90→1，中段超调 1.04
//  之后：新督军回到**待机终态**（alpha = 1 · `SetExtraScale(one)`），并触发登场语音（有数据才响）。
//
//  ⚠️ **最小 0.45 s 不可跳过**（`MinUnskippable = FlashEnd`）：前两拍（溶解 + 闪白）必须走完；
//     之后玩家可以照常操作，演出按自己的时间轴走完 —— 证据 = 这一档**没有**登记进
//     `BattleDriver.BlocksPlayerActions`（那个 switch 只认 Play/Deploy/Attack/… 那些**引擎动作**），
//     ⇒ 玩家动作队列不会被它挡住。⛔ 不冻结输入、不改引擎状态、不碰 `ctx.Rng`。
//
//  ⚠️ **无帧循环（批处理）**：补间走 `CardTween.Mode`（真机 Normal / 自检 Manual），
//     而**三拍的全部参数**由纯函数 <see cref="PoseAt"/> 给出 —— 自检直接采样它，不需要帧循环，
//     也不需要 `DOTween.ManualUpdate`。`Play` 只是把同一个 `PoseAt` 逐帧喂到两个 `CardView` 上
//     （⇒ **补间与断言不可能各说一套**，这是本文件的反自证设计）。
//
//  ────────────────────────────────────────────────────────────────────────────────
//  查不到的资源（⛔ 不冒充原版，逐条写在这里）：
//    · **粒子**：原版**没有**这条演出 ⇒ 也没有它的 prefab。`WarpforgeEffectLibrary.asset` 现读 **982 条**，
//      按 `transform` / `warlord` 零命中；`Remnant` 只有 5 条（`RemnantBody3D Aeldari/Necrons` +
//      `Remnant Aeldari Collect/Destroy`、`Remnant Necrons Destroy`）= **残骸/收集那一族**，不是变身。
//      ⇒ 本轮**自己挑**了两件**现有**的库内件（⛔ 没有一件被说成「原版就是它」）：
//        - 能量环（0.00 s 起）= `BlueSummonCircle`（库内召唤圈，最接近「脚下起一圈」）。
//        - materialize（0.45 s 起）= `Sau_Dimensional_Breach_Spawn`（库内「维度裂隙生成」，最接近「凭空出现」）。
//    · **音效**：`Resources/battle_sounds.json` 现读 34 条 cue，**没有** transform / warlord 一类的键
//      （只有 `invokeCUE` / `invokeCUELegendary` 那条**召唤**族）⇒ 复用 `BattleSfx.InvokeCUE`（登场 cue）。
//      ⛔ 没有「闪白那一拍的 whoosh」这种 cue ⇒ 那一拍**不响**（⛔ 不拿别的音顶上）。
//    · **待机/呼吸**：原版战斗里的督军没有「待机循环动画」这一档（`CardView` 只有残骸的
//      `Particles Death Idle`，不是活卡）⇒ 本演出**收在终态**（alpha 1 · 缩放 1），不自己造循环。
//      登场语音 = 走原版那条**卡级 `deploy` 语音**（`VoiceLines.ExactDeploy`，数据有值才响）。
using System;
using DG.Tweening;
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>督军变身演出（🔶 自己设计 · 用户 2026-11-03 裁定）。
    /// <para>**三拍的全部数值只在这个类里**（`CardFeel` / `BattleDriver` 都不许再写一份）。</para></summary>
    public static class TransformPresentation
    {
        // ==================================================================
        //  参数表（🔶 自己设计 · 用户 2026-11-03 裁定 —— 规格正本见文件头）
        // ==================================================================

        /// <summary>第一拍结束（旧督军溶解完）= 0.30 s。</summary>
        public const float DissolveEnd = 0.30f;
        /// <summary>第二拍结束（闪白收）= 0.45 s。**同时也是「不可跳过」的门槛**（见 <see cref="MinUnskippable"/>）。</summary>
        public const float FlashEnd = 0.45f;
        /// <summary>整条演出 = 0.90 s。</summary>
        public const float Total = 0.90f;
        /// <summary>**最小不可跳过时长** = 0.45 s（溶解 + 闪白那两拍）。之后玩家可照常操作、演出自己走完。</summary>
        public const float MinUnskippable = FlashEnd;

        /// <summary>旧督军溶解的终点缩放（1 → 0.90）。</summary>
        public const float OldEndScale = 0.90f;
        /// <summary>旧督军上浮量（世界单位，+Y）。</summary>
        public const float OldRiseY = 0.15f;

        /// <summary>闪白的峰值不透明度（0 → 0.85 → 0）。</summary>
        public const float FlashPeakAlpha = 0.85f;
        /// <summary>闪白的颜色（规格表给定值）。</summary>
        public static readonly Color FlashColor = new Color(1f, 0.95f, 0.80f, 1f);

        /// <summary>新督军 materialize 的起点缩放（0.90 → 1）。</summary>
        public const float NewStartScale = 0.90f;
        /// <summary>materialize 中段的超调峰值。</summary>
        public const float NewOvershoot = 1.04f;
        /// <summary>超调峰值出现在 materialize 段的**归一化位置**（0.70 ⇒ 绝对时刻 0.765 s）。</summary>
        public const float OvershootAt = 0.70f;

        /// <summary>槽位能量环那件库内粒子（见文件头「查不到的资源」——**不是原版件**）。</summary>
        public const string RingFx = "BlueSummonCircle";
        /// <summary>materialize 那一拍的库内粒子（同上）。</summary>
        public const string MaterializeFx = "Sau_Dimensional_Breach_Spawn";

        // ==================================================================
        //  纯函数：t（秒）→ 姿势
        // ==================================================================

        /// <summary>某一时刻该是什么样。**三拍数值的唯一出处** —— `Play` 逐帧喂它，自检直接采样它。</summary>
        public struct Pose
        {
            /// <summary>旧督军替身的整卡 alpha（1 → 0）。</summary>
            public float OldAlpha;
            /// <summary>旧督军替身的缩放**倍数**（乘在建它那一刻的 `localScale` 上；1 → 0.90）。</summary>
            public float OldScale;
            /// <summary>旧督军替身的上浮（世界单位，+Y；0 → 0.15）。</summary>
            public float OldRise;
            /// <summary>白光闪的 alpha（0 → 0.85 → 0）。</summary>
            public float FlashAlpha;
            /// <summary>新督军的整卡 alpha（0 → 1）。</summary>
            public float NewAlpha;
            /// <summary>新督军的**额外**缩放倍数（0.90 → 1.04 → 1；落到 `CardView.SetExtraScale`）。</summary>
            public float NewScale;
        }

        /// <summary>时刻所在的拍（自检读口；数值即「第几拍」）。</summary>
        public enum Phase
        {
            /// <summary>还没开始（`t &lt;= 0`）—— 与第一拍起点同形。</summary>
            Before = 0,
            /// <summary>0.00–0.30 s：旧督军溶解。</summary>
            Dissolve = 1,
            /// <summary>0.30–0.45 s：槽位白光闪。</summary>
            Flash = 2,
            /// <summary>0.45–0.90 s：新督军 materialize。</summary>
            Materialize = 3,
            /// <summary>≥ 0.90 s：终态（alpha 1 · 缩放 1）。</summary>
            Done = 4,
        }

        static float OutQuad(float u) { u = Mathf.Clamp01(u); float k = 1f - u; return 1f - k * k; }
        static float SmoothStep(float u) { u = Mathf.Clamp01(u); return u * u * (3f - 2f * u); }

        /// <summary>这一时刻属于哪一拍。</summary>
        public static Phase PhaseAt(float t)
        {
            if (t <= 0f) return Phase.Before;
            if (t < DissolveEnd) return Phase.Dissolve;
            if (t < FlashEnd) return Phase.Flash;
            if (t < Total) return Phase.Materialize;
            return Phase.Done;
        }

        /// <summary>**三拍的全部数值只在这个方法里**（🔶 自己设计 · 用户 2026-11-03 裁定）。
        /// <para>`t &gt;= Total` ⇒ **终态**（旧 alpha 0 · 新 alpha 1 · 新缩放 1），与 <see cref="DonePose"/> 逐字相同。</para></summary>
        public static Pose PoseAt(float t)
        {
            var p = new Pose();

            // ---- ① 0.00–0.30 s：旧督军溶解（OutQuad）----
            if (t < DissolveEnd)
            {
                float e = OutQuad(t / DissolveEnd);
                p.OldAlpha = 1f - e;
                p.OldScale = 1f + (OldEndScale - 1f) * e;
                p.OldRise = OldRiseY * e;
                p.FlashAlpha = 0f;
                p.NewAlpha = 0f;
                p.NewScale = NewStartScale;
                return p;
            }

            // 溶解段之后，旧督军恒「已消失」
            p.OldAlpha = 0f;
            p.OldScale = OldEndScale;
            p.OldRise = OldRiseY;

            // ---- ② 0.30–0.45 s：槽位白光闪（Sin 包络，峰值 0.85）----
            if (t < FlashEnd)
            {
                float u = (t - DissolveEnd) / (FlashEnd - DissolveEnd);
                p.FlashAlpha = FlashPeakAlpha * Mathf.Sin(Mathf.PI * u);
                p.NewAlpha = 0f;
                p.NewScale = NewStartScale;
                return p;
            }

            // ---- ③ 0.45–0.90 s：新督军 materialize（OutQuad + 超调 1.04）----
            p.FlashAlpha = 0f;
            if (t >= Total) { p.NewAlpha = 1f; p.NewScale = 1f; return p; }
            float m = (t - FlashEnd) / (Total - FlashEnd);
            p.NewAlpha = OutQuad(m);
            p.NewScale = NewScaleAt(m);
            return p;
        }

        /// <summary>materialize 段的缩放曲线（0.90 → 1.04 → 1）。两段在 `OvershootAt` 处连续。</summary>
        static float NewScaleAt(float m)
        {
            m = Mathf.Clamp01(m);
            if (m <= OvershootAt)
                return Mathf.Lerp(NewStartScale, NewOvershoot, OutQuad(m / OvershootAt));
            float k = (m - OvershootAt) / (1f - OvershootAt);
            return Mathf.Lerp(NewOvershoot, 1f, SmoothStep(k));
        }

        /// <summary>终态（自检与 `OnComplete` 共用一份 —— ⛔ 别各写一套 1/1）。</summary>
        public static Pose DonePose { get { return PoseAt(Total); } }

        // ==================================================================
        //  事件 → 电影计划（判别式只此一处）
        // ==================================================================

        /// <summary>一条事件配出来的演出计划。**判别式只此一处**（`Play` 与自检同源）。</summary>
        public struct Plan
        {
            /// <summary>`false` ⇒ 不是本演出的那一档（调用方什么都不做）。</summary>
            public bool Valid;
            /// <summary>变身的是哪一方（0/1）。</summary>
            public int Seat;
            /// <summary>旧督军卡名（事件 `Effect`；空 = 原来没有督军 ⇒ 不播溶解那一段）。</summary>
            public string OldCardId;
            /// <summary>新督军卡名（事件 `CardId`）。</summary>
            public string NewCardId;
            /// <summary>整条演出时长（秒）= <see cref="Total"/>。</summary>
            public float Seconds;
        }

        /// <summary>事件 → 计划。`Valid == false` ⇒ 不归本演出（别的 `EvtKind` / 座位越界 / `null`）。</summary>
        public static Plan PlanOf(BattleEvent e, int me)
        {
            var plan = default(Plan);
            if (e == null || e.Kind != EvtKind.WarlordTransform) return plan;
            if (e.Player < 0 || e.Player > 1) return plan;
            plan.Valid = true;
            plan.Seat = e.Player;
            plan.OldCardId = e.Effect;          // 本档专用：`Effect` 装**旧督军卡名**（见 `EvtKind.WarlordTransform` 的 doc）
            plan.NewCardId = e.CardId;
            plan.Seconds = Total;
            return plan;
        }

        // ==================================================================
        //  播放（把 PoseAt 逐帧喂到视图上）
        // ==================================================================

        /// <summary>一条正在播的演出。`Kill()` 会杀掉补间、收掉白光件，并回调清理口。</summary>
        public sealed class Playback
        {
            internal CardView OldCard;
            internal CardView NewCard;
            internal ImageQuad Flash;
            internal Vector3 SlotPos;
            internal Vector3 OldBaseScale = Vector3.one;
            internal Tween Tween;
            internal Action OnDone;
            internal Action OnDispose;
            internal bool Cleaned;

            /// <summary>已经过了多少秒。</summary>
            public float Elapsed { get; internal set; }
            /// <summary>现在在第几拍（⚠️ 属性名**不叫** `Phase` —— 与类型同名会踩 C# 的「同名简单名与类型名」歧义，别改）。</summary>
            public Phase CurrentPhase { get; internal set; }
            /// <summary>现在这一刻的姿势（= `PoseAt(Elapsed)`）。</summary>
            public Pose Current { get; internal set; }
            /// <summary>整条演出结束了吗。</summary>
            public bool Done { get { return CurrentPhase == Phase.Done; } }
            /// <summary>旧督军替身（没有旧卡 / 建不出来时是 `null`）。</summary>
            public CardView OldView { get { return OldCard; } }
            /// <summary>新督军那张视图。</summary>
            public CardView NewView { get { return NewCard; } }
            /// <summary>白光闪那一块（`null` = 没建出来）。</summary>
            public ImageQuad FlashQuad { get { return Flash; } }

            /// <summary>**按一个时刻把姿势写到视图上**（`Play` 每帧调它；自检也用它 —— 无帧循环也能验画面终态）。</summary>
            public void ApplyForTest(float t)
            {
                Elapsed = t;
                Current = PoseAt(t);
                CurrentPhase = PhaseAt(t);
                ApplyPose();
            }

            internal void ApplyPose()
            {
                var p = Current;
                if (OldCard != null)
                {
                    OldCard.SetAlpha(p.OldAlpha);
                    // ⚠️ 替身卡是**一次性**的（收尾即销毁）⇒ 直接写 `localScale`（同 `CardFeel.Dissolve` 的先例）。
                    //     ⛔ 别拿这条去改真卡：真卡走 `SetExtraScale`（见下面），否则 `_baseScale` 会停在旧值上。
                    OldCard.transform.localScale = OldBaseScale * p.OldScale;
                    OldCard.transform.position = SlotPos + new Vector3(0f, p.OldRise, 0f);
                }
                if (Flash != null)
                {
                    var c = FlashColor;
                    c.a *= p.FlashAlpha;
                    Flash.SetTint(c);
                    bool on = p.FlashAlpha > 1e-4f;
                    if (Flash.gameObject.activeSelf != on) Flash.gameObject.SetActive(on);
                }
                if (NewCard != null)
                {
                    NewCard.SetAlpha(p.NewAlpha);
                    // 🔴 真卡必须走 `SetExtraScale`：它乘在 `_baseScale × _hlMul` 之上，
                    //    后续任何一次 `SetPose` / `SetHighlightScale`（→ `ApplyScale`）都不会把卡弹回旧尺寸
                    //    （直接写 `transform.localScale` 就会 —— `CardView.BaseScale` 的 doc 记着这个坑）。
                    NewCard.SetExtraScale(Vector3.one * p.NewScale);
                }
            }

            /// <summary>提前收场（切局 / 重开 / 自检收尾）。**幂等**。</summary>
            public void Kill()
            {
                if (Tween != null && Tween.IsActive()) Tween.Kill();
                Tween = null;
                Cleanup();
            }

            internal void Cleanup()
            {
                if (Cleaned) return;
                Cleaned = true;
                // 🔴 **真卡必须回到终态** —— 正常播完时 `ApplyForTest(Total)` 已经写成 1 / 1（这里是幂等的），
                //    但**被提前 `Kill`**（切局 / 重开 / 视图被销毁前的一拍）时，新督军会停在
                //    `alpha 0 · 缩放 0.9` 上 ⇒ 那张卡在场上**看不见**（静默，最糟的一种收场）。
                if (NewCard != null)
                {
                    NewCard.SetAlpha(1f);
                    NewCard.SetExtraScale(Vector3.one);
                }
                if (Flash != null)
                {
                    var go = Flash.gameObject;
                    Flash = null;
                    // 批处理里没有帧循环 ⇒ `Destroy` 不生效（同 `BattleDriver.Kill` 的口径）
                    if (go != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); }
                }
                if (OnDispose != null) { var d = OnDispose; OnDispose = null; d(); }
            }
        }

        /// <summary>
        /// 播一次三拍演出。**只动表现层**：不改引擎状态、不碰 `ctx`、不消耗随机数。
        /// </summary>
        /// <param name="plan">由 <see cref="PlanOf"/> 得到（`Valid == false` ⇒ 本方法什么都不做，返回 `null`）。</param>
        /// <param name="oldCard">旧督军的**临时替身**视图（调用方新建、由 `onDispose` 负责销毁；`null` = 没有旧卡，跳过溶解段）。</param>
        /// <param name="newCard">新督军那张**真视图**（收尾会把它恢复成 alpha 1 · 缩放 1）。</param>
        /// <param name="fxParent">白光闪那一块的父节点（世界空间；一般是 `BattleDriver.hudRoot`）。</param>
        /// <param name="slotPos">督军格的世界坐标（白光闪与替身都摆在这儿）。</param>
        /// <param name="onMaterialize">进入第三拍（0.45 s）时回调一次 —— 调用方在这儿播 materialize 粒子。</param>
        /// <param name="onDone">整条演出播完时回调一次 —— 调用方在这儿接登场语音 / 计数。</param>
        /// <param name="onDispose">收场时回调（正常播完 / 被 `Kill` 都走）—— 调用方在这儿销毁临时替身。</param>
        public static Playback Play(Plan plan, CardView oldCard, CardView newCard, Transform fxParent, Vector3 slotPos,
                                    Action onMaterialize = null, Action onDone = null, Action onDispose = null)
        {
            if (!plan.Valid) return null;

            var pb = new Playback
            {
                OldCard = oldCard,
                NewCard = newCard,
                SlotPos = slotPos,
                OldBaseScale = oldCard != null ? oldCard.transform.localScale : Vector3.one,
                OnDone = onDone,
                OnDispose = onDispose,
            };

            // ---- 白光闪那一块：世界空间的白 quad（`ImageQuad` 只吃 `Texture`；内置白图 = 纯色）----
            if (fxParent != null)
            {
                float worldH = newCard != null
                    ? CardView.Height * Mathf.Abs(newCard.transform.lossyScale.y)
                    : CardView.Height * LayoutSpace.Scale;
                pb.Flash = ImageQuad.Create(fxParent, Texture2D.whiteTexture, Vector3.zero, worldH,
                                            new Vector2(0.5f, 0.5f), "WarlordTransformFlash");
                if (pb.Flash != null)
                {
                    pb.Flash.SetAspect(CardView.Width / CardView.Height);   // 与卡同宽高比（白图是 1:1）
                    pb.Flash.transform.position = slotPos;
                    pb.Flash.SetTint(new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0f));
                    pb.Flash.gameObject.SetActive(false);
                }
            }

            // 起始姿势当场落一次（`t = 0`：新督军隐藏、旧督军原样、白光全透明）
            pb.ApplyForTest(0f);

            // ---- 时间轴：一条线性补间把 `t` 从 0 推到 Total，每帧把 `PoseAt(t)` 写到视图上 ----
            float t = 0f;
            Phase lastPhase = pb.CurrentPhase;
            Tween tw = DOTween.To(() => t, v =>
            {
                t = v;
                pb.Elapsed = v;
                pb.Current = PoseAt(v);
                pb.CurrentPhase = PhaseAt(v);
                pb.ApplyPose();
                if (pb.CurrentPhase != lastPhase)
                {
                    // 进入第三拍 ⇒ 播 materialize 粒子（只一次）
                    if (pb.CurrentPhase == Phase.Materialize && onMaterialize != null) { var m = onMaterialize; onMaterialize = null; m(); }
                    lastPhase = pb.CurrentPhase;
                }
            }, Total, Total).SetEase(Ease.Linear);

            Component link = newCard != null ? (Component)newCard : (oldCard != null ? (Component)oldCard : null);
            if (link != null) tw = CardTween.Use(tw, Ease.Linear, link);   // 推进方式 + 绑生命周期（对象没了就杀补间）
            else tw.SetUpdate(CardTween.Mode);
            pb.Tween = tw;

            tw.OnComplete(() =>
            {
                pb.ApplyForTest(Total);                 // 终态：新 alpha 1 · extraScale 1
                pb.CurrentPhase = Phase.Done;
                var d = pb.OnDone; pb.OnDone = null;
                if (d != null) d();
                pb.Cleanup();
            });
            tw.OnKill(() => pb.Cleanup());
            return pb;
        }

        /// <summary>自检用：`t` 处「旧督军该不该已经看不见」等判据的辅助 —— 只是 <see cref="PoseAt"/> 的转发。</summary>
        public static float OldAlphaAt(float t) { return PoseAt(t).OldAlpha; }

        /// <summary>自检用：`t` 处白光闪的 alpha。</summary>
        public static float FlashAlphaAt(float t) { return PoseAt(t).FlashAlpha; }

        /// <summary>自检用：`t` 处新督军的 alpha。</summary>
        public static float NewAlphaAt(float t) { return PoseAt(t).NewAlpha; }

        /// <summary>自检用：`t` 处新督军的额外缩放倍数。</summary>
        public static float NewScaleAtForTest(float t) { return PoseAt(t).NewScale; }
    }
}
