# B9_W56ALTT · 「异画必须在卡表里」接进卡组层 + 同刻改夹具 + 补负例

> 执行代理 `B9_W56ALTT` · 2026-10-27 · 白名单 2 个代码文件：
> `RuleEngine/Core/DeckRules.cs` · `CardPresentation/Deck/DeckEditorState.cs`（**一行都没越**）。
> 另写：本报告 · 账补丁 `账/_parts/B9_W56ALTT.tsv`（2 行，`战斗界面_HUD.tsv` 的 92/93 两条）。
>
> 必跑三件（全部在**最后一批改动落盘之后**跑）：
> 1. `TMPDIR=/tmp/wf_w56 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
> 2. `bash 工具/ruleprobe.sh check` ⇒ **rc=0**，解析输出指纹 **`c9a64798…` 不变**、解析差异 **0 行**
> 3. `bash 工具/ruleprobe.sh play check` ⇒ **rc=1**，**只有 `BL64` + `DA42` 两张**（都是别人的，**没有第三张**）

---

## 一、结论

「异画必须在卡表里」这条收紧**已经接进卡组层**（`PlayerDeck.ResolveEntry`，一行），
**夹具同刻改完并补了负例**（11 条 → **21 条断言 + 4 条前提硬检查**），⛔ 不是「改断言让它绿」。

| # | 项 | 改前 | 改后 |
|---|---|---|---|
| 1 | **卡组层的收紧** | `PlayerDeck.ResolveEntry` 只要「剥出原卡 id 能查到」就认 ⇒ **任意 `#HB_<合法 id>` 都冒充异画** | 剥出原卡后再判 `c.AlternateArtsKnown && !c.HasAlternateArt(id)` ⇒ 表里**没登记**的那一条 **返回 null**（= 查不到） |
| 2 | **一致性** | —— | 五个解析口（`Validate` / `RemoveInvalidCards` / `DeckBuilder.FromDeck`(+`LookupAny`) / `DeckLibrary.ExportString+ImportString` / 编辑器 `Find`）**全部共用这一个解析口** ⇒ 语义自动一致，没有第二处要改 |
| 3 | **纯字符串层** | `SameCardKey` / `AltArtBaseIdOf` / `IsAltArtEntry` 是 id 变换 | **一个字没改**（判据 = 原版 `CardDeck.CardCopiesInDeck` / `Has` 比的是 `GetOriginalId()`，**不查卡表**）⇒ 收紧只发生在 `ResolveEntry` 这一处；新增 ①b 断言把这条「刻意的不同档」钉住 |
| 4 | **自检夹具** | 正例用 `#HB_<同阵营单位卡>`（**卡表里没有非督军异画**）⇒ 一接就红 4 条 | 正例换成 **表里真有的** `#HB_SW1`；`#HB_<单位卡>` 改成**负例**（必须不被认 + 出声）；每条负例都配**对照**（②c/④c/⑤c 用普通卡，⑥e 用 `deckA` 基线，⑦c 用 ⑦ 的正向往返）防恒真/恒假桩 |
| 5 | **出声通道** | —— | 负例在四条通道各自出声：`Validate` → `UnknownCard` · `RemoveInvalidCards` → `removed[]`（`Why = UnknownCard`）· `FromDeck` → `Debug.LogError` + 不展开 · `ImportString` → `LastDroppedIds` |
| 6 | `HUD:92/93` 状态 | B | **仍 B**（不归本件：缺的是「入口」，见 §五·3） |

---

## 二、证据（文件:行号 · 行号 = 本件改完后的现读）

### 2.1 收紧本身（唯一改动的行为）

`RuleEngine/Core/DeckRules.cs:881` `PlayerDeck.ResolveEntry` —— **只改了一行**（`:890`）：

```csharp
c = lookup(b);
if (c == null || (c.AlternateArtsKnown && !c.HasAlternateArt(id))) return null;   // 收紧（2026-10-27）：**异画必须在卡表里**
```

- 判据**不在这儿新造**：`CardDef.AlternateArtsKnown` + `CardDef.HasAlternateArt`（W46 建在 `CardDef.cs` 类尾，本件没碰）。
- 三态照抄：表里**列了** ⇒ 认 · 表里**明确没有**（`Known == true` 且没列）⇒ **不认** · 表里**没有这一列**（旧表 / 夹具直接 `new CardDef`）⇒ **判不了 ⇒ 按认处置**。
- `AltArtBaseIdOf`（`:861`）· `SameCardKey`（`:871`）· `IsAltArtEntry`（`:853`）**一行没动** ⇒ 「一个收紧一个不收紧」这件事是**刻意的**，理由写在 `:878-880` 的 doc 与类尾那段注（原版比 `GetOriginalId()`、不查表）。
- **零行数变化**：`DeckRules.cs` 仍 **907** 行；账里的落点 `:853` / `:871` / `:881` / `:899` **全部原位**（`NormalizeEntryId` 仍在 `:899`）。

### 2.2 五个口自动一致（没有第二处要改）

| 口 | 位置 | 行为变化 |
|---|---|---|
| 校验 | `DeckRules.cs:456-465`（`Validate`） | 卡位/督军/防御卡位的没登记异画 ⇒ `UnknownCard` |
| 摘不合法卡 | `:414` / `:435`（`RemoveInvalidCards`） | 没登记的那条被摘，`RemovedCard.Why = UnknownCard` |
| 展开 | `DeckBuilder.cs:271`（督军）/ `:342`（卡位）/ `:402`（`LookupAny`，防御卡与旧 id 迁移） | 没登记的那条 ⇒ `Debug.LogError` + 不展开 |
| 分享串 | `DeckLibrary.cs:540`（`SplitByType` ← `NormalizeEntryId`） | 没登记的 `HB_…` ⇒ `dropped`（→ `LastDroppedIds`） |
| 编辑器查卡 | `DeckEditorState.cs:254`（`Find`） | 没登记的条目查不到 ⇒ 列表按「空」处置（不再冒充原卡） |

### 2.3 自检件重写（`CardPresentation/Deck/DeckEditorState.cs` · 类仍在 `:1113`）

`AltArtDeckSelfCheck.Run` —— 断言行号（改后现读）：

| 段 | 行 | 断言 |
|---|---|---|
| 守卫 | `:1128` | 卡池 / lookup 都不能为空（只在 null 时出） |
| 前提 | `:1143` | ①`SW1` 是督军且同阵营有单位卡 |
| 前提 | `:1150` | ②两个异画 id **都不在卡池里**（灭自证） |
| 前提 | **`:1156`** | **③（新）**卡表这一列**说了**、且 `#HB_SW1` **在表里登记**（正例的判据） |
| 前提 | **`:1163`** | **④（新）**`<单位卡>` 表里**明确没有**异画（负例的判据；缺列 ≠ 没有） |
| ① | `:1169` | 识别：`#` 条目认、普通 id 不认 |
| ①b | **`:1174`** | **（新）**`SameCardKey` / `AltArtBaseIdOf` 仍是纯字符串归一（**不查卡表**） |
| ② | `:1183` | **正例**：`#HB_SW1` ⇒ 原卡 `SW1` + `isAltArt = true` |
| ②b | **`:1187`** | **负例**：`#HB_<单位卡>` ⇒ **null**、`isAltArt = false`（带判别式「🧨 去掉收紧 ⇒ 本条变红」） |
| ②c | **`:1192`** | **对照**：普通 id 照旧解析、`isAltArt = false` |
| ③ | `:1201` | 份数：`SW1` + `#HB_SW1` = 同一张 ×2 |
| ③b | `:1205` | 删一张只少一份 |
| ④ | `:1212` | **正例**：`RemoveInvalidCards` 保留登记过的异画督军、`removed = 0` |
| ④b | **`:1218`** | **负例**：卡位里没登记的被摘、`Why = UnknownCard`（⛔ 不许静默留下） |
| ④c | **`:1225`** | **对照**：同一槽里的普通卡留着、`removed = 0` |
| ⑤ | `:1234` | **正例**：`Validate` 对异画督军与普通督军给同一错误码、都不是 `UnknownCard` |
| ⑤b | **`:1238`** | **负例**：`Validate` 判 `UnknownCard` |
| ⑤c | **`:1243`** | **对照**：同一副牌放普通卡 ⇒ 不是 `UnknownCard` |
| ⑥ | `:1258` | **正例**：带异画督军的卡组展开张数不变 |
| ⑥b | `:1260` | **正例**：异画督军展开成原卡（`unit ×2`） |
| ⑥c | `:1262` | **正例**：合法异画条目**不进 `skipped`** |
| ⑥d | `:1264` | **正例**：督军那格是原卡 |
| ⑥e | **`:1273`** | **负例**：没登记的卡位条目**不被展开**（张数比对照少 1；出声 = `Debug.LogError`） |
| ⑦ | `:1283` | **正例**：分享串往返，异画督军仍是异画（`WarlordId`） |
| ⑦b | **`:1289`** | **（新）**`ExportString` 写的是 `HB_…`（**不带 `#`**）—— 解一次原文验 |
| ⑦c | **`:1297`** | **负例**：导入串里没登记的 `HB_…` 被丢掉并记进 `LastDroppedIds` |

附带一处**夹具隐患**顺手排掉：`unit` 的选取加了 `!DeckRules.CannotAddToDeck(c)`（`:1138`）——
④c/⑤c 拿它当「**照旧合法**」的对照，若第一条同阵营单位卡恰是那 98 张 `inventoryOptions` 卡，对照会红。

---

## 三、没查清的部分

1. **自检件仍然没有被真跑过**（红线：本件不许跑 Unity 宿主）⇒ 这 **21 条断言首次运行在同步点**。
   我做了静态复核（三处最容易踩的）：
   - `FromDeck` 在 `DefensiveId` 为空时**会补一张防御卡**，而 `faction == null` 时 `PickRandomDefence` **不限阵营**、
     `rng` 恒 `new Random(0)` ⇒ 三次调用补的是**同一张** ⇒ `la/lb/lc` 的差值仍是 1（⑥e 的判据成立）。
   - `ImportString` 对「张数不足」的串会走**旧代双解**分支；两解都非法 ⇒ 返回**现代解**、`LastDroppedIds = modernDropped`
     （⑦/⑦c 的判据成立）。
   - 宿主（`Editor/DeckScene.cs:1351`）传的 `"SW1"` 与真卡池 ⇒ 前提③/④ 都成立（`SW1` 表里有、`ASH83` 那一类没有）。
2. **`CardIds` 里「合法的异画」今天没有真值**（7 张异画**全是督军**）⇒ 卡位/防御卡位的**正向**路径仍然验不到；
   本件只把它们的**负例**补齐（④b/⑤b/⑥e/⑦c），**没有造数据**（W46 §三·4 同一条边界）。
3. **短写法 `#SW1` 现在会被拒**：`AltArtBaseIdOf` 仍然认它（`#SW1 → SW1`），但 `HasAlternateArt("#SW1")` 为假
   （表里只有 `#HB_<id>`）⇒ `ResolveEntry("#SW1") = null`。仓内**没有任何生产路径**产出这种写法
   （`NormalizeEntryId` 只会产 `#HB_…`；UI 只喂普通 id），但这是收紧的**边界**，已写进 `:878-880` 的 doc。
   若将来要支持短写法，得另裁（⛔ 本件不自己发明）。
4. **没有跑 `check_ledger_points`**（该工具正在被 W52 改）⇒ 改用人工核对：`DeckRules.cs` 仍 907 行、
   `:881/:899` 原位；`DeckEditorState.cs` 只在 `:1113` **之后**增长（账里对它的最大引用就是 `:1113`）⇒ 无漂移。
5. `CardDatabase.ResolveDeckEntry` 那一层皮现在与 `ResolveEntry` **判据重复**（见 §五·1）—— 谁去收口要调度台裁。

---

## 四、改动清单

| 文件 | 改了什么 | 行号（改后） | 行数变化 |
|---|---|---|---|
| `RuleEngine/Core/DeckRules.cs` | `ResolveEntry` 收紧一行 + doc 就地改写（4 行→4 行） | `:878-880`（doc）· **`:890`**（收紧） | **907 → 907（不变）** |
| `CardPresentation/Deck/DeckEditorState.cs` | `AltArtDeckSelfCheck` 夹具重写：前提 2→4 条、断言 11→21 条（正例换 `#HB_SW1`、新增负例 4 条 + 对照 4 条 + ①b/⑦b）；`unit` 选取加 `!CannotAddToDeck`；类 doc 5 行→5 行 | 类 doc `:1107-1111` · 类 `:1113` · 断言 `:1143-1301` | 1228 → **1306（+78，全在 `:1113` 之后）** |
| `资料/完全复刻/账/_parts/B9_W56ALTT.tsv` | 账补丁：`战斗界面_HUD.tsv` 92（`CheckToBeAlternate`）/ 93（`CreateCard`）两行的 **落点 + 差异 + 备注** 追加本笔（状态/等级不动） | —— | 新增 3 行（表头 + 2 行，11 列） |
| `资料/完全复刻/报告/B9_W56ALTT_异画收紧接线.md` | 本报告 | —— | 新增 |

**没碰**：`RuleEngine/Core/CardDef.cs` · `RuleEngine/Data/CardDatabase.cs` · `CardPresentation/Editor/DeckScene.cs` ·
`Deck/DeckRuntime.cs` · `Data/DeckBuilder.cs` / `DeckLibrary.cs` · `Battle/BattleDriver.cs` · `工具/**` ·
`*.unity` / `ProjectSettings/` / `CLAUDE.md` / `项目任务.md` / 账主表。

---

## 五、顺手发现（不改，只报）

1. 🔴 **`CardDatabase.ResolveDeckEntry` 的收紧现在是重复逻辑**（W46 §四·1 预告过这件事）：
   它 = `ResolveEntry` + `if (Known && !HasAlternateArt) return null`，而前半段现在自己就会返回 null
   ⇒ `!altArt` 分支永远走不到那半句。按「判据只留一处」，建议调度台让它**退化成直接转发** `PlayerDeck.ResolveEntry`。
   ⛔ 本件没动那个文件（W46 名下、本波白名单外）。
2. **`Editor/DeckScene.cs` 两处已过期**（文件不在本件白名单：W46 已交付、本波别碰 ⇒ 只报不改）：
   - `:1344` 的注释还写着自检「**11 条**」，实际已是 **21 条 + 4 条前提**。
   - 🔴 `:1375` 那条**灭自证判别式已经失效**：它写着「🧨 去掉 `CardDatabase.ResolveDeckEntry` 里那条收紧
     ⇒ 后半变真 ⇒ 本条红」。现在收紧已经在 `PlayerDeck.ResolveEntry` 里 ⇒ **只删 `ResolveDeckEntry` 那半句，断言仍然是绿的**。
     正确的判别式应改成「去掉 `PlayerDeck.ResolveEntry` 里那条收紧」（本件自检 ②b 的判别式就是这么写的）。
     ⚠️ 这不是「绿灯＝对」的问题（断言本身仍然在验行为），是**它自带的那个反证实验已经做不出来了**。
3. `Deck/DeckRuntime.cs:4237` `IsAltArtCard` 仍查**硬编码 7 条** `CollectionWindow.AltArtCards`，
   而卡表现在有 `CardDef.HasAlternateArt` —— **两处写同一条规则**（W46 §五·2 已记，W51 名下）。本件没碰。
4. **两条「出声」通道不同形**：`ImportString` 把认不出的 id 记进 **`LastDroppedIds`**（可读、可展示），
   而 `DeckBuilder.FromDeck` 只 `Debug.LogError` **不进 `skipped`** ⇒ UI 若要「告诉玩家哪几张被丢」，
   走 FromDeck 那条路今天拿不到清单。`DeckBuilder.cs` 不在本件白名单 ⇒ 只记一笔。
5. 本件**没有**发现新的 `play check` 差异：收紧只在 `DeckRules`/卡组层，探针 `play` 路径不经过
   `PlayerDeck`（`工具/ruleprobe/**` 里 `ResolveEntry` grep 0 命中）⇒ `BL64` + `DA42` 两张仍是别人的。

---

## 六、夹具改前 / 改后对照（为什么换掉、换成了什么）

**根因**：改前夹具的正例是 `#HB_<同阵营单位卡>`（`wlAlt` + `unitAlt` 两条），而 `cards_engine.json`
的 `alternateArts` 只有 **7 条、全是督军** ⇒ 收紧之后 `unitAlt` 必然解析成 null ⇒ ②④⑥⑦ 会红。
⇒ 正确处理是**把正例换成表里真有的那一条**，同时把 `#HB_<单位卡>` **升级成负例**（它本来就是「非法输入」）。

| 段 | 改前（夹具 / 断言） | 改后 | 为什么 |
|---|---|---|---|
| 前提 | 2 条（督军+单位卡 / 两个 id 不在池里） | **4 条**（+③正例必须在表里 · +④负例必须「明确没有」） | 收紧之后「正例真不真」「负例够不够格」都成了判据的一部分；缺列（判不了）**不能**当负例 |
| ① | 识别 `wlAlt`/`unitAlt` | 原样（只改措辞为「条目」） | 纯字符串层没变 |
| ①b | —— | **新增**：`SameCardKey`/`AltArtBaseIdOf` 不查卡表 | 把「收紧只在 ResolveEntry」这条**刻意的不同档**钉死，防以后有人顺手把它们也收紧 |
| ② | **一条**：`wlAlt` 与 `unitAlt` **都**解析回原卡 + `isAltArt` | **三条**：②正例（`#HB_SW1`）· **②b 负例（`unitAlt` ⇒ null）** · ②c 对照（普通 id） | 改前这条**正是**「任意 `#HB_` 都认」的写法；现在它必须一正一负拆开，各配对照 |
| ③ / ③b | 份数用 `unit.Id` + `unitAlt` | 改用 `wl.Id` + `wlAlt` | 份数口径是**纯字符串**规则 ⇒ 正例要用**合法**条目，不能拿一条现在会被判非法的条目当正例（否则测的是一个不存在的状态） |
| ④ | **一条**：卡位 `unitAlt` 被保留 | **三条**：④正例（**督军位** `wlAlt` 保留）· **④b 负例（卡位 `unitAlt` 被摘 + `Why=UnknownCard`）** · ④c 对照（普通卡保留） | 「卡位保留异画」今天**无真值**（没有非督军异画）⇒ 正例只能搬去督军位；卡位改成负例，并配对照防「一律摘光」的桩 |
| ⑤ | **一条**：`Validate` 异画督军 vs 普通督军同错码 | **三条**：⑤正例 · **⑤b 负例（卡位 `unitAlt` ⇒ `UnknownCard`）** · ⑤c 对照（普通卡 ≠ `UnknownCard`） | 同上：正例只能走督军；负例是这个收紧在**校验**那条通道上的出声 |
| ⑥ | **四条**，`deckB` = 督军 `wlAlt` + 卡位 `{unitAlt, unit.Id}` | **五条**：⑥/⑥b/⑥c/⑥d 正例（`deckB` 卡位改 `{unit.Id, unit.Id}`，只留异画督军）· **⑥e 负例**（`deckC` 卡位 `{unitAlt, unit.Id}` ⇒ 张数比对照少 1） | `FromDeck` 的正例同样只能靠督军位；卡位那条改成「没登记的不展开」 |
| ⑦ | **一条**：卡位异画经分享串往返仍是异画 | **三条**：⑦正例（**督军位**往返）· **⑦b 新**（分享串写的是 `HB_…`、**不带 `#`**）· **⑦c 负例**（没登记的 `HB_…` ⇒ 落 `LastDroppedIds`） | 同上；⑦b 顺手把 `RawEntry` 剥 `#` 那条规则钉住（之前一条断言都没有） |
| 夹具选卡 | 第一条同阵营 `unit` | **加 `!CannotAddToDeck(c)`** | ④c/⑤c 拿它当「照旧合法」的对照，不能选中那 98 张 `inventoryOptions` 卡 |
| 总数 | 11 条断言（+2 前提） | **21 条断言（+4 前提）** | 正例 10 · 负例 5 · 对照 3（另 3 条属识别/字符串层/分享串写法） |

**一句话**：被换掉的**不是断言的方向**，而是**夹具的取值**——改前用一条**卡表里不存在**的异画当正例，
那正是这条收紧要否掉的东西；改后正例取表里真有的 `#HB_SW1`，那条不存在的**降级成负例并配对照**。
