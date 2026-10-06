# Block23 · A571 —— 「重开」那一族补前提出声（19 行断言）

> 写手：Block23（独占 `Editor/{ShopScene,SettingsScene,ShellScene,CollectionScene}.cs`）· 2026-10-14
> 判据 = `资料/普查产出_1014/RO_窗口重建三件.md` §A571（逐字读）· `WA505_同实例重开普查.md`
> 句式照 **A94 全族已断的 9 处**（`grep -n "A94 收尾" Editor/*.cs`）
> **没跑 Unity**（硬纪律）⇒ 绿/红是**静态推理 + 秒级类型检查**，不是实跑结论。

---

## 一、逐处表（18 处 (b) + 1 处 (a)，全部做完）

**改后一律**（(b) 的 18 处，字符串逐字 = RO 文件 §A571·② 给的那句）：

`CheckTrue(<x>.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");`

⚠️ **原行尾注释一个字没删** —— 有注释的 8 处改成两行式（注释挂在第一行、message 缩进到第一实参下），无注释的 10 处仍是单行。

| # | 文件 | 原行（逐字） | 改后（逐字） | 做了没有 |
|---|---|---|---|---|
| 1 | `CollectionScene.cs` | `                    dipA.TryOpen(null);                                      // 态一（开关关）建一遍 ⇒ p1 == 设计点` | `                    CheckTrue(dipA.TryOpen(null), // 态一（开关关）建一遍 ⇒ p1 == 设计点`<br>`                              "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 2 | `CollectionScene.cs` | `                                          dipA.TryOpen(null);         // 🔴 态二**必须重建**（见文件头 ③）` | `                                          CheckTrue(dipA.TryOpen(null), // 🔴 态二**必须重建**（见文件头 ③）`<br>`                                                    "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 3 | `CollectionScene.cs` | `                    pmpA.TryOpen(null);` | `                    CheckTrue(pmpA.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 4 | `CollectionScene.cs` | `                                          pmpA.TryOpen(null);         // 🔴 态二**必须重建**（见文件头 ③）` | `                                          CheckTrue(pmpA.TryOpen(null), // 🔴 态二**必须重建**（见文件头 ③）`<br>`                                                    "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 5 | `SettingsScene.cs` | `                w1.TryOpen(null);` | `                CheckTrue(w1.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 6 | `SettingsScene.cs` | `                w2.TryOpen(null);` | `                CheckTrue(w2.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 7 | `SettingsScene.cs` | `                w3.TryOpen(null);` | `                CheckTrue(w3.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 8 | `SettingsScene.cs` | `                w4.TryOpen(null);` | `                CheckTrue(w4.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 9 | `SettingsScene.cs` | `                    a228w2.TryOpen(null);                                 // = 生产那条路（挂缩放器 + SetScale）` | `                    CheckTrue(a228w2.TryOpen(null), // = 生产那条路（挂缩放器 + SetScale）`<br>`                              "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 10 | `SettingsScene.cs` | `                a167w1.TryOpen(null);` | `                CheckTrue(a167w1.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 11 | `SettingsScene.cs` | ``                a167w2.TryOpen(null);                             // 挂缩放器 + `SetScale` `` | ``                CheckTrue(a167w2.TryOpen(null), // 挂缩放器 + `SetScale` `` <br>`                          "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 12 | `ShellScene.cs` | `                ppA.TryOpen(null);                                       // 态一（开关关）建一遍 ⇒ p1 == 设计点` | `                CheckTrue(ppA.TryOpen(null), // 态一（开关关）建一遍 ⇒ p1 == 设计点`<br>`                          "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 13 | `ShellScene.cs` | `                                      ppA.TryOpen(null);              // 🔴 态二**必须重建**（见文件头 ③）` | `                                      CheckTrue(ppA.TryOpen(null), // 🔴 态二**必须重建**（见文件头 ③）`<br>`                                                "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 14 | `ShellScene.cs` | `                ipA.TryOpen(null);` | `                CheckTrue(ipA.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 15 | `ShellScene.cs` | `                                      ipA.TryOpen(null);              // 🔴 态二**必须重建**（见文件头 ③）` | `                                      CheckTrue(ipA.TryOpen(null), // 🔴 态二**必须重建**（见文件头 ③）`<br>`                                                "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 16 | `ShopScene.cs` | ``            a294w2.TryOpen(null);                                   // = 生产那条路（挂缩放器 + `SetScale`）`` | ``            CheckTrue(a294w2.TryOpen(null), // = 生产那条路（挂缩放器 + `SetScale`）`` <br>`                      "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 17 | `ShopScene.cs` | ``            a297gw2.TryOpen(null);                                   // = 生产那条路（挂缩放器 + `SetScale`）`` | ``            CheckTrue(a297gw2.TryOpen(null), // = 生产那条路（挂缩放器 + `SetScale`）`` <br>`                      "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| 18 | `ShopScene.cs` | `            a298gw2.TryOpen(null);` | `            CheckTrue(a298gw2.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");` | ✅ |
| **19=(a)** | `ShopScene.cs` | `        if (win.CurrentState == WindowState.Closed && win.Manager != null) win.Manager.OpenWindow(win);`（助手 **`ClosePackAndReopenShop` 体内最后一句**） | 原句**保留**，其后**紧接一行**：<br>`        CheckTrue(win.CurrentState == WindowState.Open, "（前提）商店重开之后真的开着 —— 下面几段都靠它");` | ✅ |

### 对账（计数）
- `grep -c "（前提）重开之后窗真的开着 —— 下面那条才不是空断"` → `CollectionScene 4 · SettingsScene 7 · ShellScene 4 · ShopScene 3` = **18**（与 RO 逐处表**逐文件相符**）· 全 `Editor/*.cs` 里**只有这四处有**（没有多写、没写串文件）。
- (a) 那句 `grep -c "（前提）商店重开之后真的开着 —— 下面几段都靠它"` → `ShopScene 1` = **19 总数**。
- 🔴 `grep -nE "^\s*[A-Za-z_][A-Za-z0-9_]*\.TryOpen\(null\);" Editor/*.cs` ⇒ **零命中**（18 处裸调已清零）。
- **行号**：18 处的行号**与 RO 表完全对得上**（无漂移）· RO 记的 WA505 那条助手 `:1039` 确实已漂 —— 改前在 `ShopScene.cs:1068`（改后 `:1078`，`grep -n "ClosePackAndReopenShop"` 复核过）。

## 二、绿/红判定（**静态**，没跑 Unity）

- **(b) 18 处**：`Shell/WindowsManager.cs:484-516` 的 `OpenByState()` **三支全部 `return true`**（`Closed ⇒ true` / `Background ⇒ true` / `Open ⇒ true`）⇒ 带参 `TryOpen(null)` 今天**恒返回 true**；且三支里 `CurrentState` 走出来都是 `Open`（`Closed` 支写 `CurrentState = Open`；`Background` 支写回 `Open`；`Open` 支本来就是 `Open`）⇒ 这 18 条**是前提守护、不会因今天的实现变红**。
  - ⚠️ 同理它们**也挡不住**「`TryOpen` 返回 true 但窗没开」——那种局面今天不存在（见上），真正的判据是**下面那些断言**（它们读 `FindChild(...)` 拿到没拿到件）。**这条断言的价值 = 把「窗开着」这句隐含前提从静默变出声**（RO §A571② 的原话），不是新增不变量。如实记。
- **(a)**：助手 6 个调用点（`ShopScene.cs` 的 `:1597/:1627/:1719/:2176/:2529/:3210`）里 `win` 的状态走向**逐条核过**：
  ① 开包窗是 `Fullscreen` ⇒ `OpenWindow`（`:881-886`）走 `HideAllWindows()`，而被藏的窗**是 `Closed`**（`WindowsManager.cs:1070` 注释实证）⇒ 助手末句 `if (state == Closed && Manager != null)` **会命中** ⇒ `OpenWindow(win)` → `:898 win.TryOpen(data)` → `Closed` 支 ⇒ `state = Open` ✔；
  ② `bp == null`（买卡包那条没走到，`:3210` 那一处）⇒ 没人动过商店 ⇒ 仍是 `Open` ✔；
  ③ `bp != null` 且没被藏 ⇒ `bp.Close()` → `ShowPreviousWindow`（`:1042-1072`）→ 对列表尾调**无参 `TryOpen()`** → `Closed` 支重建 / `Background` 支提回前台 ⇒ 都落到 `Open` ✔。
  ⇒ 三路都不落 `Background`，新断言**绿**。

## 三、验证（跑了什么）

| 项 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_b23 bash d:/4/Unity/工具/typecheck.sh` | **运行时错误数: 0 · 编辑器错误数: 0** ✅（没有出现「错误集中在别人正在写的文件」那种情形） |
| 行尾 | `python` 二进制数 `\r\n` / `\n` | 四个文件改后 **CRLF=0**（改前也 **CRLF=0**，纯 LF）⇒ 照原行尾，没翻；`U+FFFD=0`（中文没坏） |
| 差异归属 | `git diff --numstat`（改前 → 改后） | `CollectionScene 62/6→69/10` · `SettingsScene 12/10→21/17` · `ShellScene 116/18→123/22` · `ShopScene 61/19→67/22` —— **增量逐笔对得上本次 20 处 Edit**（+7/-4 · +9/-7 · +7/-4 · +6/-3）⇒ 改动**期间没有别人动这四个文件**（没撞车、也没被顶掉） |
| 撞车预检 | `git diff -- <f> \| grep TryOpen`（改前） | 四个文件的**未提交改动里没有一处 `TryOpen`** ⇒ 与在飞写手不重叠 |

## 四、顺手发现（**只报不改**）

1. 🔴 **`RewardsScene.cs:5658` 那处「已断」的其实是 A570 立账的那 1 处**（`cw` = `CampaignRewardWindow`，**真读 `Data` / 覆写 `SetupData`**）—— RO §A571·① 已写明「27 处里落在真读 `Data` 的窗上只有 1 处 ⇒ 已由 A570 立账」。本次**没碰它**（不是我的文件、也不在 A571 的 19 行里），只是提醒合并时别把 A570 的账记到 A571 头上。
2. ⚠️ **`Editor/MainMenuScene.cs` 的 6 处（`:2740/:2744/:3932/:4414/:4622/:7222`）已断**，其中 `:2740` 那句是 **`（A94）`** 而不是 `（A94 收尾）`（同一族、前缀少两个字）—— 无害，但若将来要按 `grep "A94 收尾"` 数「全族几处」会**少数一处**。**不是缺陷**，只影响日后统计口径。
3. `Shell/WindowsManager.cs` 的 `OpenByState()` 三支**全 `return true`** ⇒ `TryOpen` 的 `bool` 返回值今天**不携带「有没有真的开」的信息**（它实际是「有没有走到/处理完」）。任何**将来**想用 `CheckTrue(x.TryOpen(...))` 当「开成功」判据的地方都会**恒绿**（= 一种同式自证）。🔴 这属于 A447 已定案的接口语义（照原版那两个重载），**不建议改**；记在这里是给「谁想拿它当成功判据」的人一个警示。
4. `ShellScene.cs` 工作区里（**别的写手**的未提交改动）有 A674 订正的口径注释：「触发集 = 任何窗的 `OpenWindow` / `TryOpen` / `.Close()`」—— 与本节 §二 的状态链推理**一致**，没冲突。

## 五、没做完的（+为什么）

- **没有**（19 行全做完）。
- ⛔ **按简报有意不做**的（不是遗漏，逐条记）：① 不把 `TryOpen(null)` 改成无参 `TryOpen()`（与 A94 全族不一致、且无参那条**不调 `SetupData`** ⇒ 语义不同，RO §A571·② 明令别改）；② 不顺手加「重开后内容非空」之类新判据（那是新判据、越界，RO §A571·②(c) 标了「建议不做」）；③ **没跑 Unity**、没动 git、没改正本。
