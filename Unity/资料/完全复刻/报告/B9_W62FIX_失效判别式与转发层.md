# B9_W62FIX · 修一条**已失效的灭自证判别式** + 两处过期/重复

> 执行代理 `B9_W62FIX` · 2026-10-27 · 白名单 2 个代码文件：
> `CardPresentation/Editor/DeckScene.cs` · `RuleEngine/Data/CardDatabase.cs`（**一行都没越**）。
> 判据（不重查）→ `报告/B9_W56ALTT_异画收紧接线.md` §三·4 与 §五·1、§五·2。
>
> 必跑三件（**全部在最后一批改动落盘之后**跑）：
> 1. `TMPDIR=/tmp/wf_w62 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
> 2. `bash 工具/ruleprobe.sh check` ⇒ **rc=0**，解析输出指纹 **`c9a64798…` 不变**
> 3. `bash 工具/ruleprobe.sh play check` ⇒ **rc=1**，**只有 `BL64` + `DA42`**（没有第三张）

---

## 一、结论

三件都做完，**两个文件的总行数一个都没变**（`DeckScene.cs` **10,331**、`CardDatabase.cs` **3,346**），
且**方法内部落点也回到原位**（`CardDatabase.ResolveDeckEntry` 的签名仍在 **`:3303`** —— 与 `B9_W46ALTD` 报告里引的那个号一致）。

| # | 件 | 改前 | 改后 |
|---|---|---|---|
| ① | **失效的灭自证判别式** | `DeckScene.cs:1375` 写「去掉 `CardDatabase.ResolveDeckEntry` 里那条收紧 ⇒ 本条红」，**但收紧已在 `PlayerDeck.ResolveEntry`** ⇒ **反证实验两个方向都做不出来（只删哪一句断言都仍然绿）** | 断言**本身**改成直调 `PlayerDeck.ResolveEntry`、判别式文案同步指向它（与 `AltArtDeckSelfCheck` ②b 同口径）⇒ **删掉 `DeckRules.cs:890` 那一句就真的红了** |
| ② | **过期注释** | `DeckScene.cs:1344` 写自检「**11 条**」 | 改成「**21 条断言 + 4 条前提硬检查**」（现数核对见 §二·2） |
| ③ | **重复的收紧** | `CardDatabase.ResolveDeckEntry` = 转发 `ResolveEntry` **再自己判一次** `if (Known && !HasAlternateArt) return null;` —— 收紧搬走后那半句**永远走不到** | **退化成纯转发**（一行 `return PlayerDeck.ResolveEntry(id, lookup, out _);`）；第 4 个调用点（本件第 ① 条那个断言）也改直调 ⇒ **"异画必须在卡表里"全仓只留 `DeckRules.cs:890` 一处** |

### ① 的重点：「新的判别式删哪一句会红、为什么旧的不红」

- **新的**：删掉 `RuleEngine/Core/DeckRules.cs:890` 的
  `if (c == null || (c.AlternateArtsKnown && !c.HasAlternateArt(id))) return null;`
  ⇒ `#HB_ASH83`（表里 `alternateArts: []` 明确没有异画）会解析成**ASH83 那张卡本身（非 null）**
  ⇒ 断言前半（`#HB_SW1` 非 null）**仍真**、后半（`#HB_ASH83 == null`）**变假** ⇒ **整条红**。
- **旧的为什么不红**：断言当时调的是 `CardDatabase.ResolveDeckEntry`，而它（改前）在转发之后
  **还留着收紧的第二份半句**（旧 `:3308`）⇒ 把 `ResolveEntry` 那半句删掉，`ResolveDeckEntry` 的第二份
  照样把 `#HB_ASH83` 判成 null ⇒ **断言仍然绿**。
  更糟的是：按判别式**字面**去删 `ResolveDeckEntry` 那半句，也**仍然绿**（因为 `ResolveEntry` 已经收紧了）
  ⇒ **两个方向都做不出反证** ⇒ 这条判别式已经**没有任何反证能力**（这正是「幂等 ≠ 有用」那一族）。
- **为什么现在不会再失效**：③ 一起做完之后，`ResolveDeckEntry` 里**不存在**第二份判断
  ⇒ 判别式指向的那个点就是**全仓唯一**能改动这条收紧的地方（防「两边一起改回去」的结构性前提）。

---

## 二、证据（文件:行号 · 全部是本件改完后的**现读**）

### 2.1 ① 判别式（`CardPresentation/Editor/DeckScene.cs`）

```csharp
:1371   Func<string, CardDef> lkAlt = k => CardDatabase.FindById(poolAlt, k);
:1372   CheckTrue(PlayerDeck.ResolveEntry("#HB_SW1", lkAlt, out _) != null
:1373             && PlayerDeck.ResolveEntry("#HB_" + noAltCard.Id, lkAlt, out _) == null,
:1374             "★ 收紧生效：`#HB_SW1` 解析得回原卡、`#HB_<没异画的卡>` **解析成 null**"
:1375           + "｜🧨 去掉 `PlayerDeck.ResolveEntry` 里那条收紧 ⇒ 后半变真 ⇒ 本条红（🔴 收紧只在这一处；`CardDatabase.ResolveDeckEntry` 已退化成纯转发，⛔ 别拿它当判别式）");
```

- **前提硬检查在别处、且先跑**：`:1360-1361` 断 `SW1` / `ASH83` 都在卡池里（不是 `if (前提) Check(结论)`，
  是独立一条 `CheckTrue`）；`:1364-1370` 两条**反向**断 `HasAlternateArt`（正/负各一条，挡恒真/恒假桩）。
  本件新增的这条排在它们**之后**，所以前提不成立时会先红在前提上，⛔ 不会静默跳过。
- **判据值（现读 `cards_engine.json`，非猜测）**：`SW1` → `"alternateArts": ["#HB_SW1"]`（登记了）；
  `ASH83` → `"alternateArts": []`（明确没有，`AlternateArtsKnown == true`）。
- **与自检件同口径**：`CardPresentation/Deck/DeckEditorState.cs:1187-1189`（`AltArtDeckSelfCheck` ②b）
  也是直调 `PlayerDeck.ResolveEntry`、判别式也写「去掉 `PlayerDeck.ResolveEntry` 里那条收紧 ⇒ 本条变红」。
- **依赖可用性**：`DeckScene.cs:21` 有 `using RuleEngine;`；`PlayerDeck` 在 `namespace RuleEngine`（`DeckRules.cs:41`）
  ⇒ 直调合法（`out _` 丢弃，编辑器程序集 typecheck **0 错**已证）。

### 2.2 ② 注释（`DeckScene.cs:1344`）

```
//   的 `AltArtDeckSelfCheck`（**21 条断言 + 4 条前提硬检查**，含「这些异画 id 本身不在池里」
//   「正例必须在表里」「负例必须明确没有」那几条**灭自证前提**），
```

现数核对（`DeckEditorState.cs` 里 `C("` 共 **26** 处）：
`5 处` = 守卫 `:1128`（只在 null 时出）+ 前提 `:1143` / `:1150` / `:1156` / `:1163`
⇒ **断言 26 − 5 = 21**，**前提 4**（另 1 条守卫）✔ 与 `B9_W56ALTT` 报告 §2.3 的逐段表逐条吻合。

### 2.3 ③ 转发层（`RuleEngine/Data/CardDatabase.cs`）

```csharp
:3303   public static CardDef ResolveDeckEntry(string id, Func<string, CardDef> lookup)
:3304   {
:3305       // 只转发：剥前缀 / 份数口径 / 「异画必须在卡表里」的**唯一实现都在 `PlayerDeck`**
:3306       //   （`ResolveEntry` / `SameCardKey`，见 `RuleEngine/Core/DeckRules.cs`）⇒ ⛔ 这里不另写一套判断
:3307       //   （本项目铁律：两处写同一条规则 = 迟早不一致）。收口后**行为逐条等价**：原来那半句
:3308       //   `if (AlternateArtsKnown && !HasAlternateArt(id)) return null;` 现在**永远走不到**（收紧已在 `:890`）。
:3309       return PlayerDeck.ResolveEntry(id, lookup, out _);
:3310   }
```

**为什么「永远走不到」是可证的、不是推断**：`PlayerDeck.ResolveEntry`（`DeckRules.cs:881-893`）里
`isAltArt = true` **只在那条收紧之后**才赋值（`:891`）⇒ 调用方拿到的 `altArt == true` **必然**已经过了收紧
⇒ 旧的 `if (c == null || !altArt) return c;` 之后那段不可达。故「删掉它」= **行为逐条等价**，不是「大概等价」。

- doc（`:3292-3298`）同步就地改写（**行数不变**）：原来写「这条收紧**在这里**兑现」→ 改成「**唯一实现 = `PlayerDeck.ResolveEntry`**」，
  并留一条 2026-10-27 的收口说明（铁律 5：改错要留痕）。
- **零行数变化**：`CardDatabase.cs` 仍 **3,346** 行（`B9_W46ALTD` 报告记的就是 3,346）；签名仍在 `:3303`、方法 `}` 仍在 `:3310`
  ⇒ 方法内部连**行号**都没漂（本件刻意把 doc 补一行、body 收一行把几何还原）。

### 2.4 四个调用点都**不依赖**「RS 自己再判一次」（逐条核过，故可退化）

| 调用点 | 用法 | 退化后有无差别 |
|---|---|---|
| `CardPresentation/Core/CostCurveDrawer.cs:113` | `var c = …; if (c == null) continue;` | 无（只关心 null / 非 null） |
| `CardPresentation/Shell/DeckInfoPopup.cs:924`（`CurWarlord`） | 返回 `CardDef` 或 null（null ⇒ 立绘位空） | 无 |
| `CardPresentation/Shell/DeckInfoPopup.cs:1805`（卡表行） | `if (c == null) continue;` | 无 |
| `CardPresentation/Shell/DeckInfoPopup.cs:1821`（防御卡） | `if (defensive != null && !IsDefensiveRow(defensive))` | 无 |
| `CardPresentation/Editor/DeckScene.cs:1372-1373` | 本件第 ① 条 | **已改直调** `PlayerDeck.ResolveEntry` |

⇒ **没有任何一处**依赖 `ResolveDeckEntry`「自己再做一次判断」（即简报里说的「若发现…就停下来」那种情况**不存在**）。

---

## 三、没查清的部分

1. 🔴 **改过的那条断言没有真跑过**（红线：本件不跑 Unity 宿主）⇒ `DeckScene.Run` 的这条**首次运行在同步点**。
   静态复核做了三件：① 前提 `SW1` / `ASH83` 的卡表值**现读资源核过**（`alternateArts` 分别 `["#HB_SW1"]` / `[]`）；
   ② `PlayerDeck` 的可见性（`using RuleEngine`）；③ 两个文件 typecheck 编辑器程序集 **0 错**（`out _` 用法编译通过）。
2. **本件没写账补丁**（`账/_parts/B9_W62FIX.tsv` **未创建**），理由三条：
   ① 本件**没有改变任何「原版方法 → 我们的落点」的覆盖关系**（`HUD:92`/`HUD:93` 状态仍 **B**，落点仍指向
   `DeckRules.cs:881` 那一套 —— 与 `B9_W56ALTT` 已合并的内容一致）；
   ② 我逐字段核过 `战斗界面_HUD.tsv` 92/93 两行的 `我们的落点`/`差异`/`备注`：**一处都没提 `ResolveDeckEntry`**，
   也没有「11 条」这种过期计数（自检件引的是 `DeckEditorState.cs:1113`，那个号没变）⇒ **没有需要就地改的过期记录**；
   ③ 那两行的字段长 **1,454～1,862 字符**，整行替换在本波多路并行下是明确的覆盖风险（`G11`/`G5` 那条重复键 + 「旧内容定时炸弹」），
   **为一句补充说明去承担这个风险不值得**。若调度台认为需要记一笔，建议在合并时手工往 `备注` 追加一句
   「③ `CardDatabase.ResolveDeckEntry` 2026-10-27 退化成纯转发（收紧只在 `DeckRules.cs:890`）」。
3. `CardDatabase.cs:3308` 注释里写了 `（收紧已在 ':890'）` —— **硬编码的上游行号**，`DeckRules.cs` 一旦在其上方增删行就会漂
   （`DeckRules.cs` 不在本件白名单，本件没动它）。同样 `DeckScene.cs:1375` 的文案只写文件与符号、**不写行号**，是刻意的。
4. **短写法 `#SW1` 仍被拒**（`B9_W56ALTT` §三·3 已记的边界）：本件未动、也未给它补断言（⛔ 不「顺手支持它」）。

---

## 四、改动清单

| 文件 | 改了什么 | 行号（改后） | 行数变化 |
|---|---|---|---|
| `CardPresentation/Editor/DeckScene.cs` | ① 断言改直调 `PlayerDeck.ResolveEntry`（`out _` 丢弃）+ 判别式文案改指向 `PlayerDeck.ResolveEntry`；② 注释「11 条」→「21 条断言 + 4 条前提硬检查」 | `:1344`（注释）· `:1372-1375`（断言 + 判别式） | **10,331 → 10,331（不变）** |
| `RuleEngine/Data/CardDatabase.cs` | ③ `ResolveDeckEntry` 退化成纯转发（删掉不可达的第二半句判断）+ doc 就地改写（「在这里兑现」→「唯一实现 = `PlayerDeck.ResolveEntry`」+ 收口说明） | doc `:3292-3298` · 签名 `:3303` · 方法体 `:3304-3310` | **3,346 → 3,346（不变）** |
| `资料/完全复刻/报告/B9_W62FIX_失效判别式与转发层.md` | 本报告 | —— | 新增 |
| `账/_parts/B9_W62FIX.tsv` | **未创建**（理由 → §三·2） | —— | —— |

**没碰**：`RuleEngine/Core/DeckRules.cs` · `RuleEngine/Core/CardDef.cs` · `CardPresentation/Deck/DeckEditorState.cs` ·
`Core/CostCurveDrawer.cs` · `Shell/DeckInfoPopup.cs` · `Deck/DeckRuntime.cs` · `Battle/BattleDriver.cs` · `工具/**` ·
`账/**`（含主表与其它补丁）· `*.unity` / `ProjectSettings/` / `CLAUDE.md` / `项目任务.md`。

### 必跑结果（原样）

```
1) TMPDIR=/tmp/wf_w62 bash 工具/typecheck.sh
   --- 运行时程序集 ---   运行时错误数: 0
   --- 编辑器程序集 ---   编辑器错误数: 0
2) bash 工具/ruleprobe.sh check
   ✓ [输出指纹] 当次现算的解析输出与被确认的那一版一致（c9a64798…）
   === 全池 1121 张 · 解析差异 0 行 · … ===   === 19:29:35 结束（rc=0）===
3) bash 工具/ruleprobe.sh play check
   🔴 **2 张卡变了** · 新增 0 · 消失 0   →  BL64  /  DA42
   === 19:30:21 结束（rc=1）===
```

⚠️ 中途那次 typecheck 曾报 **2 条 `DeckRuntime.cs(9067/9073) CS0103 HookWildcardUpdate/UnhookWildcardUpdate`** ——
那是 **`Deck/DeckRuntime.cs`（W51 正在写）** 的半成品，**本件一次都没碰那个文件**；同一分钟内复跑即 **0/0**。
（按 CLAUDE.md 铁律 13·3：错误全集中在不是自己负责的文件 ⇒ 不是本件的问题。）

---

## 五、顺手发现（不改，只报）

1. **`报告/B9_W46ALTD_异画显示侧与卡表.md:34` / `:67` 的措辞现在是历史快照**：那两行说收紧皮是它「本件新建的」，
   而 2026-10-27 起 `ResolveDeckEntry` 只是纯转发。它自己的 `:170` 已经预言了这次收口，语义可对上 ⇒
   **不是错记录，是时点快照**。本件**无权改**那份报告（不在白名单），若调度台要统一口径可加一行「2026-10-27 已收口」。
2. **`DeckScene.cs:1375` 与 `DeckEditorState.cs:1189` 现在是同一条判别式的两处副本**（措辞不同、指向同一个点）。
   这是**刻意的冗余**（两个宿主各跑一次，任一侧被删掉另一侧仍然会红）⇒ ⛔ 不建议合并成常量（合并后「删常量」会一次抹掉两条）。
3. **`CardDatabase.ResolveDeckEntry` 现在只剩「异画感知 + 换 lookup」零额外语义**，四处调用点可以等价地直调
   `PlayerDeck.ResolveEntry(id, lookup, out _)`。本件**没有**顺手改那四处（`CostCurveDrawer` / `DeckInfoPopup` ×3 都不在白名单，
   且它们现在语义正确、没有第二份判断）—— 留着这一层皮是为了**调用点读起来知道「这里是卡组条目、不是裸 id」**。
   但要知道它**已经不是任何规则的所有者**。
4. `ruleprobe play check` 那 **82 张「电池净差 = 0」** 的清单（`AM17 … UM81`）与 `BL64` / `DA42` 无关，是脚本自己标的
   「仍未验到」档 —— 本件改动（`DeckRules` 之外的显示侧 + 自检）不经过 `play` 路径 ⇒ **指纹变化仍只有那两张**（别人的）。
