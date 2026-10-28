# B89 第 1 波 · R5b —— 安卓 1.35.0 APK 挖 `PressedBtn` + U9/U10 结案

> **消费者**：调度台 · `B89-R4b` · 后续 HUD/特效写手。
> **本文件回答三件**：① **APK 里到底有没有 `PressedBtn`/`ActiveWarlordAbility` 实例**（第一句见下）② U9（HUD 741）施工指令 ③ U10（507②）原版 AI 出牌那一拍的视图形态。
> **红线遵守**：⛔ 没改任何 `.cs` · 没跑 Unity/ruleprobe · 没动 git · **没改 `d:/2` 一个字节**（只 `zipfile.read`）· 没执行包里解出来的任何东西。解包落在全新空目录 `d:/4/_tmp_view/apk135/`；脚本写在 `d:/4/_tmp_view/`（与数据目录分开），一律 `python -I -X utf8` 跑。

---

## 🔴 第一句（结论）：APK 里【没有】`PressedBtn`，也【没有】`ActiveWarlordAbility` 组件实例

**安卓 1.35.0 的 XAPK 全量扫完：`ActiveWarlordAbility` 这个类存在于脚本表（pathID `2589`），但【没有任何 prefab / 场景 / bundle 把该组件挂到任何 GameObject 上】⇒ `PressedBtn`（它的 `GameObject` 字段）在任何地方都是**无来源**的，自然读不出 rect / sprite / 子对象。**

这与 PC 的 `assets_full`、AssetRipper 导出工程**结论一致**。⇒ **U1（HUD 684④ / 687）现在是「四级源全查过、全负」的 D**（见 §一）。

---

## §一 U1（HUD 684④ / 687）—— 四级源全查证，判 D

### 四级源（全部现查，不是按名字搜）
| 级 | 源 | 查法 | 结果 |
|---|---|---|---|
| 1 | 反编译 `.c` / ILSpy | `ActiveWarlordAbility.cs` = `public GameObject PressedBtn`（字段性质） | 定性：`PressedBtn` 是**组件字段**（指向子节点），非独立资源 |
| 2 | `assets_full`（PC） | 扫 `m_Script.m_PathID==2686` | **0**（曾有账按名字搜 = 证据不足，已改按脚本反查） |
| 3 | AssetRipper `ExportedProject` | 脚本 GUID `e7c2f59b1afee1b54de6696e2a5c4a07` 反查 prefab/scene | **0**（反证：`CardScript` GUID 命中 1 个 `CardPrefab.prefab` ⇒ 方法有效） |
| 4 | **安卓 1.35.0 APK** | 见下 | **0** |

### APK 怎么挖的（分级 + 数字）
- **XAPK**（`D:/2/关服前存档/安卓版本warpforge，包含远端资料/Warhammer+40,000_+Warpforge_1.35.0_APKPure.xapk`，813 MB）= 5 项：`com.Everguild.WarhammerWarpforge.apk`(30 MB) · `config.arm64_v8a.apk`(43 MB) · **`UnityDataAssetPack.apk`(739 MB)** · icon · manifest。
- **两级解** → `d:/4/_tmp_view/apk135/`（全新空目录）：
  - `UnityDataAssetPack.apk` 里 = **84 个 Addressables `.bundle`**（`assets/aa/Android/`，含 **13 个 `scenes_scenes_battlearena*`** 战场场景束 · `battleprefabs_vfxandmisc` · `menus` · 全阵营 card/cardanims · `Waprforge_monoscripts.bundle`）+ 87 个 `assets/bin/Data/*` 目录项。
  - 主 APK `com.Everguild…apk` = 常驻 Unity 数据：`assets/bin/Data/globalgamemanagers`(333 KB) · `globalgamemanagers.assets.split0..8`(8.65 MB) · `sharedassets0.assets.split0..21`(22.7 MB) · `level0`(57 KB) · `sharedassets0.resource`(5.8 MB) · `Managed/`。
- **脚本表（class 名反查基准）**：
  - 主游戏表 `globalgamemanagers.assets`（拆 split 拼回 8,654,992 B）→ **4,302 个 `MonoScript`**，其中 **`ActiveWarlordAbility` = pathID `2589`**。（同表还能读到 `WarlordMasteryContainer`/`DeckAndWarlordData`/`DraftModeWarlordSelectionState*` 等，证明表是完整脚本表。）
  - Addressables 表 `Waprforge_monoscripts.bundle` → **722 个 `MonoScript`**（pathID 是**大哈希**，与 PC 同值，如 `DraftModeWarlordSelectionStateDemo`=3488257000444584611）—— **`ActiveWarlordAbility` 不在这 722 里**（只有 3 个 "Warlord" 类，均非它）。
- **判据法**：UnityPy 逐 bundle 打开 → 对每个 `MonoBehaviour` **按原始字节解析 `m_Script` 的 `m_PathID`（偏移 20）/ `m_FileID`（偏移 16）**（Android 包 **typetree 被剥**，`obj.read()` 会 `ValueError`，故走裸字节；偏移由「解析出的 pathID 命中脚本表的比例」反证 —— 命中率≈100% 即偏移对）→ 用 `pathID → m_ClassName` 反查类名。⛔ **全程没有按名字/按字节串搜**（那样的 0 命中不作否定证据）。

### 全量扫描结果（`ActiveWarlordAbility` 命中 = 0）
| 容器 | MonoBehaviour 数 | 解析到脚本表 | 命中 `ActiveWarlordAbility` |
|---|---|---|---|
| `battleprefabs_vfxandmisc` | 2,847 | 2,847（→722 表） | **0** |
| `scenes_scenes_battlearena1` | 1,715 | 1,715 | **0** |
| 其余 12 个 `battlearena*` 场景束 | 各 1,694–1,736 | 全命中 | **0** |
| `menus_assets_all` | 35,014 | 35,013 | **0** |
| 其余 ~70 个 Addressables 束（card/cardanims/cosmetics/decks/sound/fonts…） | 合计数万 | 全命中 722 表 | **0** |
| `sharedassets0`（拼 22 split = 22.7 MB） | 188 | 187（→4302 表） | **0**（无 sc_pid==2589） |
| `level0` | 147 | 147（→4302 表） | **0**（无 sc_pid==2589） |
| `globalgamemanagers.assets` | 29 | 29 | **0**（无 sc_pid==2589） |

⇒ **`ActiveWarlordAbility` 类存在（2589），但零实例**；`PressedBtn` 的 rect / sprite 本地确实拿不到。**四级源全查过、全负** ⇒ 符合铁律 11 例外①「那批图本地确实不存在」。

### U1 状态建议（更新）
- **684 的 ④（PressedBtn 建层）/ 687 → 判 D**（证据由「按名字搜」升级为「**ILSpy/.c + assets_full + AssetRipper + 安卓 APK 四级按脚本/GUID/类名反查，全 0**」）。
- 684 的 ①②③（isPlayer 闸 / SetupActiveSkill / TryUsingActiveAbility 返回语义）维持 **A**；687 的时序（0.4 f 熄灭）**已实现**，缺的只是「那颗件本体」，故 687 整行判 D。
- `CardHighlight.cs` 的 `PressedBtnSetActive` 钩子口（`:989` / 常量 `:957` / Tick `:996`）**保留、不动**（D 是「无判据建件」，不是「删钩子」）。

---

## §二 U9（HUD 741）—— 施工指令（调度台已裁：**做**，不判 D）

### 判据（逐字，三源互证）
- 位置：`decomp_full/BattleManager__Update.c` **`case 1`（= normalBattle）** 段，LogWarning 在 **`:185`**（`CustomDebug__LogWarning(DAT_18427c2c8,0)`）。
- **文案（字面量）**：**`"Avoid activeSkillDesc.FadeOut because we are waiting for that action in tutorial"`** —— 出处 `cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:25696`（教程支）与 **`:25711`**（战役支，同一句）。
- **触发条件（原版逐句，`out_il2csharp/Assembly-CSharp/BattleManager.cs:25680-25714`）**：
  `case 1` 内 && `(targetCard == null || targetCard != cardSelected)` && `!waitingToResolveActiveAbilityFlag`（`+0x418 == 0`） && `IsPlayerTurn()` && **(`IsTutorialStageAvailable()` 或 `IsCampaignStageAvailable()`)** && `cardSelected != null` && **`aiScriptedmanager.CheckIfWaitingToUseAbility(this, GetCurrentTurnScriptedData(), cardSelected) == true`**。
- **谓词 `CheckIfWaitingToUseAbility`**（`out_il2csharp/Assembly-CSharp/AiScripted.cs:451-…`；`script.json:9076`）：`turnData != null` && `scriptedActions != null` && `actionCounter < Count` && 当前动作 `playerAction == true` && 当前动作 `scriptedActionData != null` && **∃ data[i].actionType == `ActiveAbility(50)`**，且按其 `actingUnitType` 取那一边的卡列表（PlayerWarlord/AiWarlord→`GetHero`；Player/EnemyMinion→`GetPlayerMinionsAndRemnantInPlay`/`GetEnemyMinions…`；Player/EnemyCardInHand→`GetFriendlyHandRef`/`GetEnemyHandRef`）经 `GetChosenCard(list, actingUnit)` 选出的那张 **== `cardSelected`** ⇒ true。
- **语义**（为什么有这条 log）：在教程/战役局玩家回合、玩家选了张卡、而脚本正等他用主动技时，原版**不把**主动技描述面板 `activeSkillDesc` **FadeOut**（避免教程指向物被收掉），并打这条 warning。

### 施工指令（落到我们这边）
1. **前置：补谓词**。`RuleEngine/Core/TutorialScript.cs` 目前**只有** `IsWaitingChangeAttack(turn, actionCounter)`（`:1904`，同族兄弟）与 `CheckIfWaitingToClickCard` 的等价支（`:1329` 一带），**没有** `CheckIfWaitingToUseAbility` 的等价物（现读 `grep IsWaiting.*Ability` 0 命中）。⇒ 先按上面「谓词」逐句转写一个新的
   `public static bool IsWaitingUseAbility(TutorialTurn turn, int actionCounter, int selectedSide, int selectedSlot)`
   （镜像 `IsWaitingChangeAttack` 的形参风格；`ActiveAbility(50)` = 我们的 `ScriptedActionType.ActiveAbility`；`GetChosenCard` 用我们的「按 actingUnitType + actingUnit 定位卡」等价口）。
2. **发日志的落点**：`CardPresentation/Battle/BattleDriver.cs` 的 `Update`（现读 `:14160`）**normalBattle 段里「玩家选中卡被换掉 / 取消选择 ⇒ 主动技描述面板该 FadeOut」** 那一处（面板类是 `activeSkillDesc` 的等价；我们这一侧的 `FadeOut` 调用点集中在「成功 / 关闭」路径，现读 `BattleDriver.cs:14825-14827` 那段注释说明「这一支不 FadeOut」—— 需在**真正 FadeOut 那一支之前**加闸）。命中上面条件时：
   `Debug.LogWarning("Avoid activeSkillDesc.FadeOut because we are waiting for that action in tutorial");`
   并把 FadeOut 一并**跳过**（原版这条 log 的实际作用就是「别 FadeOut」）。
3. **判据出处（写进代码注释）**：`BattleManager__Update.c:183-185` · 字面量 `out_il2csharp/Assembly-CSharp/BattleManager.cs:25696`/`:25711` · 谓词 `AiScripted.cs:451-…` · 兄弟方法 `TutorialScript.cs:1904`。
4. **状态建议**：`741` 由 C → **B**（可做；`IsWaitingUseAbility` 是新写谓词，需落断言）。

### 还差什么（U9）
- 我们 `TutorialScript` 的 `GetChosenCard` 等价口要先确认（`actingUnitType`→卡列表的取法）；这是本条唯一要新造的件。
- `IsTutorialStageAvailable()` / `IsCampaignStageAvailable()` 的等价 = `Ctx.Tutorial != null` / `Ctx.CampaignStage != null`（`BattleContext.cs:901` / `:922`）。
- 落点 `BattleDriver.cs` 属 E5/写手文件，本代理不改。

---

## §三 U10（特效 507②）—— 原版 AI 出牌那一拍的视图形态（已查出）

**答案：原版在 AI/自动打出那一路，卡**先是一张 2D 卡（`BattleCardUI` 2D 形态，不是 board 3D body）**，从打出位 `DOLocalMove` 飞到 conversion point，`OnComplete` 才播 `Card Hand To Board`（2D→3D 变形），之后才成为棋盘上的 3D 单位。**

### 逐句判据
- `cpp2il/out_cpp2il_dev_ilspy/MinionManager.cs:1203`：`TweenCallback action = card.CardToMinionAnim;` 挂在 `DOLocalMove(transform3, endValue2, minionToConversionPointTime).OnComplete(...)`（同方法 `:1191-1203`）。**先移动、再在到达时播 Invoke 动画**。
- `CardScript.cs:8387-8396 CardToMinionAnim()` = `CardUI__BackingField.PlayInvokeAnimation();` + `GetInvokeCue(ambush?1:rarity)` + `Play2D`。`PlayInvokeAnimation`（U3 已解）= `Animation.Play("Card Hand To Board")`（`speed=1`、`time=0`、`ToggleBody3D(true)`）——**这就是「2D 卡 → 3D 身体」那一下**。
- `CardScript.cs:8384`（`ChangeCardToMinion(doAnim:false)` 之前的 `SetSortingOrder(inHandPlaying)`）与 `BattleCardUI.cs:1435 ChangeCardToMinion` = `ToggleBody3D(true)`：**`body3D` 是「变形之后」才开的**。
- `CardToMinionAnimTeleport`（`CardScript.cs:8398+`，`isFromTeleport` 支）直接 `card2DController.gameObject.SetActive(false)` + `body3D.SetActive(true)` —— 传送不经动画，直接切 3D。反证「正常路是 2D 先动、动画后才 3D」。
- `GetMinionConversionTime(card) = GetHandToBoardAnimEventTime() + minionToConversionPointTime`（`MinionManager.cs:1222+`）—— 转换时长与 **Hand→Board 动画事件时刻**绑定，进一步证明那一下就是 Hand→Board clip。

### 对我们的含义（判定 U10）
- **原版 AI 那张卡在这一拍是 Hand(2D) 形态** ⇒ `PlayInvokeAnimation` 的 `"Card Hand To Board"` 能播。
- 我们 `CardFeel.HandToBoard` 开头 `if (card.Face == CardFace.Board) return null;`，而 AI/自动打出那条路**用 `SyncBoard` 直接把卡建成 Board 形态** ⇒ 播不了。
- ⇒ **507 能不能做，答案 = 能：让 AI/自动打出的卡在「打出→落地」这一拍也走 2D(Hand) 视图**（就像玩家部署那条路已有的 `StartHandToBoardClip` 0.3 s 那样：建一张 Hand 形态的卡飞到 conversion point，播 `"Card Hand To Board"`，再切 Board/3D），而不是 `SyncBoard` 直接建 Board。

### 还差什么（U10）
- 实现上真正要改的是**AI 落地的建视图路径**（今天 `SyncBoard` 直接 Board；要改成「先 Hand 2D 视图 → 播 clip → 转 Board/3D」），牵动 `SyncBoard` / 卡面 / 高亮三条链 ⇒ **仍需单独立账 + 用户/调度台裁定这套形态改动**（不是判据问题，是架构落点问题）。
- ⚠️ 玩家部署那条路**已经**在 `StartHandToBoardClip` 播过同一支 ⇒ 507 只补 AI/自动打出那一档，⛔ 别加两处（会播两遍）。
- 状态：507 整体维持 **C**（判据已齐，等形态改动裁定）。

---

## §C 没查清（⛔ 不许猜）
1. **`ActiveWarlordAbility` 实例到底在哪 / 是否真是死代码**：四级源都无实例。可能：① 本 build 里它是**死代码**（类留、无挂载）；② 只在**运行时 `AddComponent`**（但 `decomp_full` 与 `out_il2csharp` 全树未见 `AddComponent<ActiveWarlordAbility>`，`Start()` 只 `GetComponentInParent`）。**未证实是哪一种** ⇒ U1 判 D 的措辞用「本地确实不存在」，不用「原版本身没有」（行为是真的，只是本地无实例）。
2. **U9 的 `GetChosenCard` 等价口**：`TutorialScript` 里「按 `actingUnitType` + `actingUnit` 定位卡」的现成口名字待写手确认（`IsWaitingChangeAttack` 的兄弟能照抄形参风格）。
3. **U10 的 AI 落地视图改造**：属形态语义改动，需调度台/用户裁定后单独立账（判据已齐，缺的是「做不做这套架构改动」的决定）。

---

### 附：本文件用到的现读坐标
- APK：`D:/2/关服前存档/安卓版本warpforge，包含远端资料/Warhammer+40,000_+Warpforge_1.35.0_APKPure.xapk` → `UnityDataAssetPack.apk`(739 MB, 84 束) + `com.Everguild.WarhammerWarpforge.apk`(常驻数据)。
- 脚本表：`globalgamemanagers.assets.split0..8` → 4,302 MonoScript，`ActiveWarlordAbility = pathID 2589`；`Waprforge_monoscripts.bundle` → 722 MonoScript（**无**它）。
- 扫法：UnityPy 1.24.2，MonoBehaviour 裸字节 `m_Script.m_PathID @ 偏移 20`（typetree 被剥，`obj.read()` 抛 `ValueError`），pathID→脚本表反查类名。
- U9：`BattleManager__Update.c:183-185` · `out_il2csharp/Assembly-CSharp/BattleManager.cs:25680-25714` · `AiScripted.cs:451-…` · `TutorialScript.cs:1904`(兄弟)/`:1329`/`:751`。
- U10：`MinionManager.cs:1191-1203`(DOLocalMove→OnComplete CardToMinionAnim) · `CardScript.cs:8384-8410` · `BattleCardUI.cs:1435 ToggleBody3D(true)` · `CardScript__CardToMinionAnim.c` · `CardScript.cs:535 HAND_TO_BOARD_ANIMATION_CLIP_NAME="Card Hand To Board"`。
