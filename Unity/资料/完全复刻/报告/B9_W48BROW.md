# B9_W48BROW · 战斗逻辑驱动 402 条 B 行重裁（2026-10-27）

**产出正本**：`判据/战斗逻辑驱动_B行重裁.md`（分层表 + 逐族结论 + 可执行清单 + 比例预估）。

## 结论

1. 402 条 B **按 B 理由聚成 13 族**（关键词单标签，可复现）。最大三族：**F9 观测/边界未跑 94 · F10 未核验/没读全 57 · F14 其它 58** ⇒ **≈73% 是「做了没核完」，不是缺口**。
2. **真缺口（C）≈ 43 条（11%）**，集中在 F4（交换/变形/复制墓地/谓词/计数器/`ReleaseCardbacks` 未实现，grep 实证 0 命中）+ F3（`ChooseValueOptions` 未落）+ F14。
3. **能立刻升 A 的硬证据 ≈ 50 条（12%）**。其中**零风险 3 条**（280/326/380，改落点即可）+ F6 本地化 8 条（指 `Loc.cs:3421 T()`）。
4. **F7（15 条）的机械化铁证**：15 条翻成 A 跑 `check_ledger_points.py` ⇒ **通过 0 / 失败 15**，原因全是「落点不是可机检的方法声明」。3 条能改（有真方法落点），12 条是字段/无对象（§4.1 禁字段落点 ⇒ 只能 B）。

## 证据（关键 file:line）

- `RuleCore.cs:5374（IsValidTarget）` · `:8431（ApplyDamage）` · `:1662（BeginTurn）`（L380 的真实 XOR 在 **`:1754`**，账里 1738 已漂）。
- `PlayerState.cs:69 Board` / `:72 Warlord`（字段，§4.1 不当落点）· `ActionQueue.cs:209 Count`（多行属性）· `BattleContext.cs:1049 CardsPlayedInTurn`（字段）。
- 缺口实证：`ReleaseCardbacks` 仅注释 `BattleDriver.cs:32159`；`exchangeMinion`/`MissActions`/`ResolveCopyUnitInCemetery`(RuleEngine) grep 全 0。
- 等价证据：`Loc.cs:3421 T()`；`RuleCore.cs:2075 RefreshForNewTurn`（L1093 激活）。

## 没查清的部分

- F4/F14 内部「真缺口 vs 结构等价」需逐条剔（约各 1/3 是等价，勿误判 C）。
- F5 的 A/D 二分需三源核实「原版零调用」后才能判 D。
- F13 调试串 3 条（334/335/340）是否可判 D 待调度台定。

## 该怎么改（可执行清单）

见正本 §三。摘要：① 改 3 条落点升 A（326/280/380）；② F6 本地化族指 `Loc.T` 并核 key；③ F4/F3 转 C 逐条补；④ F9/F10 维持 B，下一步跑 `ruleprobe play` 两次读数 / 逐句读。**⛔ 未改任何 `.cs` / 账主表 / 工具 / 资产**（本代理只写判据与报告）。

## 顺手发现

1. 🔴 **口径级**：纯 getter 等价 → 字段落点，在 §4.1 下永远过不了机检（F7 的 12 + F6 大部分）。不是没复刻，是「复刻成字段直读」。需调度台裁：加转发方法 或 给 §4.1 加「纯取值器可用字段声明（只报不失败）」。
2. L380 账里 `RuleCore.cs:1738` **已漂到 `:1754`**（1738 今天是 `DrawnThisTurn` 清理）。
3. L457 `PlayAi` 的 MoveNext 体**存在**（`BattleManager._PlayAi_d__399__MoveNext.c`）⇒ 归「没读」不归「读不到」。
4. L381 `GetTargetInPos` 的 `.c` **有 271 行**，只「求交系数」读不出，别判整条体被剥。
5. `mini15.tsv` + `check_ledger_points.py` 是可复用的「落点机检」验证套路（改完落点复跑，别只看状态列）。
