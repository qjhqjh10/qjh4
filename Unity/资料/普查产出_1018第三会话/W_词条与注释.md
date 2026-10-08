# W · 词条与过期注释（2026-10-18 第三会话 · 执行写手代理）

白名单五件，**实际落刀 3 个**（④⑤ 现读已改过，未动手）。改前改后**全 LF**，⛔ 未用 `sed -i`。

## ① `Core/Loc.cs` 加 `Battle/HUD/CreatedBy`

插在 `Battle/HUD/TargetsAvailable` 之后：`{ "Battle/HUD/CreatedBy", new Entry("由 {0} 创建", "Created by {0}") }`

**判据（逐条亲读，⛔ 不是抄注释）**
- 键 + 占位符（**载波②**，prefab 上零 `Localize`）：`d:/2/tools/il2cpp_out/stringliteral.json` →
  `0x4288580` = `Battle/HUD/CreatedBy`、`0x4265D10` = `{0}`。
- 消费点 = 反编译 `d:/2/tools/decomp_full/SupportMethods__GetCreatedByText.c`
  = `GetTranslation(该键).Replace("{0}", 创建者卡名)` —— 与上一条 `TargetsAvailable` **同一个替换式**。
- **英文形态**：原版预制体那颗 TMP 的占位串 `Created by someone fancy`
  （`assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1229881635839611202.json` 的 `m_text`，同串 3 颗）
  ⇒ 模板 = `Created by {0}`。注释里写明那是**样例值**、不是词条值。
- **中文如实标**：**我们自译的**（原版客户端无中文表；`zh_CN.csv` 按英文源串 `Created by` 精确查 **0 命中**）；
  取的是 `CardView.CreatedByLine` 原来写死那句、**逐字搬进来**。

### 🔴「兜底不用改」这半句 **不成立**（简报前提与实读不符）
- **`BattleDriver` 里根本没有这条键的兜底**（全仓 grep `CreatedBy`：该文件只有两处注释 + 一处 `SetCreatedBy` 调用点）。
- 唯一兜底在 **`Core/CardView.cs` 的 `CreatedByLine`**，它是
  `Loc.Current == AvailableLanguages.Chinese ? $"由 {creatorName} 创建" : $"Created by {creatorName}"`
  **硬拼**、**没有 `Loc.HasEntry` 这一跳** ⇒ **加完条目，界面上的字一个字都不会变**，本行今天是**惰性的**。
- `CardView.cs` **不在本笔白名单** ⇒ 未动（`CardView.cs:1798` 自己就写着「等 `Loc.cs` 收了这条键就改走 `Loc.T`」）。
  我已把这一点**写进 `Loc.cs` 那段注释**（明说本行惰性 + 要改哪一处 + 换过去界面不变 —— 两列与那两句**逐字相同**）。
- **旁证**：`Loc.EntryCount` 全仓只被 `Editor/SettingsScene.cs` 用一次、是 `> 10` ⇒ 加一条**不会**弄红；
  新中文字「由/创/建」在仓内已有 28 / 205 / 190 个 `.cs` 命中 ⇒ 烘字语料无缺口。

## ②③④⑤ 逐条
### ② `RankedRewardEventWindow.cs` 过期注释 —— 按**内容**找，命中 4 处，改了 **3 处注释**
- **a. `ArtBanner` 的 doc**：原文「🔴 **工程里没有** …… 只 staged 在 `Art/原版/0_mainmenu/`、**没进 `Resources/`**
  ⇒ 节点照建、**这一格不画** + 出声」→ **整段已不成立**。实读：`工具/import_original_art.py` 的 `MENU_IMAGES` 有
  `('40k_UI_Banner BW', 'atlasindividual_assets_0_mainmenu')`（注释就标着「战役奖励窗 `Bonus points` 横幅（A702）」）
  ⇒ 已落 `Resources/Art/ui_menu/40k_UI_Banner_BW.png`（**盘上实见**）⇒ 今天画得出来。取图口 `Tex` → `CardArt.MenuUi`。
- **b. `ArtCardBg` 的 doc**：原文「本仓 `Resources/` 下**一张都没有**」→ 同上：`MENU_IMAGES` 有
  `('40K_shop_offer_bg_Sororitas_0', 'boosterpacks_assets_all')` ⇒ 已落
  `Resources/Art/ui_menu/40K_shop_offer_bg_Sororitas_0.png`（`ls` 亲核）。取图口 = `RebuildCards()` 里那颗 `bgTex`
  （**初稿我误写成 `Build()`，发现后已订正**）。
- **c. `BuildCard` 的 doc**：原文「`Army Icon`（…灌）**与 `Background`（本仓没有那张图）都只建节点、不画**」
  → 拆开：`Army Icon` **仍**只建节点 + 出声（它的表在远端，**这条成立**）；`Background` **本仓有** ⇒ 正常铺满。
- ⚠️ **顺手发现（未改，请裁）**：同方法那颗**运行期 `Debug.Log`**（`if (!_warnedCardIcon)` 块）文案**同样是过期那句**
  —— 印「后者那张图本仓没有 ⇒ **两张都只建节点、不画**」，而上面 `if (bgTex != null) MenuDraw.Rect(...)`
  **现在真会把 `Background` 画出来** ⇒ 这句话**今天在骗人**。它**不是注释**（是 `Debug.Log` 的字符串），
  简报红线「纯注释/词条活 ⇒ 一行行为都不许改」⇒ **停手交裁**。（连带：那个 `if` 的条件只看 `_warnedCardIcon`、**不看 `bgTex`** ⇒ 图在不在都印这句。）

### ③ `ReplayStore.cs` 自相矛盾 —— **已改**
原文：`finalHash` 的 doc =「结算那一刻的引擎指纹（`NetProtocol.Fingerprint(Ctx)`）」。
新文：头一句改成 `= NetProtocol.StateHash(Ctx) —— 就这一个`；留痕写明原文写 `Fingerprint` **是错的**、
**本文件头部**那句写 `StateHash` ⇒ 同文件两说打架、而**两头本来都是 `StateHash`**、错的只有这一行字样。
**判据按符号指（⛔ 未写行号）**：写入 = `BattleDriver.RecFinish`；比对 = `BattleDriver.PlayReplay` 末尾那次
`int got = NetProtocol.StateHash(Ctx);`（两处**亲读**）。另补「为什么不能用 `Fingerprint`」（引 `NetProtocol.StateHash`
自己的 doc：它把表现层待办算进哈希 ⇒ 同一局面在两时刻不相等 ⇒ 会**假红**）+「⛔ 别照这句去改实现 —— 错的只有本行这个字样」。

### ④ `MenuWindowBase.cs` 的 `Spec.WingName` doc —— **已改过，跳过**
现读 `MenuWindowBase.cs:684-693` 已是改完形态（带「🔴 2026-10-18（**A994① · 铁律 5 订正**）」留痕，写明 `A967` 已删
`LiveOpsEventWindow` 的 `WingName = "Nine",`、四扇都落到缺省 `Header Background (1)`）。交叉核实：
`LiveOpsEventWindow.cs:813-818` 有对应 `A967` 留痕、`Spec` 里**确无** `WingName =` ⇒ **本笔一个字未动**。

### ⑤ `UnitChatPanel.cs` 的措辞 —— **该处不在这个文件；本文件已无「三档」**
- 本文件 grep `档` **只 2 命中**，两处都对：`:90`「`A940` 尾账 · **聊天五档** · 第三颗气泡 `RadioChat`」、
  `:205`「只有 `RadioMessage(90)` 那一档才亮」。（文件里另有「**三颗**气泡」= `PlayerChatDisplay`/`EnemyChatDisplay`/
  `RadioChat` —— 那是**气泡数**不是档数，✅ 正确，⛔ 别改。）
- 全仓 grep `聊天三档` ⇒ **只 1 命中**：`Shell/TutorialModePopup.cs:58`，且**已是改完形态**（「🔴 2026-10-18
  （**A971 第 11 处** · 铁律 5）：原文写『聊天**三档**』—— **是错的**」，并自述「同族其余 10 处（`BattleDriver.cs` 9
  + `UnitChatPanel.cs` 1）已在别的批次改完，本处是全仓最后一处」）。
- ⇒ **简报把第 11 处记在 `UnitChatPanel.cs` 是串行了**：真那处是 `Shell/TutorialModePopup.cs`，且**已改**；
  该文件也**不在本笔白名单** ⇒ 本笔一个字未动。

## 类型检查 · 行尾 · 没查清
**类型检查**：`TMPDIR=/tmp/wf_lc bash d:/4/Unity/工具/typecheck.sh` —— **跑了两遍**（中间又改过一次注释），
两遍都 **运行时错误数: 0 · 编辑器错误数: 0**。⛔ 未跑 Unity 批处理 / 未动 git / 未碰两张正本。

**行尾**（二进制读 `b.count(b'\r\n')` vs `b.count(b'\n')`）—— **改前全 LF，改后仍全 LF**：

| 文件 | 改前 | 改后 | numstat |
|---|---|---|---|
| `Core/Loc.cs` | CRLF 0 / LF 1090 | CRLF 0 / LF 1115 | `35 2`（含别人先手的改动） |
| `Shell/RankedRewardEventWindow.cs` | CRLF 0 / LF 640 | CRLF 0 / LF 652 | `21 9` |
| `Battle/ReplayStore.cs` | CRLF 0 / LF 286 | CRLF 0 / LF 298 | `14 2` |
| `Shell/MenuWindowBase.cs` · `Battle/UnitChatPanel.cs` | CRLF 0 | **未动** | — |

数字远小于文件行数 ⇒ **没有整篇翻行尾**。全程只用 Edit，⛔ 未用 `sed -i` / python 写文件。

**没查清 / 待裁**
1. 🔴 **① 今天不生效**：要真走词条得改 `Core/CardView.cs` 的 `CreatedByLine`（**不在本笔白名单**）⇒ 请派。
2. ⚠️ **② 那颗运行期 `Debug.Log` 的文案仍是过期那句** —— 属生产代码，**请裁**要不要改。
3. `40K_shop_offer_bg_Sororitas_0.png` 落盘时**还没有 `.meta`**（`40k_UI_Banner_BW.png` 有）—— 导入一次即生成，
   **不是「本地没有这张图」**；已写进 `ArtCardBg` 的注释免得下一个人再踩。
4. `Battle/HUD/CreatedBy` 的**英文真值在远端 I2 表、本地取不到**；表里那条是照预制体**样例串**推的模板形态（注释已如实标）。**将来远端值到位 ⇒ 直接改那一行。**
