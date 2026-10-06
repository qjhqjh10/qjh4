# Block 11 · Deck 那一族 5 条账（A533 / A535 / A565 / A566 / A602 + A603）

> 写手代理 · 2026-10-14（本批「清空 A 表」）· **独占文件**：`Deck/DeckRuntime.cs` · `Editor/DeckScene.cs`（只动了这两个）
> 判据：`资料/普查产出_1013/批次计划_1013.md` §六·B（A533/A535/A565/A566/A602/A603 那几行）·
> 背景：`资料/普查产出_1013/WD1b_编辑副本隔离.md`（A565 的来源报告）· `资料/普查产出_1014/清单_A表全量.md`
> ⛔ 没跑 Unity（红线）· ⛔ 没动 git · ⛔ 没改正本 · 顺手发现只报不改

---

## 一、逐条结果

| 编号 | 改了什么（文件:行，**改后现读**） | 判据 | 做完没有 |
|---|---|---|---|
| **A533** | `Editor/DeckScene.cs:3096-3118` —— 把「三条件命一即可」的弱断言拆成**三条各断一件事** + 一条前提（`WindowsManager.Instance != null` / `ReferenceEquals(wm2.popUpWindow, one)` / `wm2.popUpWindow != one` / `!one.gameObject.activeSelf`） | §六·B A533（「形状就是『弱断言分不出两种状态』那一族」）| ✅ 加强（⛔ 不是删） |
| **A535** | **未动** —— §六·B 与 `清单_A表全量.md:60,277` 都还标着**待裁**（`A535` 在「待裁（用户 / 调度台）9 条」那张表里），本件**没看到裁定** ⇒ 按简报「停手写进报告」 | §六·B A535（「留调度台裁」）| ⛔ **待裁，改法见 §三** |
| **A565** | 实现：`Deck/DeckRuntime.cs:2352-2376` 新增 `RollBackEditingCopy()`（`State.LoadDeck(Library.Current); DeckDirty = false; RefreshAll();`）· `:2344-2350` `DiscardChangesAndLeave()` 调它 · `:2419-2423`（左钮 lambda 在 `:2423`）`ShowInvalidDeckPopUp()` **同调**（⛔ 只改一处 = 只改一半）。夹具：`Editor/DeckScene.cs:3012-3028`（A364 ⑥ 回滚断言 + **重新捕获 `live`**）、`:3039-3043`（A364 ⑦ **原地再造脏**）、`:3534-3542`（A399 ③ 两条回滚断言） | §六·B A565 · WD1b §六·1（判据 = 两颗左钮都是 `HidePopUp()` + 虚槽 `0x1b8` `GameWindow.Close()`） | ✅ 实现 + 夹具**同批**改完（静态闭合见 §二） |
| **A566** | `Deck/DeckRuntime.cs:4172-4183` —— 在 `CommitDeck()` 的方法头加一段：**「⚖️ 2026-10-14 已裁（A566）：维持现状 —— 这里【不加】守卫」**＋理由＋「什么情况下这条裁定会过期」。⛔ **守卫代码一行没动** | 简报（用户 2026-10-14 已裁：维持现状）+ WD1b §六·2 | ✅ 只写口径进注释 |
| **A602** | `Deck/DeckRuntime.cs:2546-2560` —— 第三种成因（**没落盘**）现在也回填 `_importError` + `RefreshImportText()`，文案**只写一份**（`fail` 变量，`Say` 与 `_importError` 共用）· `:2984-2989` `ImportError` 属性补文档（覆盖三种成因）· 夹具：`Editor/DeckScene.cs:3901-3906` 新增一条断言（第三种成因下 `ImportError` 非空） | §六·B A602（「`ImportError` 只覆盖前两种 ⇒ 补齐第三种」） | ✅ 代码 + 一条断言 |
| **A603** | **只记不改**：`Editor/DeckScene.cs:3846-3856`（A547 那节头部）写进一条「会打脸的耦合」注释（`DeckLibrary.Load()` 每次 `new`、无静态缓存 ⇒ 本文件所有「盘上没动」类断言 + `TestLibraryWiring` 都靠它）。`RuleEngine/Data/DeckLibrary.cs` **一个字节没碰**（在白名单外） | §六·B A603（「⚠️ 记」）| ✅ 记录（注释 + 本报告） |

---

## 二、静态判据闭合（⛔ 本代理不跑 Unity，下面是**逐行读源码 + 推演**，不是跑出来的）

### A533 —— 为什么加强后**仍然应当绿**（先证明前提成立，再证明它不再平凡真）

- `WindowsManager.Instance != null`：`:3077` 那句 `WindowsManager.EnsureHost()` 在**两条支**里都保证非空
  （`WindowsManager.cs:771` 命中已有实例直接返回；`:787` 新建那条**显式 `Instance = wm`** —— 它那段注释写明了
  「编辑模式（自检）不跑 `Awake`」，所以这里是显式登记）。⇒ 新的前提条**不会**假红。
- `ReferenceEquals(wm2.popUpWindow, one)`：`ShowMessagePopUp`（`WindowsManager.cs:1473-1507`）末尾走
  `OpenWindow(win)`，而 `OpenWindow`（`:875`）对 `type != Fullscreen` 的那一支**先** `popUpWindow = win`、
  **再** `currentWindow = win`；`PopUpGameWindow.Create` 无条件 `win.type = WindowType.Popup`（`PopUpGameWindow.cs:236`）
  ⇒ 那一刻 `currentWindow == popUpWindow == one` **必成立**。
- 收尾两条：`HidePopUp()`（`WindowsManager.cs:1520`）第一句 `cur != pop ⇒ return` ⇒ 上面那条前提保证它**不早退**，
  走到 `popUpWindow.Close()`；`GameWindow.Close()`（`:590`）尾句 `SetActive(false)`、并且**先** `NotifyClosed`
  （`:990`：`openWindows.Remove` + `if (popUpWindow == win) popUpWindow = null;`）⇒ 两条都成立。
- **旧写法为什么弱**：首项 `wm2 == null` 在「收尾信号丢失」时恒真 ⇒ 后两项一条都不用管就能绿。
  改坏法（自检是否带电）：把 `wm2.HidePopUp()` 那一句删掉 ⇒ 新的两条红；把 `!one.gameObject.activeSelf` 那条删掉
  ⇒ 只剩「字段不再指着它」，`SetActive` 那半边没人管。

### A565 —— 实现 + 夹具的连锁（**这就是那条账说的「必须连夹具一起改」**）

- **为什么两颗左钮都要回滚**：`ShowInvalidDeckPopUp()` 那颗（A364 那扇「卡组不合法」窗）与
  `DiscardChangesAndLeave()`（「丢改动」窗）在原版是**同一件事** —— `.<TrySaveDeck>b__42_1` 与
  `.<ConfirmDiscard>b__45_1` **方法体逐句相同**（`HidePopUp()` + 虚槽 `0x1b8`）；两扇窗的**左钮标签也是同一个**
  （`PopUpGameWindow.KeyDiscard` = `MainMenu/General/Discard`）。⇒ 只改一处 = 另一处仍是「关窗但副本还留着」。
- **为什么它换对象**：`RollBackEditingCopy()` 走 `State.LoadDeck(Library.Current)`，而 `DeckEditorState.LoadDeck`
  （`Deck/DeckEditorState.cs:162-174`）装的是 `deck.Clone()` ⇒ **新对象**。⇒ 任何跨这一步的 `var live =
  _rt.State.Deck;` 都会**悬空**。
- **A364 ⑥ 的夹具**（`:3012-3028`）：原来靠 `live`（`:2825` 捕获）在 Discard 之后**仍是那副 29 张、不合法**；
  回滚后它变成「库里那份（30 张、合法）」。⇒ ① 判别式换成**绝对量**（`ExportString(live) == ExportString(Library.Current)`、
  `DeckCount == keepIds.Count`、`live.Name == keepName`、`Validate() == None` —— 四条都指「回滚过」那一侧）；
  ② **`live` 重新捕获**（`:3018`）。
- **A364 ⑦ 的夹具**（`:3039-3043`）：⑦ 起手是 `EscPressed()` 并期待「**不合法** ⇒ 弹第 3 扇窗」——
  回滚之后卡组**已经合法**，那一下会直接落盘 ⇒ 前提塌。⇒ 在按 ESC **之前**原地再造一次脏
  （`live.CardIds.RemoveAt(0)`，⛔ **原地改内容、不换对象** —— 换对象又是一次悬空），并补一条
  `Check(Validate() == TooFewCards, "（前提）再摘一张 ⇒ 又不合法")`。往下的 ⑦ 一字未动：
  `live.CardIds.Insert(0, keepIds[0])` 补回的正是刚摘掉的那一张（同一副、同一个下标）⇒ 「合法 + 窗还开着」
  那条判别式（A502）照旧带电。
- **A399 ③ 的夹具**（`:3534-3542`）：这一节**没有**捕获 `live`（`snapA` 是 `new List<string>(…)` 的**副本**、
  `diskA`/`fullA` 是绝对值），所以它本来就能活；我补了两条**正向**断言（`DeckCount == fullA`、
  `ExportString(State.Deck) == diskA`）——期望值全是 Discard **之前**取的绝对量，与「回滚成谁」无关。
- **收尾不受影响**：A364 收尾（`live.CardIds.Clear(); AddRange(keepIds);`）与 A399 收尾
  （`_rt.State.Deck.CardIds.Clear(); AddRange(snapA);`）读的都是**当场**的 `_rt.State.Deck`/`live`（已重新捕获）
  ⇒ 不在悬空对象上操作。
- **不会误伤别处**：全仓 `DiscardChangesAndLeave` / 新 `RollBackEditingCopy()` 的调用点
  `grep` 只有**两处**（两个弹窗各自的左钮，`DeckRuntime.cs:2324/2423`）；`DeckScene.cs` 里点**左钮**只有
  `:3006`（A364 ⑥）与 `:3525`（A399 ③）两处（`grep 'ButtonLeft'` 复核过），**两处都已改到位**。
- **真机路径不受影响**：真机上 `BackToMenu()` 切场景 ⇒ 回滚那两行只是**多做一次**（等价、无副作用：
  `RefreshAll()` 是同一条刷新链，`Say` 之前先刷界面没有时序问题）。

### A602 —— 第三种成因现在说得出来话

- 调用链：`UiTryImport()`（`:2992`）→ `TryImport()`（`:2526`）→ `Library.Add(deck)`（`:2536`，内部 `SaveOrWarn`）
  → `bool persisted = Library.LastError == null;`（`:2541`）→ 第三支（`:2546`）**现在**先算 `fail`、写
  `_importError`、`RefreshImportText()`、再 `Say(fail)`。
- **为什么断言带电**：新断言前一句就是 `_rt.UiOpenImport()`（`:2497` 的 `OpenImport()`，第一句 `_importError = ""`）
  ⇒ 若实现没回填，这里读到的是空串 ⇒ 红；且它落在 A547 已有的**探针支**（路径必然写不进去）上，
  与「串空 / 串不合法」两支**分得开**。
- ⚠️ **如实标一处语义**：此刻 `CloseImport()`（`:2505`）**已经跑过**（弹窗早关了）⇒ 这次回填**不是给弹窗看的**，
  是给**调用方 / 自检**看的（`ImportError` 的契约 = 「为什么 false」）。这一点已写进 `ImportError` 的属性文档。

---

## 三、A535 为什么停手（**待裁，我按简报没动**）

- 判据要求 = 「`SaveAndSay()` 成功支的 `HideDeckPopUp()` 失去断言覆盖」；§六·B 与 `清单_A表全量.md` 都标
  **待裁**（`A535` 在 9 条待裁清单里），**本件没看到任何裁定** ⇒ 停手。
- **现读复核过 A535 的前提，成立**：`HandlePointer`（`DeckRuntime.cs:2004`）与 `UiClickPx` 那条（`:2970`）
  两道 `if (ModalPopupOpen) return …;` 闸 ⇒ 弹窗开着时 **Done 钮点不到**；`EscPressed()` 的第 ③ 级
  （`:3308-3312`）又在弹窗开着时**直接 return** ⇒ 今天**确实没有生产可达路径**能走到
  `SaveAndSay()` 成功支那句 `HideDeckPopUp()`（`:2253`）。夹具那边（`DeckScene.cs:3036-3038`）也**如实记着**这件事。
- **要恢复覆盖的两条路**（供调度台裁，⛔ 我不替它选）：
  ① 加自检直调口 `UiPressDone()`（**绕过** `ModalPopupOpen` 那道闸 ⇒ 验的是「函数」不是「钮的接线」，
     与「自检要走生产那条路」的老口径有张力）；
  ② 不加口，改成「**断言那句 `HideDeckPopUp()` 仍然是幂等的**」这类弱一点的替代（覆盖面小）。
  ⇒ 无论走哪条，**这是新开的一条夹具债**，需要一次 `DeckScene.Run` 才能验收。

---

## 四、顺手发现（⛔ 只报不改）

1. **`ShowInvalidDeckPopUp` 那颗右钮（`Cancel`）今天没清脏标记**：点它是「留在编辑器里」，原版那一刻也没有
   dirty 这个概念（副本销毁前 `syncedToServer` 一直在那份副本上）⇒ **不是缺陷**，但值得记：我们这边
   「不合法 ⇒ 弹窗 ⇒ 点 Cancel」之后 `DeckDirty` 仍是 `true`（A415 那节收尾就是靠这个语义跑的）。
   ⛔ **别顺手「修」成清脏** —— A415 的收尾链（点右钮 → 补回一张 → ESC 才落盘）会红。
2. **同族弱断言还留着一处**（A533 的旁证，Block 2 报告也提过）：`DeckScene.cs:3082-3083`
   `one.SecondaryShown == null || one.SecondaryShown == ""` —— 它是「两种等价的空表示」，**不是**「分不出两种状态」
   那一族 ⇒ 本件**没动**（A533 指的是三条件那条）。
3. **`live` 这种「跨 Discard 捕获的别名」还有两处**（`:2711` A330 段 · `:3098` A363 段）：今天两段**都不点 Discard**
   ⇒ 安全。但**将来谁在这两段里加一次「丢改动」**，就会踩和 A364 ⑥ 一样的坑（别名悬空、改到孤儿对象上）。
   判断依据 = 「这一段里有没有 `DiscardChangesAndLeave` / 左钮 / `RollBackEditingCopy` 能走到」。
4. **A547 那节的探针只覆盖了「写盘失败」这一种成因**：A602 补的那条断言正好补上第三种，但**「串空」那一支
   今天仍没有断言**（`_importError = "先粘贴卡组串"` 那条，`DeckRuntime.cs:2531`）—— **小尾巴，只报不改**
   （本件判据没覆盖它；谁做「导入窗」那一族时顺手补一条最省事）。

---

## 五、没做完的 / 判不了的

- **没跑 Unity**（红线）⇒ 上面所有「应当绿」都是**静态推演**。真正要跑的宿主**只有 `DeckScene.Run`**
  （两个文件都只被它引用：`DeckRuntime` 的改动只在 `Editor/DeckScene.cs` 里被断言；`grep` 过别的宿主
  不驱动这两个口）。**同步点请调度台跑它一条**（本件跨了实现 + 夹具，属于铁律 12 那张表的「只动一个宿主」那一档）。
- **判不了**的（若跑红，第一嫌疑就这几处，请把实得值原样带回，⛔ 别改期望）：
  1. A565 那四条（`3022-3028`）里 `_rt.Library.Current` 与「盘上 / 编辑器那份」是否**逐字节**一致
     —— 我按源码判「最后一次成功提交之后两边就同步」（A397 那节 `:3102` 的既有前提也这么写），
       但 `DeckLibrary.CommitCurrent` 内部字段拷贝我**没逐行读**（`RuleEngine/` 在白名单外）。
  2. A533 新增的**前提**条 `ReferenceEquals(wm2.popUpWindow, one)`：按 `OpenWindow`/`Create` 的现读推演必成立，
     但它是一条**新前提**，第一次跑才知道有没有别的路径把 `popUpWindow` 挪走。
  3. A602 那条断言依赖「`UiOpenImport()` 清空 `_importError`」这一读法（`:2949` 现读），**若它没清**，
     断言会变成平凡的（不是红、而是**假绿**）—— 我读到的实现是清的，但没跑。

---

## 六、自检

- **秒级类型检查**：`TMPDIR=/tmp/wf_b11 bash d:/4/Unity/工具/typecheck.sh`
  ⇒ **运行时错误数 0 · 编辑器错误数 0**（一次通过；**没有**出现「错全在别人文件里」那种情况）。
- **行尾**：`git diff --numstat` = `DeckRuntime.cs 65/9` · `DeckScene.cs 73/4`（**不是**整篇重写）；
  二进制数 **`DeckRuntime.cs` CRLF=4585 / LF=4585** · **`DeckScene.cs` CRLF=4089 / LF=4089** ⇒ **纯 CRLF 保住**，
  一条裸 LF 都没混进来（改前先 `git show HEAD:<路径> | file -b -` 看过：两个文件都是 CRLF）。
- **越界**：`git status` 只多出本报告；被改的源文件**只有** `Deck/DeckRuntime.cs` 与 `Editor/DeckScene.cs`
  （⚠️ `DeckScene.cs` 的 `73/4` 里**含 B2 写手那 4/1**（`Application.LogCallback` 取第 1 个参数那两行），
  那是本批**别人**的未提交改动，不是我改的 —— 我这一段落在它**下面**、两段不相交）。
- **没动的文件**（复核过）：`RuleEngine/Data/DeckLibrary.cs`（A603 / A566 都点到它，**都在白名单外**，一字未动）·
  `Shell/WindowsManager.cs`（A533 只**读**它）· `Shell/PopUpGameWindow.cs`（只读）。
