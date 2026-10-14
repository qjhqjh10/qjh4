# `AC` · 拆掉 `Loc.BulkOr` 的「记号闸」（动手 · 2026-10-15）

> 写手：`AC` 波（调度台派的**唯一白名单文件** = `CardPresentation/Core/Loc.cs`）。
> 触发：P3 写手现核报「`BulkOr` 的记号闸挡住 18 处 / 11 键 ⇒ 卡面**原样印出** `((Stun_an_enemy))`」，
> 而闸自己写的理由（「要真接上，先做 `A1763` 的记号渲染层」）**已经不成立**（那个层 P0 已落地）。
> ⛔ 本笔**没跑任何 Unity 批处理、没跑 `ruleprobe`、没动 git、没改正本**。
> 判据指针（两处都读过了）：`Z_卡牌P3.md` §七·2 · `G_卡表P0.md` §七·2。

---

## 〇、结论（一句话）

✅ **闸已拆**：`Loc.BulkOr` 现在的顺序是「**取到值 → 过一遍 `CardIcons.RewriteMarks` → 渲完还留着记号才回兜底**」。
**卡面侧实测（离线演算台，见 §二）**：拆闸前 `descZh` 渲完仍残留 `((Key,…))` 的 = **18 处 / 16 张卡**，拆闸后 = **0 处 / 0 张**。
✅ **`Loc.BulkOr` 的「取不到」语义一个字没动**（`_bulk.TryGetValue` 那一支、回退英文那一支、`fallback` 那条路照旧）。
✅ `typecheck` **0 错 / 0 错** · 行尾**纯 LF 未翻**（`CRLF 0`）· `git diff --numstat` = **106 / 17**（不是整篇重写）。

---

## 一、判据出处（逐条现读）

| 要判什么 | 判据在哪 | 现读结论 |
|---|---|---|
| 渲染层**存在、且是公开口** | `CardPresentation/Core/CardIcons.cs:468`（`public static string RewriteMarks(string, bool)`）· `:448`（`public static bool HasOfficialMarks(string)`） | ✅ 有；四支按原版顺序 ①`((K,a,b))`→②`$$ID$$`→③`[[枚举]]`→④`(N)` |
| `CardIcons` 与 `Loc` **同一个程序集**（能互相引用） | `cd MyGame && find . -name "*.asmdef" -not -path "./Library/*"` ⇒ **0 个** ⇒ 都在默认 `Assembly-CSharp` | ✅ 不会构成跨程序集环 |
| 「值里那批记号」**具体是哪些** | `Resources/official_names.json`（2758 条）现数 | 带记号的值 **56 条**：`Modular_Description/*` 32 · `Achievements/*` 24；**全部是 `[[…]]`**，`((` 与 `$$` **各 0 条** |
| 卡池里**被挡住的是哪几处** | `RuleEngine/Resources/cards_engine.json`（1127 张）现数 | `descZh` 里 `((Key))` **247 处 / 73 键**；**其中值带记号的 = 11 键 / 18 处**（与 P3 报的 18/11 **逐条吻合**） |
| 消费者能不能吃「渲染后的串」 | `Core/TmpFont.cs:336-356`（`NewText` = **全工程建 TMP 的唯一一处**，`:356` `t.spriteAsset = CardIcons.SpriteAsset`）· `Battle/Label.cs:110/166`（走不了 TMP 时退回点阵，点阵那条路 `CardIcons.StripTags(text)` **会剥标签**） | ✅ 两条路都不印标签：TMP 解析 `<sprite>`、点阵剥掉 |
| 同类先例（Loc 值里本来就能带 `<sprite>`） | `Core/Loc.cs:1417`（`Battle/Effect/ChangeArmourOneTurn` 两列里就是 `<sprite name=Atlas_trait_icon_armour>`） | ✅ 这条口径**早就在跑** |

---

## 二、拆闸前后：各有哪些值走哪条路（逐类）

### 2·1 「三路」是怎么分的（改后的代码就是这三支）

| 路 | 条件 | 结果 | 出声 |
|---|---|---|---|
| **① 原样** | 值**不带**记号（`CardIcons.HasOfficialMarks(v) == false`） | 返回 `v`（**与拆闸前逐字相同**） | 无 |
| **② 渲染** | 值带记号，且 `RewriteMarks` 过完**记号没了** | 返回**渲染后的串** | 无（子词条缺时由 `CardIcons` 那几支出声） |
| **③ 回兜底** | 值带记号，`RewriteMarks` 过完**仍留着记号** | 回 `fallback` + **一次出声**（`_warnedBulk` 按键去重） | `[Loc] …渲染不出来的官方记号…` |

> **闸拆掉的是「判据」**：旧闸判「**值里有没有**记号」⇒ 拦掉了**全部 56 条**；
> 新判据判「**渲染完还有没有**记号」⇒ 只拦**真的渲不掉的**那 2 条。**新旧都保证「界面不会印出记号原文」。**

### 2·2 逐类现数（离线演算台，数据 = 仓内那份 `official_names.json`）

**A. 值带记号的那 56 条（`Resources/official_names.json`）**

| 语言 | 带记号的值 | ② 渲染出来 | ③ 渲不掉 → 回兜底 |
|---|---|---|---|
| 中文（zh 列） | 55 条 | **53 条** | **2 条**：`Modular_Description/Duplicitous` · `Modular_Description/Maintenance_x` |
| 英文（en 列） | 56 条 | **54 条** | 同上那 2 条 |

**那 2 条为什么渲不掉**（判据 = 词条 + 字形**两样都没有**）：
- `Duplicitous` = `[[tainted]]` · `Maintenance_x` = `[[maintenance]] {0}`
- `Card_Trait/tainted` · `Card_Trait/maintenance` **两张表都没有**；
  `Atlas_trait_icon_tainted` · `Atlas_trait_icon_maintenance` **在 87 个字形里也没有**
  ⇒ `CardIcons.RewriteMarkTraits` 走的是它自己那条「认不出就原样留着 + 出声」。
- 🔴 **这两条今天【零引用】**：卡池 247 处 `((Key))` 的 73 个键里**没有** `Duplicitous` / `Maintenance_x`
  （逐键现数），而 `Modular_Description/*` **只有** `RewriteMarkModular` 一个消费者
  ⇒ **③ 这一支今天 0 个活的输入**（是「将来数据进来了」的兜底，不是现在在用的路）。

**B. 卡面（`cards_engine.json` · 1127 张）**

| | 拆闸前（旧闸） | 拆闸后 |
|---|---|---|
| `descZh` 渲完**仍残留记号**的 | **18 处 / 16 张卡**（卡 `0` `Unhallowed Gifts` 一个人占 3 处） | **0 处 / 0 张** |
| 卡池里带记号、会被 `CardIcons.Rewrite` 送进渲染的卡 | 559 张 | 559 张（不变；那 950 处 `[[X]]` 与 247 处 `((K))` 同属这一批） |
| `desc`（英文列）带记号的卡 | **0 张**（现数；950 处 `[[X]]` **全在 `descZh`**） | 0 张 |

拆闸前的 18 处（16 张卡），首 6 例（这就是「卡面原样印 `((Key))`」那一批）：
`228 Howling Banshee` · `166 Chaos Lord` · `210 Chaos Sergeant` · `172 Chosen Champion` · `182 Chosen` · `0 Unhallowed Gifts` …
被挡住的 11 键 = `Stun_an_enemy`(3) `Stun_a_random_enemy`(3) `Gain_a_random_mark_of_chaos`(2) `Gain_a_mark_of_chaos`(2)
`Reanimate_a_remnant`(2) `Give_a_mark_of_chaos_to_friendly_troop`(1) `Stun_adjacent_units`(1) `Reanimate_random_remnant`(1)
`Blind_all_enemies`(1) `Stun_three_random_enemies`(1) `Blind_a_random_enemy`(1)。

渲染出来长什么样（现读演算台，例）：
- `Modular_Description/Stun_an_enemy`（zh）→ `使一个敌方目标 <link=stun><nobr><sprite name="Atlas_trait_icon_stun"><b>眩晕</b></nobr></link>`
- `Modular_Description/Jam_a_troop`（zh）→ `使一支军队 <link=jam><nobr><b>干扰</b></nobr></link>`
  ⚠️ **`jam` 没有字形** ⇒ 只印词、不插图（`CardIcons` 那条既有口径；**今天 0 个活的输入**，见 §七·2）。

**C. 「渲染不出来会怎样」（⛔ 不许印记号原文 / ⛔ 不许静默丢）**

- **不印原文**：渲不掉 ⇒ 走 ③ 回 `fallback`（值里一个 `[[` 都不会流到界面）。
- **不静默**：③ 那支**每次都记账**、**按 `term` 去重出声一次**，告警文案里写明「多半是子词条缺」+
  指到 `CardIcons` 那几支的出声（缺在哪由它们逐条报）。
- ⚠️ **但要注意下游**：卡面那一路，`BulkOr` 回 `null` 之后 `CardIcons.RewriteMarkModular` 会**原样留着 `((Key))`** 并出声
  —— 即「`((Key))` 这个**外层的**记号会印到卡面上」。这是 **P0 那层既有行为**（不是本笔引入，改前改后一样），
  它属于 `A1763` 的**数据侧缺口**（要补 `Modular_Description/*` 的**子词条**），**本笔不越权去改**。
  🔴 **今天 0 个活的输入**（= 上面那 2 条零引用的键），所以**今天卡面上一个记号都不会印出来**。

### 2·3 语义没变的三处（逐条核过）

1. **「取不到键」那一支**（`:2723-2728`）：`_bulk.TryGetValue` 失败 ⇒ 出声 + 回 `fallback`。**逐字未动**。
2. **「该取哪一列」**：原来的 `if/else if` + `v = IsNullOrEmpty(r.en) ? r.zh : r.en` 我拆开了，但**取值结果等价**：
   - 中文档且 `r.zh` 非空 → `r.zh`（并记下 `zhCol = true`，**新加的只是这个「哪一列」的标记**）
   - 英文档且 `r.en` 非空 → `r.en`
   - 其余 ⇒ 回退英文（`r.en` 空才回 `r.zh`）——**与 `T()` 同口径**，也没加新的出声
3. **「空值回兜底」**：`if (string.IsNullOrEmpty(v)) return fallback;` 位置、条件不变。

---

## 三、`Loc.BulkOr` 的**全部**消费者清单（🔴 grep 过「谁会读它的返回值」）

全仓 `*.cs` grep `BulkOr` 的**真实调用点**（`Library/` 下的 dll 是编译产物，忽略）：

| # | 位置 | 读的是哪一族 | 值带记号的条数 | 改后实际拿到什么 |
|---|---|---|---|---|
| 1 | `Core/CardIcons.cs:517`（`OfficialTerm` ← 被 `RewriteMarkModular`/`RewriteMarkCardRefs`/`RewriteMarkTraits` 三支调） | `Modular_Description/*` · `Card_Name/*` · `Card_Trait/*` | 32（卡池引用 11） | **渲染后的串**（这正是本笔要的） |
| 2 | `Shell/ProfileData.cs:122`（`TypeName`） | `Achievements/Types/*`（5 条） | **0** | 逐字不变 |
| 3 | `Shell/ProfileData.cs:244`（成就**描述**） | `Achievements/<Ach_N>` | **24**（本地 102 条成就里 24 条命中） | **渲染后的串**（原来是回兜底 = 我们 JSON 里那句英文 `Trigger Codex`） |
| 4 | `Shell/ProfileData.cs:248`（成就**名**） | `Achievements/Title/<Ach_N>` | **0** | 逐字不变 |
| 5 | `Shell/ProfileData.cs:262`（头像/称号名） | `Avatars/*` · `Titles/*` | **0** | 逐字不变 |
| 6 | `Shell/PrebuiltDecks.cs:85`（`DisplayName`） | `PrebuiltDecks/*`（74 条） | **0** | 逐字不变 |

**六个消费者都能吃「渲染后的串」**，判据两条（现读）：
- 建文字的地方**只有一处** `Core/TmpFont.cs:336`（`NewText`），它在 `:356` **无条件**挂卡面图标的 sprite asset
  （注释写的就是「不设它的话 `<sprite name=…>` 会当字面量印出来」）⇒ `<sprite>`/`<link>`/`<nobr>`/`<b>` 都能解析；
- 取不到字体资产时退回**点阵**那条路（`Battle/Label.cs:110→166`），它**会 `StripTags`** ⇒ 也不会印出标签。

⚠️ **假阳性要说清**（免得下一位照着数）：`Editor/NetSelfTest.cs:54,462` 的 `TestBulkOrder` 是**联机报文乱序**那个测试，
**与 `Loc.BulkOr` 无关**；`Editor/MainMenuScene.cs:4529` 只是**注释里提了一句**。这两个**不是消费者**。

---

## 四、改了哪些行（差分清单）

文件：`Unity/MyGame/Assets/CardPresentation/Core/Loc.cs`（**只改这一个文件**；`CardText.cs` **没动、也不需要动**）

| 现读行号 | 改了什么 |
|---|---|
| `:2645-2655` | **新增** `static bool _inBulkRender;` + 它的 doc（重入闩，见 §六·2） |
| `:2689`（首行） | doc 首句：「取到的是**未渲染的 I2 记号**」→「取到的是**渲染不出来的官方记号**」（只改措辞，与实现对齐） |
| `:2697-2716` | **换掉**原来那段「🔴 记号闸」doc，改成「三路」的说明 + **指针**（⛔ 不在注释里抄第二份数） |
| `:2729-2739` | 取值那 8 行：拆出 `bool zhCol`（**取值结果等价**，见 §2·3） |
| `:2741-2763` | **闸 → 渲染**：`HasOfficialMarks(v)` 时过 `RewriteMarks(v, zhCol)`；渲完仍有记号 ⇒ 回兜底 + 出声（新告警文案） |
| `:2764` | `return v;`（位置不变） |

- **行尾**：改后 `CRLF 0 / LF 2993`（改前 `CRLF 0 / LF 2944`）⇒ **纯 LF 未翻**。
- **`git diff --numstat`**：`106 / 17`（⚠️ 这个数**含 L 波早先那批 `Battle/Log/*` 的改动** —— 那份改动在我动手前就在盘上；
  本笔自己那 4 处 hunk 加起来是 +90 行左右）。
- ⛔ **没动** `Loc.cs` 里那 4 处「自陈过期」的 `Battle/Log/*` 注释段（L 波刚订正过的）。

---

## 五、断言（含判别式）

🔴 **本笔加了 0 条断言** —— 原因：断言宿主（`RuleEngineTest.cs` / `Editor/{BattleScene,DeckScene}.cs`）**不在本笔白名单**
（它们分别属引擎线/宿主写手），而本笔又不许跑 Unity 批处理 ⇒ **没法自己跑一条来验**。
⛔ 不因此把「没验」写成「验了」。下面是 **ready-to-paste** 的四条，交调度台分给宿主写手：

```csharp
// ⓪ 先摆基线（同 `ResetMissingWarnedForTest` 的用法；⛔ 只清去重表，不动 PlayerPrefs）
Loc.SetLanguage(AvailableLanguages.Chinese);   // ⚠️ 长批处理里请配 `Loc.PersistOverride = true`
Loc.ResetBulkWarnedForTest();

// ① 正路：拆闸前被整条挡掉的值，现在**必须渲染出来**，且一个记号都不许留
string s = Loc.BulkOr("Modular_Description/Stun_an_enemy", "<FALLBACK>");
CheckTrue(s != "<FALLBACK>"
          && s.IndexOf("[[", StringComparison.Ordinal) < 0
          && s.IndexOf("<sprite name=\"Atlas_trait_icon_stun\">", StringComparison.Ordinal) >= 0,
          "★ 拆闸后：`Modular_Description/Stun_an_enemy` 必须**渲染出来**"
          + "（记号 0 个 + 眩晕那张图在场）——实得：" + s);

// ② 判别式（灭自证）：同一口对【渲染不掉】的那条**必须回兜底**。
//    ①② 不可能被**同一份实现**同时满足 —— 「一律渲染」让 ② 红、「一律回兜底」让 ① 红。
Check(Loc.BulkOr("Modular_Description/Duplicitous", "<FALLBACK>"), "<FALLBACK>",
      "★ 判别式：值里那条 `[[tainted]]` 本地既无词条、也无字形 ⇒ 必须**回兜底**"
      + "（⛔ 不许把 `[[tainted]]` 原样印到界面上）");
// ⚠️ ② 的**前提**：`Card_Trait/tainted` 与 `Atlas_trait_icon_tainted` 两边都缺（现读）。
//    将来谁把 `tainted` 补上，**这条要换一个同类值**（同类候选：`Modular_Description/Maintenance_x`），
//    ⛔ 不是把断言删掉。

// ③ 不带记号的值**逐字不变**（拆闸不许动这条路）—— 任挑一条上面 §三 表里「带记号 0 条」的族
CheckTrue(Loc.BulkOr("Achievements/Types/Battle", "<F>") == "对战",   // ← 值以现读表为准
          "不带记号的值与拆闸前逐字相同");

// ④ 卡面那一层（`BattleScene.Run` 的卡面节更合适）：那 16 张的 `descZh` 过 `CardIcons.Rewrite` 之后
//    不许再出现 `((` / `[[` / `$$`（这才是「卡面不印记号」的直接验收）
CheckTrue(!CardIcons.HasOfficialMarks(CardIcons.Rewrite("228", CardIcons.DescZhField, descZhOf("228"))),
          "★ 卡面 228 `Howling Banshee`：记号必须**全部渲染掉**（拆闸前这条必红）");
```

**判别式为什么这么写**：本笔最怕的失效模式是「**把闸拆了但渲染没接上**」和「**渲染没商量好就把值放出去了**」——
① 咬前者、② 咬后者，两者互斥。⚠️ 光断「① 绿」不够（`RewriteMarks` 恒返回非空串就能骗过弱写法），
所以 ① 里同时断**记号 0 个**与**那张图在场**（结构上不可能由「原样返回」满足）。

---

## 六、没查清的部分（⛔ 没猜）

1. 🔴 **`RewriteMarks` 在真数据上的端到端画面证据，本笔仍然没有** —— P3 §八·1 已如实报过，本笔**继承这个缺口**
   （不许跑 Unity ⇒ `CardBaseDemo.Run` / 真机并排渲都没跑）。
   ✅ 本笔能补的是**离线演算台**（§二那张表：56 条逐条、16 张卡逐张），
   ⚠️ **但它是「按 C# 四支照抄成 py」，不是我们那份 C# 本体** —— 结论只用来**分类**，不等于画面。
2. 🔴 **「重入闩」这个设计是我加的（原指令只写「过一遍 `RewriteMarks`」）**，理由两条（已落进代码注释）：
   ① 原版四支**不回头**（`ModifyLocalization.c` 一遍扫过去），词条里再嵌 `((K))` 原版**不会**再展开
   —— 不加重入闩就会比原版**多展开一层**；
   ② 不加就**没有结构性终止保证**：`Modular_Description/A` 的值里出现 `((A))` 就是无限递归 ⇒
   `StackOverflow`（Unity 里**硬崩**，不是一条红）。
   **今天 0 个输入会走到那一支**（现核：整张表里 `((`/`$$` **各 0 条**，只有 `[[`），
   ⇒ **要么留着（零成本、挡崩），要么调度台裁掉** —— **本笔不自己发明更大的口径，先交裁**。
3. ⚠️ **「当前语言」与「调用方要的语档」不一致那一格**：`Loc.BulkOr` 按 `Current` 选值，
   而 `CardIcons.OfficialTerm` 在**手写表**那条路上按调用方传的 `zh` 取列 ⇒ 两者在
   「当前语言 = 中文 / 调用方要英文列」时**会不一致**。**本笔没改**（`CardIcons.cs` 不在白名单），
   ✅ 今天**到不了**：`CardIcons.Rewrite` 的 `zh` 由 `descZh` 字段决定，而 `descZh` 只在中文档用
   （`BattleDriver.FaceTextFull` 那条口径）。**如实留在这里。**
4. ⚠️ **`<b>` 在我们卡面字体上是否有可见效果** —— P0 §八·2 挂起的那条，本笔**照旧没查**（要真机并排渲）。
   注：本笔让 `[[…]]` 第一次真进了卡面 ⇒ **这条从「纸面」变成「看得见」**，优先级上升了。
5. ⚠️ **成就窗那 24 条**从「我们 JSON 里那句英文」变成「官方中文 + 行内图标」——
   **布局会不会被撑开/压字，没查**（要真机看 `AchievementsMenu`）。判据只查到「标签能解析标签」那一层（§一）。

---

## 七、顺手发现（**只报，⛔ 一条都没改**）

1. ⚠️ **`CardIcons.cs:504-506` 那段注释现在只对了一半**：它写「`Loc.BulkOr` 用的是**当前语言**（不是本函数的 `zh`），
   而且它自己有一道『值是未渲染记号 ⇒ 当取不到』的闸 —— 那道闸**留给调度台裁**」。
   ⇒ **前半句仍成立、后半句已过期**（闸今天拆了）。⛔ 本笔**没动 `CardIcons.cs`**（不在白名单）
   ⇒ 请调度台派一笔把那段注释就地订正（铁律 5）。
2. ⚠️ **`jam` 没有字形**（`Atlas_trait_icon_jam` 不在那 87 个字形里）—— 于是
   `Modular_Description/Jam_*` 渲出来是 `<nobr><b>干扰</b></nobr>`（**只有词、没有图**）。
   ✅ **不是缺陷、也不会印记号**：这是 `CardIcons` 那条既有口径（词条有、图没有 ⇒ 只印词）。
   ✅ **今天 0 个活的输入**：卡池 `descZh` 用的 55 种 `[[名]]` **逐种核过、全都有字形**
   （`jam` **不在**那 55 种里），`Jam_*` 那几个键**卡池一处都没引**。
3. ⚠️ **`official_names.json` 里 `Card_Trait` 只有 26 条**，而 `Card_Trait/*` 的两张表合并后能覆盖的枚举名更多
   —— 现读「卡池里 55 种名 **全部**能在两张表里取到词条」（演算台 0 处残留就是这么来的）。
   ⇒ **P0 那份 doc 里「8 种查不到」的缺口**，在 P3 补了 `Card_Trait` 26 条之后**已经关上**。
   ⛔ 本笔只报，**没去改 P0/P3 的文档**（正本不在本笔白名单）。
4. ⚠️ **`GetTraitTextSprite` 那 5 个 `Atlas_SpiritStone_*`**：字形表 87 个 = 82 个 `Atlas_trait_icon_*` + 5 个
   `Atlas_SpiritStone_1..5`。⇒ `[[Spirit_x]]` 那类**不会**走 `[[X]]` 那支（它是 `Modular_Description` 的键名，不是枚举名），
   **两条不冲突**。只报，**没改**。
5. ⚠️ **`Editor/MainMenuScene.cs:5456` 那条断言的【注释】已过期**（⛔ 不在本笔白名单，**只报**）：
   `CheckText(TextOf(…"description"), a0.Challenge, "…= challenge 的 id（真字符串…译文查不到 —— 我们拿它顶）")`
   —— 「译文查不到 ⇒ 拿 id 顶」这半句**今天不成立**（`A1803` 起先取官方词条，本笔之后其中 24 条还会**渲染出图标**）。
   ✅ **断言本身照旧绿**（期望值取的是**同一个模型** `a0.Challenge`，`Label.Text` 回的是**原样那串**、不剥标签
   —— `Battle/Label.cs:156-166` + `Editor/MainMenuScene.cs:129-133` 现读），
   但它**测不出**「描述里是不是带标签」这类变化（**自证那一族**），要不要补一条判别式**交调度台裁**。
6. ✅ **排查掉一个担心（不是缺陷）**：`Battle/Label.cs:162` 的 `_iconBoost = CardIcons.FontScaleFor(text)`
   —— 现读 `CardIcons.FontScaleFor` **恒返回 `1f`**（它整段 doc 就是「**不放大**」的结论）⇒
   文字里多了 `<sprite>` **不会**改字号/折行。只报，**没改**。

---

## 八、typecheck

```
$ TMPDIR=/tmp/wf_locgate bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（⚠️ 按铁律 13·3，独立 `TMPDIR` 只解决输出互相覆盖、**挡不住「别的写手正写到一半的文件被一起编进来」**
—— 本笔跑的那一次（脚本内部编运行时 + 编辑器两个程序集），报错**全部为 0**，
不存在「错在别人文件上」的歧义。）
