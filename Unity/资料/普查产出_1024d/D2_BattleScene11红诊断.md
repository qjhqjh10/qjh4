# D2 · `BattleScene.Run` 11 红诊断（只读代理，2026-10-24）

> **现场**：`d:/4/_tmp_view/20261024d/BattleScene.Run.log`（23:23 跑完）· `BT === 合计：5151 通过 / 11 失败 ===`
> （10 条 `BT ✗` + 1 条 `BT W10 ✗`；任务书列了 10 条，**第 11 条 = `BT W10 ✗ ★ T2·诊断`（log:133347）**，本报告一并判。）
> **基线对照**：`d:/4/_tmp_view/20261024_sync7_battle.log`（**17:18** 跑）＝ **5014 通过 / 0 失败**
> ⇒ **这 11 条全部是 17:18 之后那一批（W1/W5/W9/W10/W11/W15/W20）引入的**（见末节）。
> **纪律**：没跑 Unity、没动 git、没改任何代码 / 正本；只读源码 + 只读今天各执行报告 + 只读反编译 `d:/2/tools/decomp_full/`。
> 行号一律「本报告落盘时那一版」；这两个文件今天被多批改过 ⇒ ⛔ 别把行号当判据，按符号找。

---

## 〇、结论速览

| # | 断言（片段） | 性质 | 本批引入？ | 处置 | 根因一句话 |
|---|---|---|---|---|---|
| 1 | `W20·W10④` 技能钮 tooltip 弹一次 | **α** 断言错 | 是（W20 新断言） | 改断言 | 断言拿 `ShownBody` 比「正文」，而 `ShownBody` 是 `标题+"\n"+正文` |
| 2 | `W20·W10⑤` 技能钮选中音走到播放口 | **β** 实现缺陷 | 是（W10 新代码） | 改实现（1 行） | 批处理 `WFSoundPlayer.Enabled=false` ⇒ `Play()` 返回 null ⇒ **两个计数器都不动**（没按 `Played` 记） |
| 3 | `残骸体归位到卡上（偏差 0.59963）` | **α** 断言错 | 是（W15 改了挂点） | 改断言 | 断言还比「卡心」；W15 按反编译改成**卡底边**，`0.59963 = Height/2 × 0.36` **逐位吻合** |
| 4 | `A462：（同一帧）按下沿照旧给` | **δ** 夹具前提不成立 | 是（W20 新夹具） | 改夹具（+加固） | 上一节 W20 的 R3 出口夹具把 `_held` 停在 `true` 就收工 ⇒ A462 第 1 帧**根本不出按下沿** |
| 5 | `A462：按下沿只剩 4 处（实得 5）` | **α** 断言过期 | 是（新增第 5 处） | 改断言 **或** 改沿（见 §5） | 新加的是 `HandleReplaySessionExit` 的「点空处退出」；**这一处是我们自己加的**，原版对应物是 uGUI 按钮 |
| 6 | `回放完的终局局面哈希 == 录制时的` | **β** 实现缺陷 | 是（W1） | 改实现 | 回放**静默漏演了最后一条动作**（重入闸 + 游标先 ++） |
| 7 | `判别式：终局局面速写逐字相同` | **β**（同 6） | 是（W1） | 改实现 | 同一个根因 |
| 8 | `W9 反例①的前提（终局指纹照旧相等）` | **β**（同 6） | 是（W1） | 改实现 | 同一个根因 |
| 9 | `W9 反例②的前提（老档照样演得完）` | **β**（同 6） | 是（W1） | 改实现 | 同一个根因 |
| 10 | `新账-75③ 多播桶 355 / 钉不上 3（区间 20–400）` | **α** 断言区间过期 | 是（W5 放行 strike/slay） | 改断言 | 26→3 是 W5 的**正常后果**；`unp75 > 20` 这条下限是旧表的护栏 |
| 11 | `T2·诊断：全池没有任何「图在、但数据表里查不到」的图名` | **α** 断言语义错 | 是（W10 新断言） | 改断言（+顺手改 doc） | 计数器记的是「**不在 47 白名单**的图名」⇒ **必然**含被原版三列挡掉的那 17 条 |

**统计**：断言错（α）**5 条**（1/3/5/10/11）· 实现缺陷（β）**5 条但只有 2 个独立根因**（2 · 6/7/8/9）· 夹具前提（δ）**1 条**（4）· 纯 γ（只回归、代码没错）**0 条**
—— **但这 11 条全部是 17:18 之后那一批引入的新红**（见末节「本批回归清单」）。
**顺手发现的另两个真缺陷**（不是这 11 条红的原因，但同批、都是「空判/静默」）：见 §12。

---

## 1. `W20·W10④` 技能钮 tooltip 弹一次 —— **α（断言错）**

**证据**
- 断言：`Editor/BattleScene.cs:7911-7916`
  ```csharp
  Check(selW.AbilityTooltipShown && Tooltip.ShowCount == showBefore + 1
        && Tooltip.ShownBody == "自检·W20 tooltip", ...)
  ```
- 日志（`log:120001`）：`ShowCount` **0 → 1** ✓、正文「**自检\n自检·W20 tooltip**」—— 三格里的前两格都满足，
  **红在第三格**。日志自己的括号里已经把真实 `ShownBody` 打出来了（换行把标题和正文分成两行）。
- 现读实现：`Battle/AttackSelector.cs:1481-1489`
  ```csharp
  string text = string.IsNullOrEmpty(title) ? body : title + "\n" + body;   // title="自检" ⇒ "自检\n自检·W20 tooltip"
  Tooltip.Show(text, at.Value, 27, ...);
  ```
  而 `Core/Tooltip.cs:308-313`：`public static void Show(string body, ...) { … ShownBody = body; ShowCount++; }`
  ⇒ **`ShownBody` = 传进去的整串（含标题）**，不是「正文」。
- 同一段里紧跟的那条 `……再刷一次不再弹`（`BattleScene.cs:7917`）**是绿的**（`ShowCount` 仍是 1）⇒ `_tooltipShown` 那道闸没问题。

**最小修法（改断言，一行）**
```
旧： && Tooltip.ShownBody == "自检·W20 tooltip"
新： && Tooltip.ShownBody == "自检\n自检·W20 tooltip"
    （或 .EndsWith("自检·W20 tooltip"）—— 若想同时钉「标题也在」，用全串更好）
```
⚠️ 别改成「只比 `ShowCount`」：那样「弹的是别的文案」就抓不到了（这条的价值正在第三格）。

---

## 2. `W20·W10⑤` 技能钮选中音走到播放口 —— **β（实现缺陷）**

**证据**
- 断言：`Editor/BattleScene.cs:7926-7939`，`int d = (SelectSoundPlays-plays) + (SelectSoundMisses-misses); Check(d == 1, …)`。
  它自己的注释写着「⚠️ 批处理里可能没有音频设备 ⇒ **两个计数器任一 +1** 就算「走到了播放口」」。
- 日志（`log:120028`）：`SelectSoundPlays 0→0 · SelectSoundMisses 0→0` ⇒ `d == 0`。
- 宿主先静音：`Editor/BattleScene.cs:450-468` —— `bool prevSoundEnabled = WFSoundPlayer.Enabled; WFSoundPlayer.Enabled = false;`
  注释原话：「`WFSoundPlayer.Enabled = false` 时那个开关自己的契约是「**只记账不发声**：不建对象、不出声，
  **但 `Played` 照样涨**」⇒「音效调度算对了没有」那一族断言**照旧有效**」。
- 现读实现：`WarpforgeVFX/Runtime/WFSoundPlayer.cs:97-104`
  ```csharp
  public static AudioClip Play(WFSoundCue cue, Vector3 worldPos, bool is2d)
  {
      Played++;                                   // ← 无条件 +1（正是不受 Enabled 影响的那个口）
      if (!Enabled || cue == null) return null;   // ← 批处理在这里就返回了
  ```
- 而 `Battle/AttackSelector.cs:438-456` 的 `PlaySelectSound()`：
  ```csharp
  if (!WFSoundBank.TryGetCue(SelectSoundCue, out cue) || cue == null) { SelectSoundMisses++; … return; }
  if (WarpforgeVFX.WFSoundPlayer.Play(cue, Vector3.zero, true) != null) SelectSoundPlays++;   // ← 453
  ```
  **`Enabled=false` 时 `Play` 返回 null ⇒ `Plays` 不加；cue 又查得到 ⇒ `Misses` 也不加** ⇒ 两个计数器都不动。
  （日志里也**没有** `[Battle] 技能钮选中音：…查不到 cue` 那条 warning ⇒ `TryGetCue` 是**成功**的，
  即「调用链走没走到」与「cue 在不在」两件事被同一个 0 掩盖了。）
- 为什么不是「接线没生效」：`MarkHoverPicked`（`AttackSelector.cs:426-433`）里 `if (Hovered == Ability) PlaySelectSound();`
  确实在；断言走的正是这条链（`HighlightTriggerForTest(1)` = Order 下标 1 = `Ability`，`AttackSelector.cs:307`）。
  本条的病灶**只在计数器取错了量具**。

**最小修法（改实现，1 行）**——用这个开关自己的契约口 `Played` 的**增量**：
```
旧（AttackSelector.cs:453）：
    if (WarpforgeVFX.WFSoundPlayer.Play(cue, Vector3.zero, true) != null) SelectSoundPlays++;

新：
    int playedBefore = WarpforgeVFX.WFSoundPlayer.Played;      // 不受 Enabled 影响（WFSoundPlayer.cs:24-25）
    WarpforgeVFX.WFSoundPlayer.Play(cue, Vector3.zero, true);
    if (WarpforgeVFX.WFSoundPlayer.Played > playedBefore) SelectSoundPlays++; else SelectSoundMisses++;
```
这样：① 批处理静音时 `d == 1`（走到播放口）；② 真机出声时也是 `d == 1`；
③ 把 `MarkHoverPicked` 里那半句删掉 ⇒ `d == 0` ⇒ 断言照旧红（**灭自证保住**）。

---

## 3. `残骸体归位到卡上（偏差 0.59963）` —— **α（断言还停在旧口径）**

**证据（数字逐位吻合，不是「差不多」）**
- 断言：`Editor/BattleScene.cs:14027-14030`
  ```csharp
  float d = Vector3.Distance(vA.RemnantBodyRoot.transform.position, vA.transform.position);   // ← 与【卡心】比
  Check(d < 0.001f, $"★ 残骸体**归位到卡上**（偏差 {d:F5}；不归零会是 100 上下）");
  ```
- 现读实现（`W15` 今天改的）：`Core/CardView.cs:2344-2357`
  ```csharp
  // 原版 BattleCardUI__CreateRemnantBody.c:54-75：Instantiate(prefab, this.transform.position, …, this.transform)
  //   ⇒ 父 = 卡的 transform、局部位置 = (0,0,0)，以【原版卡根】为原点；原版卡根 = 卡底边（C6 §2·1）
  //   我们卡根在卡心 ⇒ 等价局部位置 = 卡底边 = From3DBody(0,0,0) = (0, −Height/2, 0)
  var player = WarpforgeVFX.WarpforgeEffectPlayer.Play(entry, transform, From3DBody(0f, 0f, 0f), 1f, float.PositiveInfinity);
  ```
  `WarpforgeEffectPlayer.Play` 里 `Instantiate(entry.prefab, parent)` + `t.localPosition = localPosition`
  （`WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:243,261`）⇒
  **`RemnantBodyRoot.position == card.transform.TransformPoint((0, −Height/2, 0))`**，
  与卡心的距离恒 = `Height/2 × |lossyScale.y|`。
- `Core/CardView.cs:227`：`public const float Height = 3.3313f;` ⇒ `Height/2 = 1.66565`
  日志实得 **0.59963** ⇒ 隐含 `lossyScale.y = 0.59963 / 1.66565 = 0.359985 ≈ 0.36`
  —— 与 W15 报告里那句「我方兵 0.36 档 ≈ 0.6 世界单位」（`W15_卡根接线与残骸体_执行报告.md` §三）**逐位一致**。
  （同一段后面那条 `残骸体的缩放 = 原卡卡身那个值（0.31891 vs 0.31891）` 是**绿的** ⇒ 缩放那一跳没坏。）
- **判据核过（不是只信代理报告）**：`d:/2/tools/decomp_full/BattleCardUI__CreateRemnantBody.c:52-75` 现读 ——
  `UnityEngine_Component__get_transform(param_1)` 取两次（`get_position` 与 `Instantiate` 的第 4 参），
  即 **世界位置 = 卡自身位置、父 = 卡自身** ⇒ **局部 (0,0,0)**；「原版卡根 = 卡底边，我们 = 卡心，差 3.3313/2」
  是既有定论（`资料/交接_第七十三会话.md:119` · `任务线/战斗线/任务总表.md:235` 的 `C6·Q2`）。
- `W15` 已经为这件事另建了**权威断言**：`Editor/CardRootAssert.cs`（`RunRemnantMount`，报告 §三·断言 ①~④，
  含「残骸体离卡心 > `Height × 0.45`」的灭自证）⇒ `BattleScene` 里这一条是**第二份旧口径**。

**最小修法（改断言，按「卡底边」比 + 补一条灭自证）**
1. `Core/CardView.cs` 加一个公开读口（与 `BoardEffectAnchorLocal` 同族，判据只留一处）：
   ```csharp
   /// 自检用：残骸体挂点的局部位（= 原版卡根原点 = 卡底边）。
   public static Vector3 RemnantBodyAnchorLocal { get { return From3DBody(0f, 0f, 0f); } }
   ```
   ⚠️ `CardView.cs` 不在本诊断代理白名单，也不在 `W15` 之后任何代理的白名单 ⇒ 由主对话派活时明确给。
2. `BattleScene.cs:14027-14030`：
   ```
   旧：d = Distance(root.position, vA.transform.position);  Check(d < 0.001f)
   新：var anchor = vA.transform.TransformPoint(CardView.RemnantBodyAnchorLocal);
       d = Distance(root.position, anchor);                 Check(d < 1e-3f)
       // 灭自证（挡「改回 Vector3.zero」）：它离卡心必须约 Height/2×lossyScale，不能贴卡心
       var halfH = Mathf.Abs(vA.transform.lossyScale.y) * CardView.Height * 0.5f;
       Check(Mathf.Abs(Distance(root.position, vA.transform.position) - halfH) < 0.05f, …)
   ```
   文案同步改成「归位到**卡底边**（原版卡根原点）」；`0.001` 那档别放宽（它同时挡 x=100 没归零）。

---

## 4. `A462：（同一帧）按下沿照旧给` —— **δ（夹具前提不成立）**

**证据链（能一路钉到具体那一节）**
- 断言：`Editor/BattleScene.cs:30340-30352`
  ```csharp
  BattleDriver.PointerHeldForTest = true;
  wb4.PollInputEdgesForTest();                       // 第 1 帧：按下
  Check(!wb4.ReleasedThisFrameForTest(), …);          // ✓（绿）
  Check(wb4.ClickedThisFrameForTest(), "★ A462：（同一帧）按下沿照旧给 —— ClickedThisFrame 的语义一个字没改");
  ```
  日志：`log:195719` 这一条 **✗**，而它上面那条「不给松手沿」**✓**、下面那条「一次按住只给一次」**✓**、
  第 3 帧「松手那一帧**给松手沿**」（`log:195771`）**✓**。
- 现读实现：`Battle/BattleDriver.cs:29244-29251`
  ```csharp
  void PollInputEdges() { bool held = PointerHeld();
      _downEdge = held && !_held;      // ← `_held` 已经是 true ⇒ 不出按下沿
      _upEdge   = !held && _held && …; _held = held; }
  ```
  ⇒ 上面这组绿/红**唯一自洽的解释 = 进这一节时 `_held` 已经是 `true`**（第 1 帧没有「上升」；
  后面「松手给松手沿」反而证明 `_held` 当时确实是 true）。
- `_held` 只有两个写点：`PollInputEdges()`（`BattleDriver.cs:12614`，在 `Update` 里）与
  `PollInputEdgesForTest()`（`BattleDriver.cs:29327`）。**批处理没有帧循环**（`BattleScene.Step` 只调
  `AdvanceTimeline`，见 `Editor/BattleScene.cs:43743-43752`）⇒ 只可能是**某节夹具最后一次 `Poll` 时钉着 `true`**。
- 钉 `PointerHeldForTest` 的节（`grep PointerHeldForTest Editor/BattleScene.cs`）：`10641`（R 段）·`11982`（结算后出口）·
  **`28314`（今天 W20 新加的 R3 回放出口）**·`30343`（A462 本节自己）· 34016/35417/38592/38760（**都在 A462 之后**）。
  按**执行顺序**（源码序 = 执行序），A462 之前**最后**一节 = **`28314`**。
- 那一节的收尾（`Editor/BattleScene.cs:28338-28356`）：
  ```csharp
  BattleDriver.PointerHeldForTest = false; driver.PollInputEdgesForTest();   // _held=false
  BattleDriver.PointerHeldForTest = true;  driver.PollInputEdgesForTest();   // _held=true ← 最后一次 Poll
  bool took2 = driver.TickReplaySessionExitForTest();
  …
  finally { …; BattleDriver.PointerHeldForTest = phWas; }                    // ← 只还了静态，【没有再 Poll 一次】
  ```
  ⇒ `_held` 停在 `true`，**直到 A462 的第 1 帧才有下一次 Poll**，而那一帧已经「按着」了。
  （W20 自己在同一个方法里明明写了「⚠️ 一次完整的按下沿：**先松（把 `_held` 归零）再按**」—— 它自己用对了，
  只是**收工时没把 `_held` 也还回去**。）
- **基线佐证**：17:18 的 `20261024_sync7_battle.log` 里这一条是 **✓**（那时 R3 那一节还不存在）。

**最小修法（两处，建议都做）**
1. **治源**（`BattleScene.cs:28350-28356` 的 `finally` 里，还原静态之后补一次 Poll）：
   ```csharp
   BattleDriver.PointerHeldForTest = phWas;
   driver.PollInputEdgesForTest();          // 用还原后的口径再走一帧 ⇒ `_held` 回到真实值（批处理 = false）
   driver.ReleasedThisFrameForTest();       // 顺手耗掉可能补出来的松手沿，别漏给下一节
   ```
   ⚠️ **同一族的其它节也查一遍**（`10641` / `11982` / `34016` / `35417` / `38592` / `38760`）：
   凡是「最后一次 Poll 钉着 true」的节，都会把 `_held` 留给下一节。判据 = `finally` 里有没有「还原静态之后再 Poll」。
2. **加固 A462 本节**（`BattleScene.cs:30342-30343`，照 W20 自己的写法）：
   ```csharp
   BattleDriver.PointerHeldForTest = false;  wb4.PollInputEdgesForTest();   // 先松：把 `_held` 归零
   BattleDriver.PointerHeldForTest = true;   wb4.PollInputEdgesForTest();   // 再按：这一帧才是按下沿
   ```
   ⚠️ 只做 2 也能绿，但**别的节还会再踩**（`_held` 是共享实例状态）⇒ 两条都做。

---

## 5. `A462：按下沿只剩 4 处（实得 5 处）` —— **α（计数断言过期）**；那一处**该不该是按下沿**另说

**5 个调用点逐一处（`BattleDriver.cs`）**

| 行 | 代码 | 是谁 | 该按下还是该抬起 |
|---|---|---|---|
| 7239 | `if (!ClickedThisFrame()) return false;` | **`HandleReplaySessionExit()`（今天新加）** | **见下** |
| 7391 | `if (ClickedThisFrame())` | `HandleBattleLog`（日志背板 `shade`） | 按下 ✓（原版 `EventTrigger` `eventID=2=PointerDown`） |
| 11176 | `bool clicked = ClickedThisFrame();` | `HandleEndBattleExit` | 按下 ✓（原版 `BattleManager__Update.c:116-122` `Input.GetMouseButtonDown(0)`） |
| 13141 | `if (ClickedThisFrame() && !selector.ContainsBar(world))` | 攻击选择器槽外取消 | 按下 ✓（原版无对应物，`WA462 §四·3` 保持现状） |
| 13219 | `if (!ClickedThisFrame()) return;` | 记 `_pressSlot` | 按下 ✓（中间态） |

- **新增的那一处是今天 W1 加的**：`git diff` 对 `BattleDriver.cs` 的全量 diff 里，`+…ClickedThisFrame` **只有一行**
  = `+ if (!ClickedThisFrame()) return false;`（`HandleReplaySessionExit`，整个方法今天新建；
  `W1_横幅与回放出口_执行报告.md` §〇 ②：「**新增** `HandleReplaySessionExit()`」）。
- 断言（`Editor/BattleScene.cs:30921-30929`）`Check(downCallSites.Count == 4, …)`，注释里的白名单只有 ①②③④。
- **它是不是「误用」——判据核过**：`ClickExitBattle` **全库只有一个调用点** =
  `BattleSettingsWindow__ResignButtonOnClick.c`（`d:/2/tools/decomp_full/` 里 `grep -l ClickExitBattle` 只有这两个文件）
  ⇒ 原版回放出口的触发沿 = **uGUI 按钮的 `m_OnClick` = 抬起**，**不是**按下。
  而我们的「点空处退出」是**我们自己压出来的一条**（`W1` 报告 §4·5 原话：「⚠️ **ESC 出口我们压成了一步**
  （原版：ESC 开设置窗 → 点 `Resign` → `ClickExitBattle`）…⛔ 这一条**不是复刻**」）。
  ⇒ 按 A462 自己的口径（「凡对应原版 uGUI 的钮都改走 `ReleasedThisFrame`」），这一处**更像误用**；
  但它不是「原版那颗钮」，是我们自造的等价入口 ⇒ 两种处置都说得通，**要主对话拍一下**。

**两条修法（二选一，推荐 A）**
- **A（更贴原版，断言一个字不改）**：`BattleDriver.cs:7239`
  ```
  旧： if (!ClickedThisFrame()) return false;
  新： if (!ReleasedThisFrame()) return false;
  ```
  代价：W20 的两条出口断言/夹具（`BattleScene.cs:28318-28349`）是按「按住那一帧」写的，
  要改成「松手那一帧」（把 `PointerHeldForTest` 先 true 再 false 各 Poll 一次）。
- **B（最小改动）**：`BattleScene.cs:30922`
  ```
  旧： Check(downCallSites.Count == 4, "…只剩 4 处…")
  新： Check(downCallSites.Count == 5, "…只剩 5 处…")
  ```
  并在 `30905-30920` 的白名单注释里补第 **⑤** 条：
  「⑤ 回放局空处出口（`HandleReplaySessionExit`）—— **这一处是我们自己加的**（原版对应物是设置窗 `Resign` 那颗
  uGUI 按钮 = 抬起；我们压成 ESC/点空处一步，判据见 `W1` 报告 §4·5）；保留按下沿 = **有意的偏离**，已记账」。
  同时把 `BattleDriver.cs:29259-29272`（`ClickedThisFrame` 的 doc）里「全文件只剩 **4 处**」改成 **5 处**并点这句。

---

## 6–9. 回放终局哈希 / 局面速写 / W9 反例①前提 / W9 反例②前提 —— **β（同一个真缺陷：回放静默少演一条动作）**

> 这四条红是**一个根因**。任务书里那句「若红，大概率是真差异」——**方向对，但不是引擎分叉，是回放器漏演**。

**证据一：录制端没变，回放端变了**
- 今天：`log:200154` `[Replay] 演完了：2 回合 · 终局指纹 946424094 · 轨迹摘要 -686436611（录制时 1841455181）≠ 录制时的 -1400043361 ｜ 回放全量指纹：逐条一致`
- 17:18（`20261024_sync7_battle.log`）：`终局指纹 -1400043361 · 轨迹摘要 1841455181（录制时 1841455181） = 录制时的 ⇒ 同一局`
  ⇒ **录制端的 `rec.trace` 摘要两天完全一样（1841455181）**，是**回放侧**从「2 个样本」掉成「1 个样本」。

**证据二：轨迹摘要的算术把「1 个样本 vs 2 个样本」钉死**
`NetProtocol.HashOfInts`（`Net/NetProtocol.cs:669-678`）＝ `h=29; h=h*31+v[i]`（空表 = **29**，与
`log:188580`「轨迹摘要 29」吻合）。设录的 `trace = [t0, t1]`：
- 回放摘要 `29*31 + t0 = -686436611` ⇒ `t0 = -686437510`
- 录的摘要 `31*t0 + t1 = 1841455181` ⇒ `t1 = 1646181511`（两条都**逐位对得上**，本代理已算过）
⇒ **回放只采到 `t0` 一个样本**（回放第 0 条之后与录制逐位相同，第 1 条之后**根本没采**）。

**证据三：局面速写指出「少演的是哪一条」**（`log:200194`，断言自己打出来的）
```
录的 |0: … ----UM47@23 ----|1: … ----UM46@28! ----
放的 |0: … ----UM47@25 ----|1: … ----UM46@30 ----
```
（`@` 后是血量、`!` = `Exhausted`）⇒ 录像里第 2 条 = **AI 脚本的 `Attack`**（`EvtKind.Attack` 计数 +1 那条，
`BattleScene.cs:32109-32134` 的夹具：「★ 第 2 回合第 1 条（AI 的 `Attack`）由**脚本**执行」）；
**它的伤害与「攻方疲劳」在回放里一条都没落地**，
回放终局 = **第 1 条（玩家 `EndTurn`）做完的样子** ⇒ **丢的就是最后那一条动作**。

**根因（现读代码，机制上必然）**
1. `BattleDriver.cs:1084-1091`（今天 `W1` 新加）：
   ```csharp
   //  链路：本方法 → ApplyLoggedAction → RefreshAll → PlaySignals → AdvanceTimeline(0f)
   //        → 本类末尾 → TickReplayPump → 又回到本方法
   if (_replayPumping) return true;      // 重入时返回 true（= 这一次里没做事，但也没失败）
   _replayPumping = true;
   ```
2. `BattleDriver.cs:1270-1287` `TickReplayPump()`：
   ```csharp
   int i = _replayCursor++;              // ← 先把游标推走
   if (!ApplyReplayActionAt(i)) { … }    // ← 再调；重入时这里被上面那道闸挡成 no-op
   ```
3. 链路是真的：`AdvanceTimeline(0f)` 在 `PlaySignals` 末尾（`BattleDriver.cs:17214`），
   `AdvanceTimeline` 末尾调 `TickReplayPump()`（`BattleDriver.cs:18233`）。
   ⇒ **外层正在落第 i 条动作时，内层 pump 把游标推到 i+1、然后那一条被 no-op 掉**；
   批处理那条 `while (_replayCursor < rec.actions.Count)`（`BattleDriver.cs:1030-1038`）转头看见游标已到底 ⇒ **静默收工**。
   （`W1` 报告 §5·6 自己写了这个重入链，但把「返回 true」理解成「没做事、也没失败」——**没注意到游标已经走掉了**。）
4. 为什么 17:18 是绿的：那时回放还是「一口气 for 循环灌完」（`W1` 之前的写法），没有 pump 重入。

**为什么第 7/8/9 条跟着红**：它们都直接读 `PlayReplay` 的返回值/同一份录像的终局比较
（`BattleScene.cs:32184-32191` · `32270-32275` · `32296-32301`）⇒ **少演一条**时必然一起红；
第 9 条（老档）还额外证明「与 `traceFull` 那一列无关」——**病灶在推动作，不在对账列**。

**最小修法（改实现）**
1. **把重入闸挪到游标推进之前**（`BattleDriver.cs:1279-1281`）：
   ```csharp
   if (_timeline.Count != 0 || _clock < _blockingQuietAt) return;
   if (_replayPumping) return;          // ← 新增：重入时【什么都不做，游标也别动】
   int i = _replayCursor++;
   if (!ApplyReplayActionAt(i)) { … }
   ```
   `ApplyReplayActionAt` 开头那道闸**保留**（防御别的入口），但它从此不该再被「自己人」踩到；
   顺手把它改成**返回 false**（「没做事」不等于「成功」）会让调用方误判成「分叉」⇒ **保留 true 更安全**，
   真正的修法是不让游标被偷走。
2. **给 `TickReplayPump` 配一条断言**（现在没有）：造一份「第 2 条无任何 VFX/时间线事件」的录像，
   回放后断 `ReplayCursorForTest == actions.Count && 终局指纹 == 录制时` ⇒ 改回旧写法（先 ++ 再判重入）当场红。
   ⚠️ 现有那条「逐条轨迹一条都没分叉」**抓不到**（缺的正是最后一条，比较只覆盖录到的下标）—— 见 §12·1。

**验收**：修完至少要重跑 §21（回放）整节 + `log:200154` 那一行变成 `= 录制时的 ⇒ 同一局`。

---

## 10. `新账-75③ 多播桶 355 / 钉不上 3（判据区间 20–400）` —— **α（区间下限过期）**

**证据**
- 断言：`Editor/BattleScene.cs:36585-36593`
  ```csharp
  Check(multi75 > 300 && unp75 > 20 && unp75 < 400, $"★ 新账-75③：全池多播桶 {multi75} 个，其中钉不上 … {unp75} 个（判据区间 20–400；…）");
  ```
  日志（`log:216818`）：**`multi75 = 355`（> 300 ✓）· `unp75 = 3`（`> 20` ✗）** ⇒ 红在**下限**。
- **是不是今天的放行造成的 —— 是，而且方向正确**：
  - 17:18（`sync7`）：`多播桶 502 / 钉不上 26` ⇒ **在区间内、绿**。
  - 今天 `W5` 把 `strike`(580) / `slay`(120) 从「第 ④ 档掉回整桶」改成**放行**（`Core/VfxMap.cs:2528-2562`
    `case "strike": case "slay": return true;`）。放行后这条 trigger 只取**它自己那一条 ability** 的件 ⇒
    桶里件数从 >1 掉到 ≤1 的桶变多（**502 → 355**），而 trait 件在 `CardDef.AnimTimes` 里本来就没有行
    ⇒ 那条「钉不上」的候选也跟着不再进桶（**26 → 3**）。W5 报告 §五·6 只说了「这个计数器会被我这轮的全池扫描加数」，
    **没预见条数会掉**（它逐条写清了 36 行/32 卡的归属）。
  - 也就是说：**3 不在「今天放行之前」的区间外**（放行前是 26）⇒ 确实是 W5 造成的；**但这是行为变精确的副作用，不是缺陷**。
- 计数器本身没坏：`Core/VfxMap.cs:658-668` `NoteUnpinnedPick` 在且只在该路径 +1；`warn75 > 0` 那条（`36602`）**绿**
  （日志 `log:216831`：「已经报过 N 种」）⇒ 出声链还在。

**最小修法（改断言：把「机制真的跑到了」和「没失控」分开写，不写死数）**
```
旧： Check(multi75 > 300 && unp75 > 20 && unp75 < 400, "…（判据区间 20–400；数据侧复算 ≈238 …）")
新： Check(multi75 > 300 && unp75 > 0 && unp75 < 400,
          $"★ 新账-75③：全池多播桶 {multi75} 个，其中钉不上 AnimTimes 的候选 {unp75} 个（下限 1 = 机制真跑到；上限 400 = 没失控）"
        + "（⚠️ 2026-10-24 `W5` 放行 strike/slay 后 26→3：那两条 trigger 不再掉回整桶，trait 件随之不进桶 ⇒ 掉数是预期的；"
        + "改坏法仍是把 NoteUnpinnedPick 那行删掉 ⇒ 0 ⇒ 红）");
```
⚠️ **别改成 `unp75 >= 0`**（那就恒真了）；`> 0` 保住灭自证。
⚠️ 文案里那句「数据侧按同形贪心复算 ≈238」现在已经**既不等于 26 也不等于 3**，一并删掉或标「旧表口径」。

---

## 11. `T2·诊断：全池没有任何「图在、但数据表里查不到」的图名` —— **α（断言语义与计数器语义不是一回事）**

**证据**
- 断言：`Editor/BadgeFilterAssert.cs:118-123`（新文件，今天 `W10` 加）
  ```csharp
  var unknown = Badges.UnknownIconSpritesForTest;
  Check(unknown.Length == 0, "★ T2·诊断：全池没有任何「图在、但数据表里查不到」的图名" + …);
  ```
- 日志（`log:133347`）报出 **17 个**：`ambush / armour / berzerk / companion / ephemeral / fast / flank / flying /
  huntMark / markerlight / pack / rally / talent / teleport / tide / uprising / vanguard`。
  这 17 个**恰好** = 同文件自己那两张「有图、但原版不上场上徽标」名单：
  `HasIconFalse`（5 条：`ambush berzerk flying huntMark markerlight`，`BadgeFilterAssert.cs:38`）
  ＋ `NotInPlayIcon`（12 条：`armour companion ephemeral fast flank pack rally talent teleport tide uprising vanguard`，`:41-44`）。
- 现读实现：`Core/Badges.cs:441-455` `PlaysInPlayIcon(string sprite)`
  ```csharp
  if (InPlayIconKeys.Contains(Key(sprite))) return true;
  if (_notInDataWarned.Add(sprite)) Debug.LogWarning("[Badges] 图 `" + sprite + "` 不在「场上图标」那 47 条里 ⇒ 按原版口径不画…");
  return false;
  ```
  ⇒ 计数器记的是「**不在那 47 条白名单里的图名**」（= 被原版 `hasIcon && isInPlayIcon && traitIcon` 挡掉的 17 条
  **本来就该在里面**），而 `Core/Badges.cs:434-436` 的 doc 又写着「正常卡池里**应当是空的**」——**自相矛盾**。
  真正的「图在、但数据表里查不到」（图集里有、`trait_collection.json` 77 条里没有）**这一格从来没记过**
  （要判它需要一份 77 条的完整键集，运行期读不到那份 JSON）。

**最小修法（改断言；顺手改 doc）**
```
旧： Check(unknown.Length == 0, "★ T2·诊断：全池没有任何「图在、但数据表里查不到」的图名" …)

新： var known17 = new HashSet<string>();
     foreach (var k in HasIconFalse)  known17.Add(Badges.Norm(k));
     foreach (var k in NotInPlayIcon) known17.Add(Badges.Norm(k));
     var unexplained = new List<string>();
     foreach (var k in unknown) if (!known17.Contains(Badges.Norm(k))) unexplained.Add(k);
     Check(unexplained.Count == 0,
           "★ T2·诊断：计数器里**没有没解释过**的图名（被原版三列挡掉的那 17 条属于预期，不算）"
         + (unexplained.Count == 0 ? "" : "｜✗ " + string.Join(" / ", unexplained))
         + " —— 🧨 图集里多一张对不上 `trait_collection.json`、又被当关键词用 ⇒ 红（不静默）");
     // 可选第二条（钉「还是那 17 条，没多没少」）：Check(unknown.Length == 17, …)
```
＋ `Core/Badges.cs:434-436` 的 doc 改成「= 不在 47 条白名单里的图名（当前应当恰好是那 17 条）」；
＋ （可选）加 `ResetUnknownForTest()` 并在池扫前调用 —— 现在 `_notInDataWarned` 是全进程累积的，
**这条断言实际上不限于「全池扫」这一趟**（任何更早的 `Badges.For` 都会加数）。

---

## 12. 顺手发现（**不是这 11 条红的原因**，但同批、都属「空判 / 静默」，建议一并立账）

### 12·1 🔴 `W9` 的「动作条数对账」是**自己跟自己比**（空判）

- `BattleDriver.cs:1237`（`FinishReplay`）：
  ```csharp
  if (_replayFt != null) _replayFt.Finish(rec != null && rec.actions != null ? rec.actions.Count : 0, Ctx);
  ```
  而 `RuleEngine/Core/ReplayFingerprintTrace.cs` 的 `Finish(int actualActions)` 是拿它比 `_expected.Count`
  （= `traceFull.Length`，**也是录制侧那一份**）⇒ **除非录像本身畸形，永远 0 mismatch**。
  换句话说：**「这次真演了几条」从来没有被测量过**。
- 断言侧也照抄了这层假口径：`Editor/BattleScene.cs:28134` 打印「录制 … 条 · **实演 {playRec.actions.Count} 条**」、
  `:32333` 打印「实演 {nW9} 条」（`nW9 = recW.traceFull.Length`）—— 两个「实演」都是**录像里的数**。
- **后果**：§6 那个「少演一条」它**一句都没报**（`log:200154` 的 `ActionCountMismatch = 0`、`FirstDivergentIndex = -1`）；
  第 8 条那个「改坏全量指纹」的灭自证也差点因为同一件事失真（它恰好能报出来，因为改的是**存在的**下标）。
- **修法**：`ReplayFingerprintTrace` 加 `ObservedCount`（在 `Observe` 里 +1），`Finish` 改成比它；
  两条断言打印的「实演」改读 `ObservedCount`。⚠️ 这条**独立于** §6 的修复 —— 修完 §6，它才第一次真的有力。

### 12·2 🟡 `HandleReplaySessionExit` 的「点空处」用**按下沿**（见 §5 的 A/B）

判据侧的真值：`ClickExitBattle` 的唯一调用点 = `BattleSettingsWindow__ResignButtonOnClick`
⇒ 原版那一跳是 **uGUI `m_OnClick` = 抬起**。要么改沿（A），要么把「按下」写进白名单并记成有意偏离（B）。

### 12·3 🟡 `_replayTrace` 与 `rec.trace` 的**长度差**从来没人断

`log:191873` 也是「轨迹摘要 差（-1296891905 vs -1931573734）但终局指纹相等」的形状
（17:18 那趟 `sync7` 里同样的位置是相等的）⇒ 那趟大概也少采了样本（只是终局恰好相同）。
建议：在 §21 回放节加一条 `Check(driver.ReplayTraceCountForTest == rec.trace.Count, …)`（现在没有这个读口，
`ReplayCursorForTest` 是**游标**不是**采样数**，两者在「偷游标」时正好不等 —— 用它也能抓，但要读对名字）。

### 12·4 🟢 `W15` 的挂点判据**独立复核通过**

`BattleCardUI__CreateRemnantBody.c:52-75` 现读：`get_position(this.transform)` 与
`Instantiate(prefab, pos, rot, this.transform)` 的 4 个实参 ⇒ 父 = 卡自身、局部 (0,0,0)；
配合既有定论「原版卡根 = 卡底边 / 我们 = 卡心」（`交接_第七十三会话.md:119`）⇒ `From3DBody(0,0,0)` 正确。
**别按 §3 那条红把 W15 改回去**。

---

## 13. 本批回归清单（17:18 `sync7` 5014/0 → 23:23 5151/11）

| 引入者 | 条目 | 类 |
|---|---|---|
| **W20**（组 5 新断言） | §1（④ 断言文案口径错）、§2（⑤ 计数器取错口，与 W10 的代码合起来才红） | 新断言首跑即红 |
| **W20**（组 4 的 R3 出口夹具） | §4（把 `_held` 留在 true 就收工） | 测试夹具缺陷，污染下一节 |
| **W1**（`HandleReplaySessionExit` 新加按下沿） | §5（计数 4 → 5） | 断言过期（+ 沿的取舍待拍） |
| **W1**（`_replayPumping` 重入闸 + 游标先 ++） | §6·§7·§8·§9 | **真缺陷**：回放静默漏演一条 |
| **W15**（残骸体改挂卡底边） | §3 | 断言过期（实现是对的） |
| **W5**（放行 strike/slay） | §10（26→3） | 断言区间过期（行为更精确） |
| **W10**（`BadgeFilterAssert` + `Badges.InPlayIconKeys`） | §11 | 新断言首跑即红（语义错 + doc 自相矛盾） |

> ⚠️ **不是回归的两条要特别说清**：§3 的实现（W15）与 §10 的行为（W5）**都改对了**，
> 红的是**挂在旧数字上的断言**；§11 更是**断言第一次跑**。
> 真正需要改代码的只有 **§2（1 行）** 与 **§6~9（1 处 guard 位置 + 1 条断言）**。

---

## 14. 复核建议（主对话同步点）

1. 按 §13 把修法分给代理时**按文件切**（铁律 13·3）：
   - `Battle/AttackSelector.cs`（§2）· `Battle/BattleDriver.cs`（§6~9 + §5-A 若选 A）·
     `Editor/BattleScene.cs`（§1/§3/§4/§5/§10/§11）· `Editor/BadgeFilterAssert.cs`（§11）·
     `Core/Badges.cs`（§11 doc）· `Core/CardView.cs`（§3 的公开读口）· `RuleEngine/Core/ReplayFingerprintTrace.cs`（§12·1）
   —— **`Editor/BattleScene.cs` 一个人独占**（§1/§3/§4/§5/§10 五处都在这一个文件里）。
2. 修完**只需重跑 `BattleScene.Run`**（全部改动都在这一个自检宿主的覆盖面内）；`RuleEngineTest.Run` 只有在动了
   `RuleEngine/`（§12·1）时才加跑。
3. 验 §6 的红绿**只看两个点**：`log` 里 `[Replay] 演完了` 那一行要变成
   `终局指纹 … = 录制时的 … ｜ 回放全量指纹：逐条一致`，且「回放完的终局局面哈希」那条回 ✓。
4. ⚠️ 修 §6 之后 **§12·1 会立刻变成真判据**（`ObservedCount` 才有意义）—— 建议同一批做，否则下一份录像少演时还是报不出来。
