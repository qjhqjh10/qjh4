# WB2 · 场景侧 AnimFX 静默口（A514）

> 执行写手 **WB2** · 2026-10-13 · 「清空 A 表」第四轮
> 白名单内动了 **2 个文件**：`Battle/ScenarioBlendables.cs`（+156/−7）· `Editor/BattleScene.cs`（+279/−2；
> 其中 **+83/−2 是 W-B1（A410/A513/A515）先加的那一批** ⇒ 本件实际新增 **+196 行**）。
> ⛔ 没跑 Unity（没跑任何 `-executeMethod`）· ⛔ 没动 git · ⛔ 没改正本 · 只跑了**秒级类型检查**。
> 行号一律是**改完之后**的现读值。

---

## 一、结论

A514 点名的 **4 处** + 顺手那 **2 处**，**6 处全部改完**，每一处从「静默」变成「出声」（或——数据那一档——出声但**不报警告级**）。

| # | 落点（改后） | 原状 | 现状 |
|---|---|---|---|
| ① | `ScenarioBlendables.cs:2161-2180`（`BuildSceneAnimFx` ① 段） | **无条件 `new GameObject`** | **先按「直接子件 + 同名」查，有就复用 + 出声**；没有才建（新增可观测点 `SceneAnimFxNodesReused`） |
| ② | `:1673`（`MakeAnimFx` 开头） | 静默 `return null` | `LogWarning`（并顺手把 `it == null` 收进这一跳 —— 原来它会**先 NRE**） |
| ③ | `:1719`（`AddComponent` 回 null） | 静默 `return null` | `LogWarning`（⚠️ 这一档**自检触发不了**，无断言咬，见 §五） |
| ④ | `:1783`（循环跑完没有 `animfx` 目标） | 静默 `return null` | `LogWarning`（点名到 `cls` / `owner`） |
| ⑤ | `:2096`（`_sceneStan == null` 那一支） | **静默 + 先写 `_sceneAnimFxRoot`** | `LogWarning` + **不写** `_sceneAnimFxRoot`（见 §三） |
| ⑥ | `:2120`（这一场在旁挂里没有条目） | 静默 `return 0` | `Debug.Log`（**数据不是缺口**，出声但**不报警告级**）；`_sceneAnimFxRoot` 照旧写（理由见 §三） |

**出声走的是既有通道**（⛔ 没新造一套）：`[EnvBlend] …（出声，不静默）` 这个格式 + `Debug.LogWarning` /
`Debug.Log` 两级，与 `MakeAnimFx` 里那两处既有的出声、以及本文件族的汇总日志 `SceneAnimFxMissedWhat` 同源。

**断言**：`Editor/BattleScene.cs:10609-10804` 新加一段（4 个子块，**10 条 `Check`**），落点紧跟 A431 之后。
每条都**分得出两种状态**、都写了 **🧨 改坏法**（§四）。

⚠️ **一句话风险提示（给调度台）**：本件**动了自检宿主 `Editor/BattleScene.cs`**。开工前核过：
W-B1 的最后一次写盘是 **09:56**（报告 09:58 已交），我读文件时 mtime 未再变 ⇒ 那一刻**没有别的写手**在它上面
（A表现核 块3 §④ 也写着「宿主是全局瓶颈，必须串行」）。**波5 里 W-B3 / W-B4 / W-B5 也都要这个宿主** ⇒ 排它们时请注意本段已经占了 `10609-10804`。

---

## 二、逐处改动清单

### ① `BuildSceneAnimFx` 的节点创建 —— 无条件建 → 已存在就复用

| | |
|---|---|
| 文件 | `Unity/MyGame/Assets/CardPresentation/Battle/ScenarioBlendables.cs` |
| 落点 | `:2158-2180`（① 那一段的循环体）+ 新增助手 `FindChildByName`（`:2281-2296`）+ 新可观测点 `SceneAnimFxNodesReused`（`:2046-2053` 声明 / `:2079` 清零） |

**改前**（`:2164`，原样）：
```csharp
                    var go = new GameObject(string.IsNullOrEmpty(n.name) ? "AnimFX" : n.name);
                    go.transform.SetParent(parentTr, false);
```

**改后**（`:2161-2180` 摘要）：
```csharp
                    string nodeName = string.IsNullOrEmpty(n.name) ? "AnimFX" : n.name;
                    var exist = FindChildByName(parentTr, nodeName);      // 只看**直接子件** + 名字
                    if (exist != null)
                    {
                        SceneAnimFxNodesReused++;
                        created[n.path] = exist;                          // 复用 ⇒ 照样进「父路径表」
                        bool posSame = (exist.localPosition - Vec3(n.localPos, Vector3.zero)).sqrMagnitude <= 1e-6f;
                        Debug.LogWarning($"[EnvBlend] …旁挂说它**要建**…但 …底下**已经有一个同名子件** ⇒ **复用，不重建**…"
                                       + (posSame ? "；它现读的 `localPosition` 与旁挂逐值一致"
                                                  : $"；⚠️ …**不一致**（树里 … vs 旁挂 …）—— 这一档**不改写它**…"));
                        continue;
                    }
                    var go = new GameObject(nodeName);
```

**照的是哪一处既有出声**：同一段里 `ResolveAnimFxParent` 的「挂到场根 + 逐条出声」，以及本文件族
「**出声 + 记一笔**」的既定做法（`SceneAnimFxMissedWhat`）。**⛔ 没有改成抛异常**（A表现核 §A514 明写）。

**为什么必须防重复**（判据，写进注释了）：我们这个 `res` 是 `EnvironmentApplier.SceneResolver`，
它是「**名字 + 最近位置**」⇒ 同一个父底下两份同名之后，`GoOf` 命中哪一份**不确定**（静默错）。
触发面有两条：① 两个调用点（`Arena3D` / `Warpforge_<场>`）各调一次 —— 去重是按 `root` 的，抓不到；
② `ArenaBuilder.ApplyGroupNodes` 也照原版建分组节点 / arena prefab 重烘。

**顺手订正了一条会误导的旧记录（铁律 5）**：类注释 `:27-28` 原来写「两个都接会建两遍（**节点是无条件
`new GameObject`**）」—— 那一句现在**不成立**了。已在原地订正：结论**不变**（重复挂的是**组件**），
但原因改成「每个调用点各自 `AddComponent<AnimFXController>()`，而它**没有** `[DisallowMultipleComponent]`」。

**今天不会改变既有数字**（静态核过，**这是决定性的**）：逐个 arena prepfab 比对
`Resources/ArenaPrefabs/{battlearena2,battlearena3,battlearenatauviorla}.prefab` 里
`m_Name` 的**精确匹配** —— 那 8 条 `nodes[]` 名字（`Scenario` / `Battle Arena 2 Particles` / `RocketTrail`
/ `Particle Effects` / `Lightning_Green` / `Lightning_Green (1)` / `Big Gun Effect`）**一个都不在**各自的 prefab 里
⇒ `SceneAnimFxNodesReused` 今天恒 **0**、A431 的 `1/3/5 · 2/4/0 · 2/1/0` 与合计 `5/8/5/0` **一个数都不变**。

### ② `MakeAnimFx` 开头那一跳（`:1673`）

**改前**：`if (it.targets == null || res == null) return null;`
**改后**：同一条件**多收一个 `it == null`**（原来会先 NRE）+ `Debug.LogWarning` 点名是哪一个参数不全。

**照的是哪一处既有出声**：同方法紧接着那两处 —— 「宿主对象解析不到」（`LogWarning` + 两种排查提示）、
「旁挂缺 `preventDestroy`」（`LogWarning` + 重生成提示）。逐字照它们的句式（`[EnvBlend] …（出声，不静默）`）。

⚠️ **顺手的那半句（如实标）**：`it == null` 时原来**先 NRE**（`it.targets` 解引用空对象）—— 抛异常
**不算「出声」**（它会把整条调用链带走），所以一并收进这一跳。生产上 `it` 非 null（两个调用点都先判过），
这一跳今天只被自检的合成输入走到。

### ③ `AddComponent` 回 null（`:1719`）

**改前**：`if (c == null) return null;`
**改后**：`LogWarning` + 两条排查线索（宿主是不是刚被销毁 / 上面是不是已经挂着一颗）。

⚠️ **这一处是本件唯一「没有断言咬」的**（原因见 §五），注释里已**如实写死**这一点，⛔ 别当成漏做。

### ④ 循环跑完、没有 `kind == "animfx"` 的目标（`:1783`）

**改前**：`return null;`（循环外面那一句，静默）
**改后**：`LogWarning`，点名 `{it.cls}`(owner=`{it.owner}`) + 判据（旁挂）+ 「数据 vs 缺口」那句话。

**为什么要点名**：调用方（`BuildSceneAnimFx` 的 ② 段）只会把它记成「`MakeAnimFx` 建不出来（宿主/字段缺）」
—— **原因根本不是宿主缺**，不点名就查不出是旁挂目标种类变了还是抄错了。

### ⑤ 「旁挂没读到」那一支（`:2096`）—— 见 §三

### ⑥ 「这一场在旁挂里没有条目」那一支（`:2120`）

**改后**：`Debug.Log`（**不是** `LogWarning`）+ 写清判据：
「正常 —— 原版 7 个实例只在 `battlearena2` / `battlearena3` / `battlearenatauviorla` 三场
（判据 → `资料/普查产出_1012/H2_场景侧AnimFX.md` §四）。若这一场本该有 ⇒ 旁挂旧了」；
`_sceneAnimFxRoot = arenaRoot` **照旧写**（理由见 §三）。

**照的是哪一处既有先例**：`BuildSoundTrack` 里那句 `if (n <= 0) return new PlaySoundOnTime[0];      // 原版这层本来就是空的（数据，不是缺口）`
—— 本文件族**本来就把「数据」与「缺口」分两级**，我沿用同一把尺（这里是「出声的 `Debug.Log`」，因为
「一声不响」才是 A514 要修的东西）。
> 📌 这一支**不是失败**：13 场里 10 场本来就没有。改成 `LogWarning` 会让那 10 场每次 `Load()` 都报一次
> 「警告」，把真缺口淹掉（本仓对「噪音」的一贯处理：出声、但分级）。

---

## 三、`:2096`「先写 `_sceneAnimFxRoot` 再失败 ⇒ 永久去重」的处理与理由

**改前**（一行）：
```csharp
            if (_sceneStan == null || _sceneStanBuild == null) { _sceneAnimFxRoot = arenaRoot; return 0; }
```

**那一行错在哪（两条，分开说）**：

1. **静默**：`LoadSceneStandalone()` 里那两句 `LogError` 只在**第一次读**的时候打一遍（`:1989` 的
   `_sceneStanTried` 闩住）⇒ 从第二次起，这一支**一声不响**地回 0。
2. **先写后失败**：写了 `_sceneAnimFxRoot = arenaRoot` ⇒ 同一个战场实例**再进来**会撞上 `:2015` 那段
   **按 root 去重**，打出一条「**这个战场实例已经建过** ⇒ 不重复建」—— 而事实是**一条都没建**。
   这是**谎报**，而且它**会一直那么说下去**（去重是按 root 的，那个 root 只要还活着就一直命中）。

**「该不该回滚那个赋值」—— 我的判断：该，而且更准确的说法是「这一位本来就不该写」**（不是「回滚」：
这一支在**同一次调用内**写入并立即返回，除了这个字段**没有任何中间状态**要撤，所以「不写」就是全部）。

判据是**这一位的语义**：`_sceneAnimFxRoot` 的注释写着「上一轮是给哪个战场实例**建的**」（`static Transform
_sceneAnimFxRoot; // 上一轮是给哪个战场实例建的（同一个实例不重复建）`）—— **没建就不能认领**。

**⚠️ 与 ⑥ 那一支的处置不同，而且是有意的**（这是本件的关键判断，判据 = 「数据到底读到没」）：

| | ⑤ `:2096`（`_sceneStan == null`） | ⑥ `:2120`（这一场没有条目） |
|---|---|---|
| 发生了什么 | 旁挂**没读到**（文件取不到 / 缺节） | 旁挂**读到了**，这一场就是 0 条 |
| 写 `_sceneAnimFxRoot` 吗 | **不写** | **照旧写** |
| 为什么 | 「没读到」是**环境/资产**状态，**以后可能变**（重生成旁挂 / 重进编辑器 / 重跑批处理）⇒ 认领了就等于**永久放弃** | 数据这一趟是真读到了；同一进程内数据不会再变 ⇒ 重试**答案一样**，认领**不造成任何损失**（而它换来的是「同一个实例不必再跑一遍 0 条的空转」） |

**⚠️ 如实标一条（写进代码注释了）**：**同一个进程内**重试仍然**不可能成功** —— `LoadSceneStandalone()`
开头那句 `if (_sceneStanTried) return;` 也闩着，第一次读失败后**不会再读第二次**。
⇒ 本句改掉的是「**谎报已建**」，**不是**「当场能自愈」。真要恢复得有下面三条之一：
重进一次编辑器（域重载）/ 重跑一次批处理 / 重生成旁挂后刷新。
（那条 `_sceneStanTried` 闩本身**不在 A514 的点名范围**，我**没动**，按铁律 13·4⑧ 记进 §六。）

**断言怎么咬住它**（这是本件最硬的一条）：同一实例**连调两次**，两次都必须说「一条都没建」，
且日志里**不许**出现「已经建过」——
· 改后：`missB == 2`、`claimB == false`；
· 把 `_sceneAnimFxRoot = arenaRoot;` 加回去（= 改前的写法）：第二次撞去重支 ⇒ `missB == 1`、`claimB == true` ⇒ **同一条 `Check` 红**。

---

## 四、断言清单（断什么 · 怎么分辨两种状态 · 改坏法 · 落点）

**落点**：`Editor/BattleScene.cs:10609-10804`（`BattleScene.Run` 里、A431 那一段**之后**、整轮收尾**之前**）。
**放在这个位置是必须的** —— 本段会**临时把 `sceneStandalone` 两节按掉再恢复**，放前面会把 A431 的数字带偏；
恢复是否成功，本段自己**回读一次「5 条」自证**（`:10712`）。
⛔ 不碰既有现场：四个子块各用**独立探针根**，收工 `DestroyImmediate`。

| # | 断什么 | **怎么分辨两种状态**（改前 / 改后各读出什么） | 🧨 改坏法 | 行 |
|---|---|---|---|---|
| ①-a | 节点**已存在就复用**、且总共只有一份 | `NodesReused==1` · `NodesCreated==2` · 场根底下叫 `Scenario` 的子件 `==1` · 组件 `==1` · 改挂 `==5` · 没对上 `==0`；<br>**改前**：复用恒 0 / 新建 3 / 同名子件 2 | 删掉 `FindChildByName` 判空（回到无条件建）⇒ **一处三红** | `:10650` |
| ①-b | 复用**不改写**已有节点 | 我们摆的 `(1,2,3)` 调完还在（`sqrMagnitude < 1e-8`） | 把复用支改成「照旁挂覆盖 localPos」⇒ 红 | `:10661` |
| ①-c | 复用那一档**出声** | 日志里找得到「已经有一个同名子件」 | 删掉那条 `LogWarning`（改成静默 `continue`）⇒ 红 | `:10667` |
| ②-a | 旁挂没读到 ⇒ **每次**都出声、**不谎报** | 连调两次：`"**一条都没建**"` 出现 **2** 次、日志里**没有**「已经建过」 | 把 `_sceneAnimFxRoot = arenaRoot;` 加回去 ⇒ `1` 次 + 出现「已经建过」⇒ 红 | `:10706` |
| ②-b | （自证）测试**把状态还回去了** | `SceneStandaloneDataCount() == 5` | `ForceSceneStandaloneMissingForTest(false)` 写错 ⇒ 红（且后面全建不出来） | `:10713` |
| ③ | 「这一场没有条目」⇒ 一条不建**但必须说** | `cC==0 && Built==0 &&` 日志里有「这一场没有条目」 | 把那一支改回静默 `return 0` ⇒ 红 | `:10738` |
| ④-a | 参数不全 ⇒ 出声 | 抓到 `"收到**不全的参数**"` **1** 条 | 删掉那条 `LogWarning` ⇒ 0 ⇒ 红 | `:10769` |
| ④-b | `targets[]` 里没有 animfx ⇒ 出声 | 抓到 `"的目标都没有"` **1** 条 | 同上 ⇒ 红 | `:10776` |
| ④-c | **正例**（⛔ 不是装饰）：真给一条能建的目标 | 组件**真建出来**、且三条「不建」的出声 **0/0/0** | ① 探针壳没真调到 `MakeAnimFx`（恒 null）⇒ 红；② 上面两条是「反正抓不到」的恒真判据 ⇒ 也被这条挡住 | `:10797` |

**「两种状态」是怎么分的（逐条说明，回应铁律「弱断言分不出两种状态」）**：

- ① 靠的是**两个计数的一升一降**（`Reused 0→1` 且 `Created 3→2`）**外加一条结构读数**（同一父底下的同名子件
  `2→1`）—— 三个数必须**同时**对上，任何单独一个都不是「弱断言」能凑出来的。
  （新加 `SceneAnimFxNodesReused` 这个可观测点就是为它：`SceneAnimFxNodesCreated` **单看分不出来** ——
  「树里本来就有」和「我们建了但被别的东西吃掉了」在它上面都是 2。）
- ② 靠的是**同一条日志的条数**（2 vs 1）**加上一条反向判据**（不许出现「已经建过」）—— 后者才是真牙口：
  它钉的是「**认不认领那个实例**」这件事本身，不是文案。
- ③ / ④ 靠的是 `LogType.Warning` / 文本片段的**捕获计数**，走的是本仓现成的「出声」断言范式
  （先例：`Editor/CollectionScene.cs:2111-2136` 的 `hSame`/`hDiff`、`Editor/BattleScene.cs:9548-9570` 的 A410 那一段，
  两处都写着「本仓有现成的「出声」断言范式：`Application.logMessageReceived`」）。
- ④-c 那条正例是**灭两种假绿**用的：① 没调到被测实现（恒 null）；② 上面两条判据恒真。
  ⚠️ 它自己会带出**两条别的**警告（`sounds.count` / `modules.count` 旁挂缺 —— 那是**既有的**出声口，
  不在我的三个片段里，所以不干扰计数）—— 这句已经写在断言文案里了。

**探针（都是 public，只给自检用，⛔ 不另写一套逻辑）**：
· `ScenarioBlendableFactory.MakeAnimFxForTest(item, res)`（`:1809`）= `private MakeAnimFx` 的一层壳；
· `ScenarioBlendableFactory.ForceSceneStandaloneMissingForTest(bool)`（`:2029`）= 把 `_sceneStan` / `_sceneStanBuild`
  置 null 并设 `_sceneStanTried`，**就地复现「读不到」那一档**；`off` 时把 `_sceneStanTried` 置回 false ⇒ 下次**真读**。
  ⚠️ **它不碰生产逻辑**：生产代码不读任何「测试标志」，它改的只是那三个静态缓存本身。

---

## 五、没查清 / 没做的

1. **③ 那一档（`AddComponent` 回 null）没有断言咬** —— 我**合成不出来**那个环境（`AddComponent` 回 null
   要「组件加不上」那一类条件；拿一个已销毁的宿主去 `AddComponent` 是**抛异常**、不是回 null）。
   ⇒ 如实标在代码注释与断言段的头注里，**⛔ 不是「忘了写」**。真见到那条日志时先查两件事：宿主是不是刚被销毁、
   它上面是不是已经挂着一颗 `AnimFXController`。
2. **所有断言都没实跑**（简报口径：⛔ 不许跑 Unity）。我只做到：逐条静态推演期望值 + 类型检查 **0 错误**。
   ⚠️ 其中**最需要实跑确认的一条**是 `:10650` 的 `SceneAnimFxReparented == 5`（复用路径下改挂照做）——
   我是按 A431 现在能过 + 那 5 颗粒子的世界位置不受我的位移影响推的，**推理链写进注释了**，但仍以实跑为准。
3. **没核「ArenaBuilder 建场侧建的组节点与旁挂 `nodes[]` 的重叠面」的全量普查** —— 我只逐场核了**那 3 件
   prefab 的 8 条 `nodes[]` 名字**（结论：一个都不在 ⇒ 今天复用数恒 0）。其余 10 场没有 `nodes[]`，判据上不需要。
4. **没去动 `_sceneStanTried` 那道闩**（它在 A514 点名范围外）—— 只记进 §六 第 2 条。
5. **没动 `EnvironmentApplier.EnsureSceneBlendables` 那条链**（文件头写着「只此一处调用点」，我 grep 复核：
   全仓 `BuildSceneAnimFx` 的调用点 = **生产 1 处**（`Battle/ArenaRuntimeLoader.cs:114`）+ 自检若干）。

---

## 六、顺手发现（⛔ 一条都没顺手改）

1. 🔴 **`AddComponent<AnimFXController>()` 重复挂**这一档**今天没有任何守卫** —— `AnimFXController`
   **没有** `[DisallowMultipleComponent]`（`Battle/AnimFXController.cs:190` 的类声明，上面只有 `///` 注释），
   所以「两个调用点都接」时同一个宿主上会**静默多挂一颗**（两颗各自 `OnEnable`/`Update`/销毁计时）。
   文件头那句「**两个都接会建两遍** ⇒ 别再补第二处」**结论仍然有效**（我已就地订正它的**原因**，见 §二①），
   但「**第二个调用点真被补上时没有任何东西拦它**」这一点值得记一笔：要么补一句守卫（`GetComponent<AnimFXController>() != null ⇒ 出声 + 复用/跳过`），
   要么把这条禁令升级成断言。**本件没做**（不在 A514 点名范围）。
2. 🟡 **`LoadSceneStandalone()` 的 `_sceneStanTried` 闩在失败时也是「永不重试」**（`:1989`）：
   第一次读失败 ⇒ 同一进程内**再也不会**去读 `Resources/EnvBlendables.json`。
   ⇒ 即便本件把 ⑤ 那一支的 `_sceneAnimFxRoot` 认领去掉了，「重试」在进程内仍不会成功（见 §三 最后那段）。
   要真能自愈，得把这一闩改成「**只在成功时置位**」（代价：失败时每次调用都会再读一次 Resources + 再报一次）。
   **本件没改**（不点名 + 会改缓存语义）。
3. 🟡 **A431 的 `wantNodes = {3,4,1}` 与本件新加的存在性检查是耦合的**：哪一天 arena prefab 真长出
   `nodes[]` 里的节点（或旁挂重生成后条目变了），`SceneAnimFxNodesCreated` 会**掉**、A431 的期望值要跟着改
   —— 那是**正确**的信号（说明「旁挂说没有、其实有了」这件事真的发生了），但**届时要一起改**
   `wantNodes` + 断言文案（同 A431 注释里 `Embers` 那条先例：`reparent 5 → 6` 要一起改）。
   我这一段（A514）**不受影响**（它用的是自己摆的节点）。
4. 🟡 **`ArenaBuilder.ApplyGroupNodes` 建的分组节点与 `nodes[]` 的重叠面没有全量普查过**：
   tauviorla 的 `Scenario` / `Railgun BIG (1)` 就是**建场侧**建的（正因为它们**不在** `nodes[]` 里才不冲突）。
   ⇒ 将来谁给 arena3/arena2 也开了 groups 旁挂，就可能撞上；届时**本件的复用支会出声点名**（这正是它存在的意义）。

---

## 七、类型检查结果

命令：`TMPDIR=/tmp/wf_wb2 bash d:/4/Unity/工具/typecheck.sh`（跑了 **两次**）

| 次 | 结果 |
|---|---|
| 第 1 次 | 运行时程序集 **0**；编辑器程序集 **5 个错误 —— 全在 `Editor/RewardsScene.cs`**（`:2303` `CS1593` + `:2386/:2424/:2438/:2466` `CS7036`，全是同一个 `Func<int, List<Transform>>` 参数没传）。**那不是我的文件**（波6 的 W-E3 那一族），且它的 mtime = **10:05**（我那次检查正在它写盘的过程中）⇒ 按简报口径：**重跑 + 如实记**，⛔ 没去动它 |
| 第 2 次（隔 45 秒） | **运行时 0 · 编辑器 0** ✅ |

行尾：`ScenarioBlendables.cs` = **纯 LF**（2472/2472）· `Editor/BattleScene.cs` = **纯 CRLF**（12246/12246）
—— 改前改后都数过，**没翻**。`git diff --numstat`：`+156/−7` 与 `+279/−2`（后者含 W-B1 先加的 `+83/−2`）。
