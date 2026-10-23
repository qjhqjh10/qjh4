# 执行代理 B —— 引擎线三件（`2026-10-23`）

> 范围：`animator_controllers.json` 的 `Cannon 1` 数据错 · `W62·自检` 残余 · `BoardSlots.Resolve` 侧向分支 ·
> （有余力）`W63·abilityContext` 尾巴。
> 🔴 **本代理没有跑 Unity**（同一工程只能一个实例，主对话在用）⇒ `RuleEngineTest.Run` **本轮没跑**，
> 新增/改动的断言**只有离线证据**（见 §二·3 与 §三·1）。

---

## 一、结论（逐件）

| # | 任务 | 结论 | 一句话 |
|---|---|---|---|
| 1 | `animator_controllers.json` 的 **`Cannon 1`** | ✅ **完成** | 根因坐实（跨包同 PathID 裸字典覆盖）并修好；`Cannon 1` 现读 = **`Cannon front Idle`**；防回归 = 「包内优先 + 全库唯一才回落 + 矛盾点名」＋产物里新增 `_clipIndexCollisions`（5 个 pid 的完整对照表）。 |
| 2 | **`W62·自检` 32 红 / 7 根因** | ✅ **已由前批完成（本轮只做核对，未改代码）** | 7 个根因**逐条**在代码里找得到落点（日期全是 2026-10-22）；引擎线任务总表 `W63·自检12红` 行已记「复跑 **6308 / 6308 全过**」。⇒ 本轮**没有再动** W62 的任何代码。 |
| 3 | **`BoardSlots.Resolve` 侧向分支** | ✅ **完成**（本轮唯一的引擎**行为**改动） | 「插推族」最后一段补齐：**有洞**棋盘上算出来的落点若**已经有人** ⇒ 改该侧最外的空格并出声；`Insert` 的有洞分支也补齐「请求格被占」那一档，**不再落到插推循环**。**连续棋盘行为逐字不变**（已离线逐档验证）。 |
| 4 | **`W63·abilityContext` 尾巴** | ⛔ **卡住 / 未改行为**（有意） | 那个「v14 缺行 = `Play(0)`」的一跳**不是一行能补的**：它会把 `RuleCore.cs:8921`（Slay）与 `:10630`（Mob）**明明刻意传 `null` 关键词来维持 `false`** 的两处翻成 `true`，而这两处的注释**预告了会回归**（`W63 #2` / Mob 额度被凭空取消）。这属于「口径要由主对话拍板」的范畴 ⇒ **不动**，把数据摆出来（§二·4）。 |

---

## 二、证据

### 1 `Cannon 1`（根因 → 修法 → 防回归）

**根因**（`工具/gen_animator_controllers.py` 改前 `:350-352`）：
```python
clip_index = {}
for r in roots:
    clip_index.update(clip_names_in(r))     # ← 90 个包并进一个裸 {PathID: 名字}
```
PathID **只在包内唯一**，`PathID 20` 在 **3 个包**里都有：

| 包 | PathID 20 的名字 |
|---|---|
| `bundle_scenes_scenes_battlearena1` | **`Cannon front Idle`**（`Cannon 1` 真正引用的那条） |
| `bundle_scenes_scenes_battlearenaemperorschildren` | `Emperor's Children Scene Animations` |
| `bundle_scenes_scenes_battlearenatauviorla` | `Railgun Turret 1` |

按目录名排序扫到最后一个是 `…ta viorla` ⇒ **静默覆盖**（实测改前产物里 `Cannon 1` 的 `clips[0]` 与 `clip` 都是 `Railgun Turret 1`）。

**修法**（`工具/gen_animator_controllers.py`）：
- `:218` 新增 `build_clip_index(roots)` → 返回 `(per_bundle, global_index, collisions)`：
  包内索引 `{bundle: {pid: name}}`；全库索引 `{pid: [(包名, 名字), …]}`（**列表，不覆盖**）；
  `collisions` = 全库范围内「同 pid 在 ≥2 个包里且名字不同」的对照表。
- `:250` 新增 `resolve_clip(...)`：**同包优先** → 包里没有时，全库**只有唯一名字**才回落 →
  **候选矛盾 ⇒ 点名进 `_unhandled`、留空，绝不替它挑**。
- `:420-429` 打印防回归读数（现读 **5** 个 pid：17/18/19/20/23）。
- `:475-484` 产物新增 `_clipIndexCollisions_note` + `_clipIndexCollisions`（**5 个 pid 的完整对照表**）。

**重生成结果**（`python 工具/gen_animator_controllers.py`，rc=0、`解不动的：无`）：

```
'Card 3D WH40K Explosion' -> 'Card Explosion'
'Battle Arena Leviathan'  -> 'SporeArmature|LeviathanBigSpore'
'Cannon 1'                -> 'Cannon front Idle'      ← 修好了
'Pump 1' / 'Dynamic Skulls' / 'Battle Arena 2' 三条不变
```
产物 diff = **75 增 / 2 删**，其中只有两行是**值**的变化（`clips[0]` 与 `layers[0].states[0].clip`），
其余全是新增的两个 `_clipIndexCollisions*` 节 —— 手写的 `clipsByGuid` **2/2 原样保住并逐条核对通过**。

⚠️ **下游影响 = 0（现在）**：`MyGame/Assets` 里**还没有**由这张表建出来的 `.controller` 资产
（`find -iname '*Cannon*'` 只命中与控制器无关的音频/脚本；消费者是 `WarpforgeArena1/Editor/EffectExporter.cs:4530-4540`）。
⇒ 这次修正会在**下次跑导出器时**才落到资产上，不会与盘上的旧资产冲突。

### 2 `W62·自检` —— 逐根因核对（**结论：已修完，本轮未改**）

| 根因 | 代码里的落点（现读） |
|---|---|
| **R4**（β·1，闭包捕获 `for` 变量） | `Core/EffectResolver.cs:8066` `var _p = p;`（`:8061` 留着 `W62·R4` 的就地订正注释） |
| **R1**（4 个入口没有 `ActionScope`） | `Hurt` = `RuleCore.cs:7756-7759` · `GrantDarkPact` = `EffectResolver.cs:9080-9081` · 公开 `BroadcastWhen` = `EffectResolver.cs:7339-7340` · `TryCancelAttackByOwnAboutToAttack` = `RuleCore.cs:6868-6869`（四个都是 `using (BeginAction(ctx)) …Core(…)` 的壳+体形状） |
| **R2**（触发入队与内联结算次序反转） | Mob/Synapse 额度 = `EffectResolver.cs:2150-2178`（消费入队）· 激励 = `:1960-1995`（战术正文整段入队）· Slay 交错 = `RuleCore.cs:8782-8800` + `DeathBatch`（`ActionPump.cs:592-604`）· 被哨戒打死的攻击者 = `BattleContext.DyingThisAction`（`:1574`）+ `RuleCore.cs:9278` `IsUnitInPlayOrDying`· 狂暴快照 = `EffectResolver.cs:7313/7376/7480` |
| **R3**（指代槽生命期） | `RuleCore.cs:2590-2602`：`using (DrawReferentScope.Seed(...)) using (BeginAction(ctx))` —— 槽在**外层**，盖住泵点 |
| **R5/R6/R7**（断言/夹具侧） | `RuleEngineTest.cs:26972`（改钉 `MaxPumpedActionsPerDrain`）· `:28120-28124` + `pinned` 表已改钉 `PlayCardCore`/`DeployFreeCore` · `:23848`（维护费期望 **0**）· `RuleEngineTest_T03b.cs:206-219`（`d0.Add(blocker)` 放末尾 = 牌库顶） |

补证：引擎线任务总表 `W63·自检12红` 行明写「✅ 复跑 = **6308 / 6308 全过（12 红清零）**」，
`资料/普查产出_1022/W62_引擎32红修复.md` 是那一批的修复报告（R1–R7 逐条）。

⚠️ **唯一仍开着的残余**：诊断建议的「**公开入口返回时 `ctx.ActionQueue.Count == 0`**」结构自检
**至今没加**（`W62_引擎32红修复.md` §五·2 也记着）。理由成立：它是防「以后又漏包一个入口」的唯一结构闸。
本轮**没有代它加**（见 §三·2 的理由）。

### 3 `BoardSlots.Resolve` 侧向分支（本轮唯一的引擎行为改动）

**改前**（`BoardSlots.cs` 侧向分支）：`index = IndexOf(requestedSlot)` → 夹到 `[0, count]` → **直接返回**。
**问题**：那个 `index` 是**插入下标**，`Insert` 会从 `count-1` 倒着搬到 `index`。在**有洞**棋盘上：

| 形状 | 改前后果 |
|---|---|
| 洞在中段（右 index0/2 有人、index1 空着），请求**最外那格**（index2，有人） | `index == count == 2` ⇒ 循环不跑 ⇒ **直接覆盖 index2 那个人** |
| 同上，请求 index1 以外、`index < count` | 插推循环把 **null 搬进 index2** ⇒ **那个人被抹掉** |
| 请求侧满、换对侧，对侧有洞 | `index = otherCount` 指到**被占格** ⇒ 同上 |

（原版两侧是连续 `List`，**结构上不可能有洞** ⇒ `AdjustedSlot.c` 全文**没有任何占用检查**；
这一档是补**我们表示法**的洞，⛔ 不是「照原版」。）

**改后**（`Core/BoardSlots.cs`）：
- `:202-241` 侧向分支末尾加一道闸：**`SlotOf(side,index)` 已经有人 且 棋盘不连续** ⇒
  改到**该侧最外的空格** + `Debug.LogWarning`（出声）；该侧没有空格（不可达）⇒ 换对侧最外空格 ⇒ 仍没有 ⇒ `false`。
- `:256` 新增 `OutermostFreeOnSide(ps, side)` —— **唯一**的「该侧最外空格」判据；
  `NextFreeWithoutDisplacing`（`:450`）改成调它（原先那段内联循环删掉，防止两处写法漂移）。
- `:329-346` `Insert` 的有洞分支：**请求格被占**也不再落到插推循环（原来只有「督军格 / 越界」走 `Resolve`），
  一律 `Resolve` → 直写 → ⛔ 不推人（两种情形合成同一段，避免两处写法漂移）。
- `:132` `Resolve` 的 XML doc 补了第 4 条分支说明。

🔴 **连续棋盘上行为逐字不变**（这是最关键的一条）：`count >= SlotsPerSide` 那支与 `index = clamp(IndexOf)` 那支
**一个字都没改**；新闸的前置条件是 `ps.Board[landing] != null && !IsContiguous(ps, out _)`
—— 连续棋盘上「请求的格有人」是**正常的插入点**（`List.Insert` 推后面的人），**绝不能换格**。

**断言**（`Editor/RuleEngineTest_T02.cs:856` `TestT02ResolveSideBranchHonoursAvailability`，共 6 块 ~30 条）：

| 块 | 钉什么 | 灭自证（🧨） |
|---|---|---|
| ① | 连续棋盘、请求 5 号格（有人）⇒ `(Right, 0)` **原样返回** | 把闸写成**无条件** ⇒ 实得 8 |
| ② | 连续棋盘端到端：插在两人之间 ⇒ 5→6、6→7 整体外移、对象同一 | 改成「恒追加最外」⇒ 红 |
| ③ | 有洞（中段洞）、请求被占的 7 号格 ⇒ **8 号格** | 删闸 ⇒ 实得 7（被占） |
| ④ | 有洞、请求**正是那个洞**（6 号格）⇒ **不换格**（`index(1) < count(2)`） | 写成「只要有洞就恒取最外」⇒ 实得 8 |
| ⑤ | 请求侧满 + 对侧有洞 ⇒ 换对侧后取**对侧最外空格 0**（不是被占的 1） | 删闸 ⇒ 实得 1 |
| ⑥ | 端到端：`PlayCard(…, 7)` 真落 8，7/5/3/2 号格一个都没被抹/挪 | 改回裸算式 ⇒ 7 号格那个人被静默抹掉 |

同步两处：`Editor/RuleEngineTest.cs:848` 加 `Step(...)`，并用
`PYTHONIOENCODING=utf-8 python 工具/gen_diag_runner.py` 重生成 `RuleEngineTest_Diag.cs`
（`RunCaseNames` 292 条 + `Safe(...)` 派发表**都由生成器写**，见 diff `3 增 / 2 删`）。

**离线验证（本轮唯一的「跑过」证据）**：把 `RuleEngine/Core/*.cs` ＋ `Data/CardDatabase.cs` 之外的最小桩
编进一个 **`/tmp` 下的临时 net8 工程**（不碰仓库），用**真的** `BoardSlots` / `RuleCore.NewBattle` 跑了 7 个场景，
读数与上面 6 块断言的期望**逐条相同**（`== ALL OK ==`），两条 `LogWarning` 也确认真的打出来了。

### 4 `W63·abilityContext` 尾巴 —— 为什么**没有**动行为（数据）

被点名的那一跳是 `ActionPump.AbilityContextOfAbility`（`:265-271`）**缺「v14 缺行 = `Play(0)`」**
（现在缺行 ⇒ `-1` ⇒ 回退 `LegacyActorMustBeInPlayForKeyword` 那张 5 词表）。

**判据侧确实闭合**：现读 `cards_engine.json` **v16**，`abilityContext` 落 **157 张 / 170 条**，
**真值快照覆盖 1127/1127 张**（`gen_cards_engine.py` 干跑读数）⇒ 「缺行」在两处语义里都不是「没数据」。

**但这一跳不是安全的**，两条硬证据（都在代码里，是 2026-10-22 的 D 批自己写的）：

1. `RuleCore.cs:8917-8921`（Slay 记账入队）注释原文：
   > 「旧表会判成 `true`，而我们的闸在**出队时**判（原版在入队前判，简报 §6.7）⇒ 会把 `inlineSlay`
   > 刚要就地结算的正文再次取消（**= `W63` #2 的回归**）。`-1` + 无关键词 ⇒ `false`，与改前逐字相同。」
2. `RuleCore.cs:10626-10630`（Mob 额度）注释原文：
   > 「给 `mob` 关键词会让旧表判成 `true`，而额度是在**监听器动作里**挂的、这条要等出队才跑 ⇒ **会凭空取消**。」

这两处**刻意**传 `null` 关键词就是为了拿 `-1`。而「缺行 = `Play(0)`」这一跳**恰恰**会把它们翻成 `0` ⇒ `true`
（Slay 在真值里 **22 条全是 `context 0`、0 条非 0**；Mob **17 条全 0** ⇒ 这两族的卡**全部**会翻）。

**池内会翻的卡数**（真值快照 ∩ 池，按触发名；`context 0` 的卡）：

| 触发名 | 会翻成 `true` 的卡 | 非 0 行 | 受影响调用点 |
|---|---|---|---|
| `Slay` | **10** | 无 | `RuleCore.cs:8921`（刻意 null ⇒ **会回归**） |
| `Mob` | **16** | 无 | `RuleCore.cs:10630`（刻意 null ⇒ **会回归**） |
| `UnitAttack` | **4** | 5 条 40 | `RuleCore.cs:5794` |
| `Unstable` | 0 | 18 条 40 | `RuleCore.cs:8626`（不受影响） |
| `Strike` | 24 | 4 条 40 | 关键词总口 `RuleCore.cs:10536` |
| `Artifice` / `Pray` / `Uprising` / `Teleport` / `Codex` … | 18 / 23 / 9 / 5 / 29 | 各自混 | 同上 |

⇒ **真正「做准」的路不是这一跳，而是把判点搬到【入队那一刻】**（原版
`RawCardScript__TriggerAbilities.c:108,207` 就是入队前判）—— 但 `ActionPump.cs:225-228` 的 doc 明写：
「改入队判会让 `RuleEngineTest` 的 `T27` ②③ 红」⇒ **要同时改那两条断言**（它们钉的是现状=出队判）。
这是**口径 + 断言一起动**的活，且**必须在能跑 `RuleEngineTest` 的机器上做** ⇒ 本轮**停手**。
本轮**没有**为了它改任何行为、也没加「只是不用」的死代码。

---

## 三、没查清的部分（如实标着）

1. **`RuleEngineTest.Run` 本轮没跑**（Unity 归主对话）⇒ 本轮新增的 6 块断言**未经 Unity 实跑**；
   我能给的只有：`typecheck` 0/0（编辑器程序集也算过）+ 上述 `/tmp` 临时工程里跑**真 `BoardSlots`** 的 7 个场景。
   ⚠️ **两个我判不了的风险**（主对话跑 `RuleEngineTest.Run` 时请留意）：
   - `Insert` 的有洞分支现在**不再插推**。若有既有用例在**有洞夹具**上 `PlayCard` 到一个**被占的侧向格**
     并断言落点，会变红（那种用例原本钉的是「覆盖别人」这个缺陷本身）。
   - `Resolve` 在有洞棋盘上的返回值变了（`SideOf/IndexOf` 可能换到同侧别格）——`BattleDriver` 的拖动预览
     （`CardPresentation/Battle/BattleDriver.cs:4141/19799`）只在非法局面上受影响，真对局走不到。
2. **「公开入口返回时 `ActionQueue.Count == 0`」那条结构自检没加**：它要遍历 15+3 个入口逐个造局面，
   而且对局结束（`ctx.IsOver`）那一档本来允许残留 ⇒ 写不好会**新增红**，而我**没法实跑确认**。
   建议主对话在能跑自检时补（`W62_引擎32红修复.md` §五·2 已立账）。
3. **`AbilityContextOfAbility` 那一跳的「正确形态」没有定论**：我只证了「现在照字面加会回归」，
   「改成入队判」的**具体改法**（`QueuedAction` 要不要存「入队那一刻的判定结果」、`T27` ②③ 该钉什么）
   没设计 ⇒ 留给主对话定口径。
4. **`10/20/30` 卡级载体在泵点仍表达不出**（`PumpActionQueues` 要求 `act.Actor != null` 才判 context，
   而 Hand/Deck/Cemetery 那三档的载体是 `CardInstance`、可能没有场上 `UnitState`）——
   这是 W63 原来就记着的另一条缺口，本轮**没动**（要 `QueuedAction` 加载体字段 + 各入队点传值，属扩权活）。

---

## 四、改动清单（逐文件 + 一句话）

| 文件 | 行尾 | 改动 |
|---|---|---|
| `工具/gen_animator_controllers.py` | LF | 新增 `build_clip_index` / `resolve_clip`（包内优先、全库唯一才回落、矛盾点名）；打印跨包同 pid 对照表；产物新增 `_clipIndexCollisions(_note)`。**不再有裸 PathID 覆盖。** |
| `数据/游戏数据/animator_controllers.json` | LF | 重生成：`Cannon 1` 的 `clips[0]` 与 `states[0].clip` 由 `Railgun Turret 1` → **`Cannon front Idle`**；新增两个 `_clipIndexCollisions*` 节；手写 `clipsByGuid` 2/2 原样保留。 |
| `MyGame/Assets/RuleEngine/Core/BoardSlots.cs` | LF | `Resolve` 侧向分支加「有洞且落点被占 ⇒ 换该侧最外空格并出声」；新增 `OutermostFreeOnSide`（`NextFreeWithoutDisplacing` 改调它）；`Insert` 有洞分支补「请求格被占 ⇒ 直写不推人」；`Resolve` doc 补第 4 条分支。 |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_T02.cs` | LF | 新增 `TestT02ResolveSideBranchHonoursAvailability`（6 块 ~30 条，每条带 🧨 灭自证）。 |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | **纯 CRLF**（改后仍 34815/34815） | `Run()` 里加一行 `Step(TestT02ResolveSideBranchHonoursAvailability);`。 |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_Diag.cs` | LF | **生成器写**（`gen_diag_runner.py`）：`RunCaseNames` 292 条 + 对应 `Safe(...)` —— ⛔ 没手改。 |

**验收读数**（本轮实跑）：
```
$ TMPDIR=/tmp/wf_B bash d:/4/Unity/工具/typecheck.sh      # ← 改完全部文件那一刻跑的
运行时错误数: 0
编辑器错误数: 0

$ bash d:/4/Unity/工具/ruleprobe.sh check
=== 哨兵合计：失败 0 条 ===
=== 全池 1121 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
=== 06:57:05 结束（rc=0）===
```
🔴 **类型检查的第二次读数不是 0（如实记）**：约 07:0x 再跑一次时 `运行时错误数` = **9 → 5 → 4**，
**全部落在别人正在写的文件上** —— `CardPresentation/Core/VfxMap.cs`（`RtCard.Id` 不存在，4 条）与
`CardPresentation/Battle/BattleDriver.cs`（`CardAnimClips.BodyClipOf` / `CardAnimPlacement.Ts/Tm`，上一刻 5 条）。
**没有一条落在 `RuleEngine/**`**（按文件分布 `grep error CS | uniq` 确认）⇒ 依 `CLAUDE.md` 铁律 13·3，
那是**别的代理正在写**的半成品，⛔ 不是本批的改动引起、也没去改别人的文件。
✅ 本批自己的两个面都有独立证据：`Core/*.cs` 由 `ruleprobe` **单独编过**（rc=0，07:0x 那次）、
`Editor` 面在**我改完那一刻**是 0/0。
⛔ 没碰：`CardPresentation/**` · `WarpforgeArena1/**` · `WarpforgeVFX/**` · `CLAUDE.md` · `项目任务.md` ·
`资料/任务线/**` · `d:/2/**` · git · Unity。

---

## 五、顺手发现（**只报不改**）

1. **`animator_controllers.json` 里同族还有 4 个 pid 会撞**（17/18/19/23，全是 `battlearena*` 包的场景动画）。
   修好之后它们各自走「包内优先」⇒ **当前 6 个控制器的读数全部正确**；但**没有控制器引用它们**，
   所以真正的消费方（如果有）必须自己按 `(包, pid)` 取 —— 现成的对照表已经在产物的 `_clipIndexCollisions` 里。
2. **`RuleCore.cs:11124` 那句 `if (!ctx.DeathsDeferred) TryCreditKill(...)` 现在是死代码**（队列体一律在
   `SimultaneousDamage` 里）——`W62_引擎32红修复.md` §五·1 已记，本轮复核**仍成立**。
3. **`BoardSlots.Resolve` 的「请求侧满 ⇒ 换对侧」这一支，`index` 的语义与主支不同**：
   主支是「插入下标」，这一支是「追加下标（= 对侧人数）」。连续棋盘上两者恰好同值，
   但两个分支现在共用末尾同一道有洞闸 —— 读代码的人容易误以为整段都是「插入下标」。**建议**（未做）
   在那一支补一句「这一支的 index 恒等于对侧人数，不是插入点」。
4. **`gen_animator_controllers.py` 的 `--check` 现在不会因为「跨包 pid 撞号」而返回非 0**（这是刻意的：
   撞号是常态、不是错误）。但如果将来有人把「回落」用在了**同包没有、全库唯一**的路径上而恰好撞号，
   只有 `_unhandled` 会出声 —— 那条路已经会出声（`resolve_clip` 的第三个分支），我复核过。
