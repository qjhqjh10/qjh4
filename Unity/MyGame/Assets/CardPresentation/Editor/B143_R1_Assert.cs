// B143_R1_Assert.cs — `B143` 的断言：**触发 / 出牌能力的逐卡有序演出表**
//   （`Resources/card_anim_order_trigger.json` + `CardAnimOrder.TryGetTriggerBeats`）
//
// ============================ 盯的是什么 ============================
//  判据：原版 `BattleManager.TriggerAbilities` 把**所有命中 trigger 的 `cardAbilities` 行**逐条交给
//        `AbilityLogic.PlayAbility` → `PlayAnim`（每行各播自己那份 `cardAnimations[]`）。
//        `B143-R2` 现算的全池口径（2026-11-05 换源后·**当前 build SO**）= **807 卡 / 1,024 行 / 1,319 raw 拍**，
//        其中 `ThisCardPlayed` = **659 行**。
//        ⚠️ 807 = 803 张有拍的卡 + 4 张「有行但每拍全空」的卡（`GOF14/GOF21/TL64/UM21`，0 拍）；
//        1,024 = SO 的 **(ability, logic) 组数**（ability 级行数是 928 —— 两者是不同粒度，别混）。
//  ⚠️ **1,319 是 raw 拍数**；其中 **10 拍的 `ca` 是空串**（原版自己没绑，`PlayAnim:62-64` 会跳过那一拍）
//     ⇒ 表里实际写出 **1,309 拍**。两个数都在 `stats` 里，本断言**两个都钉**。
//
// ============================ 怎么用 ============================
//   `B143_R1_Assert.Run();`（静态类 + 无参 `Run()`；全局命名空间、类名 == 文件名）
//   ⚠️ 本文件**零前提**：只读资源 + 查表，不建窗口、不碰存档、不跑引擎。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B1–B5  ← 改总数（删一张卡 / 删一行 / 删一拍）⇒ 至少一条红；
//   · B6a–d  ← 把 `stats` 与 `cards[]` 真数据对不上（只改 JSON 头不改身子，或反过来）⇒ 红；
//   · C4     ← `SAU46` 少一行（原版同一 trigger 3 行）⇒ 行数/拍数红；
//   · C5     ← `AM76` 少一行/一拍 ⇒ 拍数或顺序红；
//   · D3     ← **逐元素** `(ai,li)` 序列被重排（`AM76` 的 6 拍）⇒ 红（这是「行序 = 存储序」的唯一判别力）；
//   · C5c–f  ← `(ai, li)` 唯一定位一行（`ai=1` 自己有 3 条 logic 行）⇒ 少一行 / 串行 ⇒ 红；
//   · E1/E2  ← 改某拍的 `ca` / `vfx` / `tm`（生成器退化或表被手改）⇒ 红；
//   · E3     ← 换源后补上的 `ASH52` 那一拍被删/被漏 ⇒ 红（钉「2022 源缺、当前 SO 有」的回归）。
//              🔴 **E1/E2/E3 不套 `if`**：先硬前提 `Check(数量)`，再**无条件**断内容（前提红时结论也照跑）。
using System;
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static class B143_R1_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "[B143R1]";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Eq(int got, int want, string msg)
    {
        Check(got == want, msg + "（实得 " + got + " / 期望 " + want + "）");
    }

    static void EqS(string got, string want, string msg)
    {
        Check(string.Equals(got, want, StringComparison.Ordinal),
              msg + "（实得 `" + got + "` / 期望 `" + want + "`）");
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（实得 " + got.ToString("F4") + " / 期望 " + want.ToString("F4") + "）");
    }

    /// <summary>`B141-COIN-FLIP` 的 `NewBattle` 最小夹具：一方一张督军就够（与 `B102R1_Assert.Hero` 同形）。</summary>
    static List<CardDef> CoinFlipDeck(string tag)
    {
        return new List<CardDef> { new CardDef(tag, tag, "hero", "", null, "Test", 0, 0, 30, 0, null, subtype: "Warlord") };
    }

    // 内容读口：**越界不抛**（返回哨兵值）⇒ 断言可以「前提红了、结论也照跑」，⛔ 不用 `if` 包起来
    static string CaAt(List<CardAnimBeat> l, int i)
    {
        return (l != null && i >= 0 && i < l.Count && l[i] != null) ? l[i].ca : "<缺>";
    }

    static string VfxAt(List<CardAnimBeat> l, int i)
    {
        return (l != null && i >= 0 && i < l.Count && l[i] != null) ? l[i].vfx : "<缺>";
    }

    static float TmAt(List<CardAnimBeat> l, int i)
    {
        return (l != null && i >= 0 && i < l.Count && l[i] != null) ? l[i].tm : float.NaN;
    }

    static int AiAt(List<CardAnimBeat> l, int i)
    {
        return (l != null && i >= 0 && i < l.Count && l[i] != null) ? l[i].ai : -999;
    }

    static int LiAt(List<CardAnimBeat> l, int i)
    {
        return (l != null && i >= 0 && i < l.Count && l[i] != null) ? l[i].li : -999;
    }

    static int Trigger0 = 0;   // `AbilityTrigger.ThisCardPlayed`

    public static void Run()
    {
        _pass = _fail = 0;

        // ------------------------------------------------ A 前提
        var ta = Resources.Load<TextAsset>("card_anim_order_trigger");
        Check(ta != null, "A0 **前提**：`Resources/card_anim_order_trigger.json` 载得到"
                        + "（载不到 ⇒ 跑 `python -I -X utf8 工具/gen_card_anim_order_trigger.py --write`）");
        if (ta == null) { Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败"); return; }

        CardAnimTriggerFile f = null;
        try { f = JsonUtility.FromJson<CardAnimTriggerFile>(ta.text); }
        catch (Exception e) { Check(false, "A1 解析抛异常：" + e.Message); }
        Check(f != null, "A1 解析成功（`CardAnimTriggerFile`）");
        if (f == null) { Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败"); return; }
        Check(f.stats != null, "A2 `stats` 段在（生成器写的总数都靠它）");
        Check(CardAnimOrder.TriggerLoaded, "A3 **读取口**：`CardAnimOrder.TriggerLoaded` = true"
                                        + "（`TriggerLastError` = " + (CardAnimOrder.TriggerLastError ?? "<null>") + "）");
        if (!CardAnimOrder.TriggerLoaded) { Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败"); return; }

        // ------------------------------------------------ B 总数（`B143-R2` 口径）
        var st = f.stats;
        Eq(st.cards, 807, "B1 卡数 = **807**（803 张有拍 + 4 张「有行但全空」；换源后·当前 build SO）");
        Eq(st.rows, 1024, "B2 行数 = **1,024**（= SO 的 (ability, logic) 组数；ability 级行数是 928，不同粒度）");
        Eq(st.steps_raw, 1319, "B3 raw 拍数 = **1,319**（含 10 拍空 `ca`）");
        Eq(st.steps, 1309, "B4 可播拍数 = **1,309**（= raw − 10 拍空 `ca`；原版 `PlayAnim:62-64` 跳过）");
        Eq(st.this_card_played_rows, 659, "B5 `ThisCardPlayed` 行数 = **659**");
        Eq(st.blank_guid, 10, "B5b 空 `ca` 拍数 = 10（B3 − B4 必须等于它）");
        Eq(st.steps_raw - st.steps, st.blank_guid, "B5c **口径自洽**：`steps_raw − steps == blank_guid`");

        // B6：**从身子重算**，与 stats 交叉（只改头不改身子 ⇒ 红）
        int dataCards = 0, dataRows = 0, dataSteps = 0, dataTrigger0 = 0;
        if (f.cards != null)
        {
            dataCards = f.cards.Length;
            for (int ci = 0; ci < f.cards.Length; ci++)
            {
                var c = f.cards[ci];
                if (c == null || c.abilities == null) continue;
                for (int ai = 0; ai < c.abilities.Length; ai++)
                {
                    var ab = c.abilities[ai];
                    if (ab == null) continue;
                    dataRows++;
                    if (ab.trigger == Trigger0) dataTrigger0++;
                    if (ab.steps != null) dataSteps += ab.steps.Length;
                }
            }
        }
        Eq(dataCards, st.cards, "B6a 身子重算卡数 == `stats.cards`");
        Eq(dataRows, st.rows, "B6b 身子重算行数 == `stats.rows`");
        Eq(dataSteps, st.steps, "B6c 身子重算拍数 == `stats.steps`");
        Eq(dataTrigger0, st.this_card_played_rows, "B6d 身子重算 `trigger == 0` 行数 == `stats.this_card_played_rows`");

        // ------------------------------------------------ C 读取口
        Eq(CardAnimOrder.TriggerCount, 807, "C0 `CardAnimOrder.TriggerCount` = 807（与表头一致）");

        var list = new List<CardAnimBeat>();
        int n = CardAnimOrder.TryGetTriggerBeats("Heldrake", Trigger0, list);   // = `EC29`（卡名键）
        Check(n == 1, "C1 **前提**：按卡名 `Heldrake` 取 trigger 0 的拍数 = 1（实得 " + n + "）");
        EqS(CaAt(list, 0), "bc43540f679144d81b72a2c599c5cb63", "C1a `Heldrake` 第 1 拍的 `ca`");
        EqS(VfxAt(list, 0), "EC Heldrake Rally", "C1b `Heldrake` 第 1 拍的 `vfx`");
        list.Clear();
        Eq(CardAnimOrder.TryGetTriggerBeats("EC29", Trigger0, list), 1, "C2 按**卡 id** `EC29` 同样命中 1 拍");

        list.Clear();
        int n3 = CardAnimOrder.TryGetTriggerBeats("GSC6", 680, list);
        Check(n3 == 1, "C3 **前提**：`GSC6` 的 `Uprising`(680) = 1 拍（实得 " + n3 + "）");
        EqS(VfxAt(list, 0), "BulletImpact_autogun_1shot", "C3a `GSC6` 那一拍的 vfx");

        // C4：同一 trigger **多行** —— 原版 `TriggerAbilities` 会逐行全播（`SAU46` 有 3 行 `ThisCardPlayed`）
        Eq(CardAnimOrder.TriggerRowCount("SAU46", Trigger0), 3, "C4★ `SAU46` 的 trigger 0 有 **3 行**（少一行 ⇒ 红）");
        list.Clear();
        int n4 = CardAnimOrder.TryGetTriggerBeats("SAU46", Trigger0, list);
        Check(n4 == 3, "C4a **前提**：`SAU46` 三行合计 3 拍（实得 " + n4 + "）");
        EqS(CaAt(list, 2), "5707d13c3d0da4eba9604feb80f67e21", "C4b `SAU46` 第 3 拍（`li=2`）的 ca");
        EqS(VfxAt(list, 2), "", "C4c `SAU46` 第 3 拍是**纯延时拍**（`vfx` 空串，但 ca 非空）");

        // C5：多行多拍 + 行序（`AM76`：ai0/li0 ×3 → ai1/li0 → ai1/li1 → ai1/li2）
        Eq(CardAnimOrder.TriggerRowCount("AM76", Trigger0), 4, "C5★ `AM76` 的 trigger 0 有 **4 行**");
        list.Clear();
        int n5 = CardAnimOrder.TryGetTriggerBeats("AM76", Trigger0, list);
        Check(n5 == 6, "C5a **前提**：`AM76` 四行合计 **6 拍**（实得 " + n5 + "）");
        // D3：**逐元素**钉 `(ai,li)` 序列（⛔ 不是只比数量）—— 这是「行序 = 存储序」的唯一判别力所在
        int[] wantAi = { 0, 0, 0, 1, 1, 1 };
        int[] wantLi = { 0, 0, 0, 0, 1, 2 };
        for (int i = 0; i < wantAi.Length; i++)
        {
            Eq(AiAt(list, i), wantAi[i], "D3 第 " + (i + 1) + " 拍 `ai`（行序 = 存储序）");
            Eq(LiAt(list, i), wantLi[i], "D3 第 " + (i + 1) + " 拍 `li`");
        }
        // C5b：按 `abilityIndex` 只取一条 ability（`AM76` 的 ai=1 自己就有 3 条 logic 行）
        list.Clear();
        Eq(CardAnimOrder.TryGetTriggerBeats("AM76", Trigger0, 1, list), 3, "C5b 只取 `ai=1` 那一族 = 3 拍");
        // C5c–f：`(ai, li)` **唯一定位一行**（只传 ai 定位不到 —— 这是本轮复核点 2 的落点）
        list.Clear();
        Eq(CardAnimOrder.TryGetTriggerBeats("AM76", Trigger0, 1, 1, list), 1, "C5c 只取 `(ai=1, li=1)` = 1 拍");
        EqS(CaAt(list, 0), "1e56d9192c5c85b4e893b1755c751bab", "C5d `(ai=1, li=1)` 那一拍的 ca");
        list.Clear();
        Eq(CardAnimOrder.TryGetTriggerBeats("AM76", Trigger0, 0, 0, list), 3, "C5e 只取 `(ai=0, li=0)` = 3 拍");
        list.Clear();
        Eq(CardAnimOrder.TryGetTriggerBeats("AM76", Trigger0, 1, 2, list), 1, "C5f 只取 `(ai=1, li=2)` = 1 拍");

        // C6：没命中的两档（都是正常：池内 416 张没有触发/出牌能力；或这张卡没有该 trigger）
        list.Clear();
        Check(!CardAnimOrder.TriggerHas("ASH62"), "C6 **前提**：池内 `ASH62` 不在触发表里（它没有触发/出牌能力）");
        Eq(CardAnimOrder.TryGetTriggerBeats("ASH62", Trigger0, list), 0, "C6a 不在表里 ⇒ 返回 0（⛔ 不硬扩）");
        list.Clear();
        Eq(CardAnimOrder.TryGetTriggerBeats("EC29", 680, list), 0, "C6b 卡在、但没有 680 这个 trigger ⇒ 0");

        // C7：没命中要计数（诊断口）
        CardAnimOrder.ResetTriggerCounters();
        list.Clear();
        CardAnimOrder.TryGetTriggerBeats("ASH62", Trigger0, list);
        Eq(CardAnimOrder.TriggerFallbackHits, 1, "C7 没命中 ⇒ `TriggerFallbackHits` +1（诊断口真的在记）");
        CardAnimOrder.ResetTriggerCounters();

        // ------------------------------------------------ D 结构
        int emptyCa = 0, badAi = 0;
        var ids = new HashSet<string>();
        int dupIds = 0;
        if (f.cards != null)
        {
            for (int ci = 0; ci < f.cards.Length; ci++)
            {
                var c = f.cards[ci];
                if (c == null) continue;
                if (!string.IsNullOrEmpty(c.id) && !ids.Add(c.id)) dupIds++;
                if (c.abilities == null) continue;
                for (int ai = 0; ai < c.abilities.Length; ai++)
                {
                    var ab = c.abilities[ai];
                    if (ab == null) continue;
                    if (ab.ai < 0 || ab.li < 0) badAi++;
                    if (ab.steps == null) continue;
                    for (int si = 0; si < ab.steps.Length; si++)
                        if (ab.steps[si] == null || string.IsNullOrEmpty(ab.steps[si].ca)) emptyCa++;
                }
            }
        }
        EqS(CardAnimOrder.TriggerLastError, null, "D0 读取口无载入错误（`TriggerLastError` = null）");
        Eq(emptyCa, 0, "D1 表内**没有**空 `ca` 的拍（生成期已按原版跳过）");
        Eq(badAi, 0, "D2 每行的 `ai` / `li` 都 ≥ 0（`-1` 只该出现在「不知道」的老数据里）");
        Eq(dupIds, 0, "D2b 卡 id 唯一（重 id 会让按 id 查表命中错卡）");

        // ------------------------------------------------ E 灭自证（内容级；**前提红时结论也照跑**，⛔ 不用 `if` 包）
        list.Clear();
        int nE1 = CardAnimOrder.TryGetTriggerBeats("EC29", Trigger0, list);
        Check(nE1 == 1, "E1 **前提**：`EC29` 恰好 1 拍（实得 " + nE1 + "）");
        Near(TmAt(list, 0), 2.75f, 1e-4f, "E1★ `EC29` 那一拍 `timeMoving` = **2.75 s**（改生成器/手改表 ⇒ 红）");
        list.Clear();
        int nE2 = CardAnimOrder.TryGetTriggerBeats("AM76", Trigger0, list);
        Check(nE2 == 6, "E2 **前提**：`AM76` 恰好 6 拍（实得 " + nE2 + "）");
        EqS(CaAt(list, 5), "1e56d9192c5c85b4e893b1755c751bab",
            "E2★ `AM76` 最后一拍的 ca（少一拍 ⇒ 红；重排的判别力在 D3 的逐元素 `(ai,li)` 序列）");

        // E3：**换源后新增的卡**必须真在表里（`ASH52` = 原 2022 源漏掉、当前 SO 才有的那批）
        list.Clear();
        int nE3 = CardAnimOrder.TryGetTriggerBeats("ASH52", Trigger0, list);
        Check(nE3 == 1, "E3 **前提**：`ASH52`（换源后才补上的卡）trigger 0 = 1 拍（实得 " + nE3 + "）");
        EqS(CaAt(list, 0), "0b053d490ea2df14894d76a29470e7b8",
            "E3★ `ASH52` 那一拍 ca（把这张卡/这一拍从表里删掉 ⇒ 红）");

        // ------------------------------------------------ F 开局掷硬币吃表（`B141-COIN-FLIP` 的 `BattleContextRandom` 那一半）
        //   ⚠️ 本组**只断 `BattleContext` 自身**（`FirstSeatRandomInt` / `DrawFirstSeatRandomInt`）；
        //   `RuleCore.NewBattle(drawFirstSeat:)` 那一跳见下面的 **G 组**（✅ 2026-11-05 `B143-R3` 已接线）。
        //   判据：原版 `SearchOpponentManager__StartBattle.c:20-21` = `GetRandomInt(0,2)` 存 `MatchData+0xe8`，
        //   是开局**第 0 格**、**无条件**掷（平局支 `SupportMethods.cs:3216-3228` 读它：`== 0` ⇒ 座位 0 先手）。
        //   🔴 **本组不钉 `.NET Random` 的具体烧表值**（那是实现细节，跨运行时可能变）：改成**注入表值**
        //      （4 = 偶 / 5 = 奇），既证明「读的是表」也证明奇偶方向 —— 与 `B111_E1B_Assert` 同一个手法。
        {
            var c0 = new BattleContext(0);
            Eq(c0.RandomNumbersIndex, 0, "F0 **前提**：新 ctx 游标 = 0");
            Eq(c0.RandomNumbers.Length, 100, "F0b 表长 = 100");
            Eq(c0.FirstSeatRandomInt, 0, "F0c `FirstSeatRandomInt` 初值 = 0（= 还没掷的哨兵）");

            c0.RandomNumbers[0] = 4; c0.RandomNumbersIndex = 0;          // 偶数
            int g0 = c0.DrawFirstSeatRandomInt();
            Eq(g0, 0, "F1★ 第 0 格 = 4（偶）⇒ 掷出 **0**（座位 0 先手）");
            Eq(c0.RandomNumbersIndex, 1, "F1b **恰好吃 1 格**（游标 0 → 1）");
            Eq(c0.FirstSeatRandomInt, g0, "F1c 结果写进了 `FirstSeatRandomInt`（唯一写点）");

            var c1 = new BattleContext(0);
            c1.RandomNumbers[0] = 5; c1.RandomNumbersIndex = 0;          // 奇数
            Eq(c1.DrawFirstSeatRandomInt(), 1, "F2★ 第 0 格 = 5（奇）⇒ 掷出 **1**（奇偶方向不能反）");
            c1.RandomNumbers[1] = 4;
            Eq(c1.DrawFirstSeatRandomInt(), 0, "F3 第二次掷吃**第 1 格**（不是重复第 0 格）");
            Eq(c1.RandomNumbersIndex, 2, "F3b 两次之后游标 = 2");

            // F4：消费前的边界 —— `GetRandomInt(0,1)` 是**不消耗**的那一档（`max <= min+1`）
            var c2 = new BattleContext(0);
            int before = c2.RandomNumbersIndex;
            Eq(c2.GetRandomInt(0, 1), 0, "F4 `GetRandomInt(0,1)` 返回 0（原版 `max<=min+1` 支）");
            Eq(c2.RandomNumbersIndex, before, "F4b 且**游标不动**（不消耗随机数）");

            // F5：游标按表长回绕（原版 `+0x28 % Count`）
            var cw = new BattleContext(0);
            cw.RandomNumbers[0] = 5; cw.RandomNumbersIndex = 100;
            Eq(cw.DrawFirstSeatRandomInt(), 1, "F5 游标 = 100 ⇒ 取模回到第 **0** 格（注入的 5 ⇒ 1）");
            Eq(cw.RandomNumbersIndex, 101, "F5b 游标继续前进（101，不归零）");

            // F6：烧表本身 —— 同种子逐格相同、异种子不全同（**不钉具体值**，只钉确定性）
            var a = new BattleContext(20260911);
            var bb = new BattleContext(20260911);
            var dd = new BattleContext(20260912);
            bool sameSeed = true, diffSeed = false;
            for (int i = 0; i < 10; i++)
            {
                if (a.RandomNumbers[i] != bb.RandomNumbers[i]) sameSeed = false;
                if (a.RandomNumbers[i] != dd.RandomNumbers[i]) diffSeed = true;
            }
            Check(sameSeed, "F6 同一颗种子 ⇒ 预烧表前 10 格逐格相同（可复现）");
            Check(diffSeed, "F6b 不同种子 ⇒ 前 10 格不全同（表真的按种子烧）");
            Eq(a.RandomNumbersIndex, 0, "F6c 烧表之后游标 = 0（`InitRandomNumbers` 的复位）");
        }

        // ------------------------------------------------ G `RuleCore.NewBattle(drawFirstSeat:)` 接线（`B141-COIN-FLIP` 的 RuleCore 半）
        //   判据 = F 组那段注释（原版 `SearchOpponentManager__StartBattle.c:20-21` 开局**无条件**掷、吃第 0 格）。
        //   🔴 **本组是唯一能抓「API 存在但 `NewBattle` 没接线」的断言**：只删 G1 覆盖不到 —— 所以
        //      「`false` 游标不动 / `true` 恰好吃 1 格」两条**必须都在**（缺一条就不是这对判别式）。
        //   ⚠️ `firstSeat` 的缺省是 **`0`（显式）**不是 `-1`：只有传 `-1` 才走「用掷出来的」那一档
        //      （施工单 §2·6 有意保留 —— 改缺省会波及 40+ 探针调用点）。
        {
            var cg = RuleCore.NewBattle(CoinFlipDeck("B143R1G0a"), CoinFlipDeck("B143R1G0b"), seed: 0, shuffle: false);
            Eq(cg.RandomNumbersIndex, 0, "G1 ★ 缺省（不传 `drawFirstSeat`）⇒ 游标**不动**（探针 40+ 调用点与 `play check` 指纹的前提）");

            var cd = RuleCore.NewBattle(CoinFlipDeck("B143R1G1a"), CoinFlipDeck("B143R1G1b"), seed: 0, shuffle: false,
                                        drawFirstSeat: true, firstSeat: -1);
            Eq(cd.RandomNumbersIndex, 1, "G2 ★ `drawFirstSeat: true` + `firstSeat: -1` ⇒ **恰好吃 1 格**（0 → 1）");
            Eq(cd.FirstSeat, cd.RandomNumbers[0] % 2 == 0 ? 0 : 1,
               "G3 ★ 掷出来的值决定座位（`RandomNumbers[0] % 2`；奇偶方向不能反）");

            var cf = RuleCore.NewBattle(CoinFlipDeck("B143R1G2a"), CoinFlipDeck("B143R1G2b"), seed: 0, shuffle: false,
                                        drawFirstSeat: true, firstSeat: 1);
            Eq(cf.RandomNumbersIndex, 1, "G4 ★★ 显式 `firstSeat: 1` ⇒ **格子照吃**");
            Eq(cf.FirstSeat, 1, "G4b …且**值照旧覆盖**（原版 `StartBattle` 无条件掷 + 分支覆盖取值，不是二选一）");

            var cz = RuleCore.NewBattle(CoinFlipDeck("B143R1G3a"), CoinFlipDeck("B143R1G3b"), seed: 0, shuffle: false,
                                        drawFirstSeat: true);
            Eq(cz.RandomNumbersIndex, 1, "G5a 缺省 `firstSeat = 0` 也**照吃格子**");
            Eq(cz.FirstSeat, 0, "G5b …但它是**显式 0**（不是『没指定』）⇒ 座位 0、不用掷出来的值");
        }

        // ------------------------------------------------ H 出牌（`ThisCardPlayed`）那张表（`B143` · 执行写手 `W1`）
        //   判据：原版打出这张卡时播的 = 它 **`trigger == 0` 那些行**各自的 `cardAnimations[]`
        //   （`AbilityLogic__PlayAnim.c:45-60` + `PlayAbility.c:273`）；表 = `Resources/card_anim_order_play.json`
        //   （`工具/gen_card_anim_order_play.py` 产出，现读 **535 卡 / 659 行 / 802 raw 拍 / 796 可播 / 6 拍空 `ca`**；
        //     535 = 534 张有拍 + `TL64`（trigger 0 行两拍全空）· `UM39` 换源后已自动剔除，见 `判据/B143_Play表覆盖缺口.md`）。
        //   ⚠️ 与 `card_anim_order_trigger`（全部 trigger）**同源不同表**：H11 断「两边对同一行给同一个 ca」。
        {
            var tap = Resources.Load<TextAsset>("card_anim_order_play");
            Check(tap != null, "H0 **前提**：`Resources/card_anim_order_play.json` 载得到"
                             + "（载不到 ⇒ 跑 `python -I -X utf8 工具/gen_card_anim_order_play.py --write`）");
            CardAnimPlayFile fp = null;
            if (tap != null)
            {
                try { fp = JsonUtility.FromJson<CardAnimPlayFile>(tap.text); }
                catch (Exception e) { Check(false, "H1 解析抛异常：" + e.Message); }
            }
            Check(fp != null, "H1 解析成功（`CardAnimPlayFile`）");
            Check(CardAnimOrder.PlayLoaded, "H2 **读取口**：`CardAnimOrder.PlayLoaded` = true"
                                          + "（`PlayLastError` = " + (CardAnimOrder.PlayLastError ?? "<null>") + "）");
            if (fp != null && fp.stats != null && CardAnimOrder.PlayLoaded)
            {
                var sp = fp.stats;
                Eq(sp.cards, 535, "H3a 卡数 = **535**（534 张有拍 + `TL64` 空行）");
                Eq(sp.rows, 659, "H3b 行数 = **659**（= trigger 表的 `this_card_played_rows`，同源）");
                Eq(sp.steps_raw, 802, "H3c raw 拍数 = **802**");
                Eq(sp.steps, 796, "H3d 可播拍数 = **796**（原版 `PlayAnim:62-64` 跳过空 `ca`）");
                Eq(sp.blank_guid, 6, "H3e 空 `ca` 拍数 = 6");
                Eq(sp.steps_raw - sp.steps, sp.blank_guid, "H3f **口径自洽**：`steps_raw − steps == blank_guid`");

                int dCards = 0, dRows = 0, dSteps = 0; bool mono = true;
                if (fp.cards != null)
                {
                    dCards = fp.cards.Length;
                    for (int i = 0; i < fp.cards.Length; i++)
                    {
                        var cc = fp.cards[i];
                        if (cc == null || cc.rows == null) continue;
                        int pa = -1, pl = -1;
                        for (int j = 0; j < cc.rows.Length; j++)
                        {
                            var rr = cc.rows[j];
                            if (rr == null) continue;
                            dRows++;
                            if (rr.steps != null) dSteps += rr.steps.Length;
                            if (rr.ai < pa || (rr.ai == pa && rr.li < pl)) mono = false;   // (ai,li) 升序 = 原版遍历序
                            pa = rr.ai; pl = rr.li;
                        }
                    }
                }
                Eq(dCards, sp.cards, "H4a 身子重算卡数 == `stats.cards`（只改头不改身子 ⇒ 红）");
                Eq(dRows, sp.rows, "H4b 身子重算行数 == `stats.rows`");
                Eq(dSteps, sp.steps, "H4c 身子重算拍数 == `stats.steps`");
                Check(mono, "H4d 全表每张卡的 `rows` 按 `(ai, li)` **升序**（重排 ⇒ 红）");

                Eq(CardAnimOrder.PlayCount, 535, "H5 `CardAnimOrder.PlayCount` = 535（与表头一致）");

                // H6：抽样（单行卡）
                var lp = new List<CardAnimBeat>();
                int np = CardAnimOrder.TryGetPlayBeats("Heldrake", lp);   // = `EC29`
                Eq(np, 1, "H6 **前提**：`EC29` 的出牌拍数 = 1");
                Eq(CardAnimOrder.PlayRowCount("EC29"), 1, "H6b `EC29` 出牌表里 1 行");
                if (np == 1)
                {
                    EqS(CaAt(lp, 0), "bc43540f679144d81b72a2c599c5cb63", "H6c `EC29` 第 1 拍的 `ca`");
                    EqS(VfxAt(lp, 0), "EC Heldrake Rally", "H6d `EC29` 第 1 拍的 `vfx`");
                    Near(lp[0].tm, 2.75f, 1e-4f, "H6e `EC29` 第 1 拍的 `timeMoving` = **2.75 s**");
                }

                // H7：多行卡（与 trigger 表 C4 同一张：`SAU46` 3 行）
                Eq(CardAnimOrder.PlayRowCount("SAU46"), 3, "H7★ `SAU46` 出牌表里 **3 行**（少一行 ⇒ 红）");
                lp.Clear();
                Eq(CardAnimOrder.TryGetPlayBeats("SAU46", lp), 3, "H7b `SAU46` 三行合计 3 拍");

                // H8：**灭自证** —— 表里没有的卡必须回 0 且记一次 fallback（⛔ 不是「静默 0」）
                int hits0 = CardAnimOrder.PlayFallbackHits;
                lp.Clear();
                Eq(CardAnimOrder.TryGetPlayBeats("B143_NO_SUCH_CARD", lp), 0, "H8 未知卡 ⇒ 0 拍");
                Eq(CardAnimOrder.PlayFallbackHits, hits0 + 1, "H8b 且 `PlayFallbackHits` **+1**（挡「静默返回 0」）");

                // H9：**跨表一致性** —— 同一张卡同一条 trigger-0 行，两张表必须给同一个 `ca`
                //     （两张表同源「全量件的 path==cardAbilities」；分裂 = 生成器口径漂了）
                var lt = new List<CardAnimBeat>();
                var lp2 = new List<CardAnimBeat>();
                bool tOk = CardAnimOrder.TryGetTriggerBeats("EC29", 0, lt) == 1;
                bool pOk = CardAnimOrder.TryGetPlayBeats("EC29", lp2) == 1;
                Check(tOk && pOk, "H9 **前提**：`EC29` 在两张表里各 1 拍（trigger 表 " + (tOk ? "1" : "≠1")
                                                          + " / 出牌表 " + (pOk ? "1" : "≠1") + "）");
                if (tOk && pOk)
                    EqS(CaAt(lp2, 0), CaAt(lt, 0), "H9b★ `EC29` 在 trigger 表与出牌表里是**同一个 `ca`**（两表同源，分裂即红）");

                CardAnimOrder.ResetPlayForTest();
            }
        }

        Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }
}
