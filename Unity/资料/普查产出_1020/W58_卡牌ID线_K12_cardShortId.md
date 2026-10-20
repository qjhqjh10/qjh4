# W58 · K12 最后一条 · `cardShortId` 收口（2026-10-20）

> 任务：把 `资料/卡牌制作_正本.md` §七·2 第 8 条（`cardShortId` 用途）查清或写死「还差什么」。
> 判据层级：原版反编译方法体（`d:/2/tools/decomp_full/`）＞ 反编译元数据（`d:/2/tools/il2cpp_out/`）＞ 当前 build SO（`资料/普查产出_1018/allcards_1350/cards_raw.json`）＞ 我们引擎。
> 本轮**只读**：未改任何产品代码 / 数据 / 正本 / `CLAUDE.md` / `项目任务.md` / 任务线总表；未跑 Unity / 自检 / player / git / 截图。
> 只新建：本报告 + `工具/_w58_k12_{typed,flow,lambda,full,inherit}_scan.py`（一次性只读脚本，LF）。

---

## 〇 结论

**能解出一半（而且是可操作的那一半），不能解出「原作者想拿它做什么」。**

1. **客户端 0 读点 —— 强证据（不是「没搜到」）。**
   - 按名字：反编译 25,096 个方法体 **0 命中**；类桩只有 1 处**字段声明**；`all_strings.txt` / `stringliteral.json` / `GameAssembly.dll` 字符串 / `assets_full` 全 JSON **全 0**；`global-metadata.dat` 只有字段名 1 处（元数据本来就存字段名）。
   - 按偏移：对 **484 个「签名或自身类型含 `RawCardScript`」的方法**做 `+0x40` 访问普查（含继承字段 `EntityScript.rawCardData` / `CardScript.rawCardData` 的数据流跟踪），**0 处**是 `RawCardScript.cardShortId`。关键类反编译覆盖率：`CardScript` **423/423**、`RawCardScript` **105/105**、`CardDisplayWindow` 37/37、`DeckEditingWindow` 29/29、`CardInBoosterPack` 19/19、`PlayerDataManager` 397/397、`BattleManager` 647/649。
   - 通道侧：牌组串 = `uniqueId`；PvP = `uniqueInGameID`；本地存档只有设置项；协议里没有 `shortId` 键。
2. **它不是 id、不是 `uniqueId` 短形式、不是 PnP 编号、不是 UI 排序键。**
   - 48 组重复 / 103 张（含不同卡共用，如 `DA3`↔`SW3`=855、`UM76`↔`UM87`=481、`ASH74`↔`ASH76`=575）；
   - 与 `uniqueId` 数字后缀相等 **1/904**；
   - 与 PnP 图文件名编号不重合（例：`DA17` Deathwing Terminator：PnP 文件名 **20**、`m_Name` 前缀 **14**、`cardShortId` **805**）。
3. **实测形态 = 内容编写期的全局顺序号**（这一段是**实测结构**，不是用途结论）：
   - 1…914，按阵营成块；块序 = 阵营登场顺序（`Tau 369–443` · `Sororitas 576–656` · `Genestealers 657–731` · `AstraMilitarum 723–799` · `DarkAngels 791–857` 等，块内 **~90% 连号**，Tau/GSC 块 **74/74 连号**）；
   - **`exp65` 之后再没有新分配**：`exp70/75/80` 的 4 条非零值**全部**与旧卡同号（`UM88=212=UM48` · `UM87=481=UM76` · `ASH76=575=ASH74` · `DA75=856=DA1/DA75 组`）；
   - **复制资产继承原值**：19 条带 `refNameId` 的变体里 **14 条**与基准卡同号；
   - 与 `path_id` 序（ρ=-0.030）、卡名字母序（ρ=-0.086）、`m_Name` 前缀号（ρ=-0.034）基本无关。
4. **不能解出的**：原作者（编辑器 / CMS / 后端）拿它做什么 —— 客户端与本地资产里 0 读点，本地没有任何后端代码/抓包。**⛔ 不猜。** 要证只剩两条路（见 §八）。
5. **对做新卡的可操作结论已经确定**：照抄同源卡的取值或填 0，⛔ 不赋予语义。⇒ **正本 §七·2 第 8 条可以从「没查清」移出**，替换文字见 §七。

---

## 一 字段事实（静态可证）

| 项 | 值 | 出处 |
|---|---|---|
| 声明 | `public int cardShortId;`（普通公有字段，**没有 getter/setter**） | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:9` |
| 偏移 | `+0x40`（Il2CppDumper 原始 dump） | `d:/2/tools/il2cpp_out/dump.cs:23247` |
| 类型化头文件 | `struct RawCardScript_Fields : PlayerItem_Fields { int32_t cardShortId; ... }` | `d:/2/tools/il2cpp_out/il2cpp.h:16413-16414` |
| 类 | `RawCardScript : PlayerItem : ObtainableItem, IPurchasableItem, IIdentifiable`（ScriptableObject） | `RawCardScript.cs:7` · `PlayerItem.cs:4` |
| 子类 | **无**（全库没有 `: RawCardScript` 的派生类） | 本轮解析 dump.cs 全类型 |

**分布（本轮现算）**

| 数据集 | 张数 | 非零 | 零 | 不同非零值 | 范围 | 重复组 |
|---|---:|---:|---:|---:|---|---:|
| 当前 build 全量 SO（`cards_raw.json`，1,351） | 1,351 | **915** | **436** | **860** | 1–914 | **48 组 / 103 张** |
| 我们卡池（`cards_engine.json`，1,127） | 1,127 | 840 | 287 | 827 | 1–914 | 12 组 |

- 我们这份**逐张与 SO 相同**：1,127/1,127 命中、**0 处不符**（`cards_engine.json × cards_raw.json` 按 `uniqueId` join）。
- 436 个 0 里：**276 个是 GUID 形 id**（其中 **81 个**同时带 `refNameId`，即变体/NC），其余 160 个是普通前缀 id。

---

## 二 静态消费点普查（命令 + 0 命中）

```bash
# 1) 我们的工程：只有注释 + DTO 字段，0 读取
grep -rn "cardShortId" /d/4/Unity/MyGame/Assets --include=*.cs
#   4 行 = DeckRules.cs:267(注释) / CardDatabase.cs:154,158(注释+字段) / CardDatabase.cs:318(注释串)
# 2) 原版类桩：只有字段声明
grep -rn "cardShortId" /d/2/Warpforge_code/Scripts/Assembly-CSharp
#   1 行 = RawCardScript.cs:9
# 3) 反编译方法体：按名字 0
grep -rn "cardShortId" /d/2/tools/decomp_full | wc -l        # 0
# 4) 字符串字面量：0（⇒ 没有按名字的反射 / JSON 键 / 协议键）
grep -c "cardShortId" /d/2/tools/all_strings.txt              # 0
grep -c "cardShortId" /d/2/tools/il2cpp_out/stringliteral.json # 0
# 5) 二进制字符串：0
grep -a -c "cardShortId" /d/2/unity_run_ref/GameAssembly.dll  # 0
# 6) 全局元数据：1（字段名表本身，属正常）
grep -a -c "cardShortId" "/d/2/unity_run_ref/Warpforge_Data/il2cpp_data/Metadata/global-metadata.dat"  # 1
# 7) 解包资源 JSON：0
grep -rl "cardShortId" "/d/2/新解包资源/assets_full" | wc -l  # 0
```

> ⚠️ 第 3 条 **按名字** 0 命中**不能单独当证据**（IL2CPP 字段访问走偏移量）。真正的证据是下一节的偏移级普查。

---

## 三 偏移级静态普查（`+0x40`）—— 三级探针里能做到的最强一级

### 3·1 方法集与覆盖率

脚本：`工具/_w58_k12_full_scan.py`（主）· `_w58_k12_inherit_scan.py`（继承字段）· `_w58_k12_lambda_scan.py`（编译器生成 lambda）· `_w58_k12_typed_scan.py`（第一遍）。

```
python -I -X utf8 d:/4/Unity/工具/_w58_k12_full_scan.py
# methods: 158965  scanned methods: 484  raw-returning: 69
# mapped: 458 / 484  TOTAL reads: 0
```

- 方法集 = ① 类 `RawCardScript` 的全部实例方法（105，**105/105 有反编译体**）；② 签名里出现 `RawCardScript` 的方法（383）；③ 参数类型是「含 RawCardScript 字段的类」的静态方法。
- 跟踪四种来源后查 `*(int*)(<expr>+0x40)`：
  ① `this`（类为 `RawCardScript` 或其派生 —— 派生类 0 个）；
  ② 类型为 `RawCardScript` 的参数；
  ③ 从 RawCardScript 类型字段（**含继承**：`EntityScript.rawCardData +0x28`、`CardDeck.deckHero +0x40`、`AlternateArtCard.originalCard +0x48`、`CardScript.<referencedBaseCard> +0x280` 等 63 个类的字段）装入的局部变量；
  ④ 从「返回 RawCardScript」的 69 个方法（`get_rawCardData` / `get_OriginalCard` / `get_referencedBaseCard` / `GetTalentCard` 等）装入的局部变量；
  ⑤ 方法签名里是 `List<RawCardScript>` / `RawCardScript[]` / `Dictionary<RawCardScript,…>` 时的 `get_Item` / `get_Current` 结果。

**结果：0 处**读 `RawCardScript.cardShortId`。

### 3·2 命中的 `+0x40` 都是别的对象（逐类核对）

第一遍文本扫描命中 **375 行 / 46 个文件**；再对**编译器生成的 lambda / 嵌套类**补扫一遍（同一批方法 + 26 个泛型/嵌套；该遍输出 301 行命中，多数与第一遍重合），逐条看**没有一条**是 `RawCardScript+0x40`。按成因分四类（各举一例）：

| 成因 | 例子 | 真相 |
|---|---|---|
| `param_1+0x40` 是**声明类自己**的字段 | `CardDeck__AddCard.c:23` `uVar3 = *(param_1 + 0x40)` | `CardDeck.deckHero`（`CardDeck` 的 `+0x40`），不是卡 |
| 访问的是**别的类型**的 `+0x40` | `AlternateArtPanel__Initialize.c:67` `iVar7 = *(int*)(lVar3 + 0x40)` | `AlternateArtCard.style`（枚举），随后传给 `SetStyle` |
| `+0x40` 是 `char` 而不是 `int` | `RawCardScript__IsValidTarget.c:28` `*(char*)(param_2 + 0x40)` | `CardScript` 上的一个 bool（参数类型是 `CardScript`） |
| 委托 / 组件 / 集合内部字段 | `DraftEventBase…PickPack_b__0.c:68`、`PreloadPack…` `*(lVar+0x40)` | 委托对象的字段 / `List` 内部字段；不是卡 |

最像的三条也逐一排除：
- `BattleManager__GetChoiceOptions.c:42` `iVar1 = *(int*)(lVar8 + 0x40)`：`lVar8` = `RawCardScript.choicesDefinition`（`+0x2A0`，`TargetCriteria`），`+0x40` 是**可选目标数**；
- `AlternateArtPanel__Initialize.c:67`：见上，`AlternateArtCard.style`；
- `AbilityLogic__GetCardToSummon.c:73`：`*(manager + 0x10)` 再 `+0x40`，是管理器内部对象的 vtable 槽。

### 3·3 覆盖率与已知盲区（如实写）

- 关键卡类覆盖率≈100%（§〇）；全游戏程序集约 **77%**（15,917 个 Assembly-CSharp 方法中 12,243 个有反编译体）。
- **盲区 1（可量化，已排除）**：26 个泛型 / 嵌套方法没有可映射的 `.c`（`DraftEventBase<TS>` 的 24 个 + `Enchantments.CardEffectToCard<T>` 的 2 个）。逐个人工看过：它们的 `+0x40` 全是委托调用 / 字典值指针，且 `CardEffectToCard` 系列**没有任何 `+0x40`**。
- **盲区 2（无法静态排除）**：先 `object`/`PlayerItem` 再**向下转型**成 `RawCardScript` 读字段的方法 —— 机器码里没有类型痕迹，方法签名里也不会出现 `RawCardScript`。本轮对全库 `*(int*)(param_N+0x40)` 做了穷举（329 处），逐条核过参数类型，**没有一条**的参数可能是卡（`param_2/param_3` 的 43 处也逐条看过）。
- **盲区 3（工具缺陷）**：`decomp_full` 对**编译器生成的 lambda** 存在**文件名与函数体不一致**（实测：`CardCollectionTab.__c___GetCollection_b__8_1.c` 里装的是 `AlternateArtCardCollectionTab_<>c__<GetCollection>b__11_1`）。⇒ lambda 级结论只当**旁证**，决定性结论来自具名方法。

---

## 四 三级探针逐级结果（按 §3.1 配方）

| 级别 | 配方要求 | 本轮实际 | 结果 |
|---|---|---|---|
| **L1 运行期改写-观察** | 改 mod，给所有 `RawCardScript.cardShortId` 写哨兵值，进出菜单/收藏/卡组/商店/战斗 | ⛔ **未跑**（任务明令不跑 Unity / 不起游戏） | 未取得证据。它仍是**唯一能正面排除「UI 运行时消费」**的一级 |
| **L2 网络/存档字节对照** | Hook `SendPlayerAction` / `PUNReceivePlayerAction` / `CardDeck.Serialize` / `AzureDeckInfo`，dump payload | **静态侧已做**（见下） | 三条通道全部指向 `uniqueId` / `uniqueInGameID`；**没有**出现 `cardShortId` 的迹象；协议键名里也没有 `shortId` |
| **L3 类型感知反编译** | `il2cpp_out/script.json` + cpp2il / Il2CppDumper 的类型化反编译，搜 `RawCardScript.cardShortId` 的 `ldfld` | **cpp2il 未安装、本地无类型化产物**；改用「方法签名类型 + 字段/取值数据流」等价扫描（§三） | 0 读点。**不是**严格的 `ldfld` 级证明 |

**L2 静态侧的判据（可复现）**

1. 牌组串 = `uniqueId`：`SerializedPlayerDeckData.SerializedCard { public string id; public int tier; }`（类桩）；`DeckAndWarlordData__GetSerializedDeckData.c:47` 读 `RawCardScript + 0x38`（= `PlayerItem.uniqueId`）再 `Select(...)`。
2. PvP：`BattleCommsManager.PUNReceivePlayerAction(int actionType, int actingCardID, int targetCardID, …)`（类桩 `BattleCommsManager.cs:149`）—— 是**局内实体号**，不是卡号；上一轮已证 = `uniqueInGameID`。
3. 本地存档：`C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/` 下 6 个文件逐个扫过 —— `PlayerDecks39.dat`（1,203 B）只有设置键（`loginName/loginPassword/deviceId/languageVersion/...`），没有任何卡号列表；`eventcache` 是 Unity Analytics `windowOpen/gameEnded`；`RemoteConfigCache.json` / `PendingPurchases_*.dat`（2 B）/ `Player.log` / `Player-prev.log` **都没有卡号列表**。
4. 字符串字面量：`cardShortId` / `shortId` **0 条**；只有 `{{ CardID = {0} }}`、`{{ cardId = {0} }}`、`actingCardID` 等，均属 PlayFab 参数模板 / 局内实体号。

---

## 五 数据形态：实测结构（用途属推断，已明确标注）

> 下面的**每一行数字都是本轮现算**，源 = `资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张当前 build SO）。

1. **全局顺序号，按阵营成块**（`shortId` 升序的阵营游程 = 94 段，主要块）：

   | 阵营（`CardArmy`） | shortId 区间 | 块内连号率 |
   |---|---|---|
   | Ultramarines(10) + Goff(20) 混合 | 1–97 | 高 |
   | Sautekh(40) | 98–155 | 48/48 |
   | BlackLegion(50) | 156–205 | 51/51 |
   | SaimHann(30) | 206–267 | 53/53 |
   | Leviathan(60) | 268–319 | 52/52 |
   | TauEmpire(70) | 369–443 | **74/74** |
   | Sororitas(80) | 323–656（多段） | 72/74 |
   | Genestealers(90) | 657–731 | **74/74** |
   | AstraMilitarum(100) | 154–799（多段） | 69/74 |
   | DarkAngels(110) | 328–857（多段） | 60/64 |

   逐阵营的「`shortId − m_Name 前缀号`」中位偏移：Tau 362 · Sororitas 578 · Genestealers 650 · AM 725 · DA 792 —— 即**新阵营是按「阵营块起点 + 阵营内序号」排的**。
2. **`exp65` 之后停止新分配**：

   | 扩展包 | 卡数 | 非零 | 其中「全局唯一值」 | 「与别人同号」 |
   |---|---:|---:|---:|---:|
   | 0–65 | — | — | 几乎全部 | 仅变体 |
   | 70 | 12 | 1 | **0** | 1 |
   | 75 | 60 | 2 | **0** | 2 |
   | 80 | 12 | 1 | **0** | 1 |

3. **复制资产继承原值**：19 条带 `refNameId` 的变体里 **14 条**与基准卡 `shortId` 相同（`_tutorial` / `_EASY` / `_enchantment` / `_startinghand`）；连**不同卡**也同号，共同点是能力模板相同（`DA3`↔`SW3`=855、`DA1`↔`SW1`↔`DA75`=856、`DA2`↔`SW2`=857、`UM76`↔`UM87`=481、`ASH74`↔`ASH76`=575）。
4. **不是身份键**：48 组重复（103 张），含跨阵营不同卡。
5. **不是 `uniqueId` 短形式**：与 `uniqueId` 末尾数字相等 1/904。
6. **不是 PnP 编号**：`DA17` Deathwing Terminator = PnP 文件名 20 / `m_Name` 前缀 14 / `cardShortId` 805。
7. **不参与排序**：与 `path_id`（ρ=-0.030）、卡名字母序（ρ=-0.086）、`m_Name` 前缀（ρ=-0.034）都无关；收藏页的排序/筛选 lambda 读的是 `showInCollectionOptions(+0x48)`、`cardType(+0x94)`、`IsAlternateArt()`，不是它。

> **推断（明确标注：不是判据）**：这些形态最像一个**内容编写期的全局顺序号 / CMS 行号**：老内容按「阵营登场顺序 + 阵营内序号」连续分配；之后字段停更（新卡不再分配）；由复制模板产生的新资产把旧值一起复制过来。**它对不上任何客户端身份/网络/存档概念。**

---

## 六 与同族概念的关系（`cardShortId` 没有被搞混，但文档里有两处要留意）

| 概念 | 是什么 | 与 `cardShortId` 的关系 | 结论 |
|---|---|---|---|
| `uniqueId` | 卡的主键字符串（`PlayerItem.uniqueId +0x38`；`GetID()` 返回它） | 数值后缀相等 **1/904**；牌组串 / 商店 / 平衡性改动 / 云端牌组全用它 | **完全不同**；⛔ 别互转 |
| `card_ids.json` | 我们的真值映射：`mapping`（uniqueId→卡名，1,351 条）+ `poolTrueIds`（历史迁移表，`poolIdsAreTrue=true`） | 里面**没有** `cardShortId` 这一维 | 无关；⛔ 别把 `cardShortId` 塞进 id 映射 |
| `pick_id` / `pickIds` | **我们自己的**回放/联机字段：`BattleContext.ChooseCardIds : Queue<string>`（记录「选牌面板选了哪张」的 `uniqueId`） | 名字里有 id、值是字符串 uniqueId | 无关；⛔ 别当卡号 |
| `refNameId` | 本地化键/卡名引用（空 = `Card_Name/<id>`；非空 = `Card_Name/<refNameId>`） | 19 条非空变体里 14 条与基准同 `shortId`（复制痕迹） | 不相等，但**变体关系**上有弱相关 |
| `m_Name` 前缀号（`14 DarkAngels-…`） | 原版**资产名**里的阵营内序号 | 新阵营块里「`shortId` ≈ 阵营块起点 + 前缀号」（中位偏移：DA 792） | 结构相关，**不等价**；⛔ 别用前缀号反推 |
| PnP 编号（`A970`：`Warpforge_<NN>_<名字>.png`） | 印刷品文件名编号 | 与 `shortId` **0 重合**（旧账 0/763；本轮 `DA17` 单点复核 20 vs 805） | 完全不同 |
| `enchantmentCardID`（`ActiveEnchantment` / `TriggerAbility` 参数） | 局内附魔卡的实体号 | 不是 `shortId`（0 读点；`+0x40` 在 `CardScript` 上是 bool） | 无关 |

**订正建议（文档，不改）**
1. `RuleEngine/Data/CardDatabase.cs:154` 的注释「1…914、860 种、**294 张为 0**」是**混快照数字**：当前 SO 快照是 **860 种 / 436 张为 0**，池内是 **827 种 / 287 张为 0**。建议改成「全量 SO：860 种/436 零；池内：827 种/287 零」。
2. `gen_cards_engine.py:3132` 的「与 `A970` 那套 0/763 重合、全库 0 处读取点」仍然成立，可以补一句「判定级普查见 `普查产出_1020/W58…`」。

---

## 七 正本 §七·2 第 8 条 —— 建议替换文字

> 建议：**把它从「没查清」里移出**（条目 8 换成下面这段），并把「做新卡怎么写」落到 §1·5。理由：对复刻/做新卡有影响的结论已经全部确定；剩下的只是「原作者意图」，本地没有判据、也不影响我们。

```markdown
8. **`cardShortId` —— ✅ 2026-10-20 收口：客户端 0 读点；它是内容编写期的全局顺序号，不是 id**
   字段 `RawCardScript.cardShortId`（**public int**，`RawCardScript.cs:9`；偏移 **`+0x40`**，`dump.cs:23247` / `il2cpp.h:16414`；全库**无派生类**、无 getter/setter）。
   **客户端 0 读点（强证据）**：
   - 按名字：`decomp_full` 25,096 个方法体 **0**；类桩仅字段声明 1 处；`all_strings.txt` / `stringliteral.json` / `GameAssembly.dll` 字符串 / `assets_full` JSON **全 0**；`global-metadata.dat` 只有字段名 1 处。
   - 按偏移：对 **484 个「签名或自身类型含 `RawCardScript`」的方法**逐个查 `+0x40` 访问 —— **0 处**是 `cardShortId`；命中的 `+0x40` 全是别的对象（`CardDeck.deckHero` · `AlternateArtCard.style` · `TargetCriteria` 计数 · `CardScript` 的 bool · 委托字段）。关键类反编译覆盖率 `CardScript` 423/423 · `RawCardScript` 105/105 · `BattleManager` 647/649。
   - 通道：牌组串 / 云端牌组用 `uniqueId`（`SerializedPlayerDeckData.SerializedCard.id` 是 string）；PvP 用 `uniqueInGameID`；本地 `PlayerDecks39.dat` 只有设置项；协议里没有 `shortId` 键。
   **实测形态**：全局顺序号 **1…914**，按阵营成块、块序 = 阵营登场顺序（Tau 369–443 · Sororitas 576–656 · Genestealers 657–731 · AM 723–799 · DA 791–857；块内 ~90% 连号）；**exp65 之后不再新分配**（exp70/75/80 的 4 条非零值全部与旧卡同号）；变体/复制资产**继承**原值（19 条带 `refNameId` 的变体里 14 条同号）。
   ⇒ **不是唯一身份键**（48 组重复 / 103 张）、**不是 `uniqueId` 短形式**（1/904 相等）、**不是 PnP 编号**、**不参与 UI 排序**。
   **做新卡**：照抄同源卡的取值或填 0；⛔ 不要赋予语义。
   **仍不可证**：原作者（编辑器 / CMS / 后端）拿它做什么 —— 客户端与本地资产 0 读点；要证只能跑**原版运行期探针**或 **cpp2il 全程序类型化反编译**。
   判据全文 → `资料/普查产出_1020/W58_卡牌ID线_K12_cardShortId.md`。
```

**§1·5 同步建议（可选）**：把 `1·5` 的「运行时用途本正本没查到」改成「**客户端 0 读点（判据见 §七·2 第 8 条）**；写新卡照抄同源卡或填 0」。

---

## 八 没查清的部分（缺什么 + 去哪拿）

1. **原作者的用途（编辑器 / CMS / 后端）** —— 本地**没有判据**：玩家 build 不含 Editor 程序集（`Warpforge_Data/` 下无 `Managed/`），后端代码不在本机，历史抓包不存在。
   - 能证它的两条路：
     ① **运行期探针（L1/L2）**：mod 副本给所有 `RawCardScript.cardShortId` 写哨兵 → 走主菜单/收藏/卡组编辑/商店/战斗，看有没有错位、错筛、导入导出失败、网络/日志出现哨兵；Hook `BattleCommsManager.SendPlayerAction / PUNReceivePlayerAction`、`CardDeck.Serialize/DeserializeDeckString`、`AzureDeckInfo` 构造，dump payload 对照。
     ② **类型感知反编译（L3 终局）**：装 `cpp2il`（或对现有 Ghidra 工程跑 Il2CppDumper 的 `ghidra_with_struct.py`，用 `il2cpp_out/il2cpp.h` 的 `RawCardScript_Fields` 打类型），全程序搜 `RawCardScript` 类型上的 `+0x40` `ldfld`；0 命中即「客户端死字段」终局证明。
2. **盲区 2（先转 `object`/`PlayerItem` 再下转 `RawCardScript` 的读法）**：本轮用「全库 329 处 `*(int*)(param_N+0x40)` 逐条核参数类型」的方式排除了可见部分；严格排除需要 ① 或 ②。
3. **`decomp_full` 的 lambda 文件名/函数体错位**（§3·3 盲区 3）：建议另立账（工具线）修 Ghidra 导出脚本；不影响本条结论。
4. **`CardDatabase.cs:154` 的陈旧数字**（294/860）与池内实测（287/827）不一致 —— 建议由主对话并入订正（本轮 ⛔ 不改 `.cs`）。

---

## 附 本轮自检

- 未改任何产品代码 / 数据 / 正本 / `CLAUDE.md` / `项目任务.md` / 任务线总表；未跑 Unity / 自检 / player / git / 截图。
- 只新建：本报告 + 5 个一次性只读脚本（`工具/_w58_k12_*_scan.py`，LF 行尾，均 `python -I -X utf8` 运行）。
- 数据现算全部来自：`资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351）· `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（1,127）· `d:/2/tools/il2cpp_out/{dump.cs,il2cpp.h,script.json}` · `d:/2/tools/decomp_full`（25,096 个方法体）。
- 静态扫描可复现命令与输出摘要见 §二 / §三。
