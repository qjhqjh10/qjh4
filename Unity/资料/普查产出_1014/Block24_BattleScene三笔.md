# Block24 · `Editor/BattleScene.cs` 三笔（A532 / A554 / A606）+ 协调台追加三条（含 A555）

> 写手代理 · 2026-10-14 · **只改了一个文件**：`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`
> （⛔ 未跑 Unity · 未动 git · 未改任何正本 / 别人的文档 —— 本文件是我唯一写的 `.md`）
> ✅ **秒级类型检查**：`TMPDIR=/tmp/wf_b24 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
> （跑过四次：三笔落完 / A554 改成「共用表」+ A531 细口径 / A555 数据驱动 / 最后一处注释收口之后）
> ✅ **行尾**：改前 / 改后都是**纯 CRLF**（`b'\r\n'` 13455 = `b'\n'` 13455；进文件前 = HEAD 那版 13260）
> `git diff --numstat` = **1534 / 1248**（远小于 13455 ⇒ 没翻行尾）
> 📌 下面行号 = **改完之后**的现读行号（会漂，按**内容/符号**定位）

---

## 一、逐条

| 账 | 改了什么（文件:行 / 符号） | 判据 | 做完没有 |
|---|---|---|---|
| **A532** | `BattleScene.cs:10481-10490`：在 `Check(hookedBefore388 >= 16, …)`（`:10491`）**正上方**补一段注释，写明① **现读真值 = 19**（出处逐字：`Unity/_tmp_view/battle.log:39110`「A388：开局后静态钩子挂着 19 条」；与 A515 注释里那句「那 19 槽」一致）② 这里 **16 只是下限、故意不写 19**③ 🔑 **通则**：这类「钩子数」断言**一律别写死数**——先问「这组数谁挂的、有没有哪个挂点**带条件**」，只要有一个带条件（这里 `AttachPostFx` 取不到 `Volume` ⇒ `OnPostFx` 保持 null）就要用**同一次运行的基线**或**下限 + 点名环境无关的那几条**。**断言本身一字未动**（没弱化、也没把 16 改成 19） | `资料/普查产出_1013/批次计划_1013.md` §六·B（A532）；根因段 = 同文件 A515 注释 `:10474-10478`；真值出处 = `battle.log:39110` | ✅ |
| **A554** | **做了两半**：<br>① **共用表**：把 `keys` / `wantBuilt` / `wantNodes` / `wantReused` / `wantReparen` 五张表**从 A431 块里提到块外**（`:11119-11131`，紧挨方法体那一层）并写清为什么；`A514 ①` 的断言改成**读同一张表**（`:11349` `SceneAnimFxNodesReused == wantReused[0]` · `:11351` `builtA == wantBuilt[0]` · `:11352` `Reparented == wantReparen[0]`），报错文案也跟着改成插值（`:11358`）⇒ 耦合从「注释提醒」变成**编译期**的：哪天变了**两段一起红**。<br>② A431 块内只留一行指针（`:11161-11162`），⛔ 块里不再抄第二份 | 账面原文「A431 的 `wantNodes{3,4,1}` 与 A514 新的**存在性检查耦合** ⇒ 哪天真撞上，期望值要一起改（**同 `Embers` 那条先例**）」；先例 = 同段 `:11152-11156` 那条「预告了 `reparent 5 → 6`、结果没发生」⇒ **注释挡不住漂移**；`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」。⚠️ 数字**逐值未变**（3/4/1 · 1/2/2 · 5/0/0），零行为变化 | ✅ |
| **A606** | `BattleScene.cs:11511-11561`：把 `WSmall2_场景重试闩.md` §五那段成品代码**逐字贴进来**（两条 `Check`：闩/去重 + 自愈可观测），落点 = **A514 段之后（`:11509` 那个 `}` 之后）· A463 段（`:11563`）之前**，⛔ 一个字没改它的判别力；块头补了 14 行注释（为什么非要新开关 · 两条改坏法 · 落点两条 · 自证那一条 · 开关必须放 `finally`）+ 出处 | 账面原文 =「A553 的断言**没落地**」+「成品代码在 §五，**可直接粘贴**」；§五「建议落点」= A514 段之后 · 整轮收尾之前 | ✅ |

### A606 落点为什么是「A514 `}` 之后、A463 之前」（而不是整轮最末）
§五建议「排在 A514 之后」的原话理由是**日志更干净**（放前面会让 A514 ② 的 `hB` 窗口多一行「重试成功」）。
本段全程**只读静态缓存 + 日志窗口**（不 `Instantiate`、不碰 `driver`），自带自证（`backE == 5`），
所以排哪儿都过；放这里同时满足账面那两条（`A514 段之后` · `整轮收尾之前`），且 A463 段不依赖 `sceneStandalone`。
**已核**：`ForceSceneStandaloneMissingForTest(false)` 本身**不触发读**（只置两个 null + 清闩）⇒ `dE == 4` 成立；
`SceneStandaloneDataCount()` 会触发一次 `LoadSceneStandalone()`（`Battle/ScenarioBlendables.cs:2149-2153`）；
两条检测串与实现逐字对上（`ScenarioBlendables.cs:2117` 的 `**失败（资源取不到）**` · `:2082` 的 `**重试成功**`）。

---

## 二、协调台中途追加的两条

| # | 做了什么 | 判据 | 做完没有 |
|---|---|---|---|
| **① 必做** | `BattleScene.cs:8604-8611`：`At("EnergyAccumulation_Me", 1785.6f, 555.65f)`（`:8611`）⇒ **`1787.46f, 555.6f`**（+ `:8604-8610` 七行订正注释：原来钉的是**我们自己那个错的 x**、两证、真值 1787.4575、修完实测中心、⛔ 别把实现改回 `1746.7`） | `资料/普查产出_1014/Block12_BattleDriver族.md` §2.3（我方那盏灯照抄了敌方的 x：`RectTransform_2719.json` 我 `-80.2` vs `RectTransform_2939.json` 敌 `-81.3`）+ §2.6（改法逐字） | ✅ |
| **② 可选** | `BattleScene.cs:8638-8669`：补 **8 条细口径断言**（`:8650` `void AtExact(...)` + `:8662-8669` 八次调用，阈值 `1e-4` 世界单位 ≈ 0.0108 px，形状照 A513 那条）——`TitleBackground_Me/Foe` · `AvatarItemSmall_Me/Foe` · `OffensiveButton` · `EnergyAccumulation_Foe/Me` · `OvertimeIndicator`（另两件 `ChatButton` / `CenterCameraButton` 上一轮已各自收口，**没重复**） | 期望值表 = `Block12_BattleDriver族.md` **§2.5**（原版直读 · 父链走完；换算式与 `HudAbs` 逐字同一条：中心 = `x + w/2` / `y + h/2`，y 从上） | ✅ |

**② 我自己又独立验了一遍（静态，float32 复算）**：把 §2.5 给的中心值代进**我写的那个式子**
（`cx/1920f` · `(1080f-cy)/1080f` → `ToWorld`）与 `HudAbs` 的式子（`(x + w*0.5f)/1920f` · `1f - (y + h*0.5f)/1080f`）逐件比
⇒ **8 件全部逐位相同（世界坐标差 = 0，阈值 1e-4）** ⇒ 这 8 条不该出现边界抖动。
另外**顺手核了 §2.6 那句「其余 9 条粗口径仍绿」**（`Block12` 说最大 |Δ| = 0.047 px）：
我按同法算出 10 条粗口径的最大 |Δ| = **0.0474 px**（`TitleBackground_Foe` 的 y）、其余 ≤ 0.0302 px
⇒ **与它的说法一致**，10 条全在 1.5 px 容差内。（`HudExtraPosPx` 那趟往返另加 ~1e-4 px，量级可忽略。）

---

## 二·2、协调台第三条追加（**必做** · 与 A554 同一处）—— A555 的「期望值由旁挂驱动」

| 做了什么 | 判据 | 做完没有 |
|---|---|---|
| **① 新增读数口**（类作用域，`:56-96`）：`[System.Serializable] class EnvBuildProbeFile { … }` + `static int[] EnvBuildNodeCounts(string[] keys, out int found)` —— **只读** `Resources/EnvBlendables.json` 的 `sceneStandaloneBuild[]`，按 `root` 找节、取 `nodes.Length`（取法照生产那条 `Battle/ScenarioBlendables.cs:2304-2305`，⛔ 不按下标）。**为什么另起 DTO**：同文件那份 `SceneStandaloneFile` 是 **internal**（跨程序集看不见）且 `_sceneStanBuild` 是 private static ⇒ 照 `ScenarioBlendables.cs:1975-1978` 的先例只声明自己那节、**复用它公开的 `AnimFxBuildGroup`**（不另造类型）<br>**② 期望值改成数据驱动**（`:11124-11132`）：`wantNodes = EnvBuildNodeCounts(keys, out found)` · `wantReused = (int[])wantNodes.Clone()`（= 全部走复用）⇒ **`3/4/1` 与 `8` 都不再写死**<br>**③ 新增前提断言**（`:11136-11138`）：三场都要能按 `root` 找到一节（`found == 3`）—— 少了它，读不到会静默回 `0/0/0`，「没读到」会被伪装成「旁挂本来就是空的」<br>**④ 合计那条改写**（`:11269-11280`）：`totalReused == 8` ⇒ **`== totalWant`**（现读合计）<br>**⑤ 🆕 专给「`nodes[] == 0` 那一档」的断言**（`:11282-11295`）：`components == 5 && nodes + reused == totalWant` —— 那一档的坑是「把空 `nodes[]` 当成整节没数据 ⇒ 连组件也不建」 | 协调台第三次追加 + `RO_战场与窗口判据三件.md` **§一·②「还缺什么」第 1 条** + §一·③「写手件 B」；⚠️ 那条普查已证实 `EnvBlendables.json`（10-05 22:31）比它的两份输入都旧（`_groups.json` 10-06 10:14 · 生成器 10-06 13:37）⇒ 重跑后 3 场 `nodes[]` 归零 | ✅ |

**⛔ 没有弱化（逐条对照）**：①「**新建必须 == 0**」两侧都保留（`nodesA == 0` / `nodes == 0` / 合计 `totalNodes == 0`）②「新建 + 复用 == 旁挂条数」的和式保留、只是**条数**改从旁挂现读 ③ 每场/合计的 `reparent`、`missed`、组件数一个没动。
**改的是「期望值的来源」，不是阈值，也不是删条款**；今天实测数据仍是 `3 / 4 / 1` ⇒ **读数与改前逐值相同，零行为变化**。
**另加了一条「读不到就红」的前提断言**（③）—— 这是把期望值改成现读之后**必须补**的，否则「读了没读到」与「数据本来就是 0」分不出来。

---

## 三、顺手发现（⛔ 只报不改）

1. 🔴 **A514 ① 那条「复用不改写」断言仍有一格挡不住**（B1 报告 §二·4 已如实标，我这里**独立复核了一遍**，确认成立）：
   判据换成「调用前后 prefab 自带那颗的 `localPosition` 逐位不变」之后，因为**树里的值与旁挂值本来就一致**，
   若哪天实现把旁挂值**写回**那颗节点，这条**照样绿**。要挡回来得造出「树里 ≠ 旁挂」——
   而那正是 B1 刚删掉的旧夹具（会造出生产路径不会出现的「两份同名」）⇒ **两边都不该动**，如实挂在这里。
2. ℹ️ `Editor/BattleScene.cs:12010` 那句 `Debug.Log(P + "--- A463 段结束（下面还有别的段）---")` 已经过期
   （下面没有别的段了；A462 早搬走、紧跟的就是外层 `}`）—— 纯日志文案，**没改**（B1 报告 §二·3 也点过）。
3. ℹ️ `At(…)` 那个局部函数（`:8586`）的容差是 **1.5 px**、`AtExact` 是 **1e-4 世界单位 ≈ 0.011 px**——
   两档**并存**是 A423/A513 定下的口径（粗口径挡「整体跑偏」、细口径挡「取整」，细的挡不住就白写），
   我按原样加了 8 件，**没有动粗口径那 10 行**。
4. ℹ️ **同一处的 `wantReparen{5,0,0}` / `wantBuilt{1,2,2}` 仍是写死的**（= A555 同一族风险，但**这次普查没点名**）：
   它们分别来自 `sceneStandaloneBuild[].reparent[]` 与 `sceneStandalone[].items`，
   生成器那条守卫（`if paths[i] in groups_paths: continue`）**只对 `nodes[]` 生效** ⇒ 现读 `reparent = 5 / 0 / 0` 稳定。
   ⇒ **我没动**（⛔ 不越账；要动的话改法与 `wantNodes` 完全同形）。**留给调度台裁**：
   若 A418② 那条腿跑完发现这两格也漂了，照 `EnvBuildNodeCounts` 的写法加两个读数口即可。
5. ℹ️ A555 的**写手件 A / C 不在这批**（`RO_战场与窗口判据三件.md` §一·③ 分的三件）：A 订正
   `ArenaBuilder.cs:3201-3203` 的「只有 2 场有旁挂」（现读 13/13）；C 是「把 `<场>_mirror.json` 的 `go`
   算进 `built_paths`」+ 重生成 13 份 `_groups.json` + prefab 重烘 ⇒ **那件要先要调度台裁一句口径**
   （`reflection camera` 那颗要不要留空壳）。两件都不在 `Editor/BattleScene.cs` ⇒ 我一件没碰。
6. ℹ️ A532 那条注释里引的 `battle.log:39110` 是**上一次实跑**的产物（`_tmp_view/battle.log`）——
   它是「19」这个数的**唯一出处**（我没法跑 Unity 复核）；若下次实跑后那个行号漂了，按内容搜「开局后静态钩子挂着」即可。

---

## 四、没做完的（+ 为什么）

1. 🔴 **A514 ②-a 的 `missB == 2` 那个口径要不要改 —— 留给调度台裁**（`WSmall2_场景重试闩.md` §六·2 明写「留给调度台」）：
   该报告**特意没有**去重 `BuildSceneAnimFx` 那条「一条都没建」（实例级、每次调用都报），
   而 A514 ②-a **明写** `missB == 2`（「每次都出声」）。若调度台认为那一处也该去重，
   **A514 ②-a 的 `missB` 要从 2 改成 1** —— 那是**动 A514 段的判据**，不在本件的账里 ⇒ **我没动**。
2. ⚠️ **所有断言没实跑**（简报口径：⛔ 不许跑 Unity）。我做到的是：**逐条静态推演 + float32 复算**
   （见 §二）+ **两条秒级类型检查**；**仍以同步点那次 `BattleScene.Run` 为准**。
3. ℹ️ A554 我只把**同一份事实**收成一份（`wantNodes` / `wantReused` / `wantBuilt` / `wantReparen`），
   `NodesCreated == 0` 与 `dupA == 1` 仍**两处各写一遍字面量**（A431 的 `nodesA == 0` / A514 的 `Created == 0`）——
   它们是**规则**（「都烘在 prefab 里 ⇒ 新建 0」）不是**逐场数字**，收进表要再动 A431 的两处 `== 0`，
   与本账点名的 `wantNodes{3,4,1}` 不是一件事 ⇒ **没扩面**，如实记在这里备调度台判。

---

## 五、改了什么（一句话清单）

- `Editor/BattleScene.cs:10481-10490` —— A532 注释（16 是下限 · 真值 19 · 通则「别写死数」）
- `Editor/BattleScene.cs:8604-8611` —— `EnergyAccumulation_Me` 的粗口径期望 1785.6 → **1787.46**（y 555.65 → 555.6）+ 订正注释
- `Editor/BattleScene.cs:8638-8669` —— A531 的 8 条细口径断言（`AtExact`）
- `Editor/BattleScene.cs:56-96` —— 🆕 A555：`EnvBuildProbeFile` DTO + `EnvBuildNodeCounts()`（只读旁挂 `sceneStandaloneBuild[].nodes.Length`）
- `Editor/BattleScene.cs:11110-11138` —— A554（共用一份期望值表）+ A555（`wantNodes`/`wantReused` 改成数据驱动 + 新增「三节都找到」前提断言）
- `Editor/BattleScene.cs:11269-11295` —— A555：合计那条改 `totalReused == totalWant` + 🆕「`nodes[] == 0` 那一档也必须绿」的专门断言
- `Editor/BattleScene.cs:11347-11360`（A514 ① 那两行）—— A554：A514 ① 改读同一张表
- `Editor/BattleScene.cs:11511-11561` —— A606：A553 断言落进宿主（§五成品逐字）
