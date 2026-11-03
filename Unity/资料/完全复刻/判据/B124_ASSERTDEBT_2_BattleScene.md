# B124 断言债分诊 2（BattleScene 134 红）· 2026-11-03

> 只读诊断代理（批次 B124）。日志 `_tmp_view/battle_b123.log`（166 MB，流式扫，**未整份读**）。
> 判据优先级：① `d:/2/tools/decomp_full/` ② prefab/YAML ③ 现读源码。**本文件不改任何代码/账/正本。**

## 〇 结论摘要

| 项 | 数 |
|---|---|
| 日志末尾合计（`BT === 合计：11615 通过 / 134 失败 ===`，log:1726870） | **134** |
| 实际打出的 `✗` 行（按「行首前缀 + ✗」匹配，非 `grep -c ✗`） | **133** |
| 差额 1 | 有 1 条被计入失败但**没打出 ✗ 行**（推测是宿主 `Check` 之外的异常/早退计数），本报告按 133 条分诊 |
| 宏分组 | **18 组**（细分组 39 个，按根因合并） |
| (α) 断言/判据本身过期 | **37** |
| (β) 实现缺陷（含待确认 23） | **33**（其中 5 条是同一根因的「前提」旁证） |
| (γ) 本批回归 | **0 条独立成组**（见下注） |
| (δ) 夹具前提不成立 | **46** |
| 待定（需 10–30 分钟隔离实验） | **13** |
| 混合归属（δ/α 3 · α/待定 1） | **4** |
| 合计 | 37+33+46+13+4 = **133** ✔ |

**关于 (γ)**：117–122 会话从未跑过 Unity，所以「15:24 全绿基线之后被改坏」的东西在账面上都是 γ；但逐条追下来，**绝大多数是「判据/夹具没有跟着
2026-11-02～11-03 的判据变更走」（α/δ）**，真正「产品行为变差」的只有 3 条链（错侧落点、提交后未结算、落点在手牌带）——它们的时间戳落在
2026-11-02 `B120-W24`（allowNearest）与 2026-11-03 `B121-WB`（删容差）之间，已在 β 里单列并给出出处。**没有一条是 123 会话新接的 9 个断言类自己写错**
（新接的类里只有 `B123C4` 出现过，且 15/0 全绿）。

---

## 一 分组总表

| # | 组 | 条数 | 四类 | 证据（断言源码 → 被断的实现） | 建议 |
|---|---|---|---|---|---|
| 1 | 拖拽跟手/落点夹具（让位重排 + 提交即出牌） | 13 | δ | `Editor/BattleScene.cs:6563/6610/6620/6628/6634/6647/6664/6671/6891` ← `Hand/CardInteraction.cs:1064-1068,1084` | 改夹具（见 §二-1） |
| 2 | 部署飞行闸 B2（同 #3 根因） | 7 | β待定 | `Editor/BattleScene.cs:41422/41425/41430/41438/41447/41454/41457` ← `CardInteraction.Release` 判 MissedSlot（log:903791） | 先修 #3 |
| 3 | 单位拖上场未落地 | 7 | β待定 | `Editor/BattleScene.cs:16338/16343/16411/16428/16454/16456/16458/16460` ← log:700340 `落点判定=MissedSlot` | 查 `BoardLayout.DropTargetWorld` 的**局部/世界**口径（§二-3） |
| 4 | 战术卡/造牌/部署提交后引擎未结算 | 8 | β待定 | `Editor/BattleScene.cs:19139-19145`、`:19171-19180`、`:19216-19217` ← log:720566/720582 提交 Ok 但 3 s 后全无变化 | 追 `Release → DeploySequence.onDone → LocalAct` 断在哪一跳（§二-4） |
| 5 | 错侧落点判 `Ok`（E8/X9 + 真实引擎） | 5 | **β** | `Editor/HandAssertE8.cs:559/561/570/576`、`Editor/BattleScene.cs:19117` ← `Board/BoardLayout.cs:524-540`（2026-11-03 `B121-WB` 删容差） | 回源：`DropReject` 的「指针在哪块棋盘」不能再用「解析成功」当判据（§二-5） |
| 6 | `B118-W18` 描边/状态环（前提塌） | 4 | δ | `Editor/B118W18_Assert.cs:175/183/197/323` ← `CardInteraction.cs:1084`（拖动当帧就提交） | 夹具改成「不越过提交闸」的拖法 |
| 7 | 3D 挡板 collider 取不到 | 11 | δ | `Editor/B118_T11_Assert.cs:83-118`、`Editor/B120_W13_Assert.cs:174/250` ← `Core/CardView.cs:109-118 + 4323/4421 + 16073` | 夹具卡补 `faction`（§二-7） |
| 8 | `B117-WGB` 拖拽公式（放弃闸提前触发） | 6 | δ | `Editor/B117_WGB_Assert.cs:157-204` ← `CardInteraction.cs:1084→2622 TryGiveUpTooHigh`（log:1083794） | 夹具首个点 dy=5 改成 < 3.43 |
| 9 | `B120-W16` 几何前提（槽 4→5、抬高 1.78） | 6 | δ | `Editor/B120_W16_Assert.cs:173/237/240/248/252` | 夹具重算 `Make()` 的卡/格坐标 |
| 10 | `B120-W24` 默认档容差（A1/B4） | 3 | α | `Editor/B120_W24_Assert.cs:173/187/246` ← `BoardLayout.cs:524-529`（有意删，注释写明） | 改断言（契约已变）+ 保留一条「产品路径不许回落到错侧」 |
| 11 | `B120-W24` C1 / `B120-W28` C2 几何 | 3 | δ | `Editor/B120_W24_Assert.cs:276`、`Editor/B120_W28_Assert.cs:443` | 同 #9 |
| 12 | 层号迁移后仍断 `ArenaLayer(8)` | 4 | α | `Editor/BattleScene.cs:13378/20997/24210/35140` ← 卡已迁到 `BoardCardLayer(10)` | 期望值改成 10（或 `badLayer` 判 10） |
| 13 | 战场相机 cullingMask | 1 | 待定 | `Editor/BattleScene.cs:47169` | 先打印实得掩码（现无值） |
| 14 | 命中矩形 / `HitSlot`（Q04e/f/g） | 7 | α | `Editor/BattleScene.cs:39185/39189/39247/39251/39281/39337/39363` | 已知判据变更，按 `hits[0]`/无回落重写 |
| 15 | 源级字面/计数/分支断言过期 | 17 | α | `BattleScene.cs:1311/1861/2232/16410`、`B120_W13_Assert.cs:124`、`B120_W17_Assert.cs:232`、`B120_W21_Assert.cs:456`、`B99B1_Assert.cs:353-362`、`B118W1_Assert.cs:365/368` | 逐条改字面量（§二-15，回报最高） |
| 16 | 选中态判据（`B-12` 同档不选） | 2 | α | `BattleScene.cs:9419`（`selectedType:4`）、`B117_G10_08_Assert.cs:182` ← `Battle/AttackSelector.cs:1583` | 夹具换 `selectedType` |
| 17 | 音源 `OnValidate` 前提 | 1 | δ | `B120_W11_Assert.cs:182`（`AddComponent` 已自动跑 OnValidate） | 改成「先清空再喊」 |
| 18 | 卡面/动画/演出/杂项（28 条） | 28 | 混合 | 见 §二-18 | 逐条 |

**合计：13+7+7+8+5+4+11+6+6+3+3+4+1+7+17+2+1+28 = 133 ✔**

---

## 二 逐组详述

### 1 · 拖拽跟手/落点夹具（13 条 · δ）
- 失败行：log 670552 / 670565 / 670675 / 670701 / 670714 / 670727 / 670740 / 670766 / 670792 / 671313 / 671351 / 671364 / 671377。
- 断言源码：`Editor/BattleScene.cs:6610`（`dt=0` ⇒ 卡不动）、`:6563`（指针/卡两个候选靶分得开）、`:6620/6628/6634`（让位插槽号）、
  `:6647`（只亮落点那一格）、`:6664`（落点指示挂在被拖卡底下）、`:6671`（长宽比）、`:6891`（落位被检测到）。
- 现场：`670539 空位右边的牌让位了（Hand_Flamecaller x 1.58 → 0.85）` 之后，`670552（前提）dt=0 这一喂卡一步都没挪 … 实得 1.42E+001`。
  ⇒ **`UpdateDrag` 同一帧里先写 `t.position`（`CardInteraction.cs:996`）再跑让位 `Relayout(false)`（`:1068`）**，重排把被拖那张卡一起摆走了
  ⇒「dt=0 ⇒ 不动」这个前提在**指针换了槽号**的那一喂上必然不成立。
- 后续 670701/670714/670727 的 `InsertIndex = -1`：`TryCommitWhileHolding`（`CardInteraction.cs:2399-2406`）在越过闸的那一帧
  就 `Release` ⇒ `_dragging = null` ⇒ 后面 24 帧 `SimulateDrag` 全是空操作（`CardInteraction.cs:1816`）；670740/670766/670792 因此读到**已收掉的**指示器。
- 建议：**改夹具**——① `dt=0` 那三条改成「先把指针放回原槽号（不触发让位）再喂 dt=0」，或改成 `dt=1e-6`；
  ② 「拖到棋盘」那一段改成**只喂一帧**（新口径下第一帧就提交），断言改读「提交后」的状态；③ 指示器三条移到 `SimulateRelease` 之前并在**同帧**读。
- ⚠️ β 疑似：如果产品里「让位重排把被拖那张卡也搬走」是真的，那会让卡在拖拽中跳一下（原版 `DisplayHandWithCardSpace` 不搬正在拖的那张）。
  建议顺手跑一次隔离实验（只调 `Relayout` 看被拖卡 x 是否变），**别直接判 β**。

### 2 · 部署飞行闸 B2（7 条 · β待定，根因同 #3）
- 失败行：log 903819 / 903832 / 903858 / 903871 / 903884 / 903897 / 903910；断言源码 `BattleScene.cs:41422-41457`。
- 现场：`log:903791 Release：指针世界 (-5.85,-0.86) · 卡 (-5.85,-0.84,-0.60) … 落点判定=MissedSlot` ⇒ **release 点在手牌带**，
  飞行根本没起；于是「闸是真的 / 起飞票 / 起飞 cue / 3 s 后引擎出牌 / 动作数」全部级联红。
- 判据：`BattleManager__Update.c:249-288`（越过高度即 commit）本身没问题，问题在**喂进去的点**。见 #3。

### 3 · 单位拖上场未落地（7 条 · β待定）
- 失败行：log 700380 / 700393 / 700461 / 700474 / 700487 / 700500 / 700513；源码 `BattleScene.cs:16321,16338,16343,16428,16454-16460`。
- 现场：`log:700340 Release：指针世界 (1.57,-0.86) · 卡 (1.57,-0.84,-0.60) · 落点判定=MissedSlot`；`log:700368 [DBG 16红#1] awaitingPlayTarget=False · 引擎格位有单位=False · 场卡视图在=False`。
- **可疑点（优先查）**：夹具用 `pBoard.DropTargetWorld(freeSlot)` 当**世界坐标**喂 `SimulateDrag`（`BattleScene.cs:16321`），
  而 `BoardLayout.DropTargetWorld` 返回的是 **`transform.InverseTransformPoint(...)` = 本物体局部坐标**（`Board/BoardLayout.cs:547-557`），
  它的 doc 却说「和 `SlotPosition` 同一个约定」（`:543-546`），而 `SlotPosition` 返回的是 `LayoutSpace.ToWorld(...)`（`:477-480`）——**两者不是同一个空间**。
  实测喂进去的点 (1.57,-0.86) 归一化后在**手牌带内**（对比 log:670617「指针在手牌带 y 0.122 < 手牌限位线 0.278」），而棋盘行心不在这。
- 判据：原版落点 = `GetTargetInPos` 的屏幕点 → `GetClosestAvailableSlot`；换算必须与 `SlotPosition` 同一空间。
- 建议：`BoardLayout.DropTargetWorld` 改成返回 `transform.TransformPoint(...)`（或把调用点统一成局部），并补一条「`DropTargetWorld(s)` 与
  `SlotPosition(s)` 同空间」的断言（现有断言 `BattleScene.cs:670896` 只比了两个口彼此相等，没有第三方锚）。**这是本批最值得先做的一条**：#2、#3 共 14 条。

### 4 · 战术卡/造牌/部署提交后引擎未结算（8 条 · β待定）
- 失败行：log 720598 / 720611 / 720624 / 720637（战术卡离开手牌/扣费/弃牌堆/目标挨打）、724795 / 724810（造 3 张药剂）、728822 / 728837（部署 2 个墓刃战机）。
- 源码：`BattleScene.cs:19139-19145`（`DriveTacticAndCheck` 里那四条）、`:19171-19180`、`:19216-19217`。
- 现场：`log:720566 提交（还按着就 commit）… 落点判定=Ok` → `log:720582 落位：Hand_Cleansing Flames → 战术卡打向 5（敌方半场）` → **之后 90 帧（3 s）内一条引擎日志都没有**，
  手牌 7→7、费用 4→4、弃牌堆不变、目标 31→31。夹具确实在 `SimulateRelease` 后推了 90×1/30 s（`BattleScene.cs:19134-19137`）。
- 判据：原版 `BattleManager.Update` 提交后走 `MoveIntoFieldFromHand` → `_ResolvePlayCardFromHand` 状态机 → 结算；我们的等价口是
  `CardInteraction.Release`（`:1380`）→ `OnDeployFlightStart`（`:1472`）→ 驱动 `DeploySequence` onDone → `LocalAct`。
- 建议：在 `Release` 的 `onDone` 前后各加一条临时日志（或读 `driver.WaitingForBlockingAction`），确认是「飞行没完成」还是「LocalAct 被 `WaitingForBlockingAction` 吞」。
  ⚠️ 注意 `log:903845 飞行中引擎还没出牌（手牌仍 5 张）✓` 说明飞行闸本身是通的 ⇒ 更可能是 **`LocalAct` 之前被 `CanPlayTactic` 拒**（而拒绝路径没出声，违反「不许静默失败」）。
  若确认是「引擎拒但没出声」，就是**真 β**（静默失败）。

### 5 · 错侧落点判 `Ok`（5 条 · **β**，本批最确定的产品缺陷）
- 失败行：log 693023 / 693053（前提：两块棋盘分得开）、693083 / 693098（X9-A / X9-B）、720540（X9-B 真实引擎）。
- 断言源码：`Editor/HandAssertE8.cs:556-576`、`Editor/BattleScene.cs:19114-19119`。
- 根因（实读）：`Board/BoardLayout.cs:524-529` —— **2026-11-03 `B121-WB`（账 `B120-Q04c3`）把 2D 兜底档的两个容差 `snapTolerance 0.036` / `snapToleranceY 0.09` 彻底删掉**，
  语义变成「永远返回最近候选格」（`:531-540`）。而 `CardInteraction.DropReject` 用「`TryResolveSlot` 是否成功」来判
  **「指针压在哪块棋盘」**（`Hand/CardInteraction.cs:1301-1305, 1315-1323`）⇒ 容差一没，**两块棋盘对同一个点都返回 true**（`log:693023/693053` 已经把这个前提打红），
  于是 `hitWant` 永远为真 ⇒ 敌方目标战术卡丢在我方同号格也判 `Ok`（`实得 0`），**静默打到敌方同号格**——正是 X9 这组断言当初要挡的那个 bug。
- 判据：原版 `BattleManager__Update.c:660-673`（`IsValidSpellTarget` 失败 ⇒ `NotifyCantDoAction(BattleTarget)`）；「指针在哪块棋盘」在原版是
  **由哪个 `MinionManager` 收到这次 Update** 决定的，不是由「能不能解析出格位」决定的。
- 建议：**修实现**——`DropReject` 里判侧别改用**与解析无关的量**（例如归一化 y 与各自 `lineY` 的距离、或哪块棋盘的 `lineY` 更近），
  并**保留** `TryResolveSlot` 现在「无容差最近格」的原版口径（⛔ 别把容差加回去，那会推翻 `B121-WB` 的判据）。
  再补一条断言：「同一指针点，两块棋盘 **至多一块** 被判为『指针所在棋盘』」。

### 6 · `B118-W18` 描边/状态环（4 条 · δ）
- 失败行：log 1716727（前提 `Dragging == card`）、1716743（②）、1716791（③b）、1717126（E）。
- 源码：`Editor/B118W18_Assert.cs:175/183/197/323`；现场：`1716727（前提）拖拽态还在（否则 ② 读的是松手后的值）` 红 ⇒ 拖动当帧就提交了
  （`CardInteraction.cs:1084 → TryCommitWhileHolding`）。
- 建议：夹具改成把落点放在**手牌带以内**（不越过 `PlayCommitGate`），让 `_dragging` 保持。

### 7 · 3D 挡板 collider 取不到（11 条 · δ）
- 失败行（9 条 `B118T11`）：log 1719143 / 1719157 / 1719171 / 1719185 / 1719199 / 1719227 / 1719241 / 1719255 / 1719269；另 `B120W13` B0b/C0：1708559 / 1708574。
- 断言源码：`Editor/B118_T11_Assert.cs:82-118`、`Editor/B120_W13_Assert.cs:174/250`。
- 根因（链）：夹具卡 = `CardData.Simple("TapBlockProbe",3,3,4)`（`B118_T11_Assert.cs:74`）⇒ `CardView.cs:109-118` 里 **`faction = null`**
  ⇒ `CardArt.Frame(null,…)` 直接 `return null`（`Core/CardArt.cs:73`）⇒ `CardView.Build` 的 3D 体整段在 `if (frameTex != null)`（`Core/CardView.cs:4323`）里
  ⇒ `BuildBody3D` 从没被调 ⇒ `EnsureTapBlockCollider`（`CardView.cs:16073`）没跑 ⇒ `_bodyCollider3D` 恒 null。
- 这是 CLAUDE.md 已记过的坑（「`CardData.Simple(...)` 的 `faction` 恒 null」）。
- 建议：**改夹具**——用 `new CardData { …, faction = "Ultramarines" }`（或 `CardDatabase.Load()` 里取一张真卡）。
  或者更彻底：`CardView` 在 `frameTex == null` 时也建 `body3D`（但那是产品行为改动，要先有判据，**暂不建议**）。
- 连带：这 11 条也解释了 #14 里 `B2b③`（collider 放大 1.2× 仍 False/False）。

### 8 · `B117-WGB` 拖拽公式（6 条 · δ）
- 失败行：log 1083813 / 1083829 / 1083845 / 1083861 / 1083894 / 1083927；源码 `Editor/B117_WGB_Assert.cs:157-204`。
- 现场：`log:1083794 拖拽被中止（拖得太高（高出按下点 5.00 我们的单位 > 3.43 = 25 原版世界单位）且这一手打不出去 ⇒ 收回手牌 …）`，
  栈 = `TryGiveUpTooHigh`（`CardInteraction.cs:2622`）← `UpdateDrag:1084`。夹具第一喂 `w1 = p0 + (100,5,0)`（`B117_WGB_Assert.cs:154`）**dy=5 直接撞上放弃闸**
  ⇒ `AbortDrag` 把卡摆回手牌、`_dragging = null` ⇒ 后面五条全在空转（位置恒 (0.13,-3.78,0)）。
- 判据：原版 `BattleManager__Update.c` 的 `maxHeightOfUnplayableCard` = 25 世界单位（= 3.43 我们的单位）——**产品是对的**。
- 建议：夹具第一个点 `dy` 从 5 改成 ≤ 3.0；②③④⑤ 相对第一点也要保持 dy 不超闸。

### 9 · `B120-W16` 几何前提（6 条 · δ）+ 11 · `W24` C1 / `W28` C2（3 条 · δ）
- 失败行：log 1708650（落点解析到第 4 格，实得槽 5）、1708864（抬高 1.78380 < 0.04121 不成立）、1708880 / 1709014（同 1708650）、1708950（B1）、1708966（B2）；
  1712625 / 1712695（W24 C1）、1714928（W28 C2）。
- 源码：`Editor/B120_W16_Assert.cs:173/237/240/248/252`、`Editor/B120_W24_Assert.cs:276`、`Editor/B120_W28_Assert.cs:443`。
- 现场：抬高实测 **1.78380**，而夹具按 **0.038** 设计（注释 `B120_W16_Assert.cs:223`）⇒ `Make(lineY:0.30, cardNormY:0.2962)` 的两个量**已经不在同一个空间**
  （`home` 取 `card.transform.position`，`drop` 取 `board.SlotPosition(4)`）。槽 4→5 同理（最近格口径变了，见 #5）。
- 建议：**先修 #3 的 `DropTargetWorld` 口径**，再重算这三个夹具的坐标；或者夹具改用 `LayoutSpace.ToNormalized` 直接在归一化域里摆（两边同一个尺子）。

### 12 · 层号迁移遗留（4 条 · α）
- 失败行：log 687816（技能拍特效父层 10，期望 8）、736137（`场上的卡都在 ArenaLayer（不符 2 张）`）、744693（攻方特效锚点父层 10）、867176（3D 棋盘那档实得 10）。
- 源码：`BattleScene.cs:13378 / 20997 / 24210 / 35140`。
- 判据：2026-11-02 `B120-W19/W27` 已把**场上的卡**迁到 `BoardCardLayer(10)`（`ArenaSlots.BoardCardLayer`），`ArenaLayer(8)` 只留竞技场地面/背景。
  ⇒ 这四条断的「卡/挂在卡锚点下的特效仍在 8」是**旧口径**。
- 建议：**改断言**：736137 `badLayer` 判 10；35140 判 `BoardCardLayer`；13378/24210 判「父层 == 卡所在层（10）」。
  ⚠️ 顺手核一下特效在 10 上是否仍在 `boardCam.cullingMask` 里（`BattleScene.cs:47169` 的断言正是这句话，见 #13）。

### 13 · 战场相机 cullingMask（1 条 · 待定）
- `BattleScene.cs:47169`：`bcam.cullingMask == ((1<<ArenaLayer) | (1<<ArenaSlots.BoardCardLayer))`。日志**没打实得值**，无法定类。
- 建议：把实得掩码写进断言文案（一行改动），下次跑就能判。

### 14 · 命中矩形 / `HitSlot`（7 条 · α）
- 失败行：log 898566 / 898579（视觉中心与 0.45/0.35 半高判回自己）、898631（x 边界 ±0.985，实得 ±0.9350）、898644（y ±1.51，实得 1.4600/-1.5900）、
  898670（B2b① 逐点同判）、898709（B2b② z 序）、898722（B2b③ collider 放大 1.2×）。
- 源码：`BattleScene.cs:39185/39189/39247/39251/39281/39337/39363`。
- 分类：**已知判据变更族**（brief 列的 `B120-Q04e` 射线 miss 不回落投影、`B120-Q04f/g` `hits[0]` 无 z 序）⇒ α。
  `898722` 的「放大后仍 False/False」**同时**受 #7（无挡板）影响，修完 #7 再看。
- 建议：按 `hits[0]` 口径重写期望值；命中矩形那三条的期望值要回源 `2DCard` 的 `BoxCollider2D`（1.97×3.02 @0）**并说明现在量的是哪条路**（3D collider vs `Contains`）。

### 15 · 源级字面 / 计数断言过期（13 条 · α，**回报最高的一组**）
| 失败行 | 断言 | 现读实现 | 改法 |
|---|---|---|---|
| 660337 / 660350 / 660363 | `BattleScene.cs:1861` 找 `PlayHitFrameShake(tgtView);` | `BattleDriver.cs:22538` 已是 `PlayHitFrameShake(tgtView, atkView);` | 1861 行字面量补 `, atkView`（一行修 3 条） |
| 660875 | `BattleScene.cs:2232-2233` 要求**全文**不含 `if (recoilBeat) CardFeel.ShakeCameraForHit(` | 该串现在只出现在 `BattleDriver.cs:25467` 的**注释**里（那句注释在解释「已经删掉了」） | 负判据前先剥注释（或只看 `if (recoilBeat) v != null` 那行） |
| 658591 | `BattleScene.cs:1311` 断 `DurationOf(eDeployHand) vs DurationOf(EvtKind.Deploy)` 差 >0.5 | `DurationOf(EvtKind.Deploy)` 现在也走手牌档（同一份映射） | 灭自证改成 `DurationOf(Unknown) 1.3 vs 手牌 0.4` |
| 1709846（B120W17 B6b） | `B120_W17_Assert.cs:232` 要求 `DrainOne` 含 `Play(p.clip` | `BattleSfx.cs:377` `DrainOne()` 的实现写法已变 | 回源重写（**已知会红**，brief 已列） |
| 1710677（B120W21 D5） | `B120_W21_Assert.cs:456` 断 `SideCount(` 恰好 **9** 次 | 现在是 **11** 次（多了新调用点） | 计数断言改成「≥ 9 且每处解析都带数」，别钉死数字 |
| 1035183/1035199/1035215/1035231（B99B1 ⑦1-3） | `B99B1_Assert.cs:353-362` 在 `Release` 里找三处 `DropAccepted` 的次序 | `CardInteraction.Release`（`:1380` 起）结构已改（B120-W16/W24） | 回源重算三处的下标 |
| 1716167 / 1716183（B118W1 E2/E3） | `B118W1_Assert.cs:365` 找 `_fatigueHoldEvent = e;`；`:368` 要求该行含 `t += beatGap` | 现读 `BattleDriver.cs:30961 = h;`（找的是同批那条 `Hit`）、`:19700 t += EventTiming.DurationOf(e)` | 按 2026-11-02 `B118_W11` 的新口径（`EvtKind.Fatigue`）重写这两条 |
| 662161 / 662902 | `BattleScene.cs:2686` 期望 `CardFeel.DealDuration/Foe` = 0.30/0.15；`662902` 期望 0.3 | `EventTiming.cs:759` = `DrawDealOf(player)`，而 `:1216` 明写「抽卡动作 = `timeToPositionCard` 0.2 **+** `TimeToDrawCard`」⇒ 实得 **0.50/0.35** | 期望值改成 `0.2 + …`（两条一起改） |
| 700435 | `BattleScene.cs:16410` 拖空松手 ⇒ 提示条 `Battle/Tips/DragToTarget` | 现场 `log:700419`：那一拖被判「**拖得太高且打不出去 ⇒ 收回手牌**」（原版这一支**不播** cantdo，注释 `:700419` 自己写着） | 夹具改拖到「低处空白」（不触发放弃闸）；或断言按放弃闸口径重写 |
| 1708499（B120W13 A5） | `B120_W13_Assert.cs:124` 要求整句 `… out raySlot); return raySlot; }` | `BattleDriver.cs:33320` 现在是 `TryHitSlotByBoardRay(views, world, out raySlot, out _)` | 期望串补 `out _`；更稳的写法是「含 early-return + 不含 `bestDist`」 |

### 16 · 选中态判据（2 条 · α）
- 失败行：675817（`SelectSoundPlays 0→0 · SelectSoundMisses 0→0`）、1075375（B117G1008 B4「已选那格仍算受体」）。
- 源码：`BattleScene.cs:9413-9424`、`B117_G10_08_Assert.cs:182`；被断实现：`Battle/AttackSelector.cs:1583`
  —— **2026-11-02 `B120-W3`（账 `B-12`）：`opt.Kind == _selected ⇒ 不可选`**。
- 现场：夹具用 `selectedType: 4`（= Ability）建圈（`BattleScene.cs:9389`）再悬停 Ability ⇒ `OnHoverEnter` 在 `:1571` 之前就被 `Selectable` 挡掉 ⇒ `Hovered` 不是 Ability ⇒ 不播音。
- 建议：夹具改 `selectedType` 为近战（或先 `ClearSelection`）。

### 17 · 音源 `OnValidate` 前提（1 条 · δ）
- `B120_W11_Assert.cs:182`：`AddComponent` 之后 `f2.Source == null` 不成立——**编辑器里 `AddComponent` 会立即跑 `OnValidate`**（字段自动 `GetComponent`）。
- 建议：先把 `audioSource` 显式置 null 再 `SendMessage("OnValidate")`。

### 18 · 卡面/动画/演出/杂项（28 条 · 混合）
| 失败行 | 条 | 类 | 证据 / 建议 |
|---|---|---|---|
| 745472 / 745500（W11 端到端震屏） | 2 | **β** | 实得 Δ=(0, 0.018982, 0) ⇒ 只乘了 `Vector3.up`，横向恒 0；断言期望横向 9.5690。查 `CardFeel.ShakeCameraForAttack` 的位移向量 |
| 680506（2D 卡面淡回来） | 1 | **β** | `BattleScene.cs:10759` 期望 0.35 s 时 `ArtAlpha≈0.30`，实得 **1.00** ⇒ 倒放的 [0.25,0.5833] 段没生效（与 1725892/1725928 同一族） |
| 1725892 / 1725928（B118W14 C6/C6c） | 2 | **β**（brief 已列「已知会红」） | `B118W14_Assert.cs:314/319`；`CardFeel.cs:1948/2193` 有 `SetAlpha2D(1f)`，但 `ReturnToHand` 的 `OnKill` 那条没覆盖 ⇒ 打断后永久偏淡；C6c 同因（2D 层没收） |
| 753737（疲劳横幅 0 条） | 1 | β待定 | `BattleDriver.cs:30943` 现在只认 `EvtKind.Fatigue`（2026-11-02 `B118_W11`）；要么引擎没发这条，要么夹具牌库非空（`:30952` 会出声，去日志搜那句话即可定性） |
| 867293（技能动画深度缩放） | 1 | 待定 | `BattleScene.cs:35269-35274`：`DepthScaleWrites==1` 但 localScale=1.0000（期望 1.9444）。查 `Card3DAnimationController.ApplyDepthScaleTick` 的曲线来源（探针 clip 深度是否恒定） |
| 688140（W63 落地 ⇒ 起播） | 1 | 待定 | 0.33 s 时状态仍是 `Flying`、`Enabled=False`（期望 `Landed`）。查 `FlyingCardController.Update` 的 0.15 s 落地补间是否被 `anim2.Update` 的顺序卡住；**先补一条打印状态迁移的日志** |
| 760810 / 760823（T16 灭自证） | 2 | 待定 | 「同样位移但没出卡 ⇒ 锁存仍为 1」与「卡内松手仍是轻点」双双红 ⇒ 点击/拖拽判别在新提交口径下变了；**可能是 β**（点卡不再开展示窗），建议真机点一次 |
| 759999（换牌逐张错开） | 1 | 待定 | 0.05 s 时第 0 张已离开 6.535、第 1 张 0.2890（期望仍压在牌堆）⇒ 错开间隔可能被改小 |
| 699625（B-25 回弹） | 1 | 待定 | 松手 60 帧后 `ScrollPx = -0.13`（期望 ≈0）⇒ 弹性回弹没走完/回弹补间被断 |
| 1718389（B118W2 B2） | 1 | δ/α | 断「缺陷必须复现」得 0 颗 ⇒ 夹具判别力没了（那条缺陷已被别处修掉），改成断言正命题 |
| 1000772（B118W23 N2） | 1 | 待定 | `UIstate` 期望 `normalBattle(1)`；夹具自称「普通档」，需打印实得值 |
| 1009112 / 1009835（B120_W2） | 2 | α/δ | `1009112` 断源码切片含 `skipSequence: TutorialSkipsNormalEnd()`（写法变了）；`1009835` 是夹具前提「棋盘上有带主动能力的单位」 |
| 1724726（B121D2 B4） | 1 | α/待定 | cue `Space Wolves Relase Jingle` 的 `group` 期望 `scene/event`；先确认这条 cue 的现读 group 值 |
| 1720201（B121_WB A6） | 1 | 待定 | 两候选正中 x=0.5000 ⇒ 应留更早下标的槽 3，实得 5 ⇒ 平手规则或 `NxOf` 间距 |
| 906881 / 906894（autoPlay 夹具） | 2 | δ | 「我方场上有一个单位」前提不成立（`BattleScene.cs:42832/42895`）⇒ 两个 `Check(false,…)` 是夹具自检，先修摆盘 |
| 884445（教学跳过闸） | 1 | 待定 | `BattleScene.cs:37128` `!driver.Ctx.IsOver` 红 ⇒ `minTimeBeforeSkip` 没挡住语义那半；可能是 β（跳过直接判死） |
| 742231（手感常量登记） | 1 | α | `ShakeDecayInSlope` / `ReshuffleToDeckSeconds` 没登记出处 ⇒ 补注释/账（纯文档） |
| 1711907（B120W19 C③） | 1 | α | 断言自带 `[待迁移]` 前缀 = 调度台还没改完；`CardFeel.IsArenaCard` 需改成比 `ArenaSlots.BoardCardLayer` |
| 686209 / 686494（技能面板灯） | 2 | α | `BattleScene.cs:12455` 期望 `CurrentLight == Available`，实得 `None`；现读判据已换成 `CanUseActiveAbility`（`SkillPanel.cs:1695-1697, 2401` + `BattleDriver.cs:15757` 的 `canUse: _canUse`，2026-11-02 `B120-W3`）⇒ 期望值要按 `RuleCore.CanStartActiveAbility` 重算 |
| 1721201 / 1721799 / 1722102（B121_Q4B A7/C2/D2） | 3 | 待定 | `B121_Q4B_Assert.cs:233/322/360`：A7 队列 `2998`（期望 `QHandMoving` 3001）、C2 `lights/returns` 计数不符、**D2「撤销后仍 `OnDeployed`」(实得 1) 最像真缺陷**——撤销没把补间序列 Kill 干净 |

---

## 三 疑似已修、待复跑确认

123 会话修掉的四类问题，**本日志里都没有对应红**（即修复已经生效在代码里，只是 BattleScene 还没复跑过）：

| 123 会话修的 | 本日志表现 | 结论 |
|---|---|---|
| `AbilityHealRowDto` / `AbilityManaRowDto` 缺 `[Serializable]` | 无 `MissingMethodException` / `SerializationException` | ✅ 疑已修 |
| `B121_D1B` / `D5` 夹具 trigger 传 0（17 条恒红） | 日志里 `B121D1B` 段无失败行 | ✅ 疑已修（该组未出现在失败清单） |
| `B118W14_Assert` 无宿主（已接 `BattleScene.cs:47035`） | 该文件**确实在跑**（有 ✓/✗ 行，如 1725892），说明宿主接线生效 | ✅ 接线已生效；剩下的 2 条是**断言本身的语义红**，不是接线问题 |
| `Shop` / `RuleEngineTest_EngineE1` 卡池 1129→1142 | 与 BattleScene 无关 | 不在本报告范围 |
| `B123C4`（新接 9 类之一） | `log:1726857 === 合计：✓ 15 · ✗ 0 ===` | ✅ 绿 |

**另有 1 条「计入 134 但没打 ✗ 行」的失败**：本报告查不到它对应哪条断言（流式扫确认：133 条 `✗` 行 + 所有「合计」行都核对过）。
建议复跑时把宿主 `Check` 之外的失败计数口（`BattleScene.cs:659/660` 之外的 `Debug.LogError`）也打一行。

---

## 四 修复优先级（先修哪三组）

1. **#3 + #2（`DropTargetWorld` 局部/世界不统一，14 条）** —— 一条实现改动同时回收「单位拖上场 7 条」与「部署飞行闸 7 条」。
   判据：`BoardLayout.cs:543-557` 的 doc 与 `:477-480` 自相矛盾；`BattleScene.cs:16321 / 41418` 都把它当世界点用。
2. **#5（错侧落点，5 条 · 确信 β）** —— 用户可见的静默错误（敌方目标卡丢自己半场会打错目标且无提示）。
   判据：`BoardLayout.cs:524-540`（`B121-WB` 删容差）+ `CardInteraction.cs:1301-1322`（用解析成功当「在哪块棋盘」）。
3. **#15（源级字面断言，17 条 · 一行级修复）** —— 尤其 `BattleScene.cs:1861` 一处改字修 3 条、`:2232` 剥注释修 1 条、`:1311` 修 1 条、`:2686` 修 2 条；
   这组**零产品风险**、改动最小、回报最高。

紧随其后：**#7（3D 挡板，11 条 · 只改夹具一行 `faction`）**、**#4（提交后未结算 8 条）**。

---

## 九 顺手发现

1. **`BattleScene.cs` 259 条红里有 47 条是「夹具前提」类**（`（前提）…`、`（夹具）…`），而不是产品判断错 —— 建议以后每批先跑一次「前提红」统计，
   前提红一律先修夹具，避免把级联当成实现缺陷（本批 #1/#6/#8/#9/#11 全是这种）。
2. **`BoardLayout.DropTargetWorld` 的 doc（`:543-546`）与 `SlotPosition`（`:477-480`）的空间约定自相矛盾**，且 `:554` 用 `InverseTransformPoint`。
   即便当前 `board.transform` 恰好是单位矩阵，这也是**定时炸弹**（一旦 board 有父缩放就全错）。建议补一条三方锚断言（`DropTargetWorld` ↔ `SlotPosition` ↔ 屏幕点）。
3. **`B121-WB` 删容差（`BoardLayout.cs:524-529`）的影响面比它自己写的大**：注释说「只服务 `ExplainDrop`/`DropRejectKindForTest`/`CardBaseDemo`」，
   但 `CardInteraction.DropReject` 的**侧别判定**恰恰依赖它 —— 建议在那段注释里就地补一句（铁律 5：错的描述就地改）。
4. **`_fatigueHoldEvent` 的断言仍按 2026-11-02 之前的 `EvtKind.Hit` 口径写**（`B118W1_Assert.cs:365/368`），而实现已改判 `EvtKind.Fatigue`
   （`BattleDriver.cs:30940-30943`）—— 同一份「状态 + 细节」混写的老问题，建议这类源级断言统一改成**读常量/读枚举**而不是读字符串。
5. **`B120_W13 A5`（log:1708499）的期望串少了 `out _`**（`BattleDriver.cs:33320` 现在是 `out raySlot, out _`）——
   这类「整句源码形状」断言最容易漂，建议改成「必须含 early-return + 必须不含 `bestDist`」两条语义判据。
6. 日志里 `653380 / 660467 / …` 一串 `[CardView] 未行动绿光材质 …` 属正常启动日志，**不是**失败噪音，别误读。
