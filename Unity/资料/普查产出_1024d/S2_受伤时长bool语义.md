# S2 · 受伤时长那个 bool 的语义（S4 的闭合件）

> 只读查证，**不改代码**，只给结论与落点。判据 = `d:/2/tools/decomp_full/` 全量反编译 + `d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 签名桩 + `d:/2/unity_run_ref/GameAssembly.dll` 常量实读。

---

## 一、结论先行

1. **`+0x34` 那个 bool 叫 `doItFast`**，不是「要不要等」，**语义没反**：
   `doItFast == false` ⇒ 走 `WaitForSeconds(1.0)`；`doItFast == true` ⇒ 两处 `WaitForSeconds` **全跳过**。
2. **原版「普通近战/远程命中后的等待」= 1.0 s**。条件：`doItFast == false`（普通攻击命中就是 false）
   且 `DamageInfo.damageType` **不是** `Vulnerable(7)` / `VulnerableCombat(8)`。
   - 若 `damageType ∈ {7, 8}` ⇒ 另有一段 **0.4 s**（在置旗之前，攻击序列**不等**它，见 §三·4）。
3. **我们应把 `EventTiming.DurationOf(EvtKind.Hit)` 由 0.75 改成 1.0**（`Core/EventTiming.cs:384`）。
   理由不是「S4 说 1.0」，而是：**这 1.0 s 是原版攻击序列的一道真门** —— `_ResolveAttack` 以 **0.2 s 粒度轮询**
   受击方的 `wholeDamageAnimFlag`，等它清零才放行下一步（§三·2/3）。这跟「某个 tween 多长」是两回事。
4. 原版实际放行时刻 = **1.0 s（个别帧序下 1.2 s）**；我们改成 1.0 取的是那道门的下界。
   ⇒ 改前每次命中**早 0.25 s**（S1 的「偏快」方向**成立**，台账「偏多 0.3 s」方向是反的）。

---

## 二、bool 字段档案

| 项 | 值 | 出处 |
|---|---|---|
| **名字** | **`doItFast`** | `CardScript.cs:825-836`（编译器生成的 `_003CReceiveDamage_003Ed__381` 字段表）· `CardScript.cs:2441` 签名 `ReceiveDamage(UnitDeathType deathType, DamageInfo damageInfo, bool doItFast = false)` |
| **偏移** | **`+0x34`**（state machine 实例上） | 字段布局与 `MoveNext` 用到的 **7 个偏移全对上**：state `0x10` · current `0x18` · `__this` `0x20` · `damageInfo` `0x28` · `deathType` `0x30` · **`doItFast` `0x34`** · `hasDoneDamage` `0x35` |
| **唯一写点** | `*(undefined1 *)(lVar1 + 0x34) = param_4;` | `CardScript__ReceiveDamage.c:24`（迭代器工厂，`param_4` = 第 3 个真实参，bool） |
| **读点** | ① `MoveNext:296`（漏洞伤害那一段的 0.4 s）② `MoveNext:340`（收尾的 1.0 s） | `CardScript._ReceiveDamage_d__381__MoveNext.c` |
| **取值条件** | 只由调用点实参决定；`true` ⇒ ①②**都跳过**（`:304` goto、`:340` 不成立 ⇒ 落到 `:365` 清旗结束） | 同上 |
| **别名/混淆** | 无。`state machine` 其余 bool（`hasDoneDamage` = `+0x35`）与它无关 | —— |

> ⚠️ 反编译里调用点写成 5 参 `ReceiveDamage(target, 0xf, info, 0, 0)` —— 这是 Ghidra 给**无原型**函数多补的**尾位 artifact**
> （同族例证：`WaitForSeconds___ctor(this, float, 0)`、`RemoveBastionDamage(this, int, 0)`）。
> 签名桩是 3 参 ⇒ 真实参是前三个，**`doItFast` = 第一个 `0`**。

---

## 三、分支推演（`_ResolveAttack` → `ReceiveDamage` → 出口）

**1. 调用点** `BattleManager._ResolveAttack_d__438__MoveNext.c:999`
```c
CardScript__ReceiveDamage(*(longlong *)(param_1 + 0x130), 0xf, lVar20, 0, 0)
```
- `param_1+0x130` = 受击方；`0xf` = 15 = **`UnitDeathType.combatDefender`**（`UnitDeathType.cs:5`）
  ⇒ 这一处就是**普通攻击结算给防守方**（近战/远程共用）。
- `lVar20` = `param_1+0x140` 的 `DamageInfo`（前面 `0 < *(lVar20+0x18)` 判伤害值 > 0）。
- **`doItFast` = 0 = false** ⇒ 会走到 1.0 s。

**2. 调用之后不是 fire-and-forget 就完了** —— `:1001` 只 `StartCoroutine`（未 yield），序列继续做
邻接伤害/践踏，最后落到 `LAB_1809af2fa`（`:1152`）：

```c
if (*(char *)(*(longlong *)(param_1 + 0x130) + 0x351) != '\0') {   // target.wholeDamageAnimFlag
    *(param_1 + 0x18) = *(lVar1 + 0x4c8);      // = BattleManager.twoFractDelay = WaitForSeconds(0.2)
    *(param_1 + 0x10) = 0x19; return 1;        // 睡 0.2 s，回来再查
}
```
`case 0x18 / 0x19` 都 `goto LAB_1809af2fa`（`:1626-1629`）⇒ **0.2 s 一拍地轮询，直到旗清零**。
（`+0x4c8` = `twoFractDelay`：`BattleManager.cs:4597`；常量 `DAT_1834b2bb0` 实读 **0.2**；
 与我们 `EventTiming.SourceOf(Trigger)` 的记载一致。`+0x351` = `CardScript.wholeDamageAnimFlag`，
 `CardScript.cs:1547` 是 `private set`，全二进制只有这条协程写它。）

**3. 协程内部（`CardScript._ReceiveDamage_d__381__MoveNext.c`）**
| 行 | 做什么 |
|---|---|
| `:48-258` | 同步改数值（HP / 堡垒 / 漏洞…）。**伤害在此刻已落到数据上** |
| `:291-304` | 若 `GameStaticData.IsVulnerableDamage(damageInfo+0x1c)`（`damageType-7 < 2` ⇒ **7/8**）⇒ 播图标/触发动画，**`doItFast==false` 时 `WaitForSeconds(0.4)`**（`DAT_1834b3158`=0.4），state 2 |
| `:307-319` | `UpdateHealthText` · `ClearPendingDamage` · `CheckForSkullEarned` · `CheckIfDead` · `BattleCardUI.DamageReceived` |
| **`:312`** | **`wholeDamageAnimFlag = 1`**（从这一刻起，`_ResolveAttack` 的轮询会被挡住） |
| **`:340-346`** | **`if (doItFast == '\0')` ⇒ `WaitForSeconds(DAT_1834b2bb8 = 1.0)`，state 3** |
| `:363-368` | state 3 回来 ⇒ `wholeDamageAnimFlag = 0`，协程结束 |

⇒ **「伤害落账 → 放行下一步」= 1.0 s**（0.2 s 轮询对齐 ⇒ 实测放行 1.0 s，帧序不利时 1.2 s）。

**4. 漏洞伤害那一支的时序（别混淆）**：那 0.4 s 发生在**置旗（`:312`）之前**，此时 `wholeDamageAnimFlag` 仍是 0
⇒ `_ResolveAttack` 那一帧的轮询**看到的是「已放行」**，序列直接往下走**根本不等**；那 0.4 + 1.0 全在后台跑。
⚠️ 即：**漏洞伤害（`DamageType` 7/8）原版反而没有这道门** —— 我们若统一给 1.0，对这一类会**偏长**。
这类在实战里占多少没统计（见 §六·2），所以**先只改普通命中那一档**，别顺手给 `Hit` 分第二档。

**5. 其它调用点（`doItFast` 实参 = 第 3 个真实参）**
- **= 0（false）**：`_ResolveAttack…:686`（手里剑，deathType `0x14`=ability）· **`:999`（combatDefender = 普通攻击命中）** ·
  `:1142`（combatAttacker = 反击）· `:1290 / :1421`（ability）· `ResolveTraitAdjacentDamage.c:24` · `ResolveTraitStompDamage.c:22` ·
  `_ResolveMultiDamageAndDestroySelf…:168 / :181`。
- **= 变量 `battleAction.extraValue`**：`_ResolveMultiDamage…:154 / :168`（AoE 多目标）—— 见 §六·1。
  ⇒ ⛔ 「所有命中都等 1.0 s」这句话不成立，写注释时必须带条件。

---

## 四、常量核实（怎么读的）

`DAT_1834b2bb8`（`MoveNext:342` 传给 `UnityEngine.WaitForSeconds(float)`）：

| 项 | 值 |
|---|---|
| VA | `0x1834b2bb8` |
| ImageBase | `0x180000000` ⇒ **RVA `0x34B2BB8`** |
| 节 | `.rdata`（VA `0x34B2000` · VSize `0xC95284` · RawPtr `0x34AFC00`） |
| 文件偏移 | `0x34B07B8` |
| 8 字节原样（hex） | `00 00 80 3F` + `00 00 00 40`（前 4 = 1.0f，后 4 = 2.0f） |
| **4 字节 float** | **1.0**（`0x3F800000`）✅ 正确档 |
| 8 字节 double | `2.00000047` —— 是「1.0f 后紧跟 2.0f」被硬当 double 读出来的假值，**不是判据** |

读法：PE 头 → `SizeOfOptionalHeader` → 节表每项 40 字节（**VirtualSize 在 VirtualAddress 前**）→ `fileOff = RawPtr + (RVA − VA)`。
工具：`工具/read_literal.py`（两档都「像字面量」时会提醒，这里按**调用点参数类型 float** 取 4 字节档，是对的）。

同一段地址的邻居（一并读，供对照）：`0x1834b3158` = **0.4**（漏洞分支的等待，也是 `DoPushBack` 的 punch 时长）·
`0x1834b2bb0` = **0.2**（`BattleManager.twoFractDelay`，轮询粒度）。

---

## 五、对整局节奏的影响

1. **每条命中 +0.25 s**（0.75 → 1.0）。10 次命中/回合 ≈ **+2.5 s**，正好是用户说的「刚才太快」那一档。
2. **源码落点两处 + 自检两处，必须一起改**（只改一处会留两份不一致的口径）：
   - `Core/EventTiming.cs:384` `case EvtKind.Hit:` → 返回 **1.0**（建议新增一个带出处的常量，别裸写）；并把 `SourceOf(Hit)`（`EventTiming.cs:833-840`）里「候选真值 / 语义未查清 / ⛔ 别猜」整段换成已闭合的判据。
   - `Core/CardFeel.cs:126-127` 与 `:672 / :675` 的 doc 现在写「`HitRotDuration` 的唯一消费者是 `DurationOf(Hit)`」—— 改完就**没有消费者了**，要一起订正。
     ⚠️ `HitRotDuration` / `ResetDuration` **本身别删**：`:20440` 还在断 `HitRotDuration == 0.5`（那是「旋转 punch 多长」的**表现**参数，与 `Hit` 拍长无关）。
3. **会红的自检（`BattleScene` 宿主）**：
   - `Editor/BattleScene.cs:1237` —— `Check(|DurationOf(Hit) − 0.75| < 1e-3)` **写死了旧值**，必须改成 1.0，并把上面那行文案（「`Impact Light Tween`：0.5 旋转 + 0.25 复位」）换成本文判据。
   - `Editor/BattleScene.cs:1685` —— `Check(|DurationOf(Hit) − (HitRotDuration + ResetDuration)| < 1e-4)`，同样要改。
   - `Editor/BattleScene.cs:1608` 只断 `>= 0.3` ⇒ 仍绿，不用动。
   - `Editor/BattleScene.cs:20908` 的 `2f * DurationOf(Hit)`（阵亡时刻）是**推出来的** ⇒ 自动 +0.5 s，不用改数；但**时间线基线整体后移** ⇒ 主对话跟一次 `BattleScene.Run` 重定基线（子代理别跑）。
4. **不会动到引擎**：`RuleEngine/` 里对 `EventTiming` 只有注释 + 一条 `DurationOf(Hit) > 0f` 的断言（`RuleEngine/Editor/RuleEngineTest_EngineE2.cs:232`），改后仍绿。
   ⇒ 纯表现层改动，按铁律 12 只跑 `BattleScene.Run` 那条宿主。

---

## 六、没查清的部分 + 顺手发现

1. **`_ResolveMultiDamage` 的 `doItFast` 传的是 `battleAction.extraValue`（没查清「为什么」）**：iterator `+0x117` → 结构体内偏移 `0xF7`。按 IL2CPP 字段顺序推，`BattleAction` 恰好 0x100 字节、`0xF7` = `extraValue`（旁证：`+0x38` = `multiTargetCard`、`+0x120` = `__this`，两处都对上）。**值本身确定**（`ResolveMultiDamage` 真可能快进），但 `extraValue` 这名字跟「快进」看不出关系，**没追谁把它置 true** —— 不影响近战结论。
2. **漏洞伤害（`DamageType` 7/8）在实战里的占比没统计** ⇒ 要不要给 `Hit` 再分一档（普通 1.0 / 漏洞 ≈0，见 §三·4）**判据不足**，别顺手做。
3. 🔴 **S1 §2·13 有一处数要就地订正（铁律 5）**：S1 写「`Impact Light Tween` 有 **90 件 `BulletImpact_*` 引用**」——
   现读 `animfx_modules.json`（912 条 effect）：引用它共 **73 件** = **56 `BulletImpact_*` + 17 `Lasrifle_*`**，**没有 90**；
   `Impact Heavy Tween`（含 `… 0,5s` 那个）**0 引用**（资产在 `数据/游戏数据/tween/`，但没被任何 AnimFX 模块挂上）。
   不影响 S1 结论（都是远程弹着），但计数必须改，且**更不能拿 Impact Heavy 当近战挨打的尺子**。
4. **「近战挨打」原版到底等多久（口径分开写清）**：
   - **挨打手感本身** = `BattleManager.ResolveUnitShake` → `CardScript.DoPushBack`
     （`DOPunchPosition(direction, **0.4**, vibrato 8, elasticity **0.3**)`，`CardScript__DoPushBack.c`；
     常量 `DAT_1834b3158`=0.4 / `DAT_1834b2dc8`=0.3）——这 0.4 s 由 `_ResolveAnim` 以 **fire-and-forget 协程**起播，
     **与 `ReceiveDamage` 的 1.0 s 并行**，不是串行相加。
   - **序列放行** = `ReceiveDamage` 收尾的 **1.0 s**（§三）。⇒ 近战挨打到下一步 ≈ **1.0 s**，其中前 0.4 s 在抖，后 0.6 s 是「站定」。
   - 我们的模型（`DurationOf(Hit)` = 一拍、`PushBackDuration` 0.4 在拍头起播）与这个结构**同构**，只是拍长少了 0.25。
5. **S1 那句「调用点传双 false」措辞要改**：只有**一个** bool（`doItFast`），另一个 `0` 是反编译器的尾位 artifact（§二末）；
   且 AoE（`ResolveMultiDamage`）可能传 true ⇒ 「所有命中都等 1.0」不能写进注释，必须带条件。

---

（完 · 目标 ≤150 行）
