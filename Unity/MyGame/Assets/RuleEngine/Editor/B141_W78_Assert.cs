// B141_W78_Assert.cs — 执行写手 `B141_W78`（第一百二十八会话 · 2026-11-04）· CHK-05 战斗域 L1121 / L1187
//
// ============================ 它盯的是什么 ============================
//  L1187 · `PlayerHand.RemoveHandEffectAt(int index)`（**已修**，本件 A/B 组）
//    · 判据：`d:/2/tools/decomp_full/PlayerHand__RemoveHandEffectAt.c:30-40` 全文 —— 原版**根本不看 `numberOfUses`**：
//            ① 逐手牌 `activeEffects(+0x108).Contains(handEffects[index])` → `CardScript.RemoveEffect(那条)`
//            ② 末尾 `activeEffects(+0x48).RemoveAt(index)`（**按下标、无条件**）
//            ＋ `CardScript__RemoveEffect.c` 首句 `permanentBuff(+0x74) != 0 ⇒ return`（牌身上永久条不摘、记录照样摘）。
//    · 我们的落点：`RuleEngine/Core/RuleCore.cs` 的 `RemoveHandEffectAt`（门面）+ `RemoveHandEffectRecordAt`（类尾，新）。
//    · 🔴 修之前：记录身份借自 `HandEffect.Uses` 那个共享计数盒 ⇒ `Uses == null`（= **不限次档**，`MakeHandBuffUses(<=0)`
//      的产物、今天卡池的全部情况）走 `RemoveHandEffectFromHand`（首句 `uses == null ⇒ return 0`）⇒ 本口**恒空转**。
//
//  L1121 · `MinionManager.GetNextSlotPos` 的残余①（**⛔ 判据本身错：不是缺口**，本件 C/D 组把反证钉成断言）
//    · 账里写「选项 14 的 `pos.x != 0` 两支未实现」——**那两支在 shipped player 里恒不可达**：
//      ① 原版 `MinionManager__GetNextSlotPos.c` 只在 `required == 1` 且**两侧都有空位**时读 `(float)local_48`（= `pos.x`）；
//      ② 唯一会传非零 `pos` 的调用点是 `BattleManager__GetAnimTransform.c` 的 `case 0xe (14)`：
//         `MousePositionDebug.GetMouseClickPosition(...)`。**在 shipped build 里它就是 `return Vector2.zero;`** ——
//         二进制直读（`工具/disasm_va.py`）：`dump.cs:637653` 给的 RVA `0x4F0EC0` 反汇编出来是
//         「`il2cpp_class_init` → 读 `[class+0xb8]` 静态区 offset 0 的 8 字节 → 返回」，
//         与 `Vector2.get_zero`（RVA `0x3004080`）**指令序列逐字节同形** ⇒ 返回的就是 `Vector2.zero`
//         （= SRP core 里 `#if UNITY_EDITOR ... #else return Vector2.zero; #endif` 那一支的编译结果）。
//         ⇒ `pos` 恒 `(0,0)` ⇒ `pos.x != 0` 两支**恒不可达**。
//      ③ 而且池内**没有 14 档的攻击件**：`cardanim_clips.json` 里 18 行 sp/ep=14 **全是召唤件**；
//         `card_anim_order.json` 的 158 拍里 13 拍为 14，**全部**指向那 18 行里的召唤 prefab ⇒ 走的是
//         `AbilityLogic.GetPos(14)` → `CreateTempAbilityTransform(isPlayer, 0, 1)`（**恒传 `Vector2.zero`**）那条。
//    ⇒ 我们的实现（`BoardSlots.NextSlotPos(ps, required)` 只走中性支 + `AnimAnchors` 14 档 → `MinionSpot(actorSeat,false,1)`）
//      **与实况逐值等价**。本件把这几条钉住：数据形状（C）+ 源码形状（D）+ 中性支三个读数（C1..C3）。
//
// ============================ 断言 ============================
//  A 行为（L1187）：不限次档能摘（记录表 −1、牌身上 −N）· 越界不做事 · 限次档不回归 · 永久条只留牌身 ·
//                  同 `RecordId` 的副本也摘（= 身份是**记录号**不是引用）· 别的记录不受牵连。
//  B 结构（L1187）：`RemoveHandEffectAt` 体里**必须**有 `RemoveHandEffectRecordAt(`、**不许**再有原来那句
//                  `rec == null || rec.Uses == null) return 0`；`RemoveHandEffectRecordAt` 体里按 `RecordId` 认。
//  C 行为 + 数据（L1121）：中性支三个读数（人少那侧 / 平手走右 / 一侧满走另一侧）· 14 档数据形状（召唤件、无攻击件）。
//  D 结构（L1121）：`AnimAnchors` 的 14 档仍是 `MinionSpot(ctx.actorSeat, false, 1)` · `BoardSlots.NextSlotPos` 仍**不带 pos**。
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · A1b：把门面改回 `if (rec == null || rec.Uses == null) return 0;` ⇒ 返回 0、表不变 ⇒ A1/A1b/A1c 红。
//  · A5 ：把身份只留 `ReferenceEquals(e, rec)`（去掉 `RecordId` 那一半）⇒ 同号的副本留下 ⇒ A5 红。
//  · A4 ：把 `!e.Permanent` 闸删掉 ⇒ 永久条被摘 ⇒ A4/A4c 红；把记录那半也套上永久闸 ⇒ A4b 红。
//  · A6 ：把 `recs.RemoveAt(index)` 改成按 `Source` 匹配 ⇒ 同来源的另一条也被摘 ⇒ A6 红。
//  · C1/C2/C3：把中性支改回「恒右」或「按人数」⇒ 对不上原版 `.c:210-242` ⇒ 相应行红。
//  · D1：`AnimAnchors` 的 14 档改走别的档/别的座位 ⇒ 红（那是 GetPos 语义，改了就与 `.c:258-265` 不符）。
//
// ============================ 怎么接（宿主聚合由调度台做）============================
//  `public static void Run();` —— **零前提**（只建内存对局 + 读源码/资源文本，不建窗口、不碰存档）。
//  挂法：`Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加
//      `B141_W78_Assert.Run(); _pass += B141_W78_Assert.Pass; _fail += B141_W78_Assert.Fail;`
//  （⚠️ 追加文本必须落在该行第一个 `//` **之前**；本文件白名单外，接线由调度台统一做。）
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B141_W78`：CHK-05 战斗域 L1187（手牌效果「不限次」档按下标摘）/ L1121（选项 14 两支在 player 里恒不可达）的回归断言。</summary>
public static class B141_W78_Assert
{
    const string Tag = "B141W78";
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_L1187_Behavior();
        GroupB_L1187_SourceGate();
        GroupC_L1121_NeutralBranchAndData();
        GroupD_L1121_SourceGate();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · L1187 行为：不限次档（`Uses == null`）也能按下标摘
    // ==================================================================
    static void GroupA_L1187_Behavior()
    {
        Debug.Log("--- A · L1187 手牌效果按下标摘（原版 `PlayerHand__RemoveHandEffectAt.c:30-40`，不看 numberOfUses）---");

        // A1：不限次档 —— 摘得掉（修之前这里恒空转）
        {
            var ctx = NewCtx(9001, UnitCard("W78HandA"));
            var inst = FirstHand(ctx, 0);
            if (!Pre(ctx, inst, "A0")) return;
            var recs = ctx.Players[0].HandEffectRecords;

            var rec = new CardInstance.HandEffect { Source = "W78Unlimited", RecordId = ctx.NextHandEffectRecord() };  // `Uses` 留 null = 不限次档
            recs.Add(rec);
            inst.HandEffects.Add(rec);
            int before = recs.Count;
            int idx = recs.IndexOf(rec);

            int n = RuleCore.RemoveHandEffectAt(ctx, 0, idx);

            Check(n, 1,
                  "A1 ★★★ **结论（L1187）**：**不限次档**记录按下标摘 ⇒ 返回 1（那一张牌上的条目摘掉了）"
                + "｜🧨 把门面改回 `if (rec == null || rec.Uses == null) return 0;` ⇒ 实得 0 ⇒ 红");
            Check(recs.Count, before - 1,
                  "A1b ★★★ **结论（L1187）**：记录表 **−1**（原版 `:36-39` 那句 `RemoveAt(index)` 无条件）"
                + "｜🧨 同上 ⇒ 表不变 ⇒ 红");
            CheckRaw(!inst.HandEffects.Contains(rec),
                     "A1c ★★ 牌身上那一条也没了（原版逐手牌 `CardScript.RemoveEffect`）");
        }

        // A2：越界不下手（反向对照 —— 证明不是「一调就清空」）
        {
            var ctx = NewCtx(9002, UnitCard("W78HandB"));
            var inst = FirstHand(ctx, 0);
            if (!Pre(ctx, inst, "A2")) return;
            var recs = ctx.Players[0].HandEffectRecords;
            var rec = new CardInstance.HandEffect { Source = "W78Keep", RecordId = ctx.NextHandEffectRecord() };
            recs.Add(rec); inst.HandEffects.Add(rec);
            int before = recs.Count;

            int n = RuleCore.RemoveHandEffectAt(ctx, 0, before);      // 越界（合法下标 = 0..before-1）

            Check(n, 0, "A2 ★★ 越界下标 ⇒ 返回 0");
            Check(recs.Count, before, "A2b ★★ 反证：记录表**没变**（越界不清表）");
            CheckRaw(inst.HandEffects.Contains(rec), "A2c ★★ 反证：牌身上那条还在");
        }

        // A3：限次档不回归（原来的那条路仍走 `RemoveHandEffectFromHand`）
        {
            var ctx = NewCtx(9003, UnitCard("W78HandC"));
            var inst = FirstHand(ctx, 0);
            if (!Pre(ctx, inst, "A3")) return;
            var recs = ctx.Players[0].HandEffectRecords;
            var box = new CardInstance.HandBuffUses { Limited = true, Left = 2, Max = 2 };
            var rec = new CardInstance.HandEffect { Source = "W78Limited", RecordId = ctx.NextHandEffectRecord(), Uses = box };
            recs.Add(rec); inst.HandEffects.Add(rec);

            int n = RuleCore.RemoveHandEffectAt(ctx, 0, recs.IndexOf(rec));

            Check(n, 1, "A3 ★★ 限次档（`Uses != null`）**照旧摘得掉**（返回 1）——本笔没有把老路改坏");
            CheckRaw(!inst.HandEffects.Contains(rec) && recs.IndexOf(rec) < 0, "A3b ★★ 限次档：两边都摘掉");
        }

        // A4：永久条 —— 牌身上留着、记录照样摘（原版 `CardScript__RemoveEffect.c` 首句 `permanentBuff != 0 ⇒ return`）
        {
            var ctx = NewCtx(9004, UnitCard("W78HandD"));
            var inst = FirstHand(ctx, 0);
            if (!Pre(ctx, inst, "A4")) return;
            var recs = ctx.Players[0].HandEffectRecords;
            var rec = new CardInstance.HandEffect { Source = "W78Perm", RecordId = ctx.NextHandEffectRecord(), Permanent = true };
            recs.Add(rec); inst.HandEffects.Add(rec);

            int n = RuleCore.RemoveHandEffectAt(ctx, 0, recs.IndexOf(rec));

            Check(n, 0,
                  "A4 ★★★ **结论（L1187）**：永久条（`Permanent`）⇒ 牌身上**不摘** ⇒ 返回 0"
                + "｜🧨 删掉 `!e.Permanent` 闸 ⇒ 实得 1 ⇒ 红");
            CheckRaw(inst.HandEffects.Contains(rec),
                     "A4b ★★★ **灭自证**：牌身上那条**还在**（原版 `CardScript.RemoveEffect` 首句 `permanentBuff != 0 ⇒ return`）");
            CheckRaw(recs.IndexOf(rec) < 0,
                     "A4c ★★★ **灭自证**：但**记录表里那条照样摘掉**（原版 `:36-39` 的 `RemoveAt` 没有永久闸 —— 否则后进手牌的牌还会补上它）"
                   + "｜🧨 给记录那半也套上永久闸 ⇒ 红");
        }

        // A5：同 `RecordId` 的**副本**也摘（身份 = 记录号，不是对象引用）
        {
            var ctx = NewCtx(9005, UnitCard("W78HandE"));
            var inst = FirstHand(ctx, 0);
            if (!Pre(ctx, inst, "A5")) return;
            var recs = ctx.Players[0].HandEffectRecords;
            int rid = ctx.NextHandEffectRecord();
            var rec = new CardInstance.HandEffect { Source = "W78Copy", RecordId = rid };
            var copy = new CardInstance.HandEffect { Source = "W78Copy", RecordId = rid };   // 「后来进手牌那几张」的副本：同号、不同对象
            recs.Add(rec); inst.HandEffects.Add(rec); inst.HandEffects.Add(copy);

            RuleCore.RemoveHandEffectAt(ctx, 0, recs.IndexOf(rec));

            CheckRaw(!inst.HandEffects.Contains(copy),
                     "A5 ★★★ **灭自证**：**同 `RecordId` 的副本**也被摘下（身份 = 记录号，与 `RegisterHandEffectRecord` 按号去重同口径）"
                   + "｜🧨 把身份只留 `ReferenceEquals(e, rec)` ⇒ 副本留下 ⇒ 红");
        }

        // A6：别的记录不受牵连（摘的是**那一条**，不是同来源全部）
        {
            var ctx = NewCtx(9006, UnitCard("W78HandF"));
            var inst = FirstHand(ctx, 0);
            if (!Pre(ctx, inst, "A6")) return;
            var recs = ctx.Players[0].HandEffectRecords;
            var other = new CardInstance.HandEffect { Source = "W78Same", RecordId = ctx.NextHandEffectRecord() };
            var rec = new CardInstance.HandEffect { Source = "W78Same", RecordId = ctx.NextHandEffectRecord() };   // 同来源、不同记录
            recs.Add(other); recs.Add(rec);
            inst.HandEffects.Add(other); inst.HandEffects.Add(rec);

            RuleCore.RemoveHandEffectAt(ctx, 0, recs.IndexOf(rec));

            CheckRaw(recs.Contains(other) && inst.HandEffects.Contains(other),
                     "A6 ★★★ **灭自证**：**同来源的另一条**（不同 `RecordId`）一条都没动"
                   + "｜🧨 把 `recs.RemoveAt(index)` 改成「按 `Source` 匹配」⇒ 两条都没了 ⇒ 红");
        }
    }

    // ==================================================================
    //  B · L1187 结构：门面必须走新口、旧的那句早退不许回来
    // ==================================================================
    static void GroupB_L1187_SourceGate()
    {
        Debug.Log("--- B · L1187 源码闸：`RemoveHandEffectAt` 转调 `RemoveHandEffectRecordAt`、旧早退已删 ---");
        string code = ReadCore();
        if (code == null) { Report(false, "B0 前提不成立：读不到 RuleEngine/Core/RuleCore.cs"); return; }

        int i0 = code.IndexOf("public static int RemoveHandEffectAt(", StringComparison.Ordinal);
        CheckRaw(i0 >= 0, "B0 前提：切得到 `RemoveHandEffectAt` 的方法头");
        if (i0 < 0) return;
        int i1 = code.IndexOf("\n        }", i0, StringComparison.Ordinal);
        CheckRaw(i1 > i0, "B0b 前提：切得到方法体尾");
        if (i1 <= i0) return;
        string body = code.Substring(i0, i1 - i0);

        CheckRaw(body.IndexOf("RemoveHandEffectRecordAt(", StringComparison.Ordinal) >= 0,
                 "B1 ★★★ **结论（L1187）**：门面里真的分出「不限次档」那一支（`RemoveHandEffectRecordAt(`）"
               + "｜🧨 删掉这一支 ⇒ 行为面 A1 也红");
        CheckRaw(body.IndexOf("rec.Uses == null) return 0", StringComparison.Ordinal) < 0,
                 "B2 ★★★ **灭自证**：旧早退（`rec == null || rec.Uses == null) return 0`）**不在**了"
               + "｜🧨 改回去 ⇒ 红（那正是 L1187 的缺口本身）");

        int j0 = code.IndexOf("static int RemoveHandEffectRecordAt(", StringComparison.Ordinal);
        CheckRaw(j0 >= 0, "B3 前提：找得到新口 `RemoveHandEffectRecordAt`");
        if (j0 < 0) return;
        int j1 = code.IndexOf("\n        }", j0, StringComparison.Ordinal);
        if (j1 <= j0) { Report(false, "B3b 前提：切得到新口的方法体尾"); return; }
        string nr = code.Substring(j0, j1 - j0);
        CheckRaw(nr.IndexOf("RemoveAt(index)", StringComparison.Ordinal) >= 0,
                 "B4 ★★★ **结论（L1187）**：新口用的是原版那句**按下标**摘（`RemoveAt(index)`）");
        CheckRaw(nr.IndexOf("RecordId", StringComparison.Ordinal) >= 0,
                 "B5 ★★★ **灭自证**：认「同一条」用的是 `RecordId`（不是来源名）");
        CheckRaw(nr.IndexOf("!e.Permanent", StringComparison.Ordinal) >= 0,
                 "B6 ★★★ **灭自证**：牌身那一跳带着 `!e.Permanent` 闸（原版 `CardScript__RemoveEffect.c` 首句）"
               + "｜🧨 删掉 ⇒ A4 红");
    }

    // ==================================================================
    //  C · L1121：中性支的三个读数 + 14 档数据形状（证明「两支在 player 里到不了」）
    // ==================================================================
    static void GroupC_L1121_NeutralBranchAndData()
    {
        Debug.Log("--- C · L1121 `GetNextSlotPos(required=1)` 的中性支（`.c:210-242`）＋ 选项 14 的池内形状 ---");

        // C1：两侧都空 ⇒ 人少那侧（平手 `lc < rc` 不成立 ⇒ 右第 1 格）
        var ctx = NewCtx(9101, UnitCard("W78Slots"));
        if (ctx == null || ctx.Players == null) { Report(false, "C0 前提不成立：对局没建起来"); return; }
        var ps = ctx.Players[0];
        Check(BoardSlots.NextSlotPos(ps, 1), 1,
              "C1 ★★ 两侧皆空（`lc=rc=0`）⇒ **右第 1 格**（`+1`；`lc < rc` 不成立 ⇒ 平手走右）");
        Place(ps, BoardSlots.SlotOf(BoardSlots.Right, 0), ctx);
        Place(ps, BoardSlots.SlotOf(BoardSlots.Right, 1), ctx);
        Check(BoardSlots.NextSlotPos(ps, 1), -1,
              "C2 ★★ 右侧 2 人 / 左侧 0 ⇒ **左第 1 格**（`-1`；中性支 = 人少那一侧）"
            + "｜🧨 把中性支改成「恒右」⇒ 实得 3 ⇒ 红");
        Place(ps, BoardSlots.SlotOf(BoardSlots.Left, 0), ctx);
        Place(ps, BoardSlots.SlotOf(BoardSlots.Left, 1), ctx);
        Check(BoardSlots.NextSlotPos(ps, 1), 3,
              "C3 ★★ 左 2 / 右 2 **平手** ⇒ **右第 3 格**（`rc + 1` = `+3`；`.c:237-242` 的「平手走右」）"
            + "｜🧨 把中性支的 `lc < rc` 改成 `lc <= rc` ⇒ 实得 -3 ⇒ 红");

        // C4：一侧满 ⇒ 只能去另一侧（原版 `.c:210-226` / `:263-265`）
        var ctx2 = NewCtx(9102, UnitCard("W78Slots2"));
        if (ctx2 == null || ctx2.Players == null) { Report(false, "C4 前提不成立：第二场没建起来"); return; }
        var ps2 = ctx2.Players[0];
        for (int i = 0; i < BoardSpec.SlotsPerSide; i++) Place(ps2, BoardSlots.SlotOf(BoardSlots.Right, i), ctx2);
        Check(BoardSlots.NextSlotPos(ps2, 1), -(0 + 1),
              "C4 ★★ 右满（4）⇒ **左第 1 格**（`-1`）"
            + "｜🧨 把「右满」那道支删掉 ⇒ 走中性支（lc=0 < rc=4 ⇒ -1）同样 -1 —— 这一条**不是**判别式；"
            + "判别式是 C5 的两侧都满 ⇒ `int.MinValue`");
        var ctx3 = NewCtx(9103, UnitCard("W78Slots3"));
        if (ctx3 == null || ctx3.Players == null) { Report(false, "C5 前提不成立：第三场没建起来"); return; }
        var ps3 = ctx3.Players[0];
        for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
        { Place(ps3, BoardSlots.SlotOf(BoardSlots.Right, i), ctx3); Place(ps3, BoardSlots.SlotOf(BoardSlots.Left, i), ctx3); }
        Check(BoardSlots.NextSlotPos(ps3, 1), int.MinValue,
              "C5 ★★★ **结论（L1121）**：两侧都满 ⇒ `int.MinValue`（原版回督军 `GetEffectAnchor()`，且**不加** `ExtraYOnBoard`）");

        // C6/C7：**选项 14 的池内形状** —— 全在召唤件上，没有攻击件（读 Resources 里的两份 JSON 现算）
        string clipsPath = Path.Combine(Application.dataPath, "CardPresentation/Resources/cardanim_clips.json");
        string orderPath = Path.Combine(Application.dataPath, "CardPresentation/Resources/card_anim_order.json");
        if (!File.Exists(clipsPath) || !File.Exists(orderPath))
        { Report(false, "C6 前提不成立：读不到 cardanim_clips.json / card_anim_order.json"); return; }

        var clips = JsonUtility.FromJson<ClipsFile>(File.ReadAllText(clipsPath));
        var order = JsonUtility.FromJson<OrderFile>(File.ReadAllText(orderPath));
        if (clips == null || clips.rows == null || order == null || order.cards == null)
        { Report(false, "C6b 前提不成立：两份 JSON 没解出 rows / cards"); return; }

        int clip14 = 0, clip14Bad = 0;
        var prefabs = new HashSet<string>();
        for (int i = 0; i < clips.rows.Length; i++)
        {
            var r = clips.rows[i];
            if (r == null || (r.sp != 14 && r.ep != 14)) continue;
            clip14++;
            if (r.sp != 14 || r.ep != 14) clip14Bad++;      // 原版这一档 sp/ep 应成对
            if (!string.IsNullOrEmpty(r.n)) prefabs.Add(r.n);
        }
        Check(clip14, 18, "C6 ★★ `cardanim_clips.json` 里 `sp/ep == 14` 的行 = **18**（现读；数据一变本组要重核）");
        Check(clip14Bad, 0, "C6b ★★ 那 18 行的 `sp` 与 `ep` **成对**都是 14（没有只一头的）");

        int step14 = 0, step14NotPaired = 0, step14UnknownPrefab = 0;
        for (int i = 0; i < order.cards.Length; i++)
        {
            var c = order.cards[i];
            if (c == null || c.steps == null) continue;
            for (int j = 0; j < c.steps.Length; j++)
            {
                var s = c.steps[j];
                if (s == null || (s.sp != 14 && s.ep != 14)) continue;
                step14++;
                if (s.sp != 14 || s.ep != 14) step14NotPaired++;
                if (string.IsNullOrEmpty(s.vfx) || !prefabs.Contains(s.vfx)) step14UnknownPrefab++;
            }
        }
        Check(step14, 13, "C7 ★★ `card_anim_order.json`（能力拍表）里 `sp/ep == 14` 的拍 = **13**");
        Check(step14NotPaired, 0, "C7b ★★ 那 13 拍也是 `sp == ep == 14`（成对）");
        Check(step14UnknownPrefab, 0,
              "C7c ★★★ **结论（L1121）**：13 拍**全部**指向 `cardanim_clips` 里那 18 行的召唤 prefab"
            + " ⇒ 选项 14 在池内**只由召唤能力拍用到**（走 `AbilityLogic.GetPos(14)` → `CreateTempAbilityTransform(isPlayer,0,1)`，"
            + "那条**恒传 `Vector2.zero`**）；**没有任何攻击件**走 `BattleManager.GetAnimTransform(14)` 那一支"
            + "｜🧨 出现一条 14 档攻击件 ⇒ 红（那时才需要复核 `pos` 的来源）");
    }

    // ==================================================================
    //  D · L1121 结构：14 档仍走 GetPos 语义、引擎半仍不带 pos
    // ==================================================================
    static void GroupD_L1121_SourceGate()
    {
        Debug.Log("--- D · L1121 源码闸：`AnimAnchors` 14 档 = `MinionSpot(ctx.actorSeat,false,1)` · `NextSlotPos` 不带 pos ---");
        string path = Path.Combine(Application.dataPath, "CardPresentation/Battle/AnimAnchors.cs");
        if (!File.Exists(path)) { Report(false, "D0 前提不成立：读不到 AnimAnchors.cs"); return; }
        string code = StripComments(File.ReadAllText(path));

        CheckRaw(code.IndexOf("return MinionSpot(ctx.actorSeat, false, 1, opt);", StringComparison.Ordinal) >= 0,
                 "D1 ★★★ **结论（L1121）**：14 档仍是 `MinionSpot(ctx.actorSeat, false, 1, opt)`"
               + "（= 原版 `AbilityLogic.GetPos.c:258-265` → `CreateTempAbilityTransform(isPlayer, 0, 1)`，**行动方那一侧**）"
               + "｜🧨 改走别的座位/别的档 ⇒ 红");
        CheckRaw(code.IndexOf("return MinionSpot(ctx.actorSeat, true, 1, opt);", StringComparison.Ordinal) >= 0,
                 "D1b ★★ 15 档 = `MinionSpot(ctx.actorSeat, true, 1, opt)`（对面那侧；原版 `:266-273`）");

        string bpath = Path.Combine(Application.dataPath, "RuleEngine/Core/BoardSlots.cs");
        if (!File.Exists(bpath)) { Report(false, "D2 前提不成立：读不到 BoardSlots.cs"); return; }
        string bcode = StripComments(File.ReadAllText(bpath));
        CheckRaw(bcode.IndexOf("public static int NextSlotPos(PlayerState ps, int required)", StringComparison.Ordinal) >= 0,
                 "D2 ★★★ **结论（L1121）**：引擎半 `NextSlotPos(PlayerState ps, int required)` **不带 `pos`** ⇒ 只走中性支"
               + "（原版那一支要求 `pos.x != 0`，而 `pos` 在 shipped player 里恒 `(0,0)` —— `GetMouseClickPosition` 的二进制体 = `return Vector2.zero`，"
               + "反证见本文件头与 `报告/B141_W128_CHK05_1121_1187.md`）"
               + "｜🧨 若哪天有人按报告里的补丁把 pos 接进来、那一定有新判据 ⇒ 本行必须重核（不是「自动红 = 错」）");
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    [Serializable] class ClipsFile { public ClipRow[] rows; }
    [Serializable] class ClipRow { public string a; public string n; public int sp; public int ep; }

    [Serializable] class OrderFile { public OrderCard[] cards; }
    [Serializable] class OrderCard { public string id; public string name; public OrderStep[] steps; }
    [Serializable] class OrderStep { public string vfx; public int sp; public int ep; }

    static CardDef UnitCard(string name)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 0, 1, 3, 0, null);
    }

    static CardDef HeroCard(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    /// <summary>一局最小对局（0 号玩家手牌里塞 <paramref name="hand0"/>；发牌由 `NewBattle` 走同一套）。</summary>
    static BattleContext NewCtx(int seed, params CardDef[] hand0)
    {
        var d0 = new List<CardDef> { HeroCard("W78FixHero0", "Ultramarines") };
        for (int i = hand0.Length - 1; i >= 0; i--) d0.Add(hand0[i]);
        var d1 = new List<CardDef> { HeroCard("W78FixHero1", "Goff") };
        for (int i = 0; i < 4; i++) d1.Add(UnitCard("W78FillB" + i));
        return RuleCore.NewBattle(d0, d1, seed: seed, shuffle: false, cardPool: new List<CardDef>());
    }

    static CardInstance FirstHand(BattleContext ctx, int p)
    {
        var hand = ctx != null && ctx.Players != null && p >= 0 && p < ctx.Players.Length ? ctx.Players[p].Hand : null;
        return hand != null && hand.Count > 0 ? hand[0] : null;
    }

    /// <summary>每组的前提问一句（⛔ 不写 `if (x != null) Check(...)` 那种静默跳过）。</summary>
    static bool Pre(BattleContext ctx, CardInstance inst, string tag)
    {
        bool ok = ctx != null && ctx.Players != null && inst != null;
        Report(ok, tag + "0 **前提断言**：对局建起来了、0 号玩家手牌里有一份实例（不成立 ⇒ 本组后面的行就不该跑）");
        return ok;
    }

    /// <summary>往某格摆一个单位（只为 `CountOnSide` / `RoomOnSide` 记账）。</summary>
    static void Place(PlayerState ps, int slot, BattleContext ctx)
    {
        if (ps == null || ps.Board == null || slot < 0 || slot >= ps.Board.Length) return;
        ps.Board[slot] = new UnitState(ctx.NewInstance(UnitCard("W78SlotUnit")), false);
    }

    static string ReadCore()
    {
        string p = Path.Combine(Application.dataPath, "RuleEngine/Core/RuleCore.cs");
        return File.Exists(p) ? StripComments(File.ReadAllText(p)) : null;
    }

    /// <summary>去掉 `//` 与 `/* */` 注释（保留字符串内容）—— 注释里的示例不算接线。</summary>
    static string StripComments(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        bool line = false, block = false, str = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            char n = i + 1 < s.Length ? s[i + 1] : '\0';
            if (line) { if (c == '\n' || c == '\r') { line = false; sb.Append(c); } continue; }
            if (block) { if (c == '*' && n == '/') { block = false; i++; } continue; }
            if (str)
            {
                sb.Append(c);
                if (c == '\\' && n != '\0') { sb.Append(n); i++; continue; }
                if (c == '"') str = false;
                continue;
            }
            if (c == '/' && n == '/') { line = true; i++; continue; }
            if (c == '/' && n == '*') { block = true; i++; continue; }
            if (c == '"') { str = true; sb.Append(c); continue; }
            sb.Append(c);
        }
        return sb.ToString();
    }

    // ==================================================================
    //  最小断言框架
    // ==================================================================

    static void Check<T>(T actual, T expected, string msg)
    {
        Report(EqualityComparer<T>.Default.Equals(actual, expected),
               msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckRaw(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }
}
