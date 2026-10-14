# `G` · 卡表 P0 —— `A1763`（代码层）+ `A1945`（2026-10-14 · 子代理写手报告）

> **一件活、一个写手**：`资料/普查产出_第三十会话/卡表块_施工设计_1014.md` 的 **P0**。
> 改动**只有一个文件**：`Unity/MyGame/Assets/CardPresentation/Core/CardIcons.cs`（**`+357/-1`**，LF 未翻）。
> ⛔ 没碰 `cards_engine.json` / `cardface_fixes.json` / `card_ids.json` / `gen_cards_engine.py`（**零指纹扰动**，见 §六）。
> ⛔ 没跑 Unity 批处理（`CardBaseDemo.Run` / `IconSetup.Run` 留给主对话在同步点跑）。

---

## 一、结论一句话

`CardIcons.Rewrite` 现在**能渲染原版四支记号**（`((…))` / `$$…$$` / `[[…]]` / `(N)`，**按原版顺序**），
并且**官方模板文本不再经过我们自造的那套记号层**（= `A1945` 的「退场」落地）。
**今天零行为变化**（卡池 1126 张的每个字段扫过，`[[`/`((`/`$$` **各 0 处** ⇒ 一条也走不到新路），
等 P3 把官方文本灌进 `descZh` 就自动生效。

---

## 二、反编译判据（**逐处现读确认过**）

| 项 | 值 |
|---|---|
| **主判据** | `d:/2/tools/decomp_full/SupportMethods__ModifyLocalization.c`（**497 行**）—— ⚠️ **文件名与内容现读核对过**：里面就是 `longlong SupportMethods__ModifyLocalization(longlong param_1)`（本仓踩过 6 例「文件名与内容对不上」） |
| 四支行号（**现读**，与施工设计给的 `:75-186`/`:188-319`/`:320-467`/`:469-483` **一致**，末支实为 `:469-487`） | ① ①`((Key,a,b))` `:75-186` · ② `$$ID$$` `:188-319` · ③ `[[枚举名]]` `:320-467` · ④ `(N)` `:469-487` |
| ③ 的展开函数 | `d:/2/tools/decomp_full/GameStaticData__TraitNameToString.c`（119 行，**现读**）＋ `CardTraitCollection__GetTraitTextSprite.c`（6 行，**现读**） |

### `_DAT_` 字面量（**逐条现读解出来的**，地址 → 值）

解码路 = `资料/战斗规则与数值_出处.md` §三（`RVA = 地址 − 0x180000000` → PE 节表 → 文件偏移 → `GameAssembly.dll`）。
⚠️ 反编译里偏移**常写成十进制**（`+ 300` 就是 `0x12C`），`grep 0x12c` 会漏。

| 地址 | 值 | 用在哪 |
|---|---|---|
| `0x18426a500` / `0x184270f48` | `((` / `))` | ① 的定界符 |
| `0x1842b3a48` | 正则 `\(\((.*?)\)\)` | ① |
| `0x184237f88` | `Modular_Description/` | ① 词条前缀 |
| `0x184265d10` / `0x184272af0` | `{0}` / `{1}` | ① 代参 |
| `0x1842552f8` | `$$` | ② 定界符 |
| `0x1842b3848` | 正则 `\$\$(.*?)\$\$` | ② |
| `0x1842cde40` | `Card_Name/` | ② 词条前缀 |
| `0x18428b900` | `PlayerDataManager not found` | ② 里「先查玩家收藏」那一路取不到玩家数据时的出声文字 |
| `0x1842b06f0` / `0x1842b9628` | `[[` / `]]` | ③ 定界符 |
| `0x1842be020` | `MainMenu/General/` | ③ 的 `melee`/`ranged`/`reanimate` 特例（**本笔不接**，见 §三③） |
| `0x1842ce340` | `Card_Trait/` | ③ 词条前缀 |
| `0x184238f80` | `<sprite name=Atlas_trait_icon_` | ③ 的图标签前缀 |
| `0x184237e80` / `0x1842d26d8` / `0x1842cf1e8` | `<nobr>` / `<b>` / `</b></nobr>` | ③ 的加粗档 |
| `0x1842cf8e8` | `</nobr>` | ③ 的不加粗档 |
| `0x184237880` / `0x18423b778` / `0x1842cf7e8` | `<link=` / `>` / `</link>` | ③ 最外那一层 |
| `0x184269e20` / `0x18426f0c8` | `(` / `)` | ④ |
| `0x1842c79a0` | `" <sprite name=Mana"`（**带前导空格**） | ④ |

---

## 三、逐支交代（**四支各一条**）

### ① `((Key,a,b))` → `Modular_Description/<Key>` —— ✅ **做完了（代码层）**

- **判据**：`SupportMethods__ModifyLocalization.c:75-186`（现读）。原版步骤：正则 `\(\((.*?)\)\)` 逐条命中 →
  把命中串里的 `((`/`))` 各剥一次 → `String.Split(串, 0x2c)`（= `,`）→ 词条键 = `Modular_Description/` + 第 0 段
  → `GetTermTranslation` → 第 1 段非空则 `Replace("{0}", 第1段)`、第 2 段非空则 `Replace("{1}", 第2段)`
  → 把**命中整串**换成这个词条。
- **改了哪几行**：新增 `RxMarkModular`（`:478-480`）+ `RewriteMarkModular`（`:559-580`）。
  ⚠️ **不 Trim 任何一段**（原版就没 Trim）——`((Deal_x_damage, 3))` 那种带空格的会把空格代进去，照抄。
- **没查清**：无（这一支的反编译是全四支里最直白的一段）。
- **数据缺口（⛔ 不是本层 bug，如实报）**：`Modular_Description/*` 官方有 **340** 条，
  `Card_Description` 里用到 **86** 种键 / **508** 处（现数），而它在 `Resources/` 下**一条都没有**
  （`Resources/official_names.json` 只有 `Avatars/Titles/Achievements/Cardbacks/Event_Description/PrebuiltDecks`
  六族 = **1327** 条；`Loc` 手写表里也没有）⇒ **本支今天会全部「原样留着 + 出声」**。
  施工设计 §六·3 已经把它记成 `A1763` 的数据缺口（并入 `A1982`）。

### ② `$$ID$$` → `Card_Name/<ID>` —— ✅ **做完了（代码层）**

- **判据**：`:188-319`（现读）。正则 `\$\$(.*?)\$\$` → 命中串里的 `$$` 各剥一次 → 键 = `Card_Name/` + id
  → `GetTermTranslation` → **再查 `PlayerDataManager.allCardCollection` 里有没有这张卡**，有就用
  `RawCardScript.GetLocalizedCardName(那张卡)` **覆盖**词条；玩家数据取不到时印 `PlayerDataManager not found`。
- **改了哪几行**：新增 `RxMarkCardRef`（`:481-483`）+ `RewriteMarkCardRefs`（`:582-608`）。
- 🔴 **如实记的偏离**：**没有做「玩家收藏里那张卡的显示名覆盖词条」那一路** —— 卡面渲染这条路**没有那个上下文**，
  原版那一支也自带「取不到就 `CustomDebug.Log("PlayerDataManager not found")`」的兜底。
  这一条写在方法 doc 里，⛔ 不静默。
- **数据缺口**：卡池正文在用的 id **116** 种 / **151** 处，其中既有 `AM5`/`TL37` 这种 `前缀+数字`，
  也有 `27a4b748a7bf358499f038aa09895ead` 这种 **32 位 GUID**；`Card_Name/*` 官方有 **1065** 条，
  而 `Resources/` 下**同样没有**（`Loc` 手写表里那 20 来条是我们自造的 `Ember Warlord` 那类）⇒ 本支今天也全部「原样留着 + 出声」。
- **没查清**：GUID 型 id 与「我们的卡 id」的关系 —— 属 `A970`/`A1800`（P1 的活，⛔ 不在本笔）。

### ③ `[[枚举名]]` → `TraitNameToString` —— ✅ **做完了（代码层）**，且**今天就能出画面**（47/55 种查得到词条）

- **判据**：`:320-467`（现读）+ `GameStaticData__TraitNameToString.c` 全 119 行 + `CardTraitCollection__GetTraitTextSprite.c` 全 6 行。
  原版：`Enum.GetValues(<DefinedTrait>)` **遍历枚举成员**，逐个拼 `[[成员名]]`、命中就整串换成
  `TraitNameToString(成员, true)` ⇒ **只认枚举名，认不出的 `[[x]]` 原样留着**（本层照此，认不出就留 + 出声）。
- **整项形状（三档 + 最外一层）**：
  · 有词条 · 加粗档（**调用点传的就是这一档**）⇒ `<nobr>{图}<b>{词}</b></nobr>`
  · 有词条 · 不加粗 ⇒ `<nobr>{图}{词}</nobr>` · 词条为空 ⇒ **只剩枚举名**（`Enum.ToString()` 那支）
  · 三种都被外面**再包一层、且只包这一层** `<link={枚举名}>…</link>`
- **改了哪几行**：`RxMarkTrait`（`:484-486`）+ `RewriteMarkTraits`（`:610-671`）+ `TraitTermOf`/`Norm`/`SpriteTagOf`（`:673-695`）。
- 🔴 **图名不需要映射表**：原版拼的就是 `<sprite name=Atlas_trait_icon_{枚举名}>`；卡池正文在用的
  **55 种枚举名逐种核过**，我们 dump 的原版图集（`数据/游戏数据/trait_textsprites.json`，87 个字符名）
  **每一种都有**对应字形；而 `Editor/IconSetup.cs:23-25` 已把 `Atlas_*` 全名与短名**都注册**进
  `Resources/Fonts/Warpforge Trait TextSprites.asset`。⇒ 官方值里的 `<sprite name=Atlas_trait_icon_faith>`
  那一串**照抄就能画**。
- ⚠️ **唯一的改写**：官方词条值里 `<sprite>` 是**不带引号**的，我们**补引号**（`QuoteSprites`）——
  口径出处 `Core/CardText.cs:275`（「两条路写法要一致」）。这是**有意偏离原版写法**，已写在 `RxSpriteUnquoted` 的 doc 里。
- ⚠️ **`melee` / `ranged` / `reanimate` 那三个词【没接】**：原版确实有一条特例（`:322-357`：
  数组 = `melee`/`ranged`/`reanimate`、前缀 `MainMenu/General/`、形状 `<nobr><b>{词条}</b></nobr>`），
  但 **`资料/任务_卡组卡牌战斗.md:294` 的 ⑤（2026-10-14 查实）明确写了不接**：这三条双括号形态
  **只出现在 `Missions/*` 任务文案**里（4/4/2 处），`Card_Description`/`Modular_Description`/`Card_Name`/`Card_Abilities` **全 0**
  ⇒ 接了也永远没有输入。本层**按口径不写**，并在注释里记了为什么。
- 🔴 **词条缺口（现读，两张表 + exact/归一化两条路都算）**：55 种里 **47 种 / 1194 处**查得到
  （`Loc` 手写表 `Card_Trait/*` 共 **62** 条），**8 种 / 56 处查不到**：
  `markOfChaos` 18 · `frenzied` 12 · `concussive` 8 · `markOfSlaanesh` 8 · `rage` 6 ·
  `markOfKhorne` 2 · `markOfNurgle` 1 · `markOfTzeentch` 1。
  **这 8 个名字在官方表里全都有**（`Card_Trait/<名>`），只是我们 `Loc` 用的是**规范键**
  （`destroyer`/`penitence`/`darkpact`/`concussion`）⇒ **是数据侧的改名映射缺口**。
  ⇒ 那 56 处今天落到**原版自己的兜底**（只印枚举名 + 出声）。
  ⛔ **本层【不】私自加别名表**，理由两条：① 会把缺口藏起来；② `markOfSlaanesh` 的官方词是
  `Dark Pact of Excess`，与 `darkpact`（`Dark Pact`）**不是同一个词** ⇒ 硬映射就是**印错的名字**（工程红线）。
- **没查清**：`Loc` 那 62 条 `Card_Trait/*` 里有多少条**本身就是「暂缓换回官方值」**（`Core/Loc.cs` 文件头说
  有一族 33 条暂缓）⇒ 拿它当 `A1762`/`A1763` 数据层的输入时**要先换回官方值**。本笔不裁。

### ④ `(N)` → `" <sprite name=ManaN>"` —— ✅ **做完了（代码层）**，但**今天 0 处输入**、且**画不出来**

- **判据**：`:469-487`（现读）：`for (N = 0; N < 9; N++)`，命中 `(N)` 就换成 `Concat(" <sprite name=Mana", N, ">")`。
  **那个前导空格是原版字符串本身带的**（`0x1842c79a0` = `" <sprite name=Mana"`）⇒ 照抄。
- **改了哪几行**：`RewriteMarkMana`（`:697-724`）。
- 🔴 **两条现数**：① **全表 9441 条词条里 `(\d)` 命中 0**（含 `Card_Description` 1098 条）⇒ 这一支**永远没有输入**；
  ② 我们的 sprite asset 里**没有 `Mana0..8`**（原版那份 dump 的 87 个字符名里没有 `Mana*`，那是原版**另一个** asset 的东西）。
  ⇒ 本支带一道 **「图不在就原样留着 + 出声」** 的闸 —— ⛔ **不盲换**（换出一个渲染不出来的标签 = 静默失败）。
- **没查清**：原版 `ManaN` 那张图在哪个 sprite asset 里（**没查**，因为这一支今天没有输入）。

---

## 四、`A1945`（「方括号那一层退场」）—— 怎么落的，以及**为什么不是删代码**

**落地形态 = `Rewrite` 里按【文本世代】分流**：

```
if (HasOfficialMarks(text)) return RewriteMarks(text, field == DescZhField);   // 官方文本 ⇒ 原版四支
if (string.IsNullOrEmpty(cardId)) return text;
LoadPlan(); ...                                                              // 自造记号那一代 ⇒ 原账旧路
```

**判据**：正文带 `[[` / `((` / `$$` ⇒ 那就是 `ModifyLocalization` 四支的**输入形状** ⇒ 交给新层；
它**不再经过** `card_icon_plan`（那份表的 token `[Attack]`/`①`/`路标石。` 全是**我们自造记号**，
两个世代混起来是施工设计 §五·11 点名过的坑）。

🔴 **为什么不「把那一支删掉」** —— 这一条要请调度台确认（我把判据摆全，⛔ 没自己拍）：

| 做法 | 结果 |
|---|---|
| **删掉**方括号那一支（字面「整体退场」） | **卡面立刻掉光图标**：今天 `desc`/`descZh` 仍是自造记号那一代（官方文本要等 P3），删了 = **全池卡面退步** + `IconSetup.Verify` 那几条（DA44/SAU23/SOR66/ASH80/DA9/GOF101…）当场红 |
| **按文本世代分流**（本笔做的） | 官方文本来了就**不再经过**自造层 ⇒ **退场目的达到、零退步**；官方文本一到，那一支自然变成死代码 |

⇒ 若调度台要的是「**本批就物理删掉**」，那**必须先做 P3 的数据层**（`descZh` 换成官方文本），
否则一定退步。**本笔按「分流」落地**，`A1945` 在「官方文本走官方层」这一半上**已闭合**，
「物理删除那一支」要挂在 P3 之后 —— **这一条我按红线报上来、不自己定。**

---

## 五、改了哪些（清单）

**唯一文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Core/CardIcons.cs`（LF，**`git diff --numstat` = `357 1`**，**不是整篇重写**）

| 位置（现读行号） | 是什么 |
|---|---|
| `:209-230` | `Rewrite` 头部：**加官方记号的分流闸**（`A1763`+`A1945`）＋大段判据注释 |
| `:276-289`、`:359-362` | **铁律 5 就地更正**：`TraitNameToString` 的两处旧说法（见 §七·1） |
| `:419-440` | 官方记号层的大段文件内说明（含 `_DAT_` 地址→值表、四支顺序为什么不能改） |
| `:440` | `public const string DescZhField` |
| `:448-454` | `public static bool HasOfficialMarks(string)` |
| `:468-476` | `public static string RewriteMarks(string, bool)` |
| `:478-493` | 四条 `Regex`（`\(\((.*?)\)\)` / `\$\$(.*?)\$\$` / `\[\[(.*?)\]\]` / 不带引号的 sprite） |
| `:495-500` | `WarnMarkOnce`（按记号去重出声） |
| `:502-521` | `OfficialTerm`（走 `Loc.HasEntry`/`HasBulk`，**取不到回 null**） |
| `:523-528` / `:530-543` | `SpriteExists` / `QuoteSprites` |
| `:559-580` | ① `RewriteMarkModular` |
| `:582-608` | ② `RewriteMarkCardRefs` |
| `:610-671` | ③ `RewriteMarkTraits` |
| `:673-695` | `TraitTermOf` / `Norm` / `SpriteTagOf` |
| `:697-724` | ④ `RewriteMarkMana` |

⛔ **`工具/gen_icon_plan.py` 与 `Resources/card_icon_plan.json` 一行都没动** —— 施工设计把这两个划给了本笔，
但**做完之后判定「不需要」**，理由三条（请调度台复核）：
1. **官方记号是自描述的、不需要按卡的条目**：`((Key,…))` 的答案在 `Modular_Description/<Key>`（官方值里
   **自带** `<sprite>`/`<link>` 标签 —— 现读 340 条里 12 条自带图、其余是纯文本）；`[[X]]` 的答案在
   `Card_Trait/X` + 图名规则 `Atlas_trait_icon_<枚举名>`（55 种逐个核过，**命名是机械的**）；
   `$$ID$$` 在 `Card_Name/<id>`。⇒ **没有任何一格需要「逐卡 + 证据」那张表**。
2. 加一个「官方记号计划形状」= **把官方表再抄一份**，正是工程红线「两处写同一条规则 = 迟早不一致」。
3. `gen_icon_plan.py` 有 **rc≠0 不落盘** 的口径（施工设计 §五·11），而且它的 `descZh` 半边**归 P3**。
⇒ 结论：**新记号不需要新的计划形状**；要补的是**词条族**（`Modular_Description`/`Card_Name`/官方 `Card_Trait`），
那是 P3 / `A1982` 的数据侧。

---

## 六、验了什么 / 没验什么

| 项 | 怎么验 | 结果 |
|---|---|---|
| **编得过** | `TMPDIR=/tmp/wf_p0 bash d:/4/Unity/工具/typecheck.sh`（跑了 **4** 次） | **运行时 0 · 编辑器 0** |
| **XML doc 没写坏** | `WF_DOC=1 TMPDIR=/tmp/wf_p0 bash d:/4/Unity/工具/typecheck.sh`（本仓的 `A453` 那档） | **本文件 0 条 `CS1570`**（第一版有过 35 条，**已全部转义**；同行还自查过「别把纯 `//` 注释也转义了」） |
| **行尾没被翻** | `io.open(...,'rb')` 数 `\r\n`/`\n` + `git diff --numstat` | `CRLF 0 / LF 724`；diff **`357 1`** |
| **解析器没被带坏** | `bash d:/4/Unity/工具/ruleprobe.sh check` | **rc=0 · 哨兵失败 0 条 · 解析差异 0 行 · 改名 0 · 新增 0 · 消失 0**（与施工设计 §五·5 的预期一致 —— P0 本不该动它） |
| **零行为变化（零回归）** | 扫 `cards_engine.json` **1126 张 × 17 个字段**（含 `desc`/`descZh`/`name`/`nameZh`）里的 `[[`/`((`/`$$` | **各 0 处** ⇒ 今天的卡面**一条都走不到新路** |
| **算法本身通** | 演算台（白名单外，`d:/4/_tmp_p0/g_mirror.py`，⛔ **不是交付物**）：把四支**照抄一份成 py**，拿**真实官方正文**（2196 条）× 真实 `Loc` 词条跑 | ③ 的加粗档逐字对（`[[ephemeral]]` → `<link=ephemeral><nobr><sprite name="Atlas_trait_icon_ephemeral"><b>临时</b></nobr></link>`）· ①②④ 在缺表时**原样留着 + 出声**（不静默）· 用**合成词条**验证 ①②④ 的「有数据」档也通，且 **①→③ 的顺序依赖**在真实数据上兑现（`Modular_Description/Blind_a_random_enemy` 的值里就带 `[[blind]]`） |
| ⛔ **没验**（按令） | `CardBaseDemo.Run`（并排渲）· `IconSetup.Run` | **留给主对话在同步点跑**（同工程同一时刻只能一个 Unity 实例） |
| ⛔ **没验**（判据不足） | `<b>` 在我们的卡面字体上**渲出来是不是真的变粗** —— TMP 的 `<b>` 需要字体有粗体面或 faux-bold。**没跑到真机 ⇒ 不知道**，列在这里 | 见 §八 |

---

## 七、顺手发现（**只报，⛔ 没顺手改**）

1. 🔴 **已按铁律 5 就地更正（在我自己这个文件里）**：`CardIcons.cs` 两处旧注释说
   `TraitNameToString` 的 `param_2` 是「要不要图标那支」、并据此说「原版整项 = `<link><nobr>图词</nobr></link>`」。
   **现读 `GameStaticData__TraitNameToString.c` 不成立**：两个分支**都插图标**，`param_2` 是**加粗开关**；
   `:75-76` 是 `param_2 == false` 那支，而 `ModifyLocalization.c:455` 的调用点**传的是 1** ⇒
   原版正文那一项是 `<nobr>{图}<b>{词}</b></nobr>`。**更正只落注释**（那一支是我们自己的形状、有断言钉着，⛔ 不能动）。
2. 🔴 **`Loc.BulkOr` 有一道「记号闸」现在可以拆了**（在别人白名单里，**我只报**）：
   `Core/Loc.cs` 的 `BulkOr` 判据是「值里含 `[[` 或 `((` ⇒ 当取不到、回兜底并出声」，理由写的是
   「**要真接上，先做 `A1763` 的记号渲染层**」。⇒ **那个层现在有了**（`CardIcons.RewriteMarks`，公开口、
   纯文本变换、带「缺词条就留着 + 出声」的闸）⇒ 建议把 `BulkOr` 的返回值**过一遍 `RewriteMarks`**。
   ⚠️ 现数：本产物里带记号的值 **24** 条（`official_names.json` 1327 条里）。
3. 🔴 **官方正文里有 `<br>`**（现读样本 `[[ephemeral]]<br>Lower the cost of all cards...`）⇒
   P3 灌数据时要记得：`<br>` TMP 认、我们的 `StripTags` 会剥掉（点阵兜底那条路本来就不认识标签，口径一致）。
   同族还有 `Modular_Description/Create_x_in_your_hand` 值里的 `<nobr>` 与弯引号 `”{0}”`。
4. ⚠️ **`A1763` 行里的两个计数与这份快照复现不出**（⛔ 只报、不改正本）：
   · 行里写「EN 列 `[[armour]]` **115** 处」——我现数是 **97** 处（口径可能是「含 ZH 列」或另一份快照）；
   · 行里写「**但 132 张官方 zh 为空**」——现数 `Card_Description/*` **1098 条里 zh 列为空的 0 条**。
   ⇒ 那 132 更像「卡池里**没有 `Card_Description/<id>` 行**的张数」（1126 − 1098 = 28，也对不上）⇒ **判据不足，不裁**。
5. ⚠️ **官方 `Card_Trait/*` 有 77 条、我们 `Loc` 有 62 条**，按 exact/归一化两条路**两边都有的 51 条**
   ⇒ 换官方 `Card_Trait` 值时**会有 26 条要新增**（`battleHonour`/`berzerk`/`chooseOne`/`combo`/`counterOffensive`/
   `courage`/`dropPod`/`firstStrike`/`frontLine`/`jam`/`large`/`relentless`/`resolution`/`sneak`/`support`/
   `frenzied_old`/`markOfChaosOLD`/`questPoints` …）。属 P3 数据侧，**没动**。
6. ⚠️ **`[[X]]` 的参数里可以再嵌记号**：真实样本
   `Card_Description/1bfe0893…` = `[[ephemeral]].<br>((Give_trait_x_to_your_units_this_turn,[[shuriken]] 1))`
   ⇒ **①→③ 的顺序是承重的**（原版也是靠这个顺序收掉嵌进来的 `[[shuriken]]`）。
   ⛔ 谁都不要「顺手把四支的顺序调成看起来更合理的顺序」。

---

## 八、还没查清 / 要调度台裁的

1. **`A1945` 到底要不要「物理删掉」方括号那一支**（本笔按「按文本世代分流」落地，判据见 §四）——
   要物理删，**必须先做 P3**。
2. **`<b>` 在我们卡面字体上是否有可见效果**（要真机并排渲才能判；`CardBaseDemo.Run` 是主对话的活）。
   若「渲不出粗体」，要不要保留 `<b>`（原版有、我们渲不出）或是改用别的办法 —— **等并排渲的结果再裁**。
3. **`Modular_Description` / `Card_Name` 两族怎么进 `Resources/`**（≈ 340 + 1065 条）：走 `official_names.json`
   扩族，还是另起一份 `Resources/*.json` —— 属 P3 / `A1982` 的**形状决定**，**本笔不裁**（我只把消费者口做成
   `Loc.HasEntry` → `Loc.HasBulk` 两条路，两种形状都能吃）。
4. **原版 ① 用 `GetTermTranslation`、③ 用 `GetTranslation`**（两个不同的 I2 口）—— 回退行为是否有别，
   **没查**（我们的 `Loc` 只有一个口）。

---

## 九、没跑的东西（按令）

⛔ **没跑任何 Unity 批处理**（同工程同一时刻只能一个实例）：`CardBaseDemo.Run` · `IconSetup.Run` ·
`BattleScene.Run` 都没跑 ⇒ **`IconSetup.Verify` 那几条「计划表里每个 sprite 名都查得到」的断言、
以及 `CardBaseDemo` 的并排渲，本笔没有实测证据**，都要等同步点。
✅ **跑过的**：秒级类型检查（4 次，含 `WF_DOC` 档）· `ruleprobe check` · 纯 py 的演算台。
