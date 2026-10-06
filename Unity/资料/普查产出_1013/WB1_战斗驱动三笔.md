# WB1 · 战斗驱动三笔（A410 日志 + A513 ChatButton + A515 静态钩子）

> 执行写手 WB1 · 2026-10-13 · 「清空 A 表」第四轮
> 白名单内只动了 **2 个文件**：`Battle/BattleDriver.cs`（+109/−23 行）· `Editor/BattleScene.cs`（+85/−1 行）。
> ⛔ 没跑 Unity（没跑任何 `-executeMethod`）· ⛔ 没动 git · ⛔ 没改正本 · 只跑了**秒级类型检查**。
> 行号一律是**改完之后的现读值**。

---

## 一、结论

三条都改到了，而且三条**各自的断言都能把「改回错的」判红**（断言细节见 §四）。

| 账 | 结果 | 一句话 |
|---|---|---|
| **A410** | ✅ 改完 | `BeginFromPendingCore` 那句日志不再写死「联机开局」，按**这一趟是哪一档**分三档说（`attachNet` + `_net`）；回放那一档现在是 `[Replay] 回放开局：…` |
| **A513** | ✅ 改完 + **归属订正** | `ChatButton` 那四个 px 从 0.1 取整值改成**原版未取整值**（父链走完的精确矩形）；断言加了一条 **1e-4 世界单位**档的细口径（**改之前那条会红**，实测偏差 2.0e-4 / 4.4e-4 世界单位，两个轴都超阈） |
| **A515** | ✅ 改完 | 两个补间解析口（`UnitTweenRuntime.HeroBySeat` / `SeatOf`）从 `BuildHud()`（被 `_hudBuilt` 闩住）**挪进 `Begin()`** 的 `HookUnitTweenResolvers()` ⇒ 二次 `Begin()` 也把钩子接全 |

**结论一句话**：三处都是「一处小改 + 一条会红的断言」，没有扩大改动面；A513 那条断言如果**改之前**跑，是**红的**（A 表现核 判得对：不是「可能有问题」，是实测已偏）。

---

## 二、逐条改动清单

### A410 —— 回放局的日志不再自称「联机开局」

| | |
|---|---|
| 文件 | `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` |
| 落点 | `:1786`（`BeginFromPendingCore`，方法在 `:1774`）+ `:1747` 的方法 `<summary>` |

**改前**（`:1777-1778` 原文）：
```csharp
Debug.Log($"[Net] 联机开局：种子 {pb.Seed} · 模式 {pb.ModeStr} · **本机座位 {pb.MySeat}** · "
        + $"先手座位 {pb.FirstSeat}（{(pb.FirstSeat == pb.MySeat ? "我" : "对面")}）· 战场 {pb.Arena}");
```

**改后**（`:1786-1791`）：
```csharp
string kindNet = attachNet ? "[Net] 联机开局"
               : _net != null ? "[Net] 联机重建（重连）"
               : "[Replay] 回放开局";
Debug.Log($"{kindNet}：种子 {pb.Seed} · 模式 {pb.ModeStr} · **本机座位 {pb.MySeat}** · "
        + $"先手座位 {pb.FirstSeat}（{(pb.FirstSeat == pb.MySeat ? "我" : "对面")}）· 战场 {pb.Arena}");
```

**为什么这么改**（判据 + 为什么不是「只看 `attachNet`」）：

- 简报说「按 `attachNet`（**或等价判据**）分流」。**只按 `attachNet` 会引入一条新的假话**：三个入口里
  `NetReplay()`（重连重建）走的也是 `attachNet: false`（`:582` 那一处，源码注自写「重建（`Net` 保持挂着）」）
  ⇒ 二值分流会把**重连**那一档说成「回放」。所以判据取 **`attachNet` + `_net`** 两格：
  | 入口 | `attachNet` | `_net` | 现在的措辞 |
  |---|---|---|---|
  | `BeginFromDeckLibrary()` 拿着 `NetPendingBattle.Take()` 那一支（`:1893`） | `true` | — | `[Net] 联机开局` |
  | `NetReplay()` 重连重建（`:582`） | `false` | **非 null** | `[Net] 联机重建（重连）` |
  | `PlayReplay()` 放录像（`:496`） | `false` | **null** | `[Replay] 回放开局` |
- `_net` 只在 `AttachNet()`（`:184`）里被赋值，**没有任何地方把它置回 null** ⇒ 重连那一档读到非 null 是稳的；
  而放录像的生产路径是 `Start()` → `ReplayStore.TakePending()` → `PlayReplay()`（`:1229-1230`，新场景新 driver）
  ⇒ `_net` 恒 null。
- **顺带把 `[Net]` 前缀也分了**：放录像那一档现在打 `[Replay]`（同文件 `:474` 那条 `[Replay] 开播：…` 就是
  这个前缀）+ **回放不是联机局**（判据 = 原版 `MatchType.Replay = 160` 是独立的一档，与 A387 同一条）。
- ⛔ **没碰模式通道**：`pb.ModeStr`（字符串）那一句 `GameplayVariables.For(...)` 原样不动 —— 那是 A383 的账。

### A513 —— `ChatButton` 的四个 px 改成原版精确值

| | |
|---|---|
| 文件 | `Battle/BattleDriver.cs`（`:8054`，在 `BuildHudExtras` 里）|
| 还有 | `:4090` 加了一个自检口 `HudExtraWorldPos(string)` |

**改前**（`:7967` 原文）：
```csharp
_chatBtn = HudAbs(root, "40k_UI_bt_voicelines", 50.9f, 880.2f, 64.44f, 61.85f, "ChatButton");
```
**改后**（`:8054`）：
```csharp
_chatBtn = HudAbs(root, "40k_UI_bt_voicelines", 50.9201953125f, 880.15399932861328f, 64.443f, 61.846f, "ChatButton");
```

**为什么**：期望值来自原版资产直读（**不是**我们自己的常量）——
`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2984.json`
（`ChatButton`，挂 GO 85）：`anchoredPosition (19.219999313354492, 110.0)` · `sizeDelta (64.44300079345703, 61.84600067138672)`
· `anchorMin=anchorMax=(0,0)` · `pivot (0,0)`；父链走完
`2984 → 3319(PlayerInfo (31.7001953125,28.0) 260×75) → 2849/3498/2684/2759（全 stretch、零偏移）→ 1395（裸 `Transform`，localPosition 0）`
⇒ 绝对 `x[50.9201953125, 115.36319610595703] y[880.15399932861328, 942.0]`，
**中心 = (83.14169570922852, 911.0769996643066)**。

**float32 逐值核过**（用 numpy 按 C# 的 float 语义算了一遍，见 §四「怎么算的」）：
- `64.443f`/`61.846f` 转成 float32 之后**与那份 JSON 的 double 逐位相等**（`64.44300079345703` / `61.84600067138672`）✓
- 新实现的中心 = **(83.14170, 911.07697)**（px），与断言的期望路径**逐位相同**（x 差 0，y 差 3.0e-7 世界单位）
- 旧实现的中心 = **(83.12001, 911.12499)**（px）—— 与 A 表现核 记的 (83.12, 911.125) 对得上 ✓

⚠️ 这条**与 A423 那颗 `CenterCameraButton` 不是同一串数**：那颗是 `64.4429931640625 / 61.84600830078125`
（与 `64.443f / 61.846f` **不是同一个 float32**），所以是**两份常量各钉各的**，没并成一个（源码注释里写明了）。

### A515 —— 二次 `Begin()` 要把 19 条静态钩子全接回来

| | |
|---|---|
| 文件 | `Battle/BattleDriver.cs` |
| 落点 1 | `:1716` **新增** `void HookUnitTweenResolvers()`（两个解析口的赋值从 `BuildHud` 搬过来） |
| 落点 2 | `:2053` `Begin()` 里新增一行 `HookUnitTweenResolvers();`（紧挨 `HookAnimFxShake(); HookAnimFxCards();`） |
| 落点 3 | `:7549` `BuildHud()` 里那两段赋值**删掉**，只留 `UnitTweenRuntime.Install();` |
| 落点 4 | `:1881` `ForEachStaticHook` 的 ③ 段注释改成「现在挂在 `Begin()`」 |
| 落点 5 | `:1930` 新增 `public static int StaticHookSlots`（清单**槽数**，当分母/参照用） |

**改前**（`BuildHud` 里，`_hudBuilt` 闩之后）：
```csharp
UnitTweenRuntime.HeroBySeat = seat => { … ViewAt(seat, BoardSpec.WarlordSlot) … };
UnitTweenRuntime.SeatOf     = tr   => { … 扫一遍视图表 … };
UnitTweenRuntime.Install();
```
**改后**：那两段原样搬进新方法 `HookUnitTweenResolvers()`，由 `Begin()` 每次调用；`Install()` **留在原地**。

**为什么**：
- `BuildHud()` 被 `_hudBuilt` 闩住（`:7442-7443`，HUD 结构只能建一次）⇒ 挂在里面 = **只对第一次 `Begin()` 生效**；
  而 `DetachStaticHooks()`（`OnDestroy` 第一句）会把这两格置 null（它们在 19 槽清单里）⇒
  **二次 `Begin()` 只接回 17/19**，静默少两条（下游 `UnitTweenRuntime.ResolveHero` 在 `HeroBySeat == null` 时
  直接 `return null` ⇒ 表现为「补间定位不到督军」，不报错）。
- 挪到 `HookAnimFx*` 那一族是**同一条纪律**：钩子绑的是**这个 driver 实例**（lambda 捕 `this` / 方法组绑 `this`）
  ⇒ **每次 `Begin` 都要重挂**（赋值本身幂等，直接覆盖静态字段）。
- ⚠️ **`Install()` 刻意没搬**：它装的两条（`WFModuleTween.OnInvoke` / `UnitTweenTable.HeroOf`）绑的是
  `UnitTweenRuntime` 的**静态方法**、不指着 driver，`Installed` 那个闩**幂等且不该动**（`:1831-1833` 原注的话）。
  顺序无所谓 —— `ResolveHero` 是**调用时**才读那两个解析口。
- **全仓只有这一处赋值**（grep `HeroBySeat|SeatOf` 全命中：定义 1 处 + 新赋值 1 处 + 清单里的置 null 1 处 + 消费 3 处）。

---

## 三、A513 的归属订正说明（A 表说反了什么）

**A 表原文**（`项目任务.md:436`）：

> 🟡 **2026-10-12（H47 顺手查出，要做 / 同族口径）** —— `ChatButton` 也是**同一批取整字面量**
> （中心 **83.12** vs 粗口径 83.15；今天容差 1.5px 不红，**细口径判据没查**）⇒ 查细口径 + 按需订正

**它说反/说漏的三件事**（以现读与实算为据）：

| 数 | 真实身份 | 出处 |
|---|---|---|
| **83.12** | 🔴 **是我们的实现**（`50.9 / 64.44` 算出来的中心 = **83.12000513076782**） | `Battle/BattleDriver.cs` 改前那行 `HudAbs(…50.9f…, 64.44f…)` |
| **83.15** | 🔴 **是断言里的粗值**（容差 1.5 px 的那条） | `Editor/BattleScene.cs` 的 `At("ChatButton", 83.15f, 911.1f);` |
| **83.14169570922852** | ✅ **原版精确值**（A 表整行**一次都没提**它） | `RectTransform_2984.json` + 父链（见 §二） |

A 表把「**我们的实现**」和「**断言里的粗值**」并排写成「中心 83.12 vs 粗口径 83.15」，
读起来像「83.12 是原版/目标值、83.15 是我们抄粗了」—— **正好反了**；而真值 **83.1417** 两个都不是。
A 表现核 §A513 已经先一步订正过这一点（它写「归属反了」），这里照它的口径**落盘**并补上实算：

- 我们（改前）中心 = **(83.12000513076782, 911.1249876022339)**
- 原版中心 = **(83.14169570922852, 911.0769996643066)**
- 偏差 = **0.0217 px / 0.0480 px = 2.0e-4 / 4.4e-4 世界单位**（1 px = 1/108 世界单位）
- A423 那一档阈值 = **1e-4 世界单位（≈0.011 px）** ⇒ **两个轴都超** ⇒ **不是「可能有问题」，是实测已偏**。

ℹ️ 另一件顺带核实的：A 表说「今天容差 1.5px 不红」—— 属实，那条粗口径**现在也不红**
（新中心与 `83.15/911.1` 差 0.0083 / 0.023 px）。**两档并存是对的**（A423 已经立了这个先例）：
粗口径挡「大错」，细口径挡「取整没消」。

---

## 四、断言清单（断什么 · 期望值来源 · 改坏法 · 落点）

三条账**一共 5 条断言**，全落 `Editor/BattleScene.cs`：

### A410（`Editor/BattleScene.cs:9545-9570`，在回放那一段里）
**怎么抓**：`Application.logMessageReceived` 圈住 `driver.PlayReplay(playRec)` 那一趟
（本仓现成的抓法，同 `Editor/SettingsScene.cs` 的 `CaptureErrors`；`try/finally` 摘钩子）。
⚠️ 为了让抓取圈住那一趟，`bool same = driver.PlayReplay(playRec);` 拆成了 `bool same; try { same = … } finally { … }`
（**只包这一趟**，不包别的）。

| # | 断什么 | 期望值来源 | 改坏法（必须红） |
|---|---|---|---|
| ① | 抓到的日志里**没有「联机开局」** | 判据 = 原版 `MatchType.Replay = 160` 是独立的一档（同 A387）⇒ 回放不是联机局 | 把 `BeginFromPendingCore` 那句日志改回写死的「联机开局」 ⇒ **红** |
| ② | 抓到的日志里**有「回放开局」** | 红线「⛔ 不许静默失败」⇒ 它得说清自己是哪一档 | 把那句日志整句删掉（或改成只报状态） ⇒ **红** |

② 是**防「把话说没了也算过关」**的那一半：只断 ①，删掉日志就「绿」了。

### A513（`Editor/BattleScene.cs:8546-8568`，紧跟在粗口径 `At(…)` 那一组之后）
| # | 断什么 | 期望值来源 | 改坏法（必须红） |
|---|---|---|---|
| ③ | `ChatButton` 世界中心偏 **< 1e-4 世界单位**（≈0.011 px） | **原版精确值**：`RectTransform_2984.json` 的 `sizeDelta`/`anchoredPosition` + 父链走完 ⇒ 中心 `(83.14169570922852, 911.0769996643066)`（px，y 从上）⇒ `LayoutSpace.ToWorld(83.14169570922852f/1920f, 168.9230003356934f/1080f)` | 把 `BuildHudExtras` 那四个 px 写回取整值（`50.9/880.2/64.44/61.85`）⇒ 偏 2.0e-4 / 4.4e-4 ⇒ **红**（2026-10-13 之前就是这个状态） |

写法**逐字照 A423 那一档**（`:10240` 那一处）：`hudRoot.TransformPoint(...)`（⛔ 不假设 `hudRoot` 在原点）
+ 只比 x/y + 阈值 `1e-4f`。为此在 `BattleDriver` 加了个自检口 `HudExtraWorldPos(name)`（`:4090`）——
**为什么不能拿现成的 `HudExtraPosPx` 顶替**：它读的是 `transform.localPosition`（**假设 `hudRoot` 在原点**）
且口径是粗的（px），语义对不上 1e-4 档。
**怎么算的**：用 numpy 按 float32 语义把两条路径都算了一遍（`x+w*0.5` 与 `y+h*0.5` 那两条式子都按 C# 的
`float` 运算逐位模拟）⇒ 新实现路径与断言路径 **x 差 0、y 差 3.0e-7 世界单位**（远在阈内）；旧实现路径差
**2.0e-4 / 4.4e-4**（超阈）。**这不是估的，是算出来的。**

### A515（`Editor/BattleScene.cs:10405-10432`，在 A388(b) 那一块里 —— 复用现成的「二次 `Begin`」时刻）
| # | 断什么 | 期望值来源 | 改坏法（必须红） |
|---|---|---|---|
| ④ | 二次 `Begin()` 之后 `StaticHookCount` **== 首次 `Begin()` 后的实测条数**（`hookedBefore388`） | **同一次运行**的实测基线（动态量） | 把那两句挪回 `BuildHud()` ⇒ 二次 `Begin` 只接回 17/19 ⇒ **红** |
| ⑤ | `UnitTweenRuntime.HeroBySeat` / `SeatOf` 在二次 `Begin()` 后**都非 null** | A515 的缺陷本体（这两个口就是差额） | 同上 ⇒ **红**；把 `Begin()` 里那句 `HookUnitTweenResolvers()` 删掉 ⇒ ④⑤ **一起红** |

🔴 **为什么基线用 `hookedBefore388` 而不用清单槽数 19**（这条我改过一版，如实记）：
先写的是 `hookedBeforeB == BattleDriver.StaticHookSlots`（= 19），后来改成同一次运行的基线 ——
因为那 19 槽里 `WFModulePostProcess.OnPostFx` **只在「载进来的战场 prefab 里有 `Volume`」时才挂得上**
（`AttachPostFx` 取不到 Volume 就出声并保持 null）⇒ 换个环境它可能少一条，**写死 19 会把那种环境误报成红**。
同一次运行的基线没有这个问题，而且**同样咬得住**「新加的钩子忘了重挂」。
（为此把 `int hookedBefore388` 从 (a) 那个 `{ }` 里**提到块外**（`:10366`），语义没变；(b) 段现在也读它。
`StaticHookSlots` 还是加上了，但在断言里**只打印出来当参照**，不当期望值。）

**三条断言的前提都核实过**：
- A410 那一趟是**单机放录像** —— 同一文件里 `driver.AttachNet(null)`（联机探针那一段）在它**之前**被调过，
  `_net` 已清 ⇒ 驱动侧落在 `attachNet == false && _net == null` 那一档。⚠️ **联机 / 重连那两档的措辞这里不验**
  （如实标，见 §五）。
- A513 的 `ChatButton` 在那一节**一定存在**：同节上一条 `Check(drv.HudExtraCount == 10, …)` 已把 10 件都数过；
  `HudExtraWorldPos` 找不到会返回 `Vector3.zero` ⇒ 偏得离谱 ⇒ **红**（不会静默通过）。
- A515 的「这是第 N 次 `Begin()`」有实据：同一节上一条断言 `HudExtraCount == 10` ⇒ `_hudBuilt` 已为真。
- ✅ **不会把 A388 那两条弄红**：(a) 是 `hookedBefore388 >= 16`（修完 19，仍 ≥ 16）；
  (b) 用动态量 `hookedBeforeB`（⛔ 没写死 17）—— 与 A 表现核 的预判一致。

**派活必查行逐条自检**（简报要求的那三条）：
1. **断言自证/同义反复** —— ③ 的期望值是**原版资产字面量**（不是从 `BuildHud` 读回来的数），⛔ 没有自证；
   ①② 是「日志里有没有这个词」，判据来自原版 `MatchType.Replay`；④⑤ 的基线是**同一次运行的实测**，
   但「差两条 = 那两个解析口」这个**归因**另有 ⑤ 独立钉一遍。
2. **弱断言分不出两种状态** —— ①② 两条一起才成立（只 ① 的话「删掉日志」也绿）；④ 用基线而**不写死数**，
   在修前（17/19）与修后（19/19）**取值不同** ⇒ 真能分状态（不是恒真）。
3. **「凡 `!RectOfUnion` / 『一个 quad 都没有』式断言」** —— 本件**没有**这类断言（新加的是「位置偏多少」
   与「日志里有没有某句话」，不会因为「新加一层绘制」而变脆）。

---

## 五、没查清 / 没做的

1. **没跑 Unity**（简报红线）⇒ 三条断言**只做了静态核对，没跑过**
   （`BattleScene.Run` 实测要 7m56s，且必须串行 —— 按铁律 12 攒批由调度台在同步点跑）。
   **预期**：③ 在改之前是红的（实算超阈）；修完应当绿；④⑤ 同理（日志实据是修前 17）。
   ⚠️ 这是**预测、不是实测**，如实标。
2. **A410 的联机 / 重连两档措辞没验**：自检里只跑得到「单机放录像」那一档（见 §四）。
   联机档 = `N3` 那一条链，重连档 = `NetBattleTest.Run` 里的重连那一趟 —— 那两条**都不在本件的白名单/宿主里**
   （`Editor/NetBattleTest.cs` 我没有权动）。⇒ 建议调度台把「联机/重连两档的日志措辞」挂进
   `NetBattleTest.Run` 或联机那条线的下一次派活（本条**不是**「不做」，是**本件做不到**）。
3. **A513 只钉了 `ChatButton` 一件**：同族「0.1 px 取整字面量」在这份文件里**还有别的**（`BuildHudExtras`
   里那 10 件里至少 `TitleBackground_*` / `AvatarItemSmall_*` 也是取整值，粗口径 `At(...)` 用 1.5 px 容忍它们）。
   ⛔ **不是「影响小所以不做」** —— 是**本件只认 A513 这一件**（判据只读到 `ChatButton` 那一份父链）；
   建议按 A513 的形状**新开一笔账**逐件查（每件都要走一次父链 + 一条 1e-4 断言）。**我没改其余任何一件。**
4. **A515 的「真卸载 → 再进场景」那条真路径仍然没验**（编辑模式不派 `OnDestroy`，见 A388(b) 的既有注）
   ⇒ 已挂在 `资料/真Play待验清单.md` 的 **D39**，本件不重复记账。
5. **`StaticHookSlots` 是我新加的公开口**（只有断言在用）。若调度台认为不该加公开 API，可以去掉 ——
   ④ 那条断言**不依赖它**（只用它打印）。加它的理由是「清单只写一处、槽数别在别处抄第二份」。

---

## 六、顺手发现（⛔ 一条都没改）

1. 🔴 **`AttachPostFx` 会把 `OnPostFx` 置 null 而 `Begin` 后半段还有一条 `return`**
   （`Battle/BattleDriver.cs`：`AttachPostFx` 第一句就是 `DetachPostFx()` = 把静态回调清掉；
   取不到 `Volume` 时**保留 null 并出声**）。⇒ 「钩子数」这件事**本环境是 19、换环境可能少一条** ——
   我因此把 A515 的期望值改成了同一次运行的基线（见 §四）。
   **建议**：这是**设计使然**（原版没有战场 Volume 就不该有后期），不是缺陷 —— 记一笔是为了**以后写这类断言别写死数**。
2. 🟡 **`Editor/BattleScene.cs` 的 A388(a) 段注释里那句「`Begin` 之后 ≥ 16」**现在实际是 **19**
   （修完 A388 之后 `HeroBySeat`/`SeatOf` 也回来了）—— 注释与数字**不矛盾**（它本来就写着「数不是判据」），
   只是那个 `16` 现在离真值更远了。**我没改**（不在本件范围，且改了要动 (a) 段文案）。
3. 🟡 **同族「取整字面量」的剩余面**见 §五·3（`BuildHudExtras` 其余 9 件）—— **这可能是 A513 的下一笔账**。
4. 🟡 **A 表 `:398` 那条 A410 的出处行号（`Battle/BattleDriver.cs:1708`）与现读（改前 `:1735` / 改后 `:1786`）对不上**
   —— 又一处 A338 那族（我们自己的行号引用会漂）。**我没去改 A 表**（正本只有主对话能写）。
5. 🟢 **本次没有发现新的实现缺陷** —— 三处都是账上已知的那三件。

---

## 七、类型检查结果

命令（简报指定，独立 `TMPDIR` 避免与别的代理互相覆盖）：

```bash
TMPDIR=/tmp/wf_wb1 bash d:/4/Unity/工具/typecheck.sh
```

| 次 | 运行时程序集 | 编辑器程序集 | 说明 |
|---|---|---|---|
| 第 1 次（三处改完，A515 断言还是第一版） | **0** | **0** | 干净 |
| 第 2 次（改完 A515 基线那一版之后） | **1** ⚠️ | 0 | 报错在 **`Shell/MainMenuRuntime.cs(907,13)`：`CS0103 当前上下文中不存在名称"BuildResourcesBar"`** —— 🔴 **不是我的文件**（我在白名单里只碰了 `Battle/BattleDriver.cs` 与 `Editor/BattleScene.cs`，全仓 diff 可查）⇒ **没去改它**，如实记 |
| 第 3 次（隔 75 秒重跑） | **0** | **0** | ✅ **确认第 2 次那条是别的写手正在写 `Shell/MainMenuRuntime.cs` 造成的瞬时态**（那个文件现在 `git status` 里是 ` M`，不是我的改动） |

**结论**：**两套程序集最终 0 错**。第 2 次那条错**全部集中在一个不属于本件的文件上**，
按简报的口径「重跑一次 + 如实记，⛔ 别改别人的文件」处理。

**本件动过的文件（`git diff --numstat`，无整篇行尾翻转）**：
```
109  23   Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs
 85   1   Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs
```
⚠️ `git status` 里另有 **十几个别的文件**（`Shell/*.cs` · `Deck/*.cs` · `Editor/{DeckScene,MainMenuScene}.cs` · `工具/*.py` …）
是**本批次别的写手**在改，**与本件无关**（我一行都没碰）。
