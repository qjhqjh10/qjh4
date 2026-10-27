# B9_W12USHK — `AnimInfo.unitsShake`（`+0x78`）数据链 + `minionAnimationClipInfo`（`+0x68`）列定义

- 执行代理：`B9_W12USHK`　日期：2026-10-27
- 独占文件：`Unity/工具/gen_cardanim_clips.py` · `MyGame/Assets/CardPresentation/Resources/cardanim_clips.json`（生成物）· `MyGame/Assets/CardPresentation/Battle/CardAnimClips.cs`
- ⛔ 未碰：`Battle/BattleDriver.cs`（W10）· `Core/CardFeel.cs` / `Editor/BattleScene.cs`（W11）· 任何 `.unity` · git · 账主表

---

## 一、结论

1. **`unitsShake` 的数据链已补齐**（原来是**一整片零数据**）：
   生成器新增 `ush` 列（**257 行 / 271 条**，键 = `CardAnim` 名），运行期契约新增
   `RtRow.ush` → `CardAnimUnitShake[]` → `CardAnimPlacement.UnitShakes`，并给出读数口
   `CardAnimClips.UnitShakesOf(cardAnim)`。`python -I -X utf8 工具/gen_cardanim_clips.py --check` = **OK**（幂等）。
2. **⛔ 没有接调用方**（`BattleDriver.cs` 不在本代理白名单）⇒ `CardFeel.ResolveUnitShake` **仍是 0 调用点**。
   接线指令见 §七（一句话）。
3. 🔴 **一条会改变验收口径的发现**：271 条里 **235 条 `targetUnit == 0`（undefined）**——
   原版 `GetTargetUnit` 对 0 一律返回 null ⇒ 协程只白等 `shakeDelay` 就退出，**什么都不做**。
   真正有效的只有 **36 条 / 35 个 CardAnim**，其中**今天真能播到的 = 28 条 / 28 个**（其余 8 条所属 CardAnim
   没有任何卡引用）。而这 36 条里 **35 条**是 `targetUnit = abilityTarget(30)` + `awayFromUnit = self(10)`
   （另 1 条是 `DelightfulAgonies` 的 `{t:10, aw:30}`，方向相反）⇒ 按原版二选一，
   36 条**全部**走**击退支**（`DoPushBack`）、**没有一条**走 `DoUnitShake` 抖动支（详见 §三·2）。
   ⇒ **本数据下「抖单位本体」一次都不会发生**，能看到的只有「被弹开/后坐」。
   ⛔ 因此**别按「抖一下」去验收**，那会得出「没做对」的假结论。
4. **`minionAnimationClipInfo`（`+0x68`）只补了列定义与读数口，没有值就如实留空**：
   现读全池 **2 行**（`Atk_Gauss_Tesla` / `…_Annihilator`），两行 `macEn = 0`、`macs[].clip = "0"`（null）
   ⇒ **今天零可观测差**（与原版 `_d__466:166-171` 的 `if (enable)` 闸自洽）。
5. 🔴 **简报里「数据落进 `card_anim_order.json`」这一句要改成 `cardanim_clips.json`**：
   运行期真正读的是后者（`CardAnimClips.ResourcePath = "cardanim_clips"`，`RtRow` 就在那个文件里）；
   `Resources/card_anim_order.json` 是**另一张表**（只收 `activeAbility` 的 62 张卡 / 99 拍，**没有** `unitsShake`）。
   `数据/游戏数据/card_anim_order.json`（**工作副本、不在 `Assets` 里**）确实带 `unitsShake`（**104 行 / 110 条**，
   这就是 `CardFeel.cs:3806` 那句「现读 110 条」的来历）—— 但**运行期读不到它**，⛔ 别把它当运行期数据源。
6. **简报里「`attack_melee` 桶 0 件」不成立**：现读 **4/12** 件带 `ush`（见 §六）。
   （`B9_W1BDR` 报的「attack_melee **0 件**」说的是 `shk`/`screenShake` 那一族，两族别混。）

---

## 二、证据（文件:行号）

### 原版判据（🥇 反编译真体优先）

| 结论 | 出处 |
|---|---|
| `unitsShake` 是 `List<UnitShake.UnitShakeInfo>`，偏移 `+0x78`；`minionAnimationClipInfo` 在 `+0x68`；`tweenAnim` 在 `+0x80` | `d:/2/tools/il2cpp_out/dump.cs:25239-25243` |
| `UnitShake.UnitShakeInfo` 五字段：`targetUnit 0x10` · `awayFromUnit 0x14` · `shakeSize 0x18` · `shakeDuration 0x1C` · `shakeDelay 0x20` | `dump.cs:25498-25511` |
| `MinionAnimClipInfo`：`enable 0x10` · `delay 0x14` · `minionClips 0x18[]` | `dump.cs:25310-25340` |
| `MinionAnimClipInfoDefinition`：`target 0x10` · `animationClip 0x18` · `delay 0x20` · `waitForCompletion 0x24` · `toggleFlyOff 0x25` · `safeResetOnEnd 0x26` | `dump.cs:25259-25305` |
| `TargetUnit` 枚举：`undefined 0 / self 10 / cardTarget 20 / abilityTarget 30 / friendlyHero 40 / enemyHero 50` | `dump.cs:45603-45613` |
| `UnitSize` 枚举：`small 0 / medium 2 / big 3`（⚠️ **不是** 0/1/2） | `dump.cs:101810-101817` |
| 远程那一路：`DelayedVFXPlay` 之后 → `unitsShake` 非空则**逐条** `ResolveUnitShake(new UnitShake(info, actingCard(+0x30), targetCard(+0x38)))`，**否则**（if/else）`tweenAnim` | `decomp_full/BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:50-108`（`unitsShake` 循环 `:64-84`、`tweenAnim` `:85-107`） |
| 同一个分支在**通用拍**里也有一份（⇒ 本列**与事件桶无关**） | `decomp_full/BattleManager._ResolveAnim_d__466__MoveNext.c:108-145` |
| `d__468` 的两个卡形参名 = `actingCard @0x30` / `targetCard @0x38` | `dump.cs:28854-28862` |
| 协程四步：① `shakeDelay > 0` 先 `WaitForSeconds`　② 非空检查　③ `IsInPlay(target)`　④ `awayFrom != null && != target` ⇒ 击退支，否则抖动支（`shakeSize 0/1→2 · 2→4 · 3→8`） | `decomp_full/BattleManager._ResolveUnitShake_d__472__MoveNext.c:29-181`（`CardFeel.ResolveUnitShake` 已逐句实现，本批未动） |
| `UnitShake..ctor`：标量原样搬 + 两个枚举各过一次 `GetTargetUnit` | `decomp_full/UnitShake__.ctor.c:8-18` |
| `GetTargetUnit`：`10 ⇒ actingCard` · **`20 与 30 都 ⇒ targetCard`** · `40 ⇒ GetHero(acting.isPlayer)` · `50 ⇒ GetHero(!acting.isPlayer)` · **0 / 其它 ⇒ null** | `decomp_full/BattleManagerSupport__GetTargetUnit.c:13-50` |
| `enable == 0` ⇒ **根本不调** `ResolveAnimAnimationClip` | `decomp_full/BattleManager._ResolveAnim_d__466__MoveNext.c:166-171` |

### 我们（改后现读）

| 结论 | 出处 |
|---|---|
| 生成器新增 `_unit_shake_block` / `_minion_clip_block`，并在 `build()` 里落 `ush` / `mac*` | `Unity/工具/gen_cardanim_clips.py:361`（`_unit_shake_block`）· `:408`（`_minion_clip_block`）· `:548-553`（调用点） |
| 运行期列：`RtRow.ush` / `macEn` / `macD` / `macs` | `CardPresentation/Battle/CardAnimClips.cs:289-292` |
| 两个 DTO：`RtUnitShake`（`t/aw/sz/du/dl`）· `RtMinionClip`（`t/clip/d/wc/tf/sr`） | `CardAnimClips.cs:297` · `:314` |
| 运行期契约：`CardAnimPlacement.UnitShakes / BodyClipEnabled / BodyClipDelay / BodyClips / HasUnitShake` | `CardAnimClips.cs:196` · `:201` · `:204` · `:208` · `:216` |
| 映射进表：`MapUnitShakes` / `MapBodyClips` | `CardAnimClips.cs:394-395`（`EnsureLoaded` 里那两行）· `:580` · `:598` |
| 读数口：`CardAnimClips.UnitShakesOf(cardAnim)` / `BodyClipsOf(cardAnim)` | `CardAnimClips.cs:572` · `:620` |
| 两个公开结构体：`CardAnimUnitShake`（含 `Effective = TargetUnit != 0`）· `CardAnimBodyClip` | `CardAnimClips.cs:633` · `:659` |
| 数据：`ush` 257 行 / 271 条；`mac*` 2 行 | `MyGame/Assets/CardPresentation/Resources/cardanim_clips.json`（= `数据/游戏数据/cardanim_clips.json`，两份逐字节相同，md5 `d3ed931973b50823618ec71e91d832ec`） |

---

## 三、unitsShake 取值分布表（现读，口径 = **条目**，共 271 条 / 257 行）

### 3·1 逐字段分布

| 字段 | 取值 → 条数 |
|---|---|
| `t`（`targetUnit`） | **0 = 235** · 10(self) = 1 · 30(abilityTarget) = 35 |
| `aw`（`awayFromUnit`） | 0 = 26 · 10(self) = 48 · **30(abilityTarget) = 189** · 40(friendlyHero) = 6 · 50(enemyHero) = 2 |
| `sz`（`shakeSize`，`UnitSize`） | **0(small) = 238** · 2(medium) = 33 · 3(big) = **0** |
| `du`（`shakeDuration`，秒） | 0.0 = 3 · **0.2 = 147** · 0.3 = 10 · 0.4 = 18 · 0.5 = 26 · 0.7 = 18 · 0.8 = 18 · 1.0 = 30 · 2.0 = 1 |
| `dl`（`shakeDelay`，秒） | 0.0 = 86 · **0.1 = 139** · 0.3 = 2 · 0.4 = 14 · 0.5 = 1 · 0.55 = 2 · 0.8 = 1 · 1.0 = 22 · 2.0 = 4 |

- 每行条数：**1 条 = 243 行**、**2 条 = 14 行**（最多 2 条）
- 不同五元组：**38 种**；最常见的是 `(t=0, aw=30, sz=0, du=0.2, dl=0.1)` **111 条**（占 41%，且**全部空转**）

### 3·2 「真正会动」的那 36 条（`t != 0`）

```
35 条 = t=30(⇒targetCard) + aw=10(⇒actingCard)     ← 含 DelightfulAgonies 的第 2 条
 1 条 = t=10(⇒actingCard) + aw=30(⇒targetCard)     ← DelightfulAgonies 的第 1 条（方向相反）
 ────────────────────────────────────────────────
36 条 = 击退支（awayFrom != target ⇒ DoPushBack）   ← 全部 36 条
 0 条 = 抖动支（DoUnitShake）
```

**再筛一层「今天真的能播到」**（= 该 CardAnim 在运行期 `card_vfx_by_card.json` 里被至少一张卡引用）：

```
有效 36 条 / 35 个 CardAnim
  − 8 条 / 7 个 CardAnim（没有任何卡引用它们 ⇒ 今天不可达）
    Atk_BulletImpact_Tyranid_Pyrovore OLD · Atk_Gauss_Imotekh_AA · Atk_MegaBlasta ·
    Atk_Plasma_Pink OLD · Atk_TankShot · Shuriken_Short_Random · DelightfulAgonies(2 条)
──────────────────────────────────────────────
可达 = 28 条 / 28 个 CardAnim，全部 (t=30, aw=10) ⇒ 全部击退支
```
⇒ **接完线之后，玩家能看到的 = 28 个 CardAnim 上的「受击单位被弹开」**（每个桶都有，`attack_ranged` 占大头）。
`CardFeel.ResolveUnitShake` 的**抖动支**在本数据下**依然一次都不会走** —— 那不是接线漏了，是数据如此。

### 3·3 桶 × 件数（桶 = `Resources/card_vfx_by_card.json` 的 `events[].ev`；一个 CardAnim 可落多桶）

| 事件桶 | 桶内 distinct CardAnim | 其中带 `ush` | 占比 |
|---|---|---|---|
| `play` | 406 | 64 | 15.8% |
| `trigger` | 240 | 47 | 19.6% |
| **`attack_ranged`** | **215** | **127** | **59.1%** |
| `ability` | 69 | 16 | 23.2% |
| `trigger_unmapped` | 13 | 5 | 38.5% |
| **`attack_melee`** | **12** | **4** | **33.3%** |
| `trait_onplay` | 6 | 0 | 0% |
| `death` | 6 | 0 | 0% |
| `hit` | 1 | 0 | 0% |

- 全表 distinct `a` = **754**，其中带 `ush` = **205**；另有 **54** 个带 `ush` 的 CardAnim **不在运行期表里**（无卡引用）。
- ✅ 简报给的 **127/215** 与本次复算**逐字对上**；❌ 简报给的 **`attack_melee` 0 件** 不成立（见 §一·6）。
- ⚠️ 桶不能相加（一个 CardAnim 可同时属于 `attack_ranged` 与 `play`/`trigger`）。

### 3·4 典型值（每条都标「有效 / 空转」）

| CardAnim | `ush` | 备注 |
|---|---|---|
| `Atk_Gauss_Tesla`（`attack_ranged`+`trigger`） | `{t:30, aw:10, sz:0, du:0.4, dl:0.4}` | ✅ 有效 ⇒ **击退支**；与 `B9_W1BDR` 报告 §三·1 的样本逐字段一致 |
| `Atk_BulletImpact_Eldar_FusionGun` | `{t:30, aw:10, sz:2, du:0.5, dl:1.0}` | 唯一带 `sz=2` 的有效条目（击退支仍按 `aw≠t` 走，`sz` 只影响幅度 1.0） |
| `Atk_BulletImpact_2shot_autocannon`（`attack_melee`） | `{t:0, aw:30, sz:0, du:0.2, dl:0.1}` | 空转（`t=0`）—— `attack_melee` 那 4 件里 **3 件**是这种，只有 `Atk_Gauss_Large` 有效 |
| `Atk_BulletImpact_Ork Missile Big` | `{t:30, aw:10, sz:0, du:0.5, dl:1.0}` | 有效 ⇒ 击退 |

---

## 四、没查清的部分（如实记，⛔ 不许当成已完成）

1. **`CardFeel.ResolveUnitShake` 的第 ③ 步 `stillInPlay`** —— 接线时必须想清楚传什么：
   原版读 `cardState ∈ {2,3,15,17}`，我们视图侧**没有这一列**（`CardFeel` 已记这条，本轮未碰该文件）。
   传 `null` 会回退 `activeSelf`；这属于 W11/`B9_W2FEEL` 已裁过的口径，本轮**不重新裁定**。
2. **`tweenAnim`（`+0x80`）仍没有数据列** —— 245 个 CardAnim 有非空 `tweenAnim`，它是 `unitsShake` 的
   **if/else 另一支**。本轮只做 `unitsShake`，`tweenAnim` 仍是「判据在、数据无列、消费无」（`B9_W1BDR` 已记账）。
   ⇒ **接了 `unitsShake` 之后，那 245 件里「`ush` 为空」的部分仍然少一段演出**（⛔ 别以为接完就齐了）。
3. **32 个 CardAnim 生成器读不到**（`anim_address_map` 里没有 `sourceMono`/`targetName`）——
   其中 **2 个带 `unitsShake`**（`Rally_MasterOfExecutions`、`Self-Destruction_Effect`，**两条都是 `t=0` ⇒ 空转**）
   ⇒ **可观测损失 = 0**，但「读不到」≠「没有」。要不要给这 32 个补 32 行，属**另一条账**（涉及 `anim_address_map`，
   不在本代理白名单）。
4. **「击退支 vs 抖动支」在 `_d__468` 反向调用点上的方向**（`_d__438:803` 把 acting/target **对调**传参 ⇒
   击退方向相反）—— 本轮只核到「有这个反向调用点」，**没有逐卡核哪些卡走那一路**。接线时若只接主调用点，
   那几族（哨戒/反制）会缺这一段（`B9_W1BDR` 已单开一条账）。
5. **`minionAnimationClipInfo` 的 `m_FileID`**：两条数据都是 `m_FileID = 0`（= 同文件内、pathID 0 = null）。
   ⛔ 没有别的样本可交叉验证「非 null 时长什么样」，所以 `clip` 列只写 pathID 字符串、**不解析资产名**。

---

## 五、改动清单

| # | 文件 | 改法 |
|---|---|---|
| 1 | `Unity/工具/gen_cardanim_clips.py` | 新增 `_unit_shake_block`（→ `ush` 数组列，5 字段原样）· `_minion_clip_block`（→ `macEn/macD/macs[]`）· `build()` 调用点 · 两行统计打印 · 文件头 docstring 补一族。**+122 行 / −0 行** |
| 2 | `Unity/MyGame/Assets/CardPresentation/Resources/cardanim_clips.json`（生成物，`数据/游戏数据/cardanim_clips.json` 逐字节相同） | 重新生成：258,932 B → **286,919 B**（`+2,661 / −226` 行；`ush` **257 行**、`mac*` **2 行**）。`--check` = **OK**（连跑两遍逐字节相同） |
| 3 | `Unity/MyGame/Assets/CardPresentation/Battle/CardAnimClips.cs` | `+206 行 / −0 行`（全部**追加**；470 → 676 行）：`RtRow` 末尾 4 字段 + 两个 DTO（`RtUnitShake` / `RtMinionClip`）· `CardAnimPlacement` 末尾 4 字段 + `HasUnitShake` · `EnsureLoaded` 里 2 行映射（`+3 行`，含注释）· 类尾 `UnitShakesOf` / `BodyClipsOf` + `MapUnitShakes` / `MapBodyClips` · 类尾（文件末）两个公开结构体 `CardAnimUnitShake` / `CardAnimBodyClip` |
| 4 | `Unity/资料/完全复刻/账/_parts/B9_W12USHK.tsv` | 账补丁 3 行：**更新** `AnimInfo.GetAnimDuration()`（落点 329 → **418**、差异里 `:338` → `:427`）· **更新** `BattleManager.ResolveUnitShake(UnitShake unitShake)`（差异里「数据没有」改成「数据已有 / 仍 0 调用点」+ 两条硬事实）· **新增** `UnitShake(UnitShake.UnitShakeInfo, CardScript, CardScript)`（B：数据契约 + 枚举→卡那一跳的判据）。`merge_ledger_parts.py --dry --only B9_W12USHK` = 「更新 2 · 新增 1」 |

**验证**：`TMPDIR=/tmp/wf_w12 bash 工具/typecheck.sh`（四次复跑的**全部**观测，如实列）：

| 次序 | 结果 | 报错位置 |
|---|---|---|
| ① 本代理首个版本改完 | 运行时 **0** / 编辑器 **0** | —— |
| ② 加 `BodyClipsOf` 后 | 运行时 11 / 编辑器 0 | `Shell/DraftModeWindow.cs`(8, W8) · `Core/CardFeel.cs`(3, W11) —— 都是别人的中途态 |
| ③ 再复跑 | 运行时 8 / 编辑器 0 | 全部 `Shell/DraftModeWindow.cs`（W8） |
| ④ 最终（补齐 doc 后） | 运行时 **0** / 编辑器 1 | `Editor/BattleScene.cs:24176`（W11） |

🔑 **四次里 `CardAnimClips` 的命中数 = 0**（`grep -a -c CardAnimClips`）⇒ 我这份文件本身编得过；
残余全是别的代理正在写的文件（`CLAUDE.md` 13·3：错误全部集中在你不管的文件 ⇒ 不是你的问题）。
`python -I -X utf8 工具/gen_cardanim_clips.py --check` → **check: OK**。
`check_ledger_points.py` 对 `CardAnimClips.cs` 只报**一条漂移**（`GetAnimDuration` 账里 329 / 实际 418）——正是补丁第 4 行修的那条，
合并后即绿；其余红/漂移全部来自**别的代理在飞的活**（本代理未碰）。

**JSON 与 `RtRow` 的键对账**：JSON 里 83 个键中，`RtRow` 声明 55 个、其余 28 个是**故意不声明**的全量记录列（`mm*`/`ss*`）；
`RtRow` 的 55 个键**全部**在数据里出现过（无死列）。`ush` 内层键 = `{aw,dl,du,sz,t}`、`macs` 内层 = `{clip,d,sr,t,tf,wc}`，与 DTO 逐字一致。

---

## 六、顺手发现（⛔ 都没改，只是报）

1. 🔴 **简报的 `attack_melee` 桶「0 件」是错的**：现读 **4/12** 带 `ush`
   （`Atk_BulletImpact_2shot_autocannon` · `…_Bolter_Wide Area` · `Atk_Gauss_Large` · `Healing_Volley_Effect`）。
   4 条里 3 条 `t=0` 空转，只有 `Atk_Gauss_Large` 有效（⇒ 击退）。
   推测来源：`B9_W1BDR` 报的「attack_melee **0 件**」是 `shk`/`screenShake` 那一族，被串成了 `ush`。
2. 🔴 **「抖单位本体」在本数据下不存在** —— 36 条有效条目**全部**走击退支，其中今天可达的 **28 条 / 28 个 CardAnim**（§三·2）。
   这条会影响**验收方式**：接完线玩家看到的是「受击单位被弹开 / 后坐」，**不是抖动**；
   若按「抖动」验，会误判成没做对。
3. `CardFeel.cs:3806` 的 doc 说「数据在 `数据/游戏数据/card_anim_order.json` 的 `unitsShake` **现读 110 条**」——
   **数字对**（该文件 104 行 / 110 条），但那是**工作副本、不在 `Assets` 里**；`Resources/card_anim_order.json`
   **没有**这个字段。运行期真源现在是 `Resources/cardanim_clips.json` 的 `ush`（257 行 / 271 条）。
   ⇒ 建议 W11 顺手把那句 doc 改成指向 `CardAnimClips.UnitShakesOf`（**本代理不改 `CardFeel.cs`**）。
4. `minionAnimationClipInfo` 现读 **`enable==1` 的 0 个** —— 与 `账/战斗逻辑驱动.tsv:287,496` 两条 A 行的
   「卡体 clip 表在本 build 里全空」**互证**（本轮的 `macEn` 列把那条断言从「人工读数」变成「可机检」）。
5. 32 个读不到的 CardAnim 里，`Wait_1s / Wait_1,5s / Wait_2s / Wait_300ms / Wait_3s` 这 5 个是**纯计时拍**
   （无 prefab）—— 它们本来就不该有 `ush`；剩余 27 个是关键字/阵营类动画（`BlastAnim` 一族）。

---

## 七、下一路怎么接（一句话指令）

> 在 `BattleDriver` 拍子起播那一层（与 `PlayBeatShake` 同一处，拿到 `CardAnim` 名 `an` 与 `actor`/`target` 两张 `CardView` 之后），
> 写 `var p = CardAnimClips.PlacementOf(an); if (p.HasValue && p.Value.UnitShakes != null) { for (int i = 0; i < p.Value.UnitShakes.Length; i++) { var s = p.Value.UnitShakes[i]; var tgt = ResolveTargetUnit(s.TargetUnit, actor, target); if (tgt == null) continue; var away = ResolveTargetUnit(s.AwayFromUnit, actor, target); CardFeel.ResolveUnitShake(tgt, away, s.ShakeSize, s.ShakeDuration, s.ShakeDelay, /*stillInPlay 见 CardFeel doc ①*/ null); } }`
> —— 其中 `ResolveTargetUnit` 必须**逐字照** `BattleManagerSupport__GetTargetUnit.c:13-50`
> （`10 ⇒ actor` · **`20` 与 `30` 都 `⇒ target`** · `40 ⇒ 同阵营督军` · `50 ⇒ 敌方督军` · **`0`/其它 `⇒ null`**），
> ⛔ 不许给 `0` 兜底成 `target`（那样 **235/271 条**会从「什么都不做」变成「弹开 / 抖一下」= **凭空多出 235 次动作**，
> 而且不会有任何报错）。`s.Effective`（= `TargetUnit != 0`）就是给这个判断用的。
> **这一支与 `tweenAnim` 是 if/else**：`UnitShakes` 非空时**不**走 tween 那一支。
