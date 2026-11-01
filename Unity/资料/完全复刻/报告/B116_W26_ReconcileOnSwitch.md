# B116_W26 · 存档切前对账 `ReconcileOnSwitch`（UI-15 / B-322）· 2026-11-02

> 执行代理 `B116_W26`。施工面 = `判据/B116_R8_存档切换施工面.md` §1 表 A 第 1/2 行 + §3 B5。
> **结论一句：做了。** 对账机制 + 挂载 + 33 条断言；⛔ 本批**没切**卡牌等级/轮抽本身（下一批）。

## §1 雷在哪

`SaveKeys.ActiveEvent`（轮抽 `WarpforgeDraft/run.json`）与 `SaveKeys.OursCardProgress`（卡牌等级
`WarpforgeCardLevels.json`）的中央档**只可能是几周前一次性 `ImportAll` 的旧快照** —— 这两条通道至今
**0 个中央写口**（现读），玩家这几周一直在写**旧档**。⇒ 直接把读口换成 `ReadThroughLegacy`（中央优先）
就会一进界面读到几周前的进度。

## §2 方案（含「只做一次」的判据）

**API**（`SaveCenter` 类尾追加）：`ReconcileMarkPrefix = "_ours_Reconcile."` · `ReconcileMarkKeyOf(key)` ·
`bool ReconcileOnSwitch(string key, string legacyFullPath, out string detail)`（`true` = 真的写了中央档）。

**规则**（顺序执行，命中任一条即带 `detail` 返回 false）：① `StoreOn == false` ⇒ 一字节不碰；② 旧档不存在 ⇒ 不动；
③ 中央档不存在 ⇒ 交给 `ImportAll`；④ 中央档读不出来（坏档/未来版本）⇒ ⛔ 不用旧档盖它；
⑤ 标记在且**中央档当前指纹 ≠ 标记指纹** ⇒ ⛔ 不覆盖；⑥ 旧档不是合法 JSON 对象 ⇒ 不导；
⑦ 两边 `DiffPayload == ""` ⇒ 不写（首次顺便记下指纹）；⑧ `旧档 mtime ≤ 中央档 mtime` ⇒ ⛔ 不覆盖；
⑨ 其余 ⇒ `SetRaw(中央档 ← 旧档原文)` + 写标记 + `Debug.Log`（旧档赢）。

**「只做一次」= 指纹自终止，⛔ 不靠「跑过几次」的计数**：标记 key = `_ours_Reconcile.<通道 key>`
（`_ours_` 前缀由 `SaveKeys.IsKnown` 放行 ⇒ **`SaveKeys.Count` 仍 51**），payload = `{key, payload:<FNV-1a 64 指纹:长度>, at}`。
指纹**相符** ⇒ 中央档仍是「上次从旧档对过来的那一份」、没人写过 ⇒ 旧档仍是权威 ⇒ 可再对；
指纹**不符** ⇒ 中央档被别的东西写过（= 通道**已切成中央权威**）⇒ **永远停手**。
⇒ 通道切换后第一次经 `SaveThroughLegacy` 写中央档，指纹立刻不符 ⇒ **自动停手，不需要切换方配合清标记**。
⛔ 不用「只跑第一次启动」的计数器：「已对账」到「读口真切过去」之间还有空窗期，那期间子系统仍只写旧档 ⇒ 会冻在过期值上。
🔴 指纹用 FNV-1a 自算，**不用 `string.GetHashCode()`**（.NET 上逐进程随机化，跨启动永不相等 ⇒ 标记恒「不符」）。

## §3 落点（`文件:行号`）

| 文件 | 落点 | 内容 |
|---|---|---|
| `Shell/SaveCenter.cs` | `:1177-1320`（类尾，全在最后一处账锚点 `:1130` 之后） | `ReconcileMarkPrefix`（`:1186`）/ `ReconcileMarkKeyOf`（`:1189`）/ `ReconcileOnSwitch`（`:1206`）/ `WriteReconcileMark` / `TryReadReconcileMark` / `StableFingerprint` / `IsoUtc` |
| `Shell/SaveCenter.cs` | `:1606-1619`（文件尾） | 文件级 `[Serializable] class SaveReconcileMark`（同 `SaveVersionProbe`：非嵌套、非 `MonoBehaviour`） |
| `Shell/ShellRuntime.cs` | `:356`（**同一物理行**，`ImportAll()` 之后、`RegisterMirrors()` 之前） | `ReconcileLegacyChannelsOnSwitch();` |
| `Shell/ShellRuntime.cs` | `:1487-1508`（类尾；方法在 `:1498`） | 对 `ActiveEvent → DraftRel`、`OursCardProgress → CardLevelsRel` 各跑一次（路径取自 `LegacySaveReader`，⛔ 不在 SaveCenter 里重抄一份） |
| `Editor/B116W26_Assert.cs` | 新建 · 195 行 | 33 条断言 |
| `Editor/ShellScene.cs` | `:11093`（聚合行**同一物理行**、`Debug.Log(… 合计 …)` 之前） | `B116W26_Assert.Run(); _sink.Pass += …; _sink.Fail += …;` |

- **顺序 = `ImportAll → 对账 → RegisterMirrors`**：对账那次 `SetRaw` 写中央档时**双写表还空着** ⇒ 不会顺带再写一遍旧档。
- 🔶 **`自己设计（非原版）` 三件套**已打在 `SaveCenter.ReconcileOnSwitch` 与 `ShellRuntime.ReconcileLegacyChannelsOnSwitch` 的 doc 注释上（是什么 / 原版在服务端、关服后判据在远端 ⇒ 只能自定 / 调度台 2026-11-02）。
- ⛔ **没动 `LegacyMirrorOn`**（`SaveCenter.cs:915`）：施工面 §4 的 8 条门槛只满足第 3、4 条（第 1/2 条要等两条通道
  真切过去、第 5 条真档全表对账没跑、第 6 条是用户产品决定）；也**没动** `LegacySaveReader.cs` / `SaveKeys.cs`
  （前者不在本批白名单、后者本波归 `W21`）。

## §4 断言（33 条 · `Editor/B116W26_Assert.cs`）

| 段 | 条数 | 盯什么 |
|---|---|---|
| 前提 | 5 | `ValidateTable()` 通过 · `Count == 51`（标记 key ⛔ 不进 `All`）· `IsKnown(标记)` · 标记 key 形状 · `StoreOn` |
| **A 旧档赢** | 6 | 中央档 21 天前 / 旧档现在 ⇒ **真的写了** · 中央 == 旧档（逐字）· 逐字段相等 · 标记落下 · 日志说「旧档赢」 |
| **C 幂等** | 3 | 再跑一次 ⇒ 不写 · 内容不变 · **mtime 都不变**（证明真没写盘） |
| **B 灭自证「不是永远旧档赢」** | 3 | 中央比旧档新 ⇒ 不写 · 中央原封不动 · 日志说「不比中央档新」 |
| **D 只做一次（= 雷本身）** | 3 + 3 前提 | 模拟通道已切换（`SetRaw` 真写一次中央）后让旧档变新 ⇒ **不覆盖** · 切换后的真写保住 · 日志说「已被写过」 |
| **E 门位** | 3 + 1 前提 | 清 `OverrideRoot` ⇒ 不写 · 那份中央档**一字节没动** · 硬闸 `!Application.isPlaying`（Play 里会碰玩家真档） |
| **F 缺档/坏档** | 4 | 旧档不存在 · 旧档不是 JSON 对象 · 中央没被坏档污染 · 中央档不存在 ⇒ 交给 `ImportAll` |

**灭自证四面**：对账没接 ⇒ A1；改成永远旧档赢 ⇒ B2；每次启动都重写 ⇒ C3；没有「只做一次」判据 ⇒ D2。
**不碰玩家真档**：全程 `%TEMP%/wf_b116w26` 同时当 `SaveCenter.OverrideRoot` 与旧档根；跑之前先 `UnregisterLegacyMirror`
（否则 `SetRaw` 会顺带写登记表里那份旧档），`finally` 还原 `OverrideRoot` + 两条登记并删目录。

## §5 三道闸读数

| 闸 | 读数 |
|---|---|
| `bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（第 3 次复跑；前两次的错全在**别人的在飞文件** —— `BattleDriver.cs` 与上述两处，逐条复核过**没有一条**落在本批四个文件） |
| `check_ledger_points.py` | **A 5831 · 通过 5831 · 漂移 0 · 失败 0**（告警 864 · B 4 / 落点问题 2，与切换前一致） |
| `check_aggregate_lines.py Editor/ShellScene.cs --any-assert-call` | **PASS**（`:11093` 69 个调用全在首个 `//`（字符 8350）之前 · 行宽 9212） |

**行数/行尾**：`ShellScene.cs` 11,447 → 11,447（同物理行）；`SaveCenter.cs` 1,460 → 1,621、`ShellRuntime.cs`
1,487 → 1,510（**只加在最后一处账锚点之后**：SaveCenter 最深 `:1130`、ShellRuntime 最深 `:1450` ⇒ 账闸实测 0 漂移）。全部 LF、裸 CR 0。

## §6 下一步：切「卡牌等级」要注意什么

1. **对账已先落地** —— 那一批只改 `CardDetailPopup.cs:186` 读 / `:215` 写；⛔ 顺序不能反：先让本批挂载在真机跑过一次
   （看到 `[SaveCenter] 切前对账：'_ours_CardProgress' 旧档赢…` 或「两边逐字段相等」），再切读口。
2. `Editor/B104_P3_Assert.cs:216` 用「`OverridePath` 指**目录** ⇒ `Save()` 回 false」验失败支（**已现读确认**）——
   换成 `SaveThroughLegacy` 后语义仍在（`AtomicWrite` 失败回 false），但**错误文案会变**。
3. **轮抽放最后**：100 处账落点 + 两条删档路径（`:825` / `:849`），删档必须同调 `SaveCenter.Delete(ActiveEvent)`，
   否则会**复活已结束的一局**（`ReadThroughLegacy` 中央优先）。
4. 切换那批做完后本对账会**自动停手**（指纹不符）—— ⛔ 不要手工删 `_ours_Reconcile.*`，删了就等于「当成第一次」。

## §7 顺手发现 / 边界

1. 标记里存**指纹**不存整份 payload 副本（卡牌等级那份上千条，存副本会让每次对账多写一份等大的档）；对账**不改旧档** —— 只在 `SetRaw(中央)` 之后写标记，此刻双写表为空、标记 key 也没登记双写。
2. 旧版客户端「回滚后再回来」不在本批自动处理范围（届时指纹已不符 ⇒ 停手），仍由 `ImportAll()` 末尾的 `NewerLegacyReport()` **大声报「旧档比中央档新」**（自动倒灌要人拍板）。
3. 未跑 Unity（红线）；未动 git；未碰玩家真档。
