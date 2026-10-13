# W2 —— `A1594`（WillRun 两份实现 / ④ 档补断言）+ `A1412`（三条断言落地）

会话：第二十五会话 · 日期：2026-10-13 · 写手：W2（执行代理）
白名单内改动的文件（只有这三个）：
`Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` · `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_S14.cs` · `Unity/工具/ruleprobe/WillRun.cs`

---

## 件一（`A1594`）：`WillRun` 判据的**两份实现**

### ① 选了哪一种做法、为什么

**选了「退而求其次」那一档**（注释写死权威 + ④ 档补真断言），**并且顺手把一处真分叉收成了同一份判据**。

为什么**不能真收成一份**（这是事实，不是取舍）：
`工具/ruleprobe/ruleprobe.csproj:14-29` 只编 `RuleEngine/Core/*.cs` + `工具/ruleprobe/` 自己那几个文件；
而 `RuleEngineTest.cs` 那一份住在 Unity 的**编辑器程序集**里，并且**裸用 Unity 类型**
（`Debug.Log` / `Resources.Load` / `JsonUtility` —— 见 `RuleEngineTest.cs` 的 `ReportWillRunMechanism` 与
`LoadRawKeywordEntries`）⇒ **两边互相看不见**，没有任何一个 csc 能同时编到这两份实现。
（`RuleEngineTest.cs` 里那段 `[A1440-PURE-BEGIN]` 只保证「没有 Unity 类型」，**不**保证「探针编得到它」。）
⇒ 真要走「一份」，出路是把它搬进 `RuleEngine/Core/`（两个工程都编那一份）—— 那要动 `Core/`，**不在本轮白名单**，
已写进下面「需要别人改的」。

### ② 改了什么（`文件:行号`，行号为改后）

1. **`RuleEngineTest.cs:9082-9103`**（`ReportWillRunMechanism` 的 doc 尾部，接在原来那句
   「探针与本法是两条腿的两次实现」**之后**）—— 新增 `A1594` 那一段：
   写死「**这一整段是 `工具/ruleprobe/WillRun.cs` 的移植副本、权威在那边**」+ **为什么收不成一份** +
   **两处已查实的分叉**：
   ① ④ 档比法（探针 `A1593` 起**按通道比** `ClaimChannel`，本法还是**全卡 `op` 签名并集** ⇒ 本法结构上会少报，
      实例 = 探针 §⑦ 断言 A/B 的 `SOR28` / `SOR36`）；
   ② ② 判别式（探针用引擎 `ParseSegment`，本法原来是**头正则**，漏 `Oath N:`）
   + **改动纪律**（改一侧必须同时改另一侧、两边都要报读数）。
2. **`RuleEngineTest.cs:9515`**（`WRSegPaid` 这个常量的注释）—— 它原来写
   「`N ☀:` 形状 …… `WillRunOps` 的 unit/hero 支**压根不看**这类段」，**这句今天不成立**（`Oath N:` 走 `OathOps`、
   单位支是看的）⇒ 就地订正为「**引擎认的付费前缀（`N <货币>:` / `Oath N:`）而它自己那条货币通道没收下来**」；
   同一句错话还在 `:9068`（方法头 doc）与 `:9261`（④ 的报表正文）各一份，**三处一起改**（铁律 5）。
3. **`RuleEngineTest.cs:9570-9581`** —— 新增 `static int WillRunPaid(string raw, out string kind)`
   （判据 = **引擎自己的 `EffectText.ParseSegment`**，与探针的 `AddFaceBody` 同一份）。
   它是「**收成一份**」这一次唯一真正做到的那部分：**两边的判据不再是两条规则**（都读引擎那一次 `ParseSegment`）。
4. **`RuleEngineTest.cs:9526-9543`**（`WillRunFace` 三个新字段，类在 `:9526`）+ **`:9716-9726`**（`AddWillRunFace` 尾部算出它们）—— 新增
   `PaidCost` / `PaidKind` / `Currency` 三个字段（**照探针的 `FaceBody` 逐字同形**），
   并在 `AddWillRunFace` 里算出来（原来直接 `into.Add(new WillRunFace { … Ops = ops })`）。
5. **`RuleEngineTest.cs:9824-9835`**（`ClassifyWillRunFace` ②）—— `if (Regex.IsMatch(b.Head, "^\d+\s*[\[\(]?\s*[a-z\u2600]"))`
   **换成** `if (b.PaidCost > 0)`，`Why` 文案照探针的 `Classify` ② 改（带货币名）。
6. **`RuleEngineTest.cs:9239-9251`（④ 段级）与 `:9312-9317`（⑤ 无头档）** —— **把「判据行」从 `Debug.Log` 变成断言**：
   `Check(totalMissed,0)` / `Check(nMiss,0)` / `Check(nPaid,0)` / `Check(headNone,0)`。
   这四条正是本方法末尾那句 `Debug.Log` 自己写的那条判据（原来**一条断言都没有** ⇒ `WillRunOps` 变了它不会红）。
7. **`RuleEngineTest.cs:9402-9459`** —— 新增 **哨兵④**（`Func<string,string,CardDef>` 现造夹具卡，**不进卡池**）：
   - ④-a `Rally: Deal 1 damage. 2 [Energy]: Gain 1 Quest Point` ⇒ 腿 A **1** 条 · 孤儿 **1** 条 ·
     档位 **付费段** · `PaidCost == 2`（**灭自证**：那半句的货币是 `energy`、单位支**没有这条通道** ⇒ 结构上收不到）；
   - ④-b `Rally: Deal 1 damage. Rally: Deal 3 damage` ⇒ 孤儿 **1** 条 · 档位 **同头两写**；
   - ④-c `WillRunPaid("Oath 2: …") == 2`（**引擎判据**）＋ **同一行并排量出旧头正则给它相反答案**
     （`"oath 2"` 不匹配那条正则）＋ **反例**（`"Gain 1 Quest Point"` 必须给 0，否则判据恒真）。
     ⚠️ ④-c **只能这么钉**：`Oath N:` 的 op 会被 `CardDef.CollectOathOps` 收进 `OathOps` ⇒ 腿 A 必然收到
     ⇒ 它**永远不会是孤儿** ⇒ 走不到 `ClassifyWillRunFace`（**卡片级夹具做不出来**，如实标着）。
8. **`工具/ruleprobe/WillRun.cs:51-66`**（文件头 doc 尾部）—— 反向那一半：写死「本文件是**权威**那份 +
   对面那份移植副本在哪 + 为什么收不成一份 + **同样的两处分叉** + 改动纪律」。
9. **`工具/ruleprobe/WillRun.cs:572-610`**（`SelfCheck` 的 **断言 D**）—— 把**上面 ④-a / ④-b 的两个夹具
   在探针侧再量一遍**，并要求**旧口径（全卡并集）也报 1 条**（= 两份实现今天都能判对）。
   ⚠️ 挑 `Gain 1 Quest Point`（而不是探针断言 A 的 `Deal 1 additional damage`）**正是为此**：
   后者在并集比法下会被 `Rally:` 那条顶成「已收到」⇒ **副本拿不到那个形状**。

### ③ 判据与出处

| 改动 | 判据出处 |
|---|---|
| ② 判别式改用 `ParseSegment` | `WillRun.cs` 的 `AddFaceBody`（`:991-1001`，`A1593`）＋ `Classify` ②（`:1285-1302`）；**引擎侧** `EffectText.cs:2105 ParseSegment` / `SegResult.PaidCost:1602` |
| 「头正则漏 `Oath N:`、`UM*` 那 18 张被误判」 | `WillRun.cs:1310-1328`（`IsPaidHead` 的 `A1593` 就地订正）＋ 本次现跑 `ruleprobe willrun` §⑧ 的输出行「反面对照（有尾数字但**不认**）：`oath 1`（UM82）… 共 28 种」 |
| ④/⑤ 四条池断言 | 本方法自己那句 `Debug.Log`（`RuleEngineTest.cs:9347-9350`）＋ 探针 `willrun` 的 rc 判据行（`WillRun.cs` 末尾那段） |
| 「并集比法 ⊆ 通道比法」 | `WillRunOps` 的七个来源本身就含 `TriggerOps` / `FaithOps` / `OathOps` ⇒ 通道那几条**都在** leg A 里（`RuleEngineTest.cs:9654` 那行注释同一件事） |

### ④ 读数（**都现跑过**）

```text
bash d:/4/Unity/工具/ruleprobe.sh willrun      [2026-10-13 18:33 · rc=0]
  ④ 卡面带正文的段 326 条 · 已收到 326 条 · 没收下来 0 条（0 张卡）· 收漏 0 · 付费段 0 · 同头两写 0
  ⑤ 无头段 108 条（事件层 81 · 回合起止 19 · 光环 7 · 降费触发器 1）· 🔴 无消费点 0 条
  ② 收漏了 0 张
  ⑦ 自检：A/B/C 全过 ✅ ＋ 🆕 断言 D：
      ✅ D「FX-D1」`Rally: Deal 1 damage. 2 [Energy]: Gain 1 Quest Point`
           实得 腿 A 1 条 · 旧口径孤儿 1 条 · 付费段孤儿 1 条 · PaidCost 2     ← 与我这四条新断言**逐条对上**
      ✅ D「FX-D2」`Rally: Deal 1 damage. Rally: Deal 3 damage`   实得 1 条 · 旧口径 1 条
bash d:/4/Unity/工具/ruleprobe.sh check        构建「已成功生成」· 解析差异 0 行 · 哨兵失败 0 · rc=0
TMPDIR=/tmp/wf_w2{,_2,_3} bash d:/4/Unity/工具/typecheck.sh   三次都 0 错（运行时 0 / 编辑器 0）
git diff --stat:  RuleEngineTest.cs +173/-11 · RuleEngineTest_S14.cs +42 · WillRun.cs +57
行尾：RuleEngineTest.cs 仍 **纯 CRLF**（28166/28166）· 另两个仍 **纯 LF**（2377/1415）
```

### ⑤ 没查清的部分（如实标着）

1. ⛔ **没跑 Unity**（红线）⇒ 那 4 条池断言与哨兵④**只在纸上与探针侧对上**，`RuleEngineTest.Run` 里是不是真绿，
   要主对话在同步点跑一次才知道。⚠️ 风险点已逐个量过（见 ④ 的 D 行），但**不能替代实跑**。
2. ⚠️ **④ 档的比法分叉没修**（探针按通道、副本按并集）。本轮只把**档位判据**收成一份，
   **比法**仍是两套 —— 因为照 `ClaimChannel` 补一份到副本里，是给副本再加一份**探针侧代码的复制品**
   （`ResolveTriggerKey` 那套「近似引擎私有 `StripParamSuffix`」的写法**正是** `A1598②` 记过的那种负担）。
   正解是搬进 `Core/`（见下）。⇒ **今天两份读数的差距 = 「并集多顶掉的那几条」**（探针实测 0 条可达）。
3. ⚠️ `RuleEngineTest.cs` 里那段 `[A1440-PURE-BEGIN]` 块**改了**（加 `WillRunPaid` + 三个字段 + ② 判据），
   而**仓库外**那份构造性实测副本（`D:/tmp/wf_a1440/*`）**没有跟着抽**（不在白名单、也不是我的活）
   —— 它下次抽的时候会带上新代码，**在那之前它测的是旧块**。谁要用它请重抽。

---

## 件二（`A1412` 尾巴）：三条断言落地

### 改了什么

`Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_S14.cs`，**全部落在**已有的
`TestS14CostSharedChargesOnce`（`:1464`）里，⛔ **没有新增 `Test*` 方法**：

1. **`:1510-1521`**（插在原来 `:1509` 那行 `CheckTrue(S14Log(ctx, "不再收"), …)` **之后**）——
   新增一行断言：
   `CheckTrue(S14Log(ctx, "「lower its cost by 2 and give it flank」的 2 点能量"), "★★ 判别式（A1412）…")`。
   原句 = `SendMessage...`；新句紧跟在它后面，断言「**连付钱那条自己也走「已经收过」那一支**」。
2. **`:1550-1581`**（插在 SOR6 那块的收尾 `}` **之后**、方法收尾 `}` 之前）—— 两个新夹具块：
   - `W11_OATH`（`Oath 2: Lower its cost by 2 and give it Flank.` 挂在**单位**上 ⇒ 走 `UseOathAbility`）：
     能量 1 < `Oath 2` ⇒ `UseOathAbility` 回 `ErrUnimplemented` · **一分钱不扣** · **次数不涨** ·
     **`!u.Has("flank")`（判别式：标着 `CostShared` 的后半句也不许落地）** · 日志里有「一并跳过」；
   - `W11_SLOT`（`PlayCard(…, out int landedSlot)`）：空盘请求格 2 ⇒ `landed == 3` 且 `SlotOf` 与之一致。

### 判据与出处（⛔ 没把期望值改松，一处都没动）

- 三条补丁**逐字**取自 `资料/普查产出_第二十二会话/W11_引擎A1597A1412.md` §三（写手 `W11` 交件），
  该节同时写了「**怎么才能红**」：删 `ChargePaidGroups` / 删 `ok[j]=false` ⇒ `!u.Has("flank")` 红；
  `landedSlot` 改回请求格 ⇒ `Check(landed,3)` 红；改回逐 op 收 ⇒ 最后那条 `S14Log` 红。
- 期望值的**引擎侧出处**（我逐条现核过，不是照抄）：
  - `UseOathAbility` 付不起 ⇒ `RuleCore.cs:7105-7119`（`ResolveOathAbility` 回 false ⇒ **不消耗次数**、
    回 `ErrUnimplemented`、**一条 `EvtKind.Ability` 都不发**）—— 三条断言（能量 1 / 次数 0 / 返回码）与它一致；
  - `!u.Has("flank")` 的判据 = `EffectResolver.cs:6607-6632` 的 `ChargePaidGroups`（付不起 ⇒ `ok[i]=false`
    ⇒ 调用方**整组跳过**），「一并跳过」那句日志在 `:531-536`（⚠️ **只在组 >1 条时打**）；
  - `「lower its cost by 2 and give it flank」的 2 点能量` 这个串 = `EffectResolver.cs:679-680` 打的那句，
    其中 `{op.Source}` 与 `{shown}` 我**现跑 `ruleprobe seg`** 核过：
    `seg "Oath 2: Lower its cost by 2 and give it Flank."` ⇒ `Source=[lower its cost by 2 and give it flank]`
    · `cost=2` · `ck=oath` ⇒ `shown` = 能量；
  - `landed == 3` = 原版 `MinionManager__AdjustedSlot.c:81-96`（`A1597` 那行：空盘请求 0/1/2 一律落 3），
    落点走 `BoardSlots.Insert` 回填、由 `RuleCore.cs:2256` 的 `out landedSlot` 报出。

### 读数

`TMPDIR=/tmp/wf_w2_3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
`RuleEngineTest_S14.cs` 仍纯 LF（0 CRLF / 2377 行）· `git diff --numstat` = **42/0**（纯插入，零删除）
⇒ **期望值一个都没被改松**。

### 没查清的部分

⛔ **没跑 `RuleEngineTest.Run`**（红线）⇒ 这 6 条断言的真绿假绿要主对话跑。
⚠️ 我**没能**独立验证的两处（只能引 `W11` 的读数）：① `W11_SLOT` 那张空盘夹具里
「请求 2 落 3」是不是在**这个夹具的棋盘状态**下成立（`W11` 的读数是全池 `play` 跑出来的）；
② `Check(u.OathUsesThisTurn, 0)` 里 `OathUsesThisTurn` 的初值 —— 现读 `RuleCore.cs:7085` 只在成功时 `++`，
`CanUseOathAbility` 用它比上限 ⇒ 初值 0 成立。

---

## 需要别人改的

1. 🔴 **把 `WillRun` 的判据搬进 `RuleEngine/Core/`**（`A1594` 的根治做法）—— 新开一个
   `Core/WillRunLens.cs`（纯逻辑：档位常量 + `Measure`/`Classify` 那一族，⛔ 无 Unity 类型），
   然后 ① `RuleEngineTest.cs` 的 `[A1440-PURE-BEGIN]` 那一块改成转调它；② `工具/ruleprobe/WillRun.cs`
   也只留 I/O。**为什么**：两个工程都编 `Core/*.cs` ⇒ 这才是「一份实现」；
   现在这版的注释（「两份、权威在探针、改动要同步」）只是**止血**。**判据齐了**：两边代码都在，
   逐段对照即可（分叉点只有两处，已写在 `RuleEngineTest.cs:9093-9101` 和 `WillRun.cs:58-66`）。
2. ⚠️ **`RuleEngineTest.cs:9082-9103` / `WillRun.cs:51-66` 这两段注释**将来谁再改 `WillRun` 的档位，
   请**同时**看这两处（我写的是当前状态，不是永久真理）。
3. ⚠️ **仓库外那份构造性实测**（`D:/tmp/wf_a1440/*`）要**重抽** `[A1440-PURE-BEGIN]` 块
   （本轮块内容变了：+`WillRunPaid` / +3 字段 / ② 判据）—— 见件一 §⑤·3。

## 顺手发现的（⛔ 只报告、未改）

1. ⚠️ **`EffectResolver.cs` 里「认不出的货币」那道闸仍然逐条过**（`A1412` 起组内 op 在调用方就被跳过）
   ⇒ 同一句日志由 N 条变 1 条（`W11` §六·4 已记，池侧 0 处可达）—— **与我的白名单无关，未动**。
2. 🔴 **探针侧没有「④ 档按通道比」到「副本侧按并集比」的差值断言**：今天两份读数的差距**恰好 0 条**
   （`SOR28` / `SOR36` 那两张已被 `A1593` 收口成「收到」）⇒ 「并集比法会少报」这件事**只在夹具上可观测**
   （探针 §⑦ 断言 A 的 `FX-A1` / `FX-A2`：旧口径 0 条 vs 新口径 1 条）。
   建议**另立账**：给副本补一条「同形夹具」的断言，把这份分叉钉住（本轮没做：那要先补 `ClaimChannel` 的移植）。
3. ⚠️ `RuleEngineTest.cs:606` / `:559` 记着一种**很省事的内环**：「期望值**全部先用离线探针真跑过一遍**
   （`D:/tmp/wf_a1564/`：`Stub.cs` + 只读引用 `Core/*.cs`）」。本轮我是靠**往探针 `SelfCheck` 里加断言 D**
   做到同一件事的（更省事：不用另起 scratch 工程，`ruleprobe.sh` 现编现跑）—— 建议把它**写进**
   `工具/ruleprobe/README.md` 的「怎么给新断言取期望值」一节（我没动 README：它不在白名单）。
4. ⚠️ `RuleEngineTest.cs` 的 `④` 报表里那句「（落在 N 张卡上）」在 `N=0` 时读起来像废话，
   不影响判据 —— 未改。
