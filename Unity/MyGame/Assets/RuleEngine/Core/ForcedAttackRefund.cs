// ForcedAttackRefund.cs — 「这一记**强制攻击**该不该退还一次行动额度」的真值查询口（`Core/` 那一半）
//
// ── 这是什么（原版判据，逐支读过）──────────────────────────────────────────
// 原版 `AbilityLogic.PlayAbility` 按 `chosenAbility` 分派（同一段 `switch`），**只有三支**
// 把 `dontCountAttack = 1` 传给建动作那一跳：
//   · `231` `0xe7` → `AddForceAttack(…, 1, 0)`（`decomp_full/AbilityLogic__PlayAbility.c:2666-2686`）
//   · `237` `0xed` → `AddScriptedForceAttack(…, 1, 0)`（`:2752-2766`）
//   · `238` `0xee` → `AddScriptedForceAttack(…, 1, 0)`（`:2767-2780`）
// 其余几支**不退还**：
//   · `230` `0xe6` → `AddForceAttack(…, 0, 0)`（最后一参 = 0）；
//   · `233` `0xe9` → `AddForceAttackFriendlyUnits`（该族签名里**根本没有**这一格）；
//   · `235` `0xeb` → `AddScriptedAttack`（签名到 `blockPlayerActions` 就完了，`dump.cs:32502`；
//     方法体 `BattleManager__AddScriptedAttack.c` 里 `RemoveAttackThisTurn` / `+0x48` **0 命中**）；
//   · `236` `0xec` → `AddRedirectedAttack`（`dump.cs:32505` 同，方法体同 `0 命中`）。
//
// 退还**怎么发生**（两条合起来，判据全文 → `RuleCore.DeclareAttack` 旁边的注释块）：
//   入队预扣 + 记 `wasFree`（`BattleManager__AddForceAttack.c:39-43` /
//   `BattleManager__AddScriptedForceAttack.c:42-46`）→ 结算后 `wasFree` 才排 `AddResetActions`
//   （`BattleManager._ResolveForceAttack_d__504__MoveNext.c:73-77`）→ `CardScript__ResetActions.c:29-34`
//   （计数 `-1` + `ActivateMinion` 重算 `canAct`/`canAttack`）。
//   我们这边 = `RuleCore.DeclareAttackRefundingForced` / `RuleCore.RefundForcedAttackAction`。
//
// ── 数据从哪来 ──────────────────────────────────────────────────────────
// 卡表 `Resources/cards_engine.json` 的 **v32 `chosenAbility` 列**（生成器 = `工具/gen_cards_engine.py`；
// 源 = `cards_truth.json` 的 `abilities[].logic[].chosenAbility`，
// 加 `activeAbility` / `alternativeAbility` 两条主动能力支；形状见生成器 `chosen_abilities_of`）。
// 🔴 **⛔ 别拿 `trigger` 代判**：`GOF86` = trigger 30、`GOF93` = trigger **410**，两张都是 235；
//    而 238 也出现在 410 ⇒ trigger 分不开这两族（`B8_BK` 诊断 §3·4 ①）。
//
// ── 三态（`Unknown` 是**故意的**，⛔ 别折成 false）────────────────────────
// · `Refund`   = 这张卡的 ability 里**有** {231,237,238} 之一 ⇒ 退还；
// · `NoRefund` = **没有**退还族，但**有** {230,233,235,236} 之一 ⇒ 不退还；
// · `Unknown`  = 查不到（旧表 / 探针里没装载 / 这张卡的强制攻击来自**别处**
//   —— 例：`SW56 Let Loose` 的攻击在它造出来的**附魔**上（`chosenAbility 238` 落在
//   `f16df0b3…` 那张附魔模板上，本卡自己的 abilities 只有 30/320/260）/ 合成夹具）
//   ⇒ 调用点**退回旧行为**（`RuleCore` 那边：退还）。⛔ 不要在这里替它猜一档。
//
// ── 为什么 `Core/` 这一半只有查表、没有装载 ─────────────────────────────
// 🔴 本文件在 `Core/` 下 ⇒ **不许碰 `UnityEngine`**（`Editor/RuleEngineTest.cs` 的
//    `TestCoreLayerPurity` 有一条源文扫描断言盯着 `Core/*.cs`）。装载要 `Resources.Load` +
//    `JsonUtility` ⇒ 那半页在 `Data/ForcedAttackRefundMap.cs`（同一个 `partial class` 的另一半，
//    照 `Data/RelatedCardMap.cs` / `Data/TutorialDatabase.cs` 那套先例）。
//    离屏探针（`工具/ruleprobe` 只编 `Core/*.cs`）里那一半**不在编译集** ⇒ `LoadFromUnity()`
//    的声明与调用一起被抹掉 ⇒ 表恒 `null` ⇒ `Of()` 一律 `Unknown` ⇒ 调用点退回旧行为。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>「这一记强制攻击退不退还行动额度」的**唯一判据口**（原版 `chosenAbility` 的三支）。
    /// 语义 / 判据 / 三态契约 / 为什么不碰 UnityEngine → 文件头。</summary>
    public static partial class ForcedAttackRefund
    {
        /// <summary>三态。⚠️ `Unknown` 的下游语义 = **退回旧行为（退还）**，见 <see cref="Of"/>。</summary>
        public enum Verdict
        {
            /// <summary>查不到（旧卡表 / 探针没装载 / 这张卡的强制攻击来自别处）⇒ 下游退回旧行为。</summary>
            Unknown = 0,
            /// <summary>原版**退还**（`dontCountAttack = 1`）：`chosenAbility ∈ {231, 237, 238}`。</summary>
            Refund = 1,
            /// <summary>原版**不退还**：`chosenAbility ∈ {230, 233, 235, 236}`。</summary>
            NoRefund = 2,
        }

        /// <summary>退还那三支（`0xe7` / `0xed` / `0xee`）。判据 → 文件头。</summary>
        static bool IsRefundFamily(int chosenAbility)
        {
            return chosenAbility == 231 || chosenAbility == 237 || chosenAbility == 238;
        }

        /// <summary>不退还那四支（`0xe6` / `0xe9` / `0xeb` / `0xec`）。判据 → 文件头。</summary>
        static bool IsNoRefundFamily(int chosenAbility)
        {
            return chosenAbility == 230 || chosenAbility == 233
                || chosenAbility == 235 || chosenAbility == 236;
        }

        /// <summary>`卡 id → 三态`。`null` = 还没装载（探针里**恒** `null`）。</summary>
        static Dictionary<string, Verdict> _map;

        /// <summary>`Data/ForcedAttackRefundMap.cs` 里那一半的实现（`Resources.Load` + `JsonUtility`）。
        /// 离屏探针里没有实现 ⇒ 调用被抹掉 ⇒ 表恒 `null`（**故意**：探针不碰 Resources）。</summary>
        static partial void LoadFromUnity();

        /// <summary>`Data/` 半页那个「只试一次」的闸一起复位（否则清表之后永远不再装载）。
        /// 探针里没有实现 ⇒ 调用被抹掉（那边本来就没有装载）。</summary>
        static partial void ResetLoaderForTest();

        /// <summary>装载口（**只给 `Data/` 半页用**）：把一张卡**全部 ability** 的 `chosenAbility` 灌进来。
        /// <para>判定（唯一一处）：含 `{231,237,238}` ⇒ <see cref="Verdict.Refund"/>；
        /// 否则**含有**任何「不退还」族 ⇒ <see cref="Verdict.NoRefund"/>；
        /// 两者都没有（这张卡的强制攻击来自别处）⇒ **不落键**（保持 `Unknown`）。</para>
        /// <para>⚠️ 与 <see cref="SetForTest"/> 的关系：后写的赢（夹具 id 形如 `FixtureA1606Each`，
        /// 与卡池 id 不会撞）。</para></summary>
        public static void Install(string cardId, int[] chosenAbility)
        {
            if (string.IsNullOrEmpty(cardId) || chosenAbility == null || chosenAbility.Length == 0) return;
            bool refund = false, noRefund = false;
            for (int i = 0; i < chosenAbility.Length; i++)
            {
                int ca = chosenAbility[i];
                if (IsRefundFamily(ca)) refund = true;
                else if (IsNoRefundFamily(ca)) noRefund = true;
            }
            // 退还族优先：一张卡**同时**有两族时（现读 0 张）按退还走 —— 保守，且与「Unknown 也退还」同向。
            if (refund) { EnsureMap()[cardId] = Verdict.Refund; return; }
            if (noRefund) { EnsureMap()[cardId] = Verdict.NoRefund; return; }
        }

        /// <summary>查一张卡的三态。`Unknown` 的**下游语义 = 退回旧行为（退还）** ——
        /// 调用点（`RuleCore`）负责那一跳，⛔ 别在这里替它写成 `NoRefund`。</summary>
        public static Verdict Of(string cardId)
        {
            LoadFromUnity();                       // 探针里被抹掉；Unity 侧自带「只试一次」
            if (string.IsNullOrEmpty(cardId) || _map == null
                || !_map.TryGetValue(cardId, out var v))
            {
                UnknownLookupsForTest++;
                return Verdict.Unknown;
            }
            return v;
        }

        /// <summary>🔴 **只为自检开门**（与 `RelatedCardData.SetForTest` 同一条纪律：
        /// 产品路径一次都不调它）。合成夹具（`FixtureA1606*` / `T_LetLoose`）在卡表里没有键
        /// ⇒ 让它们**显式声明**自己代表哪一族（⛔ 不是给实现开旁路：判据仍然只读 <see cref="Of"/>）。</summary>
        public static void SetForTest(string cardId, Verdict verdict)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            EnsureMap()[cardId] = verdict;
        }

        /// <summary>清空（只给自检用）—— 让一条用例的声明不污染下一条：整表清空 **+ 复位装载闸**
        /// ⇒ 下一次 <see cref="Of"/> 会重新从 `Resources` 装载真值（探针里则照旧恒 `Unknown`）。</summary>
        public static void ResetForTest()
        {
            _map = null;
            ResetLoaderForTest();
        }

        /// <summary>`Of()` 返回 `Unknown` 的次数（只增不减）。**只给自检/诊断用**：
        /// 「这条断言其实走的是回退档」在日志里看不出来，靠它当判别式。</summary>
        public static int UnknownLookupsForTest { get; private set; }

        /// <summary>表里有多少张卡（探针里恒 0）。**只给自检/诊断用**。</summary>
        public static int TableCards { get { LoadFromUnity(); return _map == null ? 0 : _map.Count; } }

        static Dictionary<string, Verdict> EnsureMap()
        {
            if (_map == null) _map = new Dictionary<string, Verdict>(StringComparer.Ordinal);
            return _map;
        }
    }
}
