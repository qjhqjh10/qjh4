# Block7 · `Editor/ShopScene.cs` α 侧 8 条 + #73/#74 期望值半边（2026-10-14）

> 写手代理产出。**独占文件 = `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/ShopScene.cs`**（本批只改了这一个文件）。
> 判据来源：`资料/普查产出_1014/清单_90条红改法.md` §一 第 7 组（#83–#90）+ §二（#73/#74 = β 主因 + α 期望过期）
> + `资料/普查产出_1013/D1013_诊断_块4_Shop与Rewards.md` 逐条小节 F1–F8。
> ⛔ 没跑 Unity · 没动 git · 没改正本 · 没碰别的文件（`Shell/PurchasePremiumWindow.cs` / `Shell/BaseOfferPopup.cs` / `Battle/Label.cs` 由调度台与别的写手负责）。
> ⚠️ **本文件里的「改前 `:NNNN`」= `git show HEAD:` 那一版的坐标**；「改后 `:NNNN`」= 本批落地后的坐标（文件内行号整体后移了 19～49 行）。

---

## 一、逐条

| # | 改了什么（文件:行） | 判据 | 做完没有 |
|---|---|---|---|
| **83** | `Editor/ShopScene.cs:4046-4052`（改前 `:4043-4044`）：`gop.MissingArt.Count` 期望 **`1` → `0`**，断言文案改写成「★ 本窗**一张图都不缺**（`OctagonUI Filled SDF` 已进 `Resources/Art/ui_menu/`）」，并把上面那句「工程里没有 ⇒ 这一格不画」整段换成订正注释 | ① `Resources/Art/ui_menu/OctagonUI_Filled_SDF.png` **在盘**（mtime `2026-10-06 12:31`、`.meta` `12:40`，**早于本 run 的 12:58**）② `grep '[OptionsPanel]' shop.log` **只有 2 行、两行都是「开了…」，一条「图取不到」都没有** ③ `Tex(ArtShadow, …)` 在 `Build()` 路径上**无条件**调（`Shell/GenericOptionsPanel.cs:364`）⇒ 这个 `0` 是**实读数**、不是「没跑到所以空表」 | ✅ |
| **84** | `Editor/ShopScene.cs`（**删掉**改前 `:4045-4046` 两行「缺的是 `OctagonUI_Filled_SDF`」） | `Count == 0` 时三元取 `"-"`、与任何图名**恒不等** ⇒ 留着必红且**已无可断言对象**（清单 §一 #84 明写「删掉这两行」） | ✅ |
| **85** | `Editor/ShopScene.cs:4093-4102`（夹具，改前 `:4088`）：`MyRole = 3` → **`MyRole = 0`**；上面补 8 行注释写明「为什么是夹具错、不是实现错」 | 原版规则（`Shell/AllianceMemberOptionsPopup.cs`）：文件头 `:26` + `:179` = `outrank = sameGroup && role < myRole`；`:396-399` = `Promote = outrank && role < 3` · `Demote = outrank && role > 0` · `Kick = outrank` · `Quit = isSelf` · `Challenge = !isSelf`。喂 `Role=0 / MyRole=3` ⇒ `outrank=true` ⇒ **`Promote` 本来就该开着**，是夹具与断言文案前提不符。**影响面逐条核过**：`Quit` 那条只看 `isSelf`、`Challenge` 那条只看 `!isSelf`，**都不含 `MyRole`**（`:393-399` 的 switch 现读） | ✅ |
| **85（期望值半边）** | `Editor/ShopScene.cs:4109-4110`（原 `:4095`）：**一个字没改** —— 按清单的**主写法**（改夹具，保留对 `outrank` 的覆盖）。⛔ 没走备选「期望改 `true`」 | 清单 §一 #85 + D4 #6 的「二选一，推荐前者（保留断言的判别力）」 | ✅ |
| **73（α 半边）** | `Editor/ShopScene.cs:4157-4167`（改前 `:4143-4144`）：`ppw.MissingArt.Count` 期望 **`1` → `0`**，注释整段订正（并注明 β 那半 = A803 已由调度台在 `Shell/PurchasePremiumWindow.cs` 落地） | ① `Resources/Art/ui_menu/UI_HIghlight_Internal.png` **在盘**（12:31 / meta 12:40）② 日志里**没有** `UI_HIghlight Internal` 的「图取不到」告警 —— 同 run 里 `[Premium]` **只报了 `Army Icon`**（`shop.log:46189/46203`）⇒ 这条口是活的、没对高亮图报缺 ③ `Tex(ArtHightlight, …)` 在 `Build()` 路径上**无条件**调（`Shell/PurchasePremiumWindow.cs:701`）。⚠️ 本 run 拿到的 `0` 是 F1「`Build()` 没跑」的**平凡值**；补上 `Build()` 之后才是实读数（诊断对这一步的置信度 = **中高**） | ✅ |
| **74** | `Editor/ShopScene.cs`（**删掉**改前 `:4145-4146` 两行「缺的是 `UI_HIghlight_Internal`」） | 同 #84（`Count == 0` ⇒ 三元取 `"-"`） | ✅ |
| **86** | `Editor/ShopScene.cs:4202-4206`（改前 `:4180`）：`rre.WindowNode` 的期望串补三个 `*` → `…|*Title\|*Description\|*Timer\|Bonus points\|Scroll View`；补 3 行订正注释 | `KidNames` 给「出厂关着」加 `*` 前缀 = `Editor/ShopScene.cs:459-472` 的约定；**紧随其后三条断言**（`Title` 关 / `Description` 关 / `Timer` 整件关）本 run **全绿** ⇒ 原串与同一节自相矛盾 | ✅ |
| **87** | `Editor/ShopScene.cs:4230-4242`（改前 `:4206-4207`）：`rre.MissingArt.Count` 期望 **`1` → `0`**，文案改掉；注释里**显式加了一条「⛔ 别顺手把下面 `SetBoost` 那段的 `40K_shop_offer_bg_Sororitas_0` 也改成 0」** | ① `Resources/Art/ui_menu/40k_UI_Banner_BW.png` **在盘**（12:31）② **同一条口本 run 确有一条「图取不到」告警、但名字是 `40K_shop_offer_bg_Sororitas_0`**（`shop.log:46627`，来自 `SetBoost`）⇒ 口是活的、**没对横幅报缺** ⇒ 横幅取到了 ③ `Tex(ArtBanner, …)`（`Shell/RankedRewardEventWindow.cs:378`）在 `Build()` 路径上**无条件**调 ⇒ `0` 是实读数 | ✅ |
| **88** | `Editor/ShopScene.cs`（**删掉**改前 `:4208-4209` 两行「缺的是 `40k_UI_Banner_BW`」） | 同 #84 | ✅ |
| **89** | `Editor/ShopScene.cs:4306-4310`（改前 `:4273`）：`content` 的期望串补两个 `*` → `…\|*Referred View\|…\|counter number\|*counter`；补 3 行订正注释 | `KidNames` 的 `*` 约定（`:459-472`）；**下面那两条断言**（`Referred View` 关 / `counter` 关，逐字写着 `prefab act = F`）本 run **全绿** | ✅ |
| **90** | `Editor/ShopScene.cs:4460-4466`（**删掉**改前 `:4425-4426` 两行 `CheckTrue(WindowButton.MissingPressedArt.Count == 0, …)`）：按清单「建议直接删」处置，原位留 6 行注释说明**为什么删**（保留更正痕迹） | `Shell/PromptPopup.cs:589-591` 那张表的注释**自己写着**「⇒ 这份表**只出声、不当缺点断**」（本批现读复核过，逐字一致）⇒ 该断言与共用件的**已定口径直接打架**；且同一条信息 `Editor/ShopScene.cs:3229` 已经**打进日志**（重复）。本 run 表里那 1 条是 `" → "`（`Shell/InboxWindow.cs:339` 把常态图名传成 `null`），属注释里说的「多数是合法的」那一档 | ✅ |

**合计：清单点名的 8 条（#83–#90）全部落实 + #73/#74 的期望值半边落实 = 10 处改动，零遗留。**

---

## 二、顺手发现（⛔ 只报不改 —— 都不在本批白名单）

1. **同形的「旧口径注释」还有三处**（都是「这张图工程里没有 ⇒ 这一格不画 + 出声」，现已被素材腿证伪）：
   · `Shell/GenericOptionsPanel.cs:114-117`（`ArtShadow` 的 `/// <summary>` 末尾那句）
   · `Shell/PurchasePremiumWindow.cs:177-180`（`ArtHightlight`：「只 staged 在 `Art/原版/0_mainmenu/`、**没进 `Resources/`**」）
   · `Shell/RankedRewardEventWindow.cs:158-162`（`ArtBanner`：「只 staged 在 `Assets/CardPresentation/Art/原版/0_mainmenu/`、**没进 `Resources/`**」）
   —— 三张图**都已进盘**（12:31）。⚠️ 同文件 `Shell/RankedRewardEventWindow.cs:168-170` 关于 `ArtCardBg`（`40K_shop_offer_bg_Sororitas_0`）的同一句写法**仍然成立**（那张是真缺）⇒ 修的时候**只删前三处**、别把第四处一起删掉。铁律 5 适用，但这三个文件**不在本批白名单**（B8 只占 `PurchasePremiumWindow.cs` + `BaseOfferPopup.cs`，`RankedRewardEventWindow.cs` / `GenericOptionsPanel.cs` 没人占）⇒ 未动。
2. **`Shell/InboxWindow.cs:339` 的 `Bind(baseQ, null, "40k_general_bt_yellow_hover")`** 把常态图名传成 `null` ⇒ `MissingPressedArt` 里多一条**认不出是谁的** `" → "`（诊断 §六·2 也点了这条）。删掉断言（#90）之后它只剩「日志指不出是哪一颗」的可读性问题。建议给它一个真名字，或让 `Bind` 跳过空 `art` 的那一条。不在白名单，未动。
3. **诊断 §六·5 的「缺图三套口径」= A810**（`gop`/`ppw`/`rre` 的 `MissingArt.Count` 断言 · `:3229` 的日志 · `CheckNoMissingSwapArt`）。本次**只按最小改法把三处期望值改对**，**没有统一口径**（诊断自己建议「统一成『0 才绿』」，另立账）。
4. **本文件内部的行号引用会漂，我新写的注释一律不用本文件行号做锚点**：本批改完文件内行号整体后移 **19～49 行**（`git diff --numstat` = 61/19，文件 4591 → 4633 行）。证据：诊断自己就订正过一次 `:4086` → `:4163`；本批里 `Promote` 那条断言从 `:4095` 漂到 `:4110`。⇒ 建议把「**本文件内的注释引用一律写节点名/断言文案，不写 `:NNNN`**」当纪律。⚠️ 我**没有**去改既有注释里的行号（不在本批范围，且会引入大面积 diff）。
5. `Editor/ShopScene.cs` **是纯 LF**（`git show HEAD:` 与工作区实测都是 `crlf 0`）—— 与清单 §三 里那三条「纯 CRLF」的宿主文件不同，改前已核过。

---

## 三、没做完的 / 判不了的

1. **#79（现 `:4185-4186`，改前 `:4163`）一个字没动** —— 按简报与清单 §二 A806，它要**先只改 A803 → 跑一次 `ShopScene.Run` → 读那一条报出的实得坐标**才能反证 A780 的推导。期望值四个数（`226.055 / 757.285 / 100.57 / 263.89`）**原样保留**。✅ 已复核：本批 diff 里没有它。
2. **「缺图」类 6 条（#73/#74 · #83/#84 · #87/#88）改完也不稳**：那 13 张手拷图（含本批用到的 3 张）**没有任何导入器登记**（= A808）⇒ 谁跑一次 `工具/import_original_art.py`（或按 CLAUDE.md 那条「删 `Resources/Art/` 退回占位美术」）这几条会**静默翻回红**。已在三处注释里写明。
3. **#73 的最后一跳判不了**：「补上 `Build()` 之后 `ppw.MissingArt` 到底是不是 0」—— 我只能证到「图在盘 + `.meta` 在 + `Tex()` 在 Build 路径无条件调」；真正的实读数要等**补完 `Build()` 的那一次 run**（诊断 §五·2 自己也把它列为「判不了」，置信度标 **中高**）。
4. **没跑 Unity**（按纪律）。所以本批**没有一个实得值**是本次量到的 —— 8 条的期望值都是**静态判据**（图在盘 / 日志无告警 / 同类断言已绿），**要在调度台的同步点那次 `ShopScene.Run` 上核**。
5. ⚠️ **`SetBoost` 那一段（现 `:4243` 起）没碰**：`40K_shop_offer_bg_Sororitas_0`（阵营卡的底）**是真缺**，它进 `MissingArt` 的时机在那一段之后 —— 我改的是它**之前**的那条期望。

---

## 四、本批自证

| 项 | 结果 |
|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_b7 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 · 编辑器错误数 0** |
| 行尾 | 改前/改后都是**纯 LF**（`crlf 0 / lf 4591` → `crlf 0 / lf 4633`） |
| `git diff --numstat` | **61 / 19**（不是整篇重写）；唯一被改的文件 = `Unity/MyGame/Assets/CardPresentation/Editor/ShopScene.cs` |
| 工具纪律 | 只用 Edit 工具（⛔ 没用 `sed -i`、⛔ 没用 python 文本模式写） |
| 删除类改动（#74/#84/#88/#90）逐一核过 | 删的都是清单明写「删掉」的那四对行；`grep MissingPressedArt Editor/ShopScene.cs` 现在只剩 `:3229` 的**日志**（+ 我留的注释） ✅ |
| `#79` 未动自证 | `grep "第 1 个容器的框" Editor/ShopScene.cs` ⇒ `:4186`，四个期望数逐位未变 ✅ |
