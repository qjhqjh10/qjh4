# W58 · 战斗线 —— No-mulligan 开场独白断言块插入（`W58-INTROASSERT`，1020）

> 执行代理 **W58-INTROASSERT**。任务 = 把 `W58_战斗线_NoMulligan开场独白.md` §4 的断言块落进
> `Editor/BattleScene.cs`，只改这一份文件。
> ⛔ 未改任何正本 / 其它文件；未动 git；未跑 Unity / `*.Run` / 截图。✅ 只跑秒级 typecheck。

---

## 1. 结论

**已插入，`typecheck` 运行时 0 / 编辑器 0。** 四条覆盖全部落地：

| # | 断言 | 灭自证（改哪一处会红） |
|---|---|---|
| ① | 非教程 No-mulligan（遭遇）开局 ⇒ `IntroStage == 0`（自动起播）+ 真开口（`LastEvent=Intro`、气泡在 `IntroFirstForTest` 侧） | 删掉 `BattleDriver.Begin` 无换牌支的 `StartIntroMonologue()` ⇒ 恒 -1，必红 |
| ② | 无换牌路开场白**文本键** = `CardChatIntro` 精确档 / 按阵营档 / `-Chat0`、`-Chat1` 兜底（与 `SpeakIntro` 同口径） | 把调用改成另写一套无键 Speak ⇒ 红 |
| ② | 无换牌路开场白**播 `customSound`**（`LastClipLoaded && LastClipName == entry.SoundAsset`） | `SpeakIntro` 的 `wantCustom` 支改回 voice_lines ⇒ 条目命中时红 |
| ③ | 教程局（`matchType = 100`）⇒ `IntroStage < 0`（**不播**） | `IsTutorialOrCampaignMatch` 删成 `return false;` ⇒ 教程局会起播，本条红 |

- 块尾 `finally` **完整保留**：`drv.mulliganEnabled = savedMulNM;` + `drv.Begin("Ultramarines", "Goff", 20260917);`
  —— 与本节入口状态（`:14458` 那次 `Begin`）逐字一致，后续 ChatPopup 等小节复用同一个 driver。

## 2. 插入位置与行号（现读）

- 位置（符号）：`Editor/BattleScene.cs` 第 14c 节「开局独白」内，
  `Check(drv.IntroStage < 0, $"★ 两条都播完 → 独白自己收尾…")` 之后、该 `{ }` 的闭合 `}` 之前。
- 现读行号：断言块 **`:15169-15251`**（空行 `:15169`；块头 `:15170`；教程断言 `:15237-15243`；`finally` `:15245-15251`）。
- 插入前的锚点行：`:15167-15168`（`Check(drv.IntroStage < 0, …)`），块后接原 ChatPopup 小节 `:15253`。
- 净增：**+83 行 / −0 行**（文件 32916 → 32999 行）。

## 3. 逐句核对（本文件本会话既有改动未受影响）

- 插入点前后已存在的 `T12` / 徽标 / `T18`（`:15137-15154`）/ `A1220` 段落全部逐句读过、未重叠。
- 所用测试口**全部已存在**（未新增 API、未开白名单外文件）：
  - `MakeDeck(List<CardDef>, string, int, string, int gameMode = 0)` —— `BattleScene.cs:32495`（本文件内）。
  - `GameplayVariables.For(GameMode)` —— `RuleEngine/Core/GameplayVariables.cs:380`；`Skirmish.showMulligan = false`（`:308`）。
  - `MatchTypes.For(GameMode)` —— `RuleEngine/Core/BattleContext.cs:1651`（`Skirmish → FastMode 200`）。
  - `BattleDriver.Begin(... seed, myDeck, foeDeck, foeFaction, vars ...)` —— `Battle/BattleDriver.cs:3143-3175`；
    `playMode == null` 时 `Ctx.PlayMode = Vars.IsSkirmish ? Skirmish : Classic`（`:3557`）。
  - `BattleDriver.BeginTutorial(int)` —— `BattleDriver.cs:2944`。
  - `drv.IntroStage` / `drv.IntroFirstForTest` —— `BattleDriver.cs:6245` / `:6286`（本会话工作区已含）。
  - `chat.LastChatTerm` / `LastClipLoaded` / `LastClipName` / `LastEvent` / `ShownSide` / `ShownText` —— `Battle/UnitChatPanel.cs:265,260,262` 等。
  - `CardChatIntro.TryResolve` / `TermFor` / `Entry.SoundAsset` —— 已在本节 `:15050-15063`、`:15145-15151` 用过。
- 已核「正常由 `FinishMulligan` 调」的补充调用点：
  `BattleDriver.Begin` 无换牌支 `if (!Ctx.Vars.showMulligan) StartIntroMonologue();` —— 现读 `BattleDriver.cs:3807-3808`。
- 闸门：`StartIntroMonologue` 内 `if (Ctx == null || IsTutorialOrCampaignMatch(Ctx.MatchType)) return;`（`BattleDriver.cs:6263`）。

## 4. grep 证据

```
15170:  // ---- 🆕 2026-10-20（战斗线 `W58-INTROFIX`）：**非教程的 No-mulligan 模式也播开场独白** ----
15194:  Check(!drv.Ctx.Vars.showMulligan && !drv.Ctx.MulliganOpen
15195:        && drv.Ctx.MatchType == MatchTypes.For(GameMode.Skirmish), …
15201:  Check(drv.IntroStage == 0, "★ 非教程 No-mulligan（遭遇）开局 ⇒ 开场独白**自动起播**"
15210:  Check(spokeNM, "★ 非教程 No-mulligan 也**真的开口**…
15225:  Check(chat.LastChatTerm == wantTermNM, "★ 无换牌路的开场白**文本键**照旧…
15229:  Check(chat.LastClipLoaded && chat.LastClipName == eNM.SoundAsset, "★ 无换牌路的开场白**播的是 customSound**…
15237:  drv.BeginTutorial(0);
15239:  && drv.Ctx.MatchType == RuleEngine.MatchType.Tutorial,   ← 歧义修正（见 §6）
15241:  Check(drv.IntroStage < 0, "★ 教程局 ⇒ **不播**开场独白…
15245:  finally
15248:  drv.mulliganEnabled = savedMulNM;
15249:  drv.Begin("Ultramarines", "Goff", 20260917);   ← 还原，后续小节复用同一 driver
```

## 5. 缺的口

**无。** 块所需的口全部已存在（见 §3），无需改白名单外文件、无需新增 API。

## 6. 一处必要的就地修正（不是偏离口径）

源块写 `drv.Ctx.MatchType == MatchType.Tutorial`。本文件已有 `using System.IO;`，
.NET 里存在 `System.IO.MatchType` ⇒ **CS0104 二义**（第一次 typecheck 实测：编辑器错误数 1）。
改为 `RuleEngine.MatchType.Tutorial`（与本文件既有写法 `:23894` 一致），语义逐字不变。

## 7. 验收

```
TMPDIR=/tmp/wf_introa bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 行尾：**CRLF 32999 / LF 32999 / bare LF 0**（插入前后均纯 CRLF，未翻行尾）。
- `git diff --numstat`（该文件，含本会话此前未提交改动）：`973 / 36`；
  本任务净增 **+83 / −0**（T12 / 徽标 / T18 / A1220 等改动在任务开始前已存在）。
- ⛔ 未跑 `BattleScene.Run` / 任何 `*.Run`、未开 Unity、未截图（铁律 12）。
