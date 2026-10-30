// B103W3_Assert.cs — 执行代理 `B103_W3`（账 `特效动画:521` 的最后两条**本切片可做**的残差）
//
// ============================ 它盯的是什么 ============================
//  ① **`RemoveTrait` 无序** —— 原版那本账是**一条** `List<CardEffect>`（`EntityScript.activeEffects +0x108`），
//     出表序 = `List.Add` 序。我们把它单独存成 `HashSet<string>`（服务 `HasRemovedTrait` 的去重语义，那是对的），
//     序就丢了 ⇒ 投影里硬写 `Seq = 0` ⇒ 全族排队尾、族内按**名字字典序**（`EffectsToShow.SortShown` 的 tie-break）。
//     本批补一本旁挂的**有序簿记**（`UnitState.RemovedTraitSeq`），写点 = `MarkTraitRemoved`。
//     判据：`decomp_full/CardScript__AddEffect.c:680-694`（先 `RemoveActiveEffects(4, trait)` 去重、再 `List.Add`）。
//
//  ② **S3-c `RowGroup` 的数据槽 + 投影** —— 消费端（`CardDisplayWindow.RowsOfEffects` 的 `case ShownFamily.Stat`）
//     早就建好「按 `RowGroup` 合并成一行」，缺的是**生产侧的组号**。本批把数据槽与透传建起来
//     （`UnitState.StatMod.RowGroup` · `AddStatMod(…, rowGroup:)` · `TempBuff.RowGroup` · `Of()` 三处透传）。
//     ⚠️ **生产端（`EffectResolver.DoGive` 的 `ctx.NextRowGroup()`）不在本切片白名单** ⇒ 今天全池仍恒 `0` = 不合并；
//     可粘贴补丁 → `报告/B103_W3.md` §四。**本文件把「接上即生效」那条链逐段钉住**，接上之后不需要改断言。
//
//  ③ **S3-d / S3-f 的 `Seq`**（2026-10-30 第二/三段）—— 两个记录类各加一格 `Seq`、投影三处改读它；
//     发号口**唯一一处** = `CardInstance.NextEffectSeq()`（`UnitState.NextEffectSeq` 现在是**转发**到它）。
//     写点：附魔 = `Enchantments.Attach`（跟着牌走，**与载体在不在棋盘上无关**）；手牌 = `EffectResolver.AttachHandEffect`
//     （补挂那条**照抄**原记录的号，⛔ 不另取号）。
//
// ==================== 宿主接线（一行；⛔ 本代理不改宿主 `Editor/BattleScene.cs`）====================
//  在 `BattleScene.Run()` 里那条超长聚合行（本会话现读 = `:46701`，末尾是 `Debug.Log(P + $"=== 合计：…")`）
//  —— **同一物理行**追加（行数零变化）：
//
//      B103W3_Assert.Run(); pass += B103W3_Assert.Pass; fail += B103W3_Assert.Fail;
//
//  🔴 **追加必须落在该行第一个 `//` 之前**（本仓已踩过 6 次「代码被行末 `//` 吞掉」）。
//  ⚠️ 本件**不建 GameObject / 不用 `BattleDriver` / 不用 `BattleContext`**（纯静态投影 + 纯状态位），任何宿主都能跑。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · 把 `Of()` 的 `Seq = u.RemovedTraitSeq(t)` 改回 `Seq = 0`
//    ⇒ **A3-a 红**（出表序退回名字字典序：[ambush, zeal]）、**A4 红**（行的 `Seq` 与 `RemovedTraitSeq` 不等）。
//  · 把 `MarkTraitRemoved` 里那句 `NoteRemovedTraitOrder(trait)` 去掉（= 回到纯 `HashSet`）
//    ⇒ **A1 / A2 / A3 / A4 / A5 全红**，而 A6 仍然绿（属性行自己的序戳不受影响）—— 两者独立。
//  · `AddStatMod` 忽略 `rowGroup` 形参 ⇒ **B1 / B2 红**；只把 `StatMod.RowGroup` 收下、`Of()` 不透传 ⇒ **B2 红而 B1 绿**（两面分开）。
//  · `AddTempBuff` 不把 `b.RowGroup` 传给它建的 `Mod` ⇒ **B5 / B5-a 红**，B1..B3 仍绿。
//  · 🧨 **最硬的一条**：把 `RowGroup` 一律当 `0`（不合并）⇒ B3 红；一律当同一个非 0 值（无条件合并）⇒ B4 红
//    —— **B3 与 B4 结构上不可能同时满足**。
//  · 把 `Of()` 的附魔 `Seq = e.Seq` 改回 `0` ⇒ **D1-b / D3-c 红**；把手牌那两处 `Seq = he.Seq` 改回 `0` ⇒ **D2-a 红**。
//  · 把 `Enchantments.Attach` 里 `Seq = carrier.NextEffectSeq()` 删掉（或让 `CardInstance.NextEffectSeq()` 恒返 0）⇒ **D3-a / D3-b / D3-c 红**，D1/D2 仍绿（读点独立）。
//
// ==================== 期望值从哪来（⛔ 不拿我们的常量证明我们的实现）====================
//  · 「出表序 = 施加序」这个期望本身来自**原版 `.c`**（`List.Add` 序），而**施加序**由本文件自己给定
//    （`zeal` 先、`ambush` 后 —— 刻意与**字典序相反**，否则断言区分不了「有序」与「按名字排」）。
//  · `RowGroup` 的组号是本文件注入的（生产端未接，没有数据面真值可抄）；本文件断的是
//    **「注入什么、投影/消费端就照什么走」**这条链，⛔ 不是「组号本该是 7」。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B103_W3`：账 `特效动画:521` 的 `RemoveTrait` 有序 + S3-c `RowGroup` 数据槽/投影。</summary>
public static class B103W3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103W3] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>跑过的断言里通过的条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    // ============================================================ 夹具
    static List<CardDef> _pool;

    /// <summary>一张干净单位（`Deathwing Terminator`：没有 `abilityEffectTexts`、投影里本来 0 行 —— 见 C0）。</summary>
    static UnitState NewUnit(string cardName)
    {
        var d = Def(cardName);
        return d == null ? null : new UnitState(d, false);
    }

    /// <summary>卡池里按名字取 `CardDef`（找不到 ⇒ `null`；调用方必须先断前提）。</summary>
    static CardDef Def(string cardName)
    {
        if (_pool == null) _pool = CardDatabase.Load();
        var d = CardDatabase.Find(_pool, cardName);
        Tr(d != null, $"（夹具前提）卡池里找得到「{cardName}」");
        return d;
    }

    /// <summary>投影里 `RemoveTrait` 那一族的 trait 名，**按出表序**。</summary>
    static List<string> RemovedRows(UnitState u)
    {
        var outp = new List<string>();
        var rows = EffectsToShow.Of(u, true);
        for (int i = 0; i < rows.Count; i++)
            if (rows[i].Family == ShownFamily.RemoveTrait) outp.Add(rows[i].Trait);
        return outp;
    }

    static int CountOf(List<ShownEffect> list, ShownFamily f)
    {
        int n = 0;
        for (int i = 0; i < list.Count; i++) if (list[i].Family == f) n++;
        return n;
    }

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        Section_RemoveTraitOrdered();
        Section_RowGroupSlotAndProjection();
        Section_NotNarrowed();
        Section_EnchantAndHandSeq();
    }

    // ============================================================ A · `RemoveTrait` 有序
    static void Section_RemoveTraitOrdered()
    {
        S("B103_W3 · A：`RemoveTrait` 出表序 = **施加序**（判据 `CardScript__AddEffect.c:680-694`）");

        var u = NewUnit("Deathwing Terminator");
        Tr(u != null, "A 前提：造得出一张干净单位");
        if (u == null) return;

        // ⚠️ 刻意**先 zeal 后 ambush** —— 字典序是 ambush < zeal ⇒ 「按名字排」与「按施加序」在这一组上给出相反结果。
        u.MarkTraitRemoved("zeal");
        u.MarkTraitRemoved("ambush");

        int sZeal = u.RemovedTraitSeq("zeal"), sAmbush = u.RemovedTraitSeq("ambush");
        Tr(sZeal > 0 && sAmbush > 0, "A1 两条「已移除」都拿到了序戳（>0；旧实现恒 `0`）");
        Tr(sAmbush > sZeal, "A2 后施加的那条序戳更大（ambush > zeal）");

        var order = RemovedRows(u);
        C(order.Count, 2, "A3 投影出 **2 条** `RemoveTrait` 行（集合去重不该把它压成 1 条）");
        Tr(order.Count >= 2 && order[0] == "zeal" && order[1] == "ambush",
           "A3-a 出表序 = **施加序**（zeal → ambush）；旧实现两条都 `Seq = 0` ⇒ 排队尾 + 按名字字典序 ⇒ 会印成 [ambush, zeal] ⇒ 本条红");

        // 行上的 `Seq` 必须就是那本簿记里的号（不是另取一份）
        var rows = EffectsToShow.Of(u, true);
        int bad = 0, seen = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Family != ShownFamily.RemoveTrait) continue;
            seen++;
            if (rows[i].Seq != u.RemovedTraitSeq(rows[i].Trait)) bad++;
        }
        C(seen, 2, "A4 前提：扫到 2 条 `RemoveTrait` 行");
        C(bad, 0, "A4-a 每一行的 `Seq` **逐字等于** `RemovedTraitSeq(该 trait)`（读的是同一本簿记，⛔ 不是另取一个号）");

        // 重复 lose ⇒ 换到队尾（原版先摘旧记录、再 `List.Add` 新的一条）
        var u2 = NewUnit("Deathwing Terminator");
        Tr(u2 != null, "A5 前提：第二张干净单位造得出来");
        if (u2 != null)
        {
            u2.MarkTraitRemoved("zeal");
            u2.MarkTraitRemoved("ambush");
            u2.MarkTraitRemoved("zeal");                 // 再来一次
            var o2 = RemovedRows(u2);
            C(o2.Count, 2, "A5 重复 lose **不新增记录**（`HashSet.Add` 去重 ⇒ 簿记里也只有一条 zeal）");
            Tr(o2.Count == 2 && o2[0] == "ambush" && o2[1] == "zeal",
               "A5-a 重复 lose 把这一条**换到队尾**（判据：原版先 `RemoveActiveEffects(4, trait)` 摘旧记录、再 `List.Add`）");
        }

        // 与属性族**同一本计数器** ⇒ 跨族先后可比
        var u3 = NewUnit("Deathwing Terminator");
        Tr(u3 != null, "A6 前提：第三张干净单位造得出来");
        if (u3 != null)
        {
            u3.MarkTraitRemoved("ambush");
            var mod = u3.AddStatMod("attack", 2);
            Tr(mod != null, "A6 前提：`AddStatMod` 返回非 null");
            if (mod != null)
            {
                Tr(mod.Seq > u3.RemovedTraitSeq("ambush"),
                   "A6 属性行序戳 > **之前**那条 removeTrait ⇒ 两族共用 `NextEffectSeq()` 一本计数器（⛔ 各发各的号会让跨族序不可比）");
                var rows3 = EffectsToShow.Of(u3, true);
                int iRem = -1, iStat = -1;
                for (int i = 0; i < rows3.Count; i++)
                {
                    if (iRem < 0 && rows3[i].Family == ShownFamily.RemoveTrait) iRem = i;
                    if (iStat < 0 && rows3[i].Family == ShownFamily.Stat) iStat = i;
                }
                Tr(iRem >= 0 && iStat >= 0 && iRem < iStat,
                   "A6-a 出表序：先施加的 removeTrait 行排在**后施加**的属性行之前（旧实现它恒排队尾 ⇒ 本条红）");
            }
        }
    }

    // ============================================================ B · S3-c `RowGroup`
    static void Section_RowGroupSlotAndProjection()
    {
        S("B103_W3 · B：S3-c `RowGroup` —— 数据槽 → 投影 → 消费端合并（生产端未接，见报告 §四）");

        // ---- B1/B2/B3：同一组的两条属性 ----
        var ub = NewUnit("Deathwing Terminator");
        Tr(ub != null, "B 前提：造得出一张干净单位");
        if (ub == null) return;

        var m1 = ub.AddStatMod("attack", 2, rowGroup: 7);
        var m2 = ub.AddStatMod("ranged", 2, rowGroup: 7);
        Tr(m1 != null && m2 != null, "B0 前提：两条 `AddStatMod` 都返回非 null");
        if (m1 == null || m2 == null) return;
        C(m1.RowGroup, 7, "B1 `StatMod.RowGroup` 收得下组号（数据槽落地；旧实现没有这一格）");
        C(m2.RowGroup, 7, "B1-a 同一次 give 的第二条也收得下同一个组号");

        var proj = EffectsToShow.Of(ub, true);
        int k = 0, idx0 = -1, idx1 = -1;
        for (int i = 0; i < proj.Count; i++)
        {
            if (proj[i].Family != ShownFamily.Stat || proj[i].Value != 2) continue;
            if (k == 0) idx0 = i; else if (k == 1) idx1 = i;
            k++;
        }
        C(k, 2, "B2 前提：投影里恰好两条 `+2` 属性行");
        Tr(idx0 >= 0 && proj[idx0].RowGroup == 7, "B2-a 投影把组号透传出去（第一条）");
        Tr(idx1 >= 0 && proj[idx1].RowGroup == 7, "B2-b 投影把组号透传出去（第二条）");
        Tr(idx1 == idx0 + 1, "B2-c 两条同组属性在表里**相邻**（消费端的合并只扫相邻项）");

        var uiMerged = CardDisplayWindow.RowsOfEffects(proj, true);
        C(uiMerged.Count, 1, "B3 **端到端**：同组两条属性 ⇒ 消费端合成 **1 行**（原版 = 一条 `ChangeBothAttacks`）");

        // ---- B4：灭自证的另一面 —— 不分组就必须是 2 行 ----
        var ub2 = NewUnit("Deathwing Terminator");
        Tr(ub2 != null, "B4 前提：第二张干净单位造得出来");
        if (ub2 != null)
        {
            var n1 = ub2.AddStatMod("attack", 2);        // 无组号（= 今天生产端的真实状态）
            var n2 = ub2.AddStatMod("ranged", 2);
            Tr(n1 != null && n2 != null, "B4 前提：两条 `AddStatMod` 都成功");
            if (n1 != null && n2 != null)
            {
                C(n1.RowGroup, 0, "B4-a 不传组号 ⇒ `RowGroup == 0`（今天全池的真实值）");
                var uiSplit = CardDisplayWindow.RowsOfEffects(EffectsToShow.Of(ub2, true), true);
                C(uiSplit.Count, 2, "B4-b 组号为 0 ⇒ **不合并**、出 2 行（🧨 与 B3 结构上不可能同时满足：一律合并 ⇒ B4 红；一律不合并 ⇒ B3 红）");
            }
        }

        // ---- B5：限时那一档也带组号 ----
        var ut = NewUnit("Deathwing Terminator");
        Tr(ut != null, "B5 前提：第三张干净单位造得出来");
        if (ut != null)
        {
            ut.AddTempBuff(new UnitState.TempBuff { IsKeyword = false, Name = "attack", Value = 2, RowGroup = 9 });
            C(ut.TempBuffs.Count, 1, "B5 前提：限时增益登记上了 1 条");
            var tb = ut.TempBuffs.Count > 0 ? ut.TempBuffs[0] : null;
            Tr(tb != null && tb.Mod != null, "B5-a 前提：`AddTempBuff` 替它建了那条 `Mod`");
            if (tb != null && tb.Mod != null)
            {
                C(tb.Mod.RowGroup, 9, "B5-b `AddTempBuff` 把 `TempBuff.RowGroup` 透传进它建的 `Mod`");
                var pt = EffectsToShow.Of(ut, true);
                int timed = -1;
                for (int i = 0; i < pt.Count; i++) if (pt[i].Family == ShownFamily.Stat && pt[i].Timed) { timed = i; break; }
                Tr(timed >= 0, "B5-c 前提：投影里有一条**限时** Stat 行");
                if (timed >= 0) C(pt[timed].RowGroup, 9, "B5-d 限时那条也把组号透传到投影（读的是它自带的 `Mod`）");
            }
        }

        // ---- B6：**真 `give` 路径**（`DoGive` 发号 → `AddStatMod(rowGroup:)` → 投影 → 合成 1 行）----
        //   B1–B3 注入的是「已经带上组号的数据」；这一段走**生产端**，证明 `DoGive` 真的把号发下来了。
        {
            var ctx6 = new BattleContext(0xB10306);
            ctx6.Players[0] = new PlayerState();
            ctx6.Players[1] = new PlayerState();
            ctx6.Active = 0;
            var d6 = Def("Deathwing Terminator");
            Tr(d6 != null, "B6 前提：卡池里有夹具卡");
            if (d6 != null)
            {
                var i6 = ctx6.NewInstance(d6);
                Tr(i6 != null, "B6 前提：`ctx.NewInstance` 发得出实例");
                if (i6 != null)
                {
                    var u6 = new UnitState(i6, false);
                    ctx6.Players[0].Board[5] = u6;
                    var op6 = new EffectOp
                    {
                        Verb = "give",
                        Payload = "+2 melee and +2 ranged",
                        Source = "B103W3 夹具",
                        Target = new EffectTargetSpec { Raw = "（夹具：己方一个单位）", Side = "own", Kind = "unit", Count = 1, Auto = true },
                    };
                    RuleCore.ResolveOps(ctx6, 0, null, new List<EffectOp> { op6 }, "B103W3", chosen: u6);

                    int n6 = 0; int g0 = 0, g1 = 0;
                    for (int i = 0; i < u6.StatMods.Count; i++)
                    {
                        var m = u6.StatMods[i];
                        if (m == null) continue;
                        if (n6 == 0) g0 = m.RowGroup; else if (n6 == 1) g1 = m.RowGroup;
                        n6++;
                    }
                    Tr(n6 >= 2, "B6 前提：真 `give`「+2 melee and +2 ranged」给这张卡留下 **≥2** 条属性修饰");
                    if (n6 >= 2)
                    {
                        Tr(g0 != 0, "B6-a 真 `give` 盖上了 **非 0** 组号（生产端 `DoGive` → `ctx.NextRowGroup()`；🧨 不接发号 ⇒ 恒 0 ⇒ 本条红）");
                        Tr(g0 == g1, "B6-b 同一子句的两条属性**同组**");
                        var ui6 = CardDisplayWindow.RowsOfEffects(EffectsToShow.Of(u6, true), true);
                        C(ui6.Count, 1, "B6-c 端到端：`+2 melee and +2 ranged` ⇒ 卡面 **1 行**（原版一条 `ChangeBothAttacks`）");
                    }
                }
            }
        }
    }

    // ============================================================ C · 不许收窄
    static void Section_NotNarrowed()
    {
        S("B103_W3 · C：投影仍是「7 源全量」（⛔ 本批不许把它收窄成只读某几族）");

        var uz = NewUnit("Deathwing Terminator");
        Tr(uz != null, "C 前提：造得出一张干净单位");
        if (uz == null) return;
        C(EffectsToShow.Of(uz, true).Count, 0, "C0 干净单位**不凭空出行**（投影没多印）");

        var uc = NewUnit("Deathwing Terminator");
        Tr(uc != null, "C1 前提：第二张干净单位造得出来");
        if (uc == null) return;
        uc.AddStatMod("attack", 2);
        uc.ApplyTraitEffect(KeywordTable.Vanguard, 1, buffType: UnitState.BuffTypeAddTrait);
        uc.MarkTraitRemoved("zeal");

        var rows = EffectsToShow.Of(uc, true);
        Tr(CountOf(rows, ShownFamily.Stat) >= 1, "C1 属性族仍出行（① `_statMods`）");
        Tr(CountOf(rows, ShownFamily.Trait) >= 1, "C1-a 关键词族仍出行（③ `_traitEffects`）");
        Tr(CountOf(rows, ShownFamily.RemoveTrait) >= 1, "C1-b `RemoveTrait` 族仍出行（⑤ `_removedTraits`）");
        Tr(rows.Count >= 3,
           "C1-c 三族**同时**在（🧨 把投影收窄成只读一族 ⇒ 本条红 —— 这正是账 521 那条「收窄版不接受」的机器面）");
    }

    // ============================================================ D · S3-d / S3-f 的 `Seq` 管道
    static void Section_EnchantAndHandSeq()
    {
        S("B103_W3 · D：附魔 / 手牌两族的 `Seq`（读点 + 写点；发号口唯一 = `CardInstance.NextEffectSeq`）");

        // ---- D1：附魔行的**读点**（手工注入一枚号 ⇒ 投影必须照读）----
        var ue = NewUnit("Deathwing Terminator");
        Tr(ue != null, "D1 前提：造得出一张干净单位");
        if (ue == null) return;
        Tr(ue.Instance != null, "D1 前提：单位身上有 `CardInstance`");
        if (ue.Instance == null) return;

        ue.Instance.ActiveEffects.Add(new ActiveCardEffect
        {
            BuffType = 15,                                    // singleUseEnchantment（池内那三档之一：12/15/16）
            Source = "Pray Enchantment",
            Enchantment = new ActiveEnchantment { SpellType = 0 },
            Seq = 3,                                          // ← 本件新加的字段：读点必须原样带出去
        });
        var re = EffectsToShow.Of(ue, true);
        int ei = -1;
        for (int i = 0; i < re.Count; i++) if (re[i].Family == ShownFamily.Enchantment) { ei = i; break; }
        Tr(ei >= 0, "D1-a 前提：投影里有一条 `Enchantment` 行");
        if (ei >= 0) C(re[ei].Seq, 3, "D1-b 附魔行的 `Seq` **原样读自 `ActiveCardEffect.Seq`**（🧨 读点写回 `0` ⇒ 本条红）");

        // ---- D2：手牌行的**读点**（写点由下面的 D5 覆盖）----
        var uh = NewUnit("Deathwing Terminator");
        Tr(uh != null, "D2 前提：造得出第二张干净单位");
        if (uh == null || uh.Instance == null) return;
        uh.Instance.HandEffects.Add(new CardInstance.HandEffect
        {
            Op = new EffectOp { Verb = "give", Payload = "flank", Source = "B103W3 夹具" },
            Source = "B103W3 夹具",
            Seq = 11,                                         // ← 本件新加的字段
        });
        var rh = EffectsToShow.Of(uh, true);
        int hi = -1;
        for (int i = 0; i < rh.Count; i++)
            if (rh[i].Family == ShownFamily.Trait && rh[i].Seq == 11) { hi = i; break; }
        Tr(hi >= 0, "D2-a 手牌行把 `HandEffect.Seq` **带进投影**了（🧨 `Seq = he.Seq` 改回 `0` ⇒ 本条红）");
        if (hi >= 0) C(rh[hi].Trait, "flank", "D2-b 而且带的是那条手牌载荷给的关键词");

        // ---- D3：附魔的**写点**（`Enchantments.Attach`）----
        var ctx = new BattleContext(0xB10303);
        ctx.Players[0] = new PlayerState();
        ctx.Players[1] = new PlayerState();
        var d = Def("Deathwing Terminator");
        Tr(d != null, "D3 前提：卡池里有夹具卡");
        if (d == null) return;
        var inst = ctx.NewInstance(d);
        Tr(inst != null, "D3 前提：`ctx.NewInstance` 发得出实例");
        if (inst == null) return;
        var unit = new UnitState(inst, false);
        ctx.Players[0].Board[5] = unit;                        // 放在右侧第一格（4 = 督军格）

        var eff = Enchantments.Attach(ctx, inst, d, spellType: 0, source: "B103W3 夹具");
        Tr(eff != null, "D3 前提：附魔挂上了（`Attach` 返回非 null）");
        if (eff == null) return;
        Tr(eff.Seq > 0,
           "D3-a 附魔记录拿到一枚 **>0** 的序戳（发号口 = `CardInstance.NextEffectSeq`，与属性/关键词那几本账**同一个号源**）");

        var sm = unit.AddStatMod("attack", 2);
        Tr(sm != null, "D3 前提：`AddStatMod` 成功");
        if (sm != null)
        {
            Tr(sm.Seq > eff.Seq,
               "D3-b 同一张卡：**后**加的属性行序戳 > **先**挂的附魔行 ⇒ 两族共用同一个号源（跨族先后可比）");
            var r3 = EffectsToShow.Of(unit, true);
            int iE = -1, iS = -1;
            for (int i = 0; i < r3.Count; i++)
            {
                if (iE < 0 && r3[i].Family == ShownFamily.Enchantment) iE = i;
                if (iS < 0 && r3[i].Family == ShownFamily.Stat) iS = i;
            }
            Tr(iE >= 0 && iS >= 0 && iE < iS,
               "D3-c 出表序：先挂的附魔（洞）排在**后面**加的属性行之前（改前附魔恒排队尾 ⇒ 本条红）");
        }

        // ---- D4：载体**不在棋盘上**（手牌/牌库）也照样有序戳 ----
        //   原版 `activeEffects(+0x108)` 长在**那张牌自己**身上，跟它在哪无关 ⇒ 发号口搬到 `CardInstance` 之后，
        //   「还没上场就被附魔」的牌也该有序戳（旧方案「扫棋盘借单位计数器」会给 0 ⇒ 本条红）。
        var inst2 = ctx.NewInstance(d);
        Tr(inst2 != null, "D4 前提：再发一个实例");
        if (inst2 != null)
        {
            var eff2 = Enchantments.Attach(ctx, inst2, d, spellType: 0, source: "B103W3 夹具（不在棋盘）");
            Tr(eff2 != null, "D4 前提：附魔挂上了");
            if (eff2 != null) Tr(eff2.Seq > 0, "D4-a 载体**不在棋盘上**也拿到 **>0** 的序戳（跟着牌走，与它此刻在哪无关）");
        }

        // ---- D5：手牌「补挂」那条写点（`AttachHandEffectCopy` 必须**照抄**原记录的号）----
        var inst5 = ctx.NewInstance(d);
        Tr(inst5 != null, "D5 前提：再发一个实例");
        if (inst5 != null)
        {
            var src = new CardInstance.HandEffect
            {
                Op = new EffectOp { Verb = "give", Payload = "flank", Source = "B103W3 夹具" },
                Source = "B103W3 夹具",
                Seq = 7,
            };
            Tr(RuleCore.AttachHandEffectCopy(ctx, inst5, src), "D5 前提：`AttachHandEffectCopy` 返回 true（补挂成功）");
            Tr(inst5.HandEffects.Count == 1 && inst5.HandEffects[0].Seq == 7,
               "D5-a 补挂那条**照抄**原记录的 `Seq`（🧨 改成 `inst.NextEffectSeq()` 另取号 ⇒ 本条红）");
        }
    }
}
