# W · 批次 C —— `RuleEngineTest` 3 红 ＋ `BattleScene` 1 红（写手回执）

> 2026-10-19。**只改了两个宿主文件**：`Assets/RuleEngine/Editor/RuleEngineTest.cs` · `Assets/CardPresentation/Editor/BattleScene.cs`。
> ⛔ 没跑 Unity / `*.Run` / player；没动 git 写；没改正本、数据、`Core/`。
> 验收：**typecheck 0/0**（`TMPDIR=/tmp/wf_redC bash d:/4/Unity/工具/typecheck.sh` → `运行时错误数: 0` · `编辑器错误数: 0`；两份 rsp 都确认包含这两个文件）。
> 简报里的路径 `MyGame/Assets/CardPresentation/Editor/RuleEngineTest.cs` **不存在** —— 实际文件在 **`MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`**（已按实际路径改）。

## §一 逐条表

| # | 红（宿主:行 → 现行） | 性质 | 改了什么（文件:行） | 依据 | typecheck |
| --- | --- | --- | --- | --- | --- |
| 1 | `RuleEngineTest.cs:8630`「部署触发段真的跑过」（日志 `:86511`） | δ 夹具前提不成立 | `RuleEngineTest.cs:8588-8659` —— 4 局循环里，**每个回合开始（`BeginTurn` 之后、`PlayAiTurn` 之前）由夹具自己**：① 手里还有 `Experimental Drones` 且 `CanPlayTactic == OK` ⇒ `RuleCore.PlayTactic` 打出（走真路径 ⇒ `DoPersist` 登记）；② `PersistentEffects` 里有本方 `deploy` 效果且还没出现过 `部署触发段：` ⇒ 扫**整副手牌**找第一张 `CanPlayCard == OK` 的 Drone（不再只看第一张）、`RuleCore.PlayCard` 部署。成功/失败都 `ctx.Log`（带 `RuleCodes.Describe` 返回码、能量、回合、格位）。**断言一个字没改**。 | `DIAG_red_C_RuleEngine_Battle2.md` §一·1)；机制判据 = 同测试 ① 段 `:8493-8499` + 卡面 `For the rest of this battle, give Shield to all Drones you deploy` | 0/0 |
| 2 | `RuleEngineTest.cs:8631`「真的筛中了 Drone」（日志 `:86512`） | δ（与 #1 同一颗：`fired`/`granted` 数同一行日志） | 同 #1（修 #1 即修 #2）。两行断言都保留原样。 | 同上；`fired` 数 `部署触发段：`、`granted` 数 `盯上`，两个子串同在 `EffectResolver.cs:5785` 那一行 ⇒ 恒等 | 0/0 |
| 3 | `RuleEngineTest.cs:20279`「效果文本里没有 `Cadian Honour`」 | α 断言过期（数据是对的） | `RuleEngineTest.cs:20324-20337` —— 前提换成「**替身点名那一支产不出 `Cadian Honour`**」：`CreatePool.MentionedCards(pool, minka, minka.Desc, out why0, 8)` 里不含 id `3a52026b99bb56f40aa9dc130fc66f34`；并把 `why0 != null`（索引没建、查都没查成）也算红——不让它空过。下一句「真值优先」断言原样保留。**⛔ 数据一个字没改**。 | PnP 成品卡 `d:/2/Warpforge部队卡片/Astra Militarum/3部队/Warpforge_16_Minka-Lesk.png` 逐字含 `Talent: Cadian Honour`；`CreatePool.StripTalentSegments`（`. `/`。`/换行分段，`Talent:` / `天赋：` 起整截挖掉）；`DIAG §一·3)` | 0/0 |
| 4 | `BattleScene.cs:12705`「绿光材质 … 队列 3000」（日志 `:143626`） | γ 回归（实现有意，断言同步） | `BattleScene.cs:12702-12713` —— `renderQueue == 3000` → **`== probe.Queue`**（`probe` 是同一张 `CardView`，public `Queue`）；文案里保留「原 prefab `m_CustomRenderQueue` 是 3000」这一事实 + 实得值。**⛔ 没有回退 `A1674`**。 | `A1674`：`CardView.SetQueue` 扫 `ParticleSystemRenderer`、`StampQueue` 写这张卡那一段；场上卡段 = `BoardLayout.QueueOf` 2962..2979；`CardFaceProbe` 已有 2971 用例；`DIAG §一·4)` | 0/0 |

## §二 关键 diff

### ① 部署触发段夹具（`RuleEngineTest.cs:8588-8659`，`TestDeployBuffAndCostMore` ③ 段）

```csharp
RuleCore.BeginTurn(ctx);
{
    var ps0 = ctx.Players[ctx.Active];
    // ① 登记：夹具自己打（不再等 SimpleAI 在 PlayAiTurn 内部打）
    for (int i = 0; i < ps0.Hand.Count; i++)
    {
        if (ps0.Hand[i] == null || ps0.Hand[i].Card.Name != "Experimental Drones") continue;
        int canTac = RuleCore.CanPlayTactic(ctx, ctx.Active, i, -1);
        if (canTac == RuleCodes.OK)
        {
            int rTac = RuleCore.PlayTactic(ctx, ctx.Active, i, -1);
            ctx.Log($"—— 夹具：打出 `Experimental Drones` ⇒ {RuleCodes.Describe(rTac)}…");
        }
        else ctx.Log($"—— 夹具：`Experimental Drones` 这一回合打不出（{RuleCodes.Describe(canTac)}…）——");
        break;
    }
    // ② 消费：登记之后（同一回合优先）扫整副手牌找一张付得起、有格位的 Drone
    //    ……（原 `hasDeployEffect` / `firedYet` 判据保留；`FirstFreeSlot` + `CanPlayCard` 逐个试）
}
PlayAiTurn(ctx, ref tac);
```

行为要点（为什么这样能绿）：
- 当前能量曲线 = `TurnCount + 1`（经典），4 费战术卡在**自己第 3 个回合（全局 T5、4 能）**首次打得动 ⇒ 登记**不再依赖 AI 何时想起它**。
- 打完 4 费后通常**当回合付不起** Drone（能量归零）—— 这时只 `ctx.Log` 一条原因，**不判失败**；下一个自己回合（T7，5 能）的 ② 会用 1–3 费的 Drone 真正部署 ⇒ `ResolveDeploy` 记 `部署触发段：`/`盯上`。
- ② 改成扫**整副手牌**（原实现「第一张 Drone 付不起 ⇒ 整轮放弃」）。
- 每局仍只在**还没触发过**时补消费（`部署触发段：` = `ResolveDeploy` 出口）。

### ② AM15 前提（`RuleEngineTest.cs:20324-20337`）

```csharp
string why0;
var alt0 = CreatePool.MentionedCards(pool, minka, minka.Desc, out why0, 8);
CheckTrue(why0 == null && !alt0.Exists(c => c.Id == "3a52026b99bb56f40aa9dc130fc66f34"),
          "（前提）点名那一支产不出 `Cadian Honour`（它只出现在 `Talent:` 段里，`StripTalentSegments` 会剥掉）"
          + (why0 != null ? $"｜🧨 而且查都没查成：{why0}" : "")
          + $"｜desc=`{minka.Desc}`｜点名那一支实得 {alt0.Count} 张");
```

### ③ 绿光队列（`BattleScene.cs:12702-12713`）

```csharp
Check(prA != null && prA.sharedMaterial != null
      && prA.sharedMaterial.shader != null
      && prA.sharedMaterial.shader.name == "Universal Render Pipeline/Particles/Unlit"
      && prA.sharedMaterial.renderQueue == probe.Queue,
      "★ 绿光材质 = 原版 `Circle_Hoop Additive`（shader = URP `Particles/Unlit`）"
    + $"—— 队列跟着这张卡那一段走（现读 {probe.Queue}；原 prefab 的 `m_CustomRenderQueue` 是 3000，A1674 起按「一张卡一段队列」盖章 → `CardView.SetQueue`）；实得 {…}");
```

## §三 需要主对话复跑的宿主

- **`RuleEngineTest.Run`** —— 覆盖 #1/#2/#3（`TestDeployBuffAndCostMore` ③ 段 + `TestRelatedCardsTruth`）。
- **`BattleScene.Run`** —— 覆盖 #4（绿光材质那条）。
- 类型检查已过（0/0），但**行为没验**（按红线段规矩不跑 `*.Run`）。

## §四 没查清 / 顺手发现

1. **`RuleEngineTest.cs` 里本次改动之外还有一大批未提交改动**（`W_red_A` 的 128/67 计数、AM24 断言、中文档 58/33 等）。`git diff --stat` 显示该文件 168 行增改，**绝大部分不是我写的**；我只动了 `:8588-8659` 与 `:20324-20337` 两处，没有覆盖那些。
2. **#1/#2 还剩下的唯一依赖**：登记发生在自己第 3 个回合（全局 T5）后，消费要靠**自己第 4 个回合（T7）真的开始**。诊断的离线复刻说当前池 4/4 局都在 `ctx.Turn=7` 结束（= T7 会开始）；若将来数据/种子让某局在 **T6 就分出胜负**，这一条仍会红 —— 但那时 `ctx.Events` 里会有我新加的 `夹具：登记了但这一回合打不出 Drone（…）` 一行，能直接看出是「没轮到」而不是「机制没跑」。**没查清**：Unity 实跑里每局的 `ctx.Turn`（诊断 §三·2 已记「读不到」）。
3. **顺带（不改）**：`PlayAiTurn` 对**双方**都跑（P0 也是 AI 驱动），所以夹具 ① 会在任意行动方的回合尝试打 `Experimental Drones`；对手（Goff）牌组里没有这张卡，实际只对 P0 生效。这是既有结构，没动。
4. **#4 的读法**：`StampQueue` 走 `mr.material.renderQueue`（先实例化），所以 `sharedMaterial.renderQueue` 读回来就是盖章值 —— 与 `CardFaceProbe` 的 A1674 用例（`:331`）同一读法。若主对话复跑时这条仍红，先看消息里的「现读 {probe.Queue} / 实得 {…}」两个数是否相等，能立刻区分「没盖章」与「卡段号不同源」。
