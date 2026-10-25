# R10 · `profile.json` 六字段 → 中央档（切换单 C8）· 第 2 轮

> 执行代理 **R10**（第八十会话 · 批次 2）· 2026-10-25
> 白名单：`Shell/ProfileData.cs` · `Shell/SaveKeys.cs`（**只在必须时**）· `Shell/AchievementsData.cs`（**不存在** ⇒ 没建）
> 额外只动了一处「不属于我白名单但简报②点名」的地方：`Shell/LegacySaveReader.cs`（见 §三「越界说明」）。

---

## 一、结论

**① 迁字段：做完。② 切双写：做完**（按简报要求，① 的 dump 逐字段相等**先**跑绿，才做的 ②）。

| 步骤 | 状态 | 一句话 |
|---|---|---|
| ① 让中央档完整承载 `profile.json` 的全部语义 | ✅ | 中央 `CosmeticData` 现在的 payload **就是那份旧档的整份**（`ver` + 6 字段，逐字转存）；成就另按原版形状投影进 `AchievementsData`。旧的「只取 `avatarIndex/titleIndex`」代码已删。 |
| ② 把 `profile` 从 `RegisterMirrors` 的硬跳过名单里移出来 | ✅ | `Entry.CosmeticOnly` 拆成两个正交字段（`ProfileSpecial` / `NoMirror`），现读 `NoMirror = 0 条` ⇒ `CosmeticData` **已登记**双写。 |

三条验收读数（判据见 §二）：

1. **切前 dump vs 切后中央档逐字段相等** → ✅（`SaveJsonDiff.Diff == ""`；真实 fixture `profile.json` sha256 `a6331c8d…0039aa` 与 R4 记的一致）
2. **旧档 vs 切前 dump 逐字段相等** → ✅（双写写出去的那份 `profile.json` 不带 `_v`、7 个顶层键全在）
3. **老档（没有这些字段）读进来** → ✅ 走 `SaveDto` 的字段初始值（`ver=1` / `playerName=""` / `titleIndex=-1` / `avatarBorderOn=true` / 两个空数组），**不抛异常、不报错**；再写回时 7 个字段全部补齐（升级不丢字段）

工具读数：`TMPDIR=/tmp/wf_r10 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**；离线脚手架 `D:/tmp/wf_n6_verify/harness` → **PASS 296 / FAIL 0**（本轮开始时 236，**+60 条**）；变异矩阵 **M1–M16 全部会红**（§四·3）。

---

## 二、判据（每条都能复查）

### 2.1 「地雷」到底是什么（E3 / R4 两轮点名）

`Shell/ProfileData.cs` 的 `SaveDto`（现 `:982-997`）有 **7 个**字段 = `ver` + **6 个真数据**；
而 E3 当年只把 `avatarIndex` / `titleIndex` 迁进中央 `CosmeticData`。双写是「**同一份 payload 写两份**」
⇒ 一旦登记双写，就会**用 2 个字段覆盖掉那份 6 字段的旧档 = 抹掉玩家名与全部成就进度**。
E3 因此在 `RegisterMirrors()` 里硬跳过它（`E3_存档Step3-4_第2轮.md:21`），R4 又把这条钉成断言
（`R4_存档Step4切换_第2轮.md:38,176`）。

### 2.2 六个字段各归谁（原版出处 → 我们的落点）

| 旧档字段 | 语义 | **原版存哪**（判据） | 我们中央档落点 | 谁读 |
|---|---|---|---|---|
| `playerName` | 玩家显示名 | **PlayFab 玩家档案**：`PlayerDataManager.SetPlayfabDisplayName` → `PlayFabClientAPI.UpdateUserTitleDisplayName`（`d:/2/tools/decomp_full/PlayerDataManager__SetPlayfabDisplayName.c`）—— **不是任何一个 `PlayerDataKey`** | `CosmeticData.playerName` | `ProfileData.PlayerName`（档案窗 / 联机握手 / HUD 名牌） |
| `avatarIndex` | 选中头像下标 | `CosmeticData`（key 13） | `CosmeticData.avatarIndex` | `ProfileData.AvatarIndex` 及其全部读者 |
| `titleIndex` | 选中称号下标（−1 = 没选） | `CosmeticData`（key 13） | `CosmeticData.titleIndex` | `ProfileData.TitleIndex` / 战斗 HUD |
| `avatarBorderOn` | 头像边框开关 | `CosmeticData`（key 13） | `CosmeticData.avatarBorderOn` | `AvatarTab` |
| `achIds` / `achValues` | 成就进度（下标配对） | `AchievementsData`（key 10），类型 = `Dictionary<string, AchievementPersistence>`（`Warpforge_code/.../PlayerDataManager.cs:953`） | ① `CosmeticData` 里那份（旧档兼容 + **读口**）② `AchievementsData` **投影**（原版形状，只写） | `ProfileData.AchievementValue` / Trophies 页 |
| `ver` | 我们自己的格式记号 | 原版没有 | `CosmeticData.ver` | `ProfileData.SaveDto` |

🔴 **更正一条旧判据**：方案 `判据/存档体系_原版语义与施工方案.md` §六·1 写「`playerName` → 归 `NameChanges` 那条线」——
**这条不成立**：`PlayerDataKey.NameChanges`（值 47）装的是 `NameChangeRecord { originalName, changes[] }`
（**改名历史**，`Warpforge_code/.../NameChangeRecord.cs`），而当前显示名根本不在 `PlayerDataKey` 里。
已在 `SaveKeys.cs` 的 `NameChanges` 注释里就地钉死（**那句判据文档我没改**，见 §五「还欠」）。

### 2.3 `AchievementsData` 的原版形状（简报要求「先查清再动手」——**已查清**）

- `PlayerDataManager.rawAchievementsData` 的类型：`Dictionary<string, AchievementPersistence>`（`PlayerDataManager.cs:953`）。
- `Everguild.LiveOps.AchievementPersistence`（`Warpforge_code/Scripts/Assembly-CSharp/Everguild/LiveOps/AchievementPersistence.cs:6-17`）：
  `id` / `highestValue` / `value` / `enabled`；基类 `ChallengeProgress`：`collectedRewards` / `premiumCollectedRewards` /
  `synced`（`[JsonIgnore]`）/ `customData`（`[JsonIgnoreIfEmpty]`）/ `resetProgress`（`[JsonIgnoreIfEmpty(false)]`）。
- 反编译佐证字段偏移：`AchievementPersistence.CurrentValue` 的 getter/setter 读写 `+0x3c`（= `value`），ctor 把 `+0x40` 置 1（= `enabled`，`AchievementPersistence__.ctor.c`）。
- ⇒ 投影形状（我们写的）：`{"Ach_1":{"collectedRewards":0,"premiumCollectedRewards":0,"id":"Ach_1","highestValue":72,"value":72,"enabled":true},…}`
  （`customData` 空 ⇒ 不写；`synced` 本来就 `[JsonIgnore]`；`resetProgress=false` 按 `[JsonIgnoreIfEmpty(false)]` 省略）。
- **三处只能填默认值**（如实记，⛔ 不是原版读出来的）：`collectedRewards`（我们的档位是从进度值现推的，没有「领取」记录）、
  `highestValue`（我们的模型只有一个数；只涨不落的写点下与 `value` 相等，但 `SetAchievementValue` 显式调低时原版会保留最高值、我们不能）、
  `customData`（恒空）。

### 2.4 为什么整份塞进 `CosmeticData`（而不是把成就单独放 `AchievementsData`）

`SaveCenter` 的双写是 **1 个 key ↔ 1 份旧档路径**（`RegisterLegacyMirror` 还明确禁止两个 key 抢同一文件）。
`profile.json` 是**一个文件装 6 个字段** ⇒ 中央那份 payload 只能是这 6 个字段的**并集**，
否则双写必然「用少覆盖多」。`AchievementsData` 因此是**语义归属**上的投影（只写不读），
读口仍然从 `CosmeticData` / `profile.json` 拿 —— 这样用户**回滚旧客户端玩过一轮**时读到的还是最新的
（旧档里就有 `achIds/achValues`，而中央的投影会过期）。

---

## 三、改动清单

### 3.1 `Shell/ProfileData.cs`（1185 → 1258 行，+73）

| 位置 | 改了什么 |
|---|---|
| `:947-981` | 「本地存档」节头：新增 **6 字段归属表** + 双写口径（中央 key / 旧档路径 / 读法写法）+「`playerName` 不是 `NameChanges`」的更正 |
| `:1046 LoadFromDisk` | 改成 **`SaveCenter.ReadThroughLegacy(CosmeticData, SavePath)`**（先中央后旧档）。前面先探 `File.Exists(SavePath)` / `SaveCenter.Exists(CosmeticData)`：两边都没有 = 第一次跑，**不打假警告** |
| `:1090 SaveNow` | ① 序列化整份 `SaveDto` → ② **写盘前的闸**：`LegacySaveReader.MissingProfileFields(json)` 非 null ⇒ **拒绝写盘 + LogError**（少一个字段就写 = 抹数据）→ ③ `SaveCenter.SaveThroughLegacy(CosmeticData, SavePath, json)` → ④ `StoreOn` 时把成就投影写进 `AchievementsData` |

> 🔴 `SaveNow` 不再自己 `Directory.CreateDirectory` + `File.WriteAllText`：落盘全交给 `SaveCenter`（原子写 + 双写 + 失败出声）。

### 3.2 `Shell/LegacySaveReader.cs`（452 → 696 行，+244）

| 位置 | 改了什么 |
|---|---|
| `:13-45` 文件头 | 映射表：`profile.json` → `CosmeticData`（**整份**）+ `AchievementsData`（**投影**）；新增「6 个字段各归谁」的判据段（含 `playerName` 的原版出处、`AchievementsData` 的原版形状）；**删掉过期句**「本文件不接入任何现有子系统」（`ShellRuntime.Awake:322-326` 已经在调） |
| `:200 ImportProfile`（原 `ImportProfileCosmetic`） | ① 中央档不存在 ⇒ **旧档原文整份转存**（逐字，与其它 6 类同口径）② 存在但**读不出来** ⇒ 报失败、**一个字节都不写**（读口会退回旧档）③ 存在但**字段不全**（旧版 2 字段那种）⇒ **只补缺的键**、已有的键一个不动（`TryFillMissingProfileFields`）④ 存在且齐全 ⇒ 跳过（不覆盖）⑤ 无论哪支，`AchievementsData` 不存在时补一次成就投影 |
| `:268 ProfileFields` | `profile.json` 该有的 7 个顶层键（`ver` + 6），**唯一一份清单** |
| `:278 MissingProfileFields` | payload 缺哪几个顶层键（`null` = 齐）。**两处用**：导入时判「旧版 2 字段档」+ **写口**（`ProfileData.SaveNow`）的最后一道闸 |
| `:~300 TryFillMissingProfileFields` | 补缺迁移：**按键是否存在**判缺（不是按值，空名字/0 都是合法值）；成就两个数组**成对**补（只补一半会让 id/value 错位） |
| `:~405 / :410` `Entry` | `CosmeticOnly` → 拆成 `ProfileSpecial`（走专用导入）+ `NoMirror`（禁止双写，现读 0 条，留作防线） |
| `:~470` `Entries` | `profile.json` 那条改成 `ProfileSpecial = true`（不再 `CosmeticOnly`） |
| `:~480 RegisterMirrors` | 判据从 `if (e.CosmeticOnly) continue;` 改成 `if (e.NoMirror) continue;` ⇒ **`CosmeticData` 现在会登记**（E3/R4 的红线按前提解除，不是被删掉） |
| `:561 BuildAchievementsPayload` | 成就进度 → 原版 `AchievementPersistence` 字典形状（序数序，可 diff；`SaveJsonDiff.Quote` 转义） |
| 文件级 DTO | 新增 `LegacyProfileFields`（7 字段，**只给补缺迁移用**）；`LegacyProfileFields` 旧的 2 字段版 → 改名 `LegacyProfileAchievements`（只读 achIds/achValues）；删掉 `LegacyCosmeticPayload` |

### 3.3 `Shell/SaveKeys.cs`（只改注释，3 处）

| 行 | 改了什么 |
|---|---|
| `:70-76 AchievementsData` | 从「本轮空缺」→「🆕 C8 起 `ProfileData.SaveNow` 会写；形状 = 原版 `AchievementPersistence` 字典；**只写不读**」 |
| `:85-91 CosmeticData` | 从「只迁两个字段」→「**整份 7 字段**，双写期与 `profile.json` 1:1 镜像」+ 为什么 name/ach 也在这里 |
| `:204-212 NameChanges` | 新增「🔴 别把 `playerName` 塞进这个 key」+ 原版出处（PlayFab `UpdateUserTitleDisplayName`） |

### 3.4 越界说明（`LegacySaveReader.cs`）

简报的 ② 明确要求「把 `profile` 从 `RegisterMirrors` 的硬跳过名单里移出来」，而那份名单就在
`Shell/LegacySaveReader.cs`（不在我的 ✅ 白名单里，**但也不在「别人正在写」的名单里**）。
我只改了这一件事 + 与它同源的 profile 导入/投影（**同一个文件、同一个主题**），没有碰其它 key 的任何行为。
如果调度台认为这算越界，请让 R4 复核这一段的 diff（`git diff -- LegacySaveReader.cs`）。

---

## 四、断言 / 读数（dump 逐字段对比）

### 4.1 脚手架的新增断言（`D:/tmp/wf_n6_verify/harness/Program.cs`）

| 组 | 断言 |
|---|---|
| `LegacyImport()`（改） | `CosmeticData` 是**整份**（7 个顶层键）· 7 个字段逐一存在 · **与旧档逐字相等** · `AchievementsData` 建出来（6 条）· 形状（键=成就 id / `value`+`highestValue`=72 / `enabled` / 序数序） |
| `Step4Switch()` ⑤b（新） | `ProfileData(C8)` 走通用 `SwitchOne`：**切后中央档 vs 切前 dump**、**切后旧档 vs 切前 dump**、信封只一层、旧档无 `_v`、`DiffWithLegacy == ""`、与切前 dump **零差异** |
| `Step4Switch()` ⑥（改） | 登记 **7 条**（含 `CosmeticData`）· `CosmeticData` **已登记** · 在双写表里 · 旧档路径 = `<LegacyRoot>/WarpforgeProfile/profile.json` |
| `C8Profile()`（新，**36 条**） | ① 真名字档（`playerName="R10 玩家"`）导入后名字/边框/下标都活着 ② 切后**中央档 + 旧档**两边都与切前 dump 逐字段相等 ③ 成就投影与 `achIds/achValues` 一一对应（含 0 值也写）④ **老档缺字段** ⇒ 5 条默认值断言 + 写回 7 字段 ⑤ **DTO 漂移**（读 `ProfileData.cs` 源码抠 `SaveDto` 字段名，与 `PilotProfileDto` / `ProfileFields` 逐名比）⑥ **补缺迁移**：中央 2 字段 + 旧档 7 字段 ⇒ 只补缺、已有键不被覆盖、数组成对补、补完过闸、第二遍原样不动 |

`PilotProfileDto`（脚手架里的「切后 payload」）**逐字段照抄** `ProfileData.SaveDto`；
⑤ 那条**读源码**的漂移断言就是钉「两个 DTO 必须一字不差」的（两个程序集没法用反射互看）。

### 4.2 dump 逐字段对比（实际读数）

- **切前 dump**（真实 fixture，sha256 `a6331c8d5be107c8ba9600319a5bb4bd53f591e08aacb49438bc7e7ab00039aa`）：
  `ver=1 · playerName="" · avatarIndex=0 · titleIndex=0 · avatarBorderOn=true · achIds=[Ach_1,Ach_19,Ach_24,Ach_3,Ach_4,Ach_5] · achValues=[72,8,1128,2,2,1]`
- **切后中央档 payload**：`SaveJsonDiff.Diff(切前, 中央) == ""`（**数组顺序也一致** —— `_achProgress` 是按文件顺序填的 `Dictionary`，
  保存时按插入序写回；⛔ 别在这里改成排序，否则「切前 vs 切后」会假红）
- **切后旧档**：`Diff(切前, 旧档文件) == ""`，且**不含 `_v`**（旧客户端照旧能读）
- **合成档（真名字）**：`playerName="R10 玩家"` / `avatarBorderOn=false` / `avatarIndex=7` / `titleIndex=3` / 3 条成就 ——
  导入 → 写出 → 再读，三轮之后 7 个字段全在
- **老档**：`{"avatarIndex":3}` → `titleIndex=-1` / `avatarBorderOn=true` / `playerName=""` / 两个空数组（**不报错**），写回得 7 个键

### 4.3 变异矩阵（每条都**真的会红**；脚本 `D:/tmp/wf_n6_verify/mutate_run.py`）

| # | 改坏法 | 结果 |
|---|---|---|
| M1–M8 | （R4 留下的地基：缺 key / 坏 JSON / 未来版本 / Diff 失灵 / 双写失效 / 只看 mtime / 关着不写旧档 / 旧档带信封） | 全部红（2/16/9/15/10/1/2/30 条） |
| **M9** | `if (e.NoMirror) continue;` → `if (true) continue;`（**把双写登记整块关死**） | **FAIL 5** |
| M10 | 大小写折叠永远通过 | FAIL 1 |
| **M11** | `ProfileData.SaveDto` 把 `achValues` 改名 | **FAIL 2**（DTO 漂移断言） |
| **M12** | `BuildAchievementsPayload` 少写 `highestValue` | **FAIL 2** |
| **M13** | **复活地雷**：`ImportProfile` 改回只写 `{avatarIndex,titleIndex}` | **FAIL 17** |
| **M14** | `HasTopField` 只查 `ContainsKey`（非空数组一律当缺） | **FAIL 8** |
| **M15** | `MissingProfileFields` 永远返回 null（写口那道闸形同虚设） | **FAIL 5** |
| **M16** | 补缺迁移改成整份覆盖（违反「不覆盖已有中央档」） | **FAIL 1** |

> M9 的靶子**反过来**了：原来是「去掉跳过 ⇒ profile 被双写（当年会抹数据）」，现在 profile 补齐了字段、**本来就该登记**
> ⇒ 靶子是「把登记门关死」。旧靶子（M9 = 去掉 `CosmeticOnly` 跳过）已作废，`mutate_run.py` 里写了原因。

---

## 五、没查清 / 残留风险（如实）

1. **「补缺迁移」只在导入那一拍生效**。如果哪天出现「中央档字段不全、但旧档已经被删」的组合，补不回来（旧档我们一个都不删，所以正常不会发生）。
2. **`AchievementsData` 只写不读**。谁读它 = 将来的「成就子系统」；现在**没有任何代码读**，所以它如果和 `profile.json` 那一份不一致，
   玩家**看不到**（但进度不会丢）。不一致的唯一入口是「回滚旧客户端玩过一轮」—— 那种情况读口读的是旧档（最新），投影会旧一版。
3. **`highestValue` 我们用 `value` 顶**（§2.3）。要真复刻就得让 `profile.json` 多存一列或让 `AchievementsData` 成为读口，**那是下一步的决定**，本轮没做（会牵动 `ProfileData` 的全部读口）。
4. **`collectedRewards` / `premiumCollectedRewards` 恒 0**：原版记「已领几档」，我们的档位是现推的（`AchievementTierOf`）。要补需要「领取」这一层业务（Trophies 页现在没有领取动作）。
5. **判据文档有一句要改**（我没动它，避免和调度台/正本打架）：
   `资料/完全复刻/判据/存档体系_原版语义与施工方案.md` §六·1 的「`playerName` → 归 `NameChanges` 那条线」**不成立**（证据见 §2.2），
   同节「只迁 `avatarIndex`/`titleIndex`」也已过期。建议由调度台就地改。
6. **删除口**：`ProfileData` **没有任何删档路径**（全仓 `grep` 过：只有 `SavePathOverrideForTest` + 临时文件清理，`Editor/*` 里删的是测试临时档）
   ⇒ 简报那条「切了读口必须同步切删除口」在 profile 这一族**没有对象**。将来若加「重置档案」，必须**中央 + 旧档一起清**（`SaveCenter.Delete` + 删旧档），
   否则 `ReadThroughLegacy` 会从中央那份把「已删除」的数据读回来。
7. **`ver` 与 `_v` 两套版本号并存**：`_v`（`SaveCenter` 信封，管迁移/未来版本锁）与 `ver`（旧档内部记号，只是标记）。
   现在同一份 payload 里两个都有；`ver` 永远不会被 `SaveCenter` 读。**别拿 `ver` 当 schema 版本**。
8. **真机没验**（本代理按纪律不跑 Unity/不自检）。下面三件需要真 Play 或自检宿主来收口：
   ① 真 Play 里第一次 `SaveNow` 会不会在日志里出现双写登记/失败（`SaveThroughLegacy` 的路径改登记警告）；
   ② `MainMenuScene.cs:8307-8348` 那条「档案落盘/重读」自检（我只做了纸面推演：批处理下 `StoreOn=false` ⇒
      `SaveThroughLegacy` 只写 `SavePathOverrideForTest` 那一份，`ReadThroughLegacy` 也只读那一份 ⇒ 与切换前同）；
   ③ `ProfileDataAssertE13` / `RuleEngineTest_EngineX2` / `EngineX18` 三处同样只用 `SavePathOverrideForTest`。

---

## 六、顺手发现（报上来，由调度台分流；⛔ 我没顺手改）

1. 🔴 **成就结算会「一条一存盘」**：`ApplyAchievementTally` 对 102 条循环，每条 `GainAchievement → AddAchievementValue → SaveIfAuto → SaveNow`
   ⇒ 一局结算最多 **~20+ 次整档落盘**；本轮起每次落盘 = **中央 + 旧档 + `AchievementsData` 三次原子写**（放大约 1.5 倍）。
   建议：`ApplyAchievementTally` 收尾只落一次（把 `SaveIfAuto` 换成显式 `SaveNow`）。**属性能/寿命问题，不影响正确性。**
2. **原版有 4 个成就 key**：`AchievementsData`(10) / `AchievementsNewData`(11) / `AchievementsCurrentData`(12) / `Achievements`(46)。
   我们只用了第 1 个；另外三个的原版语义**没查到**（`PlayerDataKey` 里只有名字）。⛔ 别硬套。
3. **`SaveCenter.ReadThroughLegacy` 在「两边都没有」时会刷一条警告**（`SafeRead` 里 `File.ReadAllText` 抛异常 → `LogWarning`）。
   我在 `ProfileData.LoadFromDisk` 里用「先探存在性」绕开了；其它子系统（Daily/BattleLog/Ranked/Inventory）**没查**是否也在首跑时刷这条噪声。
4. **`Entries` 的 `How` 字段**只有报告里用；`profile` 那条我写的是「整份导入 + 成就投影」，与 `ImportProfile` 的两条 ✅ 行重复 —— 不影响功能。
5. **`LegacySaveReader.ProfileFields` 是「旧档该有的 7 个键」的唯一清单**，但它与 `ProfileData.SaveDto` 仍靠**断言**保持一致
   （跨程序集没法编译期互证）。本轮起有两道闸：脚手架读源码的漂移断言 + `SaveNow` 的写盘前检查。**新加字段时两处都要改**（注释里都写了）。

---

## 七、交接给下一个代理

**现在的状态**：C8 已完成，`profile.json` 这一族在双写期内（中央 `CosmeticData` + 旧档 `profile.json` + 投影 `AchievementsData`）。

**下一个人可以接着做的（按优先级）**：

1. **真机/自检收口**（本代理没跑 Unity）：按 §五·8 那三条验一遍；重点看**第一次真 Play 启动**的日志里
   `[SaveCenter] 已登记双写：'CosmeticData'` 与 `[LegacySaveReader] 双写登记完成：7 条` 是否出现且无 ERROR。
2. **`highestValue` 的真复刻**（§五·3）：决定是「`AchievementsData` 转正为读口」还是「`profile.json` 加一列」。
   ⚠️ 动了读口就要同步动删除口与 `NewerLegacyReport` 的判据（那边现在是按 `CosmeticData` 比的）。
3. **判据文档 §六·1 的更正**（§五·5）：`playerName` 的归属与「只迁两个字段」两句都要改。
   证据：`PlayerDataManager__SetPlayfabDisplayName.c`（PlayFab 玩家档案）+ `NameChangeRecord.cs`（改名历史）。
4. **成就结算的落盘合并**（§六·1）：`ApplyAchievementTally` 收尾只落一次。

**改这些文件的人注意**：
- 改 `ProfileData.SaveDto` 的**字段名/数量** ⇒ 必须同步改 `LegacySaveReader.ProfileFields` 与 `LegacyProfileFields`，
  否则 `SaveNow` 会**拒绝写盘并报错**（这是故意的：宁可报错，不许抹数据）。
- 改 `LegacySaveReader.ImportProfile` 的「整份转存」⇒ 必须同步 `LegacyImport()` / `C8Profile()` 里那几条「逐字相等 / 逐字段相等」的断言。
- 动 `RegisterMirrors` 的 `NoMirror` ⇒ 先读 §2.1（地雷的前提还在不在）。
