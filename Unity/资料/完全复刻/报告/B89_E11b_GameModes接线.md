# B89-E11b · `GameModes` 接线（注册表 · L584/L585 的那一半 · 立账）

> 执行代理 `B89-E11b` · 2026-10-28（转岗自 `B89-E11`，判据沿用不重查）
> 上一批：`报告/B89_E11_GameModes.md`（12 条 · A=9 / B=3）· 账 `账/_parts/B89_E11.tsv`

---

## §0 一句话答案

**`GameModesHandler` 已经真注册进 `LiveOpsManager`** —— 就在原版那条注册形状里（单例 getter）：`Shell/LiveOpsManager.cs:92`
`Register(LiveOpsLocalHandler.Default)` **之后**追加 `Register(new GameModesHandler())`，**已有 handler 的注册序没动**，且**该文件行数不变**（356 行 → 356 行，只就地改写了一行注释 + 一行 getter）。

> ⚠️ **但必须如实说**：`LiveOpsManager.Instance` 全仓**目前 0 个生产调用方**（改之前也是 0 —— `LiveOpsLocalHandler.Default` 同样因此从未被登记过）。所以「注册上了」≠「启动链会建它」——
> 这只是**原来就存在的接线缺口**，不是我这次弄坏的。见 §4 待接 ①。

---

## §1 ① 注册表接线

| 改了什么 | 位置 | 说明 |
|---|---|---|
| `Register(new GameModesHandler())` | `Shell/LiveOpsManager.cs:92` | 追加在 `LiveOpsLocalHandler.Default` 之后；`Register` 本身幂等（同类型覆盖） |
| 注释就地改写（**零行数变化**） | `Shell/LiveOpsManager.cs:89` | 写明「建单例时登记内建 handler / 注册序不变 / 判据 = 原版 `BuildHandlers` 建成并登记（`LiveOpsManager.cs:2029`）」 |

**断言（`Editor/GameModesCheck.cs` E 节 · `RunRegisteredHandler()` :239）**
- E1 `GetHandler<GameModesHandler>()` **非 null**（= 真在注册表里）
- E2 `HandlerKeys` 同时含 `"GameModes"` 与 `"local"` ⇒ **没顶掉已有那个**
- E3 拿注册表里那个实例：`SetupEvents({})` → `SetResultCount("Practice", Player)` ⇒ `ResultCounter["Practice"].Won == 1`
  （= 原版 `PracticeEvent.OnMatchFinished` 第一句 `LiveOpsManager.GetHandler<GameModes>().SetResultCount("Practice", result.battleWinner)` 的落地路径）

---

## §2 ② `L584` / `L585` 落在 `GameModesHandler` 的那一半

### L584（练习对局记账）—— `GameModes` 半：**已就绪**
原版 `PracticeEvent.OnMatchFinished`（`Everguild.LiveOps/PracticeEvent.cs:78-85`）只有两句：
```csharp
GameModes handler = LiveOpsManager.GetHandler<GameModes>();
handler.SetResultCount("Practice", result.battleWinner);          // ← 这一半 = 本批
PlayerDataManager.singletonManager.RecordPracticeMatchFinished(…); // ← 服务端记账（P3）
```
- `SetResultCount` 已按 `.c` 落地（E11 账 224），注册表接线后 `GetHandler` 也拿得到了 ⇒ **GameModes 侧两句话都通了**（断言 E3）。
- **还缺（不在我的文件里）**：① **调用点** —— 本机没有 `PracticeEvent` 类，练习流程的胜局回调在 `Shell/PracticeModePopup.cs`（归 R3b/E10b）/ `Battle/BattleDriver.cs`（**E5b 正在写**）⇒ 按要求**写待接点，没动那两个文件**；② `RecordPracticeMatchFinished`（服务端，P3）；③ 它要的 `enemyMilestone` / `MatchData` 两个派生参数。

### L585（练习首胜解锁草稿）—— 这里有一个**必须先说的阻塞**
原版 `PracticeEvent.OnMatchWon` 只有两句：`demoData.canEnterDraftMode = true; PlayerDataManager.singletonManager.UploadOnlineDemoData();`
R2d 已经把闸建好了（`Shell/DraftEventBase.cs:340/343`：`public static Func<bool> CanEnterDraftMode` + `CanEnterDraft`，null ⇒ 允许）。**但它是泛型类上的 static**：

```csharp
public abstract class DraftEventBase<TS, TD> : PlayEvent<TS, TD> { … public static Func<bool> CanEnterDraftMode; }
```
⇒ **每个闭合类型各一份**。而本机**没有任何具体子类**（`Shell/DraftEventBase.cs:346` 明写「本文件不给具体子类」）⇒
**今天不存在那个 static 实例，生产上无处可注入**。⛔ 我没有为了「看起来接上了」去编一个闭合类型。

我做的（生产者，写在 `Shell/GameModesHandler.cs` 类尾，前面成员的账落点不受影响）：

| 成员 | 行 | 作用 |
|---|---|---|
| `DraftModeUnlocked` | `:461` | 原版 `demoData.canEnterDraftMode` 那一位的本地替身 |
| `InstallDraftGate<TS,TD>()` | `:471` | 注入 `DraftEventBase<TS,TD>.CanEnterDraftMode = () => DraftModeUnlocked`（**活读口**，不是快照） |
| `NotifyPracticeMatchWon()` | `:484` | 原版 `OnMatchWon` 的落地口：置 true + 出声（上传那半是 P3） |

**将来接上只要一行**（等轮抽线的具体子类落地）：
```csharp
GameModesHandler.InstallDraftGate<DraftEventSaveGame, DraftEventData>();   // 或该子类自己的 TS/TD
```

🔶 **自己设计（非原版）—— 两处，都已打标**：
1. **这一位住在 `GameModesHandler`**：原版在 `PlayerDataManager.demoData`（本机无 `demoData` 层，全仓 `canEnterDraft` 0 命中）。
2. **默认值 `true`（放行）也是我们挑的**：原版默认 false **且**有「首胜置 true」的解锁路径；我们**没有**那条路径（账 584/585 仍 C）
   ⇒ 若照原版默认 false 接上，轮抽会变成**谁都进不去且不可恢复**（原版没有的死状态）⇒ 兜底放行。等练习首胜调用点落地，把 `DraftModeUnlocked` 的初值翻成 `false` 即逐字复刻（**接口不变**）。

### 灭自证（要求 3）
`GameModesCheck` F 节（`:278`，用检查文件里的 `FakeDraftEvent : DraftEventBase<PlayEventSaveGame, DraftEventData>` 拿一个闭合类型）：
- **F1 未注入 ⇒ 允许** —— 先把该闭合类型的注入清掉，`CanEnterDraft == true`（**= R2d 改前的逐字行为**，证明我们没把这支改坏）
- **F2** 调 `InstallDraftGate<…>()` 后该闭合类型的 `CanEnterDraftMode != null`
- **F3** `DraftModeUnlocked = true` ⇒ 允许
- **F4 ★灭自证** `DraftModeUnlocked = false` ⇒ **拒绝**（证明是活读口，不是装了个常量 true）
- **F5** `NotifyPracticeMatchWon()` ⇒ 解锁
- `finally` 里还原值 + `FakeDraftEvent.ResetInjectionForTest()`（⛔ 不留测试态）

---

## §3 ③ 立账（不改代码 · 写进补丁）

`账/_parts/B89_E11b.tsv`（11 列表头 · **7 行** · `轮次` = `B89-E11b` · 已跑 `check_patch_conflicts.py` ⇒ **本补丁 0 冲突**）：

| # | 原版类 | 方法 | 状态 | 一句话 |
|---:|---|---|---|---|
| 1 | `ILiveOps` | `get_Priority()` | C | 类上有实现（`LiveOpsEvent.cs:385`），**接口没声明** ⇒ 排序只能绕 `GetEventPriorityForMainMenu()` |
| 2 | `ILiveOps` | `get_ShowInMainMenu()` | C | 同上（`LiveOpsEvent.cs:379`）⇒ 主菜单那档必须 downcast |
| 3 | `IPlayEvent` | `get_EventPlayMode()` | C | **整条接口未建**（13 成员，代表行）；代价 = 本批两处反射 + `CurrentSeasonNumber` 卡死 |
| 4 | `GameModes` | `get_PreviousSeasonClassicLeaderboardKey()` | C | 两行代码，判据已就绪 |
| 5 | `GameModes` | `get_PreviousSeasonFastLeaderboardKey()` | C | 读的是 **FastSharedData(+0x88)**，⛔ 别写错表 |
| 6 | `GameModes` | `get_LastPlayedSeason()` | C | 两表取 **Math.Max** |
| 7 | `GameModes` | `get_CurrentSeasonNumber()` | C | **被 `IRankedEvent` 卡住**（双重阻塞） |

> ⚠️ **`L584`/`L585` 两条我**没有**写进补丁**：`B89_R2c.tsv`（两行）与 `B89_R2d.tsv`（`OnMatchWon`）已经占着同样的键，`check_patch_conflicts.py` 会报重复且**按字典序 `R2c`/`R2d` 后合并 ⇒ 我的更新会被盖掉**。⇒ 改动文本放这里，由调度台决定并进谁那份：
> · **584 差异列建议追加**：「🆕 B89-E11b 2026-10-28：**GameModes 半已就绪**（`GameModesHandler.SetResultCount` + 注册表接线 `LiveOpsManager.cs:92`，断言 `GameModesCheck` E3）。仍缺：练习胜局的**调用点**（`PracticeModePopup`/`BattleDriver`，E5b 在写）、`PlayerDataManager.RecordPracticeMatchFinished`（P3）、`enemyMilestone`/`MatchData` 两个派生参数。」
> · **585 差异列建议追加**：「🆕 B89-E11b 2026-10-28：**这一半不落在 `GameModes`**；生产者已备（`GameModesHandler.InstallDraftGate<TS,TD>` / `DraftModeUnlocked` / `NotifyPracticeMatchWon`），但 R2d 的闸是**泛型类 static**、本机**无闭合类型** ⇒ 生产上暂不可注入（待轮抽具体子类）。默认值取「放行」的兜底理由见 `报告/B89_E11b_GameModes接线.md` §2。」

---

## §4 待接 / 没查清

1. **`LiveOpsManager.Instance` 仍无生产调用方**（改前也是 0）。注册表是我接的，但「谁在启动时把单例建起来」这一跳在启动链（`Shell/ShellRuntime.cs` / `PlayerDataManager`，别的代理）。不接这一跳 ⇒ 运行期 `GetHandler<GameModesHandler>()` 仍是 null（只有自检 E 节会因为自己调了 `Instance` 而拿到）。
2. **草稿闸的生产注入点**：等轮抽线的具体子类（`DraftEvent : DraftEventBase<…>`）落地后加一行 `InstallDraftGate<…>()`。
3. **练习首胜的调用点**（L584/585 剩下的那一半）在 `PracticeModePopup.cs` / `BattleDriver.cs`（E5b）——本批不碰。
4. 没查清：`DemoData.canEnterDraftMode` 在**原版**是否还有别的写点（例如服务器下发的 `demoData` 里带初始值 true）——本轮只读了 `PracticeEvent.OnMatchWon` 这一处写点。

---

## §5 typecheck

```
TMPDIR=/tmp/wf_B89E11b* bash d:/4/Unity/工具/typecheck.sh
```
- 修完 ①②③ 那一轮：**运行时 0 / 编辑器 0**（`GameModesHandler.cs` + `LiveOpsManager.cs` + `GameModesCheck.cs` + `LiveOpsSharedData.cs` 全在内）。
- 之后加「草稿闸」那一段，连续三次都被**别人的半成品**挡住：
  - 第 1 次：`Assets/RuleEngine/Core/EffectResolver.cs(9967)` `CS0103 FireMobOrRegimentOnSameSide`（E3b）
  - 第 2、3 次：`Assets/RuleEngine/Core/BattleContext.cs(2350)` `CS0708 MatchTypes.ResolvingExtrinsic 不能在静态类里声明实例成员`（E14）
  - 每次**只有那一条**，且**都不在我的白名单文件里** ⇒ 按简报「报错全部集中在白名单之外 ⇒ 别人的活」处理，⛔ 没去改。
- 说明：csc 一次报全部错误，那几轮**没有任何一条**指向我的四个文件 ⇒ 我的改动本身没有编译错误；但要拿到干净的 0/0 读数，得等 E14 存盘后主对话重跑一次。
- 🆕 **补做了「隔离验证」（不改任何别人的文件）**：运行时那份读数是可信的（csc 一次报全部错误，唯一那条在 `BattleContext.cs`）。
  编辑器程序集因为缺 `WFCheck.dll` 没跑，我拿**上一次干净编译出的 `WFCheck.dll`** 当引用、手工编译了一遍编辑器 rsp
  （副本在 `%TEMP%/wf_e11b_ed/`，⛔ 没碰仓库）：`Editor/GameModesCheck.cs` 报的错**只有** `DraftModeUnlocked` / `InstallDraftGate` / `NotifyPracticeMatchWon` 三个
  「旧 DLL 里还没有」的 CS0117（正是草稿闸那段新增成员），**其余一条都没有** —— 包括 `FakeDraftEvent : DraftEventBase<…>` 的泛型约束、
  `ResetInjectionForTest` / `CanEnterDraft` 的可见性、E 节全部符号。⇒ 结论：我的编辑器文件除「旧 DLL 缺新成员」外**无编译错误**。

---

## §6 断言清单（`Editor/GameModesCheck.cs`，入口 `GameModesCheck.Run`）

62 处 `Ok`/`Eq`：A 20（`DeserializeSharedData` 三支 + 2 灭自证）· B 9（`SetResultCount` 三支 + default 不建格）· C 9（两个 setter + `CombatFinished` 清空）· D 10（`GetActiveEvent` 三道闸 + 排位递归支）· **E 6（注册表接线）** · **F 5（草稿闸两支 + 灭自证）**（含 7 条夹具前提）。
⛔ 没碰宿主的 `pass/fail` 合计口径（按调度台要求，保持每宿主独立打印）。
