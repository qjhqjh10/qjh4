# H21 · 非 Shell 块的 XML doc 转义（A451）+ `typecheck.sh` 的 `-doc:` 可选档（A453）+ 同族抽查（A452）

> 写手代理 · 2026-10-12。判据与做法全部沿用 H16（`H16_XMLdoc转义清理.md` §三）。
>
> 一句话：本件白名单（**`CardPresentation/{Core,Deck,Net}` · `RuleEngine/`（非 Editor）**）的
> **303 条 CS1570 → 0** —— **184 处**裸字符转义 + **11 处**结构性修补，落 **32 个文件**；
> `/doc` 编译实证、`git diff -U0` 自证**改动全在 `///` 行**。
> 另给 `工具/typecheck.sh` 加了一个**默认关**的 `WF_DOC=1` 档（开了才看得见 `CS15xx`）。
> ⛔ **没碰** `Shell/` · `Battle/` · `Editor/` · `WarpforgeVFX/` · `WarpforgeArena1/`（余量见 §四）。

---

## 一、结论

| 范围 | 改前 CS1570 | 改后 | 说明 |
|---|---|---|---|
| **本件白名单**（`Core`/`Deck`/`Net`/`RuleEngine`，非 Editor · 31 文件） | **303** | **0** | ✅ **全清**（`/doc` 编译实证） |
| 运行时程序集**全量** | 534 | **231** | 差 **303** = 本件的全部 ✅ |
| 编辑器程序集 | 82 | 82 | ⛔ 白名单外（`Editor/**` 归 H17/H19） |
| 常规 `typecheck.sh`（不带 `/doc`） | 0 / 0 | **0 / 0** | ✅ 逐字未变（§七） |
| `WF_DOC=1`（新档） | —— | **运行时 519 · 编辑器 104**（格式类；另有 CS1591「缺注释」3502 / 574） | 🆕 A453 |

* **根因**（与 H13 §八·1 / H16 §一 同族）：XML doc 注释里**贴代码 / 路径 / 占位符，`<` 与 `&` 没转义**。
  另有两族是**结构**问题而不是字符问题：**`</summary>` 多一个 / 少一个**、**`<param>` 嵌在 `<summary>` 里**。
* 🔴 **条数 ≠ 处数**（本件实测）：**303 条**诊断落在 **213 个不同的行**上，而**根因只有 195 处**
  （184 转义 + 11 结构）⇒ 平均 **1.55 条/处**。本件块的比率比 H16 那块低（H16 有过 1 行产 14 条的），
  所以**照「条数」估工作量会偏大**，一律按**根因处**记账。
* 🆕 **一支新根因**（H16 没遇到，本件撞上）：**`]]>` 在 XML 正文里是非法序列**，与 `<` 转义**无关** ——
  必须写成 `]]&gt;`（`UpnpPortMapper.cs:281`）。全仓 `///` 里只此 1 处（§六）。

---

## 二、A451 改动清单

### 2·1 「裸字符 → 实体」**184 处 / 30 文件**（165×`<`→`&lt;` · 19×`&`→`&amp;`）

| 文件 | 处 | 行 |
|---|---|---|
| `RuleEngine/Core/EffectText.cs` | 61 | 181, 558, 1274, 1280, 2341, 2419, 2470, 2555, 2596, 2817, 2818, 2819, 3622, 3771, 3772, 3807, 3822, 3828, 3937, 3939, 4086, 4180, 4186, 4514, 4711, 4919, 5150, 5290, 5401, 5641, 6040, 6042, 6246, 6247, 6248, 7325, 7344, 7401 |
| `CardPresentation/Core/CardArt.cs` | 12 | 151, 320, 337, 351, 432, 596, 648, 651, 669, 713 |
| `CardPresentation/Deck/DeckRuntime.cs` | 11 | 181, 198, 1018, 1220, 2264, 2442, 2721, 3481, 4000 |
| `RuleEngine/Core/RuleCore.cs` | 11 | 2766, 3157, 3158, 3259, 3425 |
| `CardPresentation/Core/CardView.cs` | 10 | 64, 715, 2341, 2851, 2854, 3265, 3289, 4654 |
| `CardPresentation/Core/CardIcons.cs` | 9 | 111, 120, 123, 124, 125, 126, 127 |
| `CardPresentation/Core/VoiceLines.cs` | 8 | 125, 130, 149, 193, 235, 245 |
| `RuleEngine/Core/Aura.cs` | 7 | 138, 154, 163 |
| `RuleEngine/Core/CreatePool.cs` | 5 | 241, 244, 254 |
| `CardPresentation/Core/CardText.cs` | 4 | 230, 231, 240 |
| `RuleEngine/Core/BattleContext.cs` | 4 | 36, 199, 635 |
| `RuleEngine/Data/SimpleAI.cs` | 4 | 193, 194, 203, 989 |
| `CardPresentation/Core/Badges.cs` | 3 | 165, 168, 299 |
| `CardPresentation/Core/CardFeel.cs` | 3 | 67, 138 |
| `CardPresentation/Core/RelatedCards.cs` | 3 | 18 |
| `CardPresentation/Core/TmpFont.cs` | 3 | 179, 182, 183 |
| `CardPresentation/Core/Tooltip.cs` | 3 | 57, 416, 428 |
| `CardPresentation/Net/UpnpPortMapper.cs` | 3 | 280, 281 |
| `RuleEngine/Core/CardDef.cs` | 3 | 370, 1211, 2483 |
| `RuleEngine/Core/EffectResolver.cs` | 3 | 5600, 5601, 5810 |
| `CardPresentation/Core/EnvironmentConditions.cs` | 2 | 82, 93 |
| `CardPresentation/Core/PlayerBoot.cs` | 2 | 184 |
| `RuleEngine/Core/PlayerState.cs` | 2 | 94 |
| `RuleEngine/Core/WhenEvent.cs` | 2 | 288 |
| `CardPresentation/Core/TraitParticles.cs` | 1 | 29 |
| `CardPresentation/Core/VfxMap.cs` | 1 | 29 |
| `CardPresentation/Core/WarpforgeAudio.cs` | 1 | 22 |
| `CardPresentation/Deck/DeckEditorState.cs` | 1 | 37 |
| `CardPresentation/Net/NetTransport.cs` | 1 | 269 |
| `RuleEngine/Core/GivePayload.cs` | 1 | 59 |
| **合计** | **184** | 30 文件 |

### 2·2 结构性修补 **11 处 / 7 文件**（不是字符问题）

| # | 文件:行 | 改了什么 | 判据 |
|---|---|---|---|
| S1 | `CardPresentation/Core/BattleAutoDrive.cs:493` | **删**行尾多余的 `</summary>` | `:491` 已经 `<summary>…</summary>` 关过一次（编译器指的就是这一处） |
| S2 | `CardPresentation/Core/CardText.cs:400` | 行尾**补** `</summary>` | 那一块 `summary` 开 1 关 0（`:401` 就是下一个成员声明） |
| S3 | `CardPresentation/Core/VfxMap.cs:42` | 行尾的 `*/` → **`</summary>`** | 🔴 **A452 那一族的真凶**：作者写了 `*/`（块注释的尾巴），XML doc 的闭合是 `</summary>` |
| S4 | `CardPresentation/Deck/DeckRuntime.cs:4157` | **插一行** `/// </summary>` | `<summary>` 一路没关，`:4158` 直接跟 `<param name="key">` |
| S5 | `RuleEngine/Core/EffectResolver.cs:3683` | **插一行** `/// </summary>` | 同上（`BroadcastWhen` 那块，后面跟 4 个 `<param>`） |
| S6 | `RuleEngine/Core/EffectResolver.cs:4053` | 光秃的 `///` → **`/// <summary>`** | 那一块**开了 0 关了 1**（`:4065` 是 `</summary>`，前面没有开） |
| S7 | `RuleEngine/Core/UnitState.cs:90` | **删**多余的 `/// </summary>` | `:87` 已经关过一次 |
| D1 | `CardPresentation/Deck/DeckRuntime.cs:4204` | 行尾**补** `</summary>` | 同上族（`HoverTint` 那块） |
| D2 | `CardPresentation/Deck/DeckRuntime.cs:4205` | 行尾**补** `</param>` | 第一个 `<param>` 没关 ⇒ `:4208` 的 `</summary>` 才被判「与 param 不匹配」 |
| D3 | `CardPresentation/Deck/DeckRuntime.cs:4208` | **删**行尾多余的 `</summary>` | 见 D1/D2 |
| D4 | `CardPresentation/Net/UpnpPortMapper.cs:281` | `]]>` → **`]]&gt;`** | `]]>` 在 XML 正文里**非法**（与 `<` 转义无关；本条改前**也有** CS1570） |

* ⚠️ **S1/S7/D3 的裁法**（「删哪一个是多余的」有两解）**一律以编译器指的为准**：
  XML 语义上**第一个 `</summary>` 才算配对**，第二个就是「多余的」——
  编译器报的正是第二个 ⇒ 删第二个。原文**一字未动**（只删/加标记）。
* ⚠️ D1/D2/D3 是**第一轮落盘后重编才发现**的（S1～S7 那一轮只看到 3 条残余，其中 2 条在这块）。
  **`/doc` 编译是唯一的判据** —— 我自写的「块级标签平衡扫描」在**白名单**上漏掉了 `<link>`（见 §八·2）。

### 2·3 行尾（python **二进制**数，改前 → 改后）

32 个改动件**行尾类型全部保持原样**（**26 个 LF · 6 个 CRLF**），只有 3 个件行数变了（都是**结构性插/删行**）：

| 文件 | 行尾 | 行数 | 为什么变 |
|---|---|---|---|
| `CardPresentation/Deck/DeckRuntime.cs` | CRLF → CRLF | 4336 → **4337** | S4 插 1 行 |
| `RuleEngine/Core/EffectResolver.cs` | CRLF → CRLF | 6021 → **6022** | S5 插 1 行 |
| `RuleEngine/Core/UnitState.cs` | LF → LF | 725 → **724** | S7 删 1 行 |

* 全程**只走 python 二进制读 + `wb` 写**（⛔ 没有 `sed -i`、⛔ 没有文本模式 `open(...,'w')`），
  两个脚本各自带**行尾守卫断言**：① 不许有 `\r` 落在**行内**；② CR/LF 计数只允许按「插了几行/删了几行」变化。
  🔴 **写这条守卫时真抓到一次**：第一版脚本对 CRLF 文件做 `行 + '</summary>'` ⇒ `\r` 会被插到行中间
  （**静默毁文件**，且 CR 计数**不变**、守卫抓不到）—— 在 DRY-RUN 就发现并改掉了（§八·5）。

---

## 三、根因块表（**改前** 303 条是怎么分布的）

列 = 文件 · 条数 · 根因行（`:条数` = 该根因块产出的诊断条数，按「诊断行归属到它前面最近的那个根因」聚块）。

| 文件 | 条 | 根因块（行:条数） |
|---|---|---|
| `RuleEngine/Core/EffectText.cs` | 85 | 181:2 · 558:1 · 1280:2 · 2341:2 · 2419:2 · 2470:2 · 2555:2 · 2596:2 · 2819:2 · 3622:2 · 3772:2 · 3807:2 · 3822:2 · 3828:3 · 3939:2 · 4086:2 · 4180:2 · 4186:2 · 4514:2 · 4711:2 · 4919:2 · 5150:2 · 5290:2 · 5401:2 · 5641:2 · 6042:3 · 6248:2 · 7325:2 · 7344:2 · 7401:2（⚠️ 表里只列**产出了诊断**的那 30 个根因行；本文件另有 **8 个转义行**编译器一条都没报） |
| `CardPresentation/Deck/DeckRuntime.cs` | 30 | 181:2 · 198:2 · 1018:2 · 1220:1 · 2264:4 · 2442:2 · 2721:2 · 3481:2 · 4000:2 · 4157:1 · 4206:2 |
| `CardPresentation/Core/CardArt.cs` | 23 | 151:2 · 320:2 · 337:2 · 351:2 · 432:3 · 596:2 · 651:2 · 669:2 · 713:2 |
| `CardPresentation/Core/CardView.cs` | 19 | 64:2 · 715:2 · 2341:2 · 2851:1 · 2854:3 · 3265:3 · 3289:2 · 4654:2 |
| `CardPresentation/Core/CardIcons.cs` | 16 | 111:2 · 123:1 · 125:1 · 126:1 · 127:3 |
| `RuleEngine/Core/RuleCore.cs` | 13 | 2766:1 · 3157:1 · 3158:1 · 3259:1 · 3425:2 |
| `RuleEngine/Data/SimpleAI.cs` | 11 | 193:1 · 194:1 · 203:1 · 989:2 |
| `CardPresentation/Core/VoiceLines.cs` | 10 | 125:1 · 130:1 · 149:1 · 193:2 · 245:2 |
| `RuleEngine/Core/Aura.cs` | 10 | 138:2 · 154:2 · 163:2 |
| `CardPresentation/Core/CardText.cs` | 9 | 230:1 · 231:1 · 240:3 · 400:1（S2） |
| `RuleEngine/Core/BattleContext.cs` | 7 | 36:2 · 199:2 · 635:2 |
| `RuleEngine/Core/EffectResolver.cs` | 7 | 3683:1（S5）· 4053:1（S6）· 5601:2 · 5810:2 |
| `CardPresentation/Core/CardFeel.cs` | 6 | 67:1 · 138:2 |
| `CardPresentation/Core/Tooltip.cs` | 6 | 57:2 · 428:3 ← **两条都是 `<link>`**（§八·2） |
| `RuleEngine/Core/CardDef.cs` | 6 | 370:2 · 1211:2 · 2483:1 |
| `CardPresentation/Core/Badges.cs` | 5 | 168:2 · 299:1 ← `<link>` |
| `CardPresentation/Core/TmpFont.cs` | 5 | 179:1 · 183:2 ← `<link>` |
| `CardPresentation/Core/EnvironmentConditions.cs` | 4 | 82:2 · 93:2 |
| `CardPresentation/Core/RelatedCards.cs` | 4 | 18:2 |
| `RuleEngine/Core/CreatePool.cs` | 4 | 254:2 |
| `CardPresentation/Core/PlayerBoot.cs` | 3 | 184:2 |
| `CardPresentation/Core/VfxMap.cs` | 3 | 29:1 · 42:1（S3） |
| `RuleEngine/Core/WhenEvent.cs` | 3 | 288:2 |
| `CardPresentation/Core/TraitParticles.cs` | 2 | 29:2 |
| `CardPresentation/Core/WarpforgeAudio.cs` | 2 | 22:2 |
| `CardPresentation/Deck/DeckEditorState.cs` | 2 | 37:1 |
| `CardPresentation/Net/NetTransport.cs` | 2 | 269:1 |
| `RuleEngine/Core/GivePayload.cs` | 2 | 59:2 |
| `RuleEngine/Core/PlayerState.cs` | 2 | 94:2 |
| `CardPresentation/Core/BattleAutoDrive.cs` | 1 | 493:1（S1） |
| `RuleEngine/Core/UnitState.cs` | 1 | 90:1（S7） |
| **合计** | **303** | **根因 195 处**（184 转义 + 11 结构）|

---

## 四、已清 / 未清

### 4·1 ✅ 已清（本件范围）

**31 文件 / 303 条 → 0** —— `CardPresentation/Core/*`（20 件）· `CardPresentation/Deck/*`（2 件）·
`CardPresentation/Net/*`（2 件）· `RuleEngine/Core/*`（12 件 里的 11 件 + `RuleEngine/Data/SimpleAI.cs`）。
逐件清单 = §二 两张表（32 个改动件 = 30 个转义件 + `BattleAutoDrive` + `UnitState` 两个纯结构件）。

### 4·2 ⛔ 未清（**本件白名单外，一条没动**）

| 块 | 条数 / 文件 | 归属 |
|---|---|---|
| `CardPresentation/Battle/*` | **171 / 15** | H18/H20 在写 |
| **9 件黑名单** `Shell/*`（有命中的 6 个：`MenuDraw` 15 · `WindowsManager` 11 · `ShopData` 8 · `AllianceMemberTab` 4 · `CampaignRewardWindow` 2 · `CampaignTab` 2） | **42 / 6** | 归它们自己的写手 |
| `WarpforgeVFX/Runtime/*` | **15 / 7** | ⚠️ **无主**（H16 §七 的「按目录切块」里没点它；本件白名单也没它） |
| `Assets/WarpforgeArena1/Runtime/ArenaByArmy.cs` | **3 / 1** | ⛔ 明令不碰 |
| **运行时合计** | **231 / 29** | |
| **编辑器程序集**：`WarpforgeArena1/Editor/*` 38/5 · `CardPresentation/Editor/*` 35/9 · `RuleEngine/Editor/RuleEngineTest.cs` 9/1 | **82 / 15** | H17/H19 在写 |

* ⚠️ 其中 **`RuleEngine/Editor/RuleEngineTest.cs`（9 条）** 严格说在 `RuleEngine/**` 里，
  但派单把 `Editor/**` 整块划给了别人 ⇒ 本件**按「不越界优先」没碰它**，**在表里点名**（§九）。
* 逐文件的行号表（供后面切块直接抄）：本轮实测在
  `C:/Users/qjh36/AppData/Local/Temp/wf_h21/rest.txt`（临时目录，不随仓库走）——
  复现办法 = §七 的三条命令。

---

## 五、A453 · `工具/typecheck.sh` 的 `-doc:` 可选档

### 怎么用

```bash
# ① 默认（与以前**逐字相同**，现有调用方零影响）
bash d:/4/Unity/工具/typecheck.sh

# ② 打开 XML doc 档（**唯一的新东西**）
WF_DOC=1 bash d:/4/Unity/工具/typecheck.sh
```

`WF_DOC=1` 时**多两行**（常规那两行一字不动）：

```text
--- 运行时程序集 ---
运行时错误数: 0
<前 20 条 `warning CS15xx`（已滤掉 CS1591）>
运行时 XML doc 警告数: 519（格式类，不含 CS1591；另有「缺 XML 注释」CS1591 共 3502 条）
```

### 实现要点

* **开关**：环境变量 `WF_DOC`（`DOC_MODE="${WF_DOC:-0}"`）。**不设或 ≠ 1 = 老样子**。
* 开了之后做**两件事**（缺一不可 —— H13/H16 查实的两层原因）：
  1. `mkdoc()` 把 rsp 复制一份，在 `-out:` 那行**前面插一行 `-doc:"…/wfcheck/WFCheckDoc.xml"`**
     （⛔ **不动 `-out:`** ⇒ 产物 DLL 路径不变 ⇒ 编辑器那份 rsp 仍然找得到刚编出的 `WFCheck.dll`）；
  2. `check()` 里**另外**把 `warning CS15xx` 打出来并计数（原来只 grep `error CS`，而这族**全是 warning**）。
* ⚠️ **`CS1591`「缺 XML 注释」单列**：本工程实测 **3502** 条，混进来会把真正的格式错淹掉
  （`head -20` 那 20 行会全是 1591）⇒ 计数只算**格式类**，1591 单独报一个数。
* ⚠️ **只在 doc 档加 `-utf8output`**：csc 默认按控制台代码页（GBK）输出，中文诊断在 UTF-8 终端里是乱码 ——
  默认档**一个字节都不动**（保持逐字相同），doc 档才加。
* **不额外多一次编译**：只是给同一次 csc 多传一个 `-doc:`；两个程序集各多写一个 `.xml` 到 `$TMP/wfcheck/`。

### 本条已验

```text
① 默认档：改前/改后各跑一次 ⇒ `diff` **逐字相同**（两行都是「错误数: 0」）✅
② WF_DOC=1：跑出「运行时 XML doc 警告数: 519」「编辑器 XML doc 警告数: 104」✅
   其中 CS1570 = 231（运行时）/ 82（编辑器）—— 与本件直连 csc 的数**逐条一致** ✅
③ 生成的 XML：wfcheck/WFCheckDoc.xml（3.7 MB）· WFCheckEditorDoc.xml（489 KB）✅
```

---

## 六、A452 · 同族改动全仓抽查（「`/* */` 改 XML doc 时只换了行首」）

**搜了哪 6 种形状**（脚本只扫 `.cs` 的注释文本，⭐ = 真缺陷形状）：

| 形状 | 判据 | 命中 | 处理 |
|---|---|---|---|
| **A1** ⭐ | `///` 行的**行尾**就是 `*/` | **0**（改前 **1**） | ✅ 那 1 处 = `VfxMap.cs:42`，本件**已顺手改**（S3） |
| A2 | `///` 行**中间**含 `*/` | **13** | ⛔ **不改**：全是**正文里摘录的代码注释**（`5 /*ManaType.SpiritStone*/` 那类），对 XML 无影响 |
| **B** ⭐ | 行首是 `/**`（C# 里 `/** */` 也是 doc 写法） | **0** | —— |
| C | `///` 行含 `/*` | **46** | ⛔ **不改**：逐条看过，**全是通配符**（`MonoBehaviour/*.json` · `Editor/*Scene.cs`）或摘录注释 ⇒ 我第一版扫描器的**假阳性** |
| **D** | `///` 块里夹着以 `*` 开头的续行 | **0** | —— |
| **E** | 行首 `*/` 紧跟在一个 `///` 块之后 | **0** | —— |
| F | `///` 行落在 `/* */` 块里（= doc 被注释掉了） | **0**（全仓） | —— |

**A2 的 13 条逐条**（本件块内 6 条 · 块外 7 条「只报」）：

| 位置 | 内容 | 判断 |
|---|---|---|
| `RuleEngine/Core/BattleContext.cs:357/359/360` | `` `*(int*)(ability + 0x10) == 600 /*UseSpiritStone*/` `` 等 | 正文摘录 ⇒ **无害** |
| `RuleEngine/Core/EffectResolver.cs:3346` | `` `UseMana(pm, ability+0x20, 5 /*ManaType.SpiritStone*/, 0, 0)` `` | 同上 |
| `CardPresentation/Core/WarpforgeAudio.cs:117` | `` `SoundManager.Play2D(cue, MixerType.Jingles /*=3*/)` `` | 同上 |
| `CardPresentation/Deck/DeckRuntime.cs:2194` | `` `DeckUtility__ValidateDeck(deck, out err, /*validateOwnership*/1, 0);` `` | 同上 |
| `CardPresentation/Battle/BattleDoors.cs:52` · `CombatAutoZoom.cs:306` | 同上 | 块外 · 只报 |
| `CardPresentation/Shell/LiveOpsEventWindow.cs:540/610` · `MenuWindowBase.cs:107` · `RankedTab.cs:329` · `TitleTab.cs:71` | 同上 | 块外 · 只报 |

* 🔑 **结论**：这一族**全仓只剩 1 处真缺陷，已在本件块内清掉**（`VfxMap.cs:42`）。
  其余 A2/C 都是**文字里引用标记 / 通配符**，不是「只换了行首」的残留。
* ⚠️ **抽查口径**：「搜了哪 6 种形状、各命中多少、处理了几条」= 上表；
  ⛔ 我**没有**逐条通读全仓 14 万行注释 —— 这是**抽查**，不是全量审计。

---

## 七、怎么验的（原文）

```text
# ① 本件专属 TMPDIR（⛔ 不与别人抢 /tmp/wfcheck/WFCheck.dll）
export TMPDIR="C:/Users/qjh36/AppData/Local/Temp/wf_h21"
cd /d/4/Unity/MyGame
D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_csc_rsp.py
# 再把两份 rsp 各复制一份、在 -out: 前插一行 -doc:（= 后来 A453 里 mkdoc 干的事）

# ② `/doc` 编译（改前 / 改后各一次；-utf8output 才有中文）
dotnet "C:/Program Files/dotnet/sdk/8.0.425/Roslyn/bincore/csc.dll" @…/wf_csc_doc.rsp -utf8output
  error CS                      : 0
  warning CS1570 全量            : 534 -> 231      （差 303 = 本件的全部）
  warning CS1570 · 本件白名单     : 303 -> 0        ✅
  warning CS1570 · 白名单外       : 231 -> 231      ⛔ 一条没动

# ③ 常规类型检查（= 工具/typecheck.sh 的原始跑法）—— 改前/改后逐字相同
TMPDIR=… bash d:/4/Unity/工具/typecheck.sh   →  运行时 0 / 编辑器 0  ✅
WF_DOC=1 TMPDIR=… bash …typecheck.sh         →  格式类 519 / 104   ✅

# ④ 自证「改动行全是注释行」——**两条独立证据**
(a) before/after **字节快照**逐行比（python difflib，78 个白名单文件）：
      changed files = 32 ; 非 `///` 行改动 = 0  ✅
(b) `git diff -U0 -- <四个目录>`：新增 596 / 删除 337 行，其中 `///` 是 351 / 150；
      非 `///` 的 227(+)/21(-) **逐条**在 before 快照 / `git show HEAD:` 里找得到
      ⇒ 全是**别人先前的未提交改动**（如 `FilterPanelModel.cs` 的字号常量、`DeckLibrary.cs`）
      ⇒ 认不出的行 = **0**  ✅
    ⚠️ 这一条踩了个坑：第一版判据没剥 `\r`，**CRLF 文件的行永远匹配不上**
      ⇒ 假报「184 行认不出」（§八·6）。

# ⑤ 行尾（python 二进制数）：32 个改动件，行尾类型 0 变化；只有 3 件行数按「插/删几行」变（§2·3）

# ⑥ 块级标签平衡扫描（每块数 summary/remarks/para 的开/关）—— 本件块内**零报**
```

* ⛔ **没跑 Unity** · ⛔ 没动 git（只读 `git diff` / `git show`）· ⛔ 没改两张正本 · ⛔ 没越白名单 ·
  ✅ 改了 `工具/typecheck.sh`（**A453 派单内**，且验证过默认档逐字不变）。
* 📌 **本件零自检**（按类型：纯 `///` 注释 + 工具脚本；用户口径「A 表清完再跑」）。

---

## 八、顺手发现（⛔ 只报不改）

1. 🔴 **`typecheck.sh` 的 `TMPDIR` 默认值下，并发跑会互相覆盖** —— 这不是新问题（`CLAUDE.md` 已写），
   但**本件实测更狠一层**：`gen_csc_rsp.py` 把 `TMPDIR` 拼成 `-out:`，
   而 `TMPDIR=/tmp/xxx` 交给 **Windows python** 时会解析成 **`D:\tmp\xxx`**（相对当前盘）——
   必须传 **Windows 形式**（`TMPDIR="C:/Users/…/Temp/wf_h21"`），否则 rsp 与 shell 的 `cygpath` **指向两个地方**。
   写进 §七 的命令里了。
2. 🔴 **「已知 XML 标签名白名单」不能照抄 —— `<link>` 不是 XML doc 标签**。
   本仓 `///` 里的 `<link>` / `<link=…>` **全是 TMP 富文本标记**（`Badges.cs:165/168` ·
   `Tooltip.cs:57/416/428` · `CardView.cs:3265/3289` · `CardIcons.cs:111/125` · `TmpFont.cs:179` ·
   `CardText.cs:230/231` 共 **12 处**）。H16 的白名单里**没有** `link`（它是对的），我第一版**加进去了**
   ⇒ 那 12 处**照旧产 CS1570**（`Tooltip.cs` 那 6 条、`Badges.cs` 2 条、`TmpFont.cs` 1 条就是这么漏的）。
   **DRY-RUN 时对照编译器才发现**（`扫描器说 0 处 / 编译器说 6 条`，对不上）。
   ⇒ **给后面几块（H18/H20 的 `Battle/`、H17 的 `Shell/`）**：白名单**只放真的 XML doc 标签**；
   本仓的富文本标记名（`sprite` `nobr` `link` `b` `i` `u` `sprite name=`）**大部分不是** —— ⚠️ 但 **`b`/`i`/`u`/`em`/`br`/`c`/`code`** 在本仓**确实被当格式标记用**（`<b>灵魂石</b>`、`<c>Board[…]</c>`、`<em>修饰词…</em>`），
   这几个**要留在白名单里**（它们良构、不产诊断）。
3. 🟡 **`AnimFXController.cs:220`（`Battle/`，H18 那块）有一处「正文明文的 `<i>`」**：
   ``（`modules.<i>`）`` —— 它良构性**不成立**（没闭合）⇒ 产 CS1570（`:222` 那条就是它）。
   同族还有 `Battle/BattleLogPanel.cs:311` 那个**真的正则字符串** `"<link=\"([^\"]*)\""`（那是代码，不是注释，别动）。
4. 🟡 **`typecheck.sh` 的 CS15xx 档会把「缺 XML 注释」CS1591 算成 3502 条** —— 已单列（§五）。
   若以后有人想「把 CS1591 清零」，那是**另一个决定**（要写注释，不是修 XML），⛔ 别混进这一族。
5. 🔴 **行尾守卫能抓到的一个静默坑（本件 DRY-RUN 抓到、没落盘）**：
   对 **CRLF** 文件做「行首锚点 + 行尾追加」时若用 `行 + '…'`，`\r` 会被推进**行中间**
   （`…。\r</summary>`）⇒ 下一行变成 `</summary>        static …`。
   **CR/LF 计数在这种损坏下是不变的** ⇒ 只数计数**抓不到**；
   判据要写成「**不许有 `\r` 落在行内**」。已写进两个脚本的断言。
6. 🟡 **「行尾」这个坑还有第三个变种**：拿 `git diff` / `git show` 的输出与**磁盘文件的行**做字符串比对时，
   git 输出恒为 LF，而 CRLF 文件的磁盘行**带 `\r`** ⇒ **永远匹配不上**（本件因此假报 184 行）。
   比之前**两边都 `.rstrip('\r')`**。
7. 🟡 **编译器诊断的「行号会漂」在本件同样成立**：一个裸 `<` 的诊断常报在**它后面 1～10 行**的闭合标记上
   （`EffectText.cs:181` 的裸 `<` → 报在 `:192/:193`）⇒ **认代码锚点，别认行号**。
8. ✅ **反证 H16 一条记账**：H16 §四·1 说「`Shell/` 之外 **54 文件 / 492 条**」——
   本件实测（`RuleEngine` + `Core` + `Deck` + `Net` 一共 **31 文件 / 303 条**）
   加上 `Battle/` 15 件 171 条 + `WarpforgeVFX/` 7 件 15 条 + `WarpforgeArena1/Runtime` 1 件 3 条 = **54 件 / 492 条** ✅ **逐条吻合**。
9. 🟡 **本件动过的两个 `Deck/` 文件与 D1/D2 那两笔报告同目录**（`D1_卡组脏标记与补名.md` 21:58 ·
   `D2_模态错误窗与DeckScene副本.md` 22:34，**都早于本件的快照 23:21**）⇒ 本件是**在它们改完的版本上**
   继续改的，⛔ 没有并发撞车（用 before 快照逐行验过：认不出的行 = 0）。
   ⚠️ **但若 D1/D2 之后再落一次盘，请回头核本件的转义与 `</summary>` 还在不在**（复核命令 = §七 ④a）。

---

## 九、还欠什么

* ⛔ **本件范围外的 231 条（运行时）+ 82 条（编辑器）**：一条没动，切块已在 §4·2 列好。
  同一套脚本（本件 `/tmp/wf_h21/fix21.py` 的 DRY-RUN 模式 + `struct21.py` 的「带断言逐处改」）可直接复用。
  🔴 **复用前先把 `KNOWN` 白名单按 §八·2 校一遍**（`link` 一定要**不在**里面）。
* ⚠️ **`WarpforgeVFX/Runtime/*`（15 条 / 7 件）现在没有主** —— 它既不在本件白名单，
  也不在 H16 §七 建议的四刀里。**请调度台指定归属**。
* ⚠️ **`RuleEngine/Editor/RuleEngineTest.cs`（9 条）**：`RuleEngine/**` 的字面范围含它，
  但 `Editor/**` 被派单整块划走 ⇒ 本件**没碰**。**要么归 H17/H19，要么明说它归 `RuleEngine` 那刀。**
* ⚠️ **本件改的 `工具/typecheck.sh` 是共用件** —— 后续若有别的写手也在改它，
  冲突点只有三处：文件头注释 · `DOC_MODE=` 那行 · `check()` 里的 doc 分支。
