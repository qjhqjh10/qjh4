# `B117-FIXB` · 大卡展示窗 / 效果清单一簇（22 条 BT 红）—— 根因与修复

> 现读输入：`d:/4/_tmp_view/battle.log`（113 MB · 2026-11-02 13:52）。
> 失败簇 = 日志 `712052 … 713533` 共 **22 条**（与调度台给的清单逐条对上）。
> 结论一句话：**不是窗开不出来，是窗在这一节开始时【本来就开着】** ——
> `B117-48` 把「点战斗日志行」的终点从小预览卡换成了大卡窗，
> 而那段自检的**收尾没有把大卡窗关回去** ⇒ 下游「棋盘轻点开/关窗」整节**每一条都错一位**。

---

## 一、根因（一处 · 已被三条独立证据锁定）

### 1·1 直接证据：窗是被【日志点行】打开后没人关

`battle.log` 里这一节之前**唯一**一次「大卡窗」开启，发生在日志块：

```
707266  [Battle] 点日志第 1 行 ⇒ **开大卡信息窗**（…ClickCardLink → BattleManager.DisplayCard(card,1) → CardDisplayWindow.ShowBattleCard）
707302  [展示窗] 开窗动画：1 格从 (0,60)/0°/scale210 补到扇形终态 …
707467  BT    [图] d:/4/_tmp_view/battle/02b3_日志点行弹卡.png
712052  BT    ✗ 轻点我方单位 → 开大卡展示窗
```

中间**没有任何一次 `Hide()`**（`Hide()` 不打日志，但同一段里也没有第二条开窗记录，
说明这一开就是一直开着到 9737）。

### 1·2 代码侧：`ClickLogRow` 换了落点，但收尾没换

`Assets/CardPresentation/Battle/BattleDriver.cs:12535` 现在是：

```csharp
ShowLogCardLarge(key);     // → BattleDriver.cs:40572 _cardDisplay.ShowBattleCard(...)  ⇒ 大卡窗**开着**
HideLogCard();             // 只收「悬停用小预览卡」（LogHoverCard）
_logPanel.Hide();          // BattleDriver.cs:12549 只关日志面板
```

`git diff` 证明这一行是本波换的（换之前那一条**不碰**大卡窗）：

```
-            ShowLogCard(key, row);      // 小预览卡 226×359.8，只画 LogHoverCard
+            ShowLogCardLarge(key);      // 大卡窗 CardDisplayWindow.ShowBattleCard
```

原版这一条**也确实是把窗留着**（不是我们写错）：
`d:/2/tools/decomp_full/CemeteryManager__ClickCardLink.c` 尾段
`BattleManager__DisplayCard(lVar6, uVar9, 1, 0, 0)` → `BattleManager__DisplayCard.c` 尾段
`CardDisplayWindow__ShowBattleCard(...)`；随后的
`CemeteryManager__HideCemeteryLogBtn.c` 只 `SetActive(false)` 掉 `+0x50`（面板）与 `+0x58`
（**悬停**那张 `showingCardUI`），**没有一处动大卡窗**。
⇒ 生产行为正确，**缺的是自检夹具的收尾**。

### 1·3 症状形状：整节「开/关」全部反相，而不是「开不出来」

日志里每一次轻点都印了三态，全部是 `NotBlockedButNotOpened`（**没有任何前置闸**），
而这正是「`Toggle` 走了**关**那一支」的读数 —— `CardDisplayWindow.Toggle`
（`Battle/CardDisplayWindow.cs:1243-1247`）是 `if (Visible) Hide(); else Show(...)`；
窗本来就开着 ⇒ 第一下**关上**（`HideCore` 把 `ShownTitle` 置 null，所以紧跟的那句
`窗里显示的是：「」`）、第二下**打开** ⇒ 后面每一对「开窗 / 关窗」断言逐条反相。
`EffectRowCount` 实测 0 也是同一个原因（`HideCore` 里 `SetEffectRows(null)`）。

把日志按时间序排一遍就是完美的交错：

| 断言 | 期望 | 实际 | 真实状态 |
|---|---|---|---|
| 9737 轻点我方单位 → 开大卡窗 | 开 | 关 | 本来就是开 ⇒ 这一下把它关了 |
| 9743 再轻点一次 → 关窗 | 关 | 开 | 这一下才是开 |
| 9757 有 buff 的单位 → 照样开窗 | 开 | 关 | 又一次反相 |
| 712790 态 A 开窗 / 712712 再点一次关掉 / 713183 轻点对手单位 … | —— | —— | 同一条链一路反相到 713533 |

**22 条全部是这一条的级联，没有第二条独立成因。**

### 1·4 为什么上一轮（S118）是绿的

S118 那一轮 `ClickLogRow` 调的是 `ShowLogCard`（小预览卡，`HideLogCard()` 当场收掉），
**大卡窗从头到尾没被打开过** ⇒ 9737 那一节的前提（「本来是关着的」）自然成立。
本波把落点换成大卡窗之后，前提被破坏 —— **断言本身没有过期，是夹具前提没跟着改**。

---

## 二、改了什么（`BattleScene.cs` 不在本代理白名单内 ⇒ 交**可直接粘贴的补丁**）

红线要求：⛔ 不许改 `Editor/*Scene.cs`；且 `账/*.tsv` 里对 `Editor/BattleScene.cs` 有
**61 处 `文件:行号` 引用，其中 44 处在本行之后** ⇒ **必须零行数变化**，
否则 `check_ledger_points` 会集体报漂移。

### 补丁（零行数 · 改一行）

文件 `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`，
**第 7254 行**（现读 `                log.Hide();`，该行**没有行尾 `//` 注释**）——

```diff
-                log.Hide();
+                log.Hide(); if (drv.CardDisplay != null) drv.CardDisplay.Hide();   // 🔴 B117-FIXB：B117-48 之后「点日志行」的终点是大卡窗且**留着**（原版同），本段收尾必须把它关回去，否则下游「棋盘轻点」整节每条断言错一位
```

- 该行与 7255（`Check(!log.Visible, …)`）都**不是**账的 A 档落点（不是声明行），改它零风险。
- 备选落点：第 7255 行行尾（同样无 `//`）。
- ⛔ **不要新起一行**（那会让 7252 之后 44 处账落点整体 +1）。

### 调用链自证（补丁为什么能编译、为什么安全）

- `drv` = `Object.FindObjectOfType<BattleDriver>()`（`BattleScene.cs:6947`）—— 与 `RunCore` 里
  `driver`（`:9737` 用的那个）是**同一个单例**；`drv.CardDisplay` 在 `:7224` 已经在用。
- `BattleDriver.CardDisplay`（`BattleDriver.cs:2339`）→ `_cardDisplay`（`:29547` 建）。
- `CardDisplayWindow.Hide()` 是 `public`（`CardDisplayWindow.cs:1342`）→ `HideCore(true)`
  → `Visible = false` + `ShownTitle = null` + `SetEffectRows(null)`；
  **已经关着时是幂等的**（关窗音那一句 `if (playSound && Visible)` 不会响）。
- 7240–7270 这一段 **0 条**账落点（`grep -oh "Editor/BattleScene\.cs:[0-9]*" 账/*.tsv` 现核）。

### 为什么这是「夹具收尾」而不是「改断言」

- 7224-7226 那条断言（`CardDisplay.Visible && ShownTitle == wantKey && LogHoverCardKey 为空`）
  **一个字没动**，仍然要求「点行 ⇒ 大卡窗开着」；
- 9737 那一节的前提（「本来是关着的」）也一个字没动；
- 补的回的是**两节之间**没人管的那一份状态。

---

## 三、改完应该转绿的清单（22 条，按日志行号）

```
712052 轻点我方单位 → 开大卡展示窗
712220 再轻点一次 → 关窗
712246 有 buff 的单位 → 照样开窗
712259 …而且效果清单露了 2 行
712272 第 1 行「谁给的」= 施加者真卡名（SourceCard）
712285 第 1 行「给了什么」
712298 第 2 行是关键词（GetTraitOneTurn + traitValue）
712712 …再点一次关掉
712790 （态 A · 5 条）开窗
712803 ★ 5 条 buff ⇒ 5 行都露出来
712829 ★ 后加的 3 条按序落进后 3 个槽
712842 ★ 5 个槽的「给了什么」两两不同
712963 …收尾：窗关回去
712989 （态 B · 6 条）再开窗
713002 ★ 6 条 buff ⇒ 仍然只画 5 行
713015 ★ 第 6 条一个槽都没进去、也没挤掉任何一条
713131 …收尾：窗再关回去
713183 轻点对手单位 → 也开大卡窗
713278 …再点一次关上
713304 …这时不开大卡窗（拖过的松手不算轻点）
713481 normalBattle ⇒ 开窗闸为假（轻点手牌照旧能开大卡窗）
713533 关窗后 ⇒ 开窗闸恢复为假
```

⚠️ **验证方式**：这一簇的每一条都带三态读数，跑完看
**凡是断「开窗 / 照样开窗 / 也开大卡窗」的那几格，`[自检·轻点] … 三态 = NotBlockedAndOpened`**，
而 `NotBlockedButNotOpened` 只应出现在「本来就该不开」的那几格（本次 22 条红**全部**印的是后者）。
**不要只看断言合计**——本条真正的判据是**开关相序**。

---

## 四、typecheck

`TMPDIR=/tmp/wf_fixb bash d:/4/Unity/工具/typecheck.sh`
⇒ 运行时错误数 **0** · 编辑器错误数 **0**。
（本代理**没有改任何源码**，这一步证明的是「补丁不涉及编译面 / 工作树当前是编得过的」。）

---

## 五、仍红的 / 边界（如实记）

1. **本代理零代码改动**：根因在 `Editor/BattleScene.cs` 的夹具收尾，属白名单外 ⇒ 按
   铁律 13·3「撞上了就别硬改，写可粘贴补丁交回」处理。补丁见 §二。
2. **同一段里的另外两条红不属于本簇**（日志 `707402` / `707454`）——
   `★ 点行 ⇒ 大卡信息窗显示的就是那一行那张卡（猎鹰 / 期望 Falcon）`：
   `ShownTitle` 是**中文本地化名**、`RowLinkKey` 是**英文键**。
   那是「显示名 vs 键名」的判据问题（`B117-WREPLAY` 那一支），**与本簇无关**。
   ⚠️ 但两件事**必须在同一次运行里一起验**：日志里 `707454 换一行再点` 也红了（原因未查，属那一支），
   而 `02b3_日志点行弹卡.png` 只拍到第一张卡 —— 拍图这一步不影响本簇（窗开没开与拍几张无关）。
3. **没有第二条独立成因**（已排查）：
   - `CardDisplayWindow.SetChrome`（`G10-13`）只改 `_voiceBtn` 的 `SetActive` 方向，
     不碰 `Visible` / `gameObject.SetActive` ⇒ 与开窗无关；
   - `EndPanel.cs` / `SkillPanel.cs` 的 diff **零处**引用 `CardDisplay` ⇒ 没有「抢窗口单例」；
   - `HideCore` 只被 `Show`（清理）与 `Hide()` 调用，没有任何「别人顺手关掉」的路径。
4. **残留风险（未跑 Unity，只能写出来）**：本簇 22 条里 6 条断的是**槽位内容与相异**
   （712829/712842/713002/713015）。它们在本波**没有改动过的路径**上（`SetEffectRows` / `EffectsToShow`
   这一波没动），S118 是绿的 ⇒ 复位后预期回绿；若仍红，则是一条**独立的**效果清单缺陷，需另开一单
   （本代理没在日志里看到任何 `CardDisplayWindow.cs:433` 的越界告警，说明 `SetEffectRows` 这条路
   **根本没被走到**——这也反证了「窗是关着的」）。
