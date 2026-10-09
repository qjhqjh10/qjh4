# P-E · 五个包装层补 autosize 形参（`A1205`）+ 接线（`A1212` 本块）

> 执行代理 **P-E** · 2026-10-19 · 白名单 = 只有那 5 个 `.cs`（**别的文件一个都没碰**）。
> 红线：⛔ 没跑 Unity / `_run_8_checks.sh` · ⛔ 没动 git · ⛔ 没碰两张正本 · `d:/2/**` 只读。
> ✅ 跑了**秒级类型检查**（`TMPDIR=/tmp/wf_pe bash d:/4/Unity/工具/typecheck.sh`）—— 读数列在 §五。
> 📌 判据来源一律是 `资料/普查产出_第十会话/R5_包装层菜单族.md`（`R5`）+ 简报点名的两处先例
> （`Shell/SettingsWindow.cs` 的 `A1181` / `Shell/MenuWindowBase.cs` 的 `A1173`，**两份都现读过**）；
> **R5/R6 的行号是快照**，本报告里的行号一律是 **2026-10-19 改完之后的现读**。

---

## §一 一句话结论

**5 个签名都补好了（形状 = `A1181` 那一档：两个开关拆开），`R5` 落在本块上的 26 行「✅该接」接了 25 行
（= 27 处，`#59` 一行含 3 个宿主调用点）**；**唯一没接的是 `#61`（`MissionsTab:924 counter text`）——
它正是 `A1208` 挂起的那笔，按简报「只报不动」**；另外 `MainMenuRuntime.Text` 那一族在 `R5` 账上
**0 处该接**（两行都是「原版关着」）⇒ **签名补了、没有调用点可接**。

📌 **与 `A1205` 那个「28 处」对账**：本块 `R5` 账上 `✅该接` 共 **26 行** ·
其中 `#59`（`count`）**一行含 3 个宿主调用点** ⇒ **26 − 1 + 3 = 28 处** ✅ —— 两个数对得上；
**本笔落地 27 处**（`#61` 那一处按 `A1208` 挂起，见 §四）。

---

## §二 第一部分：五个签名（改前 → 改后）

**五个都取同一档 = `Shell/SettingsWindow.cs` 的 `Text`（`A1181`）：`wrapPx` = 折行开关 ·
`autoMinPx` = 自适应开关（`fitW = wrapPx > 0 ? wrapPx : (autoMinPx > 0 ? 本格框宽 : 0)`，
`autoMinPx > 0 && wrapPx <= 0` 时还原 `SetWrapping(false)`）—— ⛔ 不是 `MenuWindowBase.Text`（`A1173`）那一档。**

### 判据：为什么是 `A1181` 那一档（逐个文件从「原版真有的组合」倒推）

| 方法 | 本块「✅该接」处的折行分布 | 需不需要「折行 0 + 自适应」这个组合 | 取哪档 |
|---|---|---|---|
| `DeckInfoPopup.Txt` | 7 处 = **5 处折行 0** + 2 处折行 1 | **要** | `A1181` |
| `ImportDeckPopup.Txt` | 4 处 = **3 处折行 0** + 1 处折行 1 | **要** | `A1181` |
| `PracticeModePopup.Txt` | 8 处 = **1 处折行 0** + 7 处折行 1 | **要** | `A1181` |
| `MissionsTab.Txt1` | 3 处 = **2 处折行 0** + 1 处折行 1 | **要** | `A1181` |
| `MainMenuRuntime.Text` | **0 处**（`R5` §2·D 两行都是「原版 `m_enableAutoSizing = 0`」） | **判据倒推不出形状** | `A1181`（**见下面那条说明**） |

🔴 **`A1173` 那一档为什么不行**：它把折行与自适应绑在**同一个 `wrapPx`** 上
（`if (wrapPx > 0f) { SetWrapWidth(…); if (autoMinPx > 0f && fontPx > autoMinPx) SetAutoFitBox(…) }`）
⇒ **表达不了「折行 = 0 且开着自适应」**，而上表前四行每一个文件里**都有**这一档的站
（`A1195` 的裁定原话：**「原版真的有『折行 = 0 且开着 autosize』那一档」**）。
🔑 **`MainMenuRuntime.Text` 是唯一一处判据不足的**：它今天没有一处要接 ⇒ 拿「原版组合」倒推不出形状。
**我按「选宽的一档」取 `A1181`**，理由 = 它**严格更宽**（能表达那个组合，而 `A1173` 那档不能），
**而本方法今天无人传这四个形参 ⇒ 选宽的不会造成任何偏离**；反过来选窄的，将来真要接「折行 0 + 自适应」
就得再改一次签名。**这条是我的判断（不是判据）**，异议请调度台裁 —— 已写进 `MainMenuRuntime.Text` 的注释。

### 2·1 逐个签名

**① `Shell/MainMenuRuntime.cs:135` `Text`** —— 先例：`A1181`（`SettingsWindow.Text`）
```csharp
// 改前
Label Text(Transform parent, string text, float x1, float x2, float y1, float y2, int scale,
           Color color, string name, float fontPx = 0f, int queue = QText)
// 改后（新增四个，全缺省 0）
Label Text(Transform parent, string text, float x1, float x2, float y1, float y2, int scale,
           Color color, string name, float fontPx = 0f, int queue = QText,
           float wrapPx = 0f, float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f)
```
体：`SetGlyphHeight` 之后插两段（`fitW` 计算 → `SetWrapWidth` + `SetAutoFitBox` → 还原折行），无后续重排（本方法没有裁切）。

**② `Shell/DeckInfoPopup.cs:1610` `Txt`** —— 先例：`A1181`
```csharp
// 改前
Label Txt(Transform parent, Transform basis, string text, float x1, float y1, float x2, float y2,
          float fontPx, Align align, string name, int q)
// 改后
Label Txt(Transform parent, Transform basis, string text, float x1, float y1, float x2, float y2,
          float fontPx, Align align, string name, int q,
          float wrapPx = 0f, float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f)
```
体：插在 `SetGlyphHeight` **之后**、两句 `Align*On` **之前**（`SetWrapWidth`/`SetAutoFitBox` 会改 `sizeDelta.x`，
而对齐读的就是它 —— 次序判据 = `Battle/Label.cs` 的 `SetWrappingMode` 头「`SetAutoFitBox` → 换行模式 → 对齐 → 量」）。

**③ `Shell/ImportDeckPopup.cs:484` `Txt`** —— 先例：`A1181`
```csharp
// 改前
Label Txt(Transform parent, string text, float x1, float y1, float x2, float y2, float fontPx,
          Align align, string name, int q)
// 改后
Label Txt(Transform parent, string text, float x1, float y1, float x2, float y2, float fontPx,
          Align align, string name, int q,
          float wrapPx = 0f, float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f)
```
体：同 ②（`SetGlyphHeight` 之后、对齐之前）。

**④ `Shell/PracticeModePopup.cs:1939` `Txt`** —— 先例：`A1181`
```csharp
// 改前
Label Txt(Transform parent, string text, float x1, float x2, float y1, float y2, float fontPx,
          Align align, string name, int q,
          PxRect? clip = null, Vector2 clipSoftness = default(Vector2))
// 改后（⚠️ 新形参排在 clip/clipSoftness 之后 ⇒ `:1180` 那处按位置传的 `null, DeckClipSoft` 语义不变）
Label Txt(Transform parent, string text, float x1, float x2, float y1, float y2, float fontPx,
          Align align, string name, int q,
          PxRect? clip = null, Vector2 clipSoftness = default(Vector2),
          float wrapPx = 0f, float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f)
```
体：autosize 两步 + 还原折行排在末句 `MenuDraw.ClipText` **之前**（裁切必须最后一步 —— 本方法头那一段）。

**⑤-a `Shell/MissionsTab.cs:348` `Txt`** —— 先例：`ItemDrawer.ClippedText` 的 `wrapOff`（同族唯一先例）
```csharp
// 改前
Label Txt(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
          float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f)
// 改后（新增 1 个 bool —— 见下面「为什么这里不能是 wrapPx」）
Label Txt(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
          float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f, bool wrapOff = false)
```
🔴 **为什么这个口用 `wrapOff`（布尔）而不是 `wrapPx`**：本方法走 `_win.TextBox`，**出厂就是「限宽换行」**
（`MenuDraw.TextBox` 无条件 `SetWrapWidth(r.W)`）⇒ 这里的开关只能朝**「关」**那一侧开，
而且**缺省必须是「照旧折行」**（既有调用点一律折行）；写成 `wrapPx = 0 表示不折行` 的话，
**缺省值一落下去就是「全部调用点都不折行」= 静默改整页**。⛔ 这个不对称是**被两条既有事实夹出来的**，不是随手。

**⑤-b `Shell/MissionsTab.cs:425` `Txt1`** —— 先例：`A1181`
```csharp
// 改前
Label Txt1(Transform parent, PxRect r, string text, Color color, string name, float fontPx)
// 改后
Label Txt1(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
           float wrapPx = 0f, float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f)
```
⚠️ **`Txt1` 是「转调」口，不是裸 `Label` 口**（`_win.Text` = `MainMenuSubmenuWindow.Text` = **`A1173` 那一档**，
带 `wrapPx > 0 ∧ autoMinPx > 0 ∧ fontPx > autoMinPx` 三道闸）。补法 = **在这一侧把两个开关拆开**：
自适应那一档把 `wrapPx` 补成**本格框宽**喂进去（否则就是**死实参**），出来再 `SetWrapping(false)` 还原。
⛔ **没有改 `MenuWindowBase.Text` 的契约**（`A1195` 不许把两边改成一个样）—— 详见 `Txt1` 的 doc。

### 「全缺省 ⇒ 旧调用点逐位不变」的机械保证（两档各一条）

- **`Txt` / `Text` / `Txt1` 那四个 float 档**：`wrapPx <= 0 && autoMinPx <= 0` ⇒ `fitW = 0` ⇒ **一个分支都不进**
  （`SetWrapWidth` / `SetAutoFitBox` / `SetWrapping` 三句全不执行）⇒ **与改前逐位相同**。
- **`MissionsTab.Txt` 的 `wrapOff`**：缺省 `false` ⇒ 那一句 `SetWrapping(false)` 不执行 ⇒ 同上。

---

## §三 第二部分：逐处表（**27 处** = `R5` 的 25 行，其中 `#59` 一行含 3 个宿主调用点）

**读法**：`四格` = `min / max / base / 折行`（一律**原版那四个字段的原文**）；
`折行还原?` = 本笔有没有显式把它设回原版那一档（`SetWrapWidth`/`SetAutoFitBox` 会**无条件**开成 `Normal`）。
⚠️ **本块的 28 处里没有一处在 `3` 档**（那几处在 `DeckRuntime` / `CollectionWindow`，不属本块）⇒ 不需要 `Label.SetWrappingMode(3)`。
📌 **行号 = 我落地写的那几行实参所在行**（调用语句本身可能从**上一行**起 —— 例：`Txt(...` 在 `:1121`、`"Cost Text", …` 在 `:1122`）。

### A. `Shell/DeckInfoPopup.cs`（`Txt`，走 `Label.SetAutoFitBox`，**无闸**）—— 7 处

| # | 现读行号 | 节点名 | 四格（min/max/base/折行） | 折行还原? | 判据出处 |
|---|---|---|---|---|---|
| 35 | `:849` | `Deck Name` | 10 / 45 / 42 / 0 | ✅（`SetWrapping(false)`） | `R5` §2·F #35 |
| 36 | `:855` | `Warlord Name` | 10 / 40 / 38 / 0 | ✅ | `R5` §2·F #36 |
| 37 | `:901` | `Text {Practice,Edit,Select} Deck`（三颗同一处） | 10 / 40 / 12 / 0 | ✅ | `R5` §2·F #37 |
| 38 | `:1123` | `Cost Text` | 18 / 50 / 32 / **1** | 不动（原版就是 1） | `R5` §2·F #38 · `wrapPx: 40f` = 本格框宽 |
| 39 | `:1125` | `Name`（卡名） | 2 / 38 / 27.69 / 0 | ✅ | `R5` §2·F #39 |
| 40 | `:1127` | `Count`（`x3`） | 2 / 32 / 36 / 0 | ✅ | `R5` §2·F #40 |
| 41 | `:1153` | `Deck Information Cost/balance text` | 10 / 44 / 22.3 / **1** | 不动 | `R5` §2·F #41 · `wrapPx: DiHeadR - DiHeadL` |

### B. `Shell/ImportDeckPopup.cs`（`Txt`，走 `Label.SetAutoFitBox`，**无闸**）—— 4 处

| # | 现读行号 | 节点名 | 四格 | 折行还原? | 判据出处 |
|---|---|---|---|---|---|
| 42 | `:203` | `Main Search message` | 4 / 50 / 36 / **1** | 不动 | `R5` §2·G #42 · `wrapPx: MsgR - MsgL` |
| 43 | `:217` | `Error msg`（`Build` 入口） | 4 / 28 / 36 / 0 | ✅ | `R5` §2·G #43 |
| 45 | `:376` | `Error msg`（`RebuildErrorLine` 入口） | 4 / 28 / 36 / 0 | ✅ | `R5` §2·G #45 · ⚠️ 两个出生入口都传（`CLAUDE.md` §10 第 5 条） |
| 44 | `:234` | `Confirm Text` | 12 / 45 / 12 / 0 | ✅ | `R5` §2·G #44 |

### C. `Shell/PracticeModePopup.cs`（`Txt`，走 `Label.SetAutoFitBox`，**无闸**）—— 8 处

| # | 现读行号 | 节点名 | 四格 | 折行还原? | 判据出处 |
|---|---|---|---|---|---|
| 46 | `:515` | `Selected Army Title` | 1 / 36 / 23 / **1** | 不动 | `R5` §2·H #46 · `wrapPx: ArmyTxR - ArmyTxL` |
| 47 | `:567` | `Deck Name` | 1 / 36 / 22.68 / **1** | 不动 | `R5` §2·H #47 · `wrapPx: DnR - DnL` |
| 48 | `:575` | `Warlord Name` | 1 / 35 / 24.3 / **1** | 不动 | `R5` §2·H #48 · `wrapPx: WnR - WnL` |
| 49 | `:610` | `Card / Energy cost` | 1 / 34 / 22.3 / **1** | 不动 | `R5` §2·H #49 · `wrapPx: CostTxtR - CostTxtL` |
| 50 | `:638` | `Change Deck Text` | 10 / 45 / 12 / **0** | ✅ | `R5` §2·H #50（本文件 8 处里唯一折行 0） |
| 51 | `:796` | `Back Text` | 1 / 45 / 31.9 / **1** | 不动 | `R5` §2·H #51 · `wrapPx: BackTxR - BackTxL` |
| 52 | `:814` | `tooltip` | 1 / 38 / 23.2 / **1** | 不动 | `R5` §2·H #52 · `wrapPx: TipR - TipL` |
| 54 | `:833` | `Battle Text` | 18 / 45 / 36 / **1** | 不动 | `R5` §2·H #54 · `wrapPx: BtTxR - BtTxL` |

### D. `Shell/MissionsTab.cs` —— 8 处（`Txt` 4 处 · `Txt1` 3 处 · 助手 1 处）

⚠️ **这一块走的是【带闸】的两条路**（`Txt` → `MenuDraw.TextBox` 的 `fontPx > autoMinPx`；
`Txt1` → `MenuWindowBase.Text` 的**三条**）⇒ 下面每一处我都现算过闸：**全部过得去**
（`FS()` 对 min 与 fontPx **各乘一次**、比值不变）。

| # | 现读行号 | 口 | 节点名 / 宿主 | 四格 | 折行还原? | 判据出处 |
|---|---|---|---|---|---|---|
| 60 | `:607` | `Txt1` | `progress`（每日行） | 10 / 40 / 36 / 0 | ✅ | `R5` §2·J #60 · 闸：`40.25 > 11.5` ✅ |
| 59 | `:574` | `Txt`/助手 | 奖励格 `count` —— **每日行档** | 20 / 40 / 36 / 0 | ✅（助手内恒 `wrapOff: true`） | `R5` §3 注④ 第 1 档 |
| 59 | `:791` | `Txt`/助手 | 奖励格 `count` —— **登录卡档** | 10 / 40 / 36 / 0 | ✅ | `R5` §3 注④ 第 2 档 |
| 59 | `:889` | `Txt`/助手 | 奖励格 `count` —— **骷髅卡档** | 10 / 40 / 36 / 0 | ✅ | `R5` §3 注④ 第 2 档 |
| 56 | `:826` | `Txt` | `Timer`（登录卡；**页内实例**） | 15 / 38 / 36 / **1** | 不动 | `R5` §2·J #56（来源分叉见 §六·3） |
| 57 | `:981` | `Txt` | `name`（`Weekly Challenge`） | 10 / 36 / 46 / **0** | ✅ | `R5` §2·J #57 |
| 62 | `:1022` | `Txt1` | `counter`（周常 `13/15`） | 15 / 40 / 36 / **1** | **不动（本笔从 `0` 改回 `1`）** | `R5` §2·J #62 · `wrapPx: cw`（62.53）· 闸：`33.15 > 15` ✅ |
| 58 | `:1085` | `Txt` | `Timer`（周常 `Ends in …`） | 15 / 38 / 36 / **1** | 不动 | `R5` §2·J #58 |

**助手签名（本块第 8 处改动，非 `R5` 行）**：`BuildRewardCell(...)`（`MissionsTab.cs:1520` 附近）
新增 `float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f` ——
**原因 = 铁律 5·c**：`count` 这一颗在**同一个原版 prefab**里**三档值**，由**调用点的宿主**决定，
⛔ 不能填一个常量（`R5` §3 注④ 那张表）。三个调用点已全部显式传值。

### ⚠️ 「折行宽」这一格：不是原版字段值（逐处登记）

本块**折行 = 1** 的那 9 处（`#38` · `#41` · `#42` · `#46–#49` · `#51` · `#52` · `#54` · `#62`）都要给 `wrapPx`，
而**原版那颗的 `m_SizeDelta.x` 是布局组排出来的 / prefab 里是模板位**（`R5` 没给这一格，`A1181` 自己那条
注释写着同一件事：「自适应框的宽取的是本格框宽 …… 这一格**不是原版字段值**」）⇒
**我一律取「本格框宽」**（我们的 `r.W` / `x2 - x1` / 常量差），并**逐处写进了代码注释**。
🔴 **这是本笔最可能出错的一格**（见 §六·2 的保留）。

---

## §四 没接上的逐条（写清为什么）

| 项 | 位置 | 为什么不接 |
|---|---|---|
| `R5` §2·J **#61** `counter text` | `Shell/MissionsTab.cs:924`（`Txt1(…, "counter text", 26.8f)`） | 🔴 **`A1208` 挂起的那笔，本简报明令「只报不动」**。它是 `✅该接` 里**唯一接不上的**：原版 `min = 30`、我们标称 `26.8`（都过 `FS() = ×1.15`）⇒ 被 `MenuWindowBase.Text` 那条 `fontPx > autoMinPx` 闸挡住（`30.82 > 34.5` 假）。**裁定在 `A1208`**：「先查实是不是标称抄错」⇒ ⛔ 我没改闸、也没改那一站的 min/标称。**一句提醒**：`Txt1` 补了签名之后，**这一站随时可以接**（把四个实参填上就行），闸该不该动仍然听 `A1208`。 |
| `R5` §2·D **#31 / #32** | `Shell/MainMenuRuntime.cs`（`ChatPreview` 的 `Message Preview` / `Message Preview (1)`） | ⛔ **不是缺口** —— 原版那两颗 `m_enableAutoSizing = 0`（`MB 1795/1814` 实读，`R5` §2·D）⇒ **加了就是主动制造偏离**。**没碰**。 |
| `R5` §2·H **#53** | `Shell/PracticeModePopup.cs`（`Toggle Label` · `Game mode`） | ⛔ **不是缺口** —— 原版 `EverguildTextMeshPro` 那颗**关着** autosize（`R5` §2·H #53）。**没碰**。 |
| `R5` 账上**不属于本块**的 28 处 | 别的文件（`DeckRuntime` 15 · `CollectionWindow` 7 · `ShopWindow` 5 · `SkillPanel` 4 · `SettingsPanel` 3 · `DailyStreakPopup` 2 · `ItemDrawer` 2 · `RewardWindow` 1 · `CampaignTab` 1 `＋` `MainMenuSubmenuWindow` 那一族） | **不归本块**（别的代理正在那些文件上干活）—— 一行都没碰。 |

---

## §五 验证

**① 秒级类型检查**（改完最后一个 `.cs` 之后跑的，命令见文首）：
```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
✅ **两个程序集都是 0 错**（这一次**没有**出现「错在别人正在写的文件上」那一档 —— 别的代理的文件
当时是干净的/或没被编到我这一趟里；无论如何**这一趟读数是 0/0**）。

**② `git diff --numstat`（5 个文件，改完即测）**：
```
42	2	Unity/MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs      （1498 行）
65	8	Unity/MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs        （1626 行）
54	5	Unity/MyGame/Assets/CardPresentation/Shell/ImportDeckPopup.cs      （493 行）
70	10	Unity/MyGame/Assets/CardPresentation/Shell/PracticeModePopup.cs    （2009 行）
117	16	Unity/MyGame/Assets/CardPresentation/Shell/MissionsTab.cs          （1535 行）
```
⇒ **增删数都远小于文件行数 ⇒ 行尾没被翻**（全部 LF，一律用 Edit 工具改、没用过 `sed -i`/文本模式写）。
**③ ⛔ 没跑任何 Unity 自检**（简报明令）；本块**要覆盖的自检**（留给调度台在同步点跑）：
`ShellScene.Run`（`DeckInfoPopup` / `ImportDeckPopup` / `PracticeModePopup` 的自检宿主）·
`RewardsScene.Run`（`MissionsTab`）· `MainMenuScene.Run`（`MainMenuRuntime`）—— **动的是共用件邻居而不是引擎**
⇒ **不需要** `RuleEngineTest.Run`。

---

## §六 没查清 / 留了保留的部分（⛔ 没猜，逐条写清「还差什么」）

1. 🔴 **`MainMenuRuntime.Text` 的契约选择是判断、不是判据** —— 它 `R5` 账上 **0 处该接**，
   拿「原版组合」倒推不出形状。我按「选严格更宽的那一档（`A1181`）」取，理由写在 §二 与代码注释里。
   **若调度台认为该照 `A1173` 取（与它形似的那一族），改起来是 5 行**（把 `fitW`/还原那两段换成 `if (wrapPx > 0f)` 那一档），
   但那样就**表达不了**「折行 0 + 自适应」。
2. 🔴 **9 处「折行宽」取的是本格框宽（不是原版字段值）** —— 原版那颗的 `m_SizeDelta.x` 我**没读到**
   （`R5` 没给这一列；prefab 里那一批是布局组排出来的模板位）。**这一格最可能与我抄的不一样**。
   ⚠️ 最具体的风险 = `DeckInfoPopup:1118` `Cost Text`：折行宽 40、标称 26 ⇒ 两位数的费用（如 `10`）
   可能被 TMP 折断成两行（原版那颗的宽若明显大于 40 就不会）。**要不要复核这一格，请调度台定**。
3. ⚠️ **`MissionsTab` 的「来源分叉」仍未裁**（`R5` §5·3 + `MissionsTab` 自己 `:1107-1110` 那条写着「判据要调度台裁定」）：
   登录卡的 `Timer`（`#56`）与奖励格 `count`（`#59`）都是「页内实例 vs 独立预制体 `Daily Login Bonus Container`」两套值。
   **本笔一律按 `R5` 的取法 = 页内实例**（玩家看到的那份），并把来源写进了注释。裁定若翻，改的是**值**、不是形状。
4. ⚠️ **`MissionsTab.Txt1` / `Txt` 的「先还原折行、再裁」次序** —— 还原那一句排在那两条漏斗的 `ClipText` **之后**。
   **今天无影响**（`RewardsWindow` 整棵树**没有 `ViewportClip`**、本窗也没设 `Clip` ⇒ `RenderClip == null`
   ⇒ 那两句 `ClipText` 本来就不执行 —— 我现读过 `MenuWindowBase.Text:343` / `TextBox:369` 与
   `RewardsWindow.cs` 全文件 0 处 `Clip`）。**将来给本页挂上视口裁切时，这两处要改成
   「先还原折行、再 `ClipText`」**（`Shell/SettingsWindow.cs:4748` 就是那个次序）—— 已写进两处注释。
5. **没做（也不该由我做）**：`A1208` 那两条被闸挡住的（`MissionsTab:924` / `ShopWindow:723`）—— 裁定在调度台。
6. **`MissionsTab:1022` 那处（`#62`）我把上一轮的「故意不折行」改回了原版那一档 —— 见 §七·1。**

---

## §七 顺手发现（⛔ 只报不改）

1. 🔴 **`MissionsTab` 周常 `counter`（`#62`）我把折行档从 `0` 改回了 `1` —— 这是一次「照原版值」的改动，请复核。**
   上一轮（2026-09-23）**有意**写成「不换行」，理由是**我们自己的渲染图**上 `TextBox` 把 `13/15` 拆成了
   `13/` + `15`；**而 `R5` §2·J #62 现读原版那颗 `m_TextWrappingMode = 1`**，`R5` §5·5 还专门警告
   「写手要连折行档一起核 …… 不显式设回去就静默改了折行」。⇒ 本笔**照原版把折行开回来了**，
   并同时给它接上 `auto[15~40] base36`（上一轮那句注释写的场景是**还没有自适应**的时候）。
   ⚠️ **若真机上它仍被折成两行**，那是「照原版字段值」的结果 —— **要不要为观感退回 NoWrap，请用户/调度台拍板**
   （退回的办法 = 把 `wrapPx: cw` 去掉，`Txt1` 就会自己 `SetWrapping(false)`）。
2. **`MainMenuRuntime` 有 2 处「调用点先 `Text`、再自己补 `SetAutoFitBox`」的写法**（`NavButton` 内 ·
   `BuildModeCard` 内，两处都在 `R5` §6 里「坐实」过、**不在 `A1212` 的 166 里**）：
   ```csharp
   var navText = Text(b, label, …);
   if (navText != null) navText.SetAutoFitBox(146.92f / 108f, 39.39f / 108f, 18f, 33f, 24f);   // auto[18~33] base24
   ```
   现在这四个形参**已经能直传**了（`Text(…, wrapPx: 146.92f, autoMinPx: 18f, autoMaxPx: 33f, autoBasePx: 24f)`）
   —— **本批没动它们**（不在账上、也不是缺陷），**登记给调度台**：要不要收口成一处，是另一笔。
   ⚠️ 与本文件那两个 ⛔ 行（`ChatPreview` 两条）**不是一回事**，别合并。
3. **`DeckInfoPopup` 的三处标称字号与 `R5` 读数不符**（`R5` §5·2 已登记、未裁，我**照旧没动标称**）：
   `:899` 传 `34` 而原版 `m_fontSize = 40` · `:1118/:1120/:1122` 三处传 `26` 而原版是 `50 / 38 / 32`。
   ⚠️ **本笔只接了 min/max/base/折行**，标称那一格原样。🔴 **但它与我这笔有一个交叉点**：
   `#38` `Cost Text` 的标称 26 比原版 50 **小一半**，而我把折行打开、折行宽给到 40 ——
   **两位数的费用**贴着这个组合最容易被折断（见 §六·2）。
4. **`PracticeModePopup` 的 `Txt` 有两个「接P」站点**（`:1180` 的 `Deck Name` · `:1402` 的 `Name`，
   都是「调用点自己补 `SetAutoFitBox`」）—— 它们**不在账上**，本批没动；现在四个形参也能直传了，登记同上。
5. **`MissionsTab.Txt` 的 `autoMaxPx = 0` 但 `autoBasePx > 0` 这一档**（`:1111` 卡头 `name` 那一类）
   会调 `FitWindow(…, autoMinPx, fontPx, autoBasePx)` —— **上界退回标称**是 `A333` 的既有语义（逐位不变），
   我把新加的几处都**同时给了 max**，没有踩这一档。**仅登记**（免得下一轮有人以为我漏了 max）。
6. **`import` 那扇窗的 `Error msg` 有两个出生入口**（`Build` / `RebuildErrorLine`）——
   本笔**两处都传**（`CLAUDE.md` §10 第 5 条）；⚠️ 顺带提醒：**同族还有别的「多入口」件**
   （`BuildRewardCell` 的 `count` 也算一种宿主分叉）⇒ 改动这类件时**按「入口」逐个点**，别只改 grep 的第一条。
7. **骷髅卡的 `Timer` 走的是另一个助手 `BuildClockRow`（`MissionsTab.cs:950` / 定义 `:1619`），它【已经有】
   autosize 形参、而且已经传了 `15f, 38f, 36f`** —— 与 `#56`（登录卡的 `Timer`）是**同一颗原版件的两个宿主**，
   值也对得上（15/38/36）⇒ **本块在这一点上没有缺口**。**本笔没动它**（它不在 `R5` 的 ✅ 账上）。
   ⚠️ 同一段里 `BuildButton(..., 12f, 44f, 12f)`（`Collect` 那颗）也是**早就接过**的 —— 登记一句，
   免得下一轮又被当成「包装层没地方传」的缺口重查一遍。

---

## §八 我没做的事（如实登记）

- ⛔ 没跑 Unity / `_run_8_checks.sh` / 任何断言（**只跑了秒级类型检查**，读数列在 §五）；
- ⛔ 没动 git（没 `add` / `commit` / `checkout` / `stash` / `reset`）；
- ⛔ 没碰 `项目任务.md` / `CLAUDE.md`（两张正本），也没碰白名单外的**任何** `.cs`
  （`SettingsWindow.cs` / `MenuWindowBase.cs` / 别人正在写的文件 —— **只读**）；
- ⛔ 没把任何「查不到」写成猜测：§三「折行宽」那一格、§六 的五条都写清了「还差什么」；
- ✅ 所有改动走 **Edit 工具**（没用 `sed -i`、没用 python 文本模式写）⇒ 行尾 LF 未翻（§五 的 numstat 为证）。
