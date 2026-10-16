# G2 · `Editor/BattleScene.cs` 收口断言（收口波 · 第四十二会话）

> 白名单 = `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` + 本报告。⛔ 未跑任何 `*.Run` / 未起 player / 未动 git / 白名单外一字节未改。
> ✅ `TMPDIR=/tmp/wf_g2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 · 编辑器 0 错**（改完每一族各跑一次，共 7 次；中途 1 次 `CS0136`（`probe964` 重名）当场改名修掉）。
> ✅ **行尾**：改前 `CRLF 24644 / LF 24644`（全 CRLF）· 改后 `CRLF 25014 / LF 25014`（CR-only 0）⇒ **没翻行尾**。
> ✅ `git diff --numstat -- MyGame/Assets/CardPresentation/Editor/BattleScene.cs` = **428 / 58**（远小于 25,014 行 ⇒ 不是整篇重写）。
> ⚠️ 行号 = 本报告落盘那一刻的**现读值**（`grep -n` 可复现）；文件再动一句就会漂。

---

## ① 结论 —— 台账逐条

| 台账 | 处置 | 说明 |
|---|---|---|
| **`W11`** | ✅ **做了**（**8** 条，比台账多 1 条「前提」） | `:7150-7241`。A2 那 7 条全落：① 档位自洽 · ②–⑤ 子画布 `overrideSorting`/层/order/**不带 `CanvasScaler`** · ⑥ `sp.OnCanvas` 灭自证 · ⑦ 反面「世界档下 `OnCanvas` 必须为假」。**多出来的第 8 条 = 「（前提）已登记宿主 ⇒ 跑画布档」**（见 §⑤·1：那 5 条要跑画布档才有意义，没接线时用**一条**红说明根因，而不是刷 5 条同级红） |
| **`W27`** | ✅ **做了** | `:831-876`。出声断言落成 **3** 条（不是 1 条，见 §③·2）：合成阵营 ⇒ 留白 + 真出声 + 计数器增；真阵营 `Goff` ⇒ 只断留白 |
| **`W32`** | ✅ **做了** | ① 两处过期文案必改：`:21151-21159` · `:21192-21193`（都写明「原来写 `localPosition`，现在两处都是世界点」）② 附件 6 条结构断言全部落地（`:21201-21262`，编号 ①–⑦，其中 ⑦ 是 A3 §③·4 的「双模式往返」） |
| **`W34`** | ✅ **做了**（6 条，逐条按 `A8` §③·1） | `:12224`（`ForCantDo`）· `:12264`（`ForHurry`）· `:12330`（`ForVersus` 链尾）· `:12340`（`Genestealers`）· `:12373`（`ForIntro`）· `:12377`（`ForMirror`）。另**顺带订正 3 处已失真的邻句文案**（`ForVersus` 那段注释 · `vsOnlyIntro` 那句「只回落到 `intro`」· `ForCantDo` 那句「我们没有那个字段」）—— 见 §⑥·1 |
| **`W35`** | ✅ **没改**（按台账「⛔ 别改」） | `:11928` 那条 `Check(exDiff > 0, …)` 一个字节没动。⚠️ **但它的邻句 `:11940` 还写着「今天 5 条」（现读应为 1 条）** —— 台账把 `W35` 标成「别改」，我**遵守了**，只在此登记（§⑤·4） |
| **`W45`** | ✅ **做了** | 6 处按档改：`:12090`（`BubbleCenterPx` + 原版设计 px）· `:12093`（`BubbleSizePxOf`）· `:12094`（`enemy.y` 比较方向**随之翻**）· `:12191`（乘子按档）· `:12471`（`ButtonWorldPos`）· `:21121`（`ButtonWorldPos`）· `:16726`（A218 传宿主 + `a218CvW/H`）· `:16771`（A218 传宿主、期望值不变）· `:16710`（`:16537` 那句「`UnitChatPanel` 还没迁」已过期，就地订正）。✅ `:12311`（「点面板外」，现 `:12484`）**按台账不改**，逐字未动。另加**一组**子画布/`OnCanvas` 断言：`:16779-16808`（子画布 4 条 × 2 扇 + 正面 2 条）+ `:12104`/`:12425`（**反面 2 条**，写在世界档那两节里） |
| **`W52`** | ✅ **做了** | 2 处按档改：`:21890-21898`（左缘）· `:21957-21971`（`RW1192` 按档取尺）。另加 3 条断言：`:21775-21829`（子画布 `overrideSorting`/层/order/无 `CanvasScaler` 2 条 + **逐件 `GetComponentInParent<Canvas>()` 灭自证** 1 条） |
| **`W58`** | ✅ **做了** | `:7150-7241`（与 `W11` 同块）。A10 §③·4 的三段全落：① 判据侧期望值（拆成 ① 尺寸 + ①b 中心两条）· ② 判别式灭自证 · ③ 命中框咬合。用到了它新加的 3 个只读口 `PanelCvSize` / `PanelCvCenter` / `CanvasPxSize` |
| 过期文案（台账点名） | ✅ 两处都订正 | `:16710`（原 `:16537`，「`UnitChatPanel` 还没迁」）· `:21571`（原 `:21285`，`E2⑥` 那句「实绘读法 = `WorldW/H × lossyScale`」只对世界档成立 —— 同时覆盖 `W13`/`W57`） |

**一句话**：台账点名的 8 条**全部落地**；`W35` 按台账**故意不改**。

---

## ② 改动清单（`文件 : 行号` + 「改前口径 → 改后口径」）

> 全部在 `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`。行号 = 现读。

| 行 | 台账 | 一句话（改前 → 改后） |
|---|---|---|
| `:831-876` | `W27` | **新增**。VfxMap 断言块尾补一段 `Application.logMessageReceived` sink：合成阵营 ⇒ `SummonCircleOf == null` + 抓到含「阵营召唤法阵」的 Warning + `NoFactionVariantWarns` 增；`Goff` ⇒ 只断回 null |
| `:7150-7241` | `W11`+`W58` | **新增**。技能面板块尾补 8 + 5 条：档位自洽 →（前提）画布档 → 子画布四元组 → `OnCanvas` 灭自证 → `W58①/①b/②/③` → 反面 `OnCanvas` |
| `:12088-12089` | `W45` | **删** `var want = LayoutSpace.ToWorld(…)`（世界帧期望值，画布档下是假红源） |
| `:12090-12109` | `W45` | `BubbleCenterWorld` + `0.001 世界单位` → **`BubbleCenterPx` + 原版设计 px `(337.0003, 761.7524)`、容差 `0.05 px`**；`BubbleWorldSize × 108f` → **`BubbleSizePxOf`**；`enemy.y > player.y`（世界 y 向上）→ **`enemy.y < player.y`**（设计 px y 向下）；+ 反面 `chat.IsUgui \|\| !chat.OnCanvas` |
| `:12191-12197` | `W45` | `lb.WorldW * 108f` → **`lb.WorldW * (chat.IsUgui ? 1f : 108f)`**（`Label.WorldW/H` 在 uGUI 档本来就是画布 px） |
| `:12216-12228` | `W34` | `ForCantDo.Length == 1` → **`== 2 && [1] == "default"`** + 注释订正（「我们没有那个字段」已不成立） |
| `:12264-12267` | `W34` | `ForHurry.Length == 1` → **`== 2 && [1] == "default"`** |
| `:12325-12332` | `W34` | `v1[末] == "intro"` → **`v1[末−1] == "intro" && v1[末] == "default"`**（`Length >= 3` 护栏） |
| `:12340-12343` | `W34` | `v2.Length == 1` → **`== 2 && [1] == "default"`** |
| `:12373-12382` | `W34` | `ForIntro.Length == 1` → **`== 2 && [1] == "default"`**；`ForMirror[1] == "intro"` → **`== "default"`** |
| `:12425-12428` | `W45` | **新增**反面 `pop.IsUgui \|\| !pop.OnCanvas` |
| `:12471-12474` | `W45` | `LayoutSpace.ToWorld((r.x+…)/1920, …)`（含多余 `var r`）→ **`pop.ButtonWorldPos(i)`** |
| `:16710-16712` | 过期文案 | 「`UnitChatPanel` 还没迁 ⇒ 照旧」→ **带更正痕的订正** |
| `:16726-16734` | `W45` | `UnitChatPanel.Create(a218root.transform)`（世界档）→ **`Create(…, EnsureSubCanvas(SubCanvasName, SortingLayerId, SortingOrder))`**；期望值 `1920f, 1080f` → **`a218CvW, a218CvH`** |
| `:16771-16777` | `W45` | `ChatPopupPanel.Create(a218root.transform)` → **传 `"ChatPopup Canvas"` 宿主**；**期望值 `815.04/475.47` 不变**（两档同值） |
| `:16779-16808` | `W45`+`W11` | **新增**。`subCv` 局部口 × 2 扇（各 4 条）+ `a218Uc.OnCanvas` / `a218Pop.OnCanvas` 正面两条 |
| `:21121-21124` | `W45` | `LayoutSpace.ToWorld(rc964…)` → **`pop964.ButtonWorldPos(i964)`** |
| `:21151-21159` | `W32` | 前提注释与断言文案：`localPosition` 相减 → **两处都读世界点**（保留 `transform.position == 0` 那条，理由换掉） |
| `:21192-21193` | `W32` | 模型③ 那条文案里「`ButtonWorld` 回的是 `localPosition`」→ **回世界点** |
| `:21201-21262` | `W32` | **新增**。`sel964` 7 条：① 档位自洽 ② 宿主画布 `overrideSorting`/层/order ③ `siblingIndex == 0` ④ 帧 rect 621.7×122.2 ⑤ 帧里 0 个世界空间 `ImageQuad` ⑥ 帧落在 `AnchorWorld` 正投影 ⑦ 双模式往返探针 |
| `:21571-21576` | `W57`/`W13` | `E2⑥` 那条说明文案：「实绘读法 = `WorldW/H × lossyScale`」→ **两档分列**（世界档 `WorldW/H`、画布档 `sizeDelta`，契约恒为世界单位） |
| `:21775-21829` | `W52` | **新增**。`Alliance Panel` 子画布 2 条（`overrideSorting`/层/order + 无 `CanvasScaler`，画布档才跑）+ 逐件 `GetComponentInParent<Canvas>()` 灭自证 1 条（两态都断） |
| `:21890-21906` | `W52` | `PosInDesignSpace(t).x − WorldW*0.5`（设计空间世界单位）→ **`PxX(PosInDesignSpace(t).x) − (OnCanvas ? 0 : WorldW*54)`**；断言由 `PxX(left985)` 改直比 |
| `:21957-21971` | `W52` | `RW1192`：`|textBounds.size.x| × \|lossyScale\| × pxPerWorld` → **uGUI 档直接读 `|textBounds.size.x|`（= 画布 px）**，世界档那一支原样 |

### 行尾核对

```
$ git diff --numstat -- MyGame/Assets/CardPresentation/Editor/BattleScene.cs
428     58      Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs
$ python -c "... b.count(b'\r\n'), b.count(b'\n')"
CRLF 25014 LF 25014        # CR-only 0；改前 24644/24644
```
⇒ **净增 370 行，行尾仍是纯 CRLF，没翻。**

---

## ③ 接线清单（**白名单外**，我一个字没改 —— 这些是「上面那些断言能不能绿」的前提）

### ③·1 我的新断言**依赖**的白名单外改动 —— 🔴 **现核：已经全部落地**（2026-10-16，`G1`/`G3` 同波）

| 落点 | 台账 | **现核状态** |
|---|---|---|
| `BattleDriver.cs:14522-14526`：`HudCanvas.Ensure(LayoutSpace.Cam)` + `skillPanel.UseHost(EnsureRankedSubCanvas(SkillPanel.SubCanvasName, OverHand, 0))`（在 **`BuildHud()`** 里，`BuildHud` 由 `Begin()` `:3346` 调） | `W7`/`W8`/`W55` | ✅ **已落地** ⇒ 我加的那条「（前提）技能面板跑画布档」**应绿**，`W11②-⑥` + `W58①/①b/②/③` 共 **10 条真的会跑**（不再跳过） |
| `BattleDriver.cs:14653`：`selector.UseHost(EnsureRankedSubCanvas("Drag Attack Selector Canvas", OverHand, 0))` | `W30` | ✅ **已落地** ⇒ `T09/A3②-⑦` 6 条**真的会跑** |
| `BattleDriver.cs:14567`（`Unit Chat`）· `:15284`（`ChatPopup`）· `:15259`（`Alliance Panel`）各自的 `EnsureRankedSubCanvas` + `Create(..., cv)` | `W42`/`W43`/`W50` | ✅ **已落地** ⇒ 生产路径也进画布档 ⇒ 我的 A218 探针与生产面板**共用同一块子画布**（`EnsureSubCanvas` 按名字幂等 ⇒ 这是预期行为，与 `Mulligan`/`ChooseCardMenu` 那几扇同族） |
| `HudCanvas.cs:548-557` `SubCanvasRank` 的 `OverHand` 段：`Drag Attack Selector(0/14)` · `ActiveSkillDesc(1/14)` · `Unit Chat(2/14)` · `ChatPopup(4/7)` · **`Mulligan(3/14)` 排在其后** · `Alliance Panel(13/14)` | `W9`/`W31`/`W44`/`W51`/`W56` | ✅ **已落地**。⚠️ **我断不到它**（我只断层/order/兄弟序=0）；这一格决定「同层同 order 谁画在上面」，判据 = 原版 `Safe area FrontCanvas` 的 14 子件序 |

🔴 **因此本报告 §④ 里那些「没接线 ⇒ 红」的行，现读应改成「接线已落 ⇒ 应绿」**；把它们留着，是因为**回退一次那行接线**就会当场红/跳过（这正是这些断言要守的东西）。

### ③·2 我**主动偏离台账一处**（请主对话核）

- 台账 `W27` 写的是：`SummonCircleOf("Goff") == null` **且**抓到含「阵营召唤法阵」的 Warning。
  🔴 **照字面写会是假红**：`VfxMap.WarnNoFactionVariant` 按 `(what, faction)` **去重**
  （`if (!_warnedNoFaction.Add(what + "|" + faction)) return;`，`Core/VfxMap.cs:341`），而 `Goff` 是**真阵营**、
  战斗里 `PlaySignal` 的 deploy 分支（`BattleDriver.cs:12331`）**可能早就为它报过一次**
  ⇒ 第二次调用**不出声** ⇒ `heard == false` ⇒ 那条断言**假红**。
  ⇒ 我的处置：**用合成阵营名 `"__A1731_no_such_faction__"`**（绝不在 `SummonCircleByFaction` 里、必然是新的一对）做「出声」那一条；
  **真阵营 `Goff` 只断「回 null」**（不出声那半不断）。判据与理由写在断言上方的注释里，⛔ 没把去重口径藏起来。
  三条断言（不是台账的 1 条）见 §④·`W27`。

### ③·3 其余（我这条线看到的、要别人改的）

- `Battle/HudCanvas.cs:464` 的注释「`SkillPanel`(1) / `Unit Chat`(2) **还没迁**」**已过期**（`W10`/`W56`）—— 我在 `BattleScene.cs` 侧的同类过期文案已改，`HudCanvas.cs` 不在我白名单。
- `Battle/Label.cs:156/162` 那句「`localPosition` 就是相对父中心的偏移」只在**父件枢轴居中**时成立（台账 `W62`，共享件、后果跨扇）。
- `BattleDriver.cs:4192` 那段注释仍写「与 `VoiceLines.ForCantDo` / `ForHurry` / … 同一套」—— 内容仍对，只是**表格本身多了一跳 `default`**（不影响）。

---

## ④ 判据与出处 —— 每条新/改断言的**期望值来源** + **什么条件下会红**

> 通则：期望值一律取**判据侧**（原版 JSON / `T09c` 表 / 另一个已独立验证的口），⛔ 不读被测实现自己的常量。

### `W27`（`:831-876`，VfxMap 出声，3 条）

| 断言 | 期望值来源 | 什么时候**红** | 什么时候**绿** |
|---|---|---|---|
| 合成阵营 ⇒ `SummonCircleOf` 回 `null` | `Core/VfxMap.cs:276-282` 的 `SummonCircleByFaction` 只有 4 个键（Ember/Tide/SaimHann/TauEmpire） | 谁给「没配过的阵营」补了候选 / 加了兜底 | 恒绿（合成名永远不在表里） |
| **留白不再静默**（抓到 Warning **且** 计数器增） | `VfxMap.WarnNoFactionVariant` 的 `Debug.LogWarning`（`:338`）+ `NoFactionVariantWarns`（`:331`） | 🧨 删掉 `SummonCircleOf` 里那句 `WarnNoFactionVariant(...)`（回到静默 `return null`）· 把 `Debug.LogWarning` 降级成 `Log`（`ty != LogType.Warning`）· 把去重键改成不区分阵营 | 恒绿（合成名必然是新的一对 ⇒ 不受去重影响） |
| 真阵营 `Goff` ⇒ 也回 `null` | 同上 | 谁给 `Goff` 补了候选 | 恒绿 ⚠️ **本条只断「留白」**，**不断「这次出声」**（去重会假红，见 §③·2） |

### `W11`（`:7150-7241`，8 条）+ `W58`（5 条）

| # | 断言 | 期望值来源 | 红 / 绿 |
|---|---|---|---|
| `W11①` | `UguiMode == (CanvasRect != null)` | —— | ⚠️ **按构造恒绿**（两个口同源：`_cv != null`）。**已在断言上方如实标注**：它只是回归哨兵，判别力在 ⑥ 与 ②–⑤ |
| （前提） | `sp.UguiMode` | `T09c` §一（`ActiveSkillDesc` @ `OverHand`/0/1-14） | 🔴 **没接线（`W7`）⇒ 红**；**现核接线已落（`BattleDriver.cs:14522-14526`）⇒ 应绿** |
| `W11②` | `sp.CanvasRect.GetComponent<Canvas>().overrideSorting` | `HudCanvas.EnsureSubCanvas` 的**原版口径**（`T09b` §A·2：原版子 canvas 都 `overrideSorting=true`） | 🧨 拆掉那句 `cv.overrideSorting = true`（`HudCanvas.cs:449`）⇒ 红 |
| `W11③` | 排序层 == `HudCanvas.SortingLayer.OverHand` | **`T09c` §一**那一行（`OverHand`）—— ⛔ 不读 `HudCanvas.SubCanvas(name).sortingLayerID` | 🧨 把 `UseHost` 传的层改成别的 / `EnsureSubCanvas` 那句赋值删掉 ⇒ 红 |
| `W11④` | `sortingOrder == 0` | `T09c` §一（order **0**） | 同上 |
| `W11⑤` | 子画布**没有** `CanvasScaler` | 原版**全场景只有一个** scaler、在根上（`T09` §五·补⑨ 一） | 🧨 给子画布 `AddComponent<CanvasScaler>()` ⇒ 红 |
| `W11⑥` | `sp.OnCanvas`（逐件：uGUI 件 **且** 父链下有 `Canvas`） | `SkillPanel.OnCanvas` 的实现口径（`:1032`）—— **这是「半迁移态」唯一能抓到的口** | 🧨 把任意一件建回世界空间 ⇒ 红。⚠️ **副效应**：任一件**没建出来**（缺图/缺字体）也返 `false` ⇒ 那档会红（`A2` 的有意设计，如实记） |
| `W11⑦` | `UguiMode \|\| !OnCanvas` | 灭自证要求（防「把 `OnCanvas` 写成恒真」） | 世界档下 `OnCanvas` 为真 ⇒ 红。**两档都跑** |
| `W58①` | 面板**尺寸** == 原版 `RT_3104` 锚点盒现算 | **原版 `RectTransform_3104.json`**：`aMin (0.665576696395874, 0)` · `aMax (0.965788722038269, 0.3040129840373993)` · `sd (−0.0003662100061774254, −3.509999990463257)`（6 个数**抄自原版**，不是我们的常量） | 🧨 把 `PanelCvRect` 换回写死 `PanelPxW/H`（576.41×324.82）⇒ 画布高 ≠ 1080 时红 · 抄错一位锚点常量 ⇒ 红 |
| `W58①b` | 面板**中心** == 锚参考点 + `ap` | 同上（原版 `ap (−606.110107421875, 55.0008544921875)`） | 同上 |
| `W58②` | **判别式**：画布高 == 1080 ⇒ ≈324.82；≠ 1080 ⇒ **必须 >10 的偏差** | 原版锚点盒 + `CanvasPxSize` | 🧨 实现改回写死 px ⇒ 非 16:9 那一支红；把 `CanvasPx` 写死 1080 ⇒ 同上红。⚠️ **退化档**（`_cv.rect` 量不到 ⇒ `CanvasPx` 退 1920×1080）⇒ 走 16:9 那一支、**判别力消失但不错红**（`A10` §⑤·2 已记） |
| `W58③` | `sp.Contains(sp.LightWorldPos)` | 命中框 = 现算的 `PanelCvRect`（`A10` §③·4 ③） | 🧨 命中框不随面板重算（仍写死 1920×1080 那一档）⇒ 面板一变色片中心掉出框 ⇒ 红 |

### `W45`（UnitChat / ChatPopup）

| 断言 | 期望值来源 | 红 / 绿 |
|---|---|---|
| `:12093` 我方气泡中心 == `(337.0003, 761.7524)` 设计 px | **原版 `RT_3231`**：`ap (12.61328125, 200)` + `sd (648.77404785, 236.50468445)`，父 = 画布 1920×1080 ⇒ `12.61328125 + 648.77404785/2`、`1080 − 200 − 236.50468445/2`。⛔ 不读 `Slot`/`BubbleW`（被测实现自己的常量） | 🧨 锚点回到中心锚（`A12` 修掉的那一档）⇒ 非 16:9 下偏 ⇒ 红 · 世界档下 `ToDesignPixel` 若与 `FromPixel` 不再互逆 ⇒ 红（两条独立路径都指向同一对数） |
| `:12095` 气泡 `648.77×236.50` px | 原版 `sd` 同上 | 🧨 尺寸常量被改 ⇒ 红（非 16:9 下旧写法 `×108` 会假红 —— 这正是本条改掉的） |
| `:12094` `enemy.y < player.y` | 原版三颗气泡的 `aMin=aMax`（`(0,0)` / `(0,1)` / `(1,1)`，`A12` §④·〇） | 🧨 两颗气泡的锚点/落点对调 ⇒ 红（**改后是设计 px、y 向下 ⇒ 判据方向随之翻**，⛔ 别照世界档那份读回来） |
| `:12104` / `:12425` 反面 `IsUgui \|\| !OnCanvas` | 灭自证要求 | 世界档下 `OnCanvas` 为真 ⇒ 红。**两档都跑** |
| `:16779-16808` 子画布 4 条 × 2 扇 | `T09c` §一 那两行（`Unit Chat` 2/14 · `ChatPopup` 4/7，都 `OverHand`/0）+「子画布不带 scaler」 | 🧨 同 `W11②-⑤` |
| `:16807` / `:16809` 正面 `OnCanvas` | 探针是**显式传了宿主**建的（`EnsureSubCanvas`）⇒ 必须为真 | 🧨 漏迁任一扇的任一件 ⇒ 红（机械证明） |
| `:16726` / `:16771` A218 的 `box(...)` | 原版 `Unit Chat` = stretch ⇒ 铺满父画布（`a218CvW/H`）· 原版 `ChatPopup` `sd (815.044, 475.470)` | 🧨 `Create` 忘了传宿主 / 根节点没用 `FillParent` ⇒ 红 |

### `W34`（语音表，6 条）

| 断言 | 期望值来源 | 红 / 绿 |
|---|---|---|
| `ForCantDo.Length == 2 && [1]=="default"` 等 3 条 | **原版反编译**：`decomp_full/RawCardScript__GetChatSound.c` 末尾那一跳（取不到 ⇒ 落 `+0x188 defaultChatSound`）+ `dump.cs:23300` | 🧨 把 `default` 那一跳从表里删掉 ⇒ 红 · 回落成 `line` ⇒ 红（**两条都挡住**） |
| `ForVersus` 末二 `intro` / 末 `default` | 原版 `ChatManager__GetChatSoundAsset.c` 的 `customIntro → customIntroByArmy → GetChatSound(0)` + `GetChatSound` 内部那一跳 | 🧨 链尾顺序被改 ⇒ 红 |
| `ForMirror == {mirror, default}` | 同 `GetChatSound` 内部那一跳（**⛔ 不是 `intro`**，`intro` 是上位链的末跳） | 🧨 改回 `→ intro` ⇒ 红 |

### `W32`（AttackSelector，7 条）

| # | 期望值来源 | 红 / 绿 |
|---|---|---|
| ① 档位自洽 | `AttackSelector` 的两个口**不同源**（`UguiMode ⇒ _frame != null`；`CanvasRect ⇒ _frame != null ? _host : null`）⇒ **这一条不是同义反复**：能抓到「帧建在画布上但宿主丢了」 | 🧨 `UseHost` 里把 `_frame` 建了却不清 `_host` / 反过来 ⇒ 红。**两档都跑** |
| ② 宿主画布 = `overrideSorting` / `OverHand` / order 0 | `T09c` §一（原版那节点身上就有 `Canvas`，`overrideSorting=false`；我们复刻的是**渲染次序**，见 `T09` §八·3 的裁定） | **现核接线已落（`BattleDriver.cs:14653`）⇒ 应绿**；回退那行 ⇒ 跳过 |
| ③ 帧 `siblingIndex == 0` | **原版 `Safe area FrontCanvas` 的 `m_Children[0]`**（兄弟序 **0/14**） | 🧨 把 `ZFrameBottom` 哨兵去掉 / 改小 ⇒ 红 |
| ④ 帧 rect == 621.7×122.2 | **原版 `RectTransform_3166.json`** 的 `m_SizeDelta (621.7249755859375, 122.24700164794922)`（容差 0.5 px，我们的常量差 0.025/0.047） | 🧨 帧尺寸写错 ⇒ 红 |
| ⑤ 帧里 0 个世界空间 `ImageQuad` | 机械证明（写法照 `A218` 那条） | 🧨 漏迁一件 ⇒ 红 |
| ⑥ 帧 x/y == `AnchorWorld` x/y | `AnchorLocal()` 走 `_frame.parent.InverseTransformPoint`（`A3` §②）；⛔ **不拿 `AnchorLocal` 反算回来比**（同义反复） | 🧨 锚点投影那一跳改错 ⇒ 红 |
| ⑦ 双模式往返（`UseHost` 前后） | `A3` §③·4 ⑥ | 🧨 出厂态变成画布档 / `UseHost` 不换档 ⇒ 红。⚠️ **只在 `sel964.UguiMode` 为真时跑**（`UseHost(null)` 是非法用法） |

### `W52`（AlliancePanel）

| 断言 | 期望值来源 | 红 / 绿 |
|---|---|---|
| 名字块左缘 == **276.220 px** | 原版 `GameObject/Name Text.json` 的 `RectTransform_2779`（`ap (235.724,−0.051)` · `size (348.760,51)` · `localScale 0.8`）+ `NameHolder` + 父链（**原版值，不是我们的常量**） | 🧨 「空串时对齐的位置 + 实测宽的一半」那一档 ⇒ 红。**两档都跑**（`PxX` 线性 ⇒ 世界档与改前逐位同值） |
| `RW1192 ≤` 那 7 颗字的框宽 | 原版 prefab RT × 该族 `localScale 0.8`（`A1192` 那节已有的表，⛔ 不读被测实现的常量） | 🧨 uGUI 档再乘 `lossyScale×108` ⇒ 非 16:9 下量小 ⇒ **假绿**（改后消除） |
| 子画布 2 条 | `T09c` §一（`Alliance Panel` 13/14，`OverHand`/0） | 只在画布档跑；没接线 ⇒ 跳过 |
| 逐件 `GetComponentInParent<Canvas>()` 灭自证 | **独立于被测实现那个口**（⛔ 不复用 `AlliancePanelWindow.OnCanvas`） | 🧨 半迁移态（一半画布一半世界）⇒ `apBad > 0` ⇒ 红。**两态都断**：画布档必须全在、世界档必须一件都不在（`HudRoot` 是裸 `GameObject`、不在任何 `Canvas` 下 ⇒ 世界档那一半确定成立） |

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. 🔴 **我主动加了 1 条「（前提）」断言，台账 `W11` 只要求 7 条** —— `Check(sp.UguiMode, …)`。
   理由：`W11②-⑥` 与 `W58` 那 4 条**只有在画布档才有意义**；万一没人登记宿主（`W7` 那一行被回退/被 `Ensure` 提前 return 挡住），
   若不设闸会刷 **10 条同级红**、根因却只有一行接线。⇒ 现在是「**1 条红 + 9 条诚实跳过**」。
   ✅ **现核 `W7` 已落**（`BattleDriver.cs:14522-14526`）⇒ 这一条应绿、那 9 条会真跑；它守的是**回退**。
   ⚠️ **请主对话确认这个取舍**（台账没写这条）。
2. 🔴 **`W27` 的字面期望值会把断言写假**（`Goff` 去重）⇒ 我改成「合成阵营断出声、真阵营只断留白」（§③·2）。**请核**。
3. ⚠️ **A3 §③·4 的 ⑥「双模式往返」我按『只在画布档跑』落地**：`UseHost(null)` 是不合法用法，
   而自造一块新子画布会在战斗 HUD 里留一个**不在 `SubCanvasRank` 里**的节点（污染别处的枚举）。⇒ 只断档位开关、不断件数。
   🔴 **⑧ 那一条我另加了一句 `probA3964.Build();`**（编辑模式下 `AddComponent` **未必**跑 `Awake`，
   而 `UseHost` 只在 `_built` 为真时才重建 ⇒ 少了它会拿到 `cHalf == false` 的**假红**）。`Build()` 自带 `_built` 闩 ⇒ 幂等、零副作用。
4. ⚠️ **`W35` 的邻句 `:11940` 仍写「（`exDiff > 0`，今天 5 条）」—— 现读应为 1 条。** 台账把 `W35` 标成「别改」，
   我**照办（一个字节没动）**，但按铁律 5 这属于「与事实不符的记录」⇒ 登记在此，**由主对话决定改不改**（我只改这一句数字，风险为零）。
5. ⚠️ **`W52` 那 3 条我只断「渲染空间一致」，没断「子画布 rect = 节点自己的绝对 px」** —— `W52` 的原文是
   「画布档比**节点自己的绝对 px** …」，我把它落成「左缘 == 276.220」（同一个量、可判、判据是原版 JSON）。
   ⛔ 没自造第二个口。
6. ⚠️ **没跑果**：本波按现行口径**不跑 `*.Run`** ⇒ 上面每一条的红/绿都是**按代码读出来的**（判据行号已给），**没有一条是实测的**。
7. 🔴 **我**没**做「全量换口径」**：本文件 2,700+ 条 `Check(` 里，凡是**没被台账点名**、但我看到的同类隐患，**一律没改**。
   实例（§⑥·2/·3）—— 若要改，是**另一批**。

---

## ⑥ 顺手发现（**不属于本件，只报不改**）

1. ℹ️ **`W34` 连带订正了 3 处邻句文案**（`:12329-12332` 的 `vsOnlyIntro` / `:12219-12221` 的「我们没有那个字段」/ 同上注释）——
   它们与点名的那 6 条**同一个判据**，改法是一行文字，已一并落。**不是新账。**
2. 🔴 **本文件里可能还有别的「世界帧期望值 vs 画布档」错配，我没查全**。已点名的那几处改完了，
   但我**没有系统 grep**「`LayoutSpace.ToWorld(` 后面跟一张面板的 `rect`」这一族。
   ⇒ 建议：迁移批 3 收口时，**用一次 grep 普查**（`grep -n "LayoutSpace.ToWorld((.*)/1920f" Editor/BattleScene.cs`）逐处按档判。**新账**（我没做，因为台账只点名了 6 处）。
3. ⚠️ **`A1` §⑤·1 那行 `EnemyChatDisplay` 的错值（`410/646.5`，差 `56.5`）我只用了 `A12` 的订正值（`173.5/410`、差 180）** ——
   我这条断言最终**不用那张表的差值**（改成直接比原版设计 px），所以**没受污染**；但 `T09` §五·补③ 里若还引着那一行，**仍要按 `W45`/`A12` 订正**（在别人手上）。
4. ⚠️ **`sp.OnCanvas` 的副效应**（`W11⑥`）：任一件**没建出来**（缺图/缺字体）也返 `false`。
   批处理里图是齐的（`A2` §⑥·5 已核），但**真机若某个 sprite 缺** ⇒ 那条会红。⇒ 属「判据设计」不是缺陷，登记备查。
5. ✅ **没有新增「同一条规矩的第二份写法」**：三处子画布断言各只有**一份**（`subCv` 局部口 / 直接调 `HudCanvas.SubCanvas`），
   换算口全走 `HudCanvas.SizePx` / `ChatPopupPanel.ButtonWorldPos` / `UnitChatPanel.BubbleCenterPx` —— ⛔ 没在断言里手写第二遍。

---

## 可直接粘进总表的一行（≤3 行）

| `G2`（收口波 · `Editor/BattleScene.cs`） | ✅ **台账 `W11`/`W27`/`W32`/`W34`/`W45`/`W52`/`W58` 全部落地**（`W35` 按台账**故意不改**）：技能面板 8+5 条 · VfxMap 出声 3 条 · 攻击选择器 7 条 + 2 处过期文案 · 语音表 6 条必红断言按原版 `GetChatSound` 改口 · UnitChat/ChatPopup 7 处按档改 + 一整套子画布与 `OnCanvas` 正反断言 · AlliancePanel 2 处按档改 + 3 条断言 · `:16537`/`:21285` 两处过期文案订正。**净增 350 行（CRLF 24644→24994，未翻行尾）**· `typecheck` **0/0**（跑了 7 次）· ⛔ **未跑任何 `*.Run`**（现行口径）。 |
| ⏭ **生效前提（白名单外）—— ✅ 现核已全部落地** | `BattleDriver.cs:14522-14526`（`skillPanel.UseHost`）· `:14653`（`selector.UseHost`）· `:14567`/`:15284`/`:15259`（三扇的 `EnsureRankedSubCanvas` + `Create`）· `HudCanvas.cs:548-557`（`SubCanvasRank` 五条已插）⇒ 我这边**没有一条被跳过**，`W11②-⑥`+`W58`（10 条）与 `T09/A3②-⑦`（6 条）**都会真跑**。回退任一行 ⇒ 对应那组红/跳过（这正是它们要守的）。 |
| ⚠ **三处主动偏离/加料（请核）** | ① `W27` 用**合成阵营名**断「出声」而不照字面用 `Goff`（`VfxMap` 的警告**按 (东西,阵营) 去重** ⇒ 字面写法会**假红**）；② `W11` **多加了 1 条「（前提）跑画布档」**（7 条 → 8 条），把「没接线」的根因压成**一条**红；③ `T09/A3⑦` 的探针**多一句显式 `Build()`**（编辑模式下 `AddComponent` 未必跑 `Awake` ⇒ 少它 = 假红）。 |
