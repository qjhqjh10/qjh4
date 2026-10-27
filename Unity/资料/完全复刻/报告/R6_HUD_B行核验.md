# R6 · 战斗界面_HUD 的 99 条 B 行核验（B87-R6 · 2026-10-27）

> 代理：R6（只读 + 产出补丁）。对象：`账/战斗界面_HUD.tsv` **状态=B 的 99 行**（现读总表 A 467 / B 99 / C 7 / D 233，共 806 行）。
> 补丁：`账/_parts/B87_R6_HUD核验.tsv`（11 列表头逐字照主表 · **行数 99** · `轮次 = B87-R6`）。
> 机检：`python -I -X utf8 工具/check_ledger_points.py <补丁>` ⇒ **A 42 · 通过 42 · 漂移 0 · 告警 3（MEMBER，合法）· 失败 0**。
> 复现脚本：`d:/4/_tmp_view/r6_20261027/`（`check_landing.py` 落点现读 · `sym_check.py` 符号重定位 · `grep_pass/grep2/grep3/grep4.py` 缺口普查 · `gen_patch.py` 生成补丁）。
> ⛔ 本波未跑 Unity、未动 git、未碰主表、未改任何 `.cs`。

---

## ① 统计

| 档 | 条数 | 含义 |
|---|---:|---|
| **A** | **42** | 已实现且语义一致：双侧方法级（`文件:行（声明名）` 已机检通过） |
| **B** | **43** | 仍待核 / 结构差异无可观察量钉住（备注写清还差哪一份） |
| **C** | **14** | 真缺口（P1 两条 · P2 十二条） |
| 合计 | **99** | 覆盖 99/99，无遗漏（`gen_patch.py` 有 `assert` 校验每条 B 行都有裁定） |

**与「73% 转 A」的口径对不上，原因是这一批的 B 里混了两类不同东西**：
- 一类是**真的只差机检/核验**（42 条，转 A）；
- 另一类是**结构差异 + 一个明写的残留**（43 条 B），它们不是「没做」，而是「做了但有一格没对上或有半句没核」。
  典型：`TrackedCard` 是每帧算出来的所以「收上一张」自动成立、但 `lastMousePos` 那半没有；`UIstate` 我们没有单枚枚举；`Update` 1416 行/13 分支没人逐条对过。

**14 条 C 的分布**：收局摧场族 2（689 / 704）· 交互闸族 3（693 手牌开窗缺 `UIstate==1`、695 缺 `handAnimFlag`、696 坟场缺 `duringBattle`）· 引擎/表现各半 4（537 结转能量、709 战役关、713 单位尺寸比、757 敌方展示缩放）· 联机/教程长尾 5（722 / 731 / 759 / 805 / 806）。

---

## ② 最重要的几条 C（按影响面）

1. **`BattleManager.DestroyUnitsAfterBattleEnd` + `BasicBattleEndSequence`（689 / 704，P1）—— 收局摧场整段没有。**
   原版收局是「先 hero 特例（deathType 0xf）→ 逐单位 `UnitDeath(skipDeathSound)` → 0.5 s → 1.0 s → 结算面板」。
   我们只有 `ShowEndPanelAndFreezeInput`（31991：收窗 31997 / 停输入 32171 / 结算）与「引擎发 `Death` 事件时」的 `PlayDeathFeel`(25194)。
   ⇒ **残局画面上单位不会逐个倒下**，结算直接来。`Assets/` 全树无该方法等价（grep 只 2 处注释：`EventTiming.cs:408`、`PlayerState.cs:196`）。

2. **`ManaManager.SetManaSaved`（537，P2）—— 结转能量只有壳、拿不到真值。**
   显示侧的六步状态机**全做了**（`SetSavedMana:33782` + `ToggleManaAccumulation:33811`），但**生产路径没人调它**：全树 13 处 `SetSavedMana` 命中里，唯一调 HUD 那一口的是自检口 `SetSavedManaForTest:33853`。引擎侧 `PlayerState.SetSavedMana`(PlayerState.cs:482) 与 `RuleCore.cs:2766/1776` 在算/清结转，**没有桥**。后果：能量累积两颗 ON/OFF 与「Energy − saved」显示永远拿不到真值（`SavedManaOf:34504` 恒 0）。

3. **`CardScript.HighlightBloodThirst`（759，P2）—— 我们比原版多了一下可见动作。**
   现读 `BattleCardUI.HighlightBloodThirst` 读的 `ui+0x268 = bloodThirstFrame` **恒 null** ⇒ 原版第一句就 return（**零画面**，24936-24951 已逐句读）。我们额外播了徽标脉冲（`PlayTraitPulse:24630`）⇒ 玩家看到原版没有的一下。

4. **`ShowStatusWhenConnecting.GetConnectingDots / OnGUI`（805 / 806，P2）—— 连接中的常驻横幅没有。**
   原版 `OnGUI` 每帧自绘 + `GetConnectingDots` 按 `floor(Repeat(timeSinceLevelLoad × k, m))` 每秒加点；我们只有一次性 `NetRuntime.Notice`(81) 弹窗（全树 `ConnectingDots` 0 命中）。

5. **`BattleManager.GetUnitSizeRatio`（713，P2）—— 0.86 那一档没有消费者。**
   原版 = 我方 1 / 敌方 × `enemyUnitsScaleRatio(+0x74) = 0.86`；全树 `GetUnitSizeRatio` **0 命中**，`0.86` 只在 `CardFeel.cs:3282` 的读数表注释里出现一次。账原「重钉 → `ArenaSlots.CardScale`(90)」**不成立**：那是 0.36/0.69 的落位缩放（MinionArea.desiredScale），不是同一个量。

（其余 9 条 C 的逐条差异见补丁的「差异」列：693 手牌轻点缺 `UIstate==1`、695 缺 `handAnimFlag`、696 坟场缺 `duringBattleFlag`、709 战役关数据无写点、722 「记进坟场展示列表」那一跳、731 `Time.timeScale=1` + `DOTween.Init`、757 `EnemyCardDisplayScale=1.4` 零消费者、805/806 见上。）

---

## ③ 还差什么（43 条 B 的分族；每条的「还差」已写进补丁备注列）

| 族 | 条数 | 差什么 |
|---|---:|---|
| **某半句没核** | 16 | 737 淡出/复位次序 · 765 `attacksThisTurn`/`AfterAttack` · 778 `AlreadyInPosition` 早退 · 779 堡壘层 · 780 单位卡偏移 · 787 影子「隐藏到默认位」· 790 坐标系两档 · 792 与缩放手表逐格 · 718 OnDestroy 清理清单 · 724 `stop showing`/`lastMousePos` · 725 `TraitCollection.Init` · 743 计数开关 · 754 「回读 UI sprite」调用点 · 761 敌方手牌守卫接线 · 773 四块画法 · 802 碎石粒子锚点 |
| **结构差异·无观察量钉住** | 11 | 712 无单枚 `UIstate` · 723 手牌状态派生（无重置例程）· 745(已 A) · 767 不建牌库视图 · 784/794 数据来源（表直给 vs abilities 解析）· 795/796/797/799/807 「自定义能力 / LiveOps 交集 / showTraits」概念缺 · 801 abstract 无体 |
| **渲染残项（缺旋转那半）** | 3 | 762 / 768 / 777 都是「转向目标的四元数 + `forcedExtraRotation`」同一条缺口 |
| **建层判据本地拿不到** | 2 | 684 / 687 `PressedBtn`（节点名/rect/sprite 本地确实没有 ⇒ 钩子未接时出声） |
| **整段未逐条对** | 1 | 741 `BattleManager.Update`（**1416 行 / 13 分支**，本域最贵的一条，建议单独派活） |
| **判据只 4 行体、不知生效对象** | 1 | 743 `CardDamageCounterController.Toggle` |
| **一次界面等价未证** | 3 | 736 / 774 触摸输入恢复 · 800 残骸载体（独立体 vs 切卡层） |
| **待落点** | 4 | 694 隐式 private 属性（见 ④-1）· 685/681 已用共享落点 · 737 面板淡出 |

---

## ④ 顺手发现（**只报不改**）

1. 🔴 **`check_ledger_points.py` 会把「隐式 private 的成员声明」判失败** —— `BattleDriver.cs:17149 bool TutorialAllowsCardDisplay { get; }`（无显式访问修饰符）被报「找不到这份声明」，而 2026-10-27 的例外明写「纯取值器可用字段/属性当落点」。
   ⇒ `CanDisplayLargeCardInTutorial`（**L694**）因此只能记 **B**（语义已对齐：`!(教程局 && hideLargeCardDisplay)`，三个开窗口 13077/13165 共用）。
   修法二选一：工具放宽到「类型名开头的成员声明」，或给该属性补一个显式 `private`。

2. 🔴 **`AddNextReplayAction` 的调用是【字符串型】** —— 账原判「原版 0 调用」是这么来的：`StartCoroutine("AddNextReplayAction")`（dev_ilspy `BattleManager.cs:21399 / 21478 / 48201` 三处），按方法名 grep `.c` 恒 0。
   ⇒ 这是「搜不到调用点 ≠ 死代码」的**第四个变体（字符串分派）**，建议补进 CLAUDE.md 的已知坑（现有四变体：内联 / 异步状态机 / 实参常量 / `= true` 写点丢）。

3. 🔴 **`BattleManager__ResolveDisappearUnit.c` 是【可读】的（234 行）** —— 账原记「anonymous-type 拷贝（错内容）、判据缺」**不成立**（那 60 字节拷贝是 `BattleAction` 结构体拷贝）。真体只有三句：取该卡视图 → `LogWarning` + `gameObject.SetActive(false)` → `FinishResolvingAction`；**既没有残骸分支也没有溶解**。
   ⇒ L721 的「判据要点」整条已就地订正。

4. 🔴 **B 行的落点【系统性漂移】**：99 行里 **40 多行**的 `BattleDriver.cs:行号` 落在空行/注释/别的语句上（漂 13–500 行）。抽样：`CollectAltArtCopies` 34547→**34560** · `SetSavedMana` 33348→**33782** · `WarlordTryUseActiveAbility` 34352→**34831** · `PlayDeathFeel` 24494→**25194** · `ToCardData` 27773→**27786** · `ShowEndPanelAndFreezeInput` 31214→**31991** · `OnDestroy` 4113→**4139**。
   ⇒ 建议在「没有 `.cs` 写手 + 补丁已合并」的窗口里跑一次 `check_ledger_points --all-A --fix`（⛔ 本波按红线没跑；本补丁已把这 42 条 A 的落点改指真声明）。

5. 🟠 **`ShowDefaultTrait` / `CountShowTraits` 的语义源可能选错列（**属卡牌面域**）**：原版这两个方法查的是 **`showTraits`**（池内非空只 32 张），而 `RuleEngine/Data/CardDatabase.cs:164` 明令「读 `CardTraits`、⛔ 不读 `ShowTraits`」（650 张）——两列差 633 张。
   「有没有这个 trait」用 `CardTraits` 是对的，但「**印不印**」按原版应看 `showTraits`。⇒ 卡面 trait 显示可能多印。已记 B（799）并在此上报。

6. 🟠 **`Time.timeScale` 在 `Assets/` 全树【零写点】**（`grep "Time.timeScale"` 只 3 处、全是注释）。所以 `StartBattleManager` 缺的 `timeScale = 1`（`0x1834b2bb8` 实读 1.0）与 `DOTween.Init(useSafeMode:true…)` 今天不可观测，**但将来做回放快进 / 暂停就会现形**（原版 `AddNextReplayAction` 那条链就在读 timeScale）。

7. 🟠 **两套「敌方展示缩放」并存**：`VarsGlobal.EnemyCardDisplayScale = 1.4f`（`GameplayVariables.cs:601`，原版 `+0x48` 实读）**零消费者**；实际生效的是 `ShowCardInCenter` 那一套（手牌缩放 ×1.1、敌方再 ×1.5 = 1.65，`BattleDriver.cs:25706-25707`）。位置那一半已按 `enemyCardDisplayXoffset(−26)/Yoffset(24)` 落（`EnemyCardPlayedDisplayPointWorld:35568`）。

8. 🟠 **`CardHighlight.EnemyHandCardClickable`（`CardHighlight.cs:836`）全树 0 调用** —— 敌方手牌 Sabotage 守卫写了没接（L761 记 B）。

9. 🟠 **`CampaignStage` 有槽无写点**：`BattleContext.cs:922` 有字段、`RuleCore.cs:1220/2799` 自承「恒 null」⇒ 原版 `GetCurrentTutorialStage` 的 130/190 两支恒空（L709 记 C）。

10. 🟢 **`PressedBtn` 建层是 684 / 687 两条的同一条阻塞**（本地确实没有那颗按钮的 prefab 实例）⇒ 合成一条施工单比拆两条省事；两条的断言已写在 `Editor/BattleScene.cs` 的 `K4HoldAssert`，**本波未跑 Unity ⇒ 一条都没跑过**，同步点请跑 `BattleScene.Run`。
