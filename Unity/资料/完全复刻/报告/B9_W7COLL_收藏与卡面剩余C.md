# B9_W7COLL · 收藏线 + 卡牌面 剩余 C 级条目（K-02 / K-13 / K-14 / K-17）

> 执行代理 `B9_W7COLL` · 2026-10-27 · 派活包 = `判据/B9_P0剩余C_派活包.md`（只读 K-02/K-13/K-14/K-17 四节）+
> `判据/B9_P0剩余C_施工单.tsv`。账补丁 = `账/_parts/B9_W7COLL.tsv`（3 行）。
> 全程 **未跑 Unity**、**未动 git**、**未改正本/主表**；每改一次 `.cs` 跑过
> `TMPDIR=/tmp/wf_w7 bash 工具/typecheck.sh`，**最终 0/0**（中途红过两轮，全部落在别人正在写的
> `Core/CardView.cs`（W4）与 `Editor/DeckScene.cs` + `ItemDrawer`（W6），复跑即绿 —— 不是我的文件）。

---

## 一、结论（逐条）

| 包 | 账行号 | 原等级 | 本轮结果 | 落点 |
|---|---|---|---|---|
| K-02 | **HUD:101** `BattleCardUI.BattleCardSetup` | A（已做） | **核销：不改** —— `CardArt.DeckCardbackSdf(cardbackId, faction)` 已在（`CardArt.cs:1300`），调用点在 `BattleDriver.cs:31190`（K-01 域）。本轮只复核，无改动 | `Core/CardArt.cs`（已有） |
| K-02 | **收藏:203** `GameModesIconCollection.Release` | A（已做） | **核销：不改** —— `CardArt.ReleaseModeIcon(int?)` 已在（`:1268`）、销毁那一跳已在 `MenuDraw.ReleaseArtUnder`（`:3635`，先 `IsCached` 挡掉 N-1 次）；`PerItemArtPrefix` 仍**不含** `ui_menu/` ✅ | `Core/CardArt.cs`（已有） |
| K-17 | **收藏:247** `InventoryManager.HasEnough<T>(LoadableReference<T>, int)` | C | **做完 → A**：新建 `Core/LoadableRef.cs`（142 行）+ `Inventory.HasEnough<T>(LoadableRef<T>, int)` | `Core/LoadableRef.cs`（新建）· `Shell/Inventory.cs:1310` |
| K-14 | **收藏:15** `ArmyItemContainer.Initialize` | C | **做完一半 → B**：战役页/锻造页每颗阵营格建 `Badge Highlight` + 计数行 + `BadgeWiring.Attach(…)`。**仍差「按阵营过滤」**（见 §三·1，卡在别人的账行上） | `Shell/CampaignTab.cs:1633` · `Shell/ForgeTab.cs:1295` |
| K-13 | **收藏:348** `WildcardDisplay.OnEnable` | C | **保持 C（部分交付）**：shader `TRANSITION_SHINY` **已忠实移植**（`Shaders/WildcardShine.shader`，169 行、逐条式子带出处）+ 掩码贴图**已解出并证明可解析复现**；**未接线**（原因见 §三·2：那颗 Background 是 9 个子 quad）。另**订正了账里那张 W64 表的适用对象**（它属于另一个实例） | `Shaders/WildcardShine.shader`（新建） |

**没做的**：`收藏:244`（`GetOwnedCount<T>(LoadableReference<T>)`）按派活包**保持 D、本轮不做**。
核了一遍：它确实**不需要做**——三源（prefab `m_MethodName` / 17 个场景 / SO）0 引用、C# 侧 29 处
`GetOwnedCount` 无一处传 `LoadableReference` ⇒ 派活包给的「建议 D」站得住，**没有需要调度台裁的东西**。

---

## 二、证据（文件:行号）

### K-17（收藏:247）
- 原版签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/LoadableReference.cs` —— 三格字段
  `genericObjectReference`(UnityEngine.Object) / `targetId`(string) / `reference`(T) + `TargetId` / `Reference`。
- 原版 `TargetId` getter 语义：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/LoadableReference.cs` 的 `TargetId` 体
  首句 `if (reference != null) … this + 24 …`（+24 = 0x18 = `targetId` 那一格）⇒ **reference 非空时先用它回填 targetId**。
- `HasEnough` 的比较式：`decomp_full/InventoryManager__HasEnough_object_.c` 末句 `return param_3 <= iVar1;`
  （= `amount <= owned`）；⚠️ `LoadableReference` 那一支的机器码**没有单独导出**，与既有两部同族保持一致是**推来的**（已在代码注释里写清）。
- 3 处活调用（判据行照抄施工单）：`CurrencyCondition__Validate.c:14` · `CardServices__CheckForCardUpgrade.c:88` · `UpgradePanel__CheckUpgradeRequirements.c:106`。
- 我们的落点：`CardPresentation/Core/LoadableRef.cs:42`（类）· `CardPresentation/Shell/Inventory.cs:1310`（`HasEnough`）。

### K-14（收藏:15）
- 原版三段：`decomp_full/ArmyItemContainer__Initialize.c`（`param_4` = `ArmySelectorType`：`==0` 直接 return；
  `(parentType-1) != 1` ⇒ `CampaignBadge`；else ⇒ `ForgeBadge`；两支都 `AddNotificationType(new Notification[]{badge})`）
  ＋ C# 形状 `cpp2il/out_cpp2il_dev_ilspy/ArmyItemContainer.cs:202-360`。
- 两个 record 的过滤语义：`decomp_full/Everguild.UI.Notifications.RewardsBadge__Validate.c` —— `other` 是
  `RewardsBadge` 且**类型不同**时比 `parentId`，**同类型**时落到 record 的 `Equals`（⇒ `CampaignBadge.army` 进比较）
  ⇒ 原版是**按阵营**过滤，我们今天只能按**类型**。
- prefab 版式（`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Campaign Army Item Button" --depth 3`）：
  `Badge Highlight` = 35×35 · 锚 (1,1) · pos (−17.5,−17.5) · 图 `40K_notification_number`(65×65) ·
  Simple + preserveAspect · 色 (0.736,0.736,0.736,1) · 组件 `Image,UiBadgeNotification,EverguildButtonMaterialModifier`；
  子件 `OneText`（fs 34 / base 36 / auto[18,34] / Center-Middle / 白）。`Forge Army Item Button` 逐项相同。
- 我们的落点：`Shell/CampaignTab.cs:1633`（`BuildArmyBadge`）+ `:906`（调用，插在 `Icon` 之后、`Slider` 之前 = prefab 子件序）；
  `Shell/ForgeTab.cs:1295`（`BuildArmyBadge`）+ `:770`（调用，替换原来那颗 alpha 0 的空壳）。

### K-13（收藏:348）—— 四步判据链（全是直读，没有一条是猜）
1. **挂在哪个节点**：收藏窗那颗 `WildcardDisplay`（MB `9137287304817861333`）的 `shinyEffect` PPtr →
   GO **`5282456591777456853` = `WIldcard Display ▸ Background`**。
   同一 GO 上的组件（UnityPy 现读）：`Image`（`m_Type 1` Sliced · `m_Sprite` 外链 · `m_Color (0.46226,0.46226,0.46226,1)`）
   + **`UIEffect`** + **`UIEffectTweener`** + `CanvasRenderer`。
   🔴 另一个实例（MB `6030196207654661924`，`Deck Editing Menu ▸ WIldcard Counter`）的 `shinyEffect` → `WIldcard Counter ▸ Background`。
2. **参数（两个实例不同 —— 铁律 5·c）**：
   | 字段 | 收藏窗这颗（本行） | `Deck Editing Menu` 那颗（= 账里 W64 表） |
   |---|---|---|
   | `m_TransitionRotation` | **120** | 139 |
   | `m_TransitionWidth` | **0.25** | 0.213 |
   | `m_TransitionSoftness` | **0.5** | 0.533 |
   | `m_TransitionColor` | **(1,1,1,1)** | (2,2,2,1) |
   | tweener `m_Duration` | **0.75** | 1.0 |
   两实例相同的有：`m_TransitionFilter 4 (Shiny)` · `m_TransitionRate 1.0` · `m_TransitionReverse 0` ·
   `m_TransitionColorFilter 6` · `m_TransitionColorGlow 0` · `m_TransitionKeepAspectRatio 1` ·
   `m_TransitionAutoPlaySpeed 0` · `m_BlendType 1 / Src 1 / Dst 10` · `Tone/Color/Sampling/Detail = 0`。
3. **shader + 像素式子**：`globalgamemanagers` 的 `Shader_77`（`m_ParsedForm.m_Name` = `Hidden/UI/Default (UIEffect)`）→
   ruri 导出 `d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/Hidden_UI_Default (UIEffect)_0/`
   24 个关键字变体；**`TRANSITION_SHINY` 那支 = `Sub0_Pass0_Fragment_b16.hlsl`（747 行）**。
   关键式子已逐条落进 `WildcardShine.shader`（`_416/_429/_430/_433/_439/_440/_444/_447/_501/_510.._513` 原名保留）。
4. **掩码贴图**：`m_TransitionTex` = externals[14] = `CAB-34ccb919c2f6cc86831e4bdd13fd16cf` → 该 CAB 内的 Texture2D
   **`Transition-Horizontal`**（256×256 · `TextureFormat 1` = Alpha8）。逐像素（UnityPy）：y 每 32 采样
   `254, 223, 190, 159, 127, 95, 63, 31`（横向 8 采样全同）⇒ **沿 y 严格线性**（`≈ 255·(256−y)/256`，8 bit 量化）
   ⇒ **`1 - saturate(uv.y)` 是逐像素等价复现，不是近似**（这条是本轮最关键的一条：原来判「贴图解不出就做不了」，
   其实它可解析复现，只是**它的方向/缩放仍由组件算的那张 UV 矩阵决定**）。

---

## 三、逐条「原版怎么算 / 我们原来怎么算 / 改成什么 / 怎么验」

### 1) 收藏:15 · `ArmyItemContainer.Initialize` 的 Campaign / Forge 两支
- **原版怎么算**：`parentType` 三段。`Neutral` 不挂；`Campaign` 给每格 `new CampaignBadge(army)`、
  `Forge` 给每格 `new ForgeBadge(army)`，都 `AddNotificationType(new[]{badge})` 到该格的 `UiBadgeNotification`
  ⇒ 该格的红点计数 = 「与该 `army` 相符的通知条数」，`count > 0` ⇒ `Show()`（alpha 3/s 补间 + 计数文字），否则 `Hide()`。
- **我们原来怎么算**：`CampaignTab.BuildArmyItems` **一颗徽记节点都没建**（只在 `ArmyItemW` 的注释里写着
  「35×35 右上角 (−17.5,−17.5)」）；`ForgeTab.BuildArmyItems` 建了那颗 quad，但 **tint α = 0 且没有任何驱动**
  （无 `UiBadgeNotification`、无定义注册）⇒ 节点在、红点永不亮。
- **改成什么**：两窗各加 `BuildArmyBadge(item, army, r)` —— 建 `Badge Highlight`（35×35 · 锚 (1,1) · pos (−17.5,−17.5) ·
  `40K_notification_number` · preserveAspect · 色 0.7358 中灰）+ 计数行 `OneText`（`BadgeWiring.Attach` 按原版那组数建）
  + `BadgeWiring.Attach(…, definitions = [typeof(CampaignBadge)] / [typeof(ForgeBadge)])`；
  队列用**新的** `QArmyBadge`(战役 3037 / 锻造沿用 3024) 与 `QArmyBadgeText`(战役 3038 / 锻造 3031) ——
  那 35² 里没有别的件 ⇒ 与谁都不重叠（分层仍走渲染队列，⛔ 不用 z）。
- **怎么验**（**本批没跑**，见 §四）：
  ① 战役页 / 锻造页每颗 `CampaignArmyItem_*` / `ForgeArmyItem_*` 下有且只有 1 颗 `Badge Highlight`（13 颗各一）、
  收藏左抽屉 `Cell_fac_*` **没有**；② 那颗 quad 的 shader/材质在、`UiBadgeNotification.notificationDefinitions` 长度 1 且类型对；
  ③ 有可领内容时 `CurrentCount > 0` 且 alpha 补间到 1（批处理用 `SnapToTargetForTest()` 或 `Step(1f)` 读终值；
  ⚠️ 别用 `activeSelf` 判 —— 红点是 **alpha 显隐**、`activeSelf` 恒 true）。

### 2) 收藏:348 · `WildcardDisplay.OnEnable` 的 Shiny
- **原版怎么算**：`OnEnable` 里 `if (currentArmy != Neutral) shinyEffect.Play(resetTime:true)`（`Initialize(army)` **不播**、
  `UpdateCounters()` 里再播一次）。`shinyEffect` = `UIEffectTweener`（0.75 s 线性 0→1）驱动那颗
  `Background` 上的 `UIEffect`（Shiny / 120° / 0.25 / 0.5 / 白 / filter 6），shader 侧是一段
  「旋转后的线性斜坡 + 带宽 + 软边」的三角带（式子见 `.shader` 头注与 `b16.hlsl`）。
- **我们原来怎么算**：完全没有（工程里 0 个 Shiny shader、0 处 `TransitionTex`）。
- **改成什么（本轮）**：`CardPresentation/Shaders/WildcardShine.shader` —— `TRANSITION_SHINY` 那一支的移植，
  参数名/语义逐字照原版（`_TransitionRate/Width/Softness/Color/ColorFilter/ColorGlow/Reverse/AutoPlaySpeed`、
  `_TransitionTex/_TransitionTex_ST`、外加 `_ShineMatrix` = 原版 `_RootViewMatrix` 的替身）；
  掩码贴图**不必**要（解析式等价），也留了「贴上真贴图就走贴图」的口（`_UseTransitionTex`）。
  ⚠️ 只实现这一档（原版另外还有 `TONE_GRAYSCALE` / `TRANSITION_DISSOLVE` / `EDGE_SHINY` +
  Gradation/Target/Detail/Shadow/Color/Sampling 与 UI 裁剪两路变体 —— 本实例全是 None，已如实写在文件头）。
- **⛔ 为什么没接线（卡在哪，一句话）**：原版那颗 `UIEffect` 挂在 `WIldcard Display ▸ Background` 上，而那颗
  Background 是 `ImageQuad.CreateNineSlice` 建的 **9 个子 quad**（`ImageQuad.cs:564` 的三层循环）。
  shader 的掩码坐标要用**整框 uv**，而子 quad 的 `i.uv` 是**图集 uv** ⇒ 每块都得再给一份
  「子块 uv → 整框 uv」的仿射（`sx,sy,ox,oy`），于是**每块各要一份材质**（或 MPB）——
  这不是一根线，是九根，且**本批不许跑 Unity ⇒ 无法验证阴影/队列/材质泄漏**。
  半做 = 只给 1/9 块染上材质（画面错、还静默），所以**停手不接**。
  👉 **下一步的确定做法**（已核过 API，可直接照做）：`Nine(...)` 拿到 root 后遍历
  `root.GetComponentsInChildren<ImageQuad>()`，对每块读 `q.UvRect` + `MenuDraw.QuadRectPx(q)`，
  算 `sx=(块宽/框宽)/uvW`、`ox=块x1/框宽 − uvX·sx`（y 同理），各建一份材质（材质用 `_wcShineMat` 缓存字典复用），
  `SetMaterial` 后统一 `PlayWildcardShine()`；播放下标 = **建页时**（= 原版 `OnEnable`），**换旗不播**。

### 3) K-17 / K-02（结论见 §一，证据见 §二）
- 收藏:247 的做法与验收口（三档夹具：=amount ⇒ true · −1 ⇒ false · +1 ⇒ true · 空 TargetId ⇒ 出声且 false）见
  `Inventory.cs:1310` 的 doc；**夹具没跑**（红线）。
- K-02 两条都已是 A 档且落点在别人的域（`BattleDriver`），本轮只复核、**零改动**；
  按派活包要求确认了两件事：`DeckCardbackSdf` 存在且被牌堆那一层调用；`PerItemArtPrefix` **仍不含** `ui_menu/`。

---

## 四、没查清的部分 / 没跑的部分（如实记）

1. **K-13 的 `_RootViewMatrix` 公式拿不到**：Coffee 的 context 在 **C# 侧**，而 `decomp_full` 里
   `Coffee_UIEffects_UIEffect*` **0 条方法体**（`Coffee.UIEffect.dll` 是独立程序集，那趟反编译没覆盖它的方法）。
   ⇒ `.shader` 里 `_ShineMatrix` 由 C# 按「绕中心旋转 + 按宽高比归一」（`p' = R(θ)·(a·x, y)/a`，`a=宽/高`）现算，
   **已标 `🔶 自己设计`**（写在与它配套的 `ShineUvMatrix` 说明里；本轮接线撤掉了，所以这段说明现在只存在于本报告 §三·2 与
   `.shader` 的 `_ShineMatrix` 注释里 —— 接线的代理照那段写即可）。
2. **K-13 没跑 Unity ⇒ 那个 `.shader` 一次都没编译过**。⚠️ 这是本报告最大的未验证项：
   `typecheck.sh` 只编 `.cs`，不编 shader。**下一个动这块的代理必须先让它在编辑器里编一次**
   （最省的办法：开工程看 Console 有没有 `Shader error in 'CardPresentation/WildcardShine'`）。
3. **K-13 的 shader 进包问题**：它在 `CardPresentation/Shaders/`（不在 `Resources/`），`Shader.Find` 在 player 包里
   需要 `ProjectSettings/GraphicsSettings.asset` 的 AlwaysIncluded —— **那份工程设置不在本件所有权内，未改**。
   接线的代理要么加进 AlwaysIncluded，要么把它放进 `Resources/`（两条都要在报告里留痕）。
4. **K-17 / K-14 的断言一条都没跑**（红线：本批不许跑 Unity 宿主）。建议落点：
   - `收藏:247`：`Editor/CollectionScene.cs`（或 `DeckScene.cs`）加三档夹具 + 空 TargetId 的「出声且 false」。
   - `收藏:15`：`Editor/RewardsScene.cs` 已有一整套战役/锻造阵营条断言（`FindChild(fArmy,"ForgeArmyItem_1")` 那一批），
     在它旁边加「每格有且只有 1 颗 `Badge Highlight`」「`UiBadgeNotification.notificationDefinitions.Count == 1`」
     「收藏窗 `Cell_fac_*` 没有这颗」三条即可 —— ⛔ 别用 `activeSelf` 判显隐（红点恒 active）。
     ⚠️ **顺序坑**：`BadgeWiring.Attach` 末尾就 `Recount()`，而 provider 是靠 `BadgeWiring.RegisterDefaults()` 登记的
     （生产入口 = `Shell/ShellRuntime.cs:827` / `MainMenuRuntime.cs:403`；自检入口 = `Editor/RewardsScene.cs:3554`）。
     如果自检里**先建页、后登记**，那一次 `Recount` 会数到空表 ⇒ 红点 alpha 停在 0（看着像「没接上」）。
     验计数前先 `UiBadgeNotificationManager.Refresh(true)` + 对那几颗 `UiBadgeNotification` 再 `Recount()`。
5. **`ForgeBadge` 缺 `army` 字段这件事目前【没有账行】**（见 §五·1）。

---

## 五、顺手发现（请调度台分流）

1. 🔴 **`ForgeBadge` 也没有 `army`**，而 `账/锻造.tsv` 的 `ForgeHandler.CheckNotification` 那行现判 **A「一致」** ——
   原版 `ForgeHandler.<CheckNotification>b__9_0`（`decomp_full/…b__9_0.c`）是
   `Repeat(new CampaignBadge(army…), Count(GetRewardsToCollect()))`（decompiler 把类名印成 CampaignBadge，实参是 forge 的 army），
   ⇒ **原版 ForgeBadge 带 army、逐阵营计数**；我们是 `yield return new ForgeBadge()` 全阵营摊平 ⇒
   那一行「一致」**不成立**，应降 B 并补 `army`。**建议新开/改一行账**（本件不改正本，故只报）。
2. **账里 `收藏:348` 的 W64 参数表适用对象错了**（见 §二·K-13 第 2 步的对照表）：那套 139/0.213/0.533/1.0s/(2,2,2,1)
   > 🔴 **2026-10-27 就地订正（铁律 5 · `B9_W36SHINEA` 一手现读）**：那套 139/0.213/0.533/1.0s/(2,2,2,1) **既不属于收藏窗、也不属于 Deck 编辑窗** —— 全库只有一个 `m_TransitionRotation==139` 的 UIEffect（`MonoBehaviour_-7763153431244756047`），父链是 **`Message Container ▸ Content`**（脚本 `MessageContainer`）。⇒ **Deck 编辑窗那颗与收藏窗逐字段完全相同**（120 / 0.25 / 0.5 / (1,1,1,1) / filter 6 / **0.75 s** / Once）；139 那一套是**第 9 个实例**（已派 W51 查它在我们工程里有没有对应窗）。
   属于 `Deck Editing Menu ▸ WIldcard Counter`，**不是**收藏窗这颗。已写进账补丁的「差异」列（铁律 5 就地订正）。
3. **`_TransitionTex` 那张图其实不必解**：`Transition-Horizontal` 是沿 y 的严格线性斜坡（逐像素实测）⇒ 解析式等价。
   这条对**同族其它 Shiny 需求**（`B9_菜单商店C_派活包.md` 里「战役 19 进度条流光」「轮抽 `UpdateDeckInfoPanelAnimSync`」）
   同样成立 —— 它们也是 120/0.25/0.5（与收藏窗这颗一致），可以复用同一个 shader。
4. **子 quad 与材质**：`MenuWindowBase.Nine` 返回的是**整棵树的根**，`GetComponentInChildren<ImageQuad>()` 只会拿到
   **1/9 块** —— 任何「给九宫格换材质/染色」的活都必须遍历 `GetComponentsInChildren<ImageQuad>()`。
   （我差点踩：第一版就是这样写的，读到 `ImageQuad.cs:564` 的三层循环才改。）
5. **`CollectionWindow.Update` 只有一个消费者**（`TickDrawers`）；这个窗是 MonoBehaviour，**可以**在 `Update` 里挂
   第二条每帧泵（批处理没有帧循环 ⇒ 自检要走确定性口，照 `TickDrawers(dt)` 那条先例）。
