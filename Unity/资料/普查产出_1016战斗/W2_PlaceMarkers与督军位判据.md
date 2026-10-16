# W2 · 两笔判据裁定：`PlaceMarkers` 换算（G）· 督军位 scale（H）

> 2026-10-16 · **只读诊断**（⛔ 没跑 Unity · ⛔ 没动 git（写）· ⛔ 没改任何 `.cs`/`.unity`/`.json`/正本）· 只写了本文件。
> **行号本件现核**。⚠️ `BattleDriver.cs` / `BattleScene.cs` 此刻**有别的写手在改**（`git status` 两个 `M`）——
> 标 ⏳ 的几条**在写本件这一轮里已经漂过**（`PoseFor` 12541 → **12550**），动手前按**内容** grep，别按行号。

---

## G · `BoardLayout.PlaceMarkers` 走哪条换算

### 结论
**记录甲（代码注释 `BoardLayout.cs:280-286`）错 · 记录乙（`W_卡牌飞行族普查.md:68` §二 G）对 —— 但把风险说小了。**
正解 = `LayoutSpace.ScreenToWorld(new Vector2(msp.x, msp.y))`，也就是 `DropTargetWorld` 那一支；
现在 `:288-293` 的 `FromPixel` 支 **两个轴都错**，不是「只在 1920×1080 才等价」的量纲差。

甲为什么会写反（三条，逐条有实据）：
1. **甲的前提当场不成立**：`ScreenToWorld` 用的**不是战场相机**，是静态 `LayoutSpace.Cam`
   （`LayoutSpace.cs:121-131`）；战斗里由 `BattleScene.cs:23100-23103` 的 `Apply(cam)` 赋值 —— `cam` = HUD 那台
   **正交** `Main Camera`，它还带 `MainCamera` tag ⇒ `:131` 的回落也是它。**「拿战场相机的反投影」这句里相机认错了。**
2. **「偏左 600 px」的真根因，同一 commit 的另一条注释自己写了**：`BoardLayout.cs:218-224`（`EnsureMarkers`）——
   底片**试过挂 `ArenaLayer`**，于是「按 HUD 平面口径算的位置」被**透视相机**画 ⇒ 两者不配对。
   `git log -S` 现核：`DropTargetWorld` 与 `FromPixel` 两支**由同一个 commit `a1fb82f`（2026-10-01）引入**
   （此前那版 `PlaceMarkers` 用的就是 `DropTargetWorld`），而那一笔**同时**改了「层挪回 default」+「位置换 `FromPixel`」
   ⇒ **把「层」的功记到了「换算」账上**。
3. **量纲（乙那半 · 判据是代码自己写的 doc）**：`FromPixel` 收**设计 px 1920×1080 · 左上原点**（`LayoutSpace.cs:187-189`）；
   `WorldToScreenPoint` 给**实际 px · 左下原点**（同一个仓自己核过这件事：`BattleScene.cs:11454` 写的是
   `float fromTop = 1f - bcam.WorldToViewportPoint(...)`）⇒ x 差 `pixelWidth/1920` 倍、**y 连原点方向都反**。
   ⇒ 乙那句「只有 1920×1080 下等价」**低估**：x 那一半确实只在 1920 宽重合，**y 那一半在任何分辨率下都错**，
   1920×1080 下正好是「上下镜像」。

### 证据（文件:行号，现核）
- 现码：`BoardLayout.cs:287` 默认 `DropTargetWorld` → `:288-293` 3D 覆盖成 `FromPixel`；
  **同函数的尺寸支** `:311` = `boardCam.pixelWidth / LayoutSpace.VisibleWidth`（**实际像素口径**）
  ⇒ **一个函数里两条口径打架**（这也是条静态判别式，见下）。
- 运行期相机/投影：`Unity/_tmp_view/arena3/run_battle_1014.log:126549` = `相机 aspect=1.7778 pixel=640x480 …`；
  同 run `:126491` 我方行卡心 **60.59% 距顶** · `:126502` 敌方 **39.68%**（另一轮 `_tmp_view/BattleScene.log:24836` = 60.86%）。
- 由上面两个现读数算（算术，非拍）：`ScreenToWorld` ⇒ 我方行 `y=(0.3941−0.5)×10 = −1.06`、槽 4 `x=0`；
  现码 `FromPixel` ⇒ `y=(1 − 189.2/1080 − 0.5)×10 = +3.25`、槽 4 `x=(320/1920−0.5)×17.78 = −5.93`
  （= 屏心**左边 640 px**、**上方 465 px**）。1920×1080 下 x 归位、**y 仍镜像**（±1.06 ⇒ 差 2.11 世界 ≈ **228 px**）。
- `DropTargetWorld` 那一支本身有别的活口验它：A337 落位补间终点 = `CardInteraction.cs:547` 的
  `which.DropTargetWorld(land)`，而「卡到达 = 到达 `DropTargetWorld`」那条 0.02 容差断言在最近一轮是**绿的**
  （`BattleScene.cs:3640 / :3669-3690`；`_tmp_view/battle_1016.log:4815`）。⛔ 它要是「偏左 600 px」，这条链早看出来了。
- **但它今天不可观测**：亮着那一格被 `AttachDropSlotTo` 挂成**被拖那张卡**的子物体（`BoardLayout.cs:355-373`）、
  位置另算，其余八格 `MarkerIdle` α=0。全仓 grep `SlotMarkerWorldPos|LitSlot`（`Assets/**` + 全部 `*.log`）
  ⇒ **无一处读底片的绝对位置** ⇒ 本笔的代价是「以后谁用 `SetDropSlot` 不带 reparent / 谁加一条底片断言才现形」。

### 判别式（可执行 · **必须在非 1920×1080 下跑**才有 x 鉴别力）
```
if (!bd.use3D) continue;                                            // 2D 档两支逐位相等，测不出东西
var want = bd.transform.TransformPoint(bd.DropTargetWorld(slot));    // 正解那一支
var got  = bd.SlotMarkerWorldPos(slot);                             // 被测（只比 xy，容差 0.01）
Check(xy(want) ≈ xy(got), "底片 = 该格 3D 卡心的 HUD 投影（这一支只此一处）");
Check(xy(got).y < 0f,     "我方那一行的底片在屏幕中心线**以下**（lineY 0.411 / 现读 60.59% 距顶）");
Check(|got(0).x + got(8).x| < 0.01f, "同一行 9 格关于屏心左右对称（3D 落点本来就对称）");
Check(|xy(got) − xy(TransformPoint(SlotPosition(slot)))| > 0.1f, "3D 下它**不是** 2D 行线（灭自证）");
```
鉴别力：现码在 **1080p** 就红第 2 条（镜像 228 px），在 640×480 下四条全红；`ScreenToWorld` 支全绿。
把「被测 + 期望」**一起**改回 `FromPixel` ⇒ 第 1 条会跟着绿（自证），**第 2/3/4 条仍红** ⇒ 那三条才是真判据。
另一条**不用跑**的静态判别式：**尺寸支已经是实际像素口径（`:311`），位置支就不可能用设计像素口径。**

### 没查清 + 搜过什么
- 🔴 **那张 A/B 我没能用来定案**：`_tmp_view/battle/2d_落点指示_单独亮.png`（1920×1080 · Oct 1 08:38 · 早于 `a1fb82f`）。
  按 marker 色相（r/g≈0.52 · b/g≈0.18）找连通域，只找到两块：`x 64–127 / y 128–255`（≈2500 px，左上角，更像场地/UI 的绿）
  与 `x 960–1023 / y 704–767`（73 px —— **正好在 2D 行线 708 px、屏心 x=960**）⇒ 更像「更早那版 `SlotPosition`」的残留
  ⇒ **不能当任何一支的证据**，以后也别再引它。
- ⛔ **没查到（不猜）**：`Battle.unity` 里两台相机的实际 `pixelRect`（只从日志拿到 `pixel=640x480`）；
  真机分辨率下底片的实拍（今天不可观测，见上）。
- 搜过的词/文件：`FromPixel`（全仓调用点逐个看过）· `ScreenToWorld|WorldToScreenPoint` · `DropTargetWorld` ·
  `SlotMarkerWorldPos|LitSlot|SetDropSlot|AttachDropSlotTo|EnsureMarkers|_band` ·
  `git log -S "FromPixel(msp.x"` 与 `-S "markerPos = DropTargetWorld"` · `_tmp_view/**/*.log` 与
  `Unity/_tmp_view/**/*.log`（找 `底片|SlotMarker|pixelWidth|Screen.width|投影诊断`）· `git status --porcelain`。

---

## H · `DropTargetWorld` 的督军位该不该用 `HeroScale`

### 结论
**该改**（只改 `BoardLayout.cs:510` 一处）。而且**不需要真 Play 定案** —— 「真落位」和「投影」在我们**自己**的代码里
就能对齐（这与 `WB3_A337.md:83-84` / `WB4_A337_A359.md:89-92` 的建议不同：那时还没注意到真落位的出处就在同仓）。
1. **督军位真落位 = `PoseFor`（唯一出处）**：`BattleDriver.cs:12550-12667`⏳，`:12558`
   `u.IsWarlord ⇒ ArenaSlots.HeroScale(foe)`、位置 `RootPosition(4, foe, HeroScale)`（含 `heroExtraOffset` z）。
   ★ **实证（不是读码）**：`BattleScene.cs:11203-11206` 那条软阴影断言**自己**就按「槽 4 ⇒ `HeroScale`」算期望直径，
   而它是**绿的**（`run_battle_1014.log:126131`「不符 0 张」）⇒ 满足 2% 容差 ⇒ **场上槽 4 那张卡的实际 scale 就是 0.40 / 0.77**。
   ★ 原版侧旁证：`MonoBehaviour_4372.json` 两个字段都在（`desiredScale=0.36` · `heroScale=0.40`）；
   反编译 `BattleManager__GetUnitSizeInPlay.c` 里 `cardType==10` 那一支读**另一个 offset**（`+0xa8`）、普通那支读 `+0x80`
   ⇒ 「督军另有 scale」在原版是**真的**（offset ↔ 字段名没解，不影响本条）。
2. **逆运算 `TryResolveSlot` 走的仍是 `CardScale`，但它够用**：`ArenaSlots.cs:152` 恒 `CardScale(enemy)`。
   理由 = `CardCenter` 的 **x 与 scale 无关**（`ArenaSlots.cs:134-137` 只加 `up·scale`）⇒「最近格」的 x 判据分毫不受影响；
   只有 y 门限被挪 0.05（我）/ 0.11（敌）世界单位，而门限 `tolY` = 两行投影间距的一半 ≈ **1.0 世界单位**（`:155`）⇒ 20 倍余量。
3. **不会弄坏「往返一致 18/18」**：那条断言的形状 = `DropTargetWorld(s)` 再喂回 `TryResolveSlot` 必须解回 `s`
   （`BattleScene.cs:11404-11432`；18/18 见 `run_battle_1014.log:126467`）。换成 `HeroScale` 后槽 4 的点只挪
   `(0.40−0.36)×(3.3313/2 − 0.34335) = 0.0529`（我）/ `(0.77−0.69)×… = 0.1058`（敌）世界单位 ⇒ **照样 18/18**。
   ⚠️ 同时说明：**那条断言对本笔没有鉴别力**（盖不住 0.05~0.11）。
4. 边界：**别**顺手改共用件 `ArenaSlots.TryResolveSlot`（两块棋盘共用，且 x 判据与 scale 无关，改了只挪 0.05 的 y 门限、白担风险）；
   要**在注释里写明**「逆支按 `CardScale` 是**够用**，不是判据」。
   量级（现读值算 · 1080p）：督军位卡心投影偏 **9.6 px（我 · 182.14 px/单位）/ 9.1 px（敌 · 86.2 px/单位）**、**只有 y**。
   影响面 = 一切「挂在督军位上」的 HUD 件：伤害飘字 `BattleDriver.cs:12359`⏳ · 棋盘事件特效 `:12056 / :12067`⏳ · F 那一笔的选择器锚点。

### 判别式（可执行 · 期望值取「那张卡自己的**实测**位姿」，不取常量表）
```
var v = drv.BoardViewAt(ArenaSlots.WarlordSlot, mine);      // 槽 4 那个视图
float scLive = v.transform.localScale.x;                    // ← 实测，不读 ArenaSlots 常量
var want = HudPlaneProject(ArenaSlots.CardCenter(ArenaSlots.WarlordSlot, !mine, scLive));
Check(|xy(want) − xy(bd.DropTargetWorld(ArenaSlots.WarlordSlot))| < 0.01f,
      "投影的督军位 = 场上那张督军卡**实际**位姿的 HUD 点");
Check(|xy(CardScale 版) − xy(HeroScale 版)| > 0.04f, "灭自证：两支必须可分（我 0.0529 / 敌 0.1058 世界单位）");
```

### 没查清 + 搜过什么
- ⚠️ **判不了的那一半（说清缺什么）**：本件改法把判据钉在「**槽 4 ⇔ 督军**」上，而 `DropTargetWorld` 只拿得到**格号**。
  今天等价的间接证据有两条（`BoardLayout.cs:107` `IsWarlord(slot) => slot == WarlordSlot`；`PoseFor` 用的是 `u.IsWarlord`）。
  **若哪天督军能换格**，这一支要改成「那一格的单位是不是督军」（`DropTargetWorld` 得换签名/加参数）——
  缺的是「引擎里督军是否**恒**在槽 4」的硬判据（`RuleEngine.BoardSpec` 本件没读）。
- ⛔ **没查到**：原版 `heroScale`/`desiredScale` ↔ 序列化 offset（`+0x80`/`+0xa8`）的映射 —— 本件没查
  `d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 的字段桩；**不影响本条**（H 的判据是我们自己的真落位）。
- 搜过：`HeroScale|CardScale`（`Assets/**` 全仓）· `PoseFor` · `IsWarlord` · `WarlordSlot` · `DropTargetWorld` 全部调用点 ·
  反编译 `BattleManager__GetUnitSizeInPlay.c`（全篇）· `MonoBehaviour_4372.json`（逐字段打印）；
  文档 `WB3_A337.md:83-84` · `WB4_A337_A359.md:89-92` · `W_卡牌飞行族普查.md:69,107`。

---

## 如果现在就要改，先改哪一处、风险是什么
**先改 H**（`BoardLayout.cs:510` 改成 `slot == ArenaSlots.WarlordSlot ? ArenaSlots.HeroScale(mirror) : ArenaSlots.CardScale(mirror)`）：
判据最硬（真落位同一份代码 + 一条**已绿的实跑断言**佐证）、改动一行、风险低 —— 两条既有断言的余量都吃得下
（18/18 的 `tolY≈1.0` ≫ 0.05~0.11；A359 那条比的是「与 2D 行线的距离」≈0.5~0.7 世界，不受影响）。
**再改 G**（`BoardLayout.cs:293` 的 `FromPixel` → `ScreenToWorld`）：今天**画面上看不出来**（亮的那格被 reparent），
所以它的风险不是「改坏画面」而是「改完仍没人验、下轮再漂一次」⇒ **同批把那四条判别式断言补上**，否则这一笔改不动账。
⚠️ 通用风险：`BattleDriver.cs` / `BattleScene.cs` **此刻有别的写手**（`git status` 两个 `M`）⇒ 行号会继续漂
（本件一轮内 `PoseFor` 就从 12541 漂到 12550）—— **按内容 grep 定位，⛔ 别按行号**。
