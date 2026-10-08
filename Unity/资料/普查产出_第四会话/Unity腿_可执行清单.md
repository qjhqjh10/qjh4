# Unity 腿 · 可执行清单（第四会话 · 只读盘点产出）

> 只读盘点代理 · 2026-10-08。⛔ 没跑 Unity · 没动 git · 没改任何别的文件。
> 用途：把这批「只能跑 Unity 批处理才能收口」的账排成**主对话一趟趟照敲**的清单。
> 通用起手（每条都用这个）：`unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod <入口> -logFile "d:/4/_tmp_view/<名>.log"`，`UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"`。
> 🔴 判绿红**看日志末的「断言合计」、不看退出码**（`BattleScene.Run` 的 139 是已知间歇段错误，`工具/_run_8_checks.sh:22`）。

---

## 1 · `A845(a)` 网格导入路实跑验证 `CopySerialized` 落到已有网格资产上

| 字段 | 内容 |
|---|---|
| 要跑什么 | ① 全量重导：`-executeMethod EffectExporter.Run -logFile d:/4/_tmp_view/exp_a845.log`（默认 `Resume=false` = 清产物重导）② **回读**：`-executeMethod MeshSanityProbe.Run -logFile d:/4/_tmp_view/ms_a845.log`（筛 `^MS`，逐网格打顶点极值 + 单独渲图）③ **必须跟三步**：`EffectExporter.RunListed` → `BoosterPackExporter.Run` → `EffectLibraryBuilder.Run` |
| 前置 | 零代码改动（只跑 + 回读）。源码注释 `EffectExporter.cs:1936-1940` 自陈「这条路没实跑过…第一次跑完必须回读 `_typelessdata`」；`d:/2` 原版包要在位（读的是源 bundle）。 |
| 判定 | `Assets/WarpforgeVFX/Meshes/*.asset` 回读 `_typelessdata`：**NaN/Inf 或 \|值\|>100 = 仍坏**，正常应落 ±1 量级（判据读法 = `MeshImportMethodProbe.cs:56-86` `ReadBackVertexRange`）。库数按 `命令速查.md:258-263` 的既有口径回到 **962**。⇒ 全绿 = 「没验过」变「现在验过」。 |
| 还原 | 无手改还原；但 `Run()` 先 `ClearGenerated()` ⇒ 现存 **1399 个 `.mat` / 250 个 `.asset`** 会被带走再重导（**这是设计**）；跑完 `git status` 只看预期内的产物 diff。⛔ 真出事时的退路 = `DeepCopyMesh` 改往**已有资产** `SetVertexBufferData` 就地重灌，**别退回 `CreateAsset`**。 |
| 预计耗时 | 未实测（导出器全量 + 三步收尾；本清单里最贵的一条，估 ≥15 min，按实测改） |
| 依据 | `资料/普查产出_1018/R-ASSET_资产族现核.md:28,42-47` · `资料/普查产出_1016/W21_EffectExporter确定路径.md:75-81` · `资料/命令速查.md:197,212,258-263,722-725` |

## 2 · `A878` 字体缺字语料 —— 只剩「跑一次覆盖自检」

| 字段 | 内容 |
|---|---|
| 要跑什么 | `-executeMethod TmpSetup.BuildCjkFontAsset -logFile d:/4/_tmp_view/cjk_a878.log`（该入口**内部先跑** `CheckCoverage`，`TmpSetup.cs:160` / `:522`）；随后 `-executeMethod BattleScene.Run`（§14b4c 那 3 条断言） |
| 前置 | 零改动（代码侧已做完：`Core/CardText.cs:642` `foreach (var v in Loc.AllChinese())` 已并进语料；`Core/Loc.cs:1032`）。 |
| 判定 | `cjk_a878.log` 必须出现 `TMPSETUP OK 覆盖检查：语料 N 个不同的字全都能烘出来，无缺字`（`TmpSetup.cs:533-535`）；出现 `语料里有 M 个字**这份字体里没有**` = 红。再 `BattleScene.Run` = **2273/0**（末次基线见 `交接_1018第三会话.md:14`），§14b4c 三条（语料含 `手牌剩余` / `拖到目标上再松手` / `可用 {0}`）全 ✓。 |
| 还原 | 无（重建产物 = `Assets/CardPresentation/Resources/Fonts/NotoSerifCJK-Regular SDF.asset`，Dynamic 模式、盘上 0 字形**是对的**）。 |
| 预计耗时 | 建资产 + 试烘语料：未实测；`BattleScene.Run` 约 **7m56s** |
| 依据 | `资料/普查产出_1018/RS_外壳族8条现核.md:34,95` · `MyGame/Assets/CardPresentation/Editor/TmpSetup.cs:153,160,489-500,522-535` · `MyGame/Assets/CardPresentation/Editor/BattleScene.cs` §14b4c（`CardText.AllChinese()` 三句 `Contains`） |

## 3 · `A950` 战斗侧手牌布局三态对照（**要临时改 `HandLayout.cs` 再还原**）

| 字段 | 内容 |
|---|---|
| 要跑什么 | 同一条 `-executeMethod BattleScene.Run` 跑 **3 次**（①基线 ②态2 ③态3），逐次留不同日志名 |
| 前置（态2） | `Hand/HandLayout.cs:385-390` 的 `IsCompressed(int)` **恒 `return false`** |
| 前置（态3） | `Hand/HandLayout.cs:570-571` 那两行的位移量换成 `0f`（即 `pos.x -= 0f` / `+= 0f`）。⛔ **别改字段本身**（`:195` `selectedCardExtra`）—— 改了 `:10579` 那条「= 2.0 世界单位」会连坐，判别力就糊了 |
| 判定 | 基线应 **2273/0** 且 ⑥c 全绿。**态2 预期红 4 条** = `:10589`（`IsCompressed(8)`）+ `:10590`（`SpacingFor(7) > SpacingFor(12)`，**连坐、属预期**，`SpacingFor` 与 `IsCompressed` 共用判据）+ `:10760`（9 张 `IsCompressed`）+ `:10763`（让位量）。**态3 预期只红 `:10763`**。7 张那两条（`:10728`/`:10736`）三态都该绿。⚠️ 归档原文写「态2 应红 2 条」（`历史/A表批次行_归档_1018.md:57`）与现读算式对不上 ⇒ **按「红在哪几条 + 为什么」判读，别按条数**。 |
| 还原 | 把 `HandLayout.cs` 改回；判据 = `git -C d:/4 diff --numstat -- Unity/MyGame/Assets/CardPresentation/Hand/HandLayout.cs` **为 0**.⚠️ 该文件是**热文件**，改它之前**必须确认没有任何在飞写手**。 |
| 预计耗时 | 3 × ≈8 min ≈ **24 min** |
| 依据 | `资料/待办判据_1018.md:68,198` · `资料/历史/A表批次行_归档_1018.md:57` · `MyGame/Assets/CardPresentation/Editor/BattleScene.cs` ⑥b(2)/⑥c 节 · `MyGame/Assets/CardPresentation/Hand/HandLayout.cs:369-390,555-571` |

## 4 · `A268①` `ClipTmpMesh` 的 `i ≥ 1`（多材质子件）支路走不走得到

| 字段 | 内容 |
|---|---|
| 要跑什么 | 在 `Editor/CardFaceProbe.cs` 里那个 `foreach (var t in root.GetComponentsInChildren<TextMeshPro>(true))` **临时加一列** `mi={t.textInfo.meshInfo.Length}`（或对 `Farseer` 的 `keywords` 单独打一行）⇒ `-executeMethod CardFaceProbe.Run -logFile d:/4/_tmp_view/cfp_a268.log` |
| 前置 | 只有这一行临时探针（该探针**纯 `Debug.Log`、零断言**，不影响任何自检）。⚠️ 该文件是**纯 CRLF**，改动用 Edit、⛔ 别 `sed -i`。 |
| 判定 | 带 `<sprite>` 的那层（`keywords`）`mi ≥ 2` ⇒ **成立** ⇒ `Editor/CollectionScene.cs` 的 `GlyphVertsOf` 只收 `materialReferenceIndex==0` **会静默漏掉图标字形**（A250 块那条断言量不到图标）⇒ 补断言 / 或如实标成自检覆盖缺口。`mi == 1` ⇒ 该支路**今天不可达** ⇒ 记进 `资料/已知的坑.md`、别再加断言。 |
| 还原 | 撤掉那行临时探针（`git diff --numstat` 该文件归零） |
| 预计耗时 | 未实测（`CardFaceProbe.Run` 跑 24 张卡 + 逐层打印） |
| 依据 | `d:/4/项目任务.md:415,541` · `资料/普查产出_1018/S6_A965卡面探针取槽.md:97-105`（本条就是它留给调度台的「只差实跑」）· `MyGame/Assets/CardPresentation/Shell/MenuDraw.cs` 的 `ClipTmpMesh`（按 `ch.materialReferenceIndex` 取槽）· `MyGame/Assets/CardPresentation/Editor/CollectionScene.cs` `GlyphVertsOf` doc（已就地订正前提） |

## 5 · `A964③` 外壳侧 `E1` 实扫几扇窗 —— 🔴 **现核：已经跑过了，这条可销**

| 字段 | 内容 |
|---|---|
| 要跑什么 | **不必再跑**，先读现成读数：`d:/4/_tmp_view/shell.log`（`shell.log` mtime Oct 8 14:10 = 第三会话末次全套）已有 `A964：**扫了 237 颗 / E1 报 0 / E3 报 37**（覆盖 16 个窗类 · 本场景有实例 16 个 · 全库 GameWindow 子类 38 个 · 明细 237 行 → d:/4/_tmp_view/hitprobe/shell_hits.tsv）`，4 条判别式 + 2 条结构断言全 ✓，合计 **965 通过 / 0 失败**。要复核就重跑 `-executeMethod ShellScene.Run -logFile d:/4/_tmp_view/shell.log`（**9 秒**）。 |
| 前置 | 无 |
| 判定 | 合计 965/0；`E1 报 0` **不等于没问题** —— 探针自己写着「E1/E3 零条 ⇒ 先看覆盖率」，而**只有 16 个窗类有实例**（全库 38）⇒ 余下那批今天判不了，要如实记。明细 TSV 238 行在 `d:/4/_tmp_view/hitprobe/shell_hits.tsv`。 |
| 还原 | 无 |
| 预计耗时 | 0（复核 9 s） |
| 依据 | `MyGame/Assets/CardPresentation/Editor/ShellScene.cs` 的 `Section("★ A964：命中区覆盖探针…")` 段（`:5704` 起）· `资料/普查产出_1018第三会话/W_命中区探针_外壳.md`（它自己标「未跑 Unity ⇒ 静态推断」，**已被 `shell.log` 推翻**）· `d:/4/_tmp_view/shell.log:20330-20885` |

## 6 · `F3` 行两条悬案

| 字段 | 内容 |
|---|---|
| 要跑什么 | **第一条已答、直接销**：`_tmp_view/menu.log:13652/13694/13736/13778` 四条「框在原版矩形的**竖**位上（y 差 0.00px ≤ 1.0）」⇒ 8.78/104.92/1.60/1.57px **全在 x 上**。**第二条仍欠**：在 `Editor/MainMenuScene.cs` 那四条 `CheckLeftAlignedAtWorld` 旁补一条量**渲染宽/字号**的断言（写法照本文件 `:4519` 那颗 `Window Title` 的 `TmpRenderedRect`），再跑 `-executeMethod MainMenuScene.Run -logFile d:/4/_tmp_view/menu.log` |
| 前置 | 补那一条断言（**结构上量不到字号**：现行 `CheckLeftAlignedAtWorld` 比的 x 与框左沿**同源**，都从 `WorldW` 缓存推） |
| 判定 | 基线应 **2579/0**（`交接_1018第三会话.md:17`）；新断言断的是「真渲染右缘 ≤ 原版框右沿」（= 渲染宽 ≤ 框宽，写法零假红）。**绿** ⇒ `AlignLeft` 之外没有发散的第二因；**红在 y** ⇒ 真有第二因（`SetAutoFitBox` 收敛字号偏大）⇒ 回头查。 |
| 还原 | 无（只加断言，不改实现） |
| 预计耗时 | **24 s** |
| 依据 | `d:/4/项目任务.md:552` · `资料/普查产出_1018/R-ASSET_资产族现核.md:32-33,57-63` · `资料/普查产出_1017/D3_外壳诊断.md:88-89`（§四·1/2）· `资料/普查产出_1017/F3_外壳修复.md:16,40` |

## 7 · `A848` alpha 裁切「裁得正不正」（影响内接后的整体偏移）

| 字段 | 内容 |
|---|---|
| 要跑什么 | 🔴 **尺子未定，先裁再跑**。两条候选（可先做不需 Unity 的那条）：(a) 离线 —— 量 `Assets/CardPresentation/Resources/Art/cardbacks/*.png` 的**不透明外接框相对图片中心的对称性**，并与原版 `Sprite/*.json` 的 `m_Rect` 比（例 `Cardback_AM_Shield of Humanity_Main` = **707×1020 @ x=158,y=2**，我们导出的 PNG = **707×981** / **707×996**）⇒ 差值就是被裁掉的透明量；(b) Unity —— 在三处内接站点比**渲染矩形中心 vs 框中心**：`-executeMethod DeckScene.Run`（卡背格）+ `-executeMethod CardBaseDemo.Run`（预览/抽屉，四档分辨率 + 出图） |
| 前置 | (a) 要写一个只读 python（PNG alpha bbox + 同名 `Sprite/*.json`），**工程里今天没有这个脚本**；(b) 无前置。 |
| 判定 | (a) 上下裁量 − 下裁量 ≈ 0（左右同理）⇒ 对称；非 0 且 >1px ⇒ 内接后会整体偏 **(Δ/2)**。(b) 渲染中心 vs 框中心偏差 ≤ 1px。**两者都绿**才算「量过了」。 |
| 还原 | 无 |
| 预计耗时 | (a) 分钟级（纯 python）· (b) `DeckScene` 约 **2m06s** + `CardBaseDemo` 约 1 min |
| 依据 | `资料/普查产出_1018第三会话/W_卡面版面.md:113,149` · `资料/普查产出_1018第三会话/调度台_中间账.md:120,170` · `d:/4/项目任务.md:539` · `资料/普查产出_1018第三会话/W_卡背比例.md:96-101`（原版 `m_Rect` 实测表） |

## 8 · `A828④` 35 个 def 会走 `[ERROR] particle system not assigned`（**没跑过**）

| 字段 | 内容 |
|---|---|
| 要跑什么 | 需要一条**播全部效果**的入口（现读：全仓**只有** `Editor/BattleScene.cs:5182-5203` 重置并读 `WFModuleCollisions` 诊断，且**只播一件** `BulletImpact_1shot_trail` ⇒ 现有自检**覆盖不到**那 35 个 def）。⇒ 走 VFX 线那条全量 sweep：`WFSWEEP_SIDE=exp ... -executeMethod EffectSweepBatch.Run -logFile d:/4/_tmp_view/sweep_e.log`（先跑 orig 趟、exp 趟读 orig 的取景缓存，顺序不能反） |
| 前置 | 无代码改动。⚠️ `animfx_modules.json` 已含 `collisionEvent` 数据（`EffectLibraryBuilder.Run` 要跑过才算进运行期）。 |
| 判定 | 日志里 `[ERROR] particle system not assigned to particle VFX`（`WFModuleCollisions.cs:175`）的行数 ≈ **35**（数据侧预测：def 数 656 → **691**，多出的 35 个 `particleSystem` 是空引用），`WFModuleCollisions.MissingParticles` 同值。🔴 **原版也走这一支、那是原版的 LogError 文案 ⇒ 不是缺陷**，这一条只回答「日志量有多大 / 要不要加『每进程只报一次』的限流」（⛔ 限流=改原版行为，先别做）。 |
| 还原 | 无 |
| 预计耗时 | 未实测（全量 sweep 很贵，按 sweep 台账的既有耗时） |
| 依据 | `资料/普查产出_1016/W2_A828碰撞屏震.md:5-8,135-145` · `d:/4/项目任务.md:540` · `MyGame/Assets/WarpforgeVFX/Runtime/WFModuleCollisions.cs:175-176,469,493` · `资料/命令速查.md:215` · `资料/比对基线/README.md:34-43,52-57` |

## 9 · ⚠️ 文档打架：自检宿主到底几份 —— **真缺口是 `DeckScene`，不是 `BattleScene`**

| 字段 | 内容 |
|---|---|
| 「七份」原话 | `资料/可并行任务清单.md:451`（与 `:474` 逐字相同）：`> 🔴 **三条硬约束照旧**：**Unity 与自检只有主对话能跑** · **七份自检宿主**同一时刻**每个只能一个写手** · …`；另 `:508` 写「A167 要**七份宿主全空**」。**这三处都不列举。** |
| 列举在哪 | **`资料/普查产出_1010/S1_下一批切块普查.md:10`** 末列（A167 行）= `Editor/{ShellScene,MainMenuScene,CollectionScene,RewardsScene,ShopScene,SettingsScene,BattleScene}.cs` —— **含 `BattleScene`、缺 `DeckScene`** |
| 「六份」那批 | `可并行任务清单.md:271,284,294,321,357,392` 等都写「六份」并列 `{ShopScene,RewardsScene,ShellScene,MainMenuScene,CollectionScene,DeckScene}` ⇒ **缺 `BattleScene` + `SettingsScene`** |
| 对照 | `CLAUDE.md` 铁律 12 的 **8 份** = `{BattleScene,DeckScene,ShellScene,MainMenuScene,RewardsScene,ShopScene,CollectionScene,SettingsScene}`；`工具/_run_8_checks.sh:25-41` 实跑 **12 条 / 8 个断言宿主**（`CardBaseDemo` 2026-10-17 并入） |
| 结论 | 🔴 **三套口径（6 / 7 / 8）并存**，源因 = 那份清单是 2026-10-0x 写的、`SettingsScene`（联机三条）与 `CardBaseDemo` 后来才并入。⇒ 建议裁定：**以 `CLAUDE.md` 的 8 份为准**，把 `可并行任务清单.md` 那 2 处「七份」+ 6 处「六份」统一改写并**写出列举**（⛔ 不要只改数字）。 |

---

## ① 建议的串行顺序（含理由）

1. **`A964③`（第 5 条）** —— 0 成本：先读现成 `shell.log`，**我判断它已经跑过**（见下节顺手发现 1）；要去复核也只要 9 秒。放最前是因为它可能**直接销掉一条账**。
2. **`A878`（第 2 条）→ `A268①`（第 4 条）→ `F3` 第二条（第 6 条）** —— 三条都只碰**一个**宿主/探针、互不相交，且判据已齐。`A878` 的 `BattleScene.Run` 可以和 `A950` 的基线共用一趟（见 3）。
3. **`A950` 三态（第 3 条）** —— 唯一**要临时改生产文件**（`HandLayout.cs`）的一条 ⇒ 🔴 **必须排在「确认没有任何在飞写手」之后**，且三态跑完立刻还原、用 `git diff --numstat` 核归零。它自带基线那一趟，可与 `A878` 的 `BattleScene.Run` **合并成一趟**（同一入口、同一份日志里两类断言都在）。
4. **`A845(a)`（第 1 条）→ `A828④`（第 8 条）** —— **必须连做**：`A845` 的全量重导正好是 `A828` 那条 sweep 的干净前提；两条都走 `WarpforgeVFX/` 那份池子，中间**不能插别的 Unity 任务**。`A845` 收尾的三步（`RunListed` → `BoosterPackExporter` → `EffectLibraryBuilder`）**一步都不能漏**。
5. **`A848`（第 7 条）** —— **先裁尺子再排**；其中离线那一半（量 PNG alpha 对称性）**不占 Unity、随时可插**，Unity 那一半排在最后。
6. **第 9 条（文档打架）不属于 Unity 腿** —— 纯文档，主对话直接改正本。

## ② 顺手发现的

1. 🔴 **`A964③` 其实已经跑过了（账上是过期记录）**：`d:/4/_tmp_view/shell.log`（第三会话末次全套、mtime `Oct 8 14:10`）含 `A964：扫了 237 颗 / E1 报 0 / E3 报 37（覆盖 16 个窗类 · 本场景有实例 16 个 · 全库 GameWindow 子类 38 个）`，六条判别式/结构断言全 ✓、合计 **965/0**，明细 `hitprobe/shell_hits.tsv` **238 行**。⇒ `W_命中区探针_外壳.md` 里那句「未跑 Unity ⇒ 一切运行时读数都是静态推断」**已被推翻**（铁律 5 该就地订正）。⚠️ 但 **`E1 报 0` ≠ 没问题**：只有 **16** 个窗类有实例、全库 **38** 个 ⇒ 剩下 22 个窗类**今天判不了**，别把它记成「全扫过了」。
2. ⚠️ 同目录 `d:/4/_tmp_view/hitprobe/battle_hits.tsv` 只有 **1469 B**（mtime `Oct 8 15:53`，**晚于**那次全套），而 shell 侧那份 **56 KB / 238 行** ⇒ **战斗侧探针的产出小两个数量级**，值得核一下「是候选本来就少、还是探针只写了很少的行」（可能的假绿）。
3. 🔴 **`F3` 第一条悬案已答、可以销**：`menu.log:13652/13694/13736/13778` 四条 y 差 **0.00px** ⇒ 四个差值**全在水平方向**（= `(框宽−字宽)/2`）。`R-ASSET` 已判「可销」，但 `项目任务.md:552` 那句仍把两条并列 ⇒ 只销前半句。
4. 🔴 **第 9 条的真缺口是 `DeckScene`**（不是任务简报里说的 `BattleScene`）：七份那份列举**含 `BattleScene`**；真正缺的宿主是 **`DeckScene.cs`**（另有「六份」那 6 处缺 `BattleScene` + `SettingsScene`）。
5. ⚠️ `TmpSetup.Corpus()` 的语料**含拉丁字母 + 中英标点**（`TmpSetup.cs:508` 之前那句 `sb.Append("0123456789ABC…")`）⇒ `A878` 的判据认**日志那句「N 个不同的字全都能烘出来」**，别只对中文。
6. ⚠️ **`A950` 的三态会连红，那是设计好的判别力证明**：态2 的连坐来自 `SpacingFor` 与 `IsCompressed` **共用同一判据**（`HandLayout.cs:376`）。跑之前把「预期红哪几条」写下来再跑，否则会把预期红当成真缺陷去查一轮。
7. ⚠️ `EffectExporter.Run` 全量重导有**三处连带**（`RunListed` / `BoosterPackExporter` / `EffectLibraryBuilder`），漏一步就会让 `BattleScene` 的建库断言红 —— 2026-10-01 **亲踩过**，`命令速查.md:258-263` 有原文。
8. ⚠️ `A268①` 的临时探针落在 `Editor/CardFaceProbe.cs`，该文件是**纯 CRLF**（`S6_A965卡面探针取槽.md:110-112` 实测）—— 改动一律用 Edit，⛔ 别 `sed -i`；改完立刻 `git diff --numstat` 核字数。
9. 📌 本清单**没有**覆盖到的两条尾巴（同族但要另排）：`A964 ②` 战斗侧 `E2` 有 **7 行未覆盖**（每条要补一个只读口）· `A985⑥②` 那 19 条 `Cemetery` 文案（在远端）；本条目的范围是简报给的那 8 条 + 文档打架那 1 条。
