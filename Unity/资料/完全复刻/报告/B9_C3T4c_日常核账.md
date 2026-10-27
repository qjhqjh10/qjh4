# B9_C3T4c · 日常/奖励线 B 行逐条核账 —— 报告

> 子代理 **C3T4**（查判据型 · 只读）· 2026-10-27
> 范围：`d:/4/Unity/资料/完全复刻/账/日常_奖励.tsv` 里 **状态 = B 的全部 114 行**（现读：该表 779 行，A 318 / B 114 / C 238 / D 109）。⛔ 未碰 C（238 条）。
> 产出：`判据/B行逐条核_日常.tsv`（8 列 × 114）· `账/_parts/B9_C3T4c.tsv`（11 列 × 114；`状态` 全保持原值 B）· 本报告。

## 一、方法

1. 逐行取 `判据出处` → `d:/2/tools/decomp_full/` 找原版真方法体（记 EXISTS / MISSING）。
2. 逐行解析 `我们的落点` 的 `文件:行`，打开该行看**是不是一个方法声明**（落点闸的可操作判据）；对「落点失效」候选另 grep 落地文件/全仓，确认是不是「早就做完了只是落点错」。
3. 结合行内已有的 `差异`（历轮代理逐句读原版方法体写下的缺口）判 **A / B / 落点失效 / 判据缺**。
4. 铁律 11：休眠机制**不判不做**；只有「原版不可达 / 判据空」才可入 D（需时间限定）。

## 二、结论分布

| 建议等级 | 条数 | 含义 |
|---|---|---|
| **B（真偏离）** | **32** | 落点是**真实方法声明**，但 `差异` 有实打实缺口 |
| **落点失效** | **76** | 引用的 `文件:行` **不是方法声明**（字段 / 注释 / 语句 / 花括号 / 空白 / 类型声明 / 属性），或「无落点(—)」。**≠ 未实现** —— 多数是账的落点要重钉（很多是行号漂移或指向数据字段；等价实现常在附近或别的文件） |
| **判据缺** | **6** | 原版方法体读不到或判据为空 |
| A（等价） | 0 | 本批无「方法级落点 + 无缺口」的行 |

## 三、6 条「判据缺」

| # | 方法 | 原因 |
|---|---|---|
| 35 | `LiveOpsAssetUtility.TRef>` | **账的方法名被截断**（原 `GetReference<T,TRef>`）⇒ 签名非法、无法定位；⚠️ 账数据 bug |
| 97 | `PracticeEvent.SetDifficulty` | 判据**为空**：原版写入 `selectedDifficulty` 后**全量反编译零读取点** ⇒ 只到字段级（`BattleDriver.aiDifficulty`，字段非方法）。按铁律 11 例外①可入 D（时间限定），此处记判据缺 |
| 105 | `SignalEventTrigger.Register(Action)` | **泛型类**（`SignalEventTrigger<T>`）：decomp_full **0 产物**、ILSpy/il2csharp 体都是壳（`Method not found @1818A6ED0`）。恢复路 = `-readOnly` 泛型重导（同 G6） |
| 106 | `SignalEventTrigger.TryTrigger(T)` | 同上 |
| 107 | `SignalEventTrigger.Unregister()` | 同上 |
| 108 | `SignalEventTrigger.ValidateSignal(T)` | 同上（抽象声明可读；具体子类 `AddTagTrigger`/`WildcardTrigger` 体在 `out_il2csharp` 可读） |

## 四、76 条「落点失效」的构成（这是本批最值钱的一格）

按落点行的实际内容分类（都能在 `判据/B行逐条核_日常.tsv` 的「我们的落点」列对上）：

- **指向字段 / 常量 / 数据表**（落点是 `static readonly ...` / `public X;` / `public readonly List`）：账把「数据持有」当方法落点。例 #3/#4/#7/#77（`DailyData._daily`/`_weeklySteps` 数组）、#37/#53/#54/#68/#69（`LiveOpsLocalCatalog.PrimaryTerm` 字段）、#95（`ArmyCells` 字段）。
- **指向注释 / `<summary>` / `<code>`**：例 #15/#32/#38/#55/#56/#58/#60/#61/#63/#67/#87/#88/#93/#110/#114。
- **行号漂移 → 指到方法体中间语句**：例 #8（`var f=…`）、#59（`int queued=0`）、#72/#74/#76（`return _weeklySteps[...]`）、#83（`if (rcT…)`）、#91（`int pending=…`）、#111/#113（`if …`）。
- **指向 `{` / `}` / 空白行**：例 #13/#26/#33/#36/#40/#79/#112 等（多为漂移）。
- **指向类型声明 / 属性**：例 #24（`class Msg`）、#41/#46（property）、#25/#27（字段）。
- **无落点 `—` / NOLINE**：#9/#14/#19/#78/#80/#81/#82（#97 归判据缺）。

> 🔴 **「落点失效 ≠ 未做」**：抽查（coordinator 提示的「先 grep 落点」）证明不少行**等价实现其实存在**，只是账的落点没指过去 —— 例 `LiveOpsHandler.GetEvent(string)` 实现在 `Shell/LiveOpsHandler.cs:696`（账落点却指到 `LiveOpsLocalCatalog` 的 `PrimaryTerm` 字段）；`RefreshHandler` 在 `LiveOpsHandler.cs` 有实现（账落点行是方法体里的语句）。⇒ 这 76 条的**处理动作 = 重钉落点（`--fix` 只能修漂移；字段/注释/无落点要人工重指）**，不是「当新活派单」。

## 五、32 条 B（落点真实方法 + 真缺口）的性质

按家族：
- **LiveOps 基类/调度器**（#43 OpenWindow / #51 ExpireEvent / #57 QueueEventPopup / #62 ScheduleEventPopups / #64 ShowTriggerEventPopup / #66 BuildHandler / #70 Initialize / #71 ResetHandlers / #48 DeserializePersistence）：逐句照抄已做，缺口多为**依赖未接的 P2**（`GetComponentReference` 的 GUID→prefab、`mainMenuOptions`）或**反编译读不出的字段**（#36/#40/#59/#60/#63 的「某字段读不出 / 未坐实」）。
- **RewardService / Inventory**（#99 Collect / #101 CollectItemReward / #103/#104 SharedEventData）：等价层在，缺分派细节/Energy 特判/context 子类。
- **ChallengeLog**（#5/#10/#11）：只有局部计数/成就，无通用变量统计表。
- **PlayEvent**（#85/#86/#90/#92）：等价层在，缺未结算判定/平局 2.0/LastOpponentInfo。
- **其它**（#1/#2/#17/#18/#20/#22/#23/#75/#84/#96/#109）：等价层在，缺分支/数据源。

> ⚠️ 不少 B 行的「未坐实 / 读不出」属**判据可再挖**（不是我们没做）：可回 `decomp_full` 或 VA 反汇编补证据后升 B→A（视缺口是否消失）。

## 六、验证深度与残余（如实）

- 114/114 的**原版方法体存在性**（decomp_full）与**落点行内容**已机械核过（这是 §三/§四 的全部依据）。
- **泛型类缺口**：`LiveOpsEvent<TSave,TData>` / `LiveOpsHandler` / `PlayEvent` / `MissionEvent` / `GameNewsEvent` / `FastModeBaseEvent` / `BaseEventProgressRewardDrawerController<T>` 在 decomp_full **无方法体**（G6 已知缺口，可 `-readOnly` 泛型重导补）；这些行多因落点失效被记「落点失效」，其原版缺口文字来自现有 `差异`（历轮代理读 ILSpy/VA 写下），本轮**未逐条重做 VA 反汇编**。
- 行为层「逐句比」：本轮主要**复核落点有效性 + 差异结论一致性**；未对 114 条全部重读原版全文（体量原因）。**残余风险**：少数「落点失效」行的等价实现是否真的存在，需重钉落点时逐条确认（抽查已发现多例存在）。
- `check_patch_conflicts.py` → **无跨补丁重复键（4 份补丁）**；补丁 114 行全 11 列、状态全 B、轮次=B9_C3T4c。
- 顺手发现 ① **#35 账方法名被截断（`TRef>`）** —— 账数据 bug，需修方法名（`GetReference<T,TRef>`）。② 本域 `落点` 列大面积「字段/注释/漂移」，建议整表过一遍落点闸。
