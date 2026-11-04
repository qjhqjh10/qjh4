// B141_W43_Assert.cs — 批次 **B141** · 执行写手 `B141-W43`（`B141-SAN27-ROWID` · **事件按行承载**）
//
// ============================ 它盯的是什么 ============================
//   **「这一拍播哪一行的目标」**。改前 `BattleDriver.ExpandAbilityBeats` 拿**同一个** 并集列表
//   （`BattleEvent.AbilityTargetList`）给每一拍、每个目标排条 ⇒ 多行主动能力里「两行都带 `abilityTarget` 拍」时
//   **每一拍都播并集**（over-play）。本件盯三件事：
//     ① **数据侧**：生成器把「行归属」烘进 `CardAnimBeat.li`（改前 `_assemble_cards` 有 `logicIndex`、emit 时丢了）；
//     ② **取表**：`BattleDriver.BeatTargetsOf` 按 `li` 取**那一行**的表，取不到才退回并集；
//     ③ **边界**：空行 ≠ 并集 · 旧形状（`AbilityRowTargets == null`）逐字节等价 · 替代能力（`ab=-2`）行号不许混用。
//
// ============================ 原版判据（现读）============================
//   · `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:262/273`（第二处 `:1524-1531`）：
//     **每一行**各调一次 `GetTargets(criteria)` 再 `PlayAnim(bm, data, abilityTargets, 0)`
//     ⇒ 每一拍只播**它自己那一行**的目标。
//   · `AbilityLogic__PlayAnim.c:172-176`：`abilityTargets` 为空 ⇒ `LogError` + **return**
//     ⇒ 「这一行没选到目标」**不等于**「退回别人的目标」。
//   · 🔴 **为什么行号只能用生成器的 `logicIndex`、不能用 `CardAnimBeat.ca`**：`ca` 是 **CardAnim 资产 GUID**，
//     池内 132 张主动能力卡里 53 张多行，其中 **12 张跨行 `ca` 重复**（`AM28` 两行都是 70 · `DA18` 都是 60 ·
//     `SOR13` 都是 30）⇒ 反查**不唯一**（判据 → `报告/B141_W128_RowAbility.md` §5·2）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · **B1**：把 `BeatTargetsOf` 改回 `return union;`（= 改前的「每拍都用同一个列表」）⇒ 三拍全拿到 3 项 ⇒ **红**。
//   · **B1b**：把「按 `li` 取」写成「按 `rows[0]` 取」⇒ 第 3 拍拿到行 0 ⇒ 红。
//   · **A1/A2**：把生成器的那句 `b["li"] = li` 删掉并重生成 ⇒ 多行卡变 0 张 / `li` 分布全 0 ⇒ **红**。
//   · **C1**：把「空表也算有效答案」写成「空表 ⇒ 退回并集」⇒ 第 3 拍拿到 3 项 ⇒ 红。
//   · **C4**：去掉 `AbilityRowTargetsObject == CardDef.ActiveAbilityObject` 那道闸 ⇒ 替代能力那一档拿到行 0 ⇒ 红。
//   ⚠️ **如实标**：本件**不执行** `ExpandAbilityBeats` 本体（它要活 `BattleDriver` + 视图 + 时间线，批处理里造不出来）
//      ⇒ 「循环里确实用了这张表」那一跳靠**结构**保证（`targetsOf` 是该方法里唯一的取表口，
//      `nTgt` / `sig.beatTgt*` 两处都读它）。⚠️ 端到端（真打一局看画面）仍需 Unity / 真机。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（落在该行第一个 `//` **之前**）：
//     `Guard("B141W43_Assert", B141_W43_Assert.Run); _pass += B141_W43_Assert.Pass; _fail += B141_W43_Assert.Fail;`
//   （接线由调度台统一做；本写手的白名单不含那个文件。）
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>B141-W43 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B141_W43_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B141-W43：事件按行承载（`CardAnimBeat.li` → `BattleEvent.AbilityRowTargets`）");
        GroupA_DataRowAttribution();
        GroupA2_IndexAlignment();
        GroupB_PerBeatRowPick();
        GroupC_Fallbacks();
        GroupD_ProducerEndToEnd();
        Summary();
    }

    // ==================================================================
    //  A · 数据侧：生成器把行归属烘进 `CardAnimBeat.li`
    // ==================================================================
    static void GroupA_DataRowAttribution()
    {
        Tr(CardAnimOrder.Loaded && CardAnimOrder.Count > 0,
           "A0 （前提）逐卡演出表载进来了（Count=" + CardAnimOrder.Count + "；LastError=" + CardAnimOrder.LastError + "）");
        if (!CardAnimOrder.Loaded) return;

        // A1：多行卡 `AM28 Chimera` —— 三拍的行归属必须是 [0,0,1]（全量件现读 `activeAbility` 两行 = 2 拍 + 1 拍）
        var beats = new List<CardAnimBeat>();
        int n = CardAnimOrder.TryGetBeats("AM28", beats);
        if (n <= 0) n = CardAnimOrder.TryGetBeats("Chimera", beats);   // `BattleEvent.CardId` 装的是**卡名**
        Tr(n == 3,
           "A1 （前提）`AM28 Chimera` 在表里是 **3 拍**（每拍一张 CardAnim；实际 = " + n + "）"
         + "｜判据：全量件 `card_anim_order.json` 的 `activeAbility` 两行（行 0 = 2 拍 · 行 1 = 1 拍）");
        if (n == 3)
        {
            string seq = beats[0].li + "," + beats[1].li + "," + beats[2].li;
            Check(seq, "0,0,1",
                  "A1b ★ **行归属烘进数据**：三拍的 `li` = 0,0,1（⛔ 不是靠 `ca` 反查 —— `AM28` 两行都 `ca=70`，反查不唯一）"
                + "｜🧨 生成器删掉 `b[\"li\"] = li` 并重生成 ⇒ 全 0 ⇒ 红");
        }

        // A2：全表规模 —— 158 拍全部带 `li`，其中 9 张卡有**多行**
        int steps = 0, noLi = 0, multi = 0;
        int c0 = 0, c1 = 0, c2 = 0, cOther = 0;
        foreach (var card in CardAnimOrder.Items)
        {
            if (card == null || card.steps == null) continue;
            bool hasMulti = false;
            int firstLi = int.MinValue;
            for (int i = 0; i < card.steps.Length; i++)
            {
                var s = card.steps[i];
                if (s == null) continue;
                steps++;
                if (s.li < 0) noLi++;
                if (s.li == 0) c0++; else if (s.li == 1) c1++; else if (s.li == 2) c2++; else cOther++;
                if (firstLi == int.MinValue) firstLi = s.li;
                else if (s.li != firstLi) hasMulti = true;
            }
            if (hasMulti) multi++;
        }
        Check(steps, 158, "A2 （前提）表里一共 **158 拍**（现读）");
        Check(noLi, 0,
              "A2b ★ **158 拍全部带行归属**（`li < 0` 的 = 0）"
            + "｜🧨 生成器丢了 `logicIndex` ⇒ 全是 -1（或 0）⇒ 红");
        Check(multi, 9,
              "A2c ★ **9 张卡有不止一行**（`AM28`/`AM35`/`DA18`/`DA20`/`DA71`/`DA75`/`SW23`/`SW8`/`UM105`）"
            + "｜🧨 `logicIndex` 被摊平 ⇒ 0 张 ⇒ 红（那时「每拍播并集」就再也没法修）");
        Check(c0 + "/" + c1 + "/" + c2 + "/" + cOther, "148/9/1/0",
              "A2d `li` 分布 = 148 / 9 / 1 / 其余 0（与生成器 `--write-slim` 现读一致；⛔ 别拿它当「行数」）");
    }

    // ==================================================================
    //  A2 · 索引对齐：`li`（2022 快照烘的）必须真的落在**当前卡表**的那一行上
    // ==================================================================
    static void GroupA2_IndexAlignment()
    {
        // 🔴 为什么单开一组：`li` 来自 **2022 快照**（`cards_1195_2022时代.json` 的 `activeAbilityLogic` 枚举序），
        //   而运行期按行取表用的是**当前 build** 的 `activeAbilityLogic[j]`。若两张表的行增删过 ⇒ 行号会错位，
        //   而错位的表现是「播了另一行的目标」—— **静默、看起来还像对的**。这一组就是钉它。
        var pool = CardDatabase.Load();
        Tr(pool != null && pool.Count > 0, "A3 （前提）卡表载入（Count=" + (pool == null ? -1 : pool.Count) + "）");
        if (pool == null) return;

        var byId = new Dictionary<string, CardDef>(pool.Count);
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c != null && !string.IsNullOrEmpty(c.Id) && !byId.ContainsKey(c.Id)) byId[c.Id] = c;
        }

        int checkedCards = 0, bad = 0, missing = 0;
        string firstBad = null;
        foreach (var card in CardAnimOrder.Items)
        {
            if (card == null || card.steps == null) continue;
            int maxLi = -1;
            for (int i = 0; i < card.steps.Length; i++)
                if (card.steps[i] != null && card.steps[i].li > maxLi) maxLi = card.steps[i].li;
            if (maxLi < 0) continue;
            CardDef def;
            if (!byId.TryGetValue(card.id ?? "", out def) || def == null) { missing++; continue; }
            var grp = def.ActiveAbilityLogicGroup(CardDef.ActiveAbilityObject);
            int rows = (grp != null && grp.Count > 0 && grp[0] != null && grp[0].TargetsAffected != null)
                     ? grp[0].TargetsAffected.Length : -1;
            checkedCards++;
            if (rows <= maxLi)
            {
                bad++;
                if (firstBad == null) firstBad = "`" + card.id + "` 用到行 " + maxLi + "，但当前卡表只有 " + rows + " 行";
            }
        }
        Tr(checkedCards > 0, "A3b （前提）有卡可比（checked=" + checkedCards + " · 卡表里查不到的=" + missing + "）");
        Check(bad, 0,
              "A3c ★ **行号对齐**：每一拍用到的 `li` 都**落在当前卡表那一行之内**（0 张越界）"
            + (bad == 0 ? "" : "｜首个越界：" + firstBad)
            + "｜🧨 2022 快照与当前 build 的 `activeAbilityLogic` 行数/顺序变了 ⇒ 越界或错位 ⇒ 红"
            + "（错位本身抓不到，抓的是越界那一半 —— 越界 = 铁证，错位要人工核）");
    }

    // ==================================================================
    //  B · 取表：`BeatTargetsOf` 按 `li` 取**那一行**（不是并集）
    // ==================================================================
    static void GroupB_PerBeatRowPick()
    {
        var beats = new List<CardAnimBeat>();
        int n = CardAnimOrder.TryGetBeats("AM28", beats);
        if (n != 3) n = CardAnimOrder.TryGetBeats("Chimera", beats);
        if (n != 3) { Tr(false, "B0 （前提）`AM28 Chimera` 3 拍取不到（n=" + n + "）⇒ B 组跳过（不静默：这一条已经红）"); return; }

        // 行 0 = 己方两张（`ta=0 any` 那类），行 1 = 敌方一张 —— 两行目标**互不相同**，并集是三者
        var row0 = new List<BattleEventTarget> { T(0, 2, 4101), T(0, 6, 4102) };
        var row1 = new List<BattleEventTarget> { T(1, 6, 4201) };
        var union = new List<BattleEventTarget> { T(0, 2, 4101), T(0, 6, 4102), T(1, 6, 4201) };
        var rows = new List<List<BattleEventTarget>> { row0, row1 };
        var e = Ev(union, rows, CardDef.ActiveAbilityObject);

        var g0 = BattleDriver.BeatTargetsOf(beats[0], e, union);
        var g1 = BattleDriver.BeatTargetsOf(beats[1], e, union);
        var g2 = BattleDriver.BeatTargetsOf(beats[2], e, union);

        Tr(ReferenceEquals(g0, row0) && ReferenceEquals(g1, row0) && ReferenceEquals(g2, row1),
           "B1 ★ **每拍只播本行**：拍 0/1（`li=0`）→ 行 0 · 拍 2（`li=1`）→ 行 1"
         + "（实得：拍0=" + Dump(g0) + " · 拍1=" + Dump(g1) + " · 拍2=" + Dump(g2) + "）"
         + "｜判据 `AbilityLogic__PlayAbility.c:262/273`（每行各一次 `GetTargets + PlayAnim`）"
         + "｜🧨 把 `BeatTargetsOf` 改回 `return union;`（= 改前的「每拍都用同一个列表」）⇒ 三拍都是 3 项 ⇒ **红**");

        Check(g0 == null ? -1 : g0.Count, 2,
              "B1b 拍 0 的目标数 = **2**（行 0 那两张；并集是 3 ⇒ 多出来的那张就是改前的 over-play）");
        Check(g2 == null ? -1 : g2.Count, 1,
              "B1c 拍 2 的目标数 = **1**（行 1 那一张）"
            + "｜🧨 把「按 `li` 取」写成「按 `rows[0]` 取」⇒ 拍 2 拿到行 0（2 项）⇒ 红");
        Tr(g2 != null && g2.Count == 1 && g2[0].Player == 1 && g2[0].InstanceId == 4201,
           "B1d 拍 2 播的是**敌方那一张**（`Player=1` · `InstanceId=4201`）—— 不是己方那两张");

        // B2：`li` 是逐拍的数据，不是卡的属性 —— 同一张卡三拍取到的表**不是同一张**
        Tr(!ReferenceEquals(g0, g2),
           "B2 同一张卡的拍 0 与拍 2 取到**不同的**行表（⛔ 不是「一卡一表」—— 那正是改前的形状）");
    }

    // ==================================================================
    //  C · 边界 / 退回
    // ==================================================================
    static void GroupC_Fallbacks()
    {
        var beats = new List<CardAnimBeat>();
        int n = CardAnimOrder.TryGetBeats("AM28", beats);
        if (n != 3) n = CardAnimOrder.TryGetBeats("Chimera", beats);

        var row0 = new List<BattleEventTarget> { T(0, 2, 5101) };
        var row1Empty = new List<BattleEventTarget>();
        var union = new List<BattleEventTarget> { T(0, 2, 5101), T(1, 6, 5201) };
        if (n == 3)
        {
            // C1：空表 = **这一行确实没选到目标** ⇒ 返回空表（原版 `PlayAnim.c:172-176` = LogError + return），
            //     ⛔ 不是「退回别人的目标」
            var eC1 = Ev(union, new List<List<BattleEventTarget>> { row0, row1Empty }, CardDef.ActiveAbilityObject);
            var gC1 = BattleDriver.BeatTargetsOf(beats[2], eC1, union);
            Tr(gC1 != null && gC1.Count == 0,
               "C1 ★ **空行是有效答案**：`rows[1]` 是空表 ⇒ 拍 2 拿到**空表**（0 项），⛔ 不退回并集"
             + "（原版 `abilityTargets` 空 ⇒ `PlayAnim` `LogError` + return，`PlayAnim.c:172-176`）"
             + "｜🧨 把「`rows[li] != null` 才用」写成「非空才用」⇒ 这里变 2 项 ⇒ 红");

            // C2：旧形状（生产端还没填这个字段）⇒ 与改前**逐字节等价**（退回并集）
            var eC2 = Ev(union, null, CardDef.ActiveAbilityObject);
            Tr(ReferenceEquals(BattleDriver.BeatTargetsOf(beats[0], eC2, union), union),
               "C2 **旧形状零回归**：`AbilityRowTargets == null` ⇒ 退回并集（改前口径；此刻生产端尚未接线）");

            // C3：拍没有行归属（`li < 0`）⇒ 退回并集（+ 出声）
            var beatNoRow = new CardAnimBeat { ca = beats[0].ca, vfx = beats[0].vfx, sp = beats[0].sp, ep = beats[0].ep, pt = beats[0].pt, li = -1 };
            Tr(ReferenceEquals(BattleDriver.BeatTargetsOf(beatNoRow, Ev(union, new List<List<BattleEventTarget>> { row0 }, CardDef.ActiveAbilityObject), union), union),
               "C3 `li < 0`（不知道属于哪一行）⇒ 退回并集 + 出声（⛔ 不猜第 0 行）");

            // C4：替代能力（`ab = -2`）的行号**也是从 0 起** ⇒ 不许拿来按主动能力的表取
            Tr(ReferenceEquals(BattleDriver.BeatTargetsOf(beats[0], Ev(union, new List<List<BattleEventTarget>> { row0 }, CardDef.AlternativeAbilityObject), union), union),
               "C4 ★ **行号不许跨能力对象混用**：`AbilityRowTargetsObject = -2`（替代能力）⇒ 退回并集"
             + "（`CardAnimOrder` 的表只装 `activeAbility` 那一份）"
             + "｜🧨 去掉 `== CardDef.ActiveAbilityObject` 那道闸 ⇒ 这里拿到行 0 ⇒ 红");

            // C5：`li` 越界（行表比行数短）⇒ 退回并集
            Tr(ReferenceEquals(BattleDriver.BeatTargetsOf(beats[2], Ev(union, new List<List<BattleEventTarget>> { row0 }, CardDef.ActiveAbilityObject), union), union),
               "C5 `li` 越界（行表只有 1 项、拍是 `li=1`）⇒ 退回并集 + 出声");
        }

        // C6：`union == null`（引擎没填这张表 —— 誓约那一支）⇒ 返回 `null`（调用方退回改前的 0/1 口径；`null` ≠ 空表）
        var beatAny = new CardAnimBeat { ca = "X", li = 0 };
        Tr(BattleDriver.BeatTargetsOf(beatAny, Ev(null, null, CardDef.ActiveAbilityObject), null) == null,
           "C6 `AbilityTargetList == null` ⇒ 返回 `null`（0/1 旧口径；`null` ≠ 空表 —— 见 `B9_W54ENG2` §六·4）");
    }

    // ==================================================================
    //  D · 生产端端到端：`AppendOneRowAbilityTargetsToEvent` 真的**按行**记表
    //     （`B141-W43b` 起 `RuleCore` 已接线；这一组是那条接线的唯一证明）
    // ==================================================================
    static void GroupD_ProducerEndToEnd()
    {
        var card = MultiRowCard("B141W43_RowCard");
        var ctx = NewCtx();
        int p = ctx.Active;
        var caster = Place(ctx, p, 5, card);
        Place(ctx, p, 3, Unit("B141W43_Mate1", 1));
        Place(ctx, p, 7, Unit("B141W43_Mate2", 2));
        var foe = Place(ctx, 1 - p, 4, Unit("B141W43_Foe", 2));

        int friendlyMinions = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var q = ctx.Players[p].Board[s];
            if (q != null && q.IsAlive && !q.IsWarlord) friendlyMinions++;
        }
        Check(friendlyMinions, 3, "D0 （前提）己方恰 3 个非督军（施放者 + 两个陪练）");

        ctx.Emit(EvtKind.Ability, p, 5, card.Name, instanceId: caster.InstanceId);
        var e = LastAbilitySignal(ctx);
        Tr(e != null, "D0b （前提）发得出一条 `EvtKind.Ability`");
        if (e == null) return;

        // ---- 行 0：`ta = 10`（己方非督军，零随机消费档）----
        //  🔑 **期望值不用手算**：用引擎自己的单行建表口 `AbilityTargetListFromCriteriaForTest` 当 oracle
        //     （生产端必须记的正是它这一行的输出）⇒ 断言不会因为「我数错了棋盘」而假红。
        var expect0 = RuleCore.AbilityTargetListFromCriteriaForTest(
            ctx, p, card.ActiveAbilityTargetCriteriaAt(CardDef.ActiveAbilityObject, 0), caster, null);
        var expect1 = RuleCore.AbilityTargetListFromCriteriaForTest(
            ctx, p, card.ActiveAbilityTargetCriteriaAt(CardDef.ActiveAbilityObject, 1), caster, null);
        Tr(expect0 != null && expect0.Count > 0 && expect1 != null && expect1.Count > expect0.Count,
           "D0c （前提）两行各自的目标表**不一样**（行 0 = " + (expect0 == null ? -1 : expect0.Count)
         + " 项 · 行 1 = " + (expect1 == null ? -1 : expect1.Count) + " 项）—— 否则本组没有鉴别力");

        RuleCore.AppendOneRowAbilityTargetsToEventForTest(ctx, p, caster, null,
                                                          new[] { 30, 10, 0, CardDef.ActiveAbilityObject });
        var rows = e.AbilityRowTargets;
        Check(rows != null ? rows.Count : -1, 1,
              "D1 ★ **行 0 记进来了**（`AbilityRowTargets[0]` 存在）"
            + "｜🧨 生产端不写这个字段（= 改前）⇒ `rows == null` ⇒ 红");
        if (rows == null) return;
        Check(rows[0] != null ? rows[0].Count : -1, expect0 == null ? -2 : expect0.Count,
              "D1b 行 0 的表 = **引擎那一行自己的表**（`ta=10`，实际 " + Dump(rows[0]) + "）");
        Tr(rows[0] != null && SameSet(rows[0], expect0),
           "D1b2 行 0 与 oracle **逐项相同**（`InstanceId` 集合相等）");
        Tr(rows[0] != null && rows[0].TrueForAll(t => t.Player == p),
           "D1c 行 0 里全是己方（⛔ 不是并集）");

        // ---- 行 1：`ta = 0 any`（双方全场）----
        RuleCore.AppendOneRowAbilityTargetsToEventForTest(ctx, p, caster, null,
                                                          new[] { 30, 0, 1, CardDef.ActiveAbilityObject });
        Check(rows.Count, 2, "D2 ★ 行 1 也记进来了（下标 = 行号 ⇒ `Count == 2`）");
        Check(rows[1] != null ? rows[1].Count : -1, expect1 == null ? -2 : expect1.Count,
              "D2b 行 1 的表 = **引擎那一行自己的表**（`ta=0 any`，实际 " + Dump(rows[1]) + "）");
        Tr(rows[1] != null && rows[1].Exists(t => t.InstanceId == foe.InstanceId),
           "D2c 行 1 里**有敌方那一张**（`ta=0 any`）");
        Check(e.AbilityRowTargetsObject, CardDef.ActiveAbilityObject,
              "D2d `AbilityRowTargetsObject` 记成 `-1`（消费端按它才敢按行取）");

        // ---- D3：并集（`AbilityTargetList`）口径**没变** ----
        var union = e.AbilityTargetList;
        Tr(union != null && union.Exists(t => t.InstanceId == foe.InstanceId),
           "D3 并集 `AbilityTargetList` 仍含敌方（= 改前口径；它是消费端取不到本行时的兜底，⛔ 别把它改成按行）");
        Tr(union != null && SameSet(union, expect1),
           "D3b 并集 == 两行并（本例行 0 ⊆ 行 1 ⇒ 并集应与行 1 同集；实际 " + Dump(union) + "）");

        // ---- D4：白名单外的行（`ta = 20 self`）**不记**（⛔ 不是记空表）----
        RuleCore.AppendOneRowAbilityTargetsToEventForTest(ctx, p, caster, null,
                                                          new[] { 30, 20, 2, CardDef.ActiveAbilityObject });
        Check(rows.Count, 2,
              "D4 ★ 白名单外那一行**不加进来**（`Count` 仍是 2）"
            + "｜🧨 记成空表 ⇒ 那一拍的拍序会被当成「没选到目标」而不排（原版那是**另一档**：`ta=20` 有它自己的目标）");

        // ---- D5 灭自证：行 0 **不含**敌方（把并集写进每一行就红）----
        Tr(rows[0] != null && !rows[0].Exists(t => t.InstanceId == foe.InstanceId),
           "D5 ★ **行 0 不含敌方**"
         + "｜🧨 生产端偷懒把 `AbilityTargetList`（并集）整个抄进每一行 ⇒ 这里含敌方 ⇒ 红");

        // ---- D6：把生产端产出的**真事件**喂给消费端，逐拍只播本行 ----
        var beats = new List<CardAnimBeat>();
        int nb = CardAnimOrder.TryGetBeats("AM28", beats);
        if (nb != 3) nb = CardAnimOrder.TryGetBeats("Chimera", beats);
        if (nb == 3)
        {
            Tr(ReferenceEquals(BattleDriver.BeatTargetsOf(beats[0], e, union), rows[0])
               && ReferenceEquals(BattleDriver.BeatTargetsOf(beats[2], e, union), rows[1]),
               "D6 ★ **端到端**：生产端产的 `AbilityRowTargets` + `AM28` 的 `li=[0,0,1]` ⇒ 拍 0 播行 0、拍 2 播行 1"
             + "（这条是「事件按行承载」整条链的**唯一**端到端证据）");
        }

        // ---- D7：**没记**的那一行（越界）⇒ 消费端退回并集（不是空表）----
        var beatRow2 = new CardAnimBeat { ca = "X", li = 2 };
        Tr(ReferenceEquals(BattleDriver.BeatTargetsOf(beatRow2, e, union), union),
           "D7 `rows[2]` 没记（白名单外）⇒ 消费端**退回并集**（与「记了空表」是两档，见 C1）");
    }

    // ==================================================================
    //  夹具 / 口令 / 汇总
    // ==================================================================
    static BattleEventTarget T(int player, int slot, int inst)
    {
        return new BattleEventTarget { Player = player, Slot = slot, CardId = "B141W43T" + inst, InstanceId = inst };
    }

    static BattleEvent Ev(List<BattleEventTarget> union, List<List<BattleEventTarget>> rows, int obj)
    {
        return new BattleEvent
        {
            Kind = EvtKind.Ability, Player = 0, Slot = 3, CardId = "Chimera",
            AbilityTargetList = union, AbilityRowTargets = rows, AbilityRowTargetsObject = obj,
        };
    }

    static string Dump(List<BattleEventTarget> l)
    {
        if (l == null) return "null";
        if (l.Count == 0) return "[]";
        var sb = new System.Text.StringBuilder("[" + l.Count + "]{");
        for (int i = 0; i < l.Count; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(l[i].Player).Append(':').Append(l[i].Slot).Append('#').Append(l[i].InstanceId);
        }
        return sb.Append('}').ToString();
    }

    /// <summary>两张目标表是否**同一集合**（`InstanceId` 优先，`0` 时退回 `Player+Slot` —— 同引擎的去重口径）。</summary>
    static bool SameSet(List<BattleEventTarget> a, List<BattleEventTarget> b)
    {
        if (a == null || b == null || a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++) if (!b.Exists(x => SameTarget(x, a[i]))) return false;
        return true;
    }

    static bool SameTarget(BattleEventTarget x, BattleEventTarget y)
    {
        if (x.InstanceId != 0 && y.InstanceId != 0) return x.InstanceId == y.InstanceId;
        return x.Player == y.Player && x.Slot == y.Slot;
    }

    // ---- 端到端夹具（与 `B141_W37_Assert` 同形：合成一张多行主动能力卡）----
    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static CardDef Unit(string id, int cost)
        => new CardDef(id, id, "unit", "", null, "Test", cost, 1, 5, 0, (IEnumerable<string>)null, subtype: "Infantry");

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B141W43Warlord0") };
        var d1 = new List<CardDef> { Hero("B141W43Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // ⚠️ `Exhausted = false` 必须显式写：`UnitState` 的 ctor 恒 `true`（部署当回合 = 疲劳）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static BattleEvent LastAbilitySignal(BattleContext ctx)
    {
        var sig = ctx.Signals;
        for (int i = sig.Count - 1; i >= 0; i--) if (sig[i] != null && sig[i].Kind == EvtKind.Ability) return sig[i];
        return null;
    }

    /// <summary>三行主动能力：行 0 `ta=10`（己方部队）· 行 1 `ta=0 any`（双方）· 行 2 `ta=20 self`（**白名单外**）。</summary>
    static CardDef MultiRowCard(string id)
    {
        var rows = new List<CardDef.TargetCriteriaRowSpec>();
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.ActiveAbilityObject, Logic = 0,
            Criteria = new CardDef.CountSetSpec
            {
                TargetsAffected = 10, FriendlyUnits = 1, EnemyUnits = 0,
                IncludeHero = 0, IncludeMinions = 1, IncludeRemnants = 0,
                MaxTargets = 0, TargetSelectMethod = 0,
            },
        });
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.ActiveAbilityObject, Logic = 1,
            Criteria = new CardDef.CountSetSpec { TargetsAffected = 0, MaxTargets = 0, TargetSelectMethod = 0 },
        });
        rows.Add(new CardDef.TargetCriteriaRowSpec
        {
            Ability = CardDef.ActiveAbilityObject, Logic = 2,
            Criteria = new CardDef.CountSetSpec { TargetsAffected = 20, MaxTargets = 0, TargetSelectMethod = 0 },
        });
        return new CardDef(id, id, "unit", "", null, "Test", 3, 1, 5, 0,
                           (IEnumerable<string>)null, subtype: "Infantry",
                           activeAbilityTargetCriteria: rows,
                           activeAbilityTargetCriteriaVersioned: true);
    }

    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void Tr(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B141_W43 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B141_W43 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
