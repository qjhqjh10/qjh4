# B9_W28MOUNT · 特效挂载载体（D 段位窗两颗 / E 卡片展示窗容器 + 抢材质）

> 日期 2026-10-27 · 上一路 = `B9_W20WIRE`（它的 §三·1 / §三·2 = 本路要解的两处挂载阻塞）
> 白名单内 4 个文件（`工具/gen_mainmenu_effects.py` · `Resources/Art/card_display_effects.json`（生成物） ·
> `Shell/CardDisplayEffects.cs` · `Shell/RankedDivisionChangeWindow.cs`）+ 本报告 + `账/_parts/B9_W28MOUNT.tsv`
> **一行 Unity 没跑、一次 git 没动。**

---

## 一、结论

### 1·1 一句话

**两处挂载阻塞都解开了 —— 但"挂上去"不等于"看得见"：D 的两颗原版 `m_Sprite = 0`（图由服务端喂），E 的两件原版
用的材质 `Border Effect For Level up` 还没导进工程 ⇒ 今天这四处仍**零观感**，而**根因已经不是挂载**。**

| 项 | 原判（`B9_W20WIRE` §三） | 本路结果 |
|---|---|---|
| **D** 段位窗 `Division Image` / `Rank Icon` | 只用 `MenuDraw.Node` 建空节点，没有 `ImageQuad`/`MeshRenderer` ⇒ `Attach(Renderer,…)` 无处可挂 | ✅ **载体已建**（`BuildDivisionImage` / `BuildRankIcon`，tint **α = 0**）+ `Attach(… Slot("_Blend", 0f))`。**图缺失的原因 = 服务端下发（P3）**，不是我们漏了 |
| **E-①** `Level Up Effect` 容器 | 「这个节点我们根本没建」 | ✅ **切实验证：原版有它**（`2DCard/Level Up Effect`，`m_IsActive = false`，是 `…Down` 的父件）。已进生成器名单 → 已重建，子件挂回它下面 |
| **E-②** 两组件抢材质 | 担心 `UIBorderGlow` 顶掉 AMP 的材质 ⇒ 假挂载 | ✅ **原版本来就同时挂两个**，次序 = `Image → UIBorderGlow → [Cinemachine…] → AnimateUIMaterialProperty` ⇒ **照抄原版**。且 `UIBorderGlow.playOnEnable` 两件都是 **0**、本层**没有任何 `Play()` 调用点** ⇒ 静止时它根本不 `CacheMaterial()`，**不抢** |
| **附带** `Level Up Effect Down` 的尺寸 | 直接拿 `m_SizeDelta` 的绝对值（110.03 × 121.04）当尺寸 | 🔴 **修正**：它的锚点是 `(0,0)-(1,1)` **stretch** ⇒ 真尺寸 = 父矩形 + delta = **506.07 × 716.93**（父局部）⇒ 折成 `2.3003 × 3.2588`（`2DCard` 单位）。**差 37 倍** |

### 1·2 两处挂载阻塞的「解开方式」逐条

**D（段位窗）** —— 原版那两颗的 `Image` 与 `AnimateUIMaterialProperty` **在同一个 GameObject 上**
（AMP 的 `image` PPtr → 同 GO 的 `Image`）。所以载体必须长在那颗节点**自己**身上：

- `MenuDraw.Node(transform, "Division Image", DivisionR)` → **`MenuDraw.Rect(transform, CardArt.Solid(), DivisionR, "Division Image", QDivision, α=0)`**
  —— `MenuDraw.Rect` 建出来的 GameObject 名字就是 `Division Image`、带 `ImageQuad + MeshFilter + MeshRenderer`
  ⇒ 节点名/位置/尺寸全不变，只是多了渲染件。`NodeAt("Division Image")` 与 `GetComponent<ImageQuad>()` 都能命中。
- 同上建 `Rank Icon`。
- `WarpforgeVFX.AnimateUIMaterialProperty.Attach(quad.GetComponent<MeshRenderer>(), Slot("_Blend", 0f))`。
- 🔴 **为什么 tint 的 α 是 0**：原版 `m_Sprite = 0`，而 uGUI 对空 sprite 画的是「全白贴图 × `m_Color`」——
  那是**纯白块**（851×851）。但原版 `Open()` 在 `sprite == null` 时 `SetActive(false)` ⇒ **屏幕上什么都没有**。
  我们的等价物 = 有个"看不见但存在"的 quad。⛔ 不是 `MenuDraw.Node` 那种没有渲染件的空节点。
- 失败兜底：`MenuDraw.Rect` 整块落在裁切框外会返回 null（既有语义）⇒ 退回 `MenuDraw.Node` 并 **`WarnChannel` 出声**
  （那一档丢的是**材质挂载点**，不是图 —— 不静默）。

**E-①（容器）** —— 逐项核对结果（不是看字段名，是按 `m_Children` 树解）：

```
Level Up Effect          active=False   parent = 2DCard
   ├ RectTransform  anchor(0.5,0.5)-(0.5,0.5)  sizeDelta 616.1031 × 837.9722  localScale 0.0045454
   ├ CanvasRenderer
   ├ UnityEngine.UI.Image      m_Sprite = 1588741661991143589(FileID 6)  m_Material = 4(FileID 5)
   ├ UIBorderGlow              animTime 2.0 · animDelay 0 · playOnEnable 0
   ├ Cinemachine.CinemachineIndependentImpulseListener
   └ AnimateUIMaterialProperty  image→本 GO 的 Image · _Reveal −0.5 · changeMaterialOnAnimation = 4(FileID 5)

Level Up Effect Down     active=True    parent = Level Up Effect（唯一子件）
   ├ RectTransform  anchor(0,0)-(1,1) stretch  sizeDelta −110.032 × −121.037  localScale 1
   ├ CanvasRenderer
   ├ UnityEngine.UI.Image      m_Sprite = 8880629346401627699(FileID 7)  m_Material = 4(FileID 5)
   ├ UIBorderGlow              animTime 1.0 · animDelay 0 · playOnEnable 0
   └ AnimateUIMaterialProperty  image→本 GO 的 Image · _Reveal −0.5
                               · floatProperty2 = _Noise_Strength_X_Speed_Y_Scale_Z (0.02, −0.1, 8.17, 0) useVector4
                               · changeMaterialOnAnimation = 4(FileID 5)
```

⇒ 生成器 `TARGETS` 补 `Level Up Effect`、新增 `IMAGE_NODES` 常量（父件排在子件之前），重生成 DTO（3 条图件 → **4 条**）。

### 1·3 E-② 的裁定：**照抄原版**

原版**同时**挂着 `UIBorderGlow` 与 `AnimateUIMaterialProperty`，次序固定（`m_Component` 序）。
所以本路**照原版次序挂**，并逐条证明"不抢"：

1. `UIBorderGlow.CacheMaterial()`（唯一会写 `_renderer.sharedMaterial = _inst` 的地方，`Shell/UIBorderGlow.cs:167-187`）
   **只在 `Play()` 里被调**（`Play()` :120-126 → `CacheMaterial()`）。
2. 两件的 `playOnEnable` 都是 **0** ⇒ `OnEnable()`（`:103-106`）什么都不做。
3. 本层（`Shell/CardDisplayEffects.cs`）**没有任何 `Play()` 调用点**；原版那条 legacy clip `Level Up Card`
   的曲线/事件里也没有它（`Effects/CardDisplayLevelUp.cs` 文件头 §驱动源 逐条现读：两条事件 = `ChangeFrame` / `AnimationFinished`，
   曲线 = `floatProperty1.value` / `m_Color.a` / `m_IsActive` / `m_LocalScale`）。
4. ⇒ 静止时渲染件一直是 **AMP 克隆的那一份**，`Update` 写得进去 —— **不是假挂载**。
   （万一将来有人调 `Play()`，那也**是原版行为**：`UIBorderGlow` 会克隆当前材质顶上去，AMP 转而写到旧实例。
   ⛔ 别"修"成每帧比对 `sharedMaterial` —— 那是猜出来的修法，见 AMP 文件头。）

### 1·4 今天为什么还是零观感（如实记）

| 件 | 缺什么 | 判据 |
|---|---|---|
| `Level Up Effect` / `…Down` | 材质 `Border Effect For Level up` **没导进工程** | `scenes_mainmenuwarpforge/Material/Material_4.json`：`_Reveal −0.5` · `_Noise_Strength_X_Speed_Y_Scale_Z (0.02, −0.1, 8.17, 0)` · `_ColorMain (0.9893, 0, 1, 1)` · `_ColorSecondary (0.15395, 0, 1, 1)`；shader = **`Everguild/UI/UI Border Highlight Appear Pulsating Scale`**（同 bundle 的 `Shader_-4463273156108355430.json`，属性表里就有 `_Reveal`）。我们工程里早有等价 shader `WarpforgeVFX/UI/UI Border Highlight`（`WFUIBorderHighlight.shader:23`，含同名 `_Reveal` / `_Noise_Strength_…`）⇒ **下一步只是做一张 `.mat` + 进 `Resources/`**，`AnimateUIMaterialProperty` 一行都不用改 |
| 同上 + 段位窗两颗 | 那几块图当前用 `Sprites/Default` ⇒ `_Reveal` / `_Noise` / `_Blend` **不存在** ⇒ `Material.SetFloat` 静默 no-op（原版同，AMP 不报警） | `AnimateUIMaterialProperty.cs:85-89` |
| `Division Image` / `Rank Icon` | 图（服务端） | P3 |

---

## 二、证据（文件:行号）

### 2·1 判据来源（全部现读，可复跑）

| 内容 | 出处 |
|---|---|
| `Level Up Effect` / `…Down` 的组件序、rect、组件字段 | `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/GameObject/Level Up Effect.json`（6 个组件 pid 1126/902/2545/2432/2619/2071）· `…/Level Up Effect Down.json`（5 个 pid 1422/684/2057/2236/2204） |
| 同上，逐组件类名走 `m_Script` → `bundle_Waprforge_monoscripts/MonoScript` | `MonoBehaviour_2545`=Image · `2432`=UIBorderGlow · `2619`=`Cinemachine.CinemachineIndependentImpulseListener` · `2071`=AnimateUIMaterialProperty；Down 侧 `2057`/`2236`/`2204` |
| 父链 | `Level Up Effect` → `2DCard`(scale 1, sizeDelta 2.0927×3.3313) → `CardUI`(**scale 250**) → … ⇒ `2DCard` 的 250 px/单位 |
| `Division Image` / `Rank Icon` 的组件序与 AMP 字段 | `bundle_menus_assets_all/GameObject/Division Image.json` + `MonoBehaviour_-7044926665261084338`(Image) / `MonoBehaviour_4555674787176354126`(AMP) / `Canvas_6271197376907022670`；`GameObject/Rank Icon.json` + `MonoBehaviour_-691165018575761074` / `MonoBehaviour_-6557930150585593522` |
| `Rank Icon Glow`（见 §四·4） | `bundle_menus_assets_all/GameObject/Rank Icon Glow.json`：Image `m_Sprite = 0` · `m_Color (1, 0.9168, 0, 1)` · `m_Material` pid `-8464978790139598553` |
| 材质 + shader | `bundle_scenes_scenes_mainmenuwarpforge/Material/Material_4.json`（`m_Name` = `Border Effect For Level up`）· `bundle_shaders_assets_all/Shader/Shader_-4463273156108355430.json`（`m_ParsedForm.m_Name` = `Everguild/UI/UI Border Highlight Appear Pulsating Scale`，props 含 `_Reveal` / `_Noise_Strength_X_Speed_Y_Scale_Z`） |
| 我们的等价 shader | `WarpforgeVFX/Shaders/WFUIBorderHighlight.shader:23`（`Shader "WarpforgeVFX/UI/UI Border Highlight"`）· `:34` `_Reveal ("Reveal", Range(-1,1))` · `:32` `_Noise_Strength_X_Speed_Y_Scale_Z` |
| `ImageQuad` 的渲染件在**同一 GameObject**、每件一份材质 | `Battle/ImageQuad.cs:127-137` |
| `UIBorderGlow` 唯一写 `sharedMaterial` 的地方 = `CacheMaterial()`，只被 `Play()` 调 | `Shell/UIBorderGlow.cs:167-187`（`_renderer.sharedMaterial = _inst` 在 `:185`）· `Play()` `:120-126` · `OnEnable()` `:103-106` |
| AMP 的挂载适配层（`image.material` ↔ `Renderer.sharedMaterial`）与「三条换材质口」 | `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs:311-331` · 文件头 `:55-62` |

### 2·2 本路改到的位置

**`Shell/RankedDivisionChangeWindow.cs`（LF，1233 → 1295 行）**

| 内容 | 行 |
|---|---|
| `_division = BuildDivisionImage();`（原来是 `MenuDraw.Node(...)` 那一行，**1→1**） | **`:276`** |
| `_rankIcon = BuildRankIcon();`（同上，**1→1**） | **`:293`** |
| `WarnNoMaterialTarget()` 的文案就地订正（3→3 行） | `:1230` |
| 判据注释块 + `BuildDivisionImage()` / `BuildRankIcon()` | `:1233` 起 · `:1259` · `:1278` |

🔴 **账里本文件的落点是 `:475`（`PromoteDelay`）与 `:1163`（`ToggleSeal`）** ⇒ 两个建件口在 `Build()` 里各占**一行**、实现全追加在类尾，
**行号零漂**（已复核：`:475` 仍是 `public static float PromoteDelay(int steps)`、`:1163` 仍是 `public void ToggleSeal(int i, bool on)`）。

**`Shell/CardDisplayEffects.cs`（LF，366 → 455 行）**

| 内容 | 行 |
|---|---|
| `_warnedNoSprite` / `_warnedParent`（两个"只出声一次"的闸） | `:46` / `:48` |
| 图件循环改「按 `parent` 嵌套、父件先建」 | `:104-127` |
| `BuildImage` 改为**返回 `GameObject`** + `hasSprite` 两档 tint | 声明 `:237` · 两档 `:264-275` |
| `AnimateUIMaterialProperty` 挂载（在 `UIBorderGlow` 之后 = 原版次序） | `:301-330` |
| `CdeImage` 新字段（`parent` / `hasSprite` / `hasAmp` / 两组槽） | `:436-454` |

**`工具/gen_mainmenu_effects.py`（LF，406 → 489 行）**

| 内容 | 行 |
|---|---|
| `TARGETS` 补 `Level Up Effect`（附三条后果） | 名单里 `"Cog", "Craft",` 之后 |
| `IMAGE_NODES = (… Level Up Effect …)` 常量 | 紧跟 `TARGETS` |
| `rect_in_card()`：`sizeDelta` / `anchoredPosition` 的**父件局部 → 本层单位**换算（含 stretch 与累计 `localScale`） | `emit_runtime` 内 |
| DTO 新增 11 个键（`parent` / `anchor*` / `hasSprite` / `hasAmp` / `ampFloat*`） | 同上 |
| dry 打印的「21 条」改成 `len(TARGETS)`（原来写死） | `main()` |

### 2·3 生成物与幂等

```
写入 …/Resources/Art/mainmenu_effects.json （275854 字节）
写入 …/Resources/Art/card_display_effects.json （36779 字节；10 颗粒子 / 4 条图件）
连跑两遍逐字节相同：card_display_effects.json IDENTICAL · mainmenu_effects.json IDENTICAL
```

新 DTO 里 4 条图件的关键值（现读，可与 §2·1 逐一对照）：

| name | parent | sizeX × sizeY | posY | hasSprite | hasGlow / glowTime | hasAmp / 槽 |
|---|---|---|---|---|---|---|
| `Side Border Left` | `Borders` | 933.910 × 2280.330 | −19.4 | false | false | false |
| `Side Border Right` | `Borders` | 933.910 × 2280.330 | 0.0 | false | false | false |
| `Level Up Effect` | `2DCard` | **2.80047 × 3.80896** | −0.02342 | true | true / **2.0** | true / `_Reveal` −0.5 |
| `Level Up Effect Down` | `Level Up Effect` | **2.30032 × 3.25880** | **0.00159** | true | true / **1.0** | true / `_Reveal` −0.5 + `_Noise_… (0.02,−0.1,8.17,0)` |

（`Side Border*` 两条**逐字节没变** —— 它们的父件 `Borders` 不在名单里 ⇒ 走既有近似。）

---

## 三、没查清的部分（⛔ 都是「查不到 + 还差什么」）

1. **原版 `Division Image` 还带一个自有 `Canvas`**（`Canvas_6271197376907022670`：`m_RenderMode 2`(WorldSpace) ·
   `m_OverrideSorting 1` · `m_SortingOrder −1` · `m_AdditionalShaderChannelsFlag 0`）⇒ 原版那一颗是**单独一层、排在最底**。
   我们没有 uGUI Canvas ⇒ 只能用 `QDivision = 3412` / `QRank = 3413` 的渲染队列近似。
   **这条没验**（今天 α = 0 不显示，看不出差别）⇒ 等图到了要复核「它到底压不压得住 `Menu Draw` 之外的件」。
   ⚠️ 另注：`m_AdditionalShaderChannelsFlag = 0` ⇒ **没有 TexCoord1** ⇒ 原版那颗上就算挂 `UIBorderGlow` 也读不到 `uv0.zw`
   （实际它**没挂** UIBorderGlow，所以这条只是旁证）。

2. **`Side Border Left/Right` 的空间仍是既有近似**（本路有意不动）：它们的父件 `Borders` 是 `Safe area Only Horizontal` 下的
   1920×1080 **画布**节点（`Game UI` 那一级 `m_LocalScale = 0` —— 显然由运行时脚本改），
   和 `2DCard` 的 **250 px/单位**不是一把尺。所以现在 `sizeX/sizeY = 933.910 × 2280.330`、`posX = ±1426`
   被当成"本层单位"用，量级几乎肯定不对。**还差**：`Game UI` 的 scale 是谁在运行时设的（`TransformScalerBySmallScreenUI`？），
   以及 `Borders` 相对 `2DCard` 的真实比例。**今天不可见**（整层出厂关着、且 `_fx` 没有任何地方 `SetActive(true)`）。

3. **`Level Up Effect` 上的 `Cinemachine.CinemachineIndependentImpulseListener` 我们没有对应物** ——
   菜单层没有 Cinemachine（`Effects/CardDisplayLevelUp.cs` 已裁定等价物 = 「抖窗容器」）。
   本路**没有**给它造占位组件（造了就是假挂载）。⇒ 「相机遇冲量」这一跳仍是 `CardDisplayLevelUp` 的 §自己设计 ①。

4. **那张材质要不要用我们自己的等价 shader 造**（`WarpforgeVFX/UI/UI Border Highlight` vs 原版
   `Everguild/UI/UI Border Highlight Appear Pulsating Scale`）—— 原版 shader 的**编译字节码在**、源码被剥，
   我们那份是**同名属性 + 同类观感**的重建（那份 shader 自己的文件头 `:18-19` 写着「**没有**逐指令重建」）。
   ⇒ 要真出画面，得先拍板「用重建 shader 还是走运行时 bundle 原件」，**这不在本路白名单**（要新建 `.mat` + 改
   `工具/import_mainmenu_effect_mats.py` 的 `MAP`，那个文件不归我）。

5. **原版到底有没有人调 `UIBorderGlow.Play()`**（这两件）—— 我在 **ILSpy 全树 + `decomp_full` 的方法名**里都没找到
   指向这两颗的 `Play()` 调用点，而 `playOnEnable = 0` ⇒ **判「静止不播」是有据的**；
   但「运行时会不会被某个我们没解出来的协程调」**没有正面证据**（要靠真机）。
   ⇒ 结论按「静止不播」写，影响面只在"顶上材质的那一瞬"。

---

## 四、改动清单

| # | 文件 | 改动 | 行数变化 | 行尾 |
|---|---|---|---|---|
| 1 | `工具/gen_mainmenu_effects.py` | ①`TARGETS` 补 `Level Up Effect` ②新增 `IMAGE_NODES` ③`emit_runtime` 图件段重写（`rect_in_card()` + 11 个新键）④dry 打印去掉写死的「21」 | 406 → **489** | LF 保持（bare CR = 0） |
| 2 | `Resources/Art/card_display_effects.json` | **重新生成**（3 → 4 条图件；粒子 10 条逐字节不变） | 34,818 → **36,779** 字节 | 生成器写（LF、无 BOM） |
| 3 | `Resources/Art/mainmenu_effects.json` | **重新生成**（`targets` 21 → 22 条 + 新增 `Level Up Effect` 那一条 item） | 270,779 → **275,854** 字节 | 同上 |
| 4 | `Shell/CardDisplayEffects.cs` | ①`:104-127` 图件嵌套 ②`BuildImage` 返回 `GameObject` + `hasSprite` 两档 tint + `AnimateUIMaterialProperty` ③`CdeImage` 新字段 ④两处注释订正 | 366 → **455**（+89） | LF 保持 |
| 5 | `Shell/RankedDivisionChangeWindow.cs` | ①`:276`/`:293` 换建件口（**1→1 行**）②`:1230` 文案就地订正（3→3 行）③类尾 `BuildDivisionImage`/`BuildRankIcon` | 1233 → **1295**（+62，全部在 `:1231` 之后 ⇒ 落点 `:475`/`:1163` 零漂） | LF 保持 |
| 6 | `账/_parts/B9_W28MOUNT.tsv` | **新建** 4 行 + 11 列表头（`AnimateUIMaterialProperty` 的 `OnEnable()` / `Update()` / `Update(Material material)` / `SetMaterial(Material material)`） | 5 行 | LF / 无 BOM / 无裸 CR |
| 7 | `账/_parts/_gen_B9_W28MOUNT.py` | 补丁的生成器（**不带 `--apply` 只打印用法**，从主表读行、只追加备注列 ⇒ 不用手抄长行） | —— | LF |
| 8 | `资料/完全复刻/报告/B9_W28MOUNT_特效挂载载体.md` | 本文件 | —— | —— |

**没动的**（守住所有权）：`Shell/UIBorderGlow.cs`（**W22 独占，正在改** —— 本路一个字没碰，也没提改动请求：按 §1·3 的判据，
它现在这套「只有 `Play()` 才顶材质」的行为**与原版一致**，没有要改的地方）·
`WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs` · `Effects/*` · `Battle/**` · `RuleEngine/**` ·
`工具/import_mainmenu_effect_mats.py`（不在白名单，要导材质得另派）· 账主表 · `CLAUDE.md` / `项目任务.md` / 各线任务总表 / 别人的报告 ·
一切 `*.unity` 与 `ProjectSettings/`。

**验证**

- `TMPDIR=/tmp/wf_w28 bash d:/4/Unity/工具/typecheck.sh`
  - 改完当次：**运行时 0 / 编辑器 0**。
  - 收工前又跑两次：**9 条错，全部在 `Assets\RuleEngine\Core\DeckRules.cs`**（`CS0103: 当前上下文中不存在名称"SameCardKey"`，`:787/:795/:796`），
    **那不是我负责的文件**（RuleEngine 归 W27 那条线，`git status` 显示它正在被改）⇒ 按铁律 13·3 的判据，**这不是本路的错**；
    我改的 3 个文件在错误列表里**一条都没有**。
- 生成器幂等：连跑两遍，两个产物**逐字节相同**（见 §2·3）。
- `python -I -X utf8 工具/check_patch_conflicts.py` ⇒ **✅ 没有跨补丁重复键（2 份补丁）**。

⚠️ **账还没合并**：按铁律 16·8，主表只有调度台能写、且必须带 `--only B9_W28MOUNT`；合并后请把本补丁移进
`_parts/_merged_B9_W28MOUNT/`（否则下次整目录合并会把这一版盖回去）。

---

## 五、顺手发现

### 1. `Resources/Art/` 整个目录被 `.gitignore` 忽略 —— 两个产物**不进 git**
`.gitignore:93: /Unity/MyGame/Assets/CardPresentation/Resources/Art/` ⇒ `card_display_effects.json` /
`mainmenu_effects.json` 都是**可再生品**，改它们只能在"生成器幂等"这一条上验收（本路已验）。
⇒ 下一个人不要以为 `git status` 看不到它们就等于没改过。

### 2. 🔴 `Level Up Effect Down` 的尺寸原来差 **37 倍**（本路已修）
`m_AnchorMin/Max = (0,0)-(1,1)` + `m_SizeDelta = (−110.032, −121.037)` = **stretch**，
`rect = 父矩形 + sizeDelta`。原来的实现直接把 `|sizeDelta|` 当尺寸 ⇒ **110.03 × 121.04**；
真值是 **506.07 × 716.93**（父件局部）。折成 `2DCard` 单位后差 37 倍。
⚠️ 今天看不出来（整层出厂关着），但**图/材质一到就会是个小得离谱的件**。
同一条换算现在也覆盖了 `Level Up Effect` 自己（`localScale 0.0045454` 已折进尺寸）。

### 3. `WarnNoMaterialTarget()` 的文案已过期（本路就地订正，3→3 行）
原写「缺的是挂载目标（这颗节点我们没画图、没有 `MeshRenderer`）」—— 本路之后 `Division Image` / `Rank Icon`
**已经有载体**了。改成「`Division Image`/`Rank Icon` 已由 `BuildDivisionImage`/`BuildRankIcon` 补上载体；
走到这里说明该 path 的节点上没有 `ImageQuad`」。**行为零变化**（只在真找不到组件时才打），只订正事实。

### 4. 📌 已查到但**没做**的同类缺口：`Rank Icon Glow` 仍然没有载体
现读 `bundle_menus_assets_all/GameObject/Rank Icon Glow.json`：`m_IsActive = false` · rect `68×68` ·
Image **`m_Sprite = 0`** · `m_Color (1, 0.9168, 0, 1)`（黄）· `m_Material` pid `-8464978790139598553`
（**那张材质带 `_Noise_Strength_X_Speed_Y_Scale_Z`**，与 `Border Effect For Level up` 同一族）。
⇒ 它是「靠材质 `_Reveal` 揭开的黄色辉光」，**不是**纯色块（纯色块只是 `m_Sprite = 0` 时的 uGUI 兜底观感）。
今天 `ApplyChannels` ③ 对 `Division Image/Rank Icon/Rank Icon Glow` 这条 alpha 曲线**每次都出声**
（「这一颗没有 `ImageQuad` ⇒ 画不出来」）—— 那条**仍然成立**，因为本路只给 `Division Image` / `Rank Icon` 建了载体。
**为什么没顺手做**：给它建载体就得决定「用 `Sprites/Default` 画一块纯黄（**会明显不对**）还是等那张材质」，
而材质这条路要新建 `.mat` + 改 `工具/import_mainmenu_effect_mats.py`（**不在本路白名单**）⇒ 判据不足，按红线停手。
**下一步**：与 §1·4 那张材质**同一批**做（都是 `_Reveal` 族），一次把三件（LUE / LUE Down / Rank Icon Glow）的材质配齐。

### 5. ③ alpha 通道对 `Division Image`/`Rank Icon` 的"画不出来"告警现在不再触发（**不是静默**）
它们有 quad 之后，`BaseQuad` 缓存的基准 α = **0**（我们给的载体 α）⇒ 曲线乘上去仍是 0。
**观感与原版一致**（原版 `m_Sprite = 0` 时 `Open()` 直接 `SetActive(false)`，屏幕上什么都没有），
而且「图来自服务端」这件事仍由 `Build()` 里的 `NoteNoArt("Division Image"/"Rank Icon", …)` **每次出声** ⇒ 没有丢信息。

### 6. `MenuDraw.Rect` 建出来的节点是**裸 `Transform`**（没有 `RectTransform`）
本路把 `Division Image` / `Rank Icon` 从 `MenuDraw.Node`（`RectTransform`）换成了 `MenuDraw.Rect` ⇒ 这两颗**没有 `RectTransform`** 了。
逐条核过影响面：`_division` 只当父件用（`MenuDraw.Node` 的 `Local()` 读 `position/lossyScale`，不读 rect）；
`NodeScaleForTest` 读 `localScale`；`NodeActiveForTest` 读 `activeSelf`；`DivisionNode`/`RankIconNode` 两个属性**全仓 0 使用者**

> 🔴 **2026-10-27 就地订正（铁律 5 · `D4`/`F1` 现核）**：原文「`DivisionNode`/`RankIconNode` 两个属性**全仓 0 使用者**」**不成立** —— `DivisionNode` **有 1 个使用者**（`Editor/MainMenuScene.cs` 段位窗那条「不画」断言，正是本会话因加载体而变红的那条）；只有 `RankIconNode` / `RankGlowNode` 是 0 使用者。错因：当时只 grep 了属性名在 `Shell/` 下的引用、没扫 `Editor/`。；
`Editor/ShellScene.cs` 对这两颗的断言只有 `NodeScaleForTest("Division Image")`（`:9551`）与 `NodeActiveForTest`。
⚠️ 但这是一处**结构偏离**（原版有 `RectTransform`）—— 记在这里，将来若有人要读它们的 `sizeDelta` 会读到 0。

### 7. `mainmenu_effects.json` 的条目从 21 变 22（R2 那 21 条名单本身我没动）
`TARGETS` 是 R2 普查的"环境光效"名单，我把 `Level Up Effect` 补进去是为了让它进 `items`（判据正本要能查到这颗）。
若将来有人拿 `len(targets)` 当"主菜单环境光效 = 21"的证据，注意现在是 22。
