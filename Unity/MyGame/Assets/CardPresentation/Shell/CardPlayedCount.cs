// CardPlayedCount —— 原版 `CardPlayedCount : MissionChallenge`（「打出 N 张卡」这类任务挑战的**消费方**）
// 🆕 2026-10-30 · 执行代理 `B103_W1` · 账 `日常/奖励:786`
//   · 判据正本 → `资料/完全复刻/判据/B102_R2_日常奖励B类五条.md` §一·④（逐句 `.c:行号`）
//   · 原版真身：`d:/2/tools/decomp_full/CardPlayedCount__OnCardPlayed.c`（全文 77 行，🥇）
//     ＋ `cpp2il/out_cpp2il_dev_ilspy/CardPlayedCount.cs:67-124`（同序，字段名可读）
//     ＋ `dump.cs:58863-58876`（字段表）
//
// ============================ 判据（逐句，顺序就是判据，⛔ 不要重排）============================
//   `CardPlayedCount__OnCardPlayed.c`：
//   <code>
//   if (obj == null) throw;                                                    // .c:18-22（原版 NRE）
//   if (!obj.IsPlayer(+0x10)) return;                                          // .c:23 ★ 只看本地玩家
//   if (!anyCardType(+0x60) && RawCard.cardType(+0x94) != attackingType(+0x68)) return;   // .c:24-29
//   if (!anyTrait(+0x61)  && !withTrait(+0x70).All(RawCard.HasDefaultTrait)) return;      // .c:30-39
//   if (!anyKeyword(+0x62) && RawCard.keyword(+0x98) != keyword(+0x78)) return;           // .c:40-45
//   if (costCondition(+0x58) == null || RawCard == null) throw;                            // .c:46-47
//   if (!Compare(costCondition.ComparisonMethod(+0x10), RawCard.manaCost(+0x78), costCondition.Cost(+0x14))) return;  // .c:48-52
//   if (onlyCountCompanions(+0x64)) { if (CurrentCard(+0x20) == null) return; if (!isCompanion(+0x64)) return; }      // .c:53-66
//   if (!anySpellType(+0x63) && RawCard.spellType(+0x90) != spellType(+0x6C)) return;     // .c:67-72
//   UpdateProgress(this, 1, 0);                                                             // .c:73 ★ 推进 1（不是覆盖）
//   </code>
//   `.ctor`（ILSpy `CardPlayedCount.cs:126-138`）：
//   `costCondition = new CostCondition { ComparisonMethod = HigherOrEqual }`（`Cost` 缺省 0）· `anyCardType = true`
//   ⇒ **默认实例什么都数不到**（`anyTrait/anyKeyword/anySpellType` 全是 false，滤到默认值 0）——这是原版默认值，⛔ 别改成「更好用」的默认。
//
// ============================ 与我们的架构的对接（三处如实记）============================
// ① `Signal.Register/Unregister` → <see cref="RuleEngine.CardPlayedSignals"/>（发起源在引擎侧，见那个文件的文件头）。
// ② 载荷三格收窄：`RawCardScript → CardDef` · `CardScript(+0x20) → bool CurrentCardIsCompanion`
//    · `CurrentCost(+0x28)` **整格删掉**（原版零消费方，施工单 §一·b）。
// ③ `withTrait` 原版是 `DefinedTrait[]`（枚举数字）⇒ 本仓收窄成 `string[]`（trait **名**）：
//    我们的 `CardDef` 上那条路只有名字（`HasDefaultTrait(string)`；`cardTraits` 列存的是显示写法，
//    数字→名字的映射只在生成器里，⛔ 不在运行期）⇒ 结构不同、语义相同。
//    ⚠️ 数据在**远端** `MissionsConfig`（本机 0 份）⇒ 本类今天没有实例，它只是「形状与行为在」。
// ④ 原版 `CardPlayedCount.Army`（`CardPlayedCount__get_Army.c`，按 `onlyCountCompanions` / `spellType==260`
//    / `withTrait` 含 610 返回某个 `CardArmy`）**没搬** —— 本仓 `MissionChallenge` 基类没有 `Army` 这一位
//    （记进报告「还差什么」）。它不参与 `OnCardPlayed` 那六道闸，故不影响本条账的 A 判。
//
// ⚠️ 文件名为什么不叫 `CardPlayedCount.cs`：调度台本波给本代理的独占白名单是
//    `RuleEngine/Core/CardPlayedSignal*.cs`（见 `资料/完全复刻/文件所有权_当前.md`）⇒ 文件名带 `Signal` 前缀。
//    铁律 7「类名 == 文件名」的**后果**只对 `MonoBehaviour` / `ScriptableObject` 生效（没有同名脚本资产
//    就会被烘成 `!u!115` 存根）；本类与 `MissionChallenge` 都是纯 `[Serializable]` 类（同族先例：
//    `Shell/EnergySpent.cs` 一个文件里 6 个类、`CardPresentation` 命名空间）⇒ **不触发那条后果**。
//    类名照原版 `CardPlayedCount`（账里那行写的就是它），⛔ 别改成 `CardPlayedSignalCount`。
//    ✅ 若调度台愿意，一次 `git mv` 到 `CardPresentation/Shell/CardPlayedCount.cs` 即可归位（内容不用动）。
using System;
using RuleEngine;
// （Core 不许 `using UnityEngine` —— 架构护栏；这里用全限定 `UnityEngine.Debug`，同 `ActionPump.cs` 的写法）

namespace CardPresentation
{
    /// <summary>原版 `CardPlayedCount`（「打出 N 张卡」这类挑战）。行为 = 六道闸 + 一次 `UpdateProgress(1)`，
    /// 逐句判据见文件头。</summary>
    [Serializable]
    public class CardPlayedCount : MissionChallenge
    {
        /// <summary>原版 `public CostCondition costCondition; // 0x58`。`.ctor` 给 `ComparisonMethod = HigherOrEqual`（`Cost` 缺省 0）。</summary>
        public CostCondition costCondition;

        /// <summary>原版 `public bool anyCardType; // 0x60`。`.ctor` 给 **`true`**（= 不筛卡型）。</summary>
        public bool anyCardType;

        /// <summary>原版 `public bool anyTrait; // 0x61`（缺省 `false` ⇒ 要筛 trait）。</summary>
        public bool anyTrait;

        /// <summary>原版 `public bool anyKeyword; // 0x62`。</summary>
        public bool anyKeyword;

        /// <summary>原版 `public bool anySpellType; // 0x63`。</summary>
        public bool anySpellType;

        /// <summary>原版 `public bool onlyCountCompanions; // 0x64` = 只数「伴生造出来的那一份」。</summary>
        public bool onlyCountCompanions;

        /// <summary>原版 `public CardTypeOptions attackingType; // 0x68` —— 原值枚举
        /// （`CardDef.CardTypeMinion = 0 / CardTypeHero = 10 / CardTypeTactic = 20 / CardTypeWhispers = 40`）。
        /// ⚠️ 与卡表 `Type` 字符串（`unit/tactic/…`）**不是一回事**，判它只认同名那格 `CardDef.CardType`。</summary>
        public int attackingType;

        /// <summary>原版 `public SpellType spellType; // 0x6C`（`SpellType` 枚举原值）。</summary>
        public int spellType;

        /// <summary>原版 `public DefinedTrait[] withTrait; // 0x70` —— 本仓收窄成 **trait 名**（见文件头 ③）。</summary>
        public string[] withTrait;

        /// <summary>原版 `public Keyword keyword; // 0x78`（`Keyword` 枚举原值；卡上同名那位 = `CardDef.KeywordId`）。</summary>
        public int keyword;

        /// <summary>原版 `.ctor`：`costCondition = new CostCondition { ComparisonMethod = HigherOrEqual }` + `anyCardType = true`。
        /// `Cost` 保持缺省 0（原版也没给）。</summary>
        public CardPlayedCount()
        {
            costCondition = new CostCondition { ComparisonMethod = ChallengeComparison.HigherOrEqual };
            anyCardType = true;
        }

        /// <summary>原版 `protected override void OnInitialize()`（Slot 9）= `Signal.Register(OnCardPlayed)`
        /// （`CardPlayedCount__OnInitialize.c` 全文一跳）。</summary>
        protected override void OnInitialize()
        {
            CardPlayedSignals.Register(OnCardPlayed);
        }

        /// <summary>原版 `protected override void OnReset()`（Slot 8）= `Signal.Unregister(OnCardPlayed)`
        /// （ILSpy `CardPlayedCount.cs:61-65`；`.c` 那一跳被 ICF 折成同址的 `Register` 符号，以 ILSpy 为准）。
        /// <para>⚠️ 本仓基类 `MissionChallenge` 没有 `OnReset` 的抽象位 ⇒ 这里不是 `override`，
        /// 是「同名方法、由调用方显式调」（与 `Shell/TroopsKilledCount.cs:72` / `Shell/EnergySpent.cs:140` 同一处置）。</para></summary>
        public void OnReset()
        {
            CardPlayedSignals.Unregister(OnCardPlayed);
        }

        /// <summary>原版 `private void OnCardPlayed(CardPlayedSignal obj)` —— 六道闸逐句见文件头。
        /// <para>⚠️ 原版四处空闸都是**抛 NRE**（`.c:18-22` / `:46-47` / `:55-62`），全工程口径改成
        /// 「出声 + 不推进」（⛔ 不让游戏崩）；效果上与原版一致（抛出去的那一次也不计数）。</para></summary>
        public void OnCardPlayed(CardPlayedSignal obj)
        {
            if (obj == null)
            {
                UnityEngine.Debug.LogError("[CardPlayedCount] OnCardPlayed：信号是 null（原版这一跳 NRE）⇒ 不推进");
                return;
            }
            if (!obj.IsPlayer) return;                                       // ① `.c:23` —— 只数本地玩家打出的

            var raw = obj.RawCard;
            if (raw == null)
            {
                UnityEngine.Debug.LogError("[CardPlayedCount] OnCardPlayed：`RawCard` 是 null（原版这一跳 NRE）⇒ 不推进");
                return;
            }

            // ② 卡型（`!anyCardType` 才筛；`CardDef.CardType` 的 `-1` = 未知，⛔ 不当成 Minion(0)）
            if (!anyCardType && raw.CardType != attackingType) return;       // `.c:24-29`

            // ③ trait 全含（`withTrait` 为 null ⇒ 原版 `Enumerable.All(null)` 抛 ArgumentNullException）
            if (!anyTrait)
            {
                if (withTrait == null)
                {
                    UnityEngine.Debug.LogError("[CardPlayedCount] OnCardPlayed：`anyTrait = false` 但 `withTrait` 是 null"
                                   + "（原版这一跳抛 `ArgumentNullException`）⇒ 不推进");
                    return;
                }
                for (int i = 0; i < withTrait.Length; i++)
                {
                    string why;
                    bool has = raw.HasDefaultTrait(withTrait[i], out why);   // `.c:30-39`：`All(HasDefaultTrait)`
                    if (why != null)
                    {
                        // 卡表没挂 `cardTraits` ⇒ 判不了。⛔ 不许静默当成「没有」，也不推进（与原版「没有」同结果，但出声）
                        UnityEngine.Debug.LogError("[CardPlayedCount] OnCardPlayed：判不了 trait `" + withTrait[i] + "` —— " + why);
                        return;
                    }
                    if (!has) return;
                }
            }

            // ④ 关键词（原版 `RawCard.keyword(+0x98)` 一格整数比对）
            if (!anyKeyword && raw.KeywordId != keyword) return;             // `.c:40-45`

            // ⑤ 费用：**卡面费用** `RawCard.manaCost(+0x78)`，⛔ 不是实付费用（`CardPlayedSignal.CurrentCost` 原版零消费方）
            var cond = costCondition;
            if (cond == null)
            {
                UnityEngine.Debug.LogError("[CardPlayedCount] OnCardPlayed：`costCondition` 是 null（原版这一跳 NRE）⇒ 不推进");
                return;
            }
            if (!ChallengeComparisonExtensions.Compare(cond.ComparisonMethod, raw.Cost, cond.Cost)) return;   // `.c:48-52`

            // ⑥ 只数伴生（原版 `CurrentCard == null` ⇒ return；`!isCompanion` ⇒ return）
            if (onlyCountCompanions && !obj.CurrentCardIsCompanion) return;  // `.c:53-66`

            // ⑦ 法术类型
            if (!anySpellType && raw.SpellType != spellType) return;         // `.c:67-72`

            UpdateProgress(1);                                               // `.c:73` —— 推进 1（不是覆盖）
        }
    }
}
