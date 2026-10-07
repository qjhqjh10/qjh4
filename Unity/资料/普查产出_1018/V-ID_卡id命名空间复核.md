# V-ID · 卡 id 命名空间复核（只读）

> 复核对象：`资料/普查产出_1018/R-ASSET_资产族现核.md` **§6·①**（第 193–238 行）那条「原版卡 id 命名空间 ≠ 我们 `card_ids.json` 的命名空间」。
> 本轮**只读、只判、不改**：没跑任何生成器、没跑 Unity、没动 git 写操作、**一个 id 都没回填**。
> 落笔时间：2026-10-18。判据全部在**本地**（`d:/2` 解包资源 + 本仓数据 + `E:/BaiduNetdiskDownload/`）。

---

## 1. 一句话结论

**成立。类别 = (b)「两套命名空间本来就不同，而引用侧把原版 id 当成了我们的 id」——但底下压着 (a) 的一半：`card_ids.json` 的 `id` 列本身就**不是**游戏卡 id（它是 PnP 卡片图文件名的编号），而它的 `note` 自称「PnP 卡表编号=游戏卡ID」是错的。两者合起来 ⇒ **预组牌 / 教程牌 / 卡组编辑里那些「照原版抄的卡」全部（或绝大部分）解析成了另一张卡**。置信度：**高**（0/17 逐条反证全对不上，且 4 个独立结构性检验全部指向同一侧）。

**关键量化读数**（下面 §2 逐条给证据）：

| # | 检验 | 读数 | 若两套命名空间一致应当是 |
|---|---|---|---|
| A | 「原版 `targetId` → 名字」17 条，与 `card_ids.json` 逐条比 | **0 / 17 一致** | 17/17 |
| B | 「被打出的牌在本方牌组里」 | 原版 id **17/24** · 我们表 **3/24** | 我们表也应是 ~17/24 |
| C | `DT <阵营> R<稀有度>` 的 `targetId` 经我们表解出的稀有度 vs 该掉落表的稀有度 | **70/315 = 22.2%**（低于瞎猜「common」的 29.3% 基线） | ≈100% |
| D | 236 副预组牌的 `heroId`（它**必然是**一张督军卡的 id）经我们表解出的卡是不是督军 | **73/236 = 31%** · 基线 5% | 236/236 |
| E | 103 副预组产物的 `cardIds` 里混进了多少张**督军卡**（督军不可能进牌组） | **43 副 / 84 张** | 0 |
| F | 已知真值的 17 条「原版 id↔卡名」，抽卡包「包名 = 包内某张卡」反测 | **3/3 命中**（可测的只有 3 个） | —— |

---

## 2. 三条旁证逐条复现

### 旁证① `DT *` 掉落表 `targetId` → 名字，17 条全对不上 —— ✅ **原样复现，且是 0/17 逐条都不一致**

**怎么复现的（不跑生成器，纯读 JSON）**：

1. `d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/DT *.json`
   （+ `bundle_duplicateassetisolationso_assets_all/…/DT Ultramarines R{3,4}.json`）
   —— 每条 `obtainableItems[]` = `{genericObjectReference:{m_FileID:2,m_PathID}, targetId:"UM23", reference:{…}}`。
   **共 329 个 pid，`pid → targetId` 是 1:1（无一个 pid 映射到两个 targetId）**，覆盖 `UM/GOF/EC/SW` 四个阵营。
2. `d:/2/新解包资源/assets_full/bundle_tutorialso_assets_all/MonoBehaviour/Warpforge_TutorialStage{1..6}.json`
   —— `turnScriptedData[].scriptedActions[].actionType` **字面就是卡名**，形如 `"PlayCard (Bladeguard Lieutenant)"`
   （还有 `"PlayCard (Bladeguard Lieutenant (tutorial))"` 这种**教程专用变体卡**，见下），
   同一记录的 `scriptedActionData[].actingUnit.m_PathID` 就是该卡的 pid。
   **共 107 行 PlayCard，其中 75 个互不相同的 pid，17 个能在 DT 里查到。**
3. 两边按 **pid 相等**联立（`m_FileID` 一个是 2 一个是 3，但 **17/75 的 pid 命中率不可能是巧合** ⇒ 两者解析到同一个 CAB，即缺的 `allcards`）：
   **这就是「原版 id ↔ 卡名」的唯一本地直接来源。**

**读数（17 条真值 vs `card_ids.json`）**：

| 原版 id | 真名（原版自己标的） | 我们表说这个 id 是 |
|---|---|---|
| `UM1` | Scout | Marneus Calgar |
| `UM2` | Octavio Infiltrator | Master Tactician |
| `UM4` | Assault Intercessor | Master of the Fleet |
| `UM5` | Primaris Reiver | Varro Tigurius |
| `UM6` | Primaris Intercessor | （表里是空号） |
| `UM8` | Eliminator | （表里是空号） |
| `UM13` | Primaris Chaplain | Firstborn |
| `UM14` | Honour Guard | Primaris Intercessor |
| `UM23` | **Bladeguard Lieutenant** | **Spear of Macragge** |
| `UM33` | Pariah Vanguard | （表里是空号） |
| `UM34` | **Point-Blank Shot** | **Bladeguard Lieutenant** |
| `UM39` | Inspired Retribution | Devastator Centurion |
| `UM56` | Intercessor Sergeant | （表里是空号） |
| `GOF1` | **Grot** | **Grukk Face-Rippa（督军）** |
| `GOF4` | Slugga Boy | Grot |
| `GOF9` | Shoota Boy | Stormboy |
| `GOF32` | Grot Bomb | Ardshell Gurk |

⇒ **0 / 17**。R-ASSET 举的那一对（pid `-8500042654006139415` = 教程 S6 AI 打的 `Bladeguard Lieutenant`，
DT 标 `UM23`；我们表 `UM23` = `Spear of Macragge`、`UM34` 才 = `Bladeguard Lieutenant`）**三段逐字复现**。

**比它多出来的两条交叉验证**（它没做）：

- **「×3 对 ×3」**：教程 S2 玩家连打 `Grot`×3 · `Slugga Boy`×3 · `Shoota Boy`×3 · `Grot Bomb`×3；
  而 **打这些牌的正是 `Orks_Deck0_Tutorial2_Ghazghkull` 那副牌，它的 `cardLibraryIds` 里恰好
  `GOF1`×3 · `GOF4`×3 · `GOF9`×3 · `GOF32`×3** —— **四张卡、四个 id、连重数都对上**（原版侧 4/4）。
  按我们表：`GOF1`=督军 Grukk、`GOF9`=Stormboy、`GOF32`=Ardshell Gurk，**三张对不上**。
- **稀有度分布**（见 §1 检验 C）是**与「名字」完全无关的第二把尺子**，结论同向：两套编号之间**没有相关性**。

### 旁证② 「被打出的牌要在打它那方的牌组里」 —— ✅ **方向复现；分母与它给的不同（我这边是 17/24 vs 3/24）**

**怎么复现的**：把 §旁证① 的 (关卡, 哪一方, 卡名, pid) 与 `TUT_ROLE`（`工具/gen_prebuilt_decks.py:130-140`）
联立出「这一方这一关用的是哪一副 SO」，再读那 12 个 SO 的 `cardLibraryIds`。

**读数**：

```
原版 targetId 在牌组里: 17/24
我们表(按名字) 在牌组里:  3/24      ← 其中 1 条是巧合（GOF4=Grot 恰好也在这副 Ork 牌里）
我们表里存在该名字的号: 17/24      ⇒ 不是「查不到」，是「查到了别的卡」
```

**与 R-ASSET 的 22/31 vs 4/31 的差别（如实说）**：它按 31「行」算，我按 **24 个互不相同的 pid、并剔除
`(tutorial)` 后缀的教程专用变体卡**算（那类卡的 pid **本来就不在 DT 里**，算进去只会稀释分母）。
**两条口径的结论同向、比例同量级**，我复现的是它的**结论**，不是它的**分母**。

### 旁证③ `Orks_Deck0_Tutorial2_Ghazghkull` 的 `GOF1`×3 / `UM_SK_1` 的 `UM47` —— ✅ **原样复现**

- `bundle_prebuiltdecks_assets_all/MonoBehaviour/Orks_Deck0_Tutorial2_Ghazghkull.json` 的
  `cardLibraryIds` **逐字**含 `GOF1`×3（还有 `GOF9`×3 · `GOF32`×3 · `GOF4`×3）。
  我们池 `cards_engine.json` 的 `GOF1` = **`Grukk Face-Rippa`，`type=hero`** ⇒ **一副牌三张督军**（结构上不可能）。
- `UM_SK_1.json`：`deckName="Spear of Macragge"` · `deckHero.targetId="UM47"` · 12 张牌。
  我们表 `UM47` = **`Tactical Insight`（计策卡）** ⇒ 督军位放了一张计策。
  ⚠️ 顺带：`warlord_ids.json` 说 `UM47 = UM_Warlord_Uriel Ventris`，
  而教程 SO `Ultramarines_Deck0_Tutorial6_Uriel` 的 `deckHero.targetId` **也正是 `UM47`**（SO 名里写着 `Uriel`）
  ⇒ **这两条原版来源互相印证 `UM47 = Uriel Ventris`**，我们表错。

---

## 3. 我们这套 id 的来路（生成器 · 源头 · 为什么是这样的编号）

**它的编号 = `d:/2/Warpforge部队卡片/<阵营>/<分类>/Warpforge_<N>_<卡名>.png` 里的那个 `N`。**
一路可追、四段都有实物：

1. **源头不是游戏，是一部粉丝 PnP（print-and-play）同人卡片集**。
   `d:/2/Warpforge部队卡片/` 根目录里同时放着 `Warpforge_Offline_Rulebook_1_5-3_{英文原版,中文翻译}.md`
   （**该规则书自己写着 `Not official. Fan project.`**）· `card.html`（打印模板）· `卡牌数据总表.md`
   （自己写着「来源：`D:\2\Warpforge部队卡片\` 各阵营 PnP 卡面图（视觉模型 OCR，2026-08-16）」）。
   同一部 PnP 在 `E:/BaiduNetdiskDownload/Warpforge带编号/`（用户 2026-09-24 提供的另一版，按 `Core/`+DLC 分好目录）。
2. **编号印不在卡上**：本轮亲读一张成品卡图
   `d:/2/Warpforge部队卡片/Ultramarines/2天赋/Warpforge_23_Spear-of-Macragge.png` ——
   卡面只有卡名 / 阵营 / 兵种 / 效果 / 费用 / 底排四个数值，**没有任何编号**。
   ⇒ 那个 `23` 是**文件名层面的序号**（同人整理时排的），**卡图本身给不出它**。
3. **它的「值」侧另有来源**：`card_stats.json` 的 `ocrSrc` 逐张指着
   `d:/2/Warpforge部队卡片/Orks/Core/Warpforge_04_Grot.png` 这类路径 ⇒
   **`card_ids.json` 的 key 就是那个文件名里的号、value 就是那张图上的卡名**（或 GSC 那份的 `ocrName`）。
4. **老工程自己写明了「这是 OCR 扩展点、编号体系是 PnP 的」**：
   - `d:/warpforge/tools/export_game_data.py:12`：「`data/card_ids.json` — 卡 ID→卡名映射表 (**空, OCR 扩展点**)」，
     第 328-329 行**写进去的就是一个空 `{"mapping":{}}`**；
   - `d:/warpforge/scripts/deck_collection.gd:3`：「卡 ID→卡名为**服务端数据本地缺失**… **OCR 数据到位后 (card_ids.json)** 显示卡名」；
   - `d:/warpforge/Warpforge备份/0826_batch1_data/battle.gd:1868`：「模板卡组: 卡 ID 映射已建
     (**card_ids.json, PnP 编号体系**)」。← **这句是全部线索里最直白的一句**。
5. **本仓自己写的口径**（同一条假设，三处）：
   - `数据/游戏数据/card_ids.json:1000` 的 `note`：「(2026-08-17 **自建: PnP 卡表编号=游戏卡ID** 偏移0 Core/扩展区段…)」；
   - `资料/预组卡组_原版规格.md:565` · `资料/原版预组牌_核对.md:179` 复述同一条；
   - `资料/历史/阵营推进_批次记录_0913-14.md:244`：「`card_ids.json` 的 id↔卡名是**推出来的**…
     拿 PnP 卡图核过：**818 处逐字一致 · 19 处写法差异 · 159 处编号对不上**」。
     ⚠️ **那次「核」是拿 PnP 的号去对 PnP 的表，属于自证** —— 它只验了「值（卡名）抄得对不对」，
     从来没验过「号是不是游戏的号」。
   - `工具/gen_cards_engine.py:541-556` 的结论写着「**id↔卡名 基本可信**（不是逐条验过）」⇒ 于是
     第 585 行 `load_ids()` 读它、第 600 行 `pick_id()` **把它当成「原版 id」发给卡池**
     （`gen_cards_engine.py:855` 产物品的描述就写着「id 另取 `数据/游戏数据/card_ids.json`，对不上的自造」）。

**所以编号为什么长这样**：它是**同人 PnP 集里「督军 → 它那张天赋 → 下一个督军 → …」的自排顺序**
（`资料/原版预组牌_核对.md:246-252` 记着这条自排规则，2026-09-24 用它**反向改过同人图集的文件名**）。
**游戏自己的号是从「普通部队」起排的**（见 §4 检验 C 的理由），两套起点就不同 ⇒ 整体错位。

⚠️ **补一条本轮新查实、直接推翻「偏移 0」那句 note 的**：
`E:/BaiduNetdiskDownload/Warpforge带编号/Ultramarines/Core/` 里今天摆着的
`Warpforge_23_Spear-of-Macragge.png` / `Warpforge_34_Bladeguard-Veteran.png` / `Warpforge_47_Tactical-Insight.png`
**与 `card_ids.json` 的编号一一相同** ⇒ **那份「带编号」目录是拿我们这张表反推着改的名**
（`资料/原版预组牌_核对.md:246` 自己记着「2026-09-24 已执行的改名」），
**不能当独立旁证**（这是本轮唯一要提醒调度台的「看起来最硬、其实是循环」的判据）。

---

## 4. 原版那侧 id 的来路（逐个字段/资产）

| 来源 | 位置（`文件:行号` / 资产路径） | 字段 | 说明 |
|---|---|---|---|
| **预组牌 SO**（主源） | `d:/2/新解包资源/assets_full/bundle_prebuiltdecks_assets_all/MonoBehaviour/*.json`（**236 份**） | `cardLibraryIds[]`（卡组）· `deckHero.targetId`（督军）· `deckId`/`deckName`/`gameMode`/`isPracticeDeck` | **原版自己写的卡 id**。`m_Script` = `PrebuiltDeck` 类 |
| **教程关 SO** | `…/bundle_tutorialso_assets_all/MonoBehaviour/Warpforge_TutorialStage{1..6}.json` | `playerDeck`/`aiDeck`（PPtr）· `turnScriptedData[].scriptedActions[].actionType`（**卡名明文**）· `.scriptedActionData[].actingUnit`（pid） | **卡名 ↔ pid ↔（DT 的）原版 id** 的唯一本地直接桥 |
| **掉落表 `DT *`**（本轮第一份「原版自己标的 id」） | `…/bundle_cosmeticsso_assets_all/MonoBehaviour/DT <阵营> R<档>.json`（33 份）+ `…/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/DT Ultramarines R{3,4}.json` | `obtainableItems[].targetId` + `{genericObjectReference,reference}`（pid）· `cardArmy`/`cardRarity` | **329 个 pid → 300 条 `(army,rarity,targetId)`**，覆盖 UM/GOF/EC/SW；`pid↔targetId` **1:1** |
| **我们的导入产物** | `d:/4/Unity/数据/游戏数据/decklists.json`（236 副，`decklists[]`） | `heroId` · `cardIds[]` | **就是上面那批 SO 的转写**（`d:/warpforge/tools/export_game_data.py` 导出）⇒ 它带的是**原版 id** |
| **抽卡包** | `…/bundle_draftpacks_assets_all/MonoBehaviour/*.json`（**882 份**） | `m_Name`（包名，是**卡名**）· `cardIds[]`（3 张）· `packId` | 见 §6 检验 F。**这是一条还没被用起来的、可能是最大的一条「原版 id↔卡名」线索** |
| **`warlord_ids.json`（我们的表，但 key 侧是真的）** | `d:/4/Unity/数据/游戏数据/warlord_ids.json`（57 条） | `{督军 id: 督军名}` | 🔴 **key 集合 = 236 副牌 `heroId` 去重后的集合，逐族完全吻合**（UM `46/47/48/65/76` · AM `1/2/3/66` · GOF `27/28/29/75/77` · ASH `1/2/3/74/76` …）⇒ **它用的是原版命名空间**；⚠️ **它的 value（名字）不可靠**（`TL74` 那条是坏的，`gen_prebuilt_decks.py:262-266` 已记） |
| **两个「看着像、其实不能用」的** | `…/bundle_menus_assets_all/MonoBehaviour/Campaign Node Data*.json` 的 `NodeId`（`UM44`/`UM2`…）· `draftpacks` 的 `packId`（`UM_SpearofMacragge`） | —— | `NodeId` 是**战役节点**号，不是卡（`Campaign Node Data.json` 那条 `NodeId=UM44` 的奖励是 `Booster Pack Ultramarines`）；`packId` 是包 id |

**结构性事实（用来判断「原版号长什么样」）**：`DT Ultramarines R1`（**R1 = 最低稀有度**）里的 id 是
`UM1 · UM2 · UM3 · UM4 · UM6 · UM7 · UM8 · UM9 · UM10 · UM11 · UM14 · UM15 …` ——
**原版的号是从「普通部队」起排的**（`UM1`=Scout、`UM2`=Octavio Infiltrator、`UM4`=Assault Intercessor …，
全是 common）。而我们表把 `UM1/UM3/UM5` 排成了 **Marneus Calgar / Uriel Ventris / Varro Tigurius 三张督军**
⇒ 一副「普通掉落表」里会出现三张督军与多张天赋，**结构上不可能**。这是 §1 检验 C 之所以成立的道理。

---

## 5. 引用点清单（谁在用它 · 只列不改）

**A. 读 `card_ids.json` / 把原版 id 当我们的 id 用**

| 文件:行 | 用途 |
|---|---|
| `工具/gen_cards_engine.py:555` `IDS_SRC` · `:585 load_ids()` · `:600 pick_id()` · `:855`（产物 note） | **卡池 `cards_engine.json` 的 id 就是从这里发的** ⇒ 「我们的 id」整族继承了这套编号 |
| `工具/gen_prebuilt_decks.py:191 build_matcher()` · `:210 match()` · `:215 nm = mapping.get(cid)` | 原版 id →（`card_ids.json`）→ 卡名 →（卡池）→ 我们的 id。**当卡名能精确命中时它是恒等映射**（因为卡池 id 本就是从同一张表发的）⇒ **产物里的 id 字符串和原版牌组一模一样**，但在我们游戏里指着**另一张卡** |
| `工具/gen_prebuilt_decks.py:258 hero_match()` · **`:264 c = by_id.get(hid)`** | 🔴 **最直接的那一处**：拿原版 `heroId` **直查我们卡池**（注释自己写着「我们的卡池里 973/1126 张就是原版 id」）。命中就返回，**不看名字** ⇒ 静默取到错卡；只有当那张卡**恰好是 hero 型**时才返回，否则回落 `warlord_ids` 名字表（这就是它 31% 蒙对的原因） |
| `工具/check_prebuilt_decks.py:20-25` · `:85-92` · `:113 match()` · `:118` · `:265-267` · `:293` · `:324` · `:341-345` · `:377` · `:400` | 核对报告（`match()` 规则的**唯一副本**，`gen_prebuilt_decks.py` 是照它抄的） |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs:7252 TestCardIds()`（`:7263` 起那张三组表） | 断言**只钉「同名卡分得开、按 id 反查回得来」**，id 值本身取自同一张表 ⇒ **自洽、不验真伪** |

**B. 下游产物 / 消费方（它们吃到的是上面那一步的结果）**

| 文件:行 | 用途 |
|---|---|
| `MyGame/Assets/RuleEngine/Resources/cards_engine.json` | 卡池；995 条原版形态 id（其中**编号来自 `card_ids.json`**）+ 131 条自造 slug |
| `MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json`（103 副） | 预组页那一池 |
| `MyGame/Assets/RuleEngine/Resources/tutorial_decks.json`（12 副） | 教程 6 关两方牌 |
| `MyGame/Assets/CardPresentation/Shell/PrebuiltDecks.cs:36/50/58/222/232` | 注释把 `heroId`/`cardIds` 写成「**我们的卡 id**」（`:58` 还写着「按原版卡表原序」） |
| `MyGame/Assets/CardPresentation/Shell/DeckSelectionPopup.cs:71` · `:692` | 预组页签 |
| `MyGame/Assets/CardPresentation/Battle/BattleDriver.cs:2231` · `:2266` · `:2350-2414` · `:2645` | 教程关卡读 `tutorial_decks.json` |
| `MyGame/Assets/RuleEngine/Core/TutorialScript.cs:323` | 关卡状态机 |
| `MyGame/Assets/CardPresentation/Editor/BattleScene.cs:13946` · `:13969` · `:13985` · `:13996` | 教程自检（断言「牌库顺序 == `tutorial_decks.json` 那一份」⇒ **顺序对，但牌可能是错的**） |
| `MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs:2687-2689` · `:2812` · `:2855` | 预组页自检（只验「条数/能读出来」） |
| `MyGame/Assets/CardPresentation/Editor/CollectionScene.cs:2714` | 收藏页的预组页 |

**C. 卡内其它按 id 命名的产物（改 id 会连带，本轮只记录）**
`数据/游戏数据/card_icon_plan.json` · `voice_lines.json` · `Resources/Art/cards/art_*.png`
（`资料/普查产出_1017/…/W_B26_卡id回填.md` 已经记过同族的连带面）。

---

## 6. 我找过的反证（有没有判据说我们这套本来就是对的）

**找到 4 条「支持我们这套是对的」的判据，逐条评估**：

| # | 反证 | 出处 | 评估 |
|---|---|---|---|
| 1 | 🔴 **正本明写「id 空间本来就是共用的，不需要映射桥」** | `资料/预组卡组_原版规格.md:211`：「`gen_cards_engine.py:541-547` 生成卡池时**已按名从 `card_ids.json` 抄了原版 id** ⇒ 卡池 1126 张里 973 张就是原版形态 id … **实测：产物生成时第一步纯 id 查表就中 130 副**」 | ❌ **这条本身就是待证命题**。那「中 130 副」正是 `hero_match:264` 的**错命中**（把原版号当我们的号直查）。本轮检验 D 给出它的真实命中率：**heroId 解成督军只有 73/236** |
| 2 | **同人「带编号」目录 820/843 逐字吻合** | `资料/原版预组牌_核对.md:230-234` | ❌ **循环**：那份目录（`E:/…/Warpforge带编号/`）的编号是**照我们这张表改的文件名**（同文件 `:246` 自己记着 2026-09-24 执行的改名） |
| 3 | **拿 PnP 卡图核过：818 逐字一致、159 处编号对不上** | `资料/历史/阵营推进_批次记录_0913-14.md:244` | ⚠️ **只验了「值」没验「键」**。而且它自己已经报出 **159 处编号对不上** —— 那正是本缺陷的可见症状，当年被归成「写法差异/资料不全」放过了 |
| 4 | **第三方复刻 `CardboardConsole` 的 id 也对不上** | `资料/第三方复刻对照_CardboardConsole.md:15-16` | ⚠️ **它压根不用序号**（纯 slug `wf_foundation_…`）⇒ 既不能证也不能否 |

**端到端断言有没有钉住 id 值？—— 没有。**（这是我觉得最该说清的一条）
本轮的搜索范围与结论：

- `MyGame/Assets/**/*.cs` 里 grep `GOF1`/`UM23`/`UM34` 等字面 id：**命中的全部是注释**（`UM34` 那一族 15 处注释，
  逐处看过，**没有一处是字符串比较**）⇒ 加不加、改不改都不影响断言。
- `RuleEngineTest.cs:7252 TestCardIds()` 是三组「同名跨阵营」的 id 断言，**右侧 id 取自同一张表** ⇒ 自证。
- `BattleScene.cs:13996` 断「第 1 关我方牌库 == `tutorial_decks.json` 的 `player.cardIds`」——
  **断的是产物与产物一致，不是产物与游戏一致**。
- `MainMenuScene.cs:2688-2689` 断「预组数据读到了 / 条数相等」—— 量的是**能读**。
⇒ **12 条自检 16,123 条断言全绿，与这条缺陷完全无关**（它没有真值可断）。**「自检绿不等于口径对」**（本仓原话）。

**我另外主动找的、可能翻案的两条，结果都是「不成立」**：

- **「是不是只错在 DLC 段、Core 段是对的？」** —— 否。反例全在 Core：`UM1/UM2/UM4/UM5/UM6/UM8/UM13/UM14/UM23`、
  `GOF1/GOF4/GOF9/GOF32` 都是 Core 号。
- **「换个固定偏移能不能对上？」** —— 不能。按 `heroId → 督军` 扫 `off=-8..+8`：
  `-2` → 23.7%、`0` → 29.2%、`+1` → 11.0%、`+3..+5` → **0%**，**没有任何偏移能接近 100%**
  ⇒ 不是「差一个常数」的错位，是**两套不同的排序**。

---

## 7. 影响面（逐条 · 只列不改 · 按严重度排）

| 严重度 | 受影响的东西 | 证据 / 读数 |
|---|---|---|
| 🔴 **1** | **`card_ids.json` 的 `id` 列本身**（不是它的名字列）：996 条里**至少 17 条已被证明是错的**（真值可查的那 17 条全错）；其余**没有本地判据能单独判定**（见 §8） | §2 |
| 🔴 **2** | **`gen_prebuilt_decks.py` 的 `match()` / `hero_match()`**：`hero_match:264` 直查卡池取错卡；`match()` 退化成恒等映射 ⇒ **产物里的 id 与牌组里的 id 同字不同卡** | `工具/gen_prebuilt_decks.py:210-283` |
| 🔴 **3** | **`Resources/prebuilt_decks.json`（103 副）**：**43 副的 `cardIds` 里有督军卡、共 84 张**（合法牌组不可能有督军）。例：`BlackLegionDeck1` = 2×Abaddon + 2×Haarken + 2×Sylar Hexcorn；`AstraMilitarumDeck1` = 2×Ursula Creed；`Deck_UM_Titus` = 1×Marneus Calgar | 本轮实测（照 `cards_engine.json` 的 `type=="hero"` 判） |
| 🔴 **4** | **`Resources/tutorial_decks.json`（12 副）**：`Orks_Deck0_Tutorial2_Ghazghkull` 那副的 `cardIds` 与 SO 的 `cardLibraryIds` **逐字相同**（只少了那个 GUID）⇒ 教程里「玩家打 Grot×3」而牌库里放的是 **Grukk Face-Rippa×3**。产物自己的 `problems` 列已经写着 `over2:GOF1 x3;GOF9 x3;GOF32 x3;GOF4 x3;GOF16 x3`、`overLegendary:GOF32 x3 (Ardshell Gurk)` —— **症状已经报出来了，只是被当成「上游缺 id」** | 本轮实测 + `tutorial_decks.json` 的 `problems` 列 |
| 🟠 **5** | **`check_prebuilt_decks.py` 的全部结论**（它是 `gen_prebuilt_decks.py` 规则的唯一副本，也是 `资料/原版预组牌_核对.md` 这个生成物的来源）：「能拼齐 / 拼不齐」这套数**量的是错配后的能不能对上**，`complete = 70/103` 这个数**不表示「还原对了 70 副」** | `工具/check_prebuilt_decks.py:265-400` |
| 🟠 **6** | **`warlord_ids.json` 的 value 列**（57 条）：key 是对的（= 原版 `heroId`），value 在 52 个可比的 key 上**只有 5 个与我们表一致**，且其中 `TL74` 已被证明是坏的 ⇒ **这张表不能用来自证，也不能拿来当「谁对」的判据** | 本轮实测（52 条逐条列出） |
| 🟡 **7** | **连带产物**：`card_icon_plan.json` · `voice_lines.json` · `Resources/Art/cards/art_*.png` · `cards_engine.json` 的 995 条原版形态 id | §5-C |
| 🟡 **8** | **`A876`（`GSC1` 到底是谁）**：2026-10-17 那次裁定「**裁 `card_ids.json` 对**，判据 = PnP 目录编号 + 池内一致性」—— **判据本身循环**（PnP 编号就是待证对象）。按本轮结论，那一裁**很可能是反的**（`warlord_ids` 的 `GSC1 = GSC_Warlord_Primus Saffa` 反而更接近原版）。⚠️ **我没有独立证据钉死 `GSC1`**，只能标「**存疑、需重裁**」 | `资料/待办判据_1018.md:121` · `资料/普查产出_1017/W_B26_卡id回填.md:99` |
| 🟢 **9** | **「哪几张卡是对的」目前**没有**可复算的清单**：两套编号在同族内高度重叠（EC/SW 的 DT 号 **58/58 全落在我们表的号域内**），所以**不能靠号段粗筛**，必须逐条比 | 本轮实测 |

---

## 8. 没查清的部分（⛔ 不拿猜测填空）

1. 🔴 **「原版 id → 卡名」的完整表本地拿不全**。目前的直接来源只有三处、都很窄：
   `DT *` × 教程卡名 = **17 条**（只有 UM/GOF 有名字，EC/SW 只有 id 没有名字）·
   预组 SO 的 `deckHero.targetId` + SO 资产名尾段（**督军 ~63 条，且只是「名字里含这个词」的近似**）·
   `heroId` 集合本身（不含名字）。
   ⇒ **「我们表里到底错了几条、错在哪几条」这个问题，本轮给不出可复算的全量清单**。**不猜。**
2. ⚠️ **`0/17` 的样本只覆盖 UM/GOF 两个阵营、且 UM 占 13 条**。EC/SW 只参与了「稀有度分布」这一把（无名字的）尺子。
   ⇒ 结论的**普遍性**靠的是检验 C/D（n=315 / n=236）与检验 E（产物实测），不是靠那 17 条。
   但**「除 UM/GOF 外每一个阵营都错了」这句话我没有逐条证据**，只能说「没有反例、且量级一致」。
3. ⚠️ **`UM103/105/113..118` 这批号在我们表里**根本不存在**（我们最大 `UM101`）。
   它们是真卡还是别的东西（皮肤/异画/未上线卡）—— **没查**。
4. ⚠️ **抽卡包「包名 = 包内某一张卡」这条只有 3/3（可测样本只有 3 个）**，
   而且要推翻的反面证据很硬：`资料/原版预组牌_核对.md` 写着「`bundle_draftpacks_assets_all` 里包名与该包内卡名**无关**
   （`Bomb Squig.json` 的 `cardIds` 是 GOF3/36/61），**不能用它反推名字**」。
   **标记「疑似」**：3 个正例全部命中**真值 id**（`UM34`=Point-Blank Shot · `UM14`=Honour Guard · `UM13`=Primaris Chaplain），
   而那条反面结论是在**假定我们表正确**的前提下得出的。**要不要把它当第六个来源，请调度台先派一条只读的专项核**（882 个包）。
5. ⚠️ **`deckName` 是不是该督军天赋卡的名字**：我测了 236 副的 `(heroId, deckName)`，
   **同一个 heroId 平均带 2.4 个不同 deckName**（`AM3` 一个人就有 `Press the Attack` / `Lead by Example` / `Armoured Regiment` …）
   ⇒ **不是函数关系、不能用来推 id**。但里面有几处巧合值得留意（`AM3` 那副叫 `Lead by Example`，
   而我们表把 `Lead by Example` 放在 `AM2`；`ASH1` 那副叫 `Path of the Seer`，我们表放在 `ASH2`）——
   **只记现象，不据此下结论**。
6. ⚠️ **`0/17` 里那 5 个「我们表是空号」（`UM6/UM8/UM33/UM56`）**：它们是真空号，还是我们表把号放到了别处
   （`资料/原版预组牌_核对.md:238` 已把 `UM6`=Master of Arcana、`UM8`=Phobos Lieutenant、`UM15`=Primaris Reiver
   当作「能补」的 A 类）—— **本轮没有逐号裁**。
7. ⚠️ **「原版号 1..118 是**谁的**顺序」不知道**（是不是 Collection 里的排序？）—— 本轮没查，
   而它决定了「重做这张表」有没有捷径。**没查，不猜。**

---

## 9. 我搜过的目录与关键词（🔴 「全仓没有 X」必附）

**搜过的目录**（全部只读）：
`d:/2/新解包资源/assets_full/`（**90 个 bundle 全列过一遍**；逐个看过 `bundle_prebuiltdecks_assets_all` 236 份、
`bundle_tutorialso_assets_all` 7 份、`bundle_cosmeticsso_assets_all` 全部 `DT *` 33 份、
`bundle_duplicateassetisolationso_assets_all` 15 份、`bundle_draftpacks_assets_all` 882 份、
`bundle_menus_assets_all/MonoBehaviour/Campaign Node Data*`、`bundle_uiwarlords_assets_all`、
`bundle_cosmeticavatarsimages_assets_all/Sprite` 475 份、`bundle_spacemarinesultramarinescardassets_assets_all/Sprite`）
· `d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`（**84 个 bundle 全列过**）
· `d:/2/Warpforge部队卡片/` · `E:/BaiduNetdiskDownload/Warpforge带编号/` · `d:/warpforge/{tools,scripts,autoload,data}` ·
`d:/4/Unity/{工具,数据/游戏数据,MyGame/Assets,资料}` · `C:/Users/qjh36/AppData/LocalLow/{Everguild,Unity/Everguild_Warpforge}`（只看目录名）。

**搜过的关键词**：
`allcards` · `cardLibraryIds` · `"UM23"` · `"UM103"` · `"UM105"` · `Bladeguard Lieutenant` ·
`card_ids` · `card_ids.json` · `warlord_ids` · `mapping` · `"mapping"` · `PnP` · `PnP 卡表编号` · `卡表编号` · `编号对不上` ·
`Warpforge_` · `cardId` · `deckHero` · `targetId` · `NodeId` · `deckName` · `命名空间` · `原版 id` · `A876`。

**明确说「没有」的三条（附搜法）**：

1. **`allcards` bundle 本地不存在**（R-ASSET 的边界声明成立）。搜法：
   ① `ls d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`（84 项，**无 `allcards*`**）；
   ② `find /d/2 /d/4 -iname "*allcards*"`（命中**全在**反编译产物 `*.c`，另有 `Hand_Buff_UM_AllCards.json` 那种同名动画，**与卡数据无关**）；
   ③ `ls -d d:/2/新解包资源/assets_full/*/`（90 项，无）。
2. **本仓没有 `card_ids.json` 的生成器**。搜法：`grep -rn "card_ids" --include=*.py --include=*.cs d:/4`（读了 4 处：`gen_cards_engine.py` / `gen_prebuilt_decks.py` / `check_prebuilt_decks.py` / `RuleEngineTest.cs`，**全是「读」**）；
   `git log --diff-filter=A -- 数据/游戏数据/card_ids.json` = **只有初始提交 `87aef98`**；
   再追到 `d:/warpforge/tools/export_game_data.py:328`（**写的是空表**）+ `deck_collection.gd:3`（"OCR 扩展点"）⇒ **它是「补进去的」，不是一个可重跑的脚本产物**。
   ⚠️ **没找到**（也没去找）把 OCR 结果灌进去的那一步 —— 那个脚本**不在本仓、也不在老 Godot 仓的 `tools/`**。
3. **`card_ids.json` 的编号列本身有没有可复算的生成规则**：`E:/…/Warpforge带编号/` 的改名规则（`资料/原版预组牌_核对.md:246-252`）
   只解释了 **Core 段**，DLC 段的偏移规则在**同一份文档 `:236-238`** 里被自己标成「别把任何单一偏移当规则」。
   ⇒ **没有可复算的规则**，本轮也**没有**去反推新的规则。

---

## 附：给调度台的三句话

1. **这条账要开，而且优先级应该在 `A876` 之上** —— `A876`（两张自建表打架）其实是这条账的**一个断面**，
   而它的裁定用的判据（PnP 编号）正好是被推翻的那一个。**先重裁 `A876` 再谈别的。**
2. **不要现在去动 `card_ids.json` 的编号列** —— 本轮没有全量真值表（§8-1），
   硬改等于把「已知的错」换成「未知的错」。**正确的下一步是先把「原版 id→卡名」的采集面做大**
   （重点：`draftpacks` 882 个包那条疑似线索 §8-4；EC/SW 目前只有 id 没有名字）。
3. **本轮一个字节都没改**：没跑生成器、没跑 Unity、没动 git、没回填任何 id（含 `GOF2`/`GOF1`）。
