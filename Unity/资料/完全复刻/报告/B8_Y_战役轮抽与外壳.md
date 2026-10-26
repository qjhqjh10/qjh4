# B8_Y —— `CardPresentation/Shell/**` 剩下的真缺口（战役 7 / 轮抽 10 / 收藏·卡组 2）

> 执行代理 `B8_Y` · 2026-10-26 · 白名单 = `CardPresentation/Shell/**`（+ `账/_parts/B8_Y.tsv` + 本报告 + `判据/战役开局_判据与落点.md`）
> ⛔ 未跑 Unity / 未动 git / 未改主表 / 未改正本。

---

## 一、结论（每块做到哪 / 判据缺什么）

| 块 | 条数 | 本轮做到哪 | 账上状态 |
|---|---|---|---|
| **轮抽 `RandomSeedProvider`** | 10 | ✅ **全族照反编译逐句落成代码**（`Shell/RandomSeedProvider.cs`，229 行）：`SeedOptions` + `ControlledSeedState`（ctor / `GetRandom(int,int)` / `GetRandom(float,float)` / `Reset`）+ 静态 7 个（`InitSeed` / `GetRandom(seedID,int,int)` / `GetRandom(seedID,float,float)` / `GetSeedState` / `SetSeedState` / `ResetSeed` / `GenerateRandomSeed`）。**没接回 `RollWarlords`**（见 §四·1） | **B**（G3 未跑 —— 观测量要跑 `UnityEngine.Random` 序列，本代理不许跑批处理） |
| **战役 7 条** | 7 | ✅ 判据**全部读透并落到行号**，常量全部实读；`判据/战役开局_判据与落点.md` 给逐句语义 + 施工顺序。**代码没落**（4 条 C 的落点全在 `Battle/**` 与 `RuleEngine/**`，不在白名单） | **3 条 D**（`PlayBossAnim` / `PlayCampaignBossAnim` / `GetCampaignPathData`）+ **4 条 C**（`LandWarlordsInCampaign` / `TimedCampaignSetup` / `TimedCampaignStartSequence` / `ShowHeroesIntroMessageCampaign`） |
| **收藏 `CardCollectionTab.ToFocus`** | 1 | ✅ 落了：`CollectionTabPage.ToFocus()`（**追加在类尾**）+ `CollectionWindow.RefillCollectionForFocus(page)`（页 0 = 重灌卡组格 / 页 1 = 重灌卡池 + 空态；页 2/3 照原版**不**重灌） | **C → B**（G3 未跑） |
| **收藏 `ItemDrawer.OnDestroy`** | 1 | ✅ 落了：新件 `Shell/ItemDrawerLeash.cs`（挂在抽屉根节点上的薄 MonoBehaviour，节点销毁 ⇒ `ItemDrawer.Release(view)`）+ `ItemDrawer.cs:1871` **同行**追加 `ItemDrawerLeash.Attach(v)`（零行数变化） | **C → B**（G3 未跑） |
| **账里 `Shell/` 相关的 B 行** | 173+ | ⚠️ **只做了「清点 + 判据复核」，没有批量升档**：主表里 Shell 的 B 行绝大多数卡在 **G3（要有可观察量）**，而 G3 必须跑 Unity ⇒ 不是我这一轮能升的（见 §四·4 的清单与建议） | 不动 |

**关键数字**：新文件 2 个（229 + 50 行）· 改动文件 4 个（`CollectionWindow.cs` +44 · `CollectionTabPage.cs` +15 · `ItemDrawer.cs` 1 行原地改写 · `DraftModeWindow.cs` 4 行 doc 原地改写）· 账补丁 **19 行**（11 列 / LF / 每行 11 字段，已验）· `typecheck` **0 / 0** · `check_ledger_points` **失败 0**、我这几个文件的漂移 **0**。

---

## 二、逐条证据

### 2·1 轮抽 `RandomSeedProvider`（10 条 → 全族实现）

判据（🥇 方法体全在）：
`decomp_full/RandomSeedProvider__InitSeed.c:18-64` · `__GetRandom.c` · `__GetSeedState.c:18-23` ·
`__SetSeedState.c:17-37` · `__ResetSeed.c:17-25` · `__GenerateRandomSeed.c:15-24` ·
`RandomSeedProvider.ControlledSeedState__{ctor:14-34,GetRandom:14-22,Reset:5}.c` · `RandomSeedProvider__.cctor.c`。
字段/枚举：`il2cpp_out/dump.cs:101069-101107`（`SeedOptions{0,5,10}` · `startValue@0x10` · `currentRoll@0x14`）。

我们落点（`Shell/RandomSeedProvider.cs`）：
`SeedOptions:39` · `ControlledSeedState:52` · `ctor:62` · `GetRandom(int,int):92-100` · `GetRandom(float,float):104-112` ·
`Reset:116` · `seedStates:128` · `InitSeed:136-142` · `GetRandom(seedID,int,int):147-151` ·
`GetRandom(seedID,float,float):154-158` · `StateFor:162-171` · `GetSeedState:175-180` · `SetSeedState:184-187` ·
`ResetSeed:191-198` · `GenerateRandomSeed:204-219` · 自检两个只读口 `SeedCount:220` / `ClearForTest:223`。

机制（G2）：
- 核心 = **存全局 `Random.state` → `InitState(startValue + currentRoll)` → `Range` → 还 state → `currentRoll++`**（逐句同形）。
  用 `UnityEngine.Random` 是**照原版**、且先例已有（`Battle/CardChoicePanel.cs:695`「随机选是 UI 侧的 `UnityEngine.Random`，原版就是」）。
- 差异①：`InitSeed` 原版先 `new(Random,seedInt)` 再按 `SeedOptions` 重写 `startValue`（`Offset` 档 `DateTime.Now` 调**两次**、以第二次为准）；
  我们只构造一次 ⇒ 差一两个 tick，无观测差异。
- 差异②：`ResetSeed` 命中但 state 为 `null` 时原版抛 NRE，我们返回 `false`（表里不会有 null；只为不静默）。
- 差异③：`Random.Range(int,int)` 是 **[min,max)**、`Range(float,float)` 是**闭区间** ⇒ 两个重载照原版各走各的。

原版唯一调用方（**订正**）：不是「`DraftEventBase` 某处」，而是 **`DraftEventBase.GetRandomWarlords()`（:2353-2932）**：
`:2560` `InitSeed(seedID, Fixed, deckHero)` · `:2610` / `:2858` `GetRandom(seedID, 0, 候选数)` 并移除已选 ⇒ **一条顺序流**。

### 2·2 战役 7 条

见 `判据/战役开局_判据与落点.md`（本报告不重复；那张表有逐句语义、七步时序、常量表、判据订正）。
三个 D 的判据落点：
- `PlayBossAnim` / `PlayCampaignBossAnim`：`decomp_full/BattleManager__PlayBossAnim.c:2-11` / `__PlayCampaignBossAnim.c:2-11`，
  体 = 一句 `CustomDebug.LogWarning("[WARNING] Implement")`（`.c:9`）。
- `GetCampaignPathData`：`decomp_full/WarlordPathData__GetCampaignPathData.c:2-20`；数据本地 0 命中（三条取证见判据文件 §2·7）。

### 2·3 收藏 `CardCollectionTab.ToFocus`

- 判据：🥈 `CardCollectionTab.cs:186-194`（`UpdateCollection(GetCollection())`）· `DeckCollectionTab.cs:158-165`（同形）·
  🥇 `decomp_full/CardCollectionTab__ToFocus.c:2-17`（体 = base + 对 `[this+0x30]` 的一次间接调用）·
  传导链 `GameWindow.TryOpen` 的 `Background` 支 → `GameWindowWithTabs.ToFocus():430-441` → `CurrentTab.ToFocus()`
  （我们同形：`Shell/GameWindow.cs:540` · `Shell/GameWindowWithTabs.cs:220`）。
- 落点：`Shell/CollectionTabPage.cs:86-92`（`ToFocus` 覆写，**追加在类尾**）→
  `Shell/CollectionWindow.cs:6358-6382`（`RefillCollectionForFocus`：页 0 `RebuildDeckListNow()` / 页 1 `RebuildCardsCells`+`RefreshCardsEmpty()`）。
- 机制：只重灌**格子**（数据源每趟重读：`CardsState.VisibleCards()` / `FilteredDeckIndices()`），⛔ 不重建页框
  （`RebuildCardsPage` 会清池 + 拆整页 = 「首开 75.5 s」那条性能史的重活）。

### 2·4 收藏 `ItemDrawer.OnDestroy`

- 判据：🥈 `ItemDrawer.cs:134-141`（`OnRelease()` + `drawerReferences.TryGetValue(this, out ref) ⇒ ref.Release(General)`）·
  登记侧 `ItemDrawer.cs:36-68`（= 我们 `ItemDrawer.cs:1871` 的 `_handles[v] = …`）。
- 落点：`Shell/ItemDrawerLeash.cs`（新件，50 行；类名与文件名相同 = `A1506` 纪律）+ `Shell/ItemDrawer.cs:1871` 同行追加。
- 为什么不用 `ConditionalWeakTable`：那样 `HandleCount`（自检读口）会变成 GC 相关的不确定值 ⇒ 自检写不出来。
  挂在节点上的薄组件是**先例做法**（`Shell/ShadeHitMark.cs` 就是「静态 HashSet 只增不减」的同一类病，解法同形）。

---

## 三、改了哪些文件 / 哪些行 / 新增行数

| 文件 | 改动 | 新增行数 |
|---|---|---|
| `MyGame/Assets/CardPresentation/Shell/RandomSeedProvider.cs` | **新文件**（`RandomSeedProvider` 全族） | **+229** |
| `MyGame/Assets/CardPresentation/Shell/ItemDrawerLeash.cs` | **新文件**（`OnDestroy` 钩子） | **+50** |
| `MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | 类尾追加 `RefillCollectionForFocus(int)`（`Close()` 之后，`:6338-6382`） | **+44** |
| `MyGame/Assets/CardPresentation/Shell/CollectionTabPage.cs` | 类尾追加 `ToFocus()` 覆写（`:86-92`） | **+15** |
| `MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs` | **`:1871` 一行原地改写**（`_handles[v] = res.Drawer;` 后加 `ItemDrawerLeash.Attach(v);`） | **0** |
| `MyGame/Assets/CardPresentation/Shell/DraftModeWindow.cs` | `RollWarlords` 的 doc **原地改写 4 行**（原写「原版方法体缺失」→ 已订正：ILSpy 体在场、机制是 RandomSeedProvider 一条顺序流） | **0** |
| `资料/完全复刻/判据/战役开局_判据与落点.md` | **新文件**（战役 7 条的判据全文 + 常量表 + 订正） | 新 |
| `资料/完全复刻/账/_parts/B8_Y.tsv` | **新文件** 19 行（11 列 / LF / 11 字段已验） | 19 |

⚠️ `ItemDrawer.cs` 是别人刚动过的那份「追加区」—— 我只改了 **`:1871` 这一行**（同行内追加，**零行数变化**），
没碰追加区的任何新成员。`DraftModeWindow.cs` 同理，只改 doc 文字、行数不变。

---

## 四、没查清的部分（⛔ 别当结论用）

1. **`RollWarlords` 要不要接 `RandomSeedProvider`** —— 原版是「一条顺序流、`startValue = deckHero`」，我们是「逐位独立种子」。
   接回来会让同 seed 的选将**全变**，而 `seedID` 那条字符串在产物里丢了、`deckHero` 我们在选将那一刻还没有 ⇒
   现在接 = **半猜**。判据够不够：**不够**（需要先拿到 `seedID` 或确认 `deckHero` 的来源）。
2. **战役局（`MatchType.Campaign = 130`）从哪开起来** —— ILSpy 全树 `= MatchType.Campaign` **0 命中**（覆盖约 29%）⇒ 查不到。
3. **`TimedCampaignSetup` 的调用方** —— 产物 0 个。
4. **`ShowHeroesIntroMessageCampaign` 传给 `ExecuteCampaignScriptedTurn` 的表 / `TimedCampaignStartSequence` 第 6 步那两个调用**
   —— 产物分别是「共享运行时函数名错位」与「全局共享空方法桩」⇒ **读不到**。
5. **`introStageStepTime` 的运行时值** —— `VarsGlobal` 是 `ScriptableObject`，序列化值要读 prefab/SO 实例，本轮没取。
6. **Shell 的 173 条 B 行没有批量升档** —— 绝大多数卡 G3（可观察量要跑 Unity）。抽样复核过的 5 条（`TabButtons.AddTabButton`
   的 Sprite 半边 / `GameWindowWithTabs.GetStartingTab` 的 options / `DestroyTab`·`ChangeTab<T>`·`OpenTabs` 零生产调用点）
   **都属实、都不是「Shell 里便宜就能修」的那类**（Sprite 半边要图标资源、options 要资产、零调用点不是缺陷）⇒ 没动。

---

## 五、顺手发现（⛔ 我只报，没顺手改的会写明原因）

1. **`账/战斗逻辑驱动.tsv:1358` 的方法名是错的**：写的是 `static(ControlledSeedState, bool)`，真名是
   **`GetSeedState(string seedID)`**（`cpp2il/out_cpp2il_dev_ilspy/RandomSeedProvider.cs:183` 的返回类型被当成了方法名）。
   我**没改键**（补丁按 `(原版类,方法)` 匹配，改键会变成「新增一行」而旧行留下）⇒ 请调度台用
   `fix_ledger_patch_keys.py` 改名，或手工改主表那一格。
2. **同一族还有两处「类归属」错**：`GetRandom(int,int)` / `GetRandom(float,float)` / `Reset()` 这三条账里挂在
   `RandomSeedProvider` 名下，实际是**嵌套类 `ControlledSeedState`** 的方法 ⇒ 补丁的落点列已写「（等价实现 = …）」。
3. **`BattleManager` 的 `threeSecDelay` 与 `fourSecDelay` 构造参数都是 3.0**（`DAT_1834b2e8c`，`BattleManager__.ctor.c:196-201`）——
   名字叫 four、值是 3.0，原版真值如此。⛔ 将来别按名字推值。
4. **`GameWindow.TryOpen` 的 `ToFocus()` 只在 `Background` 支被调**（`GameWindow.cs:180-200` 的 `flag4 = !(state==Open) && flag`
   推出来；`Closed` 支跑完后 state 已是 `Open` ⇒ 不调）⇒ 账里「切页签重灌集合」的说法**不准确**，已在两处就地订正。
5. **`DraftModeWindow.RollWarlords` 的 doc 原写「原版 `GetRandomWarlords()` 方法体缺失」—— 不成立**：
   🥈 `DraftEventBase.cs:2353-2932` 有体（只是 `decomp_full` 那份没有：泛型类 `// RVA: -1` 那族）⇒ 已原地订正，
   并把「原版是 `RandomSeedProvider` 一条顺序流」这条写进去（这是 §四·1 那个决策的依据）。
6. **`CollectionTabPage.cs` 插在类中间会让 `Setup` 从 56 → 69**，`check_ledger_points` 立刻报 **2 条漂移**
   （`收藏.tsv` 的 `CollectionTab.GetCollection()` / `CollectionTab.Setup(GameWindow)`）。我踩了一次、已挪到类尾
   （漂移 192 → 190，我的文件归零）。⇒ **纪律复述**：`Shell/` 里加成员一律「类尾」。

---

## 六、给调度台的请求

1. **跑一次 `ShellScene.Run`（9 s）并加三条断言**（这批 13 条 B 行的 G3 全靠它）：
   - `RandomSeedProvider`：`ClearForTest()` → `InitSeed("t", Fixed, 12345)` → 连取 3 次 `GetRandom(0,100)` 记下来 →
     再 `InitSeed` 同一个 → 再取 3 次 ⇒ **两串逐位相同**；`CurrentRoll` = **3**；`ResetSeed("t")` ⇒ `CurrentRoll` = **0**。
     **灭自证**：把 `InitState(startValue + currentRoll)` 改成 `InitState(currentRoll)` ⇒ 第一串必变。
   - `ItemDrawer`：画两格 ⇒ `HandleCount` = N；销毁其中一格节点 ⇒ `HandleCount` = **N-1**。
     **灭自证**：把 `ItemDrawerLeash.Attach(v)` 删掉 ⇒ 计数不变、这条红。
   - `CollectionTabPage.ToFocus`：收藏窗开 Deck 页 → 让窗进 `Background` → 再 `TryOpen` ⇒
     `RebuildDeckListNow` 真的跑过一次（可看 `DeckCells.Count` 或加一个计数读口）。
2. **把战役那 4 条 C 派给 `Battle/**` 的写手**（`判据/战役开局_判据与落点.md` §二 已给逐句语义 + 常量）。
   顺带一问：**战役局怎么开起来的**（§四·2）—— 这条不弄清，那 4 条落了也没有入口。
3. **账务三件**：① `战斗逻辑驱动.tsv:1358` 改名 `GetSeedState(string seedID)`；② 那三条 `ControlledSeedState` 方法的类归属；
   ③ 战役 4 条 `C` 的 `等级` 我按 `CLAUDE.md` §四 从 `P2` 订正成 **`P1`**（若调度台另有口径，请覆盖我这一格）。
4. **补丁合并在同步点做**：已 `--dry` 验过 ⇒ **更新 19 · 新增 0 · 无变化 0**。
   ⚠️ 合并前缀要写 **`--only B8_Y`**（不是 `B8_Y_`）—— 本补丁叫 `B8_Y.tsv`、末尾没有下划线，
   写 `B8_Y_` 会被 `startswith` 判成「没有补丁」。
   命令：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_Y`
   （按 `CLAUDE.md`：合并完把补丁归档到 `_parts/_merged_B8_Y/`）。
