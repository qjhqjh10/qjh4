# D · `RewardsScene.Run` 四条红分诊（只读诊断）

> 只读诊断代理 · 2026-10-18 · ⛔ 未跑 Unity / player / `*.Run`，⛔ 未改任何代码/资料（只写本报告）。
> 输入 = `d:/4/_tmp_view/rewards_run.log`（2026-10-18 12:48，2218 通过 / 4 失败）+ 工作区 diff + `git log`/`git blame`。
> 结论先说：**四条里 3 条是断言过期（2 条旧账早在 2026-10-17 / 10-18 就写在代码注释里「会红」），
> 只有第 4 条是真实现缺陷；这 4 条没有一条是本次未提交批次（T09/N71/T29/N66/A1232）改出来的**
> —— 第 1 条由本批 T29·D2 **触发**（实现本身对、断言没跟），第 2/3/4 条的根都在已提交的 `09ecc15` / `66ec0ea` 里。

---

## 速览

| # | 红 | 分类 | 一句话根因 |
|---|---|---|---|
| 1 | 左栏底图高 `338.34 ≈ 1009.06±2` | **(α) 断言过期**（本批 T29·D2 触发） | D2 把单块 `Rect` 换成 `GradientV` 的 **3 条横带**，`CheckRectPx` 的 `GetComponentInChildren<ImageQuad>()` 只拿到**第 1 条带**（1009.06 × 21974/65535 = 338.34）。实现 = 原版 prefab 的 `Gradient2`，**不改** |
| 2 | `Timer` 文案 = 活倒计时（`Resets in …`） | **(α) 断言过期** | `ResetIn()` 的前缀 2026-10-18 起是 `Loc.T("Missions/CollectCounter")`（中文档 = **「准备在 {0} 收集」**，与原版实拍逐字一致）；断言还钉着 10-17 那版硬编码 `"Resets in "`。`MissionsTab.cs:1006-1007` **已经写明「那条断言会红、需另行更新」** |
| 3 | 第 3 条文案期望带 `<sprite name="Atlas_trait_icon_ranged">` | **(α) 断言过期** | `DailyData.DailyDesc` 自 `66ec0ea` 起把 `[[ranged]]` 先换成**当前语言的词**（「远程」）再 `RewriteMarks`；原版中文实拍那一行就是纯文字 **「远程攻击 15 次」**（无图标）。断言漏了这一步 |
| 4 | 重摇后描述「对敌方战将造成 45 伤害」⇒ 同一条 | **(β) 实现缺陷** | `RerollDaily` 只拷 `Desc/Target/RewardArt/RewardText`，**没拷 `Term`/`Arg`** ⇒ `DailyDesc` 仍走**旧的** `Missions/Daily_5` + 旧 Arg 45；同一时刻目标已变成 `0/300`（日志实证）⇒ 玩家看到「旧任务名 + 新进度」 |

---

## 红 1 —— 左栏底图高 338.34（期望 1009.06）

**分类**：(α) 断言过期（由本批 **T29 · D2** 触发；实现正确）

**根因（证据）**

- 断言：`Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs:1012`
  `CheckRectPx(bar, 167.17f, 332.17f, 70.94f, 1080f, "左栏底图 …")`。
- 量法：`RewardsScene.cs:68-74` —— `t.GetComponentInChildren<ImageQuad>()` ⇒ **深度优先第 1 颗 quad**。
- 本批改动：`Shell/MainMenuSubmenuWindow.cs:498` 把
  `Rect(bar, ArtBarBg, …)` 换成 `MenuDraw.GradientV(bar, Art(ArtBarBg), barRect, QPanel, "Background", TabGradT, TabGradC)`。
- `MenuDraw.GradientV`（`Shell/MenuDraw.cs:1609-1626`）= 按 4 个色标把矩形切成 **N−1 = 3 条横带**
  （`Band 0/1/2`，各自一个 `ImageQuad`、UV 按 `[t_i, t_{i+1}]` 切）。
- 于是 `GetComponentInChildren<ImageQuad>()` 拿到 **Band 0**：高 = `1009.06 × 21974/65535 = 338.34px`
  —— **与日志里的 338.34 逐位吻合**（另外两条带 = 400.63 + 270.09，三条和 = 1009.06）。
- 宽度那条照样绿（三条带同宽 165）——这也反证「不是没画，是量到了其中一条」。

**判据**

- 原版 prefab：`Tab Buttons` 的 `Image` + `Gradient2`（`_gradientType=1` 竖 · `_blendMode=0` Override ·
  4 色键 `0→黑 / 0.3353→白 / 0.7324→白 / 1→0.283 灰` · alpha 恒 1；5 扇窗逐位相同）。
- 本仓对账：`Unity/资料/普查产出_1018/R_T29_全界面底纹背景对账.md` **§2 第 77 行 / §D2（第 210-225 行）**。
- 同族样板（已按 3 条带断过）：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs:13110-13126`
  （`ParentEventWindow` 的 `Tab Buttons/Image` ⇒ `GetComponentsInChildren<ImageQuad>()` 长度 **3** + 底带/顶带色标）。

**建议（只改断言）**

把这条改成「**并集**」量法（或至少：带数 = 3、各带宽 = 165、**三条带高之和** = 1009.06、最上/最下沿对齐 70.94/1080）。
最省事的写法是在 `RewardsScene` 里加一个小助手：

```csharp
// 语义 = 原版「一块 Image + Gradient2」⇒ 我们画成 3 条带，量【并集】而不是第一颗 quad
var bands = bar.GetComponentsInChildren<ImageQuad>();   // 3
float h = 0f; foreach (var b in bands) h += b.WorldH * 108f;
```

⚠️ 别改成 `CheckRectPx(FindChild(bar, "Background"), …)` —— `Background` 根节点上**没有** `ImageQuad`，
`GetComponentInChildren` 仍会落到 Band 0，照样红。
✅ 也可以顺手抄 `MainMenuScene.cs:13115-13126` 那两条色标断言（底带 黑→白、顶带 白→0.283 灰），把 D2 的值也钉住。

---

## 红 2 —— 登录卡 `Timer` 实得「准备在 11小时 11分钟 收集」

**分类**：(α) 断言过期（**旧账**：2026-10-17 `09ecc15` 就写明了，2026-10-18 `66ec0ea` 换成中文词条后第一次跑到）

**根因（证据）**

- 断言：`Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs:2423-2427`
  `timerText.StartsWith("Resets in ")`。
- 实现链：
  - `Shell/MissionsTab.cs:1001` `Txt(…, DailyData.ResetIn(), …)`；
    `:1008` `TimerDisplay.Attach(tl, () => DailyData.NextResetAt, Loc.T("Missions/CollectCounter"), …)`。
  - `Shell/DailyData.cs:723-724` `ResetIn()` = `TimerDisplay.TextFor(NextResetAt, Loc.T("Missions/CollectCounter"), …)`。
  - `Shell/TimerDisplay.cs:256-270` `TextFor` → `Compose(prefix, FormatTimeString(remain))`。
  - `Core/Loc.cs:418` `Missions/CollectCounter` = **「准备在 {0} 收集」/ "Ready to collect in {0}"**；
    默认语档 = **中文**（`Loc.cs:151` `Default = Chinese`，本机无 `PlayerPrefs["Language"]`）。
  - 所以实得「准备在 11小时 11分钟 收集」= **完全正确**。
- **代码自己记了这笔账**：`Shell/MissionsTab.cs:1006-1007`（`git blame` = `09ecc15` 2026-10-17）：
  「`Editor/RewardsScene.cs:2209` 那条断言还把文案钉在出厂串 `Resets in 12h 34 m` 上 —— 本件只改实现、
  **没动 Editor** ⇒ 那条断言**会红**，需主对话另行更新」。
  2026-10-18 `66ec0ea` 把 `ResetIn()` 的前缀从硬编码 `"Resets in "` 换成了 `Loc.T("Missions/CollectCounter")`
  （`git show 66ec0ea -- Shell/DailyData.cs`），断言仍未同步。

**判据**

- **原版实拍（第一判据）**：`Unity/资料/原版参照图/用户实拍_1017/奖励—布道所（每日任务）参考图.png`
  —— 骷髅卡底部「**准备在 4分钟 5秒 收集**」、周常「**准备在 12小时 29分钟 收集**」；
  格式与我们的「准备在 {0} 收集」逐字一致。
- 词条：`Core/Loc.cs:418`（官方 I2 键 `Missions/CollectCounter`）。
- 铁律 5：旧记录「钉死串」已过期，就地改断言。

**建议（只改断言）**

断言别再写死英文前缀，改成**语言无关**的写法（前缀从词条里拆，剩余部分非空）：

```csharp
string fmt = Loc.T("Missions/CollectCounter");          // 「准备在 {0} 收集」
string pre = fmt.Substring(0, System.Math.Max(0, fmt.IndexOf("{0}")));
CheckTrue(timerText != null && timerText.StartsWith(pre)
          && timerText.Length > pre.Length && timerText.EndsWith(fmt.Substring(fmt.IndexOf("{0}") + 3)));
```

（⚠️ 不要拿 `DailyData.ResetIn()` 当期望值 —— 那就是同源自证，只证明它等于它自己。）

---

## 红 3 —— 第 3 条任务文案：期望带 `<sprite ...>[远程]`，实得「远程攻击 15 次」

**分类**：(α) 断言过期（`66ec0ea` 改了实现、同一次提交里同步了另外两条文案断言，**漏了这一条的 `[[ranged]]` 替换**）

**根因（证据）**

- 断言：`Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs:9575-9576`
  `Check(DailyData.DailyDesc(2), CardIcons.RewriteMarks(Loc.T("Missions/Daily_10").Replace("{0}", "15"), CardText.Zh), …)`。
- 实现：`Shell/DailyData.cs:162-183`，关键在 `:177-178`：
  `s.Replace("[[ranged]]", CardText.Keyword("ranged")).Replace("[[melee]]", CardText.Keyword("melee"))`
  ⇒ 「远程」+「攻击 15 次」= **「远程攻击 15 次」**，之后 `RewriteMarks` 无事可做。
- 词条：`Core/Loc.cs:422` = `[[ranged]]攻击 {0} 次`；`Card_Trait/ranged` = 「远程」(`Core/Loc.cs:429`，经 `CardText.Keyword`)。
- `git show 66ec0ea -- Editor/RewardsScene.cs`：同一次提交里第 1/2 条改成了 `Loc.T(…).Replace("{0}", …)`，
  第 3 条却写成 `CardIcons.RewriteMarks(Loc.T(…), …)` —— **少了 `.Replace("[[ranged]]", CardText.Keyword("ranged"))`**。

**判据**

- **原版实拍**：`Unity/资料/原版参照图/用户实拍_1017/奖励—布道所（每日任务）参考图.png`
  第 3 行就是纯文字「**远程攻击 15 次**」（**没有**内联图标 sprite）。
- 官方词条 `Missions/Daily_10`（`[[ranged]]` 是记号，原版在**卡面**上渲染成图标、在**任务行**里是词）。

**建议（只改断言）**

最小改法（补齐实现同一步）：

```csharp
Check(DailyData.DailyDesc(2),
      Loc.T("Missions/Daily_10").Replace("{0}", "15").Replace("[[ranged]]", CardText.Keyword("ranged")),
      "文案：第 3 条 = 官方 `Missions/Daily_10`（远程攻击 15 次）");
```

✅ 更强（防同源自证，推荐）：按 `RewardsScene.cs:8774` 那条既有先例做**中英分档字面量**：

```csharp
string want3 = Loc.Current == AvailableLanguages.Chinese ? "远程攻击 15 次" : "Attack Ranged 15 times";
```

（英文侧取原版实拍/官方英文列 `Attack [[ranged]] {0} times` 把记号换词后的结果 —— 与中文同一套语义。）

---

## 红 4 —— 重摇后描述没换（「对敌方战将造成 45 伤害」⇒ 同一条）

**分类**：(β) 实现缺陷（`66ec0ea` 引入 `Term` 时漏改 `RerollDaily`；**不是断言的问题**）

**根因（证据）**

- 现象：日志 `d:/4/_tmp_view/rewards_run.log:41300` 前后 —— 描述没变，但**紧接着**那条
  「…目标/进度也不同：0/45 ⇒ **0/300**」是 **✓ 绿** ⇒ 重摇**真的发生了**（换了任务、换了目标），
  只是**显示文案还取旧那条**。
- `DailyDesc`（`Shell/DailyData.cs:162-183`）的优先级：**有 `Term` ⇒ 一律走 `Loc.T(Term)`**，
  只有 `Term` 为空才回落到 `Desc`（`:168-182`）。
- `RerollDaily`（`Shell/DailyData.cs:612-686`）在 `:671-676` 只拷
  `Desc / Progress / Target / RewardArt / RewardText / St` —— **`Term` / `Arg` 一个都没动**
  ⇒ 重摇后 `t.Term` 仍是 `"Missions/Daily_5"`、`t.Arg` 仍是 `45`
  ⇒ `DailyDesc(0)` = 「对敌方战将造成 **45** 伤害」（旧任务 + 旧参数），而 `DailyCounter(0)` 已是 `0/300`。
- 页面画的就是这个函数：`Shell/MissionsTab.cs:661` `DailyData.DailyDesc(index)`。

**同族的另外两处缺口（同一根因，修的时候必须一起补，否则下一次会换一种红）**

1. `Shell/DailyData.cs:86-93` `SnapshotOf` 不拷 `Term`/`Arg`；`:2008-2020` `ResetMissionsForTest` 只还原
   `Desc/Target/RewardArt/RewardText` ⇒ 一旦 `RerollDaily` 开始清 `Term`，**自检的后续小节**（§八 的
   `文案：第 N 条` 那三条）就会拿到 `Term == null` 而回落英文 `Desc` ⇒ **必红**。
2. 落盘/读盘：`RowDto`（`Shell/DailyData.cs:1673-1684`）**没有 term 字段**；`:1787-1794` 读盘时
   `rerolled` 行只覆盖 `Desc/Target/RewardArt/RewardText` ⇒ 重启后 `Term` 又是出厂键 ⇒
   **旧任务名 + 重摇后的进度**（和自检里一模一样的症状，只是发生在玩家存档上）。

**判据**

- 原版：`Confirm` → 服务端 `RerollChallenge` 换**一整条新任务**（描述/目标/奖励全换）；
  我们自己的契约也写在 `DailyData.cs:608-611`「换完逐项不同」+ `RerollDaily` 的 `Say` 里。
- 重摇池（`:493-508`）每条**只有 `Desc`、没有 `Term`** ⇒ 正确的语义是「新任务没有官方键 ⇒ 回落到 `Desc`」。

**建议（改实现，只动 `Shell/DailyData.cs`；断言不动）**

1. `RerollDaily` 的字段拷贝块（`:671-676`）补两句：
   `t.Term = src.Term; t.Arg = src.Arg;`（池里是 null/0 ⇒ `DailyDesc` 自然回落到 `Desc`）。
2. `SnapshotOf`（`:90-91`）与 `ResetMissionsForTest`（`:2012-2015`）补 `Term`/`Arg` 的拷/还原。
3. 读盘那支（`:1787-1794`）在 `r.rerolled` 时把 `_daily[i].Term = null; _daily[i].Arg = 0;`
   （盘上没有 term 字段；若想保留中文文案，就得给 `RowDto` 加 `term`/`arg` 并把 `version` +1 —— 但那属于「池子本地化」的另一个决定）。
4. 顺手（非本条红）：`_rerollPool` 里有两条与官方键语义**完全对得上**
   （`Deploy {0} Troops` = `Missions/Daily_Generic_6`、`Play {0} Stratagems` = `Missions/Daily_Generic_7`，
   见 `Unity/MyGame/Assets/CardPresentation/Resources/official_loc_13lang.json`）——
   要让重摇后也显示中文，可以给这两条补 `Term`/`Arg`；其余三条官方没有对应键（`Deal 300 damage with ranged units` /
   `Win 1 battle with a Warlord alive` / `Destroy 8 enemy units`）⇒ 保持 `Desc`（英文）并在报告里记账。

⚠️ **不要**改成「改断言放过它」：屏上确实是「旧任务名 + 0/300」，这是玩家可见的真 bug。
⚠️ 另注：`Editor/RewardsScene.cs:10657` 的「页面上那一行立刻显示新任务」是 `TextOf(description)` vs
`DailyData.DailyDesc(0)` —— **两边同源**，本条红在它眼皮底下照样绿（自证型断言，铁律 §三「灭自证」那一族）。
修完实现后它仍值得加强（例如钉住「新描述 ≠ 旧中文串」）。

---

## 建议的修复顺序

1. **红 4（实现）** —— 唯一真缺陷、玩家可见；改 `Shell/DailyData.cs` 一处 + 补 `SnapshotOf`/`Reset`/读盘三处。
   ⚠️ 改完必须跑 `RewardsScene.Run`（跨 `DailyData` 的持久化与自检夹具）。
2. **红 1（断言）** —— 本批 T29·D2 的直接后果，一行级；顺手把 D2 的 3 带/色标钉住。
3. **红 3（断言）** —— 一行级；建议顺手改成中英分档字面量（去自证）。
4. **红 2（断言）** —— 一行级；前缀从 `Loc.T("Missions/CollectCounter")` 拆。
   （2/3/4 都是 `Editor/RewardsScene.cs` 单文件，可与上一个「批次」一起跑一次 `RewardsScene.Run`。）

## 必须改实现 / 只改断言

| 条目 | 改哪里 | 性质 |
|---|---|---|
| 红 1 | `Editor/RewardsScene.cs:1012`（+ 可加小助手） | **只改断言**（实现 = 原版 prefab，⛔ 别回退成单块 `Rect`） |
| 红 2 | `Editor/RewardsScene.cs:2423-2427` | **只改断言**（实现 = 原版实拍「准备在 … 收集」） |
| 红 3 | `Editor/RewardsScene.cs:9575-9576` | **只改断言**（实现 = 原版实拍「远程攻击 15 次」） |
| 红 4 | `Shell/DailyData.cs`（`RerollDaily` + `SnapshotOf` + `ResetMissionsForTest` + 读盘） | **必须改实现**；断言不动 |

## 顺手发现（不属于这四条红，未跑验证，仅登记）

1. `Shell/MissionsTab.cs:1006-1007`（2026-10-17）**早就写明** RewardScene 那条 Timer 断言会红、需另行更新 ——
   说明 `RewardsScene.Run` 至少从那天起没跑过（或红了没人收）。建议把这种「实现改了、Editor 不在白名单」
   的已知红**同步落到本线待办**（否则一定会以「新红」的面目再出现一次）。
2. `Shell/DailyRewardPopup.cs:571` 注释仍写「前缀与 `RewardTimerText()` 同一格（`Resets in `）」——
   前缀已本地化，注释过期（铁律 5，就地改注释即可，无行为影响）。
3. `Shell/DailyData.cs:1673-1684` 的 `RowDto` 没有 term 字段；`version` 目前 = 1。
   若决定给重摇池补官方 `Term`，这里要跟着升版本（`version +1`，读到旧版本按出厂值走）。
4. 本次四条红**没有一条来自 A1232 的字号档改动** —— 那批改过的窗口断言（`progress` / 三处 `count` /
   `Collect` / 卡头 `name` / 骷髅 `Timer`，以及 A1212 那几条）在日志里**逐条 ✓**，A1232 可以视为通过。
