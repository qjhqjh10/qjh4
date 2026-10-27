# B9_W23DRAFT · 轮抽接线与预组校验（三件收口）执行报告

> 日期 2026-10-27 · 域 = `账/卡组编辑.tsv`（另涉 `轮抽.tsv` 的行号漂移）· 白名单 = `Shell/DraftPacks.cs` · `Shell/DraftModeWindow.cs` · `Shell/BoosterPackAnim.cs`
> + 本报告 + `账/_parts/B9_W23DRAFT.tsv` · **一行 Unity 没跑、一次 git 没动**
> 前置：`报告/B9_W8PREB_预组与轮抽C.md`（§四 R1 · §五 · §六）· `报告/B9_W14FX_特效组件两族.md`（§二·3 补丁 B）

---

## 一、结论

**三件全部做完，`typecheck` 0/0（运行时 0 / 编辑器 0）。** 过程中挖出 **3 条会让人做错的东西**，都已就地处置：

| # | 事实 | 影响 | 处置 |
|---|---|---|---|
| **A** | **补丁 B 的字面写法 `tg.fadeTargets.Add(hl)` 在我们这边会【静默空转】** —— `MenuDraw.Nine` 返回的是**容器节点**（`ImageQuad.CreateNineSlice` 先 `new GameObject(name)`，九宫的那些 `ImageQuad` 全在**子件**上），而 `UIEffectTogglerImage.Add` 只 `GetComponent`、**不递归**（= 原版 `Image[] images` 的口径）⇒ `TargetCount == 0`、`Toggle` 什么也不做 | 照抄 = 「接好了」但画面毫无变化，且日志干净 | 新增 `PackScanFeed`：把 `Highlight` 底下的 `ImageQuad` **逐个**喂进 `fadeTargets`，收不到就 `Debug.LogWarning` |
| **B** | **`B9_W8PREB` 记的「shiny = Coffee `UIEffect Shiny` 的 shader，等 K-13」不成立。** 那个字段是 `UIEffectTogglerController`（`+0x60`，`dump.cs:107070`），它驱的是本节点上的 `UIEffectTogglerImage`（**纯 α 写入、不依赖任何 shader**） | 这条一直挂着「等 K-13」= 一条假的资源依赖 | 三拍 `Toggle` 按方法体接上：`OnEnable → false/0`、`SelectAnimDummy → true/0.3`、飞行 `OnStart → false/0.4`；`OpenAnim` / `CloseAnim` **全文无 Toggle**（⛔ 别补） |
| **C** | `images[0]` 的归属我**自己现读 prefab 复核**过（不是只信二手）：toggler `MonoBehaviour_-535494133076742670.json` 的 `images[0]` = PathID `-8333113853195211278` = GO `3832911137508339186`（**`Highlight`**）上的 `Image`；`deactivateOnDisable = 1`（与我们基类默认一致） | 目标节点判错 = 淡错对象 | 按 `Highlight` 挂 |

三件逐条：

1. **`卡组:290` 的调用点已接** —— `DraftPacks.RollRound` 在 `cand.Add(p)` **之前**逐包过 `PrebuiltDecks.PackIsValid(p.cardIds, ResolvedCardCount(p.cardIds, pool), null)`；过不了先出声再 `continue`。⛔ 判据**只写这一处**（`DraftModeWindow` 没有第二次判断）。
2. **`Prebuilt Pack UI` 的扫光已接** —— 根上 `UIEffectTogglerImage + UIEffectTogglerController`，`fadeTargets` 逐个喂子块，`RefreshTargets()` 与 `Rescan()` 都在，初始态 = 原版 `OnEnable` 首句；选中时副本上 `Toggle(true)` → 飞起那一拍 `Toggle(false, 0.4)`。
3. **`Shell/BoosterPackAnim.cs` 文件头已订正（零行数变化）** —— `OpenCardbacks.anim` 改成指针（0.25 s · Once · 卡背两层入场 · 消费者 = `PracticeModePopup.BuildCardbacks`）。**cue 核对结果**：本文件**一条 cue 常量都没有**（简报里「本文件那几条 cue」实际不存在）；`WindowSounds.cs:245-248` 那四条**全部已在 `DraftModeWindow` 接上**（`:2008` / `:3831` / `:3944` / `:3952`）⇒ 无需补。

---

## 二、证据（文件:行号）

### 2·1 原版判据（🥇 `.c` ＞ 🥈 ILSpy）

| 事 | 判据 |
|---|---|
| `OnEnable` 首句关扫光 | `decomp_full/PrebuiltPackUI__OnEnable.c:1-37`（`uiEffectTogglerController.Toggle(option: false, 0f)`）· 🥈 `cpp2il/out_cpp2il_dev_ilspy/PrebuiltPackUI.cs:93` |
| `OnDisable` 同样关 | `PrebuiltPackUI__OnDisable.c` · 🥈 `…PrebuiltPackUI.cs:144` |
| `SelectAnimDummy` ④ 开扫光（默认 0.3 s） | `decomp_full/PrebuiltPackUI__SelectAnimDummy.c:59` = `UIEffectTogglerController__Toggle(*(+0x60), 1, DAT_1834b2dc8, 0, 0)` · 🥈 `…PrebuiltPackUI.cs:386-388` |
| ⑤ 飞行 `OnStart` 关扫光（0.4 s） | `…__SelectAnimDummy.c:65` = `*(undefined4 *)(lVar2 + 0x18) = 0x3ecccccd`（= 0.4f，即 `sequenceStep2AnimTime`）· 🥈 `…PrebuiltPackUI.cs:400` |
| `OpenAnim` / `CloseAnim` **没有** Toggle | `PrebuiltPackUI__OpenAnim.c`（83 行）· `PrebuiltPackUI__CloseAnim.c`（30 行）两份全文扫过，**零** `UIEffectTogglerController__Toggle` |
| 字段类型与偏移 | `d:/2/tools/il2cpp_out/dump.cs:107070` = `private UIEffectTogglerController uiEffectTogglerController; // 0x60` |
| `IsValid()` 的活调用点 | `Everguild.LiveOps/DraftEventBase.cs:378`（三截：`packEnabled && packArmy == warlord.cardArmy && pack.IsValid()`） |
| prefab：`images[0]` 是子件 `Highlight` | `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-535494133076742670.json`（`deactivateOnDisable: 1`；`images[0].m_PathID = -8333113853195211278`）→ `MonoBehaviour_-8333113853195211278.json`（其 `m_GameObject.m_PathID = 3832911137508339186`，`m_Color = (1, 0.558, 0.078, 1)`）→ `GameObject/Highlight_3832911137508339186.json`（`m_Name = "Highlight"`） |

### 2·2 我们的落点（改后的**真行号**，账补丁按这些写）

| 文件:行 | 是什么 |
|---|---|
| `Shell/DraftPacks.cs:336` | `RollRound(...)` —— 新增可选形参 `IList<CardDef> cardPool = null`（**只为夹具/自检留口**，产品路径传 `null`） |
| `Shell/DraftPacks.cs:358-368` | 候选循环里的那一句 `ResolvedCardCount` + `PackIsValid` + 出声 |
| `Shell/DraftPacks.cs:474` / `:483` | `ResolvedCardCount(string[])` / `ResolvedCardCount(string[], IList<CardDef>)`（后者 = 造负例的口） |
| `Shell/DraftPacks.cs:441-470` | `_cardPool` / `CardPool()` / `InvalidateCardPool()`（卡池缓存，**与包表缓存 `InvalidateCache` 是两件事**） |
| `Shell/DraftModeWindow.cs:2434` | `var hl = MenuDraw.Nine(… "Highlight" …)`（返回值接住） |
| `Shell/DraftModeWindow.cs:2494` | `RegisterPackUi(content, selNode, index, hl)` |
| `Shell/DraftModeWindow.cs:3765` / `:3769` | 签名加形参；`if (index == 0) { _packUis.Clear(); _packTogglers.Clear(); }`（原地扩，不推下游行号） |
| `Shell/DraftModeWindow.cs:3781-3782` | `_packUis.Add(ui)` → `RegisterPackScan(ui, highlight)` → `PlayPackOpenAnim`（顺序 = 原版 `OnEnable` 早于 `Initialize/OpenAnim`） |
| `Shell/DraftModeWindow.cs:3923` | `PackSelectFly`（④ 在 `:3937-3938`、⑤ 在 `:3944-3947`） |
| `Shell/DraftModeWindow.cs:3974` | `PackScanFade = 0.3f`（原版 `Toggle` 的形参默认值） |
| `Shell/DraftModeWindow.cs:3979` | `_packTogglers`（1:1 与 `_packUis` 同序；目前只是句柄表） |
| `Shell/DraftModeWindow.cs:3992` / `:4020` / `:4038` / `:4056` | `RegisterPackScan` / `PackScanFeed` / `PackScanToggle` / `PackScanRetarget` |
| `Shell/BoosterPackAnim.cs:12` | `OpenCardbacks.anim` 那一行（**1 行改写 · 文件总行数不变 353**） |
| `Shell/WindowSounds.cs:245-248` | 四条 cue 常量（本批**没改**，只核对调用点） |

---

## 三、没查清的部分（如实记，⛔ 不拿猜测填）

1. **`卡组:290` 的负例断言没落地。** 现池 882/882 全 valid ⇒ 这条过滤今天恒 true、不造负例验不出。而断言宿主 `CardPresentation/Editor/DraftAssertD1.cs` **不在本代理白名单**（红线段 4）⇒ 我只把夹具口开出来（`RollRound` 的第 5 个实参 / `ResolvedCardCount(ids, pool)`），**没写断言**。可直接粘贴的口径：
   ```csharp
   // 前提（硬检查，先断再测）
   var pool = CardDatabase.Load();
   Ok(pool != null && pool.Count >= 3, "前提：卡池读得到");
   var three = 前三张 unit 卡的 id;                       // 同 D2/D3 现有夹具的取法
   // ① 正常包 ⇒ true
   Eq(DraftPacks.ResolvedCardCount(three), 3, "① 三张都查得到");
   Ok(PrebuiltDecks.PackIsValid(three, 3, null), "① 正常包 valid");
   // ② 少一张 ⇒ false（条件①）
   var two = new[] { three[0], three[1], "NO_SUCH_CARD_ID" };
   Eq(DraftPacks.ResolvedCardCount(two), 2, "② 查不到的 id 不计入");
   Ok(!PrebuiltDecks.PackIsValid(two, 2, null), "② 少一张 ⇒ invalid");
   // ③ hidden 命中包内任一张 ⇒ false（条件②）
   Ok(!PrebuiltDecks.PackIsValid(three, 3, new[] { three[1] }), "③ hidden 命中 ⇒ invalid");
   // ④ 端到端：把「只含 2 张卡的池」当 cardPool 传进去，坏包就不该出现在这一轮
   var onlyTwo = only the first two卡;                    // 同一个包 3 张卡里第 3 张不在池里
   var roll = DraftPacks.RollRound(12345, 0, army, 3, null, /*cardPool:*/ onlyTwo);
   Ok(roll.TrueForAll(p => p.packId != badPack.packId), "④ 坏包不进候选池");
   ```
   ⚠️ 现成的 `DraftWindowAssertD2` / `DraftAssertD3` 里的合成包都用**真卡 id**（`CardDatabase.Load()` 里挑 unit）⇒ 新过滤**不会**把它们打掉（我逐个核过）；`D2` 那条 `cardIds = new string[0]` 的「异阵营」包本来就按阵营被筛掉，行为不变。
2. **静止态的橙色描边会被 `Toggle(false, 0f)` 关掉**（`SetActive(false)`）—— 那是原版 `OnEnable` 首句同款（`Toggle` 的瞬间档直接 `ToggleGameObjects(option)`，与 `deactivateOnDisable` 无关），**但真机外观没验**（本批禁跑 Unity）。若实机看着「包卡少了橙描边」而原版有 ⇒ 回头复核 `images[0]` 归属（判据链见 §2·1 最后一行，随时可重放）。
3. **另一个实例 `Done Highlight`（卡组编辑）仍未挂** ⇒ `特效动画.tsv` 里 `UIEffectTogglerImage` 那 4 条 C 行**是否升 A，我不敢自己裁**：现在只有 2 个 prefab 实例里的 1 个有挂载点。请调度台定（W14FX 的原话是「C 的唯一原因是零挂载点」）。
4. **影子副本的 `Instantiate` 引用重映射没验** —— 我们已**显式**把副本的 `fadeTargets` 重指到副本自己的 `Content/Highlight`（`PackScanRetarget`），所以不依赖它；但「原版确实靠重映射」这一步只是静态推断。
5. **飞行那 0.4 s 里，扫光和 `PackFade` 同时写同一批 quad 的 α**（两条都 1→0，同向；逐帧谁后写谁的）。批处理没有帧循环 ⇒ **没逐帧验**。若真机看到描边闪烁，改法是把 Highlight 那几块从 `PackFade` 的 `quads` 里排掉（照它已排 `CardView` 子件的做法）。
6. **`_packTogglers` 目前没有消费方**（除 1:1 清理外）—— 它是补丁 B 要求的句柄表，也是将来「悬停进/出」那两拍要用它的地方（那两拍原版**没读到**：`OnEnable`/`OnDisable`/`SelectAnimDummy` 之外的 Toggle 调用点，`grep` 全类只有那三处）。

---

## 四、改动清单

| 文件 | 改动 | 行数变化 |
|---|---|---|
| `MyGame/Assets/CardPresentation/Shell/DraftPacks.cs` | ① `using RuleEngine;` ② `RollRound` 加可选形参 + 候选循环里那条 `IsValid` 过滤（含出声）③ 类尾 `_cardPool` / `CardPool()` / `InvalidateCardPool()` / `ResolvedCardCount` ×2 | 429 → **493**（**+64**） |
| `MyGame/Assets/CardPresentation/Shell/DraftModeWindow.cs` | ① `BuildPackCard` 接住 `Highlight` 并传给 `RegisterPackUi` ② `RegisterPackUi` 里挂扫光（`RegisterPackScan`）+ 顺带清 `_packTogglers` ③ `PackSelectFly` 接 ④⑤ 两拍 + 订正那段「等 K-13」的 doc ④ 类尾追加 `PackScanFade` / `_packTogglers` / `RegisterPackScan` / `PackScanFeed` / `PackScanToggle` / `PackScanRetarget` | 3963 → **4067**（**+104**） |
| `MyGame/Assets/CardPresentation/Shell/BoosterPackAnim.cs` | **文件头 1 行**（`OpenCardbacks.anim` 从「用途未查清，不接」改成指针） | 353 → **353**（**0**，红线：零行数变化） |
| `资料/完全复刻/账/_parts/B9_W23DRAFT.tsv` | **新建**：2 条数据（`卡组:290` / `卡组:303`）+ 11 列表头 · LF · 无 BOM · 无裸 CR · `方法` 全带签名（`check_patch_conflicts.py` ⇒ ✅ 无跨补丁重复键，3 份补丁） | 新文件 |
| `资料/完全复刻/报告/B9_W23DRAFT_轮抽接线与预组校验.md` | 新建（本文件） | 新文件 |

**没动的**（守所有权）：`Battle/BattleDriver.cs` · `Core/CardFeel.cs` · `Editor/BattleScene.cs` · `Core/CardView.cs` · `Editor/CardFaceAssertE5.cs` · `RuleEngine/Core/{EffectResolver,UnitState}.cs` · `Effects/UIEffectToggler*` · `Deck/**` · `Shell/BoosterPackOpenWindow.cs` · `Shell/{CollectionWindow,CampaignTab,ForgeTab,UIBorderGlow}.cs` · 一切 `*.unity` · 账主表 · 基线文件 · 正本。

### ⚠️ 我造成的账漂移（请调度台在同步点跑一次 `--fix`）

只读闸 `python 工具/check_ledger_points.py --all-A` 现读，**归属本批的 6 条**（其余 679 条是存量 / 别路代理）：

| 账里写的 | 符号 | 现在实际 |
|---|---|---|
| `DraftPacks.cs:332` | `RollRound` | **336** |
| `DraftPacks.cs:372` | `BuildClosedDeck` | **394** |
| `DraftModeWindow.cs:3820` | `PackPlayOpen` | **3821** |
| `DraftModeWindow.cs:3849` | `PackPlayClose` | **3850** |
| `DraftModeWindow.cs:3874` | `PackPlaySelect` | **3875** |
| `DraftModeWindow.cs:3916` | `PackSelectFly` | **3923** |

（`RegisterPackUi :3765` / `PackUi :3668` **没动**；`--fix` 只能在「没有 `.cs` 写手 + 补丁已合并完」时跑。）

**验证**：`TMPDIR=/tmp/wf_w23 bash d:/4/Unity/工具/typecheck.sh` ⇒ 收工前最后一次 **运行时 0 / 编辑器 0**。

---

## 五、顺手发现

1. **`Shell/BoosterPackOpenWindow.cs:93` 与 `:1255` 还写着「`OpenCardbacks` 用途未查清 / 不接」。** 那是 **W20 的文件**（本批没动）。内容已查清（= `DeckGeneralInfoDemo.cardbacksAnimation`，消费者 = `PracticeModePopup.BuildCardbacks`）⇒ 请调度台转给 W20 把这两处改成指针（同一句错话被复制到两处，正是铁律 5 说的那种）。
2. **`BoosterPackAnim.cs` 里没有任何 cue 常量** —— 简报让我「顺手核对本文件里那几条 cue」，实际该文件只有 `BoosterPackAnim` 的曲线/表；它注释里出现的 `clickSound` / `audioCue` 属 `CardInBoosterPack`（开包窗那族），现读已由 `BoosterPackOpenWindow.cs:795`（`BoosterCardClickCue`）/ `:839`（`BoosterRarityCue`，`MixerType.Jingles`）接上。**四条 draft cue 全在 `DraftModeWindow` 接好**（`:2008` 预览 / `:3831` 出现 / `:3944` 起飞 / `:3952` 落位）。
3. **`MenuDraw.Nine` 返回的节点自己没有 `ImageQuad`**（九宫全在子件）—— 这条对**所有**「按 GameObject 挂 `UIEffectTogglerImage`」的地方都成立：`Add` 不递归子件（那是它照原版 `Image[] images` 的正确口径），所以接线方必须自己展开，否则**静默空转**。建议把这条写进 `UIEffectTogglerImage` 的文件头或 `B9_W14FX` 的补丁 B 旁边。
4. **typecheck 过程中先后撞见两处「别人的文件」的瞬时错误**（不是本批回归，收工那次已 0/0）：`Effects/UIEffectTogglerCoffeeEffects.cs`（W16，3 个：`mx` 重名 + `Vector3` 与 `float` 混算）· `RuleEngine/Core/EffectResolver.cs`（S0，6 个：`TargetsAffectedForOp` / `CondDeadByTa…` 找不到）。按铁律 13·3 不当成自己的错去改。
5. **`RegisterPackUi` 的清理行原地扩写**成 `if (index == 0) { _packUis.Clear(); _packTogglers.Clear(); }` —— 因为该文件被账按 `文件:行号` 引用（`卡组编辑` / `轮抽` / `日常_奖励` / `未分类_其他` / `战斗逻辑驱动` 五域），能不动行数就不动。
6. **`PackUi` 类里没加字段**（`_packTogglers` 放窗口级）：往 `PackUi` 里插字段会把 `:3820/:3849/:3874/:3916` 那四行整体推下去，而「类尾追加」才是本文件的纪律。代价 = 副本那颗控制器只能按节点现查（`PackScanToggle`），这一点已写进它的 doc。
