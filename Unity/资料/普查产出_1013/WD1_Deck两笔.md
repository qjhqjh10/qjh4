# WD1 · Deck 两笔（A407 字号 base + A502 ESC）

> 2026-10-13 · 写手 WD1 · 只碰了白名单里的两个文件：`Deck/DeckRuntime.cs` · `Editor/DeckScene.cs`
> （路径相对 `d:/4/Unity/MyGame/Assets/CardPresentation/`）· **没跑 Unity** · **没动 git** · 没碰 `DeckEditorState.cs` 与两张正本。
> 判据全部现读复核（简报里的行号已漂的按现读写，见下）。

---

## 一、结论

1. **A407 做完**：两处 `SetAutoFitBox` 补上 `c.LabelAutoMax` / `c.LabelBase`，期望值**全部**取自
   `Core/FilterPanelModel.Cell`（四族 + 卡背页那一族的原版实读常量），写法照**同族样张**
   `Shell/CollectionWindow.TextAligned`（`0 = 不指定 ⇒ 旧行为` 的 `> 0f ? :` 兜底逐字同源，⛔ 没自己填数）。
2. **A502 做完**：`DeckRuntime.EscPressed()` 三级 → **四级**，新那一级的判据 = **那扇窗自己的 `closeOnEsc`**
   （实现 = 把这一下**原样转给** `_popup.ESCPressed()`，两道门槛与「出声」都留在 `GameWindow` 那一处，⛔ 没有第二份规则）。
   `ImportDeckPopup`（`closeOnESC = 1`）那一级**一个字没动**，反例断言仍在。
3. 🔴 **简报没点名、但必须跟着改的一处**：`A330` 那一节 ④ 之后**模态窗还开着**（③ 的 Done 与 ④ 的 ESC 都走
   「不合法 ⇒ 弹窗 + return」），A502 之后 ESC 不再穿窗 ⇒ 那一节的 ⑤/收尾两次 ESC 会**一个字节都写不进去**，
   还会把窗留在场上把后面几节（A364 起手那条前提）连锁弄红 ⇒ **补了一步「点右钮收窗」**（同 A415 收尾的写法）。
4. **两处覆盖迁移，已如实记账**（不假装还钉着）：
   · A364 ⑤「还开着时再弹 ⇒ 复用同一扇」原来是靠 ④ 那次 ESC（当时它不认弹窗）**带电**的 ⇒ 搬到 A502 那一节
     （那里**显式再 `TryClose()` 一次**，断言同一个实例），A364 ⑤ 原地留注释说明它已成平凡真；
   · A364 ⑦「合法保存 ⇒ 窗收掉」（原版 `__TrySaveDeck.c:103 HidePopUp`）**今天没有生产可达路径**
     （原版那一刻同样轮不到 `DeckEditingWindow.ESCPressed`，我们的 Done 钮又被 `ModalPopupOpen` 挡住）
     ⇒ 那一节按新前提改向，并如实写明这一行代码**失去了断言覆盖**（详见 §五·2）。
5. **类型检查 0 错**（跑过 4 次；其中一次撞上别的写手正在写 `Shell/MainMenuRuntime.cs`，重跑即 0，见 §七）。

---

## 二、A407 改动清单

| # | 位置（现读） | 改前 | 改后 | 期望值取自哪 |
|---|---|---|---|---|
| 1 | `Deck/DeckRuntime.cs:3509-3512`（卡牌筛选栏四族；A 表旧号 `:3476`） | `if (c.LabelAutoMin > 0f) lb.SetAutoFitBox(Px(lr.W), Px(lr.H), c.LabelAutoMin, c.LabelPx);` | 同句 + 第 4、5 个实参 = `c.LabelAutoMax > 0f ? c.LabelAutoMax : c.LabelPx` · `c.LabelBase` | `Core/FilterPanelModel.Cell`（字段 `:340` / `:346`；赋值点 `:447` `:482` `:501` `:524`）· 常量原文 `:163`（开关 **32/32**）`:219`（稀有度·类型 **27/36**）`:229`（费用 **45/36**） |
| 2 | `Deck/DeckRuntime.cs:3684-3687`（卡背抽屉 `$owned`；A 表旧号 `:3643`） | 同上形 | 同上形 | `FilterPanelModel.BuildCosmetics` 的 `:648-650`（`ToggleFontAutoMax = 32` / `ToggleFontBase = 32`，判据注释 `:154-163`） |

**样张**（为什么这么写）：`Shell/CollectionWindow.cs:1584-1585` 已经把
`c.LabelAutoMin, c.LabelCenter, c.LabelWrap, c.LabelAutoMax, c.LabelBase` 全传进 `TextAligned`，
兜底实现在 `:1662-1663`（`autoMaxPx > 0f ? autoMaxPx : fontPx`，`basePx` 直接透传）。两处写法**逐字同源**。

**改前的状态（复核过）**：`grep -rn "LabelAutoMax\|LabelBase" Deck/` = **0 命中** ⇒ Deck 这半确实是这一族的漏网。

**这一改在数值上动到了谁**（诚实分档，都是算出来的，不是估的）：
- **稀有度 / 类型**两族：原版 `fs 23.2 · auto[10~27] · base 36`，旧写法上限 = `LabelPx` = **23.2** ⇒ 天花板矮 **3.8px**（真偏离）；
- **开关 / 费用 / 卡背页 `$owned`**三族：原版上限**恰好等于标称**（32/32、45/45）⇒ `fontSizeMax` 读数**不变**；
  base 也恰好等于标称（32/32、36 vs 45…）⇒ 接上它们只是**把字段变成显式的**（不再靠巧合），与上面那条真偏离不同。

---

## 三、A502 改动清单

### 3·1 判据（两条，都是本件**亲读**的反编译/资源，不是转抄）

| 判据 | 出处 | 读出什么 |
|---|---|---|
| ESC **只打给最上面那扇窗、没有第二跳** | `d:/2/tools/decomp_full/WindowsManager__Update.c` | `GetKeyDown(0x1b)` → 取 `+0x58`（= `currentWindow`）→ 过 `IsOpen()`（虚表 `0x1f8`，为假**直接 return**）→ 调那扇窗的 `ESCPressed()`（虚表 `0x1e8`）→ **走完就 return**；**没有**「第一扇不吃就顺着往下传」 |
| 弹窗一开，`currentWindow` 就是它 | `WindowsManager__OpenWindowCO.c`（`set_CurrentWindow` 在 if/else **之外**，我们 `Shell/WindowsManager.cs:830-833` 也早记过同一条） | ⇒ 弹窗在场的每一刻，ESC 都落在**弹窗**上，`DeckEditingWindow.ESCPressed`（= 保存）**根本轮不到** |
| 那扇窗自己 `closeOnESC = 0` ⇒ 什么都不做 | `GameWindow__ESCPressed.c`（第二道门槛 `+0x39`）+ `Shell/PopUpGameWindow.cs:238`（`win.closeOnEsc = false;  // 实证 closeOnESC = 0`） | ⇒ ESC 被吃掉、**什么都不做** |

### 3·2 代码改动

| 位置 | 改前 | 改后 |
|---|---|---|
| `Deck/DeckRuntime.cs:3212-3230` `EscPressed()` | ①输入框 ②导入窗 ③`SaveAndSay()` | ①输入框 ②导入窗 **③模态消息窗开着 ⇒ `_popup.ESCPressed(); return;`** ④`SaveAndSay()` |
| `Deck/DeckRuntime.cs:3163-3211` 方法头注释 | 「三级顺序」+ 三条改坏法 | 「四级顺序」+ ③ 的两条判据 + **两条硬约束**（见 3·3）+ 新改坏法 |

### 3·3 🔴 为什么**不是**一刀切（两条硬约束都落实了）

1. **② 那一级（`ImportDeckPopup`，`closeOnESC = 1`）一个字没动** —— 它在本模型里是我们**内联**的模态层
   （`_importOpen` + `imp_*` 那批件），不是 `GameWindow`，所以不会被我新加的那一级吞掉；
   `Editor/DeckScene.cs:2677-2681` 那条反例断言（ESC ⇒ `!ImportOpen` 且**不落盘**）**仍然绿**，
   我另外在该处补了 4 行注释把「②③ 的差别**就在那扇窗自己的 `closeOnEsc` 上**」写明白（`:2682-2685`）。
2. **新那一级判的不是「有没有弹窗」**，而是 `_popup.closeOnEsc` —— 实现上直接调 `_popup.ESCPressed()`
   （= 原版 `GameWindow.ESCPressed` 那两道门槛那一份实现）。若哪天这扇窗 `closeOnEsc = 1`，
   同一句会自动变成「ESC 关掉它、也不落到保存」，不需要改调用点。
   ⛔ 没有写成 `if (ModalPopupOpen) return;`（那样就把「按窗自己那个字段」这条判据写死了）。
3. **两扇窗不可能同时在场**（开导入要过 `HandlePointer` 的 `ModalPopupOpen` 闸、开消息窗要过 `_importOpen` 闸）
   ⇒ ②③ 的先后**不可观测**，按既有顺序排（注释里写明了）。

### 3·4 `Editor/DeckScene.cs` 里跟着改的地方（含简报没点名的）

| 位置（现读） | 改了什么 · 为什么 |
|---|---|
| `:2756-2765` **A330 补收窗**（🔴 简报没点名） | ④ 之后那扇模态窗还开着；A502 之后 ESC 不再穿窗 ⇒ **补**「点右钮收窗」+ 两条断言。不补的话 ⑤「闸不是恒关」当场红、且窗会留到 A364 起手那条前提（`:2826`）一起红 |
| `:2960-2972` A364 ④ 注释 | 原「如实标注一处偏离」整段改向：说明 A502 已按原版补上那一级 + 保留**诚实标注**「这里两条断言分不出两种状态」 |
| `:2977-2980` A364 ⑤ 注释 | 如实记：那条「复用同一扇」已成平凡真，带电的搬到 A502 一节 |
| `:3010-3042` A364 ⑦ 改向 | 由「合法 ⇒ 落盘 + 收窗」改成「合法 + 窗开着 ⇒ ESC **仍然什么都不做**；收窗后 ESC 才落盘」+ 如实记 `HideDeckPopUp()` 失去覆盖 |
| `:2682-2685` A223 ② 注释 | 反例对照（②=1 ⇒ 关得掉 / ③=0 ⇒ 什么都不做），⛔ 别合并 |
| `:3254-3258` A415 收尾注释 | 旧口径「ESC 会把同一扇窗再配一遍」订正为「**什么都不做**（结论不变：不会落盘）」 |

---

## 四、断言清单（断什么 · 期望值来源 · 改坏法）

| # | 位置 | 断什么 | 期望值来源 | 改坏法（把实现改回错的必须红） |
|---|---|---|---|---|
| 1 | `Editor/DeckScene.cs:3402-3468` **新增 A502 一节** | ① 合法 + 脏 + 「丢改动」窗开着 ⇒ ESC **什么都不做**：窗还开着 · 正文键没变 · 脏标记还在 · 没离场 · **盘上一个字节都没动**（`ExportString` 逐字节 + 盘上名字双保险）；② 窗开着再 `TryClose()` ⇒ **复用同一扇实例**；③ 右钮收窗、名字复位后再 ESC ⇒ **才**落盘、盘上逐字节复原 | 「盘上没动」是**我们的存档真值**（不是我们的常量）；正文键 `MenuDeck/HUD/DiscardChanges` 是原版地址表实读；实例相同 = 原版字段 `popUpWindow` 的语义 | **删掉 `EscPressed()` 里 ③ 那一级** ⇒ ESC 落回 `SaveAndSay()` ⇒ 卡组合法 ⇒ `CommitDeck()` 把「A502·ESC 不该落盘」写进盘 / 窗被 `HideDeckPopUp()` 收掉 ⇒ **至少 4 条红**。把「复用」改成每次新建 ⇒ ②红 |
| 2 | `:3015-3042` A364 ⑦ 两条 | 合法 + 窗开着 ⇒ ESC 仍不落盘（判别式 = 名字 `keepName + "·不该落盘"` 与盘上**不同**）；收窗后 ESC 才落盘 | 同上（盘上真值 + 版本内字面量） | 同 #1（删 ③ ⇒ 保存 ⇒ 盘上出现「…·不该落盘」⇒ 红） |
| 3 | `:3584`（类型族） | `Label.FontSizeToPx(typeLb.FontSizeMax) ≈ 27` | **原版读数** `auto[10~27]`（`FilterPanelModel` 常量注释里的 MB 实读）—— ⛔ 不读我们传进去的常量 | `DeckRuntime.cs:3510-3512` 改回只传 4 参 ⇒ 上限读成 **23.2**（= 标称）⇒ 红 |
| 4 | `:3612`（稀有度族，上限） | 同上（27） | 同上 | 同上 |
| 5 | `:3614`（稀有度族，**base**） | `Label.FontSizeToPx(rarLb.FontSizeBase) ≈ 36` | 原版 `m_fontSizeBase = 36`（MB 实读）· 读口是**反射直读 TMP 真字段**（`Label.FontSizeBase`，同族先例 `Editor/RewardsScene.cs:5118` 就是这么断 36 的） | 删掉 `c.LabelBase` 那个实参 ⇒ `basePx = 0` ⇒ `baseCur = cur` ⇒ base 停在标称 **23.2** ⇒ 红 |
| 6 | `:2677-2681`（A223 ②，**既有**，本件只补注释） | 导入窗开着 ⇒ ESC **关得掉** + 不落盘 | 原版 `ImportDeckPopup` prefab `closeOnESC = 1` | 把两级合并成「有弹窗就不做」⇒ 这条红 |

**自证检查**：#1/#2 比的是**盘上真值**（`DeckLibrary.Load()` 从文件重读）与**窗的实例/正文键**，
不是我们的常量；#3~#5 读的是 **TMP 自己的字段**（`fontSizeMax` / `m_fontSizeBase`），期望值写死原版读数。

---

## 五、没查清 / 没做的

1. **A407 第二处（卡背页 `:3684`）没有带鉴别力的断言** —— 那一族的模型值是
   `LabelAutoMax = 32 = 标称`、`LabelBase = 32`；4 参 vs 5 参在 `fontSizeMax` 上**读数完全相同**，
   在 `m_fontSizeBase` 上也**碰巧相同**（`basePx = 0` ⇒ `baseCur = cur`，而 `FontSizeToPx(cur)` 恰好就是标称 32）。
   ⇒ 我只写了注释（「同一条口径」），**没有**编一条必然绿的空断言（那正是本工程最怕的「绿得没道理」）。
   ⛔ 顺带说明：这一处**今天可能实际什么都没改**，见 §六·1 的浮点早退。
2. **`SaveAndSay()` 成功支的 `HideDeckPopUp()` 失去断言覆盖**（原版 `__TrySaveDeck.c:103`）：
   A502 之后 ESC 到不了它，Done 钮又被 `ModalPopupOpen` 挡着（`HandlePointer:2001`）⇒
   在**我们与原版**都成了防御性代码。想恢复覆盖需要一个自检口直调 Done 那一拍（例如 `UiPressDone()`），
   本件**没加**（超出「只改与本件相关的断言」的范围，留给调度台裁）。原 A364 ⑦ 那条的措辞已按新前提改掉，不假装还钉着。
3. **一处仍在推断**（沿用前一轮的如实标注，本件没有新增证据）：`GameWindow +0x39 == closeOnESC`
   —— 字段名与偏移是**推断**（取值 1/0 是实读的）。
4. **没跑 Unity**（纪律：自检由主对话在同步点统一跑）⇒ 本件的三条新断言**没有实跑过**，只在纸面上核过；
   若同步点跑出红，最可疑的两处是：① `Label.FontSizeBase` 的反射读在批处理下拿到意外值
   （同族先例是绿的，见 `RewardsScene.cs:5118`）② A330 那一节补的收窗步骤与后面的状态假设。
5. **`DeckScene.Run` 之外的宿主没有覆盖**：`DeckRuntime.EscPressed()` 只有 `Editor/DeckScene.cs` 这一个自检宿主
   （已 `grep` 全仓确认）⇒ 本件不需要动其它宿主。

---

## 六、顺手发现（⛔ 本件**没有**顺手改）

1. 🔴 **`Label.SetAutoFitBox` 的 `basePx` 在 `basePx == nomPx` 时写不进 TMP 真字段**（不是本件引入）：
   `baseCur = cur * (basePx / nomPx)`，`basePx == nomPx` ⇒ `baseCur == cur`（浮点上逐位相等时）⇒
   `_tmp.fontSize` 的 setter 走 `m_fontSize == value` 早退 ⇒ `m_fontSizeBase` **不更新**
   （这个洞 `Battle/Label.cs:601-604` 自己写着「残留一个洞（如实说）」）。
   受影响的是**开关族（32/32）**与**卡背页 `$owned`（32/32）** —— 也就是 **A407 第二处今天可能实际什么都没改**。
   ⚠️ 反过来说，稀有度/类型（36 vs 23.2）与费用（36 vs 45）比值 ≠ 1 ⇒ 写得进去 ⇒ 我那两条 base 断言有效。
   出处：`Battle/Label.cs:612-614` + `:594-606`；`nomPx` = `NominalPx()` = `FontSizeToPx(cur)`。
2. **`DeckScene` 里「弹窗开着 + ESC」的组合原本散落在 A330 ④ / A364 ④ / A415 三处**，只有 A415 收尾写了
   「先收窗再按 Done/ESC」，**A330 是漏收**（本件补上）。同类形状将来还会再犯 ⇒
   值得一条通用纪律：「**弹出模态窗的那一节，收尾必须显式收窗**」（A399 那一节就是这么写的）。
3. `Editor/DeckScene.cs:3073` 的 `CheckTrue(wm2 == null || !one.gameObject.activeSelf || wm2.popUpWindow != one, …)`
   是**弱断言**（三个条件命一即可），其中 `wm2 != null` 恒真、`!activeSelf` 与 `popUpWindow != one` 互不独立 ——
   本件**没动**它（不属于这两笔），只登记。

---

## 七、类型检查结果

命令：`TMPDIR=/tmp/wf_wd1 bash d:/4/Unity/工具/typecheck.sh`（每次独立 `TMPDIR`）

| 次序 | 时机 | 运行时 | 编辑器 |
|---|---|---|---|
| 1 | 改完 `DeckRuntime.cs`（A407） | 1（`DeckRuntime.cs(3226,24): CS1061 PopUpGameWindow 未包含 EscPressed`）| 0 |
| 2 | 修掉大小写（`ESCPressed`）后 | **0** | **0** |
| 3 | 改完 `DeckScene.cs`（第一版） | 1 —— **`Shell/MainMenuRuntime.cs(907,13): CS0103 不存在名称 BuildResourcesBar`**（⛔ **不是本件的文件**，是别的写手当时正写到一半；隔 45 秒用 `/tmp/wf_wd1b` 重跑）| 0 |
| 4 | 重跑（同一状态） | **0** | **0** |
| 5 | 补 A330 收窗后（`/tmp/wf_wd1d`） | **0** | **0** |
| 6 | **全部改动落定后**（`/tmp/wf_wd1e`，最终一次） | **0** | **0** |

行尾（改完立刻核）：`git diff --numstat` = **`Deck/DeckRuntime.cs 53/9`** · **`Editor/DeckScene.cs 154/14`**；
两文件 `CRLF == LF` 计数（纯 CRLF，没被翻成 LF）—— 全程用 Edit 工具，⛔ 没用 `sed -i`。
