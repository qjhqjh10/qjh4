# B145-E2 RuleCore A-01/A-03

> 执行代理 `B145-E2` · 第一百三十一会话 · 2026-11-05 · 离线批（⛔ 未跑 Unity / 未动 git / 未改账与活状态表）
> 白名单：`RuleEngine/Core/RuleCore.cs` ✅ · 新建 `CardPresentation/Editor/B145_E2_Assert.cs` ✅ · 其余源码一律未碰。
> 结论：**A-01 ✅ 已改** · **A-03 ✅ 已改** · **A-02 只报告**（要整块搬 ⇒ 留给重钉同步点）。

---

## 判据（.c 文件:行号 + 摘录）

### A-01 —— `targetSelectMethod = 5 (lastAdded)` 少一步逆序

`D:/2/tools/decomp_full/AbilityLogic__GetTargets.c:1274-1313`（`case 5`，逐句现读）：

```c
case 5:
  lVar7 = <new List>; List<…>___ctor(lVar7, …);
  if (pool != 0) {
    iVar15 = *(int *)(pool + 0x18);            // iVar15 = pool.Count
    while (true) {
      iVar15 = iVar15 + -1;                    // 先减再取 ⇒ 从 Count-1 起
      uVar12 = *(int *)(pool + 0x18) - *(int *)(param_2 + 0x40);   // pool.Count - maxTargets
      uVar11 = uVar14;                         // uVar14 = 0
      if (-1 < (int)uVar12) uVar11 = uVar12;   // uVar11 = max(0, pool.Count - maxTargets)
      if (iVar15 < (int)uVar11) { result = lVar7; goto done; }      // 取到底 ⇒ 收尾
      uVar8 = List__get_Item(pool, iVar15);    // 倒着取
      result.Add(uVar8);                       // 追加
    }
  }
```

⇒ 原版语义 = **取 `pool[Count-1] … pool[max(0,Count-max)]`，按递减次序逐个 append**
= 「表尾 N 张 + **逆序**」。

现读我们（改前）`RuleCore.cs:18071`：`pool.RemoveRange(0, pool.Count - max);`
= 保留表尾 N 张、**原序** ⇒ 缺的那一步就是 `Reverse()`。

（对照：`case 7 firstAdded` = `pool.RemoveRange(max, pool.Count - max)` 保留表头 N 张、**不逆序**。
两者不可互换 —— 断言 §A-01-c 专门盯这一点。）

### A-03 —— `pindown` 闸放得太靠上（绿光被误灭）

三层分工，三份 `.c` 逐句现读：

1. **`CardScript.CanAttackNow` 不看 pindown** ——
   `D:/2/tools/decomp_full/CardScript__CanAttackNow.c` 全文（79 行）：查攻击值（`get_CurrentMeleeAttack < 1 ∧ get_CurrentRangeAttack < 1`）、
   `0x96`(cantattack=150) / `0x370`(noncombatant=880)、`cardState ∈ {2,3,0x11}`、`UIstate`、
   `+0x230`(canAct) / `+0x23C`(canAttack)。**`grep 0x3ca` = 0 命中**。
   ⇒ 我们的 `CanAttackNow` = 这函数（绿光判据），**不该含 pindown**。

2. **`BattleManager.IsValidAttackTarget` 看 pindown（只近战）** ——
   `D:/2/tools/decomp_full/BattleManager__IsValidAttackTarget.c:211-223`：

   ```c
   LAB_18096ef06:
     cVar2 = EntityScript__HasCurrentTrait(param_3, 0x2da);      // :211-213 summonRift(目标) 裸闸
     if (cVar2 != '\0') return 0;
     cVar2 = EntityScript__HasCurrentTrait(param_2, 100);        // :215-218 stun(攻方)
     if (cVar2 != '\0') return 0;
     cVar2 = EntityScript__HasCurrentTrait(param_2, 0x3ca);      // :219-222 pindown(攻方)
     if ((cVar2 != '\0') && (param_5 == 1)) return 0;            //          ∧ param_5==1(近战)
     return 1;                                                   // :223
   ```

   ⇒ `param_2` = 攻方、`param_5` = 打法（`1` = 近战）⇒ **「近战打某目标」这一层管**。

3. **`BattleManager.GetAvailableAttackActions` 看 pindown（只近战）** ——
   `D:/2/tools/decomp_full/BattleManager__GetAvailableAttackActions.c:138`：
   `(cVar2 = EntityScript__HasCurrentTrait(param_2, 0x3ca), cVar2 == '\0')` 是近战那支的合取项
   （远程支 `:154-166` **没有**这一条）⇒ **「排近战动作」这一层管**。

⇒ 正确的落点 = ② `IsValidTarget` 尾部（近战档）+ ③ `AttackTargetGate → AttackActionPowerGate`（原样），
**不在** `CanAttackNow`。改前我们把它放进了 `CanAttackNow` ⇒ 一个「近战 3 / 远程 0 + pindown」的单位
`CanActNow` 变假 ⇒ **绿光被误灭**（原版这里是亮的）。

---

## 改动（文件:行号 → 改前/改后；加了几行、加在哪）

`RuleCore.cs` 全程 **行数零变化**（改前 18,866 换行 / 改后 18,866 换行 · LF 纯、裸 CR 0）。

| # | 位置 | 改前 | 改后 |
|---|---|---|---|
| A-01 | `RuleCore.cs:18071` | `pool.RemoveRange(0, pool.Count - max);` | `pool.RemoveRange(0, pool.Count - max); pool.Reverse();` + 出处注释（同一物理行追加） |
| A-03-1 | `RuleCore.cs:5922` | `{ var _pg = AttackActionPowerGate(ctx, p, u, ranged); if (_pg != RuleCodes.OK) return _pg; }` | `{ if (FieldAttack(ctx, p, u, ranged) <= 0) return ranged ? RuleCodes.ErrNoRangedAttack : RuleCodes.ErrNoMeleeAttack; }`（**只保留攻击力那半、去掉 pindown**；同一物理行、零行数） |
| A-03-2 | `RuleCore.cs:5777` | `return target.Has(KeywordTable.SummonRift) ? RuleCodes.ErrTarget : RuleCodes.OK;` | `{ if (target.Has(KeywordTable.SummonRift)) return RuleCodes.ErrTarget; if (!ranged && attacker.Has("pindown")) return RuleCodes.ErrPindown; return RuleCodes.OK; }`（同一物理行、零行数） |
| A-03-注 | `RuleCore.cs:5776` | 结尾停在「…（见账 `:325` 备注）。」 | 同段末尾**就地追加**一句 A-03 的订正说明（不新增行） |

**没有新增 / 删除任何行**；`RuleCore.cs` 里的两个「行内块」（`{ … }`）写法是照铁律 18·b（账按行号引用 365+ 处）。

> ⚠️ `AttackActionPowerGate`（`RuleCorePartial_B97W34.cs:46`）**一个字都没动** ——
> 它仍含 pindown，继续服务「排近战动作」那一层（`AttackTargetGate` → `SimpleAI.EnumerateActions`）。
> 本次只是**不再从 `CanAttackNow` 调它**。`RuleCorePartial_B97W34.cs` 不在本代理白名单。

### 为什么「攻击力 ≤ 0」必须留在 `CanAttackNow`

原版 `CardScript.CanAttackNow` 有一条「近战 < 1 ∧ 远程 < 1 ⇒ 不能打」；我们把它拆成按档的两问
（`CanActNow` 同时问近战/远程，任一过即绿光 —— 等价）。而 `IsValidTarget` 里**没有**攻击力闸
（2026-10-29 因 `play check` ①-b 50→414 张回退过，见 `RuleCore.cs:5774-5776`）⇒
`DeclareAttackCore` 挡 0 攻单位**只能靠 `CanAttackNow` 这一句**。⛔ 别在后续整并时把它一起删掉。

---

## 断言（类名/条数/建议接线宿主）

**新文件**：`Unity/MyGame/Assets/CardPresentation/Editor/B145_E2_Assert.cs`
**类**：`B145_E2_Assert`（`public static`，`Run()` 入口，计数照 `CheckSink` / `MenuNearStyle.Compact2`）
**条数**：**16 条**（A-01 5 条 · A-03 11 条），另每节 `MenuCheck.Section` 1 行标题。
**纯内存夹具**：只 `RuleCore.NewBattle` + `Place`，⛔ 不建场景 / 不建资产 / 不跑 Unity。

| 段 | 条数 | 盯什么 | 灭自证 |
|---|---:|---|---|
| §A-01-a ×2 | 2 | 前提：表非 `null`、截断后正好 3 条 | 建池支写丢 ⇒ 红 |
| §A-01-b | 1 | `method=5` ⇒ `[E5,E3,E2]`（表尾 3 张**逆序**） | 删 `pool.Reverse()` ⇒ `[E2,E3,E5]` ⇒ 红 |
| §A-01-c | 1 | `method=7` ⇒ `[E0,E1,E2]`（表头 3 张**原序**） | 把 `Reverse()` 挪到 `case 7` ⇒ 红；与 b 结构上不可能同时满足 |
| §A-01-d | 1 | `max=9 > pool.Count` ⇒ 不截断、原序 | `Reverse()` 做成无条件 ⇒ 红 |
| §A-03-0 ×3 | 3 | 控制组（无 pindown ⇒ 两路都 `OK`）＋ 前提（夹具真带 pindown） | 关键词没挂上 ⇒ 全是空转 |
| §A-03-a / a2 | 2 | 绿光：`CanAttackNow(近战)=OK` / `CanActNow=true` | pindown 并回 `CanAttackNow` ⇒ 红 |
| §A-03-b / b2 | 2 | `IsValidTarget(近战)=ErrPindown` / `(远程)=OK` | pindown 拿掉 + `!ranged` 去掉 ⇒ 红 |
| §A-03-c / c2 | 2 | `EnumerateActions`：无近战那条 / 有远程那条 | 删 `AttackActionPowerGate` 里的 pindown ⇒ 红 |
| §A-03-d / d2 | 2 | 端到端 `DeclareAttack(近战)=ErrPindown` / `(远程)=OK` | 锁出口码不回退（与 `RuleEngineTest` ⑥ 同码） |

**建议接线宿主**：`RuleEngine/Editor/RuleEngineTest.cs:1400`（现有 B144 七条 `Guard(...)` 所在的**同一物理行**）
—— 纯规则断言、不需要场景宿主。追加形如：

```csharp
B145_E2_Assert.Run(); pass += B145_E2_Assert.Pass; fail += B145_E2_Assert.Fail;
```

（⚠️ 本条**未接线** —— 按简报要求接线由调度台统一做。）

---

## A-02 现读形状（只报告，不改）

`RuleCore.cs` 现读（2026-11-05）：

- **Sentry 块**：`:6893` `if (target.Has("sentry"))` … `:6914` `}`（伤害调用 `:6904`）
- **Shuriken 块**：`:6918` `if (attacker.Has("shuriken"))` … `:6932` `}`（伤害调用 `:6921`）
- ⇒ **Sentry 在 Shuriken 之前**（与施工单描述一致，行号 `:6904` / `:6921` 现读仍有效）。

原版 `BattleManager._ResolveAttack_d__438__MoveNext.c`：
- **Shuriken**（`get_CurrentShuriken` + `EnoughPendingDamageToDieWithDamageValues` + `ReceiveDamage(…, 0x14, …)`）：`:617-717`
- **Sentry**（`get_CurrentSentry` + … + `ResolveAttackRangedAnim`）：`:720-...`
⇒ 原版 **Shuriken 在前、Sentry 在后**。

**未改**：两段**段长不等**（Sentry 22 行 / Shuriken 15 行），整块交换必然改动 `RuleCore.cs` 行数
⇒ 会让 365+ 条按行号引用的账整体漂移；施工单也明确要求**放到「允许重钉的同步点」做**。
⇒ 本代理**零改动**，交调度台排同步点。（池内 `shuriken` / `sentry`：本代理未扫，施工单称 both=0，未复核。）

---

## typecheck 结果

`cd D:/4/Unity/MyGame && TMPDIR=/tmp/wf_e2 bash D:/4/Unity/工具/typecheck.sh`：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 首次运行有 **1 条我造成的** `CS0111`（见「顺手发现 ①」）—— 已当场删掉重复成员并复跑归零。
- 期间另两轮运行出现过 `BattleDriver.cs` 5 条 + `B142_E17/E62_Assert.cs` 4 条错误 —— **全部在别的代理的文件里**
  （`AttackSelectorAllowed` / `ShowHitDamagePop` / `DimInk` / `PlateInk` 等，属 A-09/A-12/A-13 那一批在飞写手）；
  等它们落盘后复跑已 **0/0**。
- `RuleCore.cs` 行尾：LF 纯（`\n` 18,866 · `\r\n` 0 · 裸 CR 0）。

---

## 卡住 / 与判据冲突

### 🔴 两条**存量断言**把 A-03 改前的错口径钉死了 —— 本代理无权改，请调度台分配

不在本代理白名单，改后会各自红一条。它们都是 2026-10-29 `B98_A9` 写的「灭自证」，
当时的结论是「pindown 只在动作表那档生效、⛔ 别并回 `IsValidTarget`」——
**与 `.c:219-222` 相反**（原版 pindown 就在 `IsValidAttackTarget` 里）。

1. `CardPresentation/Editor/B97W34_Assert.cs:182-184`（A4-c）

   ```csharp
   CheckCode(RuleCore.IsValidTarget(ctx, 0, 3, 1, 3, false), RuleCodes.OK,
             "★ A4-c **灭自证**：`pindown` 这张卡在共用判据里**仍然合法**（返 `OK`）…");
   ```
   ⇒ 现读实得 `ErrPindown` ⇒ **本条会红**。建议改成 `RuleCodes.ErrPindown`，
   文案改成「A-03（2026-11-05）：原版 `.c:219-222` 就在 `IsValidAttackTarget` 里 ⇒ 共用判据必须拒近战」。

2. `CardPresentation/Editor/B98G3_Assert.cs:226-227`（A4-c）

   ```csharp
   C(RuleCore.CanAttackNow(ctx, 0, 5, false), RuleCodes.ErrPindown,
     "★ A4-c `CanAttackNow`：近战 3 + pindown 仍返 `ErrPindown`…");
   ```
   ⇒ 现读实得 `OK`（绿光不再被压制误灭）⇒ **本条会红**。建议改成 `RuleCodes.OK`，
   文案改成「A-03（2026-11-05）：`CardScript.CanAttackNow.c` 全文 0 处 `0x3ca` ⇒ 绿光不看 pindown」。

**不受影响**（已逐条核过，改后仍绿）：
- `B97W34_Assert.cs:141-146` A1-b / `:165-167` A3-c（0 攻/近战 0 的 `IsValidTarget` 仍 `OK`）——
  攻击力闸**没**进 `IsValidTarget`。
- `B97W34_Assert.cs:176-190` A4-a/A4-b/A5（走 `Gate` = `AttackTargetGate`）—— `AttackActionPowerGate` 未动。
- `RuleEngineTest.cs:6941`「被压制的单位不能近战」走的是 `DeclareAttack` —— 由 `CanAttackNow` 放行后再由
  `IsValidTarget` 拦下，**净返回码仍是 `ErrPindown`**。
- `BattleScene.cs:12269/12284/36173/36188` 是 stun / 普通 OK，不涉 pindown。

### 未查清

- `play check` 指纹是否逐字节不变：**本代理无权跑 Unity/ruleprobe**。静态推断**应为不变**
  （池内 `pindown` = 0 张、`targetSelectMethod=5` = 0 行，2026-11-05 现扫 `cards_engine.json` 1142 张）——
  但**要由调度台在同步点跑 `play check` 证明**，⛔ 别拿静态推断当验收。

---

## 顺手发现

1. 🔴 **`AbilityTargetListFromCriteriaForTest` 早已存在**（`RuleCore.cs:18197`，5 参版；
   另有 `:18310` 的 6 参 `publishTable` 重载）—— 我第一版在类尾又加了一个同签名成员 ⇒ `CS0111`。
   已**当场删除**（`RuleCore.cs` 回到 18,866 行、零新增），断言改为直接用既有的那个口。
   📌 教训：**派活简报里说「类尾加一个自检门」之前，先 `grep` 一次同名测试口** —— 本工程已有大量 `…ForTest`。
2. 🔴 **`stun`(trait 100) 是同一类「闸放太高」**：原版 `IsValidAttackTarget.c:215-218` 也在攻击目标校验里，
   而 `CardScript.CanAttackNow` 同样**不查 stun**；我们现在把 `u.IsStunned` 放在 `CanAttackNow`（`:5911`）
   ⇒ 理论上**被晕单位绿光也会被灭**，与 A-03 同病。
   **本笔未动**（不在施工单 A-03 范围；且会连带 `BattleScene.cs:12269` 那条 `CanAttackNow == ErrStunned`）。
   ⇒ 建议立一条新账（「`stun` 闸同样需要下沉到 `IsValidTarget`」），一次同步点内连带改断言。
3. **「`IsValidTarget` 同时服务技能目标」这条前提不成立**（`RuleCore.cs:5775`、`RuleCorePartial_B98G3AI.cs:25`、
   `SimpleAI.cs:374` 三处都这么写）—— 现读全仓 `IsValidTarget` 调用点**全是攻击**：
   `EffectResolver.cs:3473`（`PreferKillable` 强攻选靶）· `RuleCore.cs:6460`（`DeclareAttackCore`）·
   `:6735`（续攻复检）· `:17300`（`ValidTargetSlots`）· `AiScoring.cs:564` · `SimpleAI.cs:374/1371` ·
   `BattleDriver.cs:39161`。能力目标走的是 `CanUseAbilityTarget`（`AiScoring.cs:564` 的注释自己也这么说）。
   ⇒ 2026-10-29 那次回退的真实原因只能是**攻击力闸**（0 攻 / 将死），**不是** pindown 半边；
   所以本次只搬 pindown 是安全的（且池内 0 张，零可观测）。
   ⚠️ 建议顺手改掉那三处措辞（铁律 5），⛔ 别让下一批再被「技能目标」吓回去。
4. `cards_engine.json` 现读 `count = 1142`（旧文档常写 1128）—— 报数字一律现读。
5. `cards_engine.json` 里 **`pindown` 0 命中、`targetSelectMethod==5` 0 节点**（两处递归全扫）——
   与施工单的「零可观测」一致，已在本报告里当证据用。
