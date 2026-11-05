# B148 · Unity 窗口执行记录（2026-11-05 起）

> 施工依据：`判据/B147_对账_R5Unity构建.md` §〇（15 步）+ `判据/B148_W10_战场粒子闸.md` §四（闸门重建后的期望值）。
> ⛔ 同一工程只能跑一个 Unity 实例；每次一条 `-executeMethod`，串行。
> 🔴 启动前必须 `unset ELECTRON_RUN_AS_NODE`。

## 进度

| # | 步骤 | 命令 | 状态 | 验收值 |
|---:|---|---|---|---|
| 0 | 三场清单重生成 | `gen_unity_arena_manifest.py --write --arena <场> --no-copy` | ✅ 2026-11-05（arena1 / arena2 / astramilitarum） | 三场 rc=0 |
| 1 | 纹理导入设置 | `ArtBaker.ApplyImportSettings` | ✅ 2026-11-05 rc=0（日志唯一 Error = licensing token，非业务） | 无报错；`Resources/Art` 的 mip/wrap 不被无条件覆盖 |
| 2 | 战场纹理导入 | `ArtBaker.ApplyArenaImportSettings` | ✅ 2026-11-05 rc=0（日志无业务错误） | 与 `arena_texture_import.tsv` 逐张一致 |
| 3 | 单场探针（加性混合） | `WF_ARENA=battlearena3 ArenaBuilder.BuildFromCLI` | ✅ 2026-11-05 rc=0 | **盘上实测**：`PS_citclr_light` / `PS_treasure_ray` / `PS_default_001_add` 的 `_SrcBlendAlpha` 全部 = **5**（原为 1）；`PS_bubble_light`（emperorschildren，未重建）仍 1 —— 预期 |
| 4 | 战斗场景重建 | `BattleScene.BuildAndSaveScene` | ✅ 2026-11-05 rc=0 | **7 格全对**：AmbientEquator (0.3255,0.5862,1) · AmbientGround (0.5896,0.5896,1) · SubtractiveShadow (0.6368,0.7276,1) · **`m_BakeBackend: 0`（Unity 6 里没被归一）** · PVRSampleCount 500 · `m_StitchLightmapSeams: 0` · Gauss 1/5。⚠️ `git diff` 1699/1702 是**累积差异**（本次无预跑快照，不能当证据） |
| 5 | 主菜单重建 | `MainMenuScene.BuildAndSaveScene` | ✅ 2026-11-05（**重跑一次才过**） | 首次跑完六格只生效 Occlusion（near 仍 0.01 / far 仍 200）⇒ 根因 = `rt.Build()` 内部 `MainMenuRuntime.cs:456` 又跑一次 `LayoutSpace.Apply`，把值打回。已把六格挪到 `rt.Build()` **之后**并写死「⛔ 别挪回去」；重跑后 **near 0.3 / far 1000 / HDR 0 / MSAA 0 / Occl 0 / depth −1** 全对（`orthographic: 1` 按设计不照抄） |
| 6 | 逐场 prefab 重建 | `WF_ARENA=<场> ArenaBuilder.BuildPrefabFromCLI`（arena1/2/astra） | ✅ 2026-11-05 rc=0 | **逐个现读**：arena1 `LR=6 Anim=2 Clip=0` · arena2 `3/3/0` · astra `9/0/1` 且 `m_Animation` = `{fileID:7400000, guid:1235f5e8…}`（非 0）；PS 数 34 / 46 / 20 |
| 7 | 13 场粒子闸验证 | 其余 10 场 `BuildPrefabFromCLI` 后统计 | 🟡 **数量 ✅ 576 · 挂载 76/101 · 剩 25 待查** | ✅ `!u!198` 合计 **576**（逐场与 R7 预测逐个吻合）。挂载三跑：**101 全失败** → 加 `FindByOriginalPath`（去前缀 + 唯一同名叶）→ **剩 25**（blacklegion 1 · darkangels 5 · tau 19）→ 再加「按 `go` 层数父先子后」→ **仍是 25**（⇒ 原因不是顺序）。⚠️ `照原版关着的路径节点` 三跑都是 **0**（W10 预期 tau 46 / DA 8）。**下一步诊断点**：挑 tau 那颗 `…/Bullet Particle (1)/Muzzle Flash glow/Sphere lightning`，看我们 prefab 里 `Muzzle Flash glow` 这级**到底存不存在**（多半是父级被 `ApplyGroupNodes` 合并/改名） |
| 10 | 8 宿主串行 | `_run_8_checks.sh`（12 条） | 🔴 **跑完 = 3 崩溃 + 111 条失败** | `ruleengine` ❌ · `battle` **13001/67** · `deck` **RunCore 中途抛异常（合计不完整）** · `shell` **同为中途抛异常** · `menu` **同为中途抛异常** · `rewards` 3341/6 · `collection` 未取到合计 · `settings` 2109/11。**已归因三类**：① **W3/W6 字体缺省面有意改动** ⇒ shell 3 条旧期望过期（`MenuDraw.Text/TextBox` 仍期望 CJK）、ruleengine `EvtKind` 期望 21+ 而现读 **27**；② 🔴 **W7 的 `BringToFront` 队列偏置是真回归** —— 它把压暗层/吸收层的档一起抬了 ⇒ menu 3 条 + collection 3 条的「吸收层档 = 内容命中档 − 1」全红（3126→3140 / 3307→4051）；③ shell 崩溃点 = `InvalidOperationException: DontDestroyOnLoad … ~E11dShell`（编辑模式下不允许，**是否本轮引入待查**）。其余约 90 条需逐条三分类 |
| 8 | UI Renderer 资产 | 建 `UI_Renderer.asset` + `Battle.unity:963` rendererIndex 1 | ⬜ | `Battle.unity:963` = 1 |
| 9 | Audio Mixer | 面板加 3 Send + 2 Duck Volume | ⬜ | 10 个效果控制器 |
| 10 | 8 宿主串行 | `_run_8_checks.sh`（12 条） | ⬜ | 逐条看合计数 |
| 11 | 层迁移 | 先 UI→5，再场地→0 + 两机掩码同批 | ⬜ | 相机掩码 1137 / 32 |
| 12 | 输入后端 | `activeInputHandler 2→0`（**必须最后**，改完重启编辑器） | ⬜ | HUD 输入仍可达 |

## 已知前置

- 13 场 scene mtime 落后于 `ArenaBuilder.cs` ⇒ 出图前必须先跑一次 BuildAll（本窗口内做）。
- `BattleScene.Run` / `MainMenuScene.Run` **都不存盘** ⇒ 重建必须显式 `BuildAndSaveScene`，且先建后跑。
- 本批新增断言已接线：`B148_W2 → RuleEngineTest:1400` · `W2/W3/W4/W6/W7/W8 → BattleScene:47042` · `W10 → ArenaBuilder.BuildArenaPrefabs() 尾`。

## 每步跑完要做的

1. 把状态改成 ✅ 并写「验收读数」。
2. 若期望值不符 ⇒ **先记下来**（⛔ 别当场改代码，Unity 跑着改 `.cs` 会污染下一次构建）。
3. 全部跑完再统一回代码层修。
