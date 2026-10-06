# Block 4 · `Editor/MainMenuScene.cs`（清单 §一·4 = #27–#51）

> 写手代理（B4）· 2026-10-14（本机日期 2026-10-06）。**只改了一个文件**：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`。
> ⛔ 没跑 Unity · 没动 git · 没改正本 · 没碰别的代理的文件。
> 判据来源：`资料/普查产出_1014/清单_90条红改法.md` §一·4 + §二 + §三（B4 行）· `资料/普查产出_1013/D1013_诊断_块3_MainMenu.md` §二 §三 §五。
> ⚠️ **行号会漂** —— 下面给的「改后行号」是**本次改完的现读值**；定位一律按内容（断言文案 / 变量名）。

---

## 一、逐条（成族合并）

| # | 改了什么（文件:行 = 改后现读） | 判据 / 为什么新期望是对的 | 做完没有 |
|---|---|---|---|
| **#28–#36**（族①·9 条 α） | `Editor/MainMenuScene.cs:789`（`w0`）· `:921` · `:924` —— 三处读数 `pu.Value.w - pu.Value.x` → **`pu.Value.z - pu.Value.x`**（一处 3 行治 9 条） | `quadUnion` 那个 lambda 的末句 = `new Vector4(X1, Y1, X2, Y2)`（现读 `:751` 一带）⇒ `.x`=左沿 · `.y`=**上沿** · `.z`=右沿 · `.w`=**下沿** ⇒ 宽 = `.z − .x`。**反证（同一次运行）**：`:912` 那条断**上沿**（`pu.Value.y ≈ 16.467`）的**全绿** ⇒ `.y` 确实是 `Y1` ⇒ `.w` 只能是 `Y2`。原式的实得值 `−741.018 / −1007.528 / −1274.038 / −1540.548` 与手算 `Y2 − X1` **逐位吻合**（D1013 族①前言那张表，误差 0.001px）⇒ **实现一字不错**。⚠️ **没改成 `Vector4(x,y,w,h)`**（那样 `:912` 会反过来红） | ✅ |
| **#37–#40**（族②·4 条 α） | `:967-971` —— `float tw = tlb.WorldW * 108f;` → **读框宽**：`var twTmp = tlb.GetComponentInChildren<TMPro.TextMeshPro>();`（带一条「（前提）是真 TMP」）`float tw = twTmp != null ? twTmp.rectTransform.sizeDelta.x * 108f : -1f;` | `Label.WorldW` = TMP `textBounds` = **字形行宽**（`Battle/Label.cs` 的 `WorldW ⇒ _tmpW`），不是框；1 位数字的行宽结构上到不了 `widthMin = 103.51`（实得 13.71 ≈ 27.43/2 = **1 位:2 位**的行宽比；若是框宽四格会同值 103.51）⇒ 旧式**与实现无关地必红**。折行宽写的就是 `sizeDelta.x`（`Core/TmpFont.cs` 的 `SetWrapWidthRect`）。同形先例（**清单指定**）：`Editor/ShopScene.cs` 的 `wsTmp` · `Editor/CollectionScene.cs` 的 `tmp`；本文件内先例 `:1705-1709` / `:8591` | ✅ |
| **#41**（族④·α） | `:8310` —— 期望串 `"In Alliance\|No Alliance"` → **`"In Alliance\|*No Alliance"`** | `DirectKids` 的约定 = **关着的件加 `*` 前缀**（现读 `:8765`）；`No Alliance` 出厂 **act = F** ⇒ 期望串漏了 `*`。**同文件自相矛盾的反证**：紧随其后的 `panel.NoAllianceGo.activeSelf == false` 那条**全绿**；A103 报告 `WA103_三扇活动窗.md:133/265` 也写 `act = F` | ✅ |
| **#42**（族④·α） | `:8458` —— `"…\|Reward Tile\|Reward Help\|…"` → **`"…\|Reward Tile\|*Reward Help\|…"`** | 同上（`Reward Help` 出厂关着 = 同节 `:8509` 那条 `activeSelf == false` **全绿**；A103 报告同） | ✅ |
| **#43–#46**（族③·4 条 α） | `:8685` —— `{ imgX1 = a; imgX2 = b; }` → **`{ imgX1 = a; imgX2 = c; }`**（一处治 4 条） | `QuadPxRect(q, out x1, out y1, out x2, out y2)`（现读 `:9524`）⇒ `b` = `y1` = **上沿**；那一格的矩形 `a781Cell = (0,0,600,60)` ⇒ **上沿恒 0** ⇒ 三态（含**控制组**）都印 `0.000`。**控制组也印 0** = 与裁切无关、只能是量法坏（若实现坏，控制组该量到 600）。字那一半全绿（120.00/963.13/598.64…）⇒ A781 的功能本身是好的 | ✅ |
| **#47**（δ） | `:872-878` —— 几何节开头（注释块之后、`ClearCounterForTest()` 之前）补一句 **`MainMenuRuntime.EnergyInResourcesBar = false;`** | 该开关是**静态**（`Shell/MainMenuRuntime.cs:1088`）：夹具② 在 `:846` 置 `true`、**只在收尾 `:993` 复位**，几何节（`:878-980`）正落在中间 ⇒ `if (EnergyInResourcesBar) shown.Insert(0, EnergySpec)`（`MainMenuRuntime.cs:1209`）插在最前，而**那一支不看拥有量**（拨 0 也插）⇒ `Check(childCount, 3)` 红。本节注释自称「在夹具①那个状态上量」⇒ 补这一句才与声明一致；收尾那句照旧保留 | ✅ |
| **#48 + #49**（δ·成对） | `:8402-8411` —— ① `Check(si.MissingArt.Count, 1, …)` → **`Check(…, 0, "…开箱图取到了（素材腿已进盘）⇒ 不记账")`**；② 原 `MissingArt[0]` 那条（`Count == 0` 时三元取 `"-"`、与图名**恒不等**）**整条改写**为「**画的是哪张图**」：`si.Chests[0].Texture.name == "40k_Crate_Tier1_Iron_open"` | 图**在盘上**：`Resources/Art/ui_menu/40k_Crate_Tier1_Iron_open.png`（354,906 B，mtime 12:31:13 < `menu.log` 的 12:58:33）；代码侧 `AllianceEventScoreInfo.SwapChestOpen` 取到就 `SetTexture`、取不到才记账 ⇒ `MissingArt == 0` 是**正确行为**、旧期望过期。写成「画的是哪张图」比只断「没记账」强（后者在「取到了但没换」时照样绿）；读口 `Chests` 是**现成公开读口**（`Shell/AllianceEventScoreInfo.cs` 的 `public List<ImageQuad> Chests`） | ✅ |
| **#50 + #51**（δ·成对） | `:8543-8557` —— ① `Check(en.MissingArt.Count, 1, …)` → **`Check(…, 0, …)`**；② `MissingArt[0]` 那条（同「恒不等」）**整条改写**为断 `Energy Icon` 那一格的 **`ImageQuad.Texture.name == "40k_topmarquee_currency_energy"`**；③ 原件里那条「`Energy Icon` 节点照建（**只是这一格不画**）」的**文案已过期**，一并改成「节点在那（取不到图时那一支才是只建节点）」——断言本身保留 | 图**在盘上**：`Resources/Art/ui_menu/40k_topmarquee_currency_energy.png`（13,863 B，mtime 12:31:13，同一次拷贝）；`Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `var en = Art(ArtEnergy); if (en != null) MenuDraw.Rect(…)` ⇒ 取到就画、不记账 = 正确。同形先例（**本文件内、本轮全绿**）：`:829` 断 `40k_topmarquee_currency_gold` 那条走的是同一条 `CardArt.MenuUi` 取图路 | ✅ |
| **#27（= A812）** | ⛔ **一个字都没改** | 见下 §三 | ⏸ 待裁 |

**24 / 24 条改完**（#28–#51）；**#27 按令不动**。

### 两个 A808 备注（已写进代码注释）
`#48/#49` 与 `#50/#51` 两处的注释里都写了 ⚠️ **A808**：这批手拷图**没有任何导入器登记** ⇒
谁跑一次 `工具/import_original_art.py` 或删掉 `Resources/Art/`，这四条会**静默翻回红**
（那时要恢复「记账 1 张」那一版）。见 `清单_90条红改法.md` §三·2。

---

## 二、验了什么

- **秒级类型检查**（`TMPDIR=/tmp/wf_b4 bash d:/4/Unity/工具/typecheck.sh`）：**运行时 0 · 编辑器 0**（跑两次；第二次是改完注释后的复跑）。⚠️ 本机同时有别的代理在飞（`git diff --numstat` 里 `Editor/{DeckScene,ShopScene}.cs` 等也在动）—— 本次两次都没有报「不是你负责的文件」的错。
- **行尾**：改前 `git show HEAD:… | file -b -` = 纯 LF；改后数出来 **CRLF 0 / LF 9821**（改前 9772 + 净增 49 行，**逐数对得上**）⇒ 没翻行尾。
- **`git diff --numstat`**：`Editor/MainMenuScene.cs` **+68 / −19**（只有我这一个文件；其余行是别的代理的）。
- **没跑 Unity**（按纪律）。

---

## 三、没做完的 / 判不了的

1. 🔴 **#27（= A812）· 待用户拍板 —— 一个字没动。** 两边口径各是什么（复述诊断 §二·16 与清单 §四·8）：
   - **实现侧**（`Shell/AllianceEventScorePanel.cs` 的空串分支）：**只打日志 + `return`**（旧名字原样留着），日志文案却写「保持 prefab 出厂原文」。
   - **断言侧**（`Editor/MainMenuScene.cs:8339`）：期望「空串 ⇒ **回到出厂原文** `Alliance Name`」。
   - **写手旧报告**（`WA103_三扇活动窗.md:111`）：「空串 ⇒ **不动** + 出声」（= 现在的实现）。
   - **为什么本地判不了**：反编译只读到 `Initialize(string allianceName, …)` 直接把传入串灌给 `allianceNameText`；**空串的处置只有远端 group service 有真值**（原版客户端与本地导出里没有）⇒「该回写占位串」是**我们的口径、不是原版判据**。
   - **两条可选裁法**（二选一，两边必须对一个）：① 改实现为回写占位串（三处口径统一）；② 改 `:8339` 期望为「保持 `Tester Squad`」+ 日志文案改成「不动」（但「旧联盟名一直挂着」要另立一条账）。
2. **β 那一条** = 就是 #27（清单 §一·4 的 β 1 = D1013 §二·16）⇒ 按「待裁」处理，**没有别的 β 落在这个宿主**（D1013 §一：β = 1 条，就是它）。
3. **#37–#40 的「更好写法」我也做了**（读 `sizeDelta.x`），但诊断 §四·1 留了一个**本块判不了**的问题：`MainMenuRuntime` 算 clamp 用的 `nat = lb.WorldW * 108f`（= 字形行宽）**该不该改**，要真 Play / 实拍才能定（原版那颗 TMP 的 `m_enableAutoSizing` 会不会把字涨到 `fontSizeMax = 42px`）。⚠️ **不影响那 4 条红的判定**（那是量纲错、与实现取值无关）；**我按令只改了断言侧，没碰实现**。

---

## 四、顺手发现（⛔ 只报不改）

1. **过期注释（本文件）**：`Editor/MainMenuScene.cs:985-991` 那一整段「**本地还差 4 张币种小图标** …… 本文件末尾 `Section("图：一张都不能少")` 那一条**现在会为它们变红**」—— 实测该节 **✓**（`menu.log:36001`），4 张图 12:31 已进盘。**现在是在说反话**（清单 §四·13 已点名这一带）。
2. **弱断言 + 过期免责句**：`:976-979` 的 `paChecked >= 1` 配着「本地没导图的那几颗不画 quad，所以这个数 < 格数是**预期的**」—— 图全进盘之后，这条**分不出「4 格都画了图标」与「只有 gold 那颗画了」**（D1013 §五·2 建议随 A374 那条账一起收紧成 `paChecked == n`）。
3. **A374 覆盖缺口**：`CounterSpecs` 6 颗币种里**只断了 gold 那一颗的贴图名**（`:852-861` 一带）；crystals / blackStones / gachaTickets / energy 四颗**现在都有图了**却**没有一条断言**钉住各自画的是哪张（D1013 §五·3）。
4. **同一次素材导入的连带过期注释（别的文件，⛔ 我没碰）**：
   `Shell/EnergySinglePlayerOnlyEventWindow.cs:190`（`// 90×90 · Simple（⚠️ 本仓 Resources 里**没有**）`）· 同文件 `:544-547` · `Shell/AllianceEventScoreInfo.cs:68` / `:179` / `:211`（「开箱图本仓没有」）。
5. **A808（跨会话风险）**：`Resources/Art/**` 在 `.gitignore` ⇒ 那 13 张手拷图是**盘上事实、不是 git 事实**（素材腿一跑，`git status` 一声不响，断「缺图」的断言就全过期）。建议素材腿排到**全部写手交件之前**，或交件时复核一次 `Resources/Art/ui_menu/`（D1013 §五·5）。
6. **一处旧账仍在**：`AllianceEventScorePanel` 的日志文案 vs 语义不一致（= #27 本身）—— 待裁之后改哪边，另一边要跟着统一，⛔ 别只改一处（现在正是「三处口径两个说法」的状态）。

---

## 五、改动清单（可逐行复核）

| 现读行 | 内容 |
|---|---|
| `:789` | `float w0 = pu0.Value.z - pu0.Value.x;`（原 `.w − .x`）+ 上面 4 行注释（字段序 + 反证） |
| `:872-878` | 几何节开头注释 + `MainMenuRuntime.EnergyInResourcesBar = false;` |
| `:917-924` | `.z − .x` ×2 + 上面 5 行注释 |
| `:962-971` | 族②注释 6 行 + `twTmp` 前提 + `float tw = …sizeDelta.x * 108f : -1f;` |
| `:8310-8313` | `"In Alliance\|*No Alliance"` + 注释 |
| `:8402-8411` | `MissingArt` 0 + `si.Chests[0].Texture.name` 那条（改写 #48/#49） |
| `:8458-8461` | `…\|*Reward Help\|…` + 注释 |
| `:8543-8557` | `MissingArt` 0 + `enIconT/enIconQ/enIconTex` 两条（改写 #50/#51） |
| `:8680-8685` | 族③注释 4 行 + `{ imgX1 = a; imgX2 = c; }` |
