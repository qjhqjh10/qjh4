# S10 · `Company Master` 降费（今天一次都不生效）

> 写手 `S10`。⛔ 没跑 Unity（只有调度台能跑）· ⛔ 没动 git · ⛔ 没改正本 · ⛔ 没跑生成器。
> ✅ 只改了白名单里的 **3 个文件**：`Core/RuleCore.cs` · `Core/EffectResolver.cs` · `Editor/RuleEngineTest.cs`。
> ✅ 每批改完都跑了秒级类型检查（`TMPDIR=/tmp/wf_s10`），**最近一次 0/0**。
> ✅ 另建了一个**离线探针**（`D:/tmp/wf_s10_probe/`，**不在仓库里**）做夹具验证 + **变异验证**
> （收编自 S7 的那份；加了 `s10cm` / `s10restore` / `s10slot` / `s10rubric` / `s10stale` 五条）。
> ✅ **在 S7 改完的基础上叠加**：`RuleCore.cs` 的 `DrawnThisTurn` 挪位（现 `:1160-1180`）与
> `EffectResolver.cs` 的注释订正（现 `:5091` 区域）**一个字都没动**（`git diff` 里逐 hunk 核过）。

---

## 1. 改了什么（逐处：文件:行号 · 改前 → 改后 · 一句话理由）

| # | 文件:行号 | 改前 → 改后 | 一句话理由 |
|---|---|---|---|
| **1** | `Core/EffectResolver.cs` **`:4682-4743`**（新增 `DrawReferentScope`） | 无 → 新增一个 `struct DrawReferentScope : System.IDisposable` + 静态工厂 `Seed(ctx, inst)`（**紧挨着 `EventSlotsScope` 放**，`EventSlotsScope` 在 `:4636-4680`） | 把「`draw` 广播期间的指代槽」做成**可复用的、异常安全的一层**（`using` ⇒ 异常也会还原）—— 与兄弟件 `EventSlotsScope` 同族同位置；**只动 `DrawnThisResolve` 一个槽**，所以和 `EventSlotsScope` 嵌套不打架 |
| **2** | `Core/RuleCore.cs` **`:1144-1154`**（`Draw` 里） | `BroadcastWhen(ctx, WhenEventKind.Draw, p, card, null);` → 同一句外面套 `using (DrawReferentScope.Seed(ctx, inst))`（前面加 **11 行注释**：判据 + 指针到 `DrawReferentScope` 的头注释） | 让**普通抽牌**的监听器也读得到「刚抽到的那一份」—— 改之前这条路上 `DrawnThisResolve` **一个写点都没有** ⇒ 降费永远读不到指代对象 |
| **3** | `Editor/RuleEngineTest.cs` **`:19780-19912`**（新增 `TestS10CompanyMasterLowerCost`） | 无 → 新增一个测试函数（**134 行**），两条断言各钉一半（见 §3） | 收口判据要「一条真行为判别式」；离线变异证明**单靠 ① 分不出还原** ⇒ 必须 ①+② 成对 |
| **4** | `Editor/RuleEngineTest.cs` **`:455-462`**（`Run()` 里） | 无 → 新增 `Section("🆕 \`Company Master\` 降费生效（…）")` + `Step(TestS10CompanyMasterLowerCost)`，**排在 S7 那一节之后** | 把新函数挂进宿主 |

**行为的净变化只有一处**：`draw` 广播**期间** `ctx.DrawnThisResolve` 的内容 = **恰好刚抽到的那一份**；
广播一结束立刻还原成调用方原来的值。**广播之外，一个字节都没变。**

---

## 2. 那层 scope 的形状（保存/清/种/还原 逐句 · 为什么必须套在广播外面）

```csharp
struct DrawReferentScope : System.IDisposable
{
    readonly BattleContext _ctx;
    readonly List<CardInstance> _saved;

    DrawReferentScope(BattleContext ctx, CardInstance inst)
    {
        _ctx = ctx;
        _saved = new List<CardInstance>(ctx.DrawnThisResolve);   // ← ① 保存（拷一份，不持引用）
        ctx.DrawnThisResolve.Clear();                            // ← ② 清
        if (inst != null) ctx.DrawnThisResolve.Add(inst);        // ← ③ 种
    }

    public static DrawReferentScope Seed(BattleContext ctx, CardInstance inst)
    { return new DrawReferentScope(ctx, inst); }

    public void Dispose()                                        // ← ④ 还原（using 退出时跑，**异常也会跑**）
    {
        _ctx.DrawnThisResolve.Clear();
        _ctx.DrawnThisResolve.AddRange(_saved);
    }
}
```
调用点（`Core/RuleCore.cs:1153`）：
```csharp
using (DrawReferentScope.Seed(ctx, inst))
    BroadcastWhen(ctx, WhenEventKind.Draw, p, card, null);
```

**四条理由（逐条都有离线变异读数撑，见 §3）**：

1. 🔴 **必须套在广播【外面】** —— 监听器的正文**在广播进行中**就要读这个槽
   （`Company Master` 的 `DoLowerCost` 就在监听器那一跳里跑）。种在广播之后 = 白种；
   在广播**之前**种、广播**之后**还原，才是「窗口」。
2. 🔴 **还原的是「进入前的值」，不是 `Clear()`** —— 与 `EventSlotsScope` 同一条纪律。
   本题里它是**硬需求**：`DoDraw`（`EffectResolver.cs:1653`）在**广播之外**把
   「本次结算抽到的全部」累加进这个槽（`For each troop drawn …` 数它）。
   只清不还 ⇒ 那一批被吃掉、而且会**重复累加**（实测：掉血 2 → **3**）。
3. ⚠️ **用 `using`（不是手写 save/restore 三句）** —— 监听器正文里再广播事件是常事
   （嵌套），异常路径也要还原。`EventSlotsScope` 头注释里那句
   「**别再往这三跳里各写一份 save/restore** —— 三份迟早不一致」是同一个理由；
   本例只有**一个**调用点，但形状照抄它，将来第二个调用点直接 `Seed` 即可。
4. ⚠️ **只动 `DrawnThisResolve` 一个槽** —— `LastCreated` / `LastHandTarget(s)` / `EventTarget` /
   `EventCard` **一律不碰**。所以它与 `BroadcastWhen` **内部自己会开的那层 `EventSlotsScope`
   不打架**（两者管的槽不相交），嵌套顺序也无所谓。

---

## 3. 加了哪些断言（逐条：钉什么 · 判别式 · 变体实验读数）

新函数 `TestS10CompanyMasterLowerCost`（`Editor/RuleEngineTest.cs:19780-19912`）。
夹具一律走**真 `NewBattle` + 真卡池**（`cardPool: CardDatabase.Load()`），
靶子 `Company Master` 与对照 `BL15` 都是**真卡**。

### 3·1 断言 ① —— 钉「**种**」那一半（`CostOf` 5 → 4）

- **夹具**：`Company Master`（`DA31`，真卡）用 `Place(ctx,0,3,cm)` 摆上场；
  牌库次序按 `pop_back` 排成「**第 5 抽才是那张 5 费靶子 `S10Big`**」
  （起手 3 = `S10Ctrl`(5 费) · `tB` · `tC`；回合开始那一抽 = `tA`；第 5 抽 = `S10Big`）。
  然后 `RuleCore.Draw(ctx, 0)`。
- **观测量**：`RuleCore.CostOf(ctx, 0, 抽上来那一份)`。
- **三条前提断言**（缺一条这一格会变假绿 —— 全写进去了）：
  · 抽上来的**确实是 `S10Big`** 且它的印价 = 5；
  · 对照那张**同为 5 费**的 `S10Ctrl` **真在手里**、且是**另一份实例**；
  · 它**现在按牌面价 5**（降费是这次抽牌的结果，不是本来就便宜）。
- 🧨 **判别式**：`Core/RuleCore.cs:1153` 那个 `using (DrawReferentScope.Seed(ctx, inst))`
  **整段删掉**（退回裸 `BroadcastWhen`），**或**只把 `EffectResolver.cs:4728` 那行
  `ctx.DrawnThisResolve.Add(inst);` 删掉 ⇒ **实得 5 ⇒ 红**。
- **配套的两条对照**（防「全场降价」冒充）：
  · 手里另一张 5 费牌 `S10Ctrl` 仍是 **5**（降费是**按份钉**的）；
  · `Company Master` **自己**仍是 **6**（`it` 指抽出来那张、不是它自己）。
- **结构性的一条**：广播跑完之后 `ctx.DrawnThisResolve.Count == 0`（这次 `Draw` 不在任何
  `ResolveOps` 里 ⇒ 还原成「进入前的值 = 空」）。⚠️ 文案里**如实标了**：它读的是引擎自己的
  计数账（`CountRef("draw")` 读的就是它），**行为侧的那一半是 ②**。

### 3·2 断言 ② —— 钉「**还原**」那一半（`For each troop drawn` 仍然只算 2 遍）

- **夹具**：战术 `S10Count` = `Draw 2 cards. For each troop drawn, deal 1 damage to an enemy troop`
  （真解析：`draw amount=2` + `deal amount=1 countRef=[any] countScope=[draw]`，离线 `s10seg` 量过）；
  敌方槽 3 摆一个 30 血靶子；`PlayTactic(ctx, 0, ti, 3)`。
- **观测量**：靶子掉了**几点血**（= 那条 `for each` 结算了几遍）。
- ⚠️ **为什么这一条不能省**：`DoDraw` 是在**广播之外**累加的那一批。
  种子若**不还原**，① 的 `CostOf` **照样是 4**（离线实测 —— ① 分不出还原）
  ⇒ 只写 ① 就是**半截判别式**。两条合起来才是完整的「保存/清/种/还原」。
- 🧨 **判别式**：把 `DrawReferentScope.Dispose` 改成空过（只种不还）
  ⇒ 种子留在槽里、`DoDraw` 再把同一份累加一遍 ⇒ **实得 3 遍 / 3 血 ⇒ 红**。
- 附一条同源的账侧读数：结算完 `ctx.DrawnThisResolve.Count == 2`。

### 3·3 变体实验读数（离线探针 `D:/tmp/wf_s10_probe`，**不占 Unity 实例**）

`PYTHONIOENCODING=utf-8 python mut.py`（每轮**从仓库重取源码**再套变异，不叠加上一轮）：

| 变体 | ① `CostOf`（期望 4） | ① 对照 `S10Ctrl`（期望 5） | ① `CM` 自己（期望 6） | ① 广播后槽（期望 0） | ② 掉血（期望 2） | ② 槽（期望 2） |
|---|---|---|---|---|---|---|
| **V0 真代码（scope 在位）** | **4** ✅ | 5 ✅ | 6 ✅ | **0** ✅ | **2** ✅ | 2 ✅ |
| **V1 整个 scope 删掉** | **5** ❌ | 5 | 6 | 0 | 2 | 2 |
| **V2 保留 save/清/还原、只删 `Add(inst)` 那一行** | **5** ❌ | 5 | 6 | 0 | 2 | 2 |
| **V3 保留种子、只删还原**（`Dispose` 空过） | **4**（==> ① 分不出！） | 5 | 6 | **1** ❌ | **3** ❌ | **3** ❌ |

⇒ **三行**（`using` 那两行 + `Add(inst)` 那一行）**每一处删掉都有断言变红**；
还原那一处由 ① 的槽读数 + ② 的**行为**双双咬住。

### 3·4 回归探针（同一次离线跑，**两侧都实测过**：改后 = 真代码 · 改前 = V1 变异）

| 探针 | 量什么 | 改前 → 改后 |
|---|---|---|
| `s10slot`（`A888` ③-d 的离线翻版） | `Draw a card. Draw a card. Give them Flank` ⇒ `LastHandTargets.Count=1` · 挂上效果的份数 = 1 · 槽 = 2 份 | 1 / 1 / 2 → **1 / 1 / 2** ✅（两侧都实测） |
| `s10rubric` | 另一张 `draw` 监听器 `BL15 Rubric Marine` ⇒ Dark Pact 0 → 1 | 0→1 → **0→1** ✅（两侧都实测） |
| `s10restore` | `Draw 2 cards. For each troop drawn, …` ⇒ 掉血 / 槽 | 2 / 2 → **2 / 2** ✅（两侧都实测） |
| `s10cm` | 主靶（同 §3·3 的 V0/V1 两行） | 5 → **4** ✅（两侧都实测） |
| `emergency` | `Emergency Dispensation`（`DoDrawRef` 写点 + `(指代上一张)` 读点） | `Desolation Marine cost=3（印 6）` → **逐字相同** ✅（两侧都实测；这条链不经过 `RuleCore.Draw`） |
| `jackal` / `suppressor` / `played` | 另外三条老探针（加费 / 候选域 / 「本局打出过的牌」） | **改后与改前日志逐字相同** ✅（⚠️ 只有改后是本次新跑的，改前那次是 S7 那份探针同一版本 —— **旁证，未两侧对跑**） |

---

## 4. grep 现扫：谁读 `DrawnThisResolve` / `CountRef("draw")` / `HandReferents`

`grep -rn "DrawnThisResolve" --include=*.cs Assets/` 全库只有 3 个文件（`BattleContext.cs` / `EffectResolver.cs` /
`RuleEngineTest.cs`）—— **`CardPresentation/**` 零命中**。逐处判：

| # | 处（现行号） | 角色 | 窗口变窄/变宽有没有影响 |
|---|---|---|---|
| 1 | `BattleContext.cs:545` | 定义 | —— |
| 2 | `EffectResolver.cs:54`（`ResolveOps` 入口） | **清** | 🔴 **不受影响**：它在广播**之外**跑；我的 scope 还原的是「进入前的值」，两者不会互相覆盖（scope 只在 `RuleCore.Draw` 之内存在） |
| 3 | `EffectResolver.cs:466`（`DoDrawType` 写点） | 写 | 不受影响（广播之外；`drawtype` 走的不是 `RuleCore.Draw`） |
| 4 | `EffectResolver.cs:1653`（`DoDraw` 写点） | 写 | 🔴 **这是最需要盯的一处**：它在**广播之外**累加「本次结算抽到的全部」。**还原保证了它不被吃掉** —— ② 的断言钉的就是这件事（不还原 ⇒ 打 2 张算 3 遍） |
| 5 | `EffectResolver.cs:2393`（`DoChooseCard` 写点） | 写 | 不受影响（选牌路，不调 `RuleCore.Draw`） |
| 6 | `EffectResolver.cs:3453`（**`HandReferents` 兜底**） | **读** | ⚠️ **窗口内读数变宽了**（从「空/陈旧」变成「1 份」）。**今日零命中**：`HandReferents` 的两个调用点是 `EffectResolver.cs:5601`（`DoGive` 的 `prev` 手牌支）与 `:6488`（`ConditionHolds` 的 `targethaskw` 手牌支），而**卡池里只有 2 张监听 `draw`**（见 §5），两张的效果**都不走这两个调用点**。**广播之外读数不变** |
| 7 | `EffectResolver.cs:3733`（`DoDrawRef` 写点） | 写 | 不受影响（`Draw it` 走的是它自己那条路，不调 `RuleCore.Draw`；离线 `emergency` 探针照旧 ✅） |
| 8 | `EffectResolver.cs:4726-4741` | **就是本次新增的 scope** | —— |
| 9 | `EffectResolver.cs:5509`（**`CountRef("draw")`**，`CountScope == "draw"`） | **读** | ⚠️ 同上：**窗口内**从「空/陈旧」变成「1 份」。**今日零命中**（2 张 `draw` 监听器都不带 `for each … drawn`）。**广播之外读数完全不变**（还原）—— ② 的断言就是它的回归钉 |
| 10 | `EffectResolver.cs:7001`（`DoLowerCost` 的 `(指代上一张)` 支） | **读** | ✅ **这就是本次要修的那一处**（`LastCreated` 空时退到它） |

**结论**：窗口**只在 `BroadcastWhen(Draw)` 之内**变宽（空/陈旧 → 恰好 1 份），
**窗口外一个读数都不变**；今日卡池里**没有任何一张卡**能踩到 ⑥ / ⑨ 的窗口内变化
（判据见 §5 的逐张判）。**将来新增 `draw` 监听器时要重新判这两处**（已写进
`DrawReferentScope` 的头注释，标成「如实标着」）。

---

## 5. 影响面

### 5·1 该跑哪些自检（按铁律 12 的三条判据推的，⚠️ **我没跑 Unity**，只有调度台能跑）

| 覆盖面 | 自检 | 为什么 |
|---|---|---|
| **必跑** | **`RuleEngineTest.Run`** | 引擎 `Core/` 改了（`Draw` + 新增 scope），且**新断言就住在它里面** |
| **强烈建议同批** | `BattleScene.Run` | `RuleCore.Draw` 是**对战主链**：回合开始那一抽（`RuleCore.cs:837`）、`EndTurn`/`BeginTurn`、疲劳（`MoveDeckTopToHand`）、教程那一抽（`TutorialScript.cs:859/1339`）都走它 |
| **建议** | `NetBattleTest.Run` | 它是「两个裸 `BattleContext` 真打一局」⇒ **也会抽牌**、也走这条改动（它住 `CardPresentation/Editor/`，但**吃的是引擎**） |
| 不必 | `DeckScene` / 菜单外壳那一族（`ShellScene` / `MainMenuScene` / `RewardsScene` / `ShopScene` / `CollectionScene` / `SettingsScene`） | 没碰 `CardPresentation/**`、没碰共用件；这几个宿主不跑抽牌链 |
| 不必 | `CardBaseDemo` / `NetSelfTest` | 前者是卡面渲染；后者只测「传输/握手/心跳/掉线重连」，不打对局 |

（⚠️ 一句话概括：**动了引擎的抽牌主链 ⇒ 批次至少 `RuleEngineTest.Run` + `BattleScene.Run`**。）

### 5·2 别的卡会不会受影响 —— 逐张判

**① 哪些卡既监听 `draw`、又用「指代上一张」**（会真的吃到这次改动）：

全卡池 1126 张，`desc` 里带 `When you draw a card` 的**只有 2 张**（正则实测）：

| 卡 | 卡面 | 效果怎么走 | 受影响吗 |
|---|---|---|---|
| **`DA31 Company Master`** | `When you draw a card, it costs 1 less this turn` | `lowercost payload=(指代上一张)` ⇒ **走 `DrawnThisResolve`** | ✅ **就是本次修的**：5 → 4 |
| **`BL15 Rubric Marine`** | `When you draw a card, gain a Dark Pact of Fate.` | `gain …` 落在**自己**身上，**不读任何指代槽** | ❌ 不受影响（离线 `s10rubric` 实测：Dark Pact 0→1 照旧） |

**② 哪些卡用「`for each … drawn`」**（读 `CountRef("draw")`，共 **8 张**）：
`BL57 Hosts of Chaos` · `DA54 Hunt the Fallen` · `EC51 Mechanised Murder` · `SAU76 Awakened Dynasty` ·
`SOR62 Shrineworld` · `SW57 Hunter's Guile` · `TL56 Leviathans Tendrils` · `UM_Gather_the_Company Gather the Company`
—— **全部是「先 `Draw N cards`、后按抽到的张数结算」的自施法战术卡**（`DoDraw` 写、同一个 resolve 里读，
**都在广播之外**）⇒ **还原保证了读数不变**（② 的断言就是这个形状的回归钉）。

**③ 哪些卡用「手牌代词兜底」**（读 `HandReferents` 的 `DrawnThisResolve` 兜底）：
`GOF50 Tide of Muscle` · `TAU54 Dynamic Offensive` · `GOF_Da_Red_Waaagh` · `UM23 Spear of Macragge`
（口径出处：`EffectResolver.cs:3420-3431` 的实测清单）—— 四张**全是 `drawtype` 或 `Draw a …` 的战术卡**，
**没有一个在 `draw` 广播期间读它** ⇒ 不受影响（③-d 的离线翻版 `s10slot` 实测照旧）。

**④ 反过来问**：有什么东西在**广播期间**读这个槽、因而被「变宽」影响？—— **今日没有**（见 §4 的 ⑥/⑨）。

---

## 6. 类型检查读数 + 行尾核对（原样贴）

```
cd d:/4 && TMPDIR=/tmp/wf_s10 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（共跑了 **4 次**：改 `EffectResolver.cs` 后 · 改 `RuleCore.cs` 后 · 改完断言后 · 补完那条弱断言后
—— 每次都是 **0/0**。
✅ 并**验证过 `RuleEngineTest.cs` 确实进了编辑器程序集的源清单**
（`grep -c RuleEngineTest.cs /tmp/wf_s10/wf_csc_editor.rsp` = **1**）——
⛔ 不是「没编到它所以 0/0」。本轮**没有**撞上别的写手的半成品报错。）

行尾（**二进制读**，`b.count(b'\r\n')` vs `b.count(b'\n')`）：

| 文件 | CRLF | LF | 判 | 原样 |
|---|---|---|---|---|
| `Core/RuleCore.cs` | 0 | 4935 | 纯 LF | ✅ 没被翻 |
| `Core/EffectResolver.cs` | 7401 | 7401 | 纯 CRLF | ✅ 没被翻 |
| `Editor/RuleEngineTest.cs` | 20252 | 20252 | 纯 CRLF | ✅ 没被翻 |

`git diff --numstat`（只列我动过的三个）：
```
85	4	Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs
57	6	Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs
443	9	Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs
```
⚠️ **这三个数里混着别的写手未提交的增量**（不是我被翻行尾）：
· `EffectResolver.cs` 的 `4` 个删除 + `RuleCore.cs` 的 `6` 个删除**全是我改之前就在的**
  （S7 那一批：`22/4` 与 `46/6`）；我的增量分别 ≈ **+63 / 0** 与 **+11 / 0**。
· `RuleEngineTest.cs` 的 `9` 个删除也是**改之前就在的**（S7 那批 `309/9`；更早还有 `WE` 的夹具）；
  我的增量 ≈ **+134 / 0**。
· 🔴 判据：**「数字接近文件行数 = 行尾被翻」→ 这里最大的 443 ≪ 20253** ⇒ 不是翻行尾。

---

## 7. 顺手发现（都没改）

### 7·1 🔴 **`LastCreated` 不在结算入口清 ⇒ 「先 create、后 draw」时它会【抢班】**
`DoLowerCost` 的 `(指代上一张)` 支是 **`LastCreated` 优先、空了才退到 `DrawnThisResolve`**
（`EffectResolver.cs:7001` 那句三元）。而 `LastCreated` **不在 `ResolveOps` 入口清**
（`grep` 到的 `LastCreated.Clear()` 只有 `:1825 DoCreate` / `:2301 DoChooseCard` / `:3739 DoDrawRef` /
`:3825 DoReturn` / `:3906` 五处，**没有一处是 `ResolveOps` 入口**）——
⇒ 上一张卡留下的指代会一直挂到下一张卡里。

**离线实测（`s10stale`，真卡真卡池，`D:/tmp/wf_s10_probe`）**：
```
场上：Company Master（DA31）
先打出 `Create a Reanimation Protocol in your hand`  ⇒ LastCreated = [Reanimation Protocol（印 1）]
再 RuleCore.Draw 抽一张 5 费牌
★ 结果：抽到 = S10Big（印 5）· CostOf = 5      ← **该降的那张没降**
        对照·LastCreated 里那一份（印 1）· CostOf = 0   ← **被降的是它**
```
卡面写的是 `When you draw a card, **it** costs 1 less`（用户 2026-10-18 澄清过「`it` = 抽出来的那张牌」）
⇒ 这是**真差异**，而且 §1 那个修法**只覆盖 `LastCreated` 为空的那一半**（普通抽牌、回合开始那一抽）。
⛔ **我没改**：① 超出本简报授权（CLAUDE.md 13·4 ⑧：顺手发现只许报）；② 清 `LastCreated` 的窗口口径
牵动别处（`create` → `lowercost` 同卡跨分句、`DoReturn` → `It costs 4 less` 那条一号验收靶），
**由调度台定**。判据建议：`FreezeLastCreated(ctx)` 与 `DrawnThisResolve` **同一个窗口**
（= `ResolveOps` 入口清），但**要先把「同一条 resolve 内跨 op 的指代」全数盘一遍**再动。
**卡池里踩得到的形状**：`SAU76 Awakened Dynasty`（`Draw 2 cards. For each troop drawn, create … in your hand`）
之前若打过任何 `create`/`return` 类卡，DA31 的 `it` 就会指错。

### 7·2 ⚠️ `BattleContext.cs:848-854` 那段注释与事实**不完全一致**（**没改**：不在我的白名单里）
它写「**一层卡一个窗口清（`DrawnThisResolve` 在 `ResolveOps` 入口清、`LastCreated` 在 `DoCreate` 入口清）**」
—— 后半句**不成立**：`DoCreate` 入口清 ≠ 「一条卡一个窗口」，`LastCreated` 其实**跨卡存活**
（就是 7·1 量到的那个现象）。前半句是真的。

### 7·3 ✅ 一条**核过、成立**的（不改）
`RuleCore.cs` 的 `DealOpeningHand` 头注释说「`DrawnThisTurn` 那一半今天观测不到差异」——
本轮的 `draw` 窗口改动**不影响**它（起手发牌走 `MoveDeckTopToHand`、**根本不进 `Draw`**
⇒ 也不进那个 scope）。

---

## 8. 没查清的部分

1. 🔴 **`DrawReferentScope` 的「一次抽多张」语义我只有间接论据**：
   种法是 **`Clear()` + `Add(inst)`**（**恰好那一份**），不是「调用方原有的 + 这一份」。
   理由是卡面 `it` 是**单数**（用户 2026-10-18 澄清「`it` = 抽出来的那张牌」）——
   但**没有**在反编译里找到「原版用哪个容器装它、一次抽多张时是几份」的直接对应
   （与 S7 §9·5 同一条**没查清**）。若将来判出「应当是累加」，改法是把构造函数里那两句
   改成「先拷 `_saved`、再 `Add(inst)`」——**两处断言都要跟着重挑期望值**。
2. ⚠️ **`BL15 Rubric Marine` 只做了「监听器照样被叫醒」的离线旁证**（Dark Pact 0→1），
   **没有**核过它「一条 Dark Pact of Fate」的完整结算链。
3. ⚠️ **§5·1 的自检清单是按覆盖面【推的】，不是实测** —— 我跑不了 Unity（铁律：只有调度台能跑）。
   真机上若有既有断言翻红，第一嫌疑是 §4 的 ⑥/⑨（窗口内变宽的两个读点），
   排查入口 = `grep -n "CountScope == \"draw\"\|HandReferents(" Core/EffectResolver.cs`。
4. ⚠️ **`RuleEngineTest.cs` 的 numstat 增量（≈ +133）是估算**：那个文件里混着 `S7` 与更早
   `WE` 的未提交增量，我只能确认**我的锚点没和它们的 hunk 相交**（改前 `git diff -U0` 列过 hunk 范围）。
5. ⚠️ **`s10stale`（7·1）只量了 DA31 这一张的读数**，**没有**盘「全卡池里还有多少张
   `(指代上一张)` 会被陈旧的 `LastCreated` 顶掉」—— 那需要另开一遍普查。

---

## 附：未落盘的东西（交接用）
- 离线探针 = `D:/tmp/wf_s10_probe/`（**不在仓库里**，临时目录迟早被清）：
  `S10Probe.cs`（`CompanyMaster` / `Restore`）· 同文件里的 `S10Regress`（`Slot` / `Rubric`）·
  `S10Stale`（7·1 的复现）· `mut.py`（四档变异，每轮从仓库重取源码）。
  跑法：`cd /d/tmp/wf_s10_probe && dotnet build -v q && ./bin/Debug/net8.0/wfprobe.exe s10cm`。
- 📌 收口判据还要求「在 `资料/真Play待验清单.md` 补一条实局观测」——
  **那份文件不在我的白名单里，我没写**，请调度台补。
