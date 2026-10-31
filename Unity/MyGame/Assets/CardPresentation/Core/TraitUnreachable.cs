// TraitUnreachable.cs — 「trait 触发框在本 build 不可达」的登记表（纯静态表 + 读口）
//
// ---- 它是什么 ----------------------------------------------------------------
// 一张**静态、确定性排序、零副作用**的表：逐条登记「`TraitEffect.TriggerType ∈ {3,6}` 的行确实存在，
// 但在本 build 里 `BattleCardUI.DisplayTriggerAnim(trait, …)` **永远收不到这个 trait**」的那几个 trait，
// 每条带「不可达类别 / 判据出处 / 为什么不可达 / 时间限定」。
//
// ---- 为什么在这里 ------------------------------------------------------------
// 特效线 `AN1-1`（施工单 → `资料/完全复刻/判据/B107_特效线施工单_v2.md` §2；源头 = `AN1` R9③）要求
// 「把 `regeneration` 及同形状 trait 逐条登记进不可达清单，含逐条为什么不可达，做成自检可读的表」。
// 在此之前全仓**只有一句注释**点名过 `regeneration`（`Core/VfxMap.cs:2648` 的错件清单里），没有成表。
//
// ---- 判据（每条登记都同时命中两条腿）----------------------------------------
//   ① **原版 `AbilityTrigger` 枚举里没有能 name-parse 到它的成员**。
//      映射函数逐句：`d:/2/tools/decomp_full/CardTraitCollection__GetTraitFromAbilityTrigger.c` ——
//      特例 `0 → 10 rally` / `0x50(80) → 0x78(120) backlash` / `0x51(81) → 0x3f2(1010) unstable`（`:19-31`）；
//      其余 = 把 `AbilityTrigger` 名转 **lowerCamel** 后 `Enum.TryParse<DefinedTrait>`（`:32-60`），
//      解不出就返回 0 ⇒ `CardScript__ActivateTriggerTraitAnim.c:8-10` 直接 return ⇒ 框不播。
//      第二份独立判据（不用推）：`CardTraitCollection__GetTraitsFromAbilitiesRawCard.c:28-53` 的 25 个
//      硬编码 `(DefinedTrait << 32 | AbilityTrigger)` 常量，解码后与 name-parse 表逐条一致。
//   ② **`DisplayTriggerAnim` 的全部 18 处调用点里，没有一处会传这个 trait 常量**。
//      调用点全表（`d:/2/tools/decomp_full` 全树 grep，2026-10-31 现读）：
//        常量 12 处：`BattleManager__EndTurnClick.c:83`(0x168 berzerk) · `BattleManager__AddBerzerkAttack.c:29`(0x168) ·
//          `CardScript__HighlightBloodThirst.c:8`(0xdc bloodThirst) · `CardScript__ReceiveSneakAttackAnim.c:15`(0x14 sneakAttack) ·
//          `CardScript__UseSurvivor.c:46`(0x1ae survivor) · `CardScript__TriggerSacrifice.c:19`(0x1d6 sacrifice) ·
//          `CardScript__Stun.c:142`(0xfa unstunnable) · `CardScript__ReactToUnitJammed.c:181`(0x82 jam) ·
//          `BattleManager._ResolveAttack_d__438__MoveNext.c:952`(0x208 terror) · `…:1034`(0x15e blast) · `…:1095`(0x4d8 stomp) ·
//          `CardScript._ReceiveDamage_d__381__MoveNext.c:295`(0x1e0 vulnerable) · `CardScript__AddEffect.c:641`(0x4ec huntMark)；
//        动态 2 处：`CardScript__ActivateTriggerTraitAnim.c:14` 与 `CardScript__ActivateIconAnim.c:11`
//          （都吃 `GetTraitFromAbilityTrigger` 的结果 ⇒ 回到腿 ①）。
//      ⚠️ **② 必须按 Ghidra `.c` 数，⛔ 别按 ILSpy 数**：ILSpy 会丢异步状态机体
//      （`_ResolveAttack_d__438__MoveNext` 那几个），只按 ILSpy 数会漏掉 `stomp`(0x4d8)/`huntMark`(0x4ec)
//      这类**其实可达**的 ⇒ 会把可达误登记成不可达（见报告 `资料/完全复刻/报告/B107_W9.md` §二）。
//
// ---- 时间限定 ----------------------------------------------------------------
// 本 build = **2025-07-30 资源快照 + 安卓 1.35.0 远端包**（`D:/2/关服前存档/…/1.35.0`）。
// ⚠️ 换 build 后**必须重核本表**：两条判据（枚举里没有那个名字 / 调用点没有那个常量）都会随版本变。
// ⚠️ 本表只回答「原版能不能播」，**不回答「我们的引擎该不该触发」** ——
//    ⛔ 不许拿它当「按名字把 trait 接上」的理由（那会造出原版没有的触发，`AN1` R9 红线）。
//
// 🔶 自己设计（非原版）：这张「不可达清单」是我们整理的登记表，原版没有对应物 ·
//    为什么必须自己定：原版只给出枚举与调用点，没有把「谁永远播不出」写成表；
//    判据全部来自原版可读证据（`AbilityTrigger` 枚举 + `d:/2/tools/decomp_full` 的调用点）·
//    调度台 2026-10-31 裁定（执行代理 `B107_W9`）。
using System;
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>「trait 触发框在本 build 不可达」的登记表 + 读口。
    /// 判据与时间限定见本文件头；本类**不写日志、不挂 Awake、无任何运行时副作用**。</summary>
    public static class TraitUnreachable
    {
        /// <summary>本表适用的 build。换 build 必须重核（判据是「枚举里没有 / 调用点没有」，两者都会随版本变）。</summary>
        public const string BuildScope = "2025-07-30 资源快照 + 安卓 1.35.0 远端包";

        /// <summary>不可达类别：本 build 里没有任何 `AbilityTrigger` 能 name-parse 到它，且无直接调用点。</summary>
        public const string KindNoAbilityTrigger = "no-ability-trigger";

        /// <summary>不可达类别：同上，**并且**该 trait 的 `effectAnim` 是空引用（即使被叫到也没有动画可播）。</summary>
        public const string KindNoAbilityTriggerAndEmptyEffect = "no-ability-trigger+empty-effect";

        /// <summary>一条登记。</summary>
        public readonly struct Entry
        {
            /// <summary>trait 名（`DefinedTrait` 成员的 lowerCamel 写法，与 `trait_effect_bindings.json` 的 `trait` 列同口径）。</summary>
            public readonly string Trait;
            /// <summary>原版 `DefinedTrait` 枚举值。</summary>
            public readonly int DefinedTrait;
            /// <summary>不可达类别，见 <see cref="KindNoAbilityTrigger"/> 等常量。</summary>
            public readonly string Kind;
            /// <summary>判据出处（`文件:行` 或 反编译路径），逐条可复查。</summary>
            public readonly string Evidence;
            /// <summary>为什么不可达（一句话说死机制）。</summary>
            public readonly string Why;
            /// <summary>时间限定（= <see cref="BuildScope"/>）。</summary>
            public readonly string Scope;

            public Entry(string trait, int definedTrait, string kind, string evidence, string why, string scope)
            {
                Trait = trait;
                DefinedTrait = definedTrait;
                Kind = kind;
                Evidence = evidence;
                Why = why;
                Scope = scope;
            }

            public override string ToString()
            {
                return Trait + "(" + DefinedTrait + ") [" + Kind + "] " + Why;
            }
        }

        // 确定性排序 = 按 DefinedTrait 升序（680 < 930 < 940 < 950 < 1030）。
        // ⛔ 别改成按名字排：名字大小写/别名将来会变，id 是原版枚举值，稳定。
        static readonly Entry[] _all =
        {
            new Entry(
                "frenzied", 680, KindNoAbilityTrigger,
                "`AbilityTrigger` 枚举 97 条里没有 `Frenzied`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs` 全表）· " +
                "`DisplayTriggerAnim` 18 处调用点无一传 0x2a8(680)（`d:/2/tools/decomp_full`）· " +
                "代码里唯一引用 = `out_cpp2il_dev_ilspy/BattleManager.cs:45815` 的 `HasCurrentTrait(DefinedTrait.frenzied)`（攻击判定，不播框）",
                "name-parse 永远解不出 680 ⇒ `GetTraitFromAbilityTrigger` 返回 0 ⇒ " +
                "`CardScript__ActivateTriggerTraitAnim.c:8-10` 直接 return ⇒ 触发框永远不播",
                BuildScope),

            new Entry(
                "concussive", 930, KindNoAbilityTrigger,
                "`AbilityTrigger` 枚举里没有 `Concussive` · `DefinedTrait.concussive` 在 `out_cpp2il_dev_ilspy` 全树 0 命中（只有枚举定义本身）· " +
                "`DisplayTriggerAnim` 18 处调用点无一传 0x3a2(930) · " +
                "现读池内 **6 张**卡带 `concussive` trait（`RuleEngine/Resources/cards_engine.json` 的 `cardTraits`，逐卡现算，⛔ 别写死张数）",
                "name-parse 永远解不出 930 ⇒ 同 frenzied 那条链，触发框永远不播（有卡也播不出）",
                BuildScope),

            new Entry(
                "large", 940, KindNoAbilityTriggerAndEmptyEffect,
                "`AbilityTrigger` 枚举里没有 `Large` · 代码 0 引用（`out_cpp2il_dev_ilspy` 全树）· " +
                "`DisplayTriggerAnim` 18 处调用点无一传 0x3ac(940) · " +
                "该行 `effectAnim` 是空引用（`Assets/CardPresentation/Resources/trait_effect_bindings.json` 的 `unresolved` 列，原版这条 effect 本来就没有动画）",
                "name-parse 永远解不出 940；**并且**该行没有动画资源 ⇒ 两条腿各自都能让它播不出",
                BuildScope),

            new Entry(
                "mekaniak", 950, KindNoAbilityTriggerAndEmptyEffect,
                "`AbilityTrigger` 枚举里没有 `Mekaniak` · 代码 0 引用（`out_cpp2il_dev_ilspy` 全树）· " +
                "`DisplayTriggerAnim` 18 处调用点无一传 0x3b6(950) · " +
                "该行 `effectAnim` 是空引用（`trait_effect_bindings.json` 的 `unresolved` 列）",
                "name-parse 永远解不出 950；**并且**该行没有动画资源 ⇒ 两条腿各自都能让它播不出",
                BuildScope),

            new Entry(
                "regeneration", 1030, KindNoAbilityTrigger,
                "`AbilityTrigger` 枚举里没有 `Regeneration` · " +
                "`CardScript__OnTurnEnd.c:84-107`：`HasCurrentTrait(0x406)` 为真时**直接** `BattleManager.HealOneCharacter`，**没有** `OnTrigger`（对比 `:72` 的 TurnEnd(40) 是有的）· " +
                "`DisplayTriggerAnim` 18 处调用点无一传 0x406(1030) · " +
                "`BattleCardUI__DisplayStatusAnim.c:86-119`/`:121-198` 只处理 `TriggerType == 0/2`，而 regeneration 的行是 3 ⇒ 状态框那条路也走不到",
                "name-parse 永远解不出 1030 ⇒ 回合末的治疗直接结算、不经过任何 trigger ⇒ 触发框永远不播",
                BuildScope),
        };

        // 读口用的索引（大小写不敏感，键 = 小写 trait 名）。确定性构建、只读。
        static readonly Dictionary<string, Entry> _byName = BuildIndex();

        static Dictionary<string, Entry> BuildIndex()
        {
            var d = new Dictionary<string, Entry>(_all.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _all.Length; i++)
            {
                d[Norm(_all[i].Trait)] = _all[i];
            }
            return d;
        }

        static string Norm(string traitName)
        {
            return string.IsNullOrEmpty(traitName) ? "" : traitName.Trim().ToLowerInvariant();
        }

        /// <summary>全部登记，**按 <see cref="Entry.DefinedTrait"/> 升序**（确定性；自检可直接遍历）。</summary>
        public static IReadOnlyList<Entry> All { get { return _all; } }

        /// <summary>登记条数。</summary>
        public static int Count { get { return _all.Length; } }

        /// <summary>这个 trait（大小写不敏感、两侧空白不计）是否被登记为不可达。</summary>
        public static bool IsUnreachable(string traitName)
        {
            Entry e;
            return TryGet(traitName, out e);
        }

        /// <summary>取一条登记；没有则返回 false（**不抛异常、不写日志**）。</summary>
        public static bool TryGet(string traitName, out Entry entry)
        {
            return _byName.TryGetValue(Norm(traitName), out entry);
        }

        /// <summary>不可达原因（一句话）；未登记 ⇒ 空串。</summary>
        public static string ReasonOf(string traitName)
        {
            Entry e;
            return TryGet(traitName, out e) ? e.Why : "";
        }

        /// <summary>判据出处；未登记 ⇒ 空串。</summary>
        public static string EvidenceOf(string traitName)
        {
            Entry e;
            return TryGet(traitName, out e) ? e.Evidence : "";
        }
    }
}
