# B97_SMALL · 五个小域 12 条 B/C 逐条结案

> 执行代理 `B97_SMALL` · 2026-10-29 · **有写权限**
> 目标：清零 `账/轮抽.tsv`(6B) · `账/社交_聊天.tsv`(2B) · `账/教程_引导.tsv`(1B) · `账/锻造.tsv`(1B) · `账/其他_基础设施.tsv`(1B+1C) = **12 条**。
> 判据入口：`判据/B96_小域19_分诊.md`（承前）+ `判据/B96_商店轮抽个人32_分诊.md` §二（轮抽段）。
> **结果一句话**：**12 条一条不落全给了结论**；其中 **1 条真清零（`轮抽:14` B→A，判据闭合 + 三档已实现 + 断言）**，**1 条落点/等价实现就地订正（`其他:160`）**，
> **3 条纯「判据空/休眠」（`社交:15` / `教程:3` / `其他:160`）**，**7 条 REMOTE（服务端/远端数据）**——后两类**本轮零代码可做**，账补丁里已把「为什么做不到」的机器可核证据钉住。

---

## 〇、交付清单

| 产物 | 路径 | 状态 |
|---|---|---|
| 账补丁（11 列 / 首行表头 / 12 行） | `资料/完全复刻/账/_parts/B97_SMALL.tsv` | ✅ 已生成（48,867 B），`merge_ledger_parts.py --dry --parts D:/tmp/wf_small_parts` = **更新 12 · 新增 0 · 无变化 0** |
| 断言行 | `CardPresentation/Editor/B97_SMALL_Assert.cs`（+ `.meta`） | ✅ 新建；**未接线**（见 §三） |
| 新实现 | `CardPresentation/Shell/DraftWarlordTriage.cs`（+ `.meta`） | ✅ 新建（`轮抽:14` 的三档剔除） |
| 改码 1 行 | `CardPresentation/Shell/DraftModeWindow.cs:3487` | ✅ 谓词那一行（**行数零变化**，4481 行不变） |
| 秒级类型检查 | `TMPDIR=/tmp/wf_small bash 工具/typecheck.sh` | ✅ **运行时 0 / 编辑器 0** |

---

## 一、12 条逐条结论

### 轮抽（6）

#### `轮抽:6` `DraftEventBase.Expire(Action onComplete)` → **维持 B（REMOTE：缺触发源）**
- **现读判据**：方法体在本仓 `CardPresentation/Shell/DraftEventBase.cs:277`（**账里写的 `:257` 是文档注释行，已订正**），关窗 / `<5` 与 `==15` 两处阈值 / 没加入走基类 / `FinishRun` / 三处服务端件各自出声 —— **逐条在位**。
- **本轮新证据（这是它「为什么还是 B」的完整链）**：原版到期 = 服务端 → `LiveOpsHandler.ExpireEvent` → `ILiveOps.Expire`。本仓**这条链是通的**：
  `Shell/LiveOpsHandler.cs:487 ExpireEvent` → `:503 newEvent.Expire(handleOnComplete)`（非弹窗支）/ `:516 window.OnCloseWindow += () => newEvent.Expire(null)`（弹窗支）；
  排期闸 = `ScheduleEventPopups` 的 `EventState == Finished(3)`（`:373` 起）。
  ⇒ **缺的不是接线、是数据**：`EventState` 由 `EventData.GetState(now)` 按 `startDate/endDate/expirable/expireDate` 算（`Shell/LiveOpsEvent.cs:240`），
  而**轮抽事件本地根本没有实例**：`Resources/draft_packs.json` 只有 `PrebuiltPack`（现读 `count=882`，`endAt`/`endDate`/`expireDate` **三键全 0 命中**），
  本地 13 份活动 SO 目录（`Shell/LiveOpsLocalCatalog.cs` 头注）里也没有 draft。
- **做了什么**：零代码。**断言 C 段把这件钉住**（`draft_packs.json` 一旦出现到期时刻 ⇒ C2 红 ⇒ 提醒回来接 `Expire`）。
- **账补丁**：状态 B（不变），差异/备注追加**触发链逐跳**；落点订正 `:257 → :277`。
- **没查清**：无。

#### `轮抽:14` `DraftEventBase.GetPossibleWarlords()` → **B → A（本轮唯一真清零）**
- **现读判据**：真体在本地 —— `d:/2/tools/decomp_generic_trial/out_b94/DraftEventBase_object__object_.GetPossibleWarlords__18109d0.c`（251 行，全文现读）。
- 🔴 **B96_L3 说的「三个格子未解」全部解出**：
  1. `*(int *)(card + 0x4c) == 10` ⇒ `0x4c` = **`RawCardScript.inventoryOptions`**（`d:/2/tools/il2cpp_out/dump.cs:23252`），`10` = **`CardInventoryOptions.NotInInventory`**（同文件 `:45676` 起枚举）。语义 = 「不在库存里」的卡不能当候选。
  2. `(**(code **)(*card + 0x2a8))(card, *(longlong *)(*card + 0x2a8 + 8))` —— **既不是「槽 83」、也没有第二个实参**：
     IL2CPP 虚表项是 `VirtualInvokeData{ methodPtr; methodInfo; }` = **16 字节**（`d:/2/tools/il2cpp_out/il2cpp.h` 的 `struct Il2CppClass` 末尾 `VirtualInvokeData vtable[255]`），
     所以 `klass+0x2a8` = 方法指针、`klass+0x2b0` = **同一项的 `MethodInfo*`**（那个"实参"是 MethodInfo）。
     vtable 基址 = `sizeof(Il2CppClass_1)=0xB8` + `static_fields 8` + `rgctx_data 8` + `sizeof(Il2CppClass_2)=0x70` = **0x138**
     ⇒ 槽号 = `(0x2a8 − 0x138) / 16` = **23** = **`PlayerItem.IsHidden()`**（`dump.cs:18906` `Slot: 23`，无参虚 bool，形状吻合）。
  3. `RawCardScript.IsAlternateArt()` = `+0x88 currentAlternateArt != null`（`decomp_full/RawCardScript__IsAlternateArt.c`）。
- 🔴 **另一处订正**：「分支 1a 的实例谓词（`data + 0x1a8`，目标是 `this`）」**不成立** —— 那个 `+0x1a8` 落在 **`rgctx_data` 第 53 项**
  （`*(longlong *)(*(longlong *)(param_2 + 0x20) + 0xc0) + 0x1a8`，`param_2` = `MethodInfo*`，`+0x20` = `klass`，`+0xc0` = `rgctx_data`），装的是委托**方法指针**；
  委托 target = `**(klass+0xb8)`（`static_fields` 第 0 格 = `<>c.<>9`）。对应 lambda 的体已读：`_GetPossibleWarlords_b__20_0__eaa0b0.c` = `w => w.Reference`（**取值器**，不是谓词）。
- **做了什么（代码）**：
  - 新建 `CardPresentation/Shell/DraftWarlordTriage.cs` —— 三档逐条实现（`NotInInventory` 常量、`IsNotInInventory` / `IsHidden` / `IsAlternateArt` / `IsPossibleWarlord`），
    档 B（`FeatureConfig` 在远端）与档 C（`currentAlternateArt` 无写点）按 **🔶 自己设计** 口径标注。
  - `Shell/DraftModeWindow.cs:3487` 候选池那一行加谓词（**同一行内改条件 ⇒ 文件仍 4481 行，下游账落点零漂**）。
  - **本地剔除 0 张**（56 张 `hero` 的 `inventoryOptions` 全是 0；档 B/C 恒 false）⇒ 行为与改前一致，但口径与原版对齐。
- **断言**：`B97_SMALL_Assert.cs` A 段 6 条（字面量 10 / 5 / 56 + `==5` 不被误剔 + `null ⇒ false` + 56 张全过）+ B 段 3 条（`RollWarlords` 取满 5 张 / 同 seed 同结果 / 每张过谓词）。
- **残差（REMOTE）**：源选择三支（`specificWarlords +0xb8` / `specificArmies +0xc0` / `blacklistedWarlords +0xd0`）全在 LiveOps 事件数据（服务端）
  ⇒ 只走「两份名单都空」那一支 —— **这是原版在同样数据形态下的同一支，不是偏离**。
- ⚠️ **A 的边界**：A 只覆盖「本地可判的那一半」。若调度台认为「三支名单面未复刻 ⇒ 不能算 A」，退回 B 即可（**代码不用动**）。

#### `轮抽:24` `DraftEventBase.PickWarlord(...)` → **维持 B（REMOTE / P3）**
- **现读判据**：`.c` 真身仍只有一句云脚本 `PlayfabWrapper.GenericCloudScriptHandler(CloudHandle.PickDraftWarlord, { id = GetID(), warlord }, onComplete, onError)`；校验 / 写存档都在服务器侧。
- **玩家可见那半已等价且断言在位**：`Shell/DraftModeWindow.cs:3247 ChooseWarlord`（只认本局候选 → 存卡 → 可视高亮 → `Continue` 可点），判据在 `Editor/B94B10_Assert.cs` A 段（`eventData.<0x88> = warlord` 的本地等价 = `DraftRunSave.warlordId`）。
- **本轮**：零代码可做。账补丁追加复核结论。

#### `轮抽:30` `DraftEventBase.SetCustomMatchOptions(RoomOptions, int)` → **维持 B（计算已 A，无消费者）**
- **现读**：纯函数 `CardPresentation/Net/NetRuntime.cs:441 ComputeDraftMatchOptions` 在位（`C1/C4/C5` + 5 键 lobby 表 + 新手档 `ELO<=800 && won<3 ⇒ ELO=0`），断言在 `Editor/B94Y6_Assert.cs:170/176/180`。
- **本轮新证据（为什么不接）**：轮抽开战走**本地单机**（`Shell/DraftModeWindow.cs:3423 StartedBattle` → `PrebuiltDecks.SetPendingBattleDeck` → `ShellRuntime.LoadSceneWithFade`），**没有任何 P2P 房**；
  `CardPresentation/Net/` 全树 grep `Draft` 只命中那段注释和这个纯函数 ⇒ 要接消费者 = **先做「轮抽里开 P2P 房」这个功能本身**。
- **决策项 D3**（见 §二）：这一步属**服务器相关**（我们没有 Photon `RoomOptions`，自建 TCP 房参数是另一套）⇒ 允许自设计，但要调度台拍板。

#### `轮抽:114` / `轮抽:147`（Timed 付费页族 / Timed 选将页）→ **维持 B（REMOTE）**
- `114`：`Shell/DraftModeWindow.cs:4281 BuildTimedFamily`（gated `TimedDraftLocal.Enabled`，默认关）在位；双阵营条 `:4311` + `SetArmies` 两格阵营徽记在位；联盟计分 = 占位文本（排行榜 = 服务端）；事件图 = `TimedDraftLocal.EventImage`（本地 0）。**代码面零残差，残差全是服务端量**，且都已在 `TimedDraftLocal` 标 🔶。
- `147`：`specificArmies` 过滤（`:4299-4305`：`RemoveAll` 非两阵营 → `RebuildWarlordCards`）在位；**残差只剩标题幅面** `LiveOpsAssetUtility.GetLabel(eventData, Title)` ——
  词条在**远端事件数据的 `EventLabels`** 里，本地 13 份活动 SO 目录（4 Parent + 4 Scoring + 4 Premium + 1 Dark Zealots）**没有 draft** ⇒ 拿不到标题键。⛔ 不用我们自己起的键顶。

### 社交 / 聊天（2）

#### `社交:15` `AllianceInvitationEntry.OnDestroy()` → **维持 B（判据空 · 架构性不适用）**
- **现读 `.c` 全文**（`decomp_full/AllianceInvitationEntry__OnDestroy.c:7-15`）：只有两跳 `UnityEventBase.RemoveAllListeners(*(this+0x50)+0x100)` 与 `…(*(this+0x58)+0x100)`，任一 null ⇒ `FUN_1803f47a0()`（NRE 桩）。**不销毁、不置空、零可观察语义。**
- 我们的按钮是闭包、随节点销毁，**没有 `UnityEvent` 可摘** ⇒ 架构性不适用。按 2026-10-28 口径「架构性不适用 ⇒ B、⛔ 不判 D」⇒ **维持 B**，⛔ 别为它建 UnityEvent 壳。

#### `社交:187` `BattleAlliancePanel.Open(string,string,int,PlayerAvatar,CosmeticItemAvatarBorder,string,int,GroupBadge)` → **维持 B（REMOTE：联盟数据）**
- **代码面无残差（本轮再钉一次，因为账里还有旧句）**：
  面板侧 `Shell/AlliancePanelWindow.cs:884 Open(...)` 收 9 参、`:1081 Apply(...)` 真写 name/title/alliance/badge+tier/两个 score；
  **喂参侧** `CardPresentation/Battle/BattleDriver.cs:5988` 的 `_alliancePanel.Open(AlliancePanelName, "", "", null, 0, -1, -1, foeAvatarArt, null)`
  **已经把第 8 格头像与第 9 格边框喂进去了** —— ⚠️ **账里写的 `:5978` 现读漂到 `:5988`**（补丁已订正）。
- **残差**：⑤ 联盟名 / 联盟分 / 徽章（服务端，单机恒空串 / `-1`）⇒ 属 **P2**（联盟 UI 照原版、数据本地模拟 + 出声），⛔ **不是 D**。

### 教程 / 引导（1）

#### `教程:3` `BattleTutorialManager.Initialize()` → **维持 B（判据空 · 零行为）**
- **现读 `.c` 全文**（`decomp_full/BattleTutorialManager__Initialize.c:1-28`）：**只有两条字段写** + 两次字段写屏障（`FUN_1803f3730` ≈ `il2cpp_gc_wbarrier_set_field`）：
  `*(this+0x38) = *(*(<class>+0xb8) + 8)`、`*(this+0x40) = **(<class2>+0xb8)`（两个 `static_fields` 格 = 两个单例引用）。**零行为、零可观察语义。**
- 架构性不适用（我们的 `BattleDriver` 就是 BattleManager、`Ctx` 直接持有）⇒ **维持 B**，⛔ 不判 D，⛔ 别再指 `EnsureTutorialOverlay`（那是教程表现层，B92_V1 已推翻）。

### 锻造（1）

#### `锻造:23` `BoosterPackVisualItem.Initialize(string, BoosterPackOpenWindow)` → **维持 B（结构差 · 依赖不在我名下）**
- **现读判据**：`Instantiate(BoosterPackVisualItem prefab, boosterPackAnchor)` 出一件**独立世界物件**（`Renderer myRenderer` 3D 网格 + `TextMeshPro armyTexts[2]` **world 空间** + `AudioCue` + `OverwriteCameraShakePreset[]`），再调它的 `Initialize(null, this)`。
- 🔴 **缺口数量与族再订正一次**（现读 `d:/2/新解包资源/assets_full/bundle_boosterpacks_assets_all/`）：
  | 项 | 现读 |
  |---|---|
  | `Mesh/BoosterPack.obj` | **1 份**（3D 物件本体，**未导**） |
  | `Texture2D/40K_shop_offer_booster_*.png` | **16 张 / 工程已导 5**（`Resources/Art/ui_menu/`：Sautekh · Space_Wolves · UM · generic · leviathan）⇒ **11 张待导** |
  | 🔴 `Texture2D/WF_Booster Pack_*.png` | **17 张 / 工程 0 张** —— 这才是那件 3D 物件贴的族（17 个 `BoosterPackVisualItem` 实例一一对应）。**账里从没记过这一族** |
- ⇒ 真实待导 = **11 + 17 张图 + 1 个 mesh**（不是账里写的「11 张」）。② 3D 物件本体要拍「菜单层要不要真的摆一件世界物件」。
- **本轮为什么不动**：① 图归调度台/工具线（`工具/import_original_art.py` 红线外）；② 3D 物件是**新形态**（本工程菜单层是世界空间 quad + 渲染队列，从来没有 `MeshRenderer` 物件），且**我不能跑 Unity 验**，写了也无法自证 —— 按「结构差要拍的先报」交回。
- **决策项 D4**：菜单层做不做这件 3D 世界物件（现有等价物是「抖卡容器」代替相机抖，已在 `Shell/BoosterPackAnim.cs` 定口径）。
- **可粘贴方案（若调度台批）**：
  1. 工具线：`import_original_art.py` 加两族（`MENU_IMAGES` 的 `40K_shop_offer_booster_*` 剩 11 张）+ 新脚本导 `WF_Booster Pack_*` 17 张（`Resources/Art/boosterpack/`）；
  2. `BoosterPackExporter` 补 `Mesh/BoosterPack.obj` + 17 `Material` 到 `Assets/WarpforgeBooster/`；
  3. `Shell/BoosterPackOpenWindow.cs` 在 `Booster Animation Parent`（`:506` 建）下 `Instantiate` 3D 物件 + world-space TMP×2 + 按 `boosterName` 取 `Armies/<阵营>_short`，**拆包声挪到点卡包那一刻**。

### 其他 / 基础设施（2）

#### `其他:160` `Extensions.AlmostEquals(Quaternion,Quaternion,float)` → **维持 C（休眠）+ 落点/等价实现就地订正**
- **现读判据**：`out_cpp2il_dev_ilspy/Extensions.cs` 四个重载都在（`:56` Vector3 · `:76` Vector2 · `:96` **Quaternion** · `:136` float）；Quaternion 那支**原版 0 调用点**（全树只有 `NetworkingPeer` 自己的 private `AlmostEquals(object,object)`）。
- 🔴 **就地订正（铁律 5）**：账里那句「现读仍是『我们没有内联等价』/ 全仓 `Quaternion.Dot` 0 命中」**已过期** ——
  2026-10-29 `B95_A2B17` 把散在三处的量角器收口成 **`CardPresentation/Core/AngleMath.cs:51 AngleDegTiny(a,b)`**（运行时程序集、全仓唯一一份），
  算式 `θ = 2·atan2(|xyz|, |w|)`（相对四元数）≡ 原版 `2·acos(|dot|)·57.29578`；调用点 `Core/CardView.cs:9975` 写的是
  `AngleMath.AngleDegTiny(fr.rotation, transform.rotation) > 0.01f` ⇒ **这就是原版 `AlmostEquals(q1,q2,0.01f)` 取反**。
- **精确残差（记在案、不实现）**：原版在 `dot > 0.999999f`（夹角 < **0.16205695°**）时**短路返回 `maxAngle > 0`**；`AngleDegTiny` 不设 dot 阈值 ⇒ 同一区间返回真实小角
  ⇒ 两者只在「`0 < maxAngle < 0.162°`」这一窄带给出不同答案。⛔ **别**为它给 AngleMath 加回 dot 短路（那正是 B95_A2B17 特意去掉的 `Quaternion.Angle` 盲区）。
- **为什么仍维持 C**：原版那个封装**0 调用点** ⇒ 行为不可观测 ⇒ 休眠档（B94-R-L 2026-10-29 裁定）。⛔ 也别新建 Extensions 门面（那才是死代码）。

#### `其他:218` `GameModes.GetActiveEvent(Type)` → **维持 B（REMOTE）+ 🔴 新增一处口径偏离，交调度台裁**
- **现读**：`Shell/GameModesHandler.cs:263 GetActiveEvent(Type)`；`RankedEventInterfaceType`（`:433`）默认 `null` ⇒ 原版那条 `typeof(IRankedEvent)` 特殊支**整个不进入**；`GameModesConfigSource.MainRankedType` 默认 `null`。
- 🔴 **本轮新发现（此前没人写过）**：原版判据是 `if (config == null || config.mainRankedType == 0) goto LAB_18068658b;`（`decomp_full/GameModes__GetActiveEvent.c:44`），
  而 `LAB_18068658b` = `/* Subroutine does not return */ FUN_1803f47a0()`（`:106-108`）⇒ **原版在「拿不到 `GameModesConfig`」时是 throw，不是 skip**。
  本机 `assets_full` 全树 grep `rankedRanks` = **0 命中** ⇒ 我们恒处于 `config == null` 那一档。
  **严格按 §18·0「原版的 bug 也照抄」**，默认应当 `RankedEventInterfaceType = typeof(IRankedEvent)`（我们有那个接口，`Shell/RankedEventBase.cs:228`）然后**抛 NRE**；
  现口径（gate=null）是**朝「不崩」方向的偏离**。
- **本轮不改**：改了会让 `CurrentSeasonNumber` / 排位页崩，与「P2 服务端数据本地模拟 + ⛔ 不静默失败」冲突。
- **决策项 D5**（见 §二）。断言 `B97_SMALL_Assert.cs` D 段把两个默认值钉住。

---

## 二、要调度台拍板的 5 件（⛔ 我一件都没自己定）

| # | 事项 | 我的建议 | 不拍板的后果 |
|---|---|---|---|
| **D1** | `轮抽:14` 是否接受 **B→A**（判据闭合 + 本地三档已实现；三支名单面仍属远端） | **接受 A**，把「名单面」写在差异列当可见边界 | 若认为名单面未复刻就不能算 A ⇒ 改回 B，**代码不用动** |
| **D2** | `其他:218` 要不要**照 §18·0 打开 gate 并抛 NRE**（严格照抄原版） | **维持现状（gate=null）** —— 现口径只是「朝不崩的方向偏离」，改了会崩排位页 | 维持现状 ⇒ 那行永远 B |
| **D3** | 要不要做「**轮抽里开 P2P 房**」（`轮抽:30` 的消费者） | 先不做（服务器相关 ⇒ 允许自设计但要拍）；做了这一个功能才谈得上「接消费者」 | 不做 ⇒ `轮抽:30` 永远 B（计算已 A、无消费者） |
| **D4** | `锻造:23` 的 **3D 世界物件**做不做（含导 11+17 张图 + 1 mesh） | 图先导（便宜、无风险），3D 物件本体**先报**：菜单层从未有过 `MeshRenderer` 物件，且不能跑 Unity 自证 | 不做 ⇒ 那行永远 B；图不导 ⇒ 连后面都没法开始 |
| **D5** | `轮抽:6` 是否接受「B 是终态」（缺服务端到期事件 + 事件数据两样） | **接受**（用户 2026-10-24/25「不自己编到期时钟」） | —— |

> **`社交:15` / `教程:3`（判据空）· `社交:187` / `轮抽:24` / `轮抽:114` / `轮抽:147` / `其他:160`（REMOTE/休眠）本轮不需要拍板**：它们的结论已经是终态，
> 只等「服务器侧有没有一天能补上」。账补丁里已把每条「缺什么」写成可复查的坐标。

---

## 三、断言接线（**我没改 `Editor/RuleEngineTest.cs`**）

新建 `CardPresentation/Editor/B97_SMALL_Assert.cs`，入口 `B97_SMALL_Assert.Run()`，另有 `Pass` / `Fail`。

**建议插在 `CardPresentation/Editor/MainMenuScene.cs` 第 `18447` 行的合计行之前**（与上一批同一手法：**同一行内追加 ⇒ 行数零变化、下游账落点不漂**）：

```csharp
B97_SMALL_Assert.Run(); _sink.Pass += B97_SMALL_Assert.Pass; _sink.Fail += B97_SMALL_Assert.Fail;
```

共 **17 条**断言（A 段 8 · B 段 4 · C 段 5 · D 段 2，其中含 5 条硬前提）。灭自证对照写在文件头「🔴 灭自证」那一节。

> ⚠️ **诚实记一条**：B 段（`RollWarlords` 过三档）在当前数据下**守不住接线** —— 56 张 hero 全过三档，把那行谓词撤掉 B 段也不会红。
> 真正守这条的是 **A 段**（直接打谓词）。所以 A 段是主判据，B 段只证明「接线跑得通」。

---

## 四、顺手发现（不改代码也能改账 / 下一轮该知道的）

1. 🔴 **账里 4 处落点行号已漂**（本轮已在本补丁里订正 3 处）：
   `社交:187` `BattleDriver.cs:5978 → 5988` · `轮抽:6` `DraftEventBase.cs:257 → 277` · `社交:187` 面板 `:870 → 884`；
   另有 **`其他:160` 的「等价实现」整句已过期**（见 §一）。
2. 🔴 **`判据/B96_小域19_分诊.md` 有两处读数要就地订正**（我没改那份文件，因为它不是我的产物 —— 请调度台核）：
   ① 「虚槽 `+0x2a8` = 槽号 83、实参取对象 `+0x1580`」→ 实为 **槽 23 = `PlayerItem.IsHidden()`**、第二操作数是 **`MethodInfo*`**（推导见 §一 `轮抽:14`）；
   ② 「分支 1a 的实例谓词（`data + 0x1a8`，目标是 `this`）」→ 那个 `+0x1a8` 是 **`rgctx_data` 第 53 项**，委托 target 是 `<>c.<>9`，对应 lambda = `w => w.Reference`。
   同源的 `判据/B96_商店轮抽个人32_分诊.md` §二 `轮抽:14` 那一行的「三格未解」也随之作废。
3. **`LiveOpsHandler.ExpireEvent` 这条到期链是活着的**（`:487/503/516`）—— 下一个要接「任何活动过期」的人**不用再从头找**；只差事件数据里的 `endDate`。
4. **`Extensions`/`Quaternion.Angle` 的 0.162° 盲区**，以及我们已用 `AngleMath.AngleDegTiny` 绕开它 —— 这个事实账里此前只记了「Quaternion.Angle 用在 `CardView.cs:9975`」（已过期）。
5. **`bundle_boosterpacks_assets_all` 里其实有两族包面图**：`40K_shop_offer_booster_*`（16，商店货架用）+ `WF_Booster Pack_*`（17，3D 物件用）。账里只记了前一族 ⇒ 后一族**从来没被算进缺口**。

---

## 五、覆盖边界（本文件**没做**的）

- **没跑 Unity 批处理 / `run_8_checks.sh` / `ruleprobe` / `play check`**（红线：本代理不跑 Unity；用户口径「战斗 100% 之前不合并账补丁、不跑自检」）。
- **没合并账补丁**（只写 `_parts/B97_SMALL.tsv`；主表与 `_parts/` 归档归调度台）。`--dry` 已跑：**更新 12 · 新增 0 · 无变化 0**。
- **没改** `判据/B96_*.md`（不是我的产物，两处订正写在 §四-2，请调度台落）。
- **没验** `B97_SMALL_Assert.cs` 的运行时行为（只过了秒级类型检查）；接线后第一次跑请优先看 C2（它读盘上 JSON）。
- **没查透**：`轮抽:14` 的 `blacklistedWarlords(+0xd0)` 那一支在 `.c` 里的**具体取数顺序**（`while` 里 `Remove(item.Reference)`，但 `Reference` 为空时原版行为未逐字核）；本地无数据 ⇒ 不影响当前结论。
- **账的覆盖边界（六行版，权威 → `资料/完全审查与完全复刻_方案.md` §2.4）**：代码账分母 **13,182 行**；资源账 **247,612 个对象**；功能点索引未建；
  四类已知未覆盖 = ①「读过」≠「复刻过」 ② 724 条 / 165 类**无方法体** ③ 少数只能 **VA 级反汇编** ④ 已抓 **6 例文件名与内容不符**。数字现读、⛔ 别抄死。
