# 报告 · G 基础设施与未分类 B→A（第 4 轮）+ A 口径收紧

> 代理 G · 2026-10-25
> 账：`账/其他_基础设施.tsv`（769）· `账/未分类_其他.tsv`（754）
> 判据：`判据/基础设施与未分类.md`
> **本轮没有改任何 `.cs`** ⇒ 按铁律 12「纯账/纯文档零条」**不跑自检、不跑 typecheck**。
> 唯一 `d:/2` 只读取证：`cpp2il` 的 `WindowsManager.cs` / `TypedSerializer.cs` / `SerializationHelper.cs` / `ReflectionExtensions.cs`。

## 一 数字

**两块 B→A 的原始任务（100 + 196）**

| 账 | 段 | 改判后（本轮第一次落账） |
|---|---|---|
| 其他_基础设施 | 100 条 B | A 35 · B 3 · C 2 · D 60 |
| 未分类_其他 | 194 条 B（196 里 2 条名字对不上，原样留） | A 145 · B 17 · C 6 · D 26 |

**然后调度台下达 A 收紧 ⇒ 全量重判（见 §三），最终：**

| 账 | 行数 | A | B | C | D | E |
|---|---:|---:|---:|---:|---:|---:|
| 其他_基础设施 | 769 | **11** | 28 | 259 | 471 | 0 |
| 未分类_其他 | 754 | **34** | 173 | 64 | 483 | 0 |
| 泛型与启动链_补读 | 866 | 73 | 84 | 186 | 521 | 2 |
| 社交_聊天 | 444 | 25 | 155 | 16 | 248 | 0 |
| 个人资料_成就_排行 | 341 | 52 | 219 | 1 | 69 | 0 |
| 教程_引导 | 155 | 52 | 90 | 0 | 13 | 0 |
| **合计** | **3329** | **247** | **749** | **526** | **1805** | **2** |

## 二 你点名的五处抽查

### ① `SerializationHelper`（10 条，D）
- **消费点查实**：`TypedSerializer.cs:40/173/174` + `TypedDictionarySerializer.cs:200`（**只有这两个消费者**，都是 Json.NET 的 `JToken` 读写）。
- 我们这边**没有**「按类型反射读写」这一层：存档/数据全走 `JsonUtility` + 显式字段 ⇒ **D + 替代物 = `JsonUtility`**。
- 账里 10 条备注已按此写（原来就有，本轮复核确认）。

### ② `ReflectionExtensions`（10 条，D）—— **原来是空备注，本轮补上**
- **发布包里没有调用点**：按 5 个方法名在 `cpp2il` 全量 grep（`GetFieldRecursive` / `GetPropertyRecursive` / `FetchField` / `FetchProperty` / `CallMethod`）**0 命中**（只有它自己）。
- ⇒ 死代码/编辑器侧；本工程也没有按名反射层 ⇒ **D，替代物「不需要」**。备注已补（原来差异栏是空的）。

### ③ `BiDirectionalDictionary<TKey,TValue>`（16 条）—— **C/A 混判 → 统一 D**
- **唯一消费点** = 原版 `WindowsManager.automaticallyLoadedWindows`（`WindowsManager.cs:764` 声明；`:1300` 开窗前 `TryGetValue`；`:1336` 建完 `Add`；`:1623-1628` 关窗时 **`TryGetKey(window)` → `Remove(key)`**）。
- **我们的等价物** = `Shell/WindowsManager.cs:671` 的 `_openByRef`（单向 `Dictionary<string,GameWindow>`，键 = prefab 根名；
  关窗拿不到回调 ⇒ **惰性删**，`:664-665` / `:857-874`；行为等价「关过之后永不复用」）。**反向查找**由「`openWindows` 里还有没有它」覆盖。
- ⇒ **架构上不需要这个通用容器** ⇒ 16 条全 **D**，替代物 `_openByRef` 写进每条。
- 🔴 **同步**：`账/泛型与启动链_补读.tsv` 里同类的 **15 条**（原 A 6 / C 9）也一并改 D —— 两份账口径一致（铁律 6）。

### ④ `LoadableReference` —— 不重复判（按你说的）
- `其他_基础设施` 里 17 行，其中只有 **2 行**（`GetReference` / `GetTargetId`）是本轮从 B 落下来的，备注写「见 `账/泛型与启动链_补读.tsv`」。
- 两份账逐方法对过：**状态无冲突**（补读账 19 行 = C 10 / D 8 / A 1；基础设施账 17 行 = C 12 / D 4 / A 1，
  差额是 `op_Implicit`×4 与两个比较运算符在基础设施账里没有对应行）。
- `ValidateReferences()` 原版**方法体为空** ⇒ 落点写「（原版方法体为空 ⇒ 无需落点）」。

### ⑤ `LoginManager` 残余（35 条）—— **逐条复核过**
- 两份账 **35 vs 35，逐方法状态零差异**（D 33 / A 0 / B 2 —— 2 条 B 是 A 收紧退下来的，见 §三·5）。
- 33 条 D 全部带「离线不适用 + 替代语义/断点」，没有一条是光秃秃的「离线不复用」。
- 启动链里另外 16 条 `SceneInitializer`（H 已落地）：**15 条 A**（落点 `Shell/SceneInitializer.cs:<声明行>`），
  1 条 B（`GetUnityFormatStacktrace` —— 该行为在我们这边**确实没有同名/等价方法**，不硬凑）。

> 📌 **你消息里说的「62 行是抄你的账（LoginManager 34 + LoadableReference 12 + SceneInitializer 16）」**：
> 盘上实际是 **LoginManager 35 + LoadableReference 17 + SceneInitializer 16 = 68 行**。
> 这 68 行里 63 行本来就不是 B；**我只动了仍是 B 的 5 行**（LoginManager 3 + LoadableReference 2），都标了「见 G 补读账（不重复判）」。

## 三 A 口径收紧（调度台 2026-10-25 裁定）—— 这条我照做了，并扩到全部六份账

### 你点名的那 85 行
`未分类_其他.tsv` 里**备注含「类级」的 A 行 = 恰好 85 条**（与你数的一致）⇒ 全部退 **B**：
只改状态/差异（差异写「类级命中；方法级对应未核」），**判据要点与落点原文一个字没删**。

### 顺着你第二句（「其他账里也自查收紧」）做的全量重判
判据：**A 必须双侧方法级**；`*`（整类行）一律 B。

| 账 | A→B | A 改写成 `文件:行`（保留 A） | A 原行号合格 | 收紧后 A |
|---|---:|---:|---:|---:|
| 其他_基础设施 | 36 | 0 | 0 | 0 → 补 11（见下） |
| 未分类_其他 | 73 | 15 | 15 | 103 → 30 → 34 |
| 泛型与启动链_补读 | 86 | 54 | 17 | 157 → 71 → 73 |
| 社交_聊天 | 155 | 25 | 0 | 180 → 25 |
| 个人资料_成就_排行 | 214 | 46 | 0 | 260 → 46 → 52 |
| 教程_引导 | 90 | 29 | 23 | 142 → 52 |

**为什么退这么多 —— 抽样实据（这不是「我没做」，是「落点不成立」）**：
- `Loc.cs:720` = 一行**分节注释**（`// ---- Army ----`），不是 `GetCardArmyTerm` 的实现；
- `TutorialFlow.cs:314` = `_selectSeen = true;`（函数中间一行），不是 `FlowTutorialStage.Complete`；
- `AchievementTally.cs:53` = 讲 `CardPlayedCount` 的**注释**，不是 `OnCardPlayed/OnInitialize/OnReset`；
- `SettingsWindow.cs:932`「字段表实读」被 4 个 `AccountTab` 方法共用 —— 典型「一个落点覆盖多个方法」。

**判定流程（可复现）**：① 落点行必须是方法声明/数据表项 → ② 不行就在**同一文件**按方法名（或备注里的方法名）找声明行 → ③ 仍没有 ⇒ B。
**特意没做**「全仓唯一同名」兜底：实测会把 `OnDestroy` 认到 `BattleMusic.cs:262`、`CleanUp` 认到 `BadgesNotifications.cs:170` 这种**别的类**上 —— 假精确比不精确更糟。

**补回的 12 条**（同一文件里确实有声明，只是我第一版判定太窄）：`CardSlotPreview.Initialize`→`DraggableController.cs:118` ·
`Countdown.Update`→`TimerDisplay.cs:116` · `MessageList.Initialize`→`InboxWindow.cs:572` ·
`UnitOnBoardAttackTypeSelector.Awake`→`AttackSelector.cs:817` · `SceneInitializer.InitializeBehaviours`→`:271` ·
`SceneInitializer.AsyncInitialization`→`:367` · `RankedFastMode.ToggleRankedMode`→`RankedData.cs:332` 等 6 条排行窗。
**基础设施另补 11 条**（原版名不同、我们确有等价方法，已逐条核到声明行；表见 `判据/基础设施与未分类.md` §二）。

**同步改了的口径文档**（防止两份打架）：`判据/社交与成就.md` §一、`判据/教程_音频_本地化.md` §一、`判据/泛型与启动链.md` §一，
都留了「2026-10-25 订正 + 原数字 + 原因」。

## 四 新 C（8 条，全 P2）与「核不动」的 B

**新 C**
- `GameStaticData.CheckIfNewCards` / `FoundNewCard` —— 缺「新卡标记」状态（本地没有「新卡」概念）。
- `Decorator` ×5（`Awake` / `OnObjectUpdated` / `OnDecorableUpdated` / `OnEnable` / `OnDisable`）—— 缺「可装饰对象的订阅-刷新」机制；本工程各页自刷。**归活动线/通用件**。
- `UIResourceCounter.AddQuantity` —— 缺数字滚动动画（我们直接改值重画）。

**「核不动」B（22 条，清单在判据 §三）**：其中 8 条是「有等价物但形态不同」（uGUI 组件 vs 我们的 `ImageQuad` 网格 / 拆在各 builder 里）；
`ContainerBuilder/ContainerHolder` 7 条与 `ExpansionPass…` 1 条属**活动线暂停**。
⚠️ `ExpansionPassProgressBarScaleAndPositionController` 的差异栏已写明**我们画静态尺寸、原版按 milestone state 顶位置** ——
活动线恢复时应改判 **C**（本轮按暂停线不动）。

## 五 没查清 / 需要你拍板的一条

1. 🔶 **口径边界**：**数据表项**算不算方法级？我按「算」处理了（`FilterPanelModel.RarityNames:386` = 稀有度名函数的返回物，
   所以 `CardRarityToString` 记 A）。如果你认为数据表项也该算类级，那基础设施这 11 条里 1 条要退 B。
2. `LoginManager.IsActivatedLanguage` / `SetInitialLanguage` 两条落在 `Loc.cs:149`（12 语言枚举）/ `:178`（`Default = Chinese`）——
   按上面的数据表口径它们可以是 A，但它们是**常量/枚举**不是「做这件事的那几行」，我**保守记了 B**。要不要算 A 请你定。
3. `GameStaticData.IsWarlordEquivalent` 的**等价类定义**（哪些卡算督军等价）仍未逐条对（`CardDef` 有督军字段只是必要条件）。
4. 社交 16 / 泛型 186 的 C **继续不动**（LiveOps/PlayEvent 族 + 你点名的三条）—— 本轮零接触。
