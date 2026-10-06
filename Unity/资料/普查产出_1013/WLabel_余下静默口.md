# WLabel · `Label` 余下 5 处静默口（A545）

> 独占文件：`Battle/Label.cs`（共用件）· 断言宿主 `Editor/SettingsScene.cs`（**接着 A491 那一节加**，W-E5 那 6 条**一字未动**）
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用只读的 `git status` / `git diff` / `git show` / `git diff --numstat`）· ⛔ 没改两张正本
> ⛔ 没越白名单（`Battle/{BattleDriver,ScenarioBlendables}.cs` · `Editor/BattleScene.cs` · `Shell/*` · `Deck/*` · `工具/*` **全部只读**）
> ✅ 类型检查**改完立刻**跑过 3 次：`TMPDIR=/tmp/wf_wlabel bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**

---

## 一、结论

**A545 做完**：`Battle/Label.cs` 里同族 `_tmp == null` 的**最后 5 处静默早退**（`SetWrapWidth` / `SetWrappingMode` / `ForceRelayout` / `SetFontSize` / `SetAutoFitBox`）**全部改成出声**，走的是**已有的** `NoteDotAlign`（**没有另开一套**）；其中两处 **doc 自陈静默**（「什么都不做（如实，不假装）」/「会静默无效」）**就地订正**。

| 项 | 状态 | 一句话 |
|---|---|---|
| **5 处主体** | ✅ **做了** | 每处的 `if (_tmp == null) return;` → `if (_tmp == null) { NoteDotAlign(…); return; }`（**逐字照 A476/A491 的形状**） |
| **出声助手** | ✅ **仍是唯一一口** | 只给核心 `NoteDotAlign` **加了一个 `why` 形参**（原来那句「没有对齐这回事」是写死的，而这 5 个口「缺什么」各不相同）⇒ **key 的拼法仍只有一份**（`which + "\|" + 节点全路径`，`:934`） |
| **A476 / A491 零回归** | ✅ | 两条消息**逐字节没变**（Python 复算 = `True`/`True`）；现存断言 `Editor/SettingsScene.cs:1546` 的 `Contains("点阵后端没有对齐这回事")` **仍成立** |
| **key 不撞车** | ✅ | 已有 3 个（`左对齐` / `右对齐` / `逐行左对齐`）＋ 新的 5 个（`折行宽` / `换行模式` / `重排` / `字号` / `自适应框`）**两两不同**（§三） |
| **两句 doc** | ✅ **改了** | §四（两处都保留「原文」引用 —— 铁律 5 要留更正痕迹） |
| **断言** | ✅ **10 条**（§六） | 1 条夹具自检（A476 回归）＋ 5 条「必须出声」＋ 3 条前提 ＋ **1 条反面对照**（有 TMP 时一行都不许出） |
| **共用件影响** | ✅ **有字体资产时逐位为零** | **机械复核过**（§五）：5 个方法**守卫之后的语句与 HEAD 逐行相同** |
| **类型检查** | ✅ **0 / 0** | 三次都是（§九）；**没有一条错落在别人的文件上** |
| **行尾** | ✅ **没翻** | `Label.cs` = **CRLF 1055 · LF 1055**（纯 CRLF，改前 979）· `SettingsScene.cs` = **CRLF 0 · LF 2636**（纯 LF，改前 2524） |

🔴 **本件是静态核对 + 类型检查，不是「跑绿了」** —— 断言**没跑**（本轮口径：A 表清完再跑；⛔ 本件不许跑 Unity）。要跑的就是 **`SettingsScene.Run`**。这条要如实转述（同 §七·1）。

---

## 二、逐处改动（行号 = 本件收工时**现读**；⚠️ 行号会漂，一律以你现读为准）

| # | 文件:行 | 改前 | 改后 | 助手 | **我用的 key（`which`）** |
|---|---|---|---|---|---|
| 1 | `Battle/Label.cs:251-256`（`SetWrapWidth`） | `if (_tmp == null) return;`（**一个字都不留**） | `if (_tmp == null) { NoteDotAlign("折行宽", "不会折行", "折行宽度 = " + worldWidth, "没生效、这段文字仍然不折行"); return; }` | `NoteDotAlign` | **`折行宽`** |
| 2 | `:407-412`（`SetWrappingMode`） | 同上 | `NoteDotAlign("换行模式", "只有「单行」这一档", "换行模式 = " + originalMode, "没生效、这段文字仍然单行不折")` | 同上 | **`换行模式`** |
| 3 | `:456-461`（`ForceRelayout`） | 同上 | `NoteDotAlign("重排", "不经过 TMP 排版", "重排一次（把版面推下去）", "没生效（这个后端没有可推的那一版）")` | 同上 | **`重排`** |
| 4 | `:506-512`（`SetFontSize`） | `if (_tmp == null \|\| worldSize <= 0f) return;` | **两句拆开**：`if (worldSize <= 0f) return;`（**无效入参 ⇒ 照旧不出声**，见下）→ `if (_tmp == null) { NoteDotAlign("字号", "没有 TMP 的 fontSize（只有整数档 scale）", "fontSize = " + worldSize, "没生效（点阵那条路只认整数档，见 SetSizes）"); return; }` | 同上 | **`字号`** |
| 5 | `:641-646`（`SetAutoFitBox`） | `if (_tmp == null) return;` | `NoteDotAlign("自适应框", "既不会折行、也没有自适应", "一个 " + worldW + "×" + worldH + " 的框 + 自适应 " + minPx + "~" + maxPx + "px", "没生效（框、折行、字号自适应三样都没有）")` | 同上 | **`自适应框`** |
| 6 | `:931-937`（**助手本体**） | 签名 `NoteDotAlign(string which, string wanted, string symptom)`；消息里**写死**「但\*\*点阵后端没有对齐这回事\*\* ⇒ 」 | 签名 `(string which, string why, string wanted, string symptom)`；消息模板 `"…但**点阵后端" + why + "** ⇒ " + symptom` | —— | —— |
| 7 | `:905`（2 参转发，A476 用） | `NoteDotAlign(which, which + "（x=" + worldX + "）", "没生效、整块停在框心")` | `NoteDotAlign(which, "没有对齐这回事", which + "（x=" + worldX + "）", "没生效、整块停在框心")` | —— | `左对齐` / `右对齐`（**没动**） |
| 8 | `:333`（A491 的 `SetAlignLeft` 调用点） | `NoteDotAlign("逐行左对齐", "逐行左对齐（每行在块内贴左）", "没生效、每一行仍在块内居中")` | 中间插一格 `"没有对齐这回事", ` | —— | `逐行左对齐`（**没动**） |
| 9 | doc / 注释 6 处 | 见 §四 | 见 §四 | —— | —— |

**第 4 处为什么把 `worldSize <= 0f` 那道闸【挪到前面】**：合成一句时，「点阵后端下传 `0`」会被归因成「这个后端没有字号」—— 而真正的原因是**入参非法**。挪到前面之后，「无效入参照旧不出声」这句话才**严格成立**（三种组合逐条核过：`_tmp != null` 时与 HEAD 完全一致；`_tmp == null && size <= 0` 与 HEAD 同样静默；只有 `_tmp == null && size > 0` 这一格从静默变成出声 —— 那正是本账要修的那一格）。

**没改**的（逐条核过）：

- 5 个方法**守卫之后**的一切语句（§五 用机械法证明逐行相同）；
- 两个 `Align*On` 的**正常路径**与 `HasMeasuredWidth()` 那条**有意不出声**的守卫（A476 的 doc 一条没动）；
- `SetWrappingMode` 里**传非法档**那一支（`Debug.LogWarning`，与后端无关，早就出声）；
- `SetCharSpacing`（已是出声口，但它走自己的 `Debug.Log`）· `RefreshBounds`（同款早退，判**不同族**，见 §七·6）。

---

## 三、key 不撞车核对表（已有三组 key vs 我新加的）

| 口 | 方法 | **`which`（= key 的头一段）** | 现读 |
|---|---|---|---|
| A476 | `AlignLeftOn` | **`左对齐`** | `Battle/Label.cs:864` |
| A476 | `AlignRightOn` | **`右对齐`** | `:874` |
| A491 | `SetAlignLeft` | **`逐行左对齐`** | `:333` |
| **A545** | `SetWrapWidth` | **`折行宽`** | `:253` |
| **A545** | `SetWrappingMode` | **`换行模式`** | `:409` |
| **A545** | `ForceRelayout` | **`重排`** | `:458` |
| **A545** | `SetFontSize` | **`字号`** | `:508` |
| **A545** | `SetAutoFitBox` | **`自适应框`** | `:644` |

- **key 的完整形状** = `which + "|" + 节点全路径`（`Battle/Label.cs:934`，**只此一份**，本件没加第二份）。
- `_dotAlignNoted` 是 `System.Collections.Generic.HashSet<string>`（`:938`）⇒ 判等是**全串相等** ⇒ 上面 **8 个字面量两两不同**即 8 组 key 两两不撞（`HashSet` 不做前缀匹配，`折行宽` 与 `折行模式` 这种「像」不算数）。
- 🔴 **断言层面也咬住了**：★①~★⑤ 是**在同一个探针节点上依次调**的 —— ★① 刚消费掉 `折行宽|A545 dot probe`，★② 还能响 ⇒ 两口 `which` 不同；★② 到 ★⑤ 同理。**「5 条全响」这一件事本身就等于「5 个 key 两两不同」**（换个节点就**测不出撞车** —— 这是 A491 ★② 那条经验的推广）。
- ⚠️ **同族还有一个第 9 个出声口 `SetCharSpacing`（`:524-533`）不走这张表**（它用自己的 `Debug.Log`、不去重）⇒ 见 §八·1，本件**没动它**。

---

## 四、那两句 doc 改成了什么

**① `ForceRelayout` 的方法头**（原 `:421`，现读 `:447-451`）

```
改前：/// <para>⚠️ 点阵后端（`_tmp == null`）没有折行/mesh 这回事 ⇒ 什么都不做（如实，不假装）。</para>

改后：    /// <para>🔴 **2026-10-13（A545）就地订正（铁律 5）**：本行原文写「点阵后端（`_tmp == null`）没有
          /// 折行/mesh 这回事 ⇒ **什么都不做（如实，不假装）**」—— 前半句是事实，**后半句是自陈静默**：
          /// 调用方以为「版面已经推下去了」，日志里却一个字都没有 ⇒ 与 `CLAUDE.md` §三「不许静默失败」打架。
          /// 现在**出声**后返回，**口名 / key 头段 = `重排`**（⛔ 别改成别的口的名字，理由见 `NoteDotAlign`）。
          /// 断言 → `Editor/SettingsScene.cs` 的 A545 那一节。</para>
```
（**保留原文引用** —— 铁律 5 要留更正痕迹；⚠️ 上面「什么都不做（如实，不假装）」这一串在本文件里现在**只剩这一处引用**，实测 `grep` 1 命中。）

**② `SetFontSize` 的方法头**（原 `:459`，现读 `:493-500`）

```
改前：/// ⚠️ 调它之前 `_tmp` 必须已经建好（`Create` 之后、且 `TmpFont.Available`）；
      /// 没字体资产时会静默无效（那本来就走点阵后端，点阵只有整数档 —— 见 `SetSizes`）。

改后：    /// ⚠️ 调它之前 `_tmp` 必须已经建好（`Create` 之后、且 `TmpFont.Available`）。
          /// <para>🔴 **2026-10-13（A545）就地订正（铁律 5）**：本行原文写「没字体资产时会**静默无效**」
          /// （…）—— 「静默无效」**是自陈静默**，与 `CLAUDE.md` §三「不许静默失败」打架 ⇒ 现在**出声**后返回
          /// （**口名 / key 头段 = `字号`** …）。
          /// ⚠️ **传非正数那一支【照旧】不出声**：那是**无效入参**（没有字号可设），不是「这一档功能不存在」
          /// —— 为了这句话**严格成立**，无效入参那道闸**必须排在 `_tmp == null` 之前**（…）。
          /// 断言 → `Editor/SettingsScene.cs` 的 A545 那一节。</para>
```

**③ 另外三处 doc 是【新增】A545 段**（它们原来只「描述行为」或根本没提出不出声）：

| 文件:行 | 新增段的核心内容 |
|---|---|
| `Label.cs:241-247`（`SetWrapWidth` 头） | 「折行这一档根本不存在 ⇒ 必须出声」＋ **口名 = `折行宽`** ＋ ⛔ 别改成别的口的名字（撞 key = 静默复发） |
| `Label.cs:399-404`（`SetWrappingMode` 头） | **就地订正**原文那句「点阵后端**直接返回**」（只描述行为）＋ 口名 = `换行模式` ＋ ⛔ **别与 `SetWrapWidth` 的 `折行宽` 合并**（`SetAutoFitBox` 内部会调 `SetWrapWidth` ⇒ 合并 = 后响的那个永远静默）＋ 传非法档那一支照旧走 `LogWarning` |
| `Label.cs:632-638`（`SetAutoFitBox` 头） | 「这一档整个不存在（框 / 折行 / 自适应三样都没有）⇒ 必须出声」＋ 口名 = `自适应框` ＋ 调用点数（**生产 39**） |

**④ 助手 doc**（`:907-929`）：「三个调用点」→「**全部 8 个口**共用它」＋ **`which` 全表**（8 个）＋ `why` 参数的由来（**A476/A491 两条消息逐字节没变**）＋ ⛔「名字里的 `Align` 是**历史**，别再为别族另开一只」＋ ⚠️ 现行例外 `SetCharSpacing` 如实登记。

---

## 五、共用件影响评估（「有字体资产时是否逐位为零」的独立复核）

**结论：是逐位为零。** ⛔ 不是「照 W-E5 的结论抄一遍」—— 本件**自己机械核了一遍**：

**核对法（可复现，零 Unity）**：取 `git show HEAD:…/Battle/Label.cs` 与工作区版，逐方法取出方法体，剥掉注释行/空行，再把**以 `_tmp == null`（或 `worldSize <= 0f`）开头的那条守卫连同它所在的 `if` 块整段删除**，然后逐行比较：

```
=== SetWrapWidth     剥掉守卫之后逐行相同: True
=== SetWrappingMode  剥掉守卫之后逐行相同: True
=== ForceRelayout    剥掉守卫之后逐行相同: True
=== SetFontSize      剥掉守卫之后逐行相同: True
=== SetAutoFitBox    剥掉守卫之后逐行相同: True
=== 全 5 处 TMP 路径逐行相同: True
```

⇒ **有字体资产（= 现在所有自检、所有生产路径）时：一行日志都不多、行为逐位为零** —— 与 W-E5 对 A491 的结论一致，且这次是**5 个方法一起**核的。

**影响面（没有字体资产那一档，`TmpFont.Available == false`，判据 `Core/TmpFont.cs:52`）**：

- 每个「**口 × 节点全路径**」打**一行** `Debug.Log`（**每处一次**，不是每次调用一行）—— 与 A476/A491 同一个粒度，理由同那两条（调用点多 + 每窗重建一次就跑一遍，逐次刷屏会把别的告警淹掉）。
- **会不会污染既有断言**：全仓挂 `Application.logMessageReceived` 的宿主 = **7 个文件 / 23 处注册**（`Core/ClickLog.cs` · `Editor/{BattleScene,CollectionScene,RewardsScene,SettingsScene,ShellScene,ShopScene}.cs`），逐个核过**都是窄作用域 + 按类型/子串过滤**；更关键的是 **`grep "AddComponent<Label>"` 全仓只有 3 处**（`Label.Create` 内部那一处 + A491 的探针 `SettingsScene.cs:1539` + 本件的探针 `:1613`）⇒ **没有任何一条既有自检会造点阵后端 `Label`** ⇒ 这 5 行新日志在既有断言里**一次都不会出现**（字体资产在时根本不执行那一支）。
- **新增的唯一依赖**：这 5 个口现在依赖同文件私有的 `NoteDotAlign` + 静态 `HashSet`（已过类型检查）。

**生产调用点计数**（`\.<方法>(` 去掉注释行、再去掉 `Battle/Label.cs` 与本文件；`Editor/` 那些算自检探针）：

| 方法 | 生产 | 自检探针 | 备注 |
|---|---|---|---|
| `SetWrapWidth` | **10** | 2 | `MainMenuRuntime` 2 · `MenuDraw` 2 · `PromptPopup` 2 · `DeckRuntime` 1 · `MatchLogRow` 1 · `PlayerProfileWindow` 1 · `SettingsWindow` 1 |
| `SetWrappingMode` | **3** | 2 | `DeckRuntime` 1 · `CollectionWindow` 1 · `ProfileTab` 1 |
| `ForceRelayout` | **4** | 2 | `DeckRuntime` 1 · `InboxWindow` 1 · `MainMenuRuntime` 1 · `PracticeModePopup` 1 |
| `SetFontSize` | **0** | 0 | **外部一处都没有**；唯一调用点是 `ForceRelayout:463` 内部那一处 |
| `SetAutoFitBox` | **39** | 10 | 最多的三个文件：`MainMenuRuntime` 5 · `ShopWindow` 4 · `DeckRuntime` 4 |

---

## 六、断言清单（断什么 · 用的探针名 · 改坏法 · 落点）

**落点**：`Editor/SettingsScene.cs:1592-1702`（`Section("A545：…")`），位置 = **A491 那一块（`:1512-1590`）之后、A167 那一节（`:1704`）之前** —— 该处**不在任何 `if` 守卫里**（`Run()` 体里的自由语句，同 A491/A167），且**不依赖任何窗口对象**。W-E5 那 6 条**一字未动**。
**宿主自检**：`SettingsScene.Run`（11 条里的第 10 条，实测 ~10s）。
**探针名（本次运行第一次出现）**：`A545 dot probe`（点阵后端）· `A545 tmp probe root` / `A545 tmp label`（TMP 后端顶）。⚠️ 点阵那棵与 TMP 那棵**不同名** —— 同名会让两条「节点全路径」撞在一起（key 是**全路径**，不是节点名）。

| # | 行 | 断什么 | **怎么分辨两种状态** | **改坏法（怎么改就红）** |
|---|---|---|---|---|
| 前提① | `:1614` | 探针落在**点阵后端**（`CanRenderChinese == false`） | —— | 夹具坏了（万一建出 TMP）⇒ 红 |
| **★⓪** | `:1622` | **夹具自检 + A476 回归**：`AlignLeftOn(1f)` 出声（消息含 `点阵后端没有对齐这回事`） | 同一只网、同一次调用里抓到的行数（打印出来了） | 网坏了 / 夹具坏了 ⇒ 红（**先把责任分给网，别去改那 5 个口**） |
| **★①** | `:1628` | `SetWrapWidth(7.5f)` 在点阵后端下**出声**（锚 `点阵后端不会折行`） | 出声 / 不出声两态 | 删掉那句 `NoteDotAlign(...)` ⇒ 红 |
| **★②** | `:1635` | `SetWrappingMode(3)` **出声**（锚 `点阵后端只有「单行」这一档`） | 同上；**它同时是「与 ★① 不撞 key」的判据**（★① 刚在同一节点上消费过 `折行宽`） | ① 删掉出声 ⇒ 红；② **把 `which` 抄成 `折行宽`** ⇒ ★① 吃掉它 ⇒ 红（**静默复发**） |
| **★③** | `:1644` | `ForceRelayout()` **出声**（锚 `点阵后端不经过 TMP 排版`） | 同上 | 删掉出声 ⇒ 红 |
| **★④** | `:1652` | `SetFontSize(4f)` **出声**（锚 `点阵后端没有 TMP 的 fontSize`） | 同上（⚠️ 传的是**正数** —— 非正数那一支**故意不出声**） | ① 合成原来那句 `if (_tmp == null \|\| worldSize <= 0f) return;` ⇒ 红；② 删掉出声 ⇒ 红 |
| **★⑤** | `:1660` | `SetAutoFitBox(5f, 2f, 10f, 30f)` **出声**（锚 `点阵后端既不会折行、也没有自适应`） | 同上 | 删掉出声 ⇒ 红 |
| 前提② | `:1672` | `TmpFont.Available`（★⑥ 要的就是「有 TMP」那一档） | —— | 没字体资产 ⇒ 红（**同族前提**：A228 那一节也会红，所以本条的加入不改变「谁先红」） |
| 前提③ | `:1680` | ★⑥ 的探针确实是 TMP 后端（`CanRenderChinese == true`） | —— | 夹具坏了 ⇒ 红 |
| **★⑥** | `:1693` | **反面对照**：**有 TMP** 时这 5 个口**一行 `[Label]` 都不出**（`a545tmpLogs.Count == 0`） | 🔴 **谓词与 ★①~★⑤ 逐字相同**（`Contains("[Label]")`）⇒ 两边量的是同一件事；同时把「本趟共抓 N 行」也打印出来 | 把任一句 `NoteDotAlign(...)` 搬到 `if (_tmp == null)` **外面** ⇒ 红 |

**🔴 为什么正反必须成对**（本工程那条系统性毛病「弱断言分不出两种状态」）：

- 只断「点阵后端出声」⇒ **无条件出声**（把那句 `Debug.Log` 搬到 `if` 外面）照样绿 —— 而那会让有 TMP 的那几万次调用也刷屏；
- 只断「有 TMP 时不出声」⇒ **把出声整句删掉**照样绿（**那正是 A545 要修的原始状态**）。
⇒ ★①~★⑤ ＋ ★⑥ 合起来才分得出**三态**：**条件出声 / 永远出声 / 永不出声**。

**灭自证（防「两边一起改回去」）**：★①~★⑤ 与 ★⑥ 用的**不是同一条判据的两种写法** —— 前者是「点阵后端**必须响**」、后者是「TMP 后端**必须不响**」，**夹具不同（两棵不同名的树）、期望相反** ⇒ 没有任何一种「单向改坏」能同时满足两边。而「key 撞车」这条**不能**靠 ★⑥ 发现，它是靠 ★①~★⑤ **在同一个节点上依次响**咬住的（换个节点就测不出来）。

**⚠️ 「本次运行没出现过的 id」那条坑（`资料/已知的坑.md:4037`）已照做**：

- `_dotAlignNoted` 是**进程内静态**、key 含**节点全路径** ⇒ 探针节点名必须是**本次运行没出现过的**：本案用 **`A545 dot probe`** / **`A545 tmp label`**（全仓唯一，本趟第一次出现）⇒ 不会命中跑过的 key（假红）。
- 每个口在**同一条断言里只调一次**（★①~★⑤ 各一次）⇒ 不触碰「同一进程里同一 key 只有第一遍才响」那条（第二次调必然 0 行）。
- ★⑥ 那棵树的节点名与点阵那棵**不同名**（理由见上）。

---

## 七、没查清 / 没做的

1. 🔴 **断言没跑**（本轮口径：A 表清完再跑自检；⛔ 本件不许跑 Unity）⇒ 本件是**静态核对 + 类型检查 0/0**，**不是「跑绿了」**。要跑的就是 **`SettingsScene.Run`**。**这一条要如实转述给用户。**
2. ⚠️ **通过数会变**：本件给 `SettingsScene.Run` 加了 **10 条 `CheckTrue`** ⇒ 收口跑那一趟的「断言合计」会比上一轮 **+10**（若都过）。**别把 +10 当成异常**。
3. ⚠️ **`CaptureLogs` 在批处理里真能收到 `Debug.Log` 这件事，本件没有实跑证据**（那是 W-E5 加的那只网）。依据是：① `Editor/BattleScene.cs:208-210` 已有一条**在跑且全绿**的同族计数器（`Application.logMessageReceived += dwCounter`）；② `Application.LogCallback` 收**全部** log 类型。⚠️ **判据提示**：万一 ★⓪ 也红，**先怀疑这只网**，别去改 `Battle/Label.cs`。
4. **没做的（本件范围外，都不是「不做」）**：`SetAlignLeft` 那条「要在量尺寸之前调、代码里没有守卫」= **A546 ②**（W-E5 顺手发现④，本件没碰）；`Editor/ChatBoxProbe.cs` 里那三处探针（`SetAlignLeft` / `SetWrapWidth` / `SetAutoFitBox`）**一个字没动**。
5. ⚠️ **A492 同口**：`Shell/LiveOpsEventWindow.cs:733` 的「`SetAutoFitBox` 排在 `SetCharSpacing` 之前」是本件 `SetAutoFitBox` 的**同类口**，但**改动在 `Shell/` 那一侧**（W-E6 的账）⇒ 本件与它**零文件交集**，没有撞车。
6. ⚠️ **`RefreshBounds()`（`:798-800`）也是同款 `_tmp == null` 早退，本件【判不同族】、没碰** —— 理由：它对点阵后端**不是「缺这一档功能」**（点阵侧的尺寸由 `SetTextDot` → `RebuildMesh` 自己维护，`WorldW/WorldH` 走 `_texW/_texH`）⇒ 调它没有「调用方以为生效、其实没有」的语义。**这是我的判断，请主对话复核**；若将来有人把点阵侧的尺寸改成惰性重算，这一处要一起看。
7. ⚠️ **`SetAutoFitBox` 内部那两条 `return`**（`cur <= 0f || minPx <= 0f || maxPx <= 0f` 与 `nomPx <= 0f`）**没动**：同属「入参不成立」那一族（与 `SetFontSize` 的 `worldSize <= 0` 同类），本件按 A545 的边界只处理「后端缺这一档」。⚠️ 没查过有没有 `minPx = 0` 的调用点（生产上够不到：`CapWorld ≥ 0.07` ⇒ `cur > 0`）。

---

## 八、顺手发现（⛔ 一个都没改）

1. 🔴 **同族还有【第 9 个】出声口，它不走 `NoteDotAlign`**：`SetCharSpacing`（`Battle/Label.cs:524-533`）—— 它 2026-10-03 就出声了，但走的是**它自己的 `Debug.Log`**，而且**不去重**（每次调用打一行）。
   ⇒ 「**key 的拼法只有一份**」这句话**今天并不完全成立**（两套消息模板、两套去重策略）。收编它 = 改它的消息 + 改它的刷屏行为 ⇒ **不在 A545 范围**（简报只给了 5 处），**另立一条账**。本件只把这个事实**如实登记**进 `NoteDotAlign` 的 doc（`:927-929`）。
2. 🟡 **`SetAutoFitBox` 内部两条静默 `return`**：见 §七·7 —— 同属「入参不成立」，**没查过**有没有调用点会撞上（生产够不到，探针里也没有）。要立账的话判据是「`minPx`/`maxPx`/`cur` 有没有可能 ≤ 0」。
3. 🟢 **`Battle/CardDisplayWindow.cs:379` 那条注释照旧成立**（「⚠️ 不能只调 `Label.SetAlignLeft()`」）—— 它讲的是**另一件事**（`SetAlignLeft` 只管 TMP 的 `alignment`、**不管块位置**）；**出声之后这条仍然对**，别误删。
4. 🟡 **助手名字 `NoteDotAlign` 现在是【历史名】**：它管 **8 个口**，其中只有 3 个是「对齐」。本件在它的 doc 里写明「名字里的 `Align` 是历史」但**没有改名** —— 改名要牵动 `Editor/SettingsScene.cs:246` 那处引用与别处文档，**另立账更省**。
5. 🟡 **`Editor/SettingsScene.cs` 的 A228 前提注释**（A491 段里，`:1523-1526` 那一带）**本件没改**：那是 **A546 ①**（W-E5 顺手发现④）—— 两笔账**都指向同一个文件**，本件特意**只增不改**（新块自包含、成对可回退）。

---

## 九、类型检查结果

```
TMPDIR=/tmp/wf_wlabel bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- **跑了 3 次**（改完 `Label.cs` 之后 / 加完断言之后 / 最后一次）——**三次都是 0 / 0**。
- ⚠️ 按简报口径注明：**当时树里有别人的未提交改动**（`Battle/BattleDriver.cs` · `Battle/ScenarioBlendables.cs` · 各 `Shell/*` · `Deck/DeckRuntime.cs` · `Editor/{BattleScene,CollectionScene,DeckScene,MainMenuScene,RewardsScene}.cs` 等）—— **没有一条错落在别人的文件上**，所以没有触发简报里那条「重跑 + 如实记」。
- 本件自己的只读核对：`git diff --numstat`（**只读命令**）
  · `Battle/Label.cs` **115 / 12**（其中 W-E5 的 A491 是 **30 / 3** ⇒ **本件 ≈ 85 / 9**）
  · `Editor/SettingsScene.cs` **207 / 0**（其中 W-E5 是 **95 / 0** ⇒ **本件 112 / 0**）
- 行尾自检（**二进制读**，判据 = `b.count(b'\r\n')` 对 `b.count(b'\n')`）：
  · `Battle/Label.cs` **CRLF 1055 · LF 1055**（**纯 CRLF**，改前 979 ⇒ 一行没翻）
  · `Editor/SettingsScene.cs` **CRLF 0 · LF 2636**（**纯 LF**，改前 2524 ⇒ 一行没翻）
- 改动**全部走 Edit 工具**（⛔ 没用 `sed -i`、没用 python 写盘）。

### 附：§五 那段机械复核的脚本（可复现，零 Unity）

```python
import io, subprocess, difflib
path = 'Unity/MyGame/Assets/CardPresentation/Battle/Label.cs'
head = subprocess.run(['git','show','HEAD:'+path], capture_output=True, cwd='d:/4'
                      ).stdout.decode('utf-8').replace('\r\n','\n').split('\n')
work = io.open('d:/4/'+path,'rb').read().decode('utf-8').replace('\r\n','\n').split('\n')

def strip(ls):                       # 去注释行 + 空行
    return [l for l in ls if l.strip() and not l.strip().startswith(('//','///'))]

def body(ls, sig):                   # 按大括号配平取方法体
    for i,l in enumerate(ls):
        if sig in l:
            d = 0; res = []
            for j in range(i,len(ls)):
                res.append(ls[j]); d += ls[j].count('{') - ls[j].count('}')
                if d == 0 and j > i: return res

def cut(b):                          # 整段删掉「_tmp == null / worldSize <= 0f」那条守卫
    out = []; i = 0
    while i < len(b):
        l = b[i]
        if ('_tmp == null' in l) or ('worldSize <= 0f' in l):
            if '{' in l:                                  # 守卫自带开括号
                d = 0
                while i < len(b):
                    d += b[i].count('{') - b[i].count('}'); i += 1
                    if d == 0: break
            elif i+1 < len(b) and b[i+1].strip().startswith('{'):   # 括号在下一行
                i += 1; d = 0
                while i < len(b):
                    d += b[i].count('{') - b[i].count('}'); i += 1
                    if d == 0: break
            else:
                i += 1                                    # 单行 `if (…) return;`
            continue
        out.append(l); i += 1
    return out

for sig in ['public void SetWrapWidth(float worldWidth)', 'public void SetWrappingMode(int originalMode)',
            'public void ForceRelayout()', 'public void SetFontSize(float worldSize)',
            'public void SetAutoFitBox(float worldW']:
    print(sig, '=>', cut(strip(body(head,sig))) == cut(strip(body(work,sig))))   # 全 True
```

---

## 红线核对

⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用了只读的 `git status` / `git diff` / `git show` / `git diff --numstat`）
· ⛔ 没改 `CLAUDE.md` / `项目任务.md` / 任何正本 · ⛔ 没越白名单（写只落在 `Battle/Label.cs` 与 `Editor/SettingsScene.cs`）
· ⛔ 没碰 W-E5 那 6 条断言（**只在其后追加**，新块自包含）· ⛔ 没自己顺手改任何「顺手发现」
· ✅ 改完**立刻**跑了类型检查（3 次 · 独立 `TMPDIR=/tmp/wf_wlabel`）⇒ **0 / 0** · ✅ 行尾二进制复核过
