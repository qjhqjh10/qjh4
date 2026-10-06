# WSmall3 · Select 出口（A600）+ DeckInfoPopup 断言（A601）

> **白名单内动过 5 个文件**：`Shell/CollectionData.cs`（A600 本体）· `Shell/DeckInfoPopup.cs`（A600 的第 1 个调用点）·
> `Shell/CollectionWindow.cs`（第 2 个）· `Shell/PracticeModePopup.cs`（第 3 个）· `Editor/CollectionScene.cs`（**只加我这一节断言**）。
> **没跑 Unity、没动 git、没改两张正本。** 类型检查跑 **2 次**（改完 4 个 `Shell` 一次、加完断言一次）⇒ **两次都 0/0**（§七）。
>
> **行尾**（二进制读）：5 个文件改前改后**全是纯 LF**（`\r\n` 计数恒 `0`）⇒ **一个都没翻**。
> **`git diff --numstat`**（含同批别人未提交的改动，见 §七那两条警告）：
> `Editor/CollectionScene.cs 376/0`（= W-E2 的 `+4031,60` + W-E4 的 A503 节 78 行 + **我这一节 238 行**）·
> `Shell/CollectionData.cs 118/17`（= W-E4 的 78/14 + 我 40/3）· `Shell/DeckInfoPopup.cs 35/5`（= WSmall1 的 A548 26/3 + 我 9/2）·
> `Shell/CollectionWindow.cs 7/2` · `Shell/PracticeModePopup.cs 9/1`（这两份**全是我的**）。
>
> 🔴 **先说两条要调度台过目的**：
> ① **`.cs` 文件的「唯一修改者」前提在 `Editor/CollectionScene.cs` 上不成立** —— 我落笔那一刻同文件里**已有**
>   W-E4 的 A503 节（`:5142-5211`）与 W-E2 的 A404 段（`:4031-4090`），**两份都在未提交的工作区里**。
>   我**只用 Edit（唯一串匹配）**、落点在 `Run()` **最末尾**（A503 那一节之后、`int total` 之前），与那两块**零交集**；
>   复查方式 = `git diff -U0` 的两个 hunk（`@@ -5074,0 +5135,316 @@` = W-E4 的 78 + 我的 238；`@@ -4030,0 +4031,60 @@` = W-E2 的）**一条不多一条不少**。
> ② **A600 的三处调用点改动【含一个行为设计选择】**（`Close()` 无条件照旧 / 不加 `return`）—— 见 §五·2，
>   这一条请调度台明示要不要往「失败就拦下来」走；我给的是**最小改动 + 出声**，⛔ 没自己发明玩家可见流程。

---

## 一、结论

| 验收条目 | 做没做 | 一句话 |
|---|---|---|
| A600：给 `Select` 一个出口，**照已改好的三处形状** | ✅ | `Select` 从 `void` 改成 **`bool`**，新增 **`LastSelectError`**；两种失败分开（「**没选中**（下标越界）」/「**选中了，但没写进存档**」）+ 各一条 `Debug.LogWarning` —— **逐条对齐 `DeleteDeck`**（同一处出口、同一句 `SaveFailReason()`、同一条语义锚） |
| A600：7 个调用点**该跟的跟、不能跟的写进报告** | ✅ | **3 处跟着改**（`DeckInfoPopup` / `CollectionWindow` / `PracticeModePopup`，全在白名单内）· **4 处不能改**（`LiveOpsEventWindow` ×2 = 待派 · `MainMenuScene` = W-E1 在写 · `CollectionScene:2542` = 本件只许加断言）⇒ §三 逐处表 |
| A601：断言**不自证**、**能分辨两支**、**有改坏法** | ✅（**未跑**） | `Editor/CollectionScene.cs:5213-5449` 新增两节共 **44 条**（A600 20 条 + A601 24 条）：控制组（好路径成功）+ 探针（写不进去的路径）**成对**，每条都点名改坏法。⚠️ **一条都没跑**（Unity 全局串行、只有主对话能跑）⇒ §五·1 |
| `TMPDIR=/tmp/wf_wsmall3 typecheck` **0 错** | ✅ | 第 1 次：运行时 0 / 编辑器 2 —— **2 条全在 `Editor/MainMenuScene.cs:5216`（W-E1 的文件，不是我的）**；第 2 次：**0/0**（§七） |

**判据来源**：① `资料/普查产出_1013/WSmall1_Deck两尾巴.md` §六·1（A600 原文 + 7 个调用点）与 §五·6（A601 的宿主与先例）·
② `资料/普查产出_1013/WE4_卡组库静默失败.md` §二/§四（**出口形状的范本** + 断言该怎么写的范本）·
③ **`Shell/CollectionData.cs` 现读**（`DeleteDeck` 那一处 = 照抄的成品）· ④ `RuleEngine/Data/DeckLibrary.cs` 的
`Select:66-69` / `Save():155-161` / `SaveOrWarn():180-188` / `LastError:38`。
**没走原版反编译**：原版卡组存在**服务器**上（`CardDeck.syncedToServer` / `deckId`，见 `DeckLibrary.cs` 文件头自述）
⇒ **本地这条链没有原版对应物**；本笔判据 = **本项目红线「⛔ 不许静默失败」** + A503 已经定下的那套口径。
⛔ **没另立一套**（出口名、分支判据、警告前缀、文案措辞全部与 A503 同形）。

---

## 二、A600 改动（出口形状 · 照的是 CollectionData 哪一处）

**落点（改完后现读行号）**：`Shell/CollectionData.cs` 的 `LastSelectError` `:82` · `Select` `:88-116`
（越界支 `:93-99` · 落盘失败支 `:108-115` · `return true` `:116`）。

**照的是 `CollectionData.DeleteDeck`（同文件 `:156-167`）的哪几段**：

| 形状要素 | 范本（`DeleteDeck`，A503 成品） | 我这一半（`Select`，A600） |
|---|---|---|
| 出口 | `public static string LastDeleteError { get; private set; } = "";` | **逐字同形** `LastSelectError` |
| 开头清 | `LastDeleteError = "";` | 同 |
| 分支判据 | **观测状态**：`Lib.Decks.Count` 变没变（⛔ 不读 `LastError`，`DeckLibrary.Delete` 越界那条早退且**不清 `LastError`**） | **同一思路**：`Lib.CurrentIndex != i`（⛔ 不读 `LastError`，`DeckLibrary.Select` 同样早退 + 不清）—— ★ **没有把 `DeckLibrary.Select` 的越界判断抄第二份** |
| 两支文案 | `"删了，但**没写进存档**：" + SaveFailReason() + "（…重启它又回来）"` / `"**没删** —— 下标 N 越界（库里现在 M 套）"` | `"选中了，但**没写进存档**：" + SaveFailReason() + "（…战斗会拿磁盘上那套旧的）"` / `"**没选中** —— 下标 N 越界（库里现在 M 套）"` |
| 出声 | 两支共用末尾一条 `Debug.LogWarning("[CollectionData] 删卡组失败：" + LastDeleteError)` | 同前缀同形 `"[CollectionData] 选中卡组失败：" + LastSelectError`（写在两支里，因为中间夹着 `Lib.Save()`） |
| 兜底措辞 | 复用已有的 `SaveFailReason()` | **复用同一个 `SaveFailReason()`**（⛔ 没新写文案） |
| 返回值 | `bool`（内存删掉 **且** 落盘成功）；失败时**是哪一种**看 `LastDeleteError` | 同义：`bool` = 当前选中项已经是这一套 **且** 落盘成功 |

**照的是 `CollectionData.ImportDeck` / `CreateDeck` / `DuplicateDeck` 的哪一段**：那三处是「**返回值装不下落盘成败**」
（返回名字/空串）⇒ 拿到空串 + 一条 `Debug.LogWarning`。`Select` 属于**前者**（能返回 `bool`）⇒ 照 `DeleteDeck` 那一档，
⚠️ **不要**按那三处的空串契约去改（会与「调用点只想知道成没成」的语义打架）。

### 二·一 三处与范本**不同**的地方（如实交代，都不是「另立一套」）

1. **签名 `void` → `bool`**（范本本来就是 `bool`）。**这不是改契约**：所有 7 个调用点都把它当**语句**用
   ⇒ 返回值被丢弃，**一处不改也编得过**（已由类型检查实测：`MainMenuScene.cs` / `LiveOpsEventWindow.cs` /
   `CollectionScene.cs:2542` 三个**我碰不到的文件**照样编过）。⛔ 别为了它加 `out` 或改签名形状。
2. **越界那一支【不再落盘】**（改前是 `Lib.Select(i); Lib.Save();` **无条件**写一趟，即使下标越界）。
   现在越界 ⇒ **早退、不写盘** —— 照的是 `DeckLibrary.Delete` 的越界早退（它也不写）。
   ⚠️ **这是一处真实的行为差**：副作用 = 「库里有未落盘的内存改动、又调了一次 `Select(越界)` ⇒ 那一趟水不再冲」。
   本仓没有这种调用点（`DeckLibrary` 每个改动口自己 `Save`/`SaveOrWarn`）⇒ **实测无影响**，但**如实记在这里**；
   若调度台认为要保「无条件写一趟」，把 `if (Lib.CurrentIndex != i) { … return false; }` 挪到 `Lib.Save()` **之后**即可
   （那时 `LastSelectError` 要先算出来、`Save()` 的成功与否只决定返回哪一支）。
3. **判据用「`Lib.CurrentIndex != i`」而不是「`Lib.Select` 的返回值」**：`DeckLibrary.Select` 是 `void`（没有返回值），
   而它的越界判断是 `index >= 0 && index < _decks.Count`。⛔ 我**没有**去改 `DeckLibrary.Select` 的签名
   （那个文件不在白名单）—— 观测状态是**唯一**不作弊的办法，也正是 A503 在同一族问题上定的口径。

### 二·二 A600 的调用点改动（三处，全在白名单内）

| # | 位置（改后行号） | 改前 | 改后 |
|---|---|---|---|
| 1 | `Shell/DeckInfoPopup.cs:1181-1193`（`OnButton` 的 `Select Deck` 支） | `CollectionData.Select(DeckIndex); Debug.Log("[DeckInfo] 已选中「…」"); Close();` | `if (CollectionData.Select(DeckIndex)) Debug.Log("[DeckInfo] 已选中「…」"); else Debug.Log("[DeckInfo] 选中卡组失败：" + CollectionData.LastSelectError); Close();` |
| 2 | `Shell/CollectionWindow.cs:2606-2621`（`SelectDeck`） | `CollectionData.Select(i);`（下面照旧 `RebuildDeckCells`） | `if (!CollectionData.Select(i)) Debug.LogWarning("[Collection] 选中第 " + i + " 套失败：" + CollectionData.LastSelectError);`（**重建那趟照旧无条件跑**） |
| 3 | `Shell/PracticeModePopup.cs:1552-1578`（`StartBotBattle`） | `CollectionData.Select(DeckIndex);  // 注释` | `if (!CollectionData.Select(DeckIndex)) Debug.LogWarning("[Practice] 选中第 " + DeckIndex + " 套失败（⚠️ 这一局会拿磁盘上那套旧的）：" + CollectionData.LastSelectError);` |

---

## 三、7 个调用点逐处表（跟着改 / 不能改 + 为什么）

判据 = 简报那份清单（WSmall1 §六·1），**全仓 `CollectionData.Select` 只有这 7 处** —— 我 `grep` 复核过，一条不多一条不少。

| # | 位置（**现读行号**） | 在白名单？ | 结论 | 为什么 |
|---|---|---|---|---|
| 1 | `Shell/DeckInfoPopup.cs:1181-1193` | ✅ | **跟着改了** | 玩家点 `Select Deck` 那一刻。失败时打 `LastSelectError` 的**原话**（⛔ 不另写一句）。⚠️ **`Close()` 保持无条件、位置不动** —— 见 §五·2 |
| 2 | `Shell/CollectionWindow.cs:2606-2621` | ✅ | **跟着改了** | 点一格卡组那一刻。⚠️ 下面那趟 `RebuildDeckCells` **照旧无条件跑**：落盘失败那一支**内存里的选中态是变了的**，而且格子高亮读的就是 `CurrentIndex()`（`BuildDeckCell` 的 `i == CollectionData.CurrentIndex()`）⇒ 不重建反而会画出过期的高亮 |
| 3 | `Shell/PracticeModePopup.cs:1552-1578` | ✅ | **跟着改了** | **全链最要命的一处**：紧接着（非批处理）就 `LoadScene` 切走，而 `BattleDriver.PickSavedDeck` 是**从磁盘重读**的 ⇒ 落盘失败 = **这一局打的是旧牌**。⛔ **没有加 `return`**（不许静默失败 ≠ 我该改玩家可见流程）—— 见 §五·2 |
| 4 | `Shell/LiveOpsEventWindow.cs:937` | ⛔ **不能改**（待派） | **只报不改** | 遭遇战窗开战那一步，与 #3 同类（也是「切场景前最后一次 `Select`」）⇒ **建议接着派**，改法与 #3 逐字同形 |
| 5 | `Shell/LiveOpsEventWindow.cs:967` | ⛔ **不能改**（待派） | **只报不改**（+ §六·1 一条新发现） | `CreateDeckInMode`：`int idx = IndexOf(name); Select(idx);` —— ⚠️ `CreateDeck` 失败时它是 **−1** ⇒ 新 `Select` 会**回 `false` 并写 `LastSelectError`**；返回值没人读 ⇒ **行为不变**（`Select(-1)` 仍是早退、`_current` 仍由 `DeckLibrary.Create` 置好 ⇒ WE4 §六·4 判过的「良性」**仍然成立**）。**但将来谁读了它，就必须同时处理「新建失败」那一支** |
| 6 | `Editor/MainMenuScene.cs:4152`（⚠️ 现读；W-E1 在写这个文件，行号已从 `4112` **漂到 4152**） | ⛔ **不能改**（W-E1 在写） | **只报不改** | 自检里的「用完还原」调用（`Select(0)` 把当前卡组还回 0 号）⇒ 返回值**忽略是有意**的；⚠️ 若调度台想让它也出声，**必须等 W-E1 收手**再派 |
| 7 | `Editor/CollectionScene.cs:2542` | ⚠️ 文件在白名单，但**只许加断言** | **不动**（有意） | 同一件事：自检里的**还原**调用（`Select(curSaved)`），失败会让后面几节的前置状态漂，但本件没被授权改这一行 ⇒ §五·3 挂一条 |

---

## 四、A601 断言（断什么 · 怎么分辨两支 · 改坏法 · 落点）

**落点**：`Editor/CollectionScene.cs` 的 `Run()` **最末尾**（`int total = _pass + _fail;` **之前**），
两块：**A600 节 `:5213-5332`**（`Section` 在 `:5244`）+ **A601 节 `:5334-5449`**（`Section` 在 `:5335`）。
**宿主 = `CollectionScene.Run`**。共用一套小工具（`:5228-5241`）：`ClickHit(...)` = 抓「这一下点击产生的**全部**日志」
（`Application.logMessageReceived`，**判据范式 = 本文件 `:2111-2135` 的 A229 那一段 + `Editor/BattleScene.cs:210`**；
⚠️ A229 那节**已在 HEAD 里**、且近期自检全绿 ⇒ 这条通道在批处理下**确实会触发**，不是我在赌）；
`HitCount(前缀)` / `FirstHit(前缀)` 按**消息内容**数。

**怎么点**：`DeckInfoPopup` 的 `Opt(n)` / `Btn(n)` 返回**节点**，节点上挂着 `WindowButton`
（`Shell/DeckInfoPopup.cs:1501-1502` 的 `Hit(...)` 里 `hit.AddComponent<WindowButton>(); wb.onClick = onClick;`）
⇒ `ClickHit(pop.Opt("Delete"))` 走的是 `ClickForTest()` → **`Click()`**（与真点**同一口**，判据 = `Shell/PromptPopup.cs:1095`）。
`Opt:Delete` / `Opt:Duplicate` 的 `interactable` 由 `ApplyControlStates`（`:586-588` / `:603-604`）按**库内套数**落一次，
与 `DeckIndex` **无关** ⇒ 「越界那一支」可以**先用有效下标开窗、再把 `DeckIndex` 改成越界**（更贴近真实成因：★ 下标失效）。

### 四·一 A600 节（20 条）—— `CollectionData.Select` 的出口 + 调用点

**探针**：`DeckStore.OverridePath` 拐到 `…/__wf_a600_no_such_dir__/x.json`（父目录不存在 ⇒ `File.WriteAllText` 抛
`DirectoryNotFoundException` ⇒ `DeckStore.SaveAll` 回 false、`error` = `"存档写入失败：" + e.Message`）。
⚠️ **与 A503 那一节不同**：这里**不调** `CollectionData.ResetForTest()`（那会读成空库）—— 内存里那份库**要留着**。

| # | 断什么（简写） | 怎么分辨两种状态 | 改坏法 ⇒ 红 |
|---|---|---|---|
| 1–2 | （前提）探针目录存在 · 库里 ≥ 2 套 | —— | 目录没建 / 套数不够 ⇒ 下面全是假绿（这两条自己先红） |
| 3–5 | （控制组）好路径 `Select(有效下标)` ⇒ 回 **true** · `LastSelectError` **空串** · `CurrentIndex` **真的是它** | **好路径 vs 坏路径**在同一节里各断一次 | 实现写成「恒返 false」/「成功也写错误」⇒ 红 |
| 6–8 | ★ 越界 ⇒ 回 **false** · 报「**没选中**」· **`CurrentIndex` 一动没动** | **越界 vs 落盘失败**两种成因分开断 | 删掉 `if (Lib.CurrentIndex != i)` 那一支 ⇒ #6 红 |
| 9–10 | （前提）那条路径**确实写不进去** · 写不进去时**带了原因** | —— | 路径其实写得进去 / 原因为空 ⇒ 这两条先红（挡住下面「空串恒真」的假绿） |
| 11–13 | ★ 写盘失败 ⇒ 回 **false** · 报「**没写进存档**」· **带上了 `DeckStore.SaveAll` 那条 `catch` 的运行时原话** | **成功 vs 失败**；「随便说一句」vs「把原因说出来」 | 退回裸 `Lib.Save();` 不读返回值 ⇒ #11 红；把原因换成写死的文案 ⇒ #13 红 |
| 14 | ★ 而内存里**真的换了**那一套（`CurrentIndex == target`） | 钉住语义：「失败」专指**没落盘**（与 A503 同一条**语义锚**） | 把失败做成「回滚 `_current`」⇒ 红 |
| 15 | ★ 两种失败是**两句不同的话**（`eRange != eSave`） | 合成一句就红 —— 那正是同类缺陷的形状 | 两支合并 ⇒ 红 |
| 16 | （控制组）好路径点 `Select Deck` ⇒ 「已选中『…』」**且没有**失败那句 | **好路径 vs 坏路径**、**同一份实现**只翻两个输入 | 那一支写成恒报失败 ⇒ 红 |
| 17–19 | ★ 坏路径 + `DeckIndex = n0+7` 点 `Select Deck` ⇒ **恰好一条**失败日志 · 打的是 `LastSelectError` 的**原话** · **没有**「已选中」 | 有 vs 没有；两条**互斥**（弱断言分不出这两种状态） | 退回 `CollectionData.Select(DeckIndex); Debug.Log("已选中…")` ⇒ #17/#19 一起红 |
| 20 | （收尾）`OverridePath` 还回夹具那条 | —— | 没还原 ⇒ 夹具被污染（也会让下一节的库变空） |

### 四·二 A601 节（24 条）—— `DeckInfoPopup` 删 / 复制失败的**两支文案**

| # | 断什么（简写） | 怎么分辨两支 | 改坏法 ⇒ 红 |
|---|---|---|---|
| 1–2 | （前提）探针目录存在 · **2 ≤ 套数 < 114** | —— | `Delete`/`Duplicate` 的 `interactable` 靠它（`n > 1` / `n < 114`） |
| 3–4 | （控制组）先建一套当靶子 · 那扇窗的两颗圆钮**都可点** | —— | 前提不成立 ⇒ 下面几条会**静默变成「点了不生效」**（那条自己会红） |
| 5–6 | （控制组）好路径点 `Duplicate` ⇒ 「已复制成『…』」**且没有**「复制失败」· 库里**真多了一套** | **成功 vs 失败** | 那一支写成恒报失败 ⇒ 红 |
| 7–8 | （控制组）好路径点 `Delete`（删掉刚复制那套）⇒ 「已删除该卡组」**且没有**「删卡组失败」· 库里**真少了一套** | 同上 | 同上 |
| 9–10 | （前提）那条路径**确实写不进去** · **带了原因** | —— | 挡住 #15「空串恒真」的假绿 |
| 11–13 | ★ 删失败**出声** · 越界支文案含「**没删**」且**不含**「没写进存档」· 且**一套都没少** | **越界 vs 没落盘**：同一颗钮、同一路径、**只翻 `DeckIndex`** | 退回旧那句「删不了（…只剩一套时不许删 / 下标越界）」⇒ #12 红 |
| 14–17 | ★ 没落盘支文案含「**没写进存档**」且**不含**「越界」· **带上了运行时真原因** · 内存里**确实少了一套** · 且 `del1 != del2` | 同上；「随便说一句」vs「把原因说出来」 | 两支合成一句 ⇒ #17 红；`LastDeleteError` 换成写死文案 ⇒ #15 红 |
| 18–20 | ★ 复制失败**出声** · 越界支含「**没复制**」且**不含**「没写进存档」· 库里**一套没多** | **越界 vs 没落盘**，同一路径只翻 `DeckIndex` | 退回旧那句「复制失败（`DeckLibrary.Duplicate` 返回空）」⇒ #19 红（它把原因**一律归给「返回空」**） |
| 21–23 | ★ 没落盘支含「**没写进存档**」且**不含**「越界」· 内存里**确实多了一套** · 且 `dup1 != dup2` | 同上 | 同上 ⇒ #23 红 |
| 24 | （收尾）`OverridePath` 还回夹具那条 | —— | 没还原 ⇒ 污染 |

### 四·三 为什么**不算自证 / 不是弱断言**

- **两条探针是环境造的**（不存在的父目录），**期望值来自契约**（`Select` 回 `bool`、失败时按 A503 那套形状给「两种人话」），
  **不是抄实现的输出** ⇒ 实现改了它就红。
- **控制组与探针成对**：「恒报失败 / 恒返 false」与「恒报成功 / 恒返 true」两种坏实现**各有对应的一条能红**
  —— 只断失败那一半会被「恒返 false」蒙过（A600 节 #3-5 vs #11-14；A601 节 #5-8 vs #11-17 / #18-23）。
- **「报的是真原因」那两条（A600 #13 · A601 #15）拿的是运行时值**：`DeckLibrary.Load().Save()` 在坏路径上**独立**
  跑出来的 `LastError`（`DirectoryNotFoundException` 的原文），不是我们源码里的常量 ⇒ **不是同义反复**。
- **「两支文案不同」那两条（A600 #15 · A601 #17 / #23）咬的正是原来的缺陷**：改前那两句**在两种情况下都打**，
  合成一句照样「看着有依据」⇒ 这一条是**专治那个形状**的。
- **灭自证（防「两边一起改回去」）**：把「`CollectionData` 的两种失败」与「`DeckInfoPopup` 的两种文案」**一起改回**旧写法
  ⇒ A600 #15、A601 #12/#17/#19/#23 一起红（判据与被打者不在同一个文件、也不是同一个表达式）。
  把 `DeckStore.SaveAll` 弄成永远失败 ⇒ 两个控制组（A600 #3-5/#16 · A601 #5-8）全红。
- ⚠️ **派活必查行三条自查**：① 不自证 —— 上面已逐条对；② 弱断言 —— 每个「两支」都有**两条互斥的否定断言**
  （「不许说没写进存档」/「不许说越界」/「不许说已选中」）；③ **没有**用 `!RectOfUnion` / 「一个 quad 都没有」那种
  「默认没东西画、新加一层就假红」的形状 —— 本节的断言全是**日志内容 + 计数 + 库内套数**，与渲染层无关。

---

## 五、没查清 / 没做的（⛔ 不猜、不静默）

1. 🔴 **这 44 条断言【没跑过】** —— 本批口径是「A 表清零前不跑自检 / Unity 全局串行、只有主对话能跑」。
   只过了**类型检查**。收口那次 `CollectionScene.Run` 若红，**最可能的四条**（按可能性排）：
   ① **`DeckInfoPopup` 的节点取不到**（`Opt_Delete` / `Opt_Duplicate` / `Btn_Select Deck`）⇒ 症状是
      `del1 == null` / `dup1 == null` / 「（控制组）好路径点 `Select Deck`」那两条红 —— **先看这三条**，
      它们是**其余全部**断言的入口；
   ② **`win.Manager` 在那一刻不可用**（`CollectionScene.Run` 末尾，前面已经有 `ShopWindow`（Fullscreen）开关过一轮）
      ⇒ 症状 = 开窗那一串 `CheckTrue` 全红。⚠️ **这是我没法静态排除的一条**（`:5012` / `:5042` 用的是同一个
      `win.Manager`，所以**大概率没事**，但我没跑过 §五·1 第一条的口径就是这样，**如实记**）；
   ③ **探针目录被谁建出来了**（`…/__wf_a600_no_such_dir__` / `…/__wf_a601_no_such_dir__`）⇒ 那两条「（前提）」自己红；
   ④ **库里套数 < 2**（A503 那节收尾 `ResetForTest()` 之后从夹具重读）⇒ 两条「（前提）」自己红。
   📌 **预期读数**：`_pass` **+44**（全过）；日志里会多出**故意造的**失败行（`[CollectionData] 选中卡组失败：…` ×2 ·
   `[DeckLibrary] …落盘失败` · `[DeckInfo] 删卡组失败：…` ×2 · `[DeckInfo] 复制失败：…` ×2）—— **那是预期输出，不是自检红**。
2. 🔴 **一处【行为设计选择】请你裁**（A600 的三个调用点）：**失败时我把「出声」加上了，但玩家可见流程一律没动** ——
   `DeckInfoPopup` 的 `Close()` **照旧无条件**、`PracticeModePopup` **没有加 `return`**（照样开战）、
   `CollectionWindow` **照样重建列表**。理由：① 那条红线要的是「**不许静默失败**」，而 A398/A503 在本批定的落点就是
   **`LastError` + `Debug.LogWarning`**（`DeckRuntime.TryImport` 失败时也只是 `Say(...)`，调用方 `case "imp_ok"`
   **照样丢弃返回值**）；② 「失败就拦下来 / 弹提示 / 留窗」= **改玩家可见流程**，正是 WSmall1 §五·4 明确留给调度台的那一类。
   ⇒ **要做的话是另一笔账**，三处的改法我写在上面那三条注释里（每处一行）。⛔ 我没自己发明。
3. **`Editor/CollectionScene.cs:2542` 那一处调用点没跟**（`Select(curSaved)` 自检还原调用）。原因是白名单只给「加断言」，
   ⛔ 不许动那一行。**代价**：那一处若失败（写盘坏了），后面的断言会拿到**没还原的**当前卡组 —— 属于**自检内部**的自伤，
   不是产品缺陷 ⇒ 建议**接着派一笔一行的小活**（或并进别的 `CollectionScene` 活里）。
4. **`Select` 越界那一支现在不落盘了**（改前无条件写一趟）—— 见 §二·一·2，**如实记**；本仓无受影响调用点。
5. **A600 只测了一种失败条件**（父目录不存在 ⇒ `DirectoryNotFoundException`）：磁盘满 / 权限 / 只读归档**没测**
   （不确定、不适合进自检）—— `DeckStore.SaveAll` 对它们走**同一条 `catch`**（照 A398 报告 §五·4 的口径，不是新结论）。
6. **没做回滚**（内存改了、盘上没改 ⇒ 内存与盘不一致）：与 A503 一致，A398 在数据层已定过语义
   ⇒ ⛔ **我不自己发明**一套「失败就撤内存」。危险点靠**出声**顶住（三个调用点各一条警告 + `LastSelectError`）。
7. **没有断言去咬「三个调用点里那句 `Debug.Log` 的**通道**（`Log` vs `LogWarning`）**」：调用点 1 用的是 `Debug.Log`
   （为与**同一个 `OnButton`/`OnOption` 里** A548 那两支**同前缀同通道**），调用点 2/3 用的是 `Debug.LogWarning`。
   ⇒ 见 §六·2。**要不要统一 = 调度台裁**，我没动 A548 那两支。

---

## 六、顺手发现（⛔ 本件只报不改 —— 两条全越了白名单 / 不在授权内）

1. 🔴 **`Shell/LiveOpsEventWindow.cs:967` 的 `Select(idx)` 里 `idx` 可能是 −1**（`CreateDeck()` 拿到空串 ⇒
   `IndexOf("")` = −1）。新 `Select` 对它会**回 false 并写下 `LastSelectError`**。
   ⚠️ **行为仍然良性**（`Select(-1)` 是早退、`_current` 由 `DeckLibrary.Create` 置好 ⇒ WE4 §六·4 的判断**不变**），
   但**返回值一旦有人读，就必须同时处理「新建失败」那一支** —— ⛔ 别只把 `if (!Select(idx))` 抄过去。
2. **失败日志的通道在本窗里不统一**：`Shell/DeckInfoPopup.cs` 的 `OnOption` 里，
   `Delete` / `Duplicate` 两支用的是 **`Debug.Log`**（A548 换掉的是**原来就在那儿的** `Debug.Log` 行），
   而我新加的 `Select Deck` 支为**同方法内一致性**也用了 `Debug.Log`；
   但 `CollectionData` 自己的失败一律是 **`Debug.LogWarning`**（A503），`Blocked()` / `PromptPopup.Click()`
   （「点了不生效」）也都是 `LogWarning`。⚠️ **不是缺陷，是口径不齐** ⇒ 统一与否请调度台裁（改一处 = 一行）。
3. **`WSmall1` §五·5 那条「更干净的做法」现在可以做了**：`DeckInfoPopup` 分「复制失败」两支撑的判据是
   「调用前后 `DeckCount()` 变没变」—— 那是因为 `CollectionData.Lib` 私有、拿不到 `LastError`。
   现在同文件里**已经有 `SaveFailReason()` 这个先例**，再给 `DuplicateDeck` 补一条 `LastDuplicateError`
   （照 `LastDeleteError` 的样子）就能让调用点**直接打原话**（与 `Delete` 那支完全同形）⇒ **建议接着派一笔**。
   ⚠️ 本件**没做**（`CollectionData` 那半虽在我白名单，但简报的账是 A600/A601，⛔ 不顺手扩范围）。
4. **`DeckLibrary.Load()` 每次 `new`、没有静态缓存**（`DeckLibrary.cs:41-52`）—— 我这两节**依赖**这一点：
   探针里那个 `DeckLibrary.Load().Save()` 是**另一个实例**，不与 `CollectionData` 那份内存库互相干扰。
   ⚠️ **谁把 `Load()` 改成缓存单例，我这两节的探针就不再独立**（A600 #9/#10、A601 #9/#10 会失真）。
   （这一条 WSmall1 也记过，因为它的探针同形。）
5. **`DeckStore.SaveAll` 写的是 `Path + ".tmp"` 再 `Move`** ⇒ 探针失败时**不会留下半个坏存档**
   （顺着「报的是真原因」那条看的：`DirectoryNotFoundException` 的原文里带的是 `.tmp` 那个路径）——
   **不是缺陷，是好的那一半**，记下来免得以后有人「修」它。

---

## 七、类型检查结果

```
$ TMPDIR=/tmp/wf_wsmall3 bash d:/4/Unity/工具/typecheck.sh      # 第 1 次：改完 4 个 Shell 文件
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
Assets\CardPresentation\Editor\MainMenuScene.cs(5216,39): error CS0103: 当前上下文中不存在名称"phF"
Assets\CardPresentation\Editor\MainMenuScene.cs(5216,63): error CS0103: 当前上下文中不存在名称"phF"
编辑器错误数: 2

$ TMPDIR=/tmp/wf_wsmall3 bash d:/4/Unity/工具/typecheck.sh      # 第 2 次：加完断言
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⚠️ **第 1 次那 2 条错【全部集中在不是我的文件上】**（`Editor/MainMenuScene.cs` = **W-E1 在写**，那个 `phF` 变量
在它那一轮里一会儿有、一会儿没有）⇒ 按简报口径：**重跑 + 如实记，⛔ 没去改别人的文件**。
第 2 次重跑 **0/0** ⇒ **我的五处改动与新增的 44 条断言都编得过**。

**行尾**（二进制读 —— ⚠️ 文本模式读会把 `\r\n` 折成 `\n`、两个数恒相等）：

| 文件 | 改前 `\r\n` / `\n` | 改后 `\r\n` / `\n` | 判定 |
|---|---|---|---|
| `Shell/CollectionData.cs` | `0 / 210`（纯 LF） | `0 / 247` | ✅ 没翻 |
| `Shell/DeckInfoPopup.cs` | `0 / 1500`（纯 LF） | `0 / 1507` | ✅ 没翻 |
| `Shell/CollectionWindow.cs` | `0 / 2759`（纯 LF） | `0 / 2764` | ✅ 没翻 |
| `Shell/PracticeModePopup.cs` | `0 / 1811`（纯 LF） | `0 / 1819` | ✅ 没翻 |
| `Editor/CollectionScene.cs` | `0 / 5236`（纯 LF） | `0 / 5474` | ✅ 没翻 |

**`git diff --numstat`**（改完立刻看 —— ⚠️ **这 5 个文件里，`CollectionData.cs` / `DeckInfoPopup.cs` /
`CollectionScene.cs` 三份都还带着同批别人未提交的改动**，所以数字**不是**我一个人的）：

```
376   0  Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs   ← W-E2 60 + W-E4 78 + 我 238
118  17  Unity/MyGame/Assets/CardPresentation/Shell/CollectionData.cs     ← W-E4 78/14 + 我 40/3
 35   5  Unity/MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs      ← WSmall1 26/3 + 我 9/2
  7   2  Unity/MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs   ← 全部是我的
  9   1  Unity/MyGame/Assets/CardPresentation/Shell/PracticeModePopup.cs  ← 全部是我的
```

**我的两个 hunk 的位置与行数（复核用）**：`git diff -U0` 给出 `@@ -5074,0 +5135,316 @@`（= W-E4 的 A503 节 **78** 行
+ 我这一整块 **238** 行）与 `@@ -4030,0 +4031,60 @@`（= W-E2 的 A404 段）—— **一条不多一条不少**，
说明我**只**加了自己那一块、**没有**碰过那两块（W-E2 段 `:4031-4090` 与 A503 节 `:5142-5211` 现读仍在、无重复）。

**影响面（决定收口要跑哪几条）**：全仓**只有 3 个自检宿主**碰 `CollectionData` —— `Editor/CollectionScene.cs` ·
`Editor/DeckScene.cs` · `Editor/MainMenuScene.cs`（`grep -l "CollectionData\."` 实测）。
其中**只有 `CollectionScene.Run` 与 `MainMenuScene.Run` 走到 `Select`**（`DeckScene` 不调它）
⇒ 建议收口跑 **`CollectionScene.Run` + `MainMenuScene.Run`** 两条（`DeckScene.Run` 只是「顺带」，跑不跑都行）。
