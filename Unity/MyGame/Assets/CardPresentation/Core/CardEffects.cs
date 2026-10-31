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
        /// ⚠️ 钩子只认单点 ⇒ 这里是**按点各调一次**钩子（`PlayInstance` 优先，没接时才走 `Play`）；顺序 = 数组顺序（调用方可依赖它）。</summary>
        public static void Fire(string effectName, Vector3[] points, Transform parent = null)
        {
            // 🆕 2026-10-31（`B107_W24` · 特效线 `FX-04`）：落点规则**只留一处** ——
            //   本方法转发给 <see cref="FireGet(string,Vector3[],Transform)"/>（唯一的那份循环在那里），
            //   实例结果丢弃。语义与改前逐字相同（没接钩子时照样只记一行日志、空数组照样出声）。
            FireGet(effectName, points, parent);
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

        // ==================================================================
        //  🆕 2026-10-31（`B107_W24` · 特效线 `FX-04`）：**可拿实例**的播放口
        // ==================================================================
        //  【为什么加】原版「播一件特效」的那几个入口**都返回实例**（第一权威，逐句读过）：
        //    · `CardScript.PlayTriggerAnim(GameObject animPrefab, CardScript targetCard, int traitValue = 0)`
        //      —— 返回类型 `GameObject`；`decomp_full/CardScript__PlayTriggerAnim.c:33-50`：
        //      `Instantiate(prefab, cardUI.effectsAnchor(+0x1a0))` → `GetComponent<AnimFXController>().SetData(...)`
        //      → `return lVar3`（= 新实例）。签名桩：`Warpforge_code/.../CardScript.cs:2794`。
        //    · `BattleManager.CreateAnim(...)` —— 返回类型 `CardAnimController`；
        //      `decomp_full/BattleManager__CreateAnim.c:62-82`：`Instantiate(prefab, startParent)` →
        //      `AddComponent<CardAnimController>()` → `Initialize(...)` → `return lVar4`。
        //      `CardScript.CardSetup` **真的用了它**（`CardScript__CardSetup.c:312-317`：拿返回值
        //      `GetComponent<Transform>()` 再 `set_parent(..., this.toBoardVFXAnchor(+0x188))`）。
        //  ⇒ 我们这边 `Play` 是 `Action<...>`、`Fire` 全 `void`，**形状上与原版不一致**（调用方拿不到句柄）。
        //
        //  【⚠️ 但**不要**据此把清理改成「调用方登记 + DoDestroy」】—— 那是**被推翻过的旧方案**：
        //    · 原版 `AnimFXController.DoDestroy` 的**唯一调用点** = `BattleManager.RemoveDestroyableAnims`
        //      （`decomp_full/BattleManager__RemoveDestroyableAnims.c:45/80`），它只操作
        //      `BattleCardUI.TraitFrameEffectsPlay` / `TraitFrameEffectsTrigger` 这两本**逐 trait 字典**
        //      （`dump.cs` `BattleCardUI` 字段 `+0x2D0` / `+0x2D8`，`buffType ∈ {15,16}` 那一档）——
        //      即**状态框**，不是逐卡一次性特效。
        //    · 原版 `Exit` 的唯一调用点 = 状态动画摘除（`BattleCardUI__RemoveStatusAnim.c:62/96` ·
        //      `BattleCardUI__CleanStatusAnims.c`）。
        //    · `preventDestroy = 1` 的一次性特效，原版**从不**给它们排销毁、也**没有**任何调用方拿返回值
        //      去登记 —— 它们靠「实例化在卡的 `effectsAnchor` 下（`CardScript__PlayTriggerAnim.c:29-33`）、
        //      卡离场 ⇒ 整棵子树一起没」收场（`AnimFXController__OnEnable.c`：`destroyTime > 0 && !preventDestroy`
        //      才 `Destroy(go, destroyTime)`）。
        //  ⇒ **等价物 = 我们的 `WarpforgeEffectPlayer._ownerWatch`**（2026-10-25 D 域已落，见
        //    `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:129-152/322/499-514`）——
        //    ⛔ 别把这条路改成 `DoDestroy()`（有模块的实例 `DoDestroy` 不销毁，换了等于没修；
        //    判据 → `报告/D_特效动画_第1轮.md` §⑨·3）。
        //
        //  【这两个口解决什么】`parent != null` 那一路本来就能靠 `childCount` 差认出实例（原 `Fire` 的
        //  `StampNewChildren` 就是这么找的）；**`parent == null` 的根特效**认不出 —— 新钩子
        //  <see cref="PlayInstance"/> 补上那一格。⛔ 没接它时行为与改前逐字相同（返回 null、不报错）。
        // ==================================================================

        /// <summary>🆕 2026-10-31（`FX-04`）：与 <see cref="Play"/> 同参数的**可选**钩子，但**返回新建实例的根**。
        ///
        /// <para>接法（一行，装在特效库那一侧 —— 依赖方向不变：`CardPresentation` 仍然不认识 `WarpforgeVFX`）：
        /// `CardEffects.PlayInstance = (name, pos, parent) => { …; return fx.gameObject; };`
        /// 可粘贴补丁见 `资料/完全复刻/报告/B107_W24.md` §四。</para>
        ///
        /// <para>⚠️ **两个钩子同时接时本钩子优先**（`FireOne` 里是 if/else）—— 别让它和 `Play` 各实例化一次。</para>
        /// <para>⚠️ 自检里把 `Play` 换成假钩子的既有用例**不受影响**：本字段是 null 时仍走老路
        /// （`Play` + `childCount` 差）。</para>
        /// <para>⚠️ 返回 null = 「这次没建出实例」（名字取不到 / 库没装 / 假钩子）—— 调用方**自己判**，
        /// 这里不额外出声（`WarpforgeEffectPlayer.Play` 已经在失败处打过警告）。</para></summary>
        public static Func<string, Vector3, Transform, GameObject> PlayInstance;

        /// <summary>空结果。⚠️ 共用一份只读数组 —— 调用方⛔ 别改元素。</summary>
        static readonly GameObject[] NoInstances = new GameObject[0];

        /// <summary>🆕 2026-10-31（`FX-04`）：单点版（语义见 <see cref="FireGet(string,Vector3[],Transform)"/>）。</summary>
        public static GameObject FireGet(string effectName, Vector3 worldPos, Transform parent = null)
        {
            var made = FireGet(effectName, new[] { worldPos }, parent);
            return (made != null && made.Length > 0) ? made[0] : null;
        }

        /// <summary>🆕 2026-10-31（`FX-04`）：**与 <see cref="Fire(string,Vector3[],Transform)"/> 逐字同规则**，
        /// 但把每一拍新生成的实例根返回给调用方（诊断 / 需要按实例收场时用）。
        ///
        /// <para>落点规则与出声口径**只此一处**（`Fire` 转发到这里）：
        /// 空名字静默返回 · 空数组**出声**并返回空 · 每个点各播一次（顺序 = 数组顺序）。</para>
        /// <para>返回值长度 == `points.Length`；某一位是 null = 那个点没建出实例（见 <see cref="PlayInstance"/>）。</para>
        /// <para>⚠️ 拿到实例**不等于**「要由调用方销毁」—— 清理语义见本节头部那段（原版靠
        /// `preventDestroy` + 父件消失，我们靠 `_ownerWatch`）。</para></summary>
        public static GameObject[] FireGet(string effectName, Vector3[] points, Transform parent = null)
        {
            if (string.IsNullOrEmpty(effectName)) return NoInstances;
            if (points == null || points.Length == 0)
            {
                Debug.LogWarning($"[CardPresentation] `{effectName}` 给了一个空的落点数组 ⇒ 不播"
                               + "（⛔ 不是原版有这件效果、而是调用方没给出落点）。");
                return NoInstances;
            }
            var made = new GameObject[points.Length];
            for (int i = 0; i < points.Length; i++)
                made[i] = FireOne(effectName, points[i], parent);
            return made;
        }

        /// <summary>🆕 2026-10-31（`FX-04`）：事件版的单点（名字由 `VfxMap` 查，语义同 <see cref="FireGet(string,Vector3,Transform)"/>）。</summary>
        public static GameObject FireEventGet(string evt, Vector3 worldPos,
                                              string faction = null, string cardId = null, Transform parent = null)
        {
            return FireGet(VfxMap.Resolve(evt, faction, cardId), worldPos, parent);
        }

        /// <summary>🆕 2026-10-31（`FX-04`）：事件版的多落点（语义同 <see cref="FireGet(string,Vector3[],Transform)"/>）。</summary>
        public static GameObject[] FireEventGet(string evt, Vector3[] points,
                                                string faction = null, string cardId = null, Transform parent = null)
        {
            return FireGet(VfxMap.Resolve(evt, faction, cardId), points, parent);
        }

        /// <summary>`FireGet` 的单拍实现 —— **唯一**的「调钩子 / 找新实例 / 排队」那一跳。
        /// <para>找实例的两条路：接了就问 <see cref="PlayInstance"/>；否则按 `childCount` 差取
        /// `parent.GetChild(before)`（与 2026-10-23 `F4` 的记账口径同一个前提：`Play` 是同步
        /// `Instantiate(prefab, parent)`，新件追加在末尾）。`parent == null` 且没接新钩子 ⇒ 认不出，返回 null。</para>
        /// <para>⚠️ `StampNewChildren` **无论哪条路都要跑**（原 `Fire` 就是无条件跑的）—— 少跑会让
        /// 挂在卡上的特效丢掉排序段（`CardView.StampEffectRoot`）。</para></summary>
        static GameObject FireOne(string effectName, Vector3 p, Transform parent)
        {
            int before = (parent != null) ? parent.childCount : 0;
            GameObject made = null;
            if (PlayInstance != null)
            {
                made = PlayInstance(effectName, p, parent);
            }
            else if (Play != null)
            {
                Play(effectName, p, parent);
                // 同步 Instantiate ⇒ 调用前后 `childCount` 的差就是这一拍新生成的实例（末尾那一个）。
                if (parent != null && parent.childCount > before)
                    made = parent.GetChild(before).gameObject;
            }
            else Debug.Log($"[CardPresentation] （未接特效钩子）{effectName} @ {p}");
            if (parent != null) StampNewChildren(parent, before);
            return made;
        }
    }
}
