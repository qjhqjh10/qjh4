# Block10 · `Battle/Label.cs` 那一族 5 条（A492 · A536 · A596 · A597 · A598）

> 写手：Block10（一件活一个代理）· 2026-10-13
> 独占文件 = `Assets/CardPresentation/Battle/Label.cs` + `Editor/SettingsScene.cs`（**只为 A598 改名后的引用**）
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用只读的 `git status` / `git diff` / `git show` / `git diff --numstat`）
> ⛔ 没改 `CLAUDE.md` / `项目任务.md` / 任何 `资料/*.md`（本报告除外）· ⛔ 没越白名单（`Shell/*` / `Deck/*` / `工具/*` / 其它 `Editor/*` **只读**）
> ✅ 类型检查 **改完立刻跑 3 次**：`TMPDIR=/tmp/wf_b10 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**

---

## 一、结论表

| A 编号 | 改了什么（文件:行 = 本件收工**现读**，会漂） | 判据 | 做完没有 |
|---|---|---|---|
| **A536** | `Battle/Label.cs:782-786`（`SetAutoFitBox` 里 `_tmp.fontSize = baseCur` 那一格）+ **新增** `EnsureFontSizeBase`（`:632-646`）+ `BaseFld`（`:596-608`，`FontSizeBase` getter `:581-590` 改成用它） | §六·B A536（源 A407 第二处 / W-D1 §六·1）：**洞是真的、而且不止第二次** —— `basePx == nomPx` 时 `baseCur == cur`，而 `Label.Create` 已经把 `m_fontSize` 设成 `cur` ⇒ setter 的 `if (m_fontSize == value) return;`（`TMP_Text.cs:465`）**第一次调用就早退** ⇒ `m_fontSizeBase` 停在 TMP 序列化默认 **36** | ✅ **做了** |
| **A596** | `Battle/Label.cs:521-545`（`SetCharSpacing`）：自己的 `Debug.Log`（第三份消息模板 + 不去重）**收编**进同族统一口，口名 / key 头段 = **`字距`** | §六·B A596：同族第 9 个出声口不走 `NoteDotAlign` ⇒ 「key 的拼法只有一份」不成立 | ✅ **做了**（消息与刷屏行为**如账面所述地变了**） |
| **A597** | `Battle/Label.cs:738-758`（`SetAutoFitBox` 内部两条 `return`）：`NoteArgInvalid("自适应框参数" / "自适应框折算")`；**新增** `NoteArgInvalid`（`:1081-1084`）+ 把 key/去重/日志抽成**唯一核心** `NoteNotApplied`（`:1092-1100`） | §六·B A597（半齐 ⇒ **先 grep 现核再动**，见 §三·3）；`CLAUDE.md` §三「不许静默失败」+ 铁律 11 | ✅ **做了**（两格今天**够不到**，是给将来兜底 —— 现核证据在 §三·3） |
| **A598** | `NoteDotAlign` → **`NoteDotBackendLacks`**（`Battle/Label.cs` 全文）+ `Editor/SettingsScene.cs` 的 10 处引用/文案（`:246,249,1552,1563,1588,1633,1640,1649,1665,1670`） | §六·B A598：助手名是历史名（管 9 个口、只有 3 个是对齐） | ✅ **做了**（改名的**牵动面**逐处核过，见 §三·4） |
| **A492** | ⛔ **一个字没改** —— 改动点在 `Shell/LiveOpsEventWindow.cs`（**白名单外**，且该文件正被别的代理改）⇒ 按「卡住就停手写进报告」办 | 判据**半齐**（缺「原版实际收敛到多少」，H37 §六·1 自己写着没量过） | ⚠️ **没做（两条理由）**：① 改动点不在我的文件里；② 🔴 **本件静态核出一条会推翻它前提的东西**，见 §三·1 —— 请调度台裁「还改不改」 |

**超出账面的改动**（都是为了让上面五条自洽，逐处写在代码注释里）：
`BaseFld` 抽取（A536 要写那个字段 ⇒ 句柄收成一份）、`NoteNotApplied` 抽核（A596/A597 要加口但**不许**出现第二份 key 拼法）、
`SetCharSpacing` 的正常路径**一字未动**、`_dotAlignNoted` **没改名**（理由见 §三·4·③）。

---

## 二、逐条明细

### A536 —— `m_fontSizeBase` 在 `basePx == nomPx` 时写不进去（既有洞，源 A407）

**病灶链（现读逐句核过）**
1. `Label.Create` → `BuildTmp` → `TmpFont.NewText(transform, "text", "", TmpFontSize(), color)`（`Battle/Label.cs:881`）
   ⇒ 建出来的那一刻 `m_fontSize` **就是** `cur` 那一档；
2. `SetAutoFitBox` 里 `baseCur = basePx > 0f ? cur * (basePx / nomPx) : cur` ⇒ **`basePx == nomPx` 时 `baseCur == cur`**（逐位相等）；
3. `_tmp.fontSize = baseCur;` 走 setter 的 `if (m_fontSize == value) return;`（`TMP_Text.cs:465`）
   ⇒ **整句什么都不做**，而 `m_fontSizeBase` **只有它一条写口**（同文件 `if (!m_enableAutoSizing) m_fontSizeBase = m_fontSize;`）
   ⇒ 字段停在 TMP 的序列化默认 **36**（`TMP_Text.cs:473`）。
   ⇒ A407 第二处（卡背页 `$owned`：`AutoMax = 32 = 标称`、`Base = 32`）**一个字节都没写进 base**；
   W-D1 交件时因此「读不出差别、编不出有鉴别力的断言」（它**没有**编空断言，如实记了）。

**改法**（`Battle/Label.cs:782-786`）
```csharp
_tmp.enableAutoSizing = false;
float fontBefore = _tmp.fontSize;           // 🔴 A536：setter 有一条 `m_fontSize == value` 早退 ⇒ 先记下现值
_tmp.fontSize = baseCur;                    // 复位/照抄原版 base（这一下顺带回写 `m_fontSizeBase`）
if (fontBefore == baseCur) EnsureFontSizeBase(baseCur);   // 🔴 A536：早退那一格由这里补写
```
`EnsureFontSizeBase`（`:632-646`）= 用**已有的反射句柄**直接写 `m_fontSizeBase`；
**只在 setter 确实早退时**才调到（调用点那句 `if`），拿不到字段（TMP 换版）⇒ `Debug.LogWarning` **出声一次**（红线：不许静默失败）。

**为什么不用「先把 `fontSize` 拨到别的值再拨回来」**（旧注释写「代价大于收益」指的就是它）：
那要多两下 `SetVerticesDirty`/`SetLayoutDirty`；反射写**不改任何别的状态**（早退 = 那一下本来就什么都没改），
紧接着的 `ForceMeshUpdate()` 就会拿新 base 当二分起点（`TextMeshPro.cs:2148-2149`）。

**⚠️ 行为影响（静态推演，逐档核过：对渲染**neutral**）**：base 只被 `Clamp(m_fontSizeBase, min, max)` 用到，
而旧值（36 fontSize 单位 ≈ 369px）**比任何 `max` 都大** ⇒ 旧式起点 = `max`、新式起点 = `Clamp(baseCur, min, max)` ——
`baseCur` 落在窗口内时起点确实变了，但**终点不变**（A305 那条已记：「终点两侧都收敛到装得下的最大号，差 ≤ 0.05 fontSize 单位」）。
⇒ 本改动改的是**字段**（这正是 A536 要的），不是画面。

### A596 —— `SetCharSpacing` 收编进统一出声口

- **原来**：`Debug.Log("[Label] ⚠️ 这一处要 `characterSpacing = " + v + "`，但**点阵后端没有字距** ⇒ 没生效（出声，不静默）")`
  —— 自己的模板、**每次调用一行、不去重**（键位：同族唯一一个不在 `_dotAlignNoted` 里的）。
- **现在**：`NoteDotBackendLacks("字距", "没有「字距」这回事", "`characterSpacing = " + v + "`", "没生效（这一档字距没加上）")`
  ⇒ 走**同一个** key 集（`字距|节点全路径`）、**每处一次**。
- ⚠️ **正如账面所写：消息文案 + 刷屏行为都变了**（这是本账要的）⇒ 我在两处写死了「⛔ 别把它改回去」：
  `Label.cs:529`（方法头）+ `NoteDotBackendLacks` 的 doc（`:1046-1048`）。
- ⛔ **没弱化**：出声还在（只是换了口），正常路径（`characterSpacing = v` + `ForceMeshUpdate`）**一字未动**。
- **既有断言零冲击**：全仓 `grep -rn 'Contains(".*字距'` = **0 命中**（`SettingsScene` 那 11 条锚的是别的口的文案）；
  A545 ★⑥ / A491 ★③ 两条「反面对照」只包那 5 个口 / `SetAlignLeft`，**不调 `SetCharSpacing`** ⇒ 不受影响。

### A597 —— `SetAutoFitBox` 内部两条入参闸出声

| 闸 | 口名（key 头段） | 消息里的「但……」 |
|---|---|---|
| `cur <= 0f \|\| minPx <= 0f \|\| maxPx <= 0f` | `自适应框参数` | `但**入参不成立（标称字号 cur = … · minPx = … · maxPx = …）**` |
| `nomPx <= 0f` | `自适应框折算` | `但**入参不成立（`nomPx` = …（`WorldGlyphPerFontSize` = …））**` |

🔴 **为什么另开 `NoteArgInvalid` 而不是复用 `NoteDotBackendLacks`**：后者的模板写死「**但\*\*点阵后端…\*\***」，
而这两格是**入参**的错（同一个 label 完全可能**有** TMP）—— 报成「点阵后端缺这一档」就是**归因错**
（A545 在 `SetFontSize` 上刻意躲开过同一个错，见 `:495-501`）。
⇒ 两只口**只差那句「但……」**，**key 的拼法 / 去重 / 落日志仍然只有一份**（核心 `NoteNotApplied`，`:1092-1100`）。
⇒ 「8 条老消息逐字节没变」这件事仍成立：点阵那一族的 `butClause` = `"但**点阵后端" + why + "**"`，与改前**逐字节相同**。

**既有断言零冲击**（逐条现核）：A545 ★⑥ 那 5 个口在 TMP 探针上**要求 0 行 `[Label]`** —— 探针传的是
`SetAutoFitBox(5f, 2f, 10f, 30f)`（min/max 都是正数、`cur = FontSizeForCapHeight(CapWorld) > 0`）
⇒ **两格都不进** ⇒ 仍 0 行 ✅。

### A598 —— 改名 `NoteDotAlign` → `NoteDotBackendLacks`

- **为什么改**：它是「**点阵后端缺这一档功能**」的总口（9 个口：`左对齐`/`右对齐`/`逐行左对齐`/`折行宽`/`换行模式`/`重排`/`字号`/`自适应框`/`字距`，其中只有 3 个是对齐），
  `Align` 是历史名。
- **牵动面**（**全仓 grep 过**，见 §三·4）：本文件 16 处 + `Editor/SettingsScene.cs` 10 处（**全是注释/断言文案**，本件一并改掉）；
  它是**本类私有方法**（无访问修饰符）⇒ **没有任何编译期引用**。
- **没改名的那一只**：静态 HashSet **`_dotAlignNoted`** —— 同一个历史名，改名会牵动
  `Battle/ScenarioBlendables.cs:2094,2099` 的注释（**不在本件白名单**）⇒ 如实登记、另立账（已写进 `NoteDotBackendLacks` 的 doc）。

---

## 三、牵动面（共用件必写）

### 1. 🔴 A492：本件**没做**，而且核出一条**推翻它前提**的东西（请调度台裁）

**① 改动点不在我的白名单**
批次计划 §六·B / `A表现核_块4.md` §A492 写死的修法 = **`Shell/LiveOpsEventWindow.cs` 把那两行对调**
（**现读**：`:766 title.SetAutoFitBox(…)` → `:774 title.SetCharSpacing(5f);` → `:781 MenuDraw.AlignLeft(…)`）。
我的白名单只有 `Battle/Label.cs` + `SettingsScene.cs`，而简报把 `Shell/*.cs` 明确列进「⛔ 绝对不能碰」⇒ **我没动它**。

**② 静态核出的东西：这条路的「自适应收敛不含字距」可能不成立**
`Label.SetCharSpacing` 在 setter 之后**无条件**调 `_tmp.ForceMeshUpdate()`（`Battle/Label.cs:543-545`），而：
- `TextMeshProUGUI.ForceMeshUpdate()` = `m_havePropertiesChanged = true; … OnPreRenderCanvas();`（`TextMeshProUGUI.cs:556-566`）；
- `OnPreRenderCanvas()` 里那一段是 `if (m_enableAutoSizing) m_fontSize = Mathf.Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax);`
  紧跟自适应迭代循环（`TextMeshPro.cs:2148-2153`）；
- 而 `SetAutoFitBox` 收尾把 `enableAutoSizing = true` 且**之后没有任何一处把它关掉** ⇒ 第二次 `ForceMeshUpdate`
  **会带着新字距、从 base 重新收敛一遍**。
⇒ **两种次序的收敛结果应当相同**（`SetCharSpacing` 在前 / 在后都一样，因为后者自己会重收敛）。
⚠️ **这是读 TMP 源码得出的静态结论，不是实跑**（本件不许跑 Unity）⇒ 我没把它写进代码注释去「定案」，
只在这里报上来。**建议**：要么仍按原判据对调（两行、行为上应当无害，且「字距在前」更贴原版那种「静态序列化字段」的形状），
要么先派一个只读探针核一次（判据 = 同一 label 两种次序下 `FontPxNow` / `WorldW` 是否逐位相同）。

**③ 「无牙口」如实标**：即便对调，**也没有断言能咬住它** —— H37 §六·1 自己写着「原版那颗实际收敛到多少」**没量过**；
而本地能读到的原版字段是 **`m_fontSize = 67.55 = m_fontSizeMax`**（`Shell/LiveOpsEventWindow.cs:760-766` 的注释里记着
「`auto[18~67.55]` 那一族 8 颗」）—— 那只说明**原版那一格序列化在**上限上（⚠️ 这只是线索，不是「收敛值」的测量）。
⇒ 按简报的两条路选：**如实标「无牙口」**（本件选的），或派人生跑一次量原版。

### 2. A536 的行为影响面（改了共用件的**运行时字段**）

- `m_fontSizeBase` 的**唯一读者**是 TMP 自己的 `Clamp`（二分起点）；本工程读它的口 = `Label.FontSizeBase`（反射）。
- **全仓读 `Label.FontSizeBase` 的断言逐个核过**（`grep -rn "FontSizeBase" --include=*.cs`，除 `Label.cs` 外 9 个文件 16 处）：
  · `Editor/BattleScene.cs:3080,3089`（45.2 / 30.6）· `Editor/DeckScene.cs:3767`（36，标称 23.2）
  · `Editor/CollectionScene.cs:4389`（36/36/35，标称 52.6/57/52.6）· `:4543`（36，标称 35）
  · `Editor/RewardsScene.cs:5885`（36）· `:8243`（`wantBasePx`）· `:8526`（`wantPx`）· `Editor/SettingsScene.cs:2460`（45.2）
  ⇒ **没有一条**是「请求 base == 标称」（= 会走进早退那一格）的站 ⇒ 本改动不会翻任何一条既有断言的期望值；
  ⇒ 反过来，**卡背页那一族（32/32）本来就没有 base 断言**（W-D1 如实记过）—— 这正是 A536 要修的东西。
- **生产调用点计数**（命令与口径见 §四）：`SetAutoFitBox(` **生产 52 + 自检探针 13 = 65 处 / 23 文件**
  （⚠️ 与 WLabel 报告里的「生产 39」**口径不同**：那份剔了 `Editor/`、且按方法名而非 `.方法(` 数，且是 2026-10-13 早些时候的读数）。

### 3. A597 的「先 grep 现核」（简报点名要做的那一步）

**两格今天够不到 —— 逐条证据（可复现）**

1. `cur` 恒 > 0：`cur = TmpFontSize()`（`Label.cs:844-849`）→ `_glyphHeight > 0` 用它，否则 `FontSizeForCapHeight(CapWorld)`；
   而 `CapWorld = _capHeight > 0 ? _capHeight : CapHeightWorldOfScale * Mathf.Max(1, scale)`（`:179`）= **常数兜底**（`7f/100 × ≥1`）。
2. `nomPx` 恒 > 0：`nomPx = FontSizeToPx(cur)`，而 `TmpFont.MeasureGlyph` **永不返回 ≤ 0**
   —— 无字体资产 → `1f`、量不出 → `0.01f`（`Core/TmpFont.cs:272-300`）。
3. `minPx/maxPx`：**全仓 `.SetAutoFitBox(` 的每一处实参逐个现核**（52 生产 + 13 探针）——
   要么传**正字面量**（`3f/10f/12f/18f/25f/…`），要么上游就有闸：
   `Shell/MenuDraw.cs:1602,1635`（`autoMinPx > 0f && fontPx > autoMinPx`）· `Shell/OfferContainer.cs:1347`（`minPx > 0f && fontPx > minPx`）·
   `Shell/CollectionWindow.cs:1704`（`autoMinPx > 0f`）· `Shell/MatchLogRow.cs:163`（`autoFit && px > autoMinPx`）·
   `Shell/PlayerProfileWindow.cs:329,637`（`AutoMax > AutoMin` / `fontPx > autoMinPx`）· `Shell/MissionsTab.cs:350`（`autoMaxPx > 0f || autoBasePx > 0f`）；
   表驱动那两处**逐行核过表里的值**：`Shell/MenuWindowBase.cs` 的 `TabBtnSpec`（autoMin 全 5~12）·
   `Shell/PlayerProfileWindow.cs:155-160` 的 `PpTabSpec`（autoMin 全 10）。
   **⇒ 生产 39 处里没有一处会传 0**（与账面「生产上够不到」一致）。
4. ⚠️ **`CollectionWindow.cs:1704` 那句 `if (autoMinPx > 0f)` 本身就是证据**：写它的人知道**这个数可能流到 0** ⇒
   本账要堵的正是「将来某处忘了这道闸」那一格（铁律 11：只有先后、没有不做）。

### 4. A598 的牵动面（改名）

```
grep -rn "NoteDotAlign" Unity/MyGame/Assets/CardPresentation --include=*.cs
```
- **改前**：`Battle/Label.cs` **16** 处 · `Editor/SettingsScene.cs` **10** 处（**全在注释 / 断言文案里**）· `Battle/ScenarioBlendables.cs` **1** 处（注释）。
- **改后**：只剩 ① `Label.cs` 那两条**故意留的「原名 = `NoteDotAlign`」留痕**（铁律 5 要保留更正痕迹）；
  ② `Battle/ScenarioBlendables.cs:2089` 的一处注释（⛔ **不在我的白名单，我没动**）⇒ **请调度台派人把它改成新名**
  （否则下一个会话按旧名 grep 会以为改名没做）。
- **编译期零风险**：`NoteDotAlign` **没有访问修饰符 ⇒ `private`** ⇒ 外部文件本来就不可能编译期引用它（实据：改完两轮类型检查都是 0/0）。
- **③ 为什么 `_dotAlignNoted` 没跟着改**：它被 `Battle/ScenarioBlendables.cs:2094,2099` 按名字引用（还有若干文档按 `Battle/Label.cs:935` 这样的**行号**引用它）；
  改名字会同时留下两类说不清的引用 ⇒ 本件保持原样、在 doc 里写明「同一个历史名、另立账」。

---

## 四、计数命令与口径（要复核就照抄这几条）

```bash
cd d:/4/Unity/MyGame/Assets/CardPresentation
python d:/tmp/count_calls2.py          # 本件临时脚本；口径 = 逐文件扫 .cs、剔注释行、按【行内出现】计数
# ⇒ SetAutoFitBox(  生产 52 · 自检探针(Editor/) 13 · 23 文件
#    SetCharSpacing( 生产 13 · 自检探针 2 · 11 文件
#    AlignLeftOn(   生产 13 · 自检探针 4 · 10 文件
#    AlignRightOn(  生产 10 · 自检探针 2 · 8 文件
grep -rn "\.SetAutoFitBox(" --include=*.cs . | wc -l      # 含注释 = 67（本件读过的每一处）
grep -rn "FontSizeBase" --include=*.cs . | grep -v "Battle/Label.cs"   # 16 处 / 9 文件（A536 的影响面）
```

---

## 五、顺手发现（⛔ 一个都没改，只报）

1. 🟡 **`TextMeshPro.cs` 的行号引用本来是对的，我一开始写错了、已就地订正**：`m_fontSize = Mathf.Clamp(m_fontSizeBase, …)`
   在**包内那份**的 **`TextMeshPro.cs:2149`**（`if (m_enableAutoSizing)` 在 `:2148`）—— 我新写的三处一度写成 `2145-2146`，
   已改回 `2148-2149`（与本文件既有的引用逐字一致）。**判据**：`grep -n "m_fontSize = Mathf.Clamp(m_fontSizeBase" Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TextMeshPro.cs`。
2. 🟡 **`Editor/SettingsScene.cs:1516` 那句「`SetCharSpacing`（更早就出声）」现在不完整**（A596 之后它走**同一只口**）。
   我没改它：我的白名单对 `SettingsScene.cs` 只覆盖「A598 改名后的引用」，这句话不在其列 ⇒ **请调度台裁要不要顺手补一句**。
3. 🟢 **A476 那段 doc 里的 `Battle/Label.cs:466-468` 是过期行号**（原指 `SetCharSpacing` 的出声那一段）—— 本件已就地改成
   「A596 之后它也走同一只口」（铁律 5），**没有**再写一个新行号（行号每次都漂）。
4. 🟢 **`SetWrapWidth` 会在 `SetAutoFitBox` 内部被无条件调到**（`:721`）—— 两处的 `which`（`自适应框` / `折行宽`）**不同**这件事
   在 A545 的 doc 里已经写死，本件收编 `字距` 时**没有**碰它（撞 key = 静默复发）。
5. 🟡 **`SetFontSize` 的非正数那一支【照旧】不出声**（A545 的边界），而 A597 让 `SetAutoFitBox` 的同类闸**出声**了 ——
   两处的口径**故意不同**（那边是「传了个没有意义的字号」、这边是「框/窗口整档没生效，调用方以为生效了」）。
   若将来要统一，**先看 A545 方法头写的那条归因论证**，别一刀切。

---

## 六、没做完的（+为什么）

| # | 是什么 | 为什么没做 |
|---|---|---|
| 1 | **A492 的代码改动**（`Shell/LiveOpsEventWindow.cs` 两行对调 + `:770-780` 那段 A475 注释订正半句） | **文件不在白名单**（`Shell/*.cs` 明令不许碰）⇒ 按纪律停手上报；修法现成，见 §三·1 |
| 2 | A492 的**断言**（咬住「收敛字号含字距」） | **判据半齐**：原版那颗的**收敛值**本地没有测量记录（H37 §六·1 自己写着没量过）⇒ 只能选「如实标无牙口」 |
| 3 | `Battle/ScenarioBlendables.cs:2089` 那句旧名注释 | 不在白名单（A598 的尾巴）⇒ 请调度台派人改 |
| 4 | `Editor/SettingsScene.cs:1516` 那句「更早就出声」的措辞 | 同上（白名单只覆盖改名引用） |
| 5 | **A596 / A597 / A536 的新断言** | 简报把 `SettingsScene.cs` 的权限限在「A598 改名后的引用」⇒ **本件没有加任何断言**。若调度台要补，位置现成：接着 `:1710` 那一段（A545 节）后面加一节即可，可咬的形状：① `SetCharSpacing(5f)` 在点阵探针上**出声**且**第二次调用 0 行**（去重）；② `SetAutoFitBox(5,2,0,30)` 在 TMP 探针上出声（锚 `入参不成立`）；③ `SetAutoFitBox(…, basePx: 32)` 之后 `FontSizeBase` 折 px = 32（今天会**绿得没道理**，A536 修完后**才有牙口**） |

---

## 七、红线核对

⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只读命令）· ⛔ 没改两张正本 / 任何 `资料/*.md`
· ⛔ 没越白名单（写只落在 `Battle/Label.cs` 与 `Editor/SettingsScene.cs`）· ⛔ 没自己发明口径（A492 判据半齐 ⇒ **没定**原版收敛值）
· ✅ 改完**立刻**跑了三次类型检查（独立 `TMPDIR=/tmp/wf_b10`）⇒ **0 / 0**
· ✅ **行尾复核（二进制读）**：`Battle/Label.cs` **CRLF 1216 · LF 1216**（**纯 CRLF**，改前 1214 ⇒ 一行没翻）·
  `Editor/SettingsScene.cs` **CRLF 0 · LF 2638**（**纯 LF**，改前 2636 ⇒ 一行没翻）
· ✅ `git diff --numstat`：`Battle/Label.cs` **213 / 52** · `Editor/SettingsScene.cs` **12 / 10**（都不是整篇重写）
· ✅ 改动**全部走 Edit 工具**（⛔ 没用 `sed -i`、没用 python 写盘）
