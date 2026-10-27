# B9_W20WIRE · 特效组件接线与过期文案（执行报告）

> 日期 2026-10-27 · 上一路 = `B9_W14FX`（组件本体与两处适配层已修）· 本路 = **接线 + 过期文案**
> 白名单内 5 个 `.cs` + 本报告 + `账/_parts/B9_W20WIRE.tsv` · **一行 Unity 没跑、一次 git 没动**

---

## 一、结论

### 1·1 一句话

**`UIEffectTogglerImage`（`Done Highlight`）与 `AnimateUIMaterialProperty`（`Booster pack Background`）两处已真正接上并有真调用点/真驱动；另两处（段位窗 `Division Image`/`Rank Icon`、主菜单 `Level Up Effect`×2）经现读判为【挂载前提不具备】⇒ 按红线停手、写清「还差什么」，没有造一个假挂载。**

### 1·2 逐处交付表（补丁 A–E）

| 补丁 | 落点文件（本路白名单内？） | 结果 | 一句话 |
|---|---|---|---|
| **A** `Done Highlight` → `UIEffectTogglerImage` | `Deck/DeckRuntime.cs` ✅ | ✅ **已接** | 类尾 `ToggleDoneHighlight()`（`:8880`）；硬 `SetOn(_doneHl, …)` 换成它（`:4869`） |
| **B** `Prebuilt Pack UI` → `UIEffectTogglerImage` | `Shell/DraftModeWindow.cs` ⛔ 归 W8 | 📝 **改动请求** | 未碰该文件，补丁原文 + 落地位置写在 §五·B |
| **C** `Booster pack Background` → `AnimateUIMaterialProperty` | `Shell/BoosterPackOpenWindow.cs` ✅ | ✅ **已接** | `AttachBgDistort()`（`:1291`）挂两槽 + `StepBgShake()`（`:1332`）按 clip 键逐帧驱动 |
| **D** 段位窗 `Division Image` / `Rank Icon` → `AnimateUIMaterialProperty` | `Shell/RankedDivisionChangeWindow.cs` ✅ | ⛔ **挂载阻塞**（驱动口已补） | 那两颗**我们没画图、没有 `MeshRenderer`** ⇒ `Attach(Renderer,…)` 无处可挂；`materialFloat` 通道已升级成**真驱动**（组件一出现就自动接上） |
| **E** `Level Up Effect` / `Level Up Effect Down` → `AnimateUIMaterialProperty` | `Shell/CardDisplayEffects.cs` ✅ | ⛔ **挂载阻塞**（两条硬前提） | ① `Level Up Effect` 这个节点**我们根本没建**（判据 JSON 的 `images` 里只有 `Level Up Effect Down`）② 那颗已经挂了 `UIBorderGlow`，两者会抢同一个 `Renderer.sharedMaterial`（见 §三·3）⇒ 硬粘 = 造一个**写进空气**的假挂载 |

### 1·3 三处过期文案

| # | 位置 | 处理 |
|---|---|---|
| 1 | `Shell/RankedDivisionChangeWindow.cs:947-956`（原文案「那个组件我们工程里没有 / `grep class …` 零命中」） | ✅ **已就地改写**（同段零行数变化）：改走 `WarnNoMaterialTarget()`（`:1225`），文案 = 「组件早就在 `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`，缺的是**挂载目标**」 |
| 2 | `Shell/BoosterPackOpenWindow.cs:95`（「原版 shader 的逻辑本地没有」） | ✅ **已就地改写**（2→2 行），并按铁律 5 留了「就地订正」痕迹 + 真相（`Shader Graphs/Nebula`） |
| 3 | `Shell/BoosterPackAnim.cs:14-15` | 📝 **改动请求**（该文件归 W8），原文写在 §五·D |

另外**同一事实**在第 1256-1257 行（`HintGaps()` 的**运行期日志**，写着「⑦ … 逻辑本地没有 ⇒ **没做**」）也过期了 —— 已一并就地订正（2→2 行）。

### 1·4 同族 `_captured` 死锁

`Effects/UIEffectCanvasGroupToggler.cs`：`_captured` 死锁按 `UIEffectTogglerImage` 的同款修掉（`:_capturedTargets`，`:50` / `:58-59`），类尾补 `RefreshTargets()`（`:167`）。⚠️ 现有唯一调用点（`Shell/ForgeTab.cs:423`）**不填 `fadeTargets`** ⇒ 行为零变化（A 档不假）。

### 1·5 账

**`B9_W20WIRE.tsv`（新建，9 行含表头）**：8 行就地订正 —— `UIEffectTogglerImage` 4 行 `C→A`；`AnimateUIMaterialProperty` 的 `OnEnable()` / `Update()` / `Update(Material)` 3 行 `C→A`；`SetMaterial(Material)` **保持 C**（组件有挂载点了，但全树 0 调用者）。
`check_patch_conflicts.py` ⇒ **✅ 没有跨补丁重复键（4 份补丁）**。

**关于「缺 3 条 clip」那条过期说法**（简报第 4 件）：**主表那一行已经是对的、不需要我再改一遍** —— `账/特效动画.tsv:84` 的 `备注` 列现在写的就是「🔴 本条就地订正（B9_W14FX 2026-10-27 现读）：… 三条现在都在 `WarpforgeVFX/Animations/`」（`B9_W14FX` 的补丁已合并进主表，`_parts/_merged_B9_W14FX/` 在位）。
本轮**自己复核了一遍**，与该订正一致（都是现读，不是转抄）：三条 `m_Legacy: 1` · `m_SampleRate 60` · `m_StopTime 1`；`Rank Up.anim:390` 的 `AnimationFinished` @3.9167 s、`Level Up Card.anim:507` 的 `ChangeFrame` @1.45 s；`floatProperty1.value` 的 `script` guid 与我们 `AnimateUIMaterialProperty.cs.meta` 一致。⇒ **没有「缺 clip」这回事了**，剩下的只是「谁播、谁接那几颗事件」（`CardDisplayLevelUpAnimationController`，归 Shell）。

---

## 二、证据（文件:行号）

### 2·1 已接的两处 —— 落到哪一行 / 生效前提

**A. `Deck/DeckRuntime.cs`（CRLF 文件，8866 → 8908 行）**

| 内容 | 行 |
|---|---|
| 调用点（替换原 `SetOn(_doneHl, err == DeckError.None)`） | **`:4869`** |
| 新字段 `UIEffectTogglerController _doneHlCtl` / `bool _doneHlCtlWarned` | `:8870` / `:8871` |
| 新方法 `ToggleDoneHighlight(bool on)` | **`:8880`** |

生效前提（按补丁注释逐条落实）：
1. `UIEffectTogglerImage` 加在 **`_doneHl` 自己那颗 GO**（原版 `images[0]` = 同节点 Image ⇒ 不填 `fadeTargets`，走 `Capture()` 的自动档）；
2. 控制器加在 **`_doneHl.transform.parent`**（= DeckRuntime 的 `Root`，`DeckRuntime.cs:1282` `Root = transform`）—— 不能加在 `_doneHl` 身上：`deactivateOnDisable` 会把它 `SetActive(false)`，`Update` 再也不跑；
3. **`Rescan()` 显式调**（编辑/批处理下 `AddComponent` 不触发 `Awake`，`togglers` 恒空）；
4. 非运行期 `UIEffectTogglerController.Toggle` 自动 `duration = 0`（`Effects/UIEffectTogglerController.cs:57`）⇒ 与原来的硬切**行为等价**，既有断言不受影响。

**C. `Shell/BoosterPackOpenWindow.cs`（LF，1275 → 1345 行）**

| 内容 | 行 |
|---|---|
| `_openT = 0f; _bgShakeT = 0f;`（重开一包 ⇒ 时钟归零） | `:449` |
| `AttachBgDistort();`（挂在「背景 quad 已建成」之后的第一句上，零行数变化） | `:506` |
| `_fxClock += dt; StepBgShake(dt);`（`Advance()` 里唯一时钟，零行数变化） | `:903` |
| `WarpforgeVFX.AnimateUIMaterialProperty _bgAnim; float _bgShakeT;` | `:1286-1287` |
| `AttachBgDistort()` | `:1291` |
| `BgCurve1()` / `BgCurve2()`（两条 `AnimationCurve`，逐键含 in/outSlope） | `:1308` / `:1322` |
| `StepBgShake(float dt)` | `:1332` |

clip 键值（**现读** `WarpforgeVFX/Animations/Booster Window - Background Shake On Open.anim`，`m_Legacy: 1` · `m_SampleRate 60` · `m_WrapMode 0` · `m_StopTime 1`）：
- `floatProperty1.value`：`(0, −2, 0, 0)` · `(0.5833333, 0.2, in/outSlope 0.8727274)` · `(0.9166667, 0.4, 0, 0)`
- `floatProperty2.value`：`(0, 0)` · `(0.2166667, 0.35)` · `(0.5833333, 0.35)` · `(0.9166667, 0)`
- 两条曲线的 `script` guid = `c69dab8c0b494019bb4e17a0d92b9e79` = **我们 `AnimateUIMaterialProperty.cs.meta` 的 guid**（逐字节比对过）⇒ 这两条曲线本来就绑在我们这个类上，只是没有组件可绑。

🔴 **为什么不是「挂 `Animation` 组件播那条 clip」**（这是本路唯一偏离补丁字面的地方，理由如下）：
`WarpforgeVFX/Animations/` **不在任何 `Resources/` 下**（`find Assets -name "*.anim" -path "*Resources*"` = 0 行）⇒ 运行期 `Resources.Load<AnimationClip>` 取不到；而本工程运行期重建 clip 的唯一口 `RankedDivisionChangeWindow.BuildClip` **只给 Transform / GameObject 两族 `SetCurve`**（`NativeBindable`，`:599-602`），MonoBehaviour 曲线一律走手动通道。⇒ 采用本工程既有做法（`BoosterPackAnim` 的「采样表 + 代码补间」），逐帧写 `floatPropertyX.value`，**语义与 legacy clip 播一次等价**（停在末键）。

⚠️ **今天观感 = 零**（与 `B8_AS` §三·3 同一条）：我们那块图是 `Sprites/Default`，`Material.SetFloat` 对不存在的属性是静默 no-op（原版同）。要真出画面得先让 `Booster pack Background` 用 `Shader Graphs/Nebula`。

**D. `Shell/RankedDivisionChangeWindow.cs`（LF，1211 → 1233 行）**

| 内容 | 行 |
|---|---|
| `MatTrack[] materialPaths`（原 `string[]`） | `:629` |
| `matList = new List<MatTrack>()` / 收集时带曲线 | `:662` / `:693` |
| ⑥ 块改成**真驱动** `AnimateUIMaterialProperty.floatProperty1.value` | `:947-957`（11→11 行） |
| `struct MatTrack { path; AnimationCurve a; }` | `:1214` |
| `WarnNoMaterialTarget(string path)`（订正后的文案） | `:1225` |
| 文件头那句「我们这套世界空间 UI 一个这样的组件都没有」就地订正 | `:450`（1→1 行） |

**⛔ 挂载阻塞的证据**：`Build()` 里 `Division Image` 只 `MenuDraw.Node(...)`（`:276`），**没有 `MenuDraw.Rect`/`ImageQuad`**；`Rank Icon` 同（`:293`）。`MenuDraw.Node` 只建空 `RectTransform` ⇒ `GetComponent<MeshRenderer>()` = null ⇒ `AnimateUIMaterialProperty.Attach(Renderer,…)` 无处可挂（原版那两颗 `Image.m_Sprite = 0`，图由服务端喂，见本文件 `:34` 与 `NoteNoArt`）。⇒ 在本路补齐「真驱动」这一半，另一半点名交回调度台。

**U. `Effects/UIEffectCanvasGroupToggler.cs`（LF，159 → 174 行）**

| 内容 | 行 |
|---|---|
| `bool _captured; int _capturedTargets = -1;`（🔶 自己设计，单行标记） | `:50` |
| `Capture()` 幂等闸改「按 `fadeTargets.Count`」 | `:57-59`（3→3 行） |
| `RefreshTargets()` | `:167` |

### 2·2 过期文案的三处（+1 处同族）

| 位置 | 改后 |
|---|---|
| `RankedDivisionChangeWindow.cs:947-957` | 不再无条件出声；组件在就驱动、不在才出声，且文案 = 「组件早就在 `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`，缺的是挂载目标（这颗节点我们没画图、没有 `MeshRenderer`）」 |
| `BoosterPackOpenWindow.cs:94-95`（文件头） | 「✅ 已接（2026-10-27 `B9_W20WIRE`）」+ 「🔴 就地订正：原写『原版 shader 逻辑本地没有』**不成立**（`B8_AS` 已解出 `Shader Graphs/Nebula`）」 |
| `BoosterPackOpenWindow.cs:1256-1257`（`HintGaps()` **运行期日志**） | 「⑦ 背景那条 … **已接**（`_Distort_Scale`/`_Distort_Strength`；`Shader Graphs/Nebula` 已解出，但我们的图仍用 `Sprites/Default` ⇒ 今天无观感，两属性静默 no-op）」 |
| （同族）`Shell/CardDisplayEffects.cs:236-237` | 「本工程没有带 `_Reveal` 的 shader（grep = 0）」**不成立** —— `WarpforgeVFX/Shaders/WFUIBorderHighlight.shader` 就有（`_Reveal` + `_Noise_Strength_X_Speed_Y_Scale_Z`，`:34-35` 现读）；缺的是**这块图用的材质** |

### 2·3 判据来源（都是现读，不是引用别人的结论）

- 补丁原文：`资料/完全复刻/报告/B9_W14FX_特效组件两族.md` §二·3（A–E）· §五（三条过期文案）
- 组件 API：`WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs:191`（`Attach`）· `:169/:178`（`Slot`/`SlotV4`）· `:205-211`（`changeMaterial` 时序补偿）
- 控制器：`Effects/UIEffectTogglerController.cs:45`（`Rescan`）· `:54-76`（`Toggle`，含 `:57` 非运行期强制 `duration = 0`）
- 基类默认：`Effects/UIEffectTogglerBase.cs:32`（`deactivateOnDisable = true`）
- `ImageQuad` 的渲染件在**同一 GameObject** 上：`Battle/ImageQuad.cs:130-136`（`AddComponent<MeshRenderer>()`，且**每件一份材质**）⇒ `BgQuad.GetComponent<MeshRenderer>()` 成立
- clip 索引：`WarpforgeVFX/Animations/` 下 `Rank Up.anim` / `Division Up.anim` / `Level Up Card.anim` **都在**（`ls` 现读），`floatProperty1.value` 的 `script` guid 与我们 `AnimateUIMaterialProperty.cs.meta` 一致

---

## 三、没查清的部分（⛔ 都是「查不到 + 还差什么」，没有猜）

1. **原版 `Done Highlight` 的 `Toggle(option, duration)` 实参 / 触发时机**（`B9_W14FX` §三·1 也没读到）。
   ⇒ 我沿用默认 `0.3f`，触发点 = 原来那句硬切所在的 `RefreshHeader()`（语义不变）。**还差**：读 `PrebuiltPackUI.OpenAnim/SelectAnim/CloseAnim` 的方法体。
2. **`Done Highlight` 的控制器在原版挂在哪一级祖先** —— 原版该 GO 上只有 `Image / UIEffectTogglerImage / EverguildButtonMaterialModifier`，控制器在别的节点，具体哪一级要沿 `bundle_menus_assets_all` 解父链。⇒ 我们用 `_doneHl.transform.parent`，**已按铁律 18 标成「🔶 我们的选择」**（`DeckRuntime.cs:8876-8878`）。
3. **`Level Up Effect` 这个节点在我们工程里不存在** —— 判据 JSON `Resources/Art/card_display_effects.json` 的 `images` 只有 3 条：`Side Border Left` / `Side Border Right` / `Level Up Effect Down`；生成器 `工具/gen_mainmenu_effects.py:47` 的名单里也只有 `Level Up Effect Down`。
   ⇒ **还差**：先查清原版 `2DCard/Level Up Effect` 这一级容器（生成器为什么没摘它），并把 `Level Up Effect Down` 挂回它下面 —— 那两步都要动生成器 + 重跑（不在本路白名单、且要 Unity）。
4. **`Level Up Effect Down` 上 `UIBorderGlow` 与 `AnimateUIMaterialProperty` 的抢材质问题**（见 §三·3 详述）—— 原版两件都有（`hasGlow = true` + `B8_AS` 表 #6），但两者写同一份材质的**不同实例**，谁在渲染取决于最后写入者。**查不到**原版运行时到底哪一份生效（要真机看 / 要读 `EverguildButtonMaterialModifier` 那一族）。
5. **`Level Up Effect`/`…Down` 共用的外部材质 shader 名**没解（`m_FileID 5 / m_PathID 4`，`B8_AS` 记）⇒ 那两件的 `_Reveal` / 噪声 vector 喂给哪只 shader **未证**。
6. **`Shader Graphs/Nebula` / `Everguild/UI/Division Change` 在我们这儿能不能运行期取到**、取到之后 `_Distort_*` / `_Blend` 是否真在属性表里 —— 我**没跑 Unity**，验不了（`WarpforgeShaderMap.TryResolve` 那条路）。
7. **`UIEffectTogglerImage` 的账行只有 4 条**（`B9_W14FX` §一·0 已记；简报说的第 5 条在 `特效动画.tsv` 里不存在）。

---

## 四、改动清单（5 个 `.cs` + 1 份账补丁 + 本报告）

| # | 文件 | 改动 | 行数变化 | 行尾 |
|---|---|---|---|---|
| 1 | `CardPresentation/Deck/DeckRuntime.cs` | ①`:4869` 硬切 → `ToggleDoneHighlight(...)` ②类尾新增 `_doneHlCtl` / `_doneHlCtlWarned` / `ToggleDoneHighlight()` | 8866 → **8908**（+42，全部在 `:8864` 之后 ⇒ 账里最大落点 `:8859` 不动） | CRLF 保持（bare CR = 0） |
| 2 | `CardPresentation/Effects/UIEffectCanvasGroupToggler.cs` | ①`:50` 加 `_capturedTargets`（🔶）②`Capture()` 幂等闸 `:57-59`（3→3 行）③类尾 `RefreshTargets()` | 159 → **174**（+15，全部在 `:157` 之后） | LF 保持 |
| 3 | `CardPresentation/Shell/BoosterPackOpenWindow.cs` | ①`:449`/`:506`/`:903` 三个**零行数**挂钩 ②`:94-95` + `:1256-1257` 两条过期文案 ③类尾 `_bgAnim`/`_bgShakeT`/`_bgC1`/`_bgC2`/`AttachBgDistort`/`BgCurve1`/`BgCurve2`/`StepBgShake` | 1275 → **1345**（+70，全部在 `:1273` 之后 ⇒ 最大落点 `:1082` 不动） | LF 保持 |
| 4 | `CardPresentation/Shell/RankedDivisionChangeWindow.cs` | ①`:629` `string[]`→`MatTrack[]` ②`:662`/`:693` 收集曲线 ③`:947-957` ⑥块改真驱动（11→11 行）④`:450` 文件头订正（1→1 行）⑤类尾 `MatTrack` + `WarnNoMaterialTarget` | 1211 → **1233**（+22，全部在 `:1206` 之后 ⇒ 落点 `:1163` 不动） | LF 保持 |
| 5 | `CardPresentation/Shell/CardDisplayEffects.cs` | `:236-237` 过期文案（2→2 行）· **零行数变化** | 366 → **366** | LF 保持 |
| 6 | `资料/完全复刻/账/_parts/B9_W20WIRE.tsv` | **新建** 8 行 + 11 列表头 | 9 行 | LF / 无 BOM / 无裸 CR |
| 7 | `资料/完全复刻/报告/B9_W20WIRE_特效接线与过期文案.md` | 本文件 | —— | —— |

**没动的**（守住所有权）：`Shell/DraftModeWindow.cs`（W8）· `Shell/BoosterPackAnim.cs`（W8）· `Effects/UIEffectTogglerImage.cs`（W14FX 已收口）· `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs` · `Effects/UIEffectTogglerController.cs` · 一切 `Battle/**` · `Core/**` · `RuleEngine/**` · 账主表 · `CLAUDE.md` / `项目任务.md` / 各线任务总表 / 别人的报告 · 一切 `*.unity` 与 `ProjectSettings/`。

**验证**：`TMPDIR=/tmp/wf_w20 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**（改完即跑，无残留）。
`check_patch_conflicts.py` ⇒ **✅ 没有跨补丁重复键（4 份补丁）**。

⚠️ **账还没合并**：按铁律 16·8，主表只有调度台能写、且必须带 `--only B9_W20WIRE`；合并后请把本补丁移进 `_parts/_merged_B9_W20WIRE/`。

---

## 五、顺手发现

### A. （已修）`BoosterPackOpenWindow.HintGaps()` 那条 ⑦ 是**运行期日志**，比文件头更该改
文件头只是注释，`HintGaps()` 是**每次开包都打到控制台**的玩家侧事实陈述。它原来写「⇒ **没做**」，接完线后就是**当场错**。已就地订正。

### B. 📝 改动请求 —— `Shell/DraftModeWindow.cs`（归 W8，两处）

**B-1 接线（补丁 B 原文，`MakePrebuiltPackCard`/`BuildPackCard` 起于 `:2413`）**

现状：`MenuDraw.Nine(content, …, "Highlight", …)` 的**返回值没接**（该函数在 `:1766`，返回 `GameObject`，**整块在视口外会返回 null**）。

```csharp
var hl = MenuDraw.Nine(content, Tex("OctagonUI_Border_SDF"), …, "Highlight", null, null, default(Vector2));
// 原版 `Prebuilt Pack UI`：根上 `PrebuiltPackUI + UIEffectTogglerController + UIEffectTogglerImage`，
// 而 `images[0]` = **子件 `Highlight`**（不是同节点！）⇒ 必须显式给 fadeTargets。
if (hl != null)
{
    var tg = node.gameObject.AddComponent<UIEffectTogglerImage>();
    tg.fadeTargets.Add(hl);
    tg.RefreshTargets();      // ⚠️ 必须：运行期 `AddComponent` 当场跑过 `Awake → Capture()`（表还空 ⇒ 收的是根）
    var ctl = node.gameObject.AddComponent<UIEffectTogglerController>();
    ctl.Rescan();             // ⚠️ 必须：控制器 Awake 早于 toggler 挂载
    _packTogglers.Add(ctl);
}
```
```csharp
// ---- 类尾追加 ----
readonly List<UIEffectTogglerController> _packTogglers = new List<UIEffectTogglerController>();
```
⚠️ 调用时机（悬停进/出、被选中、开包/关闭）**原版是哪一拍没读到** ⇒ 接线方按 `PrebuiltPackUI.OpenAnim / SelectAnim / CloseAnim` 的调用点再核一次，别照猜。
（`RefreshTargets()` 本路已经补进 `UIEffectTogglerImage`（`B9_W14FX` 修的）—— 直接调即可。）

**B-2 过期文案（`Shell/BoosterPackAnim.cs:14-15`）**

原文（现读）：
```
//      （`AnimateUIMaterialProperty` 的材质属性，原版 shader 的逻辑本地没有）⇒ **没做**，见 §缺
```
建议改成：
```
//      （`AnimateUIMaterialProperty` 的 `_Distort_*`；`Shader Graphs/Nebula` 已解出）⇒ **已接**，见 `BoosterPackOpenWindow.AttachBgDistort/StepBgShake`
```

### C. 🔴 `Shell/DraftModeWindow.cs` 之外，**还有一处同类「假挂载」风险**（供调度台判）
`AnimateUIMaterialProperty` 的文件头自己写了：挂到「会被 `ImageQuad.SetMaterial` / `EnsureGreyCapable` 换材质」的图上 = `Update` **写进空气**（静默）。本路接的 `Booster pack Background` 建成后只有 `SetTint` 写色（不改材质）⇒ 安全。
⚠️ 但 **`Level Up Effect Down` 正是这一类**：它身上的 `UIBorderGlow.CacheMaterial()` 会 `_renderer.sharedMaterial = _inst`（`Shell/UIBorderGlow.cs:183-185`，**不论材质有没有 `_Reveal`**）⇒ 谁后跑谁占渲染件。这是我拒粘补丁 E 的第二条理由（第一条是没有 `Level Up Effect` 这个节点）。

### D. `_Reveal` 那句「grep = 0」曾让两个组件都白等
`WarpforgeVFX/Shaders/WFUIBorderHighlight.shader:34-35` 早就有 `_Reveal` 与 `_Noise_Strength_X_Speed_Y_Scale_Z`。`CardDisplayEffects.cs:236-238` 的「本工程没有带 `_Reveal` 的 shader」是**照更早的目录结构写的**（那时这批 `.shader` 还没导入？没查到）。⇒ 已就地订正；真正缺的是**图件用的材质**仍是 `CardArt.Solid()` → `Sprites/Default`。

### E. 我改到的 4 个文件都在账的 `文件:行号` 引用里 —— 逐份核过落点没漂
- `DeckRuntime.cs` 最大落点 `:8859`，插入点在 `:8864` 之后 ✔
- `BoosterPackOpenWindow.cs` 最大落点 `:1082`，插入点 `:1273` 之后；三处挂钩都是**零行数变化** ✔
- `RankedDivisionChangeWindow.cs` 落点 `:220 / :475 / :1163`，插入点 `:1206` 之后；`:450` 与 `:947-957` 都是**等行数改写** ✔
- `UIEffectCanvasGroupToggler.cs` 落点 `:91 / :100 / :130 / :137`，插入点 `:157` 之后；`Capture()` 3→3 行 ✔
（`CardDisplayEffects.cs` 在账里 0 处引用，仍按等行数改。）
