# B94 · R-H · 死亡回合谓词 VA 复核（`HasDiedThisTurn` / `HasDiedSinceLastTurn`）

> 只读代理 R-H（第九十四会话 · 波 1.5）· 2026-10-29
> 触发：F4 报「`decomp_full` 的体看起来是 `diedTurn == curTurn + 1`，而账里判据要点写的是 `curTurn - 1`，**符号相反**」。
> 结论先说：**三份产物方向完全一致，没有任何一份是反的；账上的判据要点是对的。**
> 「符号相反」是**代数误读** —— `turnDied + 1 == turnCounter` **就是** `turnDied == turnCounter - 1`，
> 那个 `+1` 加在 **`turnDied`** 上，不是加在 `turnCounter` 上。
> ⛔ 本代理**未改**任何源码 / 账 / 正本。临时脚本在 `D:/4/_tmp_view/rh_va/`（`va.py` / `freq3.py` / `filter.py`）。

**二进制口径**：`D:/2/unity_run_ref/GameAssembly.dll`（md5 `4d4ba80d786cc763cc520da3e7769cfe`，76,765,184 B）。
⚠️ 本机三份 `GameAssembly.dll`（`unity_run_ref/` · `10.13游戏/` · `Warhammer 40k Warpforge/`）**md5 完全相同**，是同一个 build，不存在「换了个包」的歧义。
地址换算式：`RVA = VA − 0x180000000` → 用 **PE 段表**（`SizeOfOptionalHeader` 决定段表起点，PE32+ 是 240 不是 224）换文件偏移。
`0x1834xxxxx` 这一族落在 **`.rdata`**（`VA 0x34B2000` 起）。

---

## 一、两条谓词的最终判据

### 1.1 `CardScript.HasDiedThisTurn()`

```
RVA 0x5EF960 · VA 0x1805EF960 · file_off 0x5EE360（dump.cs:36764）
```

| 机器码（VA） | 含义 |
|---|---|
| `0x5EF989  mov rax, [rbx+0x258]` | `rax = this.BattleMgr`（参数为 0 时后面 `call 0x1803f47a0` = 抛 NRE） |
| `0x5EF990  mov edi, [rbx+0x250]` | `edi = this.turnDied`（**逐卡**字段） |
| `0x5EF9A2  movups xmm0, [rax+0x3f8]` / `0x5EF9A9 mov ebx,[rax+0x408]` | 取 `BattleMgr.turnCounter` 的 `ObscuredInt`（16 B 密文块 + `fakeValueActive` 在 `+0x408`，共 `+0x3F8..+0x40B`） |
| `0x5EF9D7  call 0x180579e40` | `ObscuredInt.op_Implicit`（**解密**，RVA 0x579E40） |
| `0x5EF9E1  cmp edi, eax` · `0x5EF9E3 sete al` · `ret` | `return turnDied == turnCounter;` |

**⇒ 真语义：`turnDied == turnCounter`（纯等号，没有 ±1）。**

### 1.2 `CardScript.HasDiedSinceLastTurn()`

```
RVA 0x5EF850 · VA 0x1805EF850 · file_off 0x5EE250（dump.cs:36767）
```

| 机器码（VA） | 含义 |
|---|---|
| `0x5EF879..0x5EF8D0` | 同上的解码：`rdi = this.turnDied`，`eax = BattleMgr.turnCounter`（解密后） |
| `0x5EF8DA  cmp edi, eax` · `0x5EF8DC jne 0x1805EF8EB` · `0x5EF8DE mov al,1` · `ret` | `if (turnDied == turnCounter) return true;` |
| `0x5EF8EB..0x5EF939` | **重新**取一遍 `this.BattleMgr` / `this.turnDied` 并**再解密一次** `turnCounter`（源码里 `battleMgr.turnCounter` 写了两次，编译器没合并） |
| `0x5EF943  lea ecx, [rdi + 1]` | `ecx = turnDied + 1` ← **`+1` 加在 `turnDied` 上** |
| `0x5EF946  cmp ecx, eax` · `0x5EF948 sete al` · `ret` | `return (turnDied + 1) == turnCounter;` |

**⇒ 真语义：`turnDied == turnCounter || turnDied + 1 == turnCounter`**
（等价写法：`turnDied == turnCounter || turnDied == turnCounter - 1` —— **本回合死的、或上一个回合死的**）

### 1.3 两个字段的写入 / 初值（判「活卡」边界要用）

- **`turnDied`（`CardScript +0x250`，`int`）**
  - 写点①（**生产**唯一写点）：`decomp_full/CardScript__TriggerUnitBacklashActions.c:75-82`
    —— `turnDied = ObscuredInt.op_Implicit(battleMgr.turnCounter)`，即**临死那一刻把当前回合号烙上去**。
  - 写点②：`decomp_full/CardScript__set_turnDied.c:5`（属性 setter，`*(int*)(p+0x250)=param_2`）。
  - ⛔ **`.ctor` / `CardSetup` 都不写 `+0x250`**（两文件里 `0x250` 0 命中）⇒ **没死过的卡 `turnDied == 0`**。
  - 全库 `grep "+ 0x250) ="` 的命中里，只有上面两处属于 `CardScript`（其余是别的类同名偏移）。
- **`turnCounter`（`BattleManager +0x3F8`，`ObscuredInt`）**
  - 初值 **0**：`decomp_full/BattleManager__SetupInitialState.c:122`（`ObscuredInt 0`）。
  - 递增：`decomp_full/BattleManager._NextTurn_d__395__MoveNext.c:164-175`
    —— `ObscuredInt.op_Increment` 之后写回 `+0x3F8..+0x408`（**回合推进时 +1**）。
    ⚠️ ILSpy 只露出 3 个写点（`BattleManager.cs:14926/15259/20696`），**这条递增被 ILSpy 丢了**（`.c` 里才有）。

---

## 二、三份产物逐份对照 —— 谁对谁错

| 产物 | `HasDiedThisTurn` | `HasDiedSinceLastTurn` | 方向判对了吗 | 备注 |
|---|---|---|---|---|
| **VA 机器码**（本轮，权威） | `turnDied == turnCounter` | `== turnCounter \|\| turnDied+1 == turnCounter` | ✅ | `sete`/`lea+1` 逐条可读，见 §一 |
| **`decomp_full/*.c`**（Ghidra） | `CardScript__HasDiedThisTurn.c:32` `return iVar1 == iVar4;` | `:32` `return true;`（`iVar1==iVar4` 分支）· `:48` `return iVar1 + 1 == iVar4;` | ✅ **对** | Ghidra 把 `ObscuredInt` 的 5 个字段复制进栈、再调 `op_Implicit` 解出 —— 和机器码逐句对得上 |
| **`cpp2il/out_il2csharp`** | `CardScript.cs:12803` `__field_turnDied == (int)ObscuredInt{…}` | `:12809` `if (turnDied != …) { … return (turnDied + 0x1) == (int)obscuredInt3; }` | ✅ **对** | **字段名一并给了**（`__field_turnDied` / `__field_turnCounter` / `__field_BattleMgr`）⇒ 名字↔偏移绑定就靠它 |
| **`cpp2il/out_cpp2il_dev_ilspy`** | `CardScript.cs:19860-19874` `object obj = turnDied - num; return obj == null;` | `CardScript.cs:19876-19902` `if (turnDied == num) return true; … obj2 == null`（`obj = turnDied + 1; obj2 = obj - num2`） | ✅ 方向对 | 🔴 **形式是错的**：`ObscuredInt` 解密整个丢了（`num = (ObscuredInt)(&obscuredInt)` 是垃圾），把整数相等写成了**引用相等 `== null`**。**照它逐字抄会写出一段荒谬代码**，但**比较方向（含 `+1`）仍然保留** |

### 2.1 「谁错了」的答案

- **没有任何一份产物把方向翻掉。** 三个数据源 + 机器码四方一致。
- **唯一「错」的是 F4 的那句转写**：把 `.c` 的 `iVar1 + 1 == iVar4` 读成了「`diedTurn == curTurn + 1`」。
  原式左边是 `turnDied + 1`，右边是 `turnCounter`；把它写成 `diedTurn == curTurn + 1` 相当于**把 `+1` 挪到了右边**，
  于是与账里的 `curTurn - 1` 看起来「符号相反」。**移项不翻号，这个式子两边本来就是同一件事。**
- 账 `战斗逻辑驱动:771 / :772` 的「判据要点」列（`turnDied == turnCounter 或 turnDied + 1 == turnCounter` / `turnDied == turnCounter`）**正确，不需要改**。
- 全库没有第二份拷贝：`decomp_AI` / `decomp_animfx_nested*` / `decomp_chat` / `decomp_cv_0917` / `decomp_generic_trial` 里
  **都没有** `CardScript__HasDied*`（`ls` 0 命中）⇒ 不存在「两份 `.c` 互相打架」。

---

## 三、我们代码里对应的实现 —— 现在是哪一版、要不要改

| 落点 | 内容 | 与原版对不对 |
|---|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/ConditionEval.cs:324-326` | `case 221:` 活卡 `return false`，否则 `DiedInTurn(ctx, card, owner, 0, instanceId)` | ✅ 等价 `turnDied == turnCounter` |
| `Unity/MyGame/Assets/RuleEngine/Core/ConditionEval.cs:331-334` | `case 222:` 活卡 `return false`，否则 `DiedInTurn(…,0,…) \|\| DiedInTurn(…,1,…)` | ✅ 等价 `turnDied == turnCounter \|\| turnDied+1 == turnCounter` |
| `Unity/MyGame/Assets/RuleEngine/Core/ConditionEval.cs:605-618` | `DiedInTurn`：`d.DeathTurn == ctx.Turn - turnsAgo` | ✅ `ctx.Turn` ↔ `turnCounter`、`DeathTurn` ↔ `turnDied` 的逐条对应 |
| `Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs:11355` | `diedsincelastturn` **计数**档：`DeathTurn != ctx.Turn && d.DeathTurn != ctx.Turn - 1 → continue` | ✅ 同一个窗口（这是「数几个」的另一个量，不是本谓词，但窗口一致） |
| 数据侧 | `ctx.DeadUnits` 的 `DeathTurn` 写点：`RuleCore.cs:9794` / `RuleCore.cs:10260`（都是 `DeathTurn = ctx.Turn`） | ✅ 与「临死烙当前回合号」同形 |

**判定：代码不需要改。** 行为已等价，与 `B92_战斗判据_B4.tsv:10-11`、账 `771/772` 的既有裁定一致。
（账里「我们的落点」写 `BattleContext.cs:132`（`DeadUnit.DeathTurn` 字段）—— 那是**字段声明**，真正消费在 `ConditionEval.cs:324/331`；
这是账的落点口径问题，**已在 B92/J1SB 记过**，本轮不重复记、也不动账。）

### 3.1 ⚠️ 一处**已知的边界差异**（要不要「逐字照抄原版 bug」请调度台裁定）

原版**对「活着的卡」没有任何闸** —— 它只读 `turnDied` 字段，而没死过的卡 `turnDied == 0`（§1.3）。
于是：

| `turnCounter` | 原版 `HasDiedThisTurn`（活卡） | 原版 `HasDiedSinceLastTurn`（活卡） | 我们（`u != null → false`） |
|---|---|---|---|
| `0`（开局 / Setup 后、第一次 `NextTurn` 前） | `0 == 0` ⇒ **true** | **true** | false |
| `1`（第一次 `NextTurn` 之后） | false | `0+1 == 1` ⇒ **true** | false |
| `≥2` | false | false | false |

⇒ 若 condition 221/222 的**候选集里包含活卡**，原版在**第 0 / 第 1 个回合**会把活卡判成「死过」。
我们统一返 false = **「修好了」原版的这个行为**（铁律 18·0：默认应照抄原版，含原版 bug）。

**这条我【没有】验完整，如实写明**：
- 已确认：池内用到这两个条件的卡只有 **5 张**（`cards_engine.json` 的 `unitConditions`）：
  `EC39`×3 · `EC58`×2 · `SOR42`×5 · `SW44`×2 · `UM77`×2（`diedSinceLastTurn` 9 处 / `diedThisTurn` 5 处）。
- 未确认：`FilterMethods.MeetsConditionCriteria` 的候选集在**这几个条件**下是否包含活卡（要追每个调用点传进来的 `List<CardScript>`），
  以及 `turnCounter == 1` 究竟对应哪个**具体阶段**（第一次 `NextTurn` 是在第 1 回合开始前还是结束后触发）。
- ⇒ **建议**：由调度台决定是否补一张探测（`EC39` / `SOR42` 在 turn 1 的判定）再定要不要照抄。**本轮不判。**

---

## 四、附带清单：`EntityScript` 这一族里「比较方向可疑」的谓词

**先说范围结论**：
- `EntityScript__*.c`（135 个）里 **`0x3f8` 0 命中、`turnCounter` 0 命中** ⇒ **基类 `EntityScript` 本身没有任何读 `BattleManager.turnCounter` 的谓词**
  （这两个谓词全在派生类 `CardScript` 上）。
- `CardScript` 里读 `+0x3F8` 的共 **9 个**方法，逐个看下来：

| # | 方法 | 逐卡字段 | 关系式 | 有 ±1 吗 |
|---|---|---|---|---|
| 1 | `CardScript.HasDiedThisTurn()` | `turnDied(+0x250)` | `turnDied == turnCounter` | 无 |
| 2 | `CardScript.HasDiedSinceLastTurn()` | `turnDied(+0x250)` | `turnDied == turnCounter \|\| turnDied+1 == turnCounter` | **有（唯一一处）** |
| 3 | `CardScript.IsTheSameTurnDrawn()` | `turnDrawn(+0x310)` | `turnDrawn == turnCounter` | 无 |
| 4 | `CardScript.IsTheSameTurnPlayed()` | `turnPlayed(+0x30c)` | `turnPlayed == turnCounter` | 无 |
| 5 | `CardScript.SetCardTurnDrawn()` | 写 `+0x310 = turnCounter` | —— | —— |
| 6 | `CardScript.OnTurnStart()` | 读 | 未见比较（用于回调参数） | 无 |
| 7 | `CardScript.ResolveCardPlayed()` | 读 | 未见 ±1 | 无 |
| 8 | `CardScript.ResolveUnitSummoned()` | 读（Uprising 闸 `playedIntoFieldTurnCounter`） | `==`（`:86 if (iVar4 == iVar5)`） | 无 |
| 9 | `CardScript.ShouldTriggerTeleport()` | 读 | `==`（`:36 if (iVar1 == iVar5)`） | 无 |
| — | `CardScript.TriggerUnitBacklashActions()` | **写** `+0x250 = turnCounter` | —— | —— |

⇒ **「同类比较方向可疑」在这一族里只有 `HasDiedSinceLastTurn` 一处，而它是对的**（§一）。
另外两个 `IsTheSameTurn*` 是**纯等号**、且**没有「上一回合」那一档** —— 将来若有人照着 `HasDiedSinceLastTurn` 的样子
给它们补 `+1`，那就是**新增了原版没有的行为**，⛔ 别补。

> ⚠️ 别用「gzrep `0x3f8`」当判据：全库 **190 个 `.c` 提到 `0x3f8`**，但那是**任何类**都可能有的偏移；
> 真正认定「这是 `BattleManager.turnCounter`」靠的是三件事同时出现：
> ① `this+0x258` 取 `BattleMgr` ② `+0x3f8` 起 **16 字节密文块 + `+0x408` `fakeValueActive`** ③ 紧跟 `ObscuredInt.op_Implicit`。

---

## 五、还差什么（⛔ 没查到的明说）

1. **`ctx.Turn` ↔ `turnCounter` 的逐轮对齐**：本轮**沿用既有裁定**（B92/J1SB），**没有**再核「我们每方回合 +1」与「原版 `NextTurn` 每方回合 +1」在同一处。
   若要坐实，得读 `RuleEngine` 的 `BeginTurn` 递增点 + 原版 `NextTurn` 的调用者。
2. **§3.1 那张表的可达性未验**（活卡 + turnCounter 0/1）。需要追 `MeetsConditionCriteria` 的候选集来源，或用真机探针跑 `EC39`/`SOR42` 的 turn 1。
3. **`turnDied` 的第二个写点（属性 setter）的上游调用者没追**：`set_turnDied` 的调用者 ILSpy/`.c` 里 0 命中（多半被内联进 `TriggerUnitBacklashActions`）。
   影响：**对结论无影响**（两条路写的都是「当前 `turnCounter`」），但不是「已证只有两个写点」。
4. `0x1834B2BCC`（−360）只有 **5 处**引用（§六 / §七），其它 6 个常量 32–2335 处 —— 说明它的「共用度」很低，别把它当通用常量。

---

## 六、附带：`EventBadge.ShowNotification` 的 7 个浮点常量（VA 读出 + 逐拍落位）

```
RVA 0x6BD120 · VA 0x1806BD120 · file_off 0x6BBB20（dump.cs:64698，`Slot: 4`）
```
读法同上（PE 段表 → `.rdata`，**4 字节 `movss`**；各地址的 8 字节 double 档都是垃圾，已排除）。

| 常量 | 值（float） | 在 8 拍里的位置 | 机器码证据 | 全库引用数 |
|---|---|---|---|---|
| `0x1834B2DC4` | **0.1** | **拍 1**：`AppendInterval(0.1)` —— 第一拍前的静默 | `0x6BD199 movss xmm1,[…]` → `call 0x180AE3DC0`（`DG.Tweening.TweenSettingsExtensions.AppendInterval`） | 135 |
| `0x1834B2E7C` | **0.25** | **拍 2**：`icon.DOPunchScale(punch, 0.25, vibrato 1)` 的**时长** | `0x6BD247 movss xmm2,[…]` → `call 0x180ADCBD0`（`ShortcutExtensions.DOPunchScale`） | 75 |
| `0x1834B2BB8` | **1.0** | **拍 3**：`holder.DOFillAmount(1, 0.4).From(0)` 的 **endValue**；**拍 4**：`holder.DOFade(1, 0.2).From(0)` 的 **endValue** | `0x6BD28F movss xmm1,[…]` → `call 0x180563CA0`（`DOTweenModuleUI.DOFillAmount`）；`0x6BD2CF movss xmm1,[…]` → `call 0x180563820`（`DOFade`） | **2335（全库第 1 高频）** |
| `0x1834B3158` | **0.4** | **拍 3 / 拍 7**：两段 `DOFillAmount` 的时长；**拍 5 / 拍 8**：两段 `DORotate` 的时长 | `0x6BD283 movss xmm9,[…]`（存进 callee-saved，后面 `movaps xmm2,xmm9` 多次复用） | 32 |
| `0x1834B2BC4` | **+360** | **拍 5**：`icon.DORotate((0,0,+360), 0.4, mode 2=WorldAxisAdd)` 的 z | `0x6BD32B movss xmm0,[…]` → `call 0x180ADD600`（`ShortcutExtensions.DORotate`），`lea r9d,[rbp+2]`=mode 2 | 80 |
| `0x1834B2BBC` | **2.0** | **拍 6**：中间的 `AppendInterval(2.0)`（停留） | `0x6BD376 movss xmm1,[…]` → `call 0x180AE3DC0`（`AppendInterval`） | 327 |
| `0x1834B2BCC` | **−360** | **拍 8**：`icon.DORotate((0,0,−360), 0.4, mode 2)` 的 z（转回来） | `0x6BD3BA movss xmm0,[…]` → `call 0x180ADD600` | 5 |

**8 拍总表（VA 逐条对出来，与 `.c` 一致；"游标" = DOTween Sequence 的 Append 游标）**

```
拍1  AppendInterval(0.1)                                                  0.00–0.10
拍2  Append( icon.DOPunchScale(Vector3.one*0.2, 0.25, vibrato 1, elasticity 0) )  0.10–0.35
拍3  Append( holder.DOFillAmount(1, 0.4).From(0) )                        0.35–0.75
拍4  Join  ( holder.DOFade(1, 0.2).From(0) )                              0.75–0.95
拍5  Join  ( icon.DORotate(0,0,+360 ; 0.4 ; WorldAxisAdd) )               0.75–1.15
拍6  AppendInterval(2.0)                                                  1.15–3.15
拍7  Append( holder.DOFillAmount(0, 0.4) )                                3.15–3.55
拍8  Join  ( icon.DORotate(0,0,-360 ; 0.4 ; WorldAxisAdd) )               3.15–3.55
```

（拍 3/拍 4 与 拍 5 有重叠是 `Join` 的正常行为：`Join` 挂到**当前游标**、不推进游标。）

### 6.1 顺带定死的一条：`PunchBase = Vector3.one` **有判据了**

`.c` 里 punch 的基向量是 `*(Vector3*)(<某静态对象> + 0xc)`，一直被记成「本机读不到 ⇒ 我们挑 `Vector3.one`」。
VA + 元数据把它定了：

- `DAT_1842da2a8` 在 `il2cpp_out/script.json` 的 `ScriptMetadata` 里 = **`UnityEngine.Vector3_TypeInfo`**（`Address 0x42DA2A8`）。
- 取法是 `klass->static_fields(+0xb8)` → **静态字段块 `+0xc`**（= 一个 `Vector3`，12 字节）。
- `UnityEngine.Vector3` 的静态字段声明序 = `zero(0x00) · one(0x0C) · up(0x18) · down(0x24) · left(0x30) · right(0x3C) · forward(0x48) · back(0x54) …`，
  与本 build 里「从 `Vector3_TypeInfo` 静态块取」的偏移分布完全吻合：**`+0xc` 95 处 · `+0x18` 36 处 · `+0x48` 18 处 · `+0x3c` 9 处 · `+0x54` 3 处**（`one` ≫ `up` ＞ `forward` ＞ `right` ＞ `back`）。
- ⇒ **punch = `Vector3.one * 0.2`**，`PunchBase = Vector3.one` **是原版值，不是「我们挑的」**。
  ⚠️ 置信度：**高**，但它是**按静态字段声明序推出来的**（没读运行时内存）；若要做成铁案，真机 dump 一次 `Vector3.one` 的静态块即可。
  （对应 `CardPresentation/Shell/EventBadge.cs` 文件头那段「本机读不到 … 仍是我们挑的」的注释 —— **本轮按红线未改代码**，登记给调度台。）
- 另一个只当默认值的：`DOPunchScale` 的 **elasticity = 0**（`0x6BD25B movss [rsp+0x20], xmm8`，`xmm8` 由 `0x6BD24F xorps` 清零；`[rsp+0x20]` 是 x64 第 5 实参栈槽）。
  ⚠️ 置信度中（实参序位推断）。
- `DORotate` 的 **mode = 2**（`lea r9d,[rbp+2]`，`rbp` 已 `xor` 清零）⇒ DOTween `RotateMode.WorldAxisAdd`——与我们代码注释一致。

---

## 七、`0x1834xxxxx` 这一族的高频常量表（可复用）

扫法：全 `.text`(4.9 MB) + `il2cpp`(50 MB) 段逐条反汇编（1,437 万条指令，含跳表处的重同步），
收集所有 **RIP 相对寻址且落点 ∈ `[0x183400000, 0x183800000)`** 的引用并计数；再按「4 字节像干净 float 且不像字符串」过滤。
（脚本 `D:/4/_tmp_view/rh_va/freq3.py` + `filter.py`；全表 6,727 个落点 → `_tmp_view/rh_va/freq.tsv`。）

| 地址 | 引用数 | float | 说明 |
|---|---|---|---|
| `0x1834B2BB8` | **2335** | **1.0** | 全库第一高频；`DOFillAmount`/`DOFade`/各种「填满/不透明」目标值 |
| `0x1834B2BB4` | 955 | **0.5** | 半值（`DOFade(0.5)` 之类） |
| `0x1834B2BC8` | 406 | **−1** | |
| `0x1834B2BBC` | 327 | **2.0** | |
| `0x1834B2F60` | 273 | **0.0**（4B 档） | ⚠️ 同址 8 字节是 **double `1.0`**（`0x3FF0000000000000`）⇒ **这是 double 的 1.0**，别按 float 用 |
| `0x1834B2E08` | 221 | **100** | |
| `0x1834B2DC0` | 196 | **0.017453292**（= π/180） | **度→弧度**，改角度必用 |
| `0x1834B2E50` | 195 | **1.0** | 与 `2BB8` 同值不同址 |
| `0x1834B2BC0` | 153 | **255** | 颜色分量上限 |
| `0x1834B2F48` | 152 | **0.0001** | |
| `0x1834B2DE0` | 149 | **0.0**（4B 档） | ⚠️ 同址 8 字节是 **double `0.5`** |
| `0x1834B2DC4` | 135 | **0.1** | ← 本次的拍 0 |
| `0x1834B2DBC` | 119 | **0.01** | |
| `0x1834B2E8C` | 118 | **3.0** | |
| `0x1834B2E98` | 97 | **57.29578**（= 180/π） | **弧度→度** |
| `0x1834B2DC8` | 82 | **0.3** | 已记：`DoPushBack` 的 elasticity |
| `0x1834B2BC4` | 80 | **360** | ← 本次的 +360 |
| `0x1834B2E70` | 76 | **0.001** | |
| `0x1834B2E7C` | 75 | **0.25** | ← 本次的 punch 时长 |
| `0x1834B2BB0` | 72 | **0.2** | ← punch 幅度倍数 / fade 时长 |
| `0x1834B2F44` | 68 | **1e-5** | |
| `0x1834B2DF4` | 63 | **5.0** | |
| `0x1834B2F94` | 61 | **0.05** | |
| `0x1834B2DF0` | 56 | **4.0** | 已记：换牌掉线重试等待 |
| `0x1834B2FA0` | 56 | **0.8** | |
| `0x1834B2E9C` | 52 | **180** | |
| `0x1834B2E90` | 51 | **3.1415927**（= π） | |
| `0x1834B2E00` | 51 | **8.0** | |
| `0x1834B3090` | 41 | **1.5** | |
| `0x1834B301C` | 40 | **20.0** | |
| `0x1834B2E04` | 37 | **90** | |
| `0x1834BEC00` | 37 | **6.2831855**（= 2π） | |
| `0x1834B3158` | 32 | **0.4** | ← 本次的 fill/rotate 时长 |
| `0x1834B2EC4` | 31 | **1.5707964**（= π/2） | |
| `0x1834B3180` | 24 | **0.6** | |
| `0x1834B302C` | 22 | **−10.0** | |
| `0x1834B2BCC` | **5** | **−360** | ← 本次的 −360（引用极少） |

⚠️ **这张表里「引用数高」不代表「字面量」**：同族里大量地址是**字符串/UTF-16 字符串**（引用数也很高，例如 `0x1834B35A0` = 1104 次，
但 4 字节是 `0x00740033` 这种 UTF-16 片段）。上表已按「不像字符串」过滤，但**用之前仍要看反汇编上下文**（同 `read_literal.py` 文件头那条纪律）。

---

## 八、本轮边界

- ⛔ **未改**源码 / 账 / 正本 · ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改 `D:/2`（Ghidra 工程压根没开，全部用 capstone 直接读 dll 字节）。
- 产出只有这一个文件 + `_tmp_view/rh_va/` 下的临时脚本。
- 给调度台的两条**待办候选**（本代理无权落账）：
  1. **账 `771/772`「判据要点」列本身是对的** —— 若还想补一句，建议加「`+1` 加在 `turnDied` 上（`lea ecx,[rdi+1]`），写成 `turnDied == turnCounter - 1` 与写成 `turnDied + 1 == turnCounter` 是同一件事」以免下一个人再误读。
  2. §3.1 的「活卡在 turn 0/1 原版判真」边界 —— 是否照抄原版 bug（铁律 18·0），需先补可达性探测。
