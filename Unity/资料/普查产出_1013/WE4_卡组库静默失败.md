# WE4 · 卡组库静默失败（A503）—— 写手报告（2026-10-13）

> 白名单内动过 **2 个文件**：`Shell/CollectionData.cs`（本体）· `Editor/CollectionScene.cs`（断言，加在 `Run()` 最末尾）。
> **没跑 Unity 批处理、没动 git、没改任何正本**。类型检查跑了 **2 次**（改完 `CollectionData` 一次、加完断言一次）⇒ **两次都 0 错**（§七）。
> 行尾：两个文件改前改后都是**纯 LF**（`b.count(b'\r\n') == 0`）；`git diff --numstat` = **`78/14`** 与 **`78/0`**
> （数字与我的改动量吻合 ⇒ **不是整篇重写**，没翻行尾）。
>
> 🔴 **先说一条要调度台过目的（文件所有权）**：`Editor/CollectionScene.cs` 在 `批次计划_1013.md` §波 6 里是
> **W-E2 的独占文件**（A404 + A512 的 `CollectionScene` 那半）。我**动笔前**查过三样，确认那一刻**没有写手在动它**：
> ① `git diff --numstat` **空**（无未提交改动）· ② mtime = **2026-10-05 23:33**（上一轮，不是今天）· ③ W-E2 的另一个文件
> `Shell/CardDetailPopup.cs` 同样冷（`10-05 22:58`）⇒ 满足简报「**只在你确定宿主、且那个文件不是别人的活时**」。
> 我的改动是**纯追加、自包含一个 `{ }` 块、落在 `Run()` 最末尾**，与 W-E2 的落点（`:2488` 一行 + `CardDetailPopup.cs`）**不相交**。
> 若调度台仍认为该文件只归 W-E2 ⇒ **把这一段整块搬走即可**（它就是 §四 里那一节）；⛔ **别两处各留一份**。

---

## 一、结论

| 验收条目 | 做没做 | 一句话 |
|---|---|---|
| ① 三个口把失败**如实报出来** | ✅ | `CreateDeck` / `DuplicateDeck` / `ImportDeck` 现在都**读 `LastError`**：写盘失败 ⇒ 按各自既有契约**回空串**（`ImportDeck` 顺带把原因写进 `out why`）+ **一条 `Debug.LogWarning`**；三处多余的 `Lib.Save()` 全删（`DeckLibrary` 内部本来就是 `SaveOrWarn`） |
| ② 「没删」与「删了没落盘」**分开报** | ✅ | `DeleteDeck` 按**调用前后 `Decks.Count` 变没变**分两支，报两句**不同的话**；新增 `CollectionData.LastDeleteError`（成功时空串）供调用点直接用 |
| ③ `:99` 文档与实现对齐 | ✅ | 选 **改注释**（不是补实现）—— 理由见 §三 |
| ④ **有断言**、能分辨两种状态、有改坏法 | ✅（**未跑**） | `Editor/CollectionScene.cs` 末尾新增一整节（**19 条断言**，下表按语义归成 14 行）：**控制组**（好路径）+ **探针**（写不进去的路径）+ 两种 `false` 分开断。⚠️ **自检没跑过**（Unity 全局串行、只有主对话能跑）⇒ 见 §五·1 |
| ⑤ 类型检查 0 错 | ✅ | 运行时 **0** / 编辑器 **0**（两次都过） |

**判据来源**：① `资料/普查产出_1013/A表现核_块2.md` §A503（现读行号）· ② `资料/普查产出_1012/H44_Deck一族三笔.md` §六·1/2/3。
**没走原版反编译**：原版卡组存在**服务器**上（`CardDeck.syncedToServer` / `deckId`，见 `DeckLibrary.cs` 文件头自己写着
「⚠️ 和原版不一样的地方」）⇒ **本地这条链没有原版对应物**，本笔的判据是 **本项目红线「⛔ 不许静默失败」** + `DeckLibrary`
侧**已经补好的出口**（A398：`Save()` 写 `LastError`、`SaveOrWarn` 出声、`Rename`/`Delete` 是 `return Save();`）。
⛔ 没有新造一套出声机制 —— 用的就是 A398 那一条（`LastError` + `Debug.LogWarning`）。

---

## 二、逐处改动清单

### `Shell/CollectionData.cs`（行号 = **改完之后**的现读值）

| 行 | 改前 | 改后 | 为什么 |
|---|---|---|---|
| `:14` | （无） | 新增 `using UnityEngine;` | 出声要用 `Debug.LogWarning`（同族 `DeckLibrary.SaveOrWarn` 就是这条通道） |
| `:99-108` | `/// <summary>删一套卡组。**只剩一套时不许删**（`DeckLibrary.Delete` 的规矩）。返回删没删成。</summary>` | 文档**就地订正** + 新增 `public static string LastDeleteError { get; private set; }` | ③ 文档与实现错位（§三）；`LastDeleteError` = 两种失败原因的**唯一出口**，也是调用点拆文案要用的钩子 |
| `:119-130` | `public static bool DeleteDeck(int i) { bool ok = Lib.Delete(i); if (ok) Lib.Save(); return ok; }` | 见下 | ② 两种 `false` 分开报 + 删掉多余的第二趟 `Save()`（它把 `Delete` 自己的返回值也吞了） |
| `:135-147` | `DuplicateDeck`：`Lib.Duplicate(i)` → `if (d == null) return ""` → **`Lib.Save();`** → `return d.Name;` | `Lib.Duplicate(i)` → `d == null ⇒ ""` → **`if (Lib.LastError != null)` ⇒ 警告 + `""`** → `return d.Name;` | ① 读返回值/`LastError`；删掉多余的 `Save()`（`Duplicate` 内部是 `SaveOrWarn`） |
| `:152-171` | `ImportDeck`：`Lib.Add(deck); Lib.Save(); return deck.Name;` | `Lib.Add(deck)` → **`if (Lib.LastError != null)` ⇒ `why = "卡组串读出来了，但**没写进存档**：" + 原因` + 警告 + `""`** → `return deck.Name;` | ① 同上；`why` 是**已有**的那个人话通道（`ImportDeckPopup` 直接打在错误行上） |
| `:177-192` | `CreateDeck`：`Lib.Create(...); Lib.Save(); return d != null ? d.Name : name;` | `Lib.Create(...)` → **`if (Lib.LastError != null)` ⇒ 警告 + `""`** → `return got;` | ① 同上；返回契约补写进文档 |
| `:194-201` | （无） | 新增 `static string SaveFailReason()` | **四个口共用这一句**文案（「两处写同一条规则 = 迟早不一致」）；兜底措辞与 `DeckRuntime.SaveAndSay` 的「写不进存档文件」一致 |

**`DeleteDeck` 改后（全文）**：

```csharp
public static bool DeleteDeck(int i)
{
    LastDeleteError = "";
    int before = Lib.Decks.Count;
    bool ok = Lib.Delete(i);          // ⛔ 别在后面再加一次 `Lib.Save()`（A398 起它自己 `return Save();`）
    if (ok) return true;
    LastDeleteError = Lib.Decks.Count < before
        ? "删了，但**没写进存档**：" + SaveFailReason() + "（内存里已经少了这一套、盘上还在 ⇒ 重启它又回来）"
        : "**没删** —— 下标 " + i + " 越界（库里现在 " + before + " 套）";
    Debug.LogWarning("[CollectionData] 删卡组失败：" + LastDeleteError);
    return false;
}
```

🔴 **判据为什么是「`Decks.Count` 变没变」而不是「`LastError` 空不空」**：`DeckLibrary.Delete` 的**越界那条是早退**
（`if (index < 0 || index >= _decks.Count) return false;`），而它**不清 `LastError`** ⇒ 上一次失败留下的旧值会挂在那儿，
只看 `LastError` 会把「越界」**误判成「没落盘」**。这是个会**静默报错原因**的坑，写在代码注释里了。

**`Lib.Save()` 三处删除的依据**：`RuleEngine/Data/DeckLibrary.cs` 的 `Create:81` / `Duplicate:113` / `Add:297` 内部就是
`SaveOrWarn(...)`（A398 加的），`Delete:129` 是 `return Save()` ⇒ Shell 侧再调一次是**同一份存档写两趟**。

### `Editor/CollectionScene.cs`

| 行 | 改了什么 |
|---|---|
| `:5075-5151` | **新增一整节** `Section("A503：…")`，落在 `Run()` **最末尾**（紧挨 `int total = _pass + _fail;` 之前）。14 条断言，见 §四 |

---

## 三、`:99` 文档与实现错位 —— 选了哪条路、为什么

**原文**（改前 `:99`）：`/// <summary>删一套卡组。**只剩一套时不许删**（\`DeckLibrary.Delete\` 的规矩）。返回删没删成。</summary>`

**事实**：`RuleEngine/Data/DeckLibrary.cs` 的 `Delete` **没有这条规矩** —— 它只判 `index` 越界，删到 0 套也照删
（`if (_decks.Count == 0) _current = -1;`）。「只剩一套时不许删」真正的落点是 **UI 侧**：
`Shell/DeckInfoPopup.DeleteInteractable`（`:397` / `:580` `DeleteInteractable = n > 1;`，注释自己标着
「照原版 `DeckInfoControls__Initialize` 的 **`1 < iVar1`**」），点击那一侧还有 `if (Blocked("Delete", DeleteInteractable)) return;`（`:1258`）。

**选择：改注释（不补实现）**。三条理由：

1. **那条规矩本来就不属于数据层**：它是**按钮的 `interactable`**（原版 `deleteButton.interactable = 1 < 卡组数`）——
   补进 `CollectionData` 就是**两处写同一条规则**（CLAUDE.md §三 明令禁止），而且会把「库里能不能删到 0 套」这件事改掉。
2. **实现是「对」的那一半**：写文档的人**把 UI 的规矩记到了数据层头上**（H44 §六·3 已经这么判过），错的是文字不是代码。
3. 铁律 11 的例外不适用（不是「原版没有」，也不是「用户拍板不做」）—— 这是**文档订正**，不是砍功能。

改后文档写清了「本层会拦的只有**下标越界**；那条规矩在 UI 侧」，并**保留了订正痕迹**（「原来写 X，实际是 Y」+ 出处），
照铁律 5 的格式。

---

## 四、断言清单（断什么 · 怎么分辨两种状态 · 改坏法 · 落点）

**落点**：`Editor/CollectionScene.cs` 的 `Run()` 末尾（`:5075-5151`，**在 `int total = _pass + _fail;` 之前**），宿主 = **`CollectionScene.Run`**。
**探针**：`DeckStore.OverridePath` 拐到 `…/collection/__wf_a503_no_such_dir__/x.json`（父目录不存在 ⇒ `File.WriteAllText` 抛
`DirectoryNotFoundException` ⇒ `DeckStore.SaveAll` 回 false）。**收尾**把路径还回夹具那条 + `CollectionData.ResetForTest()`。

| # | 断什么（简写） | 怎么分辨两种状态 | 改坏法 ⇒ 红 |
|---|---|---|---|
| 1 | （前提）探针目录存在 | —— | `ShotDir` 没建 ⇒ 后面全是假绿（这条自己先红） |
| 2 | （控制组）好路径 `CreateDeck` 回**名字** | **好路径 vs 坏路径**两个状态在同一节里各断一次 | 实现改成「恒返空串」⇒ 红 |
| 3 | （控制组）`DuplicateDeck` 回名字 | 同上 | 同上 |
| 4 | （控制组）`ImportDeck` 回名字**且 `why` 空** | 成功时 `why` 必须空 | 把成功也写成 `why=…` ⇒ 红 |
| 5 | （控制组）`DeleteDeck` 回 **true** 且 `LastDeleteError` 是**空串** | 「成功不报错」 | 实现改成「恒报失败」/ 成功也写错误 ⇒ 红 |
| 6 | （前提）坏路径**确实写不进去** | —— | 那条路径其实写得进去（例如目录被谁建了）⇒ 这条红，**挡住下面全部假绿** |
| 7 | （前提）坏路径下起手是**空库** | —— | 读得到东西 ⇒ 红 |
| 8 | ★ 写盘失败 ⇒ `CreateDeck` 回**空串** | **回名字 vs 回空串** | 把 `CreateDeck` 里 `if (Lib.LastError != null)` 支整段删掉、照旧 `return got;` ⇒ 红 |
| 9 | ★ ……而它**真的建在内存里**了（`DeckCount() == 1`） | 钉住语义：「失败」专指**没落盘**（内存建了、盘上没有） | 把失败做成「回滚删除内存里那套」⇒ 红（**这条是本节的语义锚**） |
| 10 | ★ 写盘失败 ⇒ `DuplicateDeck` 回**空串** | 同上 | 删掉它那个 `Lib.LastError` 支 ⇒ 红 |
| 11 | ★ 写盘失败 ⇒ `ImportDeck` 回**空串** | 同上 | 删掉它那个 `Lib.LastError` 支 ⇒ 红 |
| 12 | ★ ……且 `why` 是**第三种人话**（既不是「先粘贴卡组串」也不是「这不是一条合法的卡组串」） | 串本身是好的 ⇒ 报的必须是**没写进存档** | 把 `why = "卡组串读出来了…"` 那行删掉 ⇒ 红 |
| 13 | ★ **两种 `false` 分开报**：`e1 != e2` 且两句都非空（①「没删」· ②「删了没落盘」） | **把两句合成一句**就红 —— 那**正是原来的缺陷** | `DeleteDeck` 里那两支合并成一句「删不了」⇒ 红 |
| 14 | ★ ……而且各自点明是**哪一种**（① 含「没删」· ② 含「没写进存档」）+ 两种状态下 `Decks.Count` 一个**没少**、一个**少了一套** | 状态对 + 文案对，两条一起才咬得住 | 两支互换文案 ⇒ 红 |

**为什么不算自证 / 为什么不是弱断言**：
- **探针是环境造的**（不存在的目录），**不是我们的常量**；期望值来自**契约**（写盘失败 ⇒ 按既有 `string` 契约回空串），
  而不是抄实现的输出 —— 实现改了、这条就红。
- **控制组与探针成对**（#2–5 vs #8–12）：「恒报失败」和「恒报成功」两种坏实现**各有对应的一条能红**
  —— 只断失败那一半会被「恒返空串」的实现蒙过。
- **灭自证那一族**：想「两边一起改回去」也不成立 —— 坏实现（不读 `LastError`）⇒ 控制组的 5 条照绿、探针那 5 条全红；
  把 `DeckStore.SaveAll` 弄成永远失败 ⇒ 控制组全红。

**⚠️ 这一节的三条静态核对**（我跑不了 Unity，只能静态核）：
1. `Run()` 开头（`:760`）把 `OverridePath` 指到 `TestDeckFile`（`d:/4/_tmp_view/collection/_test_decks.json`），
   全文件**只有**那处和本节动它 ⇒ `keepPath` = 夹具那条，收尾还原正确。
2. 本节这一段**没有别人依赖的后续**：它后面只剩 `int total = …` 与退出码 ⇒ 「往库内存里多塞几套 + 拐坏路径」不影响别的断言。
3. `CollectionData.Raw(0)` 在坏路径下仍非空（内存里那套），`DeckLibrary.ExportString(...)` 是**纯静态**、不碰盘
   ⇒ 探针里造导入串**不能**用 `DeckLibrary.Load().Decks[0]`（坏路径下那是空库、会 `IndexOutOfRange`）—— 已在注释里写死。

📌 **给收口那次的读数提醒**：本批「A 表清零前不跑自检」，所以**下一条 `CollectionScene.Run` 的断言合计会 +19**
（全过时 = `_pass` 涨 19）。若合计只涨了一部分 ⇒ 涨的那部分就是我这一节里**过的那几条**，红的那几条按上表逐条看。
另外：`collection.log` 里会多出 **4 条 `[CollectionData]` 警告**（探针**故意**造的：新建 / 复制 / 导入 / 删卡组 各一条，
每条都写着「没写进存档」）—— 那是本节的预期输出，**不是**自检红。

---

## 五、没查清 / 没做的（⛔ 不猜、不静默）

1. 🔴 **这一节的断言【没跑过】** —— 本批口径是「Unity 批处理全局串行、只有主对话能跑」（⛔ 我一条都没跑）。
   它只过了**类型检查**。收口那次 `CollectionScene.Run` 若红，**两条最可能**：
   ① `（前提）这条路径确实写不进去` —— 只可能因为 `…/__wf_a503_no_such_dir__` 那个目录**被谁建出来了**（先查它）；
   ② `（前提）坏路径下起手是空库` —— 只可能因为 `OverridePath` 没生效（先查 `keepPath` 打出来是什么）。
   其余 12 条都是纯内存断言。**①③（控制组）在好路径上跑，不依赖探针**，若它们红了就是 `CollectionData` 真的坏了。
2. **只测了一种失败条件**（父目录不存在 ⇒ `DirectoryNotFoundException`）：磁盘满 / 权限 / 只读归档**没测**
   （不确定、不适合进自检）—— `DeckStore.SaveAll` 对它们走**同一条 `catch`**（这一句是照 A398 报告 §五·4 的口径，不是新结论）。
3. **「内存改了、盘上没改」我【没有】做回滚**（不做二次修正）：A398 在数据层已经定过语义
   （`SaveOrWarn` 的警告原文写着「内存改动**已生效**，但**落盘失败**」）⇒ 我**不自己发明**一套「失败就撤掉内存改动」的语义。
   **如实标注**：所以现在写盘失败之后，**内存与盘是不一致的**（列表里看得见、重启就没了）—— 危险点靠**出声**顶住：
   四个口各一条 `Debug.LogWarning`（措辞都写着「重启就没了 / 重启它又回来」）。**要不要做回滚 = 另一笔账，得调度台/用户裁**。
4. **`LastDeleteError` 是我新加的一个 public 只读属性**（：`108`）。它不是「新造一套出声机制」（原因还是 `LastError` + 警告），
   而是**那句话的唯一拼装处** + 调用点拆文案要用的钩子（见 §六·2）；如果调度台不要它，删掉三行即可（两次赋值 + 属性），
   本节断言 #13/#14 随之要改成别的形状（那两条正是咬它的）。
5. **`DeleteDeck` 的返回值含义我保持原样**（= 内存删掉**且**落盘成功）—— 没有为了「内存删了」改成回 true。
   理由：`DeckLibrary.Delete` 就是 `return Save()`（A398 定死），Shell 侧与它**同义**才不会出现两套口径；
   代价是调用点会看到 `false`（它今天本来就是 `false`，**行为没变**，见 §六·2 那条）。

---

## 六、顺手发现（⛔ 本件只报不改 —— 都在白名单外）

按「同一个缺陷的下游还有谁」逐条列，**每条都带落点**，调度台可以直接派：

1. 🔴 **`Deck/DeckRuntime.cs:2465-2483`（`TryImport`）是同族的【编辑窗那一半】，同样静默** ——
   `Library.Add(deck)`（内部 `SaveOrWarn`，会警告）之后**不看落盘结果**，直接
   `Say("已导入「" + deck.Name + "」")` + `DeckDirty = false`。写盘失败时页脚说的是「已导入」，而**脏标记被清掉了**
   （下一次 Done 不会再试）⇒ 玩家关掉编辑器就永久丢。**它正好是我在 `CollectionData.ImportDeck` 文档里写的
   「与卡组编辑那边逐字一致」的那一半** ⇒ 两边现在**不一致**（我这半边会报失败，它那半边不会）。
   ⚠️ 该文件是 **W-D1 的**（本批在飞），⛔ 我没碰。判据与 A503 同一条（`Library.LastError`）。
2. **`Shell/DeckInfoPopup.cs` 三处**（都是文案/注释，本件白名单外）：
   - `:1260` `else Debug.Log("[DeckInfo] 删不了（\`DeckLibrary.Delete\` 的规矩：只剩一套时不许删 / 下标越界）")`
     —— **现在这句话是唯一还在「混着说」的地方**，而且它引用的那条规矩**不存在**（§三）。
     改法一行：`Debug.Log("[DeckInfo] 删卡组失败：" + CollectionData.LastDeleteError)`（`LastDeleteError` 直接可用）。
   - `:1269` `else Debug.Log("[DeckInfo] 复制失败（\`DeckLibrary.Duplicate\` 返回空）")` —— 现在这句在**两种**情况下都会打
     （下标越界 / 写盘失败），后半句把原因归错了。
   - `:396` 的文档「⇒ 只剩一套时不给你删（**`CollectionData.DeleteDeck` 本来也拒**，这条把它摆到按钮这一层）」
     —— **前半句对（`:580 DeleteInteractable = n > 1`），后半句错**（`DeleteDeck` 从来没拒过「只剩一套」）。
3. **`Shell/CollectionWindow.cs:2694-2701`（`CreateDeck`）**：拿到空串时仍然 `RebuildDeckCells` + `Debug.Log("新建卡组「" + name + "」")`
   ⇒ 失败时日志会打印「新建卡组「」」。**功能上没坏**（那一套**确实**在内存里，列表也该重建），只有日志那半句要看一眼。
4. **`Shell/LiveOpsEventWindow.cs:962-975`（`CreateDeckInMode`）**：拿到空串 ⇒ `IndexOf("")` = −1 ⇒
   `Select(-1)`（有 `index >= 0` 守卫，无操作）+ `PendingEditDeck = -1` + 进编辑器。**我静态核过，这是良性的**：
   `DeckRuntime` 开局 `if (CollectionData.PendingEditDeck >= 0)`（`:461`）⇒ 不吃交接，落到 `Library.Current`
   —— 而 `DeckLibrary.Create` 已经把 `_current` 设成刚建那套 ⇒ **还是同一副**。⇒ 只是「交接」这一跳没了，**行为等价**。
   （⛔ 若将来有人改 `DeckLibrary.Create` 的 `_current` 行为，这一条就不再良性的 —— 记在这儿。）
5. **全仓这四个口的调用点只有 5 处**（本笔影响面就这些，一条不漏地列出来）：`CreateDeck` = `Shell/CollectionWindow.cs:2696`
   + `Shell/LiveOpsEventWindow.cs:965`；`DuplicateDeck` = `Shell/DeckInfoPopup.cs:1267`；`ImportDeck` = `Shell/ImportDeckPopup.cs:205`；
   `DeleteDeck` = `Shell/DeckInfoPopup.cs:1259`；`Select`（同文件、本笔没改）= 收藏窗 / `DeckInfoPopup` / 练习窗 / 模式窗。

---

## 七、类型检查结果

```
$ TMPDIR=/tmp/wf_we4 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

跑了**两次**（改完 `Shell/CollectionData.cs` 一次；再把断言加进 `Editor/CollectionScene.cs` 之后一次），**两次都是 0/0**，
**没有**出现「错误集中在别人文件上」的情况。

**`git diff --numstat`**（改完立刻看，防行尾被翻）：

```
78  14  Unity/MyGame/Assets/CardPresentation/Shell/CollectionData.cs
78   0  Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs
```
