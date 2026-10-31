# B106_D3 — 第一百零六会话同步点 · `DeckScene` / `MainMenuScene` 两条失败诊断（只读）

- 代理：`B106_D3`（只读诊断，**未改任何 `.cs` / 账 / 正本**；未跑 Unity）
- 输入：`d:/4/_tmp_view/deck.log`（10-31 12:04）· `d:/4/_tmp_view/menu.log`（10-31 12:05）
- 计数口径：按**行首**看失败标记 —— `deck.log` 里 `✗` 共 6 处，其中 5 处是**断言文案 / 探针子 sink 的回显**（`:46944`/`:46959`/`:83952`/`:83965`/`:84008`），**真失败 1 条 = `:88066`**，与尾部合计 `2155/2156` 相符；`menu.log` 里 `✗` 1 处（`:122574`）+ 尾部「失败重列」1 处（`:181725`，同一条），与 `6395 通过 / 1 失败` 相符。

---

## 一、`DeckScene.Run` — `[B106W7] ✗ E0（前提）30 张同阵营 ⇒ 合法`

### 1.1 失败原文（`deck.log:88066`）

```
[B106W7]    ✗ E0（前提）30 张同阵营 ⇒ 合法（否则下面两条可能只是撞在张数上） —— 期望 [None]，实得 [CopyLimitExceeded]
   at B106W7_Assert:E_ValidateStillReportsWrongFaction () (Assets/CardPresentation/Editor/B106_W7_Assert.cs:259)
   at B106W7_Assert:Run ()        (Assets/CardPresentation/Editor/B106_W7_Assert.cs:115)
   at DeckScene:RunCore ()        (Assets/CardPresentation/Editor/DeckScene.cs:1397)
```

尾部合计：`deck.log:88323` → `DK === 结束：2155/2156 通过，**1 条失败** ❌ ===`。

后两条（E1/E2）**都过了**：`deck.log:88082` E1 ✓（异阵营 ⇒ `WrongFaction`）、`deck.log:88098` E2 ✓（无督军 ⇒ `NoWarlord`）—— 也就是说失败只发生在 E0 这条**前提**上，被测的两支语义本身没坏。

### 1.2 定位（现读行号）

| 东西 | 位置 |
|---|---|
| 断言本体（E 段） | `Unity/MyGame/Assets/CardPresentation/Editor/B106_W7_Assert.cs:253` `E_ValidateStillReportsWrongFaction()` |
| **失败那一行** | 同文件 `:258` `for (int i = 0; i < 30; i++) ok.Add(AlphaU1);` → `:259-260` `C(DeckRules.Validate(...), DeckError.None, "E0（前提）…")` |
| 夹具卡 | 同文件 `:92-95` `Unit(id, faction)` ⇒ `new CardDef(id, id, "unit", "", "common", faction, 1, 1, 1, 0)`（稀有度 **common**、卡型 **unit**） |
| 被调的实现 | `Unity/MyGame/Assets/RuleEngine/Core/DeckRules.cs:449` `Validate` |
| ④ 张数 | `DeckRules.cs:483-485`（经典档 `want = 30`） |
| ⑤ 阵营（本批 W7 改的） | `DeckRules.cs:485` 同行追加 `deckArmy` + `:489` |
| **⑦ 同名上限（真正拦下的那一关）** | `DeckRules.cs:516` `if (kv.Value > CopyLimit(c.Rarity, c.Type)) return DeckError.CopyLimitExceeded;` |
| 同名上限值 | `DeckRules.cs:162-168` `CopyLimit` ⇒ 非督军按稀有度取 `GameplayVariables.Classic`；`Core/GameplayVariables.cs:105` `ClassicCopiesOther = 2` |

### 1.3 实际值 vs 期望值

- 期望：`DeckError.None`
- 实得：`DeckError.CopyLimitExceeded`
- 原因：夹具把**同一张** `B106W7A1`（common / unit）塞了 **30 张**。`Validate` 走到 ⑦（`:516`）时同名计数 `kv.Value = 30 > CopyLimit("common","unit") = 2` ⇒ 直接返回 `CopyLimitExceeded`。
- 先后次序也解释了为什么它没被 ④ ⑤ ⑥ 提前拦下：30 张正好等于经典档 `deckSize`（④ 过）、30 张全是 `B106W7Alpha` 与督军同阵营（⑤ 过）、全是 unit（⑥/⑥b 过）⇒ **一定撞到 ⑦**。也就是说这条前提**在任何输入下都不可能成立**。

### 1.4 定性：**α**（断言/Fixture 写错）

- 不是 β：产品实现按原版规则办事（同名上限 2 是原版值，⑦ 早就存在，本批 `DeckRules` 的 diff 只动了 ⑤ 的 `deckArmy` 读口 —— 见 `git diff -- RuleEngine/Core/DeckRules.cs`，只有 `:431`/`:440-441`/`:485`/`:487`/`:489` 五行）。
- 不是 γ：`B106_W7_Assert.cs` 是**本批新建、从未跑过**的文件（`git status` = `??`，untracked）—— 这是它**第一次执行**，不存在「上一轮绿、这一轮红」。
- 也可按项目分类记作 **δ（夹具前提不成立）**：E0 自身就是一条前提断言，而它自己搭的夹具不满足前提。两种叫法指向同一个修法（改夹具），**不涉及任何生产代码**。

### 1.5 修法（只给方案，未动手）

把 E 段的 30 张换成「**≥15 张不同的同阵营 unit，各 2 张**」（同名上限 2、总数 30、经典档）。

现成范式 = `Unity/MyGame/Assets/RuleEngine/Editor/DeckRulesTest.cs:80` `BuildLegalDeck` —— 它就是用 `CopyLimit(c.Rarity)` 决定每张放几份、从真池子里凑 30 张。E 段是合成池，可以照同一思路本地补：

```csharp
static void E_ValidateStillReportsWrongFaction()
{
    // 30 张合法 = 15 张不同的同阵营 unit × 2（⑦ 同名上限 = ClassicCopiesOther = 2，⛔ 不能再靠「同 id 塞 30 张」）
    var pool = new List<CardDef> { Hero(HeroA, FactionA), Unit(BetaU1, FactionB) };
    var ok = new List<string>();
    for (int i = 0; i < 15; i++)
    {
        var id = "B106W7EU" + i;                 // 夹具自造 id（🔶 自己设计：合成池，同本文件既有夹具口径）
        pool.Add(Unit(id, FactionA));
        ok.Add(id); ok.Add(id);                  // 每张 2 份 ⇒ 30 张，恰好等于上限（不是超）
    }
    var s = new DeckEditorState(pool);

    C(DeckRules.Validate(new PlayerDeck("E0", HeroA, null, ok, 0), s.Find), DeckError.None,
      "E0（前提）30 张同阵营（15 张 × 2）⇒ 合法（否则下面两条可能只是撞在张数上）");

    var mixed = new List<string>(ok);
    mixed[29] = BetaU1;                          // 仍是第 30 位换成异阵营 ⇒ ⑤ 先于 ⑦ ⇒ WrongFaction
    ...  // E1 / E2 原样，把 s 换成新 s 即可（E2 无督军 ⇒ ② 先于 ⑦，不受影响）
}
```

要点：

1. **不能改回「张数更少」**（如 `skirmish: true` 12 张）—— 同名上限与张数无关，12 张同 id 依旧是 `CopyLimitExceeded`；最小合法集合仍是「≥6 张不同 id」。
2. E1 仍可用 `mixed[29] = BetaU1`（⑤ 在 ⑦ 之前，`DeckRules.cs:489` vs `:516` ⇒ 先报 `WrongFaction`）。
3. E2 无督军 ⇒ `DeckRules.cs:467` 那句 `if (warlord == null) return DeckError.NoWarlord;` 先返回，与 ⑦ 无关（日志里 E2 现在就是 ✓）。
4. 合成 unit 的 `inventoryOptions` 取默认 `-1` ⇒ ⑥b 走 `WarnUnknown(c)` 返回 false（放行），**每条会打一条警告**；想安静一点可在 `Unit(...)` 夹具里显式传 `inventoryOptions: 100`（不改变 `CannotAddToDeck` 的结论）。

### 1.6 还缺什么

- 无缺判据。产品行为（同名上限 = 2、⑦ 的次序、E1/E2 两支语义）都已由 E1/E2 的 ✓ 和 `RuleEngineTest` 既有断言覆盖。
- ⚠️ 提醒：修的是**新建断言文件**，不影响 `DeckScene` 里别的账；但 `B106_W7_Assert.cs` 结尾有 `+1` 计数，修完需要重跑 `DeckScene.Run`（合计应回到 2156/2156）。

---

## 二、`MainMenuScene.Run` — `A833 ✗ 整块在视口下的 Guild Name 连节点都不建`

### 2.1 失败原文（`menu.log:122574`，尾部重列 `:181725`）

```
[Menu]    ✗ ★ A833：**整块**在视口下的 `Guild Name` **连节点都不建**（`MenuDraw.Text` 开头那句 `if (!Visible(r, _st.RenderClip)) return null;`）—— 与上面「部分越界的那两颗照样建、只是被夹到框沿」正好是 A822 那一节的两态｜改坏法：把 `MenuDraw.Text` 开头那道闸删掉 ⇒ 本条红 —— 期望 [True]，实得 [False]
   at MainMenuScene:RunCore () (Assets/CardPresentation/Editor/MainMenuScene.cs:11338)
```

合计（`menu.log:181713`）：`[Menu] === 合计：6395 通过 / 1 失败 ===`。

紧邻的前提全是 ✓（`menu.log`）：

- `:122559`（前提）`Guild Name` 的行内框 `50..96` 在压边行里**整块落在视口沿之下**（它的顶 **942.83** > 视口底 **937.83**）
- `:122105`（前提）压边行与参照行都找到了；`:122135` 参照行整颗在视口内
- `:122246` A833 行底九宫格被截到视口下沿 937.83 ✓（= 裁切链本身是好的）
- 下一条 `:122589`（参照行里 `Guild Name` **建出来了**）✓

⇒ 唯一失败是「压边行里 `Guild Name` 应当**不建**，实际建了」。

### 2.2 定位（现读行号）

| 东西 | 位置 |
|---|---|
| 失败断言 | `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs:11338` `CheckTrue(a833Edge != null && FindChild(a833Edge, "Guild Name") == null, …)` |
| 断言依赖的参照（✓） | 同文件 `:11345`（参照行里那颗要**建出来**） |
| 前提算式 | 同文件 `:11334` `a833GuildTop = a833EdgeCy - OrigLbRowH*0.5f + 50f`（= `GuildR` 的行内顶 50） |
| 「整块在外 ⇒ 不建」那道闸 | `Unity/MyGame/Assets/CardPresentation/Shell/MenuDraw.cs:2029` `if (!Visible(r, _st.RenderClip)) return null;`（比较的是 `MenuDraw.cs:356` `Visible`） |
| **回归源头：`Guild Name` 建节点时用的矩形** | `Unity/MyGame/Assets/CardPresentation/Shell/LeaderboardRow.cs:250` `guildBox = Abs(r, new PxRect(GuildR.x1, NameHolderR.y1, GuildR.x2, NameHolderR.y2));` |
| 同族（`Name`） | `LeaderboardRow.cs:242` `nameBox = Abs(r, new PxRect(NameR.x1, NameHolderR.y1, NameR.x2, NameHolderR.y2));` |
| 矩形常量 | `LeaderboardRow.cs:99-101` `NameHolderR = (290,0,1023.95,100)` · `NameR = (290,6,1000.79,52)` · `GuildR = (290,50,1000.79,96)` |
| 行内→绝对换算 | `LeaderboardRow.cs:131` `Abs(row, local)` |
| 调用处 `MenuDraw.Text(holder, guildBox, …)` | `LeaderboardRow.cs:251` |

### 2.3 实际值 vs 期望值（算得出唯一解释）

- 压边行实测：行中心 `a833EdgeCy = 942.83`、行高 `OrigLbRowH = 100` ⇒ **行顶 = 892.83**；视口底 `VpBot = 937.83`。
- **旧版（HEAD）**：`guildAbs = Abs(r, GuildR)` ⇒ y 跨度 `942.83 .. 988.83` ⇒ `Visible` 首句：`r.y1 (942.83) >= c.y2 (937.83)` ⇒ 不可见 ⇒ `MenuDraw.Text` 返回 null ⇒ 节点**不建** ⇒ 断言绿。
- **现版（W13）**：`guildBox = Abs(r, [GuildR.x1, NameHolderR.y1, GuildR.x2, NameHolderR.y2])` ⇒ y 跨度 `892.83 .. 992.83` ⇒ 跨过视口底 ⇒ `Visible` = true ⇒ 节点**建了** ⇒ 断言红。
- 这正好对应日志里「前提 ✓（设计框 50..96 确在视口下）」+「参照行 ✓（建出来）」+「压边行 ✗（也建出来）」的三种读数组合 —— **唯一的差别就是建节点时喂给闸的矩形被换成了 `NameHolderR` 的整高 100**。

### 2.4 定性：**γ**（本批回归），笔数 = **`B106_W13`**

- 该改动**未提交、在今天运行前落地**：`git status` = ` M Shell/LeaderboardRow.cs`，文件 mtime `10-31 11:06`（另：`git diff` 里那段新注释写着 `🆕 2026-10-31（B106_W13 · 铁律 18·0）`）。
- `git diff -- Shell/LeaderboardRow.cs` 的**唯一行为性差异**就是这处：旧 `var nameAbs = Abs(r, NameR); var guildAbs = Abs(r, GuildR);` → 新 `nameBox/guildBox` 用 `NameHolderR.y1..y2`（0..100）。其余是注释 + 类尾新增 `VlgStack/PrefH/PlaceAt`。
- **反证已排除**：
  - 同一条断言在 `_tmp_view/b102_menu6.log`（10-30 19:55）是 ✓，而 `LeaderboardRow.cs` 的 HEAD 版（提交 `b08bab8f` 10-30 20:08）就是旧写法 ⇒ 时间线对得上。
  - 今天另有两个文件被改：`Shell/LeaderboardWindow.cs`（11:32）、`Shell/MenuScroll.cs`（11:33），但 `git diff --stat` 各只 **1 行**、且都是 🔶 自设计标签注释 ⇒ **不可能是它们**。
  - `Editor/MainMenuScene.cs` 今天的 +234 行是 `W29` 的 AUD06 段（`LeaderboardData.ApplyScoringMode` 那几条），`git diff` 里**没有**任何 `A833 / Guild Name / LeaderboardRow` 的改动。
  - `MenuDraw.cs` 未在本次改动之列（mtime 10-30 22:51），闸本身没变。
- 这台机器上「画面」不差（`ClipText` 照样把越界字裁成零面积），差的是**节点存在性** —— 而这正是 A798/A833 立的那条契约（原版 `RectMask2D` 对图与字一视同仁、整块在外连节点都不建）。⇒ 属于「与原版不符」，按铁律 11 记一条要做。

### 2.5 修法（只给方案，未动手）

**原则**：W13 把框高抬到 100 是为了让 `Text` 内部的 autofit（`MenuDraw.cs:2071-2072` 的 `SetAutoFitBox(…, LayoutSpace.Px(r.H), …)`）按 holder 全高算、不把 40px 的单行挤小 —— 这一点**要保留**；错的是**同一份矩形被同时拿去喂「整块在外就不建」那道闸**（`MenuDraw.cs:2029`）。所以：**几何框照旧 100 高，粗筛另用设计帧里的真实矩形**。

最小改法（只动 `LeaderboardRow.cs`，不动共用件）—— 用 `Shell/MenuDraw.cs:375` 现成的 `VisibleAbove(parent, r, clip)`（它就是 `ViewportClip.Resolve(parent, …).RenderClip` + `Visible`，与 `MenuDraw.Text` 内部走的是**同一条父链、同一个 RenderClip**；`ProfilePage.cs:162`、`LiveOpsEventWindow.cs:1110`、`PracticeModePopup.cs:1810`、`AllianceMemberTab.cs:632` 都是这个范式）：

```csharp
// LeaderboardRow.cs:242 附近
var nameBox  = Abs(r, new PxRect(NameR.x1,  NameHolderR.y1, NameR.x2,  NameHolderR.y2));   // 高 100：autofit 用
var nm = MenuDraw.VisibleAbove(holder, Abs(r, NameR), null)                                // 粗筛：设计帧真实框 6..52
         ? MenuDraw.Text(holder, nameBox, …) : null;

// :250 附近
if (!string.IsNullOrEmpty(d.Guild))
{
    guildBox = Abs(r, new PxRect(GuildR.x1, NameHolderR.y1, GuildR.x2, NameHolderR.y2));      // 高 100：autofit 用
    if (MenuDraw.VisibleAbove(holder, Abs(r, GuildR), null))                                  // 粗筛：设计帧真实框 50..96
        gn = MenuDraw.Text(holder, guildBox, …);
}
```

（`VlgStack` 只在 `guild != null` 时才用 `guildX` —— 见 `LeaderboardRow.cs:390`；`gn == null` 时 `PrefH` 返回 0，`Name` 单独居中，正是原版「`Guild` 缺席」那一支，所以不建不会有副作用。）

**为什么不去改断言**：`MainMenuScene.cs:11338` 这条是 10-18 立的、连绿 12 天，且它的期望与**原版语义**一致（`RectMask2D` 按每个 graphic 自己的矩形裁；原版 `Guild Name` 的 RT 是 `Name Holder` 的**子件**，不是整个 holder）。按铁律 18·0，这里应当**复刻原版**、改生产代码。

**也别选的两条路**：

- 把 `guildBox` 直接改回 `Abs(r, GuildR)` —— 会连带把 W13 修好的字号（autofit 按 46 高算）退回去，**正中 W13 修的反面**。
- 给 `MenuDraw.Text` 加 `cullRect` 形参 —— 语义更「正」，但动的是共用件（`Text` 146 个调用点），且 A833 的期望基数就是 `GuildR` ⇒ 收益不大、风险更大；留作日后统一收口时的候选。

**改完要顺带确认的三条（都在同一段 `RunCore` 里，预计仍绿）**：

1. `MainMenuScene.cs:11345` 参照行 `Guild Name` 仍建出来（参照行整颗在视口内 ⇒ `Abs(r, GuildR)` 可见 ⇒ 建）。
2. `(E)` **浅下沿档**（越界量 55→30）里 `Guild Name` 是「部分越界 ⇒ 建、mesh 被夹」：此时行顶 = `VpBot − 70` ⇒ `GuildR` 顶 = `VpBot − 20` < `VpBot` ⇒ 仍可见 ⇒ 建 ⇒ 不变。
3. `(D)` **上沿档**用 `Name`：行顶 = `VpTop − 28` ⇒ `NameR`（6..52）跨度 `VpTop−22 .. VpTop+24` ⇒ 跨过视口顶 ⇒ 仍可见 ⇒ 建 ⇒ 不变。

**回归面**：`LeaderboardRow.Build` 的其它调用方（`LeaderboardWindow` 弹出棵 / 嵌入棵；无 `ViewportClip` 的上下文）里 `VisibleAbove` 与 `Visible(r, null)` 逐位相同（`MenuDraw.cs:377` 首句 `if (!clip.HasValue) return true;`）⇒ **无裁切时行为一字不变**。

### 2.6 还缺什么

1. **残余保真差（W13 带进来、目前无断言覆盖）**：`PlaceAt` 只改 `localPosition`（`MenuDraw.Local` 只返回位置，见 `MenuDraw.cs:30-31`），**不改标签 RectTransform 的高度** ⇒ 两行 `Label` 的 rect 仍是 100 高，而原版 VLG 会把 `Guild Name`/`Name` 的 RT 高写成各自 TMP 首选高。要不要补「RT 高 = 首选高」这条、由调度台定（补的话建议顺带加一条断言：`Label` 的 rect 高 ≈ `PrefH`）。这条**不影响**本次红。
2. **粗筛近似**：`GuildR`（行内 50..96）比原版 VLG 摆完的真实位置（≈ 行内 `50+hN/2` 起）略高 ⇒ 在「行顶落在 `VpBot−73` .. `VpBot−50`」这 ~23px 带里，原版会裁掉而我们仍建节点（画面上仍被 `ClipText` 裁成零面积，**看不出**）。要更准就得在建节点前做一遍 VLG 量高（需先有标签，循环依赖），不建议为它加复杂度；**若日后要断，断言得用 VLG 摆位后的矩形而不是 `GuildR`**。
3. 本条与「画面」无关（截图看不出来），验收只能靠断言（A833 这条就是）。修完请重跑 `MainMenuScene.Run`，期望回到 `6396 通过 / 0 失败`。

---

## 三、一句话结论

| 宿主 | 失败 | 定性 | 归因 | 修哪 |
|---|---|---|---|---|
| `DeckScene.Run` | `[B106W7] E0（前提）30 张同阵营 ⇒ 合法`：实得 `CopyLimitExceeded` | **α**（也可记 δ） | 新断言夹具自己犯规：30 张同 id，同名上限 = 2（`DeckRules.cs:516` + `GameplayVariables.cs:105`） | `Editor/B106_W7_Assert.cs:253-260`：30 张改成 **15 张不同 id × 2**（范式 `RuleEngine/Editor/DeckRulesTest.cs:80`） |
| `MainMenuScene.Run` | `★ A833：整块在视口下的 Guild Name 连节点都不建`：期望 True / 实得 False | **γ** | **`B106_W13`**（`Shell/LeaderboardRow.cs`，未提交）把 `Name`/`Guild Name` 建节点用的矩形从「设计帧真实框（6..52 / 50..96）」换成「holder 全高 100」⇒ 粗筛不再判「整块在外」 | `Shell/LeaderboardRow.cs:242`、`:250`：**框高保持 100 给 autofit**，另用 `MenuDraw.VisibleAbove(holder, Abs(r, NameR / GuildR), null)` 做粗筛（`MenuDraw.cs:375`，同族范式见 `ProfilePage.cs:162` 等） |
