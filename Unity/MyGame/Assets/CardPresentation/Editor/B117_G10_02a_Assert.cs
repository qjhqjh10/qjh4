// B117_G10_02a_Assert.cs — 执行代理 `B117-G10-02a`（2026-11-02）的断言：
//   **场地环境效果卡 · 数据层与应用时机**（差异表 → `判据/B117-G10-02a_场地环境效果卡.md`）
//
// ============================ 它盯的是什么 ============================
// ① **「13 场 × 4 环境」的映射表完整且互不串** —— 13 条 default + 39 张进攻卡引用的 effect，四组关系互相咬：
//      · 每个阵营的 `defaultEnvSO` 命中且 `kind == "default"`；
//      · 每张卡的 `envSO` 命中且 `kind == "effect"`；
//      · 39 个卡的 SO **两两不同**、13 个 default SO **两两不同**、两个集合**不相交**。
//    判据：原版 `EnviromentalEffectCardsSO.GetEnviromentalEffect`（逐阵营按 armyId 取表 + 空卡取
//    `DefaultEnviromentalEffectVFX`，`decomp_full/EnviromentalEffectCardsSO__GetEnviromentalEffect.c` 现读）。
// ② **数据是「从原版 SO 直读」的形状** —— `default` 不带 `scenarioObjects`（prefab 为空）、卡引用到的 `effect`
//    都带；全部 `blendTime > 0`、`fogDensity >= 0`；13 条 default 的 `defaultScenarioObjectsState` 全是 1。
//    （本次已按 55/55 逐字段与导出工程的 `.asset` 对过：0 处不一致 — 见判据文件 §三。）
// ③ **本次三条判据的语义**（实现只在 `EnvironmentApplier` 类尾那四个成员里，本件只读它们）：
//      · `OutQuad` = DOTween 默认缓动（**不是**线性）；
//      · `FogOnForDensity` = `0 < fogDensity`（**不是**恒开）；
//      · `BlendTimeFor(instant:true, x) == 0`（**不是**恒 `blendTime`）；
//      · `EnvAnchorPosition()` 落在 `Board Center`（**不是**相机位置）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · 映射表那三条是**互相咬的**，不读我们自己的常量、只读数据本身：
//      · 把某张卡的 `envSO` 指到一条 default ⇒ ③「卡命中 effect」红；
//      · 把两张卡指到同一条 SO ⇒ 「39 个两两不同」红；
//      · 把某条 SO 从 `EnvironmentConditions.json` 里删掉 ⇒ 查不到 ⇒ 全部红；
//      · 把某条 default 的 `prefabName` 填上 ⇒ 「default 不带 prefab」红。
//  · `OutQuad(0.5)==0.75` / `OutQuad(0.25)==0.4375` 用的是 **DOTween 的公式**（`2x − x²`），不是我们的形状：
//    把实现改回 `Mathf.Clamp01(x)`（线性 = 改前那一版）⇒ 立刻红。
//  · 「雾判据」**两条一起**：`FogOnForDensity(0) == false` **且**「数据里两类密度都存在」。
//    只改判据成 `return true` ⇒ 第一条红；把数据改成全 0（那样这条判据等于没查）⇒ 第二条红。
//  · `EnvAnchorPosition().z == BattleDriver.BoardCenterZ` **且** `BoardCenterZ` 仍是 −2.8108（原版 `Board Center`
//    的 localPos z）⇒ 把落点改回 `Camera.main.position`、或改那颗节点的 z ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接进任一宿主的聚合行（同一物理行、`//` 之前）：
//      B117G1002a_Assert.Run(); _sink.Pass += B117G1002a_Assert.Pass; _sink.Fail += B117G1002a_Assert.Fail;
//  ⚠️ **本件不驱动 `EnvironmentApplier.Apply`** —— 同一个理由见 `B110_R17_Assert` 头注释（会改 `RenderSettings`，
//     并给真实场景对象挂 `ScenarioBlendable` 组件，污染同一宿主里后面的断言）。
//     ⇒ 已知边界：`Apply` 里「**先算 `BlendDuration` 再建实例**」那一条（本次修的次序缺陷）本件**抓不到**
//     —— 它的可观测值那一半由 `BlendTimeFor` 钉住，次序那一半只能靠代码审查 / 真机看补间是否真的在动。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>`B117-G10-02a` 的断言集（**只读生产判据**，不驱动 `Apply`）。</summary>
public static class B117G1002a_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B117G1002a] ");

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    /// <summary>原版 `Board Center` 节点的 localPos z（判据 → `BattleDriver.EnsureBoardCenterAnchor` 头上那一段：
    /// `MonoBehaviour_4569.json` 的 `battleCenterAnchor` PPtr 1490 ⇒ `GameObject/Board Center.json`，localPos `(0,0,-2.810800)`）。
    /// ⚠️ 这是一个**原版值**，⛔ 不是我们挑的 ⇒ 它变了就是判据变了。</summary>
    const float OriginalBoardCenterZ = -2.8108f;

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B117-G10-02a：场地环境效果卡（数据层 13×4 映射 + 应用时机判据）");

        // ---------------- 前提（硬检查：⛔ 不许「前提不成立就静默跳过」）----------------
        var conds = EnvironmentConditions.All;
        var armies = OffensiveCards.All;
        MenuCheck.True(_sink, conds != null && conds.Length > 0,
                       "前提：`Resources/EnvironmentConditions.json` 读到了（=0 ⇒ 环境切换根本做不了）");
        MenuCheck.True(_sink, armies != null && armies.Length >= 13,
                       $"前提：`Resources/OffensiveCards.json` 读到了且至少 13 个阵营（实得 {(armies == null ? 0 : armies.Length)}）");
        if (conds == null || conds.Length == 0 || armies == null || armies.Length == 0) return;

        var bySo = new Dictionary<string, EnvironmentConditions.Item>();
        foreach (var c in conds)
            if (c != null && !string.IsNullOrEmpty(c.so) && !bySo.ContainsKey(c.so)) bySo[c.so] = c;

        // ---------------- ① 13 × 4 映射表：四组关系互相咬 ----------------
        var badDefault = new List<string>();
        var badCard = new List<string>();
        var badSlot = new List<string>();
        var cardSos = new List<string>();
        var defaultSos = new List<string>();
        foreach (var a in armies)
        {
            if (a == null || string.IsNullOrEmpty(a.army)) { badDefault.Add("<空阵营行>"); continue; }
            EnvironmentConditions.Item d;
            if (string.IsNullOrEmpty(a.defaultEnvSO) || !bySo.TryGetValue(a.defaultEnvSO, out d))
                badDefault.Add(a.army + ":查不到「" + a.defaultEnvSO + "」");
            else if (d.kind != "default")
                badDefault.Add(a.army + ":`" + a.defaultEnvSO + "` 的 kind = `" + d.kind + "`（应为 default）");
            else defaultSos.Add(a.defaultEnvSO);

            if (a.cards == null || a.cards.Length != 3) { badSlot.Add(a.army + ":" + (a.cards == null ? "null" : a.cards.Length.ToString())); continue; }
            var seenIdx = new HashSet<int>();
            foreach (var c in a.cards)
            {
                if (c == null) { badCard.Add(a.army + ":<空卡行>"); continue; }
                if (c.idx < 0 || c.idx > 2 || !seenIdx.Add(c.idx)) badSlot.Add(a.army + ":槽号 " + c.idx);
                EnvironmentConditions.Item e;
                if (string.IsNullOrEmpty(c.envSO) || !bySo.TryGetValue(c.envSO, out e))
                    badCard.Add(a.army + "/槽" + c.idx + ":查不到「" + c.envSO + "」");
                else if (e.kind != "effect")
                    badCard.Add(a.army + "/槽" + c.idx + ":`" + c.envSO + "` 的 kind = `" + e.kind + "`（应为 effect）");
                else cardSos.Add(c.envSO);
            }
        }
        MenuCheck.True(_sink, badDefault.Count == 0,
                       "① 每个阵营的 `defaultEnvSO` 都命中一条 **kind=default** 的 SO"
                       + (badDefault.Count > 0 ? " —— 坏行：" + string.Join("、", badDefault.ToArray()) : "（" + defaultSos.Count + " 条）"));
        MenuCheck.True(_sink, badSlot.Count == 0,
                       "② 每个阵营**恰好 3 个**进攻卡槽、槽号 0/1/2 互不重复（原版 `offensiveCards` 三档）"
                       + (badSlot.Count > 0 ? " —— 坏行：" + string.Join("、", badSlot.ToArray()) : ""));
        MenuCheck.True(_sink, badCard.Count == 0,
                       "③ 每张进攻卡的 `envSO` 都命中一条 **kind=effect** 的 SO"
                       + (badCard.Count > 0 ? " —— 坏行：" + string.Join("、", badCard.ToArray()) : ""));
        if (badSlot.Count > 0 || badCard.Count > 0) return;

        var cardSet = new HashSet<string>(cardSos);
        var dftSet = new HashSet<string>(defaultSos);
        MenuCheck.Check(_sink, cardSet.Count, cardSos.Count,
                        "④ 39 张进攻卡的 SO **两两不同**（`13 场 × 3 张` 真的是 39 个格子 —— 若有人把几张卡指到同一条 SO 就会小）");
        MenuCheck.Check(_sink, dftSet.Count, defaultSos.Count,
                        "⑤ 13 条 default SO **两两不同**（每个阵营一套默认环境，不共用）");
        int overlap = 0;
        foreach (var s in cardSet) if (dftSet.Contains(s)) overlap++;
        MenuCheck.Check(_sink, overlap, 0,
                        "⑥ 卡引用的 SO 集合与 default 集合**不相交**（一张卡的「换环境」不能指向某阵营的默认环境）");

        // ---------------- ② 数据形状（= 从原版 SO 直读，⛔ 手填会在这里露）----------------
        var dftWithPrefab = new List<string>();
        foreach (var c in conds)
            if (c != null && c.kind == "default" && EnvironmentConditions.HasPrefab(c)) dftWithPrefab.Add(c.so);
        MenuCheck.True(_sink, dftWithPrefab.Count == 0,
                       "⑦ 13 条 default **都不带 `scenarioObjects`（prefab 为空）** —— 原版 `EnableEnvironment` 那一支走 `RuntimeKeyIsValid=false`"
                       + (dftWithPrefab.Count > 0 ? "；坏了：" + string.Join("、", dftWithPrefab.ToArray()) : ""));

        var refNoPrefab = new List<string>();
        foreach (var s in cardSet)
        {
            EnvironmentConditions.Item e;
            if (bySo.TryGetValue(s, out e) && !EnvironmentConditions.HasPrefab(e)) refNoPrefab.Add(s);
        }
        MenuCheck.True(_sink, refNoPrefab.Count == 0,
                       "⑧ 卡引用到的 39 条 effect **都带环境 prefab**（空着 ⇒ 那张卡只补间雾/环境光、不加物件 = 少一大半表现）"
                       + (refNoPrefab.Count > 0 ? "；坏了：" + string.Join("、", refNoPrefab.ToArray()) : ""));

        var badBlend = new List<string>();
        var badDen = new List<string>();
        var badState = new List<string>();
        int denZero = 0, denPos = 0;
        foreach (var c in conds)
        {
            if (c == null) continue;
            if (c.blendTime <= 0f) badBlend.Add(c.so + "=" + c.blendTime.ToString("0.###"));
            if (c.fogDensity < 0f) badDen.Add(c.so + "=" + c.fogDensity.ToString("0.####"));
            if (c.fogDensity == 0f) denZero++; else denPos++;
            if (c.kind == "default" && c.defaultScenarioObjectsState != 1) badState.Add(c.so + "=" + c.defaultScenarioObjectsState);
        }
        MenuCheck.True(_sink, badBlend.Count == 0,
                       "⑨ 全部 55 条 `blendTime > 0`（0 会让补间变成瞬跳；原版 ctor 默认 2.0）"
                       + (badBlend.Count > 0 ? "；坏了：" + string.Join("、", badBlend.ToArray()) : ""));
        MenuCheck.True(_sink, badDen.Count == 0, "⑩ 没有负的 `fogDensity`");
        MenuCheck.True(_sink, badState.Count == 0,
                       "⑪ 13 条 default 的 `defaultScenarioObjectsState` **全是 1**（= 「撤环境 ⇒ 战场自己的 FX 全开」那一路的方向）"
                       + (badState.Count > 0 ? "；坏了：" + string.Join("、", badState.ToArray()) : ""));

        // ---------------- ③ 判据语义（四件，都只读生产实现）----------------
        MenuCheck.Near(_sink, EnvironmentApplier.OutQuad(0.5f), 0.75f, 1e-4f,
                       "⑫ `OutQuad(0.5)` = **0.75**（DOTween `Ease.OutQuad` 的 `2x−x²`；改回线性就是 0.5 ⇒ 红）");
        MenuCheck.Near(_sink, EnvironmentApplier.OutQuad(0.25f), 0.4375f, 1e-4f,
                       "⑬ `OutQuad(0.25)` = **0.4375**（同上；线性是 0.25）");
        MenuCheck.True(_sink, EnvironmentApplier.OutQuad(0f) == 0f && EnvironmentApplier.OutQuad(1f) == 1f
                              && EnvironmentApplier.OutQuad(-3f) == 0f && EnvironmentApplier.OutQuad(9f) == 1f,
                       "⑭ `OutQuad` 的两端与夹取：`f(0)=0 · f(1)=1 · f(<0)=0 · f(>1)=1`");

        MenuCheck.True(_sink, EnvironmentApplier.BlendTimeFor(true, 2f) == 0f
                              && EnvironmentApplier.BlendTimeFor(false, 2f) == 2f
                              && EnvironmentApplier.BlendTimeFor(false, 0f) == 0f
                              && EnvironmentApplier.BlendTimeFor(false, -1f) == 0f,
                       "⑮ blendable 时长 = `instant ? 0 : blendTime`（原版 `EnableEnvironment` 的 `real1`/`real2`；"
                       + "「instant 也按 blendTime 补间」或「恒 blendTime」都会红）");

        MenuCheck.True(_sink, !EnvironmentApplier.FogOnForDensity(0f)
                              && EnvironmentApplier.FogOnForDensity(0.005f),
                       "⑯ 开雾判据 = `0 < fogDensity`（原版 `_ApplyFog_b__20_4` / instant 支同一句）");
        MenuCheck.True(_sink, denZero > 0 && denPos > 0,
                       $"⑰ 数据里**两类密度都真的存在**（0 的 {denZero} 条 / >0 的 {denPos} 条）—— "
                       + "否则上一条等于没查（改前写死 `fog = true` 的错只对「密度 0」那类才现形）");

        Vector3 anchor = EnvironmentApplier.EnvAnchorPosition();
        MenuCheck.Near(_sink, anchor.z, BattleDriver.BoardCenterZ, 1e-4f,
                       "⑱ 环境 prefab 的落点 z = `BattleDriver.BoardCenterZ`（原版 `battleCenterAnchor.position`；"
                       + "改回 `Camera.main.position` 就红）");
        MenuCheck.Near(_sink, BattleDriver.BoardCenterZ, OriginalBoardCenterZ, 1e-3f,
                       "⑲ `BoardCenterZ` 仍是原版 `Board Center` 的 localPos z = **−2.8108**（⛔ 这是我们抄的原版值，不是挑的）");

        MenuCheck.True(_sink, EnvironmentConditions.Find(null) == null && EnvironmentConditions.Find("") == null,
                       "⑳ `EnvironmentConditions.Find` 对空名返回 null（调用方**要出声**，⛔ 别静默返回一条默认环境）");
        MenuCheck.True(_sink, EnvironmentConditions.Find(OffensiveCards.All[0].defaultEnvSO) != null,
                       "㉑ `Find` 的往返：拿第一条 default 名再查一次取得到（查表没坏）");
    }
}
