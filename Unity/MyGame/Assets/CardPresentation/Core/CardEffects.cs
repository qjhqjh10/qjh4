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
            Fire(effectName, new[] { worldPos }, parent);
        }

        /// <summary>🆕 2026-10-14（`A1884`）**多落点**：同一次事件要在**好几个点各播一份**。
        ///
        /// 为什么需要（判据 → `资料/普查产出_第二十八会话/W12_伏击面朝下与材质过渡.md` §A1865）：
        /// 原版抽卡那件特效（`UM_CardDraw` / `SAU_CardDraw`）**自己会飞**
        /// （`card_anim_map.json` 实读 `shouldMoveVFX = 1` · `timeMoving = 2.5`），
        /// 而 `Play` 钩子只能把它摆到**一个**点上 ⇒ 只吃一个点的签名**表达不出**「从这儿到那儿」。
        /// 这个重载就是那个缺口：**给 N 个点就在 N 个点各播一份**（原版那一件的对应物）。
        ///
        /// ⚠️ 一个点都不给（`null` / 空数组）⇒ **出声**并**什么都不播** ——
        ///    静默吞掉比播错更糟（红线：不许静默失败）。
        /// ⚠️ 钩子只认单点 ⇒ 这里是**按点各调一次** `Play`；顺序 = 数组顺序（调用方可依赖它）。</summary>
        public static void Fire(string effectName, Vector3[] points, Transform parent = null)
        {
            if (string.IsNullOrEmpty(effectName)) return;
            if (points == null || points.Length == 0)
            {
                Debug.LogWarning($"[CardPresentation] `{effectName}` 给了一个空的落点数组 ⇒ 不播"
                               + "（⛔ 不是原版有这件效果、而是调用方没给出落点）。");
                return;
            }
            for (int i = 0; i < points.Length; i++)
            {
                var p = points[i];
                // 🆕 2026-10-23（D · `F4`）：**特效要跟着它挂的那张卡排队**（判据见
                //   `CardView.StampEffectRoot` 的那一大段）。`Play` 是同步的 ——
                //   `WarpforgeEffectPlayer.Play` 里 `Instantiate(prefab, parent)` 之后立刻返回
                //   ⇒ 调用前后 `parent.childCount` 的差就是**这一拍新生成的实例**。
                int before = (parent != null) ? parent.childCount : 0;
                if (Play != null) Play(effectName, p, parent);
                else Debug.Log($"[CardPresentation] （未接特效钩子）{effectName} @ {p}");
                if (parent != null) StampNewChildren(parent, before);
            }
        }

        /// <summary>`parent` 下从 `from` 开始新出现的子节点 = 刚生成的特效实例 ⇒ 交给
        /// <see cref="CardView.StampEffectRoot"/> 挂到那张卡的排序段上。
        /// <para>⚠️ 父链上**没有** `CardView`（世界特效 / 棋盘特效 / 特效挂在别的载体上）⇒ 什么都不做 ——
        /// 那些件的排序是另一笔账（`D` 报告 §④·6）。</para>
        /// <para>⚠️ 自检里把 `Play` 换成假钩子时它不建子节点 ⇒ 这里天然是空操作。</para></summary>
        static void StampNewChildren(Transform parent, int from)
        {
            if (parent == null || parent.childCount <= from) return;
            var cv = CardView.OwnerOf(parent);
            if (cv == null) return;
            for (int i = from; i < parent.childCount; i++) cv.StampEffectRoot(parent.GetChild(i));
        }

        /// <summary>
        /// 播一个**事件**：名字由 `VfxMap` 按「事件 + 阵营 + 卡」查出来。
        /// 卡牌那边只说「我登场了」「我挨打了」，具体播哪个特效是 `VfxMap` 的事。
        /// ⚠️ 单点版**转发**给数组版（落点规则只留一处，见上面那个重载的 doc）—— 别在这儿另写一遍循环。
        /// </summary>
        public static void FireEvent(string evt, Vector3 worldPos,
                                     string faction = null, string cardId = null, Transform parent = null)
        {
            Fire(VfxMap.Resolve(evt, faction, cardId), worldPos, parent);
        }

        /// <summary>🆕 2026-10-14（`A1884`）：多落点版（语义见 <see cref="Fire(string,Vector3[],Transform)"/>）。</summary>
        public static void FireEvent(string evt, Vector3[] points,
                                     string faction = null, string cardId = null, Transform parent = null)
        {
            Fire(VfxMap.Resolve(evt, faction, cardId), points, parent);
        }

        // ---- 老接口（按事件走的那套之前是硬编码三个名字，现在转发到 VfxMap）----
        public static string EffectDeploy { get { return VfxMap.Resolve(VfxMap.Deploy); } }
        public static string EffectAttack { get { return VfxMap.Resolve(VfxMap.AttackMelee); } }
        public static string EffectDamage { get { return VfxMap.Resolve(VfxMap.Hit); } }
    }
}
