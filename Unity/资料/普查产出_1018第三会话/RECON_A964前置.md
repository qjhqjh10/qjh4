# A964 前置判据 · 只读现核（2026-10-18 第三会话）

> 立项 = `资料/待办判据_1018.md` §A964 + §A964 续。侦察 = `资料/普查产出_1018/P-HIT_命中区探针侦察.md`（**先读它**）。
> ⛔ 本件**没跑 Unity**、**没改别的文件**、**没动 git**。所有读数 = 静态代码判定。

---

## 🚨 0 · **推翻侦察报告前提**的发现（本件最大产出）

**`m_RaycastPadding` / `m_Padding` 的符号：🔴 负 = 外扩，不是「往里缩」。** 侦察 §4·3 那条「校准样本」的**前提是反的**。

**判据（Unity 自己的代码，本地就有）**：① 官方测试 `Library/PackageCache/com.unity.ugui@27635d171b1a/Tests/Runtime/UGUI/EventSystem/GraphicRaycasterTests.cs:82-101`：`raycastPadding = (-50,-50,-50,-50)`，指针放在 **rect 外 60 px** ⇒ `Assert.IsNotEmpty(results, "…outside the graphics RectTransform whose padding would make the click hit")` ⇒ **框外还命中 = 负值外扩**。② 官方编辑器 gizmo `…/Editor/UGUI/UI/GraphicEditor.cs` 的 `DrawRect`：`p0 = rect.x + offset.x` · `p2.x = rect.xMax − offset.z`（Top/Bottom 同形）⇒ 负 offset ⇒ 两边各自**向外**。③ 同一函数也服务 `RectMask2D`（`RectMask2D.cs:178-183` `IsRaycastLocationValid` 传 `m_Padding`）⇒ **`m_Padding` 同符号**。

**我们仓里两种口径并存，一对一错：**

| 位置 | 口径 | 判 |
|---|---|---|
| `Shell/MenuDraw.cs:419-421 PaddedRect`（`x1+pad.x`/`x2−pad.z`/`y1+pad.w`/`y2−pad.y`）· `:474`「**正值缩小、负值扩大**」· `Shell/BoosterInfoPopup.cs:494-501`（三条反证）· `Editor/CollectionScene.cs:1624-1633`（`(−20)×4` 外扩 56.86→96.86） | 负 = 外扩 ✅ | **对** |
| 🔴 `Battle/BattleDriver.cs:3044`「**负 = 往里缩**」· `:3123` 同 · `:3045`「64.443×61.846 → **48.443×45.846**」· `:3047-3048` `CameraResetHitPxW/H = Rect **−** 16f` · `Editor/BattleScene.cs:13044-13053`（连文案） | 负 = 内缩 ❌ | **错** |

🔴 **这是一条真缺陷（铁律 11：与原版不符 ⇒ 记录 + 完全复刻）**：原版那钮的命中区应是 **`64.443+16 = 80.443` × `61.846+16 = 77.846`**，我们做成了 `48.443×45.846` —— **每边少 8 px、面积少约 42%**，症状正是 A964 要抓的那一族（「看着在钮上、点不动」）。⇒ **请调度台写进 `项目任务.md` §三**（我只读代理⛔不改代码/正本）。

**判据链逐行核过**（⚠️ 侦察写的 `:11349`/`:11299` 对不上，以本表为准）：

| 环节 | 出处 | 事实 |
|---|---|---|
| 原版 rect | `BattleDriver.cs:3046` | `64.4429931640625 × 61.84600830078125`（`RectTransform_3487`） |
| 原版 padding | `:3067-3069` | `MonoBehaviour_4796.json`：`m_RaycastTarget 1` · `m_RaycastPadding (−8,−8,−8,−8)` · `m_PreserveAspect 1` |
| **正确命中区** | 上式外扩 | **80.443 × 77.846**（我们写成 48.443 × 45.846 ❌） |
| **实绘 = 61.846²** | `HudAbs` 定义 **`:11397-11405`**（**`:11400`：`w` 只参与算中心**；真正传下去的是 `h/108f` `:11402`）· `HudImageTex:12141-12144`（只收 `worldHeight`）· `Battle/ImageQuad.cs:107-108`（`WorldW = _worldH × _aspect`）⇒ 237×237 的图 ⇒ 61.846² | 调用点 `:11559`；建完立刻 `SetActive(false)` `:11562` |
| 🔴 **修完会红的断言** | `Editor/BattleScene.cs:13048-13053`（`off423 = 25px`，靠「25 > 半宽 22.9」区分命中/实绘）⇒ 正确半宽 38.92 ⇒ **必须重写**（改 35 px 档：命中区内、实绘外，两向都断）—— 未实测 | —— |

**三条连带影响**：① 侦察 §6 陷阱 6「原版本来就『命中比实绘小』：`m_RaycastPadding` 为负」—— **反了**，负 padding 只让命中区**更大**。② **白名单规则改成**：**padding ≤ 0 时「命中 ⊇ 实绘」才是无条件的原版行为；正 padding（= 缩小）才是白名单**。正 padding 极罕见 —— `BoosterInfoPopup.cs:495-496` 已实读：**全包 11880 个带该字段的件里 11672 个是 (0,0,0,0)，非零几乎全负，唯一一个正的** = `DeckInfoPopup.WarlordPad`（`Shell/DeckInfoPopup.cs:157` = `(246.8,84.44,338.6,132.38)`，走 `PaddedHitRect:819`）—— 这就是外壳侧 E2 的**白名单全集**。③ **校准样本仍成立但性质变了**：`CameraResetButtonHit` 是 **E2 的一条【真红】而非白名单项** —— 报得出来，且**该报**。

---

## 1 `PointerLayer` 现状 + 建议开什么口

### 1·1 🔴 **先纠一条：不能开 `internal`，必须 `public`**

本仓 **0 个 `.asmdef`**（`find MyGame/Assets -name "*.asmdef"` ⇒ 空）⇒ 运行期在 `Assembly-CSharp.dll`、`Editor/**` 在 `Assembly-CSharp-Editor.dll`；`internal` 是程序集级 ⇒ **编辑器程序集看不见**。判据 = `资料/已知的坑.md:2927`「**`internal` 在自检里用不了** …… **要开读口就用 `public`**（或 `[InternalsVisibleTo]`，本工程没用过）」。
逐个核过运行期那几个 `internal`（`AnimFXController.PlayCue` · `MulliganPanel.SetScriptFont` · `SettingsPanel.SolidTex` · `CardDisplayWindow.SolidTexFor` · `CardView.FaceRoot` · `TmpFont.Kill`）⇒ **Editor 侧引用数 = 0**。⚠️ `SearchingOpponentWindow.DeckIndex:230` 是 `internal`，Editor 读的是**另外两个 public 同名字段**（`PracticeModePopup.DeckIndex:672` · `DeckInfoPopup.DeckIndex:543`）—— 正是坑表点名的「同名不同类」陷阱。
⇒ 写成 **`public static` + `ForTest`**，与本类既有约定一致（`ButtonCountForTest:953` · `ButtonCountUnder:966` · `ClearScrolls:212`）。

### 1·2 今天有什么口 / 缺什么

| 想要 | 今天 | 出处 |
|---|---|---|
| 「这一点上是谁」/「点一下」/「可导航数」 | ✅ `ButtonAt` · `ClickAt` · `ButtonCountForTest` · `ButtonCountUnder` | `:935` `:926` `:953` `:966` |
| **「某颗 `WindowButton` 的命中区矩形」·「它的命中 quad」·「哪颗钮不可命中」** | ❌ **全没有**（`HitBoxPx:870` · `HitQuad:897` · `Navigable:944` · `AllButtons:908` **全 private static**） | —— |

🔴 缺的正是 **E1 的判据**。拿点去 `ButtonAt` 试**试不出「矩形是多少」**（要二分 ≈22 次 × 逐颗钮）。不补口 ⇒ 探针只能抄 `HitBoxPx:870-886` ⇒ 违反 §三「两处写同一条规则」+ **改坏实现时探针跟着错 = 自证**。

### 1·3 建议开的三个口（**3 行**，放 `:886` 后或 `:911` 那节起）

```csharp
/// <summary>自检用：这一颗钮的**命中区矩形**（画布 px，世界帧；`false` = 它今天不可命中）。
/// 严格转发 `HitBoxPx`（唯一实现）—— ⛔ 探针别再抄一份算式。</summary>
public static bool HitBoxForTest(WindowButton b, out Vector2 center, out Vector2 half, out ImageQuad quad)
    => HitBoxPx(b, out center, out half, out quad);
public static ImageQuad HitQuadForTest(WindowButton b) => HitQuad(b);
public static WindowButton[] AllButtonsForTest() => AllButtons();   // 含不可命中的（ButtonCountForTest 会滤掉）
```

---

## 2 `BattleDriver` 档位表 + `cameraReset` 是什么 + 改哪几行

### 2·1 现有档位表（**两张 switch，逐字同形，都没有 `default`**）

| 档 | `HudButtonWorldPosForTest:12898-12909` | `HudButtonActiveForTest:12912-12923` |
|---|---|---|
| `settings`/`cemetery`/`chat`/`offensive` | ✅ 各一 `case` → 对应 `_xxxBtn` | ✅ 同 |
| **`cameraReset`** | ❌ **没有** | ❌ **没有** |

### 2·2 `"cameraReset"` = 什么 / 探针为什么需要它

**是什么**：`_cameraResetBtn`（`:977`，HUD「重置自动镜头」钮；建在 `:11559`、**建完立刻 `SetActive(false)`** `:11562`）。
**三条理由**：① **全 HUD 唯一「出厂就藏」的钮** ⇒ E4「藏起来 ⇒ 不许响应」**白送一个样本**；② 它的命中判定（`CameraResetButtonHit:3128`，public）与显隐（`ToggleCameraResetButton:3102`，public）各有真判据，而 `HandleCameraResetButton:3138-3141` **判了 `activeSelf`** ⇒ **正例**，与 `_settingsBtn`（§4·4，**没判**）正好一对 ⇒ **两态判别式现成**；③ E4 要 `foreach which in {…}` 跑到底，缺这档就得开特例 ⇒ **5 档只覆盖 4 档、静默少一条**。

### 2·3 按符号给改法

**① 两处各加一行**（`:12903` 那组与 `:12917` 那组）：`case "cameraReset": q = _cameraResetBtn;  break;` —— ⚠️ **两处都要**，只加一处 = 静默半条。

**② 新增写口（约 14 行，放 `:12923` 后）**：

```csharp
/// <summary>自检用：把 HUD 某颗钮显/藏（E4 的夹具）。⛔ 只写测试态场景 —— 生产仍走各窗的 `SetVis` / `ToggleCameraResetButton`。
/// `which` 与 `HudButtonWorldPosForTest` **同一张表**（加档要一起加）。</summary>
public void SetHudButtonActiveForTest(string which, bool on)
{
    ImageQuad q = null;
    switch (which)
    {
        case "settings":    q = _settingsBtn;    break;
        case "cemetery":    q = _cemeteryBtn;    break;
        case "chat":        q = _chatBtn;        break;
        case "offensive":   q = _offensiveBtn;   break;
        case "cameraReset": q = _cameraResetBtn; break;
    }
    if (q != null) q.gameObject.SetActive(on);
}
```

⚠️ 本写口**只 `SetActive`、不起 tween** ⇒ 不给 `cameraReset` 引 `DOPunchScale`；但若之前有人 tick 过 punch，`localScale ≠ 1` ⇒ **喂点前探针要自己归位**（§6·4）。
**③ 顺手**：给 `HudButtonWorldPosForTest` 注释补一句「返回的是 **quad 中心**（`anchor` 非 (0.5,0.5) 时 ≠ 命中矩形中心）」；并在 `BattleScene` 补一条 `Check` 把 `HudButtonWorldPosForTest("cameraReset")` 与既有 `CameraResetButtonWorldPos:3176` **钉成相等**（今天应等，但**零断言**）。

---

## 3 原版 `m_RaycastPadding` / `m_Padding` 实读

**核心结论就是上面 🚨 段**（符号定义 + 我方两种口径 + 唯一正 padding 站点 + `BoosterInfoPopup.cs:495-501` 给出的全包分布）。**逐窗全量真值表**见 `## 附` —— ⛔ 若那边报「查不到」，就按「本件只核了 🚨 段那几处 + 全包分布来自我方既有实读」交账，**不拿我们自己的常量冒充原版判据**。

---

## 4 两块探针：宿主 · 段落 · 行数 · 扫什么 · 输出 · 两个阳性样本的证据

### 4·0 宿主与落点（**宿主内新段，⛔ 不新建文件** —— 同 §A964 续⑤）

| 块 | 宿主 | 落点（按符号） | 行数 |
|---|---|---|---|
| **H-外壳** | `Editor/ShellScene.cs`（5657 行） | `Run()` 最后一个节（★ A435·庚 `:5139`）的 `{}` 收口**之后**、`Debug.Log(P + shell.Dump())`（`:5584`）**之前** ⇒ 插在 `:5582` 与 `:5584` 之间 | ~170 |
| **H-战斗** | `Editor/BattleScene.cs`（17893 行） | A463 段收口（`:16401` 的 `}`；`:16399` 已打「--- A463 段结束（下面还有别的段）---」）**之后**、`// 收尾：把玩家的 Auto Zoom …`（`:16403`）**之前** | ~200 |

⚠️ `ShellScene.Build` 是 `static`（= private，`:556`）· `QuadPxRect` private（`:141`）· `TmpSpanPx` `internal static`（`:192/217`）⇒ 写进宿主文件**零接线**；写新文件要改可见性 + 手抄 `BattleScene.Run:281-330` 那堆全局态压平（**夹具漂移先例**）。

### 4·1 H-外壳（~170 行）· 扫 `PointerLayer.AllButtonsForTest()`（§1·3 新口）

- **E1（不可命中）· ~45 行**：逐颗 `b` 跳过 `absorbOnly`（同 `Navigable:944`）与 `GetComponentInParent<GameWindow>(true)?.CurrentState == Background`（同 `PointerReachable:1093`）；`HitQuadForTest(b) == null` ⇒ 报。🔴 **报时必须打「它在不在已开的窗里」** —— `HitQuad` 恒 null 有**三个**原因（`:899` 不 active+enabled / `:901` 无 quad / `:901` quad 不在激活链），分不清就会把「窗没开」报成「A8」（**一片假红**）。⇒ **逐窗开**：`CloseAllWindows()`（`:1257` 有用法）→ 逐扇 `OpenWindow` → 扫 → 关，**一次只开一扇**（被压到 `Background` 的窗不参与命中）。
- **E3（平手）· ~40 行**：按 `q.RenderQueue` 分组建队、逐对求交非空 ⇒ 报。只比**命中区之间**（文字层不带命中区 ⇒ 不踩 `MenuDraw.cs:2103-2106`「`Absorb` 与文字档同号是故意的」）。
- **两条结构断言 · ~35 行**：★ 命中 quad 的 `anchor == (0.5,0.5)`（`HitBoxPx:875` 拿 `transform.position` 当矩形中心的**前提**，今天成立但**零断言**）；★ 命中 quad **不是**九宫格/平铺的子块（`CreateNineSlice:422`/`CreateTiled:499` 的**根上没有 quad** ⇒ `HitQuad` 会取到**第一块角块**）。
- **输出**：`d:/4/_tmp_view/hitprobe/shell_hits.tsv`（`节点路径 · 命中矩形 · 实绘矩形 · 四边差 px · RenderQueue · 父链窗名+CurrentState`）+ 日志打 `扫了 N 颗 / E1 报 M / E3 报 K`（**N 必须打**，否则「0 条」不可信）。⛔ 不写 `资料/`；**截图截不出这类缺陷**（写进段头）。
- 🔴 **覆盖度如实打**：能扫多少取决于探针**能开出几扇窗**（`GameWindow` 子类 ≥20，`ShellScene.cs:3014` 那条前提写着 `nWin >= 20`），而**每扇窗构造入口不同** ⇒ 必须把「**没覆盖到的窗类**」逐条打出（`typeof(GameWindow).Assembly.GetTypes()` 减掉扫过的），⛔ 不许让「只开 3 扇、扫完 0 条」冒充全景。

### 4·2 H-战斗（~200 行）

- **E4（藏起来还响应）· ~70 行** —— 三条前提（不写清就假红/假绿）：

| `which` | 路由入口 | 短路前提 |
|---|---|---|
| `settings` | `TickSettingsInputForTest:12864` → `HandleSettings:2892` | `_settingsPanel.Visible` 必假（`:2896` 先返回 true） |
| `chat` | `TickChatInputForTest:12862` → `HandleChatPopup`（守卫 `:3235-3236`） | `_chatPopup.Visible` 必假（`:3216` 先返回 true）；**要抬起沿**（`:3237`） |
| `cemetery` | `TickHudButtonsForTest:12873` → `HandleBattleLog:3596`（守卫 `:3621-3625`） | `_logPanel.Visible` 必假（`:3598` 先返回 true）；**抬起沿**（`:3629`） |
| `offensive` | 同上 → `HandleOffensiveButton`（守卫 `:2961`） | 抬起沿 |
| `cameraReset` | 同上 → `HandleCameraResetButton`（守卫 `:3140`+`:3141`） | 抬起沿；`localScale` 归位 |

  喂点 = `HudButtonWorldPosForTest(which)` → `SetHudButtonActiveForTest(which,false)` → `PointerWorldForTest`（`:12834`）+ `PointerHeldForTest`（`:12831`）+ `PollInputEdgesForTest`（`:12838`）。**两态**（藏⇒必须无反应 / 显⇒必须有反应）。
- **E1 在战斗侧不存在**（`ImageQuad.Contains` 不看 `activeSelf` ⇒ 没「拿不到 quad」这一档）；换成侦察 §5·3·B ④ 的「模型③ 只断**视觉中心必中**」。
- **E2（覆盖）· ~80 行** = 侦察 §5·3·B 那张 12 行表（**命中一律调生产函数**，多数已 public）；🔴 **本件新增必进表的一条**：`CameraResetButtonHit`（预期 = **违**，且**是缺陷不是白名单**）。
- **输出**：`d:/4/_tmp_view/hitprobe/battle_hits.tsv` + 段尾计数。

### 4·3 🔴 两个「已知阳性」的现有证据在哪

| 先例 | 证据 | 今天能否当**活体**阳性 |
|---|---|---|
| **`A8`**（卡组格裸节点） | **回归锁已有** = `Editor/CollectionScene.cs:1371-1447`（注释原文「**这一段是会红的**：把 `DeckCell` 改回裸节点它立刻红」）。三根钉：`:1392-1395` 命中区下**真挂着 `ImageQuad`**（★）· `:1397` 命中 quad 队列 = `QPageRow` · `:1399-1402` **走真命中路** `pl.ButtonAt(...)`（★）。另见 `资料/真Play待验清单.md:39` · `项目任务.md` §三 第 29 条 `A26` · 修法 `Shell/MenuDraw.cs:2813 MakeHitQuad` | ❌ **不能**（10-04 已修）。✅ **可当判别式**：造一颗只有 `WindowButton`、没 quad 的节点（`MenuDraw.Node` + `AddComponent<WindowButton>()`）⇒ E1 必须报；补 `MakeHitQuad` ⇒ 必须**不**报。⚠️ 夹具用完**必须 `DestroyImmediate`**（批处理无帧循环；命中表是 `FindObjectsByType` **现扫**，夹具会污染后续断言） |
| **`E12`**（聊天/墓园钮「藏起来还点得到」） | ⚠️ **只有一半**：`Editor/BattleScene.cs:14705-14747`（节 ⑭ ②③④）断的是 **`ChatButtonVisibleForTest`/`CemeteryButtonVisibleForTest` = 显/藏两态**（`:14712` `:14715` `:14732` `:14733` `:14742`）—— **一条都没断「藏起来还响不响应」**。🔴 `HudButtonActiveForTest:12912` **全仓 0 个调用点**（grep 整个 `CardPresentation/` 只命中定义行）。修法（带 `activeSelf`）= `:3235-3236`（聊天；⚠️ 侦察写的 `:3220-3224` **偏了 15 行**）· `:3624`（墓园）。另见 `真Play待验清单.md:140` | ❌ **不能当活体**（已带守卫）。✅ **可当判别式**：`SetActive(false)` 后喂钮心 ⇒ 必须 false；**删掉 `:3236` 或 `:3624` 那一句它立刻红** |

### 4·4 🔴 本件顺手查出、**E4 一跑就会翻出来**的一条（铁律 5·b 记这儿等调度台收编）

**`_settingsBtn` 没有 `activeSelf` 守卫**：`BattleDriver.cs:2912-2913` = `if (_settingsBtn == null) return false; if (!_settingsBtn.Contains(WorldPointer())) return false;` —— **第三句没了**；而 `ImageQuad.Contains` **不看 `activeSelf`**（反证：`:12659 HitTip` 自己就先判 `activeSelf` 再调 `Contains` ⇒ 作者知道这条边界）。
今天**生产不可达**：全仓 `SetVis` 只作用在 `_chatBtn:7904`/`_cemeteryBtn:7907`/牌库手牌那几件（`:7899-7901`），**没有一处关 `_settingsBtn`**（`_settingsBtn` 全部出现处已逐个 grep）。
**但 E4 会自己 `SetActive(false)` 把它翻出来** ⇒ 🔴 **探针必须带白名单/标注，否则这是第一条假红**。它是「同族漏网」的**第 5 个**成员（另四个都已带守卫：`_offensiveBtn:2961` ✅ · `_cameraResetBtn:3140` ✅ · `_chatBtn:3236` ✅ · `_cemeteryBtn:3624` ✅）。

---

## 5 派活建议

### 5·1 能不能并行 —— ✅ **两个写手**，但**共用件必须先做完**

| 件 | 独占文件 | 并行 | 撞车核算 |
|---|---|---|---|
| **W0 共用件**（§1·3 三个口 + §2·3 两处 `case` + `SetHudButtonActiveForTest`） | `Shell/PointerLayer.cs` · `Battle/BattleDriver.cs` | 🔴 **先做完**（两块都要） | 本批**没别人**碰这两文件（`可并行任务清单.md:565-575` 的 S1/S2/S3 都是别的文件；**A833 已钉死 `Editor/MainMenuScene.cs`** ⇒ 与 `ShellScene.cs` **不撞**） |
| **H-外壳** | `Editor/ShellScene.cs` | ✅ 与 H-战斗并行 | —— |
| **H-战斗** | `Editor/BattleScene.cs` | ✅ | —— |

⇒ **三步**：① W0 补共用件 → ② **秒级类型检查**（`bash d:/4/Unity/工具/typecheck.sh`，几秒）→ ③ 两块并行。⚠️ **一个文件同一时刻只有一个写手**（铁律 13·3）：`PointerLayer.cs`/`BattleDriver.cs` **不许**同时派两个写手。

| 写手 | ✅ 能碰 | ⛔ 绝对不能碰 |
|---|---|---|
| **W0** | `Shell/PointerLayer.cs` · `Battle/BattleDriver.cs` | 其余一切 |
| **W-外壳** | `Editor/ShellScene.cs` | `Editor/BattleScene.cs` · `Shell/**` · `Battle/**` · 两张正本 |
| **W-战斗** | `Editor/BattleScene.cs` | `Editor/ShellScene.cs` · `Shell/**` · `Battle/**` · 两张正本 |

**三者都⛔**：跑 Unity · 动 git · 改 `CLAUDE.md`/`项目任务.md`/`资料/**`（发现写进自己的报告）。
📌 **🚨 段那条符号缺陷**建议**单开一件**（要动 `BattleDriver.cs` **和** `Editor/BattleScene.cs` ⇒ **与 W-战斗撞 `BattleScene.cs`** ⇒ 排后或合并）；⛔ 它**不在** `可并行任务清单.md` 里 ⇒ **归属由调度台裁**。

### 5·2 自检落点（**按覆盖面，别一上来跑全套**）

| 改动 | 要跑 |
|---|---|
| **W0**（两个共用件都跨面，实测 `grep -rl`）：`PointerLayer` 被 **7 个 Editor 宿主**引用（`CollectionScene`·`DeckScene`·`MainMenuScene`·`RewardsScene`·`SettingsScene`·`ShellScene`·`ShopScene`），`BattleDriver` 被 **12 个** ⇒ **没有「只跑对应那几条」这一档** | `bash d:/4/Unity/工具/_run_8_checks.sh`（12 条，**串行**） |
| W-外壳（只加 `ShellScene.cs` 一段） | `ShellScene.Run` |
| W-战斗（只加 `BattleScene.cs` 一段） | `BattleScene.Run` |
| 纯文档 / 纯 `.py` | **0 条** |

⚠️ 判绿红看「**断言合计**」不看退出码（`BattleScene.Run` 的 **139 是已知间歇**）。⚠️ **探针不许写自证断言** —— 命中一律调生产函数，期望值取自原版字面量（§4·3 两条判别式即模板）。

---

## 6 没查清的部分（⛔ 不拿猜测填空）

1. **🚨 段那条符号缺陷的爆炸半径没穷举** —— 只核了 `BattleDriver`（错）与 `MenuDraw`/`BoosterInfoPopup`/`CollectionScene`（对）；其余折进命中区的 padding（`PaddedHitRect` / `maskPad` 的调用点）**没逐个核符号**。修 `CameraResetHitPxW/H` 会连带 `BattleScene.cs:13044-13053`（25 px 那档）**必须一起改**，替代档位（35 px）**没实测**。
2. **原版逐窗 `m_RaycastPadding`/`m_Padding` 真值表** —— 本件只交三处（`MonoBehaviour_4796` 的 (−8)×4 · `BoosterInfoPopup` 的全包分布 · `DeckInfoPopup.WarlordPad`），**没做逐包扫描**（见 `## 附`）。
3. **外壳侧 E1 的覆盖度** —— **能实扫几扇窗没实测**（子类 ≥20，构造入口各不相同）⇒ 探针必须自带「未覆盖窗类」清单。
4. **`_cameraResetBtn` 的 `localScale` 在 E4 夹具里归不归位** —— `ToggleCameraResetButton(true)` 先 `localScale = one` 再起 punch；`SetHudButtonActiveForTest` 不碰 tween ⇒ 若之前 tick 过 punch，`localScale ≠ 1` 会让**局部半宽**跟着缩。**没实测**。
5. **`HudButtonWorldPosForTest("cameraReset")` 与既有 `CameraResetButtonWorldPos:3176` 会不会打架** —— 今天应相等，**零断言**。
6. **外壳侧 E2 的假红面** —— 侦察 §7·3 说 46 处 `MenuDraw.Hit` 调用点里 `vis ≠ hr` 的**一处都没逐处核**。⇒ 建议**外壳侧第一步只上 E1/E3，E2 只在战斗侧上**（同调度台裁定 ②）。
7. **`ShellScene.cs:531 MakeNavButton` 造的夹具会不会污染同一次 `Run` 后面的断言** —— 侦察 §7·5 挂着，本件**没查**。
8. **全程没跑 Unity**（红线）⇒ 「实绘 = 61.846²」这类读数都是**静态代码判定**（`HudAbs` 算式 + `ImageQuad.WorldW = _worldH × _aspect` + PNG 头 237×237），**没在 Unity 里量过一次**。

## 附 · 原版资源逐包扫描（只读子代理，全树 `d:/2/新解包资源/assets_full/`）

**字段存在**（这份解包是 Unity 2022.1+）：`m_RaycastPadding` 命中 **22289** 个文件（16 个包）· `m_RaycastTarget` 22289（与之一对一）· `m_Padding` **2051**（RectMask2D **222** + LayoutGroup 1824 + FlexibleGridLayout 5）· `m_Softness` 222（= RectMask2D 数）。⚠️ 全小写 `raycastPadding` = **0**；**本树没有「无 type tree 的原始文件」**（6523 个非 json 全是 `AudioClip/*.ogg` / `Texture2D/*.png` 载荷）⇒ 本次 grep 是**有效否定**。
**非零**：`m_RaycastPadding` **444** 个组件 · `m_Padding` **75** 个（**全是 RectMask2D**；1824 个 LayoutGroup 全 0）。按包：`bundle_menus_assets_all` 208 · 13 个 `battlearena*` **各 17（完全同构）** · `mainmenuwarpforge` 12 · `generalgamewindows` 3。

**那条 `(−8,−8,−8,−8)` ✅ 逐字核实**：`battlearena1/GameObject/CenterCameraButton.json` → `m_Component = [3487, 4796, 4733, 1768, 4603]`；`MonoBehaviour_4796.json:22-27` = `m_RaycastTarget 1` + `x/y/z/w = −8`；该 `m_Script` 反查到 `m_ClassName "Image"`（`UnityEngine.UI`）；`m_Sprite` pid 在 `0_GeneralUI Atlas` 里索引对齐 = **`40k_UI_bt_center_camera`**（Sprite `m_Rect` 237×237）；`RectTransform_3487.m_SizeDelta = 64.4429931640625 × 61.84600830078125` —— 与 `BattleDriver.cs:3046` **字面完全相同**。13 个 arena 包**每个都有** `CenterCameraButton`，**每个都是 (−8)×4**。⇒ `BattleDriver.cs:3042-3048` 的注释**逐字准确**（含引用的文件名），**只有符号那半句错**（见 🚨 段）。

**逐窗真值表（`bundle_scenes_scenes_battlearena1`，其余 12 个 arena 同构）**：

| 节点 | 文件 | 组件 | 值 |
|---|---|---|---|
| CenterCameraButton | MB_4796 | Image | `(−8,−8,−8,−8)` |
| ChatButton | MB_5007 | Image | `(−8,−8,−8,−8)` |
| ShowCemeteryBtn | MB_4020 | Image | `(−8,−8,−8,−8)` |
| **SettingsBtn** | MB_4012 | Image | **`(−23.13,−38.6,−26.9,−25.81)`** |
| TurnBtn | MB_4543 | Image | `(−14.19,−30.3,−26.19,−18.4)` |
| EnemyInfo | MB_4095 | `NonDrawingGraphic` | `(−24.78,−18.45,−25.29,0)` |
| Close Button | MB_5056 | Image | `(−20,−20,−20,−20)` |
| Handle ×3 | 4077/4502/4984 | Image | `(−25,−25,−25,−25)` |
| Button ×2 | 4202/5292 | Image | `(0,−40,0,−40)` |
| **Dark Shade** | MB_4415 | Image | **`(0,+1000,0,0)`（正 = 缩小 1000）** |
| SpiritStoneHolder ×2 / FaithHolder ×2 | 4105/4191/5133/5169 | Image | `(+10,+10,+10,+10)` **`m_RaycastTarget=0`**（不吃射线 ⇒ 不在 A964 范围） |
| Text Area | MB_4148 | **RectMask2D** | `m_Padding (−8,−5,−8,−5)` · Softness (0,0) |

`bundle_menus_assets_all`（208 项，节选）：`Text Area` **13 个 RectMask2D 全 `(−8,−5,−8,−5)`** · `Icon`/`Background` 各 10+ 个 `(−20)×4` · `Handle` 8 个 `(−25)×4` · `info`/`Tooltip`/`tooltip trigger` 7 个 `(−15)×4` · `InboxBtn (−6.28,−23,−5.7,−23)` · RectMask2D 特例：`Viewport (0,0,0,−10)`+Softness(0,54) · `Window (0,9.69,0,9.69)` · `Dynamic Content (0,−15,0,0)` · 3 个 `(0,0,−500,0)`+Softness(100/200,0) · `(83,0,0,0)`+Softness(107,0)。
`mainmenuwarpforge`：`Background Mask` RectMask2D `(61.5,0,−46.41,0)`+Softness(71,0) · `Profile content (+42,+25,+46.5,+27.5)` · `Challenge button (−4.14,−10.1,−5.22,−10.4)` · `SettingsBtn (−4.38,−18.1,−9.7,−17.86)` · `Feedback Button / InboxBtn`（同上形）· `Icon`×2 / `Background`×2 `(−20,…)` · `Handle (−25,…)`。

🔴 **连带纠一条我方注释（铁律 5）**：`Shell/BoosterInfoPopup.cs:495-496` 写「全包 **11880** 个带该字段的件里 11672 个是 (0,0,0,0)，**唯一一个正的**在一件 `246.8,84.4,338.6,132.4` 的怪件上」—— 与本次全树实读**不符**：字段总数 **22289**、非零 **444**，**正的至少 6 组**（`(+10)×4`×52 · `Dark Shade` 的 `(0,+1000,0,0)` · `Main Menu Offer Container Stat (+2.49,…)` · `Background Mask (61.5,0,−46.41,0)` · `Profile content (+42,+25,+46.5,+27.5)` · `(83,0,0,0)`）。⚠️ 但**正的里头好几个 `m_RaycastTarget = 0`**（`SpiritStoneHolder`/`FaithHolder`/`Collectable Highlight`）⇒ **需调度台复核后改那句注释**（我没有改，只读代理）。

**没查清的**：① 444 项里只逐条列出了 ~110 项（其余同构）；② `bundle_generalgamewindows_assets_all` 的**节点名解析不出来**（其 GameObject 的 `m_Component` 里没有那几个 pid）；③ **全树只有 28 个包带 `GameObject/` 目录**，落在其余 ~63 包的组件**给不出所属节点名**（是「本树查不到名字」，**不是「没有」**）；④ **没做 `RectTransform.m_Father` 父子链遍历** ⇒ `menus` 那 208 项**没归到「哪扇窗」**（要归窗得再爬一次）；⑤ 每个 Image 的 sprite 名未逐个解析（只对 `CenterCameraButton` 做了 atlas 索引对齐）。

