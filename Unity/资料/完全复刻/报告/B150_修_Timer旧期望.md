# B150 · 修 6 条「Timer 整棵不建」的旧期望

日期：2026-11-05 · 执行代理：B150-TIMER · 只改两个文件：`Editor/MainMenuScene.cs`（5 条）· `Editor/ShellScene.cs`（1 条）。

**口径（照派活，未自选）**：产品是对的 —— 2026-11-05 起两扇事件窗的 `Timer` 是「**建节点 + 无值整棵藏**」（原版 `SkirmishEventWindow__TryOpen` 的 `SetActive(activationType != 0)`；`TimerDisplay__UpdateTimer.c` 的 `return;` 只解释「不写字」，解释不了「不建节点」）。⛔ 没有回退任何 `Shell/*.cs` 生产文件。

## 一、逐条改动

| 文件:行 | 改前 | 改后 | 灭自证怎么验 | 是否增删行 |
|---|---|---|---|---|
| `Editor/MainMenuScene.cs:10077-10079`（排位窗段位块） | `CheckTrue(rkContent != null && rkTimer == null, "★ 无截止时刻 ⇒ Ranked Division Info/Content/Timer **整棵不建**（原版那一支是 `return;`）")` | `CheckTrue(rkTimer != null && !rkTimer.gameObject.activeSelf, "★ … **建了但整棵藏**（原版 `SetActive(false)`；`return;` 只解释「不写字」）")` | 产品改回「不建」⇒ `rkTimer == null` ⇒ 首合取项 false ⇒ 红；改回「建了不藏」⇒ `activeSelf` 为真 ⇒ 红 | 无（3 行→3 行） |
| `Editor/MainMenuScene.cs:11893-11895`（段位块 `Content/Timer`） | `CheckTrue(dvContent != null && tmr == null, "★ 无截止时刻 ⇒ Content/Timer **整棵不建**（Timer Icon / 文字一起不建）")` | `CheckTrue(dvContent != null && tmr != null && !tmr.gameObject.activeSelf, "★ … **建了但整棵藏**（`Timer Icon` / 文字跟着一起藏）")` | 同上：不建 ⇒ `tmr == null` ⇒ 红；建了不藏 ⇒ 红 | 无（3 行→3 行） |
| `Editor/MainMenuScene.cs:11896`（独立读数 · 递归 `FindChild`） | `CheckTrue(FindChild(dvContent, "Timer Icon") == null, "…Content 整棵子树里**也没有** Timer Icon…")` | `CheckTrue(FindChild(dvContent, "Timer Icon") != null && FindChild(tmr, "Timer Icon") != null && !tmr.gameObject.activeSelf, "…整棵子树里**找得到** Timer Icon（且确实挂在 Timer 下），但 Timer 整棵是关的…")` | 不建 ⇒ `tmr == null` ⇒ 红；把 `Timer Icon` 挪到更深一层偷偷建（父链不在 `Timer` 下）⇒ `FindChild(tmr, …)` 空 ⇒ 红；建了不藏 ⇒ 红 | 无（1 行→1 行） |
| `Editor/MainMenuScene.cs:16520-16522`（能源窗根的直接子件 · 兄弟序） | 期望串 `…Header With Back Button 竖线 Event Instructions 竖线 Army Selector Panel…`，文案写「**`Timer` 不在**」 | 期望串插入 `*Timer`：`…Header With Back Button 竖线 *Timer 竖线 Event Instructions 竖线 Army Selector Panel…`（`*` = `DirectKids` 对关着的件加的前缀） | 不建 ⇒ 实得串少一段 ⇒ 红；建了不藏 ⇒ 前缀 `*` 没掉 ⇒ 红 | 无（3 行→3 行） |
| `Editor/MainMenuScene.cs:16589`（能源窗 `Timer`） | `CheckTrue(FindChild(en.transform, "Timer") == null, "★ 无截止时刻 ⇒ 能源窗 Timer **整棵不建**…")` | `CheckTrue(en.transform.Find("Timer") != null && !en.transform.Find("Timer").gameObject.activeSelf, "★ … **建了但整棵藏**…")` | 不建 ⇒ `Find` 返 null ⇒ 红；建了不藏 ⇒ 红 | 无（1 行→1 行） |
| `Editor/ShellScene.cs:8757-8758`（能源窗 · 小屏字号那一节） | `CheckTrue(FindChildIn(a1126En.transform, "Timer") == null, "★ 无截止时刻 ⇒ 能源窗 Timer **整棵不建**（⛔ 不再印 `--`）")` | `CheckTrue(a1126En.transform.Find("Timer") != null && !a1126En.transform.Find("Timer").gameObject.activeSelf, "★ … **建了但整棵藏**…")` | 不建 ⇒ 红；建了不藏 ⇒ 红 | 无（2 行→2 行） |

**为什么第 4 条用 `*Timer` 而不是「在」**：`DirectKids` 对 `activeSelf == false` 的件加 `*` 前缀（`MainMenuScene.cs:18828` 现读）。宿主日志 `b151_MainMenuScene.log:735355` 的实得串正是 `…Header With Back Button 竖线 *Timer 竖线 Event Instructions…`，与改后期望逐字一致。
**`BackgroundHit` / `AbsorbHit` 是我们的节点**那一半判定逻辑**原样未动**，只插入了 `*Timer` 这一段。

### 顺带就地订正的注释（都是「说现状」的句子，⛔ 非仅历史叙述）
- `MainMenuScene.cs:10064-10068`：块头「① Content 下**没有** Timer（整棵不建）」+「改坏法」→ 改成「**建了但关着**」「不建 / 照旧显示 ⇒ ① 红」；并注明 2026-10-30 那版「节点不在」也已订正。
- `MainMenuScene.cs:10075`、`11880`：把「整棵不建（`RankTimerDeadline` 闸）」改成「节点恒建、无值 `SetActive(false)` 整棵藏（原写已过期）」。
- `MainMenuScene.cs:11883`：覆盖说明里「`Timer` 在不在 / `Timer Icon` 在不在」从「由 `B99A4Assert` 覆盖」改为「**本处已直断**，`B99A4Assert` 只剩下「文案带数字」那一档」。
- `MainMenuScene.cs:11900 / 11902 / 11905`：三处对照文案里的「不建 `Timer`」→「藏 `Timer`」。
- `ShellScene.cs:8754-8756`：原来的「本窗 Timer 只在有值时建 ⇒ 节点不存在」整段改成「建节点 + 无值整棵藏」，并保留「建出来时的尺子仍由 `B99A4Assert` ④c-④e 在注入口覆盖」这条指针。
- `ShellScene.cs:11663`（`B143W4_E74Stations` 的类文档）：「…原版同口径『整棵不建』」→「**建节点 + 整棵藏 ⇒ 本宿主量不到**；建出来时的尺子见 `B99A4Assert`」。

## 二、typecheck

```
TMPDIR=/tmp/wf_b150timer bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```
（跑了两遍：改完代码一遍、改完注释一遍，都是 0/0。）

行尾/行数自检（二进制读）：`MainMenuScene.cs` lines 21269 · crlf 0 · bareCR 0；`ShellScene.cs` lines 11780 · crlf 0 · bareCR 0 —— 我的每一处都是**就地改写**，两次读数一致。

## 三、同族 grep 结果（`grep -rn "整棵不建" Assets/CardPresentation/Editor`）

**仍是「整棵不建」字样、但语义正确（灭自证/改坏法里的否定式）**：
- `MainMenuScene.cs:10067 / 10079 / 11895 / 16589`、`ShellScene.cs:8758` —— 本次改写的「🧨 整棵不建 ⇒ 本条红」。
- `MainMenuScene.cs:9500 / 9509`、`SkirmishWindowAssertE15.cs:216 / 219`、`B99A4_Assert.cs:194` —— 更早批次（B150-WM / E15）已改成「建了但藏」的断言，只把「整棵不建」留在**改坏法**文案里，正确。

**真·疑似漏网（⛔ 未改，按纪律只登记）**：
- `Editor/B99A4_Assert.cs:12`：文件头仍写「做闸：**没有截止时刻 ⇒ 整棵不建**（先例 = `B99_W2`）」。该文件 115 / 179 行已就地订正过（含「原写『整棵不建』已过期」），**只有第 12 行这句没跟上**。不是我负责的文件。
- `Editor/ShellScene.cs:7831`：`B143-W4` 的 ✅ 标记里写「建窗表里**其它窗**的 `Timer` 本地恒 null ⇒ 整棵不建，⛔ 不硬加」—— 说的是**建窗表里别的窗**（不是 `Skirmish/Ranked/Energy` 这三扇），是否仍成立本次未核；`ShellScene.cs` 虽是本代理文件，但这条属于 `B143-W4` 的账，⛔ 没动。
- `Editor/ShellScene.cs:8817 / 8818`：2026-10-30 的历史叙述（「`Timer` 那一半随『无截止时刻 ⇒ 整棵不建』撤出本节」「已改成『无截止时刻整棵不建』」）—— 是**历史留痕**，不是现状描述，按铁律 5 的「保留更正痕迹」**未改**。

## 四、顺手发现

1. **`ShellScene.cs` 上有并发写手的改动落在我的文件里**（不是我写的、也不是本次会话开始时那批）：`NodeCheck(... Vector4 pad = default)`（带 `B150` #3 注释）、两处 `B142E74_Assert.CheckHidden` 的入参从 `path` 改成父件名（`B150` #1 / #2）。这些在我动手前就已在盘上，与我的 6 条不重叠、无冲突；但**按铁律 13·3「一个文件同一时刻只有一个写手」，这份所有权登记需要调度台核一下**。
2. **`Shell/EnergySinglePlayerOnlyEventWindow.cs:1442`** 的行尾注释仍写「节点本来不建时它什么都不做 —— `EnergyTimerDeadline` 为 null ⇒ `Build()` 不建 `Timer`」—— 产品已改成恒建，这句**已过期**。属生产文件，⛔ 按派活没动。
3. `MainMenuScene.cs:7381`（改名窗）与 `10785`（`UIRankingListController.SetTimer` 那一档）**已经是**「`!= null && !activeSelf / !activeInHierarchy`」的写法 —— 与本次口径一致，说明这两处此前已被改过；本次未动。
4. 本次 6 条全部只依赖 `activeSelf`（节点自己被 `SetActive(false)`），没有用 `activeInHierarchy`：这两处窗口在宿主里可能整体被 `SetActive(false)`（`MainMenuScene.cs:10294` 那条 W64 现场就断过「排位宿主整棵关」），用 `activeSelf` 才能把「窗口关着」与「Timer 关着」分开。
