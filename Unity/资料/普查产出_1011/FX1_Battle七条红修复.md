# FX1 · Battle 七条红修复 + A321（写手 · 2026-10-11）

> **结论一句话**：`Editor/BattleScene.cs` 那 9 条 A191/A201 断言里，**7 条红的根因全部按 D5 的 R1–R5 修掉了**
> （判据一行没换：**全是断言自己的 bug**，实现/资产侧一个字没动）；另做完 **A321** 那句注释订正。
> 类型检查 **0/0**（原文见 §六）。⛔ 未跑 Unity / 未动 git / 未越白名单（只改了 `Editor/BattleScene.cs` 一个文件）。

---

## 一、结论（7 条逐条：改了什么 / 新期望值与出处）

> 「新期望值」列的数**全部现算**（分母取自旁挂、比对取自 prefab）—— ⛔ 一个都没写死，理由见 §四·2。
> ⚠️ **旁挂在我做活期间被 FX2（A343）换过版**（`_groups.json` 两件，落盘 16:47）⇒ 下表给**两个口径**。
> 我用的尺子 = **独立解 `Resources/ArenaPrefabs/*.prefab` 的 YAML 重建父树**（Python，只读）+ 与建场日志逐数对账。

| # | 断言（现读行号） | 改了什么 | 新期望值（判据出处） |
|---|---|---|---|
| **#1** | `:1548` `★ A191：分组节点真的当父节点` | ① 吃 **R1**（`findPath` 修对）；② 判据从「`Turret 1 barrel` **和** `…/Lance Fire (5)` 都必须解析得到」改成「**前三条必须全中**」（`clipFirst3 == 3`），再打印 5 条里命中几条 | 今天 **3/5**（前三条 ✓、`Lance Fire (5)`×2 被闸门挡掉）⇒ **绿**；prefab 重建后 **5/5** ⇒ 仍绿。出处：`ArenaBuilder.cs:1997-2016` 第一道闸门 + D4 §二 族 1 + 我解 prefab 实测 3/5 |
| **#2** | `:1553` `★ A191：darkangels 的旁挂结构逐条对上` | **R2**（比父路径）+ **R3**（`adds[]` 走生产解析器）+ **R4**（三档拆开）；判据从 `dkMove == targets.Length(61)` 改成 `dkBad == 0 && dkNode == nodes.Length && dkAnim >= 2` | `dkBad = 0` · `dkAnim = 2`（prefab 里 `!u!111` 恰 2 块：分组节点 + `Directional Light`）；`dkNode` 随旁挂：**旧旁挂 2/2 ⇒ 绿**；**新旁挂 12 ⇒ 2/12 ⇒ 红**（prefab 未重建，见 §四·1，重建后 12/12 ⇒ 绿） |
| **#3** | `:1618` `★ A191：tauviorla 那两个「清单外的」` | 只吃 **R1**（判据/文案未动，只补了一句「**必须**用 `findPath` 查，因为验的就是那条父链」） | 两件**都在** ⇒ **绿**（我解 prefab 逐字核过 `Railgun Turret 1 Target `（含尾随空格）与 `…/Cylinder.001/Railgun turret`；D5 §二 #3 同结论） |
| **#4** | `:1624` `★ A191：tauviorla 的旁挂结构逐条对上` | 同 #2 的三档；判据从 `tvMove == 60` 改成 `tvBad == 0 && tvNode == nodes.Length && tvAnim >= 2` | `tvBad = 0` · `tvAnim = 2`（两个炮塔节点带 `Animation`）；`tvNode`：**旧旁挂 8/8 ⇒ 绿**；**新旁挂 36 ⇒ 8/36 ⇒ 红**（同上，重建后 36/36） |
| **#5** | `:1672` `★ A201：组件建出来了、myAnimation 非空` | **R5**：宿主查找**改用生产那份解析器**（`SceneResolver.GoOf(it2.targets[0])`），删掉自写的 `findPath(…, "…baked")` | `bl != null && myAnimation != null` ⇒ **绿**（`Create` 由 D5 证过「宿主一好必成立」；宿主 `Battle Arena Dark Angels baked` 在 prefab 里**同名只有 1 件** ⇒ `FindNearest` 必命中） |
| **#6** | `:1699` `★ A201：GUID → LoadAsset → AddClip` | **R5**：**文案**改对 —— 前置不满足时如实写「**没跑到**」，不再输出空串 `0/2：`；并补注「这是取 clip 那一跳的探针，不等于两条 clip 会播」 | `2/2` ⇒ **绿 —— 但有一个未验环**：编辑器**非播放态**下 `Animation.AddClip` 是否被接受（D5 §五·1）。**若这条是唯一红者 ⇒ 见 §四·3，我按红线没自行降级** |
| **#7** | `:1730` `★ A201：filterCode 配对` | **R5**：文案不再写死 `Directional Light` / `LightAnimationOrbital`，改成**现读旁挂**（另一颗的 `ownerLeaf`+`filterCode`）+ **现读 SO 侧全部 `filterCode`** 再判配不配得上 | `bl.filterCode == "VoidCombatAnimations"` ⇒ **绿**（`env_blendables.json` 里那颗 `floats.filterCode` 就是它；另一颗 `LightAnimationOrbital` vs SO `LightAnimationOrbit` ⇒ **一个都配不上**，照抄原版） |

**三档计数器的语义（R4 的落地，写在代码注释里）**：
`move` = 找得到 + 父路径逐条对上 · `bad` = **必须 0**（父路径**在树里存在却不是它** ⇒ 我们摆错了；外加「该带 `Animation` 的没带」）·
`notBuilt` = 树里没有（自身对象缺 **或** 它要求的父节点缺 ⇒ 闸门挡掉 / prefab 没重建），**逐条点名但不当缺陷**；
另有 `missNode` = 旁挂要的**节点**在 prefab 里没有（专门用来报「旁挂与 prefab 不同步」）。

**两个口径下的实测（我解 prefab + 旁挂算出来的，与建场日志逐数吻合）**：

| 口径 | darkangels | tau |
|---|---|---|
| **旧旁挂**（D5 分析的那版：nodes 2 / targets 61；8 / 60） | 节点 2/2 · move **51** · bad **0** · notBuilt **10** | 节点 8/8 · move **12** · bad **0** · notBuilt **48**（28 自身缺 + 20 父缺） |
| **新旁挂**（FX2/A343 版：nodes 12 / targets 51；36 / 32）+ **旧 prefab** | 节点 **2/12** · move 51 · bad **0** · notBuilt **0** | 节点 **8/36** · move 12 · bad **0** · notBuilt **20** |
| **新旁挂 + prefab 重建后**（FX2 §四·1 预测「没对上 0」） | 节点 12/12 · move 51 · bad 0 · notBuilt 0 ⇒ **绿** | 节点 36/36 · move **32** · bad 0 · notBuilt 0 ⇒ **绿** |

> 旧口径那两行的 **51 / 12 / 10 / 48** 与建场日志 `改挂 51 · 没对上 10`｜`改挂 12 · 没对上 48` **逐数吻合**（D5 §二 同结论）——
> 这是「我的重算 == 生产那一步」的**交叉验证**，也是 R2/R3/R4 改对了的最硬证据（改前重算能给 51/12 却报「对不上」）。

---

## 二、A321 订正（`Editor/BattleScene.cs:214-227`）

- **原文**（已删）：「批处理下 `AddComponent` **不会**触发 `Awake`（那是 Play 模式的事）」—— **把局部推广成一般**。
- **改后**：分**两档**写，并**留更正痕迹**：① **不跑**那一档 = 组件建在**未激活的父链**下（A260 那条：`WindowsManager.Instance`
  只在 `Awake` 里赋 ⇒ 自检里恒 null）；② **会跑**那一档 = 宿主**激活**时（日常页那批 TMP 的字渲出来了，而
  `MeshRenderer`/`MeshFilter`/`m_mesh` 只在它的 `Awake()` 里建）。出处 = `资料/普查产出_1011/W3_子2.md` §二·4·③ / §五·4。
- 🔴 **我顺手核出一条比 A321 原文更细的事实，如实写进注释当「未查清」**（⛔ 没有替它下结论）：
  **②那一档实测的宿主是 TMP（带 `[ExecuteAlways]`）**，而本程这三个宿主
  （`AttackSelector`/`TargetReticle`/`SkillPanel` —— `BuildScene` 里那三处 `AddComponent`，`:9640-9653`）
  建在**激活的** `sceneRoot` 下、**三个类都没有 `[ExecuteAlways]`**（`grep` 三件源码，都是裸 `MonoBehaviour`）
  ⇒ 「两档的分界是**父链激活**还是**类型带 `[ExecuteAlways]`**」**没定论**；注释里写明「⛔ 别把『激活就会跑』当一般结论」，
  而**照旧显式补 `Build()`**（两种解释下都安全，且这是本段真正需要的结论）。
- ⚠️ 所以 A321 的订正口径我收在「**不是『批处理一律不跑』**」这一条上 —— 这比 W3 简报里那句更强的「激活就会跑」**更保守**，
  因为后者会与上面那条反证打架（若要裁到「激活就会跑」，得先解释 `AttackSelector` 这一族为什么算例外）。

---

## 三、改动清单（⛔ 只此一个文件：`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`）

| 位置（现读行号） | 改了什么 |
|---|---|
| `:214-227` | **A321**：注释订正（见 §二） |
| `:1383-1410` | **R1**：`findPath` 分支写反 → 改成「首段是根名就从第 2 段起，否则**相对根**全程下沉」（语义 = 两种写法都支持） |
| `:1418-1445` | **R2/R3 的公共件**：新增 `parentPathOf`（比**父**路径、父=根回空串）· `normPath`（逐段 `Trim` 后比）· `parentInTree`（把「父在树里却不是它」与「父根本不在树里」分开） |
| `:1460-1531` | **darkangels 三档**：`dkMove/dkBad/dkNotBuilt/dkMissNode` 四个计数器 + 逐条点名；`adds[]` 改走 `findTarget`（**R3**）；改挂比对改 `parentPathOf`（**R2**） |
| `:1521-1552` | **#1**：5 条 clip 路径（补上漏抄的 `Turret 2 barrel/Lance Fire (5)`，**订正 D5 §二 #1 指出的那条注释**）+ 门槛改为「前三条必须全中」 |
| `:1553-1561` | **#2** 汇总：判据换成 `dkBad == 0 && dkNode == nodes.Length && dkAnim >= 2`，文案改「结构逐条对上」并分三档打印 |
| `:1562-1616` | **tauviorla 三档**：与 dk 同一套（`tvMove/tvBad/tvNotBuilt/tvMissNode`） |
| `:1618-1635` | **#3/#4**：#3 只加注释；#4 汇总同 #2 |
| `:1646-1660` | **#5（R5/A201）**：宿主改用 `SceneResolver.GoOf(it2.targets[0])`（删掉自写的路径查找） |
| `:1664-1707` | **#6**：`clipRan` + 文案「**没跑到**」+ 「这是探针不是『会播』」的注 |
| `:1708-1735` | **#7**：两处写死的字符串改成**现读**旁挂 + SO |

- 行尾**未被翻**：`crlf == lf == 10343`（纯 CRLF，与 `HEAD` 同）· `git diff --numstat` = **854 / 18**
  （其中**绝大部分是 W11 那批未提交的新增**；本件落在 `@@ -214 +214,13 @@` 与 `@@ -1335,0 +1348,399 @@` 两个 hunk 里）。

---

## 四、没做 / 判不了的（⛔ 不猜）

1. 🔴 **旁挂与 prefab 现在不同步（不是我的改动引入的）**：FX2/A343 把被闸门挡掉的对象收进了 `nodes[]`
   （dk **2→12** · tau **8→36**，落盘 16:47，我实测过），而 `Resources/ArenaPrefabs/*.prefab` **还是旧树** ⇒
   **#2/#4 现在会红在「节点数」那一档**（`2/12` · `8/36`），文案会直说「旁挂与 prefab 不同步 ⇒ 跑一次
   `ArenaBuilder.BuildArenaPrefabs`」。**这是正确的红**（两个产物确实不一致）；**重建后即绿**（FX2 §四 已排了这条命令）。
   ⚠️ **顺序**：重建 prefab（FX2 那条）→ `BattleScene.BuildAndSaveScene` → 再跑 `BattleScene.Run`。
2. **`notBuilt` / 节点数一个都没写死**：全部现算（分母 = `nodes.Length` / `targets.Length`）。所以 A343 之后再变也不怕 ——
   这是 D5 §四 ④ 点名要的写法。
3. ⚠️ **#6 唯一没验的一环 = 编辑器非播放态下 `Animation.AddClip` 是否被接受**（D5 §五·1；`ScenarioBlendables.cs:958-963`，
   断言随后用 `GetClip(clip.name)` 回读）。本件**不能跑 Unity** ⇒ 判不了。**若改完只有这一条红**，
   按 D5 §五·1 的降级法裁（断言只到 loader 那一跳、`AddClip/GetClip` 挪进 `if (Application.isPlaying)`）——
   **那是要裁的，我没有自行降级**。
4. **没跑自检 / 没跑 Unity**（红线）⇒ 本报告里「绿/红」都是**纸面结论** + 我独立解析 prefab 的实测，不是 Unity 的结论。
5. **`EnvironmentApplier` 那条既有注记我没核**（不在白名单）：`ScenarioBlendables.cs` 的类注释还写着
   「我们 13 件 arena prefab 里一个 `Animation` 组件都没有」的**老话**（同一段里已另有 2026-10-11 的更正）。
   本件**没动**它（白名单只给 `BattleScene.cs`），只在这里报一句。

---

## 五、顺手发现（⛔ 只报不改）

1. 🔴 **`ArenaBuilder.LastGroupNode{Created,Moved,AnimAdded,Missed}` 不能替掉本段的重算**（D5 §六·3 建议过这条路，
   **我没采用**，理由三条，都在代码注释里）：① `BattleScene.Run` **不建场** —— 它只 `Resources.Load` prefab +
   `LoadManifest`（`grep` 全文件，`ApplyGroupNodes` 一次都没调）⇒ 那四个静态量在本轮里是**上一次**（或 0）的值；
   ② 它们描述**内存里那一次建场**，而断言要验的是**存盘的 prefab**（运行时 `ArenaRuntimeLoader` 取的就是它）；
   ③ 它们是「最后一次调用」的全局量，**两场不可能同时拿到**。
   ✅ 但 D5 那条顾虑（同一件事两处实现）**已被我按能改的部分消掉**：目标解析走**生产那份** `SceneResolver`。
2. ⚠️ **`ArenaBuilder.FindBuilt` 的文档与实现不符**：注释写「容差与 `SameObject`（0.05）一致」，实现里**没有任何容差**
   （`d < bd` 全程取最近）。不影响今天的行为（今天不会出现「差一点点」的候选），只影响读者 —— 属 A3xx 类，未立账。
3. ⚠️ **同名多实例会让新断言假红的风险点**：tau 有 **8 条**目标的同名件父不同（`Bullet Particle` ×2 ·
   `Bullet Particle (1)` ×2 · `Rings` ×4）⇒ 若 `pos` 匹配到**另一个实例**，`tvBad` 会假红。今天不会（实测 bad=0），
   且**生产侧同一套判据**（`FindBuilt` 与 `FindNearest` 我逐行比过：都是「`Norm(name)` 相等 + `Transform.position` 最近、无容差」）
   ⇒ 断言选中的实例 = 建场当时搬动的那个实例。**下一个改这段的人要知道这个前提。**
4. ℹ️ **FX2 §四·3 里那条期望文案是旧格式**（`改挂 51/51`）—— 本件把 #2/#4 的文案改成了三档（`改挂 51 条、父路径全对；
   …条树里没有…`）。**数字不变、文案变了**，跑 `BattleScene.Run` 时别按旧串 grep。

---

## 六、跑过的检查（类型检查那两行原文）

```
$ TMPDIR=/tmp/wf_fx1 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（改完所有 `.cs` 之后跑的；全文件行尾 `CRLF` 未被翻 —— `crlf 10343 / lf 10343`。）
