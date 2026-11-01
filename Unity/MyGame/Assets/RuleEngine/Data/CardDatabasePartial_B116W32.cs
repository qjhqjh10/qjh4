// CardDatabasePartial_B116W32.cs — 执行写手 `B116_W32`（2026-11-02 · 活状态表 `B-329`）
//   **第三张旁表**：选项 SO（`TargetCriteria.filterSpecificCards` 指向的卡）的装载端
//   产物 = `Assets/RuleEngine/Resources/choice_option_cards.json`
//          （生成器 = `工具/gen_choice_option_cards.py`，⚠️ 必须带 `--write`/`--check`）
//
// ── 为什么是旁表，不是「把 57 张卡塞进可玩池」──────────────────────────────────────
//   · 原版取法（现读 `d:/2/tools/decomp_full/`）：
//       `BattleManager__GetChoiceFullPool.c:36-55` —— `targetsAffected == 0xb4(180)` 且
//       `filterSpecificCards` 非空 ⇒ 候选池**就是那张表**（`TargetCriteria+0x70`，
//       类型 `List<RawCardScript>`，`d:/2/tools/il2cpp_out/dump.cs:23870`），
//       **不再查任何 collection**；只有那张表为空才回落 `fullCardCollection`（`:57` 起）。
//       `GetChoiceOptions.c:36-70` 抽的是 `uniqueId` 字符串；
//       `AbilityLogic__PlayAbility.c:2032-2040`（`chosenAbility 360`）跑的是**选项 SO 自己**
//       的 `ThisCardPlayed`（`OnTriggerAlt(0, …)`）。
//   · ⇒ 选项 SO **不是可打出的卡**（`desc` 空 · `cardType 20` · 不进收藏/卡组）。
//     塞进 `cards_engine.json` 会改 `count`（1,129）⇒ 影响可玩池、收藏、轮抽与探针基线
//     **一大片分母**，而原版根本不用那条路。⇒ 单开一张表，只服务「按 id 查选项 SO」这一条读路。
//   📌 与 `enchantment_templates.json`（第二张表，`B89/E6c`）**同一条既定口径**：
//     ⛔ 不进 `cards_engine.json` / ⛔ 不进 `count` / ⛔ 不进任何分母。
//
// ── 为什么复用 `EnchantmentTemplateFileDto` / `EnchantmentTemplateDto` ─────────────────
//   两张表的**逐键形状相同**（`templates[].id/name/cardType/spellType/
//   abilities[{trigger,triggerValue,logic[{ca,pa,ta,fm}]}]/abilityBuffs[]`）
//   ⇒ 复用同一套 DTO + 同一套映射（`MapEnchantmentTriggerAbilityIndices` /
//   `EnchantmentTemplateTriggersOf` / `RecordEnchantmentTemplateAbilityBuffs`），
//   ⛔ **不另写第二份**（铁律 6：两处写同一条规则迟早不一致）。
//   `parents[]`（谁引用它）是排查列，DTO 里没有 ⇒ 本层不读，⛔ 不参与任何判据。
//
// ── 注入方向 ──────────────────────────────────────────────────────────────────────
//   `Core/` 不引用 `Data/`（方向固定：Data → Core）⇒ 与 `EnchantmentTemplatesProvider`
//   同一条路：本文件的**静态字段初始化器**把加载器登记进 `BattleContext.ChoiceOptionCardsProvider`。
//   ⚠️ **不能**再写一个 `static CardDatabase()` —— 同一个类只能有一个静态构造（`CS0111`）；
//      而静态字段初始化器会被编译器合并进**那一个**静态构造、并在它的体**之前**执行。
//
// ── ruleprobe 里会怎样 ────────────────────────────────────────────────────────────
//   探针的 `Resources.Load` 是桩（恒 `null`）⇒ 本表在探针里**恒空** ⇒
//   `BattleContext.FindChoiceOptionCard` 恒 `null` ⇒ 结构化支照旧回落文本
//   ⇒ `play check` 指纹**不变**（与第二张表 `enchantment_templates.json` 完全同一条现状）。
//   ⚠️ 所以「本件在网上跑通了」的证据**不能**拿 `play check` 当；由
//      `Editor/B116W32_Assert.cs`（Unity 宿主）＋ 报告 §四 的离线复跑给出。
using System.Collections.Generic;
using UnityEngine;

namespace RuleEngine
{
    public static partial class CardDatabase
    {
        /// <summary>第三张表的资源名（`Resources/choice_option_cards.json`）。</summary>
        public const string ChoiceOptionCardsResourcePath = "choice_option_cards";

        /// <summary>「表读不到」只出声一次（每次开局都会问到，别刷屏）。</summary>
        static bool _choiceOptionCardsMissingLogged;

        /// <summary>把加载器登记进 `Core/BattleContext` 的钩子（**静态字段初始化器**，理由见文件头）。
        /// <para>返回 `null` 只是为了让它是「字段初始化器」的合法表达式 —— ⛔ 别把它当返回值用。</para></summary>
        static readonly object _choiceOptionCardsProviderInstalled = InstallChoiceOptionCardsProvider();
        static object InstallChoiceOptionCardsProvider()
        {
            BattleContext.ChoiceOptionCardsProvider = LoadChoiceOptionCards;
            return null;
        }

        /// <summary>读第三张表 → 选项 SO 的 `CardDef` 列表（**不并入** <see cref="Load"/> 的可玩池）。
        /// <para>读不到 ⇒ **出声并返回空表**（⛔ 不许静默）；调用方要区分「表不存在」与「表是空的」
        /// 请看日志 —— 与 <see cref="LoadEnchantmentTemplates"/> 同一口径。</para></summary>
        public static List<CardDef> LoadChoiceOptionCards()
        {
            var asset = Resources.Load<TextAsset>(ChoiceOptionCardsResourcePath);
            if (asset == null)
            {
                if (!_choiceOptionCardsMissingLogged)
                {
                    _choiceOptionCardsMissingLogged = true;
                    Debug.LogError($"[RuleEngine] 读不到第三张表 `Resources/{ChoiceOptionCardsResourcePath}.json`"
                                 + " —— `choose one:` 一类卡的选项 SO **一律查不到** ⇒ 结构化支回落文本支。"
                                 + "重跑 `python -I -X utf8 工具/gen_choice_option_cards.py --write`。");
                }
                return new List<CardDef>();
            }
            return ParseChoiceOptionCards(asset.text);
        }

        /// <summary>第三张表的**纯解析口**（不碰 `Resources`）—— 自检/离线复跑直接用字符串喂。</summary>
        public static List<CardDef> ParseChoiceOptionCards(string json)
        {
            var list = new List<CardDef>();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[RuleEngine] 选项 SO 旁表是空字符串 —— 返回空表。");
                return list;
            }
            var file = JsonUtility.FromJson<EnchantmentTemplateFileDto>(json);
            if (file == null || file.templates == null)
            {
                Debug.LogError($"[RuleEngine] 选项 SO 旁表解析失败（{json.Length} 字符）—— 返回空表。");
                return list;
            }
            var seen = new HashSet<string>();
            foreach (var t in file.templates)
            {
                if (t == null || string.IsNullOrEmpty(t.id))
                {
                    Debug.LogError("[RuleEngine] 选项 SO 表里有一行**没有 `id`** —— 该行丢弃。");
                    continue;
                }
                if (!seen.Add(t.id))
                {
                    Debug.LogError($"[RuleEngine] 选项 SO 表里 `id = {t.id}` **重复** —— 后一条丢弃。");
                    continue;
                }
                // `type` 用 "choiceoption"：它既不是 unit 也不是 tactic，更**不当作可玩卡**
                // （`inventoryOptions = 10 (NotInInventory)` / `showInCollectionOptions = 0 (Hide)`
                //  与第二张表的模板同一档）。⛔ 别把它写回 "enchantment" —— 那会让
                //  「这是附魔模板」这一类判据把它误认成模板。
                var def = new CardDef(t.id, string.IsNullOrEmpty(t.name) ? t.id : t.name,
                                      "choiceoption", "", "", "",
                                      0, 0, 0, 0, null,
                                      spellType: t.spellType,
                                      inventoryOptions: 10, showInCollectionOptions: 0);
                def.AttachCardType(t.cardType);
                def.AttachTriggerAbilityIndices(MapEnchantmentTriggerAbilityIndices(t.abilities, t.id));
                def.AttachAbilityTriggers(EnchantmentTemplateTriggersOf(t.abilities));
                RecordEnchantmentTemplateAbilityBuffs(def, t);      // 写口仍是唯一的 `RecordAbilityBuffs`
                list.Add(def);
            }
            if (file.count != list.Count)
                Debug.LogError($"[RuleEngine] 选项 SO 表 `count` = {file.count}，实读 {list.Count} 张 —— 表坏了。");
            Debug.Log($"[RuleEngine] 选项 SO 旁表 v{file.version}：**{list.Count}** 张"
                    + $" —— 来自 `Resources/{ChoiceOptionCardsResourcePath}.json`");
            return list;
        }
    }
}
