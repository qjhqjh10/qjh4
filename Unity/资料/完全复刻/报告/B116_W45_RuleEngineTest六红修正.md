# `B116_W45` · `RuleEngineTest` 六红修正（只改测试侧）

- 执行代理：`B116_W45`（写权限仅限 `RuleEngine/Editor/**`；`RuleEngine/Core/**` 一行未动）
- 日期：2026-11-02
- 结论来源：只读诊断 `B116_D1` —— 「3 类×2」为误读；实际 = `RuleEngineTest` 本体 3 条 + `B116W27_Assert` 3 条
  （后者只加 `_fail`、不进回显列表）。

## 逐条改前 / 改后

### ① `A1574`（期望值过期 · α）— 已改
- 位置：`Assets/RuleEngine/Editor/RuleEngineTest.cs:35329-35338`（doc 段，10 行原地重写、行数不变）
  与 `:35381-35384`（期望值 `Check(host.Attack, 4, …)`）。
- 改前：期望 **4**；doc 写「`subject` = 被并掉那张」「**「it」↔ `actingCard`**」；
  自证标记 = 「不传 subject ⇒ 5」。
- 改后：期望 **5**；doc 改成「`subject`(=actingCard) = 被并掉那张；监听者那句 `it` 按 `ta=30` 读
  **targetCard** = 留下的 host」，并加一行订正留痕（铁律 5）；自证标记改成
  「**`FireBoardListenerDeferred` 的 seed 改回 `subject` ⇒ 4**」。
- 判据：`EffectResolver.cs:10650`（`B-314` 已落地：`(kind == Swarm && target != null) ? target : subject`）＋
  `cards_engine.json` 的 `TL15 abilityTargets[{620, ta:[30]}]` ＋ `AbilityLogic__GetTargets.c:192-202`
  （六参重载 `ta == 30` 读 `param_5` = targetCard）。
- 一致性：与 `RuleEngineTest_B116W12.cs` ①-a（`host.Attack == 5`）**不再矛盾**。

### ② `A7b`（needle 措辞 · α）— 已改
- 位置：`Assets/RuleEngine/Editor/RuleEngineTest_B116W6.cs:177`（含 `:178` 文案）。
- 改前：`HasLogB116W6(ctx, "不在可玩卡池里")`。
- 改后：`HasLogB116W6(ctx, "都没有它")`；文案同步成「可玩池与选项 SO 旁表都没有它」。
- 判据：生产措辞 `EffectResolver.cs:21652-21654`「**可玩池与选项 SO 旁表都没有它**」——
  `不在可玩卡池里` 全仓只在注释与该断言自身出现。**生产措辞未动。**

### ③ `AbilitiesForTrigger(580)` 前提（夹具 · δ）— 已改
- 位置：`Assets/RuleEngine/Editor/RuleEngineTest_EngineB94Z2.cs:261-273`（夹具签名 + 一行取值）、
  `Assets/RuleEngine/Editor/RuleEngineTest_B116W19.cs:181`（调用点）。
- 改前：`B94Z2Fixture` 硬造 `new AbilityTargetSpec[triggers.Length]`（**全 null 占位**）⇒
  `AbilitiesForTrigger(580)` 恒空表 ⇒ `W19T3` 的 C3 变成「0 == 0」空断言。
- 改后：夹具尾部加**可选**形参 `CardDef.AbilityTargetSpec[] abilityTargets = null`（`null` ⇒ 逐字旧行为），
  `ta = abilityTargets ?? new AbilityTargetSpec[…]`；`W19T3` 显式喂
  `new[]{ new CardDef.AbilityTargetSpec { Trigger = 580 } }`。
- 判据：`CardDefPartial_B94B16.cs:181-191` 按 `s != null && s.Trigger == trigger` 过滤；
  真卡池 1,273 行里 null 行 = **0** ⇒ 纯夹具问题。

### ④ `B116W27_Assert` 的 C1 / D1 / D4（期望值过期 · α，**读码推断**）— 已改，待重跑确认
- 位置：`Assets/RuleEngine/Editor/B116W27_Assert.cs`（C1 `:257`、D1 `:298`、D4 `:314` 一带）。
- 改前：**1 / 1 / 0**；改后：**2 / 2 / 1**，并重写三条语义注释。
- 依据：`DispatchCardAbilityRows` 在战术路除 `ca 380` 还会派 `ca 320`
  （`CardAbilityDispatch_B116.cs:80` B5 ＋ `:156-157`，执行体 `:236`）；
  `GOF104` 的 `chosenAbility = [30,260,320,380]`、`AM54 = [0,30,320,380]` ⇒ trigger 0 上确有 `ca 320` 行。
  D4（上一张能力目标表为空）⇒ 380 不结算、320 照派 ⇒ 1。
- 🔴 **`待重跑确认`**：这三条是**读码推断**、未真跑宿主；若实际读数不符，错在推断而非被测实现，
  请以重跑输出为准再订正一次（⛔ 别反过来改产品代码）。

## `RuleEngineTest.cs` 行数

| 时点 | 行数 |
|---|---|
| 改前 | **39,737** |
| 改后 | **39,737**（零行数变化） |

其他被改文件行数：`RuleEngineTest_B116W6.cs` 276（不变）· `RuleEngineTest_B116W19.cs` 263→268（+5）·
`RuleEngineTest_EngineB94Z2.cs` 281→287（+6）· `B116W27_Assert.cs` 465→469（+4）。
`RuleEngineTest.cs` 现读 `CRLF=39737 / LF=39737 / 裸 CR=0`（行尾未翻）。

## 三道闸读数

| 闸 | 读数 |
|---|---|
| `bash 工具/typecheck.sh` | 运行时错误数 **0** / 编辑器错误数 **0** |
| `python 工具/check_ledger_points.py` | A **5,831** 条 · 通过 **5,831** · **漂移 0** · **失败 0**（B 档 4 条只报告：落点问题 2 条·均在散文） |
| `bash 工具/ruleprobe.sh play check` | **rc=0**；哨兵基线 `a6ab8bcb…` 一致（未重钉，本来就对） |

## 边界与未做

- ⛔ 未跑 Unity（按调度台口径统一跑）· ⛔ 未动 `活状态表.md` / 账本 / `RuleEngine/Core/**` · ⛔ 未动 git · ⛔ 未联网。
- ④ 的「待重跑确认」是本次唯一带风险的改动面（其余五条均为α/δ 的确定性订正）。
