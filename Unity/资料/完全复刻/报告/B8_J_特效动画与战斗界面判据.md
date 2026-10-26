# B8_J · 特效动画 / 战斗界面-HUD 判据报告

> 判据代理 `B8_J` · 2026-10-26
> 覆盖：`账/特效动画.tsv` 的 **B 38 + C 49 = 87** 条 · `账/战斗界面_HUD.tsv` 的 **C 15** 条 = **102 条**
> 产物：`判据/B8_特效动画与战斗界面_判据表.tsv`（102 行 × 12 列）· `判据/B8_三条缺口_判据与资源.md`
> 纪律：⛔ 未改任何 `账/*.tsv` · ⛔ 未跑 Unity · ⛔ 未碰 git · ⛔ 未改任何 `.cs`（本轮结论**不需要改代码**，见 §⑥）

---

## ① 结论

### 1·1 三块各能升 A / 留 B / 判 D 几条

| 块 | 条数 | 能升 **A** | 留 **B** | 留 **C** | 账→**C**（降级） | 建议 **D**（P3，待调度台） |
|---|---|---|---|---|---|---|
| 特效动画 · B | 38 | **0** | **34** | 0 | **1**（`CardEffectsService.EnableCardEffectSlots`） | **3**（`SegmentationController` 376/377/378） |
| 特效动画 · C | 49 | **0** | 0 | **44** | — | **5**（`SegmentationController` 380–384） |
| 战斗界面/HUD · C | 15 | **0** | 0 | **15** | — | 0 |
| **合计** | **102** | **0** | **34** | **59** | **1** | **8** |

**「能升 A = 0」不是没查，是口径**：A 档要求**两侧都到方法级**（原版方法 ↔ 我们某几行逐句对齐）。
本轮 102 条里，34 条的我们侧落点是「**等价机制**（拍序列 / 整块重建 builder / MemberwiseClone 载体）」
或「**类级命中**」—— 判据本身已经全清，但落点达不到方法级 ⇒ 按方案 §4.1 一律留 B。
**那 34 条留 B 的里面，有 18 条（NestedFadeGroup 一族）连差异都查清了（= 不嵌套，只剩粒度）**，
但落点在 `CardView.cs`（14k 行 + 同时在别的写手手里）⇒ 那是**实现活**，不是判据活。

### 1·2 三条缺口各是什么结论

| 缺口 | 结论 | 判据清不清 | 缺的到底是什么 | 工作量 |
|---|---|---|---|---|
| **A** `DoSimpleShake` 缺接线 | **组件侧已完整、下游已接，缺的是「引擎 → 表现」那 4 跳** | ✅ 全清 | `EffectResolver.EmitTraitRemoved` **+1 个可选形参** `effect:` · `RuleCore` **3 处** emit（1 开舱 `:7670` / 2 破舱 `:8369`+`:8373`、`:8426`）· `BattleDriver` **1 处**消费 | **小** |
| **B** `ItemDrawer.OnPointerClick` 缺那一声 | **cue / clip / PathID / 三份资源位置全部现读定位；缺的只是「一个 wav 没导入」+ 3 行播放代码** | ✅ 全清 | `Resources/Art/audio/sfx/UI Click 1.wav`（**工程内素材库已有原件，只是没导入**）+ `ItemDrawer.cs:1971` 改 3 行 | **小**（但要 1 次 Unity 批处理） |
| **C** `OpponentDesyncCancelMatch` 缺 `MatchWon == false` | **缺的不是「不判结果」（`Abort` 已做到），而是「离场」那一半**；原语 `NetSession.Close(say:false)` **已经存在** | ✅ 全清 | `NetBattle.LeaveWithoutResult(why)` = 本机不记账 + **`Close(say:false)` 硬断**（⛔ 不是 `Close(true)`，那会让对端**当场**判我们弃权；原版是 15 s 重连窗口后才判） | **小** |

**三条缺口的详细判据、逐行改法与资源位置 → `判据/B8_三条缺口_判据与资源.md`**

---

## ② 按域交叉表（状态 × 建议 × 工作量）

| 域 | 账状态 | 建议状态 | 工作量 小 | 中 | 大 | 小计 |
|---|---|---|---|---|---|---|
| 特效动画 | B | B | 8 | 8 | 18 | 34 |
| 特效动画 | B | C | 1 | 0 | 0 | 1 |
| 特效动画 | B | D | 0 | 0 | 3 | 3 |
| 特效动画 | C | C | 21 | 16 | 7 | 44 |
| 特效动画 | C | D | 0 | 0 | 5 | 5 |
| 战斗界面/HUD | C | C | 7 | 8 | 0 | 15 |
| **合计** | | | **37** | **32** | **33** | **102** |

**「大」那 33 条集中在三族**（都可切块并行，文件互不相交）：
① **NestedFadeGroup 18 条**（同一份 `CardView` 改动，一次解决 18 条 —— 但 `CardView` 是 Hot 文件）
② **SegmentationController 8 条**（P3，建议整体判 D）
③ **ObjAnimEffects 7 条**（同一件通用分派组件 + 9 个 prefab 的数据导出）

---

## ③ 最值钱的发现

### 3·1 🔴 账里 **7 处判据出处/语义是错的**，已就地订正（铁律 5）

| 账原写 | 实读 | 影响 |
|---|---|---|
| `BasicCardUI.cs:583-586`（`SetEmptyRarityStars`） | **`:583-586` 是 `OnDescriptionShow`**；`SetEmptyRarityStars` 真身在 **`:514-517`** | 照账去核会核到**另一个方法**，以为「判据对上了」 |
| `SetupActiveSkill` 的闸在 `:170` | 闸在 **`:160`**（`:170` 是 `else` 支的那句 `text7 = …Description()`） | 「什么时候拼后缀」的核心判据指错行 |
| `ManaTypeHolder.SetSavedMana :186-260` | 体是 **`:186-246`**（`:248` 起已是 `ToggleManaAccumulation`） | 半句判据落在别的方法里 |
| `ObjAnimEffects.PlayDelayedEffect` = 「按 animDelay 分流」 | 分流那段是 **`PlayEffects`（`:137-173`）**；`PlayDelayedEffect`（`:222`）只是 `WaitForSeconds(animDelay)` 的协程 | 方法级语义**整体错位** |
| `ObjAnimEffects.PlayEffect` = 「同 PlayDelayedEffect 但不看 waitToPlayEffects」 | `PlayEffect`（`:175-220`）是**按 `effectType` 分派**的那个方法 | 同上 |
| `ObjAnimEffects.ResolveAnimPrefab` = 「协程等 animDelay」 | `ResolveAnimPrefab`（`:289`）是**实例化 prefab** 那一步 | 同上 |
| `ResolvePlaySound` / `ResolveFade` / `ResolveScreenShake` 三条描述互相错位 | 正确：`ResolveFade`:237 = 批量淡变 · `ResolveAnimPrefab`:289 = 实例化 · `ResolvePlaySound`:313 = `PlayOneShot` · `ResolveScreenShake`:338 = `CameraShake` | **四条判据串了一格**，照账实现会写错 4 个方法 |

**额外一条（账里没有的）**：`ObjAnimEffects.ResolvePlaySound`(:313) 与 `ResolveScreenShake`(:338)
**在本类零调用者** —— `PlayEffect` 把这两段**内联**了（`:190-212` / `:213-216`）⇒ 它们是原版**死代码**。
照账当"要实现的方法"去做，会做出两个永远不跑的方法。

### 3·2 🔴 缺口 C 的真实缺口与我们记的**不是同一件事**（这条最值钱）

账（`战斗界面_HUD.tsv:181` + `NetBattle.cs:325-331` 的注释）都写「本机没有『不判结果直接离场』的等价物」。
**现读：那个等价物已经有了** —— `NetBattle.Abort()`（`:268`）就是「只报信、本机不记账」。

**真正缺的是「离场」那一半，而且做法和我们想的相反**：

- 原版 `CancelMatch(false)` ⇒ `BattleNetworkManager.LeaveBattle()` ⇒ **`LeaveRoom()`**（退房）。
  对端走 `OnPhotonPlayerDisconnected` ⇒ `_ForfeitDisconnectedEnemy` ⇒ **先 15 s 重连窗口、到期才判**。
- 我们若用 `Close(true)` 离开 ⇒ 对端走 `HandlePeerClosed`（收到 `bye`）⇒ **当场判我们弃权**。
  ⇒ **这不是等价物，是把「无结果」变成「立即判负」。**
- 而 `NetSession.Close(say: false)`（**不发 `bye`**，注释自己写着「对面只能靠心跳超时发现」）
  才是 `LeaveRoom` 的对端语义 ⇒ 对端落 `HandlePeerLost` → `StartReconnectCountdown()`（15 s）。

⇒ **结论：这个缺口的答案是「加 `Close(say:false)`，不要加判负/判胜的支」**，工作量从「中」降到「小」。

### 3·3 🟡 `MatchWon == false` 那一支**不是** `CancelMatchWithoutResult`

账与多次文档把两者并列写。现读：`CancelMatch(false)` **只调** `BattleNetworkManager.LeaveBattle()`；
`BattleManager.CancelMatchWithoutResult` 是**另一个方法**，而且它**会给自己记一笔结果**
（`PlayerDataManager.CancelMatch(won = !(state ∈ {0,7,8}))`，`decomp_full/BattleManager__CancelMatchWithoutResult.c:3-60`）
—— 与「不判结果」是**反的**。⛔ 别再带这个名字。

### 3·4 🟡 缺口 A 的「唯一调用者」其实是**三个时机、两个力度**

`CameraShakerManager.DoSimpleShake` 的唯一调用者是 `CardScript.RemoveDropPod`（这句账上对），
但 `RemoveDropPod` 有 **3 个调用点**：回合开始自动开舱（`CardScript.cs:7348`）· 出手后自动开舱
（`_ResolveAttack…:339-379`）· 血池被打空（`CardScript.cs:15251`）。
力度 = `isDestroyed ? 3.0 : 0.5` ⇒ **只补「开舱」会让破舱少 6 倍**。
**并且我们引擎的血池写入点有 3 个**（`:7670` / `:8369` / `:8426`），破舱的 emit 要**两处**都补
（`:8426` 是「易伤第二发把池打空」，可达性今天 = 0，但铁律 11 要做）。

### 3·5 🟡 账的「物理行」比文件真行**小 1**（全库性的口径问题）

现读实测：账写 `特效动画.tsv` 物理行 `72` = 文件**第 73 行**；账写 `战斗界面_HUD.tsv` `19` = 文件**第 20 行**。
⇒ 所有按「账物理行号」定位的人都偏一行。**本表的 `账物理行号` 沿用账的编号**（方便与调度台/别的代理对表），
但**真要 `grep` 文件时请 +1**。
⚠️ 这条要在 `调度台日志` 里落一笔 —— 它会让「按行号核对」这类审计**静默核到隔壁那一行**。

### 3·6 🟢 `AnimateUIMaterialProperty` 那一族的判据**已经够了**，不需要再查

G12 已把结构读全（两个 `AnimatedMaterialPropertyFloat` 槽 · `Equals(default)` 守卫语义从 `.c` 的
`il2cpp_value_box + ValueType.Equals` 读出）。本轮**独立复核了 `Update` / `OnEnable` / `SetMaterial` 三个方法体
都在 ILSpy 里、不是签名壳** ⇒ 剩下的只是「建通用件 + Shell 两处接线」，归 F7。

---

## ④ 没查清的部分（如实写，⛔ 不拿猜测填空）

1. **NestedFadeGroup 一族的 18 条，我**没有**逐方法写「这个方法自己做什么」** ——
   18 条的 `原版行为` 用的是**整族的组树语义**（G12 已读全）。要拆到方法级（`OnAlphaChanged` 具体推给谁、
   `UpdateAlpha` 与 `RefreshAlpha` 的先后）还得再把 `NestedFadeGroupBase.cs` 读一遍。**本代理没做**，
   表里已注明「本行原版行为 = 组树语义；逐方法一句话需再读一次」。
2. **`ItemDrawer` 的 `inspectSound` 到底**是不是** `ButtonClick 1`** —— 本轮复核的是
   「cue `ButtonClick 1` 存在 且它的 `clipList` → `UI Click 1`」以及「`R0_资源覆盖账.tsv:244580` 写着
   `UI Click 1 <- ButtonClick 1`」。**`ItemDrawer` prefab 实例上 `+0x48` 那个 PPtr 的值我没读**
   （要解 `bundle_menus_assets_all` 的 raw MonoBehaviour，无 type tree）。cue 名沿用上一轮的结论 ⇒
   **建议导入时顺手核一次 prefab**（若 `+0x48` 指向别的 cue，第 5 步那 3 行照样成立，只是 cue 名要换）。
3. **`CameraVerticalFramer.CalculateFraming` 的三处缺口里，(b)「把 `originalSensorSize`/`originalLensShift` 写回相机」
   我们代码已显式记「在我们这一档没有可观测效果」**。本轮**没有独立验证**这个论断
   （要读 `CombatCameraZoom` 那条 vcam 回写链）。⇒ 表里如实标成「我们代码自己记的，本代理未复核」。
4. **`ScenarioMaterialFader` 的 `doFade = !doFade` 到底会不会取消该淡的那一次** ——
   判据全清（`.c:96/124`），但**必须实测「每条环境调几次 `DoScenarioBlend`」**才能决定。
   计数探针已就位（`BlendCalls`/`BlendCallsForward`），**需要一次 Unity 宿主或真 Play 才有数**。本代理没跑。
5. **`CardEffectsService` 的槽位上限**（我把这一条从账的「等价」改判成 C）—— 上限的**具体数字**
   要读 `Card Display Window` prefab 上 `CardEffectItem[]` 的序列化长度。**本代理没读那件 prefab。**

---

## ⑤ 顺手发现（不属于本批，但该报）

1. **`PlayEffect` 里 PlaySound / ScreenShake 的体是「内联」而不是调 `ResolvePlaySound`/`ResolveScreenShake`** ——
   后者是**原版死代码**。⇒ 将来做 `ObjAnimEffects` 那一族时，**按 `PlayEffect`（:175-220）做，别按那四个
   `Resolve*` 做**（四个里只有两个活着）。这条已经写进本表的 L294/L295 备注。
2. **`SupportMethods.GetTargetAvailableText` 的体是 `"(" + target + ")"`**（`:1494-1502`）——
   账里写「`"(" + 第一个目标卡名 + ")"`」是对的，但要注意它**只加括号**，不做别的格式化。
3. **`BattleCardUI.ShowAvailableTargets` 末句是 `SetDescription(text2 + "<i>" + text + "</i>")`** ——
   `<i>` 是**富文本斜体**；而 **`ActiveSkillDisplay.SetupActiveSkill`（`:196`）用的是空格 + 纯文本**
   （`text7 = 描述 + " " + 后缀`，**无斜体**）⇒ **两处口径不同**，⛔ 别抽成一个共用函数。
   （`CardDisplayWindow` 那一侧我**没核**它用不用斜体。）
4. **`NetBattle.Abort()` 不断链**（只 log + Notice，`_s` 一个字不动）——
   这意味着「状态指纹对不上」那一档今天**两端都还连着、这一局永远悬着**。
   账上写「只报不打分」是对的，但没写「**也不断链**」⇒ 这条在缺口 C 的改法里必须一起解决（`Close(say:false)` 就是它）。
5. **`RuleCore` 里 `u.RemoveAll(KeywordTable.DropPod)` 有 3 处**（`:7670` / `:8369` / `:8426`）——
   将来任何「空投舱」相关的事件/演出都要**三处一起补**，只补 `OpenDropPod` 会静默漏掉两个破舱档。
6. **`EventTiming.cs:501` 把 `EvtKind.TraitRemoved` 登记为「不占时间线」**，理由写的是
   「`BattleDriver` 侧今天**没有任何消费者**（纯表现信号）」—— 缺口 A 一旦接线，**这句就过期了**
   ⇒ 到时候要回来改这句话（同一处还写着「这两个发出点」，接完会变成三个）。

---

## ⑥ 给调度台的请求

### 6·1 本轮**没有改任何 `.cs`** ⇒ **不需要跑 typecheck**，也**不需要跑自检**。

（这是刻意的：三条缺口的落点分别在 `RuleEngine/**` · `BattleDriver.cs` · `Net/**` · `Shell/ItemDrawer.cs` ·
`工具/import_original_sfx.py` —— **全部在本代理白名单之外**，所以只出判据与请求。）

### 6·2 需要调度台亲自跑的一条（Unity 批处理，缺口 B 的第 4 步）

```bash
# 前提：已有代理把 import_original_sfx.py 的 EXTRA_CUES 加上 "ButtonClick 1" 并跑完 --check + 真写
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
  -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod ShellScene.Run -logFile -
```
（`ShellScene.Run` ≈ 9 秒，本机最便宜的宿主；只为刷新 AssetDatabase 把新 wav 导进来。
⚠️ 它会重建 `Shell.unity` ⇒ 按 `CLAUDE.md` §二先留场景副本，跑完 `git diff --numstat` 对一眼。）

### 6·3 需要派出去的 4 个执行件（文件互不相交，可并行）

| # | 谁（白名单归属） | 改什么 | 判据 |
|---|---|---|---|
| 1 | 引擎线（`RuleEngine/**`） | `EffectResolver.EmitTraitRemoved` +1 形参；`RuleCore` 3 处 emit | 缺口文档 §1·3 |
| 2 | F5（`BattleDriver.cs`） | `EvtKind.TraitRemoved && CardId == "droppod"` 一支消费 → `DoSimpleShake(3f/0.5f)` | 缺口文档 §1·3 |
| 3 | 联机线（`Net/**`） | 类尾加 `LeaveWithoutResult`；`case NetKind.Hash` 改走它；注释就地订正 | 缺口文档 §3·4 |
| 4 | Shell + 工具 | `工具/import_original_sfx.py` `EXTRA_CUES` 加 1 行 → 跑导入 → `Shell/ItemDrawer.cs:1971` 改 3 行 | 缺口文档 §2·4 |

### 6·4 需要落账的三条（⛔ 本代理不动 `账/*.tsv`）

1. **`战斗界面_HUD.tsv:57`** 判据出处 `:583-586` → **`:514-517`**（另加 `CardRarityHolderController.cs:13-60`）。
2. **`战斗界面_HUD.tsv:19`** 判据出处「闸在 `:170`」→ **`:160`**（`:172` = `maxTargets==1` 判定）。
3. **`特效动画.tsv` 的 L289/290/291/292/293/294/295 七行** —— `判据要点` 全部串了一格（见 §3·1），
   建议整段重写；并且 `ResolvePlaySound` / `ResolveScreenShake` 要标「**原版死代码，0 调用者**」。
4. **`特效动画.tsv:196`** 状态 `B` + 差异「等价」→ 建议 **`C`**（槽位上限）。
5. **账的「物理行」比文件真行小 1**（§3·5）—— 建议在 `调度台日志` 记一条口径。
