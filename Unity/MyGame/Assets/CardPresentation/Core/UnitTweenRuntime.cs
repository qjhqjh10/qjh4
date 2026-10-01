// UnitTweenRuntime.cs — 把 `WarpforgeVFX.WFModuleTween` 的静态钩子接到我们这张补间表上。
//
// 背景：`WFModuleTween.OnInvoke` 是**故意留的空钩子**（那层不认识 `CardPresentation`，
// 也不该认识）。全仓 **138 个 `AnimFXModuleTween` 实例**的补间请求原来全部落在
// `DroppedRequests` 里 —— 「要播但没人接」，**不静默**，但也没人接。
// 这个文件就是那个下游。
//
// 判据（怎么接才对）→ `Core/UnitTweenTable.cs` 的文件头（逐行读的 `UnitTweenSO.BuildSequence`
// + `AnimFXModuleTween.PlayAnimCoroutine` + 六个子类的 `GetTween`）。
//
// 🔴 **契约**（`WFModuleTween.OnInvoke` 的注释定的，别改）：
//   返回值 = 「这条要不要等它播完再播下一条」（原版 `UnitTweenSO.waitAnimation`）；
//   **返回 true 时必须在播完时调 `onFinished`** —— 否则后面的补间永远不播
//   （挂住的状态记在 `WFModuleTween.WaitingCount` 里，**只涨不落就是这里漏了回调**）。
//   ⇒ 我们除了 `OnComplete` 还挂了 `OnKill`（被销毁/杀掉也要回调），并用一个 `done` 闩防双发。
using System;
using DG.Tweening;          // `Sequence.OnComplete/OnKill`（TweenSettingsExtensions）
using UnityEngine;

namespace CardPresentation
{
    public static class UnitTweenRuntime
    {
        /// <summary>钩子挂上了没有（自检盯它）。</summary>
        public static bool Installed { get; private set; }

        /// <summary>接过多少次请求 / 其中多少条要等 / 多少条跳过了。</summary>
        public static int Invoked { get; private set; }
        public static int Waited { get; private set; }
        public static int Skipped { get; private set; }
        /// <summary>最近一次跳过的原因（**出声**用；不许静默）。</summary>
        public static string LastSkip { get; private set; }
        /// <summary>跳过时记过的最后一条效果名（定位用）。</summary>
        public static string LastSkipEffect { get; private set; }

        /// <summary>🆕 **「取督军」解析口**：座位 → 那个督军的 transform。
        /// 由 `BattleDriver` 挂上（只有它知道视图）；对应原版 `GetTargetUnit` 的 40/50 两档。</summary>
        public static Func<int, Transform> HeroBySeat;

        /// <summary>🆕 **「这个 transform 是哪一方的」**：由 `BattleDriver` 挂上（扫一遍自己的视图表）。
        /// 用来判 `targetUnit == 40`（本方的督军）还是 50（对面的督军）。返回 -1 = 认不出。</summary>
        public static Func<Transform, int> SeatOf;

        /// <summary>装钩子。**幂等**（重复调没事）。装完之后 `WFModuleTween` 那条链就活了。</summary>
        public static void Install()
        {
            if (Installed) return;
            WarpforgeVFX.WFModuleTween.OnInvoke = Invoke;
            // 「取朝向」的替身（见 `UnitTweenTable` 文件头 ①）：默认取卡自己的 forward
            UnitTweenTable.HeroOf = ResolveHero;
            Installed = true;
            Debug.Log("[UnitTween] 钩子已挂 —— `AnimFXModuleTween` 的补间请求有人接了"
                    + $"（表里 {UnitTweenTable.Count} 串；"
                    + (UnitTweenTable.Loaded ? "载入正常" : "⚠️ " + UnitTweenTable.LastError) + "）");
        }

        /// <summary>自检用：把计数清零（每段断言前调）。</summary>
        public static void ResetCounters()
        {
            Invoked = Waited = Skipped = 0;
            LastSkip = null; LastSkipEffect = null;
        }

        static Transform ResolveHero(Transform source, bool sameSide)
        {
            if (source == null || HeroBySeat == null || SeatOf == null) return null;
            int seat = SeatOf(source);
            if (seat < 0) return null;
            return HeroBySeat(sameSide ? seat : 1 - seat);
        }

        /// <summary>钩子本体。**返回 true ⇒ 一定会在播完（或被杀）时回调 `onFinished`。**</summary>
        static bool Invoke(WarpforgeVFX.WFModuleTween.TweenRequest req, Action onFinished)
        {
            Invoked++;
            var e = UnitTweenTable.Get(req.asset);
            if (e == null)
            {
                Skip(req, "补间表里没有这个名字 —— 跑一次 `python 工具/gen_unit_tweens.py`"
                        + (UnitTweenTable.Loaded ? "" : $"（表根本没载进来：{UnitTweenTable.LastError}）"));
                return false;
            }
            if (req.actor == null && req.target == null)
            {
                // ⚠️ **不许静默**：这条补间本来该动点什么，但我们不知道动谁。
                //    调用方要传 `WarpforgeEffectPlayer.Play(..., actor: 卡, target: 卡)`。
                Skip(req, "调用方**没带源/目标卡**（`Play(...)` 的 actor/target 都是 null）"
                        + " ⇒ 补间定位不了「该动谁」");
                return false;
            }

            var seq = UnitTweenTable.Build(e, req.actor, req.target, req.effect);
            if (seq == null) { Skip(req, UnitTweenTable.LastSkip); return false; }

            if (e.wait == 0) return false;            // 原版：不等 ⇒ 直接接着下一条

            Waited++;
            bool done = false;
            Action fire = () => { if (done) return; done = true; if (onFinished != null) onFinished(); };
            seq.OnComplete(() => fire());
            seq.OnKill(() => fire());                 // 被销毁/杀掉也算「结束了」，否则 WaitingCount 只涨不落
            return true;
        }

        static void Skip(WarpforgeVFX.WFModuleTween.TweenRequest req, string why)
        {
            Skipped++;
            LastSkip = why; LastSkipEffect = req.effect;
            Debug.LogWarning($"[UnitTween] 跳过「{req.asset}」（效果 `{req.effect}`，第 {req.index}/{req.count} 条）：{why}");
        }
    }
}
