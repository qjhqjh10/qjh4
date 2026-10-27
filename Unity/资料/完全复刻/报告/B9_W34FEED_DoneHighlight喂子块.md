# B9_W34FEED · Done Highlight 喂子块加固 + 负例断言 + 两条过期文案（执行报告）

> 日期 2026-10-27 · 白名单 = `Deck/DeckRuntime.cs` · `Shell/BoosterPackOpenWindow.cs` · `Editor/DraftAssertD1.cs`
> + 本报告 + `账/_parts/B9_W34FEED.tsv` · **一行 Unity 没跑、一次 git 没动（只读 `git show`/闸都未碰写）**
> 前置：`报告/B9_W23DRAFT_轮抽接线与预组校验.md` §三（PackScanFeed 口径与四条可粘断言）· `报告/B9_W14FX_特效组件两族.md` §二·3 A · `报告/B9_W20WIRE_特效接线与过期文案.md`（Done Highlight 首接）

---

## 一、结论

**四件全部做完，`TMPDIR=/tmp/wf_w34 bash 工具/typecheck.sh` 收尾 0/0（运行时 0 / 编辑器 0）。**

| # | 简报要求 | 结论 |
|---|---|---|
| **1** | `Done Highlight` 喂的是容器还是子块？ | 🔴 **它喂的是【自己那颗节点】，而且自己就是 `ImageQuad`** ⇒ **改前 `TargetCount` 已经是 1，不是 0**、**本来就没有静默空转**（`Img()` 建单块 `ImageQuad`，`MenuDraw.Nine` 才返回容器 —— 两者不是一个建法）。**仍按 W23 口径加固**：`DoneHlFeed` 逐子件喂（`GetComponentsInChildren<ImageQuad>(true)`，含自身）+ 收不到就 `LogWarning` ⇒ **改后 `TargetCount` 仍 1、行为逐位不变、抗将来变容器**。 |
| **2** | `Deck/` 里其它 toggler 挂载点 | **一处都没有**（`grep -rn "UIEffectTogglerImage\|UIEffectCanvasGroupToggler\|UIEffectTogglerController" Deck/` ⇒ 只有 `Done Highlight` 那一处）。 |
| **3** | `BoosterPackOpenWindow.cs` 同类挂载点 | **零个**（`grep "UIEffect"` = 0 命中）⇒ 只需改两条过期文案（见 5）。 |
| **4** | 负例断言 | ✅ 新增 `DraftAssertD1.A8_PackIsValidNegative`（`Editor/DraftAssertD1.cs:481`），四条口径全落（① 正常包 valid · ② 少一张 invalid · ③ hidden 命中 invalid · ④ `RollRound(cardPool: 只含 2 张的池)` 端到端：坏包不进候选池）。 |
| **5** | 两处过期文案 | ✅ `Shell/BoosterPackOpenWindow.cs:93` 与 `:1255` 就地把「`OpenCardbacks` 用途未查清」改成指针（**零行数变化**：1345 → 1345）。 |
| **6** | 账 4 条 C→A 裁量 | ⚠️ **那 4 条在主表里【已经是 A】**（`B9_W20WIRE` 早已 C→A 并合并，见 `_parts/_merged_B9_W20WIRE`）⇒ **简报「C 行」前提过期**。我**复核确认 A 站得住**（真有挂载点、喂的是自己那颗 ImageQuad），落 `B9_W34FEED.tsv` 给 4 行**追加一条复核备注（状态保持 A）**，⛔ 没有为了数字动状态。 |

🔑 **一句话**：`Done Highlight` **不是** W23 那个 bug 的形状（它是 `Img()` 单块；W23 的坑只在 `MenuDraw.Nine` 容器上）。但「只喂根节点 + `Add` 不递归」这个**风险是真的**，所以我把接线做成**两种建法都吃**（逐子件喂 + 收不到出声），并把「挂不上」从静默变出声。

---

## 二、证据（文件:行号）

### 2·1 【第 1 条核心】改前 / 改后 `TargetCount` 的证据

| 环节 | 判据 |
|---|---|
| `_doneHl` 是怎么建的 | `Deck/DeckRuntime.cs:2477` `_doneHl = Img("foot_hl", "FX_Square_UI_SDF", …)` |
| `Img()` 的建法 | `Deck/DeckRuntime.cs:8551` `ImageQuad.Create(parent, tex, …)` —— **返回的是 `ImageQuad`**，不是容器 |
| `ImageQuad.Create` 把组件加在哪 | `Battle/ImageQuad.cs:124-127`：`var go = new GameObject(name); … var q = go.AddComponent<ImageQuad>();` ⇒ **`ImageQuad` 就在返回的 GO 自己身上** |
| `Capture()` 空表时的自动档 | `Effects/UIEffectTogglerImage.cs:67` `Add(gameObject)` → `:73` `go.GetComponent<ImageQuad>()` ⇒ **命中 `_doneHl` 自己** ⇒ `_quads.Count = 1` |
| `TargetCount` 的算式 | `Effects/UIEffectTogglerImage.cs:82` `_quads.Count + _labels.Count + _graphics.Count`（`Label`/`Graphic` 都取不到）⇒ **1** |
| 场景实据（单块、无子件） | `Scenes/DeckEditor.unity:113519` `m_Children: []`（`foot_hl` 那颗 GO 的子件表为空，`m_Name: foot_hl` 在 `:113501`；4 个组件 = Transform / MeshFilter / MeshRenderer / ImageQuad） |

⇒ **改前 `TargetCount` = 1**（静态三重证据：建法 / 场景子件空 / Capture 逻辑）。
⇒ **改后 `TargetCount` = 1**：`DoneHlFeed`（`Deck/DeckRuntime.cs:8987`）用 `GetComponentsInChildren<ImageQuad>(true)`，**含自身** ⇒ 仍收到 1 块。
⇒ ⛔ 不是 0 ⇒ 这条 A **不是「为了数字好看」，是真有可淡件**。

### 2·2 为什么 Done Highlight ≠ W23 那个 bug（形状差）

| 事 | 判据 |
|---|---|
| `UIEffectTogglerImage.Add` 只 `GetComponent`、**不递归** | `Effects/UIEffectTogglerImage.cs:70-79`（只 `go.GetComponent<...>()`，`foreach` 的是 `fadeTargets` 表，不往子件走） |
| 会踩坑的是「容器 + 九宫子块」 | `MenuDraw.Nine` = `Shell/MenuDraw.cs:1766` `static GameObject Nine(...)`（返回 **`GameObject` 容器**）；`ImageQuad.CreateNineSlice` = `Battle/ImageQuad.cs:564` `static GameObject CreateNineSlice(...)`（同样是容器） |
| W23 的坑就在这里（`Prebuilt Pack UI`） | `Shell/DraftModeWindow.cs:4020` `PackScanFeed` · `:4014-4018` 那段 doc 明写「`MenuDraw.Nine` 返回容器、九宫块全在子件、根上没有 `ImageQuad`」 |
| 😀 Done Highlight 用的是 `Img()` 不是 `MenuDraw.Nine` | 见 2·1 第一、二行 ⇒ **不中这一枪** |

### 2·3 我们改了哪几行（改后真行号）

| 位置 | 内容 |
|---|---|
| `Deck/DeckRuntime.cs:8874` | 新字段 `UIEffectTogglerImage _doneHlTg`（判「那颗 toggler 还在不在控制器表里」，`Build()` 重建 `foot_hl` 后失效即自动重接线） |
| `Deck/DeckRuntime.cs:8890-8899` | `ToggleDoneHighlight(bool on)` 改成：目标不在控制器表里就 `SetupDoneHighlight()`；否则 `Toggle`；再否则硬 `SetActive` 兜底 |
| `Deck/DeckRuntime.cs:8956-8982` | `SetupDoneHighlight()`：加 toggler → `DoneHlFeed` 逐子件喂 → 控制器 `Rescan()` → **收不到就 `Debug.LogWarning`**（⛔ 不静默） |
| `Deck/DeckRuntime.cs:8987-8996` | `DoneHlFeed(tg, target)`：`GetComponentsInChildren<ImageQuad>(true)` + `RefreshTargets()`；口径与 `DraftModeWindow.PackScanFeed` **同一条**（⛔ 不另发明） |
| `Deck/DeckRuntime.cs:4869` | 调用点**未动**（仍是 `ToggleDoneHighlight(err == DeckError.None)`，W20WIRE 落的） |

### 2·4 断言（第 4 条）

| 位置 | 内容 |
|---|---|
| `Editor/DraftAssertD1.cs:84` | 调用 `A8_PackIsValidNegative(packs)`（**与 `A7` 同一行** —— 零行数变化，不动账里 `DraftAssertD1.cs:153` 那条散文引用） |
| `Editor/DraftAssertD1.cs:481-…` | `A8_PackIsValidNegative`：硬前提（卡池 ≥3 张 → 找一条 3 张卡互不相同且都在池里的 enabled 真包 → 拿到 2 张真卡）**先断**，再断 ① ② ③ ④ |
| `:506-507` | ① `ResolvedCardCount(three)==3` 且 `PackIsValid(three,3,null)` 为真 |
| `:512-513` | ② `ResolvedCardCount({a,b,NO_SUCH})==2` 且 `PackIsValid(…,2,null)` 为假（条件①） |
| `:517` | ③ `PackIsValid(three,3,{three[1]})` 为假（条件②） |
| `:529-546` | ④ 端到端：临时把包表换成**只含这条包**（让结果确定）→ 全池 ⇒ 恰好给出这 1 个包；**只含 2 张卡的池 ⇒ 返回 0 个**（证明 `RollRound` 真调了 `PackIsValid`）；`finally` 还原真表 |
| `:551` | 文件总行数 472 → 551（+79，全部在 `:471` 之后 + `:84` 同行改写；`:153` 未动） |

> ④ 为什么用「合成单包表」而不直接拿真 882 包表跑：真表下「抽中哪 3 个包」由 `System.Random` 决定、**不可控**；合成单包表把「好包在 / 坏包不在」变成**确定的两条断言**（正例 + 负例），比「真表里恰好没抽到」强。简报原话「`cardPool: 只含 2 张卡的池` 端到端」的口径**保留了**（`RollRound(12345, 0, bp.army, 1, null, onlyTwo)`）。

### 2·5 两条过期文案（第 5 条）

| 位置 | 改后（零行数变化） |
|---|---|
| `Shell/BoosterPackOpenWindow.cs:93` | 「✅ `OpenCardbacks` 那条 clip **不接是【对的】** —— 已查清（2026-10-27 `B9_W34FEED` 订正「用途未查清」）= `DeckGeneralInfoDemo.cardbacksAnimation`（0.25 s · Once · 卡背两层入场），消费者 = `Shell/PracticeModePopup.BuildCardbacks`」 |
| `Shell/BoosterPackOpenWindow.cs:1255` | 运行期日志 ⑥ 同步改成「已查清 = `DeckGeneralInfoDemo.cardbacksAnimation`，0.25 s · Once · 消费者 `PracticeModePopup.BuildCardbacks`，与开包窗无关」 |

判据（现读，不转抄）：`Shell/BoosterPackAnim.cs:12`（W23 已订正同一句）· `Shell/PracticeModePopup.cs:3586/3596/3675`（`cardbacksAnimation` 的消费与 0.25 s）· `资料/完全复刻/调度台日志.md:6001`（两层、`m_WrapMode=1` Once、内层 `(0.3,0.3)→(11.2012,4.9016)`、两层 z 转到 +1.60° / 相对 −3.43°）。

### 2·6 账（第 6 条）

- `B9_W34FEED.tsv` = 11 列表头 + **4 行**（`UIEffectTogglerImage` 的 4 个方法），**状态保持 A**，每行 `备注`（末列）追加一条 W34FEED 复核说明。
- **现读依据**：这 4 行在 `账/特效动画.tsv:462-465` 里**状态列已经是 `A`**（`B9_W20WIRE` 的补丁已合并到 `_parts/_merged_B9_W20WIRE/`，主表已反映）。
- `python -I -X utf8 工具/check_patch_conflicts.py` ⇒ **✅ 没有跨补丁重复键（1 份补丁）**。
- 格式校验：11 列 × 5 行（含表头）· LF · 无 BOM · 无裸 CR · `方法` 全带签名（`AnimationFinished()` / `DoUpdate(float currentTime, float normalizedTime)` / `Toggle(bool option, float duration)` / `ToggleGameObjects(bool option)`）。

---

## 三、没查清的部分（⛔ 都是「查不到 + 还差什么」，没有猜）

1. **`TargetCount` 只是【静态三重推算】，不是运行时实测**（本批禁跑 Unity）。三条静态证据（建法 / 场景 `m_Children: []` / `Capture` 逻辑）互相印证，但**没有一次 `Debug.Log(TargetCount)`**。若同步点允许跑一次宿主，`DeckScene.Run` 里加一句打印即可坐实。
2. **运行期淡入观感未验**（禁跑 Unity）：`deactivateOnDisable=1` 下「淡出走完才 `SetActive(false)`」这一拍在真机上是 0.3 s；编辑/批处理由控制器强制 `duration=0` 瞬间档（`Effects/UIEffectTogglerController.cs:57`）⇒ **与改前硬切等价**，既有断言（`Editor/DeckScene.cs:6871/6885/7004`）应照过，但**没实际跑**。
3. **原版 `Done Highlight` 的控制器挂在哪一级祖先** —— 仍未查到（沿用 W14FX/W20WIRE 的 `_doneHl.transform.parent`，已按铁律 18 标 `🔶`）。
4. **`A8` ④ 没在真 882 包表上跑**（理由见 2·4 末段）—— 用「合成单包表的正例 + 负例」替代，语义等价但没覆盖「真表里 882 个包逐一过滤」的规模。
5. **`A8` 断言本身没执行过**（禁跑 Unity）—— 只保证 `typecheck 0/0`（编得过）与静态逻辑自洽（`RollRound` 的过滤代码 `Shell/DraftPacks.cs:358-366` 现读）。

---

## 四、改动清单

| 文件 | 改动 | 行数变化 | 行尾 |
|---|---|---|---|
| `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs` | ①`:8874` 新字段 `_doneHlTg` ②`:8890-8899` `ToggleDoneHighlight` 改「不在表里就重接线」③类尾 `SetupDoneHighlight` / `DoneHlFeed`（逐子件喂 + 收不到出声） | 8957 → **8998**（+41，全部在 `:8874` 之后 ⇒ 账里最大落点 `:8862` 不动） | **CRLF 保持**（CRLF = LF = 8998，裸 CR = 0） |
| `MyGame/Assets/CardPresentation/Shell/BoosterPackOpenWindow.cs` | `:93` / `:1255` 两条过期文案就地改写（`OpenCardbacks` 用途已查清 = 指针） | 1345 → **1345**（**0**） | LF 保持 |
| `MyGame/Assets/CardPresentation/Editor/DraftAssertD1.cs` | ①`:12` 文件头「两条额外交叉校验」②`:84` 同行加 `A8_PackIsValidNegative(packs)` ③类尾 `A8_PackIsValidNegative`（四条负例 + 合成单包表端到端） | 472 → **551**（+79；`:153` 及之前**零变化**） | LF 保持 |
| `资料/完全复刻/账/_parts/B9_W34FEED.tsv` | **新建**：表头 + 4 行（`UIEffectTogglerImage` × 4 方法，状态 A，备注追加 W34FEED 复核） | 5 行 | LF · 无 BOM · 无裸 CR |
| `资料/完全复刻/报告/B9_W34FEED_DoneHighlight喂子块.md` | 本文件 | —— | —— |

**没动的（守所有权）**：`Effects/UIEffectTogglerImage.cs`（已定稿，只调用）· `Effects/UIEffectTogglerController.cs` · `Shell/DraftModeWindow.cs`（W8）· `Shell/DraftPacks.cs` · `Shell/PrebuiltDecks.cs` · `Battle/**` · `RuleEngine/**` · `Shell/{CollectionWindow,CampaignTab,ForgeTab,CardDetailPopup,CardDisplayEffects,RankedDivisionChangeWindow}.cs` · `工具/*.py` · 一切 `*.unity` · `ProjectSettings/` · 账主表 · 基线文件 · `CLAUDE.md` / `项目任务.md` / 各线任务总表 / 别人的报告。

**验证**：`TMPDIR=/tmp/wf_w34 bash d:/4/Unity/工具/typecheck.sh` 共跑 3 次（改完 DeckRuntime / 改完 DraftAssertD1 / 改完 BoosterPackOpenWindow），**每次都 0/0**。
**账漂移**：`python -I -X utf8 工具/check_ledger_points.py` 现读 —— **我的编辑点（≥`DeckRuntime.cs:8874`）之后没有任何 col-6 落点**（最大落点 `:8862`），⇒ **本批造成的漂移 = 0**。主表里 DeckRuntime 的落点**统一 +3**（如 `SayError` 账 8859 / 实 8862）是**我动手之前就有的**（`Deck/` 还有 W31 也在动，或更早批次），⛔ 不是我造成的；同步点 `--fix` 一次即可。

---

## 五、顺手发现

1. **🔴 简报「4 条 C 行」的前提过期**：`账/特效动画.tsv:462-465` 的 `UIEffectTogglerImage` 4 行**状态已经是 `A`** —— `B9_W20WIRE` 首接时就 C→A 并已合并（`_parts/_merged_B9_W20WIRE/` 在位）。我的裁量结论 = **A 站得住**（不是空转），已用备注形式落到 `B9_W34FEED.tsv`，**没有重复改状态**。请调度台知悉：若另有便签仍记「这 4 条是 C」的，需要按本条订正。
2. **`FootShine` 那一段上移了 10 行**：`Deck/DeckRuntime.cs` 的 `void FootShine` 从 `:8929`（账里 `特效动画.tsv:455` 备注引用的旧号）**现在是 `:8919`**；`WildcardShineLayer _footShine;` 从 `:8926` → `:8916`。那两处引用都在**col 7 差异 / col 10 备注**（散文），**不参与闸**，但同步点可顺手订正。
3. **`_doneHlCtl` 挂在 `Root` 上 ⇒ `Rescan()` 会收整个窗口子树的 toggler**：今天卡组编辑窗里**只有 1 个** `UIEffectTogglerBase`（Done Highlight）⇒ 无害；**若将来窗口内新增第二个 toggler**，它会被 Done Highlight 的控制器**一起 `Toggle`**（静默多动一个）。更稳的做法是给 Done Highlight 一个**专用子宿主**挂控制器，但那会偏离 W20WIRE 的 `🔶 parent` 选择 ⇒ **本批不改，记在这里**，等真有第二个 toggler 再收口。
4. **`DoneHlFeed` 与 `DraftModeWindow.PackScanFeed` 现在是两份同口径实现**（一个 `private static` 在 `DraftModeWindow`、一个在 `DeckRuntime`）。按「两处写同一条规则 = 迟早不一致」的口径，长远应收口到一个共用静态工具（如 `Core/UiTogglerFeed.cs`）。本批**不新开文件**（白名单外）⇒ 记在这里，交给调度台决定。
5. **`BoosterPackOpenWindow.cs` 里确实没有任何 toggler 挂载点**（`grep -i "UIEffect"` = 0 命中；只有 `BlinkGraphic`（`:363`）与 `AnimateUIMaterialProperty`（W20WIRE 接的背景抖，非 toggler 族））⇒ 简报第 3 条「若同类挂载点」**不成立**，无需接线。
6. **`A8` 用了 `DraftPacks.SetPacksForTest` 临时换表**（`:534` 处），并在 `finally` 还原 —— 因为它会改**进程级** `_cache`，若将来有人在 `A8` 之后加断言会读到合成表。今天 `A8` 是 `Run()` 里**最后一条**，且 `Run` 的 `finally` 还会 `InvalidateCache()` ⇒ 安全；**以后若在 `A8` 后面新增断言，务必知道这里换过表**。
