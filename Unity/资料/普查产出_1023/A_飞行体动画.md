# A · 飞行体动画（战斗线 `W63` 收口）—— 执行代理 A 的报告（2026-10-23）

## ① 结论（一句话 + 四类完成度）

**`Card3DAnimationController` 这一层建起来了：状态机 / 两个状态位 / 飞行闸 / 攻击·受击两个调用点全部落地，
`UnitAttack(50)` 13 行与 `UnitDamaged(60)` 4 行也真的从旧桶搬进了自己的桶并在生产路径上播了 ——
但「卡体 `AnimationClip`」那一档在本 build 是【数据为空】（1,099/1,099 个 `AnimInfo` 的
`minionAnimationClipInfo.enable` 全是 0），所以那一档只做到「结构完整 + 断言用探针 clip 证明它真能播」。**

| 类 | 完成度 | 说明 |
|---|---|---|
| ① 飞行词条（推进中动 / 落地闸住 / 演完飞回） | **做完了** | `DoAnim(disableFlying:true)` 会 `ToggleFlying(false, 0.15)` → 落地回调 → 起播 → 演完 `ToggleFlying(true, 0.45)` 飞回；两个状态位 `0x48/0x49` 逐位照抄；`SyncUnitFlying` 加了守卫（否则每批事件都会把落地取消） |
| ② `UnitAttack(50)` 技能动画 | **做完了** | 运行期两桶 **12 + 4**（原版行数 **13 + 4**，差的 1 条是 `SAU31 Wait_1s` 纯计时拍、无 prefab）；从 `attack_melee` **搬出**（不再当近战粒子），改在**攻击收尾**那一刻播在攻方/目标锚点上 |
| ③ `UnitDamaged(60)` 技能动画 | **做完了** | 从 `hit` 桶**搬出**（受击那一拍原版一件粒子都不播），改在 `Hit` 那一拍播 |
| ④ 攻击/受击的**卡体**动画 | **一半（有判据的那一半早就在，缺的那一半判据为空）** | `ReceiveAttackAnim` → `CardFeel.HitReact`（命中帧，已接）· `ResolveAttackRangedAnim` → `CardFeel.AttackRecover` + 远程 VFX（已接）· **新增** = 卡体 `AnimationClip` 这一层（`DoAnim`），数据为空，只做到结构与断言 |

---

## ② 证据

### 2.1 原版这一层的逐句判据（`d:/2/tools/decomp_full/`）

| 原版 | 内容（逐句读） | 我们 |
|---|---|---|
| `Card3DAnimationController__DoAnim.c` | `clip == null` ⇒ 调回调、返回 false（最前那段）；否则记 `safeResetOnEnd`、关自己+`Animation`、`AddClipIfNecessary`；**`disableFlying` 为真时**先判 `IsFlying()`，在飞且 `0x49` 未置 ⇒ 置位 + 挂回调 + `Toggle(false, 0.15)` 并 return | `Card3DAnimationController.DoAnim`（逐分支照抄，见文件头状态机） |
| `__OnFlyingToggleFinish.c` | `param_2 != 0 ? AnimFinished() : StartAnim()` | 同名方法，同语义（true = 飞起来了） |
| `__Update.c` | `time -= dt`；`1.0/targetFrameRate + time < 0` 时：在飞 ⇒ `Toggle(true, 0.15)` 并 return；否则 `AnimFinished()` | 同名方法；`OneFrameSlack = 1.0` 来自 `DAT_1834b2bb8`（4 字节 float 实读） |
| `__AnimFinished.c` | 在飞 ⇒ 摘回调 + `Toggle(true, 0.45)` + **清 2 字节（0x48/0x49 一起清）**；然后回调、`ClearClips`、关自己、`safeResetOnEnd` 时复位三件 | 同名方法，顺序照抄 |
| `__StartAnim.c` | `enabled=1` + `Animation.enabled=1`；`time = clip.length`；`CrossFade(clip.name, 0.2)` | 同名；`CrossFadeTime = 0.2` = `DAT_1834b2bb0`（4 字节 float 实读） |
| `__cctor.c` | 静态区两个 float：`0x3ee66666` = **0.45**（偏移 0）· `0x3e19999a` = **0.15**（+4）；签名桩声明顺序 = `TOGGLE_ON_FLYING_TIME` / `TOGGLE_OFF_FLYING_TIME` | `ToggleOnFlyingTime/ToggleOffFlyingTime` 两个 `const` |
| `__Start.c` / `__ClearClips.c` / `__AddClipIfNecessary.c` / `__Toggle.c` / `__OnDestroy.c` | `Start` 先关自己再缓存 position/rotation/localScale；`ClearClips` 逐个 `RemoveClip` 再清表；`Toggle` 两处一起写；`OnDestroy` 摘回调 | 全部照抄（`Detach()` = `OnDestroy` 那一个动作） |
| `d:/2/il2cpp_out/dump.cs:25210-25242` | `AnimInfo` 字段表：`startPosOption(0x18)` / `endPosOption(0x1C)` / `pointTowardsOption(0x20)` / `vfxDelayTime(0x24)` / `shouldMoveVFX(0x38)` / `minionAnimationClipInfo(0x68)` / `minionMovementInfo(0x70)` | `CardAnimClips.CardAnimPlacement` 逐字段对应 |
| `dump.cs:25259-25320` | `MinionAnimClipInfo.enable` 序列化注释 = **"Use animation clips or not"**；`MinionAnimClipInfoDefinition` = `{target, animationClip, delay, waitForCompletion, **toggleFlyOff**(0x25), **safeResetOnEnd**(0x26)}` | 解释清楚 `DoAnim` 第二个实参的真名是 `toggleFlyOff`（⛔ 不是「loop」）；`safeResetOnEnd` 也是逐 clip 数据 |
| `AbilityLogic__PlayAnim.c` → `BattleManager__AddCardAnim.c` → `_ResolveAnimAnimationClip_d__473` → `_DelayedBodyAnimationPlay_d__474__MoveNext.c:63` → `CardScript__DoBodyAnimation.c` | **唯一**调用链（`grep Card3DAnimationController__DoAnim` 全库 1 处） | 我们的等价链：`ScheduleAttackPresentation` / `PlaySignal(Hit)` → `PlayUnitSkillAnim` → `PlayOneUnitAnim` → `DoAnim` |
| `BattleManager._ResolveAnim_d__466__MoveNext.c:166-171` | `minionAnimationClipInfo.enable == 0` ⇒ **不进** `ResolveAnimAnimationClip` | 我们据此判定卡体 clip 档为空（见 ③-1） |
| `CardScript__ReceiveAttackAnim.c` | 不是 clip：**`DOPunchPosition(方向 = 我−攻方 归一化, 时长 0.4, vibrato 8, elasticity 0.3)`** + 延迟 | `CardFeel.HitReact`（既有，命中帧已接） |
| `CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c` | `DOLocalMove + DORotateQuaternion + DOScale`（各 `attackStepTime`）→ `WaitForCompletion` → `WaitForSeconds(attackStepTime)` → 才发远程件 | `CardFeel.AttackRecover` + `ScheduleAttackPresentation` 的 `AttackStepRanged`（既有） |

### 2.2 数据（现读）

* 卡池 `cards_engine.json`：`AnimTimes` 里 `tn == "UnitAttack"` = **13 行 / 11 张卡**
  （`DA22×2` · `SAU31×2` · 其余 9 张各 1）；`tn == "UnitDamaged"` = **1 行**（`BL20`）。
* 运行期 `Resources/card_vfx_by_card.json`（按 `EnsureLoaded` 的同一套过滤模拟过一遍）：
  `attack_melee` authoritative = **12 行 / 11 张卡**（少的 1 行 = `SAU31 Wait_1s`，`n` 为空被生成器丢掉）、
  `hit` = **4 行 / 4 张卡**。
  ⇒ **原版行数 13 + 4 = 17**（本件验收口径）· **运行期有件 12 + 4 = 16**。
* `CardAnimClips` 的位置参数表 **16 条**（= 运行期那 16 个有件的拍），逐值抄自
  `assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/<CardAnim>.json` 的 `animInfo`
  （重取命令写在 `Battle/CardAnimClips.cs` 文件头）。

### 2.3 代码坐标

| 文件 | 行 | 内容 |
|---|---|---|
| `Battle/Card3DAnimationController.cs`（新） | 全文 | 状态机 / 两个状态位 / `BuildProbeClip` |
| `Battle/CardAnimClips.cs`（新） | 全文 | 16 行 `AnimInfo` 位置参数 + `Resolve` + `BodyClipOf`（恒 null，带判据） |
| `Core/VfxMap.cs` | `UnitAttack`/`UnitDamaged` 常量段 · `EnsureLoaded` 的搬桶段 · `IsOriginalNoGenericEvent` · `Events` | 两桶的定义、口径、搬桶、静默白名单 |
| `Battle/BattleDriver.cs` | `SyncUnitFlying` 守卫 · 「卡体动画层」整节（`CardAnimOf`/`TickCardAnims`/`ClearCardAnims`/`PlayUnitSkillAnim`/`PlayOneUnitAnim`）· `AdvanceTimeline` 里的 `TickCardAnims` · `ScheduleAttackPresentation` 末尾 · `PlaySignal` 的 `Hit` 那一支 | 持有 / 推进 / 两个调用点 |
| `Editor/BattleScene.cs` | 飞行探针那段之后新增的「W63 · 卡体动画层」整块 + 一处计数 5→7 | 断言 |

---

## ③ 没查清 / 判据为空（**如实，别当成「做完了」**）

1. 🔴 **卡体 `AnimationClip` 这一档在本 build 数据为空**（铁律 11 例外①）。
   实测：`assets_full` 里含 `minionAnimationClipInfo` 的资产共 **1,099** 个，
   `enable` 为 **1** 的 **0 个**（文本正则逐个核过）；`minionClips` 非空的有 **2 个**
   （`Atk_Gauss_Tesla` / `Atk_Gauss_Tesla_Annihilator`），但那两条的 `animationClip` 都是
   **null 引用**（`m_PathID: 0`）且它们自己的 `enable` 也是 0。
   ⇒ 原版这条链在这份数据上「拿不到任何一条可播的卡体 clip」。
   ⇒ 我们**没有**伪造 clip：`AnimationClip` 那一路完整可用，断言用
   `Card3DAnimationController.BuildProbeClip` 造一条探针 clip 证明「建组件 → AddClip → CrossFade →
   倒计时 → AnimFinished → 复位」整条真的在跑；生产路径走「技能动画 prefab + `AnimInfo` 时长」那一路。
2. **`toggleFlyOff` / `safeResetOnEnd` 两个逐 clip 布尔没有判据**（没有 clip 定义可读）。
   我们取 `toggleFlyOff = true`（= 本件要求的那条行为）、`safeResetOnEnd = **false**`
   （复位会把卡弹回建控制器那一刻的旧位；理由写在 `PlayOneUnitAnim`）。
   唯一那两条空 clip 定义里它们是 `1/1` —— 只有 2 个样本，**不足以当判据**，如实记。
3. **`abilityTarget` 那一路的锚点在 `Hit` 事件上是退回 `self` 的**。
   原版 `abilityTarget` 来自那条 `CardAbility` 的目标列表，我们的事件不带目标 ⇒ 池内这 4 条
   `UnitDamaged` 全是自己的效果（`BL20` = 自 buff），故退回 `self` 并**出声一次**。
   将来若出现「挨打时对**别人**播」的 `UnitDamaged` 行，这条要重判。
4. **原版 `Update` 过期那一支用 `TOGGLE_OFF_FLYING_TIME`(0.15) 飞回去，而 `AnimFinished` 用 0.45。**
   两个值不一致是**原版如此**（逐行核过两次），照抄了；看着像原版自己的怪值，⛔ 没改。
5. **`attack_melee` 里另有 2 条 `inferred-name`（`EC29 HeldrakeStrike` / `SOR8 Arco-Flagellant Strike`）
   留在旧桶**：它们不是 `UnitAttack(50)` 触发的（按名字猜的），不在本件口径内。
   它们今天仍会当「近战件」播在受击方 —— **是不是错的没查清**（需要逐卡核这两条的 CardAnim 触发源）。
6. **`SafeResetOnEnd` 那条复位路径在批处理里没有端到端验证**（探针只证明它不崩、且 `Enabled/clip 表`都对；
   实际「三件复位」的数值没逐值断）。可补：探针 clip 改成写 `localPosition.y`，跑完后比对 y 回到初值。

---

## ④ 改动清单（逐文件）

| 文件 | 一句话 |
|---|---|
| **`Battle/Card3DAnimationController.cs`**（新，415 行） | 原版 `Card3DAnimationController` 的逐句复刻：`DoAnim` / `StartAnim` / `AnimFinished` / `ClearClips` / `AddClipIfNecessary` / `Toggle` / `OnFlyingToggleFinish` / `Update`，两个状态位 `0x48/0x49`，四个常数（0.45 / 0.15 / 0.2 / 1.0），外加 `BuildProbeClip`（自检用）与 `Detach`（= 原版 `OnDestroy`） |
| **`Battle/CardAnimClips.cs`**（新，219 行） | `UnitAttack`/`UnitDamaged` 那 16 个有件的拍的 `AnimInfo` 位置参数（sp/ep/pt/mv/vd/ts/tm）+ `Resolve` 取数口 + `BodyClipOf`（恒 null，带「为什么空」的判据与⛔ 别拿 `.anim` 顶的警告） |
| `Core/VfxMap.cs` | 新增 `UnitAttack` / `UnitDamaged` 两个事件名与口径注释；`EnsureLoaded` 把这两族的行**搬到**新桶（⛔ 不再写回 `attack_melee`/`hit`，否则同一件会播两遍）；新增 `CardTableUnit*Entries` / `UnitAnimCardIds`；把两者列进 `IsOriginalNoGenericEvent` 与 `Events` |
| `Battle/BattleDriver.cs` | ① `SyncUnitFlying` 加「动画层正在为动画落地时不许拉起」的守卫；② 新增「卡体动画层」整节（`_animViews` / `CardAnimOf` / `TickCardAnims` / `ClearCardAnims` / `PlayUnitSkillAnim` / `PlayOneUnitAnim` / 三个自检口）；③ `AdvanceTimeline` 里 `TickCardAnims(dt)`（紧跟 `TickFlyingUnits`）；④ `ScheduleAttackPresentation` 末尾排攻方的 `UnitAttack`；⑤ `PlaySignal` 的 `Hit` 那一拍排受击方的 `UnitDamaged`（**`Amount < 0` 不算挨打** —— 那是幸存者广播的治疗标记，原版 `__UseSurvivor` 不进 `ReceiveDamage`）；⑥ 三处 `ClearFlyingUnits()` 旁边补 `ClearCardAnims()` |
| `Editor/BattleScene.cs` | 飞行探针之后新增「W63 · 卡体动画层」一整块断言（常数 / 空动画 / 真 clip 全链 / **飞行闸三段** / 13·12·4·16 四个数 / 逐卡命中 / `BL20` 受击非空 / 分桶灭自证 / 生产口真的起播）；另把「原版没有通用件」那条断言的计数 **5 → 7**（族里真的多了两个成员，判据没放宽） |
| `.meta` | 两个新 `.cs` 的 meta（Unity 已自动生成，未手改） |

**自检（秒级类型检查）**：`TMPDIR=/tmp/wf_A bash 工具/typecheck.sh` → 运行时 **0** / 编辑器 **0**。
⚠️ 中途有一次编辑器错误落在**别人的文件**（`WarpforgeArena1/Editor/UserEffectTools.cs` 的 `Cleanup` 参数个数），
下一次跑就没了（别的代理正在写）—— 按简报口径**不是我改的、也不是我的错**，如实记在这里。

---

## ⑤ 顺手发现（**只报不改**）

1. **生成器 `工具/gen_card_vfx_by_card.py` 的 `TRIGGER_EVENT` 还在把 `UnitAttack→attack_melee` /
   `UnitDamaged→hit`**。本件是在**运行期**搬桶（生成器不在白名单），所以：
   ① 每次重跑生成器都要确认「`attack_melee` 里 authoritative 的那些正好还是 `UnitAttack` 那 13 行」；
   ② **纯计时拍会被生成器丢掉**（`es[].n` 为空 ⇒ 不写进运行期表）——
   `SAU31 Wait_1s` 那 1.0 s 的停顿今天没有对应的排期（只是少一拍停顿，不出件）。
   建议：生成器直接写成 `unit_attack_anim` / `unit_damaged_anim` 两个桶，并把纯计时拍也带出来（带 `n` 为空的行）。
2. **`CardAnimClips` 那 16 行是「数据抄进代码」**（因为 `数据/游戏数据/animinfo_lookup.json` 不在 `Assets` 里）。
   更好的做法：把那份 JSON 搬进 `Assets/CardPresentation/Resources/`、改成运行期读（白名单外 ⇒ 没做）。
   表头写了**重取命令**，重跑生成器之后照它刷一遍即可。
3. **`hit` 桶里 3 条卡的 id 是 GUID 形式**（`a84a5b49…` / `68909cf0…` / `30007162…`），**不在卡池**
   ⇒ 那 3 条 `UnitDamaged` 在生产里**永远触发不到**（`inferred-name` 的副产物）。
   本件把它们接进了桶（口径要 4），但它们实质上是死条目。
4. **`SyncUnitFlying` 每批事件都会 `Toggle(true)`** —— 这是「飞行浮动的 home 跟随」那条实现带来的副作用；
   本件靠「两个状态位同时为真就跳过」的守卫绕开了冲突，但**这个守卫是新增的语义**（值 = 动画层的两个位）。
   如果以后别的地方也想「临时把飞行单位按下去」，必须走同一对状态位，⛔ 别各自再写一套。
5. **`CardFeel.HitReact` 的方向是「往自己半场外侧」的固定上/下**，而原版是
   `(自己 − 攻击者)` 归一化 × `GetPushBackFactor(攻击方尺寸)`（`CardScript__ReceiveAttackAnim.c` 逐句）。
   这是既有实现（`CardFeel` 里有注释），**不是本件引入的**；但它是 ④ 里唯一还差的原版语义。
6. **原版 `CardScript.ResolveAttackRangedAnim` 还有两处副作用我们没做**：`attackCounter++`（+0x48）
   与 `state = 3`（+0x228）—— 那是原版的内部记账（不是画面），本件没碰。
