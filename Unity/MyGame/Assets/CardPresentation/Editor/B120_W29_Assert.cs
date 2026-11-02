// B120_W29_Assert.cs — 执行代理 `B120_W29`（第一百二十会话）的断言
//
// ============================ 它盯的是什么 ============================
//   E04 · `ReshuffleCemeteryIntoDeck`（`RuleEngine/Core/RuleCore.cs:14319`）—— 原版
//         `BattleManager._ResolveDrawCard_d__432__MoveNext.c` 里「`shouldReshuffle == 真` ⇒ 墓地回填」那一支。
//         产品默认值**仍是 false**（`GameplayVariables.cs:327`，= 原版两模式的值，⛔ 本件不改它）；
//         只有**自检夹具**把这一局的 `ctx.Vars.shouldReshuffle` 强制 true，跑通
//         「牌库抽空 → 墓地洗回 → 继续抽 → 不再触发疲劳」这一整条路（改前**从没被真跑过**）。
//   F22 · legacy FIFO 的输出混音组：原版 `BattleManager.PlaySoundAsset` 那条 FIFO 播在
//         `BattleManager.audioSource`（场景实例 `AudioSource_1595`，`OutputAudioMixerGroup {fileID:0,pathID:0}`
//         ⇒ **Master**）上；改前我们走 `WFSoundPlayer.Play(AudioClip…)`（它把源写死 `FxGroup`）。
//
// ============================ 判据（路径，⛔ 别在这里抄第二份）============================
//   · `decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c`
//       `:55-68` 牌库空 ⇒ 读 `matchData.gameplayData.shouldReshuffle`（假 ⇒ `ResolveFatigue`）
//       `:77-173` 从墓地**末尾往前**扫：`+0x223 createdInInitialDeck != 0` ⇒ 回填 / 否则**只从墓地删掉**
//       `:197-204` 回填后**回到抽取**（`DrawCardFromDeck`）；抽不到 ⇒ 整条协程结束、**没有疲劳**
//   · `decomp_full/BattleManager__AddCardInPlayToDeck.c:36-64`（回填落位随机 `GetRandomInt(0,Count+1)`；牌库空 ⇒ 不掷）
//   · `decomp_full/BattleManager__PlayNextSoundInQueue.c:20-67`（出队 · 闸 · 音量）＋ 场景实例
//     `bundle_scenes_scenes_battlearena1/MonoBehaviour/AudioSource_1595.json`
//     （无混音组 ⇒ Master · `m_Volume = 1.0` · `Priority 154` · `Spatialize false`）
//   ⚠️ 逐句对照与改前/改后 → `资料/完全复刻/报告/B120_W29_执行.md`。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//   🧨 1（E04）：A 组（`shouldReshuffle = 真`）与 B 组（`假`）是**同一个输入的两个状态** ——
//       把 `!ctx.Vars.shouldReshuffle` 那一道闸删掉（恒洗回）⇒ **B1/B2/B3/B4 全红**；
//       把它改成恒 false ⇒ **A1/A3/A4/A5 全红**。一条改动不可能让两边同时绿。
//   🧨 2（F22）：`LegacySource()` 里一旦写 `outputAudioMixerGroup = …` ⇒ F1/F2/F6 红；
//       把出队那一路改回 `WFSoundPlayer.Play(AudioClip…)` ⇒ F5（源码闸）红。
//
// ============================ 怎么接 ============================
//   宿主聚合行末尾追加（**同一物理行** ⇒ 行数零变化）：
//      B120_W29_Assert.Run(); pass += B120_W29_Assert.Pass; fail += B120_W29_Assert.Fail;
//   ⚠️ E04 组**零前提**（自己 `RuleCore.NewBattle` 建局，不依赖活 driver）；
//      F22 组会在编辑器里建一颗隐藏 `AudioSource`（`HideFlags.DontSave`，同 `WarpforgeAudio.PlayJingle` 的做法）。
//   ⚠️ 未跑 Unity（子代理红线）：本文件的编译与运行由调度台在同步点验。
using System;
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;
using CardPresentation;
using WarpforgeVFX;

/// <summary>执行代理 `B120_W29`：`B120-E04`（墓地洗回沉睡支跑通）+ `B120-F22`（legacy FIFO 走 Master）。</summary>
public static class B120_W29_Assert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    const string P = "[B120W29] ";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    static void Eq(int got, int want, string msg)
    {
        Check(got == want, msg + (got == want ? "" : $" —— 期望 [{want}]，实得 [{got}]"));
    }

    public static void Run()
    {
        _pass = _fail = 0;
        Debug.Log(P + "=== B120_W29 开始（E04 墓地洗回 · F22 legacy FIFO 混音组）===");
        try
        {
            GroupE04_TrueBranch();
            GroupE04_FalseBranch();
            GroupF22_LegacyMasterGroup();
        }
        catch (Exception e)
        {
            _fail++;
            Debug.LogError(P + "✗ 未捕获异常（本组余下的断言不会执行）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        }
    }

    // ==================================================================
    //  A · E04：`shouldReshuffle = 真` ⇒ 牌库抽空 → 墓地洗回 → 继续抽 → **不再疲劳**
    // ==================================================================
    static void GroupE04_TrueBranch()
    {
        Debug.Log(P + "--- A · E04 真档：洗回 + 继续抽 ---");
        var ctx = NewFixtureBattle();
        var ps = ctx.Players[0];
        Check(ps.Deck.Count > 0, $"[A0 前提] P0 牌库非空（实得 {ps.Deck.Count} 张）");
        Check(!ctx.Vars.shouldReshuffle,
              "[A0b 前提] 这一局的 `Vars` 是 `Classic.Clone()` ⇒ `shouldReshuffle` 初值 = **false**"
            + "（= 原版两模式的值；本组只改**这一局**的副本，⛔ 产品默认值不许动）");

        // 两张「开局就在牌库」的卡挪进墓地（原版 `+0x223 createdInInitialDeck` ⇒ **回填**）
        var k1 = TakeDeckTopToCemetery(ps);
        var k2 = TakeDeckTopToCemetery(ps);
        // 一张「战斗里造出来的」也放进墓地（原版 `+0x223 == 0` ⇒ **只从墓地删掉**、不回牌库）
        var junk = ctx.NewInstance(Unit("FixtureJunk", 1, 1, 1));
        ps.Discard.Add(junk);
        Check(k1 != null && k2 != null && ps.Discard.Count == 3,
              $"[A0c 前提] 墓地里现在 3 张（2 张开局牌 + 1 张造物；实得 {ps.Discard.Count}）");
        if (k1 == null || k2 == null)
        {
            Check(false, "[A0f] 夹具没能从牌库拿两张牌 ⇒ 本组余下断言无法执行（先修夹具）");
            return;
        }
        Check(ctx.InitialDeckInstanceIds.Contains(k1.Id) && ctx.InitialDeckInstanceIds.Contains(k2.Id),
              "[A0d 前提] 那两张带「开局那份」章（`InitialDeckInstanceIds`）—— 不带的话它们不会回填，下面全空转");
        Check(!ctx.InitialDeckInstanceIds.Contains(junk.Id), "[A0e 前提] `FixtureJunk` **不带**那个章");
        k1.QuestCounter = 5; k2.QuestCounter = 5;          // 观测点：原版回填前 `ResetToValuesInHand(card, 1)`

        ps.Deck.Clear();                                   // 牌库抽空
        ctx.Vars.shouldReshuffle = true;                   // ← 只改这一局的副本
        int hand0 = ps.Hand.Count, hp0 = ps.Warlord.Health, fat0 = ps.Fatigue;

        RuleCore.Draw(ctx, 0);

        Eq(ps.Fatigue, fat0, "★ A1 **洗回那一趟不加疲劳**（原版 `_ResolveDrawCard…:197-204`：回填后回到抽取；"
                           + "疲劳只在 `shouldReshuffle == 假` 那一支）｜🧨 恒 false ⇒ 本行红");
        Eq(ps.Warlord.Health, hp0, "★ A2 督军不掉血（同上）");
        Eq(ps.Hand.Count, hand0 + 1, "★ A3 洗回之后**继续抽**：手牌 +1（原版 `:197` 回到 `DrawCardFromDeck`）");
        Check(ps.Hand.Contains(k1) || ps.Hand.Contains(k2), "★ A4 抽到的是洗回牌库的那两张之一");
        Eq(ps.Deck.Count, 1, "★ A5 另一张留在牌库（2 张回填 − 抽走 1 张）");
        Check(!ps.Hand.Contains(junk) && !ps.Deck.Contains(junk),
              "★ A6 非「开局那份」的 `FixtureJunk` **只从墓地抹掉、不回牌库**（原版 `:169-173`）");
        Eq(ps.Discard.Count, 0, "★ A7 墓地清空（两张回填 + 一张抹掉）");
        Eq(k1.QuestCounter, 0, "★ A8 回填前按原版 `ResetToValuesInHand(card, 1)` 清账（`questCounter` 归零）");
        Eq(k2.QuestCounter, 0, "★ A8b 同上（另一张）");

        // 继续抽：牌库里剩下那 1 张也抽掉 ⇒ 牌库空 + 墓地空 ⇒ 原版「回填也回填不出东西」⇒ 不抽、也不疲劳
        RuleCore.Draw(ctx, 0);
        Eq(ps.Hand.Count, hand0 + 2, "★ A9 第 2 次抽牌照常（仍不疲劳）");
        Eq(ps.Fatigue, fat0, "★ A10 第 2 次也不疲劳");
        int hand2 = ps.Hand.Count;
        RuleCore.Draw(ctx, 0);
        Eq(ps.Hand.Count, hand2, "★ A11 牌库空 + 墓地空 ⇒ **不抽**（原版 `.c:201-204`：`DrawCardFromDeck` 拿不到就整条结束）");
        Eq(ps.Fatigue, fat0, "★ A12 而且**不走疲劳**（判据 = `RuleCore.cs:3241` 的空库守卫）");
        Eq(ps.Warlord.Health, hp0, "★ A12b 督军仍然满血");
    }

    // ==================================================================
    //  B · E04：`shouldReshuffle = 假`（产品默认值）⇒ 空库只走疲劳
    // ==================================================================
    static void GroupE04_FalseBranch()
    {
        Debug.Log(P + "--- B · E04 假档（产品默认值）：空库只走疲劳 ---");
        var ctx = NewFixtureBattle();
        var ps = ctx.Players[0];
        Check(!ctx.Vars.shouldReshuffle, "[B0 前提] 本局 `shouldReshuffle` = false（原版两模式的值）");
        var c1 = TakeDeckTopToCemetery(ps);
        Check(c1 != null && ctx.InitialDeckInstanceIds.Contains(c1.Id),
              "[B0b 前提] 墓地里那张带「开局那份」章 —— 连它都**不许**洗回（闸看的是模式，不是卡）");
        if (c1 == null) { Check(false, "[B0c] 夹具没能从牌库拿一张牌 ⇒ 本组余下断言无法执行（先修夹具）"); return; }
        ps.Deck.Clear();
        int hp0 = ps.Warlord.Health;

        RuleCore.Draw(ctx, 0);

        Eq(ps.Fatigue, 1, "★ B1 假档：**走疲劳 +1**（原版 `_ResolveDrawCard…:58-68` 的 `ResolveFatigue` 支）");
        Eq(ps.Warlord.Health, hp0 - 1, "★ B2 督军挨 1 点（疲劳 = 当前疲劳值）");
        Eq(ps.Deck.Count, 0, "★ B3 牌库**仍然空**（一张都没洗回）");
        Eq(ps.Discard.Count, 1, "★ B4 墓地**一张没动**");
        Check(!ps.Hand.Contains(c1), "★ B5 那张「开局那份」**没有**被抽到手（它还在墓地）");
        Check(GameplayVariables.Classic.shouldReshuffle == false && GameplayVariables.Skirmish.shouldReshuffle == false,
              "★ C **灭自证**：两个模式的**产品默认值**仍是 false —— 上面 A 组证明「闸能开」、本组证明「默认关」；"
            + "把闸改成恒洗回 ⇒ B1/B3/B4 红；把闸改成恒关 ⇒ A1/A3/A4 红");
    }

    // ==================================================================
    //  F · F22：legacy FIFO 的输出源 = **Master**（不是 FX）
    // ==================================================================
    static void GroupF22_LegacyMasterGroup()
    {
        Debug.Log(P + "--- F · F22 legacy FIFO 混音组 ---");
        float fxWas = WarpforgeAudio.SoundFx;
        AudioClip clip = null;
        try
        {
            WarpforgeAudio.Ensure();                       // 解析 FX 组（`RouteEffectSounds`）
            var fxGroup = WFSoundPlayer.FxGroup;
            Check(fxGroup != null,
                  "[F0 前提] `Resources/Audio/FX.mixer` 里的 `General` 组解析出来了"
                + "（否则下面「不是 FX 组」那条没有判别力；见 `WarpforgeAudio.RouteEffectSounds`）");

            var src = BattleSfx.EnsureLegacySourceForTest();
            Check(src != null, "[F0b 前提] legacy FIFO 的 `AudioSource` 建出来了");
            Check(src != null && src.outputAudioMixerGroup == null,
                  "★ F1 出队那条路的源**不挂混音组 ⇒ Master**（原版 `AudioSource_1595` 的 "
                + "`OutputAudioMixerGroup {fileID:0, pathID:0}`）｜🧨 `LegacySource()` 里写一句 `= FxGroup` ⇒ 红");
            Check(src != null && fxGroup != null && src.outputAudioMixerGroup != fxGroup,
                  "★ F2 **灭自证**：它不是 `WFSoundPlayer.FxGroup` —— 改回 `WFSoundPlayer.Play(AudioClip…)`"
                + "（那条把源写死 `FxGroup`）⇒ F1/F2 一起红");
            Check(src != null && src.spatialBlend == 0f && Mathf.Approximately(src.volume, 1f)
                  && src.priority == 154 && !src.playOnAwake,
                  "★ F3 源的另外几格照原版实例：`Spatialize false`（spatialBlend=0）· `m_Volume = 1.0` · "
                + "`Priority 154` · `playOnAwake = false`");

            // 音量实参 = 音效滑块 × 入队音量（原版 `.c:55-60`）
            clip = AudioClip.Create("B120W29Probe", 256, 1, 44100, false);
            Check(clip != null, "[F3b 前提] 探测 clip 建出来了");
            WarpforgeAudio.SetSoundFx(0.5f);
            bool played = BattleSfx.PlayLegacyOnMasterForTest(clip, 0.8f, 1f);
            Check(played, "[F3c 前提] legacy 那条播放口真的播了一次（`SoundFx = 0.5 > 0`）");
            Check(Mathf.Abs(BattleSfx.LastLegacyPlayVolume - 0.4f) < 1e-4f,
                  $"★ F4 音量实参 = `SoundFx 0.5 × 入队音量 0.8` = 0.4（实得 {BattleSfx.LastLegacyPlayVolume:F4}）"
                + "｜判据 `_PlayNextSoundInQueue.c:55-60`；🧨 改成 `AudioListener.volume` 或 mixer 组音量 ⇒ 红");

            WarpforgeAudio.SetSoundFx(0f);
            bool playedAtZero = BattleSfx.PlayLegacyOnMasterForTest(clip, 0.8f, 1f);
            Check(!playedAtZero && BattleSfx.LastLegacyPlayVolume == 0f,
                  "★ F5 `SoundFx = 0` ⇒ **整条不播**（原版 `.c:44-46`；⛔ 不是以 0 音量播）");
        }
        finally
        {
            WarpforgeAudio.SetSoundFx(fxWas);
            if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
        }

        // ---- F6/F7 源码闸（结构性灭自证：出队那一路**不再**走 `WFSoundPlayer.Play(AudioClip…)`）----
        string path = Application.dataPath + "/CardPresentation/Battle/BattleSfx.cs";
        if (!System.IO.File.Exists(path))
        {
            Check(false, "[F6 前提] 读得到 `Battle/BattleSfx.cs`（" + path + "）—— 读不到时 F6/F7 等于没验 ⇒ 当场红");
            return;
        }
        bool hasLegacyCall = false, hasOldCall = false, hasGroupAssign = false;
        foreach (var raw in System.IO.File.ReadAllLines(path))
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//")) continue;                     // 注释里提到这些名字**不算**（本文件多处是判据说明）
            if (t.IndexOf("PlayLegacyOnMaster(p.clip", StringComparison.Ordinal) >= 0) hasLegacyCall = true;
            if (t.IndexOf("WFSoundPlayer.Play(p.clip", StringComparison.Ordinal) >= 0) hasOldCall = true;
            if (t.IndexOf("outputAudioMixerGroup", StringComparison.Ordinal) >= 0) hasGroupAssign = true;
        }
        Check(hasLegacyCall && !hasOldCall,
              "★ F6 出队那一跳走类尾 `PlayLegacyOnMaster(p.clip, …)`、**不再**调 `WFSoundPlayer.Play(p.clip, …)`"
            + $"（实得 新调用={hasLegacyCall} · 旧调用={hasOldCall}）");
        Check(!hasGroupAssign,
              "★ F7 `BattleSfx.cs` 的**非注释代码**里没有任何 `outputAudioMixerGroup` 赋值 —— "
            + "原版那颗源 `{fileID:0, pathID:0}` ⇒ Master｜🧨 给 `LegacySource()` 补一句挂组 ⇒ 红");
    }

    // ==================================================================
    //  夹具（⛔ 只为本件服务；与 `RuleEngineTest.Deck/Unit` 同形，但那边是 private）
    // ==================================================================
    static BattleContext NewFixtureBattle()
    {
        return RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 0, shuffle: false);
    }

    /// <summary>1 张督军 + 20 张垫牌（`shuffle:false` ⇒ 抽牌 = 从列表末尾取）。</summary>
    static List<CardDef> FixtureDeck()
    {
        var d = new List<CardDef> { Hero("FixtureWarlord", 2, 30) };
        for (int i = 0; i < 20; i++) d.Add(Unit("filler" + i, 1, 0, 1));
        return d;
    }

    /// <summary>把牌库顶那一张（我们的约定 = **列表末尾**）挪进墓地，返回它。</summary>
    static CardInstance TakeDeckTopToCemetery(PlayerState ps)
    {
        if (ps == null || ps.Deck.Count == 0) return null;
        var inst = ps.Deck[ps.Deck.Count - 1];
        ps.Deck.RemoveAt(ps.Deck.Count - 1);
        ps.Discard.Add(inst);
        return inst;
    }

    static CardDef Hero(string name, int atk, int hp)
    {
        return new CardDef(name, name, "hero", "", null, "Test", 0, atk, hp, 0, null);
    }

    static CardDef Unit(string name, int cost, int atk, int hp)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, 0, null);
    }
}
