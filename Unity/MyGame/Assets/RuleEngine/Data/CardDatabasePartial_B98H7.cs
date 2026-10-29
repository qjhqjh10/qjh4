// CardDatabasePartial_B98H7.cs — 第九十八会话 · 波 7 · 执行代理 `B98_H7`
//   **「触发数值 → 正文 op」那座桥的数据侧组装**（账 `战斗逻辑驱动:1288`，同时是
//   `871` / `952` / `506①` / `647` 的唯一前置）。
//
// 🔴 **为什么另起 `partial class CardDatabase`、写在【新文件】**：同 `CardDatabasePartial_B97W5.cs`
//   的文件头纪律 —— `Data/CardDatabase.cs` 被 `账/*.tsv` 按【行号】钉了一批落点
//   （现读 `:1799` / `:2426` / `:2449` / `:3379` / `:3467` / `:3472` / `:3567`）⇒ 在那个文件里
//   **任何位置**加行都会让下游落点整体漂移。本文件走 partial ⇒ 那边只**在同一物理行 `:1749`
//   末尾追加一句调用**（零行数变化）。
//
// ── 一、判据（不重新查，照抄任务书 / 判据文件）──────────────────────────────────────────────
//   · 数据半形状：`判据/B98_R6_trigger分派批.md` §三 Step 0 · `报告/B98_D1.md` §2.4 —
//     `CardDef.TriggerAbilityIndices`（v43 卡表的 `triggerAbilityIndices`，`[{ab,lg,trigger}]`）
//     选出 `(ab, lg)` ⇒ 取该卡**正文 op** 里属于那条 ability 的那些 ⇒ 按 `trigger` 分组挂表。
//   · 消费口：`CardDef.AttachTriggerOpsByNumber` / `TriggerOpsByNumber(int)`
//     （`Core/CardDefPartial_B96CA.cs:80 / :102`）—— **本文件只负责组装，⛔ 不改那个口的两级口径**。
//   · 原版语义（`CardDefPartial_B96CA.cs` 文件头 §一 已取证）：`RawCardScript.TriggerAbilities`
//     遍历 `cardAbilities`、**按 `ability.trigger(+0x10) == 传入 trigger` 选 ability**，再逐条跑
//     它自己的 `abilityLogic` ⇒ **trigger 是 ability 级、logic 是 ability 内级**，同一个 trigger
//     命中 N 条 ability 就 N 条全跑。
//
// ── 二、`(ab,lg) → 哪些 op` 的取法（⚠️ 这是本件唯一的口径点，逐条写明理由）────────────────────
//   ① **op 全集从哪来**：我们的正文是**文本 parse**（`CardDef.CollectTriggerOps` → `EffectText.Parse`
//      逐段），落在 `_triggerOps` 里。那一格**是私有的、没有整表公开口**（`RuleCore.cs:15579` 的
//      `ActiveAbilityBodyOps` 已记过同一件事）⇒ 这里走**同一条现成走法**：遍历
//      `CardDef.RoutableTriggers` 逐个 `def.TriggerOps(kw)`。⛔ 别为此在 `Core/` 另开口子
//      （那会让本代理越界，也会与那条注释两处不一致）。
//   ② **怎么认出「这条 op 属于哪条 ability」**：`EffectOp.AbilityIndex` / `LogicIndex`
//      （`Core/EffectText.cs:224 / :226`）—— 那是 `EffectText.AssignAbilityIndices` 按
//      `unitConditions` 的 `ab` **带闸门推断**出来的块下标（G1~G5，见那里的注释）。**本件不另造判据**：
//      `op.AbilityIndex == 行的 ab` 就是唯一判据。
//   ③ **`lg` 怎么用**：原版 `trigger` 是 **ability 级**字段（一条 ability 只有一个 trigger），
//      `lg` 只表示「这条 ability 的第几条 logic」。⇒ 同一个 `(ab, trigger)` 可能有多行（多 logic），
//      它们**共用同一批 op** ⇒ 本件按 `ab` 取 op、按 `trigger` 分桶，**多行只算一次**（按对象引用去重）。
//      `lg == -1` = 这条 ability **没有任何 logic 行**（原版 `TriggerAbilities` 拿到它就是空循环）
//      ⇒ **不贡献 op**，计入「跳过」。
//   ④ 🔴 **`AbilityIndex == -1` 一律跳过、不算猜**（R6 §三 Step 0 点名的唯一口径风险）：
//      那些 op 归不了位（`AssignAbilityIndices` 的闸门没过），⛔ 不许「按卡面顺序硬配」。
//
// ── 三、挂不挂（三态，⛔ 别合并「未知」与「明确没有」）────────────────────────────────────────────
//   | `TriggerAbilityIndices` | 组装结果 | 动作 |
//   |---|---|---|
//   | `null`（旧表 / 列未知） | —— | **不挂** ⇒ `TriggerOpsByNumber` 走它原有的「卡面关键词」退回档 |
//   | `[]`（原版**明确没有** ability 行） | 空表 | **挂空表**（= 这张卡的触发数值一个都没有正文） |
//   | 非空 | 非空 | **挂**（按 trigger 分组的那张表） |
//   | 非空 | **空**（一条都归不了位） | **不挂** + 计数出声 |
//   🔴 最后那一格是**已知缺口的如实表示，⛔ 不是原版口径**：挂一张空表会把退回档关掉、却什么也没换成
//      （`CardDefPartial_B96CA.cs:94-100` 的两级口径：挂了表就以表为准）⇒ 宁可保留退回档，也不把
//      「归位失败」写成「没有正文」（同 `AttachAbilityDamage` 那份三态 doc 的纪律）。
//      缺口由 <see cref="TriggerOpsByNumberSummary"/> 如实报数，⛔ 不静默。
//
// ── 四、⛔ 别做的事 ────────────────────────────────────────────────────────────────────────────
//   · ⛔ 别改 `CardDef.TriggerOpsByNumber` 的两级口径（那是 `Core/` 的文件，也不在本代理白名单）。
//   · ⛔ 别在调用点按卡 id 猜 op（`RuleCore.FireTriggerByNumber` 的 doc 已写死这条）。
//   · ⛔ 别给 `TriggerOpsByNumber` 返回的列表克隆（那是 `_triggerOps` 的**同一批 op 对象**，
//     克隆会让挂上去的表与正文两处不再同源；`RuleCore.ActiveAbilityBodyOps` 已记过同一句）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class CardDatabase
    {
        // ==========================================================================================
        //  读数（只给自检 / 探针 / 调度台；⛔ 生产侧不读）
        //  🔴 为什么要有：本件的覆盖**天然很低**（`AbilityIndex` 是带闸门的推断，见文件头 §二·④）
        //     ⇒ 「挂上了几张」必须可观测，否则下一个人会以为「1129 张全接上了」。
        // ==========================================================================================

        /// <summary>本表一共解析了多少张卡（每次 `Parse` 调一次本件就 +1）。</summary>
        public static int TriggerOpsByNumberCardsSeen { get; private set; }
        /// <summary>`triggerAbilityIndices` **整列未知**（旧表 `< v42` / 夹具直建）的卡数 ⇒ 一律不挂。</summary>
        public static int TriggerOpsByNumberUnknownColumn { get; private set; }
        /// <summary>列**已知为空**（原版明确没有 ability 行）的卡数 ⇒ 挂空表。</summary>
        public static int TriggerOpsByNumberEmptyColumn { get; private set; }
        /// <summary>真正挂上了**非空**表的卡数（= 本件的覆盖率）。</summary>
        public static int TriggerOpsByNumberAttached { get; private set; }
        /// <summary>列非空、但一条 op 都归不了位 ⇒ **不挂**（保留退回档，见文件头 §三）的卡数。</summary>
        public static int TriggerOpsByNumberUnmapped { get; private set; }
        /// <summary>行级统计：总行数 / 已归位行数。</summary>
        public static int TriggerOpsByNumberRows { get; private set; }
        public static int TriggerOpsByNumberRowsMapped { get; private set; }
        /// <summary>行级跳过原因分布（`lg == -1` / `ab` 坏 / `trigger` 坏 / 本卡一条正文 op 都没有 / 该 ab 无 op）。</summary>
        public static int TriggerOpsByNumberSkipLgNeg { get; private set; }
        public static int TriggerOpsByNumberSkipAbNeg { get; private set; }
        public static int TriggerOpsByNumberSkipTriggerNeg { get; private set; }
        public static int TriggerOpsByNumberSkipNoBody { get; private set; }
        public static int TriggerOpsByNumberSkipNoOpsForAb { get; private set; }

        /// <summary>一行汇总（自检 / 探针直接打印它，⛔ 别在别处再算一遍同样的数）。</summary>
        public static string TriggerOpsByNumberSummary()
        {
            return "[triggerOpsByNumber] 卡 " + TriggerOpsByNumberCardsSeen
                 + "｜列未知 " + TriggerOpsByNumberUnknownColumn
                 + "｜列空 " + TriggerOpsByNumberEmptyColumn
                 + "｜已挂表 " + TriggerOpsByNumberAttached
                 + "｜归位失败(不挂) " + TriggerOpsByNumberUnmapped
                 + "｜行 " + TriggerOpsByNumberRows + "（已归位 " + TriggerOpsByNumberRowsMapped
                 + "；跳过 lg=-1 " + TriggerOpsByNumberSkipLgNeg
                 + " · ab 坏 " + TriggerOpsByNumberSkipAbNeg
                 + " · trigger 坏 " + TriggerOpsByNumberSkipTriggerNeg
                 + " · 无正文 op " + TriggerOpsByNumberSkipNoBody
                 + " · 该 ab 无 op " + TriggerOpsByNumberSkipNoOpsForAb + "）";
        }

        /// <summary>自检 / 探针用：把读数清零（下个断言段落重新数）。⛔ 生产侧别调。</summary>
        public static void ResetTriggerOpsByNumberStatsForTest()
        {
            TriggerOpsByNumberCardsSeen = 0;
            TriggerOpsByNumberUnknownColumn = 0;
            TriggerOpsByNumberEmptyColumn = 0;
            TriggerOpsByNumberAttached = 0;
            TriggerOpsByNumberUnmapped = 0;
            TriggerOpsByNumberRows = 0;
            TriggerOpsByNumberRowsMapped = 0;
            TriggerOpsByNumberSkipLgNeg = 0;
            TriggerOpsByNumberSkipAbNeg = 0;
            TriggerOpsByNumberSkipTriggerNeg = 0;
            TriggerOpsByNumberSkipNoBody = 0;
            TriggerOpsByNumberSkipNoOpsForAb = 0;
        }

        // ==========================================================================================
        //  组装口（由 `CardDatabase.Parse` 在 `:1749` 那一行**构造后挂载**之后调 ——
        //  必须排在 `AttachTriggerAbilityIndices` **之后**，否则读不到那一列）
        // ==========================================================================================

        /// <summary>
        /// 把 `triggerAbilityIndices` 组装成 `CardDef.AttachTriggerOpsByNumber` 要的那张
        /// `{trigger 数值 → 正文 op}` 表。判据 / 口径 / 三态 → 文件头 §一~§三（⛔ 别在这里另写一份）。
        /// </summary>
        /// <returns>`null` = **不挂**（列未知 / 归位失败）⇒ 消费口保留「卡面关键词」退回档；
        /// 非 `null`（可能是**空表**）= 挂上（空表 = 这张卡的触发数值都没有可跑的正文）。</returns>
        static Dictionary<int, List<EffectOp>> MapTriggerOpsByNumber(CardDef def)
        {
            TriggerOpsByNumberCardsSeen++;
            if (def == null) { TriggerOpsByNumberUnknownColumn++; return null; }

            var rows = def.TriggerAbilityIndices;
            if (rows == null)
            {
                // 旧表 / 列未知 ⇒ 判不了 ⇒ 保留退回档（与 `AttachAbilityDamage` 的 `null` 同口径）。
                TriggerOpsByNumberUnknownColumn++;
                return null;
            }
            if (rows.Length == 0)
            {
                // 原版**明确没有** ability 行 ⇒ 「这张卡的触发数值没有正文」是**已知事实**（三态里的第三态）。
                TriggerOpsByNumberEmptyColumn++;
                return new Dictionary<int, List<EffectOp>>();
            }

            // ---- ① 本卡全部触发正文 op（同一批对象，⛔ 不克隆）----
            List<EffectOp> body = null;
            var rts = CardDef.RoutableTriggers;
            if (rts != null)
            {
                for (int k = 0; k < rts.Length; k++)
                {
                    string kw = rts[k];
                    if (string.IsNullOrEmpty(kw)) continue;
                    var ops = def.TriggerOps(kw);
                    if (ops == null) continue;
                    for (int i = 0; i < ops.Count; i++)
                    {
                        var op = ops[i];
                        if (op == null) continue;
                        if (body == null) body = new List<EffectOp>(8);
                        body.Add(op);
                    }
                }
            }

            // ---- ② 逐 (ab, lg, trigger) 行归位（判据 = `op.AbilityIndex == ab`，文件头 §二·②）----
            var map = new Dictionary<int, List<EffectOp>>();
            var taken = new Dictionary<int, HashSet<EffectOp>>();   // 每个 trigger 一张「已收」表（按对象引用）
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row == null) continue;
                TriggerOpsByNumberRows++;
                if (row.Trigger < 0) { TriggerOpsByNumberSkipTriggerNeg++; continue; }
                if (row.Ab < 0) { TriggerOpsByNumberSkipAbNeg++; continue; }
                if (row.Lg < 0)
                {
                    // 该 ability **没有任何 logic 行** ⇒ 原版 `TriggerAbilities` 拿到它是空循环 ⇒ 不贡献 op。
                    TriggerOpsByNumberSkipLgNeg++;
                    continue;
                }
                if (body == null) { TriggerOpsByNumberSkipNoBody++; continue; }

                int got = 0;
                HashSet<EffectOp> seen;
                if (!taken.TryGetValue(row.Trigger, out seen))
                {
                    seen = new HashSet<EffectOp>();
                    taken[row.Trigger] = seen;
                }
                List<EffectOp> bucket;
                if (!map.TryGetValue(row.Trigger, out bucket))
                {
                    bucket = new List<EffectOp>(4);
                    map[row.Trigger] = bucket;
                }
                for (int j = 0; j < body.Count; j++)
                {
                    var op = body[j];
                    if (op.AbilityIndex != row.Ab) continue;      // 🔴 -1 在这里被自然跳过（⛔ 不猜）
                    if (!seen.Add(op)) continue;                  // 同一 ability 的多条 logic 行共用同一批 op ⇒ 只算一次
                    bucket.Add(op);
                    got++;
                }
                if (got == 0) { TriggerOpsByNumberSkipNoOpsForAb++; continue; }
                TriggerOpsByNumberRowsMapped++;
            }

            // ---- ③ 挂不挂（三态，文件头 §三）----
            // 🔴 先清空桶再判：空桶让 `TriggerOpsByNumber` 回一个空列表（它自己也会把空列表折成 null，
            //    但别依赖那一层 —— 表里就不该有「这个 trigger 有 0 条」这种条目）。
            // ⚠️ 必须「先收集键、再删」：`Dictionary` 在 `foreach` 里 `Remove` 会抛 `InvalidOperationException`。
            List<int> dead = null;
            foreach (var kv in map)
                if (kv.Value == null || kv.Value.Count == 0)
                {
                    if (dead == null) dead = new List<int>(2);
                    dead.Add(kv.Key);
                }
            if (dead != null) for (int i = 0; i < dead.Count; i++) map.Remove(dead[i]);
            if (map.Count == 0)
            {
                // 🔴 列非空、却一条都归不了位 ⇒ **不挂**（挂空表 = 关掉退回档却没换成东西）。
                TriggerOpsByNumberUnmapped++;
                return null;
            }
            TriggerOpsByNumberAttached++;
            return map;
        }
    }
}
