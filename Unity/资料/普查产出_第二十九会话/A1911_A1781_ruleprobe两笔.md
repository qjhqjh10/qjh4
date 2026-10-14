# `A1911` + `A1781` —— `ruleprobe` 两笔小账（2026-10-14 第二十九会话）

> 白名单 = `d:/4/Unity/工具/ruleprobe/**`。本次**只改了 1 个文件**：`工具/ruleprobe/Program.cs`（`git diff --numstat` = **56 / 4**，LF 未翻）。
> ⛔ 没动 git · 没动两张正本 · 没动 `_a1759_*.py` · 没动 `Assets/**` · 没动三个指纹常量（`PinnedPoolMd5` / `PinnedBaselineMd5` / `PinnedParseMd5` —— 它们**在我动手前就已经是现值**，是主对话 `A1908` 那批重钉的）。

---

## 一、`A1911`：`LoadPool` 没传新三列

### 改前 / 改后（`文件:行号`）

| | 位置 | 内容 |
|---|---|---|
| **改前** | `工具/ruleprobe/Program.cs:393-395`（`= git show HEAD:…` 的那一版；工作区当时同形） | `list.Add(new CardDef(S("id"), …, S("descZh"), true, S("subtype"), S("goesSecond")));` —— **到这里就结束了**，`inventoryOptions` / `showInCollectionOptions` / `initiative` 三个**可选参数没传** |
| **改后** | `工具/ruleprobe/Program.cs:404-449`（新增 404-417 注释+局部量 · 423-430 三列判在不在 · **435-437 传值** · 440-449 缺列出声） | 传 `inventoryOptions: hasInv ? invEl.GetInt32() : -1, showInCollectionOptions: …, initiative: …` |

### 为什么这么改（三条）

1. **同一条口径只能有一处。** 这三个参数是 `Core/CardDef.cs:215-216` 上的**可选参数、默认全是 `-1`** ——
   而 `-1` 在这三列上的语义是**「真值里查不到这张卡」**（`CardDef.InventoryOptions` / `.ShowInCollectionOptions` / `.Initiative` 三个属性的 doc 都写着）。
   于是探针造出来的每一张卡都带着「没数据」，而它**长得和真值一模一样**（都是 `-1`）⇒ **静默**。
   引擎那边的对应调用点是 `RuleEngine/Data/CardDatabase.cs:159-161`（**同一个 JSON、同一批列名**）——
   这就是 `A1339` 那一族的**第 2 处**「探针与引擎口径差」。
2. **消费者是谁**（写进注释里了）：`DeckRules.CannotAddToDeck`（能不能进卡组）· `FirstSeatRules.ByInitiative`（谁先手）。
   今天这两条路**探针里没人调**（下面 §三 的 grep 实证），所以这笔账修的是**「将来拿探针去量这两件事」的口径**，不是今天的某个读数。
3. **缺列【不许】当 `0`** —— 这是我做的一个明确取舍，理由写进了代码注释：
   这三列的 `0` 都是**合法真值**（`CardInventoryOptions.InInventory` / `ShowInCollectionOptions.Hide` / `Initiative.Undefined`），
   而 `LoadPool` 里原有的 `I()` 助手**缺列返回 `0`** ⇒ 对它仨**不能用**。
   ⇒ 缺列一律 `-1`（= 缺值）**并出声**（`Program.cs:441` 版本 < 8 / `:446` 逐列缺行数，两处 `⚠️ Console.WriteLine`）。
   ⚠️ **与引擎的一处已知差异（如实写在这）**：引擎走 `JsonUtility`，缺字段会读成 `0`（它自己也为此出声：`CardDatabase.cs:108-111` 的 `version < 8` 警告）。
   两边**都出声、都不把缺值当有数据**，但**取值不同**（引擎 `0` / 探针 `-1`）—— 探针按 `CardDef` 那三个属性的 doc 口径（`-1` = 缺值）走。
   现读卡池（v8）**三列一格不缺**，所以这条分支今天走不到。

### 那三列现在读到什么（**从 `CardDef` 对象上取，不是从 JSON 取**）

我用一行**临时**自检（跑完已删，`grep TEMP-A1911` = 0 命中）把 `list` 里的 `CardDef` 当场数了一遍，
与**独立**用 python 现读 JSON 算出来的逐格相同：

| 列 | 缺值 `-1` | 其它 |
|---|---|---|
| `initiative` | 171 | `20` × **955** |
| `inventoryOptions` | 171 | `0` × 826 · `5` × 31 · `10` × **98** |
| `showInCollectionOptions` | 171 | `5` × 862 · `0` × 93 |

⇒ 与 `DeckRules.CannotAddToDeck` 的 doc（`:252-261`）自报的「955 张接上真值 · 拦下 98 张 · 171 张记 -1」**逐个吻合**。
探针 `list.Count` = **1126**（= 卡池 `count`）。

### 我怎么验的（四步）

1. **编得过 + 没改坏任何读数**：`check` **哨兵合计：失败 0 条** · `全池 1120 张 · 解析差异 0 行` ·
   `✓ [基线指纹] 卡池与基线的指纹都与钉住的一致` · `✓ [输出指纹] …e3a4b082…` · `rc=0`（`check` 会先 `dotnet build`，构建失败会出声并停）。
2. **值级**：上面那行临时自检（现读 `CardDef` 分布 == JSON 分布）。
3. **A/B（证明「改前是 `-1`」这件事可测、且对现有读数零影响）**：把三列**临时**写成字面量 `-1`（= 改前）跑 `play check`
   ⇒ 结算指纹与改后**逐字节相同**（`888869e6…`）、差异张数也相同（主档 `773`/新增 193/消失 193 · 电池 `345`/179/179）。
   依据（为什么必然相同，不靠这一次实测也行）：**全仓 `工具/ruleprobe/*.cs` 对 `Initiative` / `InventoryOptions` /
   `ShowInCollectionOptions` 的引用 = 0 处**（改前 grep 实测；现在唯一命中是我自己那行注释），
   而 `CannotAddToDeck` 与 `FirstSeatRules` 的调用点全在 `MyGame/Assets/**`（`DeckEditorState` / `DeckBuilder` / `BattleDriver`），**探针一个都不走**。
4. `git diff --numstat` = **56 / 4**（行尾没被翻）。

---

## 二、`A1781`：`faith` 探针夹具的旧 id

### 结论：**这笔账已经不存在了 —— 2026-10-14 的提交 `843e78a`（第二十八会话收口）里已经改掉**，我这次只是**把它核了一遍**（没有可改的东西）。

### 旧 id 是哪个 / 换成什么（逐条，6 组）

`工具/ruleprobe/FaithProbe.cs:248-257`（`Scenario(pool, "<id>", <faith>)` 那 10 行），`git show 843e78a -- 工具/ruleprobe/FaithProbe.cs` 的 `-/+` 行：

| 行 | 改前（旧 id） | 改后（现 id） | 卡（= 夹具本意） | `faith` 取值（**未改动**） |
|---|---|---|---|---|
| 248-250 | `SOR27` | **`SOR23`** | Canoness（☀ 门槛 2, 4） | 0 / 2 / 6 |
| 251-252 | `SOR11` | **`SOR7`** | Crusader（3） | 2 / 3 |
| 253 | `SOR68` | **`SOR67`** | Sacred Rose Sister（2, 4） | 4 |
| 254-255 | `SOR12` | **`SOR9`** | Preacher（5） | 4 / 5 |
| 256 | `SOR72` | **`SOR71`** | Adelaide the Serene（6） | 6 |
| 257 | `SOR40` | **`SOR38`** | Paragon Warsuit（6） | 6 |

### 依据（三条独立判据，互相对得上）

1. **普查报告的「旧表卡名 / 新表卡名」两列** —— `资料/普查产出_第二十七会话/旧id残留_普查.md:58-67`
   （表头在 `:37`：`文件:行号 | 字面量 | 旧表卡名 | 新表卡名 | 上下文`）：
   `SOR27` 旧表 = **Canoness** / 新表 = `Penitent Engine`；`SOR11` 旧 `Crusader` / 新 `Dominion`；
   `SOR68` 旧 `Sacred Rose Sister` / 新 `Ministorum Priest`；`SOR12` 旧 `Preacher` / 新 `Retributor`；
   `SOR72` 旧 **Adelaide the Serene** / 新 `Castigator`（报告当时就标了「简报点名的两条之一」）；
   `SOR40` 旧 **Paragon Warsuit** / 新 `Inspiring Oratory`（同）。
2. **现读卡池**（`MyGame/Assets/RuleEngine/Resources/cards_engine.json`，只读）：旧 id 现在**确实指别的卡**
   （`SOR27`→`Penitent Engine` · `SOR11`→`Dominion` · `SOR68`→`Ministorum Priest` · `SOR12`→`Retributor` ·
   `SOR72`→`Castigator` · `SOR40`→`Inspiring Oratory`，逐条与报告一致），
   而「本意那张卡」在现池里的 id 就是新写法：`SOR23`=Canoness · `SOR7`=Crusader · `SOR67`=Sacred Rose Sister · `SOR9`=Preacher · `SOR71`=Adelaide the Serene · `SOR38`=Paragon Warsuit。
3. **夹具自洽（最硬的一条）**：`faith` 那 10 个阈值**一个字没动**，而每条 `Scenario` 打出来的「落场后的卡」正好是上表那 6 张、
   且「门槛够 / 门槛不够」两份清单与该卡的 ☀ 花费**逐个对得上**（例：`SOR23` faith=0 ⇒ 门槛不够=2,4；
   faith=6 ⇒ 门槛够=2,4）⇒ **不是随便换了个能跑的 id，是换回了同一张卡**。

### 顺带核到（`A1777` 的 `ruleprobe` 子集）

`旧id残留_普查.md:68-70` 点名的另 3 处**也已经是修好的状态**：`WillRun.cs:537` 的哨兵对已从 `SOR28/SOR36` 改为 `SOR22/SOR34`
（旧写法只留在 `WillRun.cs:495-503` 的**更正注释**里，是铁律 5 要求的痕迹）· `NonKwProbe.cs:404` 的 `GOF85` 已改为 `GOF86`（`Unleashed TramplaSquig`，与它自己那行标签一致）。
全目录复扫（把 `工具/ruleprobe/*.cs` 里所有「像卡 id 的字符串字面量」∩ 现池 id）⇒ **一条不剩**（剩下的候选全是夹具名 `FIX_*`/`FX-*`/`P19_*` 与代码标识符）。

---

## 三、跑了什么命令 · 读数

```
bash d:/4/Unity/工具/ruleprobe.sh check      （会先 dotnet build）
  已成功生成。
  ✓ [基线指纹] 卡池与基线的指纹都与钉住的一致（基线没被静默改写）
  ✓ [输出指纹] 当次现算的解析输出与被确认的那一版一致（e3a4b082…）
  === 哨兵合计：失败 0 条 ===
  === 全池 1120 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
  === 19:33:59 结束（rc=0）===

bash d:/4/Unity/工具/ruleprobe.sh faith        （**最终态**那次 · 19:35:20）
  6 条夹具全 ✅（SOR23×3 · SOR7×2 · SOR67 · SOR9×2 · SOR71 · SOR38）
  「⛔ 池里没有 `X`」「⛔ `X` 打不出去」= **0 次**
  === 19:35:20 结束（rc=0）===

bash d:/4/Unity/工具/ruleprobe.sh play check   （A/B 用；**两遍读数逐字节相同**）
  结算指纹：当次现算 = 888869e68be7df5c347854b8ed3b6de0 · 基线文件 = d1fe0d96…（不同）
  🔴 773 张卡变了 · 新增 193 · 消失 193      （主档）
  🔴 345 张卡变了 · 新增 179 · 消失 179      （事件电池）
  ✓ ② 哨兵：基线文件的内容与钉住的一致（d1fe0d96…）
  rc=1  ← 🔴 **与本次改动无关**，见 §五·1

git diff --numstat -- 工具/ruleprobe/Program.cs
  56      4       （LF 未翻）
```

**没跑**：Unity 自检（一条都没跑）、`scan`（会重写基线 ⇒ 白名单外）、`play all`。

---

## 四、没办成 / 需要别人决定的

1. **`A1781` 没有「我改的东西」可交** —— 它已在提交 `843e78a` 里落地。
   ⇒ **请主对话把 `资料/任务_卡组卡牌战斗.md:61` 那一行标 ✅**（我⛔不能改正本）。若要留痕，建议写「已在 `843e78a` 落地；第二十九会话复核实务:`FaithProbe.cs:248-257` 现读 6 张全对 + `faith rc=0`」。
2. **`A1911` 的「值级验收」我是用临时自检行做的（跑完已删）** —— 探针里**没有任何常驻出口**会打这三列。
   如果主对话希望**将来能一眼验**这件事，需要**另立一笔**（例如给 `card <id>` 或 `layers` 加三列）。
   ⛔ 我没顺手加：那会动 `layers` 的列名清单（`Layers.AuraColsDrift` 有机器级守卫），属于新账。
3. **`Program.cs` 工作区里还带着主对话 `A1908` 那批「三个指纹重钉」的未提交改动**（`PinnedPoolMd5` / `PinnedBaselineMd5` / `PinnedParseMd5` + 长注释）。
   我**一个字没碰**这几个常量；`check` 现读三个都自洽（池 md5 `9ab791869c375210bd5eb8376aa37b75` · 基线 md5 `6c0e41d71599bd7a42f078a50e90f614` · 解析 md5 `e3a4b0820b1ff2f274b208a0732b5e88`）。

---

## 五、顺手发现（**只报，⛔ 我没改**）

1. 🔴 **`play check` 现在是红的（`rc=1`），但这是【改动前就有】的漂移**，不是本次引入的：
   - 实证 = §一 那个 A/B：把三列强制回 `-1`，结算指纹与差异张数**逐字节相同**；
   - 佐证 = 差异形状是**整代 id 的增删**（主档 新增 193 / 消失 193、电池 179/179），而那只能来自 **id 置换**；
   - 时点 = `工具/ruleprobe/基线/out_play_baseline.txt` 的 mtime = **2026-10-13 01:33**（`ls` 现读），**早于** 10-13/14 那批换 id 与 10-14 的引擎改动 ⇒ 基线本身过期。
   ⇒ 这是**主对话才能做的动作**（重生成 play 基线 + 重钉 `PlayProbe.PinnedPlayMd5`，属 `A1619` 那一族，⛔ 子代理禁动）；我按红线**停手不碰**。
2. **`PlayProbe.cs:629-641` 的 `Blank(c)` 是同族的第二个坑位**：它把被测卡**复制一份**（`desc` 清空给对照局用），
   而那个 `new CardDef(...)` 同样**只传到 `subtype`** ⇒ 复制出来的卡那三列**回到 `-1`**。
   今天**不产生错误读数**（走 `play` 这条路，没人读那三列 —— 判据同 §一·依据），
   但形状与 `A1911` **一模一样**（「探针现造的 `CardDef` 静默丢列」）⇒ 记在这里请主对话裁要不要一并补。
3. ✅ **「1120 vs 1126」已查清、不是缺陷**（先记结论，别当账挂）：`Scan` 打「全池 **1120** 张」而 `LoadPool` 的 `list` 是 **1126** ——
   差因 = `工具/ruleprobe/Scan.cs:137` 的 `if (string.IsNullOrEmpty(desc)) continue;`（dump **跳过 `desc` 为空的卡**），
   而现读卡池里 **`desc` 恰好为空 6 张**（`ASH61 Weapons Platform` · `BL2 Black Legionary` · `BL26 Obliterator` ·
   `GOF8 Hornhelmz Boy` · `TL61 Stranded Termagant` · `UM6 Primaris Intercessor`）⇒ **1126 − 6 = 1120，逐格对上**。
   （⚠️ 这 6 张是 `scan`/`check` 的**盲区**：调它们的效果解析改动，本基线**照不出来** —— 记在这里备查，⛔ 不是本次的账。）

