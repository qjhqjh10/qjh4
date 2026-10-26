# B8_AA · `CardView` 那批 B 行的【视图层可观察量】

> 执行代理 **AA**（代号 `B8_AA`）· 2026-10-26
> 白名单 = `CardPresentation/Core/CardView.cs` · `CardPresentation/Editor/BattleScene.cs` · 账补丁 `账/_parts/B8_AA.tsv` · 本报告
> 红线执行：**没跑 Unity / 批处理**（只跑 `TMPDIR=/tmp/wf_b8aa 工具/typecheck.sh`）· **没动 git** · **没改主账**（只写 `_parts/B8_AA.tsv`）· **没改正本** · **没越白名单** · **没派子代理**
> `typecheck`：**运行时 0 / 编辑器 0**（两次：改完 `.cs` 后各一次）
> 行尾：`BattleScene.cs` **CRLF 47792 / LF 47792 / 裸 CR 0**（二进制核过，没翻）；`CardView.cs` **未改**

---

## ① 结论（数字）

| 项 | 数 |
|---|---|
| 账切片现读（`--file CardView --status B`，落点真指到 `CardView.cs:<行号>` 的） | **36 行**（任务书写 ≈31，现读 36 —— 以现读为准） |
| **B → A** | **18** |
| **保持 B**（其中 **10** 条本轮复核并重写备注/差异） | **18**（10 改 + 8 未动） |
| 新增自检断言 | **11 条 `Check(...)`**（伏击块 4 + 新方法 7） |
| 改动的 `.cs` | **1 个文件**（`Editor/BattleScene.cs`，净 **+136** 行） |
| `Core/CardView.cs` | **未改**（`B8_N` 建好的读口已够用，不再加成员 → **零行号位移**） |
| 账补丁 | `账/_parts/B8_AA.tsv` **28 行**（11 列 · LF · 无跨补丁重复键） |

**一句话**：`B8_N` 的「CardView 那批 B 行全卡在 G3（没有可观察量）」这条结论**不成立** ——
`BattleScene.Run` 里**早就有**一大片 `CardView` 的视图态观测量（状态环 / 徽标 / 虫群 / 场上队列 / 四格数值涨落色），
本批做的是 **① 把那些既有断言连回账（13 条 A 的观测量本来就在）· ② 补上真正缺的 11 条断言**，
并用 `ProjectSettings/TagManager.asset` 的排序层顺序给「谁压谁」那 6 条第一次找到了**外部判据**。

---

## ② 逐条去向（36 行）

### ②·a 升 A（18）

| # | 账行 | 原版方法 | 观测量（两侧） | 读数口 |
|---|---|---|---|---|
| 1 | 卡牌面_渲染:44 | `CardHighlight.Start()` | 新建卡 `State==Normal` 且 `RimVisible==false` vs 原版 `Start()` 末步 `ChangeFrameColor(RegularColor, **time 0**)` + `ChangeFrameColor` 的「alpha==0 ⇒ `ToggleFrames(false)`」 | **新** `CheckCardViewState`(1) |
| 2 | 战斗逻辑驱动:776 | `CardScript.AlreadyInPosition` | `SetPose` 同值调用前后 pos/rotZ/BaseScale 逐值不变 vs 原版「dot>0.999999 且 ｜Δ｜<1e-5 ⇒ 早退、不写 transform」 | **新** `CheckCardViewState`(4) |
| 3 | :998 | `ReturnToOriginalPos` | `SetPose` 落位后 pos == 目标（瞬移、非补间半程） | **新** (4) 前置条 + 同值条 |
| 4 | :1011 | `SetAsUnitInHand` | 手牌卡 pos == `HandLayout.TryLastPose` 那一格 | **新** (4) + 既有补位补间断言 |
| 5 | :1024 | `SetInDeckPosition` | `SetPose` 后 pos/rot/BaseScale == 目标；同值再摆不动 | **新** (4) |
| 6 | :1039 | `SetupInDeck` | `Body3DVisible==false` + `SetPose` 位姿 == 目标 | **新** (4) + 既有读口 |
| 7 | :1030 | `SetSortingLayer` | `SubtreeQueues()` 每一层同号；手牌段 > 场上段 | 既有 `BattleScene`「A1672 …每一层都在它自己那一段里」/「场上整段在手牌段之下」+ **新** (2) |
| 8 | :1031 | `SetSortingOrder` | `Queue` 与 `SubtreeQueues()` 的 [min..max] 都 == 布局给的号 | 既有 A1672 三条 |
| 9 | :1035 | `SetUnitInPlayLayer` | 同格「我方队列 > 敌方队列」vs 原版同层内 order **10 vs 0** | 既有 A1672 行间条 |
| 10 | :1050 | `ShowCardInHandLayer` | 手牌段顶号 3000 > 场上顶号 2979；段下限 2980 vs 原版 `Hand` 层 > `Elements` 层 | 既有 A1672 + **新** (2) `QHandMoving` |
| 11 | :891 | `HighlightIcon` | `PulseCount +1`（认不出的关键词不涨）vs 原版 `DOScale ×1.6` | 既有「★ 脉冲高亮播了一次…」 |
| 12 | :823 | `DisplaySwarm` | `SwarmVisible` / `SwarmAnimationPlayCalls +1` / `SwarmOnSoundCalls +1` / `SwarmIconLocalPos` | 既有 W58⑤ 三条 |
| 13 | :976 | `RemoveSwarm` | 关的时候**不出声**、不重播动画；0.3 s 淡出后 `SwarmVisible==false` | 既有 W58⑤ 两条 |
| 14 | :1051 | `ShowCardLight` | 点亮 ⇒ `RimVisible==true` + 走完 0.1 s `RimColor ≈ #00FF21`；熄灭 ⇒ 淡出后 `RimVisible==false` | 既有状态环那一节三条 |
| 15 | :1059 | `StopDisplayingActiveAbility` | `SetHighlight(Normal)` 后 `RimVisible==false` vs 原版 `ChangeState(0)` ⇒ alpha 0 | 既有「常规态 → 环淡出之后关掉」 |
| 16 | :1109 | `UpdateCostText` | `FaceCostText.text == "3"` 且 `FaceCostVisible` vs 原版 `SetText(currentCost…)` 印裸数字 | **新** (3) + `CardBaseDemo`「① 费用数字是真 TMP」 |
| 17 | :1110 | `UpdateFigures` | 四格数值 3/2/1/4 + 费用 3 + `BadgesShown == Data.badges.Count` | **新** (3) + 既有徽标恒等式 |
| 18 | :1115 | `UpdateTraitIcons` | `BadgesShown == badges.Count` / 清空后 0 / 第 i 位贴图 | 既有三条 |

### ②·b 保持 B（18）

| 账行 | 原因（本轮复核结论） |
|---|---|
| `UpdateHealthText(bool)` :1111 | **降 A → B**（本轮先升后降，如实记）：Bastion 那一支 `UpdateBastionr` **缺席**，属真缺口（门槛③） |
| `ShowCardInCenter(float)` :1048 | 位姿那一半有观测量了，但**多段补间的时长/缓动取自 `globalVars`，我们那张表没逐值对** |
| `UpdateAssetInPlayPosition(...)` :1107 | `setAsPermanent` 无等价物；`doAnim` 两支分流没逐支对 |
| `UpdateMinionInPlayPosition(...)` :1113 | 同 1107，另 `isFromCard` 分支未核 |
| `TryCardHighlight()` :1098 | 四态观测量已在案，但 `IsAeldariRemnant → ShowOriginalRemnantCard` 分支我们**没有入口**（真缺口） |
| `UnDissolveWholeCard()` :1103 | 原版是 async 状态机且**顺带起一颗粒子（311）**，那颗粒子是哪个 prefab **未解** |
| `UndissolveToDeck()` :1104 | `SetCardImage` 那一半有判据有读口；`FadeCardItems` 的**时长/对象集未取**、`SetActive` 收尾无单一入口 |
| `SetRawCardData(...)` :1028 | 判据全文已读；**只差一条断言**（`CardView.TitleText.text == 卡名`），但兜底卡面上 `_title` 建不建**没核**，可能假红 ⇒ 先核再写 |
| `SetMulliganSortingLayer()` :1026 | 原版关的是**子画布** `overrideSorting`；我们没有子画布 ⇒ 这个**开关状态**没有等价物（只有「谁压谁」的净结果）⇒ 缺可观察量，不是缺实现 |
| `ToggleOverrideSorting(bool)` :1066 | 同 1026 |
| `SetAsUnitTransforming()` :1013 | 原版用状态位 15「变形中」；我们无中间态（残骸换体由视图直切） |
| `SetEnemySecret()` :1020 | `BattleCardUI__SetEnemySecret` `:40` 之后未逐句读完；secret 面 sprite 未取 |
| `SetStateInHand()` :1033 | `SetCardTextInHand` 的「手牌档文本」与我们 `SetData` 的手牌档是否逐字一致**未核** |
| `SetTraitsOnPlayAttackingStatus(bool)` :1034 | 原版 `pointerEnter/exit` 时切 `hideStatus` 的分支我们没有（真缺口，P2） |
| `ShowEnemyCardInHand()` :1054 | 状态机 `MoveNext`（ILSpy 4564-4648）未逐句读 —— 如实记「没读」 |
| `StopAnimsInPlay()` :1058 | AnimFXController 那侧是否要一并 Kill 未核 |
| `TransformUnitInPlayToCard()` :1069 | `ResetTeleportCardOutOfBattle` 的等价行为未核（我们的传送只走 PlayCard 一支） |
| 特效动画:275 `NestedFadeGroupBase.RefreshAlpha()` | 「不嵌套」已查清；剩余差异是**粒度**，本轮未细读 |

---

## ③ 新增的 11 条断言（含**灭自证**设计）

### ③·a 伏击「面朝下」那一块（`Run()` 现读 `:2920-2975`，+4 条）

判据：`CardTextCountersController.HideStat`（🥈 `:698-721` / 🥇 `.c:26-33` 的静态串 `"?"`）· `CardScript__SetAmbush.c:21-41`（只对面那一方）· `CardScript__ResetHide`（只清 `hiddenStat`）· `BattleCardUI__SetCardImageTo3DBase.c`（ambush 词条在才用伏击图）。

1. `★ 埋伏（原版 isPlayer == 0）⇒ 数值位印 ?，不是空白`
2. `★ 灭自证·另一面：看得见数值的那一侧一颗 ? 都不该有`
3. `★ 翻开之后 ? 也收掉了`
4. `★ 翻开之后 3D 卡体 _CardImage 换回自己的立绘（⛔ 不再是伏击图）`

**灭自证设计**（结构上互不代偿，不是同一个 bool 的两面）：
- ① vs 既有的「四个数值层读 `activeSelf` ⇒ 都关着」：**只关层不印 `?`** 的那一版，②红？不 —— 是**①红、既有那条仍绿**。
  一条管「层关没关」、一条管「字印没印」，改坏任一侧另一条都不会跟着变。
- ② 与「两边都印 `?`」（删掉 `isPlayer` 那道闸）**互斥**。
- ③ 与「只清数值层、不收 `?`」**互斥**。
- ④ 与「`SetCardImageTo3DBase` 恒喂伏击图」**互斥**。

### ③·b 新方法 `CheckCardViewState(driver, Check)`（`BattleScene.cs:47683-47791`，+7 条）

调用点在 `Run()` 的「⑥ 场上真单位」之后（现读 `:3588`）—— 那里已经确认场上有卡。

| 条 | 断言 | 原版期望值 + 推导链 | 🧨 改坏法 |
|---|---|---|---|
| (1) | 新建卡 `State==Normal && !RimVisible` | `CardHighlight__Start.c:13-36`（缓存 scale → `ToggleFrames(1)` → `ChangeFrameColor(+0xc0 的色, 0)`）+ `ChangeFrameColor.c:24-57`（**time<=0 且 color.a==0 ⇒ `ToggleFrames(false)`**） | 初始态改 `ValidTarget` |
| (2) | `QHandMoving(3001) > 手牌段顶号(3000)` | `TagManager.asset:46-73` 的 `m_SortingLayers` 顺序：… Hand < **HandMoving** < OverHand …；原版 `ShowCardInHand.c` / `ShowCardBeingDrawn.c` / `ShowCardInCenter.c` 把「正在动」的卡放 `HandMoving` | `QHandMoving` 塞进手牌段 |
| (3a) | 2D 四格数值 = `"3","2","1","4"` | `CardScript__UpdateHealthText.c` / `__UpdateAttackText.c` / `__UpdateCostText.c:1-22` 都是「读当前值 → `SetText`」⇒ 卡面印**裸数字** | 退烘图（读口返空串）/ 印 `?` |
| (3b) | `FaceCostText.text == "3"` 且 `FaceCostVisible` | `UpdateCostText.c:1-22`（7 参 `SetText(currentCost,…)`） | 删 `BuildFaceCostText` |
| (4a) | **（前提）** `SetPose` 真的会摆位 | —— | —— |
| (4b) | 同值再摆一次：pos / rotZ / BaseScale **逐值不变** | `CardScript__AlreadyInPosition.c:13-36`（dot>0.999999 且 ｜Δ｜<1e-5 ⇒ 早退、不写 transform） | `SetPose` 每次多叠一个偏移 |
| (4c) | 缩放进 `BaseScale`（不是 `localScale`） | 原版 `CardHighlight` 缓存的是 `CardBodyToScale` 初始 scale、状态环只乘系数 | 直接写 `localScale` |

**灭自证设计**：
- (3) 的前提是 `TmpFont.Available`；取不到时**不假绿** —— 走 `Debug.LogWarning` 明确「本刻没跑」，账里也照写。
- (4) 用「异值 ⇒ 真的会动（4a）+ 同值 ⇒ 不动（4b）」一对：**只留 4b 会被「`SetPose` 空转」骗过**（那是 4a 的职责）。
- (2) 是**方向断言**（不是绝对值）：期望方向来自外部判据（TagManager），读数来自生产布局。
- (1) 与既有状态环那一节（`Playable` 不亮 / `ValidTarget` 绿 / `Normal` 淡出）**不是同一条**：那一节验「切换」，本条验「出生」。

---

## ④ 改了哪些文件、哪些行（新增行数）

**只有 `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`**（47656 → 47792 行，净 **+136**）

| 位置（现读行号） | 内容 | 净增 |
|---|---|---|
| `:2929-2939` | 伏击：`?` 印出来了（+ 判据注释 + 灭自证说明） | **+11** |
| `:2952-2956` | 伏击：我方那一侧一颗 `?` 都没有 | **+5** |
| `:2954`（同一段） | 伏击：翻开后 `?` 收掉 + `_CardImage` 换回自己立绘 | **+10** |
| `:3588` | `CheckCardViewState(driver, Check);`（调用点） | **+1** |
| `:47683-47791` | 新方法 `CheckCardViewState(BattleDriver, Action<bool,string>)` | **+109** |

### 行号位移表（给 `check_ledger_points`）

| 账里写的原行号 | 现在该加 |
|---|---|
| **≤ 2928** | **0** |
| **2929 – 3561** | **+26** |
| **≥ 3562** | **+27** |

新方法在**文件尾**（47683 起）⇒ **不移动任何既有行**。
现读账里引用 `Editor/BattleScene.cs:<行号>` 的只有 **6 处**（全库扫的），漂移量很小；**本批没有 `--fix`**（有别的代理在写 `.cs`）。

---

## ⑤ 没查清的部分（如实记）

1. 🔴 **新加的 11 条断言我一次都没跑过**（子代理不许跑 Unity）。它们的「我们侧期望读数」是**读代码推出来的**（例：`FaceStatTextNow` 应为 `"3"`），不是实测。**最可能红的一条** = (3a) —— 若兜底卡面（`CardData.Simple`，`faction=null`）上 `_faceStatText` 没建出来，读口返 `""`。重跑判据见 §⑦。
2. 2D 卡面 `_title` 在兜底卡面上建不建（决定 `SetRawCardData` 能否升 A）。
3. `ShowCardInCenter` 的 `globalVars` 时长表逐值对未做。
4. `UnDissolveWholeCard` 的粒子（311）未解（`CardScript.<UnDissolveWholeCard>d__311` 的 `MoveNext` 没读）。
5. `UndissolveToDeck` 的 `BattleCardUI.FadeCardItems` 两个参数（时长/对象集）未取。
6. `TryCardHighlight` 的 `ShowOriginalRemnantCard` 是「换数据」还是「换表现」未判。
7. `UpdateHealthText` 的 `UpdateBastionr` 语义（Bastion 数值叠加显示）未取。
8. `NestedFadeGroupBase.RefreshAlpha` 的「粒度」差异本轮未细读（原账 P2）。
9. 「手牌层 vs 场上层」的层序判据我用的是 **AssetRipper 导出的 `TagManager.asset`**；它与**运行期**那份是否一致没有实机核（原版已关服，运行期 TagManager 只能在能起游戏时看）。这条**不影响方向结论**（Hand 在 Elements 之上是常识 + 原版 prefab 里手牌卡在 UI 画布上层），但**如实记来源**。

---

## ⑥ 顺手发现（比多升一条 A 更有价值的几条）

1. 🔴 **`B8_N` 的「全部卡在 G3」是误判**：`BattleScene.Run` 里早就有一大片 `CardView` 读数口 ——
   状态环 `:3028-3075`（6 态 + 补间半程 + 瞬切）· 徽标 `:3386-3421`/`:3558` · 虫群 `:42443-42482` ·
   场上队列 `:20157-20247`（含「场内每层同号」「场上整段在手牌段之下」「我方 > 敌方」）· 四格数值/落色 `:3438-3495`。
   ⇒ **本批 18 条 A 里，13 条的观测量本来就有**，缺的只是「有人把断言连回账」。
   **建议**：以后判「表现层没有可观察量」之前，先 `grep -n "Check(" Editor/<宿主>.cs | grep <类名/读口名>`。
2. 🟢 **新判据源**：`d:/2/tools/assetripper_out/ExportedProject/ProjectSettings/TagManager.asset:46-73`
   给出原版 9 个 sorting layer 的**顺序**：`Default < Background < Elements < OverElements < Hand < HandMoving < OverHand < PopUps < Blocking`。
   这是「谁压谁」第一次有**外部判据**（以前只能写「原版明写」）。
3. 🔴 **`SetRawCardData` 的「卡名 + 空格 + id」不是卡面文字**（铁律 5 订正）：
   `.c:42-47` 走的是 `UnityEngine_Object__set_name`（**对象名**，玩家看不见）⇒ 原账把它记成可见差异是**误记**，已在补丁里改掉。
4. 🟢 **`CardHighlight.Start()` 的机制补齐**：`ChangeFrameColor` 里 **`time <= 0` 且颜色 `a == 0` ⇒ `ToggleFrames(false)`**（`.c:24-57`），
   `Start()` 传的 time 就是 **0** ⇒ 「新建卡不亮」是**当拍**，不是「补间走完才关」。原账只写「alpha 0 ⇒ 立刻隐藏」，没说清机制。
5. 🟢 **`DisplaySwarm` 的 2D/3D 双子树闸已定死**：`cardState ∈ {0,1,4,8,9,10,12,16}` ⇒ 挂 `+0x30`（2D 子树），否则挂 `+0x1a0`（3D 子树）。
   我们「单槽位挂卡根（= 原版 `3DBody`）」是**等价物**，这条差异可以写死，⛔ 不用再查。
6. 🟢 **`UpdateTraitIcons` 的行号**：原账写 `BattleCardUI__UpdateTraitIcons.c`（132 行）；本轮没重读，只引用了既有结论（`Badges.cs` 头部那段）。
7. 🔴 **「自检读口恒 null / 恒空」这一类自证陷阱（本轮查了 4 个，都有明确的适用面）**：
   | 读口 | 什么时候**恒空** | 判据 |
   |---|---|---|
   | `FrameTexture` / `FrameDrawSize` / `FrameUvRect` | **场上形态**（`CardFace.Board`） | `CardView.cs:4412` 的 `if (!board)`（`B8_N` 已记，我复核仍成立） |
   | `FaceStatTextNow(s)` / `FaceCostText` / `FaceCostVisible` | **场上形态**（场上走四个字形网格层，不建 TMP） | `Core/CardView.cs:13208` 的 `bool face = _faceMode != CardFace.Board`；`SyncFaceCostText:13318` 同 |
   | **`TitleText` / `KeywordsText`（文字层）** | 🔴 **场上形态**（`if (_faceMode != CardFace.Board) BuildTextLayers(d);`，`Core/CardView.cs:4590`）—— **这一条是本轮新查到的** | 同上；⇒ 想拿 `TitleText` 当 `SetRawCardData` 的观测量，**必须建 `CardFace.Full`/`Hand` 的卡**，拿场上的卡会读到 null（而那不是缺陷，是原版「场上整张 2DCard 关掉」） |
   | `DissolveAmount` / `FrameDissolveAmount` | 没调过 `BeginDissolve()` / 没上过溶解材质 ⇒ **−1** | `Core/CardView.cs:6973` / `:6740`（−1 = 复位哨兵，不是 0） |

   ✅ **反例（不是陷阱）**：`RimVisible` / `RimColor` —— 状态环 `Build` 里**无条件**建（`CardView.cs:4567-4586`，不受形态闸影响）⇒ 各形态都读得到真值；本批 (1) 那条断言不是假绿。

---

## ⑦ 给调度台的请求（重跑单）

1. **`BattleScene.Run`** —— 唯一新增断言的宿主（11 条）。⚠️ 它会重写 `Battle.unity`，按铁律先留副本、跑完 `git diff --numstat` 对一眼。
   - 重点看这 4 条会不会红：`★ 2D 卡面四格数值 = 裸数字` · `★ 费用那一格也是真 TMP` · `★ 新建的卡：高亮态 = Normal 且状态环不亮` · `★ 同值再摆一次…`。
   - **红了不要直接改实现** —— 先判是「我们侧真不一样」还是「断言的前提不成立」（我在这 4 条上都写了撤回条件）。
2. **`CardBaseDemo.Run`** —— 本轮**没改它**，但 `UpdateCostText` 那条 A 引用了它的「① 费用数字是真 TMP」⇒ 合并后复核一次。
3. **账合并**：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_AA`（⛔ 不要不带参跑）。
   合并后跑 `check_a_vs_diff.py` / `verify_annot.py`。
   ⚠️ 我没有权限写主表 ⇒ 这两个工具我**离线复刻了它们的判法**在自己的补丁上跑了一遍：
   **缺注解 0 · 同名 0 / 异名已注解 18 · 强缺口词命中 0 · 11 字段齐 28/28 · CRLF 0 / LF 28 / 裸 CR 0**。
4. **`check_ledger_points.py`**：本批给 `BattleScene.cs` 加了行（位移表见 §④），现读只有 6 条账引用它。
   `--fix` 请照铁律在「没有 `.cs` 写手」时跑一次。
5. **转派（不在我的白名单，只写请求）**：
   - **`CardPresentation/Battle/CardDisplayWindow.cs`** 的伏击支（`ShowBattleCard` 里 `B8_N` 请求 3）：在 `Show(...)` **之后**对主卡那个 `CardView` 调 `SetStatsHidden(true, hideArmourCell: true)`。
     判据 = `BasicCardUI__ToggleStats`（护甲那件多一次 `Toggle(false)` ⇒ **整格关掉、连 `?` 都不印**）；写口已在 `CardView.cs:14251-14278`。
     ⚠️ 该档**可能静默**：`EnsureAmbushMark` 的 warning 只在 `_faceMode == Board` 之后才可能触发 ⇒ 展示窗那一档要先判 `TmpFont.Available` 并出声（`B8_N` §⑦·5 已记）。
     ⚠️ 它现在是 `战斗界面_HUD.tsv:307`（A 档）里写着「断言尚未写」的那条 —— 本批我**没有**替它写断言（那个视图实例在 `CardDisplayWindow` 里，不在我的宿主）。
   - **`BattleDriver.cs`**（敌方攻击类型高亮 `(melee<range)?Ranged:Melee` 喂 `SetOwnAttackType` 之前先推）—— `B8_N` §⑤·1 已立账（`战斗逻辑驱动.tsv:1108` C/P1），**仍未做**。
6. **`CardView.cs` 零改动** ⇒ `B8_N` 报的那 52 条 `CardView.cs` 漂移**不会因本批增加**。
