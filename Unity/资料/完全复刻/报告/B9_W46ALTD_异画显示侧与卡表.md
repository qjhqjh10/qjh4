# B9_W46ALTD · 异画显示侧两处「少算 1」· 卡表 `AlternateArts` 读侧 + 收紧判据 · 自检件接线

> 执行代理 `B9_W46ALTD` · 2026-10-27 · 白名单 5 文件（`Shell/DeckInfoPopup.cs` · `Core/CostCurveDrawer.cs` ·
> `RuleEngine/Core/CardDef.cs` · `RuleEngine/Data/CardDatabase.cs` · `Editor/DeckScene.cs`），**一行都没越**。
> 类型检查 `TMPDIR=/tmp/wf_w46 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（最后一次在所有改动落盘之后跑）。
> 引擎对拍：`bash 工具/ruleprobe.sh check` ⇒ **rc=0**（全池 1121 张 · 解析差异 0 行 —— 生成器与池指纹一字节没动）。
> `bash 工具/ruleprobe.sh play check` ⇒ **rc=1 / 2 张卡变了**（`BL64` 已知 + **`DA42` 归 W43 在飞的 `EffectResolver.cs`**，归因见 §二·4）。

---

## 一、结论

上一路 `B9_W31ALTART` 留下的**显示侧小口**与**卡表收紧**三件**都做完了**；但 `HUD:92/93` **仍是 B** ——
它转 A 缺的是 `Battle/BattleDriver.cs` 那一半（W40/W29），不是这三件。

| # | 项 | 改前 | 改后 |
|---|---|---|---|
| 1 | 卡组详情窗的**督军位**（`DeckInfoPopup.CurWarlord`） | 异画督军 `#HB_SW1` 裸查 `CollectionData.Card(id)` ⇒ **null ⇒ 立绘位空着** | ✅ 解析回 `SW1`（`CardDatabase.ResolveDeckEntry`） |
| 1 | 卡组详情窗的**卡表行**（`BuildDeckList`） | 异画那一条 `continue` 掉 ⇒ **少一行**，份数 `xN` 也少 1 | ✅ 正常一行 + 份数正确 |
| 1 | **费用曲线**（`CostCurveDrawer.Counts`） | 同上 ⇒ **少 1 张**（`PracticeModePopup` 那一扇**同一处修好**，它共用这个 `Counts`） | ✅ 记进对应费用行 |
| 1➕ | **防御卡位**（`DeckInfoPopup:1821`，W31 的请求里**没有列**） | 同一形状的第三处裸查 | ✅ 顺手一并改掉（铁律 14：判据已查清 / 在同一文件 / 当场 typecheck 过） |
| 2 | 卡表**读侧** | `cards_engine.json.alternateArts` **0 个消费者**（生成器注释自己写着「目前没有消费者」） | ✅ `CardDef.AlternateArts` / `AlternateArtsKnown` / `HasAlternateArt`；`CardDatabase.CardDto.alternateArts` + `Parse` 构造后挂载 |
| 2 | 「任意 `#HB_<合法 id>` 都接受」⇒「**必须在表里**」 | 判不了（没有表） | ✅ **判据建好并接在显示侧**：`#HB_<没异画的卡>` 现在**解析成 null**（不再冒充异画）。⚠️ **卡组校验那一侧（`PlayerDeck.ResolveEntry`）没有改** —— 那个文件不在本件白名单（当时归 W6DECK，现已合并、文件已空出）⇒ 一行补丁见 §四「改动请求 ①」，**但接之前必须先处理它和 W31 自检夹具的冲突**（§三·2） |
| 3 | 自检件 `AltArtDeckSelfCheck`（11 条） | **写了但没人调** ⇒ 一条都没跑过 | ✅ `DeckScene.RunCore` 里接上（`CheckTrue` 作 check 委托），并**另加 4 条**覆盖第 2 项（两个方向各一条，挡恒真/恒假桩） |
| — | `HUD:92/93` 状态 | B | **仍 B**（`BattleDriver.CollectAltArtCopies` 只扫 `CardIds`、7 张异画全是督军 ⇒ 永不触发；那是 W40 的 `BattleDriver.cs`） |

---

## 二、证据（文件:行号 · 行号 = 本件改完后的现读）

### 2.1 显示侧：三处裸查 ⇒ 异画条目查不到（「少算 1」）

判据链（为什么 `#HB_SW1` 会查不到）→ `RuleEngine/Core/DeckRules.cs` 里 `PlayerDeck` 类尾那段注（W31 写，本件没动）；
本件只把「裸查」换成 W31 已经建好的那个口 + 本件新建的收紧皮（`CardDatabase.ResolveDeckEntry`）。

| 位置（改后） | 改前 | 改后 | 症状 |
|---|---|---|---|
| `CardPresentation/Shell/DeckInfoPopup.cs:924`（`CurWarlord`） | `CollectionData.Card(id)` | `CardDatabase.ResolveDeckEntry(id, CollectionData.Card)` | 异画督军 ⇒ 督军立绘/名字位**空着** |
| `CardPresentation/Shell/DeckInfoPopup.cs:1805`（`BuildDeckList` 的逐条） | 同上 | 同上 | 异画那一条被 `continue` ⇒ **少一行**、`xN` 少 1 |
| `CardPresentation/Shell/DeckInfoPopup.cs:1821`（防御卡并进列表） | 同上 | 同上 | 异画防御卡 ⇒ 那一行不画（W31 请求里**漏了这处**，本件补齐） |
| `CardPresentation/Core/CostCurveDrawer.cs:113`（`Counts` 的逐条） | `find(id)` | `CardDatabase.ResolveDeckEntry(id, find)` | **费用曲线少 1 张**（两扇窗共用：`DeckInfoPopup:2162` / `PracticeModePopup:950`） |

> **零行数变化**：四处都是「整行替换」，`DeckInfoPopup.cs` 仍是 **2974** 行、`CostCurveDrawer.cs` 仍是 **502** 行 ⇒
> 这两个文件**不制造账漂移**（下面 §四 的漂移全部来自 `CardDef` / `CardDatabase` / `DeckScene`）。

### 2.2 卡表那一列（**只改读侧**；生成器一字节没动）

数据现读（`RuleEngine/Resources/cards_engine.json`，v34）：

| 事实 | 读数 |
|---|---|
| `alternateArts` 键的覆盖 | **1128 / 1128 行都有**、`null` **0** 行 ⇒ 全池 `AlternateArtsKnown == true` |
| 非空的行 | **7 行**：`AM2` / `BL34` / `DA1` / `SAU35` / `GOF28` / `SW1` / `UM76`，值形如 `["#HB_SW1"]` |
| 7 张的卡型 | **全是 `hero`** ⇒ 今天只可能住在 `PlayerDeck.WarlordId`，`CardIds` 里不会有「合法的」异画条目 |
| 生成器契约 | `工具/gen_cards_engine.py:1193`：**`[]` = 这张卡没有异画 · `null` = 整列缺席**（⛔ 两者不许混） |

改动（三处，逐条）：

1. `RuleEngine/Core/CardDef.cs:6685` `AlternateArts` · `:6690` `AlternateArtsKnown` · `:6695` `AttachAlternateArts` ·
   `:6713` `HasAlternateArt(string)`，判据全文写在 `:6655-6680` 那段注里。
   🔴 **为什么是「构造后挂载」而不是构造参数**：`CardDef` 的构造函数下面有 **40 处账落点按行号引用**
   ⇒ 加构造参数会让它们整体漂移。先例 = 同文件类尾的 `AttachCardTraits` / `AttachKeywordId` /
   `AttachEnchantTrait` / `AttachCardType`（`CardDatabase.Parse` 里 `:1635-1663` 那段 v31 注已经把这条理由写死）。
   本件把新成员**全部追加在 `CardDef` 类尾**（`:6655` 起）⇒ **`CardDef.cs` 零账漂移**（该文件最大的账落点是 `:6552`）。
2. `RuleEngine/Data/CardDatabase.cs:72` `CardDto.alternateArts`（`string[]`，`null`/`[]` 三态写在 doc 里）·
   `:1677` `def.AttachAlternateArts(c.alternateArts);`（与 `AttachCardType` 同一段、同样「构造后挂载」）。
3. `RuleEngine/Data/CardDatabase.cs:3303` **`ResolveDeckEntry(id, lookup)`** = 吸收紧的**唯一**皮：
   `PlayerDeck.ResolveEntry` 查卡 → 若是异画条目且 `AlternateArtsKnown && !HasAlternateArt(id)` ⇒ **返回 null**。
   ⚠️ 剥前缀/份数口径**仍只有 `PlayerDeck` 那一处实现**（本方法只是「拿这里的 `lookup` 去查 + 按卡表收紧」），
   ⛔ 没有另写一套（`HasAlternateArt` 里只去一个前导 `#`，比的是**整条克隆 id**，与 `AltArtBaseIdOf` 那个「取最后一个 `_` 之后」**不是同一个变换**，doc 里已写明）。

**收紧口径（三态，写进 `CardDef` 类尾注 + `ResolveDeckEntry` 的 doc）**：
表里**列了** ⇒ 认；表里**明确没有**（`Known == true` 且没列）⇒ **不认**；表里**没有这一列**（旧表 / 夹具直接 `new CardDef(...)`）
⇒ **判不了 ⇒ 按认处置**（⛔ 不许把「缺列」静默读成「没有异画」—— 那会把全池异画判死）。

### 2.3 自检件接线（W31 写好、一条没跑过）

`CardPresentation/Editor/DeckScene.cs:1348` 新起一节 `Section("状态：异画副本进卡组（AltArtDeckSelfCheck · 账 HUD:92/HUD:93）")`：

- `:1351` `AltArtDeckSelfCheck.Run(...)` —— `CheckTrue` 的签名是 `(bool,string)`，而它要 `Action<string,bool,string>`
  ⇒ 用一个**适配 lambda**（`tag` 与 `detail` 拼成一句，`detail` 空就只印 `tag`）。
- `:1358-1375` **另加 4 条**（本件第 2 项否则**一条断言都没有**）：
  1. （前提·硬检查）卡池里有 `SW1`（表里列了异画）与 `ASH83`（表里没有）—— 两条反向断言都靠它；
  2. `SW1`：`AlternateArtsKnown && HasAlternateArt("#HB_SW1") && HasAlternateArt("HB_SW1")`（**两种写法都认**）；
  3. `ASH83`：`AlternateArtsKnown && !HasAlternateArt("#HB_ASH83")`（⚠️ `Known` 必须同时为真：**缺列 ≠ 没有**）；
  4. **收紧生效**：`ResolveDeckEntry("#HB_SW1") != null` **且** `ResolveDeckEntry("#HB_ASH83") == null`。
  🔴 **2/3/4 是灭自证的一对**：只断「有异画的认得出」挡不住**恒真桩**，只断「没异画的认不出」挡不住**恒假桩**
  ⇒ 三条合起来不可能被一个桩同时满足（第 4 条里那句「🧨 去掉那条收紧 ⇒ 后半变真 ⇒ 本条红」就是给它的判别式）。
- ⛔ 本件**没有跑** `DeckScene.Run`（红线：宿主会重建场景）⇒ 这些断言**首次运行在同步点**，见 §三·1。

### 2.4 `play check` 的那 2 张卡 —— 逐张归因（**没有一张是本件**）

- `BL64`（`playRc 18 → 0` 等）：W31 报告 §2.5 已记的**已知真偏离**（`cond=341` 被 W27 修好、基线未重钉）。
- `DA42 Reconnaissance Mission`（tactic，`Choose a card in your opponent's hand.`）：差异 = 基线里
  `牌库 P0 +「Smothering Decree」×1` 这一笔**我们这边没有**，其余三笔（能量 −1 / 手牌丢 `DA42` / 弃牌 +`DA42`）**逐字相同**
  ⇒ 差的是**「从对手手牌挑一张 → 那张牌的去向」**那一跳，坐在 `EffectResolver` 的 choice/hand 路径上。
  **归因证据**：`MyGame/Assets/RuleEngine/Core/EffectResolver.cs` 的 mtime = **19:08:21**、`RuleCore.cs` = **19:05:06**
  （本件第一次对拍是 **19:07**、复跑 19:11 —— `EffectResolver.cs` 在两次运行之间**又变过一次**）⇒ 有代理（W43）
  正在写这两个文件；而本件 diff 是**纯增量且无引用**：`HasAlternateArt` / `ResolveDeckEntry` 在 `工具/ruleprobe/**` 与
  `RuleEngine/Core/{RuleCore,EffectResolver,ConditionEval,TargetCriteriaEval}.cs` 里 **grep 命中 0**，
  探针的 `play` 路径也**不经过** `PlayerDeck` / `DeckRules`（W31 报告 §2.5 已给过同一条反证）。
  ⇒ **本件没有加新差异**；这 2 张的红等同步点重钉基线。

---

## 三、没查清的部分

1. **自检件本体从未被任何宿主跑过**（W31 没接、本件不许跑 Unity）⇒ 首次运行是否 11 条全绿**未验**。
   我做了静态复核（最可能出问题的两条）：⑥ `FromDeck` 的督军那一格走 `PlayerDeck.ResolveEntry`、防御卡与卡位走
   `DeckBuilder.LookupAny`（`DeckBuilder.cs:267/290` 都是异画感知）；⑦ `ExportString` → `ImportString` 走
   `DeckLibrary.SplitByType` 的 `NormalizeEntryId` 归一。**两条都读得通，但没跑。**
   ⇒ 若首跑出红，**先怀疑夹具前提**（它依赖「`SW1` 是督军且同阵营有单位卡」+「`#HB_*` 不在池里」两条前提，两条都由它自己硬检查并出声）。
2. 🔴 **卡组层那条收紧与 W31 自检夹具直接冲突**（本件没接，也不该由本件偷偷接）：
   W31 的 ②③④⑥⑦ 用的夹具是 **`#HB_<同阵营单位卡>`**（`DeckEditorState.cs:1147`），而**表里没有非督军的异画**
   ⇒ 一旦把「必须在表里」接进 `PlayerDeck.ResolveEntry`，**这 5 条里至少 ②④⑥⑦ 会立刻变红**。
   ⇒ 谁去接那条，**必须同刻把夹具改成「表里真有的那一条」（即 `#HB_SW1`）+ 另补一条『没登记的 `#HB_<单位卡>` 被拒』的负例**，
   ⛔ 不许只改代码让它变绿（那是自证）。
3. **原版卡组编辑器里「异画副本」是不是单独一行**（能不能与普通那张同存一副牌）：W31 §三·4 记的没查清，本件**没动**。
4. **非督军异画 / 异画防御卡的正向路径今天无真值**（7 张全是督军）⇒ 本件那 4 条断言只覆盖
   「督军位那一条**认**」+「没登记的**不认**」，卡位/防御卡位上的「真有一条合法异画」**验不到**（数据不存在，⛔ 不造）。
5. **分享串里的克隆 id（`HB_SW1`）仍是两条反汇编推出来的**（W31 §三·1），没有一条真的卡组串可读 ⇒
   `HasAlternateArt` 之所以「两种写法都认」，正是为了不把这条未定的写法变成硬判据（doc 已写明）。
6. 本件**没有**验证「`JsonUtility` 对 `[]` 给出空数组而非 `null`」在**真机**上的行为（只按 Unity 文档语义 + 生成器契约实现）；
   但 `cards_engine.json` 现读 **`null` 值 0 行**、键覆盖 1128/1128 ⇒ 即便哪天语义有出入，也只影响「旧表」那一档（判不了 ⇒ 认）。

---

## 四、改动清单（5 个代码文件 + 本报告；**没有账补丁**）

| 文件 | 改了什么 | 行号（改后） | 行数变化 |
|---|---|---|---|
| `RuleEngine/Core/CardDef.cs` | 类尾追加：`AlternateArts` / `AlternateArtsKnown` / `AttachAlternateArts` / `HasAlternateArt` + 判据整段注（含三态与「构造后挂载」的理由） | `:6655-6680`（注）· `:6685` · `:6690` · `:6695` · `:6713` | 8740 → **8812**（+72，**全在类尾 ⇒ 零漂移**） |
| `RuleEngine/Data/CardDatabase.cs` | ① `CardDto.alternateArts`；② `Parse` 里 `def.AttachAlternateArts(...)`；③ 新增 `ResolveDeckEntry`（收紧皮） | `:72` · `:1677` · `:3303` | 3305 → **3346**（+41） |
| `CardPresentation/Shell/DeckInfoPopup.cs` | 三处裸查换 `CardDatabase.ResolveDeckEntry`（督军位 / 卡表行 / 防御卡），两处 doc 就地改写 | `:918`（doc）· `:924` · `:1805` · `:1821` | **2974 → 2974（不变）** |
| `CardPresentation/Core/CostCurveDrawer.cs` | 费用曲线逐条换 `CardDatabase.ResolveDeckEntry`；doc 就地改写 | `:100`（doc）· `:113` | **502 → 502（不变）** |
| `CardPresentation/Editor/DeckScene.cs` | 新起一节接 `AltArtDeckSelfCheck.Run(...)` + 另加 4 条卡表读侧/收紧断言 | `:1342-1377`（`Section` 在 `:1348`、`Run` 在 `:1351`、4 条新断言 `:1358-1375`） | 10294 → **10331**（+37） |
| `资料/完全复刻/报告/B9_W46ALTD_异画显示侧与卡表.md` | 本报告 | —— | 新增 |

**没碰**：`工具/gen_cards_engine.py`（**一字节没动** ⇒ 池/基线指纹不变，`ruleprobe check` 0 差异可反证）·
`RuleEngine/Core/DeckRules.cs` · `RuleEngine/Data/DeckBuilder.cs` / `DeckLibrary.cs` · `Deck/DeckEditorState.cs` ·
`Deck/DeckRuntime.cs` · `Battle/BattleDriver.cs` · `Shell/CollectionWindow.cs` · 任何 `*.unity` / `ProjectSettings/` /
`CLAUDE.md` / `项目任务.md` / 任务总表 / 账主表 / 基线文件。

### 账漂移（本件制造的，逐条列清；同步点 `--fix` 或按右列直接改）

`CardDef.cs` = **0 条**（新成员全在类尾，最大的账落点 `:6552` 在它上面）。

| 账文件 · 行 | 符号 | 旧落点 | 新落点 | 增量 |
|---|---|---|---|---|
| `其他_基础设施.tsv` 346 / 347 / 426 | `AllExpansionCards` / `AllLegionCards` / `LoadAllCards` | `RuleEngine/Data/CardDatabase.cs:1040-1070` | 同文件 **`:1048-1078`** | **+8**（`CardDto.alternateArts` 那 8 行在它上面） |
| `战斗逻辑驱动.tsv` 1300 | `SupportMethods.GetCardListFromIds` | `RuleEngine/Data/CardDatabase.cs:3207`（`DeckLookup`） | **`:3273`** | **+13**（+8 上面那笔，+5 `AttachAlternateArts` 那一笔） |
| `卡组编辑.tsv` 8 | —— | `Editor/DeckScene.cs:2712` | **`:2749`** | **+37** |
| `卡组编辑.tsv` 244 | —— | `Editor/DeckScene.cs:1401` | **`:1438`** | **+37** |
| `战斗逻辑驱动.tsv` 1323 | —— | `Editor/DeckScene.cs:2281` | **`:2318`** | **+37** |
| `泛型与启动链_补读.tsv` 60 / 67 / 69 / 138 / 139 | —— | `Editor/DeckScene.cs:2111 / 1833 / 1838 / 2048 / 2048` | **`:2148 / 1870 / 1875 / 2085 / 2085`** | **+37** |
| `菜单_外壳.tsv` 120 / 123 | —— | `Editor/DeckScene.cs:2340 / 2384` | **`:2377 / 2421`** | **+37** |

（`DeckScene.cs` 那 10 个点已逐条**新号回读**过一遍，落点内容与旧号对应得上；`1401`/`2281`/`2111`/`2048` 等仍在原语义位置。）

### 改动请求（都在本件白名单外）

1. 🔴 **把「必须在表里」接进 `RuleEngine/Core/DeckRules.cs` 的 `PlayerDeck.ResolveEntry`**（`DeckRules.cs:889-892` 那两行，
   一行改法）：
   ```csharp
   c = lookup(b);
   if (c == null || (c.AlternateArtsKnown && !c.HasAlternateArt(id))) return null;   // 收紧：必须在卡表里
   ```
   **⚠️ 接之前必须先做 §三·2 那件事**（W31 自检的 4 条夹具用的是「表里没有的 `#HB_<单位卡>`」，接了就会红）
   —— **改夹具 + 补负例**，⛔ 不许只改代码让它变绿。
   接完之后：`CardDatabase.ResolveDeckEntry` 里那半句收紧就变成**重复**了（届时可退化成直接转发 `PlayerDeck.ResolveEntry`，
   由调度台按「判据只留一处」裁决 —— 本件**不自己裁**，因为两个文件当时分属不同写手）。
2. **`HUD:92/93` 转 A 的关键仍在 `Battle/BattleDriver.cs`**（W31 报告 §六·2 已写全：`CollectAltArtCopies` 要加扫
   `WarlordId` / `DefensiveId`；4 处裸查 `WarlordId` 换 `PlayerDeck.SameCardKey(...)`）。本件**一行没碰**那个文件。

---

## 五、顺手发现（不改，只报）

1. ✅ **已顺手做掉的**：`Shell/DeckInfoPopup.cs:1821` 的防御卡裸查 —— W31 的改动请求只列了 `:922-924` / `:1801-1806` 两处，
   **防御卡是同一形状的第三处**（`#HB_<defence>` 会让防御卡那一行不画）。判据同前两处，且在同一文件 ⇒ 按铁律 14 当批做掉。
2. 🔴 **「这张卡有没有异画」今天有**两张表**：`Deck/DeckRuntime.cs:4237` 的 `IsAltArtCard(CardDef)` 读的是
   `CollectionWindow.AltArtCards`（`Shell/CollectionWindow.cs:2208-2217`，**硬编码 7 条**），而卡表现在有了
   `CardDef.HasAlternateArt`。两处写同一条规则 ⇒ 迟早不一致（例如表里新增一条时只改一边，卡名的 `*` 与立绘就会打架）。
   建议派给 `Deck/DeckRuntime.cs` 的写手（本波是 `W34FEED` 那一族）改成读 `CardDef`，并留一条「两表逐条相等」的断言。**本件没动**（不在白名单）。
3. `CardDatabase.DeckLookup`（`:3273`）返回的 `Func<string,CardDef>` **不是**异画感知的（裸 `IdIndex` + 卡名），
   它的调用方若拿它去解异画条目会静默查不到。今天它的调用点都在「旧存档按 id/卡名解」那一侧（不需要异画），
   所以**无害**，只是记一笔：**要异画就用 `ResolveDeckEntry`**（本件新建），两者别混用。
4. `play check` 的 `DA42` 是本波**新出现**的差异（W31 那时只有 `BL64`），归因见 §二·4 —— 同步点重钉基线时请把这条的归属记在
   `EffectResolver` 那位写手名下，别记成本件的。
5. 本波文件所有权表里 `B9_W6DECK`（`调度台日志:5734`）也列过 `Core/CostCurveDrawer.cs` / `Editor/DeckScene.cs`，
   但它的回执 `12·6` 已写「✅ 已合并」⇒ 本件动这两个文件**没有撞车**（现读 `CostCurveDrawer.cs` 502 行、
   `DeckScene.cs` 的 `TestItemDrawerDeckClick` 那段仍在位）。记一笔备查。
