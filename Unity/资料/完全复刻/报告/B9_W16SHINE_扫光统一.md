# B9_W16SHINE · Shiny 扫光这一族统一落地（收藏:348 / 7 个 Coffee 实例 / 收藏:15 的 army）

> 执行代理 `B9_W16SHINE` · 2026-10-27 · 有写权限 · **全程未跑 Unity（含不编译 shader）· 未动 git · 未改正本/账主表**
> 每改一次 `.cs` 都跑过 `TMPDIR=/tmp/wf_w16 bash 工具/typecheck.sh`，**最终 0/0**
> （中途红过三轮，全部落在别人正在写的文件上：`Shell/UIBorderGlow.cs`（2 条）· `Shell/DraftModeWindow.cs`（5 条）·
> `Shell/CardDetailPopup.cs`（2 条）· `Editor/BattleScene.cs`（1 条）—— 复跑即绿，**一条都不是本件的文件**）。
> 账补丁 = `账/_parts/B9_W16SHINE.tsv`（5 行，11 列已自检）

---

## 一、结论（三件，逐条）

| # | 任务 | 结果 | 落点 |
|---|---|---|---|
| 1 | **`收藏:348` 收藏窗野牌格扫光** | ✅ **接上了**（上一轮 C ⇒ 本轮 A） | `Shell/CollectionWindow.cs:1043`（建页调用）· `:6430`（`BuildWildcardShine`）· `Effects/UIEffectTogglerCoffeeEffects.cs:133`（`WildcardShineLayer`） |
| 2 | **7 个 Coffee 实例改走我们自己的 shader** | ✅ 层 + 3 个宿主已接；**2 个实例有宿主未定**（B/D） | `CampaignTab.cs:931/854/880` · `ForgeTab.cs:768/833/1343/1354` · `DeckRuntime.cs:4872/8929` · `Effects/UIEffectTogglerCoffeeEffects.cs:64`（`Toggle`） |
| 3 | **`收藏:15` 补 `army`（B→A）** | ✅ **补齐**（含 `ForgeBadge`，见下） | `Shell/BadgeNotifications.cs:79`（`ForgeBadge`）· `:97`（`CampaignBadge`）· `:348` / `:373`（两个 provider）· `CampaignTab.cs:1662` / `ForgeTab.cs:1300`（`BuildArmyBadge` 的过滤定义） |

**三句话总结**：

1. **上一轮卡住的那条「9 块要 9 份仿射」的判断，被这一轮推翻成「一块都不需要」。**
   我去读了 Coffee 的 IL（`ilspycmd` 反编译 `Assets/ThirdParty/Coffee/Coffee.UIEffect.dll`），
   拿到 `UIEffect.transitionRoot` 的 getter = **`m_CustomRoot ?? 自己的 transform`**，而 5 个实例的 `m_CustomRoot` 全是 `null`
   ⇒ 原版那一串 `unity_ObjectToWorld → _CanvasToWorldMatrix → _RootViewMatrix`（`b16.hlsl:103-130`）的结果就是
   **「顶点世界位置对**本件自己的 Rect** 归一」**。⇒ 掩码按**世界位置**算 ⇒ 九宫格 9 块**共用一份材质**、掩码天然连续，
   而且裁切/软边重切都不会失效（仿射方案一被裁切就作废）。**这条同时把「材质泄漏」压成「每层一份」而不是「每块一份」。**
2. **7 个实例里真正的画面只有 5 个**（A/E 的 `uiEffects` 是空数组，原版本来就什么都不做）。
   本轮接上 **C / G / F** 三个；**B（`Army Item Button No Arrow Variant/Icon`）与 D（`Army Item Button/Icon`）宿主未定** ——
   这两个 prefab 在我们工程里对应哪颗节点**我没查到**，⛔ 没有猜（见 §四·1）。层是通用的，定位到节点后**一行**就能挂。
3. **`army` 那条「上一轮说它不进 Validate」是错的**，本轮就地订正：
   `RewardsBadge.Validate` 在「同类型」那一支**落到 record 的 `Equals`**（`RewardsBadge__Validate.c` 末尾那个虚调用），
   而 `CampaignBadge.Equals` 比 `+0x18` 的 army（`CampaignBadge__Equals.c`）⇒ **army 确实进过滤**。
   ⇒ 我们补 `army` 不是「加个字段」，而是**恢复了原版的过滤语义**：13 颗阵营格只有该阵营那颗会亮（原来 13 颗一起亮）。

---

## 二、证据（文件:行号 / 资产路径）

### 2·1 `_RootViewMatrix` 到底是哪个坐标空间（本轮最关键的一条）
| 事实 | 判据 |
|---|---|
| `_RootViewMatrix` 由 C# 算 | `UIEffectContext.UpdateViewMatrix(Material, RectTransform transitionRoot, Canvas)`，`b16.hlsl:48` 是它的 uniform |
| `transitionRoot` = **自己的 RectTransform** | `ilspycmd -o /tmp/coffee_il` 反编译 `out_cpp2il_dev/Coffee.UIEffect.dll` → `Coffee.UIEffects/UIEffect.cs` 的 `public override RectTransform transitionRoot`：`if (m_CustomRoot) return m_CustomRoot; … return (RectTransform)transform;` |
| 5 个实例 `m_CustomRoot` 全 `null` | `报告/B9_W5COF_Coffee搬运.md` §2·6 的逐字段表（唯一真在用的滤镜 = `m_TransitionFilter 4 (Shiny)`） |
| 坐标用法 | `b16.hlsl:103-130`：顶点 → `unity_ObjectToWorld` → `_CanvasToWorldMatrix` → 逐行点积 `_RootViewMatrix` → **`clamp(0,1)`** → `_TransitionTex` |
| 能坐实的另外三件 | `UpdateViewMatrix` 里出现 `transitionRoot.rect` / `transitionRoot.pivot`（`-0.5f` 偏移）/ `worldToLocalMatrix` / `1/W`、`1/H` 与一个 `m_TransitionKeepAspectRatio` 分支 / 一个绕 `m_TransitionRotation` 的 `Matrix4x4.TRS(pos, quat, scale)`（`ilspycmd -il` 的 `IL_0041..IL_01cc`） |
| ⛔ 拿不到的那一件 | 该方法的**局部变量全被 Cpp2IL 打散**（`ilspycmd -il` 里只有 `ldloc N` + `NoteDecompilerIssue("Method not found @…")`）⇒ **θ 之后那一步的精确算式只能是重构**（标 🔶，见 §三·1） |

### 2·2 shader 与参数
- 移植本体：`Assets/CardPresentation/Shaders/WildcardShine.shader`（195 行；本轮改的是**掩码坐标那一跳**，见 §三·1）。
- 播法（`UIEffectTweener`）：`m_Duration` 秒 · **线性 0→1** · `m_Delay 0` · `m_PlayOnEnable 0` · `m_ResetTimeOnEnable 1` ·
  `m_WrapMode 0(Once)/1(Loop)`；`UIEffectTogglerCoffeeEffects.DoUpdate` 原版是 `RVA 0x4B33B0` 的**空桩**
  ⇒ 画面**只**由 `Toggle` 里那次 `Play()` 决定（不是每帧补间）—— 这也是我们**不需要** `UIEffectTogglerController` 的理由。
- 关键：`_TransitionRate = 1` 与 `0` **都不亮**（`band` 把带推到框外），只有 0→1 的中段扫过去 ——
  `.shader:169-174` 的 `rate * 0.99989998` 就是原版为躲 `frac(1.0)=0` 那个坑而写的常数，**照抄**。

### 2·3 Done 颗的开关判据（实例 F）
`decomp_full/DeckEditingWindow__UpdateDoneButton.c`：`valid = DeckUtility.ValidateDeck(...)`；
`param_2 != 0`（静默那条）⇒ `controller.Toggle(valid, 0)`；否则 `valid ? Toggle(true, 0.3f)+DOPunchScale : Toggle(false, 0.3f)`
⇒ **扫光 ⟺ 这副卡组合法**，与我们的 `Done Highlight` / 保存闸**同一条判据**（落在 `DeckRuntime.cs:4869` 那两句的位置）。

### 2·4 Forge 颗的开关判据（实例 G）
`decomp_full/ForgeRewardSelector__ToggleCollectReward.c`：`UIEffectTogglerController__Toggle(*(this+0x48), param_2, 0.3f, 0)`；
调用者 `FinishedSnapping` 传 `[this+0x68]`、`RewardCollected` 传 `false`。
而 controller 的 togglers = `GetComponentsInChildren<UIEffectTogglerBase>()` ⇒ **整排 13 颗 `…/Icon` 同开同关**，
判据 = 当前锻造有没有可领奖励 ⇒ 我们落在 `ForgeTab.Refresh()` 里紧挨 `_readyToggler` 那一句（`:833`）。

### 2·5 徽记的 `army`
- `decomp_full/Everguild.LiveOps.CampaignHandler._CheckNotification_d__21__MoveNext.c`：
  `Everguild_UI_Notifications_CampaignBadge___ctor(uVar6, uVar1, 0)`，`uVar1 = *(undefined4*)(lVar2 + 0x7c)` = **SelectedArmy**。
- `decomp_full/Everguild.LiveOps.ForgeHandler.__c___CheckNotification_b__9_0.c`：
  `CampaignBadge___ctor(uVar3, uVar2, 0)`（**反编译器把类名印错了**，实参是该 forge 的 army）+ `Enumerable.Repeat(badge, Count(rewards))`。
- **army 进不进 Validate**：`decomp_full/Everguild.UI.Notifications.RewardsBadge__Validate.c` 末段 ——
  `if (GetType(other) != GetType(this)) { string.Equals(parentId, other.parentId); return; }` **否则落到虚调用**
  `(**(code**)(*param_1 + 0x1b8))(param_1, param_2, …)` = `Equals`；`Everguild.UI.Notifications.CampaignBadge__Equals.c`
  在比完 `RewardsBadge__Equals` 后再比 `*(undefined4*)(param_1 + 0x18)` 的 army。
  ⇒ **同类型时 army 进比较** ⇒ 原版逐阵营过滤成立。

---

## 三、逐条「原版怎么算 / 我们原来怎么算 / 改成什么 / 怎么验」

### 1) `收藏:348` · `WildcardDisplay.OnEnable` 的 Shiny

- **原版**：`OnEnable` 里 `if (currentArmy != Neutral) { shinyEffect.Play(resetTime:true); SetCountersText(); }`；
  `shinyEffect` = 那颗 `UIEffectTweener`（0.75 s 线性 0→1，Once），驱动 `WIldcard Display ▸ Background` 上的
  `UIEffect`（Shiny · 120° · 0.25 · 0.5 · 白 · filter 6）。
- **我们原来**：shader 移植好了但**零消费方**（上一轮 C）。上一轮判「做不了」的理由是「Background 是 9 个子 quad，
  每块各需一份 uv 仿射 + 各自材质」。
- **改成什么**：
  ① `.shader` 的掩码坐标改成**顶点世界位置**（`i.world.xy − _ShineFrame.xy`，绕框心转 θ，再乘 `1/max(W,H)`）；
     两个新 uniform：`_ShineFrame`(世界中心 xy + 世界宽高 zw)、`_ShineRot`(cosθ, sinθ, 1/max, 0)，**替掉** 原来的 `_ShineMatrix`。
  ② `WildcardShineLayer`（`Effects/UIEffectTogglerCoffeeEffects.cs:133`）：`AttachNine(root)` 用
     `GetComponentsInChildren<ImageQuad>(true)` 收 9 块（⚠️ 根节点自己没有 `ImageQuad`），换**一份**材质；
     `Play()` 从 0 推到 1；`Tick` 写 `_TransitionRate`；`StepAll(dt)` 是确定性推进口（批处理没有帧循环）。
  ③ 建页那一拍播（= 原版 `OnEnable`）：`CollectionWindow.BuildCardsPage` 的 `Nine(...)` 之后一句
     `BuildWildcardShine(wcBg)`（`:1043`），里面 `if (!ArmyIcons.IsNeutral(_wcArmy)) _wcShine.Play(true)`。
     `SetWildcardArmy`（= 原版 `Initialize`）**不播** —— 原版 Initialize 也不播。
- **🔶 自己设计（非原版）**：θ 之后那一步「统一 `1/max(W,H)` 归一 + 绕框心旋转」。
  为什么必须自己定：Coffee 的 `UpdateViewMatrix` 方法体在 Cpp2IL 重建的 dll 里局部变量全被打散（§二·1 末行）。
  取这一种的理由：另两种取法（`1/min` 统一 / 逐轴 `1/W,1/H`）会让宽扁的框**一半落在 [0,1] 之外**，
  而 HLSL 那一步是 `clamp(0,1)` ⇒ 半个框被钉成 alpha=1 的一整片死白，与「Shiny 是一条扫过去的带」直接矛盾。
  · 调度台 2026-10-27 裁定（沿用 `B9_W7COLL` 的 🔶 口径）。
- **怎么验**（本批**没跑**，红线）：
  ① `CollectionWindow.WildcardShineRate == 0` 刚建页、`StepWildcardShineForTest(0.5f)` 后 ≈ 0.67、`(1f)` 后 == 1；
  ② 那颗 `Background` 下 9 块 quad 的 `MeshRenderer.sharedMaterial` **是同一个对象**且 `shader.name == "CardPresentation/WildcardShine"`；
  ③ 9 块的 `_ShineFrame` 相同（比 `Material.GetVector("_ShineFrame")`），且 ≈ 那 9 块网格世界并集；
  ④ 让 `_wcArmy = ArmyIcons.Neutral` 再建页 ⇒ `Rate == 1`（不播）。
- **⚠️ 未验证**：这个 shader **一次都没编译过**（本波不许跑 Unity）。见 §六。

### 2) 7 个 Coffee 实例 → 我们自己的等价层

- **原版**：`UIEffectTogglerCoffeeEffects.Toggle(bool)` = 「逐件 `Behaviour.enabled = option`；开的时候同节点
  `UIEffectTweener.Play()`」；`duration` 一个字节都没用。
- **我们原来**：挂真的 `Coffee.UIEffects.UIEffect` —— 而它派生自 `UIEffectBase : IMeshModifier, IMaterialModifier`，
  **只能挂 uGUI `Graphic`**；我们的 UI 是世界空间 `ImageQuad` + `MeshRenderer` ⇒ **类型接上、视觉为零**。
- **改成什么**：字段 `List<Coffee.UIEffects.UIEffect> uiEffects` → **`List<WildcardShineLayer> shineLayers`**
  （同一语义槽位），`Toggle` = 逐层 `SetEnabled(option)` + 开时 `Play()`；`DoUpdate` / `AnimationFinished` 仍留空（照原版空桩）。
  ⇒ **本类不再引用 `Coffee.UIEffect.dll`**（文件仍在 `Assets/ThirdParty/`，⛔ 别删，见 §五·4）。
- **怎么验**：`Toggle(true)` 后每层 `Enabled == true ∧ Playing == true`、`Rate` 从 0 涨；`Toggle(false)` 后
  `Rate == 1`（不亮）且材质还在（图不会被隐藏 —— 上一版把 `enabled` 打在 `ImageQuad` 上是**语义错**）。

### 3) `收藏:15` · `ArmyItemContainer.Initialize` 的两支徽记

- **原版**：`Campaign(1) ⇒ new CampaignBadge(army)`；`Forge(2) ⇒ new ForgeBadge(army)`；`Neutral(0)` 直接 return。
  计数 = 「与该 `army` 相符的通知条数」⇒ **13 颗格子只有该阵营那颗会亮**。
- **我们原来**：徽记节点 + 注册都做了，但 `CampaignBadge`/`ForgeBadge` **没有 `army`** ⇒ 按**类型**匹配 ⇒ **13 颗一起亮**。
- **改成什么**：
  ① `ForgeBadge` / `CampaignBadge` 各加 `public string army` + `override Validate`（同类型 ∧ 同 army）；
  ② 两个 provider 逐条带阵营：`new CampaignBadge { army = CampaignData.Selected }` / `new ForgeBadge { army = army }`；
  ③ 两窗 `BuildArmyBadge` 的定义改成 `useFilter: true, filter: new …Badge { army = army }`；
  ④ 左上角那颗「锻造/战役页签」的红点**不带过滤**（`BadgeWiring.DefinitionsForTab` 不动）—— 原版那两颗也是无过滤定义。
- ⚠️ **表示法差异（结构不同、语义一致，⛔ 不打 🔶）**：原版 army 是 `CardArmy` 枚举，我们存**阵营名串**
  （与 `RewardsBadge.parentId` 用 string 同一条先例；`CampaignData.Armies`/`ForgeData.Armies` 本来就是名字串）。
- **怎么验**：只在 Ultramarines 有可领内容时 —— 战役页 `CampaignArmyItem_<UM>` 的 `UiBadgeNotification.CurrentCount > 0`，
  **其余 12 颗 == 0**；锻造页同理（数 = 该阵营 `ToCollect` 格数）。⚠️ 顺序坑：先 `RegisterDefaults()` 再建页，
  中间 `Refresh(true)` + `Recount()`（沿用 `B9_W7COLL` §四·4 那条）。

---

## 四、没查清的部分（「查不到 + 还差什么」）

1. 🔴 **实例 B / D 的宿主未定**：`Army Item Button No Arrow Variant/Icon` 与 `Army Item Button/Icon` 这两个 prefab
   在我们工程里对应哪颗节点**没查到**。已核的：两者结构 = `HighlightBG / Icon / Badge Highlight / OneText`（`R0_资源覆盖账.tsv` 里那几棵子树），
   与我们 `Cell_fac_*`（收藏左抽屉，只有 `Icon`）**形似但父链没读全**。**还差什么**：把收藏窗左抽屉那格的父链
   （`menu_dump.py` 到 `Army Item Button …` 的实例 GO）读一遍，或直接看那个 prefab 的 4 个使用点。
   层是通用的：定位后 `WildcardShineLayer.AttachQuad(icon, Params.Icon)` 一行即可。
2. **θ 之后那一步的精确算式**（🔶，见 §三·1）：Coffee 的 `UpdateViewMatrix` 方法体在 Cpp2IL 重建的 dll 里局部变量全丢。
   **还差什么**：对 `GameAssembly.dll` 里那个方法的 **VA 级反汇编**（`dump.cs` 有 RVA），把 `1/W`、`1/H`、
   `Matrix4x4.TRS(rot)` 三者的**乘序**读出来。⛔ 现在不要拿我们的算式冒充原版。
3. **两张 `_TransitionTex` 的 `_TransitionTex_ST/_Speed` 采样方向**：本实例全是默认值（`(1,1)/(0,0)/(0,0)`），
   所以「解析式 `1-saturate(uv.y)`」与「贴图采样」等价；**如果将来有实例改了 `_ST/_Speed`，解析式那一支不再等价**
   （要走 `_UseTransitionTex = 1` + 真贴图）。**还差什么**：真贴图（`CAB-34ccb919…` 的 `Transition-Horizontal`）还没导进工程。
4. **`KeepAspectRatio = 0` 那一档没实现**：本 shader 只做「保比例」那一种归一（5 个实例全是 1）。
   原版另一个分支（逐轴 `1/W,1/H`）没写 —— 将来有实例设 0 时必须补，⛔ 别让它静默用错归一。
5. **Deck 编辑窗那颗 `WIldcard Counter ▸ Background`**（= 账里 W64 表那一套 139/0.213/0.533/1.0 s/(2,2,2,1)）
   **本轮没接**（不在派活范围）：层通用，缺的只是一个 `Params` 预设 + `DeckRuntime.cs:1693` 那句 `MenuDraw.Nine` 之后一行 +
   「切到 Cards/Deck info 页时播」（原版同样是 `OnEnable` 那一拍）。参数已在本报告与 `.shader` 头注里写死，别重查。

---

## 五、顺手发现（请调度台分流）

1. 🔴 **`账/战役.tsv` 里「army **不进** Validate」那句是错的**（上一轮 C2T7 记的）。判据见 §二·5：
   `RewardsBadge.Validate` 同类型那一支落到 `Equals`，而 `CampaignBadge.Equals` 比 army。**本条已在账补丁里就地订正**。
   连带：`账/社交_聊天.tsv:453` 那行（`CampaignBadge` 数据模型 record）也该跟着改 —— 它的「方法」列是
   `（数据模型 record：9 个编译器样板方法）`（全角括号、不是签名）⇒ **补丁校验器会拒**，只能调度台手改。
2. 🔴 **`账/锻造.tsv:64`（`ForgeHandler.CheckNotification`）的「一致」上一轮判过不成立** —— 本轮已补齐并改写（见补丁第 3 行）。
3. **`DeckRuntime.cs` 在我读它之后被别人改过**（2026-10-27 18:40:01，`git status` 有 `M`，`账/_parts/_merged_B9_W6DECK/` 刚生成）。
   我把落点重新读了一遍再改（原 `SetOn(_doneHl, …)` 已被改成 `ToggleDoneHighlight(…)`，我挂在它后面），
   **两次写没有覆盖对方的改动**（`git diff` 里 W6DECK 的内容仍在）。⚠️ 但派活表说 `Deck/**` 归我 ⇒ 请调度台确认那一刻没有并发的活。
4. **`Coffee.UIEffect.dll` 现在零引用**（唯一使用者是本类）。可删可留：**建议留一版**（它是「原版引用了什么」的物证，
   且 `W5COF` 的报告逐条引它）。要删的话连 `Assets/ThirdParty/` 一起删，别只删文件。
5. **`ImageQuad` 没有公开的 `MeshRenderer` 取用口**（`_mr` 私有）⇒ 本轮两处（材质比对、几何取框）都走
   `q.GetComponent<MeshRenderer>()` / `MeshFilter`。**没改 `ImageQuad`**（不在所有权内，且它有 184 处调用点）。
   将来若要 frequent 访问，建议在类尾加一个 `public MeshRenderer Renderer => _mr;`（**增量、零行为变化**）。
6. **`WindowButton.RefreshGray` 会把 quad 的材质换走再换回**（= 原版 `SoftDisable` 那一跳），而「换回」用的是
   **变灰那一刻存下的**那一份 ⇒ 「先灰、后挂扫光」会让扫光静默消失。本件已加 `WildcardShineLayer.StillAttached` +
   `FootShine` 里的重挂（只在「该亮」时重挂，灰着时不挂）。**别的挂特效的族**（`UIEffectTogglerImage` 那批）
   有没有同一个病，本轮**没查**。
7. **`WildcardShineLayer` 的材质是「每层一份、不缓存共享」**：共享会让 `ImageQuad.SetTexture`（悬停换图）
   把别人的贴图一起改掉（静默）。泄漏靠 `PruneDead()`（建新层时 + 每帧 `StepAll`）回收 ⇒ **上限 = 同时活着的层数**。
   批处理里没有每帧泵，只能靠「建新层」那一下 ⇒ 自检若反复重建页面，**用例之间调 `WildcardShineLayer.ResetForTest()`**。

---

## 六、7+1 处扫光实例 × 参数 × 载体

> 「+1」= 收藏窗那颗（它不是 `UIEffectTogglerCoffeeEffects` 的实例，是 `WildcardDisplay.shinyEffect` 的）；
> 参数一律**逐实例现读**，⛔ 别拿一套套全部（铁律 5·c）。

| # | 原版节点 | 参数（rotation / width / softness / color / filter / duration / wrap） | 我们这边接在哪 | 状态 |
|---|---|---|---|---|
| A | `Ranked Army Selector Container V2/Featured Icon` | —— 原版 `uiEffects` **空数组** ⇒ 什么都不做 | —— | ✅ 原版即无（⛔ 别当缺口） |
| E | `Ranked Army Selector Container V2/Army Icon` | 同上 | —— | ✅ 原版即无 |
| **C** | `Campaign Army Item Button/Icon` | 120 / 0.25 / 0.5 / (1,1,1,1) / 6 / **1.0 s** / Once | `CampaignTab.cs:931`（`AttachArmyShine`）· 播放口 `:854`（`PlayArmyIconShine`，由 `OnArmyAmountChanged` 那一拍） | ✅ 已接 |
| **G** | `Forge Army Item Button/Icon` | 同 C | `ForgeTab.cs:768`（`AttachArmyShine`）· 开关 `:833`（`SyncArmyShine`，在 `Refresh()` 里） | ✅ 已接（整排同开同关，判据 = `HasToCollect(Selected)`） |
| **F** | `Deck Editing Menu/…/Sidebar/Footer/Done` | 120 / 0.25 / 0.5 / **(3.4822,3.4822,3.4822,1)** / 6 / 1.0 s / **Loop** | `DeckRuntime.cs:4872`（调用）· `:8929`（`FootShine`，惰性挂在 `foot_done` 上） | ✅ 已接（开关 = 卡组合法） |
| B | `Army Item Button No Arrow Variant/Icon` | 同 C | —— **宿主未定**（§四·1） | ⚠️ 层就绪，未接 |
| D | `Army Item Button/Icon` | 同 C | —— **宿主未定** | ⚠️ 层就绪，未接 |
| **+1** | `WIldcard Display ▸ Background`（收藏窗） | 120 / 0.25 / 0.5 / (1,1,1,1) / 6 / **0.75 s** / Once | `CollectionWindow.cs:1043` + `:6430` | ✅ 已接（9 块共用一份材质） |
| （旁注） | `Deck Editing Menu ▸ WIldcard Counter ▸ Background` | **120 / 0.25 / 0.5 / (1,1,1,1) / 6 / 0.75 s / Once**（🔴 2026-10-27 订正：原文写的 139/0.213/0.533/1.0 s **不属于这颗**，那套是 `Message Container ▸ Content` 的第 9 个实例） | `DeckRuntime.cs:1693`（背景已建；扫光接法已派 W51） | ⏭ 层就绪（§四·5） |

---

## 七、同步点要验什么（本波不许跑 Unity，全部留给调度台）

1. 🔴🔴 **shader 编一次**（最大未验证项）：`WildcardShine.shader` 从未编译过。开工程看 Console 有没有
   `Shader error in 'CardPresentation/WildcardShine'`。⚠️ 重点看三处：`fixed4 c = tex2D(...) * i.color`（`fixed` 与 `float4` 混算）·
   `frac()`/`saturate()` 的目标档（写的是默认 2.5 档，末尾刻意用 `floor(x+0.5)` 代替 `round()`）· `unity_ObjectToWorld` 在 `vert` 里的可用性。
2. 🔴🔴 **进包**：shader 在 `Assets/CardPresentation/Shaders/`（不在 `Resources/`）⇒ **player 包里 `Shader.Find` 会返回 null**
   （会打一条 warning，然后扫光不出现）。两条路选一条：① 加进 `ProjectSettings/GraphicsSettings.asset` 的
   `m_AlwaysIncludedShaders`（那份工程设置不在本件所有权内 ⇒ 未改）；② 移进 `Resources/Shaders/`（同 `CardSprite.shader` 的先例）。
   **⛔ 不做这一条，真机验收一定看不到扫光**。
3. **`typecheck` 复跑到 0/0**（本件最后读到的是 0/0，但中途有别人在写 `DraftModeWindow.cs` / `CardDetailPopup.cs` / `BattleScene.cs`）。
4. **账**：合并 `账/_parts/B9_W16SHINE.tsv`（5 行）后跑一次 `check_ledger_points --all-A --fix`。
   ⚠️ 本波在 4 个宿主文件里**加了行**（行号会整体下移），受影响规模（合并前实测的引用数）：
   `CollectionWindow.cs` 89 行（`收藏.tsv` 主）· `CampaignTab.cs` 35 行（`战役.tsv`）· `ForgeTab.cs` 33 行（`锻造.tsv`）·
   `DeckRuntime.cs` 74 行（`卡组编辑/卡牌面_渲染/收藏/战斗逻辑驱动`）· `BadgeNotifications.cs` 36 行。
   `UIEffectTogglerCoffeeEffects.cs` **已刻意保持 `Toggle`/`DoUpdate`/`AnimationFinished`/`WarnIfVisualBodyMissing`
   仍在 64/86/89/95 行** ⇒ 那 3 条账（453/454/455）**零漂移**。
5. **该跑的宿主**（按铁律 12 的「只动一个宿主就只跑那一条」再放宽到共用件）：
   本件动的是**窗（收藏/战役/锻造）+ DeckRuntime + 一个共用件（BadgeNotifications）+ 新共用件（WildcardShineLayer）**
   ⇒ 至少 `CollectionScene.Run` + `RewardsScene.Run`（战役/锻造红点都在它）+ `DeckScene.Run` + `MainMenuScene.Run`（顶栏红点定义没动，但 `BadgeWiring` 是共用件）。
   ⚠️ 本波**没有加任何断言**（`Editor/*` 宿主不在本件所有权内）⇒ 上面 §三 那几条「怎么验」需要调度台另派一个执行代理落成断言。
6. **真 Play**：扫光只在 `Application.isPlaying` 里有帧泵（`WildcardShineLayer.Pump` 插在 PlayerLoop 的 `Update` 阶段）。
   批处理/编辑器里**不会自己动**（自检必须走 `StepAll(dt)` / `StepWildcardShineForTest(dt)`）。

---

## 八、改动清单

| 文件 | 动作 | 说明 |
|---|---|---|
| `Assets/CardPresentation/Shaders/WildcardShine.shader` | **改**（170→195 行） | 新增 `_ShineFrame`/`_ShineRot`；掩码改用**顶点世界位置**（原 `_ShineMatrix` 删除）；文件头补 ④ 那段取证 |
| `Assets/CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs` | **重写 + 追加**（改动前 106 → 489 行；**HEAD 是 94**，上一轮那 12 行还没提交） | 去掉 Coffee 依赖（字段换 `List<WildcardShineLayer>`）；**方法签名行号保持不变**；文件尾追加 `public sealed class WildcardShineLayer`（≈340 行） |
| `Assets/CardPresentation/Shell/CollectionWindow.cs` | 改（+53 行） | `Nine(...)` 结果接住（`var wcBg`）+ `BuildWildcardShine(wcBg)`；类尾追加 `_wcShine` / `BuildWildcardShine` / `WildcardShineRate` / `StepWildcardShineForTest` |
| `Assets/CardPresentation/Shell/CampaignTab.cs` | 改（+82 行） | Icon 取住 + `AttachArmyShine`；`_armyShine` 字典；`PlayArmyIconShine` **实现**（原来是「未实现」告警）；`BuildArmyBadge` 改带过滤定义 + 就地订正那段「已知差异」注释 |
| `Assets/CardPresentation/Shell/ForgeTab.cs` | 改（+104 行） | Icon 取住 + `AttachArmyShine`；`_armyShine` 字典 + `SyncArmyShine`（`Refresh()` 里一行）+ `ArmyShineRate`；`BuildArmyBadge` 改带过滤定义 |
| `Assets/CardPresentation/Deck/DeckRuntime.cs` | 改（**本件 +62 行**；与 HEAD 的总差 91 行，另 29 行是并发的 `W6DECK` 的改动） | `RefreshAll()` 里 `FootShine(err == DeckError.None)`；类尾追加 `_footShine` / `FootShine` / `DoneShineRate` |
| `Assets/CardPresentation/Shell/BadgeNotifications.cs` | 改（+37 行） | `ForgeBadge` / `CampaignBadge` 加 `army` + `Validate`；两个 provider 逐条带阵营 |
| `资料/完全复刻/账/_parts/B9_W16SHINE.tsv` | **新建** | 5 行（`收藏:348`→A · `收藏:15`→A · `锻造:64` 改写 · `战役 CampaignHandler` 改写 · `特效动画:455`→A） |

**没有动**：任何 `*.unity` · `ProjectSettings/` · `CLAUDE.md` / `项目任务.md` / 任务总表 · 账主表 · 别人的报告 · 基线文件 ·
`ImageQuad.cs` / `MenuDraw.cs` / `WindowButton.cs` / `Editor/*`（都是别人的域）· git。

---

## 九、交接给下一个代理（本件无半成品）

- 三件事都做完了（B/D 的宿主定位除外，见 §四·1）；`typecheck` 0/0。
- **第一件事**：让 shader 编一次 + 进包（§七·1/2）—— **不编不验就等于没做**。
- **第二件事**：真 Play 看四处的扫光（收藏窗背景 / 战役阵营图标 / 锻造阵营图标 / Deck 的 Done）。
  若「扫光位置偏」⇒ 先查 `_ShineFrame` 是否跟上了窗口缩放（`FollowFrame` 只在 `Tick` 里跑）与 §三·1 的 🔶 算式。
- **第三件事**：把 §三 的「怎么验」落成断言（需要写 `Editor/CollectionScene.cs` / `RewardsScene.cs` / `DeckScene.cs` —— 不在本件所有权内）。
