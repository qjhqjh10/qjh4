# B94_DG3 — `ShellScene.Run` 崩溃 + `SettingsScene.Run` 7 红 诊断报告

> 只读诊断（本会话未改任何源码 / 账 / 正本）。日期 2026-10-29。
> 输入日志：`_tmp_view/shell.log`（03:17）· `_tmp_view/settings.log`（03:20）· 对照基线 `_tmp_view/b91s/{shell,settings}.log`（2026-10-28 18:24）。

**一句话结论**

| 宿主 | 现象 | 定性 |
|---|---|---|
| `ShellScene.Run` | `AmbiguousMatchException` ⇒ `executeMethod ... threw exception`（退出码 1） | **α 自检件写错**（`:224` 用裸名 `GetMethod`）+ 这一族**本仓库里第一次真被跑到** |
| `SettingsScene.Run` | **不是崩溃**：`通过 1610 · 失败 7` ⇒ `EditorApplication.Exit(1)` | 前 **6 条 = γ 本批回归**（新增的 `win.Close()` 没配对重开）；第 **7 条 = α 自检件前提不成立** |

---

## §一 两个宿主的崩溃点（文件:行 + 那一行代码）

### ① `ShellScene.Run` —— 真异常退出

**崩溃行**：`Unity/MyGame/Assets/CardPresentation/Editor/ShellScene_B93D4SelfCheck.cs:224`

```csharp
var g = t.GetMethod("CollectItemReward");        // ← 抛 AmbiguousMatchException（不是 NRE）
C(g != null, "`CollectItemReward` 名字在（泛型版）");
```

**它不是「找不到方法返回 null」，是「找到两个、无法裁决」**：

| 方法 | 位置 |
|---|---|
| `public static void CollectItemReward<T>(T reference, string instanceID, int quantity) where T : class` | `Shell/Inventory.cs:1995` |
| `public static void CollectItemReward(RewardInfo reward)` | `Shell/Inventory.cs:2016` |

`Type.GetMethod(string)` 在**同名多载**时抛 `AmbiguousMatchException`（.NET 语义；本机日志逐字印证）。

**日志证据**（`shell.log`）：
- `:237354` `AmbiguousMatchException: Ambiguous match found.` → 栈 `System.Type.GetMethod(String)` → `CardPresentation.B93D4SelfCheck.RewardServiceEntries(...) [0x00083] ... ShellScene_B93D4SelfCheck.cs:224` → `ShellScene.RunCore() ... ShellScene.cs:11020`
- `:237366` `executeMethod method ShellScene.Run threw exception.` / 退出码 1
- 崩溃前最后两条 ✓：`:221` `CollectItemReward(RewardInfo)` 已建 · `:222` 「泛型入口不是特化重载」——**两条都过**，`:224` 才炸。

**后果（比红严重）**：`B93D4SelfCheck.Run` 在 `ShellScene.cs:11020` 被调用，异常**直接冲出 `RunCore`** ⇒ 其后**全部没跑**：
`B93D4SelfCheck` 自己的 `MissionToast` / `LeaderboardTotalStats` / `EventBadge(.Controller).SelfCheck`、以及 `B94S2SelfCheck`、`LiveOpsB93N2SelfCheck`、`B93S3Assert`、`B94Y6_Assert`、`B94B4Assert` / `B94B6Assert` / `B94B17Assert`、`=== 合计 ===` 行、失败重列、`EditorApplication.Exit(...)` 全部没执行。
判据：`grep -c "=== 合计" shell.log` = **0** · `grep -c "✗" shell.log` = **0**。

### ② `SettingsScene.Run` —— **不是崩溃**，是 7 条断言红 + 设计内退出

- `settings.log:136265`：`[Settings] ===== 通过 1610 · 失败 7 =====`
- 紧随 7 条 `失败重列：…`（`:136277/136289/136301/136313/136325/136337/136349`）
- `SettingsScene.cs:5626`：`if (Application.isBatchMode) EditorApplication.Exit(_sink.Fail == 0 ? 0 : 1);` ⇒ **退出码 1 是设计内的**（Fail > 0）。
- `abort_threads: Failed aborting id: …` / `debugger-agent: Unable to listen on 3628` / `##utp:{"type":"MemoryLeaks"…}` = **`EditorApplication.Exit` 之后的 mono 关闭噪声**（`Shut down.` / `Cleanup mono` 已在其前），**不是段错误、不是卡死超时**。
- `grep -c "Scripts have compiler errors"` = **0** ⇒ 不是编译失败；全程只有 1 条 `Licensing::Module Error: Access token`（启动噪声）。

7 条红的精确位置（栈里的 `SettingsScene.cs:NNN` / `B94W2_Assert.cs:NNN`）：

| # | 断言位置 | 文案关键字 | 根因 |
|---|---|---|---|
| 1 | `SettingsScene.cs:4351`（`CheckRectPx`） | A208 IP 输入框渲出来的矩形 | 窗被关着 |
| 2 | `SettingsScene.cs:4357`（`CheckRectPx`） | A208 密码输入框 | 窗被关着 |
| 3 | `SettingsScene.cs:4374` 附近（`RectOf`） | 输入框左沿 vs 标签左沿（框 0.00） | 窗被关着 |
| 4 | `SettingsScene.cs:4438` 附近（`CheckRectS`） | 【Test Public IP】落在 Save/Check 右边 | 窗被关着 |
| 5 | `SettingsScene.cs:4458`（`CheckRectPx`） | 客机块 `IP Field` 也过 `Screen()` | 窗被关着 |
| 6 | `MenuDraw.CheckAbsorbRule`（`Shell/MenuDraw.cs`，经 `SettingsScene.cs:4482`） | 面板矩形内找得到一个点、命中是**本窗自己的**吸收层 | 窗被关着（`PointerLayer.ButtonAt` 命不中） |
| 7 | **`B94W2_Assert.cs:250`** | 「关窗才生效」脏位（期望 True 实得 False） | **自检前提不成立**（当前档本来就是 VeryLow） |

---

## §二 归因（α / β / γ / δ）+ 证据链

### A. ShellScene ⇒ **α 自检件写错（并附带一条：它此前从未被真跑过）**

1. **错在哪**：`:224` 想验「泛型版存在」，却用了**裸名** `GetMethod`。同名的 `CollectItemReward(RewardInfo)` 与 `CollectItemReward<T>(…)` 同时在 ⇒ 必抛 `AmbiguousMatchException`。断言**意图是对的**（判据 `:222` 已确认没有 `(ICurrency,string,int)` 特化重载），只是**查法**错。
2. **不是 β**：`RewardService` 的两个入口都在、签名都对（`get` 型断言 `:221`/`:222` 都 ✓），实现没缺陷。
3. **不是本批语义回归**：`Inventory.cs` 与 `ShellScene_B93D4SelfCheck.cs` **本会话零改动**（`git status --porcelain` 里两者都不在；`git log -- <两个文件>` 最后一次提交都是 `6877d563` = HEAD）⇒ 这是 **HEAD 上就存在的坏点**，只是从没被跑到。
4. **「从没被跑到」的证据**：`_tmp_view/b91s/shell.log`（2026-10-28 18:24，2129 通过 / 0 失败）里 `grep -c "B93D4SelfCheck 跑"` = **0** —— 那次 `ShellScene` 根本没有这条调用。D4 自检与这两个重载是**同一批**落进 HEAD 的（`git log` 都是 `6877d563`），所以**第一次真跑 = 本会话这次**，第一次跑就炸。
5. ⚠️ **与 HEAD 提交信息的矛盾（顺手发现，建议记账）**：`6877d563` 的标题写着「同步点五闸全绿 + **20 宿主 0 失败**」，而**该提交里的这两份文件本身就是这次必然崩溃的版本** ⇒ 那次同步点**不可能**跑过 `ShellScene.Run` 之后还绿。推论：D4 自检是**接线/合并在 shell 那次跑之后**才落进去的，`0 失败` 这句话不覆盖它。（不是本报告要修的东西，但调度台的「20 宿主」数字要按这个更正。）

### B. SettingsScene 前 6 条 ⇒ **γ 本批回归：`SettingsScene.cs:3145` 新加的 `win.Close()` 没有配对的重开**

**因果链（逐环都有证据）**：

1. **新代码**：`SettingsScene.cs:3145` `win.Close();  // = 原版 SettingsMenu.Close() 那一刻`
   `git diff` 确认这一行（连同随后的「关窗之后…」两条断言）是**本批 W2 新增**；旧版 ⑥ 段是「换档 ⇒ 当场应用」，**不关窗**。
2. **`Close()` 会把窗根置灰**：`Shell/GameWindow.cs:804` `gameObject.SetActive(false);`（= 原版 `CloseWindowCO` 里 `Hide()` 的尾句；非 `baseMenu` 那一支）⇒ 窗整棵子树 `activeInHierarchy == false`。
3. **之后再没开回来**：全文件只有 `:2607` / `:2782` / `:3109` / `:3124` / `:5708` 五处开窗；`:3145` 之后、到 `:5708`（`Setup()` 那条）之前**没有**开窗调用。也就是说 **`:3145` 之后一直到 `RunCore` 结束，窗都是关着的**。
4. **日志证明「从这一刻起没有任何一次成功的矩形测量」**：
   - `grep -n "渲出来 = \[" settings.log` 共 **11 条**，**全部 < 118748**；
   - `grep "（应为 " settings.log` 里 **> 118748 的 = 0 条**；
   - `settings.log:118748` 正是 `SettingsWindow.Close()`（`SettingsScene.cs:3145`）打出来的那条 `当前画质档不允许超采样 ⇒ 已把 Use super sampling 关掉`。
5. **失败的 5 条矩形断言的共同闸**：`RectOf` = `MenuDraw.UnionQuadRectPx(t, MenuDraw.QuadGate.InHierarchy, true, …)`（`SettingsScene.cs:154-167`），注释里明写「返回 false = 一个 active 的 `ImageQuad` 都没有」；失败文案自带那句「（没有 active 的 `ImageQuad` ⇒ 量不到渲染矩形）」。**窗灰 ⇒ 一个 active quad 都没有**，逐字吻合。
6. **第 6 条（吸收层）是同一根的另一个面**：`MenuDraw.CheckAbsorbRule` 用 `PointerLayer.Instance.ButtonAt(tx,ty)` 找「本窗自己的吸收层」；窗灰 ⇒ 命中表里没有它的候选 ⇒ `found = false`。
   而同一节里**不吃激活闸**的四条**全 ✓**：`吸收层节点在`(123470) · `渲染矩形左沿`(123504，走 `QuadRectPx(q)` 直接读 `ImageQuad`) · `档 = 内容命中档−1`(123576) · `没报过档位告警`(123610) —— 这正是「**组件读得到、激活闸过不去**」的形状，反证「树没坏、只是被关着」。
7. **不是昨天就有**：同一宿主昨天的基线 = `_tmp_view/b91s/settings.log`（2026-10-28 18:24）**1567 通过 / 0 失败**；今天 **1610 通过 / 7 失败**（1610 − 43 条新增的 `B94W2Assert` = 1567，条数也对得上）⇒ **本批引入**。
8. **不是 β**：`MenuInputField.Create`（`Shell/SettingsWindow.cs:6573+`）路径没动、也没打「底板图取不到」的告警（`grep -c "输入框底板图" settings.log` = 0）⇒ 底板是真的建出来了，只是父链灰。
9. **不是 δ**：日志走到 `===== 通过 … =====` 与失败重列，退出是 `EditorApplication.Exit(1)`，没有超时/段错误特征。
10. **连带损失（修好后条数会变）**：`CheckAbsorbRule` 里 `if (!found) return;`（`Shell/MenuDraw.cs`，该函数第 ⑦ 段之后）⇒ 其后的「（前提）这一刻窗是开着的」/「点面板 ⇒ 窗不关」/「点面板外 ⇒ 关窗」等约 **6–7 条一次都没跑**。⚠️ 其中「（前提）窗是开着的」本条**本该红却因为提前返回而没报** —— 假如把它挪到 `if (!found) return;` 之前，这次会**再红一条**（这说明该函数对「窗没开」这种前提失效的自我诊断顺序还可以更硬，但**不属本次要改的范围**）。

### C. SettingsScene 第 7 条（`B94W2_Assert.cs:250`）⇒ **α 自检件前提不成立（假红）**

1. **断言**：选「第 0 行」后要求 `SettingsWindow.ApplyGraphicsChangesOnClose == true`。
2. **原版语义**（判据 = `d:/2/tools/decomp_full/GraphicsTab__GraphicsQualityDropdownChange.c`，逐字）：
   ```c
   if (*(int *)(*(longlong *)(DAT_18427be00 + 0xb8) + 0x120) != iVar1) {   // 与当前 graphicsQuality 不同
     if (*(longlong *)(param_1 + 0x28) == 0) goto LAB_...;
     *(undefined1 *)(*(longlong *)(param_1 + 0x28) + 0xc0) = 1;             // 才置脏位
   }
   *(int *)(… + 0x120) = iVar1;                                             // 无论如何都写值
   ```
   ⇒ **只在「选中的档 ≠ 当前档」时**置脏位。我们的实现（`Shell/SettingsWindow.cs` 的 `ChooseQualityOption`）逐字照抄，**正确**。
3. **那一刻当前档本来就是 VeryLow**：日志 `settings.log:134557` 的警告原句写着
   `Graphics quality changed in settings to VeryLow（… ApplyGraphicsChangesOnClose = False）`
   —— 同一条日志的下一行（`:134590` 前）就是 value 断言 ✓（`graphicsQuality == 0`）。⇒ 值没变、脏位当然不置。
4. **为什么当前是 VeryLow**：`SettingsScene.RunCore` 初始化在 `:629` 把 `GameStaticData.graphicsQuality` 设成 `VeryLow`，之后各节逐段放回 `VeryLow`；`B94W2Assert.Run(_sink, win)` 在 `:5504` 调用，进来时就是 `VeryLow`。
5. 定性：**β 不成立**（实现对原版）、**γ 不成立**（是新断言自己的前提没安排）。

---

## §三 最小修复方案（逐条；本报告不改代码）

### 修 1 —— `Editor/ShellScene_B93D4SelfCheck.cs:224`（α；**零行数变化**）

把**那一行**（`:224`）换成按「名字 + 是否泛型定义」过滤（**行数不变** ⇒ 不碰账落点；该文件在 `资料/完全复刻/账/*.tsv` 里**零引用**，即使加行也安全）：

```csharp
            var g = System.Array.Find(t.GetMethods(), m => m.Name == "CollectItemReward" && m.IsGenericMethodDefinition);
```

- `Type.GetMethods()` 的默认集合 = Public|Instance|Static，与 `GetMethod(string)` 默认同口径；**不需要** `using System.Reflection`。
- `:225` 的 `C(g != null, …)` 一个字不用动。
- 可选加严（不改也行）：把文案改成「`CollectItemReward` 的**泛型定义**在」，避免下次又被读成「裸名在」。

### 修 2 —— `Editor/SettingsScene.cs:3156` 追加配对重开（γ；**零行数变化**）

`:3155-3156` 现在是：

```csharp
                GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.VeryLow;
                win.RebuildGfxRows();
```

改成（**同一行**追加，行数不变 —— 该文件被账按行号引用 8 处，加行会触发 `check_ledger_points` 漂移）：

```csharp
                GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.VeryLow;
                win.RebuildGfxRows(); win.Manager.OpenWindow(win);   // 🆕 ⑥ 段在 :3145 真把窗关掉了（新加的「关窗才生效」那条）⇒ 必须开回来：后面各节（联机页 / A208 矩形 / 吸收层）都在【开着】的窗上量，否则 activeInHierarchy 全假。
```

- 形状照本文件既有先例：`:3104 Close()` → `:3109 OpenWindow(win)`、`:3115 Close()` → `:3124 OpenWindow(win)`（**同一个函数里已经这么配对两次**，这里是第三次漏了配对）。
- `OpenWindow` → `Open()` = `Build(); OpenTab(Current);`（`Shell/SettingsWindow.cs` 的 `Open()`）⇒ 树会整棵重建；后面各节本来就是**现取** `Bar(root)` / `FindChild(root, …)`（`:3145` 之后没有跨重建的缓存），所以不需要别的改动。
- ⚠️ 不要改成「把 `:3145` 的 `win.Close()` 删掉」—— 那两条「关窗之后才清开关/清脏位」是**本批要验的原版语义**，删了就丢覆盖。

### 修 3 —— `Editor/B94W2_Assert.cs`（α；新文件、无账落点，+1~2 行安全）

在 `:218` `win.OpenTab(SettingsTab.Graphics);` **之前**注入一个「当前档 ≠ VeryLow」的前提（并在 `:250` 那条断言前加一条**硬前提**，不要写成 `if (前提) Check(...)`）：

```csharp
            // 🔴 前提：原版 `GraphicsQualityDropdownChange.c:45` 只在「选中档 ≠ 当前档」时置脏位
            //    ⇒ 不先换一个值，选第 0 行（VeryLow）等于「没变」，脏位当然不置（= 假红）。
            GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.High;
            MenuCheck.True(s, (int)GameStaticData.graphicsQuality != 0,
                "（前提）进本轮时当前档 ≠ VeryLow（否则选第 0 行不会置脏位 ⇒ 下面那条就成了假红）");
```

- `ChooseDropdown` 没有「同 index 早退」的闸（`Shell/SettingsWindow.cs` 的 `ChooseDropdown`：`HideDropdown → box.Choose(i) → RefreshDropdownRows`），所以点第 0 行一定会走到 `ChooseQualityOption(0)`；`High → VeryLow` 是**真的变了** ⇒ 脏位置上、caption 换 `Loc.T("Settings/Graphics/VeryLow")`、`!SuperSampling.RowVisible` 三条**原样成立**，`:247/:250/:253` 三条断言一个字都不用改。
- `finally` 里已有 `GameStaticData.graphicsQuality = qWas;` + `ApplyGraphicsChangesOnClose = dirtyWas` + `RebuildGfxRows()` ⇒ 注入不会漏出去。
- 备选（不注入）：改成点「`(int)qWas + 1) % 4` 那一行」，同时把 value / caption 两条的期望值跟着换 —— 但那样要改 3 条文案，**不如注入**。

### 建议的验证顺序（由调度台执行，本代理不跑 Unity）

1. `bash 工具/typecheck.sh`（秒级，改完 `.cs` 立刻跑）。
2. `ShellScene.Run` 单跑一次 —— 期望：不再抛异常、打出 `B93D4SelfCheck 跑 N 条断言` 与 `=== 合计 ===`；**若其后冒出新红，那才是本批的其它问题**（本报告只能证明「崩溃点在此」，证明不了「其后全绿」）。
3. `SettingsScene.Run` 单跑一次 —— 期望 `失败 0`，且**条数会多于 1567**（`CheckAbsorbRule` 不再提前返回，补跑约 6–7 条 + `B94W2Assert` 的 43 条）⇒ **要按新条数记基线**，别再拿 1567/1610 当历史对照。
4. 账闸：`check_ledger_points` 应无漂移（两处修复都是**零行数**）。⚠️ `check_ledger_points --fix` 的第一遍输出是修前快照，要复跑一次才是真数字。

---

## §四 判不了的（差什么）

1. **修好 ShellScene 之后还剩几条红 —— 判不了。** 崩溃点在宿主**尾段**（`ShellScene.cs:11020`），其后约 6 个自检族 + 合计**一次没跑**，没有任何基线可比；只能复跑才知道。同样判不了「`B93D4SelfCheck` 里 `MissionToast` / `LeaderboardTotalStats` / `EventBadge.SelfCheck` 是否本来就有红」——它们也是被这次异常挡住的。
2. **HEAD 提交信息「20 宿主 0 失败」与本次崩溃的矛盾 —— 只能记录，不能还原过程。** 我这里能证明的是「HEAD 里这两份文件就是会崩的版本」（`git log` 两者都停在 `6877d563`、内容与 HEAD 一致），**无法**证明那次同步点到底是「没跑 shell」还是「跑在 D4 接线之前」。
3. **SettingsScene 修好后的准确条数 —— 判不了**（受第 1 条同族的「提前返回补跑」影响），需重跑取值。
4. **第 3 条矩形断言（输入框左沿 vs 标签左沿）在窗开着时是否本来就绿 —— 判不了**：它今天 `okBox == false`（`fx1` 停在 0.00）；昨天的 `b91s/settings.log` 是 0 失败，**按推论应该是绿的**，但那是 W2 改动之前的代码（`Screen()` 那一路没动），若要 100% 确认需重跑。
5. **本次全部结论都是「静态读代码 + 日志证据」的推断**，未经「修复后复跑」证实（红线：本代理不跑 Unity）。
