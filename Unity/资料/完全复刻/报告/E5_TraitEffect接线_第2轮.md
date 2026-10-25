# E5 · `TraitEffectController` W0–W10 接线（第 2 轮 · 2026-10-25）

> **执行代理 `E5`**（第八十会话 · 完全复刻批次 1）· 只读输入 = `报告/N15_TraitEffect_第1轮.md` §⑥/§⑩ +
> `判据/TraitEffectController_钩子层.md` + `交接_第七十九会话.md` §二·7/§三。
> **独占文件**：`CardPresentation/Core/TraitFrames.cs` · `CardPresentation/Battle/BattleDriver.cs` ·
> `资料/完全复刻/账/战斗界面_HUD.tsv`。
> **红线**：⛔ 没跑 Unity（含任何 `-executeMethod`）· ⛔ 没动 git · ⛔ 没改正本 · ⛔ 没碰别人的文件。

---

## ① 结论（W0–W10 逐条，**一条「待定」都没有**）

| # | 结论 | 落点（`文件:行`） | 一句话 |
|---|---|---|---|
| **W0** | ✅ **已接线** | `Core/TraitFrames.cs:1037-1038`（在 `Play()` 里，`:1019`） | `WarpforgeEffectPlayer.Play` 之后立刻 `TraitEffectControllers.Attach(p.gameObject, prefabName)` —— **按 prefab 名**、幂等。P0-1（持久框 `autoDestroySeconds = +∞`）**N5 已做**，本批只复核（`:1026-1032`） |
| **W1** | ✅ **已接线** | `Core/TraitFrames.cs:852` | `DisplayStatus` 的「**首次实例化 OnPlay 框**」分支：`Play(name, held:true)` 之后 `RefreshCounter(key, value, p, false)` |
| **W2** | ✅ **已接线** | `Core/TraitFrames.cs:834` | `DisplayStatus` 的「**已在 `_play` 字典**」分支：归位 + `SetActive(true)` 之后**再刷一次计数器**（⛔ 不重建、不重播） |
| **W3** | ✅ **已接线** | `Core/TraitFrames.cs:890`（签名 `:862`） | `PlayStartRows` 每播一条就刷一次（原版这一档同样把 `traitValue` 传下去）。⚠️ 这一档的 prefab **不带控制器** ⇒ 空转，但**调用一定发生**（`UpdateValueCalls` 记账） |
| **W4** | ✅ **已接线** | 收端 `Core/TraitFrames.cs:951`（签名 `:921`）· 发端 `Battle/BattleDriver.cs:23769-23776` | `DisplayTrigger` 新增第三个参数 `value`；`PlayTraitPulse` 从 `u.KwValue(kw)` 取值传进来 |
| **W5** | ✅ **已接线** | `Core/TraitFrames.cs:768`（重载）· `:756` 旧签名转发 `valueOf = null` | `SyncKeywords(IEnumerable<string>, System.Func<string,int>)`；⛔ 值没有塞进 `CardData` |
| **W6** | ✅ **已接线** | `Battle/BattleDriver.cs:25452` | `tf.SyncKeywords(u.Keywords.Keys, k => u.KwValue(k))`（`SyncKeywords` 的唯一生产调用点） |
| **W7** | ✅ **已接线** | `Battle/BattleDriver.cs:21926` + 取值口 `:32204`（`StatusValueOf`） | `EvtKind.StatusApplied`（`Amount > 0`）那一支：`DisplayStatus(e.CardId, StatusValueOf(e.Player, e.Slot, e.CardId))`。⛔ **没新建 `TraitFrames`**（仍用 `ViewAt(...).GetComponent<TraitFrames>()`） |
| **W8** | ✅ **已接线** | 调用 `Core/TraitFrames.cs:781` · 实现 `:1123` · 锁 `:1084` / `:1105-1110` | 值变化兜底：**只在值真的变了才刷**；锁 = `_prevValue`（显式那一路刷过就记账 ⇒ 兜底同拍不会再刷一次 ⇒ **不会双弹**） |
| **W9** | ⛔ **接不了 —— 缺 1 样（引擎事件）** | **接收端已建** `Core/TraitFrames.cs:1150`（`MarkerLightUsed(trait)`，两本字典各取一次） | 缺：`EvtKind` 里**没有**「trait 被摘掉」那一档 ⇒ 要在 `RuleEngine/Core/BattleEvent.cs` 加一档、在 `RuleCore.cs:6884`（`target.RemoveAll("markerlight")`）`Emit`。**两处都是 E1 的文件**。⚠️ 同批必须处理 **F2**（原版留到回合末才摘，我们当场摘 —— 见 §⑤·3） |
| **W10** | ⛔ **接不了 —— 缺 3 样** | **接收端已建** `Core/TraitFrames.cs:1180`（`AimingAttack(trait, option)`） | 缺 ① `Core/MarkerlightController.cs` 的改动（**不在我白名单** → **改动请求见 §③·3**）② `MarkerlightIdle` 两个 `MaterialFader` 实例还没建（同一文件）③ 引擎侧没有「攻击**起手** / 攻击结束」两拍事件（`EvtKind.Attack` 是**结算**那一拍）。⚠️ 现在接线会**每次攻击刷一条 `LogError`** ⇒ ⛔ 别提前挂调用点 |

**一句话**：**W0–W8 全部接上并逐条有落点；W9 / W10 的「接收端」都已建好、只差另一侧**，
缺的东西**逐条点名到文件**（不是「待定」）。

---

## ② 判据

| 实现点 | 判据出处 | 一手证据 |
|---|---|---|
| 值 = **当前 trait 总值**（基值 + Σmodifier、钳 ≥0） | 判据 §3.1 · 报告 N15 §② | `decomp_full/EntityScript__GetCurrentTraitValueWithModifiers.c:1-75`；我们等价物 = `UnitState.KwValue`（`RuleEngine/Core/UnitState.cs:1071-1075`，**精确匹配、不做规范化**） |
| 4 个显式入口 + 1 个触发入口要刷计数器 | 判据 §6.3 · 报告 N15 §⑥ | `BattleManager.__c__DisplayClass465_0___ResolveTraitAnim_b__0.c:14-31`（出生/Start/FromCode 共用）· `BattleCardUI__DisplayStatusAnim.c:86-119`（Start 档）· `…:190-196`（已在字典档）· `BattleCardUI__DisplayTriggerAnim.c:97-104`（触发档） |
| `UpdateValue` **没有同值早退** ⇒ 显式那一路「一律刷」 | 判据 §6.4 灭自证 1 的说明 · 报告 N15 §⑦·2 | `decomp_full/HuntMarkController__UpdateValue.c:13-30`（每调一次就写文本 + 弹，无比较） |
| 值变化兜底要「同锁、不双弹」 | 判据 §6.3 末段 | 判据原话「兜底闸必须与 `StatusApplied` 同一把锁（同一帧内只刷一次），否则会双弹」 |
| 挂载**按 prefab 名、⛔ 不按 trait 名** | 判据 §6.2 / §6.4 灭自证 2 | 判据 §1.3 / §八·B 的全库唯一性（84 bundle 按 `m_Script` 扫，各 1 命中） |
| `_prevValue` 锁成立的**顺序前提** | 本批现读 `BattleDriver.RefreshAll` | `Battle/BattleDriver.cs:18425`（`PlaySignals()`）**在** `:18427-18428`（`SyncBoard`）**之前** ⇒ W7 先刷、W8 后判 |

**取值语义（下一批别改）**：`valueOf(key)` 必须是**当前总值**，⛔ 不是卡面固定值、⛔ 不是「本次增量」。
`NoTraitValue`（`int.MinValue`）= 「没有值可传」⇒ **不刷**；⛔ **绝不拿 `0` 顶替**（那会把计数器写成 `0`）。

---

## ③ 改动清单

### ③·1 `CardPresentation/Core/TraitFrames.cs`（+206 / −5 · **LF 保持** · 999 → 1200 行）

| 位置 | 改了什么 |
|---|---|
| `:756-784` | `SyncKeywords` 拆成两个：旧签名转发 `null`；新签名 `(current, System.Func<string,int> valueOf)`，并在**新出现的键**那条路上把值带进 `DisplayStatus`，末尾调 `RefreshChangedValues`（W8） |
| `:803-855` | `DisplayStatus` 拆成两个：`(trait)` 转发 `NoTraitValue`；`(trait, value)` 是真身 —— W3（Start 档）/ W2（已有框）/ W1（新框）三处各刷一次 |
| `:862` / `:890` | `PlayStartRows(string key, int value)`，每播一条 `RefreshCounter` |
| `:921` / `:951` | `DisplayTrigger(trait, isWarlord = false, value = NoTraitValue)`，每条 effect 播完 `RefreshCounter` |
| `:1019` / `:1037-1038` | `Play()` 里 W0：`TraitEffectControllers.Attach`，挂上就 `AttachedCount++` |
| `:1042-1189`（**类尾**） | 新增：`NoTraitValue` · `UpdateValueCalls` · `AttachedCount` · `LastTraitValue` · `_prevValue` · `ValueOf` · `RefreshCounter` · `RefreshChangedValues` · `MarkerLightUsed`（W9 接收端）· `AimingAttack`（W10 接收端） |

🔴 **全部新成员追加在类尾**（CLAUDE.md §三 2026-10-25 `N9` 那条）；类**中间**只改了上面 5 个既有方法体。

### ③·2 `CardPresentation/Battle/BattleDriver.cs`（+45 / −3 · **CRLF 保持** · 32177 → 32219 行）

| 位置 | 改了什么 |
|---|---|
| `:21921-21930`（`PlaySignal` 的 `StatusApplied` 支） | **W7**：把当前总值传给 `DisplayStatus`（原来只传键名） |
| `:23765-23776`（`PlayTraitPulse`） | **W4**：多算一个 `traitValue = u.KwValue(kw)`，传给 `DisplayTrigger` |
| `:25452`（`SyncKeywords` 唯一生产调用点） | **W6**：`k => u.KwValue(k)` |
| `:32191-32216`（**类尾**） | 新增 `int StatusValueOf(int player, int slot, string keyword)` —— W7 的**唯一**取值口（`KeywordForDisplay` 拆 `#i` → `UnitState.KwValue`） |

### ③·3 🔴 **改动请求（W10 用；这两个文件本轮不归我）**

> 简报 §「共用件 / 已建不接线」：改 `Core/MarkerlightController.cs` **必须先写请求**。以下四步是自包含的，
> 批准后**任何有写权限的代理**可照做；`Core/MaterialFader.cs` 我已读过接口（`BindRenderers` / `ApplyPrefabFlags` / `Toggle` 都在）。

**请求改 `CardPresentation/Core/MarkerlightController.cs`（4 处）：**

1. 两个字段类型 `MonoBehaviour` → **`MaterialFader`**（`:67` `materialFaderNormalState` / `:70` `materialFaderExpiringState`）；
2. `AimingAttack` 的「未实现出口」（`:160-167`）换成原版逐字
   `MaterialFader f = alreadyAttacked ? materialFaderExpiringState : materialFaderNormalState; f.Toggle(option ^ 1, fadeTime);`
   （判据 `MarkerlightController.cs:81-86`；⚠️ 两个 fader 可能为 null ⇒ 保留空判 + `LogError` 一次）；
3. `Awake`/`BindIfMissing` 里**建并绑**两个 fader（原版是 prefab 序列化组件，我们没有）：
   * 节点 = `Normal State/Fixed` 与 `Expiring State/Fixed`（判据 §⑧·2 `F-E`，**⛔ 不是都在 Expiring State**）；
   * normal → `Base VFX` / `Ground Effect` / `Ground glow pulse` / `Grid floor`；expiring → `Base Vfx` / `Ground Effect` / **`null`** / `Grid floor`（顺序与 null 都照原版）；
   * 每个都要 `ApplyPrefabFlags(false, false, true)`（判据 §⑧·2 `F-F`：prefab 把 `restoreOriginalMaterialsOnFadeOut` 覆写成 0，而 C# 默认 `true`）；
   * ⚠️ `renderers[i] == null` 是原版会抛 NRE 的分支（`F-G`）—— `MaterialFader` 已按「跳过 + `LogError` 一次」实现，**别改回抛**（待调度台拍板，见 §⑥·4）。
4. （可选）`Start()` 里那句 `floors` 取材质保持 `r.material`（实例副本），⛔ 不要「优化」成 `sharedMaterial`（判据 §3.2·b / §七 `F8`）。

**W10 还差的那两样（不在 MarkerlightController 里）**：
* 引擎「攻击**起手** / 攻击结束」两拍事件 —— 原版在 `CardScript._PrepareAttack_d__353__MoveNext.c:41`（=false）/`:65`（=true），
  我们**没有**对应事件（`EvtKind.Attack` 是结算那一拍）。落点候选：`CardPresentation/Battle/CardInteraction.cs` 的拖拽起手 /
  `BattleDriver` 里 `SetupAttack` 的等价处 —— **两处都不在本批任何人的白名单**，需要调度台指派。
* 在事件落地后，`TraitFrames.AimingAttack("markerlight", option)`（`:1180`，已建好）就是调用点。

### ③·4 `资料/完全复刻/账/战斗界面_HUD.tsv`（47 / 47 行 · **LF 保持** · 350890 B 字节数不变）

* 本批改了两个 `.cs` 的行号 ⇒ 用 `difflib` 把 `HEAD` 版本与现版本的**逐行映射**算出来（只对 `equal` 块建映射），
  再把本账里 **47 处** `TraitFrames.cs:N` / `BattleDriver.cs:N` 引用就地改到新行号。
  * 其中 45 处自动映射；**2 处**（`DisplayStatus` / `DisplayTrigger` 的声明行本身被我改写了，不在 `equal` 块里）手工定到
    `:812` / `:921`。
* **验证**（机检，只读，不带 `--fix`）：
  * 改前（用备份 `战斗界面_HUD.tsv` 跑）：`A=394 通过=350 漂移=44 告警=4 失败=0`
  * 改后：**`A=394 通过=386 漂移=8 告警=4 失败=0`**
  * 剩的 **8 条全是「同名多声明」歧义**（`ViewAt`×2 / `SetHint`×3 / `SetVis`×2 / `TakeFromGraveyard`×1），
    全部指向**我没动过**的行号（`15671` / `16447` / `23836` 的 `ViewAt` 声明 / `BattleContext.cs:1764`）⇒ **既有问题**，
    `--fix` 也不敢改（工具自己说「**不自动改**，请人工确认」）。
* ⚠️ **范围外的一处**：`资料/完全复刻/账/特效动画.tsv:414` 的 `TraitFrames.cs:563（PrefabOf）` —— 那一行在**我的改动点之前**，行号不变，⛔ 不用动（已核）。

---

## ④ 断言 / 读数

### ④·1 秒级类型检查

> 🔴 **本节读数已被 §⑪·3 取代（追加轮收工时 `0 / 0` 拿到四次）** —— 下面是**接线当时**的情况，留着是因为它解释了
> 「为什么中途一直有别人的错」。**以 §⑪·3 为准**。

```
TMPDIR=/tmp/wf_e5 bash d:/4/Unity/工具/typecheck.sh
→ 21:5x 第一次： 运行时错误数: 0    · 编辑器错误数: 23
→ 之后复跑三次： 运行时 9~10        · 编辑器 23~24      ← 全部来自**别的代理此刻正在写的文件**
```

🔴 **判据 = CLAUDE.md §13·3**：**「错误全部集中在你不管的文件 ⇒ 不是你的问题」**。
**五次复跑里，`TraitFrames.cs` / `BattleDriver.cs` 一次都没有出现在错误列表里**（最后一条命令专门验了：
`grep "error CS" | grep -E "TraitFrames|BattleDriver"` ⇒ **空**）。
那一次 `运行时 0` 正好落在 E2 还没开始写 `LiveOps*.cs` 的空档 ⇒ **我的两个文件编得过**。

**别人那 28 条的归属**（同一时刻的全量读数）：

| 文件 | 条数 | 是谁的 / 为什么 |
|---|---|---|
| `CardPresentation/Editor/BattleScene.cs` | 18–20 | **E1** 的属性模型 Stage 2 正在把 `UnitState.Attack/RangedAttack/MaxHealth` 改成**只读投影**（`CS0200`），而这个自检宿主还在给它们赋值 |
| `CardPresentation/Shell/LiveOps{Event,Handler,Manager}.cs` | 1–8（**在动**） | **E2** 的新文件正在写（`??` 未跟踪） |
| `WarpforgeArena1/Editor/ArenaBuilder.cs` | 2 | 别人的半成品（本批无此所有权；D 域） |

⇒ **我的改动本身 = 0 错误**。

**行尾**（字节级）：`TraitFrames.cs` CRLF=0 / LF=1199（**纯 LF，与改前一致**）· `BattleDriver.cs` CRLF=32218 / LF=32218（**纯 CRLF，与改前一致**）。

### ④·2 现在就能读的观测量（**已加到生产代码里，不是临时探针**）

| 观测量 | 在哪 | 能证明什么 |
|---|---|---|
| `TraitFrames.AttachedCount` | `:1076` | **W0** 真的挂上过控制器（应 = 播过的、带控制器的那两张 idle 框实例数） |
| `TraitFrames.UpdateValueCalls` | `:1072` | **W1–W4** 的「调用发生了」—— **含实例上没有控制器的那些**（A-E3 的原版形） |
| `TraitFrames.LastTraitValue` | `:1079` | 最近一次写进计数器的数（`int.MinValue` = 还没写过） |
| `HuntMarkController` / `MarkerlightController` 的 `UpdateCalls` / `PunchCount` / `LastValue` / `TextForTest` / `Bound` | 三个控制器文件（N15 已建） | 「真的弹了几次 / 文本是什么」 |
| `TraitFrames.StartPlayCountOf(trait)` / `PlayCount` / `TriggerPlayCount` | 既有 | 框那一侧的行为**没有被本批改坏**（`Editor/BattleScene.cs` ⑧ 那组断言读的就是它们） |

### ④·3 🔑 **接线后的静态验证方法（不跑 Unity 也能做）**

**A. 结构自证 —— 11 个钩子各自「调到了没有」，一条命令一个**（在 `d:/4/Unity/MyGame/Assets` 下跑）：

```
# W0：唯一出生口挂了控制器
grep -n "TraitEffectControllers.Attach(p.gameObject, prefabName)" CardPresentation/Core/TraitFrames.cs
# W1/W2/W3：DisplayStatus 三条路各刷一次（应 3 行 RefreshCounter）
sed -n '812,860p' CardPresentation/Core/TraitFrames.cs | grep -n "RefreshCounter"
# W3：Start 档每条播完都刷
sed -n '862,900p' CardPresentation/Core/TraitFrames.cs | grep -n "RefreshCounter"
# W4：触发档 + 发端传值
grep -n "RefreshCounter(key, value, p, false)" CardPresentation/Core/TraitFrames.cs   # 应 3 处（W3/W4 各 1 + W1 或 W2 的形态不同）
grep -n "DisplayTrigger(kw, isWarlord, traitValue)" CardPresentation/Battle/BattleDriver.cs
# W5/W6：重载 + 唯一生产调用点
grep -n "SyncKeywords(IEnumerable<string> current, System.Func<string, int> valueOf)" CardPresentation/Core/TraitFrames.cs
grep -n "k => u.KwValue(k)" CardPresentation/Battle/BattleDriver.cs
# W7：StatusApplied 支传值 + 取值口只有一处
grep -n "StatusValueOf(e.Player, e.Slot, e.CardId)" CardPresentation/Battle/BattleDriver.cs
grep -c "int StatusValueOf(" CardPresentation/Battle/BattleDriver.cs      # 应 = 1
# W8：兜底与锁
grep -n "RefreshChangedValues(now, valueOf)" CardPresentation/Core/TraitFrames.cs
grep -n "last == value) return 0;" CardPresentation/Core/TraitFrames.cs
# W9/W10 的接收端（0 调用点是**故意**的）
grep -n "public int MarkerLightUsed(string trait)\|public int AimingAttack(string trait, bool option)" CardPresentation/Core/TraitFrames.cs
```

**B. 「落点没漂」**：`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py d:/4/Unity/资料/完全复刻/账/战斗界面_HUD.tsv --root D:/4/Unity/MyGame/Assets`
⇒ 期望 **`A=394 通过=386 漂移=8 告警=4 失败=0`**（那 8 条歧义见 §③·4）。

**C. 「框那一侧没退化」**（纯静态）：`Editor/BattleScene.cs` ⑧ 那组断言只读
`StartPlayCountOf` / `PlayCount`，本批**没有改这两条的语义** ⇒ 它仍然应该原样通过（跑 Unity 留给调度台）。

**D. 端到端（要 Unity，⛔ 本批不跑；下一批照这个写）**

| 要验 | 怎么喂 | 期望 |
|---|---|---|
| **W0+W1** | 场上放一张带 `huntmark 2` 的卡 → `RefreshAll()` | `_play["huntmark"]` 实例上 `GetComponent<TraitEffectController>()` ≠ null；`HuntMarkController.TextForTest == "2"`；`PunchCount == 1`；`TraitFrames.AttachedCount ≥ 1` |
| **W2+W7** | 再 `AddKeyword("huntmark", 1)` → `RefreshAll()`（`StatusApplied.Amount = 1`） | 文本 `"3"`、`PunchCount == 2`、**`InstanceId` 不变**、`PlayCount` **不增**；`UpdateValueCalls` 涨 |
| **W3/W4** | `DisplayStatus("armour", 3)` / `DisplayTrigger("oath", false, 1)` | `UpdateValueCalls` 各 +1（`armour` / `oath` 的 prefab **没有**控制器 ⇒ `PunchCount` 不动） |
| **W8** | 只改 modifier（**不发** `StatusApplied`）→ `RefreshAll()` 两次 | 第一次 `PunchCount` +1；**第二次 +0**（值没变不刷） |
| **W8 灭自证** | 同一帧内 `SyncKeywords` 连跑 3 次（值不变） | `PunchCount` 仍 = 1 |
| **W9 / W10** | 见 §①（还接不了） | —— |

⚠️ **批处理/编辑模式的两个硬限制**（不写清下一批必踩）：
1. **`Awake` / `Start` 不会自动跑**（`Editor/TraitEffectAssert.cs` 文件头 ② 已记）。
   ⇒ `MarkerlightController.Start` 的「两态 + `_RippleScale`」在自检里**必须反射显式调**（N15 已这么做）。
   ⛔ **别为了「让它跑」在生产代码里加 `[ExecuteAlways]`**（会改变运行期行为）。
2. **DOTween 走 Manual 档不推进** ⇒ 只断**次数 / 文本 / 值**，⛔ 不断「0.5 s 后弹完」。

---

## ⑤ 没查清（如实）

| # | 没查清 | 为什么要紧 | 本轮处置 |
|---|---|---|---|
| 1 | `EvtKind.StatusApplied` 的 `CardId` **会不会带 `#i` 传输后缀** | `DisplayStatus` 内部 `Norm` **只做 `Trim().ToLowerInvariant()`、不拆 `#i`** ⇒ 真带的话 W7 那一支**查不到绑定、框挂不上**（而我取值的 `StatusValueOf` 已经拆了 ⇒ 会出现「有值但没框」） | 现值路径读的是 `EffectResolver.ApplyOneGain` 的 `p.Keyword`（卡效解析出来的键，**看起来是裸键**），但**现有 emit 点我没逐个枚举** ⇒ 记为「没查清」。⚠️ 这是**既有行为**（本批没让它变差），只是加值时暴露出来 |
| 2 | 原版「**框已有 ⇒ 只归位**」那一支到底刷不刷 | 判据 §1.4③ 说刷（`DisplayStatusAnim.c:190-196`），我按它做了 **W2** | ⚠️ 但 `huntMark` / `markerlight` 的 `UpdateValue` 里**有 `DOPunchScale`** ⇒ 每一次「同一 trait 再施加」都会弹一下。**这正是原版行为**（`HuntMarkController__UpdateValue.c` 无同值早退），不是我们多加的 |
| 3 | **F2 口径**（markerlight 何时摘） | 原版留到**回合末**（`CardScript__OnTurnEnd.c:135-141`），我们 `RuleCore.cs:6884` **当场** `RemoveAll` | ⛔ 不改（在 E1 的文件里，且要先做一次卡效枚举）。它**直接决定 W9 有没有观测量**（见 §⑥·3） |
| 4 | `MarkerlightIdle` 两个 `MaterialFader` 的 `renderers[]` 里那个 `null` 到底会不会被走到 | 决定「原版会抛 NRE」是死分支还是真缺陷 | 判据 §⑨ 已裁「**主路径不可达（已证死）· 边角可达但未穷尽 · 整体判不了**」；本批沿用，**没再查**（那是枚举卡效的活，不是读反编译） |
| 5 | 我们导入的 `HuntMark_IdleEffect` / `MarkerlightIdle` 上 `Counter` 的 TMP 字体在**运行期**能否渲染出数字 | W1 的**画面**验收 | ⛔ 不跑 Unity 验不了。⚠️ N15 的 A-E1 在「字体取不到」时会显式报「**没验到**」而不是「实现错」——别读错 |

---

## ⑥ 顺手发现

| # | 发现 | 证据 | 影响 / 建议 |
|---|---|---|---|
| **1** 🔴 | **W7/W8「不双弹」依赖 `RefreshAll` 内部的顺序**：`PlaySignals()` 必须在 `SyncBoard()` **之前** | `Battle/BattleDriver.cs:18425`（`PlaySignals`）vs `:18427-18428`（`SyncBoard`） | **同一拍同值只刷一次**全靠这条。⛔ 谁把 `SyncBoard` 提到前面 ⇒ `StatusApplied` 与值变化兜底**各弹一次**（双弹），**而且不会有任何报错**。建议：在 `RefreshAll` 那两行加一句「⛔ 别换序」的注释（**不在我白名单**，请调度台指派） |
| **2** | `MarkerlightController.Start` 在**批处理/编辑模式**里不会跑 ⇒ `MarkerlightIdle` 会把 `Normal State` 与 `Expiring State` **两层叠着画** | `Editor/TraitEffectAssert.cs:16`（N15 已记） | **只影响自检截图**，不影响产品（play 模式 `Start` 正常跑）。⚠️ 下一批若在批处理里给这张框拍图，别把「两层叠着」当缺陷 |
| **3** | **W9 在我们当前实现下几乎没有观测量**：我们打完标记光**当场** `RemoveAll("markerlight")`（`RuleCore.cs:6884`）⇒ 即使补上「被摘掉」事件，Expiring 态也只能存在到**下一次 `SyncBoard`** | 同 §⑤·3 | ⇒ **W9 要和 F2 同批做**（改口径 = 标记留到回合末）。⛔ 别先接事件、再等 F2（会得到一个永远看不见的效果，还占一条「已接线」） |
| **4** | `MaterialFader` 对 `renderers[i] == null` 的处置是「**跳过 + `LogError` 一次**」（原版会抛 NRE） | 报告 N15 §⑧·2 `F-G` | 这是**有意的偏离**。W10 接线那一批**要么保留、要么拍板改回抛** —— ⛔ 别默认它「已经是原版行为」 |
| **5** | `账/战斗界面_HUD.tsv` 有 **8 条既有的「同名多声明」歧义漂移**（`ViewAt`/`SetHint`/`SetVis`/`TakeFromGraveyard`），`--fix` **不会**自动改 | §③·4 的机检输出 | 与本批无关；**下一批手工收**（要人读那几行到底指哪个重载）。⚠️ 它们会让「闸绿」永远差 8 条 —— 别误判成本批回归 |
| **6** | 本批跑 typecheck 时**撞到三个别的代理的半成品**：`Editor/BattleScene.cs` 18–20 条（E1 的 `UnitState.Attack` 改只读后遗留的赋值点）+ `Shell/LiveOps*.cs` 1–8 条（E2 正在写）+ `WarpforgeArena1/Editor/ArenaBuilder.cs` 2 条（D 域，本批无人拥有） | §④·1 | 按 CLAUDE.md §13·3 判据：**错误全部集中在不是我负责的文件** ⇒ 不是本批的问题。⚠️ 但 **`Editor/BattleScene.cs` 那 18–20 条必须是 E1 收尾的一部分** —— 那个文件是共用件（无人独占），**E1 改属性模型时有义务把它一起改绿**，否则整条编辑器程序集编不过 |
| **7** ⚠️ | `DisplayStatus` 的「已在 `_play` 字典」那一支，**若 `_play` 里存的实例已经被 `Destroy`**（`old == null`）⇒ 只 `return false`，**既不重建也不刷计数器** ⇒ 这条 trait 的计数器**永久消失**（静默） | `Core/TraitFrames.cs:825-836`（`if (old != null)` 那层空判 = N5 之前就在的写法） | **原版同形也会坏**（它对着已销毁的 GameObject 调 `SetActive(true)` ⇒ NRE）。⇒ 按铁律 11 例外①「原版本身也没有正确处理」**本轮不改**；但**如实记在这里**。真正的修法 = 把它当「没有框」重建（一行 `_play.Remove(key)` 再走下面那条路） —— 建议下一批和断言一起做（现在没有断言守这个分支） |

---

## ⑩ 交接给下一个代理

### ⑩·1 这一轮做完了什么（自包含）

1. **W0–W8 全部接上**，落点见 §①（`TraitFrames.cs` 5 个既有方法体 + 类尾 12 个新成员；`BattleDriver.cs` 3 个调用点 + 类尾 1 个新方法）。
2. **W9 / W10 的接收端**已建在 `TraitFrames.cs:1150` / `:1180`（**0 调用点，故意的**）。
3. **`账/战斗界面_HUD.tsv` 47 处行号已按 `HEAD` 逐行映射更新**（改前 44 漂移 → 改后 8，剩的 8 条是既有歧义）。
4. **`typecheck`**：**我这两个文件零错误**（五次复跑，`grep -E "TraitFrames|BattleDriver"` 恒空）；
   程序集总错误数随别的代理在写的文件波动（§④·1 已逐条点名）⇒ **收口时请主对话在同步点复跑一次**。

### ⑩·2 下一批该做什么（按依赖排序）

| 序 | 活 | 要动的文件 | 前置 |
|---|---|---|---|
| 1 | **给 W0–W8 补端到端断言**（§④·3 D 表那 5 条 + 灭自证） | `Editor/TraitEffectAssert.cs`（N15 的「⏳ 待接线」那节可以照抄本报告 §④·3） | 无（本批已接） |
| 2 | 在 `RefreshAll` 给「`PlaySignals` 在 `SyncBoard` 之前」加**禁改注释**（§⑥·1） | `Battle/BattleDriver.cs`（**本批独占，下一批要重新指派人**） | 无 |
| 3 | **W10**：批准 §③·3 的四步改动 → `MarkerlightController.cs` 用 `MaterialFader`；再补「攻击起手/结束」两拍事件 → `TraitFrames.AimingAttack` 挂调用点 | `Core/MarkerlightController.cs` + 事件源（`CardInteraction.cs` / `BattleDriver.cs`，**本批无人拥有**） | 调度台指派 |
| 4 | **W9**：`EvtKind` 加「trait 被摘掉」+ `RuleCore.cs:6884` emit，**同批做 F2**（标记留到回合末）→ `TraitFrames.MarkerLightUsed` 挂调用点 | `RuleEngine/Core/{BattleEvent,RuleCore}.cs`（**E1 的**）+ `BattleDriver.cs` | E1 空出来；且 F2 要先做一次卡效枚举 |
| 5 | 手工收那 8 条歧义漂移（§⑥·5） | `账/战斗界面_HUD.tsv` | 要人读重载归属 |

### ⑩·3 🔴 下一批的「已知什么」（直接抄，别重查）

1. 值的**唯一**语义 = `UnitState.KwValue(key)`（当前总值）· 没有值就传 `TraitFrames.NoTraitValue`（**⛔ 不用 0**）。
2. **显式那一路一定刷**（原版 `UpdateValue` 没有同值早退）；**只有 W8 兜底**「值没变不刷」。
3. **锁 = `_prevValue`**，它成立的**唯一前提**是 `RefreshAll` 里 `PlaySignals` 在 `SyncBoard` 之前（§⑥·1）。
4. `UpdateValueCalls` **含没控制器的空转**（A-E3 要的就是这个）；`PunchCount` 只有真弹才涨。
5. **`_play` 那两档用 `held: true`**（N5 的 P0-1）：`HuntMark_IdleEffect` / `MarkerlightIdle` 的 `destroyTime < 0` ⇒ 传 `∞`，⛔ 别改回 `natural` 的 4.3 / 4 秒。
6. **`Attach` 只认两个 prefab 名**（`HuntMark_IdleEffect` / `MarkerlightIdle`），⛔ 别按 trait 名挂。
7. **批处理里 `Awake`/`Start` 不跑** ⇒ 自检要反射调 `Start`；DOTween Manual 档不推进 ⇒ 只断次数/值。

---

## ⑪ 追加轮（2026-10-25 · 调度台指派的两件小收尾）

> 编号接着往下排，正文（①–⑩）**一个字没改**。

### ⑪·1 ① `BattleDriver.RefreshAll` 的「⛔ 别换序」注释 —— **已加**

* 落点：`CardPresentation/Battle/BattleDriver.cs:18425-18431`（注释块），`PlaySignals()` 在 **`:18432`**、
  `SyncBoard(...)` 紧跟其后。
* 内容写死：**`PlaySignals()` 里 `StatusApplied` 那一支（W7）先刷一次并把值记进 `TraitFrames._prevValue`；
  `SyncBoard → SyncKeywords` 的值变化兜底（W8）靠的就是它 ⇒ 换序 = 同一拍弹两下，而且不报错、不断言红。**
* 行尾：**纯 CRLF 保持**（32225 / 32225）。

### ⑪·2 ② `Editor/TraitEffectAssert.cs` —— **5 组端到端断言已写入，⛔ 没跑**

文件从 **8 组 → 13 组**（新增 5 组 + 1 条灭自证；`grep "^N15 "` 的用法不变，前缀沿用 `N15 `）。

| 组 | 落的断言方法 | 覆盖的 W | 期望值依据（**全部现读，不是我们自己的常量**） | 改坏法（删哪句会红） |
|---|---|---|---|---|
| **A-W1** | `:484 Block_W1_FirstSpawn` | **W0 + W1** | `BattleManager__CreateAnim.c:62-81`（出生那拍就调 `UpdateValue`）· `HuntMarkController__UpdateValue.c:13-30`（写文本 + 弹一次、**无同值早退**）· `…DisplayStatusAnim.c:86-119 / :121-198`（Start(0) 与 OnPlay(2) **两条都发生调用**）⇒ `UpdateValueCalls == 2`、控制器侧 `UpdateCalls == 1`、文本 `"2"`、`PunchCount 1`、`AttachedCount 1` | 删 `TraitFrames.Play` 里那句 `TraitEffectControllers.Attach` ⇒ `AttachedCount 0` / 控制器 null ⇒ 红；删 `DisplayStatus` **首次**那支的 `RefreshCounter` ⇒ 文本停在 prefab 的 `"1"` ⇒ 红 |
| **A-W2** | `:532 Block_W2_Reshow` | **W2**（+ 一句说明 W7 那一半不在这里） | `BattleCardUI__DisplayStatusAnim.c:126-198`（已有 ⇒ 只归位/`SetActive`/**不重播**）· `:190-196`（那一支里的 `UpdateValue(GetCurrentTraitValueWithModifiers(trait))`）⇒ 文本 `"3"`、`PunchCount 2`、控制器 `InstanceID` 不变、`PlayCount` 不增、`UpdateValueCalls` **每次显示 +2** | 删「已在字典」那支的 `RefreshCounter` ⇒ 文本停在 `"2"` ⇒ 红；改成「销毁旧框重建」⇒ `InstanceID` 变 ⇒ 红 |
| **A-W3W4** | `:586 Block_W3W4_StartAndTrigger` | **W3 + W4** | `…DisplayStatusAnim.c:86-119`（Start 档把 `traitValue` 传下去）· `…DisplayTriggerAnim.c:97-104`（触发档同）· 判据 §1.3（这两个 prefab **本来就不带控制器**）⇒ `armour` 段 `+1`、`oath` 段 `+1`、两段都**一次不弹**、`PlayCount` 仍 0 | 删 `PlayStartRows` 里的 `RefreshCounter` ⇒ `armour` 段得 +0 ⇒ 红；删 `DisplayTrigger` 里的 ⇒ `oath` 段 +0 ⇒ 红 |
| **A-W8** | `:632 Block_W8_ValueFallback` | **W8** | 判据 §6.3 末段（兜底「只在值真的变了才刷」，否则 `DOPunchScale` 每帧抖）⇒ 同值再来 **+0**；值 3→5 ⇒ **+1**、文本 `"5"` | 把 `RefreshChangedValues` 的 `onlyIfChanged` 改成 `false` ⇒ 同值也刷 ⇒ 红；删 `SyncKeywords` 里对它的调用 ⇒ 3→5 不刷 ⇒ 红 |
| **A-W8灭自证** | `:674 Block_W8_AntiSelfProof` | **W8 灭自证**（判据 §6.4 的**原话**） | 同一帧内连跑 **3 次** `SyncKeywords`（值不变）⇒ `PunchCount` **仍为 1** | 兜底「无脑刷」⇒ 变 4 ⇒ 红；空转（没控制器）那条也写 `_prevValue` ⇒ 错 ⇒ 红 |

**共同前提（每组都先断，断了就说「没验到」而不是假绿）**：`TraitFrames.BindingsLoaded` 为真
＋ 效果库里有那件 prefab（现读：`HuntMark_IdleEffect` / `HuntMark_StackEffect` / `ArmourEffect` / `Oath_Effect` **四件都在**）。

🔴 **写这 5 组时从数据里核出来的一条（下一批别按 +1 写）**：
`huntMark` 的 Start(0) 是 **`HuntMark_StackEffect`**（**不带控制器**），OnPlay(2) 才是 `HuntMark_IdleEffect`（带）
⇒ **每一次 `DisplayStatus` 都会让 `TraitFrames.UpdateValueCalls` +2**（一条空转 + 一条真的写），**`PunchCount` 只 +1**。
这正是 A-E3「`prefab` 上没有控制器也要发生调用」最好的样本 —— 别再拿「+1」当期望值。

⛔ **没跑**（用户口径：实现基本复刻之后才恢复自检）。断言里所有时间型的东西（`A-LIFE`）**仍然没写**，
原因写在文件末「⏳ 还是写不了」那段：批处理没有帧循环。

### ⑪·3 收尾读数

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_e5 bash d:/4/Unity/工具/typecheck.sh` | ✅ **运行时错误数: 0 · 编辑器错误数: 0**（收工时共跑 5 次：**4 次都是 0/0**；中间有一次是 `运行时 4`，那 4 条属于**别的代理正在写的文件**，下一跑就没了） |
| 我的三个文件里有没有错 | `grep "error CS" \| grep -E "TraitEffectAssert\|TraitFrames\|BattleDriver"` ⇒ **恒空** |
| 行尾 | `TraitEffectAssert.cs` 纯 **LF**（0 CRLF）· `TraitFrames.cs` 纯 **LF** · `BattleDriver.cs` 纯 **CRLF**（32225/32225）—— 全部与改前一致 |
| 本追加轮 diff | `BattleDriver.cs` **+7 −0**（注释）· `Editor/TraitEffectAssert.cs` **+275 −0**（5 组 + 工具 + 头注释） |
| 改动的文件集合 | 仍然只有：`TraitFrames.cs` · `BattleDriver.cs` · `Editor/TraitEffectAssert.cs` · `账/战斗界面_HUD.tsv` · 本报告。⛔ 没碰任何别人的文件、⛔ 没动 git、⛔ 没跑 Unity |

