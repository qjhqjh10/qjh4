# H44 · A396 + A398 + A399（Deck 一族三笔）—— 写手报告（2026-10-12）

> 本件 = `普查产出_1012/S6_剩余现核_前半.md` 的 **A396 / A398 / A399**（三笔都在卡组编辑这条线上）。
> 白名单内只动了 **3 个文件**：`CardPresentation/Deck/DeckRuntime.cs` · `RuleEngine/Data/DeckLibrary.cs` ·
> `CardPresentation/Editor/DeckScene.cs`。**没跑 Unity 批处理、没动 git、没改任何正本**
> （用户口径：A 表清完再一起跑自检；本件是收尾前最后一件 ⇒ 报告只给**该跑哪几条**）。
> 类型检查：`TMPDIR=/tmp/wf_h44 bash Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（改完跑了三次，都过）。
> 行尾：三个文件改前改后都是**纯 CRLF**（`b.count(b'\r\n') == b.count(b'\n')`，逐个数过；没有一句被翻成 LF）。

---

## 一 · 结论

| 账 | 改向了没有 | 一句话 |
| --- | --- | --- |
| **A396** | ✅ | `ClickOrder` 里把 **`name_clear` 挪到 `name_box` 前面**（清空图标画在输入框底**之上**）⇒ 那颗钮**用鼠标点得到了**；与事实不符的那句注释（「本窗矩形今天互不重叠」）就地订正 |
| **A398** | ✅ | `Rename` / `Delete` ⇒ **`return Save();`**（照 `CommitCurrent` 先例）；`Create` / `Duplicate` / `Add` 的**返回类型装不下** bool ⇒ 新增 `SaveOrWarn()`（`LastError` + 一条警告），并如实标注没做的那一半 |
| **A399** | ✅ | **`hdr_back` 按原版改走 `TryClose()`**：**脏 ⇒ 弹「丢弃改动」两钮窗；干净 ⇒ 直接关**；⛔ **关闭钮这条链从此不再保存**（原来「先 `SaveAndSay()` 再离场」是偏离） |

---

## 二 · 判据（逐条亲读，都在 `d:/2/tools/`）

### A399 —— `TryClose` 那条链（**唯一判据**）

`decomp_full/DeckEditingWindow__TryClose.c`（行号 = `cat -n` 现读）：

```text
:5   if (param_1[0x23] == 0) …                      // 0x23×8 = 0x118：窗上那份**编辑中的** CardDeck
:9   if (*(char *)(param_1[0x23] + 0x60) == '\0')  // +0x60 = syncedToServer；突变写 0 = **脏**
:10      DeckEditingWindow__ConfirmDiscard(param_1, 0);
:11      return;                                    // ⇒ **脏了先问**
:15  (**(code **)(*param_1 + 0x1b8))(param_1, …);   // 虚槽 0x1b8 = GameWindow.Close() ⇒ **直接关**
```

`decomp_full/DeckEditingWindow__ConfirmDiscard.c`：

```text
:25/:41  右钮：+0x10 = DAT_1842be120（标签）· :41 +0x18 = 委托（回调）
:46/:49  左钮：+0x10 = DAT_1842be418（标签）· :49 +0x18 = 委托到**窗**上的 DAT_1842d13c0
:54  WindowsManager__ShowPopUp(lVar4, DAT_1842d00e8, 1, 0, /*5th=左*/lVar2, /*6th=右*/lVar1, 0)
```

两颗回调的**方法体**也读了：

| 钮 | 回调 | 体 | 语义 |
| --- | --- | --- | --- |
| 左 `DAT_1842be418` | `DeckEditingWindow___ConfirmDiscard_b__45_1.c` | `HidePopUp()` + 虚槽 `0x1b8` | **丢弃 + 关窗** |
| 右 `DAT_1842be120` | `DeckEditingWindow.__c___ConfirmDiscard_b__45_0.c` | **只有** `HidePopUp()` | **留在编辑器里** |

🔴 **三个地址都查实了，不是猜的**：

1. **正文键** `DAT_1842d00e8` → `il2cpp_out/stringliteral.json` 的地址表 = **`MenuDeck/HUD/DiscardChanges`**
   （同表 `0x42BE418 → MainMenu/General/Discard` · `0x42BE120 → MainMenu/General/Cancel`，
   与 `Shell/PopUpGameWindow.cs` 文件头那两行**逐字对得上** ⇒ 这套读法本身有旁证）。
2. **`DAT_1842d13c0` 就是 `b__45_1`** → `il2cpp_out/script.json`（十进制 `70063040`）：
   `"Name": "Method$DeckEditingWindow.<ConfirmDiscard>b__45_1()"`。
   同一张表里 `0x42D16C0`(= `70063808`) 正是 A364 读过的 `.<TrySaveDeck>b__42_1` ⇒ 对位无误。
3. `+0x60` = `syncedToServer`、`0` = 脏 —— A363 那一轮已经坐实（`资料/普查产出_1012/D1_卡组脏标记与补名.md` §一）。

⚠️ **这条链只由【关闭钮】走**：我们的 `hdr_back` = 原版 `Content Area/Header/Close`（`Button Text` = 'Back'，
出处 `资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md:43`）。**`ESC` 是另一条**（= `TrySaveDeck`）。

### A396 —— 谁画在上面

| 件 | 矩形（现读常量） | 画在哪一档 |
| --- | --- | --- |
| `name_box`（输入框底 `name_bg`） | `DeckRuntime.cs:119` `NameX/Y/W/H = 9.5, 311, 307.7, 50` ⇒ x∈[9.5,317.2] y∈[311,361] | `QPanel = 3004`（`:695`） |
| `name_clear`（清空图标） | `:121` `NameClrX/Y/W/H = 277.2, 316, 35, 40` ⇒ x∈[277.2,312.2] y∈[316,356] | `QBorder = 3005`（`:714`） |

⇒ 图标**整个落在**输入框里、而且**画在它上面** ⇒ 命中顺序必须照可见层：`name_clear` 在前。
（原来 `ClickOrder` 里 `name_box` 在前 ⇒ `TopKeyAt(294.7, 336)` 恒是 `name_box`。）

**顺带证了「真鼠标那条路也够得着」**（`UiClickPx` 会跳过 `HandlePoolClick` / `HandleDeckRowClick`，
所以要单独核这两处）：`HandlePoolClick` 要求 `px.x ≥ PoolX(330.2)`、`HandleDeckRowClick` 要求
`px.y ∈ [ListY(366), 1010.1]` —— 图标中心 `(294.7, 336)` **两条都不满足** ⇒ 真指针也会落到 `HandleButtons`。

### A398 —— 只改「吞掉返回值」，不改别的

`CommitCurrent` 在 A363 已经修成 `return Save();`（D1 §二 那一行），本件把同族的对齐；
返回类型是 `PlayerDeck` 的三个装不下 bool —— **没有**为了回传 bool 去改签名（那会连带断 `Shell/CollectionData.cs`）。

---

## 三 · 改动清单（文件:行号 = **改完之后**的现读值，每处一句为什么）

### `CardPresentation/Deck/DeckRuntime.cs`（A396 + A399）

| 行 | 改了什么 | 为什么 |
| --- | --- | --- |
| `:2124-2143` | `ClickOrder`（`:2139-2143`）里 **`name_clear` 挪到 `name_box` 前面**；上面那段注释（`:2124-2138`）**订正**（删掉「本窗矩形今天互不重叠 ⇒ 两套顺序行为一致」，改成「**顺序判据 = 谁画在上面谁先吃到**（可见层）」+ 记下这一对就是重叠的） | **A396 本体**；订正那半句照铁律 5（它与事实不符，而且正是它把缺陷说成了「看不出分叉」） |
| `:2145-2165` | `ClearDeckName` 的文档段（`:2165` 那个方法，文档在它上面）：把「顺手发现（本件只报未改）」改成「**已修**」+ 判据 | 铁律 5：那条待办已销，不能留着旧话 |
| `:2172-2176` | `UiNameClear` 文档：「⛔ 不能用 `UiClickPx` 代替」→「**已经点得到了**，自检优先走真鼠标那条路」 | 同上（这句已过期） |
| `:2191` | `case "hdr_back"` ⇒ **`TryClose();`**（原来 `SaveAndSay(); BackToMenu();`） | **A399 本体**：原版关闭钮**从不保存** |
| `:2187-2190` | `hdr_back` 上那段注释改向（原来写「我们不做『存不了就把人扣下』」） | 那是旧口径的说明，已不成立 |
| `:2258-2282` | **新增**「关窗」整段判据注释（`TryClose.c` / `ConfirmDiscard.c` / 两颗回调 / 三个地址的出处） | 铁律 2/3：判据写进代码注释，人不用再去翻 |
| `:2284-2287` | **新增** `public const string DiscardChangesKey = "MenuDeck/HUD/DiscardChanges";`（文档 `:2284-2286`） | 正文键是**地址表实读**的，唯一出处 |
| `:2289-2301` | **新增** `public void TryClose()`（文档 `:2289-2296` / 方法 `:2297-2301`） | A399 本体（脏⇒问 / 干净⇒走，两条都不保存） |
| `:2303-2313` | **新增** `void ShowDiscardChangesPopUp()` | = 原版 `ConfirmDiscard`：复用 A364 那条 `ShowMessagePopUp` 宿主链（原版也是同一个 `ShowPopUp`） |
| `:2316-2331` | **新增** `void DiscardChangesAndLeave()` | = `b__45_1`（`HidePopUp` + 关窗）；顺手把**脏标记清掉**（不清的话「已丢弃」之后按 Done 又会写进去） |
| `:290`（`DeckDirty` 文档）/ `:2073`（`BackToMenu` 文档） | 订正「真落盘只留 `SaveAndSay()`（`hdr_back` 与 ESC 走的是同一个函数）」「『返回』= **存盘之后**离场」两句 | 关闭钮改向之后这两句都假了（铁律 5） |

### `RuleEngine/Data/DeckLibrary.cs`（A398）

| 行 | 改了什么 | 为什么 |
| --- | --- | --- |
| `:20-31` | 类文档补 A398 那一节（哪些 `return Save();`、哪些走 `SaveOrWarn`、为什么） | 两处口径写在一处 |
| `:75-83` | `Create`：`Save();` → **`SaveOrWarn("Create");`** | 返回值是对象、装不下落盘成败 ⇒ 至少**说得出话** |
| `:91-102` | `Rename`：`Save(); return true;` → **`return Save();`** | 原来**写失败也报成功**（静默）；⚠️ 就地写明 `false` 现在有**两种**含义 |
| `:105-115` | `Duplicate`：`Save();` → **`SaveOrWarn("Duplicate");`** | 同 `Create` |
| `:117-130` | `Delete`：`Save(); return true;` → **`return Save();`** | 同 `Rename`；⚠️ 如实标注 `Shell` 侧把两种含义混着用 |
| `:163-188` | **新增** `bool SaveOrWarn(string who)` | 失败时 `LastError`（`Save()` 写的）+ **一条 `Debug.LogWarning`**；⛔ 不改那三个方法的签名 |

### `CardPresentation/Editor/DeckScene.cs`（三笔的断言）

| 行 | 改了什么 |
| --- | --- |
| `:3078-3108` | A363 那一节的 ② **改向**：`name_clear` **改走 `UiClickPx`（真鼠标那条路）** + 新增 A396 四条与**反向对照**两条（`name_box` 自己那块地还得进改名态） |
| `:3221-3272` | **新增一整节**「A398：`DeckLibrary` 的落盘失败说得出话」 |
| `:3274-3351` | **新增一整节**「A399：关窗 = `TryClose`」 |

---

## 四 · 断言（逐条 + 改坏法）—— 全部落 `DeckScene.Run`

### A396（`:3086-3108`）

| # | 断言（简写） | 改坏法 ⇒ 红 |
| --- | --- | --- |
| 1 | `UiTopKeyAt(294.7, 336) == "name_clear"` | 把 `ClickOrder` 里那一对换回原顺序 / 删掉 `name_clear` ⇒ 红 |
| 2 | `UiClickPx(294.7, 336)` **有人吃** | 同上 |
| 3 | ……名字真的被写成「新卡组」+ 标脏 + 盘上没动（A363 那三条**照旧**） | 清名字那条路被绕开 ⇒ 红 |
| 4 | **反向**：`UiTopKeyAt(60, 336) == "name_box"` 且点下去进**改名态**（`UiEditKind == 1`） | 把 `name_box` 整颗从表里删掉（= 把「点名字框改名」弄丢）⇒ 红 |

### A398（`:3228-3272`）

| # | 断言（简写） | 改坏法 ⇒ 红 |
| --- | --- | --- |
| 1 | （前提）探针目录存在 + `LastError` 起手为空 + **`Save()` 在这条路径上确实返 false** | 探针路径其实写得进去 ⇒ 红（挡住「假绿」） |
| 2 | 坏路径下 `Create` / `Duplicate` / `Add` ⇒ **`LastError != null`** | 把那三处换回裸 `Save();` **且**去掉警告 ⇒ 红 |
| 3 | 坏路径下 `Rename` / `Delete` ⇒ **返回 false** | 换回「`Save(); return true;`」⇒ 红 |
| 4 | **控制组**：好路径下 `Create`（`LastError` 空）/ `Rename` / `Delete` 全回 **true** | 一个「恒返 false / 恒报失败」的实现 ⇒ 红（**只测失败那一半会被它蒙过**） |

### A399（`:3279-3351`）

| # | 断言（简写） | 改坏法 ⇒ 红 |
| --- | --- | --- |
| 1 | （前提）抽屉关着 + 无模态窗 + `UiTopKeyAt(267.2, 113.5) == "hdr_back"` | —— |
| 2 | **干净** ⇒ 点关闭钮**直接离场**（`LeaveCount +1`）、**不弹窗**、盘上没动 | 删掉 `!DeckDirty` 那一支（干净也问）⇒ 红 |
| 3 | **脏** ⇒ **不离场** + 弹出模态窗 | 脏那一支写成直接 `BackToMenu()` ⇒ 红 |
| 4 | 窗上 **`MessageKey == "MenuDeck/HUD/DiscardChanges"`** | 自己编一句人话 / 换键 ⇒ 红 |
| 5 | 窗上 `PrimaryKey == MainMenu/General/Discard` · `SecondaryKey == MainMenu/General/Cancel` | 两颗钮接反 ⇒ 红 |
| 6 | 右钮（Cancel）⇒ 窗关、**没离场**、**脏标记留着**、盘上没动 | 把 Cancel 也接成离场 ⇒ 红 |
| 7 | 左钮（Discard）⇒ **离场 +1**、窗关、**脏标记清掉** | 那颗回调接成空钮 ⇒ 红 |
| 8 | ……而且**盘上一个字节都没动**（`ExportString` 指纹） | 把 `DiscardChangesAndLeave` 里的 `BackToMenu()` 换成 `SaveAndSay()` ⇒ 红 |
| 9 | （收尾）状态层逐字节还原 + `EscPressed()` ⇒ 盘上复原成起手那份 | 还原没做干净 ⇒ 红 |

---

## 五 · 没查清 / 如实标注（⛔ 不猜）

1. 🔴 **「丢弃」在我们这边只做了一半**：盘上那份**本来就没被动过**（A363 起突变只标脏），会留下的是**内存**
   那份 —— 而 `State.Deck` 与 `Library.Current` 是**同一个对象**（D1 §五·2 那笔账）。⇒ `DiscardChangesAndLeave`
   **不做内存回滚**，只清脏标记；原版那条路是「编辑副本 + 关窗销毁」，我们的等价物是**真离场时场景整个重来**。
   ⛔ 本件没去动那条账（要动 `Build` / `TryImport` / 夹具里 `var live = _rt.State.Deck` 那几处的心智模型）。
2. 🔴 **弹窗开着时按 ESC 这件事，我们和原版不一样**（**既有偏离**，A364 段在 `Editor/DeckScene.cs:2944-2950`
   已如实标注并有断言钉着）：原版那一刻 ESC 归**最上面那扇窗**（`closeOnESC = 0` ⇒ 什么都不做），
   而 `DeckRuntime` 不是 `GameWindow`、直接轮询键盘 ⇒ 我们的 ESC 仍会走 `SaveAndSay()`。
   **A399 把这扇新窗也带进了同一个已知偏离**：丢改动窗开着时按 ESC ⇒ 我们**保存**并顺手把那扇窗收掉，
   原版是**什么都不发生**。⛔ 本件**没改**（改它 = 推翻 A364 那条明确决定 + 把 `:2951-2952` 那条断言改向）
   ⇒ **建议调度台单独裁一笔**。
3. **`Create` / `Duplicate` / `Add` 的返回值永远装不下「落盘成没成」**：本件用 `SaveOrWarn`（`LastError` + 警告）
   代替，**没改签名**（改了会断 `Shell/CollectionData.cs` 三处）。要一个真正的 bool 只能走 `CommitCurrent`。
4. **A398 的失败探针只用了一种失败条件**（父目录不存在 ⇒ `DirectoryNotFoundException`）——
   磁盘满 / 权限 / 只读归档这些**没测**（那些不确定，不适合进自检）。`SaveAll` 对它们走的是同一条 `catch`。
5. **原版 `TryClose` 的 `param_1[0x23] == 0`（编辑中那份为空）那一支**：原版直接炸（`FUN_1803f47a0` 不返回）
   —— 我们**没有**对应的判空场景（`State.Deck` 恒非空）⇒ 这一支**没法照做也无需照做**，如实记一笔。

---

## 六 · 顺手发现（⛔ 本件只报不改）

1. **`Shell/CollectionData.cs` 的三个调用点既不读返回值、也不读 `LastError`**：
   `CreateDeck`(:125-133) / `DuplicateDeck`(:102-108) / `ImportDeck`(:114-122) —— 每一个都在 `DeckLibrary.*`
   **之后又调了一次 `Lib.Save()`**（那个返回值同样丢掉）⇒ 写盘失败时玩家看到的是「成功」，盘上却没变。
   **建议另开一笔账**（`Shell/*` 不在本件白名单）。
2. **`Shell/CollectionData.DeleteDeck:104` 把两种含义混着打印** —— `Delete` 现在回 `false` 有两种原因
   （越界没删 / 删了但没落盘），而 `Shell/DeckInfoPopup.cs:1260` 的文案只写了前一种（「删不了（只剩一套时不许删 / 下标越界）」）。
3. **`DeckLibrary.Delete` 的「只剩一套不许删」这条规矩其实不存在**：`Delete` 本身删到 0 套也不拦
   （`_current = -1`），拦的是 **UI 侧**的 `DeleteInteractable`。而 `Shell/CollectionData.cs:99` 的文档
   写的是「**`DeckLibrary.Delete` 的规矩**」⇒ 文档与实现错位（既有，本件没动：在 Shell 侧 + 会牵断言）。
4. **`资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md:43`** 那张表里 `Close` 一行的「❌ **未接**」**早就过期**
   （A24 已接悬停换图，本窗自检里有 `CheckHoverOne("hdr_back", …)`）—— 与本件无关，顺手报。

---

## 七 · 复跑建议（给调度台）

- **必跑**：**`DeckScene.Run`** —— A396 / A398 / A399 的**全部**断言都落在这一个宿主里。
- **另加**：**`RuleEngineTest.Run`** —— 本件动了 `RuleEngine/Data/DeckLibrary.cs`（`Rename` / `Delete` 的返回值
  换了语义 + 新增 `SaveOrWarn`）。`DeckRulesTest` 里那几条（`CheckTrue(lib.Rename(0, "改名了"))` ·
  `lib2.Delete(2)` · `!lib2.Delete(0)`）**预期不红**：那个自检把 `DeckStore.OverridePath` 指到临时目录，
  落盘会成功 ⇒ `Save()` 回 true；`!lib.Rename(0, "")` / `!lib.Rename(99, …)` 走的是**更早的早退**，一个字没动。
- 其余 9 条与本件无关。
- ⚠️ 自检一旦真跑，`DeckScene.Run` 的日志里会出现 **3 条 `[DeckLibrary] …落盘失败…` 警告** ——
  那是 A398 探针**故意**造的（坏路径那一段），**不是**自检红。

---

## 八 · 过期文档点名（⛔ 本件不改 —— 越白名单；由调度台合并时处理）

| 文档 | 现在该改什么 |
| --- | --- |
| `普查产出_1012/D1_卡组脏标记与补名.md` §五·b 的 **1 / 2 / 3** | 三条「待接线」**本件全做了** ⇒ 可销账（1 = A399 · 2 = A398 · 3 = A396） |
| `普查产出_1012/S6_剩余现核_前半.md` `:45` / `:47` / `:48` | A396 / A398 / A399 三行 ⇒ 可销账 |
| `普查产出_1011/WB1_A330.md:118` | 「`hdr_back` 的『离场』**不看保存成败**」—— 关闭钮这条链现在**整个不保存**了（A399），这句只对 ESC 那一半成立 |
