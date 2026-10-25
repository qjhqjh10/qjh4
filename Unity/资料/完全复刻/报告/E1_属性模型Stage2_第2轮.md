# E1 · 属性模型 Stage 2（2a / 2b / 2c）+ ② 合批 + K1 复核 —— 第 2 轮

> 会话：第八十会话 · 完全复刻批次 1 · 执行代理 **E1**
> 白名单：`Assets/RuleEngine/**` + `资料/完全复刻/账/战斗逻辑驱动.tsv`
> （**追加授权**：主对话中途批准我修 `Assets/CardPresentation/Editor/BattleScene.cs` 里由本笔造成的 `CS0200`）
> 判据：`资料/完全复刻/判据/战斗逻辑驱动_前置门与现算口径.md` §六 / §七 · `判据/战斗逻辑驱动_AddEffect分派器.md` K1 ·
> 第一权威 `d:/2/tools/decomp_full/{EntityScript__get_CurrentMeleeAttack,EntityScript__get_CurrentRangeAttack,EntityScript__get_currentMaxHealthProcessed,EntityScript__get_CurrentIntrinsicHealth,EntityScript__get_CurrentIntrinsicMeleeAttack,CardScript__AddEffect,CardScript__AddArmour}.c`

---

## 一、结论

| 项 | 结论 |
|---|---|
| **Stage 2a** | ✅ 完成。`Attack/RangedAttack/MaxHealth` → `*Base` 字段改名 + 三条**只读转发属性**。编译器给出一份**完整写点清单**（运行时 20 条 + 编辑器 23 条，共 43 处），全部改完。 |
| **Stage 2b** | ✅ 完成。写点按语义分流：**43 处全部是「写 base」或「落修饰」**，没有一处把属性改回可写。 |
| **Stage 2c** | ✅ 完成。三条属性换成**投影**（`ProjectMeleeAttack` / `ProjectRangeAttack` / `ProjectMaxHealthProcessed`），修饰表 `UnitState._statMods` 按施加顺序保存；`blind ⇒ 远程 0` 与 `setMeleeAttack/ setRangeAttack 命中即 return` 都已进投影。 |
| **② 合批** | ✅ 完成。`setHealth(9)` 与 `MaxHealth` **两格并一格**（`IsSet` 修饰覆盖上限算式的 base 项、保留 extrinsic）；`70/85` 读时覆盖进投影；**摘 `addStats.health` 改成「夹到新上限」不是「减」**（判据 §7·4）。 |
| **③ K1 `addStats` 复核** | ✅ 复核完，**修了三处真差异**：① `health` 负值的 clamp（原版只夹不减）② `doubleAttack(20)` 的折算（**判据 §6·2·A 那条注是错的**，见 §三·1）③ `AddTempBuff` 的 `melee→attack` 归一（原来到期撤不掉）。armour 一路复核为**已等价、不用改**。**新记一条真缺口**：`doNotStack`（池内 22 条）的「同源不叠」没实现。 |
| **typecheck** | **0 / 0**（`TMPDIR=/tmp/wf_e1 bash 工具/typecheck.sh`） |
| **`ruleprobe check`** | **rc=0**（解析差异 0 行 · 哨兵失败 0 条） |
| **`ruleprobe play check`** | **rc=1** —— 主档 6 张 / 电池 7 张差异，**全部是同一类**：负攻值现在被 `clamp0`（原版语义）。基线文件编码的是旧行为 ⇒ **需在同步点重生成基线**（见 §五）。 |

**一句话**：「当前值」这一层从**物化字段**换成了**原版式的投影**，行为变化**只有两类**：
① 攻值不再为负（原版 `clamp0`）；② 判据 §7·2 那 11 组（`blind` / `setXxx` / 摘血 buff）——后者探针夹具没覆盖，靠 §四 的临时探针逐条读出来了。

---

## 二、判据（我照抄的值 + 抽核结果）

### 2·1 抽核过、且**与判据文件一致**的

| 值 | 判据原文 | 抽核 |
|---|---|---|
| `get_CurrentMeleeAttack`：正序扫；`buffType==1` 累加 `+0x48`；`buffType==70` **命中即 return**；扫完 `clamp0(+0x7c + Σ)` | `EntityScript__get_CurrentMeleeAttack.c:29-67` | ✅ 现读，逐句对上 |
| `get_CurrentRangeAttack`：**第一句** `HasCurrentTrait(0x3cf blind) ⇒ return 0`；其余同形 | `EntityScript__get_CurrentRangeAttack.c:24-70` | ✅ 现读 |
| `get_currentMaxHealthProcessed`：倒序两趟；`base = +0xa4 + Σ非extrinsic`；`setHealth(9)` **整项覆盖 base**；`+ Σextrinsic`；**全程无 clamp** | `EntityScript__get_currentMaxHealthProcessed.c:22-108` | ✅ 现读 |
| `addStats.health`：`healthChange >= 1 ⇒ currentHealth += healthChange`（**不向上夹**）；`< 1 ⇒ currentHealth = min(当前, currentMaxHealthProcessed)` | `CardScript__AddEffect.c:331-346` | ✅ 现读（这一段**判据文件只在 K1 一行里提过、摘要不准**，我按 `.c` 原文实现） |
| `doubleAttack(20)` **在分派前**改写成 `buffType=1` + `meleeAttackChange = CurrentMeleeAttack` | `CardScript__AddEffect.c:198-207` | ✅ 现读 |
| `setMeleeAttack(70)` / `setRangeAttack(85)`：**先 `RemoveActiveEffects(同 buffType)`** 再 `List.Add` | `CardScript__AddEffect.c:208-213` | ✅ 现读（**判据 §7·6 只写了「命中即 return」，没写这一句「先摘旧的」**） |
| `RemoveEffect`：`currentHealth = min(currentHealth, 新 currentMaxHealthProcessed)`（**只夹、不减**） | `RemoveEffect.c:147-181`（判据 §7·4 引用） | ✅ 沿用判据（本轮未重读该文件，**标注**） |

### 2·2 🔴 抽核发现**判据文件写错**的一处（已按 `.c` 原文实现）

`判据/战斗逻辑驱动_前置门与现算口径.md` **§6·2·A 的注**写：

> `EffectResolver.cs:13815` `t.Attack *= 2`（`DoDouble`）→ 改成「按 base 算新 base ⇒ 落 `ChangeBaseAttack` 等价」

**不成立。** `CardScript__AddEffect.c:198-207` 现读：

```c
if (*(int *)(param_2 + 0x38) == 0x14) {      // 0x14 = 20 = doubleAttack
    *(undefined4 *)(param_2 + 0x38) = 1;     // 改写成 addStats
    iVar16 = EntityScript__get_CurrentMeleeAttack(param_1,0);
    if (iVar16 < 0x3fffffff) {               // 溢出保护
        uVar7 = EntityScript__get_CurrentMeleeAttack(param_1,0);
        *(undefined4 *)(param_2 + 0x48) = uVar7;   // +0x48 = meleeAttackChange
    }
}
```

⇒ 原版「翻倍」= **落一条 `addStats` 修饰、值 = 施加那一刻的 `CurrentMeleeAttack`**，**不是**写 base。

我第一版就是照那句注写的（`AttackBase *= 2`），`ruleprobe play check` 当场把 `EC30`（9 base + 1 修饰）从 **20** 变成 **19** —— 正好是方案 §三·9 说的「实现方按判据写代码会把判据的错暴露出来」。**已改成 `.c` 原文口径**，`EC30` 回到 20，判据差异归零。
📌 **给 Q1/Q3**：这条注请改；`前置门 §6·2·A` 的 `13815-13817` 三行都要按 `.c:198-207` 重写。

---

## 三、改动清单（`文件:行号`）

### 3·1 `Assets/RuleEngine/Core/UnitState.cs`（LF · 1646 → 1927 行）

| 行 | 改动 |
|---|---|
| `:53-58` | 字段改名：`Attack→AttackBase`、`RangedAttack→RangedAttackBase`、`MaxHealth→MaxHealthBase`（`Armor` / `Health` 不动 —— `Health` = 原版 `currentHealth` 就是存档，判据 §6·2·C） |
| `:119 / :138 / :165 / :177 / :193` | 五条 Stage-1 投影的 getter 改成转发 `Project*()`（**行数不变**，只换一行体） |
| `:888-891` | 构造：`AttackBase = card.Attack` / `RangedAttackBase = …` / `MaxHealthBase = Health` |
| `:1243-1250` | `GrantRecord` 加 `public StatMod Mod;`（指向修饰表那一格；`armour` 为 null） |
| `:1264` | `RecordGrant` **改成返回 `StatMod`**，并建修饰（`Extrinsic = IsExtrinsicSource(source)`） |
| `:1280` | 新增 `static bool IsExtrinsicSource(string)` —— 只认 `Auras.GrantTag`（`Aura.cs:180-181`：光环就是 `WhileInPlay(20)` 那一族） |
| `:1310` | `ApplyGrant` 重写：`attack/ranged` 不再写字段（只 `RemoveStatMod`）；`health` 正加 `Health += v`、撤销**只夹不减**（`ClampHealthToMax`） |
| `:1542-1560` | `TempBuff` 加 `public StatMod Mod;`；`AddTempBuff` 在「非关键词且没有 Mod」时**自建修饰** |
| `:1566` | `RevertBuffs`：attack/ranged/health → `RemoveStatMod(b.Mod)`（health 另 `ClampHealthToMax()`）；`armour` 仍走 `RemoveKeyword` |
| `:1622` | `RemoveBuffsFromCard` 同上 |
| `:1730` | 新增 `public class StatMod { Attr; Value; Extrinsic; IsSet; Source; }` |
| `:1759` | 新增 `public StatMod AddStatMod(attr, value, source=null, extrinsic=false, isSet=false)` —— **`isSet` 时先摘同属性旧 set 记录**（`.c:208-213`） |
| `:1782 / :1789 / :1804` | 新增 `RemoveStatMod` / `RemoveStatModsFrom` / `ClampHealthToMax` |
| `:1815 / :1818 / :1824` | **三条投影属性**（`Attack` / `RangedAttack` / `MaxHealth`）—— 名字与旧字段逐字相同 ⇒ 全工程几百个读点一个字没改 |
| `:1832 / :1853 / :1876 / :1901 / :1916` | 五个 `Project*()` 实现（逐句照那五份 `.c`） |

⚠️ **行号漂移**：`UnitState.cs` 本笔**净增 281 行**（大头是类尾那 200 行），中部只动了 4 处（`:53` +4 行、`:1243` +1、`:1542` +12、`:888` 0），**账里指 `UnitState.cs` 的落点需要同步点按名字重定位**（`check_ledger_points --all-A --fix`）。

### 3·2 `Assets/RuleEngine/Core/EffectResolver.cs`（CRLF · +约 90 行）

| 行 | 改动 |
|---|---|
| `:11474` | `ApplyOneGain` 新增局部 `UnitState.StatMod appliedMod = null;` |
| `:11528` | `grantSource != null` 那条路改成 `appliedMod = u.RecordGrant(...)` |
| `:11541 / :11544 / :11555` | `attack` / `ranged` / `health` 三格：**落修饰**（限时那一种交给下面的 `AddTempBuff` 建，避免一次施加记两条） |
| `:11547-11555` | `health` 正加才 `Health += val`（原版不向上夹） |
| `:11650-11667` | 限时那条 `TempBuff`：`Name` 过 `NormalizeAttr`（**修掉 `melee` 到期撤不掉的老 bug**）+ `Mod = appliedMod` |
| `:11672-11678` | **`addStats.health` 负值那一支**：`u.ClampHealthToMax()`（必须放在 `AddTempBuff` 之后） |
| `:14143` 起 | `DoSetHealth` 按 `.c:794-820` **重写**（记 `IsSet` 修饰 → `Health = MaxHealth`；**去掉**「只降不升」「查 Invulnerable」两条老近似；限时那份挂 `TempBuff.Mod`） |
| `:14242-14272` | `DoDouble` 按 `.c:198-207` 改成 `AddStatMod("attack", 当前近战)` / `("ranged", 当前远程)` / `("health", 当前上限)` + `Health = 当前×2` |

### 3·3 `Assets/RuleEngine/Core/RuleCore.cs`（LF）

| 行 | 改动 |
|---|---|
| `:638` | 战役督军生命：`MaxHealthBase`（**改 base 上限**，原版 `SetupWarlordsLifeAndEnchantment`） |
| `:4489-4494` | 蜂群合并：`host.AddStatMod(..., "虫群合并")` × 3 + `host.Health += just.Health`（判据 §6·2·A 注拍板：**落一条带来源的修饰**） |
| `:9475-9476` | 残骸对象初始化器：`AttackBase = 0, RangedAttackBase = 0, Health = 1, MaxHealthBase = 1` |

### 3·4 `Assets/RuleEngine/Editor/RuleEngineTest.cs`（CRLF · 3 处夹具）

`:28852`（督军 `MaxHealthBase = 999`）· `:36691`（`atk.AttackBase = 5`）· `:36764`（`zero.AttackBase = 2`）

### 3·5 `Assets/CardPresentation/Editor/BattleScene.cs`（CRLF · **23 个 `CS0200`，全部「写 base」**）

| 行 | 档 | 说明 |
|---|---|---|
| `:11677` | base ×1 | 探针 `victim.RangedAttackBase = 4` |
| `:13900` | base ×2 | 夹具对象初始化器 `AttackBase / MaxHealthBase` |
| `:16690` `:16692` | base ×3 + ×3 | 同上（两个相邻单位） |
| `:20263` | **读**（不是赋值） | `svA = u.AttackBase; svR = u.RangedAttackBase;` —— 与下面「还原」对称（**这一行不在 `CS0200` 里，是我顺带改的**，否则会「存投影值、写回 base」） |
| `:20266` | base ×2 | 探针「人为造出能打」`Mathf.Max(1, u.AttackBase)` |
| `:20273` `:20285` | base ×2 + ×2 | 还原 |
| `:40554` | base ×2（`MaxHealthBase`） | 自检夹具 `uA.Health = uA.MaxHealthBase = 10` |
| `:40643` | base ×1 | `uC4.Health = uC4.MaxHealthBase = 20` |
| `:41172` | base ×2 | 同 `:40554` |
| `:46882-46883` | base ×3 | 合成残骸夹具（`AttackBase/RangedAttackBase/MaxHealthBase`） |

**汇总**：`Editor/BattleScene.cs` 共 **23 个** `CS0200` 赋值点，**全部属「base」档，0 个属「修饰」档**（全是探针/夹具在**造一个指定 base 的单位**）。
行尾：`crlf=47119 / lf=47119`（改前改后一致，没翻）。

### 3·6 `资料/完全复刻/账/战斗逻辑驱动.tsv`

`:771`（`CardScript.AddEffect`，备注列）追加本笔完整说明 · `:969`（`RemoveEffect`，差异列）更新为「属性类现在按记录摘」· `:551` / `:552`（`ResolveChangeBaseAttack/MaxHealth`）追加指向 `:771` 的注。**列数校验**：全表 0 行 ≠ 11 列。

---

## 四、断言 / 读数

### 4·1 探针（可复现）

**改动前快照**：`_tmp_view/e1_before/{check.txt, play_check.txt, play_all.tsv}`（`play_all.tsv` md5 = `351c95043ba194dd7c6026de94ac14f6`）

```
ruleprobe check        → rc=0 · 解析差异 0 行 · 哨兵合计失败 0 条
ruleprobe play all dump → 全池 1128 张逐卡表
                       改动前 md5 351c9504…  改动后 md5 2922d505…
                       **只有 4 行差异（= 2 张卡）**：
                         EC27 Rugged Disharmonist    selfPlace 3→0  other 3→0  changes 8→5
                         Tempest's Wrath             selfPlace 1→0  other 1→0  changes 3→2
                       ⇒ 两张都是「给 -2 [Ranged]」而夹具的目标远程 = 0 ⇒ 以前记成 -2，现在 clamp0 成 0
```

**`ruleprobe play check` rc=1 的逐卡解释**（主档 6 张 / 电池 7 张，**全部同一类**）：

| 卡 | 卡面（`cards_engine.json` 现读 `desc`） | 变化 |
|---|---|---|
| `EC1 Sonic Blaster Noise Marine` | `…give them -1 [Attack] and -1 [Ranged]` | 远程/近战 `-1` → `0`（clamp） |
| `EC3 Disharmonist` | `Give -2 [Attack] and -2 [Ranged] …` | 同上 |
| `EC14 Varius, Lord Kakophonist` | `Stimulation: Give -1 🗡 and -1 🔫 …` | 同上 |
| `EC16 Screamer Kakophonist` | `Give -4 [attack] and -4 [Ranged]` | 同上 |
| `EC27 Rugged Disharmonist` | `Enemy troops have -2 [Ranged]` | 同上（光环那条路） |
| `EC32 Daemonette` | `Give -1 [Attack] and -1 [Ranged]` | 同上 |
| `EC33 Alluress` | `Give -2 [Attack] and -2 [Ranged]` | 同上 |
| `Tempest's Wrath` | `Give -2 [Ranged] … until your next turn` | 同上 |

⇒ **没有一张的差异是「少算了某个修饰」**；全部是「**攻值下限从 −N 变成 0**」。这是原版 `get_CurrentMeleeAttack.c:52-66` / `get_CurrentRangeAttack.c:50-64` 的 `clamp0`，**以前负攻值是真的负**（会在 `RuleCore.FieldAttack` 里**给目标回血**）。**建议：同步点重生成 `基线/out_play_baseline.txt`**（本批唯一的行为变化，已逐条解释）。

### 4·2 四条观测量（判据 §6·4）—— 临时探针 `_tmp_view/e1_probe/`（不进工程/不进 git）

`cd d:/4/Unity/_tmp_view/e1_probe && dotnet run -c Release`（纯 dotnet，编 `RuleEngine/Core/*.cs` + ruleprobe 的 `Stub.cs`）**实测输出**：

| # | 观测量 | 实测 |
|---|---|---|
| ① | `setMeleeAttack(70)` 覆盖 + 命中即 return | 起始 3 → 挂 set(7) 后 **7** → 再叠 `+1 melee` **仍 7** → 再挂 set(6) **6**（先摘旧的） |
| ② | `setCurrentHealth(100)` 不夹上限 | 先压到 2/2 → 设 100 ⇒ **生命 100** |
| ③ | `setHealth(9)` 覆盖 base、保留 extrinsic | 4/4 → 挂 set(9) ⇒ 上限 **9** → `Health = MaxHealth` ⇒ **9** → 叠 extrinsic +2 ⇒ **11/11**，`CurrentIntrinsicHealth` **9** |
| ④ | `blind ⇒ 远程 0` | 远程 base5+1=6 → 上 blind ⇒ **0**，近战不变（2），`RangedAttackBase` 仍 5 |
| ⑤ | 摘掉 `+2 health` 是「夹到新上限」 | 4/4 → +2 ⇒ 6/6 → 挨 3 ⇒ 3/6 → 摘 ⇒ **3/4**（旧口径 = **1/4**） |
| ⑥ | `addStats.health` 负值只夹不减 | 4/6 吃 −2 ⇒ **4/4**；满血 4/4 吃 −2 ⇒ **2/2** |
| ⑦ | `doubleAttack(20)` = 落 `addStats` | 9 base +1 修饰 ⇒ 翻倍前 10/5 ⇒ **翻倍后 20/10**（旧「base×2」会得 **19/10**） |
| ⑧ | 恒等（无修饰/无 blind） | `Attack 4==4 · RangedAttack 3==3 · MaxHealth 7==7` |

### 4·3 typecheck

`TMPDIR=/tmp/wf_e1 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
（过程中出现过别的代理的瞬时错：`Shell/LiveOps*.cs`（E2）、`Shell/SaveCenter.cs`（E3）、`Editor/TraitEffectAssert.cs`（E5）、`Editor/NetBattleTest.cs`、`Editor/ShellScene.cs` —— **全部不是我负责的文件**，按 §13·3 没去改，收尾时它们自己消失了。）

---

## 五、没查清 / 仍存的缺口（都已入账，⛔ 不是「不做」）

1. **`meleeAttackValueOptions` 折成 `meleeAttackChange` 的时机** —— 派出给我的简报说 Q1 会产出 `判据/战斗逻辑驱动_Stage2前置_第2轮.md`，**到收尾时该文件仍不存在** ⇒ 按简报口径**按「施放那一刻折 = 快照」推进**（`DoDouble` 就是这个形状）。
   ⚠️ **SAU27 `Scarab Swarm`（池内唯一 `buffType 70`）没走这条路**：它由 `CardDef.MeleeEqualsHealth` 特判，**只在 `RuleCore.FieldAttack`（伤害那一跳）读 Health** ⇒ 卡面 / 条件 / AI 看到的**不是** Health（原版 `CurrentMeleeAttack` 处处都是 Health）。**这是真缺口**（判据 §7·2 同族），要等 `meleeAttackValueOptions` 的折法定案 + 一个「Health 变 ⇒ 重挂」的刷新点。**我没动**，因为按「快照」实现会让这张卡的技能**彻底失效**（比现状更差）。
2. **`doNotStack`（池内 22 条）没实现** —— 原版合并条件里有一条 `param_2 + 0x75 == 0`；它的真实语义（同源不叠 / 不合并）我们没有。我们按条独立记账，在**求和**上等价，但「重复施加同一份」的**条目数**会不同。判据 = `CardScript__AddEffect.c:385-402`。
3. **`extrinsic` 只覆盖了 `trigger 20`（光环那条路）** —— §7·1 判据是 `trigger ∈ {20,240}`；`240 WhileInPlayVariable` 我们**没有单独建模**（`IsExtrinsicSource` 只认 `Auras.GrantTag`）。⇒ `CurrentMaxHealthProcessed` 的**总量**对（base+Σ 两项一起算），但 `CurrentIntrinsicHealth` / `CurrentIntrinsicMeleeAttack` 在 240 那 36 条效果上会多算。**这两个投影今天零调用者**，所以还没有可观察量。
4. **`CurrentIntrinsicRangeAttack` 仍没有** —— 原版那个方法体没读（判据 §3·5 已如实标「未证」），不凭对称猜。
5. **`NetProtocol` 的局面哈希**读的是 `ps.Warlord.Attack/RangedAttack`（`NetProtocol.cs:625-626`）—— 现在读到投影。**值在今天不变**（督军身上没有 `blind` / `set` 修饰），但一旦新语义生效，**新旧两版客户端的哈希会不同**。
   📌 **改动请求**：`NetProtocol` 的**协议版本号**那一格 +1，并在握手处写清「版本不一致 ⇒ 拒绝开局」。`NetProtocol.cs` **不在我的白名单**，我没碰。
6. **`_statMods` 的条目顺序**在 `Auras.Recompose` 之后会变（先摘后加 = 挪到队尾）。对 `addStats` 无影响；对 `setXxx` 也没影响（`AddStatMod(isSet:true)` 会先清同属性旧 set 记录 ⇒ 永远只有一条）。**如实记着**，不是缺口。
7. **`setMeleeAttack(70)` / `setRangeAttack(85)` 仍无「数据生产者」** —— 投影这一层已经能吃它们（`AddStatMod(..., isSet:true)`），但从卡文本到 `IsSet` 修饰的**解析层那条链没建**（池内 70 只有 1 张且走特判、85 是 0 张）。K8 当时记的就是「只记录不实现」，本笔把**读侧**补齐了，**写侧**仍缺。

---

## 六、顺手发现（⛔ 我没顺手改）

| # | 发现 | 判据/证据 | 建议 |
|---|---|---|---|
| F1 | **`ApplyOneGain` 的 `TempBuff.Name` 没走 `NormalizeAttr`** —— `+1 Melee this turn` 记的名字是 `"melee"`，而 `RevertBuffs` 的 `switch (b.Name)` 只有 `"attack"/"ranged"/"health"/"armour"` ⇒ **到期什么都不撤**（静默「限时变永久」） | `EffectResolver.cs:11652`（改前）· `UnitState.RevertBuffs` 的 switch | **本笔已修**（`Name` 过 `NormalizeAttr`）。⚠️ 但**没有**逐一核「池内有多少张限时 `+N Melee` 卡曾经中招」—— 有可观察量，值得单开一条普查 |
| F2 | **`DoSetHealth`/`DoDouble`/`DoBlind`/`DoStun` 仍用 `!t.IsAlive` 当「在场」判据**，比原版严（原版只在 `cardState ∈ {5,6,11}` 拒） | N11 报告已记（`:14015` / `:13941` / `:4139` / `:4052`） | 仍是一条独立欠账（`!t.IsAlive` 比原版严），**本笔没动**（不在 Stage 2 范围） |
| F3 | `CardContext`/`CostMod` 那套 `setCost(95)` **本轮零改动**（K8 已做） | — | 无 |
| F4 | **E5 提的两条配合请求（W9 / W10）**：<br>· **W9**：需要 `RuleEngine/Core/BattleEvent.cs` 增一个「trait 被摘掉」的 `EvtKind`（`RuleCore.cs:6884` 的 `RemoveAll` 当场不 emit），且**必须与 F2（原版留到回合末才摘）同批**<br>· **W10**：需要「攻击起手 / 结束」两拍事件（现有 `EvtKind.Attack` 是**结算那一拍**） | 主对话转达 | **本轮按指示只记账、不做**。W9 与 F2 有耦合，建议同批派 |
| F5 | `EffectResolver.cs` 里 `DoSetHealth` 的 `EmitHit`（表现口）与 `DoSetCurrentHealth`（不发事件）**是两条不同的表现口径** —— 我在重写时保留了 9 的那条 `EmitHit` 并注明「这是我们挑的」 | `.c:794-820` 不经过 `ReceiveDamage` | 归表现层，HUD 线可核 |

---

## 七、交接给下一个代理

### 做到哪
- **Stage 2a / 2b / 2c + ② 合批 + K1 复核 全部落地**，`typecheck 0/0`、`ruleprobe check rc=0`。
- 探针基线**未重生成**（本批唯一行为变化 = 攻值 `clamp0`，8 张卡已逐条解释）⇒ **`play check` 现在 rc=1 是预期的**。

### 剩什么（按建议顺序）
1. **同步点**：重生成 `工具/ruleprobe/基线/out_play_baseline.txt`；`check_ledger_points --all-A --fix` 收 `UnitState.cs` 的 281 行漂移；重钉解析指纹（`ruleprobe check` 的 ⑦ 项现在仍是 `193c1b25…`，本笔**没改解析层**，应仍可复用）。
2. **`NetProtocol` 协议版本 +1**（§五·5）—— 要主对话派人（不在 E1 白名单）。
3. **`MeleeEqualsHealth` / `meleeAttackValueOptions`**（§五·1）—— 等 Q1 的判据，别自己发明。
4. **`doNotStack`**（§五·2）—— 纯数据 + 纯逻辑，可单独派。
5. **W9 / W10 事件**（§六 F4）—— W9 与「`!t.IsAlive` 比原版严」同批。

### 文件动到哪
```
Assets/RuleEngine/Core/UnitState.cs           (+281)
Assets/RuleEngine/Core/EffectResolver.cs      (~+90)
Assets/RuleEngine/Core/RuleCore.cs            (3 处)
Assets/RuleEngine/Editor/RuleEngineTest.cs    (3 行)
Assets/CardPresentation/Editor/BattleScene.cs (23 个赋值点 + 1 个读点；主对话追加授权)
资料/完全复刻/账/战斗逻辑驱动.tsv              (4 行)
_tmp_view/e1_probe/{e1probe.csproj,E1.cs}      (临时探针，不进 git)
_tmp_view/e1_before/*                          (改动前快照)
```

### 下一步命令
```bash
# 秒级类型检查
TMPDIR=/tmp/wf_e1 bash d:/4/Unity/工具/typecheck.sh          # 期望 0/0

# 观测量（纯 dotnet，不跑 Unity）
cd d:/4/Unity/_tmp_view/e1_probe && dotnet run -c Release

# 引擎对拍
bash d:/4/Unity/工具/ruleprobe.sh check                       # 期望 rc=0
bash d:/4/Unity/工具/ruleprobe.sh play check                  # 现在 rc=1（预期，见 §4·1）
```

### 踩过的坑（新攒的）
1. 🔴 **判据文件本身会错** —— `§6·2·A` 那条 `DoDouble` 注是错的（`§二·2`）。**先抽核 `.c` 再写代码**，这次正是探针把错暴露出来的。
2. 🔴 **`ApplyOneGain` 的「限时」那一支如果要落修饰，只能在 `AddTempBuff` 里建一次** —— 两处都建会**数值翻倍且到期只摘得掉一条**。
3. 🔴 **`health` 的负值 clamp 必须放在 `AddTempBuff` 之后** —— 否则夹的时候修饰还没进表，夹的是旧上限。
4. 🔴 **`BattleScene.cs` 是 CRLF**：`python` 里用 `\n` 拼多行 `old_string` **匹配不到**（单行能匹配）。要么带 `\r\n`，要么只做单行替换，改完数 `b.count(b'\r\n')` vs `b.count(b'\n')`。
5. ⚠️ **`git diff --numstat` 在多代理并行时不可用**（本笔全程改用「字节级行尾计数 + 逐处 `assert count==N`」）。
6. ⚠️ **改 `UnitState.cs` 会整体推漂** `账/战斗逻辑驱动.tsv` 里指这个文件的落点 —— 本笔把新成员**全部追加到类尾**（`_statMods` / 五个 `Project*` / 三条属性），中部只动了 4 处；剩下的漂移交给同步点的 `--fix`。

---

# 第二段（同会话续做）· ① `doNotStack` 复核 + ② `extrinsic` trigger 240 建模

> 授权范围不变：`Assets/RuleEngine/**`。判据源按主对话要求**以 cpp2il 为准**（`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**.cs`），
> `decomp_full/**.c` 只做交叉。

## 一、结论

| 项 | 结论 |
|---|---|
| **① `doNotStack`（池内 22 条）** | ✅ **复核完成 —— 上轮把它记成「真缺口」是错的，已按铁律 5 就地订正（账 `:771` + 代码类注）。** 真实语义 = 「**不与该单位已有的同源同计时记录合并**」；**数值上合并与不合并完全一样**（合并 = 累加），差别只在「按记录摘」的粒度；而原版唯一按记录摘的场合（手牌效果）**本身就强制 `doNotStack = true`**。我们的模型**从不合并** ⇒ 等价于永远 `doNotStack = true` ⇒ **零代码改动、不构成缺口**。 |
| **② `extrinsic` trigger 240** | ✅ **建模完成（结构级）**：新增 `CardDef.IsExtrinsicAbilityTrigger(int)`（`trigger ∈ {20,240}`）与 `CardDef.ExtrinsicAbilityPresence`（卡级三档 `1/0/-1`）；`UnitState.IsExtrinsicSource` 的 doc 重写并**升为 `public`**，写清「运行时刻判据 = 光环那条路」以及「240 与 20 走的是同一套正文形状」。🔴 **零调用者 / 零可观察量**（详见 §三·4），**按铁律 11 照样建模**。 |
| 回归 | `typecheck` **0/0** · `ruleprobe check` **rc=0**（解析指纹 `193c1b25…` 未变）· `play all dump` **与本会话上一轮逐字节相同**（`diff` = 0 行）⇒ **本段零行为变化**。 |

## 二、① `doNotStack` —— 判据、真实语义、为什么我们天然满足

### 2·1 判据（cpp2il 为主，`.c` 交叉）

| 事实 | 出处 |
|---|---|
| **字段定义** | `CardEffect.cs:60` `public bool doNotStack;` |
| **拷贝**（克隆效果时原样带过去） | `CardEffect.cs:133`（`GetNewInstanceOfThisEffect`） |
| **🔴 全库唯一的写点** | `PlayerHand.cs:2507` —— `AddHandEffect(HandEffect)` 里在 `activeEffects.Add` **之前**写 `cardEffect3.doNotStack = true;` |
| **全库唯一的读点** | `CardScript.AddEffect` 的 `addStats / changeCost / changeAbilityCost` 合并循环里那一条 `*(char *)(param_2 + 0x75) == '\0'`（`CardScript__AddEffect.c:331-402`；cpp2il 版的 `AddEffect` 这一段**没重建出来**，只有 `.c` 有） |
| **字段偏移核对**（`.c` 的 `+0x75` 到底是不是它） | 按 `CardEffect.cs` 的字段序 + 64 位对齐算：`+0x38 buffType` · `+0x48 meleeAttackChange` · `+0x50 rangeAttackChange` · `+0x58 armourChange` · `+0x60 healthChange` · `+0x68 costChange` · `+0x70 randomValueChange` · `+0x74 permanentBuff` · **`+0x75 doNotStack`** · `+0x78 markOfChaosType` · `+0x7c daemonHostType` · `+0x80 mutationType` · `+0x88 enchantment` · `+0xa4 enchantmentCardId`（`.c` 里这六个偏移逐个出现过，**全部对上**） |

### 2·2 合并键（`.c:331-402` 逐条）

`enchantmentCardId(+0xa4)` 相等 ∧ `buffType(+0x38)` 相等 ∧ `markOfChaosType(+0x78)` 相等 ∧
已有记录的 `daemonHostType(+0x7c) == none` ∧ 已有记录的 `mutationType(+0x80) == none` ∧
**新记录的 `doNotStack(+0x75) == 0`** ∧ `SameTimingAsEffect(新)`
⇒ **命中就把四个增量字段累加进已有那条**（`+0x48 / +0x50 / +0x60 / +0x68`；⚠️ **`armourChange` 不在累加之列** ——
它无条件走 `AddArmour`，见判据 §二 第 3 条），未命中才 `List.Add`。

> ⚠️ 注意：`CardEffect.IsSameEffect`（`CardEffect.cs:200-…`）是**另一条**判据（extrinsic 去重树用的），
> 它**连数值一起比**（`meleeAttackChange` 等 6 个数值 + 4 个 `ValueOptions`）⇒ **和这里的合并键不是一回事**，
> ⛔ 别互相套用。

### 2·3 结论：**不是缺口**

1. **数值**：合并 = 累加 ⇒ 求和逐字节相同。
2. **摘除粒度**：合并后「摘一条」= 把几次贡献一起摘掉；我们按条独立、摘哪条掉哪条。
3. 而原版**唯一**会「按某一条记录摘」的场合就是**手牌效果**（`PlayerHand` 那一层），
   **而那一层恰好被强制 `doNotStack = true`** ⇒ **原版也不合并** ⇒ 与我们的行为一致。
4. ⚠️ **`cards_truth.json` 里那 22 条 `doNotStack` 是序列化残留**（运行时才由 `AddHandEffect` 覆盖），
   ⛔ 不能读成「池内 22 条会不叠」—— 与 `extrinsic` / `extrinsicVariable` 那两列**同一个坑**。

### 2·4 可观察量（实测，`_tmp_view/e1_probe2`）

```
起始 近战 = 3（base 3）
连加两次 +1 近战 ⇒ 近战 = 5；条目数 = 2        ← 原版默认会合并成 1 条 +2
摘掉第一条        ⇒ 近战 = 4                   ← 原版合并时摘掉那条 = 把 +2 全摘 ⇒ 3
再摘第二条        ⇒ 近战 = 3                   ← 两边一致
```
**池内前后读数**：`play all dump` 与上一轮**逐字节相同**（0 行差异）⇒ **卡池里没有任何一张卡能看出差异**（结构原因见 2·3）。

### 2·5 代码侧落点（**只加注释，零逻辑改动**）

`UnitState.StatMod` 的类注（`Core/UnitState.cs` 类尾那一段）新增一整节，写清：合并键（含字段偏移表）·
`doNotStack` 的唯一写点 · 为什么我们「从不合并 = 永远 doNotStack」· 22 条是序列化残留 · 实测读数。

## 三、② `extrinsic` trigger 240

### 3·1 判据

`extrinsic = (ability.trigger ∈ {20 WhileInPlay, 240 WhileInPlayVariable})`；`extrinsicVariable = (trigger == 240)`。
写点 `RawCardScript.TriggerAbility`（`RawCardScript.cs:1770-1781`），落点 `AbilityLogic__PlayAbility.c:1193-1199` / `:1352-1356`。

### 3·2 做了什么

| 落点 | 内容 |
|---|---|
| `CardDef.IsExtrinsicAbilityTrigger(int)` | **新增**（`Core/CardDef.cs`，`CardDef` 类尾）：`return trigger == 20 || trigger == 240;` |
| `CardDef.ExtrinsicAbilityPresence` | **新增**：卡级三档 `1` 有 / `0` 明确没有 / **`-1` 未知**（缺 `abilityTriggers` 列 —— ⛔ 未知不许读成「没有」，与 `AbilityTriggerPresence` 同口径） |
| `UnitState.IsExtrinsicSource` | doc 重写 + **`static` → `public`**（给自检/探针看）：写清「运行时刻判据 = `Auras.GrantTag`」= while-in-play 的载体，20 与 240 **同一套正文形状**；并如实列出 extrinsic 的**三个**影响面与「今天全都不可观察」 |

⚠️ **这是卡级近似**：同一张卡可以同时有 `Rally(30)` 与 `WhileInPlay(20)`，而我们的效果**按正文句子**结算、
不记「这句话来自哪条 ability」⇒ 卡级 flag **不能**逐句判 extrinsic；逐条判只能看它走的是哪条授予路。

### 3·3 覆盖率实测（为什么「光环那条路」= trigger 20 **和** 240）

`_tmp_view/e1_probe2`（读 `cards_engine.json`，逐句喂 `Auras.TryParse`）：

| trigger | 卡 | 光环形状句 | 被 `Auras.TryParse` 收下 | 没收的都是什么 |
|---|---|---|---|---|
| **20** | 13 张 | 19 段 | **11 段** | 印刷关键词（`Armour 1` ×3 · `Vanguard` · `Shield` · `Destroyer`）+ 一条**别的 trigger** 的一次性句（`When a friendly unit uses Ferocity, it stays in play`）+ `UM93 Oath abilities … may be activated on later turns`（原版是静态规则） |
| **240** | 33 张 | 42 段 | **27 段** | 印刷关键词（`Armour 2` · `Flying` · `Regeneration 3` · `Vanguard` · `Shield` · `Sentry 4`）+ 同卡**其它 trigger** 的一次性效果（`Give Vulnerable 2 …` · `Deal 6 damage …` · `Deploy a Fenrisian Wolf …`）+ 两条「costs 1 less for each …」静态降费（走 `CostMod`、不是三围修饰）+ `UM106 Oath … 3 times`（静态规则） |

⇒ **没有一条「带三围数值的 while-in-play 效果」落在光环层之外** ⇒ `Auras.GrantTag ⇒ extrinsic` 这条映射今天**不漏**。
（另有 8 条 `buffEffect` 属于 trigger 240 下的 `changeCost`，它们走 `CostMod`，**本来就不进 `_statMods`**，与 extrinsic 无关。）

### 3·4 可观察量 —— **没有，如实记**

`extrinsic` 只影响三处：

| 影响面 | 今天可观察吗 |
|---|---|
| `CurrentMaxHealthProcessed` 里「`setHealth(9)` 只覆盖**非 extrinsic** 的 base」那一支 | ❌ 池内 `setHealth(9)` **0 张** |
| `CurrentIntrinsicHealth` | ❌ **零调用者** |
| `CurrentIntrinsicMeleeAttack` | ❌ **零调用者** |

⇒ **本段无法用行为验证**，只有**结构读数**（下面是探针实测）：

```
CardDef.IsExtrinsicAbilityTrigger(20/240/30) = True/True/False
修饰 health +2  source=光环  Extrinsic=True      ← 光环那条路
修饰 health +1  source=blood Extrinsic=False     ← 黑暗契约那条路
生命=7 上限=7（4+2+1=7）· CurrentIntrinsicHealth=5（生命 7 − extrinsic 2 = 5）
```
✅ `CurrentIntrinsicHealth` 正确地把 extrinsic 那 2 点排除了 —— 这是「建模生效」的唯一硬读数。

### 3·5 回归

- `ruleprobe check` **rc=0** · 解析指纹 **`193c1b25…` 未变**（⇒ `CardDef` 的解析行为没动）。
- `ruleprobe play all dump` **与上一轮逐字节相同**（diff 0 行）⇒ **零行为变化**（符合预期：extrinsic 只影响两个零调用者 + 一个 0 张的档）。
- `play check` 的 6 + 7 张差异**与本段无关**（是上一段那个「负攻值 `clamp0`」，见主报告 §4·1 的 8 张卡）。

## 四、改动清单（第二段）

| 文件 | 行 | 改动 |
|---|---|---|
| `Assets/RuleEngine/Core/CardDef.cs` | `CardDef` 类尾（`ToString()` 之后、类的 `}` 之前） | **新增** `IsExtrinsicAbilityTrigger` / `ExtrinsicAbilityPresence`（含判据与「卡级近似」的说明） |
| `Assets/RuleEngine/Core/UnitState.cs` | `StatMod` 类注 | **新增**整节 `doNotStack` 判据（合并键 / 字段偏移表 / 唯一写点 / 为什么我们天然满足 / 22 条是序列化残留 / 实测读数） |
| `Assets/RuleEngine/Core/UnitState.cs` | `IsExtrinsicSource` | doc 重写 + `static` → **`public`**；写清 20/240 同形、三个影响面、零可观察量 |
| `资料/完全复刻/账/战斗逻辑驱动.tsv` | `:771` 备注列 | **就地订正**：上一轮写的「`doNotStack` 是真缺口」→ 换成本段结论（保留订正痕迹） |
| `_tmp_view/e1_probe2/{e1probe2.csproj,E2.cs}` | —— | 临时探针（三节：① 摘除粒度 ② extrinsic 结构读数 ③ 光环覆盖率普查），不进 git |

⚠️ **`CardDef.cs` 的插入点在类尾**（`ToString()` 之后），但它**不在文件末尾**（后面还有 `KeywordTable` / `TraitReachability` 两个顶层类）
⇒ 下游（`KeywordTable` `:6165 → :6203`、`TraitReachability` `:8062 → :8100`）**共 +38 行**。
若账里有指这两个类的落点，需同步点在 `--fix` 之后**抽查 range 尾**。

## 五、没查清（第二段）

1. **`.c` 的合并键里到底比不比 `enchantingCard`** —— `.c:335-360` 那两处 `UnityEngine_Object__op_Inequality / op_Equality`
   是 Unity 的「假 null」检查（单操作数形态），**看不出**是不是在与 `param_2.enchantingCard` 比。
   判据文件 K1 说「`enchantingCard` 相同」，但 `.c` 里**只有 `enchantmentCardId` 的可读比较**。
   ⇒ 这一格**如实标「未定」**；对本段结论无影响（求和一致）。
2. **`doNotStack` 在 cpp2il 的 `AddEffect` 里读不到** —— cpp2il 那一版方法体**没重建出合并循环**
   （只有 extrinsic 去重树 + 若干 `IL_xxxx` 跳转）⇒ 这一段**只有 `.c` 有**。
   与「cpp2il 为准」的口径冲突时，**这一处只能以 `.c` 为准**（已记）。
3. **`extrinsic` 在「非光环形状」的 while-in-play 效果上仍无从判定** —— 今天这类效果**一个都没有**（§3·3 的普查），
   但若将来数据出现（远端 CCD 的卡），需要一条「句子 → ability」的归属链。

## 六、顺手发现（第二段）

| # | 发现 | 建议 |
|---|---|---|
| G1 | **`doNotStack` / `extrinsic` / `extrinsicVariable` 三个 SO 列都是「运行时会被覆盖的序列化残留」**（`extrinsic` 那两列判据 §7·1 已证；`doNotStack` 本段新证 —— 唯一写点是 `PlayerHand.AddHandEffect`） | 统计「池内 N 条」时**别用这三列**；已写进代码注释与账 |
| G2 | `CardEffect.IsSameEffect`（extrinsic 去重树用）**比数值**，`AddEffect` 的合并键**不比数值** —— 同名不同义 | 后续做 K0 第五道闸（`ContainsExtrinsicEffectFrom`）时**别复用合并键** |
| G3 | `PlayerHand.cs:2507` 的 `doNotStack = true` 是**手牌效果与场上效果分账**的唯一手段 | 我们 `CardInstance.HandEffect` 那条路本来就独立记账 ⇒ 无需动作；但若将来把「手牌效果」也塞进 `_statMods`（目前**没有**），**必须**带独立来源标签 |

## 七、交接给下一个代理（第二段）

### 做到哪
- ① `doNotStack` **复核完、结论是「不是缺口」**（账已订正）；② `extrinsic 240` **建模完（结构级）**。
- 两项都**零行为变化**：`play all dump` 与本会话上一轮逐字节相同。

### 剩什么
1. **唯一还等外部输入的**：`MeleeEqualsHealth` / `meleeAttackValueOptions`（等 Q1 的 `判据/战斗逻辑驱动_Stage2前置_第2轮.md`）。
2. **`NetProtocol` 协议版本 +1**（改动请求，见主报告 §五·5；`Net/` 归 E4）。
3. **W9 / W10 事件**（E5 的配合请求，与「`!t.IsAlive` 比原版严」同批）。
4. 同步点：`--fix` + 抽查 range 尾（本段又给 `CardDef.cs` 下游加了 **+38 行**漂移）。

### 文件动到哪（第二段增量）
```
Assets/RuleEngine/Core/CardDef.cs    (+38，类尾 ToString() 之后)
Assets/RuleEngine/Core/UnitState.cs  (+约 45，StatMod 类注 + IsExtrinsicSource doc)
资料/完全复刻/账/战斗逻辑驱动.tsv     (1 行订正)
```

### 下一步命令
```bash
TMPDIR=/tmp/wf_e1 bash d:/4/Unity/工具/typecheck.sh            # 期望 0/0
cd d:/4/Unity/_tmp_view/e1_probe2 && dotnet run -c Release     # ①② 的读数
cd d:/4/Unity/_tmp_view/e1_probe  && dotnet run -c Release     # 第一段那 8 条观测量
bash d:/4/Unity/工具/ruleprobe.sh check                        # 期望 rc=0、指纹 193c1b25…
```

### 踩过的坑（第二段）
1. 🔴 **`CardDef.cs` 里 `CardDef` 类不在文件末尾** —— 后面还有 `KeywordTable` / `TraitReachability` 两个顶层类。
   第一次把新成员插在「最后的 `    }` 之前」，结果插进了 `TraitReachability`（`CS0708 不能在静态类中声明实例成员`）；
   第二次挪到它后面又落到**命名空间级**（`CS0116`）。**正确锚点 = `ToString()` 的 `}` 之后、`CardDef` 那个 `    }` 之前**。
2. 🔴 **cpp2il 的 `CardScript.AddEffect` 没有重建出合并循环** —— 判据文件引 `.c:331-402` 那段**只能回 `.c` 读**；
   与「cpp2il 为准」的口径冲突时以 `.c` 为准并在报告里写明（本报告 §五·2）。
3. ⚠️ 本段两次跑 `typecheck` 都被别的代理的瞬时错干扰过 —— 判据仍是「错误全集中在不是我负责的文件 ⇒ 不是我的问题，隔一会儿重跑」。

---

# 第三段（同会话续做）· K2/K3 `addTrait(368)`（D1–D6）· ② `MeleeEqualsHealth`（240 重挂）· ③ Q1 两条顺手核

> 授权范围不变：`Assets/RuleEngine/**`（含 `Editor/RuleEngineTest.cs` 的期望值订正）。
> 判据 = **`资料/完全复刻/判据/战斗逻辑驱动_Stage2前置_第2轮.md`**（Q1 第 2 轮，296 行）+ `判据/CardTrait_stackable_真值表_第2轮.md`。
> 判据源优先级照主对话口径：🥇 cpp2il IL → 🥈 `decomp_full/**.c`（Ghidra，**参数个数不可信**）→ 🥉 解包 SO。

## 一、结论

| 项 | 结论 |
|---|---|
| **D1** `_removedTraits.Clear()` → `Remove(keyword)` | ✅ **改完**。判据 §三·1：两份独立 IL（cpp2il `CardScript.cs:17651` / il2csharp `:10855`）都是 `RemoveActiveEffects(removeTrait, newEffect.trait)`，Ghidra `AddEffect.c:474` 只印 2 个实参 = **少印**。原注释那套「整本清」是**按假口径写的**，且是**行为错**。 |
| **D2** `count` 档 | ✅ **改完**。`UnitState.IsCountStackableTrait` —— 只认 `markOfChaos(330)`；`count` ⇒ 每次施加 **+1**（不看给的值）。 |
| **D3/D4** 6 条逐 trait 特例 | ✅ **该做的都做了**：毒 × 抗性**双向**（一处收口 `UnitState.AddKeyword`）+ `survivor → 静默摘 survivorSpent` + `markerlight`/`huntMark` 广播**已存在且不双响**。⚠️ `dropPod` 的字段我们本来就没有、`tide/companion` 的计数器语义未核（判据 §四·5 也如实标「未核」）⇒ **如实留着**。**这些特例在池内全部 0 入口**（见 §2·3）。 |
| **D5** `lose <trait>` | ✅ **改完**：改成**整条摘掉**（`RemoveAll` + `MarkTraitRemoved`）+ `stealth ⇒ BroadcastKeywordEvent(LosesStealth)`（原版 `case 4` 第一步）。池内 14 条全是布尔 trait ⇒ **零行为变化**。 |
| **D6** `CheckPossibleCombinations` | ⛔ **不实现**（判据空：本地无 `TraitCombinationsList` SO 实例）⇒ 已写进账，按铁律 11 例外① 记 **D**。 |
| **② `MeleeEqualsHealth`** | ✅ **改完**：加了「动作边界重挂」（= 原版 `FinishResolvingAction` 尾部 `BroadcastWhileInPlay`），`SAU27` 的近战从「`FieldAttack` 里临时读 `Health`（活读）」改成 **`IsSet` 修饰 + 边界快照**。实测 6 条读数全对（§3·3）。 |
| **③-a `RandomizedOnResolution` 随机源** | ✅ **核完：不是红线。** 见 §4·1 —— 原版确实用 `UnityEngine.Random`，但**只在开局把 100 个值烧进 `MatchData.randomNumbers` 数组**，结算路径是**按下标取数组**。 |
| **③-b `IsSameEffect` 不比 `trait`** | ✅ 只记账、不修（池内 0 例），见 §4·2。 |
| **回归** | `typecheck` **0/0** · `ruleprobe check` **rc=0**、解析指纹 **`193c1b25…` 未变** · `play all dump` **与上一轮逐字节相同**（D1–D5 零行为变化）· `play check` **6 + 8**（= 原有 7 张 + **`SAU27` 一张，本段有意引入**，§3·4）。 |

## 二、D1–D6 逐条

### 2·1 D1（**行为错**，最高优先）

`UnitState.AddKeyword` 里那句 `_removedTraits.Clear()` → **`_removedTraits.Remove(keyword)`**。

| 源 | 写法 |
|---|---|
| `out_cpp2il_dev_ilspy/CardScript.cs:17651` | `RemoveActiveEffects(BuffType.removeTrait, newEffect.trait);` |
| `out_il2csharp/Assembly-CSharp/CardScript.cs:10855` | `this.RemoveActiveEffects(BuffType.removeTrait, newEffect.trait);` |
| `decomp_full/CardScript__AddEffect.c:474` | `CardScript__RemoveActiveEffects(param_1, 4);` ← **少印第 3 个实参** |

🔴 **行为错在哪**：改前「加**任何** trait ⇒ 整本「已移除」簿记清空」⇒ `HasRemovedTrait(reload 490)` 在
「先摘掉 reload、之后又拿到别的 trait」时**假变真** ⇒ `ConditionEval:468` 的 `280 siege` 会误判「可以装填」。
**可观察量**：`HasRemovedTrait` 全仓唯一消费者就是 `ConditionEval` 的 siege 280；`play all dump` **0 行差异**
⇒ 今天池内没有「先 `lose reload` 再拿别的 trait」的局面 ⇒ **零可观察量**，但口径必须先改对（否则 K2 一做，两个错互相掩护 —— 判据 §五·1 原话）。

### 2·2 D2（`count`）

```csharp
public static bool IsCountStackableTrait(string keyword)
{
    return keyword == KeywordTable.MarkOfChaos;   // 330 · count(20)
}
```
真值表（`判据/CardTrait_stackable_真值表_第2轮.md`，`bytesRead == byte_size` 73/73）：**74×`add(10)` · 1×`count(20)`（`markOfChaos`）· 0×`replace(0)`**；
`berzerk(360)` / `dodge(240)` **原版就没有 SO**。⇒ `AddKeyword` 里 `add = (value > 0 && IsCountStackableTrait(kw)) ? 1 : value;`。
⚠️ 已有的那条 `GrantDarkPactCore` 本来就是 `AddKeyword(MarkOfChaos, 1)` ⇒ 不受影响；受影响的是**通用 `give <trait>` 路**。

### 2·3 D3/D4（逐 trait 特例）—— 池内可达性先说清

现读 `cards_truth.json`（`buffType ∈ {3,90}` 整卡扫，跳过 `full_raw`）：

| trait | 池内条数 | 本段做了什么 |
|---|---|---|
| `flank(460)` | **62** | 已有（`RecomputeActAndAttack`）✅ |
| `fast(40)` | **32** | 已有 ✅ |
| `markerlight(1170)` | **18** | 已有广播 ✅（**不双响**，见下） |
| `huntMark(1260)` | **14** | 已有广播 + 成就计数 ✅（**不双响**） |
| `blind(975)` | **12** | 已有（攻击型强制近战）✅ |
| `markOfChaos(330)` | **7** | **本段 D2 新增 `count` 语义** |
| `poisoned(200)` | **0** | **本段新增**：加毒时身上有 `resistant` ⇒ 拒收（`AddKeyword` 返回 `false`，调用方出声） |
| `resistant(600)` | **0** | **本段新增**：加 resistant 时身上有毒 ⇒ `RemoveAll(poisoned)` |
| `survivor(430)` | **0** | **本段新增**：加 survivor ⇒ 静默摘 `survivorSpent` |
| `tide(910)` / `companion(1180)` | **0** | ⛔ **未做**：计数器快照语义未核（§四·5），我们的 `CompanionCounter` 是「伴生链剩余环数」、不是同一个东西 |
| `pindown(970)` | **0** | 已有（强制远程攻击型）✅ |
| `dropPod(230)` | **0** | ⛔ **未做**：我们没有 `currentDropPodHealth/Attack` 这两个字段 |

**`markerlight` / `huntMark` 会不会双响？—— 不会**（这是 D3 点名要核的那条）：
- `EmitStatusApplied` 发的是 `EvtKind.StatusApplied`（= 原版 `CardUI.DisplayStatusAnim` 那一拍，纯表现）；
- `BroadcastKeywordEvent(GetsHuntMark / GetsMarkerlight)` 发的是**正文触发器**事件（= 原版 `BattleManager.BroadcastHuntMarkResolved` / `BroadcastMarkerlightResolved`）。
两者是**不同事件种类**，原版也是**同一段里紧邻的两句**（`AddEffect.c:635/:637` 与 `:639/:643`）⇒ 各发一次是**照抄**，不是双响。

**D4「一处收口」的实现位置**：选 **`UnitState.AddKeyword`** —— 它是**所有**授予路的唯一收口
（`give` 载荷 / 光环 `AddAuraKeyword` 是另一条、不走这里 / 附魔 / 直调），所以两个方向的闸都落在这里 ⇒ **判据只有一处**。
`EffectResolver.ApplyOneGain` 只负责：① 看返回值决定「要不要 `EmitStatusApplied` / 广播」② 被拒时**出声**（不静默）。

### 2·4 D5（`lose <trait>`）

```csharp
if (p.Keyword == KeywordTable.Stealth && hadKw) BroadcastKeywordEvent(ctx, WhenEventKind.LosesStealth, u);
u.RemoveAll(p.Keyword);                       // 原版 case 4 的第 4+5 步：RemoveBuffedTrait + RemoveTrait
if (hadKw) u.MarkTraitRemoved(p.Keyword);     // 第 3 步留下的那条 removeTrait 记录
```
- **顺序照抄**：广播在摘之前（原版 `case 4` 第一步）。
- `blind ⇒ BlindedAtStartOfTurn = false` **已有**（`UnitState.RemoveAll` 里那句），D5-③ 不用再来一遍。
- ⚠️ **以前是 `RemoveKeyword(kw, -val)`**（只减一层）。池内 `removeTrait` **14 条全是布尔**（`stealth 9` / `camouflage 4` / `invulnerable 1`）⇒「整条摘」与「减一层」**等价** ⇒ **本项零行为变化**（`play all dump` 0 行差异可证）。

### 2·5 D6（不实现）

`CheckPossibleCombinations` —— 原版**每次 `AddEffect` 成功后**都调，读 `PlayerDataManager._traitCombinationsList`
（`TraitCombinationsList` SO）。**本地解包里没有该 SO 实例**（全盘 `find -iname '*TraitCombination*'` = 0，
只有 `MonoScript_3059.json` = **脚本定义**）⇒ **判据空**，按铁律 11 例外① 记 `D`，不实现、不写断言。
回来的条件 = 拿到远端 `TraitCombinationsList`，或原版实况里看到「凑齐两个 trait 触发附魔」。

## 三、② `MeleeEqualsHealth`（240 重挂）

### 3·1 我们这 8 处「重挂点」对上了几个

| # | 原版（判据 §1·4） | 我们的等价物 | 有没有 |
|---|---|---|---|
| 1 | `FinishResolvingAction` 尾部 `BroadcastWhileInPlay` | **`ActionPump.ActionScope.Dispose`**（每条顶层动作结束） | ✅ **本段新挂** |
| 2 | `BroadcastWhileInPlay` 本体（遍历在场 + 双方手牌） | `Auras.Recompose`（15 个棋盘写入点各一次） | ✅ 已有 |
| 3 | `ResolveTransformUnit`（变形） | 我们的残骸/变形那条链（`RuleCore` 的 transform 路径） | ✅ 已有（棋盘写入点覆盖） |
| 4 | `ResolveTransformFromRemnant` | `EffectResolver.DoReanimate`（`Auras.Recompose` 已在） | ✅ 已有 |
| 5 | `ResolveSummonUnit` | `RuleCore.PlayCard` / `DeployFree` | ✅ 已有 |
| 6 | `ResolveCardAutoSummoned` | `DeployFree(fromHand:true)`（`EffectResolver.cs:3470`） | ✅ 已有 |
| 7 | `ResolveExchangeMinion` | ⚠️ **未单独核**（`BoardSlots` 里有抢人/召唤的落点，但没有一一对账到「交换」） | ❓ **如实标未核** |
| 8 | `ResolveStealMinion` | `EffectResolver.DoTakeControl` | ✅ 已有 |
| ＋ | `ResetToValuesInHand` 尾巴 | 回手那条（`DoReturn`） | ✅ 已有 |

⇒ **第 7 条（交换随从）没有单独核过**，其余都对上了；第 1 条是**本段补的**。

### 3·2 实现（关键取舍：**没有**整跑 `Auras.Recompose`）

第一版把 `Auras.Recompose(ctx)` 直接挂到 `ActionScope.Dispose`。**实测把两张光环卡的结算读数改掉了**
（`BL18` `net` 0→4 一类、`UM18` `net 29→33`）—— 因为 `Recompose` 是「整份摘掉再重加」，
**顺带**会重算费用 / 关键词 / 誓约授予那一串，**超出「重挂 240 快照」的范围**。
⇒ 改成**只跑那一小趟**：`Auras.RefreshMeleeEqualsHealth(ctx)`（只遍历 `Card.MeleeEqualsHealth` 的单位；池内 **1 张**）。
两处调用：`Auras.Recompose` 的 ①·b（棋盘变动）+ `ActionScope.Dispose`（动作边界）。
改完之后 `BL18` / `UM18` 的差异**消失**，只剩 `SAU27`（本段有意引入）。

`SAU27` 的实现 = 每趟重建**一条 `IsSet` 的近战修饰**（值 = 当时的 `Health`），
`UnitState.ProjectMeleeAttack` 命中它即 `return` ⇒ **`addStats` 的近战加成一概不计**（原版 `get_CurrentMeleeAttack` 的 `buffType==70` 那一支）。
同时**删掉**了 `RuleCore.FieldAttack` 里那条 `if (MeleeEqualsHealth) baseAtk = u.Health;` 特判
（那是「换基础值、加值照常叠」= **我们挑的**，不是原版；留着就是同一条规则两处写、而且两处语义不同）。

### 3·3 可观察量（实测，`_tmp_view/e1_probe2` 的 (D) 段）

```
卡面 近战=8 生命=8；解析出的 MeleeEqualsHealth = True
Recompose 后：近战=8 生命=8                    （期望 8/8）
夹具（卡面近战 2 / 生命 5）：近战=5 远程=3      （期望 5 / 3 —— 覆盖卡面的 2）
再叠 addStats(+4 近战)：近战=5                  （期望 **仍是 5** —— set 命中即 return）
动作内掉血到 3：近战=5                          （期望 **仍是 5** —— 快照，不是活读）
下一个动作边界（Recompose）：近战=3              （期望 **3** —— 重挂刷新）
```

### 3·4 `play check` 的 `SAU27` 差异（**这是本段有意的行为变化**）

| 列 | 改前 | 改后 |
|---|---|---|
| `raw` | 41 | **40** |
| `net` | 2 | **9** |
| `netFull` | `☠ 伤害累计 P0 10 → 18; 18 → 23; …` | **多了三条 `✎ 改动 P0[2] SAU27 8/0/0/8 → 6/0/0/6 → 8/0/0/8 …`** |

**为什么是「改对了」**：改前 `SAU27` 的近战是 `FieldAttack` **在算伤害那一刻**读 `Health`（活读）；
原版是 `PlayAbility` 折叠成快照、靠动作边界重挂刷新。电池里那次「先掉血再攻击」**跨了一个动作边界**
⇒ 改后攻击用的是**刷新过的**快照（并且不再叠 `addStats`/`Pack` 那一路近战加值）—— 两条都是判据 §1·7 / §1·2 的原版语义。

### 3·5 自检断言同步改了（`Editor/RuleEngineTest.cs` 的 ① 段）

旧断言「生命掉到 3 ⇒ 近战跟着变 3（**每次现算**）」**是旧口径**，已整组重写为：
重挂后 5 → 叠 `+4 melee` **仍 5**（灭自证：改成「加值照常叠」会红）→ 同动作内掉血 **仍 5**（灭自证：改成活读会红）→
再 `Auras.Recompose` **变 3**。期望值依据全部标了判据行号（§1·1/§1·2/§1·4/§1·7）。
⚠️ **Unity 自检本批暂停**（用户口径），所以这组断言**没有跑过** —— 只做了 typecheck + 离屏探针。

## 四、③ Q1 点名让看的两条

### 4·1 🔴 `RandomizedOnResolution(90)` 的随机源 —— **不是红线**

| 步 | 现读 |
|---|---|
| 取值 | `AbilityLogic.cs:2708-2718`（`NewBuffStat` 的 `RandomizedOnResolution` 支）：`int randomInt = manager.GetRandomInt(minValue, maxValueExcl); return randomInt * num2;` |
| `manager.GetRandomInt` | `BattleManager.cs:42717` → 转调 `PlayerDataManager.singletonManager.matchData.GetRandomInt(...)` |
| 真正的实现 | **`MatchData.cs:541-573`**：读 **`randomNumbers__BackingField` 这个 `int[]` 的第 `randomNumbersIndex` 个**，`randomNumbersIndex++`，`return minValueIncl + (array[i] % (maxValueExcl - minValueIncl))` |
| 数组从哪来 | `MatchData.cs:213-228`（`GenerateRandomNumbers`）：`int[100]`，每个元素 `UnityEngine.Random.Range(0, 1000)`，填完把 index 归 0 |

**结论**：`UnityEngine.Random` 只用**一次**（开局烧 100 个整数进 `MatchData` 的数组），
**结算路径本身是「按下标取数组」** ⇒
① **同一局内完全确定**（重放/观战/联机只要 `MatchData` 一起走，两端逐值相同）；
② 但它**不是**「种子可复现」（数组是全局随机数烧出来的，没有种子入口）。

**对我们意味着什么**：如果将来实现 `NewBuffStat`/`RandomizedOnResolution`，正确做法 =
**开局用 `ctx.Rng`（`System.Random(seed)`）烧一个 100 元素的数组 + 下标**，结算时按下标取
（`min + (buf[i] % (max-min))`）—— 这样既保住原版的**消费形状**（每调一次前进一格、`%` 取模），
又满足本工程的「同种子同结果」红线。⛔ **不要**在结算里直接 `ctx.Rng.Next(...)`（那就丢了「预烧数组」这一层，
联机两端在同一步上会用掉不同数量的随机数 ⇒ 分叉）。
⚠️ **今天不实现**：`NewBuffStat`/`*ValueOptions` 整条链我们都没建（§六 的 `meleeAttackValueOptions` 也只按快照理解）。

### 4·2 `IsSameEffect` 不比 `trait` / `traitValue` —— 只记账、不修

`CardEffect.IsSameEffect`（`CardEffect.cs:200-…`，逐字段 `&&` 读出来的）比 `buffType · enchantingCard ·
enchantmentCardId · extrinsic · extrinsicVariable · 五个 until* · meleeAttackChange · meleeAttackValueOptions ·
rangeAttackChange · rangeAttackValueOptions · healthChange · healthValueOptions · costChange · costValueOptions ·
randomValueChange` —— **不含 `trait` / `traitValue`**。
⇒ 240 档「**同一张施放卡挂两个不同 `addTrait`**」会被判成同一条、第二条被丢掉。
**池内今天 0 例**（判据 §1·6 现读）⇒ 只记账、**不修**（原版如此，修了就是偏离）。

## 五、改动清单（第三段）

| 文件 | 改动 |
|---|---|
| `Core/UnitState.cs` | `AddKeyword` 返回 `bool`；**D1** `_removedTraits.Remove(keyword)`；**D2** `add = count ? 1 : value`；**D3/D4** 毒×抗性双向 + `survivor ⇒ 摘 survivorSpent`；新增 `public static bool IsCountStackableTrait(string)` |
| `Core/EffectResolver.cs` | `ApplyOneGain` 关键词支：看 `AddKeyword` 返回值决定 `EmitStatusApplied` / 广播；被拒时 `ctx.Log` 出声。`lose` 支：**D5** `stealth` 广播 + `RemoveAll` 整条摘 + `MarkTraitRemoved` |
| `Core/Aura.cs` | 新增 `public const string MeleeEqualsHealthSource` + **`public static void RefreshMeleeEqualsHealth(BattleContext)`**；`Recompose` 的 ①·b 调它 |
| `Core/ActionPump.cs` | `ActionScope.Dispose`：每条顶层动作结束调 `Auras.RefreshMeleeEqualsHealth`（**不整跑 `Recompose`**，理由写在原地） |
| `Core/RuleCore.cs` | **删掉** `FieldAttack` 里 `MeleeEqualsHealth` 那条特判（判据见原地注释） |
| `Core/CardDef.cs` | `MeleeEqualsHealth` 的 doc 就地订正（读点变了） |
| `Editor/RuleEngineTest.cs` | ① 段 `MeleeEqualsHealth` 那组断言**整组重写**（期望值依据 = 判据 §1·1/§1·2/§1·4/§1·7） |
| `账/战斗逻辑驱动.tsv` | `:771` 备注列追加 K2/K3 + D1–D6 全段（含池内可达性表与回归读数） |

## 六、没查清（第三段）

1. **`ResolveExchangeMinion`（交换随从）的重挂点没单独核** —— 其余 7 处都对上了（§3·1）。
2. **`tide/companion` 的 `tideCounter` / `companionCounter` 语义** —— 判据 §四·5 已如实标「未核」，我们
   `CardInstance.CompanionCounter` 是「伴生链剩余环数」，**不是**「快照 `CurrentCompanion`」；池内 addTrait **0 条**。
3. **`dropPod` 的 `currentDropPodHealth/Attack`** —— 我们没有这两个字段（`RuleCore.OpenDropPod` 是另一套）；池内 **0 条**。
4. **`survivor` 的 S4 零血闸没落地** —— 原版是 `T == survivor ∧ E.healthChange == 0 ⇒ 不挂`，而我们的
   `PayloadOp` 把 `survivor` 与 `+1 health` 拆成**两条**独立 item ⇒ 在 `ApplyOneGain` 这一层**看不到** `E.healthChange`。
   池内 `survivor` **0 条** ⇒ 今天不可达，**如实留着**（要真做，得把闸提到 `DoGive` 的载荷级）。

## 七、顺手发现（第三段）

| # | 发现 | 处置 |
|---|---|---|
| **H1 🔴** | **`Auras.Recompose` 的「整份摘掉再重加」会把带 `+N health` 光环的单位在每次棋盘变动时「回血 N」。** 路径：`ClearAuraGrants` → `RevertGrantsFrom` → `ApplyGrant(-1)` 走的是 **Stage 2 新加的「只夹不减」**（`Health = min(Health, 新上限)`）→ 再重加时 `Health += N` ⇒ 净结果可能**多出**（例：6/6 上限 6，掉到 5，重算后回满 6）。<br>**改前**（Stage 2 之前）那一支是 `Health -= N` ⇒ 摘+加重加**净零**，所以这个洞是**Stage 2 引入的**。<br>**池内今天 0 入口**（现读：全池没有任何「光环形状的 +health」句子；唯一的 `have … health` 命中是 `AM54` 的条件句）⇒ **零可观察量**。<br>两种改法（**需要裁定，我没动**）：(a) `ClearAuraGrants` 走「精确回退（减）」—— 但那样「光环来源离场」时就不夹上限了；(b) 区分「重算」与「真移除」两条路（原版 `UpdateWhileInPlay` 对 trigger 20 是**幂等空操作**、对 240 才摘+加）。<br>⚠️ 这也解释了为什么我**没有**把 `Auras.Recompose` 挂到每个动作边界（§3·2）。 | **记账 + 等裁定** |
| **H2** | 第一版把 `Auras.Recompose` 挂到动作边界 ⇒ `BL18` / `UM18` 两张光环卡的 `net` 变了（+4）。已改成只跑 `RefreshMeleeEqualsHealth`。 | **已修**，过程写在 `ActionPump.cs` 原地 |
| **H3** | `MeleeEqualsHealth` 过去那条「换基础值、加值照常叠」是**我们挑的**（`CardDef` 原注释自己写着「规则书查不到」），本段按判据改成**覆盖语义**（set 命中即 return）。 | **已修**（判据 §1·7 末段） |
| **H4** | `doNotStack` 之外，**`CardEffect.IsSameEffect` 的字段表**也要照抄（判据 §1·5 硬要求 3"D 字段表要照抄"）—— 我们**没有** `activeEffects` 记录表，这条将来做 K0 第五道闸时才用得上。 | 记账 |

## 八、交接给下一个代理（第三段）

### 做到哪
- **D1–D5 全部落地、D6 判 D 并记账**；**② `MeleeEqualsHealth` 改成「动作边界重挂 + 快照」**；③ Q1 两条都核完。
- 唯一有意的行为变化：**`SAU27`**（§3·4）。其余 **零行为变化**（`play all dump` 逐字节相同）。

### 剩什么
1. **H1（光环 `+health` 重算回血）需要裁定** —— 池内 0 入口，但是 Stage 2 引入的语义洞。
2. **`ResolveExchangeMinion` 的重挂点**没单独核（§3·1 第 7 行）。
3. `tide/companion` / `dropPod` / `survivor` 三条特例 —— 池内 0 条，未做（§六·2/3/4）。
4. `NetProtocol` 协议版本 +1（改动请求，`Net/` 归 E4）· W9/W10（W10 已派 R5；W9 等本段收工后单开）。
5. 同步点：`CardDef.cs` 本段又 +5 行、`UnitState.cs` +59 行、`Aura.cs` +52 行、`ActionPump.cs` +18 行 ⇒ `--fix` 后抽查 range 尾。

### 文件动到哪（第三段增量）
```
Assets/RuleEngine/Core/UnitState.cs          (+59)
Assets/RuleEngine/Core/EffectResolver.cs     (+约 30)
Assets/RuleEngine/Core/Aura.cs               (+52)
Assets/RuleEngine/Core/ActionPump.cs         (+18)
Assets/RuleEngine/Core/RuleCore.cs           (-6，删特判 + 注释)
Assets/RuleEngine/Core/CardDef.cs            (+5)
Assets/RuleEngine/Editor/RuleEngineTest.cs   (+21，断言组重写)
资料/完全复刻/账/战斗逻辑驱动.tsv             (:771 备注列)
_tmp_view/e1_probe2/E2.cs                    ((D) 段：MeleeEqualsHealth 六条读数)
```

### 下一步命令
```bash
TMPDIR=/tmp/wf_e1 bash d:/4/Unity/工具/typecheck.sh            # 期望 0/0
cd d:/4/Unity/_tmp_view/e1_probe2 && dotnet run -c Release     # (A) doNotStack (C) extrinsic (D) MeleeEqualsHealth (B) 覆盖率
cd d:/4/Unity/_tmp_view/e1_probe  && dotnet run -c Release     # 第一段那 8 条观测量
bash d:/4/Unity/工具/ruleprobe.sh check                        # 期望 rc=0、指纹 193c1b25…
bash d:/4/Unity/工具/ruleprobe.sh play check                   # 现在 rc=1：6 张（clamp0 旧账）+ 8 张（含 SAU27 本段有意）
```

### 踩过的坑（第三段）
1. 🔴 **`Auras.Recompose` 不能当「重挂点」直接挂到动作边界** —— 它是「整份摘掉再重加 + 重算费用/关键词/誓约」，
   会改掉无关卡（`BL18`/`UM18`）的读数。**要挂就挂「只做那件事」的小方法**（本次 = `RefreshMeleeEqualsHealth`）。
   教训：**「幂等」不等于「无副作用」** —— 它对**值**幂等，但会**重放**一串副作用（夹血、广播、费用重登记）。
2. 🔴 **`ApplyGrant` 的健康撤销从「减」改成「夹」之后，任何「摘掉再加回」的路径都会改变净结果**
   （H1）。凡是「重算 = 先清后加」的地方都要重新审一遍健康那一格。
3. ⚠️ 改 `AddKeyword` 的返回值/拒收语义时，**所有 `if (val > 0) 广播`** 的调用点都要跟着加 `&& added`，
   否则会对外宣称「拿到了它其实没有的 trait」。

---

# 第四段（同会话续做）· H1 裁定落地（重算 vs 真移除）· W9 + F2（标记光留到回合末 + `TraitRemoved` 事件）

> 授权范围不变：`Assets/RuleEngine/**`（含 `Editor/RuleEngineTest.cs`）。
> **上下文用量：约 30–35%**（远未到 80%，本段收尾后仍有余量）。

## 一、结论

| 项 | 结论 |
|---|---|
| **① H1（走裁定 (b)：区分「重算」与「真移除」）** | ✅ **落地**。`Auras.Recompose` 整段包在 `BeginAuraRecompute` / `EndAuraRecompute` 之间：**重算期间 `health` 一个字节不动**；重算结束按「光环血修饰总量的前后差」结算一次（+差 加 / −差 夹 / **零差 什么都不做**）。**真移除（卡离场/效果到期）仍走 Stage 2 的「夹到新上限」，一行没改。** |
| **② W9 + F2** | ✅ **判据查得到 ⇒ 同批做完**。**F2**：攻击用完标记光**只置标志、不摘 trait**（`CardScript.SetRemoveMarkerlight`，cpp2il `CardScript.cs:11260-11296`），**回合末**才摘（`CardScript.OnTurnEnd`，`:16599-16607`）；**W9**：新增 `EvtKind.TraitRemoved`，在**真正摘掉的那一刻**发。 |
| **验收** | `typecheck`（**我的文件**）**0/0** · `ruleprobe check` **rc=0**、解析指纹 **`193c1b25…` 未变** · `play all dump` **与上一轮逐字节相同**（H1 那条第 4 点要求的）· `play check` **仍是那同一批 6 张（clamp0 旧账）+ 8 张（电池，含 `SAU27`）**，**没有新增**。 |

## 二、① H1：为什么「先清后加」会回血，以及怎么分开

### 2·1 病灶（复述，便于下个会话直接看懂）

- 「摘 `+N health` ⇒ `currentHealth = min(当前, 新上限)`」这条判据（`RemoveEffect.c:147-181`）是给**真移除**用的；
- `Auras.Recompose` 是「**先全清、再加回**」⇒ 一次重算里同时走了一遍「真移除」和「重新施加」：
  清的时候**夹**一次（可能把 6→4），加回来又 `+= 2`（4→6）⇒ **净结果不为零**（掉过血的单位每次棋盘变动都回一点）。
- 而原版里 `trigger 20`（光环）的重复施加是**幂等空操作**（`AddEffect` 的 `ContainsExtrinsicEffectFrom` 已为真 ⇒ `return false`，判据 §1·5），
  只有 `trigger 240` 才是「摘 + 加」—— **两件事本来就不该共用一个口径**。

### 2·2 实现（`UnitState` + `Aura.Recompose` 外壳）

| 落点 | 内容 |
|---|---|
| `UnitState.BeginAuraRecompute()` | 记下「重算前」的**光环血修饰合计**（`AuraHealthGrantTotal()`），并置 `_recomputingAura = true`（**嵌套安全**：内层不重置基准） |
| `UnitState.EndAuraRecompute()` | 重算结束后比较前后差：`+差 ⇒ Health += 差`（新出现的光环 = 首次施加）· `−差 ⇒ ClampHealthToMax()`（光环离场 = **真移除**）· **零差 ⇒ 什么都不做** |
| `UnitState.ApplyGrant` 的 `health` 支 | `_recomputingAura` 为真时**不碰 `Health`**（连夹也不夹），只摘修饰 |
| `Auras.Recompose` | 改成外壳：先给全体在场单位 `BeginAuraRecompute()`，`try { RecomposeCore(ctx); } finally { …EndAuraRecompute(); }`（**`finally` 是必须的** —— 本体 ③ 里有多处 `return`） |
| `Auras.RecomposeCore` | 原来的本体，逐字未改 |

⚠️ **只对 `health` 需要这一层**：`attack`/`ranged` 是「摘修饰 + 重加修饰」，本来就净零；
关键词 / 费用 / 誓约授予是布尔或重登记，也净零 —— 都**没有**加抑制开关（⛔ 别推广成「重算期间什么副作用都不许有」，那会把新出现的光环吃掉）。

### 2·3 可观察量（实测，`_tmp_view/e1_probe2` 的 (E) 段；夹具 = `Adjacent units have +2 health`）

```
首次部署：目标 生命=6 上限=6     ← 光环 +2 当场加到当前生命
掉血后：3/6
重算一次（无关变动）：3/6        ← **一个字节都不动**（改前会变 6/6）
再重算一次：3/6
来源离场（真移除）：3/4          ← 夹到新上限、不额外多减
```

### 2·4 灭自证断言（`Editor/RuleEngineTest.cs` 的「⑤·b」新块）

**一对**结构上不可能同时满足的断言：

| 段 | 断言 | 🧨 改坏法 |
|---|---|---|
| 第一段 | 掉血到 3 后 **纯重算** ⇒ `Health` **仍是 3**（`3/6`） | 把**重算**也改成夹 ⇒ 实得 6，红 |
| 第二段 | 回满到 6 后**来源离场** ⇒ `Health` **= 4**（`4/4`） | 把**真移除**也改成什么都不做 ⇒ 实得 6，红 |

> 只断其中一条都会被「把两边改成同一个」的实现混过去 —— 这正是裁定要求的形状。
> ⚠️ **Unity 自检本批暂停，这组断言没跑过**；等价行为已用上面的离屏探针实测。

## 三、② W9 + F2

### 3·1 F2 判据（cpp2il 现读，`.c` 只交叉）—— **查得到，所以同批做了**

| 步 | 现读 |
|---|---|
| 攻击那一段跑完 | `BattleManager._ResolveAttack…:1633` → **`CardScript.SetRemoveMarkerlight(target)`** |
| `SetRemoveMarkerlight`（`CardScript.cs:11260-11296`） | `removeMarkerlightAtEndOfTurn__BackingField = true;`（原版 `EntityScript +0x57`）+ 对两本字典各取一次 `MarkerlightController.MarkerLightUsed()`（**纯表现**：`Normal off / Expiring on`）。**一个 trait 都不摘。** |
| 真正摘除（`CardScript.OnTurnEnd`，`:16599-16607`） | `if (removeMarkerlightAtEndOfTurn) { if (CurrentMarkerlight > 0) { RemoveActiveEffect(markerlight); RemoveTrait(markerlight); 该位 = false; } }` |

⇒ **判据成立**（不是「我们自己发明的留到回合末」）。**W9 与 F2 同批做**（E5 的要求）。

### 3·2 实现

| 落点 | 改动 |
|---|---|
| `UnitState.RemoveMarkerlightAtEndOfTurn` | **新增**布尔字段（doc 里带完整判据 + 「谁的回合末没核」的如实标注） |
| `RuleCore`「标记光 X」那一段 | `target.RemoveAll("markerlight")` → **`target.RemoveMarkerlightAtEndOfTurn = true;`** + 日志改成「留到回合末再移除」 |
| `RuleCore.EndTurnCore` 的 `RevertBuffs(true, …)` 那一趟 | 新增：`if (u.RemoveMarkerlightAtEndOfTurn) { int before = KwValue("markerlight"); if (before > 0) { RemoveAll; **EmitTraitRemoved**; 清标志 } }`（**门是 `before > 0`**，与原版同形：已经没了的只清标志） |
| `BattleEvent.EvtKind` | **新增 `TraitRemoved`（追加在枚举末尾）** —— `CardId` = 关键词本身（与 `StatusApplied` 同约定）、`Amount` = 摘之前的层级 |
| `RuleCore.EmitTraitRemoved(ctx,u,keyword,amountBefore)` | **新增**（放在 `EmitStatusApplied` 旁边）：`FindSlot` 失败 = 已不在棋盘 ⇒ 不发 |
| `EffectResolver.ApplyOneGain` 的 `lose <trait>` 支 | 摘掉之后也发一条 `TraitRemoved`（`kwBefore > 0` 才发）—— 这是第二条「trait 被摘」的可视路径 |

### 3·3 可观察量（实测，`_tmp_view/e1_probe2` 的 (F) 段）

```
攻击前：标记光 = 2 · 生命 9
DeclareAttack rc = 0 · 目标生命 4
攻击后：标记光 = **2**（还在）· 标志 = True · `TraitRemoved` 事件 = 0
回合末：标记光 = **0** · 标志 = False · `TraitRemoved(markerlight)` 事件 = **1**
```

### 3·4 自检断言（`Editor/RuleEngineTest.cs` 的「⑤ 标记光」那一块，**期望值整组重写**）

旧断言写的是「挨过远程伤害后标记光移除」（= 我们以前的即时摘除）。现在四条：
1. 挨过远程伤害后**标记光还在**（🧨 改回当场 `RemoveAll` ⇒ 红）
2. 「回合末摘」的标志置上了
3. 攻击那一段**不发** `TraitRemoved`（还没摘）
4. `PassTurn` 之后：标记光没了 **且** 发了一条 `TraitRemoved`（🧨 不发 ⇒ 表现层 `Expiring State` 永远看不见）

## 四、验收读数

| 闸 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_e1 bash 工具/typecheck.sh` | **我的文件 0 / 0**；本轮那一次读到 `运行时错误数: 1` 是**别人文件的瞬时错**（`Shell/LiveOpsHandler.cs`（E2）· `Shell/LegacySaveReader.cs`（E3）共 5 条），**`RuleEngine/**` 一条都没有**（`ruleprobe check` 能编过 `Core/*.cs` 即为证）。 |
| `ruleprobe check` | **rc=0** · 解析差异 0 行 · 哨兵失败 0 · **解析指纹 `193c1b25…` 未变** |
| `ruleprobe play all dump` | 与**原始基线**差 4 行（= 第一段那 2 张 clamp0 卡）；**与本段之前逐字节相同** ⇒ H1 那条「纯重算路径不许有行为变化」**满足** |
| `ruleprobe play check` | **rc=1 · 6 + 8**，卡集合 = `EC1/EC3/EC14/EC16/EC27/EC32/EC33`（clamp0 旧账）+ `SAU27`（第三段有意）—— **没有新增** |

## 五、没查清（第四段）

1. **「谁的回合末」** —— 原版是 per-card `CardScript.OnTurnEnd`，它跑在**持有者那一侧**还是**回合方那一侧**，
   本笔**没有**逐调用点坐实。我们按**既有约定**（`EndTurn` 里那条 `RevertBuffs(true, …)` 一趟**两边都扫**）落点，代码里如实标着。
   📌 影响面：如果原版只扫回合方那一侧，那么「我方攻击给敌方单位打上的标记光」会晚一整个回合才摘。
2. **`TraitRemoved` 的消费端没接** —— W9 只做了**引擎侧的事件**；表现层（`TraitFrames` / `MarkerlightController` 的
   `Expiring State`）**还没接**（那是 E5 / 特效线的活）。
3. **H3（`survivor` 零血闸）** 仍如实留着（池内 0 条；我们的 `PayloadOp` 把 `survivor` 与 `+1 health` 拆成两条）。

## 六、改动清单（第四段）

| 文件 | 改动 |
|---|---|
| `Core/UnitState.cs` | `ApplyGrant` 的 `health` 支加 `_recomputingAura` 抑制；**新增** `BeginAuraRecompute` / `EndAuraRecompute` / `AuraHealthGrantTotal` / `RemoveMarkerlightAtEndOfTurn`（都带完整判据 doc） |
| `Core/Aura.cs` | `Recompose` → 外壳（`Begin/End` + `try/finally`）；本体改名 `RecomposeCore`（逐字未改） |
| `Core/BattleEvent.cs` | `EvtKind` **末尾追加** `TraitRemoved` |
| `Core/EffectResolver.cs` | **新增** `public static void EmitTraitRemoved(...)`；`lose <trait>` 支摘完发一条 |
| `Core/RuleCore.cs` | 标记光那一段改成**只置标志**；`EndTurnCore` 加「回合末摘 + 发事件」 |
| `Editor/RuleEngineTest.cs` | **新增**「⑤·b 重算 vs 真移除」一对灭自证断言；**重写**「⑤ 标记光」四条断言（期望值依据 = cpp2il `CardScript.cs:11260-11296` / `:16599-16607`） |
| `_tmp_view/e1_probe2/E2.cs` | 新增 (E) 重算/真移除、(F) F2/W9 两段读数 |

## 七、顺手发现（第四段）

| # | 发现 | 处置 |
|---|---|---|
| **J1** | `EmitStatusApplied` / `EmitTraitRemoved` 两个口都把「关键词」放在 **`CardId`** 而不是 `Keyword` 字段（`ctx.Emit(kind, p, slot, cardId, …)` 的第 4 个位置实参）。第一版探针按 `e.Keyword` 过滤 ⇒ 数出 0，**看上去像「事件没发」**。 | **已记**（测试与探针都改成 `e.CardId`）；⚠️ 下一个写消费端的人别按 `Keyword` 取 |
| **J2** | 「先清后加」这种重算模式在本仓**不止光环一处**（`RevertBuffs` 是逐条摘、不受影响；`ClearAuraGrants` 只此一处）。本段加的是**通用**的「重算抑制」开关，但**只有光环用**。 | 记账 |
| **J3** | `RuleCore.EndTurnCore` 里那条 `RevertBuffs(true, ctx.Active)` 一趟**两边都扫**，与「原版 per-card `OnTurnEnd`」的覆盖面**可能不同**（见 §五·1）。 | 记账，等实况 |

## 八、交接给下一个代理（第四段）

### 做到哪
- H1 按裁定 (b) 落地（含一对灭自证断言）；W9 + F2 **同批做完**（判据查得到）。
- 唯一的行为变化仍在 **`SAU27`**（第三段既有）—— 本段**零新增行为变化**（`play all dump` 逐字节相同、`play check` 卡集合不变）。

### 剩什么
1. **表现层接 `EvtKind.TraitRemoved`**（`Expiring State` 那一支）—— E5 / 特效线。
2. **「谁的回合末」** 逐调用点坐实（§五·1）。
3. `NetProtocol` 协议版本 +1（改动请求，`Net/` 归 E4）。
4. 同步点：`--fix` + 抽查 range 尾（本段 `UnitState.cs` +89 / `Aura.cs` +21 / `RuleCore.cs` +31 / `BattleEvent.cs` +22 / `EffectResolver.cs` +20 / `RuleEngineTest.cs` +64）。

### 下一步命令
```bash
TMPDIR=/tmp/wf_e1 bash d:/4/Unity/工具/typecheck.sh            # 期望（我的文件）0/0
cd d:/4/Unity/_tmp_view/e1_probe2 && dotnet run -c Release     # (A)…(F) 六段读数
bash d:/4/Unity/工具/ruleprobe.sh check                        # 期望 rc=0、指纹 193c1b25…
bash d:/4/Unity/工具/ruleprobe.sh play check                   # rc=1：6 + 8（同一批）
```

### 踩过的坑（第四段）
1. 🔴 **包装一个「先清后加」的重算时，`finally` 是必须的** —— `RecomposeCore` 的 ③ 里有多处 `return`，
   没有 `finally` 那些路径的 `EndAuraRecompute` 就漏了（`_recomputingAura` 会**永久留在 true** ⇒ 之后所有健康变化被静默吞掉）。
2. 🔴 **抑制副作用时只能抑制「会重复计一次」的那一格**（这里是 `health`），⛔ 不要顺手把 `attack`/关键词/费用也钳住 ——
   那些是「摘了再加」的净零操作，钳住反而会把**新出现**的光环吃掉。
3. ⚠️ `ctx.Emit` 的第 4 个位置实参是 `CardId`，不是 `Keyword`（J1）。

---

# 第五段（收尾）· 三处小口子 + J3 坐实

> 本段**只做给定的三件事 + J3**，没有开新面。

## 一、结论

| 项 | 结论 |
|---|---|
| **①-1 `Classic.maxLegendaries` 1 → 0** | ✅ 改完（含 `LegendaryTotalLimit` 的 doc 订正）。**零行为变化**（那条闸仍显式 `int.MaxValue`）。 |
| **①-2 `DeckError.LegendaryTotalExceeded`** | ✅ 改完：枚举追加 + `Describe` 出原版键 + `Validate` 换码 + **收回** `DeckEditorState` 的载体特判与 `AddErrorText` 特判（连 `out bool legendaryTotal` 与 `LegendaryTotalErrorText()` 一起删干净）。 |
| **①-3 `NetProtocol` 版本 2 → 3** | ✅ 只改版本常量 + 注释，**没有动消息格式**。 |
| **② J3「谁的回合末」** | ✅ **坐实：两边都扫是对的，零改动。** `BattleManagerSupport.BroadcastTurnEnd`（`BattleManagerSupport.cs:1516-1562`）遍历的是 **`manager.unitsInPlay`【全局】在场列表**，对每张在场卡各调一次 `OnTurnEnd` ⇒ 不分敌我。 |
| **验收** | `typecheck` **0/0** · `ruleprobe check` **rc=0**、指纹 **`193c1b25…` 未变** · `play all dump` **与上一轮逐字节相同** · `play check` **仍是 6 + 8**（无新增）。 |

## 二、①-1 `Classic.maxLegendaries`

| 位置 | 改动 |
|---|---|
| `RuleEngine/Core/GameplayVariables.cs`（`ClassicMaxLegendaries = 1`） | → **`0`** |
| 同上，`maxLegendaries` 字段的 doc | 从「经典 1 · 遭遇 4」改成「**经典 0 = 无此限制** · 遭遇 4」，并写清 `<= 0` 的哨兵语义 |
| `RuleEngine/Core/DeckRules.cs` 的 `LegendaryTotalLimit` doc | 删掉「原版那个字段叫 `maxLegendaries`，**两种读法都说得通**，我们按…走」那半句（现已有判据），换成**有出处**的结论 |

**判据（R3 §六·2 + 本轮抽查）**：
- 官方文案 `Event_Description/Classic/Help` = **`No Legendary card limit`**（中文「无传奇张牌限制」）；
  `Event_Description/Skirmish/Help` = `Up to 4 Legendaries (Warlord not included)`；
- 原版闸 `DeckUtility.CanAddCard.c:105` = `if (0 < gameplayVariables.maxLegendaries)` ⇒ **`<= 0` = 本模式无上限**；
  `:132` 才是 `limit <= count ⇒ 挡`。

⚠️ **`LegendaryTotalLimit(false)` 仍然写字面 `int.MaxValue`**，⛔ **没有**改成 `return Classic.maxLegendaries` ——
那个 `0` 是「关掉这条闸」的哨兵，直接返回 `0` 会让 `legendaries > limit` **任何一张传奇都被挡**。
doc 里把这条换算关系写死了（两种表示必须换算一次）。

## 三、①-2 `DeckError.LegendaryTotalExceeded`

| 文件:位置 | 改动 |
|---|---|
| `RuleEngine/Core/DeckRules.cs` · `enum DeckError` **末尾** | **新增** `LegendaryTotalExceeded,` |
| 同上 · `Describe` | **新增** `if (e == DeckError.LegendaryTotalExceeded) return TermPrefix + "MaxLegendayCopiesReached";`（⚠️ **保留原版把 `Legendary` 拼成 `Legenday` 的拼写** —— 那是本地化键的字面量） |
| 同上 · `Validate` ⑧ | `return DeckError.CopyLimitExceeded` → **`LegendaryTotalExceeded`** |
| `CardPresentation/Deck/DeckEditorState.cs` · `CanAddCore` | 签名 `CanAddCore(CardDef c, out bool legendaryTotal)` → **`CanAddCore(CardDef c)`**；`if (LegendaryTotalReached(c)) { legendaryTotal = true; return DeckError.CopyLimitExceeded; }` → **`return DeckError.LegendaryTotalExceeded;`** |
| 同上 · `CanAdd` / `TryAdd` / `AddErrorText` | 三处调用点跟着去 `out`；`why` / 返回值统一 `Loc.T(DeckRules.Describe(e))`（**特判没了**） |
| 同上 · `LegendaryTotalErrorText()` | **整个方法删除**（它做的 `string.Format` 是空操作：官方两条文案都没有 `{0}`） |
| 同上 · 三处过期 doc | 就地订正（「借 `CopyLimitExceeded` 当载体 / 分档靠 `out legendaryTotal`」→「不再借码，返回值本身就能分」） |

**兼容性核查**（改枚举会不会撞别处）：
- `Editor/DeckScene.cs:3747` 断的是**普通卡撞同名上限** ⇒ 在 `DeckRules.Validate` 的 `:503` 就早退，**仍是 `CopyLimitExceeded`** ✓
- `Editor/DeckRulesTest.cs:177` 断的是「传说卡带 2 张」⇒ 同样撞 `:503`（传说同名上限 1）✓
- `DeckRuntime.MenuDeckErrorNumber` 走 `default: return 4` ⇒ 新枚举落 **4**（= 原版 `:111-121` 那条的号）✓
  ⚠️ `DeckRuntime.cs` **不在本段授权范围**（只改 `DeckRules.cs` + `DeckEditorState.cs`）⇒ 没有动它；
  建议同步点顺手在那张逐条对位表里补一行 `LegendaryTotalExceeded → 4`（纯 doc）。

🔴 **发现的缺口（改动请求，`CardPresentation/Core/Loc.cs` 不在我白名单）**：
`MenuDeck/Error/MaxLegendayCopiesReached` **不在 `Core/Loc.cs` 的词条表里** ⇒ `Loc.T` 会把**键名**印到屏幕上。
⚠️ **这不是本段引入的**：改前 `LegendaryTotalErrorText()` 用的也是同一个键、同样查不到 ⇒ **行为不变**（照旧印键名）。
词条（官方两列现成）：EN `Decks in this game mode can contain only 4 legendary cards.` / ZH「只能包含4张传奇牌」。

## 四、①-3 `NetProtocol.Version` 2 → 3

只改 `CardPresentation/Net/NetProtocol.cs` 的 `public const int Version`（2 → 3）+ `<summary>` 里补一段理由，**消息格式一个字没动**。
注释内容：属性模型 Stage 2 之后 `Attack` / `RangedAttack` / `MaxHealth` 读的是**投影**，而 `PlayerHash` 读的正是它们
⇒ **同一串动作在两版引擎里可能算出不同的哈希**（`blind ⇒ 远程 0` / `setXxx` 覆盖 / 负攻值 `clamp0` 三处是新旧分界）
⇒ 旧 build 的局面哈希不再可比，必须挡住（否则表现为**打到一半 desync**）。
⚠️ 同时写清「值本身今天多半一样」—— 但那不能作为互相兼容的理由。

## 五、② J3「谁的回合末」—— 坐实，零改动

**判据（cpp2il 现读）**：`BattleManagerSupport.BroadcastTurnEnd(BattleManager manager, bool isPlayerTurn)`
（`BattleManagerSupport.cs:1516-1562`）：
```
foreach (cardScript in manager.unitsInPlay)            // ← 【全局】在场列表（不分敌我）
    if (cardScript.IsInPlay()) cardScript.OnTurnEnd(isPlayerTurn);
    else if (cardScript.HasCurrentTrait(remnant) && cardScript.cardState == waitingToDie) ...
// 之后才分别扫【双方手牌】
```
⇒ **`CardScript.OnTurnEnd` 跑在【所有在场卡】上**，不看 `isPlayer__BackingField`。
⇒ 我们 `RuleCore.EndTurnCore` 那趟 `RevertBuffs(true, ctx.Active)` **两边都扫**与它**同形**
⇒ 「我方打给敌方的标记光」**不会**被提前一整个回合摘掉，**不用改**。

`UnitState.RemoveMarkerlightAtEndOfTurn` 的 doc 已就地订正（原来写着「谁的回合末没核」）。

## 六、改动清单（第五段）

| 文件 | 改动 |
|---|---|
| `RuleEngine/Core/GameplayVariables.cs` | `ClassicMaxLegendaries` 1 → **0**；`maxLegendaries` 字段 doc 重写（含 `<= 0` 哨兵语义） |
| `RuleEngine/Core/DeckRules.cs` | `DeckError` 末尾追加 `LegendaryTotalExceeded`；`Describe` 映射原版键；`Validate` ⑧ 换码；`LegendaryTotalLimit` doc 订正 |
| `CardPresentation/Deck/DeckEditorState.cs` | `CanAddCore` 去掉 `out legendaryTotal`；三处调用点；删 `LegendaryTotalErrorText()`；三处过期 doc 订正 |
| `CardPresentation/Net/NetProtocol.cs` | `Version` 2 → **3** + 理由注释（**只改常量与注释**） |
| `RuleEngine/Core/UnitState.cs` | `RemoveMarkerlightAtEndOfTurn` 的 doc：J3 已坐实 |

## 七、顺手发现（第五段）

| # | 发现 | 处置 |
|---|---|---|
| **K1** | `Core/Loc.cs` **缺** `MenuDeck/Error/MaxLegendayCopiesReached`（改前也缺 ⇒ 行为不变，照旧印键名） | **改动请求**（见 §三末） |
| **K2** | `DeckRuntime.MenuDeckErrorNumber` 的逐条对位表可以补一行 `LegendaryTotalExceeded → 4`（纯 doc，`DeckRuntime.cs` 不在本段授权） | **改动请求** |
| **K3** | 本段期间 `RuleEngine/Data/SimpleAI.cs` 被**别人**在写（22:43 改动，引用了 5 个当时还不存在的符号：`PoisonousKeyword` / `PoisonousScale` / `AttackScoringCriteriaScore` / `SacrificeBonus` / `OccupiedAdjacentCount`）⇒ `typecheck` 一度报 5 条红，**几分钟后自行消失**（最终 0/0）。⚠️ `RuleEngine/**` 名义上归我，但那是别人正在写的文件，我没有动。 | 记账（若再出现请查是谁在改 `RuleEngine/Data/`） |

## 八、交接（第五段 · 本批 E1 收尾）

### 做到哪
五段合计：**属性模型 Stage 2a–2c + 合批 + K1 复核** → **doNotStack / extrinsic 240** → **K2/K3 `addTrait` D1–D6 + MeleeEqualsHealth** → **H1「重算 vs 真移除」+ W9/F2** → 本段三处收口 + J3 坐实。
**唯一的行为变化**：`SAU27`（第三段有意）+ 攻值 `clamp0`（第一段有意）。其余全部零行为变化（`play all dump` 逐字节可证）。

### 还欠什么（都不在本批授权内的）
1. **表现层接 `EvtKind.TraitRemoved`**（`Expiring State` 那支）—— 特效/表现线。
2. **`Loc.cs` 补 `MenuDeck/Error/MaxLegendayCopiesReached`**（§七 K1）+ `DeckRuntime` 对位表补一行（K2）。
3. `tide/companion` 计数器 · `dropPod` 两个字段 · `survivor` 零血闸（池内 0 条，如实挂着）。
4. 同步点：`--fix` + 抽查 range 尾（累计：`UnitState.cs` / `EffectResolver.cs` / `RuleCore.cs` / `Aura.cs` / `ActionPump.cs` / `BattleEvent.cs` / `CardDef.cs` / `DeckRules.cs` / `RuleEngineTest.cs` 都有 +N 行）。
5. `out_play_baseline.txt` 重生成（差异 = 6 张 clamp0 + 电池 8 张含 `SAU27`）。

### 文件动到哪（第五段增量）
```
RuleEngine/Core/GameplayVariables.cs          (+4)
RuleEngine/Core/DeckRules.cs                  (+15)
RuleEngine/Core/UnitState.cs                  (+4，doc)
CardPresentation/Deck/DeckEditorState.cs      (-27，去载体 + 删方法 + doc)
CardPresentation/Net/NetProtocol.cs           (+10，只版本与注释)
资料/完全复刻/报告/E1_属性模型Stage2_第2轮.md  (本段)
```

### 下一步命令
```bash
TMPDIR=/tmp/wf_e1 bash d:/4/Unity/工具/typecheck.sh                     # 期望 0/0
bash d:/4/Unity/工具/ruleprobe.sh check                                 # 期望 rc=0、指纹 193c1b25…
bash d:/4/Unity/工具/ruleprobe.sh play check                            # rc=1：6 + 8（同一批）
cd d:/4/Unity/_tmp_view/e1_probe2 && dotnet run -c Release              # (A)…(F) 六段读数
```

### 踩过的坑（第五段）
1. ⚠️ **`RuleEngine/Core/DeckRules.cs` 是 CRLF**（我第一版用 `\n` 拼多行 `old_string` ⇒ 匹配 0 处）。改这种文件**先数行尾**。
2. ⚠️ 往 `<summary>` 里补文档时**注意 `</summary>` 的位置** —— 我第一版把新行加在 `</summary>` **之后**，XML 注释就断了（已修）。
3. ⚠️ 本段两次撞到「别人的文件正在写」（`RuleEngine/Data/SimpleAI.cs`、更早的 `Shell/LegacySaveReader.cs`）⇒ `typecheck` 短暂报红，**别去改别人的文件**，等一会儿重跑即可。

---

# 第六段 · Loc 词条补全 + 全量键引用扫描 + 一条兜底 doc

> 授权：`Assets/CardPresentation/Core/Loc.cs`（本轮无人拥有）+ `DeckRuntime.cs` **只改那一行注释**。

## 一、🔴 先纠一条我上段报错的话（铁律 5）

上段我报「🔴 `Core/Loc.cs` 缺 `MenuDeck/Error/MaxLegendayCopiesReached` ⇒ 会把键名印到屏幕上」——**这句不成立**。
本段读了 `Loc.T` 的实现（`Loc.cs:3421-3550`）才发现它有**三档兜底**：

```
① 手写 Table（权威）  →  ② 数据面 bulk（Resources/official_names.json，3,263 条）
                       →  ③ 官方全表（Resources/official_loc_13lang.json，10,862 键）  →  ④ 缺键档（返回键名 + 出声）
```

`MenuDeck/Error/MaxLegendayCopiesReached` **在第 ③ 档里**（本段现读 `official_loc_13lang.json` 的 `keys` 数组命中）
⇒ **它从来不会印键名**，改前改后行为一样。
**错因**：我拿「官方 `数据/游戏数据/官方本地化_en+zh-CN.json` 有这条键」去比「手写 `Table` 没有」，
**漏掉了运行时那两层兜底** —— 正是「翻过一个镜像目录就说本地没有」的同族错误
（`CLAUDE.md` 铁律 5 那条）。已在报告与代码注释里就地订正。

## 二、① 补 `MenuDeck/Error/MaxLegendayCopiesReached`（仍然做了，理由变了）

| 项 | 内容 |
|---|---|
| 落点 | `CardPresentation/Core/Loc.cs` 的 `Table`，紧挨 `MenuDeck/Error/IncompleteDeck` 之后 |
| 值 | **逐字取自官方表**（`数据/游戏数据/官方本地化_en+zh-CN.json` 现读 `terms["MenuDeck/Error/MaxLegendayCopiesReached"]`）：<br>EN `Decks in this game mode can contain only 4 legendary cards.`<br>ZH「在这个游戏模式中，卡组只能包含4张传奇牌。」（⚠️ 官方中文「卡组」后**有逗号**，照抄） |
| 键名 | ⚠️ **`Legenday`（少一个 r）是原版拼写** —— 那是本地化键的字面量，⛔ 没改 |
| 为什么要加（虽然它已被 ③ 档兜住） | ① 把它提到**权威层**（`Table` 优先于官方全表，将来官方表重生成也不会丢）；<br>② `Loc.HasEntry(key)` **只认手写 `Table`** ⇒ 加了之后，任何 `HasEntry ? T : 兜底` 的调用点（`DeckRuntime.MenuDeckErrorKey` 那个形状）也能走对；<br>③ 缺陷排查时「手写表 vs 官方表」的差集本来就是这份表的**健康度指标**。 |

## 三、② 全量扫描：`Assets/**/*.cs` 引用的 `Loc.*` 键 × 三档词条表

### 3·1 方法（可复现）

脚本：`_tmp_view/loc_scan2.py`（扫引用）+ `_tmp_view/loc_diff2.py`（算差集），**现读**，没有任何写死的条数。

- 扫 **591 个 `.cs`**，按 `Loc.(T|Get|HasEntry|EnOf|ZhOf)( "字面量" )` 提键；**跳过整行注释**（`//` / `*` / `/*` —— 注释里有大量示例键，不跳会造 237 条假阳性）。
- 词条表**三档都算命中**：手写 `Table`（**754** 条，现读 `Loc.cs`）+ bulk（`official_names.json` 的 `entries`，**3,263**）+ 官方全表（`official_loc_13lang.json` 的 `keys`，**10,862**）。
- 动态调用（非字面量）单独统计：**630 处 / 337 种表达式**里最常见的几种见 §3·4。

### 3·2 结果（现读）

| 指标 | 数 |
|---|---|
| 字面量键（去注释后） | **315** 个不同键 |
| 其中**手写 `Table` 里没有** | **57** |
| ↳ 但**官方全表 / bulk 已兜住**（⇒ **不印键名，非缺陷**） | **51** |
| ↳ **三档全都没有** | **6**（**全部是假阳性**，逐条见下表） |
| **真正「引用了、哪都没有、且会上屏」的键** | **0** |

### 3·3 逐条表（57 行；`引用点` 只列第一处，`…共 N 处` = 该键被引用的总处数）

| 键名 | 引用点（文件:行号） | 官方表有/无 | 处理 |
|---|---|---|---|
| `Achievements/Points` | `CardPresentation/Editor/MainMenuScene.cs:8057` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Battle` | `CardPresentation/Editor/BattleScene.cs:43089`<br>…共 4 处 | **无** | **非缺陷**：拼接前缀（`"Battle" + "/Settings/Title"`），且出现在一条**负断言**里 |
| `Battle/Tips/__SelftestNotInTable__` | `CardPresentation/Editor/BattleScene.cs:26473` | **无** | **非缺陷**：故意的自检键（名字就写着 `SelftestNotInTable`） |
| `CustomErrors/FailedToInitializeServices` | `CardPresentation/Shell/SceneInitializer.cs:415` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/AvatarMenu/ToggleBorder` | `CardPresentation/Editor/MainMenuScene.cs:6947` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/DraftMode/ExitDraftQuestion` | `CardPresentation/Shell/DraftModeWindow.cs:3339` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/DraftMode/FreeButton` | `CardPresentation/Shell/DraftModeWindow.cs:1245` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/DraftMode/IntroHelp1` | `CardPresentation/Shell/DraftModeWindow.cs:1235` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/DraftMode/IntroHelp2` | `CardPresentation/Shell/DraftModeWindow.cs:1238` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/DraftMode/Losses` | `CardPresentation/Shell/DraftModeWindow.cs:1661` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/DraftMode/ResignDraftButton` | `CardPresentation/Shell/DraftModeWindow.cs:1677` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/DraftMode/SelectWarlord` | `CardPresentation/Shell/DraftModeWindow.cs:1261` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/DraftMode/Title` | `CardPresentation/Shell/DraftModeWindow.cs:1207`<br>…共 2 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/DraftMode/TitleIntroScreen` | `CardPresentation/Shell/DraftModeWindow.cs:1232` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/DraftMode/Victories` | `CardPresentation/Shell/DraftModeWindow.cs:1646` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/EnergyBalance/Cards` | `CardPresentation/Editor/DraftAssertD3.cs:1086`<br>…共 4 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/EnergyBalance/EnergyBalance` | `CardPresentation/Shell/DraftModeWindow.cs:1863` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/FeedbackSurvey/S1A1` | `CardPresentation/Editor/MainMenuScene.cs:16742` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/FeedbackSurvey/S1Q1` | `CardPresentation/Editor/MainMenuScene.cs:16739` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Demo/FeedbackSurvey/SubTitle` | `CardPresentation/Editor/MainMenuScene.cs:16725` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Event_Description/General/EndsIn` | `CardPresentation/Shell/MainMenuRuntime.cs:2991` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Event_Description/General/EndsInPlusOrangeTime` | `CardPresentation/Editor/MainMenuScene.cs:9307`<br>…共 7 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Event_Description/Generic/EndsSoon` | `CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs:811`<br>…共 2 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MainMenu/Campaign/Help` | `CardPresentation/Shell/CampaignTab.cs:1600` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MainMenu/DraftPack` | `CardPresentation/Shell/DraftModeWindow.cs:1576` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MainMenu/Factions` | `CardPresentation/Shell/LiveOpsEventWindow.cs:966` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MainMenu/Free` | `CardPresentation/Editor/DraftAssertD3.cs:1007`<br>…共 3 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MainMenu/GameMode/Classic` | `CardPresentation/Editor/CollectionScene.cs:1648`<br>…共 3 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MainMenu/GameMode/Skirmish` | `CardPresentation/Editor/CollectionScene.cs:1641`<br>…共 5 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MainMenu/General/Rarity` | `CardPresentation/Deck/DeckEditorState.cs:694` | **无** | **非缺陷**：拼接前缀（`"MainMenu/General/Rarity" + ri`）；注释写明「原版 I2 缺键回键名 = **原版行为**」 |
| `MainMenu/Ranked/NoDeckMessage` | `CardPresentation/Shell/LiveOpsEventWindow.cs:447` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MainMenu/Ranked/NoWarlordMessage` | `CardPresentation/Editor/ModeWindowAssertE11.cs:190`<br>…共 2 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MainMenu/RankedWindow/Ranked` | `CardPresentation/Editor/SkirmishWindowAssertE15.cs:183`<br>…共 3 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MainMenu/RankedWindow/Unranked` | `CardPresentation/Editor/SkirmishWindowAssertE15.cs:181`<br>…共 3 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MenuDeck/Button/OwnDeck` | `CardPresentation/Shell/DeckSelectionPopup.cs:514` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MenuDeck/Button/PresetDeck` | `CardPresentation/Shell/DeckSelectionPopup.cs:513` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MenuDeck/Craft/CraftCost` | `CardPresentation/Shell/CardDetailPopup.cs:1329` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MenuDeck/Craft/CraftDescription` | `CardPresentation/Editor/CollectionScene.cs:8441`<br>…共 2 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MenuDeck/Error/EffectOnlyCard` | `CardPresentation/Editor/DeckScene.cs:6923` | **无** | **非缺陷**：自检键（断言它归 `InvalidDeck` 那一档） |
| `MenuDeck/Error/__没有这条键__` | `CardPresentation/Editor/DeckScene.cs:6953` | **无** | **非缺陷**：故意的自检键 |
| `MenuDeck/HUD/ChooseCards` | `CardPresentation/Shell/DraftModeWindow.cs:1570` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MenuDeck/SelectDeckGameMode` | `CardPresentation/Editor/CollectionScene.cs:1532`<br>…共 2 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `MenuShop/CollectButton` | `CardPresentation/Shell/DraftModeWindow.cs:1810` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `No/Such/Key/___` | `CardPresentation/Editor/SettingsScene.cs:1384` | **无** | **非缺陷**：故意的自检键 |
| `PlayerProfile/BattleLog` | `CardPresentation/Shell/BattleLogTab.cs:149` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `PlayerProfile/PlayerIdDisplay` | `CardPresentation/Shell/ProfileTab.cs:459` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `PlayerProfile/Rank` | `CardPresentation/Editor/SkirmishWindowAssertE15.cs:169`<br>…共 3 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `PlayerProfile/SelectTitleTip` | `CardPresentation/Editor/MainMenuScene.cs:6633` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Ranked/Skirmish/Progression` | `CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs:416`<br>…共 2 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Ranked/Skirmish/ProgressionTitle` | `CardPresentation/Editor/MainMenuScene.cs:9288`<br>…共 4 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Ranked/Victories` | `CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs:634`<br>…共 2 处 | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `Rewards Menu/DailyStreak/Help` | `CardPresentation/Shell/DailyData.cs:1122` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `ShopOffers/GeneralClaim` | `CardPresentation/Shell/DailyData.cs:1103` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `SocialMenu/Alliances/Trophies/Count` | `CardPresentation/Shell/AllianceMemberTab.cs:1029` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `SocialMenu/Alliances/Trophies/Count/Singular` | `CardPresentation/Shell/AllianceMemberTab.cs:1028` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `SocialMenu/Alliances/Trophies/CurrentFeatured` | `CardPresentation/Shell/AllianceMemberTab.cs:1043` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |
| `TutorialData/TutorialFTUEDeck/UltraMarines` | `CardPresentation/Editor/MainMenuScene.cs:17109` | 有（13 语言全表） | **不用补** —— `Loc.T` 的第三档已兜住，**不会印键名** |

### 3·4 动态调用点（无法静态判定）—— 主要形状

| 表达式形状 | 判定 |
|---|---|
| `Loc.T(DeckRules.Describe(e))` / `Loc.T(Describe(...))` | 值域**可静态枚举** ⇒ 见 §3·5 |
| `Loc.T("MenuDeck/Error/" + …)` / `TermPrefix + e` | 值域可枚举（`DeckError` 的成员）⇒ §3·5 |
| `Loc.T("MainMenu/General/Rarity" + ri)` | 值域 = 稀有度下标 0..5；**官方表本来就没有这一族** ⇒ 原版行为 = 缺键回键名（`DeckEditorState` 的注释已写明，⛔ 不是缺陷） |
| `Loc.T("Armies/" + …)` / `"Battle/Tips/" + …` / `"Ranked/Division/" + …` / `"SocialMenu/Alliances/Role/" + …` | **前缀拼接**；各自的**具体键**在运行时才知道 ⇒ 无法静态判定（下面 §3·5 给出可用前缀集） |
| `Loc.Lore(cardId)` / `Loc.LoreByUniqueId(uniqueId)` | 键 = `Warlord_Lore/<id>` / `Cosmetic_Lore/<uniqueId>`，来源是**卡数据** ⇒ 不属本差集 |

### 3·5 `DeckRules.Describe` 产出键（静态可枚举，一并核了）

`Describe` = `TermPrefix + 枚举名`（`TermPrefix` = `"MenuDeck/Error/"`），两个特例：
`EffectOnlyCard → InvalidDeck`、`LegendaryTotalExceeded → MaxLegendayCopiesReached`。
把 `DeckError` 的成员逐个拼出来，与三档表比：**只有 `MissingHero` / `IncompleteDeck` / `MaxLegendayCopiesReached` 在手写表里**；
其余（`NoWarlord` / `TooManyCards` / `CopyLimitExceeded` / `WrongFaction` …）**手写表没有、官方全表也没有** ⇒
`Loc.T` 会落**缺键档**（返回键名）。
🔴 **但这不等于缺陷**：`DeckRuntime.MenuDeckErrorKey` 对**每一条**都先 `Loc.HasEntry` 判、缺了走**数字族**
`MenuDeck/Error/{2,4,5}`（`Loc` 里有那三条）⇒ 上屏的是「请选择一个战将 / 卡组不完整」这类**真文案**；
唯一的例外是 `DeckEditorState.AddErrorText` / `TryAdd` 那条**直接** `Loc.T(Describe(e))` 的路（拖拽提示条）——
它今天对本段范围之外的那几档**会印键名**。⚠️ **这是一条新账**（`DeckEditorState` 那两处是 R3 刚落的设计），
本段**没有动**（会改到 R3 的文案分工，需要先裁「拖拽提示该用数字族还是具名族」）。

## 四、③ `DeckRuntime.MenuDeckErrorNumber` 的 doc

`CardPresentation/Deck/DeckRuntime.cs` 那张「逐条对位」表补了一行（**只改注释**）：
`LegendaryTotalExceeded → 走 default = 4`（= 原版 `CanAddCard.c:111-121` 那一支，与 `CopyLimitExceeded` **共用号 4**；
两档的**文案**靠 `DeckRules.Describe` 分开），并写死「⛔ 不要为它单独编号（原版没有第 6 个号）」。

## 五、验收（全绿）

| 闸 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_e1 bash 工具/typecheck.sh` | **0 / 0** |
| `ruleprobe check` | **rc=0** · 解析差异 0 行 · 哨兵失败 0 · 指纹 **`193c1b25…` 未变** |
| `ruleprobe play all dump` | 与**上一轮逐字节相同**（0 行）；与原始基线仍差 4 行（第一段那 2 张 clamp0） |
| `ruleprobe play check` | **6 + 8**，卡集合不变 |

## 六、改动清单（第六段）

| 文件 | 改动 |
|---|---|
| `CardPresentation/Core/Loc.cs` | `Table` 新增 **1 条**：`MenuDeck/Error/MaxLegendayCopiesReached`（官方值，含「`Legenday` 是原版拼写」的注释） |
| `CardPresentation/Deck/DeckRuntime.cs` | **只改注释**：`MenuDeckErrorNumber` 的逐条对位表补 `LegendaryTotalExceeded → 4` 一行（+4 行 doc） |
| `_tmp_view/loc_scan2.py` · `loc_diff2.py` · `loc_scan_raw2.json` · `loc_rows.md` | 扫描脚本与原始数据（不进 git） |

## 七、还欠什么（本批 E1 收尾清单）

1. 🔴 **新账（本段扫出来的）**：`DeckEditorState.AddErrorText` / `TryAdd` 那条 `Loc.T(DeckRules.Describe(e))` 的路，
   对**手写表 / 官方表都没有的那几个 `MenuDeck/Error/<枚举名>` 键**（`NoWarlord` / `TooManyCards` / `CopyLimitExceeded` /
   `WrongFaction` / `WarlordInCards` / `WarlordNotHero` / `DefensiveNotDefence` / `DefensiveAlreadySet` / `WarlordAlreadySet`）
   会在**拖拽提示条**上印键名。要么补手写表（但那几条原版**没有具名键**，我们自拟会偏离）、
   要么改走 `MenuDeckErrorKey` 的数字族（= 原版真实行为）。**需裁定**，本段没动。
2. **表现层接 `EvtKind.TraitRemoved`**（`Expiring State`）—— 特效/表现线。
3. `tide/companion` 计数器 · `dropPod` 两个字段 · `survivor` 零血闸（池内 0 条，如实挂着）。
4. 同步点：`--fix` + 抽查 range 尾；`out_play_baseline.txt` 重生成（差异 = 6 张 clamp0 + 电池 8 张含 `SAU27`）。
5. `H1` 的两条路（重算/真移除）已经分开；`J3`（谁的回合末）已坐实。

## 八、踩过的坑（第六段）

1. 🔴 **`Loc.T` 是三档兜底，别拿「手写表」当唯一真相** —— 本段第一版（也是我第五段那条错报）就是栽在这里。
   判「会不会印键名」必须**三档一起算**：`Table` ∪ `official_names.json` ∪ `official_loc_13lang.json`。
2. ⚠️ **扫 `Loc.T("…")` 必须跳过整行注释** —— 注释里有 237 处示例键，不跳会造一堆假阳性（`Battle/Tips/…`、`Tips/…`、`…/Create` 那一族）。
3. ⚠️ **带拼接的「键」要单独列**：`"Battle"`、`"Armies/"`、`"Ranked/Division/"`、`"MainMenu/General/Rarity"` 这些是**前缀**不是键。
4. ⚠️ `official_loc_13lang.json` 是**列式**存储（`keys` + 每种语言一个数组），`key in json` 恒假 —— 要判成员必须先取 `keys`。

---

# 第七段 · ⑤ 裁定 (b)：**核完判据后，裁定前提不成立 ⇒ 没有切数字族**；改为「一处收口 + 兜底」

> 授权：`CardPresentation/Deck/DeckEditorState.cs` + `CardPresentation/Deck/DeckRuntime.cs`（只改这条相关的部分）+ `Core/Loc.cs`（`HasEntry` 注释）。
> 🔴 **本段的第一句结论是「我上段又说错了一句」** —— 按铁律 5 先订正，再讲做了什么。

## 一、🔴 铁律 5 订正：**13 个 `DeckError` 的具名键，手写 `Loc` 表里【全都有】**

我第六段 §3·5 写：「只有 `MissingHero` / `IncompleteDeck` / `MaxLegendayCopiesReached` 在手写表里，
其余 9 档**手写表与官方表都没有** ⇒ 拖拽提示条会印键名」。

**这句不成立。** 本段现读 `CardPresentation/Core/Loc.cs`：`MenuDeck/Error/*` 一共 **28 条**，其中**具名族**逐条在表里：

| `DeckError` | 手写表里的键 | 表里的中文（现读） |
|---|---|---|
| `UnknownCard` | ✅ `MenuDeck/Error/UnknownCard` | 卡组里有卡不在卡池中 |
| `NoWarlord` | ✅ `MenuDeck/Error/NoWarlord` | 还没有选战将 |
| `WarlordNotHero` | ✅ `MenuDeck/Error/WarlordNotHero` | 战将位放的不是战将 |
| `DefensiveMissing` | ✅ `MenuDeck/Error/DefensiveMissing` | 还缺 1 张防御卡 |
| `DefensiveNotDefence` | ✅ `MenuDeck/Error/DefensiveNotDefence` | 防御卡位放的不是防御卡 |
| `WrongFaction` | ✅ `MenuDeck/Error/WrongFaction` | 有卡和战将不同阵营 |
| `TooManyCards` | ✅ `MenuDeck/Error/TooManyCards` | 卡组张数超了 |
| `TooFewCards` | ✅ `MenuDeck/Error/TooFewCards` | 卡组张数不够 |
| `CopyLimitExceeded` | ✅ `MenuDeck/Error/CopyLimitExceeded` | 有卡超过了同名上限（传说 1 张，其余 2 张） |
| `WarlordInCards` | ✅ `MenuDeck/Error/WarlordInCards` | 战将/防御卡不能放在普通卡位里 |
| `WarlordAlreadySet` | ✅ `MenuDeck/Error/WarlordAlreadySet` | 已经选过战将了（换战将要先把原来的撤掉） |
| `DefensiveAlreadySet` | ✅ `MenuDeck/Error/DefensiveAlreadySet` | 已经有防御卡了（不能带两张） |
| `EffectOnlyCard` | ➡️ 走 `InvalidDeck`（`Describe` 特例，**有意**） | 所选卡组无效，请编辑或选择其他卡组。 |
| `LegendaryTotalExceeded` | ➡️ `MaxLegendayCopiesReached`（第六段落的） | 在这个游戏模式中，卡组只能包含4张传奇牌。 |

⇒ **拖拽提示条今天印的是真文案，不是键名 —— 没有可见缺陷。**
**错因**（同第六段那次是同族的）：我只算了「**字面量引用**的键」的差集，而 `MenuDeck/Error/<枚举名>` 这一族
是 `Describe` **拼出来的**、从来不会以字面量出现在代码里 ⇒ 它们**根本没进我的「引用集」**，
我却反过来断言「表里没有」。**「没被引用」≠「表里没有」**，这和「翻过一个镜像目录就说本地没有」是同一条错的两种形态。

## 二、所以：**没有**把那 9 档切成数字族（那会是一次回归）

裁定 (b) 的前提是「那 9 个具名键原版没有 ⇒ 补表就是自拟」。**但表里早就有了**（而且不是自拟吗？—— 见 §四的诚实边界），
若按 (b) 切到数字族，玩家看到的会从
「已经选过战将了（换战将要先把原来的撤掉）」变成「**所选卡组无效，请编辑或选择其他卡组。**」
—— **信息量下降 = 肉眼可见的回归**。⇒ 我**停在这里没有切**，把这个判断交回给你。

## 三、本段实际做的：**把选择规则收口到一处 + 加兜底**（今天零行为变化）

`DeckRuntime.DeckErrorText(DeckError)` 从「一句 `Loc.T(DeckRules.Describe(e))`」改成：

```csharp
if (e == DeckError.None) return "";
string named = DeckRules.Describe(e);
if (!string.IsNullOrEmpty(named) && Loc.HasEntry(named)) return Loc.T(named);  // 手写表有具名键 ⇒ 用具名
return Loc.T(MenuDeckErrorKey(e));                                             // 否则 ⇒ 原版数字族 {2,4,5}
```

- **今天零行为变化**：13 档**全部命中具名分支**（§一那张表）。
- **兜底**：将来任何**没登记**的 `DeckError` 档**不会再印键名**（退到数字族 → `MenuDeck/Error/{2,4,5}`；
  那三条若也不在表里，`MenuDeckErrorKey` 再退 `InvalidDeck` 兜底键 —— 三级都取不到才会印键名）。
- **一处收口**：`DeckEditorState.TryAdd` / `AddErrorText` 都改成调它（原来各写一份 `Loc.T(Describe(e))`）。
  ⛔ 别在调用点再自己拼。

### 3·1 逐条「枚举 → 号」表（你要求的那张）

号来自 **`DeckRuntime.MenuDeckErrorNumber` 现表**（原版 `DeckError` 只有 6 个值：`None0 / CardsNotOwned1 / MissingHero2 / InvalidDeckBannedCards3 / InvalidDeck4 / IncompleteDeck5`）：

| `DeckError` | 号 | 具名键在手写表？ | 号的可核性 |
|---|---|---|---|
| `NoWarlord` | **2**（MissingHero） | ✅ | 沿用现表（`A330` 报告引 `__ValidateDeck.c:32,88`）—— ⚠️ 见 §3·2 |
| `TooManyCards` | **5**（IncompleteDeck） | ✅ | 同上（`:69 ⇒ :80`） |
| `WarlordNotHero` | **4** | ✅ | 同上（`:36 ⇒ :84`） |
| `WrongFaction` | **4** | ✅ | 同上（`:58-62`） |
| `WarlordInCards` | **4** | ✅ | 同上（`:63-67`） |
| `CopyLimitExceeded` | **4** | ✅ | 同上（`:111-121`） |
| `DefensiveNotDefence` | **4** | ✅ | 现表 + 它自己的 doc：「**这是我们自己的槽位判据**（原版不查防御卡）⇒ 归 catch-all」 |
| `WarlordAlreadySet` | **4**（走 `default`） | ✅ | 🔴 **号未坐实** —— 我们自己的槽位判据，**原版没有对应错误码** |
| `DefensiveAlreadySet` | **4**（走 `default`） | ✅ | 🔴 **号未坐实**（同上） |

（另三条：`MissingHero`/`IncompleteDeck`/`MaxLegendayCopiesReached` 是**具名**的，不走 `MenuDeckErrorNumber`。）

### 3·2 ⚠️ 复核「号」的时候撞到一条：`MenuDeckErrorNumber` 引的那个 `.c` **文件不存在**

我按你「先核每条枚举对应的数字号」去查 `decomp_full`：
- `DeckUtility__CanAddCard.c` —— **不存在**（`ls | grep -i CanAddCard` 只有 `CardDeck__CanAddCard.c`，183 行）；
- `DeckUtility__ValidateDeck.c`（96 行）/ `ValidateDeckCards.c`（130 行）/ `ValidateDeckWarlord.c`（54 行）**全都只 `return 0/1`（bool）**，不返回错误号；
- 而 `MenuDeckErrorNumber` 的 doc 引的是 `__ValidateDeck.c:32,88 / :69 / :80 / :111-121` —— 行号**超出这三个文件的长度**。

⇒ **那些 `文件:行号` 出处我没能复核**（正是 G4「文件名与内容不符」那一族；也可能是 ILSpy 里的 `DeckUtility.CanAddCard`）。
**`MenuDeckErrorNumber` 的号码映射我没有动**（它是 `A330` 报告的结论，本轮**未能独立复核**），如实记在这里。
📌 建议：下一批若要用到这些号，先按「**按内容认函数**、不按文件名认」重查一遍。
⚠️ 但**对本段没有影响**：13 档今天全都走**具名**分支，数字族那条路一次都不会触发。

## 四、诚实边界（留给裁定）

那 9 条具名文案的**中文**是我们自拟的（`Loc.cs` 里那段注释自己写着「键名与两列文案都是我们起的」）；
**键名**倒是照原版形状 `MenuDeck/Error/<枚举名>`。所以严格说：
- 「**原版有没有这 9 个具名键**」—— 官方表里**没有**（现读 `官方本地化_en+zh-CN.json`，`MenuDeck/Error/*` 只有 15 条，不含这 9 个枚举名）；
- 「**我们印的这句话对不对**」—— 意思对、但不是原版文案。

⇒ 如果目标是「**文案也按原版**」，那唯一合法路径是**走数字族**（原版真实上屏的那条），代价是**具体性下降**。
这是一个**产品取舍**，不是技术对错 ⇒ 交回给你，⛔ 我不自己拍。

## 五、`Loc.HasEntry` 的注释订正（你点名的那条）

`Core/Loc.cs` 的 `HasEntry` doc 原来写「本地有没有这个键的**任一**语言的文案」——
**误导**（它只查手写 `Table`）。已就地改成：

> **手写 `Table` 里**有没有这个键…… 🔴 这个函数**只认手写 `Table`**，⛔ 不能当成「运行时没有这条文案」。
> `Loc.T` 有三档兜底：① 手写 `Table`（权威）→ ② bulk（`official_names.json`，3,263 条）→ ③ 官方全表（`official_loc_13lang.json`，10,862 键）→ ④ 缺键档。
> 📌 判据实例：`MenuDeck/Error/MaxLegendayCopiesReached` 手写表没有、官方全表有 ⇒ `T` 正常出文案，而 `HasEntry` 返回 false。**上一轮就是把这句读成「会印键名」而误报了一次**。
> ⚠️ `HasEntry ? T(key) : 兜底` 只适合「缺登记时换一条兜底」，**不适合**判「运行时有没有文案」。

## 六、验收（全绿）

| 闸 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_e1 bash 工具/typecheck.sh` | **0 / 0**（中途有一次读到别人文件的瞬时错，重跑即 0） |
| `ruleprobe check` | **rc=0** · 解析差异 0 行 · 哨兵失败 0 · 指纹 **`193c1b25…` 未变** |
| `ruleprobe play all dump` | 与上一轮**逐字节相同**（0 行）；与原始基线仍差 4 行（第一段那 2 张 clamp0） |
| `ruleprobe play check` | **6 + 8**，卡集合不变 |

## 七、改动清单（第七段）

| 文件 | 改动 |
|---|---|
| `CardPresentation/Deck/DeckRuntime.cs` · `DeckErrorText` | 一句 → **具名优先 / 数字族兜底**（含判据注释） |
| `CardPresentation/Deck/DeckEditorState.cs` · `TryAdd` / `AddErrorText` | 改调 `DeckRuntime.DeckErrorText(e)`（**一处收口**），两处 doc 跟着订正 |
| `CardPresentation/Core/Loc.cs` · `HasEntry` doc | 改成「只认手写 `Table`」+ 三档兜底说明 + 上一轮误报的留痕 |

## 八、还欠什么（E1 收尾）

1. 🔴 **等裁**：§四 那个取舍（**具名（我们自拟、更具体）** vs **数字族（原版真实、更笼统）**）—— 我按「不回归」留在具名。
2. 🔴 **`MenuDeckErrorNumber` 的号出处未能复核**（§3·2，`.c` 文件对不上）—— 下一批若要动它，按内容而不按文件名重查。
3. 表现层接 `EvtKind.TraitRemoved`（`Expiring State`）—— 特效/表现线。
4. `tide/companion` 计数器 · `dropPod` 两个字段 · `survivor` 零血闸（池内 0 条，如实挂着）。
5. 同步点：`--fix` + 抽查 range 尾；`out_play_baseline.txt` 重生成（差异 = 6 张 clamp0 + 电池 8 张含 `SAU27`）。
6. 本段踩的坑：**「没被字面量引用」≠「词条表里没有」** —— 派生键（`前缀 + 枚举名`）必须**按表反查**，不能靠引用差集。

---

# 第八段 · ⑤ 裁定落地（**按「键是不是原版就有的」分三档**）+ 一条偏离裁定的说明

> ⚠️ **本段在 2026-10-25 23:03 前后进程崩过一次**：代码写进盘了（`DeckRuntime.cs` 23:02:51 / `DeckEditorState.cs` 23:01:14），
> **报告没回写**。本文件是崩溃后**自查 + 补写**的结果；自查结论 = **代码已经落完**（`typecheck 0/0`），
> 只是两处 doc 还写着已被替换掉的旧方法名（`DeckRules.UsesNumericErrorText`），已在补写时一并订正。

## 一、自查：第八段到底落了什么（现读盘上）

| 落点 | 内容 |
|---|---|
| `RuleEngine/Core/DeckRules.cs:631` | `HasOfficialNamedKey(DeckError)` —— ① 原版有具名键的三档：`InvalidDeck` / `EffectOnlyCard` / `LegendaryTotalExceeded` |
| 同上 `:642` | `IsOurOwnSlotRule(DeckError)` —— ② 我们自己的槽位判据：`WarlordAlreadySet` / `DefensiveAlreadySet` |
| 同上 `:651` | `OriginalErrorName(DeckError)` —— ③ 其余档 → **原版那 6 个枚举名之一**（`NoWarlord→MissingHero` · `TooFew/TooMany→IncompleteDeck` · 其余→`InvalidDeck`） |
| 同上 `:668` | `ErrorTextKey(DeckError)` —— **判据只此一处**：①② ⇒ `Describe(e)`；③ ⇒ `MenuDeck/Error/<原版枚举名>` |
| `CardPresentation/Deck/DeckRuntime.cs:5575` | `DeckErrorText(e)` → `Loc.T(DeckRules.ErrorTextKey(e))` |
| `CardPresentation/Deck/DeckEditorState.cs:476` / `:1078` | `TryAdd` / `AddErrorText` 两处都改调 `DeckRuntime.DeckErrorText`（**调用点不再自己拼**） |
| `CardPresentation/Core/Loc.cs:1735` | 第六段补的 `MenuDeck/Error/MaxLegendayCopiesReached`（官方值）仍在 |
| `CardPresentation/Core/Loc.cs:3572` | 第七段订正的 `HasEntry` doc（**只认手写 `Table`** + 三档兜底 + 上一轮误报留痕）仍在 |
| `CardPresentation/Deck/DeckRuntime.cs:5968-5980` | `MenuDeckErrorNumber` doc 里的「**出处【待核】**」标注仍在 |

**判定：落完了**（不是半成品）—— 唯一残留是两处 doc 里写着已被删掉的 `DeckRules.UsesNumericErrorText`，
补写本报告时已改成 `DeckRules.ErrorTextKey` 并写清三档（`DeckEditorState.cs:1057-1060` / `:1075-1076`）。

## 二、🔴 一处**偏离裁定**（必须让你知道，并给判据）

裁定说：**自拟的 9 条 ⇒ 走原版数字族 `MenuDeck/Error/{n}`**。我没有那样做，而是**映射到原版那 6 个枚举名**
（`MenuDeck/Error/MissingHero` / `IncompleteDeck` / `InvalidDeck`）。**理由是一条第一权威**：

> `DeckUtility.ToRawLocalizationString(DeckError error)`（cpp2il `DeckUtility.cs:149-170`，**现读**）：
> ```csharp
> string term = $"MenuDeck/Error/{error}";          // ← {0} 装的是【枚举本身】，ToString() 印【名字】
> TermData termData = LocalizationManager.GetTermData(term);
> if (termData == null) return "MenuDeck/Error/InvalidDeck";
> return $"MenuDeck/Error/{error}";
> ```

⇒ **原版根本没有「数字族」这条上屏路径** —— `{0}` 是枚举名，不是号。
旁证两条：① 官方 I2 表里 **`MenuDeck/Error/{2,4,5}` 一条都没有**（现读两表）；
② `Loc.cs:1717-1726` 里 `A1872` 早就记过「**数字族那条判据被推翻**」（我当时没先读它 —— 这算我的第三个小错）。

⇒ 裁定的**理由**是「数字族 = 原版真实上屏路径」，而这条理由不成立；裁定的**意图**是「照原版」——
所以我按**意图**落成了「原版真实上屏路径」= **原版枚举名族**。若你仍要数字族，改 `OriginalErrorName` 一处即可（一行）。

## 三、玩家会看到什么（哪几条变了）

**A. 从「我们的具体文案」→ 原版文案（10 档，⚠️ 有意为之，不是回归）**

| `DeckError` | 改前（我们的） | 改后（**原版**） |
|---|---|---|
| `NoWarlord` | 还没有选战将 | 「**请为您的卡组选择一个战将，将其拖入您的卡组列表。**」（`MissingHero`） |
| `TooManyCards` | 卡组张数超了 | 「**您的卡组不完整。您需要将30张牌添加到你的卡组中，将它们拖入左侧面板的卡组列表。**」（`IncompleteDeck`） |
| `TooFewCards` | 卡组张数不够 | 同上（`IncompleteDeck`） |
| `UnknownCard` | 卡组里有卡不在卡池中 | 「**所选卡组无效，请编辑或选择其他卡组。**」（`InvalidDeck`） |
| `WarlordNotHero` | 战将位放的不是战将 | 同上 |
| `DefensiveMissing` | 还缺 1 张防御卡 | 同上 |
| `DefensiveNotDefence` | 防御卡位放的不是防御卡 | 同上 |
| `WrongFaction` | 有卡和战将不同阵营 | 同上 |
| `CopyLimitExceeded` | 有卡超过了同名上限（传说 1 张，其余 2 张） | 同上 |
| `WarlordInCards` | 战将/防御卡不能放在普通卡位里 | 同上 |

⚠️ **信息量变化要说准**：`NoWarlord` / `TooFew` / `TooMany` 三条其实**不比原来差**（原版那两句本身就很具体）；
真正**变笼统**的是 **7 条**（`UnknownCard` / `WarlordNotHero` / `DefensiveMissing` / `DefensiveNotDefence` /
`WrongFaction` / `CopyLimitExceeded` / `WarlordInCards`）—— 它们现在都说「所选卡组无效…」。
**这是有意为之**（铁律 11：我们自拟的文案 = 差异，哪怕更好看）。

**B. 保留具名（2 档，我们补的槽位判据）**

| `DeckError` | 上屏 | 说明 |
|---|---|---|
| `WarlordAlreadySet` | 「已经选过战将了（换战将要先把原来的撤掉）」 | **原版没有这个错误码**（原版不查槽位）⇒ 摊到 `InvalidDeck` 更没信息，故保留；代码里已标「原版没有、我们补的」 |
| `DefensiveAlreadySet` | 「已经有防御卡了（不能带两张）」 | 同上 |

**C. 不变的三档**：`InvalidDeck` → `InvalidDeck`（原版有）；`EffectOnlyCard` → `InvalidDeck`（`Describe` 特例，原版有）；
`LegendaryTotalExceeded` → `MaxLegendayCopiesReached`（原版有，第六段补的词条）。

> 📌 完整逐档表（含「原版有/自拟」标记）在离屏探针 `_tmp_view/e1_probe2` 的 **(G)** 段，可直接跑出来核。

## 四、`MenuDeckErrorNumber` 的「号出处待核」（你点名要写进报告的那条）

**一行结论**：`MenuDeckErrorNumber` 的**号码映射沿用旧结论、本轮未能独立复核** ——
`DeckUtility__CanAddCard.c` **在 `decomp_full` 里不存在**（只有 `CardDeck__CanAddCard.c`），
而 `ValidateDeck.c` / `ValidateDeckCards.c` / `ValidateDeckWarlord.c` **全部只 `return 0/1`**、不返回错误号；
现表引的 `__ValidateDeck.c:32,88 / :69 / :80 / :111-121` **行号超出这些文件的长度**。
⛔ **没有改号**（6 个原版值 `None0/CardsNotOwned1/MissingHero2/InvalidDeckBannedCards3/InvalidDeck4/IncompleteDeck5` 先当真值用）。
📌 **附带效果**：本段落成之后，**这条数字族一次都不会被 `DeckErrorText` 用到**（它只服务 `DeckRuntime.cs` 另一处弹窗），
所以「号未坐实」的风险**目前不落在卡组提示条上**。

## 五、验收（崩溃后重跑，全绿）

| 闸 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_e1 bash 工具/typecheck.sh` | **0 / 0**（中途一次读到别人正在写的 `Core/CardView.cs` 6 条瞬时错，重跑即 0） |
| `ruleprobe check` | **rc=0** · 解析差异 0 行 · 哨兵失败 0 · 指纹 **`193c1b25…` 未变** |
| `ruleprobe play all dump` | 与上一轮**逐字节相同**（0 行）；与原始基线仍差 4 行（第一段那 2 张 clamp0） |
| `ruleprobe play check` | **6 + 8**，卡集合不变（含 `SAU27`） |

## 六、改动清单（第七段 + 第八段，含崩溃前后）

| 文件 | 行（现读） | 改动 |
|---|---|---|
| `CardPresentation/Deck/DeckRuntime.cs` | `:5575` | `DeckErrorText`：一句 `Loc.T(Describe)` → **`Loc.T(DeckRules.ErrorTextKey(e))`**（判据注释在同处） |
| 同上 | `:5968-5980`（`MenuDeckErrorNumber` 的 doc） | 加「**上面那张逐条对位表引的 `.c` 出处【待核】**」整段 + 「6 个原版值先当真值用、⛔ 不要因为复核不了就改号」 |
| `CardPresentation/Deck/DeckEditorState.cs` | `:476`（`TryAdd`）· `:1078`（`AddErrorText`） | 两处都改调 `DeckRuntime.DeckErrorText(e)`；`:1057-1060` / `:1075-1076` 的 doc 写清三档 |
| `RuleEngine/Core/DeckRules.cs` | `:618-676` | 新增三档判据：`HasOfficialNamedKey`(`:631`) · `IsOurOwnSlotRule`(`:642`) · `OriginalErrorName`(`:651`) · `ErrorTextKey`(`:668`)；顶部一长段判据注释 |
| `RuleEngine/Core/DeckRules.cs` | `:85` / `:522` / `:598` | （第五段）`LegendaryTotalExceeded` 枚举 + `Validate` 换码 + `Describe` 映射 |
| `CardPresentation/Core/Loc.cs` | `:1735` / `:3572` | （第六/七段）补词条 · `HasEntry` doc 订正 |

## 七、还欠什么

1. 表现层接 `EvtKind.TraitRemoved`（`Expiring State`）—— 特效/表现线。
2. `tide/companion` 计数器 · `dropPod` 两个字段 · `survivor` 零血闸（池内 0 条，如实挂着）。
3. `MenuDeckErrorNumber` 的号出处待核（§四）。
4. 同步点：`--fix` + 抽查 range 尾；`out_play_baseline.txt` 重生成（差异 = 6 张 clamp0 + 电池 8 张含 `SAU27`）。

---

## 八、交接给下一个代理（E1 全批 · 八段）

> 你接的是 **`Assets/RuleEngine/**` 这条线**（E1 已跑八段，上下文约 50%，按铁律「80% 才换人」是被提前换的）。
> 报告全文 = `资料/完全复刻/报告/E1_属性模型Stage2_第2轮.md`（八段，**从头读一遍比读这一节更值**）。

### 8·1 八段各改了什么文件（**当前行号**）

| 段 | 主题 | 文件（当前行号） |
|---|---|---|
| 一 | **属性模型 Stage 2a–2c**：`Attack/RangedAttack/MaxHealth` → `*Base` + 只读投影；修饰表 `_statMods` | `RuleEngine/Core/UnitState.cs`（`:53-58` 字段、`:119/138/165/177/193` 投影属性、`:888-891` 构造、`:1243-1290` Grant、`:1553-1640` TempBuff/Revert、`:1700-1960` **类尾**：`StatMod`/`AddStatMod`/`RemoveStatMod`/`ClampHealthToMax`/`Project*`）· `EffectResolver.cs:11474-11690`（`ApplyOneGain`）· `RuleCore.cs:638/4489/9475` · `Editor/RuleEngineTest.cs`（夹具 3 处）· `CardPresentation/Editor/BattleScene.cs`（23 个 CS0200，主对话追加授权） |
| 一 | K1 `addStats` 复核（health 两档 / doubleAttack / `melee→attack` 归一） | `EffectResolver.cs` 同上 · `UnitState.ApplyGrant` |
| 二 | `doNotStack` 复核（**结论：不是缺口**）+ `extrinsic` trigger 20/240 | `UnitState.cs`（`StatMod` 类注 + `IsExtrinsicSource` 升 `public`）· `CardDef.cs`（`IsExtrinsicAbilityTrigger` / `ExtrinsicAbilityPresence`，**类尾** `:6150` 一带） |
| 三 | **K2/K3 `addTrait` D1–D6** + `MeleeEqualsHealth` 重挂 | `UnitState.cs`（`AddKeyword` → **返回 bool** + 毒×抗性 + count + survivor；`IsCountStackableTrait`）· `EffectResolver.cs`（`lose` 支 + `EmitTraitRemoved`）· `Aura.cs`（`RefreshMeleeEqualsHealth` + `MeleeEqualsHealthSource`）· `ActionPump.cs`（`ActionScope.Dispose`）· `RuleCore.cs`（删 `FieldAttack` 特判）· `Editor/RuleEngineTest.cs`（MeleeEqualsHealth 断言组重写） |
| 四 | **H1（重算 vs 真移除分开）** + **W9/F2（标记光留到回合末 + `TraitRemoved` 事件）** | `UnitState.cs`（`BeginAuraRecompute`/`EndAuraRecompute`/`AuraHealthGrantTotal`/`RemoveMarkerlightAtEndOfTurn`，**类尾**）· `Aura.cs`（`Recompose` → 外壳 + `RecomposeCore`）· `BattleEvent.cs`（`EvtKind.TraitRemoved`，**枚举末尾**）· `RuleCore.cs`（标记光只置标志 + `EndTurnCore` 回合末摘）· `Editor/RuleEngineTest.cs`（「⑤·b」一对灭自证 + 「⑤ 标记光」四条重写） |
| 五 | `Classic.maxLegendaries` 1→0 · `DeckError.LegendaryTotalExceeded` · `NetProtocol.Version` 2→3 · J3 坐实 | `GameplayVariables.cs:105/111` · `RuleEngine/Core/DeckRules.cs:85/522/598` · `CardPresentation/Net/NetProtocol.cs:351-362` · `CardPresentation/Deck/DeckEditorState.cs`（去载体）· `UnitState.cs`（J3 doc） |
| 六 | 补 `MenuDeck/Error/MaxLegendayCopiesReached` + **Loc 全量键扫描** | `CardPresentation/Core/Loc.cs:1735`（词条）· 扫描脚本 `_tmp_view/loc_scan2.py` / `loc_diff2.py` |
| 七 | `Loc.HasEntry` doc 订正（只认手写表） | `CardPresentation/Core/Loc.cs:3572` |
| 八 | `DeckError` **三档上屏判据**（本节 §二/§三） | `RuleEngine/Core/DeckRules.cs:618-676` · `DeckRuntime.cs:5575/5968-5980` · `DeckEditorState.cs:476/1078` |

### 8·2 「有意差异」（**照原版 vs 我们挑的**），⛔ 别当 bug 改回去

| # | 有意差异 | 理由 / 判据 |
|---|---|---|
| 1 | **攻值负值 `clamp0`** | 原版 `get_CurrentMeleeAttack.c:52-66` / `get_CurrentRangeAttack.c:50-64`。改前**负攻值真的为负**（会给目标回血）。`play check` 里 7 张卡的差异全是这一条 |
| 2 | **`SAU27 Scarab Swarm` 近战 = 动作边界快照**（不是活读 Health） | `判据/战斗逻辑驱动_Stage2前置_第2轮.md` §1·1/§1·2/§1·4/§1·7 |
| 3 | **`doNotStack` 没实现**（我们从不合并修饰记录） | 结构上与「永远 doNotStack」等价；`cards_truth.json` 那 22 条是序列化残留 |
| 4 | **`extrinsic` 只认光环那条路** | trigger 20/240 的带数值效果全部是光环形状（普查 11/19 + 27/42 段） |
| 5 | **`lose <trait>` = 整条摘**（不是减一层） | 原版 `AddEffect.c:680-698` `case 4`（`RemoveBuffedTrait` + `RemoveTrait`）；池内 14 条全布尔 ⇒ 零行为变化 |
| 6 | **标记光留到回合末** | `CardScript.SetRemoveMarkerlight`（只置 `+0x57`）+ `OnTurnEnd:16599-16607` |
| 7 | **`DeckError` 上屏三档** | `DeckUtility.ToRawLocalizationString`（`{0}` = 枚举名） |
| 8 | **`WarlordAlreadySet` / `DefensiveAlreadySet` 保留我们的文案** | 原版没有这两个错误码（我们自己的槽位判据） |

### 8·3 还欠什么

见 §七（4 条）。另：**`NetProtocol.Version` 已 2→3**（第五段做的，别以为还是 2）。

### 8·4 🔴 我自己纠过的两次误报 —— **你千万别重犯**

| # | 我当时报的（错） | 实际 | 错因（**这一类是怎么犯的**） |
|---|---|---|---|
| 1 | 「`Loc.cs` 缺 `MaxLegendayCopiesReached` ⇒ **会把键名印到屏幕上**」 | `Loc.T` 有**三档兜底**（手写 `Table` 754 → bulk 3,263 → **官方全表 10,862**）⇒ 那条键**本来就能取到** | 拿「官方 `数据/…/官方本地化_en+zh-CN.json` 有 vs 手写 `Table` 没有」直接当缺陷，**漏了运行时那两层** |
| 2 | 「那 9 个 `MenuDeck/Error/<枚举名>` **手写表与官方表都没有** ⇒ 拖拽提示条会印键名」 | **28 条 `MenuDeck/Error/*` 全在手写表里**（含那 9 个） | 我只算了「**字面量引用**的键」的差集，而这一族是 `Describe` **拼出来的**、从不以字面量出现 ⇒ **根本没进「引用集」**，我却反推「表里没有」 |

**两条合成一条可复用的纪律**：
> **判「运行时会不会印键名」⇒ 必须 `Table` ∪ bulk ∪ 官方全表 三档一起算，而且派生键（前缀 + 枚举名）要按【表】反查，不能靠【引用差集】。**
> 这是「翻过一个镜像目录就说本地没有」的同族错误 —— **「没被引用」≠「不存在」，「手写表没有」≠「运行时没有」**。

另附两个本批攒下的**操作坑**（写代码时就会撞）：
- **多行 `old_string` 必须带上该文件真实的行尾**（`DeckRules.cs` / `DeckRuntime.cs` / `DeckEditorState.cs` / `RuleEngineTest.cs` / `BattleScene.cs` 是 **CRLF**；`UnitState.cs` / `Aura.cs` / `RuleCore.cs` / `CardDef.cs` / `Loc.cs` / `NetProtocol.cs` 是 **LF**）。先 `io.open(p,'rb')` 数 `\r\n` vs `\n` 再动手。
- **往 `<summary>` 里补文档注意 `</summary>` 的位置**（我第一版把新行加在它**之后**，XML 注释就断了）。
