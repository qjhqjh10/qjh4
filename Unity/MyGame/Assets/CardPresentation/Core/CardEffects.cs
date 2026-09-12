// CardEffects.cs — 卡牌表现 ←→ 特效库 的连接点
//
// 设计上这两边要**解耦**（见 `自研游戏_特效与卡牌基座_设计.md` 2.5）：
// 卡牌代码不该直接 new 一个特效播放器，否则换特效要改卡牌、换布局要碰特效。
// 所以这里只留一个钩子：游戏启动时把它接到 `WarpforgeEffectPlayer.Play` 上。
//
// 好处：
//   · CardPresentation 不依赖 WarpforgeVFX —— 特效库不在（或还没装）时，卡牌照样能跑
//   · 批处理自检里可以挂一个假的钩子，只记录「谁在什么时候播了什么」，不真渲特效
using System;
using UnityEngine;

namespace CardPresentation
{
    public static class CardEffects
    {
        /// <summary>参数：效果名 / 世界坐标 / 父节点（可为 null）</summary>
        public static Action<string, Vector3, Transform> Play;

        /// <summary>播一个特效（按名字）。没接钩子就只记一行日志 —— 绝不因为「特效没配好」把卡牌流程打断。</summary>
        public static void Fire(string effectName, Vector3 worldPos, Transform parent = null)
        {
            if (string.IsNullOrEmpty(effectName)) return;
            if (Play != null) Play(effectName, worldPos, parent);
            else Debug.Log($"[CardPresentation] （未接特效钩子）{effectName} @ {worldPos}");
        }

        /// <summary>
        /// 播一个**事件**：名字由 `VfxMap` 按「事件 + 阵营 + 卡」查出来。
        /// 卡牌那边只说「我登场了」「我挨打了」，具体播哪个特效是 `VfxMap` 的事。
        /// </summary>
        public static void FireEvent(string evt, Vector3 worldPos,
                                     string faction = null, string cardId = null, Transform parent = null)
        {
            Fire(VfxMap.Resolve(evt, faction, cardId), worldPos, parent);
        }

        // ---- 老接口（按事件走的那套之前是硬编码三个名字，现在转发到 VfxMap）----
        public static string EffectDeploy { get { return VfxMap.Resolve(VfxMap.Deploy); } }
        public static string EffectAttack { get { return VfxMap.Resolve(VfxMap.AttackMelee); } }
        public static string EffectDamage { get { return VfxMap.Resolve(VfxMap.Hit); } }
    }
}
