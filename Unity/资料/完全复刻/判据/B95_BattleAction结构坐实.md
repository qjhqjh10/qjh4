# B95 —— `BattleAction` 结构坐实（`BattleManager.AllowResolveAttack` 的第 ③ 道闸判的是谁）

> 只读取证（**本文件是本代理唯一写过的文件**；未改任何源码 / 账 / 正本，未跑 Unity / ruleprobe，未动 git）。
> 日期 2026-10-29 · 执行代理 `B95_A2B17` · 上一手：`B95_WRC`（追到「候选」档）。

---

## 0. 一句话结论

**`lStack_110` = `BattleAction.actingCard`（攻方）**，不是目标那张。
⇒ `AllowResolveAttack` 的 ③（`:438-548`：`IsAttacking()` ∧ `matchType != 0xA0`）判的是**攻方那张卡**；
**目标那张在整个 `AllowResolveAttack` 里一次 `IsAttacking()` 都没被调用过**（它在 `:334` 只作为 `IsValidAttackTarget` 的 `target` 实参出现）。
⇒ 旧账「③ 要的是**目标那张**的 `{3,17}`」**要订正**（`B92_B1` 附录 C-④ 的 3 行 + 账 `战斗逻辑驱动:193` 的备注列）。

---

## 1. 三问逐条（值 + 出处）

### Q1 · `BattleAction` 的真实结构大小 = **0x100（256）字节**

| 来源 | 证据 | 读出的东西 |
|---|---|---|
| ① 签名桩 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleAction.cs:5-81`（`public struct BattleAction` 在 `:5`，`actingCard` 在 `:11`，末字段 `cardAnim` 在 `:81`） | 38 个字段的**声明顺序**（`actionType, isPlayer, actingCard, targetCard, … cardAnim`） |
| ② IL2CPP dump（**偏移表，最权威**） | `d:/2/tools/il2cpp_out/dump.cs:118958-119000` | `actionType // 0x0` · `isPlayer // 0x4` · **`actingCard // 0x8`**（`:118963`）· `targetCard // 0x10`（`:118964`）· … · `extraValue // 0xF7` · **`cardAnim // 0xF8`**（`:118998`，指针 8 字节） |
| ③ 生成的 C 头（`il2cpp.h`） | `d:/2/tools/il2cpp_out/il2cpp.h:20409-20447` | `struct BattleAction_Fields{…}` 字段顺序/类型与 ② 逐条一致，末位成员 = `cardAnim`（指针） |
| ④ cpp2il | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BattleAction.cs:7-83`（`public struct BattleAction` 在 `:7`，`actingCard` 在 `:13`，末字段 `cardAnim` 在 `:83`） | 字段顺序同 ①（**无**偏移注释） |
| ⑤ 反编译体（拷贝宽度） | `d:/2/tools/decomp_full/BattleManager__AllowResolveAttack.c:53` / `:95` / `:137` / `:179` / `:247` / `:286` / `:342` / `:391` / `:442`（**同一段整结构拷贝被内联了 9 次**，每处都是 `lVar25 = 2` 起头） | 拷贝循环 = `lVar25 = 2`，每轮搬 **0x10 个 qword（= 0x80 字节）**，游标 `puVar22 = puVar22 + 0x10` ⇒ **一次整结构拷贝 = 2 × 0x80 = 0x100 字节** |
| ⑥ cpp2il il2csharp | `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:6493-6540`（`num = 2` 在 `:6499`，拷贝体 `:6503-6522`） | 同样 `num = 2` 轮、每轮 `obj + 0x80` ⇒ **0x100** |

⇒ **结构大小 = 0x100**：末字段 `cardAnim` 在 `0xF8`（8 字节指针，止于 `0x100`），IL2CPP 的最大对齐 = 8 ⇒ 正好 `0x100`，与「整结构拷贝 0x100 字节」**两条独立路径同值**。

🔴 **WRC 的「每份 0x100」这半句要说准**：`lVar25 = 2` × 16 qword **不是**「两份、每份 0x100」，而是「**一份 0x100**，分两轮搬」（每轮 16 qword = 0x80）。函数里确实有**两份副本**（`local_218` 与 `local_118`），但那是**同一份 `param_2` 被拷贝了两次**（每次都是完整 0x100），不是「一份结构被切成两半」。

### Q2 · 字段 `+0x08` = **`actingCard`**（不是别的）

- 偏移表直接写死：`dump.cs:118963` `public CardScript actingCard; // 0x8`；
- 声明顺序同：`BattleAction.cs`（`actionType / isPlayer / actingCard / targetCard …`）+ `il2cpp.h:20412` 三源一致；
- **函数内算术自证**（见 §2 那张表）：`local_218` 的 `+0` 被当 `actionType` 用（`:92` 与 `0x34` 比、`:134` 与 `0x4a` 比），`+0xF7` 被当 `extraValue` 用（`:176` `local_121 != 0` ⇒ `return true`）—— 基准位对齐后，`+0x08` 只能是 `actingCard`。

### Q3 · 因此 ③ 判的是 **攻方那张（`actingCard`）**

③ 的完整形状（`BattleManager__AllowResolveAttack.c`）：

```text
:385   if (lStack_110 == 0) {  /* NRE 桩 */ }
:390   if (*(char *)(lStack_110 + 0x40) != '\0') {          // = actingCard.isPlayer
:438       if (lStack_110 == 0) goto LAB_1809579dd;          // NRE 桩（Ghidra 在实参被丢的调用前补的空检查）
:439       cVar19 = CardScript__get_IsAttacking();           // ← Ghidra 把实参丢了；见下面那条
:440       if ((cVar19 == '\0') && (matchType(param_1) != 0xa0)) {
              … CustomDebug.LogWarning(<前缀> + cardState(lStack_110).ToString())   // :490
              … errorMessage(+0x528) = 同一串；return 0;                              // :542-548
           }
       }
```
- `CardScript.get_IsAttacking` 的体 = `cardState(+0x228) ∈ {3, 0x11}`（`d:/2/tools/decomp_full/CardScript__get_IsAttacking.c:1-8`，函数签名是 `(longlong this)` ⇒ **必须有一个接收者实参**，Ghidra 只是把它打丢了）。
- 接收者是谁 —— 四条独立证据都指向 `lStack_110`：
  1. `:438` 在调用前**专门对 `lStack_110` 做了空检查**（Ghidra 对「被丢实参的 this 调用」的固定形状）；
  2. 整段嵌在 `:390` 的 `lStack_110 + 0x40`（cardState/isPlayer 那一族 CardScript 字段）条件里；
  3. 日志读的是 `*(undefined4 *)(lStack_110 + 0x228)`（`cardState`，`:490` / `:542`）⇒ 打印的是**这一张**的状态；
  4. **目标那张（`local_208`）在整份 `.c` 里从未被空检查、从未被读字段、从未被当 `this` 传给任何 CardScript 方法**（`grep -n "local_208"` = **2 处**：`:40` 声明 + `:334` 唯一一次使用）。

🔴 **WRC 提到在其上读 `+0x222` —— 本函数里【没有】这个偏移**：全文件只有 `+0x228`（`grep "0x222\|0x22c" BattleManager__AllowResolveAttack.c` = 0 命中）。`+0x222` 是 `CardScript.<alreadyTriggeredBacklash>`（`dump.cs:35448`），那是**别的函数**的事，别混进来。
（顺带把 WRC 那三个偏移坐实：`+0x40` = `EntityScript.<isPlayer>`，`dump.cs:21973`；`+0x228` = `CardScript.<cardState>`，`dump.cs:35454` —— **都是 `CardScript`（: `EntityScript`）的字段，不是 `BattleAction` 的**，与「`lStack_110` 是 CardScript 指针」自洽。）

---

## 2. 副本基址 → 字段的算术自证（这张表是本报告的硬核）

Ghidra 的栈变量名就是「相对栈帧的偏移」，两个 0x100 字节副本的**基址**分别是 `local_218`（第 1 份）与 `local_118`（第 2 份）。把 `dump.cs` 的偏移表套上去：

| 表达式（`.c` 里怎么写） | 绝对偏移 | 相对副本基址 | 偏移表 ⇒ 字段 | 代码怎么用它（语义核对） |
|---|---|---|---|---|
| `local_218[0]`（`:92` / `:134`） | `0x218` | 第1份 **+0x00** | `actionType` | 与 `0x34`(52 `scriptedAttack`) / `0x4a`(74 `redirectedAttack`) 比 ⇒ 命中即 `return true`（脚本攻击/被改向攻击**跳过全部闸**，语义自洽） |
| `lStack_210`（`:228/:235/:237/:245/:246`） | `0x210` | 第1份 **+0x08** | **`actingCard`** | 传给 `CheckIfPlayerActionPermittedInTutorial(…, actingUnit, …)`（`:231`）· `get_hasAvailableActions` · `+0x40` · `+0x228` |
| `local_208`（`:334`） | `0x208` | 第1份 **+0x10** | `targetCard` | 作为 `IsValidAttackTarget(attacker, **target**, …)` 的第 3 实参（`:334`） |
| `local_121`（`:176`） | `0x121` | 第1份 **+0xF7** | `extraValue` | `!= 0 ⇒ return true`（早退闸） |
| `lStack_110`（`:334/:385/:390/:438/:490/:542`） | `0x110` | 第2份 **+0x08** | **`actingCard`** | 作为 `IsValidAttackTarget(**attacker**, …)` 的第 2 实参（`:334`）· `+0x40` · `IsAttacking()` · `+0x228` |

两个**外部 API 的形参名**把「+0x08 = 攻方 / +0x10 = 目标」双向钉死（`dump.cs` 现读）：
- `dump.cs:32869` `public bool IsValidAttackTarget(CardScript attacker, CardScript target, bool showTips, AttackTypes attType = 0, bool allowAutoActions = False, bool isCheckingDestroyerTargets = False)`
  ⇒ `:334` `IsValidAttackTarget(param_1, **lStack_110**, **local_208**, 0, 0, …)` = `(attacker = lStack_110, target = local_208)` ✅
- `dump.cs:32239` `public bool CheckIfPlayerActionPermittedInTutorial(BattleActionType actionType, CardScript actingUnit, CardScript targetUnit, int targetPosition = 0)`
  ⇒ `:231` `(param_1, 1, **lStack_210**, 0, 0, 0)` = `(actionType = 1 attack, actingUnit = lStack_210, targetUnit = null)` ✅

---

## 3. 三种来源是否互相一致

**一致（无冲突）**：

| 要核的事 | ①签名桩 | ②`dump.cs` 偏移表 | ③`il2cpp.h` | ④cpp2il | ⑤反编译体 `.c` | ⑥`out_il2csharp` |
|---|---|---|---|---|---|---|
| 字段顺序（actionType→isPlayer→actingCard→targetCard…） | ✅ | ✅ | ✅ | ✅ | ——（只有偏移算术） | —— |
| `actingCard @ +0x08` | 顺序推得 | ✅ 明写 | ✅ 顺序/类型 | ✅ 顺序 | ✅（§2 表） | ✗（重命名成 `targetCard`/`handEffect`，见下） |
| 结构大小 `0x100` | 末字段推得 | ✅（`0xF8` + 8B 指针） | ✅（末成员指针） | —— | ✅（拷贝 2×0x80） | ✅（`obj+0x80` × 2 轮） |
| `IsAttacking` 的接收者是攻方 | —— | ✅（`IsValidAttackTarget(attacker,target)` 形参名） | —— | —— | ✅（§1 Q3 四条） | —— |

⚠️ **两处「看着不一致、实际是反编译器噪声」**（按 CLAUDE.md 已记的族，记在这里免得下一个人再踩）：
1. `out_il2csharp/Assembly-CSharp/BattleManager.cs:6503-6504` 那串 `((byte*)(obj4 + 0x80) - 0x80)[0] = battleAction1.actionType; … = battleAction1.targetCard;` —— 它把 `+0x08` 那格**命名成 `targetCard`**，但**同一行块里紧接着就是 `((byte*)&battleAction1.handEffect - 0x60)`**（`handEffect` 在偏移 `0x80`，与 `+0x08` 毫无关系）⇒ 那是 **struct 拷贝式反编译的整体错位**（本文件对 struct 拷贝向来「极难读」，见 CLAUDE.md §三）。**⛔ 不要拿它当 `+0x08 = targetCard` 的证据。**
2. `out_cpp2il_dev_ilspy/BattleManager.cs:22754+` 同一方法的体被 `NoteDecompilerIssue` 淹掉、只剩 `battleAction2 + 128` 的游标 ⇒ 只能读出「0x80 步进」，读不出字段名。

---

## 4. 该订正哪几处（⛔ 我没改，交调度台）

| 文件 | 行 | 现在写的 | 应改成 |
|---|---|---|---|
| `资料/完全复刻/判据/B92_战斗判据_B1.md` | **`:233`**（附录 C-④ 的 ⑦ 条） | 「⑦ **目标那张** `isPlayer(+0x40)` 为真 ∧ `!IsAttacking()` ∧ `matchType != 0xA0(160)` ⇒ …」 | 「⑦ **攻方那张**（`actingCard`）`isPlayer(+0x40)` 为真 ∧ `!IsAttacking()` ∧ `matchType != 0xA0(160)` ⇒ …（判据 = 本文 §2 表：`lStack_110` = 第 2 份副本基址 +0x08 = `actingCard`；代码 `:385-548`）」 |
| 同上 | **`:237`**（缺 4 条清单） | 「⑦「**目标是玩家那张** ⇒ `!IsAttacking && matchType != 160`」」 | 「⑦「**攻方是玩家那张** ⇒ …」 |
| 同上 | **`:240`**（还差什么 ③） | 「⑦ 里的 `IsAttacking()` 原版指「**目标那张**正处于攻击中状态（`cardState ∈ {3, 0x11}`）」，我们的 `IsAttackingOnBoard` 是「在场上且活着」—— 不是同一个量」 | 前半句改为「**攻方那张**」；后半句结论**不变**（`IsAttackingOnBoard` 仍不是同一个量，⛔ 仍不许拿它顶） |
| `资料/完全复刻/账/战斗逻辑驱动.tsv` | **`:193` 第 11 列（备注）** | 「③ 要的是**目标那张**的 `{3, 0x11}`（`IsAttacking()` 的体就是这两个值）」 | 「③ 要的是**攻方那张**（`actingCard`）的 `{3, 0x11}`；`lStack_110` = 第 2 份副本基址 `0x118` + `0x08` = `actingCard`（`dump.cs:118963`），目标那张在本函数**一次都未被 `IsAttacking` 判过**」 |
| `资料/完全复刻/账/战斗逻辑驱动.tsv` | **`:193` 第 8 列（差异）**（可选） | 「③ ⑦（目标 `isPlayer` 时 `!IsAttacking() && matchType != 0xA0(160)`）」 | 同上，把「目标」改「攻方」 |

（`B92_B1` 里 `:230` 的 ④ 教程闸写的是 `actingCard.isPlayer` —— 那条**本来就是对的**，与本文 `:390` 的 `lStack_110 + 0x40` 同义，⛔ 别一起改。）

---

## 5. 没查清 / 残留风险（如实标）

1. **`:439` 的实参本身拿不到字节级证明**：`.c` 打成 `CardScript__get_IsAttacking()`（无实参）。上面的归属靠**四条佐证**（`:438` 空检查 · `:390` 的 `lStack_110+0x40` 外层条件 · `:490/:542` 打印 `lStack_110+0x228` · 目标那张全函数零字段读）；**没有任何一条反证**。若要 100% 坐实，需要对该调用点做 **VA 级反汇编**（看 `rcx` 从哪个栈槽装载）——本代理**没做**（只读文本产物，未跑 Ghidra）。
2. **`matchType` 的 `0xA0`(160) 语义**（Replay？）本报告只引用数值，未去核 `MatchType` 枚举（`B92_战斗判据_B1.md:115` 那处写 `0xA0(160 Replay)`，**未在本轮复核**）。
3. **`get_hasAvailableActions` 的实现**（`:237`）本轮未读（账里已有口径：`+0x48==0 || (HasTrait(0xdc) && +0x48==1)`）；本文只用到「它的接收者是 `lStack_210`」这一点。
4. **两个副本为什么是两份**（`local_218` / `local_118`）：属编译器/内联产物，未深究；对本问**无影响**（两份内容相同、都是 `param_2` 的整拷贝）。

---

## 6. 复现命令（只读）

```bash
grep -n "struct BattleAction" d:/2/tools/il2cpp_out/dump.cs            # → 118958
sed -n '118958,119000p' d:/2/tools/il2cpp_out/dump.cs                  # 偏移表
sed -n '20409,20447p' d:/2/tools/il2cpp_out/il2cpp.h                   # 生成的 C 头
sed -n '1,8p' d:/2/tools/decomp_full/CardScript__get_IsAttacking.c     # cardState ∈ {3,0x11}
grep -n "local_218\[0\]\|local_121 != \|lStack_210\|lStack_110\|local_208\|IsAttacking\|IsValidAttackTarget\|CheckIfPlayerActionPermitted" \
     d:/2/tools/decomp_full/BattleManager__AllowResolveAttack.c
grep -n "IsValidAttackTarget\|CheckIfPlayerActionPermittedInTutorial" d:/2/tools/il2cpp_out/dump.cs   # 32869 / 32239
```
