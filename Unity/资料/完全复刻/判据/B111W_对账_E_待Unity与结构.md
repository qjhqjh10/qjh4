# B111W 对账 · E 组（待 Unity / 结构）

> **只读对账**（2026-11-01）。负责 `B-13` `B-33` `B-45` `B-46` `B-55` `B-129` 六条，检查「**是不是已经做完、只是没核销**」。
> ⛔ 全程**没跑 Unity**、没动 git、没改任何已有文件（含 `活状态表.md` / 账 / 正本）。
> 判据源 = `活状态表.md` 那六行 + **当前代码/资产现读**；行号一律现读。

| ID | 现读结论 | 证据（文件:行号） | 判定 | 还差哪一步（要跑哪个入口） |
|---|---|---|---|---|
| **B-13** | **包已装、资产已齐，但 `VFXManager.asset` 仍是空表（308 B / 2026-09-10，五 shader 全 `{fileID: 0}`、无 `m_RuntimeVersion`）**。⇒ 「定档」只做完「装包」那一步；12 kernel 真播从未验 | `MyGame/Packages/manifest.json:12`（`com.unity.visualeffectgraph: 17.3.0`）· `MyGame/Packages/packages-lock.json:178`（`source: builtin`）· `MyGame/ProjectSettings/VFXManager.asset`（12 行 · 308 B · mtime 2026-09-10）· `MyGame/Assets/WarpforgeVFX/Editor/B109_E25_VfxAsset_Assert.cs`（宿主 `MyGame/Assets/CardPresentation/Editor/BattleScene.cs:46896`）· `工具/_run_8_checks.sh:113` | 🟠 **真没做**（剩的那半是 Unity/人眼，代码侧无处可做） | 开 Unity（**见「顺手发现 ②」：批处理跑过 24 次它都没被重写**）⇒ 读 `VFXManager.asset` 的 `m_RuntimeVersion != 0` + 5 个 shader 引用非空 ⇒ `AssetDatabase.LoadAssetAtPath<VisualEffectAsset>` 非空 ⇒ Play 看 `Aeldari Portal` 12 kernel（画面由用户看） |
| **B-33** | **仍是原样**：`RuleEngineTest.cs:1378` 一行 6 个断言类（`EngineE1Assert` / `C1_EngineFixes_Assert` / `C8_Assert` / `B110_E5_Assert` / `B111E3B_Assert` / `B112_T2C_Assert`），全文件 39,715 行 | `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs:1378`（现读：该行 6 处 `*_Assert.Run()`） | 🟠 **真没做**（纯代码结构债，**不需要 Unity**） | 拆成每行一条 ⇒ **同批**跑 `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix`（不拆行会整片漂）⇒ 必须**真跑** `RuleEngineTest.Run` 看断言印出来（同族纪律见 `判据/B110_G5D_§8§9§10§11复核.md:50` 的 N134） |
| **B-45** | **仍是 🔶 自己设计**：文件头自己写着「本轮没有逐指令重建那条曲线」「🔶 自己设计（非原版）」，算式 `reveal/soft/opened/edge` 仍是 2026-10-27 那版，此后无任何改动 | `MyGame/Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader:16-20`（🔶 标记 + 裁定日期）· `:116-122`（`reveal` / `soft` / `opened` / `edge` 四行算式） | 🟠 **真没做**（差 Unity 差分实验） | 差分实验：同一材质分别跑原版 DXBC 与我们的替身，量「`_Reveal` 推移时亮带前沿的角位置」⇒ 拟合出原版曲线 ⇒ 改 `:116-122`。原版 HLSL 已被剥，只剩 `compressedBlob`（DXBC），所以只能在 GPU 侧比 |
| **B-46** | ✅ **已经做完了**（状态行是旧的）：三份 mixer 现读 **Main 1 / Music 5 / FX 2 = 8 条快照**，名字与 `B109_R11` §1.4 **逐条相同**（`Snapshot` · `General/NoVolume` · `General/BattleMusicLow/Medium/High/NoBattleMusic`）；组数 5/5/2、`m_SuspendThreshold` 全 `-50`、暴露参数含 `VolumeMaster`（原版 5 + Music 3）——**§2 那 11 条差已全部消失** | `MyGame/Assets/CardPresentation/Resources/Audio/Main Mixer.mixer`（5 组 / 1 快照 / start `24500006` / thresh −50 / 暴露 `VolumeFX,VolumeJingles,VolumeMaster,VolumeMusic,VolumeVoices`）· `Music.mixer`（5 组 / 5 快照 / start `24500022` / 暴露 `TrackHigh/Low/MediumVolume`）· `FX.mixer`（2 组 / 2 快照）· 结构断言 `MyGame/Assets/CardPresentation/Editor/B109_E17_Mixer_Assert.cs`（复用 `AudioSetup.MixerStructureReport`）· **宿主 `MyGame/Assets/CardPresentation/Editor/ShellScene.cs:11093`**（实测：该行第 4805 字符 = `B109_E17_Mixer_Assert.Run(false)`，行内**第一个 `//` 在 6664** ⇒ 在注释之前、**真的会执行**；调用链 `ShellScene.Run():1009 → RunCore():1038 → EditorApplication.Exit(_sink.Fail>0?1:0) :11100`）· `Core/WarpforgeAudio.cs:270-278`（6 档名字）· `工具/_run_8_checks.sh:58`（`AudioSnapshotCheck.Run`）· `资料/完全复刻/调度台日志.md:12826`（2026-11-01 · **24 入口 24/24 全绿**） | ✅ **代码侧已做完**（实物 + 断言 + 宿主三者齐，且已被 2026-11-01 那次全套覆盖） | 只剩**留档**：复跑一次 `B109_E17_Mixer_Assert.Run`（或 `AudioSetup.Verify`）拿 `AUD-MIX === 合计：N 通过 / 0 失败 ===` 存档（本机 `_tmp_view/audiosnap.log` 已被清空 ⇒ 现无日志可引）。**没有代码工作** |
| **B-55** | **确实只做了 3/4**：2 脚本 + 2 TSV 都在（现读 1,023 / 426 行 · 157 / 1 行），**第 4 件「窗口渲图宿主」全仓 0 命中**，`_tmp_view/shots/win/` 也不存在 | `工具/fp_oracle_check.py`（1,023 行）· `工具/fp_sbs_check.py`（426 行）· `资料/完全复刻/判据/功能点_oracle规格.tsv`（157 行）· `判据/功能点_并排比记账.tsv`（**1 行 = 只有表头**，设计如此：代理不许代签）· `grep -rn "RenderWindowTo\|shots/win" MyGame/Assets --include=*.cs` = **0** · `ls _tmp_view/shots/` = 只有 `1024_e6` / `2026-10-24_A1_段位图案` / `2026-10-24_M2对比` | 🟠 **真没做** | ① 给每个 `CardPresentation/Editor/*Scene.cs` 加一个**只读** `RenderWindowTo(窗名)`（→ `_tmp_view/shots/win/<窗名>.png`）② 由**主对话跑 Unity** 出图 ③ 由**用户签** `功能点_并排比记账.tsv`（代理不许代签） |
| **B-129** | **不是一条可核销的活，是一条同步点纪律**；其工具与现状都已就位：`--fix-meta-guid` 已实现（`--dry-run` 也在），两个 Texture3D `.meta` 现读**已经是原版 guid** ⇒ 现在跑它 = 幂等零写入 | `工具/build_texture3d.py:117`（`--fix-meta-guid` 说明）· `:89` / `:100`（期望 guid `0ca68a91…` / `97873f43…`）· 实测 `MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared/vectorfield.asset.meta` = `0ca68a919af0232488e11af9e15d3695` · `SDF_Test.asset.meta` = `97873f430c01bb8418bc8463d7963d81` · `.gitignore:26`（`/Unity/MyGame/Assets/WarpforgeVFX/*` ⇒ 这两份**不进 git**） | **不是活，是同步点检查项** | 每次跑完 `B109_E19_Texture3DBuild.Run` **且等 Unity 退出**后跑一次 `python -I -X utf8 工具/build_texture3d.py --fix-meta-guid`。⛔ 别从活状态表里当「待办」关掉 —— 它是**每次同步点必做**的检查项 |

## 逐条明细

### B-13 · VFXManager 定档 + 灵族 12 kernel 真播

**现读**（`MyGame/ProjectSettings/VFXManager.asset`，12 行 / 308 B / mtime **2026-09-10**）：

```
m_IndirectShader: {fileID: 0}
m_CopyBufferShader: {fileID: 0}
m_SortShader: {fileID: 0}
m_StripUpdateShader: {fileID: 0}
m_RenderPipeSettingsPath:  （空）
```

- 没有 `m_PrefixSumShader` / `m_EmptyShader` / `m_RuntimeResources` / `m_CompiledVersion` / `m_RuntimeVersion` —— 与原版（`m_CompiledVersion: 7` · `m_RuntimeVersion: 38` · 5 shader + `m_RuntimeResources`）差得就是这一层。
- 包侧**已就位**：`manifest.json:12` = `com.unity.visualeffectgraph: 17.3.0`，`packages-lock.json:178` `source: builtin`（不联网）。
- 资产侧**已就位**：`Aeldari Portal.asset` / `Aeldari Dust.asset` + 6 个输出 shader 早在工程里，`B109_E25_VfxAsset_Assert` 已在 `BattleScene.cs:46896` 的聚合行宿主里（2026-11-01 全套 24/24 覆盖）。
- **结论**：这条**不是「代码还没写完」**，而是「**剩下那一步只有 Unity 能走**」。⛔ 别手抄原版那份 `VFXManager.asset`（原版 7 个 guid 在本机 PackageCache 0 命中，抄进去 = 新一批悬空，`B110_R5` §B-13 已写明）。

### B-33 · `RuleEngineTest.cs:1378` 一行挤 6 个断言类

现读该行（截断显示）：

```
EngineE1Assert.Run(); _pass += …; C1_EngineFixes_Assert.Run(); … C8_Assert.Run(); … B110_E5_Assert.Run(); …
B111E3B_Assert.Run(); … B112_T2C_Assert.Run(); …
```

- **为什么当时挤成一行**：不挪账的 A 行落点（同一文件里大量 `文件:行号` 引用，插行会让下游整片漂）。
- **所以拆行不是单纯的排版**：必须与 `check_ledger_points.py --all-A --fix` **同一批**做，并且拆完**必须真跑** `RuleEngineTest.Run` 看断言印没印出来（同族纪律 = `判据/B110_G5D_§8§9§10§11复核.md:50` 的 N134 那行）。
- 这条与 `MainMenuScene.cs:18498` 那行（N134）是**同一族的第二个实例**，可并批做。

### B-45 · `WFUIBorderHighlight.shader` 的 `_Reveal` 算式

现读文件头（`:16-20`）：

```
// ⚠️ 本文件是「属性表兼容的替身」，不是原版像素级复刻：
//   …本轮没有逐指令重建那条「旋转噪声 + `_Reveal` 揭开」的曲线…
//   ⇒ 🔶 自己设计（非原版）：噪声取模、揭开方向、柔化曲线都是我们挑的 · 调度台 2026-10-27 裁定
```

算式（`:116-122`）仍是：

```hlsl
float reveal = saturate((_Reveal + 1.0) * 0.5);
float soft   = max(_Smoothness, 0.001);
float opened = saturate((reveal * (1.0 + soft) - ang) / soft);
float edge   = saturate(1.0 - (0.5 - length(c)) * 2.0);
```

- 这条**不存在「代码侧已经写好了」的可能** —— 原版 HLSL 源码被剥（`Shader.m_Script = null`），只剩 DXBC；只能靠**同一材质两版并排跑**量出前沿角位置再拟合。
- 该文件**进 git**（`git ls-files` = tracked；`git check-ignore` = 不忽略）⇒ 改了就落盘，不存在「改在生成物里」的问题。

### B-46 · 三份 mixer / 8 条快照

**现读三份文件（逐块解析，不靠 grep 计数）**：

| mixer | 组 | 快照（名字 · `m_FloatValues` 格数） | `m_StartSnapshot` | `m_SuspendThreshold` | 暴露参数 |
|---|---:|---|---|---:|---|
| `Main Mixer.mixer` | 5 | `Snapshot`（10） | `24500006` | −50 | `VolumeFX` `VolumeJingles` `VolumeMaster` `VolumeMusic` `VolumeVoices` |
| `Music.mixer` | 5 | `General` `BattleMusicLow` `BattleMusicMedium` `BattleMusicHigh` `NoBattleMusic`（各 10） | `24500022` | −50 | `TrackHighVolume` `TrackLowVolume` `TrackMediumVolume` |
| `FX.mixer` | 2 | `General` `NoVolume`（各 4） | `24500010` | −50 | （0 条，与原版一致） |

与原版（`判据/B109_R11_音频mixer.md` §1.1 / §1.3 / §1.4 / §2）**逐格一致**：3 份 mixer · 12 组 · 8 快照 · 名字全对 · `VolumeMaster` 有了 · `m_SuspendThreshold` 由 −80 变 −50。

**为什么判「已做完」而不是「差一次批处理」**（证据链三段，缺一段才算没做）：

1. **结构断言存在且只有一份实现**：`AudioSetup.MixerStructureReport()`（`Editor/AudioSetup.cs:986`），`B109_E17_Mixer_Assert.cs` 只是复用它。
2. **断言真会跑**：宿主 = `Editor/ShellScene.cs:11093`；现读该物理行：`B109_E17_Mixer_Assert.Run(false)` 在第 **4805** 字符、该行**第一个 `//` 在 6664** ⇒ **在注释之前**（CLAUDE.md 那条「追加落进行末 `//`」的陷阱不成立）；调用链 `Run():1009 → RunCore():1038`，方法尾 `:11100` 是 `EditorApplication.Exit(_sink.Fail > 0 ? 1 : 0)`。
3. **它确实绿过**：`调度台日志.md:12826` 记 2026-11-01 **24 入口 24/24 全绿**；mixer 提交于 `972983a1`（2026-10-31 20:29）⇒ 那次跑在它之后。`ShellScene` 退出码 0 ⇒ `_sink.Fail == 0` ⇒ `B109_E17_Mixer_Assert.Fail == 0`。

⚠️ **唯一缺的是「日志留档」**：`_tmp_view/audiosnap.log` 不存在（`_tmp_view/` 里最新的是 10-31 的脚本），所以**现在没有一行 `AUD-MIX === 合计 …` 可引**。建议复跑一次留个底，但**不要再把它排进「待做」**。

### B-55 · N109 第 4 件「窗口渲图宿主」

- 3/4 的实物都在（脚本 + TSV 现读 1,023 / 426 / 157 / 1 行）。
- 第 4 件：`grep -rn "RenderWindowTo\|shots/win" MyGame/Assets --include=*.cs` = **0**；`_tmp_view/shots/` 下只有 3 个历史目录，没有 `win/`。
- 工程里**有**别的截图路（`PlayerBoot.cs` / `PlayerBuild.cs` / `BattleAutoDrive.cs` / `LutBlendProbe.cs` 用 `ScreenCapture`/`ReadPixels`），但**都不是「逐窗名出图」的宿主**，不能顶替。
- `功能点_并排比记账.tsv` 只有表头是**设计**（`B110_G5D` 明写「代理不许代签」）⇒ 别把它当「没做」。

### B-129 · `--fix-meta-guid` 同步点纪律

- 工具已成：`工具/build_texture3d.py:117` 起一节专讲，`:488-512` 是参数解析，`:384` 是 `do_fix_meta_guid()`（含 `--dry-run`）。
- 现状**已经满足**：`vectorfield.asset.meta` = `0ca68a919af0232488e11af9e15d3695`（= `:89` 期望值）、`SDF_Test.asset.meta` = `97873f430c01bb8418bc8463d7963d81`（= `:100` 期望值）⇒ 现在跑只会打印「已是原版 guid（零写入）」。
- 为什么必须留：`.gitignore:26` 把 `/Unity/MyGame/Assets/WarpforgeVFX/*` 全忽略 ⇒ 这两份 `.meta` **进不了 git**，干净树重跑 `Texture3DBuild` 会拿新 guid。
- **判定 = 不是活，是同步点检查项**（同 `B-90` / `B-106` 那类「长期纪律」行）。

## 顺手发现（单列 · ⛔ 我没有改任何东西）

1. **🔴 混音效果链（Duck Volume / Send / Receive）从未做，且活状态表里没有对应行。**
   `判据/B109_R11_音频mixer.md` §1.5 给了逐组清单，`报告/B109_E17.md` §4-3 明写「**本批没做**，必须在 Audio Mixer 窗口里加」，而我 `grep -n "Duck" 活状态表.md` = **0 命中**。
   缺的是：`Main.FX` / `Main.Music` 各一条 `Duck Volume`（−25 / −35 起）· `Main.Voices` 两条 `Send` · `Main.Jingles` 一条 `Send` · `FX.Master` 一条 `Receive` · `numSideChainBuffers`（Main 2 / FX 1 / Music 0）。
   按铁律 11「与原版不符 ⇒ 必须做」，这应当**立一条新账**（别混进 `B-46`，`B-46` 只讲快照）。

2. **B-13 的一条判据线索（不是结论）**：VFX Graph 包 2026-10-31 18:32 就装好了，2026-11-01 又跑过 **24 个批处理入口**，而 `VFXManager.asset` 的 mtime **仍是 2026-09-10** ⇒ 「打开工程 ⇒ Unity 自动重写 `VFXManager.asset`」（`B110_R5` §B-13 改法第 1 步）这条**在 batchmode 下没有发生**。
   ⇒ 剩下的那一步**可能必须开非批处理编辑器**（或先在 Inspector 里选中 VFX 资产触发导入）。下次派人做之前**先核这一条**，别按旧判据「跑个批处理就会自己好」。

3. **`B-46` 那行的**描述文字**本身也过期**：状态列写「三份 `.mixer` 各只有 1 条 `Snapshot`」，但现读是 **Main 1 / Music 5 / FX 2**；这与它自己引的证据锚（`B109_R11` §2 第 6 条「8 条 vs 我们 6 条」）**措辞也对不上**。核销时建议一并改写，别只把「未开始」改成「已核销」。

4. **`B-55` 那行漏了一半阻塞**：`B110_G5D` 与 N109 原文都写着「**卡面并排比比例未统一（我们 700×1115 vs PnP 900×1200）仍是第一号阻塞**」，但活状态表 `B-55` 那行只写了「窗口渲图宿主从没建」。核销/派活前要补进去，否则「渲图宿主建完」会被误当成本条完成。

5. **`AudioSnapshotCheck` 与 `B109_E17_Mixer_Assert` 有重叠但不完全覆盖**：前者断 6 档名字 + 值 + `ParamsOk`，后者断**三份 mixer 的结构七条**。两条都在跑（`_run_8_checks.sh:58` 与 `ShellScene` 汇总行），**没有重复实现**（都复用 `AudioSetup` 的三份 Report）。这一格是健康的，不是问题。

## 汇总：✅ 1 / 🟠 4 / ⛔ 0 / ❓ 0 （+ 1 条「不是活，是同步点检查项」）

- ✅ `B-46` —— **已做完，只差核销**（实物 + 断言 + 宿主 + 2026-11-01 全绿覆盖）。
- 🟠 `B-13`（剩 Unity 开编辑器读 `m_RuntimeVersion` + 用户看 12 kernel）· `B-33`（纯代码，拆行 + 同批 `--fix` + 真跑宿主）· `B-45`（Unity 差分实验）· `B-55`（要新建 `RenderWindowTo` + 用户签）。
- **同步点检查项**：`B-129`（每次跑完 `B109_E19_Texture3DBuild.Run` 且 Unity 退出后跑 `--fix-meta-guid`；现状已幂等）。
