# B9_C3T4i · 「文件名与内容不符」复核（第 8~15 例）

> 子代理 **C3T4**（查判据型 · 只读）· 2026-10-27
> 任务：逐条复核本会话新抓的「文件名与内容不符」第 8~15 例（⛔ 不照抄调度台表），并进 `G4_文件名与内容不符_全量.tsv`，补 3 列 + 把 **ICF 折叠** 与 **内容错位** 分开。
> 产出：`判据/G4_文件名与内容不符_全量.tsv`（更新版）· 本报告。

## 一、先说路径（铁律 6 提醒）

- G4 全量**实际现存于** `普查产出_第八十会话/G4_文件名与内容不符_全量.tsv`（4,130 行原始扫描，**不在 `判据/`**）。
- 本轮按你要的路径**新建**了 `判据/G4_文件名与内容不符_全量.tsv`（= 原 4,130 行 + 本轮补 1 行 = **4,131 数据行**，新增 **5 列**：`类别 / 体语义是否仍可用 / 正确产物该去哪找 / 首次发现者 / 备注`）。
- ⚠️ 现在**两份同源**（普查产出 原始扫描 · 判据 更新版）。建议你**保留 `判据/` 这份、把 `普查产出_第八十会话/` 那份删掉或改成指针**（删文件我不擅自动手，等你定）。

## 二、8 例逐条复核结论（**其中 2 条要打回**）

| 例 | 文件（**实际路径/名**，你表里有的写短了） | 内容首行函数 | 类别 | 体语义是否仍可用 | 正确产物该去哪找 |
|---|---|---|---|---|---|
| 8 | `CircularBuffer__GetBuffer.c` | `<>f__AnonymousType0<object>__get_itemId`（`return *(this+0x10)`） | **ICF折叠(疑似)** | 可能等价（两函数同码）；若 GetBuffer 就是返回 `+0x10` 的 buffer 则等价 | cpp2il `CircularBuffer.GetBuffer` 核字段偏移 |
| 9 | `PlayerHand__GetWhispersCardsCount.c` | `<>f__AnonymousType31__GetHashCode`（`return 0`） | **内容错位** | **不可用**（体是常量 `return 0`，不是计数） | cpp2il `PlayerHand.GetWhispersCardsCount` / VA |
| 10 | `AiScripted__ExecuteAction.c` | `AiScripted__ExecuteAction`（**首行=自己**） | 🔴 **同名重载覆盖**（不是文件名不符） | 体是**其中一个重载**（教程份），另一份（战役）无产物 | cpp2il 里按重载找另一份 |
| 11 | `AiScripted.__c__DisplayClass33_0__.ctor.c` | `<>f__AnonymousType31___ctor`（调 `NetworkingPeer__OnMessage`） | **内容错位** | **不可用**（anon 类型 ctor 体，非 DisplayClass 捕获初始化） | cpp2il `AiScripted.<>c__DisplayClass33_0..ctor` |
| **12** | `BattleManager__AddNextReplayAction.c` · `BattleManager__BasicBattleEndSequence.c` · `BattleManager__ResolveDisplayFromHand.c` · `BattleManager__ResolveDisappearUnit.c` | **首行都=自己**（`BattleManager__AddNextReplayAction` 等） | 🔴 **不成立（被夸大）** | 4 个**都是协程工厂**（`longlong ...()` 建状态机），**不是 anonymous-type 结构体拷贝** | 文件名可信、直接读；状态机真体在 `*_d__NNN__MoveNext.c` |
| 13 | `BattleManager__GetMainCamera.c` | `BasicCardUI__get_HealthCountersController`（`return *(this+0xa0)`） | **内容错位** | **不可用**（不是 Camera.main） | cpp2il `BattleManager.GetMainCamera` |
| 14 | `ActiveWarlordAbility__GetParentCard.c` | `FriendInfo__get_Room`（`return *(this+0x20)`） | **ICF折叠(疑似)** | 可能等价（同码 `return *(this+0x20)`）；若 GetParentCard 也返回 `+0x20` 引用则等价 | cpp2il 核字段偏移 |
| 15 | `Everguild.LiveOps.LiveOpsEventData__GetID.c`（你表写 `LiveEventData`，**少 `Ops`**） | `AuthenticationValues__get_AuthGetParameters`（`return *(this+0x18)`） | **ICF折叠** | **等价**（`+0x18` 就是 id 字段；多个 `*__GetID.c` 同码） | 可直接信 `+0x18`，或 cpp2il `LiveOpsEventData.GetID` |

### 复核结果汇总
- **真·内容错位（体不可用）×3**：例 9（`return 0`）、例 11（anon ctor）、例 13（返回 HealthCountersController）。→ 必须换 cpp2il/VA。
- **ICF 折叠（体可对）×3**：例 15（**等价**，坐实）、例 8 / 例 14（**疑似等价**，待 cpp2il 核字段偏移）。
- **同名重载覆盖 ×1**：例 10（文件名与内容一致，只是两个重载只导出一份）。
- **不成立/被夸大 ×1 组**：例 12（4 个文件全部文件名与内容一致，是协程工厂，不是 anon 拷贝）。

## 三、三条「要打回/要补」的差异说明（重点）

1. 🔴 **例 12 被夸大**：你表写「AddNextReplayAction / BasicBattleEndSequence / ResolveDisplayFromHand / ResolveDisappearUnit 均为 anonymous-type 结构体拷贝」——**不成立**。实际文件是 `BattleManager__<方法>.c`（带 `BattleManager__` 前缀），**首行就是自己**，体是协程工厂（`longlong BattleManager__Xxx(...)` 建并返回状态机）。它们**不在 4,130 的不符清单里**。真体在 `BattleManager._<方法>_d__NNN__MoveNext.c`。
   > 🔴🔴 **2026-10-27 就地订正（第三批回源）—— 本条判断【错】了**：`AddNextReplayAction` / `BasicBattleEndSequence` / `ResolveDisplayFromHand` 三份的**体确实是 `<>f__AnonymousType7<Int32Enum>___ctor` 造匿名类型并返回**（不是协程工厂）；**只有 `ResolveDisappearUnit` 是真体**。⇒ 调度台例 12 与 B9_E1:149 **本来就对**，别按本条打回。详见「第三批」§〇。
2. 🔴 **例 10 不是「文件名不符」**：`AiScripted__ExecuteAction.c` 首行=`AiScripted__ExecuteAction`（一致）。真正的问题是**同名重载（教程/战役）只导出一份**（你说「教程那份」），属「重载覆盖」，与「文件名 ≠ 内容」是两回事。已在报告标明，**未并入不符清单**。
3. 🔴 **例 11 是真不符，但 G4 4,130 扫描【漏收】了它**：`AiScripted.__c__DisplayClass33_0__.ctor.c` 内容确是 `<>f__AnonymousType31___ctor`，但原始扫描表里**没有这一行**（嵌套类型 `.ctor` 的解析缺口）。本轮已在 `判据/` 版**末尾补进这一行**（备注标「★G4 扫描漏收」）。

## 四、表更新说明

- `判据/G4_文件名与内容不符_全量.tsv`：4,130 行 + 本轮补 1 行（例 11）= **4,131 数据行**；新增 5 列；**已填 6 行**（例 8/9/11/13/14/15），其余 4,125 行新列留空（=「未判」，⛔ 不代表可用）。
- `ICF 折叠` 与 `内容错位` **在 `类别` 列分开写**（不再混）：
  - `ICF折叠` / `ICF折叠(疑似)` = 首行他名但**机器码相同**（体对）；
  - `内容错位` = 体是**另一个不同函数**的（体可能不可用）；
  - `同名重载覆盖` / `不成立(文件名匹配)` = 另两类（未列入 4,130 扫描，见 §三）。

## 五、建议（给你落）
给 `普查产出_第八十会话/G4_文件名与内容不符_全量扫描.md` 补两条：
1. **扫描有解析缺口**：嵌套类型构造器（如 `Type.__c__DisplayClassNN_0__.ctor.c`）会漏收——例 `AiScripted.__c__DisplayClass33_0__.ctor.c` 真不符但未进 4,130；建议补扫 `.ctor.c`。
2. **4,130 里只标了「名字 ≠ 内容」，没判「体到底可不可用」**：`类别`（ICF折叠=体对 / 内容错位=体可能错）需逐条判；本轮只判了 6 行，其余 4,125 行仍是「未判」。

## 六、还差什么（如实）
- 4,125 未判行的「体语义是否仍可用」**未逐条判**（本轮只判验证的 6 行；这是个大活，可按 G4 的 A/B/C 档分批派）。
- 例 8 / 例 14 的 ICF 是否**真等价**，需 cpp2il 核 `CircularBuffer.GetBuffer` / `ActiveWarlordAbility.GetParentCard` 的字段偏移（本轮未做）。
- 「首次发现者」一列本轮填「本会话代理汇总（C3T4 复核）」——**具体首报代理**散在各线报告（如 R350 `GetWhispersCardsCount`、B8_AT 悬案等），未逐条回溯。

---

## 第二批（2026-10-27 · C3T4i 续）：例 8/14 坐实 + 本会话已读 `.c` 的类别清单

### 1. 例 8 / 例 14 的 ICF 是否等价 —— **坐实「等价」**

| 例 | 文件 | 体 | dump.cs 字段证据 | 结论 |
|---|---|---|---|---|
| 8 | `CircularBuffer__GetBuffer.c` | `return *(undefined8 *)(param_1 + 0x10)` | `CircularBuffer.buffer` = `private float[] buffer; // 0x10`；`GetBuffer()` RVA `0x4B8D90`（非空桩） | **ICF 折叠 · 等价**（体正是 GetBuffer 返回 buffer） |
| 14 | `ActiveWarlordAbility__GetParentCard.c` | `return *(undefined8 *)(param_1 + 0x20)` | `ActiveWarlordAbility.parentCard` = `private CardScript parentCard; // 0x20`；`GetParentCard()` RVA `0x4C68A0`（非空桩） | **ICF 折叠 · 等价**（体正是 GetParentCard 返回 parentCard） |

⇒ `判据/G4` 里这两行已从「ICF折叠(疑似)/可能等价」升级为 **「ICF折叠 / 等价」**。

### 2. 本会话已读 `.c` 的类别清单（**只收「真读过体」的**，⛔ 不泛扫全 4,130）

> 「本会话」以 **C3T4 亲读体**的为准（其他代理的读数散在各自报告，见 §4 残余）。

**A. 正常（文件名 = 首行函数名，体可用=是）** —— 这些**不是**不符，**未进 G4**：
`BattleManager__ResolveRemoveEffect.c` · `BattleManager__ResolveAction.c`（case 0x10 内联体）· `BattleManagerSupport__BroadcastRemoveFromGame.c` · `CardScript__RemoveFromGameCard.c` · `CardScript__ReactToUnitJammed.c` · `BattleManager__RemoveSelfBuffsFromQueues.c` · `BattleManager__CancelEffectsFromDisappearingUnit.c` · `BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c` · `CardScript__RemoveEffect.c` · `CardScript__SendRemoveEffect.c` · `BattleManagerSupport__BroadcastRemoveEffectsFrom.c` · `BattleManager__ResolveRemoveEffectsFrom.c` · `CardScript__CheckMeetsCombination.c` · `CardScript__CheckPossibleCombinations.c` · `CardScript__CheckStun.c` · `CardScript__ContainsTrait.c` · `BattleManager._ResolveTransformIntoRemnant_d__554__MoveNext.c` · `ItemDrawer_object___Draw__1ae9cc0.c`（泛型产物，155 行 0 NoteDecompilerIssue）。证据 = 各报告已逐段引用其体。

**B. ICF 折叠（首行他名、**体对**，体可用=等价/疑似）→ 已并进 G4**：
`Everguild.LiveOps.LiveOpsEventData__GetID.c`（=AuthGetParameters `+0x18`）· `CircularBuffer__GetBuffer.c`（例8）· `ActiveWarlordAbility__GetParentCard.c`（例14）· `ItemDrawer_object___GetItemName__6e6e80.c`（=MissionsTab lambda，体=item.LocalizedName）· 6 个 `*Drawer__GetItemName.c` 折进 MissionsTab lambda（AllianceBadge/Cardback/ExpansionPassPremium/Premium/XSollaOffer）· `ForgePointIconDrawer__GetItemName.c`（=匿名 GetHashCode `return 0`）· `RandomCardIconDrawer/ TitleIconDrawer__GetItemName.c`（=get_LocalizedName `**(DAT+0xb8)`，疑似）· `RandomCardIconDrawer/TitleIconDrawer__Draw.c`（=NetworkingPeer.OnMessage 空体，疑似）· `AvatarDrawer/ExpansionPassPointDrawer/ForgePointDrawer__GetItemName.c`（分别折到 AvatarBorder/CampaignPoint 的 GetItemName，等价）。

**C. 内容错位（体是别的函数，体可用=不可用）→ 已在 G4**：
`PlayerHand__GetWhispersCardsCount.c`（例9，`return 0` 非计数）· `AiScripted.__c__DisplayClass33_0__.ctor.c`（例11，anon ctor）· `BattleManager__GetMainCamera.c`（例13，返回 HealthCountersController `+0xa0`）。

**D. 重载覆盖 / 不成立（非「文件名不符」）→ **不进 G4**，只在报告**：
`AiScripted__ExecuteAction.c`（例10，首行=自己，两个重载只导出一份）· `BattleManager__AddNextReplayAction / __BasicBattleEndSequence / __ResolveDisplayFromHand / __ResolveDisappearUnit.c`（例12，首行都=自己、协程工厂，被夸大成 anon 拷贝）。

### 3. G4 更新（`判据/G4_文件名与内容不符_全量.tsv`）
- 数据行 4,131 → **4,135**（`append` 4：`AvatarDrawer/ExpansionPassPointDrawer/ForgePointDrawer__GetItemName.c`（**method_only，原不在 4,130 扫描**）+ `ItemDrawer_object___GetItemName__6e6e80.c`（**泛型产物，不在 decomp_full 扫描**））。
- **填 10 行 ICF**（上面 §B 的 drawer 族）；**升级例 8/14 为等价**；共 **20 行**已判（含上一批 6 行）。
- ⚠️ 3 个 `GetItemName`（AvatarDrawer/ExpansionPassPoint/ForgePoint）**4,130 扫描里没有**——G4 归 `method_only`（类名不同、方法名同），不属「mismatch」，本轮补进；建议给 G4_全量扫描.md 的「类别」注记里补一句：**`method_only` 里的 `*Drawer__GetItemName` 是同码折叠，体可用**。

### 4. 还差什么（如实）
- 仍「疑似」的 4 条（RandomCardIcon/TitleIcon 的 `GetItemName` 静态配置名、`Draw` no-op）需 VA 反汇编确认。
- 「本会话各代理」只并入了 **C3T4 亲读体**的；其他代理（B8/B9 各批报告）里读过体的 `.c` **未逐条并入**（按同一格式可续补）——不泛扫全 4,130，避免把「没人读过体」的行也标上类别。
- G4 里其余 ~4,115 行仍「未判」（`类别` 空）：需按 A/B/C 档分批派代理读体后填。

---

## 第三批（2026-10-27 · C3T4i 续）：从 B9_*.md 各代理报告抽「读过体」的 `.c`，按 A/B/C/D 分类

> 范围：本会话 `资料/完全复刻/报告/B9_*.md`（108 份）里**明确写了「读了 `.c`/ILSpy 体、并给出体语义」**的 `.c`（grep `.c` + 文件名不符/ICF/首行/折叠/撞名/重载/anon 等词抽）。
> ⛔ 只并 **B/C/D** 进 `判据/G4`；A（正常）不进表（表是「名字≠内容」清单）。下面先放**两条订正**（含订正我自己上一批）。

### 〇、两条订正（先看）

1. 🔴 **订正我上一批（第二批）对「例 12」的打回 —— 我上批判错了**：我说「`AddNextReplayAction` 等 4 个是协程工厂、文件名可信、被夸大」；**回源核后 3 个我判错**：
   - `BattleManager__AddNextReplayAction.c` / `__BasicBattleEndSequence.c` / `__ResolveDisplayFromHand.c` 的**体确实是 `<>f__AnonymousType7<Int32Enum>___ctor(lVar,0,0)` 造匿名类型并 `return`**（存 `param_1` 到 `+0x20`），**不是本方法** —— 真体在各 `_d__NNN__MoveNext.c`（例：`_AddNextReplayAction_d__420` 143 行回放驱动器）。⇒ 类别 = **内容错位 · 不可用**。**只有 `__ResolveDisappearUnit.c` 是真体**（结构体拷贝 + 逻辑，正常）。
   - ⇒ 调度台例 12 与 **B9_E1:149 本来就对**（「只 new 一个匿名类型并返回」），是我上一批误判，**以回源为准、就地订正**。
   - 🔴 这还暴露 **G4 扫描的盲区**：「**首行名对、但体是 ICF 折叠的 anon ctor**」——G4 只比「文件名 vs 首行名」，两边都等于自己 ⇒ **抓不到**。本轮把 3 条按「内容错位(首行名对)」补进表并标 ★。
2. 🔴 **订正 B9_E4:91 的措辞**：它说 `TriggerAbility__OnReset.c` 首行是 `…_Register` —— **回源首行就是 `TriggerAbility__OnReset`**（全库没有 `TriggerAbility__OnRegister.c`）。真正错的是**体内 `Signal.Register` / `Signal.Unregister` 这两个 thunk 被 Ghidra 标成同一符号**（体级符号错，**不是文件名不符**）⇒ 以 ILSpy 为准。**不进 G4**。

### 一、逐条（方法 | 来源 `.c` | 体语义一句 | 报告来源 | 类别）

| # | 方法 | 来源 `.c` | 体语义（回源核） | 报告来源 | 类别 |
|---|---|---|---|---|---|
| 1 | BattleManager.AddNextReplayAction | `BattleManager__AddNextReplayAction.c` | 体 = `new <>f__AnonymousType7<Int32Enum>(0,0)` + 存 `+0x20` 返回（anon ctor，非本方法）；真体 `_d__420__MoveNext.c` | B9_E1:149（+调度台例12） | **C 内容错位·不可用** |
| 2 | BattleManager.BasicBattleEndSequence | `BattleManager__BasicBattleEndSequence.c` | 同上（anon ctor）；真体 `_d__391__MoveNext.c` | B9_E1:149 / 例12 | **C 内容错位·不可用** |
| 3 | BattleManager.ResolveDisplayFromHand | `BattleManager__ResolveDisplayFromHand.c` | 同上（anon ctor，`lVar13`）；真体 `_d__548__MoveNext.c` | B9_E1:149 / 例12 | **C 内容错位·不可用** |
| 4 | (对照) BattleManager.ResolveDisappearUnit | `BattleManager__ResolveDisappearUnit.c` | **真体**（BattleAction 结构拷贝 + 逻辑）；**不是 anon 拷贝** | 回源（订正例12「4 个都是」） | **A 正常**（不进表） |
| 5 | Missions.BlockAutoUpdates | `Missions__BlockAutoUpdates.c` | 体 = `VoiceLinesController.SetPopUpDisabled`（`*(this+0x78)=0`），不是 Missions 的 → 「按名字找调用点拿到假货」的根源 | B9_J5AU:11/115 | **C 内容错位·不可用** |
| 6 | Missions.EnableAutoUpdates | `Missions__EnableAutoUpdates.c` | 体 = `VoiceLinesController.SetPopUpEnabled`（`*(this+0x78)=1`） | B9_J5AU:11/115 | **C 内容错位·不可用** |
| 7 | CardDisplayLevelUpAnimationController.FadeOutImages | `CardDisplayLevelUpAnimationController__FadeOutImages.c` | 体首行=`...__OnEnable`，但 ILSpy 两方法体**逐字相同** ⇒ 同码折叠 | B9_J4FX:181 | **B ICF折叠·等价** |
| 8 | (SetTimer lambda) | `UIRankingListController___SetTimer_b__19_0.c` | 体=`YourTurnUI__FinishAnim` 本身（ICF）⇒ 能证 FinishAnim 的体，**不能当 SetTimer lambda 的调用证据** | B9_J12DK:175 | **B ICF折叠** |
| 9 | VoiceLinesPopupSelector.Toggle | `VoiceLinesPopupSelector__Toggle.c` | 体=`Card2DController.Toggle`（`SetActive`）；若本类 Toggle 也是 SetActive 则等价 | B9_B2AU:493 / J5AU | **B ICF折叠(疑似)** |

### 二、A 类（正常，未进表）

本会话大量 `.c` 是「读了体、用于判据、且**文件名=首行**」的正常件（不进 G4）。样例（第二批已列过一部分）：`ResolveRemoveEffect` / `ResolveAction` / `BroadcastRemoveFromGame` / `RemoveFromGameCard` / `ReactToUnitJammed` / `RemoveSelfBuffsFromQueues` / `CancelEffectsFromDisappearingUnit(FromQueue)` / `RemoveEffect` / `SendRemoveEffect` / `BroadcastRemoveEffectsFrom` / `ResolveRemoveEffectsFrom` / `CheckMeetsCombination` / `CheckPossibleCombinations` / `CheckStun` / `ContainsTrait` / `ItemDrawer_object___Draw__1ae9cc0`（155 行 0 issue）等。证据 = 各报告逐段引用其体、首行=自己。

### 三、D 类（重载覆盖 / 不成立，未进表）

- **例 10** `AiScripted__ExecuteAction.c`：首行=自己；**同名重载（教程/战役）只导出一份**（战役那份无产物）⇒ 重载覆盖（非文件名不符）。来源 B9_E3CMP:17/191。
- **`AbilityLogic.GetTargets` 4 参重载**：`.c` 产物是 **6 参重载**（文件头 6 形参），**4 参重载无 Ghidra 产物**（4 参体=纯转调，ILSpy 可读）⇒ 重载覆盖，别拿它当 4 参判据。来源 B9_J2WS:89。

### 四、G4 更新

- `判据/G4_文件名与内容不符_全量.tsv`：4,135 → **4,139 数据行**。
- **append 4**：3 条 anon-ctor 内容错位（AddNextReplayAction / BasicBattleEndSequence / ResolveDisplayFromHand，标 ★首行名对·体是 anon ctor·G4 盲区）+ `VoiceLinesPopupSelector__Toggle.c`（ICF 疑似）。
- **fill 4**（原 G4 扫描行）：`Missions__BlockAutoUpdates` / `Missions__EnableAutoUpdates`（内容错位·不可用）、`CardDisplayLevelUpAnimationController__FadeOutImages`（ICF 等价）、`UIRankingListController___SetTimer_b__19_0`（ICF）。
- 累计 **28 行已判**（第二批 20 + 本批 8）。

### 五、还差什么（如实）

- 本轮只抽了 **grep 命中「明确讨论 `.c` 体/文件名不符」** 的条目；B9 报告里**大量「读了体并用于判据、但没写『文件名不符』」的行**（那才是多数）**未逐条并入**——它们基本属 A（正常），按任务「只并 B/C/D」不进表。
- `VoiceLinesPopupSelector__Toggle` / `Missions×2` 属 **ICF（同码，体可能对）** 还是 **真错位**，仍「疑似」，需 cpp2il 逐体核（本轮只核了首行名与体形状）。
- `AbilityLogic.GetTargets` 4 参体在 ILSpy 可读（纯转调），**未并入**（重载覆盖，非文件名不符）。

---

## 第四批（2026-10-27 · C3T4i 续）：把「疑似」坐实（cpp2il + dump.cs 偏移）

### 结论：四组「疑似」**全部 = ICF 折叠 · 等价（体对）**，无一条真错位

| # | 方法 | `.c` 体 | ILSpy 同名方法体 / dump.cs 偏移 | 结论 |
|---|---|---|---|---|
| 1 | VoiceLinesPopupSelector.Toggle(bool) | =`Card2DController.Toggle`：`GetGameObject(); SetActive(option)` | ILSpy `VoiceLinesPopupSelector.Toggle`（`:570-574`）= `base.gameObject.SetActive(option)`（逐句同） | **ICF 折叠 · 等价** |
| 2 | Missions.BlockAutoUpdates() | `*(this+0x78)=0` | ILSpy `Missions.BlockAutoUpdates` = `AutoUploadProgress=false`；dump.cs `Missions.<AutoUploadProgress>k__BackingField // 0x78` **且** `VoiceLinesController.isPopUpShown // 0x78`（**同偏移**） | **ICF 折叠 · 等价** |
| 2' | Missions.EnableAutoUpdates() | `*(this+0x78)=1` | ILSpy = `AutoUploadProgress=true`；同 +0x78 | **ICF 折叠 · 等价** |
| 3 | RandomCardIconDrawer.GetItemName | `**(DAT_184557100+0xb8)` | ILSpy `GetItemName` = 读静态 `[184557100]+0xB8` 的 LocalizedName 并 `return`（Ghidra 标成 `get_LocalizedName` = 同码） | **ICF 折叠 · 等价**（读静态配置名） |
| 3' | TitleIconDrawer.GetItemName | 同上 | 同上 | **ICF 折叠 · 等价** |
| 4 | RandomCardIconDrawer.Draw(DropTableItem,int) | =`NetworkingPeer.OnMessage{return;}`（空） | ILSpy `protected override void Draw(DropTableItem item, int quantity=1) { }` = **空体** | **ICF 折叠 · 等价**（真实 Draw 就是 no-op） |
| 4' | TitleIconDrawer.Draw | 同上（空） | ILSpy 同 = 空体 | **ICF 折叠 · 等价** |

### 🔴 订正（我第三批判错）：Missions ×2 不是「内容错位·不可用」
第三批我把 `Missions__BlockAutoUpdates.c` / `Missions__EnableAutoUpdates.c` 记成「内容错位·不可用（体是 VoiceLinesController 的）」——**错**。回源：**Missions.AutoUploadProgress 也在 `+0x78`**，`Missions.BlockAutoUpdates()` 与 `VoiceLinesController.SetPopUpDisabled()` 的机器码**完全相同**（`*(this+0x78)=0`）⇒ **体对、ICF 折叠·等价**。
B9_J5AU 说的「按名字找调用点拿到假货」**只是「首行名是 VoiceLinesController ⇒ 用 Missions 名 grep 不到」**，**不是体错**。已在 G4 两行改为「ICF折叠·等价」并写「订正」。

### G4 更新
- 7 行升级为 **ICF折叠·等价**（Missions×2 订正 · VoiceLinesPopupSelector__Toggle · RandomCardIcon/TitleIcon 的 GetItemName / Draw）。
- 表仍 **4,139 行** / **28 行已判** / 10 列校验通过。

### 盲区一句话（可直接进正本）
> **G4 只比「文件名 vs 首行名」；「两边都=自己、但体是 ICF 折叠的 anon-ctor（或别人的体）」这一档抓不到** —— 例：`BattleManager__AddNextReplayAction.c` 首行就是自己，体却是 `<>f__AnonymousType7<Int32Enum>___ctor`。⇒ 判「体可不可用」不能只查 G4，要读体或对 cpp2il。

### 说明（诚实 · 一次写盘事故已恢复）
- 本轮更新 G4 时，我的生成脚本有一次 `io.open(P,'wb')` **漏了 `.encode()`**，`wb` 先把文件**截断成 0 字节**（正是 CLAUDE.md『wb 先截断再求值』那条坑）。已用 part1(从普查产出重建)→part2→part3→part4 全链**重建**，现 4,139 行 / 10 列 / 28 已判、校验通过。
- 教训再记：**`wb` 要先把内容算成 `bytes` 再 `open`，或先写临时文件再替换**；本次是「先算好 str、open(wb) 后 `.write(str)`」才炸的。

### 还差什么
- 这 7 条已坐实「等价」；其余 G4 里约 **4,111 行仍「未判」**（`类别` 空），需按 A/B/C 档分批读体（本轮只针对 7 组疑似）。
