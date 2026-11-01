// CardDatabasePartial_B110E5.cs — 第一百一十会话 · 执行代理 `B110_E5`
//   **附魔模板的 `abilities[]` → `CardDef.TriggerAbilityIndexSpec[]` 这座桥**
//   （判据 = `判据/B110_V1_真缺陷复核A.md` §🔴5，改法 = 该节「施工单」第 2/3 步）。
//
// ── 为什么另起 partial 文件 ───────────────────────────────────────────────────────────
//   `Data/CardDatabase.cs` 被 `账/*.tsv` 按【行号】钉了 25 处落点（现读最大 = `战斗逻辑驱动.tsv:1044`
//   引的 `:3567`）。本文件走 partial ⇒ 那边只在 `LoadEnchantmentTemplates` 内**插一段调用 + 注释**
//   （位置在全部落点之后 ⇒ 零漂移；前后对比读数见 `报告/B110_E5.md` §账落点前后对比）。
//
// ── 一、判据（⛔ 不重查、不另造）───────────────────────────────────────────────────────
//   · 原版每张卡（含附魔模板）恒有 `cardAbilities` ⇒ 模板的 ability 行**永远挂**（不是「有些模板没有 ability」那种三态）。
//   · `RawCardScript.GetAbilitiesForTriggerRawcard` / `CardAbility__CanTriggerAbility.c:71` 按
//     `ability.trigger(+0x10) == 传入 trigger` 选 ability ⇒ 我们的对应物 = `CardDef.TriggerAbilityRowsOf(trigger)`。
//     形状照卡表主路 `MapTriggerAbilityIndices`（`Data/CardDatabase.cs:4813-4841`）。
//     ⚠️ 2026-11-01 更正（`B114_WB2`）：原写 `:4785-4810`，v47 加了 `ca` 之后整段下移 ⇒ 已按现读订正。
//   · `triggerValue` = `CardAbility.triggerValue // +0x20`（`dump.cs:20340`；
//     语义见 `判据/B109_R18_附魔三判据.md` §2，V4 §H·2 复核过）。
//
// ── 二、三态（🔴 别合并「未知」与「明确没有」）──────────────────────────────────────────
//   · 入参 `null`（表里连 `abilities` 键都没有 = 旧表 / 手写夹具）⇒ 返回 **`null` = 列未知**
//     ⇒ 消费端 `EnchantmentHasAbilityForTrigger`（`EffectResolverPartial_B109E6.cs:160-183`）落 ③
//     「按文本 op 兜底」那一档并**出声一次**（⛔ 不静默）。
//   · `rows.Length == 0`（表里有这个键、值是 `[]`）⇒ 返回**空数组 = 明确没有 ability**
//     （现读 85 张里有 2 张是这一档：`Hive Fleet Leviathan` / `Saviour Protocols`）。
//   · `trigger < 0`（坏值）⇒ **出声 + 丢弃该行**（⛔ 不猜 `0` = `ThisCardPlayed`）。
//   · `triggerValue` 缺键 / 坏值 ⇒ `-1`（未知），⛔ 不当 `0`（`0` 是「明确 val 0」那一档）。
//
// ── 三、为什么 `public`（与兄弟 `MapTriggerAbilityIndices` 不同）────────────────────────
//   `MapTriggerAbilityIndices` 是 private；本方法 `public` **只为让 `Editor/B110_E5_Assert.cs` 直调验三态**
//   —— 它是**纯函数**、无副作用、不碰任何静态状态。⛔ 别因为「与兄弟不一致」改回 private：
//   改了断言就只能测真表，测不到「缺键 / 坏值 / 空表」这三档。
//
// ── 四、今天的行为面 ────────────────────────────────────────────────────────────────
//   · 判定为**零数值差异**：模板卡的正文 op 为空（模板不进 `cards_engine.json` 的 `cards[]`、不做文本 parse）
//     ⇒ `FireEnchantmentAbilitiesByNumber` 走到 `TriggerOpsByNumber` 那一步就 `continue`
//     ⇒ 只可能多发**表现信号** `EmitCardTrigger`（且它自己还有 `HasTriggerSource` 闸）。
//   · `ruleprobe` 不受影响：桩 `Resources.Load` 恒 null ⇒ `LoadEnchantmentTemplates` 直接返回空表。
using System.Collections.Generic;
using UnityEngine;

namespace RuleEngine
{
    public static partial class CardDatabase
    {
        /// <summary>附魔模板的 `abilities[]` → <see cref="CardDef.TriggerAbilityIndexSpec"/>。
        /// <para>`Ab` / `Lg` = **模板自己的下标**（原版 `GetAbilitiesForTriggerRawcard` 就按这套下标取 logic）。</para>
        /// <para>🔴 **三态**：入参 `null` ⇒ 返回 `null`（列未知）；`[]` ⇒ 返回**空数组**（明确没有）；
        /// 否则逐行展开（一条 ability 的 N 条 logic ⇒ N 行，`Ab` 相同 —— ⛔ 别去重、别只取第一条）。
        /// `trigger &lt; 0` 的行**出声 + 丢弃**。｜🆕 v47（`B114W-B2`）：`lg >= 0` 的行带 `Ca`（原版 `AbilityLogic.chosenAbility`，数据 = `enchantment_templates.json` 的 `abilities[].logic[].ca`）；`lg == -1`（该 ability 无 logic 行）**没有 `AbilityLogic` 实例** ⇒ `Ca = -1`（未知），⛔ 别去猜。</para>
        /// <para>🧨 灭自证：把 `LoadEnchantmentTemplates` 里那一跳删掉 ⇒ `TriggerAbilityRowsOf` 恒 `null` ⇒
        /// `B110_E5_Assert` 的 C 组红；把本方法的 `null` 与 `[]` 折成同一档 ⇒ B 组红。</para></summary>
        public static CardDef.TriggerAbilityIndexSpec[] MapEnchantmentTriggerAbilityIndices(
            EnchantmentTemplateAbilityDto[] rows, string tid)
        {
            if (rows == null) return null;                 // 缺 `abilities` 键 = 列**未知**（⛔ 不是「没有行」）
            var list = new List<CardDef.TriggerAbilityIndexSpec>(rows.Length);
            for (int i = 0; i < rows.Length; i++)
            {
                var a = rows[i];
                if (a == null)
                {
                    Debug.LogError($"[RuleEngine] 附魔模板 `{tid}` 的 `abilities[{i}]` 是 null —— **该行丢弃**。");
                    continue;
                }
                if (a.trigger < 0)
                {
                    Debug.LogError($"[RuleEngine] 附魔模板 `{tid}` 的 `abilities[{i}]` trigger 不合法（trigger={a.trigger}）"
                                 + " —— **该行丢弃**（⛔ 不猜 0 = ThisCardPlayed）。重跑 工具/gen_cards_engine.py。");
                    continue;
                }
                if (a.logic == null || a.logic.Length == 0)
                {
                    // 原版拿到这种 ability 就是一个空 logic 循环 ⇒ 它仍**占一行**（与卡表 `triggerAbilityIndices`
                    // 的 `lg = -1` 同口径：`trigger_ability_indices_of` 的「空 logic 那支」）。
                    list.Add(new CardDef.TriggerAbilityIndexSpec
                    { Ab = i, Lg = -1, Trigger = a.trigger, TriggerValue = a.triggerValue });
                    continue;
                }
                for (int j = 0; j < a.logic.Length; j++)
                    list.Add(new CardDef.TriggerAbilityIndexSpec
                    { Ab = i, Lg = j, Trigger = a.trigger, TriggerValue = a.triggerValue, Ca = (a.logic[j] != null) ? a.logic[j].ca : -1 });   // 🆕 v47（`B114W-B2`）：`Ca` = 原版 `AbilityLogic.chosenAbility`（数据侧 = `enchantment_templates.json` 的 `abilities[].logic[].ca`，DTO `CardDatabase.cs:3657`）—— `-1` = 未知，⛔ 不是 0
            }
            return list.ToArray();                         // 空数组 = **明确没有**（⛔ 不是 null）
        }
    }
}
