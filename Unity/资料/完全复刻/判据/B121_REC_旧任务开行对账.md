# B121-REC · 旧任务开行对账（只读代理 · 2026-11-03）

> 范围 = `活状态表.md` 四节 **23 行非 ✅ 行**：§B-10（7）· §B-13（3）· §B-8（1）· §D（5）· §E（7）。
> 方法：**每条现读当前代码 / 数据**（⛔ 不按账备注判 —— 备注是追加式的，后写的「✅」会压在旧句后面）。
> 判四档：`✅已做完待核销` · `🟠真没做` · `⛔判据本身错 / 前提不成立` · `❓查不到`。
> 参照：`报告/B120_REC_对账.md`（2026-11-02）已覆盖其中 12 条；本轮的增量 = **11-03 之后是否变化** + 另 11 条首核。

---

## 一、§B-10 真机验收（第一百一十七会话）

| ID | 原状态 | 判定 | 证据（现读） | 为什么 / 还差什么 | 建议 |
|---|---|---|---|---|---|
| **B117-R5** | ↪ 已并入 `D-01` | 🟠真没做 | `WarpforgeVFX/Runtime/GrabPassTransparentFeature.cs:101` `public bool redrawAt501 = false;` · `:184` `return _env501 == 2 ? redrawAt501 : _env501 == 1;` | 默认档仍关 ⇒ 那条路今天**根本没走**；`WF_GRAB501=0/1` 两跑 A/B 未做。与 `FX-01`／`D-01` 是**同一件活**，三处各挂。 | 保持开；**并成一条**（`D-01` 为唯一挂点），需真机两跑 + 同一局面比画面。 |
| **B117-R7** | ❓待复现 | 🟠真没做（**但已有更强候选根因**） | `Shell/LiveOpsEventWindow.cs:2398` `SetBattleInteractable(bool on)` · `Shell/SkirmishEventWindow.cs:792` `SetBattleInteractable(valid)`（= 原版 `OnDeckSelected` 首句 `battleButton.interactable = valid`）· `Shell/WindowButton.cs:545` `if (!_interactable) return;`（`OnPointerDown` 原版闸）· `:829`（`OnPointerClick` 同闸） | 🔴 **「按视觉位置点不中」有一个比「量错坐标」更硬的解释**：`interactable=false` 时 uGUI **压根不派发点击**，症状与「点不中」完全同形。复验时**先读 `BattleButtonWb.Interactable`**（`LiveOpsEventWindow.cs:2389` 已有只读口）再判命中区。 | 改判据：复验第一步 = 读 `Interactable` + `menu_rect` 量命中区（并集 + `m_RaycastPadding`）；⛔ 仍不排期成缺陷。 |
| **B117-R17** | 🔶 用户裁定 · 已改 | ✅**已做完待核销**（断言待 Unity 实跑） | `Shell/DailyData.cs:3124` `public static readonly bool AutoPopupEnabled = false;`（`:3113-3123` 三样写全的 `🔶 自己设计（非原版）`）· `:3130-3132` `IsAutoPopupAllowedFor(isPlaying, injected) => injected \|\| (AutoPopupEnabled && isPlaying)` · `:2402` `SchedulingAllowed` 接线 · `:2547` 飘窗共用闸已拆开 · 断言 `Editor/RewardsScene.cs:13739-13751`（5 条纯函数两档 + 反证注入口） | 用户裁定**已完整落地**（原版方法体一行未删 ⇒ 恢复 = 改一个字段）。唯一残留 = 14 条断言**没跑过**。 | 核销代码面；断言归 `RewardsScene.Run`（Unity 轮）。 |
| **B117-36** | ⚠️ 构建纪律 | 🟠真没做 | 代码侧**已进 HEAD**：`git status` 干净；`Board/DeploySequence.cs:334` `RotateEase = Ease.OutQuad;`（commit `26aefcff` 11-02 15:51）· `Core/CardFeel.cs:2090` 那条 `SetArtLocalY` 已删（commit `60c211b3` 11-03 00:31）。**包侧仍旧**：`_tmp_view/player/WarpforgePlayer_Data/Managed/Assembly-CSharp.dll` mtime = **2026-11-02 07:52** | 「只在工作树」这半**已过期**（已提交）；但**真机包仍早于两处修复** ⇒ 照旧必须重建。 | 保留（改成「代码已在 HEAD、包待重建」）；重建后本条自动可核销。 |
| **B117-42** | 📊 量化基线 | ⛔**判据本身错 · 前提不成立**（旧读数过期） | 三根因**全部已改**：① `Battle/BattleDriver.cs:19617` 同批多目标 `Hit` 并排起播 + `:40407` `HitBurstParallel`（`B117-37`）· ② `Core/EventTiming.cs:423` Trigger 前置 **0**（`B117-39`）· ③ `EventTiming.cs:950` 那处 `Death` 额外 0.1 已去（`B117-41`） | 账上「我们 4.0 s」是**改之前**量的，其三个根因都已被单独核销 ⇒ 这个 2.9 倍**不能再用作现状**。 | 改判为「待重测的度量项」：注明旧读数 4.0 s 过期、三根因已改；重测放真机轮（或按新常量静态重算）。 |
| **B117-R25** | ⚠️ 待真机复核 | 🟠真没做 | 无代码可判：G2 结论是**静态数值判据**，本来就要求渲图对帧 | 缺 = 真机截 `t≈0.31` / `t≈0.72` 两帧，判「有没有播、播的是不是那个」。 | 保留，归真机批量轮。 |
| **B117-R43** | 🟠未核销 | 🟠真没做（**三条闸不成立，真凶另在某处**） | `Core/CardView.cs:13493` `void BuildFaceCostText()`；三个提前返回 `:13496` `!TmpFont.Available` · `:13497` `!ShowsCost(Data)` · `:13498` `_faceMode == CardFace.Board`。`:2553` `ShowsCost = d.cost >= 0 && d.type != "hero"` ⇒ **第 3 行单位卡三条全过**。读口 `:13579 FaceCostText` · `:13581 FaceCostVisible` · 写入口 `:13550 SyncFaceCostText`（`if (_faceCostText == null) return;`） | ⇒ 「被三者之一挡了」的解释**对第 3 行单位卡不成立**（单位卡 cost>=0、非 hero、非场上）。剩余两个候选：**① TMP 建不出来**（`NewText` 回 null，`:13514-13520` 有出声分支）② **`B118-W2`/`B117-R73` 的「下一帧重盖」**（`:13557` `_enableRestampPending`）把数字盖住。 | 改为「待探针」：最小探针 = 读第 3 行任一格 `cv.FaceCostText`（null?）· `FaceCostVisible` · `_faceCostText.text`；截图前**停留 3 秒**。 |
| 汇总 | — | ✅1 · 🟠5 · ⛔1 | | | |

---

## 二、§B-13 正向查漏补录

| ID | 原状态 | 判定 | 证据（现读） | 为什么 / 还差什么 | 建议 |
|---|---|---|---|---|---|
| **D118-38** | ⬜ 待 Unity 验证 | 🟠真没做（**代码与断言都已具备**） | `Shell/LiveOpsHandler.cs:838` `LocalLiveOpsEvent : LiveOpsEvent<PlayEventSaveGame, LiveOpsEventData>` · `:973` `typeof(PlayEventSaveGame)`（`B118_W5` 订正注释在位）· 断言 `Editor/B118W5_Assert.cs` 已接 `Editor/ShellScene.cs:11214` 聚合行（`B118W5Assert.Run()`） | 唯一缺口 = **没跑** `ShellScene.Run` + `MainMenuScene.Run`（13 份本地活动存档 JSON 变宽的回归）。 | 保留，归 Unity 轮；**不用再派写手**。 |
| **D118-57** | ⬜ 待真机 | 🟠真没做（验收项） | `CardPresentation/Shaders/ArtOpaque.shader:68` `c.a = i.color.a;`（注释写明原版语义 = 贴图 alpha 不要、材质/顶点 alpha 要）。同族潜伏口 `Core/CardFeel.cs:3596 public static Tween UnDissolve(...)` —— 全仓搜 `CardFeel.UnDissolve(` / `UnDissolve(card` = **0 命中** ⇒ 确实无调用点 | 前提已具备，缺 = 真机逐条看「发牌淡入 / 阵亡消散 / 相关卡压暗」淡得对不对。 | 保留，真机批量轮；`UnDissolve` 的 0 调用点已复核成立。 |
| **D118-85** | 🟠 包络已改 | 🟠真没做（4 条里 1 条已落） | ② **已落**：`Core/CardFeel.cs:1739-1741`（decay / attack 两段改非线性，`B120_W4`）· 断言 `Editor/B120_W4_Assert.cs`（报告 `B120_W4_执行.md:240`：`u=0.5 ⇒ 0.25`，灭自证对线性）。① **仍开**：`Core/LayoutSpace.cs:78 cam.orthographic = true;`（原版 UI Camera 是透视 fov 40）⇒ HUD 相机整台没动 | ③ 开场运镜与震屏互踩、④ 清屏色与画质档数（2 vs 6）本轮**未取证**（不在 `CardFeel`/`LayoutSpace` 两处，需另找落点）。 | 拆条：② 核销；① 保留挂 `D-1`（等真机 A/B）；③④ 各留一小条。 |
| 汇总 | — | 🟠3 | | | |

---

## 三、§B-8 旧任务对账

| ID | 原状态 | 判定 | 证据（现读） | 为什么 / 还差什么 | 建议 |
|---|---|---|---|---|---|
| **FX-01** | ↪ 已并入 `D-01` | 🟠真没做 | `WarpforgeVFX/Runtime/GrabPassTransparentFeature.cs:101` `redrawAt501 = false`（同 `B117-R5`）；批 E（`CardView.SetLayer`）判据**已查清**：`Core/CardView.cs:2321 public void SetLayer(int layer)` + `:9509-9510` 注释（判据 = 原版无「挪层」活路径） | 机制与 233 件接线全在，**生产默认档下原版那一步没生效**；缺 = `WF_GRAB501=0/1` 两跑。 | 与 `B117-R5`／`D-01` **合成一条**（现在是三处各挂同一件活）。 |

---

## 四、§D 真机验收（挂起）

| ID | 原状态 | 判定 | 证据 | 为什么 / 还差什么 | 建议 |
|---|---|---|---|---|---|
| **D-01** | 未开始 | 🟠真没做 | `GrabPassTransparentFeature.cs:101` 仍 `false`；A/B 需要 Unity 两次起进程 | 用户 2026-10-31「代码层面全解决后再开」= 仍挂起；一旦开就是 13 场全局生效（排最末）。 | 保留挂起；**合并 `FX-01`/`B117-R5` 进来**，只留一个挂点。 |
| **D-02** | 未开始 | 🟠真没做 | `判据/非方法体审查_任务清单.md` AUD12 行仍 `⬜ 未开始`（2026-10-31 用户加排、须真机）；全仓 `grep AUD12 --include=*.cs` = **0 命中** ⇒ 无任何代码/断言 | 5 组（AUD12 全套 / AUD08 边框 activeSelf / AUD03 #62 / AUD05 J3·J5 / AUD10 走查）**全部要真机 dump 或真人走查**。 | 保留挂起。 |
| **D-03** | 未开始 | 🟠真没做 | 5 项（时钟图预乘 α / 随机卡叠序 / `C7` CJK fallback 真渲 / mixer 真听感 / `N135`）均为**观感或听感**判据 | 代码侧判不了（`mixer` 一项连离线度量都没有）。 | 保留挂起。 |
| **D-04** | 未开始（`A860 的续`） | ✅**已做完待核销**（指针行） | `资料/真Play待验清单.md:201` `\| **D58** \| 🆕 **【A860】菜单版卡牌详情窗那颗「眼睛」钮的实况行为** …`（注明「2026-11-02 由 `B120-REC` 补登记」），`grep -c A860` = **1** | 账原写「双向无主」的前提**已不成立** —— 清单侧已有可执行项。 | 核销本行、转成指针（与 `D-05` 同形）。 |
| **D-06** | 未开始 | 🟠真没做 | `任务线/自检断言线/任务总表.md:53-55` 三条状态仍 `？`（缺 Unity 探针：`CollectionScene.cs:10602` 前打 `ttTmp.text` / `characterCount` / `isActiveAndEnabled`） | 469 格同帧激活尖峰**未证**；「若证实要修」是条件项。 | 保留，随 Unity 轮（探针 + 真机逐页切）。 |
| 汇总 | — | ✅1 · 🟠4 | | | |

---

## 五、§E 旧任务与暂停线

| ID | 原状态 | 判定 | 证据（现读） | 为什么 / 还差什么 | 建议 |
|---|---|---|---|---|---|
| **E-03** | 🔴 仍 5 条 | 🟠真没做（**子项大批已落，需拆核销**） | 已落：`WarpforgeArena1/Editor/ArenaBuilder.cs:3727` 无专属 LUT 的 6 场回退共享 `LUT Normal`（`B114_R3`→`W-14`，2026-11-01）· `:807/:882` `EnsurePreviewFresh` 守卫（`B115_W11`）· `:2711` `maxParticleSize` 逐颗赋值 | 五条**本身是逐场对比/优化类** ⇒ 按用户 **2026-11-02** 新口径仍暂缓（与行内文案一致）。但行内的 `A1578`/`A1667`/`A1580②` **自述可核销**，却仍挤在同一行里。 | 拆条：把 4 个已转子项核销，只留 5 条对比；`T05` 改挂 `startColor`/`colorOverLifetime` 那批。 |
| **E-05** | ⏸ 判据在远端 | 🟠真没做（**本轮独立复核，结论不变**） | `d:/2/新解包资源/assets_full`（91 包含 `level0`/`resources`/`globalgamemanagers`）全树 `grep "guildbadge\|cantLoseRankText"` = **0**（⚠️ 该目录 JSON **带字段名**，已用 `bundle_aeldarisaimhanncardanims_assets_all/MonoBehaviour/AeldariRecall_2_ability.json` 验证过 ⇒ 这个 0 是有效否定证据）；`assetripper_out/ExportedProject` 全树 `BattleEndPlayerData` **只命中脚本桩** `Assets/Scripts/Assembly-CSharp/BattleEndPlayerData.cs`；类本体 11 个 `[SerializeField]`（`badge`/`guildbadge`/`nameText`/`rankText`/`subRankText`/`cantLoseRankText`/`clanText`/`troopsText`/`tacticsText`/`damageText`/`quoteText`） | 本地确实**零实例**（不是「没按脚本反查」那种假否定）。回来条件不变 = 哪只远端包补进来。 | 保留；把「已用两种独立方法复核（字段名 grep + AssetRipper 全树）」补进证据列。 |
| **E-06** | ⏸ 用户 2026-10-18「多人优化时再做」 | ⛔**判据本身错 · 前提不成立**（现状描述过期） | `Shell/TopBar.cs:285` `parts.ChallengeRoot = challenge.gameObject` · `:322` `Wire(challenge, ChallengeBox, true, ChallengePad, ChallengeClicked)` · `:434-441 ChallengeClicked()` → `ChallengeManager.OnChallengeButtonClick(_challengeRoot)`（= 原版 `ChallengeButton.OnButtonClick` 的本地那一半，`Shell/ChallengeManager.cs:329-351`；`ChallengeEngine.cs:82-110` 是 `Initialize/Toggle/OnSignalRaise/Awake` 模型半边）· 出厂 `SetActive(false)`（`TopBar.cs:137`） | 账上「现状 = 一直画 + 点了出声」**已不成立**：视图已建、命中区有 padding、点击走等价实现、出厂隐藏。真正卡的只有「显隐由服务端条件驱动」（`ChallengeButton.Initialize`）那一格 = 远端。 | 就地订正现状描述；用户暂缓口径保留（改成「仅剩服务端条件那一格」）。 |
| **E-07** | ⏸ 战场线暂停 | ⛔**判据本身错 · 前提不成立** | `WarpforgeArena1/Scenes/` 13 场 `.unity` 全在（含 `battlearenaastramilitarum.unity` 3.28 MB · `battlearenasororitas.unity` 7.19 MB，均 2026-11-01 重建）· `Resources/ArenaPrefabs/` 13 件 `.prefab` 全在（含 astra / sororitas） | ①「2 场建场」**已不是待办**（场景 + prefab 都在）；②「战场线暂停」这个理由**已被用户 2026-11-02 改掉**（功能/bug/资源/装配/接线照做，只暂停逐场对比）⇒ 这 6 条里的 **`T00·素材备用` / `T00·13 场引用修复` / `T06·残余` / `T09`** 属功能类，**现在就能做**；只有 `T01` 是对比类。 | 拆成「可做 4 条」+「暂缓 1 条（T01）」+「已做完 1 条（2 场建场）」。 |
| **E-08** | ⏸ 本线暂停 | 🟠真没做（**3 个子项里 2 个已处理**） | `Shell/MenuTutorialFlow.cs:335/:598/:604` 三阶段都有实现 · 缺 SO 的两阶段已按「空态 + 出声」落地并有断言（`Editor/MainMenuScene.cs:18230-18232` `EnterLevelUpCardForTest()` 断「缺服务端数据」）· **跳过键 `F9` 全仓 0 命中**（`grep "KeyCode.F9"` = 0） | 只剩：① `F9` 跳过键未接 UI ② 7 阶段主链未逐段走（真机 `-wfftue`）。`LevelUpCard`/`ClaimForge` 那条**已按口径处理完**，不再是缺口。 | 订正现状；其余保留（用户 2026-10-25 暂停本线）。 |
| **E-09** | ⏸ 本线暂停 | 🟠真没做 | `Editor/RewardsScene.cs:5957-5958` `CheckTrue(FindChild(camp, "Tutorial Message") == null, "…**不建**（`ToggleChooseArmyText` 全库 0 调用者 ⇒ 改由教程线做）")`；全仓无菜单侧 `Tutorial Message` / `TutorialHighlight` 建树代码（`Battle/TutorialOverlay.cs` 是**战斗侧**那一份） | 「整棵没建」**现读成立**（且有断言把它**钉成不建**）⇒ 建它时会与该断言冲突，要一起改。 | 保留；施工时先改 `RewardsScene.cs:5957` 那条反向断言。 |
| **E-10** | ⏸ 判据在远端 | ⛔**判据本身错（部分）** | `任务线/远端挂起线/任务总表.md` 现读：**R04** / **P07**（PlayFab 永久不可解）/ **P08**（无 mission 包）= 真·远端；**P03**（NAT-PMP）= **用户拍板不做**；**P04**（`Sunset Popup`）= **用户边界「过期活动不做」**；**P10**（`A1635`）= **用户 2026-10-22 已拍板去掉**（账原文） | 六条被同一句话「判据在远端」打包，但**三条是用户拍板不做、不是远端缺判据** ⇒ 混在一起会让它们永远等一个不会来的条件。 | 拆三档：远端 3 条（R04/P07/P08）· 用户拍板不做 3 条（P03/P04/P10）。 |
| 汇总 | — | 🟠4 · ⛔3 | | | |

---

## 六、汇总计数

| 四档 | 条数 | ID |
|---|---|---|
| ✅已做完待核销 | **2** | `B117-R17` · `D-04` |
| 🟠真没做 | **17** | `B117-R5` · `B117-R7` · `B117-36` · `B117-R25` · `B117-R43` · `D118-38` · `D118-57` · `D118-85` · `FX-01` · `D-01` · `D-02` · `D-03` · `D-06` · `E-03` · `E-05` · `E-08` · `E-09` |
| ⛔判据本身错 / 前提不成立 | **4** | `B117-42` · `E-06` · `E-07` · `E-10`（部分：六条里 P03/P04/P10 是用户拍板不做，不是远端） |
| ❓查不到 | **0** | — |

> 合计 2 + 17 + 4 = **23**，与本轮范围（§B-10 7 + §B-13 3 + §B-8 1 + §D 5 + §E 7）一致。
> `E-05` 归 🟠 一档，但本轮**另行取证**（两法复核「本地零实例」成立）⇒ 判据本身**没错**，只补证据口径。

---

## 七、顺手发现（可直接改账，**零代码改动**）

1. 🔴 **`B117-R7` 有了比「量错坐标」更硬的候选**：`interactable=false` 时 uGUI 不派发点击（`WindowButton.cs:545/829`），症状与「点不中」同形。复验第一步应读 `BattleButtonWb.Interactable`。
2. 🔴 **`B117-42` 的「我们 4.0 s / 2.9 倍」已过期**：它的三个根因（`B117-37`/`-39`/`-41`）全部已核销 ⇒ 别再拿这个数当现状去排活。
3. 🔴 **`B117-R43` 的「三个提前返回挡了它」对第 3 行单位卡不成立**（`ShowsCost` 对单位卡恒真、非场上）⇒ 真凶在 TMP 建不出来或 `_enableRestampPending` 重盖，按探针判。
4. ⚠️ **`E-06`/`E-07`/`E-08` 三条的「现状」描述都已过期**（视图已建 / 2 场已建 / 缺 SO 已按口径处理）—— 这是「按成本/状态分档决定做不做」的后遗症，三条都只是**外部条件**变了、不是活没干。
5. ⚠️ **`FX-01` / `B117-R5` / `D-01` 是同一件活**（`WF_GRAB501` A/B），账上挂了三处 ⇒ 合并后少两行噪声。
6. ⚠️ **`D-04` 与 `D-05` 同形**（都是「真机项已落到 `真Play待验清单.md`」）⇒ 后者已核销，前者应照办。
7. ⚠️ **`E-10` 把「用户拍板不做」混进了「判据在远端」** —— 会让 P03/P04/P10 永远等一个不会来的条件。
