// CardDefPartial_B96CA.cs — 第九十五会话 · 波 · 执行代理 `B96_CA`
//   `CardDef` 的**又一半**：**「触发数值 → 这条 ability 的正文 op」**那张表（账 `战斗逻辑驱动:1288`
//   `RawCardScript.UpdateTriggerOnce` 的**代码半**）。
//
// 🔴 **为什么另起一个 `partial class CardDef` 块、写在【新文件】**：
//   同 `CardDefPartial_B94B16.cs` / `CardDefActiveAbility.cs` 的文件头纪律 ——
//   `CardDef.cs` 里有一批账落点按【行号】引用（现读 81 处），在既有文件里插成员会让它们整体漂移。
//   本文件走 partial ⇒ `CardDef.cs` 一个字节不动 ⇒ **预期零漂移**。
//
// ── 一、判据（第一权威 = `d:/2/tools/decomp_full/`，2026-10-29 逐句现读）────────────────────────
//   · `RawCardScript__UpdateTriggerOnce.c:1-16`（**全文**）：纯转发
//     `TriggerAbilities(200, mgr, thisCard, cardPlayed = 0, targetCard = 0, 空 damageInfo,
//      isEnchantment = 0, resolveOnly = 0, ignoreRandom = 0, dontResolve = 0)`。
//   · `RawCardScript__TriggerAbilities.c:199-232`（取值那一跳）：遍历
//     `cardAbilities(+0x290)`，**按 `ability.trigger(+0x10) == 传入 trigger` 选 ability**，再逐条
//     `TriggerAbility(...)`。⇒ 原版**按触发数值**取 ability，**不按卡面文本**。
//   · 闸在调用方 `CardScript.UpdateWhileInPlay`（ILSpy `cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:16033-16039`）：
//     `if (triggeredAbility && UpdateTriggerOnce(...)) { triggeredAbility = false; }`，
//     其中 `triggeredAbility = HasTriggerOnceAbility()`（任一 ability 的 `trigger == 200`）。
//
// ── 二、我们缺的是什么（账 1288 的「差异」列原文）──────────────────────────────────────────────
//   我们的正文是**按卡面关键词**收的（`CardDef._triggerOps`，键 = `KeywordTable.*` 的规范小写名），
//   而**没有任何一张卡的卡面写 `TriggerOnceIfBoardCriteria:` 这个词** ⇒ `_triggerOps` 里
//   **永远不会有 trigger 200 那一格** ⇒ `FireTriggerOnce` 恒 null。
//
// ── 三、本文件做了什么（**代码半**；数据半留给生成器，⛔ 不在本代理白名单）─────────────────────
//   ① `TryAbilityTriggerNameOfNumber(int, out string)` —— **数值 → 触发词名**的反查。
//      ⚠️ 键表**只有一份**（`CardDef.AbilityTriggerNumberOfName`，`CardDef.cs:1913-1948`，97 条）
//      ⇒ 本文件**不抄第二份**（铁律 6），只是把那张私有表反着扫一遍。
//   ② `AttachTriggerOpsByNumber(...)` / `TriggerOpsByNumber(int)` —— 「触发数值 → 正文 op」的口。
//      · 数据半（生成器 `工具/gen_cards_engine.py` → `CardDatabase`）把
//        `{trigger 数值 → List<EffectOp>}` 挂进来之后，本口**直接**返回那一格；
//      · **没挂**时退回「**卡面关键词 == 触发词名**」那一格（`_triggerOps[name.ToLowerInvariant()]`）
//        —— 这是今天唯一有判据-backed 的取法，⛔ **不是**「猜一个 op 出来」。
//   ③ 📌 **数据半的落点（交给别人做，照下面照抄即可）**：生成器输出
//      `[{"ab":<cardAbilities下标>,"lg":<abilityLogic下标>,"trigger":<数值>}]`，
//      `CardDatabase` 解析时按 `(ab,lg)` 找出该 logic 正文 op 并调 `AttachTriggerOpsByNumber`。
//      ⚠️ 那一天之前：**全池 `abilityTriggers` 含 200 的卡 = 0 / 1,129**（现扫 `cards_engine.json`）
//      ⇒ 本口**恒返回 null**、`FireTriggerOnce` 恒 false ⇒ **零行为变化**（休眠件，铁律 11 照样建）。
//
// ── 四、⛔ 别做的事 ────────────────────────────────────────────────────────────────────────────
//   · ⛔ 别在 `CardDef.cs` 里再写一份数值↔名字的映射（那会让两份表迟早不一致）。
//   · ⛔ 别把「没有 op」当成「用卡面正文的某一句凑一个」（原版那一步是按 ability 选的，不是按句子猜的）。

using System;
using System.Collections.Generic;

namespace RuleEngine
{
    public partial class CardDef
    {
        /// <summary>
        /// **触发数值 → 触发词名** 的反查（判据 → 文件头 §一；键表 = `CardDef.AbilityTriggerNumberOfName`，
        /// **同一份、不抄第二份**）。
        ///
        /// <para>⚠️ 有 **6 组同义词共用一个数值**（`Strike 580` / `Duty 662` 等由 `RoutableTriggers` 那边区分），
        /// 反查会命中**其中名字序靠前的那个** ⇒ 想按名字区分用 `AbilityTriggerNumber(name)`，别拿本口反向推。
        /// 本口只服务「按数值取 ability 正文」这一件事（那里名字只用来做 `_triggerOps` 的键，同义词本来就落到同一格）。</para>
        /// </summary>
        /// <returns>认得出返回 `true` 并给出名字；`false` ⇒ 那个数值不在这张表里（⛔ 不猜）。</returns>
        public static bool TryAbilityTriggerNameOfNumber(int number, out string triggerName)
        {
            foreach (var kv in AbilityTriggerNumberOfName)
            {
                if (kv.Value != number) continue;
                triggerName = kv.Key;
                return true;
            }
            triggerName = null;
            return false;
        }

        /// <summary>数据半挂进来的「触发数值 → 正文 op」表（`null` = 还没落列 / 旧表）。
        /// ⚠️ 只由 `AttachTriggerOpsByNumber` 写；⛔ 别在别处塞。</summary>
        Dictionary<int, List<EffectOp>> _triggerOpsByNumber;

        /// <summary>由加载层在**构造之后**挂载（与 `AttachAbilitySummon` / `AttachWhileInPlayAnim` 同族；
        /// 理由 = `CardDef` 构造函数上那四十处账落点，⛔ 不往它加形参）。
        /// `null` = 旧表（未知）⇒ 整口退回「卡面关键词 == 触发词名」那一格。</summary>
        public void AttachTriggerOpsByNumber(Dictionary<int, List<EffectOp>> map)
        {
            _triggerOpsByNumber = map;
        }

        /// <summary>`triggerOpsByNumber` 这一列**是否已知**（`true` = 数据半已经挂过表）。
        /// ⚠️ 与「表是空的」不是一回事 —— `false` 时调用方应知道自己在用退回口径。</summary>
        public bool HasTriggerOpsByNumber { get { return _triggerOpsByNumber != null; } }

        /// <summary>
        /// **按触发数值取这一条 ability 的正文 op**（= 原版 `RawCardScript.TriggerAbilities` 按
        /// `ability.trigger(+0x10) == 传入 trigger` 选 ability 那一跳；判据 → 文件头 §一）。
        ///
        /// <para>两级口径（**顺序固定**）：
        /// ① 数据半挂进来的表（`_triggerOpsByNumber`）里有这个数值 ⇒ 就返回它（`null` = 明确没有 ⇒ 也回 `null`）；
        /// ② 没挂表 ⇒ 退回「**卡面关键词 == 触发词名**」那一格：`TriggerOps(名字的小写)`。</para>
        ///
        /// <para>⚠️ 第 ② 级**不是原版口径**，是今天唯一有判据-backed 的近似（我们全部正文都收在
        /// `_triggerOps` 里、键是关键词）。它对 `200 (TriggerOnceIfBoardCriteria)` 恒返回 `null`
        /// （现读全池没有一张卡面写那个词）—— **这正是账 1288 的缺口本体**，⛔ 别当成 bug 去「修」成别的东西。</para>
        /// </summary>
        /// <returns>`null` = 这个触发数值在本卡上没有正文（或判不了）。空列表也回 `null`（口径同 `TriggerOps`）。</returns>
        public List<EffectOp> TriggerOpsByNumber(int trigger)
        {
            if (_triggerOpsByNumber != null)
            {
                List<EffectOp> hit;
                return _triggerOpsByNumber.TryGetValue(trigger, out hit) ? hit : null;
            }
            string nm;
            if (!TryAbilityTriggerNameOfNumber(trigger, out nm) || string.IsNullOrEmpty(nm)) return null;
            var ops = TriggerOps(nm.ToLowerInvariant());
            if (ops == null || ops.Count == 0) return null;
            return ops as List<EffectOp> ?? new List<EffectOp>(ops);
        }
    }
}
