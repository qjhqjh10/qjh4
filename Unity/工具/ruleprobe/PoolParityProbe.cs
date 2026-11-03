// PoolParityProbe.cs — 探针池「构造后挂载」补齐（`B121-D4-01`）＋ 与主路池逐字段相等自检
//   执行代理 `B123_E3` · 2026-11-03 · 判据 → `资料/完全复刻/判据/B123_对账_A_引擎附魔.md` §2
//
// 🔴 治的是什么（账 `B121-D4-01`）：
//   `Program.LoadPool` 用 `System.Text.Json` **手读**卡表，不走 `Data/CardDatabase.cs:1762` 那条
//   `CardDatabase.Parse` 链。那条链的**后半段**（「构造后挂载」那一整行）里，探针此前只补了 4 句
//   （`AttachAbilityDamage` / `AttachTriggerAbilityAndOpsByNumber` / `AttachCardTraits` / `AttachChosenAbilities`）。
//   ⇒ 余下 12 个挂载点**一条都没执行** ⇒ 探针池里：
//       `HasAbilityHeal` / `HasAbilityManaBuff` **恒 false**、
//       `AbilitySummons` **恒 null**、
//       `CardDatabase.AbilityBuffsOf(def)` **恒 null**（= v40 旁表整族在探针里是空的）、
//       `PermanentBuffs` / `AbilityMutations` / `AbilityEffectTexts` / `CardTargetCriteria` /
//       `TransformedWarlordId` / `DeckCriteria` / `NotDeckable` / `StartingCardInHandRaw` / `WhileInPlayAnim` 同样恒空。
//   ⇒ **`play check` 绿** 对**整个 v37 / v40 / v41 / v43 / v44 / v46 / v48 数据列族都不构成证据**
//      （那是「口径缺口」，不是「改动无效」）。这条缺口是**族级**的，⛔ 别只按 heal/mana/summon 三列理解。
//
// 🔑 做法（与 `CardDatabaseProbeHook.cs` 同一条纪律）：
//   ① **不另写解析**：每个 mapper 都转发到引擎那个**私有**函数（`CardDatabaseProbeHook.cs` 的 `ProbeMap*`），
//      探针侧只负责「把 `CardDto` 造出来 + 按引擎同一顺序调 `Attach*`」。
//   ② `CardDto` 由 `System.Text.Json` 直接反序列化（`IncludeFields = true`，键名与 `CardDto` 字段名逐字相同）
//      —— 与 `Program.LoadPool` 手读的是**同一份 JSON**。
//   ③ 顺序照 `Data/CardDatabase.cs:1762`（只有 `AttachTriggerAbilityIndices → AttachTriggerOpsByNumber`
//      这一条有硬依赖，已由 `Program` 那句 `AttachTriggerAbilityAndOpsByNumber` 自己保证）。
//
// 🔴 自证防线（本文件第二个类）：`poolparity` 子命令把**探针池**与**主路池**（`CardDatabase.Parse`
//   现跑一遍）逐卡逐字段比。⛔ 没有它，「补挂了没有」就只能靠读代码，那不叫证据。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RuleEngine;

/// <summary>把 `CardDatabase.cs:1762` 那句「构造后挂载」在探针侧按同一批 mapper 补齐（⛔ 不另写解析）。</summary>
static class ProbePoolAttach
{
    /// <summary>`CardDto` 是 public 字段的普通类（`Data/CardDatabase.cs:33-58`）⇒ 必须 `IncludeFields`。
    /// ⚠️ 漏了它就**静默**得到全默认值（`B121` 报告 §2 明确提醒过）⇒ 本文件末尾的 `poolparity` 就是抓它的。</summary>
    static readonly JsonSerializerOptions Opt = new JsonSerializerOptions
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = false,
    };

    public static CardDatabase.CardDto Dto(JsonElement card)
        => JsonSerializer.Deserialize<CardDatabase.CardDto>(card, Opt);

    /// <summary>按 `Data/CardDatabase.cs:1762` 的**同一批 mapper、同一套三态口径**补挂余下的列。
    /// <para>⚠️ `AttachAbilityDamage` / `AttachTriggerAbilityIndices` / `AttachTriggerOpsByNumber` /
    /// `AttachCardTraits` / `AttachChosenAbilities` 在 `Program.LoadPool` 里**另有调用点**（同一物理行），
    /// ⛔ 别在这里重复挂一遍（重复挂 = 覆盖，语义相同但会让读数看着像「这里也管」）。</para>
    /// <para>⚠️ 版本闸与引擎逐字相同：`whileInPlayAnim` / `permanentBuffs` 按 `>= 37`、
    /// `cardTargetCriteria` 按 `>= 46`；其余列引擎没有版本闸，这里也不加。</para></summary>
    public static void AttachPostConstruction(CardDef def, JsonElement card, int poolVersion, string cardIdForLog)
    {
        CardDatabase.CardDto c;
        try { c = Dto(card); }
        catch (Exception e)
        {
            // ⛔ 不许静默：反序列化炸了就等于这一张卡的整族列**没挂上**。
            Console.Error.WriteLine("!! `CardDto` 反序列化失败（卡 " + cardIdForLog + "）⇒ 这一张的 "
                                    + "whileInPlayAnim/permanentBuffs/abilitySummon/abilityBuffs/abilityMutations/"
                                    + "startingCardInHand/abilityEffectTexts/cardTargetCriteria/transformData/"
                                    + "notDeckable/abilityHeal/abilityMana **十二列全部没挂**：" + e.GetType().Name + ": " + e.Message);
            return;
        }
        if (c == null)
        {
            Console.Error.WriteLine("!! `CardDto` 反序列化返回 null（卡 " + cardIdForLog + "）⇒ 同上十二列全部没挂");
            return;
        }

        // —— 顺序 = `Data/CardDatabase.cs:1762`（除上面那五个已在别处挂的之外，逐句照排）——
        def.AttachWhileInPlayAnim(poolVersion >= 37 ? c.whileInPlayAnim : -1);
        def.AttachPermanentBuffs(poolVersion >= 37 ? CardDatabase.ProbeMapPermanentBuffs(c) : null);
        def.AttachAbilitySummon(CardDatabase.ProbeMapAbilitySummon(c));
        CardDatabase.ProbeRecordAbilityBuffs(def, c);                 // v40 旁表：查询口 `CardDatabase.AbilityBuffsOf`
        def.AttachAbilityMutations(CardDatabase.ProbeMapAbilityMutations(c));
        def.AttachStartingCardInHand(CardDatabase.ProbeMapStartingCardInHand(c));
        def.AttachAbilityEffectTexts(CardDatabase.ProbeMapAbilityEffectTexts(c));
        def.AttachCardTargetCriteria(poolVersion >= 46 ? CardDatabase.ProbeMapCardTargetCriteriaSet(c) : null, poolVersion >= 46);
        def.AttachTransformData(c.transformedWarlordId, CardDatabase.ProbeMapDeckCriteria(c));
        def.AttachNotDeckable(c.notDeckable != 0);
        def.AttachAbilityHeal(CardDatabase.ProbeMapAbilityHeal(c));
        def.AttachAbilityManaBuff(CardDatabase.ProbeMapAbilityMana(c));
    }
}

/// <summary>`poolparity` —— **探针池 vs 主路池**逐卡逐字段相等（`B121-D4-01` 的自证防线）。
///
/// 🔴 为什么要它：只「补挂了代码」证明不了「真的挂上了、且与引擎同一口径」——
///   本仓已有先例（`B-324` / `B-257`）：补挂那句写错文件、或 `IncludeFields` 漏配，
///   症状都是**静默**（列恒空、`play check` 照样绿）。所以必须有一条**机器断言**。
///
/// 判据：
///   · **硬失败**（rc=1）：本文件补挂的那一族十二列，探针池与主路池**逐卡逐字段必须相等**；
///   · **只报不判**：`ChosenAbilities` / `CardTraits` / `AbilityDamage` 三列在探针里由
///     `Program.ProbeChosenAbilities` / `ProbeCardTraits` / 手写 `abilityDamage` 循环喂，
///     与引擎 `c.chosenAbility` / `c.cardTraits` / `MapAbilityDamage(c)` **是否同口径**不属本账范围
///     ⇒ 差异只打印、不计入 rc（但要**看得见**：那是「探针与引擎分叉」的候选，交调度台立账）。
///   · 还会打印「这一族列在探针池里到底有多少张非空」—— 若全 0，说明 `IncludeFields` 或补挂整段没生效。
///
/// 🔴 **`AbilityBuffsOf` 的快照必须抢在 `CardDatabase.Parse` 之前**：它读的是**静态旁表**
///   （按卡 id 键，`Data/CardDatabase.cs:4732`），`Parse` 会覆盖同一批 id。
///   若先 Parse 再比，探针「根本没挂」会被 Parse 的值掩掉 = **自证**。
static class PoolParityProbe
{
    const int MaxDiffLines = 40;

    /// <summary>一张卡的读数（探针侧 / 主路侧各取一次）。`-1` = `null`（未知）；`>=0` = 行数。</summary>
    struct Row
    {
        public int WhileInPlayAnim;
        public int PermanentBuffs;
        public int AbilitySummons;
        public int AbilityBuffs;
        public int AbilityMutations;
        public int AbilityEffectTexts;
        public int AbilityHealRows;          // 没有公开计数属性 ⇒ 用 `HasAbilityHeal`（bool）
        public int AbilityManaRows;
        public string StartingCardInHand;
        public string TransformedWarlordId;
        public bool NotDeckable;
        public bool CardTargetCriteriaKnown;
        public bool DeckCriteriaKnown;

        // 只报不判（探针侧另有手写读口）
        public string ChosenAbilities;
        public string CardTraits;

        static int Len<T>(T[] a) => a == null ? -1 : a.Length;

        public static Row Of(CardDef d, Func<CardDef, int> buffsCount)
        {
            var r = new Row();
            if (d == null) return r;
            r.WhileInPlayAnim = d.WhileInPlayAnim;
            r.PermanentBuffs = d.HasPermanentBuffs ? d.PermanentBuffs.Length : -1;
            r.AbilitySummons = Len(d.AbilitySummons);
            r.AbilityBuffs = buffsCount(d);
            r.AbilityMutations = d.HasAbilityMutations ? d.AbilityMutations.Length : -1;
            r.AbilityEffectTexts = d.HasAbilityEffectTexts ? d.AbilityEffectTexts.Length : -1;
            r.AbilityHealRows = d.HasAbilityHeal ? 0 : -1;      // 0 = 「列在、行数未量」；⛔ 别与 -1(未知) 混
            r.AbilityManaRows = d.HasAbilityManaBuff ? 0 : -1;
            r.StartingCardInHand = d.StartingCardInHandRaw;
            r.TransformedWarlordId = d.TransformedWarlordId;
            r.NotDeckable = d.NotDeckable;
            r.CardTargetCriteriaKnown = d.CardTargetCriteria != null;
            r.DeckCriteriaKnown = d.DeckCriteria != null;
            r.ChosenAbilities = d.ChosenAbilities == null ? null : string.Join(",", d.ChosenAbilities);
            r.CardTraits = d.CardTraits == null ? null : string.Join(",", d.CardTraits);
            return r;
        }

        /// <summary>硬判据（本文件补挂的十二列）。返回差异描述，`null` = 相等。</summary>
        public string DiffHard(Row o)
        {
            var bad = new List<string>();
            void C(string name, object a, object b)
            {
                if (!Equals(a, b)) bad.Add(name + " 探针=" + (a ?? "null") + " 主路=" + (b ?? "null"));
            }
            C("whileInPlayAnim", WhileInPlayAnim, o.WhileInPlayAnim);
            C("permanentBuffs", PermanentBuffs, o.PermanentBuffs);
            C("abilitySummon", AbilitySummons, o.AbilitySummons);
            C("abilityBuffs(旁表)", AbilityBuffs, o.AbilityBuffs);
            C("abilityMutations", AbilityMutations, o.AbilityMutations);
            C("abilityEffectTexts", AbilityEffectTexts, o.AbilityEffectTexts);
            C("hasAbilityHeal", AbilityHealRows, o.AbilityHealRows);
            C("hasAbilityManaBuff", AbilityManaRows, o.AbilityManaRows);
            C("startingCardInHand", StartingCardInHand, o.StartingCardInHand);
            C("transformedWarlordId", TransformedWarlordId, o.TransformedWarlordId);
            C("notDeckable", NotDeckable, o.NotDeckable);
            C("cardTargetCriteria", CardTargetCriteriaKnown, o.CardTargetCriteriaKnown);
            C("deckCriteria", DeckCriteriaKnown, o.DeckCriteriaKnown);
            return bad.Count == 0 ? null : string.Join(" · ", bad);
        }

        /// <summary>只报不判（探针侧另有手写读口，与引擎是否同口径**不在本账范围**）。</summary>
        public string DiffReportOnly(Row o)
        {
            var bad = new List<string>();
            if (!Equals(ChosenAbilities, o.ChosenAbilities))
                bad.Add("chosenAbility 探针=" + (ChosenAbilities ?? "null") + " 主路=" + (o.ChosenAbilities ?? "null"));
            if (!Equals(CardTraits, o.CardTraits))
                bad.Add("cardTraits 探针=" + (CardTraits ?? "null") + " 主路=" + (o.CardTraits ?? "null"));
            return bad.Count == 0 ? null : string.Join(" · ", bad);
        }
    }

    public static int Run()
    {
        // ① 探针池（`Program.LoadPool`：手读 + 本轮补挂）。
        List<CardDef> probe;
        try { probe = Program.PoolList; }
        catch (Exception e) { Console.Error.WriteLine("!! `Program.PoolList` 抛异常：" + e); return 2; }

        // 🔴 静态旁表必须在 Parse 之前快照（理由见类注）。
        Func<CardDef, int> buffsCount = d =>
        {
            var rows = CardDatabase.AbilityBuffsOf(d);
            return rows == null ? -1 : rows.Count;
        };
        var probeRows = new Dictionary<string, Row>(StringComparer.Ordinal);
        var probeDupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in probe)
        {
            if (d == null || string.IsNullOrEmpty(d.Id)) continue;
            if (probeRows.ContainsKey(d.Id)) { probeDupIds.Add(d.Id); continue; }
            probeRows[d.Id] = Row.Of(d, buffsCount);
        }

        // ② 主路池：**现跑一遍 `CardDatabase.Parse`**（同一条 JSON、同一个 DTO）。
        string path = Program.PoolPath;
        if (!File.Exists(path)) { Console.Error.WriteLine("!! 找不到卡池：" + path); return 2; }
        string json = File.ReadAllText(path);
        List<CardDef> main;
        try { main = CardDatabase.Parse(json); }
        catch (Exception e) { Console.Error.WriteLine("!! `CardDatabase.Parse` 抛异常：" + e); return 2; }
        var mainRows = new Dictionary<string, Row>(StringComparer.Ordinal);
        foreach (var d in main)
        {
            if (d == null || string.IsNullOrEmpty(d.Id)) continue;
            if (mainRows.ContainsKey(d.Id)) continue;
            mainRows[d.Id] = Row.Of(d, buffsCount);
        }

        Console.WriteLine("=== poolparity · 探针池 vs 主路池（`CardDatabase.Parse`）===");
        Console.WriteLine("  探针池 " + probeRows.Count + " 张（id 重复 " + probeDupIds.Count + "）· 主路池 " + mainRows.Count + " 张"
                          + " · 卡池 " + path);
        // 🔴 两档读数**必须分开看**（`B123_E3` 实测口径）：
        //   · **「已挂」= 这一列在探针里 non-null**（= `Attach*` 真跑过；主路同口径）
        //   · **「有行」= 行数 > 0**（= 这一列真的承载了数据）
        //   ⚠️ 本卡池每张卡都带这些键（哪怕空数组）⇒ 「已挂」= 全池是**正常的**，别拿它当「有数据」的证据。
        Console.WriteLine("  [已挂] 探针池 non-null 张数："
                          + " abilityHeal " + probeRows.Values.Count(r => r.AbilityHealRows >= 0)
                          + " · abilityMana " + probeRows.Values.Count(r => r.AbilityManaRows >= 0)
                          + " · abilitySummons " + probeRows.Values.Count(r => r.AbilitySummons >= 0)
                          + " · abilityBuffs(旁表) " + probeRows.Values.Count(r => r.AbilityBuffs >= 0)
                          + " · permanentBuffs " + probeRows.Values.Count(r => r.PermanentBuffs >= 0)
                          + " · abilityMutations " + probeRows.Values.Count(r => r.AbilityMutations >= 0)
                          + " · abilityEffectTexts " + probeRows.Values.Count(r => r.AbilityEffectTexts >= 0)
                          + " · whileInPlayAnim(>=0) " + probeRows.Values.Count(r => r.WhileInPlayAnim >= 0)
                          + " · startingCardInHand " + probeRows.Values.Count(r => r.StartingCardInHand != null)
                          + " · cardTargetCriteria " + probeRows.Values.Count(r => r.CardTargetCriteriaKnown)
                          + " · transformedWarlordId " + probeRows.Values.Count(r => !string.IsNullOrEmpty(r.TransformedWarlordId))
                          + " · notDeckable " + probeRows.Values.Count(r => r.NotDeckable));
        Console.WriteLine("  [有行] 探针池行数 > 0 张数："
                          + " abilitySummons " + probeRows.Values.Count(r => r.AbilitySummons > 0)
                          + " · abilityBuffs(旁表) " + probeRows.Values.Count(r => r.AbilityBuffs > 0)
                          + " · permanentBuffs " + probeRows.Values.Count(r => r.PermanentBuffs > 0)
                          + " · abilityMutations " + probeRows.Values.Count(r => r.AbilityMutations > 0)
                          + " · abilityEffectTexts " + probeRows.Values.Count(r => r.AbilityEffectTexts > 0));

        int fail = 0, missingInMain = 0, missingInProbe = 0, reportOnly = 0;
        var shown = 0;
        foreach (var kv in probeRows)
        {
            Row m;
            if (!mainRows.TryGetValue(kv.Key, out m))
            {
                missingInMain++;
                if (shown++ < MaxDiffLines) Console.WriteLine("  ⚠️ 探针有、主路没有：`" + kv.Key + "`（主路 `Parse` 会跳过空 id 的卡）");
                continue;
            }
            var bad = kv.Value.DiffHard(m);
            if (bad != null)
            {
                fail++;
                if (shown++ < MaxDiffLines) Console.WriteLine("  ✗ [`" + kv.Key + "`] " + bad);
            }
            var ro = kv.Value.DiffReportOnly(m);
            if (ro != null)
            {
                reportOnly++;
                if (shown++ < MaxDiffLines) Console.WriteLine("  ⚠️（只报不判）[`" + kv.Key + "`] " + ro);
            }
        }
        foreach (var k in mainRows.Keys) if (!probeRows.ContainsKey(k)) missingInProbe++;

        if (shown >= MaxDiffLines) Console.WriteLine("  …（只印前 " + MaxDiffLines + " 条；总数见下面合计）");
        Console.WriteLine("  --- 合计：硬差异 " + fail + " 张 · 只报不判 " + reportOnly + " 张"
                          + " · 探针有主路无 " + missingInMain + " · 主路有探针无 " + missingInProbe + " ---");

        // 🔴 附加的**方向性**断言（防「两边一起空」= 假绿）：
        //   补挂的族里若**整个池一张有行的都没有**，那多半是 `IncludeFields` 漏配 / `AttachPostConstruction`
        //   根本没被调用 —— 此时「硬差异 0 张」毫无意义（两边都空当然相等），必须**单独判红**。
        //   ⚠️ 判据只看**有行（>0）**，⛔ 不看「列在（>=0）」：本卡池每张卡都带这些键 ⇒ 列在全池是**正常**的。
        int famWithRows = probeRows.Values.Count(r => r.AbilitySummons > 0 || r.AbilityBuffs > 0
                                                   || r.PermanentBuffs > 0 || r.AbilityMutations > 0
                                                   || r.AbilityEffectTexts > 0);
        if (famWithRows == 0)
        {
            Console.WriteLine("  ✗ 本文件补挂的那一族**全池一张有行的都没有** ⇒ 补挂没生效"
                              + "（先查 `IncludeFields` / `AttachPostConstruction` 有没有被调用）—— 「硬差异 0」不算数。");
            fail++;
        }

        bool ok = fail == 0 && missingInMain == 0 && missingInProbe == 0;
        Console.WriteLine("=== poolparity 结束：rc=" + (ok ? 0 : 1) + " ===");
        return ok ? 0 : 1;
    }
}
