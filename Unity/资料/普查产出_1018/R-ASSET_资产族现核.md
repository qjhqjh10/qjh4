# R-ASSET · 资产族 + 表下单行 + 无编号 · 现核与判据检索

> 只读现核代理 · 2026-10-08。**没跑 Unity · 没动 git · 没改任何别的文件。**
> 用了 UnityPy（`D:/2/Warpforge_tools/py312/python.exe`）读**原版真 bundle 的 externals 表** —— 这是本次唯一"新工具"，
> 它把「哪个 pid 属于哪个包」从推测变成了**可复查的事实**。
> ⛔ 凡说「本地没有」的都列了搜过的目录 / 关键词 / 包（§4 / §7）。

---

## 1. 一句话结论

| 类别 | 条数 | 是哪几笔 |
|---|---|---|
| ✅ **能现在就派**（判据齐、只差照着改） | **3** | `A965`(参考) · **`BattleDriver.cs:5857-5861` 过期注释** · **`GOF50 Tide of Muscle`**（+ 同族 3 张，见 §5.4） |
| 🟡 **判据这轮查实、但落地要动 Unity**（= 主对话的腿，**不能派子代理**） | **3** | **`A845(a)`**（要跑导出器）· **`A845(b)`**（判据仍未定，但**这一轮给了可裁的判据候选**，见 §3.2）· **F3 两条悬案**（**已由 2026-10-07 的 `MainMenuScene.Run` 实跑回答掉一半**，见 §3.3） |
| 🔵 **上一轮的账** | **1** | **批 3 附带发现**：**成立、且判据这轮查实了**（原版顺序与我方相反）⇒ **建议并案可派** |
| ⛔ **挂起（且理由成立）** | **1** | **`A897`**：8 个 pid 里 **7 个确实无路**（穷举过）；**第 8 个拿到原版 id `GOF2`，但撞上一个更大的问题**（见 §4 与 §6·①） |

🔴 **本轮最大的一条不在账上**：**原版的卡 id 命名空间 ≠ 我们 `card_ids.json` 的（自建）命名空间** ——
证据见 §6·①。它直接决定 `A897` 能不能"用原版 id 换名字"，**所以先报，⛔ 一个字没改**。

---

## 2. 逐笔表

| 笔 | ①还成不成立 | ②判据（文件:行号 / 资产路径） | ③要改哪些文件 | ④最小改法 | ⑤自检宿主 | ⑥能否现在派 | 置信度 |
|---|---|---|---|---|---|---|---|
| **`A845(a)`** 网格路实跑验 `CopySerialized` | ✅ **成立**（代码已改、**从没实跑过**） | `WarpforgeArena1/Editor/EffectExporter.cs:1936-1941`（源码自己写着「这条路没实跑过」）· 回读口 `MeshImportMethodProbe.cs:56-86` | 无（**只跑 + 回读**，不改码） | `-executeMethod EffectExporter.Run`（全量）⇒ 回读 `Assets/WarpforgeVFX/Meshes/*.asset` 的 `_typelessdata`：NaN/Inf 或 \|值\|>100 = 仍坏 ⇒ 退路 = `DeepCopyMesh` 改 `SetVertexBufferData` 就地重灌（⛔ 别退回 `CreateAsset`）。**跑完必须跟一次 `BoosterPackExporter.Run`**（全量重导换 guid） | **不是那 11 条自检**（导出器是独立入口） | ⛔ **不能派**（子代理不许跑 Unity）⇒ 主对话的腿 | 高 |
| **`A845(b)`** 撞名真复刻 | ✅ **成立**（今天只到「出声不静默」） | 现场 = `WarpforgeVFX/Materials/`（**复算：1399 个 `.mat`、199 个基名带 `X N.mat`**；`Meshes/` 同形 102）· 守卫 `EffectExporter.cs:1397/:1417` · 判据缺口 `W21_EffectExporter确定路径.md` §④·2 | `WarpforgeArena1/Editor/EffectExporter.cs`（`ImportMaterial:1568-1586` · `ImportMesh:1941-1952`） | **候选判据（我们挑的、非原版规格）**：把确定路径从 `{stem}.mat` 改成 `{stem}__{8位内容指纹}.mat`，指纹 = 现成的 `MatFields:1323` / `MeshFields:1353` ⇒ 每个**内容不同**的源各自一份、**与到达顺序无关**（今天那 18 个名字里「先到的那种在工程里没有对应文件」的洞就此补上）。⚠️ **裁剪前要查**：有没有别处**按路径名**引用这些 `.mat`（prefab 是 guid 引用、不受影响） | 同上（导出器入口） | 🟡 **前半（改码）可派**；**但「真复刻」的验收 = 跑一次导出器**（Unity），所以**排在 (a) 同一趟** | 中（判据是**我们挑的**，不是原版有这条） |
| **`A897`** 8 个 pid | 🟡 **半成立**：7 个**确实**拿不到；第 8 个拿到**原版 id**、但**不能换成名字** | 见 §4 逐 pid 表 | 无（**挂起**，或**并进 §6·① 那笔新账**） | —— | —— | ⛔ 挂起（**理由已复核成立**，见 §4） | 高 |
| **`A965`**（参考） | ✅ 仍在（与 `A851` 同族） | `Editor/CardFaceProbe.cs:245 / :250` · 判据 `S2_A851槽号.md` | `Editor/CardFaceProbe.cs` | 照 `Editor/ShellScene.cs:250` 改按 `chr[i].materialReferenceIndex` 取槽 | 卡面探针族（`CardBaseDemo.Run`） | ✅ 可派（**本轮未重复查**，按简报） | 高（引账） |
| **F3 行第一条**：四条字栏差值的 x/y 分解没量 | ❌ **已经量了 —— 这条可以销** | `_tmp_view/menu.log`（2026-10-07 22:35 那次）**:13652 / :13694 / :13736 / :13778** 四条「框在原版矩形的**竖**位上（**y 差 0.00px** ≤ 1.0）」 | 无 | **零代码**，把账销掉（顺带记：那 8.78 / 104.92 / 1.60 / 1.57px **全是水平方向**，正是 `(框宽−字宽)/2`） | 那次就是 `MainMenuScene.Run`（2442 通过 / 0 失败） | ✅ **不需要派**（已答） | 高 |
| **F3 行第二条**：`AlignLeft` 之外是否有第二因未分离 | ✅ **成立（半个）** ⇒ 仍要 Unity | 位置层面**已分离**（上一条）；**「`SetAutoFitBox` 收敛出的字号/字宽与原版是否相同」这一格今天没有任何断言** —— `CheckLeftAlignedAtWorld`（`Editor/MainMenuScene.cs:189-209`）**结构上量不到它**（它比的 x 是"真渲染左缘 vs 框左沿"，而两者都由同一份 `WorldW` 缓存推出来） | `Editor/MainMenuScene.cs`（加一条量**渲染宽度/字号**的断言） | 在那四条旁边补一条「真渲染宽 ≤ 原版框宽」＋「字号 = ?」的断言（写法照本文件 `:4519` 那颗 `Window Title` 的量法） | **`MainMenuScene.Run`（24 秒）** | 🟡 **要跑 Unity 才判**（断言改动本身可派，**验收不能**） | 中-高 |
| **`Battle/BattleDriver.cs:5857-5861` 过期注释** | ✅ **成立**（行号已漂，不是 5417-5421） | 现读 `BattleDriver.cs:5857-5861` 仍写「`OnClosed` **全仓零接线**…`OnPeerLost` 也零接线…原版判据**没查到**」；实况 = `Net/NetBattle.cs:279-280` **早已接上**（`HandlePeerLost` / `HandlePeerClosed`）＋`:308-309` 摘线；判据 `NetBattle.cs:245-262` 逐条列着（`SetOpponentDisconnected.c:13` 等） | `CardPresentation/Battle/BattleDriver.cs`（**只有这一段注释**） | 把那 4 行 `<para>` 整段改写成「**已接**：`Net/NetBattle.cs:279-280`；判据 = `…SetOpponentDisconnected.c:13` ⇒ 弹窗 + 停表 + 30 秒倒计时」；**保留更正痕迹**（铁律 5）。⛔ **别改任何代码** | 纯注释 ⇒ **零条自检**（铁律 12 判据 3 的邻档）；要跑就跟一次 `BattleScene.Run` | ✅ **可派**（一行级） | 高 |
| **`GOF50 Tide of Muscle` 仍没效果** | ✅ **成立**，**但「语义不可恢复」这句是错的** | 卡面 = `d:/2/Warpforge部队卡片/Orks/4计策/Warpforge_50_Tide-of-Muscle.png`：`Draw two troops and give them +1` + **粉拳**（近战图标，与 `Genestealer Familiar` 卡面**同一枚**）；同族 `Get'em ladz!` / `Da Red Waaagh` / `Krumpaklaw` 卡面**也是同一枚粉拳** | `d:/4/Unity/数据/游戏数据/cardface_fixes.json`（**`desc` 列**，脚本 `gen_cards_engine.py:818-820` 会覆盖卡池 desc） | 在 `desc` 列加 `"Tide of Muscle": "Draw two troops and give them +1 Attack"`（**写法照同表 `Genestealer Familiar` 的 `+1 Attack`** —— 同一枚图标、同一个词）；重跑 `工具/gen_cards_engine.py`。`descZh` 已经是「+1 攻击」、**不动** | **`RuleEngineTest.Run`**（引擎/卡数据） | ✅ **可派**（数据侧、不碰 Unity） | **高**（卡面逐张看过，见 §5） |
| **批 3 附带发现**：原版「别的单位监听这张卡死亡」在反噬 action 之内跑 | ✅ **成立 —— 这轮把判据查实了，顺序与我方【相反】** | 原版：`decomp_full/BattleManager._ResolveBacklash_d__451__MoveNext.c:88` `ResolveDeadCard` · `:94` `TriggerUnitDeathActions` · **`:144` `BroadcastDeadUnit`**（`BattleManagerSupport__BroadcastDeadUnit.c:147/206/262/369` 里**逐卡** `ResolveDeadCard` = "别的卡监听这次死亡"）· 反噬入队 `CardScript__TriggerUnitBacklashActions.c:181`。我方：`RuleCore.cs:3151` `BroadcastWhen(Die)` · `:3156` `FlushDeathWatches` · **`:3178` `FireTriggerAt(…Backlash)`** ⇒ **广播在反噬之前** | `RuleEngine/Core/RuleCore.cs`（`CleanupDeaths`，一处；落点行号已从账上的 `:2630-2637` 漂到 `:3144-3178`） | **建议并案**（归 `A962`「广播 ↔ 记账的先后」那一族）：把 `:3151` / `:3156` 两跳挪到 `:3178` 之后。⚠️ **`:3144` 的 `ctx.Emit(EvtKind.Death…)` 是表现层那一跳**（"趁格位还有意义时播阵亡特效"），**要单独判、别一起挪** | **`RuleEngineTest.Run`**（引擎行为 + 要补一条次序判别式） | 🟡 **要先裁**（并案否 / 表现层那跳怎么处理），裁完可派 | 中-高（VA 级反编译，调用图已逐条核） |

---

## 3. 逐笔展开

### 3.1 `A845(a)` —— 成立，且**只差一次实跑**

- 代码**已经改完**（`ImportMesh` 落盘段 `EffectExporter.cs:1941-1952` = 确定路径 + `CopySerialized`），源码注释 `:1936-1940` 自己写着「**这条路没实跑过**…第一次跑完导出器**必须回读** `_typelessdata`」。
- 为什么非跑不可：`EditorUtility.CopySerialized` 落到**已有网格资产**上没验过；`MeshImportMethodProbe` 的 M3 那一档验的是「从**包里的**网格 copy 进 `new Mesh()`」，输入不同 ⇒ **既不能判它死、也不能判它活**（W21 §④·1(b)）。
- 回读口现成：`MeshImportMethodProbe.ReadBackVertexRange`（`MeshImportMethodProbe.cs:56-86`，读 Force-Text 的 `.asset` YAML）。
- ⚠️ 跑完的**副作用**：`Run()` 先 `ClearGenerated()` ⇒ 现存 1399 个 `.mat` / 250 个 `.asset` 会被带走（这是**设计**，不是意外）；随后**必须**跟一次 `BoosterPackExporter.Run`。

### 3.2 `A845(b)` —— 成立，判据这轮从「未定」推进到「有候选、但要裁」

- **现场复算**（只读）：`WarpforgeVFX/Materials/` = **1399** 个 `.mat`，其中**形如 `X N.mat` 的**散布在 **199** 个基名上；
  `Meshes/` 同形 = **102**（W21 记的 190 / 102 是"且同目录还有 `X.mat`"的更严判据 —— 两数口径不同，**别当矛盾**）。
- **诊断不是唯一的**：这些 `X N` 副本至少有两个成因并存 —— **(a) 真撞名**（同一个 `src.name`、内容不同）与 **(b) 反复 `RunListed` 的累积**。W20 用**字段级**比较给出「18 个名字 / 107 个文件内容确实不同」，我用 **md5** 口径数到 130 个基名 —— ⚠️ **两种口径不可直接比**（md5 含依赖 guid 噪声），**要按哪把尺子得先裁**（W20 的字段口径更接近"内容"）。
- **候选判据**：`{stem}__{MatFields 短哈希}.mat`。理由：① 指纹**已经在代码里**（`MatFields:1323` / `MeshFields:1353`，就是撞名守卫用的那份）；② 它**与到达顺序无关**（今天"先到赢"那种随遍历顺序漂移的问题一并消掉）；③ 内容相同的源塌成一份**无害**（视觉等价）。
- ⛔ **必须标成"我们挑的"**（铁律 3）：**原版没有"材质资产"这个概念**（材质活在 bundle 里、各有自己的 pathID）⇒ 这一格**没有原版规格可照**，只有"更接近原版（一源一份）"这个方向。

### 3.3 F3 两条悬案 —— **第一条已经答了（就在 10-07 22:35 那次自检里）**

- 那次 `MainMenuScene.Run` = **2442 通过 / 0 失败**（`_tmp_view/menu.log`），四条字栏逐条给了：
  `✓ TutorialTitle 的框在原版矩形的竖位上（y 差 0.00px ≤ 1.0）` + `✓ 真渲染左缘 = 1344.25（应为框左沿 1344.25 ±1.5）`（另三条同形，`:13652/:13694/:13736/:13778`）。
- ⇒ **x/y 分解 = 全部在 x 上**（那 8.78 / 104.92 / 1.60 / 1.57px 就是 `(框宽−字宽)/2`），**账上那半句可以销**。
- ⇒ **第二因**：`AlignLeft` **之外没有影响"位置"的第二因**（y 差 0.00、x 差 0.00）。
  ⚠️ **但「`SetAutoFitBox` 收敛出的字号是否与原版相同」这一格今天仍然没有断言** —— 现行 `CheckLeftAlignedAtWorld` 比的是"真渲染左缘 vs 框左沿"，而这两个数**同源**（都从 `Label` 的 `WorldW` 缓存推）⇒ **结构上量不到字号差**。要分离它**必须跑 Unity 加一条量渲染宽/字号的断言**。⇒ **"要 Unity 量"这句只对后半句成立。**

### 3.4 `BattleDriver.cs` 过期注释 —— 一行级，可派

- **行号已漂**：账上写 `:5417-5421`，现读是 **`:5857-5861`**（`LeaveNetRoom` 的 `<para>` 段）。**别再按行号找**（铁律、`项目任务.md` 那条教训）。
- 三句都要改：①「`OnClosed` 全仓零接线」②「`OnPeerLost` 也零接线」③「原版判据没查到」。
- 顺带核过**全仓还有 6 处提到「零接线」**（`NetBattle.cs:244` · `NetBattleTest.cs:570/:628/:650` · `NetSelfTest.cs:536`）—— **那些是过去时/写在断言文案里的历史**，**正确，别一起改**。

### 3.5 `GOF50` —— 见 §5（卡面实据）

### 3.6 批 3 附带发现 —— 建议并案

- 原版链条（逐条读出）：反噬被包成 auto action 入队（`TriggerUnitBacklashActions.c:181`）⇒ 该 action 的协程体
  `_ResolveBacklash_d__451__MoveNext` 里**先** `ResolveDeadCard`(`:88`) → `TriggerUnitDeathActions`(`:94`) → … → **`BroadcastDeadUnit`(`:144`)** → `FinishResolvingAction`(`:175`)；
  而 `BroadcastDeadUnit` 内部**逐卡**再调 `ResolveDeadCard`（`:147/:206/:262/:369`）= "全场别的卡对这次死亡的监听"。
- 我方：`Emit(Death)`(表现) → `BroadcastWhen(Die)` → `FlushDeathWatches` → **反噬**（`RuleCore.cs:3144/3151/3156/3178`）。
- ⇒ 两边**方向相反**（原版：反噬 → 死亡监听；我方：死亡广播 → 反噬）。这与 `A962`（另一处"广播 ↔ 记账先后"）是同一族的**第二个落点**。
- ⚠️ 一个**不能顺手挪**的点：`:3144` 的 `ctx.Emit(EvtKind.Death…)` 是**表现层**（注释写明"趁格位还有意义的时候播阵亡特效"）⇒ 要动的是 `:3151/:3156` 两跳，`Emit` 那跳**得单独裁**。

---

## 4. `A897` 那 8 个 pid 的**复查结论**（逐 pid）

### 4.0 先把「这个 pid 属于哪个包」坐实（本轮新工具）

用 UnityPy 读**真 bundle 的 SerializedFile externals 表**（`D:/2/unity_run_ref/…/StandaloneWindows64/tutorialso_assets_all.bundle`，
其自身 CAB = `CAB-91cb8e575fb6026f8f115dafe0c2af4a`）：

| m_FileID | externals | 是什么 |
|---|---|---|
| 1 | `CAB-17b1b5f865c595045b5fe42b7f8c8967` | **`Waprforge_monoscripts.bundle`**（脚本；`m_Script` 走它） |
| 2 | `CAB-dbb6f14c119af496b058533ae98dc790` | `prebuiltdecks_assets_all.bundle` |
| **3** | **`CAB-fb8f01449289635703d83d52c3f18245`** | **`allcards_assets_all`（远端 CCD，本地没有）** |

**8 个 pid 全部是 `m_FileID = 3`** ⇒ 全部指向 **allcards**（逐 pid 复算过，`m_PathID` 与 `m_FileID` 一一对上）。
⚠️ **顺带订正一处读法**：tutorialso 的 `m_Dependencies` 列表**顺序 ≠ externals 顺序**（`m_Dependencies[0]` 是 `cab-0279bb55…`＝**tyranids…cardassets**，而它是 externals 下标 4）⇒ **⛔ 别拿 `m_Dependencies` 的下标去解 `m_FileID`**。

**`cab-fb8f0144…` 确实本地没有**：我用 UnityPy 把 84 个真包的**自身 CAB 名**全部读出建表（186 个 CAB），
`cab-fb8f01449289635703d83d52c3f18245` **不在里面**；而 `catalog.bin` 里它写作
`allcards_assets_all_ea8bca4f5c6909d3124fd4975dc96173.bundle`（catalog 里 261 处 `.bundle`、本地只有 84 个）。

### 4.1 逐 pid

| # | pid | 出现在（原版侧） | **拿到名字没** | 怎么拿的 / 为什么拿不到 |
|---|---|---|---|---|
| 1 | `-124800208381561086` | Stage2（RadioMessage ×1 · SmallTip ×1）**＋ `bundle_cosmeticsso_assets_all/MonoBehaviour/DT Goff R4.json`** | 🟡 **拿到了「原版卡 id」= `GOF2`**，**但换不成名字** | `DT Goff R4` 的 `obtainableItems[3]` = `{"genericObjectReference":{2,pid},"targetId":"GOF2","reference":{2,pid}}` —— 即**原版自己**把 allcards 里这个对象标成 `GOF2`。⚠️ **但我们的池里 `GOF2`='Boss Zagstruk'**，而 §6·① 显示**两边 id 命名空间整体对不上** ⇒ **不能据此说这张卡是 Boss Zagstruk** |
| 2 | `1696504206920290120` | Stage3 `playerStartingTroopsInHand[0]` | ⛔ **没有** | 穷举（§4.2）只命中教程 SO 本身 |
| 3 | `4406439244507976715` | Stage5 AI turn6 SmallTip · turn7 PlayCard | ⛔ **没有** | 同上（该 PlayCard 的括号里**没有名字** —— 见下注） |
| 4 | `7386907376838221809` | Stage3 AI turn2 PlayCard | ⛔ **没有** | 同上 |
| 5 | `8095124868697240450` | Stage4 `DrawCard` | ⛔ **没有** | 同上 |
| 6 | `8223774895360888806` | Stage1 AI turn0 **Attack** | ⛔ **没有** | 同上 |
| 7 | `8270355771166178363` | Stage6 `playerStartingTroopsInHand[1]` | ⛔ **没有** | 同上 |
| 8 | `8906429412044970690` | Stage6 `playerStartingTroopsInHand[2]` | ⛔ **没有** | 同上 |

> **注（为什么这 8 个偏偏没名字）**：`ScriptedAction.UpdateName()` 只在「**动作类 + 括号里写的是单位显示名**」时把名字写进括号
> （`ScriptedAction__UpdateName.c:140` 那一段：`Concat(名字, " (", *(actingUnit + 0x60), ")")`，`+0x60` = 那个对象的 `name`）。
> 这 8 个 pid 出现在 **RadioMessage / SmallTip / DrawCard / Attack / startingTroopsInHand** 这些**不带单位名**的位置
> （或者虽带括号、但 `actingUnitType` 落在生成器排除的那几档里）⇒ **不是"我们没解出来"，是原版这条数据里就没写**。

### 4.2 我搜过哪些地方 / 哪些词（⛔ 报"没有"的依据）

- **字节级全盘扫描**（不是只 `grep` 文本）：roots = `D:/2/新解包资源/assets_full` · `D:/2/解包整理` · `D:/4/Unity/数据` · `D:/2/unity_run_ref` · `D:/2/Warpforge_tools` · `D:/2/Warpforge_code` · `D:/warpforge` · `D:/4/Unity/素材` · `D:/4/Unity/MyGame/Assets` · `D:/4/Unity/MyGame/Library/Artifacts`，**共扫 676,000+ 个文件**（跳过 >80 MB 的），关键词 = 8 个 pid 的**十进制字面量**。
- **命中**：7 个 pid **只在** 教程 SO（`bundle_tutorialso_assets_all/MonoBehaviour/Warpforge_TutorialStage{1,3,4,5,6}.json`）+ 它在 `素材/Warpforge原版/游戏数据/教程/` 的**镜像** + **我们自己的产物**（`RuleEngine/Resources/tutorial_stages.json` 与 Unity 的 `Library/Artifacts` 缓存）里。第 8 个多一处 `DT Goff R4.json`。
- **包级穷举**：`ls` 了 **两个游戏安装**（`D:/2/unity_run_ref/…` 与 `D:/2/Warhammer 40k Warpforge/…`）的 `aa/StandaloneWindows64/` ⇒ **各 84 个 bundle、grep `allcards` 0 命中**；`assets_full` 91 个目录里 `find -iname "*allcards*"` 只命中无关的 `Hand_Buff_UM_AllCards.json`。
- **Addressables 下载缓存**：`%USERPROFILE%/AppData/LocalLow/Everguild/Warpforge/com.unity.addressables/` 里**只有 3 个 catalog 文件**（`catalog_main.bin/json/hash`），**没有一个 `.bundle"**；`…/LocalLow/Unity/Everguild_Warpforge/` 只有 3 个空壳目录 + `__info`。

### 4.3 结论

**`A897` 的挂起理由成立**（8 笔里 7 笔「本地确实取不到」已穷举证实）。
但**"8 个 pid 连名字都拿不到"这句要改一个字**：第 1 个 pid 能拿到**原版卡 id `GOF2`**——
**问题不在拿不拿得到 id，在于 §6·① 那个命名空间冲突**，所以它**换不成名字**。

---

## 5. `GOF50` 的卡面实据 —— **语义可恢复（"不可恢复"这句是错的）**

### 5.1 卡面原文

`d:/2/Warpforge部队卡片/Orks/4计策/Warpforge_50_Tide-of-Muscle.png`（900×1200，亲读）：

```
Tide of Muscle            （费用 3，右上）
Draw two troops and
give them +1 (粉拳图标)
```

`+1` 后面那枚 = **粉色的拳头，白圈 + 深红底**。

### 5.2 那枚图标 = 近战（Melee / 本工程的 `Attack`）

**同款图标逐像素对照**：`Genestealer Familiar`（`Genestealer Cults/3部队/Warpforge_07_Genestealer-Familiar.png`）卡面
`Adjacent units have +1 (同一枚粉拳)` —— 两枚并排放大后**同形同色**（都是"白圈 + 深红底 + 粉色握拳"）。

判据链：
1. `资料/卡表核对_卡图提取/_合并总表.md:683`：`Warpforge_50_Tide-of-Muscle.png | Tide of Muscle | … | Draw two troops and give them +1【图标:拳头】`；
   `_还原效果文字.md:677` 写作 `… give them +1Melee`。
2. `cardface_fixes.json` 的 `_manual_desc_note`（同一张表的表头说明）：**「拳头 = 近战 Attack」**（且写了"写入的是我们能解析的规范写法，如 `Gain +1 Attack`"）。
3. `Genestealer Familiar` 在**我们卡池里**已经就是 `desc: Adjacent units have +1 Attack.`（`GSC7`）⇒ **"粉拳 → `Attack`"这一步早就落地过、这就是同一个写法**。
4. `card_icon_plan.json` 里 `[Attack]`/`[attack]` 那一族的 `sprite` 就是 `"Melee"`（20+ 条，例：`DA44 desc [Attack] → Melee`「卡图 … 那枚 = 拳头」）。

⇒ **`GOF50` 的正确写法 = `Draw two troops and give them +1 Attack`**（`descZh` 已经是「抽两个部队并给予它们 +1 攻击。」，**不用动**）。

### 5.3 最小改法（可派）

| 步 | 动作 |
|---|---|
| 1 | `d:/4/Unity/数据/游戏数据/cardface_fixes.json` 的 **`desc` 列**加一条 `"Tide of Muscle": "Draw two troops and give them +1 Attack"`（⚠️ 这是**手工维护列**，`gen_cards_engine.py:818-820` 会用它覆盖；键名照同表先例**用裸卡名**，跨阵营同名才写 `阵营/名字`） |
| 2 | 重跑 `"D:/2/Warpforge_tools/py312/python.exe" -X utf8 d:/4/Unity/工具/gen_cards_engine.py`（产出 `RuleEngine/Resources/cards_engine.json`） |
| 3 | 自检 = **`RuleEngineTest.Run`**；⚠️ 若顺手加断言，**必须取原版语义**（"抽两张部队、给这两张 +1 近战"），⛔ 别复述我们的常量 |

⚠️ **别改成 `EffectText.cs` 那一侧**（账上已经写死：通道是通的、缺的是载荷）—— 改了会变成"猜一个属性"（工程红线）。

### 5.4 顺手发现：**同族还有 3 张，卡面图标一模一样**（都没改）

同一趟逐张亲读了卡面，`+N` 后面**都是同一枚粉拳**，而 `desc` 里都是**裸 `+N`**：

| 卡 | 卡图 | 卡面原文 | 我们池里的 `desc` | `descZh` |
|---|---|---|---|---|
| `GOF53 Get'em ladz!` | `Orks/4计策/Warpforge_53_Getem-ladz.png` | `Give +2 (粉拳) to your units this turn` | `Give +2 to your units this turn` | 本回合给予你的单位 +2 攻击。 |
| `GOF_Da_Red_Waaagh` | `Orks/2天赋/Warpforge_01B_Da-Red-Waaagh.png` | `Ephemeral / Draw a troop and give it +1 (粉拳)` | `Draw a troop and give it +1` | 抽一个部队并给予其 +1 攻击。 |
| `GOF_Krumpaklaw` | `Orks/3部队/Warpforge_07A_Krumpaklaw.png` | `Unstable. Mob: Gain +2 (粉拳) and (盾) Armour 1` | `Mob: Gain +2 and Armour 1` | 群体：获得 +2 攻击，并获得护甲 1。 |

⇒ **四张一个改法、一次改完**（同一次生成器重跑、同一条自检）。
⚠️ **与 `PnP卡图_逐张对账_0915.md:560` 那句「不动（照旧）」冲突**：那句话写"英文本来就是裸 `+N`、**卡面也裸**" ——
**"卡面也裸"是错的**（卡面有图标，只是**文本层**没带走）。⇒ **建议把那句就地订正**（铁律 5），并把它从"已裁定不动"改成"要做"（铁律 11）。

---

## 6. 顺手发现（**都没改**）

### ① 🔴 原版的卡 id 命名空间 ≠ 我们 `card_ids.json`（自建）的命名空间 —— **这会静默换错卡**

**怎么发现的**：`A897` 找出 `DT *` 掉落表这条路时（`bundle_cosmeticsso_assets_all/MonoBehaviour/DT <阵营> R<n>.json` 与
`bundle_duplicateassetisolationso_assets_all/…/DT Ultramarines R{3,4}.json`），它们的每个 item 都形如
`{"genericObjectReference":{2,pid},"targetId":"UM23","reference":{2,pid}}` —— **原版自己**给 allcards 里的对象标了 id。

**拿它去核我们自己的表，17/17 全对不上**（用教程 `PlayCard (卡名)` 括号里那个**客观名字**当中介）：

| pid | 教程括号里的名字（= 该资产的 `name`） | **原版 `targetId`** | 我们池里该 id 的名字 | 我们池里那个名字的 id |
|---|---|---|---|---|
| `-8500042654006139415` | `Bladeguard Lieutenant` | **`UM23`** | `Spear of Macragge` | `UM34` |
| `649335698799241562` | `Scout` | **`UM1`** | `Marneus Calgar` | `UM11` |
| `7750930892288725894` | `Assault Intercessor` | **`UM4`** | `Master of the Fleet` | `UM12` |
| `2590347532802576856` | `Primaris Intercessor` | **`UM6`** | （不在池里） | `UM14` |
| `4389092107264808524` | `Pariah Vanguard` | **`UM33`** | （不在池里） | 自造 id `UM_Pariah_Vanguard` |
| `-4629209585521307817` | `Slugga Boy` | **`GOF4`** | `Grot` | `GOF8` |
| `3079129606857888211` | `Grot` | **`GOF1`** | `Grukk Face-Rippa`（**督军**） | `GOF4` |
| `5869009015909125340` | `Shoota Boy` | **`GOF9`** | `Stormboy` | `GOF13` |
| `-1436903533376650005` | `Grot Bomb` | **`GOF32`** | `Ardshell Gurk` | `GOF41` |
| …（共 17 条，**17/17 不一致**） | | | | |

**第二个独立检查 —— 「教程里被打出的牌，得在打它的那一方的牌组里」**（31 行同类数据，甲/乙两说各算一次）：

| 判据 | 「在牌组里」 | 不在 |
|---|---|---|
| **按原版 `targetId`** | **22 / 31** | 9 |
| **按我们的 `card_ids.json`** | **4 / 31** | 27 |

（例：S6 AI 打 `Bladeguard Lieutenant` ⇒ 原版说 `UM23`，而 `UM23` **正在** S6 AI 牌组
`Ultramarines_Deck0_Tutorial6_Uriel` 的 `cardLibraryIds` 里；我们的 `UM34` **不在**。
反向的 S2 AI 打 `Point-Blank Shot`/`Pariah Vanguard` 同形。）

**第三个旁证（最直观）**：`Orks_Deck0_Tutorial2_Ghazghkull` 的 `cardLibraryIds` 里有 `GOF1`**×3**。
按我们的表 `GOF1` = **`Grukk Face-Rippa`（督军）** ⇒ 一副牌里三张督军；按原版 `targetId` = `Grot`（普通部队 ×3）。同理
`UM_SK_1`（牌组名就叫 `Spear of Macragge`）的 `deckHero.targetId` = `UM47`，按我们的表 `UM47` = **`Tactical Insight`（计策卡）**。

**为什么从来没被发现**：
- `card_ids.json` 的 `note` **自己写着这是自建的** ——「(2026-08-17 **自建: PnP 卡表编号=游戏卡ID** 偏移0 Core/扩展区段…)」。
- 而 `gen_prebuilt_decks.py:424` 的注释写着「这批 SO 的 `cardLibraryIds` 是**原版直引**」⇒ **原版 id 被当成我们的 id 直接用了**。
- 两边**恰好有一部分对得上**（反例：`AM_SK_1` 的 12 张在两边都是合理 AM 牌 ⇒ AM 那一块看着就是对的），所以冒烟测试看不出来。
- 本地**从来没有**原版 id↔资产 的对照表 —— `DT *` 掉落表是**第一份**（这也是 `A897` 那轮的副产品）。

**⚠️ 我做了什么 / 没做什么**：只**报**。**一个字节都没改**（没碰 `card_ids.json` / `cards_engine.json` / 任何 `.py`），
**也没跑任何生成器**。这是**新账**，判据在**本地**（不用远端、不用真 Play），但**影响面极大**（预组牌 / 教程牌 / 卡组编辑 /
`warlord_ids.json` 全都建在这套 id 上）⇒ **请调度台先派人只读复核一遍再动**。
⛔ **别拿 §4 那个 `GOF2` 直接回填**（两边命名空间还没对齐之前，那等于换错卡）。

### ② `DT *` 掉落表本身是一个**没用过的原版数据源**（顺手）

- 位置：`bundle_cosmeticsso_assets_all/MonoBehaviour/DT <Army> R<rarity>.json`（+ `duplicateassetisolationso` 里 2 个）。
- 结构：`obtainableItems[] = {genericObjectReference(PPtr→allcards), targetId(字符串), reference(PPtr→allcards)}`；
  `cardRarity`/`cardArmy` 在场 ⇒ **可以按稀有度/阵营摊成一份「卡 → 掉落池」表**。
- ⚠️ **两种 ppid 有时相同、有时不同**（全库统计：**相同 1934 / 不同 625**；`deckHero` 那类通常不同）⇒ **读的时候别默认它们相等**。
- **与本轮无关但会用到**：`Enchantments.json`（`markOfChaos`）等 SO 的 `targetId` 是 **GUID 形态**（例 `9a2cadc64c80a6b42912441105bb1cee`）⇒ **`targetId` 是 Addressables 地址**（这家 catalog 只有 guid 地址、名字地址是本轮的发现之一）。

### ③ `A965`（只在表里确认仍开着，未重复查）

`Editor/CardFaceProbe.cs:245 / :250` 仍写死 `meshInfo[0]`（与 `A851` 同族、判据已齐）⇒ **可派**。

---

## 7. 没查清的部分（如实）

1. 🔴 **§6·① 那笔我只有"两条原版来源相互一致、我们这一侧不一致"这个层次**，**没能独立钉死"原版 id 就是它标的那个对象"**
   （`targetId` 配的是 `reference` 还是 `genericObjectReference`，本地无法再往前证一步 —— 因为被引用的对象本身在**没有的** allcards 里）。
   我给的三个检查都指向同一侧，但**这是"强旁证"、不是"直接读到"** ⇒ **请另派只读复核**。
2. ⚠️ **`A845(b)` 的现场数字两套口径打架**（W20 的字段口径 18 个名字 / 107 文件 vs 我这个 md5 口径 130 个基名）——
   **哪把尺子对没裁**（md5 含依赖 guid 噪声，我倾向 W20 那把更接近"内容"，但**没验**）。
3. ⚠️ **批 3 那条的"反噬效果 vs `ResolveDeadCard` 谁先"**：我只能证到"**`ResolveDeadCard`/`BroadcastDeadUnit` 在反噬那条 action 的协程体内**"
   （`_ResolveBacklash_d__451__MoveNext.c:88/:94/:144`），而**反噬的伤害本身在哪一跳执行**没在协程体里看见
   ⇒ "先反噬、后广播" 这个方向**置信度中-高，不是 100%**。要坐实得再读 `TriggerUnitBacklashActions` 建的 action 的 ops。
4. ⚠️ **F3 第二条**只判到「位置层面没有第二因」；**字号那一格今天没有断言、也没量过** ⇒ 若调度台认为这一格要闭合，**要跑 Unity 加断言**。
5. `A845(a)` 的真风险（`CopySerialized` 落到已有网格）**只能靠跑一次导出器回答**，本轮**没有**任何新证据能替代它。
