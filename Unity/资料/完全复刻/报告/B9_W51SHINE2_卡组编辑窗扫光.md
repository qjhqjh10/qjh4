# B9_W51SHINE2 · 卡组编辑窗的扫光接线 ＋ 第 9 个实例（`Message Container ▸ Content`）查清并接上

> 执行代理 `B9_W51SHINE2` · 2026-10-27 · 有写权限
> **全程未跑 Unity（含不编译 shader）· 未动 git · 未改正本/账主表/别人的报告/基线**
> 每改一次 `.cs` 都跑过 `TMPDIR=/tmp/wf_w51 bash 工具/typecheck.sh`：
> 我自己那两个文件**一律 0 错**；最后两次跑到的红**全部落在别人的文件**（见 §四·4）。
> 白名单外**一个字没动**（`Effects/UIEffectTogglerCoffeeEffects.cs` 未碰 —— 见 §一·2 的理由）。

---

## 一、结论（三件，逐条）

| # | 任务 | 结果 |
|---|---|---|
| 1 | **接上卡组编辑窗的扫光** | ✅ 接上了：`Deck/DeckRuntime.cs:9033 WildcardShineAfterTab(int oldTab)`，由 `SetTab` 末句驱动；参数 = **复用的** `Params.WildcardDisplay`（**一行新预设都没加**）。**零行数变化**（`SetTab` 里只在两条已有语句上就地加词 ⇒ 行号一个没动，账里 74 处引用本文件的落点**零漂移**） |
| 2 | **查第 9 个实例（`Message Container ▸ Content` 那颗 139°）** | ✅ **我们工程里有这个窗** = **收件箱**（`Shell/InboxWindow.cs`，原版 `Inbox Menu` 的每一条 `Message Container` 条目）。父链按 `m_Children/m_Father` 真解过（不是按名字猜，见 §二·2）；用途字段 = `MessageContainer.shinyEffect` + `effectTimer = 3.0` ⇒ 它是**「未读行每 3.0×(1±0.2) 秒扫一下」**那条。**同一批接上了**（参数用 **139 那组真值**，⛔ 没套 120） |
| 3 | **`.meta` 说明** | `Resources/Shaders/WildcardShine.shader` **缺 `.meta`**（同目录另两个 shader 都有）⇒ **同步点第一次跑 Unity 时会自动生成并编译**。⚠️ 本件**没有造 GUID**、也没建 `.meta` |

**四句话**：

1. **Deck 那颗的播放时机不是「每次切页签」** —— 按反编译真体重排了一遍：它只在计数条**由关变开**那一拍播
   （Cosmetics → Cards/Deck info）；Cards ↔ Deck info 之间**不重播**（Unity 对已激活的 GO 再 `SetActive(true)`
   不发 `OnEnable`）；**开窗那一拍也不播**（`TryOpen` 里 `ToggleDisplay(1)` 排在 `FilterToDeck` → `Initialize` 之前，
   那一刻 `currentArmy` 还是出厂 0）。判据链完整写在 `WildcardShineAfterTab` 的 doc 里（§二·1）。
2. **`Params.WildcardDisplay` 一行没改**：Deck 那颗与收藏窗那颗**逐字段完全相同**（120 / 0.25 / 0.5 / (1,1,1,1) /
   filter 6 / **0.75 s** / Once），所以直接复用；**139/0.213/0.533/(2,2,2,1)/1.0 s 那组一个字段都没进 Deck**。
3. **第 9 个实例是「收件箱未读行的扫光」**，而且它**有一个现成的替代实现**：`InboxWindow` 原来因为「本工程没有扫光那一层」
   用「`New!` 字的亮度抬一档」冒充 ⇒ 这次**换成真扫光**（挂在每条未读行的底图 = 原版 `Content` 那张 `Image` 上）。
   那颗字的颜色**恢复恒定**（原版那颗字本来就不动）。
4. **两处如实标注的偏差**（⛔ 不是原版）：Deck 侧「材质惰性到第一次由关变开时才换」（零行数变化的代价，静止态逐像素相同）；
   收件箱侧「计时器在窗级、原版在行实例上」+「随机源是定种子 `System.Random`」。

---

## 二、证据（文件:行号 / 资产路径 + 字段）

### 2·1 Deck 编辑窗那颗的播放时机（🥇 机器码真体 + 序列化字段）

| 事实 | 判据 |
|---|---|
| 原版那一拍 = `WildcardDisplay.OnEnable`，闸是 `currentArmy != 0` | `d:/2/tools/decomp_full/WildcardDisplay__OnEnable.c`：`if (*(int *)(param_1 + 0x50) != 0) { Coffee_UIEffects_UIEffectTweener__Play(*(longlong *)(param_1 + 0x48),1,0); WildcardDisplay__SetCountersText(param_1,0); }` |
| `Initialize` **只写 army、不 Play** | `d:/2/tools/decomp_full/WildcardDisplay__Initialize.c`：早退于 `param_2 == *(int*)(this+0x50)`，否则 `*(int*)(this+0x50) = param_2` + 换旗 + `SetCountersText`，**没有** `UIEffectTweener__Play` |
| 计数条的唯一开关 = `wildcards(0xd0).SetActive(param)` | `d:/2/tools/decomp_full/DeckEditingWindow__ToggleDisplay.c`（同一方法里还有 `cardDisplay(0x78)` / `cosmeticDisplay(0x80)`） |
| 开窗顺序 = 先 `ToggleDisplay(1)`、后 `FilterToDeck`（⇒ 那一刻 army 还是 0） | `d:/2/tools/decomp_full/DeckEditingWindow__TryOpen.c`：`DeckEditingWindow__ToggleDisplay(*(longlong *)(lVar5 + 0x20),1,0)` 在 `DeckEditingPanel__Initialize` / `DeckEditingWindow__FilterToDeck(param_1,0)` **之前** |
| `deckArmy == 0` 时原版传字面量 **10**（Ultramarines）⇒ Initialize 之后 `currentArmy` 恒非 0 | `DeckEditingWindow__FilterToDeck.c:24-28`（本文件 `DeckRuntime.cs:1750-1759` 早已记着这条） |
| 参数（逐字段，与收藏窗那颗**完全相同**） | `UIEffect` MB `3594646202839789348` / tweener `2688218479952654116`（2026-10-27 现读，上一路 `B9_W36SHINEA` §2·4）；本次又亲手核了 `m_TransitionFilter = 4`、`keepAspect = 1`、`autoPlay = 0`、`rate = 1`、`range (0,1)`，与收藏窗那颗 `-3693768431965379883` / `1791554316153643733` **逐值相同** |

### 2·2 第 9 个实例：**我们工程里有这个窗**（三源，父链按 `m_Children/m_Father` 解）

**① 原始资产（父链，⛔ 没按名字猜）**

| 环节 | pathID | 说明 |
|---|---|---|
| 挂 `UIEffect` 的 GO | `-2571570060004938831` | `bundle_menus_assets_all/GameObject/Content_-2571570060004938831.json`，`m_Name = Content` |
| 它的 `RectTransform` | `8749932977937198001` | `m_Children` = **3**（Title / Date / New）· `m_Father` = `955468390103264177` |
| 父 RT = **根**的 RT | `955468390103264177` | `m_Father = 0`；它属于 GO `6777149060859177905` = `GameObject/Message Container.json`（`m_Name = Message Container`） |
| 根上的 `MessageContainer` 组件 | MB `-9188051066216764495` | 字段 `shinyEffect` → **`-3271889046122976335`** · `effectTimer = 3.0` · `background` → `5058986616577207217`（= `Content` 的 `Image`）· `unreadMark` / `selectButton` / `normalMessageSprite` / `selectedMessageSprite` 等 |
| `Content` 上的 `UIEffect` | MB `-7763153431244756047` | rotation **139** · width **0.213** · softness **0.533** · color **(2,2,2,1)** · `m_TransitionColorFilter 6` · glow 0 · reverse 0 · keepAspect 1 · autoPlay 0 · rate 1 |
| 同 GO 的 `UIEffectTweener` | MB `-3271889046122976335` | duration **1.0** · wrap **0 (Once)** · delay 0 · playOnEnable 0 · resetTimeOnEnable 1 |

`menu_dump.py bundle_menus_assets_all "Message Container" --depth 6` 的组件列也逐字对上：
`Message Container` = `MessageContainer`；`Content` = `Image, EverguildButton, EverguildButtonMaterialModifier, UIEffect, UIEffectTweener`。

**② 反编译（用途 = 未读行每 ~3 秒扫一次）**

`d:/2/tools/decomp_full/Everguild.LiveOps.MessageContainer__Update.c`：
```
if (*(longlong *)(param_1 + 0x30) != 0) {            // 有消息数据
  cVar4 = FUN_1800021f0(2,DAT_1842a39f0);            // = IsRead（同 OnInitialize 里 SetActive(!IsRead) 那个口径）
  if (cVar4 == '\0') {                               // 未读
    fVar5 = Time.deltaTime + *(float *)(param_1 + 0xa8);
    *(float *)(param_1 + 0xa8) = fVar5;
    if (*(float *)(param_1 + 0xac) <= fVar5) {       // 到点
      fVar1 = *(float *)(param_1 + 0x90);            // = effectTimer (3.0)
      *(undefined4 *)(param_1 + 0xa8) = 0;
      fVar5 = UnityEngine_Random__Range(-0.2, 0.2);
      *(float *)(param_1 + 0xac) = (fVar5 + 1.0) * fVar1;
      Coffee_UIEffects_UIEffectTweener__Play(*(longlong *)(param_1 + 0x88),1,0);   // +0x88 = shinyEffect
    }
  }
}
```
`+0xac`（当前阈值）与 `+0xa8`（已累计）都**不在 prefab 序列化字段表里** ⇒ 出厂 0 ⇒ **建行后第一帧就播一次**。

**③ 我们的工程里对应的窗**

| 事实 | 判据 |
|---|---|
| 那个窗 = **收件箱** `InboxWindow`（原版 `Inbox Menu`） | `Shell/InboxWindow.cs` 头部：`类名照原版：InboxWindow : BaseInboxWindow : GameWindow`；条目模板 = `bundle_menus_assets_all/GameObject/Message Container` |
| 条目 = `MessageContainer` 实例（原版由 `MessageList.Initialize` 逐个造） | `decomp_full/Everguild.LiveOps.MessageList__Initialize.c`：循环里 `ContainerBuilder<object>__MakeContainer(list, *(param_1+0x28), …)` → 之后 `Everguild_LiveOps_MessageContainer__ToggleSelection` / `__AnimateIn` |
| 我们手建的是**等价树**（不是实例化 prefab） | `Shell/InboxWindow.cs:1038 BuildRow`（节点名 `Message Container_<idx>`）+ `:1051` 那颗 `"Background"` quad（= 原版 `Content` 的 `Image`） |
| 全工程**只有这一处**是 `MessageContainer` 的对应物 | `grep -rn "MessageContainer"` 命中：`Shell/InboxWindow.cs`（正文/注释）· `Shell/InboxData.cs`（`GetCreatedTimeLocal` 照原版）· `Editor/RewardsScene.cs`（收件箱那一段自检）· `Effects/UIEffectTogglerCoffeeEffects.cs:155`（上一路订正的那句）—— **没有第二个窗** |

⇒ **接线落点** = 每条**未读**行的底图 quad；**参数** = `Shell/InboxWindow.cs:1243 RowShineParams`（139 / 0.213 / 0.533 / (2,2,2,1) / filter 6 / 1.0 s / Once）；
**触发** = `ShineTick`（`:1008`）到点 → `PlayRowShine`（`:1253`）。

### 2·3 账户/账里那条要改的旧口径

| 账 | 旧话 | 现在 |
|---|---|---|
| `社交_聊天.tsv` 的 `MessageContainer Update()` | 差异列：「原版是 Coffee.UIEffects 的 `UIEffectTweener` 扫光，**本工程没有那一层** ⇒ 用「亮度抬一档」代替（如实标）」 | **已过期** ⇒ 补丁 `账/_parts/B9_W51SHINE2.tsv` 第 3 行改成「真扫光已落地 + 仍存的两处偏差」 |
| `收藏.tsv` 的 `WildcardDisplay OnEnable()` | 落点只有收藏窗那一处 | 补丁第 2 行补上**第二个宿主**（Deck 编辑窗 `WildcardShineAfterTab`） |
| `卡组编辑.tsv` 的 `DeckEditingWindow ToggleDisplay(bool)` | 只写了「切卡片/卡背显示」 | 补丁第 1 行补上**同一拍的另一件副作用**（`wildcards.SetActive` ⇒ 扫光） |

---

## 三、改动清单

| 文件 | 动作 | 说明 |
|---|---|---|
| `Assets/CardPresentation/Deck/DeckRuntime.cs` | **+95 行 / 0 行改动**（8998 → 9093） | `SetTab`（`:7061` / `:7065`）两条**已有语句就地加词**（零行数变化）＋ 类尾 95 行（`_wcShine` / `_wcShineWarned` / `WildcardShineAfterTab` / `WildcardBarRoot` / `WcShineRate` / `WcShineEnabled`，`:8997-9091`） |
| `Assets/CardPresentation/Shell/InboxWindow.cs` | **+68 / −1 行**（1230 → 1298） | ① `_rowNew`(Label) → `_rowShine`(扫光层) 三处同形替换；② 计时器那段注释**就地订正**（原文「本工程没有那一层」已过期）＋ `ShinePulse`/`_shinePulse` 删除、`_shineNext` 出厂 0；③ `ShineTick` 改成 `PlayRowShine()`；④ `BuildRow` 那颗 `New!` 字不再登记/改色；⑤ 类尾：`RowShineParams`（139 那组）+ `PlayRowShine` + 三个自检读口 |
| `资料/完全复刻/账/_parts/B9_W51SHINE2.tsv` | 新建（3 行 + 表头，11 列，LF） | 见 §2·3 |
| `资料/完全复刻/报告/B9_W51SHINE2_卡组编辑窗扫光.md` | 新建 | 本文件 |

**行尾**（二进制核过）：`DeckRuntime.cs` = **CRLF 9093/9093**、裸 CR 0；`InboxWindow.cs` = **纯 LF 1297**、裸 CR 0。
⚠️ `git diff --numstat` 对本件**不可用**：`DeckRuntime.cs` 工作树里本来就有别的写手的未提交改动（我第一次看是 `230/3`），
我的增量只能按**行数差**算（+95）。

**没动**：`Effects/UIEffectTogglerCoffeeEffects.cs`（共用件，本波有别的写手；所以 `RowShineParams` 写在 `InboxWindow` 里）·
`Resources/Shaders/WildcardShine.shader` 与它的 `.meta` · `*.unity` · `ProjectSettings/` · 账主表 · 两张正本 · 别人的报告 · git。

---

## 四、没查清的部分（「查不到 + 还差什么」）

1. **真机画面一次都没看过**（红线：不跑 Unity）⇒ 「扫光在 Deck 条上 / 收件箱底图上**看起来**对不对」**没有视觉证据**。
   还差什么：同步点跑 `DeckScene.Run` + `RewardsScene.Run`，再按铁律 12 进 player 看一眼（`行首标记` 口径）。
2. **`WildcardShine.shader` 从未编译过**（本波不许跑 Unity）⇒ 断言只能保证「取得到 + 参数写进去了 + shader 名对」。
   还差什么：第一次跑 Unity 时的编译结果 + `.meta` 生成。
3. **收件箱那条的「60 帧内会不会跟入场动画打架」没验**：扫光会在**建行后第一帧**开播（原版也这样），
   而入场的 alpha/scale 动画同时在进行（0.1~0.4 s）。两者都写同一份材质的 `_Color` / 顶点 —— 理论上互不冲突
   （一个是 `_Color`、一个是顶点色/缩放），但**没有帧级证据**。还差什么：真机看一次「开窗瞬间」。
4. **`MessageContainer.Update` 的 `+0x30` 那个字段我没解出名字**（只证实它是「消息数据非空」那一跳，
   因为 `OnInitialize` 里同一个 getter 被用来给 `+0x50`（`unreadMark`）`SetActive(!IsRead)`）。
   影响：**零**（我们这边用 `_messages[i].Unread` 表达同一件事），但这半句**不是一手名字**，如实标在这里。
5. **本波 `typecheck` 最后两次都红了，但红全在别人的文件**：
   `RuleEngine/Core/RuleCore.cs`（`CS0103 EmitAbilityEvent ×2`，W43 在写）→ 后来变成
   `Battle/CardPresentation/Battle/BattleDriver.cs`（`CS0103 SkillPanelMaxTargets / SkillPanelSingleTargetName / EnemyCardPlayedDisplayPointWorld ×3`，W49 在写）。
   **我这两个文件在每一次跑里都是 0 错**（编辑器程序集两次都是 0/0）。⇒ 按铁律 13·3 第 3 条：那不是我的问题，
   **别去改别人的文件**；同步点复跑一次再看。

---

## 五、顺手发现（请调度台分流 —— ⛔ 我一条都没动手）

1. 🔴 **`MessageContainer.curve` 其实是【有数值】的**（我们代码里写着「曲线没解出数值」）：
   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-9188051066216764495.json` 的 `curve` 字段是**完整 3 键**
   （`m_PreInfinity/postInfinity = 2 (Clamp)`）：
   `(t=0.0, v=0.0, in/outSlope 0)` · `(t=0.3516588, v=0.6244674, slope 1.5301329, inW 0.3333, outW 0.19533)` ·
   `(t=1.0021973, v=1.0, slope −0.4018442, inW 0.5416, outW 0)`。
   ⇒ `Shell/InboxWindow.cs:327`（`AnimDelay` 那段）里那句「⚠️ 曲线**没解出数值**（`m_Curve` 在 MB 里、本件没读）⇒ 我们用
   `EaseOutQuad` 等价」**可以升级成原版曲线**（现在是 🔶 自己设计）。**还差**：把 3 键做成 `AnimationCurve` 采样并替换
   `ApplyRowAnim` 里的缓动（`DOScale` 与 `DOFade` 可能共用同一条曲线，需逐句核 `AnimateIn`）。
2. 🔴 **`WildcardDisplay.UpdateCounters` 也播扫光** —— `decomp_full/WildcardDisplay__UpdateCounters.c`：
   `Coffee_UIEffects_UIEffectTweener__Play(*(longlong *)(param_1 + 0x48),1,0)` + `SetCountersText`，
   由**万能卡库存变动信号**驱动。我们这边 `Inventory.WildcardInventory.UpdateItemCollection` 只发 `Updated`
   （`收藏.tsv` 的 `WildcardInventory UpdateItemCollection` 那行），**没有任何画面反应**。
   ⇒ 这是「收藏窗 / Deck 编辑窗在那条信号来时要**重播扫光**」的缺口（和 `收藏.tsv` 把 `UpdateCounters` 判 D 那个口径冲突 —— 那条 D 的理由是「缺发放源」，但**信号回调本身是本地可做的**，只是我们恒不发）。
3. ⚠️ **`DeckRuntime.cs` 里 `WcBarQuadNodes` 那条注释可能不准**（不是我的文件、只是读过）：
   `Editor/DeckScene.cs:245-250` 说 `hdr_wcbg` 是 `Img()` 建、登进了 `_named`；但 `DeckRuntime.cs:1693` 它是
   `MenuDraw.Nine(...)` 建的（**根上没 `ImageQuad`**、九块在子件、`_named` 里**没有**它）⇒ `UiQuadActive("hdr_wcbg")` 大概恒 false。
   我没有 `Editor/DeckScene.cs` 的白名单，**没改也没验**，请派给有那个文件的人。
4. ✅ **`hdr_wcbg` 是 `Root` 的【直接子件】**（`MenuDraw.Nine(Root, …)`）⇒ `Root.Find("hdr_wcbg")` 一次命中，
   不需要 `FindDeep`（那条只是兜底）。这条对下一个人有用：**别改成遍历整棵树找**。
5. 📌 **同步点必做**（本件做不了）：① 让 Unity 编一次 `WildcardShine.shader`（**从未编译过**，`.meta` 会在这一趟生成）；
   ② 跑 `DeckScene.Run` + `RewardsScene.Run`（我动了 `Deck/**` 与 `Shell/InboxWindow.cs`，后者被 `RewardsScene` 的收件箱那一段盯）；
   ③ `check_ledger_points --all-A --fix`：**`InboxWindow.cs` 行号整体下移**（+68 ⇒ 账里 37 处 `Shell/InboxWindow.cs:NNN`
   会漂移；补丁里给的两个新落点 `:1008` / `:1253` 是按**改后**行号写的）；**`DeckRuntime.cs` 零漂移**（我特意保住的行数）。

---

## 六、交接给下一个代理（本件无半成品）

- 三件都做完（含把第 9 个实例接上）；未跑 Unity；未动 git。
- **自检读口**（给写断言的人，都不需要碰我的逻辑）：
  - Deck：`DeckRuntime.WcShineRate`（`-1` = 层还没挂）· `DeckRuntime.WcShineEnabled`；
    **触发法** = `rt.UiSetTab(2); rt.UiSetTab(0);`（= 原版 Cosmetics → Cards 那一拍）；**推进** = `WildcardShineLayer.StepAll(dt)`
    （批处理没有帧泵）。期望：`UiSetTab(0)` 之后再切 `UiSetTab(1)` ⇒ **rate 不变**（不重播，判据见 `WildcardShineAfterTab` 的 doc）。
  - 收件箱：`InboxWindow.RowShineRate(i)` / `RowShinePlaying(i)` / `RowShineCount`；**触发** = `win.Tick(0.01f)`（`_shineNext` 出厂 0 ⇒ 第一拍就播）；
    **推进** = `WildcardShineLayer.StepAll(dt)`；期望：未读行 `rate` 从 0 起、1.0 s 后到 1 且 `Playing == false`；已读行 `-1`。
- **断言宿主不在我的白名单**：`Editor/DeckScene.cs`（W46）· `Editor/RewardsScene.cs`（收件箱那一段）。⇒ 上面这些读口的断言请派给有那两个文件的人。
- 若同步点发现「Deck 条上根本看不到扫光」，**先查三档**：① shader 编过没有（`.meta` 生成了吗）② `Params.WildcardDisplay` 有没有被谁改
  ③ `hdr_wcbg` 的九块是不是被 `ClipNineChildren` 关掉了（关掉的块 `AttachNine` 也会挂，但看不见）。
