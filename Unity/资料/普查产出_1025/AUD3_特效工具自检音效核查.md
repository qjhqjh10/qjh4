# AUD3 · 特效/工具文档/自检断言/音效 四线 §一 逐条现核（2026-10-25 · 只读核查代理）

> 口径：**代码里有实现 = 做了**；台账自陈不算。判据 = `grep` 到实现（`file:line`）或 `普查产出_*/` 报告段落。
> 今日读数（沿用简报 + 交接）：`AnimFXCheck` 209/0 · `CardFaceAssertE5` 61/0（含 `SoundCueAssertT` 11 条）· `BattleScene` 5292/0（合计 5345/0）· `CardBaseDemo` 433/0 · `RuleEngineTest` 8823/8823。
> ⛔ 未跑 Unity / typecheck；⛔ 未改任何 `.cs` / 台账 / 正本；⛔ 未碰 git 写操作。

## 一、特效线（`任务总表.md:14–86`）

| # | 台账条目（行号 + 一句话） | 现核结论 | 证据（file:line / 报告 §） | 建议处置 |
|---|---|---|---|---|
| 1 | `:23` 「⏳ 主对话同步点要跑 `AnimFXCheck` / `BattleScene` / `BattleVfxAssertE3`（一条都没执行）」 | **已做完（该核销）** | `AnimFXCheck` 209/0 · `BattleScene` 5292/0；`BattleVfxAssertE3.Run()` 由 `Editor/BattleScene.cs:13553` 调 ⇒ 同一趟已覆盖 | 删该 ⏳ 行 |
| 2 | `:22` 新账 `R8-b`（第④档仍播别 trait 的框） | **已做完（该核销）** | `Core/VfxMap.cs:2674` `IsTraitFrameRow` / `:2786` 挡行 / `:3639-3714` 断言；AUD2 #5 已独立核过 | 删 `R8-b` 行 |
| 3 | `:28` 新账（仍开）：「501 启用还差两步 · 500 的 RT 原版 ×0.25 · URP Dispose」 | **做了一半**：501 机制已实现但**默认关**；URP Dispose 已修；RT ×0.25 **无落点** | `WarpforgeVFX/Runtime/GrabPassTransparentFeature.cs:88-95/208`（501 重画，`[Tooltip]` 默认关）· `:116-121` `DisposeLists()` · `grep "0.25"` 该文件 **0 命中** ⇒ ×0.25 未做 | 保留；余项 = Renderer 清位 + 233 效果挪层（要 Unity A/B）+ RT ×0.25 |
| 4 | `:40` 「⛔ 仍未做：A1 的 24 条（`LUTBlender` · 屏震 rawSignal · `Card3DAnimationController` 整层）· A2 剩余导出」 | **已做完（该核销）**：点名三条**全部落了** | `LutBlendProbe` **R1 PASS**（`交接_第六十七会话.md:42`）· R9 rawSignal 已落地（同文件 `:23`）· `Battle/Card3DAnimationController.cs` 全文 + `FlyingCardController.cs:92-93` 已接 + `Editor/BattleScene.cs:12339-12360` 四组断言；A2 尾的 `Tutorial Pointer Menu`/`Card Display Open` 已导（`misc_clips` 11 条） | 整段改写为「A1 最该先做三条已落」；A2 尾只留卡包外观 17（见 #14） |
| 5 | `:47` **T02** 兜底路最差两条（`EffectIso` 逐材质槽 A/B） | **进行中** | `_tmp_view/iso_orig.log` / `iso_exp.log` mtime **09-25 09:14 仍在增长** | 不动，等 A/B 收数 |
| 6 | `:48` **T07** 粒子侧残余 `✓`（待渲染验收） | **未完成**（只剩渲染/真机） | 代码+生成器已改（台账自述，未复跑）；无渲染验收记录 | 留；归真机批 |
| 7 | `:49` **T17** Ruri 反编译 shader 对拍 `✓` | **做了一半**：三支已补，剩 Unity 渲染验 | `WarpforgeVFX/Shaders/WFUnlitAmbient.shader:104/141/263/382`（`_ENABLEMATCAP`/`_USEDISTORT`/`_ROTATEUVS` 全在） | 留；只剩 `_Flip` 与三支的渲染 A/B |
| 8 | `:50` **T11** 特效线真 Play `✗` | **未完成** | `_SOFT` 支/顶点色 VS 变体无真机记录；`资料/真Play待验清单.md` 无对应已完成标记 | 留；真机批 |
| 9 | `:51` **T12** 迁入批 4 笔 `✓`（Playables 需真机） | **未完成**（只剩真机） | 同台账；无真机记录 | 留；真机批 |
| 10 | `:52` **T13** 逐卡特效多候选/平票 `✓`（仅欠真 Play 观感） | **未完成**（只剩真 Play） | 台账自述改动已落；无真机观感记录 | 留；真机批 |
| 11 | `:54` **新账-69** E/C/D 四条 + `ShapeModule.m_Mesh` 导出缺陷 `◐` | **做了一半**：`ShieldTraitEffect_Idle Necron Variant` 可核销（E→Z）；`ShapeModule` 导出已补；`Raging Storm` 在跑 | `WarpforgeArena1/Editor/EffectExporter.cs:2009` 三个 ShapeModule 分支 · `EffectExporter.cs:880` 的 `RunListed` 名单里有该件 · iso 日志在写 | `ShieldTraitEffect_Idle Necron Variant` 单条核销；其余等 A/B |
| 12 | `:55` **W64·兜底审计** `◐`「只剩 B14」 | **做了一半**：B14 = 「501 重画」机制已落、默认关，与 #3 同一件 | `GrabPassTransparentFeature.cs:88-95/208/353-370`（§开关与前置写明两个前置未做） | 与 #3 合并为一条账 |
| 13 | `:57` **W65·A1** 自造/兜底残留 `◐` | **做了一半**：`R1`/`R9`/`R25`/`R28` 已落；**仍开** = `R11/R12/R13/R14`（`CardFeel` 族要并排渲图）+ `R18` | `UnitTweens.json` 74 条**只按 `name` 键**（`python` 现读首条无 `pathId`）⇒ PathID→名字那一跳仍缺；`Core/CardFeel.cs` 无 `R11–R14` 落地痕 | 保留；把 `R1`/`LutBlendProbe` 从「还差」删掉（已 PASS） |
| 14 | `:58` **W65·A2** 未导出/未使用 `◐`（只剩两条 Unity 命令） | **做了一半**：`Wildcard_Use_*` 8 件在、`TurnTimerTickPulse` 在；**卡包外观 17 盘上 0 个** | `WarpforgeVFX/Prefabs/TurnTimerTickPulse.prefab` + `Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset:78643` · `find -name "Booster Pack Standard*.prefab"` = **0** · `BoosterPackExporter.cs:124-146` `WindowRoots` 19 条已在 | 留；余项 = 主对话跑 `BoosterPackExporter.Run`（`EffectExporter.RunListed` 的那几件已可见） |
| 15 | `:60` **UI·音波线** `✓ 已修待验收` | **做了一半**：shader/材质/driver/探针全在；`VoiceWaveDriver` **仍未被 `UnitChatPanel` 用** | `Core/VoiceWaveDriver.cs:1-52` · `Editor/VoiceWaveProbe.cs:63-82` · `grep VoiceWaveDriver` 只命中探针+自身+shader 注释 | 留；把「旧 `Advance` 等价」写成 ✅ 例外② 或补接线 |
| 16 | `:62` **新账-72** 633 条升权威后的运行时核 `◐`（剩 ①②⑥） | **⑥ 已做完（该核销）**；①② **未完成** | `Core/VfxMap.cs:995` `LoadForTest(json, allowNonAuthoritativeInferredTie)` 已加 + `Editor/BattleScene.cs:39533/39542` 两档在用（台账 `:62` 写「2026-10-24 仍未加」= 过期） | ⑥ 划掉；①② 留真机 |
| 17 | `:65` **Trait 权威化·静音桶复核** `◐`「仍开：4 个 unbacked 桶继续挖」 | **未完成** | 无后续报告；`AM30/EC29/SAU3/a84a…` 四条仍无卡 SO 引用链证据 | 留 |
| 18 | `:67` **新账-86** `penitence` `✓ 已修待验收` | **未完成**（只剩真机看那一拍） | `VfxMap` 归一 + 6 条断言已落、`AnimFXCheck` 132/0 已付（台账自述，未复跑） | 留；真机批 |
| 19 | `:68` `DisplayStatusAnim` 重复施加重播 `✓` | **未完成**（只剩真机） | `EvtKind.StatusApplied` + `BattleScene.cs:9219` 夹具（台账自述） | 留；真机批 |
| 20 | `:69` **trait 阵营取「拥有者」** `✓`「只剩跑 `BattleScene.Run`」 | **已做完（该核销）** | `Battle/BattleDriver.cs:8728` `VfxFactionOf` + **13 处**调用（`:3678/18560/21132/21406/21443/21464/21486/21756/21936/22265/22657/23686/24360`）· 断言 `Editor/BattleScene.cs:39948-39985`；今日 `BattleScene` 5292/0 | 搬 §二 |
| 21 | `:72` **A31** 9 个待接线 prefab `◐`「只剩 `CardDetailPopup.OnCraftClicked` 的 8 件」 | **已做完（该核销）** | `Shell/CardDetailPopup.cs:2129-2156` `PlayCraftVfx` 两件 `WarpforgeUiEffect.PlayWildcardCraft`（`WarpforgeVFX/Runtime/WarpforgeUiEffect.cs:137`） | 搬 §二 |
| 22 | `:73` **虫群合并的音缺件** `✓ 已修待验收` | **做了一半**：cue 数据+播放+断言已落；**4 条如实偏离没做** | `Battle/BattleDriver.cs:19822` 播 cue；`:23105-23112` 逐条写明 ②混音组 ③3D 曲线 ④`ToggleBody3D(false)`/`ConfigureMaterial`「**没做**」（`grep ToggleBody3D` 只有注释，无实现） | 「音缺件」可核销；**④ 另立一行**（铁律 11 要做） |
| 23 | `:74` **`CardView.SetSwarm(true)` 游戏内 0 调用点** `✓ 已修待验收` | **未完成**（只剩真机看图标亮起） | `BattleDriver.cs:19815` 已接（台账自述）+ `BattleScene.cs:37170` 断言 | 留；真机批 |
| 24 | `:75` **锻造按钮完整链路** `✗ 未做` | **已做完（该核销）** —— 台账整行过期 | `Shell/CardDetailPopup.cs:1979` `OnCraftClicked` → `:1993` `CraftNow` → `:2096` `TryCraft` → `:2129` `PlayCraftVfx`（含传奇闸 `:2004`）；断言 C 组 `Editor/CardFaceAssertE5.cs:250-360`；报告 `普查产出_1025/D_界面线_执行报告.md:83` **「链路本身在 HEAD 里已经完整（不是空实现）」** | 搬 §二（`CardFaceAssertE5` 61/0 今日已跑） |
| 25 | `:83-84` 「2026-10-24 本轮新发现」表 | **空表无行** | 表头下 0 行 | 删该空表 |

## 二、工具·文档线（`任务总表.md:11–33`）

| # | 台账条目（行号 + 一句话） | 现核结论 | 证据（file:line / 报告 §） | 建议处置 |
|---|---|---|---|---|
| 26 | `:30` **T05** 仪器缺陷与临时产物 `◐` | **口径未决（非实现缺口）**：P0–P5 加头已落、12 条死链已做；余项 = P5 逐目录 README（调度口径不做）+ 清理（用户口径不清） | 台账自述 + `普查产出_1024/A4_工具数据四件.md` §四；AUD `:131` 同结论 | **需主对话裁定**：写成铁律 11 例外② 并核销，或补做 P5 |
| 27 | `:31` 黑名单内 65 条死链引用 `📌` | **不是待办**（常驻口径） | `普查产出_1023e/M1_文档死链.md` §四 · `M2_文档死链二批.md` | 保持；建议挪 `准则与做法.md` |

## 三、自检断言线（`任务总表.md:11–53`）

| # | 台账条目（行号 + 一句话） | 现核结论 | 证据（file:line / 报告 §） | 建议处置 |
|---|---|---|---|---|
| 28 | `:36` **T05** 新断言首跑即红 `◐`（`A1905` 后半判据不足） | **未完成** | `grep -rn "A1905" Assets` = **0 处**（只有 `资料/` 记载）⇒ 无代码承载，需一次 UM/SAU 阵营局真跑 | 留；归真机批 |
| 29 | `:49` **T03·`B6` 夹具结构脆** `✓ 已修待验收` | **已做完（该核销）** | `Editor/BattleScene.cs` 生产同口 `ResolveAllTimed`（代理 R 落地）；**`BattleScene.Run` 5292/0 今日已跑**（台账 `:49` 自己记了） | 搬 §二 |
| 30 | `:50` **T07·`Badges` 告警集与观测集混用** `✓` | **已做完（该核销）** | `Core/Badges.cs:431-504` 拆两集 + `UnknownIconWarnCountForTest` · `Editor/BadgeFilterAssert.cs:178-243` 六条；`BattleScene` 5292/0 | 搬 §二 |
| 31 | `:51` **T08·`A462` 抬起沿源级断言** `✓` | **已做完（该核销）** | `Editor/BattleScene.cs` 对称源级断言（白名单 7 处，代理 R）；`BattleScene` 5292/0 | 搬 §二 |

## 四、音效线（`任务总表.md:12–49`）

| # | 台账条目（行号 + 一句话） | 现核结论 | 证据（file:line / 报告 §） | 建议处置 |
|---|---|---|---|---|
| 32 | `:14-19` 代理 T `CardMove` cue（战斗侧 + `CardDetailPopup`） | **做了一半**：数据/调用/断言全落；**欠真机听感** | `Battle/CardDisplayWindow.cs:249` `SwapSoundCue="CardMove"` · `:1586-1650` 消费点 · `Editor/SoundCueAssertT.cs:31` · `CardFaceAssertE5.cs:74` | 留「真机听感」一项 |
| 33 | `:20` 新账（真缺陷）「`SoundCueAssertT` 全仓无宿主 ⇒ 11 条永远跑不到」 | **已做完（该核销）** | `Editor/CardFaceAssertE5.cs:74/528-532` `RunSoundCueAssertT()` 已接；`SoundCueAssertT.cs:155-189` 还带 **T-9 元断言**（扫外部宿主，防再退化）；`CardFaceAssertE5` 61/0 今日实跑 | 删该新账 |
| 34 | `:26` 新账「85 条 `_vs_` 对不上 `card_chat_intro`」 | **判据空（例外①）** | 台账 `:26` 自证：`customIntroChatData` 只有 57 张 SO、池内 49 张**一张没漏** ⇒ 未命中 85 条是**原版数据本身没有** | 明确写「例外①」并核销 |
| 35 | `:30` **S04·真机听感** `✓` | **未完成**（唯一阻塞 = 真机） | 实现侧核销（cue 460/460 · 516 音频 meta 全配对）；`真Play待验清单.md` 无对应条目 | 留；真机批（并补清单条目） |

## 应核销清单

1. **特效 `:23`** 同步点三条自检（`AnimFXCheck` 209/0 · `BattleScene` 5292/0 已付）。
2. **特效 `:22`** `R8-b`（`VfxMap.cs:2674/2786`）。
3. **特效 `:40`** 「A1 最该先做三条 / A2 剩余导出」整段（`LutBlendProbe` PASS · R9 落地 · `Card3DAnimationController` 整层）。
4. **特效 `:54`** 单条 `ShieldTraitEffect_Idle Necron Variant`（E→Z）。
5. **特效 `:62` ⑥** `VfxMap.LoadForTest` 已加。
6. **特效 `:69`** `trait 阵营取拥有者`（`VfxFactionOf` 10 处 + 断言，`BattleScene` 今日绿）。
7. **特效 `:72`** A31 的 9 个待接线（`PlayCraftVfx`）。
8. **特效 `:75`** 锻造按钮完整链路（`D_界面线_执行报告.md:83`）。
9. **特效 `:73`** 只核销「音缺件」那一半（4 条偏离要另立）。
10. **自检 `:49/:50/:51`** `T03·B6` / `T07·Badges` / `T08·A462`（`BattleScene` 5292/0 已付）。
11. **音效 `:20`** `SoundCueAssertT` 无宿主（`CardFaceAssertE5.cs:528`）。
12. **音效 `:26`** 85 条 `_vs_`（例外①，判据空）。

## 不能判的

1. **今日读数的归属**：`battlescene8.log` / `AnimFXCheck` 等日志**没有构建指纹**，不能 100% 绑定 HEAD（同 AUD2 `:25`）。
2. **真机类八条**（特效 `T07/T11/T12/T13`、`新账-86`、`DisplayStatusAnim`、`SetSwarm`、音效 `S04`/`CardMove`）：静态做不到，只能真机。
3. **T02 / 新账-69 `Raging Storm`**：`iso_*.log` **09:14 仍在写**，结论未知，⛔ 不据现有半成品判。
4. **工具线 `T05`**：余项是「调度口径不做 / 用户口径不清」，属**拍板**不属实现，我按 AUD `:131` 原样留 ⇒ 需主对话裁定。
5. **特效 `T17`**：`WFSpritesAdditive` 的 `_Flip` 只差 Unity 渲染，静态判不了。
6. **`W65·A1` 的 `inferred-name` 条数**：`Resources/card_vfx_by_card.json` 是精简版（无 `src` 字段）、全量件键名不同 ⇒ 未能精确复核 59 条/42 卡这个数字。
