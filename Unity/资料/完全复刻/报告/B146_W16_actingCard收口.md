# B146-W16 · actingCard 形参收口（2026-11-05）

> 任务 = `B146-A18-ACTINGCARD-PARAM`。执行代理 W16。本文件为调度台按代理交回内容补落盘（代理当时未写报告）。

## 一、改了什么

- `RuleEngine/Core/TargetCriteriaEval.cs`：`Matches` 加第 9 形参 `UnitState actingCard = null`；`ta=40/70/80`（`:111-116`）改为：
  - `actingCard == null` ⇒ `null` + `gap`（判不了，保留候选 + 出声）；
  - 否则 `ReferenceEquals(candidate, actingCard)`。
  - 删掉 `if (ta == 80) return true;` 与 `ReferenceEquals(candidate, thisCard)`。
  - 判据：`AbilityLogic__GetTargets.c:761-769`（建池即 return，不进 36 格）；`ta=70` 同出口 `:845-847`（原版 LogError 后 `goto` 同一处）。
- 可改文件 6 个调用点已透传：`ActionPump.cs:805` · `:2295` · `:2389/2391` · `:2402/2404` · `EnchantmentExecutor_B115.cs:1127`。
- **16 个调用点**（B146-D 记「13 个」是旧口径）；`RuleCore.cs:11767/18088` 与 `EffectResolver.cs:2431/3672/17919/21263` 共 5 处给补丁未改（见 `活状态表` `B146-A18-PATCHES`）。

## 二、部分上线为何安全

7 个不传 actingCard 的点，`ta=80` 从「无条件 true」变「null ⇒ 保留候选 + 出声」：候选池不变，只多 `nojudge:` 日志。影响池内 **23 行 / 23 张卡**（`ASH79 ASH66 AM17 AM7 BL30 DA5 DA28 DA22 SAU2 SAU31 GOF68 GOF57 GOF20 SOR26 SOR37 SW35 TAU34 TAU25 TL69 UM12 UM81 UM70 UM66`），**没有卡会丢目标**。

## 三、断言

- `B145_E5_Assert` GroupA18 6 条（C1–C6），C2/C3/C5 分别灭「退回无条件 true」「退回 thisCard」「把 null 冒充成 true」三种坏法；宿主已接线。
- `typecheck` 运行时 0 / 编辑器 0；4 个文件 LF、裸 CR 0。

## 四、两处口径记录

- `AiTargets.cs:612` 仍 `if (ta 为 40 或 80) return isSelf;`：AI 链的 `thisCard/thisInst` 就是正在考虑打出的那张，与原版 actingCard 重合 ⇒ 直接改可能变错，另立 `B146-A18-AITARGETS-40-80`。
- 原版有两个函数：`GetTargets`（建池，用 `actingCard`）与 `CheckIfMeetsCriteria`（筛，用 `thisCard`，`:184-189` 对 40/70 就是 `candidate == param_1`）。我们的 `Matches` 同时扮演两者 ⇒ **同一个 `ta=40` 在不同调用点该传的对象不同**（`EffectResolver:21263` 传 `thisCard` 是原版如此，不是回退）。
