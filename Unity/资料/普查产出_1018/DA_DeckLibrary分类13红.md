# DA · DeckLibrary 分类 13 红 · 只读诊断

> 只读诊断代理 · 2026-10-18 批次（HEAD = `fe239e9`，工作树干净）· 本文件是本代理**唯一**的产出。
> 证据坐标：`d:/4/_tmp_view/ruleengine.log`（4.0 MB）· `d:/4/_tmp_view/deck.log`（1.6 MB）· 源码按 `文件:行号`。

---

## 1. 一句话结论

**一个根因、一处代码**：`DeckStore.LoadAll`（`Assets/RuleEngine/Data/DeckStore.cs:136`）的 `Empty` 分支
判据 `dto.decks == null` **对「合法 JSON 但缺 `decks` 键」的输入恒假** ——
`JsonUtility` 把缺键的集合字段给成**空表、不是 `null`** ⇒ 该文件走**成功支**（`note = None`、`detail = null`）
⇒ `ClassifyLoad(null, existed:true) = None`（不是 `Failed`）⇒ 下游「类型 / 错误码 / 诊断串 / 词条键」四级全塌。

* **判决 = (β) 实现缺陷**（13 条同一根因；`δ 夹具前提写错` 是同一件事的另一面，不是独立问题）。
* **最小改法 = 只改 `DeckStore.cs` 一处**（加一行「原文里有没有 `decks` 键」的结构性判据），
  **13 条断言一个字都不用动**，全部转绿。
* `ClassifyLoad` **本身是对的**（它的四条断言 ②-1 全绿），**不要动它**；也不要动那 13 条断言（它们断的是有出处的意图）。
* ⛔ **不推荐**「把坏档夹具换成语法坏掉的文本」那条（看着更省事）：它会把 `Empty` 这一档**永久钉成死代码**，
  与铁律 11 + 红线「不许静默失败」相悖，见 §2 末「备选（不推荐）」。

---

## 2. 逐条诊断表

⚠️ **读日志的陷阱（本批已踩）**：`DeckLibrary.DeckLoadIssue.None` 与 `DeckStore.LoadNote.None` **同名**，
报文里的 `实得 [None]` 光看字**分不出是哪个枚举**，必须看 `Check<T>` 的类型参数 —— 本表已逐条按类型参数判过。

| 账 | 日志行 | 断言（源码坐标） | 结论 | 证据（实测值） | 该照哪个判据 | 最小改法 | 置信度 |
|---|---|---|---|---|---|---|---|
| **RE-1** | `ruleengine.log:58797` | `RuleEngineTest.cs:19713` `Check(libBad.LastLoadIssue, Failed)` | **β** | 实得 `DeckLoadIssue.None`（`Check<DeckLibrary/DeckLoadIssue>`） | `DeckLibrary.cs:110` `ClassifyLoad` 三分法：文件在 + 有话 ⇒ **Failed** | 见下「一处改法」 | 高 |
| **RE-2** | `ruleengine.log:58811` | `RuleEngineTest.cs:19715` `!string.IsNullOrEmpty(libBad.LastError)` | **β** | 实得 `False`（`LastError` 是 null） | `DeckLibrary.cs:137`：只在 `Failed` 时把诊断串装进 `LastError` | 同上（级联） | 高 |
| **RE-3** | `ruleengine.log:58826` | `RuleEngineTest.cs:19718` `LastLoadCode == LoadNote.Empty` | **β** | 实得 `False`（码实测 = `None`，见 DK-1） | `DeckStore.cs:72-73`：`Empty` = 「存档在，但解析出来是空的」 | 同上（级联） | 高 |
| **RE-4** | `ruleengine.log:58841` | `RuleEngineTest.cs:19720` `LastLoadIssueTerm == ErrorLoadDeckTerm` | **β** | 实得 `False`（`TermKeyOf(None)` 返回 `null`，见 `DeckStore.cs:105-106`） | `DeckStore.cs:98-108` `TermKeyOf`；键的出处见 §4 | 同上（级联） | 高 |
| **DK-1** | `deck.log:17259` | `DeckScene.cs:4839` `Check(code, LoadNote.Empty)`（`LoadAll` 的 **out 码**，**没经过 `ClassifyLoad`**） | **β** | 实得 `LoadNote.None`（`Check<DeckStore/LoadNote>`）—— **本账是全批最直接的一条观测** | 同 RE-3 | 同上 | 高 |
| **DK-2** | `deck.log:17314` | `DeckScene.cs:4866` ②-a 类型 `Failed` | **β** | 实得 `DeckLoadIssue.None` | 同 RE-1 | 同上 | 高 |
| **DK-3** | `deck.log:17327` | `DeckScene.cs:4868` ②-a 码 `Empty` | **β** | 实得 `LoadNote.None` | 同 RE-3 | 同上 | 高 |
| **DK-4** | `deck.log:17340` | `DeckScene.cs:4875` ②-b **英文档**类型 `Failed` | **β** | 实得 `DeckLoadIssue.None`（与语档无关，这里根本没走到语言那一层） | 同 RE-1 | 同上 | 高 |
| **DK-5** | `deck.log:17353` | `DeckScene.cs:4878` ②-b 英文档码 `Empty` | **β** | 实得 `LoadNote.None` | 同 RE-3 | 同上 | 高 |
| **DK-6** | `deck.log:17366` | `DeckScene.cs:4883` ②-c `Loc.HasEntry(zhTerm ?? "")` | **β**（级联） | 实得 `False`；报文里键实测是**空串**（`（``）`）⇒ `zhTerm = null` | 键应有值且**在表里**：`Loc.cs:837` 有 `{"CustomErrors/ErrorLoadDeck", Entry("卡组存档读取失败","Could not load your decks")}`（坑表 #18 那一半**本来就是绿的**） | 同上（`LastLoadIssueTerm` 一有值，`HasEntry` 即真） | 高 |
| **DK-7** | `deck.log:17380` | `DeckScene.cs:4886` ②-c 两档键同一个 | **β**（级联） | 实得 `False`；实测「」/「」（两个都是 `null`；`null == null` 为真，但前半 `!string.IsNullOrEmpty(zhTerm)` 短路成假 —— **这条断言写得对**） | `DeckStore.cs:98-108`（键只由码决定，与语档无关） | 同上 | 高 |
| **DK-8** | `deck.log:17394` | `DeckScene.cs:4888` ②-d 灭自证（诊断串非空 / 逐字不随语档变 / 无汉字） | **β**（级联） | 实得 `False`；`zhDiag = LastError = null` | `DeckLibrary.cs:137` + `DeckStore.cs:139` 的诊断串是常量、纯 ASCII | 同上 | 高 |
| **DK-9** | `deck.log:17408` | `DeckScene.cs:4892` ②-e 两档显示文案逐字不同 | **β**（级联） | 实得 `False`；两档都取到空串（`Loc.T(null)`） | `Loc.cs:837` 那条词条**真有两列**（中/英不同）⇒ 一旦有键，这条自动真 | 同上 | 高 |

**计数核对**：`ruleengine.log` 4 条（58797/58811/58826/58841）· `deck.log` 9 条（17259/17314/17327/17340/17353/17366/17380/17394/17408）
= **13 条**，与任务书给的账目**逐行吻合**（13 个行号 `sed` 取回，逐条对上）。

### 一处改法（推荐，13 条全绿）

`Assets/RuleEngine/Data/DeckStore.cs` 的 `LoadAll`，现 `:134-141`：

```csharp
                var text = File.ReadAllText(path);
                var dto = JsonUtility.FromJson<Dto>(text);
                if (dto == null || dto.decks == null)          // ← :136 这一判据对「缺键」恒假
                {
                    note = LoadNote.Empty;
                    detail = "save file has no decks";
                    return new List<PlayerDeck>();
                }
```

改成（**只加一条结构性判据 + 一段注释；`ClassifyLoad` / 断言 / 词条一个字不动**）：

```csharp
                var text = File.ReadAllText(path);
                // 🔴 2026-10-18：`JsonUtility` 对**缺键**的集合字段给的是**空表、不是 null**
                //    （实测：`{ "version": 1, "current": 0 }` ⇒ `dto.decks` 非 null ⇒ 下面那句 `== null` 恒假
                //     ⇒ `Empty` 这一支自 51351fa 起**从没被走到过**。
                //     证据 = `_tmp_view/deck.log:17259`（`LoadAll` 的 out 码实测 `None`）+
                //     `_tmp_view/ruleengine.log:58811`（`LastError` 实测 null））
                //    ⇒ 判「这份存档有没有 `decks` 那一段」只能看**原文的 JSON 键**（结构性，不是给人看的文案）。
                //    ⚠️ 键名照我们的写入器（`SaveAll` 走 `JsonUtility.ToJson` ⇒ 恒写 `"decks":`）。
                bool hasDecksKey = text.IndexOf("\"decks\":", StringComparison.Ordinal) >= 0;
                var dto = JsonUtility.FromJson<Dto>(text);
                if (dto == null || dto.decks == null || !hasDecksKey)
                {
                    note = LoadNote.Empty;
                    detail = "save file has no decks";
                    return new List<PlayerDeck>();
                }
```

* 用 `"decks":`（带冒号）而**不是** `"decks"`：后者会被「卡组名字恰好叫 `decks`」的旧档误判（字符串值 `"decks"` 后面跟的是 `,`/`}`，**永远不是 `:`**）。
* `SaveAll` 是**唯一**写入器且恒写该键 ⇒ **零迁移风险**（首版 `51351fa` 就已经是 `{version, decks}` 这个 Dto 形状，查过）。
* 一处改动覆盖全部 13 条：`LastLoadCode = Empty` ⇒ `ClassifyLoad` 得 `Failed` ⇒ `LastError = detail` ⇒ `TermKeyOf` 出键 ⇒ `Loc.HasEntry`/两档文案全真。

### 备选（**不推荐**，仅列出以便裁断）

把坏档夹具从「合法 JSON 缺 `decks`」换成**语法坏掉**的文本（`"{ 这不是 json"`，`BattleScene.cs:7246` 用过、**实测绿**），
再把 3 条断言的 `LoadNote.Empty` 改成 `LoadNote.ReadFailed`（`RuleEngineTest.cs:19718` · `DeckScene.cs:4868` · `:4878`；
`TermKeyOf` 对两码返回同一条键 ⇒ 其余 10 条不用改）。

* ⚠️ 代价：`Empty` 这一档于是成为**文档里写着（`DeckStore.cs:72-73`）、实际永远走不到**的死代码 ——
  「存档在、但拿不出卡组」这件事仍然静默（玩家看到的是「卡组没了」而不是报错），
  与铁律 11（不许按成本回避缺漏）+ 红线「不许静默失败」相悖。**只在你判定 `Empty` 应当废弃时才走这条**；
  若走这条，`Empty` 枚举成员与 `TermKeyOf` 的那一支要**一并删掉**并在文档里写明理由，别留着当摆设。

---

## 3. 根因分析（含「为什么排除其它候选解释」）

**观测链（每一步都有日志坐标）**：

1. `deck.log:17259` 是全批**最干净的一条观测**：它断的是 `LoadAll` 的 **out 码本身**，
   **没经过 `ClassifyLoad`**（`DeckScene.cs:4838`）⇒ 实测 `LoadNote.None`。
   而 `LoadAll` 里赋 `note` 的地方只有 4 处：`:122` 初始化（`None`）、`:128`（`NoSaveFile`）、`:138`（`Empty`）、`:165`（`ReadFailed`）。
2. 文件**必然存在**（同一段里 `File.WriteAllText(p, ...)` 刚写过、`probeDir` 的存在性前提断言绿、①-a 删档那条实测 `NoSaveFile`）
   ⇒ 排除 `:128`。
3. 同一段日志 `①-c`（`deck.log:17286` / `:17300`）拿**语法坏掉**的文本实测得到 `ReadFailed` + `detail` 前缀 `read failed:`
   ⇒ 说明 `FromJson` 对坏语法是**抛异常**、走 `:163` 的 catch ⇒ 排除 `:165`（本例没抛）。
4. ⇒ 只剩 `:122` 初始化后**一路走到成功返回**（`:161 return dto.decks`）这条 —— 此时 `detail` 从未被赋值 = `null`、`note = None`。
   这正是「缺 `decks` 键 ⇒ `note=None` + `detail=null`」的唯一出口，与 RE-1/RE-2、DK-1、DK-6~DK-9 的实测值**逐项吻合**。
5. ⇒ **`dto.decks` 非 `null`**（否则会走 `:138`）⇒ `dto.decks == null` 这一判据对「缺键」输入恒假 ⇒ `Empty` 是死代码。

**为什么排除其它候选**：

| 候选解释 | 为什么排除（证据） |
|---|---|
| **(A) 夹具的路径 / `OverridePath` 没设对，文件其实不在** | 文件不在 ⇒ `note = NoSaveFile`（`:128`），实测是 `None`；且同一段的 `①-a`（删档）**实测就是 `NoSaveFile`**（绿），证明路径口是通的 |
| **(B) 日志是旧的、跑的还是老 `DeckStore`** | 同一段日志里绿的是**新代码才有**的东西：`①-a` 断 `!Loc.HasCjk(detail)` 且实测 `「no save file」`、`①-c` 断 `detail.StartsWith("read failed:")` —— 老代码写的是 `"还没有存档"`/`"存档读取失败："` ⇒ **跑的就是 HEAD 的代码** |
| **(C) `JsonUtility` 回 `null` / 抛异常** | 回 null ⇒ `:136` 首项为真 ⇒ `Empty`（不是 `None`）；抛异常 ⇒ `ReadFailed`。两条都不是 `None` |
| **(D) `ClassifyLoad` 自己写错** | ②-1 四条（中文档 / 英文档 / 反方向 `NoSaveFile` / `null⇒None`）**全绿**；函数体 `DeckLibrary.cs:110-114` 只看「`note == null`」+「`existed`」两件结构性的事，与语言/措辞无关 —— **它是对的，别动** |
| **(E) 断言错（α）** | 断言断的是**文档里写明的意图**（`DeckStore.cs:72-73` 的 `Empty` 定义 + `ClassifyLoad` 的 `Failed` 定义 + 词条键在表里）—— 意图本身站得住（见 §4）；**实现没做到**，所以是 β 不是 α |
| **(F) 本批回归（γ）** | 这两处 `:136` 的判据**自首版 `51351fa` 就一字未改**（`git show 51351fa:.../DeckStore.cs` 里是同一条 `dto == null \|\| dto.decks == null`）⇒ 不是本批改坏的；本批只是**第一次写断言去断它**。判据（G3 的 `ClassifyLoad`）是本批新增，且**是对的** |

**为什么「13 条」而不是「1 条」**：这 13 条是**同一状态量的四级出口**（类型 → 码 → 诊断串 → 词条键），
外加「中英两档」各断一遍 ⇒ 一个 `None` 塌成 13 条红。**改一处即全绿**，符合「同一根因的合并说」。

---

## 4. 原版在这几处应该是什么行为（带出处）

1. **原版没有本地卡组存档** —— 卡组在**服务器**上（`CardDeck.syncedToServer` / `deckId`；我们这边
   `DeckStore.cs:6-9` 已如实标注「和原版不同」）。⇒ 「本机缺存档文件」「本机这份 JSON 读不出来」
   **这两档没有原版判据**，是我们自定的（`LoadNote.NoSaveFile` / `Empty`）。
2. **有原版出处的只有那条词条键**：
   * 枚举：`CustomError.ErrorLoadDeck = 170` —— `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CustomError.cs:24`（**实读**）。
   * 字面量：`"CustomErrors/ErrorLoadDeck"` 在 `d:/2/tools/il2cpp_out/stringliteral.json:26611`（`"address": "0x425D0A8"` 紧邻其上）
     —— `RVA = 0x425D0A8` = 反编译里的 `DAT_18425d0a8`（`0x180000000` + RVA）**逐字对上**。
   * 拼法：`"CustomErrors/" + 枚举名`（`decomp_full/CloudscriptHandler__HandleCustomError.c`，G8 已实读）。
3. ⚠️ **原版唯一的消费点，触发条件与我们不同**（如实记）：
   `d:/2/tools/decomp_full/SearchOpponentManager__SearchOpponent.c`（约 `:81-95`，全库**唯一**一处用 `DAT_18425d0a8`，
   `grep -rln "DAT_18425d0a8"` 只命中这一个文件）：
   `GameStaticData.IsValidDeck(deck, 0) == false` ⇒ `CancelBattleSearch` ⇒ `LocalizationManager.GetTermTranslation(DAT_18425d0a8)`
   ⇒ **原版这条键是「你这副牌校验不过、开不了局」时说的那句话**，不是「本地存档文件读不出来」。
   ⇒ 我们把它挂在「读存档失败」上是**同一个「你的卡组用不了」家族**，键没写错、但**触发点与原版不同**
   （原版没有本地存档 ⇒ 没有等价触发点）。**不是本次红的成因**，别顺手改。
4. ⇒ 因此「文件在、却拿不出卡组 ⇒ 要说出来」这条**只能靠我们自己的两条红线**当判据：
   ① `DeckStore.cs:116-119` 的注释（`detail` 在「有话要说」的三种情形下一律非空 = 「有没有话要说」这个信号）；
   ② 项目红线「**不许静默失败**」+ 铁律 11（发现缺漏**要做**，只有先后之分）。
   **这两条判据支持「把 `Empty` 做成可达」，不支持「删掉它」**。

---

## 5. 没查清的部分（⚠️ 明说，不拿猜测填空）

1. **机制未查清（观测已坐实）**：`JsonUtility` 对「**JSON 里没有这个键**的集合字段」到底怎么处理 ——
   是序列化后端把集合初始化成空表、还是在跑字段初始化器（`Dto.decks` 是裸字段、**没有初始化器**，
   所以后者解释不通，但我没读到源码级判据）。**同一问已被别人点过**：
   `d:/4/Unity/资料/普查产出_1018/REV_W2_教程执行器.md:250`（「本地没查到判据」）。
   ⇒ 但**「`dto.decks` 非 null」这件事本身由两条独立日志观测坐实**（见 §3 第 5 步），改法不依赖机制。
2. **`dto == null` 是否可达** —— 没查清。JSON 顶层是字面量 `null`、或非对象（数组/标量）时会怎样，本地查不到判据。
   若可达，它仍指向同一个 `Empty` 支（修法覆盖它）。
3. **显式 `"decks": null` 的存档**（键在、值是 null）—— 按「物化」那一族应也落成空表 ⇒ 我的修法（看**键在不在**）
   会把它判成「读到了」。⚠️ 我们自己的写入器**不会**产出这种文件（`SaveAll` 恒写 `"decks": [...]`），
   所以这是**残余弱点**，不是当前缺陷；要彻底堵住得把 `decks` 判成「键在 **且** 值不是 null」——`JsonUtility` 做不到，没查清出路。
4. **「缺 `decks` 键的存档」在真实玩家路径上怎么产生** —— 没查清。目前只想到「外部改档 / 未来格式 / 别的工具写的文件」；
   没查清有没有别的产生点（例如某条只写部分字段的迁移路径）。
5. 🔴 **我的修法依赖一个「预期但离线验不了」的小前提**：`JsonUtility.ToJson` 对**空表**写的是 `"decks": []`
   （而不是省略该键）—— 若它省略，则「删光卡组」的合法存档会被新判据判成 `Empty`（**假报错**）。
   预期它是写 `[]` 的（Unity 序列化不省略空数组），但**本地验不了**（`JsonUtility` 只在 Unity 里跑）。
   ⇒ **建议配套补一条断言**（成本一条，代价极低）：「`SaveAll(0 套)` → `LoadAll` ⇒ 码 `None`、`LastLoadIssue = None`」
   —— 它同时挡「新判据过界」和「把 `Count == 0` 也当失败」这两种改坏法。
6. **`FogMode` 那类枚举兜底** 与本题无关，未查（列在这里只为说明我没有扩面）。

---

## 6. 顺手发现（**一个都没改**，报上来由调度台分流）

1. 🔴 **同形状的兄弟写法都是「或空」形式，只有 `DeckStore` 是裸 `== null`** ——
   `Shell/PrebuiltDecks.cs:118`（`_doc.decks == null || _doc.decks.Length == 0`）·
   `RuleEngine/Data/TutorialDatabase.cs:42`（`f.stages == null || f.stages.Length == 0`）·
   `Core/OffensiveCards.cs:96`（`f.armies == null || f.armies.Length == 0`）。
   ⚠️ **但 `DeckStore` 不能照抄那个「或空」**：`ClassifyLoad` 明写「**哪怕是 0 套的合法存档** ⇒ `None`」，
   而 `DeckLibrary.Delete` 允许把库删到 0 套、`SaveAll` 会写出 `"decks": []` ——
   照抄「或空」会让「删光卡组的玩家」下次打开时看到「卡组存档读取失败」。**这是两个形状看着一样、语义不同的地方**，
   值得进坑表（「判『模块在不在』：集合字段的 `== null` 恒假；**但『空集合』不等于『缺键』**」）。
2. **日志读法陷阱**（已写进 §2 表头）：`DeckLoadIssue.None` / `LoadNote.None` 同名，
   `期望 [None]` 必须靠 `Check<T>` 的类型参数分辨 —— 否则会把 RE 那 4 条误读成别的东西。
3. **原版 `ErrorLoadDeck` 的触发点与我们不同**（§4.3）—— `SearchOpponentManager` 那条是「卡组校验不过」，
   全库只有这一处。如实记着，别当「对上了原版」。
4. **「语法坏档」这条夹具路已验证可用**：`BattleScene.cs:7246`（`"{ 这不是 json"`）**实测绿**；
   `DeckRulesTest.cs:250` 同一路，实测打印 `ReadFailed / read failed: ArgumentException: JSON parse error: Missing a name for object member.`
   （`ruleengine.log:24317`）⇒ 若走 §2 的「备选」，这条夹具是**已被证过**的，不是碰运气。
5. **`DeckStore.LoadAll(out string note)` 兼容重载的残留形状**（不是本账，但同族）：
   现存 2 个调用点 `DeckRulesTest.cs:227/473`，其中 `:473` 调用后 `:476` 那条是 `note == null || !note.Contains("失败")`
   —— `Contains` 那一半**已恒假**（`note` 现在是诊断串），只因前半 `note == null` 短路才没红。
   ⇒ 同一句「拿给人看的字当判据」的老话在**别处还有副本**，值得扫一遍（铁律 5 的「grep 副本一起改」）。
6. `DeckScene.cs:4839` 那条断言是**唯一**直接断 `LoadAll` out 码的地方（不经 `ClassifyLoad`）
   —— 它在这次诊断里价值最高，**保留它**（它把「`ClassifyLoad` 与 `LoadAll` 两处口径」分开钉住了）。

---

### 附：本次诊断用过的命令（可复核）

```bash
sed -n '58750,58870p'  d:/4/_tmp_view/ruleengine.log      # 4 条红 ±40 行
sed -n '17220,17430p'  d:/4/_tmp_view/deck.log            # 9 条红 ±40 行
sed -n '58797p;58811p;58826p;58841p' d:/4/_tmp_view/ruleengine.log   # 逐条核对行号
sed -n '17259p;17314p;17327p;17340p;17353p;17366p;17380p;17394p;17408p' d:/4/_tmp_view/deck.log
git show 51351fa:Unity/MyGame/Assets/RuleEngine/Data/DeckStore.cs    # 首版判据（证明不是本批回归）
sed -n '26604,26614p' d:/2/tools/il2cpp_out/stringliteral.json       # 0x425D0A8 = "CustomErrors/ErrorLoadDeck"
grep -rln "DAT_18425d0a8" d:/2/tools/decomp_full                     # 全库唯一消费点
```
