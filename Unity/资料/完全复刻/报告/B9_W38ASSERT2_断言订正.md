# B9_W38ASSERT2 —— 断言订正（虫群 ⑤ 反了 / R10③ 源级字面串 / 两处过期 doc）

> 执行代理 `B9_W38ASSERT2` · 2026-10-27 · 白名单内改动**只**落在
> `Editor/BattleScene.cs`（+ `Core/CardFeel.cs` + `Battle/CardAnimClips.cs` 两处 doc）+ 本报告 + `账/_parts/B9_W38ASSERT2.tsv`。
> `TMPDIR=/tmp/wf_w38 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（2026-10-27 现跑）。
> ⛔ 未跑 Unity（无 `-executeMethod` / 无 `_run_8_checks.sh`）· 未动 git · 未改正本 / 账主表 / 别人的报告 / `BattleDriver.cs`（只读）。
> 🔴 **零行数变化**：三个文件全部**行数不变**（`战斗逻辑驱动` 的 ④ 仍在 `BattleScene.cs:1876`、虫群 ⑤ 块仍在 `:43320-43334`、
> `CardAnimClips.UnitShakes` 仍在 `:198`、`CardFeel` 的 `<param name="target">` 仍在 `:3899`）⇒ 复跑
> `check_ledger_points` **漂移恒为 678（一字不变，= 本件没制造新的）**。
> ⚠️ 末次复跑显示 `A 3785 / 失败 121`（首次是 `3784 / 120`）—— 那是**别路并发合并进了一行新的 A**（A 计数 +1、失败 +1），
> 与本件无关：本件在这三个文件里的改动**全部零行数变化**，而它们确实有 A 行落点
> （`CardFeel.cs` **28 条** A 行 —— 例如 `:1870` / `:1874` / `:3814` / `:3906`；`CardAnimClips.cs:418` 1 条）—— 一个都没挪，所以漂移数不动。

---

## 一、结论

| # | 这件活 | 改前 | 改后 | 状态 |
|---|---|---|---|---|
| 1 | 虫群合并 ⑤（`BattleScene.cs`） | `Check(!vSwarm.Body3DVisible, …)` + 断完 `SetBody3DVisible(bodyWasSwarm)` 还原 ⇒ **断的是错口径**，而且它正是掩盖「`BattleDriver` 藏错对象」那个真缺陷的帮凶；`W29BDR2` 删掉那句之后**恒红** | `Check(vSwarm.Body3DVisible, …)`「**存活那张的 3D 卡体仍然显示**」；`bodyWasSwarm` 与还原行**删掉**；前提 `Check(vSwarm.Body3DVisible, …)` 挪到两次合并**之前**硬断 | ✅ 改对（判别式 = 把 `SetBody3DVisible(false)` 加回去就红） |
| 2 | R10③ 源级断言（`BattleScene.cs`） | 三格同时红：找字面串 `"PlayHitFrameShake();"` · 要求调用行**不含** `tgtView`/`target` · 体里含 `ShakeCameraForHit` | 找 `PlayHitFrameShake(tgtView);` · 体里含 `ShakeCameraForAttack` · 断**语义**：体内是 `target != null ? … : Vector3.zero` 的空转容忍**且无** `if (target == null) return;` 早退 | ✅ 改成断语义（不再按字面串判） |
| 3a | `CardFeel.cs:3896-3898` doc | 「它现在没有调用者」+「`card_anim_order.json` 没有 `unitsShake` 字段」—— **两半都已过期** | 改成「已接上」+ 数据在 `cardanim_clips.json` 的 `ush` 列 + 两个调用点（含 `animateFeel` 闸，🔶） | ✅ 零行数变化 |
| 3b | `CardAnimClips.cs:194-197` doc | 「现读真正有效的只有 36 条…其中今天可达的 28 条」—— **两个口径混在一句里** | 拆成 ①「**数据侧有效 = 36 条 / 35 个 CardAnim**」②「**与卡池求交后可达 = 28 条 / 28 个 CardAnim**」，并写明「**别互相判红**」 | ✅ 零行数变化 |

一句话：三件都是**断言 / 文档的判据口径**问题，没有一行产品行为改动。

---

## 二、证据（文件:行号）

### 2·1 虫群 ⑤ 改对（`Editor/BattleScene.cs`）

| 位置 | 内容 |
|---|---|
| `:43288` | 前提硬检查挪到这里（**两次 `SimulateSwarmMergeTriggerForTest` 之前**）：`Check(vSwarm.Body3DVisible, "（前提）虫群合并**之前**那张卡的 3D 体是显示着的 …")` |
| `:43320-43329` | ⑤ 的注释块（10 行）：原版对象 = **被并掉那张**的取证链 + 「原断言是反的、掩盖了真缺陷」+「藏被并掉那张今天没有载体」+「非 3D 档下 `_body3D == null` ⇒ 前提会红」 |
| `:43330-43333` | `Check(vSwarm.Body3DVisible, "★ 虫群合并⑤：合并期间**存活那张的 3D 卡体仍然显示**…｜🧨 把 `SetBody3DVisible(false)` 加回去 ⇒ 红");` |
| `:43334` | 只留 `if (!swarmWas) vSwarm.SetSwarm(false);`（图标还原）—— `bodyWasSwarm` 与 `SetBody3DVisible(bodyWasSwarm)` **已删** |

- 判据来源（**未重查，照抄 `B9_W29BDR2` 报告 §二·1/§二·2**）：`decomp_full/SwarmVFXController__Initialize.c:15-26`（藏的是传进来的那张）·
  `decomp_full/CardScript__ExecuteSwarm.c:39-80`（`this` = 被并掉那张）· `RuleCore.cs:4702`（我们这一跳只带存活那格的 `Player/Slot`）·
  `BattleDriver.cs:24817-24828`（那句已删）。
- `Body3DVisible` 定义 = `CardView.cs:3095`（`_body3D != null && _body3D.gameObject.activeSelf`）⇒ **非 3D 档下恒 false**，所以前提那条
  在非 3D 档会红 —— 这是**如实记**（前提不成立就说明本节验不了），不是缺陷。改前的 `Check(bodyWasSwarm, …)` 前提**同样**会红，
  ⇒ 前提这一格**行为没变**，变的只有结论那一格（由「假绿」变「真绿」）。

### 2·2 R10③ 改成断语义（`Editor/BattleScene.cs`）

| 位置 | 内容 |
|---|---|
| `:1826` | `iShake` 的匹配串换成 **`"PlayHitFrameShake(tgtView);"`**（`iSfx` / `iHitVfx` / `iRecoil` 三个照旧，`:1827-1829`） |
| ⚠️ 自记 | 这一格**第一版漏改**（只改了结论、忘了改匹配串）⇒ 那样 `iShake` 会恒 `-1`、前提与结论一起恒红。是**回读文件 + 从源码里抽字面串重跑模拟**才抓到的，已修；见 §二·2 末那条实测 |
| `:1849` | `bool shakeEarlyReturn = false, shakeNullTolerant = false, shakeCallsShake = false;` |
| `:1856-1859` | 体扫描三格：`"if (target == null) return;"` ⇒ `shakeEarlyReturn`（**要求 false**）· `"target != null"` **且** `"Vector3.zero"` 同一行 ⇒ `shakeNullTolerant`（**要求 true**）· `"ShakeCameraForAttack"` ⇒ `shakeCallsShake`（**要求 true**） |
| `:1866-1874` | 结论：`shakeLine` 含 `PlayHitFrameShake(tgtView);` **且** `!shakeEarlyReturn && shakeNullTolerant && shakeCallsShake && recoilHasGuard` |

- 判据（**未重查，照抄 `B9_W29BDR2`**）：`BattleDriver.cs:22664` `void PlayHitFrameShake(CardView target)`，体只有一句
  `CardFeel.ShakeCameraForAttack(cam, hudRoot, target != null ? target.transform.position : Vector3.zero, _lastAttackerMelee, 0f, boardCam);`；
  调用行 `BattleDriver.cs:22264` `if (feel) PlayHitFrameShake(tgtView);`。
- 🔑 **本件用一段 Python 逐字复跑了这条断言的扫描逻辑**（`File.ReadAllLines` 同口径：按 `\r\n`/`\n` 切）⇒ 实测：
  `lamStart/End = 22252/22273` · `iShake/iSfx/iHitVfx/iRecoil = 22264/22265/22271/22272`（次序 ok）· `shakeDecl/recoilDecl = 22664/22673` ·
  扫描区 = `22664-22672`（9 行，只吃到后坐的 doc，没吃到它的体）· `shakeEarlyReturn=False` · `shakeNullTolerant=True` · `shakeCallsShake=True` ·
  `recoilHasGuard=True` ⇒ **FINAL True**。⇒ 不跑 Unity 也能确认「改完是绿的、不是从真红变假绿」。

### 2·3 两处过期 doc

| 位置 | 过期了哪一半 | 现读事实（出处） |
|---|---|---|
| `CardFeel.cs:3896-3898` | ①「它现在没有调用者」②「`card_anim_order.json` 没有 `unitsShake` 字段」 | ① 调用点 = `BattleDriver.cs:22232`（`SchedulePieceUnitShakes`，攻击件循环）+ `:23863`（`ScheduleUnitShakes`，`PlayPlacedOne`）；② 数据在 `Resources/cardanim_clips.json` 的 `ush` 列（`CardAnimClips.cs:198` `UnitShakes`；`B9_W12USHK` 已补）。`animateFeel` 闸在 `BattleDriver.cs:35408` |
| `CardAnimClips.cs:194-197` | 「36 条」与「28 条」写在同一句、**没标口径** | 36 = 数据侧有效条数（`TargetUnit ∈ {20,30}`，`B9_W29BDR2` 复核：`(30,10) 35 + (10,30) 1`）；28 = 与卡池求交后可达（交接简报口径）。⇒ 拆成两个编号口径 |

---

## 三、没查清的部分（还差什么）

1. 🟡 **虫群 ⑤ 的前提在 `BattleScene.Run` 里到底绿不绿，本件没跑 Unity 验不了**。判法：同步点跑 `BattleScene.Run`，
   看那两条（`（前提）虫群合并**之前**…` 与 `★ 虫群合并⑤…`）。**三种可能**：①两条都绿 = 本件对了；②前提红 = 本节视图没有 3D 体
   （`use3DBoard` 档不同）⇒ 账 403 要换到 3D 档验（不是本件改错）；③前提绿、结论红 = `PlaySwarmMergeFeel` 里又有人把体藏了 ⇒ 真回归。
2. 🟡 **R10③ 的「语义」仍然是源码文本匹配**（只是从「找某个字面串」升级到「找三元 + 否定早退」）。它挡不住
   「把三元拆成两句 `if/else` 但语义等价」那种重写 ⇒ 那时会**假红**。**要彻底不脆只能加运行期探针**（`PlayHitFrameShake(null)` 不抛出、
   且 `CardFeel` 收到 `Vector3.zero`），但那是新断言、且要动 `BattleDriver` 的白盒口 ⇒ **不在本件**。
3. 🟡 **`CardFeel.cs:3896` doc 里写的「257 行 / 271 条」是照抄 `B9_W12USHK`/`B9_W29BDR2` 的现读数**，本件没有重算。

---

## 四、改动清单

### 4·1 `Assets/CardPresentation/Editor/BattleScene.cs`（**行数全程不变**）

| # | 位置（改后行号） | 改动 | 行数 |
|---|---|---|---|
| 1 | `:1839` | 两行注释压成一行（判据一条没删）—— 为 ⑤ 块的 +1 行对冲 | 2 → 1 |
| 2 | `:1826` | `iShake` 的匹配串 `"PlayHitFrameShake();"` → **`"PlayHitFrameShake(tgtView);"`** | 1 → 1 |
| 3 | `:1849` | `shakeHasGuard` → `shakeEarlyReturn` + 新增 `shakeNullTolerant` | 1 → 1 |
| 4 | `:1856-1859` | 体扫描三格的条件换成「早退 / 三元容忍 / `ShakeCameraForAttack`」 | 3 → 4（与 #1 对冲） |
| 5 | `:1866-1874` | 结论改断语义（8 行 → 8 行），判别力说明写进断言文案 | 8 → 8 |
| 6 | `:43288` | `bool bodyWasSwarm = …` → **前提硬检查** `Check(vSwarm.Body3DVisible, …)`（挪到两次合并之前） | 1 → 1 |
| 7 | `:43320-43334` | ⑤ 注释块 6 行 → 10 行（写清「原断言是反的」「藏不了被并掉那张」「非 3D 档前提会红」）；前提那三行 Check **删掉**（已挪到 #6）；结论 4 行 → 3 行；还原行删掉 | 15 → 15 |

### 4·2 `Assets/CardPresentation/Core/CardFeel.cs`（**行数不变**）

| # | 位置 | 改动 | 行数 |
|---|---|---|---|
| 1 | `:3896-3898` | 「它没有调用者 / 数据没这个字段」→ 「已接上 + 数据在 `cardanim_clips.json` 的 `ush` 列 + 两个调用点 + `animateFeel` 闸（🔶 自己设计·调度台 2026-10-27 裁定）」 | 3 → 3 |

### 4·3 `Assets/CardPresentation/Battle/CardAnimClips.cs`（**行数不变**）

| # | 位置 | 改动 | 行数 |
|---|---|---|---|
| 1 | `:194-197` | 「36 / 28」拆成**两个编号口径**并写「别互相判红」 | 4 → 4 |

### 4·4 账补丁 `Unity/资料/完全复刻/账/_parts/B9_W38ASSERT2.tsv`（2 行 + 表头，11 列，LF）

| 键（原版类 / 方法） | 改了什么 |
|---|---|
| `SwarmVFXController` / `Initialize(CardScript card, Action onFinish)` | 备注追加：「上句那条断言已按判据改对」+ 新断言原文/判别式 + 前提取值位置 + ⚠️ 旧引用 `:43329`/`:43334` 仍在原位但现在是注释行/还原行；轮次追加 `B9_W38ASSERT2 2026-10-27`。**状态仍 B**（③ 那一跳的「藏体 1.2 s」仍未复刻） |
| `CardScript` / `ResolveAttackAnimationEffects(Transform anchor, Vector3 attackDirection)` | 备注追加：「R10③ 已改成断语义」+ 三格新条件；轮次追加。**状态仍 B**（② 量级待真机核） |

`python -I -X utf8 工具/check_patch_conflicts.py` ⇒ **没有跨补丁重复键（4 份补丁）**（2026-10-27 现跑）。
⚠️ 本补丁**只含这 2 行**（不是整表拷贝）—— 避免「旧内容定时炸弹」。

---

## 五、断言逐条（断什么 / 改坏哪处会红 / 是灭自证吗）

### 5·1 虫群合并 ⑤（`BattleScene.cs:43330`）

- **断什么**：`SimulateSwarmMergeTriggerForTest(me, slot, "swarm")` 之后，**存活那张**的 `Body3DVisible` **仍为 true**。
- **改坏哪处会红**：把 `PlaySwarmMergeFeel` 里 `v.SetBody3DVisible(false)` 加回去 ⇒ 红（**这正是 `W29BDR2` 修掉的那个真缺陷**）。
- **灭自证？** ✅ **是**，而且是**从「自证」翻过来的那一类**：改前那条 `Check(!vSwarm.Body3DVisible, …)` 与它旁边的
  `SetBody3DVisible(bodyWasSwarm)` 还原**互相抵消** —— 断完就把体恢复，于是「藏掉之后没人复原」这个缺陷**结构上照不到**；
  改后断言与被测实现**方向相反**（实现若藏、断言必红），且**不再有还原行**可以把它抹平。
- **前提**：`BattleScene.cs:43288` 硬断「合并**之前** `Body3DVisible` 为 true」（不是 `if (前提) Check(结论)`）。

### 5·2 R10③（`BattleScene.cs:1866`）

- **断什么**（三条同时成立）：① 命中帧那一整个 `ScheduleAction(EventTiming.HitFrameLag(…))` 块里**四件都在**且次序是
  **震屏 → 命中音 → 命中粒子 → 后坐**（这是原来就有的那条，本件没动）；② 震屏调用行是 `PlayHitFrameShake(tgtView);`；
  ③ `PlayHitFrameShake` 体里 **含 `ShakeCameraForAttack`**、**含「`target != null` + `Vector3.zero`」的空转容忍**、
  **不含 `if (target == null) return;` 早退**；④ 后坐那半**仍含** `if (target == null) return;`。
- **改坏哪处会红**：
  · 给 `PlayHitFrameShake` 加回 `if (target == null) return;`（= 把「震屏」和「后坐」合回一个早退，`AU1` 修掉的那个缺陷）⇒ 红；
  · 把 `ShakeCameraForAttack` 换回 `ShakeCameraForHit`（方向链退回 preset）⇒ 红；
  · 去掉三元里的 `Vector3.zero` 兜底（受击视图不在就抛 NRE / 不播）⇒ 红；
  · 把后坐的早退删掉 ⇒ 红；
  · 把次序改回「粒子 → 音 → 后坐 → 震屏」⇒ 红（原来那条，本件未动）。
- **灭自证？** 🟡 **部分是**：这不是「实现改哪它就跟着改」的同义反复 —— 四条都锚在**原版行为**
  （`CardScript__ResolveAttackAnimationEffects.c:51-107` 那一串 + `ReceiveAttackAnim` 的 `target == null` 早退）。
  但它仍然是**源码文本**判据，所以只挡得住「往回改」这一类，挡不住「语义等价的另一种写法」（见 §三·2）。
  ⚠️ 本件**主动拆掉了**原来那条「调用行不含 `tgtView`/`target`」的**假红线** —— 那条在「震屏要看受击视图位置」之后
  必然恒假红，留着只会让人把整条断言关掉（那才是真正失去判别力）。

### 5·3 两处 doc（不是断言）

无判别式；它们的价值是**别让下个会话拿错判据**：① 免得有人以为 `CardFeel.ResolveUnitShake` 还是死代码；
② 免得有人拿「36」去对「28」判红（`B9_W29BDR2` §三·5 已把这个悬空项记过一次，本件把它落到 doc 里）。

---

## 六、顺手发现（⛔ 都不在白名单外改过）

1. 🔴 **`BattleDriver.cs:34757` / `:34766` 那句「`CardFeel.ShakeUnit` 今天**零消费者**」已经过期**：
   `CardFeel.cs:3934`（`ResolveUnitShakeNow` 的 ④ 抖动支）调 `ShakeUnit`，而 `ResolveUnitShake` 现在**有**调用方
   （`BattleDriver.ScheduleUnitShakes`）。⇒ 开局那条 `WarnMissingPresentationData` 警告里「① `Resources/card_anim_order.json`
   无 `unitsShake` 字段 … ⇒ `CardFeel.ShakeUnit` 无消费者（账 `732`）」**因果链已断**（「`card_anim_order.json` 没这个字段」仍字面成立，
   但数据改由 `cardanim_clips.json` 的 `ush` 列供）。**`BattleDriver.cs` 本件只读 ⇒ 没改**，建议下一路顺手改写这句 + 那条警告文案。
2. 🟡 **同一个警告还连带账 `732`**（`CardFeel.ShakeUnit` 的消费者那条）—— 该账条也应按上一条复核（本件没动账主表，只在补丁的两行里提了 adjacent 事实）。
3. 🟡 **`CardAnimClips.cs:193` 的「1,067 行里 257 行有」与 `B9_W29BDR2` 报的「257 行 / 271 条」一致**，但**「1,067 行」这个分母本件没复核**
   （它应该是 `cardanim_clips.json` 的总行数；若数据重烘过要一起改）。
4. 🟢 **`BattleScene.cs` 的 `Check` 是 `Run` 的局部函数** —— 本件**没有**在文件尾加任何新成员，全部改在既有块内，
   所以不需要 `W11ShakeAssert` / `K4HoldAssert` 那种「委托外传 + 文件尾类」模式。
