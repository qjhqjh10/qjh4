# FA · BattleScene 断言质量修（`R1` §② 的 ②-4/②-5/②-6/②-7 + `G1-7` + `W35`邻句）（修波）

> 执行：修波子代理 `FA` · 2026-10-16 · 白名单 = `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` + 本报告。
> ⛔ **未跑任何 `*.Run`**（本文件就是自检宿主 —— 只改不跑，现行口径铁律 12）· ⛔ 未起 player · ⛔ 未动 git · ⛔ 白名单外一字节未改。
> ✅ `TMPDIR=/tmp/wf_fa bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 · 编辑器 0 错**（改完每批各跑一次，共 2 次）。
> ✅ **行尾**：改前 `CRLF 25014 / LF 25014` · 改后 `CRLF 25162 / LF 25162`（**CR-only 0**）⇒ **没翻行尾**。
> ✅ `git diff --numstat -- MyGame/Assets/CardPresentation/Editor/BattleScene.cs` = **578 / 60**（`G2` 收工时是 428 / 58 ⇒ 本波净增 150 行 / 少 2 行；**远小于 25,162 行 ⇒ 不是整篇重写**）。
> ⚠️ 行号 = **本报告落盘这一刻的现读**（`grep -n` 可复现）。本文件改动会漂行号 ⇒ 复核请先 `grep -n` 对一眼。

---

## ① 结论 —— 逐条「仍成立 ⇒ 改了 / 已不成立 ⇒ 不动」

| # | `R1` 原判 | **现核（本报告落盘时）** | 处置 |
|---|---|---|---|
| **②-3** | `A3⑦` 探针 `AddComponent` 后没 `Build()`；且 `wHalf964` 恒真 | **前半已不成立**：`probA3964.Build();` **在**（`:21404`，`G2` 自报属实）。**后半仍成立**：`bool wHalf964 = !probA3964.UguiMode && probA3964.CanvasRect == null;`（`:21405`）对任何新建组件都真 | **不动**（① `G2` 已按 `R1` 的「显式 `Build()`」改完；② 后半 `G2` **有意不改**并写了理由 —— 见 §⑤·2；③ 本波任务只要求现核这一条） |
| **②-4** | `W58②` 判别式的分支条件取自被测实现自己 ⇒ 注释里「把 `CanvasPx` 换成写死 1080 ⇒ 同理红」不成立 | **仍成立**。现核原文（改前）：`bool cv16x9A2 = Mathf.Abs(chA2 - 1080f) < 1f;`，而 `float cwA2 = sp.CanvasPxSize.x, chA2 = sp.CanvasPxSize.y;`（`:7203`）⇒ `CanvasPxSize ⇒ CanvasPx ⇒ _cv.rect`（`SkillPanel.cs:626-641`） | ✅ **改了**（`:7239-7251`） |
| **②-5** | 两处 `UguiMode == (CanvasRect != null)` 是同义反复 | **一真一假，`R1` 把两条并成一条了**：<br>· 技能面板那条（`:7168`）**确实同源**（`SkillPanel.cs:294` `_cv != null` / `:297` `_cv`）⇒ 恒真；🔴 **但 `G2` 已如实标注成「回归哨兵、判别力弱、真判别力在 `W11⑥` 与 ②–⑤」**（`:7164-7167`），且同处 `W11⑥`（`:7195` 逐件 `OnCanvas`）与 ②–⑤ 是真判别力 ⇒ 按任务给的判据（**已标注 + 同处有真判别力 ⇒ 可留**）**留**。<br>· 攻击选择器那条（`:21337`）**不是同义反复**：`AttackSelector.cs:403` = `_frame != null`、`:406` = `_frame != null ? _host : null`，**两个口不同源**（能抓「帧建到画布上但 `_host` 丢了」）⇒ `R1` §②-5 引 `AttackSelector.cs:403/406` 说「返回的也是 `_cv`」**引错了**，与 `G2` 报告 §④ `W32`① 的说法一致 | ⚠️ **两处 Check 都留**；🔴 只**就地补注**：「本条不是同义反复 + 谁同源谁不同源 + 出处行号」（`:21336-21345`） |
| **②-6** | `:21215`（`A3④`）期望值写的是我们的常量 `621.7 / 122.2` | **仍成立**。现核原文（改前）：`Check(Mathf.Abs(fs964.x - 621.7f) < 0.5f && Mathf.Abs(fs964.y - 122.2f) < 0.5f, …)` | ✅ **改了**（`:21375-21380`） |
| **②-7** | `SubCanvasRank` 的跨画布次序**零断言** | **仍成立**。现核：全仓 `SubCanvasRank` 只在 `HudCanvas.cs`（表 + `ApplySubCanvasRank`）与三处**注释**里出现（`BattleScene.cs:7159`/`:21347`、`BattleDriver.cs:14610`、`ChatPopupPanel.cs:46`）；`BattleScene.cs` 里 `GetSiblingIndex(` 此前只有一处（`sel964.FrameRect` **在宿主容器内**的位次） | ✅ **补了一组 4 条**（`:20851-20925`） |
| **`G1-7`** | `evs.Append(ForChatButton[i])` 可能印错 token（只进失败文案） | **仍成立**（不是「印错」，是**含义被读错**）：现核 `BattleDriver.cs:4202` 已改成 `TryPick(id, new[] { ForChatButton[idx], "default" }, …)` ⇒ 第二跳命中时**实际 ev = `"default"`**，而断言仍印主 token | ✅ **改了文案 + 钮号**（`:12498-12511`）；真 ev 那半进 §③ 接线清单 |
| **`W35` 邻句** | 「今天 5 条」现读应为 1 条 | **仍成立**。现核原文（改前）：`//    **判别力没全丢**：灭自证那一角由 **①（\`exDiff > 0\`，今天 5 条）** 独担，`；而 `Check(exDiff > 0, …)` 本身在 `:12085` | ✅ **只改那个数字 → `今天 1 条`** + 留痕（`:12097-12104`）；`:12085` 那条**一个字节没动**（台账 `W35` 标「⛔ 别改」，`G2` 也遵守了） |

**一句话**：`R1` §② 里**落在本文件**的 5 条（②-4 / ②-5 / ②-6 / ②-7 / `W35`邻句）＋ `G1-7` **全部处理**；
其中 ②-5 **两条只留不改**（一条被 `R1` 误并进同义反复、一条 `G2` 已标注），②-3 **现核确认 `G2` 已改**。

---

## ② 改动清单（`文件 : 行号` + 「改前 → 改后」）

> 全部在 `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`。行号 = 现读。

| 行 | 台账 | 改前 → 改后 |
|---|---|---|
| `:7230-7233`（注释）· `:7234-7237` | `R1` ②-4 | 注释里「`cam.aspect` 钉 16:9」→「`CanvasScaler` 的 `Expand` 档」；补一段**订正痕**（为什么原来的分支条件没有判别力） |
| `:7239-7245` | `R1` ②-4 | **删** `bool cv16x9A2 = Mathf.Abs(chA2 - 1080f) < 1f;`（`chA2` = 被测件自己的 `CanvasPxSize`）<br>**换**：`float indHA2 = HudCanvas.RootRect.rect.height`（**判据侧**）＋ `float selfHA2 = sp.CanvasRect.rect.height`（**原始**读数）＋ `bool bothMeasuredA2 = indHA2 ≥ 1 && selfHA2 ≥ 1`＋ `bool cv16x9A2 = !bothMeasuredA2 \|\| Mathf.Abs(indHA2 - 1080f) < 1f;` |
| `:7246-7251` | `R1` ②-4 | 断言文案：把**两个读数 + 实现报的值**都印出来（`判据侧 {indHA2} / 被测件 {selfHA2} / 实现报 {chA2}`），分支语义写成「两边量得到时按 16:9 断（量不到 ⇒ 不判别，⛔ 不假红）」 |
| `:21336-21345` | `R1` ②-5 | **新增注释**（不改 Check 语义）：本条**不是**同义反复（两个口不同源，出处 `AttackSelector.cs:403/406`）＋ 指出「真同源那条在技能面板 `:7168`，`G2` 已标注、同处有 `W11⑥`」 |
| `:21346-21349` | `R1` ②-5 | 断言消息尾部**追加**一句：`｜两**不同源**：… ⇒ 抓得到「帧在画布上但 `_host` 没了」`（纯文案，**判据不变**） |
| `:21371-21380` | `R1` ②-6 | `Mathf.Abs(fs964.x - 621.7f) < 0.5f && Mathf.Abs(fs964.y - 122.2f) < 0.5f` → **`… - 621.7249755859375f) < 0.1f && … - 122.24700164794922f) < 0.1f`** ＋ 上面 8 行注释（判据 = 原版 `RT_3166.m_SizeDelta` 现读；**如实记本条今天仍绿**） |
| `:20851-20925` | `R1` ②-7 | **新增一组 4 条**（详 §④）：① 前提·根画布在 ② 前提·本批 5 块子画布都在（`nRankA2 >= 5`）③ **灭自证·名字**（`HudCanvas.IsRanked`）④ **次序**（真实 `GetSiblingIndex()` 升序 == 原版 14 子件序）。落点 = `A964` 段 `drv964.Begin(...)` 之后（`Begin` 里刚跑过 `BuildHud()` ⇒ 排次刚重排过一次；且该段**不带面板可见性那种闸**） |
| `:12498-12508` | `G1-7` | `evs.Append(VoiceLines.ForChatButton[i]).Append(' ')` → `evs.Append($"#{i + 1}={VoiceLines.ForChatButton[i]}").Append(' ')` ＋ 6 行注释（为什么要说清「这是主 token、不是实际 ev」，与接线清单） |
| `:12510-12513` | `G1-7` | 文案 `（实得 {spoke}/6：{evs}）` → `（实得 {spoke}/6）—— 逐钮主 token：{evs}（⚠️ 印的是**钮 → `VoiceLines.ForChatButton` 那个主 token**，**不是**实际播的 ev：… `\"default\"` 第二跳（台账 `W36`）⇒ 这串只作「哪几颗收下了」的标识，⛔ 别拿它当 ev 读数）` |
| `:12097` | `W35` 邻句 | `①（\`exDiff > 0\`，**今天 5 条**）` → `①（\`exDiff > 0\`，**今天 1 条**）` |
| `:12100-12104` | `W35` 邻句 | **新增留痕**：写明「原文写 5 条、现读 1 条」＋ 出处（`_收口台账_第四十二会话.md:175`、`语音线A1768_报告.md:96`）＋ 「`Check(exDiff > 0, …)` 本身按台账 ⛔ 别改、一个字节没动」 |

### 行尾核对

```
$ git diff --numstat -- MyGame/Assets/CardPresentation/Editor/BattleScene.cs
578     60      Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs
$ python -c "…b.count(b'\r\n'), b.count(b'\n')…"
CRLF 25162 LF 25162   CR-only 0        # 改前 25014/25014（G2 交班值）⇒ 仍是纯 CRLF，没翻
```

---

## ③ 接线清单（白名单外 —— 我一个字没改）

| 落点 | 要改什么 | 与哪条断言有关 | 现核状态 |
|---|---|---|---|
| `Battle/HudCanvas.cs` **`public static bool IsRanked(string)`** | `FD` 的账 `G1-5`：给 `SubCanvasRank` 开只读判据口 | 🔴 **②-7 的「灭自证·名字」那条要它** | ✅ **已落地**（本波现核读过实现与 doc）⇒ 我这条**可以直接用**，不再是「等它落地才写」 |
| `Battle/AttackSelector.cs`（`FrameName` 之侧，`R1` 报 `:388`） | `G1-1`：补 `public const string SubCanvasName = "Drag Attack Selector Canvas";` | ⚠️ ②-7 里那一项**今天仍写字面量**（字面量逐字节 == 表里那条，本波核过） | ❌ **未落地**（`grep -n "SubCanvasName" Battle/AttackSelector.cs` 零命中）⇒ 落地后把 ②-7 的第 1 项改成引用它（⛔ 别在两处再写一遍这个串） |
| `Battle/MultiCardDisplay.cs` | `const string SubCanvasName` 现在是**非 public**（同 `Battle` 程序集内可见、`Editor` 程序集看不到）⇒ ②-7 里那一项也写字面量 | 同上 | ❌ 未落地（`FD` 的 §6「4 处 `SubCanvas+EnsureSubCanvas` 包装」是另一件） |
| `Battle/BattleDriver.cs`（`SpeakChat` 旁）或 `Battle/UnitChatPanel.cs` | `G1-7` 的**精确版**：开一个只读口报「**上一次真播的 ev**」（`UnitChatPanel.Speak` 收的是合成名 `"Chat"+idx`，不是 `TryPick` 的 `out ev`） | `G1-7` 的文案那半 | ❌ 未落地 ⇒ 本波只做到「把含义写清、⛔ 不重写一遍 `TryPick` 那条链」（那是「第二份写法」） |
| `Battle/BattleDriver.cs:14674-14676` 的 doc | 它写着「想把它变成真检查，得让 `HudCanvas` 开一个 `public static bool IsRanked(string name)`（📌 已记进接线清单）」 | —— | ⚠️ **已过期**（`IsRanked` 本波现核**已存在**）⇒ 建议 `FD`/`G1` 就地订正并在 `EnsureRankedSubCanvas` 里真用它（⛔ 不在我白名单） |
| `R1` §②-8 `Shell/AlliancePanelWindow.cs:707-716` | 文件末「入口」那段「待接线」三件今天都做完了 ⇒ 改成「✅ 已接线（出处）」+ 留痕 | —— | ❌ 未做（**白名单外**，本波没人认领） |
| `R1` §②-1 / ②-9 / ②-10 / ②-11 | 分别在 `MultiCardDisplay` / `AlliancePanelWindow` / 三个面板文件 | —— | ❌ 未做（**白名单外**） |

---

## ④ 判据与出处 —— 每条**新/改**断言的**期望值来源** + **什么条件下会红**

> 通则（同上批）：期望值一律取**判据侧**（原版 JSON / 原版子件序），⛔ 不读被测实现自己的常量。**本波不跑**，所以下表是唯一可核处。

### ②-4 `W58②` 判别式（`:7239-7251`，改）

| 项 | 内容 |
|---|---|
| **期望值来源** | **原版 `RT_3104`** 的锚点五元组（`aMin 0.665576696395874,0` · `aMax 0.965788722038269,0.3040129840373993` · `sd −0.0003662100061774254,−3.509999990463257`）⇒ 16:9 档的面板高 = `0.3040129840373993×1080 − 3.51` ≈ **324.824**（常量 `324.82` 是它的舍入）。**分支**那一侧取 `HudCanvas.RootRect.rect.height`（**根画布**，判据侧；`SkillPanel.CanvasPx` 自己的 doc 就写着「期望值要取原版锚点 + `HudCanvas.RootRect.rect`」） |
| **红** | ① **两边都量得到（≥1）且根画布高 ≠ 1080** ⇒ 走非 16:9 支、要求 `\|面板高 − 324.82\| > 10`：<br> · 把 `PanelCvRect` 换回写死 `PanelPxW/H`(324.82) ⇒ 红；<br> · **把 `CanvasPx` 换成写死 1080** ⇒ 实现报 324.82 而根画布仍 1440 ⇒ **红**（这正是改前**抓不到**的那一条）。<br>② 两边都量得到且根画布 == 1080 ⇒ 走 16:9 支、要求 ≈324.82 ⇒ 面板高写错/锚点常量抄错一位 ⇒ 红 |
| **绿** | 实现正确 ⇒ 两支各自成立。⚠️ **`rect` 退化（0×0）⇒ `bothMeasuredA2 == false` ⇒ 退回 16:9 支**（与 `SkillPanel.CanvasPx` 的退档口径同一条）⇒ **不假红**（这是本波专门加的护栏）。⚠️ 断言**仍可能被环境弄成「无判别力的绿」**（那两个读数都退化成 0×0 时），但**不会假红** |

### ②-6 `A3④` 帧尺寸（`:21371-21380`，改）

| 项 | 内容 |
|---|---|
| **期望值来源** | **原版 `bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3166.json` 的 `m_SizeDelta = (621.7249755859375, 122.24700164794922)`**（本波自己现读，逐位）。⛔ 不取 `AttackSelector.BarW/BarH`（实现自己的常量） |
| **红** | `\|fs964.x − 621.7249755859375\| ≥ 0.1` 或 `\|fs964.y − 122.24700164794922\| ≥ 0.1` ⇒ 🧨 帧尺寸常量改一位 · `SetPxSize` 那条被换成世界单位（÷108，会小 108 倍） |
| **绿** | 实现用 `621.7 / 122.2` ⇒ 差 `0.025 / 0.047`，**都 < 0.1** ⇒ **今天仍绿**（本波**如实记**：收窄容差**没有**把它变成红，这一笔改的是「期望值取自谁」） |

### ②-7 子画布跨画布次序（`:20851-20925`，**新增 4 条**）

| # | 断言 | 期望值来源 | 红 / 绿 |
|---|---|---|---|
| ① | `HudCanvas.RootRect != null` | ——（前提） | 🔴 `HudCanvas.Ensure` 没跑过 ⇒ 红 |
| ② | `nRankA2 >= 5`（本批迁的 5 块子画布都在根画布下） | `BattleDriver.BuildHud()` 里那 5 句 `UseHost`/`EnsureRankedSubCanvas`（现核都在） | 🔴 少一块 = 那扇面板没登记宿主 / `EnsureSubCanvas` 返 `null` ⇒ 红。⚠️ **这一条带一个残余风险**：若场景挂的 `selector` / `skillPanel` 为 null（`BuildHud` 里那两处 `if`），台上只有 4 块 ⇒ 红（见 §⑤·1） |
| ③ | **灭自证·名字**：上面那几块的名字**都在** `HudCanvas.SubCanvasRank` 表里（`HudCanvas.IsRanked`，`offTableA2 == 0`） | **`HudCanvas.SubCanvasRank` 自身**（这一条**就是**在问「名字有没有对上表」——它没有别的判据侧；`IsRanked` 是 `FD` 本波新开的只读口，账 `G1-5`） | 🔴 把表里/调用点任一侧的名字写错一个字符 ⇒ 红。**绿的判据**：10 个候选串本波逐字比过 `SubCanvasRank`（4 个取面板自己的 `SubCanvasName` 常量、6 个现写字面量） |
| ④ | **次序**：真实兄弟序升序 == **原版 `Safe area FrontCanvas` 的 14 子件序**（`badA2 == 0`，且 `nRankA2 >= 2`） | **原版 `bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3055.json` 的 `m_Children`（14 格，本波自己现读，逐格 pathID 解出 GO 名）**：`[0] Drag Attack Selector · [1] ActiveSkillDesc · [2] Unit Chat · [3] Mulligan · [4] Generic Multi Card Display Combat · [5] ChooseCardMenu · [6] Card Display Window · [7] DebugButtons · [8] AboveShader · [9] PopupAnchor · [10] BattleSettingsPanel · [11] Tutorial · [12] LoadingMenu · [13] Alliance Panel`。⚠️ `ChatPopup` **不在 14 格里**（它是 `Unit Chat` 的子件，原版 `RT_3280` 的 4/7）⇒ 照「子件画在父件之上」排在 `Unit Chat` 之后、`Mulligan`(3) 之前 | 🔴 **① 删掉 `ApplySubCanvasRank()` 里那句 `t.SetSiblingIndex(idx)`** ⇒ 兄弟序退回**建序**（= `BuildHud` 调用序：`AboveShader` → 技能面板 → 单聊 → 选择器 → 联盟 → 聊天弹窗）⇒ 第 1 对 `Drag Attack Selector Canvas` vs `ActiveSkillDesc Canvas` 当场反了 ⇒ 红；<br>🔴 **② 表里任一项名字写错一个字符** ⇒ 它拿不到最小位次 ⇒ 它前后那一对红；<br>⚠️ **只断「已建出来的那些」之间的相对次序**（缺席的格子既不断它自己的位次、也不当红的理由 —— 逐格比是**假红**）。<br>**绿**：`ApplySubCanvasRank` 正常跑 ⇒ 排到末尾那一段按表升序 ⇒ 与原版子件序同序 |

### `G1-7`（`:12498-12513`，改 · **纯文案**）

| 项 | 内容 |
|---|---|
| **期望值来源** | `VoiceLines.ForChatButton`（`Core/VoiceLines.cs:373-374` = `{ "greet","threat","wp","gen1","gen2","gen3" }`，doc 写死「下标 0…5 依次 = `Greet/Threat/WellPlayed/Taunt/Sorry/Oops`」）＋ `BattleDriver.SpeakChat`（现读 `:4202` 的 `TryPick(id, new[] { ForChatButton[idx], "default" }, …)`） |
| **红/绿** | ⚠️ **改的是失败文案，判绿红的那一半一个字没动**（`spoke == 6` 只看 `ChatClickAt` 的返回值）。改后**不可能**让原来绿的变红、也不可能让原来红的变绿 |
| ⛔ **没做的** | 让文案印**真 ev**（要 `TryPick` 的 `out ev` ⇒ 得先知道「哪个督军在说」⇒ 要一个只读口，见 §③）。⛔ **没在断言里重写一遍 `TryPick` 那条链**（= 第二份写法） |

### `W35` 邻句（`:12097` + `:12100-12104`，改 · **纯文案**）

| 项 | 内容 |
|---|---|
| **期望值来源** | 台账 `资料/普查产出_1016并行/_收口台账_第四十二会话.md:175`（「那条『灭自证①』`exDiff > 0` 现在实得 **1 条**（改前 5 条）」）＋ `语音线A1768_报告.md:96` 同一说法（**本波没跑自检 ⇒ 这个 1 不是我自己数出来的**，见 §⑤·3） |
| **红/绿** | **纯注释**。`:12085` 的 `Check(exDiff > 0, …)` 一个字节没动（台账 `W35` 标「⛔ 别改」） |

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. 🔴 **②-7 的 ② 号断言（`nRankA2 >= 5`）在「场景挂的 `selector`/`skillPanel` 为 null」时会红** —— 本波**没跑**，格不到那个前提。
   现核到的旁证：`BuildHud` 里 `if (skillPanel != null)` / `if (selector != null)` **各有一道闸**（两处都 `Debug.LogError` 出声），
   而别处的自检有 `Check(sel964 != null, "★ A964/模型③：（前提）攻击选择器在")` ⇒ 正常路径下两件都在、`nRankA2` 应 ≥ 6。
   ⛔ **没把它降成「≥2」**（那样这条就没了判别力）；**如实登记**：若将来真机上它红了，先看 `BuildHud` 那两句 error、再看那两块子画布建没建出来。
2. ⚠️ **`R1` §②-3 的后半（`wHalf964` 恒真）我没改** —— `G2` 报告 §⑤·3 写了**有意不改**的理由：
   `A3 §③·4⑥` 的原稿是 `Check(!probe.UguiMode && pgo.transform.GetComponentsInChildren<ImageQuad>(true).Length > 0, …)`，
   而 `G2` 判「**缺图那一档会把件数断成假红**」⇒ 落成「只断档位开关、不断件数」。
   现核：该 Check 是**联合式** `Check(wHalf964 && cHalf964, …)`，其中 `cHalf964`（登记后真进画布档）**有判别力**，`wHalf964` 那一半确实平凡
   ⇒ 本条**今天不是零判别力**，但**也不等于 `R1` 要的那条结构性检查**。**要不要补**（并配 `ImageQuad > 0` 的兜底判据）⇒ 请主对话裁。
3. ⚠️ **`exDiff == 1` 这个数字我【没有独立复核】** —— 本波按现行口径**不跑 `*.Run`**，而它对 `voice_lines.json` 全表逐卡算，
   手工复算要另写脚本（且要复刻 `TryPick` 的语义 ⇒ 有它自己的错风险）。
   ⇒ 我采信的是**两处独立文档**（本轮台账 `:175` ＋ `A1768` 报告 `:96`，两处都写「现在 1 条、改前 5 条」）。
   **若主对话要一个「跑出来的数」**，那是下一批跑 `BattleScene.Run` 时顺手读 `精确档口径` 那一行（`:12082` 一直在报 `exDiff`）。
4. ⚠️ **②-7 的 ③ 号断言**在语义上是「**实现自洽**」而不是「跟原版对不对」（它问的是「名字在不在 `HudCanvas.SubCanvasRank` 表里」）。
   它的**判别力**来自「表那一侧 + 调用点那一侧是两个地方」——**这正是 `R1` §项 1 末说的「表最容易静默写坏的方式」**。
   ⛔ 我没把它伪装成「原版判据」（④ 号才是原版判据）。**如实记**：③ 号绿 ⇏ 次序对，两条是**配对使用**的。
5. ⚠️ **`R1` §③·1「批处理下 `_cv.rect` 到底有没有值」仍然未定论** —— 本波只做到「两种解释下都不假红」（`bothMeasuredA2` 那道护栏）。
   **但②-7 的 ④ 号断言间接依赖它**：`GetSiblingIndex()` 与 `rect` 无关 ⇒ **不受影响**（这一条不读数、只读兄弟序）。
6. ⚠️ **`②-4` 的判别力仍可能为 0**：若批处理下 `RootRect.rect` 也量不到（0×0）⇒ 走 16:9 支 ⇒ 那条只在断「面板高 = 324.82」。
   ⇒ **本波没解决「怎么让它在批处理下一定有判别力」**（那要么跑一次真机、要么在批处理里显式排一次版 —— 都超出本波范围，⛔ 不猜）。

---

## ⑥ 顺手发现（只报不改）

1. 🔴 **`BattleDriver.cs:14674-14676` 的 doc 已过期**：它还写着「想把它变成真检查，得让 `HudCanvas` 开一个 `public static bool IsRanked(string name)`（📌 已记进接线清单）」
   —— `FD` 本波**已经开了**（`HudCanvas.IsRanked`，账 `G1-5`）⇒ 那两句「还没开」的说法与 `EnsureRankedSubCanvas` 的注释都该就地订正。**白名单外，未改**。
2. 🔴 **`R1` §②-5 的「两条并一条」是个会复发的坑**：`SkillPanel.cs:294/297`（同源）与 `AttackSelector.cs:403/406`（**不同源**）**看着一模一样**，
   而 `R1` 引了攻击选择器的行号去证技能面板的结论 ⇒ 下一个人照 `R1` 去删 `A3①`，会把一条**有判别力**的断言删掉。
   ⇒ 我已在 `:21336-21345` 就地写死「谁同源谁不同源」。⚠️ **`R1` 报告本身没有订正**（那份不在我白名单）。
3. ⚠️ **`R1` §②-8（`Shell/AlliancePanelWindow.cs:707-716` 那段「待接线」）今天仍然过期**，且**本波没人认领** ⇒ 谁碰 `AlliancePanelWindow` 时顺手订正（铁律 5）。
4. ⚠️ **`HudCanvas.SubCanvas` / `HudCanvas.RootRect` / `HudCanvas.IsRanked` 三个只读口现在都在**，但 `BattleScene.cs` 里**新写的这类断言很少**用它们；
   本波 ②-7 是第一个把「根画布的直接子件」真读出来数的 —— 同族还没被覆盖的是**层**（`PopUps` / `Blocking` 那几块不进 `SubCanvasRank`，它们的先后由 `(层, order)` 定，今天只有逐块的层/order 断言、**没有**「同一层内谁先」的机械证明）。
5. ℹ️ **`G2` 报告 §② 里那两行（`:12088-12089` 删掉 `var want`、`:12471-12474` 去掉多余 `var r`）现读已生效**，与 `FA` 的改法不冲突（本波没碰那两处）。
6. ℹ️ `R1` §②-4 原文建议「`HudCanvas.RootRect.rect.height`，或 `Screen.height` + 相机 aspect 自算」—— 本波选**前者**：
   后者要在断言里重算一遍 `CanvasScaler.ScaleWithScreenSize + Expand` 的 `scaleFactor`（= 第二份写法），且 `Screen.height` 在批处理里与相机 `pixelRect` 未必一致（**没查**）。

---

## 可直接粘进总表的一行（≤3 行）

| `FA`（修波 · `Editor/BattleScene.cs` 断言质量） | ✅ `R1` §② 落在本文件的 5 条 + `G1-7` **全部处理**：**②-4** 判别式分支条件换成**判据侧** `HudCanvas.RootRect.rect.height`（原来读被测件自己的 `CanvasPxSize` ⇒ 注释里「写死 1080 会红」**不成立**）＋「两边量不到就不判别」护栏；**②-5** 两处**只留不改**（技能面板那条 `G2` 已标注回归哨兵、同处有 `W11⑥`；攻击选择器那条 **`R1` 引错行号** —— `AttackSelector.cs:403/406` **不同源**，就地补注）；**②-6** 期望值 `621.7/122.2` → 原版 `621.7249755859375/122.24700164794922`、容差 `.5→.1`（今天仍绿，改的是「取自谁」）；**②-7 新增 4 条**（`nRankA2>=5` · `IsRanked` 灭自证·名字 · 真实 `GetSiblingIndex()` ⟹ 原版 `RT_3055` 14 子件序）；`G1-7` 文案印钮号 + 说清「不是实际 ev」；`W35` 邻句 `5 条 → 1 条`（`Check(exDiff>0)` 一字未动）。**`typecheck` 0/0**（跑 2 次）· `git diff --numstat` **578/60** · 行尾 **CRLF 25162/25162**（未翻）· ⛔ **未跑任何 `*.Run`**。 |
| ⏭ **接线（白名单外，本波一行未改）** | 🔴 **②-7 依赖的 `HudCanvas.IsRanked` 已落地**（`FD` 账 `G1-5`）⇒ 不阻塞；**未落地**的：`AttackSelector.SubCanvasName`（账 `G1-1`，落地后 ②-7 第 1 项改引用它）· `MultiCardDisplay.SubCanvasName` 的可见性 · `G1-7` 精确版要的「上一次真播的 ev」只读口 · **`BattleDriver.cs:14674-14676` 那句「IsRanked 还没开」已过期**。 |
| ⚠ **请主对话裁 / 未查清** | ① `R1` §②-3 后半（`wHalf964` 平凡）`G2` **有意不改**（缺图会假红）⇒ 要不要按原稿补 `ImageQuad > 0` 的结构性检查；② `exDiff == 1` 这个数**我只采信两处文档**（本波不跑自检）⇒ 要「跑出来的数」得等下一批读 `:12082` 那行日志；③ ②-4 的判别力在「`RootRect.rect` 也量不到」时**仍为 0**（本波只保证**不假红**）。 |
