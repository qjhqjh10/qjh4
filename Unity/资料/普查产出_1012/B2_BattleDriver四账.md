# B2 · `Battle/BattleDriver.cs` 四账（A293 + A368 + A381 + A382）· 2026-10-12

> **白名单**：只改 `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（本件 **+193 / −58**，8616 → 8751 行）。
> ⛔ 没动 git · ⛔ 没跑 Unity 自检（本轮口径）· ⛔ 没改正本 · ⛔ 没碰 `Editor/BattleScene.cs`（另一个写手占着）。
> **行号一律 = 本件改完之后的现读值**（A 表行号已普遍漂移，见 `S2_战斗侧_开账现核.md` §二.1）。

## 一、结论（四账各一句）

| 账 | 结论 | 改动落点 |
|---|---|---|
| **A381** | ✅ **要做的那一件**：结算那一整块**加了回放闸**（`_replaySession`）—— 放录像不再写第二条对局记录 / 不再推日常与骷髅 / 不再收尾录像 / 不再弹结算面板与开门视频。**闸是「整场回放期间」的**，不是「灌动作那一瞬」的（清早了下一帧就复发） | `BattleDriver.cs:471`（置真）· `:769`（字段）· `:8250-8259`（闸）· `:1846`（`Begin` 清零） |
| **A382** | ✅ **要做的那一件**：那个「静默全跳过」的耦合**解除**了 —— 结算块**不再挂 `_endPanel != null`**，闩换成显式字段 `_settled`（一局一张账）；面板真取不到时**出声**（`LogError`）且**账照记** | `BattleDriver.cs:759`（字段）· `:8250-8279`（分家）· `:1846`（`Begin` 清零） |
| **A368** | ✅ **真缺陷那一半**（`:1214/1215`）修了：兵线**抽成一处判据** `TryMinionLines`，**带 3D 支**（`ArenaSlots.Position(...).z`），`MinionLines` 钩子与 `PositionParticleColliders` **共用它**。A 表写的另一半（`:1465/1466`）**本来就对**，一行没动它的语义 | `BattleDriver.cs:1496-1538`（新方法）· `:1246`（钩子改转发）· `:1552-1555`（消费方共用） |
| **A293** | ✅ 最后一处同源错记（1 行注释）照 `丁` 报告 §② 的现成口径改对（原版 13/13 场都有 `ColorLookup`，那 6 场指的是**共享的 `LUT Normal`**，不是「空的」） | `BattleDriver.cs:1373-1387` |

## 二、改动清单（文件:行号，每处一句为什么）

| # | 行号（改后） | 为什么 |
|---|---|---|
| 1 | `BattleDriver.cs:462-471` | `PlayReplay` 里灌动作**之前**置 `_replaySession = true`（放在 `BeginFromPendingCore` **之后**：那里面走 `Begin`，会清零这一格） |
| 2 | `:752-769` | 三个新字段：`_settled`（A382 的闩）· `_settleCount`（自检计数）· `_replaySession`（A381 的闸），各带判据注释 |
| 3 | `:805-811` | 三个自检口：`Settled` / `SettleCount` / `ReplaySession`（断言用，见 §三之二） |
| 4 | `:1238-1246` | `WFModuleScaleByTarget.MinionLines` 由**写死 2D 的 lambda** 改成**转发 `TryMinionLines`**（A368 真缺陷那一处） |
| 5 | `:1373-1387` | A293：那句「原版静态 LUT 也是空的 6 场之一」就地订正（保留更正痕迹 + 判据指针） |
| 6 | `:1496-1538` | 新增 `TryMinionLines(out, out)`：**兵线判据只此一处**（3D 支 / 2D 兜底支），A368 |
| 7 | `:1540-1550` | 新增自检口 `MinionLinesForTest(out, out)`：问**模块实际用的那个委托**（不重写判据） |
| 8 | `:1552-1555` | `PositionParticleColliders` 改成走同一处判据（**行为逐字不变**：原来那两支的条件与表达式原样搬进 `TryMinionLines`） |
| 9 | `:1836-1846` | `Begin()` 里清 `_settled` / `_replaySession`（本局的账不跨局；`_settleCount` **不清**，它是累计计数） |
| 10 | `:4578-4586` | `Update()` 那处 `if (Ctx.IsOver)` 补指针注释（它不是结算；账的落点在 `UpdateHud`）—— 普查 §二.8 点的两处之一 |
| 11 | `:8235-8315` | 结算那一整块：闩换 `_settled`（A382）+ 回放闸（A381）+ 面板 null 时出声，**账与面板分家** |

## 三、每条账：改了什么 · 断言（改坏了会不会红）

### 三之一 · 改了什么（逐账）

**A381**（回放不再结账）
- 闸的位置：`UpdateHud()` 结算块（`:8250`）。**`Update()` 那两处 `if (Ctx.IsOver)` 都不记任何账** —— `:4578` 那处只是 `UpdateHud(); …R 键…; return;`，账的**唯一落点**仍是 `UpdateHud`（grep 实数：`DailyData.OnBattleEnd` / `RecordBattleLog` / `RecFinish` 三个调用点**各只有一处**，全在这个块里）。
- 为什么闸要「整场留着」：`PlayReplay` 是**一口气**把动作灌完的（`Ctx.IsOver` 在那趟结束时就 true），画面靠 `_timeline` 慢慢演 ⇒ `finally` 里清掉 = 下一帧 `Update()` 就把账又记一遍。
- 清掉的**唯一**入口 = `Begin()`（新开一局）。回放局按 `R` 走 `Restart()` → `Begin()` ⇒ 新那一局是正常对局、账照记。
- 回放那一趟**也出声**（红线：不静默）——`Debug.Log("[Replay] 这一局的结算**整块跳过**…")`，且 `_settled = true` 保证一局只打一次。

**A382**（解除静默耦合）
- 老闩 `_endPanel != null && !_endPanel.Visible` 的**两层病**：① 面板为 null ⇒ 四件事（面板 / 日常 / 对局记录 / 录像收尾）**全不做、且没人出声**；② 「可见性」根本不是闩（它靠的是新一局一开局 `UpdateHud` 的 `else` 支顺手 `Hide()` 这个副作用）。
- 现在：`if (!_settled) { _settled = true; … }` —— **账与面板分家**，面板 null 时 `Debug.LogError` 出声、**三条账照记**。
- ⚠️ **行为差别只有一处**（说清）：若「上一局的面板还开着」时又结束一局 —— 老代码那一刻**跳过结算**（第二笔账静默丢 / 或面板停在旧内容），新代码**正常记一笔**。自检里到不了这一格：`Begin()` 末尾**必调** `RefreshAll(); UpdateHud();`（`:2049-2050`），而那时 `Ctx.IsOver == false` ⇒ `else` 支已经把面板 `Hide()` 掉（§12 投降那一节、A147/9b-3 那几节都因此**两版同结果**）。

**A368**（兵线量纲）
- 消费方 `WFModuleScaleByTarget.ChangeShapeAngle`（`WarpforgeVFX/Runtime/WFModuleScaleByTarget.cs:206-208`）算的是
  `lineD = |pLine − eLine|` **÷** `cardD = |targetCard.position − actingCard.position|` 这个**比值** ——
  真 3D 下两张卡是**战场世界坐标**，而旧 lambda 恒给 **HUD 正交平面**的点。
- **量纲差算出来了**（都是常量，出处 `Board/ArenaSlots.cs:58,68`）：
  2D 兵线距 = `0.2241 × DesignHeight(10)` = **2.241**；3D 兵线距 = `|EnemyZ − PlayerZ| = |1.043 − (−6.655)|` = **7.698** ⇒ **差 3.435 倍**（`lineD/cardD` 一路偏小）。
- **旁证（原版那一对量就是 3D 世界坐标）**：`WFModuleScaleByTarget.cs` 文件头列的三个常量里有一个 **abs 掩码 `FF FF FF 7F`（`0x1834B2E60`）**，**用在「兵线距 = |玩家线 − 敌方线|」上、只取 Z 轴** ⇒ 与我这条 3D 支（x=0、只差 z）逐字同形。
- ⚠️ A 表那一半是**错的**（照 `S2` §二.2）：`:1465/1466` 是**非 3D 兜底支**、3D 支在 `:1459/1460` ⇒ 本件没改那支的语义（搬进 `TryMinionLines` 时**表达式一字未动**）。

**A293**（1 行注释）—— 照 `资料/普查产出_1010/丁_A219_A177尾_A293_A289.md` §②/§⑤ 的口径改成：
「补的那种 = 原版那 6 场自己包里**没有**专属 LUT ⇒ 指的是共享的 `LUT Normal`」，
并把「原版 13/13 场都有 `ColorLookup`」「7 场 `m_FileID=0` / 6 场 `PathID 382974660631151556`」「错因 = 只量了 `battlearena1` 一场就写成通用结论」写进注释。

### 三之二 · 断言（现成的三支，**已备好但落点不在我的白名单里**）

> 🔴 **卡住（见 §四）**：这三支断言的宿主是 `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（**另一个写手占着**）⇒ 本件**一个字节都没碰它**，把**可直接粘贴**的代码留在这儿，请调度台派给那个文件的写手（位置都指到行/节）。

**① A381 —— §20 本地录像那一节（`Editor/BattleScene.cs` 约 `:9028-9040`，「② ★★ 正面：放刚打完的那一局」）**

```csharp
            // 放在 `bool same = driver.PlayReplay(playRec);` **之前**：
            int logBeforeA381   = BattleLogData.Count;
            int skullBeforeA381 = DailyData.SkullsCountValue();
            int settleBefore381 = driver.SettleCount;
            // ……原来的两行 Check 不动……
            // 接在 `Check(driver.Ctx != null && driver.Ctx.IsOver, "回放演到终局（这一局演完了）");` 之后：
            Check(driver.SettleCount == settleBefore381 && driver.ReplaySession,
                  $"★ A381：放一局录像**一笔记账都没有**（结算账次数 {settleBefore381} → {driver.SettleCount}；"
                + $"回放局标记 = {driver.ReplaySession}）—— 原版 `LogMatchEnd` 只在真打完时叫");
            Check(BattleLogData.Count == logBeforeA381,
                  $"★ ……对局记录**没多**（{logBeforeA381} → {BattleLogData.Count}）");
            Check(DailyData.SkullsCountValue() == skullBeforeA381,
                  $"★ ……日常骷髅**没多**（{skullBeforeA381} → {DailyData.SkullsCountValue()}）");
            Check(driver.End == null || !driver.End.Visible, "★ ……结算面板与开门视频**没弹**");
```
- **改坏会不会红**：把 `:8250` 的 `_replaySession` 那一支删掉（或把 `PlayReplay` 里 `:471` 那句删掉）⇒ 第 1 条与第 2/3/4 条**同时红**（`SettleCount` +1、记录 +1、骷髅 +N、面板弹）。把闸改成「只在 `PlayReplay` 那一趟为真」（例如放进 `finally` 清）⇒ 也红（下一帧 `Update()` 就复发）。
- ⚠️ 那节里 `DailyData` 的**其它**写法（`SkullsCountValue()` 是方法不是属性）照 `Editor/RewardsScene.cs` 的既有用法。

**② A382 —— 同节或新开小节（「面板拿不到 ⇒ 出声且账照记」）**

```csharp
            // 该块的不变量：一局一张账（`_settled`），且**不依赖面板存在**
            driver.Begin("Ultramarines", "Goff", 20260913);
            driver.Forfeit();
            Check(driver.Settled && driver.SettleCount == settleBefore + 1, "A382：一局恰好记一笔账");
            driver.RefreshAll();     // 再刷一帧：**不许**再记第二笔
            Check(driver.SettleCount == settleBefore + 1, "A382：再刷 HUD 不会重复记账（闩是 `_settled`，不是面板可见性）");
```
- **改坏会不会红**：把 `_settled` 那一行删掉（回到 `!_endPanel.Visible`）⇒ 第二条在同帧仍绿，但**只要面板被 `Hide()` 过再 `RefreshAll()` 就会红**（拿不准的话把第二条写成「先 `driver.End.Hide()` 再 `RefreshAll()`」⇒ 老实现必红）。
- ⚠️ **`_endPanel == null` 那一支在自检里造不出来**（`EndPanel.Create` 恒建、不返回 null，见 `S2` §二.6）⇒ 「出声」那一半只能靠**代码审查**看，断言只能钉住「账不再依赖面板」这一半。**如实记，不假称验过。**

**③ A368 —— 3D 板下兵线必须是战场世界系（`driver.Begin(...)` 之后任意处）**

```csharp
            Vector3 mlA, mlB;
            float wantLine = Mathf.Abs(ArenaSlots.EnemyZ - ArenaSlots.PlayerZ);      // = 7.698
            Check(driver.MinionLinesForTest(out mlA, out mlB)
                  && Mathf.Abs(Vector3.Distance(mlA, mlB) - wantLine) < 1e-3f,
                  $"★ A368：3D 板的兵线距 = 战场世界系那两个点（期望 {wantLine:F3} = |EnemyZ − PlayerZ|；"
                + $"实得 {Vector3.Distance(mlA, mlB):F3}）—— 退回 2D 的 2.241 就红");
```
- **改坏会不会红**：把 `MinionLines = TryMinionLines;`（`:1246`）换回旧 lambda ⇒ 实得 **2.241** ⇒ 红（差 5.457，任何容差都分得开）。
- ⚠️ 这条要在**建了 3D 战场的宿主**里跑（`driver.boardCam != null`）；`CardBaseDemo` 那类宿主恒走 2D 支 ⇒ 那条断言在那边**不成立**（别抄过去）。

## 四、卡住 / 没查清的部分（⛔ 不猜）

1. 🔴 **三支断言的宿主动不了**：`Editor/BattleScene.cs` **不在白名单**（简报明写「另一个写手正占着它」）⇒ 按简报「必须动到别的文件 ⇒ 停手写进报告」，本件只把**可直接粘贴**的代码留在 §三之二，并已在 `BattleDriver.cs` 里备好三个自检口（`SettleCount` / `Settled` / `ReplaySession`）+ `MinionLinesForTest`。**谁接手都不要再造第二套口。**
2. **A382 的「面板为 null」在自检里造不出来**（`EndPanel.Create` 恒建）⇒ 「出声」那一半**没有断言能覆盖**，只能代码审查。**没查**：`_endPanel` 有没有别的置 null 路径（本件只读了 `:7381` 那一处 `Create` 与 `:4093/:5620/:8319` 三处用法）。
3. **A381 的闸只覆盖「经 `UpdateHud` 的结算」** —— 本件 grep 实证三个记账调用点**各只有一处**（都在结算块内），但**没读** `Shell/` 那边有没有第二条「进战场/出战场」的记账路（例如 `BattleLogData.Add` 的其它调用方）。⇒ 若外面还有一处，回放仍会多记那一笔。**这一条要另查**（不在本件白名单内）。
4. **A368 的 3D 支没跑过**：本件**没有跑 Unity**（本轮口径）⇒ `7.698` 那个期望值是**按常量算的**（`ArenaSlots.PlayerZ/EnemyZ`），不是实测出来的。若 `BattleScene.Run` 那条断言跑红，**先量** `Vector3.Distance(mlA, mlB)` 到底是多少，别急着改断言。
5. **回放局终局要不要给玩家一句结果**（设计问题，本件**没做**，见 §五之 1）。

## 五、顺手发现（⛔ 只报不改）

1. 🟡 **回放局演完之后，屏幕上**没有**任何结果提示**（本件改了之后的新状态）：`_resultLabel.SetText(_endPanel == null ? CardText.Phrase(r) : "")`（`:8233`）—— 面板被回放闸挡下 ⇒ 那行字是空的、面板也不弹 ⇒ 玩家看到的只是「画面停了」。
   **两条候选**（**都不是我拍的**）：① 回放局改成保留那行字（`_endPanel == null || _replaySession ? … : ""`）；② 或者按原版 `ReplayHud` 那条路让回放条显出来（`_replayBar.Setup(true)`，`ReplayBar.cs` 文件头写着原版只有 `matchType == 0xA0` 才显示）。
   ⚠️ **本件没做**：原版「回放局结束那一帧长什么样」**我们没有判据**（我们从不进回放局），按铁律 3 **不自己编**。
2. **`PlayReplay` 里 `deckNote` 传的是「联机局」**（`BeginFromPendingCore` → `Begin(…, deckNote: "联机局")`，`:1675`）—— 回放局会显示成「联机局」。**不是本件范围**（要改也得动 `BeginFromPendingCore` 的签名），只报。
3. **A 表行号又漂了一次**（供调度台订正）：本件开读时 A381 判据里写的「约 `:4482` 与 `:8129`」实为 **`:4482`（真，改前）与 `:8129`（真，改前）**，而 `S2` 报告写的 `:4425` / `:8021` 是**更早一次**的读数 ⇒ 三个数都不是同一个时刻的。**一律现读**这条纪律继续有效。
4. **`Editor/BattleScene.cs` 里那一节的行号**（`S2` 写的 `:8933-9060`）本件复量仍准确（`ReplayStore.OverrideDir` 在 `:196`、正面回放在 `:9035`）—— 与 `:9448-9452`（A293 已订正那处）不在同一段，别混。
5. 🟡 **有几个静态钩子在 `OnDestroy` 里没摘**（既有，**本件没改**）：摘了的只有 `WFModulePostProcess.OnPostFx`（`BattleDriver.cs` 的 `DetachPostFx`，`:1392`）与 `SimpleAI.Executed`（`:1702`）；而 `WFEffectCards.Resolver`（`:1196`）· `WFModuleCollisions.ColliderLookup`（`:1237`）· `WFModuleScaleByTarget.MinionLines`（`:1246`）**都没摘** ⇒ 场景切换后它们仍指着**已销毁的 driver**（下一个 driver 的 `Begin` 会覆盖；覆盖之前若正好有模块被调到，拿的是旧实例）。
   ⚠️ 本件把 `MinionLines` 从 lambda 换成**方法组**，**生命周期语义与原来完全一样**（都绑定 `this`）⇒ **不新增风险**，只是顺手报出来。
   ⚠️ A367 那轮查「`CardFeel.SpawnDeathBody` 与根特效归哪台相机」时，`WFModuleCollisions.ColliderLookup` 也可能被同一类**旧引用**咬到 —— 留个线索。

## 六、跑过的检查

| 检查 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_b2 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0**（改完全部落定**之后**跑的；本件共跑 2 次，两次都 0/0，没有出现「错误集中在别人文件上」那一类假错） |
| 行尾（python **二进制**读） | 改前 `CRLF 8616 / LF 8616`（全 CRLF）→ 改后 `CRLF 8751 / LF 8751`、**bare LF = 0** ⇒ **没翻** |
| `git diff --numstat` | `193 / 58`（只有这一个文件；`Editor/BattleScene.cs` 等改动**都是别的写手的**，本件只读不写） |
| Unity 自检 | ⛔ **一条都没跑**（简报口径：A 表清完再跑）。本件覆盖到的是 **`BattleScene.Run`**（结算/回放/兵线都在它那一节）⇒ 同步点跑**这一条**（若那一位写手同批改了 `BattleScene.cs`，一起跑即可）。 |
| git | ⛔ 没动（只读 `status` / `diff` / `diff --numstat`） |
