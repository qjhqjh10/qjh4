# P10 · `A1186` 裁定 ①② 落地：页签栏回 5 格 + `Online` 改从 `General` 页文字钮进

> 执行代理 P10 · 2026-10-19。判据 = **用户裁定（裁定①栏回原版 5 格 / 裁定②`Online` 用 `General` 页一颗文字钮进、不画图标）**
> + 原版 prefab 实读（`menu_rect` dump / 本文件 `Gen*` 常量那一段）。
> **一条 Unity 自检都没跑**（红线：手头待办没做完不跑自检；且本件不该由我跑）；只跑了**秒级类型检查**（读数见 §6）。

---

## 1 页签栏：现在 **5 格**、逐位是原版那 5 个

现在是 **5 格**：`General / Audio(=原版 Media) / Account / Graphics / Support`。
`BuildTabs` 的 `specs` 落到 **5 条**、`TabTop(i, specs.Length)` 传 5（余量按实际键数分摊）。

**现读复算**（按文件里那 4 个常量：`BarT=123.10` · `BarB=966.19` · `TabBtnH=157.68350219726562`
· `TabGap=8.920000076293945` · `BarPadTop=13` · `TabAlignY=0.5`）：

| 格 | `TabTop(i, 5)` |
|---|---|
| 1 | **139.09624** |
| 2 | **305.69975** |
| 3 | **472.30325** |
| 4 | **638.90675** |
| 5 | **805.51025** |

⇒ **逐值对上原版那 5 个数**（`surplus = 843.09 − (824.09751 + 13) = +5.99249`，一半 2.99624）。
⚠️ 原版 prefab 印的 `139.11` 是 `menu_dump` 布局仿真的**取整**，差 **0.014** —— ⛔ 不是我们算错。

**6 键档（已作废的那一档，留作反例）**：`content 990.701` · `surplus −160.611` · 首键顶 **55.794**
· 上溢出 67.31 / 下溢出 80.31 —— 已写进 `Shell/SettingsWindow.cs` 的 `TabAlignY` doc 与文件头，
`Editor/SettingsScene.cs` 的判别式注释里也加了「键数改回 6 ⇒ −80.30551 ⇒ 红」。

`SettingsTab` 枚举 **一个字节没动**（`Support=4 / Online=5`）—— `Online` 仍是**合法页号**，
`OpenTab` / `_pages` 都按它切，只是**栏里没有它的键**。

## 2 新入口钮：位置、理由、行为、词条键

**位置**（设计矩形，未过 `Screen()`）：**[1276.52, 779.56] – [1431.33, 869.56]**，即 **154.81 × 90**。
- 左沿 = `GenBtn2L + GenBtnW + GenBtnGap` = 936.52 + 300 + **40**（那个 40 是**原版 `Bottom Buttons` 的 `m_Spacing` 同值**，免得贴在 `Close Game Button` 上像它的一部分）；
- 右沿 = `GenR`（**本页内容列的右沿** —— 勾选行 / 语言行都是它；⛔ 不是 `TabsR`，那是 `Bottom Buttons` 那一行自己的右沿）；
- 顶 / 高**照抄那一行**（`GenBtnT` / `GenBtnH`）⇒ 与两颗原版钮齐平。

**为什么是这一格**（现读 `General Tab` 的节点矩形之后，本页只有三片空地）：

| # | 空地 | 为什么不用 |
|---|---|---|
| ① | 勾选行底 **683.03** → `Bottom Buttons` 顶 **779.56** 那条横带 | 落在**语言下拉列表**（原版 `LanguagesDropdown > Template` = 596.52…992.18 × 390.65…964.61）的**范围内**，列表一开就压住它 |
| ② | `Close Game Button` 右沿 **1236.52** → 本页内容列右沿 `GenR` 这一格 | ✅ **取它**：在 `x > 992.18` 之外、**任何时候都不被压**；而且它落在原版 `Bottom Buttons` 那一行**本来就没用到的余量**里（原版那颗 HLG 从左起排两颗 300 宽 ⇒ `x > 1236.52` 一直是空的）⇒ 既不压任何原版元素、也不动它们的矩形 |
| ③ | 两颗钮底下 **869.56** → 页底 966.19 | 同 ①（也在列表那一块的范围/下方） |

🔴 **这是我们自加的、原版没有**（原版那一栏只有 5 个键，`Online` 页本身也是我们自加的）⇒ 这四个数
**不是原版判据**，是**我们挑的**（已在常量 doc 里写明）。

**行为**：`OpenTab(SettingsTab.Online)` —— 与页签**同一个入口**（`OpenTab` 是切页唯一口：
切 `activeSelf` + 收语言列表 / 登录浮层 + `RefreshOnline()` + 兜一道文案）。

**词条键**：**复用 `Settings/Online/Title`**（**没加新键、没动 `Core/Loc.cs`**）：
那正是它原来在页签栏上那行字 ⇒ 撤掉页签之后**原样**搬到钮上，**一个概念一条键**（⛔ 不另造文案，
另造就会有两份、迟早不一致）。

**节点名 / 子件**：`Online Button`（`bg` / `Button Text` / `Hit`）。
**不画图标**（裁定②）；形状 = `GenButton` 同形（`40K_button` + `GenBtnTint` + 白字 + 悬停换图），
字号 **38**（我们挑的 = 它右手边那颗 `Close Game Button` 那一档），自适应四格照同族 `12 / fs / 12 · 折行 0`。

## 3 断言补丁（逐处 改前 → 改后）

### `Editor/SettingsScene.cs`

| # | 位置 | 改前 → 改后 |
|---|---|---|
| ① | `names` | `{General, Audio, Account, Graphics, **Online**}` → `{…, **Support**}`（**条数仍 5**） |
| ② | `tabTitleKeys` | 第 5 格 `"Settings/Online/Title"` → `"Settings/Support/Title"`；第 3 格 **字面 `null`** → **`"Settings/Account/Title"`**（P6 的「甲」哨兵收口；⛔ **断言没删**） |
| ③ | `want` | `i==2 ? (Loc.HasEntry(…) ? Loc.T(键) : "Account") : Loc.T(…)` → **`Loc.T(tabTitleKeys[i])`**（五格同一条算式；键被删 ⇒ `AcTerm` 退英文、而 `Loc.T` 返回**键名本身** ⇒ 仍红） |
| ④ | 键存在性 | `Account != null && Support == null` → **两颗都 `!= null`**；**新增** `FindChild(Bar, "Online") == null`（**钉裁定①**） |
| ⑤ | `pages` | 第 5 个 `"Online Tab"` → **`"Support Tab"`**（循环与内层 `j` 仍 5） |
| ⑥ | 账号页收尾 | `Click(Bar(root), "Online")` → **`Click(Bar(root), "Support")`**（语义 = 「放回**页签循环收尾时那一页**」，循环现在停在 `Support`） |
| ⑦ | 联机页那一节入口 | `Click(Bar(root), "Online")` → **`Click(Bar(root), "General")` + `Click(General Tab, "Online Button")` + 断 `Current == Online`**（照玩家走的那条路；⛔ 不改直调 `win.OpenTab`） |
| ⑧ | 波 1b probe 表 | `页签 Online` 那一行 → **两行**：`页签 Support`（`Settings/Support/Title`）+ **`General 页那颗 Online 入口钮`**（`Settings/Online/Title`，同一条键） |
| ⑨ | **新增 ⑥ 段**（General 节末） | 钮在 / 矩形 / **`Edit`：与原版 `Close Game Button` 的【渲染矩形】不相交**（判据 = 不相交，⛔ **不拿我们自己的常量比**；改坏法 = 挪到 `GenL` 上 ⇒ 红、而 ① 那条矩形断言照样绿）/ 文字 = `Loc.T("Settings/Online/Title")` / **点它 ⇒ `Current == Online`、`Online Tab` 开着、`General Tab` 关着** / 收尾切回 `General` |
| ⑩ | **新增** CheckAutoFit | `General Tab > Online Button > Button Text`，`12 / 38 / 12 / 折行 0`、框 **154.81 × 90**（本批要的「渲出来 ≤ 框」那一档） |
| ⑪ | 注释订正 | `tabTops` / `TabTop(0,5)` 那几处：写明 5 键档 = **原版真值档**、6 键档（−80.30551）当反例 |

### `Editor/MainMenuScene.cs`

| # | 改前 → 改后 |
|---|---|
| ⑫ | `tabNames.Contains("Online")` → **`Contains("Support")`**；**新增** `!tabNames.Contains("Online")`（钉裁定①）；两段过期注释**第三次**订正（原写「`Support` 建起来之后页数会变 6」已成空 —— 见 §5 顺手发现③） |

## 4 改了哪几个文件 / `git diff --numstat` / 逐文件行尾数

```
115      9       Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs
973      64      Unity/MyGame/Assets/CardPresentation/Editor/SettingsScene.cs
1667     63      Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs
```

（上面三行是 **HEAD → 工作区**的读数，含**本批别的写手**已经落的那几笔；**本件自己的净增量**
≈ `SettingsWindow +67/−2` · `SettingsScene +111/−9` · `MainMenuScene +11/−2`。）

**行尾核对（`python -I` 二进制数，逐文件）**：

| 文件 | CRLF | LF | 备注 |
|---|---|---|---|
| `Shell/SettingsWindow.cs` | **0** | 5228 | 纯 LF，**一个字节没翻** |
| `Editor/SettingsScene.cs` | **0** | 4836 | 纯 LF，**一个字节没翻** |
| `Editor/MainMenuScene.cs` | **0** | 12387 | 纯 LF（⚠️ 简报说它是 CRLF —— **实测不成立**，见 §5①） |

`numstat` 的分母离各自文件行数很远 ⇒ **不是整篇重写**；全程只走 **Edit 工具**（⛔ 无 `sed -i`、⛔ 无 python 文本模式写）。

## 5 没查清的部分 + 顺手发现（⛔ 只报不改）

### 没查清

1. **全部是静态推出来的**（本件没跑 Unity ⇒ 树 / 矩形 / 命中 / 显隐都只证明了「编得过 + 算式对」）。至少要真跑一次才算数的三件：
   ① 那颗钮在真画面上**像不像、挤不挤**（它在 `Close Game Button` 右边，中间只有 40 设计 px = 36 屏幕 px）；
   ② **英文档 `Online` 会不会被 autofit 缩**（估算 ~102px < 框 139.29px，但那是**估的**，没量）；
   ③ 下面「顺手发现⑤」那一条。
2. **P6 §6 那几条照旧**：`Terms of Service` 的显隐本地判不了、四颗外链钮的 URL 是「按语义对」不是「按地址对」—— 本件一个字没动。
3. **原版 `Tab Buttons` 父级那层 `Mask Tabs buttons` 我们仍未实现**。⚠️ 回到 5 键之后 `surplus = +5.99`
   ⇒ **不再溢出那条栏**，所以这层遮罩现在只影响「原版本来就有」的那点出入眉（首键顶高出栏顶 25.7 / 末键底低 23.8）；
   **本件没做**（不在本件范围），另立账。

### 顺手发现

1. 🔴 **简报里「`Editor/MainMenuScene.cs` 是 CRLF（已实测）」不成立**：工作区那份与 `git show HEAD:`
   那份**都是纯 LF**（改前 `CRLF=0 / LF=12378`），`git diff --numstat` 也一直是个位数行的小改。
   ⇒ 下一个写手：这份文件按 **LF** 处理（本件全程 Edit 工具，没翻行尾）。
2. 🔴 **P6 §5·1 的清单漏了三处会红的地方**（它只扫了「页签那一节」）：
   `Editor/SettingsScene.cs` 的**账号页收尾** `Click(Bar(root), "Online")`、**联机页那一节的入口**同一句、
   以及**波 1b probe 表**里的 `页签 Online` 那一行 —— 三处都以「点了不存在的键」的形式红。**本件都改了**。
   ⇒ 教训：**「页签栏里少一格」这种改动，光按「页签那一节」列清单一定会漏 —— 全库 `grep 'Click(Bar(root), "X")'` 才找得全。**
3. **P6 §5·1 (乙) 里按「6 格」算的那几条一条都不该做**：`tabTops` 换成 6 值 · `for i<5`→6 ·
   `TabTop(0,5)`→`TabTop(0,6)` 与 `-80.30551` · `TabTop(1,5)`→`TabTop(1,6)` · `pages` 那个循环的 6
   —— 按**裁定①**落地时它们本来就该**停在 5 格档**（现状就是）。本件只落了**真正该改的**那几条。
4. ⚠️ **`GenBtn2L` 那一组旧注释写的是 `.56`、实际是 `.52`**：`596.52 + 300 = 896.52`（⛔ 不是 896.56），
   `936.56` / `1236.56` 同源。判据 = 原版 `menu_rect` dump 自己印的 **`896.52`**（`Redeem Code` 那一行），
   同文件 `AcSwitchR = 896.52f` 独立印证。**新钮的坐标就挂在这个数上** —— 我先按旧注释推成了
   `1276.56 / 154.77`，**已全部改对为 `1276.52 / 154.81`**，并在常量 doc 里留了更正痕迹（铁律 5）。
5. ⚠️ **进 `Online` 页时左栏 5 格一个都不高亮**：`OpenTab` 的 `bool on = i == (int)t`，而
   `t = SettingsTab.Online = 5`、`_tabBgs.Count == 5` ⇒ 5 格全部落到 `on == false`。
   这是「**这一页在栏里没有对应格**」的必然结果。⚠️ **原版没有这一页、也没有这颗钮 ⇒ 没有原版判据**
   （铁律 11 的「原版本身就没有」那一档）⇒ **本件没改**，如实登记**请调度台裁**（要改就是给那颗钮加选中态之类，那是发明原版没有的东西）。

## 6 typecheck 读数 + 需要哪几条 Unity 自检覆盖（**本件没跑**）

```
TMPDIR=/tmp/wf_p10 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0        ← 最后一次读数（收尾）
```

改了 `.cs` **立刻**跑，共 **3 次**，**每次两栏都是 0 错**（没出现「错在别人正在写的文件上」那一档）。

**需要覆盖（写清编号名，⛔ 没跑）**：
- **必跑**：`SettingsScene.Run`（本件主宿主：页签栏 5 格 + 那颗新钮 + 九处断言补丁 + 两张词条）。
- **必跑**：`MainMenuScene.Run`（它那条页签数断言改在这儿 —— ⑤ 顺手发现③ 说的就是它）。
- **建议**：`ShellScene.Run`（它那一节 ⑥ B6 会 `SettingsWindow.Create` ⇒ 走 `Build()` 新路径）。
- **不必跑**：其余 9 条（本件没碰它们的宿主；`Loc.EntryCount` 基线也没动 —— 本件**没往表里加键**）。
