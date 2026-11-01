// B114_W19_Assert.cs — 写手 `W-19`（第一百一十四会话）的断言
//
// ============================ 它盯的是什么 ============================
//   活状态表 `B-251`：**`randomAbilityChoice` 只接在关键词路**（`RuleCore.cs:12933` 的 `FireTriggerAt`），
//   数值路 `FireTriggerByNumberWithSink` 完全没接 ⇒ 本条把数值路接上，并钉住两条路同语义。
//
// ============================ 原版口径（判据，现读 `d:/2/tools/decomp_full/`）============================
//   · `RawCardScript__TriggerAbility.c:75` `cVar3 = *(char *)(param_2 + 0x48);`
//     （`+0x48` = `CardAbility.randomAbilityChoice`；字段声明见 `AbilityLogic.cs` 同族注释
//     与 `CardDef.CardAnimTime.RandomChoice` 的 doc）
//   · 同文件 `:82-94`：为真 ∧ `abilityLogic(+0x50).Count > 1` ⇒ `BattleManager__GetRandomInt(0, Count)`
//     抽一条 → `AbilityLogic__PlayAbility(那一条)` → **直接 `return`**（其余 abilityLogic 不跑）。
//   · 🔴 **两条路在原版是同一条**：`RawCardScript__TriggerAbilities.c` 的**三处** `TriggerAbility` 调用点
//     `:184`（附魔趟）/ `:231`（无隐藏目标）/ `:257`（按隐藏目标展开）**调的是同一个函数**，
//     而「触发值选哪条 ability」只在 `CardAbility__CanTriggerAbility.c:71` 比一次
//     ⇒ **关键词触发与数值触发共用 `TriggerAbility`**，`ability+0x48` 对两条路**都成立**。
//     ⛔ 不存在「数值路本来绕过它」这一支 —— 我先回源逐句查过才动手（brief 要求 ②）。
//
// ============================ 我们怎么接的（⛔ 别推翻）============================
//   · `RuleCore.RandomAbilityChoiceOps(...)` 加**第 7 形参** `string triggerName = null`（可选尾参）：
//     `tn = 非空 ? triggerName : TriggerNameOfKeyword(keyword)` ⇒ 关键词路（不传第 7 参）**逐字不变**。
//   · `FireTriggerByNumberWithSink` 的队列体那一行（同一物理行）把 `ops` 包一层
//     `RandomAbilityChoiceOps(ctx, u, ops, null, null, false, tn)` ⇒ 数值路在**出队那一刻**做同一跳。
//   · 判法只有一份（`RandomAbilityChoiceOps`），⛔ 别另写第二份。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B1/B2：删掉 `FireTriggerByNumberWithSink` 队列体里那层 `RandomAbilityChoiceOps` 包装
//     ⇒ 两组 op 全跑（护甲 6）⇒ B1 红（B 组是「随机选择被消费」的判别式）。
//   · C2：把第 7 形参 `triggerName` 那一跳退回 `TriggerNameOfKeyword(keyword)`（数值路 keyword 是 null）
//     ⇒ `tn == null` ⇒ 整跳短路 ⇒ 又变成 6 ⇒ C2 的「两个不同」不成立 ⇒ 红。
//   · D1：`RandomAbilityChoiceOps` 把 `tn` 换成常量 / 删掉 `TriggerRandomChoice` 闸 ⇒ 关键词路不再抽 ⇒ 红。
//   · ⚠️ **C1（对照组 = 6）与 B1（2 或 4）结构上不可能同时被「两边一起改回去」满足** —— 这就是灭自证。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B114W19_Assert", B114_W19_Assert.Run); _pass += B114_W19_Assert.Pass; _fail += B114_W19_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B114_W19 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B114_W19_Assert
{
    /// <summary>数值路用的 trigger 数值 / 名字对（`CardDef.AbilityTriggerNumberOfName` 里的真值：
    /// `{"UnitDamaged", 60}`）。挑 60 而不是 410：410 那条路另有 `followAfterResolution` 分支，会混进干扰。</summary>
    const int TnNumber = 60;
    const string TnName = "UnitDamaged";

    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B114_W19：`B-251` `randomAbilityChoice` 接上**数值路**（原版 `ability+0x48` 对两条路都成立）");

        // ---- A 组：前提（夹具确实带 `randomAbilityChoice`，且正文分成两组）--------------------
        Section("A 前提：夹具的 `randomAbilityChoice` 与两组正文 op");
        List<EffectOp> opsRc, opsPlain, opsMob;
        var cardRc = Fixture("B114W19Rc", randomChoice: true, triggerName: TnName, out opsRc);
        var cardPlain = Fixture("B114W19Plain", randomChoice: false, triggerName: TnName, out opsPlain);
        var cardMob = Fixture("B114W19Mob", randomChoice: true, triggerName: "Mob", out opsMob);

        CheckTrue(cardRc.TriggerRandomChoice(TnName),
                  "A1 ★★ **前提**：夹具卡确实带 `randomAbilityChoice`（`TriggerRandomChoice(\"" + TnName + "\") == true`）"
                + " —— 这一条不成立，下面整组都在测别的东西");
        CheckTrue(!cardPlain.TriggerRandomChoice(TnName),
                  "A2 前提：对照组（`RandomChoice = false`）**不带** —— 它是灭自证的另一半");
        Check(CountGroups(opsRc), 2,
              "A3 前提：正文 op 分成 **2 组**（`LogicIndex` 0 / 1）—— 原版要 `Count > 1` 才会抽");
        CheckTrue(cardRc.TriggerOpsByNumber(TnNumber) != null && cardRc.TriggerOpsByNumber(TnNumber).Count == opsRc.Count,
                  "A4 前提：`TriggerOpsByNumber(" + TnNumber + ")` 挂得上（数值路就是从这里取正文的）");

        // ---- B 组：结论（数值路下随机选择**被消费**）------------------------------------------
        Section("B 结论：走**数值路** `FireTriggerByNumber` ⇒ 只跑**一组**正文");
        int armourRc = -1, armourPlain = -1;
        bool firedRc = false, firedPlain = false;
        {
            var ctx = MakeBattle(cardRc, out var carrier);
            CheckTrue(carrier != null, "B0 前提：载体上场了（0 号玩家格 3）");
            if (carrier != null)
            {
                Check(carrier.Armor, 0, "B0b 前提：打之前护甲 0");
                firedRc = RuleCore.FireTriggerByNumberForTest(ctx, carrier, TnNumber, 0, 3, null, null);
                RuleCore.PumpActionQueues(ctx);
                armourRc = carrier.Armor;
                CheckTrue(firedRc, "B0c 前提：数值路真的入队了一条（`FireTriggerByNumber` 返回 true）");
                CheckTrue(armourRc == 2 || armourRc == 4,
                          "B1 ★★★ **结论（随机选择被消费）**：护甲 = " + armourRc + "，只可能是 **2 或 4**"
                        + "（= 两组正文 `Gain Armour 2` / `Gain Armour 4` 里**只跑了一组**）"
                        + "｜🧨 没接那一跳 ⇒ 两组全跑 = 6 ⇒ 红");
                CheckTrue(armourRc != 6,
                          "B2 ★★★ 反面：护甲 **≠ 6** ⇒ 不是「两组全跑」"
                        + "（2 + 4 = 6 是「`randomAbilityChoice` 没被消费」时的值）");
            }
        }

        // ---- C 组：灭自证（同构夹具只改 `randomAbilityChoice`）--------------------------------
        Section("C 灭自证：同构夹具只改 `randomAbilityChoice`，差值必须来自它本身");
        {
            var ctx = MakeBattle(cardPlain, out var carrier);
            CheckTrue(carrier != null, "C0 前提：对照组载体上场了");
            if (carrier != null)
            {
                firedPlain = RuleCore.FireTriggerByNumberForTest(ctx, carrier, TnNumber, 0, 3, null, null);
                RuleCore.PumpActionQueues(ctx);
                armourPlain = carrier.Armor;
                CheckTrue(firedPlain, "C0b 前提：对照组也入队了（否则「没跑」会被误读成「随机抽走了」）");
                Check(armourPlain, 6,
                      "C1 ★★★ 对照组（`RandomChoice = false`）⇒ **两组全跑** = 护甲 6"
                    + "（= 原版「`ability+0x48` 为假 ⇒ 逐条 `PlayAbility`」那一支）");
                CheckTrue(armourRc != armourPlain,
                          "C2 ★★★ **灭自证**：`带 rc` 的 = " + armourRc + "，`不带 rc` 的 = " + armourPlain
                        + " ⇒ 两者不等"
                        + "｜🧨 把第 7 形参那一跳退回 `TriggerNameOfKeyword(keyword)`（数值路 keyword 是 null）"
                        + " ⇒ 两个都是 6 ⇒ 本行红；🧨 把队列体那层包装删掉 ⇒ 同样红");
            }
        }

        // ---- D 组：关键词路**行为不变**（第 7 形参缺省那一支）----------------------------------
        Section("D 关键词路不变：`RandomAbilityChoiceOps` 不传第 7 形参 ⇒ 仍按 `TriggerNameOfKeyword(keyword)` 判");
        {
            var ctx = MakeBattle(cardMob, out var carrier);
            CheckTrue(carrier != null, "D0 前提：关键词路夹具载体上场了");
            if (carrier != null)
            {
                var picked = RuleCore.RandomAbilityChoiceOpsForTest(ctx, carrier, opsMob, KeywordTable.Mob);
                CheckTrue(picked != null && picked.Count < opsMob.Count,
                          "D1 ★★ **关键词路照旧抽取**：`RandomChoice = true` ∧ trigger 名 `Mob` ⇒ 返回 "
                        + (picked != null ? picked.Count : -1) + " 条（< " + opsMob.Count + " 条，只跑一组）"
                        + "｜🧨 把 `tn` 那一跳写死 / 删掉 `TriggerRandomChoice` 闸 ⇒ 返回全量 ⇒ 红");

                var ctx2 = MakeBattle(cardPlain, out var carrier2);
                if (carrier2 != null)
                {
                    var all = RuleCore.RandomAbilityChoiceOpsForTest(ctx2, carrier2, opsPlain, KeywordTable.Mob);
                    CheckTrue(all != null && all.Count == opsPlain.Count,
                              "D2 ★★ **无 `randomAbilityChoice` ⇒ 原样返回**（= 改前行为，"
                            + (all != null ? all.Count : -1) + " 条 = 全量 "
                            + opsPlain.Count + " 条）");
                }
                else CheckTrue(false, "D2 前提不成立：对照组载体没上场");
            }
        }

        // ---- E 组：**附魔**那半条数值路（`FireEnchantmentAbilitiesByNumber`）--------------------
        //   判据：原版 `RawCardScript__TriggerAbilities.c:184`（附魔趟）调的**也是** `RawCardScript__TriggerAbility`，
        //   而后者的 `ability+0x48` 那一跳（`.c:75/:82-94`）**与能力属于哪张卡无关**。
        Section("E 附魔数值路：**附魔卡**的 `randomAbilityChoice` 同样被消费");
        {
            var hostCard = PlainUnit("B114W19HostE");
            var enchRc = Fixture("B114W19EnchRc", randomChoice: true, triggerName: TnName, out _);
            var enchPlain = Fixture("B114W19EnchPlain", randomChoice: false, triggerName: TnName, out _);

            CheckTrue(enchRc.TriggerRandomChoice(TnName),
                      "E0 ★★ **前提**：附魔夹具卡确实带 `randomAbilityChoice`（`TriggerRandomChoice(\"" + TnName + "\") == true`）");

            int aRc = -1, aPlain = -1;
            {
                var ctx = MakeBattleWithEnchant(hostCard, enchRc, out var carrier);
                CheckTrue(carrier != null, "E0b 前提：载体上场了（0 号玩家格 3）");
                if (carrier != null)
                {
                    CheckTrue(Enchantments.HasAny(carrier), "E0c 前提：附魔真的挂上了（`Enchantments.HasAny`）");
                    Check(carrier.Armor, 0, "E0d 前提：派发前护甲 0");
                    bool fired = RuleCore.FireEnchantmentAbilitiesByNumberForTest(ctx, carrier, TnNumber, TnName, 0, 3, null, null) > 0;
                    RuleCore.PumpActionQueues(ctx);
                    aRc = carrier.Armor;
                    CheckTrue(fired, "E0e 前提：附魔那一趟**真的派发了**（返回 > 0；否则后面的读数什么都证明不了）");
                    CheckTrue(aRc == 2 || aRc == 4,
                              "E1 ★★★ **结论（附魔数值路下随机选择被消费）**：护甲 = " + aRc + "，只可能是 **2 或 4**"
                            + "（两组正文 `Gain Armour 2` / `Gain Armour 4` 里只跑了一组）"
                            + "｜🧨 把 `EffectResolverPartial_B109E6.cs:134` 那层 `RandomAbilityChoiceOps` 删掉 ⇒ 6 ⇒ 红");
                    CheckTrue(aRc != 6,
                              "E2 ★★★ 反面：护甲 **≠ 6** ⇒ 不是「两组全跑」");
                }
            }
            {
                var ctx = MakeBattleWithEnchant(hostCard, enchPlain, out var carrier);
                CheckTrue(carrier != null, "E3a 前提：对照组载体上场了");
                if (carrier != null)
                {
                    bool fired = RuleCore.FireEnchantmentAbilitiesByNumberForTest(ctx, carrier, TnNumber, TnName, 0, 3, null, null) > 0;
                    RuleCore.PumpActionQueues(ctx);
                    aPlain = carrier.Armor;
                    CheckTrue(fired, "E3b 前提：对照组附魔**也**派发了（否则「没跑」会被误读成「被随机抽走」）");
                    Check(aPlain, 6,
                          "E3 ★★★ **灭自证（「不传 tn」那一档 = 这张卡上没有 `rc`）**：护甲 = 6 ⇒ **两组全跑**（= 改前行为）"
                        + "｜判据 `RawCardScript__TriggerAbility.c:95-106`（`ability+0x48` 为假 ⇒ 逐条 `PlayAbility`）");
                    CheckTrue(aRc != aPlain,
                              "E4 ★★★ **灭自证**：`带 rc` = " + aRc + "，`不带 rc` = " + aPlain + " ⇒ 两者**不等**"
                            + "｜🧨 把第 7 形参 `tn` 丢掉（`RandomAbilityChoiceOps` 内部只剩 `TriggerNameOfKeyword(keyword)`，"
                            + "而数值路 `keyword` 是 null）⇒ 两个都是 6 ⇒ 本行红");
                }
            }
        }

        Summary();
    }

    // ==================================================================
    //  夹具 / 读数
    // ==================================================================

    /// <summary>两组正文：`LogicIndex 0` = `Gain Armour 2`（护甲 +2）、`LogicIndex 1` = `Gain Armour 4`（+4）。
    /// ⇒ 随机抽一组 = **2 或 4**；两组全跑 = **6**。三者互不重叠 ⇒ 读数能直接判「抽没抽」。</summary>
    static CardDef Fixture(string id, bool randomChoice, string triggerName, out List<EffectOp> ops)
    {
        var g0 = Group("Gain Armour 2", 0);
        var g1 = Group("Gain Armour 4", 1);
        ops = new List<EffectOp>();
        ops.AddRange(g0);
        ops.AddRange(g1);

        // ⚠️ **两组用同一张动画行，只差 `RandomChoice`** —— 这是灭自证的前提：`EmitCardTrigger` 的
        //   `HasTriggerSource` 要它（没有动画行 ⇒ 附魔那一趟整条发不出来，对照组会被读成「没跑」
        //   而不是「两组全跑」）。B/C 组也因此在更强的意义上同构（只差那一格）。
        CardDef.CardAnimTime[] anims = new[]
        {
            new CardDef.CardAnimTime { RandomChoice = randomChoice, TriggerName = triggerName }
        };

        var c = new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 5, 0, null,
                            subtype: "Infantry", animTimes: anims);
        c.AttachTriggerOpsByNumber(new Dictionary<int, List<EffectOp>> { { TnNumber, ops } });
        return c;
    }

    /// <summary>解一句卡面 ⇒ 一组 op，并把 `LogicIndex` 钉成 <paramref name="logicIndex"/>
    /// （= 原版 `abilityLogic` 的下标；`RandomAbilityChoiceOps` 就按它分组）。</summary>
    static List<EffectOp> Group(string text, int logicIndex)
    {
        var ops = EffectText.Parse(text, out _, out _);
        if (ops == null) return new List<EffectOp>();
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i] == null) continue;
            ops[i].AbilityIndex = 0;
            ops[i].LogicIndex = logicIndex;
        }
        return ops;
    }

    static int CountGroups(List<EffectOp> ops)
    {
        if (ops == null) return 0;
        var set = new HashSet<int>();
        for (int i = 0; i < ops.Count; i++) if (ops[i] != null) set.Add(ops[i].LogicIndex);
        return set.Count;
    }

    /// <summary>一场「督军 + 一张载体 + 一张对手单位」的干净对局；返回时 <paramref name="carrier"/> = 已摆到 0 号玩家格 3 的载体。</summary>
    static BattleContext MakeBattle(CardDef carrierCard, out UnitState carrier)
    {
        var heroA = new CardDef("B114W19HeroA", "B114W19HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B114W19HeroB", "B114W19HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var foeCard = new CardDef("B114W19Foe", "B114W19Foe", "unit", "", null, "Test", 1, 1, 5, 0, null);
        var ctx = RuleCore.NewBattle(new List<CardDef> { heroA, carrierCard },
                                     new List<CardDef> { heroB, foeCard },
                                     seed: 0, shuffle: false, cardPool: new List<CardDef>());
        carrier = PlaceUnit(ctx, 0, 3, carrierCard);
        PlaceUnit(ctx, 1, 3, foeCard);
        return ctx;
    }

    static UnitState PlaceUnit(BattleContext ctx, int p, int slot, CardDef card)
    {
        if (ctx == null || card == null) return null;
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>一张干净的载体卡（不带任何 ability / 正文）—— E 组只测**附魔**那一趟。</summary>
    static CardDef PlainUnit(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry");
    }

    /// <summary>同一场对局，在载体身上挂一件附魔。**挂载口只走 `Enchantments.Attach`**
    /// （它是 `ActiveEnchantments` / `ActiveEffects` 两本表的唯一写点 —— ⛔ 别在这里手搓两条，
    ///  `Enchantments.AllSpent` 靠 `ActiveEffects` 那一本判「花掉」，手搓漏了会让整件被静默跳过）。</summary>
    static BattleContext MakeBattleWithEnchant(CardDef carrierCard, CardDef enchCard, out UnitState carrier)
    {
        var ctx = MakeBattle(carrierCard, out carrier);
        if (ctx == null || carrier == null || carrier.Instance == null) return ctx;
        Enchantments.Attach(ctx, carrier.Instance, enchCard, Enchantments.BuffTypeSingleUse);
        return ctx;
    }

    // ==================================================================
    //  最小断言框架（本文件独立类 ⇒ 不借用 `RuleEngineTest` 的 private 助手）
    // ==================================================================

    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B114_W19 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B114_W19 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口（追加到类尾、零行漂）。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
