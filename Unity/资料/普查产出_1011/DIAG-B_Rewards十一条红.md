# DIAG-B · RewardsScene 11 条红诊断（只读 · 2026-10-11）

> 只读诊断。⛔ 没跑 Unity、没动 git、没改任何文件（本文件是唯一写出的东西）。
> 现场 = `d:/4/_tmp_view/rewards.log`（`[Rewards] === 合计：1271 通过 / 11 失败 ===`，日志第 22150 行）。
> ⚠️ 日志里 `✗` 一共出现 **22** 次 —— 其中**后 11 次（:22161-:22271）是失败回顾**，
> 与前面 11 条**逐字相同**。真正的红 = **:17191 · :17216 · :17409 · :18641 · :18654 · :18704 · :18717 · :18743 · :19083 · :19111 · :21856**。
> ⚠️ 日志行号与 `Editor/RewardsScene.cs` **现读行号一一对得上**（日志栈帧里的 `RewardsScene.cs:5019/6227/…`
> 与现读文件逐条吻合）⇒ **跑的就是当前这份源码**。

---

## 一、结论（11 条各归哪一档）

| # | 日志行 | 源码行（`Editor/RewardsScene.cs`） | 判定 | 一句话 |
|---|---|---|---|---|
| 1 | 17191 | `:5032` `CheckTrue(rw2 != null, …)` | **δ**（夹具前提不成立）· 由 **W1/A309** 带出 | 夹具自己造 `WindowsManager` 却不登记 `Instance`，`RewardWindow.Show()` 里那句 `EnsureHost()` **另建了一台**管理器 + 一整套锚点，窗户落在那一台上 ⇒ `wm2.openWindows` 里当然没有它 |
| 2 | 17216 | `:5061` | **γ**（级联自 #1） | `rw2 == null` ⇒ `:5052` 的 `rw2.Close()` 没跑 ⇒ `onClose → RefreshOpenDailyWindows` 没发 ⇒ 日常窗没重建 ⇒ `colider` 还开着 |
| 3 | 17409 | `:5147` `punchFlips >= 1` | **α**（断言写错） | `DOTween.Punch` 的段数 = `(int)(vibrato × duration)` = `(int)(5 × 0.4)` = **2** ⇒ **单峰、构造性不穿零**。原版同一套参数、同一个算法 ⇒ 原版也是 0 次 |
| 4 | 18641 | `:5479` | **γ**（级联自 #1） | §九 遗留的那扇 `Reward Window` 还开着，`HoverAt(413,430)` 打在它的 `AbsorbHit`（队列 3131）上 |
| 5 | 18654 | `:5485` | **γ**（同上） | 悬停根本没落到条目上 ⇒ 底图当然没换 |
| 6 | 18704 | `:5541` | **γ**（同上） | 同上（软边那条条目的探针） |
| 7 | 18717 | `:5543` | **γ**（同上） | 同上 |
| 8 | 18743 | `:5558` | **γ**（同上） | 同上 |
| 9 | 19083 | `:295-302`（由 `:5620` 调用） | **γ**（级联自 #1） | `(5,5)` 命中的是**别家的窗**：遗留 `RewardWindow` 的 `BackgroundHit`（队列 **3130**）压过收件箱自己的压暗层（队列 **3002**） |
| 10 | 19111 | `:303-304` | **γ**（级联自 #1） | `pl.ClickAt(5,5)` 关掉的是**那扇遗留窗**（它的 `ShadeHit` 的 onClick = `Close()`），收件箱保持 `Open` |
| 11 | 21856 | `:6227` `LineCount >= 2` | **α**（断言无判别力） | `SetActive(true)` 那一下 `TextMeshPro.Awake()` 会 `m_textInfo = new TMP_TextInfo(this)` —— **把 `lineCount` 清零**。而 `LineCount` 读的就是它 ⇒ **无论 A266 那一跳做没做，这里恒为 0** |

**一句话总账：11 条红 = 1 个根因（#1，一棵新管理器）+ 1 条写错的断言（#3）+ 1 条无判别力的断言（#11）；其余 8 条全是 #1 的级联。**

---

## 二、逐条

### #1 · `:5032` `★★ 点领奖之后弹出原版那扇 Reward Window 了` —— **δ**（夹具前提）

**日志证据链（三段，全部在 `rewards.log` 里）**

1. `:17094` `[Win] 场景里没有 WindowsManager ⇒ 现建了一台 + 三个锚点（单独打开界面场景时走这条路）`
   —— 这是 `Shell/WindowsManager.cs:618` 那句 `Debug.Log`。
2. 同一段前后还有一整套**新建锚点**的痕迹：`:16944/:16994/:17044` 三条
   `[Win] WindowHolder 挂在 … 但 placement = None`（= `AddComponent` 时 `OnEnable` 先跑，`placement` 还没赋值），
   紧接着 `:16971/:17021/:17071` 三条 `[Win] 锚点 X 被两个活着的节点抢`（= **新锚点顶掉了旧锚点**，旧的那套还活着）。
3. 栈帧（`:17094` 下一行 `:17101`）指向 `WindowsManager:EnsureHost (…WindowsManager.cs:618)`。

**为什么 `Instance` 是 null** —— 夹具**自己**造管理器，没走 `EnsureHost`：

- `Editor/RewardsScene.cs:704-710`（`Build()`）：
  `MakeHolder(anchors, "1 - Below Upper Bar Holder", World)` 三句 + `var wmGo = new GameObject("WindowsManager"); var wm = wmGo.AddComponent<WindowsManager>();`
- `Shell/WindowsManager.cs:565` `public static WindowsManager Instance { get; private set; };`
  `:577-578` **只在 `Awake()` 里赋**（`if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this;`）。
- 🔴 `WindowsManager` **没有 `[ExecuteAlways]`**（对比同文件 `:64` 的 `WindowHolder` **有**）
  ⇒ **批处理（编辑模式）下 `AddComponent` 不跑 `Awake`** ⇒ `Instance` 全程 null。
  （这正是 `WindowsManager.cs:581-583` 那段注释写的事情 —— 当年为 `PointerLayer` 挪过，**`Instance` 没一起挪**。）

**为什么窗户落在别处**：A309 新接的出口走 `EnsureHost()`：

- `Shell/DailyData.cs:541` `ShowCollectedWindow(art, n, premium)`
  → `:854-859` `RewardWindow.ShowCollected(…, RefreshOpenDailyWindows)`
  → `Shell/RewardWindow.cs:446` **`var wm = WindowsManager.EnsureHost();`**
  → `:469` `wm.OpenWindow(win, ctx);`
- `EnsureHost` 在 `Instance == null` 时**不查「场景里是不是已经有一台」**，直接再建一台（`:601-620`）。
  ⇒ 新窗户进了**第二台**的 `openWindows`，`wm2`（= `win.Manager` = 第一台，`Editor/RewardsScene.cs:826`）里没有。

**闭环验证（这一步是关键，说明窗户真的开了、只是不在 `wm2` 上）**：
`d:/4/_tmp_view/rewards/05_收件箱_空态.png` **拍到的不是收件箱** —— 整屏是一扇
**"Rewards claimed" + "Get Reward" + "Click to continue"** 的 `Reward Window`，收件箱面板只在后面透出来。
它出现在 `:18151`（§六 中途），**比 #9/#10 还早** ⇒ 那扇窗从 §九一直活到 §六。见 §六·1。

**最小改法**（推荐，1 处）：`Editor/RewardsScene.cs:703-710` 改成
```csharp
var wm = WindowsManager.EnsureHost();      // 它自己建 "Window Anchors" + 三颗 Holder + 管理器，并登记 Instance
```
（把 `:704-707` 那三句 `MakeHolder` 删掉 —— `EnsureHost` 建的就是同名同 placement 的三颗；
`RewardsScene.Build` 上面的注释本来就写着「照 `ShellRuntime.Build` 里那三个」）。
⚠️ 这不是「为了自检而改自检」：`Shell/WindowsManager.cs:592-593` 白纸黑字写着
「**壳（`ShellRuntime`）与「单独打开某个界面场景按 Play」都走它 —— 两处各建一次 = 迟早不一致**」，
`RewardsScene.Build` 是**第三份**手抄（同形还有 3 处，见 §六·3）。

**为什么同时记一条设计缺陷（次要，别丢掉）**：`Shell/RewardWindow.cs:446` 用 `EnsureHost()`
= 「没有就**另建一台**」，而不是「复用已经在管这些窗的那一台」。生产侧不触发（`ShellRuntime.cs:216` 先建），
所以本条的**主判是 δ**；但**任何新的 `EnsureHost()` 开窗路径都会在同形的 4 个夹具里再犯一次**。

---

### #2 · `:5061` `…而且日常窗真被重建过：colider 整颗关掉` —— **γ**（级联自 #1）

- **不是另一件事**：`Editor/RewardsScene.cs:5054-5065` 整块在 `if (cHit != null)` 里，
  而重建那一跳只有一个来源 —— `:5052` 的 `rw2.Close()`（在 `if (rw2 != null)` 里）
  → `Shell/RewardWindow.cs:508-513` `Close()` 先发 `OnClose(Rewards)`
  → `Shell/DailyData.cs:866-879` `RefreshOpenDailyWindows` → `dw.Build()`。
  `rw2 == null` ⇒ 这一跳**根本没发生**。
- **为什么 `:5057` 的「进了 `Collected`」还绿**：`DailyData.cs:536` 在 `ShowCollectedWindow` **之前**
  就写了 `_rewardClaimed[RI(day)] = true`，而 `RewardStateOf`（`:500-509`）读的就是它
  ⇒ **状态和「窗有没有重建」是两件事**，前者早就绿了。
- **补一个坑**：就算把 #1 修好、`rw2` 找得到，**`RefreshOpenDailyWindows` 用的是 `WindowsManager.Instance`**
  （`DailyData.cs:868`）—— 如果 `Instance` 不等于 `wm2`，它遍历的是**第二台**的窗口表，
  日常窗（在第一台上）**照样不会重建**。⇒ #1 的修法必须是「让 `Instance` 就是 `wm2` 那一台」，⛔ 不是「换个地方找窗户」。
- **最小改法**：随 #1 一起解决，**不需要单独改这一条**。

---

### #3 · `:5147` `★ …而且来回穿零（实测 0 次）` —— **α**（断言写错）

**先把 DOTween 的 `Punch` 读出来（这一步是硬判据，别跳）**

我们的实现：`Shell/RewardWindow.cs:582-583`
```csharp
var tw = DOTween.Punch(() => _punchScale[k], v => _punchScale[k] = v,
                       Vector3.one * PunchAmount, PunchTime, PunchVibrato, PunchElasticity);
```
常量 `:273-276`：`PunchTime = 0.4f` · `PunchVibrato = 5` · `PunchElasticity = 0.2f` · `PunchAmount = 0.1f`。
（这四个字面量确实是原版读出来的 —— `RewardWindow__DoRewardAnimation.c:110-114`。）

`DOTween.Punch` 的**方法体**（IL 逐条读出；`Assets/Plugins/Demigiant/DOTween/DOTween.dll`，
`DG.Tweening.DOTween::Punch`，150 条指令，`Mono.Cecil` 现场读的）：

```
/*IL 0x00-0x21 */  elasticity = (elasticity > 1) ? 1 : (elasticity < 0 ? 0 : elasticity);
/*IL 0x22-0x30 */  float mag   = direction.magnitude;
/*IL 0x2a-0x30 */  int   count = (int)((float)vibrato * duration);      // ← ldarg.s vibrato → conv.r4 → ldarg.3(=duration) → mul → conv.i4
/*IL 0x31-0x36 */  if (count < 2) count = 2;
/*IL 0x37-0x59 */  float[] dur = new float[count];
                   total = Σ_{i<count} duration * (i+1)/count;
                   k = duration / total;  for(…) dur[i] *= k;            // 段时长合计 = duration
/*IL 0x98-0x109*/  Vector3[] end = new Vector3[count];
                   for (i…) {
                       if (i == count-1)     end[i] = Vector3.zero;
                       else if (i == 0)      end[i] = direction;
                       else if (i % 2 == 0)  end[i] =  ClampMagnitude(direction, mag);
                       else                  end[i] = -ClampMagnitude(direction, mag * elasticity);
                       mag -= direction.magnitude / count;
                   }
                   return DOTween.ToArray(getter, setter, end, dur).NoFrom().SetSpecialStartupMode(…);
```

**代进我们的数**（`direction = (0.1,0.1,0.1)`，`duration = 0.4`，`vibrato = 5`）：

```
count = (int)(5 × 0.4) = 2          （≥2 钳位不动它）
dur   = [0.4×1/2, 0.4×2/2] 归一化 ⇒ [0.13333, 0.26667]
end   = [ (0.1,0.1,0.1),  Vector3.zero ]      ← 只有两个关键帧，i=1 直接走 `i == count-1` 那一支
⇒ 轨迹 = 1.0 ──0.133s──▶ **1.1** ──0.267s──▶ 1.0     （单峰，**构造性不穿零**）
```
（Python 复算见 §七。**`elasticity = 0.2` 在这一档里一次都没被读到** —— `i % 2 != 0` 那条分支只在 `count ≥ 3` 时才可能走到。）

**原版那边是不是同一套算法？—— 是，我把它从二进制里反汇编出来了**

- 原文：`D:/2/unity_run_ref/GameAssembly.dll`，`DG.Tweening.DOTween$$Punch`
  （RVA `11334416`，出处 `d:/2/tools/all_methods.txt:266128`；⛔ `D:/2/Warpforge_code/Assemblies/DOTween.dll`
  与 `il2cpp_out/DummyDll/DOTween.dll` **都是空壳**（`ldnull; ret`），别拿它们当判据）。
- 反汇编（capstone，读法同 `:266588` 的 `ShortcutExtensions$$DOPunchScale` 一族）逐句与上面 IL 对位：
  - `0x84-0xa5` clamp01(elasticity)  —— 同 IL `0x00-0x21`
  - `0xc5` `call Vector3::get_magnitude` —— 同 `0x2a`
  - `0xe2-0xe9` `cvtdq2ps xmm1, [rsp+0x140]`（= **int** 形参 vibrato）`; mulss xmm1, xmm7`（xmm7 = `xmm3` = **float 形参 duration**）`; cvttss2si ebp, xmm1` —— 同 `count = (int)(vibrato * duration)`
  - `0xed-0xfb` `cmp ebp, 2 / jge … else mov ebp, 2` —— 同「钳到 ≥2」
  - `0x130-0x165` `(i+1)/count * duration` 累加 +  `0x169` `divss xmm7, xmm6`（`duration / total`）—— 同段时长那两段
  - `0x198-0x1b3` `new Vector3[count]` + `cmp edi, r15d(=count-1) / jl` —— 同 end 数组那个「最后一格走 zero」的判据
  - ⚠️ **形参序的判据**（为什么 xmm7 一定是 `duration` 而不是 `elasticity`）：x64 是**按位置**配寄存器
    （arg0→rcx · arg1→rdx · arg2→r8 · **arg3→xmm3**），而 `[rsp+0x140]` 用 `cvtdq2ps`（**整数**→float ⇒ vibrato，第 4 个形参）、
    `[rsp+0x148]` 用 `movss`（**浮点** ⇒ elasticity，第 5 个形参）；且 xmm7 既当 `(i+1)/count` 的乘数、又当 `duration/total` 的分子
    —— 只有 `duration` 说得通。形参名/序另用 Cecil 现读核对过：`getter, setter, direction, duration, vibrato, elasticity`。
- ⇒ **「原版的 `DOPunchScale(0.1·one, 0.4, 5, 0.2)` 也是单峰、也穿零 0 次」**。
  我们**调的就是同一个 API、同一组实参**（差别只在 tween 打在数组上而不是 `Transform` 上，曲线本身一字不差）
  ⇒ **`punchFlips == 0` 不是缺陷，是原版行为。**

**断言错在哪**：它把「punch 会来回弹」当成前提（`punchFlips >= 1`），而 `count` 会退化成 2。
作者自己给的那条「改坏法」也不成立 —— 「单调地放一下再收回来那种实现是 **0** 次」正好就是**正确实现**的样子。
⇒ 详见 §三·#3 给的替代判据。

---

### #4 / #5 · `:5479` `:5485`（§六 条目悬停两条）—— **γ**（级联自 #1）

**同一条因果链**：§九 遗留的 `Reward Window` 一直开着，而它的**内容吸收层**在更高的队列上：

- 探针点 = 条目命中区的中心 `((99.57+728.28)/2, (359.68+500.38)/2) = (413.9, 430.0)`（源码 `:5477`）。
- 那一点落在遗留 `Reward Window` 的 `Content` 里（`Shell/RewardWindow.cs:654` 的 `Shade` 是全屏大矩形
  `(-1327.30,-746.18)-(3247.30,1826.18)`，`:662` 的 `AbsorbHit` 盖在 `Content` 上）
  ⇒ **日志里 `实得 = AbsorbHit（窗口态 Open · 本行命中区档/队列 = 3014）`** —— 与 `RewardWindow.QShade(3130) / QCollectBg(3131)`（`:185`）逐位吻合。
- `PointerLayer` **按 `RenderQueue` 取最高**（`Shell/PointerLayer.cs:1067`
  `if (q.RenderQueue > bestQ || (q.RenderQueue == bestQ && z < bestZ))`）⇒ 收件箱条目的 3014 输给 3131。
- ⇒ `hb != rowWb`（#4 红）+ 悬停压根没落到条目上 ⇒ 底图没换（#5 红）。**两条都是 #1 的级联，不是 §六 自己的问题。**

---

### #6 / #7 / #8 · `:5541` `:5543` `:5558`（软边子块那三条）—— **γ**（级联自 #1）

同上：`:5540` 的 `HoverAt` 也被遗留窗吃掉 ⇒ `hbS != wbS`（#6 红）；
`:5543` 主格贴图没换（#7 红）；`:5558` 每个 `_soft*` 子块也没换（#8 红，日志列出
`「Background_soft00」=40K_settings_button` ⇒ **停在常态图**，正是「悬停没发生」的样子）。
`_soft` 那条的**前提**（`:5557` `nSub1 >= 1`）是**绿的** ⇒ 切分本身没问题。

---

### #9 / #10 · `:295-302` `:303-304`（收件箱窗的「点窗外关窗」两条）—— **γ**（级联自 #1）

- 调用点：`Editor/RewardsScene.cs:5620-5621` `CheckAbsorbRule("收件箱窗", inbox.transform, "AbsorbHit", …)`
  → 助手 `:295` 那条断言 + `:304` 那条对照。
- **队列对照（判据）**：
  - 遗留 `Reward Window` 的压暗层命中区 = `Shell/RewardWindow.cs:654` `"BackgroundHit"`，队列
    `QShade = 3130`（`:185`），`Shade` 是全屏（`:119`）。
  - 收件箱自己的压暗层 = `Shell/InboxWindow.cs:209` 同名的 `"BackgroundHit"`，队列
    `QShade = 3002`（`:139`）。
  ⇒ `(5,5)` 上 **3130 > 3002** ⇒ `pl.ButtonAt(5,5)` 必须是遗留窗那一颗
  ⇒ 日志 `实得 BackgroundHit = **别家的窗**`（`:300` 那条分支）**逐字吻合**。
- #10 是 #9 的直接后果：`pl.ClickAt(5,5)`（`:303`）打到的是**遗留窗的 `ShadeHit`**，
  它的 onClick 是 `() => Close()`（`RewardWindow.cs:654`）⇒ **关掉的是那扇遗留窗**，收件箱保持 `Open`。
  旁证：**再往后的 A23「重摇任务窗」那两条（`:20963` / `:21007`）是绿的** ——
  正因为这一下把遗留窗关掉了（`:5620` 在 A23 的 `wm2.CloseAllWindows()` 之前）。

---

### #11 · `:6227` `★ 激活之后补做：那一刀真的补上了…（实得 0 行）` —— **α**（断言无判别力）

**夹具在做什么**（`Editor/RewardsScene.cs:6195-6236`）：
`a266Root` 先 `SetActive(false)`（`:6203`，在建标签**之前**）→ `win.TextBox(...)` 建标签（`:6207`）
→ 断「未激活时 `LineCount == 0`」✅ → `ClipText` 不产生 `TextClipUploadSkipped` ✅
→ `a266Root.SetActive(true)`（`:6226`）→ 断 `LineCount >= 2`（**✗ 实得 0**）
→ 断 `WorldW ≈ 框宽`（**✅ 820px**）· 断 `WrappingMode == 1`（✅）。

**为什么恒为 0 —— 三步，全部有出处**

1. `LineCount` = `_tmp.textInfo.lineCount`（`Battle/Label.cs:419-422`）。
2. `TextMeshPro` 在**子物体**上：`Core/TmpFont.cs:151-154` = `new GameObject("text"); SetParent(parent,false); AddComponent<TextMeshPro>()`
   （⚠️ `SetParent` 在 `AddComponent` **之前** ⇒ 建的时候不跑 `Awake`；`Label.Create`（`Battle/Label.cs:63-67`）同理）。
3. ⇒ `a266Root.SetActive(true)` 是 `TextMeshPro.Awake()` 的**第一次**执行，而它里面：
   ```csharp
   // Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TextMeshPro.cs:582-593
   if (m_mesh == null) {
       m_mesh = new Mesh(); … m_meshFilter.sharedMesh = m_mesh;
       m_textInfo = new TMP_TextInfo(this);      // 🔴 lineCount 归零
   }
   ```
   `m_mesh` 在这一次之前**一定是 null**（只有 `Awake` 建它；`GenerateLayout` 走 `GetTextInfo` 时
   `m_renderMode = DontRender` ⇒ Phase III 被跳过 ⇒ 不会顺手建 mesh）。
   ⇒ **只要版面在激活前（或激活那一拍、TMP 的 `Awake` 之前）生成过，`lineCount` 就必然被这一次 `Awake` 抹掉。**

**⇒ 断言没有判别力**（这才是判 α 的理由，⛔ 不依赖任何「Unity 先调谁」的猜测）：

| 世界的状态 | `:6227` 读到 | `:6231` 的 `WorldW` |
|---|---|---|
| A266 那一跳**做对了**（`Label.OnEnable` 兑现 / 或更早被 `WorldW` 兑现） | **0**（被 `TextMeshPro.Awake` 抹了） | 820 ✅ |
| A266 那一跳**被删掉**（作者自己写的「改坏法」） | **0**（那时还没生成） | 820 ✅（`:6231` 的 `WorldW` 自己会 `EnsureMeasured`，见 `Battle/Label.cs:53`） |

**两种世界读数完全相同** ⇒ 这条断言（**以及紧跟的 `:6231-6234` 那条 `WorldW`**）
既不能证明 A266 那一跳存在，也不能在它被删掉时报红。
⇒ 详见 §三·#11 给的替代判据。

**我判不了的那一点点（如实写）**：`Label.OnEnable` 与 `TextMeshPro.Awake` **谁先跑**（父物体 vs 子物体），
我没有实测判据（`Label.OnEnable` 有没有真被调用，从这份日志里读不出来）。
但**上表说明这不影响本条的判定** —— 两种情况都读 0。真要知道的话，最小实验：
在 `Battle/Label.cs:268` 的 `OnEnable` 里加一句 `Debug.Log`，跑一次看它出不出。

---

## 三、若是 (α)：逐条给【可执行的正确判据文字】

### #3 —— 把 `Editor/RewardsScene.cs:5147-5148` 整条换掉

**（a）先删掉那条错的，替换成「不穿零 + 两段端点」**（端点值全部由原版字面量手算，⛔ 不读我们的常量）：

```csharp
// 🔴 2026-10-11（DIAG-B）就地订正（铁律 5）：原句是 `CheckTrue(punchFlips >= 1, "…来回穿零…")` ——
//   **那条判据与 DOTween 的算法矛盾**（不是与本实现矛盾）：
//   `DOTween.Punch` 的段数 = `(int)(vibrato × duration)`（≥2 钳位），
//   我们的实参 = 原版那四个字面量 ⇒ `(int)(5 × 0.4) = 2` ⇒ endValues = `[direction, Vector3.zero]`
//   ⇒ **单峰，构造性不穿零**（原版同一套参数、同一个算法 ⇒ 原版也是 0 次）。
//   判据出处：`DG.Tweening.DOTween::Punch` 的方法体（IL 逐条读过）+ 原版 `GameAssembly.dll` 同函数反汇编
//   （`DOTween$$Punch` RVA 11334416；两边的 `count` 算式与 end 数组构造逐句一致）。
//   ⇒ 现在断的是**曲线的形状**（端点 + 不向下穿），那才既能反映原版、又挡得住瞎写的实现。
CheckTrue(punchMin >= -1e-3f,
          $"★ …而且**从不向下穿零**（采样到的最小偏离 {punchMin:F4}）——"
          + " `count = (int)(5 × 0.4) = 2` ⇒ 曲线 = `1.0 → 1.1 → 1.0`，"
          + "**只涨不跌到 1 以下**；写成 `1.0 → 1.1 → 0.9 → 1.0`（过冲）或 `DOPunchScale(…, 10, 1)`) 这里会红");
```

**（b）再加两条「端点」断言**（段界与缓动无关，是**可判别**的那部分）：

```csharp
// 段界（缓动无关）：endValues[0] = direction 落在 delay + duration/3；末点回 0 落在 delay + duration。
//   duration/3 的出处 = DOTween 的段时长算式 `dur[i] ∝ (i+1)/count` 归一化到 duration：
//   count = 2 ⇒ [0.4×1/2, 0.4×2/2] = [0.2, 0.4] ⇒ 归一化 (×0.4/0.6) ⇒ [0.4/3, 0.8/3]。
float d11 = RewardWindow.PunchDelayAt(11, 12);                 // 0.75（原版延迟系数 0.75，已另有断言）
CheckNear(PunchDevAt(rwP, 11, d11 + 0.4f / 3f),         0.1f, 0.01f,
          "★ punch 的**第一段走完那一拍**正好停在原版那个幅度上（`direction = 0.1 × oneVector`；"
          + "`count = 2` ⇒ 段 1 = `duration/3`）—— 把幅度写成 0.3/0.05、或把段时长写成各半的实现这里红");
CheckNear(PunchDevAt(rwP, 11, d11 + 0.4f),              0.0f, 0.005f,
          "★ …`delay + duration`（0.75 + 0.4）**正好回到 0**（`endValues[count-1] = Vector3.zero`）");
```
（`PunchDevAt(w, i, t)` = 「从建窗起把 `CardTween` 推到 `t` 秒，返回 `w.PunchScaleOf(i).x − 1`」的本地小助手；
采样步长取 `≤ 0.4/3/4`，并在 `t` 附近取**最靠近**的那一拍 —— 端点那两条的容差已经留够。）

### #11 —— 把 `Editor/RewardsScene.cs:6227-6230` 改成两条

**（a）「折行真的生效」这一半**（把读数挪到 TMP 自己重排之后）：

```csharp
a266Root.SetActive(true);
a266Lb.ForceRelayout();     // 🔴 必须在 SetActive **之后**：TextMeshPro.Awake()（第一次激活时跑）
                            //    会把 `m_textInfo = new TMP_TextInfo(this)`（TextMeshPro.cs:582-593）
                            //    ⇒ 激活前生成的 `lineCount` **必然被清零** ⇒ 直接读它恒为 0（旧写法就是这么红的）。
                            //    `ForceRelayout()` 走 `ForceMeshUpdate()`（那时 `m_isAwake == true`）⇒ 版面回来。
CheckTrue(a266Lb.LineCount >= 2,
          $"★ **激活之后**：版面按框宽真的折了行（实测 {a266Lb.LineCount} 行）"
          + " —— 读之前必须先 `ForceRelayout()`（理由见上）");
```

**（b）「A266 那一跳真的被触发」这一半**（旧写法**根本没有**这一半 —— 见 §二·#11 那张两世界对照表）。
需要在 `Battle/Label.cs` 加一个**不依赖 `m_textInfo`** 的可观测：

```csharp
// Battle/Label.cs —— 与 `_pendWrapW` 放一起
/// <summary>自检用：`TryApplyPendingWrap` 真的兑现过一次的次数（**加这个是因为 `LineCount` 那次读不出真相**
/// —— `TextMeshPro.Awake()` 会在第一次激活时换掉 `m_textInfo`，把 `lineCount` 清零）。</summary>
public int PendingWrapAppliedCount { get; private set; }
…
void TryApplyPendingWrap() { …; _pendWrapW = -1f; PendingWrapAppliedCount++; TmpFont.GenerateLayout(_tmp); RefreshBounds(); }
```
```csharp
int appliedBefore = a266Lb.PendingWrapAppliedCount;
a266Root.SetActive(true);
CheckTrue(a266Lb.PendingWrapAppliedCount > appliedBefore,
          "★ **激活那一刻** `Label.OnEnable` 真的把待办兑现了（⛔ 别用 `LineCount` 判这一条 ——"
          + " `TextMeshPro.Awake()` 会把 `m_textInfo` 换掉 ⇒ 它恒为 0，读不出「兑现没兑现」；"
          + "`WorldW` 也不行 —— 它自己会 `EnsureMeasured()` 兜底兑现，删掉 `OnEnable` 照样绿）");
```

⚠️ 同时把 `:6231-6234` 那条 `WorldW` 的**称号**改掉 —— 它现在是「★ …而且 `WorldW` 是真量出来的」，
但它**不能**证明待办兑现那一刀（`WorldW` 的 getter 自己会兑现，`Battle/Label.cs:53`）。
改法：把它降级为「（结果）折行宽真的落到了框内」，并写明「本条不证 `OnEnable` 那一跳」。

---

## 四、若是 (β)/(γ)：最小改法 + 归属哪一件

| 归属 | 内容 | 最小改法 | 判据出处 |
|---|---|---|---|
| **#1 根因** | 夹具 `RewardsScene.Build` 手抄第三份「管理器 + 三锚点」，`Instance` 恒 null；A309 新接的 `RewardWindow.Show()` 走 `EnsureHost()` ⇒ **另建一台** | `Editor/RewardsScene.cs:703-710` → `var wm = WindowsManager.EnsureHost();`（删掉 `:704-707` 三句 `MakeHolder`） | `Shell/WindowsManager.cs:565,577-578,595-620,618` · `Shell/RewardWindow.cs:446,469` · 日志 `:16944-:17101` |
| **#2** | 级联 | 随 #1；⛔ **不要**去改 `RefreshOpenDailyWindows` 的 `WindowsManager.Instance`（`DailyData.cs:868`）—— 那句是对的，错的是 `Instance` 没指到 `wm2` | 同上 |
| **#4-#8** | 级联（遗留窗的 `AbsorbHit` 3131 / `Shade` 3130 压住条目） | 随 #1（修好后 §九 那扇窗只会开在 `wm2` 上，`wm2.CloseAllWindows()`（`:5261`）就能关掉它） | `Shell/RewardWindow.cs:119,185,654` · `Shell/PointerLayer.cs:1067` |
| **#9/#10** | 级联（遗留窗的 `BackgroundHit` 3130 > 收件箱的 3002） | 随 #1 | `Shell/InboxWindow.cs:139,209` · 同上 |
| **W1/A309** | 引入者 | —— | `资料/普查产出_1011/W1_A309_A310.md`（§二·③ 开窗时机） |

**⚠️ 一个必须说清的次序**：这 9 条**只能一起绿**。修 #1 之后，
`wm2.CloseAllWindows()`（`Editor/RewardsScene.cs:5261`）才关得掉 §九 的窗
⇒ §六 的悬停（#4-#8）与「点窗外关窗」（#9/#10）才会恢复。
⛔ **别为了让 #9/#10 单绿而去调队列常量** —— 那是拿我们自己的实现证明我们自己的实现。

---

## 五、判不了的（缺什么才能判）

1. **`Label.OnEnable` 到底有没有被调用**（#11 的「谁先跑」那一半）。
   缺：一次带 `Debug.Log` 的 `RewardsScene.Run`，或一条不依赖 `m_textInfo` 的计数器（§三·#11(b) 那个）。
   ⚠️ **但它不影响 #11 的判定**（两种世界读数都是 0）。
2. **`DOPunchScale` 在**原版**里那三个字面量的**第四次**复算（我只复算了 `rewardwindow` 那一处；
   `RewardWindow__DoRewardAnimation.c:110-114` 的 0.4 / 5 / 0.2 / 0.1 我**采信 W1 + 审查_W1 的读数**，没有自己 `read_literal`）。
   缺：`工具/read_literal.py` 跑一次四个地址（`0x1834b3158 / 0x1834b2bb0 / 0x1834b2dc4`；vibrato 是立即数）。
   ⚠️ **不影响 #3 的判定** —— 那一档的结论只用到「`vibrato × duration` 会退化成 2」，
   而这两个值（5 / 0.4）在**我们自己的代码**里也写着同样两个数（`Shell/RewardWindow.cs:273-274`），
   断言**用我们自己的实参**算出来的段数**就是** 2。
3. **`§九` 那扇遗留窗期间，还有没有别的断言被它悄悄影响**（我只逐条核了 11 条红；
   绿的里面凡是「真鼠标 / 窗口态 / 截图」类的都值得再扫一遍 —— 尤其 `:19043-19070` 那三条收件箱窗的绿）。
   缺：一次「修好 #1 之后再跑一遍 `RewardsScene.Run`，比通过数的涨跌」—— 但那是主对话的事（我不跑 Unity）。

---

## 六、顺手发现（⛔ 只报不改）

1. 🔴 **`d:/4/_tmp_view/rewards/05_收件箱_空态.png` 拍的不是收件箱** —— 整屏是 §九 遗留的那扇
   `Reward Window`（**"Rewards claimed" / "Get Reward" / "Click to continue"**，收件箱面板只在后面透出来）。
   它出现在 `rewards.log:18151`（`Editor/RewardsScene.cs:5324` 那句 `Shoot`），
   **比 #9/#10 还早** ⇒ 那张图从本批起就名不副实。
   `Shoot` 的「平均亮度 > 3」（`:761`）抓不到这一类 —— 那扇窗是亮的。
   **修法建议**：§六 那张图改走一个「先断当前窗口态/当前页」的助手（照 A300 的 `ShootShellPage` 那一族），
   ⛔ 别只靠亮度。
2. **同形的「手抄管理器 + 三锚点」全仓还有 3 处**（`AddComponent<WindowsManager>` 直接建）：
   `Editor/CollectionScene.cs:677` · `Editor/SettingsScene.cs:2275` · `Editor/ShopScene.cs:1064`
   （`RewardsScene.cs:710` 是第 4 处）。这 4 个夹具里 **`WindowsManager.Instance` 恒 null** ⇒
   **任何新接的 `EnsureHost()` 开窗路径都会再犯一次 #1**（A309 只是第一个受害者）。
   建议：要么 4 个夹具一起收口到 `EnsureHost`，要么把 `Shell/RewardWindow.cs:446` 换成
   「先复用已经在管窗的那一台」（`Instance` → 否则任一 `openWindows` 非空的管理器 → 才 `EnsureHost`）。
   ⚠️ 我**没有**核这 4 个夹具各自现在有没有一条 `EnsureHost()` 开窗路径在跑（那要跑 Unity）——
   这是**新账**，不是本条红的判据。
3. **`TextMeshPro.Awake()` 会换掉 `m_textInfo`**（`TextMeshPro.cs:582-593`，仅当 `m_mesh == null`）
   —— 这条对全仓**所有「先关再建，再激活」的 TMP 夹具**都成立：
   **激活之后立刻读 `LineCount` / `textInfo` 一律不可靠**。建议写进 `资料/已知的坑.md`
   （我没写 —— 只读件）。
4. **`DOTween.Punch` 的段数公式**（`count = (int)(vibrato × duration)`，≥2 钳位；
   `elasticity` 只在 `count ≥ 3` 时才被读到）是**跨件知识**：全仓其它 punch 都要按它复核
   —— e.g. `CardFeel.PushBackVibrato = 8` × 0.4s ⇒ `count = 3` ⇒ **那一条是会穿零的**；
   而 `TargetReticle` 的 `vibrato = 0` × 0.5s ⇒ `count = 2` ⇒ 单峰。
   建议进 `资料/已知的坑.md` 或 `战斗规则与数值_出处.md`（同上，我只报）。
5. **`ShootShell`/`Shoot` 这套护栏的边界**：`:777` 的「壳窗还开着」只挡「拍了个空」，
   挡不住「拍到别的东西」。§六 这张就是实例（还有 F7 注释里记的那 4 张全黑是同一个家族的另一半）。

---

## 七、我读过的文件与命令

**读过的文件（关键处）**
- `d:/4/_tmp_view/rewards.log`（22323 行，混编码：逐行 utf-8 → gbk 兜底解；用 python 读，⛔ 没写任何中间文件）
- `d:/4/_tmp_view/rewards/05_收件箱_空态.png` · `06_重摇任务.png`（**看图** —— 这是 #1 因果链的闭环证据）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs`（`:191-305` `CheckAbsorbRule` · `:686-800`
  `Build`/`Shoot*` · `:4980-5260` §九 · `:5280-5330` §六 · `:6096-6240` §十）
- `…/Shell/WindowsManager.cs`（`:555-620` 类头/`Instance`/`Awake`/`EnsureHost` · `:658-671` `GetWindowAnchor` ·
  `:701-737` `OpenWindow` · `:893-900` `CloseAllWindows`）
- `…/Shell/RewardWindow.cs`（`:119,185,230-276,360-416,418-513,568-590,640-665`）
- `…/Shell/DailyData.cs`（`:498-543,820-880`）· `…/Shell/InboxWindow.cs`（`:80-140,174,196-219`）
- `…/Shell/PointerLayer.cs`（`:935,979,1067` 命中取法）· `…/Shell/MenuDraw.cs`（`:930-1003` `ClipText` · `:1453-1482` `Text`/`TextBox` · `:1946` `CheckShadeRule`）
- `…/Battle/Label.cs`（`:36-140,225-293,405-440,690-740`）· `…/Core/TmpFont.cs`（`:147-175,196-262`）
- `d:/4/Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TextMeshPro.cs`
  （`:549-668` `Awake`/`OnEnable` · `:2119` `ForceMeshUpdate` 的闸）· `…/TMP/TMP_Text.cs`（`:1233` `textInfo` getter）
- `d:/4/Unity/资料/普查产出_1011/`：`W1_A309_A310.md`（§一/§三）· `W3_子2.md`（§2·4/§三/§四）
  · `F2_断言修正.md`（**全篇**）· `F1_W1修复.md`（**全篇**）

**跑过的命令（全部只读）**
```bash
# 日志：按【行首标记】数失败（⛔ 没用 grep -c ✗）
PYTHONIOENCODING=utf-8 python - <<'EOF'   # 逐行 utf-8→gbk 兜底解码，扫「行首 ✗」
# 命中 22 行 → 前 11 = 真红，后 11 = 失败回顾（`:22161-22271`）
EOF

# DOTween 的方法体：Mono.Cecil 读 IL（⛔ 没有反编译工具、没有写文件）
powershell -NoProfile -Command - <<'PSEOF'
Add-Type -Path 'D:/4/Unity/MyGame/Library/PackageCache/com.unity.nuget.mono-cecil@ecb9724e46ff/Mono.Cecil.dll'
$a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('D:/4/Unity/MyGame/Assets/Plugins/Demigiant/DOTween/DOTween.dll')
… $m.Body.Instructions …                     # → 150 条指令，见 §二·#3
PSEOF

# 原版那一份：GameAssembly.dll 按 RVA 反汇编（pefile 定节 + capstone）
PYTHONIOENCODING=utf-8 python - <<'EOF'
import pefile, capstone
# RVA 11334416（= d:/2/tools/all_methods.txt:266128 `DG.Tweening.DOTween$$Punch`）
# 节表：`.text` VA 0x1000 / `il2cpp` VA 0x4b3000 RawPtr 0x4b1a00 ⇒ fo = 0x4b1a00 + (0xacf310 - 0x4b3000)
EOF
grep -n "DG.Tweening" /d/2/tools/all_methods.txt | head          # 取 RVA
head -40 /d/4/Unity/MyGame/Assets/CardPresentation/Editor/DOTweenSmokeTest.cs   # 找 DOTween 版本口（没跑）
```

**没做**（红线）：⛔ 跑 Unity / 自检 · ⛔ 动 git · ⛔ 改任何文件（本文件除外）· ⛔ 写临时文件
（`_diagB_rewards_utf8.log` 那次尝试**已经撤掉、盘上不存在**）。
