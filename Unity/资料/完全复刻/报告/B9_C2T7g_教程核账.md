# B9 / C2T7g —— `教程_引导.tsv` B 行逐条核（93 条）

> 查判据型子代理（只读 + 落账补丁）。**只做核账**（判 A/B/落点失效/判据缺），**不动 `我们的落点` 列**（那头归 `R2T3B4` 的落点体检；靠方法名区分）。
> 产出：`判据/B行逐条核_教程.tsv`（8 列 · 93 行）· `账/_parts/B9_C2T7g.tsv`（11 列 · 93 行，**落点列原样不动**）· 本报告。

---

## 一、结论速览

- **93 条：建议 A = 9 · B = 84。**（无「落点失效」；落点均指向我们真实的 tutorial 框架文件。）
- **9 条升 A**：
  | # | 方法 | 等价依据 |
  |---|---|---|
  | `TutorialHidePointerInCampaign.Start()` / `.Update()` / `.OnDestroy()` | `MenuTutorialFlow.ApplyCampaignPointerFade`(:784) | `alpha = Clamp01(1 − Clamp01(norm × 12.5f))`，常量 **12.5** 逐字（差异原文也「一致」） |
  | `TutorialManager.DelayedAddPointerHideInCampaign()` | 同上 | 每次刷新按当前滚动算 alpha（等价实现） |
  | `TutorialStageClaimForgeFirstLevel.CheckForCompletion()` | `MenuTutorialFlow`（进阶段 `ForgeData.LevelOf` 快照 :335/:589） | 完成判据 = `ForgeData.LevelOf` 变化（我们锻造初值故意非零，靠变化判） |
  | `TutorialV2SaveData.LoadStage(FlowTutorialStage)` | `MenuTutorialFlow.IsCompleted(:72)` | 按 `stageId` 读完成位 |
  | `TutorialV2SaveData.LoadAllStages(FlowTutorialStage[])` | `IsCompleted(:72)` + `DoneKey(:66)` | 逐阶段 PlayerPrefs 读 |
  | `TutorialV2SaveData.SaveStage(FlowTutorialStage, bool)` | `MenuTutorialFlow.MarkCompleted(:79)` | 按 `stageId` 写完成位 |
  | `TutorialV2SaveData.SaveAllStages(FlowTutorialStage[])` | `MarkCompleted(:79)` | 逐阶段 PlayerPrefs 写 |

---

## 二、84 条 B 的成因（本域核心）

**🔑 教程线是「数据驱动的阶段表 + 通用流」，而原版是「每阶段一个 MonoBehaviour」—— 与 `个人资料` 域同型的架构差异。**

- **`TutorialStage*` 那批（约 31 条：`GetMission/GetCampaignNode/LevelUpCard/PlayRanked/SelectArmy/TutorialFinished/ClaimForgeFirstLevel` 的 `Execute / CheckForCompletion / DoMenuActions / WindowsChange / HasXxx / ShowCursor / Complete / DoingUpgrade ...`）**：原版**每个阶段一个类、每类一组方法**；我方把它们压成 **`Shell/MenuTutorialStageDef.cs` 的一张数据表**（`MenuTutorialStageDef` 带 `TitleKey/BodyKey/StageId/ClassName` 等）+ **`Shell/MenuTutorialFlow.cs` 的通用流**（`StartFlow/Advance/Tick/SkipCurrent/...` 按 stage def 驱动）。⇒ **没有同名方法可映射**（架构差异），需逐阶段比「该阶段的可见行为」是否被阶段表+流复现。**唯一能升 A 的一条已升**（ClaimForge 的 CheckForCompletion，因为它是 ForgeData.LevelOf 快照这一处具体等价）。
  - ⚠️ **`LevelUpCard` 阶段 3 条**（`DoingUpgrade/HasAlreadyUpgradedCard/HasNoCardToUpgrade`，差异原文写「本工程升级钮停用」）—— 这本是**休眠机制**（铁律 11：要做，只排先后）；但我方升级链现状是停用，方法级未定位 ⇒ **B**（不是 D）。
- **`TutorialManager` / `TutorialTipScript` / `TutorialPointer` / `TutorialModePopup` / `WildcardTutorialManager` / `FTUESelectDeckContainer` / `TutorialMessageWindowPopUp` 等（约 50 条）**：我方有对应框架类（`MenuTutorialFlow` / `Battle/TutorialOverlay`(SetTip/SetInitTip/SetPointer/SetSkip/Tick) / `TutorialPointerUi`(Show/Hide/SetText/SetFade) / `TutorialModePopup`(Open/Close/SelectStage/PlayTutorial) / `WildcardTutorial`(Start/OnFilterToggled/... ) / `FTUESelectDeckContainer`(Create/ToggleSelected) / `TutorialMessagePopup`(Open/Close/Build)），但**是 Builder/方法合并**（一个我方方法 often 承接原版多个回调）⇒ 方法级未逐句核 ⇒ **B**。
- `TutorialMessageData.Reset()`：→ `MenuTutorialStageDef` 的 `alreadyShown` 位（未定位到方法）→ B。

---

## 三、方法学 & 证据

- 读过：`Shell/MenuTutorialFlow.cs`（`DoneKey(:66)/IsCompleted(:72)/MarkCompleted(:79)/ApplyCampaignPointerFade(:784, 常量12.5)/ClaimForge 快照(:335,:589)` · `StartFlow/Advance/Tick/SkipCurrent`）· `Shell/MenuTutorialStageDef.cs`（数据表 `MenuTutorialStageDef` + `At(i)`）· `Shell/MenuTutorialManager.cs`（指针/目标解析）· `Shell/TutorialPointerUi.cs`（Show/Hide/SetText/SetFade）· `Shell/TutorialModePopup.cs`（Open/Close/SelectStage）· `Battle/TutorialOverlay.cs`（SetTip/SetInitTip/SetPointer/SetSkip/Tick）· `Shell/WildcardTutorial.cs` · `Shell/FTUESelectDeckContainer.cs` · `Shell/TutorialMessagePopup.cs`。
- A 判据：只对「找到我方真方法 + 逐字/注释等价」的 9 条升 A；其余（每阶段类、Builder 合并类）一律 B。
- **未读 `.c`**：本域判 B 的主要理由是**我方架构本身没有对应方法**（数据驱动），不是「原版判据读不到」；故未逐条读 `.c`（省时）。个别要看原版本体的（如 `ActiveAbility(40)` 桩），交战斗线 `T11`/`E3CMP` 已处理，不在本域。

---

## 四、产物 & 纪律

- `判据/B行逐条核_教程.tsv`：8 列 × **93 行**（A=9/B=84）。
- `账/_parts/B9_C2T7g.tsv`：11 列 × 93 行（表头逐字抄主表；**`状态` 列不动**；**`我们的落点` 列原样不动**（现验 0 改动，归 R2T3B4）；只改 `差异`（原差异后追加 `｜[C2T7g 2026-10-27] …`）/ `轮次`=`B9_C2T7g` / `备注` 前缀 `[C2T7g 2026-10-27]`，原备注保留）。
  - 校验：`merge_ledger_parts.py --dry --only B9_C2T7g` → **更新 93 · 新增 0 · 无变化 0 · 0 报错**；`check_patch_conflicts.py` → 无跨补丁重复键；纯 LF、无裸 CR、全 11 列；**`状态` 0 改动 / `落点` 0 改动**（脚本核过）。
- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。
