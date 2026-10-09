# AR1 · 修 `A345-T-b` 的【假目标】—— 走 (B) **C# 侧显式跳过** + 补 13 场逐场的断言

> 执行代理 AR1 · 2026-10-19 · 白名单 = `Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` + `Assets/CardPresentation/Editor/BattleScene.cs`
> 口径 = 调度台已裁的 **(B)**（诊断 → `资料/普查产出_第八会话/S1_诊断A345Tb根因.md`，本件不重查、只落地）
> ⛔ 本件**没跑 Unity**（13 场 prefab 的重建腿由主对话在收口那趟跑）；本件跑的是**秒级类型检查**（读数见 §四）。

---

## ① 改前 / 改后 —— `ArenaBuilder` 那一处

### 改前（三处口径矛盾的现状）

`ApplyGroupNodes` 对旁挂 `targets[]` / `adds[]` 的每一条都走 `FindBuilt`（同名 + 最近世界位置），
**找不到就 `LastGroupNodeMissed++`**。而旁挂里恒有一条 `target BoardCamera`（`manifest.camera` 塞进
`built_paths` 的产物），`BuildContent` **从不建相机** ⇒ **13/13 场逐场恒定 1 条没对上**
（日志原件 13 行见 `S1_诊断A345Tb根因.md` §①、本件复核过 `_tmp_view/arenaprefabs_收口.log` 13 行逐字同形）。

### 改后（4 处，全部在 `ArenaBuilder.cs`）

| # | 位置（改后行号） | 内容 |
|---|---|---|
| 1 | `:2716-2723` | 新增计数口 **`public static int LastGroupNodeCameraSkipped;`** + 一段头注（为什么单开一个口、而不是静默从 `Missed` 里扣）。同时把 `Missed` 那段 A591 旧头注**就地更正**（「没有任何断言在读它们」今天已不成立）。 |
| 2 | `:2729` | `ApplyGroupNodes` 顶部重置里加 `LastGroupNodeCameraSkipped = 0;`。 |
| 3 | `:2772`（`targets[]` 循环）+ `:2805`（`adds[]` 循环） | 在 `FindBuilt` **之前**插一行：`if (IsCameraTarget(mf, t)) { LastGroupNodeCameraSkipped++; continue; }`。两个循环**共用同一份判据**（两处各写一份 = 迟早不一致）。 |
| 4 | `:2833-2851` | 新增判据函数 **`public static bool IsCameraTarget(Manifest mf, GroupTarget t)`**（`public` ⇒ 断言侧能复用；全仓 0 个 `.asmdef`，编辑器侧直接可见）。 |

配套的**注释更正**（铁律 5，都是「原来写 X、实际是 Y」那类）：

- `:2690-2694` `ApplyGroupNodes` 顶部那段「**不许静默**」——补一句「**唯一一处例外**」（相机）。
- `:2870-2878` / `:2889-2898` `GroupNodeMissHelp()` 的**头注 + 函数体**：A591 那版写「13/13 场逐场都是这条
  ⇒【既有的、预期的】、**按预期红处置**」——**今天这一格已修掉**，那段语义**反过来**了：
  **真看到这条尾巴 = 有一件不在判据里的东西对不上** ⇒ 先比 `(名字, pos)`，对不上就是**新缺陷**。
- `:2874-2878` A591 那段末尾补一条：**「上游闸门没建」这个猜测对【相机那一格】现在可以写成「已推翻」**
  （相机不走 `meshes[]`/`particles[]`，那四道闸门作用不到它；对别的对象仍然「未证实」）。

### 判据：为什么身份 = **名字 + pos**（**不是**名字 + 父路径）

- ✅ `mf.camera.name` / `mf.camera.pos` 都在 **`<场>_manifest.json`** 里，与旁挂 `<场>_groups.json` 是**两份文件**
  ⇒ 拿它当判据**不是**「从被检的那份旁挂里把期望读回来」。
- ⛔ **父路径 `BattlePrefab/BattleBoardElements` manifest 里没有** —— `CameraData` 只有
  `name/pos/rot/fov/near/far/lensShiftY/sensorSizeX/sensorSizeY`（`:92-101` 现读）。
  ⇒ 那一格**不在生产侧编一个路径出来充数**，改由**断言侧**拿旁挂现值单独钉住（见 §③ 第 2 条）。
- ⚠️ **任一侧缺 `pos` ⇒ 不认**（照旧算 miss）：宁可红一条假的，也不吞掉一条真的。

### 现读复核（本件自己跑的，不是抄 S1）

| 复核项 | 结果 |
|---|---|
| 13 份 `<场>_manifest.json` 的 `camera.name` / `camera.pos` | **13/13** = `BoardCamera` / `[100.0, 2.222075, -13.57198]` |
| 13 份 `<场>_groups.json` 里 `Norm(name)=="BoardCamera"` 的条目数 | **13/13 恰好 1**（`targets[]` 里；`adds[]` 13 场全空） |
| 同上按 **pos** 命中的条目数 | **13/13 恰好 1**（⇒ 名字与 pos **两样都唯一**，不会误伤同名条目） |
| 那一条的 `parent` | **13/13** = `BattlePrefab/BattleBoardElements`（且该路径在 `nodes[]` 里也有 ⇒ 父节点我们**确实建**） |
| 13 份 `<场>_groups.json` 的 `targets.Length` vs 构建日志的「改挂」 | **逐场 = 改挂 + 1**（62=61+1 · 63=62+1 · 84=83+1 · 91=90+1 · 102=101+1 · 87=86+1 · 94=93+1 · 83=82+1 · 89=88+1 · 130=129+1 · 82=81+1 · 135=134+1 · 35… 见 `arenaprefabs_收口.log` 13 行） |
| 13 份 `Resources/ArenaPrefabs/*.prefab` 的 `--- !u!20 &`（Camera classID）计数 | **13/13 = 0**（`m_Name: BoardCamera` 也 13/13 = 0） |

⇒ 除相机那一条，**全场再没有第二条没对上**；且「相机不在 prefab 里」这条**今天就是事实**。

---

## ② 改前 / 改后 —— 断言那一段

**宿主** = `Assets/CardPresentation/Editor/BattleScene.cs` → 方法 **`BattleScene.Run()`**（`:281` 起），
插在 A191 那节（`:1539-2073`）**之后**、A137 那节（`:2263` 起，原来在 `:2075`）**之前**，作为**独立一节**：
**`:2075-2261`**（新加 **197** 行）。

**改前的现状（为什么必须新加）**：

- `LastGroupNodeMissed` 全仓 `.cs` **除 `ArenaBuilder.cs` 外零命中** ⇒ **没有任何断言在读它**
  （本件 `grep` 复核过；与 A591 落痕一致）。
- 现成最近的那条（`BattleScene.cs:1733`，darkangels 三档 `dkMove`/`dkBad`/`dkNotBuilt`）
  **只报不断**：它把 `BoardCamera` 归进 `dkNotBuilt`，而那档**没有 `== 0` 的要求**
  ⇒ 相机今天就静默躺在里面。实据 = `_tmp_view/battle.log:4762`：
  「改挂 83 条、父路径全对；**1 条树里没有**：**目标 `BoardCamera`（自身不在树里）**」。
- ⚠️ 那条**一个字都没动**（本件只**新增**一节，`dkInst`/`tvInst` 那段与它的小工具全部照旧）。

**改后**：`BattleScene.Run()` 里多了一节 —— 把生产那条链**逐场真的跑一遍**，两条腿：

| 腿 | 做什么 | 在哪份实例上做 |
|---|---|---|
| **① 判据侧独立核** | 实例化 `<场>.prefab`，拿**生产那份解析器**（`CardPresentation.EnvironmentApplier.SceneResolver`，与 `ArenaBuilder.FindBuilt` 是同一套「`Norm(名字)` + 最近世界位置」，⛔ 不另写第二套）逐条找 `targets[]` / `adds[]` | **干净实例**（跑腿②**之前**） |
| **② 读生产计数口** | `ArenaBuilder.ApplyGroupNodes(amf, aInst)` **真的调一次**，现读 `LastGroupNodeMissed` / `LastGroupNodeCameraSkipped` / `LastGroupNodeMissedWhat` | 同一份实例（跑腿①**之后**） |

⚠️ **次序不能反** —— `ApplyGroupNodes` 的 ① 段**会新建同名节点**（它不查节点是否已在树里），
在实例上再跑一次会给腿①造出 **263 颗多余的「同名节点」**（13 场合计）⇒ 那就是**假的「找得到」**。
⇒ 腿①在干净实例上做、腿②再跑；每场跑完 `finally { DestroyImmediate(aInst); }` 立刻收干净
（批处理下 `Destroy` 不生效，用 `DestroyImmediate` —— `CLAUDE.md` §三）。
**本段不写任何资产**（只读旁挂 + 只写一次性实例）。

**`public` 口的用法**：`ArenaBuilder.AllArenas`（`:37`）· `LoadManifest`（`:3344`）· `LoadGroups`（`:3403`）·
`ApplyGroupNodes` · `IsCameraTarget` · 四个计数口 —— **全在 `ArenaBuilder` 上，本件没为断言改过任何一个访问修饰符**
（`IsCameraTarget` 与 `LastGroupNodeCameraSkipped` 本来就是新加的，直接 `public`）。

---

## ③ 每条断言：断什么 / 期望值怎么来的 / 改坏法

新加的 `Check` 共 **1 + 13×2 = 27 条**（1 条前置 + 每场 2 条）。逐条如下。

### 0. 前置（`:2152`）—— 「13 个战场都在册」

- **断什么**：`ArenaBuilder.AllArenas.Length == 13`。
- **期望值怎么来的**：判据 A345-T-b 自己写的就是「**没对上 0 条 × 13**」—— 那个「13」是判据的一部分
  （原版 13 个战场），**这是全仓那个「13」的唯一出处**，⛔ 别在别处再抄一份。
- **改坏法**：从 `AllArenas` 里删掉一场 ⇒ 红（那时「×13」这条判据就被换了口径而没人知道）。

### 1. 每场 ①（`:2223-2236`）—— **未解析 0 条**（含 🧨 灭自证）

```csharp
Check(unresolved == 0 && bMissed == 0 && camInPrefab == 0, ...)
```

- **断什么**（三个条件**写在同一条**上）：
  - `unresolved == 0` —— 腿①（判据侧独立核）：除相机那一条外，旁挂里每一条 target 都在 prefab 里**找得到**；
  - `bMissed == 0` —— 腿②（生产口）：`ArenaBuilder.LastGroupNodeMissed == 0`；
  - `camInPrefab == 0` —— 🧨 **灭自证**：该场 prefab 实例里 `GetComponentsInChildren<Camera>(true).Length == 0`。
- **期望值怎么来的**：**判据 = `资料/普查产出_1013/WA345Ta_战场全树生成侧.md` §八 的 A345-T-b**
  （⛔ 不是我们自己的常量、也不是从被检对象里读回来的）。
  **分母现算**：`asc.nodes.Length + tArr.Length + dArr.Length`（逐场不同：6…96 / 62…135 / 0），
  **⛔ 一个数都没写死**（写死的那一刻它就不再是判据了）。
- **为什么三个条件必须挤在同一条**：「把相机**建进** prefab」与「把相机**从旁挂拿掉**」这两条**结果一样、
  性质相反**的路，**都会让未解析数变 0** ⇒ 只压未解析数的话前者被静默放过。钉上 `camInPrefab == 0`
  之后，**两条路都不可能同时满足**（依据 = `ArenaSceneState.cs:44`「战斗场景那台 `BoardCamera` 是场景自己的」
  + `ArenaRuntimeLoader.cs:127` 的 `cam` 是**外面传进来**的 ⇒ prefab 只带相机光学**数据**）。
- **🧨 改坏法**：
  1. 把 `ArenaBuilder.BuildContent` 里那句 `ApplyGroupNodes(mf, root)`（`ArenaBuilder.cs:2642`）注释掉
     ⇒ 腿② `bMissed` 暴涨 ⇒ 红；
  2. 把旁挂里某一条 `nodes[]` 删掉再重打 prefab ⇒ 腿① `unresolved` 当场点名 ⇒ 红；
  3. **把相机建进 prefab**（例如在 `BuildContent` 里加一台）⇒ `camInPrefab` 变 1 ⇒ **同一条**红；
  4. 把 `IsCameraTarget` 改成恒 `true` ⇒ 腿① 把全部 target 都跳过 ⇒ 见第 2 条的 `predAgree` 与
     `bCamSkip` 双红。

### 2. 每场 ②（`:2252-2265`）—— **相机那一格单独钉住**（五个条件钉在一起）

```csharp
Check(camEntry == 1 && camParentOk && bCamSkip == 1 && camInPrefab == 0 && predAgree == nEnt, ...)
```

| 条件 | 断什么 | 期望值怎么来的 |
|---|---|---|
| `camEntry == 1` | 旁挂里**今天仍然**写着这一条（且**恰好一条**） | **独立字面量判据**（内联 lambda `isCamEntry`，`:2142`）：`Norm(名字) == "BoardCamera"` **且** pos ≈ `(100.0, 2.222075, -13.57198)`（`1e-3`）。名字与 pos **都写死**，来源 = **原版场景包** `bundle_scenes_scenes_battlearena1/GameObject/BoardCamera.json`（转手两跳旁挂）。**⛔ 不经过 `ArenaBuilder`** —— 这正是它「独立」的地方 |
| `camParentOk` | 跳过的那一条**确系** `BattlePrefab/BattleBoardElements` 下的那一条 | 字面量 `const string CamParentWant`（来源同上一格）；比较**按段 Trim**（与 `findPath` 的逐段 `Norm` 同语义，旁挂里真有带尾随空格的名字） |
| `bCamSkip == 1` | 生产口是「**认出来 + 显式跳过**」 | 生产计数口 `LastGroupNodeCameraSkipped`（本件新加的）。⛔ 不是「旁挂里那条没了、所以无事发生」—— 那种情形这个数是 **0** |
| `camInPrefab == 0` | ⛔ **没有**把相机**建进** prefab | §27 设计（见上条） |
| `predAgree == nEnt` | 生产那份 `ArenaBuilder.IsCameraTarget` 与本件的**独立**字面量判据**逐条一致** | 两份判据**各写各的**（一份读 `manifest`，一份读原版包字面量）⇒ 生产那份被改宽/改窄（例如「同名就跳」）**当场红** |

- **为什么这五条是「结构上不可能同时满足」的**：`camEntry == 1` 与 `bCamSkip == 1` 把「旁挂里那条还在」
  和「生产口认出来了」**同时**压住；`camInPrefab == 0` 堵住「建进 prefab」；`predAgree == nEnt` 堵住
  「把判据改成无所不跳」。**四条歪路各被一格钉死** ⇒ 这就是本条的灭自证。
- **🧨 改坏法**：① 把 `gen_arena_groups.py` 的 `camera` 那三行删掉 → `camEntry` 0 且 `bCamSkip` 0 ⇒ 红；
  ② 把 `IsCameraTarget` 改成 `return t.name.Contains("Camera")` → `predAgree < nEnt` 或 `bCamSkip > 1` ⇒ 红；
  ③ 把 `IsCameraTarget` 改成恒 `false` → `bCamSkip == 0` ⇒ 红（**这条正是「改回原样也逃不掉」的那一格**）；
  ④ prefab 里加一台相机 → `camInPrefab == 1` ⇒ 红。

### 关于「反向守卫」（调度台第 4 条要求）

生产口 `LastGroupNodeMissed` **必须为 0**，因此 `MissedWhat` 今天必然是**空名单** —— 也就是说
「名单里出现一条 ≠ 相机」**在绿的状态下不可能发生**。所以这一格写成 **① 的失败文案里的反向判据**
（照 `ArenaBuilder.GroupNodeMissHelp()` 里 W9 那条的形状写的）：

> 「⚠️ 逐条比 `(名字, pos)`：**对不上** `BoardCamera` / `(100.000,2.222,-13.572)` 的那几条 = **新缺陷**，
> ⛔ 别按「既有的、预期的」放过 —— A591 那次就是这么误导的。」

即：**一旦有东西没对上，断言直接红并把名单原样打出来**，同时告诉复核者「只有那一条是假目标、别的一律按新缺陷查」。
（没有把它写成「解析 `MissedWhat` 的字符串」—— 那会把断言绑在**文案**上，文案一改断言就假绿。）

### 与三条系统性毛病的对照（自查）

| 毛病 | 本件怎么防 |
|---|---|
| 断言自证/同义反复 | 期望值来自**旁挂 + 原版包字面量**，不来自我们的实现；相机那一条更有**两份独立判据**互相钉（`predAgree`） |
| 弱断言分不出两种状态 | 「认出相机跳过」（`bCamSkip == 1`）与「旁挂里那条没了」（`bCamSkip == 0`）**是两个不同的数**，不会都绿 |
| 「靠『什么都没发生』的假断言」 | 关键一条：**不能只看 `bMissed == 0`** —— 三条件（+ 五条件）挤在一起，正是为了堵这一点 |

---

## ④ 类型检查读数 + `git diff --numstat` + 行尾

### 秒级类型检查（`TMPDIR=/tmp/wf_ar1 bash d:/4/Unity/工具/typecheck.sh`）

| 第几次 | 触发 | 运行时程序集 | 编辑器程序集 |
|---|---|---|---|
| 1 | `ArenaBuilder.cs` 改完 | **0** | **0** |
| 2 | `BattleScene.cs` 加完断言那种 | **0** | **0** |
| 3 | 把相机计数折进 ① 那条之后 | **1**（`Core/CardbackFace.cs(71,34): CS0117 CardbackTable 未包含 TrySpriteRect 的定义`） | **0** |
| 4 | 同上 + 60 秒后重跑 | **0** | **0** |
| 5 | `ArenaBuilder.cs` 最后一段注释更正后 | **0** | **0** |

🔴 **第 3 次那条错是【别人在写的文件】**（`Assets/CardPresentation/Core/CardbackFace.cs` —— **不在本件白名单、
本件一个字没碰**）。按简报口径：错**全集中**在那个文件上 ⇒ 不是本件的问题；隔一分钟重跑（第 4 次）
它就没了 ⇒ 是那个写手的**半成品**被编了进去（`CLAUDE.md` §13·3 那条实测现象）。
**本件没有去改那个文件。**

### `git diff --numstat`（逐文件，现读）

```
398	15	Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs
 95	19	Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs
```

- `BattleScene.cs`：本波之前那 15 行删除是**别的写手**留的（会话开始时该文件已是 `201/15`）；
  本件净加成 **197 行**（`201/15` → `398/15`），**全是新增一节**，**没有删过任何既有内容**。
- `ArenaBuilder.cs`：本件改动 **+95/−19**（4 处实现/接口 + 6 处注释更正，逐段见 §①）。
- 本件**只动了这两个文件**（`git status --short` 复核过：` M` 就这两条里属于本件的白名单命中）。

### 行尾（二进制读，`b.count(b'\r\n')` vs `b.count(b'\n')`）

| 文件 | CRLF | 裸 LF |
|---|---|---|
| `ArenaBuilder.cs` | 4789 | **0** |
| `BattleScene.cs` | 20317 | **0** |

⇒ 两个文件**都保持纯 CRLF**（改完当场量的，没翻行尾；⛔ 全程没用过 `sed -i`）。

---

## ⑤ 没查清的部分 + 需要哪几条自检覆盖

### 没查清 / 没做到的（如实写，⛔ 不拿猜测填空）

1. 🔴 **没跑 Unity**（简报明令）⇒ 下面两件是**推的**，**必须由收口那趟实测**：
   - **腿②在「已建好的 prefab 实例」上跑 `ApplyGroupNodes`，它的 `bMissed` 到底是不是 0**。
     本件的依据是「构建时在**新根**上跑 = 逐场恰好 1 条 = 相机」（13 行日志现读）+「相机现在被跳过」，
     但**没有实测过「在实例上再跑一遍」这条路径**（`ApplyGroupNodes` 的 ① 段会**再新建一遍同名节点**，
     `FindBuilt` 因此在「有重复同名节点」的树上匹配）。
     ⚠️ 本件判过：重复节点的世界位置与原件**逐字相同**，`FindBuilt` 取的是 `d < bd` 的**最前者**
     ⇒ 匹配结果不变、`bMissed` 只会**不变或变小**；**且腿①是在干净实例上做的**，所以即便这条推测错了，
     腿① 仍会红。**但这是推理，不是实测。**
   - **腿① `unresolved == 0`**：证据 = 构建日志「`targets.Length = 改挂 + 1` ×13」+「`FindNearest` 与
     `FindBuilt` 同一套判据（前者还更宽松：唯一匹配时连 pos 都不比）」⇒ 推得为 0。**同样没实跑。**
2. ⚠️ **13 场 prefab 的 `bCamSkip` 全是 1** —— 推的（旁挂里 13/13 恰好 1 条相机）。没实跑。
3. ⚠️ **本件没核**「另 11 场（除 darkangels / tauviorla 外）的 `SceneResolver` 与 `FindBuilt` 逐条一致」——
   现有证据只有那两场（`battle.log:4762 / 4786`，各 1 条 = 相机）。这是腿①**唯一**的残余风险面：
   若某场生产解析器与构建期匹配器不一致，腿① 会**红一条真的**（那时是**真发现**，不是断言坏）。
4. ⚠️ **13 场 prefab 里各有几颗 `Animation`** —— 断言只在文案里报 `bAnim`，**没有压**（既有那条 darkangels
   断言压的是另一棵树）。本件**没查**这个数在 13 场上对不对。
5. ⚠️ 本件**没有**碰 `资源/数据/…` 与 `工具/gen_arena_groups.py`（口径裁定：(B)，⛔ 别翻案）。
   因此旁挂里那条假目标**还在**（这是**故意的** —— 断言靠它当 `camEntry == 1` 的分母）。

### 需要哪几条自检覆盖

- 🔴 **`BattleScene.Run`** —— **必须**（断言的宿主；13 场逐场那 27 条 Check 只在这一条里跑）。
- 🔴 **`RuleEngineTest.Run` / `DeckScene.Run` / 其余菜单族** —— **不需要**（本件没碰引擎、没碰 Shell/菜单；
  改动面 = 一个 Editor 构建器 + 一个 Editor 断言宿主）。
- 🔴 **`ArenaBuilder.BuildArenaPrefabs`（13 场重建腿）** —— **不需要**重跑才能让本条绿
  （本件**没改** prefab 的产出：跳过只改**计数**、不改树形；`camSkip` 那一步 `continue` 之前
  `ApplyGroupNodes` 对相机**本来也什么都没做**）。但要**看那 13 行日志的新措辞**：
  修好后每场的 `A191 分组节点（<场>）` 那行应当是「…**没对上 0 条**」（不再带 `target BoardCamera`）。
  ⚠️ **这是本件最便宜的一条独立验收**（跑一次 `BuildArenaPrefabs`，grep 那 13 行）。
- ⛔ **不许拿「编辑器程序集 0 错」当「断言会绿」** —— 类型检查只管编不编得过。
- ⚠️ 跑 `BattleScene.Run` 时**开 `WF_PSFIXSEED=1`**（本项目已定口径）。
