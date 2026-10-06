# W15 · `BattleScene.cs`：A394 换断言 + A383 补断言（写手 · 2026-10-15）

> **一句话**：A394 段那条「钉现状」的第 8 条断言按 W10 §五·1 **整块换成**正向断言（0 / 1 / `Shake Earthquake`）；
> A383 那四条路（四扇窗各断 `driver.PlayMode` · `MatchType` 三条派生 · 通道读完就清 · **灭自证**）**一条都没落**
> ⇒ 新开一节 **9d** 补上（330 行 · 22 处 `Check` 站点 ⇒ 运行时 **31 条**断言）。**只改了这一个文件**，没跑 Unity，
> 秒级类型检查 **0 错 / 0 错**（跑 3 次，最后一次在全部改动之后）。
> 判据全部是**原版读数**（VA / RVA / `dump.cs` / 原版 `.c`），⛔ 没有一条拿我们自己的常量当期望值。

---

## 一、摘要（6 行）

1. **A394 第 8 条**（`// ---- ③ 字段那一档…`）→ 换成 W10 §五·1 给的**正向**断言：旁挂里 `modules.0.*` 那些字段
   **真装进模块了**（`cameraShakes` **0** 条 / `manualTriggerCameraShakes` **1** 条 / preset `Shake Earthquake`）。
2. 那条断言的**旧改坏法 ④ 已作废**（它指的那句出声 W10 已经改掉了）⇒ 就地订正成新那一跳（读 `def` 那两行），
   并补 ⑤（生成器键名不带方括号）。
3. **A383 新增一节 9d**（插在 9c 与「10. 战术卡」之间）：四扇窗各一条链 ⇒ 通道 ⇒ `BeginFromDeckLibrary` ⇒
   `driver.PlayMode` + `Ctx.MatchType`；再逐行核那张 **14 项派发表**、通道**读完就清**、以及 **④ 灭自证**。
4. 四扇窗的「那一档」**不是我在断言里写的字面量**：练习窗读它自己的 `PlayMode` 字段、遭遇/排位两扇
   **真调一次它们覆写的 `OnSearchFinished`**（protected ⇒ 反射）、`Practice Deck` 那条读
   `DeckInfoPopup.StartPracticeMatch` 的 **IL**（`ldc.i4.s 12`）。
5. **灭自证**那一条是「**12 张的牌 + 排位模式** ⇒ `PlayMode == Classic 0` **且** `Vars.IsSkirmish == true`」
   —— 把两处一起改回「用 `Vars` 推模式」会红。
6. ⚠️ 三处**如实标**：① 非批处理（编辑器点菜单跑）时会跳过两次 `StartBotBattle`（它会真 `LoadScene`）；
   ② `Practice Deck` 那条只做到**结构**断言（那扇窗要真跑得先有 `WindowsManager`）；
   ③ 没跑 Unity ⇒「断言本身写没写错」仍要等同步点那次 `BattleScene.Run`。

---

## 二、改动清单（`文件:锚点 | 改前 | 改后 | 依据`）

**唯一改动的文件**：`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`

| # | 锚点 | 改前 | 改后 | 依据 |
|---|---|---|---|---|
| 1 | A394 段 `// ---- ③ 字段那一档：旁挂今天只给类名 ⇒ **必须出声**` 那一整块 | `bool emptyTracks = …; bool saidGap = …; Check(emptyTracks && saidGap, …)`（**钉现状**：两条轨道都必须空 + 必须发那句出声）—— W10 落地后**两条判据都翻了**（`manualTriggerCameraShakes.Length` 现在 = 1；那句出声不再发） | **W10 §五·1 原文整块贴**（含上面两行注释）：`var mt = mod.manualTriggerCameraShakes;` + `Check(cameraShakes.Length == 0 && mt.Length == 1 && mt[0].preset == "Shake Earthquake" && 3 个 overwrite == 0 && sustainTime ≈ 0, …)` | `资料/普查产出_1015/W10_A394欠账收口.md` §五·1（那是 W5 设计好、留给主对话换的那条）；期望值 = 原版那颗模块的真值（`数据/游戏数据/animfx_modules.json` 的 `Scenario` 效果 ＋ 原版包 `MonoBehaviour_5320.json`） |
| 2 | A394 段头 `🧨 **改坏法**` 的 ④ | 「删掉 `BuildAnimFxModules` 里那条「只收到类名」的出声 ⇒ 第 6 条红」——**那句出声 W10 已经改掉、那条断言也换了** ⇒ 这条改坏法**指向不存在的东西** | 改成「删掉 `WFSceneModuleScreenShake.Configure` 里读 `def` 的那两行 ⇒ 「旁挂里那些字段真装进模块了」那条红」＋一句「第 8 条 2026-10-15 换过」；并补 ⑤（生成器键名不带方括号 ⇒ 也红） | 铁律 5（**记录与事实不符就地改掉**）；新改坏法来自 W10 那条断言自己的 🧨 文案 |
| 3 | 9c 段收尾（`}` 之后）与「`// ---- 10. 战术卡`」之间 —— 🆕 **新增 9d 整节** | 无（A383 的断言**一条都没有**，W4 白名单里没有自检宿主） | 330 行 / 22 处 `Check` 站点 ⇒ **运行时 31 条**（`Chain` 那 3 条 × 4 扇窗 = 12）；四段：① 四扇窗 · ② `MatchType` · ③ 通道读完就清 · ④ 灭自证 | W4 §四·1 的四条写法；模式号 = `dump.cs:46188` ＋ 四扇窗各自的 `get_EventPlayMode`（VA 0x1808B66B0 = 6 · `StartMatch(0xc,…)` = 12 · `FastModeBaseEvent` = 13 · `RankedV2Event` 0x1804BD440 = 0）；派发表 = 原版 `MatchData..ctor`（RVA 0x736B70） |

**期望值一览**（⛔ 全部来自原版，**不是**我们的枚举名/常量）：

| 入口窗 | `PlayMode` | `MatchType` | 那一档的判据 |
|---|---|---|---|
| 练习窗 `PracticeModePopup` | **6** `OfflinePractice` | **50** `PracticeOffline` | `PracticeEvent.get_EventPlayMode` VA 0x1808B66B0 |
| `Deck info ▸ Practice Deck` | **12** `OwnDeckTraining` | **50** `PracticeOffline` | `DeckInfoPopup__StartPracticeMatch.c` 的 `StartMatch(0xc, …)` |
| 遭遇战窗 `SkirmishEventWindow` | **13** `Skirmish` | **200** `FastMode` | `FastModeBaseEvent.get_EventPlayMode` |
| 排位窗 `RankedEventWindow` | **0** `Classic` | **10** `Ranked` | `RankedV2Event.get_EventPlayMode` VA 0x1804BD440（`33 C0 C3`） |
| （表外）`Battle4Warpforge 14` | —— | **0** `Undefined` **＋出声** | 原版那张表只有 14 项（0~13）⇒ 没判据就不编值（红线） |

---

## 三、每条新断言的**改坏法**（改哪一处它会红）

### A394（换掉的那条）
- 🧨 删掉 `WFSceneModuleScreenShake.Configure` 里读 `def` 的那两行 ⇒ **红**（那正是 W10 补上的那一跳）。
- 🧨 生成器 `pack_module_fields` 把键名写成 `manualTriggerCameraShakes.0.presetSO`（不带方括号）⇒ **也红**
  （`WFModuleDef.CountList` 数的是 `key[n]` 前缀）。
- 🧨 把 `mt[0].preset` 的 `@asset:` 前缀解析删掉（`WFModuleScreenShake.ReadList` 里那一跳）⇒ `preset` 变成
  `@asset:MonoBehaviour:Shake Earthquake` ⇒ **红**。

### A383 · 前提（5 条：阵营 / 两副牌 / 缓存护栏 / 当前选中 / 反射拿得到方法）
- 🧨 卡池里删掉某阵营的防御卡 ⇒ 「又有督军又有防御卡」那条红（两副牌的前提）。
- 🧨 `MakeDeck` 的张数改回写死 30 ⇒ 「经典 30 + 遭遇 12」那条红。
- 🧨 本宿主里**先**碰一次 `CollectionData`（缓存到真存档）⇒ 护栏红（并**因此不调** `StartBotBattle`，见 §五·1）。

### A383 · ① 四扇窗（每扇：声明那一格 + 通道那一格 + 落地 3 条）
| 谁 | 改坏法 |
|---|---|
| 练习窗 | 🧨 把 `PracticeModePopup.PlayMode` 的出厂值改成别的档 ⇒ 「自己声明的那一档 = 6」红；🧨 删掉 `StartBotBattle` 里那句 `SetPendingPlayMode(PlayMode)` ⇒ 通道那条红 |
| `Practice Deck` | 🧨 把 `StartBotBattle` 里那个实参换成**写死**的 `GameMode.OfflinePractice` ⇒ 通道那条红（这条专门咬「模式号跟着**字段**走」）；🧨 把 `DeckInfoPopup.StartPracticeMatch` 那句实参改成别的档 ⇒ **IL 那条**红 |
| 遭遇战窗 / 排位窗 | 🧨 把 `OnSearchFinished` 里 `SetPendingPlayMode(Skirmish/Classic)` 删掉或换档 ⇒ 对应那条红（**先塞了相反的一档**，所以「它没写」与「通道里原来就是那一档」分得开） |
| 四条链的落地 | 🧨 `BeginFromDeckLibrary` 改回「用 `Vars` 推模式」⇒ `PlayMode` 那几条红（遭遇/排位两条的期望正好相反）；🧨 把 `TakePendingPlayMode()` 换成「读但不清」⇒ 每次都红（**通道清空**那一条） |

### A383 · ② `MatchType`
- 🧨 `MatchTypes.For` 里改错/删掉 14 行中任一行 ⇒ 「逐行对上原版」那条红。
- 🧨 把 `default` 那一支改成**悄悄** `return MatchType.Undefined;`（不 `LogWarning`）⇒ 「不编值 + 出声」那条红。

### A383 · ③ 通道读完就清
- 🧨 删掉 `TakePendingPlayMode` 里那句 `_pendingPlayMode = null;` ⇒ 第二次照样读出 `Replay(10)` ⇒ 红。

### A383 · ④ **灭自证**（3 条）
- 🧨 把 `BeginFromDeckLibrary` 改回「用 `Vars` 推模式」⇒ `PlayMode` 会变成 `Skirmish(13)` ⇒ 第 1 条红。
- 🧨 反过来让 `Vars` 跟着 `PlayMode` 走（或让 `PickSavedDeck` 丢掉牌自己带的模式）⇒ 第 2 条红
  （`Vars.IsSkirmish` / `deckSize == 12`）。
- 🧨 把 `Ctx.MatchType` 改成「按 `Vars` 派生」⇒ 第 3 条红（期望 `Ranked(10)`，而 `Vars` 是遭遇那套）。
- 🔑 这三条**结构上不可能同时满足**：①要求模式跟着入口窗、②要求参数跟着牌、③要求派生生跟着模式号。

---

## 四、离线验证：没跑 Unity，但把**关键那一跳**照抄核过了

1. **IL 那条断言的前提是实测的**（不是推的）：用秒级类型检查**刚编出来的** `WFCheck.dll`
   （`/tmp/wf_w15/wfcheck/WFCheck.dll`）跑 Unity 自带的反汇编器
   `MonoBleedingEdge/lib/mono/4.5/ikdasm.exe`，`DeckInfoPopup::StartPracticeMatch` 的尾部逐字是：

   ```
   IL_008c:  ldarg.0
   IL_008d:  call  instance class CardPresentation.WindowsManager CardPresentation.GameWindow::get_Manager()
   IL_0092:  ldarg.1
   IL_0093:  ldarg.2
   IL_0094:  ldc.i4.s   12
   IL_0096:  call  class CardPresentation.PracticeModePopup
                     CardPresentation.PracticeModePopup::StartPracticeMatch(…, class RuleEngine.GameMode)
   ```
   ⇒ `ldc.i4.s 12` **紧挨着** `call`（中间没有别的指令）。
2. **token 形状也实测了**：在同一个 DLL 里按字节找 `1F 0C 28`（`ldc.i4.s 12` + `call`），
   命中的那次操作数 = **`0x060012B0`**（**高字节 0x06 = MethodDef**）—— 与
   `typeof(PracticeModePopup).GetMethod("StartPracticeMatch", Static|Public).MetadataToken` 同构
   （同模块的方法，IL 里就是 MethodDef）⇒ 断言里那 4 字节比较成立。
   ⚠️ 全库只有**一处**这个组合（另一处命中是别的指令流里的偶然字节，token 高字节 `0x2B` ⇒ 被 token 比较挡掉）。
3. **A394 那条换上的期望值与数据核对过**：`Resources/EnvBlendables.json` 里
   `modules.0.manualTriggerCameraShakes[0].*` 共 16 条键 —— `presetSO = @asset:MonoBehaviour:Shake Earthquake`、
   其余 15 条全是 `0.0`（`cameraShakes` 一条键都没有）⇒ 断言里的 `0 / 1 / Shake Earthquake` 与
   `overwrite* == 0` / `sustainTime ≈ 0` 逐项对得上；`ReadList` 会把 `@asset:` 前缀剥成裸名（`WFModuleScreenShake.cs:252-256`）。
4. **类型检查**：`TMPDIR=/tmp/wf_w15 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 · 编辑器 0 错**。
   ⚠️ 如实记：第一次跑出了 **3 条 `CS0104`**（`MatchType` 在 `RuleEngine.MatchType` 与 **`System.IO.MatchType`**
   之间有歧义 —— 本文件 `using System.IO`）⇒ 写全 `RuleEngine.MatchType` 之后归零。

---

## 五、没做完的 / 做不了的（如实标）

1. ⚠️ **非批处理时两格跳过**（**不是遗漏，是闸**）：`PracticeModePopup.StartBotBattle` 末句是
   `LoadScene`（`Shell/PracticeModePopup.cs:1723`），只有**批处理**下它在那一句之前就 `return`（`:1722`）。
   本宿主从菜单里点（`Tools/CardPresentation/对战自检`）跑时若调它，会把自检现场整个换掉 ⇒
   加 `canBot = Application.isBatchMode` 闸：**跳过那两格并打一条 Warning 说明「没验」**（⛔ 不假装通过）。
   批处理（= 全工程自检的常规跑法）下**照常验**。
2. ⚠️ **`Practice Deck` 那条只到「结构」**：那一档写死在 `DeckInfoPopup.StartPracticeMatch` 的实参里，
   要真跑那一扇窗得先有 `WindowsManager` + 一副能用的牌（会把**整扇练习窗**建出来）——本宿主不干这个。
   ⇒ ①c 用 IL 断言「那句实参是 12」，①b 用**真开局**覆盖「模式号跟着字段走」这一半。
   ⏭ **真行为那一格**建议落在**壳线**：`Editor/CollectionScene.cs:2521` 已经拿得到
   `PracticeModePopup.LastOpened`（`DeckInfoPopup` 那条链的回调产物）⇒ 在那儿加一句
   `prac.PlayMode == OwnDeckTraining` 就是**端到端**了（那一处不在我的白名单）。
3. ⛔ **没跑 Unity**（本轮规矩）⇒ 本节所有断言**从没被执行过**；「断言本身写没写错」要等同步点那次
   `BattleScene.Run`（判绿红按**行首标记**数，别用 `grep -c ✗`）。
4. ⛔ **`BattleDriver.cs:2106` 那条警告的措辞**（W4 §五·2 点的：「卡组张数（N）和**本局模式**对不上」——
   它判的其实是 `_vars`）**没改**：那不在我的文件白名单里，且属措辞/另一笔账。
5. **没验到的两态**（如实标，不假装覆盖）：联机那两支（`NetPendingBattle.PlayMode`）与**回放**那支
   —— W4 说它们随开局包带模式号，那两条链在**别的宿主**（`NetBattleTest.Run` / 录像那几节）。

---

## 六、顺手发现（⛔ 一个都没改，除了我自己那节里的两处规避）

1. 🔴 **`CollectionData` 有一份 `static _lib` 缓存，而且**「改存档路径」对它无效**（`Shell/CollectionData.cs:38-41`）：
   它缓存的是**第一次被碰时**那个路径下的库。若本宿主将来有谁**先**碰过它（缓存到**真存档**），
   后面任何 `CollectionData.Select(...)` 都会**写玩家的真卡组**（`Select` 末尾会 `Lib.Save()`）。
   ⇒ 我在 9d 里做了两件事：① 改「当前选中」**不经过它**（直接 `DeckLibrary.Load().Select(n).Save()`）；
   ② 两次 `StartBotBattle`（它内部会 `CollectionData.Select`）挂在 `cdSafe = CollectionData.DeckCount() == 2`
   这面旗上 —— **宁可那一格不验，也不写错存档**。建议：给 `CollectionData` 补一个「按当前 `OverridePath`
   重取」的口（或让 `Lib` 缓存 key 上路径）——那不在我的白名单。
2. ⚠️ **`MatchType` 这个名字在本文件里是个坑**：本文件 `using System.IO;`，而 `System.IO.MatchType` 也在
   （NET 7+）⇒ `MatchType x = …;` 直接 **CS0104 歧义**。写 `RuleEngine.MatchType` 或在 9d 之外新增任何
   `MatchType` 局部变量的人都会踩（我踩了一次，类型检查当场报出来）。
3. `LiveOpsEventWindow.OnSearchFinished` 是 `protected virtual`（子类覆写）——
   **`protected` 成员从别的类里只能走反射**：本文件 `:3078` 读 TMP 私有字段用的也是同一招（先例）。
   9d 里那两处调用**先塞相反的一档**再调，否则「它没写」与「通道里原来就是那档」分不开。
4. `WFCheck.dll` 那条**离线反汇编路子**值得留档（本轮第一次用）：Unity 自带的
   `MonoBleedingEdge/lib/mono/4.5/ikdasm.exe` + `bin/mono.exe` 能直接反汇编秒级类型检查编出来的 DLL
   —— 「某一句实参是什么 / 某条指令在不在」这类**结构问题**以后再遇到，**不用跑 Unity 就能查证**
   （另一条路：按字节在 DLL 里找指令模式 + 读 token 高字节判 MethodDef/MemberRef）。

---

## 七、行尾 / 自检 / 落盘

- **行尾**（二进制数过，改前 → 改后）：`Editor/BattleScene.cs` **13886/13886 → 14229/14229**（**纯 CRLF**，两个数相等
  ⇒ 一个字节都没被翻；全程 Edit 工具，**没用 `sed -i`**）。
- **秒级类型检查**：`TMPDIR=/tmp/wf_w15 bash d:/4/Unity/工具/typecheck.sh` → 运行时 **0** / 编辑器 **0**（跑 3 次）。
- **⛔ 没跑 Unity** · **没动 git** · **没改正本**（`项目任务.md` / `CLAUDE.md`）· **没碰别的 `.cs`**
  —— 本件只改 `Editor/BattleScene.cs` 一个文件（几处探针/核对脚本都写在 `/tmp/wf_w15/` 下，**没落进工程**）。
