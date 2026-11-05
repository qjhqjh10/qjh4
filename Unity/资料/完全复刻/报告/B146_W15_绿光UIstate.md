# B146-W15 · 绿光 UIstate 显示闸（2026-11-05）

> 执行代理 `W15` · S1 抓到的 `G-3`（**绿光不看 `UIstate`**）· **只做显示侧**。
> 判据 = `decomp_full/CardScript__CanAttackNow.c:44-56` ＋ `判据/B146_C04_UIstate判据.md` §三/§五。
> ✅ **引擎闸一个字没动**（`RuleCore.CanAttackNow` / `DeclareAttack` 那条路）—— 那半是 `B146-C04-USTATE-GATE`，另开一批。

## 一、改了哪一处（文件:行 + 摘录）

原版（`CardScript__CanAttackNow.c:44-56`，与 `cpp2il/out_il2csharp/CardScript.cs:1354-1362` 逐句同形）：

```csharp
if (this.isPlayer) {                      // 只对本机玩家自己的卡判
    if (UIstate != 1 /*normalBattle*/ && UIstate != 6 /*minionAttacking*/) return false;
}
```

我们改前的两个**绿光决策口**都只读 `RuleCore.CanActNow`（全仓 `UiStateNow()` 在此处 0 调用）：

| # | 位置（改后行号） | 改动 |
|---|---|---|
| ① | `Battle/BattleDriver.cs:26936`（`SyncBoard`） | `v.SetCanAct(canAct && !inDeployPerf && **GreenLightAllowsPlayerAct(owner)**);` |
| ② | `Battle/BattleDriver.cs:27014`（`RefreshCanActForView`，登场演完补推那一拍） | `v.SetCanAct(RuleCore.CanActNow(Ctx, p, s) **&& GreenLightAllowsPlayerAct(p)**);` |
| ③ | `Battle/BattleDriver.cs:14312`（`Update` 里 `TickHudButtonFeedback` 同一句） | 追加 `TickGreenLightUiState();`（每帧**采样**稳定档） |
| ④ | `Battle/BattleDriver.cs:42586-42644`（**类尾**，新增 61 行） | `_greenLightUiState` / `_greenLightUiStateOverride` · `TickGreenLightUiState()` · `GreenLightAllowsPlayerAct(int owner)` · 三个自检读口 |

判定本体（一行）：`if (owner != _me) return true;  return st == 1 || st == 6;`

**为什么只改 ①②**：绿光在我们这边只有这两个**真正的写入点**（`SyncBoard` = 引擎事件收口处；`RefreshCanActForView`
= 原版 `MinionPlayedIntoField` state 4 那一句 `SetActive(canAttackAnim, CanAttackNow())` 的对应物）。⛔ 没在别处再造第二份判据。
①②③④ 全部**零行数位移**（① ② ③ 是同一物理行改写；④ 追加在类尾，最深账落点 `:42161` 之后）。
`git diff --numstat` = `+90/−12`（其余 12 行替换是别人未提交的 `B146-E7` 删支，不是本笔）。

## 二、`UiStateNow()` 的代价与副作用处理

**读实现**（`:42261`，15 档现算 getter）：全是字段判读，**无常写、无副作用**；唯一有成本的一段是
`WaitingForBlockingAction → DeployingNonUnit`（遍历 `_handViews` 调 `interaction.IsDeployFlowing`），
**只在 `_blocking` 为真时**跑到，量级 ≈ 手牌张数。

⇒ 处理的四条：

1. **每帧只采样一次**（③）—— 不进 `SyncBoard` 的逐格循环（`SyncBoard` 每拍最多 8 格，逐格调会 8 倍化）。
2. **演出档保持上一稳定档**（`raw == 3 || raw == 5 ⇒ return`）—— `3 playingMinion` / `5 playingNonTargetSpell`
   是 `WaitingForBlockingAction` 间接驱动的**演出期**档。照它重算会把整排绿光在每段登场/法术演出期间关掉、
   演完还要等下一次 `SyncBoard` 才回来（= §5.4 风险①）。
3. 🔴 **不主动重推视图**（我原本写了 `RepushGreenLights()`，**读原版后删掉了**）：
   原版 `ActivateMinion` 是**事件驱动**（`BattleManager__FinishResolvingAction.c:105/:115` →
   `MinionManager__ActivateMinions`；另有 `BroadcastActivateMinions` / `MinionPlayedIntoField`），
   **不是「UI 档一变就把整排重算」**。照「换档就刷」做的话，玩家一按住手牌（`UIstate = 2 movingHandCard`）
   整排绿光就会灭，而原版**不会**（拖牌期间没有任何 `ActivateMinion`）。⇒ 稳定档只供 ①② 两个既有决策口读。
4. **缺省 `1`** ⇒ 没跑过 tick 的路径（批处理 / 断言 / 回放 / `ruleprobe`）行为与改前**逐字节一致**。

## 三、本机座位怎么取

用 `BattleDriver.MyIndex`（`:199` `public int MyIndex { get { return _me; } }`；`_me` 单机恒 0、
`SetMySeat` 改它、联机客机 = 1）。闸写的是 `owner != _me ⇒ 放行`，⛔ **没有写死 0**。
（`BattleScene.cs:21035` 那一圈断言本来就用 `drv.MyIndex` 换算两侧座位，口径同源。）

## 四、断言与接线

新建 `Assets/CardPresentation/Editor/B146_W15_Assert.cs`（267 行，**全局命名空间、类名 == 文件名**），
四组 24 条：`P` 前提 4 · `A` 本机卡档位对照 10 · `B` 敌方不受闸 4 · `C` 源码结构 6。

**核心判别式（A 组）**：**同一张**本机卡、`CanActNow` 保持为真，只换 UI 档 ——

| 档 | 1 | 6 | 9 | 10 | 14 | 11 | 3 | 回到 1 |
|---|---|---|---|---|---|---|---|---|
| 期望 | 亮 | 亮 | 灭 | 灭 | 灭 | 灭 | 灭 | 亮（灭自证） |

A3…A7 每条都同时把 `RuleCore.CanActNow(...)` **印在断言文案里**，用来排除「本来就是不亮」的假绿。
`B` 组证明闸按座位分：同一档 9 下敌方亮、本机灭，且 `GreenLightAllowsPlayerActForTest` 两侧取值相反。
`C` 组读源码钉五条：两条 `&& GreenLightAllowsPlayerAct(...)` 各 1 处、座位判据 `if (owner != _me) return true;`、
`return st == 1 || st == 6;`、`Update` 那句 tick、`if (raw == 3 || raw == 5) return;`。

🔴 **接线状态 = 未接线**（如实标）：本文件要一台**活 driver**（同 `B141_W8_Assert` 那种形状），宿主在
`Editor/BattleScene.cs`；那两个文件是本波热点大文件（E7/E9 已因它撞过一次），W15 **不占它的写手位**。
⇒ 调度台在 `BattleScene.cs` 卸 driver 之前那条聚合行、**同一物理行**追加即可（⛔ 不新增行）：
`B146_W15_Assert.Run(drv); _pass += B146_W15_Assert.Pass; _fail += B146_W15_Assert.Fail;`

**现有绿光断言（`BattleScene.cs:21035`）不会因本笔变红**：它比的是 `RuleCore.CanActNow`，而批处理没有
`Update` 帧循环 ⇒ `_greenLightUiState` 恒停在缺省 `1`（`GreenLightAllowsPlayerAct` 对本机也恒放行）。

## 五、typecheck

`TMPDIR=/tmp/wf_w15 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（三次：改产品后 · 加断言后 · 删 repush + 收尾注释后）。
行尾：`BattleDriver.cs` 42,729 行 **全 CRLF、裸 CR 0**（改前 42,668 ⇒ +61，全部在类尾）；新断言文件 LF（新文件，无关）。

## 六、顺手发现

1. 🔴 **没改、但要报**：`GreenLightAllowsPlayerAct` 读的档在**我方回合以外不会更新** —— `TickGreenLightUiState`
   只在 `Update` 里跑（每帧），但**批处理 / 回放**没有帧循环 ⇒ 断言路径永远是「无闸」。若以后要在自检里验
   「真实 UI 档」而不是用钉死口，得在 `SyncBoard` 入口也采样一次（本笔**故意没加**：那会在演出期把整排绿光关掉）。
2. 🔴 **判据侧**：原版 `CanActNow = CanAttackNow || CanUseAbilityNow`，而 `CanUseAbilityNow` 的白名单是
   **{1, 10, 6, 13}**（`CardScript__CanUseAbilityNow.c:69-89`），**和绿光这道 {1,6} 不是同一张表**。
   我们的 `RuleCore.CanActNow` 目前只有「近战||远程」那一半（判据 §四 缺口 ④）⇒ 本笔照 `CanAttackNow`
   写 {1,6} 是**正确的**；等 C-04 把技能那一半接上时，绿光这里必须**按两个来源分开判**，
   ⛔ 不能把 {10,13} 直接塞进这张表。
3. ⚠️ `RefreshCanActForView` 在演出档（3/5）被调用时，因为「保持上一稳定档」会看到 `1` ⇒ 登场那张卡的绿光
   **在落地那一刻就点亮**；原版是 `MinionPlayedIntoField` state 4 点亮（`CanAttackNow` 当时 UIstate 仍是 3 ⇒ 灭），
   到 `FinishResolvingAction` 才亮。**差约一拍**（≈1 s）。这是「演出中不掉灯」换来的**已知偏差**，
   若验收时被指出，正解是在 ② 处显式用**现算**档（而不是稳定档）—— 但那样会把 §5.4 风险① 那条闪灯带回来。
