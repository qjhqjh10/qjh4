// EffectsToShow.cs — 原版 `EntityScript.GetCardEffectsToShow`（第一道过滤 + mark 四神聚合）的**纯规则核**
//
// 判据（第一权威）：
//   · `d:/2/tools/decomp_full/EntityScript__GetCardEffectsToShow.c:1-196`
//   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/EntityScript.cs:2149-2497`（可读，推荐对照）
//   · `d:/4/Unity/资料/完全复刻/判据/GetCardEffectsToShow_施工细则.md` §1.3 / §1.4
//
// 🔴 本文件【只】实现「第一道过滤 + mark 聚合」这块**纯规则**（= 施工细则 §1.3 那一段，S1 的规则半边）：
//    ① 外闸（markOfChaosType ∈ {none(0), Random(50)} || trait == markOfChaos(330)）；
//    ② mark 分支（按四神各聚一条、traitValue 累加、占位 = 该神首现位置；God ∉ {10,20,30,40} ⇒ 丢弃不加入）；
//    ③ 非 mark 的两条排除档（changeCost(2)&&costChange==0 · addTrait(3)&&trait∈{1276,1277,1278}）。
//
// ⛔ **不在这里写第二道过滤**（`CardDisplayWindow.DisplayCardEffects` 那道：
//    changeRace(6) / addRace(30) / daemonHost(35) / choiceEnchantment(16)&&!hasQuests）——
//    它**已经有一份** = `UnitState.IsEffectListEntry`（`UnitStateEffectDisplay.cs:61`）。
//    铁律：同一条规则只写一处，⛔ 别在这里抄第二份（两处迟早不一致）。
//
// ✅ **整条投影【已接】**（口径同账 `特效动画:521`；旧头注写「今天没有生产消费者」= S1 落地时的状态，**已过期**）：
//    7 个源（_statMods / _traitEffects / TempBuffs / CardInstance.ActiveEffects / HandEffects / …）
//    已合成「一条有序 List」+ 每条 `Seq` 序戳（S2，`UnitState.NextEffectSeq`）＋ 表现侧消费
//    （S4 `CardDisplayWindow.RowsOfEffects`，2026-10-29 `B97_W16`）；**生产接线 = S5**（`BattleDriver.cs:13212`，2026-10-29 `B97_W18`）⇒ 本类**今天有真消费者**。
//    ⚠️ 仍缺的 3 段（如实标，⛔ 别当成「已做」）→ 见 `Of()` 上面那段「今天做不到的两段」＋ 账 `特效动画:521` 的残差。
//
// ⚠️ 本文件属 `Core/` —— **不允许依赖 UnityEngine**。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>原版 `EntityScript.GetCardEffectsToShow` 的第一道过滤 + mark 四神聚合（判据见文件头）。</summary>
    public static class EffectsToShow
    {
        // ── markOfChaosType 枚举值（`dump.cs` / 施工细则 §1.3）──────────────────────────
        /// <summary>无 mark。</summary>
        public const int MarkNone = 0;
        /// <summary>Khorne。</summary>
        public const int MarkKhorne = 10;
        /// <summary>Nurgle。</summary>
        public const int MarkNurgle = 20;
        /// <summary>Slaanesh。</summary>
        public const int MarkSlaanesh = 30;
        /// <summary>Tzeentch。</summary>
        public const int MarkTzeentch = 40;
        /// <summary>随机一神（外闸放行，但 mark 分支的 default ⇒ 丢弃）。</summary>
        public const int MarkRandom = 50;

        /// <summary>`DefinedTrait.markOfChaos = 330`（第一道外闸与 mark 分支的判据）。</summary>
        public const int TraitMarkOfChaos = 330;

        // ── BuffType 值（`dump.cs:45430-45462`）────────────────────────────────────────
        /// <summary>`BuffType.changeCost = 2`（排除档之一）。</summary>
        public const int BuffTypeChangeCost = 2;
        /// <summary>`BuffType.addTrait = 3`（排除档之一，只对 oath 三条生效）。</summary>
        public const int BuffTypeAddTrait = 3;
        /// <summary>`BuffType.markOfChaos = 8`（`CreateMarkCardEffect` 造出来的聚合条就用它）。</summary>
        public const int BuffTypeMarkOfChaos = 8;
        /// <summary>`BuffType.lastMarkOfChaos = 17`（下游 `LoadEffect` 与 8 同走 mark 双行分支）。</summary>
        public const int BuffTypeLastMarkOfChaos = 17;

        // ── DefinedTrait：被排除的三个 oath（`dump.cs` `DefinedTrait` 段）──────────────
        /// <summary>`oathInAllTurns = 1276`（`0x4fc`）。</summary>
        public const int OathInAllTurns = 1276;
        /// <summary>`oathDouble = 1277`（`0x4fd`）。</summary>
        public const int OathDouble = 1277;
        /// <summary>`oathTripleActivation = 1278`（`0x4fe`）。</summary>
        public const int OathTripleActivation = 1278;

        // ── 环境效果卡 spellType（`RawCardScript.IsEnviromentaleffect`）────────────────
        /// <summary>`spellType == 210`（池内 39 张 defence）。</summary>
        public const int SpellTypeEnv210 = 210;
        /// <summary>`spellType == 200`（池内 0 张，判据照样收）。</summary>
        public const int SpellTypeEnv200 = 200;

        /// <summary>第一道外闸（`.c:40` / `:175-186`）：只放行「普通效果」与「mark」。
        /// <c>markOfChaosType ∈ {none(0), Random(50)} || trait == markOfChaos(330)</c>。</summary>
        public static bool PassesOuterGate(int markOfChaosType, int trait)
        {
            return markOfChaosType == MarkNone || markOfChaosType == MarkRandom || trait == TraitMarkOfChaos;
        }

        /// <summary>普通效果（非 mark）的两条排除档（`.c:131-173`）：
        /// <c>changeCost(2) &amp;&amp; costChange == 0</c> · <c>addTrait(3) &amp;&amp; trait ∈ {1276,1277,1278}</c> ⇒ 不展示。</summary>
        public static bool IsExcludedNormal(int buffType, int costChange, int trait)
        {
            if (buffType == BuffTypeChangeCost && costChange == 0) return true;
            if (buffType == BuffTypeAddTrait)
                return trait == OathInAllTurns || trait == OathDouble || trait == OathTripleActivation;
            return false;
        }

        /// <summary>mark 分支的「四神槽位」：Khorne(10)→0 · Nurgle(20)→1 · Slaanesh(30)→2 · Tzeentch(40)→3；
        /// 其余（none/Random）⇒ <c>-1</c>（原版 `.c:47-130` 的 default ⇒ <c>continue</c>，**丢弃且不占位**）。
        /// ⚠️ 「外闸放行」与「mark 分支接纳」是两件事：trait==330 但 markOfChaosType∈{none,Random} 的，外闸过、这里丢。</summary>
        public static int MarkGodSlot(int markOfChaosType)
        {
            switch (markOfChaosType)
            {
                case MarkKhorne: return 0;
                case MarkNurgle: return 1;
                case MarkSlaanesh: return 2;
                case MarkTzeentch: return 3;
                default: return -1;
            }
        }

        /// <summary>下游是否按「mark 双行合成」渲染（`CardEffectItem.LoadEffect`：`buffType == 8 || == 17`）。</summary>
        public static bool IsMarkBuffType(int buffType)
        {
            return buffType == BuffTypeMarkOfChaos || buffType == BuffTypeLastMarkOfChaos;
        }

        /// <summary>环境效果卡 ⇒ 「受到以下影响」整组必须隐藏（`DisplayCardEffects` `.c:88-105`；
        /// `RawCardScript.IsEnviromentaleffect` = `spellType ∈ {210, 200}`）。</summary>
        public static bool IsEnvEffectCard(int spellType)
        {
            return spellType == SpellTypeEnv210 || spellType == SpellTypeEnv200;
        }

        /// <summary>一条记录会不会被第一道过滤保留（把外闸 + mark 分支 + 排除档合成一次判定，供「只问在不在」的调用方）。
        /// <para>mark 分支（`trait == 330`）⇒ 返回 `MarkGodSlot &gt;= 0`（false ⇒ 该条不进聚合）；</para>
        /// <para>非 mark ⇒ 过外闸且不含排除档。</para></summary>
        public static bool IsKept(int markOfChaosType, int trait, int buffType, int costChange)
        {
            if (!PassesOuterGate(markOfChaosType, trait)) return false;
            if (trait == TraitMarkOfChaos) return MarkGodSlot(markOfChaosType) >= 0;
            return !IsExcludedNormal(buffType, costChange, trait);
        }

        /// <summary>四神聚合的一条**输入**（= `.c` 遍历到的一条 mark 记录）。</summary>
        public struct MarkInput
        {
            /// <summary>`CardEffect.markOfChaosType(+0x78)`。</summary>
            public int MarkOfChaosType;
            /// <summary>`CardEffect.traitValue(+0x40)`。</summary>
            public int TraitValue;
        }

        /// <summary>四神聚合的一条**输出**（= `CreateMarkCardEffect` 那条：`buffType = 8`、`trait = 0`）。</summary>
        public struct MarkAggregate
        {
            /// <summary>归属的神（`10/20/30/40`）。</summary>
            public int MarkOfChaosType;
            /// <summary>同神多条累加后的 `traitValue`。</summary>
            public int TraitValue;
            /// <summary>该神在输入里的**首现下标**（调用方据此把聚合条插回原序的对应位置 —— `.c` 的「占位 = 首现位置」）。</summary>
            public int FirstIndex;
        }

        /// <summary>四神聚合（`.c:47-130`）：按 <paramref name="input"/> 的**给定顺序**，
        /// 每个神**首次出现处**占位、同神 <c>traitValue</c> **累加**；`MarkGodSlot == -1`（none/Random）**丢弃且不占位**。
        /// <para>顺序 = 输入 List 的 `Add` 序、不排序、不截断。返回表按「四神首现序」。</para>
        /// <para>⚠️ 输出条目的 `buffType` 恒 8、`trait` 恒 0（见施工细则 §1.3 的 `CreateMarkCardEffect` 注）；
        /// 与**普通效果**的交错位置由调用方按 <see cref="MarkAggregate.FirstIndex"/> 在合并时保持
        /// （本方法只管四神内部合并，不掺普通效果 —— 那属于 7 源投影，不在这里）。</para></summary>
        public static List<MarkAggregate> AggregateMarks(IReadOnlyList<MarkInput> input)
        {
            var outp = new List<MarkAggregate>();
            if (input == null) return outp;
            int[] slot = { -1, -1, -1, -1 };
            for (int i = 0; i < input.Count; i++)
            {
                int g = MarkGodSlot(input[i].MarkOfChaosType);
                if (g < 0) continue;                       // none/Random ⇒ continue（丢弃、不占位）
                if (slot[g] < 0)
                {
                    slot[g] = outp.Count;
                    outp.Add(new MarkAggregate { MarkOfChaosType = input[i].MarkOfChaosType, TraitValue = 0, FirstIndex = i });
                }
                var a = outp[slot[g]];
                a.TraitValue += input[i].TraitValue;
                outp[slot[g]] = a;
            }
            return outp;
        }

        // ============================================================================================
        //  S1 · `Of()` —— 原版 `EntityScript.activeEffects(+0x108)` 的 **7 源只读投影**（账 `特效动画:521`）
        //
        //  判据（⛔ 别在这里抄第二份）：`判定/GetCardEffectsToShow_施工细则.md` §三 S1 那张「逐族」表
        //    ＋ `d:/2/tools/decomp_full/EntityScript__GetCardEffectsToShow.c` / `CardEffectItem__LoadEffect.c`。
        //
        //  🔴 **本方法是纯读**：不改任何 `UnitState` 字段、不发事件、不写 `ctx.Log`（除非调用方给了 `ctx`
        //     且遇到**认不出的载荷**那种必须出声的场合）⇒ `ruleprobe play` 指纹必须逐字节不变。
        //
        //  ⚠️ **今天做不到的两段（别当成已做）**：
        //    · 附魔行 / 手牌行的 `Seq`：`ActiveCardEffect` 与 `HandEffect` 上还没有序戳
        //      （施工细则 S3-d / S3-f，落点在 `Enchantment.cs` / `CardInstance.cs` / `RuleCore.cs`，**不在本切片白名单**）
        //      ⇒ 它们 `Seq = 0`、排到队尾（见 `SortShown`）。`RemoveTrait` 是 `HashSet`（无序无戳）同理。
        //    · `RowGroup`（同一次 `give` 多条合成一行，S3-c）：`EffectResolver.DoGive` 本波在白名单内，但**生产端盖不了戳** ——
        //      `UnitState.StatMod` **没有 `RowGroup` 字段**、`AddStatMod` 也没有这个形参（两者都在 `UnitState.cs`，不属本代理）⇒ 今天恒 `0`（= 不合并）。⛔ 别只在读取端（本文件）凑一个组号：那样同一次 give 的多条属性照样各印一行。
        // ============================================================================================

        /// <summary>`BuffType.removeTrait = 4`（`dump.cs:45430-45462`）。</summary>
        public const int BuffTypeRemoveTrait = 4;

        /// <summary>
        /// **原版 `activeEffects(+0x108)` 的等价投影**：**全量、不截断、不排 5 槽**（槽位是表现侧 S4 的事）。
        /// <para>`isPlayer` = 「被展示那张卡是不是我方的」（原版 `CardEffect.originalIsPlayer(+0xA0)`）——
        /// 投影本身不用它，透传给表现侧做 secret/quest 打码（判据 `CardEffectItem__LoadEffect.c:56-70`）。</para>
        /// <para>`ctx` 只用于「认不出的手牌载荷」出声（⛔ 不是显隐闸）。</para>
        /// </summary>
        public static List<ShownEffect> Of(UnitState u, bool isPlayer, BattleContext ctx = null)
        {
            var outp = new List<ShownEffect>();
            if (u == null) return outp;

            // 原版 `DisplayCardEffects` 的第二道过滤要问「卡上有没有任务附魔」（`HasQuests`）
            bool hasQuests = u.Instance != null && Enchantments.CountQuests(u.Instance) > 0;

            var marks = new List<MarkSrc>();

            // ---- ① Stat（永久 / 覆盖）：`_statMods` --------------------------------------------------
            //   ⚠️ **跳过被某条 `TempBuff` 引用住的那几条** —— `AddTempBuff` 会建同一条修饰
            //      （`UnitState.cs:1684`），限时那一份从 ② 出（`Timed = true`），否则同一次施加会印两行。
            //   ⚠️ `Source == Auras.GrantTag` 的**不跳过**：光环那份在原版 `+0x108` 里本来就有（施工细则 A6）。
            var timedMods = new HashSet<UnitState.StatMod>();
            var buffs = u.TempBuffs;
            for (int i = 0; i < buffs.Count; i++)
            {
                var b = buffs[i];
                if (b != null && b.Mod != null) timedMods.Add(b.Mod);
            }
            var mods = u.StatMods;
            for (int i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                if (m == null) continue;
                if (timedMods.Contains(m)) continue;
                if (IsNonStatAttr(m.Attr)) continue;
                outp.Add(new ShownEffect
                {
                    Family = ShownFamily.Stat,
                    BuffType = m.BuffType,
                    Attr = m.Attr,
                    Value = m.Value,
                    IsSet = m.IsSet,
                    Timed = false,
                    Source = m.Source,
                    Seq = m.Seq,
                });
            }

            // ---- ② Stat / Trait（限时）：`TempBuffs` ------------------------------------------------
            for (int i = 0; i < buffs.Count; i++)
            {
                var b = buffs[i];
                if (b == null) continue;
                string src = SourceOfTempBuff(b);
                if (b.IsKeyword)
                {
                    string trait = b.TraitRec != null ? b.TraitRec.Trait : b.Name;
                    if (string.IsNullOrEmpty(trait)) continue;
                    if (trait == KeywordTable.MarkOfChaos)
                    {
                        // 限时 mark 也进四神聚合（`ApplyMarkBody` 今天不带时长 ⇒ 池内 0 条；防御性地收）
                        marks.Add(new MarkSrc
                        {
                            Input = new MarkInput
                            {
                                MarkOfChaosType = b.TraitRec != null ? b.TraitRec.MarkOfChaosType : 0,
                                TraitValue = b.TraitValue,
                            },
                            Seq = b.Seq,
                            Source = src,
                        });
                        continue;
                    }
                    if (IsExcludedOathTrait(trait)) continue;
                    outp.Add(new ShownEffect
                    {
                        Family = ShownFamily.Trait,
                        BuffType = b.BuffType,
                        Trait = trait,
                        TraitValue = b.TraitValue,
                        MarkOfChaosType = b.TraitRec != null ? b.TraitRec.MarkOfChaosType : 0,
                        Timed = true,
                        Source = src,
                        Seq = b.Seq,
                    });
                }
                else
                {
                    if (IsNonStatAttr(b.Name)) continue;
                    outp.Add(new ShownEffect
                    {
                        Family = ShownFamily.Stat,
                        BuffType = b.Mod != null ? b.Mod.BuffType : BuffTypeAddStats,
                        Attr = b.Name,
                        Value = b.Value,
                        IsSet = b.Mod != null && b.Mod.IsSet,
                        Timed = true,
                        Source = src,
                        Seq = b.Seq,
                    });
                }
            }

            // ---- ③ Trait / Mark：`_traitEffects`（`addTrait(3)` / `addTraitRemnant(90)` 那一族）--------
            var recs = u.TraitEffects;
            for (int i = 0; i < recs.Count; i++)
            {
                var r = recs[i];
                if (r == null) continue;
                if (r.Trait == KeywordTable.MarkOfChaos)
                {
                    marks.Add(new MarkSrc
                    {
                        Input = new MarkInput { MarkOfChaosType = r.MarkOfChaosType, TraitValue = r.TraitValue },
                        Seq = r.Seq,
                        Source = r.Source,
                    });
                    continue;
                }
                if (IsExcludedOathTrait(r.Trait)) continue;
                outp.Add(new ShownEffect
                {
                    Family = ShownFamily.Trait,
                    BuffType = r.BuffType,
                    Trait = r.Trait,
                    TraitValue = r.TraitValue,
                    MarkOfChaosType = r.MarkOfChaosType,
                    // 原版「一回合」= 五个 `until*` 位任一为真 ⇒ 我们把那五位压进了 `TimingBits` 的 bit0..4
                    Timed = (r.TimingBits & 0x1F) != 0,
                    Source = r.Source,
                    Seq = r.Seq,
                });
            }

            // ---- ④ Trait（光环给的关键词）：`AuraKeywords` -------------------------------------------
            foreach (var kv in u.AuraKeywords)
            {
                if (kv.Value <= 0) continue;
                if (IsExcludedOathTrait(kv.Key)) continue;
                outp.Add(new ShownEffect
                {
                    Family = ShownFamily.Trait,
                    BuffType = BuffTypeAddTrait,
                    Trait = kv.Key,
                    TraitValue = kv.Value,
                    Timed = false,
                    Source = Auras.GrantTag,
                    Seq = u.AuraKeywordSeq(kv.Key),
                });
            }

            // ---- ⑤ RemoveTrait：`RemovedTraits`（`HashSet` ⇒ 无序、无戳 ⇒ 排队尾，见施工细则 §六·4）----
            foreach (var t in u.RemovedTraits)
            {
                if (string.IsNullOrEmpty(t)) continue;
                outp.Add(new ShownEffect
                {
                    Family = ShownFamily.RemoveTrait,
                    BuffType = BuffTypeRemoveTrait,
                    Trait = t,
                    Seq = 0,
                });
            }

            var inst = u.Instance;
            if (inst != null)
            {
                // ---- ⑥ Enchantment：`CardInstance.ActiveEffects`（附魔 12 / 15 / 16）--------------------
                //   判据 `CardEffectItem__LoadEffect.c` + `GameStaticData.GetEffectDesc.c:388-396`：
                //   这三档的右列**是空串** ⇒ 原版这里就是一个**洞**（`EnableEffects` 空文本也占一槽）。
                for (int i = 0; i < inst.ActiveEffects.Count; i++)
                {
                    var e = inst.ActiveEffects[i];
                    if (e == null) continue;
                    outp.Add(new ShownEffect
                    {
                        Family = ShownFamily.Enchantment,
                        BuffType = e.BuffType,
                        Trait = e.Trait,
                        SecretBuff = e.SecretBuff,
                        QuestBuff = e.QuestBuff,
                        Source = e.Source,
                        // 原版 `GetEffectDesc` 的「附魔且 `spellType == 120`」支印**那张附魔卡的卡面描述**
                        Body = e.EnchantmentCard != null ? e.EnchantmentCard.Desc : null,
                        Seq = 0,
                    });
                }

                // ---- ⑦ HandBuff：`CardInstance.HandEffects`（`isHandBuff` 那一族）-----------------------
                //   ⚠️ **可达性如实写**：我们的展示窗**只对场上单位开**（`BattleDriver.cs:13062-13072`），
                //      而手牌效果是「打出时兑现即摘」（`Enchantment.cs` 文件头 + `ApplyHandBuffs` 的
                //      `ClearHandEffect`）⇒ 场上单位身上通常**已经没有** HandEffects ⇒ 今天这一段基本不出行。
                //      仍然照原版 `+0x108` 建它（那张牌「在手里」时原版确实有这几条）。
                for (int i = 0; i < inst.HandEffects.Count; i++)
                {
                    var he = inst.HandEffects[i];
                    if (he == null || he.Op == null) continue;
                    var ops = GivePayload.Parse(he.Op.Payload);
                    if (ops == null)
                    {
                        // 不静默：载荷认不出就说出来（施工细则 §六·8）。⛔ 不猜一个属性填进去。
                        LogOut(ctx, $"[效果投影] 手牌效果「{he.Op.Payload}」（来源 {he.Source}）本条解析不了 ⇒ 这一条不出行");
                        continue;
                    }
                    bool timed = he.Expire != CardInstance.HandBuffExpiry.Never;
                    for (int k = 0; k < ops.Count; k++)
                    {
                        var p = ops[k];
                        if (p.IsKeyword)
                        {
                            if (IsExcludedOathTrait(p.Keyword)) continue;
                            outp.Add(new ShownEffect
                            {
                                Family = ShownFamily.Trait,
                                BuffType = BuffTypeAddTrait,
                                Trait = p.Keyword,
                                TraitValue = p.Value,
                                Timed = timed,
                                Source = he.Source,
                                Seq = 0,
                            });
                        }
                        else if (!string.IsNullOrEmpty(p.Attr))
                        {
                            if (IsNonStatAttr(p.Attr)) continue;
                            outp.Add(new ShownEffect
                            {
                                Family = ShownFamily.Stat,
                                BuffType = BuffTypeAddStats,
                                Attr = p.Attr,
                                Value = p.Value,
                                Timed = timed,
                                Source = he.Source,
                                Seq = 0,
                            });
                        }
                        else
                        {
                            // 施工细则 §六·8：本轮只出 `addStats` / `addTrait` 两类，其余**出声跳过**
                            LogOut(ctx, $"[效果投影] 手牌效果「{he.Op.Payload}」里有一段不是属性/关键词"
                                      + $"（{p.Embedded ?? p.Resource ?? p.ToString()}）⇒ 这一段不出行");
                        }
                    }
                }
            }

            // ---- ⑧ Mark 四神聚合（复用 `AggregateMarks`：**判据只此一处**）--------------------------
            if (marks.Count > 0)
            {
                // 原版的「占位 = 该神第一次出现的位置」按 `+0x108` 的 `List.Add` 序 ⇒ 先把输入按序戳排好
                var order = new List<int>(marks.Count);
                for (int i = 0; i < marks.Count; i++) order.Add(i);
                order.Sort((x, y) => RankOfSeq(marks[x].Seq).CompareTo(RankOfSeq(marks[y].Seq)));
                var input = new List<MarkInput>(marks.Count);
                for (int i = 0; i < order.Count; i++) input.Add(marks[order[i]].Input);
                var aggs = AggregateMarks(input);
                for (int i = 0; i < aggs.Count; i++)
                {
                    var m = marks[order[aggs[i].FirstIndex]];
                    outp.Add(new ShownEffect
                    {
                        Family = ShownFamily.Mark,
                        BuffType = BuffTypeMarkOfChaos,
                        MarkOfChaosType = aggs[i].MarkOfChaosType,
                        TraitValue = aggs[i].TraitValue,
                        Timed = false,
                        Source = m.Source,
                        Seq = m.Seq,
                    });
                }
            }

            // ---- ⑨ 第二道过滤（`CardDisplayWindow.DisplayCardEffects` 那道）----------------------------
            //   判据**只此一处** = `UnitState.IsEffectListEntry`（changeRace(6) / addRace(30) / daemonHost(35)
            //   永不出行；`choiceEnchantment(16)` 只在卡上有任务附魔时出行）。⛔ 别在这里再写一遍那四条。
            for (int i = outp.Count - 1; i >= 0; i--)
                if (!UnitState.IsEffectListEntry(outp[i].BuffType, hasQuests)) outp.RemoveAt(i);

            // ---- ⑩ 序（见 `SortShown`）--------------------------------------------------------------
            SortShown(outp);
            return outp;
        }

        /// <summary>`BuffType.addStats = 1`（`dump.cs:45430-45462`）。</summary>
        public const int BuffTypeAddStats = 1;

        /// <summary>四神聚合的一条输入 + 它自己的序戳/来源（聚合后 <see cref="MarkAggregate.FirstIndex"/> 回查）。</summary>
        struct MarkSrc
        {
            public MarkInput Input;
            public int Seq;
            public string Source;
        }

        /// <summary>「队尾」用的序：`Seq &lt;= 0`（没有序戳：RemoveTrait / 附魔 / 手牌）排到最后。</summary>
        static int RankOfSeq(int seq) { return seq > 0 ? seq : int.MaxValue; }

        /// <summary>
        /// 把投影排成「原版 `+0x108` 的 `List.Add` 序」。
        ///
        /// <para>🔶 自己设计（非原版）：`Seq` 相同时的族内次序（`Family` → `Trait/Attr` 名 → `BuffType` → 值），
        /// 以及「没有序戳的族（RemoveTrait / 附魔 / 手牌）排到队尾」。**为什么必须自己定**：原版只有**一条**
        /// `List&lt;CardEffect&gt;`、序天然存在；我们把它拆成了 7 本账（`_statMods` / `_tempBuffs` /
        /// `_traitEffects` / `_auraKw` / `_removedTraits` / `ActiveEffects` / `HandEffects`），
        /// 后三本**今天没有序戳**（S3-d / S3-f 在白名单外）⇒ 跨族次序只能由我们给一条**全序**
        /// （`List.Sort` **不稳定**，⛔ 只比 `Seq` 会让相等项的顺序随实现漂移）。
        /// · 调度台 2026-10-29 裁定（承施工细则 §三 S1 的同一条要求）。</para>
        /// </summary>
        static void SortShown(List<ShownEffect> list)
        {
            if (list == null || list.Count < 2) return;
            list.Sort((x, y) =>
            {
                int c = RankOfSeq(x.Seq).CompareTo(RankOfSeq(y.Seq));
                if (c != 0) return c;
                c = ((int)x.Family).CompareTo((int)y.Family);
                if (c != 0) return c;
                c = string.CompareOrdinal(NameOf(x), NameOf(y));
                if (c != 0) return c;
                c = x.MarkOfChaosType.CompareTo(y.MarkOfChaosType);
                if (c != 0) return c;
                c = x.BuffType.CompareTo(y.BuffType);
                if (c != 0) return c;
                return x.Value.CompareTo(y.Value);
            });
        }

        static string NameOf(ShownEffect e)
        {
            return !string.IsNullOrEmpty(e.Trait) ? e.Trait : (e.Attr ?? "");
        }

        /// <summary>`TempBuff` 的「谁给的」—— 原版 `CardEffect.effectName(+0x20)` 优先、空才退施加卡名
        /// （`CardEffectItem__LoadEffect.c:110-140`）。我们三格依次退：`EffectName` → `SourceCard` → `Src`。</summary>
        static string SourceOfTempBuff(UnitState.TempBuff b)
        {
            if (!string.IsNullOrEmpty(b.DisplaySourceName)) return b.DisplaySourceName;
            if (!string.IsNullOrEmpty(b.SourceCard)) return b.SourceCard;
            return b.Src;
        }

        /// <summary>`energy` / 空 —— 原版 `GetEffectDesc` 的 `case 1 addStats` **只认四项**
        /// （melee / ranged / health / armour），`+1 energy` 那条根本不进「受到以下影响」这张表。</summary>
        static bool IsNonStatAttr(string attr)
        {
            if (string.IsNullOrEmpty(attr)) return true;
            string a = RuleCore.NormalizeAttr(attr);
            return a != "attack" && a != "ranged" && a != "health" && a != "armour";
        }

        /// <summary>
        /// 原版 `addTrait(3)` 的两条 oath 排除档（`GetCardEffectsToShow.c:146-164`，`trait ∈
        /// {oathInAllTurns 1276, oathDouble 1277, oathTripleActivation 1278}`）。
        ///
        /// <para>⚠️ **它今天是防御性的（不可达），别当成已验收**：我们的记录账只存**关键词名**
        /// （`TraitEffectRecord.Trait` 是 string），而这三条 `DefinedTrait` **在池内没有任何关键词名对应**
        /// —— 它们被建模成 `CardDef.OathDouble` / `OathTripleActivation` / `OathInAllTurns` 三个 **bool**
        /// （`CardDef.cs:4310-4348`），进 `_traitEffects` 的是「授给友军」的那两份（`Auras.Recompose` 的
        /// ②·b 誓约授予层，落到 `UnitState.OathTripleGranted` 布尔上、**同样不进记录账**）。
        /// ⇒ 下表是**按 `DefinedTrait` 名的小写拼法**建的兜底名表（与 `KeywordTable` 全小写规范一致）；
        /// 池内 `addTrait` 那几档**一条都命中不到**。断言 #11 用注入记录（`ApplyTraitEffect("oathdouble", 1)`）验它。</para>
        /// </summary>
        static readonly string[] OathTraitNames = { "oathinallturns", "oathdouble", "oathtripleactivation" };

        /// <summary>见 <see cref="OathTraitNames"/>（`.c:146-164` 那三条排除档）。</summary>
        public static bool IsExcludedOathTrait(string trait)
        {
            if (string.IsNullOrEmpty(trait)) return false;
            string t = trait.Replace(" ", "").Replace("_", "").ToLowerInvariant();
            for (int i = 0; i < OathTraitNames.Length; i++)
                if (t == OathTraitNames[i]) return true;
            return false;
        }

        /// <summary>投影里的「认不出」出声口 —— `ctx` 没给时退 `UnityEngine` 那一路是**不允许**的
        /// （本文件属 `Core/`、不依赖 `UnityEngine`）⇒ 只写 `ctx.Log`；`ctx == null`（自检/探针）时静默，
        /// 由调用方的断言显式覆盖。</summary>
        static void LogOut(BattleContext ctx, string msg)
        {
            if (ctx != null) ctx.Log(msg);
        }
    }

    /// <summary>投影里一条效果属于哪一族（施工细则 §三 S1）。</summary>
    public enum ShownFamily
    {
        /// <summary>属性增减 / 覆盖（`buffType 1 addStats` · `70/85/9/95 set*`）。</summary>
        Stat = 0,
        /// <summary>`addTrait(3)` / `addTraitRemnant(90)` / 光环给的关键词。</summary>
        Trait = 1,
        /// <summary>`removeTrait(4)`。</summary>
        RemoveTrait = 2,
        /// <summary>`markOfChaos(8)`（四神各聚一条）。</summary>
        Mark = 3,
        /// <summary>附魔那一族（`buffType 12/15/16`，原版右列空 ⇒ 占一个「洞」）。</summary>
        Enchantment = 4,
        /// <summary>手牌加成那一族（`isHandBuff`）。⚠️ 投影**不产出**它 —— 手牌那条按原版映射成
        /// `Stat` / `Trait` 行（施工细则 §三 S1 表最后一行）；本成员是给消费端分类用的保留档。</summary>
        HandBuff = 5,
    }

    /// <summary>
    /// 原版 `CardEffect`（`EntityScript.activeEffects(+0x108)` 里的一条）的**只读投影**。
    /// 字段偏移 → `dump.cs:119060-119100`；逐字段含义 → `判定/GetCardEffectsToShow_施工细则.md` §1.2。
    /// </summary>
    public struct ShownEffect
    {
        /// <summary>哪一族（决定表现侧走哪一支文本）。</summary>
        public ShownFamily Family;
        /// <summary>原 `+0x38 buffType`。</summary>
        public int BuffType;
        /// <summary>原 `+0x3c trait`（我们的关键词名；`null`/`""` = 无）。</summary>
        public string Trait;
        /// <summary>原 `+0x40 traitValue`。</summary>
        public int TraitValue;
        /// <summary>`addStats` / `set*` 的哪一项：`attack` / `ranged` / `health` / `armour`。</summary>
        public string Attr;
        /// <summary>`addStats` 的增量 / `set*` 的**绝对值**。</summary>
        public int Value;
        /// <summary>原 `+0x78 markOfChaosType`（`0 none` / `10` / `20` / `30` / `40` / `50 Random`）。</summary>
        public int MarkOfChaosType;
        /// <summary>原五个 `until*` 任一为真（⇒ 文本走「…一回合」那一档）。</summary>
        public bool Timed;
        /// <summary>原 `+0x90 secretBuff`。</summary>
        public bool SecretBuff;
        /// <summary>原 `+0x91 questBuff`。</summary>
        public bool QuestBuff;
        /// <summary>左列（谁给的）。`null`/`""` = 原版那一支留白。</summary>
        public string Source;
        /// <summary>原 `+0x28 effectText`（非空 ⇒ 右列**优先用它**）；附魔行放的是那张附魔卡的描述。</summary>
        public string Body;
        /// <summary>加序戳（S2）。`0` = 这一族还没有序戳 ⇒ 排到队尾（见 `EffectsToShow.SortShown`）。</summary>
        public int Seq;
        /// <summary>同一次 `give` 的合并组（S3-c 的生产点接线前**恒 0** ⇒ 不合并）。</summary>
        public int RowGroup;
        /// <summary>原 `buffType 70/85/9` 那三档（**覆盖**语义，不是增量）。
        /// 🆕 偏离施工细则的结构草图：S1 表 A3 那两档要靠 `StatMod.IsSet` 才判得出，草图里漏了这一格。</summary>
        public bool IsSet;
    }
}
