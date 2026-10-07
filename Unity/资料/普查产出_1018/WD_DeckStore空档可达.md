# WD · `DeckStore` 的 Empty 档做成可达

> 写手 WD · 2026-10-18 批次 · **只改了一个文件**：`Unity/MyGame/Assets/RuleEngine/Data/DeckStore.cs`
> 判据正本 = `资料/普查产出_1018/DA_DeckLibrary分类13红.md`（§3「一处改法」+ §5.5）。
> ⛔ 未跑 Unity、未动 git、未碰 `RuleEngineTest.cs` / `Editor/DeckScene.cs` / 两张正本 / 任何测试文件。

---

## 1. 改了什么（文件:行号 · 改前 → 改后 · 一句话理由）

**文件**：`d:/4/Unity/MyGame/Assets/RuleEngine/Data/DeckStore.cs`（唯一改动文件）

### 改动 ①（核心 · `LoadAll` 判据）`DeckStore.cs:136-166` 处

**改前**（原 `:134-136`）：

```csharp
                var text = File.ReadAllText(path);
                var dto = JsonUtility.FromJson<Dto>(text);
                if (dto == null || dto.decks == null)
```

**改后**（现 `:136-168`）：

```csharp
                var text = File.ReadAllText(path);
                // 🔴 2026-10-18 订正（铁律 5：原来写 X、为什么恒假、证据在哪）：…（30 行订正注释，见源码）…
                bool hasDecksKey = text.IndexOf("\"decks\":", StringComparison.Ordinal) >= 0;
                var dto = JsonUtility.FromJson<Dto>(text);
                if (dto == null || dto.decks == null || !hasDecksKey)
```

* **新增一行实质代码**：`bool hasDecksKey = text.IndexOf("\"decks\":", StringComparison.Ordinal) >= 0;`（`DeckStore.cs:166`），
  并把它作为**第三项**或进原来的判据（`DeckStore.cs:168`）。位置**在 `FromJson` 之前**（照 DA §3：这是**原文 JSON 的结构性判据**，不是解析结果）。
* **理由**：`JsonUtility` 对**缺 `decks` 键**的集合字段给的是**空表、不是 `null`** ⇒ 原来那句 `dto.decks == null` 对这类存档**恒假**
  ⇒ `Empty` 这一支**自首版 `51351fa` 起从没被走到过**。改后这一支真的可达。
* 用**带冒号**的 `"decks":`（DA §3 指定）：字符串值 `"decks"` 后面跟的是 `,` / `}`，**永远不是 `:`** ⇒ 不会被「卡组名字恰好叫 decks」的旧档误判。
* **30 行注释**（`DeckStore.cs:137-165`）按铁律 5 的格式写全了：原来写 X / 为什么恒假 / 证据在哪（
  `_tmp_view/deck.log:17259` + `_tmp_view/ruleengine.log:58811`）/ 代价（13 条红）/ 为什么只能看原文键 /
  ⛔ 别加「或空」/ ⚠️ 离线验不了的那个前提。

### 改动 ②（同文件 · 文档与实现对齐）`DeckStore.cs:72-75`（`LoadNote.Empty` 的 XML 注释）

* **改前**：`/// <summary>存档在，但解析出来是空的（`dto == null || dto.decks == null`）。</summary>`
* **改后**：`/// <summary>存档在，但**拿不出卡组** —— 2026-10-18 起判据 = `dto == null || dto.decks == null`
  **或原文 JSON 里没有 `decks` 键**（原文里原来只写了前两项 ⇒ 对「缺键」恒假 ⇒ 这一档曾是死代码；订正经过见 `LoadAll` 里那段注释）。</summary>`
* **理由**：铁律 5 —— 这条注释**逐字写着**那条恒假的判据，改完代码不改它，它就成了一条**指向已废弃判据的假记录**
  （下个会话读到「判据 = `dto == null || dto.decks == null`」会以为缺键那一档还是没人管）。
  ⚠️ 这是**我这一趟越出 DA §3 的一处**（DA §3 只列了核心那一处），属同文件、同一条订正，**请调度的审查代理复核这一处要不要留**。

**没动的东西**（逐条对照红线）：`DeckLibrary.ClassifyLoad`（`DeckLibrary.cs` 一个字未动）· 13 条断言 ·
`TermKeyOf` · `SaveAll` · 兄弟文件的「或空」写法 · 测试文件。

---

## 2. 为什么新判据【不会】误报（逐条）

### ① 「0 套」的合法存档（玩家删光卡组）—— **不会**误报，因为判据**不看数量**

新判据只问「**原文里有没有 `decks` 那一行**」，**完全不碰 `Count`**。
`DeckLibrary.Delete` 把库删到 0 套后，`SaveAll` 写出的仍是 `"decks": []`（键在）⇒ `hasDecksKey = true`
⇒ 落 `None` + `detail = null`，与 `ClassifyLoad` 明写的「**哪怕是 0 套的合法存档 ⇒ `None`**」一致。
⛔ 我**没有**照兄弟写法加 `|| dto.decks.Count == 0`（那才会让「删光卡组的玩家」看到「卡组存档读取失败」）。

> ⚠️ 这一条依赖「`JsonUtility.ToJson` 对空表写 `[]`、不省略键」这个前提 —— 见 §5 与下方 witnesses。

### ② 名字叫 `decks` 的卡组 —— **不会**误报

* 卡组名是**字符串值**：序列化成 `"Name": "decks"` ⇒ 那 7 个字符后面跟的是 `,` 或 `}`，**不是 `:`** ⇒ `IndexOf("\"decks\":")` 不命中。
* 卡组名恰好**含引号**（`decks":`）时更安全：`JsonUtility` 会把 `"` 转义成 `\"`
  ⇒ 原文里是 `"decks\":`（多了个反斜杠）⇒ 与我的模式 `"decks":` **不同串** ⇒ 也不命中。
* 实据（我们的写入器长什么样，逐字读过）：`_tmp_view/menu/_menu_boundary_decks.json` ·
  `collection/_test_decks.json` · 玩家真存档 `…/LocalLow/Unity Technologies/com_unity_template_urp-blank/WarpforgeDecks.json`
  —— 文件形状是**4 空格缩进的 pretty print**（= `JsonUtility.ToJson(dto, true)`），`"decks": [`（**冒号后有空格**），
  我的模式 `"decks":` 是它的**前缀** ⇒ 命中。

### ③ 显式 `"decks": null`（键在、值是 null）—— ⚠️ **残余弱点，如实记，没查清**

* 我们自己的写入器**不会**产出这种文件：`SaveAll` 里 `decks = decks ?? new List<PlayerDeck>()`（`DeckStore.cs:234`）
  ⇒ 恒写 `[]` 或数组。
* 若外部手改出这种文件：`hasDecksKey = true`；`dto.decks == null` 那一项**是否**接得住它 —— **没查清**
  （`JsonUtility` 把显式 `null` 物化成 `null` 还是空表，本地查不到判据；DA §5.3 也标了同一个「没查清」）。
  物化成 `null` ⇒ 被原判据接住（仍 `Empty`）；物化成空表 ⇒ 我这条判成「读到了」（残余弱点）。
  ⛔ 我**没有**拿猜测填空，这一点与 DA §5.3 口径一致。

### ④ 另一条正面证据：`json` 键不会被「省略」

`_tmp_view/daily_selftest_rewards/daily.json`（**由 `JsonUtility.ToJson` 写的**，`DailyData.cs:1737`）里，
一个**在内存中是 null** 的集合字段被写成 **`"rows":null`** ——
⇒ 该序列化器**连 null 的键都照写**，不存在「键被省略」这回事。

---

## 3. 类型检查读数（原样贴）

命令：`TMPDIR=/tmp/wf_wd bash d:/4/Unity/工具/typecheck.sh`（**带独立 TMPDIR**，两处改动都落盘之后跑的）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⇒ **0 / 0**。⚠️ 批处理跑着时**别的写手在改 `RuleEngineTest.cs` / `DeckScene.cs`**，
所以这个读数里可能已经混进了他们的半成品 —— 本次没有报错，无需分辨。

---

## 4. 行尾核对（前后各一次读数）

| 时刻 | 命令 | 读数 |
|---|---|---|
| 改之前 | `python -I -c "…b.count(b'\r\n'),b.count(b'\n')"` | **CRLF: 228 / LF: 228** |
| 改之后 | 同上 | **CRLF: 260 / LF: 260** |
| 改之后 | `git -C d:/4 diff --numstat -- Unity/MyGame/Assets/RuleEngine/Data/DeckStore.cs` | **34 / 2** |

**结论：行尾没被翻。** 判据两条：
① 两个读数**恒相等**（每一行都是 CRLF）= **纯 CRLF**，改前改后**同一种行尾**；
② `34/2` 远小于文件行数（改后 **260** 行 = `228 + 34 − 2`）⇒ 不是整篇重写（若翻了会接近 260/260）。

🔴 **本条与简报里那句「`DeckStore.cs` 是 LF」相反 —— 实际是【纯 CRLF】**（简报原文：「DA 未直接报，但同目录兄弟多为 LF」）。
我**没有**按 LF 处理，也没改行尾；⚠️ **提醒**：别的写手/代理若照「它是 LF」的前提去用 `sed -i` 或 python 文本写，会把这个文件整篇翻成 LF。

---

## 5. 🔴 离线验不了的那个前提：我找到了【同类 witness】，但没有【直接的】

DA §5.5 点的前提：**`JsonUtility.ToJson` 对空表写的是 `"decks": []`，而不是省略该键** ——
若它省略，「删光卡组」的合法存档会被我的新判据判成 `Empty`（**假报错**）。我按任务书在代码里如实挂着这条（`DeckStore.cs:160-164`），
**没有自己去改测试文件**。下面是这一趟为它做的**离线取证**（都没改任何东西，只读）：

| 找什么 | 结果 |
|---|---|
| **直接的**：仓库里有没有「0 套」的存档文件（`"decks": []`） | ❌ **没有**。`grep -rn '"decks": \[\]'` 在整个 `_tmp_view` + 玩家真存档目录 **0 命中**；5 份残留夹具（`_test_decks` / `_menu_boundary_decks` / `_menu_scroll_decks` / `_menu_test_decks` / `_shell_a754_decks`）全是**非空** `decks`；玩家真存档（`…LocalLow/Unity Technologies/com_unity_template_urp-blank/WarpforgeDecks.json`，1058 字节）也**有**卡组 |
| **同类的**：同一个序列化器写**空集合**时写什么 | ✅ **`[]`**。`_menu_boundary_decks.json` / `_menu_scroll_decks.json` / `_shell_a754_decks.json` 里都有 **`"CardIds": []`** —— 那是 `PlayerDeck.CardIds`（`List<string>`）**非 null 但空**的情形，而**这三个文件正是 `DeckStore.SaveAll` → `JsonUtility.ToJson(dto, true)` 写的**（`MainMenuScene.cs:2550-2554` 的夹具走 `DeckLibrary.Load().Create(...)` ⇒ `DeckLibrary.cs:246` ⇒ `SaveAll`） |
| **键会不会被省略** | ✅ **不会**。`daily.json` 里 **null** 的集合字段也照写 `"rows":null`（见 §2④） |

⇒ **可下的结论（不多说一格）**：同一个序列化器、同一种字段类型（`List<T>`）、**同一条写入路径**下，
**非 null 的空集合序列化成 `[]`** ⇒ 我这条新判据的前提**有很强的同类实据**，但**顶层 `decks` 恰好为空**这一具体情形
**本地仍然没有直接观测**（Unity 只在调度台手上）⇒ 「配套断言由另一份文件补」这条**原样保留**（成本一条：
「`SaveAll(0 套)` → `LoadAll` ⇒ 码 `None`、`LastLoadIssue = None`」）。

---

## 6. 顺手发现（**都没改**，报上来由调度台分流）

1. 🔴 **行尾的事实与简报的猜测相反**（详见 §4）：`DeckStore.cs` 是**纯 CRLF**（228/228 ⇒ 260/260）。
   风险面：谁按「LF」去 `sed -i`，整篇翻行尾。
2. 🔴 **我这趟改动挪了 `DeckStore.cs` 的行号 —— 下游所有 `DeckStore.cs:NNN` 的指针都得跟着走**：
   `:72` 之后 **+2**（`Empty = 2` 现在在 `:75`、`TermKeyOf` 在 `:100`）；
   `:136` 之后 **+32**（那条核心判据现在在 **`:168`**、`return dto.decks` 在 `:193`、`note = LoadNote.ReadFailed` 在 `:197`）。
   ⇒ **已失效的旧指针至少两处**（我**没改**，不是我的白名单）：
   `RuleEngineTest.cs:19524` 的注释里写着「`DeckStore.cs:55/64/90`」（那是**更早**一版的行号，本趟之前就已经不对）；
   `DA_DeckLibrary分类13红.md` §2/§3 全篇引的 `DeckStore.cs:136` / `:72-73` / `:98-108` / `:116-119`。
3. `DeckRulesTest.cs:476` 那句 **`note == null || !note.Contains("失败")`** 仍在（DA §6.5 已报）：`note` 现在是**诊断串**
   ⇒ `Contains("失败")` 那一半**已恒假**，只因前半 `note == null` 短路才没红 —— 同族「拿给人看的字当判据」的老副本。
   ⚠️ 另：该文件 `:227` 用的是 `LoadAll(out string note)` **兼容重载**（我在 §2 之外**没碰**这个文件，
   `DeckStore.cs` 里那条重载的注释提到它，未核实注释里的行号 `217/241/457` 是否还准）。
4. ✅ **同文件的兄弟夹具与新判据一致**（顺手核过，都**没改**）：
   `DeckScene.cs:4837` 写 `{ "version": 1, "current": 0 }`（缺键，**就是要它落 `Empty`**）；
   `DeckRulesTest.cs:470` 写的旧存档**带** `"decks":` 键（`"CardIds":["A"]`）⇒ 新判据**不影响**它 `:474` 的 `old.Count == 1`。
5. **`dto == null` 是否可达**（DA §5.2 也标了没查清）—— 我这趟也没查清，**没往下猜**；
   若可达它仍落同一个 `Empty` 支，改法覆盖它。

---

## 附：本趟用过的只读命令（可复核）

```bash
python -I -c "import io;b=io.open(r'd:/4/Unity/MyGame/Assets/RuleEngine/Data/DeckStore.cs','rb').read();print(b.count(b'\r\n'),b.count(b'\n'))"
TMPDIR=/tmp/wf_wd bash d:/4/Unity/工具/typecheck.sh
git -C d:/4 diff --numstat -- Unity/MyGame/Assets/RuleEngine/Data/DeckStore.cs
grep -rn '"decks": \[\]' d:/4/_tmp_view            # 0 命中
grep -o '"CardIds": \[\]' d:/4/_tmp_view/menu/_menu_boundary_decks.json   # 空集合写 []
head -c 400 d:/4/_tmp_view/daily_selftest_rewards/daily.json              # "rows":null（键不被省略）
```
