# D2 · 桶 B / 桶 C 两个崩溃 —— 只读诊断（第四十八会话）

> 只读诊断：未改任何 `.cs` / 资产 / 场景，未跑 Unity、未动 git。
> 判据来源：本仓代码（逐行现读）· `d:/2/tools/decomp_full/` 原版方法体 · `资料/普查产出_1017/自检_第四十七会话_结果与分诊.md` 桶 B/C。
> ⚠️ 两次都**没有留下日志文件**（`_tmp_view/` 里找不到这两条的 NRE 栈），所以「崩在哪一句」有一处是**按调用顺序 + 栈函数名推断**的，下面逐条标出。

---

## 桶 B · `ShellScene.Run` 崩：`ToastNotifications.Fill → Label.SetText → Label.SetTextDot`（`Label.cs:1996`）

### 1. 复现路径

- **宿主**：`ShellScene.Run`（编辑器批处理，`Application.isPlaying == false`）。
- **夹具/触发点**：本会话**新增**的那一节 `Editor/ShellScene.cs:6961-7059`（`N20/N21/N23：Toast 控制器`）。
  - `:6967` `ToastNotifications.Build(root)` —— 建四个模板（`Shell/ToastNotifications.cs:398-401`），模板里的 `Text` 都是 `MenuDraw.Text → Label.Create` 建的真 TMP `Label`。
  - `:6996` 第一句 `ToastNotifications.Show(Achievement, shortT)`（`shortT = "Hi"`）。
  - `ShowNotification → ShowInternal`：`ToastNotifications.cs:814` **`Instantiate(_templateByType[ti], _holder, false)`**，`:818` `Fill(...)`。
  - `Fill` 的 Achievement 分支 `:919` `lb.SetText(text)` —— 对被**克隆出来的** `Text` 节点上的 `Label`。
  - `Label.SetText`（`Battle/Label.cs:251-261`）：`_tmp == null` ⇒ 走 `:261 SetTextDot(...)`。
  - `Label.SetTextDot`（`:1980-2002`）：**`:1996` `var old = _mr.sharedMaterial.mainTexture;`** —— `_mr == null` ⇒ 空引用。
- **推断部分**：栈只给了 `Fill`，没给 `Fill` 的行号。按 `ShellScene` 的调用顺序（第一句就是 `Show(Achievement,…)`），走到的是 **`:918-919` 那一支**；这条是推断、未被日志直接证实（见 §5「没查清」）。

### 2. 根因（`文件:行号` + 关键代码）

**根因一句话：`Label` 不可被 `Instantiate` —— 它的后端引用是【私有、无 `[SerializeField]`】的运行时字段，克隆体上全丢。**

- `Battle/Label.cs:51-52`：
  ```csharp
  MeshRenderer _mr;      // 私有、无 [SerializeField] ⇒ 不进 Unity 序列化
  MeshFilter   _mf;
  ```
  `Battle/Label.cs:62`：`TMP_Text _tmp;`（同上）。
  `Label` 全文 **零个 `[SerializeField]`、没有 `Awake`**；`OnEnable`（`:493`）只跑 `TryApplyPendingWrap()`。⇒ 后端只在 `CreateCore` 建一次（`Label.cs:185-197`：`BuildTmp()`，失败才 `BuildDot()`）。

- Unity `Instantiate` **只按序列化字段克隆**（等价于「另存 → 载入」）：私有未标注字段不复制。模板上的 `_tmp`/`_mr`/`_text`/`_ugui`/`_vTier`… 在克隆体上全部是**字段默认值**：
  - `_tmp == null` ⇒ `SetText`（`:259`）判定「点阵后端」；
  - 而模板本来是 TMP 后端（`TmpFont.Available` 为真时 `CreateCore:185` 只走 `BuildTmp`，**根本没建 `MeshRenderer`**）⇒ 克隆体上既没有 `_mr`、也没有 `_tmp`；
  - `SetTextDot:1996` 直接解引用 `_mr.sharedMaterial` ⇒ NRE。
  - （若模板恰好是点阵后端：`MeshRenderer` 组件会被克隆过去，但 `Label._mr` 字段仍是 null ⇒ **同一句、同一个 NRE**。两条路殊途同归。）

- **原版为什么不会这样**（判据，不是自证）：`d:/2/tools/decomp_full/ToastNotificationController__ShowNotificationInternal.c` 第 1 句 `Instantiate(prefab, holder)` 之后调 `Initialize`；`ToastNotification__Initialize.c:36-40` 设的是 **Unity 序列化字段**（`param_1+0x20` 那颗 TMP 的 `text`）—— prefab 里的组件引用是**资产序列化数据**，克隆时被正确重映射。⇒ 我们的等价物（`Label` 运行时封装）必须自己做到「克隆后后端可用」。

- **`SetTextDot` 本身的缺口**：`:1996` 前没有任何 `_mr == null` 检查。红线（不许静默失败）要求这里**不能只是**返回 —— 见 §4。

### 3. 是否本批回归

**不是第四十七会话的代码回归，是「第四十五会话埋下的旧账 + 本批首次跑到」。**

- `Instantiate(_templateByType…)` 与 `Fill` 里的 `SetText` 自 Toast 族创建时就在：`git log -S "Instantiate(_templateByType"` ⇒ **只有 `ed2bbe0`（第四十五会话）**。
- `git show HEAD~1:…/Editor/ShellScene.cs | grep ToastNotifications` ⇒ **0 命中**：第四十六会话的 `ShellScene` **没有**这一节；本批 `ShellScene.cs` **+342 行**新增了 `N20/N21/N23` 这一节（`3990ca6`）。
- 第四十五/四十六会话提交说明都写着「未跑自检」⇒ 这条代码路径**在第四十七会话之前一次都没有被执行过**。
- 结论：**旧账（γ：本批新暴露）**。第四十七会话对 `ToastNotifications` 的改动（N20/N21/N23）只是改高度/队列/删死模板，**与崩溃机制无关**。

### 4. 修法建议

**⛔ 不要只给 `SetTextDot` 加 `_mr != null` 的空转**（那会把「整条 toast 文字不画」变成静默失败）。

**首选（忠于原版「克隆 prefab」这一形状）——让 `Label` 克隆安全：**

1. **克隆后重绑后端**（`Battle/Label.cs` 新增，供 `ShowInternal` 在 `Instantiate` 之后统一调；或做成就地懒绑）：
   ```csharp
   public void RebindAfterClone()
   {
       if (_tmp != null || _mr != null) return;
       // 模板的 TMP 是【子节点 "text"】（TmpFont.NewText 是 new GameObject 挂子件）⇒ 必须 InChildren
       _tmp = GetComponentInChildren<TMP_Text>(true);
       if (_tmp != null) { _ugui = _tmp is TextMeshProUGUI; return; }
       _mr = GetComponent<MeshRenderer>();
       _mf = GetComponent<MeshFilter>();
       if (_mr == null) BuildDot();          // 模板本来就没有渲染器 ⇒ 真去建
   }
   ```
   ⚠️ 懒绑的调用点不能只放 `SetText`：`WorldW/WorldH`（`:120-122`，点阵分支读 `_texW/_texH`）、`SetColor`（`:273`）、`SetRenderQueue`（`:248`）、`ForceRelayout` 都会按后端分叉。
   **最稳**是在 `ToastNotifications.ShowInternal` 的 `Instantiate` 之后走一遍 `GetComponentsInChildren<Label>(true)` 逐个 `RebindAfterClone()`（`ImageQuad` 同批，见下）。

2. **把「版面状态」也做成克隆可携带**（否则 rebind 好了，**画面仍然静默偏**）：
   `Label` 的 `_vTier / _vFace / _vBoxH`（`:1664-1666`）、`_boxHFromAutoFit`（`:1670`）、`_ugui`（`:73`）**全是私有非序列化** ⇒ 克隆体上退回出厂值：
   - `_vTier` 出厂 `Middle` ⇒ 原版 `Toast Collection Level Up` / `Get XP` 的 **`Capline`**（`ToastNotifications.cs:558,672`）与 mission `count` 的 **`Midline`**（`:470`）在克隆体上**静默退回 `Middle`**（`RefreshBounds`/`VOffsetWorldNow` 按 `_vTier` 算位移 ⇒ 字墨位置偏，**不报错**）。
   - `_ugui` 出厂 `false` ⇒ 将来若有 uGUI 档的 `Label` 被克隆，单位会差 108 倍（同 `T09` 那条铁律）。
   ⇒ 给这几个字段加 `[SerializeField]`（`_tmp/_mr/_mf` 也可以直接加：Unity 克隆时会**在克隆层次内部重映射组件引用**，那样连 `RebindAfterClone` 都不必写；但加完仍建议留 rebind 兜底，因既有场景里这些字段是 null）。

3. **下一处会接着崩：`ImageQuad` 也克隆不安全**（必须同批修，否则修完 Label 立刻换一个 NRE）：
   - `Battle/ImageQuad.cs:34-38`：`MeshRenderer _mr; MeshFilter _mf; Texture _tex; float _worldH = 1f; _aspect = 1f;` 全是私有非序列化。
   - `ToastNotifications.ApplyMeasuredHeight`（`Shell/ToastNotifications.cs:1042-1048`）对**克隆体**上的 `Hit` quad 调 `SetWorldHeight()` / `SetAspect()` ⇒ `ImageQuad.cs:328-333` / `:320-326` ⇒ **`RebuildMesh()`** ⇒ `ImageQuad.cs:364` **`_mf.sharedMesh = m;` 裸解引用** ⇒ 又是 NRE。
   - 另外 `Fill` 的 mission 分支 `ToastNotifications.cs:882` `icon.SetTexture(tex,false)` 在克隆体上因 `_mr == null` **静默不生效**（`ImageQuad.cs:194` 有判空、直接跳过）—— 换自定义图标永远画的是模板那张图。
   ⇒ `ImageQuad` 同样要么加 `[SerializeField]`（`_mf/_mr` + 几何状态 `_uv/_worldH/_aspect`），要么在 `SetTexture/RebuildMesh/SetWorldHeight/SetAspect` 里懒解析 `GetComponent<MeshRenderer|MeshFilter>()` 并给 `RebuildMesh` 补判空出声。

**备选（更省事的根治）**：`ShowInternal` **不走 `Instantiate`**，直接以 `_holder` 为父调用现成的 `BuildToast/BuildGetXP/BuildLevelUp/BuildMission`（它们是纯静态建树函数，`Node()` 用 `MenuDraw.Local(parent,…)` 按世界矩形摆，父件换成 `_holder` **世界位置逐位不变**），建完 `SetActive(true)`。
- 优点：一次性绕开 `Label`/`ImageQuad`/`WindowButton` 三个组件的克隆状态丢失，且不改变自检对 `Templates` 的四颗断言。
- 代价：与原版「Instantiate」的字面形状不同（但原版那套之所以成立，靠的是 prefab 序列化引用；我们是代码建树，**行为等价优先于形状**）。每次 Show 多一次建树 + 一顆离屏量字标签，可接受（toast 频率极低）。

**配断言（放 `ShellScene.cs` 的 N20/N21/N23 一节）：**
- 克隆出来那条 toast 的 `Text` 上 `CheckTrue(lb.CanRenderChinese, …)`（`Label.cs:125` = `_tmp != null`）—— 它正是「后端在克隆后还活着」的直读判据；
- `CheckTrue(lb.VAlignTier == Label.VAlign.Middle, …)`（Toast）/ `Capline`（Get XP、Level Up）/ `Midline`（mission count）—— 挡「`_vTier` 丢失、静默退 Middle」；
- 给 mission toast 一个**不同于模板**的 `IconArt`，断言 `icon.Texture.name` 真的换过去了 —— 挡 `ImageQuad.SetTexture` 在克隆体上静默空转；
- 一条**反证型**：`CheckTrue(go.GetComponentsInChildren<Label>(true).All(l => l.CanRenderChinese), …)`（全树无点阵兜底）—— 只对 `Text` 一条断言的话，别处的 Label 仍可能静默退化。

### 5. 没查清

- **`Fill` 的确切崩溃行号**：没留日志，按调用顺序推断是 `:919`（Achievement 分支），**未由栈直接证实**；`Fill` 里还有 `:877 / :889 / :904` 三处 `SetText`，若第一条 `Show` 不是 Achievement 则会落到别处。缺的东西 = 那次 `ShellScene.Run` 的完整 Editor.log / `-logFile` 输出。
- **「加 `[SerializeField]` 后 Unity 克隆是否把 `_tmp` 重映射到克隆体自己的 TMP 子件」**：按 Unity 的 PPtr 重映射规则应当成立，但本诊断**不跑 Unity**，未能现场验证。修的时候请用上面那条 `CanRenderChinese` 断言现场核。
- **`Label` 里到底还有多少私有状态参与版面**：本报告只列了已确认会影响 toast 的 5 个（`_vTier/_vFace/_vBoxH/_boxHFromAutoFit/_ugui`）；`_pendWrapW`/`_defLayoutPushed`/`_texW/_texH` 等的克隆后果没有逐条实测。
- **`ToastNotifications` 目前有没有生产消费者**：全仓 `grep ToastNotifications.Show*` 只命中 `Editor/ShellScene.cs`（`:6996/:6997/:7024/:7026`）⇒ 今天崩溃只影响自检；但正文注释写的生产入口（`MainMenuRuntime.Build`）如果接上，就是**真机必崩**。

---

## 桶 C · `CollectionScene.Run` 崩：`CollectionScene.cs:8247 → WindowButton.Click → PlayClickSound → get_transform`（`WindowButton.cs:883`）

### 1. 复现路径

- **宿主**：`CollectionScene.Run`（批处理，`Application.isPlaying == false` ⇒ `DestroySafe` 走 `DestroyImmediate`）。
- **夹具/触发点**：`Editor/CollectionScene.cs:8234-8249`（Styles 页「换风格箭头」那一节，「只有 1 档、箭钮是死键」）：
  ```csharp
  var rbtn = rhit.GetComponent<WindowButton>();   // :8235
  var lbtn = lhit.GetComponent<WindowButton>();   // :8238
  ...
  rbtn.Click();                                   // :8244
  Check(win.StyleIndex, 0, …);                    // :8245
  Check(win.StyleVisibleCount, 7, …);             // :8246
  lbtn.Click();                                   // :8247  ← 崩
  ```
- 顺序（逐句现读）：
  1. `:8244 rbtn.Click()` → `WindowButton.Click`（`Shell/WindowButton.cs:827-842`）→ `:840 PlayClickSound()`（此时钮还活着，正常）→ `:841 onClick()`；
  2. `onClick` 是 `CollectionWindow.cs:1713` 接的 `() => ChangeStyle(+1)`；
  3. `ChangeStyle`（`CollectionWindow.cs:1854-1861`）：**`s.Length == 0` 才早退**（只有 1 档时**照样往下走**）→ `:1860 RebuildStylesPage()`；
  4. `RebuildStylesPage`（`:1864-1872`）：`for (…) DestroySafe(page.GetChild(i).gameObject)` —— **把 Styles 页整棵子树（含左右两颗箭钮）销毁**，然后 `BuildStylesPage(page)` 重建**新的**两颗；
  5. `DestroySafe` = `Shell/MainMenuSubmenuWindow.cs:166-172`：编辑器非 Play ⇒ **`DestroyImmediate`（同步）**；
  6. 回到测试：`:8247 lbtn.Click()` —— `lbtn` 是**第 4 步之前**抓的旧实例，已销毁；
  7. `Click()` 前两句只读托管字段（`absorbOnly` / `_interactable`）不炸；`:840 PlayClickSound()` → `:883 if (MenuDraw.WasShadeHit(transform)) return;` —— **`transform` getter 在已销毁组件上抛 NRE**（Unity 的 `Component.transform` 走原生对象）。栈 = `get_transform`，与分诊文件一致。

### 2. 根因

**根因一句话：点击右箭 ⇒ `ChangeStyle` 把整个 Styles 页（包括左箭自己）销毁重建；测试随后拿【已销毁的旧引用】去点左箭，`PlayClickSound` 是第一个碰 Unity 原生对象（`transform`）的地方。**

- 触发点（本仓）：`Shell/WindowButton.cs:878-885`
  ```csharp
  void PlayClickSound()
  {
      if (!playClickSound || absorbOnly) return;
      if (MenuDraw.WasShadeHit(transform)) return;   // :883 ← 在已销毁的 WindowButton 上取 transform
      WindowSounds.PlayFx(clickCue ?? WindowSounds.DefaultButtonCue, $"按钮 `{name}`");
  }
  ```
- 自毁链（本仓）：`CollectionWindow.cs:1713`（箭钮挂在 page 下）→ `:1854-1860 ChangeStyle` → `:1868` 整页销毁。
  `ChangeStyle` 里那条注释写「箭钮是死键」，但**代码没有为「1 档」早退**（只有 `s.Length == 0` 才 return）——注释和行为不一致，这正是「死键照样重建整页」的直接成因。

- **原版为什么不会这样**（判据）：`d:/2/tools/decomp_full/AlternateArtCardCollectionTab__ChangeStyleButton.c:28` 只调 `ConfigureStyle`；`AlternateArtCardCollectionTab__ConfigureStyle.c`：
  - `:45` `UnityEngine_UI_Image__set_sprite(styleImage, 新的 style 图)`（只换 logo 那张图）；
  - `:50` `CollectionDisplay__UpdateCollection(...)`（只更新卡列表）；
  - **全程没有任何 `Destroy` / 重建页头或箭钮**（箭钮是 prefab 里的常驻 UGUI 对象）。
  ⇒ 我们「整页销毁重建」是**实现偏离**，并且正是它让「正在被点击的按钮」在点击过程中消失（原版不可能发生）。

### 3. 是否本批回归

**不是第四十七会话的回归 —— 旧账，本批首次跑到。**

- `ChangeStyle` / `RebuildStylesPage` 的正文与第四十六会话（`HEAD~1`）**逐字相同**（`git diff` 里这两段没有 hunk）；`RebuildStylesPage` 自 **`79ce65f`（2026-09-24）** 起就是「整页销毁重建」。
- 崩溃的第一触发句 `PlayClickSound` 里的 `transform` 访问自 **`09ecc15`（第四十四会话 · 音效线新建，2026-10-17）** 起就在；`ed2bbe0` 只把 cue 形参换成 `clickCue ?? Default`，**没有改变 `transform` 这一句**（`git log -S "PlayClickSound"` 只命中 `09ecc15`）。
- 这套「点右箭再点左箭」的断言自 `HEAD~1` 之前就在（旧文件 `:8137-8151`，本批只是因上面插入了别的内容整体后移到 `:8234-8249`）。
- 本批 `Shell/CollectionWindow.cs` 的改动是 `Header` 容器、`Deck Scroll View` 层级、滚动灵敏度（`A1642`/`D_N29`）——**都不在这条链上**。
- 为什么以前没崩：第四十四/四十五/四十六会话**都没跑过自检**（提交说明逐条写着「未跑自检」；最近一次 `CollectionScene.Run` 的完整读数在 `资料/普查产出_1017/D2_卡组线诊断.md`，对应 `2026-10-07` 那次，**早于音效线**）⇒ 崩溃点自 10-17 音效线落地后**第一次被执行**。

### 4. 修法建议

**两笔账分开修（一笔是测试/夹具的过期引用，一笔是实现与原版的偏离），⛔ 不要只加 null 守卫了事。**

**① 夹具侧（让自检不再拿已销毁的引用）—— 最小改动**
在 `Editor/CollectionScene.cs:8244-8248`，**每次点击之后重新取钮**（因为这次点击本来就可能重建页面）：
```csharp
rbtn.Click();
Check(win.StyleIndex, 0, …);
Check(win.StyleVisibleCount, 7, …);
lhit = FindChild(spage, "ArrowHit Left");               // ← 重取（lhit 之前也是页面重建前抓的）
lbtn = lhit != null ? lhit.GetComponent<WindowButton>() : null;
CheckTrue(lbtn != null, "（重建后）左箭钮仍然在");
lbtn.Click();
Check(win.StyleIndex, 0, …);
```
⚠️ `rbtn` 在 `rbtn.Click()` 之后同样已销毁：如果后面还要用它，也要重取（本段目前没用）。同族风险：`styleFltBtn`（`:7720` 抓、`:8222` 用）、`cFltBtn`（`:7915` 抓、`:8218` 用）都是「早抓晚用」，一旦中间任何一次点击触发整页重建，就会以**完全相同的方式**崩；改成「用前重取」或「点击后重取」是同一套纪律。

**② 实现侧（照原版 `ConfigureStyle`）—— 真正的复刻**
把 `CollectionWindow.ChangeStyle` 从「整页重建」改成「只刷新风格相关的那两件」：
- 抽一个 `RefreshStyleLogo()`：按新 `StyleIndex` 重取 `StyleLogo(CurrentStyleName())` 的图，`ImageQuad.SetTexture(...)`（或按现有 `BuildStylesPage` 的兜底逻辑重建那一格文字）；
- `var holder = PageRoot(3)?.Find("Scroll View"); if (holder != null) RebuildStyleCells(holder);` + `RefreshStyleEmpty()`。
- **⛔ 不再 `DestroySafe` 页头的 Four `Filter Toggle` / 箭钮 / 分隔线**。
- 判据出处（原版逐句）：`d:/2/tools/decomp_full/AlternateArtCardCollectionTab__ConfigureStyle.c:45`（`Image.set_sprite`）· `:50`（`CollectionDisplay.UpdateCollection`）；`AlternateArtCardCollectionTab__ChangeStyleButton.c:28`（只调 `ConfigureStyle`）。
- 附带收益：修掉「切风格把滚动位置、页头状态、筛选栏全部重建」这一串状态丢失（原版不重建 ⇒ 原版不丢）。

**③（可选，防御性，⛔ 不能替代 ①②）**
`WindowButton.Click()` 第一句加 `if (this == null) { Debug.LogWarning("[Button] 已销毁的钮仍被 Click() —— 调用方持了过期引用"); return; }`（`Object.op_Equality` 对已销毁对象安全，不会碰 `transform`）。
- 这是**出声的**守卫，符合「不许静默失败」；但它会把「调用方拿过期引用」变成一条无害日志 ⇒ **只当保险，别当修法**。
- 不建议在 `PlayClickSound` 里 `transform == null` 判空 —— 症状一样会被盖掉，而根因是「整页重建」这件事本身。

**配断言（放 `CollectionScene.cs` 的 Styles 段）：**
- **对象身份断言**：点一次箭钮前后，`spage.Find("ArrowHit Left")` 的**实例**应当**不变**（若采用修法 ②）—— 它正面咬住「整页重建」这个病；⛔ 不要只断「箭头点击后下标仍是 0」（那条在崩溃前后都能过，正是它放过了这个 bug）；
- **状态保留断言**：先把 `StyleScroll` 滚到非 0 偏移（或把抽屉开着），点一次箭钮，断言偏移/开合态**没被重置** —— 这也是原版 `ConfigureStyle` 的行为（只换图 + 更新卡列表）；
- **存活断言**：`lbtn` 点击后 `win.StyleVisibleCount` / `StyleIndex` 仍对，且 `page` 下两颗箭钮各恰好一颗（挡「重建出两份」）。

### 5. 没查清

- **崩溃瞬间的完整栈**（`Click` 里是 `:840 PlayClickSound` 还是更早）没有日志佐证；分诊文件已给到 `PlayClickSound → get_transform`，本报告按源码复现这条链，**未跑 Unity 现场验证**。
- **`DestroyImmediate` 与 `Transform.GetChild` 的迭代顺序**：`RebuildStylesPage` 是倒序删，删除过程中父件的 `childCount` 会变；代码是 `for (int i = page.childCount - 1; i >= 0; i--)`，每轮重读 `childCount` ⇒ 逻辑上安全，但**没有实测**这条循环是否恰好把箭钮也删掉（从 `BuildStyleArrow` 的 `Rect(page, …)`（`:1833`）看箭钮是 page 的直接子件，应当会）。缺的东西 = 一次带断点的运行或一条「重建后旧 `lbtn == null`」的日志。
- **`ChangeStyle` 改小重建范围会不会影响其它断言**（例如有没有断言依赖「切风格后页头被重建」）：没有逐条扫；修 ② 时需要跑 `CollectionScene.Run` 复核（本轮不跑自检，留给修复方收口）。

---

## 顺手发现（其它缺陷 / 疑点）

1. **`Label` 的克隆不安全是通用地雷，不止 Toast**：任何 `Instantiate` 一棵含 `Label` 的树都会得到 `_tmp == null && _mr == null` ⇒ 只要有人对它 `SetText` 就 NRE；若恰好只读 `WorldW/WorldH`，则读的是 `_texW/_texH` 默认值（`Label.cs:120-122`），**静默给 1/100 世界单位的假宽度**。全仓目前只有 `ToastNotifications.cs:814` 一处 `Instantiate` 命中（其它 `Instantiate` 都是战斗 FX/原版 prefab），但这是一条应当写进 `资料/已知的坑.md` 的判据：「运行时建树组件（`Label` 现在、`ImageQuad` 也是）不是 prefab，克隆会丢运行时状态」。
2. **`ImageQuad` 的克隆静默失败面比 `Label` 更宽**：`SetTexture`（`:189-194`）/`SetRenderQueue`（`:273-283`）/`SetTint`（`:308`）在 `_mr == null` 时**全是静默跳过**；`RebuildMesh`（`:364`）则是裸 NRE。`_uv/_worldH/_aspect` 也不跟着克隆（`SetUvRect` 设过的镜像/九宫格 uv 会在任何一次 `RebuildMesh` 后丢）。本轮 Toast 的 `ApplyMeasuredHeight` 一定会走到 `RebuildMesh` ⇒ **修 `Label` 之后的下一个 NRE 就是它**，别把它当成「修完又冒出一个新问题」。
3. **`Label.SetText` 的早退 `if (text == _text) return;`（`:254`）在克隆体上失效**：`_text` 也是私有非序列化字段 ⇒ 克隆体上恒为 null ⇒ 每次 `SetText` 都会真重排一次。不是 bug，但说明「克隆体会做比模板更多的工作」，别在性能敏感路径上依赖它。
4. **`ToastNotifications` 的 `Fill` 里 `FindQuad(...).SetTexture(...)` 静默不生效**（同上第 2 条）：mission toast 的 `RewardArt` 换图在克隆体上**永远画的是模板图**，而 `Fill` 的注释/自检把它当「已接上」。这条即使修好 Label 也仍然存在，建议与 `ImageQuad` 的 rebind 同批修。
5. **`CollectionWindow.ChangeStyle` 的注释与行为不符**：`CollectionWindow.cs:1827-1833` 与自检注释都写「原版只有 1 档 ⇒ 两颗箭钮**点了也不动**」，但 `ChangeStyle` 里只有 `s.Length == 0` 的早退（`:1857`），1 档时照样走完 `StyleIndex` 取模 + `RebuildStylesPage()`。**建议加 `if (s.Length <= 1) return;` 之前先对一眼原版**：`ChangeStyleButton` 在 1 档时会调 `ConfigureStyle`（重设同一张图 + 更新列表），所以「完全不动」也不完全等于原版；正确做法是 ②（只刷新、不重建），而不是简单早退。
6. **`Editor/CollectionScene.cs` 里多处「早抓晚用」的按钮引用**：`styleFltBtn`（`:7720`→`:8222`）、`cFltBtn`（`:7915`→`:8218`）与本次崩溃完全同形；其中 `cFltBtn` 在 `:8027/:8047/:8113/:8119/:8166` 被反复使用，中间夹着抽屉开合/页面重建类操作。今天它们没崩只因为中间没有触发 `RebuildStylesPage`，一旦 ② 之外的任何改动再引入整页重建，会立刻复现同一个 NRE。
7. **`ToastNotifications.Build` 的「显式重建」与 `IsAlive` 判据**（`ToastNotifications.cs:316-338`）本身没问题，但 `ToastNotifications.DisposeForTest()`（`:1157-1163`）在编辑器下 `DestroyImmediate` 后 `Instance=null`；本批自检把它放在节尾，若中途崩了（本桶 B 正是如此）⇒ `Instance` 会留着一个**已销毁但非 null 的静态引用**（`OnDestroy` 在 `DestroyImmediate` 时会跑，理论上会清；编辑器下 `DestroyImmediate` 会触发 `OnDestroy`，所以这条**大概率不成立**，列在这里供复核）。
8. **`ShellScene.cs` 的 Toast 自检把「模板常量兜底」当正常路径的风险**：`ToastRootHeight` 量不出时退回 `fallback`（`ToastNotifications.cs:983-1000`）并**只在 Warning 里出声**；自检的 `h1 > h0 + 1px` 断言在「两条都退回兜底」时会是 **108.86 vs 108.86 ⇒ 红**，所以这条大概率能抓住；但「`h0` 恰好等于 `12+89.8638+textH+7`」那条在兜底时也会红 —— 结论：现有断言能兜住，不必加新断言，仅记录。

---

## 附：本次诊断用到的关键行号（便于修复时复核）

| 位置 | 内容 |
|---|---|
| `Shell/ToastNotifications.cs:814` | `Instantiate(_templateByType[ti], _holder, false)` |
| `Shell/ToastNotifications.cs:818` / `:919` / `:877` | `Fill(...)` / Achievement 分支 `lb.SetText` / Mission 分支 `lb.SetText` |
| `Shell/ToastNotifications.cs:1042-1048` | `ApplyMeasuredHeight` 对克隆体上的 `ImageQuad` 调 `SetWorldHeight/SetAspect` |
| `Shell/ToastNotifications.cs:558 / :672 / :470` | 克隆后会被丢掉的 `Capline`（LevelUp/GetXP）/ `Midline`（mission count） |
| `Battle/Label.cs:51-52 / :62 / :73 / :1664-1670` | 私有非序列化字段（`_mr/_mf/_tmp/_ugui/_vTier/_vFace/_vBoxH/_boxHFromAutoFit`） |
| `Battle/Label.cs:185-197 / :251-261 / :1980-2002 / :1996` | 后端建立 / `SetText` 分派 / `SetTextDot` / 崩溃行 |
| `Battle/ImageQuad.cs:34-38 / :189-194 / :328-333 / :364` | 私有非序列化字段 / `SetTexture` 静默 / `SetWorldHeight` / `RebuildMesh` 裸解引用 `_mf` |
| `Shell/WindowButton.cs:827-842 / :878-885 / :883` | `Click` / `PlayClickSound` / `transform` 崩溃行 |
| `Shell/CollectionWindow.cs:1712-1713` | 两颗箭钮建在 `page` 下 |
| `Shell/CollectionWindow.cs:1854-1861 / :1864-1872 / :1868` | `ChangeStyle`（无 1 档早退）/ `RebuildStylesPage` / 整页销毁循环 |
| `Shell/MainMenuSubmenuWindow.cs:166-172` | `DestroySafe` = 编辑器非 Play ⇒ `DestroyImmediate` |
| `Editor/CollectionScene.cs:8234-8249 / :8244 / :8247` | 箭钮段 / 右箭点击 / 左箭点击（崩溃调用点） |
| `Editor/ShellScene.cs:6996` | 第一条 `ToastNotifications.Show(Achievement, "Hi")` |
| `d:/2/tools/decomp_full/AlternateArtCardCollectionTab__ChangeStyleButton.c:28` | 原版只调 `ConfigureStyle` |
| `d:/2/tools/decomp_full/AlternateArtCardCollectionTab__ConfigureStyle.c:45,50` | 原版只换 logo 图 + 更新卡列表（无销毁） |
| `d:/2/tools/decomp_full/ToastNotification__Initialize.c:36-40` | 原版写的是**序列化**组件字段的 `text`（克隆安全） |
| `d:/2/tools/decomp_full/ToastNotificationController__ShowNotificationInternal.c` | 原版第 1 句 `Instantiate(prefab, holder)` |
