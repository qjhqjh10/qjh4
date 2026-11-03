# B124 对账 D（待 Unity / 待真机 / 暂停远端）· 2026-11-03

> 代理：只读对账代理 D（批次 `B124`）。**全程没跑 Unity、没跑 typecheck、没跑任何 `--write/--fix`、没动 git、没改任何 `.cs/.json/.tsv`**；唯一写的就是本文件。
> 全部结论 = **现读磁盘**（`io.open(p,'rb')` 二进制读、按 `\r\n`/`\n` 切行；行号现读）。
> 前序：`判据/B123_对账_D_Unity前必核.md` 已给的三条结论**直接采用**，本文件不重复，只做①四档、②「是否真的只能等」、③开窗口命令、④回来条件。

---

## 〇 总表

> 四档：`✅已做完待核销` · `🟠真没做` · `⛔判据本身错/过期` · `❓查不到`。
> 「只能等什么」写的是**真正卡住的那一件事**；括号里标出「其实还有可离线做的部分」。

| ID | 四档 | 只能等什么 | 回来条件 |
|---|---|---|---|
| **B120-D13** | 🟠真没做 | **真机 A/B**（`WF_GRAB501=0/1` 两跑比「叠两遍 / 整块消失 / 闪」）。开关已现成：`GrabPassTransparentFeature.cs:441 DefaultLayer=9` · `:473` 读 `WF_GRAB501_LAYER` · `:403` `WF_GRAB501` | **先离线定层号**（「资产档 6 / 自动档 9 用哪个」是设备无关的判据，不用真机）→ 真机两跑无异常 → 改默认并补「显式 `=0` 回旧行为」断言。与 `D-01` 同一件事，一起判 |
| **B120-D16** | 🟠真没做 **（可离线，见 §九·2）** | **不需要等 Unity/真机** —— 是资产对齐 | 判据已足：原版 `QualitySettings.asset` **6 档且每档 `customRenderPipeline: {fileID: 0}`**、`GraphicsSettings.m_CustomRenderPipeline` = **`UniversalRenderPipelineAsset High`（guid `6ab4f96e…`）**；我们 2 档 + 每档挂 RPAsset。`Mobile_RPAsset.asset:29 m_RenderScale 0.8`（原版 High = **1**）· `:94 m_VolumeProfile = SampleSceneProfile`（原版 **{fileID:0}**）⇒ 照抄即可。⚠️ 全仓 0 处 `SetQualityLevel` ⇒ 今天**实际不生效**（影响面 0），但结构是偏离 |
| **B120-A11** | 🟠真没做 | **真机**（透视下「向屏幕中心收拢 ≈8%」正交相机表达不了；只有像素等价净缩放 1.0137×） | 拖起一张手牌看观感；要「真正复刻」得等 `D-1` 把 HUD 相机换透视后重做，⛔ 别用正交近似冒充 |
| **B120-A13** | ✅**已做完待核销**（判据已闭合） | —— 🟠**但顺手查出一条真缺陷**：我们自己的 EventSystem **没设 5 px** | 判据已找到：`d:/2/tools/assetripper_out/…/Scenes/Intro.unity:5429 m_DragThreshold: 5`（全工程唯一那只 EventSystem，反编译 0 处读写）⇒ 原版奔跑值 = **5 px**，不是 Unity 默认 10。**我们的 `BattleEventSystem.cs:162-166` 现建 EventSystem 从不设 `pixelDragThreshold`** ⇒ 现在是 **10 px**。→ 补 `es.pixelDragThreshold = 5`（收养/现建两条路）+ 一条断言 |
| **B120-F17** | 🟠真没做 | **真机听感**（批处理 mixer 快照不前进） | 打一局听 BGM 切档无爆音/停顿。判据已写死（`DAT_1834b3278 = -60 dB`，`BattleMusic.cs:216-253`）；离线只能核参数不能核听感 |
| **B120-Q07b** | ✅**已做完待核销** | —— 真机那行读数只是**确认**，不再是定案依据 | 已闭合：出厂 `FPSLimit=2 → Application.targetFrameRate=-1`（`Card3DAnimationController.cs:399-404`），钳制已删（`:405-407`，`B121-WB` 裁定照抄），断言 `B121_WB_Assert` C 段钉「-1 档第一次 `Update` 就收尾」。⚠️ 真机仍值得打一行 `Application.targetFrameRate` 看玩家设置是否覆盖 |
| **B120-F19** | 🟠真没做 | **Unity**（`BattleScene.Run` 里看 `B120_W11_Assert` 的 B0 / B0b） | 静态已足：`Main Mixer.mixer:178 Voices` · `Music.mixer:181/148/79`（`BattleGroupLow` / `BattleGroupMedium` / `BattleGroupHigh`），与 `WarpforgeAudio.cs:24/345` 逐字一致。红 = **mixer 导入**问题，不是判据问题。⚠️ `AudioSetup.Verify()` 在 batchmode 不跑 |
| **B120-E11** | ✅**已做完待核销** | —— 五项全部离线闭合 | ① ② ④ 已由 `B120-W18` 订正；③ `ValidateEventData` 已补导（`_tmp_view/ghidra_b120/out/LiveOpsEvent_object__object_.ValidateEventData__10173d0.c`）；⑤ **`DraftEventBase.get_EventPlayMode` 也已可读** → `d:/2/tools/decomp_generic_trial/out_b94/DraftEventBase_object__object_.get_EventPlayMode__1814550.c`（ICF 折叠，体 = 常量 `return 7`）。⚠️ 该行状态列**没写③⑤已解**，按铁律 5 应就地订正 |
| **B120-D17** | 🟠挂起（真机） | **与 `D-1` 绑死**（HUD 相机整台换透视 + 专用 UI renderer） | `D-1` 立起来 + 真机 A/B。离线可做的只有「把 `UniversalRenderPipelineAsset_Renderer UI.asset` 建进工程」这一半，但⛔ 不能单独接（要同时改 13 场 + 主菜单的 `m_RendererIndex` 与层方案） |
| **B120-D18** | 🟠挂起（真机） | **与 `D-1` 绑死**（层方案迁移会把层 6 的 440 对象 / 层 20 的 5 个爆炸件整块抹掉） | 同 `D-1`。⚠️ 与 `B120-D13` 是**同一局 A/B**（掩码 + 抓屏 501 分工成对改） |
| **B120-Q04e** | 🟠真没做（待 Unity 首跑取读数） | **Unity**（`BattleScene.Run`）。红因已能静态说死 = 射线 miss 不再回落投影 | 首跑先取读数（**别先改断言**）→ 按红清单离线改写三条旧断言（`B97_UI2_Assert:97-127` · `B97W37_Assert:100-112` · `BattleScene:38804/39089`）→ 下一窗口复跑。判红因：先看 `B97_UI2_Assert:101`「B2 前提真 collider 命中 ≥1」 |
| **B120-A16** | 🟠真没做（待 Unity） | **Unity**（`BattleScene.Run`）。`CardInteraction.cs:1084` 同行 `TryCommitWhileHolding` ⇒ 描到落点第一帧就 `Release` | 同样「先跑取读数 → 离线把四条改成**提交前量**（`BattleScene:6646/6650/6652` · `:41369-41377`）→ 复跑」。⛔ 别放宽期望 |
| **B120-Q04c2** | 🟠真没做（只剩③） | **真机**（③「HUD 正交平面→屏幕」换算与原版直吃 `TouchPosition` 是否恒等） | ① 已结（`FUN_180617800` = 指针按住态、松手无位置闸）· ② 已补导 ⇒ 只剩③。离线可做的一半：**1080p 下 108 px/单位 ⇒ 两种换算恒等**可推可断言（`LayoutSpace.ToWorld/FromPixel`），非 1080p 的缩放档才需要真机 |
| **B120-W20b** | 🟠真没做 **（可离线，一条实参）** | 不需要等 | `RuleCore.cs:9117` 现读 `u.AddKeyword(ConditionEval.Invulnerable, 0)`；`AddKeyword(string, int, bool silent = false)`（`UnitState.cs:1120`）已支持 ⇒ 补 `silent: true` 即字节级对齐。今天行为一致（原版净静默） |
| **B120-W20c** | 🟠真没做 **（可离线判）** | 不需要等 | 新反证：原版 `CardScript__ResolveCardPlayed.c` 走 `CardScript__RemoveTraitSilently(card, 5)`，而 `RemoveTraitSilently.c` 是 `List.Find` + `List.Remove` + `RemoveStatusAnim` ⇒ **只摘「一份」并且刷 UI**；我们 `RuleCore.cs:16192` 是 `RemoveAll("ephemeral", silent: true)`（`UnitState.cs:1282` 整份摘掉、无 UI 刷新）。⇒ 先判「ephemeral 会不会叠两份」（`buffType 25 ephemeralCopy` 是唯一叠加源），可达就改「摘一份」 |
| **B120-W15a** | 🟠真没做（只剩观感确认） | **真机**（飞牌起点偏不偏） | 离线已能降级：我们这一侧**不拷 localPosition**，`DrawStartWorld`（`BattleDriver.cs:41516`）给世界坐标、`v.SetPose(hudRoot.InverseTransformPoint(from))`（`:41593`）⇒ **与父级无关**，常量另有 `B120_W15_Assert` B6 灭自证。⇒ 真机只作观感确认，⛔ 别再改那两个 px 去凑 |
| **B120-Q04f** | 🟠真没做（待 Unity） | **Unity**（依赖 Q04g 的读数才能定文案） | 首跑拿到 `nHitOverlap` 后离线改文案（`BattleScene:39337` 的 `B2b②`「取 z 最小」+ `B97_UI2_Assert`/`B97W37_Assert` 的 `CardHit/HitByCollider` 措辞） |
| **B120-Q04g** | ❓**只能 Unity** | Unity 运行时行为（`RaycastNonAlloc` 缓冲序 vs z 序），本地任何静态源都判不了 | 跑 `BattleScene.Run` 看 `BattleScene:39337` 的 `pickA/pickB`：红且 `nHitOverlap == 1` ⇒ 缓冲序 ≠ z 序（判据变更实锤），按原版 `hits[0]` 认 |
| **D118-57** | 🟠真没做（待真机）· **阻塞点已变** | **真机**（三条淡入路径的观感） | ⛔ 原「`B118W14_Assert` 全仓 0 宿主」**已过期**：现已接 `BattleScene.cs:47035` 聚合行（在 `Debug.Log(合计)` 之前，**会真跑**）⇒ 自动兜底补齐，只剩真机看「淡得对不对」 |
| **D118-85** | 🟠真没做（待真机） | **真机 A/B**（正交 vs 透视、清屏色）· 包络那一半已改 | `LayoutSpace.cs:75-84 Apply()` 仍硬写 `orthographic = true`、`orthographicSize = 5`。**可离线先做的一半**：画质档数 **2 vs 原版 6** + 每档 RPAsset（见 `B120-D16`）—— 那是资产，不是投影 |
| **B121-BF-03** | 🟠真没做 | **Unity**（重建 leviathan 单场 prefab；盘上 13 件 mtime 全停在 11-01） | 现读 `battlearenaleviathan.prefab` / `battlearena1.prefab` 的 `PlayableDirector` 计数**都是 0**。离线面已闭：`arena_timelines.json`（`CardPresentation/Resources/`，5,282 B）在、`ArenaTimelinePlayer.cs` 在 ⇒ 只剩重建 + import + 真机看地堡门循环 |
| **B121-AUD-07** | 🟠等远端 | **远端**（EC `OpenSound` GUID `ba6b95f7…` 的 bundle 本地没有） | **本轮加验**：`d:/4/_tmp_view/apk135/`（安卓 1.35.0 远端 Addressables 已解包目录）全树 grep 该 GUID = **0 命中** ⇒ 连安卓 1.35.0 的包都不含它。回来条件 = 有更全的远端 addressables 包；现在 `LiveOpsLocalCatalog.cs:462` 已如实写 `"?"`，⛔ 别拿别的 cue 顶 |
| **D-01** | 🟠真没做（挂起） | **真机 A/B**（`WF_GRAB501=0/1`，排最末 —— 一开默认就是 13 场全局生效） | §D 裁决：等「能立刻做」组清零 + 用户点头开真机（2026-10-31 用户「解决所有代码层面的问题后再说」）。与 `B120-D13` 同批 |
| **D-02** | 🟠真没做（挂起） | **真机 dump / 真人走查**（AUD12 每窗 3–5 颗钮 + 命中栈 · AUD08 边框 `activeSelf` · AUD03 #62 · AUD05 J3/J5 · AUD10 走查） | 5 组全是运行期状态；**离线无代码可加**（`grep AUD12 --include=*.cs` = 0 命中，`B121_REC` 已核）。回来条件 = 开真机批 |
| **D-03** | 🟠真没做（挂起） | **真机**（时钟图预乘 α 观感 · 随机卡叠序 · `C7` CJK fallback 真渲 · mixer 真听感 · `N135`） | `N135` = `CollectionWindow.cs:1206-1208` 的 `Application.isPlaying` 闸，批处理结构性不可验（`B110_G5D:51`）。回来条件 = 开真机批 |
| **D-06** | 🟠真没做 | **Unity 探针**（`CollectionScene` 里打 `textInfo.characterCount / isActiveAndEnabled / PendingWrapAppliedCount`） | **可离线先插桩**（探针代码 + 三档判据），再在 `CollectionScene.Run` 里读。三条是条件项：探针证实才改「激活时生成」，⛔ 不是把 `AvatarTab` 改回「只建可见格」 |
| **E-03** | ⏸ 暂缓（用户 2026-11-02） | **不在排期**（逐场对比/优化类） | `T02/T03/T04/T05/T08` 五条仍开（`B114_R3` 是时效核查不是核销）；子项 `A1578`/`A1667` **可核销**。回来条件 = 用户解禁「逐场对比」 |
| **E-05** | ⏸ 等远端资源 | **远端**（`BattleEndPlayerData` 宿主 prefab 本地零实例） | `B121-REC`（2026-11-03）两路独立复核仍成立。回来条件 = 哪只包补进来 |
| **E-06** | ⏸ 用户拍板 | **用户**（「显隐由服务端条件驱动」→ 2026-10-18 拍板「多人优化时再做」） | 视图与点击本地半已通（`TopBar.cs:285/322/434-441`）；只剩服务端条件那一格 |
| **E-07** | 🟠真没做（剩 Unity + 真机） | **Unity import + 真机**（离线四项已全绿） | 现读 `battlearenaleviathan.prefab` 无 `PlayableDirector` ⇒ 需重建；余 = `ArenaAnimsProbe.Run` / `ArenaOriginalShaderRebindProbe.Run` 读数 + 真机看「动没动」。⚠️ **2 场建场（sororitas/astra）是 ⏸ 等用户拍板**（战场线总表 `:76`），不是 Unity 项 |
| **E-08** | ⏸ 本线暂停（用户 2026-10-25） | **用户解禁新手引导线** | 离线可做但**不许做**：剩下只有 `F9` 跳过键（全仓 `KeyCode.F9` / 跳过键 0 命中，确认「未接」成立）。7 阶段主链在 `-wfftue` 下已验到「选军队」那一跳 |
| **E-09** | ⏸ 本线暂停 | **用户解禁新手引导线** | 离线可做但**不许做**：`Tutorial Message / Highlight` 层整棵未建；⚠️ 建的时候必须同批改 `RewardsScene.cs:5957-5958` 那条**反向断言**（它把「不建」钉成期望） |
| **E-10** | ⏸ 三分 | 用户拍板不做 3（`P03` NAT-PMP · `P04` Sunset Popup · `P10` A1635）· 远端 3（`R04` · `P07` · `P08`） | 各自回来条件 = 用户改口 / 远端判据可得 |

> **计数**：33 行 = `✅已做完待核销` **3**（A13 · Q07b · E11）· `🟠真没做` **23**（其中**纯离线可做 3**：D16 · W20b · W20c；**另有离线子任务 4**：D13 · Q04c2 · D118-85 · D-06）· `❓只能 Unity 判` **1**（Q04g）· `⏸/远端/用户拍板` **6**（E-03 · E-05 · E-06 · E-08 · E-09 · E-10）。

---

## 一 开 Unity 窗口的执行清单（按顺序 · 全串行）

> 硬规矩：同一工程同一时刻只能一个 Unity 实例；跑了 `DeckScene.Run` 会**重写** `DeckEditor.unity`。
> 绿红判据 = 日志里各类自己的「合计」行，⛔ 不是退出码（`BattleScene.Run` 的 **139** 是已知间歇）。
> 全套一键 = `bash d:/4/Unity/工具/_run_8_checks.sh`（**现读 24 条** `run`，逐条已核）。

### 第 0 步 · 前置（约 1 分钟）
```bash
cd d:/4
git status --short                                  # 期望空
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py   # 期望 漂移 0 · 失败 0
TMPDIR=/tmp/wf_b124 bash d:/4/Unity/工具/typecheck.sh      # 期望 运行时 0 / 编辑器 0
mkdir -p d:/4/_tmp_view/scene_bak
cp "d:/4/Unity/MyGame/Assets/CardPresentation/Scenes/DeckEditor.unity" \
   "d:/4/_tmp_view/scene_bak/DeckEditor.unity.bak_20261103_b124"
```
**开窗前的离线准备（若还没做，先做完再开窗 —— 铁律「起 Unity 前先清干净」）**：
① `BattleEventSystem.Ensure()` 两条路补 `pixelDragThreshold = 5`（A13 新缺陷）· ② `RuleCore.cs:9117` 补 `silent: true`（W20b）· ③ `D-06` 的 `CollectionScene` 探针插桩 · ④（可选）`Mobile_RPAsset` 对齐（D16）。

### 第 1 步 · `RuleEngineTest.Run`（约 4 分钟）
```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod RuleEngineTest.Run -logFile "d:/4/_tmp_view/b124_ruleengine.log"
```
- 期望：`RE === 结束：N/N 全过`；**看失败 = 0**（基线 2026-11-02 = 14072/14072，本批加了类，N 会变大）。
- 逐类读新增合计：`B121D1` / `B121D1B` / `B121D3` / `B121D5` / `B121D5R` / `B121E03F_DATA` / `B121WC`。

### 第 2 步 · `BattleScene.Run`（约 8 分钟 · **本步信息量最大**）
```bash
… -executeMethod BattleScene.Run -logFile "d:/4/_tmp_view/b124_battle.log"
```
- 期望：`BT === 合计：N 通过 / M 失败`（基线 10132/0）。
- **本步一次性关掉/推进的账**：`B120-F19`（`B120_W11` B0/B0b）· `B120-Q04e`（`B97_UI2` / `B97W37`）· `B120-A16`（`LitSlotCount` 四条）· `B120-Q04f/Q04g`（`BattleScene:39337` 的 `pickA/pickB` + `nHitOverlap`）· `B120_W16_Assert` A 组 / X9 · **`D118-57` 的自动兜底**（`B118W14_Assert`，78 条，首次真跑）。
- 已知会红按 `B123` §2 那张表认（判据变更不是回归）；⛔ 首跑**只取读数不改断言**。

### 第 3 步 · `DeckScene.Run`（约 2 分钟）
```bash
… -executeMethod DeckScene.Run -logFile "d:/4/_tmp_view/b124_deck.log"
```
- 期望 `DK === 结束：N/N`（基线 2372/2372）。跑完 `git diff --numstat` 应只有 `DeckEditor.unity`。

### 第 4 步 · `CollectionScene.Run`（约 4 分钟）
```bash
… -executeMethod CollectionScene.Run -logFile "d:/4/_tmp_view/b124_collection.log"
```
- 期望 `[Collection] === 结束：N/N`（基线 3287/3287）。
- **`D-06` 的读数就在这一条**（探针三档）；`B121_C1 … 未验 4` 是 `MarkUnverified`，跑绿也关不掉 `B121-COLL-19`。

### 第 5 步 · 外壳五件（各 10～30 秒）
```bash
… -executeMethod ShellScene.Run     -logFile "d:/4/_tmp_view/b124_shell.log"    # 含 B118W5Assert（D118-38）
… -executeMethod MainMenuScene.Run  -logFile "d:/4/_tmp_view/b124_menu.log"
… -executeMethod ShopScene.Run      -logFile "d:/4/_tmp_view/b124_shop.log"
… -executeMethod RewardsScene.Run   -logFile "d:/4/_tmp_view/b124_rewards.log"
… -executeMethod SettingsScene.Run  -logFile "d:/4/_tmp_view/b124_settings.log"
```

### 第 6 步 · 战场（独立，⛔ 不许 `--all` / `BuildArenaPrefabs()`）
```bash
# ① 只重建 leviathan 一场（数据里只有它有 director）
unset ELECTRON_RUN_AS_NODE && WF_ARENA=battlearenaleviathan \
  "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaBuilder.BuildPrefabFromCLI \
  -logFile "d:/4/_tmp_view/b124_leviathan.log"

# ② 判产物（不开 Unity 就能查）
grep -c "PlayableDirector" "d:/4/Unity/MyGame/Assets/Resources/ArenaPrefabs/battlearenaleviathan.prefab"  # 现读 0 → 期望 ≥1
grep "\[Arena\] §27 \*\*单场\*\* prefab" d:/4/_tmp_view/b124_leviathan.log        # 期望 "✅ 建好"
grep "\[Arena\] Timeline：" d:/4/_tmp_view/b124_leviathan.log                    # 不该有 "找不到"

# ③ 两条战场探针
… -executeMethod ArenaAnimsProbe.Run                 -logFile "d:/4/_tmp_view/b124_anims.log"    # grep '^AAP '
… -executeMethod ArenaOriginalShaderRebindProbe.Run   -logFile "d:/4/_tmp_view/b124_shader.log"   # grep '^ASR '
```

### 第 7 步 · 收口
```bash
git diff --numstat     # 应只有 DeckEditor.unity
git checkout -- "Unity/MyGame/Assets/CardPresentation/Scenes/DeckEditor.unity"   # 不留这次重写时
```

---

## 二 真机清单（一次起 player 能验哪几条）

> 前置：**必须重出 player 包**（现包 `Assembly-CSharp.dll` = 2026-11-02 07:52，早于 11-03 的手感修复）。
> 起法 `-wfscene Shell`；点击 `工具/wf_click.py`（**关沙箱**）；截图进 `_tmp_view/shots/<日期_会话>/`；**验完即关游戏**。

| 序 | ID | 一步操作 | 读什么 / 截什么 |
|---|---|---|---|
| 1 | **B120-A11** | 拖起一张手牌 | 有没有「向屏幕中心收」的观感（没有 = 正交的已知限制，不是新缺陷） |
| 2 | **B120-F17** | 打一局听 BGM 切档 | 有无爆音/停顿（`DAT_1834b3278 = -60 dB`） |
| 3 | **B120-Q07b** | 打一行 `Application.targetFrameRate` | 期望 **-1**；若玩家设置覆盖则记真值 |
| 4 | **B120-W15a** | 抽牌看起点 | 偏不偏（偏也⛔ 别改 px，先回核 `BattleDriver.cs:41508`） |
| 5 | **B120-Q04c2③** | 拖一张牌横跨半个屏幕 | 命中/落点与你手指是否 1:1（HUD 换算恒等） |
| 6 | **D118-57** | 发牌淡入 / 阵亡消散 / 相关卡压暗 | 三条逐个看「淡得对不对」（`ArtOpaque.shader:70 c.a = i.color.a`） |
| 7 | **D118-85** | 看屏幕边角 HUD 元素相对大小 | 正交 vs 透视的差别（`D-1` 前置，只记录） |
| 8 | **B121-BF-03③** + **E-07 T00** | 进 leviathan 战场 | 地堡门**自动循环动**；legacy clip 动画「动没动」 |
| 9 | **D-01 / B120-D13** | 以 `WF_GRAB501=0` 与 `=1` 各起一次 | 比「叠两遍 / 整块消失 / 闪」，两次都要截图 |
| 10 | **D-02** | 逐窗点 3–5 颗钮 + 看边框 `activeSelf` | 每窗一屏图；AUD08 边框「该藏的藏住了吗」 |
| 11 | **D-03** | 时钟图预乘 α · 切页看随机卡叠序 · 中文/日文 fallback · 听 mixer · `N135` 卡池建完自动起教程 | 逐项一屏图/一句结论 |
| 12 | **D-06** | 打开档案窗切页（469 格） | 有没有一帧卡顿 / 缺字（与第 4 步的探针读数对读） |

---

## 三 暂停 / 远端（逐条回来条件）

| ID | 卡在哪 | 回来条件 |
|---|---|---|
| **E-03** | 用户 2026-11-02：「战场线也做，只是**对比优化**暂时不做」 | 用户解禁逐场对比 ⇒ 需先**重渲 13 场**（`RenderPreviewFromCLI` 即可，13 场 scene 已于 10-27 同步重建）再量；`A1578`/`A1667` 两个子项可**现在就核销** |
| **E-05** | `BattleEndPlayerData` 宿主 prefab 本地零实例（91 包 + AssetRipper 双路复核） | 哪只远端包补进来 |
| **E-06** | 顶栏「挑战」钮显隐由服务端条件驱动（用户 2026-10-18：「多人优化时再做」） | 用户开口做多人优化 |
| **E-08** | 新手引导线整体暂停（用户 2026-10-25） | 用户解禁；解禁后第一件 = `F9` 跳过键（纯离线） |
| **E-09** | 同 E-08；另 `Tutorial Message` 层要同批改 `RewardsScene.cs:5957` 反向断言 | 同 E-08 |
| **E-10** | `P03`/`P04`/`P10` 用户拍板不做 · `R04`/`P07`/`P08` 判据在已关服远端 | 用户改口 / 远端数据可得 |
| **B121-AUD-07** | EC `OpenSound` GUID `ba6b95f7…` 的 bundle 不存在（**连安卓 1.35.0 的 `_tmp_view/apk135/` 全树也 0 命中**） | 出现含该 Addressables 条目的包；补一行 `OpenSoundCueByGuid` |

---

## 九 顺手发现

1. 🔴 **`B120-A13` 底下藏着一处真偏离**：我们**所有** EventSystem 都是 `BattleEventSystem.Ensure()` 现建（`BattleEventSystem.cs:162-166`）或收养（`:121-158`），**两条路都没设 `pixelDragThreshold`** ⇒ 实际是 Unity 默认 **10 px**，原版 `Intro.unity:5429` 是 **5 px**。而 `AttackSelector.cs:252 PopDragThresholdPx = 5f` 已经把「手感口径 = 5」写进常量了 —— **常量对了、引擎那一格没设**，两处不一致。建议在 `Ensure()` 收口处补一行（两条路共用），并加一条「`es.pixelDragThreshold == 5`」断言。

2. 🔴 **`B120-D16` 的「等确认移动端是否要一并复刻」前提已过期**：原版 `QualitySettings.asset` **6 档、每档 `customRenderPipeline: {fileID: 0}`**，`GraphicsSettings.m_CustomRenderPipeline` = `UniversalRenderPipelineAsset High`（guid `6ab4f96e…`，`m_RenderScale 1` / `m_VolumeProfile {fileID:0}`）；我们是 **2 档 + 每档挂 RPAsset**，且 `Mobile_RPAsset.asset:94 m_VolumeProfile = SampleSceneProfile`（guid `10fc4df2…`，就是那个 URP 样例资产）· `:29 m_RenderScale 0.8`。按用户 18·0「默认复刻原版」，这条**不该再挂着问**，直接对齐即可（全仓 0 处 `SetQualityLevel` ⇒ 今天不生效、改动零风险）。⚠️ 同一条也是 `D118-85` 里「画质档数 2 vs 6」那半格的离线解。

3. ✅ **`D118-57` 的「断言 0 宿主」已被补上**（`BattleScene.cs:47035`，注释写明「B123 对账 D 发现…接在本聚合行、合计之前」）⇒ 该行阻塞点从「没有自动兜底」降级为「只剩真机看淡入」。**逐条核过：插入点在 `Debug.Log(P + "=== 合计…")` 之前、且不在行尾 `//` 之后** ⇒ 会真跑。

4. ✅ **`B120-E11` 的 ⑤ 也有体了**：`DraftEventBase.get_EventPlayMode` 在 `d:/2/tools/decomp_generic_trial/out_b94/DraftEventBase_object__object_.get_EventPlayMode__1814550.c`，**完全可读**（ICF 与 `System.Int16.GetTypeCode` 折叠，体 = `return 7`）。加上 ③（`_tmp_view/ghidra_b120/out/…ValidateEventData__10173d0.c`），五项**全部离线闭合**，该行状态列没写 ⇒ 按铁律 5 就地订正。

5. ⚠️ **`B120-W20c` 的「今天零可观测」值得复核**：原版摘 trait 5 是 **`List.Find` + `List.Remove`（摘一份）+ `RemoveStatusAnim`（刷 UI）**，我们是 `RemoveAll`（**整份**摘掉）+ 无 UI 刷新。唯一叠加源是 `buffType 25 / 26`（`ephemeralCopy` / `tideCopy`，`BattleContext.cs:1523`）⇒ **可达性要先判**，别直接写「零可观测」。

6. ⚠️ **`B120-W15a` 在我们这一侧已经不构成风险**：飞牌起点走 `DrawStartWorld`（世界坐标）+ `hudRoot.InverseTransformPoint`，**与卡的父级无关**；原版「拷 localPosition」那半只是**知识缺口**（原版卡挂在哪一级无判据），不是我们的缺陷。

7. ℹ️ **`_run_8_checks.sh` 现读 = 24 条 `run`**（`grep -cE '^run [A-Za-z]'` = 24），与 `B123` 的发现一致；`B121_宿主接线清单.md:3` 写的 27 仍是错的（我没改那份，不在白名单）。

8. ℹ️ **`Rules/` 层面两个「起 Unity 前可清」的离线活（本批不占写手）**：① `D-06` 的探针插桩（`CollectionScene` 里三档判据）② `E-08`/`E-09` 的活**虽然判据已足，但被用户 2026-10-25 的线暂停挡住** ⇒ ⛔ 别借着「离线可做」去开工。

9. ℹ️ **`B121-BF-03` 的离线面确实已闭合**：`arena_timelines.json`（`CardPresentation/Resources/`，5,282 B，`{schema,note,arenas}`）在、`ArenaTimelinePlayer.cs` 在、生成器 `--check` rc=0（`B123` 现跑）⇒ 只剩「重建 1 场 prefab + import」。现读 `battlearenaleviathan.prefab` / `battlearena1.prefab` 的 `PlayableDirector` **均为 0**，与 `B123` 一致。

---

## 附 · 本文件的证据来源（全部现读）

- 活状态表：`d:/4/Unity/资料/完全复刻/活状态表.md`（`§B-14` L817-879 · `§B-13` L722/750 · `§B-15` L966/981 · `§D` L1026-1046 · `§E` L1056-1064）
- 断言宿主：`CardPresentation/Editor/BattleScene.cs:47035`（`B118W14_Assert.Run()` 现读在此）· `BattleScene.cs:6638-6652`
- 输入：`CardPresentation/Battle/BattleEventSystem.cs:121-178` · `Battle/AttackSelector.cs:225-255` · `Battle/Card3DAnimationController.cs:390-416`
- 渲染资产：`Assets/Settings/{PC,Mobile}_{RPAsset,Renderer}.asset` · `ProjectSettings/{GraphicsSettings,QualitySettings}.asset` · `d:/2/tools/assetripper_out/ExportedProject/ProjectSettings/QualitySettings.asset`
- 引擎：`RuleEngine/Core/RuleCore.cs:9117/16192` · `RuleEngine/Core/UnitState.cs:1120/1282` · `CardPresentation/Core/LayoutSpace.cs:75-84`
- 反编译：`d:/2/tools/decomp_full/CardScript__SetInDeckPosition.c` · `CardScript__RemoveTraitSilently.c` · `CardScript__ResolveCardPlayed.c` · `d:/2/tools/decomp_generic_trial/out_b94/DraftEventBase_object__object_.get_EventPlayMode__1814550.c` · `d:/4/_tmp_view/ghidra_b120/out/LiveOpsEvent_object__object_.ValidateEventData__10173d0.c`
- 产物：`MyGame/Assets/Resources/ArenaPrefabs/*.prefab`（`PlayableDirector` 计数 0）· `d:/4/Unity/工具/_run_8_checks.sh`（24 条）· `d:/4/_tmp_view/apk135/`（GUID 0 命中）
