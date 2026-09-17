// WFModuleCardback.cs — 原版 `AnimFXModuleCardback` 的对应物
//   **30 实例 / 18 效果**（效果全在 `CreateCard*` 家族 + `DeckBuff_MoveToTop` /
//    `Sau_ReconstitutionProtocol` / `Wild Rider Chieftain`）
//
// 语义出处（**逐方法体**，不是按名字猜）
// ----------------------------------------------------------------
//   · `资料/AnimFX_18类方法体_块2.md` §1（读解）
//   · 方法体原件：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/`
//     `AnimFXModuleCardback__Initialize.c` · `AnimFXModuleCardback__SetParticleMaterial.c`
//     🔴 两者是**同一段逻辑** —— `SetParticleMaterial` 被内联进了 `Initialize`
//        （块2 §1 记着这一点），我按 `Initialize.c` 的逐行来。
//   · 报错原文逐字核过字符串表 `d:/2/tools/all_strings.txt`
//     （`:17688` `[ERROR] Can't find cardback for FX ` · `:17769` `[ERROR] Can't find particle system to assign card frame`）
//
// 照方法体还原了什么
// ----------------------------------------------------------------
//   ① `sprite = BattleManager.GetCardback(actingCard.isPlayer).Item1`（**只看 Item1**）
//   ② `sprite == null` → `LogError("[ERROR] Can't find cardback for FX " + 组件名)` 然后 **return**
//      —— **一个粒子都不改**（`__Initialize.c:44-49`）
//   ③ `particleSystemsToAddCardback` 为 null 或长度 0 → `LogError("[ERROR] Can't find particle
//      system to assign card frame")`，**不抛异常、直接返回**（`:57-59`）
//   ④ 否则逐个粒子系统：`tsa.enabled = true` · `tsa.mode = Sprites` · `tsa.AddSprite(sprite)`
//      —— **不 clear 原有 sprite**（原版也没 clear，见 `__Initialize.c:71-74`），只是追加
//   ⑤ 数组长度**不设上限**（`CreateCard GSC 5x` 是 5 个），逐个 `ResolveNode` 拿
//   ⑥ `actionStart` 这类**不读**（原版这一类没读它，30/30 都是 0=Initialize）
//
// 🔴 我们自己定的（**原版不是这样，两处**）
// ----------------------------------------------------------------
//   1. **卡背从哪来**：原版是**玩家档案里的装饰品** ——
//      `BattleManager.GetCardback(isPlayer)` → `PlayerDataManager.GetCosmeticItem(...)` →
//      `CosmeticItemCardback.GetCardBackSprites().Item1`（块2 §1）。**我们单机没有档案**，
//      换成静态回调 `CardbackResolver(isPlayer)`（挂法见下），形状与原版一致（仍带 `isPlayer`）。
//      · 回调**没挂 / 返回 null** 时，按原版「拿不到卡背」处理：LogError + **不动粒子**（不静默）。
//      · ⚠️ **工程里没有卡背 sprite**：`CardPresentation/Resources/` 下 0 命中；
//        233 张卡背 PNG 在**工程外**的 `素材/Warpforge原版/卡背/`（`Cardback_*_Main.png`）。
//        要接就得导一张进 `Resources/` 再挂回调 —— **这一步是我们的选择**，
//        与 `资料/敌方手牌_原版规格.md:146` 记的「我们固定用一张」一致。
//      · `Item1` / `Item2` 的区别（正/背？普通/金？）**查不到** ——
//        `CosmeticItemCardback.GetCardBackSprites` 没被反编译（块2 §1 末）。我们只取一张。
//   2. 手挂的模块可以直接在 Inspector 里给 `cardback`（运行时装配那条路走回调）——
//      原版没有这个字段（原版一律走档案）。
//
// 🔴 没还原的（**导出侧的真缺口**，不是本文件能补的）
// ----------------------------------------------------------------
//   原版 prefab 的 `textureSheetAnimation` 里**本来就有 sprite**；我们的导出器
//   **只导 `SpriteRenderer` / `SpriteMask` 的 sprite，没导 `tsa` 的 sprite 列表**
//   ⇒ 导出后那些槽是**死引用**。实测（2026-09-18 亲读）
//   `WarpforgeVFX/Prefabs/CreateCard DA.prefab:1547` 就是 `sprites:` + `- sprite: {fileID: 0}`。
//   ⇒ 我们 `AddSprite` 是**追加到一条死引用后面**（照原版行为，不 clear）；
//   **卡背到底显示不显示，要拿原版 `CreateCard*` 对着比一次**
//   （出处：`资料/特效还原_进度与交接.md` §〇之三 ④ 的同一条待办）。
//   ⇒ 下面在**能画出卡背时**打一条 `LogWarning`（只报一次），把这件事说出来。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleCardback")]
    public class WFModuleCardback : WFEffectModule
    {
        /// <summary>原版 `particleSystemsToAddCardback`（`@node:ParticleSystem:CreateCard/Card`）。
        /// 运行时装配时由 `Configure` 填；手挂时在 Inspector 里填路径或直接改 `targets`。</summary>
        [Tooltip("原版 particleSystemsToAddCardback 的引用值（@node:ParticleSystem:…）")]
        public string[] particleSystemsToAddCardback = new string[0];

        /// <summary>手挂用：直接给粒子系统（运行时装配那条路走 `particleSystemsToAddCardback`）。</summary>
        [Tooltip("手挂用：直接拖粒子系统进来。留空 = 按上面的引用值解析")]
        public ParticleSystem[] targets = new ParticleSystem[0];

        /// <summary>手挂用：直接给一张卡背。留空 = 问 `CardbackResolver`。</summary>
        [Tooltip("手挂用：直接给一张卡背。留空 = 问 CardbackResolver（运行时装配那条路只能走它）")]
        public Sprite cardback;

        /// <summary>🔑 **下游钩子**：原版 `BattleManager.GetCardback(bool isPlayer)` 的对应物。
        /// 参数 = `actingCard.isPlayer`；返回 null 按「拿不到卡背」处理（LogError + 不动粒子）。
        /// 我们单机固定用一张卡背也可以，但**形状保留 isPlayer**，和原版一致。</summary>
        public static Func<bool, Sprite> CardbackResolver;

        // ---- 诊断（「不许静默失败」）：每一条都能在自检里断言 ----
        /// <summary>拿不到卡背的次数（原版 = `LogError` 那条路）。</summary>
        public static int MissingCardback;
        /// <summary>粒子系统引用解析失败的次数（`ResolveNode` 自己已打警告，这里只是计数）。</summary>
        public static int ResolveFailedNodes;
        /// <summary>数组为空的实例数（原版 = `Can't find particle system…` 那条路）。</summary>
        public static int EmptyArrayCount;

        public static void ResetDiagnostics()
        {
            MissingCardback = 0; ResolveFailedNodes = 0; EmptyArrayCount = 0; _warnedTsaGap = false;
        }

        static bool _warnedTsaGap;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            if (def == null) return;
            var list = def.GetList("particleSystemsToAddCardback");
            particleSystemsToAddCardback = list.ToArray();
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);

            // ③ 数组为空 → 报错后**直接返回**（原版：不抛异常）
            bool hasInline = targets != null && targets.Length > 0;
            if (!hasInline && (particleSystemsToAddCardback == null || particleSystemsToAddCardback.Length == 0))
            {
                EmptyArrayCount++;
                Debug.LogError("[WarpforgeVFX] [ERROR] Can't find particle system to assign card frame"
                             + $"（效果 {EffectName} / 节点 {gameObject.name}）"
                             + " —— 原版这一条也是 LogError 后直接返回，粒子一个都不改。");
                return;
            }

            // ① 卡背
            var sp = ResolveCardback();
            if (sp == null)
            {
                // ② 原版：LogError + return（粒子一个都不改）
                MissingCardback++;
                Debug.LogError("[WarpforgeVFX] [ERROR] Can't find cardback for FX " + gameObject.name
                             + $"（效果 {EffectName}）"
                             + " —— 原版从这里取的是**玩家档案里的卡背装饰品**，我们单机没有档案 ⇒ "
                             + "要挂 `WFModuleCardback.CardbackResolver`（或者手挂时在 Inspector 里给 "
                             + "`cardback`）。⚠️ 卡背 PNG 在工程外：`素材/Warpforge原版/卡背/`。"
                             + $"累计 {MissingCardback} 次。");
                return;
            }

            // ④ 逐个粒子系统：enabled = true · mode = Sprites · AddSprite（不 clear）
            if (hasInline)
            {
                for (int i = 0; i < targets.Length; i++) Apply(targets[i], sp);
            }
            else
            {
                for (int i = 0; i < particleSystemsToAddCardback.Length; i++)
                {
                    var ps = ResolveNode<ParticleSystem>(transform, particleSystemsToAddCardback[i], name);
                    if (ps == null) { ResolveFailedNodes++; continue; }
                    Apply(ps, sp);
                }
            }

            if (!_warnedTsaGap)
            {
                _warnedTsaGap = true;
                Debug.LogWarning("[WarpforgeVFX] 卡背已 AddSprite，但 `textureSheetAnimation` 的 sprite 列表"
                               + "**在导出侧是死的**（导出器只导 SpriteRenderer/SpriteMask 的 sprite，"
                               + "没导 tsa 的列表 ⇒ 实测 `Prefabs/CreateCard DA.prefab:1547` 是 "
                               + "`- sprite: {fileID: 0}`）。所以卡背**可能显示不出来** —— "
                               + "要拿原版 `CreateCard*` 对着比一次（见 `资料/特效还原_进度与交接.md` "
                               + "§〇之三 ④）。这条警告只报一次。");
            }
        }

        static void Apply(ParticleSystem ps, Sprite sprite)
        {
            if (ps == null) return;
            var tsa = ps.textureSheetAnimation;   // 结构体：属性写回是有效的（Unity 的既定用法）
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Sprites;
            tsa.AddSprite(sprite);
        }

        Sprite ResolveCardback()
        {
            if (cardback != null) return cardback;
            if (CardbackResolver == null) return null;

            // 原版的实参是 `actingCard.isPlayer`。我们这边的卡上下文是可选的
            // （定义在 `WFModuleTransformModifier.cs` 的 `WFEffectCards`）——
            // **卡背本身是固定的一张**，所以拿不到上下文不报警（warn:false），按 false 走。
            bool isPlayer = false;
            WFEffectCardContext ctx;
            if (WFEffectCards.TryGet(Controller, out ctx, false)) isPlayer = ctx.actingIsPlayer;
            return CardbackResolver(isPlayer);
        }
    }
}
