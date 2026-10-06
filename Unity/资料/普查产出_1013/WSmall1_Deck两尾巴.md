# WSmall1 · Deck 两尾巴（A547 + A548）

> 白名单内动过 **3 个文件**：`Deck/DeckRuntime.cs`（A547 本体）· `Shell/DeckInfoPopup.cs`（A548 三处）·
> `Editor/DeckScene.cs`（A547 的断言，落在 `TestLayout()` **最末尾**）。
> **没跑 Unity、没动 git、没改两张正本。** 类型检查跑 **1 次**（三处都改完之后）⇒ **0/0**（§七）。
> 行尾：`DeckRuntime.cs` = **纯 CRLF**（改前 `4497/4497` → 改后 `4529/4529`）· `DeckScene.cs` = **纯 CRLF**
> （`3940/3940` → `4020/4020`）· `DeckInfoPopup.cs` = **纯 LF**（`0/1477` → `0/1500`）⇒ **一个都没翻**（§七）。
>
> 🔴 **先说一条要调度台过目的（同时飞的两支笔）**：我 10:13 开工时，`Deck/DeckRuntime.cs`（mtime 10:06:46）
> 与 `Editor/DeckScene.cs`（10:08:56）**已经有 W-D1/W-D1b 的未提交改动**（`WD1b_编辑副本隔离.md` 10:11:31 落的盘
> ⇒ 看起来已经收手）。它们**也在我的白名单里**，所以我按简报做，但**只用 Edit（精确串匹配）**、**没有任何整篇写**，
> 且**刻意避开他们的落点**：`DeckScene.cs` 的 A397 段（`:3168-3265`）**一个字没动**、`SaveAndSay` 里 A502 那一级
> **没动**（我只把同一函数里**那半句兜底措辞**换成了新 helper，见 §二末）。
> 复核方式（可当场重跑）：`git diff --numstat` = `114/19`（= W-D1b 的 `78/15` + 我的 `36/4`）与
> `339/15`（= `259/15` + 我的 `80/0`）⇒ 两边都在。若调度台仍认为那两个文件归 W-D1 ⇒ 我的改动是**自包含的两块**
> （`TryImport` 那一段 / `TestLayout` 末尾那 80 行），搬走即可，⛔ **别两处各留一份**。

---

## 一、结论

| 验收条目 | 做没做 | 一句话 |
|---|---|---|
| A547 照 A503 口径读**已有出口** + 失败出声 + **不许清脏标记** | ✅ | `TryImport` 现在**只读一次** `Library.LastError`（与 `CollectionData.ImportDeck` **同一处**出口）⇒ 写盘失败时 `Say("导入失败：卡组串读出来了，但**没写进存档**——<原因>")` + **`DeckDirty` 留着**（下次 `Done` 再试）+ 回 `false` |
| A548 三处（`:1260` / `:1269` / `:396`） | ✅ | 删失败**直接打** `CollectionData.LastDeleteError`（它自己分两种）· 复制失败按**调用前后 `DeckCount()` 变没变**分两支 · `:396` 那句「`CollectionData.DeleteDeck` 本来也拒」**就地订正**（它从没拒过） |
| 断言：分辨「成功」/「写盘失败」· 有改坏法 · 不自证 | ✅（**未跑**） | `Editor/DeckScene.cs:3786-3864` 新增一节 **16 条**：**控制组**（好路径）+ **探针**（写不进去的路径）+ **出声**（`Application.logMessageReceived` 抓的是**真原因**）。⚠️ **一条都没跑**（Unity 全局串行、只有主对话能跑）⇒ §五·1 |
| `TMPDIR=/tmp/wf_wsmall1 typecheck` **0 错** | ✅ | 运行时 `0` / 编辑器 `0`，**没有**「错误集中在别人文件上」的情况 |

**判据来源**：① `资料/普查产出_1013/WE4_卡组库静默失败.md` §六·1（A547）与 §六·2（A548 三处）·
② **`Shell/CollectionData.cs` 现读**（W-E4 刚改好的成品 = 照抄的范本）· ③ `RuleEngine/Data/DeckLibrary.cs`
的 `Save():155` / `SaveOrWarn():180` / `LastError:38`（A398 补好的出口）。
**没走原版反编译**：原版卡组存在**服务器**上（`CardDeck.syncedToServer`），本地这条链**没有原版对应物**
⇒ 判据是本项目红线「⛔ 不许静默失败」+ **A503 定下的那套口径**（与 WE4 同一条，⛔ 没另立一套）。

---

## 二、A547 —— `Deck/DeckRuntime.cs` 的 `TryImport`

**落点（改完后现读行号）**：文档 `:2481-2495` · `Library.Add` `:2506` · 结果读取 `:2511` · `DeckDirty` `:2514` ·
失败支 `:2516-2521` · 新 helper `SaveFailReason()` `:2257-2264`。

| 行 | 改前 | 改后 |
|---|---|---|
| `:2506` | `Library.Add(deck);   // …（`Library.Add` → `Save()`）` | 同左，注释订正成 **`SaveOrWarn`**（A398 起它才既写 `LastError` 又出声 —— 这句注释原来把出口说漏了） |
| `:2511` | （无） | `bool persisted = Library.LastError == null;` ← **判据只在这里读一次** |
| `:2514` | `DeckDirty = false;` | `DeckDirty = !persisted;` ← **写盘失败就留着**（= 「下次 `Done` 再写一次」的开关） |
| `:2516-2521` | （无） | `if (!persisted) { Say("导入失败：卡组串读出来了，但**没写进存档**——" + SaveFailReason() + "（重启就没了）"); return false; }` |
| `:2523` | `Say("已导入「…」…")`（无条件） | **只有成功那一支**才走到它（一字未改） |

**照的是 `CollectionData.ImportDeck`（`Shell/CollectionData.cs:152-167`）的哪一段**：

```csharp
Lib.Add(deck);                     // ⛔ 别在后面再加一次 `Lib.Save()`（A398 起它自己 `SaveOrWarn`）
if (Lib.LastError != null)
{
    why = "卡组串读出来了，但**没写进存档**：" + SaveFailReason();
    Debug.LogWarning("[CollectionData] 导入卡组「" + deck.Name + "」：" + why + "（重启就没了）");
    return "";                     // ← 「没成」比「已导入」诚实
}
```
⇒ 我这一半**逐条对齐**：同一次 `Add`、**同一个出口** `LastError`、**同一句人话**（「卡组串读出来了，但**没写进存档**」）、
**同样不回滚内存**；差别只在**出口通道**（Shell 侧是 `why` + `LogWarning`，编辑窗侧是**页脚 `Say`**，而 `Say`
内部本来就 `Debug.Log("[Deck] …")` —— 那条是本窗既有的出声通道，`SaveAndSay` 的「保存失败」用的就是它）。

⚠️ **如实交代三处与范本不同**（都不是「另立一套」，但要让调度台看见）：
1. **`SaveFailReason()` 在 `DeckRuntime` 里补了一个同名的【实例】helper（`:2261`）** —— 范本那份是 `CollectionData`
   的 `private static`，跨类拿不到。兜底措辞**逐字相同**（`"写不进存档文件"`）。
   **并且顺手把同文件里 `SaveAndSay` 那半句一模一样的表达式（原 `:2250`）改成调它** —— 否则**同一个文件里
   同一条兜底措辞会写两遍**（正是铁律「两处写同一条规则 = 迟早不一致」）。**行为等价、净 0 行变化**；
   ⛔ 若调度台认为这半句越界，**删掉它不影响 A547 的任何断言**（只影响这一处表达式的复用）。
2. **返回 `false` 而不是空串**：`TryImport` 的契约是 `bool`（`Shell/CollectionData.ImportDeck` 是 `string`）
   ⇒ 按本窗既有的 `bool` 契约落。⚠️ 于是 `false` 现在有**三种**成因（串空 / 串不合法 / **没落盘**），
   前两种仍由 `ImportError` 承担（`:2480`），第三种走页脚 + 日志 —— 见 §六·2。
3. **没有把失败写进 `_importError`**：那一刻导入弹窗已经被 `CloseImport()` 关掉了（它不是错误行该出现的地方），
   ⛔ 我不改玩家可见流程（见 §五·4）。

**调用点/影响面（全仓 `TryImport` 只有这几处，一条不漏）**：`DeckRuntime.cs:2201`（`case "imp_ok": TryImport();`，
**丢弃返回值** ⇒ 无影响）· `DeckRuntime.cs:2917`（`UiTryImport()`，自检用）· `Editor/DeckScene.cs` 里 3 处
（`:3291/:3296` 既有两条 + 我新增一节）。`Shell/ImportDeckPopup.cs:200-216` 走的是
`CollectionData.ImportDeck`（**不是** `TryImport`），它**本来就**按空串出声 ⇒ 那边早已正确。

---

## 三、A548 —— `Shell/DeckInfoPopup.cs` 三处（现读行号）

| # | 行 | 改前 | 改后 | 判据 |
|---|---|---|---|---|
| ① | `:398-403`（`DeleteInteractable` 的文档，原 `:396`） | `/// ⇒ 只剩一套时不给你删（`CollectionData.DeleteDeck` 本来也拒，这条把它摆到按钮这一层）。` | 保留前半句 + **就地订正**（铁律 5 的格式：写清「原来写 X、实际是 Y、⛔ 别当成数据层也拦着」） | 事实：`DeckLibrary.Delete:117-119` 只判**下标越界**；`CollectionData.DeleteDeck` 从没拒过「只剩一套」；那条规矩的真身在**本层**（`:580` 照原版 `1 < iVar1`） |
| ② | `:1272`（原 `:1260`） | `else Debug.Log("[DeckInfo] 删不了（`DeckLibrary.Delete` 的规矩：只剩一套时不许删 / 下标越界）");` | `else Debug.Log("[DeckInfo] 删卡组失败：" + CollectionData.LastDeleteError);` | **A503 的那条唯一出口**（WE4 §六·2 点名「改法一行」）：它自己会分成「**没删**（下标越界）」与「**删了，但没写进存档**」两句；⛔ 这里不再判一遍 |
| ③ | `:1279 / :1290-1292`（原 `:1269`） | `else Debug.Log("[DeckInfo] 复制失败（`DeckLibrary.Duplicate` 返回空）");` | `int dupBefore = CollectionData.DeckCount();` 前置一次，失败时按 **`DeckCount() > dupBefore`** 分两支：「复制出来了，但**没写进存档**（原因见上面那条 `[CollectionData]` 警告）」/「下标 N 越界，**没复制**」 | **与 `CollectionData.DeleteDeck` 分开它那两种 `false` 用的是同一条判据**（调用前后 `Decks.Count` 变没变，见 `CollectionData.cs:122-127`）⇒ 不是我新造的判据；⚠️ **`CollectionData.Lib` 是私有的**，调用点拿不到 `LastError`（见 §五·5） |

---

## 四、断言清单（落点 `Editor/DeckScene.cs:3786-3864`，宿主 = **`DeckScene.Run`** 的 `TestLayout()` 末尾）

**探针**：`DeckStore.OverridePath` 拐到 `<project>/Temp/__wf_a547_no_such_dir__/x.json`（父目录不存在 ⇒
`File.WriteAllText` 抛 `DirectoryNotFoundException` ⇒ `DeckStore.SaveAll` 回 false，`error` 是
`"存档写入失败：" + e.Message` ⇒ **恒非空**）。**控制组**用同目录下的 `_a547_probe.json`（写得进去）。
**导入串走生产那条路**（`_rt.UiShareString()` = `DeckLibrary.ExportString(State.Deck)`，⛔ 不手拼格式）。
**收尾**：路径还回夹具那条 + 删探针文件 + `_rt.CommitDeck()` 把脏标记与 `LastError` 收回 0。

| # | 断什么（简写） | 怎么分辨两种状态 | 改坏法 ⇒ 红 |
|---|---|---|---|
| 1 | （前提）探针目录存在（`Temp/`） | —— | 目录没建 ⇒ 下面全是假绿（这条自己先红） |
| 2 | （前提）导出的卡组串非空 | —— | 串漂了 ⇒ 后面会**静默变成「串不合法」**（这条挡住它） |
| 3 | （控制组）好路径 `UiTryImport()` 回 **true** | **好路径 vs 坏路径**两个状态在同一节里各断一次 | 实现改成「恒报失败/恒回 false」⇒ 红 |
| 4 | （控制组）`Library.Count` **+1** | 同上 | 恒空转 ⇒ 红 |
| 5 | （控制组）`LastError == null` | 「成功不报错」 | 成功也写错误 ⇒ 红 |
| 6 | （控制组）`!DeckDirty` | 「成功不脏」 | **没有这条，一个「恒报失败 + 脏标记恒留着」的实现照样绿** |
| 7 | （前提）坏路径**确实写不进去** | —— | 那条路径其实写得进去（目录被谁建了）⇒ 这条红，**挡住下面全部假绿** |
| 8 | （前提）失败**带了原因**（`LastError` 非空） | —— | 原因空 ⇒ 第 15 条会**空串恒真**（假绿）⇒ 这条先钉住 |
| 9 | （前提）起手 `DeckDirty` 干净 | —— | 起手就脏 ⇒ 第 12 条是假绿（第 6 条已把它兜住） |
| 10 | ★ 写盘失败 ⇒ 回 **false** | **回 true vs 回 false** | 删掉 `if (!persisted)` 支、退回无条件 `return true` ⇒ 红 |
| 11 | ★ ……而那套**真的在内存里**了（`Count == p0+1`） | 钉住语义：「失败」专指**没落盘**（内存建了、盘上没有） | 把失败做成「回滚删除内存里那套」⇒ 红（**本节的语义锚**，与 A503 那一节同形） |
| 12 | ★ ……而且**脏标记留着**（`DeckDirty == true`） | **清 vs 留** —— 那正是「关掉编辑器就永久丢」的成因 | 退回 `DeckDirty = false` ⇒ **这条红** |
| 13 | ★ ……而且**出声**（日志里有「导入失败」） | 有 vs 没有 | 删掉失败支的 `Say` ⇒ 红（⛔ 静默失败 = 红线） |
| 14 | ★ ……⛔ **不许**再说「已导入」 | 两句互斥 | 退回「照样说已导入」⇒ 红（全仓**只有** `DeckRuntime.cs:2523` 一个「已导入」字面量 ⇒ 这条不会误报） |
| 15 | ★ ……而且报的是**真原因**（日志里出现 `Library.LastError` 的**原话**） | 「随便说一句」vs「把原因说出来」 | 把原因换成写死的文案 ⇒ 红 |
| 16 | （收尾）路径恢复后 `CommitDeck()` 落得下去（`!DeckDirty && LastError == null`） | —— | 收尾失败 ⇒ 页头（A330 印 `LastError`）会污染后面那张 `deck_editor.png` 截图 |

**为什么不算自证 / 不是弱断言**：
- **探针是环境造的**（不存在的目录），**期望值来自契约**（写盘失败 ⇒ `bool` 契约回 false、脏标记留着），
  不是抄实现的输出 ⇒ 实现改了就红。
- **控制组与探针成对**（#3–6 vs #10–12）：「恒报失败」和「恒报成功」两种坏实现**各有对应的一条能红**
  —— 只断失败那一半会被「恒返 false」的实现蒙过。
- **出声那三条（#13–15）不是同义反复**：#15 拿的是 `DeckLibrary.LastError` 的**运行时真值**（环境生成的
  `DirectoryNotFoundException` 文本），不是我们源码里的常量。

---

## 五、没查清 / 没做的（⛔ 不猜、不静默）

1. 🔴 **这一节的断言【没跑过】** —— 本批口径是「Unity 批处理全局串行、只有主对话能跑」。它只过了**类型检查**。
   收口那次 `DeckScene.Run` 若红，**两条最可能**：① 「（前提）这条路径确实写不进去」—— 只可能因为
   `…/Temp/__wf_a547_no_such_dir__` **被谁建出来了**；② 「（控制组）」那四条 —— 它们跑在**好路径**上、
   **不依赖探针**，红了就是 `CollectionData`/`DeckRuntime` 真的坏了。
   本节**预期让断言合计 +16**，另外 `deck.log` 里会多出**故意造的**失败日志
   （`[DeckLibrary] Add …落盘失败` + `[Deck] 导入失败：…`）—— 那是预期输出，**不是**自检红。
   ⚠️ **第三种可能**（我节里两条「（前提）」挡不住的那种）：`impStr` 非空但**没被 `ImportString` 认**（例如这轮编辑
   过的这副牌导出后解析不了）⇒ `★` 那几条会**一起红**（#10/#11/#12 全挂在「真的走到落盘」这一步上）。
   判据：看**第 3 条（控制组）有没有红** —— 控制组也红 ⇒ 就是串的问题，不是 A547 的实现问题。
2. **只测了一种失败条件**（父目录不存在）：磁盘满 / 权限 / 只读归档**没测**（不确定、不适合进自检）——
   `DeckStore.SaveAll` 对它们走**同一条 `catch`**（照 A398 报告 §五·4 的口径，不是新结论）。
3. **「内存改了、盘上没改」不做回滚**（与 A503 一致）：A398 在数据层已经定过语义（`SaveOrWarn` 的警告原文写着
   「内存改动**已生效**、**落盘失败**」）⇒ ⛔ 我不自己发明一套「失败就撤内存」。**要回滚 = 另一笔账、得调度台/用户裁**
   （⚠️ 那会推翻我 #11 那条断言 —— 我按 A503 的形状写是为了两边一致）。
4. **失败时没有让导入弹窗留在屏幕上**（只出声到页脚 + 日志）：那一刻弹窗已被 `CloseImport()` 收起，
   `_importError` 那条错误行**是空的**。⛔ 要不要改成「留窗报错」= 改玩家可见流程，**本件不做、也不自行发明**。
5. **A548 的 `:1269` 只能用 `DeckCount()` 分**（`CollectionData.Lib` 私有、`DuplicateDeck` 只有「名字 / 空串」这一个出口）。
   更干净的做法是在 `CollectionData` 加一个 `LastDuplicateError`（照 `LastDeleteError` 的样子）——
   ⛔ **那个文件不在我的白名单**（W-E4 的），所以**没动**；⛔ 我也**没有**去改 `Lib` 的可见性。
6. **A548 三处【没有断言】**：`Shell/DeckInfoPopup.cs` 的自检宿主是 `Editor/CollectionScene.cs`（⛔ 白名单外），
   我这一侧只有 `Editor/DeckScene.cs`。⇒ 这三处目前**只有类型检查兜着**；要不要补（以及补在哪）**请调度台派**。
   （我查过：本仓有现成的「出声」断言范式 —— `Application.logMessageReceived`，先例
   `Editor/CollectionScene.cs:2111-2135` 的 A229 那段、`Editor/BattleScene.cs:210` —— 所以**是可断的**，只是不在我的白名单。）

---

## 六、顺手发现（⛔ 本件只报不改 —— 全都越了白名单）

1. 🔴 **同一族的第三条尾巴：`CollectionData.Select` 的落盘结果【没人看】。**
   `Shell/CollectionData.cs:69-78` = `Lib.Select(i); Lib.Save();` —— 返回值与 `LastError` **都不读**，
   而**七个**调用点紧接着就打「已选中」：最典型 `Shell/DeckInfoPopup.cs:1183-1184`
   （`CollectionData.Select(DeckIndex); Debug.Log("[DeckInfo] 已选中「…」")`），另有
   `Shell/CollectionWindow.cs:2610` · `Shell/PracticeModePopup.cs:1568` · `Shell/LiveOpsEventWindow.cs:937,967` ·
   `Editor/MainMenuScene.cs:4112` · `Editor/CollectionScene.cs:2542`。
   **后果**：写盘失败时「你选的那一套」**静默失效**，而 `BattleDriver.PickSavedDeck` 是**从磁盘重读**的
   （`CollectionData.cs:72-76` 自己写着这条因果）⇒ **战斗会拿磁盘上那套旧的**。判据与 A503/A547 **同一条**
   （`Lib.LastError`），修法要给 `Select` 一个出口（返回 bool 或 `LastSelectError`）—— **在 ⛔ 文件里**，⛔ 我没碰。
2. **`TryImport` 的 `false` 现在有三种成因**（串空 / 串不合法 / **没落盘**），而后两种只能靠日志与页脚区分
   （`ImportError` 只覆盖前两种）。⇒ 以后写断言别把 `!UiTryImport()` 一律说成「串被挡」。
   ⚠️ 既有那条 `Editor/DeckScene.cs:3296`（`"乱串被挡"`）走的是**串不合法**那一支，**不受本件影响**。
3. **`DeckLibrary.Load()` 每次 `new`、没有静态缓存**（`RuleEngine/Data/DeckLibrary.cs:41-52`）⇒ `_rt.Library`
   与别处 `DeckLibrary.Load()` **不是同一个实例**。这是我那节敢在**内存里塞两套**（1 控制 + 1 探针）
   而不影响 `TestLibraryWiring()`（它自己 `Load()`、且断言全是**相对计数**）的前提。写下来给后面的人：
   ⚠️ **谁把 `Load()` 改成缓存单例，我那一节的收尾假设就不再成立**（那一节会把多出来的两套带进别的断言）。
4. **`DeckStore.SaveAll` 写的是 `Path + ".tmp"` 再 `Move`**（`RuleEngine/Data/DeckStore.cs:100-120`）⇒
   探针失败时**不会**留下半个坏存档（这条是顺着 A547 的探针看的，不是缺陷，是**好的**那一半，记下来免得以后有人"修"它）。
5. `Shell/ImportDeckPopup.cs:200-216` 那一半**已经是正确的**（拿到空串 ⇒ `_error = why` + `LogWarning` + 回 false），
   A503 补完 `why` 之后它自动就对了 —— 属于**无需改动**的确认（不是新账）。

---

## 七、类型检查 + 行尾 + 改动量

```
$ TMPDIR=/tmp/wf_wsmall1 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
跑了 **1 次**（三处改完之后一起跑），**0/0**，**没有**出现「错误集中在别人文件上」的情况。

**行尾**（二进制读，`\r\n` 计数 / `\n` 计数 —— ⚠️ 文本模式读会把 `\r\n` 折成 `\n`、两个数恒相等）：

| 文件 | 改前 | 改后 | 判定 |
|---|---|---|---|
| `Deck/DeckRuntime.cs` | `4497 / 4497`（纯 CRLF） | `4529 / 4529` | ✅ 没翻 |
| `Editor/DeckScene.cs` | `3940 / 3940`（纯 CRLF） | `4020 / 4020` | ✅ 没翻 |
| `Shell/DeckInfoPopup.cs` | `0 / 1477`（纯 LF） | `0 / 1500` | ✅ 没翻 |

**`git diff --numstat`**（改完立刻看）：
```
114  19  Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs      ← 其中 W-D1b 78/15 · 我 36/4
339  15  Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs      ← 其中 W-D1b 259/15 · 我 80/0
 26   3  Unity/MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs   ← 全部是我的 A548 三处
```
数字与增量吻合（+32 / +80 / +23 行 ⇒ 不是整篇重写）。另外复核过：W-D1b 的 A397/A502/A407 那几块**仍在**
（`grep -c` A397 那句 = 1，无重复；`SaveAndSay` 的调用点与 A502 那一级**没被碰**）。
