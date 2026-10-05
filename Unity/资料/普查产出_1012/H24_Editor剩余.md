# H24 · A450（ShopScene 缺把手）+ A443 的 SettingsScene 半 + A451（Editor 剩余块的 XML doc 转义）—— 写手报告（2026-10-12）

> 派单：**A450 + A443（SettingsScene 半）+ A451**。判据来源 = `资料/普查产出_1012/H15_战役节点与商店购买.md` §三②/§五·2（A450）
> · `…/H9_自检夹具族.md` §6①（A443）· `…/H16_XMLdoc转义清理.md`（A451 的做法与验法）。
> ✅ **秒级类型检查**（`TMPDIR=/tmp/wf_h24 bash d:/4/Unity/工具/typecheck.sh`）跑了 **3 次**，末次 **0 / 0**；
> 另用 **`WF_DOC=1`**（A453 新加的那一档）+ 直接调 `csc` 两份 `/doc` rsp 逐文件核了 CS1570 条数（§五）。
> ⛔ 没跑 Unity 批处理（本轮口径：A 表清完再跑）· ⛔ 没动 git（只用只读的 `git diff` / `git status`）·
> ⛔ 没改两张正本 · ⛔ 没越白名单 · ⛔ 没改 `工具/typecheck.sh`。
> ⚠️ **行号一律以「本件改完那一刻现读」为准**（本仓行号会漂 ⇒ 认符号）。

---

## 一、结论

| 账 | 结论 |
|---|---|
| **A450** | ✅ **做完**。三条**纯判据**搬进**新共用件** `Editor/RewardWindowFixture.cs`（76 行）；`Editor/RewardsScene.cs` 里那三条定义**已删**、**21 个调用点**改成 `RewardWindowFixture.xxx(...)`；`Editor/ShopScene.cs` 新增一整节 **A439 五条断言**（§三），**并用上了共用件**。⚠️ 搬的是 **3 条不是 4 条**（`ClickCollectAndDismiss` **留在 `RewardsScene`**，理由见 §二·1 —— 它靠那个宿主的**逐宿主计数器** `Check`，拿别处的 `Check` 去断会把失败记进**别人**的合计）。 |
| **A443（SettingsScene 半）** | ✅ **做完**。`Editor/SettingsScene.cs` 里失败表**重打**那一句的行首标记 `✗` → **`失败重列：`**（与五处同族逐字同形）。**至此全仓再没有「重打带 `✗`」**（`Editor/*.cs` 只剩两条**真** `✗`：`DeckScene:85` / `SettingsScene:47` 的 `Check()` 现场那条 —— 那是**现场**，不是重列）。 |
| **A451** | ✅ **做完（本件范围）**。`Editor/` 程序集那 82 条 CS1570 里，**属于本件白名单的 55 条 / 22 个根因块 → 0**。剩 **27 条**全在**白名单外**的三件（`MainMenuScene` 10 · `BattleScene` 8 · `RuleEngineTest` 9，见 §四·2）。 |
| **没做（如实标）** | ⛔ `RuleEngineTest.cs`（9 条）：它在 `Assets/RuleEngine/Editor/` 下，而派单写着「`Core\|Deck\|RuleEngine\|Net/**`（H21 在写）」⇒ **按「宁可不做也不撞车」停手**，**请调度台指派**（§四·2·b）。 |
| 🔴 **本件收工时工作区是红的（P0，不是本件造成的）** | **23:38:13 有【别的写手】把 11 个 `.cs` 覆盖成了同一份内容** ⇒ `typecheck` **运行时错误数 60**（`CS0101`/`CS0111` 重复定义 `ArenaByArmy`）。**本件自己那 4 个文件一个都不在那 11 个里**。全文与恢复线索 → **§八**。 |

---

## 二、A450 —— 改动清单（文件:行号 = 改完现读）

### 1. 为什么是「**搬 3 条**」而不是「搬 4 条」或「改成 `internal`」

- **先 `grep` 了落点**（派单要求）：`Editor/MenuCheck.cs` **不存在**（全仓只有 `资料/阶段二_卡组线_原版规格.md:157` 那句「该收口成 `Editor/MenuCheck.cs`」的**欠账**）。⇒ **没建那个名字**：
  那一笔要收的是**另一个族**（五个宿主各自一份的 `Check` / `FindChild` / `Shoot`），而其中**四个宿主正是 H23 本轮在写的文件** ⇒ 拿它当落点既越界、又把这四条钉在一个将来还要搬的名字下。
- **新建** `Editor/RewardWindowFixture.cs`（`public static class RewardWindowFixture`，`namespace` 全局 + `using CardPresentation;`）—— 名字里不带 `Probe`，是为了与 `Editor/` 既有的 `*Probe.cs`（`-executeMethod` 入口）**分开**：这是**夹具件**，不是命令行探针。
- **搬的三条**：`FindOpenRewardWindow` / `OpenRewardWindowCount` / `DismissRewardWindows`。**全是只读/关窗、一次 `Check` 都不调** ⇒ 谁调都不会把失败记到别人账上。
- **`ClickCollectAndDismiss` 留下**（`Editor/RewardsScene.cs:894-903`）：它体内要 `Check` / `CheckTrue`（**逐宿主**的计数器和 `_failures` 表）与 `ClickButtonByQuad`（那个宿主私有的真路径点击）。**搬它就得给 `Check` 传委托**（本仓有先例：`Shell/MenuDraw.CheckShadeRule(MenuCheck chk, …)`），但那会同时改**四个**宿主的调用形状 —— 而 `ShopScene` **今天根本不需要它**（商店这条链的「买」是 `ShopTabPage.Buy(idx)` **直调**，不是点 `WindowButton`；`ShowCollected` 的 `OnCollect = null` ⇒ 窗里**没有**领奖钮）。⇒ **不搬**（少一处签名改动 = 少一处能砸掉 RewardsScene 那条绿的地方）。
- ⛔ **没选「改成 `internal static`」**：那会让 `Editor/ShopScene.cs` 去依赖 `Editor/RewardsScene.cs` 的**私房件**（一个「窗状态判据」却挂在另一个**自检宿主**名下）；且与本仓「判据要共用一份」相抵。

### 2. 改动清单

| # | 文件:行号 | 改了什么 |
|---|---|---|
| 1 | **`Editor/RewardWindowFixture.cs`（新）** 全文 76 行 | 三条纯判据 + 文件头（**为什么单开** / **为什么不是 `MenuCheck.cs`** / **为什么只搬三条** / 调用方名单）。判据文本从 `RewardsScene` **原文搬过来**，只把「本文件 `§七`…」这类**自指**改成指向原处。 |
| 2 | `Editor/RewardsScene.cs:871-889` | 段头注释重写：补 **A450** 一段（三条已搬去共用件、`ClickCollectAndDismiss` 为什么留下、⛔ 别在本文件再建一份）。 |
| 3 | `Editor/RewardsScene.cs:890-902` | `ClickCollectAndDismiss` 体内 3 处调用加类名前缀（**签名一字未改** ⇒ 它自己那 6 个调用点**一个都不用动**）。 |
| 4 | `Editor/RewardsScene.cs` **其余 18 处** | 三条判据的调用点加类名前缀：**15 处代码 + 3 处注释**，现读行号 = `:1596` `:1611` `:1836` `:1898` `:3197`(注释) `:3198` `:3204` `:4261` `:4269`(注释) `:4277` `:4280` `:4283` `:5254` `:5256` `:5257` `:5701` `:6482`。⚠️ 是**脚本改的**：精确正则 `(?<![\w.])(三条名)\(` → `RewardWindowFixture.\1(`，**命中数断言 `== 21`**，不对就 `SystemExit` 不写盘（§五）。 |
| 5 | **`Editor/ShopScene.cs:1521-1640`** | 🆕 **A439 那一整节**（`Section(...)` + 5 条 ★ 断言 + 夹具卫生），见 §三。 |

**`Editor/RewardsScene.cs` 净 −48 行**（7793 → 7745）· 行为**一字未改**（同名的三条实现只换了个家）。

---

## 三、A439 那 5 条断言（**落点说明** —— 派单点名要的）

**位置**：`Editor/ShopScene.cs:1521-1640`，插在**「传奇重复购买确认」那一节之后**（原 `:1519` 的 `ClosePackAndReopenShop(win);` 后面）——
那里**商店正开着、场上没有任何遗留窗**，且它在**四张截图（`:3429-3435`）之前**，夹具**当场开当场关**（下面 ⑥⑦ 两条把它钉住）。

| # | 断言（★ = 有牙口那条） | 判据出处 | **改坏法 ⇒ 红** |
|---|---|---|---|
| ① | 买第 2 页第 1 件（`Daily Gold Cache`，`Type = Gold Item`）⇒ **`FindOpenRewardWindow()` 非空** | 原版 `…ShopOfferEventV2…HandleSuccess_0.c:23` 传 `showAnimation = **1**`（出处全文在 `Shell/ShopWindow.cs` 的 `DoBuy` 注释里） | 删掉 `DoBuy` 里 `RewardWindow.ShowCollected(...)` 那句 |
| ② | 窗里 = **`ShopData.GrantsOf(1, 0)`**：`条数` · **逐条 `Id/Quantity/Tier`** · **画出来的格子数**（`CountByPrefix(ListHolder, "Item_")`）· 另**钉两个字面量**（`40k_topmarquee_currency_gold` × **150**，= 数据层 `_daily[0]` 那一列） | `Shell/ShopWindow.cs:793` 的实参；`RewardWindow.BuildItem` 那句 `st.NodeName = "Item_" + …` **自己写着**「自检按 `Item_` 前缀数格子」 | 传错页/错件（例如递成 `GrantsOf(1, 1)` 那张野牌）⇒ 字面量那两条红；少画一格 ⇒ 格子数那条红 |
| ③ | **负例（分档）**：买第 1 页第 4 件（卡包）⇒ **一扇领奖窗都不弹** **且** `LastBoosterPack != null` | 原版 `…ContainerService…OnComplete_0.c:107` 是 `showAnimation = **0**` | 把 `DoBuy` 的 `else` 去掉、两条一起接 ⇒ 红 |
| ④ | `ShopData.Dump()` 里出现 **`有奖励表 6/10 件`** | `Shell/ShopData.Dump()` 的 A439 那一段（**可观测口径，不是静默数据**） | 给卡包那四件也填 `Grants` ⇒ 变 `10/10` ⇒ 红 |
| ⑤ | **红线**：商品档却**没有奖励表** ⇒ **出声**（`Application.logMessageReceived` 抓 `LogType.Warning` 里含 `没有奖励表` 的那条）**且不弹窗** | `ShopWindow.DoBuy` 那条 `Debug.LogWarning`（`Shell/ShopWindow.cs:787` 一带） | 把那句 `LogWarning` 删掉 ⇒ 红 |
| ⑥⑦ | **夹具卫生**：`（编排）DismissRewardWindows() == 1` · `（编排）OpenRewardWindowCount() == 0` | 弹窗会把底窗压 `Background` ⇒ `PointerLayer` 判「点不到」（`Shell/PointerLayer.cs`）⇒ 后面那些夹具/截图全废 | 把⑥⑦删掉 ⇒ **本段后面的夹具先红**（不是静默） |

**⚠️ ⑤ 是怎么造出那个状态的（如实标）**：本店 10 件**都填齐了**，而 `Shell/ShopData.cs` **不在本件白名单** ⇒ **不往里塞假货**。
改用**越界**那一支：`pg.Buy(99)` → `NeedsLegendaryConfirm` **有边界守卫**（`Shell/ShopData.cs:269`）回 false → `DoBuy(99)` ⇒ `inRange = false` ⇒ `GrantsOf` 回 `null` ⇒ 那条 `LogWarning`。
`#99（越界）` 这个 id **本次运行从没出现过**（H15 提醒的「`Note` 按 key 去重」那条不适用，已经避开）。

**⚠️ 没实跑**：上面每条「改坏法 ⇒ 红」都是**读代码 + 与判据逐条对照**得出的，⛔ **不是跑了一次真突变**（红线不许跑 Unity）。
缺的是同步点那次 `ShopScene.Run` 的实锤 —— 尤其 **② 的「格子数」**（我信的是 `BuildItem` 那句注释，**没跑过**）。

---

## 四、A451 —— 根因块表与已清/未清

### 1. 根因块表（**改后**现读；⛔ 条数 ≠ 处数 —— 一个裸 `<` 产 2~4 条）

| # | 文件 | 根因行 | 原文 → 改后 | 裸字符数 | 消掉的诊断条数 |
|---|---|---|---|---|---|
| 1 | `Editor/AudioSetup.cs` | 32 | ``Resources.Load<AudioMixer>(…)`` → `&lt;AudioMixer>` | 1 | 2 |
| 2 | `Editor/CardFaceProbe.cs` | 176 | ``…cardface_all/<净化后的卡id>.png`` → `&lt;净化后的卡id>` | 1 | 2 |
| 3 | `Editor/DeckScene.cs` | **386**（**结构性**，不是字符） | `<para>` 开 2 关 1 ⇒ 补 `</para>` 并**拆成两行**（`…。</para>` + `/// </summary>`） | —— | 2 |
| 4 | `Editor/IconSizeProbe.cs` | 93 | ``渲一行 `<caps><sprite></caps>` `` → 三处 `&lt;` | 3 | 3 |
| 5 | `Editor/PlayerBuild.cs` | 49 | ``-wfscene <子串>`` → `&lt;子串>` | 1 | 2 |
| 6 | `Editor/RewardsScene.cs` | 581 | ``…_soft<i><j>`` → `&lt;i>&lt;j>` | 2 | 4 |
| 7 | `Editor/SettingsScene.cs` | 272 | ``GetComponentInChildren<ImageQuad>()`` → `&lt;ImageQuad>` | 1 | 2 |
| 8 | `WarpforgeArena1/Editor/ArenaBuilder.cs` | 56 · 195 · 568 · 1354 · 1687 · **1771×2** · 3201 · 3369 · 3508 · 3867 · 4092（**11 块 / 12 处**） | 全是**文件名/占位**片段：``Battle_<场>.unity`` · ``<场>_shadowflags.json`` · ``WF_ENV="<SO 名>"`` · ``WF_ARENA=<场>``（×2）· ``<清单下标>[,<下标>…]`` · ``<场>_groups.json`` · ``Materials/<名字>.mat`` · ``<场>_meshkeywords.json`` · ``<场>_texslots.json`` | 12 | 24 |
| 9 | `WarpforgeArena1/Editor/EffectCompare.cs` | 34 | ``输出：`<效果>__iso<i>_{orig,exp}.png` `` → `&lt;效果>` + `&lt;i>` | 2 | 3 |
| 10 | `WarpforgeArena1/Editor/EffectIso.cs` | 1121 | ``cullingMask = 1 << IsolateLayer`` → `1 &lt;&lt; IsolateLayer` | 2 | 4 |
| 11 | `WarpforgeArena1/Editor/EffectSweepBatch.cs` | 548 | ``（`sum<10` 是 0.7%…）`` → `&lt;10`（同行的 `sum>=1000` **不用改**，`>` 在 XML 正文里合法） | 1 | 4 |
| 12 | `WarpforgeArena1/Editor/MeshVertexColors.cs` | 278 | ``WF_VCOLDUMP=<场>:<obj>`` → `&lt;场>` + `&lt;obj>` | 2 | 3 |
| | **合计** | **22 个根因块** | **28 处裸 `<`（0 处裸 `&`）+ 1 处结构性** | **28** | **55** |

### 2. 已清 / 未清

| 范围 | 改前 CS1570 | 改后 | 结论 |
|---|---|---|---|
| **本件白名单（12 文件）** | **55** | **0** | ✅ **全清**（`/doc` 编译实证） |
| `Editor/MainMenuScene.cs` | 10 | 10 | ⛔ **H23 在写** ⇒ 不碰 |
| `Editor/BattleScene.cs` | 8 | 8 | ⛔ **H23 在写** ⇒ 不碰 |
| **`RuleEngine/Editor/RuleEngineTest.cs`** | 9 | 9 | ⛔ **归属不明 = 停手**（见下） |
| **Editor 程序集合计** | **82**（15 文件） | **27**（3 文件） | 差 = **55** ✅ |

#### a) 顺手发现的**副作用诊断**（**别误当成「改坏了」**）

`Editor/RewardsScene.cs` 的 CS1573（「参数 x 在注释里没有匹配的 `param` 标记」）**22 → 23**，新增的那条是
`:598` 的 `ScanSoftCuts(Transform, bool, PxRect)` 少一个 `<param name="root">`。
**根因**：那条 `<summary>` 原来**被裸 `<` 撑坏了、编译器根本没解析到那里**；转义之后注释**第一次被真正解析**，于是这条**本来就在**的格式提示露了出来。
**它的三个同族副本**（`ShellScene` / `CollectionScene` / `MainMenuScene` 各自的 `ScanSoftCuts`）**早就都在报同一条** ⇒ 这是**归队**，不是新缺陷。
⛔ **本件没顺手补 `<param>`**（A451 的验收是 CS1570 归零；补了会让这一份与另外三份**又不一致**，而另外三份里两个是 H23 的）。

#### b) `RuleEngineTest.cs` 那 9 条：**停手**，请调度台一句话定

派单的白名单写「`Core\|Deck\|RuleEngine\|Net/**`（H21 在写）」，而这一件在 **`Assets/RuleEngine/Editor/`** ⇒ **字面命中那条禁令**；
可派单另一处又写「`Editor/` 下其余文件的 XML doc 转义」，而 A451 的账（H16 §4·3）把**整个 Editor 程序集 15 文件 / 82 条**都算成「剩余块」。
**两条读法冲突 ⇒ 我没动它**（宁可留一条明账，不冒撞车的险）。它的 9 条**判据/做法与上表完全同族**，`WF_DOC=1` 一跑就定位得到，谁接手都是十分钟的活。

---

## 五、怎么验的（**原文**）

```text
# ① 秒级类型检查（每次改完 .cs 都跑；本件共 3 次，末次）
$ TMPDIR=/tmp/wf_h24 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---      运行时错误数: 0
--- 编辑器程序集 ---      编辑器错误数: 0

# ② 带 /doc 的两遍（用 A453 新加的 WF_DOC=1 档生成 rsp，再直接调 csc 拿全量输出）
$ WF_DOC=1 TMPDIR=/tmp/wf_h24 bash d:/4/Unity/工具/typecheck.sh
  运行时 XML doc 警告数: 521（格式类，不含 CS1591）        ← 与本件无关（本件只碰 Editor/，且 gen_csc_rsp.py
  编辑器 XML doc 警告数: 50（格式类，不含 CS1591）            的运行时那份**排除** /Editor/ 路径）
$ dotnet …/csc.dll @/tmp/wf_h24/wf_csc_editor_doc.rsp -utf8output > editor_doc_after.txt   # 退出码 0
  逐文件 CS1570（改后）:  MainMenuScene 10 · RuleEngineTest 9 · BattleScene 8   = 27
  逐文件 CS1570（改前）:  … 见 §四·2 那张表（15 文件 / 82 条）⇒ 本件范围 **55 → 0** ✅

# ③ 扫描器（`/d/4/_tmp_view/scan_doc.py`，做法照 H16 §3·2）
$ python scan_doc.py <12 个文件>          # DRY-RUN：列「裸 < / 裸 & / 块级不平衡 / 全标签不平衡」
  改前：裸 < 处 = 28 · 裸 & 处 = 0 · 块级 {DeckScene para 1} · 全标签 {AudioMixer1, sprite1, i1, j1, ImageQuad1, obj1, para1}
  改后：裸 < 处 = 0 · 裸 & 处 = 0 · 块级 {} · 全标签 { 只剩 `param: -N` —— 那是**扫描器的已知假阳性**：
        它不认 `<param name="…">` 里的属性（`>` 不紧跟名字）⇒ RewardsScene 5 开/5 关、DeckScene 2 开/2 关 **实读平衡** }
$ python scan_doc.py --apply <同样的 12 个文件>   # 逐行改（**二进制**读写、只动 `lstrip()` 之后以 `///` 起头的那整行）

# ④ 自证「改动行全是注释行」
$ git diff -U0 -- <A451 那 9 个纯转义件> | grep '^+' | grep -v '^+++' | grep -vE '^\+\s*///'
  8 个文件：0 条（`+` 行总数 = 1，就是那一行本身）· DeckScene：594 条非注释 —— 那是**另一个写手的 600+ 行代码改动**
  （A364），我那一处**逐条看过**：只有 `+ /// …</para>` 与 `+ /// </summary>` 两行（§三 §四·1 #3）

# ⑤ 行尾（python **二进制**数，改前 / 改后）
  AudioSetup 0/251 · CardFaceProbe 264/264 · DeckScene 3543→**3544**/3544（**新拆出一行**，仍纯 CRLF）·
  IconSizeProbe 0/187 · PlayerBuild 0/227 · RewardsScene 0/7745 · SettingsScene 0/2429 · ShopScene 0/3636 ·
  RewardWindowFixture 0/76 · ArenaBuilder 4514/4514 · EffectCompare 0/334 · EffectIso 1532/1532 ·
  EffectSweepBatch 746/746 · MeshVertexColors 0/331
  ⇒ **一处都没翻**（脚本里带断言：`data.count(b'\r\n')` 与改前逐位相等，不等就 `SystemExit` 不写盘）✅

# ⑥ A450 的脚本自证（`/d/4/_tmp_view/a450_rewrite.py`）
  待删块 = 行 871..935（65 行）· 调用点加前缀 = **21 处**（断言 `== 21`）· 本文件已无那三条定义 ✅ · CR=0
  另：全仓 `grep -rn "RewardsScene\.(三条名)"` = **空**（没有悬空引用）✅
```

---

## 六、同步点该跑哪几条（📌 派单点名要写）

| 入口 | 为什么 |
|---|---|
| **`ShopScene.Run`** | 动了 `Editor/ShopScene.cs`（**新增 A439 一整节：5 条 ★ + 前提 + 编排，约 18 条断言**；`Section` 也多了一节）⇒ 这**必须**跑 |
| **`SettingsScene.Run`** | 动了 `Editor/SettingsScene.cs`（失败表重打那一行 **只改文案、不改条数**）⇒ 跑它验「合计没变、红行不再翻倍」 |

**⛔ 不必跑全套 11 条**：本件**没碰** `Shell/*` / `Battle/*` / `RuleEngine/**` / `Net/**`，也**没碰**共用件以外的宿主 Build
（`RewardWindowFixture.cs` 是**新文件**，只有 `RewardsScene` 与 `ShopScene` 两个消费者 —— 严格说 `RewardsScene.Run` 也在覆盖面里，
**但**它那 21 处只是**同一份实现换了个名字**，行为一字未改 ⇒ **建议顺带跑一条 `RewardsScene.Run` 当保险**，不跑也说得过去）。

🔴 **判绿红看「断言合计」那一行，别看退出码**（`BattleScene.Run` 的 139 是已知间歇，本批不涉及）。
⚠️ **`ShopScene.Run` 若红，先看 `失败重列：` 开头那几行**（A350 已把 ShopScene 的标记换成它了）。

---

## 七、顺手发现（⛔ 只报不改）

1. 🔴 **A443 还有【第 7 处】**：**`Editor/DeckScene.cs:556`** ——
   `foreach (var f in _failures) sb.Append("\n").Append(P).Append("   ✗ ").Append(f);`
   **形状与 `CollectionScene:5082` 逐字同族**（那一处 H9/A350 已改成 `失败重列：`），同样会让 `deck.log` 里 `✗` 行数 **×2**。
   **不在本件白名单**（`DeckScene.cs` 只允许改 **XML doc 转义**）⇒ 没动。**改法**：`"   ✗ "` → `"   失败重列："`（照 `CollectionScene.cs:5082`）。
   ⚠️ 同文件 `:85` 那条 `✗` 是 `Check()` **现场**那条 ⇒ ⛔ 别一起改（H9 §5·2 那条纪律）。
2. 🔴 **`Editor/ArenaBuilder.cs` 本轮【同时有两个写手】**：我改那 12 处 `///` 转义时，工作区里已经有另一个写手的
   **A349** 改动（`/// ⚠️ **2026-10-12 订正（A349）…**` 那 9 行注释，在 `SameObject` 附近）。
   **没有互相盖掉**（`git diff` 两份都在），但**文件所有权重叠**这件事值得调度台知道 —— 派单只点名了 H22/H20/H21/H23。
   （同族：`Editor/DeckScene.cs` 有 **A364** 的 600+ 行改动；`Editor/RewardWindowFixture.cs` 与它们**没有交集**。）
3. ℹ️ **新文件没有 `.meta`**：`Editor/RewardWindowFixture.cs` 是**新建**的 ⇒ 下一次 Unity 导入时才会生成
   `RewardWindowFixture.cs.meta`（本件⛔ 不跑 Unity ⇒ 我**没有手写**一个 guid）。**风险 = 0**：那句类只被**代码**引用
   （`RewardWindowFixture.Xxx(...)`），**没有任何场景/预制体按 guid 指它**。`gen_csc_rsp.py` 是**扫目录**的（不是读 csproj）⇒ 编译已经认得它 ✅。
4. ℹ️ **`Editor/MenuCheck.cs` 这笔账还在**（`资料/阶段二_卡组线_原版规格.md:157`）：五个宿主各自一份 `Check`/`FindChild`/`Shoot`。
   本件**没碰**（四个宿主是 H23 的），只把**领奖窗**那三条先搬了出来当**先例**。
   ⇒ 将来收口时，**先例的写法**（新文件 + `public static` + 调用点带类名）可以直接照抄。
5. ℹ️ **`Shell/RewardWindow.cs` 的 `ListHolder` / `Context` / `NoIconItems` 都是 `public`** ⇒ 断言能读，
   **⛔ 别因为「自检要用」去给 `RewardWindow` 加 `ForTest` 口子**（本件一个字节都没改它）。
6. ℹ️ **A443 那两处「真 `✗`」的准确位置**（写在这儿免得下一个人拿错）：`Editor/SettingsScene.cs:47` 与 `Editor/DeckScene.cs:85`
   —— 都是 `Check()` 里**现场**打的那一条，**⛔ 不许改成 `失败重列：`**。

---

## 八、🔴 P0：本件收工【之后】工作区被别的写手覆盖了 11 个文件（**不是本件造成的**）

> ⚠️ 这一节**必须在同步点被读到** —— 它会让 `ShopScene.Run` / `SettingsScene.Run` **根本跑不起来**，
> 也会让「自检攒批」那一步**全线红**。⛔ 本件**一个字都没动**那 11 个文件（也不在白名单里）。

### 8·1 事实（全部可复核）

| 判据 | 读数 |
|---|---|
| 被覆盖的文件 | **11 个**（下面逐条列） |
| 时刻 | **同一个 mtime `23:38:13`**（`find Assets -name '*.cs' -newermt '…23:38:00' ! -newermt '…23:39:00'` 正好回 **11 条**，一个不多一个不少） |
| 内容 | **11 份字节完全相同** —— `md5 = 8dc28e11e62ff05def8e063741dffd42` · **9012 字节** · `wc -l` = 123 |
| 那份内容是什么 | **`ArenaByArmy`**（`public static class ArenaByArmy` + `Rows[]` + `SceneFor` / `OriginalNameFor` / `CanLoadScene` / `BattleSceneNameFor`）—— 也就是 `WarpforgeArena1/Runtime/ArenaByArmy.cs` 那一份 |
| 证据 | `typecheck` ⇒ **运行时错误数 60**，全是 `CS0101 命名空间<global namespace>已包含 ArenaByArmy` 与 `CS0111 类型 ArenaByArmy 已定义…SceneFor`。**60 条**里能定位的**全部落在上面 11 个文件**（本件逐条 `sed 's/(.*//' \| sort \| uniq -c` 核过 ⇒ 本件碰过的文件**一条都没有**）。ℹ️ 同一跑里 `编辑器错误数: 0` 是**可信的、而且是好消息**：运行时那份**没编出来** ⇒ 编辑器那份引用的是**上一次的旧 `WFCheck.dll`**，而那份 DLL 里**还是被覆盖之前的 `ShopData`（带 A439 的 `Grants`/`GrantsOf`）** ⇒ 等于**恰好**替本件证明了：「`ShopScene` 那一节 + `RewardWindowFixture` 对着**正确的 A439 API** 编得过」。 |
| 是不是我干的 | ⛔ **不是**：本件 `--apply` 只写过 11 个**路径明确列出**的 `Editor/*.cs`（与这 11 个**零交集**，且脚本带「命中数 / 行尾」双断言）；本件最后一次写盘 ≈ **23:33**，而这批是 **23:38:13** |

**那 11 个文件**（⛔ 都**不在**本件白名单）：

```text
Unity/MyGame/Assets/WarpforgeArena1/Runtime/ArenaByArmy.cs              （123 行 → 被自己那份内容覆盖）
Unity/MyGame/Assets/WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs       （HEAD 476 行）
Unity/MyGame/Assets/WarpforgeVFX/Runtime/ArenaParticleKeywords.cs       （HEAD 139 行）
Unity/MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs       （HEAD 386 行）
Unity/MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs          （HEAD 453 行）
Unity/MyGame/Assets/WarpforgeVFX/Runtime/WFModuleChangeMaterial.cs      （HEAD 419 行）
Unity/MyGame/Assets/WarpforgeVFX/Runtime/WFModuleTransformModifier.cs   （HEAD 547 行）
Unity/MyGame/Assets/WarpforgeVFX/Runtime/WFModuleTween.cs               （HEAD 310 行）
Unity/MyGame/Assets/CardPresentation/Shell/ShopData.cs                  （HEAD 264 行）
Unity/MyGame/Assets/CardPresentation/Shell/CampaignTab.cs               （HEAD 943 行）
Unity/MyGame/Assets/CardPresentation/Shell/AllianceMemberTab.cs         （HEAD 1245 行）
```

### 8·2 后果（两条，**第一条直接打在本件的交付上**）

1. 🔴 **本件的 A439 那一节会编不过**：`Editor/ShopScene.cs:1521-1640` 读的是 `ShopData.Offers(1)[0].Type` /
   `ShopData.GrantsOf(1, 0)` / `ShopData.Dump()` —— 而这些**正是今天（A439 / H15）新加的**，
   **HEAD 里的 `ShopData.cs` 还没有它们**（HEAD 那份是**纯商店数据**，264 行）。
   ⇒ **若把 `Shell/ShopData.cs` 从 HEAD 恢复，A439 那一节连带 `Shell/ShopWindow.cs` 的接线一起废掉。**
2. 🔴 **今天这一批（1012）在 `Shell/` 与 `WarpforgeVFX/Runtime/` 的未提交改动**在**工作区**里没了
   （`git show HEAD:` 只能拿回 **2026-10-11 那次 commit** 的状态）。**先别急着 `git checkout --`**（那会把今天这一批全抹掉）。

### 8·3 恢复线索（**我没动手**，交给调度台）

* **A. 逐条重放今天的 `Edit`**：`Shell/ShopData.cs` 的**全部 15 处 `Edit`**（含 `old_string` / `new_string`）就在
  `C:\Users\qjh36\.claude\projects\d--4\a87a079f-dd17-4f7f-885c-4902100e486d\subagents\agent-a53250ef77af61ed6.jsonl`
  （= **H15 那个写手自己的 transcript**，1.19 MB，本件只读不改）。
  做法：`git show HEAD:Unity/MyGame/Assets/CardPresentation/Shell/ShopData.cs` → 按顺序重放那 15 条 → 写回。
  **本件读过它所有段落的「改后」样子**（`Grants` / `GrantsOf` / `Dump()` 那三段我在 §三 引用的就是它，可当交叉核对）。
* **B. 直接找那份文件全文**：同一批 transcript 里还有一份**含 `GrantsOf` 定义文本**的
  （`…\subagents\agent-add154857ca32cd33.jsonl`），可能是某次 `Read` 的完整回显 ⇒ 先 `grep` 它能不能整篇取出来。
  同目录还有 `…/tool-results/*.txt`（超长工具输出会溢到那里）。
* **C. 另 10 个文件**走**同一套**（各自 git 历史 + 各自写手 transcript 里的 `Edit`）；`ArenaByArmy.cs` 只需确认
  「HEAD 那份 = 现在这份」（**我没核**：它的 HEAD md5 是 `493547ce…`，与现在的 `8dc28e11…` **不同** ⇒
  那份文件**也丢了今天/之后的改动**，⛔ 别当成「它没坏」）。
* ⚠️ **本件没有**、也不会去碰 `git`（`checkout` / `stash` / `restore` 一律没跑）—— 红线。
  上面所有读数都是**只读**命令（`git show` / `git diff` / `find` / `md5sum` / `ls`）。

---

## 九、还欠什么

* ⛔ **本件零自检**（按类型；用户口径：A 表清完再跑）—— `ShopScene.Run` / `SettingsScene.Run`（+ 保险的 `RewardsScene.Run`）**留给同步点**。
* ⛔ **没实跑突变**：§三 那张「改坏法 ⇒ 红」表全是**读代码 + 逐条对判据**得来的。
  尤其 **② 的「格子数 = 条数」**（我信 `RewardWindow.BuildItem` 里那句 `NodeName = "Item_"` 的注释）—— **没跑过**。
* ⛔ **`RuleEngineTest.cs` 9 条**（§四·2·b）与 **`MainMenuScene`/`BattleScene` 18 条**（H23）**没清**。
* 📌 建议给 `工具/typecheck.sh` 的 `WF_DOC=1` 档**加进常规流程**（否则这一族清完还会悄悄长回来）——
  本件**没改它**（不在白名单）。

---

*（报告完 · 写手代理 · 未跑 Unity 批处理 / 未动 git / 未改正本 / 只写了本文件 + §二 那 4 个源文件 + A451 那 12 个文件）*
