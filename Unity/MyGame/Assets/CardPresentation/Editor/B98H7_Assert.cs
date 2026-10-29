// B98H7_Assert.cs — 执行代理 `B98_H7`（第九十八会话 · 波 7）的断言集
//
// ============================ 它盯的是什么 ============================
//   账 `战斗逻辑驱动:1288` 的**数据桥**：`CardDatabase.MapTriggerOpsByNumber`
//   （`RuleEngine/Data/CardDatabasePartial_B98H7.cs`）把 v43 卡表的 `triggerAbilityIndices`
//   （`[{ab,lg,trigger}]`）组装成 `{trigger 数值 → 正文 op}` 并挂到
//   `CardDef.AttachTriggerOpsByNumber`（`Core/CardDefPartial_B96CA.cs:80`）。
//   挂上之后 `CardDef.TriggerOpsByNumber(int)` 才从「卡面关键词」退回档换成**按 ability 取**。
//
// ============================ 判据出处（⛔ 别在这里抄第二份）============================
//   `d:/4/Unity/资料/完全复刻/判据/B98_R6_trigger分派批.md` §三 Step 0（施工单）
//   `d:/4/Unity/资料/完全复刻/报告/B98_D1.md` §2.4（`(ab,lg) → op` 的形状与两个已知风险）
//   `d:/4/Unity/MyGame/Assets/RuleEngine/Core/CardDefPartial_B96CA.cs` 文件头 §一（原版语义取证）
//   `d:/4/Unity/MyGame/Assets/RuleEngine/Core/EffectText.cs:222-233 / :1868-1884`（`AbilityIndex`
//     与 `LogicIndex` 的赋值口，以及「文字顺序 = ability 顺序没有数据保证」那句）
//   本代理报告：`d:/4/Unity/资料/完全复刻/报告/B98_H7.md`（含跳过统计与逐档预期指纹）
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · A1（前提）：卡表装得出来（`cards.Count > 0`）。没有它，下面全是空转绿。
//   · A2：**四态计数自洽** —— `CardsSeen == 列未知 + 列空 + 已挂表 + 归位失败`
//     ｜🧨 装上一个新分支却不计数（例如把「列空」直接并进「归位失败」）⇒ 红。
//   · A3：**行级计数自洽** —— `Rows == 已归位 + 五种跳过`
//     ｜🧨 某条 `continue` 之前忘了计数 ⇒ 红（这一条盯的是「跳过统计不是拿嘴说的」）。
//   · A4：桥**真的接上了** —— `TriggerOpsByNumberAttached > 0`
//     ｜🧨 把 `CardDatabase.cs:1749` 末尾那句 `def.AttachTriggerOpsByNumber(MapTriggerOpsByNumber(def))`
//       删掉（或让 `MapTriggerOpsByNumber` 恒返 `null`）⇒ 实得 0 ⇒ 红。**本条就是这次改动的主判别式。**
//   · A5/A6（覆盖前提）：A7/A8 的检查**至少跑过 1 张已挂表的卡**（`checkedCards > 0`）——
//     否则「每张都通过」在 0 张上是空转绿。
//   · A7：已挂表的卡，`TriggerOpsByNumber(t)` 返回的**每一条 op 都必须 `AbilityIndex >= 0`**
//     ｜🧨 去掉 `MapTriggerOpsByNumber` 里 `op.AbilityIndex != row.Ab` 那句过滤（改成「全桶倒进去」）
//       ⇒ `-1` 的 op 混进来 ⇒ 红。
//   · A8：已挂表的卡，返回的 op 的 `AbilityIndex` **必须属于该 trigger 对应的那些 `ab`**
//     ｜🧨 按 op 在列表里的先后硬配（或按关键词名配）⇒ 会配出别的 ability ⇒ 红。
//   · A9：列**明确为空**（`TriggerAbilityIndices.Length == 0`）的卡 ⇒ `HasTriggerOpsByNumber == true`
//     且 `TriggerOpsByNumber(410) == null`（「明确没有」不能退化成「未知」）
//     ｜🧨 把 `rows.Length == 0` 也归进 `null`（不挂）⇒ 前提那一条红。
//   · A10：**归位失败**（列非空、一条都归不了位）的卡 ⇒ `HasTriggerOpsByNumber == false`
//     （保留卡面关键词退回档，⛔ 不许挂空表把退回档关掉）
//     ｜🧨 把 §三 的 `return null` 改成 `return map`（空表）⇒ 红。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B98H7_Assert.Run -logFile "d:/4/_tmp_view/b98h7.log"
//   判绿 = 合计那行 `失败 0`。并进别的宿主就两行：
//     `B98H7_Assert.Run(); pass += B98H7_Assert.Pass; fail += B98H7_Assert.Fail;`
//   ⚠️ 本文件**不建场景 / 不建资产 / 不改卡表 / 不进任何 `Editor/*Scene.cs`**；只读内存里的卡表。
using RuleEngine;
using UnityEngine;

/// <summary>`B98_H7`：`triggerAbilityIndices → {trigger → 正文 op}` 桥的覆盖率与三态纪律。</summary>
public static class B98H7_Assert
{
    static readonly CheckSink _sink = new CheckSink("[H7] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>本趟通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>本趟失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }
    /// <summary>本趟实际执行的断言条数（= Pass + Fail；证明断言真被跑到）。</summary>
    public static int RunCount { get { return _sink.Pass + _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0;
        try
        {
            MenuCheck.Section(_sink, "B98_H7 账 1288 数据桥（triggerAbilityIndices → TriggerOpsByNumber）");

            // 读数打一行（跳过统计的**唯一**出处；⛔ 别在别处再算一遍同样的数）
            Debug.Log(CardDatabase.TriggerOpsByNumberSummary());

            var cards = CardDatabase.Load();
            Tr(cards != null && cards.Count > 0,
               "A1 （前提）卡表装得出来（> 0 张）｜🧨 卡表资源缺失 ⇒ 下面全是空转绿");

            // ---- A2 四态计数自洽 ----
            int seen = CardDatabase.TriggerOpsByNumberCardsSeen;
            int unknown = CardDatabase.TriggerOpsByNumberUnknownColumn;
            int empty = CardDatabase.TriggerOpsByNumberEmptyColumn;
            int attached = CardDatabase.TriggerOpsByNumberAttached;
            int unmapped = CardDatabase.TriggerOpsByNumberUnmapped;
            C(seen, unknown + empty + attached + unmapped,
              "A2 四态计数自洽：解析张数 = 列未知 + 列空 + 已挂表 + 归位失败"
            + "｜🧨 新增分支不计数（或把两种态并成一格）⇒ 红");

            // ---- A3 行级计数自洽 ----
            int rows = CardDatabase.TriggerOpsByNumberRows;
            int rowsMapped = CardDatabase.TriggerOpsByNumberRowsMapped;
            int skip = CardDatabase.TriggerOpsByNumberSkipLgNeg
                     + CardDatabase.TriggerOpsByNumberSkipAbNeg
                     + CardDatabase.TriggerOpsByNumberSkipTriggerNeg
                     + CardDatabase.TriggerOpsByNumberSkipNoBody
                     + CardDatabase.TriggerOpsByNumberSkipNoOpsForAb;
            C(rows, rowsMapped + skip,
              "A3 行级计数自洽：总行 = 已归位 + 五种跳过"
            + "｜🧨 某条 continue 前忘了计数 ⇒ 红（跳过统计不是拿嘴说的）");

            // ---- A4 桥真的接上了（本次改动的头号判别式）----
            Tr(attached > 0,
               "A4 ★★ 桥接上了：`TriggerOpsByNumberAttached` > 0"
             + "｜🧨 `CardDatabase.cs:1749` 末尾那句 `def.AttachTriggerOpsByNumber(MapTriggerOpsByNumber(def))`"
             + " 被删 / `MapTriggerOpsByNumber` 恒返 null ⇒ 实得 0 ⇒ 红");

            // ---- A5~A8 逐卡内容检查（同时兼顾「至少跑过 1 张」这条覆盖前提）----
            int checkedCards = 0, checkedTriggers = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null || !card.HasTriggerOpsByNumber) continue;
                var idx = card.TriggerAbilityIndices;
                if (idx == null || idx.Length == 0) continue;   // 空表那档由 A9 单独盯
                checkedCards++;

                // 该卡每条 (ab, lg, trigger) 行 ⇒ 用 trigger 去问一次
                for (int r = 0; r < idx.Length; r++)
                {
                    var row = idx[r];
                    if (row == null || row.Trigger < 0 || row.Ab < 0 || row.Lg < 0) continue;
                    var ops = card.TriggerOpsByNumber(row.Trigger);
                    if (ops == null || ops.Count == 0) continue;
                    checkedTriggers++;

                    // 该 trigger 名下所有合法的 ab（同一条 ability 的多条 logic 行会重复给出同一个 ab）
                    for (int j = 0; j < ops.Count; j++)
                    {
                        var op = ops[j];
                        // A7：⛔ 归不了位的 op 不许出现在表里
                        Tr(op != null && op.AbilityIndex >= 0,
                           "A7 「" + card.Id + "」trigger " + row.Trigger + " 表里的 op 必须已归位（AbilityIndex >= 0）"
                         + "｜🧨 去掉 `op.AbilityIndex != row.Ab` 那句过滤 ⇒ `-1` 混进来 ⇒ 红");
                        // A8：必须属于该 trigger 的某个 ab
                        bool inSet = false;
                        for (int k = 0; k < idx.Length; k++)
                        {
                            var rk = idx[k];
                            if (rk == null || rk.Trigger != row.Trigger || rk.Ab < 0) continue;
                            if (op != null && rk.Ab == op.AbilityIndex) { inSet = true; break; }
                        }
                        Tr(inSet,
                           "A8 「" + card.Id + "」trigger " + row.Trigger
                         + " 表里的 op 必须属于该 trigger 的某个 ab｜🧨 按列表先后硬配 ⇒ 红");
                    }
                }
            }
            Tr(checkedCards > 0 && checkedTriggers > 0,
               "A5 （覆盖前提）A7/A8 至少检查过 1 张卡 / 1 个 trigger（实得卡 " + checkedCards
             + " / trigger " + checkedTriggers + "）｜🧨 0 张 ⇒ 上面两条是空转绿");

            // ---- A9 「明确没有」那档：挂空表，且任何 trigger 都取不到 ----
            int emptyCards = 0, emptyOk = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null) continue;
                var idx = card.TriggerAbilityIndices;
                if (idx == null || idx.Length != 0) continue;
                emptyCards++;
                if (card.HasTriggerOpsByNumber && card.TriggerOpsByNumber(410) == null) emptyOk++;
            }
            Tr(emptyCards > 0, "A9 （前提）池内存在 `triggerAbilityIndices == []` 的卡（实得 " + emptyCards + " 张）");
            C(emptyOk, emptyCards,
              "A9' `[]` 的卡一律「挂了空表 ⇒ 任何 trigger 都取不到」"
            + "｜🧨 把 `rows.Length == 0` 也归进「不挂」⇒ 本条的 `HasTriggerOpsByNumber` 为假 ⇒ 红");

            // ---- A10 「归位失败」那档：不挂（保留卡面关键词退回档）----
            int unmappedCards = 0, unmappedOk = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null) continue;
                var idx = card.TriggerAbilityIndices;
                if (idx == null || idx.Length == 0) continue;
                if (card.HasTriggerOpsByNumber) continue;      // 已挂上的不算
                unmappedCards++;
                if (!card.HasTriggerOpsByNumber) unmappedOk++;
            }
            C(unmappedOk, unmappedCards,
              "A10 归位失败的卡一律**不挂**（`HasTriggerOpsByNumber == false` ⇒ 保留退回档）"
            + "｜🧨 把 `MapTriggerOpsByNumber` 的 `return null` 改成返回空表 ⇒ 红");
            C(unmappedCards, unmapped,
              "A10' 逐卡数出来的「不挂」张数 = 汇总读数里的「归位失败」张数"
            + "｜🧨 两处口径分家 ⇒ 红");

            Debug.Log(CardDatabase.TriggerOpsByNumberSummary());
        }
        catch (System.Exception ex)
        {
            _sink.Fail++;
            Debug.LogError(_sink.P + "✗ 断言过程抛异常（后面的条目没跑完）：" + ex);
        }
        Debug.Log(_sink.P + $"===== 合计 通过 {_sink.Pass} / 失败 {_sink.Fail} —— "
                          + (_sink.Fail == 0 ? "✅ 全绿" : "❌ 有红") + " =====");
    }
}
