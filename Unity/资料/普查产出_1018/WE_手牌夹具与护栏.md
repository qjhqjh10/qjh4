# WE · 手牌效果夹具 6 红 + DeckStore 护栏

> 写手 `WE`。**只动了两个白名单文件**：`RuleEngine/Editor/RuleEngineTest.cs` · `RuleEngine/Editor/DeckRulesTest.cs`（后者**只加注释**）。
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改 `RuleEngine/Core/*` · `RuleEngine/Data/*` · `Editor/DeckScene.cs`。
> 报告里的行号 = **改完之后的当前行号**（本次改动会让后面的行号整体下移，别拿旧行号去对）。

---

## 1. 改了什么（逐处）

### 1·1 任务①：修那 6 条红 —— **4 个调用点**（3 处夹具），引擎一行没动

| # | 文件:行号 | 改前 | 改后 | 一句话理由 |
|---|---|---|---|---|
| a | `RuleEngineTest.cs:3878-3895`（④ 块首，**新增**） | 无 | 新增局部 `var unitCrit = new EffectTargetSpec { Raw="(夹具：手牌里的部队)", Side="own", Kind="unit", Count=0, Auto=true };` + 理由注释 | 给夹具一条**判得出**的 criteria，形状照真生产者 `GrantHandBuff` 的 `HandTroopCriteria`（`Core/EffectResolver.cs:2764-2767`） |
| b | `RuleEngineTest.cs:3898-3901`（④ 的**两个**调用点） | `…{ same[0] });` ×2 —— 第 7 实参走默认 `criteria = null` | `…{ same[0] }, unitCrit);` ×2 | 同上（`replace_all` 一次改两处，两处**都要**改，少一处就剩一条红） |
| c | `RuleEngineTest.cs:3965-3971`（⑤，1 个调用点） | `…0, hosts);` | 新增同名局部 `unitCrit` + `…0, hosts, unitCrit);` | 造出来的 `FixtureMadeUnit` 是 `unit` ⇒ `Kind="unit"` 命中 |
| d | `RuleEngineTest.cs:4009-4020`（⑥，1 个调用点） | `…{ ca });` | 新增同名局部 `unitCrit` + `…{ ca }, unitCrit);` | 载体打出去后抽上来的那张，牌库里恒是垫牌 `Unit(...)`（本文件 `Deck()` 的 `filler*`）⇒ 命中 |

* 三处 `unitCrit` 写在**三个各自独立的 `{}` 块**里，同名不冲突（C# 块作用域）。
* **为什么这样改就够**（判据链）：
  原版 `PlayerHand__SetupCardInHand.c:58` = `FilterMethods.CheckIfMeetsCriteria(card, handEffect.targetCriteria)`；
  `FilterMethods__CheckIfMeetsCriteria.c` **头两句** = `if (param_4 == 0) { CustomDebug.LogError(…); return 0; }`（**criteria 缺失 ⇒ 拒**）、
  `if (*(int *)(param_4 + 0x10) == 0) return 1;`（空 criteria ⇒ 全放行）。
  我们这一侧 `Core/RuleCore.cs` 的 `HandEffectFits`：`if (crit == null && !hasSubtype) { loose = true; return false; }`
  （2026-10-18 `W5` 把「按单位卡放行」收窄成「不放行」，忠于原版 `param_4 == 0` 那一支）。
  ⇒ 夹具旧写法（`null`）必被拒 ⇒ **要动的是夹具**，不是那道闸。
* **零玩法影响**：真对局里四个生产者**全部**传 spec（`DB` §5.2 逐条核过）⇒ `Target == null` **只可能**由自检直接调公开入口的默认值产生。
* **兑现那一段不受 criteria 影响**：`RuleCore.ApplyHandBuffs`（`Core/RuleCore.cs:3364-3399`）遍历的是 `inst.HandEffects`、**不读 `e.Target`** ⇒ ④ 那条「2 → 4 攻」照旧成立。

### 1·2 任务①尾：`:3895` 那条**假绿**（现 `:3913-3927`）

* **改前为什么恒 0**：三档 criteria 一起判不出来 ⇒ `HandEffectFits` 一律 `return false`（`crit==null && !hasSubtype`）
  ⇒ `SetupCardInHand` 在**任何**输入上都返回 0 ⇒ 它同时容得下「幂等」与「整条消费者链一次都没工作」。
* **改后能区分哪两种状态**：它现在**首次真的**在判幂等 ——
  * 正确实现：上一句「后进手牌那份**补到 2 条**」（`:3906-3910`）实测 `== 2`（= 消费者确实工作过），
    再喂同一张 ⇒ 两条记录都已被 `RecordId` 认出 ⇒ `AttachHandEffectCopy` 两次都返回 false ⇒ **0**（绿）；
  * **幂等坏了**（把 `EffectResolver.cs:3048-3049` 那两句按 `RecordId` 去重删掉）⇒ 再挂两条 ⇒ **实得 2 ⇒ 红**；
  * **消费者恒 0**（`SetupCardInHand` 早退）⇒ 上一句实得 0 ⇒ **它红**（所以这一条**必须与上一句成对**看）。
* 我只加了注释与断言文案里的判别式说明，**断言的比较值一个字没动**（还是 `0`）。断言文案里明写了「单看它容得下恒 0」。

### 1·3 任务②：新护栏 `②-3`（`RuleEngineTest.cs:19795-19832`）

* 落点 = `DeckLibrary` 那一节 `try` 块里、②-2「没有存档 ⇒ `NoSaveFile`」之后、坏档夹具之前。
* 新鲜夹具：`SaveAll(new List<PlayerDeck>(), 0, out wErr)` ⇒ 读原文（**只 `Debug.Log`，不断言**）⇒ `DeckLibrary.Load()`。
* 五条断言：`wrote == true`（前提）· 文件**在**（前提）· `Count == 0` · **`LastLoadCode == LoadNote.None`** · **`LastLoadIssue == DeckLoadIssue.None`** · `LastLoadIssueTerm == null`。
* **它为什么「改前改后都绿」**：
  * **改前**（HEAD 的 `DeckStore`）：`SaveAll(0)` 写出 `"decks": []` ⇒ `dto.decks` 是**空表不是 null** ⇒ 走成功支 ⇒ `note = None` / `detail = null` ⇒ 三条读数全绿；
  * **改后**（`DA` §2 那版「看原文有没有 `"decks":` 键」）：`SaveAll` 恒写该键（`JsonUtility.ToJson(dto, true)`）⇒ 键在 ⇒ 同一条成功支。
  * ✅ **收工时该修法已经落地**（工作树里的 `DeckStore.cs` 已有 `bool hasDecksKey = text.IndexOf("\"decks\":", …)` 那一句）
    ⇒ 这条护栏**恰好钉在它的越界方向上**；`RuleEngineTest.Run` 一跑就能同时验「13 条红转绿」与「这条护栏仍绿」。
* **那行 `Debug.Log` 是刻意留的观测**：`DA` §5.5 说「预期 `JsonUtility` 对空表写 `[]`，但**本地验不了**（它只在 Unity 里跑）」——
  这行把**原文**打进日志，下一次 `RuleEngineTest.Run` 的日志里直接能看到，**不用再猜**。
  我**没把它写成断言**：万一它真省略该键（则 `DA` 的修法不成立），那是**新发现**，不该以「WE 的夹具红了」的形式出现。

### 1·4 任务③：`DeckRulesTest.cs:476` 的**注释级**订正（`:476-484` 新增注释）

* 只加了一段「原来写 X、为什么恒假」的痕迹（铁律 5 的写法），**那一行的逻辑一个字没动**。
* 内容：`note` 现在是**诊断串**（`"no save file"` / `"save file has no decks"` / `"read failed: …"`，见 `DeckStore.cs` 兼容重载的注释）
  ⇒ `Contains("失败")` **恒假** ⇒ 这条断言**实际只靠前半 `note == null` 在撑**，后半是**死码**。
* 顺手 grep 过**同文件内**没有第二处 `Contains("失败")` / `note.Contains`（只有 `:476` 一处）。

---

## 2. 每条相关断言【把哪一处实现改坏会让它红】

🔴 逐条列（挡「断言自证 / 同义反复」「弱断言」「假断言」三族）。

| 断言（改后行号） | 变异（改坏哪一处） | 实得 | 判定 |
|---|---|---|---|
| `:3906-3910` ④「后进手牌那份**补到 2 条**」 | ① `RuleCore.HandEffectRegistry` 的**按 `RecordId` 去重**两层（`RuleCore.cs:3688-3691`）改回「来源+载荷」字符串键 ⇒ 两条记录被压成 1；② `SetupCardInHand` 早退 | ①**1** ②**0** | 红 |
| `:3911` ④「那 2 条真的落在它自己身上」 | 同①；或 `AttachHandEffectCopy` 把 `RecordId` 抄成新号（补挂变成新记录） | 1 / 4 条 | 红 |
| `:3926` ④「再补一次是**幂等**」 | `EffectResolver.cs:3048-3049` 那句 `if (e0.RecordId == src.RecordId) return false;` 删掉 | **2** | 红 |
| `:3937` ④「端到端 2 → **4** 攻」 | ① 记录被压成一条 ⇒ 只 +1；② `ApplyHandBuffs` 只跑第一个 `Op` | **3** | 红 |
| `:3981-3984` ⑤「`create … 进手牌` 也走消费者」 | `EffectResolver` 那条 `RuleCore.SetupCardInHand(ctx,p,made)`（造牌进手牌那一句）去掉 | **0** | 红 |
| `:4042-4045` ⑥「后来进手牌的牌照样吃得上」 | `HandEffectRegistry` 改回「现场扫手牌实例」（或删 `PlayerState.HandEffectRecords`） | **0** | 红 |
| `:4107-4109` ⑦「负例：非 Beast ⇒ 一条都不给」 | 见下面 §「追加三件」① —— 删兵种筛 ⇒ **落到末尾 `return true` ⇒ 1** ⇒ 红 | **1** | 红 |
| `:19383-19389` ②-1「判不出 ⇒ 一条都不给」 | `HandEffectFits` 的退路档改回 `return true`（= `W5` 之前的行为） | **1** | 红 |
| `:19383-19395` ②-1「**出声**」（新增，见下） | 撤掉 `RuleCore.SetupCardInHand` 循环里 `if (isLoose) loose++;`（或把 `if (!fit) continue;` 挪回它之前）⇒ `loose` 恒 0 | `false` | 红 |
| `:19821` ②-3「0 套 ⇒ 读出来 0 套」 | `SaveAll` 把空表写成省略键（`JsonUtility` 行为）⇒ `Empty` 支被走到 ⇒ 返回空表**仍是 0 套** | ⚠️**仍绿** | 弱（所以配了下面两条） |
| `:19822-19825` ②-3「码 `None`」 | (a) `DeckStore` 加「或空」（`dto.decks.Count == 0` 也算失败）；(b) 新判据把「空表」误判成「缺键」 | `Empty` | 红 |
| `:19826-19828` ②-3「类型 `None`」 | 同上两条 | `Failed` | 红 |
| `:19829-19830` ②-3「没有词条键」 | `TermKeyOf` 给 `None` 也返回键 | 非 null | 红 |

> ⚠️ **`②-3` 那四条里，「数量 == 0」那条单独看是弱断言**（`Empty` 支也返回空表 ⇒ 它照样绿）；
> 真正有判别力的是**码 / 类型 / 词条键**这三条。读的时候别只挑第一条。

---

## 3. 追加三件（调度台 2026-10-18 中途追加）

### ① `:4058-4068`（原 `:4010-4011`）那条 🧨 判别力说明 —— 🔴 **我改成了「结论不变、中间两环订正」，不是您说的那个改法**

**您给的判据**：「`W5` 把退路档收窄成「不放行」之后，删掉兵种筛 ⇒ 落 `HandEffectFits` 的 `:3748` ⇒ `return false` ⇒ 负例仍是 0 ⇒ 这个变异改不出红了」。

**我逐行核过 ⇒ 这条前提**不成立**，所以没有照办**。证据（全部是当前工作树）：

1. ⑦ 的夹具 spec **带 `Kind` 词**：`RuleEngineTest.cs:4082-4086` = `{ Raw="(夹具：手牌里的野兽)", Side="own", Kind="unit", SubtypeFilter="Beast", Count=0, Auto=true }`。
2. `HandEffectFits`（`Core/RuleCore.cs:3724`）`var crit = CardCriteria.FromTarget(e.Target);`
   ⇒ `CardCriteria.FromTarget`（`Core/CardCriteria.cs:113-125`）取 `Kind` ⇒ `CreatePool.IsKindWord("unit")`
   ⇒ `LookupKind("unit")` 命中 `KindWords` 表（`Core/CreatePool.cs:878` = `new[] { "unit", "type", "unit" }`）
   ⇒ `KindWord = "unit"`、`IsEmpty == false` ⇒ **`crit` 非空**。
3. ⇒ 把兵种筛（`Core/RuleCore.cs:3750-3757` 的 `if (hasSubtype) { … }`）整段删掉后：
   `:3748` 的 `crit == null && !hasSubtype` **为假**（`crit` 非空）；`:3749` 的 `crit.Matches(card)` 对
   `T_SFFoot`（`type = "unit"`、`subtype = "Infantry"`）**为真**（`MatchesKind` 比的是 `CardDef.Type`，不看 subtype）
   ⇒ 直落方法末尾 `:3758 return true` ⇒ **负例实得 1**。
4. ⇒ 这个变异**照样出红**，**旧说明的结论是对的**；错的是它中间那两环：
   · 「`crit` **判不出来**」→ 本次不成立（`crit` 非空）；
   · 「**退路档放行**」→ 是 `W5` **之前**的旧行为，今天是「判不出来 ⇒ `return false`」。

**我实际改成了**（`RuleEngineTest.cs:4058-4068`）：保留结论「删兵种筛 ⇒ 负例实得 1 ⇒ 红」，
把链路改成 `Kind="unit"` ⇒ `crit` 非空 ⇒ 过 `Matches` ⇒ **落到末尾 `return true`**，
并显式写上「旧说明错在中间那一环」的订正痕迹（铁律 5）；断言文案（`:4107-4109`）同步改掉「退路档放行」四个字。

> 🔎 **如果您的本意是「让这条判别力真的落在退路档上」**：那要**同时改夹具** —— 给一条**只有兵种、没有 `Kind` 词**的 spec
> （例：`SubtypeFilter="Beast"` 且 `Kind` 不是 kind word）⇒ `fromTarget` 返 null、`hasSubtype` 真 ⇒ 走兵种筛那一段，
> 删掉筛子仍是 `return true` ⇒ 同样红。**但结论一样、没有净收益** ⇒ 我没做，等您裁。

### ② 新增「判不出来 ⇒ 出声」断言（`RuleEngineTest.cs:19361-19395`，落在 `ctxA` 那一族）

* 位置：**紧跟**被测动作 —— 先 `int fitA = RuleCore.SetupCardInHand(ctxA, 0, byNameA["W5CCheck"]);`，
  然后**立刻**两遍扫 `ctxA.Events`（2000 条环形，`Core/BattleContext.cs` 的 `EventCapacity` / `Log()` 的 `RemoveRange`），
  再判断言。原来那句 `Check(SetupCardInHand(...), 0, …)` 拆成 `fitA` 变量，**比较值没变**。
* 判据 = `RuleCore.SetupCardInHand` 末尾那句 `if (loose > 0) ctx.Log(…)`（`Core/RuleCore.cs:3655-3660`），
  计数来自 `WC` 修活的 `:3637-3641`（先取值 / 先计数 / 再判去留）。
* 匹配词 = **同时**含 `"筛选条件我们"` **和** `"判不出来"`（避开 `"吃上了"` 那条，两件事必须分得开）。
* 🔴 **灭自证写法**：不是「Events 里有没有这句话」，而是**这次调用前后各数一遍**、只认 **`after > before`**。
  理由：只断「存在」的话，**万一别处早先打过同一句**，这条就恒绿（撤掉 `loose++` 也抓不到）。
* 🧨 判别力写在断言文案里：撤掉 `if (isLoose) loose++;`，或把 `if (!fit) continue;` 挪回它之前（= `WC` 修之前那个死代码形状）
  ⇒ `loose` 恒 0 ⇒ 那句出声打不出来 ⇒ **`after == before` ⇒ 实得 False ⇒ 红**。
* ⚠️ 我**没有**动那三段里别的断言（`②-1b` / `②-2` 那两个夹具实测**本来就带 spec** —— `:19418-19424` 的 `unitSpec`、
  `:19450-19457` 的 `prevSpec` ⇒ 不是 `criteria = null`，跟我这六条红无关，也没被 ②-1 的出声盖住）。
* ⚠️ **本条不改任何别的断言 → 不会把别的弄红**（那三段此前确实一条 `ctx.Events` 断言都没有，已核）。

### ③ 「静默失效普查」要不要收新出声 —— **只报不动**（等您裁）

**普查在哪**：`RuleEngineTest.cs:4907`（注释）· `:4965-4969`（过滤器）· `:4997-4998`（打印）——
即 `TestFactionBattle`「两个阵营打 6 局、把不顺的话去重收上来」那一段。

**事实**：过滤器是 `IndexOf("没生效"|"没实现"|"没结算"|"判不了"|"数不出来")`。
新那句出声的原文（`Core/RuleCore.cs:3656-3660`）里**这五个词一个都不含** ——
「判不出来」**不是**「判不了」的子串（`判/不/出/来` vs `判/不/了`）⇒ **这道普查看不见它**。已核。

**两个选项与代价（由您裁）**：

* **选项 A：把 `"判不出来"` 并进过滤器**
  * 改一处：`:4965-4969` 那一串 `IndexOf` 加一行（纯工具性改动，**零断言风险** ——
    那道普查唯一的断言是 `:5011` 的 `unimplemented == 0`，它只看 `"的动作「"`，不受影响）。
  * ⚠️ **代价 1（真）**：那道普查的 `key` **不做卡名归一化**（只有「有 N 条效果本版没结算」那一种做了归一化，`:4972-4974`）
    ⇒ 新词会把**每个卡名各算一种**，把「种类数」撑大。真要并，得**同时**给它加一条归一化（照 `Tail(key)` 那种写法）。
  * ⚠️ **代价 2（未知）**：**这句在实战里到底会不会触发，我没验**（验要跑 Unity）。
    它要求「记录带的 `Target` 判不出来」且当回合有牌进手牌 —— 真对局里四个生产者都传 spec（`DB` §5.2），
    解析层今天剩下的「判不出来」只有 `Kind=="prev"` 无先行词那一档 ⇒ 可能是**零次**（那就白并；不并也白不并）。
* **选项 B：不动普查**
  * 成本零。代价：「实战里到底有几条手牌效果因解析缺口被少给」这个数字**看不见**。
  * 但这**不违反红线** —— 红线管「实现不许静默」（实现已经出声了，那句在 `ctx.Events` 里），
    普查漏收只是**统计口径不全**，不是行为缺陷。
* **我的倾向（仅供参考，不替您裁）**：先 A 的**最小版**（加词、不加归一化）跑一次看有没有、再决定要不要加归一化；
  或者干脆先只跑 `RuleEngineTest.Run` 看那句在 `TestFactionBattle` 的 6 局里出现几次（**零改动**就能知道要不要并）。

---

## 4. 类型检查读数（原样贴）

```
$ TMPDIR=/tmp/wf_we bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

* **跑了几次**：每批改完跑一次，共 **3 次**（夹具 4 处 + 护栏 / 追加①② / 追加② 精修），**每次都是 0 / 0**。
* **别人的报错：没有** —— 三次都一条不差地 0/0，**没有出现**集中在 `Core/RuleCore.cs` / `Data/DeckStore.cs`
  （本轮另有人在改的那两份）上的错误。⇒ 我这一趟**没有**看到「类型检查撞上别人半成品」那件事。
* 已确认我改的两份**确实进了 rsp 的源清单**（`grep -o "RuleEngineTest.cs" /tmp/wf_we/wf_csc_editor.rsp` 命中、
  `DeckRulesTest.cs` 同样命中）⇒ 这个 0/0 **不是**「没编到我的文件」假绿。

---

## 5. 行尾核对（前后读数）

用 `python -I` 二进制读、数 `b'\r\n'` 与 `b'\n'`（**不能用文本模式**，会把 CRLF 折成 LF）：

| 文件 | 改前 CRLF / LF | 改后 CRLF / LF | 裸 LF | `git diff --numstat` |
|---|---|---|---|---|
| `RuleEngine/Editor/RuleEngineTest.cs` | 19819 / 19819 | 19945 / 19945 | **0** | `135  9` |
| `RuleEngine/Editor/DeckRulesTest.cs` | 585 / 585 | 595 / 595 | **0** | `10  0` |

* 两份**本来就是纯 CRLF**，改完**仍是纯 CRLF**（裸 LF = 0）；numstat 数字**远小于文件行数** ⇒ **没有翻行尾**。
* 全程用 **Edit 工具**（⛔ 没用 `sed -i` / python 文本写）。

---

## 6. 顺手发现（**一个都没改**，报上来由调度台分流）

1. 🔴 **`HandEffectFits` 里「兵种筛」与「`crit == null` 那条退路」是两件事，但三处注释把它们写成一件事**（`RuleEngineTest.cs:4058-4068` 原来是其中之一，我已订正）：
   `Kind="unit"`（或任何 kind word）⇒ `crit` 非空 ⇒ **永不**落到退路档；只有「**没有 kind word 也没有 subtype**」才落退路档。
   建议以后写判别力时，先问一句「这个 spec 有 `Kind` 词吗」。
2. **`:19817-19819` 那行 `Debug.Log` 是本次唯一「只观测不断言」的落点** —— `DA` §5.5 的未知前提
   （`JsonUtility.ToJson` 对空表写不写 `"decks":`）靠它复核。**跑一次 `RuleEngineTest.Run` 就能结案** ⇒ 建议收口时顺手看一眼日志里那行。
3. **`DeckStore.LoadAll(out string note)` 兼容重载的两个调用点**（`DeckRulesTest.cs:227` / `:473`）：
   `:227` 那条断的是 `note` 的**字面内容**（`ReadFailed` 那一段），`:473` 那条是本次订正的假判据。
   ⇒ 那个重载**全仓只剩这两处**（`DeckStore.cs:174-181` 的注释自己写着「不在白名单里、没跟着改」），
   要不要连同「把 `Contains("失败")` 这一族彻底清掉」一起收口，**我判不了**（要动 `DeckStore` 注释 + `DeckRulesTest` 逻辑），交调度台。
4. **`②-3` 那一段会往 `path` 写一份「0 套」的存档**，紧跟着又被坏档夹具覆盖、最后 `finally` 删档 —— 
   我核过**不会**把 `DeckRulesTest` 那边的存档（另一个 `path`，各自 `OverridePath`）搅乱；两边都各自 `finally` 还原/删档。
5. ⚠️ **`②-3` 与 `DA` 的修法有一处交互值得盯**：`DA` 的判据是**原文里有没有 `"decks":` 键**，而
   `DeckRulesTest.cs:470-472` 那份**手写**的旧存档里也有 `"decks":` ⇒ 两边一致，不会互相破坏。
   但如果将来有人把写入器换成「不写空表键」的另一套，`②-3` 会**当场红**（这正是它要挡的 (a)）。
