# B9 · W70VOPT —— `*ValueOptions` 异常值 `8` / `17` 查证报告

> 判据正本：`资料/完全复刻/判据/ValueOptions异常值_判定.md`（本报告只给结论，细节看那份）
> 只读代理 · 未改任何 `.cs` / `.py` / `.json` / 账 / 正本 / 资产 · 未跑 Unity · 未动 git

## 结论

1. **`8`（38 处）和 `17`（8 处）都是【原版 SO 里就有的值】** —— 直接读 bundle typetree + 直接读原始字节双向确认，**不是我们抄错**。
2. **不是枚举缺项、不是字段类型用错**：四份反编译（cpp2il_dev_ilspy / il2csharp / Ghidra `dump.cs` / AssetRipper）**全部 26 个成员、都没有 8/17**；typetree 里那两格的字段名确实是 `DamageCriteria.valueOptions` / `CardEffect.healthValueOptions`（类型 `ChooseValueOptions`）。
3. **原版运行时走 `default` 支**：伤害/治疗取值 ⇒ `return minValue`；buff 折叠 ⇒ 打日志后 `return minValue`（**不乘 multiplier**）；AI 评分 ⇒ `round((min+max)/2)`。
4. **这 46 行在可观测行为上全部是死数据**：38 行的 `chosenAbility` 无一属伤害族（0/20/30/60/70/100/320）；8 行的 `healthValueOptions` 要么所在行不应用 buff、要么 `healthChange=0`、要么 `buffType=3 addTrait`（应用阶段只加 trait、不碰属性）。
5. **施工结论：枚举不改、字段类型不改、数据不改**；唯一要做的是 **G26/G27 里把三条 `default` 支逐字照抄**，⛔ 不许「按 0」、⛔ 不许「当 `Normal`」（后者会乘 multiplier，与未知档不等价）。

## 证据（文件:行号）

- 数据真值：`verify_src2.py`（typetree，1351 张 / 异常 0）· `byte_check.py` · `byte_check2.py`（原始字节命中 `08 00 00 00…` / `11 00 00 00`）· `node_order.py`（字段序与 `DamageCriteria.cs` / `CardEffect.cs` 声明序逐字一致）
- 枚举真值：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs` · `d:/2/tools/il2cpp_out/dump.cs:20865-20894`
- 行为：`d:/2/tools/decomp_full/AbilityLogic__GetValueFromCriteria.c:25` + 15 处 `return uVar6` · `AbilityLogic__NewBuffStat.c:275-279`（fallback）vs `:61-68`（Normal 支乘）· `AbilityLogic__GetAverageValue.c:28,136`
- 可达性：`d:/2/tools/il2cpp_out/dump.cs:19456-19470`（`chosenAbility 0x10 / damageCriteria 0x40 / healCriteria 0x48 / buffEffect 0x50`）· `CardScript__AddEffect.c:454-455`（属性只在 `case 1` 应用）· `:460`（`case 3` 只 `AddTraitSilently`）· `AbilityLogic__PlayAbility.c:1274`（治疗读 `+0x48`）· `:235-236`（`furtherMultiplier` 乘进 multiplier）
- 版本配对：`d:/2/unity_run_ref/Warpforge_Data/globalgamemanagers`（`6000.2.6f2` / `1.35.0`）↔ bundle 头 `UnityFS 6000.2.6f2`

## 没查清的部分

- 未穷举 `AbilityLogic.damageCriteria(+0x40)` 的**全部**读取点（核过 `PlayAbility` 的 4 处 + `GetAverageDamage` 唯一调用者 = AI）；建议 G26 落地前反汇编扫一遍 `+0x40`。
- 未查原版编辑器为何产生 `(3,5,5,8)` 这坨模板（本机无编辑器）—— 只记为「原版数据既有」，⛔ 别写成因果断言。
- 未判 `damageType=5` 本身语义（另一条线）。
- 未查 `4b8214d3…` Storm of Silence 为何不在池内。

## 该怎么改（施工结论）

| 层 | 改什么 | 判据 |
|---|---|---|
| 枚举 | **不改**（⛔ 不补 8/17） | 四份反编译 26 成员 |
| 解析/运行（G26） | 未识别 option ⇒ **返回 `minValue`** + 出声 | `AbilityLogic__GetValueFromCriteria.c` |
| 运行（G27，buff 折叠） | 未识别 option ⇒ **返回原始 `*Change`、不乘 multiplier** + 先打日志 | `AbilityLogic__NewBuffStat.c:275-279` |
| AI | **已正确**，无需改 | `AiScoring.AverageValue`（`AiScoring.cs:770-775`） |
| 数据 | **不改** —— 8/17 是「原版有超纲值」的唯一活标本，用来验收 default 支 | —— |
| 验收 | 表驱动单测加两条：`8 → minValue`、`17 → 原始 change（不乘）`；再一条「未知档必须出声」 | —— |

## 顺手发现

1. 🔴 **2 条 `doHeal` 行的 `damage` 是垃圾 `(3,5,5,8)`，真正生效的是 `heal`**（Righteous Fury `heal=(1,1,0)` ↔ 卡面「Heals **1**」；Sawbonez `heal=(1,5,0)` ↔「heal **1-5**」）。把 `damage` 当通用取值结构 ⇒ 这两张治疗会算成 3。G26 落地必须点名。
2. ⚠️ `gen_cards_engine.py:1802` 注释只提了「非伤害行的 `(1,1,0,0)` 默认值」，**漏了第二种非伤害模板 `(3,5,5,8)`**（22+ 行，且**从不**出现在伤害族行）。本轮按铁律 13·3 **未改**，留给调度台派执行代理补注释。
3. ℹ️ `ai_scoring.json` 现读 `valueOptions` 只有 `{0:380, 80:1, 90:1, 204:1}` ⇒ 8/17 从未进入 AI 数据。
4. ℹ️ 「本地原版 = 1.35.0 / Unity 6000.2.6f2，与 allcards bundle 同代」这条**以前没落在任何文档里**，建议并入判据目录的公共前提。
