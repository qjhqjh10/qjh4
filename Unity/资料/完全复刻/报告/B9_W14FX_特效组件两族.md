# B9_W14FX · 特效组件两族（`UIEffectTogglerImage` / `AnimateUIMaterialProperty`）执行报告

> 日期 2026-10-27 · 域 = `账/特效动画.tsv` · 白名单 = `CardPresentation/Effects/UIEffectTogglerImage.cs`
> + `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`（账里的落点就是这个路径，见 §一·0）
> + 本报告 + `账/_parts/B9_W14FX.tsv` · **一行 Unity 没跑、一次 git 没动**

---

## 一、结论

### 1·0 先说两个与简报不符的事实（都现读，⛔ 别按简报找）

| 简报写的 | 实测 | 处置 |
|---|---|---|
| `AnimateUIMaterialParameters` | 账里**没有**这个类名；真名 = **`AnimateUIMaterialProperty`**（4 个方法与简报逐字对上：`OnEnable()` / `SetMaterial(Material)` / `Update()` / `Update(Material)`） | 按账里的真名做；落点 = `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`（**已存在**，2026-10-26 `B8_AS` 建的），**不是**简报说的「新建 `Effects/AnimateUIMaterialParameters.cs`」 |
| `UIEffectTogglerImage` **5 条** | `特效动画.tsv` 全表只有 **4 条**（行 462–465：`AnimationFinished()` / `DoUpdate(…)` / `Toggle(…)` / `ToggleGameObjects(…)`）；第 5 条**没找到**（`grep -n UIEffectTogglerImage 特效动画.tsv` = 4 行） | 4 条全做；第 5 条列入 §三「没查清」 |

### 1·1 总判

**两族的「组件本体」都已经完整且逐句对齐原版 —— C 的唯一原因是【零挂载点】（接线没做），不是缺实现。**

- 8 条 C 行（`UIEffectTogglerImage` 4 + `AnimateUIMaterialProperty` 4）我用 `decomp_full/*.c` 逐句复核了一遍，
  **没有一条需要改语义**（逐条见 §一·2）。
- 但复核中发现 **2 处真实的「接线阻塞缺陷」**，都在我们的**适配层**（原版没有这两个 API），已修：
  1. `UIEffectTogglerImage.Capture()` 用 `_captured` 死锁 ⇒ **按 `B8_AS` §三·2 那份接线写法接线会静默淡错对象**
     （`AddComponent` 时 `Awake` 先收了一轮，之后再 `fadeTargets.Add(子件)` 永远收不进来）。
  2. `AnimateUIMaterialProperty.Attach(..., changeMaterial)` 的 `changeMaterialOnAnimation` **赋值太晚**
     （`AddComponent` 当场触发 `OnEnable → CreateInstanceMaterial()`，那时它还是 null ⇒ 克隆源退化成 `originalMaterial`）。
- **C 没有变成 A，因为接线落在本代理白名单外**（`Deck/DeckRuntime.cs` / `Shell/DraftModeWindow.cs` /
  `Shell/BoosterPackOpenWindow.cs` / `Shell/RankedDivisionChangeWindow.cs` / `Shell/CardDisplayEffects.cs`）。
  ⇒ **可粘的接线补丁我写在 §二·3**，账补丁里也点名了落点，接线方照着粘即可（不需要再查判据）。

### 1·2 逐条对账（8 行，格式 = 账行 | 原版逐句 | 我们原来 | 改成什么 | 怎么验）

| 账行 | 原版逐句（🥇 `decomp_full/*.c`） | 我们原来 | 改成什么 | 怎么验 |
|---|---|---|---|---|
| `UIEffectTogglerImage.Toggle(bool option, float duration)` | `+0x21 = option`；`if (0 < duration) 传 true`；尾调 `ToggleGameObjects`（`…__Toggle.c` 全文 5 行） | 逐句一致（`instant = !(0f < duration)`），但 `Capture()` 的 `_captured` 死锁 | `Capture()` 改按 `fadeTargets.Count` 判重；类尾新增 `RefreshTargets()`（`:150`，🔶 自己设计） | ① 接线后：`AddComponent` → `fadeTargets.Add(子件)` → `RefreshTargets()` → `Toggle(true)`，读 `TargetCount == 1` 且淡的是**子件**；② 不调 `RefreshTargets()` 时 `TargetCount` 仍是 1 但目标是**根**（= 那个 bug 的复现口径） |
| `UIEffectTogglerImage.DoUpdate(float currentTime, float normalizedTime)` | `n` 先夹 [0,1]（`if(n<0)n=0; else if(DAT<n)n=DAT`）· `to = (+0x21)?1:0` · `from = 1-to` · 逐图 `c=image.color; c.a=(to-from)*n+from; image.color=c` | 逐句一致（绝对覆盖、RGB 取当前值） | **没改** | 灭自证口径：基线 α=0.5 的图在 `n=1,to=1` 必须读到 **1.0**（乘性写法只会给 0.5） |
| `UIEffectTogglerImage.AnimationFinished()` | `if (+0x20 /*deactivateOnDisable*/ && !+0x21) ToggleGameObjects(false)`（⚠️ `.c` 首行名是 `UIEffectCanvasGroupToggler__AnimationFinished` = ICF 同体折叠，**体正确**） | 逐句一致 | **没改** | `deactivateOnDisable=1` + `Toggle(false)` + 走完 → 目标 `activeSelf == false`；`Toggle(true)` 走完 → `activeSelf == true` 且 α==1 |
| `UIEffectTogglerImage.ToggleGameObjects(bool option)` | 逐个 `images[i].gameObject.SetActive(option)` | 逐句一致（原版 `private`，我侧 `public` 只为接线/自检） | **没改** | 同上第 2 条 |
| `AnimateUIMaterialProperty.OnEnable()` | `if(!+0x28) return;` → `CreateInstanceMaterial()` → `if(+0x78 != null) image.material = +0x78`（**重复赋值照抄**，re-enable 全靠它） | 逐句一致 | **没改**（只在 `Attach` 里补了时序补偿，见下） | `AddComponent` 后读 `InstancedMaterial != null` 且 `sharedMaterial == InstancedMaterial`；`OnDisable` → `sharedMaterial == OriginalMaterial`；再 enable → 又回到 `InstancedMaterial` |
| `AnimateUIMaterialProperty.SetMaterial(Material material)` | `CreateInstanceMaterial(); *(+0x78) = 形参;` + 写屏障（ILSpy `object obj = this + 120; obj = material;`）· **不回写 `image.material`** | 逐句一致 + 🔶② 只销毁自己克隆的那份 | **没改** | 调 `SetMaterial(m)` 后 `InstancedMaterial == m`，而 `sharedMaterial` **仍是旧的**（原版行为，⛔ 别修） |
| `AnimateUIMaterialProperty.Update()` | `if(+0x78 == null) return` → 两槽**各自**先 `Equals(default)` 判空（空槽跳过）→ `useVector4 ? SetVector : SetFloat` | 逐句一致 + 🔶① 空槽判定换成「名字空 + 三值零」 | **没改** | 夹具：槽1 空 / 槽2 `_Blend=0.5` → 只发生 1 次 `SetFloat("_Blend", 0.5)` |
| `AnimateUIMaterialProperty.Update(Material material)`（嵌套 struct） | `+0xc` = useVector4 ? `SetVector(+0x0, +0x10..0x1c)` : `SetFloat(+0x0, +0x8)` | 逐句一致（结构体布局逐偏移核过） | **没改** | 两条分支各一条断言（`SetFloat` / `SetVector` 的实参对） |

### 1·3 两处修复（都在**我们的适配层**，原版没有这两段代码）

**① `UIEffectTogglerImage.Capture()` 的死锁（真实接线阻塞）**

判据链（说明「原版没有这个问题」）：
- 原版字段是 `private Image[] images`（`out_cpp2il_dev_ilspy/UIEffectTogglerImage.cs:7`）—— **序列化数组，构建期定死**，
  运行期既不能 `Add` 也不能换 ⇒ 「收一次就锁住」在原版**永远是对的**。
- 我们把它换成了 `public List<GameObject> fadeTargets`（运行时填）⇒ 锁住就是 bug。
- 触发路径（就是 `B8_AS` §三·2 给的那份接线写法）：`AddComponent`（**运行期当场 `Awake → Capture()`**，那时表是空的
  ⇒ 收的是**自己**）→ 之后 `fadeTargets.Add(highlightNode)` → **被 `if (_captured) return;` 吃掉**。
- 后果：`Prebuilt Pack UI` 的 `images[0]` 在**子件** `Highlight`（`B8_AS` 现读，子件 `GO 3832911137508339186`），
  我们则会淡 + `SetActive` 到**根** ⇒ 整个包卡卡片被关掉（不是高亮描边）。**静默、且看起来像「接好了」**。

改法：`Capture()` 的幂等闸从「bool 死锁」改成「`fadeTargets.Count` 变了就重收」+ 类尾 `RefreshTargets()`（同数**替换**目标时用）。
⚠️ 编辑/批处理模式下 `AddComponent` **不触发 `Awake`** ⇒ 那种情况下本来就按第一帧的完整表收，`RefreshTargets()` 是幂等的，两种模式都对。

**② `AnimateUIMaterialProperty.Attach(...)` 的克隆源时序（真实接线阻塞）**

- 原版 `changeMaterialOnAnimation` 是 **Inspector 序列化字段**，`OnEnable` 跑之前就有值 ⇒ 原版不存在「先克隆、后赋源」。
- 我们的 `Attach` 只能 `AddComponent`（**当场触发 `OnEnable → CreateInstanceMaterial()`**）**之后**才赋值 ⇒
  克隆源退化成 `originalMaterial`。凡 `changeMaterialOnAnimation` **非空**的实例都受影响：
  `CardFrame`（⛔ 不接）、**`Level Up Effect` ×2**（`B8_AS` §三·3 表 #5/#6）。
- 改法：`Attach` 里若传了 `changeMaterial` ⇒ 调类尾新增的 `RebuildOwnInstanceMaterial()`（`:352`）。
  它**先把真原件贴回去再重建** —— 否则 `CreateInstanceMaterial()` 从当前 `sharedMaterial` 读到的「原材质」会变成上一份克隆
  （`OnDisable` 之后还原到克隆 ⇒ 材质泄漏 + 名字多一层 `(Instanced)`）。

---

## 二、证据（文件:行号）

### 2·1 现读的判据（第一权威 = `.c`）

| 类 | `.c` 产物 | 备注 |
|---|---|---|
| `UIEffectTogglerImage` | `d:/2/tools/decomp_full/UIEffectTogglerImage__{Toggle,ToggleGameObjects,DoUpdate,AnimationFinished}__.c` + `__.ctor.c` | `.ctor` 只写 `+0x20 = 1`（`deactivateOnDisable` 默认 true）· `AnimationFinished.c` 首行名是 `UIEffectCanvasGroupToggler__…`（**ICF 同体折叠，体正确**） |
| `AnimateUIMaterialProperty` | `…__{OnEnable,Update,OnDestroy,OnDisable,CreateInstanceMaterial,SetMaterial,ResetToOriginalMaterial}.c` + `…AnimatedMaterialPropertyFloat__Update.c` + `__.ctor.c` | `.ctor` 写 `+0x28 = 1`（`initializeOnEnable` 默认 true）· `ResetToOriginalMaterial.c` 首行名是 `__OnDisable`（同体折叠） |

交叉（🥈 ILSpy，`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`）：
- `UIEffectTogglerImage.cs:6` = `private Image[] images`；`:8-19` `Toggle`；`:21-43` `DoUpdate`；`:45-51` `AnimationFinished`；`:53-69` `ToggleGameObjects`（= 账里那四条 `判据出处` 的范围，逐条对得上）。
- `AnimateUIMaterialProperty.cs:9-32` 嵌套 struct（`Update(Material)` 在 `:19`）；`:34-46` 字段序（`image / initializeOnEnable / floatProperty1 / floatProperty2 / changeMaterialOnAnimation / instancedMaterial / originalMaterial`）；`:145-159` `CreateInstanceMaterial`（`instancedMaterial.name + "(Instanced)"` 在 `:155`，逐字核对）。
- ⚠️ ILSpy 的两处坑（**本轮都踩到并绕开**）：`Update()` 的 `Equals(default)` 守卫输出成 `NoteDecompilerIssue("Method not found @181F70B00")`；
  `SetMaterial` 的 `* (+0x78) = 形参` 输出成 `object obj = this + 120; obj = material;`（**字段名丢了**，offset 120 = 0x78）。

### 2·2 我方落点（改后的**真行号**，账补丁已按这些写）

`CardPresentation/Effects/UIEffectTogglerImage.cs`（157 行）
| 成员 | 行 |
|---|---|
| `Capture()` | 54 |
| `TargetCount` | 82 |
| `Toggle(bool, float)` | 84 |
| `DoUpdate(float, float)` | 92 |
| `AnimationFinished()` | 128 |
| `ToggleGameObjects(bool)` | 134 |
| **`RefreshTargets()`（新增）** | **150** |

`WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`（361 行）
| 成员 | 行 |
|---|---|
| `IsDefault` | 117 |
| `AnimatedMaterialPropertyFloat.Update(Material)` | 129 |
| `Attach(...)` | 191（补偿 = `:209`） |
| `OnEnable()` | 219 |
| `Update()` | 226 |
| `SetMaterial(Material)` | 263 |
| `CreateInstanceMaterial()` | 281 |
| `Apply(slot)` | 335 |
| **`RebuildOwnInstanceMaterial()`（新增）** | **352** |

### 2·3 接线补丁（⛔ 本代理白名单外 —— 给接线方直接粘，不需要再查判据）

#### A. `Deck/DeckRuntime.cs`（原版 `Done Highlight`；账行 = `UIEffectTogglerImage` 那 4 条）

现状 `:4869` = `SetOn(_doneHl, err == DeckError.None);`（`_doneHl` 建于 `:2477` 的 `Img("foot_hl", …)`；`SetOn` = `:4743`）

```csharp
// 原版 `Done Highlight`（`bundle_menus_assets_all`）：同一个 GO 上 `Image` + `UIEffectTogglerImage`
// （`deactivateOnDisable=1`、`images[0]` = 同节点的 Image），由**祖先**上的 `UIEffectTogglerController` 驱动。
ToggleDoneHighlight(err == DeckError.None);      // ← 替换原来那句 SetOn
```
```csharp
// ---- 类尾追加 ----
UIEffectTogglerController _doneHlCtl;

/// <summary>原版 `Done Highlight` 的 `UIEffectTogglerImage` 淡入淡出（替换硬 SetActive）。</summary>
void ToggleDoneHighlight(bool on)
{
    if (_doneHl == null) return;
    if (_doneHlCtl == null)
    {
        // ⚠️ 控制器必须挂在**常驻 active 的祖先**上：`deactivateOnDisable` 会把 `_doneHl` 自己 SetActive(false)，
        //    控制器若在它身上，淡入永远 tick 不起来（`Toggle` 里那句 `enabled = true` 也救不了 inactive 的 GO）。
        var host = _doneHl.transform.parent;
        _doneHlCtl = host.GetComponent<UIEffectTogglerController>();
        if (_doneHlCtl == null) _doneHlCtl = host.gameObject.AddComponent<UIEffectTogglerController>();
        if (_doneHl.gameObject.GetComponent<UIEffectTogglerImage>() == null)
            _doneHl.gameObject.AddComponent<UIEffectTogglerImage>();   // 同节点 ⇒ 不用填 fadeTargets
        _doneHlCtl.Rescan();     // ⚠️ 控制器的 Awake 早于上面那次 AddComponent ⇒ 必须重扫
    }
    _doneHlCtl.Toggle(on);
}
```
- `DeckRuntime` 在 `namespace CardPresentation`（`:65`）⇒ **不用加 using**。
- 🔶 **「控制器挂 `parent`」是我们的选择，不是原版判据**（原版那级父链**没查到**，见 §三·2）。
- 编辑器/批处理下 `UIEffectTogglerController.Toggle` 会强制 `duration = 0`（瞬间档）⇒ 自检里行为与现在的硬切**等价**。

#### B. `Shell/DraftModeWindow.cs`（原版 `Prebuilt Pack UI`；同一个 C 组）

现状 `:2413` 起 `BuildPackCard`：`MenuDraw.Nine(content, … , "Highlight", …)` 的**返回值没接**（该函数 `:1766`，返回 `GameObject`，**整块在视口外会返回 null**）

```csharp
var hl = MenuDraw.Nine(content, Tex("OctagonUI_Border_SDF"), …,
                       "Highlight", null, null, default(Vector2));
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
调用点：悬停进/出、被选中、开包/关闭。⚠️ **这三个时机原版是哪一拍我没读到**（§三·1）⇒ 接线方按
`PrebuiltPackUI.OpenAnim / SelectAnim / CloseAnim` 的调用点再核一次，别照我这句猜。

#### C. `Shell/BoosterPackOpenWindow.cs`（原版 `Booster pack Background`）

```csharp
using WarpforgeVFX;   // `AnimateUIMaterialProperty` 在这个命名空间
…
AnimateUIMaterialProperty.Attach(bgQuad,
    AnimateUIMaterialProperty.Slot("_Distort_Scale",    0f),
    AnimateUIMaterialProperty.Slot("_Distort_Strength", 0f));
// 再播 `WarpforgeVFX/Animations/Booster Window - Background Shake On Open.anim`（0.9167 s，m_Legacy: 1，**已在工程里**）
```

#### D. `Shell/RankedDivisionChangeWindow.cs`（原版 `Division Image` + `Division Image/Rank Icon`）

```csharp
AnimateUIMaterialProperty.Attach(divQuad,  AnimateUIMaterialProperty.Slot("_Blend", 0f));
AnimateUIMaterialProperty.Attach(rankQuad, AnimateUIMaterialProperty.Slot("_Blend", 0f));
// clip：`Rank Up.anim`（`floatProperty1.value` 绑 `Division Image/Rank Icon`）· `Division Up.anim`（绑 `Division Image`）
//       —— 两条**都已在 `WarpforgeVFX/Animations/`**（见 §五·3）
```
顺带：`:947-956` 那段 `WarnChannel` 的文案是**错的**（「那个组件我们工程里没有」）—— 见 §五·1。

#### E. 主菜单升级演出（原版 `Level Up Effect` / `Level Up Effect Down`）

```csharp
AnimateUIMaterialProperty.Attach(effectQuad,     AnimateUIMaterialProperty.Slot("_Reveal", -0.5f));
AnimateUIMaterialProperty.Attach(effectDownQuad, AnimateUIMaterialProperty.Slot("_Reveal", -0.5f),
    AnimateUIMaterialProperty.SlotV4("_Noise_Strength_X_Speed_Y_Scale_Z", new Vector4(0.02f, -0.1f, 8.17f, 0f)));
// clip：`Level Up Card.anim`（已在工程里；`floatProperty1` 绑 `2DCard/Level Up Effect` 与 `…/Level Up Effect Down`，
//       `floatProperty2.vector4Value.x` 绑后者；带 `ChangeFrame` @1.45s 事件 —— 那事件归 `CardDisplayLevelUpAnimationController`（Shell））
```

🔴 **A～E 的共同前提（照抄 `B8_AS` §三·3，本轮没推翻）**：`_Distort_Scale` / `_Distort_Strength` 只存在于原版材质
`Shader Graphs/Nebula`；`_Blend` 只存在于 `Everguild/UI/Division Change`。我们那几块图现在是 `Sprites/Default`
⇒ **现在挂上去不报错、也没有观感**（`Material.SetFloat` 对不存在的属性是静默 no-op，原版同）。
要真生效，得先让那几块图用**原版材质**（`WarpforgeShaderMap.TryResolve`；取不到要**出声**）。
⛔ `CardFrame` 那个实例原版 `m_Enabled = 0`，`CardView`/`CardFeel` 已硬编码驱动这一层 ⇒ **别再挂**（会写两遍）。

---

## 三、没查清的部分

1. **原版两处 `Toggle(option, duration)` 的实参 / 触发时机**（`Done Highlight`、`Prebuilt Pack UI`）。
   `B8_AS` 也没读到（§三·2 自己写明）。⇒ 补丁里用默认 `0.3f`（= 原版 `UIEffectTogglerBase.Toggle` 的默认值），
   **触发点沿用现有硬切那一句的语义**。要真对齐得读 `PrebuiltPackUI.OpenAnim / SelectAnim / CloseAnim` 的方法体。
2. **`Done Highlight` 的控制器挂在哪个祖先** —— 原版该 GO 上只有 `Image / UIEffectTogglerImage / EverguildButtonMaterialModifier`
   （`B8_AS` 现读），控制器**不同节点**；具体哪一级**没查到**（要沿 `bundle_menus_assets_all` 解父链）。
   补丁里选 `parent` 是 🔶 我们的选择。
3. **`Level Up Effect` 两份共用的外部材质 shader 名**没解（`m_FileID 5 / m_PathID 4`，`B8_AS` 记）。
4. **原版材质在我们这边的运行时可达性没验**：`Shader Graphs/Nebula` / `Everguild/UI/Division Change` 能不能被
   `WarpforgeShaderMap.TryResolve` 取到、取到之后 `_Distort_*` / `_Blend` 是否真的在属性表里 —— **我没跑 Unity**，验不了。
5. **`UIEffectTogglerImage` 的第 5 条账行不存在**（§一·0）。简报说 5 条，实测 4 条；我不猜第 5 条是什么。
6. **`AnimateUIMaterialProperty` 的 `Equals(default)` 逐字节等价仍未证**（`B8_AS` 也没证）——
   我们的 🔶① 只在「`materialProperty == ""` 且三值零」这一档与原版有可见差异（都是 no-op）⇒ 出厂 6 实例全覆盖，但**不保证任意输入**。

---

## 四、改动清单（只有这 3 个文件）

| # | 文件 | 改动 | 行数变化 |
|---|---|---|---|
| 1 | `Unity/MyGame/Assets/CardPresentation/Effects/UIEffectTogglerImage.cs` | ① 新增字段 `int _capturedTargets = -1;`（🔶 自己设计，带三行说明）② `Capture()` 幂等闸改「按 `fadeTargets.Count` 判重」③ 类尾新增 `RefreshTargets()`（🔶 自己设计，带 doc） | +20 行（157） |
| 2 | `Unity/MyGame/Assets/WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs` | ① `Attach(...)` 末尾补 `if (changeMaterial != null) c.RebuildOwnInstanceMaterial();`（🔶 自己设计 + 6 行说明）② 类尾新增 `RebuildOwnInstanceMaterial()`（🔶 自己设计，带 doc） | +17 行（361） |
| 3 | `Unity/资料/完全复刻/账/_parts/B9_W14FX.tsv` | **新建**：8 条 C 行的账补丁（11 列 · 表头齐全 · `方法` 全带签名 · LF · 无 BOM · 无裸 CR） | 9 行（含表头） |

**没动的**（守住所有权）：`Effects/UIEffectTogglerCoffeeEffects.cs` · `Shell/UIBorderGlow.cs` · `Battle/ScenarioMaterialFader.cs` ·
`Effects/UIEffectCanvasGroupToggler.cs` · `Effects/UIEffectTogglerController.cs` · 一切 Shell / 卡组编辑 / 轮抽文件 · 账主表 · 正本 · 别人的报告。

**验证**：`TMPDIR=/tmp/wf_w14 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**（见 §五·5 关于首次那 23 个错）。
`check_patch_conflicts.py` ⇒ **✅ 没有跨补丁重复键（3 份补丁）**。

⚠️ **账还没合并**：按铁律 16·8，主表只有调度台能写、且必须带 `--only B9_W14FX`；
**合并后请把本补丁移进 `_parts/_merged_B9_W14FX/`**（否则是一次「旧内容定时炸弹」）。

---

## 五、顺手发现

1. 🔴 **`Shell/RankedDivisionChangeWindow.cs:947-956` 有一条【会输出到日志的错误事实】**：告警文案写着
   「那个组件我们工程里没有（`grep class AnimateUIMaterialProperty` 零命中）」——
   **组件早在 2026-10-26（`B8_AS`）就建好了**，在 `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`。
   ⇒ 这条注释 + 告警文案要改（不在本代理白名单）。同族：`Shell/BoosterPackOpenWindow.cs:95` 与
   `Shell/BoosterPackAnim.cs:14-15` 都写「原版 shader 的逻辑本地没有」—— **也过期**（`B8_AS` 已解出
   `Shader Graphs/Nebula` / `Everguild/UI/Division Change`）。
2. 🔴 **账里「缺 3 条 clip」的说法已过期**（`B8_AS` §三·4 / 账 :84 备注）：`Rank Up.anim` · `Division Up.anim` ·
   `Level Up Card.anim` **现在都在 `WarpforgeVFX/Animations/`**，且都是 `m_Legacy: 1`、曲线绑定点与 `B8_AS` 表逐条吻合
   （`Division Image/Rank Icon` · `Division Image` · `2DCard/Level Up Effect` 与 `…/Level Up Effect Down`
   （后者还带 `floatProperty2.vector4Value.x`））。`Level Up Card` 的 `ChangeFrame` 事件在 1.45 s。
   ⇒ **clip 这半边不缺了**，只差接线 + 那几颗事件要谁接。已在账补丁里就地订正。
3. 🔴 **同族 `UIEffectCanvasGroupToggler.cs:50-58` 有【同一个】`_captured` 死锁**（它是 A 档）。
   现有唯一调用点 `Shell/ForgeTab.cs:423` **不填 `fadeTargets`** ⇒ 目前**无害**、A 不假。
   但只要将来有人「`AddComponent` 之后再 `fadeTargets.Add(...)`」，就会同样静默淡错对象。
   ⛔ 本代理不改它（不在白名单）—— 建议排给该文件的写手补 `RefreshTargets()`。
4. ⚠️ **`UIEffectTogglerController` 在编辑/批处理模式下 `AddComponent` 不触发 `Awake`** ⇒ `togglers` 列表恒空
   （`ForgeTab.cs:432` 那条 `Rescan()` 的注释已经记过这件事）。本轮补丁 A/B 里都显式写了 `Rescan()`，**别省**。
5. ⚠️ **首次 `typecheck` 报 23 个错，全部在 `RuleEngine/Editor/RuleEngineTest_*.cs`**（`CardDef` 缺
   `CountSetSpec` / `ConditionSpec`、找不到 `AiTargetCriteriaDto` / `AiTargetRef`）—— 那是**别的代理正写到一半**
   （`RuleEngine/Core` 的 criteria 那批）。隔一分钟重跑 = **0/0**。⛔ 我没去改那些文件。
6. ⚠️ `AnimateUIMaterialProperty.SetMaterial` 有一条**原版怪行为照抄**：换进来之后**屏幕上的材质不变**。
   将来谁觉得「这明显是 bug」想去修 —— ⛔ 别修（判据 = `…__SetMaterial.c` 全文只有 3 句）。
