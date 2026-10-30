// B102TRAIT_Assert.cs — 执行代理 `B102_R3`（2026-10-30）· 账 `特效动画:503` 的**消费侧**
//
//  原版：`CardScript.TriggerOnMinionDeath` → `deadCard.ActivateTraitParticlesFromCodeInTarget(DefinedTrait.huntMark, hero)`
//  （`CardScript__TriggerOnMinionDeath.c:40/42/43/77`；ILSpy `CardScript.cs:15680-15697`）
//  = 带猎杀标记的单位阵亡时，那件 FromCode 粒子**挂在它自己那一方督军**的 `effectsAnchor` 上（取件阵营取**死者**）。
//  我们的实现 = `BattleDriver.PlayHuntMarkTraitParticles(e, v)`（类尾新增，由 `PlayDeathFeel` 在**残骸守卫之前**调）。
//  判据链 → `资料/完全复刻/判据/B102_R3_特效B类两条.md` §503（§④ 施工单 / §⑤ 还差什么）。
//
// ==================== 宿主接线（一行；⛔ 本代理不改宿主 `Editor/BattleScene.cs`）====================
//  在 `BattleScene.Run()` 里 —— `driver.Begin(...)`（现读 `:632`）之后、任一处收尾之前 —— 加：
//
//      B102TRAIT_Assert.Run(driver, d => StepThrough(d)); pass += B102TRAIT_Assert.Pass; fail += B102TRAIT_Assert.Fail;
//
//  🔴 **2026-10-30 改（本件第 2 版）**：`B102_R3` 原来直接拿宿主那一局做夹具，而宿主 `:632` 那局用的是
//    `StarterCards.EmberFaction / TideFaction`（**自设计阵营**，`cards_engine.json` 里没有）⇒ `PlayDeathFeel`
//    里 `CardDatabase.Find(_pool, name, "Tide")` **必然落空** ⇒ 任何死讯都解析不到 `CardDef`，那条链走不通。
//    ⇒ 现在 `Run` **自己造一台 `BattleDriver`**（原版阵营对 `Ultramarines` vs `Goff`，判据 = 宿主自己在
//      `:14324` 用过同一对），跑完连同整棵舞台一起 `DestroyImmediate`。
//    ⚠️ **形参 `stepThrough` 保留**（宿主那一行因此**不用改**），但本件**不用它** —— 那是给宿主那台驱动的泵；
//      自建的那台走本件自己的 `Pump`（`AdvanceTimeline` + `CardTween.Advance` + 只推本舞台下的粒子）。
//
// ==================== 期望值从哪来（⛔ 不许拿我们自己的常量证明我们自己的实现）====================
//   · **件名** = `TraitParticles.PrefabFor("huntMark")` —— 唯一来源是 `Resources/trait_effect_bindings.json` 里
//     `triggerType == 5 (FromCode)` 的那一行（从原版 `HuntMark.asset` 的 `TraitEffects[]` 生成），现读 `HuntMark_DamageEffect`；
//     **本件不写死这个字符串**，改了数据断言跟着走。
//   · **挂点** = `CardView.EffectAnchorBoard` —— 原版 `CardUI.effectsAnchor`（`CardPrefab/Board Elements/EffectAnchor`
//     的 `localPosition (0, 1.34, −0.128)`），与 `CardView.BoardEffectAnchorLocal` 同一份判据。
//   · **观测量 = 真实的场景层级**：`WarpforgeEffectPlayer.ActivePlayers` 里那个**真被 Instantiate 出来**的实例，
//     读它的 `EffectName`（件名）与 `transform.parent`（挂点）—— ⛔ 不是读我们写进代码里的字段
//     （那样两边一起改回去还会绿 = 自证）。
//   · **闸** = `EvtKind.Death.Amount`（引擎侧发信由 `B97W28` / `B100F21` 的断言钉；本件只钉消费侧）。
//
// ==================== 🔴 灭自证（每条「什么改动能让它红」）====================
//   · **删掉** `BattleDriver.PlayDeathFeel` 里那句 `PlayHuntMarkTraitParticles(e, v);` ⇒ ①/② 的 ★ 同时红。
//   · 把那句**挪到残骸守卫之后**（`if (!fromFade && Ctx != null …) { … return; }` 之后）⇒ ②（残骸档）红、
//     ① 仍绿 —— 这正是 `B98_R1` §503 那条「放在守卫之后」的错法，② 就是钉它的。
//   · 把挂点从 `wv.EffectAnchorBoard` 改成 `wv.transform`（卡根，与既有 6 个 `Play` 调用点同款）⇒ ①② 红
//     （实例的 `transform.parent` 就不再是锚点）。
//   · 把闸写成 `e.Amount >= 0`（或干脆读死者身上的关键词而不是事件的 `Amount`）⇒ ③ 红。
//
// ==================== 其它 ====================
//   · 本件**自己摆棋盘**（把敌方非督军格清空 → 摆一张单位 → 注入 `Death` → 推播放泵），
//     **棋盘在自建的那台驱动里** ⇒ 收尾直接把整棵舞台销毁，碰不到宿主一个字节。
//   · `driver.animateFeel` 必须临时打开（`PlayFeel` 只在它开着时跑，`BattleDriver.cs:24803`）。
//   · 自建驱动**不走 `Begin()`**（那一趟会把全套静态钩子抢到自己身上、还会建一整层 HUD，
//     销毁后宿主那半局就断了）⇒ 只做它真正需要的四件事：真的 `BattleContext`（`RuleCore.NewBattle`）·
//     `_pool` / `_myFaction` / `_foeFaction` 三个私有字段 · 一块 `boardRoot` · 从宿主**借**两块 `BoardLayout`
//     （`SlotPosition` 只读 `lineY/spacing`，宿主那两块就是那一局的尺子）。视图同步直接调产品的
//     `SyncBoard` / `PlaySignals`（反射，⛔ 不复制第二份实现）。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;
using WarpforgeVFX;

public static class B102TRAIT_Assert
{
    const string P = "[B102TRAIT] ";

    // 自建驱动的舞台根（`Pump` 只推这棵树下的粒子；切场时清掉）
    static Transform _stageRoot;

    const BindingFlags AnyInst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static readonly MethodInfo _syncBoard;
    static readonly MethodInfo _playSignals;
    static readonly FieldInfo _myUnitsField;
    static readonly FieldInfo _foeUnitsField;

    static B102TRAIT_Assert()
    {
        _syncBoard = typeof(BattleDriver).GetMethod("SyncBoard", BindingFlags.Instance | BindingFlags.NonPublic);
        _playSignals = typeof(BattleDriver).GetMethod("PlaySignals", BindingFlags.Instance | BindingFlags.NonPublic);
        _myUnitsField = typeof(BattleDriver).GetField("_myUnits", BindingFlags.Instance | BindingFlags.NonPublic);
        _foeUnitsField = typeof(BattleDriver).GetField("_foeUnits", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static void Check(bool ok, string msg)
    {
        if (ok) { Pass++; Debug.Log(P + "   ✓ " + msg); }
        else { Fail++; Debug.LogError(P + "   ✗ " + msg); }
    }

    /// <summary>本件的**唯一入口**（宿主在 `BattleScene.Run()` 里加一行，见文件头）。
    /// <paramref name="stepThrough"/> = `BattleScene.StepThrough` 的包装（`d => StepThrough(d)`）——
    /// **保留形参只为宿主那一行不用改**：本件自建驱动，走自己的 `Pump`，⛔ 不用宿主的泵。</summary>
    public static void Run(BattleDriver hostDriver, Action<BattleDriver> stepThrough)
    {
        Pass = 0; Fail = 0;

        GameObject stage = null;
        List<WarpforgeEffectPlayer> fxBefore = SnapshotEffects();
        try
        {
            Check(_syncBoard != null && _playSignals != null
                  && _myUnitsField != null && _foeUnitsField != null,
                  "（前提）`BattleDriver.SyncBoard` / `PlaySignals` / 两个视图表都还在（本件靠它们摆棋盘与排事件，⛔ 不改产品代码）");
            if (_syncBoard == null || _playSignals == null || _myUnitsField == null || _foeUnitsField == null) return;

            List<CardDef> pool = null;
            try { pool = CardDatabase.Load(); }
            catch (Exception ex) { Debug.LogWarning(P + "卡池加载失败（" + ex.GetType().Name + "）"); }
            Check(pool != null && pool.Count > 0, "（前提）卡池加载得到（`CardDatabase.Load()`）");
            if (pool == null || pool.Count == 0) return;

            // ---- 原版阵营对（判据 = 宿主自己在 `BattleScene.cs:14324` 用过同一对；两者都能被 `CardDatabase` 解析）----
            const string MyFaction = "Ultramarines";
            const string FoeFaction = "Goff";
            var myDeck = DeckFor(pool, MyFaction);
            var foeDeck = DeckFor(pool, FoeFaction);
            Check(myDeck.Count > 1 && foeDeck.Count > 1,
                  "（前提）两边都凑得出一副真原版阵营的牌（" + MyFaction + " " + myDeck.Count + " 张 / "
                + FoeFaction + " " + foeDeck.Count + " 张）—— 夹具要用「卡池 + 本局阵营」能查回来的卡");
            if (myDeck.Count <= 1 || foeDeck.Count <= 1) return;

            // ---- 自建驱动（只做它真正需要的四件事，见文件头「其它」最后一条）----
            stage = new GameObject("B102TRAITStage");
            _stageRoot = stage.transform;
            var drv = stage.AddComponent<BattleDriver>();
            drv.boardRoot = stage.transform;
            drv.playerBoard = hostDriver != null && hostDriver.playerBoard != null
                              ? hostDriver.playerBoard : MakeBoard(stage.transform, "B102TRAITMyBoard", false);
            drv.enemyBoard = hostDriver != null && hostDriver.enemyBoard != null
                             ? hostDriver.enemyBoard : MakeBoard(stage.transform, "B102TRAITFoeBoard", true);
            drv.cam = hostDriver != null ? hostDriver.cam : null;
            drv.use3DBoard = false;      // 走 2D 行心那条路（`SlotPosition`；与借来的 `BoardLayout` 同一把尺）

            var ctx = RuleCore.NewBattle(myDeck, foeDeck, seed: 20260924, shuffle: false,
                                         cardPool: pool, firstSeat: 0, botSeat: 1);
            SetField(drv, "_pool", pool);
            SetField(drv, "_myFaction", MyFaction);
            SetField(drv, "_foeFaction", FoeFaction);
            SetCtx(drv, ctx);
            CardArt.Load();

            Check(drv.Ctx != null && drv.Ctx.Players != null && drv.Ctx.Players.Length > 1 && drv.Ctx.Players[1] != null,
                  "（前提）自建驱动拿到一份真的 `BattleContext`（`RuleCore.NewBattle`）：座位 0 = " + MyFaction
                + " / 座位 1 = " + FoeFaction);
            if (drv.Ctx == null) return;

            int foe = 1 - drv.MyIndex;
            if (foe < 0 || foe > 1) { Check(false, "（前提）`MyIndex` 越界（实得 " + drv.MyIndex + "）"); return; }

            // 先把视图建出来（产品的 `SyncBoard`，⛔ 不另写一份同步）
            SyncBoth(drv);

            // 本局卡池 + 敌方阵营 —— 只为判「夹具卡会不会被 `PlayDeathFeel` 解析成 `CardDef`」
            //（`BattleDriver.PlayDeathFeel` 那句 = `CardDatabase.Find(_pool, e.CardId, FactionOf(e.Player))`）。
            string foeFaction = FoeFactionOf(ctx, foe);
            Check(!string.IsNullOrEmpty(foeFaction),
                  "（前提）取得到本局敌方阵营（读敌方督军那张卡自己的 `Faction`）");

            var card = FoeUnitCard(ctx, foe, pool, foeFaction);
            Check(card != null, "（前提）夹具拿得到一张 `unit` 卡（敌方牌库 → 手牌 → 卡池按阵营）");
            if (card == null) return;

            // 🔴 夹具卡必须能在「本局卡池 + 本局敌方阵营」里查回来 —— 那正是 `PlayDeathFeel` 解析 `deadDef`
            //   的唯一判据。查不回来 ⇒ 阵亡走的是 `CardFeel.DeathExplosion(deadDef: null)` 的**即杀**路径
            //   （视图一样会没，但那是另一条路）⇒「① 视图已摘掉」就不再证明真消散那条链。
            bool deadDefResolves = Resolves(pool, card, foeFaction);
            Check(deadDefResolves,
                  "（前提）夹具卡 `" + card.Name + "` 能被 `CardDatabase.Find(卡池, 名字, \"" + foeFaction + "\")` 查回来"
                + "（查不回来 ⇒ 阵亡走 `tw == null` 的即杀路径；若本局两边阵营都是自设计阵营这一条**必红**）");

            // 三个 case **一格一个**（复用同一格会让上一轮的阵亡视图被 `SyncBoard` 按身份认领回来）
            var slots = FreeSlots(ctx, foe, 3);
            Check(slots.Count >= 3,
                  "（前提）敌方棋盘上有 3 个**互不相同**的空部署格（实得 " + slots.Count + "）");
            if (slots.Count < 3) return;

            drv.animateFeel = true;                          // `PlayFeel` 只在它开着时跑

            DeathCase(drv, ctx, foe, slots[0], card, deadDefResolves, d => Pump(d),
                      "①", huntMark: true, asRemnant: false, amount: 2, expectPlay: true,
                      why: "普通阵亡（带 2 层猎杀标记）");

            DeathCase(drv, ctx, foe, slots[1], card, deadDefResolves, d => Pump(d),
                      "②", huntMark: true, asRemnant: true, amount: 2, expectPlay: true,
                      why: "**带标记 + 翻面成残骸**（原版也播 —— 这就是 `B98_R1` §503 判错的那一格）");

            DeathCase(drv, ctx, foe, slots[2], card, deadDefResolves, d => Pump(d),
                      "③", huntMark: false, asRemnant: false, amount: 0, expectPlay: false,
                      why: "不带标记（`Amount == 0`）");
        }
        catch (Exception e)
        {
            Check(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            _stageRoot = null;
            if (stage != null) UnityEngine.Object.DestroyImmediate(stage);   // 自建的那台连整棵舞台一起收掉
            KillEffectsAfter(fxBefore);                                       // 本次跑出来的特效（含根级、不挂在舞台下的）
            Debug.Log(P + $"=== B102_R3 账 特效动画:503 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ==============================================================================================
    //  一个用例：摆一张单位 → 注入带 `Amount` 的 `Death` → 推播放泵
    //            → 数「挂在**敌方督军锚点**上、名字 = huntMark 那件 FromCode 件」的实例变化
    // ==============================================================================================

    /// <param name="amount">注入事件里的 `EvtKind.Death.Amount`（引擎发侧拍下来的猎杀标记数）。</param>
    /// <param name="asRemnant">`true` = 让这一格是「已翻面成残骸」的单位状态（钉 `PlayDeathFeel` 的残骸守卫）。</param>
    /// <param name="deadDefResolves">`PlayDeathFeel` 那句 `CardDatabase.Find` 会不会命中；
    /// `false` ⇒ 普通阵亡走 `DeathExplosion(deadDef: null)` 回 null 的**即杀**路径（只影响结构断言的措辞）。</param>
    static void DeathCase(BattleDriver driver, BattleContext ctx, int foe, int slot, CardDef card,
                          bool deadDefResolves, Action<BattleDriver> pump,
                          string tag, bool huntMark, bool asRemnant, int amount, bool expectPlay, string why)
    {
        // ---- 摆单位 ----
        var u = new UnitState(card, false);
        if (huntMark) u.AddKeyword("huntmark", 2);      // ⚠️ 必须用 `AddKeyword`（`AddTempBuff(IsKeyword:true)` 不写 `_keywords`）
        if (asRemnant) u.IsRemnant = true;              // 同一份对象翻面 ⇒ `SyncBoard` 认「同一张卡」、视图不动（真实形状）
        ctx.Players[foe].Board[slot] = u;
        SyncBoth(driver);

        string fx = null;
        Transform anchor = null;
        // 🔴 每 case 收尾：**把这一格清空并重新同步** —— 否则上一轮那张阵亡视图会被 `SyncBoard`
        //    按身份认领回来，下一条死讯就可能落在旧视图上。
        Action Cleanup = () =>
        {
            ctx.Players[foe].Board[slot] = null;
            SyncBoth(driver);
            if (fx != null && anchor != null) DestroyAll(fx, anchor);
        };

        var dead = driver.BoardViewAt(slot, false);
        var wv = driver.BoardViewAt(BoardSpec.WarlordSlot, false);

        // ---- 探针采样点 ①：摆好、还没注入死讯 ----
        bool dA0, dAct0, wA0, wAct0; string dAn0, wAn0;
        ViewState(dead, false, out dA0, out dAct0, out dAn0);
        ViewState(wv, true, out wA0, out wAct0, out wAn0);
        Check(dA0 && wA0,
              tag + "（前提）死者与**它自己那一方督军**的视图都在（死者 " + ViewText(dA0, dAct0, dAn0)
            + " · 督军 " + ViewText(wA0, wAct0, wAn0) + "）");
        if (!dA0 || !wA0) { Cleanup(); return; }

        anchor = wv.EffectAnchorBoard;                  // = 原版 `CardUI.effectsAnchor`（懒建、同一 `CardView` 上恒等）
        fx = TraitParticles.PrefabFor("huntMark");
        Check(!string.IsNullOrEmpty(fx), tag + "（前提）数据里绑了 `huntMark` 的 FromCode 件（现读 `" + fx + "`）");
        if (string.IsNullOrEmpty(fx)) { Cleanup(); return; }

        int marks = u.KwValue("huntmark");
        Check(marks == (huntMark ? 2 : 0),
              tag + "（前提）夹具真的挂上了 `huntmark " + (huntMark ? 2 : 0) + "`（实得 " + marks + "）");
        if (expectPlay)
            Check(amount > 0, tag + "（前提）注入的 `EvtKind.Death.Amount` = **死亡那一刻的猎杀标记数**（" + amount + " > 0）");

        // 清掉上一轮留下的同名实例（否则 ② 会把 ① 那一件算进来）
        DestroyAll(fx, anchor);
        int before = LiveCount(fx, anchor);
        TraitParticles.ResetProbeForTest();

        // ---- 注入死讯并推播放泵 ----
        ctx.Signals.Add(new BattleEvent
        {
            Kind = EvtKind.Death,
            Player = foe,
            Slot = slot,
            CardId = card.Name,
            Amount = amount,
        });
        _playSignals.Invoke(driver, null);               // = `RefreshAll` 第一句 `PlaySignals`（把事件排上时间线）

        // ---- 探针采样点 ②：死讯**已排上时间线、还没播** —— (a)/(b) 唯一能分辨的一刻 ----
        bool dA1, dAct1, wA1, wAct1; string dAn1, wAn1;
        ViewState(driver.BoardViewAt(slot, false), false, out dA1, out dAct1, out dAn1);
        ViewState(driver.BoardViewAt(BoardSpec.WarlordSlot, false), true, out wA1, out wAct1, out wAn1);
        int pend1 = driver.TimelinePending;

        pump(driver);                                    // 批处理没有帧循环 ⇒ 必须手动推完

        // ---- 探针采样点 ③：播完 ----
        bool dA2, dAct2, wA2, wAct2; string dAn2, wAn2;
        ViewState(driver.BoardViewAt(slot, false), false, out dA2, out dAct2, out dAn2);
        ViewState(driver.BoardViewAt(BoardSpec.WarlordSlot, false), true, out wA2, out wAct2, out wAn2);
        int pend2 = driver.TimelinePending;

        int after = LiveCount(fx, anchor);

        if (expectPlay)
        {
            bool played = after == before + 1;
            // 只在**红**的时候摊开三格探针（绿的时候不刷屏）—— 这一句就是下跑分辨 (a)/(b) 的口。
            string probe = played ? "" :
                  "｜探针〔①采样 督军 " + ViewText(wA0, wAct0, wAn0) + " / 死者 " + ViewText(dA0, dAct0, dAn0)
                + " ②注入后 督军 " + ViewText(wA1, wAct1, wAn1) + " / 死者 " + ViewText(dA1, dAct1, dAn1)
                + " ③播后 督军 " + ViewText(wA2, wAct2, wAn2) + " / 死者 " + ViewText(dA2, dAct2, dAn2)
                + " 待播事件 注入后=" + pend1 + " 播后=" + pend2 + "〕"
                + " ⇒ " + Diagnosis(wA0 && wA1 && wA2, wAct0 && wAct1 && wAct2,
                                    wAn0 == "有" && wAn1 == "有" && wAn2 == "有",
                                    dA1, pend1, pend2);
            Check(played,
                  "★★ " + tag + " 猎杀标记的 FromCode 粒子**真的播了**，且挂在**死者自己那一方督军的 `EffectAnchorBoard`** 上"
                + "（件名 `" + fx + "`；前 " + before + " / 后 " + after + "）｜" + why
                + "｜🧨 删掉 `BattleDriver.PlayDeathFeel` 里那句 `PlayHuntMarkTraitParticles(e, v);` ⇒ 红"
                + probe);
            Check(TraitParticles.LastTriggerKeyForTest == "huntmark",
                  tag + " 播放口收到的是 `huntmark`（`TraitParticles.Norm` 归一后的键；实得 `"
                + TraitParticles.LastTriggerKeyForTest + "`）");
        }
        else
        {
            Check(after == before,
                  "★★ " + tag + " **不播**（" + why + "；前 " + before + " / 后 " + after + "）"
                + "｜🧨 把闸写成 `e.Amount >= 0` ⇒ 红");
        }

        // ---- 两条「走了哪一支」的结构证据（互斥；把实现挪错位置时它们会各自露馅）----
        if (asRemnant)
        {
            Check(dA2 && dAct2,
                  tag + "（结构）走的是「翻面成残骸 ⇒ 不播阵亡消散」那道守卫（视图**还在、还激活**；播后 "
                + ViewText(dA2, dAct2, dAn2) + "）"
                + "｜🧨 把 `PlayHuntMarkTraitParticles` 挪到守卫**之后** ⇒ 上面那条 ★ 红（这一档静默不播）");
        }
        else if (expectPlay)
        {
            Check(!dA2,
                  tag + "（结构）普通阵亡真的走了阵亡那一支（视图已从格位上摘掉；播后 " + ViewText(dA2, dAct2, dAn2)
                + (deadDefResolves ? "，走的是真消散路径" : "，`deadDef` 查不回来 ⇒ 走的是 `tw == null` 的即杀路径")
                + " —— 与 ② 的残骸档**互补**）");
        }

        Cleanup();                                       // 收尾：清这一格 + 重新同步 + 收掉本轮实例
    }

    // ==============================================================================================
    //  小工具
    // ==============================================================================================

    /// <summary>自建驱动的**推进方式**：批处理没有帧循环 ⇒ 手动推（<see cref="BattleScene.StepThrough"/> 的同形，
    /// 但只认本件那台驱动：⛔ `FindObjectOfType` 会摸到宿主那台）。</summary>
    static void Pump(BattleDriver d, float maxSec = 9f)
    {
        float t = 0f;
        const float dt = 1f / 30f;
        while (d != null && d.TimelinePending > 0 && t < maxSec) { Step(d, dt); t += dt; }
        Step(d, 0.1f);        // 再留一点给刚起来的特效
    }

    static void Step(BattleDriver d, float dt)
    {
        if (d != null) d.AdvanceTimeline(dt);
        CardTween.Advance(dt);
        if (_stageRoot != null)
            foreach (var ps in _stageRoot.GetComponentsInChildren<ParticleSystem>(true))
                if (ps != null) ps.Simulate(dt, withChildren: false, restart: false, fixedTimeStep: true);
    }

    /// <summary>产品的两个私有同步口（视图同步 / 事件排期）—— 反射调，⛔ 不复制第二份实现。</summary>
    static void SyncBoth(BattleDriver d)
    {
        if (d == null || d.Ctx == null) return;
        _syncBoard.Invoke(d, new object[] { d.MyIndex, _myUnitsField.GetValue(d), true });
        _syncBoard.Invoke(d, new object[] { 1 - d.MyIndex, _foeUnitsField.GetValue(d), false });
    }

    /// <summary>自建驱动的那一副牌：**原版阵营**的督军（`type == "hero"`）+ 同阵营的 29 张。
    /// ⚠️ 这不是「随便凑」—— `RuleCore.NewBattle` 把第一张 hero 提为督军，其余进牌库；同阵营保证
    /// `CardDatabase.Find(卡池, 名字, 阵营)` 查得回来（本件所有前提都压在这上面）。</summary>
    static List<CardDef> DeckFor(List<CardDef> pool, string faction)
    {
        var deck = new List<CardDef>();
        if (pool == null) return deck;
        CardDef hero = null;
        foreach (var c in pool)
            if (c != null && c.Type == "hero" && DeckRules.SameFaction(c.Faction, faction)) { hero = c; break; }
        if (hero == null) return deck;
        deck.Add(hero);
        foreach (var c in pool)
        {
            if (deck.Count >= 30) break;
            if (c == null || c == hero || c.Type == "hero") continue;
            if (!DeckRules.SameFaction(c.Faction, faction)) continue;
            deck.Add(c);
        }
        return deck;
    }

    static BoardLayout MakeBoard(Transform parent, string name, bool mirror)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var b = go.AddComponent<BoardLayout>();
        b.mirror = mirror;
        b.EnsureMarkers();
        return b;
    }

    static void SetField(object o, string name, object val)
    {
        var f = typeof(BattleDriver).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (f == null) { Debug.LogWarning(P + "`BattleDriver." + name + "` 找不到 —— 那一格没设进去"); return; }
        f.SetValue(o, val);
    }

    /// <summary>`Ctx` 是 `{ get; private set; }` ⇒ 先走 property 的私有 setter，写不进再走 backing field（同 `B98H4_Assert`）。</summary>
    static void SetCtx(BattleDriver d, BattleContext ctx)
    {
        var p = typeof(BattleDriver).GetProperty("Ctx", AnyInst);
        try { if (p != null) p.SetValue(d, ctx); }
        catch (Exception e) { Debug.LogWarning(P + "Ctx 走 property 写失败（改走 backing field）：" + e.Message); }
        if (d.Ctx != ctx)
        {
            var f = typeof(BattleDriver).GetField("<Ctx>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null) f.SetValue(d, ctx);
        }
    }

    static List<WarpforgeEffectPlayer> SnapshotEffects()
    {
        var l = new List<WarpforgeEffectPlayer>();
        foreach (var ep in WarpforgeEffectPlayer.ActivePlayers) if (ep != null) l.Add(ep);
        return l;
    }

    /// <summary>把本次跑出来、**不在 <paramref name="before"/> 里**的特效收掉（含根级、不挂在舞台下的那些）。</summary>
    static void KillEffectsAfter(List<WarpforgeEffectPlayer> before)
    {
        if (before == null) return;
        var now = new List<WarpforgeEffectPlayer>();
        foreach (var ep in WarpforgeEffectPlayer.ActivePlayers) if (ep != null) now.Add(ep);
        foreach (var ep in now)
        {
            if (before.Contains(ep)) continue;
            try { ep.Kill(); } catch { }
            if (ep != null) UnityEngine.Object.DestroyImmediate(ep.gameObject);
        }
    }

    /// <summary>「挂在 <paramref name="parent"/> 下、件名 = <paramref name="effectName"/>」的**活着的**播放器有几个。
    /// 读的是 `WarpforgeEffectPlayer.ActivePlayers`（真实实例表）与它的 `transform.parent`（真实层级）。</summary>
    static int LiveCount(string effectName, Transform parent)
    {
        int n = 0;
        foreach (var ep in WarpforgeEffectPlayer.ActivePlayers)
            if (ep != null && ep.EffectName == effectName && ep.transform.parent == parent) n++;
        return n;
    }

    static void DestroyAll(string effectName, Transform parent)
    {
        // 先拷一份：销毁会改 `ActivePlayers` 背后的那个 `List`
        var hit = new System.Collections.Generic.List<WarpforgeEffectPlayer>();
        foreach (var ep in WarpforgeEffectPlayer.ActivePlayers)
            if (ep != null && ep.EffectName == effectName && ep.transform.parent == parent) hit.Add(ep);
        foreach (var ep in hit)
        {
            try { ep.Kill(); } catch { }                       // `Kill` 会把它从活表里摘掉
            if (ep != null) UnityEngine.Object.DestroyImmediate(ep.gameObject);
        }
    }

    /// <summary>敌方棋盘上**互不相同**的 `n` 个空部署格（三个 case 一格一个 —— 复用同一格会让上一轮的
    /// 阵亡视图被 `SyncBoard` 按身份认领回来，把「谁死了」搞混）。</summary>
    static List<int> FreeSlots(BattleContext ctx, int foe, int n)
    {
        var r = new List<int>();
        if (ctx == null || ctx.Players == null || foe < 0 || foe >= ctx.Players.Length) return r;
        for (int s = 0; s < BoardSpec.Size && r.Count < n; s++)
            if (BoardSpec.IsDeployable(s) && ctx.Players[foe].Board[s] == null) r.Add(s);
        return r;
    }

    /// <summary>本局**敌方那一侧**的阵营。`BattleDriver._foeFaction` 是私有字段，这里读同一份判据的可见来源：
    /// 敌方督军那张卡自己的 `Faction`（整副牌是从 `PoolFor(_foeFaction)` 里按阵营取出来的 ⇒ 两者恒等）。
    /// 读不到再退到牌库/手牌第一张。⛔ 不猜默认阵营。</summary>
    static string FoeFactionOf(BattleContext ctx, int foe)
    {
        if (ctx == null || ctx.Players == null || foe < 0 || foe >= ctx.Players.Length) return null;
        var ps = ctx.Players[foe];
        if (ps == null) return null;
        var w = ps.Board != null && BoardSpec.IsValid(BoardSpec.WarlordSlot) ? ps.Board[BoardSpec.WarlordSlot] : null;
        if (w != null && w.Card != null && !string.IsNullOrEmpty(w.Card.Faction)) return w.Card.Faction;
        foreach (var inst in ps.Deck)
            if (inst != null && inst.Card != null && !string.IsNullOrEmpty(inst.Card.Faction)) return inst.Card.Faction;
        foreach (var inst in ps.Hand)
            if (inst != null && inst.Card != null && !string.IsNullOrEmpty(inst.Card.Faction)) return inst.Card.Faction;
        return null;
    }

    /// <summary>`PlayDeathFeel` 解析 `deadDef` 的那一条判据（**逐字同一个 API**）。</summary>
    static bool Resolves(List<CardDef> pool, CardDef card, string faction)
    {
        if (pool == null || card == null || string.IsNullOrEmpty(faction)) return false;
        return CardDatabase.Find(pool, card.Name, faction) != null;
    }

    /// <summary>夹具用的一张 `unit` 卡：**优先**取一张能在「本局卡池 + 本局敌方阵营」里被
    /// `CardDatabase.Find` 查回来的（`PlayDeathFeel` 就是这么解析 `deadDef` 的）—— 先敌方牌库、再手牌、
    /// 最后直接按阵营从卡池里挑。都挑不到才退回「任意 unit 卡」，由调用方的**前提断言**硬红出来
    /// （⛔ 不许在这里悄悄降级成「是 unit 卡就算」）。</summary>
    static CardDef FoeUnitCard(BattleContext ctx, int foe, List<CardDef> pool, string foeFaction)
    {
        var ps = ctx != null && ctx.Players != null && foe >= 0 && foe < ctx.Players.Length ? ctx.Players[foe] : null;
        if (ps == null) return null;

        CardDef anyUnit = null;
        for (int pass = 0; pass < 2; pass++)
        {
            var list = pass == 0 ? ps.Deck : ps.Hand;
            if (list == null) continue;
            foreach (var inst in list)
            {
                if (inst == null || inst.Card == null || inst.Card.Type != "unit") continue;
                if (anyUnit == null) anyUnit = inst.Card;
                if (Resolves(pool, inst.Card, foeFaction)) return inst.Card;
            }
        }

        // 整副牌都查不回来时，直接从卡池按敌方阵营挑一张 unit（仍是同一条判据，⛔ 不是「随便一张」）
        CardDef poolUnit = null;
        if (pool != null && !string.IsNullOrEmpty(foeFaction))
            foreach (var c in pool)
                if (c != null && c.Type == "unit" && DeckRules.SameFaction(c.Faction, foeFaction)) { poolUnit = c; break; }
        if (poolUnit != null) return poolUnit;

        if (anyUnit != null) return anyUnit;
        foreach (var c in StarterCards.Of(StarterCards.TideFaction))
            if (c != null && c.Type == "unit") return c;
        return null;
    }

    /// <summary>一格**只读**探针：`AnimGate.Probe` 的 (alive, active)，外加「督军 `EffectAnchorBoard` 取不取得到」。
    /// ⚠️ 视图已销毁时**不去碰**它的属性（会抛）；`wantAnchor = false` 连锚点都不读（死者那张不需要）。</summary>
    static void ViewState(CardView v, bool wantAnchor, out bool alive, out bool active, out string anchor)
    {
        AnimGate.Probe(v, out alive, out active);
        anchor = "—";
        if (!wantAnchor || !alive) return;
        try { anchor = v.EffectAnchorBoard == null ? "null" : "有"; }
        catch (Exception e) { anchor = "抛 " + e.GetType().Name; }
    }

    static string ViewText(bool alive, bool active, string anchor)
    {
        return "alive=" + (alive ? 1 : 0) + " active=" + (active ? 1 : 0) + " anchor=" + anchor;
    }

    /// <summary>把三格探针 + 两条待播数压成一句「(a) 还是 (b)」。⛔ 不猜：三格都正常就如实写「没定住」。</summary>
    static string Diagnosis(bool wvAlive, bool wvActive, bool wvAnchor,
                            bool deadAliveAtSchedule, int pendAfterSchedule, int pendAfterPump)
    {
        if (pendAfterSchedule == 0)
            return "死讯根本没排上时间线（`PlaySignals` 之后 `TimelinePending == 0`）⇒ 不是 (a)/(b)";
        if (!wvAlive)
            return "(a) 敌方督军视图取不到（`ViewAt(敌方, 督军)` 为 null / 已销毁）";
        if (!wvActive)
            return "(a) 敌方督军视图 `activeSelf == false` ⇒ `AnimGate.CanContinue` 拦下";
        if (!wvAnchor)
            return "(a) 督军 `EffectAnchorBoard` 取不到";
        if (!deadAliveAtSchedule)
            return "(b) `PlayDeathFeel` 的 `views.TryGetValue(e.Slot)` 那一路取不到死者视图";
        if (pendAfterPump > 0)
            return "播放泵没推完（播后仍有 " + pendAfterPump + " 条待播）⇒ 死讯还没轮到播";
        return "(a)/(b) 在三个采样点上都正常 ⇒ 状态是在 `stepThrough` 内部变的，要再加一层更细的探针";
    }
}
