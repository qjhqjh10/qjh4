// B99W3_Assert.cs — 第九十九会话 · 波 2 · 执行代理 `B99_W3`（卡牌 #11 / #12 · 特效 #14 · 界面 #6）的断言
//
// ============================ 它盯的是什么（判据只写在这里一次）============================
//  A 特效 #14 —— `CardPresentation/Battle/ScenarioMaterialFader.cs` 的 `Advance` **帧内次序**。
//     判据 = `d:/2/tools/decomp_full/ScenarioMaterialFader__Update.c`（+ 同族的
//     `ScenarioGenericMaterialBlend__Update.c`，两份的控制流是同一个形状）：
//       `:15` `if (doFade == 0) return;`
//       `:19-29` **每帧第一件事**就是 `renderer.GetMaterial() → Material.get_color → Material.set_color`
//                （对每个 renderer 各一次；α 用**递减前**的 `currentBlendTime(+0x4c)`）
//       `:31`  `if (0.0 < currentBlendTime) goto LAB_180630d1a;` ⇒ 只有 `<= 0` 才走「完成」那一段
//                （`OnComplete` 回调 / 还原原材质 / `disableOnFadeOut`）
//       `:75` / `:79` `LAB_180630d1a`：**末尾**才 `currentBlendTime -= Time.deltaTime`
//     ⇒ 三句的**次序**就是本段要盯的东西：改前是「先退计时 → 再 `if (_busy)` 写」，
//       `Complete()` 会把 `_busy` 清掉 ⇒ **收尾那一帧 α 一次都没写**，最后一个 α 停在前一帧的中间值。
//  B 界面 #6 —— `RuleEngine/Data/DeckStore.cs` 新拆出来的 `Parse` / `Serialize` 两跳
//     （Step 4 双写「两行契约」的**数据半**；整份判据 = 方案 §七 Step 4）。
//     盯三件：① 往返逐字段相等；② `null` ⇒ `NoSaveFile`（第一次跑，⛔ 不是失败）；
//     ③ **「空集合」≠「缺键」**（`decks: []` 合法 ⇒ `None`；缺 `decks` 键 ⇒ `Empty`）。
//  C 卡牌 #11 —— `RuleEngine/Core/CardDef.cs` 类尾的 `AbilityEffectTexts` / `EffectTextOf`（v44 列）
//     判据 = `资料/完全复刻/判据/activeAbilityTextReferenceId_真值.md` §三·2：
//       `CardEffect.effectText(+0x28)` → `GameStaticData.GetEffectDesc`（`:1639`）→
//       term = `"Card_Abilities/" + effectText`（`:1772`）→ **缺键时原样返回 effectText 本身**（`:1778-1782`）。
//     盯三态：`null` = 整列未知 / `""` = 明确没有 / 非空 = 原文；另盯「同一个 (ab,lg) 的
//     `buffEffect` 与 `handBuff.cardEffect` 是**两格**」（`TAU51` 那种形状）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1：把 `Advance` 改回「先 `base.Advance(dt)` 再 `if (_busy) ApplyFade(...)`」⇒ 收尾帧不写 α
//        ⇒ A1 红（实得停在前一帧的中间值 0.5，而不是 `_toA` 的 0）。
//  · A2：把 `ApplyFade` 改成「只在收尾那一帧写」（`if (_left <= 0f) ApplyFade(1f)`）⇒ 中途两帧 α 不动
//        ⇒ A2 红。A1 与 A2 结构上不可能同时满足（一个要「收尾帧必须写」、一个要「中途帧必须也在写」）。
//  · B1：`Serialize` 改回手写字符串（漏 `current`）⇒ B1 红。
//  · B2：`Parse` 里把 `text == null` 也归一成空表 + `note = None` ⇒ B2 红（「第一次跑」被说成「读到了」）。
//  · B3/B4：把 `!hasDecksKey` 判据删掉（只判 `dto.decks == null`）⇒ B4 红而 B3 仍绿 ——
//        这两条**必须分开**：B3 挡「把空集合当缺键」，B4 挡「把缺键当空集合」。
//  · C1：把 `AttachAbilityEffectTexts` 里的 `_abilityEffectTexts = rows` 改成 `rows ?? new[0]`
//        ⇒ 「未知」档没了 ⇒ C1 红（`HasAbilityEffectTexts` 会变 true）。
//  · C2：把 `EffectTextOf` 的「查不到 ⇒ `""`」改成「查不到 ⇒ `null`」⇒ C2 红。
//  · C3：`AbilityEffectTextAt` 不比 `IsHand` ⇒ C3 红（同 (ab,lg) 两格会互相串）。
//  · C5：`EffectTextTermOf` 少拼 `"Card_Abilities/"` 前缀 ⇒ C5 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/BattleScene.cs` 的 `Run()` 里那一串代接线之后加：
//      B99W3_Assert.Run(); pass += B99W3_Assert.Pass; fail += B99W3_Assert.Fail;
//  （宿主由调度台代接线；本文件只碰公开只读口 ＋ 自建临时对象，**不写盘、不碰玩家真档**。）
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B99_W3`：特效 #14（材质淡出收尾次序）/ 界面 #6（`DeckStore` 的 Parse·Serialize）/ 卡牌 #11（`effectText` 三态）。</summary>
public static class B99W3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[W3] ") { Near = MenuNearStyle.Compact3 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B99_W3 特效 #14 / 界面 #6 / 卡牌 #11");
        SectionA_MaterialFaderOrder();
        SectionB_DeckStoreBridge();
        SectionC_EffectTextThreeStates();
        SectionD_RealCards();
        SectionE_LevelUpPulseSchedule();
    }

    // ==========================================================================================
    //  A 段 —— `ScenarioMaterialFader.Advance` 的帧内次序（先写 α → 再判完成 → 末尾才递减）
    // ==========================================================================================
    static void SectionA_MaterialFaderOrder()
    {
        MenuCheck.Section(_sink, "A 特效 #14：材质淡出的收尾次序（原版 `ScenarioMaterialFader__Update.c`）");

        GameObject go = null;
        try
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var r = go.GetComponent<Renderer>();
            var f = go.AddComponent<ScenarioMaterialFader>();
            f.renderers = new Renderer[] { r };
            f.fadeOnEnable = false;
            f.isInDefaultScenario = false;
            f.restoreOriginalMaterialsOnFadeOut = false;
            f.restoreOriginalMaterialsOnFadeIN = false;
            f.disableOnFadeOut = false;

            Tr(r != null && r.sharedMaterial != null, "A0 **前提**：夹具那颗粒子/网格有 `sharedMaterial`（否则 `SetAlpha` 整段空转）");

            // 淡出：α 1 → 0，时长 0.5，每帧 0.25 ⇒ 第 3 帧必须写下 α = 0 并完成。
            f.DoScenarioBlend(new ScenarioBlendOptions { duration = 0.5f, direction = false, targetValue = 0f });
            Tr(f.Busy, "A0-a **前提**：`DoScenarioBlend` 之后 `Busy`（否则 `Advance` 首句就 return、本节全空转）");

            float a0 = r.sharedMaterial.color.a;
            f.Advance(0.25f);
            float a1 = r.sharedMaterial.color.a;
            f.Advance(0.25f);
            float a2 = r.sharedMaterial.color.a;
            f.Advance(0.25f);
            float a3 = r.sharedMaterial.color.a;

            Near(a0, 1f, 1e-4f, "A0-b **前提**：起始 α = 1（`DoScenarioBlend` 的 `_fromA` 取的就是当前 α）");
            // 第 1 帧写的是 `t = 1 - duration/duration = 0` ⇒ **不动**（原版第一帧写回的就是原值）；
            // 第 2/3 帧才是 0.5 / 0（`MoveTowards` 恒定速率，`_fromA = 1`、`_toA = 0`）。
            Near(a1, a0, 1e-6f, "A2-a 第 1 帧 `t=0` ⇒ α 不动（原版每帧写的是 `MoveTowards(from, to, dt/blendTime)` 的累计量）");
            Tr(a2 < a1 && a3 < a2,
               "A2 **中途帧也在写**、且单调下降（α: " + a0.ToString("F3") + " → " + a1.ToString("F3")
             + " → " + a2.ToString("F3") + " → " + a3.ToString("F3") + "）"
             + "｜🧨 把 `ApplyFade` 改成「只在收尾那一帧写」⇒ 中途两帧不动 ⇒ 本行红");
            Near(a3, 0f, 1e-4f,
                 "A1 **收尾那一帧真的写了 α = `_toA`**（实得 " + a3.ToString("F4")
               + "；改前会停在 " + a2.ToString("F4") + " 那一档、永远到不了 0）"
               + "｜🧨 把 `Advance` 改回「先 `base.Advance` 再 `if (_busy) ApplyFade`」⇒ 本行红");
            Tr(!f.Busy, "A1-a 收尾那一帧确实走完了 `Complete()`（`Busy == false`）");

            // 原版完成时会把 `doFade` 归零（`Update.c:75`）⇒ 下一帧整段 return（α 不再被改）。
            float a4 = r.sharedMaterial.color.a;
            f.Advance(0.25f);
            Near(r.sharedMaterial.color.a, a4, 1e-6f, "A3 完成之后**不再写**（`Complete()` 把 `_doFade`/`_busy` 都清了）");

            // 淡入方向那一档（`fadeOnEnable && direction` ⇒ 先把 α 置 0、`targetFade` 恒 1.0 **不看** `targetValue`）。
            // ⚠️ 这里故意传 `targetValue = 0` —— 走通了就证明「取 1.0 而不是取 `targetValue`」那一句在。
            f.fadeOnEnable = true;
            f.DoScenarioBlend(new ScenarioBlendOptions { duration = 0.4f, direction = true, targetValue = 0f });
            Near(r.sharedMaterial.color.a, 0f, 1e-4f, "A4-a 淡入档起来那一刻 α 被置 0（原版 `DoScenarioBlend` 的 `uStack_3c = 0`）");
            f.Advance(0.2f); f.Advance(0.2f); f.Advance(0.2f);
            Near(r.sharedMaterial.color.a, 1f, 1e-4f,
                 "A4 淡入那一档收尾到 1.0 —— 而且**传的是 `targetValue = 0`**"
               + "（原版 `fadeOnEnable && direction` 支写死 `targetFade = 0x3f800000`，不看 `targetValue`）");
        }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ==========================================================================================
    //  B 段 —— `DeckStore.Serialize` / `Parse`（Step 4 切换口的数据半）
    // ==========================================================================================
    static void SectionB_DeckStoreBridge()
    {
        MenuCheck.Section(_sink, "B 界面 #6：`DeckStore.Parse` / `Serialize` 三态（空集合 ≠ 缺键）");

        var decks = new List<PlayerDeck>
        {
            new PlayerDeck("B99W3_甲", "UM1", "", new List<string> { "UM1", "UM2" }, 0),
            new PlayerDeck("B99W3_乙", "DA1", "", new List<string> { "DA1" }, 13),
        };
        string json = DeckStore.Serialize(decks, 1);
        Tr(json != null && json.Length > 2 && json[0] == '{' && json[json.Length - 1] == '}',
           "B0 **前提**：`Serialize` 产出的是 JSON **对象**（`SaveThroughLegacy` 只收 `{…}`，否则当场拒）");

        // ① 往返：逐字段相等（名字 / 督军 / 卡表 / 模式 / current）
        DeckStore.LoadNote note; string detail; int cur;
        var back = DeckStore.Parse(json, out note, out detail, out cur);
        Tr(back != null && back.Count == 2, "B1 往返：卡组套数不变（实得 " + (back == null ? -1 : back.Count) + "）");
        // ⛔ 不用 `if (back != null) Check(...)` 那种静默跳过：下面四个取值都带兜底，
        //    前提不成立时它们照样被求值、照样报红（B1 已先断「2 套」）。
        bool two = back != null && back.Count == 2;
        C(two ? back[0].Name : "<null>", "B99W3_甲", "B1-a 往返：第 0 套名字");
        C(two ? back[1].WarlordId : "<null>", "DA1", "B1-b 往返：第 1 套督军");
        C(two ? back[1].GameMode : -1, 13, "B1-c 往返：第 1 套模式（`13` 遭遇）");
        Tr(two && back[0].CardIds != null && back[0].CardIds.Count == 2, "B1-d 往返：第 0 套卡表 2 张");
        C(cur, 1, "B1-e 往返：`current` 原样带回来（⛔ 别被归一成 0）");
        C(note, DeckStore.LoadNote.None, "B1-f 往返：码 = `None`（读到了一份合法存档）");

        // ② 第一次跑：`null` ⇒ `NoSaveFile`（⛔ 不是 `None`、也⛔ 不是 `ReadFailed`）
        var none = DeckStore.Parse(null, out note, out detail, out cur);
        Tr(none != null && none.Count == 0, "B2-a `Parse(null)` 回空表（不抛）");
        C(note, DeckStore.LoadNote.NoSaveFile, "B2 `Parse(null)` 的码 = `NoSaveFile`（「第一次跑」）"
                                            + "｜🧨 把它归一成 `None` ⇒ 「第一次跑」被说成「读到了」⇒ 本行红");
        Tr(!string.IsNullOrEmpty(detail), "B2-b `detail` 非空（它是「`DeckStore` 有没有话要说」那个信号）");

        // ③ **空集合 ≠ 缺键**（两条必须分开断）
        int cur3;
        var empty = DeckStore.Parse("{\"version\":1,\"current\":0,\"decks\":[]}", out note, out detail, out cur3);
        C(note, DeckStore.LoadNote.None, "B3 **`decks: []` 是合法存档**（删光卡组的玩家）⇒ 码 = `None`、⛔ 不是 `Empty`");
        Tr(empty != null && empty.Count == 0, "B3-a `decks: []` ⇒ 0 套（不是 null）");

        var miss = DeckStore.Parse("{\"version\":1,\"current\":0}", out note, out detail, out cur3);
        C(note, DeckStore.LoadNote.Empty, "B4 **缺 `decks` 键** ⇒ 码 = `Empty`"
                                       + "｜🧨 删掉 `hasDecksKey` 判据（只判 `dto.decks == null`）⇒ 本行红而 B3 仍绿（两条必须分开）");
        Tr(miss != null && miss.Count == 0, "B4-a 缺键 ⇒ 空表（不抛）");

        // ④ 坏档：至少不能静默说「读到了」（具体码允许 `Empty` 或 `ReadFailed` —— `JsonUtility` 对垃圾串的两条路都见过）
        int cur4;
        var bad = DeckStore.Parse("这不是 JSON {{{", out note, out detail, out cur4);
        Tr(note != DeckStore.LoadNote.None, "B5 坏档 ⇒ 码 **不**是 `None`（有话要说；实得 " + note + "）");
        Tr(bad != null, "B5-a 坏档 ⇒ 回空表而不是 null（调用方不必判空）");
    }

    // ==========================================================================================
    //  C 段 —— `CardDef.AbilityEffectTexts` / `EffectTextOf` 三态
    // ==========================================================================================
    static void SectionC_EffectTextThreeStates()
    {
        MenuCheck.Section(_sink, "C 卡牌 #11：`CardEffect.effectText` 的三态 + 同一 `(ab,lg)` 的两格");

        var d = new CardDef("B99W3Fx", "B99W3Fx", "unit", "", "",
                            "Test", 0, 1, 3, 0, null);

        // ① 未知档（列缺席 / 夹具直建）
        Tr(!d.HasAbilityEffectTexts, "C1 **未知档**：没挂载 ⇒ `HasAbilityEffectTexts == false`"
                                   + "｜🧨 把 `AttachAbilityEffectTexts` 写成 `rows ?? new[0]` ⇒ 本行红");
        Tr(d.EffectTextOf(0, 0, false) == null, "C1-a 未知档 ⇒ `EffectTextOf == null`（判不了，⛔ 不是 \"\"）");
        Tr(d.EffectTextTermOf(0, 0, false) == null, "C1-b 未知档 ⇒ 没有取词键可拼");

        // ② 挂上真值形状：`TAU51` 那种「同一个 (ab,lg=0) 既有 buffEffect 又有 handBuff」
        d.AttachAbilityEffectTexts(new[]
        {
            new CardDef.AbilityEffectTextSpec { Ab = 0, Lg = 0, IsHand = false, At = "buffEffect", Src = "SaviorProtocolsEffect" },
            new CardDef.AbilityEffectTextSpec { Ab = 0, Lg = 0, IsHand = true,  At = "handBuff",  Src = "EffectImmortalPride" },
            new CardDef.AbilityEffectTextSpec { Ab = 0, Lg = 1, IsHand = false, At = "buffEffect", Src = "EffectHelspear" },
        });
        Tr(d.HasAbilityEffectTexts, "C1-c 挂载之后 `HasAbilityEffectTexts == true`");
        C(d.EffectTextOf(0, 0, false), "SaviorProtocolsEffect", "C3-a `buffEffect` 那一格取到自己的值");
        C(d.EffectTextOf(0, 0, true), "EffectImmortalPride", "C3 **同一个 (ab=0,lg=0) 的 `handBuff` 是另一格**"
                                                            + "｜🧨 `AbilityEffectTextAt` 不比 `IsHand` ⇒ 两格串味 ⇒ 本行红");
        C(d.EffectTextOf(0, 1, false), "EffectHelspear", "C3-b 别的 lg 各取各的");
        C(d.EffectTextOf(0, 1, true), "", "C2 **明确没有** ⇒ `\"\"`（不是 `null`）"
                                       + "｜🧨 把「查不到」改成回 `null` ⇒ 未知与明确没有混成一档 ⇒ 本行红");
        C(d.EffectTextOf(7, 7, false), "", "C2-a 不存在的行 ⇒ 同样 `\"\"`（列在、这一格空）");

        // ③ 取词键：原版 `"Card_Abilities/" + effectText`（`GameStaticData.cs:1772`）
        C(d.EffectTextTermOf(0, 1, false), "Card_Abilities/EffectHelspear",
          "C5 取词键 = `Card_Abilities/<src>`｜🧨 少拼前缀 ⇒ 本行红");
        Tr(d.EffectTextTermOf(0, 1, true) == null, "C5-a 没有标题那一格 ⇒ 没有键（⛔ 别回 `Card_Abilities/`）");

        // ④ 全量行可读（给调度台做「表里到底几行」的读数）
        C(d.AbilityEffectTexts == null ? -1 : d.AbilityEffectTexts.Length, 3, "C6 行数 = 挂上去的 3 行");
    }

    // ==========================================================================================
    //  D 段 —— 真卡（**列重生成之后才活**：条件写进断言本体，⛔ 不用 `if (x != null) Check`）
    // ==========================================================================================
    static void SectionD_RealCards()
    {
        MenuCheck.Section(_sink, "D 真卡：`abilityEffectTexts` 落表后的取值（列不在 ⇒ 只证明零回归）");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B99W3] `CardDatabase.Load()` 抛了：" + e.Message); }
        Tr(pool != null && pool.Count > 0, "D0 **前提**：卡池读得到（读不到这一节全部无意义）");
        if (pool == null || pool.Count == 0) return;

        // 条件式断言：**条件不成立时它仍会被求值**（`!cond || 结论`），不是 `if (…) Check(…)` 那种静默跳过。
        var um44 = CardDatabase.FindById(pool, "UM44");
        Tr(um44 != null, "D0-a **前提**：池里有 `UM44`");
        Tr(um44 == null || !um44.HasAbilityEffectTexts
           || um44.EffectTextOf(0, 0, false) == "EffectArmouredSupport",
           "D1 `UM44`（Armoured Support）若已落列 ⇒ `(0,0,buffEffect)` = `EffectArmouredSupport`（判据 §三·甲）");

        var sau55 = CardDatabase.FindById(pool, "SAU55");
        Tr(sau55 == null || !sau55.HasAbilityEffectTexts
           || sau55.EffectTextOf(0, 2, true) == "EffectImmortalPride",
           "D2 `SAU55`（Immortal Pride）若已落列 ⇒ 标题在 **`handBuff`** 那一路的 `lg=2` 上（⛔ 不在 buffEffect）");

        var tau51 = CardDatabase.FindById(pool, "TAU51");
        Tr(tau51 == null || !tau51.HasAbilityEffectTexts
           || (tau51.EffectTextOf(0, 0, false) == "SaviorProtocolsEffect"
               && tau51.EffectTextOf(0, 0, true) == "SaviorProtocolsEffect"
               && tau51.EffectTextOf(0, 2, true) == "SaviorProtocolsEffect"),
           "D3 `TAU51`（Saviour Protocols）若已落列 ⇒ 5 格同值（lg0 buff/hand · lg1 buff/hand · lg2 hand）");

        var bl75 = CardDatabase.FindById(pool, "BL75");
        Tr(bl75 == null || !bl75.HasAbilityEffectTexts
           || bl75.EffectTextOf(0, 0, false) == "EffectIdolatrous",
           "D4 `BL75`（Idolatrous Despoilers）若已落列 ⇒ `EffectIdolatrous`"
         + "｜⚠️ 这条**只断 src**，不断文案：官方表 EN/ZH 现读**都是** `[[vulnerable]] 1`"
         + "（2026-10-18 `A1956` 已把 zh 的 `8` 改回 `1`），施工单里「ZH = 8」那句**已过期**");
    }

    // ==========================================================================================
    //  E 段 —— 卡牌 #12（施工单 §2·8 的 ①②③ **逐条**）
    //
    //  ⚠️ 这一条**不是新实现**：`Effects/CardDisplayLevelUp.cs` 的 `BeginPulse` / `StepPulse` /
    //     `ResetPulse`（＋`CardView.SetExtraScale`）在 2026-10-29 `B98_M1` 就落地了，
    //     `Editor/B98M1_Assert.cs` 的 B 段也已有 8 条。本段只补**施工单点名、而那 8 条恰好没覆盖**的两格：
    //     **② 1.5833 s 回到基准**（B98M1 只在 `AnimationFinished` 之后断过「归 1」，没断「末键那一刻就是 1」）·
    //     **③ 1.0 s 仍等于 `BaseScale`**（B98M1 的 B2 断的是**比值**，本段断的是**卡视图实际 `localScale`**
    //     —— 挡「比值对但没落到 transform 上」）。判据同 B 段：`判据/升级动画scale曲线_节点映射.md` §一·1 ① / §五·A。
    // ==========================================================================================
    static void SectionE_LevelUpPulseSchedule()
    {
        MenuCheck.Section(_sink, "E 卡牌 #12：根脉冲时刻表 ②1.5833s 回基准 · ③1.0s 仍等于基准");

        var root = new GameObject("B99W3_PULSE_ROOT");
        CardView view = null;
        CardDisplayLevelUp lv = null;
        try
        {
            var data = new CardData
            {
                id = "B99W3Pulse", title = "B99W3Pulse", isUnit = true, type = "troop",
                faction = "Ultramarines", cost = 1, melee = 1, health = 1, keywords = "",
            };
            view = CardView.Create(root.transform, data, "B99W3_PULSE");
            view.SetPose(Vector3.zero, 0f, 2f);          // 基准 2（≠ 250/108，防「写死」蒙对）
            Tr(Mathf.Approximately(view.BaseScale, 2f),
               "E0 **前提**：`CardView.BaseScale == 2`（基准不是 1，写死常数也不可能蒙对）");

            var go = new GameObject("B99W3_LevelUp");
            go.transform.SetParent(root.transform, false);
            lv = go.AddComponent<CardDisplayLevelUp>();
            lv.pulseTarget = view;

            // ③ 灭自证：前 1.4333 s 是平的 ⇒ 1.0 s 时**卡视图实际缩放**必须仍等于基准
            lv.BeginPulse(); lv.Tick(1.0f);
            Near(view.transform.localScale.x, 2f, 1e-4f,
                 "E1 t=1.0 ⇒ 卡视图 `localScale` **仍等于基准**（原版前 1.4333 s 恒 250）"
               + "｜🧨 把 `PulseRiseTime` 改成 0（一开播就冲）⇒ 本行红");

            // ② 末键那一格：`PulseFallTime` 的键值就是 250 ⇒ 比值 1、缩放回基准
            lv.BeginPulse(); lv.Tick(CardDisplayLevelUp.PulseFallTime);
            Near(lv.PulseRatio.x, 1f, 2e-4f,
                 "E2 t=1.5833（末键）⇒ 比值回到 1（原版末键三轴都是 250）");
            Near(view.transform.localScale.x, 2f, 1e-4f,
                 "E2-a …而且**卡视图实际缩放**已回基准 2（施工单 §2·8 的 ②）"
               + "｜🧨 把末键时刻写错（例如沿用峰值时刻）⇒ 本行红");
        }
        finally
        {
            if (lv != null) UnityEngine.Object.DestroyImmediate(lv.gameObject);
            if (view != null) UnityEngine.Object.DestroyImmediate(view.gameObject);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
