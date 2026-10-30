# B102_D2 —— `ShellScene` 复跑后红的完整分诊（只读诊断，2026-10-30）

> 触发：第一百零二会话把 `Editor/ShellScene.cs:11023` 上「被行末 `//` 吞掉」的 20 个断言类搬回可执行位置，
> 它们**从未执行过**。复跑 = **3251 通过 / 6 失败**（另有 `B98A5`/`B98A8`/`MINI2` 用自带计数器，不并进总合计）。
> **结论：8 条 α（断言错）· 1 条 δ（夹具）· 2 条 β（真缺陷）。**

## 一、断言侧（α / δ）—— 可直接改，已派 `B102_RED5`

| # | 失败 | 分类 | 根因 | 证据 |
|---|---|---|---|---|
| 1 | `SupportMethods.ModifyLocalization` 转调到 `CardIcons.RewriteMarks`（实测 call 点 = -1） | **α** | opcode 表取错类：`typeof(OpCode)` 公开静态字段 **0 个**（常量在 `OpCodes`，226 个）⇒ 字典恒空 ⇒ `CountCalls` 第一条指令就返 `-1`。**这两条从写下那天起就不可能绿**，只是从没被接线执行过 | `B97_DAILY_Assert.cs:246`；dotnet 8 实测 `OpCode`=0 / `OpCodes`=226 |
| 2 | 四组样本两个入口都能跑 | **α** | 把 `AvailableLanguages` 枚举当 `bool` 传给 `RewriteMarks(string,bool)` ⇒ `Invoke` 抛 `ArgumentException` | `B97_DAILY_Assert.cs:104-105`；`Core/CardIcons.cs:647` |
| 2b | （连带）「两入口逐样本同值」 | **α** | `catch { break; }` 后 `allSame` 保持初始 `true` ⇒ **空转绿灯** | `B97_DAILY_Assert.cs:107/112` |
| 3 | `InitializeMissionToast` 恰好一处转调 | **α** | 同 #1 的同一个 `CountCalls` | `B97_DAILY_Assert.cs:284-331` |
| 4 | `MINI2 A5 前提：Cards 页建出来了` | **δ** | 夹具只 `CollectionWindow.Create(mgr)`、没 `Open()` ⇒ `tabs` 空、`_pages[1]` 仍 null | `B97_MINI2_Assert.cs:90-93`；`Shell/CollectionWindow.cs:3412/3797/3856-3863`；先例 `Editor/CollectionScene.cs:862-863` |
| 5 | 官方词条表里有 `Armies/Sautekh_short` | **α** | 用了 `Loc.HasEntry`（**只认手写 `Table`**）；该键**在** `official_loc_13lang.json`（10,862 键）里 | `B98A5_Assert.cs:94`；`Core/Loc.cs:3593-3608`（`HasEntry` 的 doc 自己就写「≠ 运行时没有文案」）、`:3445-3480`（`Loc.T` 第三档会兜到） |
| 6 | 官方词条表里有 `MenuShop/Alpha/AllArmies` | **α** | 同上（该键也在官方全表） | 同上 |
| 10 | `B101R8 A1 整格缩放 = 3.4988` | **α** | 期望常数错：`Content` 是 `anchorMin(0,0)/anchorMax(1,1)/sizeDelta(0,0)` 的**拉伸节点** ⇒ 宽 = 根宽 = `0.66×1080 = 712.8`（不是 712）；我们的实现用的正是 712.8 | `B101R8_Assert.cs:51`；prefab `Card Drawer` 的 ARF `m_AspectMode 2 / m_AspectRatio 0.66`；`Shell/ItemDrawerLayout.cs:59` |
| 11 | `B101R8 A2 frameK 线性` | **α** | 同一常数（半格 = 1.75138） | `B101R8_Assert.cs:75` |

## 二、真缺陷（β）—— 需要 Unity 才能定性，**两个都出在卡包窗**

### ① `armyTexts` = 0（`B98A5_Assert.cs:202`）
- **根因（已坐实）**：卡包 prefab 里两颗 `Army Text` 的 TMP 组件 `m_Script` 指向**外部 CAB `CAB-17b1b5f8…`**，而我们所有 StreamingAssets bundle 里**都没有那个 CAB** ⇒ 运行时是 **missing script** ⇒ `GetComponentsInChildren<TextMeshPro>()` = 0（日志里 `[BoosterPack] … texts=0` 即此）。
- **两条修法**：
  ① 让 `BuildVisualItemLayer` 在那两颗节点上**自己建世界空间 TMP**（节点本身在树上、rect 可读；字体材质 `Pragati-Regular Atlas Material` 已随包导入）——按铁律 18 标 `🔶 自己设计`；
  ② 把那个 script CAB 并进产物（改 `--booster-mats` 那个打包工具）。
- ⚠️ 同文件 `:203` 用 `if (texts != null && texts.Length == 2)` ⇒ **前提不成立时静默跳过 3 条**（该文件头自己写着「⛔ 不用 `if(前提) Check(结论)`」）。

### ② 粒子 `renderQueue` 只有 10/17 提到 `QCard`（`B98A5_Assert.cs:287-294`）／`B98A8_Assert.cs:125-137` 同源
- **根因（静态坐实）**：Leviathan 子树 17 颗 PSR 中，`m_Materials[0].m_FileID ∈ {11,12,13}`（= 并进来的兄弟 CAB `CAB-boostermatsdup/bs/menu`）的**恰好 7 颗**，正是红的那 7 颗；`= 0`（主 CAB）的**恰好 10 颗**，正是绿的 10 颗。
  `LiftPackParticles`（`Shell/BoosterPackOpenWindow.cs:1579-1591`）对 `sharedMaterial == null` 是 `continue` ⇒ 停在 3000。
- **两种待定机制**（不跑 Unity 判不了，如实标）：
  ① 这 7 份材质本身在运行时解不出（跨内层 CAB 的 PPtr 未闭合）；
  ② 材质解出了、但它的 `m_Shader`（externals 指 `CAB-boosterfxshaders`）解不出 ⇒ 材质无 shader ⇒ 设队列被拒。
  任一种下**粒子都画不出来**，比「队列偏低」更严重。
- **定性一条命令**：把从未接线、0 次执行的 `Editor/B97W32_Assert.cs` 接进 `ShellScene` 聚合行（它第 ② 段正是「材质槽 null = 0 / shader 坏 = 0」）；或临时在 `LiftPackParticles` 里分三档出声（`sharedMaterial==null` / `shader==null` / 其余）。
- ⚠️ 顺手：`B98A8_Assert.cs:134` 把「材质为 null」与「队列偏低」**合并判** ⇒ 分不清是哪种，应拆成两条。

## 三、其它顺手发现
- `B98A5_Assert.cs` 头部「灭自证」第 1 条说「`Armies/Sautekh` 官方表里没有」——**实测有**，该说明已过期。
- `B97_DAILY_Assert.cs:112`「两入口逐样本同值」在循环异常时**恒真**（空轮绿灯）—— 正是本轮「一条红 + 一条绿」并存的原因。

## 四、判据出处
- `d:/2/tools/decomp_full/SupportMethods__ModifyLocalization.c` · `ToastNotificationMission__Initialize.c`
- `bundle_wf_boosters_extra` 的 MB/PSR 逐颗实读（`m_Script` / `m_Materials[].m_FileID` / externals）
- `资料/完全复刻/报告/B97_W32.md` §三(3.1)·§四（它自己写着「多 CAB 产物能不能在 Unity 上 `LoadFromFile`/渲，**未验**」）
